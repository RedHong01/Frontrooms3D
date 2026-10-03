"""Single brass cup hook screwed into the wall: the emptiest key host (spec
10 §4.2; 03 §2.3 A: at most 1 key in 4, red or blue tag only, because there
is no board behind it).

Real-world reference: a 1" brass screw-in cup hook (Ø 3 mm wire, shoulder
disc, J-shaped cup with the tip up), the kind every hardware store sold.
Origin = THE WALL FACE PLANE AT FLOOR LEVEL, CENTRED; front +Z = the room.
The shank and thread are inside the wall. key_hook (0, 1.476, 0.018) is
where the ring's top inner point rests (on the wire, just behind the bend);
the hook stands 0.024 proud, its tip at about 1.486 m.

Budget §9.3: 600 / 200 / - tris (cull at 12 m), LOD 1.5 / 4 / 12 m, no LOD1.
Slot Prop_Brass. Tags interactable, key_host, wall_decor. Render-only.
"""

import interact_key_common as kc
from interact_key_common import U

NAME = "Kit_KeyHook"
LOD1_RATIO, LOD2_RATIO = 200 / 600, None
LOD_DISTANCES = (1.5, 4.0, 12.0)
BUDGET = (600, 200, None)

BRASS = "Prop_Brass"
HOOK = (0.0, 1.476, 0.018)
WIRE_R = 0.0015
SHOULDER_T = 0.0015


def build(kit):
    leg = HOOK[2] - SHOULDER_T + 0.0012          # the ring rests 1.2 mm behind the bend
    objs, hp = kc.cup_hook(kit, (HOOK[0], HOOK[1] - WIRE_R, 0.0), BRASS, wire_r=WIRE_R, leg=leg, bend_r=0.005,
                           rise=0.0045, shoulder_d=0.0068, shoulder_t=SHOULDER_T, verts=12, name="cup hook")
    for o in objs:
        kc.paint_wear(o)
    assert all(abs(a - b) < 1e-9 for a, b in zip(hp, HOOK)), (hp, HOOK)
    kit.anchor("key_hook", U(*hp))
    # Hung pose recipe (solved by the G3 self-check with BVH overlap tests on
    # the real meshes; the facade applies it, spec 10 §4.1 "hung"): the ring's
    # hook_contact goes ringLiftM above key_hook, the ring turned
    # Euler(0, -60, 0) from the host (its tangent 60 deg toward the room); the
    # key hangs tip down from key_contact with its flat normal (+X) square to
    # the room (30 deg of twist in its Ø 4.8 hole) and the tag hangs from
    # tag_contact face to the room (30 deg in its Ø 5 hole on the 2.4 mm tab).
    kit.meta["hungPose"] = {"ringYawDeg": -60.0, "ringLiftM": 0.0014, "keyTwistDeg": -30.0, "tagTwistDeg": -30.0,
                            "key": "LookRotation(down, Cross(down, hostForward)): tip down, flats to the room",
                            "tag": "LookRotation(hostForward, up): face to the room, hanging along -Y"}
    kit.no_collider()
    kit.tag("interactable", "key_host", "wall_decor")
    kc.lod_meta(kit, LOD_DISTANCES, BUDGET)
    kit.meta["keyHostDefault"] = "key_hook"
    lo, hi = kc.eval_bounds_unity(kit)
    assert hi[2] <= 0.10 and lo[2] >= -1e-6, ("proud", lo, hi)
    assert 1.47 <= lo[1] and hi[1] <= 1.495, ("height", lo, hi)
    kc.check_budget(kit, BUDGET[0])
