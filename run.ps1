[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$Build
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$exePath = Join-Path $repoRoot "display-driver-uninstaller\Display Driver Uninstaller\bin\$Configuration\Display Driver Uninstaller.exe"

if ($Build -or -not (Test-Path $exePath)) {
    & (Join-Path $repoRoot "build.ps1") -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

if (-not (Test-Path $exePath)) {
    throw "Built executable not found at '$exePath'."
}

Write-Host "Launching: $exePath"
Start-Process -FilePath $exePath -WorkingDirectory (Split-Path -Parent $exePath)
