#!/usr/bin/env python3
"""The language tables (Documentation/research/localization/README.md).

  language_tables.py keys <Frontrooms3D>    lists every string the game can translate, one per line
  language_tables.py check <Frontrooms3D>   per language: rows missing, rows no longer used, {0} and caption brackets kept, line ends,
                                            and for ja ko zh-Hans zh-Hant the characters its three faces cannot draw (run
                                            subset_fonts.py again when this lists any)

A string is translatable when it goes through FrontRoomsLanguage.T or .F or a view helper that calls T (Txt, MetaLine,
KeylineButton), or sits in FrontRoomsSettingsMenu.cs, whose rows pass through T inside Get. Tables are
Assets/Resources/Language/<code>.txt: English, tab, translation, tab, note.
Strings that reach T through a variable (a label array, a helper's case) cannot be found in the code; they are listed in
passed_keys.txt beside this file, one per line (a line break is written \n, as in the tables).
Run from anywhere; it reads only.
"""
import glob
import os
import re
import struct
import sys

# The faces each of these languages sets its text in (Assets/Resources/Fonts, cut by subset_fonts.py to the table's characters).
FACES = {
    "ja": ("NotoSansJP-Bold.otf", "NotoSansJP-Regular.otf", "NotoSerifJP-Regular.otf"),
    "ko": ("NotoSansKR-Bold.otf", "NotoSansKR-Regular.otf", "NotoSerifKR-Regular.otf"),
    "zh-Hans": ("NotoSansSC-Bold.ttf", "NotoSansSC-Regular.ttf", "NotoSerifSC-Regular.ttf"),
    "zh-Hant": ("NotoSansTC-Bold.otf", "NotoSansTC-Regular.otf", "NotoSerifTC-Regular.otf"),
}

STRING =r'"((?:[^"\\\n]|\\.)*)"'
CALL = re.compile(r'(?:FrontRoomsLanguage\.[TF]\(|\bMetaLine\(|\bKeylineButton\(|\bTxt\([^,"]*,\s*)\s*(?:[^"\n;]*\?\s*)?' + STRING + r'(?:\s*:\s*' + STRING + r')?')
LITERAL = re.compile(r'(?<![\w@])\$?' + STRING)
WHOLE_FILE = ("FrontRoomsSettingsMenu.cs", "FrontRoomsCaptions.cs")
CAPTION = re.compile(r'"(\[[A-Z][A-Z ,]*\])"')
# Literals in those files that are not text: separators, units and codes.
NOT_TEXT = re.compile(r'^[|%\s]*$|^\{0\}$|^\d+%?$|^\d+ FPS$|^\d+$|^\|$')


def unescape(s):
    return s.replace('\\"', '"').replace("\\n", "\n").replace("\\\\", "\\")


def keys(root):
    found = {}
    for path in glob.glob(root + "/Assets/Scripts/**/*.cs", recursive=True):
        name = os.path.basename(path)
        if name == "FrontRoomsLanguage.cs":
            continue
        for number, line in enumerate(open(path, encoding="utf-8", errors="replace"), 1):
            code = line.split("//")[0] if "://" not in line else line
            if code.lstrip().startswith("///"):
                continue
            if "/Audio/" in path:
                for m in CAPTION.finditer(code):
                    found.setdefault(m.group(1), f"{name}:{number}")
            if name in WHOLE_FILE:
                for m in LITERAL.finditer(code):
                    s = unescape(m.group(1))
                    if not NOT_TEXT.match(s) and any(c.isalpha() for c in s):
                        found.setdefault(s, f"{name}:{number}")
            else:
                for m in CALL.finditer(code):
                    for s in m.groups():
                        if s and any(c.isalpha() for c in s):
                            found.setdefault(unescape(s), f"{name}:{number}")
            if "const string HintKeys" in code:
                found.setdefault(LITERAL.search(code).group(1), f"{name}:{number}")
    listed = os.path.join(os.path.dirname(os.path.abspath(__file__)), "passed_keys.txt")
    for line in open(listed, encoding="utf-8"):
        line = line.rstrip("\n")
        if line and not line.startswith("#"):
            found.setdefault(unescape(line), "passed_keys.txt")
    return found


def font_characters(path):
    """The code points a font file draws, read from its cmap (formats 4 and 12): no fontTools needed."""
    data = open(path, "rb").read()
    base = None
    for i in range(struct.unpack(">H", data[4:6])[0]):
        tag, _, off, _ = struct.unpack(">4sLLL", data[12 + i * 16:28 + i * 16])
        if tag == b"cmap":
            base = off
    best = None
    for i in range(struct.unpack(">H", data[base + 2:base + 4])[0]):
        pid, eid, off = struct.unpack(">HHL", data[base + 4 + i * 8:base + 12 + i * 8])
        fmt = struct.unpack(">H", data[base + off:base + off + 2])[0]
        rank = {12: 2, 4: 1}.get(fmt, 0)
        if (pid, eid) in ((3, 10), (0, 4), (0, 6), (3, 1), (0, 3)) and rank and (best is None or rank > best[0]):
            best = (rank, base + off, fmt)
    drawn = set()
    if best is None:
        return drawn
    _, o, fmt = best
    if fmt == 12:
        for i in range(struct.unpack(">L", data[o + 12:o + 16])[0]):
            s, e, g = struct.unpack(">LLL", data[o + 16 + i * 12:o + 28 + i * 12])
            if g or e > s:
                drawn.update(range(s, e + 1))
        return drawn
    segx2 = struct.unpack(">H", data[o + 6:o + 8])[0]
    seg = segx2 // 2
    ends = struct.unpack(">%dH" % seg, data[o + 14:o + 14 + segx2])
    starts = struct.unpack(">%dH" % seg, data[o + 16 + segx2:o + 16 + 2 * segx2])
    deltas = struct.unpack(">%dh" % seg, data[o + 16 + 2 * segx2:o + 16 + 3 * segx2])
    ro_off = o + 16 + 3 * segx2
    ranges = struct.unpack(">%dH" % seg, data[ro_off:ro_off + segx2])
    for i in range(seg):
        for c in range(starts[i], ends[i] + 1):
            if c == 0xFFFF:
                continue
            if ranges[i] == 0:
                g = (c + deltas[i]) & 0xFFFF
            else:
                p = ro_off + i * 2 + ranges[i] + (c - starts[i]) * 2
                g = struct.unpack(">H", data[p:p + 2])[0]
                if g:
                    g = (g + deltas[i]) & 0xFFFF
            if g:
                drawn.add(c)
    return drawn


def table(path):
    rows = {}
    for line in open(path, encoding="utf-8", newline=""):
        line = line.rstrip("\n")
        if not line or line.startswith("#"):
            continue
        cells = line.split("\t")
        rows[cells[0].replace("\\n", "\n")] = cells[1].replace("\\n", "\n") if len(cells) > 1 else ""
    return rows


def main():
    verb, root = sys.argv[1], sys.argv[2]
    wanted = keys(root)
    if verb == "keys":
        for k in sorted(wanted):
            print(k.replace("\n", "\\n"))
        print(f"# {len(wanted)} strings", file=sys.stderr)
        return 0
    bad = 0
    for path in sorted(glob.glob(root + "/Assets/Resources/Language/*.txt")):
        code = os.path.basename(path)[:-4]
        raw = open(path, "rb").read()
        rows = table(path)
        missing = [k for k in wanted if not rows.get(k)]
        stale = [k for k in rows if k not in wanted]
        broken = [k for k, v in rows.items() if v and (k.count("{0}") != v.count("{0}") or k.count("{1}") != v.count("{1}"))]
        # FrontRoomsCaptions.Format puts the direction inside the bracket only for an ASCII ].
        broken += [k for k, v in rows.items() if v and k.startswith("[") and not (v.startswith("[") and v.endswith("]"))]
        crlf = b"\r" in raw
        # Characters of this table that one of its faces cannot draw (the tofu box): the faces are cut to the table, so a row
        # added after the last subset_fonts.py run shows here.
        undrawn = {}
        for face in FACES.get(code, ()):
            drawn = font_characters(f"{root}/Assets/Resources/Fonts/{face}")
            gone = sorted({c for v in rows.values() for c in v if not c.isspace() and ord(c) not in drawn})
            if gone:
                undrawn[face] = "".join(gone)
        print(f"{code}: {len(rows)} rows, {len(missing)} missing, {len(stale)} unused, {len(broken)} with {{0}} wrong{', CRLF!' if crlf else ''}"
              + (f", {len(set(''.join(undrawn.values())))} characters no face draws (run subset_fonts.py)" if undrawn else ""))
        for face, gone in undrawn.items():
            print(f"   no glyph in {face}: {gone}")
        for k in missing[:8]:
            print("   missing:", k[:70])
        for k in stale[:8]:
            print("   unused: ", k[:70])
        for k in broken[:8]:
            print("   {0}:    ", k[:70])
        bad += len(broken) + (1 if crlf else 0) + len(undrawn)
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
