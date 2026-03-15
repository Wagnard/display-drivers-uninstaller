[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionPath = Join-Path $repoRoot "display-driver-uninstaller\Display Driver Uninstaller.sln"

if (-not (Test-Path $solutionPath)) {
    throw "Solution not found at '$solutionPath'."
}

function Get-MSBuildPath {
    $preferredPath = "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"
    if (Test-Path $preferredPath) {
        return $preferredPath
    }

    $vsWherePath = "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vsWherePath) {
        $installPath = & $vsWherePath -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
        if ($LASTEXITCODE -eq 0 -and $installPath) {
            $candidate = Join-Path $installPath "MSBuild\Current\Bin\MSBuild.exe"
            if (Test-Path $candidate) {
                return $candidate
            }
        }
    }

    throw "Could not find Visual Studio MSBuild.exe. Install Visual Studio Build Tools or Visual Studio with MSBuild support."
}

$msbuildPath = Get-MSBuildPath

Write-Host "Using MSBuild: $msbuildPath"
Write-Host "Building: $solutionPath"
Write-Host "Configuration: $Configuration"

& $msbuildPath $solutionPath /t:Build /p:Configuration=$Configuration

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
