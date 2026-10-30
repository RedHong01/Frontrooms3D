"""Kit_KeyCabinet_Putty: the building's key cabinet with its door SHUT (30L S14, §10.2 S14; 03_palette.md key cabinet
ADJUST: putty or light grey; 01 §6 door art KEY CONTROL / ALL KEYS MUST BE SIGNED OUT (平面视觉; blank here);
plan 70 P07).

The Kit_KeyCabinet box (0.36 x 0.46 x 0.084, y 1.24-1.70) in putty enamel with a separate shut door part, a cam lock,
a piano hinge, a door label holder and real LOD1/2. No key bunches are visible (door shut). ORIGIN = THE WALL FACE AT
FLOOR LEVEL, front +Z, heights baked in. Budget 6,804 / 2,300 / 250. Slots Prop_SteelPutty, Prop_Chrome,
Prop_StationPaper (door label, atlas cell key_control), Prop_PlasticBlack. No collider (module prop noCollider).
"""

import _station_common as sc

NAME = "Kit_KeyCabinet_Putty"
LODS = (0.94, 0.90)   # hard surface: LODs drop sub-pixel parts only (no collapse: it folded boxes into triangles)
SMOOTH_ANGLE = 35.0

PUTTY, CHROME = "Prop_SteelPutty", "Prop_Chrome"
W, H, D = 0.36, 0.46, 0.084
Y0 = 1.24
YC = Y0 + H / 2


def build(kit):
    sc.register()
    # Body: a shallow drawn-steel box, rolled front edges.
    sc.box(kit, (W, H, D - 0.016), (0, YC, (D - 0.016) / 2), PUTTY, bevel=0.004, segments=3, name="cabinet body")
    # Shut door: a pan door with a 10 mm return, slightly proud of the body.
    zd = D - 0.016 + 0.008
    sc.extrude_xy(kit, sc.rounded_rect(W - 0.004, H - 0.004, 0.005, 4, 0, YC), 0.016, zd, PUTTY, bevel=0.0035, segments=3, name="door (shut)")
    zf = zd + 0.008
    # Piano hinge along the left edge.
    for k in range(12):
        y = Y0 + 0.012 + (k + 0.5) * (H - 0.024) / 12
        sc.cyl(kit, 0.0035, (H - 0.024) / 12 - 0.0015, (W / 2 - 0.001, y, zd - 0.006), "y", CHROME, verts=12, bevel=0.0003, name="hinge knuckle")
    # Cam lock (chrome face, keyway) and a small pull lip below it.
    sc.cyl(kit, 0.0105, 0.004, (-W / 2 + 0.032, YC + 0.03, zf + 0.002), "z", CHROME, verts=28, bevel=0.0012, segments=2, name="cam lock face")
    sc.box(kit, (0.0012, 0.009, 0.0008), (-W / 2 + 0.032, YC + 0.03, zf + 0.0042), "Prop_PlasticBlack", bevel=0.0, name="keyway")
    sc.box(kit, (0.05, 0.006, 0.012), (-W / 2 + 0.032, YC - 0.012, zf + 0.004), PUTTY, bevel=0.002, segments=2, name="pull lip")
    # Door label holder (chrome frame) with the KEY CONTROL card (paper atlas; blank until 平面视觉's art).
    lw, lh = 0.20, 0.07
    ly = YC + 0.12
    kit.frame((lw + 0.008, lh + 0.008), (lw, lh), 0.002, sc.B(0, ly, zf + 0.001), CHROME, bevel=0.0004, segments=1, name="label holder")
    sc.quad(kit, lw, lh, (0, ly, zf + 0.0006), sc.PAPER, "+z", uv_rect=sc.paper_rect("key_control"), name="key control card")
    # Vent louvres (period key cabinets had none; a pressed rib pattern stiffens the door instead).
    for k in range(3):
        sc.box(kit, (W - 0.08, 0.004, 0.0015), (0, Y0 + 0.07 + k * 0.012, zf + 0.0005), PUTTY, bevel=0.0006, segments=1, name="door rib")
    kit.no_collider()
    kit.collider(sc.B(0, YC, D / 2), sc.BS(W, H, D))
    kit.meta.pop("noCollider", None)
    kit.tag("wall_decor", "station")
    kit.meta["paperCells"] = {"key_control": sc.PAPER_CELLS["key_control"]}
    sc.auto_lod_drops(kit)
    sc.lods(kit, 3.0, 8.0, 40.0, (6804, 2300, 250))
    sc.station_meta(kit, "wall face at floor level, heights baked in", "door shut; KEY CONTROL card from the paper atlas")
