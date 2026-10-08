# Contact sheets from the touch playtest's frames (TOUCH_CONTROLS.md §11): labelled rows of crops
# for the verification log and the Figma motion slides. Usage:
#   <venv python> Tools/touch/sheets.py <spec.json> <out.jpg>
# spec: {"font": ttf, "tile_w": px, "rows": [{"title": str, "crop": [x, y, w, h] | null,
#        "tiles": [[png, label], ...]}]}
import json, sys
from PIL import Image, ImageDraw, ImageFont
spec = json.load(open(sys.argv[1]))
out = sys.argv[2]
font = ImageFont.truetype(spec["font"], spec.get("label_px", 18))
tfont = ImageFont.truetype(spec["font"], spec.get("title_px", 20))
tw = spec["tile_w"]; gap = 8; pad = 16; lab = 28; ttl = 34
rows = []
for r in spec["rows"]:
    tiles = []
    for path, label in r["tiles"]:
        im = Image.open(path).convert("RGB")
        if r.get("crop"):
            x, y, w, h = r["crop"]; im = im.crop((x, y, x + w, y + h))
        th = round(im.height * tw / im.width)
        tiles.append((im.resize((tw, th), Image.LANCZOS), label))
    rows.append((r["title"], tiles))
cols = max(len(t) for _, t in rows)
W = pad * 2 + cols * tw + (cols - 1) * gap
W = max([W] + [pad * 2 + int(tfont.getlength(t)) for t, _ in rows])
H = pad + sum(ttl + lab + max(im.height for im, _ in t) + pad for _, t in rows)
sheet = Image.new("RGB", (W, H), (255, 255, 255))
d = ImageDraw.Draw(sheet)
y = pad
for title, tiles in rows:
    d.text((pad, y), title, font=tfont, fill=(10, 10, 10)); y += ttl
    rh = max(im.height for im, _ in tiles)
    for i, (im, label) in enumerate(tiles):
        x = pad + i * (tw + gap)
        d.text((x, y), label, font=font, fill=(10, 10, 10))
        sheet.paste(im, (x, y + lab))
    y += lab + rh + pad
if W > 1920:
    sheet = sheet.resize((1920, round(H * 1920 / W)), Image.LANCZOS)
sheet.save(out, quality=85)
print(out, sheet.size)
