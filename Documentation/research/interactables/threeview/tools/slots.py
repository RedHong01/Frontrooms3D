# Ready-to-upload images for our proposal section 2497:3804 (FRONTROOMS · DOORS + WINDOWS · PROPOSAL).
# For each slot in build_index.PROPOSAL_SLOTS: flatten the render on the K-sheet panel colour #EEECE6,
# crop to the slot's aspect around the content (5 % margin, never cutting content), and save at 2x the
# slot size to heroes/slots/<slot name>.png. Upload with upload_assets(nodeIds=[slot node], scaleMode=FILL).
# Run with /usr/bin/python3 (it has PIL + numpy):  python3 slots.py <threeview dir>
import os, sys
import numpy as np
from PIL import Image

TV = sys.argv[1]
PANEL = (0xEE, 0xEC, 0xE6)

# Read the slot table straight out of build_index.py (one source of truth).
src = open(os.path.join(TV, "tools", "build_index.py")).read()
start = src.index("PROPOSAL_SLOTS = [")
end = src.index("]\n", start) + 2
ns = {}
exec(src[start:end], ns)
SLOTS = ns["PROPOSAL_SLOTS"]

out_dir = os.path.join(TV, "heroes", "slots")
os.makedirs(out_dir, exist_ok=True)
for slide, slot, node, rel, action, w, h in SLOTS:
    path = os.path.join(TV, rel)
    if not os.path.exists(path):
        print("missing", rel); continue
    alpha_path = path[:-4] + "_alpha.png"
    im = Image.open(alpha_path if os.path.exists(alpha_path) else path).convert("RGBA")
    a = np.asarray(im)[:, :, 3]
    W, H = im.size
    if a.min() < 250:
        ys, xs = np.nonzero(a > 8)
        x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
        m = int(.06 * max(x1 - x0, y1 - y0))
        x0, x1, y0, y1 = x0 - m, x1 + m, y0 - m, y1 + m
    else:
        x0, y0, x1, y1 = 0, 0, W, H     # opaque scene: keep the framing
    aspect = w / h
    cw, ch = x1 - x0, y1 - y0
    if cw / ch < aspect:
        g = int(round(ch * aspect)) - cw; x0 -= g // 2; x1 += g - g // 2
    else:
        g = int(round(cw / aspect)) - ch; y0 -= g // 2; y1 += g - g // 2
    bg = Image.new("RGBA", im.size, PANEL + (255,)); bg.alpha_composite(im)
    # pad instead of clamping, so the content is never cut
    padL, padT = max(0, -x0), max(0, -y0)
    padR, padB = max(0, x1 - W), max(0, y1 - H)
    if padL or padT or padR or padB:
        big = Image.new("RGBA", (W + padL + padR, H + padT + padB), PANEL + (255,))
        big.paste(bg, (padL, padT)); bg = big
        x0 += padL; x1 += padL; y0 += padT; y1 += padT
    crop = bg.crop((int(x0), int(y0), int(x1), int(y1))).convert("RGB")
    crop = crop.resize((w * 2, h * 2), Image.LANCZOS)
    name = slot.replace("img:", "") + ".png"
    crop.save(os.path.join(out_dir, name))
    print("slot", slot, node, "<-", rel, "box", (int(x0), int(y0), int(x1), int(y1)))
