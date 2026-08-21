using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IcarusProfileMod;

internal static class ProfileFinder
{
    public static IReadOnlyList<string> FindProfiles()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string playerDataPath = Path.Combine(localAppData, "Icarus", "Saved", "PlayerData");

        if (!Directory.Exists(playerDataPath))
        {
            return Array.Empty<string>();
        }

        return Directory
            .EnumerateFiles(playerDataPath, "Profile.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
