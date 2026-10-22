# DW14 "The kit, as built": crops of the four group heroes for the new slide's slots.
# Same rules as threeview/tools/slots.py (flatten alpha on the K-sheet panel #EEECE6, pad, never cut content),
# but with a margin per tile instead of 6 % of the longer side (the lineups are 4-7:1, where 6 % wastes the slot).
# Usage: W/venv/bin/python dw14_crops.py <threeview dir>
import os, sys
import numpy as np
from PIL import Image

TV = sys.argv[1]
PANEL = (0xEE, 0xEC, 0xE6)
H = os.path.join(TV, "heroes")
OUT = os.path.join(H, "slots")
# slot name, source (heroes/), slot w, h, margin as a fraction of the content width
TILES = [
    ("DW14_DoorSet_Lineup_front", "doors_lineup_pull_face", 1776, 316, .025),
    ("DW14_WindowSet_Lineup_front", "windows_lineup_face_a", 876, 317, .03),
    ("DW14_LockSet_Exploded_persp", "lock_exploded", 426, 317, .06),
    ("DW14_KeySet_Hung_close", "keys_board_hung_close", 426, 317, 0.0),
]
for name, src, w, h, marg in TILES:
    ap = os.path.join(H, src + "_alpha.png")
    im = Image.open(ap if os.path.exists(ap) else os.path.join(H, src + ".png")).convert("RGBA")
    W, Hh = im.size
    a = np.asarray(im)[:, :, 3]
    aspect = w / h
    if a.min() >= 250:          # opaque scene: centre-trim to the slot aspect
        x0, y0, x1, y1 = 0, 0, W, Hh
        if W / Hh > aspect:
            t = W - int(round(Hh * aspect)); x0 += t // 2; x1 -= t - t // 2
        else:
            t = Hh - int(round(W / aspect)); y0 += t // 2; y1 -= t - t // 2
    else:
        ys, xs = np.nonzero(a > 8)
        x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
        m = int(round(marg * (x1 - x0)))
        x0, x1, y0, y1 = x0 - m, x1 + m, y0 - m, y1 + m
        cw, ch = x1 - x0, y1 - y0
        if cw / ch < aspect:
            g = int(round(ch * aspect)) - cw; x0 -= g // 2; x1 += g - g // 2
        else:
            g = int(round(cw / aspect)) - ch; y0 -= g // 2; y1 += g - g // 2
    bg = Image.new("RGBA", im.size, PANEL + (255,)); bg.alpha_composite(im)
    padL, padT = max(0, -x0), max(0, -y0)
    padR, padB = max(0, x1 - W), max(0, y1 - Hh)
    if padL or padT or padR or padB:
        big = Image.new("RGBA", (W + padL + padR, Hh + padT + padB), PANEL + (255,))
        big.paste(bg, (padL, padT)); bg = big
        x0 += padL; x1 += padL; y0 += padT; y1 += padT
    crop = bg.crop((int(x0), int(y0), int(x1), int(y1))).convert("RGB").resize((w * 2, h * 2), Image.LANCZOS)
    crop.save(os.path.join(OUT, name + ".png"))
    print(name, "<-", src, "box", (int(x0) - padL, int(y0) - padT, int(x1) - padL, int(y1) - padT), "->", crop.size)
