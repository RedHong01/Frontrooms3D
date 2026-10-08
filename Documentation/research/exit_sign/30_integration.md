# EXIT sign: integrate (RoomStream, driver, kit library)

Exit-sign workflow, integrate stage, round 4 (2026-10-07 23:24 to 2026-10-08 01:5x). It continues the 10-07 19:31 attempt, which stopped on the usage limit at 20:06 before any Unity run finished. Nothing is written into Red's project except this folder and `Documentation/VERIFICATION_LOG.md` (rows VL210–212). The merge waits for one command (§7).

## 0. The short version

- **What changed in the game.**
  - **Run rooms:** the two code-built signs (a black box, two quads on the 1.0-mirror `Run_ExitSign`, two rods) are now two `Kit_ExitSign_Hanging` at z 3.4 and 9.2.
    - The red point light stays exactly as it was: colour (1, .10, .06), intensity 3.6, range 9.5 m, soft shadows .85, at y 1.96 m.
    - Its intensity now follows the sign: 3.6 × `Level`.
  - **Exit rooms:** a new `Kit_ExitSign` (wall sign, lit) sits on the far door's header.
  - **Lamp states:** each Run sign is dealt LIT 70 %, HALF 15 % or LOOSE 15 % from the room's own hash, with at most 1 LOOSE per room. Exit signs are always LIT.
  - **Flicker only when seen:** a LOOSE sign starts its drop-outs only once the room's lamps are cued.
  - **Reduce flashing:** a LOOSE sign holds steady at 0.6 of its loose lamp.
- **New code.**
  - `FrontRoomsExitSign` (`Assets/Scripts/Rendering/`) drives the lamps and the light.
  - `FrontRoomsKitLibrary.Spawn` attaches it to every kit tagged `exit_sign`, so map and facade spawns get a working sign.
- **Verdict: PASS.**
  - **Compile:** 0 errors on current main 22bb75f plus Red's working tree (Unity, OSXUniversal). The Roslyn check also gives 0 errors with the macOS and the iOS defines.
  - **Own tests:** 10 of 10 pass.
  - **Look-dev:** the build stage's acceptance numbers come back unchanged (T2 1.88, T3 0.88, T4 the same).
  - **Nothing else changed:**
    - the 20 validator seeds give identical map signatures, 1,500 rows, byte for byte;
    - all 10 project suites, the map interaction tests among them, give identical results;
    - the one exception is the Level Designer palette count, which changes because there are 6 new kits;
    - 38 map frames and every Lobby and Shift frame of the room-visuals harness are bit-identical.
- **Merge:** the v2 package `W/apply/exit_sign` covers the build and integrate stages: 76 paths, 61 of them new. Its dry run is OK and `rebase_apply.sh` says PASS. Run `bash /Users/redwang/FrontRoomsVisualWork/apply/exit_sign.sh --apply` (§7).
- **One FLAG (VL211).** The Exit room's sign face reads mid-grey. The lamps sit at 2.85 m, so the top 0.25 m of the end wall gets only grazing light. The letters read. Red's call: switch on the optional spill light (spec §8.6) or leave it (§9).

## 1. State, bases and what this round fixed

| Item | Value |
|---|---|
| Main | `22bb75f` (2026-10-07 23:46) plus Red's uncommitted working tree as of 01:2x. Since `3ff05ee` (the previous attempt's base), main changed `FrontRoomsKitLibrary.cs` (dress-hitch: `Prewarm`, cached `Prepared` sets) and `interact_door_frame_steel.py` (saddle fix pass). Both of this track's edits were re-applied on top of main's new files (§4, §5). Every other touched base is unchanged |
| Codex audit | `codex_audit/20_findings.md` does not exist. The 10_review_*.md files hold 3 exit-sign items, all closed by this package: K4 (`Kit_ExitSign_Dead` used a missing `Run_ExitSign_Dead` material; it now uses `ExitSign_Diffuser_Dead`), K7 (`null` in `lodDistances`; now `[5, 0, 0]`) and F4 (slots without materials). No finding touches RoomStream's sign code |
| Clones | `W/proj_es_before` = main (ee5c9bb + working tree, synced 10-07 23:29) + tools + capture harness. `W/proj_es_int` = the same + the package. `W/proj_es_final` = main 22bb75f + working tree (synced 10-08 01:22) + the package, for the final compile and tests. All three: `mkclone.sh` steps (rsync from main with iCloud copies excluded, `apply_local_patches.py` = nothing to do, `install_tools.py`) on existing clones, so the imported Library was kept |
| Fixed in this round | (1) The previous attempt put the Exit sign in `BuildProfileProps`' `else if (rule == RoomRule.Exit)` branch. That branch can never run, because the first branch already takes `Lobby \|\| Shift \|\| Exit`, so no Exit sign was built. It now builds in the first branch (§3). (2) The test and capture harness looked rooms up with `Transform.Find("Profile 3 / Run")`. `Find` reads "/" as a path separator, so it found nothing; the rooms are now looked up by exact name. (3) The stream walk test started after 1 s of title. From that start the stream never connected room 2 and never advanced. The walk now uses the stream verification's own 3 s start and walks 86 rooms, so it reaches the first two LOOSE signs the hash deals (rooms 76 and 81). (4) The 60 Hz render probe rendered inside one editor tick, and that render ignored the new light intensity and `_LampField`. The probe now advances one engine frame per sample. (5) The before clone first lost 2 scripts in Play mode ("referenced script (Unknown)"): the 19:5x import had been killed halfway. A forced reimport of `Assets/Scripts` fixed it, and both clones then ran the same scene |

## 2. The driver: `FrontRoomsExitSign` (new, `Assets/Scripts/Rendering/`, 359 lines)

- **API** (spec §8.6):
  - `Configure(seed, Temperament, Light, lightBase)`;
  - `SetMains(level)`;
  - `Tick(dt)`;
  - `Level`, `State`, `Weights`, `RedLight`, `LightBase`, `NeedsTick`, `SelfTick`.
  - Temperaments: `Lit`, `HalfA`, `HalfB`, `Loose`, `Battery`, `Dead`.
- **How it lights the sign.**
  - It finds the diffuser submeshes by material name (`ExitSign_Diffuser`, LOD0 and LOD1).
  - It writes the lamp weights (A, B, DC) to `_LampField` through a per-slot MaterialPropertyBlock.
  - A plainly LIT sign keeps no property block, so it stays SRP-batched.
  - A `_Dead` model (only `ExitSign_Diffuser_Dead`) is always Dead.
- **Level** is the mean letter level against a LIT sign: Level = 0.4587·A + 0.5413·B + 0.6621·DC.
  - The three weights are measured, not guessed: area-weighted letter means of each channel of `ExitSign_Diffuser_F` over the letter polygons of `exit_sign_geom.py` (`integrate/code/level_weights.py.txt`; re-run this round: identical).
  - So HALF A = 0.541, HALF B = 0.459, BATTERY = 0.25 × 0.6621 = 0.166.
- **LOOSE** (spec §8.3):
  - Poisson gaps, mean 3 s, clamped 0.6–8 s; 1–3 drop-outs per episode.
  - Each drop-out falls with τ 40 ms to 0.05, holds 80–300 ms and recovers with τ 60 ms.
  - Drop-outs are at least 0.5 s apart; an episode lasts at most 3 s, then at least 4 s of quiet.
  - Only one lamp is loose, so the sign never goes dark (darkest Level 0.486).
- **REDUCE FLASHING** (`FrontRoomsSettings.ReduceFlashing`):
  - the loose lamp eases to 0.6 over 0.5 s and holds;
  - the clock and the random stream keep running;
  - a mains transfer becomes a 0.5 s crossfade with no black gap.
- **Mains** (`SetMains`):
  - a brownout scales the AC lamps by level^3.4;
  - below 0.55 the AC lamps cool, there is a 0.2 s black gap, and then the DC pair strikes (BATTERY);
  - nothing in RoomStream calls it yet: mains stay at 1 until Red picks a Run! direction (spec §8.2).
- **Cost:**
  - no allocation per tick (0 B over 36,000 ticks);
  - a static sign returns at once;
  - a sign spawned by the kit library runs its own `Update` only while LOOSE or while a mains change settles; RoomStream sets `SelfTick = false` and ticks its own signs.

## 3. RoomStream diff (`Assets/Scripts/FrontRoomsRoomStream.cs`, +126 / −25)

Base: main's file, sha1 in `integrate/logs/exit_sign_bases.txt`. Main has not changed it since `3ff05ee`. The full diff is in `integrate/diffs/FrontRoomsRoomStream.cs.diff`.

| Where | Change | Why |
|---|---|---|
| `RoomSlot` | + `FrontRoomsExitSign[][] exitSigns` (per profile variant), + `bool lampsScheduled` | Only the room's own rule's signs are configured and ticked; LOOSE plays only once the lamps are cued |
| `TickRoomLights` | + `if (room.lampsScheduled) TickExitSigns(room, dt)`; new `TickExitSigns`, `ActiveExitSigns` | The stream ticks its signs; a static sign costs one call |
| `ScheduleRoomLight`, `ActivateRoomLightImmediately` | set `lampsScheduled = true` | Spec §8.2 item 2: the clock starts with the room's lamp clock |
| `BuildRoom` | + `ConfigureExitSigns(room)` after `BuildProfileProps` | The first deal of temperaments (the earlier `ConfigureLightProfile` call runs before the signs exist) |
| `BuildProfileProps` | `exitSigns = new FrontRoomsExitSign[5][]`. In the Lobby/Shift/Exit branch: `if (rule == RoomRule.Exit)` builds `BuildDoorExitSign(props, "exit sign over door")`. Run branch: the box + 2 `SignFace` quads + 2 rod boxes + light block is replaced by `BuildHangingExitSign(props, 3.4f, "exit sign A")` and `(9.2f, "exit sign B")` | The swap itself. The dead `else if (rule == RoomRule.Exit)` branch (its "exit threshold marker") stays exactly as in main (§9) |
| `RefreshRoomMaterials` | one comment line (the name rule) | No sign label or part may contain wall, floor, carpet, ceiling, seal or partition |
| `ConfigureLightProfile` | `lampsScheduled = false; ConfigureExitSigns(room)` at the end | A recycled or re-ruled slot re-deals its signs from its new sequence |
| new `SignOdds(RoomRule)` | Run: HALF .15, LOOSE .15; everything else 0 | Spec §8.2 table (no BATTERY/DEAD in Run: the red source must burn) |
| new `ConfigureExitSigns` | key `sequence * 16 + 12 + i` (lamps use 0–11), `Hash01(key, 89)` picks the state, `Hash01(key, 97)` picks the HALF lamp, at most 1 LOOSE | Spec §8.2 item 1 |
| `SignFace` removed | it had no other caller | Spec §10.1 |
| new `BuildHangingExitSign`, `BuildDoorExitSign`, `AttachExitSign` | see below | — |

- **Hanging sign.**
  - `FrontRoomsKitLibrary.Spawn("Kit_ExitSign_Hanging", props, (0, RoomHeight, z), 0°, colliders: false, label)`.
  - Origin on the ceiling; housing y 2.288–2.900, so its centre is 2.394 (the code-built sign's was 2.36).
  - The light "run exit sign light" (the same name as before) sits at the sidecar's `glow` anchor (0, −0.94, 0), which is y 1.96 under the 2.9 m ceiling.
  - If the model is missing, an empty object still carries the light, so the level keeps its red source.
- **Wall sign.**
  - `Spawn("Kit_ExitSign", props, (0, RoomHeight − DoorHeaderHeight + .014, RoomLength − DoorWallDepth/2 − .004), 180°)`.
  - Origin at its bottom: housing y 2.674–2.886, back 4 mm off the header's room face (z 11.912–11.976), facing −Z toward the arriving player.
  - It clears the 2.62 m leaves and the 2.9 m ceiling. No light of its own (spill off, spec §8.6).
- **Kept identical:** the Run light's colour, intensity, range, shadows and height; every troffer and its odds; doors; materials; the pool; recycling; rebasing. The troffer state lists in `integrate/logs/signs_before.txt` and `signs_after.txt` are identical.

## 4. `FrontRoomsKitLibrary.cs` diff (+5, on main's new dress-hitch version)

```diff
         ApplyMaterials(assetName, model, instance);
         if (colliders) AddColliders(assetName, model, instance);
+        // EXIT signs carry their own lamp driver (research/exit_sign/10_spec.md §8.6), so a
+        // map-module or facade spawn gets a working sign with no map edit.
+        var info = GetInfo(assetName);
+        if (info != null && info.HasTag("exit_sign") && instance.GetComponent<FrontRoomsExitSign>() == null)
+            instance.AddComponent<FrontRoomsExitSign>();
         return instance;
```

`GetInfo` is a cached dictionary lookup; other kits pay one tag check per spawn. Test D3: all 8 `Kit_ExitSign*` get the driver; 111 other kits spawned, none gets one.

## 5. Door frame anchor (interactables module, same visual chat)

- **What moved:** `exit_sign_p` goes from (−0.080, 2.280, 0.500) to (−0.080, 2.180, 0.500). It now means the wall sign's origin (its bottom), 3 mm over the 2.177 m head casing (spec §4.5).
- **Where:** `interact_door_frame_steel.py` (re-applied on main's 00:08 saddle fix) and the two sidecars `Kit_DoorFrame_Steel.json` / `_Steel_Alu.json` (the anchor value only).
- **Effect today:** none. No code reads `exit_sign_p` yet.
- **Note for the interactables track:** if it re-exports the steel frames from a copy of the module without this line, the anchor returns to 2.28. Re-export from main's module after this merge.

## 6. Verification

| Check | Result |
|---|---|
| Compile, Unity 6000.3.10f1, `-buildTarget OSXUniversal` | 0 `error CS` in all three clones; final clone = current main 22bb75f + working tree + package |
| Roslyn, payload over Red's current tree (`W/tools/roslyn_check.py`) | Assembly-CSharp 75 files, Assembly-CSharp-Editor 52 files: 0 errors, 0 warnings, both with the macOS and with the iOS defines |
| T5 photosafety, driver at 60 Hz (`FrontRoomsExitSignTests`) | LOOSE 200 seeds × 120 s: max **1** flash in any 1 s (bar ≤ 2), longest episode 2.65 s (≤ 5), darkest Level 0.486, 14.5 flashes a minute, every seed flickers. REDUCE FLASHING 50 seeds × 120 s: **0** transitions, Level 0.78–0.82; switching it on mid-run: largest step 0.007 per frame |
| Mains transfer | brownout 0.8 → Level 0.468 (= 0.8^3.4); drop to 0.3: 0.217 s black, then BATTERY 0.166; back: no gap; REDUCE FLASHING: no black, largest step 0.028 per frame |
| D1 static and batching | LIT and LOOSE (between drop-outs) keep 0 property blocks; HALF / BATTERY / DEAD write 1 per diffuser (LOD0 + LOD1); only LOOSE needs a tick; a library sign's `Update` runs only while LOOSE |
| D2, D3, D4 | the 4 `_Dead` models are always Dead with the light off; the driver attaches to the 8 exit-sign kits only; 0 B allocated in 36,000 LOOSE ticks |
| R1 placement (editor preview) | Run: 2 signs, housing y 2.288–2.900 at z 3.36–3.44 and 9.16–9.24, light y 1.960, 3.6, soft, range 9.5, 2 LODs. Exit: 1 sign, y 2.674–2.886, z 11.912–11.976. Lobby, Shift and Office: 0 active. 56 sign transforms, none with a forbidden word (T11) |
| R2 stream walk (the verification's own player, 3.2 m/s) | 86 rooms in 311 s: 17 Run + 17 Exit rooms; signs LIT 40, HALF 9, LOOSE 2. 0 mismatches against the hash; light = 3.6 × Level on every sign frame; a LOOSE Level never moved before its room's lamps were cued; 6 LOOSE flashes, max 1 per s. Same walk with REDUCE FLASHING: 0 flashes |
| Mesh probe | all 76 submesh lines: 0 NaN tangents, 0 short, 0 non-perpendicular (`integrate/logs/mesh_probe_final.txt`) |
| Build look-dev re-run on the newer base (`FrontRoomsExitSignLookdev -esPrepare 0` + `exit_sign_measure.py`) | T2 1.883 (bar 1.5–2.5; build R3 1.87), T3 0.88 (≥ 0.5), T4 E/X/I/T 0.495 / 0.587 / 1 / 0.562, spread 0.52–0.64, max/min 8.99: identical to the build stage |
| Rendered probe (one engine frame per sample, 300 frames, 3 m) | sign-region luminance follows Level, r = 0.994. A drop-out dims the sign region by 19 % (0 WCAG general flashes; 4 transitions against its own maximum, max 1 per s, longest 1.4 s). REDUCE FLASHING: flat (0 transitions) |
| Room-visuals harness (`FrontRoomsRoomVisualsCapture`, 65 frames, before vs after) | 38 map frames: **0** difference (max ≤ 1/255). Title Lobby/Shift/Office: ≤ 0.03 % of pixels over 8/255 (the next rooms' signs seen through open doors). Run: wide 0.93 %, sign 1.26 %, ceiling 5.8 %, arrival 1.29 %. Exit: ≤ 0.07 % (`integrate/logs/room_visuals_diff.tsv`) |
| Sign captures (`FrontRoomsExitSignStreamCapture`, 12 frames each) | Run wide 0.93 %, close-ups 4.5–7.1 %, Exit arrival 0.02 %, Exit doorway 0.01 % (`stream_capture_diff.tsv`) |
| 20 validator seeds (`FrontRoomsCxSuites`, map signatures) | 20 seeds × 25 chunks, 1,500 rows: **byte-identical** before and after (sha1 in `map_signatures_sha1.txt`); 0 determinism failures either side |
| Project suites | MapVerification, RelayNavTest, **MapInteractionTests**, LevelDesignerTests, FixtureTickTests, CameraRigTests, CaptionsTests, GlassShotTests, StreamVerification, GlassCompileCheck: identical reports and console counts. MapInteractionTests logs 2 errors and 4 exceptions on both sides (main's own state, TOOLS.md §3). One expected difference: LevelDesignerTests' palette line reads 119 kits (86 floor, 22 wall, 7 desk-top) instead of 113 (88, 18, 7): 6 new kits, and `Kit_ExitSign`/`_Dead` are Wall now, not Floor (spec K6). 138/138 pass on both sides |
| Play run (`FrontRoomsCxPlayRun`, seed 516574485, 90 map seconds, editor autopilot) | before and after: 5,507 frames each, **1 error and 2 exceptions on both sides**, and all three come from the clone environment: the FMOD bank missing in clones, the QuickSearch index, and FMOD port 9264 busy (warnings). Nothing is logged from RoomStream, the kit library or the sign. Warning counts differ (58 vs 40) only in FMOD port retries and in the autopilot's path, which hit different module-prop clearances (1 restart after being caught before, 0 after). The first after-run under load reached only 20.4 map seconds, with the same errors |

Verification log (Figma section `2595:6093`):
- **VL210** `2860:6151`, PASS: Run signs are the kit now. Before|after: wide, sign A at 1.5 m, sign B under dead troffers, from below at 1.1 m.
- **VL211** `2860:6177`, FLAG: Exit rooms get a wall sign. Exit at 4 m and at 1.7 m.
- **VL212** `2860:6191`, PASS: lamp states and flicker. Six-state strip, Level trace and rendered probe.
- Images: `images/integrate/es_i01`–`es_i08`. The cover now reads 24 tasks · 210 checks · 622 images; the ES legend reads VL145–147, 210–212 · 6 checks.

## 7. Files to merge (the package) and the command

`W/apply/exit_sign` (apply convention v2: `exit_sign.sh`, `_bases.txt`, `_expected.txt`, `_keep.txt`, `_payload/`, `_base/`, `exit_sign.diff`). Bases recorded 2026-10-08 01:2x on main 22bb75f + working tree.

- **Dry run:** `bash /Users/redwang/FrontRoomsVisualWork/apply/exit_sign.sh` → `DRY RUN OK`.
  - 76 bases match (61 absent, 15 at their sha1).
  - 9 kept metas unchanged.
  - 76 payload files match.
  - The 2 replaced metas keep main's GUID; the 27 new GUIDs are unused in Assets.
- **Rebase check:** `W/tools/rebase_apply.sh exit_sign --no-install` → `PASS: nothing to rebase`.
- **Rehearsal:** `--apply` was run on an APFS copy of Red's Assets+Tools. Result: `MANIFEST OK`, and a second run says `ALREADY APPLIED`.
- **Apply:** `bash /Users/redwang/FrontRoomsVisualWork/apply/exit_sign.sh --apply`.
  - It backs up main's 15 replaced files to `W/backup/2026-10-07_exit_sign`.
  - It writes every .meta first, then textures, materials, shader, models, Tools and the C# last.
  - Then it prints the manifest.

| Action | Path | .meta |
|---|---|---|
| replace | `Assets/Scripts/FrontRoomsRoomStream.cs` | kept (main's) |
| replace | `Assets/Scripts/Office/FrontRoomsKitLibrary.cs` | kept |
| add | `Assets/Scripts/Rendering/FrontRoomsExitSign.cs` | **new**, guid `21969c441795c4d5f93898cd1849f194` (minimal meta) |
| replace | `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs` (build: 4 ExitSign SurfaceDefs, lampField, `Run_ExitSign` smooth .38, clamp rule) | kept |
| replace | `Assets/Resources/Rendering/FrontRoomsSurface.shader` (build: `_FR_LAMP_FIELD`, off = bit-identical) | kept |
| replace | `Assets/Resources/Surfaces/Run_ExitSign.mat` (`_Smoothness` 1 → 0.38 only) | kept |
| add | `Assets/Resources/Surfaces/ExitSign_{Housing,Face,Diffuser,Diffuser_Dead}.mat` | 4 new |
| add | `Assets/Resources/Surfaces/Textures/ExitSign_{Face_A,Face_N,Face_S,Diffuser_A,Diffuser_E,Diffuser_F,Diffuser_S,Enamel_A,Enamel_N,Enamel_S}.png` | 10 new |
| replace | `Assets/Resources/Props/Models/Kit_ExitSign(.fbx, .json)`, `Kit_ExitSign_Dead(.fbx, .json)` | `.fbx.meta` replaced **with main's GUIDs** (new material remaps, LOD %); `.json.meta` kept |
| add | `Kit_ExitSign_{Hanging,Hanging_Dead,HangingFlush,HangingFlush_Dead,End,End_Dead}` `.fbx` + `.json` | 12 new `.fbx.meta` / `.json.meta` |
| replace | `Assets/Resources/Props/Models/Kit_DoorFrame_Steel.json`, `Kit_DoorFrame_Steel_Alu.json` (anchor only) | kept |
| replace | `Tools/Blender/frontrooms_kit/assets/interact_exit_sign.py`, `interact_door_frame_steel.py` | — |
| add | `Tools/Blender/frontrooms_kit/assets/exit_sign_{common,geom,hanging,hanging_flush,end}.py`, `Tools/lookdev/gen_exit_sign.py`, `Tools/lookdev/exit_sign_measure.py` | — |

- **Not merged** (clone-only):
  - `FrontRoomsExitSignLookdev.cs`, `FrontRoomsExitSignProbe.cs`, `FrontRoomsExitSignTests.cs`, `FrontRoomsExitSignStreamCapture.cs` (copies in `integrate/code/` and `build/code/`);
  - the other materials that `EnsureSurfaces` re-saved in the build clone;
  - nothing in W/LOCAL_PATCHES.md (it lists none now).
- **Unity on apply:** if Red's editor is open, it imports 10 textures, 4 materials and 8 FBX. It re-imports `Kit_ExitSign`/`_Dead` and recompiles once.

## 8. Map contract

**None needed.** No map file is touched. The map has no EXIT signs today, and `Spawn` attaches the driver, so any future module prop or facade sign works with no map edit. The spec's proposals stay proposals, unchanged: C-1 Office-side door signs (Red's call), C-2 `MainsLevel(cell)`, C-3 "Ceiling" placement in the Level Designer (`10_spec.md` §10.3).

## 9. Findings and hand-offs

1. **FLAG (VL211): the grey face in Exit rooms.**
   - RoomStream's troffers are spot lights at 2.85 m with 162° cones, so the end wall's top 0.25 m (the header and the sign) gets only grazing light. The wallpaper round the sign is just as dark.
   - The letters read; the white enamel reads mid-grey.
   - In the look-dev room (troffer 1.25 m off the wall) the same face reads white (`t1_dead_1m_troffer`).
   - Options for Red: (a) leave it; (b) the spec's optional desktop spill point at the `glow` anchor (colour (1, .13, .07), 0.5, range 1.5 m, unshadowed, off on WebGL).
2. **Main: a dead branch in `BuildProfileProps`.**
   - `else if (rule == RoomRule.Exit) { Box(..., "exit threshold marker", ...) }` can never run, so Exit rooms have no threshold marker.
   - This package leaves it as it is (keep everything else identical).
   - If Red wants the marker, move it into the first branch, as was done for the sign.
3. **RT (G14) hand-off** (spec §7.3, unchanged): classify `FrontRoomsExitSign` renderers as `EmissiveLens`, scaled by `Level`, so dead or dim signs do not glow in traced reflections.
4. **WebGL hand-off:** `_FR_LAMP_FIELD` adds one keyword on diffusers. The WebGL track may strip it (states then become brightness-only through the light).
5. **Run! direction hand-off:** direction B (mains drop) needs one call, `sign.SetMains(0)`, on its trip. Direction A does not touch the signs.
6. **Harness lessons** (for other workflows):
   - `Transform.Find` with "/" in a name finds nothing.
   - A render inside one editor tick does not see light or property-block changes made in that tick.
   - A Unity import killed halfway can leave Play-mode "missing script" warnings; a forced reimport of `Assets/Scripts` fixes it.

## 10. Where things are

- **Report and evidence** (under `Documentation/research/exit_sign/`):
  - this file;
  - `integrate/diffs/` (RoomStream, KitLibrary, door module and sidecars, the whole package diff);
  - `integrate/code/` (driver, tests, capture harness, analysis, package builder, apply script, runner);
  - `integrate/logs/` (tests, probe, measure, frame diffs, suites, signatures, bases);
  - `images/integrate/` (es_i01–es_i08).
- **Work** (under `W` = `/Users/redwang/FrontRoomsVisualWork`):
  - `es_int/` (runs/before, runs/int, runs/int_probe, runs/final, analysis, r4_src, r5_src, tools);
  - clones `proj_es_before`, `proj_es_int`, `proj_es_final`;
  - logs `W/logs/es_int/r4`, `r5`.
