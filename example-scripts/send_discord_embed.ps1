# Posts a rich Discord embed for the current track, including album art (if available).
# Replace the URL below with your own Discord webhook.

$webhookUrl = "YOUR_WEBHOOK_URL_HERE"

$title    = $env:GOODRP_TITLE
$artist   = $env:GOODRP_ARTIST
$album    = $env:GOODRP_ALBUM
$app      = $env:GOODRP_APP
$imageUrl = $env:GOODRP_IMAGE_URL

# Pick a color based on app (just for fun): music = blue, video = red.
$color = if ($app -match "vlc|mpv|video|movie") { 15158332 } else { 5814783 }

$embed = @{
    title       = $title
    description = "by $artist`nfrom $album"
    color       = $color
    footer      = @{ text = "Playing via $app" }
}

if ($imageUrl) {
    $embed["thumbnail"] = @{ url = $imageUrl }
}

$payload = @{ embeds = @($embed) } | ConvertTo-Json -Depth 5 -Compress

try {
    Invoke-RestMethod -Uri $webhookUrl -Method Post -Body $payload -ContentType "application/json"
} catch {
    # Fail silently.
}
