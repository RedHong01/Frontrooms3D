#!/usr/bin/env python3
"""Pack the stairwell-landing copies of the evacuation plan (BS6, 游戏视觉 2026-10-31).

  python3 Tools/lookdev/pack_landing_placards.py [--check]

BS6 (research/tutorial/20_requests.md, 教程设计's row, "V6 now"): on every floor below LEVEL 0 a copy of the plan hangs on
the map side of the stairwell's far door, at P1 west (FrontRoomsLandingPlacard). Each floor hangs the sheet for its own
state (叙事 research/narrative/40_floors_exit_tutorial_strings.md §3, A35): the full sheet (sheetA_B<n>: the security desk,
the padlocked stair door) when the floor has its own station and a locked EXIT, the Phase 0 sheet (sheetA_phase0_B<n>)
without. B10 and deeper share LEVEL B10+ (sheetA_B10plus, sheetA_phase0_B10plus). The art is 平面视觉's
(Tools/print/ink/art_from_graphic/placard/, BS8 of 2026-10-30, README "Per-floor sheets"), used as delivered.

What it writes (from main's tree, run in a clone; LF, UTF-8):
  Assets/Resources/Surfaces/Textures/Prop_EvacPlan_A_<T>.png          the full sheet's print, T = B1 .. B9, B10plus
  Assets/Resources/Surfaces/Textures/Prop_EvacPlan_A_<T>Phase0.png    the Phase 0 sheet's print
  Assets/Resources/Surfaces/Textures/Prop_EvacPlan_Locked_E.png       the full sheets' one glow mask (9d3a9480, every floor)
  Assets/Scripts/Rendering/FrontRoomsLandingPlacardSheets.cs           the sheet table: resources, transcripts, regions
and a .meta beside each new texture when it has none (Prop_EvacPlan_A.png.meta / Prop_EvacPlan_E.png.meta as the
template, the GUID an md5 of the asset path; an existing .meta is never rewritten).
The Phase 0 sheets' glow mask (ed18539e, every floor) is the one main already ships as Prop_EvacPlan_E.png, packed from
sheetA_phase0: the tool checks that packing a Phase 0 mask reproduces it byte for byte, and never writes it.

Every print and mask goes through Tools/lookdev/pack_evac_plan.py unchanged (2592 x 1674 -> 4096 x 2048, linear-light
Lanczos, its round-trip check), one folder at a time, into a scratch folder, then is renamed. The transcripts come from
each folder's examine.json through Tools/lookdev/inspect_transcript.py's transcript() (its region heads; byte for byte
otherwise). Glow k stays FrontRoomsPhosphor.PeakEmission (0.158): no code here touches it.

--check  writes nothing: packs and generates into a scratch folder and compares with the tree; exit 1 on any difference.
Stops (nothing written) if a folder is missing, a mask is not its variant's one mask, or an examine.json does not split.
"""
import hashlib
import json
import os
import shutil
import subprocess
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
ART = os.path.join(ROOT, "Tools", "print", "ink", "art_from_graphic", "placard")
TEX = os.path.join(ROOT, "Assets", "Resources", "Surfaces", "Textures")
CS = os.path.join(ROOT, "Assets", "Scripts", "Rendering", "FrontRoomsLandingPlacardSheets.cs")
PACK = os.path.join(HERE, "pack_evac_plan.py")
sys.path.insert(0, HERE)
import inspect_transcript  # noqa: E402  (the region heads: transcript())

TOKENS = ["B1", "B2", "B3", "B4", "B5", "B6", "B7", "B8", "B9", "B10plus"]
# One glow mask per variant (平面视觉's README, BS8): every full sheet, every Phase 0 sheet.
MASK_MD5 = {True: "9d3a9480", False: "ed18539e"}
SHARED_PHASE0_GLOW = "Prop_EvacPlan_E"          # main's, packed from sheetA_phase0 (same mask); never written here
LOCKED_GLOW = "Prop_EvacPlan_Locked_E"          # ends in _E: FrontRoomsSurfaceTextureImporter imports it linear, BC1
REGION_ORDER = ["plan", "legend", "alarm", "titleblock"]


def stop(msg):
    sys.exit("pack_landing_placards: STOP: " + msg)


def md5(path):
    with open(path, "rb") as f:
        return hashlib.md5(f.read()).hexdigest()


def folder(token, full):
    return ("sheetA_" if full else "sheetA_phase0_") + token


def print_name(token, full):
    return "Prop_EvacPlan_A_" + token + ("" if full else "Phase0")


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
    w("        /// <summary>Resources paths of the packed print (_BaseMap) and glow mask (_EmissionMap).</summary>")
    w("        public string print, glow;")
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
    for full in (True, False):
        w("    static readonly Sheet[] %s =" % ("Full" if full else "Phase0"))
        w("    {")
        for s in [s for s in sheets if s["full"] == full]:
            w("        // %s: print md5 %s, glow md5 %s, examine.json md5 %s" % (s["folder"], s["print_md5"][:8], s["mask_md5"][:8], s["examine_md5"][:8]))
            w("        new Sheet")
            w("        {")
            w("            level = %s, full = %s, folder = %s," % (cs_string(s["level"]), "true" if full else "false", cs_string(s["folder"])))
            w("            print = %s, glow = %s," % (cs_string("Surfaces/Textures/" + s["print"]), cs_string("Surfaces/Textures/" + s["glow"])))
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
    os.makedirs(staged)
    sheets = []
    locked = None
    try:
        for full in (True, False):
            for token in TOKENS:
                name = folder(token, full)
                src = os.path.join(ART, name)
                a, e, log = pack_one(src, scratch)
                shutil.move(a, os.path.join(staged, print_name(token, full) + ".png"))
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
                    "level": level(token), "full": full, "folder": name, "print": print_name(token, full),
                    "glow": LOCKED_GLOW if full else SHARED_PHASE0_GLOW, "transcript": transcript_id(token, full),
                    "lines": lines, "regions": regions_of(ex, name),
                    "print_md5": md5(os.path.join(src, "placard_print_lit.png")),
                    "mask_md5": md5(os.path.join(src, "placard_glow_mask.png")), "examine_md5": hashlib.md5(raw).hexdigest(),
                })
                print("[pack_landing_placards] %-22s -> %s.png (%s)" % (name, print_name(token, full), log.strip().splitlines()[-2].split("] ", 1)[-1]))
        cs = build_cs(sheets)
        with open(os.path.join(staged, os.path.basename(CS)), "wb") as f:
            f.write(cs.encode("utf-8"))
        outputs = [(os.path.join(staged, s["print"] + ".png"), os.path.join(TEX, s["print"] + ".png")) for s in sheets]
        outputs.append((locked, os.path.join(TEX, LOCKED_GLOW + ".png")))
        outputs.append((os.path.join(staged, os.path.basename(CS)), CS))
        metas = []
        for s in sheets:
            metas.append((os.path.join(TEX, s["print"] + ".png"), os.path.join(TEX, "Prop_EvacPlan_A.png.meta")))
        metas.append((os.path.join(TEX, LOCKED_GLOW + ".png"), os.path.join(TEX, "Prop_EvacPlan_E.png.meta")))
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
            print("[pack_landing_placards] CHECK %s: %d outputs, %d metas" % ("OK" if bad == 0 else "DIFFERS", len(outputs), len(metas)))
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
        print("[pack_landing_placards] wrote %d prints, %s.png, %s; %d new .meta" % (
            len(sheets), LOCKED_GLOW, os.path.relpath(CS, ROOT), made))
        return 0
    finally:
        shutil.rmtree(scratch, ignore_errors=True)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
