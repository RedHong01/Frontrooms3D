"""EXIT chevrons spec diagrams (ch01 two-face rule, ch02 chevron size and light).
Built from exit_sign_geom.py (main, read-only import); no external media.
usage: python make_diagrams.py <assets dir> <out dir>"""
import sys, math, json
sys.dont_write_bytecode = True
sys.path.insert(0, sys.argv[1])
OUT = sys.argv[2]
import numpy as np
from PIL import Image, ImageDraw, ImageFont
import exit_sign_geom as g

PAPER = (244, 242, 236); INK = (26, 26, 26); GREY = (130, 128, 122); LIGHT = (205, 202, 194)
RED = (196, 28, 18); DRED = (110, 30, 24); PLATE = (232, 228, 214); BLUE = (40, 90, 170)
def F(sz, bold=False):
    try:
        return ImageFont.truetype("/System/Library/Fonts/HelveticaNeue.ttc", sz, index=1 if bold else 0)
    except Exception:
        return ImageFont.load_default()

P = g.letter_polys_open()

def chevron_tabs(k):
    poly = [g.open_to_reader(p) for p in P[k][0]]
    n = len(poly)
    return [((poly[i][0] + poly[(i + 1) % n][0]) / 2, (poly[i][1] + poly[(i + 1) % n][1]) / 2) for i in (0, 2, 4)]

def draw_face(d, cx, cy, s, open_side, title, sub):
    """A face as its reader sees it. open_side: '<', '>' or None. s = px per mm."""
    hw, hh = g.HOUSING_W / 2, g.HOUSING_H / 2
    ow, oh = g.OPEN_W / 2, g.OPEN_H / 2
    d.rounded_rectangle([cx - hw * s, cy - hh * s, cx + hw * s, cy + hh * s], radius=int(g.HOUSING_R * s), fill=PLATE, outline=INK, width=3)
    d.rounded_rectangle([cx - ow * s, cy - oh * s, cx + ow * s, cy + oh * s], radius=int(g.OPEN_R * s), outline=GREY, width=2)
    def pt(p):
        a, b = g.open_to_reader(p)
        return (cx + a * s, cy - b * s)
    for k in "EXIT":
        for poly in P[k]:
            d.polygon([pt(p) for p in poly], fill=RED)
    for k in "<>":
        poly = [pt(p) for p in P[k][0]]
        if k == open_side:
            d.polygon(poly, fill=RED)
        else:
            d.line(poly + [poly[0]], fill=GREY, width=2)
    d.text((cx, cy - hh * s - 18), title, font=F(30, True), fill=INK, anchor="md")
    d.text((cx, cy + hh * s + 16), sub, font=F(24), fill=GREY, anchor="ma")

def arrow(d, x0, y0, x1, y1, col, w=5, head=18):
    d.line([(x0, y0), (x1, y1)], fill=col, width=w)
    ang = math.atan2(y1 - y0, x1 - x0)
    for sgn in (1, -1):
        a = ang + math.pi - sgn * math.radians(25)
        d.line([(x1, y1), (x1 + head * math.cos(a), y1 + head * math.sin(a))], fill=col, width=w)

def plan(d, cx, cy, s, hole_x_sign, name):
    """Top view (Unity: +X right, +Z up on the page). hole_x_sign = +1 (ChevL) or -1 (ChevR)."""
    hw, hd = g.HOUSING_W / 2, g.DEPTH_DOUBLE / 2
    d.rectangle([cx - hw * s, cy - hd * s, cx + hw * s, cy + hd * s], fill=PLATE, outline=INK, width=3)
    d.text((cx, cy - hd * s - 150), name, font=F(32, True), fill=INK, anchor="md")
    # the punched end
    ex = cx + hole_x_sign * (hw - 26) * s
    d.rectangle([ex - 13 * s, cy - hd * s - 4, ex + 13 * s, cy + hd * s + 4], fill=RED)
    # pointing direction (both faces)
    arrow(d, cx + hole_x_sign * (hw * s + 20), cy, cx + hole_x_sign * (hw * s + 150), cy, RED, 7, 24)
    d.text((cx + hole_x_sign * (hw * s + 85), cy + 40), "points local %sX" % ("+" if hole_x_sign > 0 else "−"), font=F(24, True), fill=RED, anchor="ma")
    # readers
    fr = "front reader (at +Z) reads " + ("‹EXIT" if hole_x_sign > 0 else "EXIT›")
    br = "back reader (at −Z) reads " + ("EXIT›" if hole_x_sign > 0 else "‹EXIT")
    d.ellipse([cx - 14, cy - hd * s - 110, cx + 14, cy - hd * s - 82], outline=INK, width=3)
    arrow(d, cx, cy - hd * s - 78, cx, cy - hd * s - 30, INK, 3, 12)
    d.text((cx, cy - hd * s - 116), fr, font=F(24), fill=INK, anchor="md")
    d.ellipse([cx - 14, cy + hd * s + 82, cx + 14, cy + hd * s + 110], outline=INK, width=3)
    arrow(d, cx, cy + hd * s + 78, cx, cy + hd * s + 30, INK, 3, 12)
    d.text((cx, cy + hd * s + 118), br, font=F(24), fill=INK, anchor="ma")

def axes(d, x, y, L=90):
    arrow(d, x, y, x + L, y, BLUE, 4, 14); d.text((x + L + 8, y), "+X", font=F(26, True), fill=BLUE, anchor="lm")
    arrow(d, x, y, x, y - L, BLUE, 4, 14); d.text((x, y - L - 8), "+Z (front, face_front)", font=F(26, True), fill=BLUE, anchor="md")
    d.text((x - 6, y + 10), "top view, Unity part space (+Y up, out of the page)", font=F(20), fill=GREY, anchor="la")

# ---------------------------------------------------------------- ch01
W, H = 2600, 1800
im = Image.new("RGB", (W, H), PAPER); d = ImageDraw.Draw(im)
d.text((70, 50), "EXIT chevrons · the two-face rule", font=F(52, True), fill=INK)
d.text((70, 118), "One punched end per sign: both faces point to the same physical side. The suffix names the side in the sign's own frame (front = +Z).", font=F(28), fill=GREY)
s_plan = 1.55
plan(d, 520, 520, s_plan, +1, "_ChevL")
plan(d, 520, 1170, s_plan, -1, "_ChevR")
axes(d, 260, 1700)
s_face = 1.25
fx0, fx1 = 1260, 1760
draw_face(d, fx0, 470, s_face, "<", "_ChevL · front face", "face_front (+Z): ‹EXIT, hole at local +X")
draw_face(d, fx1, 470, s_face, ">", "_ChevL · back face", "face_back (−Z): EXIT›, same hole")
draw_face(d, fx0, 860, s_face, ">", "_ChevR · front face", "face_front (+Z): EXIT›, hole at local −X")
draw_face(d, fx1, 860, s_face, "<", "_ChevR · back face", "face_back (−Z): ‹EXIT, same hole")
d.text((fx0 - 230, 1080), "Grey outline = the unused chevron, a scored knockout left in place (UL 924: unused openings covered).", font=F(22), fill=GREY)
# rule box
rx, ry = 1060, 1140
d.rectangle([rx, ry, W - 70, H - 60], outline=INK, width=3)
d.text((rx + 30, ry + 24), "What the map picks (level design)", font=F(32, True), fill=INK)
lines = [
    "d = unit horizontal world direction from the sign toward the next cell on the exit route",
    "s = Dot(signWorldRotation * Vector3.right, d)",
    "s ≥ +0.707  ->  ..._ChevL        s ≤ −0.707  ->  ..._ChevR",
    "|s| < 0.707  ->  no chevron: turn the sign 90°, or hang the closed Kit_ExitSign_Hanging",
    "The viewer's side never enters the rule. _ChevR = _ChevL turned 180° about Y.",
]
for i, t in enumerate(lines):
    d.text((rx + 30, ry + 80 + i * 40), t, font=F(25, i in (2,)), fill=INK if i != 4 else GREY)
# yaw table
tx, ty = rx + 30, ry + 300
cols = ["Spawn yaw ψ", "faces look", "d = +X", "d = −X", "d = +Z", "d = −Z"]
rows = [["0°", "±Z", "ChevL", "ChevR", "—", "—"], ["90°", "±X", "—", "—", "ChevR", "ChevL"],
        ["180°", "±Z", "ChevR", "ChevL", "—", "—"], ["270°", "±X", "—", "—", "ChevL", "ChevR"]]
cw = [210, 170, 150, 150, 150, 150]
x = tx
for j, c in enumerate(cols):
    d.text((x, ty), c, font=F(23, True), fill=INK); x += cw[j]
for i, r in enumerate(rows):
    x = tx
    for j, c in enumerate(r):
        d.text((x, ty + 38 + i * 34), c, font=F(23), fill=RED if c.startswith("Chev") else INK); x += cw[j]
d.text((tx, ty + 38 + 4 * 34 + 8), "Quaternion.Euler(0, ψ, 0): right = (cos ψ, 0, −sin ψ). World axes, parent unrotated.", font=F(20), fill=GREY)
im.save(OUT + "/ch01_two_face_rule.png")

# ---------------------------------------------------------------- ch02
W, H = 2600, 1500
im = Image.new("RGB", (W, H), PAPER); d = ImageDraw.Draw(im)
d.text((70, 50), "EXIT chevrons · the punched knockout", font=F(52, True), fill=INK)
d.text((70, 118), "The open chevron is the face's own scored knockout, cut through the 0.9 mm plate and lit by the same diffuser and lamp field as the letters.", font=F(28), fill=GREY)
# enlarged left end of the front face of _ChevL, reader frame
s = 4.2
ox, oy = 380, 660     # page position of reader (-173, 0)?? use opening coords
def rp(a, b):  # reader mm -> page
    return (ox + (a + g.HOUSING_W / 2) * s, oy - b * s)
hw, hh = g.HOUSING_W / 2, g.HOUSING_H / 2
x_cut = -g.HOUSING_W / 2 + 120   # show the left 120 mm
d.rectangle([rp(-hw, hh), rp(x_cut, -hh)], fill=PLATE, outline=None)
d.line([rp(-hw, hh), rp(x_cut, hh)], fill=INK, width=3); d.line([rp(-hw, -hh), rp(x_cut, -hh)], fill=INK, width=3); d.line([rp(-hw, hh), rp(-hw, -hh)], fill=INK, width=3)
ow, oh = g.OPEN_W / 2, g.OPEN_H / 2
d.line([rp(-ow, oh), rp(x_cut, oh)], fill=GREY, width=2); d.line([rp(-ow, -oh), rp(x_cut, -oh)], fill=GREY, width=2); d.line([rp(-ow, oh), rp(-ow, -oh)], fill=GREY, width=2)
def opoly(k):
    return [g.open_to_reader(p) for p in P[k][0]]
ch = opoly("<")
d.polygon([rp(*p) for p in ch], fill=RED)
# E (clipped by x_cut is fine; the E starts at -111.5)
for poly in P["E"]:
    pts = [rp(*g.open_to_reader(p)) for p in poly]
    d.polygon(pts, fill=RED)
d.rectangle([rp(x_cut, hh + 5)[0], 165, W, H], fill=PAPER)
# tabs
for (a, b) in chevron_tabs("<"):
    x, y = rp(a, b)
    d.ellipse([x - 7, y - 7, x + 7, y + 7], outline=INK, width=3)
# dimensions
def dim_v(xp, y0, y1, label):
    d.line([(xp, y0), (xp, y1)], fill=INK, width=2)
    for y in (y0, y1): d.line([(xp - 10, y), (xp + 10, y)], fill=INK, width=2)
    d.text((xp - 14, (y0 + y1) / 2), label, font=F(24, True), fill=INK, anchor="rm")
def dim_h(yp, x0, x1, label, below=True):
    d.line([(x0, yp), (x1, yp)], fill=INK, width=2)
    for x in (x0, x1): d.line([(x, yp - 10), (x, yp + 10)], fill=INK, width=2)
    d.text(((x0 + x1) / 2, yp + (12 if below else -12)), label, font=F(24, True), fill=INK, anchor="ma" if below else "md")
xs = [p[0] for p in ch]; ys = [p[1] for p in ch]
dim_v(rp(-hw, 0)[0] - 30, rp(0, max(ys))[1], rp(0, min(ys))[1], "57.15 (2.25 in)")
dim_h(rp(0, -hh)[1] + 40, rp(min(xs), 0)[0], rp(max(xs), 0)[0], "24.6")
e_x0 = g.open_to_reader(P["E"][0][0])[0]
dim_h(rp(0, max(ys))[1] - 40, rp(max(xs), 0)[0], rp(e_x0, 0)[0], "", below=False); d.text((rp(max(xs), 0)[0] - 10, rp(0, max(ys))[1] - 52), "10.0 (UL ≥ 9.5)", font=F(24, True), fill=INK, anchor="rd")
dim_h(rp(0, -hh)[1] + 100, rp(e_x0, 0)[0], rp(e_x0 + g.S, 0)[0], "19.05 letter stroke")
# notes
nx = 1180
notes = [
    ("Geometry (exit_sign_geom.py, unchanged numbers)", True),
    ("Chevron 57.15 high × 24.6 wide (w/h 0.43), arm 11.07 across, stroke 10.0 square", False),
    ("Centred on the letter band; 10.0 from the E / T (UL 924: ≥ 3/8 in)", False),
    ("Outside the legend; the other knockout stays a scored outline", False),
    ("Same polygon as the face texture's score: print and hole cannot drift", False),
    ("", False),
    ("The cut (new)", True),
    ("Through-hole in the 0.9 plate, 0.2 front chamfer, R 0.5 stamped notch", False),
    ("3 broken hold-tab stubs (circles) at the score's tab gaps: 1.5 × 0.35, sharp", False),
    ("The score's hairline is left on the chamfer: the break line", False),
    ("Cut walls carry the enamel (stamped and cut before painting)", False),
]
for i, (t, b) in enumerate(notes):
    d.text((nx, 200 + i * 40), t, font=F(26 if b else 24, b), fill=INK if b else GREY)
# light bars
bx, by = 1180, 720
d.text((bx, by), "Light through each hole (mean of the lamp field, relative to LIT “I”)", font=F(26, True), fill=INK)
data = json.load(open(OUT + "/../tools/field_means.json"))
states = ["LIT", "HALF_A_out", "HALF_B_out", "BATTERY"]
labels = {"LIT": "LIT", "HALF_A_out": "HALF, lamp A out", "HALF_B_out": "HALF, lamp B out", "BATTERY": "BATTERY"}
cols = {"LIT": RED, "HALF_A_out": (214, 120, 100), "HALF_B_out": (150, 70, 60), "BATTERY": (90, 90, 90)}
keys = ["<", "E", "X", "I", "T", ">"]
gw, bwid = 210, 40
base = by + 560
for i, k in enumerate(keys):
    x0 = bx + 40 + i * gw
    for j, st in enumerate(states):
        v = data[k][st] / data["I"]["LIT"]
        h = v * 440
        d.rectangle([x0 + j * bwid, base - h, x0 + j * bwid + bwid - 6, base], fill=cols[st])
    d.text((x0 + 2 * bwid - 3, base + 12), {"<": "‹ (lamp A end)", ">": "› (lamp B end)"}.get(k, k), font=F(24, True), fill=INK, anchor="ma")
d.line([(bx + 30, base), (bx + 40 + 6 * gw, base)], fill=INK, width=2)
for v in (0.25, 0.5, 1.0):
    y = base - v * 440
    d.line([(bx + 30, y), (bx + 40 + 6 * gw, y)], fill=LIGHT, width=1)
    d.text((bx + 24, y), "%.2f" % v, font=F(20), fill=GREY, anchor="rm")
for j, st in enumerate(states):
    d.rectangle([bx + 40 + j * 330, base + 60, bx + 64 + j * 330, base + 84], fill=cols[st])
    d.text((bx + 72 + j * 330, base + 72), labels[st], font=F(22), fill=INK, anchor="lm")
d.text((bx, base + 110), "A chevron is lit by the cavity fill, not a hot spot: 0.22 × the I when LIT, the brightest hole on BATTERY (two 5 W DC lamps sit behind the ends).", font=F(22), fill=GREY)
im.save(OUT + "/ch02_knockout_and_light.png")
print("ok")
