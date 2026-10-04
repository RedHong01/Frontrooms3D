"""Record what 游戏视觉's composites.py draws, without drawing.

Runs their module with the drawing primitives (frame/sprite/text/rect/crosshair/
save_crop/write_png) swapped for recorders, so every PNG in design/hud gets an exact
op list: background, crop box, scale, and each sprite/text/rect with position,
size, colour and opacity. Nothing is written to their folders.
Usage: /usr/bin/python3 plan.py <out.json>
"""
import json, os, struct, sys, types
from pathlib import Path
OUTPATH = sys.argv[1] if len(sys.argv) > 1 else "plan.json"
KD_VALUE = os.environ.get("FRONTROOMS_ICONLIB_ROOT")
if not KD_VALUE:
    raise SystemExit("plan.py needs the external keyicon_design folder; set FRONTROOMS_ICONLIB_ROOT")
KD = Path(KD_VALUE).expanduser().resolve()
if not (KD / "composites.py").is_file():
    raise SystemExit("FRONTROOMS_ICONLIB_ROOT must contain composites.py: " + str(KD))
sys.path.insert(0, str(KD))
src = (KD / "composites.py").read_text(encoding="utf-8")
head, main = src.split('if __name__ == "__main__":', 1)
ns = {"__name__": "composites_rec", "__file__": str(KD / "composites.py")}
exec(compile(head, "composites.py", "exec"), ns)
L = ns["L"]
FONTS = {}
for k in ("BAYON", "MONO", "SERIF", "COURIER_B"):
    if k in ns: FONTS[id(ns[k])] = k

def png_wh(path):
    with open(path, "rb") as f:
        d = f.read(24)
    return struct.unpack(">II", d[16:24])

class Canvas:
    def __init__(self, bg, s):
        self.bg, self.s, self.ops = bg, s, []
    def copy(self):
        c = Canvas(self.bg, self.s); c.ops = list(self.ops); return c

def frame(name, s=1):
    return Canvas(name, s)

def text(img, font, s_text, size, x, baseline, colour, s=1, opacity=1.0, tracking=0.0):
    polys, adv = font.text_polys(s_text, size * s, 0, 0, tracking * s)
    img.ops.append({"op": "text", "font": FONTS[id(font)], "text": s_text, "size": size, "x": x, "baseline": baseline,
                    "colour": colour, "opacity": opacity, "tracking": tracking, "adv": adv / s})
    return adv / s

def sprite(img, name, x, y, s=1, opacity=1.0):
    w, h = png_wh(os.path.join(ns["PNG"], "%s@%dx.png" % (name, s)))
    img.ops.append({"op": "sprite", "name": name, "x": int(round(x * s)) / s, "y": int(round(y * s)) / s, "w": w / s, "h": h / s, "opacity": opacity})
    return w / s

def rect(img, x, y, w, h, colour, s=1, opacity=1.0):
    img.ops.append({"op": "rect", "x": x, "y": y, "w": w, "h": h, "colour": colour, "opacity": opacity})

def crosshair(img, s=1):
    img.ops.append({"op": "crosshair", "x": 960 - 16, "y": 540 - 16, "w": 32, "h": 32})

OUT = []
def save_crop(img, path, s=1, box=(40, 880, 640, 1000)):
    OUT.append({"file": os.path.basename(path), "bg": img.bg, "s": s, "box": list(box), "ops": img.ops})

class LW(types.SimpleNamespace):
    pass
Lw = LW(**{k: getattr(L, k) for k in dir(L) if not k.startswith("__")})
def write_png(path, img, *a, **k):
    OUT.append({"file": os.path.basename(path), "bg": img.bg, "s": img.s, "box": [0, 0, 1920, 1080], "ops": img.ops})
Lw.write_png = write_png
ns.update(frame=frame, text=text, sprite=sprite, rect=rect, crosshair=crosshair, save_crop=save_crop, L=Lw)
# room_type / prompt / compact / full / label_parts / draw_label resolve these globals at call time
sys.argv = ["composites.py", "all"]
ns["__name__"] = "__main__"
exec(compile('if __name__ == "__main__":' + main, "composites.py", "exec"), ns)
consts = {k: ns[k] for k in ("ROW_Y", "LABEL_BASE", "FULL_Y", "PAPER", "MUTED", "ACCENT") if k in ns}
for k in ("COUR_SZ",):
    if k in ns: consts[k] = ns[k]
json.dump({"consts": consts, "crops": OUT}, open(OUTPATH, "w"), indent=0)
from collections import Counter
print(len(OUT), "outputs;", Counter((c["s"], c["bg"]) for c in OUT))
print(Counter(o["op"] for c in OUT for o in c["ops"]))
