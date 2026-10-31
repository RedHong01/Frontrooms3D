# Door frames: F4 build log (30)

*游戏视觉 (account B, session local_a9a1dce2), 2026-10-30. Built and tested in the clone `W/proj_doorframe`. Nothing has landed in main. Red's pick, verbatim: "F4 当年尺寸".*

## Status: ready to land, contract first

Built, tested, packaged and rehearsed. Not applied.

**The flaw is fixed.** After a steel frame tore, a thin wallpaper-coloured sliver showed in its soffit, about 200 mm tall, at the strike height. It showed at LOD0 only (within 4 m), once the strike had dropped off.

**How it got here.** The first film showed a much larger lens-shaped patch of wallpaper, in both D2 and TORN. The soffit stands 2 mm off the wall's end, and the dent went 3 and 7 mm, so it passed through it. Today's map trim hides that, and the F4 door trim does not. Fix 1 cut the dent to 1.2 and 1.8 mm. The lens went and D2 was clean, but the sliver stayed on TORN.

**Cause.** The strike box behind the mortise pocket starts at Z 0.9996, so the 1.8 mm dent still took it past the wall's end at Z 1.0.

**Fix 2.** One line at the end of the dent in `steel_field` (`interact_door_frame_da_break.py`): `nz = max(Z, min(nz, 0.9997))`. One Blender run, one film.

**Film, same seed, same frames, against the film before the fix.** The sliver is gone. In the view that showed it (`X_C2_FINALp05`, frame 1107), 1,177 px changed by more than 24 of 255, all in one column 7 px wide and 227 px tall, where the sliver stood. The other 26 frames are unchanged, D2 included, except 1 px in one. Tris are the same.

**Sheet:** `images/door_frame_f4_final.jpg` (original `W/door_frame/sheet/door_frame_f4_final.jpg`).

## What was built

| Frame | Before | F4 | Tris LOD0 / 1 / 2 |
|---|---|---|---|
| `Kit_DoorFrame_Steel_DA`, `_Steel_Alu_DA` | 77 × 25 | 50.8 × 11.1 | 2,030 / 724 / 276, unchanged |
| `Kit_DoorFrame_Wood_DA` | 77 × 25 | 57.2 × 17.5 + 4.8 reveal | 1,676 / 892 / 252, was 1,724 / 940 / 252 |
| `Kit_DoorFrameBreak_Wood_DA` | | | intact 2,098 / 1,300 / 460, was 2,186 / 1,388 / 460; SPLIT 1,049 / 426 / 124 |
| `Kit_DoorFrameBreak_Steel_DA` | | | intact 2,634 / 1,032 / 504, unchanged; TORN 1,882 / 432 / 168, was 1,890 at LOD0 |

Band as rendered, one side, reveal included (same rows and thresholds as `20_for_red.md`): steel 95–99 mm → 65–69 mm at 3 m (6.9 % of the leaf), wood 95–99 mm → 78–82 mm (8.3 %). Numbers in `W/door_frame/out_f4/measure.json`.

- **Unchanged:** colliders, leaf, pivot, anchors, textures, materials, the title / station pair, windows, `interact_door_common.py`.
- **Generators:** `interact_door_da_common.py` holds the F4 section as constants, and `assert_frame`, `steel_sleeve_section` and the 95° assert use them. The steel trim clearance assert is now 1 mm, because the sheet's back sits 1.4 mm off the door trim.
- **Break kits:** the wood split's field runs to the new casing face. The steel flare's span and the band cuts follow the 50.8 mm face.

## Three things the option stage did not cover

1. **Level transitions landed at 21:37, after the research, with the old casing in `FrontRoomsTransitionKit.Frame.cs`.** `CasingFace` and `CasingReturn` set the cove base's stop, the renovation halo and the proxy sleeve. Left alone, each Office door would have shown about 26–31 mm of bare wall between the casing and the base. The package moves them to `.0911` and `.0488`. The base stop now uses the casing on every door, border or not. **This has no effect in the game yet:** nothing in the map calls `BeginChunk`, so the kit is dormant, and no cove base shows on the Office door I rendered. Its change is checked by `FrontRoomsTransitionKitTests` only, not by eye.
2. **Two map tests would have failed.** `FrameChecks` looks for the head corner at the old trim size, and the EXIT sign's check allows at most 20 mm over the head (it is now 29 mm). Both edits are in the contract, because that file is 关卡设计's.
3. **The door break's frame grit** started on the old casing face. `FrontRoomsDoorBreakVfx.cs` now starts it on the new one (X .082–.099, head to 2.15, jamb to 1.048).

## Tests

Clone synced from main at 22:0x. Payload + contract applied. The Unity suites ran before fix 2. Fix 2 changes only the steel break kit, so after it I re-ran the film and Roslyn only.

| Test | Result |
|---|---|
| Roslyn macOS + iOS, main + payload | OK, 0 errors, re-run on main at 22:43 (Game `65b44dcf`, Mobile `5200c57b`, MapHunter `a716d075`), with fix 2 |
| Roslyn macOS + iOS, main + payload + contract | OK, 0 errors, same run |
| `FrontRoomsDoorShotTests` | 252 / 0 |
| `FrontRoomsDoorRigTests` | 91 / 0 |
| `FrontRoomsTransitionKitTests` | 12 / 0 |
| `FrontRoomsMapInteractionTests` | 464 / 0 (before 关卡设计's later edit of that file; the contract was re-made on it and compiles) |
| Exit run (`-autopilotExit -autopilotStation 4 -autopilotSeed 2554`) | PASS: floor 4, 27 / 27 exit, 15 / 15 station, 2 / 2 door checks, 0 errors. The first run failed only because the clone had no FMOD banks; the same checks passed. I copied the banks in and re-ran it. The clone stays silent |
| Door break film, `smoke3` (steel L0-K, veneer L0-F), re-run with fix 2 | Inserts swap on time: steel D2 and TORN, wood SPLIT. Restore differs from settled by 137 of 28,956 px; the kept stage holds D2. **Wood clean. Steel clean: the sliver is gone** |
| Leaf at 95° (steel and wood frames) | Rested at 95.09°, renders from the wall side show nothing clipping. The generator's assert leaves 3.4 mm at the wood casing and 3.9 mm at the steel, against 2.7 mm today |

## Landing order

**Contract first, then the package.** This is safe with any gap between them.

- **Contract alone:** 3 m views of L0-F, OF-F and OF-K show 0 levels of change, and every other view of that door at most 4 of 255. One Office door's head shadow moved 2 pixel rows (258 px). The EXIT takes its final frame.
- **Package alone: wrong.** The map's 70 × 20 mm trims stand outside the new kits and hide them. Every door shows a plain dark box at today's width, and the lobby door's walnut casing is gone. The last row of the sheet shows it.
- `door_frame_f4.sh --apply` refuses until `DoorTrimFace` is in main. `APPLY_BEFORE_CONTRACT=1` overrides that if both land in one step.

## Package

- `W/apply/door_frame_f4.sh` (v2): 18 paths, all replaced, 14 `.meta` kept, nothing new. Rebuilt with fix 2 (new steel break `.fbx` and `.json`, new `interact_door_frame_da_break.py`). Dry run OK on main at 22:45.
- Rehearsal on a scratch copy of main at 22:43: refused without the contract, then MANIFEST OK with it, then ALREADY APPLIED.
- `W/apply/door_frame_f4_contract/`: `README.md`, `CONTRACT_map.diff`, 2 files, 7 hunks. `patch --dry-run` and `git apply --check` OK on main at 22:45.
- Neither `door_frame_f4.sh` nor `regen_contract.sh` calls `rm` any more. Their leftovers go to `W/_trash_20261030`.

## Not done or not shown

- **EXIT stair side:** it stays 70 mm. It is `FrontRoomsStairwell.cs`, 关卡设计's file, and the frame there fills a real hole in collider walls. The contract README lists what would change. I recommend leaving it unless Red asks.
- **WebGL:** undressed doors show the 45 × 8 mm trims as their frames. Not rendered.
- No LOD1 or LOD2 close-up of the new frames, and no iPad frame. The phone frames are iPhone Pro at Medium.
- The "before" break frames on the sheet come from the door break stage, on an older main, so that steel door has no lite.
- The Run doors and the unused Steel_Alu kit are rebuilt but not rendered.
- No VERIFICATION_LOG row and no Figma upload. LANDINGS and MERGED are untouched.
