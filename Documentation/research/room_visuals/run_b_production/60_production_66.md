# 60 · Run! B production on 6000.6.3f1: running log

游戏视觉, account A, session local_5656cffd. One section per workflow stage. `W` = `/Users/redwang/FrontRoomsVisualWork`, `C` = `W/proj_runb66`, `M` = `Frontrooms3D`.

Red's calls in force: C38–C43 (2026-10-31 00:31, "全部按推荐"; systems.json L7). Testing rule (Red, 10-31 ~01:00): "做的时候先不用测，直接做，然后等都落地了一起测即可". No test rounds, smoke runs, Play-mode checks or captures in any stage. Gate = Roslyn macOS + iOS 0 errors + each package's DRY RUN OK + rehearsal MANIFEST OK. One combined test after everything lands.

---

## Stage A · rebase (10_runb_state §11 steps 1–4, no RoomStream hunk) + probes dropped

Done 2026-10-31 00:57. An earlier run of this stage stopped at about 00:50; its clone, port and logs were checked and kept, and this run finished from there.

### A.1 Clone

- `C` made by `W/tools/mkclone.sh proj_runb66` at 00:39:28 from main (.git HEAD 931ad7de, stale since the auto-commits stopped; the tree itself is rsynced). 0 dataless files in main.
- Library: `cp -Rc` from `W/proj_unlock` (6000.6.3f1, main 931ad7de) at 00:40:04.
- `install_tools.py` ran (clone-only silencer `Assets/Scripts/CloneOnly/FrontRoomsCloneOnlySilence.cs`, 42 tool files, test bundle id `com.redwang.frontroomsss.test.runb66`).
- No `--feature run_b`. That set (`W/tools/feature/run_b`: RunBSetup / Capture / Smoke) is the 6.3 harness. It calls `RunLook`, `StreamRoom` and `RoomStream.RunLookOverride`, which C40 removes. The combined test needs a new 6.6 harness.
- Main kept moving while the stage ran, so main's newer files were copied forward into `C` twice: 00:53 (11 files that landed 00:39:50–00:45:32: door_lean_flip, map, Relay) and 00:56 (15 files that landed 00:55:12, including the new `FrontRoomsFloorStation.cs`). After that, `C` = main except the port and the clone-only tools (`W/run_b66/logs/delta_vs_main_final.tsv`).

### A.2 Port by the manifest (`W/run_b66/scripts/port.py`, classify against main now: `W/run_b66/logs/classify_main_now.tsv`)

All 237 manifest rows still match their sha1 in `W/proj_runb` (read only).

| Manifest group | Rows | Done |
|---|---|---|
| FREE, project | 168 (84 content + 84 meta) | copied. 16 of them (8 cubes) then dropped (A.4), so **152 land as NEW** |
| FREE, Blender source | 19 (`run_*.py` ×18 + `_run_b_common.py`) | to the work root `W/run_b66/blender/Tools/Blender/frontrooms_kit/assets/`, not the project. No `.blend` exists for the Run kits: they are built by these modules on main's kitlib. The "19 Blender files" are these 19 modules |
| IDENTICAL | 7 PNG (`Run_Block_{A,N,S}`, `Run_SealedBeamLens_{A,E,N,S}`) | skipped. Main's content and meta stay |
| DIFFERS, content | 16 (4 FBX, 4 JSON, 4 `.mat`, 4 PNG; Stairwell A's r8 kits) | **REPLACED in place, main's GUIDs** |
| DIFFERS, meta | 23 | the clone's 23 metas are never shipped. 19 stay main's as they are. The 4 `.fbx.meta` = main's meta (GUID, 6.6 serialization) + the production importer deltas: `lODScreenPercentages` [.25, .125, .01] on all four, and on the wrap the `externalObjects` remap `Prop_SteelAlmond` |
| MOD | 4 | **none ported**: RoomStream (C40), Surface.shader / RenderSetup / URP asset (probes dropped, A.4) |

**GUID rewrites** (`W/run_b66/logs/guid_map.tsv`, 23 clone → main pairs): 18 references in 8 files.

| File | Rewrites |
|---|---|
| `RunB_BlockLower.mat`, `RunB_BlockUpper.mat` (Run_Block A/N/S) | 3 + 3 |
| `Run_SealedBeamLens.mat` | 4 |
| `Run_WrapLens.mat` | 3 |
| `Run_UnitLegend.mat` | 1 |
| `Kit_EmergencyTwinHead.fbx.meta` | 2 |
| `Kit_EmergencyTwinHead_Lamp.fbx.meta` | 1 |
| `Kit_WrapFixture_4ft.fbx.meta` | 1 |
| `Kit_EmergencyTwinHead_Yoke.fbx.meta`, `Run_NeonPilot.mat` | 0 (they point only at FREE or built-in GUIDs) |

Checks: `W/run_b66/scripts/refcheck.py` found 150 GUID references in the ported text assets, 0 unresolved. 0 files in `C/Assets` still carry a clone-only GUID. Main's `Stair_Block.mat` / `Stair_Band.mat` are untouched and keep reading main's `Run_Block` GUIDs.

**Code**
- `FrontRoomsRunRig.cs:1180` `r.GetInstanceID()` → `FrontRoomsObjectId.Of(r)`.
- C40: the `RunLook` enum and the `StreamRoom()` preset are removed from the rig. `RunRigHost.HostLamps` and the host-lamp halo helper stay compiled with no caller (doc comment says so).
- C40: `FrontRoomsRunRigTests` loses S1 / S2 and their `Sim` stream harness. R1–R7 + W1 stay.
- Probes: a doc comment on `BuildProbes` (A.4). Behaviour unchanged.
- The three `.cs` are LF and ASCII.

### A.3 Compile + import

- Roslyn on `C` after the last forward sync (`W/run_b66/logs/roslyn_{mac4,ios4}.txt`): **macOS 0 errors** (Assembly-CSharp 190 files, Editor 98), **iOS 0 errors**, FMOD assemblies clean. No warning comes from a Run file.
- One batch import, `-batchmode -nographics -quit -buildTarget OSXUniversal`, 00:55:02–00:55:37, rc 0 (`W/run_b66/logs/import1.log`): 92 imports (20 FBX, 18 textures, 2 shaders, 30 native assets, 20 text), Unity script compile 0 `error CS`, no asset error. Silent: the clone silencer, no player.
- Unity upgraded the 30 FREE metas (16 FBX, 14 PNG) to 6.6 serialization (FBX serializedVersion 24200 → 24600 plus new default fields). All 30 GUIDs unchanged. **The package ships these 6.6 metas from `C`**, not the 6.3 copies.

### A.4 Probes dropped, not measured (Red: no testing now; T8 never showed a visible gain)

- **Left out:** the box-probe hunks in `FrontRoomsSurface.shader` (+2 pragmas), `FrontRoomsRenderSetup.cs` (+2 lines) and `FrontRooms_URP.asset` (2 flags), plus the 16 cube paths (8 EXR + metas). In `C` all four shared files equal main: RoomStream 675d5589, Surface.shader bc5a0d08, RenderSetup a871837a, URP 18584f2b.
- **Moved, never deleted:**
  - the stream-room cubes → `W/_partial/runb66_c40_streamroom_cubes_20261031/`;
  - the Corridor1x9 cubes, the 3-way merged shader (`FrontRoomsSurface.shader.probe_merged`) and the stopped run's never-run P0/T8 harness `FrontRoomsRunB66Probe.cs` (from `C/Assets/Editor/RunB66` and `W/tools/feature/run_b66`) → `W/_partial/runb66_probes_dropped_20261031/`.
- The three trial merges (Surface.shader, RenderSetup, URP; 0 conflicts) stay in `W/run_b66/merge/*.merged`.
- **Rig behaviour now:** `Corridor1x9` still declares its 3 probe boxes. `BuildProbes` finds no cube (`Resources.Load` returns null), so it builds no probe and `ProbeCount` is 0.
- **For the combined test to revisit:** without per-state cubes, a tripped floor takes the zone cube (`Refl_Level0` / `Refl_Office` through `FrontRoomsLook.SetZoneReflection`), which may reflect the bright zone in a dark corridor (10_runb_state §12). If it shows, the cheap fallback is per-object probes: the two cubes back, with no shared-file hunk (Surface.shader / URP flags not needed for unity_SpecCube0).

### A.5 Clone delta vs main (00:57)

| Kind | Paths | What |
|---|---|---|
| NEW (ship) | 152 | 76 content + 76 meta: 16 FBX + 16 JSON kits, 24 `.mat`, 14 PNG, `RunBeamCone.shader`, `RunSoffitHalo.shader`, `FrontRoomsPost_Run_B.asset`, `FrontRoomsRunRig.cs`, `FrontRoomsRunDress.cs`, `FrontRoomsRunRigTests.cs` (list: `W/run_b66/logs/port_new_paths.txt`) |
| REPLACED (ship) | 20 | 16 content + 4 `.fbx.meta` (list: `W/run_b66/logs/port_replaced_paths.txt`) |
| MODIFIED shared files | 0 | no RoomStream, Surface.shader, RenderSetup or URP hunk |
| Clone-only (never ship) | 45 new + 1 changed | tools, silencer + metas; `ProjectSettings.asset` differs only in the test bundle id |

### A.6 What the next stage must know

1. **The stairwell is wrong in `C` until step 6.** `FrontRoomsStairwellDresser.cs` now spawns the production kits with the r8 numbers: l.628 cabinet at `-inner + .05f` (the new origin is on the wall, so it stands 50 mm off), l.638 old mounts (.105, .191, .008) where production is (.112, .201, .059), l.652 old feed point (production 0, .1965, .03), and l.619 / l.635 one lens / pilot renderer where the LODs need all of them (the C22 blackout would miss LOD0). The dresser fix goes in the same package as the 20 REPLACED paths. `stairwell_a.sh` refuses existing paths, so `run_b.sh` v2 must list these 20 as REPLACED with main's current bytes as their bases.
2. **Side doors** (`FrontRoomsRunDress.SideDoors`) still build `Kit_DoorFrame_Steel` + `Kit_DoorLeaf_Steel` + `Kit_DoorCloser_Body`. They should be RN-K on `_DA` kits, F4 offsets, no closer, `FrontRoomsDoorLite.AttachDead` (step 7).
3. **C41 is not applied yet.** `Corridor1x9` has `flagSign = true` (z 13.5) and `goalWallSign = true`, and the rig builds both EXIT signs and their face lights. Remove them and keep the fire gear red.
4. **C38 / C42 are not built.** The rig builds no goal door (RN-F = `Kit_DoorFrame_Steel_DA` + `Kit_DoorLeaf_Ward_DA` + F5, never locked; M8 is 关卡设计's). There is also no rig-held lite light (unshadowed, behind the goal wall, aimed away, cull-exempt, fading out as the far room's lamp comes on, none on WebGL).
5. **Not built:** the handheld tier (`FrontRoomsHandheld`), isolation from map lamps, and RELAY §6's entrance signature (red-banded ceiling detector + emissive ALARM ZONE card slot with a STAND-IN face, flagged; lettering through 平面视觉).
6. Main's shared files move daily. Take bases at package time. This stage touches none of them.
7. Old 10-22 findings F1–F12 are still open (20_build.md §9.4).

---

## Tell · the shared ALARM ZONE signature kit (RELAY §6 + row 10)

Done 2026-10-31 01:2x, account A, session local_5656cffd. Clone `W/proj_alarmtell` (mkclone 00:38 from main 931ad7de; Library from `W/proj_audit`, 6.6; `install_tools.py` ran inside mkclone, silencer in). An earlier run of this stage stopped after writing `alarm_tell_common.py` and `alarm_detector.py` in the clone. Both were checked and kept, and this run finished from there. Independent of Run! B: it marks every trigger room and references no Run type. Full write-up: `research/room_visuals/50_alarm_zone_tell.md`. Ask to 平面视觉 · 信息设计: `research/room_visuals/51_alarm_zone_for_graphic.md`. Media wanted (not downloaded): `media_candidates.md` § "50 ALARM ZONE tell".

**Package: `W/apply/alarm_tell.sh` (v2), READY, not applied.**
- 34 paths, all NEW (bases all ABSENT, nothing replaced, no kept .meta), 16 new GUIDs, none used in main.
- Dry run on main 01:18: `DRY RUN OK: every base matches`.
- Rehearsal on a scratch copy of main (`W/tools/_test/alarm_tell_1031/`, every main .meta + the files section 5 reads): `MANIFEST OK … all 34 touched paths`, then `ALREADY APPLIED`. A half-applied target was `REFUSED`.
- Compile: Roslyn `--all` on the clone, macOS and iOS, 0 errors. Also against **main NOW** with the three .cs as overrides: Assembly-CSharp 192 files, Editor 81, 0 errors (`W/alarm_tell/roslyn/`).
- No render, no Play mode, no test run (Red's rule). Builder: `W/alarm_tell/tools/build_package.py`. Unity-side files: `W/alarm_tell/tools/make_assets.py` (GUIDs in `W/alarm_tell/guids.json`).

| Built | Numbers |
|---|---|
| `Kit_AlarmDetector` (Blender `alarm_detector.py`): ivory head Ø129, 36 vent fins, fire-red base Ø156 + trim ring Ø200, red LED; origin on the ceiling plane, drop 71.5 mm | LOD0 / 1 / 2 = **8,590 / 1,210 / 314 tris**, hand-built; 4 slots → 4 draws; switch 3 m / 9 m, cull 30 m; no collider, no shadow |
| `Kit_AlarmZoneCard` (`alarm_zone_card.py`): 1990 lit sign, red box 286 × 184 × 51 mm, 254 × 152 mm lit face | **734 / 46 / 14 tris**; 3 draws then 2; switch 5 m / 15 m, never culled |
| Materials `Alarm_Shell` (ivory, kit plastic maps), `Alarm_BandRed` (#B01E18 = B's `RunB_FireRed`), `Alarm_Grille`, `Alarm_LED` (emissive, steady 0.45 as the asset), `Alarm_CardFace` (albedo + glow, emission 1.2) | FrontRooms/Surface, `_FR_MESH_UV`, no `_FR_OWN_LIGHTS` |
| STAND-IN face `Resources/Print/AlarmTell/alarm_zone_card_{albedo,glow}.png` (TeX Gyre Heros Bold, white on red, "ALARM"/"ZONE", "STAND-IN" mark) | 2032 × 1216, 8 px/mm; generator `Tools/lookdev/gen_alarm_card_standin.py` |
| `FrontRoomsAlarmTell` (static API, INERT: no caller, no init hook) + `FrontRoomsAlarmTellLed` (one shared clock, made by the first Dress in Play mode) | blip 0.14 s every 10 s (System Sensor 1400/2400 period behaviour), 3 phases; Reduce Flashing = steady; `SetAlarm` latch optional |
| `FrontRoomsAlarmTellTests` (Editor, ASCII, no tuples) | for the joint round only: `-executeMethod FrontRoomsAlarmTellTests.RunBatch` |

**API.** `Dress(entrance, openingWidth, wallThickness, ceilingHeight, doorHeadHeight, cardSide, roomDepth, detectorDepth)`:
- Entrance frame: the floor at the opening's centre, on the wall's centre plane, +Z into the room.
- Card: outside face, centre 1.524 m, at x = ±(w/2 + 0.0508 + 0.10 + 0.143), facing out.
- Detector: on the ceiling centreline at `DetectorDepth` (the sight rule: seen from 2 m outside over the head), LED toward the entrance. That is 3.69 m for a door under 2.9 m, 2.71 m for a 2.2 m arch, clamped to 6.08 m in a 5.4 m Tall zone (`SeenFromOutside` = false; the card carries the tell).
- Also `DressCeiling`, `DressCard`, `SightDepth`, `SeenFromOutside`, `CardPoint`, `Find`, `Undress`, `SetAlarm`, `ReducedTier` (handheld + WebGL: the detector's LOD0 dropped, nothing else changes), `CardFaceIsStandIn`.

**What the next stage must know.**
1. The Run! rig needs **no change** for the tell. When 关卡设计's CR-1/CR-2 rooms exist, the map calls `Dress` per entrance at chunk build, with `doorHeadHeight = ModuleUnits.ArchTop` for the south arch. The goal door (C38) is an entrance from the far side and gets its own `Dress`.
2. B's overhead services must leave a 0.25 m clear ceiling disc on the corridor centreline at z ≈ 2.71 m past the arch's wall centre (sprinkler main 2.62, tray 2.70, EMT 2.72–2.80 m: `run_calls/10` §8). Do this when B's layout is rebased.
3. The card does not replace B's NOTICE plate. Put them on opposite sides of the arch. The card says no EXIT and is not an EXIT form (C41).
4. The tell adds no light (no realtime light, no shadow caster), so it does not touch the map-lamp leak or C42's rig-held light.
5. The joint test round: `FrontRoomsAlarmTellTests.RunBatch`, then one look (`50` §7). The card legend should be 0.6–1.0× a lit Office wall (gain = `Alarm_CardFace` `_EmissionColor` 1.2), the red ring readable from 2 m out, the LED blip never the first thing seen, a phone frame at LOD1.
6. Open for others:
   - 平面视觉's art and wording (stand-in until then; set `CardFaceIsStandIn` false);
   - 系统设计: whether the trigger owner wires `SetAlarm` (period latch, steady red after a trip, never blinking);
   - the dead-circuit variant (v2.1) is not built.

---

## Stage B · steps 6 + 7, C41, C42, the rig wired to 关卡设计's hooks

Done 2026-10-31 11:2x, account A, session local_5656cffd, in `C`. An earlier run of this stage stopped at 01:45 after the dresser, the side doors, C41, C42, the CR-4 burst and a binder that a proposed map hunk called (`W/apply/run_b_contract/CONTRACT_map.diff`). All of it was read and kept. The contract approach was dropped: the hooks are in main, so the binder now attaches itself in visual-owned code, and no map file is touched. Main has not moved since 01:41 (`W/run_b66/logs/delta_vs_main_stageB.tsv`). Nothing was tested, played or rendered (Red's rule). Snapshots of the stopped run's files: `W/run_b66/merge/stageB_pre/`.

### B.1 Step 6: the stairwell dresser on the production kits (`Rendering/Stairwell/FrontRoomsStairwellDresser.cs`, MODIFIED, main base e4208aab)

- Cabinet: its back on the wall from the sidecar bounds, `-inner - cabInfo.Min.z` (production origin is on the back face, Min.z 0; r8's mid-depth origin gave `-inner + .05`).
- Yoke mounts, trunnion and EMT feed from the sidecar anchors (`mount_l` / `mount_r` (±.112, .201, .0593), `trunnion` (0, .085, 0), `feed` (0, .1965, .03)), with the production values as fallbacks (`KitAnchor`).
- Lens and pilot: every LOD's renderer and slot is kept (`lenses` / `pilots` lists) and all follow `LampLevel`, so the C22 blackout reaches LOD0.
- The wrap's FBX and its atlases move together (stage A's 20 REPLACED paths). The wrap is found by slot name, so its third slot (`Prop_SteelAlmond`) needs nothing.
- Nothing else in `Assets/Scripts` or `Assets/Editor` uses the four swapped kits.
- **Not gated by runRooms.** This changes the live safe-zone stairwell (production kits, LODs). The combined test checks it: exit run + station, C22 blackout on LOD0, cabinet flush on the wall.

### B.2 Step 7: the side doors on the RN-K DA kits (`FrontRoomsRunDress.SideDoors`)

- `Kit_DoorFrame_Steel_DA` + `Kit_DoorLeaf_SteelRun_DA` on the hinge point, as the map hangs them. No closer (A12).
- F4 offsets re-derived for a prop with no opening: the frame is squeezed along the wall normal (×0.92 with the 0.16 m wall) so its far casing sits 2.6 mm inside the wall and never shows on the neighbour's face. The near casing is 10.2 mm proud. The leaf face is 2 mm proud.
- The F5 lite is `FrontRoomsDoorLite.AttachDead` (dark wired glass for good). The kit is spawned without colliders or EXAMINE, so nothing offers a peek onto the wall.
- Locked trim (escutcheon, cylinder, plug, knob) on the corridor face at the kit's `escutcheon_s` anchor, satin chrome / stainless swaps as `DoorParts` does, the leaf in `Prop_SteelAlmond`.
- Plate or NOT AN EXIT stencil centred on the leaf at 1.75 m, above the lite (1.194–1.575 m).
- Wall finishes leave ±0.545 m round each door (under the casing's 0.549). The skin's header over a door starts at 2.12 m, under the casing top (2.149).

### B.3 C41: no EXIT sign in a Run! room

- `BuildSigns`, both signs (the flag sign at z 13.5, the sign over the goal door), their face spots and every `FrontRoomsExitSign` reference are gone from the rig. The ExitSign system is untouched.
- The fire gear stays red: sprinkler main, pull station, bell, extinguisher cabinet (test R8 lists them).
- Light budget now: armed 10 (9 wraps + the goal window light, 3 shadowed), blackout 1, tripped 9 (8 heads shadowed + the goal window light) (R7).

### B.4 C42: the goal window light (`FrontRoomsRunRig.BuildGoalLight` / `UpdateGoalLight`)

- One unshadowed spot, 0.45 m past the goal wall's far face, 2.2 m up (at most the far ceiling − 0.25), pitched 32° down, aimed away from the door. Outer 120°, inner 80°, range 8 m, intensity 5. All are SP values, tuned in the combined test.
- Built after the cull list, so the 16 m rig cull never touches it (F1's shape, for the lite). It ignores the trip, since the next zone is normal. Not built in the WebGL tier (no lite there).
- Weight = 1 − the far room's lamp. The binder reads the far lamp from the map: `LampLevel` of the goal's far cell and the next cell along the axis, times the map's own fade (`Profile.lightRadius`, the last 3 m, from the player). The brighter of the two is used.
- With no far lamp, the light fades from 6 m to 2.5 m from the door. It is off once the viewer is past the goal wall and beyond 40 m.
- Colour is the far zone's lamp: Level 0 or Office (C39), or the light lead's colours when that is on.
- Geometry check (arithmetic): the cone reaches no corridor floor (74° off axis at the goal wall's foot against a 60° half angle), and the goal wall's corridor face is back-lit. So it should not leak into the tripped corridor. The combined test confirms it.

### B.5 The rig on the map's hooks (`Rendering/FrontRoomsRunRigBinder.cs`, NEW, GUID 780b86179c1a421981d8fd06a8b9252c, unused in main)

- **Attach (no map edit):** `[RuntimeInitializeOnLoadMethod]` subscribes to `FrontRooms3DGame.MapRunStarted` and `FloorChanged`, as `FrontRoomsStationPaper` does.
  - It binds only when `map.RunRoomsOn` is true. With runRooms false (the master switch today) nothing is added and nothing subscribes.
  - The viewer is `map.Player`.
  - `MapRunStarted` is raised after the game's `WireRunRooms`, and `Bind` picks up rooms already built (`IsRunRoomBuilt`).
- **RunRoomBuilt:**
  - The root is placed at `RunRoomPoint(room, 0)` with `RunRoomRotation(room)`.
  - The dress is built in skin mode (B.6).
  - The rig is built with `OwnLamps` (CR-4: no map lamp in the cells).
  - The tell is dressed at both entrances (B.7).
  - The C42 far lamp is wired.
  - A room rebuilt while its alarm stands comes back tripped (`TripImmediate`, no events).
- **RunRoomDropped:** the root is destroyed. **RoomTriggered (Alarm):** `Trip`. **RoomReset:** the animated re-strike within 30 m, else armed at once. **RunRoomWarned:** `Warn(stage)`, the map's M3 burst envelope on the rig's wraps, with a REDUCE FLASHING sag (R11).
- **The goal door is the map's** (C38: RN-F + F5 via M8, never locked). The rig builds none. Where it comes from:
  - `RunEntrance(room, true)` gives the opening centre on the goal wall line and the outward normal. That sets `goalPoint` / `goalNormal`, which the goal head aims at.
  - `DoorBetween(Cells[8], Beyond).hinge` gives the latch side, for the card.
- **Static events** `RigBuilt` / `RigDropped(room, rig)` let 声音设计's CR-S3 find each rig's `MainsDropped` / `TransferRelay` / `StateChanged`.

### B.6 B's surface set on the Run! room (CR-3) without touching RoomRule.Run

- `FrontRoomsSurfaces.Room(RoomRule.Run, …)` is read by the title stream too (`FrontRooms3DGame.cs:708`). Mapping B's set onto it would re-skin the stream's Run room, which C40 keeps on Ward. So it stays Direction A's set.
- Instead, the dress skins every face the map's CR-3 finish would show:
  - the soffit;
  - the upper tone over the long and end faces;
  - the lower tone and cove (on the map they run on behind the semi-recessed extinguisher cabinet and stop only at the side doors; the rail still skips the cabinet);
  - new `Reveal`: the arch's reveal on the room's half of the wall (the map splits a wall's two finishes at its centre line). That is both jambs in lower tone + cove + upper tone, and the head's underside, 2 mm, no collider, no shadow.
  - The goal door's DA frame sleeves its own reveal. The VCT covers the floor from z 0 to 27.
- So the map's Run finish shows nowhere, and no map hunk is needed.
- Optional for 关卡设计, not requested: `RunMaterials()` could read B's materials for the hidden shell. That is a nicety, not a gap.
- The unused `FrontRoomsRunDress.ShellMaterial` (it only served the dropped map hunk) was removed.
- Test R12 (new) checks the soffit, the 7 reveal slabs (none at the goal), and the lower tone behind the cabinet.

### B.7 The shared tell at both entrances (`FrontRoomsAlarmTell`, landed in main 01:2x)

- **Arch:**
  - The ALARM ZONE card is on the outside face, on the approacher's left (`CardPoint(1.4, t, −1)`). B's NOTICE plate moved to the outside face on the right at z −0.08 (F8: it was buried in the wall).
  - The detector is on the ceiling centreline at the sight depth for a 2.2 m arch (2.71 m), pushed clear of the wraps (±0.6175 + 0.15).
- **Goal door:**
  - The card is on the latch side, outside.
  - The detector's sight depth for a 2.1 m door is 3.69 m (z 23.31). It is pushed clear of the wrap at 22.8, to z 22.03 (deeper, so it is seen from farther out).
- The detectors hang under the dress, so the rig's corridor ambient holds. The cards hang under the root, in the host's light.
- `LatchAlarmLed` (off) is ready for 系统设计's open period-latch call.
- Overhead clearance on the centreline (arithmetic): main at x .55, tray rods at −.41 / −.79, EMT on the wall. The Ø200 ring is clear.

### B.8 Outlets on Run walls (`Office/FrontRoomsWallFixtures.cs`, MODIFIED, main base ba8830b5, visual-owned)

- In `InstallBody`, a face whose cell is a Run! cell is left out (`world.RunRooms.IsRunCell`). The neighbour's side keeps its own.
- With runRooms off (`RunRooms` null) or no room placed, the code path is today's byte for byte.
- No Run! face is ever foreign: the planner keeps every edge it writes inside its two chunks, so the seed-pure foreign plan is unaffected.

### B.9 Compile

- Roslyn on `C` (`W/run_b66/logs/roslyn_{mac7,ios7}.txt`): **macOS 0 errors**, **iOS 0 errors** (Assembly-CSharp 198 files, Editor 99, FMOD clean). No warning from any edited file.
- All edited `.cs` files are LF. The Run files are ASCII. `FrontRoomsWallFixtures.cs` keeps main's 12 non-ASCII comment lines, and the added lines are ASCII.

### B.10 Contract / MISSING

- **No contract and no MISSING.md: nothing is missing.** The binder uses only what main exposes:
  - `MapRunStarted`, `FloorChanged`, `RunRoomsOn`, `Player`;
  - the five Run! events;
  - `RunRoomPoint` / `RunRoomRotation` / `RunEntrance`, `DoorBetween`, `LampLevel` / `NoLamp`, `ZoneOf`, `Profile.lightRadius`;
  - `RunRooms.Rooms` / `IsRunRoomBuilt` / `IsRunCell`.
- The stopped run's `CONTRACT_map.diff` (3 hunks: a `Bind` call in `WireRunRooms`, the outlet skip in `MapWorld.AddWallFaces`, `RunMaterials` → B) is superseded by B.5 / B.8 / B.6. It was moved with its work dir to `W/_partial/runb66_contract_superseded_20261031/`. `W/apply/run_b_contract/` no longer exists.

### B.11 For the land stage (`run_b.sh` v2)

| Kind | Paths |
|---|---|
| NEW | 154: stage A's 152 (`W/run_b66/logs/port_new_paths.txt`; ship the 30 FREE metas as Unity 6.6 wrote them in `C`) + `Assets/Scripts/Rendering/FrontRoomsRunRigBinder.cs` + `.meta`. `FrontRoomsRunRig.cs`, `FrontRoomsRunDress.cs` and `FrontRoomsRunRigTests.cs` are among the 152, in their stage B state |
| REPLACED | 20 (`port_replaced_paths.txt`), with main's current bytes as bases |
| MODIFIED, visual-owned | `Assets/Scripts/Rendering/Stairwell/FrontRoomsStairwellDresser.cs` (main e4208aab), `Assets/Scripts/Office/FrontRoomsWallFixtures.cs` (main ba8830b5) |
| Not shipped | every map file (none changed), RoomStream / Surface.shader / RenderSetup / URP asset (equal to main), the 45 clone-only tool paths, `ProjectSettings.asset` |

- Take the bases at package time. If main moved, re-run `W/run_b66/scripts/delta.py`.
- The 19 Blender modules are in `W/run_b66/blender/` (stage A kept them out of the project). Decide whether they go to `Tools/Blender/frontrooms_kit/assets/` in main.
- **Inert:** with runRooms false the binder never binds, and the outlet filter is a no-op. **Live:** the stairwell dresser (step 6).

### B.12 For the combined test (once, after everything lands)

- `FrontRoomsRunRigTests.RunBatch`: R1–R12 + W1.
- Exit run + station on the production kits: C22 blackout on LOD0, cabinet flush on the wall.
- `-autopilotRunRooms 2`:
  - one room per host (Level 0 and Office, C39: the hand-over frames, green Office arch → B corridor, and the wall-leak check);
  - the lit F5 square from the trip line (25 m) and from 8 m (C42);
  - goal reads "brightest, or second after a lamp" (call 4);
  - no EXIT sign (C41);
  - the tell's card and detector read from 2 m out at both entrances;
  - the arch reveal against any transition-kit frame on that edge, the extinguisher cabinet's tub against the skin behind it, the side-door slits (the leaf is 2 mm proud on bare wall).
- Still open, not in this stage:
  - step 8 (F1's goal head in the cull, F3 reset strobe, F11, F12, F8 plate check, the wrap lens emission);
  - step 9 (the handheld tier);
  - step 10 (map lamps leaking into the tripped corridor);
  - the stage A probe caveat (a tripped floor may reflect the bright zone cube);
  - the Relay contrast (T6 with Hunter's T2).
