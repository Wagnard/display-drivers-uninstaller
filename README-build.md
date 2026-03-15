# Build Notes

This repository builds with Visual Studio MSBuild, not `dotnet build`.

## Prerequisites

- Visual Studio or Build Tools with MSBuild support
- .NET Framework 4.8 targeting pack
- NSIS installed if you want to create the installer

## Build

From the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Debug build:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Configuration Debug
```

Build output:

- `display-driver-uninstaller\Display Driver Uninstaller\bin\Release\Display Driver Uninstaller.exe`

## Run

Launch the built app:

```powershell
powershell -ExecutionPolicy Bypass -File .\run.ps1
```

Build first, then run:

```powershell
powershell -ExecutionPolicy Bypass -File .\run.ps1 -Build
```

## Package Installer

Install NSIS first if `makensis.exe` is not already available:

```powershell
winget install NSIS.NSIS
```

Create the installer:

```powershell
powershell -ExecutionPolicy Bypass -File .\package.ps1
```

Build first, then package:

```powershell
powershell -ExecutionPolicy Bypass -File .\package.ps1 -Build
```

Installer output:

- `artifacts\Display Driver Uninstaller Release Setup.exe`

## Notes

- Running `.\build.ps1` or `.\package.ps1` directly may be blocked by PowerShell execution policy.
- If that happens, use `powershell -ExecutionPolicy Bypass -File ...` as shown above.
