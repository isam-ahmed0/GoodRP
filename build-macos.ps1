# GoodRP macOS build script — produces a .dmg installer.
# Usage: .\build-macos.ps1 [-Arch arm64|x64]
#
# On Windows: publishes the macOS binary (cross-compile).
#             DMG packaging requires a macOS machine.
# On macOS:   publishes + packages into .dmg.

param(
    [ValidateSet("arm64", "x64")]
    [string]$Arch = "arm64"
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
Set-Location $root

# --- 1. Read version ---
$csproj = "src\GoodRP.csproj"
$txt = Get-Content $csproj -Raw
if ($txt -match '<Version>([\d\.]+)</Version>') { $Version = $matches[1] } else { $Version = '0.0.0' }
Write-Host "Build version: $Version (arch: $Arch)"

# --- 2. Set runtime identifier ---
$rid = if ($Arch -eq "arm64") { "osx-arm64" } else { "osx-x64" }

# --- 3. Publish (self-contained, single-file) ---
Write-Host "Publishing for $rid..."
dotnet publish src/GoodRP.csproj -c Release -f net9.0 -r $rid --self-contained true `
    -p:PublishSingleFile=true -o "publish/macos-$Arch"
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

Write-Host "Binary published to: publish/macos-$Arch/GoodRP"

# --- 4. DMG packaging (macOS only) ---
$canRunMac = (Get-Command chmod -ErrorAction SilentlyContinue) -and (Get-Command hdiutil -ErrorAction SilentlyContinue)
if (-not $canRunMac) {
    Write-Host ""
    Write-Host "=== Cross-compile complete ===" -ForegroundColor Green
    Write-Host "Binary: publish/macos-$Arch/GoodRP"
    Write-Host ""
    Write-Host "To create the .dmg, run this on a macOS machine:" -ForegroundColor Yellow
    Write-Host "  chmod +x publish/macos-$Arch/GoodRP"
    Write-Host "  # Then use create-dmg or hdiutil"
    return
}

# --- macOS: full .app bundle + DMG ---
$appName = "GoodRP"
$appBundle = "publish/$appName.app"
if (Test-Path $appBundle) { Remove-Item -Recurse -Force $appBundle }

New-Item -ItemType Directory -Force -Path "$appBundle/Contents/MacOS" | Out-Null
New-Item -ItemType Directory -Force -Path "$appBundle/Contents/Resources" | Out-Null

Copy-Item "publish/macos-$Arch/$appName" "$appBundle/Contents/MacOS/$appName"
chmod +x "$appBundle/Contents/MacOS/$appName"

# Info.plist with correct version
$plist = Get-Content "installer/Info.plist" -Raw
$plist = $plist.Replace("1.0.1", $Version)
Set-Content -Path "$appBundle/Contents/Info.plist" -Value $plist

# Icon
if (Test-Path "GoodRP.icns") {
    Copy-Item "GoodRP.icns" "$appBundle/Contents/Resources/$appName.icns"
}

# Create DMG
$dmgName = "GoodRP-$Version-$Arch.dmg"
Write-Host "Building $dmgName..."

$createDmg = Get-Command create-dmg -ErrorAction SilentlyContinue
if ($createDmg) {
    & create-dmg `
        --volname "$appName $Version" `
        --window-pos 200 120 `
        --window-size 600 400 `
        --icon-size 100 `
        --icon "$appName.app" 150 190 `
        --hide-extension "$appName.app" `
        --app-drop-link 450 190 `
        "publish/$dmgName" `
        "$appBundle"
    if ($LASTEXITCODE -ne 0) { throw "create-dmg failed" }
} else {
    $tempDmg = "publish/temp.dmg"
    & hdiutil create -volname $appName -srcfolder $appBundle -ov -format UDZO $tempDmg
    & hdiutil convert $tempDmg -format UDZO -o "publish/$dmgName"
    Remove-Item $tempDmg -ErrorAction SilentlyContinue
}

Write-Host "Build complete. Output: publish/$dmgName"
