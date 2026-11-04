// EXIT die face, extended to the Latin and Cyrillic words of the translated EXIT (平面视觉, 2026-11-04).
//
// The English legend is code geometry (Tools/Blender/frontrooms_kit/assets/exit_sign_geom.py): E X I T at H 152.4,
// one stroke S 19.05 (1:8), square ends, sharp convex corners, the punch's R 0.5 in reflex corners (added later, in
// legend.py). This file draws the letters the five other words need in the same grammar, so a sign in de, fr, es,
// pt-BR or ru is the same die family:
//   - uniform stroke S, measured square to the stroke; straight strokes cut square, diagonals cut by the cap line and
//     the baseline (as X); round strokes are arcs of one centre (outer radius - inner radius = S);
//   - widths from FHWA Standard Alphabets Series B 2000 (4 in chart: E .380 H, D G N R S U .430 H, O .460 H,
//     A .540 H), times 0.93, the die face's own E / T ratio to Series B (54.0 / 57.9); no overshoot (engineering
//     lettering, as DIN 1451 and FHWA);
//   - a counter (an island of plate) is held by ties of plate, 0.30 x S wide and at least 3 mm (WORLD_TYPE §3):
//     where a bowl meets its stem (the stencil's D and B), through a bar elsewhere (O top and bottom, A's crossbar).
// Geometry is exact (CoreGraphics boolean operations on lines and arcs), flattened at the end to --tol mm.
//
//   swift exit_dieface.swift fit <WORD> [--tol 0.1]  -> JSON on stdout (opening mm, origin bottom-left, y up)
//   swift exit_dieface.swift metrics                 -> master widths and side classes
// exit_legend_dieface.py runs this and writes exit_legend_<code>.json (the schema of exit_legend.py).
import CoreGraphics
import Foundation

let H: CGFloat = 152.4          // cap height, 6 in
let S: CGFloat = 19.05          // stroke, 3/4 in
let OPEN_W: CGFloat = 320.0, OPEN_H: CGFloat = 186.0
let FLOOR_M: CGFloat = 16.8     // EXIT's top and bottom margins: ink no taller than 152.4
let SIDE_MIN: CGFloat = 21.0    // 1.25 x 16.8 (WORLD_TYPE "Air at the sides"); kept for a word below the floor
let C_MIN: CGFloat = 0.85       // narrowest skeleton (WORLD_TYPE: 长体 down to 85 %)
let TIE_FRAC: CGFloat = 0.30, TIE_MIN: CGFloat = 3.0

func P(_ x: CGFloat, _ y: CGFloat) -> CGPoint { CGPoint(x: x, y: y) }
func rect(_ x0: CGFloat, _ y0: CGFloat, _ x1: CGFloat, _ y1: CGFloat) -> CGPath {
    CGPath(rect: CGRect(x: min(x0, x1), y: min(y0, y1), width: abs(x1 - x0), height: abs(y1 - y0)), transform: nil)
}
func poly(_ pts: [CGPoint]) -> CGPath { let p = CGMutablePath(); p.addLines(between: pts); p.closeSubpath(); return p }
func unite(_ ps: [CGPath]) -> CGPath { var a = ps[0]; for q in ps.dropFirst() { a = a.union(q, using: .winding) }; return a }
func moved(_ p: CGPath, _ dx: CGFloat, _ dy: CGFloat) -> CGPath { var t = CGAffineTransform(translationX: dx, y: dy); return p.copy(using: &t)! }

// Box with rounded corners (radii bl, br, tr, tl; 0 = sharp), counter-clockwise.
func rbox(_ x0: CGFloat, _ y0: CGFloat, _ x1: CGFloat, _ y1: CGFloat, bl: CGFloat = 0, br: CGFloat = 0, tr: CGFloat = 0, tl: CGFloat = 0) -> CGPath {
    let p = CGMutablePath()
    p.move(to: P(x0 + bl, y0))
    p.addLine(to: P(x1 - br, y0))
    if br > 0 { p.addArc(center: P(x1 - br, y0 + br), radius: br, startAngle: -.pi / 2, endAngle: 0, clockwise: false) }
    p.addLine(to: P(x1, y1 - tr))
    if tr > 0 { p.addArc(center: P(x1 - tr, y1 - tr), radius: tr, startAngle: 0, endAngle: .pi / 2, clockwise: false) }
    p.addLine(to: P(x0 + tl, y1))
    if tl > 0 { p.addArc(center: P(x0 + tl, y1 - tl), radius: tl, startAngle: .pi / 2, endAngle: .pi, clockwise: false) }
    p.addLine(to: P(x0, y0 + bl))
    if bl > 0 { p.addArc(center: P(x0 + bl, y0 + bl), radius: bl, startAngle: .pi, endAngle: 1.5 * .pi, clockwise: false) }
    p.closeSubpath()
    return p
}
func stroked(_ p: CGPath) -> CGPath { p.copy(strokingWithWidth: S, lineCap: .butt, lineJoin: .miter, miterLimit: 10) }

// Horizontal width of a bar whose square stroke is S, running dx across over dy up, its ends cut level
// (exit_sign_geom.x_half_width): the run depends on the width, so iterate.
func barWidth(span: CGFloat, over dy: CGFloat, fixed: CGFloat = 0) -> CGFloat {
    var hw = S
    for _ in 0..<20 { hw = S / cos(atan((span - hw - fixed) / dy)) }
    return hw
}

enum Side: String { case straight, round, diagonal, open }
let BEARING: [Side: CGFloat] = [.straight: 6.0, .round: 5.0, .diagonal: 4.75, .open: 3.5]   // S-S = 12.0, the die face's gap
let GAP_MIN: CGFloat = 9.5      // UL 924: letters at least 3/8 in apart

struct Tie { let rect: CGRect; let note: String; var align: CGFloat = 0.5 }   // align 0: the strip starts at x (right of a stem); 1: it ends at x (left of a stem); 0.5: centred
struct Glyph {
    let ch: Character
    let width: CGFloat
    let left: Side, right: Side
    let path: CGPath
    let ties: [Tie]              // in glyph units, before the tie width is known: x centre, y span
    let islands: Int
}

// Master width (c = 1) and side classes. c narrows the skeleton: w = S + c (w0 - S).
let MASTER: [Character: (CGFloat, Side, Side)] = [
    "E": (54.0, .straight, .straight), "X": (60.0, .diagonal, .diagonal), "I": (S, .straight, .straight),
    "T": (54.0, .open, .open), "L": (54.0, .straight, .open), "A": (76.0, .diagonal, .diagonal),
    "U": (61.0, .straight, .straight), "S": (61.0, .round, .round), "G": (61.0, .round, .straight),
    "N": (61.0, .straight, .straight), "O": (65.2, .round, .round), "R": (61.0, .straight, .diagonal),
    "D": (61.0, .straight, .round), "Í": (S, .straight, .straight),
    "В": (61.0, .straight, .round), "Ы": (54.0 + 9.5 + S, .straight, .straight), "Х": (60.0, .diagonal, .diagonal),
    "О": (65.2, .round, .round), "Д": (76.0, .diagonal, .straight),
]
func width(_ ch: Character, _ c: CGFloat) -> CGFloat {
    if ch == "I" || ch == "Í" { return S }
    if ch == "Ы" { return (S + c * (54.0 - S)) + 9.5 + S }
    let w0 = MASTER[ch]!.0
    return S + c * (w0 - S)
}

// Tie placeholders: a vertical strip at x through y0...y1 (its width is set when the scale is known).
func vtie(_ x: CGFloat, _ y0: CGFloat, _ y1: CGFloat, _ note: String, beside: Bool = false, leftOf: Bool = false) -> Tie {
    Tie(rect: CGRect(x: x, y: y0, width: 0, height: y1 - y0), note: note, align: leftOf ? 1 : (beside ? 0 : 0.5))
}

let ACC_GAP: CGFloat = 0.05 * H, ACC_H: CGFloat = 0.11 * H, ACC_ANG: CGFloat = 55.0 * .pi / 180, ACC_T: CGFloat = 0.80 * S
let DESC: CGFloat = 0.11 * H

func glyph(_ ch: Character, _ c: CGFloat) -> Glyph {
    let w = width(ch, c)
    let m = MASTER[ch]!
    switch ch {
    case "I":
        return Glyph(ch: ch, width: w, left: m.1, right: m.2, path: rect(0, 0, S, H), ties: [], islands: 0)
    case "Í":
        let wa = ACC_T / sin(ACC_ANG), run = ACC_H / tan(ACC_ANG)
        let xa0 = S / 2 + 0.10 * S - (wa + run) / 2          // centred on the stem, nudged right (the acute's habit)
        let y0 = H + ACC_GAP, y1 = y0 + ACC_H
        let acc = poly([P(xa0, y0), P(xa0 + wa, y0), P(xa0 + run + wa, y1), P(xa0 + run, y1)])
        return Glyph(ch: ch, width: w, left: m.1, right: m.2, path: unite([rect(0, 0, S, H), acc]), ties: [], islands: 0)
    case "E":
        let mid = w * 48.0 / 54.0
        let m0 = (H - S) / 2, m1 = (H + S) / 2
        return Glyph(ch: ch, width: w, left: m.1, right: m.2,
                     path: unite([rect(0, 0, S, H), rect(0, H - S, w, H), rect(0, 0, w, S), rect(0, m0, mid, m1)]), ties: [], islands: 0)
    case "T":
        return Glyph(ch: ch, width: w, left: m.1, right: m.2,
                     path: unite([rect(0, H - S, w, H), rect((w - S) / 2, 0, (w + S) / 2, H)]), ties: [], islands: 0)
    case "L":
        return Glyph(ch: ch, width: w, left: m.1, right: m.2, path: unite([rect(0, 0, S, H), rect(0, 0, w, S)]), ties: [], islands: 0)
    case "X", "Х":
        let hw = barWidth(span: w, over: H)
        let p1 = poly([P(0, 0), P(hw, 0), P(w, H), P(w - hw, H)])
        let p2 = poly([P(w - hw, 0), P(w, 0), P(hw, H), P(0, H)])
        return Glyph(ch: ch, width: w, left: m.1, right: m.2, path: unite([p1, p2]), ties: [], islands: 0)
    case "A":
        let f = 1.3 * S                                       // flat apex
        let xa = (w - f) / 2
        let k = xa / H
        let hw = S * sqrt(1 + k * k)
        let lleg = poly([P(0, 0), P(hw, 0), P(xa + hw, H), P(xa, H)])
        let rleg = poly([P(w - hw, 0), P(w, 0), P(w - xa, H), P(w - xa - hw, H)])
        let yb = 0.30 * H - S / 2                             // crossbar centred at 0.30 H
        let xin = hw * 0.5 + k * (yb + S)
        let bar = rect(xin, yb, w - xin, yb + S)
        return Glyph(ch: ch, width: w, left: m.1, right: m.2, path: unite([lleg, rleg, bar]),
                     ties: [vtie(w / 2, yb - 0.5, yb + S + 0.5, "A: the crossbar, centre")], islands: 1)
    case "U":
        let R = w / 2
        let p = CGMutablePath()
        p.move(to: P(0, H)); p.addLine(to: P(0, R))
        p.addArc(center: P(R, R), radius: R, startAngle: .pi, endAngle: 2 * .pi, clockwise: false)
        p.addLine(to: P(w, H)); p.addLine(to: P(w - S, H)); p.addLine(to: P(w - S, R))
        p.addArc(center: P(R, R), radius: R - S, startAngle: 0, endAngle: -.pi, clockwise: true)
        p.addLine(to: P(S, H)); p.closeSubpath()
        return Glyph(ch: ch, width: w, left: m.1, right: m.2, path: p, ties: [], islands: 0)
    case "O", "О":
        let R = w / 2
        let outer = rbox(0, 0, w, H, bl: R, br: R, tr: R, tl: R)
        let inner = rbox(S, S, w - S, H - S, bl: R - S, br: R - S, tr: R - S, tl: R - S)
        return Glyph(ch: ch, width: w, left: m.1, right: m.2, path: outer.subtracting(inner, using: .winding),
                     ties: [vtie(w / 2, H - S - 0.5, H + 0.5, "O: top, centre"), vtie(w / 2, -0.5, S + 0.5, "O: bottom, centre")], islands: 1)
    case "D":
        let R = 0.50 * w
        let outer = rbox(0, 0, w, H, br: R, tr: R)
        let inner = rbox(S, S, w - S, H - S, br: R - S, tr: R - S)
        return Glyph(ch: ch, width: w, left: m.1, right: m.2, path: outer.subtracting(inner, using: .winding),
                     ties: [vtie(S, H - S - 0.5, H + 0.5, "D: top bar at the stem", beside: true), vtie(S, -0.5, S + 0.5, "D: bottom bar at the stem", beside: true)], islands: 1)
    case "R":
        let ym = H - 0.56 * H                                 // bowl: top 0.56 H of the letter, middle bar included
        let R = 0.42 * w
        let bowl = rbox(0, ym, w, H, br: R, tr: R).subtracting(rbox(S, ym + S, w - S, H - S, br: R - S, tr: R - S), using: .winding)
        // The leg leaves the middle bar a quarter into the counter (its left edge through (xj, ym)), so the crotch
        // opens cleanly below the bar, and lands on the baseline at the right edge.
        let xj = S + 0.25 * (w - 2 * S)
        var hwl = S
        for _ in 0..<20 { hwl = S * sqrt(1 + pow((w - hwl - xj) / ym, 2)) }
        let k = (w - hwl - xj) / ym
        let xtop = xj - k * S                                 // the left edge carried up to the bar's top
        let leg = poly([P(w - hwl, 0), P(w, 0), P(xtop + hwl, ym + S), P(xtop, ym + S)])
        return Glyph(ch: ch, width: w, left: m.1, right: m.2, path: unite([rect(0, 0, S, H), bowl, leg]),
                     ties: [vtie(S, H - S - 0.5, H + 0.5, "R: top bar at the stem", beside: true)], islands: 1)
    case "В":
        let ym = 0.47 * H - S / 2                             // middle bar centred at 0.47 H: the lower bowl the larger
        let wu = 0.92 * w
        let Ru = 0.42 * wu, Rl = 0.45 * w
        let up = rbox(0, ym, wu, H, br: Ru, tr: Ru).subtracting(rbox(S, ym + S, wu - S, H - S, br: Ru - S, tr: Ru - S), using: .winding)
        let lo = rbox(0, 0, w, ym + S, br: Rl, tr: Rl).subtracting(rbox(S, S, w - S, ym, br: Rl - S, tr: Rl - S), using: .winding)
        return Glyph(ch: ch, width: w, left: m.1, right: m.2, path: unite([rect(0, 0, S, H), up, lo]),
                     ties: [vtie(S, H - S - 0.5, H + 0.5, "В: top bar at the stem", beside: true), vtie(S, -0.5, S + 0.5, "В: bottom bar at the stem", beside: true)], islands: 2)
    case "Ы":
        let wb = S + c * (54.0 - S)
        let ym = 0.52 * H - S                                 // the bowl's middle bar tops out at 0.52 H
        let Rl = 0.45 * wb
        let bowl = rbox(0, 0, wb, ym + S, br: Rl, tr: Rl).subtracting(rbox(S, S, wb - S, ym, br: Rl - S, tr: Rl - S), using: .winding)
        let xi = wb + 9.5
        return Glyph(ch: ch, width: w, left: m.1, right: m.2, path: unite([rect(0, 0, S, H), bowl, rect(xi, 0, xi + S, H)]),
                     ties: [vtie(S, -0.5, S + 0.5, "Ы: the bowl's bottom bar at the stem", beside: true)], islands: 1)
    case "Д":
        let xt = 0.16 * w, xb = 0.06 * w                      // left leg: outer edge from (xb, 0) to (xt, H); a modest lean keeps the counter open at the top
        let k = (xt - xb) / H
        let hw = S * sqrt(1 + k * k)
        let leg = poly([P(xb, 0), P(xb + hw, 0), P(xt + hw, H), P(xt, H)])
        let parts = [rect(0, 0, w, S), rect(0, -DESC, S, 0), rect(w - S, -DESC, w, 0), rect(w - S, 0, w, H), rect(xt, H - S, w, H), leg]
        // Ties in the middle of the counter's top and base: cutting beside the right stem read as "Д" + "I".
        let xc = ((xb + hw + k * (H - S)) + (w - S)) / 2
        let xcb = ((xb + hw + k * S) + (w - S)) / 2
        return Glyph(ch: ch, width: w, left: m.1, right: m.2, path: unite(parts),
                     ties: [vtie(xc, H - S - 0.5, H + 0.5, "Д: top bar, the counter's middle"),
                            vtie(xcb, -0.5, S + 0.5, "Д: base bar, the counter's middle")], islands: 1)
    case "N":
        let hw = barWidth(span: w, over: H)
        let diag = poly([P(0, H), P(hw, H), P(w, 0), P(w - hw, 0)])
        return Glyph(ch: ch, width: w, left: m.1, right: m.2, path: unite([rect(0, 0, S, H), rect(w - S, 0, w, H), diag]), ties: [], islands: 0)
    case "G":
        let r = (w - S) / 2
        let c1 = P(w / 2, H - S / 2 - r), c2 = P(w / 2, S / 2 + r)
        let yt = H - 0.30 * H                                 // upper terminal, cut level at 0.70 H
        let ybar = 0.47 * H - S / 2                           // the bar's centre line: its top at 0.47 H
        let p = CGMutablePath()
        p.move(to: P(w - S / 2, yt)); p.addLine(to: P(w - S / 2, c1.y))
        p.addArc(center: c1, radius: r, startAngle: 0, endAngle: .pi, clockwise: false)
        p.addLine(to: P(S / 2, c2.y))
        p.addArc(center: c2, radius: r, startAngle: .pi, endAngle: 2 * .pi, clockwise: false)
        p.addLine(to: P(w - S / 2, ybar)); p.addLine(to: P(0.50 * w, ybar))          // the spur reaches half the counter
        return Glyph(ch: ch, width: w, left: m.1, right: m.2, path: stroked(p).normalized(using: .winding), ties: [], islands: 0)
    case "S":
        let r = (w - S) / 2
        let c1 = P(w / 2, H - S / 2 - r), c2 = P(w / 2, S / 2 + r)
        let beta = (90.0 - 35.0) * CGFloat.pi / 180          // spine 35 degrees below level
        let D = 2 * r / sin(beta)
        let span = c1.y - c2.y
        let L1 = (span - D) * 0.45, L2 = (span - D) * 0.55     // the upper loop the smaller
        let c1p = P(c1.x, c1.y - L1), c2p = P(c2.x, c2.y + L2)
        let n = P(cos(beta), sin(beta))
        let t1 = P(c1p.x - r * n.x, c1p.y - r * n.y), t2 = P(c2p.x + r * n.x, c2p.y + r * n.y)
        let yt1 = c1.y - 0.13 * H, yt2 = c2.y + 0.14 * H       // terminals, cut level
        let p = CGMutablePath()
        p.move(to: P(w - S / 2, yt1)); p.addLine(to: P(w - S / 2, c1.y))
        p.addArc(center: c1, radius: r, startAngle: 0, endAngle: .pi, clockwise: false)
        p.addLine(to: P(S / 2, c1p.y))
        p.addArc(center: c1p, radius: r, startAngle: .pi, endAngle: .pi + beta, clockwise: false)
        p.addLine(to: t2)
        _ = t1
        p.addArc(center: c2p, radius: r, startAngle: beta, endAngle: 0, clockwise: true)
        p.addLine(to: P(w - S / 2, c2.y))
        p.addArc(center: c2, radius: r, startAngle: 0, endAngle: -.pi, clockwise: true)
        p.addLine(to: P(S / 2, yt2))
        return Glyph(ch: ch, width: w, left: m.1, right: m.2, path: stroked(p).normalized(using: .winding), ties: [], islands: 0)
    default:
        fatalError("no glyph for \(ch)")
    }
}

func gap(_ a: Glyph, _ b: Glyph) -> CGFloat { max(GAP_MIN, BEARING[a.right]! + BEARING[b.left]!) }

struct Word { let path: CGPath; let ties: [Tie]; let islands: Int; let glyphs: [(Glyph, CGFloat)] }
func setWord(_ word: String, _ c: CGFloat) -> Word {
    var x: CGFloat = 0
    var parts: [CGPath] = [], ties: [Tie] = [], islands = 0
    var placed: [(Glyph, CGFloat)] = []
    var prev: Glyph? = nil
    for ch in word {
        let g = glyph(ch, c)
        if let p = prev { x += gap(p, g) }
        parts.append(moved(g.path, x, 0))
        for t in g.ties { ties.append(Tie(rect: t.rect.offsetBy(dx: x, dy: 0), note: t.note, align: t.align)) }
        islands += g.islands
        placed.append((g, x))
        x += g.width
        prev = g
    }
    return Word(path: unite(parts), ties: ties, islands: islands, glyphs: placed)
}

func loops(_ p: CGPath, tol: CGFloat) -> [[CGPoint]] {
    var out: [[CGPoint]] = [], cur: [CGPoint] = []
    p.flattened(threshold: tol).applyWithBlock { el in
        let e = el.pointee
        switch e.type {
        case .moveToPoint: if cur.count > 2 { out.append(cur) }; cur = [e.points[0]]
        case .addLineToPoint: cur.append(e.points[0])
        case .closeSubpath: if cur.count > 2 { out.append(cur) }; cur = []
        default: break
        }
    }
    if cur.count > 2 { out.append(cur) }
    // drop a closing point that repeats the first
    return out.map { l in
        var l = l
        if let f = l.first, let z = l.last, hypot(f.x - z.x, f.y - z.y) < 1e-6 { l.removeLast() }
        return l
    }
}

func j(_ v: CGFloat) -> String { String(format: "%.4f", Double(v)) }
func jl(_ l: [CGPoint]) -> String { "[" + l.map { "[\(j($0.x)),\(j($0.y))]" }.joined(separator: ",") + "]" }

let args = CommandLine.arguments
if args.count >= 2 && args[1] == "metrics" {
    for (ch, m) in MASTER.sorted(by: { String($0.key) < String($1.key) }) {
        print("\(ch)  w0 \(j(m.0))  w(0.85) \(j(width(ch, 0.85)))  left \(m.1.rawValue)  right \(m.2.rawValue)")
    }
    exit(0)
}
guard args.count >= 3 && args[1] == "fit" else { print("usage: exit_dieface.swift fit WORD [--tol mm] [--c x --s y]"); exit(2) }
let word = args[2]
var tol: CGFloat = 0.1
var forceC: CGFloat? = nil, forceS: CGFloat? = nil
var i = 3
while i < args.count {
    if args[i] == "--tol" { tol = CGFloat(Double(args[i + 1])!); i += 2 }
    else if args[i] == "--c" { forceC = CGFloat(Double(args[i + 1])!); i += 2 }
    else if args[i] == "--s" { forceS = CGFloat(Double(args[i + 1])!); i += 2 }
    else { i += 1 }
}

// Fit: ink no taller than 152.4 (top and bottom >= 16.8), sides >= 21.0; the widest skeleton first, then the size.
let inkBoxMaster = { (c: CGFloat) -> CGRect in setWord(word, c).path.boundingBoxOfPath }
let b1 = inkBoxMaster(1.0)
let sH = min(1.0, (OPEN_H - 2 * FLOOR_M) / b1.height)
let bMin = inkBoxMaster(C_MIN)
let sBest = min(sH, (OPEN_W - 2 * SIDE_MIN) / bMin.width)
var cFit: CGFloat = C_MIN
var cc: CGFloat = 1.0
while cc >= C_MIN - 1e-9 {
    let b = inkBoxMaster(cc)
    if b.width * sBest <= OPEN_W - 2 * SIDE_MIN + 1e-6 { cFit = cc; break }
    cc -= 0.001
}
let c = forceC ?? cFit
let s = forceS ?? sBest
let w = setWord(word, c)
let tieW = max(TIE_FRAC * S * s, TIE_MIN)         // at the sign's size
let tieWm = tieW / s                               // in master units
let tiePath = CGMutablePath()
for t in w.ties { tiePath.addRect(CGRect(x: t.rect.minX - t.align * tieWm, y: t.rect.minY, width: tieWm, height: t.rect.height)) }
let tied = w.path.subtracting(tiePath, using: .winding)
let bm = w.path.boundingBoxOfPath
// scale, then centre the ink box in the opening
var tf = CGAffineTransform(a: s, b: 0, c: 0, d: s, tx: OPEN_W / 2 - s * bm.midX, ty: OPEN_H / 2 - s * bm.midY)
let inkFinal = w.path.copy(using: &tf)!
let tiedFinal = tied.copy(using: &tf)!
let box = inkFinal.boundingBoxOfPath
let baseline = OPEN_H / 2 - s * bm.midY
var out = "{\n"
out += "\"word\": \"\(word)\",\n\"condense\": \(j(c)),\n\"scale\": \(j(s)),\n\"letterHeightMm\": \(j(H * s)),\n\"strokeMm\": \(j(S * s)),\n"
out += "\"tieWidthMm\": \(j(tieW)),\n\"baselineMm\": \(j(baseline)),\n\"islands\": \(w.islands),\n"
out += "\"inkBox\": [\(j(box.minX)),\(j(box.minY)),\(j(box.maxX)),\(j(box.maxY))],\n"
out += "\"master\": {\"widthAtC\": \(j(bm.width)), \"heightAtC\": \(j(bm.height)), \"sHeight\": \(j(sH)), \"sWidthAtCmin\": \(j((OPEN_W - 2 * SIDE_MIN) / bMin.width))},\n"
out += "\"glyphs\": [" + w.glyphs.map { (g, x) in "{\"char\": \"\(g.ch)\", \"xMm\": \(j(OPEN_W / 2 - s * bm.midX + s * x)), \"widthMm\": \(j(s * g.width)), \"left\": \"\(g.left.rawValue)\", \"right\": \"\(g.right.rawValue)\", \"islands\": \(g.islands)}" }.joined(separator: ", ") + "],\n"
out += "\"ties\": [" + w.ties.map { t in "{\"note\": \"\(t.note)\", \"xMm\": \(j(OPEN_W / 2 - s * bm.midX + s * (t.rect.minX + (0.5 - t.align) * tieWm))), \"y0Mm\": \(j(baseline + s * t.rect.minY)), \"y1Mm\": \(j(baseline + s * t.rect.maxY))}" }.joined(separator: ", ") + "],\n"
out += "\"ink\": [" + loops(inkFinal, tol: tol).map(jl).joined(separator: ",\n") + "],\n"
out += "\"holes\": [" + loops(tiedFinal, tol: tol).map(jl).joined(separator: ",\n") + "]\n}"
print(out)
