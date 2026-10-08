# 20 — In-engine renders of the interactables kit

Status: IN PROGRESS (render stage, resumed 2026-10-07 18:1x after the 17:46 usage stop). This file is updated as each group is checked.

- **Clone:** `W/proj_int` (`W` = `/Users/redwang/FrontRoomsVisualWork`). Made at 16:31 on 2026-10-07 from `W/proj_audit`, rsynced from Red's main, local patch script run (no-op), tools installed. Main moved after that only in touch controls, HUD key glyph and docs (commits 16f520e4, 5e6e7f96, 07d2c843, a5262fb6), none of which feed these frames. Every `Kit_*` FBX/JSON in the clone equals main's, except `Kit_KeyCabinet.fbx` (see §0.2).
- **Harness:** `W/proj_int/Assets/Editor/Rendering/FrontRoomsInteractablesLookdev.cs` (clone-only; a copy is saved next to this file as `harness/FrontRoomsInteractablesLookdev.cs.txt`). Post-processing: `W/int_work/int_post.py` (copy: `harness/int_post.py.txt`).
- **Nothing in Red's project was changed** except files under `Documentation/research/interactables/` (this report, `images/`, `harness/`).
