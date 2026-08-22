# Icarus Profile Mod

A small Windows save editor for ICARUS. It finds `Profile.json`, `Characters.json`, and `Mounts.json` under the normal Windows save location, lets you edit supported profile, character, blueprint, and station mount values, and creates timestamped backups before it writes changes.

Default profile location:

```text
%LOCALAPPDATA%\Icarus\Saved\PlayerData\<SteamId>\Profile.json
%LOCALAPPDATA%\Icarus\Saved\PlayerData\<SteamId>\Characters.json
%LOCALAPPDATA%\Icarus\Saved\PlayerData\<SteamId>\Mounts.json
```

## Current Features

- Auto-discovers ICARUS player data files in `%LOCALAPPDATA%\Icarus\Saved\PlayerData`.
- Lets you browse to a specific `Profile.json`, `Characters.json`, or `Mounts.json` manually.
- Shows and edits known `MetaResources` currencies with friendly labels: Ren, Refund Tokens, Exotics, Red Exotics, Legendary Biomass, Legendary Licence, and Uranium Rod Currency.
- Allows adding a missing known currency or custom `MetaRow` by name.
- Loads `Characters.json` and separates progression into Character Talents, Solo Talents, and Blueprints tabs.
- Filters regular character talents by the in-game category groups: All Talents, Survival, Adventure, Habitation, and Combat.
- Filters character talents by individual tree, including an All Trees option.
- Ships with extracted talent catalog data in `data\`, then shows display names, trees, max ranks, and clamps known ranks.
- Supports selected-rank editing, Reset Rank, Max Rank Selected, Max Rank All, and Reset All Ranks where applicable.
- Limits rank spinner controls to the selected talent max rank so the up arrow cannot exceed known valid ranks.
- Loads `Mounts.json`, lets you pick a station-stored mount, edit mount level, max level, and edit creature talent ranks.
- Supports Inject Mounts by cloning an existing station mount when available, or by using a bundled station-mount template for empty or newly-created `Mounts.json` files.
- Can inject supported station mount and companion creature types such as dog, cat, horse, moa, buffalo, tusker, terrenus, zebra, ubi, mammoth, farm animals, wolves, raptors, draven, slinker, and related variants.
- Creates timestamped backups next to the original file before saving.
- Uses built-in .NET JSON and Windows Forms APIs, plus a local UE4 property serializer for `RecorderBlob.BinaryData` in station mounts.

## Talent Catalog

The app ships with these extracted ICARUS data tables in `data\` so talent rank limits work out of the box:

```text
data\D_Talents.json
data\D_TalentTrees.json
data\D_TalentRanks.json
```

When a catalog is loaded, known talents show display name, talent tree, and max rank. Editing or saving known talents clamps ranks to the loaded catalog max.

The main progression tabs are:

```text
Character Talents  Regular player talents, grouped by category and tree
Solo Talents       Solo player talents
Blueprints         Blueprint unlock rows
Mounts             Station mount level and creature talent editing
```

Rank edits update the loaded in-memory data immediately. The file is not written until you click that tab's `Save` button. Selected `Reset Rank` and `Max Rank Selected` actions work on one or more selected rows. Bulk `Max Rank All` and `Reset All Ranks` actions are scoped to the active tab/view.

If ICARUS updates and the bundled data becomes stale, replace those three files with freshly extracted versions. The app auto-loads bundled files on startup, and the `Catalog...` button can load a different three-file folder manually by selecting its `D_Talents.json` file.

## Mounts

The Mounts tab edits station-stored animals saved in:

```text
%LOCALAPPDATA%\Icarus\Saved\PlayerData\<SteamId>\Mounts.json
```

Supported mount editing currently focuses on station mounts only. Animals deployed into an active prospect may be stored in prospect/world data and are out of scope for this editor.

The app decodes and rewrites the UE4 serialized `RecorderBlob.BinaryData` structure used by station mounts. This enables editing creature talent ranks instead of doing unsafe byte/string replacement.

If `Mounts.json` is missing for the selected player data folder, the app asks before preparing a new one. The file is created only when you click `Save`.

`Inject Mounts` adds a generic station mount by cloning an existing station mount entry when one is available. If the file is empty or newly-created, it falls back to the bundled `data\MountTemplate.json`, then patches the selected animal type, AI setup row, blueprint class, name, generated actor/object IDs, owner player ID, level, and editable arrays.

## Project Layout

```text
IcarusProfileMod.csproj  C# Windows Forms project
Program.cs               App entry point
MainForm.cs              Main Windows UI
IcarusProfile.cs         Profile.json load/edit/save logic
IcarusCharacters.cs      Characters.json load/edit/save logic
IcarusMounts.cs          Mounts.json load/edit/save and UE4 property handling
TalentCatalog.cs         Optional ICARUS talent metadata loader
InjectMountDialog.cs     Inject Mounts selection dialog
ProfileFinder.cs         ICARUS player data discovery
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

Release zips are named like:

```text
artifacts\IcarusProfileMod-v1.1.3-win-x64.zip
```

The zip should include:

```text
IcarusProfileMod.exe
IcarusProfileMod.pdb
data\D_TalentRanks.json
data\D_Talents.json
data\D_TalentTrees.json
data\MountTemplate.json
```

## Manual Publish Command

```powershell
dotnet publish .\IcarusProfileMod.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\artifacts\win-x64
```

## Safety Notes

Close ICARUS before saving changes. Steam Cloud may overwrite local files if the game or Steam sync is active while editing.

Backups are created automatically, but keep an extra copy of `Profile.json`, `Characters.json`, and `Mounts.json` before experimenting with new edits.

The Mounts tab is still newer than the profile and character editors. Test changes with expendable or backed-up saves first, especially injected mounts and creature talents.
