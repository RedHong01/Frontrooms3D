"""Create the inspectable FrontRooms runtime map in the UE editor.

This is intentionally an editor automation script.  The game remains driven by
the deterministic AFrontRoomsRuntimeMap actor in packaged Win64 builds, while
the saved .umap gives reviewers a stable viewport and startup asset.
"""

import unreal


MAP_PATH = "/Game/FrontRooms/Maps/FrontRoomsRuntime"


def main():
    unreal.EditorAssetLibrary.make_directory("/Game/FrontRooms/Maps")
    if unreal.EditorAssetLibrary.does_asset_exist(MAP_PATH):
        world = unreal.EditorLevelLibrary.load_level(MAP_PATH)
    else:
        world = unreal.EditorLevelLibrary.new_level(MAP_PATH)
    if not world:
        raise RuntimeError("Could not create FrontRoomsRuntime level")

    actor_class = unreal.load_class(None, "/Script/FrontRooms.FrontRoomsRuntimeMap")
    if not actor_class:
        raise RuntimeError("AFrontRoomsRuntimeMap class is unavailable")

    actor = next(
        (candidate for candidate in unreal.EditorLevelLibrary.get_all_level_actors()
         if candidate.get_class() == actor_class),
        None,
    )
    if actor is None:
        actor = unreal.EditorLevelLibrary.spawn_actor_from_class(
            actor_class, unreal.Vector(0.0, 0.0, 0.0)
        )
    if not actor:
        raise RuntimeError("Could not spawn AFrontRoomsRuntimeMap")

    actor.set_actor_label("FrontRoomsRuntimeMap")
    actor.set_editor_property("seed", 20261001)
    # Unreal Python strips the C++ boolean `b` prefix from reflected names.
    actor.set_editor_property("generate_on_begin_play", True)
    # Rebuild deliberately so a changed Unity contract or imported asset set
    # is reflected in the saved review map. Packaged Win64 runs then reuse
    # these serialized components without rebuilding them.
    if hasattr(actor, "rebuild_map"):
        actor.rebuild_map()
    else:
        actor.build_map()
    unreal.EditorLevelLibrary.save_current_level()
    unreal.log("FrontRooms runtime map saved: {}".format(MAP_PATH))


main()
