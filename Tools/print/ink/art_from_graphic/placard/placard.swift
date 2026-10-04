import Foundation
import CoreGraphics
import CoreText
import ImageIO
import UniformTypeIdentifiers
// EVACUATION PLAN placard, 432 x 279 mm (17 x 11 in landscape). Outputs: print (lit), glow mask, dark preview, lit preview. Units mm, y down.
let W = 432.0, H = 279.0
let projectRoot = ProcessInfo.processInfo.environment["FRONTROOMS_PROJECT_ROOT"] ?? FileManager.default.currentDirectoryPath
let FONT = URL(fileURLWithPath: projectRoot).appendingPathComponent("Assets/Fonts/Period1990/TeXGyre/").path + "/"
func font(_ file: String) -> CTFontDescriptor { (CTFontManagerCreateFontDescriptorsFromURL(URL(fileURLWithPath: FONT + file) as CFURL) as! [CTFontDescriptor])[0] }
let BOLD = font("texgyreheros-bold.otf"), REG = font("texgyreheros-regular.otf")
let capR = CTFontGetCapHeight(CTFontCreateWithFontDescriptor(BOLD, 100, nil)) / 100
enum Ink { case print, red, phos, white }   // phos = glow ink
struct Item { var draw: (CGContext, Ink) -> Void; var ink: Ink }
var items: [Item] = []
func text(_ s: String, _ x: Double, _ base: Double, cap: Double, bold: Bool = true, track: Double = 0.0, ink: Ink = .print, rot: Double = 0, center: Bool = false) -> Double {
  let f = CTFontCreateWithFontDescriptor(bold ? BOLD : REG, cap / capR, nil)
  let line = CTLineCreateWithAttributedString(NSAttributedString(string: s, attributes: [NSAttributedString.Key(kCTFontAttributeName as String): f, NSAttributedString.Key(kCTKernAttributeName as String): track, NSAttributedString.Key("CTForegroundColorFromContext"): true]))
  let w = CTLineGetTypographicBounds(line, nil, nil, nil) - track
  items.append(Item(draw: { c, _ in c.saveGState(); c.translateBy(x: x, y: base); c.rotate(by: rot * .pi / 180); c.scaleBy(x: 1, y: -1); c.textPosition = CGPoint(x: center ? -w / 2 : 0, y: 0); CTLineDraw(line, c); c.restoreGState() }, ink: ink))
  return w }
func rect(_ x: Double, _ y: Double, _ w: Double, _ h: Double, ink: Ink = .print) { items.append(Item(draw: { c, _ in c.fill(CGRect(x: x, y: y, width: w, height: h)) }, ink: ink)) }
func stroke(_ pts: [(Double, Double)], _ lw: Double, ink: Ink = .print, dash: [CGFloat]? = nil, close: Bool = false) { items.append(Item(draw: { c, _ in c.setLineWidth(lw); c.setLineCap(.butt); c.setLineJoin(.miter); if let d = dash { c.setLineDash(phase: 0, lengths: d) } else { c.setLineDash(phase: 0, lengths: []) }
  c.beginPath(); c.move(to: CGPoint(x: pts[0].0, y: pts[0].1)); for p in pts.dropFirst() { c.addLine(to: CGPoint(x: p.0, y: p.1)) }; if close { c.closePath() }; c.strokePath(); c.setLineDash(phase: 0, lengths: []) }, ink: ink)) }
func arc(_ cx: Double, _ cy: Double, _ r: Double, _ a0: Double, _ a1: Double, _ lw: Double, ink: Ink = .print) { items.append(Item(draw: { c, _ in c.setLineWidth(lw); c.beginPath(); c.addArc(center: CGPoint(x: cx, y: cy), radius: r, startAngle: a0 * .pi / 180, endAngle: a1 * .pi / 180, clockwise: false); c.strokePath() }, ink: ink)) }
func dot(_ cx: Double, _ cy: Double, _ r: Double, ink: Ink) { items.append(Item(draw: { c, _ in c.fillEllipse(in: CGRect(x: cx - r, y: cy - r, width: 2 * r, height: 2 * r)) }, ink: ink)) }
func circle(_ cx: Double, _ cy: Double, _ r: Double, _ lw: Double) { items.append(Item(draw: { c, _ in c.setLineWidth(lw); c.strokeEllipse(in: CGRect(x: cx - r, y: cy - r, width: 2 * r, height: 2 * r)) }, ink: .print)) }
func poly(_ pts: [(Double, Double)], ink: Ink) { items.append(Item(draw: { c, _ in c.beginPath(); c.move(to: CGPoint(x: pts[0].0, y: pts[0].1)); for p in pts.dropFirst() { c.addLine(to: CGPoint(x: p.0, y: p.1)) }; c.closePath(); c.fillPath() }, ink: ink)) }
// ---------------- layout
let M = 12.0
stroke([(M, M), (W - M, M), (W - M, H - M), (M, H - M)], 0.7, close: true)          // printed border
_ = text("EVACUATION PLAN", 20, 37, cap: 15, track: 0.6)
// plan: room 9 m x 6 m at 3/16" = 1'-0" (1:64)
let s = 1000.0 / 64.0                    // mm on paper per metre
let rx = 44.0, ry = 82.0, rw = 9 * s, rh = 6 * s, wall = 0.156 * s
let doorW = 1.02 * s, doorY = ry + rh / 2 - doorW / 2
// grid lines (6 m structural grid) + bubbles
for (i, gx) in [rx, rx + 6 * s].enumerated() { stroke([(gx, ry - 18), (gx, ry + rh + 8)], 0.25, dash: [6, 2, 1, 2]); circle(gx, ry - 23, 4.2, 0.35); _ = text(["A", "B"][i], gx, ry - 21.2, cap: 3.6, center: true) }
for (i, gy) in [ry, ry + rh].enumerated() { stroke([(rx - 18, gy), (rx + rw + 6, gy)], 0.25, dash: [6, 2, 1, 2]); circle(rx - 23, gy, 4.2, 0.35); _ = text(["1", "2"][i], rx - 23, gy + 1.8, cap: 3.6, center: true) }
// walls (poche): four sides, door gap in the east wall
rect(rx - wall / 2, ry - wall / 2, rw + wall, wall)                       // north
rect(rx - wall / 2, ry + rh - wall / 2, rw + wall, wall)                  // south
rect(rx - wall / 2, ry - wall / 2, wall, rh + wall)                       // west
rect(rx + rw - wall / 2, ry - wall / 2, wall, doorY - ry + wall / 2)      // east, above door
rect(rx + rw - wall / 2, doorY + doorW, wall, ry + rh + wall / 2 - doorY - doorW) // east, below door
// door leaf swinging out (in the direction of egress) + swing arc
stroke([(rx + rw + wall / 2, doorY), (rx + rw + wall / 2 + doorW, doorY)], 0.5)
arc(rx + rw + wall / 2, doorY, doorW, 0, 90, 0.3)
_ = text("EXIT", rx + rw + wall / 2 + doorW / 2 + 2, doorY + doorW + 7.5, cap: 4.2, center: true)
// match line beyond the door, then the blank sheet area
let mlx = rx + rw + 30
stroke([(mlx, ry - 14), (mlx, ry + rh + 10)], 1.0, dash: [9, 2.5, 2, 2.5])
_ = text("MATCH LINE — SEE SHEET A-3", mlx - 2.2, ry + rh + 6, cap: 3.0, track: 0.3, rot: -90)
// you are here + route (printed red)
let yx = rx + 2.6 * s, yy = ry + 3.6 * s
dot(yx, yy, 3.2, ink: .red)
_ = text("YOU ARE HERE", yx - 4, yy + 11, cap: 4.4, ink: .red)
let dy = doorY + doorW / 2
stroke([(yx + 5, yy), (rx + rw - 9, yy), (rx + rw - 9, dy), (rx + rw - 3.5, dy)], 0.9, ink: .red, dash: [3.5, 2])
poly([(rx + rw - 2, dy), (rx + rw - 6.5, dy - 2.6), (rx + rw - 6.5, dy + 2.6)], ink: .red)
// north arrow + scale (printed)
let nx = rx + 8, ny = ry + rh + 32
circle(nx, ny, 6, 0.35); poly([(nx, ny - 6), (nx - 2.6, ny + 2), (nx, ny), (nx + 2.6, ny + 2)], ink: .print); _ = text("N", nx, ny - 8.5, cap: 3.2, center: true)
_ = text("SCALE: 3/16\" = 1'-0\"", nx + 12, ny + 1.5, cap: 2.8, bold: false, track: 0.2)
// legend (glow ink): head + three rows, InkShape silhouettes at 1/15
let lx = 300.0, k15 = 1.0 / 15.0
_ = text("IN POWER FAILURE", lx, 62, cap: 5.2, track: 0.4, ink: .phos)
// row 1: FLOW chevrons, 400 mm tall, 100 mm stroke, arms 32.5 deg, pitch 375 mm
let ch = 400 * k15, cw = 100 * k15, ang = 32.5 * Double.pi / 180, armX = (ch / 2) / tan(ang)
for i in 0..<2 { let x0 = lx + Double(i) * 375 * k15, y0 = 76.0
  poly([(x0, y0), (x0 + cw / sin(ang) * 0.55, y0), (x0 + armX + cw / sin(ang) * 0.55, y0 + ch / 2), (x0 + cw / sin(ang) * 0.55, y0 + ch), (x0, y0 + ch), (x0 + armX, y0 + ch / 2)], ink: .phos) }
_ = text("WAY ON", lx + 62, 76 + ch / 2 + 2.6, cap: 5.2, track: 0.3, ink: .phos)
// row 2: HERE, two 100 x 900 mm bars beside a door (door outline printed)
let by = 116.0, bw = 100 * k15, bh = 900 * k15, gap = 1020 * k15
rect(lx, by, bw, bh, ink: .phos); rect(lx + bw + gap + 2, by, bw, bh, ink: .phos)
stroke([(lx + bw + 1, by + bh), (lx + bw + 1, by + 3), (lx + bw + gap + 1, by + 3), (lx + bw + gap + 1, by + bh)], 0.3, dash: [1.5, 1.2])
_ = text("EXIT", lx + 2 * bw + gap + 9, by + bh / 2 + 2.6, cap: 5.2, track: 0.3, ink: .phos)
// row 3: STOP, 2 horizontal bars 600 x 120 mm, one per half roll (375 mm)
let sy = 196.0
rect(lx, sy, 600 * k15, 120 * k15, ink: .phos); rect(lx, sy + 375 * k15, 600 * k15, 120 * k15, ink: .phos)
_ = text("NO EXIT", lx + 600 * k15 + 9, sy + 375 * k15 / 2 + 6.5, cap: 5.2, track: 0.3, ink: .phos)
_ = text("SYMBOLS SHOWN AT 1/15 SIZE", lx, 236, cap: 2.4, bold: false, track: 0.2)
// footer band (printed red) + title block (printed)
rect(20, 243, 268, 16, ink: .red)
_ = text("IN CASE OF FIRE DO NOT USE ELEVATORS", 154, 254.2, cap: 5.4, track: 0.5, ink: .white, center: true)
let tb = [("LEVEL 0", 0.0), ("SHEET A-2 OF 4", 0.0), ("PRINTED 03/90", 0.0)]
var cx = 300.0; let ty = 243.0, th = 16.0, cap = 3.6, pad = 3.0, rule = 0.6
var cells: [(Double, Double)] = []
for (t, _) in tb { let f = CTFontCreateWithFontDescriptor(BOLD, cap / capR, nil); let l = CTLineCreateWithAttributedString(NSAttributedString(string: t, attributes: [NSAttributedString.Key(kCTFontAttributeName as String): f, NSAttributedString.Key(kCTKernAttributeName as String): 0.15])); let w = CTLineGetTypographicBounds(l, nil, nil, nil) + 2 * pad; cells.append((cx, w)); cx += w }
stroke([(300, ty), (cx, ty), (cx, ty + th), (300, ty + th)], rule, close: true)
for (i, (x, w)) in cells.enumerated() { if i > 0 { stroke([(x, ty), (x, ty + th)], rule) }; _ = text(tb[i].0, x + pad, ty + th / 2 + cap / 2, cap: cap, track: 0.15); _ = w }
// ---------------- render
func render(_ path: String, pxPerMM: Double, mode: String) {
  let pw = Int(W * pxPerMM), ph = Int(H * pxPerMM)
  let c = CGContext(data: nil, width: pw, height: ph, bitsPerComponent: 8, bytesPerRow: pw * 4, space: CGColorSpaceCreateDeviceRGB(), bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue)!
  c.translateBy(x: 0, y: CGFloat(ph)); c.scaleBy(x: pxPerMM, y: -pxPerMM)
  let paper: (CGFloat, CGFloat, CGFloat) = mode == "dark" ? (0.035, 0.04, 0.035) : (0.957, 0.945, 0.91)
  if mode != "glow" { c.setFillColor(red: paper.0, green: paper.1, blue: paper.2, alpha: 1); c.fill(CGRect(x: 0, y: 0, width: W, height: H)) } else { c.setFillColor(red: 0, green: 0, blue: 0, alpha: 1); c.fill(CGRect(x: 0, y: 0, width: W, height: H)) }
  for it in items {
    var col: (CGFloat, CGFloat, CGFloat, CGFloat)
    switch (mode, it.ink) {
    case ("glow", .phos): col = (1, 1, 1, 1)
    case ("glow", _): continue
    case ("dark", .phos): col = (0.78, 0.97, 0.69, 1)
    case ("dark", .red): col = (0.20, 0.05, 0.05, 1)
    case ("dark", .white): col = (0.16, 0.16, 0.16, 1)
    case (_, .white): col = (1, 1, 1, 1)
    case ("dark", _): col = (0.10, 0.11, 0.10, 1)
    case (_, .phos): col = (0.84, 0.87, 0.74, 1)       // pale phosphor print under light
    case (_, .red): col = (0.78, 0.06, 0.18, 1)
    default: col = (0.09, 0.09, 0.09, 1)
    }
    c.setFillColor(red: col.0, green: col.1, blue: col.2, alpha: col.3); c.setStrokeColor(red: col.0, green: col.1, blue: col.2, alpha: col.3)
    it.draw(c, it.ink) }
  let d = CGImageDestinationCreateWithURL(URL(fileURLWithPath: path) as CFURL, UTType.png.identifier as CFString, 1, nil)!
  CGImageDestinationAddImage(d, c.makeImage()!, nil); CGImageDestinationFinalize(d) }
let out = CommandLine.arguments[1]
render(out + "/placard_print_lit.png", pxPerMM: 6, mode: "lit")
render(out + "/placard_glow_mask.png", pxPerMM: 6, mode: "glow")
render(out + "/placard_dark_preview.png", pxPerMM: 3, mode: "dark")
render(out + "/placard_lit_preview.png", pxPerMM: 3, mode: "lit")
print("items", items.count, "titleblock right edge", cx)
