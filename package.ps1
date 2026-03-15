[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$Build
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Join-Path $repoRoot "display-driver-uninstaller"
$buildScript = Join-Path $repoRoot "build.ps1"
$nsisScript = Join-Path $projectRoot "NSIS+Uninstall.nsi"
$releaseDir = Join-Path $projectRoot "Display Driver Uninstaller\bin\$Configuration"
$artifactsDir = Join-Path $repoRoot "artifacts"
$stagingDir = Join-Path $artifactsDir "package\$Configuration"
$tempScript = Join-Path $stagingDir "NSIS+Uninstall.generated.nsi"

function Get-MakensisPath {
    $command = Get-Command "makensis.exe" -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $commonPaths = @(
        "C:\Program Files (x86)\NSIS\makensis.exe",
        "C:\Program Files\NSIS\makensis.exe"
    )

    foreach ($path in $commonPaths) {
        if (Test-Path $path) {
            return $path
        }
    }

    throw "Could not find makensis.exe. Install NSIS to create the setup package."
}

if ($Build -or -not (Test-Path (Join-Path $releaseDir "Display Driver Uninstaller.exe"))) {
    & $buildScript -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

if (-not (Test-Path $nsisScript)) {
    throw "NSIS script not found at '$nsisScript'."
}

New-Item -ItemType Directory -Force -Path $stagingDir | Out-Null

$itemsToCopy = @(
    @{ Source = Join-Path $releaseDir "Display Driver Uninstaller.exe"; Destination = "Display Driver Uninstaller.exe" },
    @{ Source = Join-Path $releaseDir "Display Driver Uninstaller.pdb"; Destination = "Display Driver Uninstaller.pdb" },
    @{ Source = Join-Path $projectRoot "Issues and solutions.txt"; Destination = "Issues and solutions.txt" },
    @{ Source = Join-Path $projectRoot "License.txt"; Destination = "License.txt" },
    @{ Source = Join-Path $projectRoot "Readme.txt"; Destination = "Readme.txt" },
    @{ Source = Join-Path $projectRoot "Display Driver Uninstaller\Resources\DDU.ico"; Destination = "Display Driver Uninstaller\Resources\DDU.ico" }
)

foreach ($item in $itemsToCopy) {
    if (-not (Test-Path $item.Source)) {
        throw "Required packaging input not found at '$($item.Source)'."
    }

    $destinationPath = Join-Path $stagingDir $item.Destination
    $destinationParent = Split-Path -Parent $destinationPath
    if ($destinationParent) {
        New-Item -ItemType Directory -Force -Path $destinationParent | Out-Null
    }

    Copy-Item -Path $item.Source -Destination $destinationPath -Force
}

$settingsSource = Join-Path $releaseDir "settings"
if (-not (Test-Path $settingsSource)) {
    throw "Settings directory not found at '$settingsSource'."
}

$settingsDestination = Join-Path $stagingDir "settings"
New-Item -ItemType Directory -Force -Path $settingsDestination | Out-Null
Copy-Item -Path (Join-Path $settingsSource "*") -Destination $settingsDestination -Recurse -Force

$sourcePathEscaped = $stagingDir.Replace("\", "\\")
$scriptContents = Get-Content $nsisScript -Raw
$scriptContents = $scriptContents -replace '!define SOURCE_PATH\s+".*"', ('!define SOURCE_PATH "{0}"' -f $sourcePathEscaped)
Set-Content -Path $tempScript -Value $scriptContents -Encoding ASCII

$makensisPath = Get-MakensisPath
Write-Host "Using makensis: $makensisPath"
Write-Host "Packaging from: $stagingDir"

& $makensisPath $tempScript
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$generatedInstaller = "${stagingDir}_setup.exe"
if (-not (Test-Path $generatedInstaller)) {
    throw "Expected installer was not created at '$generatedInstaller'."
}

$finalInstaller = Join-Path $artifactsDir "Display Driver Uninstaller $Configuration Setup.exe"
Move-Item -Path $generatedInstaller -Destination $finalInstaller -Force

Write-Host "Installer created: $finalInstaller"
