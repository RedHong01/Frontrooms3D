"""Endless tiling demo (Red 2026-10-07): the paper flows the way its arrows point. 平面视觉.
Up-flow (the chevrons point up) -> the half-drop aligns -> every arrow turns and fits -> the paper
flows left / right -> turns back -> up-flow again. Seamless 8 s loop; left panel turns left,
right panel turns right. Geometry: hard_edge.py via motion_demo.py.
Usage: /usr/bin/python3 tiling_demo.py <outdir>"""
import math, os, sys, shutil, subprocess
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motion_demo as M
from PIL import Image, ImageDraw, ImageFont
he, PAL, S55, zof, Band, Group = M.he, M.PAL, M.S55, M.zof, M.Band, M.Group
TW, TH = he.TW, he.TH
FPS, LOOP = 30, 8.0
WIN = dict(x0=0.0, x1=1500.0, z0=600.0, z1=2100.0, k=0.3)
SPEED = 420.0                                   # mm/s of paper flow

def build():
    groups, statics = [], []
    for r in range(-1, 4):
        for k in range(-1, 3):
            for u in (0, 1):
                dx = r * TW + u * he.UNIT; ky = k * TH
                for x0, x1, ink in he.STRIPES:
                    statics.append((ink, [(x0 + dx, zof(ky)), (x1 + dx, zof(ky)), (x1 + dx, zof(ky + TH)), (x0 + dx, zof(ky + TH))], u))
                depth = S55 * he.BAND_HW; T = sum(t for _, t in he.MOTIF_BANDS)
                bx0, bx1, bxc = he.BAND_X0 + dx, he.BAND_X1 + dx, he.BAND_XC + dx
                for m in range(4):
                    tip = he.MOTIF_EDGE_Y + m * he.PERIOD + depth; y = tip; bands = []
                    for ink, t in he.MOTIF_BANDS: bands.append(Band(ink, bx0, bx1, bxc, y, t, up=False)); y += t
                    groups.append(Group(bands, "motif", u, ky, (bx0, bx1)))
                    hw = he.BAND_HW; hh = S55 * hw; cyd = tip - depth
                    statics.append(("cream", [(bxc - hw, zof(cyd + ky)), (bxc, zof(cyd - hh + ky)), (bxc + hw, zof(cyd + ky)), (bxc, zof(cyd + hh + ky))], u))
                    hw2 = he.DROP_HW; hh2 = S55 * hw2; cyp = tip + T + he.DROP_GAP + S55 * hw2
                    statics.append(("deep", [(bxc - hw2, zof(cyp + ky)), (bxc, zof(cyp - hh2 + ky)), (bxc + hw2, zof(cyp + ky)), (bxc, zof(cyp + hh2 + ky))], u))
                fx0, fx1, fxc = he.FIELD_X0 + dx, he.FIELD_X1 + dx, he.FIELD_XC + dx
                dy = u * he.UNIT2_DROP
                y = he.UPPER_APEX + dy; ub = []
                for ink, t in he.UPPER_BANDS: ub.append(Band(ink, fx0, fx1, fxc, y, t, True)); y += t
                y = he.LOWER_APEX + dy; lb = []
                for ink, t in he.LOWER_BANDS: lb.append(Band(ink, fx0, fx1, fxc, y, t, True)); y += t
                groups.append(Group(ub, "field", u, ky, (fx0, fx1)))
                groups.append(Group(lb, "field", u, ky, (fx0, fx1)))
    return groups, statics
GROUPS, STATICS = build()

# timeline: (slip, turn) progress
def phases(t):
    e = M.ease
    slip = e((t - 2.2) / 0.6) - e((t - 7.0) / 0.6)
    turn = e((t - 2.9) / 1.0) - e((t - 5.8) / 1.0)
    return slip, turn

# paper displacement, integrated, then trimmed so one loop moves a whole number of repeats
def build_path(sign):
    n = int(LOOP * FPS); xs, zs = [0.0], [0.0]
    for i in range(1, n + 1):
        t = (i - 0.5) / FPS
        _, turn = phases(t)
        a = math.radians(90.0 * turn)
        xs.append(xs[-1] + sign * SPEED * math.sin(a) / FPS)
        zs.append(zs[-1] + SPEED * math.cos(a) / FPS)
    Dx, Dz = xs[-1], zs[-1]
    kx = (round(Dx / TW) * TW / Dx) if abs(Dx) > 1 else 1.0      # the half-drop only matches after a whole roll
    kz = round(Dz / TH) * TH / Dz
    return [x * kx for x in xs], [z * kz for z in zs]
PATH = {"left": build_path(-1), "right": build_path(1)}

def frame_shapes(side, i):
    t = i / FPS
    slip, turn = phases(t)
    ox, oz = PATH[side][0][i] % TW, PATH[side][1][i] % TH
    out = []
    sgn = 1 if side == "right" else -1
    for g in GROUPS:
        dz = (he.UNIT2_DROP * slip) if (g.kind == "field" and g.unit == 0) else 0.0
        base = 1 if g.base_up else -1
        a = math.radians(90.0 * turn) * sgn * base
        s = 1.0 + (M.FIT[g.kind] - 1.0) * turn
        ca, sa = math.cos(a), math.sin(a)
        for b in g.bands:
            pts = []
            for x, y in b.poly(S55):
                rx, ry = x - g.cx, y - g.cy_tile
                X = g.cx + s * (rx * ca - ry * sa); Y = g.cy_tile + s * (rx * sa + ry * ca)
                pts.append((X + ox, zof(Y + g.ky) + dz + oz))
            out.append((b.ink, pts))
    stat = [(ink, [(x + ox, z + oz) for x, z in pts]) for ink, pts, u in STATICS]
    return out, stat

def draw_panel(side, i, font):
    k, x0, x1, z0, z1 = WIN["k"], WIN["x0"], WIN["x1"], WIN["z0"], WIN["z1"]
    ss = 2; W, H = int((x1 - x0) * k * ss), int((z1 - z0) * k * ss)
    im = Image.new("RGB", (W, H), PAL["ground"]); d = ImageDraw.Draw(im)
    P = lambda pts: [((x - x0) * k * ss, (z1 - z) * k * ss) for x, z in pts]
    sh, st = frame_shapes(side, i)
    vis = lambda pts: any(x0 - 300 < x < x1 + 300 for x, _ in pts) and any(z0 - 300 < z < z1 + 300 for _, z in pts)
    for ink, pts in sh:
        if vis(pts): d.polygon(P(pts), fill=PAL[ink])
    for ink, pts in st:
        if vis(pts): d.polygon(P(pts), fill=PAL[ink])
    im = im.resize((W // ss, H // ss), Image.LANCZOS)
    d = ImageDraw.Draw(im); d.rectangle([0, 0, im.width, 30], fill=(10, 10, 10))
    d.text((10, 4), "TURNS LEFT · the paper flows left" if side == "left" else "TURNS RIGHT · the paper flows right", fill=(244, 223, 59), font=font)
    return im

def _frame(args):
    i, tmp = args
    font = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial Bold.ttf", 18)
    a, b = draw_panel("left", i, font), draw_panel("right", i, font)
    f = Image.new("RGB", (a.width * 2 + 8, a.height), (255, 255, 255)); f.paste(a, (0, 0)); f.paste(b, (a.width + 8, 0))
    f.save(os.path.join(tmp, "f%04d.png" % i))

if __name__ == "__main__":
    out = sys.argv[1]; os.makedirs(out, exist_ok=True)
    tmp = os.path.join(out, "_frames_tiling"); shutil.rmtree(tmp, ignore_errors=True); os.makedirs(tmp)
    n = int(LOOP * FPS)
    import multiprocessing as mp
    with mp.get_context("fork").Pool(10) as pool: pool.map(_frame, [(i, tmp) for i in range(n)])
    mp4 = os.path.join(out, "wallpaper_tiling_up_to_left_right.mp4")
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", os.path.join(tmp, "f%04d.png"),
                    "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "20", "-movflags", "+faststart", mp4], check=True)
    for t in (1.0, 3.4, 5.0):
        shutil.copy(os.path.join(tmp, "f%04d.png" % int(t * FPS)), os.path.join(out, "tiling_key_%.1fs.png" % t))
    shutil.rmtree(tmp); print(mp4)
