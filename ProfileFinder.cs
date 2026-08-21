using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IcarusProfileMod;

internal static class ProfileFinder
{
    public static IReadOnlyList<string> FindProfiles()
    {
        return FindPlayerDataFiles("Profile.json");
    }

    public static IReadOnlyList<string> FindCharactersFiles()
    {
        return FindPlayerDataFiles("Characters.json");
    }

    public static IReadOnlyList<string> FindMountsFiles()
    {
        return FindPlayerDataFiles("Mounts.json");
    }

    public static string GetCharactersPathForProfile(string profilePath)
    {
        string directory = Path.GetDirectoryName(profilePath) ?? "";
        return Path.Combine(directory, "Characters.json");
    }

    public static string GetMountsPathForProfile(string profilePath)
    {
        string directory = Path.GetDirectoryName(profilePath) ?? "";
        return Path.Combine(directory, "Mounts.json");
    }

    private static IReadOnlyList<string> FindPlayerDataFiles(string fileName)
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string playerDataPath = Path.Combine(localAppData, "Icarus", "Saved", "PlayerData");

        if (!Directory.Exists(playerDataPath))
        {
            return Array.Empty<string>();
        }

        return Directory
            .EnumerateFiles(playerDataPath, fileName, SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
