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

---

## Land · `run_b.sh`, the inert core for 11-03 (package READY, not applied)

Done 2026-10-31 11:3x, account A, session local_5656cffd. No earlier run of this stage had left anything (no `run_b.sh`, no work dir). Nothing was tested, played, rendered or heard: no Unity process ran in this stage (Red's rule, COMMON rule 25). Work dir `W/run_b66/land/`.

### L.1 Clones re-synced to main as it is now (11:2x)

- **`C` (`proj_runb66`):** nothing to bring in. Delta against main (`W/run_b66/land/delta_runb66_post.tsv`): 0 main-only paths, 0 changed main files; the only differences are the port (152 NEW + 20 REPLACED), stage B's 4 paths (binder + .meta, the dresser, WallFixtures) and the clone-only tools. Main's bases for the two modified files are unchanged (dresser e4208aab, WallFixtures ba8830b5). Main has not moved since 01:41 (last LANDINGS row 01:37).
- **`W/proj_alarmtell`:** rsynced forward from main without `--delete` (40 main-only paths and 36 changed files came in: the Run! map hooks, B-floor stations, unlock cutscene, station paper, register, the card face). Its 3 stray `__pycache__/*.pyc` were moved, not deleted, to `W/_partial/alarmtell_resync_pyc_20261031/`. It now equals main except the clone-only tools (`W/run_b66/land/delta_alarmtell_post.tsv`). The alarm tell's payload is in main (alarm_tell 01:21 + alarm_card_face 01:23), so nothing of it is kept apart.
- `install_tools.py` ran on both (42 tool files unchanged, silencer in, test bundle ids `…test.runb66` / `…test.alarmtell`). `apply_local_patches.py`: nothing to do.

### L.2 The package (`W/apply/run_b.sh`, apply convention v2; builder `W/run_b66/tools/build_package.py`)

| Kind | Paths | What |
|---|---|---|
| NEW | 173 | stage A's 152 (16 kits FBX + JSON with their 6.6 metas, 24 materials, 14 textures, 2 shaders, `FrontRoomsPost_Run_B.asset`, `FrontRoomsRunRig.cs`, `FrontRoomsRunDress.cs`, `FrontRoomsRunRigTests.cs`, each with its .meta) + the binder (`FrontRoomsRunRigBinder.cs` + .meta) + **the 19 Blender modules** to `Tools/Blender/frontrooms_kit/assets/` |
| REPLACED | 20 | the production stairwell kits (stage A list), bases = main's bytes now |
| MODIFIED | 2 | `Rendering/Stairwell/FrontRoomsStairwellDresser.cs`, `Office/FrontRoomsWallFixtures.cs` |
| Kept | 14 | main's .meta of every replaced / modified path the payload does not carry (12 kit/material/texture metas + the 2 .cs.meta) |

- 195 paths, 81 payload metas: 77 new GUIDs (unused in main), 4 replaced `.fbx.meta` keeping main's GUID. 37 GUIDs referenced outside the payload, all resolved: 29 main assets (`FrontRoomsSurface.shader`, the `Prop_*` slot materials, MacroWear, the kept `Run_*` materials and the identical `Run_Block` / `Run_SealedBeamLens` maps) and 8 URP package assets (`run_b_external_guids.txt`, checked by the script). `Run_StencilNotExit.mat` is URP Lit alpha clip by design (20_build §2.2).
- **The 19 Blender modules ship** (decision of this stage): main has no generator for any Run kit, including the four stairwell kits this package replaces; the alarm tell and the unlock cutscene shipped theirs the same way. Not compiled by Unity; no absolute path in them.
- Files: `run_b.sh`, `run_b_{bases,keep,expected,external_guids}.txt`, `run_b_payload/` (10 MB), `run_b_base/` (exact copies of the 22 replaced + modified bases), `run_b.diff` (13,858 lines; binaries by sha1). All LF.
- Section 5 of the script refuses if main stops declaring what the payload reads: `MapRunStarted` / `FloorChanged`; the map's `RunRoomsOn`, `RunRooms`, the five Run! events, `RunRoomPoint` / `RunRoomRotation` / `RunEntrance`, `IsRunRoomBuilt`, `DoorBetween`, `LampLevel` / `NoLamp`, `Player`; `FrontRoomsRunRooms.IsRunCell` / `Placed` / `ArchWidth` / `Length`; the tell's `DressCeiling` / `DressCard` / `DetectorDepth` / `CardPoint` / `SetAlarm`; `FrontRoomsDoorLite.AttachDead`; the 18 main kits it spawns by name; and if main already uses a Run rig type, or `runRooms` is no longer false (profile default and every level asset).

### L.3 Gate (compile + dry run + rehearsal only)

- **Dry run on main** (11:35): `DRY RUN OK` (173 new absent, 22 at their recorded sha1, 14 kept metas unchanged, 195 payload files match, GUIDs and section 5 clean). Log `W/run_b66/land/dryrun_main_final.txt`.
- **Rehearsal** on an APFS copy of main (`W/tools/_test/run_b_1031/`, Assets + ProjectSettings + Packages + Tools): `--apply` → `MANIFEST OK … all 195 touched paths; 14 kept .meta files unchanged` (22 originals backed up beside it); run again → `ALREADY APPLIED`. A second copy with 2 payload files pre-written (`W/tools/_test/run_b_1031_half/`) → `REFUSED: partially applied`, nothing written. No manifest was written into `W/apply`.
- **Roslyn on main + payload** (the rehearsal tree after `--apply`, `--all`): **macOS 0 errors, iOS 0 errors** (Assembly-CSharp 197 files, Editor 82, FMOD 5 assemblies clean). No warning from a payload file. Logs `W/run_b66/land/roslyn_{mac,ios}.txt`.

### L.4 Static inert proof (`W/run_b66/land/inert_proof.txt`)

- Main has no non-comment use of `FrontRoomsRunRig` / `RunDress` / `RunRigBinder` / `RunRigTests` (the one hit, `FrontRoomsRunRooms.cs:66`, is a doc comment).
- Main + payload: only the payload's own four files use them.
- The payload's only new entry point is the binder's `RuntimeInitializeOnLoadMethod`: it adds one handler each to `FrontRooms3DGame.MapRunStarted` and `FloorChanged`, and each returns at `!map.RunRoomsOn`. `RunRoomsOn` = the map holds a Run planner, made only with the profile's `runRooms` (default false, no level asset sets it) or the editor-only `-autopilotRunRooms`. The dresser's `RuntimeInitializeOnLoadMethod` is main's own, unchanged.
- `WallFixtures`: with `RunRooms` null (runRooms off) the face list is `AddRange(faces)`, today's.
- **LIVE at once, by design:** the stairwell's twin-head unit, head lamp, yoke and wraparound become the production kits (20 REPLACED paths) dressed by the step-6 dresser, and its unit legend becomes the production face (a STAND-IN, see L.6). `FrontRoomsKitLibrary.Prewarm` also loads the 16 new kits (memory only: no kit is a pile piece, none carries a tag that main selects by, nothing is placed).

### L.5 Contract for 关卡设计: none, nothing missing (re-checked against main now)

- `W/apply/run_b_contract/` stays retired (stage B moved it to `W/_partial/runb66_contract_superseded_20261031/`). No `MISSING.md`.
- **The tell's Dress calls:** in `FrontRoomsRunRigBinder` on `RunRoomBuilt`, at both `RunEntrance` anchors: the arch (card outside on the approacher's left, detector at the arch's sight depth) and the goal door (card on the latch side from `DoorBetween(Cells[8], Beyond).hinge`, detector at the door's sight depth). `ModuleTrigger` has only `Alarm`, and only Run! rooms carry it, so that is every trigger-room entrance today. A future trigger kind calls the public `FrontRoomsAlarmTell` API (or gets a binder like this one). Main has no other detector prop, so no harmless detector exists.
- **C42's far lamp:** the binder reads `map.LampLevel` of the goal's far cell and the next cell along the axis, times the map's own fade (`Profile.lightRadius`, the last 3 m), the brighter of the two.
- **The goal door (C38):** the map's `DoorMemberFor` gives `Run` for a Run door; main's `DoorParts` row RN-F is `Kit_DoorFrame_Steel_DA` + `Kit_DoorLeaf_Ward_DA`, and `DressDoor` attaches the leaf's vision lite (F5) through `FrontRoomsDoorLite.AttachMap`; never locked (`LockedByRule`). Nothing for us to add.

### L.6 Asks in writing (not sent)

- **声音设计:** `research/room_visuals/run_b_production/61_for_sound.md`: the CR-S3 hooks (`RigBuilt` / `RigDropped`, the rig's `StateChanged` / `MainsDropped` / `TransferRelay` / `ResetStarted` with positions), CR-S4 (the rig's light names; none matches the hum's `Light` / `fluorescent light*` rule, so nothing hums unless they want the wraparounds to), CR-S1 (`Surface.VCT` exists; `RunRooms.IsRunCell`), CR-S2. Nothing blocks the package.
- **平面视觉:** `62_for_graphic.md`: the seven STAND-IN faces (unit legend, NOTICE / ELECTRICAL / STAFF plates, NOT AN EXIT stencil, pull station, extinguisher legend) in main's TeX Gyre Heros, sizes, mounting, what to send back. The unit legend first: it is live in the stairwell once this lands.
- The ALARM ZONE card's path stays `51_alarm_zone_for_graphic.md`: answered by 信息设计 (art in main 01:23); `CardFaceIsStandIn` stays true until 叙事设计 signs the wording.

### L.7 What the apply and the combined test must know

1. Apply: `bash W/apply/run_b.sh` (dry run) right before `--apply` on main; CLAIM / LANDED rows in LANDINGS (rule 19). If main moved: `W/tools/rebase_apply.sh run_b`, then rebuild with `build_package.py` if a base or a kept .meta changed.
2. On first open Unity imports 173 new and re-imports 20 replaced assets (4 FBX with their LOD settings). The new metas are Unity 6.6 serialization already.
3. The combined test list is B.12, plus: the stairwell legend at 0.3 m and the shipped Blender modules building against main's kitlib (one dry build of `run_wrap_4ft.py`, no import).
4. For a playable Run! room nothing more is needed from 游戏视觉 or from the map hooks. Turning `runRooms` on in the level profile (tests: `-autopilotRunRooms 2`) is 关卡设计's switch; Run! playable earliest 11-04 per 系统设计. Still open on our side, after the combined test: stage B's list (step 8, step 9 handheld tier, step 10 map-lamp leak, the probe caveat, the Relay contrast).

---

## Stage C · step 8 fix round, step 9 handheld tier, step 10 isolation from map lamps, C39 Office hosts

Done 2026-10-31 12:2x, account A, session local_5656cffd, in `C`. No earlier run of this stage had left anything. The core (`run_b.sh`) **landed in main at 11:38** (LANDINGS 1424/1425), so this stage works on top of main as it is now: `C` was forward-synced from main at 12:15 (76 files: Run_UnitLegend_A final face 11:44, sound 11:45/11:50, sash 11:47, BS4 stations + Level0 switch-on 11:56/11:58, Quality 11:57, door dust 12:08, and the 19 Blender modules main got from the core), `install_tools.py` re-run (42 unchanged, silencer in). After that `C` = main + this stage's 23 paths + the clone-only tools (`W/run_b66/stageC/delta_post_sync.tsv`). Snapshots before the edits: `W/run_b66/merge/stageC_pre/`. Nothing was played, rendered, measured or heard (Red 10-31). Every fix below is designed from the 10-22 numbers (20_build.md §9) and the state report (10_runb_state.md); the predicted effect is arithmetic, to be confirmed in the combined round.

### C.1 Step 8: the fix round

| Finding (10-22 number) | Change | Predicted (arithmetic) |
|---|---|---|
| **F1** the 16 m cull culled the goal head from the trip line (goal door .0102 vs .1343 cull off); far third dark | `Cull()` rewritten: every light runs while the viewer is in the room (box = the walls' centre lines) and fades over `cullBoxFade` 3 m outside it; farther, each light fades over its last 3 m before 16 m (the map's own lamp rule, XZ) instead of a hard toggle. The goal head never culls. C42's window light stays outside the cull. Lights now carry `drive` (state) x the cull weight, written every Tick, allocation-free. WebGL keeps its per-light 10 m rule (no box) | trip line: all 8 heads + the window light on: goal door ~.1343, pool z 18 .426 (-13 %, PASS) |
| **F3** reset strobe 4 flashes/s (two flicks per cell, 0.15 s stagger) | `RestrikeLevel`: **one flick per wrap** (0.55 for 0.08 s), the flicks rippling 0.025 s a cell from the entrance (neighbours overlap into one bump), dark, then one rise from 0.35 swept from 0.45 s at 0.15 s a cell. `resetFlickPeriod` gone; new `resetRippleSeconds .025`, `resetFlickLevel .55`, `resetRiseStart .45`; reset 2.4 s (was 2.63). REDUCE FLASHING path unchanged (0.5 s rises, no flick) | any window: down (heads out), up + down (ripple), up (sweep) = at most 2 flashes/s; per lamp 1.5/s |
| **F8** NOTICE plate buried | Stage B had moved it to the host face (z -0.08, right of the arch). Now every wall item outside the corridor hangs under the **room root**, not the dress, so it takes the host's light and ambient (as the ALARM ZONE cards) and no gate. The kit's back is its origin (sidecar z .0002-.0034), so it sits on the face, not in it | S2 shows it |
| **F2** dolly 1.87, cart 1.32 / 1.47 / 1.76 at sprint | Albedo, not light: `RunB_ChairGrey` sRGB .373 -> .235 (linear .115 -> .045), metallic .3 -> 0 (painted steel), smoothness .42 -> .35; `RunB_CartPlastic` sRGB .671 -> .200 (linear .41 -> .033, the black period janitor cart; the yellow bucket stays) | floor (VCT #B9B4A6, linear .46) / chair 10x, / cart 14x: >= 2 : 1 under any pool, at any trip time |
| **F11** floor before the goal dark (z 25.5 .122 x armed, r8 .271; 16.5 deg off the head's 13 deg half cone) | goal head **outer 26 -> 44 deg**, inner 22 deg kept (the core on the leaf unchanged). Cookie `Cookie_SealedBeamCore_S` radial profile read from the PNG (.98 centre, .77 plateau to r .7, .52 at .8, .24 at .9): at 44 deg the lite sits at r .29 (.80, was .77 at r .50), z 25.5 at r .73 (.70) | z 25.5: spot .34 x cookie .70 -> ~.24 x armed (r8 .271, -11 %, PASS); door level unchanged (call 4) |
| **F12** near pool z 6 .220 vs r8 .169 (+30 %) | `RunUnitDef.trimPlus / trimMinus` (gain offsets): unit 0 +Z and unit 1 -Z heads x 0.77 | .220 -> ~.169 |
| **F10** wrap lens clipped (emission 2.8) | `wrapLensEmission` 2.8 -> **1.1** (the lamp study's rule 1, 0.8-1.4) | the prism detail reads; bloom only just over its 1.0 threshold (also lowers the armed s1a / c1 p50, part of F12's armed +22-30 %) |
| **F6** LOD pops 2.39-5.01 % | Sidecar `lodDistances` (and the 5 Blender modules, the source): CableTray d01 3 -> 4.5, ChairDolly8 d12 12 -> 20, HandTruck d01 4 -> 5.8, ExtinguisherCabinet d01 2.5 -> 3.5, EmergencyTwinHead d01 3 -> 3.6. The `.fbx.meta` `lODScreenPercentages` set to the same heights so main re-imports the FBX (a sidecar change alone never re-imports). Clone import confirms the LOD groups: 0.4266/0.2133/0.064, 0.1474/0.0295, 0.1308/0.0632, 0.1115/0.0488/0.013, 0.0614/0.0221 | pop x (d / d')^2: 3.63 -> 1.61, 5.01 -> 1.80, 3.53 -> 1.68, 3.28 -> 1.67, 2.39 -> 1.66 % |
| F9 (not asked, made worse by F7) tray shadow band | the overhead services (main, heads, hangers, tray, struts, rods, EMT, straps, boxes) are built under `run b overhead services` and cast **no** shadow (r8 had none; it also cuts shadow casters, F5) | no band |

### C.2 Step 10: isolation from map lamps (the new gate) — design

- **Why a new gate.** `_FR_OWN_LIGHTS` skips lights over 6 m; the rig's reach 10-14 m and the map's 10-12 m, so range cannot tell them apart. URP light layers need the URP asset (shared) and a variant switch for the whole game. Instead: a **positional portal gate** inside the existing `_FR_OWN_LIGHTS` path (visual-owned `FrontRoomsSurface.shader`, main base bc5a0d08).
- **Shader.** `FROwnLight(lightIndex, positionWS)`: when the renderer's block has `_FR_RunGateB.w = 1`, the room is a box (the walls' centre lines, z 0 entrance .. 27 goal, floor .. ceiling). A light reaches a point when both are on the same side, or when the segment between them first leaves the box through the arch (z 0, |x| < .7, y < 2.2) or the goal opening (z 27: the F5 lite while the leaf is shut, the whole door when open or broken). Directional lights always pass. Every other renderer (the stairwell's Stair_*) never sets the block value, reads 0 and keeps the range test exactly as before. Single return, no early-out warning.
- **Materials.** `FrontRoomsRunGate` (in `FrontRoomsRunRig.cs`): every renderer the rig lights (rig + dress, never a host's) takes a runtime **twin** of its material with `_FR_OWN_LIGHTS` on: the stairwell's keyword, whose variants already ship (Stair_Block `{_FR_OWN_LIGHTS}`, Stair_RescueFace `{_EMISSION, _FR_OWN_LIGHTS}`), so no new keyword or variant. Not twinned: other shaders (glass, the URP Lit stencil, beams, halos), emissive materials (the lenses; the ALARM ZONE LED is animated through its own shared materials), and `_FR_PRINT / _FR_LAMP_FIELD / _FR_DAMAGE / _FR_SHEEN` materials. One twin per source, kept for the session. The rig writes the box (`_FR_RunGateA..E`) in every block it already writes for the SH; `SetGoalOpen` rewrites them once on a change.
- **Binder.** The goal lite's x from the map's hinge (`DoorBetween(...).hinge`, the leaf kit's `lite_centre` .34 m and `liteRect`); each LateUpdate `SetGoalOpen(door == null || broken || angle > 2 deg)` before `Tick`.
- **Physical result.** Level 0 / Office lamps light the tripped corridor only through the arch (a trapezoid of the host's light on the first metres: the hand-over) and through the shut goal door's lite (a small lit patch before the door). C42's window light, outside and aimed away, can never light the corridor through the wall.
- **Shader compile.** One Unity batch job on `C` (no Play, no render; clone-only harness `W/tools/feature/run_b66c/FrontRoomsRunB66ShaderCompile.cs`, moved out of the clone to `W/_partial/runb66_stageC_harness_20261031/`): 0 import errors; ForwardLit fragment compiled for 8 keyword sets x {macOS Metal, iOS Metal, WebGL GLES3x}: **24 OK, 0 fail, 0 warnings** (`W/run_b66/stageC/shader_compile2.txt`). GLES3x returned no bytecode (likely no WebGL module): WebGL not proven. The default path's variants compile unchanged.
- **No map change, no contract.**

### C.3 C39: Office hosts (and Level 0) — the hand-over and the wall-leak design

| Leak | Fix (same for both host types) |
|---|---|
| Host / neighbour lamps into the tripped corridor | the gate (C.2): through the openings only |
| B's lights into the neighbours (F7: 143 levels on the Office wall) | **every wrap shadowed** (`ownLampShadows` 0..8; WebGL keeps its one). Heads were all shadowed already. Armed now 9 shadowed spots, tripped 8. Main's 11:57 Quality change gives desktop HIGH an 8192 additional-light atlas (64 lamps at 1024), handheld keeps 4096 (16): both fit |
| The Run_B grade into the neighbours (F7: the 1 m blend graded 1 m of each neighbour) | main box between the long walls' centre lines, 0.15 m inside the end walls', **blend 0.3** (a camera in a side neighbour stands >= 1.88 m off the axis: weight 0; one in the corridor <= 1.12 m: weight 1) |
| The Office grade into the corridor (the corridor's cells are Office-themed, so the Office grade box covers it) | Run_B now overrides everything the Office grade sets: SMH, film grain, chromatic aberration added **with the global look's values** (shadows .97/1/.94, highlights 1/.99/.93; grain type 4, .22, .75; CA .06), so a Level 0 host is unchanged and an Office host looks the same |
| **The hand-over** (both directions) | two mouth volumes (`Run B / post mouth arch` / `... goal`): the opening's width, -0.08 .. +0.30 deep at the arch and 26.70 .. 27.08 at the goal, blend 1 m, priority 2: the grade fades over 1 m on the host side of the arch and the far side of the goal door, as the 10-22 design had it, and nowhere else. Light through the arch: the host's lamps reach in through the portal; the rig's shadowed wraps / heads reach out only through the opening |

Frames are the combined round's (Red: no frames now).

### C.4 Step 9: the handheld tier

- `FrontRoomsRunRig.HandheldTier` = `ForceHandheldForTools || FrontRoomsHandheld.Active`, false in the WebGL tier. Read once at Build; **one guarded call** `ApplyHandheldTier()` after the desktop build:
  - heads: one Light per pool (unit u's +Z head at `handheldPoolGain` 1.8; the -Z partner's Light goes, its lens still follows the state); the threshold head (unit 0 -Z) and the goal head as built: **8 -> 5**, hard shadows, no beam cones;
  - wraps: every 2nd (0, 2, 4, 6, 8, the stutter tube is 4) keeps its Light at 9 / 5 the intensity (`wrapScale`), hard shadows; every lens and halo stays: **9 -> 5**;
  - the goal window light (C42) stays (phones have the lite).
  - tripped 6 lights (5 shadowed), armed 6 (5 shadowed).
- **Desktop byte-identical, proof** (`W/run_b66/stageC/handheld_proof.txt`): every handheld change is inside `ApplyHandheldTier`; Build calls it only when the tier is on; `wrapScale` stays 1f on desktop and `x * 1f == x` exactly, so the two wrap expressions equal the old ones bit for bit. `FrontRoomsHandheld.Active` compiles to `return false` in desktop players. W2 (combined round) also builds the desktop rig before and after a forced handheld build and compares a full light signature.

### C.5 Files (23 paths, all MODIFIED against main now; list + base sha1: `W/run_b66/stageC/stageC_paths.tsv`)

- Scripts: `Rendering/FrontRoomsRunRig.cs` (F1, F3, F10, F11, F12, gate, post, handheld, `FrontRoomsRunGate`), `Rendering/FrontRoomsRunDress.cs` (F8 outside items, F9 overhead), `Rendering/FrontRoomsRunRigBinder.cs` (goal lite x, `SetGoalOpen`), `Editor/Rendering/FrontRoomsRunRigTests.cs`.
- Shader (shared, visual-owned): `Resources/Rendering/FrontRoomsSurface.shader` (main bc5a0d08).
- Assets: `FrontRoomsPost_Run_B.asset`, `RunB_ChairGrey.mat`, `RunB_CartPlastic.mat`, 5 kit `.json` + 5 `.fbx.meta`.
- `Tools/Blender/frontrooms_kit/assets/run_{cable_tray,chair_dolly,hand_truck,extinguisher_cabinet,twinhead}.py` (lod_meta + docstring).
- No new path, no new GUID. All LF; the .cs ASCII, no tuples.

### C.6 Compile gate

- Roslyn on `C` after the sync (`W/run_b66/stageC/roslyn_{mac,ios}.txt`): **macOS 0 errors, iOS 0 errors** (Assembly-CSharp 199, Editor 99, FMOD clean); with `FRONTROOMS_WEBGL_PREVIEW` on the rig: 0 errors. No warning from a Run file.
- Shader: C.2 (24/24).

### C.7 What the next stage must know

1. Package: 23 MODIFIED paths, bases = main's bytes now (the core's, `stageC_paths.tsv`); no kept-meta change except the 5 `.fbx.meta` themselves. Take bases at package time; main moves.
2. On apply Unity re-imports the 5 FBX (meta change) and the shader; the stairwell's Stair_* variants recompile (same output: their block never sets the gate).
3. Combined round adds R13 (F1 cull), R14 (F3 by frame windows), R15 (gate twins + goal portal), R16 (grade volumes + host-neutral profile), R17 (F8, F9, F2, F6 incl. the imported LOD heights), W2 (handheld + desktop signature); R7 now armed 10 / 9 shadowed. Looks: the arch hand-over both ways in a Level 0 and an Office host, the wall-leak check from each side neighbour (armed and tripped), the goal's lit lite patch, the goal head's wider pool vs the Ward_DA armour (11_rnx_lite §3 risk), the dark obstacles in their pools, the reset ripple at 60 fps (T9), the handheld preview frame.
4. Still open: the WebGL tier's leaks (one shadowed wrap, nearest head only: its own track); the `_ADDITIONAL_LIGHTS_VERTEX` path is not gated; glass and emissive materials take map lamps; the zone reflection cube on the tripped VCT (stage A caveat); the Relay contrast (T6 with Hunter's T2); F5 cost (the overhead shadows off help; 6 more shadowed wraps cost); F10's cabinet finish and label.

---

## Fix landing · `run_b_fix.sh` (stage C packaged; READY, not applied) + the video for Red

Started 2026-10-31 12:17, account A, session local_5656cffd. No earlier run of this stage had left anything (no `run_b_fix.sh`, no `W/run_b66/fix`, no video dir). Work dir `W/run_b66/fix/`. No suite, smoke run or T-gate ran (Red 10-31, COMMON rule 25).

### F.1 Re-sync (12:18)

- `run_b.sh` **is applied in main** (LANDED 11:38; `W/apply/run_b_applied_manifest.txt` = `run_b_expected.txt`, 195 paths). So the fix package's bases are main's run_b files: 22 of the 23 bases are byte for byte the core payload; the 23rd is main's own `FrontRoomsSurface.shader` (bc5a0d08, the core did not touch it).
- Main last moved 12:08 (door_dust_throw); the 12:15 sync of stage C already had it. Delta now (`W/run_b66/fix/delta_resync_pre.tsv`): 0 main-only paths, 0 changed main files outside the 23 stage C paths; the rest is the clone-only tools (45 + `ProjectSettings.asset`'s test bundle id). Nothing to bring in. 0 dataless main files. `install_tools.py` re-run: 42 unchanged, silencer in, bundle id `…test.runb66`.
- Every stage C path still has its recorded main base and its stage C sha1 (`W/run_b66/fix/scripts/check_bases.py`).

### F.2 Package `W/apply/run_b_fix.sh` (v2; builder `W/run_b66/fix/build_fix_package.py`)

| Kind | Paths |
|---|---|
| MODIFIED | 23 (stage C's list `W/run_b66/stageC/stageC_paths.tsv`): 4 `.cs` (rig, dress, binder, tests), `FrontRoomsSurface.shader`, `FrontRoomsPost_Run_B.asset`, `RunB_ChairGrey.mat`, `RunB_CartPlastic.mat`, 5 kit `.json` + their 5 `.fbx.meta`, 5 Blender modules |
| NEW | 0 (no new GUID) |
| Kept | 13 `.meta` (the .cs / .json / .mat / .asset / .shader metas) |

- 25 GUIDs referenced outside the payload, all resolved (16 main assets, 9 URP / core package scripts: the volume components the Run_B profile now carries, SMH / film grain / CA among them).
- Files: `run_b_fix.sh`, `run_b_fix_{bases,keep,expected,external_guids}.txt`, `run_b_fix_payload/`, `run_b_fix_base/` (exact copies of the 23 bases), `run_b_fix.diff` (1,606 lines). All LF.
- Section 5 refuses if: the core is missing; the map stops declaring `RunRoomsOn`, `DoorBetween`, `RunEntrance`, `Door.hinge / broken / angle`; the kit library stops declaring `GetInfo`, `TryAnchor`, `HasLite`, `LiteRect`; `FrontRoomsHandheld.Active`, `FrontRoomsPostStack.EnsureZoneVolume`, `FrontRoomsSettings.ReduceFlashing` change; the shader no longer declares `_FR_OWN_LIGHTS`; `Kit_DoorLeaf_Ward_DA` / `Kit_DoorFrame_Steel_DA` are gone; anything outside the core's four files **drives** a rig (Bind, the dress, the gate, AddComponent); `runRooms` is on anywhere. It also checks that the payload still declares the events 声音设计's director listens to (CR-S3, landed 11:45: `FrontRoomsSoundDirector` subscribes to `RigBuilt` / `RigDropped` and the rig's `MainsDropped` / `TransferRelay` / `ResetStarted`), with the same shapes. Listening is allowed: it fires only once the binder builds a rig.
- Variant check (player builds): the twins need `{_FR_OWN_LIGHTS}` x `_FR_MESH_UV` (multi_compile, both kept). Main's Resources already hold materials with `{_FR_OWN_LIGHTS}` and `{_FR_MESH_UV, _FR_OWN_LIGHTS}` (10) and `{_EMISSION, _FR_MESH_UV, _FR_OWN_LIGHTS}` (1), so those variants ship; the twin skips `_EMISSION / _FR_PRINT / _FR_LAMP_FIELD / _FR_DAMAGE / _FR_SHEEN` materials. No stripped-variant fallback expected.

### F.3 Gate (compile + dry run + rehearsal only)

- **Dry run on main** (12:2x, main with run_b applied): `DRY RUN OK` (23 bases at their sha1, 13 kept metas unchanged, 23 payload files match, 5 metas keep main's GUID, 0 new GUIDs, 25 external GUIDs resolve, section 5 clean). Log `W/run_b66/fix/dryrun_main.txt`.
- **Rehearsal** on an APFS copy of main (`W/tools/_test/run_b_fix_1031/`, Assets + ProjectSettings + Packages + Tools): `--apply` → `MANIFEST OK … all 23 touched paths; 13 kept .meta files unchanged` (23 originals in its `backup/`); again → `ALREADY APPLIED`. A second copy with 2 payload files pre-written (`W/tools/_test/run_b_fix_1031_half/`) → `REFUSED: partially applied: 2 of 23`, nothing written. No applied manifest in `W/apply`.
- **Roslyn on main + payload** (the rehearsal tree after `--apply`, `--all`): **macOS 0 errors, iOS 0 errors** (Assembly-CSharp 198 files, Editor 82, FMOD 5 assemblies clean); 0 warnings from a Run file. Logs `W/run_b66/fix/roslyn_{mac,ios}.txt`.
- Shader: Roslyn cannot compile it. Stage C's Unity batch compile of this exact shader (C.2: 24/24 macOS / iOS Metal + GLES3x, 0 warnings) stands; the payload shader is that file (8f3587e0).

### F.4 Apply

`bash W/apply/run_b_fix.sh` (fresh dry run) → LANDINGS CLAIM → `bash W/apply/run_b_fix.sh --apply` → LANDED row. Never with `APPLY_REAL`. If main moves first: `W/tools/rebase_apply.sh run_b_fix` (shader text → GATE note: the Unity batch compile again in a clone), or rebuild with `build_fix_package.py` after re-syncing `C`.

### F.5 The combined test (once, after run_b + run_b_fix are in main; nothing of it ran now)

Run all of it in one 6.6 clone of main after the landings, in this order. Gates come from `10_plan.md` §9 and `10_runb_state.md` §11 C. Each kind runs once (rule 23).

| # | Gate | What | Pass |
|---|---|---|---|
| 1 | Compile | Unity batch import of main (the 5 FBX re-import with their new LOD heights; the Surface shader recompiles) + Roslyn macOS / iOS | 0 errors; `Stair_*` variants unchanged |
| 2 | **`FrontRoomsRunRigTests.RunBatch`** | R1–R17 + W1 + W2 (EditMode) | all PASS; R7 armed 10 / 9 shadowed, tripped 9; W2 desktop light signature identical before and after a forced handheld build |
| 3 | `FrontRoomsAlarmTellTests.RunBatch` | the tell (`50` §7) | all PASS |
| 4 | **Stairwell safe-zone trace** | exit run (`-autopilotExit`) + station on the production kits: C22 blackout on LOD0, the cabinet flush on the wall, the unit legend at 0.3 m, the twin head's new LOD d01 3.6 m; the safe-zone read (steady light, the Relay never enters) | no flicker in the stairwell, the blackout reaches every LOD, no gate effect (the stairwell never sets `_FR_RunGateB`) |
| 5 | **T5** | the proposal frames on the real module (`-autopilotRunRooms 2`), the r8 cameras re-shot on 6.6 / URP 17.6 | p50 / p95 ±15 % of r8; pools z 6 / 12 / 18 / 25.5 within ±20 % (F11 floor z 25.5 ≈ .24 × armed, F12 z 6 ≈ .169) |
| 6 | **T6 / T7 with `Relay_NightShift`** + **overhead clip** | the real body (top 2.848 m, lens 1.704 m) in the tripped corridor (Hunter's T2 is the same job: run once), 12 px ring; through the goal door; the body against the sprinkler main 2.62 / tray 2.70 / EMT 2.72–2.80 m and the detector at the arch | ≥ 4 : 1 protan included; T7: goal "brightest, or second after a lamp" (call 4), obstacles ≥ 2 : 1 (F2) also at 8 m, no pockets; overhead clipping reported, with a fix if it shows |
| 7 | **T9 reset photosafety** | 30 fps timelines (frame-mean Y + saturated-red area) of the trip, the **reset ripple** (F3), a stage-1 warning burst, the stutter and the run pass; Reduce Flashing off and on | ≤ 3 flashes in any 1 s (the design predicts ≤ 2); no red-field flash; Reduce Flashing 0 flashes, no black gap |
| 8 | **T10** | the run at 5.5 m/s from the trip line to the goal, vFOV 76, head bob | every obstacle ≥ 2 : 1 eight metres before it; pool rhythm ≤ 0.92 Hz; the lit F5 square visible from the trip line (F1, C42) |
| 9 | **T11** | frame cost at fixed poses on the real module, 300 frames, Ward-free base | GPU ≤ base + 1.5 ms; first second after the trip ≤ 1.5 × median; no "too many shadowed lights" (armed 9 shadowed spots; 8192 atlas on desktop HIGH) |
| 10 | **C39 frames** | one Level 0 host and one Office host: the arch hand-over both ways (grade over 1 m, host lamps reach in only through the arch), the **wall-leak check** from each side neighbour, armed and tripped (no Run_B grade, no B light through the wall), the goal's lit lite patch, the goal head's wider pool against the Ward_DA armour (11_rnx_lite §3) | no leak either way; hand-over reads as one step |
| 11 | Handheld preview + T12 WebGL | `ForceHandheldForTools` frame (5 + 5 lights, lenses and halos kept); the WebGL define build | the T6 / T9 rules hold on both |
| 12 | The probe caveat | a tripped floor's VCT against the zone reflection cube (stage A) | no bright-zone reflection; if it shows, the per-object probe fallback (A.4) |

The video below is a look for Red, not one of these gates.

### F.6 Main moved during the stage, and the gate was re-run (12:38)

- At 12:25 and 12:27 main took `transitions_mobile` (38 paths) and `transitions_mobile_custom`. None of the 23 fix paths was touched.
- The clone was synced again: 37 files copied (`W/run_b66/fix/resync_1240.tsv`) and `install_tools.py` re-run. The clone now equals main + the 23 paths + the clone-only tools again (`delta_post_resync.tsv`).
- **The final gate passed on main as it stands now:**
  - Dry run: `DRY RUN OK` at 12:38 (`W/run_b66/fix/dryrun_main_final.txt`).
  - Fresh rehearsal `W/tools/_test/run_b_fix_1031b/`: `MANIFEST OK`, then `ALREADY APPLIED`.
  - Roslyn on that tree: macOS and iOS, 0 errors (Assembly-CSharp 199, Editor 82; `roslyn_{mac,ios}_final.txt`).
  - 0 warnings from a Run file.
- The video below was rendered before this sync, so it does not have the transitions_mobile change. That change is the phone tier of the zone transitions and does not affect a Run! room.

### F.7 The video for Red: `run_b_20261031.mp4`

- **Files.** `W/run_b66/video/run_b_20261031.mp4`, also copied to `Documentation/research/room_visuals/run_b_production/video/` with its contact sheet `run_b_20261031_contact.jpg`.
- **Encoding.** H.264 CRF 23, yuv420p, `+faststart` (moov before mdat), 1920 × 1080, 30 fps. 856 frames, **28.5 s**, **10.1 MB**, one video stream, no audio stream (ffprobe).
- **Check.** The contact sheet was the only check: one 6-tile Shot-format sheet. The look was not measured.
- **Harness.** A clone-only harness, `FrontRoomsRunB66Video`. Its source is in `W/tools/feature/run_b66v/` and the copy that ran is moved to `W/_partial/runb66_video_harness_20261031/`, so it is no longer in the clone. One Unity batch job ran on `C` from 12:29 to 12:31 (`W/run_b66/video/run_video.sh`, log `W/run_b66/video/logs/`). The clone silencer was in, no player ran and nothing made a sound. FMOD logs "bank not found" because the clone has no banks.
- **Setup.** The real map ran with `RunRoomsForTools = 2`, seed 516574485. The Relay was held: `releaseDelaySeconds` 1e6, `deviceLoudness` 0. The planner was opened at once. Camera: the game camera, desktop High, Glass RT off.
- **Rooms.** The map planned run 3 off a Level 0 zone and run 1 off an Office zone. Both goal doors are the map's RN-F, never locked, dressed, with the lite at x −0.16 m. Each far room has a lamp: 0.97 and 0.99.
- **Captions and logo.** Same style as `door_break/video/build.py`, made by `W/run_b66/video/build.py`.

| Segment | Frames | What |
|---|---|---|
| WALK-IN · LEVEL 0 ARCH | 111 | slow walk (1.44 m/s) from 2.6 m outside the arch to z 1.0, armed |
| WALK-IN · OFFICE ARCH | 99 | the same off the Office host (C39) |
| THE TRIP | 103 | over the 1.5 m line; the map trips the room, the mains drop and the twin heads take over |
| RUN | 165 | 5.5 m/s from z 2.2 to z 23.6, then a hold on the lit F5 square |
| RESET | 138 | turn round at the goal end, then `ResetRunRooms`: the one-flick ripple and the re-strike sweep toward the camera (F3) |
| ALARM ZONE | 180 | dolly to the card outside the Office arch, at (−0.99, 1.52, −0.08) local, then up through the arch to the red-banded detector at z 2.71 |

**Seen on the contact sheet (by eye; not a gate):**
- The armed corridor is evenly lit with no EXIT sign (C41), and the fire gear is red.
- Tripped, the corridor is dark with the twin heads' pools.
- The F5 square reads as the brightest thing at the end of the run.
- The ALARM ZONE card is legible.
- To check in the combined round: a twin head throws a hard bright wedge high on the left wall in the RUN frame (beam or cookie edge), and the walk-in tiles are taken from inside the arch, so the hand-over is seen only in the first second of each walk-in.

### F.8 What the next stage must know

1. `run_b_fix.sh` is **READY, not applied**. To land it: fresh dry run, LANDINGS CLAIM, `--apply`, LANDED row. If main moves first, use `rebase_apply.sh run_b_fix`; a shader merge still needs a Unity batch compile in a clone.
2. On apply, Unity re-imports the 5 FBX because their meta changed, and recompiles `FrontRoomsSurface.shader`.
3. Everything stays inert until 关卡设计 turns `runRooms` on. Run! is playable 11-04 at the earliest (系统设计).
4. Combined test: F.5, run once after the landings.
5. Waiting on others:
   - **关卡设计:** turning `runRooms` on in the level profile; CR-1..CR-6 + M8 are in main.
   - **平面视觉:** the six STAND-IN plate / stencil faces (`62_for_graphic.md`; the unit legend is final); the ALARM ZONE card wording, signed off by 叙事设计 (`CardFaceIsStandIn` stays true).
   - **声音设计:** `61_for_sound.md` (CR-S1/S2, the wraparound hum CR-S4, and the new re-strike timings in its 12:3x update).
6. Not built: the WebGL tier's leaks (its own track), the ungated `_ADDITIONAL_LIGHTS_VERTEX` path, glass and emissive materials taking map lamps (C.7 4).
