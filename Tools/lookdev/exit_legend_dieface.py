"""Latin and Cyrillic EXIT legends as plate geometry (平面视觉, 2026-11-04).

exit_dieface.swift draws the word in the EXIT die face and fits it in the opening; this file finishes the holes the
way exit_sign_geom.hole_polys does (CCW, the punch's R 0.5 in every reflex corner) and writes the legend file in the
schema of exit_legend.py (exit_legend_zh-Hans.json), so build_exit_overlay.py takes it as it takes 出口.
Rule: research/localization/WORLD_TYPE.md §3, "EXIT in the Latin and Cyrillic languages"; proof VL503.

  python3 exit_legend_dieface.py [code ...] --out DIR [--tol MM]
      -> DIR/exit_legend_<code>.json for de fr es pt-BR ru ko, and a check line each. --out is required: the shipped
         legends next to this file carry keys later steps add (letterShares, lod1), so a rebuild never writes over them
"""
import hashlib
import json
import math
import os
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
SWIFT = os.path.join(HERE, 'exit_dieface.swift')
SWIFT_KO = os.path.join(HERE, 'exit_dieface_ko.swift')     # 출구 on Noto Sans KR Bold's skeleton, stroke 14.5 mm
WORDS = {'de': 'AUSGANG', 'fr': 'SORTIE', 'es': 'SALIDA', 'pt-BR': 'SAÍDA', 'ru': 'ВЫХОД', 'ko': '출구'}   # 95 §5 table row `EXIT` (ko: the short form, 叙事 11-04 08:52)
FILLET = 0.5            # exit_sign_geom.HOLE_FILLET
CORNER_DEG = 40.0       # a vertex turning more than this is a corner of the die (the letters' corners turn 60 deg or more); less is a facet of an arc
WEB_MIN = 3.0           # WORLD_TYPE ties: at least 3 mm of plate
TOL = 0.25              # arc flattening, mm: 1 px at the hero 4K row (3.9 px/mm); --tol 0.15 for a closer hero


def area(p):
    return 0.5 * sum(p[i][0] * p[(i + 1) % len(p)][1] - p[(i + 1) % len(p)][0] * p[i][1] for i in range(len(p)))


def ccw(p):
    return p if area(p) > 0 else list(reversed(p))


def dedupe(p, eps=1e-4):
    out = []
    for q in p:
        if not out or math.hypot(q[0] - out[-1][0], q[1] - out[-1][1]) > eps:
            out.append(q)
    if len(out) > 1 and math.hypot(out[0][0] - out[-1][0], out[0][1] - out[-1][1]) <= eps:
        out.pop()
    # drop collinear points
    res = []
    n = len(out)
    for i in range(n):
        a, b, c = out[i - 1], out[i], out[(i + 1) % n]
        cr = (b[0] - a[0]) * (c[1] - b[1]) - (b[1] - a[1]) * (c[0] - b[0])
        if abs(cr) > 1e-7 * max(1.0, math.hypot(b[0] - a[0], b[1] - a[1]) * math.hypot(c[0] - b[0], c[1] - b[1])):
            res.append(b)
    return res


def fillet_reflex(poly, r, min_turn_deg=CORNER_DEG, segs=2):
    """Round the reflex corners of a CCW polygon (exit_sign_geom.fillet_reflex), only where the outline turns by more
    than min_turn_deg: the vertices along a flattened arc are not corners of the punch."""
    out = []
    n = len(poly)
    for i in range(n):
        p0, p1, p2 = poly[i - 1], poly[(i + 1) % n], None
        p0, p1, p2 = poly[i - 1], poly[i], poly[(i + 1) % n]
        e0 = (p1[0] - p0[0], p1[1] - p0[1])
        e1 = (p2[0] - p1[0], p2[1] - p1[1])
        cross = e0[0] * e1[1] - e0[1] * e1[0]
        l0, l1 = math.hypot(*e0), math.hypot(*e1)
        u0 = (e0[0] / l0, e0[1] / l0)
        u1 = (e1[0] / l1, e1[1] / l1)
        turn = math.degrees(math.acos(max(-1.0, min(1.0, u0[0] * u1[0] + u0[1] * u1[1]))))
        if cross >= 0 or turn < min_turn_deg:
            out.append(p1)
            continue
        ang = math.acos(max(-1.0, min(1.0, -(u0[0] * u1[0] + u0[1] * u1[1]))))
        t = min(r / math.tan(ang / 2), 0.45 * l0, 0.45 * l1)
        a = (p1[0] - u0[0] * t, p1[1] - u0[1] * t)
        b = (p1[0] + u1[0] * t, p1[1] + u1[1] * t)
        for k in range(segs + 1):
            s = k / segs
            out.append(((1 - s) ** 2 * a[0] + 2 * (1 - s) * s * p1[0] + s * s * b[0],
                        (1 - s) ** 2 * a[1] + 2 * (1 - s) * s * p1[1] + s * s * b[1]))
    return out


def seg_dist(p, q, r, s):
    def pt_seg(x, a, b):
        dx, dy = b[0] - a[0], b[1] - a[1]
        L = dx * dx + dy * dy
        t = 0.0 if L == 0 else max(0.0, min(1.0, ((x[0] - a[0]) * dx + (x[1] - a[1]) * dy) / L))
        return math.hypot(x[0] - a[0] - t * dx, x[1] - a[1] - t * dy)
    return min(pt_seg(p, r, s), pt_seg(q, r, s), pt_seg(r, p, q), pt_seg(s, p, q))


def min_web(polys):
    """Narrowest plate between two holes (mm) and where."""
    best = (1e9, None)
    boxes = [(min(x for x, _ in p), min(y for _, y in p), max(x for x, _ in p), max(y for _, y in p)) for p in polys]
    for i in range(len(polys)):
        for j in range(i + 1, len(polys)):
            bi, bj = boxes[i], boxes[j]
            if bi[0] - bj[2] > best[0] or bj[0] - bi[2] > best[0]:
                continue
            A, B = polys[i], polys[j]
            for a in range(len(A)):
                a0, a1 = A[a], A[(a + 1) % len(A)]
                for b in range(len(B)):
                    b0, b1 = B[b], B[(b + 1) % len(B)]
                    d = seg_dist(a0, a1, b0, b1)
                    if d < best[0]:
                        best = (d, ((a0[0] + b0[0]) / 2, (a0[1] + b0[1]) / 2))
    return best


def run(word, tol=TOL):
    cmd = ['swift', SWIFT_KO, 'fit', '--tol', str(tol)] if word == '출구' else ['swift', SWIFT, 'fit', word, '--tol', str(tol)]
    out = subprocess.run(cmd, capture_output=True, text=True, check=True).stdout
    return json.loads(out)


def build(code, word, tol=TOL):
    d = run(word, tol)
    holes_raw = [ccw(dedupe([tuple(p) for p in l])) for l in d['holes']]
    holes = [fillet_reflex(p, FILLET) for p in holes_raw]
    ink = holes_raw
    x0, y0, x1, y1 = d['inkBox']
    web, where = min_web(holes)
    S = d['strokeMm']
    ties = []
    for t in d['ties']:
        ch = t['note'].split(':')[0]
        ties.append({'char': ch, 'dir': 'up' if t['y1Mm'] > d['baselineMm'] + 0.5 * d['letterHeightMm'] else 'down',
                     'centre_mm': [round(t['xMm'], 3), round((t['y0Mm'] + t['y1Mm']) / 2, 3)],
                     'width_mm': round(d['tieWidthMm'], 3), 'across_mm': round(S, 3), 'note': t['note']})
    chars = []
    for g in d['glyphs']:
        gx0, gx1 = g['xMm'], g['xMm'] + g['widthMm']
        pieces = sum(1 for p in holes_raw if gx0 - 0.01 <= sum(x for x, _ in p) / len(p) <= gx1 + 0.01)
        chars.append({'char': g['char'], 'counters': g['islands'], 'outer': pieces, 'xMm': round(gx0, 3), 'widthMm': round(g['widthMm'], 3)})
    margins = {'left': round(x0, 3), 'right': round(320.0 - x1, 3), 'bottom': round(y0, 3), 'top': round(186.0 - y1, 3)}
    side_air = min(margins['left'], margins['right']) / max(1e-9, min(margins['top'], margins['bottom']))
    ko = word == '출구'
    src = open(SWIFT_KO if ko else SWIFT, 'rb').read()
    leg = {
        'word': word, 'lang': code, 'mode': 'stencil', 'build': 'constructed',
        'face': ('Noto Sans KR Bold skeleton, constructed at 14.5 mm: exit_dieface_ko.swift' if ko else 'EXIT die face (exit_sign_geom E X I T) extended by 平面视觉: exit_dieface.swift'),
        'faceSha1': hashlib.sha1(src).hexdigest(),
        'letterHeightMm': round(d['letterHeightMm'], 3), 'ofExit': round(d['letterHeightMm'] / 152.4, 4),
        'width': round(d['condense'], 3), 'scale': round(d['scale'], 4),
        'inkBox': [round(v, 3) for v in d['inkBox']], 'inkW': round(x1 - x0, 3), 'inkH': round(y1 - y0, 3),
        'baselineMm': round(d['baselineMm'], 3), 'margins': margins,
        'strokes': {'verticalStemMm': round(S, 3), 'horizontalStrokeMm': round(S, 3)},
        'islands': d['islands'], 'ties': ties, 'tieWidthMm': round(d['tieWidthMm'], 3), 'chars': chars,
        'holeVerts': sum(len(p) for p in holes),
        'construction': {
            'method': ('constructed on Noto Sans KR Bold\'s skeleton (centre lines and free ends at em 1000): one stroke of 14.5 mm, square ends, '
                       'ㅅ\'s legs as single arcs landing level, ends aligned with the constructed stems where the face aligns them; no counters, no ties'
                       if ko else 'die face: one stroke S = H/8 square to the stroke, straight strokes cut square, diagonals cut level, '
                       'round strokes on one centre; widths FHWA Series B 2000 x 0.93; no overshoot; counters held by ties'),
            'strokeMm': round(S, 3), 'filletMm': FILLET, 'filletCornerDeg': CORNER_DEG, 'tolMm': tol,
            'minWebMm': round(web, 3), 'minWebAt': [round(where[0], 2), round(where[1], 2)] if where else None,
            'fit': {'floorInkMm': 152.4, 'sideMinMm': 21.0, 'condenseMin': 0.85,
                    'rule': 'ink no taller than 152.4 (top and bottom >= 16.8, EXIT\'s); sides >= 21.0 (1.25 x 16.8); '
                            'the widest skeleton that fits at the largest scale (WORLD_TYPE §3, Latin and Cyrillic EXIT)',
                    'sideAir': round(side_air, 4)},
            'master': d['master'],
        },
        'holes': [[[round(x, 4), round(y, 4)] for x, y in p] for p in holes],
        'ink': [[[round(x, 4), round(y, 4)] for x, y in p] for p in ink],
        'note': (f'출구 (ko), the short form 叙事 signed 11-04 08:52 (95 §5); ink 152.4 mm, EXIT\'s floor; stroke 14.5 mm (the ㄹ\'s six stacked '
                 f'horizontals close at 19.05; the Hong Kong code\'s CJK reading, at least 10 mm); 长体 {100 * d["condense"]:.1f} % for 1.25 x side air'
                 if ko else f'{word} ({code}), the 95 §5 table word for EXIT; letters {d["letterHeightMm"]:.1f} mm = {100 * d["letterHeightMm"] / 152.4:.1f} % of EXIT, '
                 f'stroke {S:.2f} mm; under the life-safety floor because the box stays (95 §6 Q2) and no shorter word exists'),
    }
    return leg


if __name__ == '__main__':
    args, out_dir, tol = sys.argv[1:], None, TOL
    if '--out' in args:
        i = args.index('--out'); out_dir = args[i + 1]; del args[i:i + 2]
    if not out_dir or os.path.abspath(out_dir) == HERE:
        sys.exit('exit_legend_dieface.py: give --out DIR, not this folder (the shipped legends here carry letterShares and lod1)')
    if '--tol' in args:
        i = args.index('--tol'); tol = float(args[i + 1]); del args[i:i + 2]
    codes = args or list(WORDS)
    for code in codes:
        leg = build(code, WORDS[code], tol)
        with open(os.path.join(out_dir, f'exit_legend_{code}.json'), 'w', encoding='utf-8', newline='\n') as f:
            json.dump(leg, f, ensure_ascii=False, indent=1)
        m = leg['margins']
        print(f"{code:6s} {leg['word']:8s} H {leg['letterHeightMm']:6.1f} ({100 * leg['ofExit']:.1f} %) stroke {leg['strokes']['verticalStemMm']:5.2f} "
              f"width {leg['width']:.3f} ink {leg['inkW']:.1f} x {leg['inkH']:.1f} sides {m['left']:.1f}/{m['right']:.1f} top/bottom {m['top']:.1f}/{m['bottom']:.1f} "
              f"holes {len(leg['holes'])} verts {leg['holeVerts']} ties {len(leg['ties'])} @ {leg['tieWidthMm']:.2f} web {leg['construction']['minWebMm']:.2f}")
