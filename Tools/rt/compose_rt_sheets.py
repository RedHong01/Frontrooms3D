#!/usr/bin/env python3
"""Compose the G14 P0/P1 acceptance sheets from Verification/rt_p0p1 (FrontRoomsGlassRTVerify output).

Usage: /usr/bin/python3 Tools/rt/compose_rt_sheets.py <rt_p0p1 dir> <images out dir> [file prefix, e.g. 30_]
Needs Pillow (Xcode's /usr/bin/python3 has it). Sheets are JPG quality 85, at most 1600 px wide.

Panel labels (AD-8, 2026-10-14): "traced radiance" panels show the ungraded linear radiance the glass receives (x2
exposure, Reinhard, no ACES / grade): beige next to the graded game frames by construction. The planar mirror
reference is a second camera at the mirrored eye: it also renders the window stops that sit in front of the mirror
plane and the black void outside the level, which are reference artefacts, not RT errors.
"""
import os
import sys
from PIL import Image, ImageDraw, ImageFont

SRC, OUT = sys.argv[1], sys.argv[2]
PREFIX = sys.argv[3] if len(sys.argv) > 3 else ""
RAD = "traced radiance (ungraded linear, x2, Reinhard; magenta = miss)"
REF = "planar mirror reference (also renders the window stops and the void)"
os.makedirs(OUT, exist_ok=True)
MAXW = 1600


def font(size):
    for f in ["/System/Library/Fonts/Supplemental/Arial.ttf", "/System/Library/Fonts/Helvetica.ttc", "/System/Library/Fonts/Geneva.ttf"]:
        if os.path.exists(f):
            try:
                return ImageFont.truetype(f, size)
            except Exception:
                pass
    return ImageFont.load_default()


F = font(26)
FS = font(18)


def load(name):
    p = os.path.join(SRC, name)
    return Image.open(p).convert("RGB") if os.path.exists(p) else None


def label(im, text):
    d = ImageDraw.Draw(im)
    w = d.textlength(text, font=F)
    d.rectangle([0, 0, w + 20, 40], fill=(0, 0, 0))
    d.text((10, 6), text, fill=(255, 255, 255), font=F)
    return im


def grid(panels, cols, out, title=None):
    panels = [(im, t) for im, t in panels if im is not None]
    if not panels:
        print("skip", out)
        return
    w, h = panels[0][0].size
    rows = (len(panels) + cols - 1) // cols
    top = 50 if title else 0
    sheet = Image.new("RGB", (w * cols, h * rows + top), (20, 20, 20))
    if title:
        ImageDraw.Draw(sheet).text((12, 10), title, fill=(255, 255, 255), font=F)
    for k, (im, t) in enumerate(panels):
        im = label(im.resize((w, h)), t)
        sheet.paste(im, ((k % cols) * w, top + (k // cols) * h))
    if sheet.width > MAXW:
        sheet = sheet.resize((MAXW, int(sheet.height * MAXW / sheet.width)), Image.LANCZOS)
    sheet.save(os.path.join(OUT, PREFIX + out), quality=85)
    print("wrote", PREFIX + out, sheet.size)


views = sorted({f.split("_off.png")[0] for f in os.listdir(SRC) if (f.startswith("v") or f.startswith("o")) and f.endswith("_off.png")})
for v in views:
    grid([(load(v + "_off.png"), "RT off (zone cube)"), (load(v + "_ref.png"), REF),
          (load(v + "_high.png"), "RT High"), (load(v + "_ultra.png"), "RT Ultra"),
          (load(v + "_rt_high.png"), RAD + ", High"), (load(v + "_rt_ultra.png"), "traced radiance, Ultra")],
         2, "P1_" + v + ".jpg", "Level 0 · " + v + " · 1920x1080 game camera, 4x MSAA, full post")

for v in ["v2_headon_centre", "v3_angle50"]:
    grid([(load("P1_dark_beyond_" + v + "_off.png"), "dark room beyond, RT off"), (load("P1_dark_beyond_" + v + "_high.png"), "dark room beyond, RT High"),
          (load("P1_dark_beyond_" + v + "_ultra.png"), "dark room beyond, RT Ultra")], 3, "P1_dark_beyond_" + v + ".jpg",
         "Dark room beyond the pane (staged black room): the glass becomes a one-way mirror")

offs = sorted({f[len("P1_office_"):].split("_off.png")[0] for f in os.listdir(SRC) if f.startswith("P1_office_") and f.endswith("_off.png")})
for v in offs:
    grid([(load("P1_office_" + v + "_off.png"), "Office, RT off"), (load("P1_office_" + v + "_ref.png"), "planar mirror reference"),
          (load("P1_office_" + v + "_high.png"), "RT High"), (load("P1_office_" + v + "_ultra.png"), "RT Ultra"),
          (load("P1_office_" + v + "_rt_high.png"), RAD + ", High")], 2, "P1_office_" + v + ".jpg", "Office zone · " + v)

grid([(load("P0_orientation_rt.png"), "RT High, red cube behind-left, green behind-right"), (load("P0_orientation_rt_raw.png"), RAD),
      (load("P0_orientation_mirror_raw.png"), "planar mirror reference, same pixels")], 3, "P0_orientation.jpg", "Orientation test (P0-A3)")
grid([(load("P0_occluder.png"), "unregistered panel in front of the pane: nothing painted over it")], 1, "P0_occluder.jpg", "Occluder test (P0-A5)")
grid([(load("P1_relay_behind.png"), "Relay body 2 m behind the player, High"), (load("P1_relay_behind_rt.png"), RAD),
      (load("P1_relay_behind_ultra.png"), "Ultra"), (load("P0_relay_moving.png"), "moved 0.7 m: reflection updated the same frame")], 2, "P0_relay_moving.jpg", "The Relay in the reflection (P0-A5, P1-B4)")
grid([(load("P0_broken_pane_before.png"), "pane intact, RT High"), (load("P0_broken_pane.png"), "break frame: the trace leaves with the pane")], 2, "P0_broken_pane.jpg", "Broken pane (P0-A5)")
grid([(load("P0_stream_test.png"), "after Destroy(mesh): no ghost")], 1, "P0_stream_test.jpg", "Streamed mesh (P0-A5)")
grid([(load("P1_temporal_still_frame0.png"), "still camera, frame 0 (1 ray/px)"), (load("P1_temporal_still_frame33.png"), "still camera, frame 33 (accumulated, jittered)")], 2, "P1_temporal_still.jpg",
     "Temporal accumulation while nothing moves")
grid([(load("P1_edge_crawl_unfiltered.png"), "strafe 1 cm/frame: 1 ray, no filter"), (load("P1_edge_crawl_high.png"), "High: edge filter"),
      (load("P1_edge_crawl_ultra.png"), "Ultra: edge filter + adaptive rays")], 3, "P1_edge_crawl.jpg", "Moving camera (frame 15 of 30)")
grid([(load("P1_parity_raster.png"), "raster (post off, SSAO off)"), (load("P1_parity_rt.png"), "primary rays through the RT hit shader"),
      (load("P1_parity_diff8.png"), "|difference| x8")], 3, "P1_parity.jpg", "Hit-shading parity (P1-B1)")
grid([(load("P1_office_parity_raster.png"), "Office raster (post off, SSAO off)"), (load("P1_office_parity_rt.png"), "primary rays through the RT hit shader"),
      (load("P1_office_parity_diff8.png"), "|difference| x8")], 3, "P1_office_parity.jpg", "Hit-shading parity in the Office (AD-6)")

# 2026-10-14 fix pass: the showcase shots (AD-6)
grid([(load("P1_darkrelay_off.png"), "dark room beyond, Relay 2 m behind: RT off"), (load("P1_darkrelay_high.png"), "RT High"),
      (load("P1_darkrelay_ultra.png"), "RT Ultra"), (load("P1_darkrelay_rt.png"), RAD + ", High")], 2, "P1_darkrelay.jpg",
     "The Relay behind you in a dark-beyond window (the pane is a mirror there)")
grid([(load("P1_darkrelay_walk_f%02d.png" % f), "Relay crossing, frame %d of 30" % f) for f in (0, 10, 20, 29)], 2, "P1_darkrelay_walk.jpg",
     "The Relay crossing 2 m behind (6.7 cm per frame), RT High")
grid([(load("P1_moving_f%02d.png" % f), "frame %d of 60" % f) for f in (0, 15, 30, 45)] + [(load("P1_moving_rt_f30.png"), RAD + ", frame 30")], 3, "P1_moving.jpg",
     "Moving shot with lamps in the reflection: v2, 2 cm per frame, RT High")
bi = sorted({f.split("_rt_on.png")[0] for f in os.listdir(SRC) if f.startswith("b5_back") and f.endswith("_rt_on.png")})
for v in bi:
    grid([(load(v + "_high.png"), "close and oblique, RT High"), (load(v + "_rt_on.png"), RAD + ", back-surface image on"),
          (load(v + "_rt_off.png"), "back-surface image off")], 3, "P1_" + v + ".jpg", "Thin glass: the back-surface image of the 6 mm pane (P1-B5)")
grid([(load("P1_hdroff_off.png"), "camera HDR off: RT off"), (load("P1_hdroff_high.png"), "camera HDR off: RT High")], 2, "P1_hdroff.jpg",
     "LDR display path (camera HDR off)")


def bars(out, title, rows, unit="ms"):
    # rows: list of (label, [(series, value)])
    if not rows:
        return
    series = []
    for _, vals in rows:
        for s, _ in vals:
            if s not in series:
                series.append(s)
    colors = [(120, 120, 120), (90, 150, 220), (230, 160, 60), (200, 80, 80), (80, 180, 120)]
    vmax = max(max(v for _, vals in rows for _, v in vals), 1e-3)
    W, H = 1600, 120 + 70 * len(rows)
    im = Image.new("RGB", (W, H), (250, 250, 248))
    d = ImageDraw.Draw(im)
    d.text((20, 15), title, fill=(0, 0, 0), font=F)
    x0, x1 = 420, W - 120
    for k, s in enumerate(series):
        d.rectangle([x0 + k * 220, 60, x0 + k * 220 + 20, 80], fill=colors[k % len(colors)])
        d.text((x0 + k * 220 + 28, 58), s, fill=(0, 0, 0), font=FS)
    y = 100
    for lab, vals in rows:
        d.text((20, y + 10), lab, fill=(0, 0, 0), font=FS)
        bh = 56 // max(1, len(vals))
        for k, (s, v) in enumerate(vals):
            c = colors[series.index(s) % len(colors)]
            w = (x1 - x0) * max(v, 0.0) / vmax   # a negative delta (noise on the shared GPU) draws no bar, only its value
            if w > 0:
                d.rectangle([x0, y + k * bh, x0 + w, y + (k + 1) * bh - 2], fill=c)
            d.text((x0 + w + 6, y + k * bh - 2), ("%.2f " % v) + unit, fill=(0, 0, 0), font=FS)
        y += 70
    im.save(os.path.join(OUT, PREFIX + out), quality=85)
    print("wrote", PREFIX + out)


# metrics
m = {}
p = os.path.join(SRC, "metrics.tsv")
if os.path.exists(p):
    for line in open(p).read().splitlines()[1:]:
        k, _, v = line.partition("\t")
        try:
            m[k] = float(v)
        except ValueError:
            pass
rows = []
for k in sorted({k.rsplit(".", 1)[0] for k in m if k.startswith("cost.") and k.count(".") == 3}):
    rows.append((k[5:].replace(".", " "), [(s, m.get(k + "." + s, 0)) for s in ["high.as", "high.trace", "high.resolve", "ultra.trace"] if k + "." + s in m]))
bars("P1_cost_chart.jpg", "RT GPU time per stage (stage-boundary GPU counters, median of 22 interleaved rounds; other Unity jobs shared the GPU)", rows)
rows = []
for k in sorted({k.rsplit(".", 1)[0] for k in m if k.startswith("cost.") and k.count(".") == 3}):
    off = m.get(k + ".off", 0)
    rows.append((k[5:].replace(".", " "), [("empty pass - off", m.get(k + ".empty", 0) - off), ("High - off", m.get(k + ".high", 0) - off), ("Ultra - off", m.get(k + ".ultra", 0) - off)]))
bars("P0_frame_delta_chart.jpg", "Whole-frame GPU time added by RT (median of 22 interleaved rounds; other Unity jobs shared the GPU)", rows)
# the least-disturbed frames (10th percentile of 22 interleaved rounds): the closest this shared Mac gets to an idle GPU
rows = []
for k in sorted({k.rsplit(".", 1)[0] for k in m if k.startswith("cost.") and k.count(".") == 3}):
    if k + ".off.p10" not in m:
        continue
    off = m[k + ".off.p10"]
    rows.append((k[5:].replace(".", " "), [("off (whole frame)", off), ("High (whole frame)", m.get(k + ".high.p10", 0)), ("Ultra (whole frame)", m.get(k + ".ultra.p10", 0)),
                                           ("High: AS + trace + resolve", m.get(k + ".high.as.p10", 0) + m.get(k + ".high.trace.p10", 0) + m.get(k + ".high.resolve.p10", 0))]))
bars("P1_cost_p10_chart.jpg", "GPU ms, 10th percentile of 22 interleaved rounds (least disturbed by the other Unity jobs on this Mac)", rows)

# walk chart
p = os.path.join(SRC, "walk_frames.tsv")
if os.path.exists(p):
    lines = open(p).read().splitlines()[1:]
    vals = [l.split("\t") for l in lines]
    main = [float(v[1]) for v in vals]
    enc = [float(v[2]) for v in vals]
    vis = [int(v[5]) for v in vals]
    tr = [int(v[6]) for v in vals]
    W, H = 1600, 520
    im = Image.new("RGB", (W, H), (250, 250, 248))
    d = ImageDraw.Draw(im)
    d.text((20, 12), "600-frame walk at 4 m/s: RT main-thread ms (blue), render-thread encode ms (orange), clipped at 8 ms (^ = clipped); grey = receiver in view, red = traced", fill=(0, 0, 0), font=FS)
    x0, x1, y0, y1 = 60, W - 20, 60, H - 60
    # spikes from the shared, overloaded Mac would flatten the plot: the scale stops at 8 ms, clipped frames get a mark
    vmax = min(max(max(main), max(enc), 1.0), 8.0)
    for i, v in enumerate(main):
        if v > vmax:
            x = x0 + (x1 - x0) * i / max(1, len(vals) - 1)
            d.text((x - 4, y0 - 22), "^", fill=(60, 110, 200), font=FS)
    main = [min(v, vmax) for v in main]
    enc = [min(v, vmax) for v in enc]
    for i in range(len(vals)):
        x = x0 + (x1 - x0) * i / max(1, len(vals) - 1)
        if vis[i]:
            d.line([x, y1 + 8, x, y1 + 18], fill=(160, 160, 160))
        if tr[i]:
            d.line([x, y1 + 22, x, y1 + 32], fill=(200, 60, 60))
    for series, col in [(main, (60, 110, 200)), (enc, (230, 140, 40))]:
        pts = [(x0 + (x1 - x0) * i / max(1, len(series) - 1), y1 - (y1 - y0) * v / vmax) for i, v in enumerate(series)]
        d.line(pts, fill=col, width=2)
    for t in range(0, int(vmax) + 1):
        y = y1 - (y1 - y0) * t / vmax
        d.line([x0 - 5, y, x0, y], fill=(0, 0, 0))
        d.text((10, y - 9), "%d ms" % t, fill=(0, 0, 0), font=FS)
    im.save(os.path.join(OUT, PREFIX + "P0_walk_chart.jpg"), quality=85)
    print("wrote P0_walk_chart.jpg")

# lamp sync chart
p = os.path.join(SRC, "lamp_sync.tsv")
if os.path.exists(p):
    vals = [l.split("\t") for l in open(p).read().splitlines()[1:]]
    a = [float(v[1]) for v in vals]
    b = [float(v[2]) for v in vals]
    W, H = 1600, 420
    im = Image.new("RGB", (W, H), (250, 250, 248))
    d = ImageDraw.Draw(im)
    d.text((20, 12), "Lamp sync over 120 frames: real lens emission (linear, blue) vs its reflection's traced radiance (orange), each scaled to its own range", fill=(0, 0, 0), font=FS)
    x0, x1, y0, y1 = 40, W - 20, 50, H - 30
    for s, col in [(a, (60, 110, 200)), (b, (230, 140, 40))]:
        lo, hi = min(s), max(s)
        rng = (hi - lo) or 1
        pts = [(x0 + (x1 - x0) * i / max(1, len(s) - 1), y1 - (y1 - y0) * (v - lo) / rng) for i, v in enumerate(s)]
        d.line(pts, fill=col, width=2)
    im.save(os.path.join(OUT, PREFIX + "P1_lamp_sync_chart.jpg"), quality=85)
    print("wrote P1_lamp_sync_chart.jpg")
