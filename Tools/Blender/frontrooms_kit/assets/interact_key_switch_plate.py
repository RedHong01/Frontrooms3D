"""Kit_KeySwitchPlate: the stair key's own wired hook (30L S10, §10.2 S10, §9.2 item 8; 61 §4.4 key-board row;
plan 70 P03/P03a).

Real reference: a supervised key switch on a building's combination fire/burglar panel loop: a painted-steel
backer screwed to the wall, a 4 in square box (galvanised) behind a brushed steel cover plate, ONE hook on a
plunger switch (the hung key ring holds the plunger in), a 1/2 in EMT stub leaving the box downward, and an
engraved two-line plate above it (STAIR KEY / SILENT ALARM: REMOVAL SIGNALS PANEL, 平面视觉's art: blank here).

Host convention (interact_key_board.py): ORIGIN = THE WALL FACE PLANE AT FLOOR LEVEL, front +Z into the room,
heights baked in; the module places it at y 0.
- backer 0.30 x 0.40 x 0.003 at y 1.25-1.65; plate 0.15 x 0.22 at y 1.31-1.53, 0.040 proud;
- key_hook (the ring's rest point) at y 1.4215, 0.068 proud: the KeySpot;
- plate_sign: the engraved plate 0.30 x 0.10 at y 1.545-1.645;
- sidecar collider = THE BODY ONLY (backer + box + plate, 0.041 deep), so the footprint stops 0.027 short of the
  KeySpot (plan check C2-2); the module prop is noCollider, so nothing is spawned for it.
Budget 3,500 / 1,200 / 150, LOD 3 / 8 / 40 m. Slots Prop_SteelPutty, Prop_Aluminium, Prop_Chrome,
Prop_StationKeyPlate (own slot; never Prop_SignEngraved, plan check C2-4). Tags interactable, key_host, wall_decor.
"""

import math

import _station_common as sc

NAME = "Kit_KeySwitchPlate"
LODS = (0.94, 0.90)   # hard surface: LODs drop sub-pixel parts only (no collapse: it folded boxes into triangles)
SMOOTH_ANGLE = 35.0

PUTTY, ALU, CHROME = "Prop_SteelPutty", "Prop_Aluminium", "Prop_Chrome"
BACK_W, BACK_Y0, BACK_Y1, BACK_T = 0.30, 1.25, 1.65, 0.003
BOX, BOX_D = 0.102, 0.035
HOOK_Y = 1.42
PLATE_W, PLATE_H, PLATE_T = 0.15, 0.22, 0.002
PLATE_Z0 = BACK_T + BOX_D
SIGN = (0.30, 0.10, 0.003)
SIGN_Y = 1.595
REST = (0.0, 1.4215, 0.068)


def screw(kit, x, y, z, r=0.0045):
    sc.lathe_y  # (keep the import obvious)
    head = sc.cyl(kit, r, 0.0018, (x, y, z + 0.0009), "z", CHROME, verts=16, bevel=0.0006, segments=2, name="screw head")
    sc.box(kit, (r * 1.6, 0.0007, 0.0006), (x, y, z + 0.0019), CHROME, bevel=0.0, name="screw slot")
    return head


def build(kit):
    sc.register()
    # Backer (painted steel), rounded corners, with four screws.
    sc.extrude_xy(kit, sc.rounded_rect(BACK_W, BACK_Y1 - BACK_Y0, 0.006, 4, 0.0, (BACK_Y0 + BACK_Y1) / 2), BACK_T,
                  BACK_T / 2, PUTTY, bevel=0.0008, segments=2, name="backer")
    for x in (-0.13, 0.13):
        for y in (BACK_Y0 + 0.02, BACK_Y1 - 0.02):
            screw(kit, x, y, BACK_T)
    # 4 in square box (galvanised), behind the cover plate.
    sc.box(kit, (BOX, BOX, BOX_D), (0, HOOK_Y, BACK_T + BOX_D / 2), ALU, bevel=0.0015, segments=2, name="4in box")
    for y in (HOOK_Y + BOX / 2 - 0.012, HOOK_Y - BOX / 2 + 0.012):
        sc.cyl(kit, 0.0105, 0.0015, (0, y, BACK_T + BOX_D * 0.5), "x", ALU, verts=18, bevel=0.0, name="knockout ring").scale = (1, 1, 1)
    # Cover plate (brushed steel) with two screws.
    sc.extrude_xy(kit, sc.rounded_rect(PLATE_W, PLATE_H, 0.004, 3, 0.0, HOOK_Y), PLATE_T, PLATE_Z0 + PLATE_T / 2, ALU,
                  bevel=0.0006, segments=2, name="cover plate")
    for y in (HOOK_Y + PLATE_H / 2 - 0.016, HOOK_Y - PLATE_H / 2 + 0.016):
        screw(kit, 0.0, y, PLATE_Z0 + PLATE_T, r=0.004)
    zf = PLATE_Z0 + PLATE_T
    # Plunger switch: chrome bezel, black-anodised (chrome slot) plunger held in by the ring.
    sc.cyl(kit, 0.011, 0.004, (0, HOOK_Y - 0.020, zf + 0.002), "z", CHROME, verts=24, bevel=0.0012, segments=2, name="plunger bezel")
    sc.cyl(kit, 0.0055, 0.008, (0, HOOK_Y - 0.020, zf + 0.008), "z", CHROME, verts=20, bevel=0.0015, segments=2, name="plunger")
    # The one hook: a J of 4 mm rod from a hex boss, cup tip up; the ring rests at REST.
    sc.cyl(kit, 0.007, 0.006, (0, HOOK_Y + 0.012, zf + 0.003), "z", CHROME, verts=6, bevel=0.0008, name="hook boss")
    path = [(0, HOOK_Y + 0.012, zf + 0.004), (0, HOOK_Y + 0.0115, zf + 0.018), (0, HOOK_Y + 0.006, REST[2] - 0.006),
            (0, REST[1] - 0.0018, REST[2]), (0, HOOK_Y + 0.002, REST[2] + 0.0055), (0, HOOK_Y + 0.013, REST[2] + 0.0060)]
    sc.tube(kit, sc.smooth_path(path, 5), 0.002, CHROME, verts=10, name="hook rod")
    # EMT stub from the box's bottom knock-out, with its connector, down below the plate.
    zc = BACK_T + BOX_D / 2
    sc.cyl(kit, 0.0125, 0.008, (0, HOOK_Y - BOX / 2 - 0.004, zc), "y", CHROME, verts=6, bevel=0.0008, name="connector nut")
    sc.cyl(kit, 0.0105, 0.016, (0, HOOK_Y - BOX / 2 - 0.016, zc), "y", ALU, verts=20, bevel=0.0008, name="connector body")
    sc.cyl(kit, 0.00895, 0.060, (0, HOOK_Y - BOX / 2 - 0.054, zc), "y", ALU, verts=20, bevel=0.0, name="EMT stub")
    # The engraved two-line plate (blank until 平面视觉's STAIR KEY art; decal UVs over the whole plate).
    kit.box(sc.BS(*SIGN), sc.B(0, SIGN_Y, BACK_T + SIGN[2] / 2), sc.KEY_PLATE, bevel=0.0006, segments=1, name="key plate sign", uv="decal")
    for x in (-0.135, 0.135):
        screw(kit, x, SIGN_Y, BACK_T + SIGN[2], r=0.003)

    kit.anchor("key_hook", sc.B(*REST))
    kit.anchor("plate_sign", sc.B(0, SIGN_Y, BACK_T + SIGN[2]))
    # Body-only collider: the footprint for Validate (backer + box + plate), the hook and the key in front of it.
    kit.collider(sc.B(0, (BACK_Y0 + BACK_Y1) / 2, (zf) / 2), sc.BS(BACK_W, BACK_Y1 - BACK_Y0, zf))
    kit.tag("interactable", "key_host", "wall_decor", "station")
    kit.meta["keyHostDefault"] = "key_hook"
    kit.meta["hungPose"] = {"ringYawDeg": 120.0, "ringLiftM": 0.0012, "keyTwistDeg": -30.0, "tagTwistDeg": 150.0, "tagSide": "wall (behind the key)",
                            "key": "LookRotation(down, Cross(down, hostForward)): tip down, flats to the room",
                            "tag": "LookRotation(hostForward, up): face to the room, hanging along -Y",
                            "note": "the D1.6 recipe of Kit_KeyHookBoard (same hook height 1.42); M3 applies it"}
    kit.meta["plateSign"] = {"slot": sc.KEY_PLATE, "size": list(SIGN), "text": "STAIR KEY / SILENT ALARM: REMOVAL SIGNALS PANEL (平面视觉's art; blank until it exists)",
                             "layer": "close-reading only (EXAMINE or within ~1 m); never glows"}
    sc.auto_lod_drops(kit, keep=('hook', 'plunger'))
    sc.lods(kit, 3.0, 8.0, 40.0, (3500, 1200, 150))
    sc.station_meta(kit, "wall face at floor level, heights baked in", "body-only collider; KeySpot = key_hook")
