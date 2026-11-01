#!/usr/bin/env python3
"""Schematic figures for research/level_space/10_research.md (K research sub-chat, 2026-10-31).

Pure Pillow, no downloads. Writes PNGs into the research folder's images/.
Run: /Users/redwang/FrontRoomsVisualWork/venv/bin/python make_figs.py
"""
import os
from PIL import Image, ImageDraw, ImageFont

OUT = ("/Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01 Individual Game Project/"
       "Frontrooms3D/Documentation/research/level_space/images")
os.makedirs(OUT, exist_ok=True)
FD = "/System/Library/Fonts/Supplemental/"


def F(size, bold=False):
    return ImageFont.truetype(FD + ("Arial Bold.ttf" if bold else "Arial.ttf"), size)


PAPER = (244, 242, 236)
INK = (32, 32, 32)
GREY = (140, 140, 140)
LIGHT = (214, 210, 202)
WHITE = (255, 255, 255)
COL = {
    "LOBBY": (190, 158, 104), "L0": (218, 196, 112), "OF": (146, 170, 196), "CORE": (172, 172, 168),
    "RUN": (176, 72, 62), "CLUB": (190, 128, 136), "POOL": (120, 196, 196), "WATER": (40, 150, 156),
    "LEVEL": (176, 222, 220), "DARK": (18, 50, 56), "STAIR": (112, 112, 112), "EARTH": (200, 194, 182),
}


def hatch(d, box, step=12, col=LIGHT, width=2):
    x0, y0, x1, y1 = box
    for k in range(int(x0 - (y1 - y0)), int(x1), step):
        a = (max(k, x0), y1 - (max(k, x0) - k))
        b = (min(k + (y1 - y0), x1), y1 - (min(k + (y1 - y0), x1) - k))
        d.line([a, b], fill=col, width=width)


def label(d, xy, text, size=22, bold=False, fill=INK, anchor="la"):
    d.text(xy, text, font=F(size, bold), fill=fill, anchor=anchor)


def block(d, box, col, text=None, size=20, plenum=0, tcol=None):
    x0, y0, x1, y1 = box
    d.rectangle(box, fill=col)
    if plenum:
        d.rectangle((x0, y0, x1, y0 + plenum), fill=PAPER)
        hatch(d, (x0, y0, x1, y0 + plenum), step=10, col=LIGHT, width=2)
        d.line([(x0, y0 + plenum), (x1, y0 + plenum)], fill=INK, width=2)
    d.rectangle(box, outline=INK, width=2)
    if text:
        lum = 0.3 * col[0] + 0.59 * col[1] + 0.11 * col[2]
        label(d, ((x0 + x1) / 2, (y0 + plenum + y1) / 2), text, size, True,
              tcol or (WHITE if lum < 120 else INK), "mm")


def stair(d, x0, y0, x1, y1):
    d.rectangle((x0, y0, x1, y1), fill=PAPER, outline=INK, width=2)
    n = 7
    for i in range(n + 1):
        y = y0 + (y1 - y0) * i / n
        x = x0 + (x1 - x0) * i / n
        d.line([(x0, y), (x, y)], fill=COL["STAIR"], width=2)
        d.line([(x, y), (x, y + (y1 - y0) / n)], fill=COL["STAIR"], width=2)


def fig1():
    W, H = 2400, 1190
    im = Image.new("RGB", (W, H), PAPER)
    d = ImageDraw.Draw(im)
    label(d, (60, 36), "Fig. 1 · One building, one layer per level", 42, True)
    label(d, (60, 92), "Schematic section of the FrontRooms building, not to scale. LEVEL 0 is the ground floor and the "
          "highest floor you can reach; each STAIR A serves one floor and leads one floor down.", 24, False, GREY)

    g = 470
    for i, yt in enumerate((170, 270, 370)):
        d.rectangle((60, yt, 1520, yt + 100), outline=GREY, width=2)
    label(d, (790, 230), "upper floors: never reached", 24, False, GREY, "mm")
    d.rectangle((60, 370, 1520, g), fill=PAPER)

    block(d, (60, 360, 300, g), COL["LOBBY"], "LOBBY / station", 20, plenum=12)
    block(d, (300, 370, 860, g), COL["L0"], "LEVEL 0 · spec suites", 22, plenum=30)
    block(d, (860, 370, 1000, g), COL["CORE"], "core", 20, plenum=30)
    block(d, (1000, 370, 1520, g), COL["OF"], "OFFICE district", 22, plenum=30)
    d.line([(20, g), (1560, g)], fill=INK, width=5)
    label(d, (24, g - 30), "GRADE", 20, True)

    b1 = g + 112
    block(d, (60, g, 540, b1), COL["L0"], "LEVEL 0 districts", 22, plenum=26)
    stair(d, 900, 400, 960, g)
    block(d, (600, g, 900, b1), COL["OF"], "OFFICE", 22, plenum=26)
    block(d, (900, g, 1040, b1), COL["CORE"], "core", 20, plenum=26)
    block(d, (1040, g, 1520, b1), COL["RUN"], "RUN! service spine", 22, plenum=40)
    for x in range(1060, 1510, 36):
        d.ellipse((x, g + 42, x + 12, g + 54), outline=WHITE, width=2)
    stair(d, 540, g + 30, 600, b1)
    label(d, (20, (g + b1) / 2), "B1", 26, True, INK, "lm")

    b2 = b1 + 112
    deep = b2 + 120
    block(d, (60, b1, 480, b2), COL["CLUB"], "CLUB · lobby, lockers, showers", 20, plenum=34)
    d.rectangle((480, b1, 860, deep), fill=COL["POOL"], outline=INK, width=2)
    d.rectangle((480, deep - 46, 860, deep), fill=COL["WATER"])
    d.rectangle((480, b1, 860, deep), outline=INK, width=2)
    label(d, (670, b1 + 52), "POOL HALL · 5.4 m+", 22, True, INK, "mm")
    label(d, (670, deep - 23), "basin", 18, True, WHITE, "mm")
    d.rectangle((860, b1, 1290, b2), fill=COL["LEVEL"], outline=INK, width=2)
    d.rectangle((860, b2 - 22, 1290, b2), fill=COL["WATER"])
    for x0, x1, top in ((860, 1000, b1 + 40), (1000, 1150, b1 + 4), (1150, 1290, b1 + 52)):
        d.line([(x0, top), (x1, top)], fill=INK, width=2)
        d.line([(x1, b1), (x1, b2 - 22)], fill=INK, width=2)
    label(d, (1075, b1 + 60), "POOL LEVEL · flooded rooms", 20, True, INK, "mm")
    stair(d, 1290, b2 - 22, 1360, deep)
    d.rectangle((1360, b2, 1520, deep), fill=COL["DARK"], outline=INK, width=2)
    d.rectangle((1360, deep - 30, 1520, deep), fill=(28, 92, 98))
    label(d, (1440, b2 + 44), "DARK ZONE", 20, True, WHITE, "mm")
    label(d, (1440, b2 + 70), "(F3, fixed)", 18, False, WHITE, "mm")
    label(d, (20, (b1 + b2) / 2), "B2", 26, True, INK, "mm")
    for box in ((60, b2, 480, deep), (860, b2, 1290, deep)):
        d.rectangle(box, fill=COL["EARTH"])
        hatch(d, box, step=16, col=(184, 177, 163), width=2)
    gw = deep + 46
    d.rectangle((60, deep, 1520, gw), fill=(206, 214, 216))
    hatch(d, (60, deep, 1520, gw), step=14, col=(172, 186, 190), width=2)
    label(d, (790, deep + 23), "SITE · ground water", 20, True, (70, 90, 96), "mm")
    label(d, (60, gw + 26), "Hatched band under each slab = the ~1.1 m plenum above a 9 ft lay-in ceiling (77 W Wacker: 13 ft "
          "unfinished, 9 ft finished). Placement of districts is illustrative.", 20, False, GREY)
    label(d, (60, gw + 56), "The club sits on B2 and opens into the pool level (D1 c); the fire stair goes down into the dark zone "
          "(18 §3).", 20, False, GREY)

    x = 1590
    label(d, (x, 170), "What each level makes visible", 30, True)
    label(d, (x, 210), "Shearing layers: Brand 1994, after Duffy 1992", 21, False, GREY)
    rows = [
        ("L0", "LEVEL 0", "SPACE PLAN (scenery)", "partitions, dropped ceiling · 3–7 years",
         "partitions with no tenant and no logic"),
        ("OF", "OFFICE", "STUFF (set)", "furniture, panels · weeks to months", "the furniture becomes the walls"),
        ("RUN", "RUN!", "SERVICES", "pipes, ducts, power · 7–15 years", "the building's guts become the room"),
        ("POOL", "POOL + DARK ZONE", "STRUCTURE + SITE", "the box, columns, ground water · 30–300 yrs",
         "the structure holds the water it should keep out"),
        ("STAIR", "STAIRWELL", "EGRESS (code)", "concrete, rail, landing", "never goes wrong: the safe zone"),
        ("LOBBY", "LOBBY / STATION", "THE ONE 'ARCHITECTURE'", "a spec tower spends on its lobby",
         "the building tries to impress"),
    ]
    y = 262
    for key, name, layer, rate, wrong in rows:
        d.rectangle((x, y + 4, x + 34, y + 38), fill=COL[key], outline=INK, width=2)
        label(d, (x + 50, y), name, 23, True)
        label(d, (x + 50, y + 30), layer, 21, True, (90, 90, 90))
        label(d, (x + 50, y + 58), rate, 20, False, GREY)
        label(d, (x + 50, y + 84), "goes wrong: " + wrong, 20, False, INK)
        y += 132
    d.line([(x + 14, y + 6), (x + 14, y + 90)], fill=INK, width=4)
    d.polygon([(x + 4, y + 82), (x + 24, y + 82), (x + 14, y + 104)], fill=INK)
    label(d, (x + 44, y + 22), "down the building = slower layers:", 22, True)
    label(d, (x + 44, y + 52), "heavier, older, wetter, less plausible", 22, False)
    im.save(os.path.join(OUT, "ls_fig1_building_layers.png"))


def figure(d, cx, foot, s, h, eye, col, w, name):
    top = foot - h * s
    d.rounded_rectangle((cx - w / 2, top + 0.22 * s, cx + w / 2, foot), radius=int(w / 2), fill=col)
    d.ellipse((cx - w * 0.42, top, cx + w * 0.42, top + 0.26 * s), fill=col)
    ey = foot - eye * s
    d.ellipse((cx - 6, ey - 6, cx + 6, ey + 6), fill=WHITE, outline=INK, width=2)
    label(d, (cx, foot + 18), name, 20, True, INK, "ma")


def fig2():
    W, H = 2400, 1080
    im = Image.new("RGB", (W, H), PAPER)
    d = ImageDraw.Draw(im)
    label(d, (60, 36), "Fig. 2 · Office: the panel height is the structure", 42, True)
    label(d, (60, 92), "Section, heights to scale (1 m = 230 px), distances not to scale; the panel stands midway. "
          "Player 1.75 m, eye 1.62 m; Relay 2.05 m, eye 1.60 m (ModuleUnits).", 24, False, GREY)
    s, foot = 230, 900
    PE, PH, RE, RH = 1.62, 1.75, 1.60, 2.05
    cases = [
        (1.35, "53 in · 1.35 m", "proposed LOW", "PROSPECT", "both see each other", (70, 140, 80)),
        (1.52, "60 in · 1.52 m", "kit standard today", "AMBIGUOUS", "sight grazes the top", (196, 140, 40)),
        (1.65, "65 in · 1.65 m", "kit tall spine (15 % today)", "REFUGE", "you see its head; it can't see you",
         (40, 100, 170)),
        (2.16, "85 in · 2.16 m", "proposed FULL HEIGHT", "MAZE", "panels are walls", (120, 60, 60)),
    ]
    pw = 560
    for i, (ph, title, sub, verdict, why, vc) in enumerate(cases):
        x0 = 60 + i * (pw + 20)
        d.rectangle((x0, 140, x0 + pw, H - 60), outline=LIGHT, width=2)
        label(d, (x0 + 20, 156), title, 28, True)
        label(d, (x0 + 20, 194), sub, 21, False, GREY)
        ceil = foot - 2.9 * s
        for xx in range(x0 + 10, x0 + pw - 10, 24):
            d.line([(xx, ceil), (xx + 12, ceil)], fill=GREY, width=2)
        label(d, (x0 + pw - 16, ceil - 8), "ceiling 2.9 m", 18, False, GREY, "rb")
        d.line([(x0 + 10, foot), (x0 + pw - 10, foot)], fill=INK, width=3)
        xp, xm, xr = x0 + 80, x0 + pw / 2, x0 + pw - 80
        d.rectangle((xm - 7, foot - ph * s, xm + 7, foot), fill=(96, 92, 86))
        figure(d, xp, foot, s, PH, PE, (90, 120, 150), 44, "player")
        figure(d, xr, foot, s, RH, RE, (40, 40, 44), 50, "Relay")
        # player's eye over the panel top to the Relay's line
        y_r = PE + (ph - PE) * 2.0
        y_p = RE + (ph - RE) * 2.0
        vis_r = max(0.0, RH - max(y_r, 0.0)) if y_r < RH else 0.0
        vis_p = max(0.0, PH - max(y_p, 0.0)) if y_p < PH else 0.0
        for (ex, ey, tx, ty, vis, c) in ((xp, PE, xr, y_r, vis_r, (90, 120, 150)), (xr, RE, xp, y_p, vis_p, (40, 40, 44))):
            if vis > 0:
                d.line([(ex, foot - ey * s), (xm, foot - ph * s), (tx, foot - ty * s)], fill=c, width=3)
            else:
                d.line([(ex, foot - ey * s), (xm, foot - ey * s)], fill=c, width=3)
                cx, cy = xm - (10 if ex < xm else -10), foot - ey * s
                d.line([(cx - 9, cy - 9), (cx + 9, cy + 9)], fill=(220, 70, 50), width=4)
                d.line([(cx - 9, cy + 9), (cx + 9, cy - 9)], fill=(220, 70, 50), width=4)
        if 0 < vis_r < RH:
            d.rectangle((xr - 34, foot - RH * s, xr + 34, foot - max(y_r, 0) * s), outline=(220, 70, 50), width=4)
        if 0 < vis_p < PH:
            d.rectangle((xp - 30, foot - PH * s, xp + 30, foot - max(y_p, 0) * s), outline=(220, 70, 50), width=4)
        r_txt = "Relay seen: top %.2f m" % vis_r if vis_r > 0 else "Relay hidden"
        p_txt = "player seen: top %.2f m" % vis_p if vis_p > 0 else "player hidden"
        label(d, (x0 + 20, 236), r_txt, 21, True, INK)
        label(d, (x0 + 20, 266), p_txt, 21, True, INK)
        label(d, (x0 + 20, foot + 60), verdict, 30, True, vc)
        label(d, (x0 + 20, foot + 98), why, 21, False, INK)
    label(d, (60, H - 46), "Period panels: Action Office 2 at 32/39/47/53/62/67/85 in; 66 in was the norm until c.1993, when 42 in "
          "came in (Chicago Tribune 1993-10-03). Red boxes = what the other one can see over the panel.", 20, False, GREY)
    im.save(os.path.join(OUT, "ls_fig2_office_sightlines.png"))


def strip(d, x0, x1, y0, y1, segs, col, today=None, notes=(), name=""):
    mx, hm = 60.0, 6.0

    def X(m):
        return x0 + (x1 - x0) * m / mx

    def Y(h):
        return y1 - (y1 - y0) * h / hm
    for hh in (2.4, 2.9, 5.4):
        d.line([(x0, Y(hh)), (x1, Y(hh))], fill=LIGHT, width=1)
        label(d, (x0 - 10, Y(hh)), "%.1f" % hh, 17, False, GREY, "rm")
    d.line([(x0, y1), (x1, y1)], fill=INK, width=2)
    for a, b, h in segs:
        d.rectangle((X(a), Y(h), X(b), y1), fill=col)
    pts = []
    for a, b, h in segs:
        pts += [(X(a), Y(h)), (X(b), Y(h))]
    d.line(pts, fill=INK, width=3)
    if today:
        tp = []
        for a, b, h in today:
            tp += [(X(a), Y(h)), (X(b), Y(h))]
        for k in range(0, len(tp) - 1):
            (ax, ay), (bx, by) = tp[k], tp[k + 1]
            n = max(1, int(abs(bx - ax) + abs(by - ay)) // 14)
            for j in range(0, n, 2):
                p0 = (ax + (bx - ax) * j / n, ay + (by - ay) * j / n)
                p1 = (ax + (bx - ax) * (j + 1) / n, ay + (by - ay) * (j + 1) / n)
                d.line([p0, p1], fill=(110, 110, 110), width=3)
    for m, h, t in notes:
        label(d, (X(m), Y(h) - 8), t, 17, False, INK, "mb")
    label(d, (40, (y0 + y1) / 2 - 14), name, 26, True, INK, "lm")


def fig3():
    W, H = 2400, 1400
    im = Image.new("RGB", (W, H), PAPER)
    d = ImageDraw.Draw(im)
    label(d, (60, 36), "Fig. 3 · Ceiling-height rhythm along a route: today (dashed) and proposed (filled)", 42, True)
    label(d, (60, 92), "Ceiling height in metres over 60 m of walking. Alexander 190: \"A building in which ceiling heights are all "
          "the same is virtually incapable of making people feel comfortable.\"", 24, False, GREY)
    x0, x1 = 300, 2340
    rows = [
        ("LEVEL 0", COL["L0"],
         [(0, 9, 3.6), (9, 15, 2.4), (15, 27, 2.9), (27, 30, 4.0), (30, 36, 2.4), (36, 48, 5.4), (48, 60, 2.9)],
         [(0, 12, 2.9), (12, 18, 2.4), (18, 36, 2.9), (36, 42, 5.4), (42, 60, 2.9)],
         [(4.5, 3.6, "lobby band 12 ft"), (12, 2.4, "soffit at core"), (21, 2.9, "suites"),
          (28.5, 4.0, "missing tiles: plenum"), (33, 2.4, "storage"), (42, 5.4, "atrium + gallery windows"),
          (54, 2.9, "suites")]),
        ("OFFICE", COL["OF"],
         [(0, 8.5, 2.9), (8.5, 9.5, 2.55), (9.5, 17.5, 2.9), (17.5, 18.5, 2.55), (18.5, 26.5, 2.9), (26.5, 27.5, 2.55),
          (27.5, 36, 2.9), (36, 42, 2.4), (42, 50.5, 2.9), (50.5, 51.5, 2.55), (51.5, 60, 2.9)],
         [(0, 60, 2.9)],
         [(4, 2.9, "open field"), (9, 2.55, "bulkhead"), (22, 2.9, "9 m bays, one axis"), (39, 2.4, "core / files"),
          (56, 2.9, "private-office ring")]),
        ("RUN!", COL["RUN"],
         [(0, 6, 2.9), (6, 6.6, 2.1), (6.6, 15, 2.45), (15, 15.5, 2.1), (15.5, 24, 2.45), (24, 24.5, 2.1),
          (24.5, 33.6, 2.45), (33.6, 34.2, 2.1), (34.2, 46, 5.4), (46, 60, 2.9)],
         [(0, 6, 2.9), (6, 33, 2.9), (33, 60, 2.9)],
         [(3, 2.9, "Level 0"), (10.8, 2.45, "spine: 2.45 clear under services"), (19.7, 2.1, "fire door"),
          (40, 5.4, "release at the goal door"), (53, 2.9, "")]),
        ("POOL", COL["POOL"],
         [(0, 6, 2.9), (6, 12, 2.7), (12, 21, 2.4), (21, 24, 2.4), (24, 42, 5.6), (42, 45, 2.4), (45, 51, 5.4),
          (51, 57, 2.4), (57, 60, 2.4)],
         None,
         [(3, 2.9, "corridor"), (9, 2.7, "club entry"), (16.5, 2.4, "lockers"), (33, 5.6, "pool hall: the release"),
          (43.5, 2.4, "tiled neck"), (48, 5.4, "level room"), (53.5, 2.4, "low lid, big basin"),
          (58.5, 3.0, "stair: dark zone")]),
    ]
    y = 150
    for name, col, segs, today, notes in rows:
        strip(d, x0, x1, y + 20, y + 270, segs, col, today, notes, name)
        y += 300
    label(d, (x0, H - 40), "Numbers: 12 ft ground floor and 9 ft typical floors (The Crescent; 2030 Main St, 1989); 2.4 m = 8 ft "
          "back-of-house; pool values follow 18 and 10_research (D6). Illustrative route, not a level plan.", 20, False, GREY)
    im.save(os.path.join(OUT, "ls_fig3_ceiling_rhythm.png"))


if __name__ == "__main__":
    fig1()
    fig2()
    fig3()
    print("wrote", sorted(os.listdir(OUT)))
