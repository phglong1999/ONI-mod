param(
    [string] $ManagedPath = "E:\SteamLibrary\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceDir = Join-Path $projectRoot "src"
$plibRoot = Join-Path $projectRoot "tools\ONIMods-src"
$outputDir = Join-Path $projectRoot "dist"
$assemblyName = "ONIUtilityTweaks.dll"
$sdk = & dotnet --list-sdks | Select-Object -First 1

if (-not $sdk) {
    throw "dotnet SDK was not found."
}

$sdkVersion = ($sdk -split " ")[0]
$sdkRoot = Join-Path "C:\Program Files\dotnet\sdk" $sdkVersion
$compiler = Join-Path $sdkRoot "Roslyn\bincore\csc.dll"

if (-not (Test-Path $compiler)) {
    throw "Could not find Roslyn compiler at $compiler."
}

$requiredReferences = @(
    "mscorlib.dll",
    "System.dll",
    "System.Core.dll",
    "0Harmony.dll",
    "Assembly-CSharp.dll",
    "Assembly-CSharp-firstpass.dll",
    "FMODUnity.dll",
    "UnityEngine.dll",
    "UnityEngine.CoreModule.dll",
    "UnityEngine.ImageConversionModule.dll",
    "UnityEngine.InputLegacyModule.dll",
    "UnityEngine.UI.dll",
    "UnityEngine.UIModule.dll",
    "UnityEngine.TextRenderingModule.dll",
    "UnityEngine.TextCoreFontEngineModule.dll",
    "Unity.TextMeshPro.dll",
    "Newtonsoft.Json.dll",
    "System.IO.Compression.dll",
    "System.Memory.dll",
    "System.Runtime.Serialization.dll",
    "netstandard.dll"
)

foreach ($reference in $requiredReferences) {
    $referencePath = Join-Path $ManagedPath $reference
    if (-not (Test-Path $referencePath)) {
        throw "Missing ONI reference: $referencePath"
    }
}

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
Get-ChildItem -Path $outputDir -Filter "*.dll" -File -ErrorAction SilentlyContinue | Remove-Item -Force

$sources = Get-ChildItem -Path $sourceDir -Filter "*.cs" -Recurse | ForEach-Object { $_.FullName }
if (-not $sources) {
    throw "No C# source files found in $sourceDir."
}

$plibSourceDirs = @(
    "PLibCore",
    "PLibUI",
    "PLibOptions"
)

foreach ($plibSourceDir in $plibSourceDirs) {
    $fullPath = Join-Path $plibRoot $plibSourceDir
    if (-not (Test-Path $fullPath)) {
        throw "Missing PLib source directory: $fullPath"
    }

    $sources += Get-ChildItem -Path $fullPath -Filter "*.cs" -Recurse |
        Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" } |
        ForEach-Object { $_.FullName }
}

$references = $requiredReferences | ForEach-Object { "/reference:" + (Join-Path $ManagedPath $_) }
$outputPath = Join-Path $outputDir $assemblyName

& dotnet $compiler `
    /target:library `
    /optimize+ `
    /nologo `
    /nostdlib+ `
    /define:MERGEDOWN `
    "/out:$outputPath" `
    $references `
    $sources

if ($LASTEXITCODE -ne 0) {
    throw "C# compiler failed with exit code $LASTEXITCODE."
}

Copy-Item -Path (Join-Path $projectRoot "mod.yaml") -Destination (Join-Path $outputDir "mod.yaml") -Force
Copy-Item -Path (Join-Path $projectRoot "mod_info.yaml") -Destination (Join-Path $outputDir "mod_info.yaml") -Force
Copy-Item -Path (Join-Path $projectRoot "THIRD_PARTY_NOTICES.md") -Destination (Join-Path $outputDir "THIRD_PARTY_NOTICES.md") -Force

$animationOutputDir = Join-Path $outputDir "anim"
if (Test-Path $animationOutputDir) {
    Remove-Item -LiteralPath $animationOutputDir -Recurse -Force
}

$animationSourceDir = Join-Path $projectRoot "assets\anim"
if (Test-Path $animationSourceDir) {
    Copy-Item -Path $animationSourceDir -Destination $outputDir -Recurse -Force
}

Write-Host "Built $outputPath"
Write-Host "Copy the dist folder contents to your local ONI mod folder."
