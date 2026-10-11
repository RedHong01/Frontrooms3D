# EXIT sign: merge stage (2026-10-08 19:38)

## Status: NOT APPLIED. READY for `--apply`.

This stage cannot write into Red's project (merge rule d). Under merge rule (a), the stage dry-ran the existing package first. The dry run passed, so the stage stopped there. It did not rebuild a clone, re-verify, or render anything.

**Command (the visual chat runs it):**

```
bash /Users/redwang/FrontRoomsVisualWork/apply/exit_sign.sh --apply
```

### Dry run, 19:38, main `bc0aa79` (01:47) plus its working tree

| Check | Result |
|---|---|
| Bases | 76 of 76 match: 61 new paths absent, 15 existing paths at their recorded sha1 |
| Kept `.meta` files | 9 of 9 unchanged |
| Payload | 76 of 76 files match `exit_sign_expected.txt` |
| GUIDs | The 2 replaced `.meta` files keep main's GUID. The 27 new GUIDs are not used anywhere in `Assets/` |
| `W/LOCAL_PATCHES.md` files in the package | none |
| Last line | `DRY RUN OK` |

- Main has not committed since `bc0aa79`. Its working tree has many edits from other chats (touch, audio, map, mobile). None of them touch a path in this package.
- No rebase was needed, so `rebase_apply.sh` did not run. The integrate stage's last rebase (PASS, Roslyn 0 errors on macOS and iOS) still holds.
- `codex_audit/20_findings.md` still does not exist. The exit-sign findings in `10_review_kits.md` are closed in the payload: K4/F4 (`Run_ExitSign_Dead`) and K7 (`null` in `lodDistances`).

### After `--apply`

1. The script prints the sha1 manifest. It must say MANIFEST OK, and a second dry run must print `ALREADY APPLIED`.
2. Add a row to `W/apply/MERGED.md`. The backup goes to `W/backup/2026-10-07_exit_sign`.
3. Unity (if it is open) imports 8 FBX, 10 textures and 5 materials, then recompiles once. Check the console for errors.
4. Merge order: `outlets_c1` also edits `FrontRoomsRoomStream.cs`, in other hunks. Whichever track lands second runs `W/tools/rebase_apply.sh <track>` first.

## What lands (summary; full list in `exit_sign_expected.txt`)

- **Models:** `Kit_ExitSign`/`_Dead` are replaced, and main's GUIDs are kept. 6 new kits: `Kit_ExitSign_Hanging`, `_HangingFlush`, `_End`, each with a `_Dead` variant. LOD0 has 3,136–6,502 triangles and LOD1 has 984–1,999. `lodDistances` is [5, 0, 0], and none of them has a collider.
- **Surfaces:** 10 `ExitSign_*` textures and 4 new materials. Smoothness is capped at 0.60. `Run_ExitSign` smoothness goes from 1.0 to 0.38, which ends the mirror. The `_FR_LAMP_FIELD` keyword is added to `FrontRoomsSurface.shader`.
- **Game:**
  - Run rooms get 2 `Kit_ExitSign_Hanging`, at z 3.4 and z 9.2. The red light is unchanged, times the sign's Level.
  - Exit rooms get 1 `Kit_ExitSign` over the far door.
  - A new driver, `FrontRoomsExitSign`, deals each sign LIT 70 %, HALF 15 % or LOOSE 15 %. Reduce flashing holds a sign steady.
  - The door anchor `exit_sign_p` moves from 2.28 to 2.18.

## Not done in this stage (rule a), in order

1. **Critic issue 1, SHOULD-FIX (a follow-up delta after this merge).** In Run rooms a LIT sign does not read as lit: the letters reach only 0.59× the ceiling (es_i07).
   - Fix: add `EmissionGain` to `FrontRoomsExitSign`, start it at about 3, and keep `Level` and the light un-gained.
   - Add a T2-Run gate on raw HDR at 1.5 m and 6 m: letters at least 1.5× the ceiling ring; graded R/G above 2.5; T4 unchanged.
   - This needs a new package (`exit_sign_fix1`) and its own captures.
2. **Critic issue 4, LOW.** LOOSE signs ignore the stream-light fade after the start door shuts. Fix in RoomStream: `if (room.lampsScheduled && !TerminalDoorShut) TickExitSigns(room, dt);`. It fits in the same delta as issue 1.
3. **Three-view PNGs for 平面视觉: DONE 2026-10-08 19:5x** (`threeview/index.json`, 4 sheets, 8 kits, 32 PNGs in `threeview/png/`; clone `W/_partial/proj_es_tv`; builder `W/es_tv_out/build_index_es.py`; verification VL220 `2961:6863`). The text below is the original plan. The interactables index lists only the old G3 `Kit_ExitSign`/`_Dead` and says they are this track's now. After the merge, render from main's merged state:
   - Make a clone: `W/tools/mkclone.sh es_threeview`.
   - Render: `Unity -batchmode -buildTarget OSXUniversal -projectPath W/es_threeview -executeMethod FrontRoomsThreeView.RunBatch -quit -threeViewOut <out> -threeViewOnly Kit_ExitSign,Kit_ExitSign_Dead,Kit_ExitSign_Hanging,Kit_ExitSign_Hanging_Dead,Kit_ExitSign_HangingFlush,Kit_ExitSign_HangingFlush_Dead,Kit_ExitSign_End,Kit_ExitSign_End_Dead`.
   - Post-process with `research/interactables/threeview/tools/` per `INDEX_FORMAT.md`. The output goes to `research/exit_sign/threeview/` with an `index.json`.
4. **Critic issue 2, Red's call.** The sign reads as "a lamp hanging under the sign": the light sits 0.33 m below the housing, so the sign casts a large dark copy of itself on the ceiling.
   - Option: move the light to the `centre` anchor (y 2.394) and turn off the sign's shadow for that light.
   - Do an A/B capture first.
5. **Critic issue 5, optional.** The Run sign's white face reads green-grey in the red flood. Option: a weak unshadowed red fill scaled by Level.
6. **VL211 FLAG, Red's call.** The Exit-room sign's face reads mid-grey under grazing troffer light. Option: the optional desktop spill light from spec §8.6.

## Contract request to the map chat (text only; this stage edits no map file)

**File:** `Assets/Editor/FrontRoomsMap/FrontRoomsModuleEditing.cs` (Level Designer).

**Problem.** After this merge, the 4 Ceiling kits show in the "All" palette: `Kit_ExitSign_Hanging`, `_HangingFlush` and their `_Dead` variants. `NewProp` has no `Ceiling` case, so a dropped sign becomes a floor prop at y 0, under the floor.

**Proposal (pick one):**

```csharp
// in NewProp(...)
case "Ceiling":
    return new ModuleProp { kit = kit, x = at.x, z = at.y, y = MapGrid.CeilingHeight(m.height), noCollider = true };
```

or, until spec C-3 lands, filter `Placement(n) != "Ceiling"` out of `PaletteKits`.

**Sidecar follow-up (visual side, after the map chat agrees).** `minCeiling` is 0.05 today, which means nothing for an asset whose origin is the ceiling.
- Set 2.64 for Hanging: a 0.612 m drop plus the 2.03 m (80 in) headroom rule.
- Set 2.27 for HangingFlush.
- Then Low zones (2.4 m) can refuse the Hanging sign, whose bottom would sit at 1.79 m.

## Hand-offs to other owners

| Owner | Item |
|---|---|
| RT (G14) | Classify `FrontRoomsExitSign` renderers as `EmissiveLens`, scaled by Level |
| WebGL track | Keep or strip the `_FR_LAMP_FIELD` keyword |
| 平面视觉 | Sign-off on the built EXIT letterform (6 in letters, 3/4 in strokes, stencil). Add it to `Documentation/GRAPHIC_VISUAL_TASKS.md` (the visual chat edits that file) |
| Interactables track | Re-export the steel door frames from main's module after this merge, or the anchor goes back to 2.28 |
| Run! direction B | One `SetMains(0)` call on its trip |
| Visual chat | Merge `FrontRoomsExitSignTests` (T5 photosafety, at most 2 flashes per second) as an Editor test with the fix1 delta |

## Nits (do not hold the merge)

- `30_integration.md` §3: the housing is 2.288–2.500 m. The range 2.288–2.900 is canopy to bottom.
- A RenderSetup comment says the diffuser mask mean is ".72". The built value is .55.
- `Kit_ExitSign(_Dead).fbx.meta` still carry unused remaps (`Run_ExitSign`, `Prop_SteelBlack`). Remove them later for the Unreal migration, keeping the GUIDs.
- `FrontRoomsSurfaces.ExitSign`, `Run_ExitSign.mat` and its texture have no caller after the merge.
- LOD2 was not built (pipeline P-1). LOD1 is never culled.
