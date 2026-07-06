---
name: goodrp
description: Detect and control Discord Rich Presence for media playback via GoodRP. Query what's playing, set activity type (watching/listening), optionally override media details, and manage presence display.
metadata:
  author: GoodRP
  version: 1.2.0
  category: media
  tools: [get_current_media, set_presence, clear_presence, set_auto_show, get_status, get_config]
---

# GoodRP — Discord Rich Presence for Media

Control what shows on your Discord profile when playing music or videos.

## When to Use This Skill

Use this skill when the user asks to:

- "Show my music on Discord"
- "What am I listening to?"
- "Hide my Discord status"
- "Show as watching instead of listening"
- "What's my Discord status?"
- "Auto-show my music on Discord"
- "Override the song title on Discord"
- Anything related to Discord Rich Presence for media playback

## How It Works

GoodRP detects media playback on Windows via SMTC (Spotify, VLC, Chrome, etc.)
and exposes it as an MCP server (stdio) or HTTP API.

### MCP Tools

| Tool | Description |
|------|-------------|
| `get_current_media` | Get full details of what's playing (title, artist, album, estimated type) |
| `set_presence` | Show media on Discord (**requires** `type`: "watching" or "listening"; optional overrides) |
| `clear_presence` | Hide media from Discord |
| `set_auto_show` | Enable/disable auto-show when media plays |
| `get_status` | Check Discord connection and presence state |
| `get_config` | Read current GoodRP configuration |

### Key Rules

1. When calling `set_presence`, the `type` parameter is **REQUIRED**.
   You must explicitly choose `"watching"` or `"listening"` — GoodRP will not auto-detect for agents.
2. Content overrides (`title`, `artist`, `album`, `app_name`) are **optional**.
   If omitted, the detected media is used as-is.
3. `estimated_type` in `get_current_media` is computed from the app name:
   apps containing "video", "movie", "vlc", or "mpv" → `"watching"`, otherwise → `"listening"`.

## Example Interactions

### Show music on Discord
```
User: "Show my music on Discord"
Agent:
  1. get_current_media() → verify media is playing
  2. set_presence(type="listening") → show on Discord
```

### What am I listening to?
```
User: "What am I listening to?"
Agent:
  1. get_current_media() → returns title, artist, album, progress, estimated_type
  2. Reports: "You're listening to Bohemian Rhapsody by Queen"
```

### Show as watching
```
User: "Show as watching instead"
Agent:
  1. set_presence(type="watching")
  2. Confirms: "Now showing as Watching"
```

### Override song title
```
User: "Show my music but call it 'Custom Title'"
Agent:
  1. set_presence(type="listening", title="Custom Title")
  2. Confirms: "Now showing as Listening to Custom Title"
```

### Hide Discord status
```
User: "Hide my Discord status"
Agent:
  1. clear_presence()
  2. Confirms: "Discord presence hidden"
```

### Auto-show music
```
User: "Auto-show my music when it plays"
Agent:
  1. set_auto_show(true)
  2. Confirms: "Auto-show enabled. Media will appear on Discord automatically."
```

### Check status
```
User: "What's my Discord status?"
Agent:
  1. get_status() → returns connection, presence, media state
  2. Reports connection and what's showing
```

## Setup Requirements

1. **GoodRP installed** — Download from https://github.com/AhmedNSAli/GoodRP/releases
2. **Discord Application ID** — Create at https://discord.com/developers/applications
3. **GoodRP running** — Either:
   - GUI mode with auto-show enabled, OR
   - Headless mode with `--mcp` flag (or `--mcp --api` for HTTP access)

## Platform Setup

GoodRP ships with a bundled MCP skill. Install it from: https://github.com/AhmedNSAli/GoodRP/tree/main/skills/goodrp

### Claude Code

```bash
claude mcp add-json goodrp '{"command":"C:\\path\\to\\GoodRP.exe","args":["--mcp"]}'
```

Or manually add to `~/.claude.json`:
```json
{
  "mcpServers": {
    "goodrp": {
      "command": "C:\\path\\to\\GoodRP.exe",
      "args": ["--mcp"]
    }
  }
}
```

### Nanobot

Add to `~/.nanobot/config.json`:
```json
{
  "mcpServers": {
    "goodrp": {
      "command": "C:\\path\\to\\GoodRP.exe",
      "args": ["--mcp"]
    }
  }
}
```

### Antigravity

Add to `~/.gemini/config/mcp_config.json`:
```json
{
  "mcpServers": {
    "goodrp": {
      "command": "C:\\path\\to\\GoodRP.exe",
      "args": ["--mcp"]
    }
  }
}
```

### Hermes

```bash
hermes mcp add goodrp --command "C:\\path\\to\\GoodRP.exe" --args "--mcp"
```

Or add to `~/.hermes/config.yaml`:
```yaml
mcp_servers:
  goodrp:
    command: "C:\\path\\to\\GoodRP.exe"
    args: ["--mcp"]
```

### OpenClaw

Add to OpenClaw's MCP servers config:
```json
{
  "mcpServers": {
    "goodrp": {
      "command": "C:\\path\\to\\GoodRP.exe",
      "args": ["--mcp"]
    }
  }
}
```

### HTTP API (Alternative)

For HTTP API access, start GoodRP with both flags:
```bash
GoodRP.exe --mcp --api
```

The API will be available at `http://127.0.0.1:9876`.
