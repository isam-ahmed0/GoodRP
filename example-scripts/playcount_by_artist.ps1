# Tracks how many times each artist has been played.
# Results are stored in a JSON file on your Desktop, sorted by play count.

$statsPath = Join-Path $env:USERPROFILE "Desktop\artist_counts.json"

$stats = @{}
if (Test-Path $statsPath) {
    try { $stats = Get-Content $statsPath -Raw | ConvertFrom-Json -AsHashtable } catch { }
}

$artist = $env:GOODRP_ARTIST
if (-not $artist) { $artist = "Unknown" }

if ($stats.ContainsKey($artist)) {
    $stats[$artist] = [int]$stats[$artist] + 1
} else {
    $stats[$artist] = 1
}

$stats | ConvertTo-Json | Out-File -FilePath $statsPath -Encoding utf8

# Show top 5 artists by play count
$top = $stats.GetEnumerator() | Sort-Object { $_.Value } -Descending | Select-Object -First 5
Write-Host "Top artists:"
$top | ForEach-Object { Write-Host "  $($_.Key): $($_.Value)" }
