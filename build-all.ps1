# GoodRP — Build all platforms.
# Usage:
#   .\build-all.ps1              # Build all platforms
#   .\build-all.ps1 -WindowsOnly  # Windows only
#   .\build-all.ps1 -LinuxOnly    # Linux only
#   .\build-all.ps1 -MacOnly      # macOS only
param(
    [switch]$WindowsOnly,
    [switch]$LinuxOnly,
    [switch]$MacOnly
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
Set-Location $root

$doWindows = -not ($LinuxOnly -or $MacOnly)
$doLinux   = -not ($WindowsOnly -or $MacOnly)
$doMac     = -not ($WindowsOnly -or $LinuxOnly)

# --- 1. Read version ---
$csproj = "src\GoodRP.csproj"
$txt = Get-Content $csproj -Raw
if ($txt -match '<Version>([\d\.]+)</Version>') { $Version = $matches[1] } else { $Version = '0.0.0' }
Write-Host "=== GoodRP v$Version — Building all platforms ===" -ForegroundColor Cyan

# --- 2. Download DiscordRPC DLL (needed for Windows build) ---
if ($doWindows) {
    $url = "https://github.com/maximmax42/Discord-CustomRP/raw/refs/heads/master/CustomRPC/DiscordRPC.dll"
    $dest = Join-Path $root "Discord-CustomRP-master\CustomRPC\DiscordRPC.dll"
    $destDir = Split-Path $dest
    if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Force -Path $destDir | Out-Null }
    if (-not (Test-Path $dest)) {
        Write-Host "Downloading DiscordRPC.dll..."
        Invoke-WebRequest -Uri $url -OutFile $dest -ErrorAction Stop
    }
}

# --- 3. Windows ---
if ($doWindows) {
    Write-Host "`n=== Windows (win-x64) ===" -ForegroundColor Green
    & "$root/build.ps1"
}

# --- 4. Linux ---
if ($doLinux) {
    Write-Host "`n=== Linux (linux-x64) ===" -ForegroundColor Green
    & "$root/build-linux.ps1"
}

# --- 5. macOS ---
if ($doMac) {
    Write-Host "`n=== macOS (arm64 + x64) ===" -ForegroundColor Green
    & "$root/build-macos.ps1" -Arch arm64
    & "$root/build-macos.ps1" -Arch x64
}

Write-Host "`n=== All builds complete ===" -ForegroundColor Cyan
Write-Host "Output files in: publish/"
Get-ChildItem "publish/*" -File | Where-Object { $_.Extension -match '\.(exe|AppImage|dmg)$' } |
    ForEach-Object { Write-Host "  $($_.Name) ($([math]::Round($_.Length / 1MB, 1)) MB)" }
