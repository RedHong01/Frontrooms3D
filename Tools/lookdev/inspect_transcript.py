#!/usr/bin/env python3
"""EXAMINE transcript generator (游戏视觉, 2026-10-22): a placard sheet's examine.json -> the "placard_q16" entry of
Assets/Scripts/Rendering/Inspect/FrontRoomsInspectCatalog.cs, with its region heads.

The transcript format (research/desktop_ui/EXAMINE_TEXT_G9_G10.md G9; agreed with 交互设计 2026-10-22):
  FrontRoomsInspectCatalog.Transcript(id) returns string[] in reading order.
  - '\\u0001' + text   a glow line (examine.json's own marker; shown only while the ink is legible).
  - '\\u0002' + name   a region head: PLAN / LEGEND / IN CASE OF ALARM / TITLE BLOCK (the region words signed by 叙事,
                      research/narrative/40_floors_exit_tutorial_strings.md §4), one per examine.json region, in its
                      order. The lines up to the next head belong to that region. A head never glows.
  - Lines before the first head belong to no region: the sheet's title band (EVACUATION PLAN · <level>), which sits
    above every region rect (y 12-44 mm; the regions start at y 46).
  Every other line is examine.json's "transcript" line, byte for byte.

Which lines fall in which region: examine.json's regions carry rects and text-run counts ("texts"), not transcript
lines. If every region has an integer "lines" (a later examine.json may add it), those counts are used (any leading
rest is the title band). Otherwise the sheet's fixed structure (tutorial_placard.swift: sheet(), legend(), alarmBlock()
and the title-block row, the same in every sheet A variant: sheetA, sheetA_phase0, the B1 copies, sheetA_room*) places
them:
  title band  line 0, which starts "EVACUATION PLAN"
  plan        line 1 up to the legend
  legend      from the first line that starts "LEGEND"
  alarm       from the line "IN CASE OF ALARM"
  titleblock  the last line, which holds " · SHEET "
Anything else (a missing anchor, an unknown region id, a region order that is not the reading order, an empty region,
a line that already starts with '\\u0002') stops the tool: never guess a region.

Usage:
  inspect_transcript.py <examine.json> <FrontRoomsInspectCatalog.cs> [--label TEXT] [--check] [--print]
    rewrites the catalog's "placard_q16" entry (and the Transcript() doc block) in place, LF, UTF-8;
    --check  writes nothing; exit 1 if the catalog differs from what the tool would write
    --print  prints the transcript with <H> / <G> for the markers
  --label defaults from the sheet folder (sheetA_phase0 -> "sheet A, Phase 0 (no station, no padlock)").
Run it on a clone's catalog, then pack the sheet (Tools/lookdev/pack_evac_plan.py) and land both together.
"""
import hashlib
import json
import os
import re
import sys

GLOW = "\u0001"
HEAD = "\u0002"
# Region words signed by 叙事 (40 §4); a region id without a signed word stops the tool.
REGION_HEADS = {"plan": "PLAN", "legend": "LEGEND", "alarm": "IN CASE OF ALARM", "titleblock": "TITLE BLOCK"}
LABELS = {
    "sheetA_phase0": "sheet A, Phase 0 (no station, no padlock)",
    "sheetA": "sheet A (the station and the padlocked stair door)",
    "sheetA_phase0_B1": "sheet A, Phase 0, level B1 (no station, no padlock)",
    "sheetA_B1": "sheet A, level B1 (the station and the padlocked stair door)",
    "sheetA_room_phase0": "sheet A, the first room, Phase 0 (no padlock)",
    "sheetA_room": "sheet A, the first room (the station and the padlocked stair door)",
}

DOC = ("    /// <summary>\n"
       "    /// Transcript text by id (§5.2): lines in reading order (G9, research/desktop_ui/EXAMINE_TEXT_G9_G10.md).\n"
       "    /// - '\\u0001' starts a glow line (shown only while the ink is legible).\n"
       "    /// - '\\u0002' starts a region head: the region's name (PLAN, LEGEND, IN CASE OF ALARM, TITLE BLOCK; 叙事 40 §4), one per\n"
       "    ///   examine.json region, in its order. The lines up to the next head belong to that region. A head never glows.\n"
       "    /// - Lines before the first head (the sheet's title band, above every region) belong to no region.\n"
       "    /// </summary>\n")
DOC_RE = re.compile(r"^    /// <summary>(?:Transcript text by id[^\n]*</summary>\n|\n(?:    ///[^\n]*\n)*?    /// </summary>\n)(?=    static readonly Dictionary<string, string\[\]> Transcripts)", re.M)
ENTRY_RE = re.compile(
    r"^        // (?:The tutorial placard|Placeholder)[^\n]*\n(?:        //[^\n]*\n)*"
    r"(?:        \{ \"placard_q16\", new string\[0\] \},\n"
    r"|        \{\n            \"placard_q16\", new\[\]\n            \{\n(?:                [^\n]*,\n)*            \}\n        \},\n)",
    re.M)


def fail(msg):
    sys.exit("inspect_transcript: STOP: " + msg)


def body(line):
    return line[1:] if line.startswith(GLOW) else line


def partition(ex):
    """[(region id or None, [lines])]: the title band first (None), then one group per region in examine.json's order."""
    lines = ex.get("transcript")
    regions = ex.get("regions")
    if not isinstance(lines, list) or not lines or not isinstance(regions, list) or not regions:
        fail("examine.json needs a non-empty transcript and regions")
    ids = [r.get("id") for r in regions]
    for i in ids:
        if i not in REGION_HEADS:
            fail("region id %r has no signed head word (REGION_HEADS)" % (i,))
    if len(set(ids)) != len(ids):
        fail("duplicate region ids %r" % (ids,))
    for l in lines:
        if not isinstance(l, str) or HEAD in l or GLOW in l[1:]:
            fail("line %r: a marker inside a line, or a head marker already in examine.json" % (l,))
    if all(isinstance(r.get("lines"), int) for r in regions):
        counts = [r["lines"] for r in regions]
        rest = len(lines) - sum(counts)
        if rest < 0 or min(counts) < 1:
            fail("region line counts %r do not fit %d lines" % (counts, len(lines)))
        starts = {}
        at = rest
        for i, n in zip(ids, counts):
            starts[i] = at
            at += n
    else:
        if set(ids) != set(REGION_HEADS):
            fail("no explicit region line counts, and the regions %r are not the placard's four" % (ids,))
        if not body(lines[0]).startswith("EVACUATION PLAN"):
            fail("line 0 %r is not the title band (EVACUATION PLAN ...)" % (lines[0],))
        leg = [k for k, l in enumerate(lines) if body(l).startswith("LEGEND")]
        alm = [k for k, l in enumerate(lines) if body(l) == "IN CASE OF ALARM"]
        if len(leg) != 1 or len(alm) != 1:
            fail("legend / alarm anchors found %d / %d times (need 1 each)" % (len(leg), len(alm)))
        if " · SHEET " not in body(lines[-1]):
            fail("the last line %r is not the title-block row" % (lines[-1],))
        rest = 1
        starts = {"plan": 1, "legend": leg[0], "alarm": alm[0], "titleblock": len(lines) - 1}
    order = sorted(ids, key=lambda i: starts[i])
    if order != ids:
        fail("examine.json's region order %r is not the reading order %r" % (ids, order))
    groups = [(None, lines[:rest])] if rest > 0 else []
    for k, i in enumerate(ids):
        end = starts[ids[k + 1]] if k + 1 < len(ids) else len(lines)
        if end <= starts[i]:
            fail("region %s holds no line" % i)
        groups.append((i, lines[starts[i]:end]))
    if sum(len(g) for _, g in groups) != len(lines):
        fail("the regions do not cover the transcript")
    return groups


def transcript(ex):
    out = []
    for rid, group in partition(ex):
        if rid is not None:
            out.append(HEAD + REGION_HEADS[rid])
        out.extend(group)
    heads = [l for l in out if l.startswith(HEAD)]
    assert len(heads) == len(ex["regions"]) and [l for l in out if not l.startswith(HEAD)] == ex["transcript"]
    assert not any(l.startswith(HEAD) and GLOW in l for l in out)
    return out


def cs_string(s):
    out = []
    for ch in s:
        if ch == "\\": out.append("\\\\")
        elif ch == "\"": out.append("\\\"")
        elif ord(ch) < 0x20: out.append("\\u%04X" % ord(ch))
        else: out.append(ch)
    return "\"" + "".join(out) + "\""


def cs_entry(lines, label, folder, md5):
    return ("        // The tutorial placard, %s: 平面视觉's tutorial_placard.swift transcript\n"
            "        // (Tools/print/ink/art_from_graphic/placard/%s/examine.json md5 %s; strings signed by 叙事,\n"
            "        // research/narrative/40_floors_exit_tutorial_strings.md §3, region heads §4). It ships with the same packed\n"
            "        // sheet (Prop_EvacPlan_A/E); regenerate both together (Tools/lookdev/inspect_transcript.py), never type the lines here.\n"
            "        {\n"
            "            \"placard_q16\", new[]\n"
            "            {\n"
            "%s"
            "            }\n"
            "        },\n") % (label, folder, md5[:8], "".join("                %s,\n" % cs_string(s) for s in lines))


def update_catalog(text, ex, label, folder, md5):
    if "\r" in text:
        fail("the catalog has CR line ends")
    if len(DOC_RE.findall(text)) != 1:
        fail("the Transcript() doc block was not found exactly once")
    if len(ENTRY_RE.findall(text)) != 1:
        fail("the placard_q16 entry was not found exactly once")
    text = DOC_RE.sub(lambda m: DOC, text, count=1)
    return ENTRY_RE.sub(lambda m: cs_entry(transcript(ex), label, folder, md5), text, count=1)


def shown(lines):
    return "\n".join(l.replace(HEAD, "<H>").replace(GLOW, "<G>") for l in lines)


def main(argv):
    args = [a for a in argv if not a.startswith("--")]
    flags = [a for a in argv if a.startswith("--")]
    label = None
    if "--label" in argv:
        k = argv.index("--label")
        label = argv[k + 1]
        args.remove(label)
    if len(args) != 2 or any(f not in ("--check", "--print", "--label") for f in flags):
        sys.exit(__doc__)
    ex_path, cat_path = args
    raw = open(ex_path, "rb").read()
    md5 = hashlib.md5(raw).hexdigest()
    ex = json.loads(raw.decode("utf-8"))
    folder = os.path.basename(os.path.dirname(os.path.abspath(ex_path)))
    if label is None:
        if folder not in LABELS:
            fail("no default label for %s: pass --label" % folder)
        label = LABELS[folder]
    lines = transcript(ex)
    if "--print" in flags:
        print(shown(lines))
    with open(cat_path, "rb") as f:
        old = f.read().decode("utf-8")
    new = update_catalog(old, ex, label, folder, md5)
    if "--check" in flags:
        print("inspect_transcript: %s %s (%d lines, %d heads; examine %s md5 %s)" % (
            "CHECK OK" if new == old else "CHECK DIFFERS", cat_path, len(lines), sum(l.startswith(HEAD) for l in lines), folder, md5[:8]))
        return 0 if new == old else 1
    with open(cat_path, "wb") as f:
        f.write(new.encode("utf-8"))
    print("inspect_transcript: wrote %s (%d lines, %d heads; examine %s md5 %s)" % (cat_path, len(lines), sum(l.startswith(HEAD) for l in lines), folder, md5[:8]))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
