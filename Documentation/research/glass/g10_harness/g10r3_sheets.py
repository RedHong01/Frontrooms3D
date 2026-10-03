"""G10 run 3 (fix pass) sheets. Usage: /usr/bin/python3 g10r3_sheets.py <run3 dir> <run2 dir> <out dir>
Compares BEFORE | previous AFTER (run 2, state B = main's ambient) | new AFTER (run 3, state B) | no pane.
Every output is JPEG quality 85."""
import os, re, sys, shutil
from PIL import Image, ImageDraw, ImageFont, ImageChops, ImageStat

r3, r2, out = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(out, exist_ok=True)
W, H = 1920, 1080
FONT = ImageFont.truetype("/System/Library/Fonts/Helvetica.ttc", 26)
SMALL = ImageFont.truetype("/System/Library/Fonts/Helvetica.ttc", 19)
BG = (24, 24, 24)

def load(d, name):
    p = os.path.join(d, name)
    return Image.open(p).convert("RGB") if os.path.exists(p) else None

def label(img, text, font=SMALL):
    d = ImageDraw.Draw(img)
    b = d.textbbox((8, 6), text, font=font)
    d.rectangle((b[0] - 5, b[1] - 3, b[2] + 5, b[3] + 3), fill=(0, 0, 0))
    d.text((8, 6), text, fill=(255, 255, 255), font=font)
    return img

def save(img, name):
    img.save(os.path.join(out, name), "JPEG", quality=85)
    print("wrote", name, img.size)

boxes, last = {}, None
for line in open(os.path.join(r3, "g10_frames.txt"), encoding="utf-8"):
    m = re.match(r"^(\S+) \| frame", line)
    if m:
        last = m.group(1); continue
    m = re.search(r"pane box \(bottom-left origin\) (\d+),(\d+) (\d+)x(\d+)", line)
    if m and last:
        x, y, w, h = map(int, m.groups())
        boxes[last] = (x, H - (y + h), x + w, H - y)

# (dir, suffix, column label)
COLS4 = [(r3, "A_before", "BEFORE (shipped)"), (r2, "B_glass_sceneambient", "previous AFTER (run 2)"),
         (r3, "B_glass_sceneambient", "NEW AFTER (fix pass)"), (r3, "N_nopane_after", "no pane")]
COLS3 = COLS4[:3]

def grid(title, rows, cols, name, tw=560, th=315):
    gap = 6
    present = [(rid, rl) for rid, rl in rows if load(r3, f"{rid}_A_before.jpg")]
    sheet = Image.new("RGB", (len(cols) * tw + (len(cols) - 1) * gap, len(present) * (th + gap) + 48), BG)
    ImageDraw.Draw(sheet).text((10, 10), title, fill=(255, 255, 255), font=FONT)
    for r, (rid, rl) in enumerate(present):
        for c, (d, suffix, cl) in enumerate(cols):
            im = load(d, f"{rid}_{suffix}.jpg")
            if im is None: continue
            sheet.paste(label(im.resize((tw, th), Image.LANCZOS), f"{rl} · {cl}"), (c * (tw + gap), 48 + r * (th + gap)))
    save(sheet, name)

grid("G10 run 3 · audit frames 01 / 34 / 35 / 37, seed 4242 · BEFORE | previous AFTER | NEW AFTER",
     [("01_rep_L0", "01 Level 0"), ("34_rep_Office", "34 Office"), ("35_rep_Office_b", "35 Office"), ("37_rep_Dark", "37 dead lamp")], COLS3,
     "g10r3_01_representative.jpg", tw=640, th=360)
grid("G10 run 3 · Level 0 window (282,206) · BEFORE | previous AFTER | NEW AFTER | no pane",
     [("window_L0_1.5m", "1.5 m"), ("window_L0_oblique50", "~50° (audit 04)"), ("window_L0_0.7m", "0.7 m"), ("window_L0_steep", "61°")], COLS4,
     "g10r3_02_window_L0.jpg")
grid("G10 run 3 · Office window (281,206) · BEFORE | previous AFTER | NEW AFTER | no pane",
     [("window_Office_1.5m", "1.5 m"), ("window_Office_oblique50", "~50° (audit 23)"), ("window_Office_0.7m", "0.7 m"), ("window_Office_steep", "61°")], COLS4,
     "g10r3_03_window_Office.jpg")
grid("G10 run 3 · dead lamp · 38: window seen from under a dead lamp · 39: same window from the lit side",
     [("38_deadlamp_window_1.5m", "38 1.5 m"), ("38_deadlamp_window_oblique50", "38 ~50°"), ("39_dark_beyond_window_1.5m", "39 1.5 m")], COLS4,
     "g10r3_06_deadlamp_windows.jpg")

def crops_1to1(ids, name, cw=460, ch=330):
    gap = 6
    rows = [i for i in ids if i in boxes and load(r3, f"{i}_A_before.jpg")]
    sheet = Image.new("RGB", (4 * cw + 3 * gap, len(rows) * (ch + gap) + 48), BG)
    ImageDraw.Draw(sheet).text((10, 10), "G10 run 3 · 1:1 crops at the pane centre · BEFORE | previous AFTER | NEW AFTER | no pane", fill=(255, 255, 255), font=FONT)
    for r, i in enumerate(rows):
        x0, y0, x1, y1 = boxes[i]
        cx, cy = (x0 + x1) // 2, (y0 + y1) // 2
        box = (max(0, cx - cw // 2), max(0, cy - ch // 2), max(0, cx - cw // 2) + cw, max(0, cy - ch // 2) + ch)
        for c, (d, s, cl) in enumerate(COLS4):
            im = load(d, f"{i}_{s}.jpg")
            if im: sheet.paste(label(im.crop(box), f"{i.replace('window_', '')} · {cl}"), (c * (cw + gap), 48 + r * (ch + gap)))
    save(sheet, name)

crops_1to1(["window_L0_1.5m", "window_L0_0.7m", "window_L0_oblique50", "window_Office_1.5m", "window_Office_0.7m", "window_Office_steep"], "g10r3_04_window_crops_1to1.jpg")

DIAG = [("N_nopane_after", "no pane"), ("B_glass_sceneambient", "NEW AFTER"), ("D_diff_x6", "|NEW AFTER − no pane| ×6"), ("VP", "previous values, new grime layout"),
        ("VF1", "_PaneF0 .04 only (one surface)"), ("VR0", "_ReflectionMin 0"), ("VS0", "_Scatter 0"), ("VG0", "grime off")]
def diag(ids, name):
    tw, th, gap = 470, 330, 6
    rows = [i for i in ids if i in boxes]
    sheet = Image.new("RGB", (4 * tw + 3 * gap, len(rows) * 2 * (th + gap) + 48), BG)
    ImageDraw.Draw(sheet).text((10, 10), "G10 run 3 · one knob changed at a time on a copy of Glass_Window (pane region)", fill=(255, 255, 255), font=FONT)
    y = 48
    for i in rows:
        x0, y0, x1, y1 = boxes[i]
        pad = 30
        box = (max(0, x0 - pad), max(0, y0 - pad), min(W, x1 + pad), min(H, y1 + pad))
        bw, bh = box[2] - box[0], box[3] - box[1]
        s = min(tw / bw, th / bh)
        for k, (suffix, cl) in enumerate(DIAG):
            im = load(r3, f"{i}_{suffix}.jpg")
            if im is None: continue
            t = Image.new("RGB", (tw, th), BG)
            c = im.crop(box).resize((max(1, int(bw * s)), max(1, int(bh * s))), Image.LANCZOS)
            t.paste(c, ((tw - c.width) // 2, (th - c.height) // 2))
            sheet.paste(label(t, f"{i.replace('window_', '')} · {cl}"), ((k % 4) * (tw + gap), y + (k // 4) * (th + gap)))
        y += 2 * (th + gap)
    save(sheet, name)

diag(["window_L0_1.5m", "window_L0_oblique50", "window_L0_steep"], "g10r3_05_diagnostics_L0.jpg")
diag(["window_Office_oblique50", "38_deadlamp_window_1.5m", "39_dark_beyond_window_1.5m"], "g10r3_05b_diagnostics_Office_deadlamp.jpg")

PROPS = [("40_prop_bottle_WaterCooler", "water-cooler bottle", (700, 150, 1280, 560)),
         ("40_prop_vending_front", "vending front", None),
         ("40_prop_vending_oblique", "vending ~53°", None),
         ("41_desk_glass_STAGED", "STAGED desk glass", (900, 420, 1440, 700)),
         ("42_hutch_cabinet_STAGED", "STAGED hutch + cabinet", (540, 220, 1260, 760))]
def props(name):
    tw, th, gap = 640, 360, 6
    rows = [p for p in PROPS if load(r3, f"{p[0]}_A_before.jpg")]
    hgt = 48 + sum((th + gap) * (2 if crop else 1) for _, _, crop in rows)
    sheet = Image.new("RGB", (3 * tw + 2 * gap, hgt), BG)
    ImageDraw.Draw(sheet).text((10, 10), "G10 run 3 · glass props · BEFORE | previous AFTER | NEW AFTER (full frame, then a crop where marked)", fill=(255, 255, 255), font=FONT)
    y = 48
    for pid, what, crop in rows:
        ims = [load(d, f"{pid}_{s}.jpg") for d, s, _ in COLS3]
        for k, im in enumerate(ims):
            if im: sheet.paste(label(im.resize((tw, th), Image.LANCZOS), f"{what} · {COLS3[k][2]}"), (k * (tw + gap), y))
        y += th + gap
        if crop:
            s = min(tw / (crop[2] - crop[0]), th / (crop[3] - crop[1]))
            size = (int((crop[2] - crop[0]) * s), int((crop[3] - crop[1]) * s))
            for k, im in enumerate(ims):
                if im is None: continue
                t = Image.new("RGB", (tw, th), BG)
                t.paste(im.crop(crop).resize(size, Image.LANCZOS), ((tw - size[0]) // 2, (th - size[1]) // 2))
                sheet.paste(label(t, f"crop x{s:.1f} · {COLS3[k][2]}"), (k * (tw + gap), y))
            y += th + gap
    save(sheet, name)

props("g10r3_07_props.jpg")

def shards(name):
    tw, th, gap = 470, 330, 6
    cols = [("N_noshards", "no shards"), ("S0", "previous Glass_Shard (near-black)"), ("S1", "NEW Glass_Shard (0.85 × L0 carpet)"), ("S3", "transparent FrontRooms/Glass")]
    rows = [r for r in ("50_shards_1m", "50_shards_2m", "51_shards_office_1m", "51_shards_office_2m") if load(r3, f"{r}_S1.jpg")]
    sheet = Image.new("RGB", (4 * tw + 3 * gap, len(rows) * 2 * (th + gap) + 48), BG)
    ImageDraw.Draw(sheet).text((10, 10), "G10 run 3 · STAGED shards on Level 0 and Office carpet, 1 m and 2 m · full frame, then a 1:1 crop at the patch", fill=(255, 255, 255), font=FONT)
    y = 48
    for r in rows:
        crop = (W // 2 - tw // 2, H // 2 - th // 2, W // 2 + tw // 2, H // 2 + th // 2)
        for k, (s, cl) in enumerate(cols):
            im = load(r3, f"{r}_{s}.jpg")
            if im is None: continue
            sheet.paste(label(im.resize((tw, th), Image.LANCZOS), f"{r[3:]} · {cl}"), (k * (tw + gap), y))
            sheet.paste(label(im.crop(crop), f"1:1 · {cl}"), (k * (tw + gap), y + th + gap))
        y += 2 * (th + gap)
    save(sheet, name)

shards("g10r3_08_shards.jpg")

# full-resolution frames (copied, not re-encoded)
FULL = ["window_L0_1.5m", "window_L0_oblique50", "window_L0_0.7m", "window_L0_steep", "window_Office_oblique50", "39_dark_beyond_window_1.5m", "42_hutch_cabinet_STAGED", "01_rep_L0"]
for i in FULL:
    p = os.path.join(r3, f"{i}_B_glass_sceneambient.jpg")
    if os.path.exists(p): shutil.copyfile(p, os.path.join(out, f"g10r3_full_{i}_AFTER.jpg"))
for r in ("50_shards_1m", "51_shards_office_1m"):
    p = os.path.join(r3, f"{r}_S1.jpg")
    if os.path.exists(p): shutil.copyfile(p, os.path.join(out, f"g10r3_full_{r}_S1.jpg"))
print("copied full frames")

# run 2 vs run 3 BEFORE frames: is the comparison like for like?
for f in sorted(os.listdir(r3)):
    if f.endswith("_A_before.jpg") and os.path.exists(os.path.join(r2, f)):
        a, b = load(r3, f), load(r2, f)
        st = ImageStat.Stat(ImageChops.difference(a, b))
        print("A run3 vs run2", f, "mean |Δ| %.2f /255" % (sum(st.mean) / 3))
