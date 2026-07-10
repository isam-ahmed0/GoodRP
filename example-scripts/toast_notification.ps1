# Shows a Windows toast/balloon notification for the new track.
# Uses the .NET NotifyIcon balloon (no external modules required).

Add-Type -AssemblyName System.Windows.Forms

$title = $env:GOODRP_TITLE
$artist = $env:GOODRP_ARTIST
$app = $env:GOODRP_APP

$text = if ($artist) { "$title`nby $artist" } else { $title }
if ($app) { $text += "`nvia $app" }

$notify = New-Object System.Windows.Forms.NotifyIcon
$notify.Icon = [System.Drawing.SystemIcons]::Information
$notify.Visible = $true
$notify.BalloonTipTitle = "Now Playing"
$notify.BalloonTipText = $text
$notify.ShowBalloonTip(5000)

# Keep the process alive briefly so the balloon can render
Start-Sleep -Seconds 1
$notify.Dispose()
