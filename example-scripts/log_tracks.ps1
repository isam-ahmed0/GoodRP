# Log each track with a timestamp, album, and app name.
# GoodRP passes metadata via environment variables ($env:VAR in PowerShell).

$logPath = Join-Path $env:USERPROFILE "Desktop\track_history.log"
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

$line = "$timestamp | $($env:GOODRP_TITLE) - $($env:GOODRP_ARTIST) | $($env:GOODRP_ALBUM) | $($env:GOODRP_APP) | $($env:GOODRP_STATE)"

$line | Out-File -Append -FilePath $logPath -Encoding utf8
