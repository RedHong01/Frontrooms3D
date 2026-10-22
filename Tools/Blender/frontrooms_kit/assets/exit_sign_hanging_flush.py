"""Kit_ExitSign_HangingFlush: the double-face EXIT sign close under a Low ceiling (P2).

Exit-sign workflow, spec Documentation/research/exit_sign/10_spec.md §4.1
(mount D: housing 2.163-2.375 under a 2.40 m ceiling). The same housing and
faces as Kit_ExitSign_Hanging (exit_sign_hanging.build_hanging), with the
housing top 25 mm under the ceiling: the canopy (16 mm), 9 mm of stem and
the lock nuts; no collars (no room for them).

Origin: the centre of the canopy on the ceiling plane; the sign hangs into -Y;
faces +-Z. Tags exit_sign + ceiling. VARIANT Kit_ExitSign_HangingFlush_Dead.
Sidecar minCeiling 2.27 (0.237 m to the housing bottom + 2.032 m headroom = 2.269,
rounded up to the cm; set in exit_sign_hanging.build_hanging).
Budget §4.3: 5,800 +-15 % / LOD1 1,800; lodDistances [5, 0, 0].
"""

import exit_sign_common as es
import exit_sign_geom as g
import exit_sign_hanging as hang

NAME = "Kit_ExitSign_HangingFlush"
VARIANTS = {"Kit_ExitSign_HangingFlush_Dead": {es.DIFFUSER: es.DIFFUSER_DEAD}}
BUDGET = (5800, 1800, 300)
LOD1 = BUDGET[1] / BUDGET[0]
SMOOTH_ANGLE = 40.0


def build(kit):
    hang.build_hanging(kit, g.FLUSH_DROP, BUDGET, collars=False, ceiling_m=2.4)
