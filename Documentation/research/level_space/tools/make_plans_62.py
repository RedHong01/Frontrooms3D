#!/usr/bin/env python3
"""Room-programme plans for research/level_space/62_furniture_l0_office.md
(K research sub-session, line `bathroom`, 2026-10-31).

Top views of the nine room programmes and the optional corridor set, north up,
on a 1 m grid (3 m cell lines darker). Pure Pillow, no downloads.
Run: /Users/redwang/FrontRoomsVisualWork/venv/bin/python make_plans_62.py
"""
import math
import os

from PIL import Image, ImageDraw, ImageFont

OUT = ("/Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01 Individual Game Project/"
       "Frontrooms3D/Documentation/research/level_space/images")
os.makedirs(OUT, exist_ok=True)
FD = "/System/Library/Fonts/Supplemental/"


def F(size, bold=False):
    return ImageFont.truetype(FD + ("Arial Bold.ttf" if bold else "Arial.ttf"), size)


S = 34                      # px per metre
WF = 0.08                   # wall face inset (m)
PAPER = (244, 242, 236)
INK = (32, 32, 32)
GREY = (140, 140, 140)
GRID1 = (226, 224, 218)
GRID3 = (196, 192, 184)
WHITE = (255, 255, 255)
ROLE = {"S": (190, 64, 54), "W": (62, 110, 164), "C": (128, 128, 122)}
TINT = {"S": (236, 196, 190), "W": (196, 214, 234), "C": (222, 222, 218)}
GREEN = (196, 228, 190)
GREEN_EDGE = (96, 160, 92)
ROUTE = (196, 40, 160)
GOLD = (226, 168, 30)
PURPLE = (128, 84, 176)
TER = {
    "raised": (222, 234, 244), "low": (250, 249, 246), "pit": (204, 222, 200),
    "t1": (236, 236, 226), "t2": (224, 226, 212), "t3": (210, 214, 198),
    "platform": (240, 226, 200), "gallery": None,
}

# ---------------------------------------------------------------- helpers


def dashed(d, p0, p1, fill, width=2, dash=7, gap=5):
    (x0, y0), (x1, y1) = p0, p1
    L = math.hypot(x1 - x0, y1 - y0)
    if L == 0:
        return
    ux, uy = (x1 - x0) / L, (y1 - y0) / L
    t = 0.0
    while t < L:
        t1 = min(t + dash, L)
        d.line([(x0 + ux * t, y0 + uy * t), (x0 + ux * t1, y0 + uy * t1)], fill=fill, width=width)
        t = t1 + gap


def dashed_rect(d, box, fill, width=2, dash=7, gap=5):
    x0, y0, x1, y1 = box
    for a, b in (((x0, y0), (x1, y0)), ((x1, y0), (x1, y1)), ((x1, y1), (x0, y1)), ((x0, y1), (x0, y0))):
        dashed(d, a, b, fill, width, dash, gap)


def hatch(d, box, step=6, col=GREY, width=1):
    x0, y0, x1, y1 = box
    h = y1 - y0
    k = x0 - h
    while k < x1:
        xa, xb = max(k, x0), min(k + h, x1)
        d.line([(xa, y1 - (xa - k)), (xb, y1 - (xb - k))], fill=col, width=width)
        k += step


def star(d, cx, cy, r=8, fill=GOLD):
    pts = []
    for i in range(10):
        ang = -math.pi / 2 + i * math.pi / 5
        rr = r if i % 2 == 0 else r * 0.45
        pts.append((cx + rr * math.cos(ang), cy + rr * math.sin(ang)))
    d.polygon(pts, fill=fill, outline=INK)


class Plan:
    """One room drawn in module metres: origin at the room's SW corner, x east, z north."""

    def __init__(self, d, ox, oy, W, D):
        self.d, self.ox, self.oy, self.W, self.D = d, ox, oy, W, D

    def p(self, x, z):
        return (self.ox + x * S, self.oy + (self.D - z) * S)

    def box(self, x, z, w, dd):
        (a, b), (c, e) = self.p(x, z + dd), self.p(x + w, z)
        return (a, b, c, e)

    def grid(self):
        d = self.d
        d.rectangle(self.box(0, 0, self.W, self.D), fill=WHITE)
        for i in range(int(self.W) + 1):
            d.line([self.p(i, 0), self.p(i, self.D)], fill=GRID3 if i % 3 == 0 else GRID1, width=1)
        for j in range(int(self.D) + 1):
            d.line([self.p(0, j), self.p(self.W, j)], fill=GRID3 if j % 3 == 0 else GRID1, width=1)

    def strip(self, edge, cell, kind):
        deep = 1.2 if kind == "door" else 1.0
        a, b = cell * 3.0, cell * 3.0 + 3.0
        if edge == "S":
            bx = (a, WF, b - a, deep)
        elif edge == "N":
            bx = (a, self.D - WF - deep, b - a, deep)
        elif edge == "W":
            bx = (WF, a, deep, b - a)
        else:
            bx = (self.W - WF - deep, a, deep, b - a)
        self.d.rectangle(self.box(*bx), fill=GREEN, outline=GREEN_EDGE, width=1)

    def walls(self, openings):
        d = self.d
        gaps = {"S": [], "N": [], "W": [], "E": []}
        for edge, cell, kind in openings:
            mid = cell * 3.0 + 1.5
            half = 0.5 if kind == "door" else 1.5
            gaps[edge].append((mid - half, mid + half, kind))
        t = max(3, int(round(0.16 * S)))

        def run(edge, length, seg):
            cuts = sorted(gaps[edge])
            pos = 0.0
            for a, b, _k in cuts + [(length, length, None)]:
                if a > pos:
                    seg(pos, a)
                pos = max(pos, b)

        run("S", self.W, lambda a, b: d.line([self.p(a, 0), self.p(b, 0)], fill=INK, width=t))
        run("N", self.W, lambda a, b: d.line([self.p(a, self.D), self.p(b, self.D)], fill=INK, width=t))
        run("W", self.D, lambda a, b: d.line([self.p(0, a), self.p(0, b)], fill=INK, width=t))
        run("E", self.D, lambda a, b: d.line([self.p(self.W, a), self.p(self.W, b)], fill=INK, width=t))
        for edge, lst in gaps.items():
            for a, b, kind in lst:
                if kind != "door":
                    continue
                if edge in "SN":
                    z = 0 if edge == "S" else self.D
                    d.line([self.p(a, z), self.p(a + 0.98, z)], fill=GREEN_EDGE, width=2)
                else:
                    x = 0 if edge == "W" else self.W
                    d.line([self.p(x, a), self.p(x, a + 0.98)], fill=GREEN_EDGE, width=2)

    def terrain(self, items):
        d = self.d
        for it in items:
            k = it["k"]
            if k == "spiral":
                cx, cz, r = it["c"]
                x0, y0 = self.p(cx - r, cz + r)
                x1, y1 = self.p(cx + r, cz - r)
                d.ellipse((x0, y0, x1, y1), fill=(232, 230, 224), outline=INK, width=2)
                px, py = self.p(cx, cz)
                for i in range(1, 4):
                    rr = r * S * i / 4
                    d.arc((px - rr, py - rr, px + rr, py + rr), 200 + i * 30, 520 + i * 30, fill=GREY, width=1)
                d.text((px, py), it.get("t", ""), font=F(10, True), fill=INK, anchor="mm")
                continue
            if k in ("guard", "halfwall"):
                (xa, za), (xb, zb) = it["a"], it["b"]
                if k == "guard":
                    dashed(d, self.p(xa, za), self.p(xb, zb), INK, width=3, dash=5, gap=3)
                else:
                    d.line([self.p(xa, za), self.p(xb, zb)], fill=(90, 86, 80), width=6)
                continue
            if k == "rail":
                (xa, za), (xb, zb) = it["a"], it["b"]
                d.line([self.p(xa, za), self.p(xb, zb)], fill=GREY, width=1)
                continue
            if k == "text":
                d.text(self.p(*it["at"]), it["t"], font=F(10), fill=(96, 92, 84), anchor="mm")
                continue
            bx = self.box(*it["r"])
            if k in ("ramp", "steps"):
                d.rectangle(bx, fill=(238, 236, 230))
                hatch(d, bx, step=5 if k == "steps" else 9, col=(150, 146, 138))
                d.rectangle(bx, outline=(110, 106, 98), width=1)
            elif k == "gallery":
                dashed_rect(d, bx, PURPLE, width=2, dash=9, gap=5)
            else:
                d.rectangle(bx, fill=TER[k])
            if it.get("t"):
                tx, ty = it.get("at", None) or ((bx[0] + bx[2]) / 2, (bx[1] + bx[3]) / 2)
                if it.get("at"):
                    tx, ty = self.p(*it["at"])
                col = PURPLE if k == "gallery" else (96, 92, 84)
                d.text((tx, ty), it["t"], font=F(10, k in ("ramp", "steps")), fill=col, anchor="mm")

    def route(self, lines):
        d = self.d
        for pts in lines:
            for a, b in zip(pts, pts[1:]):
                dashed(d, self.p(*a), self.p(*b), ROUTE, width=2, dash=3, gap=4)

    def piece(self, pc):
        d = self.d
        kind = pc.get("kind", "rect")
        role = pc["role"]
        new = pc.get("new", True)
        col = ROLE[role]
        x, z, w, dd = pc["r"]
        bx = self.box(x, z, w, dd)
        if kind == "frame":
            # see-through barrier: dashed thick line along the long axis, studs as ticks
            if w >= dd:
                zc = z + dd / 2
                a, b = self.p(x, zc), self.p(x + w, zc)
            else:
                xc = x + w / 2
                a, b = self.p(xc, z), self.p(xc, z + dd)
            dashed(d, a, b, col, width=4, dash=4, gap=3)
        elif kind == "wall":
            d.rectangle(bx, fill=col if new else WHITE, outline=col, width=2)
        elif kind == "ceil":
            dashed_rect(d, bx, col, width=2, dash=5, gap=4)
        elif kind == "scatter":
            d.rectangle(bx, fill=(240, 240, 236))
            dashed_rect(d, bx, col, width=1, dash=2, gap=3)
        elif kind in ("tree", "circle"):
            d.ellipse(bx, fill=TINT[role] if new else WHITE, outline=col, width=2)
            if kind == "tree":
                cx, cy = (bx[0] + bx[2]) / 2, (bx[1] + bx[3]) / 2
                r = 0.28 * S
                d.ellipse((cx - r, cy - r, cx + r, cy + r), outline=col, width=2)
            if pc.get("sight"):
                d.ellipse(bx, outline=INK, width=3)
        elif kind == "desk":
            d.rectangle(bx, fill=col if new else WHITE, outline=col, width=1)
        else:
            d.rectangle(bx, fill=TINT[role] if new else WHITE, outline=col, width=2)
            if pc.get("sight"):
                d.rectangle(bx, outline=INK, width=3)
        if pc.get("level") == "g":
            dashed_rect(d, (bx[0] - 2, bx[1] - 2, bx[2] + 2, bx[3] + 2), PURPLE, width=1, dash=3, gap=2)
        n = pc.get("n")
        if n is None or not pc.get("lab", True):
            return
        txt = str(n)
        fnt = F(10, True)
        tw = d.textlength(txt, font=fnt)
        bw, bh = bx[2] - bx[0], bx[3] - bx[1]
        if pc.get("at"):
            tx, ty = self.p(*pc["at"])
            d.text((tx, ty), txt, font=fnt, fill=col, anchor="mm")
        elif bw >= tw + 3 and bh >= 11 and kind not in ("wall", "frame"):
            lum = sum(TINT[role]) / 3 if new else 255
            d.text(((bx[0] + bx[2]) / 2, (bx[1] + bx[3]) / 2), txt, font=fnt, fill=INK if lum > 140 else WHITE,
                   anchor="mm")
        else:
            d.text((bx[2] + 2, bx[1] - 1), txt, font=fnt, fill=col, anchor="ls")

    def event(self, x, z):
        star(self.d, *self.p(x, z))


def run(n, role, x0, z0, w, dd, count, axis="x", new=True, sight=None, lab_first=True, **kw):
    out = []
    for i in range(count):
        x = x0 + (i * w if axis == "x" else 0)
        z = z0 + (i * dd if axis == "z" else 0)
        pc = dict(n=n, role=role, r=(x, z, w, dd), new=new, lab=(i == 0) if lab_first else True)
        if sight is not None:
            pc["sight"] = sight(i) if callable(sight) else sight
        pc.update(kw)
        out.append(pc)
    return out


# ---------------------------------------------------------------- the rooms

P = []

# A. Level 0 vacant suite, 4 x 3, Standard, state midTask (fit-out paused)
A = dict(
    title="L0_VacantSuite  4×3 · Standard 2.9 · midTask (fit-out paused)", W=12, D=9,
    openings=[("S", 1, "door"), ("E", 0, "open"), ("W", 2, "open")],
    terrain=[],
    pieces=[
        dict(n=1, role="S", r=(7.955, 4.6, 0.09, 1.0), kind="frame"),
        dict(n=1, role="S", r=(7.955, 6.51, 0.09, 2.41), kind="frame", lab=False),
        dict(n=1, role="S", r=(8.0, 4.555, 3.92, 0.09), kind="frame", lab=False, at=(10.0, 4.25)),
        dict(n=2, role="S", r=(1.6, 6.9, 2.44, 1.22)),
        dict(n=2, role="S", r=(4.6, 8.57, 1.22, 0.35), sight=True),
        dict(n=3, role="W", r=(4.6, 3.4, 2.44, 1.22)),
        dict(n=4, role="W", r=(9.6, 6.2, 0.56, 1.07)),
        dict(n=5, role="W", r=(6.4, 0.2, 3.66, 0.45)),
        dict(n=6, role="W", r=(1.0, 2.92, 6.0, 0.16), kind="ceil"),
        dict(n=6, role="W", r=(8.5, 7.9, 3.0, 0.16), kind="ceil", lab=False),
        dict(n=7, role="W", r=(0.3, 3.3, 0.62, 0.62), new=False),
        dict(n=7, role="W", r=(0.3, 4.0, 0.62, 0.62), new=False, lab=False),
        dict(n=8, role="C", r=(7.3, 7.6, 0.32, 0.32), new=False),
        dict(n=8, role="C", r=(7.3, 7.05, 0.32, 0.32), new=False, lab=False),
        dict(n=9, role="C", r=(6.75, 7.9, 0.44, 0.29), new=False),
        dict(n=10, role="C", r=(8.25, 7.55, 1.5, 1.0), kind="scatter"),
        dict(n=11, role="C", r=(2.6, 5.2, 0.47, 0.35), new=False),
        dict(n=12, role="C", r=(0.3, 0.3, 0.46, 0.46)),
        dict(n=12, role="C", r=(0.8, 0.3, 0.46, 0.46), lab=False),
        dict(n=13, role="C", r=(2.4, 0.2, 0.3, 0.3), kind="circle"),
    ],
    legend={1: "MetalStudFrame (door gap)", 2: "DrywallStack (flat, leaning)", 3: "PlansTable",
            4: "Stepladder (under open tile)", 5: "CarpetRoll", 6: "TempLightString (ceiling)",
            7: "TransTileBox ×2 high", 8: "TransBucket", 9: "TransPaintTray", 10: "TIScrap",
            11: "TransDropCloth", 12: "RecordsCarton (plain)", 13: "AshUrn"},
    event=(7.75, 7.3), event_t="temp lights lit over a half-taped wall",
    route=[[(4.5, 0.08), (4.5, 2.3), (11.92, 1.5)], [(4.5, 2.3), (2.5, 2.3), (1.0, 5.6), (0.08, 7.5)]],
    notes=["Abandoned face: PowerPole + CableWhips", "on the 3 m grid instead of 1–6."],
)
P.append(A)

# B. Level 0 low storage, 3 x 2, Low 2.4, state midTask
B = dict(
    title="L0_LowStorage  3×2 · Low 2.4 · midTask", W=9, D=6,
    openings=[("W", 0, "door")],
    terrain=[],
    pieces=(
        [dict(n=1, role="S", r=(5.975, 0.08, 0.05, 2.52), kind="frame", at=(6.35, 1.2)),
         dict(n=1, role="S", r=(5.975, 3.6, 0.05, 2.32), kind="frame", lab=False)]
        + run(2, "S", 1.6, 5.46, 0.91, 0.46, 4, sight=True)
        + run(2, "S", 1.6, 0.08, 0.91, 0.46, 3, sight=True, lab=False)
        + run(2, "S", 1.9, 2.3, 0.91, 0.46, 3, sight=True, lab=False)
        + run(2, "S", 1.9, 2.76, 0.91, 0.46, 3, sight=True, lab=False)
        + run(2, "S", 8.46, 0.3, 0.46, 0.91, 2, axis="z", sight=True, lab=False)
        + run(2, "S", 8.46, 3.9, 0.46, 0.91, 2, axis="z", sight=True, lab=False)
        + [dict(n=3, role="W", r=(2.1, 4.9, 0.39, 0.31)),
           dict(n=3, role="W", r=(2.6, 4.9, 0.39, 0.31), lab=False),
           dict(n=3, role="W", r=(3.6, 4.95, 0.39, 0.31), lab=False),
           dict(n=3, role="W", r=(6.8, 1.0, 0.39, 0.31), lab=False),
           dict(n=3, role="W", r=(7.22, 1.0, 0.39, 0.31), lab=False),
           dict(n=3, role="W", r=(6.8, 1.33, 0.39, 0.31), lab=False),
           dict(n=4, role="W", r=(6.6, 4.0, 1.2, 1.0), new=False),
           dict(n=5, role="W", r=(0.3, 3.7, 0.61, 0.61)),
           dict(n=5, role="W", r=(0.3, 4.4, 0.61, 0.61), lab=False),
           dict(n=6, role="C", r=(0.12, 5.25, 0.3, 0.6)),
           dict(n=7, role="W", r=(4.9, 3.9, 0.92, 0.52), new=False),
           dict(n=8, role="W", r=(4.9, 0.8, 0.52, 0.48), new=False),
           dict(n=9, role="W", r=(5.3, 0.1, 0.56, 0.15), at=(5.05, 0.45)),
           dict(n=10, role="W", r=(0.5, 2.94, 8.0, 0.12), kind="ceil", new=False, at=(0.35, 3.3)),
           dict(n=11, role="W", r=(2.0, 1.3, 1.24, 0.25), kind="ceil", new=False),
           dict(n=11, role="W", r=(2.0, 4.2, 1.24, 0.25), kind="ceil", new=False, lab=False),
           dict(n=11, role="W", r=(7.0, 2.6, 1.24, 0.25), kind="ceil", new=False, lab=False)]
    ),
    legend={1: "WireCagePartition (door)", 2: "SteelShelving 1.98 m", 3: "RecordsCarton stacks",
            4: "Pallet", 5: "CarpetRoll (tile cartons)", 6: "TubeCarton", 7: "ChairDolly8",
            8: "HandTruck_Cartons", 9: "Stepladder (folded)", 10: "SprinklerMain_3m (ceiling)",
            11: "WrapFixture_4ft (ceiling)"},
    event=(6.0, 3.1), event_t="cage door open, padlock on the hasp",
    route=[[(0.08, 1.5), (5.5, 1.75), (5.5, 3.1), (6.7, 3.1)], [(1.5, 1.75), (1.35, 3.9)]],
    notes=["Aisles 1.7–2.2 m; shelving rows", "make blind aisles (1.98 m)."],
)
P.append(B)

# C. Computer room on a raised floor, 4 x 3, Standard (+0.30), state midTask
C = dict(
    title="L0_ComputerRoom  4×3 · Standard, floor +0.30 · midTask", W=12, D=9,
    openings=[("S", 0, "door"), ("E", 0, "door")],
    terrain=[
        dict(k="raised", r=(0, 0, 12, 9)),
        dict(k="low", r=(WF, WF, 2.92, 1.2), t="0.00", at=(2.5, 0.6)),
        dict(k="ramp", r=(WF, 1.28, 1.2, 3.6), t="B7", at=(0.68, 3.1)),
        dict(k="steps", r=(1.5, 1.28, 1.4, 0.6), t="B1"),
        dict(k="steps", r=(10.72, 0.9, 0.6, 1.2), t="B1"),
        dict(k="text", t="+0.30 raised floor, lid 2.6", at=(8.9, 4.45)),
    ],
    pieces=(
        run(1, "S", 4.0, 8.18, 0.76, 0.74, 5, sight=True)
        + run(2, "S", 4.0, 5.2, 0.76, 0.86, 7, sight=lambda i: i % 2 == 0)
        + [dict(n=3, role="S", r=(1.9, 6.6, 1.2, 1.25)),
           dict(n=4, role="C", r=(1.2, 6.7, 0.4, 0.3)),
           dict(n=4, role="C", r=(1.2, 7.05, 0.4, 0.3), lab=False),
           dict(n=5, role="W", r=(11.06, 4.0, 0.86, 1.83), sight=True),
           dict(n=6, role="W", r=(5.0, 2.2, 1.52, 0.76), new=False),
           dict(n=7, role="W", r=(5.15, 2.45, 0.38, 0.4), kind="desk", lab=False),
           dict(n=7, role="W", r=(5.9, 2.45, 0.38, 0.4), kind="desk", at=(6.1, 3.25)),
           dict(n=8, role="W", r=(6.7, 2.3, 0.7, 0.5)),
           dict(n=9, role="W", r=(5.45, 1.5, 0.62, 0.65), new=False),
           dict(n=10, role="W", r=(WF, 5.6, 0.38, 1.07), sight=True),
           dict(n=10, role="W", r=(WF, 6.8, 0.38, 1.07), sight=True, lab=False),
           dict(n=11, role="W", r=(8.6, 7.1, 0.91, 0.51)),
           dict(n=12, role="W", r=(3.2, WF, 0.4, 0.1), kind="wall", at=(3.4, 0.45)),
           dict(n=12, role="W", r=(11.82, 3.2, 0.1, 0.4), kind="wall", lab=False),
           dict(n=13, role="W", r=(3.8, WF, 0.38, 0.1), kind="wall", new=False, at=(4.0, 0.45)),
           dict(n=14, role="C", r=(11.12, 8.42, 0.4, 0.4), kind="circle", at=(10.85, 8.62)),
           dict(n=14, role="C", r=(11.52, 8.42, 0.4, 0.4), kind="circle", lab=False),
           dict(n=15, role="C", r=(11.77, 6.4, 0.15, 0.36), kind="wall", at=(11.4, 6.58)),
           dict(n=16, role="W", r=(6.5, WF, 2.44, 0.1), kind="wall", new=False, at=(7.7, 0.45)),
           dict(n=17, role="W", r=(8.2, WF, 0.91, 0.46), sight=True),
           dict(n=18, role="C", r=(9.6, 5.4, 0.61, 0.61))]
    ),
    legend={1: "TapeDrive ×5 (1.70)", 2: "SystemCabinet ×7 (1.52 / 1.83)", 3: "LinePrinter + basket",
            4: "FanfoldBox", 5: "CRACUnit", 6: "OfficeDesk (console)", 7: "DataTerminal ×2",
            8: "DotMatrixPrinter on stand", 9: "TaskChair", 10: "TapeRack", 11: "TapeRack cart",
            12: "EPOStation (each exit)", 13: "FireAnnunciator (halon)", 14: "HalonCylinder ×2",
            15: "ChartRecorder", 16: "InteriorWindow", 17: "SteelShelving (supplies)",
            18: "FloorTileLifted"},
    event=(5.76, 2.95), event_t="console left logged in",
    route=[[(1.5, 0.08), (2.2, 1.6), (3.4, 3.6), (10.0, 3.6), (11.92, 1.5)],
           [(3.4, 3.6), (3.5, 7.0), (8.3, 7.0)]],
    notes=["Front aisles 1.2–1.5 m, rear 1.0–2.1 m;", "tall units block a 1.62 m eye."],
)
P.append(C)

# D. Tiered training room, 4 x 4, Standard, tiers 0.30 per cell row, state midTask
seat_x = [2.95 + t * 1.525 + o for t in range(4) for o in (0.38, 1.14)]
rows = [(1.55, 2.25), (3.05, 3.75), (4.55, 5.25), (6.05, 6.75), (7.55, 8.25)]
Dp = []
for ri, (cz, tz) in enumerate(rows):
    Dp += [dict(n=1, role="W", r=(2.95, tz, 3.05, 0.46), lab=(ri == 0)),
           dict(n=1, role="W", r=(6.0, tz, 3.05, 0.46), lab=False)]
    for si, sx in enumerate(seat_x):
        Dp.append(dict(n=2, role="W", r=(sx - 0.265, cz + 0.08, 0.53, 0.55), lab=(ri == 0 and si == 0)))
Dp += [
    dict(n=4, role="W", r=(5.7, 9.3, 0.61, 0.46), at=(5.45, 9.15)),
    dict(n=3, role="S", r=(5.825, 9.32, 0.36, 0.43), kind="desk", at=(6.5, 9.15)),
    dict(n=5, role="S", r=(5.08, 11.1, 1.85, 0.7)),
    dict(n=6, role="S", r=(8.6, 10.9, 0.61, 0.51)),
    dict(n=7, role="W", r=(1.5, 10.9, 0.7, 0.65)),
    dict(n=8, role="W", r=(9.6, 11.3, 0.61, 0.46)),
    dict(n=9, role="W", r=(2.4, 11.82, 2.44, 0.1), kind="wall", at=(3.6, 11.45)),
    dict(n=10, role="W", r=(3.8, 9.3, 1.52, 0.46)),
    dict(n=10, role="W", r=(1.4, 0.2, 1.52, 0.46), lab=False),
    dict(n=11, role="C", r=(1.6, 0.28, 0.3, 0.3), kind="desk", at=(2.4, 0.95)),
    dict(n=12, role="W", r=(6.3, 0.3, 0.34, 0.34), kind="circle", new=False),
    dict(n=13, role="W", r=(7.5, WF, 0.32, 0.1), kind="wall", new=False, at=(7.66, 0.45)),
]
Dt = [
    dict(k="t1", r=(0, 3, 12, 3)), dict(k="t2", r=(0, 6, 12, 3)), dict(k="t3", r=(0, 9, 12, 3)),
    dict(k="text", t="0.00", at=(10.1, 0.6)),
    dict(k="text", t="−0.30", at=(10.1, 3.4)),
    dict(k="text", t="−0.60", at=(10.1, 6.4)),
    dict(k="text", t="−0.90 front floor", at=(2.4, 9.4)),
]
for zz in (2.7, 5.7, 8.7):
    Dt.append(dict(k="steps", r=(WF, zz, 1.2, 0.3)))
    Dt.append(dict(k="steps", r=(10.72, zz, 1.2, 0.3)))
D_ = dict(
    title="L0_TrainingRoom  4×4 · Standard, 4 levels · midTask", W=12, D=12,
    openings=[("S", 1, "door"), ("E", 3, "door")],
    terrain=Dt, pieces=Dp,
    legend={1: "TrainingTable Row2 (20 tables)", 2: "StackChair (40 seats)", 3: "OverheadProjector",
            4: "AVCart (low)", 5: "ProjectionScreen (tripod)", 6: "Lectern", 7: "FlipChartEasel",
            8: "AVCart (tall) + CRTTV", 9: "MarkerBoard", 10: "TrainingTable (single)",
            11: "CoffeeUrn", 12: "HatStand", 13: "WallClock"},
    event=(6.4, 9.95), event_t="projector still on, slide on the screen",
    route=[[(4.5, 0.08), (4.5, 1.2), (0.7, 1.25), (0.7, 10.2), (11.92, 10.5)],
           [(4.5, 1.2), (11.3, 1.3), (11.3, 10.5)]],
    notes=["Steps (hatched) only in the side aisles;", "0.30 tiers, two table rows per tier."],
)
P.append(D_)

# E. Sunken lounge, 3 x 3, Standard, pit -0.60, state midTask (used)
E = dict(
    title="L0_SunkenLounge  3×3 · Standard, pit −0.60 · midTask", W=9, D=9,
    openings=[("S", 0, "door"), ("E", 2, "open")],
    terrain=[
        dict(k="pit", r=(3, 3, 3, 3), t="−0.60", at=(3.7, 3.25)),
        dict(k="steps", r=(3.0, 4.0, 0.6, 1.6), t="B1"),
        dict(k="steps", r=(4.0, 3.0, 1.6, 0.6), t="B1"),
        dict(k="halfwall", a=(3.0, 6.07), b=(6.07, 6.07)),
        dict(k="halfwall", a=(6.07, 3.0), b=(6.07, 6.07)),
    ],
    pieces=[
        dict(n=1, role="S", r=(3.64, 5.16, 0.76, 0.84)),
        dict(n=1, role="S", r=(4.40, 5.16, 0.76, 0.84), lab=False),
        dict(n=1, role="S", r=(5.16, 5.16, 0.84, 0.84), lab=False),
        dict(n=1, role="S", r=(5.16, 4.40, 0.84, 0.76), lab=False),
        dict(n=1, role="S", r=(5.16, 3.64, 0.84, 0.76), lab=False),
        dict(n=2, role="S", r=(0.3, 7.8, 0.9, 0.9), kind="tree"),
        dict(n=2, role="S", r=(7.8, 0.3, 0.9, 0.9), kind="tree", lab=False),
        dict(n=2, role="S", r=(2.0, 2.0, 0.9, 0.9), kind="tree", lab=False),
        dict(n=3, role="W", r=(3.95, 3.95, 0.91, 0.91)),
        dict(n=4, role="W", r=(5.8, 2.4, 0.3, 0.3), kind="circle"),
        dict(n=4, role="W", r=(2.4, 5.8, 0.3, 0.3), kind="circle", lab=False),
        dict(n=5, role="W", r=(3.4, 6.0, 1.22, 0.25), at=(4.0, 6.5)),
        dict(n=5, role="W", r=(4.7, 6.0, 1.22, 0.25), lab=False),
        dict(n=5, role="W", r=(6.0, 3.4, 0.25, 1.22), lab=False),
        dict(n=5, role="W", r=(6.0, 4.7, 0.25, 1.22), lab=False),
        dict(n=6, role="W", r=(0.3, 3.6, 0.85, 0.85), new=False),
        dict(n=6, role="W", r=(0.3, 5.3, 0.85, 0.85), new=False, lab=False),
        dict(n=7, role="W", r=(0.45, 4.6, 0.56, 0.56), at=(1.45, 4.88)),
        dict(n=8, role="W", r=(4.0, 8.82, 0.61, 0.1), kind="wall", at=(4.3, 8.45)),
        dict(n=9, role="W", r=(8.59, 3.6, 0.33, 0.33), new=False),
        dict(n=10, role="W", r=(6.6, 8.4, 0.39, 0.39), kind="circle", new=False, at=(7.35, 8.6)),
        dict(n=11, role="C", r=(WF, 1.8, 0.1, 0.6), kind="wall", at=(0.45, 2.1)),
    ],
    legend={1: "ModularSofa (corner, armless)", 2: "PlanterTree (drum + canopy)", 3: "CoffeeTableLow (square)",
            4: "AshUrn", 5: "PlanterTrough on the E2 half-wall", 6: "ClubChair",
            7: "CoffeeTableLow (cube) + lamp", 8: "MagazineRack", 9: "WaterCooler", 10: "Torchiere",
            11: "FramedPicture"},
    event=(5.95, 2.95), event_t="cigarette burning on the urn",
    route=[[(1.5, 0.08), (1.5, 1.6), (7.0, 1.6), (7.0, 7.5), (8.92, 7.5)], [(4.8, 1.6), (4.8, 3.3)]],
    notes=["Grey bars: E2 half-wall 1.1 m.", "Canopies screen the eye, not the ray."],
)
P.append(E)

# F. Executive split-level suite, 4 x 3, Tall, state midTask
Fp = [
    dict(n=1, role="S", r=(8.6, 5.6, 1.83, 0.91)),
    dict(n=2, role="S", r=(9.17, 6.6, 0.68, 0.74)),
    dict(n=3, role="S", r=(3.665, 6.865, 1.07, 1.07), kind="circle"),
    dict(n=4, role="W", r=(3.91, 8.0, 0.58, 0.61)),
    dict(n=4, role="W", r=(3.91, 6.2, 0.58, 0.61), lab=False),
    dict(n=4, role="W", r=(2.98, 7.11, 0.61, 0.58), lab=False),
    dict(n=4, role="W", r=(4.81, 7.11, 0.61, 0.58), lab=False),
    dict(n=4, role="W", r=(8.8, 4.6, 0.58, 0.61), lab=False),
    dict(n=4, role="W", r=(9.65, 4.6, 0.58, 0.61), lab=False),
    dict(n=5, role="W", r=(9.2, 8.46, 1.52, 0.46), new=False),
    dict(n=6, role="W", r=(3.6, 1.9, 1.37, 0.71), new=False),
    dict(n=7, role="W", r=(3.9, 2.05, 0.47, 0.39), kind="desk", at=(5.25, 2.2)),
    dict(n=8, role="W", r=(3.97, 2.66, 0.62, 0.66), new=False),
    dict(n=9, role="W", r=(5.0, 3.6, 0.9, 2.0), new=False),
    dict(n=10, role="W", r=(2.9, 3.6, 0.85, 0.85), new=False),
    dict(n=10, role="W", r=(2.9, 4.75, 0.85, 0.85), new=False, lab=False),
    dict(n=10, role="W", r=(1.0, 7.2, 0.85, 0.85), new=False, lab=False, level="g"),
    dict(n=11, role="W", r=(3.95, 4.0, 0.61, 1.22)),
    dict(n=11, role="W", r=(1.95, 7.4, 0.56, 0.56), lab=False, level="g"),
    dict(n=12, role="W", r=(0.3, 1.6, 0.9, 0.9), kind="tree"),
    dict(n=12, role="W", r=(6.3, 5.0, 0.9, 0.9), kind="tree", lab=False),
    dict(n=13, role="W", r=(3.3, 0.3, 0.34, 0.34), kind="circle", new=False),
    dict(n=14, role="W", r=(11.5, 8.46, 0.39, 0.39), kind="circle", new=False, at=(11.2, 8.15)),
    dict(n=15, role="W", r=(11.59, 5.2, 0.33, 0.86), sight=True),
    dict(n=15, role="W", r=(11.59, 6.06, 0.33, 0.86), sight=True, lab=False),
    dict(n=15, role="W", r=(WF, 6.3, 0.33, 0.86), level="g", lab=False),
    dict(n=15, role="W", r=(WF, 7.16, 0.33, 0.86), level="g", lab=False),
    dict(n=15, role="W", r=(WF, 8.02, 0.33, 0.86), level="g", lab=False),
    dict(n=16, role="C", r=(10.85, 8.82, 0.6, 0.1), kind="wall", at=(10.6, 8.55)),
]
Fp[-6]["at"] = (11.1, 5.6)
Ft = [
    dict(k="platform", r=(6.0, 3.0, 6.0, 6.0)),
    dict(k="platform", r=(9.6, 1.8, 1.2, 1.2)),
    dict(k="text", t="+1.20 platform", at=(7.4, 3.45)),
    dict(k="ramp", r=(7.2, 1.8, 2.4, 1.2), t="B2 → +1.20", at=(8.4, 2.4)),
    dict(k="ramp", r=(6.0, 7.72, 3.0, 1.2), t="B2 +1.20 → +2.70", at=(7.5, 8.32)),
    dict(k="gallery", r=(WF, 6.0, 5.92, 2.92), t="+2.70 gallery", at=(2.3, 8.72)),
    dict(k="spiral", c=(1.5, 4.5, 1.0), t="B4"),
    dict(k="guard", a=(6.0, 3.0), b=(6.0, 7.72)),
    dict(k="guard", a=(7.2, 3.0), b=(9.6, 3.0)),
    dict(k="guard", a=(10.8, 3.0), b=(12.0, 3.0)),
    dict(k="guard", a=(2.5, 6.0), b=(6.0, 6.0)),
]
Fs = dict(
    title="L0_SplitSuite  4×3 · Tall 5.4, three levels · midTask", W=12, D=9,
    openings=[("S", 0, "door"), ("E", 0, "door")],
    terrain=Ft, pieces=Fp,
    legend={1: "ExecutiveDesk", 2: "ExecChair", 3: "ConferenceTableRound Ø1.07", 4: "GuestChair ×6",
            5: "Credenza", 6: "DeskPedestal (assistant)", 7: "Typewriter", 8: "TaskChair_Armed",
            9: "Sofa3_Charcoal", 10: "ClubChair (one on gallery)", 11: "CoffeeTableLow (rect, cube)",
            12: "PlanterTree", 13: "HatStand", 14: "Torchiere", 15: "LawBookcase (4-high / gallery 3)",
            16: "FramedPicture"},
    event=(4.4, 2.75), event_t="half-typed letter in the typewriter",
    route=[[(1.5, 0.08), (2.0, 1.0), (11.92, 1.0)],
           [(6.8, 1.0), (7.0, 2.4), (9.6, 2.4), (10.2, 3.4), (11.2, 4.6), (11.2, 7.9), (9.1, 8.1), (6.0, 8.3)]],
    notes=["Purple dash: gallery pieces (+2.70).", "Black dash: E1 guards. Relay: B2 only."],
)
P.append(Fs)

# G. Office cubicle field (existing fill, schematic) + the variety layer, 4 x 4
Gp = []
pods = [(1.6, 2.0), (1.6, 6.6), (7.3, 2.0), (7.3, 6.6)]
for i, (px, pz) in enumerate(pods):
    Gp.append(dict(n=1, role="W", r=(px, pz, 3.10, 3.45), new=False, lab=(i == 0), at=(px + 0.35, pz + 3.1)))
Gextra = []
for (px, pz) in pods:
    zm = pz + 3.45 / 2
    xm = px + 3.10 / 2
    Gextra.append(("line", (px, zm), (px + 3.10, zm)))
    Gextra.append(("line", (xm, pz), (xm, pz + 3.45)))
Gp += [
    dict(n=2, role="W", r=(1.63, 4.0, 1.5, 0.12), at=(2.4, 4.45)),
    dict(n=2, role="W", r=(7.33, 8.6, 1.5, 0.12), lab=False),
    dict(n=2, role="W", r=(8.87, 3.6, 1.5, 0.12), lab=False),
    dict(n=3, role="W", r=(11.46, 4.0, 0.46, 0.91)),
    dict(n=3, role="W", r=(11.46, 4.91, 0.46, 0.91), lab=False),
    dict(n=3, role="W", r=(11.46, 5.82, 0.46, 0.91), lab=False),
    dict(n=4, role="W", r=(11.56, 4.15, 0.25, 1.22), kind="ceil", at=(11.2, 6.95)),
    dict(n=5, role="W", r=(4.62, 3.62, 0.12, 0.12), sight=True, at=(5.0, 3.9)),
    dict(n=5, role="W", r=(4.62, 8.22, 0.12, 0.12), sight=True, lab=False),
    dict(n=5, role="W", r=(7.22, 3.62, 0.12, 0.12), sight=True, lab=False),
    dict(n=5, role="W", r=(7.22, 8.22, 0.12, 0.12), sight=True, lab=False),
    dict(n=6, role="W", r=(2.2, 2.3, 0.47, 0.39), kind="desk", at=(2.4, 2.95)),
    dict(n=7, role="W", r=(8.0, 7.0, 0.6, 0.38), kind="desk", at=(8.3, 7.65)),
    dict(n=8, role="C", r=(WF, 6.0, 0.6, 0.93), new=False),
    dict(n=9, role="C", r=(WF, 3.0, 0.33, 0.33), new=False),
]
for (cx, cz) in [(3.9, 2.6), (2.6, 5.0), (9.6, 2.5), (8.4, 4.8), (3.6, 7.2), (2.4, 9.4), (9.9, 9.5)]:
    Gp.append(dict(n=10, role="C", r=(cx, cz, 0.16, 0.16), kind="desk", lab=False))
Gp[-1]["lab"] = True
Gp[-1]["at"] = (10.4, 9.95)
G = dict(
    title="OF_CubicleField  4×4 · Standard · variety layer on the fill", W=12, D=12,
    openings=[("S", 1, "door"), ("N", 2, "open")],
    terrain=[], pieces=Gp, extra=Gextra,
    legend={1: "existing pods R2×2 (fill unchanged)", 2: "PanelFlipperBin (1 in 3)", 3: "LateralFile run",
            4: "PlanterTrough on the files", 5: "PowerPole at pod ends", 6: "Typewriter (1 in 10)",
            7: "DotMatrixPrinter (1 in 8)", 8: "Copier (wall unit)", 9: "WaterCooler",
            10: "accessories, ≤ 3 per station"},
    event=(4.15, 9.6), event_t="handset off the hook, arm lamp on",
    route=[[(4.5, 0.08), (6.0, 1.5), (6.0, 10.6), (7.5, 11.92)]],
    notes=["Spine 2.6 m, keep-clears and panel", "heights are the fill's (office_and_film/10 §4)."],
)
P.append(G)

# H. Raised trading floor, 4 x 4, Standard (+0.30), state midTask
Hp = []
banks3 = {("A", 0, 1), ("A", 1, 2), ("B", 0, 0), ("B", 1, 3)}
chairs_off = {("A", 0, 3), ("A", 1, 0), ("A", 1, 2), ("B", 0, 1), ("B", 0, 3), ("B", 1, 1), ("B", 1, 2), ("A", 0, 0)}
chair_labelled = [False]
for bench, z0 in (("A", 3.1), ("B", 8.07)):
    for side in (0, 1):
        zs = z0 if side == 0 else z0 + 1.06
        for k in range(4):
            x = 3.56 + k * 1.22
            Hp.append(dict(n=1, role="S", r=(x, zs, 1.22, 0.91), sight=(bench, side, k) in banks3,
                           lab=(bench == "A" and side == 0 and k == 0)))
            tz = zs + 0.02 if side == 0 else zs + 0.91 - 0.32
            Hp.append(dict(n=2, role="S", r=(x + 0.38, tz, 0.46, 0.30), kind="desk",
                           lab=(bench == "A" and side == 0 and k == 3), at=(x + 1.4, zs - 0.2)))
            if (bench, side, k) not in chairs_off:
                cz = zs - 0.75 if side == 0 else zs + 1.01
                Hp.append(dict(n=3, role="W", r=(x + 0.3, cz, 0.62, 0.65), new=False,
                               lab=not chair_labelled[0]))
                chair_labelled[0] = True
Hp += [
    dict(n=4, role="S", r=(4.9, 11.82, 2.25, 0.1), kind="wall", at=(6.0, 11.45)),
    dict(n=5, role="W", r=(WF, 6.0, 0.1, 2.44), kind="wall", at=(0.5, 7.2)),
    dict(n=6, role="W", r=(3.33, 4.05, 0.13, 0.09), new=False, at=(3.05, 4.5)),
    dict(n=6, role="W", r=(8.5, 4.05, 0.13, 0.09), new=False, lab=False),
    dict(n=6, role="W", r=(3.33, 9.03, 0.13, 0.09), new=False, lab=False),
    dict(n=6, role="W", r=(8.5, 9.03, 0.13, 0.09), new=False, lab=False),
    dict(n=7, role="C", r=(8.6, 3.2, 0.36, 0.26), new=False),
    dict(n=8, role="C", r=(5.4, 6.25, 1.2, 0.6), kind="scatter"),
]
Ht = [
    dict(k="raised", r=(0, 0, 12, 12)),
    dict(k="low", r=(WF, WF, 5.92, 1.2), t="0.00", at=(2.2, 0.6)),
    dict(k="ramp", r=(WF, 1.28, 1.2, 3.6), t="B7", at=(0.68, 3.1)),
    dict(k="steps", r=(3.3, 1.28, 2.4, 0.6), t="B1"),
    dict(k="steps", r=(10.72, 7.0, 0.6, 1.2), t="B1"),
    dict(k="text", t="+0.30 raised floor, lid 2.6", at=(9.4, 0.55)),
]
H = dict(
    title="OF_TradingFloor  4×4 · Standard, floor +0.30 · midTask", W=12, D=12,
    openings=[("S", 1, "door"), ("E", 2, "door")],
    terrain=Ht, pieces=Hp,
    legend={1: "TradingDesk ×16 (bold: Bank3)", 2: "TradingTurret", 3: "TaskChair (8 of 16)",
            4: "WorldClocks (north wall)", 5: "TickerBoard (west wall)", 6: "FloorBoxTombstone",
            7: "TrashBin", 8: "trade tickets (floor)"},
    event=(7.83, 10.3), event_t="handset hanging by its cord",
    route=[[(4.5, 0.08), (4.5, 1.6), (2.1, 2.2), (2.1, 6.6), (11.92, 6.6)],
           [(4.5, 1.6), (9.6, 2.2), (9.6, 11.2)]],
    notes=["Positions 1.22 m (4 ft); chair zone 0.9;", "aisles 1.2 m; Bank3 (≈1.86) blocks sight."],
)
P.append(H)

# I. Records room (Office, Standard) — the mezzanine variant stacks the same set on two tiers
Ip = (
    run(1, "S", 3.5, 3.8, 4.57, 0.61, 3, axis="z", sight=True)
    + run(1, "S", 3.5, 6.53, 4.57, 0.61, 3, axis="z", sight=True, lab=False)
    + run(2, "S", 11.61, 2.0, 0.31, 0.91, 4, axis="z", sight=True)
    + run(3, "S", 2.5, WF, 0.91, 0.46, 5)
    + [dict(n=4, role="W", r=(2.7, 0.15, 1.22, 0.25), kind="ceil", at=(3.3, 0.85)),
       dict(n=4, role="W", r=(5.2, 0.15, 1.22, 0.25), kind="ceil", lab=False),
       dict(n=5, role="W", r=(5.0, 5.88, 0.81, 0.41)),
       dict(n=5, role="W", r=(7.4, 0.8, 0.81, 0.41), lab=False),
       dict(n=6, role="W", r=(1.3, 6.6, 0.76, 1.52), new=False),
       dict(n=7, role="W", r=(0.55, 7.05, 0.62, 0.65), new=False),
       dict(n=6, role="W", r=(8.9, 8.16, 1.52, 0.76), new=False, lab=False),
       dict(n=7, role="W", r=(9.35, 7.4, 0.62, 0.65), new=False, lab=False),
       dict(n=8, role="W", r=(9.4, 8.3, 0.4, 0.55), kind="desk", at=(10.75, 8.0)),
       dict(n=9, role="W", r=(11.32, 6.3, 0.6, 0.93), new=False),
       dict(n=10, role="W", r=(11.2, 1.4, 0.45, 0.5), new=False),
       dict(n=11, role="W", r=(8.5, 4.0, 0.39, 0.31)),
       dict(n=11, role="W", r=(8.5, 4.4, 0.39, 0.31), lab=False),
       dict(n=11, role="W", r=(8.95, 4.0, 0.39, 0.31), lab=False)]
)
It = [dict(k="rail", a=(x, 3.75), b=(x, 8.42)) for x in (3.8, 5.8, 7.8)]
I_ = dict(
    title="OF_RecordsRoom  4×3 · Standard · midTask", W=12, D=9,
    openings=[("W", 1, "door"), ("S", 3, "open")],
    terrain=It, pieces=Ip,
    legend={1: "MobileShelving ×6 (one aisle open)", 2: "OpenShelfFile (colour tabs)",
            3: "LateralFile 4-drawer ×5", 4: "PlanterTrough on the files", 5: "FileCart",
            6: "OfficeDesk (clerk, fiche table)", 7: "TaskChair", 8: "MicroficheReader", 9: "Copier",
            10: "StepStool", 11: "RecordsCarton stacks"},
    event=(5.4, 6.33), event_t="one aisle open, a cart inside",
    route=[[(0.08, 4.5), (2.9, 4.5), (2.9, 1.6), (10.5, 1.6), (10.5, 0.08)]],
    notes=["Mezzanine (Tall 4×3): the same set on two", "tiers, deck +2.70, spiral + B2 (R6)."],
)
P.append(I_)

# J. Corridor and elevator lobby identity set (optional), 15 x 3
J = dict(
    title="L0_CorridorIdentity (optional set)  corridor 1×5 · Standard", W=15, D=3,
    openings=[("W", 0, "open"), ("E", 0, "open")],
    terrain=[],
    pieces=[
        dict(n=1, role="W", r=(2.37, -0.42, 0.46, 0.48)),
        dict(n=2, role="W", r=(10.5, WF - 0.02, 0.61, 0.1), kind="wall", at=(10.8, 0.5)),
        dict(n=3, role="W", r=(4.6, 2.84, 0.61, 0.1), kind="wall", at=(4.9, 2.45)),
        dict(n=4, role="C", r=(9.3, 2.84, 0.38, 0.1), kind="wall", at=(9.5, 2.45)),
        dict(n=5, role="W", r=(5.4, 2.4, 0.3, 0.3), kind="circle"),
        dict(n=6, role="W", r=(12.6, 1.95, 0.9, 0.9), kind="tree"),
        dict(n=7, role="W", r=(7.2, WF - 0.02, 0.24, 0.1), kind="wall", new=False, at=(7.3, 0.5)),
    ],
    legend={1: "DrinkingFountain (alcove)", 2: "FireHoseCabinet", 3: "DirectoryBoard", 4: "MailChute",
            5: "AshUrn by the elevators", 6: "PlanterTree", 7: "ExtinguisherCabinet"},
    event=(4.9, 2.2), event_t="directory: one suite's line blank",
    route=[[(0.0, 1.5), (15.0, 1.3)]],
    notes=[],
    lifts=[(6.2, 7.4), (7.8, 9.0)],
    alcove=(2.0, 3.2, 0.5),
)

# ---------------------------------------------------------------- page

COLW, LEGW, GAP = 12 * S + 22, 300, 16
PW = COLW + LEGW
ROWH = 24 + 12 * S + 26
TOP = 132
WID = 3 * PW + 4 * GAP
HEI = TOP + 3 * ROWH + 205
img = Image.new("RGB", (WID, HEI), PAPER)
d = ImageDraw.Draw(img)

d.text((GAP, 14), "Level space · 62 · Room programmes for Level 0 and the Office: top views (north up, 1 m grid)",
       font=F(22, True), fill=INK)
d.text((GAP, 44), "Module metres; 3 m cell lines darker. Each plan shows one instance in the state named in its title. "
       "Numbers refer to the list beside each plan; kit names drop the Kit_ prefix.", font=F(13), fill=INK)

# global legend
lx, ly = GAP, 72


def sw_rect(x, y, role, new, sight=False):
    d.rectangle((x, y, x + 22, y + 13), fill=TINT[role] if new else WHITE, outline=ROLE[role], width=2)
    if sight:
        d.rectangle((x, y, x + 22, y + 13), outline=INK, width=3)


items = [
    (lambda x, y: sw_rect(x, y, "S", True), "signature (new kit)"),
    (lambda x, y: sw_rect(x, y, "W", True), "working set (new)"),
    (lambda x, y: sw_rect(x, y, "C", True), "clutter (new)"),
    (lambda x, y: sw_rect(x, y, "W", False), "existing kit (white)"),
    (lambda x, y: sw_rect(x, y, "W", True, True), "blocks a 1.62 m eye (collider top ≥ 1.65)"),
    (lambda x, y: dashed(d, (x, y + 7), (x + 22, y + 7), ROLE["S"], width=4, dash=4, gap=3),
     "see-through barrier (body-only collider)"),
]
items2 = [
    (lambda x, y: dashed_rect(d, (x, y, x + 22, y + 13), ROLE["W"], width=2, dash=5, gap=4),
     "ceiling or on-top item (no floor footprint)"),
    (lambda x, y: d.rectangle((x, y, x + 22, y + 13), fill=GREEN, outline=GREEN_EDGE), "keep-clear strip (1.0 m; 1.2 m at doors)"),
    (lambda x, y: dashed(d, (x, y + 7), (x + 22, y + 7), ROUTE, width=2, dash=3, gap=4), "a Relay route (aisles ≥ 0.9 m)"),
    (lambda x, y: star(d, x + 11, y + 7, 7), "the room's one event"),
    (lambda x, y: (d.rectangle((x, y, x + 22, y + 13), fill=(238, 236, 230)),
                   hatch(d, (x, y, x + 22, y + 13), 5, (150, 146, 138))), "steps / ramp / stair (terrain)"),
    (lambda x, y: d.rectangle((x, y, x + 22, y + 13), fill=TER["raised"], outline=GRID3), "raised floor; other tints: levels"),
]
for row_i, lst in enumerate((items, items2)):
    x = lx
    for fn, txt in lst:
        fn(x, ly + row_i * 22)
        d.text((x + 28, ly + row_i * 22 + 7), txt, font=F(12), fill=INK, anchor="lm")
        x += 28 + d.textlength(txt, font=F(12)) + 22


def draw_panel(spec, x0, y0, plan_w_m=12):
    d.text((x0, y0 + 2), spec["title"], font=F(14, True), fill=INK)
    W, D = spec["W"], spec["D"]
    ox, oy = x0 + 8, y0 + 26
    pl = Plan(d, ox, oy, W, D)
    pl.grid()
    later = ("guard", "halfwall", "ramp", "steps", "spiral", "rail", "text")
    pl.terrain([t for t in spec["terrain"] if t["k"] not in later])
    # regrid over the tints so the metre lines stay readable
    for i in range(int(W) + 1):
        d.line([pl.p(i, 0), pl.p(i, D)], fill=GRID3 if i % 3 == 0 else GRID1, width=1)
    for j in range(int(D) + 1):
        d.line([pl.p(0, j), pl.p(W, j)], fill=GRID3 if j % 3 == 0 else GRID1, width=1)
    pl.terrain([t for t in spec["terrain"] if t["k"] in ("ramp", "steps", "spiral", "rail")])
    pl.terrain([t for t in spec["terrain"] if t["k"] == "text"])
    for edge, cell, kind in spec["openings"]:
        pl.strip(edge, cell, kind)
    if spec.get("alcove"):
        a, b, dep = spec["alcove"]
        d.rectangle(pl.box(a, -dep, b - a, dep), fill=WHITE)
    pl.walls(spec["openings"])
    if spec.get("alcove"):
        a, b, dep = spec["alcove"]
        d.rectangle(pl.box(a, -0.04, b - a, 0.08), fill=WHITE)
        d.line([pl.p(a, 0), pl.p(a, -dep), pl.p(b, -dep), pl.p(b, 0)], fill=INK, width=5)
    for a, b in spec.get("lifts", []):
        d.rectangle(pl.box(a, D - 0.12, b - a, 0.24), fill=(170, 166, 158), outline=INK)
        d.text(((pl.p(a, D)[0] + pl.p(b, D)[0]) / 2, pl.p(0, D - 0.45)[1]), "elev.", font=F(10),
               fill=(96, 92, 84), anchor="mm")
    for t in spec["terrain"]:
        if t["k"] in ("guard", "halfwall"):
            pl.terrain([t])
    for pc in spec["pieces"]:
        pl.piece(pc)
    for kind, a, b in spec.get("extra", []):
        d.line([pl.p(*a), pl.p(*b)], fill=ROLE["W"], width=2)
    pl.route(spec["route"])
    if spec.get("event"):
        pl.event(*spec["event"])
    # legend column
    lxx = x0 + 8 + plan_w_m * S + 14
    lyy = oy + 2
    roles = {}
    for pc in spec["pieces"]:
        if pc.get("n") is not None and pc["n"] not in roles:
            roles[pc["n"]] = (pc["role"], pc.get("new", True), pc.get("sight", False))
    for n in sorted(spec["legend"]):
        role, new, sight = roles.get(n, ("C", True, False))
        d.rectangle((lxx, lyy + 1, lxx + 12, lyy + 12), fill=TINT[role] if new else WHITE, outline=ROLE[role],
                    width=2)
        d.text((lxx + 18, lyy + 7), f"{n}", font=F(11, True), fill=ROLE[role], anchor="lm")
        d.text((lxx + 38, lyy + 7), spec["legend"][n], font=F(11), fill=INK, anchor="lm")
        lyy += 17
    lyy += 6
    star(d, lxx + 6, lyy + 7, 6)
    d.text((lxx + 18, lyy + 7), spec.get("event_t", ""), font=F(11, True), fill=INK, anchor="lm")
    lyy += 20
    for line in spec.get("notes", []):
        d.text((lxx, lyy + 7), line, font=F(11), fill=(80, 78, 72), anchor="lm")
        lyy += 15


for idx, spec in enumerate(P):
    r, c = divmod(idx, 3)
    x0 = GAP + c * (PW + GAP)
    y0 = TOP + r * ROWH
    draw_panel(spec, x0, y0)

# corridor strip along the bottom
yj = TOP + 3 * ROWH + 4
draw_panel(J, GAP, yj, plan_w_m=15)
NX = GAP + 15 * S + 14 + LEGW + 30
for k, line in enumerate([
        "Corridor set: one identity object per zone or chunk (10_research L0-5);",
        "wall pieces stay at most 0.10 m proud, except the fountain, which sits in an alcove.",
        "",
        "Not shown: most desk-top clutter (tent cards, papers, cups, phones), the cleared and",
        "abandoned states, and the file mezzanine's upper tier. Layouts are proposals for the",
        "map line to author; the terrain drawn (tiers, steps, ramps, platform, gallery) is schematic."]):
    d.text((NX, yj + 30 + k * 18), line, font=F(12), fill=INK)

out = os.path.join(OUT, "ls62_room_programmes.png")
img.save(out, optimize=True)
print(out, img.size)
