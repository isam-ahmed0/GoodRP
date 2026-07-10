# GoodRP HTTP API Reference

GoodRP exposes an HTTP API on `http://127.0.0.1:9876` (configurable via `--port`).

---

## `GET /api/health`

Health check.

**Response**:
```json
{ "success": true }
```

---

## WebSocket — Real-Time Events

Connect to `ws://127.0.0.1:9876/ws` to receive live media and Discord events.

**Events**:
| `type` | Payload |
|--------|---------|
| `media.changed` | `title`, `clean_title`, `artist`, `album`, `app_name`, `state`, `position_seconds`, `duration_seconds`, `estimated_type` |
| `media.stopped` | (none) |
| `playback.state` | `state` (`playing` / `paused` / `stopped`) |
| `discord.status` | `status`, `connected` |

**Example (Node.js)**:
```js
const ws = new WebSocket("ws://127.0.0.1:9876/ws");
ws.onmessage = (e) => {
  const data = JSON.parse(e.data);
  if (data.type === "media.changed") console.log("Now playing:", data.title);
};
```

**Example (Python)**:
```python
import websockets, asyncio, json
async def listen():
    async with websockets.connect("ws://127.0.0.1:9876/ws") as ws:
        async for msg in ws:
            print(json.loads(msg))
asyncio.run(listen())
```

---

## Scripting / Hooks

GoodRP can run custom scripts on media events. Configure in `%AppData%\GoodRP\config.json`:

```json
{
  "OnMediaChangedScript": "C:\\scripts\\on_media.bat",
  "OnMediaStoppedScript": "C:\\scripts\\on_stop.ps1",
  "OnPlaybackStateChangedScript": "C:\\scripts\\on_state.py",
  "ScriptTimeoutMs": 10000
}
```

Scripts receive metadata via env vars: `GOODRP_TITLE`, `GOODRP_ARTIST`, `GOODRP_ALBUM`, `GOODRP_APP`, `GOODRP_STATE`, `GOODRP_POSITION`, `GOODRP_DURATION`, `GOODRP_EVENT`.

See [HOOKS.md](HOOKS.md) for full details.


---

## `GET /api/media`

Get current media details.

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

## `POST /api/presence`

Show media on Discord. **`type` is REQUIRED**.

**Request Body**:
```json
{
  "type": "listening",
  "title": "Optional Title Override",
  "artist": "Optional Artist Override",
  "album": "Optional Album Override",
  "app_name": "Optional App Override",
  "image_url": "https://example.com/image.jpg"
}
```

All override fields are optional. If omitted, the detected media is used as-is.

**Response**:
```json
{
  "success": true,
  "activity_type": "listening",
  "showing": "Bohemian Rhapsody — Queen"
}
```

**Errors**:
- `400 Bad Request` — missing/invalid type, not connected, no media

---

## `DELETE /api/presence`

Hide current media from Discord.

**Response**:
```json
{ "success": true }
```

---

## `PUT /api/presence/activity`

Set activity type override.

**Request Body**:
```json
{ "type": "watching" }
```

**Response**:
```json
{ "success": true, "activity_type": "watching" }
```

---

## `GET /api/status`

Get connection and presence state.

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

---

## `GET /api/config`

Get current configuration.

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

## `PUT /api/config`

Update configuration.

**Request Body** (all fields optional):
```json
{
  "auto_show_on_discord": true,
  "show_album_art": false,
  "activity_type_override": "listening",
  "discord_client_id": "123456789",
  "image_providers": ["telegraph", "cloudinary", "postimage"],
  "cloudinary_cloud_name": "mycloud",
  "enable_art_finder": true
}
```

**Response**:
```json
{ "success": true }
```
