#!/usr/bin/env python3
"""Terrain figures for research/level_space/40_terrain_grammar.md (K research sub-chat, 2026-10-31).

Pure Pillow, no downloads. Writes PNGs into the research folder's images/.
Run: /Users/redwang/FrontRoomsVisualWork/venv/bin/python make_terrain_figs.py
"""
import math
import os
from PIL import Image, ImageDraw, ImageFont

OUT = ("/Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01 Individual Game Project/"
       "Frontrooms3D/Documentation/research/level_space/images")
FD = "/System/Library/Fonts/Supplemental/"


def F(size, bold=False):
    return ImageFont.truetype(FD + ("Arial Bold.ttf" if bold else "Arial.ttf"), size)


PAPER = (244, 242, 236)
INK = (32, 32, 32)
GREY = (140, 140, 140)
LIGHT = (214, 210, 202)
WATER = (96, 186, 190)
DEEP = (24, 70, 78)
FAM = {"A": (218, 196, 112), "B": (196, 120, 96), "C": (96, 186, 190), "D": (150, 160, 196), "E": (170, 170, 168),
       "F": (186, 140, 176)}


class Icon:
    """Section icon in a card: 1 m = s px; datum (h = 0) at baseline."""

    def __init__(self, d, x0, y0, s=17.0, base=92):
        self.d, self.x0, self.y0, self.s, self.base = d, x0, y0, s, base

    def X(self, m):
        return self.x0 + 8 + m * self.s

    def Y(self, h):
        return self.y0 + self.base - h * self.s

    def g(self, a, b, h, w=4):
        self.d.line([(self.X(a), self.Y(h)), (self.X(b), self.Y(h))], fill=INK, width=w)

    def v(self, x, h0, h1, w=3, col=INK):
        self.d.line([(self.X(x), self.Y(h0)), (self.X(x), self.Y(h1))], fill=col, width=w)

    def stair(self, a, b, h0, h1, n):
        pts = [(self.X(a), self.Y(h0))]
        for i in range(n):
            x = a + (b - a) * i / n
            xn = a + (b - a) * (i + 1) / n
            h = h0 + (h1 - h0) * (i + 1) / n
            pts += [(self.X(x), self.Y(h)), (self.X(xn), self.Y(h))]
        self.d.line(pts, fill=INK, width=3)

    def ramp(self, a, b, h0, h1):
        self.d.line([(self.X(a), self.Y(h0)), (self.X(b), self.Y(h1))], fill=INK, width=4)

    def water(self, a, b, hb, ht, col=WATER):
        self.d.rectangle((self.X(a), self.Y(ht), self.X(b), self.Y(hb)), fill=col)

    def poly_water(self, pts, col=WATER):
        self.d.polygon([(self.X(x), self.Y(h)) for x, h in pts], fill=col)

    def slab(self, a, b, h, t=.25):
        self.d.rectangle((self.X(a), self.Y(h), self.X(b), self.Y(h - t)), fill=INK)

    def rail(self, a, b, h, hh=1.07):
        self.d.line([(self.X(a), self.Y(h + hh)), (self.X(b), self.Y(h + hh))], fill=GREY, width=2)
        for x in (a, b):
            self.d.line([(self.X(x), self.Y(h)), (self.X(x), self.Y(h + hh))], fill=GREY, width=2)

    def ceil(self, a, b, h, dash=True):
        if not dash:
            self.d.line([(self.X(a), self.Y(h)), (self.X(b), self.Y(h))], fill=GREY, width=2)
            return
        x = a
        while x < b:
            self.d.line([(self.X(x), self.Y(h)), (self.X(min(b, x + .4)), self.Y(h))], fill=GREY, width=2)
            x += .8

    def arch(self, cx, w, spring, col=INK, wd=3):
        r = w / 2.0
        box = (self.X(cx - r), self.Y(spring + r), self.X(cx + r), self.Y(spring - r))
        self.d.arc(box, 180, 360, fill=col, width=wd)
        self.v(cx - r, 0, spring, wd, col)
        self.v(cx + r, 0, spring, wd, col)

    def person(self, x, h):
        hx, hy = self.X(x), self.Y(h)
        self.d.line([(hx, hy), (hx, hy - 1.35 * self.s)], fill=(70, 100, 130), width=3)
        r = .17 * self.s
        self.d.ellipse((hx - r, hy - 1.75 * self.s, hx + r, hy - 1.75 * self.s + 2 * r), fill=(70, 100, 130))

    def hatch(self, a, b, h0, h1):
        x0, x1, y0, y1 = self.X(a), self.X(b), self.Y(h1), self.Y(h0)
        k = x0 - (y1 - y0)
        while k < x1:
            p0 = (max(k, x0), y1 - (max(k, x0) - k))
            p1 = (min(k + (y1 - y0), x1), y1 - (min(k + (y1 - y0), x1) - k))
            if p0[0] < p1[0]:
                self.d.line([p0, p1], fill=LIGHT, width=2)
            k += 8


def icons():
    """Section recipes per terrain type (metres, card width about 15 m)."""
    def A1(i): i.g(0, 15, 0); i.person(7, 0)

    def A2(i): i.g(0, 6, 0); i.stair(6, 6.6, 0, .6, 2); i.g(6.6, 15, .6); i.person(10, .6); i.person(3, 0)

    def A3(i): i.g(0, 4, 0); i.stair(4, 6.4, 0, -1.2, 8); i.g(6.4, 10, -1.2); i.stair(10, 12.4, -1.2, 0, 8); i.g(12.4, 15, 0); i.person(8, -1.2)

    def A4(i):
        for k in range(4):
            i.g(k * 3.6, k * 3.6 + 2.4, k * .6)
            if k < 3: i.stair(k * 3.6 + 2.4, k * 3.6 + 3.6, k * .6, (k + 1) * .6, 4)
        i.person(1, 0); i.person(12, 1.8)

    def A5(i): i.g(0, 15, 0); i.slab(7, 15, 2.7); i.rail(7, 7.2, 2.7); i.ceil(0, 15, 5.4); i.person(3, 0); i.person(11, 2.7)

    def A6(i): i.g(0, 4, 0); i.g(11, 15, 0); i.hatch(4, 11, -3, 0); i.slab(4, 11, 0, .2); i.rail(4, 11, 0); i.person(7.5, 0)

    def A7(i): i.g(0, 2, 0); i.g(13, 15, 0); i.water(2, 13, -.9, -.1); i.g(2, 13, -.9, 2); i.slab(5.5, 9.5, 0, .9); i.person(7.5, 0)

    def B1(i): i.g(0, 6, 0); i.stair(6, 6.9, 0, .45, 3); i.g(6.9, 15, .45); i.person(3, 0)

    def B2(i): i.g(0, 3, 0); i.stair(3, 6.6, 0, 1.8, 12); i.g(6.6, 15, 1.8); i.person(10, 1.8)

    def B3(i): i.g(0, 3, 0); i.stair(3, 8, 0, 1.45, 9); i.slab(8, 10, 1.45, .2); i.stair(8, 3.5, 1.45, 2.9, 9); i.g(0, 3.5, 2.9, 3); i.person(1.5, 2.9)

    def B4(i):
        cx, r = 7.5, 1.2
        i.v(cx, 0, 3.2, 4)
        pts = []
        for k in range(73):
            t = k / 72.0
            pts.append((i.X(cx + r * math.sin(t * 2 * math.pi * 1.1)), i.Y(t * 2.9)))
        i.d.line(pts, fill=INK, width=3)
        i.g(0, 15, 0); i.slab(cx + r, 15, 2.9, .2); i.person(3, 0)

    def B5(i):
        i.g(0, 2, 0); i.stair(2, 10, 0, 2.4, 16); i.g(10, 15, 2.4)
        i.d.arc((i.X(.6), i.Y(.9), i.X(3.4), i.Y(-.9)), 270, 360, fill=INK, width=3); i.person(13, 2.4)

    def B6(i):
        for k in range(5):
            i.g(2 + k * .9, 2.9 + k * .9, k * .45, 3)
            i.v(2.9 + k * .9, k * .45, (k + 1) * .45, 3)
        i.g(0, 2, 0); i.g(6.5, 15, 2.25); i.person(4, .9)

    def B7(i): i.g(0, 2, 0); i.ramp(2, 11.1, 0, .76); i.g(11.1, 15, .76); i.person(6, .38)

    def B8(i):
        i.g(0, 7, 0); i.water(7, 15, -1.4, -.1); i.g(7, 15, -1.4, 2); i.v(7, -1.4, 0, 3)
        for x in (7.4, 8.0): i.v(x, -1.2, .8, 2, GREY)
        for h in (-.9, -.5, -.1, .3): i.d.line([(i.X(7.4), i.Y(h)), (i.X(8.0), i.Y(h))], fill=GREY, width=2)
        i.person(4, 0)

    def C1(i): i.g(0, 4, 0); i.g(11, 15, 0); i.water(4, 11, -1.3, -.1); i.g(4, 11, -1.3, 3); i.v(4, -1.3, 0); i.v(11, -1.3, 0); i.person(7.5, -1.3)

    def C2(i): i.water(0, 15, 0, .12); i.g(0, 15, 0); i.person(7, 0)

    def C3(i): i.g(0, 4, 0); i.poly_water([(4, -.1), (14, -.1), (14, -.9), (4 + 8 * (.8 / .9), -.9)]); i.ramp(4, 14, 0, -.9); i.person(7, -.3)

    def C4(i):
        i.g(0, 5, 0); i.g(10, 15, 0); i.water(5, 10, -.9, -.15); i.g(5, 10, -.9, 3); i.v(5, -.9, 0); i.v(10, -.9, 0)
        i.d.line([(i.X(6), i.Y(-.5)), (i.X(9), i.Y(-.5))], fill=DEEP, width=2); i.d.polygon([(i.X(9), i.Y(-.35)), (i.X(9.6), i.Y(-.5)), (i.X(9), i.Y(-.65))], fill=DEEP)

    def C5(i):
        i.water(0, 5, -.6, -.05); i.g(0, 5, -.6, 3); i.v(5, -.6, 0, 3)
        i.water(5, 10, -1.2, -.65); i.g(5, 10, -1.2, 3); i.v(10, -1.2, -.6, 3)
        i.water(10, 15, -1.8, -1.25); i.g(10, 15, -1.8, 3)
        for x in (5.05, 10.05): i.d.line([(i.X(x), i.Y(-.05 if x < 6 else -.65)), (i.X(x + .3), i.Y(-.65 if x < 6 else -1.25))], fill=DEEP, width=3)

    def C6(i):
        i.g(0, 15, -.9, 3); i.water(0, 15, -.9, -.2); i.d.arc((i.X(3), i.Y(2.6), i.X(12), i.Y(-.2)), 180, 360, fill=INK, width=3)
        i.person(7.5, -.9)

    def C7(i):
        i.g(0, 4, 0); i.stair(4, 11, 0, -2.6, 14); i.poly_water([(5.5, -.6), (15, -.6), (15, -2.6), (11, -2.6)], DEEP)
        i.water(5.5 + (11 - 5.5) * 0, 15, -.75, -.6); i.v(13, -2.6, 1.2, 3, GREY)

    def D1(i): i.g(0, 15, 0); i.ceil(0, 7, 2.2, False); i.v(7, 2.2, 2.9, 2, GREY); i.ceil(7, 15, 2.9, False); i.person(3.5, 0); i.person(11, 0)

    def D2(i):
        i.g(0, 15, 0)
        for cx in (2.8, 7.5, 12.2): i.arch(cx, 2.6, 1.9)
        i.person(7.5, 0)

    def D3(i): i.g(0, 15, 0); i.v(3.5, 0, 1.2, 3); i.v(11.5, 0, 1.2, 3); i.d.arc((i.X(3.5), i.Y(1.2 + 4), i.X(11.5), i.Y(1.2 - 4)), 180, 360, fill=INK, width=3); i.person(7.5, 0)

    def D4(i):
        i.g(0, 15, 0); i.v(3.3, 0, 1.0, 3); i.v(11.7, 0, 1.0, 3)
        i.d.arc((i.X(3.3), i.Y(1.0 + 4.2), i.X(11.7), i.Y(1.0 - 4.2)), 180, 250, fill=INK, width=3)
        i.d.arc((i.X(3.3), i.Y(1.0 + 4.2), i.X(11.7), i.Y(1.0 - 4.2)), 290, 360, fill=INK, width=3)
        for dx in (-.6, 0, .6): i.d.line([(i.X(7.5 + dx * .4), i.Y(5.1)), (i.X(7.5 + dx * 2), i.Y(0))], fill=(230, 200, 120), width=2)

    def D5(i): i.g(0, 15, 0); i.slab(0, 4.5, 2.9); i.slab(10.5, 15, 2.9); i.rail(4.3, 4.5, 2.9); i.rail(10.5, 10.7, 2.9); i.ceil(0, 15, 5.8); i.person(7.5, 0); i.person(2, 2.9)

    def D6(i):
        i.g(0, 15, 0); i.ceil(0, 15, 2.9, False)
        for x in (3, 7.5, 12): i.d.rectangle((i.X(x - .5), i.Y(2.9), i.X(x + .5), i.Y(2.4)), fill=GREY)
        i.person(5, 0)

    def E1(i): i.g(0, 8, 0); i.rail(7.8, 8, 0); i.hatch(8, 15, -3, 0); i.person(5, 0)

    def E2(i):
        i.g(0, 15, 0); i.d.rectangle((i.X(7.3), i.Y(1.1), i.X(7.7), i.Y(0)), fill=INK); i.person(4, 0)
        i.d.line([(i.X(4), i.Y(1.62)), (i.X(14), i.Y(1.2))], fill=(220, 70, 50), width=2)

    def E3(i):
        i.g(0, 6, 2.4); i.g(6, 15, 0); i.v(6, 0, 2.4, 4); i.d.rectangle((i.X(5.8), i.Y(4.2), i.X(6.2), i.Y(3.1)), fill=WATER)
        i.v(6, 4.2, 5.2, 4); i.person(3, 2.4)

    def E4(i):  # plan view: room with an apse
        x0, y0 = i.X(1), i.Y(3.6)
        x1, y1 = i.X(9.5), i.Y(-.6)
        i.d.line([(x1, y0), (x0, y0), (x0, y1), (x1, y1)], fill=INK, width=3)
        i.d.arc((i.X(9.5 - 2.1), y0, i.X(9.5 + 2.1), y1), 270, 450, fill=INK, width=3)
        i.d.text((i.X(1.2), i.Y(4.6)), "plan", font=F(14), fill=GREY)

    def E5(i):
        i.g(0, 15, 0); i.ceil(0, 15, 3.4, False)
        for x in (1.5, 4.5, 7.5, 10.5, 13.5): i.d.rectangle((i.X(x - .3), i.Y(3.4), i.X(x + .3), i.Y(0)), fill=GREY)
        i.water(0, 15, 0, .25)

    def E6(i):
        i.g(0, 15, 0); i.v(5, 0, 3, 4); i.v(10, 0, 3, 4); i.d.line([(i.X(5), i.Y(2.4)), (i.X(10), i.Y(2.4))], fill=INK, width=3)
        i.d.rectangle((i.X(5.4), i.Y(.45), i.X(9.6), i.Y(0)), fill=GREY); i.d.ellipse((i.X(7.3), i.Y(2.35), i.X(7.7), i.Y(2.15)), fill=(240, 220, 150))

    def F1(i):
        i.g(0, 15, 0); i.slab(0, 3, 2.9); i.slab(12, 15, 2.9); i.slab(0, 3, 5.8); i.slab(12, 15, 5.8)
        i.stair(3, 12, 0, 2.9, 14); i.stair(12, 3, 2.9, 5.8, 14)

    def F2(i):
        i.water(0, 4, .9, 1.75); i.g(0, 4, .9, 3); i.v(4, .3, .9, 3); i.water(4, 8, .3, .85); i.g(4, 8, .3, 3); i.v(8, -.9, .3, 3)
        i.water(8, 12, -.9, -.2); i.g(8, 12, -.9, 3); i.stair(12, 15, -.9, 0, 6); i.person(13.6, -.45)

    def F3(i):
        i.g(0, 4, 0); i.stair(4, 5.5, 0, 1.2, 8); i.g(5.5, 9, 1.2); i.stair(9, 10.5, 1.2, 2.4, 8); i.g(10.5, 15, 2.4)
        i.stair(5.5, 7, 0, -1.2, 8); i.g(7, 9.5, -1.2, 3); i.person(2, 0); i.person(12.5, 2.4)

    def F4(i):
        cx = 3.5
        i.water(6, 15, -1.0, -.1); i.g(0, 6, 0); i.g(6, 15, -1.0, 2); i.v(cx, 0, 6.2, 4)
        pts = [(i.X(cx + .9 * math.sin(k / 70.0 * 2 * math.pi * 2)), i.Y(k / 70.0 * 5.6)) for k in range(71)]
        i.d.line(pts, fill=INK, width=2); i.slab(2.3, 4.7, 5.8, .2)
        i.d.line([(i.X(4.7), i.Y(5.6)), (i.X(8), i.Y(4.2)), (i.X(10), i.Y(1.5)), (i.X(12.5), i.Y(.2))], fill=(196, 120, 96), width=6)

    def F5(i):
        i.g(0, 15, -1.2, 3); i.water(0, 15, -1.2, -.4)
        for cx in (2.5, 7.5, 12.5): i.arch(cx, 3.0, 1.0)
        i.slab(0, 15, 4.0); i.rail(0, 15, 4.0); i.person(9, 4.0)

    def F6(i):
        blocks = [(0, 3.6, .6), (3.6, 6.6, 2.4), (6.6, 9.6, 1.2), (9.6, 12.6, 3.0), (12.6, 15, .3)]
        for a, b, h in blocks:
            i.d.rectangle((i.X(a), i.Y(h), i.X(b), i.Y(-.6)), fill=(232, 222, 208), outline=INK, width=2)
        i.stair(3.0, 3.6, .6, 2.4, 6); i.stair(6.6, 7.4, 2.4, 1.2, 4); i.stair(9.0, 9.6, 1.2, 3.0, 6); i.person(8, 1.2)

    return [
        ("A", "GROUND LEVELS", [("A1", "flat floor", A1), ("A2", "dais / raised floor", A2), ("A3", "pit / sunken floor", A3),
                                ("A4", "terraces", A4), ("A5", "mezzanine", A5), ("A6", "bridge over void", A6),
                                ("A7", "island", A7)]),
        ("B", "CONNECTORS", [("B1", "threshold steps", B1), ("B2", "straight flight", B2), ("B3", "switchback", B3),
                             ("B4", "spiral stair", B4), ("B5", "grand / flared stair", B5), ("B6", "seating steps", B6),
                             ("B7", "ramp 1:12", B7), ("B8", "ladder / stepping", B8)]),
        ("C", "WATER FORMS", [("C1", "basin", C1), ("C2", "sheet flood", C2), ("C3", "beach entry", C3), ("C4", "channel", C4),
                              ("C5", "weir / cascade", C5), ("C6", "grotto", C6), ("C7", "stair into water", C7)]),
        ("D", "OVERHEAD", [("D1", "low lid", D1), ("D2", "arch / arcade", D2), ("D3", "barrel vault", D3),
                           ("D4", "dome + oculus", D4), ("D5", "double-height void", D5), ("D6", "soffit / coffer", D6)]),
        ("E", "EDGES", [("E1", "guarded edge", E1), ("E2", "half-wall", E2), ("E3", "window between levels", E3),
                        ("E4", "curved wall / apse", E4), ("E5", "column forest", E5), ("E6", "lit niche", E6)]),
        ("F", "ASSEMBLIES", [("F1", "stair hall", F1), ("F2", "terraced water court", F2), ("F3", "split levels", F3),
                             ("F4", "slide tower", F4), ("F5", "arcade over water", F5), ("F6", "casbah cluster", F6)]),
    ]


def fig4():
    W, rowH, top = 2400, 214, 150
    fams = icons()
    H = top + rowH * len(fams) + 70
    im = Image.new("RGB", (W, H), PAPER)
    d = ImageDraw.Draw(im)
    n = sum(len(t) for _, _, t in fams)
    d.text((60, 36), "Fig. 4 · Terrain types: %d elements in five families, plus six assemblies" % (n - 6), font=F(42, True), fill=INK)
    d.text((60, 92), "Section icons, 1 m = 17 px, datum = the corridor floor. A blue figure is 1.75 m. Water teal, dark water deep teal. "
           "E4 is drawn in plan.", font=F(24), fill=GREY)
    cw = 278
    for r, (key, name, items) in enumerate(fams):
        y = top + r * rowH
        d.rectangle((40, y + 6, 172, y + rowH - 14), fill=FAM[key])
        d.text((106, y + rowH / 2 - 14), key, font=F(44, True), fill=INK, anchor="mm")
        d.text((106, y + rowH / 2 + 30), name, font=F(15, True), fill=INK, anchor="mm")
        for c, (code, label, fn) in enumerate(items):
            x = 186 + c * cw
            d.rectangle((x, y + 6, x + cw - 12, y + rowH - 14), outline=LIGHT, width=2)
            ic = Icon(d, x + 6, y + 14)
            fn(ic)
            d.text((x + 10, y + rowH - 46), code, font=F(19, True), fill=INK)
            d.text((x + 52, y + rowH - 45), label, font=F(18), fill=INK)
    d.text((60, H - 46), "Sources for each type: 40_terrain_grammar.md §2 and SOURCES.md LS41-LS50. Real dimensions: risers 0.15 m "
           "(spiral up to 0.24 m), ramps 1:12, guards 1.07 m, raised floor 0.30 m.", font=F(20), fill=GREY)
    im.save(os.path.join(OUT, "ls_fig4_terrain_types.png"))


def fig5():
    W, H = 2400, 1420
    im = Image.new("RGB", (W, H), PAPER)
    d = ImageDraw.Draw(im)
    d.text((60, 36), "Fig. 5 · Splicing terrains: one vertical module, built joins, and a well without symmetry", font=F(42, True), fill=INK)
    d.text((60, 92), "Top: one continuous section through a pool-level cluster (heights to scale, 1 m = 34 px). Bottom left: the vertical "
           "module. Bottom right: the well in plan, round 2 against the proposal.", font=F(24), fill=GREY)
    # --- long section
    s = Icon(d, 60, 140, s=34.0, base=430)
    segs = [(0, 6, 0)]
    s.g(0, 6, 0); s.ceil(0, 6, 2.9, False); s.person(3, 0)
    s.arch(7.2, 2.4, 1.0); s.d.line([(s.X(6), s.Y(2.9)), (s.X(6), s.Y(2.2))], fill=GREY, width=2)
    s.poly_water([(8.4, -.1), (16, -.1), (16, -.7), (14.4, -.7)]); s.ramp(8.4, 16, 0, -.7); s.g(16, 19, -.7)
    s.water(16, 19, -.7, -.1)
    s.stair(19, 20.2, -.7, .5, 8); s.g(20.2, 26, .5); s.person(23, .5)
    s.water(26, 31, -1.2, -.2); s.g(26, 31, -1.2, 3); s.slab(26, 31, .5, .18); s.rail(26, 31, .5)
    s.water(31, 34, -1.2, -.2); s.d.rectangle((s.X(31), s.Y(.5), s.X(34), s.Y(-1.2)), fill=(232, 222, 208), outline=INK, width=3)
    s.water(34, 40, -1.2, -.5); s.g(34, 40, -1.2, 3); s.v(34, -1.2, .5, 3); s.d.line([(s.X(34.05), s.Y(.45)), (s.X(34.4), s.Y(-.5))], fill=DEEP, width=4)
    cx = 42.5
    s.g(40, 45, -1.2); s.water(40, 45, -1.2, -.5)
    s.v(cx, -1.2, 2.2, 5)
    pts = [(s.X(cx + 1.1 * math.sin(k / 60.0 * 2 * math.pi * 1.05)), s.Y(-1.2 + k / 60.0 * 3.4)) for k in range(61)]
    s.d.line(pts, fill=INK, width=3)
    s.slab(cx + 1.1, 52, 2.2, .22); s.rail(46, 52, 2.2); s.person(49, 2.2)
    s.d.arc((s.X(42), s.Y(4.8 + 5), s.X(52), s.Y(4.8 - 5)), 180, 252, fill=INK, width=3)
    s.d.arc((s.X(42), s.Y(4.8 + 5), s.X(52), s.Y(4.8 - 5)), 288, 360, fill=INK, width=3)
    s.v(42, 2.2, 4.8, 3); s.v(52, 2.2, 4.8, 3)
    for dx in (-.4, 0, .4): s.d.line([(s.X(47 + dx * .5), s.Y(9.6)), (s.X(47 + dx * 4), s.Y(2.2))], fill=(230, 200, 120), width=2)
    s.g(52, 55, 2.2)
    s.stair(55, 62, 2.2, -2.2, 22); s.poly_water([(57.4, -.9), (66, -.9), (66, -2.2), (62, -2.2)], DEEP); s.g(62, 66, -2.2)
    labels = [(3, 3.2, "corridor 2.9"), (7.2, 3.5, "D2 arch"), (12, .6, "C3 beach"), (19.6, 1.0, "B1 steps"),
              (23, 2.8, "A4 terrace +0.5"), (28.5, 1.9, "A6 bridge / C4 channel"), (32.5, 2.6, "A7 island"),
              (37, .6, "C5 weir"), (42.5, 3.0, "B4 spiral"), (49, 4.8, "A5 gallery + D4 oculus"),
              (59, 2.9, "C7 stair to the dark zone")]
    for x, h, t in labels:
        d.text((s.X(x), s.Y(h)), t, font=F(19, True), fill=INK, anchor="mb")
    d.text((60, 640), "Every height change is a built object; no two neighbours share an axis; arch, spiral and oculus sit off-centre. "
           "Water levels: ankle -0.12 (D6), wading basins -1.2.", font=F(21), fill=GREY)
    # --- vertical module (bottom left)
    x0, y0 = 60, 700
    d.text((x0, y0), "The vertical module", font=F(30, True), fill=INK)
    rows = [("riser / tread", "0.15 / 0.30 m (2R + T = 0.60); spiral riser up to 0.24 m (9.5 in code max)"),
            ("dry levels", "snap to 0.30 m: the 1980s raised floor (12 in), two risers"),
            ("terrace unit", "0.60 m = 4 risers over 1.2 m: two fine modules of run"),
            ("ramp", "1:12, at most 0.76 m per run (30 in), 1.52 m landings (ADA 1991)"),
            ("spiral", "one turn per storey inside one 3 m cell, diameter 1.5-2.4 m"),
            ("guard", "1.07 m (42 in) at every drop over 0.76 m; sight crosses"),
            ("water", "ankle -0.12; wading basins -1.07 to -1.37 (D6); weir steps 0.3-0.6"),
            ("overhead", "ceiling follows the room, not the floor: a raised floor lowers the lid")]
    for k, (a, b) in enumerate(rows):
        y = y0 + 54 + k * 74
        d.rectangle((x0, y, x0 + 24, y + 24), fill=FAM["B"] if k < 5 else FAM["E"])
        d.text((x0 + 40, y - 2), a, font=F(23, True), fill=INK)
        d.text((x0 + 40, y + 28), b, font=F(20), fill=INK)
    # --- the well in plan (bottom right)
    px, py = 1180, 700
    d.text((px, py), "The well in plan: round 2 (left) and spliced (right)", font=F(30, True), fill=INK)
    u = 22.0

    def P(ox, oy, x, y):
        return (ox + x * u, oy + y * u)
    ox1, oy1 = px + 30, py + 70
    for k in range(5):
        m = 1.2 * k
        d.rectangle((P(ox1, oy1, m, m)[0], P(ox1, oy1, m, m)[1], P(ox1, oy1, 22 - m, 22 - m)[0], P(ox1, oy1, 22 - m, 22 - m)[1]),
                    outline=(120, 120, 120) if k else INK, width=3)
    d.text((ox1, oy1 + 22 * u + 14), "concentric rings, one centre, every side equal", font=F(19), fill=GREY)
    ox2, oy2 = px + 600, py + 70
    outlines = [[(0, 0), (22, 0), (22, 22), (0, 22)], [(1.5, 2.2), (19, 1.2), (20.5, 14), (13, 20.5), (2.4, 19)],
                [(4, 3.4), (16.5, 4.2), (18, 11), (12, 18), (5.5, 16.8)], [(6.8, 6), (14.6, 7.4), (15.5, 12.6), (8.5, 15.2)]]
    for k, poly in enumerate(outlines):
        d.polygon([P(ox2, oy2, x, y) for x, y in poly], outline=INK if k == 0 else (120, 120, 120), width=3)
    d.polygon([P(ox2, oy2, x, y) for x, y in [(8.6, 8.4), (13.8, 9.2), (14.2, 12.0), (9.6, 13.8)]], fill=WATER)
    c = P(ox2, oy2, 17.2, 5.0)
    d.ellipse((c[0] - 1.2 * u, c[1] - 1.2 * u, c[0] + 1.2 * u, c[1] + 1.2 * u), outline=INK, width=3)
    d.line([c, (c[0] + 1.2 * u, c[1])], fill=INK, width=3)
    for k in range(4):
        a = P(ox2, oy2, 3.0 + k * .9, 17.0 + k * .3)
        d.line([a, (a[0] + 2.4 * u, a[1] - 1.2 * u)], fill=DEEP, width=3)
    d.polygon([P(ox2, oy2, x, y) for x, y in [(19.6, 13.6), (21.2, 13.6), (21.2, 17.6), (19.6, 17.6)]], outline=(196, 120, 96), width=3)
    notes = [((11.2, 6.2), "B4 spiral, one corner"), ((1.0, 20.6), "C5 cascade, one wall"),
             ((13.4, 18.9), "A6 bridge, one floor"), ((4.5, -1.1), "each floor's edge cut differently")]
    for (x, y), t in notes:
        d.text(P(ox2, oy2, x, y), t, font=F(18, True), fill=INK)
    d.text((ox2, oy2 + 22 * u + 14), "galleries off-centre, terraces at ±0.3/0.6 m, arches on one or two sides", font=F(19), fill=GREY)
    im.save(os.path.join(OUT, "ls_fig5_splicing.png"))


if __name__ == "__main__":
    fig4()
    fig5()
    print("ok")
