"""FrontRooms playtest sheet reader (optical mark reading, numpy + Pillow only).

usage: python3 omr_read.py TEMPLATE.json OUT.csv OUT_DIR [--blank BLANK_SCAN] scan1.png [scan2.pdf ...]

A scan is a 200-300 dpi scan or a flat phone photo of one filled sheet; a PDF is split into pages
(rendered at 200 dpi by render_pdf, built once from render_pdf.swift). For every page:
  1. find the four corner squares and map template points (pt) to image pixels (homography);
  2. try the page both ways up and keep the one whose timing marks are dark;
  3. lock each machine row onto its timing mark (vertical offset), then snap the row's ovals onto
     their printed outlines;
  4. fill = darkness inside the oval minus its blank level (from --blank, a blank sheet of the same print run;
     otherwise the row's lightest oval) -> a value, blank, light, check or MULTI;
  5. straighten every write-in box and save it as an image.
Writes OUT.csv (one row per page), OUT_DIR/crops/ (write-in boxes), OUT_DIR/by_question/ (each write-in
question across all pages, for reading) and OUT_DIR/check/ (a proof sheet per page: what the reader saw).
"""
import csv, json, os, subprocess, sys, tempfile
import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))


def font(size):
    """IBM Plex Mono for proof labels when it is installed (the form's own mono), else Pillow's default."""
    for p in (os.path.expanduser("~/Library/Fonts/IBMPlexMono-Regular.otf"), "/Library/Fonts/IBMPlexMono-Regular.otf"):
        if os.path.exists(p): return ImageFont.truetype(p, size)
    return ImageFont.load_default()


def load_gray(path):
    """Max of red and green: canary paper reads as white, graphite and black ink stay dark."""
    im = Image.open(path).convert("RGB")
    a = np.asarray(im).astype(np.float32) / 255.0
    return np.maximum(a[..., 0], a[..., 1])


def otsu(a):
    h, _ = np.histogram(a, bins=256, range=(0, 1)); p = h / h.sum(); w = np.cumsum(p); m = np.cumsum(p * np.arange(256)); mt = m[-1]
    with np.errstate(divide="ignore", invalid="ignore"):
        s = (mt * w - m) ** 2 / (w * (1 - w))
    return np.nanargmax(s) / 255.0


def blobs(mask):
    """4-connected components on a small mask; returns (area, cy, cx, h, w) per blob."""
    H, W = mask.shape; lab = np.zeros(mask.shape, np.int32); out = []; n = 0
    for y0, x0 in zip(*np.nonzero(mask)):
        if lab[y0, x0]: continue
        n += 1; stack = [(y0, x0)]; lab[y0, x0] = n; ys = []; xs = []
        while stack:
            y, x = stack.pop(); ys.append(y); xs.append(x)
            for yy, xx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
                if 0 <= yy < H and 0 <= xx < W and mask[yy, xx] and not lab[yy, xx]:
                    lab[yy, xx] = n; stack.append((yy, xx))
        ys = np.array(ys); xs = np.array(xs)
        out.append((len(ys), ys.mean(), xs.mean(), int(np.ptp(ys)) + 1, int(np.ptp(xs)) + 1))
    return out


def find_fiducials(g, tpl):
    """In each image corner window, the most square, solid dark blob of plausible size, nearest the corner."""
    H, W = g.shape; s = 900.0 / max(H, W)
    small = np.asarray(Image.fromarray((g * 255).astype(np.uint8)).resize((max(1, int(W * s)), max(1, int(H * s))), Image.BILINEAR)).astype(np.float32) / 255.0
    t = otsu(small); dark = small < min(t, 0.55)
    h, w = dark.shape; win = 0.22; pts = []
    page_w, page_h = tpl["page"]["w"], tpl["page"]["h"]
    side_pt = np.mean([f[2] for f in tpl["fiducials"]])
    exp = side_pt / max(page_w, page_h) * max(h, w)  # expected side in small px (page may be rotated)
    for cy, cx in ((0, 0), (0, 1), (1, 0), (1, 1)):
        y0 = 0 if cy == 0 else int(h * (1 - win)); x0 = 0 if cx == 0 else int(w * (1 - win))
        sub = dark[y0:y0 + int(h * win), x0:x0 + int(w * win)]
        best = None
        for area, my, mx, bh, bw in blobs(sub):
            if area < 0.5 * exp * exp or area > 2.5 * exp * exp: continue  # a filled 12 pt answer box is 0.44: never a corner
            squareness = min(bh, bw) / max(bh, bw); solidity = area / (bh * bw)
            if squareness < 0.7 or solidity < 0.75: continue
            dist = np.hypot((my + y0) / h - cy, (mx + x0) / w - cx)
            sc = dist - 0.2 * squareness
            if best is None or sc < best[0]: best = (sc, (my + y0) / s, (mx + x0) / s)
        if best is None: raise RuntimeError("corner square not found (corner %d%d)" % (cy, cx))
        pts.append((best[2], best[1]))  # (x, y) in full-res px
    return pts  # image TL, TR, BL, BR


def homography(src, dst):
    A = []
    for (x, y), (u, v) in zip(src, dst):
        A.append([-x, -y, -1, 0, 0, 0, u * x, u * y, u]); A.append([0, 0, 0, -x, -y, -1, v * x, v * y, v])
    _, _, Vt = np.linalg.svd(np.array(A)); Hm = Vt[-1].reshape(3, 3); return Hm / Hm[2, 2]


def warp_pts(Hm, xs, ys):
    xs = np.asarray(xs, float); ys = np.asarray(ys, float)
    p = Hm @ np.vstack([xs.ravel(), ys.ravel(), np.ones(xs.size)]); return p[0] / p[2], p[1] / p[2]


def sample(g, xs, ys):
    H, W = g.shape; xs = np.clip(xs, 0, W - 1.001); ys = np.clip(ys, 0, H - 1.001)
    x0 = np.floor(xs).astype(int); y0 = np.floor(ys).astype(int); fx = xs - x0; fy = ys - y0
    return (g[y0, x0] * (1 - fx) * (1 - fy) + g[y0, x0 + 1] * fx * (1 - fy) + g[y0 + 1, x0] * (1 - fx) * fy + g[y0 + 1, x0 + 1] * fx * fy)


def target_poly(o, shape):
    """Polygon (pt) of a target from its box and the template's shape, or None for an ellipse."""
    x, y, w, h = o["x"], o["y"], o["w"], o["h"]
    if shape and shape.get("type") == "box":
        return [(x, y), (x + w, y), (x + w, y + h), (x, y + h)]
    if shape and shape.get("type") == "arrow":  # tried 2026-10-15: the wallpaper chevron as a tag, roof at 55 deg
        r = shape["roof"]; return [(x + w / 2, y), (x + w, y + r), (x + w, y + h), (x, y + h), (x, y + r)]
    return None


def _seg_dist(px, py, ax, ay, bx, by):
    vx, vy = bx - ax, by - ay; t = np.clip(((px - ax) * vx + (py - ay) * vy) / (vx * vx + vy * vy), 0, 1)
    return np.hypot(px - (ax + t * vx), py - (ay + t * vy))


def prepare(tpl):
    """Per target: sample points inside (inset from the outline) and along the printed outline, as offsets from its centre."""
    shape = tpl.get("shape"); inset = tpl.get("inset", 1.75)
    for f in tpl["fields"]:
        if f["type"] != "single": continue
        for o in f["options"]:
            cx, cy = o["x"] + o["w"] / 2, o["y"] + o["h"] / 2; poly = target_poly(o, shape)
            if poly is None:  # ellipse: inner 45 % of the radii, ring on the outline
                t = np.linspace(-1, 1, 9); gx, gy = np.meshgrid(t, t); m = gx ** 2 + gy ** 2 <= 1
                o["_inner"] = np.stack([gx[m] * o["w"] / 2 * 0.45, gy[m] * o["h"] / 2 * 0.45], 1)
                th = np.linspace(0, 2 * np.pi, 24, endpoint=False)
                o["_ring"] = np.stack([np.cos(th) * o["w"] / 2, np.sin(th) * o["h"] / 2], 1)
                continue
            P = np.array(poly); xs, ys = np.meshgrid(np.arange(o["x"], o["x"] + o["w"], 0.5) + 0.25, np.arange(o["y"], o["y"] + o["h"], 0.5) + 0.25)
            px, py = xs.ravel(), ys.ravel(); inside = np.ones(px.size, bool); dmin = np.full(px.size, 1e9)
            n = len(P); area2 = sum(P[i][0] * P[(i + 1) % n][1] - P[(i + 1) % n][0] * P[i][1] for i in range(n))
            for i in range(n):
                (ax, ay), (bx, by) = P[i], P[(i + 1) % n]
                cross = (bx - ax) * (py - ay) - (by - ay) * (px - ax)
                inside &= (cross >= 0) if area2 > 0 else (cross <= 0)
                dmin = np.minimum(dmin, _seg_dist(px, py, ax, ay, bx, by))
            keep = inside & (dmin >= inset)
            o["_inner"] = np.stack([px[keep] - cx, py[keep] - cy], 1)
            ring = []  # the stroke centre: 0.375 pt inside the outline
            C = P.mean(0)
            for i in range(n):
                (ax, ay), (bx, by) = P[i], P[(i + 1) % n]
                for t in np.linspace(0, 1, 8, endpoint=False):
                    qx, qy = ax + t * (bx - ax), ay + t * (by - ay); dx, dy = C[0] - qx, C[1] - qy; d = np.hypot(dx, dy)
                    ring.append((qx + dx / d * 0.375 - cx, qy + dy / d * 0.375 - cy))
            o["_ring"] = np.array(ring)
    return tpl


def rect_darkness(g, Hm, r, paper, dx=0.0, dy=0.0, fx=0.6, fy=1.0, n=7):
    """Mean darkness on a grid covering the central fx x fy part of a rectangle (pt), shifted by dx, dy."""
    cx, cy = r["x"] + r["w"] / 2 + dx, r["y"] + r["h"] / 2 + dy
    t = np.linspace(-0.5, 0.5, n); gx, gy = np.meshgrid(t * r["w"] * fx, t * r["h"] * fy)
    u, v = warp_pts(Hm, cx + gx, cy + gy)
    return float(np.mean(1 - sample(g, u, v) / paper))


def ring_darkness(g, Hm, opts, dx, dy, paper):
    """Mean darkness on the printed outline of every option, shifted by (dx, dy) pt."""
    tot = 0.0
    for b in opts:
        cx, cy = b["x"] + b["w"] / 2 + dx, b["y"] + b["h"] / 2 + dy
        xs, ys = warp_pts(Hm, cx + b["_ring"][:, 0], cy + b["_ring"][:, 1])
        tot += float(np.mean(1 - sample(g, xs, ys) / paper))
    return tot / len(opts)


def snap(g, Hm, opts, paper, x0=0.0, y0=0.0, r=3.0):
    """Shift (pt) that puts the sampling rings on the printed oval outlines, searched around (x0, y0)."""
    best = (-1, x0, y0)
    for dx in np.arange(x0 - r, x0 + r + 0.01, 1.0):
        for dy in np.arange(y0 - r, y0 + r + 0.01, 1.0):
            v = ring_darkness(g, Hm, opts, dx, dy, paper)
            if v > best[0]: best = (v, dx, dy)
    _, bx, by = best
    for dx in np.arange(bx - 1, bx + 1.01, 0.25):
        for dy in np.arange(by - 1, by + 1.01, 0.25):
            v = ring_darkness(g, Hm, opts, dx, dy, paper)
            if v > best[0]: best = (v, dx, dy)
    return best[1], best[2]


def lock_mark(g, Hm, m, paper, r=6.0):
    """Vertical offset (pt) that centres the sampling window on a timing mark."""
    best = (-1, 0.0)
    for dy in np.arange(-r, r + 0.01, 0.5):
        v = rect_darkness(g, Hm, m, paper, 0.0, dy, 0.6, 1.0)
        if v > best[0]: best = (v, dy)
    return best[1], best[0]


def oval_darkness(g, Hm, b, paper, dx=0.0, dy=0.0):
    """Mean darkness inside a target (its inset interior samples), shifted by (dx, dy) pt."""
    cx, cy = b["x"] + b["w"] / 2 + dx, b["y"] + b["h"] / 2 + dy
    xs, ys = warp_pts(Hm, cx + b["_inner"][:, 0], cy + b["_inner"][:, 1])
    return float(np.clip(1 - sample(g, xs, ys).mean() / paper, 0, 1))


def orient(g, tpl, img_pts):
    """Homography for the page upright and turned 180 degrees; keep the one whose timing marks are dark."""
    src = [(f[0] + f[2] / 2, f[1] + f[2] / 2) for f in tpl["fiducials"]]
    tl, tr, bl, br = img_pts
    cands = [(0, homography(src, [tl, tr, bl, br])), (180, homography(src, [br, bl, tr, tl]))]
    marks = tpl.get("timing", [])
    def score(Hm):
        if marks:
            return np.mean([rect_darkness(g, Hm, m, 1.0, fx=0.5, fy=0.5, n=5) for m in marks])
        return np.mean([ring_darkness(g, Hm, f["options"], 0, 0, 1.0) for f in tpl["fields"] if f["type"] == "single"])
    scored = sorted(((score(Hm), deg, Hm) for deg, Hm in cands), key=lambda t: -t[0])
    return scored[0][1], scored[0][2], scored[0][0] - scored[1][0]


def decide(rel, tpl):
    """Fill above blank per option -> (index or None, status). Research D §5.3, tuned on test sheets:
    blank < light_min <= light < mark_min <= marked; two marks within `margin` -> MULTI;
    a read with a near rival (second >= mark_min, or within `margin`) -> check."""
    order = np.argsort(rel)[::-1]; top = rel[order[0]]; second = rel[order[1]] if len(rel) > 1 else 0.0
    if top < tpl.get("light_min", 0.15): return None, "blank"
    if top < tpl["mark_min"]: return None, "light"
    if second >= tpl["mark_min"] and top - second < tpl["margin"]: return None, "multi"
    if second >= tpl["mark_min"] or top - second < tpl["margin"]: return int(order[0]), "check"
    return int(order[0]), "ok"


def measure(path, tpl):
    """Straighten one page and measure every oval: returns (image, homography, turned, timing locks, per-field raw)."""
    g = load_gray(path)
    deg, Hm, confidence = orient(g, tpl, find_fiducials(g, tpl))
    px = np.array([p[0] for p in tpl["paper_probes"]], float); py = np.array([p[1] for p in tpl["paper_probes"]], float)
    paper = float(np.median(sample(g, *warp_pts(Hm, px, py))))
    marks = {m["id"]: m for m in tpl.get("timing", [])}
    locked = {k: lock_mark(g, Hm, m, paper) for k, m in marks.items()}
    raw = {}
    for f in tpl["fields"]:
        if f["type"] != "single": continue
        ms = [f["mark"]] if "mark" in f else f.get("marks", [])
        y0 = float(np.mean([locked[m][0] for m in ms if m in locked])) if any(m in locked for m in ms) else 0.0
        dx, dy = snap(g, Hm, f["options"], paper, 0.0, y0)
        raw[f["name"]] = (f, dx, dy, np.array([oval_darkness(g, Hm, o, paper, dx, dy) for o in f["options"]]))
    return g, Hm, deg, locked, raw


def read_page(path, tpl, out_dir, stem, cal=None):
    g, Hm, deg, locked, raw = measure(path, tpl)
    row = {"file": stem, "turned": deg}; flags = []; seen = {}
    if cal:  # each oval minus its own level on a blank printed sheet, then minus the sheet's overall shift
        resid = {n: d - cal[n] for n, (f, dx, dy, d) in raw.items()}
        shift = float(np.median(np.concatenate(list(resid.values()))))
        rels = {n: r - shift for n, r in resid.items()}
    else:    # each row minus its lightest oval, capped by the sheet's typical empty oval of that kind
        kinds = {}
        for f, dx, dy, d in raw.values(): kinds.setdefault(f.get("kind", "plain"), []).extend(d)
        blank = {k: float(np.median(v)) for k, v in kinds.items()}
        rels = {n: d - min(d.min(), blank[f.get("kind", "plain")] + 0.1) for n, (f, dx, dy, d) in raw.items()}
    for name, (f, dx, dy, d) in raw.items():
        rel = rels[name]; idx, status = decide(rel, tpl)
        row[name] = f["options"][idx]["value"] if idx is not None else ("MULTI" if status == "multi" else "")
        if status != "ok": flags.append(f"{name}:{status}")
        seen[name] = (f, dx, dy, rel, idx, status)
    found = sum(1 for dy, v in locked.values() if v >= tpl.get("mark_ok", 0.45))
    if found < len(locked): flags.append(f"marks:{found}/{len(locked)}")
    for c in tpl.get("compose", []):
        parts = [row.get(n, "") for n in c["from"]]
        row[c["name"]] = "".join(parts) if all(p not in ("", "MULTI") for p in parts) else ""
        if not row[c["name"]] and any(parts): flags.append(f"{c['name']}:incomplete")
    row["flags"] = " ".join(flags)
    # straightened write-in boxes
    cdir = os.path.join(out_dir, "crops"); os.makedirs(cdir, exist_ok=True)
    for f in tpl["fields"]:
        if f["type"] != "crop": continue
        ppt = 3.0; W = int(f["w"] * ppt); Hh = int(f["h"] * ppt)
        xs, ys = np.meshgrid(np.arange(W) / ppt + f["x"], np.arange(Hh) / ppt + f["y"])
        u, v = warp_pts(Hm, xs, ys)
        Image.fromarray((np.clip(sample(g, u, v).reshape(Hh, W), 0, 1) * 255).astype(np.uint8)).save(os.path.join(cdir, f"{stem}__{f['name']}.png"))
    proof(g, Hm, tpl, seen, locked, row, os.path.join(out_dir, "check"), stem)
    return row


def proof(g, Hm, tpl, seen, locked, row, cdir, stem, ppt=2.0, col=330):
    """Proof sheet: the straightened page in grey, read marks ringed blue, problems ringed red,
    and a read-out column on the right with each row's values at that row's height."""
    os.makedirs(cdir, exist_ok=True)
    W, H = int(tpl["page"]["w"] * ppt), int(tpl["page"]["h"] * ppt)
    xs, ys = np.meshgrid(np.arange(W) / ppt, np.arange(H) / ppt)
    u, v = warp_pts(Hm, xs, ys); page = sample(g, u, v).reshape(H, W)
    im = Image.new("RGB", (W + col, H), (255, 255, 255))
    im.paste(Image.fromarray((np.clip(0.55 + 0.45 * page, 0, 1) * 255).astype(np.uint8)).convert("RGB"), (0, 0)); d = ImageDraw.Draw(im)
    blue, red = (20, 70, 220), (215, 30, 30)
    for k, m in {m["id"]: m for m in tpl.get("timing", [])}.items():
        dy = locked[k][0]; d.rectangle([m["x"] * ppt, (m["y"] + dy) * ppt, (m["x"] + m["w"]) * ppt, (m["y"] + m["h"] + dy) * ppt], outline=blue, width=1)
    lines = {}
    for name, (f, dx, dy, rel, idx, status) in seen.items():
        c = blue if status == "ok" else red
        for i, o in enumerate(f["options"]):
            box = [(o["x"] + dx) * ppt, (o["y"] + dy) * ppt, (o["x"] + o["w"] + dx) * ppt, (o["y"] + o["h"] + dy) * ppt]
            if i == idx or (status in ("multi", "light", "check") and rel[i] >= tpl.get("light_min", 0.15)):
                poly = target_poly(o, tpl.get("shape"))
                if poly is None: d.ellipse([box[0] - 3, box[1] - 3, box[2] + 3, box[3] + 3], outline=c, width=3)
                else:
                    P = np.array(poly); C = P.mean(0); Q = C + (P - C) * 1.35  # the outline, grown so the ring sits outside the mark
                    d.line([((q[0] + dx) * ppt, (q[1] + dy) * ppt) for q in list(Q) + [Q[0]]], fill=c, width=3)
        y = int(np.mean([o["y"] + o["h"] / 2 for o in f["options"]]) * ppt) // 12 * 12
        shown = row[name] or {"blank": "(blank)", "multi": "MULTI", "light": "(light mark?)"}.get(status, "")
        lines.setdefault(y, []).append((f"{name} {shown}" + (f" ({status})" if status == "check" else ""), c))
    f13 = font(13)
    for y, items in lines.items():
        x = W + 10
        for txt, c in items:
            d.text((x, y - 8), txt, fill=c, font=f13); x += 12 + int(d.textlength(txt, font=f13))
    d.text((W + 10, 10), f"{stem} · turned {row['turned']}", fill=(0, 0, 0), font=f13)
    if row.get("flags"): d.text((W + 10, 30), "flags: " + row["flags"].replace(" ", "\n       "), fill=red, font=f13)
    im.save(os.path.join(cdir, f"{stem}__check.png"))


def pages(path, work):
    if not path.lower().endswith(".pdf"):
        yield path, os.path.splitext(os.path.basename(path))[0]; return
    stem = os.path.splitext(os.path.basename(path))[0]
    tool = next((t for t in (os.path.join(HERE, "render_pdf"), os.path.join(HERE, "..", "tools", "render_pdf")) if os.path.exists(t)), None)
    if tool is None:
        src = os.path.join(HERE, "render_pdf.swift")
        if not os.path.exists(src): raise RuntimeError("PDF input needs render_pdf; export the pages as PNG instead")
        tool = os.path.join(HERE, "render_pdf")
        subprocess.run(["swiftc", "-O", src, "-o", tool], check=True)  # one-time build, macOS with Xcode tools
    subprocess.run([tool, path, work, stem, "200"], check=True, capture_output=True)
    names = [n for n in os.listdir(work) if n.startswith(stem + "_p") and n.endswith(".png")]
    for name in sorted(names, key=lambda s: int(s[len(stem) + 2:-4])):
        yield os.path.join(work, name), os.path.splitext(name)[0]


def contact_sheets(tpl, rows, out_dir, ppt=3.0):
    """One image per write-in question: every page's answer stacked, labelled with its page, for reading and coding."""
    qdir = os.path.join(out_dir, "by_question"); os.makedirs(qdir, exist_ok=True)
    for f in tpl["fields"]:
        if f["type"] != "crop": continue
        tiles = [(r["file"], os.path.join(out_dir, "crops", f"{r['file']}__{f['name']}.png")) for r in rows]
        tiles = [(st, Image.open(p)) for st, p in tiles if os.path.exists(p)]
        if not tiles: continue
        lab = 170; W = lab + max(t.width for _, t in tiles); H = sum(t.height + 6 for _, t in tiles)
        sheet = Image.new("L", (W, H), 255); d = ImageDraw.Draw(sheet); y = 0; f14 = font(14)
        for st, t in tiles:
            sheet.paste(t, (lab, y)); d.text((8, y + 8), st, fill=0, font=f14); y += t.height + 6
            d.line([(0, y - 3), (W, y - 3)], fill=170)
        sheet.save(os.path.join(qdir, f"{f['name']}.png"))


def main():
    args = sys.argv[1:]; blank = None
    if "--blank" in args:
        i = args.index("--blank"); blank = args[i + 1]; del args[i:i + 2]
    tpl = prepare(json.load(open(args[0]))); out_csv, out_dir = args[1], args[2]; os.makedirs(out_dir, exist_ok=True)
    rows = []
    with tempfile.TemporaryDirectory() as work:
        cal = None
        if blank:  # a blank sheet from the same print run, scanned the same way: per-oval empty levels
            path, stem = next(pages(blank, work))
            cal = {n: d for n, (f, dx, dy, d) in measure(path, tpl)[4].items()}
        for src in args[3:]:
            for path, stem in pages(src, work):
                try:
                    rows.append(read_page(path, tpl, out_dir, stem, cal))
                except RuntimeError as e:
                    rows.append({"file": stem, "flags": f"unread: {e}"})
    names = ["file", "turned"] + [c["name"] for c in tpl.get("compose", [])] + [f["name"] for f in tpl["fields"] if f["type"] == "single"] + ["flags"]
    with open(out_csv, "w", newline="") as fh:
        w = csv.DictWriter(fh, fieldnames=names, extrasaction="ignore"); w.writeheader(); w.writerows(rows)
    contact_sheets(tpl, [r for r in rows if "unread" not in r.get("flags", "")], out_dir)
    bad = sum(1 for r in rows if r.get("flags"))
    print(f"read {len(rows)} pages -> {out_csv} ({bad} with flags; proofs in {os.path.join(out_dir, 'check')}, answers by question in {os.path.join(out_dir, 'by_question')})"
          + (" · calibrated on " + os.path.basename(blank) if blank else ""))


if __name__ == "__main__":
    main()
