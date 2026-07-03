# GoodRP

Lightweight Discord Rich Presence client for Windows. Auto-detects media playback (music/video) and shows it on Discord with title, artist, album, album art, and real-time progress bar.

## Features

- **Auto-detection** — Works with Spotify, VLC, MPV, Chrome, Firefox, foobar2000, and any app using Windows SMTC
- **Music & Video** — Shows "Listening to" for audio, "Watching" for video
- **Real-time progress** — Timestamps update live as you play/seek
- **Album art** — Extracts thumbnails and uploads to imgur
- **Dark GUI** — Discord-style dark theme with connection panel and settings
- **System tray** — Minimizes to tray, auto-hides when media stops/pauses
- **Auto-show** — Optionally starts Discord RP automatically when media plays
- **~10MB RAM** — Event-driven, no polling, single executable

## Quick Start

1. Create a Discord app at https://discord.com/developers/applications
2. Copy the Application ID
3. Open GoodRP, paste the ID, click Connect
4. Play music or video — GoodRP detects it automatically

Optional: Register at https://api.imgur.com/oauth2/addclient for album art support.

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
│   ├── Program.cs          — Entry point
│   ├── MainForm.cs          — Dark-themed GUI + tray icon
│   ├── MediaWatcher.cs      — Windows SMTC media detection
│   ├── DiscordManager.cs    — Discord RPC client
│   ├── ImageUploader.cs     — Thumbnail → imgur upload
│   └── ConfigManager.cs     — Settings persistence
├── installer/
│   └── setup.iss            — Inno Setup installer script
├── publish/                 — Self-contained build output
└── app.manifest             — Windows manifest
```

## Tech Stack

| Component | Technology |
|-----------|------------|
| Language | C# (.NET 9) |
| UI | Windows Forms (dark theme) |
| Discord RPC | DiscordRPC (forked, with Name property) |
| Media Detection | Windows SMTC API |
| Album Art | imgur API via HttpClient |
| Settings | JSON in `%AppData%\GoodRP\config.json` |

## Requirements

- Windows 10 1903+ or Windows 11
- Discord desktop app

## License

MIT
