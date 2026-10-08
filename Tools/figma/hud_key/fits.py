"""Add Figma (sRGB) opacities to a recorded plan so each crop matches the linear-light
composite on its own background. Usage: fits.py plan.json out.json"""
import json, math, os, re, sys
import numpy as np
from PIL import Image
KD = "/Users/redwang/FrontRoomsVisualWork/keyicon_design"   # 游戏视觉's rebuilt key-icon pipeline (the /private/tmp copy was wiped 2026-10-05)
sys.path.insert(0, KD)
import iconlib as L
PROJ = "/Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01 Individual Game Project/Frontrooms3D"
DES = os.path.join(PROJ, "Documentation/research/ui_key_icon/design")
RV = os.path.join(PROJ, "Documentation/research/room_visuals/images")
BGS = {"lit": os.path.join(RV, "room_map-l0-low_wide.jpg"), "wallpaper": os.path.join(RV, "room_map-l0-standard_wide.jpg"),
       "office": os.path.join(RV, "room_map-office_wide.jpg"),
       "dark": os.path.join(DES, "bg", "dark_59_door_key_open_t0000ms.png"),
       "leaf": os.path.join(DES, "bg", "leaf_48_almond.png")}
FONTS = {"BAYON": "Assets/Resources/Fonts/Bayon-Regular.ttf", "MONO": "Assets/Resources/Fonts/IBMPlexMono-Regular.ttf",
         "SERIF": "Assets/Resources/Fonts/SourceSerif4-Variable.ttf", "COURIER_B": "Assets/Fonts/Period1990/CourierPrime/CourierPrime-Bold.ttf"}
FONTS = {k: L.Font(os.path.join(PROJ, v)) for k, v in FONTS.items()}
lin = lambda c: np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
srgb = lambda c: np.where(c <= 0.0031308, c * 12.92, 1.055 * np.clip(c, 0, None) ** (1 / 2.4) - 0.055)
hexrgb = lambda h: np.array([int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)])
BG = {k: np.asarray(Image.open(p).convert("RGB"), dtype=np.float64) / 255.0 for k, p in BGS.items()}


def svg_layers(name):
    s = open(os.path.join(DES, "svg", name + ".svg")).read()
    out = []
    for m in re.finditer(r'<path id="([^"]+)"([^>]*)/>', s):
        attrs = m.group(2)
        col = re.search(r'(?:fill|stroke)="#([0-9A-Fa-f]{6})"', re.sub(r'fill="none"', '', attrs))
        op = re.search(r' opacity="([0-9.]+)"', attrs)
        out.append({"id": m.group(1), "colour": col.group(1).upper() if col else None, "opacity": float(op.group(1)) if op else 1.0})
    return out


def lsq(B, F, a_lin, a_fig_unit):
    """Best scalar q with sRGB blend alpha q*a_fig_unit matching linear blend alpha a_lin."""
    target = srgb(lin(B) * (1 - a_lin[..., None]) + lin(F) * a_lin[..., None])
    D = a_fig_unit[..., None] * (F - B)
    den = float((D * D).sum())
    return float((D * (target - B)).sum() / den) if den > 1e-9 else None


def text_fit(bg, o):
    f = FONTS[o["font"]]
    polys, adv = f.text_polys(o["text"], o["size"], 0, 0, o.get("tracking", 0))
    bx0, by0, bx1, by1 = L.bbox(polys)
    ox, oy = int(math.floor(o["x"] + bx0)) - 1, int(math.floor(o["baseline"] + by0)) - 1
    w, h = int(math.ceil(bx1 - bx0)) + 3, int(math.ceil(by1 - by0)) + 3
    cov = L.coverage(L.xform(polys, 1.0, 1.0, o["x"] - ox, o["baseline"] - oy), w, h, 1.0, "nonzero")
    B = bg[oy:oy + h, ox:ox + w]
    F = np.broadcast_to(hexrgb(o["colour"]), B.shape)
    q = lsq(B, F, o["opacity"] * cov, cov)
    return min(1.0, q) if q is not None else o["opacity"]


def sprite_fit(bg, o):
    im = np.asarray(Image.open(os.path.join(DES, "png", o["name"] + "@1x.png")).convert("RGBA"), dtype=np.float64) / 255.0
    rgb, al = im[..., :3], im[..., 3]
    x, y = int(round(o["x"])), int(round(o["y"]))
    h, w = al.shape
    B = bg[y:y + h, x:x + w]
    a = o["opacity"]
    lay = svg_layers(o["name"])
    # tag body / tag tab / chip fill sit on the opaque paper rim: flattened to a solid in the component
    semi = [l for l in lay if l["opacity"] < 0.999 and l["id"] not in ("tag body", "tag tab", "chip fill")]
    res = {"g": 1.0, "inner": {}}
    opaque = al >= 0.985
    if a < 0.999 and opaque.any():
        m = opaque
        q = lsq(B[m], rgb[m], a * al[m], al[m])
        res["g"] = min(1.0, q) if q is not None else a
    for l in semi:
        o_n = l["opacity"]
        m = (al > 0.02) & (al < 0.985)
        if not m.any():
            continue
        cov = np.clip(al[m] / o_n, 0, 1)
        F = np.broadcast_to(hexrgb(l["colour"]), B[m].shape)
        q = lsq(B[m], F, a * al[m], cov)
        if q is None:
            continue
        res["inner"][l["id"]] = round(min(1.0, q / res["g"]), 3)
    res["g"] = round(res["g"], 3)
    return res


if __name__ == "__main__":
    plan = json.load(open(sys.argv[1]))
    for c in plan["crops"]:
        bg = BG[c["bg"]].copy()          # grows as opaque/semi-transparent rects (the prompt card) are laid down
        for o in c["ops"]:
            if o["op"] == "text" and o["opacity"] < 0.999:
                o["fig_opacity"] = round(text_fit(bg, o), 3)
            elif o["op"] == "rect":
                x0, y0 = int(round(o["x"])), int(round(o["y"])); w, h = int(round(o["w"])), int(round(o["h"]))
                Bb = bg[y0:y0 + h, x0:x0 + w]
                F = np.broadcast_to(hexrgb(o["colour"]), Bb.shape)
                if o["opacity"] < 0.999:
                    q = lsq(Bb, F, np.full(Bb.shape[:2], o["opacity"]), np.ones(Bb.shape[:2]))
                    o["fig_opacity"] = round(min(1.0, q), 3) if q is not None else o["opacity"]
                a = o["opacity"]
                bg[y0:y0 + h, x0:x0 + w] = srgb(lin(Bb) * (1 - a) + lin(F) * a)    # what later layers sit on
            elif o["op"] == "sprite":
                r = sprite_fit(bg, o)
                if r["g"] < 0.999 or o["opacity"] < 0.999:
                    o["fig_opacity"] = r["g"]
                if r["inner"]:
                    o["fig_inner"] = r["inner"]
    json.dump(plan, open(sys.argv[2], "w"), indent=0)
    import collections
    seen = collections.OrderedDict()
    for c in plan["crops"]:
        if c["s"] != 1: continue
        for o in c["ops"]:
            if "fig_opacity" in o or "fig_inner" in o:
                k = (c["bg"], o.get("name", o.get("font")), o["opacity"])
                seen.setdefault(k, (o.get("fig_opacity"), o.get("fig_inner")))
    for k, v in seen.items(): print(k, v)
