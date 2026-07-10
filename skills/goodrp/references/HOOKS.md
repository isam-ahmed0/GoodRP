# GoodRP Scripting / Hooks — AI Agent Reference

GoodRP can run custom scripts on media events. This is useful for AI agents or automation that need to trigger side-effects (webhooks, status files, notifications) when media changes.

## Configuration

Edit `%AppData%\GoodRP\config.json`:

```json
{
  "OnMediaChangedScript": "C:\\scripts\\on_media.bat",
  "OnMediaStoppedScript": "C:\\scripts\\on_stop.ps1",
  "OnPlaybackStateChangedScript": "C:\\scripts\\on_state.py",
  "ScriptTimeoutMs": 10000
}
```

| Field | Triggered When |
|-------|----------------|
| `OnMediaChangedScript` | New track/video starts |
| `OnMediaStoppedScript` | Playback stops |
| `OnPlaybackStateChangedScript` | Play ↔ Pause transition |
| `ScriptTimeoutMs` | Kill script after N milliseconds (default 10000) |

## Environment Variables

| Variable | Example |
|----------|---------|
| `GOODRP_TITLE` | `Bohemian Rhapsody` |
| `GOODRP_ARTIST` | `Queen` |
| `GOODRP_ALBUM` | `A Night at the Opera` |
| `GOODRP_APP` | `Spotify` |
| `GOODRP_STATE` | `Playing` |
| `GOODRP_POSITION` | `00:02:22` |
| `GOODRP_DURATION` | `00:05:44` |
| `GOODRP_EVENT` | `stopped` (stop script only) |

## Behavior

- Scripts run fire-and-forget on a background thread (non-blocking).
- Killed automatically after `ScriptTimeoutMs` (process tree).
- Errors are swallowed silently.
- Metadata is passed via env vars, not CLI args.

## Example (PowerShell webhook)

```powershell
$body = @{ content = "Now playing: $env:GOODRP_TITLE — $env:GOODRP_ARTIST" } | ConvertTo-Json
Invoke-RestMethod -Uri "WEBHOOK_URL" -Method Post -Body $body -ContentType "application/json"
```

## `.grp` — GoodRP Script Format

GoodRP also supports a native `.grp` format that combines declarative directives with
inline code. It can be used instead of standalone scripts.

### Declarative (zero code)

```
@event mediaChanged
@webhook WEBHOOK_URL
@template Now playing: **{{title}}** by {{artist}} via {{app}}
```

GoodRP sends this to Discord natively — no external runtime needed.

### Embedded code

```
@event mediaChanged
@language ps1
$body = @{ content = "$env:GOODRP_TITLE - $env:GOODRP_ARTIST" } | ConvertTo-Json
Invoke-RestMethod -Uri "WEBHOOK_URL" -Method Post -Body $body
```

### Directives

| Directive | Purpose |
|-----------|---------|
| `@event` | `mediaChanged` \| `mediaStopped` \| `playbackStateChanged` |
| `@timeout` | Timeout in ms (default 10000) |
| `@language` | `ps1` \| `py` \| `bat` \| `js` (omit for declarative-only) |
| `@webhook` | Webhook URL to POST the templated message |
| `@template` | Message template with `{{variable}}` placeholders |
| `@embed` | `true` = rich Discord embed, `false` = plain text |
| `@log` | `true` = auto-log each event |
| `@logFile` | Log file path |

### What's needed

- Declarative-only: nothing extra (native).
- `@language bat` / `ps1`: built into Windows.
- `@language py` / `js`: Python / Node.js must be installed.

See `HOOKS.md` (project root) for full user-facing documentation with BAT/Python examples.
