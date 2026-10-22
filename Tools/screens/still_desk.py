"""Still desk screens for the maze's Office CRTs (tutorial T3 / 游戏视觉 V4). 平面视觉 · 广告, v2 2026-10-22.

Red approved T3 in the 教程设计 chat (2026-10-22): "T3 静止屏". On a floor, the desk monitors stop playing the ad
and show a still, dim office screen, so the station TV is the only moving, sounding screen (the beacon).

v2 follows the terminal spec, `Documentation/research/computer_ui/20_design.md` (平面视觉 · 信息设计's review, 10-22):
  - §3.5 / AT-T19: an ambient frame never uses the terminal's field colour (VGA blue) or its header/footer layout,
    and any text it shows meets the glance sizes of §2.3 (cap 18 % = 86 px or 25 % = 120 px of 480), or there is
    no text. Body-size text (v1's cap 21) read as "text you can't read", invited a walk-up and faked a READ terminal;
  - §8.2: one raster with the terminal, 640 x 480 logical, a grid of 40 columns x 15 rows of 16 x 32 px, integer
    scales only (desktop 2x = 1280 x 960);
  - the alarm-log frame is dropped (a zone number before numbering is agreed; TROUBLE is the sanctuary lamp's word).
So v2 ships no-text frames: a dark field with a block cursor, or blank. GLANCE frames (one short line at a glance
size) are supported but empty until 叙事设计 supplies era-checked words.

Colour: paper-white phosphor at ~60 % (150, 156, 160) on a near-black field (9, 10, 11). Amber = the TROUBLE lamp,
green = glow-ink guidance, VGA blue = the READ terminal, so idle screens stay neutral. No tube effects baked in.

Usage: python3 still_desk.py [out_dir]   (default: Tools/screens/out/still_desk; FR_PROJECT overrides the project root)
Writes <name>.png (640 x 480) and <name>@2x.png (1280 x 960), plus still_desk_sheet.png.
"""
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.environ.get("FR_PROJECT") or os.path.abspath(os.path.join(HERE, "..", ".."))
FONT = os.path.join(PROJECT, "Assets", "Fonts", "Period1990", "VT323", "VT323-Regular.ttf")
W, H = 640, 480
CW, RH = 16, 32                # the terminal's cell (20_design.md §8.2): 40 x 15 cells on 640 x 480
FIELD = (9, 10, 11)            # a powered tube's black, a touch above 0
INK = (150, 156, 160)          # paper-white phosphor at ~60 % (dim, neutral)
CAP = 0.560                    # VT323 cap height / em, measured from the shipped file (stand-in until C20 picks a face)

# Frames. "cursor": a still block cursor in one cell (col, row). "glance": one short line at a glance cap
# (86 or 120 px), centred, inside RP 27.3 title safe; the words must come from 叙事设计 (era-checked).
SCREENS = {
    "desk_cursor": {"cursor": (5, 2)},          # where a prompt would sit after CLS, prompt text omitted
    "desk_cursor_low": {"cursor": (5, 9)},      # a session left part-way down the screen
    "desk_blank": {},                           # powered, blanked: field only
}
GLANCE = {}                                     # e.g. "desk_word": {"text": "<narrative word>", "cap": 86, "row": 6}


def render(spec, scale):
    ss = 2 * scale                                # supersample, then downsample to the target size
    w, h = W * ss, H * ss
    text = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(text)
    if "cursor" in spec:
        col, row = spec["cursor"]
        x0, x1 = (col * CW + 1) * ss, ((col + 1) * CW - 1) * ss
        y0, y1 = (row * RH + 5) * ss, ((row + 1) * RH - 4) * ss   # the glyph band of the cell, as a block
        d.rectangle([x0, y0, x1 - 1, y1 - 1], fill=255)
    if "text" in spec:
        f = ImageFont.truetype(FONT, int(round(spec["cap"] / CAP * ss)))
        baseline = ((spec["row"] + 1) * RH - 6) * ss if spec.get("row") is not None else (H // 2) * ss
        d.text((w // 2, baseline), spec["text"], font=f, fill=255, anchor="ms")
    a = np.asarray(text, np.float32) / 255.0
    halo = np.asarray(text.filter(ImageFilter.GaussianBlur(3 * ss)), np.float32) / 255.0
    field = np.array(FIELD, np.float32)
    ink = np.array(INK, np.float32)
    out = field[None, None] * (1 - a[..., None]) + ink[None, None] * a[..., None]
    out = out + ink[None, None] * (halo * (1 - a))[..., None] * 0.22   # halo only outside the glyph: the core stays INK
    im = Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))
    return im.resize((W * scale, H * scale), Image.LANCZOS)


def ink_box(im):
    a = np.asarray(im.convert("L"), np.float32)
    ys, xs = np.where(a > 60)
    return (None if not len(xs) else (int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1))


def main(out):
    os.makedirs(out, exist_ok=True)
    tiles = []
    for name, spec in {**SCREENS, **GLANCE}.items():
        im1 = render(spec, 1)
        box = ink_box(im1)
        safe = box is None or (66 <= box[0] and box[2] <= 574 and 48 <= box[1] and box[3] <= 432)
        print(f"{'ok ' if safe else 'OUT'} {name:18s} ink {box}")
        im1.save(os.path.join(out, name + ".png"))
        render(spec, 2).save(os.path.join(out, name + "@2x.png"))
        tiles.append(im1)
    cols = 3
    rows = (len(tiles) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * (W + 16) + 16, rows * (H + 16) + 16), (40, 40, 40))
    for i, t in enumerate(tiles):
        sheet.paste(t, (16 + (i % cols) * (W + 16), 16 + (i // cols) * (H + 16)))
    sheet.save(os.path.join(out, "still_desk_sheet.png"))


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, "out", "still_desk"))
