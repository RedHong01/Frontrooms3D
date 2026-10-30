#!/usr/bin/env python3
"""Bake the station set's light track from the ad (游戏视觉 station-look-v1; 30L §3.1 "a track baked offline from the ad",
r7 §10.1; plan 70 §8).

  python3 Tools/lookdev/bake_tv_light_track.py [mp4] [out json]
    mp4       default Assets/StreamingAssets/FrontRooms_Ad_01.mp4
    out json  default Assets/Resources/Rendering/StationTvLightTrack.json

10 Hz x the loop (160 samples for the 16 s ad): per sample the frame's mean LINEAR colour (r, g, b) and its luminance Y
(Rec. 709), from ffmpeg-decoded frames scaled to 64 x 36 (a mean is all the light needs). FrontRoomsStationTvLight
indexes it by the shared ad bank's VideoPlayer time: the same curve on every platform, no GPU read-back. Re-bake when
the ad changes (the source sha1 is stored). Also prints the photosafety numbers (changes >= 10 % per second, the
largest step) for the build report.
"""
import hashlib
import json
import os
import subprocess
import sys

import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
SRC = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "Assets", "StreamingAssets", "FrontRooms_Ad_01.mp4")
OUT = sys.argv[2] if len(sys.argv) > 2 else os.path.join(ROOT, "Assets", "Resources", "Rendering", "StationTvLightTrack.json")
HZ, W, H = 10, 64, 36


def s2l(c):
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def main():
    sha1 = hashlib.sha1(open(SRC, "rb").read()).hexdigest()
    dur = float(subprocess.check_output(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "default=nw=1:nk=1", SRC]).strip())
    raw = subprocess.check_output(["ffmpeg", "-v", "error", "-i", SRC, "-vf", "fps=%d,scale=%d:%d:flags=area" % (HZ, W, H), "-f", "rawvideo", "-pix_fmt", "rgb24", "-"])
    frames = np.frombuffer(raw, np.uint8).reshape(-1, H, W, 3).astype(np.float64) / 255.0
    n = int(round(dur * HZ))
    frames = frames[:n]
    lin = s2l(frames).reshape(len(frames), -1, 3).mean(1)
    y = lin @ np.array([0.2126, 0.7152, 0.0722])
    rgby = np.column_stack([lin, y])
    data = {"hz": HZ, "loopSeconds": round(len(frames) / HZ, 3), "count": len(frames), "rgby": [round(float(v), 5) for v in rgby.reshape(-1)],
            "source": os.path.basename(SRC), "sha1": sha1}
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", newline="\n") as f:
        json.dump(data, f, separators=(",", ":"))
        f.write("\n")
    # Photosafety on the r7 intensity curve (I = 6 sqrt(Y / 0.14), clamped 4.86-8.4).
    i = np.clip(6 * np.sqrt(y / 0.140), 4.86, 8.4)
    steps = np.abs(np.diff(np.concatenate([i, i[:1]]))) / np.maximum(i, 1e-6)
    big = steps >= 0.10
    per_s = max(int(big[k:k + HZ].sum()) for k in range(len(big)))
    print("[bake] %s sha1 %s: %d samples at %d Hz (%.1f s); Y %.3f-%.3f; intensity %.2f-%.2f; changes >= 10%% in any 1 s: %d; largest step %+.0f%%"
          % (os.path.basename(SRC), sha1[:8], len(frames), HZ, len(frames) / HZ, y.min(), y.max(), i.min(), i.max(), per_s,
             100 * (np.max(np.diff(np.concatenate([i, i[:1]])) / i))))


if __name__ == "__main__":
    main()
