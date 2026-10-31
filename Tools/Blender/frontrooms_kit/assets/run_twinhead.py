"""Twin-head battery emergency lighting unit, US, c. 1975-1992: the cabinet
(Kit_EmergencyTwinHead). PRODUCTION hero kit for Run! B (EMERGENCY POWER),
rebuilt 2026-10-22 from the r8 look-dev module on main's kitlib (hand-built LOD0/1/2).

A stamped-steel cabinet in almond baked enamel holding a 6 V lead-calcium battery
and charger: a front cover with a fine seam, a red neon AC ON pilot, a PUSH TO TEST
button, a legend plate, lanced side louvres, bottom vent slots, two swivel bosses on
top for the lamp-head yokes (Kit_EmergencyTwinHead_Yoke) and a 1/2 in knockout with a
locknut for the conduit feed. No brand, no date.

Size: 338 x 191 x 114 mm (10_plan.md §1.1). ORIGIN = the wall point under the cabinet's
bottom centre: the back face is the wall plane (Blender y = 0, the body at y < 0), the
bottom at z = 0. FRONT = kit -Y (Unity +Z). Anchors: mount_l / mount_r (boss tops,
where a yoke's foot sits), feed (knockout centre), pilot (the neon jewel's front).
Budget LOD0 / LOD1 / LOD2: 3,800 / 1,500 / 450 tris; lodDistances 3.6 / 10 / never (F6, stage C).
Slots (4): Prop_SteelAlmond, Prop_PlasticBlack, Run_NeonPilot, Run_UnitLegend.
The legend atlas (Run_UnitLegend_A, 1024 x 1024): row 0 nameplate, row 1 AC ON,
row 2 PUSH TO TEST, row 3 rating line (1 x 4 cells).
"""

import kitlib
import _run_b_common as B

NAME = "Kit_EmergencyTwinHead"
SMOOTH_ANGLE = 40.0
BUDGET = (3800, 1500, 450)

STEEL = "Prop_SteelAlmond"
BLACK = "Prop_PlasticBlack"
PILOT = "Run_NeonPilot"
LEGEND = "Run_UnitLegend"

W, H, D = 0.338, 0.191, 0.114
FRONT = -D
MOUNT_X = 0.112
MOUNT_Y = -D * 0.52
BOSS_H = 0.010


def _level(kit, lv):
    # Shell: one deep-drawn shell with a 9 mm edge radius.
    sw, ss = B.bev(0.009, lv)
    if lv == 0:
        ss = 3
    kit.box((W, D, H), (0, -D / 2, H / 2), STEEL, bevel=sw, segments=ss, name="shell")
    # Front cover, 1.2 mm proud with its own round: the seam reads as a fine line.
    if lv < 2:
        B.box(kit, (W - 0.024, 0.003, H - 0.024), (0, FRONT - 0.0012, H / 2), STEEL, w=0.0015, lv=lv, name="front cover")
    cover = FRONT - 0.0027 if lv < 2 else FRONT

    # Side louvres: four lanced slots per end under pressed hoods.
    if lv < 2:
        for sx in (-1, 1):
            for i in range(4):
                z = 0.066 + 0.025 * i
                # The slot is the dark opening under the hood: no edge of its own to bevel.
                kit.box((0.004, 0.064, 0.0068), (sx * (W / 2 - 0.0012), -D * 0.52, z), BLACK, bevel=0.0, segments=0, name="louvre slot")
                if lv == 0:
                    B.box(kit, (0.0045, 0.068, 0.0032), (sx * (W / 2 + 0.0015), -D * 0.52, z + 0.0054), STEEL, w=0.0012, lv=lv, name="louvre hood")
        # Bottom vents (a standing player sees the underside): five slots.
        for i in range(5):
            x = -0.11 + 0.055 * i
            kit.box((0.044, 0.008, 0.004), (x, -D * 0.45, 0.0012), BLACK, bevel=0.0, segments=0, name="bottom slot")

    # Pilot (viewer's left): black bezel, red neon jewel.
    px, pz = -0.100, 0.132
    if lv < 2:
        B.cyl(kit, 0.0100, 0.005, (px, cover - 0.0022, pz), BLACK, lv=lv, rot=(90, 0, 0), w=0.0012, name="pilot bezel", lo=16)
    jewel = [(0.0068, 0.0), (0.0067, 0.0022), (0.0055, 0.0042), (0.0033, 0.0054), (0.0, 0.0059)]
    rings = None if lv == 0 else ([0, 2, 4] if lv == 1 else [0, 4])
    B.lathe(kit, jewel, (px, cover - (0.0045 if lv < 2 else 0.0), pz), PILOT, lv=lv, rot=(90, 0, 0), name="pilot jewel", lo=16, rings=rings)
    # Test switch (viewer's right): bezel ring and a round push cap.
    tx, tz = 0.100, 0.132
    if lv < 2:
        B.cyl(kit, 0.0108, 0.004, (tx, cover - 0.0018, tz), BLACK, lv=lv, rot=(90, 0, 0), w=0.001, name="test bezel", lo=16)
    cap = [(0.0072, 0.0), (0.0072, 0.0050), (0.0064, 0.0068), (0.0040, 0.0076), (0.0, 0.0078)]
    B.lathe(kit, cap, (tx, cover - (0.0038 if lv < 2 else 0.0), tz), BLACK, lv=lv, rot=(90, 0, 0), name="test cap", lo=16, rings=rings)

    # Legend plate: a thin satin plate riveted low on the cover, the nameplate printed on it.
    lg = cover - 0.0004
    if lv < 2:
        B.box(kit, (0.214, 0.0008, 0.058), (0.0, cover - 0.0004, 0.050), STEEL, w=0.0004, lv=lv, name="legend plate")
        lg = cover - 0.0010
        if lv == 0:
            for sx in (-1, 1):
                B.lathe(kit, [(0.0018, 0.0), (0.0016, 0.0005), (0.0, 0.0008)], (sx * 0.100, cover - 0.0008, 0.050), STEEL, lv=lv, rot=(90, 0, 0), name="rivet", lo=8)
    kit.quad(0.200, 0.050, (0.0, lg, 0.050), LEGEND, facing="-y", uv="decal", uv_rect=kitlib.Kit.atlas_cell(0, 1, 4), name="nameplate")
    kit.quad(0.052, 0.013, (px, cover - 0.0004, 0.110), LEGEND, facing="-y", uv="decal", uv_rect=kitlib.Kit.atlas_cell(1, 1, 4), name="legend ac on")
    kit.quad(0.056, 0.014, (tx, cover - 0.0004, 0.110), LEGEND, facing="-y", uv="decal", uv_rect=kitlib.Kit.atlas_cell(2, 1, 4), name="legend test")
    if lv == 0:
        kit.quad(0.120, 0.009, (0.0, cover - 0.0004, 0.168), LEGEND, facing="-y", uv="decal", uv_rect=kitlib.Kit.atlas_cell(3, 1, 4), name="legend rating")
        # Cover screws: four almond pan heads.
        for sx in (-1, 1):
            for sz in (0.022, H - 0.022):
                B.lathe(kit, [(0.0038, 0.0), (0.0037, 0.0008), (0.0028, 0.0016), (0.0, 0.0019)], (sx * (W / 2 - 0.022), cover + 0.0003, sz), STEEL, lv=lv, rot=(90, 0, 0), name="screw", lo=12)

    # Top: two swivel bosses for the yokes, and the centre knockout with its locknut.
    for sx in (-1, 1):
        B.cyl(kit, 0.016, BOSS_H, (sx * MOUNT_X, MOUNT_Y, H + BOSS_H / 2 - 0.001), STEEL, lv=lv, w=0.002, name="swivel boss", lo=16)
        if lv < 2:
            B.cyl(kit, 0.0105, 0.003, (sx * MOUNT_X, MOUNT_Y, H + BOSS_H - 0.0005), BLACK, lv=lv, w=0.0008, name="boss collar", lo=16)
    if lv < 2:
        B.cyl(kit, 0.0125, 0.0025, (0.0, -0.030, H + 0.0008), STEEL, lv=lv, w=0.0008, name="knockout ring", lo=16)
        # 1/2 in locknut: a 12-point serrated ring, drawn as a hex here (it is under the conduit).
        kit.cylinder(0.0145, 0.004, (0.0, -0.030, H + 0.0035), STEEL, verts=6 if lv else 12, bevel=0.0008 if lv == 0 else 0.0, segments=2 if lv == 0 else 0, name="locknut")


def build(kit):
    B.levels(kit, _level)
    kit.no_collider()
    kit.anchor("mount_l", (-MOUNT_X, MOUNT_Y, H + BOSS_H))
    kit.anchor("mount_r", (MOUNT_X, MOUNT_Y, H + BOSS_H))
    kit.anchor("feed", (0.0, -0.030, H + 0.0055))
    kit.anchor("pilot", (-0.100, FRONT - 0.0105, 0.132))
    kit.tag("wall_unit", "run", "run_b")
    B.lod_meta(kit, (3.6, 10.0, 0.0), BUDGET)  # F6 (stage C, 2026-10-31): pops <= 2 %; was (3.0, 10.0, 0.0)
