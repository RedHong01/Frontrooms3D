import Foundation
import PDFKit
import AppKit
// usage: render_pdf <in.pdf> <outdir> <prefix> <dpi>
let a = CommandLine.arguments
guard a.count >= 5, let doc = PDFDocument(url: URL(fileURLWithPath: a[1])) else { print("bad args or pdf"); exit(1) }
let outdir = a[2], prefix = a[3], dpi = Double(a[4]) ?? 150
for i in 0..<doc.pageCount {
  guard let p = doc.page(at: i) else { continue }
  let r = p.bounds(for: .mediaBox); let s = dpi / 72.0
  let w = Int(r.width * s), h = Int(r.height * s)
  guard let rep = NSBitmapImageRep(bitmapDataPlanes: nil, pixelsWide: w, pixelsHigh: h, bitsPerSample: 8, samplesPerPixel: 4, hasAlpha: true, isPlanar: false, colorSpaceName: .deviceRGB, bytesPerRow: 0, bitsPerPixel: 0) else { continue }
  NSGraphicsContext.saveGraphicsState()
  let ctx = NSGraphicsContext(bitmapImageRep: rep)!; NSGraphicsContext.current = ctx
  let cg = ctx.cgContext; cg.setFillColor(NSColor.white.cgColor); cg.fill(CGRect(x: 0, y: 0, width: w, height: h))
  cg.scaleBy(x: s, y: s); p.draw(with: .mediaBox, to: cg)
  NSGraphicsContext.restoreGraphicsState()
  let data = rep.representation(using: .png, properties: [:])!
  try! data.write(to: URL(fileURLWithPath: "\(outdir)/\(prefix)_p\(i + 1).png"))
  print("\(prefix)_p\(i + 1).png \(w)x\(h)")
}
