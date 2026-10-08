"""An opened carton of 24-inch Office carpet tiles with a roll of beige
masking tape on top (level transitions, V2 Renovation).

Real-world reference: a plain brown corrugated carton (no print) for 18-20
modular carpet tiles, 0.62 x 0.62 x 0.20 m; the top flaps folded back, the
stack of tiles inside, the top one lifted and turned. Beige crepe masking
tape (48 mm) on a card core. Unbranded.

Budget ~1.5 k tris. Slots: Cardboard, Office_CarpetTile_Loose, TapeBeige.
"""

import math
import random

import kitlib

NAME = "Kit_TransTileBox"
LOD1 = 0.5
SMOOTH_ANGLE = 35.0

kitlib.register_slot("Office_CarpetTile_Loose", (0.30, 0.33, 0.36), 0.9)
kitlib.register_slot("Prop_TapeBeige", (0.78, 0.70, 0.55), 0.7)
BOX, TILE, TAPE = "Prop_Cardboard", "Office_CarpetTile_Loose", "Prop_TapeBeige"
S, H, T = 0.625, 0.20, 0.004


def build(kit):
    rng = random.Random(1973)
    # Carton: base and four walls, corrugated board 4 mm.
    kit.box((S, S, T), (0, 0, T / 2), BOX, bevel=0.0015, segments=1, name="base")
    kit.box((S, T, H), (0, -S / 2 + T / 2, H / 2), BOX, bevel=0.0015, segments=1, name="wall front")
    kit.box((S, T, H), (0, S / 2 - T / 2, H / 2), BOX, bevel=0.0015, segments=1, name="wall back")
    kit.box((T, S - 2 * T, H), (-S / 2 + T / 2, 0, H / 2), BOX, bevel=0.0015, segments=1, name="wall left")
    kit.box((T, S - 2 * T, H), (S / 2 - T / 2, 0, H / 2), BOX, bevel=0.0015, segments=1, name="wall right")
    # Flaps folded back over the outside (long pair) and standing open (short pair).
    fl = S / 2
    for sy in (-1, 1):
        # Front (-Y, toward the room): folded out and down, tip resting just above the floor
        # (the flap, 0.31 m, is longer than the wall, 0.20 m). Back (+Y, against the wall):
        # standing up, leaning back at most 4 deg, so it stays inside the 0.05 m wall gap.
        th = math.radians(rng.uniform(118, 127) if sy < 0 else rng.uniform(-3, 4))
        d = (0.0, sy * math.sin(th), math.cos(th))
        hinge = (0.0, sy * (S / 2 + T / 2), H)
        alpha = 90 - math.degrees(th) if sy > 0 else 90 + math.degrees(th)
        kit.box((S - 0.01, fl, T), tuple(hinge[i] + d[i] * fl / 2 for i in range(3)), BOX, bevel=0.0015, segments=1,
                rot=(alpha, 0, 0), name="flap long")
    for sx in (-1, 1):
        th = math.radians(rng.uniform(18, 38))              # short flaps half open, leaning out (<= 0.19 m past the side)
        d = (sx * math.sin(th), 0.0, math.cos(th))
        hinge = (sx * (S / 2 + T / 2), 0.0, H)
        beta = math.degrees(th) - 90 if sx > 0 else -90 - math.degrees(th)
        kit.box((fl, S - 0.02, T), tuple(hinge[i] + d[i] * fl / 2 for i in range(3)), BOX, bevel=0.0015, segments=1,
                rot=(0, beta, 0), name="flap short")
    # The stack inside and the top tile lifted and turned.
    n = 16
    th = 0.0072
    kit.box((S - 2 * T - 0.006, S - 2 * T - 0.006, n * th), (0, 0, T + n * th / 2), TILE, bevel=0.001, segments=1, name="stack")
    kit.box((0.6, 0.6, th), (0.03, -0.04, T + n * th + th / 2 + 0.012), TILE, bevel=0.001, segments=1, rot=(0, 2.0, 9), name="top tile")
    # Masking tape roll on the top tile.
    tz = T + n * th + th + 0.012 + 0.024
    kit.lathe([(0.038, -0.024), (0.0505, -0.024), (0.0505, 0.024), (0.038, 0.024)], (0.12, 0.10, tz), TAPE, verts=28,
              rot=(0, 0, 0), close_top=False, close_bottom=False, name="tape")
    kit.lathe([(0.0365, -0.0242), (0.0385, -0.0242), (0.0385, 0.0242), (0.0365, 0.0242), (0.0365, -0.0242)], (0.12, 0.10, tz), BOX, verts=28,
              close_top=False, close_bottom=False, name="core")
    kit.collider((0, 0, H / 2), (S, S, H))
    kit.tag("renovation", "work_prop")
