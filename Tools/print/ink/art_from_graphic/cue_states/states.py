# Pattern-native cue states for WP03 hard_edge (32_pattern_native_hints.md §9): baseline, FLOW, HERE, STOP.
import sys, math, os
P='/Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01 Individual Game Project/Frontrooms3D/Tools/print/patterns'
sys.path.insert(0, P)
import hard_edge as he
from PIL import Image, ImageDraw
OUT=sys.argv[1]
PAL={k: tuple(int(v[i:i+2],16) for i in (1,3,5)) for k,v in he.PALETTE.items()}
TW, TH, S = he.TW, he.TH, he.S
Y_APEX2 = he.LOWER_APEX + he.UNIT2_DROP          # 856.5: unit-2 lower apex, tile y
Z_APEX2 = 1393.5                                  # print starts at the floor, 1125 mm blocks: unit-2 lower apex of block 2
def zof(y): return Z_APEX2 + (Y_APEX2 - y)        # y in wall-instance coords (tile y + k*TH)
def band_poly(x0, x1, xc, ya, t):                 # up-pointing constant-thickness band, tile y down
    top=[(x0, ya + S*(xc-x0)), (xc, ya), (x1, ya + S*(x1-xc))]
    bot=[(x, y+t) for x,y in top]
    return top + bot[::-1]
def clipx(poly, xa, xb):
    def clip(poly, keep, inter):
        out=[]
        for i in range(len(poly)):
            a, b = poly[i-1], poly[i]
            ia, ib = keep(a), keep(b)
            if ib:
                if not ia: out.append(inter(a,b))
                out.append(b)
            elif ia: out.append(inter(a,b))
        return out
    def ix(X): return lambda a,b: (X, a[1] + (X-a[0])*(b[1]-a[1])/(b[0]-a[0]))
    p = clip(poly, lambda q: q[0] >= xa, ix(xa))
    return clip(p, lambda q: q[0] <= xb, ix(xb)) if p else []
def rot(poly, cx, cy, deg):                       # rotate in tile coords (y down): +deg = clockwise on screen
    a=math.radians(deg); c, s = math.cos(a), math.sin(a)
    return [(cx + (x-cx)*c - (y-cy)*s, cy + (x-cx)*s + (y-cy)*c) for x,y in poly]
def wall_shapes(state, rolls=4, door=None):
    """Returns ordered (ink, polygon-in-wall-mm (x, z)) list."""
    shapes=[]; field=[]; top=[]
    for r in range(rolls):
        for k in range(-1, 3):
            for u in (0, 1):
                dx = r*TW + u*he.UNIT; ky = k*TH
                X = lambda pts: [(x, zof(y + ky)) for x,y in pts]
                # stripes (static rails) -> drawn last as 'top'
                for x0, x1, ink in he.STRIPES:
                    top.append((ink, X([(x0+dx, 0), (x1+dx, 0), (x1+dx, TH), (x0+dx, TH)])))
                # motif band arrows + diamonds (static)
                T = sum(t for _, t in he.MOTIF_BANDS); depth = S*he.BAND_HW
                for m in range(4):
                    tip = he.MOTIF_EDGE_Y + m*he.PERIOD + depth; y = tip
                    for ink, t in he.MOTIF_BANDS:
                        topl=[(he.BAND_X0+dx, y - S*he.BAND_HW), (he.BAND_XC+dx, y), (he.BAND_X1+dx, y - S*he.BAND_HW)]
                        field.append((ink, X(topl + [(px, py+t) for px,py in topl][::-1]))); y += t
                    hw=he.BAND_HW; hh=S*hw; cx, cy = he.BAND_XC+dx, tip-depth
                    field.append(("cream", X([(cx-hw, cy), (cx, cy-hh), (cx+hw, cy), (cx, cy+hh)])))
                    hw=he.DROP_HW; hh=S*hw; cy = tip + T + he.DROP_GAP + S*he.DROP_HW
                    field.append(("deep", X([(cx-hw, cy), (cx, cy-hh), (cx+hw, cy), (cx, cy+hh)])))
                # field stacks: baseline half-drop; message states slip unit 1 by one half-drop
                dy = (u if state in ('baseline', 'pressure') else 1) * he.UNIT2_DROP
                fx0, fx1, fxc = he.FIELD_X0+dx, he.FIELD_X1+dx, he.FIELD_XC+dx
                y = he.UPPER_APEX + dy
                for ink, t in he.UPPER_BANDS: field.append((ink, X(band_poly(fx0, fx1, fxc, y, t)))); y += t
                ya = he.LOWER_APEX + dy
                Y_KNEE = he.LOWER_APEX + TH                       # unit-1 lower apex in block 1 (z = 831)
                guide = (state not in ('baseline', 'pressure') and abs((ya + ky) - Y_APEX2) < 1) or (state == 'pressure' and (abs((ya + ky) - Y_APEX2) < 1 or abs((ya + ky) - Y_KNEE) < 1))
                gt, ct = he.LOWER_BANDS[0][1], he.LOWER_BANDS[1][1]
                if not guide:
                    field.append(("grey", X(band_poly(fx0, fx1, fxc, ya, gt))))
                    field.append(("cream", X(band_poly(fx0, fx1, fxc, ya + gt, ct))))
                    continue
                # which way does this field point?
                cxw = (fx0 + fx1) / 2
                if state in ('flow', 'pressure'): mode = 'right'
                elif state == 'stop': mode = 'flat'
                elif state == 'here': mode = 'right' if cxw < door[0] else 'left'
                if mode == 'flat':
                    b = band_poly(fx0, fx1, fxc, ya, gt); ys=[p[1] for p in b]; cy=(min(ys)+max(ys))/2
                    field.append(("grey", X([(fx0, cy-gt/2), (fx1, cy-gt/2), (fx1, cy+gt/2), (fx0, cy+gt/2)])))
                else:
                    b = band_poly(fx0, fx1, fxc, ya, gt)
                    ys=[p[1] for p in b]; cy=(min(ys)+max(ys))/2
                    rb = rot(b, fxc, cy, 90 if mode=='right' else -90)
                    field.append(("grey", X(clipx(rb, fx0, fx1))))
    return field + top
def render(state, path, k=0.5, ss=3, z0=300, z1=2400, x1=3000, door=None):
    W, H = int(x1*k*ss), int((z1-z0)*k*ss)
    im = Image.new('RGB', (W, H), PAL['ground']); d = ImageDraw.Draw(im)
    for ink, poly in wall_shapes(state, door=door):
        if len(poly) < 3: continue
        d.polygon([(x*k*ss, (z1-z)*k*ss) for x,z in poly], fill=PAL[ink])
    if door:  # door + trim drawn over the paper
        dx0, dx1 = door[0]-door[1]/2, door[0]+door[1]/2; trim=60; dh=2134
        for (a,b,c,e,col) in [(dx0-trim, dx1+trim, 0, dh+trim, (138,128,112)), (dx0, dx1, 0, dh, (201,194,180))]:
            d.rectangle([a*k*ss, (z1-min(e,z1))*k*ss, b*k*ss, (z1-max(c,z0))*k*ss], fill=col)
        hx = dx1 - 90; hz = 1000
        d.rectangle([(hx-12)*k*ss, (z1-hz-12)*k*ss, (hx+40)*k*ss, (z1-hz+12)*k*ss], fill=(120,110,95))
    im = im.resize((W//ss, H//ss), Image.LANCZOS); im.save(path)
os.makedirs(OUT, exist_ok=True)
for st in ('baseline', 'flow', 'stop'): render(st, f'{OUT}/wall_{st}.png')
render('here', f'{OUT}/wall_here.png', door=(1500, 1000))
render('pressure', f'{OUT}/wall_pressure.png')
# field detail: one roll (two fields) around the guide row at 2 px/mm
for st in ('baseline', 'flow', 'stop'):
    render(st, f'{OUT}/detail_{st}.png', k=2.0, z0=1000, z1=1600, x1=750)
print('ok')
