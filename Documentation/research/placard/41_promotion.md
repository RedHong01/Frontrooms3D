# 41 — Evacuation placard (Q16): promotion into Red's project

Status: **READY TO APPLY, 2026-10-08 01:3x.** One atomic package, convention v2. It supersedes `promote_src/apply_placard_patches.py.txt` and the build's file list (`20_build.md` §8).

```zsh
bash /Users/redwang/FrontRoomsVisualWork/apply/placard_q16.sh            # dry run (writes nothing)
bash /Users/redwang/FrontRoomsVisualWork/apply/placard_q16.sh --apply    # the visual chat runs this
```

- **Dry run against main 22bb75f (working tree, 01:28): `DRY RUN OK`**, 31 paths: 27 new (all bases ABSENT), 4 replaced (bases matched by sha1).
- `W/tools/rebase_apply.sh placard_q16 --no-install`: **PASS** ("nothing to rebase").
- `--apply` tested on a scratch copy of main under W: `MANIFEST OK`, then a second dry run printed `ALREADY APPLIED`.
- Compile: **0 errors** in Unity (the clone, OSXUniversal) and in Roslyn against **main's current working tree** with the payload in place (main had uncommitted map, game and kit-library edits at 01:23; they do not conflict).
- **NEEDS APPROVAL (visual chat):** the `FrontRoomsRenderSetup.cs` edits (a shared pipeline file). Without them the textures' import rule would be lost on the next re-import, so the package is all or nothing.

## 1. Package files (`W/apply/`)

| File | What |
|---|---|
| `placard_q16.sh` | The apply script (`# apply-convention: v2`; honours `APPLY_REAL`, `APPLY_DIR`, `APPLY_BACKUP`). Dry run by default; `--apply` backs up every replaced file to `W/backup/placard_q16_<stamp>/`, writes in order, then checks the manifest |
| `placard_q16_bases.txt` | sha1 or ABSENT of every written path, recorded on main 22bb75f |
| `placard_q16_expected.txt` | sha1 of every payload file, in write order |
| `placard_q16_keep.txt` | Never written: `FrontRoomsRenderSetup.cs.meta` and `FrontRoomsRoomStream.cs.meta` (GUIDs must stay; a change refuses the apply), and `FrontRoomsMapWorld.cs` (contract Q16-1 notice only) |
| `placard_q16_payload/` | The 31 files |
| `placard_q16_base/` | Exact copies of the 4 replaced bases (for 3-way rebases) |
| `placard_q16.diff` | Base → payload for review (copy: `fix_src/placard_q16.diff.txt`) |

Rebuild the package after any change in the clone: `W/venv/bin/python W/placard_fix/build_package.py` (copy: `fix_src/build_package.py.txt`).

## 2. Every path, in write order

The order matters in an open Unity: the importer rule must exist before the textures arrive, each `.meta` before its asset (so Unity never mints its own GUID), the materials before the FBX (whose `.meta` remaps to them by GUID), the sidecar JSON before its FBX, and RoomStream (which calls `FrontRoomsPlacard`) last.

| # | Path | Change | GUID |
|---|---|---|---|
| 1 | `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs` | replace: +2 SurfaceDefs (`Prop_EvacPlan`, `Prop_AluminiumAnodised`), +1 GlassDefs row (`Prop_LensNonGlare`) and its blend/shadow block, +1 importer rule (`Prop_EvacPlan_*`: no NPOT rescale, clamp, `_A` sRGB BC7, `_E` linear BC1, **WebGL tab only** max 1024). `.meta` kept | main's (kept) |
| 2–9 | `Assets/Scripts/Rendering/FrontRoomsPhosphor.cs`, `FrontRoomsPlacardGlow.cs`, `FrontRoomsPlacard.cs`, `FrontRoomsPlacardMount.cs` (+ `.meta` first) | new | 392b8fec…, e94b78d5…, 74ab23ac…, 765915f1… |
| 10–13 | `Assets/Resources/Surfaces/Textures/Prop_EvacPlan_A.png`, `_E.png` (+ `.meta`) | new; 4096 × 2048 | a46c3188…, 9ef8f2e3… |
| 14–19 | `Assets/Resources/Surfaces/Prop_AluminiumAnodised.mat`, `Prop_EvacPlan.mat`, `Prop_LensNonGlare.mat` (+ `.meta`) | new | d971225e…, 0cfd51a3…, f2e52cfd… |
| 20–27 | `Assets/Resources/Props/Models/Kit_EvacPlacard.json`, `.fbx`, `Kit_EvacPlacardLens.json`, `.fbx` (+ `.meta`) | new | 82d65f55…, 9422a5e2…, 37c4f31b…, 0d394adf… |
| 28 | `Tools/lookdev/pack_evac_plan.py` | new (outside `Assets`: no `.meta`) | — |
| 29 | `Tools/Blender/frontrooms_kit/assets/evac_placard_common.py` | replace: frame slot `Prop_AluminiumAnodised` + its preview colour | — |
| 30 | `Tools/Blender/frontrooms_kit/assets/evac_placard.py` | replace: docstring (the slot name) | — |
| 31 | `Assets/Scripts/FrontRoomsRoomStream.cs` | replace: one `FrontRoomsPlacard.Prepare(...)` call at the end of `EndStreamAt`. `.meta` kept | main's (kept) |

- Full GUIDs and sha1s: `placard_q16_expected.txt`, and `35_fix.md` / `20_build.md` §4. No GUID collides with any `.meta` in main; none of the 27 new paths exists in main.
- Sizes: the FBX are 117 KB and 16 KB; the PNGs 627 KB and 99 KB.
- Not in the package, never promoted (clone tools): `Assets/Editor/Rendering/FrontRoomsPlacardLookdev.cs`, `Assets/Editor/Audit/FrontRoomsPlacardRender.cs`, `FrontRoomsPlacardFixBatch.cs`, `Assets/Editor/Audit/PlacardCand/` (the candidate art). Copies are in `fix_src/`.
- Not in the package, owned by others: `FrontRoomsMapWorld.cs` (contract Q16-1, `40_contract_map.md`); the artwork in `Tools/print/ink/art_from_graphic/placard/` (平面视觉; the candidate colourway is a request, `42_request_graphic.md`).

## 3. Slots and materials (what the package sets up)

| Slot (kit side) | Material (Unity side) | Shader and settings | Used by |
|---|---|---|---|
| `Prop_AluminiumAnodised` (new) | `Resources/Surfaces/Prop_AluminiumAnodised.mat` | FrontRooms/Surface, no texture, base (0.91, 0.92, 0.92), metallic 1, smoothness 0.62, occlusion 1, macro tone 0, macro dirt 0, mesh UV | Kit_EvacPlacard rails + base strip |
| `Prop_EvacPlan` (new) | `Resources/Surfaces/Prop_EvacPlan.mat` | FrontRooms/Surface, base map `Prop_EvacPlan_A`, emission map `Prop_EvacPlan_E`, `_EMISSION` on at black, smoothness 0.15, macro tone 0, dirt 0.02, mesh UV | Kit_EvacPlacard sheet. `FrontRoomsPlacardGlow` drives `_EmissionColor` on one instance per placard |
| `Prop_LensNonGlare` (new) | `Resources/Surfaces/Prop_LensNonGlare.mat` | URP Lit transparent, base (0, 0, 0, 0.04), smoothness 0.55, preserve specular (`_ALPHAPREMULTIPLY_ON`, Src One), ShadowCaster and DepthOnly off, queue 3000 | Kit_EvacPlacardLens |
| `Prop_Aluminium` | unchanged | — | every other aluminium kit |

- **Kit side:** the slots are registered by the modules (`kitlib.register_slot`, a setdefault); `kitlib.SLOTS` is not edited.
- **Unity side:** the three `.mat` files ship as generated in the clone. The `SurfaceDefs`/`GlassDefs` rows in `FrontRoomsRenderSetup.cs` reproduce them exactly, so a later *Set up* keeps them as they are.
- **Textures** (`Tools/lookdev/pack_evac_plan.py` from the v2 art; input md5 print e39befc6…, mask c7e22e2f…):

| Texture | Size | Desktop | WebGL tab | iOS |
|---|---|---|---|---|
| `Prop_EvacPlan_A` (sRGB) | 4096 × 2048 | BC7, 11.2 MB with mips | 1024 × 512 DXT1, 0.35 MB | ASTC 4×4 (automatic) |
| `Prop_EvacPlan_E` (linear) | 4096 × 2048 | DXT1 (BC1), 5.6 MB | 1024 × 512 DXT1, 0.35 MB | ASTC 6×6 (automatic) |

  The power-of-two size is what makes them compress at all (`35_fix.md` §6: at 2592 × 1676 Unity kept them uncompressed, 40.5 MB).

## 4. After `--apply`

1. **If Unity is open,** let it import, then right-click `Assets/Resources/Props/Models/Kit_EvacPlacard.fbx` and `Kit_EvacPlacardLens.fbx` → **Reimport**, but only if the frame or the lens shows a grey default material. (`FrontRoomsKitImporter` drops a remap whose material was not imported yet; the shipped `.meta` already holds the right remaps.)
2. **Do not run *FrontRooms → Rendering → Set up* for this.** The materials ship as files. In the clone, *Set up* also rewrote `FrontRoomsPost_Office.asset` (5 values that differ from main) and added print defaults to about 70 materials.
3. Check once in Play mode: the console shows `[Placard] mount P1 west, door cell …` when the run starts, and (until Q16-1 lands) one `[Placard] the map has no KeepClearAtStart …` line.
4. Log the merge in `W/apply/MERGED.md`.

## 5. Conflicts with other packages

- `outlets_c1` (not applied yet) also replaces `FrontRoomsRoomStream.cs`, in other hunks (the placard adds 4 lines after the far-side `receiveShadows` loop, which `outlets_c1` keeps). Whichever lands second needs `W/tools/rebase_apply.sh <track>`, which will be a clean 3-way merge.
- The office-dress-hitch work (another chat) is editing `FrontRoomsMapWorld.cs`, `FrontRoomsKitLibrary.cs`, `FrontRoomsOfficeKit.cs` and `FrontRooms3DGame.cs` in main right now (uncommitted). The placard package touches none of them, and Roslyn compiles the package against that working tree with 0 errors. The placard calls `FrontRoomsKitLibrary.Spawn` and listens to `FrontRooms3DGame.MapRunStarted`; if either signature changes, re-run the compile check.

## 6. What changed since the build (for anyone comparing with `20_build.md`)

| Item | Build (2026-10-07) | Fix (this package) |
|---|---|---|
| Frame slot | `Prop_Aluminium` (shared) | `Prop_AluminiumAnodised` (new, placard only) |
| Textures | 2592 × 1676, stored uncompressed (40.5 MB) | 4096 × 2048, BC7 / BC1 (16.8 MB); WebGL 1024 × 512 DXT1 |
| WebGL import tab | not set | max 1024 on both maps |
| Centre height | 1.524 m | 1.560 m |
| Calm Failing read | gate of the 3 s lamp mean (G 0.087) | peak follower on the normal target (G 0.36) |
| `PeakEmission` | `FrontRoomsPlacardGlow`, comment said 12 % | `FrontRoomsPhosphor`, one absolute k (0.158), both references in the comment |
| Duplicate mounts | deferred `Destroy` only | old mounts retired at once |
| `ForceLevel` | serialized in the Editor | `[System.NonSerialized]` |
| Patch script | `promote_src/apply_placard_patches.py.txt` (2 files) | `W/apply/placard_q16.sh` (31 files, v2) |

## 7. Leftovers (not blocking)

- Three stale `evac_placard*.cpython-311.pyc` from 2026-10-03 are tracked in main; this stage wrote none (`dont_write_bytecode`) and did not delete them.
- `Kit_EvacPlacard.fbx.meta` keeps a stale remap entry for `Prop_Aluminium` (no submesh uses it). Harmless.
- LOD1/LOD2 are built and reviewed but not exported until `kitlib.make_lods` (P-1) is approved.
- A WebGL build (A10) was not run; the WebGL track should run one with this package in.
