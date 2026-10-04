#!/usr/bin/python3
"""glass_review_sheets.py -- review sheets for the GD3 pattern sign-off (plan 10 sec 5.2 step 2).

    /usr/bin/python3 glass_review_sheets.py --json-root J --render-root R --out O [--test-root T]

J/<profile>/<name>.json   crack_graph_ref.py lookdev output
R/<profile>/<name>_<stage>_<view>.png   glass_lookdev.py renders
Writes JPG q85 sheets into O (pattern_*.jpg):
  pattern_v2_<profile>.jpg          V2 sign-off: the real-glass checklist + reference ledger | 6 samples x
                                    (crack graph, 50 cm zoom of the web, S2 face-on crop, S2 at 45 deg)
  pattern_overview_<profile>.jpg    6 samples x (S1 C1, S2 C1, S2 C2, S4 C2)
  pattern_stages_<profile>_<hit>_s<seed>.jpg   one sample: every stage and view + its graph and numbers
  pattern_diagrams_<profile>.jpg    the 6 crack graphs, large
  pattern_decision1.jpg             Annealed6 vs Tempered6, same hits
  pattern_webgl_tier.jpg            Annealed6 (desktop High) vs Annealed6_WebGL crack graphs + S2
  pattern_bevel_ab.jpg              0.6 / 0.3 / 0.0 mm bevel crops (when --test-root has them)
Every sheet is at most 1920 px wide (Documentation/VERIFICATION_LOG.md sec 1.2).
Style: the step-1 sheets' palette; Bayon titles, IBM Plex Mono data, Source Serif 4 sentences (project fonts).
"""
import argparse
import json
import math
import os

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
FONT_DIR = os.path.normpath(os.path.join(HERE, "..", "..", "..", "Assets", "Resources", "Fonts"))   # Tools/Blender/glass
if not os.path.isdir(FONT_DIR):
    FONT_DIR = "/Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01 Individual Game Project/Frontrooms3D/Assets/Resources/Fonts"
MONO = os.path.join(FONT_DIR, "IBMPlexMono-Regular.ttf")
TITLE = os.path.join(FONT_DIR, "Bayon-Regular.ttf")
SERIF = os.path.join(FONT_DIR, "SourceSerif4-Variable.ttf")
BG = (21, 21, 18)
PANEL = (30, 30, 26)
FG = (226, 222, 205)
DIM = (150, 146, 128)
ACC = (232, 196, 92)
RED = (226, 110, 90)
BLUE = (120, 170, 220)
GREEN = (140, 200, 150)

COL_SHARD = (58, 66, 62)
COL_TOOTH = (150, 72, 58)
COL_CRUSH = (230, 230, 225)
COL_RADIAL = (236, 232, 214)
COL_FORK = (232, 196, 92)
COL_CHORD = (120, 170, 220)
COL_CUT = (120, 116, 104)
COL_S1 = (240, 110, 200)

T0 = 160                      # content starts here, below the header band
SW = 1920                     # sheet width
MG = 36                       # side margin
HITS = ("H1", "H2", "H3")
SEEDS = (4242, 1990)
HIT_NOTE = {"H1": "H1 eye-height centre (0, 1.62)", "H2": "H2 low right (+0.40, 0.90)",
            "H3": "H3 near the left jamb (-0.4835, 1.30)"}
VIEW_NOTE = {"C1": "C1 shot camera, 0.55 m, at the hit", "C2": "C2 45 deg, 1.5 m, lamp in the reflection",
             "C3": "C3 face-on 1.2 m", "C4": "C4 far side 2 m, floor"}


def font(path, size):
    try:
        return ImageFont.truetype(path, size)
    except Exception:                                   # noqa: BLE001
        return ImageFont.load_default()


def wrap(d, text, fnt, width):
    words = text.split()
    lines, cur = [], ""
    for w in words:
        t = (cur + " " + w).strip()
        if d.textlength(t, font=fnt) <= width:
            cur = t
        else:
            if cur:
                lines.append(cur)
            cur = w
    if cur:
        lines.append(cur)
    return lines


def text_block(d, xy, text, fnt, width, fill=FG, leading=1.12):
    x, y = xy
    size = fnt.size if hasattr(fnt, "size") else 14
    for para in text.split("\n"):
        for line in wrap(d, para, fnt, width):
            d.text((x, y), line, font=fnt, fill=fill)
            y += int(size * leading)
    return y


# ---------------------------------------------------------------------------------------------------------
# Crack-graph diagrams
# ---------------------------------------------------------------------------------------------------------
def diagram(res, W, H=None, centre=None, half=None, show=("pieces", "graph", "s1", "frame"), ss=3):
    """Draw a pattern. Full slab by default; with centre/half: a square zoom (metres) around a point."""
    w, h = res["slab"]["w"], res["slab"]["h"]
    if centre is None:
        H = H or int(W * h / w)
        x0, x1, y0, y1 = -w / 2, w / 2, -h / 2, h / 2
    else:
        H = H or W
        x0, x1 = centre[0] - half, centre[0] + half
        y0, y1 = centre[1] - half * H / W, centre[1] + half * H / W
    SW, SH = W * ss, H * ss
    im = Image.new("RGB", (SW, SH), PANEL)
    d = ImageDraw.Draw(im)
    sx = SW / (x1 - x0)

    def P(p):
        return ((p[0] - x0) * sx, (y1 - p[1]) * sx)

    lw = max(1, int(ss * (1.0 if centre is None else 1.6)))
    if "pieces" in show:
        for pc in res["pieces"]:
            col = {"shard": COL_SHARD, "tooth": COL_TOOTH, "crush": COL_CRUSH, "clump": COL_SHARD}.get(pc["kind"], COL_SHARD)
            if pc["kind"] == "shard":
                k = 0.88 + 0.12 * ((pc["id"] * 37) % 7) / 6.0
                col = tuple(int(c * k) for c in col)
            d.polygon([P(p) for p in pc["poly"]], fill=col, outline=COL_CUT)
    if "graph" in show and res.get("graph"):
        for t in res["graph"]["tracks"]:
            col = COL_RADIAL if t["initial"] else COL_FORK
            d.line([P(p) for p in t["points"]], fill=col, width=lw)
        for c in res["graph"]["chords"]:
            d.line([P(c["p"][0]), P(c["p"][1])], fill=COL_CHORD, width=lw)
        if centre is not None:
            r = 2.2 * ss
            for c in res["graph"]["chords"]:
                for p in c["p"]:
                    q = P(p)
                    d.ellipse([q[0] - r, q[1] - r, q[0] + r, q[1] + r], fill=COL_CHORD)
            for f in res["graph"]["forks"]:
                q = P(f["at"])
                r2 = 4.5 * ss
                d.polygon([(q[0], q[1] - r2), (q[0] - r2, q[1] + r2 * 0.8), (q[0] + r2, q[1] + r2 * 0.8)], outline=COL_FORK,
                          width=max(1, ss))
    if "s1" in show and res.get("stage1"):
        for c in res["stage1"]["cracks"]:
            d.line([P(p) for p in c["points"]], fill=COL_S1, width=lw * 2)
    if "frame" in show:
        st, cz = res["frame"]["stop"], res["frame"]["clear"]
        for (a, b, c_, e) in ((st["x0"], st["y0"], st["x1"], st["y1"]),):
            d.rectangle([P((a, e)), P((c_, b))], outline=DIM, width=max(1, ss))
        # dashed clear-zone boundary
        pts = [(cz["x0"], cz["y0"]), (cz["x1"], cz["y0"]), (cz["x1"], cz["y1"]), (cz["x0"], cz["y1"])]
        for i in range(4):
            p, q = pts[i], pts[(i + 1) % 4]
            L = math.hypot(q[0] - p[0], q[1] - p[1])
            n = max(1, int(L / 0.02))
            for k in range(0, n, 2):
                a = (p[0] + (q[0] - p[0]) * k / n, p[1] + (q[1] - p[1]) * k / n)
                b = (p[0] + (q[0] - p[0]) * (k + 1) / n, p[1] + (q[1] - p[1]) * (k + 1) / n)
                d.line([P(a), P(b)], fill=RED, width=max(1, ss))
    ix, iy = res["impact"]
    q = P((ix, iy))
    r = 3 * ss
    d.ellipse([q[0] - r, q[1] - r, q[0] + r, q[1] + r], outline=RED, width=max(1, ss))
    return im.resize((W, H), Image.LANCZOS)


def legend(d, x, y, fs):
    items = [(COL_RADIAL, "first radials"), (COL_FORK, "forks (branch outward)"), (COL_CHORD, "ring chords, T-ends"),
             (COL_S1, "Crack1 fins (S1)"), (COL_TOOTH, "teeth (stay in the stop)"), (COL_CRUSH, "crushed core"),
             (RED, "clear zone (dashed) / impact")]
    for col, label in items:
        d.rectangle([x, y + 4, x + 22, y + 14], fill=col)
        d.text((x + 30, y), label, font=fs, fill=FG)
        y += 22
    return y


# ---------------------------------------------------------------------------------------------------------
# Render helpers
# ---------------------------------------------------------------------------------------------------------
def load_render(root, profile, name, stage, view):
    p = os.path.join(root, profile, "%s_%s_%s.png" % (name, stage, view))
    if not os.path.exists(p):
        return None
    return Image.open(p).convert("RGB")


def fit(im, W, H):
    if im is None:
        ph = Image.new("RGB", (W, H), PANEL)
        ImageDraw.Draw(ph).text((10, 10), "missing", font=font(MONO, 14), fill=RED)
        return ph
    return im.resize((W, H), Image.LANCZOS)


def centre_crop(im, frac_w, frac_h, cx=0.5, cy=0.5):
    if im is None:
        return None
    w, h = im.size
    cw, ch = int(w * frac_w), int(h * frac_h)
    x0 = int(min(max(cx * w - cw / 2, 0), w - cw))
    y0 = int(min(max(cy * h - ch / 2, 0), h - ch))
    return im.crop((x0, y0, x0 + cw, y0 + ch))


def header(d, W, title, sub, ft, fm):
    """Title (Bayon has no middle dot: ' / ' separates) and a one-paragraph subtitle below its real ink box."""
    title = title.replace(" · ", "  /  ")
    d.text((24, 14), title, font=ft, fill=FG)
    bottom = d.textbbox((24, 14), title, font=ft)[3]
    end = text_block(d, (26, bottom + 10), sub, fm, W - 60, DIM, 1.25)
    if end > T0 - 26:
        print("[sheets] warning: header text ends at y %d, content starts at %d:" % (end, T0), title)
    return end


def save(im, path):
    im.save(path, "JPEG", quality=85, optimize=True)
    print("wrote", path, im.size)


def cases(json_root, profile):
    out = []
    for hit in HITS:
        for seed in SEEDS:
            name = "%s_%s_s%d" % (profile, hit, seed)
            p = os.path.join(json_root, profile, name + ".json")
            if os.path.exists(p):
                out.append((hit, seed, name, json.load(open(p))))
    return out


# ---------------------------------------------------------------------------------------------------------
# Sheets
# ---------------------------------------------------------------------------------------------------------
V2_ITEMS = [
    ("Radials first, near-straight",
     "SWGMAT 2004: a point impact on a pane held on all sides makes radial cracks first; they run out from the impact "
     "as nearly straight lines.",
     "8-12 first radials per break, straight between junctions, run to the frame."),
    ("Kinked, not wavy",
     "Radial cracks deflect a little where other cracks meet them; they do not wave like a sine curve.",
     "Kinks only at T-junctions: about 2 deg each (U 0.5-1.5 x 2 deg), 15 % of junctions 4-7 deg; never at a free point."),
    ("Forking outward",
     "Fineberg and Marder 1999: a fast crack branches; more energy, more branches. Branches open away from the impact.",
     "Y-forks from ring 2 outward, p 0.30 in wedges over 0.3 rad, up to p 0.85 where a wedge is over 16 cm wide."),
    ("Ring cracks are straight chords",
     "SWGMAT 2004: concentric cracks form between the radials as mostly straight segments, not circles.",
     "One straight chord per wedge and ring; p 0.97 near the impact falling to 0.45 at the frame (missing chords = daggers)."),
    ("Chords end in T's on the radials",
     "SWGMAT 2004: later cracks end at earlier ones; a ring segment stops on the radial it meets.",
     "Each chord end is its own point on the radial (at least max(4 mm, 5 % of r) from the other side's end): no '+' crossings."),
    ("Crushed spot, no star",
     "A hard hit leaves a small crushed, whitish crater with flakes missing (SWGMAT 2004; report 02 sec 2.2).",
     "Crush core: 10 mm +-30 %, kept convex, capped at 14.5 mm (12.6-12.8 mm in these samples; V5 <= 15 mm). It "
     "becomes powder and the crater at Crack1 and falls out at Crack2."),
    ("Teeth in the stop, long daggers",
     "Annealed glass leaves irregular sharp pieces; pieces held by the stop stay as teeth (report 02 sec 2.3).",
     "Teeth are a subset of the frame pieces, clipped to the tooth band (8.35 cm jambs/head, 2.35 cm sill, visible)."),
]

REFS = [
    ("Forensic radial + concentric fractures", "https://commons.wikimedia.org/wiki/File:Glass_fracture_radial_concentric.jpg"),
    ("Broken house window, West Midlands Police (CC BY-SA 2.0)", "https://commons.wikimedia.org/wiki/File:Day_195_-_Shut_the_burglar_out_(9293361460).jpg"),
    ("Broken window, Helgi Halldorsson (CC BY-SA 2.0)", "https://commons.wikimedia.org/wiki/File:Broken_Window_(3512969871).jpg"),
    ("Window broken in two parts: a low-energy break (CC BY-SA 3.0)", "https://commons.wikimedia.org/wiki/File:Broken_glas.JPG"),
    ("Broken window, Heimaey, Hannes Grobe (CC BY-SA 4.0)", "https://commons.wikimedia.org/wiki/File:Broek-window-on-heimaey_hg.jpg"),
    ("Cracked pane above an altar (geograph)", "https://commons.wikimedia.org/wiki/File:Cracked_pane_of_glass_above_the_altar_-_geograph.org.uk_-_7461197.jpg"),
    ("Tempered pane crazed in its frame (CC BY-SA 3.0)", "https://commons.wikimedia.org/wiki/File:Broken_Meat_Case_Window.JPG"),
    ("Tempered fragments (CC BY-SA 3.0)", "https://commons.wikimedia.org/wiki/File:Tempered_Glass.jpg"),
    ("Green colour of float glass", "https://commons.wikimedia.org/wiki/File:Green_color_of_float_glass.jpg"),
    ("SWGMAT, Glass Fractures (2004, NIST)", "https://www.nist.gov/document/glassfracturespdf"),
]


def _verdict_col(v):
    return GREEN if v.startswith("PASS") else (ACC if v.startswith(("FLAG", "PARTIAL")) else RED)


def sheet_v2(json_root, render_root, out, profile, verdicts):
    """V2 sign-off, 1920 wide: the checklist (real glass vs ours, verdict) on top, 6 samples (graph, 50 cm zoom of the
    web, S2 face-on crop) below, the reference ledger at the bottom."""
    cs = cases(json_root, profile)
    gap = 20
    colw = (SW - 2 * MG - 2 * gap) // 3                     # 602
    dgw = 268
    dgh = int(dgw * 1.642 / 1.391)                          # 316
    zw = colw - dgw - 14                                    # 320
    crop_h = int(colw * 9 / 16)
    block_h = 64 + dgh + 12 + crop_h + 24
    fr, fs, fm = font(SERIF, 19), font(MONO, 13), font(MONO, 16)
    # measure the checklist
    tmp = ImageDraw.Draw(Image.new("RGB", (10, 10)))
    half = (SW - 2 * MG - 28) // 2

    def item_h(real, ours):
        return 36 + len(wrap(tmp, "Real glass: " + real, fr, half - 20)) * int(19 * 1.15) + \
            len(wrap(tmp, "Ours: " + ours, fs, half - 20)) * int(13 * 1.35) + 18
    hs = [item_h(r, o) for (_, r, o) in V2_ITEMS]
    split = (len(V2_ITEMS) + 1) // 2
    list_h = max(sum(hs[:split]), sum(hs[split:]))
    refs_h = 40 + ((len(REFS) + 1) // 2) * 46 + 170
    H = T0 + list_h + 30 + 2 * block_h + refs_h
    im = Image.new("RGB", (SW, H), BG)
    d = ImageDraw.Draw(im)
    header(d, SW, "GD3 · PATTERN SIGN-OFF · V2 · " + profile.upper(),
           "Plan 10 sec 5.1 V2: radials near-straight, kinked, forking outward; ring cracks are chords ending in T's on "
           "radials. Pattern from crack_graph_ref.py, renders from glass_lookdev.py (Cycles). Reference photos are listed, "
           "not downloaded (media_candidates.md asks Red).", font(TITLE, 48), fm)
    # checklist, two columns
    for colno, rng_ in enumerate((range(0, split), range(split, len(V2_ITEMS)))):
        x = MG + colno * (half + 28)
        y = T0
        for i in rng_:
            title, real, ours = V2_ITEMS[i]
            d.text((x, y), "%d  %s" % (i + 1, title.upper()), font=font(TITLE, 28), fill=ACC)
            v = verdicts.get(title, "")
            if v:
                tw_ = d.textlength(v, font=font(MONO, 18))
                d.text((x + half - 20 - tw_, y + 6), v, font=font(MONO, 18), fill=_verdict_col(v))
            yy = text_block(d, (x + 14, y + 36), "Real glass: " + real, fr, half - 20, FG, 1.15)
            text_block(d, (x + 14, yy), "Ours: " + ours, fs, half - 20, DIM, 1.35)
            y += hs[i]
    # samples: 3 columns x 2 rows
    y0 = T0 + list_h + 30
    labels = ("CRACK GRAPH", "50 CM AROUND THE IMPACT", "S2 (0.70 S) FACE-ON AT 0.55 M, CROP AROUND THE IMPACT")
    for k, (hit, seed, name, res) in enumerate(cs):
        x = MG + (k % 3) * (colw + gap)
        y = y0 + (k // 3) * block_h
        st = res["stats"]
        d.text((x, y), "%s  SEED %d" % (hit, seed), font=font(TITLE, 28), fill=FG)
        d.text((x, y + 32), "%d pieces: %d shards, %d teeth  |  %d tracks, %d forks, %d chords, '+' %d" % (
            st["pieces_total"], st["pieces"]["shard"], st["pieces"]["tooth"], st["tracks_total"], st["forks"],
            st["chords"], st["plus_junctions"]), font=fs, fill=DIM)
        yy = y + 56
        im.paste(diagram(res, dgw, dgh), (x, yy))
        im.paste(diagram(res, zw, zw, centre=res["impact"], half=0.25), (x + dgw + 14, yy))
        d.text((x + dgw + 14, yy + zw + 4), labels[1], font=fs, fill=DIM)
        yy += dgh + 12
        r = load_render(render_root, profile, name, "S2", "C1")
        im.paste(fit(centre_crop(r, 0.42, 0.42), colw, crop_h), (x, yy))       # C1 looks at the hit: it is the centre
    # references + legend
    y = y0 + 2 * block_h
    d.text((MG, y), "REFERENCES (REPORT 02 SEC 6; NOT DOWNLOADED, NOT VIEWED)", font=font(TITLE, 26), fill=ACC)
    y += 40
    for i, (label, url) in enumerate(REFS):
        x = MG + (i % 2) * (half + 28)
        yy = y + (i // 2) * 46
        d.text((x, yy), label, font=fs, fill=FG)
        d.text((x, yy + 18), url, font=font(MONO, 11), fill=DIM)
    legend(d, MG, y + ((len(REFS) + 1) // 2) * 46 + 10, fs)
    save(im, os.path.join(out, "pattern_v2_%s.jpg" % profile))


def sheet_overview(json_root, render_root, out, profile):
    cs = cases(json_root, profile)
    cols = [("S1", "C1"), ("S2", "C1"), ("S2", "C2"), ("S4", "C2"), ("S4", "C4")]
    if profile.startswith("Tempered"):
        cols = [("S1", "C1"), ("S1", "C2"), ("S4", "C1"), ("S4", "C2"), ("S4", "C4")]
    labw, g = 150, 8
    tw = (SW - 2 * MG - labw - 4 * g) // 5
    th = int(tw * 9 / 16)
    H = T0 + 34 + len(cs) * (th + g) + 20
    im = Image.new("RGB", (SW, H), BG)
    d = ImageDraw.Draw(im)
    fs = font(MONO, 13)
    header(d, SW, "GD3 · PATTERN LOOK-DEV · " + profile.upper(),
           "Cycles, IOR 1.52 glass with iron-green absorption; W-L0 window in a Level 0 room; troffers in the reflection; "
           "dim tall hall beyond. S1 = 0.35 s, S2 = 0.70 s, S4 = settled." + (
               " Tempered has no crack stage, so S2 = S1." if profile.startswith("Tempered") else ""), font(TITLE, 48), font(MONO, 16))
    for j, (stg, view) in enumerate(cols):
        d.text((MG + labw + j * (tw + g), T0), "%s  %s" % (stg, VIEW_NOTE[view].split(",")[0]), font=fs, fill=ACC)
    y = T0 + 26
    for (hit, seed, name, res) in cs:
        d.text((MG, y + 4), "%s  S%d" % (hit, seed), font=font(TITLE, 26), fill=FG)
        st = res["stats"]
        txt = HIT_NOTE[hit].split(" ", 1)[1] + "\n" + (("%d clumps" % st["pieces_total"]) if res.get("mode") == "dice" else
                                                       "%d pieces\n%d teeth" % (st["pieces_total"], st["pieces"]["tooth"]))
        text_block(d, (MG, y + 38), txt, fs, labw - 10, DIM, 1.3)
        for j, (stg, view) in enumerate(cols):
            im.paste(fit(load_render(render_root, profile, name, stg, view), tw, th), (MG + labw + j * (tw + g), y))
        y += th + g
    save(im, os.path.join(out, "pattern_overview_%s.jpg" % profile))


def sheet_stages(json_root, render_root, out, profile):
    """One sample: rows S1 / S2 / S4, columns C1 / C2 / C4; the free cells hold the numbers and the crack graph."""
    for (hit, seed, name, res) in cases(json_root, profile):
        dice = res.get("mode") == "dice"
        g = 12
        tw = (SW - 2 * MG - 2 * g) // 3
        th = int(tw * 9 / 16)
        H = T0 + 3 * (th + 30) + 10
        im = Image.new("RGB", (SW, H), BG)
        d = ImageDraw.Draw(im)
        fs = font(MONO, 14)
        header(d, SW, "GD3 · %s · %s · SEED %d" % (profile.upper(), hit, seed),
               HIT_NOTE[hit] + ". Rows: S1 0.35 s, S2 0.70 s, S4 settled. Columns: " +
               "; ".join(VIEW_NOTE[v] for v in ("C1", "C2", "C4")) + ".", font(TITLE, 48), font(MONO, 16))
        grid = [[("S1", "C1"), ("S1", "C2"), "numbers"], [("S2", "C1"), ("S2", "C2"), "graph"],
                [("S4", "C1"), ("S4", "C2"), ("S4", "C4")]]
        if dice:
            grid[1] = [("S1", "C1"), ("S1", "C2"), "graph"]
        for r, row in enumerate(grid):
            y = T0 + r * (th + 30)
            for c, cell in enumerate(row):
                x = MG + c * (tw + g)
                if cell == "numbers":
                    st = res["stats"]
                    lines = [res["profile"] + " (" + res.get("glass", "") + ")",
                             "seed %d, rotation %.1f deg, side %+d" % (res["seed"], res["rotation_deg"], res["side"]),
                             "impact root (%.4f, %.4f), impactUV (%.3f, %.3f)" % (tuple(res["impact_root"]) + tuple(res["impact_uv"]))]
                    if dice:
                        lines += ["%d clumps, ~%d granules of 9 mm" % (st["pieces_total"], st["granules_estimate"]),
                                  "no crack stage; no teeth (the frame empties)"]
                    else:
                        lines += ["pieces %d: %d shards (bodies), %d teeth, %d crush core" % (
                                      st["pieces_total"], st["pieces"]["shard"], st["pieces"]["tooth"], st["pieces"]["crush"]),
                                  "tracks %d (%d first radials + %d forks)" % (st["tracks_total"], st["tracks_initial"], st["forks"]),
                                  "ring chords %d; T-ends %d; '+' junctions %d" % (st["chords"], st["t_junctions"], st["plus_junctions"]),
                                  "shard area p10/50/90: %.1f / %.1f / %.1f cm2" % tuple(st["shard_area_cm2_p10_p50_p90"]),
                                  "longest piece %.2f m" % st["max_piece_len"],
                                  "S2 mesh %d tris; teeth %d tris" % (st["tris_stage2"], st["tris_teeth"]),
                                  "Crack1: %d fins (%d tris); crater %d tris, r %.1f mm" % (
                                      st["stage1_cracks"], st["tris_fins"], st["tris_crater"], res["stage1"]["crater"]["radius"] * 1000),
                                  "level-2 cells %d; generator attempts %d" % (st["level2_cells"], res["attempts"])]
                    yy = y + 6
                    for l in lines:
                        d.text((x, yy), l, font=fs, fill=FG)
                        yy += 21
                    continue
                if cell == "graph":
                    dh = th
                    dw = int(dh * 1.391 / 1.642)
                    im.paste(diagram(res, dw, dh), (x, y + 20))
                    if not dice:
                        zw = min(tw - dw - 12, th)
                        im.paste(diagram(res, zw, zw, centre=res["impact"], half=0.25), (x + dw + 12, y + 20))
                    d.text((x, y), "CRACK GRAPH" + ("" if dice else "  |  50 CM AROUND THE IMPACT"), font=fs, fill=ACC)
                    continue
                stg, view = cell
                lbl = "%s %s" % (stg, view) + ("  (= S1: tempered has no crack stage)" if dice and r == 1 else "")
                d.text((x, y), lbl, font=fs, fill=ACC)
                im.paste(fit(load_render(render_root, profile, name, stg, view), tw, th), (x, y + 20))
        save(im, os.path.join(out, "pattern_stages_%s_%s_s%d.jpg" % (profile, hit, seed)))


def sheet_diagrams(json_root, out, profile):
    cs = cases(json_root, profile)
    dw = 600
    dh = int(dw * 1.642 / 1.391)
    W = 3 * (dw + 16) + 40
    H = T0 + 10 + 2 * (dh + 60) + 200
    im = Image.new("RGB", (W, H), BG)
    d = ImageDraw.Draw(im)
    ft, fm, fs = font(TITLE, 52), font(MONO, 17), font(MONO, 14)
    header(d, W, "GD3 · CRACK GRAPHS · " + profile.upper(),
           "Slab 1.391 x 1.642 m (window root, seen from the breaker's side). Grey box = stop line; red dashes = clear zone.", ft, fm)
    for i, (hit, seed, name, res) in enumerate(cs):
        x = 20 + (i % 3) * (dw + 16)
        y = T0 + (i // 3) * (dh + 60)
        st = res["stats"]
        d.text((x, y), "%s  seed %d   %d pieces  %d teeth" % (hit, seed, st["pieces_total"], st["pieces"].get("tooth", 0)), font=fm, fill=FG)
        im.paste(diagram(res, dw, dh), (x, y + 26))
    legend(d, 24, H - 180, fs)
    save(im, os.path.join(out, "pattern_diagrams_%s.jpg" % profile))


def sheet_decision1(json_root, render_root, out):
    rows = [("Annealed6", "6 mm annealed float (recommended): staged cracks, teeth stay"),
            ("Tempered6", "6 mm tempered: no crack stage; the pane dices at once into ~9 mm granules; the frame empties")]
    cols = [("S1", "C1"), ("S2", "C1"), ("S4", "C2"), ("S4", "C4")]
    hit, seed = "H1", 4242
    g = 10
    tw = (SW - 2 * MG - 3 * g) // 4
    th = int(tw * 9 / 16)
    H = T0 + 2 * (th + 80) + 10
    im = Image.new("RGB", (SW, H), BG)
    d = ImageDraw.Draw(im)
    fm, fs = font(MONO, 16), font(MONO, 13)
    header(d, SW, "GD3 · DECISION 1 · GLASS TYPE",
           "Same hit (H1, seed 4242), same room. Columns: S1 0.35 s face-on; S2 0.70 s face-on; S4 settled at 45 deg; "
           "S4 far side.", font(TITLE, 48), fm)
    y = T0
    for prof, note in rows:
        d.text((MG, y), prof.upper(), font=font(TITLE, 30), fill=ACC)
        d.text((MG + 190, y + 10), note, font=fm, fill=FG)
        y += 44
        name = "%s_%s_s%d" % (prof, hit, seed)
        for j, (stg, view) in enumerate(cols):
            s_ = "S1" if prof.startswith("Tempered") and stg == "S2" else stg
            x = MG + j * (tw + g)
            im.paste(fit(load_render(render_root, prof, name, s_, view), tw, th), (x, y + 18))
            d.text((x, y), "%s %s" % (stg, view), font=fs, fill=DIM)
        y += th + 36
    save(im, os.path.join(out, "pattern_decision1.jpg"))


def sheet_webgl(json_root, render_root, out):
    hi = cases(json_root, "Annealed6")
    lo = cases(json_root, "Annealed6_WebGL")
    g = 10
    dw = (SW - 2 * MG - 5 * g) // 6
    dh = int(dw * 1.642 / 1.391)
    tw = (SW - 2 * MG - g) // 2
    th = int(tw * 9 / 16)
    H = T0 + 2 * (dh + 44) + th + 60
    im = Image.new("RGB", (SW, H), BG)
    d = ImageDraw.Draw(im)
    fs = font(MONO, 13)
    header(d, SW, "GD3 · TIERS · DESKTOP HIGH VS WEBGL",
           "Same generator, seeds and hits. Desktop High: <= 150 pieces, 0.3 mm bevel. WebGL: <= 64 pieces and <= 40 "
           "moving bodies, no bevel (plan sec 4.6, 4.8).", font(TITLE, 48), font(MONO, 16))
    for r, (cs, label) in enumerate(((hi, "DESKTOP"), (lo, "WEBGL"))):
        y = T0 + r * (dh + 44)
        for i, (hit, seed, name, res) in enumerate(cs):
            x = MG + i * (dw + g)
            st = res["stats"]
            d.text((x, y), "%s %s s%d: %d pcs, %d bodies" % (label, hit, seed, st["pieces_total"], st["bodies"]), font=fs, fill=FG)
            im.paste(diagram(res, dw, dh), (x, y + 20))
    y = T0 + 2 * (dh + 44)
    for j, (prof, name) in enumerate((("Annealed6", "Annealed6_H1_s4242"), ("Annealed6_WebGL", "Annealed6_WebGL_H1_s4242"))):
        x = MG + j * (tw + g)
        d.text((x, y), prof + "  S2 C1 (H1, seed 4242)", font=fs, fill=ACC)
        im.paste(fit(load_render(render_root, prof, name, "S2", "C1"), tw, th), (x, y + 20))
    save(im, os.path.join(out, "pattern_webgl_tier.jpg"))


def sheet_bevel(test_root, out):
    items = [("bev_0.0006_S2_C1.png", "0.6 MM BEVEL (PLAN)"), ("bev_0.0003_S2_C1.png", "0.3 MM BEVEL (CHOSEN)"),
             ("bev_0.0_S2_C1.png", "0 MM: SHARP EDGES")]
    ims = []
    for fn, label in items:
        p = os.path.join(test_root, fn)
        if os.path.exists(p):
            ims.append((Image.open(p).convert("RGB"), label))
    if len(ims) < 2:
        return
    cw, ch = 600, 500
    W = len(ims) * (cw + 12) + 30
    H = T0 + 30 + ch + 40
    im = Image.new("RGB", (W, H), BG)
    d = ImageDraw.Draw(im)
    ft, fm = font(TITLE, 52), font(MONO, 17)
    header(d, W, "GD3 · CRACK EDGE A/B · S2 FACE-ON, 100 % PIXELS",
           "H1 seed 4242, C1 (0.55 m) at 1920x1080, 600x500 px around the impact at 100 %. Fracture faces rough 0.04-0.45 "
           "(mirror to hackle). At 0.55 m one pixel is 0.79 mm, so a 0.6 mm bevel on both pieces draws a dark 1.5 px outline.", ft, fm)
    for i, (img, label) in enumerate(ims):
        w, h = img.size
        crop = img.crop((w // 2 - cw // 2, h // 2 - ch // 2, w // 2 + cw // 2, h // 2 + ch // 2))
        x = 15 + i * (cw + 12)
        d.text((x, T0 - 6), label, font=font(TITLE, 26), fill=ACC)
        im.paste(crop, (x, T0 + 28))
    save(im, os.path.join(out, "pattern_bevel_ab.jpg"))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--json-root", required=True)
    ap.add_argument("--render-root", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--test-root", default=None)
    ap.add_argument("--verdicts", default=None, help="JSON {V2 item title: 'PASS ...'}")
    a = ap.parse_args()
    os.makedirs(a.out, exist_ok=True)
    verdicts = json.load(open(a.verdicts)) if a.verdicts else {}
    sheet_v2(a.json_root, a.render_root, a.out, "Annealed6", verdicts)
    for prof in ("Annealed6", "Tempered6"):
        sheet_overview(a.json_root, a.render_root, a.out, prof)
        sheet_stages(a.json_root, a.render_root, a.out, prof)
    sheet_diagrams(a.json_root, a.out, "Annealed6")
    sheet_diagrams(a.json_root, a.out, "Annealed6_WebGL")
    sheet_decision1(a.json_root, a.render_root, a.out)
    sheet_webgl(a.json_root, a.render_root, a.out)
    if a.test_root:
        sheet_bevel(a.test_root, a.out)


if __name__ == "__main__":
    main()
