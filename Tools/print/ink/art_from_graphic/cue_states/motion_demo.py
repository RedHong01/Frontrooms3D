"""Motion demos for the pattern-native cues, v2 (Red 2026-10-07: every arrow points the way, and the
turned shapes are FITTED to their field instead of cut at the rails). 平面视觉.

Each video is a 2x2 grid of motion treatments for one message state, rendered from the exact WP03
geometry (Tools/print/patterns/hard_edge.py):
  M1 snap    - two quantized beats hidden in lamp flickers (slip, then turn)
  M2 smooth  - the half-drop slides up, then every arrow turns and shrinks to fit together
  M3 wave    - M2's turn travels along the route (FLOW) or converges on the door (HERE)
  M4 ratchet - M3 in three 30-degree clicks, like a relay latching
Usage: /usr/bin/python3 motion_demo.py <outdir> [flow here stop]
"""
import math, os, subprocess, sys, shutil
P = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", "patterns")
sys.path.insert(0, os.path.abspath(P))
import hard_edge as he
from PIL import Image, ImageDraw, ImageFont

PAL = {k: tuple(int(v[i:i + 2], 16) for i in (1, 3, 5)) for k, v in he.PALETTE.items()}
TW, TH, S55 = he.TW, he.TH, he.S
Y_APEX2 = he.LOWER_APEX + he.UNIT2_DROP
Z_APEX2 = 1393.5                       # the print starts at the floor in 1125 mm blocks
def zof(y): return Z_APEX2 + (Y_APEX2 - y)
FPS, DUR = 30, 3.8
WIN = dict(x0=200.0, x1=2950.0, z0=700.0, z1=2100.0, k=0.25)
DOOR = (1575.0, 1000.0)
MARGIN = 6.0                           # clear paper between a fitted arrow and its rails

def ease(p): p = min(1.0, max(0.0, p)); return p * p * (3 - 2 * p)

class Band:                            # one constant-thickness chevron band, parametric so it can flatten
    def __init__(self, ink, x0, x1, xc, y_apex, t, up):
        self.ink, self.x0, self.x1, self.xc, self.ya, self.t, self.up = ink, x0, x1, xc, y_apex, t, up
    def poly(self, slope):             # tile coords, y down
        sg = 1 if self.up else -1
        top = [(self.x0, self.ya + sg * slope * (self.xc - self.x0)), (self.xc, self.ya), (self.x1, self.ya + sg * slope * (self.x1 - self.xc))]
        return top + [(x, y + self.t) for x, y in top][::-1]

class Group:
    """A stack of bands that turns as one piece. kind: 'field' (slips with its unit) or 'motif'."""
    def __init__(self, bands, kind, unit, ky, clip):
        self.bands, self.kind, self.unit, self.ky, self.clip = bands, kind, unit, ky, clip
        ys = [y for b in bands for _, y in b.poly(S55)]
        self.cy_tile = (min(ys) + max(ys)) / 2
        self.cx = (clip[0] + clip[1]) / 2
        self.turned_w = max(ys) - min(ys)                 # its width once turned 90 degrees
        self.base_up = bands[0].up

def build():
    groups, stripes = [], []
    for r in range(5):
        for k in (0, 1):                              # the tiles that cross the window, before and after the slip
            for u in (0, 1):
                dx = r * TW + u * he.UNIT; ky = k * TH
                for x0, x1, ink in he.STRIPES:
                    stripes.append((ink, [(x0 + dx, zof(ky)), (x1 + dx, zof(ky)), (x1 + dx, zof(ky + TH)), (x0 + dx, zof(ky + TH))]))
                # motif band: 4 small down-pointing arrows per tile; the diamonds are not arrows and stay
                depth = S55 * he.BAND_HW; T = sum(t for _, t in he.MOTIF_BANDS)
                bx0, bx1, bxc = he.BAND_X0 + dx, he.BAND_X1 + dx, he.BAND_XC + dx
                for m in range(4):
                    tip = he.MOTIF_EDGE_Y + m * he.PERIOD + depth; y = tip
                    bands = []
                    for ink, t in he.MOTIF_BANDS:
                        bands.append(Band(ink, bx0, bx1, bxc, y, t, up=False)); y += t
                    groups.append(Group(bands, "motif", u, ky, (bx0, bx1)))
                    hw = he.BAND_HW; hh = S55 * hw; cyd = tip - depth
                    stripes.append(("cream", [(bxc - hw, zof(cyd + ky)), (bxc, zof(cyd - hh + ky)), (bxc + hw, zof(cyd + ky)), (bxc, zof(cyd + hh + ky))]))
                    hw2 = he.DROP_HW; hh2 = S55 * hw2; cyp = tip + T + he.DROP_GAP + S55 * hw2
                    stripes.append(("deep", [(bxc - hw2, zof(cyp + ky)), (bxc, zof(cyp - hh2 + ky)), (bxc + hw2, zof(cyp + ky)), (bxc, zof(cyp + hh2 + ky))]))
                # field stack: upper chevron (3 bands) + lower chevron (2 bands); unit 2 is half-dropped
                fx0, fx1, fxc = he.FIELD_X0 + dx, he.FIELD_X1 + dx, he.FIELD_XC + dx
                dy = u * he.UNIT2_DROP
                y = he.UPPER_APEX + dy; ub = []
                for ink, t in he.UPPER_BANDS: ub.append(Band(ink, fx0, fx1, fxc, y, t, True)); y += t
                y = he.LOWER_APEX + dy; lb = []
                for ink, t in he.LOWER_BANDS: lb.append(Band(ink, fx0, fx1, fxc, y, t, True)); y += t
                groups.append(Group(ub, "field", u, ky, (fx0, fx1)))
                groups.append(Group(lb, "field", u, ky, (fx0, fx1)))
    fit = {"field": (he.FIELD_X1 - he.FIELD_X0 - 2 * MARGIN) / max(g.turned_w for g in groups if g.kind == "field"),
           "motif": (he.BAND_X1 - he.BAND_X0 - 2 * 1.5) / max(g.turned_w for g in groups if g.kind == "motif")}
    return groups, stripes, fit

GROUPS, STRIPES, FIT = build()

def direction(state, g):
    if state == "flow": return "right"
    if state == "stop": return "flat"
    return "right" if g.cx < DOOR[0] else "left"

def delay(state, g, per_unit):        # wave stagger, seconds
    if state == "here":
        far = 1400.0
        return max(0.0, (far - abs(g.cx - DOOR[0]))) / 375.0 * per_unit
    return (g.cx - WIN["x0"]) / 375.0 * per_unit

TL = dict(slip0=0.7, slip1=1.2, turn0=1.45, turn_len=0.8)
def progress(variant, state, g, t):
    """(slip 0..1, turn 0..1, dim 0..1)"""
    dim = 0.0
    if variant == "M1":
        sl = 1.0 if t >= 0.95 else 0.0
        tu = 1.0 if t >= 1.75 else 0.0
        for tb in (0.95, 1.75):
            if tb - 0.05 <= t < tb + 0.08: dim = 0.85
        return sl, tu, dim
    sl = ease((t - TL["slip0"]) / (TL["slip1"] - TL["slip0"]))
    d = 0.0 if variant == "M2" else delay(state, g, 0.07)
    tu = ease((t - TL["turn0"] - d) / TL["turn_len"])
    if variant == "M4":
        tu = math.floor(min(tu, 1.0) * 3 + 1e-6) / 3.0
    return sl, tu, dim

def shapes(state, variant, t):
    out = []
    dim_max = 0.0
    for g in GROUPS:
        sl, tu, dim = progress(variant, state, g, t); dim_max = max(dim_max, dim)
        dz = (he.UNIT2_DROP * sl) if (g.kind == "field" and g.unit == 0) else 0.0
        mode = direction(state, g)
        if mode == "flat":
            slope = math.tan(math.radians(55.0 * (1 - tu)))
            for b in g.bands:
                p = b.poly(slope); ys = [y for _, y in p]
                # keep each band's own vertical centre while it flattens
                c0 = b.poly(S55); cy0 = (min(y for _, y in c0) + max(y for _, y in c0)) / 2; cy = (min(ys) + max(ys)) / 2
                out.append((b.ink, [(x, zof(y + g.ky + (cy0 - cy)) + dz) for x, y in p]))
            continue
        # rigid turn about the group centre with a fit-to-field scale, centred in its field
        sgn = 1 if mode == "right" else -1
        base = 1 if g.base_up else -1          # up chevrons turn clockwise to point right; down ones counter-clockwise
        a = math.radians(90.0 * tu) * sgn * base
        s = 1.0 + (FIT[g.kind] - 1.0) * tu
        cx0, cy0 = g.cx, g.cy_tile
        ca, sa = math.cos(a), math.sin(a)
        for b in g.bands:
            pts = []
            for x, y in b.poly(S55):
                rx, ry = (x - cx0), (y - cy0)
                # tile coords are y-down: a positive angle turns clockwise on screen
                X = cx0 + s * (rx * ca - ry * sa); Y = cy0 + s * (rx * sa + ry * ca)
                pts.append((X, zof(Y + g.ky) + dz))
            out.append((b.ink, pts))
    return out, dim_max

_TOP = {}
def top_layer(state, W, H, P, ss, k, x0, x1, z0, z1):
    """Stripes, diamonds and the door never move: drawn once per state as an RGBA layer."""
    if state not in _TOP:
        lay = Image.new("RGBA", (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
        for ink, pts in STRIPES: d.polygon(P(pts), fill=PAL[ink] + (255,))
        if state == "here":
            dx0, dx1 = DOOR[0] - DOOR[1] / 2, DOOR[0] + DOOR[1] / 2; trim = 60; dh = 2134
            for (a, b, c, e, col) in [(dx0 - trim, dx1 + trim, 0, dh + trim, (138, 128, 112)), (dx0, dx1, 0, dh, (201, 194, 180))]:
                d.rectangle([(a - x0) * k * ss, (z1 - min(e, z1)) * k * ss, (b - x0) * k * ss, (z1 - max(c, z0)) * k * ss], fill=col + (255,))
        _TOP[state] = lay
    return _TOP[state]

def draw_cell(state, variant, t, label, font):
    k, x0, x1, z0, z1 = WIN["k"], WIN["x0"], WIN["x1"], WIN["z0"], WIN["z1"]
    ss = 2
    W, H = int((x1 - x0) * k * ss), int((z1 - z0) * k * ss)
    im = Image.new("RGB", (W, H), PAL["ground"]); d = ImageDraw.Draw(im)
    P = lambda pts: [((x - x0) * k * ss, (z1 - z) * k * ss) for x, z in pts]
    sh, dim = shapes(state, variant, t)
    for ink, pts in sh:
        if any(z0 - 400 < z < z1 + 400 for _, z in pts): d.polygon(P(pts), fill=PAL[ink])
    im.paste(top_layer(state, W, H, P, ss, k, x0, x1, z0, z1), (0, 0), top_layer(state, W, H, P, ss, k, x0, x1, z0, z1))
    im = im.resize((W // ss, H // ss), Image.LANCZOS)
    if dim > 0:
        im = Image.blend(im, Image.new("RGB", im.size, (12, 12, 10)), dim)
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, im.width, 30], fill=(10, 10, 10)); d.text((10, 4), label, fill=(244, 223, 59), font=font)
    return im

def _frame(args):
    state, i, tmp, outdir, n = args
    font = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial Bold.ttf", 18)
    labels = {"M1": "M1 SNAP · hidden in lamp flickers", "M2": "M2 SMOOTH · slide, then turn + fit",
              "M3": "M3 WAVE · the turn travels " + ("to the door" if state == "here" else "along the route"),
              "M4": "M4 RATCHET · 30° clicks, travelling"}
    t = i / FPS
    cells = [draw_cell(state, v, t, labels[v], font) for v in ("M1", "M2", "M3", "M4")]
    w, h = cells[0].size
    frame = Image.new("RGB", (w * 2 + 8, h * 2 + 8), (255, 255, 255))
    for j, c in enumerate(cells): frame.paste(c, ((j % 2) * (w + 8), (j // 2) * (h + 8)))
    frame.save(os.path.join(tmp, "f%04d.png" % i))
    if i in (0, int(1.1 * FPS), int(1.9 * FPS), n - 1):
        frame.save(os.path.join(outdir, "%s_key_%.1fs.png" % (state, t)))

def render_video(state, outdir):
    font = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial Bold.ttf", 18)
    labels = {"M1": "M1 SNAP · hidden in lamp flickers", "M2": "M2 SMOOTH · slide, then turn + fit",
              "M3": "M3 WAVE · the turn travels " + ("to the door" if state == "here" else "along the route"),
              "M4": "M4 RATCHET · 30° clicks, travelling"}
    tmp = os.path.join(outdir, "_frames_" + state); shutil.rmtree(tmp, ignore_errors=True); os.makedirs(tmp)
    n = int(DUR * FPS)
    import multiprocessing as mp
    with mp.get_context("fork").Pool(10) as pool:
        pool.map(_frame, [(state, i, tmp, outdir, n) for i in range(n)])
    mp4 = os.path.join(outdir, "cue_motion_%s.mp4" % state)
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", os.path.join(tmp, "f%04d.png"),
                    "-vf", "pad=ceil(iw/2)*2:ceil(ih/2)*2:color=white", "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "20", "-movflags", "+faststart", mp4], check=True)
    shutil.rmtree(tmp)
    return mp4

if __name__ == "__main__":
    out = sys.argv[1]; os.makedirs(out, exist_ok=True)
    states = sys.argv[2:] or ["flow", "here", "stop"]
    print("fit scales", {k: round(v, 3) for k, v in FIT.items()})
    for st in states: print(render_video(st, out))
