"""The Run! receding end's overhead track, one 3 m section (71_run_room.md s.5.1 and s.4.8 item 1: "the track is
surface-mounted on the exposed soffit from the 9 m frame to the end wall, in view from the arch", the honest tell).
PRODUCTION kit Kit_RunRecede_Track_3m, hand-built LOD0/1/2.

A steel C-channel in the maker's baked almond enamel (as period partition track was finished; a galvanized one read black on
the dark soffit, the corridor having no reflection probes), 100 x 65 mm, 3 mm wall, open downward with a 28 mm slot between two lips (the trolleys'
running surfaces), bolted through its web to the concrete soffit (wedge anchors every 600 mm, their nuts inside the
channel, out of sight), a bolted splice plate pair at the section's start joint. Like a period folding-partition or
accordion-door trackway (motion.txt M7: overhead trackways, no floor track), surface-mounted because the Run! soffit is
exposed concrete (U11).
ORIGIN = the section's start on the soffit plane at the track's centre line; the track runs +Z for 3.0 m and hangs below
(Unity y -0.065 .. 0). Budget 600 / 120 / 12; lodDistances 6 / 18 / 46. Slot: Prop_SteelAlmond. No collider.
"""

import _run_b_common as B
import _run_recede_common as R

NAME = "Kit_RunRecede_Track_3m"
SMOOTH_ANGLE = 35.0
BUDGET = (600, 120, 12)
GALV = "Prop_SteelAlmond"   # factory enamel (galvanized read black on the dark soffit: no probes, metallic)
L = 3.0


def _level(kit, lv):
    t = 0.003
    if lv == 2:
        R.rbox(kit, -0.050, 0.050, -0.065, 0.0, 0.0, L, GALV, name="track")
        return
    bw = 0.0008 if lv == 0 else 0.0
    R.rbox(kit, -0.050, 0.050, -t, 0.0, 0.0, L, GALV, bw, 1, name="track web")
    for s in (-1, 1):
        R.rbox(kit, s * 0.047, s * 0.050, -0.065, 0.0, 0.0, L, GALV, bw, 1, name="track side")
        R.rbox(kit, s * 0.014, s * 0.050, -0.065, -0.065 + t, 0.0, L, GALV, bw, 1, name="track lip")
        R.rbox(kit, s * 0.050, s * 0.054, -0.058, -0.010, -0.075, 0.075, GALV, bw, 1, name="splice plate")
        if lv == 0:
            for (y, z) in ((-0.022, -0.045), (-0.046, -0.045), (-0.022, 0.045), (-0.046, 0.045)):
                R.rcyl(kit, "x", 0.0055, 0.003, (s * 0.0555, y, z), GALV, 8, name="splice bolt head")


def build(kit):
    B.levels(kit, _level)
    kit.no_collider()
    kit.tag("run", "run_recede", "ceiling")
    kit.min_ceiling = 0.0
    B.lod_meta(kit, (6.0, 18.0, 46.0), BUDGET)
    kit.meta["runRecede"] = "card 2 head first, 2026-11-04 (71_run_room.md s.5.1); inert until rule 5b"
