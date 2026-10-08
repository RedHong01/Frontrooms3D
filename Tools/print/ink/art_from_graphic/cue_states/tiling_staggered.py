"""Staggered tiling (Red 2026-10-07: "更加错落有致…每隔一行共享一个速度坐标系统"). 平面视觉.
The rails (stripes) stay still. The field chevrons flow up their column and the band arrows flow down
theirs, each column on its own speed system. The columns settle into rows like slot reels. Every arrow
turns and fits, then each ROW flows right on its own speed system, passing behind the rails. The rows
settle, the arrows turn back, the half-drop returns, and the loop closes with no seam.
Panels: V1 two systems (every other row/column shares a speed), V2 three systems, V3 four systems
with uneven starts and stops.
Usage: /usr/bin/python3 tiling_staggered.py <outdir>"""
import math, os, sys, shutil, subprocess
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motion_demo as M
from PIL import Image, ImageDraw, ImageFont
he, PAL, S55, zof, Band = M.he, M.PAL, M.S55, M.zof, M.Band
TW, TH, UNIT, PER = he.TW, he.TH, he.UNIT, he.PERIOD
FPS, LOOP = 30, 9.0
WIN = dict(x0=0.0, x1=1500.0, z0=600.0, z1=2100.0, k=0.27)
SYS = {  # per variant: list of (speed factor, start delay, stop delay)
    "V1": [(1.00, 0.00, 0.00), (0.62, 0.15, 0.30)],
    "V2": [(1.00, 0.00, 0.00), (0.75, 0.12, 0.18), (0.50, 0.24, 0.36)],
    "V3": [(1.00, 0.00, 0.10), (0.80, 0.10, 0.35), (0.60, 0.25, 0.00), (0.90, 0.05, 0.22)]}
TITLE = {"V1": "V1 · TWO SPEED SYSTEMS (every other row / column)", "V2": "V2 · THREE SPEED SYSTEMS", "V3": "V3 · FOUR, UNEVEN STARTS AND STOPS"}

def seg(t, t0, t1, D, a=0.45, b=0.55):
    """0 -> D over [t0, t1]: linear speed-up over a, cruise, slow-down over b (reels stopping)."""
    if t <= t0: return 0.0
    if t >= t1: return D
    T = t1 - t0; a = min(a, T / 2); b = min(b, T / 2); v = D / (T - (a + b) / 2); u = t - t0
    if u < a: return v * u * u / (2 * a)
    if u < T - b: return v * (a / 2 + (u - a))
    w = T - u
    return D - v * w * w / (2 * b)

def settle_distance(base, target, period, speed, dur):
    """The whole-period distance closest to speed*dur that lands base on target (mod period)."""
    d0 = (target - base) % period
    n = max(0, round((speed * dur - d0) / period))
    return d0 + n * period

class G:
    def __init__(self, bands, kind, u, r, k, row):
        self.bands, self.kind, self.u, self.r, self.k, self.row = bands, kind, u, r, k, row
        ys = [y for b in bands for _, y in b.poly(S55)]
        self.cy = (min(ys) + max(ys)) / 2; self.cx = (bands[0].x0 + bands[0].x1) / 2
        self.col = r * 2 + u; self.ky = k * TH; self.up = bands[0].up

def build():
    gs, stripes, diamonds = [], [], []
    for r in range(-3, 4):
        for k in range(-2, 5):          # unit 0 climbs ~2.25 m per loop: keep tiles below the window
            for u in (0, 1):
                dx = r * TW + u * UNIT; ky = k * TH
                if k == 0:
                    for x0, x1, ink in he.STRIPES: stripes.append((ink, x0 + dx, x1 + dx))
                depth = S55 * he.BAND_HW; T = sum(t for _, t in he.MOTIF_BANDS)
                bx0, bx1, bxc = he.BAND_X0 + dx, he.BAND_X1 + dx, he.BAND_XC + dx
                for m in range(4):
                    tip = he.MOTIF_EDGE_Y + m * PER + depth; y = tip; bands = []
                    for ink, t in he.MOTIF_BANDS: bands.append(Band(ink, bx0, bx1, bxc, y, t, up=False)); y += t
                    g = G(bands, "motif", u, r, k, k * 4 + m)
                    hw = he.BAND_HW; hh = S55 * hw; cyd = tip - depth
                    hw2 = he.DROP_HW; hh2 = S55 * hw2; cyp = tip + T + he.DROP_GAP + S55 * hw2
                    g.extra = [("cream", [(bxc - hw, cyd), (bxc, cyd - hh), (bxc + hw, cyd), (bxc, cyd + hh)]),
                               ("deep", [(bxc - hw2, cyp), (bxc, cyp - hh2), (bxc + hw2, cyp), (bxc, cyp + hh2)])]
                    gs.append(g)
                fx0, fx1, fxc = he.FIELD_X0 + dx, he.FIELD_X1 + dx, he.FIELD_XC + dx
                dy = u * he.UNIT2_DROP
                y = he.UPPER_APEX + dy; ub = []
                for ink, t in he.UPPER_BANDS: ub.append(Band(ink, fx0, fx1, fxc, y, t, True)); y += t
                y = he.LOWER_APEX + dy; lb = []
                for ink, t in he.LOWER_BANDS: lb.append(Band(ink, fx0, fx1, fxc, y, t, True)); y += t
                gs.append(G(ub, "field", u, r, k, k * 2)); gs.append(G(lb, "field", u, r, k, k * 2 + 1))
    return gs, stripes
GROUPS, STRIPES = build()
# timeline (s)
T_UP0, T_UP1 = 0.3, 2.6          # up-flow, columns settle into rows
T_TURN0, T_TURN = 2.95, 0.6      # each row system turns
T_H0, T_H1 = 3.85, 6.1           # rows flow right
T_BACK0 = 6.35                   # turn back
T_SLIP0, T_SLIP1 = 7.25, 8.6     # the half-drop returns
V_UP, V_DOWN, V_H = 520.0, 330.0, 560.0

def motion(variant, g, t):
    sysl = SYS[variant]; n = len(sysl)
    cf, cs, ce = sysl[g.col % n]                 # column system (vertical phases)
    rf, rs, re = sysl[g.row % n]                 # row system (horizontal phase, turns)
    voff = hoff = 0.0
    if g.kind == "field":
        base = 0.0 if g.u == 1 else 0.0
        target = 562.5 if g.u == 0 else 0.0     # rows form when unit 0 rises half a drop
        t0, t1 = T_UP0 + cs, T_UP1 + ce
        D = settle_distance(0.0, target, TH, V_UP * cf, t1 - t0)
        voff = seg(t, t0, t1, D)
        if g.u == 0:                             # the half-drop returns at the end of the loop
            voff += seg(t, T_SLIP0 + cs, T_SLIP1 + ce * 0.5, 562.5)
        h0, h1 = T_H0 + rs, T_H1 + re
        Dh = settle_distance(0.0, 0.0, TW, V_H * rf, h1 - h0) or TW   # whole rolls: each piece returns to a field of its own unit
        hoff = seg(t, h0, h1, Dh)
    else:
        t0, t1 = T_UP0 + cs, T_UP1 + ce
        voff = -seg(t, t0, t1, settle_distance(0.0, 0.0, PER, V_DOWN * cf, t1 - t0) or PER)
    turn = M.ease((t - T_TURN0 - rs * 0.6) / T_TURN) - M.ease((t - T_BACK0 - rs * 0.6) / T_TURN)
    return voff, hoff, turn

def shapes(variant, t):
    fields, motifs = [], []
    for g in GROUPS:
        voff, hoff, turn = motion(variant, g, t)
        base = 1 if g.up else -1
        a = math.radians(90.0 * turn) * base             # every arrow turns to point right
        s = 1.0 + (M.FIT[g.kind] - 1.0) * turn
        ca, sa = math.cos(a), math.sin(a)
        dst = fields if g.kind == "field" else motifs
        for b in g.bands:
            pts = []
            for x, y in b.poly(S55):
                rx, ry = x - g.cx, y - g.cy
                X = g.cx + s * (rx * ca - ry * sa); Y = g.cy + s * (rx * sa + ry * ca)
                pts.append((X + hoff, zof(Y + g.ky) + voff))
            dst.append((b.ink, pts))
        if g.kind == "motif":
            for ink, poly in g.extra: motifs.append((ink, [(x, zof(y + g.ky) + voff) for x, y in poly]))
    return fields, motifs

def draw_panel(variant, t, font):
    k, x0, x1, z0, z1 = WIN["k"], WIN["x0"], WIN["x1"], WIN["z0"], WIN["z1"]
    ss = 2; W, H = int((x1 - x0) * k * ss), int((z1 - z0) * k * ss)
    im = Image.new("RGB", (W, H), PAL["ground"]); d = ImageDraw.Draw(im)
    P = lambda pts: [((x - x0) * k * ss, (z1 - z) * k * ss) for x, z in pts]
    vis = lambda pts: any(x0 - 400 < x < x1 + 400 for x, _ in pts) and any(z0 - 400 < z < z1 + 400 for _, z in pts)
    fields, motifs = shapes(variant, t)
    for ink, pts in fields:
        if vis(pts): d.polygon(P(pts), fill=PAL[ink])
    # the rails are opaque: field arrows pass behind every non-field strip
    for r in range(-3, 4):
        for u in (0, 1):
            dx = r * TW + u * UNIT
            a, b = (-5.4 + dx - x0) * k * ss, (he.FIELD_X0 + dx - x0) * k * ss
            d.rectangle([a, 0, b, H], fill=PAL["ground"])
    for ink, pts in motifs:
        if vis(pts): d.polygon(P(pts), fill=PAL[ink])
    for ink, a, b in STRIPES: d.rectangle([(a - x0) * k * ss, 0, (b - x0) * k * ss, H], fill=PAL[ink])
    im = im.resize((W // ss, H // ss), Image.LANCZOS)
    d = ImageDraw.Draw(im); d.rectangle([0, 0, im.width, 28], fill=(10, 10, 10)); d.text((8, 4), TITLE[variant], fill=(244, 223, 59), font=font)
    return im

def _frame(args):
    i, tmp = args
    font = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial Bold.ttf", 15)
    t = i / FPS
    ps = [draw_panel(v, t, font) for v in ("V1", "V2", "V3")]
    w, h = ps[0].size
    f = Image.new("RGB", (w * 3 + 16, h), (255, 255, 255))
    for j, p in enumerate(ps): f.paste(p, (j * (w + 8), 0))
    f.save(os.path.join(tmp, "f%04d.png" % i))

if __name__ == "__main__":
    out = sys.argv[1]; os.makedirs(out, exist_ok=True)
    tmp = os.path.join(out, "_frames_stag"); shutil.rmtree(tmp, ignore_errors=True); os.makedirs(tmp)
    n = int(LOOP * FPS)
    import multiprocessing as mp
    with mp.get_context("fork").Pool(10) as pool: pool.map(_frame, [(i, tmp) for i in range(n)])
    mp4 = os.path.join(out, "wallpaper_tiling_staggered_V1_V2_V3.mp4")
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", os.path.join(tmp, "f%04d.png"),
                    "-vf", "pad=ceil(iw/2)*2:ceil(ih/2)*2:color=white", "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "20", "-movflags", "+faststart", mp4], check=True)
    for i in (0, 42, 99, 150, n - 1):
        shutil.copy(os.path.join(tmp, "f%04d.png" % i), os.path.join(out, "stag_key_%d.png" % i))
    shutil.rmtree(tmp); print(mp4)
