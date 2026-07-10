# GoodRP Scripting / Hooks

GoodRP can run custom scripts when media events occur on your PC. This lets you integrate GoodRP with other tools, send webhooks, write status files, trigger notifications, or anything else a script can do.

## How It Works

GoodRP fires events when media changes. You configure a script path for each event in `config.json` (located at `%AppData%\GoodRP\config.json`). When the event fires, GoodRP runs your script on a background thread (fire-and-forget — it does not block media detection).

Metadata about the current media is passed to your script via **environment variables** (not command-line arguments, to avoid quoting/escaping issues).

## Configuration

Edit `%AppData%\GoodRP\config.json` and add the script paths you want:

```json
{
  "OnMediaChangedScript": "C:\\scripts\\on_media.bat",
  "OnMediaStoppedScript": "C:\\scripts\\on_stop.ps1",
  "OnPlaybackStateChangedScript": "C:\\scripts\\on_state.py",
  "ScriptTimeoutMs": 10000
}
```

| Config Field | Triggered When | Example Use |
|--------------|----------------|-------------|
| `OnMediaChangedScript` | New track/video starts playing | Log track, send webhook, update status file |
| `OnMediaStoppedScript` | Playback stops | Clear status, send "stopped" notification |
| `OnPlaybackStateChangedScript` | Play → Pause or Pause → Play | React to pause/resume |
| `ScriptTimeoutMs` | (global) | Max time a script may run before being killed (default: 10000 = 10s) |

Leave a field empty or omit it to disable that hook.

> **Note:** `OnPlaybackStateChangedScript` is rate-limited — it only fires on actual play/pause transitions, not on every seek.

## Environment Variables

Your script receives the following environment variables:

| Variable | Description | Example |
|----------|-------------|---------|
| `GOODRP_TITLE` | Cleaned track/video title | `Bohemian Rhapsody` |
| `GOODRP_ARTIST` | Artist name | `Queen` |
| `GOODRP_ALBUM` | Album name | `A Night at the Opera` |
| `GOODRP_APP` | Player application | `Spotify` |
| `GOODRP_STATE` | Playback state | `Playing` / `Paused` / `Stopped` |
| `GOODRP_POSITION` | Current position | `00:02:22` |
| `GOODRP_DURATION` | Total duration | `00:05:44` |
| `GOODRP_EVENT` | Event name (stop script only) | `stopped` |

## Example Scripts

### Batch (.bat)

`C:\scripts\on_media.bat`:
```bat
@echo off
echo New track: %GOODRP_TITLE% by %GOODRP_ARTIST% >> C:\scripts\now_playing.log
echo Album: %GOODRP_ALBUM% >> C:\scripts\now_playing.log
```

### PowerShell (.ps1)

`C:\scripts\on_media.ps1` — send a Discord webhook:
```powershell
$title = $env:GOODRP_TITLE
$artist = $env:GOODRP_ARTIST
$body = @{ content = "Now playing: $title — $artist" } | ConvertTo-Json
Invoke-RestMethod -Uri "YOUR_WEBHOOK_URL" -Method Post -Body $body -ContentType "application/json"
```

### Python (.py)

`C:\scripts\on_media.py` — write a status file for other tools:
```python
import os
with open("C:/scripts/now_playing.txt", "w", encoding="utf-8") as f:
    f.write(f"{os.environ.get('GOODRP_TITLE', '')} — {os.environ.get('GOODRP_ARTIST', '')}")
```

## Behavior & Safety

- **Fire-and-forget:** Scripts run on a background thread. GoodRP does not wait for them to finish (unless they exceed the timeout).
- **Timeout:** If a script runs longer than `ScriptTimeoutMs`, GoodRP kills the process (and its child processes). Default is 10 seconds.
- **Non-blocking:** A slow or crashed script will not affect media detection or Discord presence.
- **Errors are ignored:** If a script fails to start or throws an error, GoodRP logs nothing visible and continues normally.
- **Secrets:** Keep webhook URLs and API keys out of the script filename. Store them inside the script file or a separate config the script reads.

## Use Cases

- **Discord webhook:** Post "Now playing" messages to a channel when music starts.
- **Status file:** Write the current track to a file that OBS, Streamlabs, or a rainmeter skin can read and display.
- **External integrations:** Trigger IFTTT, Zapier, or home-automation webhooks based on what you're listening to.
- **Custom notifications:** Show a toast, play a sound, or run a macro when a new track begins.
- **Logging:** Keep a history of everything you've played.

## `.grp` — GoodRP Script Format

GoodRP supports a native `.grp` format that combines declarative directives with inline
code. Use it when you want a webhook or log **without writing any code**, or for
config-style scripting that embeds other languages.

### Example (declarative, zero code)

```
@event mediaChanged
@webhook YOUR_WEBHOOK_URL
@template Now playing: **{{title}}** by {{artist}} via {{app}}
```

GoodRP sends this to Discord natively — no PowerShell or Python required.

### Example (embedded PowerShell)

```
@event mediaChanged
@language ps1
$body = @{ content = "$env:GOODRP_TITLE - $env:GOODRP_ARTIST" } | ConvertTo-Json
Invoke-RestMethod -Uri $env:WEBHOOK_URL -Method Post -Body $body
```

### Directives

| Directive | Purpose |
|-----------|---------|
| `@event` | Which event triggers the script (`mediaChanged`, `mediaStopped`, `playbackStateChanged`) |
| `@timeout` | Timeout in ms before the script is killed (default 10000) |
| `@language` | Inline code language: `ps1`, `py`, `bat`, `js` (omit for declarative-only) |
| `@webhook` | Webhook URL to POST the templated message to |
| `@template` | Message template with `{{variable}}` placeholders |
| `@embed` | `true` = rich Discord embed, `false` = plain text |
| `@log` | `true` = auto-log each event |
| `@logFile` | Path for the log file |

### Template variables

`{{title}}`, `{{artist}}`, `{{album}}`, `{{app}}`, `{{state}}`, `{{position}}`,
`{{duration}}`, `{{imageUrl}}`.

### What's needed to run `.grp`

- **Declarative-only** (no `@language`): nothing extra — GoodRP handles it natively.
- **`@language bat` / `ps1`**: built into Windows — nothing extra.
- **`@language py`** / **`js`**: Python / Node.js must be installed.

See `example-scripts/README.md` for full syntax and sample `.grp` files.

## Troubleshooting

- **Script not running?** Check the path in `config.json` is correct and the file exists. Use absolute paths.
- **Variables empty?** Ensure your script reads environment variables (not arguments). In Batch use `%VAR%`, in PowerShell `$env:VAR`, in Python `os.environ['VAR']`.
- **Script hangs?** It will be killed after `ScriptTimeoutMs`. Increase the timeout if your script legitimately needs more time.
