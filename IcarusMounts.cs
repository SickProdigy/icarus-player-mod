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

internal sealed record CreatureAppearanceVariant(
    string TypeKey,
    string DisplayName,
    string AiSetupRowName,
    string BlueprintClassName,
    int Variation,
    string Source)
{
    public string DisplayText => string.IsNullOrWhiteSpace(Source) ? DisplayName : $"{DisplayName} ({Source})";
    public override string ToString() => DisplayText;
}

internal sealed record CreatureGeneticEntry(string Name, int Value);

internal sealed class IcarusMounts
{
    private const string TemplateFileName = "MountTemplate.json";

    private static readonly IReadOnlyDictionary<string, int> MaxLevelsByType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["Dog"] = 25,
        ["Cat"] = 25,
        ["Chicken"] = 25,
        ["Rooster"] = 25,
        ["Sheep"] = 25,
        ["Ram"] = 25,
        ["Cow"] = 25,
        ["Pig"] = 25,
        ["Wolf"] = 25,
        ["SnowWolf"] = 25,
        ["Hyena"] = 25,
        ["MiniHippo"] = 50,
        ["BluebackDaisy"] = 50,
        ["Skulk"] = 25,
        ["Storca"] = 25,
        ["Gribbler"] = 25,
        ["Terrenus"] = 50,
        ["Horse"] = 50,
        ["Moa"] = 50,
        ["ArcticMoa"] = 50,
        ["Buffalo"] = 50,
        ["Tusker"] = 50,
        ["Zebra"] = 50,
        ["WoolyZebra"] = 50,
        ["SwampBird"] = 50,
        ["WoollyMammoth"] = 50,
        ["Bull"] = 50,
        ["Raptor"] = 50,
        ["DuneRaptor"] = 50,
        ["Draven"] = 50,
        ["Slinker"] = 50
    };

    public static IReadOnlyList<MountInjectionDefinition> SupportedInjectionTypes { get; } =
    [
        new("Dog", "Dog", "Tame_Dog_D1", "BP_Tame_Dog_D1_C", "Companion pet with the Creature_Dog talent tree.", Rideable: false, MaxLevel: GetMaxLevelForType("Dog")),
        new("Cat", "Cat", "Tame_Cat_B1", "BP_Tame_Cat_B_C", "Companion pet with the Creature_Cat talent tree.", Rideable: false, MaxLevel: GetMaxLevelForType("Cat")),
        new("Chicken", "Chicken", "Chicken", "BP_Tame_Chicken_C", "Tamed farm animal with the Creature_Chicken talent tree.", Rideable: false, MaxLevel: GetMaxLevelForType("Chicken")),
        new("Rooster", "Rooster", "Rooster", "BP_Tame_Rooster_C", "Tamed farm animal with the Creature_Rooster talent tree.", Rideable: false, MaxLevel: GetMaxLevelForType("Rooster")),
        new("Sheep", "Sheep", "Sheep", "BP_Tame_Sheep_C", "Tamed farm animal with the Creature_Sheep talent tree.", Rideable: false, MaxLevel: GetMaxLevelForType("Sheep")),
        new("Ram", "Ram", "Ram", "BP_Tame_Ram_C", "Tamed farm animal with the Creature_Ram talent tree.", Rideable: false, MaxLevel: GetMaxLevelForType("Ram")),
        new("Cow", "Cow", "Cow", "BP_Tame_Cow_C", "Tamed farm animal with the Creature_Cow talent tree.", Rideable: false, MaxLevel: GetMaxLevelForType("Cow")),
        new("Pig", "Pig", "Pig", "BP_Tame_Pig_C", "Tamed farm animal with the Creature_Pig talent tree.", Rideable: false, MaxLevel: GetMaxLevelForType("Pig")),
        new("Wolf", "Wolf", "Tamed_Forest_Wolf", "BP_Tamed_Wolf_C", "Tamed wolf with the Creature_Wolf talent tree.", Rideable: false, MaxLevel: GetMaxLevelForType("Wolf")),
        new("SnowWolf", "Snow Wolf", "Tamed_Snow_Wolf", "BP_Tamed_Wolf_Snow_C", "Tamed snow wolf with the Creature_Snow_Wolf talent tree.", Rideable: false, MaxLevel: GetMaxLevelForType("SnowWolf")),
        new("Hyena", "Hyena", "Tamed_Desert_Wolf", "BP_Tamed_Wolf_Desert_C", "Tamed desert wolf with the Creature_Desert_Wolf talent tree.", Rideable: false, MaxLevel: GetMaxLevelForType("Hyena")),
        new("MiniHippo", "Mini Hippo", "Mini_Hippo_Quest", "BP_Mount_MiniHippo_Quest_C", "Companion creature using the station mount save format.", Rideable: false, MaxLevel: GetMaxLevelForType("MiniHippo")),
        new("BluebackDaisy", "Blueback Daisy", "Quest_Blueback_Daisy", "BP_Mount_Blueback_Daisy_C", "Companion creature with the Creature_Blueback talent tree.", Rideable: false, MaxLevel: GetMaxLevelForType("BluebackDaisy")),
        new("Skulk", "Skulk", "Tamed_Orka", "BP_Tamed_Orka_C", "Tamed skulk with the Creature_Orka talent tree.", Rideable: false, MaxLevel: GetMaxLevelForType("Skulk")),
        new("Storca", "Storca", "Tamed_Storca", "BP_Tamed_Storca_C", "Tamed storca with the Creature_Storca talent tree.", Rideable: false, MaxLevel: GetMaxLevelForType("Storca")),
        new("Gribbler", "Gribbler", "Tamed_Tundra_Monkey", "BP_Tamed_Tundra_Monkey_C", "Tamed gribbler with the Creature_Tundra_Monkey talent tree.", Rideable: false, MaxLevel: GetMaxLevelForType("Gribbler")),
        new("Terrenus", "Terrenus", "Mount_Horse", "BP_Mount_Horse_C", "Wild alien mount.", MaxLevel: GetMaxLevelForType("Terrenus")),
        new("Horse", "Horse", "Mount_Horse_Standard_A3", "BP_Mount_Horse_Standard_C", "Workshop horse variant.", MaxLevel: GetMaxLevelForType("Horse")),
        new("Moa", "Moa", "Mount_Moa", "BP_Mount_Moa_C", "Fast rideable mount.", MaxLevel: GetMaxLevelForType("Moa")),
        new("ArcticMoa", "Arctic Moa", "Mount_Arctic_Moa", "BP_Mount_Arctic_Moa_C", "Cold-resistant moa variant.", MaxLevel: GetMaxLevelForType("ArcticMoa")),
        new("Buffalo", "Buffalo", "Mount_Buffalo", "BP_Mount_Buffalo_C", "Large carrying-capacity mount.", MaxLevel: GetMaxLevelForType("Buffalo")),
        new("Tusker", "Tusker", "Mount_Tusker", "BP_Mount_Tusker_C", "Large arctic mount.", MaxLevel: GetMaxLevelForType("Tusker")),
        new("Zebra", "Zebra", "Mount_Zebra", "BP_Mount_Zebra_C", "Fast rideable mount.", MaxLevel: GetMaxLevelForType("Zebra")),
        new("WoolyZebra", "Shaggy Zebra", "Mount_WoolyZebra", "BP_Mount_Wooly_Zebra_C", "Cold-resistant zebra variant.", MaxLevel: GetMaxLevelForType("WoolyZebra")),
        new("SwampBird", "Ubi", "Mount_SwampBird", "BP_Mount_SwampBird_C", "Swamp bird mount.", MaxLevel: GetMaxLevelForType("SwampBird")),
        new("WoollyMammoth", "Woolly Mammoth", "Mount_WoollyMammoth", "BP_Mount_WoollyMammoth_C", "Massive arctic mount.", MaxLevel: GetMaxLevelForType("WoollyMammoth")),
        new("Bull", "Bull", "Mount_Bull", "BP_Mount_Bull_C", "Large rideable creature with the Creature_Bull talent tree.", MaxLevel: GetMaxLevelForType("Bull")),
        new("Raptor", "Raptor", "Mount_Raptor", "BP_Mount_Raptor_C", "Rideable raptor with the Creature_Raptor talent tree.", MaxLevel: GetMaxLevelForType("Raptor")),
        new("DuneRaptor", "Dune Raptor", "Mount_Raptor_Desert", "BP_Mount_Raptor_Desert_C", "Rideable desert raptor that uses the Creature_Raptor mount talent tree.", MaxLevel: GetMaxLevelForType("DuneRaptor")),
        new("Draven", "Draven", "Mount_Chew", "BP_Mount_Chew_C", "Rideable draven with the Creature_Chew talent tree.", MaxLevel: GetMaxLevelForType("Draven")),
        new("Slinker", "Slinker", "Mount_Slinker", "BP_Mount_Slinker_C", "Rideable slinker with the Creature_Slinker talent tree.", MaxLevel: GetMaxLevelForType("Slinker"))
    ];

    public static IReadOnlyList<CreatureAppearanceVariant> KnownAppearanceVariants { get; } =
    [
        new("Horse", "Brown Horse", "Mount_Horse_Standard_A1", "BP_Mount_Horse_Standard_C", 0, "Pet Companions"),
        new("Horse", "Black Horse", "Mount_Horse_Standard_A2", "BP_Mount_Horse_Standard_C", 1, "Pet Companions"),
        new("Horse", "White Horse", "Mount_Horse_Standard_A3", "BP_Mount_Horse_Standard_C", 2, "Pet Companions"),

        new("Dog", "Golden Labrador", "Tame_Dog_A1", "BP_Tame_Dog_A1_C", 0, "Pet Companions"),
        new("Dog", "Chocolate Labrador", "Tame_Dog_A2", "BP_Tame_Dog_A2_C", 1, "Pet Companions"),
        new("Dog", "German Shepherd", "Tame_Dog_B1", "BP_Tame_Dog_B1_C", 0, "Pet Companions"),
        new("Dog", "Panda German Shepherd", "Tame_Dog_B2", "BP_Tame_Dog_B2_C", 1, "Pet Companions"),
        new("Dog", "Pug", "Tame_Dog_C1", "BP_Tame_Dog_C1_C", 0, "Pet Companions"),
        new("Dog", "Tan Laika", "Tame_Dog_D1", "BP_Tame_Dog_D1_C", 0, "Pet Companions"),
        new("Dog", "Brown Laika", "Tame_Dog_D2", "BP_Tame_Dog_D2_C", 1, "Pet Companions"),
        new("Dog", "French Bulldog", "Tame_Dog_C2", "BP_Tame_Dog_C2_C", 1, "Homestead"),
        new("Dog", "Border Collie", "Tame_Dog_E", "BP_Tame_Dog_E_C", 1, "Homestead"),

        new("Cat", "Grey Tabby Cat", "Tame_Cat_A1", "BP_Tame_Cat_C", 0, "Pet Companions"),
        new("Cat", "Orange Tabby Cat", "Tame_Cat_A2", "BP_Tame_Cat_C", 1, "Pet Companions"),
        new("Cat", "Black Cat", "Tame_Cat_A3", "BP_Tame_Cat_C", 2, "Pet Companions"),
        new("Cat", "Himalayan Seal Point Cat", "Tame_Cat_B", "BP_Tame_Cat_B_C", 2, "Homestead"),
        new("Cat", "Tortoise Shell Ragdoll Cat", "Tame_Cat_C", "BP_Tame_Cat_C_C", 2, "Homestead")
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

    public static int GetMaxLevelForType(string mountType)
    {
        string normalized = new(mountType.Where(char.IsLetterOrDigit).ToArray());
        return MaxLevelsByType.TryGetValue(normalized, out int maxLevel) ? maxLevel : 50;
    }

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

    public static IcarusMounts CreateEmpty(string path)
    {
        JsonObject root = new()
        {
            ["SavedMounts"] = new JsonArray()
        };
        return new IcarusMounts(path, root, []);
    }

    public IcarusMount InjectMount(MountInjectionDefinition definition, string name, IcarusMount? template = null)
    {
        if (_root["SavedMounts"] is not JsonArray savedMounts)
        {
            savedMounts = [];
            _root["SavedMounts"] = savedMounts;
        }

        template ??= _mounts.FirstOrDefault() ?? LoadBundledTemplateMount();
        if (template is null)
        {
            throw new InvalidOperationException("Could not load the bundled mount template.");
        }

        JsonObject root = (JsonObject)template.Root.DeepClone();
        List<UePropertyTag> properties = CloneProperties(template.Properties);
        int actorId = GenerateUniqueActorId();
        int objectSuffix = GenerateUniqueObjectSuffix();
        int index = savedMounts.Count;
        IcarusMount mount = new(index, root, properties);
        mount.ConfigureInjected(definition, name, actorId, objectSuffix, GetOwnerPlayerId());

        savedMounts.Add(root);
        _mounts.Add(mount);
        return mount;
    }

    private static IcarusMount? LoadBundledTemplateMount()
    {
        foreach (string templatePath in GetTemplatePaths())
        {
            if (!File.Exists(templatePath))
            {
                continue;
            }

            IcarusMounts templateMounts = Load(templatePath);
            return templateMounts.Mounts.FirstOrDefault();
        }

        return null;
    }

    private static IEnumerable<string> GetTemplatePaths()
    {
        yield return System.IO.Path.Combine(AppContext.BaseDirectory, "data", TemplateFileName);
        yield return System.IO.Path.Combine(AppContext.BaseDirectory, TemplateFileName);
        yield return System.IO.Path.Combine(Environment.CurrentDirectory, "data", TemplateFileName);
    }

    private string? GetOwnerPlayerId()
    {
        string? playerFolder = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(Path));
        return string.IsNullOrWhiteSpace(playerFolder) ? null : playerFolder;
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
    public string? SaveWithBackup()
    {
        string? backupPath = null;
        string? directory = System.IO.Path.GetDirectoryName(Path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(Path))
        {
            backupPath = CreateBackupPath(Path);
            File.Copy(Path, backupPath, overwrite: false);
        }

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

    public int MaxLevel => IcarusMounts.GetMaxLevelForType(MountType);

    public string MountType => Root["MountType"]?.GetValue<string>() ?? "Unknown";
    public string ActorClassName => GetStringProperty("ActorClassName") ?? "";
    public string AiSetupRowName => GetStringProperty("AISetupRowName") ?? "";
    public int Experience => GetIntProperty("Experience") ?? 0;
    public int? CurrentHealth => GetIntProperty("CurrentHealth");
    public int? Stamina => GetIntProperty("Stamina");
    public int? FoodLevel => GetIntProperty("FoodLevel");
    public int? WaterLevel => GetIntProperty("WaterLevel");
    public int? OxygenLevel => GetIntProperty("OxygenLevel");
    public int? Variation => GetIntProperty("Variation");
    public int? UniqueVariation => GetIntProperty("UniqueVariation");
    public int? CosmeticSkinIndex => GetActorIntVariable("CosmeticSkinIndex");
    public int? AlternateCosmeticSkinIndex => GetActorIntVariable("CosmeticSkinIndex_0");
    public string AppearanceLabel => GetAppearanceVariant()?.DisplayName ?? "";

    public IReadOnlyList<CreatureAppearanceVariant> AppearanceVariants =>
        IcarusMounts.KnownAppearanceVariants
            .Where(variant => SameType(variant.TypeKey, MountType))
            .ToList();

    public CreatureAppearanceVariant? GetAppearanceVariant()
    {
        List<CreatureAppearanceVariant> variants = AppearanceVariants.ToList();
        if (variants.Count == 0)
        {
            return null;
        }

        string aiSetupRowName = AiSetupRowName;
        string actorClassName = ActorClassName;
        int variation = Variation ?? 0;

        return variants.FirstOrDefault(variant =>
                SameRowName(variant.AiSetupRowName, aiSetupRowName)
                && SameClassName(variant.BlueprintClassName, actorClassName))
            ?? variants.FirstOrDefault(variant =>
                SameClassName(variant.BlueprintClassName, actorClassName)
                && variant.Variation == variation)
            ?? variants.FirstOrDefault(variant =>
                SameRowName(variant.AiSetupRowName, aiSetupRowName)
                && variant.Variation == variation)
            ?? variants.FirstOrDefault(variant => variant.Variation == variation);
    }

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
                ["DuneRaptor"] = "Creature_Raptor",
                ["RaptorDesert"] = "Creature_Raptor",
                ["DesertRaptor"] = "Creature_Raptor",
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

    private static bool SameType(string left, string right)
    {
        return string.Equals(NormalizeMountType(left), NormalizeMountType(right), StringComparison.OrdinalIgnoreCase);
    }

    private static bool SameRowName(string left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private static bool SameClassName(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        string normalizedLeft = NormalizeClassName(left);
        string normalizedRight = NormalizeClassName(right);
        return string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeClassName(string className)
    {
        string normalized = className.Trim().Trim('\'', '"');
        int slash = normalized.LastIndexOf('/');
        if (slash >= 0)
        {
            normalized = normalized[(slash + 1)..];
        }

        int dot = normalized.LastIndexOf('.');
        if (dot >= 0)
        {
            normalized = normalized[(dot + 1)..];
        }

        return normalized;
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

    public void ConfigureInjected(MountInjectionDefinition definition, string name, int actorId, int objectSuffix, string? ownerPlayerId = null)
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
        if (!string.IsNullOrWhiteSpace(ownerPlayerId))
        {
            SetStringProperty("PlayerID", ownerPlayerId, "StrProperty");
        }
        SetStringProperty("OwnerName", "", "StrProperty");
        SetIntProperty("IcarusActorGUID", actorId);
        ClearStructArray("Talents");
        ClearStructArray("Modifiers");
        ClearStructArray("StomachContents");
        Level = 0;
    }

    public IReadOnlyList<CreatureGeneticEntry> Genetics
    {
        get
        {
            UePropertyTag? genetics = GetGeneticsProperty();
            if (genetics is null)
            {
                return Array.Empty<CreatureGeneticEntry>();
            }

            return genetics.Nested
                .Select(ReadGeneticEntry)
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
                .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
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

    public void SetName(string name)
    {
        string cleaned = string.IsNullOrWhiteSpace(name) ? MountType : name.Trim();
        Root["MountName"] = cleaned;
        SetStringProperty("MountName", cleaned, "StrProperty");
    }

    public void SetSpecies(MountInjectionDefinition definition)
    {
        Root["MountType"] = definition.TypeKey;
        SetStringProperty("AISetupRowName", definition.AiSetupRowName, "NameProperty");
        SetStringProperty("ActorClassName", definition.BlueprintClassName, "NameProperty");
        UpdateActorObjectNames(definition.BlueprintClassName);
        Level = Math.Clamp(Level, 0, definition.MaxLevel);
        ClearStructArray("Talents");
    }

    public void SetCurrentHealth(int value)
    {
        SetIntProperty("CurrentHealth", Math.Max(0, value));
    }

    public void SetStamina(int value)
    {
        SetIntProperty("Stamina", Math.Max(0, value));
    }

    public void SetFoodLevel(int value)
    {
        SetIntProperty("FoodLevel", Math.Max(0, value));
    }

    public void SetWaterLevel(int value)
    {
        SetIntProperty("WaterLevel", Math.Max(0, value));
    }

    public void SetOxygenLevel(int value)
    {
        SetIntProperty("OxygenLevel", Math.Max(0, value));
    }

    public void SetVariation(int value)
    {
        SetIntProperty("Variation", Math.Max(0, value));
    }

    public void SetAppearanceVariant(CreatureAppearanceVariant variant)
    {
        if (!SameType(variant.TypeKey, MountType))
        {
            return;
        }

        SetStringProperty("AISetupRowName", variant.AiSetupRowName, "NameProperty");
        SetStringProperty("ActorClassName", variant.BlueprintClassName, "NameProperty");
        UpdateActorObjectNames(variant.BlueprintClassName);
        SetVariation(variant.Variation);
    }

    public void SetUniqueVariation(int value)
    {
        SetIntProperty("UniqueVariation", Math.Max(0, value));
    }

    public void SetCosmeticSkinIndex(int value)
    {
        SetActorIntVariable("CosmeticSkinIndex", value);
    }

    public void SetAlternateCosmeticSkinIndex(int value)
    {
        SetActorIntVariable("CosmeticSkinIndex_0", value);
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

    public void SetGeneticLevel(string name, int level)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        UePropertyTag genetics = GetOrCreateGeneticsProperty();
        UePropertyTag? existing = genetics.Nested.FirstOrDefault(item =>
            string.Equals(ReadGeneticEntry(item).Name, name, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            genetics.Nested.Add(CreateGeneticStruct(name, level));
            return;
        }

        WriteGeneticEntry(existing, name, level);
    }

    private UePropertyTag? GetTalentsProperty()
    {
        return UePropertySerializer.FindProperty(Properties, "Talents");
    }

    private UePropertyTag? GetGeneticsProperty()
    {
        return UePropertySerializer.FindProperty(Properties, "Genetics");
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

    private UePropertyTag GetOrCreateGeneticsProperty()
    {
        UePropertyTag? genetics = GetGeneticsProperty();
        if (genetics is not null)
        {
            genetics.InnerType = "StructProperty";
            genetics.ElementName ??= "Genetics";
            genetics.StructType ??= "MountGeneticsSaveData";
            return genetics;
        }

        genetics = new UePropertyTag("Genetics", "ArrayProperty")
        {
            InnerType = "StructProperty",
            ElementName = "Genetics",
            StructType = "MountGeneticsSaveData"
        };
        Properties.Add(genetics);
        return genetics;
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

    private static CreatureGeneticEntry ReadGeneticEntry(UePropertyTag element)
    {
        string name = GetStringValue(element.Find("GeneticValueName"))
            ?? GetStringValue(element.Find("Name"))
            ?? "";
        int value = GetIntValue(element.Find("Value"))
            ?? GetIntValue(element.Find("LevelIndex"))
            ?? GetIntValue(element.Find("Level"))
            ?? 0;
        return new CreatureGeneticEntry(name, value);
    }

    private static void WriteGeneticEntry(UePropertyTag element, string name, int value)
    {
        UePropertyTag nameProperty = element.Find("GeneticValueName")
            ?? element.Find("Name")
            ?? AddNested(element, new UePropertyTag("GeneticValueName", "NameProperty"));
        nameProperty.TypeName = "NameProperty";
        nameProperty.Value = name;

        UePropertyTag valueProperty = element.Find("Value")
            ?? element.Find("LevelIndex")
            ?? element.Find("Level")
            ?? AddNested(element, new UePropertyTag("Value", "IntProperty"));
        valueProperty.Name = "Value";
        valueProperty.TypeName = "IntProperty";
        valueProperty.Value = Math.Max(0, value);
    }

    private static UePropertyTag CreateGeneticStruct(string name, int value)
    {
        UePropertyTag element = new("Genetics", "StructProperty")
        {
            StructType = "MountGeneticsSaveData"
        };
        WriteGeneticEntry(element, name, value);
        return element;
    }

    private void SetStringProperty(string name, string value, string typeName)
    {
        UePropertyTag? property = UePropertySerializer.FindProperty(Properties, name);
        if (property is null)
        {
            property = new UePropertyTag(name, typeName);
            Properties.Add(property);
        }

        property.TypeName = typeName;
        property.Value = value;
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

    private void UpdateActorObjectNames(string blueprintClassName)
    {
        string existingObjectName = GetStringProperty("ObjectFName") ?? "";
        string suffix = "";
        int lastUnderscore = existingObjectName.LastIndexOf('_');
        if (lastUnderscore >= 0 && lastUnderscore < existingObjectName.Length - 1)
        {
            suffix = existingObjectName[(lastUnderscore + 1)..];
        }

        if (string.IsNullOrWhiteSpace(suffix) || !suffix.All(char.IsDigit))
        {
            suffix = Random.Shared.Next(2147000000, int.MaxValue).ToString();
        }

        string objectName = $"{blueprintClassName}_{suffix}";
        SetStringProperty("ObjectFName", objectName, "NameProperty");
        SetStringProperty("ActorPathName", BuildActorPathForObject(objectName), "StrProperty");
    }

    private string BuildActorPathForObject(string objectName)
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
        if (property is null)
        {
            property = new UePropertyTag(name, "IntProperty");
            Properties.Add(property);
        }

        property.TypeName = "IntProperty";
        property.Value = value;
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
