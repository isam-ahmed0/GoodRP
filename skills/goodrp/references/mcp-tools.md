# GoodRP MCP Tool Reference

GoodRP exposes an MCP server over **stdio transport**.
All tools return JSON strings.

---

## `get_current_media`

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

---

## `set_presence`

Show media on Discord. **`type` is REQUIRED**.

**Parameters**:
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `type` | string | **yes** | `"watching"` or `"listening"` |
| `title` | string | no | Override media title |
| `artist` | string | no | Override artist name |
| `album` | string | no | Override album name |
| `app_name` | string | no | Override app name |
| `image_url` | string | no | Override large image URL |

**Response**:
```json
{
  "success": true,
  "activity_type": "listening",
  "showing": "Bohemian Rhapsody — Queen"
}
```

**Errors**:
- `"type is required and must be 'watching' or 'listening'"` — missing/invalid type
- `"Not connected to Discord"` — Discord not connected
- `"No media detected"` — no media playing

---

## `clear_presence`

Hide current media from Discord.

**Parameters**: none

**Response**:
```json
{ "success": true }
```

---

## `set_auto_show`

Enable or disable auto-show when media starts playing.

**Parameters**:
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `enabled` | bool | **yes** | `true` to enable, `false` to disable |

**Response**:
```json
{ "success": true, "auto_show": true }
```

---

## `get_config`

Get current GoodRP configuration.

**Parameters**: none

**Response**:
```json
{
   "discord_client_id": "123456789",
  "image_providers": ["telegraph", "cloudinary", "postimage"],
  "cloudinary_cloud_name": "mycloud",
  "cloudinary_upload_preset": "goodrp_preset",
  "enable_art_finder": true,
  "auto_show_on_discord": true,
  "show_album_art": true,
  "activity_type_override": "watching"
}
```

---

## `get_status`

Get Discord connection and media detection status.

**Parameters**: none

**Response**:
```json
{
  "connected": true,
  "showing_presence": true,
  "activity_type_override": "watching",
  "media_detected": true,
  "media_state": "playing"
}
```
