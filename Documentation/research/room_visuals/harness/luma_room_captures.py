"""sRGB luma stats per frame: mean, p5, p95, share of pixels < 0.08, mean RGB. usage: luma.py <jpg dir>"""
import os, sys
from PIL import Image
d = sys.argv[1]
for f in sorted(os.listdir(d)):
    if not f.startswith("room_") or "contact_sheet" in f or not f.endswith(".jpg"):
        continue
    im = Image.open(os.path.join(d, f)).convert("RGB").resize((480, 270))
    px = list(im.getdata())
    lum = sorted((0.2126 * r + 0.7152 * g + 0.0722 * b) / 255 for r, g, b in px)
    n = len(lum)
    mean = sum(lum) / n
    dark = sum(1 for v in lum if v < .08) / n
    mr = sum(p[0] for p in px) / n / 255; mg = sum(p[1] for p in px) / n / 255; mb = sum(p[2] for p in px) / n / 255
    print(f"{f[5:-4]:34s} mean {mean:.3f}  p5 {lum[int(n*.05)]:.3f}  p95 {lum[int(n*.95)]:.3f}  dark {dark*100:4.1f}%  RGB ({mr:.2f},{mg:.2f},{mb:.2f})")
