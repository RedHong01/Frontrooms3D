# 71 · Station look v1: the Front Desk, build report (游戏视觉, 2026-10-29)

**What this is.** The build stage of 游戏视觉's `station-look-v1` workflow: the visual side of A26, Floor 1's first room (the
station). It follows `70_station_look_v1_plan.md` after the plan check (14 + 10 = 24 changes, applied in place first; the plan's
§17 says where each went, and §1 below how each was built). Everything was built and proved in the clone `W/proj_station_2`
(made with `mkclone.sh`), with the profile's `stationModule` switched on **in the clone only**. Nothing was written in main
except this report and the plan file; no Unity, batch run or render touched main.

**Two accounts.** The build ran on account B (wf_d09cc867-20d) until B's session limit at 18:25, when it was stopped on purpose
with the build, the captures and every suite but one done (InspectPlaytest OFF was cut mid-run). It was finished on account A
(switch #8, session local_5656cffd) from the partial work, without redoing anything finished (§0).

- **Package:** `W/apply/station_look_v1.sh` (convention v2, `hud_key_b.sh`'s format; dry run by default, `--apply`, base sha1s,
  payload, expected manifest, a header listing every path). **Its dry run passes against main now: `DRY RUN OK` (19:05, account A; §4).**
  Not applied; LANDINGS / MERGED untouched. Apply command (only with 关卡设计's switch-on, contract §2):
  `bash /Users/redwang/FrontRoomsVisualWork/apply/station_look_v1.sh --apply`.
- **Switch-on note for 关卡设计:** `W/apply/station_look_v1_contract/README.md` (the one profile line, what lands together and
  in what order, contract rows C-1 to C-8, the numbers their tests can check).
- **URP light layers:** `W/apply/station_look_v1_urp.sh` (the flag + `Station_Ceiling`), separate, dry run OK, for the parent's
  explicit approval only (plan check C2-8; §5 gap 1).
- **Images:** `W/station_look_v1/images/slv1_*.jpg` (Shot format), listed with verdicts in `W/vl_pending/station_look_v1.md`.

**Status: READY** (the package; rule 22 gate green on main as it is now). On the clone re-synced to main 18:43 + the payload:
`Validate` + `ValidateStation` **0 errors / 0 warnings**, including 关卡设计's new R3 rules (screen centre 2.61 m <= 2.65,
push-in clear); MapVerification **PASS 100/100 + start group 1600/1600** ON and OFF, both = the map line's baseline; MIT **358 / 359** ON (the one failure is the landing assertion "no station module", by design, contract C-8) and **359 / 359** OFF; the autopilot (`-autopilotExit -autopilotSeed 2554 -autopilotStation 4`) **PASS**, 15 / 15 station checks ON (29 props placed) and OFF; InspectPlaytest **134 / 0** ON (placard on the station mount) and OFF (today's P1 mount);;
Roslyn **COMPILE OK** macOS + iOS on main 18:4x + the package. From the account-B round (unchanged payload): LOD0 70,946 / 100,000
and 110 (116 with the ceiling) / 120 renderers; no light leak at the 1 % bar in Play mode; check 2 > 50 % on every card (worst
75.4 %); the take drops the board 75 % on the worst card; the placard's EXAMINE caps 12.57 px against the 12 px bar.

**Not done, all open items (§6), none a gate under rule 22:** the phone frames are world views, **not compared with Figma
2528:5403** (no handheld HUD in them; the HUD is not in this package); the placard's **glow-ink dark check against Q16 1.30** was
not run on the station sheet; the **Mac build leak test** (61 §15) was not run (a build needs Red's yes); the station's paper
(register, tent card, DAR, key plate, annunciator labels) is **blank until 平面视觉's atlas** (plan GAP 27: the group should not
land with a blank register unless the parent accepts it).

**Changes during the build that the reader should know** (all in §5): V5 picture sync landed in main at 17:43 on the same
ScreenVideo file, so our hunk was 3-way merged onto it; `Station_Ceiling` moved to the URP step because main's MIT fails with it;
the TV light's shadow bias was fixed in URP's units after a sweep (it had darkened the key board and failed the take in v5); three
desk items moved a few cm because the built kits pass through the counter's end panels at the plan's positions, and the cup was
put back on the blotter; 平面视觉 updated the room sheets at 17:53 with this plan's desk and riser, and the placard textures were
re-packed from them (re-checked on A: a fresh pack of the final prints is byte-identical to the payload).

## 0. The resume on account A (switch #8, 18:40-19:1x)

Read first: the clone, the work dir `W/station_look_v1/` (logs, suites, shots, images, tools), the account-B build agent's
transcript and report drafts, `handoff/visual.md` 16:41-18:25, LANDINGS (no CLAIM from this workflow is open: nothing to finish or
release). Found finished: the plan check applied (plan §17), all 26 kits, the surfaces, the module, the code, the captures
(v1-v6), the images and the VL list, the package (dry run OK 18:17), the contract note, Roslyn on main 18:17, and every suite
except InspectPlaytest OFF. Done on A:

1. **Main had moved** since the clone's last sync (关卡设计's M5 + R3 layout rules 17:33 and Q1 = B 17:49, 声音设计's S2 fix
   17:38, 交互设计's MobileZoneName 18:38, HUD O-30 18:4x, settings strings): 14 main files the package does not touch were copied
   into the clone (Game 527ce171, Game.Mobile, MapWorld 5c57cfa5, RoomModuleData a70f8ed2, WayCueDriver, Wayfinding, KeyHud
   60f28e7f, Audio/Fmod, Audio/ScreenSound, SettingsMenu, DesktopSettingsView, MIT 5b1b9094, WayfindingTests, LevelDesignerTests;
   the clone's old copies are in `W/_partial/proj_station_2_pre_resync_1029/`) and the FMOD banks refreshed. After that the clone's
   `Assets/` = main's + the payload, byte for byte (payload vs clone: 0 differences; main vs clone: only the 4 replaced files).
2. **One test round on that base** (rule 22): `Validate` on the new contract, then MIT, InspectPlaytest, the autopilot and
   MapVerification with the module OFF (main's profile bytes 07514c33), the switch-on, and the same four ON (`tools/suite_chain7.sh`,
   logs `logs/r1-r10_*.log`, JSON copies `suites/r*_*.json`). No fix round was needed.
3. Roslyn on main 18:4x + the package (macOS + iOS), the dry run against main, the room-sheet re-pack check.
4. This report, the contract note's numbers, and the VL list (the EXAMINE check image added).

## 1. The plan check: 24 changes, applied first

Both checkers returned CHANGES (14 + 10). Every change was applied to `70_station_look_v1_plan.md` before the build, in
place, and the plan's new **§17** lists each change with where it went. In short:

| Change | Applied in the plan | Built as |
|---|---|---|
| C1-1 TV off for good at the take | §8 take row, §13, §14, GAP 43 | `FrontRoomsScreenVideo.PowerOffForGood()` (on main's R3 + V5 file, 3-way merged; a latch so E and the still mix never turn it back on); the dresser calls it on `map.StationKeyTaken` |
| C1-2 Attach passes Beacon | §8 step 2, §14.3 | `Attach(screenMount, null, FrontRoomsScreenSoundRole.Beacon)`; measured SoundRole Beacon |
| C1-3 the key gets the Station bit | P06, §9, §14.1 | the dresser tags `Kit_Key*` and M3's `FloorKeyName` object (found under the station or by overlap at the KeySpot) |
| C1-4 lamps per 61 §7; default-layer lights listed | §9 rewritten | entrance pair both layers, the other station lamps Station only; scene directional and Relay: contract notes C-4, C-5 |
| C1-5 / C2-6 no header panel over D | §5, §6.3, GAP 17 closed | none built; the stream door's header stays bare |
| C1-6 / C2-7 static entrance = the flush door | §6.3, GAP 15/18 | pulls + kick plates on the stand-in leaves; the stand-in jambs and head **deactivated**; the plate unshifted in both phases |
| C1-7 keep the take point (9.7, 10.4) | §1, §2, P03, §7, GAP 1/2 | KeySpot (10.232, 1.4215, 10.40) on the hook; take point (9.73, 10.40) |
| C1-8 / C2-9 bracket bottom >= 2.15 | P01, §8, proof (c)(11) | hinge raised to 2.36; measured min y 2.164 for set + bracket |
| C1-9 beige plastics | P02, GAP 42 | `Kit_CRTTV_Station` in `Prop_PlasticBeige` |
| C1-10 G9 + S15 | P07/P08, §13, GAP 20/21/28 | cabinet, sign-out sheet and the empty hook on the riser's south face (1.36 m from the seat); DAR / sign-out / register / key-plate transcripts in the catalog, unregistered until the atlas |
| C1-11 S26, S17 | §3.5 | `Kit_HookHeavy`, `Kit_BinderPostOrders` placed |
| C1-12 three checks | §15 (c)(2) | Weber, `ingame_check.py`, the TV colour per card: §3 (c) below |
| C1-13 clean paint | §4, GAP 32 | `Station_Wall` macro tone/dirt/grime 0 |
| C1-14 work surface to the fascia | P09 | 0.74 surface z 7.23-7.80 under the ledge; desk items on it |
| C2-1 floor-origin wall kits | §1/§3 rows | every wall kit at y 0 with heights baked in; outlets at centre 0.30; every top <= 2.85 (max 2.83) |
| C2-2 KeySpot outside P03 | P03, §1 | body-only sidecar collider (0.040 deep); Validate 0 errors |
| C2-3 layers: directional, key, ceiling, lamps, bit ops | §9, §4 | `Station_Ceiling` built as an L0_Ceiling copy, then **moved to the URP step** (§5 gap 1: main's MIT fails with it); masks by `|=` / `&= ~` only |
| C2-4 shared names | §4, §12 | `Kit_CRTTV_Station`, no pile blocks (measured), `Station_StainlessGuard`, `Prop_StationKeyPlate` |
| C2-5 Beacon, LOD hookup, power-off dependency | §8, §13 | picture added to the set's LODGroup (3 of 3 levels, measured) |
| C2-8 URP asset not in the package | §9, §13, GAP 11 | `W/apply/station_look_v1_urp.sh`, separate |
| C2-10 counts and cork | §11, §4 | renderers counted on the built set; `Prop_Cork` |

## 2. What was built (clone `W/proj_station_2`)

### 2.1 The module: `Assets/Levels/Modules/Station_FrontDesk_4x4.asset` (new GUID 5182c019…)

4 x 4, Standard (1), Level 0 (0), columns None, fill None, weight 0, allowRotate 0. South `00000000`, east `00000000`, north
`00000200` (E1 = Arch at column 2), west `00020000` (E2 = Arch at row 1), innerEast / innerNorth all Open. Lamps (i + 4j)
`05010501 05050505 05050505 05010606` (row 0 Dim · Steady · Dim · Steady; row 3 Dim · Steady (placard) · Off (axis) · Off
(TV / riser)). 29 props (P01-P29 below), 4 markers set through the inspector array's data:

| Marker | x / y / z, yaw | host |
|---|---|---|
| KeySpot (0) | 10.232 / 1.4215 / 10.40, 270 | `Kit_KeySwitchPlate` (its `key_hook` anchor) |
| StationBeacon (2) | 9.35 / 2.461 / 11.316, 180 | the tilted screen's centre (measured 9.35 / 2.467 / 11.334) |
| Placard (3) | 5.50 / 1.56 / 11.92, 180 | — |
| StationEntrance (4) | 7.50 / 0 / 0, 0 | — |

Props and where they stand (module metres; every wall kit's origin is the wall face at floor level):

| P | Kit | x / z / y, yaw | top | note |
|---|---|---|---|---|
| P01 | Kit_TVWallBracket | 9.35 / 11.92 / 0, 180 | 2.72 | hinge `set_mount` y 2.36, 0.05 off the wall, 20° |
| P02 | Kit_CRTTV_Station | 9.35 / 11.63 / 2.33, 180 | 2.83 | re-seated on P01 at run time |
| P03 | Kit_KeySwitchPlate | 10.30 / 10.40 / 0, 270 | 1.65 | riser west face |
| P04 | Kit_FireAnnunciator | 10.30 / 11.40 / 0, 270 | 1.75 | centre 1.50, 1.0 m north of the key |
| P05 | Kit_ConduitRun_StationKey | 10.30 / 10.40 / 0, 270 | 1.30 | from the plate's box down to 1.00, 1.0 m north, up into P04 |
| P07 | Kit_KeyCabinet_Putty | 11.25 / 9.90 / 0, 180 | 1.70 | riser south face, door shut |
| P08 | Kit_Clipboard_Wall | 10.72 / 9.90 / 0, 180 | 1.44 | sign-out sheet, 1.36 m from the seat |
| P28 | Kit_HookHeavy | 10.42 / 9.90 / 0, 180 | 1.62 | the empty watchclock hook, buckle loop |
| P09 | Kit_StationCounter_Run120 | 9.50 / 7.50 / 0, 180 | 1.07 | collider |
| P10 | Kit_StationCounter_Run075 | 10.475 / 7.50 / 0, 180 | 1.07 | collider |
| P11 | Kit_StationCounter_End x 2 | 8.9125 and 10.8375 / 7.50, 180 | 1.07 | colliders |
| P12 | Kit_SecurityLetters | 9.875 / 7.20 / 0.75, 180 | 0.85 | from 信息设计's SVG; origin at the fascia, so the letter faces stand at the plan's 7.181 (12.7 mm standoff + 6.35 mm letters) |
| P13 | Kit_TaskChair_Charcoal | 9.90 / 8.80 / 0, 349 | 0.95 | collider, faces the TV |
| P14 | Kit_JacketDraped | 9.90 / 8.80 / 0, 349 | 0.98 | cloth-baked on the chair |
| P15 | Kit_RegisterBook | 9.55 / 7.35 / 1.07, 180 | 1.09 | on the ledge, reads from the visitor side |
| P16 | Kit_TentCard | 10.20 / 7.35 / 1.07, 180 | 1.16 | PLEASE SIGN IN (blank until the atlas) |
| P29 | Kit_BinderPostOrders | 9.08 / 7.62 / 0.74, 90 | 0.79 | POST ORDERS spine (blank). **Plan 9.02: moved 6 cm** (at 9.02 it passes through the west end panel; §5 gap 14) |
| P17 | Kit_Clipboard | 9.37 / 7.56 / 0.74, 0 | 0.76 | the DAR. **Plan 9.25 / 7.55: moved 12 cm** (at 9.25 it overlaps the binder; gap 14) |
| P18 | Kit_DeskBlotter | 9.80 / 7.52 / 0.74, 0 | 0.75 | phone list under glass |
| P19 | Kit_CoffeeCup | 10.00 / 7.68 / 0.7505, 0 | 0.85 | cold coffee, ring stain; **on the blotter** (its top is 10.5 mm, so y 0.7505 for the plan's 0.745) |
| P20 | Kit_Ashtray | 10.22 / 7.66 / 0.74, 0 | 0.77 | glass |
| P21 | Kit_DeskPhone_Corded | 10.48 / 7.60 / 0.74, 20 | 0.83 | line lamps dark; `Kit_DeskPhone` already carries the coiled handset cord, so nothing was added (966 tris) |
| P22 | Kit_RadioCharger | 10.72 / 7.62 / 0.74, 0 | 0.79 | empty well. **Plan 10.74: moved 2 cm** (at 10.74 it passes through the east end panel; gap 14) |
| P23 | Kit_NoticeBoard | 11.92 / 8.40 / 0, 270 | 1.65 | east wall, staff side |
| P24 | Kit_WallClock (reused) | 3.00 / 11.92 / 2.20, 180 | 2.36 | — |
| P25 | Kit_TVCords | 9.55 / 11.92 / 0, 180 | 2.42 | power + RG-59 down the wall, over the rail |
| P26 | Kit_OutletDuplex (reused) | 9.60 / 11.92 / 0.30, 180 | 0.36 | centre-origin |
| P27 | Kit_JackF | 9.75 / 11.92 / 0.30, 180 | 0.36 | centre-origin |

### 2.2 The kits (Blender kit, `Tools/Blender/frontrooms_kit/assets/`, built with `build_asset.py` into the clone)

26 new kits, 28 modules (`_station_common.py` holds the Unity-space helpers and the paper atlas cells). LOD0 / LOD1 / LOD2
triangles as exported, against the plan's budget:

| P | Kit | plan LOD0 / LOD1 / LOD2 | built LOD0 / LOD1 / LOD2 | sidecar LOD0 / LOD1 |
|---|---|---|---|---|
| P01 | `Kit_TVWallBracket` | 3,000 / 1,000 / 120 | 3448 / 1104 / 864 | 3448 / 1104 |
| P02 | `Kit_CRTTV_Station` | 9,000 / 3,000 / 400 | 7210 / 4724 / 4466 | 7210 / 4724 |
| P03 | `Kit_KeySwitchPlate` | 3,500 / 1,200 / 150 | 3988 / 1852 / 1668 | 3988 / 1852 |
| P04 | `Kit_FireAnnunciator` | 6,000 / 2,000 / 200 | 6446 / 1650 / 1210 | 6446 / 1650 |
| P05 | `Kit_ConduitRun_StationKey` | ~2,000 / 700 / 0 | 1772 / 788 / 788 | 1772 / 788 |
| P07 | `Kit_KeyCabinet_Putty` | 6,804 / 2,300 / 250 | 2882 / 858 / 750 | 2882 / 858 |
| P08 | `Kit_Clipboard_Wall` | 1,800 / 600 / 60 | 1000 / 552 / - | 1000 / 552 |
| P09 | `Kit_StationCounter_Run120` | 3,500 / 1,200 / 150 | 1956 / 1156 / 940 | 1956 / 1156 |
| P10 | `Kit_StationCounter_Run075` | ~2,500 / 850 / 110 | 688 / 646 / 618 | 688 / 646 |
| P11 | `Kit_StationCounter_End` x 2 | 1,200 / 400 / 60 each | 500 / 470 / 450 | 500 / 470 |
| P12 | `Kit_SecurityLetters` | ~1,500 / 500 / 0 | 3940 / 3492 / 3492 | 3940 / 3492 |
| P13 | `Kit_TaskChair_Charcoal` | 4,484 / 1,792 / - | 4484 / 1792 / - | 4484 / 1792 |
| P14 | `Kit_JacketDraped` | 8,000 / 2,500 / 300 | 5952 / 1860 / 296 | 5952 / 1860 |
| P15 | `Kit_RegisterBook` | 2,500 / 800 / 80 | 1466 / 640 / 588 | 1466 / 640 |
| P16 | `Kit_TentCard` | 300 / 100 / 0 | 28 / - / - | 28 / 28 |
| P17 | `Kit_Clipboard` | 1,800 / 600 / 60 | 736 / 456 / - | 736 / 456 |
| P18 | `Kit_DeskBlotter` | 600 / 200 / 0 | 370 / - / - | 370 / 370 |
| P19 | `Kit_CoffeeCup` | 1,200 / 400 / 0 | 876 / 822 / - | 876 / 822 |
| P20 | `Kit_Ashtray` | 1,500 / 500 / 0 | 576 / 540 / - | 576 / 540 |
| P21 | `Kit_DeskPhone_Corded` | 966 + ~300 / 390 / - | 966 / 290 / - | 966 / 290 |
| P22 | `Kit_RadioCharger` | 2,000 / 700 / 80 | 872 / 508 / - | 872 / 508 |
| P23 | `Kit_NoticeBoard` | 1,500 / 500 / 60 | 868 / 308 / 300 | 868 / 308 |
| P25 | `Kit_TVCords` | ~400 / 150 / 0 | 2560 / 2032 / 2032 | 2560 / 2032 |
| P27 | `Kit_JackF` | ~2,700 / 1,000 / 56 | 3092 / 1008 / 56 | 3092 / 1008 |
| P28 | `Kit_HookHeavy` | 1,200 / 400 / 0 | 2220 / 1468 / - | 2220 / 1468 |
| P29 | `Kit_BinderPostOrders` | 360 | 370 / 346 / - | 370 / 346 |

Notes, all recorded as gaps in §5:
- **LODs drop sub-pixel parts only** (no collapse): the first build's collapse-decimated LODs folded the counter's and the
  cabinet's boxes into triangles in game (seen in capture v1), so every hard-surface kit now makes LOD1 / LOD2 by dropping
  parts under 0.05 m / 0.20 m. LOD1/LOD2 counts are therefore above the plan's LOD budgets; LOD0 is what the station budget counts.
- **Below the hero budget:** `Kit_CRTTV_Station` 7.2k (9k), `Kit_KeyCabinet_Putty` 2.9k (the plan quoted the old 24-hook 6.8k),
  the counter runs 2.0k / 0.7k (3.5k / 2.5k), the jacket 6.0k (8k). **Above it (LOD0):** `Kit_SecurityLetters` 3.9k (the SVG
  curves at resolution 10; 1.5k was an estimate), `Kit_TVCords` 2.6k (0.4k), `Kit_HookHeavy` 2.2k (1.2k), `Kit_JackF` 3.1k (2.7k),
  `Kit_KeySwitchPlate` 4.0k (3.5k), `Kit_TVWallBracket` 3.4k (3.0k), `Kit_FireAnnunciator` 6.4k (6.0k), `Kit_BinderPostOrders` 370
  (360). The station's total stays at 70,946 of 100,000 (§3 (b)).
- **`Kit_JacketDraped`** is a real Blender cloth bake over `Kit_TaskChair` (pinned at the shoulder line, 70 frames, then 5 mm
  thickness) plus one sleeve strip over the arm and a blank patch: a draped panel, not a tailored jacket (no collar, buttons
  or lapels). Below hero spec; open item.
- Kit previews (Cycles, 20 samples): `W/station_look_v1/kit_previews/*_a.png`, contact `W/station_look_v1/kit_contact_a.jpg`.

### 2.3 Surfaces (`Assets/Resources/Surfaces`, shader FrontRooms/Surface)

| Material | Values |
|---|---|
| `Station_Wall` | white base × **#9AA3A6**, `Office_Drywall_N` at 0.5, tile 1.2, smoothness 0.38, no mask; macro tone / dirt / grime **0** (PICK's clean eggshell, C1-13) |
| `Station_Floor` | `Run_VCT_A/N/S`, tint (0.832, 0.825, 0.816) sRGB = the linear multiply that brings the field's linear mean to **#B2AEA3** (PICK's (0.66, 0.65, 0.63) linear); smoothness scale 1.18 (mean 0.62); clean |
| `Station_FloorBorder` | the same maps tinted to the mean **#3A3A38**, mesh UVs |
| `Station_Ceiling` | **byte copy of `L0_Ceiling`** with the name changed (so the shell's ceiling takes the Station mask, C2-3c). **Not in the main package:** it ships with `station_look_v1_urp.sh` (§5 gap 1) |
| `Station_CeilingBand` | #D9D6CC, smoothness 0.15 |
| `Station_CrashRail` | #3E4650, `Prop_Vinyl_N` 0.6, smoothness 0.5 |
| `Station_Base` | #1A1918, `Prop_Vinyl_N` 0.6, smoothness 0.5 |
| `Station_StainlessGuard` | #A8A9A4, metallic 1, smoothness 0.68, `Prop_Aluminium_N` |
| `Prop_LaminateGrey` | #8E8E88, smoothness 0.5 |
| `Prop_SteelBeige` | `Prop_SteelPutty_N/_S` × `Prop_PlasticBeige_A`'s linear mean (#D3C9AE) |
| `Prop_JewelGreen` | base (0.06, 0.32, 0.08), emission (0.42, 1.95, 0.30) (green, a little warm), smoothness 0.85 |
| `Prop_AnnunciatorLabel` | #C9C3B2, smoothness 0.3 |
| `Prop_FabricUniform` | #59647A with `Prop_FabricCharcoal_N/_S` |
| `Prop_Cork` | #9C7A55, `Prop_FabricCharcoal_N` 0.4, smoothness 0.1 |
| `Prop_StationKeyPlate` | #2B2925 blank engraved plate (PICK's sign colour), until 平面视觉's art |
| `Prop_StationPaper` | copy of `Prop_Paper` (blank), until 平面视觉's atlas |
| `Prop_PlateBusinessHours` | 信息设计's `business_hours_albedo.png` **byte-identical** (`Prop_PlateBusinessHours_A`), a normal map from its height (255 = 3.2 mm), a mask from its letter mask (smoothness 0.45 cap / 0.25 letters) |

Textures: the 3 plate maps, and `Prop_EvacPlan_A/E_Room` + `_A/E_RoomPhase0` packed by main's `Tools/lookdev/pack_evac_plan.py`
from 平面视觉's 17:54 re-render with this plan's counter: `sheetA_room` (print md5 e11ba9c8124b9078515cf50598f777c9, glow 98c998a0,
examine.json 513f180f) and `sheetA_room_phase0` (print 316ffef348d87963e0f13558cef2895c, glow c4a1091d, examine.json b39cd67f),
4096 x 2048. Re-checked on account A at 18:4x: a fresh `pack_evac_plan.py` run on main's current prints gives the payload's four
PNGs byte for byte (A_Room 220fcf24, E_Room 385f5c3d, A_RoomPhase0 00d5ec22, E_RoomPhase0 f74afc96).

### 2.4 Code (`Assets/Scripts/Rendering/`, plus two stacked files)

- **`FrontRoomsStationDress.cs`** (new): hooked at `MapRunStarted` / `FloorChanged` (subscribed directly). Only with
  `map.HasStation`. Builds the trim (both phases' room side under the liner / the static entrance, the openings under the
  dress), dresses the static entrance (pulls, kick plates, jambs and head deactivated), mounts the plate twice (liner, static
  entrance) at `PlateAnchor`, re-seats the set on the bracket, attaches the picture through a scaled screen mount
  (`Attach(..., Beacon)`), adds it to the set's LODGroup, makes the set's light, powers the set off for good at the take,
  sets the rendering-layer masks (shell on `StationShellBuilt` and on the chunks built before the run started; the set's
  renderers per frame; anything parented under the station; the key; the station lamps), and mirrors `map.Station.Visible`.
- **`FrontRoomsStationTrim.cs`** (new): the procedural trim (crash rail with 10 mm bevels, base, border course, cased frames on
  both faces with throat liners, stainless guards, throat VCT, reducers, gypsum bands), one mesh per material and mask, no colliders.
- **`FrontRoomsStationTvLight.cs`** (new): the V3 r7 spot (100°/160°, range 9 m, the up-clip cookie, I = 6√(Y/0.14) clamped
  4.86-8.4 × 0.71/Y(colour), 60 % white blend, τ 0.12 / 0.5 s), driven by the baked track indexed by the broadcast clock
  (`FrontRoomsScreenSound.LoopTime`, else the bank's VideoPlayer time), × `StartLampsNorth`, gated within 24 m, Soft shadows on
  desktop with its own bias (§5 gap 5) / none on WebGL, light mask Station only, the take's fall.
- **`FrontRoomsPlacardMount.cs`** (edited): the station branch at the top of `Place` (anchor, 0.5 mm proud, no keep-clear,
  re-parented under `map.Station`, sheetA_room if `map.ExitLocked` else sheetA_room_phase0, the transcript and regions of that
  sheet); with no station the rest runs exactly as before.
- **`FrontRoomsPlacard.cs`** (edited): `Mount.Station` appended (label ST), `StationProud`.
- **On R3 + V5 (both in main):** `Media/FrontRoomsScreenVideo.cs` (`PowerOffForGood`, `IsOffForGood`, the latch; nothing else;
  3-way merged onto main's R3 + V5 file d0705a03 at 18:0x, no conflict) and `Rendering/Inspect/FrontRoomsInspectCatalog.cs` (the two station placard transcripts from `inspect_transcript.py --print`,
  their regions, and the unregistered station paper transcripts with 01 §6's strings).
- **Data / tools:** `Resources/Rendering/StationTvLightTrack.json` (160 samples, ad sha1 8bde2101), `Tools/lookdev/bake_tv_light_track.py`.
- **Clone-only, not landed:** `Assets/Editor/StationLook/FrontRoomsStationLookProof.cs` (the proof harness, ASCII, no tuples);
  `W/station_look_v1/tools/{make_station_surfaces.py, make_package.py, frame_images.py, run_when_free.sh, suite_chain*.sh}`
  (`suite_chain7.sh` = the account-A round); the silencer `Assets/Scripts/CloneOnly/FrontRoomsCloneOnlySilence.cs` (rule 21; every
  batch run here was silent). None of these is in the package.

## 3. Proofs

All in `W/proj_station_2`, Unity 6000.6.3f1, under load (other lines' Unity jobs ran beside these; 1-min load 20-66). **(a), (d)
and (e) were re-run on account A on main 18:43 + the payload (§0)**; (b) and (c) are the account-B captures on the same payload
(main c5e03368 + R3 + V5 then; nothing the station's look reads changed since, §5 gap 16). The captures' run seed is 516574485;
the suites use their own seeds. Raw frames, logs and measurements: `W/station_look_v1/shots_v6/`
(the final capture: the final payload, the desk fix, 平面视觉's 17:54 room sheets), earlier rounds `shots_v3/` to `shots_v5/`,
`W/station_look_v1/logs/`, `W/station_look_v1/suites/` (copies of every suite's JSON report).

### (a) The contract

| Check | Result |
|---|---|
| `Validate` + `ValidateStation` on `Station_FrontDesk_4x4` with every kit present | **0 errors, 0 warnings**, IsStation true, on **main 18:43's `FrontRoomsRoomModuleData` a70f8ed2 with 关卡设计's R3 rules** (`logs/r1_validate.log`, account A, 18:44; account B: `u10_validate2.log` 18:15 and `u1_validate.log`, the same): module props LOD0 63,184 / LOD1 31,184, key + placard kits 6,962, highest top 2.83 m, no pile block on the four kits that carry station names. Includes: KeySpot outside P03's body-only footprint (x 10.256-10.30 vs KeySpot 10.232); every `p.y + boundsMax.y` <= 2.85 (highest P02 2.83); TV x 9.04-9.66 (outside x 6-9) with p.y 2.33 >= 2.15; nothing over 1.5 m in x 6.9-8.1; **R3: screen centre 2.614 m <= 2.65, beacon 2.461 m, the push-in in front of the screen and of the beacon clear of collider props and of the riser footprint** (§5 gap 17) |
| `StartGroupTemplate.From(asset, KitFootprint)` | **OK** (depth 4, entrance 2, e2Row 1) |
| MapVerification, module **ON** (clone only; profile bbd2e73f = main's + the one line) | **PASS 100/100** seeds + EXIT planner 100/100 x 3 floors; **start group PASS 1600/1600** with "profile station module Assets/Levels/Modules/Station_FrontDesk_4x4.asset H4 E2r1 k4" (floor 1: 1600 EXITs at 8 walking cells, 0 widened, 0 through glass; plan ms mean / p95 / max 13.43 / 16.31 / 59.94); `map-verification-latest.json` = the map line's `verification_baseline.json` (`~/FrontRoomsMapWork/logs/a26/`) except `timestampUtc` (log `logs/r7_mv_on.log`, account A 18:55; account B `s1_mv_on.log`: the same) |
| MapVerification, module **OFF** (main's profile 07514c33) | **PASS 100/100** + EXIT planner 100/100 x 3 floors; **start group PASS 1600/1600** with the map's stand-in station (H4 E2r1 k4; plan ms 13.05 / 15.83 / 29.94). `map-verification-latest.json` = the baseline except `timestampUtc`; the start-group JSON = the ON run's in every field except the plan timings and = account B's OFF run (log `logs/r5_mv_off.log`) |
| Byte identity with the module off | The profile line is the only switch: the clone's profile during the OFF suites was main's file byte for byte (07514c33; main's + the one `stationModule` line = bbd2e73f for ON). Every new code path is behind `map.HasStation` (the dresser) or `map.FloorHasStation && map.Station != null && StationAnchor(Placard)` (the placard's branch); the ScreenVideo hunk only adds an uncalled method and a latch that stays false unless it is called; the catalog only adds dictionary entries nobody asks for. So with `stationModule` empty nothing the package adds runs: MapVerification OFF writes main's baseline, MIT OFF and the autopilot OFF pass as below |

### (b) Budget and light layers

Counted on the built set in play (`shots_v6/measure.txt`):
- **LOD0 triangles under `map.Station`** (the 29 props, the trim, the liner / riser / static entrance, the plate, the placard frame and
  lens, the TV and its picture, the key stand-in): **70,946** (cap 100,000).
- **Renderers:** 95 active under `map.Station` + 15 station-shell chunk renderers (Station_Wall / Station_Floor) = **110**; counting also the 6 ceiling renderers the map passes with the shell (they keep `L0_Ceiling` until the URP step brings `Station_Ceiling`) = **116** (cap 120). Every LOD renderer is counted, so the number is conservative. The shell's renderers are the map's
  split per material per block (the plan estimated 2-5).
- **Realtime lights in station cells:** 15 (12 Dim/Steady station lamps + the 2 entrance lamps + the set's light; the 2 Off cells have no fixture).
- **Masks set (inert: the URP flag is off):** set renderers Station only 103, both layers 5 (the opening trims), default 0; lights Station only 13 (12 station lamps + the set's light), both 2 (the entrance pair). The URP asset's `supportsLightLayers` is False in the clone, as in main.
- **Leak test L0, Play mode, desktop, masks inert** (on / off / on renders, mean luminance of the receiving region; bar 1 %):

| Pair | v3 | v5 | v6 | Frame drift on-on (v6) |
|---|---|---|---|---|
| OUT east: the set's light (forced to 8.4) on vs off, seen from the Level 0 cell east of the station, its east wall's outer face | +0.17 % | -0.02 % | +0.03 % | 0.10 % |
| OUT south: the same, through the south liner into the stream room behind D | -0.01 % | +0.10 % | +0.39 % | 0.34 % |
| IN east: eleven Level 0 lamps within 7 m east of the station on vs off, the station's east wall inner face | -0.65 % | -0.48 % | +0.28 % | 1.56 % |

  **No leak at the 1 % bar in Play mode**, in three captures (the set's light has its own soft shadows; the map lamps' leak, if any,
  sits inside the frame-to-frame drift). 61 §15 also asks for a **Mac build** test: **not run** (open item). Until then the URP flag
  is not needed and stays in the separate `station_look_v1_urp.sh`. Autopilot fps with layers on: not measured (the flag stayed off);
  with layers off, module on vs off: 58.4 / 58.4 fps, p99 18.27 / 18.29 ms (§3 (d)).

### (c) Images

Real game in the clone, module on, desktop 1920 x 1080 and phone 2532 x 1170 (phone = world view at the same vFOV 76, no handheld HUD:
**not compared with Figma 2528:5403**, which specifies the phone HUD; open item). Shot format (`W/tools/shotframe`); every row is in
`W/vl_pending/station_look_v1.md` with its verdict. VL numbers proposed only (VL421-428).

| Image (`W/station_look_v1/images/`) | What | Verdict |
|---|---|---|
| `slv1_01_from_start_door.jpg` | From the stream room as D opens: E1 + the locked EXIT ahead, the set right, the placard left | **PASS** |
| `slv1_02_at_threshold.jpg` | The room from D (walls, rail, VCT, border, counter) | **PASS** |
| `slv1_03_desk_register.jpg` | Desk + register + SECURITY from the visitor side (paper blank until the atlas) | **PASS** |
| `slv1_04_tv_playing.jpg` | The set playing on its bracket, its light along the axis | **PASS** |
| `slv1_04b_tv_light_pair.jpg` | The set's light on / off | **INFO** |
| `slv1_05_riser_face.jpg` | The riser face: key on its plate, annunciator, EMT (with the final shadow bias) | **PASS** |
| `slv1_05b_shadow_bias_sweep.jpg` | The shadow bias sweep on the riser face | **INFO** |
| `slv1_06_placard.jpg` | The placard on its anchor, sheetA_room (stair lock live) | **PASS** |
| `slv1_06c_placard_ingame_check.jpg` | ingame_check.py on the placard in the plain view | **INFO** |
| `slv1_06d_placard_examine_check.jpg` | ingame_check.py on the placard's EXAMINE close-up and zoom (InspectPlaytest, module ON): caps 12.57 / 12.5 px, bar 12 | **PASS (caps)** |
| `slv1_07_entrance_plate_start.jpg` | The locked entrance (D) and the BUSINESS HOURS plate | **PASS** |
| `slv1_13_entrance_plate_cleared.jpg` | The static entrance after ClearStartArea (render toggle) and the plate | **INFO** |
| `slv1_08_e1_exit.jpg` | E1 and the EXIT | **PASS** |
| `slv1_09_e2.jpg` | E2 | **PASS** |
| `slv1_10_key_at_keyspot.jpg` | The key at the KeySpot from the take point (D1.6 stand-in) | **PASS** |
| `slv1_take_sequence.jpg` | The take, 7 frames, worst card | **PASS** |
| `slv1_11_pick_side_by_side.jpg` | Side by side with PICK at its two cameras | **INFO** |

Measured on the frames (`shots_v6/log.txt`, `frames.txt`, `measure.txt`):
- **The set:** set + bracket rendered bounds min y **2.164**, max y **2.83** (bars >= 2.15, <= 2.85); screen centre (9.35, 2.467, 11.334)
  station-local (the beacon marker 9.35 / 2.461 / 11.316); SoundRole **Beacon**; "the playing station TV" true (R3); the picture in
  **3 of 3** of the set's LOD levels.
- **G4 from the frames' geometry:** key (10.23, 1.42, 10.40) to the screen centre: 43.9° off the screen axis in plan, 42° off the
  tilted normal, 1.64 m in 3-D.
- **Check 2, the set's share of the board's light, per card** (light on vs off, the board region round the key plate; the clock put on
  each card through the editor hook): card 1 (lavender) 78.2 %, card 2 (mauve-pink, brightest) 85.0 %, card 3 (navy + yellow) 77.0 %, card 4 (lavender) 78.1 %, card 5 (WTCZ blue, the darkest) **75.4 %** (board Y on 0.086 vs off 0.021): **PASS, > 50 % on every card**; the worst is card 5. (Capture v5, before the bias fix: 54.9-69.1 %; the acne darkened the board.) "E ≥ 0.3" is 30L's Blender illuminance; the game gives displayed luminance only.
- **The shadow bias sweep** (`slv1_05b_shadow_bias_sweep.jpg`, card 1): board Y with the pipeline's 0.6 / 0.6: 0.089, blotches beside the conduit; **the code default 1.5 / 1.0: 0.097, clean**; 2.5 / 1.5: 0.101, a faint smear; 1.5 / 2.0: 0.087, a large smear (normal bias too high); hard shadows: 0.097, dotted acne. The default stays.
- **The take (G5, 50 §6; bar: the board's displayed drop >= 70 % on the worst card, the jewel lit):** on card 5 (the lowest share, 75.4 %): board Y 0.086 → 0.039 (55 %) at 0.14 s → 0.026 (69.5 %) at 0.28 s → 0.023 (73.6 %) at 0.42 s → 0.021 (**75.3 %**) at 0.68 s → 75.5 % at 1.0 s: **PASS (>= 70 %)**. The set's light 7.28 → 0.08 at 0.55 s, off at 0.60 s; the green jewel Y 0.022 in all 7 frames. (Capture v5, before the bias fix: 57 % on card 3, a FAIL; gap 5.) The set:
  TakeStationKey true, powered false, **off for good** true.
- **V4 desk stills:** floor 1 has 8 maze desk CRTs, **8 still**; 1 playing station TV; no other screen.
- **TV colour on the blue-grey wall, every card** (the r7 light colour from the baked track × #9AA3A6; 61 §5 risk): cards 1 and 4
  (lavender) tint the wall to hue 231° at saturation 0.23; card 2 (mauve-pink, brightest) 214° / 0.13; card 3 (navy + yellow) warm,
  102° / 0.05; card 5 (WTCZ blue) 225° / 0.45. The wall alone is 195° / 0.06. So the wash reads blue-violet, never purple (no card past
  231°), strongest and bluest on the WTCZ card. The light colours the game set per card (v6): #ECE5FF, #FEF9FF, #FFF6EB, #E9E2FF, #A2B4FF for cards 1-5. Analytic from the track
  the game uses; per-card frames `shots_v6/share_card1-5.png`.
- **Placard and sign contrast on #9AA3A6:** the placard from 2.3 m: placard Y 0.470 vs the wall beside it 0.088 (Weber
  **4.3**; PICK's 0.51 was a blockout placard in shadow; here the Steady placard lamp lights it, NP point 5). The BUSINESS HOURS
  plate Y 0.154 on the liner (3.6 m); SECURITY letters Y 0.043 on the fascia 0.056 (the L* 75 on L* 59 contrast 61 §8 warns of:
  low from D, as predicted; 信息设计's check).
- **`ingame_check.py`** (平面视觉) on the placard at 1.0 m (`slv1_06c_placard_ingame_check.jpg`, reference = sheetA_room's lit preview,
  17:54): the reference sheet passes (alarm block 804 px at 3.0 px/mm, caps 15 px against the 12 px bar, white on red 5.26); in the plain view at vFOV 76 from 1.0 m the alarm block is 190 px wide (0.71 px/mm), so the 5.0 mm caps are 3.5 px. The bar (12 px caps) is the EXAMINE close-up's, and the dark check against Q16 1.30 needs the blackout state:
  the EXAMINE frames were then taken from main's InspectPlaytest (module ON, 1080p): the close-up (d 0.368 m, FOV 61) gives the
  5.0 mm caps **12.57 px** and the zoom **12.5 px**, against the 12 px bar: **PASS** (`slv1_06d_placard_examine_check.jpg`).
  White on red is not measurable in the game frames (no pixel passes the tool's white threshold under the game's grade). The
  glow-ink dark check against Q16 1.30 on the station sheet was **not run** (open item 5).
- **Pixels against §4** (frame 02 from D's threshold): wall #868673, rail #333425, VCT #91896D, border #4A452E, frame E1 #52533C, under the Level 0 lamps (warm) and the game's grade; PICK's blockout
  rendered the same albedos cooler (Blender Eevee, AgX −0.5 EV). The albedos are the decided values (§2.3); the difference is the
  game's light and grade.
- **Seams:** no z-fight or paper end cap inside the station in any frame; the border course (0.305 m, #3A3A38 VCT) reads round the
  NW corner (`shots_v6/02_02b_debug_border_corner_desktop.png`); from D at grazing angles it is subtle.

### (d) Main's suites in the clone

Account A, 18:44-19:05, the clone = main 18:43 + the payload (`tools/suite_chain7.sh`; logs `logs/r2-r10_*.log`; JSON `suites/r*_*.json`).
No fix round was needed.

| Suite | Module ON (clone only; profile bbd2e73f) | Module OFF (main's profile bytes 07514c33) |
|---|---|---|
| FrontRoomsMapVerification | PASS 100/100 + start group 1600/1600 (our asset); JSON = baseline (`r7`) | PASS 100/100 + start group 1600/1600 (stand-in); JSON = baseline (`r5`) |
| FrontRoomsMapInteractionTests (main's 5b1b9094, 359 checks) | **358 / 359** (`logs/r8_mit_on.log`): the one failure is the landing assertion "start group plan: the profile ... has no station module", by design (contract C-8); every "start group world" check ok (the shell, the skin, StationShellBuilt, the queries). Account B on the older MIT: 349 / 350, the same one failure; with `Station_Ceiling` in the clone (`s3`): 2 more, "start group world" (gap 1) | **PASS 359 / 359** (`logs/r2_mit_off.log`; account B: 350 / 350) |
| `-autopilotExit -autopilotSeed 2554 -autopilotStation 4` | **PASS · ended by exits**, **15 / 15** station checks ok (29 props placed, 0 left out; M5's route to the TV's cell, BeaconPath from D 10.2 m; the EXIT in view; TV 11.5 m ahead, 9.3° across, placard 12.1 m; the door shut with the player inside; the key taken by E at the KeySpot (10.23, 10.40); the KP-1b panel 0.02 s after E; M5's flip to N at the take; M7's send; M8; floors 2-4 Q1 = B), 0 errors; 58.4 fps, p95 16.82 ms, p99 20.58 ms (`logs/r9_autopilot_on.log`, `suites/r9_on_autopilot_report.json`). In the log: `[StationDress] dressed the station ... 11 trim renderers, 17 own renderers`, `[Placard] mount ST ... sheet sheetA_room`, `set re-seated on Kit_TVWallBracket, attached (Beacon), light on; screen centre (…, 2.467, …)`. Account B: 12 / 12 (the checks before M5 / Q1 B), 58.4 fps, p99 19.53 ms | **PASS · ended by exits**, 15 / 15 station checks ok (the map's stand-in: 4 props, KeySpot (9.70, 10.40)), 0 errors; 58.3 fps, p95 16.82 ms, p99 19.24 ms (`logs/r4_autopilot_off.log`). (With `-autopilotStation` the map forces its stand-in station even with the profile empty, so the dresser runs on the stand-in here and logs "Kit_CRTTV_Station was not placed: no station TV"; with the profile empty and no test flag `map.HasStation` is false and nothing of ours runs.) |
| FrontRoomsInspectPlaytest (counts) | **PASS 134 / 0**, 35 numbers; the placard on the station mount ("placard mount ST side -1, glow cell (84, -55)"), the EXAMINE close-up at d 0.368 m, FOV 61, 2.49 px/mm, 5.0 mm alarm lines 12.5 px (`logs/r10_inspect_on.log`, `suites/r10_on_inspect_playtest.json`; account B 18:24 the same, `s14`) | **PASS 134 / 0**, 35 numbers; the placard on today's P1 mount ("placard mount P1 side -1, glow cell (276, 6)") (`logs/r3_inspect_off.log`, `suites/r3_off_inspect_playtest.json`) |

The non-blocking `ArgumentOutOfRangeException` in the ON autopilot log is Unity's QuickSearch indexer at editor start
(`SearchDatabase.cs:351`), not game code. Every batch run was silent (`[CloneOnlySilence] FMOD output COREAUDIO -> NOSOUND: OK`).

**Main moved again during this round** (19:00-19:03: HUD O-39 KeyHud 88cc6849, 关卡设计's Relay v2 V0, inert: Game 9c4da7c9,
MapWorld 0b406078, MIT ed0c14c2; and A13 18:54). None is a package path; the dry run and Roslyn (§3 (e)) were re-run on main 19:05-19:06,
and the clone was synced to main 19:06 afterwards for the next stages (not re-run in Unity: rule 23, the inert v2 contracts and A13
share no file with this package).

### (e) Roslyn

**Account A, 18:45, main as it is now:** `W/tools/roslyn_check.py --tree W/station_look_v1/rc_tree --all` (and `--ios`), the tree =
main's `Assets` *.cs / asmdef / asmref / dll / rsp (iCloud duplicates excluded) + Packages + ProjectSettings, read 18:45 (Game
527ce171, MapWorld 5c57cfa5, KeyHud 60f28e7f, ScreenVideo d0705a03), overlaid with the package's 7 C# files (PlacardMount 220d9f86,
StationDress 65cc6882, StationTrim 7a4feb05, StationTvLight 5a7e0be1, Placard 739c918d, ScreenVideo 96976b9b, InspectCatalog
369eaaa7): **COMPILE OK** (FMODUnity 36, FMODUnityEditor 31, FMODUnityHaptics 1, FMODUnityResonance 2, FMODUnityResonanceEditor 2,
Assembly-CSharp 128, Assembly-CSharp-Editor 69 files, 0 errors) and **COMPILE OK (iOS defines)**, 0 errors. Out dirs
`W/station_look_v1/roslyn/final4_main1843_plus_pkg[--ios]`. (Account B, 18:17, on main as it was then: the same, `final3_*`.)
**Again at 19:06 on main as it is now** (Game 9c4da7c9, MapWorld 0b406078, KeyHud 88cc6849, A13's `Rendering/Exit/*` in, ScreenVideo
d0705a03 before the overlay): **COMPILE OK** macOS (Assembly-CSharp 130 files, Assembly-CSharp-Editor 69, 0 errors) and **COMPILE OK
(iOS defines)**, 0 errors; `W/station_look_v1/roslyn/final5_main1905_plus_pkg[--ios]`. The
clone's own Unity compile of main 18:43 + the payload: 0 errors (`logs/r1_validate.log`, script compilation 5.3 s).

## 4. Package

`W/apply/station_look_v1.sh` (convention v2, `hud_key_b.sh`'s format: `station_look_v1_payload/`, `_base/` with exact copies of
the 4 replaced files, `_bases.txt`, `_expected.txt`, `_keep.txt`, `_stacked.txt`, `station_look_v1.diff`; dry run by default,
`--apply`; the header lists every path). Payload: **193 files**: 189 new (26 kits x FBX / JSON / 2 metas, 16 materials + metas, 7 textures + metas, 3 scripts + metas, the module + meta, the light track + meta, 28 kit modules, the bake tool) and 4 replaced (PlacardMount, Placard, ScreenVideo, InspectCatalog; their .meta kept). Dry run against main at 18:17 (account B) and again 19:05, account A (account A, main as it is now): **DRY RUN OK** — section 0 "in main"
for R3's and V5's three files (ScreenVideo d0705a03 = R3 + V5, InspectCatalog d832a286, StationPresence b6353ae8), all 193 bases
match (189 new paths absent, 4 replaced files at their sha1), the 4 kept .meta unchanged, every MapWorld / StationSet API it binds
to present, all 193 payload files match `_expected.txt`, 80 new GUIDs unused in Assets/ and unique. Not applied; LANDINGS /
MERGED untouched. `W/apply/station_look_v1_urp.sh` (the URP flag + `Station_Ceiling`): dry run OK, for the parent's approval only.

**Rebase during the build.** V5 picture sync (`W/apply/v5_picture_sync.sh`) landed in main at 17:43 on the same
`FrontRoomsScreenVideo.cs`. Our hunk was 3-way merged onto main's new file (`git merge-file`, base R3 4eb52b27, no conflict; the V5
hunks untouched; result 96976b9b), put in the clone before capture v6, compiled by Roslyn with main (§3 (e)). The package
builder now takes every base from main; the R3 "stacked" mode is gone (both are in main).

## 5. Gaps and choices made at the build (the plan's GAP 1-44 stand; these are the build's own)

1. **`Station_Ceiling` moved to the layers step.** C2-3c made it an identical copy of `L0_Ceiling`. With it present, main's
   `FrontRoomsMapInteractionTests` "start group world" failed twice ("StationShellBuilt … 6 not in the station's materials": the
   map passes the ceiling renderers, as its own rule says, and the test only knows the wall and floor). With the URP flag off it
   changes nothing visible, so it ships with `station_look_v1_urp.sh` and contract row C-8, not in the main package.
2. **LODs drop sub-pixel parts only.** Collapse decimation folded the counter's and the cabinet's boxes into triangles in game
   (capture v1). LOD1/LOD2 now remove parts under 0.05 m / 0.20 m; their counts sit above the plan's LOD budgets (LOD0 is the
   station budget).
3. **The TV's knobs stand 12.6 mm past `Kit_CRTTV`'s front bound** (max z 0.2601 vs 0.2475). C2-9 allows "or raise the set's
   module y": the hinge is at 2.36 and the measured lowest point of set + bracket is 2.164. The footprint (collider) is identical.
4. **The set's light follows the broadcast clock** (`FrontRoomsScreenSound.LoopTime`, V5, which landed in main during the
   build), else the ad player's time. The ad player pauses while the set is out of view, so without the audio clock the light
   would freeze on one card while the player looks at the key (30L §3.1 asked that the set not pause within 18 m). In a clone
   without FMOD and in batch mode the ad does not advance, so the per-card proofs set the clock through an editor-only hook
   (`FrontRoomsStationTvLight.ToolsTime`, `#if UNITY_EDITOR`).
5. **The set's light has its own shadow bias** (bias 0.12, normal bias 1.2, strength 0.85, `UniversalAdditionalLightData`
   `usePipelineSettings` off): with the pipeline's bias a 160° spot left acne blotches on the riser face beside the key plate
   (captures v1-v4). The first try (v5) set depth 0.12 / normal 1.2, read as the built-in pipeline's units; URP reads both in shadow-map texels (its own are 0.6 / 0.6), so v5 had **more** acne: a dark wedge across the riser face, the board's share down to 55-69 % and the take's drop to 57 %. Capture v6 swept five settings (`slv1_05b_shadow_bias_sweep.jpg`): **depth 1.5 / normal 1.0** (constants `ShadowDepthBias` / `ShadowNormalBias`) is the clean one; the share went back to 75-85 % and the take to 75 %.
6. **M3's floor key is the map's key cube** (`NewKey`, keyGlow, 0.32 x 0.12 x 0.12 m, "Key · floor", 6 cm over the KeySpot): M3
   landed in main during this build with the D1.6 model still to come "through NewKey". The cube is emissive and covers the key
   plate, so the frames hide it and show the decided look (D1.6's ring, nickel key and white tag at 1.5x on the one hook,
   shadows off as for any prop under 0.3 m) as a stand-in. The dresser recognises both (`FloorKeyName`, `Kit_Key*`) for the
   Station bit. Contract row C-2.
7. **The harness completes the lamps' rise north of D** (`StartLampsNorth = 1` after the door opened): in batch mode the swing's
   rise did not run; the game sets 1 at the shut anyway. Every station frame is the post-swing state.
8. **The static-entrance frame (`slv1_13_…`) is a render toggle** (`StartAreaCleared()` alone): the map builds D's data wall only on
   its next chunk build, which is never seen, so the stream facade's paper shows behind the leaves in that one frame.
9. **The placard's EXAMINE transcript** is the station sheet's (`placard_station_room[_phase0]`, from `inspect_transcript.py
   --print`); the station paper transcripts (DAR with `{DAR_DATE}`, sign-out header, register header, key plate) are in the catalog
   but **not registered** by any kit: the paper is blank until 平面视觉's atlas, and a transcript must match the print.
10. **The plate's blank-art placeholders.** `Prop_StationPaper` is a copy of `Prop_Paper` (blank), `Prop_StationKeyPlate` is
    #2B2925 (PICK's sign colour), the annunciator's label strips are blank #C9C3B2: no invented type anywhere. The paper atlas
    cells are fixed in `_station_common.PAPER_CELLS` (4 x 4: register left/right, tent card, DAR, sign-out, KEY CONTROL, phone list,
    notices a-d, POST ORDERS spine, blank sheet).
11. **The jacket** is a cloth-baked draped panel with one sleeve (no collar or buttons), 5,952 tris: below hero spec.
12. **Renderer count 116 / 120** is tight; any new prop row needs a renderer check.
13. **VL numbers:** the images say "VL (to assign; VL421-428 proposed)"; the parent decides and places (plan GAP 36).
14. **Three desk items moved, forced by the built kits:** at the plan's positions the binder (P29, 9.02) passes through the west end
    panel (its bounds reach x 8.87, the panel's inner face is 8.925), the DAR clipboard (P17, 9.25 / 7.55) overlaps the binder, and
    the radio charger (P22, 10.74) passes through the east end panel (10.843 vs 10.825). Smallest moves: binder 9.08, clipboard
    9.37 / 7.56, radio 10.72. Everything else on the desk stands where the plan put it; the coffee cup sits **on** the blotter as
    decided, at y 0.7505 (the blotter's top is 10.5 mm, not the 5 mm the plan's 0.745 assumed).
15. **Kits over their LOD0 line** (§2.2): the key plate, the bracket, the annunciator, the letters, the cords, the hook, the F jack
    and the binder. The station's LOD0 total is 70,946 of 100,000.
16. **The clone's base.** On account B the clone was main c5e03368 + the files the build touched (re-synced 17:2x) + R3 + V5 + this
    package; the captures (§3 (b), (c)) were made on that base. On account A it was re-synced to **main 18:43 + the payload,
    byte for byte** (§0), and the gate suites (§3 (a), (d)) and Roslyn (§3 (e)) were re-run there. Main moved under the build
    only in files the package does not touch; none of them changes the station's look (lighting, materials, the set piece):
    M5 / R3 layout rules, Q1 = B zone keys, the S2 Beacon sound fix, the phone HUD zone name, HUD O-30, settings strings.
17. **关卡设计's R3 layout rules (17:33, `ValidateStation`) pass on this module unchanged:** the set's screen centre from
    `Kit_CRTTV_Station`'s `screen` anchor is 2.33 + 0.284 = **2.614 m** (bar <= 2.65); the StationBeacon marker 2.461 m; the push-in
    strip (+-0.5 m of the screen axis, out to 1.3 m: x 8.85-9.85, z 10.09-11.39) holds no prop with a collider (the counter, the
    chair and the end panels are south of it; the riser block starts at x 10.30) and misses the riser footprint. The wayfinding's
    `StationBeaconCell` reads cell (3, 3), edge 1 (north wall, 0.68 m from the cell line, yaw square). The runtime pose differs
    from the module data only by the bracket's 20° tilt (screen centre 2.467 m measured, still <= 2.65).

## 6. Open items

| # | Owner | Item |
|---|---|---|
| 1 | 平面视觉 | The `Prop_StationPaper` atlas (register page with the blank last line, PLEASE SIGN IN, the DAR with the computed date, the sign-out sheet, KEY CONTROL / ALL KEYS MUST BE SIGNED OUT, the phone list with no date, notices, POST ORDERS), the STAIR KEY plate art (`Prop_StationKeyPlate`), the annunciator's label words, the STAIR A tag lettering and colour. **The group should not land with a blank register and tent card unless the parent accepts it** (plan GAP 27) |
| 2 | 平面视觉 | Room-sheet geometry: 平面视觉 took the desk's east end 10.85 and the riser [10.3, 9.9, 12, 12] into `sheetA_room[_phase0]` at 17:53 (geometry.json cites this plan); this package's `Prop_EvacPlan_A_Room[Phase0]` are re-packed from those 17:54 prints (glow maps unchanged; transcripts and regions unchanged, examine md5 513f180f / b39cd67f). **Still open:** E2 is built 1.20 m clear, drawn 1.25. If the sheets change again, re-run `pack_evac_plan.py` |
| 3 | 关卡设计 | The switch-on line and C-1 to C-8 (`station_look_v1_contract/README.md`): the take point (9.73, 10.40); M3's key model (D1.6 assembly on the host's `key_hook`, parented under `map.Station`); M6's take → `PowerOffForGood`; the scene directional and Relay bits (only with layers); docs; the reflection hook; MIT's two checks |
| 4 | parent / Red | The URP light layers: no leak in Play mode; **the Mac build leak test is not run**. `station_look_v1_urp.sh` waits for an explicit approval and that test |
| 5 | 游戏视觉 (next stage) + 平面视觉 | The placard's EXAMINE close-up **is done** (`slv1_06d_placard_examine_check.jpg`: caps 12.57 / 12.5 px against the 12 px bar, PASS; white on red not measurable in the game frames). **Still open:** the glow-ink dark check against Q16 1.30 on the station sheet (the Phase 0 harness `FrontRoomsPlacardARender` measures the start placard only; the station sheet's glow mask is 平面视觉's 98c998a0, unchanged since 15:13), and the paper's EXAMINE frames once the atlas exists |
| 6 | 游戏视觉 | Phone frames with the handheld HUD, compared with Figma 2528:5403 (these are world views) |
| 7 | 游戏视觉 | The station reflection cube (`Refl_Station`, `ReflectionZone.Station`) was not built (inert until C-7 anyway) |
| 8 | 游戏视觉 | Hero detail below budget: the jacket (tailored), the CRT (7.2k of 9k), the cabinet, the counter runs |
| 9 | 游戏视觉 | Autopilot fps / p99 with the layers on (only with the URP step). (InspectPlaytest ON and OFF: done, §3 (d).) |
| 10 | 声音设计 | The station TV is attached with Beacon (their rule); the light now reads their `LoopTime` (no change asked) |
| 11 | parent | Add `W/proj_station_2` to the cross-account ledger's clones (rule 19); the clone is main 19:06 + the payload with the profile left **ON** (bbd2e73f) for the next stages (not opened in Unity since the 19:06 sync). VL numbers (VL421-428 proposed) to confirm against VERIFICATION_LOG and place in Figma |
| 12 | parent / Red | `W/apply/station_look_v1.sh --apply` together with 关卡设计's switch-on and M3 / M6 / M7 (contract §2). Not applied here |

## 7. For Red (RED_ACTIONS candidates; the parent decides)

1. **The URP light layers** (`W/apply/station_look_v1_urp.sh`): Play mode shows no leak at the 1 % bar, so on today's evidence they
   are not needed; 61 §15 also asks for a Mac build test, and a build needs your yes. Approve the build test (or the step itself).
2. **A blank register and tent card at the switch-on:** 平面视觉's station paper atlas is not made yet (open item 1). Land the
   group with blank paper, or hold the switch-on for the atlas.
3. **The floor key's look at the switch-on:** the map's emissive key cube until D1.6's model reaches `NewKey` (contract, "Known
   look gap"). Land with the cube, or hold for D1.6.

## 8. Apply

Only together with 关卡设计's switch-on (contract §2, §6), by the 游戏视觉 session:

```
bash /Users/redwang/FrontRoomsVisualWork/apply/station_look_v1.sh            # dry run (DRY RUN OK at 19:05)
bash /Users/redwang/FrontRoomsVisualWork/apply/station_look_v1.sh --apply    # then Roslyn mac + iOS on main
```

