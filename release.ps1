# GoodRP one-command release: bump version -> publish -> build installer -> GitHub release.
# Usage:  .\release.ps1                 (auto-increments patch, e.g. 1.0.0 -> 1.0.1)
#         .\release.ps1 -Version 1.2.0  (explicit version)
param(
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
Set-Location $root

# --- 1. Resolve version (single source of truth = src/GoodRP.csproj <Version>) ---
$csproj = "src\GoodRP.csproj"
if (-not $Version) {
    $txt = Get-Content $csproj -Raw
    if ($txt -match '<Version>([\d\.]+)</Version>') {
        $parts = $matches[1].Split('.')
        while ($parts.Length -lt 3) { $parts += '0' }
        $parts[2] = [string]([int]$parts[2] + 1)
        $Version = $parts -join '.'
    } else {
        $Version = '1.0.1'
    }
}
Write-Host "Release version: $Version"

# --- 2. Bump csproj version ---
$txt = Get-Content $csproj -Raw
$txt = $txt -replace '<Version>[\d\.]+</Version>', "<Version>$Version</Version>"
Set-Content $csproj $txt
Write-Host "Updated $csproj to <Version>$Version</Version>"

# --- 3. Publish (self-contained, single-file, win-x64) ---
Write-Host "Publishing..."
dotnet publish src/GoodRP.csproj -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish/
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# --- 4. Generate banner images if missing ---
if (-not (Test-Path "installer\goodrp-small.bmp") -or -not (Test-Path "installer\goodrp-sidebar.bmp")) {
    Write-Host "Generating banner images..."
    & powershell -NoProfile -ExecutionPolicy Bypass -File installer\make-banner.ps1
}

# --- 5. Compile installer (auto-versioned from publish\GoodRP.exe) ---
$iscc = Get-Command iscc -ErrorAction SilentlyContinue
if (-not $iscc) {
    $candidate = "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    if (Test-Path $candidate) { $iscc = $candidate } else { throw "Inno Setup compiler (iscc) not found on PATH" }
}
Write-Host "Compiling installer..."
& $iscc "installer\setup2.iss"
if ($LASTEXITCODE -ne 0) { throw "iscc failed" }

# --- 6. Commit + tag + GitHub release ---
$setup = "installer-output\GoodRP-Setup.exe"
if (-not (Test-Path $setup)) { throw "Installer not found: $setup" }

Write-Host "Committing version bump..."
git add $csproj
git commit -m "Release v$Version" | Out-Null
git tag "v$Version"

$gh = Get-Command gh -ErrorAction SilentlyContinue
if ($gh) {
    Write-Host "Pushing and creating GitHub release..."
    git push origin HEAD --tags
    & gh release create "v$Version" $setup --title "GoodRP v$Version" --notes "GoodRP v$Version"
    Write-Host "Released v$Version -> $setup (asset: GoodRP-Setup.exe)"
} else {
    Write-Host "gh CLI not found. To finish manually:"
    Write-Host "  git push origin HEAD --tags"
    Write-Host "  gh release create v$Version $setup --title 'GoodRP v$Version'"
}
