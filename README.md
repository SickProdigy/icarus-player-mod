# Icarus Profile Mod

A small Windows profile editor for ICARUS. It finds `Profile.json` and `Characters.json` under the normal Windows save location, lets you edit supported profile values, and creates timestamped backups before it writes changes.

Default profile location:

```text
%LOCALAPPDATA%\Icarus\Saved\PlayerData\<SteamId>\Profile.json
```

## Current Features

- Auto-discovers ICARUS profile files in `%LOCALAPPDATA%\Icarus\Saved\PlayerData`.
- Lets you browse to a specific `Profile.json` or `Characters.json` manually.
- Shows and edits known `MetaResources` currencies with friendly labels: Ren, Refund Tokens, Exotics, Red Exotics, Legendary Biomass, Legendary Licence, and Uranium Rod Currency.
- Allows adding a missing known currency or custom `MetaRow` by name.
- Loads `Characters.json` and lets you pick a character, filter talents, and edit/add talent `RowName` ranks.
- Ships with extracted talent catalog data in `data\`, then shows talent display names, trees, max ranks, clamps known ranks, unlocks all character talents, and maxes selected or all character talents.
- Creates timestamped backups next to the original file before saving.
- Uses only built-in .NET JSON and Windows Forms APIs.

## Talent Catalog

The app ships with these extracted ICARUS data tables in `data\` so talent rank limits work out of the box:

```text
data\D_Talents.json
data\D_TalentTrees.json
data\D_TalentRanks.json
```

When a catalog is loaded, known talents show display name, talent tree, and max rank. Applying or saving known talents clamps ranks to the loaded catalog max. `Apply Rank` and `Max Rank Selected` work on one or more selected rows. `Unlock All Talents` and `Max Rank All Talents` only target character/player talent trees; blueprint, workshop, prospect, great-hunt, and creature/pet talent trees are skipped for bulk actions.

If ICARUS updates and the bundled data becomes stale, replace those three files with freshly extracted versions. The app auto-loads bundled files on startup, and the `Catalog...` button can load a different three-file folder manually by selecting its `D_Talents.json` file.

## Project Layout

```text
IcarusProfileMod.csproj  C# Windows Forms project
Program.cs               App entry point
MainForm.cs              Main Windows UI
IcarusProfile.cs         Profile.json load/edit/save logic
IcarusCharacters.cs      Characters.json load/edit/save logic
TalentCatalog.cs         Optional ICARUS talent metadata loader
ProfileFinder.cs         ICARUS profile/characters discovery
publish.ps1              Release build script
assets/                  Source image assets
data/                    Bundled extracted ICARUS talent data tables
artifacts/               Generated publish output, ignored by Git
```

## Build Requirements

Install the .NET 8 SDK for Windows:

https://dotnet.microsoft.com/download/dotnet/8.0

The runtime alone is not enough; publishing the `.exe` requires the SDK.

## Run From Source

```powershell
dotnet run
```

## Build A Windows .exe

```powershell
.\publish.ps1
```

The generated app will be placed in:

```text
artifacts\win-x64\IcarusProfileMod.exe
```

## Manual Publish Command

```powershell
dotnet publish .\IcarusProfileMod.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\artifacts\win-x64
```

## Safety Notes

Close ICARUS before saving changes. Steam Cloud may overwrite local files if the game or Steam sync is active while editing.

Backups are created automatically, but keep an extra copy of `Profile.json` and `Characters.json` before experimenting with new edits.
