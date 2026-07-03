# GoodRP — Discord Rich Presence Client

## What is GoodRP?

GoodRP is a lightweight Windows application that **automatically detects** when you play music, videos, or movies in any app (Spotify, VLC, MPV, Chrome, SoundCloud, etc.) and shows it on Discord as Rich Presence — with **album art, title, artist, album, and real-time progress bar**.

## GoodRP vs CustomRP

GoodRP is its own application. It does **not** use or depend on CustomRP.

Both apps share the same underlying Discord RPC library (`DiscordRichPresence` by Lachee), but they are independent applications that each communicate directly with Discord via IPC pipe. GoodRP works without CustomRP installed.

```
GoodRP ──┐
         ├──► DiscordRichPresence library ──► Discord
CustomRP ─┘    (same NuGet package, different apps)
```

## Features

- **Auto-detection** — Works with Spotify, VLC, MPV, Chrome (YouTube, SoundCloud), Firefox, Windows Media Player, foobar2000, and any app that uses Windows SMTC
- **Music & Video** — Detects both audio and video playback, shows "Listening to" for music, "Watching" for videos/movies
- **Real-time timestamps** — Progress bar updates live when you skip forward/backward in the player
- **Album art** — Extracts thumbnail from media session, uploads to imgur, shows as Discord RP image
- **Full metadata** — Title, Artist, Album Name, Elapsed Time, Progress Bar
- **Dark GUI** — Discord-style dark theme with connection panel, now playing display, and settings
- **System tray** — Minimizes to tray, runs in background
- **Lightweight** — ~10MB RAM, event-driven (no polling), single executable

## Discord Rich Presence Layout

### Music
```
┌──────────────────────────────────────┐
│  [Album Art]  Listening to Title     │  Details: Song Title
│               Artist Name            │  State: Artist · Album
│               Album Name             │
│  ████████████░░░░ 2:31 / 4:05        │  Timestamps: elapsed + duration
│  From: Spotify                       │  Small image tooltip: app name
└──────────────────────────────────────┘
```

### Video/Movie
```
┌──────────────────────────────────────┐
│  [Thumbnail]   Watching Title        │  Details: Video Title
│               Channel Name           │  State: Channel · Duration
│               Duration               │
│  ████████████░░░░ 15:30 / 45:00      │  Timestamps: elapsed + duration
│  From: VLC / Chrome / MPV            │  Small image tooltip: app name
└──────────────────────────────────────┘
```

## Technology Stack

| Component | Technology | Why |
|---|---|---|
| Language | C# (.NET 9) | Modern, fast startup, lightweight |
| UI | Windows Forms (dark theme) | Full GUI + system tray |
| Discord RPC | `DiscordRichPresence` NuGet | Same library as CustomRP, well-supported |
| Media Detection | `Windows.Media.Control` | Built-in Windows SMTC API, no polling |
| Album Art Upload | Raw `HttpClient` to imgur API | No extra NuGet dependency |
| Settings | JSON in `%AppData%\GoodRP\config.json` | Simple, portable |
| Target Framework | `net9.0-windows10.0.19041.0` | Windows 10 1903+ / Windows 11 |

## How It Works

```
1. User opens GoodRP → enters Discord App ID → clicks Connect
   ↓
2. GoodRP monitors Windows SMTC for media sessions
   ↓
3. Media starts playing in any app (Spotify, Chrome, VLC, etc.)
   ↓
4. GoodRP detects: Title, Artist, Album, Position, Duration, Thumbnail
   ↓
5. User clicks "Show on Discord" (or auto-show if enabled)
   ↓
6. Album art uploaded to imgur (if Imgur ID configured)
   ↓
7. Discord RPC updated with full metadata + real-time progress bar
   ↓
8. When user skips/seeks, timeline updates → Discord RP refreshes instantly
```

## Project Structure

```
GoodRP/
├── PLAN.md                 — This file
├── app.manifest            — Windows manifest (DPI, OS compatibility)
└── src/
    ├── GoodRP.csproj       — .NET 9 Windows app (WinExe)
    ├── Program.cs          — Entry point
    ├── MainForm.cs         — Dark-themed GUI window
    ├── TrayIcon.cs         — System tray icon (legacy, now part of MainForm)
    ├── DiscordManager.cs   — DiscordRPC client (connect/set/clear/disconnect)
    ├── MediaWatcher.cs     — SMTC session monitoring + events
    ├── ImageUploader.cs    — Extracts thumbnail → uploads to imgur
    ├── ConfigManager.cs    — Settings (App ID, imgur ID, preferences)
    └── SettingsForm.cs     — Settings dialog (legacy, now part of MainForm)
```

## Real-Time Timestamp Updates

GoodRP listens to the `TimelineChanged` event from Windows SMTC. When you skip forward/backward in your player:

1. Player updates its position
2. Windows SMTC fires `TimelinePropertiesChanged` event
3. `MediaWatcher` updates `CurrentMedia.Position`
4. `MediaWatcher` fires `TimelineChanged` event
5. `MainForm` calls `DiscordManager.RefreshPresence()`
6. Discord RPC updates with new timestamp → progress bar moves

## Supported Players

### Works out of the box
| Player | Title | Artist | Album | Thumbnail | Time |
|---|---|---|---|---|---|
| **Spotify** | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Chrome/Edge** (YouTube, SoundCloud) | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Firefox** | ✅ | ✅ | ✅ | ✅ | ✅ |
| **foobar2000** (v1.5.1+) | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Groove Music / Media Player** | ✅ | ✅ | ✅ | ✅ | ✅ |
| **SoundCloud (UWP app)** | ✅ | ✅ | ✅ | ✅ | ✅ |

### Works with a plugin
| Player | Plugin needed |
|---|---|
| **VLC (Desktop)** | `vlc-win10smtc` plugin or VLC 4.0+ |
| **MPV** | `MPVMediaControl` or `MPV-SMTC` script |
| **MusicBee** | `mb_MediaControl` plugin |
| **AIMP** | `MediaControl` plugin |
| **iTunes** | `iTunes-SMTC` plugin |

## User Setup

1. **Create Discord App** at https://discord.com/developers/applications
   - Click "New Application" → Name it anything
   - Copy the **Application ID** (Client ID)
2. **Configure GoodRP**:
   - Open GoodRP
   - Paste Application ID → Click Connect
   - Status should show "Connected as [username]"
3. **(Optional) Album Art**:
   - Register at https://api.imgur.com/oauth2/addclient
   - Paste Imgur Client ID in GoodRP settings

## Build & Run

```bash
# Debug
dotnet run --project src/GoodRP.csproj

# Release
dotnet build src/GoodRP.csproj -c Release

# Publish self-contained
dotnet publish src/GoodRP.csproj -c Release --self-contained -r win-x64
```

## Lightweight Design

| Metric | Value |
|---|---|
| RAM usage | ~5-10 MB |
| Disk size | < 20 MB (self-contained) |
| CPU usage | 0% idle (event-driven, no polling) |
| Dependencies | 1 NuGet (`DiscordRichPresence`) |
| Boot time | Instant (< 1 second) |
| Windows support | Windows 10 1903+ / Windows 11 |

## Implementation Status

- [x] Project setup
- [x] MediaWatcher (SMTC detection)
- [x] DiscordManager (RPC client)
- [x] MainForm (dark GUI)
- [x] ImageUploader (thumbnail → imgur)
- [x] ConfigManager (settings)
- [x] Real-time timestamp updates
- [ ] VLC/MPV native support (planned)
- [ ] Auto-reconnect on disconnect
- [ ] Per-app allow/ignore lists
