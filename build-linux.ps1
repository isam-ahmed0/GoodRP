# GoodRP Linux build script — produces an AppImage.
# Usage: .\build-linux.ps1
#
# On Windows: publishes the Linux binary (cross-compile).
#             AppImage packaging requires a Linux machine.
# On Linux:   publishes + packages into AppImage.

param()

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
Set-Location $root

# --- 1. Read version ---
$csproj = "src\GoodRP.csproj"
$txt = Get-Content $csproj -Raw
if ($txt -match '<Version>([\d\.]+)</Version>') { $Version = $matches[1] } else { $Version = '0.0.0' }
Write-Host "Build version: $Version"

# --- 2. Publish (self-contained, single-file, linux-x64) ---
Write-Host "Publishing for linux-x64..."
dotnet publish src/GoodRP.csproj -c Release -f net9.0 -r linux-x64 --self-contained true `
    -p:PublishSingleFile=true -o publish/linux-x64
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

Write-Host "Binary published to: publish/linux-x64/GoodRP"

# --- 3. AppImage packaging (Linux only) ---
$isLinux = $PSVersionTable.PSEdition -eq "Core" -and -not $IsWindows
if (-not $isLinux) {
    Write-Host ""
    Write-Host "=== Cross-compile complete ===" -ForegroundColor Green
    Write-Host "Binary: publish/linux-x64/GoodRP"
    Write-Host ""
    Write-Host "To create the AppImage, run this on a Linux machine:" -ForegroundColor Yellow
    Write-Host "  chmod +x publish/linux-x64/GoodRP"
    Write-Host "  # Then use appimagetool or pkg2appimage"
    return
}

# --- Linux: full AppImage build ---
$appDir = "publish/AppDir"
if (Test-Path $appDir) { Remove-Item -Recurse -Force $appDir }
New-Item -ItemType Directory -Force -Path "$appDir/usr/bin" | Out-Null
New-Item -ItemType Directory -Force -Path "$appDir/usr/share/icons/hicolor/256x256/apps" | Out-Null

Copy-Item "publish/linux-x64/GoodRP" "$appDir/usr/bin/GoodRP"
Copy-Item "GoodRP.png" "$appDir/usr/share/icons/hicolor/256x256/apps/goodrp.png"

# AppRun
$apprun = @"
#!/bin/sh
DIR="`$(cd "`$(dirname "$0")" && pwd)"
exec "$DIR/usr/bin/GoodRP" "$@"
"@
Set-Content -Path "$appDir/AppRun" -Value $apprun -NoNewline
chmod +x "$appDir/AppRun"

# Desktop entry
Copy-Item "installer/goodrp.desktop" "$appDir/goodrp.desktop"

# Download appimagetool
$toolDir = "tools"
if (-not (Test-Path $toolDir)) { New-Item -ItemType Directory -Force -Path $toolDir | Out-Null }
$tool = "$toolDir/appimagetool"

if (-not (Test-Path $tool)) {
    Write-Host "Downloading appimagetool..."
    Invoke-WebRequest -Uri "https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage" -OutFile $tool
}
chmod +x $tool

# Build AppImage
$appImageName = "GoodRP-$Version-x86_64.AppImage"
Write-Host "Building $appImageName..."
$env:APPIMAGE_EXTRACT_AND_RUN = "1"
& $tool "$appDir" "publish/$appImageName"
if ($LASTEXITCODE -ne 0) { throw "appimagetool failed" }

Write-Host "Build complete. Output: publish/$appImageName"
