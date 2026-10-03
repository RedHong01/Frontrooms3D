import Foundation
import CoreGraphics
import ImageIO
import UniformTypeIdentifiers
// Hand-lettered ink art for layers 8 (forged marker) and 9 (scratched cluster). Units: mm, tile 750x750, SVG y down.
struct RNG { var s: UInt64; mutating func next() -> Double { s = s &* 6364136223846793005 &+ 1442695040888963407; return Double((s >> 11) & ((1 << 53) - 1)) / Double(1 << 53) }
  mutating func r(_ a: Double, _ b: Double) -> Double { a + (b - a) * next() }
  mutating func g(_ sd: Double) -> Double { var t = 0.0; for _ in 0..<6 { t += next() }; return (t - 3) * sd / 0.707 } }
typealias P = (Double, Double)
// single-stroke skeletons: unit cap height, y up, baseline 0; each glyph = (advance width, [strokes]); a stroke is a point list; "smooth" strokes get curve-sampled
func ell(_ cx: Double, _ cy: Double, _ rx: Double, _ ry: Double, _ a0: Double, _ a1: Double, _ n: Int = 18) -> [P] { (0...n).map { i in let a = (a0 + (a1 - a0) * Double(i) / Double(n)) * .pi / 180; return (cx + rx * cos(a), cy + ry * sin(a)) } }
let S0: [P] = [(0.50,0.86),(0.40,0.97),(0.26,1.0),(0.11,0.93),(0.06,0.76),(0.14,0.6),(0.30,0.52),(0.46,0.42),(0.54,0.25),(0.47,0.07),(0.30,0.0),(0.14,0.03),(0.03,0.15)]
let G: [Character: (Double, [[P]])] = [
 "A": (0.62, [[(0,0),(0.31,1),(0.62,0)], [(0.13,0.38),(0.49,0.38)]]),
 "B": (0.52, [[(0,0),(0,1)], [(0,1),(0.32,1),(0.47,0.9),(0.47,0.62),(0.32,0.53),(0,0.53)], [(0,0.53),(0.36,0.53),(0.52,0.4),(0.52,0.13),(0.36,0),(0,0)]]),
 "C": (0.56, [ell(0.32, 0.5, 0.31, 0.5, 42, 318)]),
 "D": (0.56, [[(0,0),(0,1),(0.28,1),(0.5,0.82),(0.56,0.5),(0.5,0.18),(0.28,0),(0,0)]]),
 "E": (0.48, [[(0.48,1),(0,1),(0,0),(0.48,0)], [(0,0.52),(0.4,0.52)]]),
 "G": (0.6, [ell(0.32, 0.5, 0.31, 0.5, 42, 335), [(0.36,0.42),(0.62,0.42),(0.62,0.04)]]),
 "H": (0.56, [[(0,0),(0,1)], [(0.56,0),(0.56,1)], [(0,0.52),(0.56,0.52)]]),
 "I": (0.04, [[(0,0),(0,1)]]),
 "K": (0.52, [[(0,0),(0,1)], [(0.52,1),(0,0.38)], [(0.17,0.58),(0.54,0)]]),
 "L": (0.46, [[(0,1),(0,0),(0.46,0)]]),
 "M": (0.7, [[(0,0),(0,1),(0.35,0.28),(0.7,1),(0.7,0)]]),
 "N": (0.58, [[(0,0),(0,1),(0.58,0),(0.58,1)]]),
 "O": (0.64, [ell(0.32, 0.5, 0.32, 0.5, 92, 452, 24)]),
 "R": (0.54, [[(0,0),(0,1),(0.32,1),(0.5,0.9),(0.5,0.62),(0.32,0.5),(0,0.5)], [(0.24,0.5),(0.56,0)]]),
 "S": (0.55, [S0]),
 "Ƨ": (0.55, [S0.map { (0.55 - $0.0, $0.1) }]),
 "T": (0.58, [[(0,1),(0.58,1)], [(0.29,1),(0.29,0)]]),
 "U": (0.56, [[(0,1),(0,0.3),(0.08,0.07),(0.28,0),(0.48,0.07),(0.56,0.3),(0.56,1)]]),
 "W": (0.82, [[(0,1),(0.19,0),(0.41,0.78),(0.63,0),(0.82,1)]]),
 "Y": (0.6, [[(0,1),(0.3,0.5),(0.6,1)], [(0.3,0.5),(0.3,0)]]),
 "0": (0.5, [ell(0.25, 0.5, 0.25, 0.5, 92, 452, 20)]),
 "1": (0.28, [[(0.02,0.8),(0.26,1),(0.26,0)]]),
 "4": (0.56, [[(0.42,0),(0.42,1),(0,0.3),(0.58,0.3)]]),
 "6": (0.5, [[(0.46,0.93),(0.26,1),(0.06,0.78),(0.02,0.38),(0.12,0.08),(0.28,0),(0.46,0.08),(0.5,0.3),(0.42,0.5),(0.24,0.55),(0.06,0.4)]]),
 "9": (0.5, [[(0.04,0.07),(0.24,0),(0.44,0.22),(0.48,0.62),(0.38,0.92),(0.22,1),(0.04,0.92),(0,0.7),(0.08,0.5),(0.26,0.45),(0.44,0.6)]]),
 "/": (0.36, [[(0,-0.06),(0.36,1.06)]]),
 ".": (0.06, [[(0.0,0.0),(0.05,0.03)]]),
 " ": (0.42, []),
]
// Catmull-Rom sampling for smooth (marker) strokes
func smooth(_ p: [P], _ per: Int = 6) -> [P] { if p.count < 3 { return p }; var o: [P] = []
  for i in 0..<(p.count - 1) { let p0 = i > 0 ? p[i - 1] : p[i], p1 = p[i], p2 = p[i + 1], p3 = i + 2 < p.count ? p[i + 2] : p[i + 1]
    for k in 0..<per { let t = Double(k) / Double(per), t2 = t * t, t3 = t2 * t
      func c(_ a: Double, _ b: Double, _ cc: Double, _ d: Double) -> Double { 0.5 * ((2 * b) + (-a + cc) * t + (2 * a - 5 * b + 4 * cc - d) * t2 + (-a + 3 * b - 3 * cc + d) * t3) }
      o.append((c(p0.0, p1.0, p2.0, p3.0), c(p0.1, p1.1, p2.1, p3.1))) } }
  o.append(p.last!); return o }
struct Stroke { var pts: [P]; var w: Double; var dot: Double }
// place a text string; returns strokes in tile mm (y down)
func setText(_ text: String, x0: Double, base: Double, cap: Double, rot: Double, slant: Double, track: Double, rng: inout RNG, hand: String, width: Double, mode: String) -> ([Stroke], Double) {
  var out: [Stroke] = []; var x = 0.0
  let cr = cos(rot * .pi / 180), sr = sin(rot * .pi / 180)
  var drift = 0.0
  for ch in text {
    guard let (adv, strokes) = G[ch] else { x += 0.4 * cap; continue }
    let sc = cap * (1 + rng.g(hand == "forger" ? 0.035 : 0.06))
    let lr = rng.g(hand == "forger" ? 2.0 : 3.0) * .pi / 180
    drift += rng.g(hand == "forger" ? 0.35 : 0.6)
    for st in strokes {
      var pts = st
      if mode == "scratch" { // angular: decimate curves to few straight segments, kink corners
        if pts.count > 9 { let segs = Int(rng.r(7, 10)); let step = max(1, pts.count / segs); pts = stride(from: 0, to: pts.count, by: step).map { pts[$0] } + [pts.last!] }
        pts = pts.map { ($0.0 + rng.g(0.025), $0.1 + rng.g(0.025)) }
      } else { pts = pts.map { ($0.0 + rng.g(0.018), $0.1 + rng.g(0.018)) }; pts = smooth(pts) }
      // letter-local -> line: scale, slant, small rotation, baseline drift
      var line: [P] = pts.map { p in let lx = p.0 * sc + p.1 * sc * tan(slant * .pi / 180), ly = p.1 * sc
        let rx = lx * cos(lr) - ly * sin(lr), ry = lx * sin(lr) + ly * cos(lr); return (x + rx, ry + drift) }
      if mode == "scratch", line.count >= 2, rng.next() < 0.55 { // ragged end: slip past the end
        let a = line[line.count - 2], b = line[line.count - 1]; let dx = b.0 - a.0, dy = b.1 - a.1, L = max(0.01, hypot(dx, dy)); let e = rng.r(0.6, 2.4)
        line.append((b.0 + dx / L * e + rng.g(0.5), b.1 + dy / L * e + rng.g(0.5))) }
      // to tile coords (rotate whole item, y down)
      let tile: [P] = line.map { p in let rx = p.0 * cr - p.1 * sr, ry = p.0 * sr + p.1 * cr; return (x0 + rx, base - ry) }
      let w = width * (1 + rng.g(0.06))
      out.append(Stroke(pts: tile, w: w, dot: mode == "marker" ? w / 2 + rng.r(0.3, 0.8) : 0))
      if mode == "scratch", rng.next() < 0.18, tile.count >= 2 { // double scratch along part of the stroke
        let off = rng.r(0.5, 0.9); let k = max(2, Int(Double(tile.count) * rng.r(0.4, 1.0)))
        out.append(Stroke(pts: tile.prefix(k).map { ($0.0 + off, $0.1 + off * 0.4) }, w: w * 0.7, dot: 0)) }
    }
    x += (adv * sc) + track * cap + rng.g(hand == "forger" ? 0.035 * cap : 0.05 * cap)
  }
  return (out, x)
}
// ---------- layer 8: forged THIƧ WAY OUT, tiling
func layer8() -> [Stroke] {
  var rng = RNG(s: 1990); var all: [Stroke] = []
  var pitches = (0..<20).map { _ in 37.5 + rng.g(2.2) }; let sum = pitches.reduce(0, +); pitches = pitches.map { $0 * 750 / sum }
  var y = rng.r(10, 30)
  for li in 0..<20 {
    let base = y + 22; y += pitches[li]
    let lineSlant = rng.r(3.5, 6.5), width = rng.r(2.8, 3.6)
    // three phrases with uneven gaps; total period 750
    var phrases: [([Stroke], Double)] = []
    for _ in 0..<3 { var r2 = RNG(s: rng.s &+ UInt64(li * 31)); _ = r2.next(); phrases.append(setText("THIƧ WAY OUT", x0: 0, base: 0, cap: 22, rot: 0, slant: lineSlant, track: 0.16, rng: &rng, hand: "forger", width: width, mode: "marker")) }
    let used = phrases.map { $0.1 }.reduce(0, +); let free = 750 - used
    var gaps = (0..<3).map { _ in rng.r(0.75, 1.25) }; let gs = gaps.reduce(0, +); gaps = gaps.map { $0 / gs * free }
    var x = rng.r(0, 750)
    let lineRot = rng.g(0.6)
    for (pi, ph) in phrases.enumerated() {
      for s in ph.0 { var t = s; t.pts = s.pts.map { p in let rx = p.0 * cos(lineRot * .pi / 180) - p.1 * sin(lineRot * .pi / 180), ry = p.0 * sin(lineRot * .pi / 180) + p.1 * cos(lineRot * .pi / 180); return (x + rx, base + ry + 2.0 * sin((x + p.0) / 750 * 2 * .pi + Double(li))) }; all.append(t) }
      x += ph.1 + gaps[pi]
    }
  }
  // seamless: duplicate strokes crossing edges
  var out: [Stroke] = []
  for s0 in all { let shift = floor(s0.pts[0].0 / 750) * 750; let s = Stroke(pts: s0.pts.map { ($0.0 - shift, $0.1) }, w: s0.w, dot: s0.dot)
    for dx in [-750.0, 0, 750] { for dy in [-750.0, 0, 750] { let t = Stroke(pts: s.pts.map { ($0.0 + dx, $0.1 + dy) }, w: s.w, dot: s.dot)
      let xs = t.pts.map { $0.0 }, ys = t.pts.map { $0.1 }
      if xs.max()! >= -5 && xs.min()! <= 755 && ys.max()! >= -5 && ys.min()! <= 755 { out.append(t) } } } }
  return out
}
// ---------- layer 9: scratched cluster (never crosses the tile edge)
func layer9() -> [Stroke] {
  var rng = RNG(s: 1993); var all: [Stroke] = []
  func item(_ t: String, _ x: Double, _ y: Double, _ cap: Double, _ rot: Double, _ slant: Double, _ track: Double, _ w: Double) { let (s, _) = setText(t, x0: x, base: y, cap: cap, rot: rot, slant: slant, track: track, rng: &rng, hand: "scratch", width: w, mode: "scratch"); all += s }
  item("IT ONLY GOES IN", 70, 230, 33, 3.5, -2, 0.22, 2.9)
  item("SAME ROLL AGAIN", 96, 300, 30, -2.5, 9, 0.14, 2.6)
  item("COUNTED 41 DOORS", 78, 372, 29, 1.5, 4, 0.18, 2.8)
  item("R.M. 6/90", 470, 452, 25, -4.5, 0, 0.12, 2.5)
  item("D.K. 11/90", 112, 486, 26, 5.5, 14, 0.2, 2.7)
  // tally: 4 gates + 3, 40 mm tall, near the first line
  var tx = 548.0; let tb = 226.0
  for gate in 0..<5 {
    let n = gate < 4 ? 4 : 3
    for k in 0..<n { let x = tx + Double(k) * 7.5 + rng.g(0.6); all.append(Stroke(pts: [(x + rng.g(0.8), tb - 40 + rng.g(1.5)), (x + rng.g(0.8) + rng.r(-1.5, 1.5), tb + rng.g(1.2))], w: 2.7, dot: 0)) }
    if gate < 4 { all.append(Stroke(pts: [(tx - 4 + rng.g(1), tb - 8 + rng.g(2)), (tx + 26.5 + rng.g(1), tb - 31 + rng.g(2))], w: 2.6, dot: 0)) }
    tx += gate < 4 ? 38 : 0
  }
  return all
}
// ---------- output
func svg(_ strokes: [Stroke], _ path: String) {
  var s = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 750 750\" width=\"750mm\" height=\"750mm\">\n<g fill=\"none\" stroke=\"#000\" stroke-linecap=\"round\" stroke-linejoin=\"round\">\n"
  for st in strokes { guard let f = st.pts.first else { continue }; var d = String(format: "M%.2f %.2f", f.0, f.1); for p in st.pts.dropFirst() { d += String(format: " L%.2f %.2f", p.0, p.1) }; s += "<path d=\"\(d)\" stroke-width=\"\(String(format: "%.2f", st.w))\"/>\n" }
  s += "</g>\n<g fill=\"#000\">\n"; for st in strokes where st.dot > 0 { let f = st.pts[0]; s += String(format: "<circle cx=\"%.2f\" cy=\"%.2f\" r=\"%.2f\"/>\n", f.0, f.1, st.dot) }
  s += "</g>\n</svg>\n"; try! s.write(toFile: path, atomically: true, encoding: .utf8) }
func png(_ strokes: [Stroke], _ path: String, _ px: Int, preview: Bool = false) {
  let k = Double(px) / 750
  let ctx = CGContext(data: nil, width: px, height: px, bitsPerComponent: 8, bytesPerRow: px * 4, space: CGColorSpaceCreateDeviceRGB(), bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue)!
  if preview { ctx.setFillColor(red: 0.05, green: 0.06, blue: 0.05, alpha: 1); ctx.fill(CGRect(x: 0, y: 0, width: px, height: px)) }
  ctx.translateBy(x: 0, y: CGFloat(px)); ctx.scaleBy(x: k, y: -k)
  ctx.setLineCap(.round); ctx.setLineJoin(.round)
  if preview { ctx.setStrokeColor(red: 0.78, green: 0.97, blue: 0.69, alpha: 1); ctx.setFillColor(red: 0.78, green: 0.97, blue: 0.69, alpha: 1) } else { ctx.setStrokeColor(red: 0, green: 0, blue: 0, alpha: 1); ctx.setFillColor(red: 0, green: 0, blue: 0, alpha: 1) }
  for st in strokes { guard let f = st.pts.first else { continue }; ctx.setLineWidth(st.w); ctx.beginPath(); ctx.move(to: CGPoint(x: f.0, y: f.1)); for p in st.pts.dropFirst() { ctx.addLine(to: CGPoint(x: p.0, y: p.1)) }; ctx.strokePath()
    if st.dot > 0 { ctx.fillEllipse(in: CGRect(x: f.0 - st.dot, y: f.1 - st.dot, width: 2 * st.dot, height: 2 * st.dot)) } }
  let d = CGImageDestinationCreateWithURL(URL(fileURLWithPath: path) as CFURL, UTType.png.identifier as CFString, 1, nil)!
  CGImageDestinationAddImage(d, ctx.makeImage()!, nil); CGImageDestinationFinalize(d) }
let outDir = CommandLine.arguments[1]
let l8 = layer8(), l9 = layer9()
svg(l8, outDir + "/layer8_forged_THIS_WAY_OUT.svg"); png(l8, outDir + "/layer8_forged_THIS_WAY_OUT.png", 4096)
svg(l9, outDir + "/layer9_scratch_cluster.svg"); png(l9, outDir + "/layer9_scratch_cluster.png", 4096)
png(l8, outDir + "/preview_layer8.png", 1200, preview: true); png(l9, outDir + "/preview_layer9.png", 1200, preview: true)
// bounds check for layer 9
let xs = l9.flatMap { $0.pts.map { $0.0 } }, ys = l9.flatMap { $0.pts.map { $0.1 } }
print(String(format: "layer8 strokes %d | layer9 strokes %d, x %.0f–%.0f mm, y %.0f–%.0f mm (v %.2f–%.2f)", l8.count, l9.count, xs.min()!, xs.max()!, ys.min()!, ys.max()!, ys.min()! / 750, ys.max()! / 750))
