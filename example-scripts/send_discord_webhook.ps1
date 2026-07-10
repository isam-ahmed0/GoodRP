# Posts a plain-text "Now playing" message to a Discord webhook.
# Replace the URL below with your own webhook (Discord -> Server Settings -> Integrations -> Webhooks).

$webhookUrl = "YOUR_WEBHOOK_URL_HERE"

$content = "Now playing: $($env:GOODRP_TITLE) - $($env:GOODRP_ARTIST) [$($env:GOODRP_APP)]"

$body = @{ content = $content } | ConvertTo-Json -Compress

try {
    Invoke-RestMethod -Uri $webhookUrl -Method Post -Body $body -ContentType "application/json"
} catch {
    # Fail silently — GoodRP will not be affected by script errors.
}
