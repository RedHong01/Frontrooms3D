#!/usr/bin/python3
"""The STAIR KEY plate (W3) in the player's language (平面视觉, 2026-11-03). Red 11-03, 叙事设计 chat: world text follows the language
(research/narrative/95_world_text_localization.md); faces and rules: research/localization/WORLD_TYPE.md §2-3.
Same plate as station_paper/build_atlas.py plate(): 0.30 x 0.10 m, a light core (#E2DDCF) cut through a dark cap (#2B2925), drawn at
12 px/mm and resized to 2048 x 1024. Words from Assets/Resources/Language/<lang>.txt (叙事's signed rows).
Fit (WORLD_TYPE §3): a line wider than the plate's measure is set narrower first (to 85 % width), then wrapped onto two lines at 85 % of
its size; it is never cut. Usage: plate_lang.py <lang> <out.png> [--sheet]"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.expanduser("~/Desktop/ArtCenter/Fall26T7/EGAM-401A-01 Individual Game Project/Frontrooms3D/")
F = ROOT + "Assets/Fonts/Period1990/"
SRC = os.path.expanduser("~/FrontRoomsInteractionWork/settings/localization/fonts/src/")
HB, HC = F + "TeXGyre/texgyreheros-bold.otf", F + "TeXGyre/texgyreheroscn-bold.otf"
RCB = F + "RobotoCondensed/RobotoCondensed-Bold.ttf"
CJKF = {"zh-Hans": (os.path.expanduser("~/Library/Fonts/NotoSansSC-VariableFont_wght.ttf"), 700), "zh-Hant": (SRC + "NotoSansTC-Bold.otf", None),
        "ja": (SRC + "NotoSansJP-Bold.otf", None), "ko": (SRC + "NotoSansKR-Bold.otf", None)}
PX, CAP, CORE = 12, (43, 41, 37), (226, 221, 207)
MEASURE = 276.0                                     # mm: 12 mm clear of each plate edge


def table(lang):
    rows = {}
    for line in open(ROOT + f"Assets/Resources/Language/{lang}.txt", encoding="utf-8"):
        if line.startswith("#"): continue
        f = line.rstrip("\n").split("\t")
        if len(f) >= 2 and f[1]: rows[f[0]] = f[1]
    return rows


def face(lang, role, cap):
    """role 'bold' (Heros Bold) or 'cond' (Heros Cn Bold); returns (font, track factor). CJK body = cap / 0.70, no tracking."""
    if lang in CJKF:
        path, w = CJKF[lang]; f = ImageFont.truetype(path, round(cap * PX / 0.70))
        if w: f.set_variation_by_axes([w])
        return f, 0.0
    path = RCB if lang == "ru" else (HB if role == "bold" else HC)
    probe = ImageFont.truetype(path, 1000); b = probe.getbbox("H")
    return ImageFont.truetype(path, round(cap * PX * 1000 / (b[3] - b[1]))), 1.0


def line_image(s, f, track_px):
    """The line on a transparent layer (alpha = ink), its baseline at y = ascent."""
    asc, desc = f.getmetrics()
    w = int(sum(f.getlength(c) + track_px for c in s) - track_px + 4) if track_px else int(f.getlength(s) + 4)
    im = Image.new("L", (max(1, w), asc + desc + 4), 0); d = ImageDraw.Draw(im); x = 2
    if track_px:
        for c in s: d.text((x, asc + 2), c, font=f, fill=255, anchor="ls"); x += f.getlength(c) + track_px
    else: d.text((x, asc + 2), s, font=f, fill=255, anchor="ls")
    return im.crop((0, 0, im.width, im.height)), asc + 2


def put(page, s, cx, base, lang, role, cap, track):
    """Narrower before smaller: Latin and Cyrillic take the condensed cut first; then the line is set narrower, down to 85 %."""
    roles = [role] + (["cond"] if role == "bold" and lang not in CJKF else [])
    for r in roles:
        f, tk = face(lang, r, cap)
        im, by = line_image(s, f, track * PX * tk)
        w = im.width / PX
        if w > MEASURE:
            sx = MEASURE / w
            if sx < 0.85: continue
            im = im.resize((max(1, round(im.width * sx)), im.height), Image.LANCZOS)
        page.paste(Image.new("RGB", im.size, CORE), (round(cx * PX - im.width / 2), round(base * PX - by)), im)
        return True
    return False


def split_two(s):
    """Two lines at the space or the CJK colon nearest the middle."""
    cuts = [i for i, c in enumerate(s) if c in " ：:"]
    if not cuts: n = len(s) // 2; return s[:n], s[n:]
    i = min(cuts, key=lambda k: abs(k - len(s) / 2))
    return (s[:i + 1] if s[i] in "：:" else s[:i]).strip(), s[i + 1:].strip()


def plate(lang):
    t = table(lang) if lang != "en" else {}
    key = t.get("STAIR KEY", "STAIR KEY"); warn = t.get("SILENT ALARM: REMOVAL SIGNALS PANEL", "SILENT ALARM: REMOVAL SIGNALS PANEL")
    page = Image.new("RGB", (300 * PX, 100 * PX), CAP)
    cap = 26.0
    while not put(page, key, 150, 47, lang, "bold", cap, 1.2):   # the last resort: smaller, never below 20 mm
        cap -= 1.0; assert cap >= 20, (lang, "STAIR KEY")
    if not put(page, warn, 150, 78, lang, "cond", 11, 0.15):
        a, b = split_two(warn)
        assert put(page, a, 150, 74, lang, "cond", 11 * .85, 0.15) and put(page, b, 150, 87.5, lang, "cond", 11 * .85, 0.15), (lang, "warning")
    return page.resize((2048, 1024), Image.LANCZOS)


if __name__ == "__main__":
    lang, out = sys.argv[1], sys.argv[2]
    os.makedirs(os.path.dirname(out) or ".", exist_ok=True)
    plate(lang).save(out, optimize=True); print("wrote", out, lang)
