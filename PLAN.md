# GoodRP — Discord Rich Presence Client

## What is GoodRP?

GoodRP is a lightweight, privacy-first Windows system tray application that **automatically detects** when you play music or video in any app (Spotify, VLC, MPV, Chrome, SoundCloud, etc.) and asks your permission to show it on Discord as Rich Presence — with **album art, title, artist, album, and time/progress bar**.

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
- **Privacy-first** — Tray notification asks "Show on Discord?" before updating your status
- **Album art** — Extracts thumbnail from media session, uploads to imgur, shows as Discord RP image
- **Full metadata** — Title, Artist, Album Name, Elapsed Time, Progress Bar
- **Smart display** — Uses "Listening to" for audio, "Watching" for video
- **Progress bar** — Elapsed time and duration shown as Discord progress bar
- **Per-app memory** — Option to remember "always show from [app]" choices
- **Lightweight** — ~10MB RAM, event-driven (no polling), single executable

## Discord Rich Presence Layout

```
┌──────────────────────────────────────┐
│  [Album Art]  Listening to Title     │  Details: Song Title
│               Artist Name            │  State: Artist · Album
│               Album Name             │
│  ████████████░░░░ 2:31 / 4:05        │  Timestamps: elapsed + duration
│  From: Spotify                       │  Small image tooltip: app name
└──────────────────────────────────────┘
```

## Technology Stack

| Component | Technology | Why |
|---|---|---|
| Language | C# (.NET 9) | Modern, fast startup, lightweight |
| Platform | Windows Forms (tray only) | Lightweight NotifyIcon, no heavy UI |
| Discord RPC | `DiscordRichPresence` NuGet | Same library as CustomRP, well-supported |
| Media Detection | `Windows.Media.Control` | Built-in Windows SMTC API, no polling |
| Album Art Upload | Raw `HttpClient` to imgur API | No extra NuGet dependency |
| Settings | JSON in `%AppData%\GoodRP\config.json` | Simple, portable |
| Target Framework | `net9.0-windows10.0.19041.0` | Windows 10 1903+ / Windows 11 |

## Privacy Notification Flow

```
1. Media starts playing in any app
   ↓
2. Windows SMTC event fires → GoodRP detects it
   ↓
3. Tray balloon notification:
   "Now Playing: Song Title — Click to show on Discord"
   ↓
4. User clicks balloon → RPC updates with full info
   User ignores → nothing shown on Discord
   ↓
5. Media pauses/stops → RPC clears after 3 seconds
```

## Project Structure

```
GoodRP/
├── GoodRP.csproj              — .NET 9 Windows app (WinExe)
├── Program.cs                  — Entry point, starts tray icon
├── TrayIcon.cs                 — NotifyIcon (tray), context menu, balloon tips
├── DiscordManager.cs           — DiscordRPC client (connect/set/clear/disconnect)
├── MediaWatcher.cs             — SMTC session monitoring + events
├── ImageUploader.cs            — Extracts thumbnail → uploads to imgur
├── ConfigManager.cs            — Settings (App ID, imgur ID, preferences)
├── SettingsForm.cs             — Settings dialog
└── app.manifest                — DPI awareness, OS compatibility
```

## Data Flow

```
┌─────────────────┐
│  Windows SMTC   │  GlobalSystemMediaTransportControlsSessionManager
│  (Any Player)   │  ← Spotify, VLC, Chrome, MPV, etc.
└────────┬────────┘
         │ Events: SessionChanged, MediaPropertiesChanged,
         │         PlaybackInfoChanged, TimelineChanged
         ↓
┌─────────────────┐
│  MediaWatcher   │  Extracts: Title, Artist, Album, Status, Position, Duration, Thumbnail
└────────┬────────┘
         │
         ↓
┌─────────────────┐
│  TrayIcon       │  Shows balloon notification: "Show on Discord?"
└────────┬────────┘
         │ Click = Yes
         ↓
┌─────────────────┐
│  ImageUploader  │  Thumbnail stream → imgur upload → returns URL
└────────┬────────┘
         │
         ↓
┌─────────────────┐
│  DiscordManager │  client.SetPresence({ Details, State, Assets, Timestamps })
└────────┬────────┘
         │
         ↓
┌─────────────────┐
│  Discord App    │  Shows Rich Presence on user's profile
└─────────────────┘
```

## API Details

### Windows SMTC (GlobalSystemMediaTransportControlsSessionManager)

```csharp
// Get session manager
var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();

// Get current session
var session = manager.GetCurrentSession();

// Events to subscribe:
session.CurrentSessionChanged  // New media started
session.MediaPropertiesChanged // Track info changed
session.PlaybackInfoChanged   // Play/Pause/Stop
session.TimelineChanged       // Position updated

// Data extraction:
var props = await session.TryGetMediaPropertiesAsync();
// props.Title, props.Artist, props.AlbumTitle, props.Thumbnail

var playback = session.GetPlaybackInfo();
// playback.PlaybackStatus (Playing, Paused, Stopped)

var timeline = session.GetTimelineProperties();
// timeline.Position, timeline.EndTime (duration)
```

### Discord RPC (DiscordRichPresence)

```csharp
var client = new DiscordRpcClient("YOUR_APP_ID");
client.Initialize();

client.SetPresence(new RichPresence()
{
    Details = "Song Title",                    // First line
    State = "Artist Name · Album Name",        // Second line
    Assets = new Assets()
    {
        LargeImageKey = "https://imgur.com/...", // Album art URL
        LargeImageText = "Song Title",
        SmallImageKey = "play_icon",
        SmallImageText = "Spotify"
    },
    Timestamps = Timestamps.Now(duration)      // Progress bar
});
```

### Imgur Anonymous Upload

```csharp
POST https://api.imgur.com/3/image
Headers: { Authorization: "Client-ID {YOUR_ID}" }
Body: base64-encoded image data

// Response contains: data.link (URL to use in Discord RPC)
```

## User Setup

1. **Create Discord App** at https://discord.com/developers/applications
   - Click "New Application" → Name it "GoodRP"
   - Copy the **Application ID** (Client ID)
   - (Optional) Go to Rich Presence → Art Assets → upload a play icon
2. **Create imgur App** at https://api.imgur.com/oauth2/addclient
   - Register as "Anonymous usage without user auth"
   - Copy the **Client ID**
3. **Configure GoodRP**:
   - Right-click tray icon → Settings
   - Paste Discord App ID
   - Paste imgur Client ID (optional, for album art)
   - Save

## Lightweight Design

| Metric | Value |
|---|---|
| RAM usage | ~5-10 MB |
| Disk size | < 20 MB (self-contained) |
| CPU usage | 0% idle (event-driven, no polling) |
| Dependencies | 1 NuGet (`DiscordRichPresence`) |
| Boot time | Instant (< 1 second) |
| Windows support | Windows 10 1903+ / Windows 11 |

## Windows Compatibility

- **Minimum**: Windows 10 version 1903 (build 18362)
- **Target**: Windows 10 1903+ / Windows 11 / Windows 11 24H2
- **Requires**: .NET 9 runtime (or self-contained build)
- **SMTC API**: Available since Windows 10 1803, but .NET 9 targets 1903+

## Implementation Order

1. **Project setup** — Create .csproj, Program.cs, basic tray icon
2. **MediaWatcher** — SMTC session detection + events
3. **DiscordManager** — Connect to Discord, set presence
4. **TrayIcon** — Balloon notifications, context menu
5. **ImageUploader** — Thumbnail extraction + imgur upload
6. **ConfigManager** — Settings storage
7. **SettingsForm** — Configuration UI
8. **Polish** — Error handling, edge cases, cleanup

## Notes

- Discord shows "Listening to" when `ActivityType` is set to Listening
- Discord shows "Watching" when `ActivityType` is set to Watching
- Progress bar only shows when both `Start` and `End` timestamps are set
- Album art must be a direct image URL (https://)
- imgur anonymous upload has rate limits (~1250/hour), sufficient for normal usage
- SMTC events are async — all processing must be thread-safe
- Multiple media sessions can be active simultaneously — GoodRP tracks the most recent one that is Playing
