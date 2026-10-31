#!/usr/bin/env python3
"""Cuts the CJK faces the game ships (Documentation/research/localization/README.md §4, LANGUAGE_MARKS.md) from the full Noto
fonts, which stay outside the repo. Run it again whenever a table gains characters.

  python3 -I subset_fonts.py <Frontrooms3D> <sources> <fonttools wheel>

<sources> holds NotoSansJP-{Bold,Regular}.otf, NotoSerifJP-Regular.otf and the KR and TC threes; the SC ones are the variable TTFs in
~/Library/Fonts, whose default weight is Thin, so each is pinned here. A language gets three faces for its text, cut to its
table's characters plus ASCII, and two tiny subsets that every language loads: its own name and its tag's token.
"""
import glob
import os
import sys

SC_SANS = os.path.expanduser("~/Library/Fonts/NotoSansSC-VariableFont_wght.ttf")
SC_SERIF = os.path.expanduser("~/Library/Fonts/NotoSerifSC-VariableFont_wght.ttf")
# Faces also set what is on screen beside the words: the arrows and marks in hints, the separators.
EXTRA = "←→↑↓‹›·…—–“”‘’«»°™"


def faces(sources):
    """code, role in the file name, source, weight to pin (variable sources only)."""
    return [
        ("zh-Hans", "NotoSansSC-Bold", SC_SANS, 700), ("zh-Hans", "NotoSansSC-Regular", SC_SANS, 400), ("zh-Hans", "NotoSerifSC-Regular", SC_SERIF, 400),
        ("ja", "NotoSansJP-Bold", sources + "/NotoSansJP-Bold.otf", None), ("ja", "NotoSansJP-Regular", sources + "/NotoSansJP-Regular.otf", None), ("ja", "NotoSerifJP-Regular", sources + "/NotoSerifJP-Regular.otf", None),
        ("ko", "NotoSansKR-Bold", sources + "/NotoSansKR-Bold.otf", None), ("ko", "NotoSansKR-Regular", sources + "/NotoSansKR-Regular.otf", None), ("ko", "NotoSerifKR-Regular", sources + "/NotoSerifKR-Regular.otf", None),
        ("zh-Hant", "NotoSansTC-Bold", sources + "/NotoSansTC-Bold.otf", None), ("zh-Hant", "NotoSansTC-Regular", sources + "/NotoSansTC-Regular.otf", None), ("zh-Hant", "NotoSerifTC-Regular", sources + "/NotoSerifTC-Regular.otf", None),
    ]


def tiny(sources):
    """file name, source, weight, characters: always loaded, whatever the language."""
    return [
        ("LanguageName-zh-Hans", SC_SERIF, 400, "简体中文"), ("LanguageName-ja", sources + "/NotoSerifJP-Regular.otf", None, "日本語"), ("LanguageName-ko", sources + "/NotoSerifKR-Regular.otf", None, "한국어"), ("LanguageName-zh-Hant", sources + "/NotoSerifTC-Regular.otf", None, "繁體中文"),
        ("LanguageToken-zh-Hans", SC_SANS, 700, "简文"), ("LanguageToken-ja", sources + "/NotoSansJP-Bold.otf", None, "日"), ("LanguageToken-ko", sources + "/NotoSansKR-Bold.otf", None, "한"), ("LanguageToken-zh-Hant", sources + "/NotoSansTC-Bold.otf", None, "繁"),
    ]


def table_characters(root, code):
    chars = set(chr(c) for c in range(0x20, 0x7F)) | set(EXTRA)
    for line in open(f"{root}/Assets/Resources/Language/{code}.txt", encoding="utf-8"):
        if line.startswith("#"):
            continue
        cells = line.rstrip("\n").split("\t")
        if len(cells) > 1:
            chars |= set(cells[1].replace("\\n", ""))
    return chars


def cut(subsetter, instancer, ttlib, source, weight, chars, out_stem):
    font = ttlib.TTFont(source, lazy=True)
    options = subsetter.Options()
    options.hinting = False
    options.name_IDs = ["*"]
    options.notdef_outline = True
    options.layout_features = ["kern", "liga", "ccmp", "locl", "palt"]
    cutter = subsetter.Subsetter(options)
    cutter.populate(unicodes=sorted(ord(c) for c in chars))
    cutter.subset(font)
    if weight is not None:
        font = instancer.instantiateVariableFont(font, {"wght": weight})
    extension = ".otf" if "CFF " in font else ".ttf"
    path = out_stem + extension
    font.save(path)
    missing = sorted(c for c in chars if ord(c) not in font.getBestCmap() and not c.isspace())
    print(f"{os.path.basename(path)}: {len(chars)} characters, {os.path.getsize(path) // 1024} KB" + (f", missing {''.join(missing)}" if missing else ""))
    return os.path.getsize(path)


def main():
    root, sources, wheel = sys.argv[1], sys.argv[2], sys.argv[3]
    sys.path.insert(0, wheel)
    from fontTools import subset as subsetter, ttLib as ttlib
    from fontTools.varLib import instancer
    out = root + "/Assets/Resources/Fonts/"
    total = 0
    for code, name, source, weight in faces(sources):
        total += cut(subsetter, instancer, ttlib, source, weight, table_characters(root, code), out + name)
    for name, source, weight, chars in tiny(sources):
        total += cut(subsetter, instancer, ttlib, source, weight, set(chars), out + name)
    print(f"total {total // 1024} KB")


if __name__ == "__main__":
    main()
