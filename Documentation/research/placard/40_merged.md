# Q16 placard: merge stage (2026-10-08 01:42)

**Status: READY, not applied.** Red's merge rule (a): the existing package dry-runs OK, so no clone rebuild and no re-verification. A subagent cannot write into Red's project, so the visual chat runs the apply.

## Command

```
bash /Users/redwang/FrontRoomsVisualWork/apply/placard_q16.sh --apply
```

First approve the `FrontRoomsRenderSetup.cs` edits. The package is all or nothing.

## Dry run (01:42, main 22bb75f plus the working tree)

- `DRY RUN OK`: all 31 bases match and all 31 payload files match.
- 27 new paths:
  - 4 runtime scripts in `Assets/Scripts/Rendering/`: Phosphor, PlacardGlow, Placard, PlacardMount;
  - 2 textures: `Prop_EvacPlan_A`, `Prop_EvacPlan_E`;
  - 3 materials: `Prop_AluminiumAnodised`, `Prop_EvacPlan`, `Prop_LensNonGlare`;
  - 2 kits as FBX + JSON: `Kit_EvacPlacard`, `Kit_EvacPlacardLens`;
  - a `.meta` for each of the above;
  - `Tools/lookdev/pack_evac_plan.py`.
- 4 replaced paths, each backed up to `W/backup/placard_q16_<stamp>/`:
  - `FrontRoomsRenderSetup.cs` (first);
  - `evac_placard_common.py`;
  - `evac_placard.py`;
  - `FrontRoomsRoomStream.cs` (last).
- Kept, never written:
  - main's `.meta` GUIDs for RenderSetup and RoomStream;
  - `FrontRoomsMapWorld.cs`, which is map-owned. Its sha1 is 6b76a00c, the contract's base, so `40_contract_map.md` (Q16-1) still applies to the current map.

## Gates

- **Fix stage:** completed with a dry run OK, rebase PASS, Unity and Roslyn compile at 0 errors, and a scratch `--apply` that gave MANIFEST OK, then ALREADY APPLIED. Details are in `35_fix.md` and `41_promotion.md`.
- **Codex audit:** `20_findings.md` still does not exist. `00_main_state.md` lists D4 placard as "not touched". `10_review_docs.md` mentions only the untracked `evac_placard*.pyc` noise, and I left those files alone.
- **Order with `outlets_c1`:** both packages edit `FrontRoomsRoomStream.cs`, in different hunks. Whichever lands second will fail its base check. Run `W/tools/rebase_apply.sh <track>` on it, which should PASS.

## After apply

- If the frame or lens shows a grey default material, reimport `Kit_EvacPlacard.fbx` and `Kit_EvacPlacardLens.fbx`.
- Do not run Set up / `RenderSetup.RunBatch` in main. The materials ship as files.

## Contracts the visual chat sends

- **Map chat:** Q16-1 `KeepClearAtStart` (`40_contract_map.md`). Without it, furniture can block P2/P3 in about 1 run in 4.
- **平面视觉:** redraw the swatches in the in-game `LobbyPrint` colourway (`42_request_graphic.md`). Add it to `GRAPHIC_VISUAL_TASKS.md`.

## Red decides

- **RenderSetup edits:** approve them. This is the apply gate.
- **Glow k:** 0.158 is recommended.

No new images, so no new VL row.
