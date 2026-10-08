"""In-game screen content generator (平面视觉, 2026-10-07). The WXRM-TV 61 broadcast for the CRTs.

One spec (spec/*.json: strings from the narrative chat, timing) renders every screen the game shows,
in the system of Documentation/SCREENS_VISUAL_SYSTEM.md and Figma section 2800:6096:
  programme  the FRONT ROOMS FURNITURE spot (agency type: Anton, Archivo Black/Black Italic, Yellowtail)
  station    WXRM's character generator (VT323 + 2 px outline + hard shadow) and its states
  alarm      the building's slate (TeX Gyre Heros Bold = the placard's EGRESS face, EXIT-sign green)
  office     amber monochrome monitors (VT323 + glow)
The tube (scanlines, phosphor, glass) is NOT baked in: it is the in-game shader's job (游戏视觉).
Faces come from Assets/Fonts/Period1990 (OFL / GUST), so everything here can ship.

Outputs (out/<name>/):
  <name>.mp4              the loop: 640x480, 30 fps, H.264, hard cuts (for the near live screens)
  <name>_flipbook.png     4x4 atlas of 256x192 frames, one per second of the loop (every other screen)
  <name>_flipbook.json    frame order and seconds per frame
  cards/*.png             each programme card at 640x480
  states/*.png            standby, sign-off, alarm slate, power-off ghost, office screens
  review_tube.png         contact sheet with a tube stand-in, for review only
Usage: /usr/bin/python3 broadcast.py spec/wxrm_v1.json [out_dir]
"""
import json
import math
import os
import shutil
import subprocess
import sys
import tempfile

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
FONTS = os.path.abspath(os.path.join(HERE, "..", "..", "Assets", "Fonts", "Period1990"))
SS = 2                                    # supersampling; everything is designed in 640x480 units
W, H = 640, 480


def hexc(h):
    return tuple(int(h[i:i + 2], 16) for i in (1, 3, 5))


C = {k: hexc(v) for k, v in {
    "ink": "#0A0A0A", "acc": "#F4DF3B", "cream": "#F4F0EC", "grey": "#BBBEC0", "pink": "#DBAFA5",
    "slate": "#9B9BAA", "deep": "#887799", "vdark": "#3A3048", "vnight": "#1E1826", "cg": "#F2F2E5",
    "blue": "#1E3A8A", "bnight": "#0B1640", "red": "#E6542E", "rose": "#A07A8C", "amber": "#FFB347"}.items()}
C["glow"] = (199, 242, 148)               # (0.78, 0.95, 0.58): the green of a self-lit EXIT sign


def font(rel, size):
    return ImageFont.truetype(os.path.join(FONTS, rel), int(size * SS))


F = {
    "anton": "Anton/Anton-Regular.ttf",
    "arcB": "Archivo/Archivo-Black.ttf",
    "arcBI": "Archivo/Archivo-BlackItalic.ttf",
    "yt": "Yellowtail/Yellowtail-Regular.ttf",
    "vt": "VT323/VT323-Regular.ttf",
    "mich": "Michroma/Michroma-Regular.ttf",
    "heros": "TeXGyre/texgyreheros-bold.otf",
}


class Canvas:
    def __init__(self, top, bottom=None):
        if bottom is None:
            self.im = Image.new("RGB", (W * SS, H * SS), top)
        else:
            t = np.linspace(0, 1, H * SS)[:, None, None]
            g = np.array(top, np.float32) * (1 - t) + np.array(bottom, np.float32) * t
            self.im = Image.fromarray(np.repeat(np.round(g).astype(np.uint8), W * SS, axis=1))
        self.d = ImageDraw.Draw(self.im)

    def rect(self, x, y, w, h, col):
        self.d.rectangle([x * SS, y * SS, (x + w) * SS - 1, (y + h) * SS - 1], fill=col)

    def text(self, s, face, size, x, y, col, center=False, shadow=0, stroke=0, track=0.0, glow=None):
        f = font(F[face], size)
        widths = [f.getlength(ch) for ch in s]
        total = sum(widths) + track * size * SS * (len(s) - 1)
        x0 = x * SS - (total / 2 if center else 0)
        if glow:                                   # phosphor bloom: blurred copy added under the text
            layer = Image.new("RGB", self.im.size, (0, 0, 0))
            self._draw(ImageDraw.Draw(layer), s, f, widths, x0, y * SS, glow, 0, track * size * SS)
            layer = layer.filter(ImageFilter.GaussianBlur(5 * SS))
            add = np.asarray(self.im, np.float32) + np.asarray(layer, np.float32) * .55
            self.im = Image.fromarray(np.clip(add, 0, 255).astype(np.uint8))
            self.d = ImageDraw.Draw(self.im)
        if shadow:
            self._draw(self.d, s, f, widths, x0 + shadow * SS, y * SS + shadow * SS, (0, 0, 0), stroke * SS, track * size * SS)
        self._draw(self.d, s, f, widths, x0, y * SS, col, stroke * SS, track * size * SS)
        return total / SS

    @staticmethod
    def _draw(d, s, f, widths, x, y, col, stroke, gap):
        for ch, w in zip(s, widths):
            d.text((x, y), ch, font=f, fill=col, stroke_width=int(stroke), stroke_fill=(0, 0, 0))
            x += w + gap

    def poly(self, pts, col):
        self.d.polygon([(px * SS, py * SS) for px, py in pts], fill=col)

    def paste_rotated(self, img, x, y, deg):
        r = img.rotate(deg, resample=Image.BICUBIC, expand=True)
        cx, cy = x * SS + img.width / 2, y * SS + img.height / 2
        self.im.paste(r, (int(cx - r.width / 2), int(cy - r.height / 2)), r)

    def out(self):
        return self.im.resize((W, H), Image.LANCZOS)


def chevron_mark(cv, cx, apex, half, bands):
    s55 = math.tan(math.radians(55))
    y = apex
    for col, t in bands:
        d = s55 * half
        cv.poly([(cx - half, y + d), (cx, y), (cx + half, y + d), (cx + half, y + d + t), (cx, y + t), (cx - half, y + d + t)], col)
        y += t


def lockup(cv, s, top, k=1.0):
    chevron_mark(cv, 320, top, 46 * k, [(C["cream"], 18 * k), (C["pink"], 11 * k), (C["grey"], 9 * k)])
    cv.text(s["brand_script"], "yt", 84 * k, 320, top + 96 * k, C["cream"], center=True, shadow=3)
    cv.text(s["brand_caps"], "arcB", 20 * k, 320, top + 200 * k, C["cream"], center=True, track=.4)


def photo_card(spec_dir, path):
    p = os.path.join(spec_dir, path) if path else None
    border = Image.new("RGBA", (292 * SS, 222 * SS), (255, 255, 255, 255))
    if p and os.path.exists(p):
        ph = Image.open(p).convert("RGBA")
        bg = Image.new("RGBA", ph.size, (255, 255, 255, 255))
        bg.alpha_composite(ph)
        ph = bg.resize((276 * SS, 207 * SS), Image.LANCZOS)
        border.paste(ph, (8 * SS, 6 * SS))
        return border, True
    d = ImageDraw.Draw(border)
    d.rectangle([8 * SS, 6 * SS, 284 * SS, 213 * SS], fill=(214, 210, 200))
    return border, False


def card1(s, ctx):
    cv = Canvas(C["deep"], C["vdark"])
    lockup(cv, s, 56)
    cv.rect(64, 318, 512, 62, C["ink"])                       # title safe edge to edge
    cv.text(s["card1_super"], "arcBI", 31, 320, 330, C["acc"], center=True, shadow=3)
    return cv.out()


def card2(s, ctx):
    cv = Canvas(C["rose"], C["vdark"])
    cv.text(s["card2_headline"], "anton", 58, 64, 36, C["cream"], shadow=3)
    card, ok = photo_card(ctx["dir"], ctx["photo"])
    ctx["photo_ok"] = ok
    cv.paste_rotated(card, 70, 134, 2)
    cv.text(s["card2_now"], "arcBI", 26, 400, 128, C["cream"], shadow=2)
    cv.text("$", "anton", 44, 398, 166, C["acc"], stroke=2, shadow=4)
    cv.text(s["card2_price"], "anton", 124, 424, 142, C["acc"], stroke=3, shadow=5)
    w = cv.text(s["card2_was"], "arcBI", 24, 404, 312, C["cream"], shadow=2)
    cv.rect(402, 328, w + 6, 3, C["red"])
    cv.rect(64, 380, 512, 44, C["ink"])
    cv.text(s["card2_finance"], "arcBI", 26, 320, 384, C["acc"], center=True)
    return cv.out()


def card3(s, ctx):
    cv = Canvas(C["vdark"], C["vnight"])
    cv.text(s["card3_lead"], "arcBI", 44, 320, 52, C["cream"], center=True, shadow=3)
    cv.text(s["card3_phone"], "anton", 132, 320, 96, C["acc"], center=True, stroke=3, shadow=6)
    cv.text(s["card3_hours"], "arcBI", 28, 320, 286, C["cream"], center=True, shadow=2)
    cv.rect(160, 346, 320, 54, C["pink"])
    cv.text(s["card3_flag"], "arcB", 28, 320, 352, C["vdark"], center=True, track=.04)
    return cv.out()


def card4(s, ctx):
    cv = Canvas(C["deep"], C["vdark"])
    lockup(cv, s, 50, .82)
    cv.text(s["card4_line"], "arcBI", 30, 320, 258, C["cream"], center=True, shadow=3)
    cv.text(s["card4_cg"], "vt", 34, 70, 366, C["cg"], stroke=2, shadow=3, track=.02)
    return cv.out()


def station_id(s, ctx):
    cv = Canvas(C["blue"], C["bnight"])
    cv.d.ellipse([235 * SS, 70 * SS, 405 * SS, 240 * SS], outline=C["cream"], width=8 * SS)
    cv.text(s["station_ring"], "mich", 82, 320, 104, C["cream"], center=True, shadow=3)
    cv.text(s["station_call"], "mich", 40, 320, 266, C["cream"], center=True, shadow=3, track=.08)
    cv.text(s["station_market"], "vt", 36, 320, 334, C["cg"], center=True, stroke=2, shadow=3, track=.06)
    return cv.out()


def standby(s, ctx):
    cv = Canvas(C["ink"])
    bars = ["#BFBFBF", "#BFBF00", "#00BFBF", "#00BF00", "#BF00BF", "#BF0000", "#0000BF"]
    low = ["#0000BF", "#131313", "#BF00BF", "#131313", "#00BFBF", "#131313", "#BFBFBF"]
    for i, b in enumerate(bars):
        cv.rect(i * W / 7, 0, W / 7 + 1, 330, hexc(b))
        cv.rect(i * W / 7, 330, W / 7 + 1, 40, hexc(low[i]))
    cv.rect(0, 370, W, 110, (16, 16, 16))
    cv.rect(96, 150, 448, 150, C["ink"])
    cv.text(s["standby_1"], "arcB", 40, 320, 166, C["cream"], center=True, track=.02)
    cv.text(s["standby_2"], "vt", 34, 320, 230, C["cg"], center=True, track=.04)
    return cv.out()


def signoff(s, ctx):
    cv = Canvas(C["ink"])
    cv.text(s["signoff_1"], "vt", 44, 320, 160, C["cg"], center=True, stroke=2, shadow=3, track=.04)
    cv.text(s["signoff_2"], "vt", 44, 320, 206, C["cg"], center=True, stroke=2, shadow=3, track=.04)
    cv.text(s["signoff_short"], "vt", 30, 320, 296, C["slate"], center=True, track=.04)
    return cv.out()


def alarm(s, ctx):
    cv = Canvas((5, 6, 5))
    for i, line in enumerate(s["alarm"]):
        cv.text(line, "heros", 46, 320, 132 + i * 72, C["glow"], center=True, track=.06, glow=C["glow"])
    return cv.out()


def power_off(s, ctx):
    cv = Canvas((36, 38, 42), (12, 13, 15))
    ghost = Canvas((0, 0, 0))
    ghost.text(s["station_short"], "mich", 64, 320, 186, (255, 255, 255), center=True, track=.04)
    return Image.blend(cv.out(), Image.composite(Image.new("RGB", (W, H), C["cream"]), cv.out(), ghost.out().convert("L")), .05)


def office(lines, pos):
    def f(s, ctx):
        cv = Canvas((11, 10, 8))
        x, y = pos
        for i, line in enumerate(lines):
            if line:
                cv.text(line, "vt", 40, x, y + i * 46, C["amber"], track=.02, glow=C["amber"])
        return cv.out()
    return f


CARDS = {"card1": card1, "card2": card2, "card3": card3, "card4": card4, "station_id": station_id}


def tube_preview(im):
    """Review only: scanlines + vignette, the shader's job in game."""
    a = np.asarray(im, np.float32)
    a[1::2] *= .86
    yy, xx = np.mgrid[0:a.shape[0], 0:a.shape[1]]
    r = np.hypot((xx - a.shape[1] / 2) / (a.shape[1] / 2), (yy - a.shape[0] / 2) / (a.shape[0] / 2))
    a *= np.clip(1.25 - .45 * r ** 2, .55, 1)[..., None]
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))


def main(argv):
    spec_path = argv[1]
    spec = json.load(open(spec_path))
    spec_dir = os.path.dirname(os.path.abspath(spec_path))
    out = argv[2] if len(argv) > 2 else os.path.join(HERE, "out", spec["name"])
    for sub in ("cards", "states"):
        os.makedirs(os.path.join(out, sub), exist_ok=True)
    s = spec["strings"]
    ctx = {"dir": spec_dir, "photo": spec.get("photo")}
    cards = {}
    for key in dict.fromkeys(item["card"] for item in spec["sequence"]):
        cards[key] = CARDS[key](s, ctx)
        cards[key].save(os.path.join(out, "cards", key + ".png"))
    states = {"standby": standby(s, ctx), "signoff": signoff(s, ctx), "alarm": alarm(s, ctx), "power_off": power_off(s, ctx),
              "office_dos": office(["C:\\>_"], (64, 60))(s, ctx),
              "office_print": office(["PRINT QUEUE  1 JOB", "", "PLAN SHEET A-3 OF 4", "PRINTING"], (64, 60))(s, ctx),
              "office_idle": office(["SYSTEM IDLE", "OCCUPANT LOAD 1"], (300, 280))(s, ctx),
              "office_alarmlog": office(["ALARM LOG 03/90", "", "ZONE 04  TROUBLE  ACK"], (64, 60))(s, ctx)}
    for k, im in states.items():
        im.save(os.path.join(out, "states", k + ".png"))
    # the loop video: hard cuts, via the concat demuxer (one still per card)
    fps = spec.get("fps", 30)
    tmp = tempfile.mkdtemp(prefix="screens_")
    lines = []
    for item in spec["sequence"]:
        lines.append(f"file '{os.path.join(out, 'cards', item['card'] + '.png')}'\nduration {item['seconds']}")
    lines.append(f"file '{os.path.join(out, 'cards', spec['sequence'][-1]['card'] + '.png')}'")
    open(os.path.join(tmp, "list.txt"), "w").write("\n".join(lines) + "\n")
    mp4 = os.path.join(out, spec["name"] + ".mp4")
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-f", "concat", "-safe", "0", "-i", os.path.join(tmp, "list.txt"),
                    "-vf", f"fps={fps},format=yuv420p", "-t", str(sum(i["seconds"] for i in spec["sequence"])), "-c:v", "libx264", "-profile:v", "main", "-crf", "18", "-movflags", "+faststart", mp4], check=True)
    shutil.rmtree(tmp)
    # flipbook: one 256x192 frame per second of the loop, 4x4 atlas
    seq = []
    for item in spec["sequence"]:
        seq += [item["card"]] * int(round(item["seconds"]))
    seq = (seq * 2)[:16]
    atlas = Image.new("RGB", (4 * 256, 4 * 192))
    for i, key in enumerate(seq):
        atlas.paste(cards[key].resize((256, 192), Image.LANCZOS), ((i % 4) * 256, (i // 4) * 192))
    atlas.save(os.path.join(out, spec["name"] + "_flipbook.png"))
    json.dump({"columns": 4, "rows": 4, "frames": seq, "secondsPerFrame": 1.0, "frameSize": [256, 192],
               "loopSeconds": sum(i["seconds"] for i in spec["sequence"]), "srgb": True},
              open(os.path.join(out, spec["name"] + "_flipbook.json"), "w"), indent=2)
    # review sheet with the tube stand-in
    keys = list(cards) + list(states)
    ims = [tube_preview((cards.get(k) or states[k]).resize((320, 240), Image.LANCZOS)) for k in keys]
    sheet = Image.new("RGB", (4 * 330 + 10, ((len(ims) + 3) // 4) * 250 + 10), (24, 24, 24))
    for i, im in enumerate(ims):
        sheet.paste(im, (10 + (i % 4) * 330, 10 + (i // 4) * 250))
    sheet.save(os.path.join(out, "review_tube.png"))
    print(mp4, "photo:", "ok" if ctx.get("photo_ok") else "MISSING (placeholder drawn)")


if __name__ == "__main__":
    main(sys.argv)
