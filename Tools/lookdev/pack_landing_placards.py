#!/usr/bin/env python3
"""Pack the stairwell-landing copies of the evacuation plan (BS6, 游戏视觉 2026-10-31; token patches, Red 10-31 "(c)").

  python3 Tools/lookdev/pack_landing_placards.py [--check]

BS6 (research/tutorial/20_requests.md, 教程设计's row, "V6 now"): on every floor below LEVEL 0 a copy of the plan hangs on
the map side of the stairwell's far door, at P1 west (FrontRoomsLandingPlacard). Each floor hangs the sheet for its own
state (叙事 research/narrative/40_floors_exit_tutorial_strings.md §3, A35): the full sheet (sheetA_B<n>: the security desk,
the padlocked stair door) when the floor has its own station and a locked EXIT, the Phase 0 sheet (sheetA_phase0_B<n>)
without. B10 and deeper share LEVEL B10+ (sheetA_B10plus, sheetA_phase0_B10plus). The art is 平面视觉's
(Tools/print/ink/art_from_graphic/placard/, BS8 of 2026-10-30, README "Per-floor sheets"), used as delivered.

Token patches (Red 2026-10-31, option (c) of 60_landing_copies_bs6.md §3): the 20 packed prints differ only where the
floor's token is printed (the title band's LEVEL B<n> and the title block's), and those texels are the same in a token's
full and Phase 0 sheets. So two base prints ship (each variant's LEVEL B1 sheet, byte for byte its packed print) plus,
for B2 .. B9 and B10+, the texels that differ from B1 as small patches, shared by both variants. FrontRoomsLandingPlacard
copies the floor's patches over the base once per floor. Each patch rect is the token's differing texels (both variants)
grown out to the PATCH_GRID (128 texels: on the 4 x 4 block grid down to mip 5, and the patch's own box mips equal the
sheet's there) and cut into power-of-two pieces (Unity 6 keeps a non-power-of-two texture with mips uncompressed).
The tool proves, pixel for pixel, that the base with a token's patches IS that token's packed print, in both variants.

What it writes (from main's tree, run in a clone; LF, UTF-8):
  Assets/Resources/Surfaces/Textures/Prop_EvacPlan_A_Landing.png          the full sheets' base (sheetA_B1's packed print)
  Assets/Resources/Surfaces/Textures/Prop_EvacPlan_A_LandingPhase0.png    the Phase 0 sheets' base (sheetA_phase0_B1's)
  Assets/Resources/Surfaces/Textures/Prop_EvacPlan_A_Landing_<T>_<Band><i>.png   T = B2 .. B9, B10plus; Band = Title (the
                                                                          title band) or Block (the title block); i = piece
  Assets/Resources/Surfaces/Textures/Prop_EvacPlan_Locked_E.png           the full sheets' one glow mask (9d3a9480, every floor)
  Assets/Scripts/Rendering/FrontRoomsLandingPlacardSheets.cs               the sheet table: base, patches + rects, glow,
                                                                          transcripts, regions
and a .meta beside each new texture when it has none (Prop_EvacPlan_A.png.meta / Prop_EvacPlan_E.png.meta as the
template, the GUID an md5 of the asset path; an existing .meta is never rewritten).
The 20 per-floor prints (Prop_EvacPlan_A_<T>[Phase0].png) are no longer written; --check reports any still in the folder.
The Phase 0 sheets' glow mask (ed18539e, every floor) is the one main already ships as Prop_EvacPlan_E.png, packed from
sheetA_phase0: the tool checks that packing a Phase 0 mask reproduces it byte for byte, and never writes it.

Every print and mask goes through Tools/lookdev/pack_evac_plan.py unchanged (2592 x 1674 -> 4096 x 2048, linear-light
Lanczos, its round-trip check), one folder at a time, into a scratch folder. The transcripts come from each folder's
examine.json through Tools/lookdev/inspect_transcript.py's transcript() (its region heads; byte for byte otherwise).
Glow k stays FrontRoomsPhosphor.PeakEmission (0.158): no code here touches it.

--check  writes nothing: packs and generates into a scratch folder and compares with the tree; exit 1 on any difference.
Stops (nothing written) if a folder is missing, a mask is not its variant's one mask, an examine.json does not split, a
token differs from B1 in other than two bands, a token's patch texels differ between its full and Phase 0 sheets, or a
base with its patches is not the token's print.
"""
import hashlib
import json
import os
import shutil
import subprocess
import sys
import tempfile

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
ART = os.path.join(ROOT, "Tools", "print", "ink", "art_from_graphic", "placard")
TEX = os.path.join(ROOT, "Assets", "Resources", "Surfaces", "Textures")
CS = os.path.join(ROOT, "Assets", "Scripts", "Rendering", "FrontRoomsLandingPlacardSheets.cs")
PACK = os.path.join(HERE, "pack_evac_plan.py")
sys.path.insert(0, HERE)
import inspect_transcript  # noqa: E402  (the region heads: transcript())

TOKENS = ["B1", "B2", "B3", "B4", "B5", "B6", "B7", "B8", "B9", "B10plus"]
BASE_TOKEN = "B1"                               # the bases are B1's prints: B1 needs no patch
# One glow mask per variant (平面视觉's README, BS8): every full sheet, every Phase 0 sheet.
MASK_MD5 = {True: "9d3a9480", False: "ed18539e"}
SHARED_PHASE0_GLOW = "Prop_EvacPlan_E"          # main's, packed from sheetA_phase0 (same mask); never written here
LOCKED_GLOW = "Prop_EvacPlan_Locked_E"          # ends in _E: FrontRoomsSurfaceTextureImporter imports it linear, BC1
REGION_ORDER = ["plan", "legend", "alarm", "titleblock"]
PRINT_W, PRINT_H = 4096, 2048                   # pack_evac_plan.py's texture
PATCH_GRID = 128                                # 4 x 4 blocks down to mip 5 (4 << 5); box mips stay inside a piece there
BANDS = ["Title", "Block"]                      # top to bottom: the title band, the title block


def stop(msg):
    sys.exit("pack_landing_placards: STOP: " + msg)


def md5(path):
    with open(path, "rb") as f:
        return hashlib.md5(f.read()).hexdigest()


def folder(token, full):
    return ("sheetA_" if full else "sheetA_phase0_") + token


def packed_name(token, full):
    """The per-floor print as pack_evac_plan.py makes it (scratch only since the token patches)."""
    return "Prop_EvacPlan_A_" + token + ("" if full else "Phase0")


def base_name(full):
    return "Prop_EvacPlan_A_Landing" + ("" if full else "Phase0")


def patch_name(token, band, i):
    return "Prop_EvacPlan_A_Landing_%s_%s%d" % (token, band, i)


def level(token):
    return "B10+" if token == "B10plus" else token


def transcript_id(token, full):
    return "placard_landing_" + token + ("" if full else "_phase0")


def guid_for(rel):
    return hashlib.md5(("FrontRooms3D BS6 " + rel).encode("utf-8")).hexdigest()


def pack_one(src_dir, scratch):
    out = os.path.join(scratch, os.path.basename(src_dir))
    os.makedirs(out, exist_ok=True)
    r = subprocess.run([sys.executable, PACK, src_dir, out], capture_output=True, text=True)
    if r.returncode != 0:
        stop("pack_evac_plan.py failed on %s:\n%s%s" % (src_dir, r.stdout, r.stderr))
    return os.path.join(out, "Prop_EvacPlan_A.png"), os.path.join(out, "Prop_EvacPlan_E.png"), r.stdout


def cs_string(s):
    return inspect_transcript.cs_string(s)


def regions_of(ex, name):
    regs = {r.get("id"): r for r in ex.get("regions", [])}
    if [r.get("id") for r in ex.get("regions", [])] != REGION_ORDER:
        stop("%s: regions are not plan, legend, alarm, titleblock" % name)
    out = []
    for rid in REGION_ORDER:
        r = regs[rid]
        x, y, w, h = r["rect"]
        out.append((rid, float(x), float(y), float(w), float(h), round(float(r["capMm"]), 2)))
    return out


def fnum(v):
    s = ("%.2f" % v).rstrip("0").rstrip(".")
    return s + "f"


def rgb(path):
    a = np.asarray(Image.open(path).convert("RGB"))
    if a.shape != (PRINT_H, PRINT_W, 3):
        stop("%s is %s, not %d x %d" % (path, a.shape, PRINT_W, PRINT_H))
    return a


def runs(mask_1d, gap):
    """Index runs of True, joined across gaps shorter than gap: [(first, last)]."""
    idx = np.nonzero(mask_1d)[0]
    if len(idx) == 0:
        return []
    out, a, b = [], idx[0], idx[0]
    for i in idx[1:]:
        if i - b > gap:
            out.append((int(a), int(b)))
            a = i
        b = i
    out.append((int(a), int(b)))
    return out


def pot_split(start, length):
    """A multiple of PATCH_GRID cut into power-of-two pieces, largest first: [(start, length)]."""
    out, n, x = [], length // PATCH_GRID, start
    bit = 1 << (n.bit_length() - 1)
    while bit:
        if n & bit:
            out.append((x, bit * PATCH_GRID))
            x += bit * PATCH_GRID
        bit >>= 1
    return out


def token_pieces(token, prints):
    """The token's patch pieces: [(band, x, y, w, h)] in texels from the top-left, from where it differs from B1."""
    diff = np.zeros((PRINT_H, PRINT_W), bool)
    for full in (True, False):
        diff |= (prints[(token, full)] != prints[(BASE_TOKEN, full)]).any(axis=2)
    if not diff.any():
        stop("%s does not differ from %s" % (token, BASE_TOKEN))
    bands = runs(diff.any(axis=1), PATCH_GRID)
    if len(bands) != len(BANDS):
        stop("%s differs from %s in %d bands, not the title band and the title block: re-plan the patches" % (token, BASE_TOKEN, len(bands)))
    pieces = []
    for band, (y0, y1) in zip(BANDS, bands):
        cols = diff[y0:y1 + 1].any(axis=0)
        x0, x1 = int(np.nonzero(cols)[0][0]), int(np.nonzero(cols)[0][-1])
        gx0, gx1 = x0 // PATCH_GRID * PATCH_GRID, -(-(x1 + 1) // PATCH_GRID) * PATCH_GRID
        gy0, gy1 = y0 // PATCH_GRID * PATCH_GRID, -(-(y1 + 1) // PATCH_GRID) * PATCH_GRID
        for py, ph in pot_split(gy0, gy1 - gy0):
            for px, pw in pot_split(gx0, gx1 - gx0):
                pieces.append((band, px, py, pw, ph))
    for i, (_, x, y, w, h) in enumerate(pieces):
        for _, x2, y2, w2, h2 in pieces[i + 1:]:
            if x < x2 + w2 and x2 < x + w and y < y2 + h2 and y2 < y + h:
                stop("%s: two patch pieces overlap" % token)
        if x < 0 or y < 0 or x + w > PRINT_W or y + h > PRINT_H or w & (w - 1) or h & (h - 1):
            stop("%s: piece %d,%d %dx%d leaves the print or is not a power of two" % (token, x, y, w, h))
    return pieces


def save_rgb(a, path):
    Image.fromarray(np.ascontiguousarray(a), "RGB").save(path, optimize=True)
    if not np.array_equal(np.asarray(Image.open(path).convert("RGB")), a):
        stop("PNG round trip failed: " + path)


def build_cs(sheets):
    lines = []
    w = lines.append
    w("// GENERATED by Tools/lookdev/pack_landing_placards.py from 平面视觉's sheets (Tools/print/ink/art_from_graphic/placard/")
    w("// sheetA_B*/ and sheetA_phase0_B*/: examine.json, print and glow md5s below). Regenerate with the textures; never edit by hand.")
    w("using UnityEngine;")
    w("")
    w("/// <summary>")
    w("/// BS6: the stairwell-landing copies of the evacuation plan, one per floor below LEVEL 0 and per state (叙事 40 §3, A35):")
    w("/// the full sheet (the security desk, the padlocked stair door) on a floor with its own station and a locked EXIT, the")
    w("/// Phase 0 sheet without. B10 and deeper share LEVEL B10+. FrontRoomsLandingPlacard hangs them; each sheet's EXAMINE")
    w("/// transcript (FrontRoomsInspectCatalog format: '\\u0001' glow line, '\\u0002' region head) and regions (sheet mm) ship")
    w("/// with its packed print, so they always match.")
    w("/// Token patches (Red 2026-10-31, option (c)): each variant ships one base print, its LEVEL B1 sheet; every other sheet")
    w("/// is that base with its token's patches (the texels where its LEVEL B<n> differs from B1, the same in both variants)")
    w("/// copied over it once per floor. The packer proves base + patches = the token's packed print, pixel for pixel.")
    w("/// </summary>")
    w("public static class FrontRoomsLandingPlacardSheets")
    w("{")
    w("    public sealed class Sheet")
    w("    {")
    w("        /// <summary>The token printed on the sheet: B1 .. B9, B10+.</summary>")
    w("        public string level;")
    w("        /// <summary>True: the full sheet (station + padlock); false: Phase 0.</summary>")
    w("        public bool full;")
    w("        /// <summary>平面视觉's folder under Tools/print/ink/art_from_graphic/placard.</summary>")
    w("        public string folder;")
    w("        /// <summary>Resources paths of the variant's base print (_BaseMap: the LEVEL B1 sheet) and of the glow mask (_EmissionMap).</summary>")
    w("        public string print, glow;")
    w("        /// <summary>Resources paths of this sheet's token patches, copied over the base once per floor; none for B1.</summary>")
    w("        public string[] patches;")
    w("        /// <summary>Per patch: x, y, w, h in the base's texels at PrintWidth x PrintHeight, from the top-left (PNG rows).</summary>")
    w("        public int[] rects;")
    w("        /// <summary>The EXAMINE transcript id (FrontRoomsInspectCatalog.SetTranscript) and its lines.</summary>")
    w("        public string transcript;")
    w("        public string[] lines;")
    w("        /// <summary>Per region (plan, legend, alarm, titleblock): x, y, w, h (sheet mm from the top-left) and the smallest cap (mm).</summary>")
    w("        public float[] regions;")
    w("    }")
    w("")
    w("    /// <summary>Region ids, in the order of Sheet.regions (5 floats each).</summary>")
    w("    public static readonly string[] RegionIds = { %s };" % ", ".join('"%s"' % r for r in REGION_ORDER))
    w("")
    w("    /// <summary>B1 .. B9 have a sheet of their own; B10 and deeper hang B10+.</summary>")
    w("    public const int Depths = %d;" % len(TOKENS))
    w("")
    w("    /// <summary>The packed print's size in texels (pack_evac_plan.py): Sheet.rects are in these.</summary>")
    w("    public const int PrintWidth = %d, PrintHeight = %d;" % (PRINT_W, PRINT_H))
    w("")
    w("    /// <summary>Every patch rect is on this grid (texels at PrintWidth): on the 4 x 4 block grid down to mip 5.</summary>")
    w("    public const int PatchGrid = %d;" % PATCH_GRID)
    w("")
    for full in (True, False):
        w("    static readonly Sheet[] %s =" % ("Full" if full else "Phase0"))
        w("    {")
        for s in [s for s in sheets if s["full"] == full]:
            w("        // %s: print md5 %s, glow md5 %s, examine.json md5 %s; packed print sha1 %s = %s" % (
                s["folder"], s["print_md5"][:8], s["mask_md5"][:8], s["examine_md5"][:8], s["packed_sha1"][:8],
                "the base" if not s["patches"] else "the base + %d patch%s" % (len(s["patches"]), "" if len(s["patches"]) == 1 else "es")))
            w("        new Sheet")
            w("        {")
            w("            level = %s, full = %s, folder = %s," % (cs_string(s["level"]), "true" if full else "false", cs_string(s["folder"])))
            w("            print = %s, glow = %s," % (cs_string("Surfaces/Textures/" + s["print"]), cs_string("Surfaces/Textures/" + s["glow"])))
            if s["patches"]:
                w("            patches = new[]")
                w("            {")
                for name, _ in s["patches"]:
                    w("                %s," % cs_string("Surfaces/Textures/" + name))
                w("            },")
                w("            rects = new[]")
                w("            {")
                for name, (x, y, ww, h) in s["patches"]:
                    w("                %d, %d, %d, %d,   // %s" % (x, y, ww, h, name[len("Prop_EvacPlan_A_Landing_"):]))
                w("            },")
            else:
                w("            patches = new string[0], rects = new int[0],")
            w("            transcript = %s," % cs_string(s["transcript"]))
            w("            lines = new[]")
            w("            {")
            for l in s["lines"]:
                w("                %s," % cs_string(l))
            w("            },")
            w("            regions = new[]")
            w("            {")
            for rid, x, y, ww, h, cap in s["regions"]:
                w("                %s, %s, %s, %s, %s,   // %s" % (fnum(x), fnum(y), fnum(ww), fnum(h), fnum(cap), rid))
            w("            },")
            w("        },")
        w("    };")
        w("")
    w("    /// <summary>")
    w("    /// The sheet for basement <paramref name=\"depth\"/> (1 = LEVEL B1, the run's floor 2; FrontRooms3DGame.FloorLabel), full")
    w("    /// or Phase 0. Depth 10 and deeper: B10+. Null for depth 0 (LEVEL 0 hangs its own sheets) or less.")
    w("    /// </summary>")
    w("    public static Sheet For(int depth, bool full)")
    w("    {")
    w("        if (depth < 1) return null;")
    w("        return (full ? Full : Phase0)[Mathf.Min(depth, Depths) - 1];")
    w("    }")
    w("")
    w("    /// <summary>Every sheet, full ones first (tools and tests).</summary>")
    w("    public static Sheet[] All()")
    w("    {")
    w("        var all = new Sheet[Full.Length + Phase0.Length];")
    w("        Full.CopyTo(all, 0);")
    w("        Phase0.CopyTo(all, Full.Length);")
    w("        return all;")
    w("    }")
    w("")
    w("    /// <summary>A sheet's regions as EXAMINE takes them (a new array each call).</summary>")
    w("    public static InspectDef.Region[] Regions(Sheet sheet)")
    w("    {")
    w("        var n = RegionIds.Length;")
    w("        var regions = new InspectDef.Region[n];")
    w("        for (var i = 0; i < n; i++)")
    w("        {")
    w("            var k = i * 5;")
    w("            regions[i] = new InspectDef.Region")
    w("            {")
    w("                id = RegionIds[i],")
    w("                rect = new[] { sheet.regions[k], sheet.regions[k + 1], sheet.regions[k + 2], sheet.regions[k + 3] },")
    w("                capMm = sheet.regions[k + 4],")
    w("            };")
    w("        }")
    w("        return regions;")
    w("    }")
    w("}")
    return "\n".join(lines) + "\n"


def meta_text(template, guid):
    with open(template, "rb") as f:
        t = f.read().decode("utf-8")
    if "\r" in t:
        stop("template meta has CR: " + template)
    head, rest = t.split("\n", 2)[:2], t.split("\n", 2)[2]
    if not head[1].startswith("guid: "):
        stop("template meta has no guid line: " + template)
    return head[0] + "\n" + "guid: " + guid + "\n" + rest


def sha1(path):
    with open(path, "rb") as f:
        return hashlib.sha1(f.read()).hexdigest()


def main(argv):
    check = "--check" in argv
    if any(a != "--check" for a in argv):
        sys.exit(__doc__)
    for token in TOKENS:
        for full in (True, False):
            d = os.path.join(ART, folder(token, full))
            for f in ("placard_print_lit.png", "placard_glow_mask.png", "examine.json"):
                if not os.path.isfile(os.path.join(d, f)):
                    stop("missing %s/%s" % (folder(token, full), f))
            if not md5(os.path.join(d, "placard_glow_mask.png")).startswith(MASK_MD5[full]):
                stop("%s: glow mask is not the %s variant's one mask %s" % (folder(token, full), "full" if full else "Phase 0", MASK_MD5[full]))
    shared = os.path.join(TEX, SHARED_PHASE0_GLOW + ".png")
    if not os.path.isfile(shared):
        stop("main's " + SHARED_PHASE0_GLOW + ".png is missing")
    scratch = tempfile.mkdtemp(prefix="pack_landing_")
    staged = os.path.join(scratch, "out")
    packed = os.path.join(scratch, "packed")
    os.makedirs(staged)
    os.makedirs(packed)
    sheets = []
    locked = None
    try:
        for full in (True, False):
            for token in TOKENS:
                name = folder(token, full)
                src = os.path.join(ART, name)
                a, e, log = pack_one(src, scratch)
                shutil.move(a, os.path.join(packed, packed_name(token, full) + ".png"))
                if full:
                    if locked is None:
                        locked = os.path.join(staged, LOCKED_GLOW + ".png")
                        shutil.move(e, locked)
                    elif md5(e) != md5(locked):
                        stop("%s: its packed glow differs from the other full sheets'" % name)
                elif md5(e) != md5(shared):
                    stop("%s: its packed glow is not byte-identical to main's %s.png" % (name, SHARED_PHASE0_GLOW))
                raw = open(os.path.join(src, "examine.json"), "rb").read()
                ex = json.loads(raw.decode("utf-8"))
                lines = inspect_transcript.transcript(ex)
                want = "LEVEL " + level(token)
                if not lines[0].endswith(want) or not any(l.startswith(want + " · SHEET ") for l in lines):
                    stop("%s: the transcript does not name %s in the title band and the title block" % (name, want))
                sheets.append({
                    "token": token, "level": level(token), "full": full, "folder": name, "print": base_name(full),
                    "glow": LOCKED_GLOW if full else SHARED_PHASE0_GLOW, "transcript": transcript_id(token, full),
                    "lines": lines, "regions": regions_of(ex, name),
                    "print_md5": md5(os.path.join(src, "placard_print_lit.png")),
                    "mask_md5": md5(os.path.join(src, "placard_glow_mask.png")), "examine_md5": hashlib.md5(raw).hexdigest(),
                    "packed_sha1": sha1(os.path.join(packed, packed_name(token, full) + ".png")),
                })
                print("[pack_landing_placards] %-22s -> %s.png (%s)" % (name, packed_name(token, full), log.strip().splitlines()[-2].split("] ", 1)[-1]))
        # The bases: each variant's B1 print, byte for byte.
        for full in (True, False):
            shutil.copyfile(os.path.join(packed, packed_name(BASE_TOKEN, full) + ".png"), os.path.join(staged, base_name(full) + ".png"))
        prints = {(t, f): rgb(os.path.join(packed, packed_name(t, f) + ".png")) for t in TOKENS for f in (True, False)}
        patches = {BASE_TOKEN: []}
        texels = 0
        for token in TOKENS:
            if token == BASE_TOKEN:
                continue
            pieces = token_pieces(token, prints)
            made, count = [], {}
            for band, x, y, w, h in pieces:
                i = count.get(band, 0)
                count[band] = i + 1
                crop = prints[(token, True)][y:y + h, x:x + w]
                if not np.array_equal(crop, prints[(token, False)][y:y + h, x:x + w]):
                    stop("%s: the %s patch at %d,%d differs between the full and the Phase 0 sheet" % (token, band, x, y))
                name = patch_name(token, band, i)
                save_rgb(crop, os.path.join(staged, name + ".png"))
                made.append((name, (x, y, w, h)))
                texels += w * h
            # The proof: the base with these patches is the token's packed print, in both variants.
            for full in (True, False):
                comp = prints[(BASE_TOKEN, full)].copy()
                for name, (x, y, w, h) in made:
                    comp[y:y + h, x:x + w] = np.asarray(Image.open(os.path.join(staged, name + ".png")).convert("RGB"))
                if not np.array_equal(comp, prints[(token, full)]):
                    stop("%s: the base + patches is not %s's packed print" % (folder(token, full), token))
            patches[token] = made
            print("[pack_landing_placards] %-8s %d patch%s: %s; base + patches = both packed prints, pixel for pixel" % (
                token, len(made), "" if len(made) == 1 else "es",
                ", ".join("%s %d,%d %dx%d" % (n.rsplit("_", 1)[1], r[0], r[1], r[2], r[3]) for n, r in made)))
        for s in sheets:
            s["patches"] = patches[s["token"]]
        print("[pack_landing_placards] %d patches, %d texels (%.2f of one print)" % (
            sum(len(v) for v in patches.values()), texels, texels / float(PRINT_W * PRINT_H)))
        cs = build_cs(sheets)
        with open(os.path.join(staged, os.path.basename(CS)), "wb") as f:
            f.write(cs.encode("utf-8"))
        textures = [base_name(True), base_name(False)] + [n for t in TOKENS for n, _ in patches[t]]
        outputs = [(os.path.join(staged, n + ".png"), os.path.join(TEX, n + ".png")) for n in textures]
        outputs.append((locked, os.path.join(TEX, LOCKED_GLOW + ".png")))
        outputs.append((os.path.join(staged, os.path.basename(CS)), CS))
        metas = [(os.path.join(TEX, n + ".png"), os.path.join(TEX, "Prop_EvacPlan_A.png.meta")) for n in textures]
        metas.append((os.path.join(TEX, LOCKED_GLOW + ".png"), os.path.join(TEX, "Prop_EvacPlan_E.png.meta")))
        stale = [packed_name(t, f) + ".png" for t in TOKENS for f in (True, False)
                 if os.path.exists(os.path.join(TEX, packed_name(t, f) + ".png"))]
        if check:
            bad = 0
            for src, dst in outputs:
                if not os.path.isfile(dst) or md5(src) != md5(dst):
                    print("[pack_landing_placards] DIFFERS: " + os.path.relpath(dst, ROOT))
                    bad += 1
            for asset, _ in metas:
                if not os.path.isfile(asset + ".meta"):
                    print("[pack_landing_placards] NO META: " + os.path.relpath(asset, ROOT) + ".meta")
                    bad += 1
            for n in stale:
                print("[pack_landing_placards] STALE: a per-floor print is still in the folder (the patches replace it): " + n)
                bad += 1
            print("[pack_landing_placards] CHECK %s: %d outputs, %d metas, %d stale per-floor prints" % (
                "OK" if bad == 0 else "DIFFERS", len(outputs), len(metas), len(stale)))
            return 0 if bad == 0 else 1
        for src, dst in outputs:
            shutil.copyfile(src, dst)
        made = 0
        for asset, template in metas:
            m = asset + ".meta"
            if os.path.isfile(m):
                continue
            rel = os.path.relpath(asset, ROOT).replace(os.sep, "/")
            with open(m, "wb") as f:
                f.write(meta_text(template, guid_for(rel)).encode("utf-8"))
            made += 1
        print("[pack_landing_placards] wrote 2 bases, %d patches, %s.png, %s; %d new .meta" % (
            len(textures) - 2, LOCKED_GLOW, os.path.relpath(CS, ROOT), made))
        if stale:
            print("[pack_landing_placards] NOTE: %d per-floor prints are still in the folder; move them out (never used now): %s" % (len(stale), ", ".join(stale)))
        return 0
    finally:
        shutil.rmtree(scratch, ignore_errors=True)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
