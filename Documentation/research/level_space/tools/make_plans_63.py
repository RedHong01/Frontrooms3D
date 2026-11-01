#!/usr/bin/env python3
"""Room programmes for Run! and the pool rooms (63_furniture_run_pool.md).

Top views of one instance per programme on a 1 m grid. Plan coordinates are
metres from each panel's south-west corner, north up. Run with the W venv:
    /Users/redwang/FrontRoomsVisualWork/venv/bin/python make_plans_63.py
Writes Documentation/research/level_space/images/ls63_room_programmes.png.
"""
import math
import os

from PIL import Image, ImageDraw, ImageFont

ROOT = ("/Users/redwang/Desktop/ArtCenter/Fall26T7/"
        "EGAM-401A-01 Individual Game Project/Frontrooms3D")
OUT = os.path.join(ROOT, "Documentation/research/level_space/images/"
                   "ls63_room_programmes.png")
FONT = "/System/Library/Fonts/Supplemental/Arial.ttf"
FONT_B = "/System/Library/Fonts/Supplemental/Arial Bold.ttf"

S = 24  # pixels per metre
W_PX, H_PX = 2600, 1660

COL = {
    "p1": (206, 64, 52), "p2": (236, 146, 52), "p3": (240, 204, 70),
    "ex": (160, 160, 160), "pool": (46, 150, 150), "sib": (140, 110, 196),
    "arch": (214, 210, 198), "door": (120, 78, 40),
}
TXT_ON = {"p1": "w", "p2": "k", "p3": "k", "ex": "k", "pool": "w",
          "sib": "w", "arch": "k"}
KIND_NAME = {"p1": "P1", "p2": "P2", "p3": "P3", "ex": "existing",
             "pool": "POOL", "sib": "62", "arch": ""}

f_title = ImageFont.truetype(FONT_B, 30)
f_sub = ImageFont.truetype(FONT, 16)
f_panel = ImageFont.truetype(FONT_B, 15)
f_note = ImageFont.truetype(FONT, 12)
f_num = ImageFont.truetype(FONT_B, 11)
f_leg = ImageFont.truetype(FONT, 13)
f_legb = ImageFont.truetype(FONT_B, 13)


class Panel:
    def __init__(self, key, title, ox, oy, w, h, note=""):
        self.key, self.title, self.note = key, title, note
        self.ox, self.oy, self.w, self.h = ox, oy, w, h
        self.items, self.zones, self.lines, self.openings = [], [], [], []
        self.circles, self.texts = [], []

    def p(self, x, y):
        return (self.ox + x * S, self.oy + (self.h - y) * S)

    def rect_px(self, x0, y0, x1, y1):
        a = self.p(x0, y1)
        b = self.p(x1, y0)
        return (a[0], a[1], b[0], b[1])

    # item: number, name, kind, rect (x0,y0,x1,y1), style, label offset
    def it(self, num, name, kind, r, style="solid", lab=None, shape="rect"):
        self.items.append(dict(num=num, name=name, kind=kind, r=r,
                               style=style, lab=lab, shape=shape))

    def zone(self, r, fill, label=None, outline=None, dashed=False):
        self.zones.append((r, fill, label, outline, dashed))

    def line(self, pts, col, width=2, dashed=False):
        self.lines.append((pts, col, width, dashed))

    def opening(self, side, a, b, kind="arch", label=None):
        self.openings.append((side, a, b, kind, label))

    def text(self, x, y, s, col=(60, 60, 60), anchor="mm"):
        self.texts.append((x, y, s, col, anchor))


def dashed_line(d, a, b, col, width=1, dash=6, gap=4):
    x0, y0 = a
    x1, y1 = b
    length = math.hypot(x1 - x0, y1 - y0)
    if length == 0:
        return
    ux, uy = (x1 - x0) / length, (y1 - y0) / length
    t = 0.0
    while t < length:
        t2 = min(t + dash, length)
        d.line([(x0 + ux * t, y0 + uy * t), (x0 + ux * t2, y0 + uy * t2)],
               fill=col, width=width)
        t = t2 + gap


def dashed_rect(d, box, col, width=1):
    x0, y0, x1, y1 = box
    dashed_line(d, (x0, y0), (x1, y0), col, width)
    dashed_line(d, (x1, y0), (x1, y1), col, width)
    dashed_line(d, (x1, y1), (x0, y1), col, width)
    dashed_line(d, (x0, y1), (x0, y0), col, width)


def darker(c, k=0.6):
    return tuple(int(v * k) for v in c[:3])


def draw_panel(img, d, pn):
    ox, oy = pn.ox, pn.oy
    w, h = pn.w * S, pn.h * S
    pn.top, pn.bot = oy, oy + h
    d.rectangle([ox, oy, ox + w, oy + h], fill=(250, 250, 247))
    # zones (water, platform, keep-clear) under the grid
    ov = Image.new("RGBA", img.size, (0, 0, 0, 0))
    od = ImageDraw.Draw(ov)
    for r, fill, label, outline, dashed in pn.zones:
        box = pn.rect_px(*r)
        if fill:
            od.rectangle(box, fill=fill)
    img.alpha_composite(ov)
    d = ImageDraw.Draw(img)
    for r, fill, label, outline, dashed in pn.zones:
        box = pn.rect_px(*r)
        if outline:
            if dashed:
                dashed_rect(d, box, outline, 1)
            else:
                d.rectangle(box, outline=outline, width=1)
    # grid
    for i in range(int(pn.w) + 1):
        x = ox + i * S
        d.line([(x, oy), (x, oy + h)],
               fill=(205, 205, 205) if i % 3 == 0 else (232, 232, 232),
               width=1)
    for j in range(int(pn.h) + 1):
        y = oy + j * S
        d.line([(ox, y), (ox + w, y)],
               fill=(205, 205, 205) if (pn.h - j) % 3 == 0 else (232, 232, 232),
               width=1)
    for r, fill, label, outline, dashed in pn.zones:
        if label:
            box = pn.rect_px(*r)
            cx, cy = (box[0] + box[2]) / 2, (box[1] + box[3]) / 2
            d.text((cx, cy), label, font=f_note, fill=(70, 90, 70),
                   anchor="mm")
    # walls
    d.rectangle([ox, oy, ox + w, oy + h], outline=(55, 55, 55), width=3)
    for side, a, b, kind, label in pn.openings:
        if side in ("S", "N"):
            y = pn.h if side == "N" else 0
            p0, p1 = pn.p(a, y), pn.p(b, y)
        else:
            x = pn.w if side == "E" else 0
            p0, p1 = pn.p(x, a), pn.p(x, b)
        if kind == "glass":
            d.line([p0, p1], fill=(70, 170, 200), width=4)
            continue
        d.line([p0, p1], fill=(250, 250, 247), width=5)
        if kind == "door":
            d.line([p0, p1], fill=COL["door"], width=3)
        else:
            for pp in (p0, p1):
                if side in ("S", "N"):
                    d.line([(pp[0], pp[1] - 5), (pp[0], pp[1] + 5)],
                           fill=(55, 55, 55), width=2)
                else:
                    d.line([(pp[0] - 5, pp[1]), (pp[0] + 5, pp[1])],
                           fill=(55, 55, 55), width=2)
        if label:
            mx, my = (p0[0] + p1[0]) / 2, (p0[1] + p1[1]) / 2
            off = {"S": (0, 11), "N": (0, -11), "W": (-12, 0),
                   "E": (12, 0)}[side]
            d.text((mx + off[0], my + off[1]), label, font=f_num,
                   fill=COL["door"], anchor="mm")
    # extra lines
    for pts, col, width, dashed in pn.lines:
        pp = [pn.p(*q) for q in pts]
        for a, b in zip(pp, pp[1:]):
            if dashed:
                dashed_line(d, a, b, col, width)
            else:
                d.line([a, b], fill=col, width=width)
    # circles (clear zones etc.)
    for (cx, cy, rr, col, dashed) in pn.circles:
        c = pn.p(cx, cy)
        box = [c[0] - rr * S, c[1] - rr * S, c[0] + rr * S, c[1] + rr * S]
        if dashed:
            n = 40
            for k in range(0, n, 2):
                a0 = 2 * math.pi * k / n
                a1 = 2 * math.pi * (k + 1) / n
                d.line([(c[0] + rr * S * math.cos(a0),
                         c[1] + rr * S * math.sin(a0)),
                        (c[0] + rr * S * math.cos(a1),
                         c[1] + rr * S * math.sin(a1))], fill=col, width=1)
        else:
            d.ellipse(box, outline=col, width=1)
    # items
    for itm in pn.items:
        box = pn.rect_px(*itm["r"])
        col = COL[itm["kind"]]
        style = itm["style"]
        if itm["shape"] == "circle":
            if style == "solid":
                d.ellipse(box, fill=col, outline=darker(col))
            else:
                d.ellipse(box, outline=col, width=2)
        elif style == "solid":
            d.rectangle(box, fill=col, outline=darker(col))
        elif style == "under":  # submerged piece: hatched outline
            d.rectangle(box, outline=col, width=2)
            for k in range(int(box[0]) - int(box[3] - box[1]), int(box[2]), 6):
                a = (max(k, box[0]), box[3] - (max(k, box[0]) - k))
                b = (min(k + (box[3] - box[1]), box[2]),
                     box[3] - (min(k + (box[3] - box[1]), box[2]) - k))
                d.line([a, b], fill=col, width=1)
        else:  # dashed: overhead or another state's position
            dashed_rect(d, box, darker(col, 0.8), 2)
    for itm in pn.items:
        if itm["num"] is None:
            continue
        box = pn.rect_px(*itm["r"])
        bw, bh = box[2] - box[0], box[3] - box[1]
        s = str(itm["num"])
        tw = d.textlength(s, font=f_num)
        cx, cy = (box[0] + box[2]) / 2, (box[1] + box[3]) / 2
        fits = bw >= tw + 4 and bh >= 13 and itm["lab"] is None
        if fits:
            on = TXT_ON[itm["kind"]]
            fill = (255, 255, 255) if (on == "w" and itm["style"] == "solid") \
                else (30, 30, 30)
            d.text((cx, cy), s, font=f_num, fill=fill, anchor="mm")
            continue
        x0, y0, x1, y1 = itm["r"]
        if itm["lab"] is not None:
            dx, dy = itm["lab"]
        elif x0 < 0.4:
            dx, dy = -0.75, 0
        elif x1 > pn.w - 0.4:
            dx, dy = 0.75, 0
        elif y0 < 0.4:
            dx, dy = 0, -0.75
        elif y1 > pn.h - 0.4:
            dx, dy = 0, 0.75
        else:
            dx, dy = 0.7, 0
        lx, ly = cx + dx * S, cy - dy * S
        pn.top = min(pn.top, ly - 8)
        pn.bot = max(pn.bot, ly + 8)
        d.line([(cx, cy), (lx, ly)], fill=(90, 90, 90), width=1)
        d.ellipse([lx - 8, ly - 8, lx + 8, ly + 8], fill=(255, 255, 255),
                  outline=darker(COL[itm["kind"]], 0.8), width=2)
        d.text((lx, ly), s, font=f_num, fill=(30, 30, 30), anchor="mm")
    for (x, y, s, col, anchor) in pn.texts:
        d.text(pn.p(x, y), s, font=f_note, fill=col, anchor=anchor)
    return d


def wrap(d, s, font, width):
    lines, cur = [], ""
    for word in s.split():
        t = word if not cur else cur + " " + word
        if cur and d.textlength(t, font=font) > width:
            lines.append(cur)
            cur = word
        else:
            cur = t
    if cur:
        lines.append(cur)
    return lines


def draw_labels(d, pn):
    # drawn after every panel body, so no later panel covers a title or note
    d.text((pn.ox, pn.top - 8), pn.title, font=f_panel, fill=(30, 30, 30),
           anchor="ls")
    if pn.note:
        y = pn.bot + 6
        for i, s in enumerate(wrap(d, pn.note, f_note, pn.w * S + 56)):
            d.text((pn.ox, y + i * 14), s, font=f_note, fill=(80, 80, 80),
                   anchor="la")


def legend(d, panels, x, y, width, height, cols):
    entries = []
    for pn in panels:
        entries.append(("head", pn.key + "  " + pn.title.split("  ", 1)[-1],
                        None))
        seen = set()
        for itm in pn.items:
            if itm["num"] is None or itm["num"] in seen:
                continue
            seen.add(itm["num"])
            kn = KIND_NAME[itm["kind"]]
            tag = (" (" + kn + ")") if kn else ""
            if itm["style"] == "dashed" and itm["kind"] != "arch":
                tag += " - dashed"
            entries.append(("item", "%s  %s%s" % (itm["num"], itm["name"],
                                                  tag), itm["kind"]))
        entries.append(("gap", "", None))
    line_h = 16
    per_col = max(1, int(height // line_h))
    col_w = width / cols
    i = 0
    for kind, s, k in entries:
        c, r = divmod(i, per_col)
        if c >= cols:
            raise SystemExit("legend overflow at: " + s)
        if kind == "gap" and r == 0:
            continue
        lx, ly = x + c * col_w, y + r * line_h
        if kind == "head":
            d.text((lx, ly), s, font=f_legb, fill=(20, 20, 20), anchor="la")
        elif kind == "item":
            d.rectangle([lx, ly + 2, lx + 10, ly + 12], fill=COL[k],
                        outline=darker(COL[k]))
            d.text((lx + 16, ly), s, font=f_leg, fill=(40, 40, 40),
                   anchor="la")
        i += 1


KEEP = (110, 200, 110, 70)
ANKLE = (150, 222, 222, 140)
WADE = (70, 175, 190, 150)
DARKW = (40, 90, 110, 120)
PLAT = (226, 222, 210, 255)
BAY = (204, 200, 190, 255)
DECK = (230, 238, 240, 255)


def build():
    P = []
    # ---------------- Row 1: Run! ----------------
    y1 = 160
    a = Panel("A", "A  RN-SPINE", 70, y1, 3, 12,
              "1 x 4 shown, state nightClean")
    a.zone((0, 0, 3, 1.5), KEEP, None)
    a.zone((0, 10.8, 3, 12), KEEP, None)
    a.opening("S", 1.0, 2.0, "door")
    a.opening("N", 1.0, 2.0, "door")
    a.opening("W", 2.0, 3.0, "door", "J")
    a.opening("E", 2.2, 3.2, "door", "M")
    a.opening("E", 4.2, 5.2, "door", "E")
    a.opening("W", 7.4, 8.4, "door", "T")
    a.it(4, "Duct_3m (overhead)", "p2", (0.30, 0.05, 0.91, 11.95), "dashed",
         lab=(0, 0))
    a.it(1, "HoseCabinet", "p1", (2.88, 7.0, 3.0, 7.9))
    a.it(2, "ServiceElevatorFront", "p1", (0.0, 4.6, 0.16, 6.43))
    a.it(3, "TimeClockStation", "p1", (2.84, 9.9, 3.0, 10.7))
    a.it(5, "BulkTruck", "p2", (0.55, 3.3, 1.21, 4.21))
    a.it(6, "StepLadder (relamp state)", "p2", (2.2, 5.9, 2.7, 6.9),
         "dashed", lab=(-0.9, 0))
    a.it(7, "LampCarton (relamp state)", "p3", (2.62, 5.4, 2.86, 5.58),
         "dashed", lab=(-0.9, -0.3))
    a.it(8, "WetFloorSign", "p3", (1.0, 2.3, 1.3, 2.6), lab=(0.7, 0))
    a.it(9, "JanitorCart (Run! B)", "ex", (0.55, 9.0, 1.12, 10.33))
    a.it(10, "PullStation (Run! B)", "ex", (2.92, 1.6, 3.0, 1.74))
    a.it(11, "ExtinguisherCabinet (Run! B)", "ex", (2.89, 5.8, 3.0, 6.04))
    a.text(1.5, 0.75, "entry 1.5", (70, 110, 70))
    a.text(1.5, 11.4, "goal 1.2", (70, 110, 70))
    P.append(a)

    b = Panel("B", "B  RN-JAN", 200, y1, 3, 3, "event: cord under door")
    b.zone((0, 0, 3, 1.2), KEEP, None)
    b.opening("S", 1.74, 2.74, "door")
    b.it(1, "MopSink", "p1", (0.08, 2.31, 0.69, 2.92))
    b.it(2, "MopRack (over sink)", "p1", (0.08, 2.67, 0.99, 2.92), "dashed",
         lab=(0.5, 0.75))
    b.it(3, "FloorMachine", "p1", (0.08, 1.73, 0.63, 2.28))
    b.it(4, "SteelShelving", "p2", (2.46, 1.33, 2.92, 2.24))
    b.it(5, "StepLadder (folded)", "p2", (0.08, 1.24, 0.23, 1.68))
    b.it(6, "JanitorCart", "ex", (1.03, 2.35, 2.36, 2.92))
    b.it(7, "FloorDrain", "pool", (1.40, 1.40, 1.60, 1.60), lab=(0.55, 0))
    b.it(8, "CartonStack", "p3", (2.46, 2.30, 2.91, 2.92))
    b.line([(0.36, 1.73), (1.2, 1.0), (2.2, 0.05)], (120, 40, 40), 1, True)
    P.append(b)

    d_ = Panel("D", "D  RN-TEL", 200, y1 + 72 + 80, 3, 3)
    d_.zone((0, 0, 3, 1.2), KEEP, None)
    d_.opening("S", 0.28, 1.28, "door")
    d_.it(1, "TelephoneBackboard", "p1", (0.30, 2.80, 1.52, 2.92),
          lab=(0, 0.75))
    d_.it(1, "TelephoneBackboard", "p1", (0.08, 1.30, 0.20, 2.52))
    d_.it(4, "CableTray_3m (overhead)", "ex", (0.30, 2.55, 1.52, 2.80),
          "dashed", lab=(0.2, -0.75))
    d_.it(2, "RelayRack", "p1", (1.80, 2.38, 2.31, 2.76))
    d_.it(3, "TelecomCabinet", "p2", (2.67, 1.30, 2.92, 1.91))
    d_.it(5, "ButtSet", "p3", (0.86, 2.68, 0.98, 2.80), lab=(0.1, -0.9))
    d_.it(6, "StepLadder (midTask state)", "p2", (0.6, 1.4, 1.1, 2.4),
          "dashed", lab=(0.85, -0.2))
    P.append(d_)

    c = Panel("C", "C  RN-ELEC", 334, y1, 3, 6, "event: ATS on EMERGENCY")
    c.zone((0, 0, 3, 1.2), KEEP, None)
    c.zone((0.23, 1.6, 1.30, 4.56), None, None, (150, 120, 60), True)
    c.opening("S", 0.92, 1.92, "door")
    c.it(5, "RubberMat", "p3", (0.23, 1.6, 1.14, 3.6), "dashed",
         lab=(0.95, 0))
    for yy in (1.6, 2.3, 3.0):
        c.it(1, "Panelboard (208 V, 480 V, disconnect)", "p1",
             (0.08, yy, 0.23, yy + 0.51))
    c.it(1, "Panelboard", "p1", (2.77, 2.0, 2.92, 2.51))
    c.it(1, "Panelboard", "p1", (2.72, 4.0, 2.92, 4.38))
    c.it(3, "TransferSwitch", "p1", (0.08, 3.8, 0.49, 4.56))
    c.it(2, "DryTypeTransformer", "p1", (0.98, 5.31, 1.74, 5.92))
    c.it(6, "ExtinguisherCabinet", "ex", (2.81, 1.3, 2.92, 1.54))
    c.text(1.6, 4.85, "work 1.07", (140, 110, 50))
    P.append(c)

    e = Panel("E", "E  RN-MECH door", 334, y1 + 144 + 100, 3, 3,
              "event: propped on a wedge")
    e.opening("S", 0.0, 3.0, "arch")
    e.opening("N", 0.0, 3.0, "arch")
    e.it(3, "Duct_3m (overhead, turn)", "p2", (0.30, 0.05, 0.91, 2.95),
         "dashed", lab=(0, 0))
    e.it(3, "Duct_3m", "p2", (0.91, 1.2, 2.95, 1.81), "dashed", lab=(0, 0))
    e.it(4, "PipeRun insulated (overhead)", "pool", (1.5, 2.05, 2.95, 2.2),
         "dashed", lab=(-0.3, 0.6))
    e.it(1, "DoorLeaf_SteelLouver (+_DA)", "p1", (2.92, 1.0, 3.06, 2.0))
    e.it(2, "ValveChart", "p1", (2.90, 2.2, 3.0, 2.81))
    e.it(5, "CartonStack, filters (service state)", "p3",
         (2.30, 2.35, 2.86, 2.85), "dashed", lab=(-0.9, 0))
    e.it(6, "DoorWedge", "p3", (2.78, 1.9, 2.92, 1.98), lab=(-0.8, -0.35))
    P.append(e)

    f = Panel("F", "F  RN-DOCK  (3 x 4)", 470, y1, 9, 12,
              "event: door half up (1.2 m)")
    f.zone((0, 0, 9, 4.6), PLAT, None)
    f.zone((0, 4.6, 9, 12), BAY, None)
    f.zone((0, 0, 3, 1.2), KEEP, None)
    f.zone((6.4, 4.6, 8.8, 5.8), (190, 186, 172, 255), "stair", (90, 90, 90))
    f.zone((0.2, 4.6, 2.6, 5.8), None, "2nd way (R6)", (90, 90, 90), True)
    f.opening("S", 1.0, 2.0, "door")
    f.text(7.6, 3.5, "platform +1.22", (90, 90, 80))
    f.text(5.0, 9.6, "bay", (90, 90, 80))
    f.it(1, "OverheadCoilingDoor", "p1", (2.67, 11.82, 6.33, 12.12))
    f.it(2, "DockLeveler", "p1", (3.58, 2.16, 5.41, 4.60))
    f.it(3, "DockLightArm (post)", "p1", (3.12, 4.38, 3.28, 4.54),
         lab=(-0.7, -0.6))
    f.line([(3.2, 4.46), (4.5, 5.3)], darker(COL["p1"]), 2, True)
    f.it(4, "DockBumper", "p2", (3.30, 4.60, 3.66, 4.72), lab=(-0.3, 0.6))
    f.it(4, "DockBumper", "p2", (5.33, 4.60, 5.69, 4.72), lab=(0.3, 0.6))
    f.it(5, "WheelChock (hung)", "p2", (8.89, 6.2, 9.0, 6.45))
    f.it(5, "WheelChock", "p2", (8.89, 6.6, 9.0, 6.85), lab=(0.75, 0))
    for (bx, by) in ((2.45, 11.45), (6.55, 11.45), (6.1, 6.05)):
        f.it(6, "Bollard", "p2", (bx - 0.09, by - 0.09, bx + 0.09, by + 0.09),
             shape="circle", lab=(0.0, -0.55))
    f.it(7, "PalletJack", "p2", (6.1, 1.05, 6.79, 2.6))
    f.it(8, "PalletLoad_Wrapped (high, low)", "p2", (6.0, 0.08, 7.2, 1.0))
    f.it(8, "PalletLoad_Wrapped", "p2", (7.4, 0.08, 8.6, 1.0))
    f.it(9, "Pallet stack", "ex", (7.5, 1.4, 8.7, 2.4))
    f.it(10, "PlatformTruck", "p2", (2.2, 6.4, 2.96, 7.62))
    f.it(11, "ReceivingDesk", "p2", (0.08, 1.6, 0.69, 2.36))
    f.it(12, "DockEdgeGuard (chain, gate)", "p2", (0.0, 4.50, 6.4, 4.60),
         lab=(-2.6, -0.6))
    f.it(13, "SteelShelving (dock)", "p2", (3.4, 0.08, 4.31, 0.54))
    f.it(13, "SteelShelving", "p2", (4.35, 0.08, 5.26, 0.54))
    f.it(14, "CartonStack", "p3", (5.4, 0.08, 5.85, 0.38), lab=(0, 0.6))
    f.it(15, "DumpsterFrontLoad", "p3", (0.08, 8.0, 1.76, 9.98))
    f.it(16, "CCTVCamera", "p3", (2.0, 11.85, 2.2, 11.98), lab=(-0.6, -0.6))
    P.append(f)

    # ---------------- Row 2: club ----------------
    y2 = 560
    g = Panel("G", "G  PC-ENTRY  (2 x 2)", 70, y2, 6, 6, "event: next class in 10 min")
    g.zone((0, 0, 3, 1.0), KEEP, None)
    g.zone((3, 5.0, 6, 6), KEEP, None)
    g.opening("S", 0.7, 2.3, "arch")
    g.opening("N", 3.9, 5.1, "arch")
    g.it(1, "TowelDesk", "pool", (3.9, 1.4, 4.5, 3.2))
    g.it(2, "TowelCubbies", "p1", (5.52, 1.3, 5.92, 3.13))
    g.it(3, "LetterBoard", "p1", (0.8, 5.86, 1.71, 5.94))
    g.it(4, "ShowcaseCounter", "p2", (0.08, 2.6, 0.59, 3.82))
    g.it(5, "BeverageCooler", "p2", (0.08, 4.0, 0.79, 4.69))
    g.it(6, "ModularSofa", "sib", (3.6, 0.08, 4.4, 0.88))
    g.it(6, "ModularSofa", "sib", (4.45, 0.08, 5.25, 0.88))
    g.it(7, "WaterCooler", "ex", (2.2, 5.54, 2.61, 5.92))
    g.it(8, "PASpeaker (ceiling)", "p3", (1.35, 2.85, 1.65, 3.15), "dashed",
         shape="circle", lab=(0.6, 0))
    g.it(8, "PASpeaker", "p3", (4.85, 4.05, 5.15, 4.35), "dashed",
         shape="circle", lab=(0.6, 0))
    g.it(9, "LostAndFound", "p3", (4.6, 3.3, 5.05, 3.65), lab=(0.7, 0.3))
    g.it(10, "PlanterFicus", "sib", (0.15, 1.15, 0.6, 1.6), shape="circle",
         lab=(0.65, 0))
    P.append(g)

    hh = Panel("H", "H  PC-LOCKER  (2 x 3)", 260, y2, 6, 9,
               "event: a dryer left running")
    hh.zone((3, 0, 6, 1.0), KEEP, None)
    hh.zone((0, 8.0, 3, 9.0), KEEP, None)
    hh.zone((3, 6, 6, 9), (215, 235, 240, 255), "gang shower")
    hh.opening("S", 3.9, 5.1, "arch")
    hh.opening("N", 0.9, 2.1, "arch")
    hh.it(1, "LockerBank", "pool", (0.08, 1.0, 0.54, 3.29))
    hh.it(1, "LockerBank", "pool", (0.08, 3.4, 0.54, 5.69))
    hh.it(1, "LockerBank", "pool", (0.6, 0.08, 2.89, 0.54))
    hh.it(2, "LockerBench", "pool", (1.44, 1.6, 1.69, 3.43))
    hh.it(2, "LockerBench", "pool", (1.44, 3.86, 1.69, 5.69))
    hh.it(3, "BalanceScale", "p1", (5.39, 1.4, 5.92, 1.76))
    hh.it(4, "HairDryerStation", "p1", (5.77, 2.65, 5.92, 2.95))
    hh.it(4, "HairDryerStation", "p1", (5.77, 3.65, 5.92, 3.95))
    hh.it(5, "VanityCounter (no mirror, D7)", "p2", (5.36, 2.4, 5.77, 4.23))
    hh.it(6, "BulkTruck (towels)", "p2", (0.08, 6.3, 0.74, 7.21))
    for xx in (3.6, 4.5, 5.4):
        hh.it(7, "ShowerSet", "pool", (xx - 0.15, 8.8, xx + 0.15, 8.92),
              lab=(0, 0.6))
    hh.it(8, "GlassBlockHalfWall", "pool", (3.0, 5.95, 4.9, 6.10),
          lab=(0, -0.55))
    hh.it(9, "FloorDrain", "pool", (4.4, 7.4, 4.6, 7.6), lab=(0.55, 0))
    hh.it(10, "TowelHookRail (drying zone)", "p2", (5.84, 4.6, 5.92, 5.82))
    hh.it(11, "GymBag", "p3", (1.42, 4.4, 1.70, 4.95), lab=(0.6, 0))
    hh.it(12, "PASpeaker (ceiling)", "p3", (2.35, 4.35, 2.65, 4.65),
          "dashed", shape="circle", lab=(0.6, 0))
    hh.it(13, "Padlock hanging open", "ex", (0.54, 2.25, 0.62, 2.35),
          lab=(0.6, 0))
    P.append(hh)

    ii = Panel("I", "I  PC-WET (later, D7)", 450, y2, 6, 6,
               "showers: see H (NE cell)")
    ii.zone((0, 0, 3, 1.0), KEEP, None)
    ii.opening("S", 1.0, 2.2, "arch")
    ii.it(1, "ToiletPartition (3 stalls)", "p1", (3.0, 4.40, 5.73, 5.92),
          "dashed", lab=(-0.6, 0.0))
    ii.line([(3.91, 4.40), (3.91, 5.92)], darker(COL["p1"]), 2)
    ii.line([(4.82, 4.40), (4.82, 5.92)], darker(COL["p1"]), 2)
    ii.line([(3.0, 4.40), (5.73, 4.40)], darker(COL["p1"]), 2)
    ii.it(2, "UrinalWallHung", "p1", (5.56, 1.6, 5.92, 2.06))
    ii.it(2, "UrinalWallHung", "p1", (5.56, 2.5, 5.92, 2.96))
    for xx in (3.45, 4.36, 5.27):
        ii.it(3, "WCWallHung", "p2", (xx - 0.18, 5.26, xx + 0.18, 5.92))
    ii.it(4, "LavatoryWallHung (tiled blank above)", "p2",
          (0.08, 2.0, 0.54, 2.51))
    ii.it(4, "LavatoryWallHung", "p2", (0.08, 2.8, 0.54, 3.31))
    ii.it(5, "TowelWasteCombo", "p2", (0.08, 3.6, 0.18, 4.01))
    ii.it(6, "HandDryer", "p3", (0.08, 1.4, 0.28, 1.68))
    ii.it(7, "FloorDrain", "pool", (2.9, 2.9, 3.1, 3.1), lab=(0.55, 0))
    P.append(ii)

    j = Panel("J", "J  PL_Court = PC-AERO  (2 x 4)", 640, y2, 6, 12,
              "event: tape run out, hiss")
    j.zone((0, 0, 6, 6), WADE, "Wade (no props)")
    j.zone((0, 6, 6, 12), ANKLE, None)
    j.opening("S", 0.0, 6.0, "glass")
    j.line([(0, 7.43), (6, 7.43)], (200, 60, 60), 1, True)
    j.line([(0, 5.9), (6, 5.9)], (200, 60, 60), 1, True)
    j.text(3.0, 0.5, "glass back wall", (40, 120, 150))
    j.it(1, "StereoComponents", "p1", (5.1, 11.5, 5.56, 11.86))
    j.it(2, "AVCart", "sib", (4.9, 11.46, 5.81, 11.92), lab=(0.2, -0.75))
    j.it(3, "InstructorPlatform", "p2", (2.08, 10.62, 3.91, 11.84))
    j.it(4, "MatStack", "p2", (0.08, 6.4, 0.69, 7.62))
    j.it(4, "MatStack (one unfolded)", "p2", (2.4, 8.0, 3.62, 9.83),
         "dashed")
    j.it(5, "BalletBarre", "p2", (5.72, 6.6, 5.92, 9.0))
    j.it(6, "PASpeaker (wall box)", "p3", (0.08, 11.6, 0.38, 11.82))
    j.it(6, "PASpeaker", "p3", (5.62, 10.8, 5.92, 11.02))
    j.it(7, "HandWeightRack", "p3", (0.08, 8.2, 0.44, 8.81))
    j.text(3.0, 7.75, "service line", (190, 70, 70))
    P.append(j)

    # ---------------- Row 3: pool ----------------
    y3 = 940
    k = Panel("K", "K  PC-HALL  (5 x 8)", 70, y3, 15, 24,
              "event: towel and sandals by lounger 3")
    k.zone((0, 0, 15, 24), DECK, None)
    k.zone((3, 3, 12, 21), WADE, None)
    k.zone((3, 3, 12, 6), ANKLE, "beach entry")
    k.zone((0, 0, 3, 1.0), KEEP, None)
    k.zone((6, 23.0, 9, 24), KEEP, None)
    k.opening("S", 0.9, 2.1, "arch")
    k.opening("N", 6.8, 8.2, "arch")
    k.line([(12, 9), (12, 15)], (60, 160, 200), 3, True)
    k.text(7.5, 13.0, "basin 9 x 18, Wade", (30, 90, 100))
    k.text(12.8, 12.0, "obs", (40, 120, 150))
    k.it(1, "GuardChair", "pool", (0.6, 11.4, 1.5, 12.6))
    k.it(2, "PaceClock", "pool", (10.3, 23.82, 10.7, 23.95))
    for yy in (4.0, 6.3, 8.6, 10.9, 13.2, 15.5):
        k.it(3, "DeckLounger (chaise, chair)", "p1",
             (14.2, yy, 14.84, yy + 1.93))
    k.it(3, "DeckLounger (chair)", "p1", (13.4, 18.2, 13.98, 18.81))
    k.it(3, "DeckLounger (chair)", "p1", (13.4, 19.2, 13.98, 19.81))
    k.it(4, "KickboardRack", "p1", (14.6, 1.2, 14.92, 2.44))
    k.it(5, "SpineBoard", "p2", (0.08, 13.3, 0.2, 13.71))
    k.it(6, "FirstAidCabinet", "p2", (0.08, 10.2, 0.23, 10.61))
    k.it(7, "PoolCoverReel (9.3 m)", "p2", (2.55, 1.0, 12.45, 1.9))
    k.it(8, "LockerBench (deck bench)", "pool", (0.08, 4.0, 0.33, 5.83))
    k.it(9, "WaterCooler", "ex", (0.08, 7.8, 0.46, 8.21))
    k.it(10, "PASpeaker (sealed)", "p3", (1.35, 17.85, 1.65, 18.15),
         "dashed", shape="circle", lab=(0.6, 0))
    k.it(10, "PASpeaker", "p3", (13.35, 2.85, 13.65, 3.15), "dashed",
         shape="circle", lab=(-0.6, 0))
    k.it(11, "RescueTube", "p3", (0.7, 12.0, 1.4, 12.12), lab=(1.2, 0.5))
    k.it(12, "PoolCleaningSet", "p3", (0.08, 19.0, 0.33, 21.5))
    k.it(13, "WetFloorSign", "p3", (1.6, 1.15, 1.9, 1.45), lab=(0, 0.6))
    k.it(14, "PoolLadder", "pool", (4.3, 20.7, 4.7, 21.0), lab=(0, 0.6))
    k.it(14, "PoolLadder", "pool", (10.3, 20.7, 10.7, 21.0), lab=(0, 0.6))
    k.it(15, "RingBuoy, phone, rescue set", "pool", (0.08, 11.0, 0.2, 11.3))
    P.append(k)

    l_ = Panel("L", "L  PL-SLIDE start  (3 x 4)", 520, y3, 9, 12,
               "event: light turns green, nobody")
    l_.zone((0, 0, 6, 3), None, None, (90, 90, 90), True)
    l_.zone((4.8, 0.4, 6.0, 1.6), KEEP, None)
    l_.text(3.0, 2.6, "start platform (upper)", (90, 90, 90))
    l_.circles.append((1.5, 1.5, 1.2, (110, 110, 110), False))
    l_.text(1.5, 1.5, "spiral", (90, 90, 90))
    l_.line([(6.0, 1.0), (7.6, 1.6), (8.4, 3.2), (9.0, 6.0)],
            (120, 120, 200), 6, True)
    l_.text(7.0, 4.4, "flume (61)", (90, 90, 170))
    l_.line([(0, 3.0), (6, 3.0)], (90, 90, 90), 2)
    l_.it(1, "SlideTrafficLight", "p1", (5.38, 0.86, 5.62, 1.14),
          lab=(0.6, 0.8))
    l_.it(2, "SlideGate", "p1", (2.68, 0.9, 2.78, 1.66), lab=(0.1, 0.9))
    l_.it(3, "DispatcherPost", "p2", (4.0, 1.8, 4.51, 2.21), lab=(0.0, 0.7))
    l_.it(4, "RulesBoard (slide face)", "pool", (0.0, 3.4, 0.1, 4.3))
    l_.it(5, "RideMatStack", "p3", (3.1, 0.1, 4.1, 0.6))
    P.append(l_)

    sp = Panel("L'", "L' splash pool (in PL_Leisure)", 520, y3 + 288 + 80,
               6, 6)
    sp.zone((0, 0, 6, 6), ANKLE, None)
    sp.zone((1.5, 1.5, 4.5, 5.9), WADE, "0.9 m?")
    sp.zone((2.4, 1.5, 3.6, 2.1), (190, 186, 172, 255), None, (90, 90, 90))
    sp.line([(2.5, 6.0), (3.5, 6.0)], (120, 120, 200), 6)
    sp.text(3.0, 5.4, "slide mouth", (90, 90, 170))
    sp.it(1, "GuardChair (low)", "pool", (4.9, 3.0, 5.7, 3.8))
    sp.it(2, "RingBuoy", "pool", (5.85, 2.0, 5.95, 2.4))
    sp.it(3, "CCTVCamera", "p3", (0.05, 4.8, 0.2, 5.2))
    sp.it(4, "RulesBoard", "pool", (0.0, 2.0, 0.1, 2.9))
    sp.it(5, "RecessedSteps", "pool", (2.4, 1.5, 3.6, 2.1), lab=(0, -0.9))
    P.append(sp)

    m = Panel("M", "M  PL-TRACE  (PL_Chain 2 x 2)", 800, y3, 6, 6,
              "1 trace in 1 room of 3")
    m.zone((0, 0, 6, 6), ANKLE, None)
    m.zone((3, 0, 6, 1.0), KEEP, None)
    m.zone((3, 5.0, 6, 6), KEEP, None)
    m.opening("S", 3.8, 5.2, "arch")
    m.opening("N", 3.8, 5.2, "arch")
    m.it(1, "DeckLounger facing the wall", "p1", (0.1, 2.5, 2.03, 3.14))
    m.it(2, "towel, dry (event)", "pool", (0.9, 2.6, 1.4, 3.04),
         lab=(0, 0.8))
    P.append(m)

    n = Panel("N", "N  PL-DARK  (PL_BasinRoom copy)", 800, y3 + 144 + 70,
              12, 12, "water 1.02 m everywhere")
    n.zone((0, 0, 12, 12), DARKW, None)
    for (cx, cy) in ((0, 3), (0, 9), (12, 3), (12, 9)):
        n.circles.append((cx, cy, 1.5, (60, 150, 60), True))
        n.it(None, "niche lamp", "arch",
             (cx - 0.12 if cx else 0.0, cy - 0.2,
              cx if cx else 0.12, cy + 0.2))
    n.circles.append((6, 12, 1.5, (60, 150, 60), True))
    n.line([(5.0, 12), (7.0, 12)], (70, 170, 200), 5)
    n.text(6.0, 11.1, "window", (40, 120, 150))
    n.text(6.0, 6.0, "clear 1.5 m before lamps and windows", (60, 110, 60))
    n.it(1, "GuardChair (facing a wall)", "pool", (9.4, 1.0, 10.2, 1.8))
    n.it(2, "DeckLounger (drowned, under water)", "p1",
         (0.8, 8.0, 1.44, 9.93), "under", lab=(1.0, 0))
    n.it(3, "RescueTube (dry, on a hook)", "p3", (6.0, 0.05, 7.22, 0.2),
         lab=(0, -0.7))
    P.append(n)
    return P


def main():
    img = Image.new("RGBA", (W_PX, H_PX), (255, 255, 255, 255))
    d = ImageDraw.Draw(img)
    d.text((40, 30), "63  ROOM PROGRAMMES: RUN! AND THE POOL ROOMS",
           font=f_title, fill=(20, 20, 20), anchor="la")
    d.text((40, 72), "One instance per programme, top views, north up, "
           "1 m grid (3 m cells heavier). Numbers refer to the key beside "
           "each row. Solid = present in the drawn state; dashed = overhead, "
           "wall-hung over another piece, or another state's position.",
           font=f_sub, fill=(70, 70, 70), anchor="la")
    panels = build()
    for pn in panels:
        d = draw_panel(img, d, pn)
    d = ImageDraw.Draw(img)
    for pn in panels:
        draw_labels(d, pn)
    rows = {
        "run": [p for p in panels if p.key in "ABCDEF" and len(p.key) == 1],
        "club": [p for p in panels if p.key in ("G", "H", "I", "J")],
        "pool": [p for p in panels if p.key in ("K", "L", "L'", "M", "N")],
    }
    d.text((1040, 120), "RUN!", font=f_panel, fill=(150, 40, 30),
           anchor="la")
    legend(d, sorted(rows["run"], key=lambda p: p.key), 1040, 140, 1520,
           352, 4)
    d.text((1040, 520), "POOL CLUB", font=f_panel, fill=(30, 110, 110),
           anchor="la")
    legend(d, rows["club"], 1040, 540, 1520, 300, 4)
    d.text((1150, 900), "POOL HALL AND POOL LEVEL", font=f_panel,
           fill=(30, 110, 110), anchor="la")
    legend(d, rows["pool"], 1150, 920, 1410, 320, 3)
    # colour key
    ky = 1290
    d.text((1150, ky), "KEY", font=f_panel, fill=(20, 20, 20), anchor="la")
    keys = [("p1", "new kit, P1 signature"), ("p2", "new kit, P2 working set"),
            ("p3", "new kit, P3 clutter"), ("ex", "existing kit"),
            ("pool", "pool line's kit (POOL)"), ("sib", "sibling 62's kit")]
    for i, (kk, s) in enumerate(keys):
        x = 1150 + (i % 3) * 470
        y = ky + 24 + (i // 3) * 22
        d.rectangle([x, y + 2, x + 14, y + 14], fill=COL[kk],
                    outline=darker(COL[kk]))
        d.text((x + 22, y), s, font=f_leg, fill=(40, 40, 40), anchor="la")
    zkeys = [(KEEP, "keep-clear strip (1.0 m arch, 1.2 m door, Run! entry "
              "1.5 m)"), (ANKLE, "Ankle water"), (WADE, "Wade water"),
             (DARKW, "dark zone water")]
    ov = Image.new("RGBA", img.size, (0, 0, 0, 0))
    od = ImageDraw.Draw(ov)
    for i, (c, s) in enumerate(zkeys):
        x = 1150 + (i % 2) * 700
        y = ky + 76 + (i // 2) * 22
        od.rectangle([x, y + 2, x + 14, y + 14], fill=c)
    img.alpha_composite(ov)
    d = ImageDraw.Draw(img)
    for i, (c, s) in enumerate(zkeys):
        x = 1150 + (i % 2) * 700
        y = ky + 76 + (i // 2) * 22
        d.rectangle([x, y + 2, x + 14, y + 14], outline=(120, 120, 120))
        d.text((x + 22, y), s, font=f_leg, fill=(40, 40, 40), anchor="la")
    y = ky + 128
    d.line([(1150, y + 8), (1190, y + 8)], fill=COL["door"], width=3)
    d.text((1198, y), "door (letters: J janitor, M mechanical, E electrical, "
           "T telephone)", font=f_leg, fill=(40, 40, 40), anchor="la")
    d.line([(1850, y + 8), (1890, y + 8)], fill=(70, 170, 200), width=4)
    d.text((1898, y), "glass wall / observation window", font=f_leg,
           fill=(40, 40, 40), anchor="la")
    d.text((1150, y + 30), "Sources and rules: 63_furniture_run_pool.md "
           "(LS100-LS119). Script: W/level_space/make_plans_63.py. "
           "2026-10-31.", font=f_note, fill=(110, 110, 110), anchor="la")
    img.convert("RGB").save(OUT, optimize=True)
    print("wrote", OUT, img.size)


if __name__ == "__main__":
    main()
