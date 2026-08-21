# Icarus Profile Mod

A small Windows profile editor for ICARUS. It finds `Profile.json` under the normal Windows save location, lets you edit profile `MetaResources`, and creates a timestamped backup before it writes any changes.

Default profile location:

```text
%LOCALAPPDATA%\Icarus\Saved\PlayerData\<SteamId>\Profile.json
```

## Current Features

- Auto-discovers ICARUS profile files in `%LOCALAPPDATA%\Icarus\Saved\PlayerData`.
- Lets you browse to a specific `Profile.json` manually.
- Shows and edits known `MetaResources` currencies with friendly labels: Ren, Refund Tokens, Exotics, Red Exotics, Legendary Biomass, Legendary Licence, and Uranium Rod Currency.
- Allows adding a missing known currency or custom `MetaRow` by name.
- Creates `Profile.backup-yyyyMMdd-HHmmss.json` next to the original file before saving.
- Uses only built-in .NET JSON and Windows Forms APIs.

## Project Layout

```text
IcarusProfileMod.csproj  C# Windows Forms project
Program.cs               App entry point
MainForm.cs              Main Windows UI
IcarusProfile.cs         Profile.json load/edit/save logic
ProfileFinder.cs         ICARUS profile discovery
publish.ps1              Release build script
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

Backups are created automatically, but keep an extra copy of `Profile.json` before experimenting with new edits.

