"""Convert the harness PNGs to room_<type>_<view>.jpg (q85, <=1920 wide) and build contact sheets.

usage: python export.py <png dir> <out images dir>
"""
import os
import re
import sys

from PIL import Image, ImageDraw, ImageFont

src, dst = sys.argv[1], sys.argv[2]
os.makedirs(dst, exist_ok=True)

pngs = sorted(f for f in os.listdir(src) if f.endswith(".png"))
out = []
for f in pngs:
    m = re.match(r"^\d+_(.+)\.png$", f)
    if not m:
        continue
    name = "room_" + m.group(1) + ".jpg"
    im = Image.open(os.path.join(src, f)).convert("RGB")
    if im.width > 1920:
        im = im.resize((1920, round(im.height * 1920 / im.width)), Image.LANCZOS)
    im.save(os.path.join(dst, name), "JPEG", quality=85, optimize=True)
    out.append(name)
    print(name)


def font(size):
    for p in ["/System/Library/Fonts/Supplemental/Courier New.ttf", "/System/Library/Fonts/Menlo.ttc", "/System/Library/Fonts/Monaco.ttf"]:
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                pass
    return ImageFont.load_default()


def sheet(names, path, cols=4, tw=480):
    th = tw * 9 // 16
    label = 26
    gap = 8
    rows = (len(names) + cols - 1) // cols
    W = cols * tw + (cols + 1) * gap
    H = rows * (th + label) + (rows + 1) * gap
    s = Image.new("RGB", (W, H), (18, 18, 16))
    d = ImageDraw.Draw(s)
    fnt = font(15)
    for i, n in enumerate(names):
        r, c = divmod(i, cols)
        x = gap + c * (tw + gap)
        y = gap + r * (th + label + gap)
        im = Image.open(os.path.join(dst, n)).convert("RGB").resize((tw, th), Image.LANCZOS)
        s.paste(im, (x, y))
        d.text((x + 2, y + th + 5), n[len("room_"):-len(".jpg")], fill=(225, 220, 200), font=fnt)
    if s.width > 1920:
        s = s.resize((1920, round(s.height * 1920 / s.width)), Image.LANCZOS)
    s.save(path, "JPEG", quality=85, optimize=True)
    print("sheet", path, s.size)


order_map = [n for n in out if n.startswith("room_map-") or n.startswith("room_start")]
order_title = [n for n in out if n.startswith("room_title-")]
sheet(out, os.path.join(dst, "room_contact_sheet_all.jpg"), cols=6, tw=320)
sheet(order_map, os.path.join(dst, "room_contact_sheet_map.jpg"))
sheet(order_title, os.path.join(dst, "room_contact_sheet_title.jpg"))
