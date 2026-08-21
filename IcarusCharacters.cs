using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IcarusProfileMod;

internal sealed class IcarusCharacters
{
    private const string WrapperKey = "Characters.json";

    private readonly JsonObject _root;
    private readonly List<IcarusCharacter> _characters;

    private IcarusCharacters(string path, JsonObject root, List<IcarusCharacter> characters)
    {
        Path = path;
        _root = root;
        _characters = characters;
    }

    public string Path { get; }

    public IReadOnlyList<IcarusCharacter> Characters => _characters;

    public static IcarusCharacters Load(string path)
    {
        string json = File.ReadAllText(path);
        JsonNode? node = JsonNode.Parse(json);
        if (node is not JsonObject root)
        {
            throw new InvalidDataException("Characters.json did not contain a JSON object.");
        }

        if (root[WrapperKey] is not JsonArray characterStrings)
        {
            throw new InvalidDataException("Characters.json did not contain the expected Characters.json array.");
        }

        List<IcarusCharacter> characters = [];
        for (int i = 0; i < characterStrings.Count; i++)
        {
            string? characterJson = characterStrings[i]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(characterJson))
            {
                continue;
            }

            JsonNode? characterNode = JsonNode.Parse(characterJson);
            if (characterNode is JsonObject characterRoot)
            {
                characters.Add(new IcarusCharacter(i, characterRoot));
            }
        }

        return new IcarusCharacters(path, root, characters);
    }

    public string SaveWithBackup()
    {
        string backupPath = CreateBackupPath(Path);
        File.Copy(Path, backupPath, overwrite: false);

        JsonSerializerOptions options = new()
        {
            WriteIndented = true
        };

        if (_root[WrapperKey] is not JsonArray characterStrings)
        {
            characterStrings = [];
            _root[WrapperKey] = characterStrings;
        }

        foreach (IcarusCharacter character in _characters)
        {
            string characterJson = character.ToJsonString(options);
            while (characterStrings.Count <= character.Index)
            {
                characterStrings.Add("");
            }

            characterStrings[character.Index] = JsonValue.Create(characterJson);
        }

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

internal sealed class IcarusCharacter
{
    private readonly JsonObject _root;

    public IcarusCharacter(int index, JsonObject root)
    {
        Index = index;
        _root = root;
    }

    public int Index { get; }

    public string Name => _root["CharacterName"]?.GetValue<string>() ?? "Unknown";

    public int Slot => _root["ChrSlot"]?.GetValue<int>() ?? Index;

    public int Xp => _root["XP"]?.GetValue<int>() ?? 0;

    public string DisplayName => $"{Name} (slot {Slot})";

    public override string ToString()
    {
        return DisplayName;
    }

    public IReadOnlyList<TalentEntry> Talents
    {
        get
        {
            JsonArray? talents = _root["Talents"] as JsonArray;
            if (talents is null)
            {
                return Array.Empty<TalentEntry>();
            }

            return talents
                .OfType<JsonObject>()
                .Select(item => new TalentEntry(
                    item["RowName"]?.GetValue<string>() ?? "",
                    item["Rank"]?.GetValue<int>() ?? 0))
                .Where(item => !string.IsNullOrWhiteSpace(item.RowName))
                .OrderBy(item => item.RowName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public void SetTalent(string rowName, int rank)
    {
        if (string.IsNullOrWhiteSpace(rowName))
        {
            throw new ArgumentException("Talent RowName is required.", nameof(rowName));
        }

        JsonArray talents = GetOrCreateTalents();
        JsonObject? existing = talents
            .OfType<JsonObject>()
            .FirstOrDefault(item => string.Equals(
                item["RowName"]?.GetValue<string>(),
                rowName,
                StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            talents.Add(new JsonObject
            {
                ["RowName"] = rowName,
                ["Rank"] = rank
            });
            return;
        }

        existing["Rank"] = rank;
    }

    public string ToJsonString(JsonSerializerOptions options)
    {
        return _root.ToJsonString(options);
    }

    private JsonArray GetOrCreateTalents()
    {
        if (_root["Talents"] is JsonArray talents)
        {
            return talents;
        }

        talents = [];
        _root["Talents"] = talents;
        return talents;
    }
}

internal sealed record TalentEntry(string RowName, int Rank);

