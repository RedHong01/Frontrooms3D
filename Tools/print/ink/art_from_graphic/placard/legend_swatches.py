"""Legend swatches for the EGRESS placard (32_pattern_native_hints.md §5b), from 平面视觉.

WP03 'hard_edge' itself, drawn with the exact cue-state geometry of ../cue_states/states.py:
baseline (as printed), FLOW (WAY ON), HERE (EXIT, the pair either side of a door), STOP (NO EXIT).
Scale 1/25 on the placard; window z 915-1665 mm around the guide row (band centre ~1.29 m).
Writes at 6 px/mm of placard:
- <state>.png: the sRGB print;
- <state>_cue.png: white = the guide row's changed band, the legend's glow;
- <state>_cream.png: white = all cream ink, i.e. what option A would make glow, kept for the §8 discussion. Usage: /usr/bin/python3 legend_swatches.py <outdir>
"""
import os, sys
from PIL import Image, ImageDraw
HERE_DIR = os.path.dirname(os.path.abspath(__file__))
src = open(os.path.join(HERE_DIR, "..", "cue_states", "states.py")).read()
# geometry only; the guide row's changed band is tagged "cue" so it can carry the legend's glow
src = src.split("os.makedirs(OUT")[0].replace("OUT=sys.argv[1]", "OUT=None")
src = src.replace('field.append(("grey", X([(fx0, cy-gt/2)', 'field.append(("cue", X([(fx0, cy-gt/2)').replace('field.append(("grey", X(clipx(rb, fx0, fx1))))', 'field.append(("cue", X(clipx(rb, fx0, fx1))))')
exec(compile(src, "states.py", "exec"))
PAL["cue"] = PAL["grey"]
SCALE = 25.0                 # wall mm per placard mm
PX = 6.0                     # placard px per mm
K = PX / SCALE               # px per wall mm
SS = 6                       # supersampling
Z0, Z1 = 915.0, 1665.0
WIN = {"baseline": (75.0, 1200.0, None), "flow": (75.0, 1200.0, None), "stop": (75.0, 1200.0, None),
       "here": (475.0, 2675.0, (1575.0, 1000.0))}


def swatch(state, outdir):
    x0, x1, door = WIN[state]
    W, H = int(round((x1 - x0) * K * SS)), int(round((Z1 - Z0) * K * SS))
    im = Image.new("RGB", (W, H), PAL["ground"]); d = ImageDraw.Draw(im)
    mk = Image.new("L", (W, H), 0); dm = ImageDraw.Draw(mk)
    cu = Image.new("L", (W, H), 0); dc = ImageDraw.Draw(cu)
    P = lambda poly: [((x - x0) * K * SS, (Z1 - z) * K * SS) for x, z in poly]
    for ink, poly in wall_shapes(state, rolls=5, door=door):
        if len(poly) < 3: continue
        d.polygon(P(poly), fill=PAL[ink])
        dm.polygon(P(poly), fill=255 if ink == "cream" else 0)
        dc.polygon(P(poly), fill=255 if ink == "cue" else 0)
    if door:
        dx0, dx1 = door[0] - door[1] / 2, door[0] + door[1] / 2; trim = 60; dh = 2134
        for (a, b, c, e, col) in [(dx0 - trim, dx1 + trim, 0, dh + trim, (138, 128, 112)), (dx0, dx1, 0, dh, (201, 194, 180))]:
            box = [(a - x0) * K * SS, (Z1 - min(e, Z1)) * K * SS, (b - x0) * K * SS, (Z1 - max(c, Z0)) * K * SS]
            d.rectangle(box, fill=col); dm.rectangle(box, fill=0); dc.rectangle(box, fill=0)
        hx = dx1 - 90; hz = 1000
        d.rectangle([(hx - 12 - x0) * K * SS, (Z1 - hz - 12) * K * SS, (hx + 40 - x0) * K * SS, (Z1 - hz + 12) * K * SS], fill=(120, 110, 95))
    w, h = W // SS, H // SS
    im.resize((w, h), Image.LANCZOS).save(os.path.join(outdir, state + ".png"))
    mk.resize((w, h), Image.LANCZOS).save(os.path.join(outdir, state + "_cream.png"))
    cu.resize((w, h), Image.LANCZOS).save(os.path.join(outdir, state + "_cue.png"))
    return (x1 - x0) / SCALE, (Z1 - Z0) / SCALE


if __name__ == "__main__":
    out = sys.argv[1]; os.makedirs(out, exist_ok=True)
    for st in ("baseline", "flow", "here", "stop"):
        print(st, "%.1f x %.1f mm on the placard" % swatch(st, out))
