import Foundation
import CoreGraphics
import CoreText
import ImageIO
import UniformTypeIdentifiers

// Small, deterministic storyboard renderer for reviewing UI motion separately
// from the Unity runtime. Coordinates use the 874 x 402 pt Figma phone master.
// This intentionally renders the UI skeleton and motion timing, not gameplay.

let args = CommandLine.arguments
let variant = args.count > 1 ? args[1].lowercased() : "cinematic"
let output = args.count > 2 ? args[2] : "/tmp/frontrooms-motion"
let width = 874
let height = 402
let fps = 30
let duration = 3.6
let frameCount = Int(duration * Double(fps))
let outURL = URL(fileURLWithPath: output, isDirectory: true)
try? FileManager.default.createDirectory(at: outURL, withIntermediateDirectories: true)

let cwdURL = URL(fileURLWithPath: FileManager.default.currentDirectoryPath)
let nestedFonts = cwdURL.appendingPathComponent("Frontrooms3D/Assets/Resources/Fonts")
let repoFonts = cwdURL.appendingPathComponent("Assets/Resources/Fonts")
let fontDirectory = FileManager.default.fileExists(atPath: nestedFonts.path) ? nestedFonts : repoFonts
for filename in ["Bayon-Regular.ttf", "IBMPlexMono-Regular.ttf", "SourceSerif4-Variable.ttf"] {
    let url = fontDirectory.appendingPathComponent(filename) as CFURL
    CTFontManagerRegisterFontsForURL(url, .process, nil)
}

// These are the registered PostScript names, not the filenames or family
// labels. Using the family label for Bayon silently fell back to a system
// sans-serif in the first pass; keeping the names explicit makes the render
// verifiable against the Unity Resources font files.
let bayonFont = "Bayon-Regular"
let monoFont = "IBMPlexMono-Regular"
let serifFont = "SourceSerif4Roman-Regular"

let nestedLogo = cwdURL.appendingPathComponent("Frontrooms3D/Assets/Resources/Brand/FrontRoomsLogo.png")
let repoLogo = cwdURL.appendingPathComponent("Assets/Resources/Brand/FrontRoomsLogo.png")
let logoURL = FileManager.default.fileExists(atPath: nestedLogo.path) ? nestedLogo : repoLogo
let titleLogo: CGImage? = {
    guard let source = CGImageSourceCreateWithURL(logoURL as CFURL, nil) else { return nil }
    return CGImageSourceCreateImageAtIndex(source, 0, nil)
}()

let ink = CGColor(red: 0.039, green: 0.039, blue: 0.039, alpha: 1)
let scene = CGColor(red: 0.078, green: 0.078, blue: 0.078, alpha: 1)
let warm = CGColor(red: 0.957, green: 0.945, blue: 0.910, alpha: 1)
let warmWash = CGColor(red: 0.929, green: 0.922, blue: 0.878, alpha: 1)
let yellow = CGColor(red: 0.956, green: 0.875, blue: 0.231, alpha: 1)
let muted = CGColor(red: 0.741, green: 0.729, blue: 0.690, alpha: 1)

func clamp(_ value: CGFloat, _ lo: CGFloat = 0, _ hi: CGFloat = 1) -> CGFloat {
    min(hi, max(lo, value))
}

func lerp(_ a: CGFloat, _ b: CGFloat, _ t: CGFloat) -> CGFloat { a + (b - a) * t }

func cubicOut(_ t: CGFloat) -> CGFloat {
    let x = 1 - clamp(t)
    return 1 - x * x * x
}

func smooth(_ t: CGFloat) -> CGFloat {
    let x = clamp(t)
    return x * x * (3 - 2 * x)
}

func spring(_ t: CGFloat) -> CGFloat {
    let x = clamp(t)
    if x >= 1 { return 1 }
    return clamp(1 - exp(-8 * x) * cos(11 * x))
}

func progress(_ time: CGFloat, _ start: CGFloat, _ end: CGFloat, _ style: String) -> CGFloat {
    let raw = clamp((time - start) / max(0.001, end - start))
    if style == "tactile" { return spring(raw) }
    if style == "reduced" { return raw }
    return cubicOut(raw)
}

func alpha(_ value: CGFloat, _ opacity: CGFloat) -> CGColor {
    CGColor(red: value, green: value, blue: value, alpha: clamp(opacity))
}

func withAlpha(_ color: CGColor, _ opacity: CGFloat) -> CGColor {
    color.copy(alpha: clamp(opacity)) ?? color
}

func roundedRect(_ ctx: CGContext, _ rect: CGRect, _ radius: CGFloat, _ color: CGColor, _ opacity: CGFloat = 1) {
    ctx.saveGState()
    ctx.setFillColor(withAlpha(color, opacity))
    ctx.addPath(CGPath(roundedRect: rect, cornerWidth: radius, cornerHeight: radius, transform: nil))
    ctx.fillPath()
    ctx.restoreGState()
}

func strokeRoundedRect(_ ctx: CGContext, _ rect: CGRect, _ radius: CGFloat, _ color: CGColor, _ line: CGFloat, _ opacity: CGFloat = 1) {
    ctx.saveGState()
    ctx.setStrokeColor(withAlpha(color, opacity))
    ctx.setLineWidth(line)
    ctx.addPath(CGPath(roundedRect: rect, cornerWidth: radius, cornerHeight: radius, transform: nil))
    ctx.strokePath()
    ctx.restoreGState()
}

func circle(_ ctx: CGContext, _ center: CGPoint, _ radius: CGFloat, _ color: CGColor, _ opacity: CGFloat = 1, _ line: CGFloat = 0) {
    ctx.saveGState()
    ctx.setStrokeColor(withAlpha(color, opacity))
    ctx.setFillColor(withAlpha(color, line > 0 ? 0 : opacity))
    ctx.setLineWidth(line)
    ctx.addEllipse(in: CGRect(x: center.x - radius, y: center.y - radius, width: radius * 2, height: radius * 2))
    if line > 0 { ctx.strokePath() } else { ctx.fillPath() }
    ctx.restoreGState()
}

enum TextAlignment { case left, center, right }

func makeLine(_ value: String, _ fontName: String, _ size: CGFloat, _ color: CGColor, _ opacity: CGFloat) -> (CTLine, CTFont, CGFloat) {
    let fontRef = CTFontCreateWithName(fontName as CFString, size, nil)
    let attributes: [NSAttributedString.Key: Any] = [
        NSAttributedString.Key(kCTFontAttributeName as String): fontRef,
        NSAttributedString.Key(kCTForegroundColorAttributeName as String): withAlpha(color, opacity)
    ]
    let line = CTLineCreateWithAttributedString(NSAttributedString(string: value, attributes: attributes))
    let advance = CGFloat(CTLineGetTypographicBounds(line, nil, nil, nil))
    return (line, fontRef, advance)
}

// Draw a line inside the same kind of top-origin text frame used by Figma.
// The baseline comes from the actual CoreText ascent/descent and the declared
// line height; no guessed `size * 0.82` offset is used.
func textFrame(_ ctx: CGContext, _ value: String, _ rect: CGRect, _ size: CGFloat, _ lineHeight: CGFloat, _ font: String, _ color: CGColor, _ opacity: CGFloat = 1, _ alignment: TextAlignment = .left) {
    let (line, fontRef, advance) = makeLine(value, font, size, color, opacity)
    let ascent = CTFontGetAscent(fontRef)
    let descent = CTFontGetDescent(fontRef)
    let leading = max(0, lineHeight - ascent - descent)
    let baseline = CGFloat(height) - rect.minY - ascent - leading * 0.5
    let x: CGFloat
    switch alignment {
    case .left: x = rect.minX
    case .center: x = rect.midX - advance * 0.5
    case .right: x = rect.maxX - advance
    }
    ctx.saveGState()
    ctx.textPosition = CGPoint(x: x, y: baseline)
    CTLineDraw(line, ctx)
    ctx.restoreGState()
}

func centeredText(_ ctx: CGContext, _ value: String, _ centerX: CGFloat, _ topY: CGFloat, _ size: CGFloat, _ lineHeight: CGFloat, _ font: String, _ color: CGColor, _ opacity: CGFloat = 1) {
    textFrame(ctx, value, CGRect(x: centerX - 300, y: topY, width: 600, height: lineHeight), size, lineHeight, font, color, opacity, .center)
}

func leftText(_ ctx: CGContext, _ value: String, _ x: CGFloat, _ topY: CGFloat, _ size: CGFloat, _ lineHeight: CGFloat, _ font: String, _ color: CGColor, _ opacity: CGFloat = 1) {
    textFrame(ctx, value, CGRect(x: x, y: topY, width: 500, height: lineHeight), size, lineHeight, font, color, opacity, .left)
}

func rightText(_ ctx: CGContext, _ value: String, _ rightX: CGFloat, _ topY: CGFloat, _ size: CGFloat, _ lineHeight: CGFloat, _ font: String, _ color: CGColor, _ opacity: CGFloat = 1) {
    textFrame(ctx, value, CGRect(x: rightX - 320, y: topY, width: 320, height: lineHeight), size, lineHeight, font, color, opacity, .right)
}

func drawImage(_ ctx: CGContext, _ image: CGImage?, _ rect: CGRect, _ opacity: CGFloat = 1, _ scale: CGFloat = 1, _ offsetY: CGFloat = 0) {
    guard let image else { return }
    ctx.saveGState()
    ctx.setAlpha(clamp(opacity))
    ctx.translateBy(x: rect.midX, y: CGFloat(height) - rect.midY + offsetY)
    ctx.scaleBy(x: scale, y: scale)
    // The source PNG is the dark production lockup. The Figma title master
    // uses the light lockup, so use the PNG as an alpha mask and tint it with
    // the same warm white used by the rest of the title screen.
    let drawRect = CGRect(x: -rect.width * 0.5, y: -rect.height * 0.5, width: rect.width, height: rect.height)
    ctx.clip(to: drawRect, mask: image)
    ctx.setFillColor(warm)
    ctx.fill(drawRect)
    ctx.restoreGState()
}

func ring(_ ctx: CGContext, _ center: CGPoint, _ radius: CGFloat, _ color: CGColor, _ opacity: CGFloat, _ fill: CGFloat = 1) {
    ctx.saveGState()
    ctx.setStrokeColor(withAlpha(color, opacity))
    ctx.setLineWidth(1.5)
    let start = -CGFloat.pi / 2
    let end = start + CGFloat.pi * 2 * clamp(fill)
    ctx.addArc(center: center, radius: radius, startAngle: start, endAngle: end, clockwise: false)
    ctx.strokePath()
    ctx.restoreGState()
}

func drawScene(_ ctx: CGContext) {
    ctx.setFillColor(scene)
    ctx.fill(CGRect(x: 0, y: 0, width: width, height: height))
    // A restrained room silhouette keeps the motion readable while matching
    // the dark, low-contrast game scene behind the Figma HUD.
    ctx.saveGState()
    ctx.setStrokeColor(CGColor(red: 0.18, green: 0.18, blue: 0.17, alpha: 0.55))
    ctx.setLineWidth(1)
    for i in stride(from: 0, through: 4, by: 1) {
        let x = CGFloat(94 + i * 178)
        ctx.move(to: CGPoint(x: x, y: 0)); ctx.addLine(to: CGPoint(x: 360 + (x - 437) * 0.2, y: 300))
    }
    ctx.move(to: CGPoint(x: 0, y: 50)); ctx.addLine(to: CGPoint(x: 874, y: 86))
    ctx.move(to: CGPoint(x: 0, y: 326)); ctx.addLine(to: CGPoint(x: 874, y: 296))
    ctx.strokePath()
    ctx.restoreGState()
}

func drawCrosshair(_ ctx: CGContext, _ opacity: CGFloat) {
    ctx.saveGState()
    ctx.setStrokeColor(withAlpha(warm, opacity * 0.9))
    ctx.setLineWidth(1)
    ctx.move(to: CGPoint(x: 425 - 12, y: CGFloat(height) - 189)); ctx.addLine(to: CGPoint(x: 425 - 3, y: CGFloat(height) - 189))
    ctx.move(to: CGPoint(x: 425 + 3, y: CGFloat(height) - 189)); ctx.addLine(to: CGPoint(x: 425 + 12, y: CGFloat(height) - 189))
    ctx.move(to: CGPoint(x: 425, y: CGFloat(height) - 189 - 12)); ctx.addLine(to: CGPoint(x: 425, y: CGFloat(height) - 189 - 3))
    ctx.move(to: CGPoint(x: 425, y: CGFloat(height) - 189 + 3)); ctx.addLine(to: CGPoint(x: 425, y: CGFloat(height) - 189 + 12))
    ctx.strokePath()
    ctx.restoreGState()
}

func drawHud(_ ctx: CGContext, _ time: CGFloat, _ style: String) {
    let playAlpha = style == "reduced" ? 1 : clamp(time / 0.32)
    leftText(ctx, "CALM", 91, 16, 26, 26, serifFont, warm, playAlpha)
    leftText(ctx, "ZONE 01", 91, 45, 11, 14, monoFont, muted, playAlpha * 0.9)
    drawCrosshair(ctx, playAlpha)

    let stickIn = progress(time, 0.28, style == "reduced" ? 0.40 : 0.52, style)
    let stickAlpha = clamp(stickIn)
    let stickCenter = CGPoint(x: 150, y: CGFloat(height) - 258)
    ring(ctx, stickCenter, 84 * lerp(0.96, 1, stickIn), warm, 0.42 * stickAlpha)
    circle(ctx, CGPoint(x: 150, y: CGFloat(height) - 258), 26 * lerp(0.82, 1, stickIn), warm, 0.46 * stickAlpha, 1)
    let knob = CGPoint(x: 150 + 11 * sin(time * 2.3), y: CGFloat(height) - (258 + 7 * cos(time * 2.3)))
    circle(ctx, knob, 5, warm, 0.52 * stickAlpha)

    let promptIn = progress(time, 0.56, style == "reduced" ? 0.68 : 0.78, style)
    let promptOut = clamp((2.15 - time) / 0.25)
    let promptAlpha = min(promptIn, promptOut)
    roundedRect(ctx, CGRect(x: 211, y: CGFloat(height) - 371, width: 452, height: 60), 8, ink, 0.22 * promptAlpha)
    leftText(ctx, "THE DOOR IS LOCKED", 250, 338, 20, 22, serifFont, warm, promptAlpha)
    leftText(ctx, "FIND A WAY THROUGH", 250, 362, 11, 14, monoFont, muted, promptAlpha)

    let usePress = (time > 0.98 && time < 1.30) ? (1 - abs(time - 1.14) / 0.16) : 0
    let useCenter = CGPoint(x: 740, y: CGFloat(height) - 246)
    let useScale = style == "reduced" ? 1 : lerp(1, 0.92, clamp(usePress))
    circle(ctx, useCenter, 36 * useScale, usePress > 0.01 ? yellow : warm, 0.76 * playAlpha, 1)
    centeredText(ctx, usePress > 0.01 ? "HOLD" : "USE", 740, 230, 17, 17, bayonFont, usePress > 0.01 ? ink : warm, playAlpha)
    if usePress > 0.01 {
        ring(ctx, useCenter, 46 * useScale, yellow, 0.92 * playAlpha, clamp(usePress * 1.4))
    }

    let sprint = controlsSocket(time, style)
    if sprint > 0 {
        let socket = CGPoint(x: 150, y: CGFloat(height) - 166)
        ring(ctx, socket, 20 * lerp(1, 1.08, sprint), yellow, 0.8 * sprint)
        centeredText(ctx, "SPRINT", socket.x, 144, 17, 17, bayonFont, yellow, sprint)
    }

    // Pause icon follows the 44 pt hit target while keeping the visual glyph
    // compact at the Figma x=818/y=34 anchor.
    let pauseAlpha = clamp((time - 1.42) / 0.22)
    ctx.saveGState()
    ctx.setStrokeColor(withAlpha(warm, 0.78 * pauseAlpha))
    ctx.setLineWidth(2)
    ctx.move(to: CGPoint(x: 815, y: CGFloat(height) - 29)); ctx.addLine(to: CGPoint(x: 815, y: CGFloat(height) - 39))
    ctx.move(to: CGPoint(x: 821, y: CGFloat(height) - 29)); ctx.addLine(to: CGPoint(x: 821, y: CGFloat(height) - 39))
    ctx.strokePath(); ctx.restoreGState()
}

func controlsSocket(_ time: CGFloat, _ style: String) -> CGFloat {
    if time < 0.72 || time > 1.55 { return 0 }
    let p = clamp((time - 0.72) / 0.2)
    let pulse = style == "reduced" ? 1 : lerp(0.72, 1, p)
    return pulse * clamp((1.58 - time) / 0.18)
}

func chip(_ ctx: CGContext, _ label: String, _ centerX: CGFloat, _ top: CGFloat, _ width: CGFloat, _ opacity: CGFloat, _ offset: CGFloat = 0, _ scale: CGFloat = 1) {
    let h: CGFloat = 40 * scale
    let w = width * scale
    roundedRect(ctx, CGRect(x: centerX - w / 2, y: CGFloat(height) - top - h + offset, width: w, height: h), 7, yellow, opacity)
    let labelTop = top + offset + (h - 17 * scale) * 0.5
    centeredText(ctx, label, centerX, labelTop, 17 * scale, 17 * scale, bayonFont, ink, opacity * 0.95)
}

func drawPause(_ ctx: CGContext, _ time: CGFloat, _ style: String) {
    let p = progress(time, 1.46, style == "reduced" ? 1.58 : 1.72, style)
    let wash = p * 0.96
    ctx.setFillColor(withAlpha(warmWash, wash))
    ctx.fill(CGRect(x: 0, y: 0, width: width, height: height))
    let menuOffset = style == "reduced" ? 0 : lerp(-18, 0, p)
    let menuScale = style == "reduced" ? 1 : lerp(0.94, 1, p)
    centeredText(ctx, "PAUSED", 437, 52 + menuOffset, 48 * menuScale, 44 * menuScale, bayonFont, ink, p)
    chip(ctx, "RESUME", 340.5, 270, 75, p, menuOffset, menuScale)
    chip(ctx, "SETTINGS", 435.5, 270, 83, p, menuOffset, menuScale)
    chip(ctx, "RESTART", 532.5, 270, 79, p, menuOffset, menuScale)
    let rows = ["MOVE", "DRAG · LOOK", "SOCKET · SPRINT", "USE · DOOR", "HOLD USE · BREAK GLASS"]
    let rowAlpha = p * 0.78
    textFrame(ctx, rows[0], CGRect(x: 257, y: 150 + menuOffset, width: 60, height: 14), 11, 14, monoFont, ink, rowAlpha, .left)
    textFrame(ctx, rows[1], CGRect(x: 343, y: 150 + menuOffset, width: 112, height: 14), 11, 14, monoFont, ink, rowAlpha, .left)
    textFrame(ctx, rows[2], CGRect(x: 481, y: 150 + menuOffset, width: 137, height: 14), 11, 14, monoFont, ink, rowAlpha, .left)
    textFrame(ctx, rows[3], CGRect(x: 278, y: 186 + menuOffset, width: 105, height: 14), 11, 14, monoFont, ink, rowAlpha, .left)
    textFrame(ctx, rows[4], CGRect(x: 409, y: 186 + menuOffset, width: 187, height: 14), 11, 14, monoFont, ink, rowAlpha, .left)
}

func drawSettings(_ ctx: CGContext, _ time: CGFloat, _ style: String) {
    let p = progress(time, 2.18, style == "reduced" ? 2.30 : 2.52, style)
    let fade = p * 0.98
    roundedRect(ctx, CGRect(x: 66, y: CGFloat(height) - 379, width: 742, height: 369), 10, ink, fade)
    leftText(ctx, "SETTINGS", 90, 30, 17, 17, bayonFont, warm, fade)
    chip(ctx, "CLOSE", 738, 18, 65, fade, 0, 1)
    leftText(ctx, "DISPLAY  +  COMFORT", 90, 62, 11, 14, monoFont, muted, fade)
    leftText(ctx, "TOUCH", 447, 62, 11, 14, monoFont, muted, fade)
    let leftRows: [(String, String, CGFloat, Bool)] = [
        ("HDR RENDER", "ON", 80, false),
        ("CAMERA MOTION", "‹ 100% ›", 124, false),
        ("REDUCE FLASHING", "OFF", 168, false),
        ("BREAK GLASS", "HOLD USE", 242, false),
        ("CAPTIONS", "OFF", 286, false),
        ("RELAY READOUT", "OFF", 330, false)
    ]
    let rightRows: [(String, String, CGFloat, Bool)] = [
        ("LOOK SPEED", "‹    5    ›", 80, true),
        ("INVERT LOOK", "OFF", 124, false),
        ("GYRO LOOK", "OFF", 168, false),
        ("STICK", "FLOATING", 212, false),
        ("SPRINT", "SOCKET", 256, false),
        ("CONTROLS SIZE", "‹ 100% ›", 300, false),
        ("CONTROLS OPACITY", "‹ 60% ›", 344, false)
    ]
    leftText(ctx, "INPUT  +  ASSIST", 90, 220, 11, 14, monoFont, muted, fade)
    func settingRow(_ row: (String, String, CGFloat, Bool), _ x: CGFloat, _ rowIndex: Int) {
        let stagger = style == "reduced" ? 0 : CGFloat(rowIndex) * 0.025
        let rowP = clamp((p - stagger) / 0.75)
        let rowAlpha = rowP * fade
        let labelColor = row.3 ? yellow : warm
        leftText(ctx, row.0, x, row.2, 17, 17, bayonFont, labelColor, rowAlpha)
        rightText(ctx, row.1, x + 337, row.2, 17, 17, monoFont, row.3 ? yellow : warm, rowAlpha)
        ctx.saveGState(); ctx.setStrokeColor(withAlpha(muted, 0.28 * rowAlpha)); ctx.setLineWidth(1)
        ctx.move(to: CGPoint(x: x, y: CGFloat(height) - (row.2 + 43))); ctx.addLine(to: CGPoint(x: x + 337, y: CGFloat(height) - (row.2 + 43)))
        ctx.strokePath(); ctx.restoreGState()
    }
    for (i, row) in leftRows.enumerated() { settingRow(row, 90, i) }
    for (i, row) in rightRows.enumerated() { settingRow(row, 447, i) }
}

for frame in 0..<frameCount {
    let time = CGFloat(frame) / CGFloat(fps)
    guard let colorSpace = CGColorSpace(name: CGColorSpace.sRGB),
          let ctx = CGContext(data: nil, width: width, height: height, bitsPerComponent: 8, bytesPerRow: width * 4, space: colorSpace, bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue) else { continue }
    ctx.setAllowsAntialiasing(true)
    ctx.setShouldAntialias(true)
    drawScene(ctx)
    let titleOut = clamp((0.62 - time) / 0.18)
    let titleIn = progress(time, 0.0, variant == "reduced" ? 0.16 : 0.30, variant)
    if time < 0.80 {
        // The Figma title is a supplied logo asset. Keep its measured frame
        // (167, 133.28, 540, 107.44) instead of rebuilding it with a fallback
        // typeface. The prompt is intentionally text-only per the latest UI
        // direction: no yellow button or extra container.
        let titleAlpha = min(titleIn, titleOut)
        let logoScale = variant == "reduced" ? 1 : lerp(0.96, 1, titleIn)
        let logoOffset = variant == "reduced" ? 0 : lerp(-8, 0, titleIn)
        drawImage(ctx, titleLogo, CGRect(x: 167, y: 133.28, width: 540, height: 107.44), titleAlpha, logoScale, logoOffset)
        let promptOffset = variant == "reduced" ? 0 : lerp(8, 0, titleIn)
        centeredText(ctx, "TAP TO START", 437, 300 + promptOffset, 17, 17, bayonFont, warm, titleAlpha)
    }
    if time > 0.30 { drawHud(ctx, time - 0.30, variant) }
    if time > 1.42 { drawPause(ctx, time, variant) }
    if time > 2.16 { drawSettings(ctx, time, variant) }
    if time > 2.98 {
        let fade = clamp((3.60 - time) / 0.3)
        centeredText(ctx, variant == "tactile" ? "Tactile / responsive" : variant == "reduced" ? "Reduced motion" : "Cinematic / quiet", 437, 382, 11, 14, monoFont, muted, fade)
    }
    if let image = ctx.makeImage() {
        let file = outURL.appendingPathComponent(String(format: "frame_%04d.png", frame))
        if let destination = CGImageDestinationCreateWithURL(file as CFURL, UTType.png.identifier as CFString, 1, nil) {
            CGImageDestinationAddImage(destination, image, nil)
            CGImageDestinationFinalize(destination)
        }
    }
}
print("Rendered \(frameCount) frames for \(variant) at \(outURL.path)")
