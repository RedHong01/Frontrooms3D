# Contact sheet: for each kit, top/front/side/persp on a light panel, scaled to fit a cell.
import sys, os
from PIL import Image, ImageDraw
src, out = sys.argv[1], sys.argv[2]
kits = sys.argv[3].split(',')
cell = int(sys.argv[4]) if len(sys.argv) > 4 else 300
views = ['top', 'front', 'side', 'persp']
W = cell * 4; H = (cell + 18) * len(kits)
sheet = Image.new('RGB', (W, H), (238, 236, 230))
d = ImageDraw.Draw(sheet)
for r, k in enumerate(kits):
    y0 = r * (cell + 18)
    d.text((4, y0 + 2), k, fill=(0, 0, 0))
    for c, v in enumerate(views):
        p = os.path.join(src, f'{k}_{v}.png')
        if not os.path.exists(p): continue
        im = Image.open(p).convert('RGBA')
        im.thumbnail((cell - 8, cell - 8), Image.LANCZOS)
        bg = Image.new('RGBA', im.size, (238, 236, 230, 255)); bg.alpha_composite(im)
        sheet.paste(bg.convert('RGB'), (c * cell + (cell - im.width) // 2, y0 + 18 + (cell - im.height) // 2))
        d.rectangle([c * cell, y0 + 18, (c + 1) * cell - 1, y0 + 18 + cell - 1], outline=(200, 198, 190))
sheet.save(out)
