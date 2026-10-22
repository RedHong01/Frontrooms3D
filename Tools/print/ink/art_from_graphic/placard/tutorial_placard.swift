import Foundation
import CoreGraphics
import CoreText
import ImageIO
import UniformTypeIdentifiers
// Tutorial placard options A/B/C (平面视觉, 2026-10-22). Same sheet, faces and render path as
// Tools/print/ink/art_from_graphic/placard/placard.swift: 432 x 279 mm, units mm, y down, 6 px/mm print.
// Strings here are DRAFT placeholders (narrative owns them). Usage: tutorial_placard <outdir> <A|B|C>
let W = 432.0, H = 279.0
let FONT = "/Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01 Individual Game Project/Frontrooms3D/Assets/Fonts/Period1990/TeXGyre/"
func font(_ file: String) -> CTFontDescriptor { (CTFontManagerCreateFontDescriptorsFromURL(URL(fileURLWithPath: FONT + file) as CFURL) as! [CTFontDescriptor])[0] }
let BOLD = font("texgyreheros-bold.otf"), REG = font("texgyreheros-regular.otf"), CNB = font("texgyreheroscn-bold.otf")
func capRatio(_ d: CTFontDescriptor) -> Double { Double(CTFontGetCapHeight(CTFontCreateWithFontDescriptor(d, 100, nil))) / 100 }
let capB = capRatio(BOLD), capR = capRatio(REG), capC = capRatio(CNB)
enum Ink { case print, red, white, tint }
enum Face { case bold, reg, cond }
struct Item { var draw: (CGContext) -> Void; var ink: Ink; var glow: Bool }
var items: [Item] = []
var GLOW = false                       // items added while true also go to the glow mask
struct Callout { var n: Int; var rule: String; var x: Double; var y: Double; var note: String }
var callouts: [Callout] = []
struct TextRec { var s: String; var cap: Double; var x: Double; var base: Double; var w: Double; var face: String }
var texts: [TextRec] = []
// EXAMINE hand-off (research/inspect/10_design.md §3.8): reading-order transcript ('\u{1}' = a glow line) and readable regions
var transcript: [String] = []
func tr(_ s: String, glow: Bool = false) { transcript.append((glow ? "\u{1}" : "") + s) }
struct RegionDef { var id: String; var x: Double; var y: Double; var w: Double; var h: Double }
var regionDefs: [RegionDef] = []
func add(_ ink: Ink, _ d: @escaping (CGContext) -> Void) { items.append(Item(draw: d, ink: ink, glow: GLOW)) }
func call(_ n: Int, _ rule: String, _ x: Double, _ y: Double, _ note: String) { callouts.append(Callout(n: n, rule: rule, x: x, y: y, note: note)) }

@discardableResult
func text(_ s: String, _ x: Double, _ base: Double, cap: Double, face: Face = .bold, track: Double = 0.0, ink: Ink = .print, rot: Double = 0, align: Int = 0) -> Double {
  let d = face == .bold ? BOLD : face == .reg ? REG : CNB, r = face == .bold ? capB : face == .reg ? capR : capC
  let f = CTFontCreateWithFontDescriptor(d, cap / r, nil)
  let line = CTLineCreateWithAttributedString(NSAttributedString(string: s, attributes: [NSAttributedString.Key(kCTFontAttributeName as String): f, NSAttributedString.Key(kCTKernAttributeName as String): track, NSAttributedString.Key("CTForegroundColorFromContext"): true]))
  let w = Double(CTLineGetTypographicBounds(line, nil, nil, nil)) - track
  let ox = align == 0 ? 0 : align == 1 ? -w / 2 : -w
  add(ink) { c in c.saveGState(); c.translateBy(x: x, y: base); c.rotate(by: rot * .pi / 180); c.scaleBy(x: 1, y: -1); c.textPosition = CGPoint(x: ox, y: 0); CTLineDraw(line, c); c.restoreGState() }
  texts.append(TextRec(s: s, cap: cap, x: x + ox, base: base, w: w, face: face == .bold ? "Heros Bold" : face == .reg ? "Heros" : "Heros Cn Bold"))
  return w }
func rect(_ x: Double, _ y: Double, _ w: Double, _ h: Double, ink: Ink = .print) { add(ink) { c in c.fill(CGRect(x: x, y: y, width: w, height: h)) } }
func frameRect(_ x: Double, _ y: Double, _ w: Double, _ h: Double, _ lw: Double, ink: Ink = .print) { add(ink) { c in c.setLineWidth(lw); c.setLineDash(phase: 0, lengths: []); c.stroke(CGRect(x: x, y: y, width: w, height: h)) } }
func rrect(_ x: Double, _ y: Double, _ w: Double, _ h: Double, _ r: Double, lw: Double = 0, ink: Ink = .print) {
  add(ink) { c in let p = CGPath(roundedRect: CGRect(x: x, y: y, width: w, height: h), cornerWidth: r, cornerHeight: r, transform: nil); c.addPath(p); if lw > 0 { c.setLineWidth(lw); c.strokePath() } else { c.fillPath() } } }
func stroke(_ pts: [(Double, Double)], _ lw: Double, ink: Ink = .print, dash: [CGFloat]? = nil, close: Bool = false, round: Bool = false) {
  add(ink) { c in c.setLineWidth(lw); c.setLineCap(round ? .round : .butt); c.setLineJoin(round ? .round : .miter); c.setLineDash(phase: 0, lengths: dash ?? [])
    c.beginPath(); c.move(to: CGPoint(x: pts[0].0, y: pts[0].1)); for p in pts.dropFirst() { c.addLine(to: CGPoint(x: p.0, y: p.1)) }; if close { c.closePath() }; c.strokePath(); c.setLineDash(phase: 0, lengths: []) } }
func arc(_ cx: Double, _ cy: Double, _ r: Double, _ a0: Double, _ a1: Double, _ lw: Double, ink: Ink = .print, dash: [CGFloat]? = nil) {
  add(ink) { c in c.setLineWidth(lw); c.setLineCap(.butt); c.setLineDash(phase: 0, lengths: dash ?? []); c.beginPath(); c.addArc(center: CGPoint(x: cx, y: cy), radius: r, startAngle: a0 * .pi / 180, endAngle: a1 * .pi / 180, clockwise: false); c.strokePath(); c.setLineDash(phase: 0, lengths: []) } }
func dot(_ cx: Double, _ cy: Double, _ r: Double, ink: Ink = .print) { add(ink) { c in c.fillEllipse(in: CGRect(x: cx - r, y: cy - r, width: 2 * r, height: 2 * r)) } }
func ring(_ cx: Double, _ cy: Double, _ r: Double, _ lw: Double, ink: Ink = .print) { add(ink) { c in c.setLineWidth(lw); c.strokeEllipse(in: CGRect(x: cx - r, y: cy - r, width: 2 * r, height: 2 * r)) } }
func ellipse(_ cx: Double, _ cy: Double, _ rx: Double, _ ry: Double, rot: Double = 0, ink: Ink = .print) {
  add(ink) { c in c.saveGState(); c.translateBy(x: cx, y: cy); c.rotate(by: rot * .pi / 180); c.fillEllipse(in: CGRect(x: -rx, y: -ry, width: 2 * rx, height: 2 * ry)); c.restoreGState() } }
func poly(_ pts: [(Double, Double)], ink: Ink = .print) { add(ink) { c in c.beginPath(); c.move(to: CGPoint(x: pts[0].0, y: pts[0].1)); for p in pts.dropFirst() { c.addLine(to: CGPoint(x: p.0, y: p.1)) }; c.closePath(); c.fillPath() } }

// ---------------- pictograms (all sizes mm; stroke >= 0.6 mm, ISO 23601 §6 d; sign height >= 7 mm, §6 c)
// Arrowhead pointing along (dx, dy) with its tip at (x, y).
func head(_ x: Double, _ y: Double, _ dx: Double, _ dy: Double, len: Double = 4.2, half: Double = 2.6, ink: Ink = .red) {
  let l = (dx * dx + dy * dy).squareRoot(), ux = dx / l, uy = dy / l
  poly([(x, y), (x - ux * len - uy * half, y - uy * len + ux * half), (x - ux * len + uy * half, y - uy * len - ux * half)], ink: ink) }
// A route: red solid polyline with an arrowhead at the end (ISO 3864-3 arrow type D spirit, period line weight).
func route(_ pts: [(Double, Double)], lw: Double = 1.4, ink: Ink = .red, dash: [CGFloat]? = nil) {
  let n = pts.count, a = pts[n - 2], b = pts[n - 1], dx = b.0 - a.0, dy = b.1 - a.1, l = (dx * dx + dy * dy).squareRoot()
  var p = pts; p[n - 1] = (b.0 - dx / l * 3.6, b.1 - dy / l * 3.6)
  stroke(p, lw, ink: ink, dash: dash); head(b.0, b.1, dx, dy, ink: ink) }
// YOU ARE HERE: red dot with a printed ring.
func yah(_ x: Double, _ y: Double, r: Double = 3.4) { dot(x, y, r, ink: .red); ring(x, y, r + 1.9, 0.6, ink: .red) }
// Stair in plan (one flight, treads across, arrow DN along the run). Box (x, y, w, h), run downward.
func stairPlan(_ x: Double, _ y: Double, _ w: Double, _ h: Double, treads: Int = 9, dn: Bool = true, label: Bool = true) {
  frameRect(x, y, w, h, 0.6)
  for i in 1..<treads { let ty = y + h * Double(i) / Double(treads); stroke([(x, ty), (x + w, ty)], 0.35) }
  let cx = x + w / 2
  stroke([(cx, y + 2.2), (cx, y + h - 4.6)], 0.8); head(cx, y + h - 1.2, 0, 1, len: 3.6, half: 2.0, ink: .print)
  dot(cx, y + 2.2, 0.9)
  if label { text(dn ? "DN" : "UP", cx + 1.8, y + h * 0.42, cap: 2.8, face: .bold) } }
// Padlock in the HUD padlock's proportions (Lock_rect_*: body 39.3 x 20, shackle half-width 8.5, stroke 2.6), scaled to body width w.
func padlock(_ cx: Double, _ cy: Double, _ w: Double, ink: Ink = .print) {
  let k = w / 39.33, bh = 20.01 * k, top = cy - bh / 2 + 2.0 * k
  rrect(cx - w / 2, cy - bh / 2 + 2.0 * k, w, bh, 3.4 * k, ink: ink)
  let sw = 2.6 * k, hw = 8.5 * k, legTop = top - 4 * k
  add(ink) { c in c.setLineWidth(sw); c.setLineCap(.butt); c.beginPath(); c.move(to: CGPoint(x: cx - hw, y: top + 1 * k)); c.addLine(to: CGPoint(x: cx - hw, y: legTop)); c.addArc(center: CGPoint(x: cx, y: legTop), radius: hw, startAngle: .pi, endAngle: 0, clockwise: false); c.addLine(to: CGPoint(x: cx + hw, y: top + 1 * k)); c.strokePath() }
  // the insert (the crosshair point's place in the HUD lock) knocked out in paper
  add(.white) { c in c.fill(CGRect(x: cx - 1.0 * k * 3, y: cy - 0.2 * k * 3 + 2 * k, width: 2.0 * k * 3, height: 3.4 * k * 3)) } }
// Key B: key + split ring + tag (HUD direction B, Red 10-14), ink silhouette. Box width w, top-left (x, y).
func keyTag(_ x: Double, _ y: Double, _ w: Double, ink: Ink = .print) {
  let k = w / 30.0
  // tag (rect, rim + paper insert)
  rrect(x, y + 11 * k, 22 * k, 13 * k, 2.4 * k, ink: ink)
  rrect(x + 2.6 * k, y + 13.6 * k, 16.8 * k, 7.8 * k, 1.2 * k, ink: .white)
  // tag neck + ring
  rrect(x + 8.6 * k, y + 8.0 * k, 4.8 * k, 4.6 * k, 2.0 * k, ink: ink)
  arc(x + 11 * k, y + 5.6 * k, 4.6 * k, 120, 400, 1.0 * k, ink: ink)
  // key: bow with a hole, blade to the right with teeth
  rrect(x + 13.0 * k, y + 0.2 * k, 8.2 * k, 7.6 * k, 2.4 * k, ink: ink)
  dot(x + 15.4 * k, y + 4.0 * k, 1.1 * k, ink: .white)
  rect(x + 21.0 * k, y + 2.6 * k, 8.6 * k, 2.8 * k, ink: ink)
  poly([(x + 22.6 * k, y + 2.7 * k), (x + 23.6 * k, y + 1.4 * k), (x + 24.6 * k, y + 2.7 * k), (x + 25.6 * k, y + 1.4 * k), (x + 26.6 * k, y + 2.7 * k), (x + 27.6 * k, y + 1.4 * k), (x + 28.6 * k, y + 2.7 * k)], ink: ink) }
// 1990 console CRT TV, front view; optional sound arcs on the right (it is heard through walls).
func tv(_ x: Double, _ y: Double, _ w: Double, sound: Bool = true, ink: Ink = .print) {
  let h = w * 0.78
  rrect(x, y, w, h, w * 0.08, ink: ink)
  rrect(x + w * 0.08, y + h * 0.12, w * 0.62, h * 0.70, w * 0.09, ink: .white)
  dot(x + w * 0.84, y + h * 0.30, w * 0.055, ink: .white); dot(x + w * 0.84, y + h * 0.55, w * 0.055, ink: .white)
  rect(x + w * 0.12, y + h, w * 0.08, w * 0.06, ink: ink); rect(x + w * 0.80, y + h, w * 0.08, w * 0.06, ink: ink)
  if sound { for i in 0..<3 { arc(x + w * 0.98, y + h * 0.47, w * (0.20 + 0.15 * Double(i)), -38, 38, 0.6, ink: ink) } } }
// Ceiling troffer seen from below (2 x 4 lay-in), lamps dipping: short rays and a down tick.
func lampDip(_ x: Double, _ y: Double, _ w: Double, ink: Ink = .print) {
  let h = w * 0.42
  frameRect(x, y, w, h, 0.6, ink: ink)
  for i in 1...3 { let tx = x + w * Double(i) / 4; stroke([(tx, y + 1.0), (tx, y + h - 1.0)], 0.5, ink: ink) }
  rect(x + 0.3, y + h * 0.5, w - 0.6, h * 0.5 - 0.3, ink: .tint)        // the lower half dimmed
  for i in 0..<5 { let rx = x + w * (0.1 + 0.2 * Double(i)); stroke([(rx, y + h + 1.4), (rx, y + h + 3.4)], 0.6, ink: ink, dash: [0.9, 0.7]) } }
// Two shoe prints, walking upward (heard through the wall: shown behind a wall stub).
func steps(_ x: Double, _ y: Double, _ w: Double, ink: Ink = .print) {
  let k = w / 16.0
  func print1(_ px: Double, _ py: Double) { ellipse(px, py, 2.4 * k, 3.9 * k, rot: -8, ink: ink); ellipse(px + 0.6 * k, py + 6.2 * k, 1.9 * k, 2.1 * k, rot: -8, ink: ink) }
  print1(x + 4.0 * k, y + 5.0 * k); print1(x + 10.6 * k, y + 1.4 * k)
  rect(x + 14.6 * k, y - 0.6 * k, 1.2 * k, 14.2 * k, ink: ink) }
// Fire-alarm bell (period round gong, front view) with one strike.
func bell(_ x: Double, _ y: Double, _ w: Double, ink: Ink = .print) {
  let r = w * 0.36, cx = x + w / 2, cy = y + w / 2
  dot(cx, cy, r, ink: ink); ring(cx, cy, r * 0.42, 0.5, ink: .white); dot(cx, cy + r * 0.1, r * 0.16, ink: .white)
  arc(cx, cy, r + w * 0.10, -40, 40, 0.6, ink: ink); arc(cx, cy, r + w * 0.10, 140, 220, 0.6, ink: ink) }
// Plan door: wall gap of width dw on a horizontal wall at y, leaf hinged at (hx, y) swinging toward +y (dir 1) or -y.
func planDoor(_ hx: Double, _ y: Double, _ dw: Double, dir: Double = 1, right: Bool = true, ink: Ink = .print) {
  let sx = right ? 1.0 : -1.0
  stroke([(hx, y), (hx, y + dir * dw)], 0.5, ink: ink)
  let a0 = dir > 0 ? (right ? 0.0 : 90.0) : (right ? 270.0 : 180.0)
  arc(hx, y, dw, a0, a0 + 90, 0.3, ink: ink); _ = sx }
// Poché wall segments (horizontal / vertical), thickness t.
func wallH(_ x0: Double, _ x1: Double, _ y: Double, _ t: Double = 1.8) { rect(min(x0, x1), y - t / 2, abs(x1 - x0), t) }
func wallV(_ x: Double, _ y0: Double, _ y1: Double, _ t: Double = 1.8) { rect(x - t / 2, min(y0, y1), t, abs(y1 - y0)) }
// Numbered marker (printed ink disc, paper numeral) used on both the plan and the legend.
func marker(_ x: Double, _ y: Double, _ n: Int, r: Double = 3.6, ink: Ink = .print) { dot(x, y, r, ink: ink); text("\(n)", x, y + r * 0.52, cap: r * 1.05, face: .bold, ink: .white, align: 1) }

// ---------------- the common sheet (v2, 2026-10-22: 教程设计's contract, 10_proposal.md §5.2)
// Header + floor number, title block; no red footer: the red block is now the four numbered lines.
func sheet(level: String, tb: [String]) {
  frameRect(12, 12, W - 24, H - 24, 0.7)                                         // printed border
  text("EVACUATION PLAN", 20, 37, cap: 15, track: 0.6)                           // header, ISO §6 b: band >= 19.5, caps >= 11.7
  text(level, 412, 37, cap: 15, track: 0.6, align: 2)                             // floor designation = the floor count (L9)
  stroke([(20, 44), (412, 44)], 0.6)
  tr("EVACUATION PLAN · " + level)
  var cx = 300.0; let ty = 243.0, th = 16.0, cap = 3.6, pad = 3.0
  var cells: [(Double, Double)] = []
  for t in tb { let f = CTFontCreateWithFontDescriptor(BOLD, cap / capB, nil); let l = CTLineCreateWithAttributedString(NSAttributedString(string: t, attributes: [NSAttributedString.Key(kCTFontAttributeName as String): f, NSAttributedString.Key(kCTKernAttributeName as String): 0.15])); let w = Double(CTLineGetTypographicBounds(l, nil, nil, nil)) + 2 * pad; cells.append((cx, w)); cx += w }
  let scale = 112.0 / (cx - 300.0)
  var x = 300.0
  stroke([(300, ty), (412, ty), (412, ty + th), (300, ty + th)], 0.6, close: true)
  for (i, (_, w0)) in cells.enumerated() { let w = w0 * scale; if i > 0 { stroke([(x, ty), (x, ty + th)], 0.6) }; text(tb[i], x + w / 2, ty + th / 2 + cap / 2, cap: cap, track: 0.15, align: 1); x += w } }
// The four lines (signed by 叙事 2026-10-22, research/narrative/40_floors_exit_tutorial_strings.md §3): white on safety red, x 20-288, y 214-259. Never RUN or HIDE as advice,
// nothing that says darkness hides you, never "relay" (RELAY §9.4; tutorial 10_proposal §5.2).
var LEVEL = "LEVEL 0"     // the floor on the header and the title block; CLI "level=B1" (C28: LEVEL 0, then B1, B2 …)
let WARDEN = "ON ALARM, A FLOOR WARDEN CHECKS EVERY ROOM."
func alarmBlock(line4: [String] = ["STAIR DOORS ARE LOCKED.", "KEY AT SECURITY DESK."]) {
  rect(20, 208, 268, 51, ink: .red)
  text("IN CASE OF ALARM", 26, 216.4, cap: 4.4, track: 0.5, ink: .white)
  stroke([(26, 219.4), (282, 219.4)], 0.35, ink: .white)
  // Fine print (叙事 v1.1 §3, Red 2026-10-22 12:15 in the 叙事 chat): names a role only; not a numbered line.
  text(WARDEN, 282, 216.4, cap: 3.6, track: 0.2, ink: .white, align: 2)
  func item(_ n: Int, _ x: Double, _ base: Double, _ lines: [String]) {
    text("\(n)", x, base, cap: 5.0, track: 0, ink: .white)
    for (i, l) in lines.enumerated() { text(l, x + 7, base + Double(i) * 7.6, cap: 5.0, track: 0.1, ink: .white) } }
  tr("IN CASE OF ALARM"); tr(WARDEN)
  for (n, l) in [(1, ["WALK, DO NOT RUN."]), (2, ["CLOSE DOORS BEHIND YOU."]), (3, ["IF THE LIGHTS DIM,", "LEAVE THE AREA."]), (4, line4)] { tr("\(n) " + l.joined(separator: " ")) }
  item(1, 26, 228, ["WALK, DO NOT RUN."])
  item(2, 26, 246, ["CLOSE DOORS BEHIND YOU."])
  item(3, 156, 228, ["IF THE LIGHTS DIM,", "LEAVE THE AREA."])
  item(4, 156, 246, line4)
  call(10, "L4", 33, 226, "walk: sprint steps are a 26 m noise")
  call(11, "L5", 33, 244, "shut doors add 9 m to its hearing path")
  call(12, "L6", 163, 226, "W1 lamps dip = it is near (live 10-22)")
  call(13, "L1 L2", 163, 244, "the stair door is locked; key at the station") }
// Desk with a console TV on it and a key tag on a hook: the security desk (player-facing name since 叙事 N1, 10-22; internally the station).
func station(_ x: Double, _ y: Double, _ w: Double, sound: Bool = false, ink: Ink = .print) {
  let k = w / 40.0
  tv(x + 3 * k, y, 16 * k, sound: false, ink: ink)                              // set on the desk, 12.5 k tall
  rect(x, y + 13.6 * k, 40 * k, 2.4 * k, ink: ink)                              // desk top
  rect(x + 1.5 * k, y + 16 * k, 2.2 * k, 9 * k, ink: ink)                       // leg
  rrect(x + 26 * k, y + 16 * k, 12.5 * k, 9 * k, 0.6 * k, ink: ink)             // pedestal
  stroke([(x + 27.5 * k, y + 20.5 * k), (x + 37 * k, y + 20.5 * k)], 0.35 * k, ink: .white)
  dot(x + 30 * k, y + 1.5 * k, 0.8 * k, ink: ink)                               // the hook
  keyTag(x + 23 * k, y + 1.0 * k, 14 * k, ink: ink)
  if sound { for i in 0..<3 { arc(x + 11 * k, y + 6 * k, (14 + 6 * Double(i)) * k, -70, -20, 0.45, ink: ink, dash: [1.4, 1.4]) } } }
// Stair enclosure in plan: walls, the stair door in the top wall SHUT with the padlock, the lit EXIT sign over it (C23), treads + DN.
func stairRoom(_ x: Double, _ y: Double, _ w: Double, _ h: Double, doorX: Double, exitLabel: Bool = true, lock: Bool = true) {
  let dw = 14.0
  wallH(x, doorX - dw / 2, y); wallH(doorX + dw / 2, x + w, y); wallV(x, y, y + h); wallV(x + w, y, y + h); wallH(x, x + w, y + h)
  rect(doorX - dw / 2, y - 0.45, dw, 0.9)                                        // the leaf, shut
  stairPlan(x + 5, y + 7, w - 10, h - 11, treads: 8)
  if lock { padlock(doorX, y, 11) }
  if exitLabel { text("EXIT", doorX + dw / 2 + 2, y - 2.4, cap: 5.0, track: 0.4, ink: .red); tr("EXIT (over the stair door" + (lock ? ", padlocked)" : ")"), glow: GLOW) } }
// Legend (right column x 300-412): the plan's own symbols. 3 rows (A, B); C adds the padlock.
func legend(lock: Bool, phase0: Bool = false) {
  let lx = 300.0, tx = 326.0
  text("LEGEND", lx, 56, cap: 3.6, track: 0.4)
  tr("LEGEND: YOU ARE HERE · STAIR (EXIT), ONE FLOOR DOWN" + (phase0 ? "" : " · SECURITY DESK (KEY)") + (lock ? " · LOCKED DOOR" : ""))
  stroke([(lx, 58.5), (412, 58.5)], 0.35)
  yah(lx + 10, 72); text("YOU ARE HERE", tx, 74.5, cap: 5.0, track: 0.1)
  stairPlan(lx + 4, 86, 12, 22, treads: 6, label: false); if !phase0 { padlock(lx + 10, 86, 8) }
  text("STAIR (EXIT)", tx, 96, cap: 5.0, track: 0.1); text("ONE FLOOR DOWN", tx, 103.6, cap: 5.0, track: 0.1)
  if !phase0 { station(lx, 118, 22)
  text("SECURITY", tx, 125, cap: 5.0, track: 0.1); text("DESK (KEY)", tx, 132.6, cap: 5.0, track: 0.1) }
  call(1, "L0", lx + 10, 72, "where you are")
  call(2, "L1 L9", lx + 10, 96, "the stair is the way on, one floor down")
  if !phase0 { call(3, "L2", lx + 11, 125, "the key is kept at the station (the playing TV)") }
  if lock {
    rect(lx + 9.55, 146, 0.9, 14); wallH(lx + 3, lx + 10, 146, 1.4); wallH(lx + 10, lx + 17, 160, 1.4); padlock(lx + 10, 153, 9)
    text("LOCKED DOOR", tx, 155.5, cap: 5.0, track: 0.1)
    call(4, "L2", lx + 10, 153, "only doors a key opens") } }
// Plan notes in the right column (north points down: the plan is turned to the reader).
func notes(_ lines: [String], y: Double) {
  north(306, y + 6)
  for (i, l) in lines.enumerated() { text(l, 318, y + 4 + Double(i) * 5.5, cap: 2.8, face: .reg, track: 0.2); tr(l) } }
// North arrow (the plan is aligned forward-up, so north points DOWN: the reader faces south, into the start room).
func north(_ x: Double, _ y: Double) {
  ring(x, y, 6, 0.35); poly([(x, y + 6), (x - 2.6, y - 2), (x, y), (x + 2.6, y - 2)]); text("N", x, y + 12.2, cap: 3.2, align: 1) }
// Drafting break on a route: two short slanted ticks (the distance here is not to scale).
func brk(_ x: Double, _ y: Double) { stroke([(x - 3.5, y + 1.2), (x + 3.5, y - 1.2)], 0.6); stroke([(x - 3.5, y + 3.2), (x + 3.5, y + 0.8)], 0.6) }

// ---------------- the start edge, true for every seed (FrontRoomsRoomStream: 2.4 m double door; placard at P1 west = the reader's right)
// Plan aligned to the reader (ISO 23601 §5 j, Levine 1982): up = the wall the placard hangs on = the start door.
func startEdge(doorX: Double, wallY: Double, x0: Double, x1: Double, s: Double) -> (Double, Double) {
  let dw = 2.4 * s, leaf = 1.12 * s
  wallH(x0, doorX - dw / 2, wallY); wallH(doorX + dw / 2, x1, wallY)
  rect(doorX - dw / 2, wallY - 0.45, leaf, 0.9); rect(doorX + dw / 2 - leaf, wallY - 0.45, leaf, 0.9)   // shut: it locked behind you
  stroke([(doorX, wallY - 1.6), (doorX, wallY + 1.6)], 0.35)
  text("NO REENTRY", doorX, wallY - 3.4, cap: 3.6, track: 0.3, align: 1)                    // NYC LL5 1973 §C26-608.5 form (叙事 10-22)
  tr("NO REENTRY (the start door, shut)"); tr("YOU ARE HERE")
  let yx = doorX + 1.733 * s, yy = wallY + 1.1 * s
  let g = GLOW; GLOW = true; yah(yx, yy); GLOW = g
  text("YOU ARE HERE", yx + 7.5, yy + 1.9, cap: 5.0, track: 0.2, ink: .red)
  call(20, "L0", yx, yy, "the dot is where the placard hangs")
  call(21, "L1", doorX, wallY - 3, "the door you came in by is shut: no way back")
  return (yx, yy) }

// =====================================================================================================
// OPTION A · ROUTE DIAGRAM: a typical floor, not to scale: YOU ARE HERE -> the station (key) -> the locked stair door -> down.
func buildA() {
  sheet(level: LEVEL, tb: [LEVEL, "SHEET A-2 OF 4", "PRINTED 03/90"])
  let x0 = 24.0, x1 = 284.0, top = 64.0, bot = 200.0
  let (yx, yy) = startEdge(doorX: 92, wallY: top, x0: x0, x1: x1, s: 10)
  wallV(x0, top, bot); wallV(x1, top, bot); wallH(x0, 220, bot)
  wallH(x0, 54, 100); wallH(70, 150, 100); wallH(190, x1, 100)                  // corridor wall: station door 54-70, arch 150-190
  wallV(110, 100, 140); wallV(110, 156, 170); wallH(x0, 110, 170)               // station room, east door 140-156
  text("SECURITY DESK", 28, 166, cap: 3.6, track: 0.3); tr("SECURITY DESK")
  station(32, 110, 40, sound: true)
  call(22, "L2", 52, 125, "the key hangs at the station, where the TV plays")
  GLOW = true; stairRoom(220, 120, 64, 80, doorX: 250); GLOW = false
  text("STAIR A", 216, 196.5, cap: 3.6, track: 0.3, align: 2); tr("STAIR A")
  call(23, "L1 L2 L9", 250, 160, "the EXIT: a locked stair door, one floor down")
  GLOW = true
  route([(yx, yy + 5.5), (yx, 84), (62, 84), (62, 118)])
  route([(80, 148), (200, 148), (200, 109), (250, 109), (250, 115.5)])
  GLOW = false
  legend(lock: false)
  notes(["TYPICAL FLOOR · NOT TO SCALE", "THIS FLOOR MAY DIFFER FROM PLAN"], y: 176)
  alarmBlock()
  tr(LEVEL + " · SHEET A-2 OF 4 · PRINTED 03/90")
  regionDefs = [RegionDef(id: "plan", x: 20, y: 46, w: 268, h: 158), RegionDef(id: "legend", x: 296, y: 46, w: 118, h: 158),
                RegionDef(id: "alarm", x: 20, y: 208, w: 268, h: 51), RegionDef(id: "titleblock", x: 296, y: 241, w: 118, h: 20)]
}

// =====================================================================================================
// OPTION B · TRUE PLAN: only what every seed guarantees (start wall, shut door, the dot, the 3 m cells), the route
// leaving across the match line, a drafting break, then the stair door at the far end.
func buildB() {
  sheet(level: "LEVEL 0", tb: ["LEVEL 0", "SHEET A-2 OF 4", "PRINTED 03/90"])
  let s = 10.0, doorX = 112.0, top = 76.0
  let x0 = doorX - 7.5 * s, x1 = doorX + 7.5 * s                                 // the 5-cell row MapRootFor guarantees (Standard L0)
  let grid = (0...5).map { x0 + Double($0) * 3 * s }
  for (i, gx) in grid.enumerated() { stroke([(gx, top - 12), (gx, top + 3 * s + 8)], 0.25, dash: [6, 2, 1, 2]); ring(gx, top - 16.5, 3.4, 0.35); text(["A", "B", "C", "D", "E", "F"][i], gx, top - 15.0, cap: 3.0, align: 1) }
  for (i, gy) in [top, top + 3 * s].enumerated() { stroke([(x0 - 8, gy), (x1 + 4, gy)], 0.25, dash: [6, 2, 1, 2]); ring(x0 - 12.5, gy, 3.4, 0.35); text(["1", "2"][i], x0 - 12.5, gy + 1.5, cap: 3.0, align: 1) }
  let (yx, yy) = startEdge(doorX: doorX, wallY: top, x0: x0 - 4, x1: x1 + 4, s: s)
  let ml = top + 3 * s + 12
  stroke([(x0 - 8, ml), (x1 + 4, ml)], 1.0, dash: [9, 2.5, 2, 2.5])
  text("MATCH LINE — SEE SHEET A-3", x0 - 8, ml + 6, cap: 3.0, track: 0.3)
  GLOW = true
  stroke([(yx, yy + 5.5), (yx, ml + 12)], 1.4, ink: .red)
  route([(yx, ml + 19), (yx, ml + 32.5)])
  stairRoom(yx - 22, ml + 38, 44, 42, doorX: yx)
  GLOW = false
  brk(yx, ml + 14.5)
  text("STAIR A", yx - 26, ml + 60, cap: 4.4, track: 0.3, align: 2)
  text("NOT TO SCALE", yx + 6, ml + 17.5, cap: 2.8, face: .reg, track: 0.2)
  call(24, "L1 L2 L9", yx, ml + 62, "the stair door, locked, at the far end of the route")
  legend(lock: false)
  notes(["SCALE: 1/8\" = 1'-0\"", "BEYOND MATCH LINE: NOT TO SCALE"], y: 176)
  alarmBlock()
}

// =====================================================================================================
// OPTION C · SEEDED SHEET: printed from this floor's map at its arrival door (illustration, not seed data).
func buildC() {
  sheet(level: "LEVEL B3", tb: ["LEVEL B3", "SHEET C-2 OF 4", "PRINTED 03/90"])
  let s = 8.4, gx0 = 30.0, gy0 = 66.0, cols = 7, rows = 5, cw = 3 * s
  for i in 0...cols { stroke([(gx0 + Double(i) * cw, gy0 - 4), (gx0 + Double(i) * cw, gy0 + Double(rows) * cw + 2)], 0.2, dash: [6, 2, 1, 2]) }
  for j in 0...rows { stroke([(gx0 - 4, gy0 + Double(j) * cw), (gx0 + Double(cols) * cw + 2, gy0 + Double(j) * cw)], 0.2, dash: [6, 2, 1, 2]) }
  let doorX = gx0 + 3.5 * cw
  let (yx, yy) = startEdge(doorX: doorX, wallY: gy0, x0: gx0, x1: gx0 + Double(cols) * cw, s: s)
  func eH(_ c: Int, _ r: Int, _ len: Int = 1) { wallH(gx0 + Double(c) * cw, gx0 + Double(c + len) * cw, gy0 + Double(r) * cw, 1.4) }
  func eV(_ c: Int, _ r: Int, _ len: Int = 1) { wallV(gx0 + Double(c) * cw, gy0 + Double(r) * cw, gy0 + Double(r + len) * cw, 1.4) }
  wallV(gx0, gy0, gy0 + Double(rows) * cw, 1.8); wallV(gx0 + Double(cols) * cw, gy0, gy0 + Double(rows) * cw, 1.8)
  eH(0, 2, 2); eH(4, 1, 2); eH(1, 4, 2); eH(5, 3, 2); eH(3, 3)
  eV(2, 0, 2); eV(5, 1, 2); eV(3, 2, 2); eV(1, 3); eV(6, 3, 2); eV(4, 4)
  let ldx = gx0 + 5 * cw, ldy = gy0 + 1.5 * cw
  rect(ldx - 1.0, ldy - 5.4, 2.0, 10.8, ink: .white); rect(ldx - 0.45, ldy - 5.4, 0.9, 10.8)
  padlock(ldx, ldy, 10)
  call(25, "L2", ldx, ldy, "a real locked door on this floor (not on Floor 1)")
  station(gx0 + 0.15 * cw, gy0 + 2.45 * cw, 26)
  call(26, "L2", gx0 + 0.6 * cw, gy0 + 2.7 * cw, "the station, drawn when it is inside the window")
  GLOW = true
  route([(yx, yy + 5.5), (yx, gy0 + 1.6 * cw), (gx0 + 6.5 * cw, gy0 + 1.6 * cw), (gx0 + 6.5 * cw, gy0 + Double(rows) * cw + 6)])
  text("STAIR A · EXIT", gx0 + 6.5 * cw - 3, gy0 + Double(rows) * cw + 9, cap: 5.0, track: 0.3, ink: .red, align: 2)
  GLOW = false
  call(27, "L1", gx0 + 6.5 * cw, gy0 + Double(rows) * cw + 4, "the bearing to the stair, set on arrival, never updates")
  legend(lock: true)
  notes(["SHEET C-3 BEYOND THIS EDGE", "PRINTED FOR LEVEL B3"], y: 176)
  alarmBlock()
}

// SHEET A · PHASE 0 (教程设计 10_proposal §5.5): ships before the stair locks. No station, no padlock; line 4 without the key.
func buildA0() {
  sheet(level: LEVEL, tb: [LEVEL, "SHEET A-2 OF 4", "PRINTED 03/90"])
  let x0 = 24.0, x1 = 284.0, top = 64.0, bot = 200.0
  let (yx, yy) = startEdge(doorX: 92, wallY: top, x0: x0, x1: x1, s: 10)
  wallV(x0, top, bot); wallV(x1, top, bot); wallH(x0, 220, bot)
  wallH(x0, 54, 100); wallH(70, 150, 100); wallH(190, x1, 100)
  wallV(110, 100, 140); wallV(110, 156, 170); wallH(x0, 110, 170)
  GLOW = true; stairRoom(220, 120, 64, 80, doorX: 250, lock: false); GLOW = false
  text("STAIR A", 216, 196.5, cap: 3.6, track: 0.3, align: 2); tr("STAIR A")
  call(23, "L1 L9", 250, 160, "the EXIT: a stair door, one floor down")
  GLOW = true
  route([(yx, yy + 5.5), (yx, 84), (170, 84), (170, 109), (250, 109), (250, 115.5)])
  GLOW = false
  legend(lock: false, phase0: true)
  notes(["TYPICAL FLOOR · NOT TO SCALE", "THIS FLOOR MAY DIFFER FROM PLAN"], y: 176)
  alarmBlock(line4: ["EXIT BY STAIR,", "ONE FLOOR DOWN."])
  tr(LEVEL + " · SHEET A-2 OF 4 · PRINTED 03/90")
  regionDefs = [RegionDef(id: "plan", x: 20, y: 46, w: 268, h: 158), RegionDef(id: "legend", x: 296, y: 46, w: 118, h: 158),
                RegionDef(id: "alarm", x: 20, y: 208, w: 268, h: 51), RegionDef(id: "titleblock", x: 296, y: 241, w: 118, h: 20)]
}

// ---------------- render
var MODE = "lit"
func render(_ path: String, pxPerMM: Double, mode: String) {
  MODE = mode
  let pw = Int((W * pxPerMM).rounded()), ph = Int((H * pxPerMM).rounded())
  let c = CGContext(data: nil, width: pw, height: ph, bitsPerComponent: 8, bytesPerRow: pw * 4, space: CGColorSpace(name: CGColorSpace.sRGB)!, bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue)!
  c.interpolationQuality = .high; c.setShouldAntialias(true); c.setAllowsFontSmoothing(false)
  c.translateBy(x: 0, y: CGFloat(ph)); c.scaleBy(x: pxPerMM * Double(pw) / (W * pxPerMM), y: -pxPerMM * Double(ph) / (H * pxPerMM))
  let paper: (CGFloat, CGFloat, CGFloat) = mode == "dark" ? (0.035, 0.04, 0.035) : mode == "glow" ? (0, 0, 0) : (0.957, 0.945, 0.91)
  c.setFillColor(red: paper.0, green: paper.1, blue: paper.2, alpha: 1); c.fill(CGRect(x: 0, y: 0, width: W, height: H))
  for it in items {
    var col: (CGFloat, CGFloat, CGFloat)
    if mode == "glow" { if !it.glow { continue }; col = it.ink == .white ? (0, 0, 0) : (1, 1, 1) }
    else if mode == "dark" {
      if it.glow && it.ink != .white { col = (0.78, 0.97, 0.69) }
      else { switch it.ink { case .white: col = paper; case .red: col = (0.20, 0.05, 0.05); default: col = (0.10, 0.11, 0.10) } } }
    else { switch it.ink { case .white: col = (0.957, 0.945, 0.91); case .red: col = (0.78, 0.06, 0.18); case .tint: col = (0.62, 0.61, 0.58); default: col = (0.09, 0.09, 0.09) } }
    c.setFillColor(red: col.0, green: col.1, blue: col.2, alpha: 1); c.setStrokeColor(red: col.0, green: col.1, blue: col.2, alpha: 1)
    it.draw(c) }
  let d = CGImageDestinationCreateWithURL(URL(fileURLWithPath: path) as CFURL, UTType.png.identifier as CFString, 1, nil)!
  CGImageDestinationAddImage(d, c.makeImage()!, nil); CGImageDestinationFinalize(d) }

let out = CommandLine.arguments[1], opt = CommandLine.arguments[2], handoff = CommandLine.arguments.dropFirst(3).contains("handoff")
for a in CommandLine.arguments.dropFirst(3) where a.hasPrefix("level=") { LEVEL = "LEVEL " + a.dropFirst(6) }
switch opt { case "A": buildA(); case "A0": buildA0(); case "B": buildB(); default: buildC() }
if handoff {
  // the v3 file names (Tools/lookdev/pack_evac_plan.py reads placard_print_lit.png + placard_glow_mask.png at 2592 x 1674)
  render("\(out)/placard_print_lit.png", pxPerMM: 6, mode: "lit")
  render("\(out)/placard_glow_mask.png", pxPerMM: 6, mode: "glow")
  render("\(out)/placard_lit_preview.png", pxPerMM: 3, mode: "lit")
  render("\(out)/placard_dark_preview.png", pxPerMM: 3, mode: "dark")
  var regs: [[String: Any]] = []
  for r in regionDefs {
    let inside = texts.filter { t in let cx = t.x + t.w / 2, cy = t.base - t.cap / 2; return cx >= r.x && cx <= r.x + r.w && cy >= r.y && cy <= r.y + r.h }
    let cap = inside.map { $0.cap }.min() ?? 0
    regs.append(["id": r.id, "rect": [r.x, r.y, r.w, r.h], "capMm": (cap * 100).rounded() / 100, "texts": inside.count])
  }
  let ex: [String: Any] = ["sheet": [W, H], "regions": regs, "transcript": transcript, "status": "strings signed by 叙事 2026-10-22 (research/narrative/40_floors_exit_tutorial_strings.md §3); layout 平面视觉"]
  let d = try! JSONSerialization.data(withJSONObject: ex, options: [.prettyPrinted, .sortedKeys])
  try! d.write(to: URL(fileURLWithPath: "\(out)/examine.json"))
  print("handoff \(opt): regions \(regs.map { "\($0["id"]!) cap \($0["capMm"]!)" }), transcript \(transcript.count) lines")
  exit(0)
}
render("\(out)/opt\(opt)_lit_6pxmm.png", pxPerMM: 6, mode: "lit")
render("\(out)/opt\(opt)_glow_6pxmm.png", pxPerMM: 6, mode: "glow")
render("\(out)/opt\(opt)_lit_2pxmm.png", pxPerMM: 2, mode: "lit")
render("\(out)/opt\(opt)_dark_2pxmm.png", pxPerMM: 2, mode: "dark")
// legibility: desktop reference 1080p. Walk-up distances at FOV 76 deg: px/mm = 1080 / (2 d tan 38) / 1000.
// EXAMINE base (0.36 m, FOV ~61): 1080 / 423.6 = 2.55 px/mm; EXAMINE zoom (0.184 m): 5.0 px/mm.
let t38 = tan(38.0 * .pi / 180)
for (name, d) in [("d300", 3.0), ("d120", 1.2)] { render("\(out)/opt\(opt)_sim_\(name).png", pxPerMM: 1080.0 / (2 * d * t38) / 1000, mode: "lit") }
render("\(out)/opt\(opt)_sim_examine.png", pxPerMM: 1080.0 / 423.6, mode: "lit")
// sidecar: callouts and every text run (cap mm, measured width) for the board and the EXAMINE regions
var json: [String: Any] = ["option": opt, "sheet_mm": [W, H]]
json["callouts"] = callouts.map { ["n": $0.n, "rule": $0.rule, "x": $0.x, "y": $0.y, "note": $0.note] }
json["texts"] = texts.map { ["s": $0.s, "cap": $0.cap, "x": ($0.x * 100).rounded() / 100, "base": $0.base, "w": ($0.w * 100).rounded() / 100, "face": $0.face] }
let data = try! JSONSerialization.data(withJSONObject: json, options: [.prettyPrinted, .sortedKeys])
try! data.write(to: URL(fileURLWithPath: "\(out)/opt\(opt).json"))
let minCap = texts.map { $0.cap }.min() ?? 0
let overflow = texts.filter { $0.x < 14 || $0.x + $0.w > 418 }.map { $0.s }
print("option \(opt): items \(items.count), texts \(texts.count), min cap \(minCap) mm, rule lines >= 5.0: \(texts.filter { $0.cap >= 5.0 }.count), overflow: \(overflow)")
