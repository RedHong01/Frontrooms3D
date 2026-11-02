"""Kit_AlarmExit_Placard: the C16 alarm door's placard slot (游戏视觉, 2026-11-01).

The SLOT only. A 15 x 3 in (381 x 76 mm) plate of 1/16 in screen-printed aluminium, R 4 mm corners, eased edges, adhesive
mounted, in the alarm family's fire red (Alarm_BandRed #B01E18); its face is ONE decal (slot Alarm_DoorPlacard, UV 0..1
across the face, u to the viewer's right, v up). The words are signed (`ALARM WILL SOUND`, one line, never EXIT:
research/narrative/90_alarm_devices_and_door_strings_signoff.md section 4); the lettering is 平面视觉 · 信息设计's. Until their
face lands, Resources/Print/AlarmDoor/alarm_door_placard_albedo.png is a STAND-IN (TeX Gyre Heros Bold, the placard face
already in main), marked STAND-IN in its corner; their final replaces that PNG in place (same path and GUID, any size of
the 381 : 76 aspect).

Origin: the BACK-face centre on the leaf face; front = Unity +Z (kit -Y); +Y up. The runtime hangs it on both faces at
door (+-0.022, 1.420, 0.770), beside the F1 vision lite on the latch-side panel, NEVER mirrored, so the decal reads right
on both faces. LOD0 bevelled plate + a rounded face; LOD1 a box + the face; LOD2 the face alone; never culled.
"""

import bmesh

import kitlib
import interact_door_common as dc
import alarm_exit_common as ax

NAME = "Kit_AlarmExit_Placard"
LOD1 = None
LODS = None
SMOOTH_ANGLE = 35.0

L = ax.lods


def face_part(kit, outline, z, name):
    """A single-sided n-gon facing Unity +Z at depth z, decal UVs across its bounds (u to the viewer's right)."""
    bm = bmesh.new()
    verts = [bm.verts.new(dc.U(x, y, z)) for x, y in outline]
    face = bm.faces.new(verts)
    face.normal_update()
    want = dc._bvec((0.0, 0.0, 1.0))
    if face.normal.dot(want) < 0:
        face.normal_flip()
    return kit._new_object(name, bm, ax.FACE, "decal", "xz")


def build(kit):
    ax.register_slots()
    w, h, t, r = ax.PLACARD_W, ax.PLACARD_H, ax.PLACARD_T, ax.PLACARD_R
    plate = dc.rounded_rect(w, h, r, 6)
    obj = dc.loft_z(kit, [(0.0, plate), (t, plate)], ax.RED, "placard plate")
    kit._bevel(obj, 0.0004, 1, angle=40.0)
    L(obj, "0")
    inset = 0.0006
    L(face_part(kit, dc.rounded_rect(w - 2 * inset, h - 2 * inset, r - inset, 6), t + 0.00012, "placard face"), "0")

    L(dc.obox(kit, ax.RED, lo=(-w / 2, -h / 2, 0.0), hi=(w / 2, h / 2, t), name="placard plate_1"), "1")
    L(kit.quad(w - 0.001, h - 0.001, dc.U(0.0, 0.0, t + 0.00012), ax.FACE, facing="-y", uv="decal", name="placard face_1"), "12")

    kit.no_collider()
    kit.tag("wall_decor", "alarm_exit", "alarm_door", "placard")
    ax.lod_meta(kit, ax.PLACARD_LOD_DISTANCES, ax.PLACARD_BUDGET,
                "hand-built LOD0/1/2 (kitlib fr_lods, P-1b); lodDistances (d01, d12, dcull) m, dcull -1 = never culled")
    kit.anchor("face_centre", dc.U(0.0, 0.0, t + 0.00012))
    kit.anchor("back", dc.U(0.0, 0.0, 0.0))
    kit.meta["partFrame"] = "origin = back-face centre on the leaf face; front = Unity +Z; +Y up; never mirrored"
    kit.meta["placard"] = {
        "size": [w, h, t], "slotFace": ax.FACE, "slotEdge": ax.RED, "words": "ALARM WILL SOUND (signed, one line; never EXIT)",
        "faceArt": "STAND-IN (TeX Gyre Heros Bold) until 平面视觉 · 信息设计 deliver; Resources/Print/AlarmDoor/alarm_door_placard_albedo.png",
        "standIn": True, "mount": "door (+-0.022, 1.420, 0.770) both faces, not mirrored",
    }
