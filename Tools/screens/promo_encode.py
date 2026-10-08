"""Encode the FrontRooms promo (FrontRoomsPromoCapture frames) for Instagram Reels. 平面视觉, 2026-10-08.

Input: <capture>/hdr/fNNNN.rgba64le (raw 16-bit PQ, Rec.2020, rows bottom-up) and <capture>/sdr/fNNNN.png (8-bit sRGB, ACES).
Adds a 2.5 s end card (the game's wordmark, Assets/Resources/Brand/FrontRoomsLogo.png, inverted to
white on black; HDR white at the BT.2408 reference 203 nits) and writes:
  <out>_HDR10.mp4  HEVC Main10, 1080x1920, 30 fps, BT.2020 / SMPTE 2084, HDR10 static metadata
  <out>_SDR.mp4    H.264 High, 1080x1920, 30 fps, BT.709 (fallback, and for anything that is not HDR)
Temporary files go to a temp dir, never into the project (Red's repo auto-commits).
Usage: /usr/bin/python3 promo_encode.py <capture_dir> <logo.png> <out_prefix>
"""
import os
import shutil
import subprocess
import sys
import tempfile

import numpy as np
from PIL import Image

W, H, FPS, END_S, FADE_S = 1080, 1920, 30, 2.5, 0.6


def pq(nits):
    y = np.clip(nits / 10000.0, 0, 1)
    m1, m2, c1, c2, c3 = 0.1593017578125, 78.84375, 0.8359375, 18.8515625, 18.6875
    yp = y ** m1
    return ((c1 + c2 * yp) / (1 + c3 * yp)) ** m2


def end_cards(logo_path, tmp):
    logo = Image.open(logo_path).convert("RGBA")
    w = 840
    logo = logo.resize((w, round(logo.height * w / logo.width)), Image.LANCZOS)
    a = np.asarray(logo, np.float32) / 255.0
    rgb_srgb = 1.0 - a[..., :3]                                  # dark wordmark -> white on black
    lin = np.where(rgb_srgb <= 0.04045, rgb_srgb / 12.92, ((rgb_srgb + 0.055) / 1.055) ** 2.4) * a[..., 3:4]
    canvas = np.zeros((H, W, 3), np.float32)
    y0, x0 = (H - logo.height) // 2, (W - logo.width) // 2
    canvas[y0:y0 + logo.height, x0:x0 + logo.width] = lin
    # SDR still
    s = np.where(canvas <= 0.0031308, canvas * 12.92, 1.055 * canvas ** (1 / 2.4) - 0.055)
    Image.fromarray((np.clip(s, 0, 1) * 255 + .5).astype(np.uint8)).save(os.path.join(tmp, "end_sdr.png"))
    # HDR still: white = 203 nits (grey, so Rec.709 -> Rec.2020 leaves it unchanged), PQ, 16-bit
    code = (pq(canvas * 203.0) * 65535 + .5).astype("<u2")
    raw = os.path.join(tmp, "end_hdr.raw")
    code.tofile(raw)
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-f", "rawvideo", "-pix_fmt", "rgb48le", "-s", f"{W}x{H}", "-i", raw,
                    "-frames:v", "1", os.path.join(tmp, "end_hdr.png")], check=True)


def main(cap, logo, out):
    tmp = tempfile.mkdtemp(prefix="promo_")
    end_cards(logo, tmp)
    fc = f"[1:v]fade=in:st=0:d={FADE_S}[e];[0:v][e]concat=n=2:v=1[v]"
    hdr_params = ("hdr-opt=1:repeat-headers=1:colorprim=bt2020:transfer=smpte2084:colormatrix=bt2020nc:"
                  "master-display=G(13250,34500)B(7500,3000)R(34000,16000)WP(15635,16450)L(10000000,1):max-cll=750,200")
    # HDR frames are Unity's raw RGBA64 (16-bit little-endian, rows bottom-up): stream them in order, flip.
    raws = sorted(f for f in os.listdir(os.path.join(cap, "hdr")) if f.endswith(".rgba64le"))
    fc_h = "[0:v]vflip[m];[1:v]fade=in:st=0:d=" + str(FADE_S) + "[e];[m][e]concat=n=2:v=1[v]"
    ff = subprocess.Popen(["ffmpeg", "-y", "-loglevel", "error", "-f", "rawvideo", "-pix_fmt", "rgba64le", "-s", f"{W}x{H}", "-framerate", str(FPS), "-i", "-",
                           "-loop", "1", "-framerate", str(FPS), "-t", str(END_S), "-i", os.path.join(tmp, "end_hdr.png"),
                           "-filter_complex", fc_h + ";[v]scale=out_color_matrix=bt2020nc:out_range=tv,format=yuv420p10le[o]", "-map", "[o]",
                           "-c:v", "libx265", "-preset", "slow", "-crf", "16", "-x265-params", hdr_params, "-tag:v", "hvc1",
                           "-color_primaries", "bt2020", "-color_trc", "smpte2084", "-colorspace", "bt2020nc", "-color_range", "tv",
                           "-movflags", "+faststart", out + "_HDR10.mp4"], stdin=subprocess.PIPE)
    for f in raws:
        with open(os.path.join(cap, "hdr", f), "rb") as fh:
            ff.stdin.write(fh.read())
    ff.stdin.close()
    if ff.wait() != 0:
        raise SystemExit("HDR encode failed")
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", os.path.join(cap, "sdr", "f%04d.png"),
                    "-loop", "1", "-framerate", str(FPS), "-t", str(END_S), "-i", os.path.join(tmp, "end_sdr.png"),
                    "-filter_complex", fc + ";[v]scale=out_color_matrix=bt709:out_range=tv,format=yuv420p[o]", "-map", "[o]",
                    "-c:v", "libx264", "-preset", "slow", "-crf", "17", "-profile:v", "high",
                    "-color_primaries", "bt709", "-color_trc", "bt709", "-colorspace", "bt709", "-color_range", "tv",
                    "-movflags", "+faststart", out + "_SDR.mp4"], check=True)
    shutil.rmtree(tmp)
    print(out + "_HDR10.mp4", out + "_SDR.mp4")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3])
