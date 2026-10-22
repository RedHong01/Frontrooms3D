"""In-game screen content generator (平面视觉, 2026-10-07; v3 station layer by 平面视觉 · 广告, 2026-10-22).
The station broadcast for the CRTs: WXRM-TV 61 in v1 (Ad_02), WTCZ-TV 61 from Ad_03 (WXRM is a real call sign).

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
PROJECT = os.environ.get("FR_PROJECT") or os.path.abspath(os.path.join(HERE, "..", ".."))   # dev copies set FR_PROJECT
FONTS = os.path.join(PROJECT, "Assets", "Fonts", "Period1990")
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

    def cg(self, s, lines, x, y, col, face="herosB", center=False, border=2, shadow=4, track=0.0):
        """Queue one character-generator line, drawn after the downsample (see cg_apply)."""
        self.__dict__.setdefault("cg_ops", []).append(dict(s=s, lines=lines, x=x, y=y, col=col, face=face,
                                                           center=center, border=border, shadow=shadow, track=track))

    def out(self):
        im = self.im.resize((W, H), Image.LANCZOS)
        ops = self.__dict__.get("cg_ops", [])
        return cg_apply(im, ops) if ops else im


# ---- Station character generator: a Chyron VP-2-class unit (research: ad_reference/station_1990 NOTES 1.5)
# 1510 positions across x 482 scan lines; sizes set in whole scan lines (library 18-58); Helvetica Medium / Bold;
# a 2-line black edge plus a 4-line drop shadow; 16 colours from 512 (3 bits a channel); no smoothing.
# One scan line = one game px down; one game px = 1510/640 CG positions across.
CG_X = 1510 / W
F["herosR"] = "TeXGyre/texgyreheros-regular.otf"
F["herosB"] = "TeXGyre/texgyreheros-bold.otf"


def q3(col):
    """Snap a colour to the VP-2's 512-colour space (3 bits a channel)."""
    return tuple(int(round(round(v / 255 * 7) * 255 / 7)) for v in col)


CG_WHITE, CG_YELLOW, CG_CYAN = q3((219, 219, 219)), q3((219, 219, 36)), q3((36, 219, 219))


def _cg_line_mask(s, face, lines, track):
    """Coverage of one line at CG resolution, no smoothing: an (rows, cols) 0/1 array and the cap-top row."""
    path = os.path.join(FONTS, F[face])
    probe = ImageFont.truetype(path, 1000)
    hb = probe.getbbox("H")
    cap = (hb[3] - hb[1]) / 1000
    px = max(4, int(round(lines / cap * CG_X)))           # em in CG positions, isotropic at CG_X x the line pitch
    f = ImageFont.truetype(path, px)
    widths = [f.getlength(ch) for ch in s]
    gap = track * px
    tw = int(math.ceil(sum(widths) + gap * (len(s) - 1))) + 8
    asc, desc = f.getmetrics()
    th = asc + desc + 8
    img = Image.new("L", (tw, th), 0)
    d = ImageDraw.Draw(img)
    d.fontmode = "1"                                      # a cheap CG has no anti-aliasing
    xx = 4
    for ch, wch in zip(s, widths):
        d.text((xx, 4), ch, font=f, fill=255)
        xx += wch + gap
    cap_top = 4 + f.getbbox("H")[1]
    rows = int(math.ceil(th / CG_X))
    centres = (np.arange(rows) + .5) * CG_X               # point-sample one row per scan line
    a = (np.asarray(img) > 127)[np.clip(centres.astype(int), 0, th - 1)]
    return a, cap_top / CG_X


def _dilate(a, rx, ry):
    out = a.copy()
    for dy in range(-ry, ry + 1):
        for dx in range(-rx, rx + 1):
            if dx * dx / max(rx, 1) ** 2 + dy * dy / max(ry, 1) ** 2 <= 1.0001:
                out |= np.roll(np.roll(a, dy, 0), dx, 1)
    return out


def cg_apply(im, ops):
    """Composite queued CG lines onto a 640x480 frame: layers at 1510 x 480, then box-filtered across only."""
    cw = int(round(W * CG_X))
    lay = {k: np.zeros((H, cw), np.float32) for k in ("sh", "ed", "fill")}
    fills = np.zeros((H, cw, 3), np.float32)
    for op in ops:
        a, cap_top = _cg_line_mask(op["s"], op["face"], op["lines"], op["track"])
        pad = int(math.ceil((op["border"] + op["shadow"]) * CG_X)) + 2
        a = np.pad(a, ((pad // 2 + op["shadow"] + 2, pad // 2 + op["shadow"] + 2), (pad, pad)))
        rows, cols = a.shape
        edge = _dilate(a, int(round(op["border"] * CG_X)), op["border"]) if op["border"] else a
        sh = np.roll(np.roll(edge, op["shadow"], 0), int(round(op["shadow"] * CG_X)), 1) if op["shadow"] else np.zeros_like(a)
        ink_w = cols - 2 * pad
        x0 = int(round(op["x"] * CG_X - (ink_w / 2 if op["center"] else 0))) - pad
        y0 = int(round(op["y"] - cap_top)) - (pad // 2 + op["shadow"] + 2)
        ys, xs = slice(max(y0, 0), min(y0 + rows, H)), slice(max(x0, 0), min(x0 + cols, cw))
        sy, sx = slice(ys.start - y0, ys.stop - y0), slice(xs.start - x0, xs.stop - x0)
        lay["sh"][ys, xs] = np.maximum(lay["sh"][ys, xs], sh[sy, sx])
        lay["ed"][ys, xs] = np.maximum(lay["ed"][ys, xs], edge[sy, sx])
        m = a[sy, sx]
        lay["fill"][ys, xs] = np.maximum(lay["fill"][ys, xs], m)
        fills[ys, xs][m] = q3(op["col"])
    def across(x):                                        # box filter 1510 -> 640 across; rows untouched
        idx = np.minimum((np.arange(cw) / CG_X).astype(int), W - 1)
        acc = np.zeros(x.shape[:1] + (W,) + x.shape[2:], np.float32)
        np.add.at(acc, (slice(None), idx), x)
        cnt = np.bincount(idx, minlength=W).astype(np.float32)
        return acc / cnt.reshape((1, W) + (1,) * (x.ndim - 2))
    base = np.asarray(im, np.float32)
    a_sh, a_ed, a_fill = across(lay["sh"])[..., None], across(lay["ed"])[..., None], across(lay["fill"])[..., None]
    col = across(fills * lay["fill"][..., None]) / np.maximum(a_fill, 1e-6)
    out = base * (1 - a_sh)
    out = out * (1 - a_ed)
    out = out * (1 - a_fill) + col * a_fill
    return Image.fromarray(np.clip(np.round(out), 0, 255).astype(np.uint8))


def cg_extents(ops):
    """Ink box of every queued CG line in game px, for the title-safe check (RP 27.3-1989: x 66-574, y 48-432)."""
    out = []
    for op in ops:
        a, cap_top = _cg_line_mask(op["s"], op["face"], op["lines"], op["track"])
        cols = np.where(a.any(0))[0]
        rows = np.where(a.any(1))[0]
        w = (cols[-1] - cols[0] + 1) / CG_X
        x0 = op["x"] - (w / 2 if op["center"] else 0)
        y0 = op["y"] - cap_top + rows[0]
        y1 = op["y"] - cap_top + rows[-1] + 1
        safe = 66 <= x0 and x0 + w <= 574 and 48 <= y0 and y1 + op["shadow"] <= 432
        out.append((op["s"], round(x0, 1), round(x0 + w, 1), round(y0, 1), round(y1, 1), safe))
    return out


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


def card4_v3(s, ctx):
    """Ad_03 card 4: the store's sign-off (agency layer, as Ad_02) with the dealer tag keyed by the station's CG (v3)."""
    cv = Canvas(C["deep"], C["vdark"])
    lockup(cv, s, 50, .82)
    cv.text(s["card4_line"], "arcBI", 30, 320, 258, C["cream"], center=True, shadow=3)
    for i, line in enumerate(s.get("card4_cg_lines", [])):
        cv.cg(line, 24, 320, 330 + i * 38, CG_WHITE, face="herosR", center=True, track=.06)
    return cv.out()


def station_id(s, ctx):
    cv = Canvas(C["blue"], C["bnight"])
    cv.d.ellipse([235 * SS, 70 * SS, 405 * SS, 240 * SS], outline=C["cream"], width=8 * SS)
    cv.text(s["station_ring"], "mich", 82, 320, 104, C["cream"], center=True, shadow=3)
    cv.text(s["station_call"], "mich", 40, 320, 266, C["cream"], center=True, shadow=3, track=.08)
    cv.text(s["station_market"], "vt", 36, 320, 334, C["cg"], center=True, stroke=2, shadow=3, track=.06)
    return cv.out()


def bars_eg1(cv):
    """SMPTE colour bars, EG 1-1990 (approved 28 Mar 1990, revising ECR 1-1978), 75 % bars, at 640x480.
    Rows 67 / 8 / 25 %: seven bars; reverse castellations; then -I, 100 % white, +Q and black, each 5/4 of a
    bar wide, the PLUGE (3.5 / 7.5 / 11.5 IRE) in thirds under bar 6, and black under bar 7.
    Values are what a calibrated monitor shows: setup (7.5 IRE) maps to 0 and 100 IRE to 255, so the gray
    bar (77 IRE) is 191, the -4 PLUGE step sits at black and the +4 step just shows (11).
    Source: ~/FrontRoomsVisualWork/reference/ad_reference/station_1990/NOTES.md 1.4."""
    bw = W / 7
    top = ["#BFBFBF", "#BFBF00", "#00BFBF", "#00BF00", "#BF00BF", "#BF0000", "#0000BF"]
    mid = ["#0000BF", "#000000", "#BF00BF", "#000000", "#00BFBF", "#000000", "#BFBFBF"]
    y1, y2 = round(H * .67), round(H * .75)
    for i in range(7):
        x0, x1 = round(i * bw), round((i + 1) * bw)
        cv.rect(x0, 0, x1 - x0, y1, hexc(top[i]))
        cv.rect(x0, y1, x1 - x0, y2 - y1, hexc(mid[i]))
    edges = [0, 1.25, 2.5, 3.75, 5, 5 + 1 / 3, 5 + 2 / 3, 6, 7]
    low = ["#000F3D", "#FFFFFF", "#22005E", "#000000", "#000000", "#000000", "#0B0B0B", "#000000"]
    for i, col in enumerate(low):
        x0, x1 = round(edges[i] * bw), round(edges[i + 1] * bw)
        cv.rect(x0, y2, x1 - x0, H - y2, hexc(col))


def bars(s, ctx):
    """Sign-on / sign-off bars with the station's call keyed in, as a station's master control would."""
    cv = Canvas(C["ink"])
    bars_eg1(cv)
    key = s.get("bars_key")
    if key:
        f = font(F["vt"], 36)
        w = (sum(f.getlength(ch) for ch in key) + .06 * 36 * SS * (len(key) - 1)) / SS
        cv.rect(320 - w / 2 - 14, 132, w + 28, 52, (0, 0, 0))
        cv.text(key, "vt", 36, 320, 138, C["cg"], center=True, track=.06)
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


# ---- Station states v3 (2026-10-15, 平面视觉 · 广告): research-corrected.
# The channel number is the brand; no "UHF", no corner bug and no time/temperature corner (both post-1993);
# CG lines on a VP-2-class unit (cg_apply). Sizes are cap heights in scan lines; NEAR floor 18.

def id_slide(cv, k=1.0, top=70):
    """The station's ID slide (WTCZ-TV 61 since 2026-10-22; was WXRM): the 61 ring and the call letters (Michroma = the station's Eurostile-type logo face)."""
    r, size = 85 * k, 72 * k                              # the numeral sits inside the ring (Ad_02's 82 px overran it)
    cv.d.ellipse([(320 - r) * SS, top * SS, (320 + r) * SS, (top + 2 * r) * SS], outline=C["cream"], width=int(8 * k * SS))
    f = font(F["mich"], size)
    cap_off = f.getbbox("6")[1] / SS                      # draw origin -> cap top
    cap_h = .75 * size                                    # Michroma cap height / em
    cv.text("61", "mich", size, 320, top + r - cap_h / 2 - cap_off, C["cream"], center=True, shadow=3)


def station_id_v3(s, ctx):
    cv = Canvas(C["blue"], C["bnight"])
    id_slide(cv)
    cv.text(s["station_call"], "mich", 40, 320, 266, C["cream"], center=True, shadow=3, track=.08)
    cv.cg(s["station_market"], 30, 320, 336, CG_WHITE, center=True, track=.06)
    if s.get("legal_id"):                                 # 47 CFR 73.1201: call letters, channel, city of licence
        cv.cg(s["legal_id"], 18, 320, 392, CG_WHITE, face="herosR", center=True, track=.08)
    return cv.out()


def station_id_slogan_v3(s, ctx):
    cv = Canvas(C["blue"], C["bnight"])
    id_slide(cv)
    cv.text(s["station_call"], "mich", 40, 320, 266, C["cream"], center=True, shadow=3, track=.08)
    cv.cg(s["station_slogan"], 36, 320, 330, CG_YELLOW, center=True, track=.04)
    return cv.out()


def standby_v3(s, ctx):
    """The ID slide doubles as the stand-by card (WVTV 1980, KPVI 1993), with the CG lines keyed over it."""
    cv = Canvas(C["blue"], C["bnight"])
    id_slide(cv, .72, 58)
    cv.text(s["station_call"], "mich", 30, 320, 196, C["cream"], center=True, shadow=3, track=.08)
    cv.cg(s["standby_1"], 36, 320, 286, CG_WHITE, center=True, track=.04)
    cv.cg(s["standby_2"], 24, 320, 346, CG_WHITE, face="herosR", center=True, track=.06)
    return cv.out()


def signoff_v3(s, ctx):
    """The announcer's sign-off over the ID slide, before the anthem film."""
    cv = Canvas(C["blue"], C["bnight"])
    id_slide(cv, .6, 52)
    cv.cg(s["signoff_1"], 30, 320, 236, CG_WHITE, center=True, track=.04)
    cv.cg(s["signoff_2"], 30, 320, 280, CG_WHITE, center=True, track=.04)
    cv.cg(s.get("legal_id") or s["signoff_short"], 18, 320, 358, CG_WHITE, face="herosR", center=True, track=.08)
    return cv.out()


def signoff_notice_v3(s, ctx):
    """Sign-off page 2: the notice (standards line, return time) as a CG page over the ID slide's field."""
    cv = Canvas(C["blue"], C["bnight"])
    lines = s.get("notice", [])
    pitch = 40
    top = 240 - (len(lines) * pitch - 16) / 2
    for i, line in enumerate(lines):
        cv.cg(line, 24, 320, round(top + i * pitch), CG_WHITE, face="herosR", center=True, track=.06)
    return cv.out()


def thought_v3(s, ctx):
    """Thought For Today: the late-night card before the sign-off (WFLD 1988-89 kept one)."""
    cv = Canvas(C["blue"], C["bnight"])
    cv.cg(s.get("thought_title", "THOUGHT FOR TODAY"), 30, 320, 142, CG_YELLOW, center=True, track=.04)
    for i, line in enumerate(s.get("thought", [])):
        cv.cg(line, 24, 320, 222 + i * 44, CG_WHITE, face="herosR", center=True, track=.06)
    return cv.out()


def bars_clock(s, ctx):
    """Off air: EG 1-1990 bars with the call and the time and date keyed in (WFLD 1988-89)."""
    cv = Canvas(C["ink"])
    bars_eg1(cv)
    cv.cg(s.get("bars_call", "WXRM-TV 61"), 30, 320, 96, CG_WHITE, center=True, track=.06)
    cv.cg(s.get("bars_time", "2:07 AM"), 48, 320, 150, CG_WHITE, center=True, track=.04)
    cv.cg(s.get("bars_date", "3-16-90"), 30, 320, 222, CG_WHITE, center=True, track=.06)
    return cv.out()


def tag_v3(s, ctx):
    """FRONT ROOMS FURNITURE dealer tag: the last 3-7 s of a 30 s spot, a full-page CG card on black."""
    cv = Canvas((0, 0, 0))
    cv.cg(s.get("tag_store", "FRONT ROOMS"), 36, 320, 70, CG_YELLOW, center=True, track=.04)
    cv.cg(s.get("tag_store_2", "FURNITURE"), 24, 320, 118, CG_YELLOW, center=True, track=.2)
    cv.cg(s.get("tag_address", "RT. 41 AT MAIN ST."), 24, 320, 174, CG_WHITE, face="herosR", center=True, track=.06)
    if s.get("tag_town"):
        cv.cg(s["tag_town"], 24, 320, 210, CG_WHITE, face="herosR", center=True, track=.06)
    cv.cg(s["card3_phone"], 48, 320, 256, CG_WHITE, center=True, track=.02)
    cv.cg(s.get("tag_hours", "OPEN 9 TO 9  7 DAYS"), 24, 320, 338, CG_WHITE, face="herosR", center=True, track=.06)
    return cv.out()


def power_off_v3(s, ctx):
    """Dark glass only: no station bug existed in 1990, so there is nothing to burn in."""
    return Canvas((36, 38, 42), (12, 13, 15)).out()


def logo(s, ctx):
    """The game's wordmark on the set (promo end): Assets/Resources/Brand/FrontRoomsLogo.png, dark ink
    inverted to white on black, with a soft phosphor bloom. Width 520 of 640 (inside title safe)."""
    path = os.path.join(PROJECT, "Assets", "Resources", "Brand", "FrontRoomsLogo.png")
    lg = Image.open(path).convert("RGBA")
    w = 520 * SS
    lg = lg.resize((w, round(lg.height * w / lg.width)), Image.LANCZOS)
    a = np.asarray(lg, np.float32) / 255.0
    white = (1.0 - a[..., :3]) * a[..., 3:4]
    cv = np.zeros((H * SS, W * SS, 3), np.float32)
    y0, x0 = (H * SS - lg.height) // 2, (W * SS - lg.width) // 2
    cv[y0:y0 + lg.height, x0:x0 + lg.width] = white
    glow = np.asarray(Image.fromarray((cv * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(6 * SS)), np.float32) / 255.0
    out = np.clip(cv * np.array([.96, .97, .92]) + glow * .35, 0, 1)
    return Image.fromarray((out * 255 + .5).astype(np.uint8)).resize((W, H), Image.LANCZOS)


def office(lines, pos):
    def f(s, ctx):
        cv = Canvas((11, 10, 8))
        x, y = pos
        for i, line in enumerate(lines):
            if line:
                cv.text(line, "vt", 40, x, y + i * 46, C["amber"], track=.02, glow=C["amber"])
        return cv.out()
    return f


CARDS = {"card1": card1, "card2": card2, "card3": card3, "card4": card4, "station_id": station_id,
         "card4_v3": card4_v3, "station_id_v3": station_id_v3}


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
    if spec.get("states"):                              # v3 specs name their station states (Ad_03 on)
        states = {k: globals()[k](s, ctx) for k in spec["states"]}
    else:                                               # v1 (Ad_02) keeps its original state set, byte for byte
        states = {"standby": standby(s, ctx), "signoff": signoff(s, ctx), "alarm": alarm(s, ctx), "power_off": power_off(s, ctx),
                  "logo": logo(s, ctx),
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
