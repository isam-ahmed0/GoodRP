# Writes a Rainmeter-compatible .inc file with track fields.
# Point a Rainmeter skin's [Variables] include at this file to display now-playing info.

$outDir = Join-Path $env:USERPROFILE "Documents\Rainmeter\Skins\GoodRP"
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

$outPath = Join-Path $outDir "NowPlaying.inc"

$lines = @(
    "[Variables]"
    "TrackName=$($env:GOODRP_TITLE)"
    "ArtistName=$($env:GOODRP_ARTIST)"
    "AlbumName=$($env:GOODRP_ALBUM)"
    "PlayerName=$($env:GOODRP_APP)"
    "State=$($env:GOODRP_STATE)"
    "Position=$($env:GOODRP_POSITION)"
    "Duration=$($env:GOODRP_DURATION)"
)

$lines -join "`n" | Out-File -FilePath $outPath -Encoding utf8
