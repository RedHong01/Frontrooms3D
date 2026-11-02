"""C16 alarm door placard: the STAND-IN face for Kit_AlarmExit_Placard's slot Alarm_DoorPlacard (游戏视觉, 2026-11-01).

STAND-IN. The placard's lettering is 平面视觉 · 信息设计's (the words are signed by 叙事设计: `ALARM WILL SOUND`, one line,
never EXIT; research/narrative/90_alarm_devices_and_door_strings_signoff.md section 4). This file only fills the slot until
their face lands, with a face already in main (TeX Gyre Heros Bold, the placard face of the stair signs, the evacuation
plan and the ALARM ZONE card's stand-in) in the alarm family's colours: a warm white legend on fire red (#B01E18), one
line centred, the largest caps that keep 12 mm side margins (about 25 mm, 1 in), no pictogram, no arrow. A small
"STAND-IN" mark sits in the bottom right corner so the stand-in can never ship unnoticed. Their final replaces the PNG in
place (same path, same .meta GUID, any size of the 381 : 76 aspect).

Pillow only, deterministic, no downloads. Run from the project root:
  python Tools/lookdev/gen_alarm_door_placard_standin.py [out_dir]
      default out_dir Assets/Resources/Print/AlarmDoor
      alarm_door_placard_albedo.png  sRGB RGB, the screen-printed face (not lit, no emission map)
Face 381 x 76 mm (15 x 3 in) at 2048 x 408 px (5.375 px/mm); u to the viewer's right, v up (the kit's decal face).
"""

import os
import sys

from PIL import Image, ImageDraw, ImageFont

FACE_W_MM, FACE_H_MM = 381.0, 76.0
W_PX, H_PX = 2048, 408
MARGIN_MM = 12.0                    # side margins of the legend's ink
CAP_MAX_MM = 30.0                   # never more than 30 mm caps (0.39 of the plate's height)
RED = (176, 30, 24)                 # #B01E18, the alarm family's red (Alarm_BandRed, the ALARM ZONE card)
LEGEND = (238, 233, 222)            # warm white screen ink
MARK = (204, 92, 82)                # the STAND-IN mark, a lighter red
FONT = os.path.join("Assets", "Fonts", "Period1990", "TeXGyre", "texgyreheros-bold.otf")
TEXT = "ALARM WILL SOUND"


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


def legend_font(path, px_mm):
    """The largest caps whose ink keeps the side margins, at most CAP_MAX_MM."""
    cap = CAP_MAX_MM * px_mm
    for _ in range(60):
        f = font_for_cap(path, cap)
        box = f.getbbox(TEXT)
        if box[2] - box[0] <= W_PX - 2 * MARGIN_MM * px_mm:
            return f, cap / px_mm
        cap *= 0.985
    return f, cap / px_mm


def draw_face(path_font):
    px_mm = W_PX / FACE_W_MM
    img = Image.new("RGB", (W_PX, H_PX), RED)
    d = ImageDraw.Draw(img)
    font, cap_mm = legend_font(path_font, px_mm)
    box = font.getbbox(TEXT)
    hb = font.getbbox("H")
    x = (W_PX - (box[2] - box[0])) / 2.0 - box[0]
    cap = hb[3] - hb[1]
    y = (H_PX - cap) / 2.0 - hb[1]
    d.text((x, y), TEXT, font=font, fill=LEGEND)
    mark = font_for_cap(path_font, 2.6 * px_mm)
    mb = mark.getbbox("STAND-IN")
    mx = W_PX - 4 * px_mm - (mb[2] - mb[0]) - mb[0]
    my = H_PX - 4 * px_mm - (mb[3] - mb[1]) - mb[1]
    d.text((mx, my), "STAND-IN", font=mark, fill=MARK)
    return img, cap_mm


def main(argv):
    out = argv[0] if argv else os.path.join("Assets", "Resources", "Print", "AlarmDoor")
    os.makedirs(out, exist_ok=True)
    if not os.path.isfile(FONT):
        sys.exit("missing " + FONT + " (run from the project root)")
    img, cap_mm = draw_face(FONT)
    path = os.path.join(out, "alarm_door_placard_albedo.png")
    img.save(path, optimize=True)
    print("wrote", path, img.size, "caps %.1f mm" % cap_mm)


if __name__ == "__main__":
    main(sys.argv[1:])
