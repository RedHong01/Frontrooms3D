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
 'brick': (158, 59, 46), 'turq': (79, 163, 165), 'navy': (26, 46, 104), 'ink': (20, 20, 20), 'paper': PAPER,
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
    allp = [TAB['body']] + [p for _, p in layers]
    for p in allp:  # outer rim: every field filled and stroked wide in paper
        d.polygon(sc(p), fill=PAPER + (255,))
        d.line(sc(p + [p[0]]), fill=PAPER + (255,), width=int(2 * RIM_OUT * S), joint='curve')
    for col, p in layers:  # enamel fields, each with a thin cloisonne line
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
