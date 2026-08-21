$ErrorActionPreference = "Stop"

$projectPath = Join-Path $PSScriptRoot "IcarusProfileMod.csproj"
$outputPath = Join-Path $PSScriptRoot "artifacts\win-x64"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "The .NET 8 SDK is required. Install it from https://dotnet.microsoft.com/download/dotnet/8.0 and run this script again."
}

$sdkList = dotnet --list-sdks
if (-not ($sdkList -match "^8\.")) {
    throw "The .NET 8 SDK is required. Installed SDKs:`n$sdkList"
}

dotnet publish $projectPath `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $outputPath

$exePath = Join-Path $outputPath "IcarusProfileMod.exe"
Write-Host "Published $exePath"
