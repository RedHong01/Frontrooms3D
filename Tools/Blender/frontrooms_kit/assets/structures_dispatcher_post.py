"""Kit_DispatcherPost: the slide's dispatcher console (CCTV monitor, ride counter, emergency stop, intercom handset, whistle hook) on the start room's shelf footprint (structures G2-d 2026-11-10, package structures_pool_finish). Built in _struct_pool_finish.dispatcher_post."""

import _run_b_common as B
import _struct_common as S
import _struct_pool_finish as F

NAME = "Kit_DispatcherPost"
SMOOTH_ANGLE = 40.0
BUDGET = (8700, 2700, 450)


def build(kit):
    B.levels(kit, F.dispatcher_post)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, F.DIST_POST, BUDGET, tags=("structures", "slide", "p15"))
