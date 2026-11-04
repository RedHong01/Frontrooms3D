// EXIT in Korean, 출구, constructed for the stencil plate (平面视觉, 2026-11-04; 叙事 signed 출구 11-04 08:52).
//
// As 出口 (WORLD_TYPE §3 "Stencils"), the legend is drawn on the face's skeleton with one stroke and square ends, the
// punch's R 0.5 in reflex corners (added in exit_legend_dieface.py). The face is Noto Sans KR Bold; its stroke centre
// lines and free ends were measured at em 1000 (11-04, below in em units, y down, advance 920). Two changes from 出口:
//   - the stroke is 14.5 mm, not EXIT's 19.05: 출 stacks six horizontals in 152.4 mm, and at 19.05 the ㄹ's gaps close
//     (3.5 mm); at 14.5 they stand 8.5 mm, 0.59 x the stroke, the face's own proportion (about 0.5), and the sign keeps
//     a weight near 出口's (13.0 was tried and read thin). The height floor holds (ink 152.4 mm); the stroke takes the
//     reading the Hong Kong code gives a CJK sign (strokes at least 10 mm, WORLD_TYPE §3 "Kept boxes").
//   - where the face aligns a free end with a stem's outer edge (ㄹ's left side, the overhang of its last bar), the
//     constructed end follows the constructed edge, so the thinner stroke keeps the face's alignments.
// No counters, so no ties. Fit: ink 152.4 high (top and bottom 16.8), sides at least 21.0 (1.25 x), the skeleton
// narrowed across (长体) to the widest 0.001 step that meets it, never below 0.85.
//
//   swift exit_dieface_ko.swift fit [--tol 0.25]    -> JSON on stdout (opening mm, origin bottom-left, y up)
import CoreGraphics
import Foundation

let OPEN_W: CGFloat = 320.0, OPEN_H: CGFloat = 186.0
let FLOOR_M: CGFloat = 16.8, SIDE_MIN: CGFloat = 21.0, C_MIN: CGFloat = 0.85
let INK_MM: CGFloat = OPEN_H - 2 * FLOOR_M          // 152.4
let STROKE_MM: CGFloat = 14.5
let TOP: CGFloat = 323, BOTTOM: CGFloat = 1248      // the word's ink in em units (출's tick top, 구's stem foot)
let K = INK_MM / (BOTTOM - TOP)                      // mm per em unit
let SU = STROKE_MM / K                               // the stroke in em units
let ADV: CGFloat = 920

func P(_ x: CGFloat, _ y: CGFloat) -> CGPoint { CGPoint(x: x, y: y) }
func poly(_ pts: [CGPoint]) -> CGPath { let p = CGMutablePath(); p.addLines(between: pts); p.closeSubpath(); return p }
func unite(_ ps: [CGPath]) -> CGPath { var a = ps[0]; for q in ps.dropFirst() { a = a.union(q, using: .winding) }; return a }

struct Word { var parts: [CGPath] = []; var cx: CGFloat; var c: CGFloat
    func X(_ x: CGFloat) -> CGFloat { cx + c * (x - cx) }                  // 长体: the skeleton narrows, strokes do not
    mutating func h(_ x0: CGFloat, _ x1: CGFloat, _ yc: CGFloat, joinL: Bool = false, joinR: Bool = false) {
        // a horizontal stroke on centre line yc; a joined end already sits on a stem's outer edge (passed in em)
        let a = joinL ? x0 : X(x0), b = joinR ? x1 : X(x1)
        parts.append(poly([P(a, yc - SU / 2), P(b, yc - SU / 2), P(b, yc + SU / 2), P(a, yc + SU / 2)])) }
    mutating func v(_ xc: CGFloat, _ y0: CGFloat, _ y1: CGFloat) {
        let x = X(xc); parts.append(poly([P(x - SU / 2, y0), P(x + SU / 2, y0), P(x + SU / 2, y1), P(x - SU / 2, y1)])) }
    mutating func arcLeg(_ a: CGPoint, _ b: CGPoint) {
        // ㅅ's leg as the face draws it: one circular arc from a (under the bar, steep) to b (its foot, level), so it
        // leaves the bar at about 51 deg and lands flat; centre straight above b; cut square (a vertical cut at b)
        let dx = abs(a.x - b.x), dy = b.y - a.y
        let r = (dx * dx + dy * dy) / (2 * dy)
        let cxp = b.x, cyp = b.y - r
        let fa = atan2(a.y - cyp, a.x - cxp), fb = CGFloat.pi / 2
        let pts = (0...10).map { i -> CGPoint in
            let f = fa + (fb - fa) * CGFloat(i) / 10
            return P(X(cxp + r * cos(f)), cyp + r * sin(f)) }
        let path = CGMutablePath(); path.addLines(between: pts)
        parts.append(path.copy(strokingWithWidth: SU, lineCap: .butt, lineJoin: .miter, miterLimit: 10)) }
    mutating func slant(_ a: CGPoint, _ b: CGPoint, levelTop: CGFloat? = nil, levelBottom: CGFloat? = nil) {
        // a straight stroke from a to b (centre line, em, y down), square to the stroke; a level cut where given
        let A = P(X(a.x), a.y), B = P(X(b.x), b.y)
        let dx = B.x - A.x, dy = B.y - A.y, L = (dx * dx + dy * dy).squareRoot()
        let nx = -dy / L * SU / 2, ny = dx / L * SU / 2
        if let yt = levelTop, let yb = levelBottom {                      // a near-vertical stroke cut level at both ends
            let kx = dx / dy, hw = SU / 2 * (1 + kx * kx).squareRoot()
            let xt = A.x + kx * (yt - A.y), xb = A.x + kx * (yb - A.y)
            parts.append(poly([P(xt - hw, yt), P(xt + hw, yt), P(xb + hw, yb), P(xb - hw, yb)]))
        } else {
            parts.append(poly([P(A.x + nx, A.y + ny), P(B.x + nx, B.y + ny), P(B.x - nx, B.y - ny), P(A.x - nx, A.y - ny)]))
        } }
}

func build(_ c: CGFloat) -> CGPath {
    var w = Word(cx: (43 + ADV + 879) / 2, c: c)
    // 출 (x as measured)
    let tickX: CGFloat = 458.5, barY: CGFloat = 442.5
    w.v(tickX, TOP, barY + SU / 2)                                         // ㅊ's tick, joined into its bar
    w.h(120, 797, barY)                                                    // ㅊ's bar, free ends
    let legY: CGFloat = 650                                                // ㅅ: arcs from under the bar's middle to feet at
    w.arcLeg(P(tickX, barY + SU / 2), P(110, legY))                        //   the face's leg tips (x 110 and 807), landing
    w.arcLeg(P(tickX, barY + SU / 2), P(2 * tickX - 110, legY))            //   level, cut square
    let uY: CGFloat = 765.5
    w.h(43, 876, uY)                                                       // ㅜ's bar
    let r1: CGFloat = 916, r2: CGFloat = 1053, r3: CGFloat = 1194, rStem: CGFloat = 710.5, lStem: CGFloat = 203
    w.v(459, uY, r1)                                                       // ㅜ's stem, into ㄹ (the face joins them)
    let rEdge = w.X(rStem) + SU / 2, lEdge = w.X(lStem) - SU / 2
    w.h(lEdge, rEdge, r1, joinL: true, joinR: true)                        // ㄹ: bar 1, its free left end on the left stem's edge
    w.v(rStem, r1 - SU / 2, r2 + SU / 2)
    w.h(lEdge, rEdge, r2, joinL: true, joinR: true)
    w.v(lStem, r2 - SU / 2, r3 + SU / 2)
    w.h(lEdge, rEdge + c * (801 - 776), r3, joinL: true, joinR: true)       // bar 3 overhangs the right edge as in the face
    // 구 (x + the advance)
    let o = ADV
    let gY: CGFloat = 428.5, gTop = o + 714.5, gBot = o + 690.5, uY2: CGFloat = 824
    w.slant(P(gTop, gY), P(gBot, uY2), levelTop: gY - SU / 2, levelBottom: uY2)    // ㄱ's down stroke, leaning as the face's
    w.h(o + 137, w.X(gTop), gY, joinR: true)                                // ㄱ's bar, its end inside the leaning down stroke, so
                                                                            //   the corner is the stroke's own (no step)
    w.h(o + 41, o + 879, uY2)                                               // ㅜ's bar
    w.v(o + 455, uY2, BOTTOM)                                               // ㅜ's stem, free foot
    return unite(w.parts)
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
    return out
}
func j(_ v: CGFloat) -> String { String(format: "%.4f", Double(v)) }
func jl(_ l: [CGPoint]) -> String { "[" + l.map { "[\(j($0.x)),\(j($0.y))]" }.joined(separator: ",") + "]" }

let args = CommandLine.arguments
guard args.count >= 2 && args[1] == "fit" else { print("usage: exit_dieface_ko.swift fit [--tol mm]"); exit(2) }
var tol: CGFloat = 0.25
if let i = args.firstIndex(of: "--tol"), i + 1 < args.count { tol = CGFloat(Double(args[i + 1])!) }

var c: CGFloat = 1.0
while c > C_MIN - 1e-9 {
    if build(c).boundingBoxOfPath.width * K <= OPEN_W - 2 * SIDE_MIN + 1e-6 { break }
    c -= 0.001
}
let word = build(c)
let bb = word.boundingBoxOfPath
// em (y down) -> opening mm (y up), the ink box centred
var tf = CGAffineTransform(a: K, b: 0, c: 0, d: -K, tx: OPEN_W / 2 - K * bb.midX, ty: OPEN_H / 2 + K * bb.midY)
let fin = word.copy(using: &tf)!
let box = fin.boundingBoxOfPath
let sylX0 = OPEN_W / 2 - K * bb.midX
var out = "{\n"
out += "\"word\": \"출구\",\n\"condense\": \(j(c)),\n\"scale\": \(j(K)),\n\"letterHeightMm\": \(j(box.height)),\n\"strokeMm\": \(j(STROKE_MM)),\n"
out += "\"tieWidthMm\": 0,\n\"baselineMm\": \(j(box.minY)),\n\"islands\": 0,\n"
out += "\"inkBox\": [\(j(box.minX)),\(j(box.minY)),\(j(box.maxX)),\(j(box.maxY))],\n"
out += "\"master\": {\"emInk\": [\(j(bb.minX)), \(TOP), \(j(bb.maxX)), \(BOTTOM)], \"mmPerEm\": \(j(K)), \"strokeEm\": \(j(SU)), \"face\": \"NotoSansKR-Bold.otf, em 1000, advance 920\"},\n"
out += "\"glyphs\": [{\"char\": \"출\", \"xMm\": \(j(sylX0 + K * (bb.midX + c * (0 - bb.midX)))), \"widthMm\": \(j(K * c * ADV)), \"left\": \"straight\", \"right\": \"straight\", \"islands\": 0}, "
out += "{\"char\": \"구\", \"xMm\": \(j(sylX0 + K * (bb.midX + c * (ADV - bb.midX)))), \"widthMm\": \(j(K * c * ADV)), \"left\": \"straight\", \"right\": \"straight\", \"islands\": 0}],\n"
out += "\"ties\": [],\n"
out += "\"ink\": [" + loops(fin, tol: tol).map(jl).joined(separator: ",\n") + "],\n"
out += "\"holes\": [" + loops(fin, tol: tol).map(jl).joined(separator: ",\n") + "]\n}"
print(out)
