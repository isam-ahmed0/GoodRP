# GoodRP

Lightweight Discord Rich Presence client for Windows. Auto-detects media playback (music/video) and shows it on Discord with title, artist, album, album art, and real-time progress bar.

## Features

- **Auto-detection** — Works with Spotify, VLC, MPV, Chrome, Firefox, foobar2000, and any app using Windows SMTC
- **Music & Video** — Shows "Listening to" for audio, "Watching" for video
- **Real-time progress** — Timestamps update live as you play/seek
- **Album art** — Two-phase approach: art finder (Deezer/iTunes keyword search) + art detector (SMTC thumbnail uploaded to Cloudinary/Discord CDN)
- **Dark GUI** — Discord-style dark theme with connection panel and settings
- **System tray** — Minimizes to tray, auto-hides when media stops/pauses
- **Auto-show** — Optionally starts Discord RP automatically when media plays
- **Global hotkeys** — System-wide shortcuts to show/hide Discord presence (default: Ctrl+Shift+G / Ctrl+Shift+H)
- **Notifications** — Balloon notification or hotkey preference (configurable)
- **MCP Server** — AI agents (Nanobot, OpenClaw, Antigravity, Claude Code, Hermes) can query and control Discord RP
- **HTTP API** — REST API for programmatic access to media detection and presence control
- **~10MB RAM** — Event-driven, no polling, single executable

## Quick Start

1. Create a Discord app at https://discord.com/developers/applications
2. Copy the Application ID
3. Open GoodRP, paste the ID, click Connect
4. Play music or video — GoodRP detects it automatically

Optional: Set up Cloudinary (free account at https://cloudinary.com) with an unsigned upload preset for album art hosting. Discord webhook URL can be added as fallback. Art finder searches Deezer/iTunes automatically — no setup needed.

## Run Modes

| Command | Mode | Description |
|---------|------|-------------|
| `GoodRP.exe` | GUI | Normal WinForms app with settings |
| `GoodRP.exe --mcp` | MCP | Headless MCP server for AI agents (stdio) |
| `GoodRP.exe --mcp --api` | MCP+API | Headless with both MCP and HTTP API |
| `GoodRP.exe --api` | API | HTTP API only |

## MCP Integration

GoodRP exposes an MCP server so AI agents can detect what's playing and control Discord presence.

### Supported Platforms

| Platform | Config Location |
|----------|----------------|
| Claude Code | `~/.claude.json` or CLI: `claude mcp add-json` |
| Nanobot | `~/.nanobot/config.json` |
| Antigravity | `~/.gemini/config/mcp_config.json` |
| Hermes | `~/.hermes/config.yaml` or CLI: `hermes mcp add` |
| OpenClaw | MCP servers config |

### Example: Claude Code

```bash
claude mcp add-json goodrp '{"command":"C:\\path\\to\\GoodRP.exe","args":["--mcp"]}'
```

### MCP Tools

| Tool | Description |
|------|-------------|
| `get_current_media` | Get full details of what's playing (title, artist, album, progress, estimated type) |
| `set_presence` | Show media on Discord (`type` required: "watching" or "listening") |
| `clear_presence` | Hide media from Discord |
| `set_auto_show` | Enable/disable auto-show when media plays |
| `get_status` | Check Discord connection and presence state |
| `get_config` | Read current GoodRP configuration |

## HTTP API

Start with `GoodRP.exe --mcp --api` or `GoodRP.exe --api`.

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/health` | Health check |
| `GET` | `/api/media` | Current media details |
| `POST` | `/api/presence` | Set presence (requires `type`) |
| `DELETE` | `/api/presence` | Clear presence |
| `PUT` | `/api/presence/activity` | Set activity type |
| `GET` | `/api/status` | Connection + presence state |
| `GET` | `/api/config` | Read config |
| `PUT` | `/api/config` | Update config |

## Supported Players

| Player | Support |
|--------|---------|
| Spotify | Built-in |
| Chrome/Edge (YouTube, SoundCloud) | Built-in |
| Firefox | Built-in |
| foobar2000 | Built-in |
| VLC | Built-in (VLC 4.0+ or `vlc-win10smtc` plugin) |
| MPV | Built-in (`MPVMediaControl` script) |
| Any SMTC-compatible app | Built-in |

## Install

### Option 1: Installer
Download `GoodRP-Setup.exe` and run it. Follow the wizard.

### Option 2: Manual
1. Download `GoodRP.exe` from releases
2. Place it anywhere on your PC
3. Run it

## Project Structure

```
GoodRP/
├── src/
│   ├── Program.cs              — Entry point, --mcp/--api flag handling
│   ├── MainForm.cs              — Dark-themed GUI + tray icon
│   ├── MediaWatcher.cs          — Windows SMTC media detection
│   ├── DiscordManager.cs        — Discord RPC client
│   ├── ImageUploader.cs         — Thumbnail → Cloudinary/Discord/PostImage upload
│   ├── ArtFinderService.cs     — Album art via Deezer/iTunes API search
│   ├── ConfigManager.cs         — Settings persistence
│   ├── NativeMethods.cs         — P/Invoke for global hotkeys
│   ├── HotkeyManager.cs         — Hotkey registration and handling
│   ├── Mcp/
│   │   ├── McpServer.cs         — MCP server host (stdio transport)
│   │   └── Tools/
│   │       ├── MediaTools.cs    — get_current_media
│   │       ├── PresenceTools.cs — set_presence, clear_presence, set_auto_show, get_config
│   │       └── StatusTools.cs   — get_status
│   └── Api/
│       └── ApiServer.cs         — HTTP API (Kestrel)
├── skills/
│   └── goodrp/
│       ├── SKILL.md             — MCP skill definition (multi-platform)
│       └── references/
│           ├── mcp-tools.md     — MCP tool documentation
│           └── api-docs.md      — HTTP API documentation
├── installer/
│   └── setup.iss                — Inno Setup installer script
└── app.manifest                 — Windows manifest
```

## Tech Stack

| Component | Technology |
|-----------|------------|
| Language | C# (.NET 9) |
| UI | Windows Forms (dark theme) |
| Discord RPC | DiscordRPC (forked, with Name property) |
| Media Detection | Windows SMTC API |
| Album Art (upload) | Cloudinary API / Discord CDN / PostImage API |
| Album Art (finder) | Deezer API + iTunes Search API |
| MCP Server | ModelContextProtocol v1.4.0 (stdio transport) |
| HTTP API | ASP.NET Core (Kestrel) |
| Settings | JSON in `%AppData%\GoodRP\config.json` |

## Requirements

- Windows 10 1903+ or Windows 11
- Discord desktop app

## License

MIT
