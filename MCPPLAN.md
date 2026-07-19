# MCP Integration Plan: GoodRP ↔ AI Agents

## Goal

Expose GoodRP's media detection + Discord Rich Presence as an MCP server
so that AI agents (Nanobot, OpenClaw, Antigravity, Claude Code, Hermes) can:

1. **Query** what media is currently playing (title, artist, album, progress, estimated type, etc.)
2. **Control** how it's displayed on Discord (watching / listening)
3. **Optionally override** media details (title, artist, album, app_name, image)
4. **Manage** presence (show / hide)

GoodRP has two modes:
- **Legacy (GUI)**: Existing WinForms features — users control everything via the interface
- **MCP (headless)**: For AI agents — agents detect, control, and optionally enhance presentation

---

## Architecture

```
┌────────────────────────────────────────────────────────┐
│  GoodRP (C# .NET 9)                                    │
│                                                         │
│  ┌────────────┐  ┌──────────────┐  ┌────────────────┐  │
│  │ MediaWatcher│  │DiscordManager│  │ ConfigManager  │  │
│  └──────┬─────┘  └──────┬───────┘  └────────────────┘  │
│         │               │                               │
│  ┌──────┴───────────────┴───────────────────────────┐  │
│  │               MCP Server (stdio)                  │  │
│  │  ┌─────────────────────────────────────────────┐ │  │
│  │  │  MediaTools   │  PresenceTools  │ StatusTools│ │  │
│  │  └─────────────────────────────────────────────┘ │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │               HTTP API (Kestrel)                  │  │
│  │  GET /api/media  │  POST /api/presence  │  ...    │  │
│  └──────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────┘
         │  JSON-RPC 2.0 over stdin/stdout    │  HTTP REST
         ▼                                    ▼
┌────────────────────────────────────────────────────────┐
│  Nanobot / OpenClaw / Antigravity / Claude Code / Hermes│
│  ┌────────────────────────────────────────────────────┐│
│  │  Agent detects: get_current_media()                ││
│  │  Agent controls: set_presence(type, overrides?)    ││
│  │  Agent manages: clear_presence() / set_auto_show() ││
│  └────────────────────────────────────────────────────┘│
└────────────────────────────────────────────────────────┘
```

---

## MCP Tool Specifications

All tools use JSON-RPC 2.0 over **stdio transport**.

### 1. `get_current_media`

Returns full details of the currently detected media.

**Parameters**: none

**Response**:
```json
{
  "title": "Bohemian Rhapsody",
  "artist": "Queen",
  "album": "A Night at the Opera",
  "app_name": "Spotify",
  "state": "playing",
  "position_seconds": 142,
  "duration_seconds": 354,
  "position_formatted": "2:22",
  "duration_formatted": "5:54",
  "progress_percent": 40.1,
  "clean_title": "Bohemian Rhapsody",
  "estimated_type": "listening"
}
```

`estimated_type` is computed from app name: "video", "movie", "vlc", "mpv" → `"watching"`, otherwise → `"listening"`.

### 2. `set_presence` ⭐

Sets Discord presence. **The `type` parameter is REQUIRED** for agents —
GoodRP will NOT auto-detect when called via MCP.

**Parameters**:
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `type` | string | **yes** | `"watching"` or `"listening"` |
| `title` | string | no | Override media title |
| `artist` | string | no | Override artist name |
| `album` | string | no | Override album name |
| `app_name` | string | no | Override app name |
| `image_url` | string | no | Override large image URL |

**Behavior**:
- Forces `ActivityType.Watching` or `ActivityType.Listening` on Discord
- Activity type is persisted to `config.json` as `ActivityTypeOverride`
- Overrides are optional — if omitted, detected media is used as-is

**Response**:
```json
{
  "success": true,
  "activity_type": "watching",
  "showing": "Bohemian Rhapsody — Queen"
}
```

### 3. `clear_presence`

Hides current media from Discord.

**Parameters**: none

**Response**:
```json
{
  "success": true
}
```

### 4. `get_status`

Returns connection and presence state.

**Parameters**: none

**Response**:
```json
{
  "connected": true,
  "discord_user": "username#1234",
  "showing_presence": true,
  "activity_type_override": "watching",
  "media_detected": true,
  "media_state": "playing"
}
```

### 5. `set_auto_show` ⭐

Enable or disable auto-show when media starts playing (Phase 1.5).

**Parameters**:
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `enabled` | bool | **yes** | `true` to auto-show, `false` to disable |

**Behavior**:
- When enabled, GoodRP automatically shows media on Discord when it starts playing
- Uses the configured `activity_type_override` (or auto-detection if set to "Auto")
- Persists to `config.json`

**Response**:
```json
{
  "success": true,
  "auto_show": true
}
```

### 6. `get_config`

Returns current GoodRP configuration.

**Parameters**: none

**Response**:
```json
{
  "discord_client_id": "123456789",
  "image_providers": ["cloudinary", "discord", "postimage"],
  "cloudinary_cloud_name": "mycloud",
  "cloudinary_upload_preset": "goodrp_preset",
  "discord_webhook_url": "https://discord.com/api/webhooks/...",
  "enable_art_finder": true,
  "auto_show_on_discord": true,
  "show_album_art": true,
  "activity_type_override": "watching"
}
```

---

## Override Requirement for Agents

When GoodRP runs in MCP mode (spawned by any MCP client):

- `set_presence` **MUST** include the `type` parameter
- If `type` is omitted, the tool returns an error: `"type is required (watching or listening)"`
- The activity type is persisted so the GUI and subsequent MCP calls use the same type
- **Agents CAN override content** (title/artist/album/app_name) — these are optional parameters
- If overrides are omitted, the detected media is used as-is

---

## HTTP API (Phase 2) ✅

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/health` | Health check |
| `GET` | `/api/media` | Current media details |
| `POST` | `/api/presence` | Set presence (`{"type":"watching"}`) — type required |
| `DELETE` | `/api/presence` | Clear presence |
| `PUT` | `/api/presence/activity` | Set activity type override |
| `GET` | `/api/status` | Connection + presence state |
| `GET` | `/api/config` | Read config |
| `PUT` | `/api/config` | Update config |

---

## Nanobot Skill (Phase 3) ✅

```
skills/goodrp/
├── SKILL.md                      # Skill definition (multi-platform)
├── references/
│   ├── mcp-tools.md              # MCP tool docs
│   └── api-docs.md               # HTTP API docs
└── scripts/
    └── check_goodrp.py           # Verify GoodRP is running
```

**Installation**:
```bash
# Bundled — copy to workspace
cp -r skills/goodrp ~/.nanobot/workspace/skills/

# Standalone — pip install
pip install nanobot-skill-goodrp
```

**Platform configs**:

| Platform | Config Location |
|----------|----------------|
| Claude Code | `~/.claude.json` or CLI: `claude mcp add-json` |
| Nanobot | `~/.nanobot/config.json` |
| Antigravity | `~/.gemini/config/mcp_config.json` |
| Hermes | `~/.hermes/config.yaml` or CLI: `hermes mcp add` |
| OpenClaw | MCP servers config |

---

## Auto-Trigger (Phase 1.5)

When `AutoShowOnDiscord` is enabled in config, GoodRP in MCP mode
automatically shows media on Discord when it starts playing:

1. `MediaWatcher.MediaChanged` fires
2. GoodRP checks `ConfigManager.Config.AutoShowOnDiscord`
3. If enabled, calls `_discordManager.SetPresence(media)`
4. `DiscordManager.UpdatePresence()` reads `ActivityTypeOverride` from config
5. Uses the agent-configured type (watching/listening) or auto-detection

**Agent control**: Agents can toggle auto-show via `set_auto_show(enabled)` tool.
When the agent calls `set_presence`, the override persists and is used
by subsequent auto-triggers.

---

## Global Hotkeys (Phase 6) ✅

System-wide keyboard shortcuts for show/hide Discord presence.

**Default shortcuts**:
- **Show**: `Ctrl+Shift+G` (G for GoodRP)
- **Hide**: `Ctrl+Shift+H` (H for Hide)

**Implementation**:
- `NativeMethods.cs` — P/Invoke for `RegisterHotKey`/`UnregisterHotKey`
- `HotkeyManager.cs` — Hotkey registration, parsing, conflict detection
- `MainForm.cs` — `WndProc` override to handle `WM_HOTKEY` messages
- `ConfigManager.cs` — `ShowHotkey`, `HideHotkey`, `UseHotkeys`, `UseNotifications` properties

**User preference**:
- Hotkeys can be enabled/disabled in Settings
- Balloon notification can be enabled/disabled independently
- Both can be active simultaneously
- Hotkey combinations are configurable (stored in config.json)

---

## Run Modes

| Command | Mode | Description |
|---------|------|-------------|
| `GoodRP.exe` | GUI | Normal WinForms app |
| `GoodRP.exe --mcp` | MCP | Headless MCP server (stdio) |
| `GoodRP.exe --mcp --api` | MCP+API | Headless with both transports |

---

## File Structure

### Source Code
```
src/
├── GoodRP.csproj              # +ModelContextProtocol, +Microsoft.Extensions.Hosting, +Microsoft.AspNetCore.App
├── Program.cs                 # --mcp/--api flag handling, auto-trigger
├── MainForm.cs                # GUI with hotkeys, notification preference
├── MediaWatcher.cs            # Windows SMTC media detection
├── DiscordManager.cs          # Discord RPC client
├── ImageUploader.cs           # Thumbnail → Cloudinary/Discord/PostImage upload
├── ArtFinderService.cs        # Auto-fetch album art (Deezer, Spotify, Cover Art Archive)
├── ConfigManager.cs           # Settings persistence (+ hotkey config)
├── NativeMethods.cs           # P/Invoke for global hotkeys
├── HotkeyManager.cs           # Hotkey registration and handling
├── Mcp/
│   ├── McpServer.cs           # MCP server host (stdio transport)
│   └── Tools/
│       ├── MediaTools.cs      # get_current_media (+ estimated_type)
│       ├── PresenceTools.cs   # set_presence, clear_presence, set_auto_show, get_config
│       └── StatusTools.cs     # get_status
└── Api/
    └── ApiServer.cs           # HTTP API (Kestrel) with all endpoints
```

### Skills
```
skills/
└── goodrp/
    ├── SKILL.md               # Multi-platform skill definition
    └── references/
        ├── mcp-tools.md       # MCP tool documentation
        └── api-docs.md        # HTTP API documentation
```

---

## Phases

| Phase | What | Depends On | Status |
|-------|------|------------|--------|
| **1** | MCP server (stdio) + tools | None | ✅ Done |
| **1.5** | Auto-trigger when media plays (MCP mode) | Phase 1 | ✅ Done |
| **2** | HTTP API (Kestrel) | Phase 1 core | ✅ Done |
| **3** | Nanobot skill + packaging | Phase 1+2 | ✅ Done |
| **4** | Content override restoration + estimated type | Phase 3 | ✅ Done |
| **4.5** | Multi-platform skill (Nanobot, OpenClaw, Antigravity, Claude Code, Hermes) | Phase 4 | ✅ Done |
| **5** | Test examples + final docs | All above | Pending |
| **6** | Global hotkeys + notification preference | None | ✅ Done |
