"""Off-air bars v3.2 for the station TV: the bars plate and the live clock's key. 平面视觉 · 广告, 2026-11-03.

Red's live-clock rule (2026-10-22, 游戏视觉 chat): "…我希望新的钟表是可以正常走动，并且呼应现实世界的时间的（钟表只是一点，
还有游戏里面的电脑等一系列有时间显示相关的设备的）". The off-air bars therefore can't carry a baked clock, and v3.1's
centred call / time / date master (broadcast.py bars_clock, a sample) moves to the layout WFLD 32 aired on 17 Dec 1989
(~/FrontRoomsVisualWork/reference/ad_reference/frames/A1_wfld_bars_0125*.png; station_1990/NOTES.md "Bars as aired"):
  - the station's logo keyed in the upper half (WFLD: FOX 32 over "WFLD · CHICAGO", which is also a visual legal ID);
  - the date over a running clock, bottom right, from a time-and-date inserter, not the CG.
Spec: Documentation/AD_TYPE_SYSTEM.md §3.6. Outputs (out/offair_bars/):
  bars_plate.png, @2x            EG 1-1990 bars + the keyed WTCZ-TV 61 / FAIRVIEW logo; no date, no time
  clock_glyphs.png, @2x          the inserter's 12 glyphs "0123456789-:", pre-keyed, straight-alpha RGBA
  offair_key.json                cells, positions, formats, tick, window and the compositing rule for the runtime
  preview_*.png                  the plate with a SAMPLE clock (and a tube stand-in), for review only, never shipped
The inserter face is drawn here as geometry, not set from a font (no download): 3 x 5 blocks, a 6 degree slant, one
fixed 4-block cell per character, measured from the WFLD frames (448 x 336: a block is 4 px x 4.2 rows, the cell pitch
16 px, the row pitch 33 rows; '1' is one right-hand stroke and the colon sits low, so it reads "01: 25: 36").
Usage: python3 offair.py [out_dir] [--unit 5] [--logo-dx 0] [--sample "12-01-90 03:48:27"]
       (default out: Tools/screens/out/offair_bars; FR_PROJECT overrides the project root)
"""
import json
import math
import os
import sys

import numpy as np
from PIL import Image

import broadcast as B

W, H = B.W, B.H

# ---- The time-and-date inserter (WFLD 32, 17 Dec 1989). One block = UNIT game px across x UNIT scan lines.
GLYPHS = {                                 # 3 x 5 blocks, top row first; '#' = lit
    "0": ("###", "#.#", "#.#", "#.#", "###"),
    "1": ("..#", "..#", "..#", "..#", "..#"),   # one right-hand stroke, as aired ("12- 17", "0 1:")
    "2": ("###", "..#", "###", "#..", "###"),
    "3": ("###", "..#", ".##", "..#", "###"),   # the middle bar stops short, as aired
    "4": ("#.#", "#.#", "###", "..#", "..#"),
    "5": ("###", "#..", "###", "..#", "###"),
    "6": ("###", "#..", "###", "#.#", "###"),
    "7": ("###", "#.#", "..#", "..#", "..#"),   # the short hook under the bar, as aired ("37")
    "8": ("###", "#.#", "###", "#.#", "###"),
    "9": ("###", "#.#", "###", "..#", "..#"),   # no tail, as aired ("89")
    "-": ("...", "...", "###", "...", "..."),
    ":": ("...", "...", "#..", "...", "#.."),   # low and to the left of its cell, as aired
}
ORDER = "0123456789-:"
SLANT = 0.105                              # tan 6 deg: WFLD's strokes lean 2 px over 18 rows (0.11) and 1 over 14 (0.07)
CELL, ROW = 4, 8                           # cell pitch and row pitch in blocks (WFLD: 16 px = 4 x 4; 33 rows = 7.9 x 4.2)
MARGIN = 2                                 # sprite margin (game px) around the cell, for the edge and the slant
INK = B.CG_WHITE                           # the inserter's white = the CG's (219, 219, 219): broadcast-legal, 86 %
TITLE_SAFE = (66, 48, 574, 432)            # RP 27.3-1989


def glyph_sprite(ch, unit, scale):
    """One pre-keyed glyph: (rgba uint8, fill, edge) at `scale` x the 640 x 480 raster.
    Drawn like the CG (broadcast.cg_apply): binary at 1510 positions across x whole scan lines, slanted one scan line at a
    time, a 1-line black border (bordered mode, X 2 positions, Y 1 line), then box-filtered across only; rows stay steps."""
    rows = GLYPHS[ch]
    gh = 5 * unit                                          # scan lines
    sw, sh = CELL * unit + 2 * MARGIN, gh + 2 * MARGIN     # sprite in game px
    per = B.CG_X                                           # CG positions per game px
    P = int(math.ceil(sw * per))
    a = np.zeros((sh, P), bool)
    centres = np.arange(P) + .5
    for line in range(gh):
        r = rows[line // unit]
        shift = (gh - 1 - line + .5) * SLANT               # game px; the bottom line sits on the cell's left edge
        for c in range(3):
            if r[c] == "#":
                x0 = (MARGIN + c * unit + shift) * per
                x1 = (MARGIN + (c + 1) * unit + shift) * per
                a[MARGIN + line] |= (centres >= x0) & (centres < x1)
    edge = B._dilate(a, int(round(1 * per)), 1)

    def across(m, s):
        idx = np.minimum((centres / (per / s)).astype(int), sw * s - 1)
        acc = np.zeros((m.shape[0], sw * s), np.float32)
        np.add.at(acc, (slice(None), idx), m.astype(np.float32))
        cnt = np.bincount(idx, minlength=sw * s).astype(np.float32)
        return np.repeat(acc / np.maximum(cnt, 1), s, axis=0)   # each scan line becomes `s` rows

    f, e = across(a, scale), across(edge, scale)
    alpha = 1 - (1 - e) * (1 - f)                          # = cg_apply's edge-then-fill, as one straight-alpha layer
    rgb = np.zeros(f.shape + (3,), np.float32)
    lit = alpha > 1e-6
    rgb[lit] = (np.array(INK, np.float32)[None, :] * (f[lit] / alpha[lit])[:, None])
    rgba = np.dstack([np.clip(np.round(rgb), 0, 255), np.clip(np.round(alpha * 255), 0, 255)]).astype(np.uint8)
    return rgba, f, e


def composite(base, rgba, x, y):
    """Straight-alpha 'over' in sRGB-encoded 8-bit values, the rule the runtime must follow (never in linear light)."""
    h, w = rgba.shape[:2]
    out = base.astype(np.float32)
    ys, xs = slice(max(y, 0), min(y + h, out.shape[0])), slice(max(x, 0), min(x + w, out.shape[1]))
    sp = rgba[ys.start - y:ys.stop - y, xs.start - x:xs.stop - x].astype(np.float32)
    a = sp[..., 3:4] / 255
    out[ys, xs] = out[ys, xs] * (1 - a) + sp[..., :3] * a
    return np.clip(np.round(out), 0, 255).astype(np.uint8)


def layout(unit):
    """Where the two rows sit at 1x: date over time, right edge on WFLD's (571 of 640), just under the castellations."""
    gh = 5 * unit
    x0 = 568 - 7 * CELL * unit - 3 * unit                  # bottom-right of the last glyph at x 568 (+ slant, edge <= 572)
    y_time = 429 - gh                                      # last lit line 428, its edge 429: inside title safe (432)
    row = min(ROW * unit, y_time - 364)                    # the date's first lit line >= 364, 4 lines under the castellations
    return {"x": x0, "date_y": y_time - row, "time_y": y_time, "pitch": CELL * unit, "row": row, "glyph_h": gh}


def logo_layers(scale, call, city, k=0.66, top=66, dx=0):
    """The WTCZ-TV 61 logo as keyed over the bars: the ID slide's ring and numeral (broadcast.id_slide) over the call
    letters, and FAIRVIEW in spaced caps under them, so the key is also a visual legal ID (call letters, then the city
    of licence: 47 CFR 73.1201; WFLD keyed "WFLD · CHICAGO" the same way). Cream with a 1.5 px black edge and a 3 px hard
    shadow, like the slide's own shadow (WFLD's key: about 1.4 px of edge and a 2-3 px shadow at 640). Centred: WFLD's
    sat a little right of centre, but our ring lockup is symmetric and reads as misplaced off the axis.
    Returns fill, edge and shadow coverage at `scale` x 640 x 480."""
    old = B.SS
    B.SS = 2 * scale
    try:
        cv = B.Canvas((0, 0, 0))
        B.id_slide(cv, k, top)
        size = 40 * k
        call_y = top + 196 * k
        cv.text(call, "mich", size, 320, call_y, B.C["cream"], center=True, track=.08)
        fc = B.font(B.F["mich"], size)
        call_w = (sum(fc.getlength(ch) for ch in call) + .08 * size * B.SS * (len(call) - 1)) / B.SS
        cap_bottom = call_y + fc.getbbox("W")[3] / B.SS
        small = size * .50                                 # cap ~10 lines: a logo line, read in the close-up
        fs = B.font(B.F["mich"], small)
        plain = sum(fs.getlength(ch) for ch in city) / B.SS
        track = min(max((call_w - plain) / (small * max(len(city) - 1, 1)), 0), 1.2)
        cap_off = fs.getbbox("F")[1] / B.SS
        cv.text(city, "mich", small, 320, cap_bottom + .55 * size * .75 - cap_off, B.C["cream"], center=True, track=track)
        big = np.asarray(cv.im, np.float32)[..., 1] / B.C["cream"][1]
    finally:
        B.SS = old
    big = np.clip(big, 0, 1)
    ss = 2 * scale
    r = int(round(1.5 * ss))                               # 1.5 px edge, as a disc
    edge = big.copy()
    for oy in range(-r, r + 1):
        for ox in range(-r, r + 1):
            if ox * ox + oy * oy <= r * r + .5:
                edge = np.maximum(edge, np.roll(np.roll(big, oy, 0), ox, 1))
    shadow = np.roll(np.roll(edge, 3 * ss, 0), 3 * ss, 1)

    def down(m):
        im = Image.fromarray(np.clip(m * 255, 0, 255).astype(np.uint8)).resize((W * scale, H * scale), Image.LANCZOS)
        out = np.asarray(im, np.float32) / 255
        return np.roll(out, int(round(dx * scale)), 1)

    return down(big), down(edge), down(shadow)


def strings(spec=None):
    """The station's call letters and city of licence, from 叙事设计's strings (the Ad_03 spec): legal_id = call, channel, city."""
    spec = spec or os.path.join(B.HERE, "spec", "frontrooms_ad_03.json")
    s = json.load(open(spec))["strings"]
    return s["station_call"], s["legal_id"].split(" ", 2)[2]


def bars_plate(scale, logo_dx=0):
    old = B.SS
    B.SS = 2 * scale
    try:
        cv = B.Canvas(B.C["ink"])
        B.bars_eg1(cv)                                     # the same bars as v3.1 (and its LANCZOS edge), byte for byte at 1x
        base = np.asarray(cv.im.resize((W * scale, H * scale), Image.LANCZOS), np.float32)
    finally:
        B.SS = old
    f, e, s = logo_layers(scale, *strings(), dx=logo_dx)
    out = base * (1 - s[..., None])
    out = out * (1 - e[..., None])
    out = out * (1 - f[..., None]) + np.array(B.C["cream"], np.float32) * f[..., None]
    return np.clip(np.round(out), 0, 255).astype(np.uint8), f


def key_clock(plate, sprites, lay, date, time, scale):
    out = plate
    for y, text in ((lay["date_y"], date), (lay["time_y"], time)):
        for i, ch in enumerate(text):
            x = lay["x"] + i * lay["pitch"]
            out = composite(out, sprites[ch], (x - MARGIN) * scale, (y - MARGIN) * scale)
    return out


def ink_box(cov, thresh=.02):
    ys, xs = np.where(cov > thresh)
    return None if len(xs) == 0 else (int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1)


def main(argv):
    args = [a for a in argv[1:]]
    opt = {"--unit": "5", "--logo-dx": "0", "--sample": "12-01-90 03:48:27"}
    pos = []
    while args:
        a = args.pop(0)
        if a in opt:
            opt[a] = args.pop(0)
        else:
            pos.append(a)
    out = pos[0] if pos else os.path.join(B.HERE, "out", "offair_bars")
    unit, dx = int(opt["--unit"]), float(opt["--logo-dx"])
    date, time = opt["--sample"].split()
    os.makedirs(out, exist_ok=True)
    lay = layout(unit)
    report = {}
    for scale, sfx in ((1, ""), (2, "@2x")):
        plate, logo_fill = bars_plate(scale, dx)
        Image.fromarray(plate).save(os.path.join(out, "bars_plate" + sfx + ".png"))
        sprites = {}
        strip = []
        for ch in ORDER:
            rgba, f, e = glyph_sprite(ch, unit, scale)
            sprites[ch] = rgba
            strip.append(rgba)
        Image.fromarray(np.concatenate(strip, axis=1), "RGBA").save(os.path.join(out, "clock_glyphs" + sfx + ".png"))
        sample = key_clock(plate, sprites, lay, date, time, scale)
        Image.fromarray(sample).save(os.path.join(out, "preview_sample" + sfx + ".png"))
        if scale == 1:
            report["logo_ink"] = ink_box(logo_fill)
            report["sprite"] = list(strip[0].shape[1::-1])
            # the clock's ink, from the composited sample (edge included)
            diff = np.abs(sample.astype(int) - plate.astype(int)).sum(2) > 0
            report["clock_ink"] = ink_box(diff.astype(np.float32))
            Image.fromarray(np.asarray(B.tube_preview(Image.fromarray(sample)))).save(os.path.join(out, "preview_tube.png"))
    gh = lay["glyph_h"]
    key = {
        "about": "Off-air bars v3.2 (平面视觉 · 广告, 2026-11-03): the plate plus the runtime key for the live date and clock. "
                 "Spec: Documentation/AD_TYPE_SYSTEM.md §3.6. Generator: Tools/screens/offair.py.",
        "raster": [W, H],
        "plate": {"1x": "bars_plate.png", "2x": "bars_plate@2x.png", "srgb": True},
        "glyphs": {"1x": "clock_glyphs.png", "2x": "clock_glyphs@2x.png", "order": ORDER,
                   "sprite": report["sprite"], "margin": MARGIN, "alpha": "straight", "srgb": True},
        "cells": {"pitch": lay["pitch"], "glyphHeight": gh, "unit": unit,
                  "note": "x, y = the top-left of a row's first cell; sprite i goes at (x + i * pitch - margin, y - margin), "
                          "all at 1x; multiply every number by 2 for @2x"},
        "rows": {
            "date": {"x": lay["x"], "y": lay["date_y"], "format": "MM-dd-90",
                     "source": "the real month and day in 1990; a real Feb 29 shows 02-28-90 (FrontRoomsTerminalContent.GameDate)"},
            "time": {"x": lay["x"], "y": lay["time_y"], "format": "HH:mm:ss",
                     "source": "the device's local time (FrontRoomsTerminalContent.Now), 24-hour, leading zeros"},
        },
        "tick": "Read the clock every frame; redraw a row only when its string changes (once a second for the time). "
                "Hard cut, no blink, no fade, no smoothing.",
        "window": {"from": "02:00", "to": "06:00", "clock": "local",
                   "note": "the station's off-air states play only in this window (AD_TYPE_SYSTEM.md §3.4)"},
        "composite": "In sRGB-encoded 8-bit values, never in linear light: out = plate * (1 - a) + rgb * a, a = alpha / 255. "
                     "To redraw a row, copy that row's rectangle back from the plate, then composite its 8 sprites left to right.",
        "rowRect": {k: [lay["x"] - MARGIN, lay[k + "_y"] - MARGIN, 7 * lay["pitch"] + report["sprite"][0], report["sprite"][1]]
                    for k in ("date", "time")},
        "ink": {"logo": report["logo_ink"], "clock": report["clock_ink"], "titleSafe": list(TITLE_SAFE)},
    }
    json.dump(key, open(os.path.join(out, "offair_key.json"), "w"), indent=2, ensure_ascii=False)
    open(os.path.join(out, "offair_key.json"), "a").write("\n")
    ok = lambda b: b is not None and TITLE_SAFE[0] <= b[0] and TITLE_SAFE[1] <= b[1] and b[2] <= TITLE_SAFE[2] and b[3] <= TITLE_SAFE[3]
    print(f"unit {unit}: date y {lay['date_y']}, time y {lay['time_y']}, x {lay['x']}, pitch {lay['pitch']}, row {lay['row']}")
    print(f"{'ok ' if ok(report['logo_ink']) else 'OUT'} logo ink {report['logo_ink']}")
    print(f"{'ok ' if ok(report['clock_ink']) else 'OUT'} clock ink {report['clock_ink']} (sample {date} {time})")
    print("sprite", report["sprite"], "->", out)


if __name__ == "__main__":
    main(sys.argv)
