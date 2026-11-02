"""Language charms as flag keychains (平面视觉, 2026-11-01). Red, 交互设计 chat 18:58: "我希望你使用国旗钥匙扣方案而不是现在的
物品钥匙扣方案，繁体中文的旗帜用香港/澳门/台湾合体旗帜"; then "在figma上已经有国旗钥匙扣了呀" and "只有没有的方案需要重新绘制".
So the tag is option C's PRINT FLAG tag from Figma (section 3834:8767, built from the B key's own tag vectors, node 2579:4780):
a paper-metal rim (#F4F1E8) 1 Figma px wide, an arched tab with the ring hole on top, and the flag printed edge to edge in flat
spot inks inside. Each flag follows its official construction (sources in Documentation/research/desktop_ui/LANGUAGE_CHARMS.md).

Canvas 64 x 72 units, ring hole at (32, 6.2) as before, so the strip's pivot is unchanged. Every tag has the same die-cut
silhouette. zh-Hans and zh-Hant are paused (Red 11-01 19:31: "为了避免中国争议，我需要你research相关的做法，来保证我可以在中国区域上架")
and keep the 10-31 landmarks drawn by charms.py. Render: python3 flags.py [out_dir]; writes LangCharm_<code>@4x.png (256 x 288, the in-game file) and @8x.png
(512 x 576) plus charms.json. The default output is outside the project."""
import json, math, os, sys
from PIL import Image, ImageDraw, ImageChops

W, H = 64, 72
RING = (32.0, 6.2)
SS = 32  # supersample: canvas px per unit; box-filtered down to 8 and 4 px per unit

PAPER = (244, 241, 232)
# Option C's spot inks (build_option.js FL), plus the few a flag needs that option C never drew.
INK = {
    'red': (200, 16, 46), 'blue': (15, 41, 115), 'gold': (224, 173, 43), 'green': (31, 107, 59), 'white': PAPER, 'black': (20, 20, 20),
    'kr_red': (205, 46, 58), 'kr_blue': (0, 71, 160),   # Korea's legislated pair (#CD2E3A, #0047A0)
    'mx_green': (0, 104, 71), 'eagle': (120, 76, 38), 'eagle2': (88, 54, 26), 'bronze': (154, 106, 58),   # Mexico (#006847) and its arms
    'br_green': (0, 148, 64), 'br_gold': (255, 203, 0), 'br_blue': (48, 38, 129),  # Brazil (#009440, #FFCB00, #302681)
}

# ---- the tag, from the B key's tag vectors (Figma px, 40 x 28 frame), scaled so the rim is 62 units wide
K = 62.0 / 39.33
HOLE_R = 1.725 * K                     # 2.72
TAB_R = 3.795 * K                      # 5.98, the arch around the hole
TAB_BOTTOM = RING[1] + (8.97 - 3.795) * K
RIM = (1.0, RING[1] + (6.898 - 3.793) * K, 63.0, RING[1] + (6.898 - 3.793 + 20.01) * K)  # x0, y0, x1, y1
RIM_R = 3.45 * K
INSET = 1.0 * K                        # rim width, 1.58 units (the landmark charms' outer rim was 1.5)
BODY = (RIM[0] + INSET, RIM[1] + INSET, RIM[2] - INSET, RIM[3] - INSET)
BODY_R = 2.45 * K
BW, BH = BODY[2] - BODY[0], BODY[3] - BODY[1]   # 58.85 x 28.39, 2.07 : 1 like option C


class Canvas:
    """Supersampled paint surface in charm units. Every shape becomes a hard mask; the box filter does the antialiasing."""
    def __init__(self):
        self.size = (W * SS, H * SS)
        self.img = Image.new('RGBA', self.size, (0, 0, 0, 0))

    def mask(self, polys=(), circles=(), evenodd=False):
        m = Image.new('L', self.size, 0)
        d = ImageDraw.Draw(m)
        for p in polys:
            pts = [(x * SS, y * SS) for x, y in p]
            if evenodd:
                t = Image.new('L', self.size, 0); ImageDraw.Draw(t).polygon(pts, fill=255); m = ImageChops.logical_xor(m.convert('1'), t.convert('1')).convert('L'); d = ImageDraw.Draw(m)
            else:
                d.polygon(pts, fill=255)
        for cx, cy, r in circles:
            d.ellipse([(cx - r) * SS, (cy - r) * SS, (cx + r) * SS, (cy + r) * SS], fill=255)
        return m

    def paint(self, colour, m):
        rgb = INK[colour] if isinstance(colour, str) else colour
        self.img.paste(rgb + (255,), (0, 0), m)


def AND(a, b): return ImageChops.multiply(a, b)
def SUB(a, b): return ImageChops.subtract(a, b)
def OR(a, b): return ImageChops.lighter(a, b)


def rrect(x0, y0, x1, y1, r, n=24):
    pts = []
    for cx, cy, a0 in ((x1 - r, y0 + r, -90), (x1 - r, y1 - r, 0), (x0 + r, y1 - r, 90), (x0 + r, y0 + r, 180)):
        pts += [(cx + r * math.cos(math.radians(a0 + 90 * i / n)), cy + r * math.sin(math.radians(a0 + 90 * i / n))) for i in range(n + 1)]
    return pts


def rect(x0, y0, x1, y1): return [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]


def star(cx, cy, r, rot=0.0, inner=0.381966):
    """Five-pointed star, one point at angle rot (degrees, 0 = straight up, clockwise on screen)."""
    pts = []
    for i in range(10):
        a = math.radians(rot - 90 + 36 * i)
        q = r if i % 2 == 0 else r * inner
        pts.append((cx + q * math.cos(a), cy + q * math.sin(a)))
    return pts


class Frame:
    """Maps a flag's own drawing units into the charm: x' = ox + s * (x * cos - y * sin) etc. (y down, angles clockwise)."""
    def __init__(self, ox, oy, s, rot=0.0):
        self.ox, self.oy, self.s, self.r = ox, oy, s, math.radians(rot)

    def p(self, pts):
        c, n = math.cos(self.r), math.sin(self.r)
        return [(self.ox + self.s * (x * c - y * n), self.oy + self.s * (x * n + y * c)) for x, y in pts]

    def circle(self, cx, cy, r, n=180):
        return self.p([(cx + r * math.cos(2 * math.pi * i / n), cy + r * math.sin(2 * math.pi * i / n)) for i in range(n)])

    def sub(self, ox, oy, s=1.0, rot=0.0):
        (x, y), = self.p([(ox, oy)])
        return Frame(x, y, self.s * s, math.degrees(self.r) + rot)


def arc_path(d_cmds, start):
    """A path of SVG relative elliptical arcs (rx = ry, no rotation), flattened. d_cmds: (r, large, sweep, dx, dy)."""
    pts = [start]
    x, y = start
    for r, large, sweep, dx, dy in d_cmds:
        x1, y1 = x + dx, y + dy
        mx, my = (x - x1) / 2, (y - y1) / 2
        lam = (mx * mx + my * my) / (r * r)
        if lam > 1: r *= math.sqrt(lam)
        sign = -1 if large == sweep else 1
        num = max(0.0, r * r * r * r - r * r * (mx * mx + my * my))
        coef = sign * math.sqrt(num / (r * r * (mx * mx + my * my)))
        cxp, cyp = coef * my, -coef * mx
        cx, cy = cxp + (x + x1) / 2, cyp + (y + y1) / 2
        a0 = math.atan2(y - cy, x - cx); a1 = math.atan2(y1 - cy, x1 - cx)
        da = a1 - a0
        if sweep and da < 0: da += 2 * math.pi
        if not sweep and da > 0: da -= 2 * math.pi
        n = max(8, int(abs(da) * r / 1.5))
        pts += [(cx + r * math.cos(a0 + da * i / n), cy + r * math.sin(a0 + da * i / n)) for i in range(1, n + 1)]
        x, y = x1, y1
    return pts


# ---- flags. Each takes the canvas, a clip mask (the field it may paint) and the field's box (x0, y0, x1, y1) in units.

def stripes_h(cv, clip, box, cols, weights=None):
    x0, y0, x1, y1 = box
    weights = weights or [1] * len(cols)
    t = sum(weights); y = y0
    for c, w in zip(cols, weights):
        h = (y1 - y0) * w / t
        cv.paint(c, AND(clip, cv.mask([rect(x0 - 1, y, x1 + 1, y + h + (0 if c is cols[-1] else 0.02))])))
        y += h


def stripes_v(cv, clip, box, cols):
    x0, y0, x1, y1 = box
    w = (x1 - x0) / len(cols)
    for i, c in enumerate(cols):
        cv.paint(c, AND(clip, cv.mask([rect(x0 + i * w, y0 - 1, x0 + (i + 1) * w + 0.02, y1 + 1)])))


def flag_ru(cv, clip, box): stripes_h(cv, clip, box, ['white', 'blue', 'red'])
def flag_de(cv, clip, box): stripes_h(cv, clip, box, ['black', 'red', 'gold'])
def flag_fr(cv, clip, box): stripes_v(cv, clip, box, ['blue', 'white', 'red'])
def flag_es(cv, clip, box): stripes_h(cv, clip, box, ['red', 'gold', 'red'], [1, 2, 1])


def flag_ja(cv, clip, box):
    x0, y0, x1, y1 = box; h = y1 - y0
    cv.paint('white', AND(clip, cv.mask([rect(x0 - 1, y0 - 1, x1 + 1, y1 + 1)])))
    cv.paint('red', AND(clip, cv.mask(circles=[((x0 + x1) / 2, (y0 + y1) / 2, 0.3 * h)])))  # disc 3/5 of the height


def flag_kr(cv, clip, box):
    """Wikimedia's construction of the 1949/1997 law: field 144 x 96, taegeuk r 24 tilted 33.69 deg, trigrams 24 x 16."""
    x0, y0, x1, y1 = box
    F = Frame((x0 + x1) / 2, (y0 + y1) / 2, (y1 - y0) / 96)
    cv.paint('white', AND(clip, cv.mask([rect(x0 - 1, y0 - 1, x1 + 1, y1 + 1)])))
    T = F.sub(0, 0, rot=33.69006752598)
    big = cv.mask([T.circle(0, 0, 24)])
    upper = cv.mask([T.p([(-30, -30), (30, -30), (30, 0), (-30, 0)])])
    red = OR(SUB(AND(big, upper), cv.mask([T.circle(12, 0, 12)])), cv.mask([T.circle(-12, 0, 12)]))
    cv.paint('kr_red', AND(clip, big))
    cv.paint('kr_blue', AND(clip, SUB(big, red)))
    # trigrams: bars 24 long, 4 thick, 2 apart, inner edge 36 from the centre; broken bars have a 2-unit gap
    def bars(rot, xs_pattern):
        R = F.sub(0, 0, rot=rot)
        polys = []
        for x, broken in xs_pattern:
            if broken:
                polys += [R.p(rect(x - 2, -12, x + 2, -1)), R.p(rect(x - 2, 1, x + 2, 12))]
            else:
                polys.append(R.p(rect(x - 2, -12, x + 2, 12)))
        return polys
    polys = bars(33.69006752598, [(-50, 0), (-44, 0), (-38, 0), (38, 1), (44, 1), (50, 1)])      # geon upper left, gon lower right
    polys += bars(-33.69006752598, [(-50, 0), (-44, 1), (-38, 0), (38, 1), (44, 0), (50, 1)])    # ri lower left, gam upper right
    cv.paint('black', AND(clip, cv.mask(polys)))


def flag_br(cv, clip, box):
    """Wikimedia's construction (field 4200 x 2940): rhombus, globe r 735, the white band, 27 stars. The motto is left out:
    at 30 units its letters are under a pixel, so the band is plain, as on most small enamel pins."""
    x0, y0, x1, y1 = box; h = y1 - y0; w = x1 - x0
    s = h / 2940
    F = Frame((x0 + x1) / 2, (y0 + y1) / 2, s)
    cv.paint('br_green', AND(clip, cv.mask([rect(x0 - 1, y0 - 1, x1 + 1, y1 + 1)])))
    # the rhombus keeps its 1.7 / 14 margin to every edge, so it stretches with the wider field
    mx, my = w * 357 / 4200, h * 357 / 2940
    cv.paint('br_gold', AND(clip, cv.mask([[(x0 + mx, (y0 + y1) / 2), ((x0 + x1) / 2, y0 + my), (x1 - mx, (y0 + y1) / 2), ((x0 + x1) / 2, y1 - my)]])))
    globe = cv.mask([F.circle(0, 0, 735, 360)])
    cv.paint('br_blue', AND(clip, globe))
    band = SUB(cv.mask([F.circle(-420, 1470, 1785, 720)]), cv.mask([F.circle(-420, 1470, 1680, 720)]))
    upper = cv.mask([F.p(rect(-3000, -3000, 3000, 1470))])
    cv.paint('white', AND(clip, AND(AND(band, upper), globe)))
    sizes = {'a': 31.5, 'b': 26.25, 'f': 21, 'h': 15, 'i': 10.5}
    pos = [(-600, -132, 'a'), (-535, 177, 'a'), (-625, 243, 'b'), (-463, 132, 'h'), (-382, 250, 'b'), (-404, 323, 'f'), (228, -228, 'a'),
           (515, 258, 'a'), (617, 265, 'f'), (545, 323, 'b'), (368, 477, 'b'), (367, 551, 'f'), (441, 419, 'f'), (500, 382, 'b'), (365, 405, 'f'),
           (-280, 30, 'b'), (200, -37, 'f'), (0, 330, 'a'), (85, 184, 'b'), (0, 118, 'b'), (-74, 184, 'f'), (-37, 235, 'h'), (220, 495, 'b'),
           (283, 430, 'f'), (162, 412, 'f'), (-295, 390, 'a'), (0, 575, 'i')]
    cv.paint('white', AND(clip, AND(globe, cv.mask([F.p(star(x, y, sizes[k])) for x, y, k in pos]))))


def flag_us(cv, clip, box):
    """13 stripes; canton 7/13 of the hoist high and 0.76 of it wide; 50 stars 0.0616 of the hoist across, rows 0.054 apart."""
    x0, y0, x1, y1 = box; h = y1 - y0
    for i in range(13):
        cv.paint('red' if i % 2 == 0 else 'white', AND(clip, cv.mask([rect(x0 - 1, y0 + h * i / 13, x1 + 1, y0 + h * (i + 1) / 13 + (0.02 if i < 12 else 1))])))
    cw, ch = 0.76 * h, 7 * h / 13
    cv.paint('blue', AND(clip, cv.mask([rect(x0 - 1, y0 - 1, x0 + cw, y0 + ch)])))
    stars = []
    for row in range(9):
        cols = range(6) if row % 2 == 0 else range(5)
        for k in cols:
            gx = 0.063 * (2 * k + 1) if row % 2 == 0 else 0.063 * (2 * k + 2)
            stars.append(star(x0 + gx * h, y0 + 0.054 * (row + 1) * h, 0.0308 * h))
    cv.paint('white', AND(clip, cv.mask(stars)))


def flag_uk(cv, clip, box):
    """Union Flag on the field's own diagonals; widths in thirtieths of the height (saltire 6, red 2 counterchanged, cross 10 / 6)."""
    x0, y0, x1, y1 = box; h = y1 - y0; w = x1 - x0; u = h / 30
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    cv.paint('blue', AND(clip, cv.mask([rect(x0 - 1, y0 - 1, x1 + 1, y1 + 1)])))
    def band(ax, ay, bx, by, off0, off1):  # a strip along a->b between perpendicular offsets off0..off1 (units, + = left of travel)
        L = math.hypot(bx - ax, by - ay); nx, ny = (ay - by) / L, (bx - ax) / L
        ex, ey = (bx - ax) / L * 3, (by - ay) / L * 3
        return [(ax - ex + nx * off0, ay - ey + ny * off0), (bx + ex + nx * off0, by + ey + ny * off0), (bx + ex + nx * off1, by + ey + ny * off1), (ax - ex + nx * off1, ay - ey + ny * off1)]
    d1 = (x0, y0, x1, y1); d2 = (x1, y0, x0, y1)
    cv.paint('white', AND(clip, cv.mask([band(*d1, -3 * u, 3 * u), band(*d2, -3 * u, 3 * u)])))
    # counterchange: in each quarter the red sits on the side of the diagonal that turns clockwise (broad white uppermost at the hoist)
    pin = cv.mask([[(cx, cy), (x1 + 1, cy), (x1 + 1, y1 + 1)], [(cx, cy), (cx, y1 + 1), (x0 - 1, y1 + 1)], [(cx, cy), (x0 - 1, cy), (x0 - 1, y0 - 1)], [(cx, cy), (cx, y0 - 1), (x1 + 1, y0 - 1)]])
    cv.paint('red', AND(clip, AND(pin, cv.mask([band(*d1, -2 * u, 2 * u), band(*d2, -2 * u, 2 * u)]))))
    cv.paint('white', AND(clip, cv.mask([rect(cx - 5 * u, y0 - 1, cx + 5 * u, y1 + 1), rect(x0 - 1, cy - 5 * u, x1 + 1, cy + 5 * u)])))
    cv.paint('red', AND(clip, cv.mask([rect(cx - 3 * u, y0 - 1, cx + 3 * u, y1 + 1), rect(x0 - 1, cy - 3 * u, x1 + 1, cy + 3 * u)])))


# ---- the composites

DIVIDER = float(os.environ.get('FLAG_DIVIDER', 0.9))   # paper-metal line between two flags on one tag, units (between the 0.55 cloisonne and the 1.58 rim)
EN_DIVIDER = float(os.environ.get('FLAG_EN_DIVIDER', 0.0))   # option C had none: the Union Flag's own white saltire already parts the halves
KEYLINE = float(os.environ.get('FLAG_KEYLINE', 0.4))         # a fine engraved line where the rim meets the enamel, so white fields keep their edge (units)
KEY_INK = (204, 197, 180)


def flag_en(cv, clip, box):
    """Option C's split, kept (Red 10-31: English is half and half, UK and US): the US above the fly-to-hoist diagonal, the UK below."""
    x0, y0, x1, y1 = box
    flag_us(cv, clip, box)
    uk = AND(clip, cv.mask([[(x1 + 1, y0 - 1 - (y1 - y0) / (x1 - x0)), (x1 + 1, y1 + 1), (x0 - 1 - (x1 - x0) / (y1 - y0), y1 + 1)]]))
    flag_uk(cv, uk, box)
    if EN_DIVIDER:
        cv.paint('white', AND(clip, cv.mask([strip(x1, y0, x0, y1, EN_DIVIDER)])))


def strip(ax, ay, bx, by, width, over=3.0):
    """A straight band of the given width along a -> b, run past both ends."""
    L = math.hypot(bx - ax, by - ay); ux, uy = (bx - ax) / L, (by - ay) / L; nx, ny = -uy * width / 2, ux * width / 2
    ax, ay, bx, by = ax - ux * over, ay - uy * over, bx + ux * over, by + uy * over
    return [(ax + nx, ay + ny), (bx + nx, by + ny), (bx - nx, by - ny), (ax - nx, ay - ny)]


def ellipse_pts(cx, cy, rx, ry, rot=0.0, n=40):
    a = math.radians(rot); c, s = math.cos(a), math.sin(a)
    return [(cx + rx * math.cos(2 * math.pi * i / n) * c - ry * math.sin(2 * math.pi * i / n) * s,
             cy + rx * math.cos(2 * math.pi * i / n) * s + ry * math.sin(2 * math.pi * i / n) * c) for i in range(n)]


def flag_mx(cv, clip, box, centre=None):
    """Mexico, 4 : 7: green, white, red; the arms in the white, drawn small and plain: the eagle in profile facing the hoist on a
    nopal with its fruit, the snake in its beak, the rock in the lake, the oak and laurel below. `centre` moves the flag's middle."""
    x0, y0, x1, y1 = box; h = y1 - y0
    W_ = 1.75 * h; mid = centre if centre is not None else (x0 + x1) / 2; st = W_ / 3
    cv.paint('mx_green', AND(clip, cv.mask([rect(x0 - 1, y0 - 1, mid - st / 2 + 0.02, y1 + 1)])))
    cv.paint('white', AND(clip, cv.mask([rect(mid - st / 2, y0 - 1, mid + st / 2 + 0.02, y1 + 1)])))
    cv.paint('red', AND(clip, cv.mask([rect(mid + st / 2, y0 - 1, x1 + 1, y1 + 1)])))
    u = st / 18.0; cx, cy = mid, (y0 + y1) / 2   # arms units: the white band is 18 across
    m = lambda pts: AND(clip, cv.mask([[(cx + x * u, cy + y * u) for x, y in pts]]))
    E = lambda x, y, rx, ry, r=0: [(cx + px * u, cy + py * u) for px, py in ellipse_pts(x, y, rx, ry, r)]
    # oak and laurel: a U of leaves under the scene, tied with the colours
    leaves = []
    for side in (-1, 1):
        for k in range(7):
            a = math.radians(200 + 20 * k) if side < 0 else math.radians(-20 - 20 * k)
            px, py = 6.0 * math.cos(a) * (1 if side < 0 else 1), 1.0 - 4.6 * math.sin(a) * -1
            leaves.append(E(px * (-1 if side < 0 else 1) * (1 if side < 0 else -1) if False else 6.0 * math.cos(math.radians(180 + 13 * k)) * (1 if side < 0 else -1), 1.8 + 4.4 * math.sin(math.radians(13 * k + 10)), 1.25, 0.55, (25 + 12 * k) * (-1 if side < 0 else 1)))
    cv.paint('mx_green', AND(clip, cv.mask(leaves)))
    cv.paint('red', AND(clip, cv.mask([E(0, 6.6, 0.9, 0.6)])))
    # the lake and the rock
    cv.paint('blue', AND(clip, cv.mask([E(0, 5.2, 3.6, 0.7)])))
    cv.paint('bronze', AND(clip, cv.mask([E(0, 4.4, 1.8, 0.7)])))
    # the nopal: three pads, its red fruit
    cv.paint('mx_green', AND(clip, cv.mask([E(0, 2.4, 1.3, 1.9), E(-1.9, 1.4, 1.0, 1.5, -30), E(1.9, 1.2, 1.0, 1.5, 30)])))
    cv.paint('red', AND(clip, cv.mask([E(-2.5, -0.1, 0.35, 0.35), E(2.6, -0.3, 0.35, 0.35), E(0.6, 0.5, 0.3, 0.3)])))
    # the eagle: body, both wings raised, head to the hoist, gold beak, the snake hanging from it
    body = E(0.5, -1.6, 2.0, 2.7, 25)
    wing_b = [(1.4, -2.6), (4.6, -7.0), (5.2, -5.5), (4.8, -3.2), (3.0, -0.6)]
    wing_f = [(-0.2, -2.8), (-1.2, -7.2), (0.6, -6.2), (1.8, -3.4)]
    head = E(-1.6, -4.2, 1.15, 1.0)
    cv.paint('eagle', AND(clip, cv.mask([[(cx + x * u, cy + y * u) for x, y in wing_b], body, [(cx + x * u, cy + y * u) for x, y in wing_f], head])))
    cv.paint('eagle2', AND(clip, cv.mask([[(cx + x * u, cy + y * u) for x, y in [(2.0, -2.2), (4.6, -7.0), (4.9, -6.1), (3.4, -2.0)]]])))
    cv.paint('gold', AND(clip, cv.mask([[(cx + x * u, cy + y * u) for x, y in [(-2.6, -4.6), (-3.6, -4.1), (-2.6, -3.7)]]])))
    snake = []
    for i in range(14):
        t_ = i / 13; snake.append((-3.2 - 1.0 * math.sin(t_ * 5.5), -4.0 + 4.6 * t_))
    cv.paint('mx_green', AND(clip, cv.mask([strip_path([(cx + x * u, cy + y * u) for x, y in snake], 0.45 * u)])))


def strip_path(pts, w):
    """A polyline widened to `w` as one polygon (miter-free, fine at this size)."""
    left, right = [], []
    for i, (x, y) in enumerate(pts):
        a = pts[max(0, i - 1)]; b = pts[min(len(pts) - 1, i + 1)]
        dx, dy = b[0] - a[0], b[1] - a[1]; L = math.hypot(dx, dy) or 1
        nx, ny = -dy / L * w / 2, dx / L * w / 2
        left.append((x + nx, y + ny)); right.append((x - nx, y - ny))
    return left + right[::-1]


def flag_es_mx(cv, clip, box):
    """Red 11-01 19:26: "西班牙墨西哥各一半". English's split: Spain above the fly-to-hoist diagonal, Mexico below, its middle at
    0.75 of the field so the white band and the arms sit inside its half; a paper-metal line parts them (no white edge of its own)."""
    x0, y0, x1, y1 = box
    flag_es(cv, clip, box)
    low = AND(clip, cv.mask([[(x1 + 1, y0 - 1 - (y1 - y0) / (x1 - x0)), (x1 + 1, y1 + 1), (x0 - 1 - (x1 - x0) / (y1 - y0), y1 + 1)]]))
    flag_mx(cv, low, box, centre=x0 + 0.75 * (x1 - x0))
    cv.paint('white', AND(clip, cv.mask([strip(x1, y0, x0, y1, DIVIDER)])))


FLAGS = [  # in-game order (FrontRoomsLanguage.ListedLanguages)
    ('en', 'English', 'United Kingdom + United States, diagonal halves', flag_en),
    ('ru', 'Русский', 'Russia', flag_ru),
    ('es', 'Español', 'Spain + Mexico, diagonal halves', flag_es_mx),
    ('pt-BR', 'Português (Brasil)', 'Brazil', flag_br),
    ('de', 'Deutsch', 'Germany', flag_de),
    ('fr', 'Français', 'France', flag_fr),
    ('ja', '日本語', 'Japan', flag_ja),
    ('ko', '한국어', 'Korea', flag_kr),
]


def render(fn):
    cv = Canvas()
    tab = OR(cv.mask(circles=[(RING[0], RING[1], TAB_R)]), cv.mask([rect(RING[0] - TAB_R, RING[1], RING[0] + TAB_R, TAB_BOTTOM)]))
    tag = OR(tab, cv.mask([rrect(*RIM, RIM_R)]))
    tag = SUB(tag, cv.mask(circles=[(RING[0], RING[1], HOLE_R)]))
    cv.paint('white', tag)
    body = cv.mask([rrect(*BODY, BODY_R)])
    fn(cv, body, BODY)
    if KEYLINE:
        inner = cv.mask([rrect(BODY[0] + KEYLINE, BODY[1] + KEYLINE, BODY[2] - KEYLINE, BODY[3] - KEYLINE, BODY_R - KEYLINE)])
        cv.paint(KEY_INK, SUB(body, inner))
    return cv.img


# The die-cut, x0 y0 x1 y1 in units (y0 = the tab's top, y1 = the rim's bottom): the same for every flag.
SILHOUETTE = [round(RIM[0], 2), round(RING[1] - TAB_R, 2), round(RIM[2], 2), round(RIM[3], 2)]


def scale(big, px):
    f = SS // px
    return big.convert('RGBa').reduce(f).convert('RGBA')  # premultiplied box filter: exact coverage, no fringe


def bbox_units(img, px):
    a = img.getchannel('A').point(lambda v: 255 if v >= 8 else 0)
    x0, y0, x1, y1 = a.getbbox()
    return [round(x0 / px, 2), round(y0 / px, 2), round(x1 / px, 2), round(y1 / px, 2)]


if __name__ == '__main__':
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.expanduser('~/FrontRoomsVisualWork/lang_charms/out_flags')
    os.makedirs(out, exist_ok=True)
    meta = {'canvasUnits': [W, H], 'pivotUnits': list(RING), 'unitsPerPx': {'desktop': 0.6, 'phone': 0.5, 'tablet': 0.55},
            'note': 'flags (11-01, Red: flag keychains, option C tags from Figma; zh-Hans and zh-Hant paused, they keep the 10-31 landmarks); pivot = the ring hole; one die-cut for all',
            'tagUnits': {'rim': [round(v, 3) for v in RIM], 'rimRadius': round(RIM_R, 3), 'field': [round(v, 3) for v in BODY], 'fieldRadius': round(BODY_R, 3),
                         'holeRadius': round(HOLE_R, 3), 'tabRadius': round(TAB_R, 3)},
            'charms': {}}
    for code, name, what, fn in FLAGS:
        big = render(fn)
        for px in (8, 4):
            scale(big, px).save(os.path.join(out, f'LangCharm_{code}@{px}x.png'), optimize=True)
        meta['charms'][code] = {'name': name, 'flag': what, 'bboxUnits': SILHOUETTE, 'inGame': True, 'set': 'flags'}
        assert bbox_units(scale(big, 8), 8)[2] - SILHOUETTE[2] < 0.2, code  # the pixels agree with the die-cut
    with open(os.path.join(out, 'charms.json'), 'w', newline='\n') as f:
        json.dump(meta, f, ensure_ascii=False, indent=1); f.write('\n')
    print('ok', len(FLAGS), out)
