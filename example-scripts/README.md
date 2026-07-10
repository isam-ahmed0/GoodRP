# GoodRP Example Scripts

A collection of example scripts you can wire up as GoodRP hooks. These demonstrate
what's possible with the scripting feature — logging, Discord webhooks, status files,
analytics, and notifications.

## How to use

1. Pick a script below and copy it somewhere on your PC.
2. Edit the script if needed (e.g., set your webhook URL).
3. Point GoodRP at it via `config.json` (`%AppData%\GoodRP\config.json`):

```json
{
  "OnMediaChangedScript": "C:\\example-scripts\\send_discord_embed.ps1",
  "OnMediaStoppedScript": "C:\\example-scripts\\on_stop.bat",
  "OnPlaybackStateChangedScript": "C:\\example-scripts\\toast_notification.ps1",
  "ScriptTimeoutMs": 10000
}
```

## Environment variables

Every script receives these (no command-line arguments):

| Variable | Example |
|----------|---------|
| `GOODRP_TITLE` | `Bohemian Rhapsody` |
| `GOODRP_ARTIST` | `Queen` |
| `GOODRP_ALBUM` | `A Night at the Opera` |
| `GOODRP_APP` | `Spotify` |
| `GOODRP_STATE` | `Playing` |
| `GOODRP_POSITION` | `00:02:22` |
| `GOODRP_DURATION` | `00:05:44` |
| `GOODRP_IMAGE_URL` | `https://telegra.ph/file/...` (album art URL, may be empty) |
| `GOODRP_EVENT` | `stopped` (only in the stop script) |

## Scripts in this folder

| Script | Language | What it does |
|--------|----------|--------------|
| `log_tracks.bat` | Batch | Appends each track to `track_history.log` on your Desktop |
| `log_tracks.ps1` | PowerShell | Same, with timestamp + album + app name |
| `send_discord_webhook.ps1` | PowerShell | Posts "Now playing" to a Discord webhook (plain text) |
| `send_discord_embed.ps1` | PowerShell | Posts a rich Discord embed with color, footer, album art |
| `write_status_file.py` | Python | Writes current track to `now_playing.txt` (for OBS/Rainmeter) |
| `write_rainmeter.ps1` | PowerShell | Writes a Rainmeter `.inc` file with track fields |
| `daily_playlist.ps1` | PowerShell | Appends each track to a `YYYY-MM-DD.txt` playlist file |
| `track_counter.ps1` | PowerShell | Keeps a running total of tracks played (persisted counter) |
| `playcount_by_artist.ps1` | PowerShell | Tracks per-artist play counts in a JSON file |
| `toast_notification.ps1` | PowerShell | Shows a Windows toast/balloon popup for the new track |
| `on_stop.bat` | Batch | Logs when playback stops |

## `.grp` — GoodRP Script Format

GoodRP also supports a native `.grp` format that combines declarative directives with
inline code. It's perfect when you want a webhook or log **without writing any code**,
or when you want the simplicity of config-style scripting.

### Sample files in this folder

| File | What it does |
|------|--------------|
| `sample_webhook.grp` | Declarative Discord webhook (plain text, no code) |
| `sample_embed.grp` | Declarative Discord webhook (rich embed with album art) |
| `sample_embedded.ps1.grp` | Embedded PowerShell code inside a `.grp` file |
| `sample_log.grp` | Declarative log-to-file |

### Syntax

```
# Lines starting with # are comments
@event mediaChanged          # mediaChanged | mediaStopped | playbackStateChanged
@timeout 10000               # ms before the script is killed (default 10000)
@language ps1                # optional: ps1 | py | bat | js (omit for declarative-only)
@webhook https://...          # webhook URL (declarative)
@template Now playing: {{title}} by {{artist}}  # message template
@embed true                  # false = plain text, true = rich embed
@log true                    # auto-log each event
@logFile C:\logs\tracks.log  # log file path

# If @language is set, everything below is the code body (real code):
$body = @{ content = "$env:GOODRP_TITLE" } | ConvertTo-Json
Invoke-RestMethod -Uri $env:WEBHOOK_URL -Method Post -Body $body
```

### Template variables

| Variable | Replaces with |
|----------|---------------|
| `{{title}}` | Track title |
| `{{artist}}` | Artist name |
| `{{album}}` | Album name |
| `{{app}}` | Player app |
| `{{state}}` | Playing / Paused / Stopped |
| `{{position}}` | Current position |
| `{{duration}}` | Total duration |
| `{{imageUrl}}` | Album art URL (may be empty) |

### Declarative-only (zero code)

```
@event mediaChanged
@webhook YOUR_WEBHOOK_URL
@template Now playing: **{{title}}** by {{artist}} via {{app}}
```

GoodRP sends this to Discord natively — no PowerShell or Python needed.

### Rich embed (`@embed true`)

```
@event mediaChanged
@webhook YOUR_WEBHOOK_URL
@embed true
@template by {{artist}}
from {{album}}
```

Produces a Discord embed with the track title, your template as description, a blue (music)
or red (video) color, the app name in the footer, and the album art as thumbnail (if available).

### Inline code

```
@event mediaChanged
@language ps1
# Real PowerShell code — full power when declarative isn't enough
```

Supported `@language` values: `ps1` (PowerShell, built-in), `bat` (Batch, built-in),
`py` (Python, must be installed), `js` (Node.js, must be installed).

### What's needed to run `.grp`

- **Declarative-only**: nothing extra — GoodRP handles it natively.
- **`@language bat` / `ps1`**: built into Windows — nothing extra.
- **`@language py`** / **`js`**: Python / Node.js must be installed.

## Notes

- Scripts run **fire-and-forget** on a background thread — they never block media detection.
- If a script exceeds `ScriptTimeoutMs` (default 10s), GoodRP kills it.
- Keep secrets (webhook URLs, API keys) inside the script file, not in the filename.
- PowerShell scripts may need execution policy: `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned`
