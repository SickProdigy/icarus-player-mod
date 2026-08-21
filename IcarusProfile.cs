using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IcarusProfileMod;

internal sealed class IcarusProfile
{
    private readonly JsonObject _root;

    private IcarusProfile(string path, JsonObject root)
    {
        Path = path;
        _root = root;
    }

    public string Path { get; }

    public string UserId => _root["UserID"]?.GetValue<string>() ?? "Unknown";

    public IReadOnlyList<MetaResource> MetaResources
    {
        get
        {
            JsonArray? resources = _root["MetaResources"] as JsonArray;
            if (resources is null)
            {
                return Array.Empty<MetaResource>();
            }

            return resources
                .OfType<JsonObject>()
                .Select(item => new MetaResource(
                    item["MetaRow"]?.GetValue<string>() ?? "",
                    item["Count"]?.GetValue<int>() ?? 0))
                .Where(item => !string.IsNullOrWhiteSpace(item.Name))
                .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public static IcarusProfile Load(string path)
    {
        string json = File.ReadAllText(path);
        JsonNode? node = JsonNode.Parse(json);
        if (node is not JsonObject root)
        {
            throw new InvalidDataException("Profile.json did not contain a JSON object.");
        }

        return new IcarusProfile(path, root);
    }

    public void SetMetaResource(string name, int count)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Resource name is required.", nameof(name));
        }

        JsonArray resources = GetOrCreateMetaResources();
        JsonObject? existing = resources
            .OfType<JsonObject>()
            .FirstOrDefault(item => string.Equals(
                item["MetaRow"]?.GetValue<string>(),
                name,
                StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            resources.Add(new JsonObject
            {
                ["MetaRow"] = name,
                ["Count"] = count
            });
            return;
        }

        existing["Count"] = count;
    }

    public string SaveWithBackup()
    {
        string backupPath = CreateBackupPath(Path);
        File.Copy(Path, backupPath, overwrite: false);

        JsonSerializerOptions options = new()
        {
            WriteIndented = true
        };

        File.WriteAllText(Path, _root.ToJsonString(options));
        return backupPath;
    }

    private JsonArray GetOrCreateMetaResources()
    {
        if (_root["MetaResources"] is JsonArray resources)
        {
            return resources;
        }

        resources = new JsonArray();
        _root["MetaResources"] = resources;
        return resources;
    }

    private static string CreateBackupPath(string profilePath)
    {
        string directory = System.IO.Path.GetDirectoryName(profilePath) ?? "";
        string fileName = System.IO.Path.GetFileNameWithoutExtension(profilePath);
        string extension = System.IO.Path.GetExtension(profilePath);
        string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        return System.IO.Path.Combine(directory, $"{fileName}.backup-{timestamp}{extension}");
    }
}

internal sealed record MetaResource(string Name, int Count);
