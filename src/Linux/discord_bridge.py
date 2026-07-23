#!/usr/bin/env python3
"""Discord Rich Presence IPC bridge for GoodRP on Linux.

Pure stdlib — no pip install needed.
Runs an HTTP server on 127.0.0.1:19876 that GoodRP calls to manage Discord RPC.
"""

import json
import os
import socket
import struct
import sys
import threading
import time
import traceback
from http.server import HTTPServer, BaseHTTPRequestHandler

OPCODE_HANDSHAKE = 0
OPCODE_FRAME = 1
OPCODE_CLOSE = 2
OPCODE_PING = 3
OPCODE_PONG = 4

_discord_sock = None
_discord_lock = threading.Lock()
_connected = False
_username = ""
_client_id = ""
_log_file = None


def _log(msg):
    global _log_file
    try:
        if _log_file is None:
            _log_file = open("/tmp/goodrp-bridge.log", "a")
        _log_file.write(f"{time.strftime('%H:%M:%S')} {msg}\n")
        _log_file.flush()
    except Exception:
        pass


def find_ipc_path(index=0):
    candidates = []
    xdg = os.environ.get("XDG_RUNTIME_DIR", "")
    tmpdir = os.environ.get("TMPDIR", "") or os.environ.get("TMP", "") or os.environ.get("TEMP", "")
    subdirs = [
        "",
        "app/com.discordapp.Discord",
        "snap.discord",
        ".flatpak/dev.vencord.Vesktop/xdg-run",
        "app/com.discordapp.DiscordCanary",
        "app/com.discordapp.DiscordPTB",
    ]
    for base in [xdg, tmpdir, "/tmp"]:
        if not base:
            continue
        for sub in subdirs:
            path = os.path.join(base, sub, f"discord-ipc-{index}") if sub else os.path.join(base, f"discord-ipc-{index}")
            candidates.append(path)
    return candidates


def send_frame_raw(sock, opcode, data):
    payload = json.dumps(data).encode("utf-8")
    header = struct.pack("<II", opcode, len(payload))
    sock.sendall(header + payload)


def recv_frame_raw(sock):
    header = b""
    while len(header) < 8:
        chunk = sock.recv(8 - len(header))
        if not chunk:
            raise ConnectionError("Socket closed")
        header += chunk
    opcode, length = struct.unpack("<II", header)
    if length > 0:
        payload = b""
        while len(payload) < length:
            chunk = sock.recv(length - len(payload))
            if not chunk:
                raise ConnectionError("Socket closed")
            payload += chunk
        return opcode, json.loads(payload.decode("utf-8"))
    return opcode, {}


def discord_connect(client_id):
    global _discord_sock, _connected, _username, _client_id

    if _connected:
        _log(f"Already connected as {_client_id}, ignoring connect to {client_id}")
        return True

    _log(f"Connecting to Discord with client_id={client_id}")
    for path in find_ipc_path(0):
        try:
            _log(f"Trying {path}")
            sock = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
            sock.connect(path)
            sock.settimeout(10)

            send_frame_raw(sock, OPCODE_HANDSHAKE, {"v": 1, "client_id": client_id})
            opcode, data = recv_frame_raw(sock)

            _log(f"Response opcode={opcode}, data={json.dumps(data)[:200]}")

            if opcode == OPCODE_FRAME and data.get("evt") == "READY":
                _username = data.get("data", {}).get("user", {}).get("username", "unknown")
                _connected = True
                _client_id = client_id
                sock.settimeout(None)
                _discord_sock = sock
                _log(f"Connected as {_username}")
                return True

            sock.close()
        except Exception as e:
            _log(f"Connection failed: {e}")
            try:
                sock.close()
            except Exception:
                pass
            continue

    _log("Failed to connect to Discord on any IPC path")
    return False


def discord_set_presence(title, artist, album, app_name, activity_type="listening", image_url=None):
    global _connected
    if not _connected or not _discord_sock:
        _log(f"set_presence: not connected (connected={_connected}, sock={_discord_sock})")
        return False

    try:
        state_parts = []
        if artist:
            state_parts.append(artist)
        if album:
            state_parts.append(album)
        state = " • ".join(state_parts) if state_parts else app_name

        details = title if title else "Unknown Track"

        atype_str = activity_type.lower()
        TYPE_MAP = {
            "playing": 0,
            "streaming": 1,
            "listening": 2,
            "watching": 3,
            "competing": 5,
        }
        atype = TYPE_MAP.get(atype_str, 2)

        assets = {"small_image_text": app_name or ""}
        if image_url:
            assets["large_image"] = image_url
            assets["large_image_text"] = details

        activity = {
            "type": atype,
            "state": state,
            "name": app_name or "GoodRP",
            "details": details,
            "assets": assets,
        }

        args = {
            "cmd": "SET_ACTIVITY",
            "args": {
                "pid": os.getpid(),
                "activity": activity,
            },
        }

        _log(f"SET_ACTIVITY: type={atype} title='{title}' artist='{artist}' app='{app_name}'")
        with _discord_lock:
            _discord_sock.settimeout(5)
            send_frame_raw(_discord_sock, OPCODE_FRAME, args)
            opcode, data = recv_frame_raw(_discord_sock)
            _discord_sock.settimeout(None)

        _log(f"SET_ACTIVITY response: opcode={opcode} data={json.dumps(data)[:200]}")
        return opcode == OPCODE_FRAME
    except Exception as e:
        _log(f"set_presence error: {e}")
        _connected = False
        return False


def discord_clear_presence():
    global _connected
    if not _connected or not _discord_sock:
        return False

    try:
        args = {
            "cmd": "SET_ACTIVITY",
            "args": {
                "pid": os.getpid(),
                "activity": None,
            },
        }

        with _discord_lock:
            send_frame_raw(_discord_sock, OPCODE_FRAME, args)
            opcode, data = recv_frame_raw(_discord_sock)

        return True
    except Exception:
        _connected = False
        return False


def discord_disconnect():
    global _connected, _discord_sock, _username, _client_id
    _connected = False
    _username = ""
    _client_id = ""
    if _discord_sock:
        try:
            send_frame_raw(_discord_sock, OPCODE_CLOSE, {"reason": "GoodRP shutting down"})
        except Exception:
            pass
        try:
            _discord_sock.close()
        except Exception:
            pass
        _discord_sock = None


class Handler(BaseHTTPRequestHandler):
    def log_message(self, fmt, *args):
        _log(fmt % args)

    def _send_json(self, status, obj):
        body = json.dumps(obj).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def _read_body(self):
        length = int(self.headers.get("Content-Length", 0))
        if length > 0:
            return json.loads(self.rfile.read(length))
        return {}

    def do_GET(self):
        if self.path == "/status":
            self._send_json(200, {
                "connected": _connected,
                "username": _username,
                "client_id": _client_id,
            })
        else:
            self._send_json(404, {"error": "not found"})

    def do_POST(self):
        if self.path == "/connect":
            data = self._read_body()
            client_id = data.get("client_id", "")
            if not client_id:
                self._send_json(400, {"error": "client_id required"})
                return
            if _connected:
                self._send_json(200, {"connected": True, "username": _username})
                return
            ok = discord_connect(client_id)
            if ok:
                self._send_json(200, {"connected": True, "username": _username})
            else:
                self._send_json(502, {"error": "Failed to connect to Discord"})

        elif self.path == "/presence":
            if not _connected:
                self._send_json(503, {"error": "Not connected to Discord"})
                return
            data = self._read_body()
            ok = discord_set_presence(
                title=data.get("title", ""),
                artist=data.get("artist", ""),
                album=data.get("album", ""),
                app_name=data.get("app", "GoodRP"),
                activity_type=data.get("type", "listening"),
                image_url=data.get("image_url"),
            )
            if ok:
                self._send_json(200, {"success": True})
            else:
                self._send_json(502, {"error": "Failed to set presence"})

        elif self.path == "/reconnect":
            data = self._read_body()
            client_id = data.get("client_id", _client_id)
            discord_disconnect()
            ok = discord_connect(client_id)
            if ok:
                self._send_json(200, {"connected": True, "username": _username})
            else:
                self._send_json(502, {"error": "Failed to reconnect"})

        else:
            self._send_json(404, {"error": "not found"})

    def do_DELETE(self):
        if self.path == "/presence":
            ok = discord_clear_presence()
            self._send_json(200, {"success": ok})
        else:
            self._send_json(404, {"error": "not found"})


class ReusableHTTPServer(HTTPServer):
    allow_reuse_address = True
    allow_reuse_port = True


def main():
    port = 19876
    if len(sys.argv) > 1:
        try:
            port = int(sys.argv[1])
        except ValueError:
            pass
    server = ReusableHTTPServer(("127.0.0.1", port), Handler)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        discord_disconnect()
        server.server_close()


if __name__ == "__main__":
    main()
