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

---

## Run! rethink · card 1: the rig in cells (2026-11-04)

游戏视觉, account G, workflow `visual-run-rethink` (parent session local_1fc9f09c), stage "Rig in cells". The card is 71 §9, 游戏视觉 1 (`research/level_space/71_run_room.md`, 关卡设计 / level_space's page, read only). Red's answer to 71 §8, 2026-11-03 23:18: "全部按推荐" (71 §0.1). It rests on two decisions:
- **decision 3:** lengths by depth in whole cells, 7 for the run's first Run!, then 9, 12 from T4, 14 at T5 where it fits; the cap is 14 cells (42 m);
- **decision 7:** the RESET segment, the white re-strike from the entrance, as built.

Card 3 (the red looks) waits for Red's yes on 灯具适配 1's still, so nothing red was built. Rule 5b holds: frames, the receding end and the Run door enter the game only after Red has walked SP7 in scene 4. What was not done: no render, nothing on main, one EditMode batch in `C`, nothing heard.

### R.1 What changed (3 files; `W/apply/run_rig_cells.sh`, landed 2026-11-04 06:40)

| File (base → payload) | Change |
|---|---|
| `Scripts/Rendering/FrontRoomsRunRig.cs` 9e964b3b → 48ae4e5e | `RunRigLayout.Corridor(int cells, archWidth, doorWidth, frames)` builds 7 to 14 cells by the rules in R.2. Any other count is refused with a warning and built at 9. `Corridor1x9(...)` is now `Corridor(9, ...)`. New: `CellLength`, `TodayCells` / `MinCells` / `MaxCells`, `Cells`, `ValidCells`, and three fields with empty defaults: `probeBake`, `probeCubes` (probes by segment) and `compartmentFrames` (only when asked; nothing builds them yet). `BuildProbes` loads each box's reused cube. `ResetDuration` is public |
| `Scripts/Rendering/FrontRoomsRunRigBinder.cs` 1136e0ae → 969fef2e | `CellsOf(room)` reads `room.Cells.Length`, 9 without one. The goal cell is the last cell, not `Cells[8]`. The animated-reset reach is 30 m + half the length past 27 m. A warning fires if the map's goal door is not at 3 m × the cells. The layout keeps its bake name before the binder renames it |
| `Editor/Rendering/FrontRoomsRunRigTests.cs` a95799bf → 69cac3c5 | R18 (today unchanged) and R19 (7 to 14 cells). `TodayCorridor1x9` is a frozen verbatim copy of the landed preset |

`FrontRoomsRunDress.cs` is unchanged: it is a function of the layout.

### R.2 The rules (`RunRigLayout.Corridor`; every value from the cell count, never by scaling)

- **Strips.** A strip every cell, at z 1.8 + 3k, all shadowed (F7). The stutter tube is in cell 5 at every length (SP7's z 13.8).
- **Units, heads every two cells.**
  - Units sit on the odd cell lines from the entrance, z 3, 9, 15 and so on, on alternate walls with the entry unit on the right.
  - The goal unit is always two cells before the goal wall (z L − 6), so the goal head lights the door from 6 m, as call 4 tuned it.
  - At an even count the last interval is three cells (9 m). A one-cell interval would double T10's 0.92 Hz pool rhythm.
  - T-A units at both ends, T-B remote heads between, F12's trim on the near pool.
- **Obstacles.** One in every pool but the goal's: the chair dolly, the janitor cart and the hand truck in turn, on alternate walls, mirrored on the other wall.
- **Side doors.** ELECTRICAL ROOM in cell 1 on the right (z 5); NOT AN EXIT two cells before the goal on the right (z L − 4); STAFF ONLY in cell 5 on the left (z 17) when it stands a cell or more short of NOT AN EXIT, so not at 7 cells.
- **Fire gear.** The pull station and the bell stay at the entrance. Extinguisher cabinets go on the left at z 10.2, plus z 31.2 from 12 cells, by NFPA 10's 75 ft (22.86 m) travel distance.
- **Probes by segment (71 §5.8).** One 9.6 m box per three cells: the entrance box at z 4.5, the middle ones every 9 m after it, the goal box at L − 4.54. Each reuses the 9-cell bake's entrance, middle or goal cube. No cube ships today, so `ProbeCount` stays 0.
- **The rest of the room.** The post box, floor, services and runs cover 0 to L; the goal assembly (goal point, face and door gap, goal unit) sits at L.
- **The reset.** The same re-strike from the entrance, 0.15 s per cell, over the current length: 0.45 + n × 0.15 + 0.40 + 0.2 s.

| Cells | Units (z, wall) | Obstacles | Doors | Cabinets | Probes | Lights armed / tripped (shadowed) | Reset | Tris LOD0 / LOD1 / LOD2 | Renderer slots |
|---|---|---|---|---|---|---|---|---|---|
| 7 | 3R 9L 15R | 2 | 2 | 1 | 3 | 8 (7) / 7 (6) | 2.10 s | 226,597 / 85,282 / 20,686 | 445 |
| 8 | 3R 9L 18R | 2 | 3 | 1 | 3 | 9 (8) / 7 (6) | 2.25 s | 264,367 / 99,976 / 24,664 | 504 |
| **9 (today)** | 3R 9L 15R 21L | 3 | 3 | 1 | 3 | 10 (9) / 9 (8) | 2.40 s | 301,689 / 114,160 / 28,160 | 582 |
| 10 | 3R 9L 15R 24L | 3 | 3 | 1 | 4 | 11 (10) / 9 (8) | 2.55 s | 319,013 / 120,784 / 29,752 | 624 |
| 11 | 3R 9L 15R 21L 27R | 4 | 3 | 1 | 4 | 12 (11) / 11 (10) | 2.70 s | 364,175 / 137,440 / 33,220 | 701 |
| 12 | 3R 9L 15R 21L 30R | 4 | 3 | 2 | 4 | 13 (12) / 11 (10) | 2.85 s | 384,099 / 145,064 / 35,054 | 747 |
| 13 | 3R 9L 15R 21L 27R 33L | 5 | 3 | 2 | 5 | 14 (13) / 13 (12) | 3.00 s | 424,252 / 159,798 / 38,534 | 827 |
| 14 | 3R 9L 15R 21L 27R 36L | 5 | 3 | 2 | 5 | 15 (14) / 13 (12) | 3.15 s | 441,388 / 166,322 / 40,102 | 867 |

How the cost columns were measured: R19 in Unity on the desktop tier, the whole room (dress + rig) as built in the clone. A renderer in a LOD group counts at its level; one outside counts at every level. "Renderer slots" is renderers × materials before batching. Frame time was not measured. The narrowest free lane beside an obstacle is 1.77 m at every length (rule: at least 1.4 m). The worst reset window is 1 flash/s at every length (bar 3; T9 still measures it on screen in the combined test).

### R.3 Today unchanged: the proof

- **Offline, no Unity.** `W/run_rethink/tmp/rig_cells/eqcheck/run.sh` compiles a small program with Unity 6000.6.3f1's own csc and dotnet 8.0.21. The program executes two compiled `Assembly-CSharp.dll`s by reflection: main as it is (from `roslyn_check`, so today's `Corridor1x9`) and main with our three files as overrides. Result: 2388 leaf values in 6 builds (3 width pairs × `Corridor(9)` and `Corridor1x9`), 0 differences, floats bit for bit. The three new fields hold their class defaults at 9 cells. Output: `eqcheck_out.txt`.
- **In Unity** (`C`, one batch, `FrontRoomsRunRigTests.RunBatch`): 21 passed, 0 failed.
  - R18: 2793 values in 7 builds, 0 differences against the frozen copy. The dress and rig built from each are identical piece for piece: 1343 pieces with the look-dev shell, 1354 with the map skin. Their lights match armed and tripped.
  - R19: the rules above at 7 to 14 cells, built and measured; 6 and 15 cells refused at 9 with the warning.
  - Log: `W/run_rethink/logs/rigcells_tests/` (tests.json d8927180).

### R.4 Gate

- Roslyn macOS and iOS on main with the three files as overrides (zsh array, read back from the `.rsp`): 0 errors. Warnings are 43 / 38, the same as main's, none in our files.
- The same with the contract applied: 0 errors, 7 overrides.
- Package: DRY RUN OK on main. A rehearsal `--apply` on a scratch copy gave MANIFEST OK, and a second run reported ALREADY APPLIED.
- Logs: `W/run_rethink/logs/gate_rig_cells/`.

### R.5 Contract for 关卡设计 (`W/apply/run_rethink_contract/`)

The README says what the rig reads, and `CONTRACT_map.diff` is an inert diff of 10 hunks in 4 of their files (`patch --dry-run` OK). Inert means `Cells.Length` is 9 in every record today, so no value changes.
- **Now:** nothing to do. The rig reads `room.Cells.Length`.
- **When the map sends another length:**
  - `Cells` holds exactly the room's cells, 7 to 14;
  - `LengthMetres`, `RunEntrance` and the goal cell follow the record;
  - the diff adds `LengthCells` and `GoalCell` to the record and replaces the nine `Cells[FrontRoomsRunRooms.Length - 1]` uses.
- **Later (card 2, after Red walks SP7):** the receding seats on the record.

### R.6 What the next stage must know

1. **Frames against units.** Compartment frames are data only (`Corridor(n, frames: true)`, z 9, 18, 27, 36 inside the room). The frame lines at z 9 and z 27 fall on the unit lines at z 9 (the remote head, left) and z 27 (unit 4 at 11 or more cells). FD1 at 9 m is the recede's trigger (decision 4), so card 2 must move that head onto the frame's jamb or beside it when it builds the frame kit.
2. **The recede (card 2)** builds on `Corridor(L_max)`: the hidden cells' strips, units and probes are already laid out by the same rules. The probe boxes "move with their segment" there: the goal segment's box rides the head.
3. **Light budget at long lengths.** At 14 cells the room carries 14 shadowed strips armed (today 9) and 12 shadowed heads tripped (today 8). Every strip stays shadowed (F7: no light through the long walls). The shadow atlas is 设置菜单's call; check it in the combined test.
4. **Two choices Red can see in SP7:** the 9 m last interval at even counts, and a 7-cell room without STAFF ONLY. Either is a small change in `CorridorUnits` or `Corridor`.
5. Everything stays inert until 关卡设计 sends other lengths, and Run! itself until `runRooms` is on.

## Run! rethink · card 2: the recede, head first (2026-11-04)

游戏视觉, account G, workflow `visual-run-rethink` (parent session local_1fc9f09c), stage "Recede head". The card is 71 §9, 游戏视觉 2 (`research/level_space/71_run_room.md`, level_space's page, read only), head first. Red's answer to 71 §8, 2026-11-03 23:18: "全部按推荐" (71 §0.1). It rests on:
- **decision 4:** the stretch, deep floors only (9 → 12 from T4 on B2 or deeper, 9 → 14 at T5 on B3 or deeper), stepped (a two-cell flinch, one-cell steps, a heavy final seat, the 6 m/s keep-away glide), tripping at the 9 m frame (FD1);
- **decision 5:** the T1 后退 head on every tier and platform, carrying T2 重组 on desktop and iPad (T3 not built now; T4 held for a pattern capture).

Not built by this stage: T2's block pieces and bakes (the next stage), card 3's red looks, the Run door (card 4). The red tube in the R-B strip (灯具适配, landed 11-04 06:33 on Red's "加，用更亮档") is used as landed: the uncovered strips strike once in the room's state, so tripped their red tube rises. Rule 5b holds: nothing in today's game builds a receding room; it enters with Red's walk of SP7 in scene 4. Not done: nothing on main, no download, nothing heard; Unity only in `W/proj_runb66` (one batch at a time, edit mode, no Play mode).

### H.1 What changed (`W/apply/run_recede_head.sh`, DRY RUN OK, not applied)

| Path (base → payload) | Change |
|---|---|
| `Scripts/Rendering/FrontRoomsRunRecede.cs` (new) | The recede: `RunRecedeSchedule` (one timeline from one trigger time, pure arithmetic), `FrontRoomsRunRecede` (builds the end and its hardware, the gaze gate, the keep-away glide, the events, the 8-byte record and `Restore`), `RunRecedeCue` (the event list for 声音设计) |
| `Scripts/Rendering/FrontRoomsRunRig.cs` 97727e79 → f56090ab | `RunRigLayout.Receding(9, 12 | 14)`; `RunUnitDef.carried` (the goal unit on the end); `recedeLine`, `carriedGoalGain`, `carriedFloorAhead`; the rig's `GoalMount`, `SetGoalLine` (the light gate's far end per step), `RevealTo` / `StrikeRevealed` (hidden strips, a one-way strike at the seat), `GoalBeyondHidden` (C42 at full while the door looks onto the hidden length). In a framed corridor (`Corridor(n, frames: true)`) the frames are FD1 z 9 and FD2 z 18 (71 §10) and every unit but the goal unit stands half a cell on: card 1's flag (the unit on line 9 stood in FD1's stub) |
| `Scripts/Rendering/FrontRoomsRunDress.cs` 20c6da54 → 57e08a4f | `CompartmentFrames`: `Kit_RunCompartmentFrame`, two `Kit_DoorLeaf_SteelRun_DA` leaves at 115° on `Kit_RunMagnetHolder`s; only for a layout that lists frames |
| `Editor/Rendering/FrontRoomsRunRigTests.cs` 69cac3c5 → ccbf4803 | R20 (the recede); R19's frame rule |
| `Resources/Props/Models/Kit_RunRecede_{Head, HeadGear, SweepSeated, SweepMoving, Track_3m, TrackStop, FloorSlot_3m}`, `Kit_RunMagnetHolder`, `Kit_RunCompartmentFrame` (.fbx + .json + metas, new) | the kits, hand-built LOD0/1/2 |
| `Resources/Surfaces/RunB_{BlockLower, BlockUpper, Soffit, Floor, FloorLane, FloorAccent, FloorWax}_Ghost.mat` (new) | the ghost bands' finishes |
| `Tools/Blender/frontrooms_kit/assets/_run_recede_common.py`, `_run_recede_sweep.py`, `run_recede_*.py`, `run_magnet_holder.py`, `run_compartment_frame.py`; `Tools/lookdev/gen_run_ghost_surfaces.py` (new) | the generators |

### H.2 The schedule (71 §4.5, `motion.txt` R5)

- R = 3k m in whole cells: k = 1 the final step alone; k = 2 the flinch alone, seated heavily; k ≥ 3 a two-cell flinch, k − 3 one-cell steps, a one-cell final step.
- T2 timings (the default on desktop, iPhone and iPad, so the three keep one timing; WebGL runs T1's): tells 0.25 (first, final) / 0.15; travels 1.1 (flinch) / 0.8 / 1.0 (final); seats 0.2 / 0.35 (final); dwell 0.2. R 9 seats at 1.55 and 3.35 s; R 15 at 1.55, 2.90, 4.25 and 6.05 s. T1: R 15 at 1.15, 2.15, 3.15 and 4.85 s.
- Travel: accelerate 0.20 s, cruise, decelerate 0.15 s (the flinch peaks at 6.49 m/s). The tell is the magnets' two clacks 30 ms apart and a 15 mm jolt toward the player; a seat overshoots 30 mm (35 mm on the final seat) and settles.
- The gaze gate: the first step waits until the camera faces the goal within 35°, or the player passes two thirds of the first length (18 m).
- The keep-away: between steps (and at the gate), a player within 12 m of the end turns the rest into one glide to the final line, 6 m/s peak after a 0.1 s tell, then the heavy seat. A full-bar sprinter in a 9 → 12 room never triggers it: 15.5 m from the end at the flinch's seat, 8.6 m at the final seat (71 §4.5).
- The schedule reads the player's position and gaze only, never the Relay (rule 11). Each step: the magnets let go, the sweeps relax, the cells it uncovers come on behind the end (props here, strips in the rig), the light gate moves to the step's line, the line it leaves shows its ghost band; at the seat the sweeps press and the uncovered strips rise once.
- Events for 声音设计 (C# only, nothing played, rule 24): MagnetRelease ×2, Unlatch, TrolleyRoll, SweepDrag, Whump, Seat, FinalSeat, Grit, CellUncovered, StripStrike.

### H.3 The end (71 §5.1), at hero spec

- **Why its shape:** it travels through the reserved length past everything mounted there, so its sides stand 85 mm off the walls (the bumper rail, the EMT and its boxes and the holders pass), and over 2.53 m the corridor's services pass through it: brush-lined slots in its header for the strip lamp, the sprinkler main (and its coupling at a cell line), the ladder tray, and notches for the two tracks. Load path: trolleys in the tracks (x ±1.25) → corner hangers → a girder inside the wall's top.
- **The parts:** the block-faced wall (the corridor's two-tone finish on both faces, world-projected across the axis, so moving it does not swim), brown hollow-metal edge channels, sill, caps and slot linings (the door frame's paint); two four-wheel trolleys with 16 mm hangers, nuts and washers; nylon strip brushes trimmed to the services that sit in the slots at a seat; rubber sweeps in two poses (seated: bowed on the walls and the floor; moving: swung back 40°, as a period automatic door bottom), swapped on the beat; two bottom guides in 20 mm floor slots (x ±0.95); armature brackets for the hold-open magnets (wall holders at every line it can seat on, 70 mm proud at 1.95 m); a 1.6 m braced arm whose mount plate carries the T-A twin-head (灯具适配's unit as landed: the goal head floods the leaf at 0.9 × a flood head, the other the floor 4 m before the door; on its battery, no feed); C42 in a louvred box behind the wall over the door.
- **The door:** the map's RN-F as dressed today (Steel DA frame, Ward DA leaf with its F5 lite, the lever set) hangs on `DoorMount` and moves with the end. 71 §5.1 names a panic bar: that is the retired RN-X's (A54 a); RN-F keeps the lever (Q-RN); a crossbar would be a door-set change (`Kit_ExitDevice_Crossbar` exists).
- **The honest tell:** the tracks run in both soffit corners from the entrance wall to the deepest line. 71 §5.1 starts them at FD1, but FD1's header (2.1 m) hides the soffit beyond 9 m from the arch (still `s11`), so they start at the entrance to be read before commit. In the maker's almond enamel: a galvanized track (metallic 0.75) read black on the dark soffit, the corridor having no reflection probes.
- **Ghost bands:** a 0.2 m band of the corridor's own finish in its `_Ghost` twin (macro dirt × 0.25, grime × 0.3, the base 4 % lighter; the wax unworn: smoothness + 0.12) on both walls, the soffit and the floor at every line it leaves.
- **Compartment frames** FD1 and FD2: a 0.16 m block cross wall with a 2.0 × 2.1 m steel pair frame; the leaves swung 115° until their edges meet the side walls, held there on magnets (叙事设计 P1: the frames behind the runner stay open at the trip).

### H.4 The receding layout (`RunRigLayout.Receding(9, 12 | 14)`)

The reserved length laid out by card 1's rules with the variant's: FD1 and FD2 only; the fixed length's units half a cell past their odd lines (4.5 R, 10.5 L, 16.5 R, 22.5 L; T-A at the entry, T-B after) and the carried goal unit; no side doors (71 §4.8 item 9); cabinets at 10.2 and 24.2 (the second's 75 ft reaches the deepest end); obstacles only in the fixed length's pools; the goal point on the end's first line (it moves with the end). Nothing is mounted on the walls the end travels past but the finishes, the rail and the magnets, so in the tripped light that length is lit by the carried unit alone (the honest flaw on the sheet).

### H.5 Today unchanged, and the recede: the proof (Unity in `C`, `FrontRoomsRunRigTests.RunBatch`, 22 passed, 0 failed)

- R18: `Corridor(9)` = today's preset, 2898 values in 7 builds, 0 differences; 1352 / 1363 pieces identical (look-dev shell / map skin).
- R20: the seats above; the flinch's peak 6.49 m/s, the glide's 6.00; `Receding(9, 12 | 14)` by rule, 9 → 13 refused; a built 9 → 12 room: 3 hidden strips and 3 cells off, the walker's seats at 33 and 36 m (final), 24 events in order, the strips struck at their seats, the record 8 bytes, `Restore` seated with no event; a walker 5 m from the end at the gate glides the whole way (seated 2.15 s); the gaze gate shut facing away, open at 30°; a plain 9-cell room has no goal mount, no hidden strip, its gate on z 27.

### H.6 Images and film

- Sheet `images/run_recede_head.jpg` (+ `_fit.txt`, ALL LABELS FIT): TODAY first; arch, the tell, the lite, 9 m (the trip: flinch, seated), the seat, phone tier, hero 0.35 m (door, lite, magnet, trolley, sweep seated and moving), COST. Hero rows in the armed light after the reset with a camera fill (look-dev, the same for TODAY and HEAD).
- Film `video/run_recede_head.mp4` (10 s, 30 fps, H.264, no audio stream) + `images/run_recede_head_contact.jpg`: walk in at 3.2 m/s, the trip at FD1, sprint at 5.5 m/s; the end flinches two cells and seats at 36 m with the runner 8.6 m short of it; the door, lit by the carried unit, the lite onto the release.

### H.7 Cost (desktop, Editor, measured in Unity)

| Kit (new) | Tris LOD0 / LOD1 / LOD2 | Slots | Switch (m) |
|---|---|---|---|
| `Kit_RunRecede_Head` (the wall, edges, arm) | 3,796 / 1,876 / 360 | 3 | 10 / 28 / never |
| `Kit_RunRecede_HeadGear` (trolleys, brushes, C42 box, guides) | 5,744 / 1,420 / 284 | 2 | 10 / 28 / never |
| `Kit_RunRecede_SweepSeated`, `_SweepMoving` (one shows) | 768 / 224 / 96 each | 1 | 10 / 28 / never |
| `Kit_RunRecede_Track_3m` | 532 / 84 / 12 | 1 | 6 / 18 / 46 |
| `Kit_RunRecede_TrackStop` | 268 / 116 / 12 | 2 | 4 / 12 / 30 |
| `Kit_RunRecede_FloorSlot_3m` | 340 / 36 / 12 | 2 | 4 / 12 / 30 |
| `Kit_RunMagnetHolder` | 896 / 176 / 24 | 3 | 3 / 9 / 25 |
| `Kit_RunCompartmentFrame` | 2,292 / 564 / 96 | 4 | 6 / 20 / never |

- **The end's own parts:** 10,308 tris LOD0 (3,520 from 10 m). 71 §5.1 estimated 8-10k with the door and the unit; at hero spec the end alone is 10.3k. With the map's RN-F door kits as today: 36 renderers, 66 renderer slots (every LOD level counted, before batching), 32,788 tris at LOD0. The carried T-A unit: 15 renderers, 40 slots, 10,128 tris (it was the goal unit on the wall today).
- **The room:** today's 9 cells 1,017 renderers / 1,480 slots / 335,529 tris (LOD0, all); the 9 → 12 receding room 1,464 / 2,083 / 415,235 (the reserved length's props are built and off until each seat).
- **Memory:** 3.40 MiB of new meshes; no new texture (the kits take the corridor's own materials; the seven `_Ghost` twins reuse their textures).
- **CPU (Editor, batch):** the rig and the recede tick 0.19 ms on average, 3.73 ms at worst (the trip's frame); the light gate's block rewrite 1.2 ms, once a step (at most four per recede). GPU frame time: not measured.
- **Lights:** the seated room tripped: 10 on, 10 shadowed (23 built). Armed, the 12-cell room's 13 shadowed spots made URP reduce the shadow atlas's resolution (the harness's log); 设置菜单's call, in the combined test.

### H.8 Gate

- Roslyn macOS and iOS on main with the four code files of the payload as overrides (zsh array, read back from the `.rsp`): 0 errors; warnings 43 / 38, main's, none in our files (`W/run_rethink/logs/gate_recede_head/`).
- Unity in `C` (the clone resynced from main at 07:2x over the red tube and the structures): import and compile 0 `error CS`; the kits import with their LOD groups; EditMode 22 of 22 (R1-R20, W1, W2).
- Package: `W/apply/run_recede_head.sh`, 67 paths (64 new; 3 replaced, their metas kept): DRY RUN OK on main; a rehearsal `--apply` on a scratch copy gave MANIFEST OK, then ALREADY APPLIED. Not applied.

### H.9 What the next stage must know

1. **T2's body** (the next stage): about 90 block pieces with object-space UVs on this head (the end wall is `Kit_RunRecede_Head`'s skins; its girder and edges stay), two Blender rigid bakes (one cell, the flinch) with contact lists, phones half-course strips, WebGL T1 alone; grit by `54`'s GPU path. The schedule's T2 timings already run: T2 adds visuals only.
2. **The map's side** is the contract's card 2 section (`W/apply/run_rethink_contract/README.md`): the record, the trip at FD1, colliders and graph per seat, `DoorMount`, the release swap. The binder's ~15 lines wait for the record.
3. **Lighting the travelled length:** no wall unit can stand where the end travels; if the combined test or SP7 finds the extension too dark in the tripped light, the carried unit's floor head can aim farther, or a soffit unit be designed to pass the slots (a decision, not built).
4. **Combined test items:** T9 on a capture of the recede (the strips' one-way strikes, no flash); the gate rewrite (1.2 ms once a step) on a phone; the shadow atlas (the 12-cell room armed carries 13 shadowed spots: the atlas already reduced resolution in the harness); `Kit_*` LOD switches at 10 / 28 m on the end.

## Run! rethink · card 2: the T2 body (2026-11-04)

游戏视觉, account G, workflow `visual-run-rethink` (parent session local_1fc9f09c), stage "T2 body". The card is 71 §9, 游戏视觉 2 (`research/level_space/71_run_room.md`, level_space's page, read only), its T2 part: "then T2's block pieces and two baked clips with contact lists (phones: half-course strips, no grit; WebGL: T1 alone); grit by `54`'s GPU path; the hidden cells' strips (with 灯具适配) striking once as each cell seats". Red's answer to 71 §8, 2026-11-03 23:18: "全部按推荐" (71 §0.1), so decision 5 stands: the T1 head on every tier and platform, carrying **T2 重组** on desktop and iPad. 71 §5.3 is the brief: the end wall comes apart course by course from the top, streams back along the side walls and the soffit, the door on its steel rolls back alone, lit and untouched, and the blocks drop back into courses from the bottom up round it.

Not built: card 3's red looks (the parent's, after this workflow); T3 (not built now) and T4 (held for a pattern capture). Rule 5b holds: no game code builds a receding room; it enters with Red's walk of SP7 in scene 4. Not done: nothing on main, no download, nothing heard (rule 24); Unity only in `W/proj_runb66`, one batch at a time, edit mode.

### T.1 What changed (`W/apply/run_t2_body.sh`, stacked on the head since P.1, not applied)

Built at 10:01 as a cumulative package that carried the unapplied `run_recede_head` (113 paths). **The Package stage rebuilt it stacked on `run_recede_head` (P.1): 52 paths (45 new; 7 replaced, 4 metas kept), its bases the head's payload**, so the head lands first and this after it. The table below is the change against today's main, the head's part included.

| Path (main base → payload) | Change |
|---|---|
| `Scripts/Rendering/FrontRoomsRunRestack.cs` (new, e2e71f90) | the T2 body: the pieces of a level merged once a session into one skinned mesh (a bone per piece), LOD0/1/2 (blocks) or one level (strips), its two clips warped onto the schedule's phases, the contacts as cues, the grit on desktop |
| `Scripts/Rendering/FrontRoomsRunGrit.cs` (new, 1c2f0733), `Resources/Rendering/FrontRoomsRunGrit.shader` (new, 377e2022) | the mortar grit: grains written once per course landing, moved analytically in the vertex stage (54's grain look); desktop only |
| `Scripts/Rendering/FrontRoomsRunRecede.cs` (new, 1380c669; the head's 5b97a6f5 + T2) | the end's block field is its own static kit (`Kit_RunRecede_Wall`); `DriveRestack` (the body from a step's tell to the end of its seat, never in the glide or a restore); six cues appended; the grit's clock past the final seat |
| `Scripts/Rendering/FrontRoomsRunRig.cs` 97727e79 → f56090ab, `FrontRoomsRunDress.cs` 20c6da54 → 57e08a4f | the head's (H.1), unchanged by T2 |
| `Editor/Rendering/FrontRoomsRunRigTests.cs` 69cac3c5 → 5d9ef898 | R20 (the head's) and R21 (T2) |
| `Editor/Rendering/FrontRoomsKitImporter.cs` 4faea6aa → 5714cf2b | `Kit_RunRestack_*` imported readable (the body is merged from them at run time, as the door break's debris) |
| `Resources/Props/Models/Kit_RunRestack_{Blocks, Strips}` (.fbx + .json), `Kit_RunRestack_{Blocks, Strips}_{flinch, step}.json`, `Kit_RunRecede_Wall` (new); `Kit_RunRecede_Head` (the head's, now steel only) | the pieces, the clips, the static wall; the end's steel with a 51 mm lintel and the carried unit's arm cantilevered from the header |
| `Resources/Surfaces/RunB_Block{Upper, Lower}_Piece.mat`, `RunB_BlockCore.mat`, `RunB_BlockMortar.mat`, `Textures/Run_BlockCore_{A, N, S}.png` (new) | the corridor's two paints in mesh UVs (the end's own bond, locked to it), the raw block and the mortar |
| `Tools/Blender/frontrooms_kit/{run_restack_bake.py, assets/_run_restack_common.py, run_restack_blocks.py, run_recede_wall.py}`, `Tools/lookdev/gen_run_block_core.py` (new); `run_recede_head.py` (the head's, changed) | the generators |

### T.2 The pieces (71 §5.3 asked for about 90 "until the real Run_Block layout is counted")

- **Counted on the real bond: 69.** `Run_Block_A` is a 0.4 × 0.2 m running bond; the end carries it on both faces in mesh UVs locked to the end (front u = −x + 0.1, back u = x + 0.3, v = y), so the bed joints run on with the corridor's walls. 13 courses: c0 cut on the steel sill (0.06-0.20), c1-c11, c12 cut under the girder (2.40-2.53); c0-c10 stand in the two bays beside the RN-F frame (|x| 0.551-1.295), c11 and c12 run the full width over a new 51 mm steel lintel on the frame's head.
- **Hero LOD0:** a 1990 6-in CMU (ASTM C90): two 32 mm face shells with 3 mm painted arrises, 26 mm webs, one or two hollow cores cast round-cornered (20 mm fillets), torn mortar on the beds and heads (irregular six-sided mounds); LOD1 the shells and webs as boxes (the cores still read from 10 to 28 m); LOD2 one box. **33,396 / 3,852 / 828 tris** (424-516 a piece at LOD0). 71 estimated 5.4k at 60 tris a piece: the hero level is six times that and shows only within 10 m; a walker meets the re-stack at 13-20 m, on LOD1.
- **The swap is exact:** the at-rest wall (`Kit_RunRecede_Wall`, 120 tris, its bays running on under the frame's trim) and the pieces wear the same bond in the same UVs (`RunB_Block*_Piece`, the corridor's paints with `_FR_MESH_UV`; the wear stays world-space, as on every wall). The sheet's seat and jamb rows: T1 and T2 at rest are the same wall.
- **Raw faces and mortar:** `Run_BlockCore_{A,N,S}` (1024², tile 0.4 m: cement paste, sand, small stones, pores; generated, `Tools/lookdev/gen_run_block_core.py`); the mortar takes the same maps at a 0.2 m tile, lighter and warmer.
- **Phones:** 28 half-course strips (each bay course one strip a side; over the lintel two half courses and the middle block alone), a 3 mm chamfered box each, 1,232 tris.
- **The end's steel** (`Kit_RunRecede_Head`, now 4,044 / 1,780 / 264): the block field left it for `Kit_RunRecede_Wall`; it gained the lintel and a girder soffit plate, and the carried unit's arm is cantilevered from the header (the head stage's brace stood on courses that now come apart).

### T.3 The motion: two baked clips (the door break's method)

- **Clips:** `_flinch` (tell 0.25, travel 1.1, seat 0.2 s, 6 m) and `_step` (0.15 / 0.8 / 0.2 s, 3 m), 30 Hz keys in the end's frame plus a contact list; the final step (0.25 / 1.0 / 0.35 s) and the flinch alone play them warped per phase. The seat is each clip's end pose. Baked by `Tools/Blender/frontrooms_kit/run_restack_bake.py` as analytic choreography (the door break's prototype found Blender's rigid-body bake failing its own gates; `51_prototype.md` 3).
- **What the player sees:** the magnets let go and the mortar cracks; the courses come out from the top, the outer piece first, each lifting out of its bed and turning a few degrees; **they hold where they were while the door, its frame, the lintel and the end's steel roll back alone, lit**; then they come back to the door, the inner pieces first, sliding into lanes along the walls (the top course rises into the soffit corners, past the main and the tray), and land from the bottom up, the inner piece of a course first, the top course last into the seat with a 2 mm knock home.
- **The lit door never dips** (71 §5.7): the middle pieces over the door ride with it, and the carried lamp's flood onto the leaf is an obstacle the bake checks (the first build shadowed the leaf; fixed).
- **Gates** (the bake fails loudly): both ends exactly at rest; no two pieces interpenetrate (oriented boxes, worst 0.7 mm: touching faces); clear of the door's path, its frame and lintel, the main, the tray and its rods, the strip lamps, the tracks and trolleys, the header, sill and edge channels, the walls' rail and holders, the carried lamp's arm and cabinet, and its flood onto the leaf; along the corridor a piece never moves toward the player after its peel (worst 1.2 mm, the top course's knock home) and never stands more than 0.25 m in front of the line it left.

### T.4 Run time

- **One skinned body per room** (`FrontRoomsRunRestack`): one renderer per level, 4 / 3 / 3 material slots on desktop and iPad, 3 on phones; built with the end before the rig, so it is in the room's ambient and light gate like the end's other parts. Shown from a step's tell to the end of its seat (`FrontRoomsRunRecede.DriveRestack`); the static wall otherwise. **The keep-away glide and a restore keep the wall whole.**
- **Grit** (`FrontRoomsRunGrit`, `FrontRooms/RunGrit`), desktop only: each course's landing writes about 200 grains once into a 4,096-grain ring (2,570 a step); the vertex stage moves them (gravity, drag to each size's settling speed, at rest where they meet the floor), streaked and at least 1 px wide with the opacity as coverage, lit by the Forward+ lights (the carried goal lamp), gone by 2.6 s. One procedural draw per camera while any grain lives. No per-frame C# particles (71 §5.8).
- **Tiers:** desktop blocks and grit; iPad blocks, no grit; phones the strips, no grit; WebGL builds nothing (its schedule is T1).
- **Events for 声音设计:** MortarCrack, SubframeSlide, BlockLift (per course), StreamClatter (each stream), CourseLand and MortarSift (per course), from the clips' contacts; nothing is played (the contract, `W/apply/run_rethink_contract/README.md`, "Card 2, T2").
- **The uncovered cells' strips** (灯具适配's kit) are unchanged: the rig still strikes each once at its seat, after the body has landed (71 §5.7).

### T.5 Today unchanged, and the body: the proof (Unity in `C`, `FrontRoomsRunRigTests.RunBatch`, 23 passed, 0 failed)

- R18 (today's room) and R20 (the head) pass with the body in place; 0 `error CS`.
- **R21:** the four clips (69 / 28 pieces, T2's phases, rest at both ends, courses landing 0 → 12); a built 9 → 12 room on desktop: three levels 33,396 / 3,852 / 828, its materials gated, the static wall shown at rest; a walker's recede: the body flew 190 frames, pieces hung back up to 3.08 m, the wall whole in every dwell and after the final seat, 26 course landings bottom up, 5,140 grit grains; the glide and a restore ride whole and quiet; iPad blocks without grit, phones 28 strips without grit; a plain 9-cell room builds none of it.

### T.6 Images and film

- Sheet `images/run_t2_body.jpg` (+ `_fit.txt`, ALL LABELS FIT): TODAY, T1 (the wall riding whole, T2's timings), T2, T2 phone; the trip point (9 m) and the arch at the flinch's and the final step's mid-travel, the 9 m frames also cropped 4.4× at the end; the seat; the jamb at 0.35 m; a block and a strip in flight at 0.35 m (LOD0, a camera fill); COST.
- Film `video/run_t2_body.mp4` (10 s, 30 fps, H.264, no audio stream) + `images/run_t2_body_contact.jpg`: walk in at 3.2 m/s, the trip at FD1, walk on; the flinch and the final step re-stack round the lit door, the grit sifts in the goal lamp.

### T.7 Cost (desktop, Editor, measured in Unity)

| Item | Number |
|---|---|
| Blocks (69) | 33,396 / 3,852 / 828 tris; in flight 1 renderer, 4 / 3 / 3 slots |
| Strips (28, phones) | 1,232 tris; 1 renderer, 3 slots |
| Static wall at rest | 120 tris, 2 slots (the head stage's skins, now their own kit) |
| End steel | 4,044 / 1,780 / 264 tris |
| Kit meshes | 7.83 MiB (readable: merged at run time) |
| Skinned body | 10.32 MiB desktop and iPad, 0.44 MiB phones (made once a session) |
| Clips | 333 KB of JSON (four) |
| Textures | `Run_BlockCore_A/N/S`, 1024² each |
| Grit | one draw while alive, 4,096 grains × 32 B (128 KB), 2,570 grains a step |
| CPU | posing 69 bones 0.012 ms a frame; rig + recede tick 0.36 ms mean while apart, 3.97 ms worst (the trip's frame; Editor, batch) |
| GPU | not measured |

### T.8 The pattern (card 5) as numbers here

A proxy, not a Harding FPA or PEAT result: the harness films the T2 room and a T1 room on the same walker's path in the same light, so their difference is what the re-stack adds. **T2 differs from T1 on at most 5.25 % of the frame (0.55 % on average while apart), the largest luminance difference 0.186, and adds no frame with flash pairs** (the trip's own light change, 43.8 % of the frame, is the rig's, in both films). The Harding FPA or PEAT capture of the recede at T5 stays a combined-test row (card 5).

### T.9 Gate, and what the next stage must know

- Roslyn macOS and iOS on main with the package's seven code files as overrides (zsh array, read back from the `.rsp`): 0 errors; warnings 43 / 38, main's, none in our files (`W/run_rethink/logs/gate_t2_body/`). Unity in `C`: import and compile 0 `error CS` (three runs), EditMode 23 of 23.
- Package `W/apply/run_t2_body.sh` as built at 10:01: 113 paths (109 new, 4 replaced, 4 metas kept), DRY RUN OK on main; a rehearsal `--apply` on a scratch copy gave MANIFEST OK, then ALREADY APPLIED. Not applied. Since the Package stage it is stacked on the head (52 paths; P.1, P.2).
- **Honest flaws:** from the arch the re-stack is a few pixels, and at the trip point it covers at most 5 % of the frame (the end is 20 m away); the readable kit meshes and the skinned body hold about 18 MiB on desktop and iPad (`BoneWeight1` would trim the body); the mortar mounds are regular in size; the phone strips read as louvres, not blocks; the grit's size and density are set by eye.
- **Combined test items:** T9 and the pattern capture on the recede with T2; the body's GPU cost on an iPad (the hero LOD within 10 m); the grit on desktop at 1080p and with MSAA off.

## Run! rethink · card 4: the Run door's tell (2026-11-04)

游戏视觉, workflow `visual-run-rethink` (relayed from account G to B to A; parent session local_be0f3747 on A), stage "Run door tell". The card is 71 §9, 游戏视觉 4 (`research/level_space/71_run_room.md`, level_space's page, read only): "For the door entrance: the ALARM ZONE card and the red-banded detector base at the door (`AlarmTell/FrontRoomsAlarmTell.cs`), the corridor's armed light in the 15 x 15 in lite and under the door, readable from at least 5 m (with 关卡设计 5)." Red's answer to 71 §8, 2026-11-03 23:18: "全部按推荐" (71 §0.1), so decision 8c stands: reading one, D1, an honest Run door that fools a habit (71 §7.2), from T3 after one plain Run!; the changed door (D4) is not built. The signals it must carry are 71 §7.5's S1 (approach, 5-15 m: the square window, the light in it and under the door) and S2 (threshold, 2 m: the red-banded detector base and the ALARM ZONE card).

Not built: card 3's red looks (WAIT-RED in this workflow; nothing red is added: the tripped colours below are the rig's as built). Rule 5b holds: the map deals a Run door only with `rules.doorEntrance`, off until Red has walked SP7 in scene 4. Not done: nothing on main, no download, nothing heard (rule 24); Unity only in `W/proj_runb66`, one batch at a time, edit mode.

### D.1 What changed (`W/apply/run_door_tell.sh`, stacked on T2 since P.1, not applied)

8 paths, our files only (4 new, 4 replaced, their metas kept). Built at 13:00 on main; the Package stage rebuilt it stacked on `run_t2_body` (P.1): the rig, the dress and the tests go from the T2 payload to the clone's tested files (rig c0c64842, dress b94ade20, tests d4f2ec43: the 3-way merge of the two main-based payloads), the binder from main's.

| Path (base once T2 is in → payload) | Change |
|---|---|
| `Scripts/Rendering/FrontRoomsRunRig.cs` f56090ab → c0c64842 | `RunRigLayout.UseEntranceDoor(1.0, hinge)`: the entrance face's opening becomes the door's (1.0 × 2.1), the NOTICE plate moves to the hinge side; `FrontRoomsRunRig.SetEntranceOpen`: the light gate's entrance portal is none while the leaf is shut and the whole door once it is off the latch (an arch ignores it) |
| `Scripts/Rendering/FrontRoomsRunDress.cs` 57e08a4f → b94ade20 | no arch reveal at a Run door entrance (the DA frame sleeves its own, as at the goal) |
| `Scripts/Rendering/FrontRoomsRunRigBinder.cs` 969fef2e → 2e707ee0 | a door on the entrance edge (`DoorBetween(Outside, Cells[0])`) makes it a Run door entrance; `DressEntranceTell`: the card on the door's latch side, the red-banded detector on the ceiling at the door head's sight depth (3.69 m, past the 2.5 m trip, clear of the strips); the arch's tell exactly as landed; the light under the door each frame |
| `Scripts/Rendering/FrontRoomsRunDoorGap.cs` (new, 971446a3), `Resources/Rendering/RunDoorGap.shader` (new, 67a19202) | the light under the shut door (D.3) |
| `Editor/Rendering/FrontRoomsRunRigTests.cs` 5d9ef898 → d4f2ec43 | R22 |

The map side is 关卡设计's card 5, landed in main at 12:46 (`RunVariant.Door`, `EntranceDoor`, `IsEntranceDoorEdge` / `IsRunDoorEdge`, `DoorTrip` 2.5 m, the device point at the trip, the validator's checks, the Level Designer's Run card `RunDoor9`). The door is the map's RN-F as at the goal: the Steel DA frame with its fluted saddle, the Ward DA leaf with set A's F5 lite (15 × 15 in, polished wired glass), the lever set, never locked. This stage's own stand-in for that side (`CONTRACT_door.diff`, 12:0x) is superseded and does not ship; the contract section (`W/apply/run_rethink_contract/README.md`, "Card 4") says what our package reads.

### D.2 What the player sees, and where each signal is

- **5 m (S1):** a walnut door with a stainless kick plate and a square wired window where today's arch opens on the white corridor; the red ALARM ZONE card on its latch side, the NOTICE plate on its hinge side. Through the window, the far corridor: cool grey, the goal door small at its end.
- **3 m:** the line under the door: the saddle catches the corridor's cool white light where the leaf meets it.
- **0.35 m (hero, LOD0):** the window shows the lit corridor through the wire mesh (the strips, the sprinkler main, the far door); the card's final face (`CardFaceIsStandIn` false, wording signed 11-01).
- **2 m, the door pushed open (S2):** the red-banded detector on the ceiling 3.69 m in, seen under the 2.1 m head, past the trip line: opening and looking costs nothing, as 71 §7.2 asks.
- **Tripped:** the window and the line follow the corridor's light as the rig drives it (the mains out, the threshold head's warm pool, the reset's re-strike); nothing on the approach side reacts to the hunter (S1-S2 are part of the place, 71 §7.5).

### D.3 The light under the door (`FrontRoomsRunDoorGap`)

The RN-F leaf hangs 3 mm over the saddle's 12 mm flat. Light from the lit corridor floor leaves that slot almost level, lands on the saddle's outer flat, catches its bevel and dies over a hand's width of carpet. A 3 mm slot is far under a shadow texel, so no Light can draw it: it is one additive strip draped 0.4 mm over the saddle's outer half and the floor in front (17 × 13 vertices, the profile in vertex alpha), its level **read, never typed**: each frame it sums the rig's own Lights at the corridor floor just inside the door, attenuated as URP attenuates them, times the floor's albedo. So armed it is the strips' cool white (0.227 / 0.256 / 0.288 linear), black in the trip's gap, the threshold head's warm pool after the trip (0.227 / 0.153 / 0.083), back with the reset; it fades as the leaf leaves its latch, when the corridor's real light comes through. One draw, no Light, no texture, every tier; no allocation per frame. (Run 4 found the level set as sRGB through `MaterialPropertyBlock.SetColor`, five times too dark in the linear project; fixed with `SetVector`.)

### D.4 Today unchanged: the proof (Unity in `C`, `FrontRoomsRunRigTests.RunBatch`, 24 passed, 0 failed)

- R18 (today's preset, field for field) and the rest pass with the door code in place; 0 `error CS`.
- **R22:** every `Corridor(7..14)` keeps today's arch (0.7 half, 2.2 head; its NOTICE plate at x 1.1, the card at x −0.994, the detector at 2.71 m); `UseEntranceDoor` with either hand gives the 1.0 × 2.1 opening, no reveal, the header from 2.1 m, the plate on the hinge side; the gate's entrance portal is none shut and the whole door open; the card at x −0.794 (latch side), the detector at 3.69 m; the strip one draw, zero Lights, cool white armed, black in the trip's gap, the warm pool tripped, 17 Lights read, no allocation.
- In the game today the map deals no Run door (the switch is off), so the binder finds no door and builds every room as before.

### D.5 Images

Sheet `images/run_door_tell.jpg` (+ `_fit.txt`, ALL LABELS FIT): TODAY (the arch), RUN DOOR, both again on the phone tier; rows 5 m, the 5 m frames enlarged 3× round the door, 3 m, the lite at 0.35 m, the card at 0.35 m, the sill at 0.6 m, pushed open from 2 m, 3 m tripped; a row on the line under the door (3 m enlarged 4× and the sill, the strip off as a debug frame beside the armed frame); COST. Made in `C` by the clone-only harness `FrontRoomsRunDoorTellHarness` through the Level Designer's RunRoom view (the game's own map builder, lamps, kits and film look; the doors dressed as in Play), seed 3, 1920 × 1080, vertical FOV 76, eye 1.62 m.

### D.6 The read, as numbers (on the tonemapped 1080p frames, relative luminance)

| Frame | The lite against the leaf beside it | The line under the door | The card |
|---|---|---|---|
| 5 m, desktop | 0.043 against 0.093 (0.46×); the same pixels through today's open arch 0.046 | 3 px tall: 0.139 against the carpet's 0.184; not read | 36 × 22 px, red (0.43), legible at 1080p |
| 5 m, phone tier | 0.043 against 0.093 (0.46×) | 3 px: 0.187 against 0.151 (the phone tier's contact shadow darkens the carpet); not read | the same |
| 3 m, desktop | 0.074 against 0.103 (0.72×); the arch 0.086 | 6.5 px: 0.125 with the light, 0.050 with the strip off (2.5×): a cool grey line on the dark saddle | 63 × 37 px |
| 3 m, phone tier | 0.056 against 0.098 (0.57×) | 0.171 against the carpet's 0.141 | the same |
| 3 m, tripped | 0.062 against 0.097 | 0.077 (the threshold head's warm pool) | the same |
| the lite at 0.35 m | 0.153 (phone tier 0.114): the lit corridor fills it; the open arch from the same eye 0.187 | | |

So at 5 m the red card and the square lite carry the read; the lite is a cool square **darker** than the leaf (from 5 m a standing eye looks down through a window at 1.19-1.57 m onto the far corridor's lower walls and floor, as dark through today's open arch), and the line under the door is about 3 px tall, below a read; it reads from about 3 m. This is the card's open flaw: brighter would need glass that scatters the corridor's light (it would hide the corridor that 8e lets the player watch through the lite) or a lamp aimed at what the lite sees; neither is built. Red judges it in SP7.

### D.7 Cost (Editor, batch, one Unity; GPU not measured)

| Item | Number |
|---|---|
| The tell's kits, as landed | card 734 / 46 / 14; detector 8,590 / 1,210 / 314; RN-F frame (Steel DA, saddle) 2,030 / 724 / 276; leaf (Ward DA, F5 lite) 4,720 / 1,316 / 212 (tris LOD0 / LOD1 / LOD2); the door is the map's RN-F, as at the goal |
| New | the strip: 221 vertices, 384 tris, 1 draw, 0 Lights, no texture; one shader; no new shadow map |
| The room with the Run door | 996 renderers, 1,461 draws, 330,073 tris LOD0 (the arch room 1,002 / 1,467 / 329,773: the arch's reveal pieces go) |
| Rig lights | 18, armed 10 on, 9 shadowed: the same as the arch room |
| CPU | the strip's tick 3.7 µs (17 Lights read); the light gate's rewrite 0.89 ms once per change of the door (Editor, batch) |
| GPU | not measured |

### D.8 Gate, and what the next stage must know

- Roslyn macOS and iOS on main as of 13:08 (关卡设计's 12:46 card 5 and 13:04 lamp tick files in it) and the package's 5 code files as overrides (zsh array, read back from the `.rsp`): 0 errors; warnings 45 / 38, main's, none in our files (`W/run_rethink/logs/gate_door_tell/gate8_*`). Unity in `C`: import and compile 0 `error CS`; EditMode 24 passed, 0 failed.
- Package `W/apply/run_door_tell.sh`: 8 paths (4 new, 4 replaced, 4 metas kept), with three read-only API rows (`DoorBetween`, the card face final, `IsEntranceDoorEdge`). Built main-based at 13:00 (DRY RUN OK on main, rehearsal MANIFEST OK, then ALREADY APPLIED); since the Package stage stacked on T2, rehearsed in the landing order (P.2). Not applied.
- **Honest flaws:** the 5 m light read (D.6); the line under the door is drawn, not lit (a 3 mm slot has no shadow-map answer), at a level summed from the rig's Lights; the detector cannot be seen with the leaf shut (S2 holds at the threshold once the door is open, as at the arch).
- **Combined test items:** a Run door room in Play (the preview's `RunDoor9`, or the deck with `doorEntrance` on in a tools run): the card, the lite and the line at 5, 3 and 2 m at a walk and a sprint on desktop and a phone; the strip's level through the trip and the reset; the gate's portal as the leaf swings; the room met from its goal end (with 关卡设计).

## Run! rethink · landing: the order, the contracts and the combined test (2026-11-04)

游戏视觉, workflow `visual-run-rethink` (relayed from account G to B to A; parent session local_be0f3747 on A), stage "Package". The cards are 71 §9 游戏视觉 1, 2, 4 and 5 (`research/level_space/71_run_room.md`, level_space's page, read only); Red's answer to 71 §8, 2026-11-03 23:18: "全部按推荐" (71 §0.1). This stage applied nothing: the parent lands. Rule 5b holds: the receding end and the Run door entrance enter the game only after Red has walked SP7 in scene 4. Card 3 (the red looks) is WAIT-RED in this workflow (灯具适配 1) and was not built. Not done: nothing on main, no Unity run (the clone's run 7 stands, P.2), nothing heard.

### P.1 The order (`W/apply/`, apply convention v2)

| Order | Package | Paths | Bases | Main as of 13:27 |
|---|---|---|---|---|
| 1 | `run_rig_cells` (card 1, R) | 3 | main | landed 06:40: ALREADY APPLIED |
| 2 | `run_recede_head` (card 2, the head, H) | 67: 64 new, 3 replaced, 3 metas kept | main | DRY RUN OK |
| 3 | `run_t2_body` (card 2, T2, T) | 52: 45 new, 7 replaced, 4 metas kept | main + 2 | refuses until 2 is in (by design) |
| 4 | `run_door_tell` (card 4, D) | 8: 4 new, 4 replaced, 4 metas kept | main + 2 + 3 | refuses until 3 is in (by design) |

- **Why stacked.** All of them replace some of the same three files (the rig, the dress, the tests), so this stage rebuilt 3 over 2 and 4 over 2 and 3 (`W/run_rethink/tmp/pkg/mkapply_stacked.py`, the pool look's helper with its own view folder). Each then lands by its own dry run and `--apply`, with no rebase at landing.
  - 3 holds only T2's delta: `FrontRoomsRunRecede.cs` 5b97a6f5 → 1380c669, the tests ccbf4803 → 5d9ef898, `FrontRoomsKitImporter.cs` 4faea6aa → 5714cf2b, `Kit_RunRecede_Head` (fbx, json, fbx.meta: the T2 piece material remapped) and `run_recede_head.py`, and T.1's 45 new paths.
  - 4 takes the rig f56090ab → c0c64842, the dress 57e08a4f → b94ade20 and the tests 5d9ef898 → d4f2ec43 (the 3-way stack the clone tested), the binder 969fef2e → 2e707ee0, and adds `FrontRoomsRunDoorGap.cs` and `RunDoorGap.shader` with their metas.
  - The main-based builds they replace (T2 cumulative, 113 paths; the door, 8) are kept in `W/run_rethink/tmp/pkg/mainbased_1315`.
- **If main moves under a base** before the parent lands, the first package that refuses goes through `W/tools/rebase_apply.sh` (rule 2), then the next one is dry-run again.
- **Docs** land with them, in our folder, as replacements (P.5).

### P.2 Rehearsal and gate

- **Rehearsal** (`W/run_rethink/tmp/pkg/rehearsal/`): a scratch copy of main holding its `ProjectVersion.txt`, all 3,776 of its Assets metas (so the GUID checks are main's), the bases and the three read-only API files. In P.1's order each package gave DRY RUN OK, then `--apply` MANIFEST OK, then ALREADY APPLIED: 26, 20 and 2 new GUIDs, none used in main; T2's one replaced `.meta` keeps its GUID; the door's API rows (`DoorBetween`, the card face final, `IsEntranceDoorEdge`) found. **The end state equals the clone on all 118 paths**, so what lands is what Unity run 7 tested (EditMode 24 of 24, 0 `error CS`; D.4).
- **Gate** (rule 25; `W/run_rethink/tmp/pkg/gate_stack.sh`, logs `W/run_rethink/logs/gate_package/`): Roslyn macOS and iOS on main at each landing state, main + the head (4 code overrides), + T2 (7), + the door (9), every override read back from the `.rsp`: COMPILE OK, 0 errors; warnings 45 / 38, main's, none in our files. Run at 13:16-13:20 and again at 13:27-13:29, after 设置菜单's `FrontRoomsRenderSetup` (13:23) and 水体仿真调研's landings: the same. The two shaders (`FrontRoomsRunGrit`, `RunDoorGap`) compiled in Unity in the clone; Roslyn cannot read them.
- On main as it is, the two stacked packages refuse with nothing written (their bases are the earlier payloads), as designed.

### P.3 The contracts (`W/apply/run_rethink_contract/README.md`, section "Final")

- **关卡设计.** The cell count: the rig reads `room.Cells.Length` (7 to 14; anything else builds 9 with a warning); their 08:09 landing of card 1's diff is all the record needs. The receding variant (their card 4, after SP7): the record (L_max cells, `RecedeFrom = 9`, the 8-byte `recede.Record`), the seats (`SeatReached`: the goal point and normal, colliders, walk volume and graph edges up to each line, the release swap at the final seat), the movable GoalHead (their RN-F on `recede.DoorMount`; the end's collider theirs), the trip at the FD1 frame (`recede.TripLine`, 9 m). Our binder's ~15 lines for it wait for their record fields. The Run door entrance: their card 5, in main since 12:46; nothing more is asked; open on their side: a room met from its goal end.
- **声音设计.** 16 `RunRecedeCue` events on `FrontRoomsRunRecede.Cue` (T1's ten on every tier, T2's six with the blocks and strips), the rig's Run! B events unchanged, nothing new for the Run door (a normal wooden door, rule 10). Also written into `61_for_sound.md`, its update of 11-04, where the sound line reads Run! hooks.
- **灯具适配.** The carried twin-head (their `Kit_EmergencyTwinHead` on `Kit_RunRecede_Head`'s `lamp_mount`, arm from the end's header, head 0 on the leaf, head 1 on the floor 4 m ahead); the hidden strips' one strike at each seat in the room's state, their kit untouched; card 3's red waits for their card 1's still. Their `zone_lamps_redtube` landed at 06:33 and its MERGED row records Red's yes at the brightest level: whether that starts card 3 is the parent's call.

### P.4 The combined test (rule 25: one run, silent; paste into `handoff/visual_combined_test.md` when the packages land)

```
## run_recede_head + run_t2_body + run_door_tell (Run! rethink cards 2 and 4, account A; inert until SP7, rule 5b; detail research/room_visuals/run_b_production/60_production_66.md P.4)
- [ ] Run A (import + EditMode): 0 console errors; the new kits import with their LOD groups (Kit_RunRecede_{Head, HeadGear, SweepSeated, SweepMoving, Track_3m, TrackStop, FloorSlot_3m, Wall}, Kit_RunMagnetHolder, Kit_RunCompartmentFrame, Kit_RunRestack_{Blocks, Strips}), Kit_RunRestack_* readable; the FrontRoomsRunGrit and RunDoorGap shaders compile for every build target; FrontRoomsRunRigTests 24/24 (R18 Corridor(9) = Corridor1x9, R20 the recede, R21 T2, R22 the Run door).
- [ ] Run C (forced Run! room): today's plain 9-cell room with its arch looks and behaves exactly as before (the tell: card x -0.994, detector 2.71 m in; the trip; the RESET re-strike); no end, wall kit, body, grit or door strip is built.
- [ ] The recede (desktop; a receding layout through the clone harnesses FrontRoomsRunRecedeHarness / FrontRoomsRunT2Harness, or SP7 once scene 4 drives FrontRoomsRunRecede): 9 -> 12 and 9 -> 14 trip at FD1 (9 m); a two-cell flinch, one-cell steps, the heavy final seat (seats at 1.55 / 3.35 s for R 9, 1.55 / 2.90 / 4.25 / 6.05 s for R 15); the keep-away glide 6 m/s within 12 m; the gaze gate 35 deg (fallback 18 m); the lit door never dips; the light gate's far end follows each step; a ghost band at each line it left.
- [ ] T2: the wall comes apart from the top course, the door rolls back alone, the courses land bottom up round it; nothing crosses the door, its frame, the main, the tray or the strips; the wall whole at rest, in the glide and after a restore; iPad blocks without grit, phones 28 strips, WebGL T1 alone.
- [ ] The hidden strips: dark behind the end, one strike at each seat in the room's state (tripped: 灯具适配's red tube on the battery circuit, as landed); no flash pair.
- [ ] Card 5, not red: T9 over the trip, the recede and the reset in today's light; Harding FPA or PEAT on that capture at T5 (the clone's proxy: T2 at most 5.25 % of the frame, no added flash pair); the T2 pattern.
- [ ] Cost on device: the gate rewrite (1.2 ms once a step) on a phone; the T2 body's GPU cost on an iPad with the hero LOD within 10 m; the grit at 1080p with MSAA off; the shadow atlas with a 12-cell armed room's 13 shadowed spots; memory (readable kit meshes + skinned body about 18 MiB on desktop and iPad); the end's LOD switches at 10 / 28 m.
- [ ] Events: the 16 RunRecedeCue cues per step in the contract's order (a one-cell T2 step: 43 T2 cues besides T1's); nothing audible from our code (rule 24). The record: a receding room dropped and rebuilt 48 m or more away after its recede began comes back seated at its deepest line, quietly.
- [ ] The Run door (关卡设计's rules.doorEntrance on in a tools run, or the Level Designer's RunDoor9): the card (latch side), the square lite and the line under the door at 5, 3 and 2 m, at a walk and a sprint, desktop and a phone (PARTIAL: at 5 m the card and the lite carry it, the lite darker than the leaf, 0.043 against 0.093; the line from about 3 m; Red judges in SP7); the line armed cool white, black in the trip's gap, the warm pool tripped, back with the reset, fading as the leaf leaves the latch; the gate's entrance portal none while shut, the whole door open; the detector 3.69 m in, seen under the head once open; a room met from its goal end (with 关卡设计).
- [ ] WAIT-RED (灯具适配 1; card 3 not built): the Relay read at 2.05 m under red with protan (at least 4 : 1); T9 over the red trip front and B's pulse train; Harding FPA or PEAT on that capture; the dark reds on the phone tier; one sheet per look along the film's camera path.
```

### P.5 What lands besides the packages (our folder, docs only)

- This file, as a replacement while main's copy is a2516eef: main's text plus sections H, T, D and P (R.1's header now says landed; T.1, T.9, D.1 and D.8 say stacked).
- `images/run_recede_head.jpg` (+ `_fit.txt`, `_contact.jpg`), `images/run_t2_body.jpg` (+ `_fit.txt`, `_contact.jpg`), `images/run_door_tell.jpg` (+ `_fit.txt`); `video/run_recede_head.mp4` and `video/run_t2_body.mp4` (H.264, no audio stream). Every sheet is in Shot format with chip PARTIAL and ALL LABELS FIT.
- `61_for_sound.md`, as a replacement while main's copy is adcd8f07: main's text plus its update of 11-04 (the 16 cues).
- VL: three rows in the day's batch after landing; the spec and the frames the combined test takes are in `W/vl_pending/run_rethink.md`.
