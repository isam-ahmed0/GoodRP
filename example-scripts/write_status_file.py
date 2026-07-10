#!/usr/bin/env python3
"""Write the current track to a status file that OBS, Rainmeter, or any
other tool can read and display.

GoodRP passes metadata via environment variables (os.environ).
"""
import os

title = os.environ.get("GOODRP_TITLE", "Unknown")
artist = os.environ.get("GOODRP_ARTIST", "Unknown")
album = os.environ.get("GOODRP_ALBUM", "")
app = os.environ.get("GOODRP_APP", "")

status = f"{title} — {artist}" if artist else title
if album:
    status += f"\n{album}"
if app:
    status += f"\nvia {app}"

out_dir = os.path.join(os.path.expanduser("~"), "Desktop")
out_path = os.path.join(out_dir, "now_playing.txt")

with open(out_path, "w", encoding="utf-8") as f:
    f.write(status)
