#!/usr/bin/python3
"""FrontRooms station paper: Prop_StationPaper atlas + the STAIR KEY plate (平面视觉, 2026-10-30, BS7).
Cells and sizes come from Tools/Blender/frontrooms_kit/assets/_station_common.py and the kits; strings from
research/station/01_research.md §6. Each cell is drawn at its physical size, then stretched into its square atlas cell,
because the kits map the whole cell onto the quad. Paper is white here: Prop_StationPaper's _BaseColor tints it to #DCD8CC.
Usage: build_atlas.py <out dir>"""
import json, os, sys
from PIL import Image, ImageDraw, ImageFont

ROOT = "/Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01 Individual Game Project/Frontrooms3D"
F = ROOT + "/Assets/Fonts/Period1990/"
HB, HR, HC = F + "TeXGyre/texgyreheros-bold.otf", F + "TeXGyre/texgyreheros-regular.otf", F + "TeXGyre/texgyreheroscn-bold.otf"
TYPED = F + "CourierPrime/CourierPrime-Regular.ttf"
HAND = os.path.expanduser("~/Library/Fonts/RockSalt.ttf")
K, RULE, BLUE, PAPER = (26, 26, 26), (120, 120, 118), (31, 42, 92), (255, 255, 255)
PX = 12                                    # px per mm while drawing
CELL, N = 1024, 4


def font(path, cap):
    probe = ImageFont.truetype(path, 1000)
    b = probe.getbbox("H")
    return ImageFont.truetype(path, round(cap * PX * 1000 / (b[3] - b[1])))


class Page:
    def __init__(self, w, h):
        self.im = Image.new("RGB", (round(w * PX), round(h * PX)), PAPER)
        self.d = ImageDraw.Draw(self.im)
        self.w, self.h = w, h

    def text(self, s, x, y, path, cap, fill=K, track=0.0, align="l", stroke=0):
        """x, y in mm; y is the baseline."""
        f = font(path, cap)
        w = sum(f.getlength(c) + track * PX for c in s) - track * PX if track else f.getlength(s)
        px = x * PX - (w if align == "r" else w / 2 if align == "c" else 0)
        if track:
            for c in s:
                self.d.text((px, y * PX), c, font=f, fill=fill, anchor="ls")
                px += f.getlength(c) + track * PX
        else:
            self.d.text((px, y * PX), s, font=f, fill=fill, anchor="ls", stroke_width=stroke, stroke_fill=fill)

    def line(self, x0, y0, x1, y1, w=0.25, fill=RULE):
        self.d.line([(x0 * PX, y0 * PX), (x1 * PX, y1 * PX)], fill=fill, width=max(1, round(w * PX)))

    def box(self, x0, y0, x1, y1, w=0.4, fill=K):
        self.d.rectangle([x0 * PX, y0 * PX, x1 * PX, y1 * PX], outline=fill, width=max(1, round(w * PX)))

    def bar(self, x0, y0, x1, y1, fill=(150, 150, 148)):
        self.d.rectangle([x0 * PX, y0 * PX, x1 * PX, y1 * PX], fill=fill)


def form(p, heads, widths, x0, top, pitch, rows):
    """A printed ruled form: header band, column rules, row rules."""
    x = x0
    total = sum(widths)
    p.box(x0, top, x0 + total, top + 8 + rows * pitch, 0.4)
    p.line(x0, top + 8, x0 + total, top + 8, 0.4, K)
    for h, w in zip(heads, widths):
        p.text(h, x + 1.2, top + 5.4, HB, 2.1, track=0.1)
        if x > x0:
            p.line(x, top, x, top + 8 + rows * pitch, 0.25, K)
        x += w
    for r in range(1, rows):
        y = top + 8 + r * pitch
        p.line(x0, y, x0 + total, y)


def hand(p, s, x, y, cap=3.6, fill=BLUE):
    p.text(s.replace(" ", "  "), x, y, HAND, cap, fill, stroke=1)   # a ballpoint line; Rock Salt's word space is too tight


REG_HEADS = ["DATE", "NAME", "COMPANY", "VISITING", "TIME IN", "TIME OUT"]
REG_W = [19, 28, 46, 40, 24, 24]
VISITORS = [("T.R.", "COURIER", "MAILROOM", "0915", "0922"), ("L.H.", "COPIER SVC", "RM 214", "1010", "1135"),
            ("B.O.", "HVAC SVC", "BLDG SVCS", "1040", "1310"), ("M.C.", "WATER SVC", "FRONT OFFICE", "1125", "1140"),
            ("J.P.", "COURIER", "MAILROOM", "1330", "1336"), ("S.V.", "OFFICE SUPPLY", "STOCK RM 3", "1405", "1450"),
            ("R.A.", "PEST CONTROL", "BLDG SVCS", "1520", "1640"), ("D.N.", "VENDING", "RM 118", "1630", "1745"),
            ("T.R.", "COURIER", "MAILROOM", "1705", "1711")]


def register_left():
    p = Page(205, 285)
    form(p, REG_HEADS, REG_W, 12, 14, 9, 28)
    x = [12 + sum(REG_W[:i]) for i in range(6)]
    for r, v in enumerate(VISITORS):
        y = 14 + 8 + r * 9 + 6.8
        if r:                                # the date itself is live (01 §6): row 1 stays open, then ditto
            hand(p, '"', x[0] + 7, y)
        for c, s in zip(range(1, 6), v):
            hand(p, s, x[c] + 1.4, y, 3.0 if c in (2, 3) else 3.3)
    return p


def register_right():
    p = Page(205, 285)
    form(p, REG_HEADS, REG_W, 4, 14, 9, 28)
    return p


def tent_card():
    p = Page(200, 90)
    p.box(6, 6, 194, 84, 0.8)
    p.text("PLEASE SIGN IN", 100, 53, HB, 13, align="c", track=0.5)
    return p


def dar():
    p = Page(216, 279)
    lines = ["          NIGHT  DESK", "2300  ON DUTY  D.K.", "0000  TOUR 1 COMPLETE  ALL SECURE", "0100  TOUR 2 COMPLETE  ALL SECURE",
             "0112  BLDG SVCS CALLED  LAMPS OUT STOCK RM 3", "0200  TOUR 3"]
    for r in range(24):
        p.line(14, 40 + r * 9.5, 202, 40 + r * 9.5)
    for i, s in enumerate(lines):
        p.text(s, 16, 38.4 + i * 9.5, TYPED, 2.7)
    return p


def sign_out():
    p = Page(216, 279)
    heads = ["DATE", "KEY NO.", "NAME", "OUT", "IN", "INIT."]
    w = [26, 40, 46, 30, 30, 20]
    form(p, heads, w, 12, 16, 11, 21)
    x = [12 + sum(w[:i]) for i in range(6)]
    rows = [("14", "A.W.", "0810", "0925", "AW"), ("07", "R.M.", "0940", "1015", "RM"), ("22", "G.S.", "1120", "1305", "GS"),
            ("03", "R.M.", "1330", "1415", "RM"), ("14", "N.F.", "1500", "1620", "NF"), ("STAIR A", "", "", "", "")]
    for r, row in enumerate(rows):
        y = 16 + 8 + r * 11 + 8
        if 0 < r < 5:
            hand(p, '"', x[0] + 10, y)
        for c, s in zip(range(1, 6), row):
            if s:
                hand(p, s, x[c] + 2, y, 3.6)
    return p


def key_control():
    p = Page(200, 70)
    p.text("KEY CONTROL", 100, 36, HB, 15, align="c", track=0.4)
    p.text("ALL KEYS MUST BE SIGNED OUT", 100, 54, HB, 6, align="c", track=0.25)
    return p


def phone_list():
    p = Page(140, 200)
    p.text("PHONE LIST", 70, 24, HB, 5.5, align="c", track=0.4)
    p.line(14, 30, 126, 30, 0.4, K)
    for i, (a, b) in enumerate([("FIRE", "911"), ("POLICE", "911"), ("SECURITY DESK", "200"), ("FRONT OFFICE", "101"), ("MAILROOM", "118"),
                                ("BLDG SVCS", "210"), ("BOILER RM", "214"), ("CONTROL RM", "299")]):
        y = 46 + i * 16
        p.text(a, 16, y, TYPED, 4.2)
        p.text(b, 124, y, TYPED, 4.2, align="r")
    return p


def greek(p, x0, y0, x1, pitch, rows, cap, seed=0):
    """Text blocks with no readable text: the notices are blank by the plan."""
    w = x1 - x0
    for r in range(rows):
        k = [.97, .92, 1.0, .88, .95, .7][(r + seed) % 6]
        p.bar(x0, y0 + r * pitch, x0 + w * k, y0 + r * pitch + cap)


def notice(w, h, title, rows, seed):
    p = Page(w, h)
    p.bar(w * .1, h * .1, w * title, h * .1 + h * .045, (70, 70, 68))
    greek(p, w * .1, h * .22, w * .9, h * .07, rows, h * .022, seed)
    return p


def spine():
    p = Page(200, 34)
    p.text("POST ORDERS", 100, 24, HB, 14, align="c", track=0.8)
    return p


def plate():
    """The engraved two-ply plate, 0.30 x 0.10: a light core showing through a dark cap (Prop_StationKeyPlate #2B2925)."""
    cap, core = (43, 41, 37), (226, 221, 207)
    p = Page(300, 100)
    p.im.paste(cap, (0, 0, p.im.width, p.im.height))
    p.text("STAIR KEY", 150, 47, HB, 26, core, track=1.2, align="c")
    p.text("SILENT ALARM: REMOVAL SIGNALS PANEL", 150, 78, HC, 11, core, track=0.15, align="c")
    return p


CELLS = [("register_left", register_left), ("register_right", register_right), ("tent_card", tent_card), ("dar", dar), ("sign_out", sign_out),
         ("key_control", key_control), ("phone_list", phone_list), ("notice_a", lambda: notice(151, 195, .62, 11, 0)), ("notice_b", lambda: notice(140, 100, .7, 7, 2)),
         ("notice_c", lambda: notice(100, 140, .8, 8, 3)), ("notice_d", lambda: notice(120, 80, .75, 6, 1)), ("post_orders_spine", spine), ("sheet_blank", lambda: Page(216, 279))]


def main(out):
    os.makedirs(out, exist_ok=True)
    atlas = Image.new("RGB", (CELL * N, CELL * N), PAPER)
    for i, (name, fn) in enumerate(CELLS):
        im = fn().im.resize((CELL, CELL), Image.LANCZOS)
        atlas.paste(im, ((i % N) * CELL, (i // N) * CELL))
        print(name, i)
    atlas.save(os.path.join(out, "Prop_StationPaper_A.png"), optimize=True)
    plate().im.resize((2048, 1024), Image.LANCZOS).save(os.path.join(out, "Prop_StationKeyPlate_A.png"), optimize=True)
    slots = {"note": "runtime date slots, in atlas UV (origin top-left): empty on the art so a live MM-DD-90 can sit there; a blank slot reads as unfilled",
             "dar_date": {"cell": "dar", "x_mm": 16, "baseline_mm": 38.4, "w_mm": 34, "cap_mm": 2.7, "face": "Courier Prime"},
             "register_date": {"cell": "register_left", "x_mm": 13, "baseline_mm": 28.8, "w_mm": 17, "cap_mm": 2.5, "face": "Rock Salt, blue"},
             "sign_out_date": {"cell": "sign_out", "x_mm": 13, "baseline_mm": 32, "w_mm": 24, "cap_mm": 2.8, "face": "Rock Salt, blue", "same_date_as": "register_date"}}
    with open(os.path.join(out, "date_slots.json"), "w", newline="\n") as f:
        json.dump(slots, f, indent=1, ensure_ascii=False)
        f.write("\n")


main(sys.argv[1])
