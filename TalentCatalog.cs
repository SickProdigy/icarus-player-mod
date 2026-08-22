using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json.Nodes;

namespace IcarusProfileMod;

internal sealed class TalentCatalog
{
    private static readonly HashSet<string> CharacterTalentArchetypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Combat",
        "Construction",
        "Player_Adventure",
        "Solo",
        "Survival"
    };

    private readonly Dictionary<string, TalentMetadata> _talents;

    private TalentCatalog(string path, Dictionary<string, TalentMetadata> talents)
    {
        Path = path;
        _talents = talents;
    }

    public string Path { get; }

    public int Count => _talents.Count;

    public IEnumerable<TalentMetadata> Talents => _talents.Values.OrderBy(talent => talent.DisplayName, StringComparer.OrdinalIgnoreCase);

    public IEnumerable<TalentMetadata> CharacterTalents => Talents.Where(talent => talent.IsCharacterTalent);

    public IEnumerable<TalentMetadata> Blueprints => Talents.Where(talent =>
        talent.TreeArchetype.StartsWith("Blueprint_", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(talent.TalentType, "Reroute", StringComparison.OrdinalIgnoreCase));

    public IEnumerable<TalentMetadata> CreatureTalents => Talents.Where(talent =>
        talent.TreeRowName.StartsWith("Creature_", StringComparison.OrdinalIgnoreCase));

    public static TalentCatalog LoadFromDirectory(string directory)
    {
        string talentsPath = System.IO.Path.Combine(directory, "D_Talents.json");
        string treesPath = System.IO.Path.Combine(directory, "D_TalentTrees.json");
        string ranksPath = System.IO.Path.Combine(directory, "D_TalentRanks.json");

        if (!File.Exists(talentsPath))
        {
            throw new FileNotFoundException("D_Talents.json was not found.", talentsPath);
        }

        Dictionary<string, TalentTreeMetadata> trees = File.Exists(treesPath)
            ? LoadTalentTrees(treesPath)
            : new Dictionary<string, TalentTreeMetadata>(StringComparer.OrdinalIgnoreCase);

        if (File.Exists(ranksPath))
        {
            _ = LoadDisplayNames(ranksPath);
        }

        return Load(talentsPath, trees);
    }

    public static TalentCatalog Load(string path)
    {
        return Load(path, new Dictionary<string, TalentTreeMetadata>(StringComparer.OrdinalIgnoreCase));
    }

    public static string? FindDefaultDirectory()
    {
        foreach (string directory in GetCandidateDirectories())
        {
            if (HasTalentCatalog(directory))
            {
                return directory;
            }
        }

        return null;
    }

    public static bool HasTalentCatalog(string directory)
    {
        return File.Exists(System.IO.Path.Combine(directory, "D_Talents.json"))
            && File.Exists(System.IO.Path.Combine(directory, "D_TalentTrees.json"))
            && File.Exists(System.IO.Path.Combine(directory, "D_TalentRanks.json"));
    }

    public TalentMetadata? Find(string rowName)
    {
        return _talents.TryGetValue(rowName, out TalentMetadata? metadata) ? metadata : null;
    }

    private static TalentCatalog Load(string path, Dictionary<string, TalentTreeMetadata> trees)
    {
        string json = File.ReadAllText(path);
        JsonNode? node = JsonNode.Parse(json);
        if (node is not JsonObject root || root["Rows"] is not JsonArray rows)
        {
            throw new InvalidDataException("D_Talents.json did not contain the expected Rows array.");
        }

        Dictionary<string, TalentMetadata> talents = new(StringComparer.OrdinalIgnoreCase);
        JsonObject defaults = root["Defaults"] as JsonObject ?? [];

        foreach (JsonObject row in rows.OfType<JsonObject>())
        {
            string rowName = row["Name"]?.GetValue<string>() ?? "";
            if (string.IsNullOrWhiteSpace(rowName))
            {
                continue;
            }

            string displayName = ExtractDisplayText(row["DisplayName"]?.GetValue<string>() ?? "");
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = rowName;
            }

            string treeName = (row["TalentTree"] as JsonObject)?["RowName"]?.GetValue<string>() ?? "";
            TalentTreeMetadata tree = trees.TryGetValue(treeName, out TalentTreeMetadata? metadata)
                ? metadata
                : new TalentTreeMetadata(treeName, treeName, treeName);
            string talentType = row["TalentType"]?.GetValue<string>() ?? defaults["TalentType"]?.GetValue<string>() ?? "Talent";
            int maxRank = GetMaxRank(row, defaults, talentType);
            bool isCharacterTalent = IsCharacterTalentTree(tree);

            talents[rowName] = new TalentMetadata(
                rowName,
                displayName,
                tree.RowName,
                tree.DisplayName,
                tree.Archetype,
                talentType,
                maxRank,
                isCharacterTalent);
        }

        return new TalentCatalog(path, talents);
    }

    private static bool IsCharacterTalentTree(TalentTreeMetadata tree)
    {
        return CharacterTalentArchetypes.Contains(tree.Archetype);
    }

    private static int GetMaxRank(JsonObject row, JsonObject defaults, string talentType)
    {
        if (row["Rewards"] is JsonArray explicitRewards)
        {
            return string.Equals(talentType, "Reroute", StringComparison.OrdinalIgnoreCase)
                ? 0
                : Math.Max(1, explicitRewards.Count);
        }

        if (defaults["Rewards"] is JsonArray defaultRewards)
        {
            return Math.Max(1, defaultRewards.Count);
        }

        return 1;
    }

    private static Dictionary<string, TalentTreeMetadata> LoadTalentTrees(string path)
    {
        string json = File.ReadAllText(path);
        JsonNode? node = JsonNode.Parse(json);
        if (node is not JsonObject root || root["Rows"] is not JsonArray rows)
        {
            throw new InvalidDataException($"{System.IO.Path.GetFileName(path)} did not contain the expected Rows array.");
        }

        Dictionary<string, TalentTreeMetadata> trees = new(StringComparer.OrdinalIgnoreCase);
        foreach (JsonObject row in rows.OfType<JsonObject>())
        {
            string rowName = row["Name"]?.GetValue<string>() ?? "";
            if (string.IsNullOrWhiteSpace(rowName))
            {
                continue;
            }

            string displayName = ExtractDisplayText(row["DisplayName"]?.GetValue<string>() ?? "");
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = rowName;
            }

            string archetype = (row["Archetype"] as JsonObject)?["RowName"]?.GetValue<string>() ?? rowName;
            trees[rowName] = new TalentTreeMetadata(rowName, displayName, archetype);
        }

        return trees;
    }

    private static Dictionary<string, string> LoadDisplayNames(string path)
    {
        string json = File.ReadAllText(path);
        JsonNode? node = JsonNode.Parse(json);
        if (node is not JsonObject root || root["Rows"] is not JsonArray rows)
        {
            throw new InvalidDataException($"{System.IO.Path.GetFileName(path)} did not contain the expected Rows array.");
        }

        Dictionary<string, string> displayNames = new(StringComparer.OrdinalIgnoreCase);
        foreach (JsonObject row in rows.OfType<JsonObject>())
        {
            string rowName = row["Name"]?.GetValue<string>() ?? "";
            if (string.IsNullOrWhiteSpace(rowName))
            {
                continue;
            }

            string displayName = ExtractDisplayText(row["DisplayName"]?.GetValue<string>() ?? "");
            displayNames[rowName] = string.IsNullOrWhiteSpace(displayName) ? rowName : displayName;
        }

        return displayNames;
    }

    private static IEnumerable<string> GetCandidateDirectories()
    {
        string baseDirectory = AppContext.BaseDirectory;
        yield return System.IO.Path.Combine(baseDirectory, "data");

        string currentDirectory = Environment.CurrentDirectory;
        yield return System.IO.Path.Combine(currentDirectory, "data");

        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            yield return System.IO.Path.Combine(localAppData, "IcarusProfileMod", "data");
        }

        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (!string.IsNullOrWhiteSpace(documents))
        {
            yield return System.IO.Path.Combine(documents, "IcarusProfileMod", "data");
        }
    }

    private static string ExtractDisplayText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        Match match = Regex.Match(value, @"NSLOCTEXT\(""[^""\\]*(?:\\.[^""\\]*)*"",\s*""[^""\\]*(?:\\.[^""\\]*)*"",\s*""(?<text>(?:\\.|[^""\\])*)""\)");
        if (!match.Success)
        {
            return value;
        }

        return match.Groups["text"].Value.Replace("\\\"", "\"");
    }
}

internal sealed record TalentTreeMetadata(string RowName, string DisplayName, string Archetype);

internal sealed record TalentMetadata(
    string RowName,
    string DisplayName,
    string TreeRowName,
    string TreeName,
    string TreeArchetype,
    string TalentType,
    int MaxRank,
    bool IsCharacterTalent);

