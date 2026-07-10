# Appends each track to a date-named playlist file (e.g. 2026-07-10.txt)
# in a "Playlists" folder on your Desktop. Build a daily play history.

$playlistDir = Join-Path $env:USERPROFILE "Desktop\Playlists"
if (-not (Test-Path $playlistDir)) { New-Item -ItemType Directory -Path $playlistDir -Force | Out-Null }

$date = Get-Date -Format "yyyy-MM-dd"
$outPath = Join-Path $playlistDir "$date.txt"

$line = "$(Get-Date -Format 'HH:mm') - $($env:GOODRP_TITLE) - $($env:GOODRP_ARTIST) [$($env:GOODRP_APP)]"
$line | Out-File -Append -FilePath $outPath -Encoding utf8
