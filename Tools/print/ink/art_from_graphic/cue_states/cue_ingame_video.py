"""Assemble the in-game cue preview (FrontRoomsCuePreviewCapture frames) into one review MP4. 平面视觉.
Each pass (A down the corridor, B along a wall) gets a caption bar naming the simulated order's beat,
so a reviewer can tell what the game is doing at every moment. Frames are read from the clone and
the temporary captioned frames go to a temp dir outside the project (Red's repo auto-commits).
Usage: /usr/bin/python3 cue_ingame_video.py <clone>/Verification/cue_preview <out.mp4>
"""
import os
import shutil
import subprocess
import sys
import tempfile

from PIL import Image, ImageDraw, ImageFont

FPS = 30
# (start s, label) of the harness's simulated order: TWarn 2, TChase 6, TSearch 11, TStage0 14, end 15.5
BEATS = [(0.0, "NORMAL · plain hard-edge print"),
         (2.0, "WARN STAGE 1 · lamp bursts · the room set's arrows turn (3 × 30°), no stepping"),
         (6.0, "CHASE · stepping wave leaves your room at 8 m/s · lamps dip as it passes"),
         (11.0, "SEARCH · the wave retracts outside-in, each cell turns back after its loop"),
         (14.0, "STAGE 0 · your room turns back")]
PASS = {"A": "A · down the corridor (route = away from camera)", "B": "B · along one wall (route = left to right on screen)"}


def caption(im, top, beat, t):
    d = ImageDraw.Draw(im)
    f1 = ImageFont.truetype("/System/Library/Fonts/Menlo.ttc", 22)
    f2 = ImageFont.truetype("/System/Library/Fonts/Menlo.ttc", 18)
    d.rectangle([0, im.height - 78, im.width, im.height], fill=(10, 10, 10))
    d.text((24, im.height - 70), beat, fill=(244, 223, 59), font=f1)
    d.text((24, im.height - 36), f"{top}   ·   t = {t:5.2f} s   ·   simulated order, Relay dormant · seed 4242", fill=(230, 228, 220), font=f2)


def main(src, out):
    tmp = tempfile.mkdtemp(prefix="cue_ingame_")
    n = 0
    for tag in ("A", "B"):
        d = os.path.join(src, tag)
        if not os.path.isdir(d):
            continue
        files = sorted(f for f in os.listdir(d) if f.endswith(".png"))
        for i, f in enumerate(files):
            t = i / FPS
            beat = [b for s, b in BEATS if t >= s][-1]
            im = Image.open(os.path.join(d, f)).convert("RGB")
            caption(im, PASS[tag], beat, t)
            im.save(os.path.join(tmp, f"f{n:05d}.png"))
            n += 1
        # a half-second black gap between passes
        if tag == "A":
            for _ in range(FPS // 2):
                Image.new("RGB", im.size, (0, 0, 0)).save(os.path.join(tmp, f"f{n:05d}.png"))
                n += 1
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", os.path.join(tmp, "f%05d.png"),
                    "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "21", "-preset", "slow", "-movflags", "+faststart", out], check=True)
    shutil.rmtree(tmp)
    print(out, n, "frames")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
