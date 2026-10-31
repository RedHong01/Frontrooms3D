"""NOT AN EXIT, spray-stencilled on a locked side door (US, 1975-1992): one alpha-clipped
decal quad, 560 x 140 mm (76 mm Heros Bold Condensed stencil letters with bridges;
Run_StencilNotExit_A, RGBA, through 平面视觉 before shipping). PRODUCTION part of the
Run! B kit (Kit_StencilNotExit), listed with the safety plates in 10_plan.md §1.2.
ORIGIN = the door-face point at the stencil centre; the quad floats 0.5 mm off it.
FRONT = kit -Y (Unity +Z). 2 tris, no LOD; lodDistances 0 / 0 / 25.
Slots: Run_StencilNotExit (URP Lit, alpha clip, as Prop_StencilBlack).
"""

import _run_b_common as B

NAME = "Kit_StencilNotExit"
SMOOTH_ANGLE = 30.0
BUDGET = (2, 0, 0)


def build(kit):
    kit.quad(0.56, 0.14, (0, -0.0005, 0), "Run_StencilNotExit", facing="-y", uv="decal", name="stencil")
    kit.no_collider()
    kit.tag("wall_decor", "run", "run_b", "sign")
    B.lod_meta(kit, (0.0, 0.0, 25.0), BUDGET)
