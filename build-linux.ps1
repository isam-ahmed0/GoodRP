# GoodRP Linux build script
# Usage: .\build-linux.ps1
param()

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
Set-Location $root

# Read version
$csproj = "src\GoodRP.csproj"
$txt = Get-Content $csproj -Raw
if ($txt -match '<Version>([\d\.]+)</Version>') { $Version = $matches[1] } else { $Version = '0.0.0' }
Write-Host "Build version: $Version"

# Publish (single-file, all native libs bundled inside)
Write-Host "Publishing for linux-x64..."
dotnet publish src/GoodRP.csproj -c Release -f net9.0 -r linux-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o publish/linux-x64
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

Write-Host ""
Write-Host "Done! Output: publish/linux-x64/GoodRP" -ForegroundColor Green
Write-Host "Share only the GoodRP file — .so files are bundled inside."
