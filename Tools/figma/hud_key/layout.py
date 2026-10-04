"""Board + twin layout for the HUD KEY section. Input: plan with fits. Output: layout.json
Boards are 1920x1080 deck frames; each cell is a clipping 'view' onto an instance of a
twin component (the 1:1 native copy of one design/hud PNG)."""
import json, sys
plan = json.load(open(sys.argv[1]))
crops = {c["file"].rsplit(".", 1)[0]: c for c in plan["crops"]}
DIRS = [("A_S", "A · S"), ("A_L", "A · L"), ("Achip", "A + chip"), ("B", "B · tag"), ("C", "C · plate")]
BGS = [("lit", "Lit · low rooms"), ("wallpaper", "Wallpaper"), ("office", "Office"), ("dark", "Dark · at the door")]
STATES = [("held", "Held"), ("other", "Other zone"), ("missing", "No key"), ("used", "Used")]
X0, LABW, CW = 72, 176, 1776
boards = []


def view(twin, x, y, w, h, gx, gy, s=1):
    """cell at board (x,y) of size (w,h) showing game-frame point (gx,gy) at its top-left; s = 1 or 2."""
    c = crops[twin]
    bx, by = c["box"][0], c["box"][1]
    return {"twin": twin, "x": x, "y": y, "w": w, "h": h, "dx": (gx - bx) * s, "dy": (gy - by) * s, "s": s}


def hud_y(d):        # top of a view framing the bottom-left HUD row, 1x
    return 906 if d == "B" else 924


# KV02 · held on four backgrounds (1x)
cells, heads = [], []
cw = (CW - LABW - 4 * 24) // 4                      # 376
y = 280
for j, (bg, bl) in enumerate(BGS):
    heads.append({"text": bl, "x": X0 + LABW + 24 + j * (cw + 24), "y": 244})
for i, (d, dl) in enumerate(DIRS):
    heads.append({"text": dl, "x": X0, "y": y + i * 116 + 34})
    for j, (bg, _) in enumerate(BGS):
        cells.append(view("%s_held_%s_1x" % (d, bg), X0 + LABW + 24 + j * (cw + 24), y + i * 116, cw, 92, 56, 888 if d == "B" else 908))
boards.append({"key": "KV02", "cells": cells, "heads": heads})

# KV03 / KV04 · four states (lit, dark)
for key, bg in (("KV03", "lit"), ("KV04", "dark")):
    cells, heads = [], []
    for j, (st, sl) in enumerate(STATES):
        heads.append({"text": sl, "x": X0 + LABW + 24 + j * (cw + 24), "y": 244})
    for i, (d, dl) in enumerate(DIRS):
        heads.append({"text": dl, "x": X0, "y": y + i * 116 + 34})
        for j, (st, _) in enumerate(STATES):
            tw = "%s_state_%s_%s_1x" % (d, st, bg)
            if st == "missing":
                cells.append(view(tw, X0 + LABW + 24 + j * (cw + 24), y + i * 116, cw, 92, 960 - cw / 2, 528))
            else:
                cells.append(view(tw, X0 + LABW + 24 + j * (cw + 24), y + i * 116, cw, 92, 56, 888 if d == "B" else 908))
    boards.append({"key": key, "cells": cells, "heads": heads})

# KV05 · B zones: rows kind x bg, cols colour
cells, heads = [], []
cw3 = (CW - LABW - 3 * 24) // 3
KINDS = [("rect", "Rect · 14"), ("round", "Round · 37"), ("long", "Long · 08")]
COLS = [("red", "Red"), ("blue", "Blue"), ("white", "White")]
for j, (col, cl) in enumerate(COLS):
    heads.append({"text": cl, "x": X0 + LABW + 24 + j * (cw3 + 24), "y": 244})
r = 0
for kind, kl in KINDS:
    for bg in ("lit", "dark"):
        yy = 280 + r * 100
        heads.append({"text": kl + (" · lit" if bg == "lit" else " · dark"), "x": X0, "y": yy + 22})
        for j, (col, _) in enumerate(COLS):
            cells.append(view("B_zone_%s_%s_%s_1x" % (kind, col, bg), X0 + LABW + 24 + j * (cw3 + 24), yy, cw3, 84, 56, 896))
        r += 1
boards.append({"key": "KV05", "cells": cells, "heads": heads})

# KV06 · A+chip zones (3x3 per bg) + C plates
cells, heads = [], []
cwz, chz = 236, 52
for j, (col, cl) in enumerate(COLS):
    heads.append({"text": cl, "x": X0 + LABW + 24 + j * (cwz + 16), "y": 244})
r = 0
for kind, kl in KINDS:
    for bg in ("lit", "dark"):
        yy = 280 + r * 68
        heads.append({"text": kl + (" · lit" if bg == "lit" else " · dark"), "x": X0, "y": yy + 14})
        for j, (col, _) in enumerate(COLS):
            cells.append(view("Achip_zone_%s_%s_%s_1x" % (kind, col, bg), X0 + LABW + 24 + j * (cwz + 16), yy, cwz, chz, 56, 925))
        r += 1
cx0 = X0 + LABW + 24 + 3 * (cwz + 16) + 56
PL = [("dark", "Default · 00"), ("red", "Red · 14"), ("blue", "Blue · 37"), ("white", "White · 08")]
for j, bg in enumerate(("lit", "dark")):
    heads.append({"text": "C · " + bg, "x": cx0 + 176 + 24 + j * (cwz + 16), "y": 244})
for i, (v, vl) in enumerate(PL):
    yy = 280 + i * 68
    heads.append({"text": vl, "x": cx0, "y": yy + 14})
    for j, bg in enumerate(("lit", "dark")):
        cells.append(view("C_zone_%s_%s_1x" % (v, bg), cx0 + 176 + 24 + j * (cwz + 16), yy, cwz, chz, 56, 925))
boards.append({"key": "KV06", "cells": cells, "heads": heads})

# KV07 · label options, 1x lit/dark + 2x lit/dark
cells, heads = [], []
LO = [("level", "Today · LEVEL 0 KEY"), ("bayon", "All Bayon"), ("plex", "Number in Plex Mono"), ("courier", "Number in Courier Prime"), ("code", "Code today")]
w1, w2 = 248, 496
xs = [X0 + LABW + 24, X0 + LABW + 24 + w1 + 24, X0 + LABW + 24 + 2 * (w1 + 24), X0 + LABW + 24 + 2 * (w1 + 24) + w2 + 24]
for x, t in zip(xs, ("Lit · 1x", "Dark · 1x", "Lit · 2x", "Dark · 2x")):
    heads.append({"text": t, "x": x, "y": 244})
for i, (o, ol) in enumerate(LO):
    yy = 280 + i * 128
    heads.append({"text": ol, "x": X0, "y": yy + 46})
    cells.append(view("label_%s_lit_1x" % o, xs[0], yy + 30, w1, 60, 56, 920))
    cells.append(view("label_%s_dark_1x" % o, xs[1], yy + 30, w1, 60, 56, 920))
    cells.append(view("label_%s_lit_1x" % o, xs[2], yy, w2, 120, 56, 920, 2))
    cells.append(view("label_%s_dark_1x" % o, xs[3], yy, w2, 120, 56, 920, 2))
boards.append({"key": "KV07", "cells": cells, "heads": heads})

# KV08 / KV09 · held at 2x (two backgrounds each)
for key, pair in (("KV08", ("lit", "wallpaper")), ("KV09", ("office", "dark"))):
    cells, heads = [], []
    w = (CW - LABW - 24 - 24) // 2
    for j, bg in enumerate(pair):
        heads.append({"text": dict(BGS)[bg] + " · 2x", "x": X0 + LABW + 24 + j * (w + 24), "y": 244})
    for i, (d, dl) in enumerate(DIRS):
        yy = 280 + i * 140
        heads.append({"text": dl, "x": X0, "y": yy + 52})
        for j, bg in enumerate(pair):
            cells.append(view("%s_held_%s_1x" % (d, bg), X0 + LABW + 24 + j * (w + 24), yy, w, 128, 56, (902 if d == "B" else 922), 2))
    boards.append({"key": key, "cells": cells, "heads": heads})

# KV10 · A+chip zones at 2x
cells, heads = [], []
w = (CW - LABW - 3 * 24) // 3
for j, (col, cl) in enumerate(COLS):
    heads.append({"text": cl + " · 2x", "x": X0 + LABW + 24 + j * (w + 24), "y": 244})
r = 0
for kind, kl in KINDS:
    for bg in ("lit", "dark"):
        yy = 280 + r * 112
        heads.append({"text": kl + " · " + bg, "x": X0, "y": yy + 40})
        for j, (col, _) in enumerate(COLS):
            cells.append(view("Achip_zone_%s_%s_%s_1x" % (kind, col, bg), X0 + LABW + 24 + j * (w + 24), yy, w, 104, 56, 924, 2))
        r += 1
boards.append({"key": "KV10", "cells": cells, "heads": heads})

used = {c["twin"] for b in boards for c in b["cells"]}
twins = [k for k in crops if not k.endswith("_2x") and "fullframe" not in k]
print("boards", [(b["key"], len(b["cells"])) for b in boards])
print("twins", len(twins), "unused on boards:", sorted(set(twins) - used))
missing = [c["twin"] for b in boards for c in b["cells"] if c["twin"] not in crops]
print("cells pointing at unknown twins:", missing)
json.dump({"boards": boards, "twins": twins}, open(sys.argv[2], "w"), indent=0)
