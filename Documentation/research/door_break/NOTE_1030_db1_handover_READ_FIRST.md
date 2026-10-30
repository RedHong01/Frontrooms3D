# READ FIRST: DB1 handover for the door-break rethink, 2026-10-30

The DB1 options agent was stopped at 10:5x because Red widened the task: rethink the whole door-break animation from research, prototype it, and send one video. This is its handover, relayed by the 游戏视觉 session. Its own 40_for_red draft recommended B and was not shown to Red; it is in W/_partial/db1_page_20261030/40_for_red_DRAFT_stopped.md.

## Renders you can reuse (landed D1.6 kit, Blender)
- 52 PNGs at 1600×900, 96 spp, in `W/door_break/DB1_page/renders/final/`, named `db1_<A|B|C2>_L0F_burst_<stage>_<view>_<light>.png`. Stages run S0–S4; views are V1 76°, V1Zf44 ×1.8 and V6 12 m, plus close-ups.
- 24 Shot sheets in `research/door_break/images/db1_*.jpg`. They carry a "FOR RED" chip, so relabel them before reuse.
- The C2 S4 ×1.8 frame was never made.
- The scene tooling is in `W/door_break/DB1_page/scripts/`:
  - `patch_dirs.py` generates `scene2.py` from the C2 r3 scene and adds `--dir A|B|C`;
  - `queue*.sh` runs the renders;
  - `publish_db1.py` builds the Shot sheets.
- Run line: `Blender -b --factory-startup --python scripts/scene2.py -- --dir B --member L0F --case burst --stage D2 --jobs V1:76:LA --res 1600x900 --spp 96 --haze off --out <dir>`.
- Timings: A and B take 25–190 s per stage; C2's D2 and FINAL take 4–5 min each.
- The Figma section 3769:8635 (board 3769:8549, x 59977, y 2000) is labelled "DRAFT STOPPED · NOT FOR RED", with 24 empty slots. Reuse or remove it.

## What the three directions show (Level 0 free door, Relay on the far side, five blows)
- **A · jamb split.** M–D2 read as an intact door from 3 m. FINAL: a 68 × 420 mm jamb strip and the strike break out toward the player.
- **B · lock hole.** The rose and lever loosen, then fall. D2 opens a 54 mm through-hole, torn out to about 96 mm along the grain. FINAL: the leaf tears from the hole to the edge. Only B's stages read from 3 m; at 12 m the hole is about 3 px.
- **C2 · the wall gives.** Drywall bays burst around the frame, with 29 pieces. The r2 review said NOT READY, and the r3 fix was never reviewed.

## Facts about the landed D1.6 leaf that a break needs
- `Kit_DoorLeaf_Veneer_DA` LOD0 has 1,434 verts in 16 pieces with 96 open edges, and only Door_Veneer + Prop_Brass. The slab is a **hollow shell: no core, edge band or veneer layer** in geometry or materials. A break needs authored core and cut faces.
- The faces are a few large quads. Any crack or dish needs new topology (r3 cuts a 12 mm grid) or a pre-fractured variant.
- **Damage maps need their own UV set.** r3 adds UVd = (Z, Y/2.1) in the scene; the kit has none.
- In Blender, EXACT booleans only work with hole-tolerant on. LOD1 (538 verts) and LOD2 (76 verts) are closed and clean.
- Lockset centre: Y 1.000, Z 0.920 (75 mm from the latch edge). Latch at (0, 1.0, 0.995), strike at (0, 1.0, 0.998), turned 180°.
- `Kit_DoorFrame_Wood_DA` is one mesh, and its brush fins seal the gaps. The jamb lining is a 2 mm skin over the map's wall end, so a split needs r3's jamb window/strip geometry or the 25 mm recess (`10_design.md` §7.1 item 8).
- **The broken-leaf pose is wrong for pivots.** The game turns the broken leaf 3° about the top of its hinge edge, which suits butt hinges. r3's proposal: the bottom arm tears off the leaf foot, so the leaf hangs from its top pivot with the latch corner on the carpet. That would be a contract line for 关卡设计.
- **Hooks today:**
  - `DoorBroken`, `DoorBrokenFrom(door, from, swingSide)`, `DoorBrokenTried`;
  - `MapHunter.DoorBlow(Vector3)`, which gives a position only, not the door;
  - `DetachStrike` and `BeatWobble` in `Rendering/Doors/FrontRoomsDoorDressEvents.cs`;
  - sound's `Door/Blow` event with a Damage parameter.
  
  There is **no per-door damage stage** yet.
- **Wood face:** Red also asked (10-30) for a new door wood texture; the options are in `research/door_wood/`. The break prototype should take the face veneer as a parameter, so it can adopt Red's pick.
