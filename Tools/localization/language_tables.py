#!/usr/bin/env python3
"""The language tables (Documentation/research/localization/README.md).

  language_tables.py keys <Frontrooms3D>    lists every string the game can translate, one per line
  language_tables.py check <Frontrooms3D>   per language: rows missing, rows no longer used, {0} and caption brackets kept, line ends

A string is translatable when it goes through FrontRoomsLanguage.T or .F or a view helper that calls T (Txt, MetaLine,
KeylineButton), or sits in FrontRoomsSettingsMenu.cs, whose rows pass through T inside Get. Tables are
Assets/Resources/Language/<code>.txt: English, tab, translation, tab, note.
Strings that reach T through a variable (a label array, a helper's case) cannot be found in the code; they are listed in
passed_keys.txt beside this file, one per line.
Run from anywhere; it reads only.
"""
import glob
import os
import re
import sys

STRING = r'"((?:[^"\\\n]|\\.)*)"'
CALL = re.compile(r'(?:FrontRoomsLanguage\.[TF]\(|\bMetaLine\(|\bKeylineButton\(|\bTxt\([^,"]*,\s*)\s*(?:[^"\n;]*\?\s*)?' + STRING + r'(?:\s*:\s*' + STRING + r')?')
LITERAL = re.compile(r'(?<![\w@])\$?' + STRING)
WHOLE_FILE = ("FrontRoomsSettingsMenu.cs", "FrontRoomsCaptions.cs")
CAPTION = re.compile(r'"(\[[A-Z][A-Z ,]*\])"')
# Literals in those files that are not text: separators, units and codes.
NOT_TEXT = re.compile(r'^[|%\s]*$|^\{0\}$|^\d+%?$|^\d+ FPS$|^\d+$|^\|$')


def unescape(s):
    return s.replace('\\"', '"').replace("\\\\", "\\")


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
            found.setdefault(line, "passed_keys.txt")
    return found


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
        print(f"{code}: {len(rows)} rows, {len(missing)} missing, {len(stale)} unused, {len(broken)} with {{0}} wrong{', CRLF!' if crlf else ''}")
        for k in missing[:8]:
            print("   missing:", k[:70])
        for k in stale[:8]:
            print("   unused: ", k[:70])
        for k in broken[:8]:
            print("   {0}:    ", k[:70])
        bad += len(broken) + (1 if crlf else 0)
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
