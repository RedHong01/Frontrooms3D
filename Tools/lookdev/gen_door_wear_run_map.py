"""Door wear for the Run! B kit leaves (游戏视觉, zone doors, Run! polish, 2026-11-06; Documentation/research/double_doors/
30_build.md Part R): the leaf-wear mask of Door_RunAlmond_Map (Prop_SteelAlmond + FrontRooms/Surface _FR_LEAFWEAR), worn by
the almond Kit_DoorLeaf_SteelRun_DA props of FrontRoomsRunDress: the locked side doors (ELECTRICAL ROOM / STAFF ONLY / NOT AN
EXIT) and the held-open compartment leaves (FD1 / FD2). numpy + Pillow only, no downloads, deterministic (seed 61121). It
reuses gen_door_wear_map.py's layout, noise, blob, flake and streak helpers and its channel contract (R grime, G dull, B
burnish, A chip), so the mask lands on the kit leaf exactly as the map members' masks do (u = Z / 1.0, v = Y / 2.11, 1024 x 2048
RGBA linear; the materials' _FR_LeafWearRect (0, 0, 1, 1 / 2.11); face P reads it through wrapU Mirror,
FrontRoomsInteractableKit.DoorParts.MirrorLeafWear, which the dress calls on these leaves).

The Run! leaf: the RN-K kit (F5 15 x 15 in lite at Z 0.1495-0.5305, Y 1.194-1.575 in its pressed frame; 10 in stainless kick
plates Y 0.018-0.272; the locked trim's knob at Z 0.92, Y 0.968 on the corridor face of the side doors). Run! B is a service
corridor of fire compartments with carts (research R.1; C-1's DoorWear_Run): the wear is the C-1 pair's language on the map leaf:
  - a small, patchy hand halo round the knob (a locked door is tried, not pushed) and knuckle marks at the latch stile;
  - cart and hand-truck rubber streaks and grey-primer chips just above the kick plate, more toward the latch half;
  - a mop line at the foot, a duller lower half, the latch edge a little darker at hand height.
The plates and the lite's pressed frame cover their rects; the hinge strip stays plain (the edges and the stile's arc sample it).

Run (from the project root):
  python Tools/lookdev/gen_door_wear_run_map.py Assets/Resources/Surfaces/Textures           -> DoorWear_RunMap.png
  python Tools/lookdev/gen_door_wear_run_map.py <dir> preview                                -> + preview_DoorWear_RunMap.png
"""

import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gen_door_wear_map as g  # noqa: E402  (layout, helpers, writer: never edited here)

X, Y = g.X, g.Y
LITE_F5 = (0.1495 - 0.019, 0.5305 + 0.019, 1.1938 - 0.019, 1.5748 + 0.019)
KNOB = (0.920, 0.968)
NAME, SEED = "DoorWear_RunMap", 61121


def knob_zone(rng):
    """A locked door's hand marks: palms and fingertips round the knob and its escutcheon, patchy, a few smudges."""
    d = np.hypot((X - KNOB[0]) * 1.0, (Y - KNOB[1]) * 1.3)
    ring = g.smoothstep(0.028, 0.050, d) * (1.0 - g.smoothstep(0.06, 0.13, d))
    patchy = g.smoothstep(0.30, 0.80, g.fbm(rng, 0.05, 4))
    smudge = g.blobs(rng, 10, (0.80, 0.985), (0.86, 1.12), (0.007, 0.018), aspect=(0.6, 1.4), angle_jitter=1.5)
    smudge *= g.smoothstep(0.035, 0.05, d)
    return np.clip(ring * (0.25 + 1.0 * patchy) + 0.45 * smudge, 0.0, 1.0)


def run(rng):
    m = np.zeros((g.H, g.W, 4))
    h = knob_zone(rng)
    m[..., 0] += 0.55 * h
    m[..., 2] += 0.30 * h
    # knuckle marks on the latch stile at knob height
    k = g.blobs(rng, 12, (0.915, 0.983), (0.82, 1.10), (0.004, 0.009), aspect=(0.7, 1.3))
    m[..., 0] += 0.40 * k
    m[..., 1] += 0.40 * k
    # the latch edge darker at hand height
    e = np.exp(-(g.LEAF_Z1 - X) / 0.012) * g.band(0.88, 1.25, 0.07) * (0.6 + 0.8 * g.fbm(rng, 0.05, 3))
    m[..., 0] += 0.45 * e
    # carts: rubber streaks and grey-primer chips just above the kick plate, toward the latch half
    bias = 0.45 + 0.55 * g.smoothstep(0.30, 0.90, X)
    t = g.streaks(rng, 24, (0.05, 0.95), (0.280, 0.400), (0.05, 0.22), (0.003, 0.010), slope=0.08)
    m[..., 0] += 0.65 * t * bias
    m[..., 1] += 0.30 * t
    m[..., 3] += g.flakes(rng, 30, (0.04, 0.97), (0.280, 0.360), (0.0050, 0.0170), aspect=(1.5, 3.2), cluster=(1, 4)) * bias
    m[..., 3] += 0.8 * g.flakes(rng, 10, (0.06, 0.94), (0.40, 0.95), (0.0030, 0.0080), aspect=(1.3, 2.6), cluster=(1, 2))
    # the mop line at the foot (over the plate's bottom margin) and a duller, greyer lower half
    edge = 0.020 + 0.004 * (g.fbm(rng, 0.06, 3) - 0.5)
    mop = g.smoothstep(0.014, 0.016, Y) * (1.0 - g.smoothstep(edge - 0.002, edge + 0.002, Y))
    m[..., 0] += 0.45 * mop * (0.7 + 0.3 * g.fbm(rng, 0.02, 3))
    m[..., 1] += 0.14 * (1.0 - g.smoothstep(0.35, 1.0, Y)) * g.fbm(rng, 0.25, 3)
    m[..., 0] += 0.07 * (1.0 - g.smoothstep(0.3, 0.9, Y)) * g.fbm(rng, 0.18, 3)
    m = np.clip(m, 0.0, 1.0)
    # the plates cover their rects; the lite's pressed frame faintly; the hinge strip and outside the leaf plain
    keep = np.ones((g.H, g.W))
    keep[g.rect_in(g.KICK, 0.002)] = 0.0
    keep[g.rect_in(LITE_F5, 0.0)] *= 0.35
    outside = (X < g.LEAF_Z0) | (X > g.LEAF_Z1) | (Y < g.LEAF_Y0) | (Y > g.LEAF_Y1)
    keep[outside] = 0.0
    keep *= g.smoothstep(0.006, 0.030, X)
    return m * keep[..., None]


def main(argv):
    out = argv[0] if argv else "Assets/Resources/Surfaces/Textures"
    preview = len(argv) > 1 and argv[1] == "preview"
    os.makedirs(out, exist_ok=True)
    m = run(np.random.default_rng(SEED))
    g.write(os.path.join(out, NAME + ".png"), m)
    print("%s: R %.3f G %.3f B %.3f A %.3f (mean); max %.2f %.2f %.2f %.2f" % ((NAME,) + tuple(m[..., i].mean() for i in range(4)) + tuple(m[..., i].max() for i in range(4))))
    if preview:
        from PIL import Image
        tiles = [np.flipud((np.clip(m[..., i], 0, 1) * 255).astype(np.uint8)) for i in range(4)]
        Image.fromarray(np.concatenate(tiles, axis=1), "L").resize((g.W, g.H // 4)).save(os.path.join(out, "preview_" + NAME + ".png"))


if __name__ == "__main__":
    main(sys.argv[1:])
