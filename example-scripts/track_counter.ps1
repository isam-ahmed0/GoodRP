# Keeps a running total of how many tracks you've played.
# The counter is persisted in a JSON file on your Desktop.

$counterPath = Join-Path $env:USERPROFILE "Desktop\track_count.json"

$data = @{ Total = 0; LastTrack = ""; LastPlayed = "" }
if (Test-Path $counterPath) {
    try { $data = Get-Content $counterPath -Raw | ConvertFrom-Json -AsHashtable } catch { }
}

$data.Total = [int]$data.Total + 1
$data.LastTrack = "$($env:GOODRP_TITLE) - $($env:GOODRP_ARTIST)"
$data.LastPlayed = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

$data | ConvertTo-Json | Out-File -FilePath $counterPath -Encoding utf8

# Optional: print progress (visible if you run the script manually)
Write-Host "Total tracks played: $($data.Total)"
