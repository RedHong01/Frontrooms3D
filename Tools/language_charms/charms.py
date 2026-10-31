"""Language charms: landmark souvenir keychains (平面视觉, 2026-10-31). Red: replace the A · TAG tokens with each
language's landmark keychain (enamel colour, from option C's colour idea), keep the one-row strip; the chosen charm
flies onto the key at the row's head. Canvas 64 x 72 units; the shared ring tab at the top centre; paper metal rim
(#F4F1E8) around every enamel field (cloisonne) and a thicker outer rim. Render: python3 charms.py"""
import math, os
from PIL import Image, ImageDraw

W, H = 64, 72
PAPER = (244, 241, 232)
C = {  # one enamel palette for the whole set
 'red': (184, 51, 42), 'blue': (36, 65, 140), 'gold': (217, 167, 58), 'patina': (94, 156, 134), 'stone': (207, 198, 176),
 'marble': (233, 228, 214), 'green': (62, 125, 79), 'teal': (58, 127, 134), 'bronze': (154, 106, 58), 'slate': (74, 85, 96),
 'brick': (158, 59, 46), 'turq': (79, 163, 165), 'navy': (26, 46, 104), 'lav': (185, 160, 207), 'amber': (214, 146, 52), 'amber2': (232, 178, 82), 'lye': (122, 67, 33), 'wood': (201, 160, 102), 'woodd': (107, 74, 43), 'spruce': (236, 205, 140), 'black': (28, 28, 30), 'bronze2': (128, 84, 44), 'blue2': (58, 92, 170), 'sand': (218, 196, 152), 'sand2': (190, 166, 122), 'granite': (96, 104, 96), 'lav2': (150, 118, 182), 'woodl': (214, 178, 122), 'cream': (240, 226, 200), 'ink': (20, 20, 20), 'paper': PAPER,
}

def mirror(right):  # right-half outline (top to bottom, x >= 32) -> full symmetric polygon
    return right + [(64 - x, y) for (x, y) in reversed(right)]

def circ(cx, cy, r, n=40, a0=0, a1=360):
    return [(cx + r * math.cos(math.radians(a)), cy + r * math.sin(math.radians(a))) for a in [a0 + (a1 - a0) * i / n for i in range(n + 1)]]

def rect(x0, y0, x1, y1): return [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]

def trap(cx, y0, hw0, y1, hw1): return [(cx - hw0, y0), (cx + hw0, y0), (cx + hw1, y1), (cx - hw1, y1)]

def onion(cx, top, r, cy):  # bulb of radius r centred at cy, pointed tip at top
    pts = [(cx, top)]
    for a in range(-60, 241, 10):
        pts.append((cx + r * math.cos(math.radians(a)), cy + r * math.sin(math.radians(a))))
    return pts

def rot(pts, deg, ox, oy):
    a = math.radians(deg); c, s = math.cos(a), math.sin(a)
    return [(ox + (x - ox) * c - (y - oy) * s, oy + (x - ox) * s + (y - oy) * c) for x, y in pts]

TAB = {'body': [(27.5, 12.5)] + circ(32, 6.2, 4.5, 24, 180, 360) + [(36.5, 12.5)], 'hole': circ(32, 6.2, 1.9, 20)}

# Each charm: list of (colour, polygon) enamel fields in paint order; 'cuts' = holes through the charm.
def eiffel():
    body = mirror([(32.6, 11), (33.2, 14), (33.6, 18), (35.5, 18), (35.5, 19.6), (34.4, 19.6), (35.6, 30), (38.5, 30), (38.5, 32), (36.2, 32),
                   (38.4, 44), (44.5, 44), (44.5, 46.5), (40.8, 46.5), (50, 69), (43.6, 69), (42.8, 63), (40.6, 59), (37, 56.5), (32, 55.8)])
    return [('bronze', body)], []

def bigben_liberty():
    bar = rect(14.8, 11.4, 49.2, 13.4)
    # Elizabeth Tower, centre x 17 (hangs from the bar's left end by its spire)
    spire = [(17, 13), (18.2, 17), (20.8, 24), (13.2, 24), (15.8, 17)]
    belfry = rect(12, 24, 22, 35)
    shaft = rect(13, 35, 21, 66)
    base = rect(11, 66, 23, 70)
    clock = circ(17, 29.5, 3.0, 28)
    # Statue of Liberty, hangs from the bar's right end by her torch (raised right hand, the viewer's left of her head)
    flame = [(47.0, 13.0), (48.8, 15.0), (48.6, 17.6), (45.6, 17.6), (45.4, 15.0)]
    handle = rect(46.4, 17.4, 47.6, 19.6)
    arm = [(46.0, 19.4), (48.2, 19.4), (50.2, 28.4), (48.0, 29.2)]
    crown = [(47.0, 25.2), (48.4, 26.4), (48.2, 22.8), (49.8, 25.4), (50.6, 22.0), (51.4, 25.4), (53.2, 23.0), (52.8, 26.6), (54.2, 25.6), (53.0, 28.6), (47.6, 28.6)]
    head = circ(50.4, 29.0, 2.3, 24)
    robe = [(47.4, 31.2), (53.4, 31.2), (55.2, 46), (56.2, 56), (45.0, 56), (46.2, 44)]
    tablet = [(53.6, 33.4), (57.2, 34.4), (56.4, 41.2), (52.8, 40.2)]
    pedestal = rect(45.0, 56, 56.2, 62)
    plinth = rect(43.0, 62, 58.2, 70)
    return [('stone', bar), ('gold', spire), ('stone', belfry), ('gold', shaft), ('stone', base), ('paper', clock),
            ('gold', flame), ('patina', handle), ('patina', arm), ('patina', robe), ('patina', tablet), ('patina', head), ('patina', crown),
            ('stone', pedestal), ('stone', plinth)], []

def temple_of_heaven():
    def roof(y0, y1, hw0, hw1):
        return mirror([(32 + hw0, y0), (32 + hw1 + 1.2, y1 - 1.4), (32 + hw1 + 0.6, y1), (32 + hw1 - 1.6, y1 + 0.2)])
    fin = [(32, 11), (33.0, 13.5), (32.8, 17.5), (31.2, 17.5), (31.0, 13.5)]
    return [('gold', fin), ('blue', roof(17, 24, 1.6, 9.5)), ('red', trap(32, 24, 6.4, 27, 6.4)), ('blue', roof(27, 34, 6.8, 13.5)),
            ('red', trap(32, 34, 10.4, 37, 10.4)), ('blue', roof(37, 45, 10.8, 18)), ('red', trap(32, 45, 14, 54, 14)),
            ('marble', trap(32, 54, 20, 58, 20)), ('marble', trap(32, 58, 24, 63, 24)), ('marble', trap(32, 63, 28, 69, 28))], []

def taipei101():
    out = [('paper', trap(32, 11, 0.8, 20, 0.8)), ('teal', trap(32, 20, 2.4, 24, 4.0))]
    y = 24
    for k in range(8):
        out.append(('teal', trap(32, y, 4.2, y + 4, 6.2))); y += 4
    out += [('slate', trap(32, 56, 7.0, 63, 7.8)), ('slate', trap(32, 63, 9.6, 69, 9.6))]
    return out, []

def st_basil():
    out = []
    out.append(('gold', onion(32, 11, 2.4, 16)))
    out.append(('green', trap(32, 18, 1.2, 34, 6.0)))
    out.append(('brick', trap(32, 34, 5.0, 44, 5.0)))
    for cx, col in ((20, 'red'), (44, 'blue')):
        out.append((col, onion(cx, 15.5, 6.0, 25)))
        out.append(('brick', trap(cx, 30, 3.6, 44, 3.6)))
    for cx, col in ((9, 'gold'), (55, 'green')):
        out.append((col, onion(cx, 27, 4.8, 35)))
        out.append(('brick', trap(cx, 39, 3.0, 46, 3.0)))
    out.append(('brick', rect(4, 44, 60, 66)))
    out.append(('stone', rect(3, 66, 61, 70)))
    cuts = [circ(32, 56, 3.2, 20, 180, 360) + [(35.2, 66), (28.8, 66)], circ(17, 57, 2.4, 16, 180, 360) + [(19.4, 64), (14.6, 64)],
            circ(47, 57, 2.4, 16, 180, 360) + [(49.4, 64), (44.6, 64)]]
    return out, cuts

def sagrada():
    out = []
    for cx, top in ((17, 19), (26.5, 12.5), (37.5, 12.5), (47, 19)):
        sp = [(cx, top)] + [(cx + 1.4, top + 2.5), (cx + 2.2, top + 9), (cx + 3.2, 46), (cx - 3.2, 46), (cx - 2.2, top + 9), (cx - 1.4, top + 2.5)]
        out.append(('stone', sp)); out.append(('gold', circ(cx, top + 1.6, 1.5, 16)))
    out.append(('stone', rect(8, 44, 56, 70)))
    cuts = [[(28, 70), (28, 57), (32, 51), (36, 57), (36, 70)], [(14, 70), (14, 61), (16.5, 57.5), (19, 61), (19, 70)], [(45, 70), (45, 61), (47.5, 57.5), (50, 61), (50, 70)]]
    return out, cuts

def christ():
    head = circ(32, 15.5, 2.7, 24)
    arms = [(6, 19.6), (32, 18.6), (58, 19.6), (58, 22.0), (32, 23.2), (6, 22.0)]
    robe = trap(32, 21, 4.3, 50, 6.2)
    plinth = trap(32, 50, 6.0, 56, 6.0)
    hill = [(4, 70)] + [(32 + 28 * math.cos(math.radians(a)), 70 - 15 * math.sin(math.radians(a))) for a in range(180, -1, -10)][::-1] + [(60, 70)]
    hill = [(32 - 28 * math.cos(math.radians(a)), 70 - 15 * math.sin(math.radians(a))) for a in range(0, 181, 10)]
    neck = rect(31, 11, 33, 13.5)
    return [('green', hill), ('slate', plinth), ('marble', neck), ('marble', robe), ('marble', arms), ('marble', head)], []

def brandenburg():
    quad = [(26, 22), (27.5, 17.5), (30.5, 16.5), (31.3, 11), (32.7, 11), (33.5, 16.5), (36.5, 17.5), (38, 22)]
    out = [('patina', quad), ('stone', trap(32, 22, 13, 28, 13)), ('stone', trap(32, 28, 27, 33, 27)), ('stone', trap(32, 33, 26, 64, 26)),
           ('stone', trap(32, 64, 29, 70, 29))]
    cuts = [rect(8.5, 37, 14, 64), rect(17.5, 37, 23.5, 64), rect(27, 37, 37, 64), rect(40.5, 37, 46.5, 64), rect(50, 37, 55.5, 64)]
    return out, cuts

def fuji():
    # a wide, low charm: a flat summit with crater notches right under the ring, concave slopes (about 70 deg under the
    # summit easing to about 37 deg at the foot), a two-tone flank like a woodblock print, snow tongues down the gullies,
    # and a sea of clouds as the base. Charms hang at their own lengths, so Fuji ends higher than the towers.
    Y0, HGT = 14.6, 27.4
    def pt(sv):
        return (37.0 + 23.0 * (0.45 * sv + 0.55 * sv * sv), Y0 + HGT * sv)
    right = [(32, 15.2), (33.6, 14.4), (35.2, 15.0), (37.0, 14.6)] + [pt(k / 12) for k in range(1, 13)]
    body = mirror(right)
    shade = [(37.0, 14.6)] + [pt(k / 12) for k in range(1, 13)] + [(48.6, Y0 + HGT), (42.0, 30.0), (38.6, 19.6)]
    snow = mirror([(32, 15.2), (33.6, 14.4), (35.2, 15.0), (37.0, 14.6), pt(0.18), pt(0.30), (38.2, 21.6), (37.2, 25.8),
                   (36.0, 20.6), (34.6, 23.6), (33.4, 19.8), (32, 22.4)])
    clouds = [(3.5, 47.5), (3.5, 41.0)]
    for cx, r in ((8, 4.2), (15.5, 5.0), (24.5, 4.4), (32, 5.2), (39.5, 4.4), (48.5, 5.0), (56.5, 4.2)):
        clouds += [(cx + r * math.cos(math.radians(a)), 41.4 - r * math.sin(math.radians(a))) for a in range(180, -1, -15)]
    clouds += [(60.5, 41.0), (60.5, 47.5)]
    return [('blue', body), ('navy', shade), ('marble', snow), ('marble', clouds)], []

def seoul_tower():
    hill = [(32 - 28 * math.cos(math.radians(a)), 70 - 18 * math.sin(math.radians(a))) for a in range(0, 181, 10)]
    mast = trap(32, 11, 0.7, 24, 1.2)
    deck = [(26.5, 24.5), (37.5, 24.5), (38.5, 27), (38.5, 31), (37, 33), (27, 33), (25.5, 31), (25.5, 27)]
    band = rect(25.5, 27.6, 38.5, 29.6)
    shaft = trap(32, 33, 2.2, 56, 3.4)
    return [('green', hill), ('marble', shaft), ('marble', mast), ('red', rect(31.2, 11, 32.8, 14)), ('marble', deck), ('blue', band)], []

def galata():
    out = [('gold', trap(32, 11, 0.6, 15.5, 0.9)), ('slate', [(32, 14.5), (41, 28), (23, 28)]), ('stone', trap(32, 28, 10, 31, 10)),
           ('stone', trap(32, 31, 8.6, 38, 8.6)), ('stone', trap(32, 38, 8.2, 64, 9.6)), ('stone', trap(32, 64, 11, 70, 11))]
    cuts = [circ(cx, 33.6, 1.3, 12, 180, 360) + [(cx + 1.3, 36.6), (cx - 1.3, 36.6)] for cx in (26.5, 32, 37.5)]
    cuts += [rect(31, 46, 33, 51)]
    return out, cuts

def st_mary_krakow():
    # St Mary's Basilica, Krakow: the taller north tower (gilded crown on its helm) under the ring, the lower bell tower right
    needle = trap(31.6, 11, 0.6, 15, 0.8)
    helm = [(31.6, 14.5), (34.6, 22), (36.4, 29), (26.8, 29), (28.6, 22)]
    crown = trap(31.6, 19.4, 3.0, 21.6, 3.8)
    pinl = [(26.0, 23.5), (27.2, 29), (24.8, 29)]
    pinr = [(37.2, 23.5), (38.4, 29), (36.0, 29)]
    tall = rect(26.0, 29, 37.2, 70)
    dome = [(46, 25.5), (47.0, 27.5)] + [(46 + 5.2 * math.cos(math.radians(a)), 34 - 5.6 * math.sin(math.radians(a))) for a in range(20, 161, 10)] + [(45.0, 27.5)]
    dome = [(46 + 5.4 * math.cos(math.radians(a)), 34.5 - 6.0 * math.sin(math.radians(a))) for a in range(0, 181, 10)]
    lantern = trap(46, 25.5, 0.8, 29.2, 1.2)
    short = rect(40.6, 34.5, 51.4, 70)
    front = rect(37.2, 46, 40.6, 70)
    base = rect(22.6, 66, 55, 70)
    return [('brick', front), ('brick', tall), ('brick', short), ('patina', helm), ('patina', pinl), ('patina', pinr), ('gold', crown),
            ('paper', needle), ('gold', lantern), ('patina', dome), ('stone', base)], [[(30.2, 70), (30.2, 58), (31.6, 55.4), (33.0, 58), (33.0, 70)],
            [(44.8, 66), (44.8, 50), (46, 47.6), (47.2, 50), (47.2, 66)], rect(30.4, 36, 32.8, 44)]

def syrenka():
    sword = [(32, 11), (33.4, 12.6), (33.6, 24.0), (31.8, 24.0)]
    guard = rect(30.2, 23.8, 35.2, 25.2)
    arm = [(31.8, 25.0), (34.2, 25.0), (36.2, 32.6), (33.8, 33.4)]
    head = circ(30.8, 30.2, 2.6, 24)
    torso = [(27.4, 33.2), (35.2, 33.0), (34.6, 45.0), (28.6, 45.2)]
    tail = [(28.6, 45.0), (34.6, 45.0), (36.2, 52.0), (34.2, 58.6), (28.2, 62.2), (21.2, 61.2), (17.6, 57.2), (19.4, 54.4), (22.4, 57.0), (27.2, 57.4), (30.2, 54.0), (30.0, 50.0)]
    fin = [(18.4, 57.6), (12.0, 52.6), (13.2, 60.4)]
    shield = circ(24.6, 40.2, 6.4, 32)
    rock = [(13, 70), (17, 65.2), (30, 63.6), (44, 65.0), (49, 70)]
    return [('slate', rock), ('bronze', tail), ('bronze', fin), ('bronze', torso), ('bronze', head), ('bronze', arm),
            ('paper', sword), ('gold', guard), ('gold', shield)], []

def st_andrew():
    out = [('gold', trap(32, 11, 0.7, 16, 0.9)), ('green', trap(32, 16, 1.6, 19.5, 1.6)),
           ('green', [(32 + 8.6 * math.cos(math.radians(a)), 30 - 11 * math.sin(math.radians(a))) for a in range(0, 181, 10)]),
           ('turq', trap(32, 30, 7.2, 40, 7.2))]
    for cx in (12, 52):
        out.append(('gold', trap(cx, 18.5, 0.5, 22, 0.7)))
        out.append(('green', [(cx + 3.2 * math.cos(math.radians(a)), 27 - 5 * math.sin(math.radians(a))) for a in range(0, 181, 15)]))
        out.append(('turq', trap(cx, 27, 3.0, 46, 3.0)))
    out += [('turq', trap(32, 40, 23, 64, 23)), ('marble', trap(32, 64, 27, 70, 27))]
    cuts = [circ(cx, 51, 2.0, 14, 180, 360) + [(cx + 2.0, 58), (cx - 2.0, 58)] for cx in (20, 32, 44)]
    return out, cuts

def pisa():
    parts = [('marble', trap(32, 15, 3.6, 18, 4.6)), ('marble', trap(32, 18, 5.6, 26, 5.6))]
    y = 26
    for k in range(6):
        parts.append(('marble', trap(32, y, 7.6, y + 5.4, 7.6))); y += 5.4
    parts.append(('stone', trap(32, 58.4, 8.6, 66, 8.6)))
    lines = [rect(23.6, 25.3 + 5.4 * k, 40.4, 26.3 + 5.4 * k) for k in range(7)]
    out = [(c, rot(p, -7.5, 32, 66)) for c, p in parts]
    out += [('stone', rot(l, -7.5, 32, 66)) for l in lines]
    out += [('stone', rect(20, 66, 44, 70)), ('marble', rot(rect(31.2, 11, 32.8, 15.5), -7.5, 32, 66))]
    return out, []

CHARMS = [('EN', 'English', 'Elizabeth Tower + Statue of Liberty', bigben_liberty), ('ZH-HANS', '简体中文', 'Temple of Heaven', temple_of_heaven),
          ('RU', 'Русский', "St Basil's Cathedral", st_basil), ('ES', 'Español', 'Sagrada Família', sagrada),
          ('PT-BR', 'Português (Brasil)', 'Christ the Redeemer', christ), ('DE', 'Deutsch', 'Brandenburg Gate', brandenburg),
          ('FR', 'Français', 'Eiffel Tower', eiffel), ('JA', '日本語', 'Mount Fuji', fuji), ('KO', '한국어', 'N Seoul Tower', seoul_tower),
          ('PL', 'Polski', "St Mary's Basilica, Kraków", st_mary_krakow), ('ZH-HANT', '繁體中文', 'Taipei 101', taipei101), ('TR', 'Türkçe', 'Galata Tower', galata),
          ('UK', 'Українська', "St Andrew's Church", st_andrew), ('IT', 'Italiano', 'Leaning Tower of Pisa', pisa)]

RIM_OUT, RIM_IN = 1.5, 0.55

def render(fn, S=16):
    layers, cuts = fn()
    big = Image.new('RGBA', (W * S, H * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(big)
    sc = lambda pts: [(x * S, y * S) for x, y in pts]
    allp = [TAB['body']] + [it[1] for it in layers if len(it) == 2]
    for p in allp:  # outer rim: every field filled and stroked wide in paper
        d.polygon(sc(p), fill=PAPER + (255,))
        d.line(sc(p + [p[0]]), fill=PAPER + (255,), width=int(2 * RIM_OUT * S), joint='curve')
    for item in layers:  # enamel fields, each with a thin cloisonne line; 3-tuples are detail lines (colour, polyline, width u)
        if len(item) == 3:
            col, p, w = item
            d.line(sc(p), fill=C[col] + (255,), width=max(1, int(w * S)), joint='curve')
            continue
        col, p = item
        d.polygon(sc(p), fill=C[col] + (255,))
        d.line(sc(p + [p[0]]), fill=PAPER + (255,), width=max(1, int(RIM_IN * S)), joint='curve')
    hole_mask = Image.new('L', big.size, 0); hd = ImageDraw.Draw(hole_mask)
    for p in cuts + [TAB['hole']]:
        hd.polygon(sc(p), fill=255)
    rim_ring = Image.new('L', big.size, 0); rd = ImageDraw.Draw(rim_ring)
    for p in cuts:
        rd.line(sc(p + [p[0]]), fill=255, width=int(2 * RIM_IN * S * 1.6), joint='curve')
    big.paste(PAPER + (255,), (0, 0), rim_ring)
    a = big.getchannel('A'); a = Image.composite(Image.new('L', big.size, 0), a, hole_mask); big.putalpha(a)
    return big

if __name__ == '__main__':
    out = os.path.expanduser('~/FrontRoomsVisualWork/lang_charms/out')   # never inside the project
    for code, name, lm, fn in CHARMS:
        big = render(fn)
        for k, px in (('8x', 8), ('4x', 4)):
            big.resize((W * px, H * px), Image.LANCZOS).save(os.path.join(out, f'LangCharm_{code}@{k}.png'))
    print('ok', len(CHARMS))


# ---- English options (Red 10-31: the bar pairing is out; two landmarks must share one charm)
def _bigben(cx, tip, scale=1.0):
    s = scale
    spire = [(cx, tip), (cx + 1.2 * s, tip + 4 * s), (cx + 4 * s, tip + 11.5 * s), (cx - 4 * s, tip + 11.5 * s), (cx - 1.2 * s, tip + 4 * s)]
    by = tip + 11.5 * s
    belfry = rect(cx - 5.5 * s, by, cx + 5.5 * s, by + 11 * s)
    clock = circ(cx, by + 5.5 * s, 3.3 * s, 28)
    return spire, belfry, clock, by + 11 * s

def _liberty(dx, dy, s):
    P = lambda pts: [(dx + x * s, dy + y * s) for x, y in pts]
    flame = P([(0.0, 0.0), (1.8, 2.0), (1.6, 4.6), (-1.4, 4.6), (-1.6, 2.0)])
    handle = P([(-0.6, 4.4), (0.6, 4.4), (0.6, 6.6), (-0.6, 6.6)])
    arm = P([(-1.0, 6.4), (1.2, 6.4), (3.2, 15.4), (1.0, 16.2)])
    crown = P([(0.0, 12.2), (1.4, 13.4), (1.2, 9.8), (2.8, 12.4), (3.6, 9.0), (4.4, 12.4), (6.2, 10.0), (5.8, 13.6), (7.2, 12.6), (6.0, 15.6), (0.6, 15.6)])
    head = [(dx + 3.4 * s + 2.3 * s * math.cos(math.radians(a)), dy + 16.0 * s + 2.3 * s * math.sin(math.radians(a))) for a in range(0, 361, 15)]
    robe = P([(0.4, 18.2), (6.4, 18.2), (8.2, 33.0), (9.2, 43.0), (-2.0, 43.0), (-0.8, 31.0)])
    tablet = P([(6.6, 20.4), (10.2, 21.4), (9.4, 28.2), (5.8, 27.2)])
    return flame, handle, arm, crown, head, robe, tablet

def en_side():
    # E2 side by side: Big Ben's spire and Liberty's torch both meet the ring; one shared plinth; no bar
    spire, belfry, clock, sy = _bigben(26.0, 12.0)
    shaft = rect(21.5, sy, 30.5, 63)
    flame, handle, arm, crown, head, robe, tablet = _liberty(36.0, 12.0, 1.0)
    ped = rect(34.0, 55, 46.0, 63)
    plinth = rect(17.5, 63, 50.5, 69.5)
    return [('stone', plinth), ('gold', spire), ('stone', belfry), ('paper', clock), ('gold', shaft),
            ('stone', ped), ('gold', flame), ('patina', handle), ('patina', arm), ('patina', robe), ('patina', tablet), ('patina', head), ('patina', crown)], []

def en_layered():
    # E3 layered: Big Ben behind under the ring; Liberty smaller in front, lower right, overlapping the tower; one plinth
    spire, belfry, clock, sy = _bigben(29.5, 11.5)
    shaft = rect(25.0, sy, 34.0, 64)
    flame, handle, arm, crown, head, robe, tablet = _liberty(34.2, 30.5, 0.78)
    ped = rect(31.6, 64 - 7.6, 43.4, 64)
    plinth = rect(19.5, 63.4, 48.5, 69.5)
    return [('stone', plinth), ('gold', spire), ('stone', belfry), ('paper', clock), ('gold', shaft),
            ('stone', ped), ('gold', flame), ('patina', handle), ('patina', arm), ('patina', robe), ('patina', tablet), ('patina', head), ('patina', crown)], []


# ---- v2 set (Red 10-31: pick what reads from afar; food and objects are fine; every charm its own outline and colour)
def ell(cx, cy, rx, ry, rotd=0, n=36):
    a = math.radians(rotd); c, sn = math.cos(a), math.sin(a)
    return [(cx + rx * math.cos(t) * c - ry * math.sin(t) * sn, cy + rx * math.cos(t) * sn + ry * math.sin(t) * c) for t in [2 * math.pi * i / n for i in range(n)]]

def panda():
    face = ell(32, 37, 22, 19.5)
    ears = [ell(15.5, 21.5, 7.2, 7.2), ell(48.5, 21.5, 7.2, 7.2)]
    patches = [ell(23.4, 37.5, 5.6, 7.6, -28), ell(40.6, 37.5, 5.6, 7.6, 28)]
    eyes = [ell(24.4, 36.6, 1.7, 1.9), ell(39.6, 36.6, 1.7, 1.9)]
    nose = ell(32, 46.2, 3.2, 2.2)
    neck = rect(30.5, 11, 33.5, 19)
    return [('marble', neck)] + [('black', e) for e in ears] + [('marble', face)] + [('black', p) for p in patches] + [('marble', e) for e in eyes] + [('black', nose)], []

def bubble_tea():
    straw = rot(rect(30.4, 9.5, 34.6, 34), 8, 32.5, 22)
    lid = [(32 + 15.5 * math.cos(math.radians(a)), 27 - 7.5 * math.sin(math.radians(a))) for a in range(0, 181, 10)]
    rimb = rect(15.5, 26.2, 48.5, 29.2)
    cup = [(16.5, 29), (47.5, 29), (43.5, 68), (20.5, 68)]
    tea = [(17.0, 35), (47.0, 35), (43.6, 67.4), (20.4, 67.4)]
    pearls = [ell(x, y, 2.6, 2.6) for x, y in ((24.5, 63.5), (30, 64), (35.5, 63.6), (40.5, 63.2), (27, 58.8), (32.6, 59.0), (38, 58.6), (23.6, 54.6))]
    return [('marble', cup), ('lav', tea)] + [('black', pp) for pp in pearls] + [('marble', rimb), ('marble', lid), ('red', straw)], []

def matryoshka():
    body = ell(32, 50, 17.5, 19.5)
    head = ell(32, 25, 11.5, 12.5)
    scarf = ell(32, 25.5, 11.5, 12.5)
    face = ell(32, 26.5, 7.0, 7.4)
    apron = ell(32, 52, 11, 13.5)
    flower = [ell(32 + 3.6 * math.cos(math.radians(a)), 52 + 3.6 * math.sin(math.radians(a)), 2.6, 2.6) for a in range(0, 360, 72)]
    center = ell(32, 52, 2.2, 2.2)
    cheeks = [ell(28.6, 29.4, 1.5, 1.2), ell(35.4, 29.4, 1.5, 1.2)]
    base = rect(18, 66, 46, 69.6)
    return [('red', body), ('red', scarf), ('stone', face), ('gold', apron)] + [('red', f) for f in flower] + [('gold', center)] + [('red', c) for c in cheeks] + [('red', base)], []

def guitar():
    upper = ell(32, 41, 10.2, 9.6); lower = ell(32, 56, 13.2, 12.6)
    waist = rect(23.5, 40, 40.5, 56)
    neck = rect(29.8, 17, 34.2, 33)
    head = [(28.6, 11), (35.4, 11), (35.8, 18), (28.2, 18)]
    hole = ell(32, 43.5, 4.0, 4.0)
    rosette = ell(32, 43.5, 5.6, 5.6)
    bridge = rect(26.5, 57.5, 37.5, 60)
    return [('spruce', lower), ('spruce', upper), ('spruce', waist), ('woodd', neck), ('woodd', head), ('gold', rosette), ('black', hole), ('woodd', bridge)], []

def pretzel():
    outer = [(11, 27), (13, 18), (20, 11.6), (28, 12.4), (32, 16.4), (36, 12.4), (44, 11.6), (51, 18), (53, 27), (52, 37), (48, 47), (41, 55), (32, 59), (23, 55), (16, 47), (12, 37)]
    holes = [ell(22.6, 27.4, 5.0, 7.0, -22), ell(41.4, 27.4, 5.0, 7.0, 22), [(27.6, 41.5), (36.4, 41.5), (32, 50)]]
    salt = [ell(x, y, 1.1, 0.9, 30) for x, y in ((17, 22), (25, 15.5), (39, 15.5), (47, 22), (15.5, 38), (48.5, 38), (24, 50), (40, 50), (32, 55.5))]
    return [('lye', outer)] + [('marble', ss) for ss in salt], holes

def croissant():
    out = []
    segs = [(208, 5.2, 7.0, 'amber2'), (232, 6.6, 8.6, 'amber'), (270, 7.4, 10.2, 'amber2'), (308, 6.6, 8.6, 'amber'), (332, 5.2, 7.0, 'amber2')]
    cx, cy, R = 32, 50, 22.5
    order = [0, 4, 1, 3, 2]
    for k in order:
        a, rw, rl, col = segs[k]
        x, y = cx + R * math.cos(math.radians(a)), cy + R * math.sin(math.radians(a))
        out.append((col, ell(x, y, rl, rw, a + 90)))
    tips = [ell(cx + (R + 1) * math.cos(math.radians(188)), cy + (R + 1) * math.sin(math.radians(188)) + 3, 2.6, 4.2, 10),
            ell(cx + (R + 1) * math.cos(math.radians(352)), cy + (R + 1) * math.sin(math.radians(352)) + 3, 2.6, 4.2, -10)]
    return [('amber', t) for t in tips] + out, []

def hahoe():
    face = [(32 + 15.5 * math.cos(math.radians(a)), 33 + 20.5 * math.sin(math.radians(a))) for a in range(-180, 1, 10)] + [(47.0, 40), (43, 47), (32, 49.5), (21, 47), (17.0, 40)]
    jaw = [(20.5, 51.5), (32, 54), (43.5, 51.5), (41.5, 59), (36, 64), (28, 64), (22.5, 59)]
    brows = [rot([(20, 25.5), (29.5, 24.0), (29.5, 26.0), (20.5, 27.6)], -6, 25, 25.5), rot([(34.5, 24.0), (44, 25.5), (43.5, 27.6), (34.5, 26.0)], 6, 39, 25.5)]
    nose = [(30.4, 33), (33.6, 33), (35.2, 41.6), (28.8, 41.6)]
    eye_l = [(19.8, 33.4)] + [(25 + 5.4 * math.cos(math.radians(a)), 33.6 - 3.4 * math.sin(math.radians(a))) for a in range(170, 9, -20)] + [(30.2, 33.4), (25, 32.2)]
    eye_r = [(64 - x, y) for x, y in eye_l]
    smile = [(21.5, 44.5), (32, 47.8), (42.5, 44.5), (41.2, 46.6), (32, 50.2), (22.8, 46.6)]
    return [('wood', face), ('wood', jaw), ('woodd', brows[0]), ('woodd', brows[1]), ('wood', nose)], [eye_l, eye_r, smile]

V2_ = [('EN', 'English', 'Elizabeth Tower + Statue of Liberty, side by side', en_side), ('ZH-HANS', '简体中文', 'Panda', panda),
      ('RU', 'Русский', 'Matryoshka', matryoshka), ('ES', 'Español', 'Spanish guitar', guitar),
      ('PT-BR', 'Português (Brasil)', 'Christ the Redeemer', christ), ('DE', 'Deutsch', 'Pretzel', pretzel),
      ('FR', 'Français', 'Croissant', croissant), ('JA', '日本語', 'Mount Fuji', fuji), ('KO', '한국어', 'Hahoe mask', hahoe),
      ('ZH-HANT', '繁體中文', 'Bubble tea', bubble_tea)]


def _cr(pts, n=10):
    out = []
    P = [pts[0]] + pts + [pts[-1]]
    for i in range(1, len(P) - 2):
        p0, p1, p2, p3 = P[i - 1], P[i], P[i + 1], P[i + 2]
        for k in range(n):
            t = k / n; t2, t3 = t * t, t * t * t
            out.append(tuple(0.5 * ((2 * p1[j]) + (-p0[j] + p2[j]) * t + (2 * p0[j] - 5 * p1[j] + 4 * p2[j] - p3[j]) * t2 + (-p0[j] + 3 * p1[j] - 3 * p2[j] + p3[j]) * t3) for j in (0, 1)))
    out.append(pts[-1]); return out

def rope(pts, w0, w1=None):
    """a smooth rope along pts, width w0 at the start easing to w1 at the end, round caps"""
    w1 = w0 if w1 is None else w1
    c = _cr(pts); L, R = [], []
    for i, (x, y) in enumerate(c):
        a = c[max(0, i - 1)]; b = c[min(len(c) - 1, i + 1)]
        dx, dy = b[0] - a[0], b[1] - a[1]; m = math.hypot(dx, dy) or 1
        nx, ny = -dy / m, dx / m; w = (w0 + (w1 - w0) * i / (len(c) - 1)) / 2
        L.append((x + nx * w, y + ny * w)); R.append((x - nx * w, y - ny * w))
    e, sx = c[-1], c[-2]; ang = math.atan2(e[1] - sx[1], e[0] - sx[0])
    cap_e = [(e[0] + w1 / 2 * math.cos(ang + math.pi / 2 - math.pi * k / 8), e[1] + w1 / 2 * math.sin(ang + math.pi / 2 - math.pi * k / 8)) for k in range(9)]
    b0, b1 = c[0], c[1]; ang0 = math.atan2(b0[1] - b1[1], b0[0] - b1[0])
    cap_s = [(b0[0] + w0 / 2 * math.cos(ang0 + math.pi / 2 - math.pi * k / 8), b0[1] + w0 / 2 * math.sin(ang0 + math.pi / 2 - math.pi * k / 8)) for k in range(9)]
    return L + cap_e + list(reversed(R)) + cap_s

def pretzel():
    belly = rope([(13.5, 33), (15.5, 45), (24, 53.5), (32, 55.5), (40, 53.5), (48.5, 45), (50.5, 33)], 9.5)
    arm_r = rope([(50.5, 34), (52.5, 24), (47, 15.5), (39, 13.6), (33, 18), (29.5, 26), (24.5, 40), (21.5, 46.5)], 6.6, 5.6)
    arm_l = rope([(13.5, 34), (11.5, 24), (17, 15.5), (25, 13.6), (31, 18), (34.5, 26), (39.5, 40), (42.5, 46.5)], 6.6, 5.6)
    salt = [ell(x, y, 1.15, 0.95, 30) for x, y in ((20.5, 50.5), (27, 54.6), (34, 55.2), (41, 52.6), (46.6, 47.2), (17, 44))]
    return [('lye', belly), ('lye', arm_r), ('lye', arm_l)] + [('marble', ss) for ss in salt], []

def croissant():
    out = []
    segs = [(206, 5.4, 7.4, 'amber2'), (232, 6.8, 8.8, 'amber'), (270, 7.4, 10.4, 'amber2'), (308, 6.8, 8.8, 'amber'), (334, 5.4, 7.4, 'amber2')]
    cx, cy, R = 32, 42.2, 22.5
    tips = [ell(cx + (R - 0.5) * math.cos(math.radians(194)), cy + (R - 0.5) * math.sin(math.radians(194)) + 5.2, 3.0, 4.6, 14),
            ell(cx + (R - 0.5) * math.cos(math.radians(346)), cy + (R - 0.5) * math.sin(math.radians(346)) + 5.2, 3.0, 4.6, -14)]
    out += [('amber', t) for t in tips]
    for k in [0, 4, 1, 3, 2]:
        a, rw, rl, col = segs[k]
        x, y = cx + R * math.cos(math.radians(a)), cy + R * math.sin(math.radians(a))
        out.append((col, ell(x, y, rl, rw, a + 90)))
    return out, []

def hahoe():
    face = [(32 + 16 * math.cos(math.radians(a)), 32 + 19 * math.sin(math.radians(a))) for a in range(-180, 1, 10)] + [(47.6, 38), (44.5, 45.5), (32, 48.4), (19.5, 45.5), (16.4, 38)]
    jaw = [(19.6, 50.4), (32, 54.6), (44.4, 50.4), (42.8, 57.6), (36.6, 63), (27.4, 63), (21.2, 57.6)]
    def crescent(cx, cy, r, t):
        top = [(cx + r * math.cos(math.radians(a)), cy - r * 0.62 * math.sin(math.radians(a))) for a in range(180, -1, -15)]
        bot = [(cx + (r - t) * math.cos(math.radians(a)), cy + 0.6 - (r - t) * 0.45 * math.sin(math.radians(a))) for a in range(0, 181, 15)]
        return top + bot
    eyes = [crescent(24.6, 33.6, 5.6, 2.2), crescent(39.4, 33.6, 5.6, 2.2)]
    brows = [[(18.6, 25.6), (25, 23.6), (30.2, 25.2), (30, 26.8), (25, 25.4), (18.8, 27.4)], [(45.4, 25.6), (39, 23.6), (33.8, 25.2), (34, 26.8), (39, 25.4), (45.2, 27.4)]]
    smile = [(19.2, 46.2)] + [(32 + 13.4 * math.cos(math.radians(a)), 46.0 + 3.6 * math.sin(math.radians(a))) for a in range(180, -1, -15)] + [(44.8, 46.2)] + \
            [(32 + 12.0 * math.cos(math.radians(a)), 48.6 + 2.0 * math.sin(math.radians(a))) for a in range(0, 181, 15)]
    return [('wood', face), ('wood', jaw), ('woodd', brows[0]), ('woodd', brows[1])], eyes + [smile]

V2 = [('EN', 'English', 'Elizabeth Tower + Statue of Liberty, side by side', en_side), ('ZH-HANS', '简体中文', 'Panda', panda),
      ('RU', 'Русский', 'Matryoshka', matryoshka), ('ES', 'Español', 'Spanish guitar', guitar),
      ('PT-BR', 'Português (Brasil)', 'Christ the Redeemer', christ), ('DE', 'Deutsch', 'Pretzel', pretzel),
      ('FR', 'Français', 'Croissant', croissant), ('JA', '日本語', 'Mount Fuji', fuji), ('KO', '한국어', 'Hahoe mask', hahoe),
      ('ZH-HANT', '繁體中文', 'Bubble tea', bubble_tea)]


# ---- v3 (Red 10-31: refine the drawing; use the building where it reads better: Eiffel for French; English E2 stays; Fuji stays)
def _sym(right):
    return mirror(right)

def eiffel_v3():
    R = [(32.5, 11), (32.7, 14.6), (33.8, 14.6), (33.8, 17.6), (33.6, 18.4), (34.0, 22), (34.6, 26), (35.2, 29.6), (38.2, 29.6), (38.2, 31.8),
         (36.0, 31.8), (36.9, 35.5), (38.0, 39.5), (39.6, 43.8), (44.6, 43.8), (44.6, 46.8), (43.0, 46.8), (44.6, 51.5), (46.6, 56.5),
         (49.0, 62), (51.6, 67.4), (52.6, 69.6), (45.4, 69.6), (45.2, 67.4)]
    arch = [(32 + 13.2 * math.cos(math.radians(a)), 69.6 - 14.2 * math.sin(math.radians(a))) for a in range(5, 86, 10)]
    right = R + arch + [(32, 55.4)]
    body = _sym(right)
    shade = [(32, 11), ] + right[:] 
    shade = [(32, 11.2)] + R + arch + [(32, 55.4)]
    lines = []
    def leg(sign):
        X = lambda x: 32 + sign * (x - 32)
        for y0, y1, xo0, xo1, xi0, xi1 in ((47.0, 52.0, 43.2, 44.8, 37.6, 40.4), (52.0, 57.5, 44.8, 46.9, 40.4, 42.6), (57.5, 63.0, 46.9, 49.4, 42.6, 44.4)):
            lines.append(('paper', [(X(xi0), y0), (X(xo1), y1)], 0.42))
            lines.append(('paper', [(X(xo0), y0), (X(xi1), y1)], 0.42))
            lines.append(('paper', [(X(xi1), y1), (X(xo1), y1)], 0.42))
    leg(1); leg(-1)
    for y0, y1, h0, h1 in ((32.2, 37.6, 3.7, 5.4), (37.6, 43.4, 5.4, 7.3)):
        lines.append(('paper', [(32 - h0, y0), (32 + h1, y1)], 0.42)); lines.append(('paper', [(32 + h0, y0), (32 - h1, y1)], 0.42))
    lines.append(('paper', [(32 - 1.8, 22), (32 + 2.4, 28.6)], 0.38)); lines.append(('paper', [(32 + 1.8, 22), (32 - 2.4, 28.6)], 0.38))
    return [('amber', body), ('bronze', shade)] + lines, []

def temple_v3():
    def roof(y0, y1, hw0, hw1):
        return mirror([(32 + hw0, y0), (32 + hw1 + 1.4, y1 - 1.6), (32 + hw1 + 0.8, y1), (32 + hw1 - 1.8, y1 + 0.2)])
    def roof_hi(y0, y1, hw0, hw1):  # the lit left half
        return [(32 - hw0, y0), (32, y0 - 0.6), (32, y1 + 0.2), (32 - hw1 + 1.8, y1 + 0.2), (32 - hw1 - 0.8, y1), (32 - hw1 - 1.4, y1 - 1.6)]
    out = [('gold', [(32, 10.8), (33.2, 13.6), (33.0, 16.6), (31.0, 16.6), (30.8, 13.6)])]
    tiers = [(17, 24, 1.6, 9.5, 24, 27, 6.4), (27, 34, 6.8, 13.5, 34, 37, 10.4), (37, 45, 10.8, 18.0, 45, 54, 14.0)]
    for y0, y1, h0, h1, d0, d1, dh in tiers:
        out += [('blue', roof(y0, y1, h0, h1)), ('blue2', roof_hi(y0, y1, h0, h1)), ('gold', [(32 - h1 - 0.8, y1 - 0.2), (32 + h1 + 0.8, y1 - 0.2)], 0.5)]
        out.append(('red', trap(32, d0, dh, d1, dh)))
        if d1 - d0 > 4:
            for k in range(-3, 4):
                out.append(('gold', [(32 + k * dh / 3.6, d0 + 0.8), (32 + k * dh / 3.6, d1 - 0.6)], 0.42))
        else:
            out.append(('gold', [(32 - dh + 0.6, (d0 + d1) / 2), (32 + dh - 0.6, (d0 + d1) / 2)], 0.42))
    for y0, y1, hw in ((54, 58, 20), (58, 63, 24), (63, 69, 28)):
        out.append(('marble', trap(32, y0, hw, y1, hw)))
        out.append(('stone', [(32 - hw + 0.8, y0 + 1.4), (32 + hw - 0.8, y0 + 1.4)], 0.42))
        for k in range(-int(hw / 4), int(hw / 4) + 1):
            out.append(('stone', [(32 + k * 4, y0 + 1.4), (32 + k * 4, y1 - 0.4)], 0.36))
    return out, []

def gate_v3():
    quad = [(25.4, 22.6), (26.6, 18.8), (28.4, 17.8), (29.4, 19.2), (30.4, 16.4), (31.2, 11.6), (32.8, 11.6), (33.6, 16.4), (34.6, 19.2), (35.6, 17.8), (37.4, 18.8), (38.6, 22.6)]
    out = [('patina', quad), ('patina', [(32, 11.2), (32, 15.6)], 0.5),
           ('sand', trap(32, 22.4, 13.6, 28.4, 13.6)), ('sand2', trap(32, 28.4, 27.6, 33.6, 27.6)), ('sand', trap(32, 33.6, 26.4, 64.2, 26.4)),
           ('sand2', trap(32, 64.2, 29.4, 69.6, 29.4))]
    out.append(('sand2', [(19.2, 25.4), (44.8, 25.4)], 0.42))
    for k in range(-12, 13):
        out.append(('sand', [(32 + k * 2.1, 29.6), (32 + k * 2.1, 32.4)], 0.42))
    cols = [(5.8, 8.6), (14.0, 17.4), (23.6, 27.0), (37.0, 40.4), (46.6, 50.0), (55.4, 58.2)]
    for a, b in cols:
        out.append(('sand2', [(b - 0.9, 34.4), (b - 0.9, 63.6)], 0.42))
        out.append(('sand2', [(a + (b - a) / 2, 35.0), (a + (b - a) / 2, 63.0)], 0.36))
    cuts = [rect(8.6, 37.2, 14.0, 64.2), rect(17.4, 37.2, 23.6, 64.2), rect(27.0, 37.2, 37.0, 64.2), rect(40.4, 37.2, 46.6, 64.2), rect(50.0, 37.2, 55.4, 64.2)]
    return out, cuts

def bubble_tea_v3():
    straw = rot(rect(30.6, 9.0, 35.0, 37.0), 9, 32.8, 22)
    straw_hi = rot(rect(30.6, 9.0, 32.2, 37.0), 9, 32.8, 22)
    seal = rect(15.0, 24.6, 49.0, 28.2)
    cup = [(15.8, 28.0), (48.2, 28.0), (44.0, 68.4), (20.0, 68.4)]
    milk = [(16.4, 33.0), (47.6, 33.0), (46.6, 43.0), (17.4, 43.0)]
    tea = [(17.4, 43.0), (46.6, 43.0), (43.8, 67.8), (20.2, 67.8)]
    pearls = [(24.2, 64.2), (29.6, 64.6), (35.0, 64.4), (40.2, 63.8), (26.8, 59.4), (32.2, 59.6), (37.6, 59.2), (23.4, 55.0), (41.0, 54.8)]
    out = [('marble', cup), ('lav', milk), ('lav2', tea)]
    for x, y in pearls:
        out.append(('black', ell(x, y, 2.55, 2.55)))
        out.append(('paper', [(x - 1.0, y - 0.9), (x - 0.3, y - 1.4)], 0.5))
    out += [('marble', seal), ('stone', [(16.0, 26.4), (48.0, 26.4)], 0.42), ('red', straw), ('cream', straw_hi)]
    out += [('stone', [(18.6, 30.6), (21.0, 66.0)], 0.36), ('stone', [(45.4, 30.6), (43.0, 66.0)], 0.36)]
    return out, []

def matryoshka_v3():
    body = ell(32, 50.4, 17.6, 19.4); head = ell(32, 25.4, 11.6, 12.6)
    face = ell(32, 27.0, 7.0, 7.6)
    hair = [(32 + 6.8 * math.cos(math.radians(a)), 26.0 - 6.6 * math.sin(math.radians(a))) for a in range(0, 181, 15)] + [(25.4, 25.6), (29.0, 23.8), (32, 24.6), (35.0, 23.8), (38.6, 25.6)]
    knot = [(28.6, 36.4), (32, 34.2), (35.4, 36.4), (32, 38.6)]
    apron = ell(32, 53.0, 11.6, 13.6)
    out = [('red', body), ('red', head), ('cream', face), ('woodd', hair), ('gold', knot), ('gold', apron)]
    out += [('black', ell(29.4, 28.2, 0.9, 1.0)), ('black', ell(34.6, 28.2, 0.9, 1.0)), ('red', ell(27.8, 30.8, 1.5, 1.1)), ('red', ell(36.2, 30.8, 1.5, 1.1)),
            ('red', ell(32, 32.6, 1.3, 0.7))]
    for cx, cy, r in ((32, 49.0, 3.2), (27.0, 55.6, 2.6), (37.0, 55.6, 2.6)):
        for a in range(0, 360, 72):
            out.append(('red', ell(cx + r * 0.9 * math.cos(math.radians(a)), cy + r * 0.9 * math.sin(math.radians(a)), r * 0.62, r * 0.62)))
        out.append(('gold', ell(cx, cy, r * 0.5, r * 0.5)))
    out += [('green', ell(29.4, 61.2, 2.6, 1.2, -30)), ('green', ell(34.6, 61.2, 2.6, 1.2, 30))]
    for a in range(200, 341, 28):
        out.append(('paper', ell(32 + 11.0 * math.cos(math.radians(a)), 25.4 + 11.6 * math.sin(math.radians(a)), 0.8, 0.8)))
    out.append(('red', rect(18.4, 66.4, 45.6, 69.6)))
    return out, []

def guitar_v3():
    upper = ell(32, 41.6, 10.4, 9.8); lower = ell(32, 56.4, 13.4, 12.8); waist = rect(23.4, 41, 40.6, 56)
    neck = rect(29.9, 18.4, 34.1, 34); head = [(28.4, 10.8), (35.6, 10.8), (36.2, 19.0), (27.8, 19.0)]
    out = [('spruce', lower), ('spruce', upper), ('spruce', waist), ('woodd', neck), ('woodd', head)]
    out += [('black', rect(29.6, 12.2, 31.0, 17.4)), ('black', rect(33.0, 12.2, 34.4, 17.4))]
    for y in (13.4, 15.2, 17.0):
        out += [('stone', ell(26.6, y, 0.9, 0.7)), ('stone', ell(37.4, y, 0.9, 0.7))]
    out += [('gold', ell(32, 44.0, 6.2, 6.2)), ('spruce', ell(32, 44.0, 5.0, 5.0)), ('gold', ell(32, 44.0, 4.4, 4.4)), ('black', ell(32, 44.0, 3.6, 3.6))]
    out += [('woodd', rect(26.2, 58.0, 37.8, 60.6)), ('paper', [(27.8, 58.6), (36.2, 58.6)], 0.42)]
    for y in (21.4, 24.2, 26.8, 29.2, 31.4, 33.4):
        out.append(('stone', [(30.0, y), (34.0, y)], 0.36))
    for k in range(6):
        x0 = 30.6 + k * 0.56; x1 = 29.6 + k * 0.96
        out.append(('paper', [(x0, 19.2), (x0, 34.4), (x1 + 0.1, 58.4)], 0.26))
    return out, []

def christ_v3():
    peak = [(6, 70), (14, 62), (21, 52), (26.4, 46.2), (37.6, 46.2), (43, 52), (50, 61), (58, 70)]
    forest = [(6, 70), (10, 66.4), (16, 63.6), (22, 62.6), (28, 63.6), (34, 62.4), (40, 63.4), (46, 62.8), (52, 65.4), (58, 70)]
    ped = rect(27.6, 40.6, 36.4, 46.4)
    head = ell(32, 15.6, 2.8, 3.0)
    arms = [(5.6, 19.8), (9.0, 19.0), (26.0, 18.6), (32, 18.0), (38.0, 18.6), (55.0, 19.0), (58.4, 19.8), (58.0, 21.6), (55.0, 21.8), (38.0, 23.0), (32, 23.4), (26.0, 23.0), (9.0, 21.8), (6.0, 21.6)]
    robe = [(27.6, 21.0), (36.4, 21.0), (37.8, 33), (38.4, 40.8), (25.6, 40.8), (26.2, 33)]
    robe_sh = [(32, 21.0), (36.4, 21.0), (37.8, 33), (38.4, 40.8), (32, 40.8)]
    neck = rect(31.0, 11.0, 33.0, 13.6)
    out = [('granite', peak), ('green', forest), ('stone', ped), ('marble', neck), ('marble', robe), ('stone', robe_sh), ('marble', arms), ('marble', head)]
    out += [('stone', [(30.2, 24.0), (29.4, 40.0)], 0.4), ('stone', [(33.8, 24.0), (34.8, 40.0)], 0.4), ('stone', [(27.8, 30.0), (36.6, 30.0)], 0.36)]
    return out, []

def hahoe_v3():
    face = [(32 + 16 * math.cos(math.radians(a)), 32 + 19 * math.sin(math.radians(a))) for a in range(-180, 1, 10)] + [(47.6, 38), (44.6, 45.6), (32, 48.2), (19.4, 45.6), (16.4, 38)]
    face_sh = [(32, 13), ] + [(32 + 16 * math.cos(math.radians(a)), 32 + 19 * math.sin(math.radians(a))) for a in range(-90, 1, 10)] + [(47.6, 38), (44.6, 45.6), (32, 48.2)]
    jaw = [(19.8, 50.6), (32, 54.6), (44.2, 50.6), (42.6, 57.8), (36.6, 63.2), (27.4, 63.2), (21.4, 57.8)]
    def crescent(cx, cy, r, t):
        top = [(cx + r * math.cos(math.radians(a)), cy - r * 0.6 * math.sin(math.radians(a))) for a in range(180, -1, -15)]
        bot = [(cx + (r - t) * math.cos(math.radians(a)), cy + 0.6 - (r - t) * 0.42 * math.sin(math.radians(a))) for a in range(0, 181, 15)]
        return top + bot
    eyes = [crescent(24.6, 33.4, 5.6, 2.2), crescent(39.4, 33.4, 5.6, 2.2)]
    smile = [(19.2, 46.0)] + [(32 + 13.4 * math.cos(math.radians(a)), 45.8 + 3.6 * math.sin(math.radians(a))) for a in range(180, -1, -15)] + [(44.8, 46.0)] + \
            [(32 + 12.0 * math.cos(math.radians(a)), 48.4 + 2.0 * math.sin(math.radians(a))) for a in range(0, 181, 15)]
    out = [('woodl', face), ('wood', face_sh), ('woodl', jaw), ('wood', [(32, 54.6), (44.2, 50.6), (42.6, 57.8), (36.6, 63.2), (32, 63.2)])]
    for k, y in enumerate((18.4, 20.6, 22.8)):
        out.append(('woodd', [(32 + (9 - k) * math.cos(math.radians(a)), y + 1.6 * math.sin(math.radians(a))) for a in range(200, 341, 20)], 0.55))
    out += [('woodd', [(18.8, 28.0), (24.6, 25.6), (30.4, 27.6)], 1.1), ('woodd', [(45.2, 28.0), (39.4, 25.6), (33.6, 27.6)], 1.1)]
    out += [('woodd', [(19.6, 31.2), (24.6, 29.8), (29.6, 31.2)], 0.45), ('woodd', [(44.4, 31.2), (39.4, 29.8), (34.4, 31.2)], 0.45)]
    out += [('wood', [(30.4, 33.6), (33.6, 33.6), (35.6, 41.4), (32, 42.6), (28.4, 41.4)]), ('woodd', [(21.0, 38.0), (24.0, 41.4)], 0.45), ('woodd', [(43.0, 38.0), (40.0, 41.4)], 0.45)]
    out += [('woodd', [(24.6, 48.6), (24.0, 51.6)], 0.5), ('woodd', [(39.4, 48.6), (40.0, 51.6)], 0.5)]
    return out, eyes + [smile]

def en_v3():
    layers, cuts = en_side()
    spire, belfry, clock, sy = _bigben(26.0, 12.0)
    extra = [('ink', [(26.0, 29.5), (26.0, 27.2)], 0.5), ('ink', [(26.0, 29.5), (27.6, 30.4)], 0.5),
             ('stone', [(22.4, 36), (22.4, 62)], 0.36), ('stone', [(29.6, 36), (29.6, 62)], 0.36), ('stone', [(26.0, 36), (26.0, 62)], 0.36),
             ('stone', [(39.6, 33.0), (38.6, 54.0)], 0.4), ('stone', [(42.0, 33.0), (42.6, 54.0)], 0.4)]
    return layers + extra, cuts

V3 = [('EN', 'English', 'Elizabeth Tower + Statue of Liberty', en_v3), ('ZH-HANS', '简体中文', 'Temple of Heaven', temple_v3),
      ('RU', 'Русский', 'Matryoshka', matryoshka_v3), ('ES', 'Español', 'Spanish guitar', guitar_v3),
      ('PT-BR', 'Português (Brasil)', 'Christ the Redeemer, Corcovado', christ_v3), ('DE', 'Deutsch', 'Brandenburg Gate', gate_v3),
      ('FR', 'Français', 'Eiffel Tower', eiffel_v3), ('JA', '日本語', 'Mount Fuji', fuji), ('KO', '한국어', 'Hahoe mask', hahoe_v3),
      ('ZH-HANT', '繁體中文', 'Bubble tea', bubble_tea_v3)]
