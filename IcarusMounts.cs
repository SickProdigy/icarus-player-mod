using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace IcarusProfileMod;

internal sealed record MountInjectionDefinition(
    string TypeKey,
    string DisplayName,
    string AiSetupRowName,
    string BlueprintClassName,
    string Description,
    bool Rideable = true,
    int MaxLevel = 50)
{
    public string DefaultName => new(DisplayName.Where(char.IsLetterOrDigit).ToArray());
    public string DisplayText => Rideable ? $"{DisplayName} ({TypeKey})" : $"{DisplayName} ({TypeKey}, companion)";
}
internal sealed class IcarusMounts
{
    public static IReadOnlyList<MountInjectionDefinition> SupportedInjectionTypes { get; } =
    [
        new("Dog", "Dog", "Tame_Dog_D1", "BP_Tame_Dog_D1_C", "Companion pet with the Creature_Dog talent tree.", Rideable: false, MaxLevel: 25),
        new("Cat", "Cat", "Tame_Cat_B1", "BP_Tame_Cat_B_C", "Companion pet with the Creature_Cat talent tree.", Rideable: false, MaxLevel: 25),
        new("Chicken", "Chicken", "Chicken", "BP_Tame_Chicken_C", "Tamed farm animal with the Creature_Chicken talent tree.", Rideable: false, MaxLevel: 25),
        new("Rooster", "Rooster", "Rooster", "BP_Tame_Rooster_C", "Tamed farm animal with the Creature_Rooster talent tree.", Rideable: false, MaxLevel: 25),
        new("Sheep", "Sheep", "Sheep", "BP_Tame_Sheep_C", "Tamed farm animal with the Creature_Sheep talent tree.", Rideable: false, MaxLevel: 25),
        new("Ram", "Ram", "Ram", "BP_Tame_Ram_C", "Tamed farm animal with the Creature_Ram talent tree.", Rideable: false, MaxLevel: 25),
        new("Cow", "Cow", "Cow", "BP_Tame_Cow_C", "Tamed farm animal with the Creature_Cow talent tree.", Rideable: false, MaxLevel: 25),
        new("Pig", "Pig", "Pig", "BP_Tame_Pig_C", "Tamed farm animal with the Creature_Pig talent tree.", Rideable: false, MaxLevel: 25),
        new("Wolf", "Wolf", "Tamed_Forest_Wolf", "BP_Tamed_Wolf_C", "Tamed wolf with the Creature_Wolf talent tree.", Rideable: false, MaxLevel: 25),
        new("SnowWolf", "Snow Wolf", "Tamed_Snow_Wolf", "BP_Tamed_Wolf_Snow_C", "Tamed snow wolf with the Creature_Snow_Wolf talent tree.", Rideable: false, MaxLevel: 25),
        new("Hyena", "Hyena", "Tamed_Desert_Wolf", "BP_Tamed_Wolf_Desert_C", "Tamed desert wolf with the Creature_Desert_Wolf talent tree.", Rideable: false, MaxLevel: 25),
        new("MiniHippo", "Mini Hippo", "Mini_Hippo_Quest", "BP_Mount_MiniHippo_Quest_C", "Companion creature using the station mount save format.", Rideable: false, MaxLevel: 25),
        new("BluebackDaisy", "Blueback Daisy", "Quest_Blueback_Daisy", "BP_Mount_Blueback_Daisy_C", "Companion creature with the Creature_Blueback talent tree.", Rideable: false, MaxLevel: 25),
        new("Skulk", "Skulk", "Tamed_Orka", "BP_Tamed_Orka_C", "Tamed skulk with the Creature_Orka talent tree.", Rideable: false, MaxLevel: 25),
        new("Storca", "Storca", "Tamed_Storca", "BP_Tamed_Storca_C", "Tamed storca with the Creature_Storca talent tree.", Rideable: false, MaxLevel: 25),
        new("Gribbler", "Gribbler", "Tamed_Tundra_Monkey", "BP_Tamed_Tundra_Monkey_C", "Tamed gribbler with the Creature_Tundra_Monkey talent tree.", Rideable: false, MaxLevel: 25),
        new("Terrenus", "Terrenus", "Mount_Horse", "BP_Mount_Horse_C", "Wild alien mount.", MaxLevel: 50),
        new("Horse", "Horse", "Mount_Horse_Standard_A3", "BP_Mount_Horse_Standard_C", "Workshop horse variant.", MaxLevel: 50),
        new("Moa", "Moa", "Mount_Moa", "BP_Mount_Moa_C", "Fast rideable mount.", MaxLevel: 50),
        new("ArcticMoa", "Arctic Moa", "Mount_Arctic_Moa", "BP_Mount_Arctic_Moa_C", "Cold-resistant moa variant.", MaxLevel: 50),
        new("Buffalo", "Buffalo", "Mount_Buffalo", "BP_Mount_Buffalo_C", "Large carrying-capacity mount.", MaxLevel: 50),
        new("Tusker", "Tusker", "Mount_Tusker", "BP_Mount_Tusker_C", "Large arctic mount.", MaxLevel: 50),
        new("Zebra", "Zebra", "Mount_Zebra", "BP_Mount_Zebra_C", "Fast rideable mount.", MaxLevel: 50),
        new("WoolyZebra", "Shaggy Zebra", "Mount_WoolyZebra", "BP_Mount_Wooly_Zebra_C", "Cold-resistant zebra variant.", MaxLevel: 50),
        new("SwampBird", "Ubi", "Mount_SwampBird", "BP_Mount_SwampBird_C", "Swamp bird mount.", MaxLevel: 50),
        new("WoollyMammoth", "Woolly Mammoth", "Mount_WoollyMammoth", "BP_Mount_WoollyMammoth_C", "Massive arctic mount.", MaxLevel: 50),
        new("Bull", "Bull", "Mount_Bull", "BP_Mount_Bull_C", "Large rideable creature with the Creature_Bull talent tree.", MaxLevel: 50),
        new("Raptor", "Raptor", "Mount_Raptor", "BP_Mount_Raptor_C", "Rideable raptor with the Creature_Raptor talent tree.", MaxLevel: 50),
        new("DuneRaptor", "Dune Raptor", "Mount_Raptor_Desert", "BP_Mount_Raptor_Desert_C", "Rideable desert raptor with the Creature_Raptor_Desert talent tree.", MaxLevel: 50),
        new("Draven", "Draven", "Mount_Chew", "BP_Mount_Chew_C", "Rideable draven with the Creature_Chew talent tree.", MaxLevel: 50),
        new("Slinker", "Slinker", "Mount_Slinker", "BP_Mount_Slinker_C", "Rideable slinker with the Creature_Slinker talent tree.", MaxLevel: 50)
    ];
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

    public IcarusMount InjectMount(MountInjectionDefinition definition, string name, IcarusMount? template = null)
    {
        if (_root["SavedMounts"] is not JsonArray savedMounts)
        {
            savedMounts = [];
            _root["SavedMounts"] = savedMounts;
        }

        template ??= _mounts.FirstOrDefault();
        if (template is null)
        {
            throw new InvalidOperationException("Injecting a mount requires at least one existing station mount to use as a save-format template.");
        }

        JsonObject root = (JsonObject)template.Root.DeepClone();
        List<UePropertyTag> properties = CloneProperties(template.Properties);
        int actorId = GenerateUniqueActorId();
        int objectSuffix = GenerateUniqueObjectSuffix();
        int index = savedMounts.Count;
        IcarusMount mount = new(index, root, properties);
        mount.ConfigureInjected(definition, name, actorId, objectSuffix);

        savedMounts.Add(root);
        _mounts.Add(mount);
        return mount;
    }

    private int GenerateUniqueActorId()
    {
        HashSet<int> existingIds = new(_mounts.SelectMany(mount => mount.KnownIntegerIds()));
        for (int attempt = 0; attempt < 1000; attempt++)
        {
            int candidate = Random.Shared.Next(100000, 999999);
            if (!existingIds.Contains(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Could not generate a unique mount actor id.");
    }

    private int GenerateUniqueObjectSuffix()
    {
        HashSet<int> existingIds = new(_mounts.SelectMany(mount => mount.KnownObjectSuffixes()));
        for (int attempt = 0; attempt < 1000; attempt++)
        {
            int candidate = Random.Shared.Next(2147000000, int.MaxValue);
            if (!existingIds.Contains(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Could not generate a unique mount object id.");
    }

    private static List<UePropertyTag> CloneProperties(IEnumerable<UePropertyTag> properties)
    {
        return properties.Select(CloneProperty).ToList();
    }

    private static UePropertyTag CloneProperty(UePropertyTag property)
    {
        UePropertyTag clone = new(property.Name, property.TypeName)
        {
            Value = CloneValue(property.Value),
            InnerType = property.InnerType,
            StructType = property.StructType,
            EnumType = property.EnumType,
            ElementName = property.ElementName
        };
        foreach (UePropertyTag child in property.Nested)
        {
            clone.Nested.Add(CloneProperty(child));
        }
        return clone;
    }

    private static object? CloneValue(object? value)
    {
        return value is byte[] bytes ? bytes.ToArray() : value;
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

    public int MaxLevel => NormalizeMountType(MountType) switch
    {
        "Dog" or "Cat" or "Chicken" or "Rooster" or "Sheep" or "Ram" or "Cow" or "Pig" or "Wolf" or "SnowWolf" or "Hyena" or "MiniHippo" or "BluebackDaisy" or "Skulk" or "Storca" or "Gribbler" => 25,
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
            string type = NormalizeMountType(MountType);
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
                ["Ubi"] = "Creature_SwampBird",
                ["Blueback"] = "Creature_Blueback",
                ["BluebackDaisy"] = "Creature_Blueback",
                ["Bull"] = "Creature_Bull",
                ["Raptor"] = "Creature_Raptor",
                ["DuneRaptor"] = "Creature_Raptor_Desert",
                ["DesertRaptor"] = "Creature_Raptor_Desert",
                ["Draven"] = "Creature_Chew",
                ["Chew"] = "Creature_Chew",
                ["Slinker"] = "Creature_Slinker",
                ["Wolf"] = "Creature_Wolf",
                ["SnowWolf"] = "Creature_Snow_Wolf",
                ["Hyena"] = "Creature_Desert_Wolf",
                ["DesertWolf"] = "Creature_Desert_Wolf",
                ["Skulk"] = "Creature_Orka",
                ["Orka"] = "Creature_Orka",
                ["Storca"] = "Creature_Storca",
                ["Gribbler"] = "Creature_Tundra_Monkey",
                ["TundraMonkey"] = "Creature_Tundra_Monkey"
            };

            return aliases.TryGetValue(type, out string? treeRowName) ? treeRowName : $"Creature_{type}";
        }
    }

    private static string NormalizeMountType(string mountType)
    {
        return new string(mountType.Where(char.IsLetterOrDigit).ToArray());
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

    public void ConfigureInjected(MountInjectionDefinition definition, string name, int actorId, int objectSuffix)
    {
        string objectName = $"{definition.BlueprintClassName}_{objectSuffix}";
        Root["DatabaseGUID"] = "noguid";
        Root["MountName"] = name;
        Root["MountType"] = definition.TypeKey;
        Root["MountIconName"] = actorId.ToString();

        SetStringProperty("MountName", name, "StrProperty");
        SetStringProperty("AISetupRowName", definition.AiSetupRowName, "NameProperty");
        SetStringProperty("ActorClassName", definition.BlueprintClassName, "NameProperty");
        SetStringProperty("ObjectFName", objectName, "NameProperty");
        SetStringProperty("ActorPathName", BuildInjectedActorPath(objectName), "StrProperty");
        SetIntProperty("IcarusActorGUID", actorId);
        ClearStructArray("Talents");
        ClearStructArray("Modifiers");
        ClearStructArray("StomachContents");
        Level = 0;
    }

    public IEnumerable<int> KnownIntegerIds()
    {
        if (int.TryParse(Root["MountIconName"]?.GetValue<string>(), out int iconId))
        {
            yield return iconId;
        }

        int? actorGuid = GetIntProperty("IcarusActorGUID");
        if (actorGuid.HasValue)
        {
            yield return actorGuid.Value;
        }
    }

    public IEnumerable<int> KnownObjectSuffixes()
    {
        foreach (string? value in new[] { GetStringProperty("ObjectFName"), GetStringProperty("ActorPathName") })
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            string suffix = value[(value.LastIndexOf('_') + 1)..];
            if (int.TryParse(suffix, out int id))
            {
                yield return id;
            }
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

    private void SetStringProperty(string name, string value, string typeName)
    {
        UePropertyTag? property = UePropertySerializer.FindProperty(Properties, name);
        if (property is not null)
        {
            property.TypeName = typeName;
            property.Value = value;
        }
    }

    private void ClearStructArray(string name)
    {
        UePropertyTag? property = UePropertySerializer.FindProperty(Properties, name);
        if (property is not null)
        {
            property.InnerType = "StructProperty";
            property.ElementName ??= name;
            property.Nested.Clear();
        }
    }

    private string BuildInjectedActorPath(string objectName)
    {
        string? existingPath = GetStringProperty("ActorPathName");
        if (string.IsNullOrWhiteSpace(existingPath))
        {
            return objectName;
        }

        int lastDot = existingPath.LastIndexOf('.');
        if (lastDot < 0)
        {
            return objectName;
        }

        return existingPath[..(lastDot + 1)] + objectName;
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
