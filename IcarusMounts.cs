using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace IcarusProfileMod;

internal sealed class IcarusMounts
{
    private readonly JsonObject _root;
    private readonly List<IcarusMount> _mounts;
    private readonly UePropertySerializer _serializer = new();

    private IcarusMounts(string path, JsonObject root, List<IcarusMount> mounts)
    {
        Path = path;
        _root = root;
        _mounts = mounts;
    }

    public string Path { get; }
    public IReadOnlyList<IcarusMount> Mounts => _mounts;

    public static IcarusMounts Load(string path)
    {
        string json = File.ReadAllText(path);
        JsonNode? node = JsonNode.Parse(json);
        if (node is not JsonObject root || root["SavedMounts"] is not JsonArray savedMounts)
        {
            throw new InvalidDataException("Mounts.json did not contain the expected SavedMounts array.");
        }

        UePropertySerializer serializer = new();
        List<IcarusMount> mounts = [];
        for (int i = 0; i < savedMounts.Count; i++)
        {
            if (savedMounts[i] is not JsonObject mountRoot)
            {
                continue;
            }

            JsonArray? binaryArray = mountRoot["RecorderBlob"]?["BinaryData"] as JsonArray;
            if (binaryArray is null)
            {
                continue;
            }

            byte[] binaryData = binaryArray.Select(value => value?.GetValue<byte>() ?? 0).ToArray();
            mounts.Add(new IcarusMount(i, mountRoot, serializer.Deserialize(binaryData)));
        }

        return new IcarusMounts(path, root, mounts);
    }

    public string SaveWithBackup()
    {
        string backupPath = CreateBackupPath(Path);
        File.Copy(Path, backupPath, overwrite: false);

        if (_root["SavedMounts"] is not JsonArray savedMounts)
        {
            savedMounts = [];
            _root["SavedMounts"] = savedMounts;
        }

        foreach (IcarusMount mount in _mounts)
        {
            JsonObject mountRoot = mount.Root;
            byte[] binaryData = _serializer.Serialize(mount.Properties);
            if (mountRoot["RecorderBlob"] is not JsonObject recorderBlob)
            {
                recorderBlob = [];
                mountRoot["RecorderBlob"] = recorderBlob;
            }

            JsonArray binaryArray = [];
            foreach (byte value in binaryData)
            {
                binaryArray.Add(value);
            }
            recorderBlob["BinaryData"] = binaryArray;

            while (savedMounts.Count <= mount.Index)
            {
                savedMounts.Add(null);
            }

            if (savedMounts[mount.Index] is null)
            {
                savedMounts[mount.Index] = mountRoot;
            }
        }

        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
        File.WriteAllText(Path, _root.ToJsonString(options));
        return backupPath;
    }

    private static string CreateBackupPath(string filePath)
    {
        string directory = System.IO.Path.GetDirectoryName(filePath) ?? "";
        string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
        string extension = System.IO.Path.GetExtension(filePath);
        string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        return System.IO.Path.Combine(directory, $"{fileName}.backup-{timestamp}{extension}");
    }
}

internal sealed class IcarusMount
{
    public IcarusMount(int index, JsonObject root, List<UePropertyTag> properties)
    {
        Index = index;
        Root = root;
        Properties = properties;
    }

    public int Index { get; }
    public JsonObject Root { get; }
    public List<UePropertyTag> Properties { get; }

    public string Name => Root["MountName"]?.GetValue<string>() ?? GetStringProperty("MountName") ?? "Unknown";

    public int Level
    {
        get
        {
            int jsonLevel = Root["MountLevel"]?.GetValue<int>() ?? 0;
            int actorLevel = GetActorIntVariable("LastLevelAchieved") ?? 0;
            int experienceLevel = EstimateLevelFromExperience(Experience);
            return Math.Clamp(Math.Max(Math.Max(jsonLevel, actorLevel), experienceLevel), 0, MaxLevel);
        }
        set
        {
            int cappedLevel = Math.Clamp(value, 0, MaxLevel);
            Root["MountLevel"] = cappedLevel;
            SetIntProperty("Experience", EstimateExperienceForLevel(cappedLevel));
            SetActorIntVariable("LastLevelAchieved", cappedLevel);
        }
    }

    public int MaxLevel => MountType switch
    {
        "Dog" or "Cat" => 25,
        _ => 50
    };

    public string MountType => Root["MountType"]?.GetValue<string>() ?? "Unknown";
    public string ActorClassName => GetStringProperty("ActorClassName") ?? "";
    public string AiSetupRowName => GetStringProperty("AISetupRowName") ?? "";
    public int Experience => GetIntProperty("Experience") ?? 0;
    public int? CurrentHealth => GetIntProperty("CurrentHealth");
    public int? Stamina => GetIntProperty("Stamina");

    public string CreatureTreeRowName
    {
        get
        {
            string type = MountType.Replace(" ", "", StringComparison.OrdinalIgnoreCase);
            Dictionary<string, string> aliases = new(StringComparer.OrdinalIgnoreCase)
            {
                ["Dog"] = "Creature_Dog",
                ["Cat"] = "Creature_Cat",
                ["Horse"] = "Creature_Horse",
                ["Moa"] = "Creature_Moa",
                ["ArcticMoa"] = "Creature_ArcticMoa",
                ["Buffalo"] = "Creature_Buffalo",
                ["Tusker"] = "Creature_Tusker",
                ["Terrenus"] = "Creature_Terrenus",
                ["Zebra"] = "Creature_Zebra",
                ["WoolyZebra"] = "Creature_WoolyZebra",
                ["WoollyMammoth"] = "Creature_WoollyMammoth",
                ["SwampBird"] = "Creature_SwampBird",
                ["BluebackDaisy"] = "Creature_Blueback"
            };

            return aliases.TryGetValue(type, out string? treeRowName) ? treeRowName : $"Creature_{type}";
        }
    }

    public IReadOnlyList<TalentEntry> Talents
    {
        get
        {
            UePropertyTag? talents = GetTalentsProperty();
            if (talents is null)
            {
                return Array.Empty<TalentEntry>();
            }

            return talents.Nested
                .Select(ReadTalentEntry)
                .Where(entry => !string.IsNullOrWhiteSpace(entry.RowName))
                .OrderBy(entry => entry.RowName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public override string ToString()
    {
        return $"{Name} ({MountType}, level {Level})";
    }

    public void SetTalent(string rowName, int rank)
    {
        UePropertyTag talents = GetOrCreateTalentsProperty();
        RemoveEmptyTalentEntries(talents);
        UePropertyTag? existing = talents.Nested.FirstOrDefault(item =>
            string.Equals(ReadTalentEntry(item).RowName, rowName, StringComparison.OrdinalIgnoreCase));

        if (rank <= 0)
        {
            if (existing is not null)
            {
                talents.Nested.Remove(existing);
            }
            return;
        }

        if (existing is null)
        {
            talents.Nested.Add(CreateTalentStruct(rowName, rank));
            return;
        }

        WriteTalentEntry(existing, rowName, rank);
    }

    private UePropertyTag? GetTalentsProperty()
    {
        return UePropertySerializer.FindProperty(Properties, "Talents");
    }

    private UePropertyTag GetOrCreateTalentsProperty()
    {
        UePropertyTag? talents = GetTalentsProperty();
        if (talents is not null)
        {
            talents.InnerType = "StructProperty";
            talents.ElementName ??= "Talents";
            talents.StructType ??= "MountTalentSaveData";
            return talents;
        }

        talents = new UePropertyTag("Talents", "ArrayProperty")
        {
            InnerType = "StructProperty",
            ElementName = "Talents",
            StructType = "MountTalentSaveData"
        };
        Properties.Add(talents);
        return talents;
    }

    private static void RemoveEmptyTalentEntries(UePropertyTag talents)
    {
        talents.Nested.RemoveAll(item => string.IsNullOrWhiteSpace(ReadTalentEntry(item).RowName));
    }

    private static TalentEntry ReadTalentEntry(UePropertyTag element)
    {
        string rowName = GetStringValue(element.Find("TalentRowName"))
            ?? GetStringValue(element.Find("RowName"))
            ?? GetStringValue(element.Find("TalentName"))
            ?? GetStringValue(element.Find("Talent"))
            ?? "";
        int rank = GetIntValue(element.Find("TalentRank"))
            ?? GetIntValue(element.Find("Rank"))
            ?? GetIntValue(element.Find("Level"))
            ?? 0;
        return new TalentEntry(rowName, rank);
    }

    private static void WriteTalentEntry(UePropertyTag element, string rowName, int rank)
    {
        UePropertyTag rowNameProperty = element.Find("TalentRowName")
            ?? element.Find("RowName")
            ?? element.Find("TalentName")
            ?? element.Find("Talent")
            ?? AddNested(element, new UePropertyTag("TalentRowName", "StrProperty"));
        rowNameProperty.TypeName = "StrProperty";
        rowNameProperty.Value = rowName;

        UePropertyTag rankProperty = element.Find("TalentRank")
            ?? element.Find("Rank")
            ?? element.Find("Level")
            ?? AddNested(element, new UePropertyTag("TalentRank", "IntProperty"));
        rankProperty.Value = rank;
    }

    private static UePropertyTag CreateTalentStruct(string rowName, int rank)
    {
        UePropertyTag element = new("Talents", "StructProperty")
        {
            StructType = "MountTalentSaveData"
        };
        WriteTalentEntry(element, rowName, rank);
        return element;
    }

    private string? GetStringProperty(string name)
    {
        return GetStringValue(UePropertySerializer.FindProperty(Properties, name));
    }

    private int? GetIntProperty(string name)
    {
        return GetIntValue(UePropertySerializer.FindProperty(Properties, name));
    }

    private int? GetActorIntVariable(string variableName)
    {
        UePropertyTag? intVariables = UePropertySerializer.FindProperty(Properties, "IntVariables");
        if (intVariables is null)
        {
            return null;
        }

        UePropertyTag? variable = intVariables.Nested.FirstOrDefault(item =>
            string.Equals(GetStringValue(item.Find("VariableName")), variableName, StringComparison.OrdinalIgnoreCase));
        return GetIntValue(variable?.Find("iVariable"));
    }

    private void SetActorIntVariable(string variableName, int value)
    {
        UePropertyTag intVariables = GetOrCreateStructArray("IntVariables", "ActorIntVariableRecord");
        UePropertyTag? variable = intVariables.Nested.FirstOrDefault(item =>
            string.Equals(GetStringValue(item.Find("VariableName")), variableName, StringComparison.OrdinalIgnoreCase));

        if (variable is null)
        {
            variable = new UePropertyTag("IntVariables", "StructProperty")
            {
                StructType = "ActorIntVariableRecord"
            };
            AddNested(variable, new UePropertyTag("VariableName", "NameProperty") { Value = variableName });
            AddNested(variable, new UePropertyTag("iVariable", "IntProperty") { Value = value });
            intVariables.Nested.Add(variable);
            return;
        }

        UePropertyTag valueProperty = variable.Find("iVariable")
            ?? AddNested(variable, new UePropertyTag("iVariable", "IntProperty"));
        valueProperty.Value = value;
    }

    private void SetIntProperty(string name, int value)
    {
        UePropertyTag? property = UePropertySerializer.FindProperty(Properties, name);
        if (property is not null)
        {
            property.Value = value;
        }
    }

    private UePropertyTag GetOrCreateStructArray(string name, string structType)
    {
        UePropertyTag? property = UePropertySerializer.FindProperty(Properties, name);
        if (property is not null)
        {
            property.InnerType = "StructProperty";
            property.ElementName ??= name;
            property.StructType ??= structType;
            return property;
        }

        property = new UePropertyTag(name, "ArrayProperty")
        {
            InnerType = "StructProperty",
            ElementName = name,
            StructType = structType
        };
        Properties.Add(property);
        return property;
    }

    private static string? GetStringValue(UePropertyTag? property)
    {
        return property?.Value as string;
    }

    private static int? GetIntValue(UePropertyTag? property)
    {
        return property?.Value switch
        {
            int value => value,
            uint value => (int)value,
            _ => null
        };
    }

    private static UePropertyTag AddNested(UePropertyTag parent, UePropertyTag child)
    {
        parent.Nested.Add(child);
        return child;
    }

    private int EstimateLevelFromExperience(int experience)
    {
        int bestLevel = 0;
        int bestDelta = int.MaxValue;
        for (int level = 0; level <= MaxLevel; level++)
        {
            int delta = Math.Abs(EstimateExperienceForLevel(level) - experience);
            if (delta < bestDelta)
            {
                bestDelta = delta;
                bestLevel = level;
            }
        }

        return bestLevel;
    }

    private static int EstimateExperienceForLevel(int level)
    {
        if (level <= 1)
        {
            return 0;
        }

        (int Level, int Xp, int Tangent)[] keypoints =
        [
            (10, 13500, 2250),
            (30, 140000, 17000),
            (50, 1150000, 88000)
        ];

        if (level < keypoints[0].Level)
        {
            return Math.Max(0, keypoints[0].Xp - (keypoints[0].Level - level) * keypoints[0].Tangent);
        }

        if (level >= keypoints[^1].Level)
        {
            return keypoints[^1].Xp + (level - keypoints[^1].Level) * keypoints[^1].Tangent;
        }

        for (int i = 0; i < keypoints.Length - 1; i++)
        {
            (int l0, int xp0, int t0) = keypoints[i];
            (int l1, int xp1, int t1) = keypoints[i + 1];
            if (level < l0 || level >= l1)
            {
                continue;
            }

            double t = (double)(level - l0) / (l1 - l0);
            double t2 = t * t;
            double t3 = t2 * t;
            double h00 = 2 * t3 - 3 * t2 + 1;
            double h10 = t3 - 2 * t2 + t;
            double h01 = -2 * t3 + 3 * t2;
            double h11 = t3 - t2;
            int span = l1 - l0;
            return (int)(h00 * xp0 + h10 * t0 * span + h01 * xp1 + h11 * t1 * span);
        }

        return 0;
    }
}
