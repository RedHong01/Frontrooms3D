"""Kit_AlarmZoneCard: the ALARM ZONE tell's lit card at every entrance of a trigger room (RELAY §6, row 10).

A 1990 internally lit warning sign of the "X-RAY IN USE" / lighted-legend kind: a pressed-steel box 11.25 x 7.25 x 2 in
(286 x 184 x 51 mm) in fire-red enamel (#B01E18, the red family: red = ALARM ZONE), a face frame flange 4 mm deep and
16 mm wide held by four tamper-proof pan screws, and a 10 x 6 in (254 x 152 mm) translucent acrylic face 2 mm behind
the flange, lit from inside. The face is one decal quad (UV 0..1, u to the viewer's right) on the slot Alarm_CardFace:
its art is 平面视觉's (ask: Documentation/research/room_visuals/51_alarm_zone_for_graphic.md); until it lands the
material carries a STAND-IN drawn with TeX Gyre Heros Bold (the placard face already in main), flagged in the art.

Origin: the WALL-FACE point at the housing's centre; the face looks down Unity +Z (kit -Y); nothing behind the wall.
FrontRoomsAlarmTell mounts the centre at 1.524 m (60 in, ADAAG 1991 4.30.6), on the approach side of the entrance.
Tags "wall_decor" -> placement Wall. No collider (it stands 51 mm off the wall, above the hands' line). Emission only:
no realtime light on any tier. Hand-built LODs (kitlib P-1b): LOD0 bevelled housing, flange and screws; LOD1 plain
housing and flange; LOD2 the box and the face. Never culled (the card is the read from down a corridor).
"""

import kitlib
import alarm_tell_common as at

NAME = "Kit_AlarmZoneCard"
LOD1 = None
LODS = None
SMOOTH_ANGLE = 35.0
BUDGET = at.CARD_BUDGET
M = at.MM


def build(kit):
    at.register_slots()
    w, h, d = at.BOX_W * M, at.BOX_H * M, at.BOX_D * M
    fw, fh = at.FACE_W * M, at.FACE_H * M
    ft = at.FRAME_T * M
    body_d = d - ft

    # Housing (pressed steel, rolled edges) behind the flange.
    at.lods(kit.box((w, body_d, h), (0, -body_d / 2, 0), at.RED, bevel=0.003, segments=2, name="housing_0"), "0")
    at.lods(kit.box((w, body_d, h), (0, -body_d / 2, 0), at.RED, bevel=0.0, name="housing_1"), "1")
    # Face frame flange (one closed frame mesh, so the bevel runs round the corners).
    at.lods(kit.frame((w, h), (fw, fh), ft, (0, -(body_d + ft / 2), 0), at.RED, bevel=0.0012, segments=2, name="flange_0"), "0")
    at.lods(kit.frame((w, h), (fw, fh), ft, (0, -(body_d + ft / 2), 0), at.RED, bevel=0.0, name="flange_1"), "1")
    # LOD2: one box to the flange's front.
    at.lods(kit.box((w, d, h), (0, -d / 2, 0), at.RED, bevel=0.0, name="housing_2"), "2")

    # The lit acrylic face: 2 mm behind the flange front in LOD0/1 (inside the flange's reveal); on the box front in LOD2.
    yf = -(d - at.FACE_INSET * M)
    at.lods(kit.quad(fw, fh, (0, yf, 0), at.FACE, facing="-y", uv="decal", name="face_01"), "01")
    at.lods(kit.quad(fw, fh, (0, -(d + 0.0004), 0), at.FACE, facing="-y", uv="decal", name="face_2"), "2")

    # Four tamper-proof pan screws on the flange (LOD0 only: 7 mm heads are sub-pixel past 5 m).
    for sx in (-1, 1):
        for sz in (-1, 1):
            s = kit.cylinder(0.0035, 0.0016, (sx * at.SCREW_X * M, -(d + 0.0008), sz * at.SCREW_Z * M), at.CHROME,
                             verts=12, rot=(90, 0, 0), bevel=0.0007, segments=1, name="screw")
            at.lods(s, "0")

    kit.no_collider()
    kit.tag("wall_decor", "alarm_tell", "alarm_zone_card")
    at.lod_meta(kit, at.CARD_LOD_DISTANCES, BUDGET,
                "hand-built LOD0/1/2 (kitlib fr_lods, P-1b); lodDistances (d01, d12, dcull) m, dcull -1 = never culled")
    kit.anchor("face_centre", (0.0, yf, 0.0))
    kit.anchor("wall", (0.0, 0.0, 0.0))
    kit.meta["partFrame"] = "origin = wall-face point at the housing centre; face looks down Unity +Z; mount centre 1.524 m"
    kit.meta["alarmTell"] = {
        "box": [w, h, d], "face": [fw, fh], "faceInset": at.FACE_INSET * M, "centreHeight": at.CARD_CENTRE_M,
        "slotFace": at.FACE, "slotHousing": at.RED,
        "faceArt": "STAND-IN (TeX Gyre Heros Bold) until 平面视觉 delivers: research/room_visuals/51_alarm_zone_for_graphic.md",
        "standIn": True,
    }
