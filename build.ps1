# GoodRP local build script (for users / beginner devs).
# Reads the version from source (src/GoodRP.csproj), downloads the native
# DiscordRPC DLL, builds + publishes, compiles the installer, then launches
# the installer from installer-output. Does NOT modify the source version.
# Usage:  .\build.ps1
param()

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
Set-Location $root

# --- 1. Read version from source (no modification) ---
$csproj = "src\GoodRP.csproj"
$txt = Get-Content $csproj -Raw
if ($txt -match '<Version>([\d\.]+)</Version>') { $Version = $matches[1] } else { $Version = '0.0.0' }
Write-Host "Build version (from source): $Version"

# --- 2. Download DiscordRPC native DLL (first) ---
$url = "https://github.com/maximmax42/Discord-CustomRP/raw/refs/heads/master/CustomRPC/DiscordRPC.dll"
$dest = Join-Path $root "Discord-CustomRP-master\CustomRPC\DiscordRPC.dll"
$destDir = Split-Path $dest
if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Force -Path $destDir | Out-Null }
Write-Host "Downloading DiscordRPC.dll -> $dest"
Invoke-WebRequest -Uri $url -OutFile $dest -ErrorAction Stop

# --- 3. Build ---
Write-Host "Building (dotnet build -c Release)..."
dotnet build src/GoodRP.csproj -c Release
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }

# --- 4. Publish (self-contained, single-file, win-x64) ---
Write-Host "Publishing..."
dotnet publish src/GoodRP.csproj -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish/
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# --- 5. Generate banner images if missing ---
if (-not (Test-Path "installer\goodrp-small.bmp") -or -not (Test-Path "installer\goodrp-sidebar.bmp")) {
    Write-Host "Generating banner images..."
    & powershell -NoProfile -ExecutionPolicy Bypass -File installer\make-banner.ps1
}

# --- 6. Compile installer (auto-versioned from publish\GoodRP.exe) ---
$iscc = Get-Command iscc -ErrorAction SilentlyContinue
if (-not $iscc) {
    $candidate = "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    if (Test-Path $candidate) { $iscc = $candidate } else { throw "Inno Setup compiler (iscc) not found on PATH" }
}
Write-Host "Compiling installer..."
& $iscc "installer\setup2.iss"
if ($LASTEXITCODE -ne 0) { throw "iscc failed" }

# --- 7. Launch the compiled installer from the output folder ---
$setup = Join-Path $root "installer-output\GoodRP-Setup.exe"
Write-Host "Launching installer: $setup"
Start-Process -FilePath $setup

Write-Host "Build complete. Installer: $setup"
