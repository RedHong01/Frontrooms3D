# Trim empty panel around the hero renders (both <name>.png and <name>_alpha.png, same box).
# Lineups keep their natural content aspect; every other hero keeps its own aspect (4:3).
import os, sys
import numpy as np
from PIL import Image
H = sys.argv[1]
for f in sorted(os.listdir(H)):
    if not f.endswith("_alpha.png"): continue
    name = f[:-len("_alpha.png")]
    flat = os.path.join(H, name + ".png")
    if not os.path.exists(flat): continue
    a = Image.open(os.path.join(H, f))
    al = np.asarray(a)[:, :, 3]
    ys, xs = np.nonzero(al > 8)
    W, Hh = a.size
    if len(xs) == 0: continue
    x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
    if (x1 - x0) > .97 * W and (y1 - y0) > .97 * Hh:
        print("keep", name); continue
    m = int(.05 * max(x1 - x0, y1 - y0))
    x0, x1, y0, y1 = x0 - m, x1 + m, y0 - m, y1 + m
    if "lineup" not in name:
        aspect = W / Hh
        cw, ch = x1 - x0, y1 - y0
        if cw / ch < aspect:
            grow = int(ch * aspect) - cw; x0 -= grow // 2; x1 += grow - grow // 2
        else:
            grow = int(cw / aspect) - ch; y0 -= grow // 2; y1 += grow - grow // 2
    # shift into the frame, then clamp
    if x0 < 0: x1 -= x0; x0 = 0
    if y0 < 0: y1 -= y0; y0 = 0
    if x1 > W: x0 -= x1 - W; x1 = W
    if y1 > Hh: y0 -= y1 - Hh; y1 = Hh
    x0, y0 = max(0, x0), max(0, y0)
    box = (int(x0), int(y0), int(x1), int(y1))
    for p in (flat, os.path.join(H, f)):
        im = Image.open(p)
        im.crop(box).save(p)
    print("crop", name, (W, Hh), "->", (box[2] - box[0], box[3] - box[1]))
