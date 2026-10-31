"""ALARM ZONE card face: the STAND-IN art for Kit_AlarmZoneCard's slot Alarm_CardFace (游戏视觉, 2026-10-31).

STAND-IN. The card's lettering is 平面视觉's (ask: Documentation/research/room_visuals/51_alarm_zone_for_graphic.md).
This file only fills the slot until that art lands, with a face already in main (TeX Gyre Heros Bold, the placard face of
the stair signs and the evacuation plan) and the brief's defaults: white legend on fire red (#B01E18), two lines
"ALARM" / "ZONE", 1.625 in (41.3 mm) caps, no arrow, no pictogram, no EXIT. A small "STAND-IN" mark sits in the bottom
right corner of the albedo so the stand-in can never ship unnoticed. 平面视觉's final replaces the two PNGs in place
(same paths, same .meta GUIDs, same size or any size of the same 254 : 152 aspect).

Pillow only, deterministic, no downloads. Run from the project root:
  python Tools/lookdev/gen_alarm_card_standin.py [out_dir]
      default out_dir Assets/Resources/Print/AlarmTell
      alarm_zone_card_albedo.png  sRGB RGB, the face as seen unlit (translucent red acrylic, white legend)
      alarm_zone_card_glow.png    sRGB RGB, the emission map: legend full white, ground at 0.75 of the red
Face 254 x 152 mm at 8 px/mm = 2032 x 1216 px; u to the viewer's right, v up (kitlib decal quad).
"""

import os
import sys

from PIL import Image, ImageDraw, ImageFont

PX_PER_MM = 8
FACE_W_MM, FACE_H_MM = 254.0, 152.0
CAP_MM = 41.275                     # 1.625 in caps (18 mm side margins on ALARM)
LINE_GAP_CAPS = 0.6                 # baseline-to-baseline = 1.6 caps
RED = (176, 30, 24)                 # #B01E18, the red family (B's RunB_FireRed value)
LEGEND = (238, 233, 222)            # warm white acrylic, unlit
GLOW_LEGEND = (255, 251, 242)
GLOW_GROUND = 0.75                  # the ground's share of the red in the emission map (sRGB)
MARK = (204, 92, 82)                # the STAND-IN mark, a lighter red, albedo only
FONT = os.path.join("Assets", "Fonts", "Period1990", "TeXGyre", "texgyreheros-bold.otf")
LINES = ("ALARM", "ZONE")


def cap_px(font):
    box = font.getbbox("H")
    return box[3] - box[1]


def font_for_cap(path, cap):
    size = int(cap * 1.4)
    for _ in range(40):
        f = ImageFont.truetype(path, size)
        c = cap_px(f)
        if abs(c - cap) <= 1:
            return f
        size = max(4, int(round(size * cap / max(c, 1))))
    return ImageFont.truetype(path, size)


def draw_face(path_font, glow):
    w, h = int(FACE_W_MM * PX_PER_MM), int(FACE_H_MM * PX_PER_MM)
    ground = tuple(int(round(c * GLOW_GROUND)) for c in RED) if glow else RED
    img = Image.new("RGB", (w, h), ground)
    d = ImageDraw.Draw(img)
    cap = CAP_MM * PX_PER_MM
    font = font_for_cap(path_font, cap)
    pitch = cap * (1.0 + LINE_GAP_CAPS)
    block = cap + pitch * (len(LINES) - 1)
    top = (h - block) / 2.0                      # the first line's cap top
    for i, text in enumerate(LINES):
        box = font.getbbox(text)                 # ink box relative to the drawing origin
        ink_w = box[2] - box[0]
        x = (w - ink_w) / 2.0 - box[0]           # centre the ink, not the advance
        cap_top = top + i * pitch
        hb = font.getbbox("H")
        y = cap_top - hb[1]
        d.text((x, y), text, font=font, fill=GLOW_LEGEND if glow else LEGEND)
    if not glow:
        mark = font_for_cap(path_font, 3.5 * PX_PER_MM)
        mb = mark.getbbox("STAND-IN")
        mx = w - 6 * PX_PER_MM - (mb[2] - mb[0]) - mb[0]
        my = h - 6 * PX_PER_MM - (mb[3] - mb[1]) - mb[1]
        d.text((mx, my), "STAND-IN", font=mark, fill=MARK)
    return img


def main(argv):
    out = argv[0] if argv else os.path.join("Assets", "Resources", "Print", "AlarmTell")
    os.makedirs(out, exist_ok=True)
    if not os.path.isfile(FONT):
        sys.exit("missing " + FONT + " (run from the project root)")
    for glow, name in ((False, "alarm_zone_card_albedo.png"), (True, "alarm_zone_card_glow.png")):
        img = draw_face(FONT, glow)
        img.save(os.path.join(out, name), optimize=True)
        print("wrote", os.path.join(out, name), img.size)


if __name__ == "__main__":
    main(sys.argv[1:])
