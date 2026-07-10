# 9XT — Modern Configurable GUI for GoodRP

## Overview

A second GUI for GoodRP ("9XT") with sidebar navigation, a rounded flat-dark theme, and
enhanced configuration features. It runs **alongside** the existing `MainForm` (which is
never modified). Users opt in via `"GuiVersion": "9xt"` in `%AppData%\GoodRP\config.json`.

Both GUIs live in `src/`, compile into the **same `GoodRP.exe`**, and are chosen at runtime
by config. No reflection, no DLL, no new project.

## New Features (vs default GUI)

| Feature | Description |
|---------|-------------|
| Multiple Discord App IDs | Store named App IDs (e.g. "Spotify RP", "YouTube RP") and quick-switch from the Connections page |
| Filterable Activity Log | Real-time log of media changes, script runs, and Discord events. Filter by type (All / Media / Discord / Script / Error) |
| Scripts/Hooks Table | Each hook is a row: enable toggle + path textbox + Browse button (OnMediaChanged, OnMediaStopped, OnPlaybackStateChanged) |
| Image Provider Order | Drag-reorderable list of providers (telegraph, cloudinary, postimage) |
| App Filtering Lists | Add/remove apps from AllowedApps and IgnoredApps |
| All existing settings | Album art, auto-show, MCP, notifications, hotkeys, Cloudinary creds — consolidated in Settings |

## Files

### Create

- `src/ModernMainForm.cs` — the 9XT GUI (~900 lines), 100% code-generated (no designer)
- `src/LogService.cs` — in-memory ring-buffer activity logger (~80 lines)
- `9XT.md` — this document

### Modify

- `src/ConfigManager.cs` — add `GuiVersion`, `DiscordAppIds`, `ActiveAppIdIndex` to `AppConfig`
- `src/Program.cs` — route to `ModernMainForm` when `GuiVersion == "9xt"`; log startup

### Never modified

- `src/MainForm.cs`, `src/SettingsForm.cs`, all other files

## Config Model Changes (`ConfigManager.cs`)

```csharp
// GUI version: "default" | "9xt"
public string GuiVersion { get; set; } = "default";

// Multiple Discord App IDs for quick-switching
public List<DiscordAppEntry> DiscordAppIds { get; set; } = new();
public int ActiveAppIdIndex { get; set; } = 0;
```

```csharp
public class DiscordAppEntry
{
    public string Name { get; set; } = "";
    public string Id { get; set; } = "";
}
```

Backward compatible: existing configs default to `"default"` + empty list. `DiscordClientId`
stays for compat; 9XT writes to both `DiscordClientId` and `ActiveAppIdIndex` on switch.

## LogService

```csharp
public static class LogService
{
    public static event Action<LogEntry>? EntryAdded;
    public static void Log(string type, string message, bool success = true);
    public static List<LogEntry> GetEntries();
    public static List<LogEntry> Filter(string? type = null);
    public static void Clear();
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Type { get; set; }     // "Media" | "Discord" | "Script" | "Error"
    public string Message { get; set; }
    public bool Success { get; set; }
}
```

Event wiring done inside `ModernMainForm`:
- `MediaWatcher.MediaChanged` → `Log("Media", "Playing: ...")`
- `MediaWatcher.MediaStopped` → `Log("Media", "Playback stopped")`
- `MediaWatcher.PlaybackStateChanged` → `Log("Media", state)`
- `DiscordManager.StatusChanged` → `Log("Discord", msg)`
- `DiscordManager.PresenceUpdated` → `Log("Discord", "Presence: " + msg)`
- Script runs → `Log("Script", ...)` before/after `ScriptingService.RunScript`

## Program.cs

```csharp
LogService.Log("App", "GoodRP started");

Form mainForm;
if (string.Equals(ConfigManager.Config.GuiVersion, "9xt", StringComparison.OrdinalIgnoreCase))
    mainForm = new ModernMainForm(_mediaWatcher, _discordManager);
else
    mainForm = new MainForm(_mediaWatcher, _discordManager);
```

`FindWindow` uses shared title prefix `"GoodRP"` so single-instance works for either GUI.

## ModernMainForm Layout

Window 720×580, `FixedDialog`, no maximize, centered, `BackColor = #1a1a2e`.

```
┌──────────────────────────────────────────────────────────────┐
│ SIDEBAR (200px)        │  CONTENT PANEL (520px)             │
│  ┌─────────────┐       │  (switches by nav selection)        │
│  │  9XT        │       │                                      │
│  │  by GoodRP  │       │                                      │
│  └─────────────┘       │                                      │
│  [●] Home              │                                      │
│  [ ] Connections       │                                      │
│  [ ] Scripts           │                                      │
│  [ ] Logs              │                                      │
│  [ ] Settings          │                                      │
│  [ ] About             │                                      │
│  ───────────────       │                                      │
│  Status: ● Connected   │                                      │
│  Now: Bohemian...      │                                      │
└──────────────────────────────────────────────────────────────┘
```

### Pages

- **Home** — album art (120×120 rounded), title/artist/album labels, Show/Hide buttons
- **Connections** — ListView of stored App IDs (Name/ID/Active), Add/Remove/Connect/Set Active buttons, status label
- **Scripts** — 3-row hook table (toggle + path + Browse), timeout numeric, GRP note
- **Logs** — filter ComboBox + Clear button + ListView (Time/Type/Message/Status), auto-scroll, subscribes to `LogService.EntryAdded`
- **Settings** — sections: Discord (activity type, auto-show, MCP), Album Art (show, art finder), Notifications, Hotkeys (toggle + rebind labels), Image Upload (provider order ListBox + Cloudinary creds), App Filtering (Allowed/Ignored listboxes)
- **About** — version + credits

### Shared helpers (private)

- `MakeRounded(Control, radius)` — GraphicsPath region
- `CreateToggle(bool, Action<bool>)` — custom flat toggle switch
- `MakeButton(text, color, w, h)` — flat rounded button
- `MakeTextBox(placeholder, w)` — flat rounded textbox
- `MakeSectionHeader(text)`
- `MakeNavItem(text, y)` — sidebar nav label

### Behavior

- Same tray icon (NotifyIcon + "G" icon, Open/Quit context menu, double-click to show)
- Same `HotkeyManager` (Show/Hide global hotkeys, WndProc override)
- Minimize-to-tray on close (cancel UserClosing, hide form)
- `SaveSettings()` wired to every control change; `LoadSettings()` populates from `ConfigManager.Config`
- Media callbacks update Home + feed LogService
- Multiple App IDs: Connect disconnects current client, connects selected, writes `DiscordClientId` + `ActiveAppIdIndex`

## Build

```bash
cd src && dotnet build GoodRP.csproj -c Debug
```

Expect 0 errors. Existing users (no `GuiVersion` → `"default"`) see no change.

## How to switch GUI

The GUI is chosen once at startup by `Program.cs`, so switching requires a restart.

**Option A — In-app dropdown (easiest):**
- Classic GUI: Settings → "GUI version:" → choose **9XT (Modern)**.
- 9XT GUI: Settings → "GUI version:" → choose **Classic**.
- A popup confirms the choice was saved; **restart GoodRP** (close & reopen) to apply.

**Option B — config.json:**
Edit `%AppData%\GoodRP\config.json`:

```json
"GuiVersion": "9xt"
```

Values: `"default"` (classic `MainForm`) or `"9xt"` (modern `ModernMainForm`).
