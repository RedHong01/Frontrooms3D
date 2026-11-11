#!/usr/bin/python3
"""FrontRooms station paper: Prop_StationPaper atlas + the STAIR KEY plate (平面视觉, 2026-10-30, BS7).
Cells and sizes come from Tools/Blender/frontrooms_kit/assets/_station_common.py and the kits; strings from
research/station/01_research.md §6. Each cell is drawn at its physical size, then stretched into its square atlas cell,
because the kits map the whole cell onto the quad. Paper is white here: Prop_StationPaper's _BaseColor tints it to #DCD8CC.
Usage: build_atlas.py <out dir> [lang]
Languages (Red 11-03, 叙事设计 chat: world text follows the language; research/narrative/95 §5.3 words, WORLD_TYPE.md faces):
the same cells, rules, hands and slots; only the words change (words95.py reads 95 §5.3 and the tables). A word too long for its
place is set in the condensed cut, then narrower (to 85 %), then smaller: never cut. lang=en is byte-identical to the English atlas.
zh-Hans (11-10, the faces Red let us download): heads 黑体 Noto Sans SC 700 (R1/R3), typed lines 仿宋 Zhuque Fangsong on a
typewriter's full-width grid (R2: one cell a character, digits and initials too; its stem laid on to Courier Prime's, the ribbon's
spread), the visitors' and key columns in Long Cang (R4, the same blue ballpoint line as Rock Salt's); CJK em = cap / 0.70, no
tracking (WORLD_TYPE.md §2-3)."""
import json, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from words95 import words
from PIL import Image, ImageDraw, ImageFont

ROOT = "/Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01 Individual Game Project/Frontrooms3D"
F = ROOT + "/Assets/Fonts/Period1990/"
HB, HR, HC = F + "TeXGyre/texgyreheros-bold.otf", F + "TeXGyre/texgyreheros-regular.otf", F + "TeXGyre/texgyreheroscn-bold.otf"
TYPED = F + "CourierPrime/CourierPrime-Regular.ttf"
HAND = os.path.expanduser("~/Library/Fonts/RockSalt.ttf")
FW = os.path.expanduser("~/FrontRoomsVisualWork/iconography/fonts_world/")
CJK = {"zh-Hans"}
K, RULE, BLUE, PAPER = (26, 26, 26), (120, 120, 118), (31, 42, 92), (255, 255, 255)
PX = 12                                    # px per mm while drawing
CELL, N = 1024, 4


LANG = sys.argv[2] if len(sys.argv) > 2 else "en"
W = words(LANG) if LANG != "en" else {}
if LANG in CJK:   # the roles' faces for zh-Hans; a path may carry "#<wght>" for the variable Noto Sans SC
    SC = os.path.expanduser("~/Library/Fonts/NotoSansSC-VariableFont_wght.ttf")
    HB = HC = SC + "#700"; HR = SC + "#400"
    TYPED = FW + "zhuque/x/ZhuqueFangsong-Regular.ttf"
    HAND = FW + "longcang/LongCang-Regular.ttf"


def font(path, cap):
    if LANG in CJK:   # CJK em = the Latin cap / 0.70
        path, _, wght = path.partition("#")
        f = ImageFont.truetype(path, max(1, round(cap / 0.70 * PX)))
        if wght: f.set_variation_by_axes([float(wght)])
        return f
    probe = ImageFont.truetype(path, 1000)
    b = probe.getbbox("H")
    return ImageFont.truetype(path, round(cap * PX * 1000 / (b[3] - b[1])))


def grow_mm(path, cap):
    """Stem laid on as a stroke (mm), CJK only. Typed: Zhuque's stem (0.056 em) up to Courier Prime Regular's (0.126 cap),
    the ribbon's spread. Hand: Long Cang's pen (0.043 em) up to Rock Salt's line at that cap plus its 1 px ballpoint stroke."""
    em = cap / 0.70
    if path == TYPED: return 0.126 * cap - 0.056 * em
    if path == HAND: return 0.087 * cap + 2 / PX - 0.043 * em
    return 0.0


def grid(s):
    """A typed CJK line on the typewriter's grid: a word space next to a hanzi goes, a double space is one empty cell."""
    out, i = [], 0
    while i < len(s):
        if s[i] == " ":
            j = i
            while j < len(s) and s[j] == " ": j += 1
            n = j - i; prev = s[i - 1] if i else ""; nxt = s[j] if j < len(s) else ""
            cjk = lambda c: c and ord(c) > 0x2E7F
            if i == 0: out += [" "] * n
            elif n >= 2: out.append(" ")
            elif not (cjk(prev) or cjk(nxt)): out.append(" ")
            i = j
        else:
            out.append(s[i]); i += 1
    return out


def tr(s):
    """The word in LANG: 95 §5.3 and the tables; TOUR 2 follows TOUR 1 with its number changed."""
    if LANG == "en": return s
    if s in W: return W[s]
    if s == "TOUR 2 COMPLETE  ALL SECURE": return W["TOUR 1 COMPLETE  ALL SECURE"].replace("1", "2", 1)
    raise KeyError((LANG, s))


class Page:
    def __init__(self, w, h):
        self.im = Image.new("RGB", (round(w * PX), round(h * PX)), PAPER)
        self.d = ImageDraw.Draw(self.im)
        self.w, self.h = w, h

    def fit(self, s, x, y, paths, cap, maxw, fill=K, track=0.0, align="l", stroke=0):
        """text() in the first face that fits maxw mm; else the last face set narrower (to 85 %), else smaller."""
        if LANG in CJK:
            w = self.cjk_width(s, paths[0], cap)
            if w <= maxw: return self.cjk(s, x, y, paths[0], cap, fill, align)
            if w * 0.85 <= maxw: return self.cjk(s, x, y, paths[0], cap, fill, align, sx=maxw / w)
            return self.fit(s, x, y, paths, cap * 0.95, maxw, fill, track, align, stroke)
        for path in paths:
            f = font(path, cap)
            w = (sum(f.getlength(c) + track * PX for c in s) - track * PX if track else f.getlength(s)) / PX
            if w <= maxw: return self.text(s, x, y, path, cap, fill, track, align, stroke)
        sx = maxw / w
        if sx >= 0.85:
            lay = Page(w + 2, cap * 3); lay.im = Image.new("L", lay.im.size, 0); lay.d = ImageDraw.Draw(lay.im)
            lay.text(s, 1, cap * 2, paths[-1], cap, 255, track, "l", stroke)
            im = lay.im.resize((max(1, round(lay.im.width * sx)), lay.im.height), Image.LANCZOS)
            px = x * PX - (im.width if align == "r" else im.width / 2 if align == "c" else 0) - PX * sx
            self.im.paste(Image.new("RGB", im.size, fill if isinstance(fill, tuple) else (fill, fill, fill)), (round(px), round((y - cap * 2) * PX)), im)
            return
        return self.fit(s, x, y, paths, cap * 0.95, maxw, fill, track, align, stroke)

    def cjk_width(self, s, path, cap):
        if path == TYPED: return len(grid(s)) * cap / 0.70
        return font(path, cap).getlength(s) / PX + grow_mm(path, cap)

    def cjk(self, s, x, y, path, cap, fill=K, align="l", sx=1.0):
        """CJK text, drawn at 4x so the laid-on stem keeps its fraction of a pixel; typed lines one em cell a character,
        a half-width glyph centred in its cell; sx narrows the run (长体, never under 0.85)."""
        q = 4
        em = cap / 0.70 * PX * q
        f = font(path, cap * q)
        st = max(0, round(grow_mm(path, cap) / 2 * PX * q))
        cells = grid(s) if path == TYPED else None
        w = len(cells) * em if cells else f.getlength(s)
        asc, desc = f.getmetrics()
        pad = 2 * st + 8
        m = Image.new("L", (int(w) + 2 * pad, asc + desc + 2 * pad), 0)
        d = ImageDraw.Draw(m)
        if cells:
            for i, c in enumerate(cells):
                if c != " ": d.text((pad + i * em + em / 2, pad + asc), c, font=f, fill=255, anchor="ms", stroke_width=st, stroke_fill=255)
        else:
            d.text((pad, pad + asc), s, font=f, fill=255, anchor="ls", stroke_width=st, stroke_fill=255)
        m = m.resize((max(1, round(m.width * sx / q)), max(1, round(m.height / q))), Image.LANCZOS)
        ww = w * sx / q
        px = x * PX - (ww if align == "r" else ww / 2 if align == "c" else 0) - pad * sx / q
        self.im.paste(Image.new("RGB", m.size, fill if isinstance(fill, tuple) else (fill, fill, fill)), (round(px), round(y * PX - (pad + asc) / q)), m)

    def text(self, s, x, y, path, cap, fill=K, track=0.0, align="l", stroke=0):
        """x, y in mm; y is the baseline."""
        if LANG in CJK: return self.cjk(s, x, y, path, cap, fill, align)
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
        if LANG == "en": p.text(h, x + 1.2, top + 5.4, HB, 2.1, track=0.1)
        else: p.fit(tr(h), x + 1.2, top + 5.4, [HB, HC], 2.1, w - 2.4, track=0.1)
        if x > x0:
            p.line(x, top, x, top + 8 + rows * pitch, 0.25, K)
        x += w
    for r in range(1, rows):
        y = top + 8 + r * pitch
        p.line(x0, y, x0 + total, y)


def hand(p, s, x, y, cap=3.6, fill=BLUE, maxw=None):
    s = "".join(grid(s)) if LANG in CJK else s.replace(" ", "  ")   # Rock Salt's word space is too tight; a CJK hand writes 楼梯A, 214室
    if LANG != "en" and maxw:   # a longer word is written smaller, as a hand does when the box runs out
        f = font(HAND, cap)
        while cap > 2.0 and f.getlength(s) / PX > maxw: cap -= 0.1; f = font(HAND, cap)
    p.text(s, x, y, HAND, cap, fill, stroke=1)   # a ballpoint line


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
            hand(p, tr(s) if c in (2, 3) else s, x[c] + 1.4, y, 3.0 if c in (2, 3) else 3.3, maxw=REG_W[c] - 2.8)
    return p


def register_right():
    p = Page(205, 285)
    form(p, REG_HEADS, REG_W, 4, 14, 9, 28)
    return p


def tent_card():
    p = Page(200, 90)
    p.box(6, 6, 194, 84, 0.8)
    if LANG == "en": p.text("PLEASE SIGN IN", 100, 53, HB, 13, align="c", track=0.5)
    else:
        t = tr("PLEASE SIGN IN"); f = font(HC, 13)
        if f.getlength(t) / PX * 0.85 <= 176: p.fit(t, 100, 53, [HB, HC], 13, 176, align="c", track=0.5)
        else:   # a long phrase takes two lines on the card, never a small one
            cuts = [i for i, c in enumerate(t) if c == " "]; i = min(cuts, key=lambda k: abs(k - len(t) / 2))
            p.fit(t[:i], 100, 42, [HB, HC], 11, 176, align="c", track=0.4); p.fit(t[i + 1:], 100, 62, [HB, HC], 11, 176, align="c", track=0.4)
    return p


def dar():
    p = Page(216, 279)
    lines = ["          NIGHT  DESK", "2300  ON DUTY  D.K.", "0000  TOUR 1 COMPLETE  ALL SECURE", "0100  TOUR 2 COMPLETE  ALL SECURE",
             "0112  BLDG SVCS CALLED  LAMPS OUT STOCK RM 3", "0200  TOUR 3"]
    for r in range(24):
        p.line(14, 40 + r * 9.5, 202, 40 + r * 9.5)
    if LANG != "en":   # the same typed lines: the leading blank (the live date's slot), the times, the words in LANG
        lines = ["          " + tr("NIGHT DESK"), "2300  " + tr("ON DUTY D.K."), "0000  " + tr("TOUR 1 COMPLETE  ALL SECURE"),
                 "0100  " + tr("TOUR 2 COMPLETE  ALL SECURE"), "0112  " + tr("BLDG SVCS CALLED  LAMPS OUT STOCK RM 3"), "0200  " + tr("TOUR 3")]
    for i, s in enumerate(lines):
        if LANG == "en": p.text(s, 16, 38.4 + i * 9.5, TYPED, 2.7)
        else: p.fit(s, 16, 38.4 + i * 9.5, [TYPED], 2.7, 184)
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
                hand(p, tr(s) if s == "STAIR A" else s, x[c] + 2, y, 3.6, maxw=w[c] - 3.5)
    return p


def key_control():
    p = Page(200, 70)
    if LANG == "en":
        p.text("KEY CONTROL", 100, 36, HB, 15, align="c", track=0.4)
        p.text("ALL KEYS MUST BE SIGNED OUT", 100, 54, HB, 6, align="c", track=0.25)
    else:
        p.fit(tr("KEY CONTROL"), 100, 36, [HB, HC], 15, 180, align="c", track=0.4)
        p.fit(tr("ALL KEYS MUST BE SIGNED OUT"), 100, 54, [HB, HC], 6, 180, align="c", track=0.25)
    return p


def phone_list():
    p = Page(140, 200)
    if LANG == "en": p.text("PHONE LIST", 70, 24, HB, 5.5, align="c", track=0.4)
    else: p.fit(tr("PHONE LIST"), 70, 24, [HB, HC], 5.5, 112, align="c", track=0.4)
    p.line(14, 30, 126, 30, 0.4, K)
    for i, (a, b) in enumerate([("FIRE", "911"), ("POLICE", "911"), ("SECURITY DESK", "200"), ("FRONT OFFICE", "101"), ("MAILROOM", "118"),
                                ("BLDG SVCS", "210"), ("BOILER RM", "214"), ("CONTROL RM", "299")]):
        y = 46 + i * 16
        if LANG == "en": p.text(a, 16, y, TYPED, 4.2)
        else: p.fit(tr(a), 16, y, [TYPED], 4.2, 92)
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
    if LANG == "en": p.text("POST ORDERS", 100, 24, HB, 14, align="c", track=0.8)
    else: p.fit(tr("POST ORDERS"), 100, 24, [HB, HC], 14, 184, align="c", track=0.8)
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
