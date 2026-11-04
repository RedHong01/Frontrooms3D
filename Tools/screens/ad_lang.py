"""The TV ad in the player's language (world text W19): Ad_03 with its supers set in each script. 平面视觉 · 广告, 2026-11-03.

Red, 2026-11-03, 叙事设计 chat: "目前切换成不同的语言，应该游戏里比如说对应的广告，平面视觉的type应该也需要做对应的改变，也就是所有的
diegetic narratives 需要adapt 语言的变化。" then "全部按推荐" (research/narrative/95_world_text_localization.md §6): frame A, a
dubbed film. The same American spot plays; only its words change. The voice stays English (captions are the HUD's).
Strings: the game's own tables, Assets/Resources/Language/<lang>.txt (key = the signed English, 95 §5.4). Proper names,
the phone number, the prices, the store's logotype and the whole station ID card stay as they are.

Faces (AD_TYPE_SYSTEM.md §1.3). Each translated word keeps its English role, place, colour and shadow:
  Latin (de fr es pt-BR)  the English faces: Anton, Archivo Black and Black Italic, Heros on the CG
  Cyrillic (ru)           Oswald Bold for Anton; Libre Franklin Black / Black Italic for Archivo; Roboto Condensed Bold and
                          Regular on the CG (a neo-grotesque pair like Heros's; Franklin set too wide for the CG's slots)
  CJK (zh-Hans zh-Hant ja ko)  Noto Sans CJK Bold (the game's own cut, Resources/Fonts), as a 1990 phototypesetter set
                          gothic for TV: 長体 (condensed) for the Anton headline, 斜体 (slanted) for the italic supers, a
                          touch heavier for the black weights; Bold and Regular on the CG, which keeps its scan-line steps
Sizes match the English cap height (CJK: the ideograph face a little above it, as CJK needs to read). A word wider than its
slot steps down to 75 % of the design size, then (the card-4 line only) breaks in two; past that it is an OVER for 叙事设计.
Outputs (out/FrontRooms_Ad_03/<lang>/): FrontRooms_Ad_03_<lang>.mp4 (640 x 480, 30 fps, the same 16 s and cuts at
3/7/10/14 s as Ad_03, so 声音设计's loop lines up), cards/*.png, states/*.png; and lang_sheet.png + lang_fit.txt (review).
Usage: python3 ad_lang.py [lang ...]   (default all nine; FR_PROJECT overrides the project root)
"""
import json
import math
import os
import shutil
import subprocess
import sys
import tempfile

import numpy as np
from PIL import Image, ImageDraw, ImageFont

import broadcast as B

W, H = B.W, B.H
LANGS = ["zh-Hans", "zh-Hant", "ja", "ko", "de", "fr", "es", "pt-BR", "ru"]
OUT = os.path.join(B.HERE, "out", "FrontRooms_Ad_03")
SPEC = os.path.join(B.HERE, "spec", "frontrooms_ad_03.json")
TABLES = os.path.join(B.PROJECT, "Assets", "Resources", "Language")
RES_FONTS = os.path.join(B.PROJECT, "Assets", "Resources", "Fonts")
P90 = B.FONTS
SAFE = (66, 574)                                         # RP 27.3 title safe, across


class Face:
    """One face in one role. ref = how its size is matched: 'cap' (the H) or 'ideo' (the CJK em, face 0.86 em)."""
    def __init__(self, path, ref="cap", condense=1.0, oblique=0.0, embolden=0.0, lift=1.0, track=None):
        self.path, self.ref, self.condense, self.oblique, self.embolden, self.lift, self.track = \
            path, ref, condense, oblique, embolden, lift, track

    def font(self, px):
        return ImageFont.truetype(self.path, max(4, int(round(px))))

    def px_for(self, cap_px):
        """The em (px) that gives this face the English cap height cap_px (CJK: its ideographs a touch taller)."""
        if self.ref == "ideo":
            return cap_px * self.lift / 0.86
        probe = self.font(1000)
        b = probe.getbbox("H")
        return cap_px * self.lift / ((b[3] - b[1]) / 1000)


def p90(rel):
    return os.path.join(P90, rel)


LATIN = {"super": Face(p90("Archivo/Archivo-BlackItalic.ttf")), "head": Face(p90("Anton/Anton-Regular.ttf")),
         "flag": Face(p90("Archivo/Archivo-Black.ttf")),
         "cgB": p90("TeXGyre/texgyreheros-bold.otf"), "cgR": p90("TeXGyre/texgyreheros-regular.otf")}
CYRILLIC = {"super": Face(p90("LibreFranklin/LibreFranklin-BlackItalic.ttf")),
            "head": Face(p90("Oswald/Oswald-Bold.ttf"), embolden=.018),
            "flag": Face(p90("LibreFranklin/LibreFranklin-Black.ttf")),
            "cgB": p90("RobotoCondensed/RobotoCondensed-Bold.ttf"), "cgR": p90("RobotoCondensed/RobotoCondensed-Regular.ttf")}
CJK_FILES = {"zh-Hans": ("NotoSansSC-Bold.ttf", "NotoSansSC-Regular.ttf"), "zh-Hant": ("NotoSansTC-Bold.otf", "NotoSansTC-Regular.otf"),
             "ja": ("NotoSansJP-Bold.otf", "NotoSansJP-Regular.otf"), "ko": ("NotoSansKR-Bold.otf", "NotoSansKR-Regular.otf")}


def faces(lang):
    if lang in CJK_FILES:
        bold, reg = (os.path.join(RES_FONTS, f) for f in CJK_FILES[lang])
        return {"super": Face(bold, "ideo", oblique=10, embolden=.03, lift=1.2, track=.02),
                "head": Face(bold, "ideo", condense=.86, embolden=.032, lift=1.0, track=.0),
                "flag": Face(bold, "ideo", embolden=.03, lift=1.15, track=.04),
                "cgB": bold, "cgR": reg, "cg_track": 0.0}
    return dict(CYRILLIC if lang == "ru" else LATIN)


def table(lang):
    rows = {}
    for line in open(os.path.join(TABLES, lang + ".txt"), encoding="utf-8"):
        if line.startswith("#") or "\t" not in line:
            continue
        p = line.rstrip("\n").split("\t")
        rows[p[0].replace("\\n", "\n")] = p[1].replace("\\n", "\n")      # the tables write a line break as a literal \n
    return rows


def strings(lang):
    """The translated words of the spot, split into the spec's elements."""
    t = table(lang)
    g = lambda k: t[k]
    now_was = g("NOW $499  WAS $899")
    i = now_was.find("$499")
    now, was = now_was[:i].strip(), now_was[i + 4:].strip()
    lead = g("CALL 555-0147").replace("555-0147", "").strip()
    return {"card1_super": g("BACK ROOM CLEARANCE"), "card2_headline": g("3-PC SOFA SET"), "card2_now": now, "card2_was": was,
            "card2_finance": g("90 DAYS SAME AS CASH"), "card3_lead": lead, "card3_hours": g("OPEN 9 TO 9  7 DAYS"),
            "card3_flag": g("FREE DELIVERY"), "card4_lines": g("ALWAYS MORE\nIN THE BACK").split("\n"),
            "station_slogan": g("STAY TUNED."), "standby_1": g("PLEASE STAND BY"), "standby_2": g("TECHNICAL DIFFICULTIES"),
            "thought": g("THOUGHT FOR TODAY\nTHE STEADY WALKER\nGOES FARTHER THAN\nTHE ONE WHO RUNS.").split("\n"),
            "tag_hours": g("OPEN 9 TO 9")}


# ---- setting one line of the agency layer
def set_mask(s, face, px, track):
    """Coverage of one line at canvas resolution (B.SS): drawn, emboldened, condensed and slanted like a phototypesetter."""
    ss = B.SS
    f = face.font(px * ss)
    widths = [f.getlength(ch) for ch in s]
    gap = track * px * ss
    total = sum(widths) + gap * max(len(s) - 1, 0)
    asc, desc = f.getmetrics()
    pad = int(px * ss * .6) + 8
    img = Image.new("L", (int(math.ceil(total)) + 2 * pad, asc + desc + 2 * pad), 0)
    d = ImageDraw.Draw(img)
    emb = int(round(face.embolden * px * ss))
    x = pad
    for ch, w in zip(s, widths):
        d.text((x, pad), ch, font=f, fill=255, stroke_width=emb, stroke_fill=255)
        x += w + gap
    if face.condense != 1.0:
        img = img.resize((max(1, int(round(img.width * face.condense))), img.height), Image.LANCZOS)
    if face.oblique:
        t = math.tan(math.radians(face.oblique))
        base = pad + asc                                  # shear about the baseline: x' = x + t * (base - y)
        extra = int(math.ceil(t * img.height))
        img = img.transform((img.width + extra, img.height), Image.AFFINE, (1, t, -t * base, 0, 1, 0), Image.BICUBIC)
    return img


def ink(mask):
    a = np.asarray(mask) > 24
    ys, xs = np.where(a)
    return (xs.min(), ys.min(), xs.max() + 1, ys.max() + 1) if len(xs) else (0, 0, 1, 1)


def english_box(s, face_key, size, x, y, center, track=0.0):
    """Where the English line's ink sits on the card (canvas px), as broadcast.Canvas.text draws it."""
    f = B.font(B.F[face_key], size)
    widths = [f.getlength(ch) for ch in s]
    total = sum(widths) + track * size * B.SS * (len(s) - 1)
    x0 = x * B.SS - (total / 2 if center else 0)
    img = Image.new("L", (int(total) + 8 * B.SS + int(size * B.SS), int(size * B.SS * 2)), 0)
    d = ImageDraw.Draw(img)
    xx = 4 * B.SS
    for ch, w in zip(s, widths):
        d.text((xx, 0), ch, font=f, fill=255)
        xx += w + track * size * B.SS
    b = ink(img)
    return (x0 - 4 * B.SS + b[0], y * B.SS + b[1], x0 - 4 * B.SS + b[2], y * B.SS + b[3])


class Fit:
    def __init__(self):
        self.rows = []

    def add(self, lang, elem, s, design, used, width, slot, verdict):
        self.rows.append((lang, elem, s.replace("\n", " / "), design, round(used, 1), int(round(width)), int(round(slot)), verdict))


def place(cv, s, role, lang, F, size_en, en_face, en_text, x, y, col, shadow, slot, fit, elem, center=True,
          align_left=None, track_en=0.0, min_scale=.75, right_limit=None):
    """Set s in the language's face for this role so its ink sits where the English ink sat; shrink to fit the slot."""
    face = F[role]
    eb = english_box(en_text, en_face, size_en, x, y, center, track_en)
    en_cap = B.font(B.F[en_face], size_en).getbbox("H")
    cap_px = (en_cap[3] - en_cap[1]) / B.SS
    px = face.px_for(cap_px)
    track = face.track if face.track is not None else track_en
    design = px
    m = set_mask(s, face, px, track)
    b = ink(m)
    w = (b[2] - b[0]) / B.SS
    verdict = "ok"
    if w > slot:
        scale = max(slot / w, min_scale)
        px = design * scale
        m = set_mask(s, face, px, track)
        b = ink(m)
        w = (b[2] - b[0]) / B.SS
        verdict = "SHRUNK" if w <= slot + .5 else "OVER"
    cy = (eb[1] + eb[3]) / 2                              # vertical: the English ink's centre
    if center:
        ox = x * B.SS - (b[0] + b[2]) / 2
    elif right_limit is not None and eb[0] + (b[2] - b[0]) > right_limit * B.SS:
        ox = right_limit * B.SS - b[2]                    # too long to start where English did: end at the limit
    else:
        ox = (align_left if align_left is not None else eb[0]) - b[0]
    oy = cy - (b[1] + b[3]) / 2
    composite(cv, m, ox, oy, col, shadow)
    fit.add(lang, elem, s, round(design, 1), px, w, slot, verdict)
    return ((ox + b[0]) / B.SS, (oy + b[1]) / B.SS, (ox + b[2]) / B.SS, (oy + b[3]) / B.SS)


def composite(cv, m, ox, oy, col, shadow):
    """Shadow (black, offset) then fill, at canvas resolution, as Canvas.text does."""
    base = np.asarray(cv.im, np.float32)
    a = np.zeros(base.shape[:2], np.float32)
    ox, oy = int(round(ox)), int(round(oy))

    def paste(dst, src, dx, dy):
        h, w = src.shape
        y0, x0 = max(dy, 0), max(dx, 0)
        y1, x1 = min(dy + h, dst.shape[0]), min(dx + w, dst.shape[1])
        if y1 > y0 and x1 > x0:
            dst[y0:y1, x0:x1] = np.maximum(dst[y0:y1, x0:x1], src[y0 - dy:y1 - dy, x0 - dx:x1 - dx])

    src = np.asarray(m, np.float32) / 255
    if shadow:
        sh = a.copy()
        paste(sh, src, ox + shadow * B.SS, oy + shadow * B.SS)
        base = base * (1 - sh[..., None])
    fill = a.copy()
    paste(fill, src, ox, oy)
    base = base * (1 - fill[..., None]) + np.array(col, np.float32) * fill[..., None]
    cv.im = Image.fromarray(np.clip(np.round(base), 0, 255).astype(np.uint8))
    cv.d = ImageDraw.Draw(cv.im)


# ---- the cards, as broadcast.py draws them, with the translated words placed by `place`
def card1(s, L, ctx, lang, F, fit):
    cv = B.Canvas(B.C["deep"], B.C["vdark"])
    B.lockup(cv, s, 56)
    cv.rect(64, 318, 512, 62, B.C["ink"])
    place(cv, L["card1_super"], "super", lang, F, 31, "arcBI", s["card1_super"], 320, 330, B.C["acc"], 3, 512 - 28, fit, "card1 super")
    return cv.out()


def card2(s, L, ctx, lang, F, fit):
    cv = B.Canvas(B.C["rose"], B.C["vdark"])
    place(cv, L["card2_headline"], "head", lang, F, 58, "anton", s["card2_headline"], 64, 36, B.C["cream"], 3, SAFE[1] - 64, fit,
          "card2 headline", center=False)
    card, ok = B.photo_card(ctx["dir"], ctx["photo"])
    ctx["photo_ok"] = ok
    cv.paste_rotated(card, 70, 134, 2)
    if L["card2_now"]:
        place(cv, L["card2_now"], "super", lang, F, 26, "arcBI", s["card2_now"], 400, 128, B.C["cream"], 2, SAFE[1] - 400, fit,
              "card2 now", center=False)
    else:
        fit.add(lang, "card2 now", "(none: the price stands alone)", 0, 0, 0, 0, "ok")
    cv.text("$", "anton", 44, 398, 166, B.C["acc"], stroke=2, shadow=4)
    cv.text(s["card2_price"], "anton", 124, 424, 142, B.C["acc"], stroke=3, shadow=5)
    box = place(cv, L["card2_was"], "super", lang, F, 24, "arcBI", s["card2_was"], 404, 312, B.C["cream"], 2, SAFE[1] - 372, fit,
                "card2 was", center=False, right_limit=SAFE[1])
    eb = english_box(s["card2_was"], "arcBI", 24, 404, 312, False)
    off = 328 - (eb[1] + eb[3]) / 2 / B.SS               # the strike sits where it crossed the English line
    cv.rect(box[0] - 2, (box[1] + box[3]) / 2 + off, box[2] - box[0] + 6, 3, B.C["red"])
    cv.rect(64, 380, 512, 44, B.C["ink"])
    place(cv, L["card2_finance"], "super", lang, F, 26, "arcBI", s["card2_finance"], 320, 384, B.C["acc"], 0, 512 - 24, fit, "card2 finance")
    return cv.out()


def card3(s, L, ctx, lang, F, fit):
    cv = B.Canvas(B.C["vdark"], B.C["vnight"])
    place(cv, L["card3_lead"], "super", lang, F, 44, "arcBI", s["card3_lead"], 320, 52, B.C["cream"], 3, SAFE[1] - SAFE[0], fit, "card3 lead")
    cv.text(s["card3_phone"], "anton", 132, 320, 96, B.C["acc"], center=True, stroke=3, shadow=6)
    place(cv, L["card3_hours"], "super", lang, F, 28, "arcBI", s["card3_hours"], 320, 286, B.C["cream"], 2, SAFE[1] - SAFE[0], fit, "card3 hours")
    # the pink flag grows with its word (English: 320 wide), never past title safe
    face = F["flag"]
    en_cap = B.font(B.F["arcB"], 28).getbbox("H")
    m = set_mask(L["card3_flag"], face, face.px_for((en_cap[3] - en_cap[1]) / B.SS),
                 face.track if face.track is not None else .04)
    b = ink(m)
    bw = min(max(320, (b[2] - b[0]) / B.SS + 48), 512)
    cv.rect(320 - bw / 2, 346, bw, 54, B.C["pink"])
    place(cv, L["card3_flag"], "flag", lang, F, 28, "arcB", s["card3_flag"], 320, 352, B.C["vdark"], 0, bw - 28, fit, "card3 flag",
          track_en=.04)
    return cv.out()


def card4_v3(s, L, ctx, lang, F, fit):
    cv = B.Canvas(B.C["deep"], B.C["vdark"])
    B.lockup(cv, s, 50, .82)
    lines = L["card4_lines"]
    joiner = "" if lang in ("zh-Hans", "zh-Hant", "ja") else " "
    one = joiner.join(lines)
    slot = SAFE[1] - SAFE[0]
    trial = Fit()
    place(B.Canvas((0, 0, 0)), one, "super", lang, F, 30, "arcBI", s["card4_line"], 320, 258, B.C["cream"], 3, slot, trial, "card4 line",
          min_scale=.9)
    if trial.rows[-1][-1] == "ok" or trial.rows[-1][-1] == "SHRUNK":
        place(cv, one, "super", lang, F, 30, "arcBI", s["card4_line"], 320, 258, B.C["cream"], 3, slot, fit, "card4 line", min_scale=.9)
    else:                                                 # two lines, 34 px apart, centred on the English line
        for i, line in enumerate(lines):
            place(cv, line, "super", lang, F, 30, "arcBI", s["card4_line"], 320, 258 - 17 + 34 * i, B.C["cream"], 3, slot, fit,
                  f"card4 line {i + 1}/2")
    for i, line in enumerate(s.get("card4_cg_lines", [])):
        cv.cg(line, 24, 320, 330 + i * 38, B.CG_WHITE, face="herosR", center=True, track=.06)
    return cv.out()


# ---- the station states: CG lines in the language's CG faces, stepping down the CG ladder to fit title safe
LADDER = [48, 36, 30, 24, 18]


def cg_fit(cv, text, lines, x, y, col, face, track, lang, fit, elem):
    sizes = [z for z in LADDER if z <= lines]
    for z in sizes:
        e = B.cg_extents([dict(s=text, lines=z, x=x, y=y, col=col, face=face, center=True, border=2, shadow=4, track=track)])[0]
        if e[1] >= SAFE[0] and e[2] <= SAFE[1]:
            break
    w = e[2] - e[1]
    verdict = "ok" if z == lines else ("SHRUNK" if e[1] >= SAFE[0] and e[2] <= SAFE[1] else "OVER")
    cv.cg(text, z, x, y + (lines - z) / 2, col, face=face, center=True, track=track)
    fit.add(lang, elem, text, lines, z, w, SAFE[1] - SAFE[0], verdict)


def register_cg(lang, F):
    B.F["cgB_" + lang], B.F["cgR_" + lang] = F["cgB"], F["cgR"]
    return "cgB_" + lang, "cgR_" + lang


def slogan(s, L, lang, F, fit):
    cv = B.Canvas(B.C["blue"], B.C["bnight"])
    B.id_slide(cv)
    cv.text(s["station_call"], "mich", 40, 320, 266, B.C["cream"], center=True, shadow=3, track=.08)
    kb, kr = register_cg(lang, F)
    cg_fit(cv, L["station_slogan"], 36, 320, 330, B.CG_YELLOW, kb, F.get("cg_track", .04), lang, fit, "slogan")
    return cv.out()


def standby(s, L, lang, F, fit):
    cv = B.Canvas(B.C["blue"], B.C["bnight"])
    B.id_slide(cv, .72, 58)
    cv.text(s["station_call"], "mich", 30, 320, 196, B.C["cream"], center=True, shadow=3, track=.08)
    kb, kr = register_cg(lang, F)
    cg_fit(cv, L["standby_1"], 36, 320, 286, B.CG_WHITE, kb, F.get("cg_track", .04), lang, fit, "standby 1")
    cg_fit(cv, L["standby_2"], 24, 320, 346, B.CG_WHITE, kr, F.get("cg_track", .06), lang, fit, "standby 2")
    return cv.out()


def thought(s, L, lang, F, fit):
    cv = B.Canvas(B.C["blue"], B.C["bnight"])
    kb, kr = register_cg(lang, F)
    cg_fit(cv, L["thought"][0], 30, 320, 142, B.CG_YELLOW, kb, F.get("cg_track", .04), lang, fit, "thought title")
    for i, line in enumerate(L["thought"][1:]):
        cg_fit(cv, line, 24, 320, 222 + i * 44, B.CG_WHITE, kr, F.get("cg_track", .06), lang, fit, f"thought {i + 1}")
    return cv.out()


def tag(s, L, lang, F, fit):
    cv = B.Canvas((0, 0, 0))
    kb, kr = register_cg(lang, F)
    cv.cg(s.get("tag_store", "FRONT ROOMS"), 36, 320, 70, B.CG_YELLOW, center=True, track=.04)
    cv.cg(s.get("tag_store_2", "FURNITURE"), 24, 320, 118, B.CG_YELLOW, center=True, track=.2)
    cv.cg(s.get("tag_address", "RT. 41 AT MAIN ST."), 24, 320, 174, B.CG_WHITE, face="herosR", center=True, track=.06)
    cv.cg(s["tag_town"], 24, 320, 210, B.CG_WHITE, face="herosR", center=True, track=.06)
    cv.cg(s["card3_phone"], 48, 320, 256, B.CG_WHITE, center=True, track=.02)
    cg_fit(cv, L["tag_hours"], 24, 320, 338, B.CG_WHITE, kr, F.get("cg_track", .06), lang, fit, "tag hours")
    return cv.out()


CARDS = {"card1": card1, "card2": card2, "card3": card3, "card4_v3": card4_v3}
STATES = {"station_id_slogan_v3": slogan, "standby_v3": standby, "thought_v3": thought, "tag_v3": tag}


def encode(spec, lang_dir, lang, card_paths):
    fps = spec.get("fps", 30)
    tmp = tempfile.mkdtemp(prefix="adlang_")
    lines = []
    for item in spec["sequence"]:
        lines.append(f"file '{card_paths[item['card']]}'\nduration {item['seconds']}")
    lines.append(f"file '{card_paths[spec['sequence'][-1]['card']]}'")
    open(os.path.join(tmp, "list.txt"), "w").write("\n".join(lines) + "\n")
    mp4 = os.path.join(lang_dir, f"{spec['name']}_{lang}.mp4")
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-f", "concat", "-safe", "0", "-i", os.path.join(tmp, "list.txt"),
                    "-vf", f"fps={fps},scale=out_color_matrix=bt601:out_range=tv,format=yuv420p,{B.COLOR_PARAMS}",
                    "-t", str(sum(i["seconds"] for i in spec["sequence"])),
                    "-c:v", "libx264", "-profile:v", "main", "-crf", "18", *B.COLOR_TAGS, "-movflags", "+faststart", mp4], check=True)
    shutil.rmtree(tmp)
    return mp4


def main(argv):
    langs = argv[1:] or LANGS
    spec = json.load(open(SPEC))
    s = spec["strings"]
    ctx = {"dir": os.path.dirname(os.path.abspath(SPEC)), "photo": spec.get("photo")}
    en_dir = OUT
    fit = Fit()
    tiles = {}
    for lang in langs:
        L, F = strings(lang), faces(lang)
        d = os.path.join(OUT, lang)
        for sub in ("cards", "states"):
            os.makedirs(os.path.join(d, sub), exist_ok=True)
        paths = {}
        for key, fn in CARDS.items():
            im = fn(s, L, ctx, lang, F, fit)
            p = os.path.join(d, "cards", key + ".png")
            im.save(p)
            paths[key] = p
            tiles[(lang, key)] = im
        paths["station_id_v3"] = os.path.join(en_dir, "cards", "station_id_v3.png")   # the station ID card has no words to change
        for key, fn in STATES.items():
            im = fn(s, L, lang, F, fit)
            im.save(os.path.join(d, "states", key + ".png"))
            tiles[(lang, key)] = im
        print(encode(spec, d, lang, paths), "photo:", "ok" if ctx.get("photo_ok") else "MISSING")
    with open(os.path.join(OUT, "lang_fit.txt"), "w") as f:
        f.write("lang\telement\ttext\tdesign\tused\twidth\tslot\tverdict\n")
        for r in fit.rows:
            f.write("\t".join(str(v) for v in r) + "\n")
    over = [r for r in fit.rows if r[-1] == "OVER"]
    print(f"fit: {len(fit.rows)} lines, {sum(r[-1] == 'SHRUNK' for r in fit.rows)} shrunk, {len(over)} over")
    for r in over:
        print("  OVER", r)
    # review sheet: English first, then each language; the four cards and the four states, quarter size
    cols = list(CARDS) + list(STATES)
    rows = ["en"] + langs
    tw, th = 320, 240
    sheet = Image.new("RGB", (len(cols) * (tw + 8) + 8, len(rows) * (th + 8) + 8), (24, 24, 24))
    for r, lang in enumerate(rows):
        for c, key in enumerate(cols):
            if lang == "en":
                sub = "cards" if key in CARDS else "states"
                im = Image.open(os.path.join(en_dir, sub, key + ".png")).convert("RGB")
            else:
                im = tiles.get((lang, key))
            if im is not None:
                sheet.paste(im.resize((tw, th), Image.LANCZOS), (8 + c * (tw + 8), 8 + r * (th + 8)))
    sheet.save(os.path.join(OUT, "lang_sheet.png"))


if __name__ == "__main__":
    main(sys.argv)
