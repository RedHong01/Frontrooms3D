# Live clocks: inventory and design

**Status:** design v1, 2026-10-22 14:4x. Written by 游戏视觉 (session `local_a9a1dce2…`, account B), workflow `live-clocks` (wf_8fc13519-635), stage 1 (inventory + design). It is read-only work: no code, Unity or kit runs. The clone `W/proj_clock` is not made yet.
**Read with:** `NOTE_1022_READ_FIRST.md` in this folder, which sets the ownership and the peers' formats. This design follows it.

**Red's request** (2026-10-22 14:1x, 游戏视觉 chat, with a Play-mode screenshot of the wall clock in a Level 0 room):
> 目前我不确定这个钟表是否是旧的钟表，如果是旧的我希望新的钟表是可以正常走动，并且呼应现实世界的时间的（钟表只是一点，还有游戏里面的电脑等一系列有时间显示相关的设备的）

In English: "I'm not sure whether this is the old clock. If it is, I want the new one to run normally and match real-world time. The clock is only one part: the same goes for the computers and the other devices in the game that show time."

## 0. Summary

- **It is the old clock.** `Kit_WallClock` was built on 10-02 (`Tools/Blender/frontrooms_kit/assets/wall_clock.py`, 538 tris, one mesh, no LOD). Its hands are baked into the mesh at the catalogue time 10:08:37, and it has no pivots. It predates the hero-LOD0 order.
- **The new clock:**
  - a hero rebuild under the same name and GUIDs;
  - three separate hand kits on pivots;
  - a quartz second hand that steps once a second with a tiny settle;
  - minute and hour hands geared to it;
  - LOD0/1/2, with the hands gone at LOD2.
- **One time source for the whole game:** `FrontRoomsRealTime`.
  - It reads the device's local time (the browser's zone on WebGL, via a small `.jslib`).
  - It runs on real time, so the clock keeps going during pause and EXAMINE.
  - It gives the screens the strings the peers asked for: `TimeText` `2:25 PM`, `DateText` `MON 10/22/90`, `DateDashText` `10-22-90` and `NightShiftDateText` `10-21-90`.
  - Any date uses the real month and day with the year 1990.
- **Only the wall clock is wired in this run.**
  - **Nothing else that shows time is built yet:** the terminal header, the TV off-air bars, the station's Daily Activity Report, the CCTV monitor and the time clock. For those, this design gives the contract.
  - **Devices that exist but show no time** keep doing so: desk CRTs (ad / still screen), the copier counter, the vending price, the PC case, the phone's dark display.
- **Gameplay is unchanged.** 系统设计 has already accepted that players can now count RELAX (RELAY §11, "Countable RELAX"). Nothing in gameplay reads this clock.
- **Nothing here needs Red to proceed.** §12 lists two optional points with their defaults.

## 1. Decisions this design builds on

These were made by the 游戏视觉 session within Red's request. They are restated here so the build stages do not reopen them.

| # | Decision |
|---|---|
| D1 | **Time source:** the player's real local time (device clock, local zone), read through one shared, visual-owned static service, `FrontRoomsRealTime`. It runs on real time, not `Time.time`, and allocates nothing per frame. |
| D2 | **Dates:** the real month and day, with the year 1990 and the weekday from the 1990 calendar. 叙事 added two rules: a real Feb 29 shows Feb 28 (`02-28-90`, a Wednesday), so the date never runs ahead; and every printed date is 1989 or earlier (`research/station/01_research.md` §5). |
| D3 | **The wall clock:** a new hero `Kit_WallClock` with the same name and GUIDs. It is the 1990 battery quartz institutional clock the script already describes: 3 hands, no numerals, bar markers. It hangs at 2.1 m, as placed. |
| D4 | **Hands:** separate hour, minute and second meshes on pivots at the dial centre, with anchors in the sidecar. The second hand steps once a second (a tiny settle, no sweep). The minute and hour hands follow the real time. |
| D5 | **Driver:** `FrontRoomsWallClock` on the kit's root. It ticks only when the clock is visible or within a few metres, and it stays cheap with 20 clocks in range. EXAMINE shows the live time. |
| D6 | **Other devices:** the terminal header shows live `h:MM AM`; the station's clock is the same kit. Desk screens and CRT ads show no time and are left alone. Anything else that shows time is listed in §8 for later. |
| D7 | **Gameplay:** not ours to decide. The RELAX note goes to 系统设计 as an FYI, and 系统设计 has answered (§9). |

## 2. Inventory: everything in the game that shows or implies time

"Today" means main at 2026-10-22 14:4x (read-only check of `Assets/`, the kit scripts and the docs). Frequencies come from the code and the inventories cited.

### 2.1 Built and in the game

| Device | Kit / code | Where it is placed, how often | What it shows today | This run |
|---|---|---|---|---|
| **Wall clock** | `Kit_WallClock` (`wall_clock.py`, 538 tris, 4 slots: PlasticBlack, PlasticWhite, PlasticRed, Glass) | **One per `L0_WaitingRoom_4x3` module**, at x 4.5, z 8.89, y 2.1, yaw 180 (`Assets/Levels/Modules/L0_WaitingRoom_4x3.asset`). Level 0 rooms that fit a 4×3 module roll `moduleChance` 0.3 (`FrontRoomsMap.cs:123, :436`), and this is Level 0's only module. The G10 glass census counted **×2 in one built map** (`research/glass/logs/g10_log.txt:167`). The Level Designer's waiting-room template places it the same way (`FrontRoomsLevelDesigner.cs:317`). | **Frozen at 10:08:37** (hands baked into the mesh) | **Replaced** (§4–§6) |
| Wall clock as an EXAMINE target | `FrontRoomsInspectCatalog.cs:62` fallback: flat, 0.32 × 0.32, dist 0.59, 40° up, `ship: false` | Test bed only: it registers with `FrontRoomsInspect.DevTargets` (Editor and dev builds). R6: "none of the SHOULD props ship in v1" (`research/inspect/10_design.md:1064`) | The frozen hands | Unchanged block; the close-up now shows live hands (§6.6) |
| Office desk CRT monitors | `Kit_CRTMonitor` + `FrontRoomsScreenVideo` (`FrontRoomsOfficeKit.cs:928`) | Office equipped stations | The ad loop (Ad_03, `StreamingAssets/FrontRooms_Ad_01.mp4`). The A16 rule (still, dim office screen, no text: `research/screens/30_screen_strings.md` §6, v2.1) is the target | **No time; leave** (D6) |
| CRT TV | `Kit_CRTTV` | Furniture piles (and the planned security-desk "beacon" TV, station S01) | Ad loop, or off | **No time; leave** (D6). The off-air state is in §2.2 |
| Desk phone | `Kit_DeskPhone` (`desk_phone.py`) | 45 % of equipped Office stations, pile piece (Small), station S18 | A **dark smoked display window** (`Prop_GlassCRT`); line lamps dark (station S18) | No time. Optional for later (§8, §12 O2) |
| Copier | `Kit_Copier` (`copier.py`, 平面视觉-owned for now) | Office dressing 60 % / 25 %, `Office_Bullpen_4x4` module | LCD copy counter `001` and a mechanical key counter | Not a clock; leave |
| Vending machine | `Kit_VendingMachine` | Office dressing 70 % / 30 % | Lit price readout `$0.65` | Not a clock; leave |
| Desktop PC | `Kit_PCDesktop` | Office stations | Off: the LED window is unlit (`pc_desktop.py:11`) | No display; leave |
| Caught card | `FrontRoomsTouchControlsView.cs:819` (`SURVIVED`) and the desktop equivalent | After a catch | Run length in game time | A game stat, not a device in the world; leave |
| Debug event log | `FrontRooms3DGame.cs:2680` (`DateTime.Now` in a CSV file name) | Dev only | Never shown | Leave |

### 2.2 Designed or planned, not built in main

| Device | Source | What it would show | Contract (§8) |
|---|---|---|---|
| **Terminal header** (the Office READ terminal, "Setup A" host station) | 系统设计's `research/computer_ui/20_design.md` §6.4 item 2, §8.1 (updated 10-22 14:26); 信息设计's G0 masters (Figma 3218:7277) | `program · user · h:MM AM`, no seconds, no blinking colon. Any date reads `MON 10/22/90` | `TimeText`, `DateText`, `MinuteChanged` |
| TV **off-air bars** (SMPTE bars with the time and date keyed in) | `SCREENS_VISUAL_SYSTEM.md` §2 (non-programme states); 叙事's `30_screen_strings.md` §4.1 (rewritten 10-22: live date and time; sign-off only between 02:00 and 06:00 local) | `<H:MM AM>` and `<MM-DD-90>` | `TimeText`, `DateDashText`. The picture is pre-rendered video, so it needs a runtime CG overlay |
| Station **Daily Activity Report** (EXAMINE text) | `research/station/01_research.md` §6 (叙事, 10-22) | Header `<MM-DD-90>  NIGHT  DESK`; the entries are fixed strings | `NightShiftDateText` and a `{NIGHT_SHIFT_DATE}` token |
| Station sign-out sheet, visitor register | station §6 | Column headers only; the rows are blank or fixed | None |
| `Kit_CCTVMonitor9` (station S33, P2, optional) | `research/station/30_look.md:90` | A still; a 1990 time-date generator stamp would be period-true | If it gets a stamp, then `TimeText` + `DateDashText` (or a seconds variant) |
| `Kit_TimeClock` + `Kit_TimeCardRack` (station S36, Direction C receiving desk, P2) | `30_look.md:93`; `10_directions.md:119-122` | A punch time clock: an analog face or a digit window | Analog: reuse `FrontRoomsWallClock` through the sidecar `clock` block. Digital: the service's fields |
| `Kit_WatchclockStation` (S35, P2) | `30_look.md:92` | **None.** It is a key box on a chain, and the watchclock itself "is gone" with the guard (S26, an empty hook) | — |
| `Kit_WallPhone` (S36) | `30_look.md:93` | None (a 1990 wall phone has no display) | — |
| VCR | none in the kit or the plans. Station research names a time-lapse VCR and bans `NO SIGNAL` (`station/02_refs.md:87`) | — | If a VCR is ever added, its clock reads the live time, never a blinking 12:00 |

### 2.3 Printed or written times and dates (not devices; owned by 叙事)

- **Placard title block:** `PRINTED 11/89` from sheet A on. The shipped v3 keeps `03/90` until sheet A replaces it after C22 (`research/narrative/40_floors_exit_tutorial_strings.md:98`).
- **A.11 ink initials:** `R.M. 6/90`, `D.K. 11/90` (`wallpaper_motion/30_narrative_phosphor.md` A.11, frozen). Under the live 1990 calendar these can lie in the future. 叙事 has already flagged `D.K. 11/90` (station §6).
- `ALARM LOG 03/90` was dropped in strings v2.1 (`30_screen_strings.md` §6).
- **Fiction that leaned on the stopped clock:**
  - "because time here is wrong" (`wallpaper_motion/30_narrative_phosphor.md:595`, `20_level_design_phosphor.md:867`);
  - "Time has stopped" (`research/inspect/01_inventory.md:187`, ours);
  - 系统设计's frozen-time rule (`research/computer_ui/10_research.md:556`, already marked superseded).
- **叙事's new time model** (station §5): "The building's clocks are right. Its hours are not."

## 3. The old clock, and what must not change

**Facts** (`wall_clock.py`; `Assets/Resources/Props/Models/Kit_WallClock.json`):
- 0.32 m across, 0.0495 m deep, origin at the back centre (the wall-nail point), dial facing −Y in Blender and +Z in Unity.
- Dial at 0.037 m, lens at 0.0465 m. Anchors `hang` (0, 0, 0) and `dial` (0, 0, 0.037), Unity frame.
- No collider; tags `office`, `wall_decor`; placement Wall.
- 538 tris, one mesh, no LODGroup (`codex_audit/…/kit_lod_dump_279c144.txt:109`).
- Hands drawn at 10:08:37 with the outlines in `wall_clock.py:84-90`.
- FBX GUID `0fc6c05e16a0446a9a060222d97f09cd`, JSON GUID `717c57084b255430cb28e48714989eb0`.

**What reads it (none of it may break):**

| Reader | What it relies on |
|---|---|
| `L0_WaitingRoom_4x3.asset` (map-owned) | The name `Kit_WallClock`, origin on the wall, y 2.1 |
| `FrontRoomsLevelDesigner.cs:317`, `FrontRoomsLevelDesignerTests.cs:204-207` ("hung kit goes up 2.1 m on its wall") | Placement Wall, bounds and footprint about the back-centre origin |
| `FrontRoomsInspectCatalog.cs:62`, `FrontRoomsInspectTests.cs:284` | The kit name, a 0.32 × 0.32 face, `ship: false` |
| `FrontRoomsGlassVerification.cs:602, :629` (prop look-dev, spawns at y 1.6) | The kit name; a `Prop_Glass` lens (glass census) |
| Station look-dev (`research/station/data/FrontRoomsStationLookdev.cs.txt:1676`, clone-only) | `WallKit("Kit_WallClock", …, 2.47f)`, over E1 |
| Codex-audit runtime signatures (W tools) | `R module props/Kit_WallClock|580/4/…`. This **changes** (tris, submeshes, children): a one-time re-baseline |

**Kept:**
- the name, both GUIDs and the origin convention;
- the outer envelope (Ø 0.320, depth 0.0495), so bounds, footprint and `minCeiling` are unchanged;
- `noCollider`, the `hang` and `dial` anchors, the `office` and `wall_decor` tags, and a `Prop_Glass` lens.

No module, map or Level Designer change is needed.

## 4. Kit spec: the new `Kit_WallClock` and its three hands

### 4.1 Reference object

A 1990 US institutional battery quartz wall clock, the classroom and office type that hangs over doors:
- a 12.5 in (0.32 m) deep black ABS case with a rounded front bezel;
- a white printed dial with bar hour markers, heavier at 12 / 3 / 6 / 9, and no numerals;
- black hour and minute hands (stamped aluminium, painted);
- a thin red second hand on a red centre cap;
- a flat glass lens;
- a single-cell quartz movement behind the dial.

This is the description already in `wall_clock.py:1-14`, which this design keeps.

There are no brands and no printed words on the dial. A small `QUARTZ` under 12 would be period-true, but it is a string and type, so it belongs to 叙事 and 平面视觉. It is not planned.

**Movement behaviour.** A quartz clock drives a Lavet-type single-phase stepping motor. One drive pulse per second turns the rotor 180°, which advances the second hand one tick. The pulse is about 8 ms in one patent design (US4351039) and about 47 ms in another (US6359840). The whole gear train steps with it, so the minute hand advances 0.1° and the hour hand 0.0083° at each tick. The overshoot and settle of the hand after a step is not documented in the sources found, so the settle in §6.3 is an **ESTIMATE**, tuned by eye in VL374.

### 4.2 Frames and anchors

**Body.** Blender as today: the origin is at the back centre, the back face is y = 0, and the dial faces −Y. The kit export maps it to Unity (−x, z, −y), so the front faces +Z.

**Hands.** Each hand is its own kit (the precedent is `Kit_DoorCloser_Arm`'s PART frame, `interact_door_closer_arm.py:12-18`):
- The origin is on the arbor axis, at the hand's back face.
- At zero rotation the hand points at 12 (Blender +Z, Unity +Y), and its front faces Unity +Z.
- It rotates about its local Unity **+Z**: `localRotation = Quaternion.Euler(0, 0, a)`, where `a` is degrees clockwise from 12 **as seen from the front**.
- Check: 3 o'clock is Unity −X, the viewer's right when facing the clock. Unity's Z rotation takes +Y to −X at +90°, and Blender +X maps to Unity −X; this matches `wall_clock.py:_hand`. Gate G10 proves it by rendering.

**Sidecar anchors** (Unity frame; the depths are the script's hand stack, under the lens at 0.0465 with ≥ 1.5 mm clearance):

| Anchor | Pos (m) | What sits there |
|---|---|---|
| `hang` | (0, 0, 0) | the wall nail (unchanged) |
| `dial` | (0, 0, 0.0370) | the dial face (unchanged) |
| `arbor_hour` | (0, 0, 0.0385) | `Kit_WallClock_HandHour` origin |
| `arbor_minute` | (0, 0, 0.0400) | `Kit_WallClock_HandMinute` origin |
| `arbor_second` | (0, 0, 0.0415) | `Kit_WallClock_HandSecond` origin. Its cap front sits at 0.0455 |

**New sidecar fields on `Kit_WallClock`:**

```json
"tags": ["office", "wall_decor", "live_clock"],
"clock": {
  "hands": [
    {"kit": "Kit_WallClock_HandHour",   "anchor": "arbor_hour",   "shows": "hour"},
    {"kit": "Kit_WallClock_HandMinute", "anchor": "arbor_minute", "shows": "minute"},
    {"kit": "Kit_WallClock_HandSecond", "anchor": "arbor_second", "shows": "second"}
  ],
  "second": "step",
  "handCull": 18.0
},
"lodDistances": [5.0, 18.0, 45.0]
```

- `FrontRoomsKitLibrary.Info` gets a matching `[Serializable] ClockDef clock`. `JsonUtility` ignores the field in older readers.
- The block is generic, so a later analog `Kit_TimeClock` reuses the driver.
- **Hand sidecars:**
  - tags `clock_hand`, `part`;
  - `noCollider`;
  - `kit.meta["motion"] = {"type": "rotate", "axis": [0, 0, 1], "pivot": "origin (arbor)", "driver": "FrontRoomsWallClock", "zero": "12 o'clock", "sign": "+deg = clockwise seen from the front"}`.

### 4.3 Body parts and dimensions (metres; Blender distance in front of the wall)

| Part | Spec | Slot | LODs |
|---|---|---|---|
| Case | `lathe`, keeping today's envelope: back edge r 0.148 at 0; side r 0.160 from 0.008 to 0.034; bezel top 0.0495; inner lip r 0.1435 at 0.0475; down to the dial at r 0.1415, 0.037. **Hero change:** the 6 profile points become about 13, so the side-to-bezel and lip are true fillets (side 0.160 → 0.1595 at 0.040 → 0.158 at 0.045 → 0.1555 at 0.0485 → 0.152 at 0.0495 → 0.147 at 0.049 → 0.1435 at 0.0475 → 0.1425 at 0.044 → 0.1415 at 0.037). The back stays closed (a flat cap at y = 0) | PlasticBlack | LOD0 **96** segments (3.75°, sagitta 0.09 mm); LOD1 48; LOD2 24 with 5 rings |
| Dial | flat disc r 0.1418 at 0.037, facing the room | PlasticWhite | 96 / 48 / 24 |
| Hour markers | 12 printed bars: majors (12/3/6/9) 7.5 × 26 mm, minors 4.5 × 17 mm, outer end at r 0.127 (as today). Quads **0.2 mm** proud (today 0.5) for less parallax at 0.3 m | PlasticBlack | LOD0 and LOD1; **dropped at LOD2** (under 0.4 px at 18 m) |
| Lens | flat disc r 0.1428 at 0.0465 (single face, as today) | Glass | LOD0, LOD1; **dropped at LOD2** (one transparent draw fewer, and no hands behind it) |

The red second hand and its cap move to their own kit, so **the body loses `Prop_PlasticRed`** (3 slots: PlasticBlack, PlasticWhite, Glass). The `.fbx.meta` keeps a harmless remap entry for it.

**Not modelled** (never seen on the wall, or below a pixel):
- the hanger keyhole, battery door and movement box;
- the brass shaft nut (hidden under the hour hub).

### 4.4 Hands (three kits)

Outline coordinates are (x across, z along) in metres at the 12 o'clock pose, from `wall_clock.py:84-90` plus hubs.

| Kit | Outline | Hub | Thickness, edge | Slot | Budget LOD0 / LOD1 |
|---|---|---|---|---|---|
| `Kit_WallClock_HandHour` | tail −0.020 (w 9.6 mm) → shoulder 0.066 (w 8.4) → point 0.077 | round boss Ø 10 mm around a Ø 3.6 mm pipe stub | 1.2 mm; 0.25 mm chamfer on LOD0 | SteelBlack (painted metal, dielectric) | 300 / 80 |
| `Kit_WallClock_HandMinute` | tail −0.024 (w 7.2) → shoulder 0.104 (w 6.0) → point 0.116 | boss Ø 8 mm | 1.0 mm; 0.25 mm chamfer on LOD0 | SteelBlack | 300 / 80 |
| `Kit_WallClock_HandSecond` | tail −0.034 (w 2.8) → tip 0.122 (w 1.6), square tip as today | red cap Ø 5.5 × 4.0 mm with a bevelled dome (`wall_clock.py:91-92`) | 0.8 mm; no chamfer | PlasticRed | 350 / 90 |

**Hand LODs:**
- `lodDistances [5, 18, 18]`: LOD0 → LOD1 at 5 m, culled at 18 m.
- `FrontRoomsKitImporter` reads the array's last entry as `dcull` when the FBX has two levels (`FrontRoomsKitImporter.cs:88-92`).
- **The hands therefore vanish exactly where the body switches to LOD2**, as D4 asks.

**Shadows:** none. `FrontRoomsKitLibrary.Prepare` turns shadow casting off for kits under 0.3 m, and a 2–4 mm stand-off cannot be resolved by the shadow map anyway.

### 4.5 LOD distances and what they mean on screen

The figures are metres at lodBias 1 and vFOV 76°, the convention `FrontRoomsKitImporter.cs:82` converts. At 1080p, the screen gives 691 / d px per metre.

| Switch | Distance (lodBias 1) | On screen then | Why |
|---|---|---|---|
| LOD0 → LOD1 (body and hands) | 5 m | clock 44 px; minute hand 16 px long, 1 px wide | 48-segment silhouette error 0.34 mm = 0.05 px |
| LOD1 → LOD2 body, **hands culled** | 18 m | clock 12 px; minute hand 4.4 px, hour 2.9 px | Beyond this the time cannot be read |
| Body culled | 45 m | clock 4.9 px | Rooms rarely give a 45 m line of sight; it matches small wall decor |

The quality levels scale every switch the same way:

| Platform default | Quality level | lodBias | Hands visible to |
|---|---|---|---|
| Standalone | Ultra | 2.0 | 36 m |
| WebGL | High | 1.0 | 18 m |
| iPhone | Medium | 0.7 | 12.6 m |

### 4.6 Budgets

| | LOD0 | LOD1 | LOD2 | Draws (opaque + transparent) |
|---|---|---|---|---|
| Body | **3,000 ±15 %** (est. 2,550) | 1,000 (est. 900) | 250 (est. 220) | 3 (black, white, glass) → 2 at LOD2 |
| 3 hands | 950 | 250 | culled | 3 |
| **Clock total** | **≈ 3,950** (today 538) | ≈ 1,250 | ≈ 250 | **6** (today 4) |

- **Room check:** `L0_WaitingRoom_4x3` props today total ≈ 9.2k LOD0 (3 × LadderChair 1,778 + SideTableTurned 2,688 + Torchiere 636 + clock 538). With the new clock that is ≈ 12.6k (≈ 17k after the W2 torchiere), far under `LEVEL_MODULE_SPEC` §8's 60k. Renderers go +3 per clock.
- **Textures:** none. Everything is geometry, so it is crisp at any distance and costs no memory.

### 4.7 Build notes for the kit stage

- **Hand-built LODs** (P-1b, `obj["fr_lods"]`), not decimation: decimating rings and thin hands gives faceted silhouettes and broken hubs (the torchiere and chair lessons, `office_and_film/w2_props/20_build.md:47-51`). Use `kitlib.py` as it is; no kitlib change.
- **Shared constants** go in `wall_clock_common.py`: the anchor depths and the hand outlines. `wall_clock.py`, `wall_clock_hand_hour.py`, `wall_clock_hand_minute.py` and `wall_clock_hand_second.py` import them.
- **The previews and three-views** show the hands at **10:08:37**, the catalogue pose. That is the only place the old time survives: the kit sheet looks like the catalogue, and the game shows live time.
- **What hero means at 0.3 m** (the T4 precedent, `room_visuals/run_b_production/10_plan.md:444`):
  - no facets on the bezel silhouette;
  - the bezel fillet catches a continuous highlight;
  - the hand chamfers read as edges;
  - the second-hand cap reads as a dome;
  - the markers are crisp, with no z-fighting at any LOD distance.

## 5. `FrontRoomsRealTime`: the shared time service

**File:** `Assets/Scripts/Rendering/Clock/FrontRoomsRealTime.cs`. Visual-owned. The code is ASCII only, uses no C# tuples, and follows the house style.

### 5.1 What it is

- A static class with a hidden runner: one `HideAndDontSave`, `DontDestroyOnLoad` GameObject with `[DefaultExecutionOrder(-900)]`, made lazily on first use, as `FrontRoomsInspect`'s hidden runner is.
- It samples the time **once per frame** in `Update`, so every reader in a frame sees the same value. Outside Play mode, it samples on demand when `Time.frameCount` has moved.
- **It reads no gameplay state, and gameplay never reads it.** The director, RELAX, the dry clock, the tier clock and `elapsed` keep game dt (`research/inspect/10_design.md:471-477`).

### 5.2 API

```csharp
public static class FrontRoomsRealTime
{
    public const int EraYear = 1990;

    // ---- the local wall time (device clock + local zone; WebGL: the browser's zone)
    public static long LocalTicks { get; }       // DateTime ticks, real calendar
    public static DateTime LocalNow { get; }     // Kind Unspecified, real year
    public static int Hour { get; }              // 0-23
    public static int Minute { get; }            // 0-59
    public static int Second { get; }            // 0-59
    public static float SubSecond { get; }       // 0..1 since the last second boundary
    public static double HourF { get; }          // Hour + Minute/60 + (Second+SubSecond)/3600
    public static double MinuteF { get; }        // Minute + (Second+SubSecond)/60
    public static double SecondF { get; }        // Second + SubSecond
    public static int SecondSerial { get; }      // +1 at each second boundary seen (change detection)

    // ---- the era calendar (D2): real month/day in 1990, Feb 29 -> Feb 28, 1990 weekdays
    public static DateTime EraToday { get; }     // 1990-MM-DD 00:00; do date arithmetic on this
    public static DayOfWeek EraWeekday { get; }

    // ---- events (fired by the runner's Update; at most once each per frame)
    public static event Action SecondChanged;    // the 1 Hz tick
    public static event Action MinuteChanged;
    public static event Action DayChanged;       // local midnight (EraToday changed)

    // ---- strings for screens (信息设计 / 叙事 formats); cached, rebuilt only when the value changes
    public static string TimeText { get; }           // "2:25 PM"  (h:MM AM; 0 h -> "12:MM AM", 12 h -> "12:MM PM")
    public static string DateText { get; }           // "MON 10/22/90"
    public static string DateDashText { get; }       // "10-22-90" (TV off-air bars <MM-DD-90>)
    public static string NightShiftDateText { get; } // "10-21-90" (station DAR header, 叙事's rule)
    public static int WriteTimeText(char[] dst, int at);   // never allocates; returns the length

    // ---- harness and capture
    public static void Pin(int hour, int minute, int second, bool advance = false); // fixed or running from a set time
    public static void PinDate(int month, int day);   // date tests (e.g. 2, 29)
    public static void Unpin();
    public static bool Pinned { get; }

    // ---- diagnostics
    public static TimeSpan UtcOffset { get; }
    public static string Source { get; }  // "system" | "browser" | "pinned"
}
```

### 5.3 Sampling, the zone and the edge cases

- **Each frame:**
  - `utc = DateTime.UtcNow.Ticks` (a struct; no allocation);
  - `local = utc + offsetTicks`;
  - derive the fields from `local % TicksPerDay` with integer maths.
- **The offset** is refreshed at start, at each local minute boundary, and on `OnApplicationFocus(true)` / `OnApplicationPause(false)`:
  - **Desktop, iOS and the Editor (every target):** `TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow)`. Focus and resume first call `TimeZoneInfo.ClearCachedData()`, so a zone change while the game is in the background is picked up. This happens only on those events, so any allocation there is acceptable.
  - **WebGL player:** the `.jslib` (§7).
- **Daylight saving and odd zones.** Every current DST change, including the 30-minute ones, happens on a minute boundary (in practice on the hour). The per-minute refresh applies it at that minute, and the hands jump then. A real battery clock would keep the old time until someone reset it, but for the player the true local time is the point.
- **Clock jumps** (NTP, the user changes the time, a sleep longer than 2 s, a frame later than 1.5 s): the driver snaps with no settle and never sweeps backwards. Each event fires once, not once per skipped second.
- **Era date.** `EraToday` maps the real month and day to 1990, with Feb 29 → Feb 28. The 1990 weekday follows from `DateTime(1990, m, d).DayOfWeek` (2026-10-22 → `MON 10/22/90`). Consumers do day arithmetic on `EraToday`, so a subtraction crosses into 1989 correctly.
- **`NightShiftDateText`** (`NOTE_1022_READ_FIRST.md` §4; `station/01_research.md` §6) = `EraToday − 1 day` if `Hour ≥ 7`, else `EraToday − 2 days`, formatted `MM-DD-YY` in the era calendar. Checked in Python against the note's cases (all match):

| Real local time | `DateText` | `NightShiftDateText` |
|---|---|---|
| 2026-10-22 14:25 | `MON 10/22/90` | `10-21-90` |
| 2026-10-22 03:00 | `MON 10/22/90` | `10-20-90` |
| 2027-01-01 08:00 | `MON 01/01/90` | `12-31-89` |
| 2027-01-01 03:00 | `MON 01/01/90` | `12-30-89` |
| 2028-02-29 08:00 | `WED 02/28/90` | `02-27-90` |
| 2028-03-01 03:00 | `THU 03/01/90` | `02-27-90` |

- **Strings without garbage.**
  - Each string is rebuilt only when its value changes: `TimeText` once a minute, the dates once a day.
  - `TimeText` values come from a lazily filled `string[1440]` table, indexed by minute of day. Each value allocates once per session, about 40 B, and never again. The date strings change once a day.
  - `WriteTimeText` exists for UI that takes a `char[]`.
  - Steady frames allocate 0 B.
- **Pinning.**
  - **Command line:** `-frClock HH:MM:SS` pins a fixed time; `-frClock HH:MM:SS+` runs from it. `-frClockDate MM-DD` pins the date. The command line is parsed once.
  - **In code:** `Pin(…)`.
  - **What `advance` follows:** `Time.captureDeltaTime` frames under a capture harness, else unscaled real time. Recorded video therefore shows the clock at the recording's own pace.
  - **Every deterministic capture must pin** (pixel diffs, hash tests, signature dumps). Without a pin, the clock shows the real time.

## 6. `FrontRoomsWallClock`: the driver

**File:** `Assets/Scripts/Rendering/Clock/FrontRoomsWallClock.cs`. Visual-owned.

### 6.1 How it attaches (no map change)

`FrontRoomsKitLibrary.Spawn` already adds `FrontRoomsExitSign` to kits tagged `exit_sign` (`FrontRoomsKitLibrary.cs:233-236`), and every map module prop goes through `Spawn` (`FrontRoomsMapWorld.cs:2370`). It gets the same hook:

```csharp
if (info != null && info.HasTag("live_clock")) FrontRoomsWallClock.Attach(instance, info);
```

`Attach`:
- **is idempotent:** an existing driver with its hands is kept, so a re-spawn or a saved look-dev scene never doubles the hands;
- **spawns each hand kit** through `FrontRoomsKitLibrary.Spawn(handKit, root, anchorPos, Quaternion.identity, null, colliders: false, label, inspectable: false)`, at its sidecar anchor and as a child of the clock root (the hands' own LODGroups sit beside the body's);
- **poses the hands at once**, so the first rendered frame and any Edit-mode preview (the Level Designer) show the right time. In Edit mode the clock is posed only at spawn; it does not tick.

The station look-dev and the glass prop look-dev spawn through `Spawn`, so they get the hands too.

### 6.2 What it writes

- **Angles in degrees,** clockwise from 12 as seen from the front:
  - `hour = 30·(Hour mod 12 + Minute/60 + Second/3600)`;
  - `minute = 6·(Minute + Second/60)`;
  - `second = 6·Second + settle(SubSecond)`.
- **The hour and minute hands use the integer second,** exactly as a quartz gear train does: the whole train steps once a second.
- **They still read as continuous.**
  - Per step, the minute-hand tip moves 0.2 mm. That is 0.27 px in the EXAMINE close-up (the clock at about 432 px tall) and less from the floor.
  - Updating them every frame would cost writes with no visible gain.

### 6.3 The quartz step and its settle (ESTIMATE, tuned in VL374)

At each second boundary the hand follows a second-order step response over τ = time since the boundary (SubSecond seconds):

```
y(τ) = 1 − e^(−ζωτ)·(cos(ω_d τ) + ζ/√(1−ζ²)·sin(ω_d τ)),  ω_d = ω·√(1−ζ²)
angle = 6·(Second − 1) + 6·y(τ)   for τ < 0.12 s;   6·Second afterwards
ζ = 0.55, ω = 90 rad/s
```

- **The shape this gives:**
  - first peak at 42 ms;
  - overshoot 12.6 % (0.76°);
  - within 2 % by 81 ms, then clamped at 0.12 s.
- **At 60 fps:**
  - the first frame after the boundary shows about 59 % of the step;
  - the next is at the peak;
  - then a small recoil.
- **At 30 fps** it reads as a jump with one in-between frame.
- **How visible the settle is:**
  - in EXAMINE, the step is about 17 px at the tip and the overshoot about 2.3 px;
  - at 3 m the overshoot is about 0.4 px, so it disappears with distance by itself.
- **No settle when** the previous second seen was not `Second − 1` (a jump, a hitch over 1 s, or the first frame): the hand snaps.

### 6.4 Visibility gating (D5) and the update hook

- **The hook:**
  - One static handler on `RenderPipelineManager.beginContextRendering` runs once per frame, after every `LateUpdate` (so the camera rig is final) and before culling. It ticks every registered clock. The precedent is `FrontRoomsWallFixtureRenderer`'s `beginCameraRendering` submitter.
  - The camera list arrives with the call, with no allocation, and is walked with a `for` loop.
  - Tests and batch harnesses call `FrontRoomsWallClock.TickAll(Camera)` directly.
- **Live clocks.** A clock is *live* for a frame when, for any Game camera in the list (and the Scene view camera in the Editor), either test passes:
  - `d < NearRange` (3 m), which covers shadows, reflections and the edge of the frame; or
  - `d < HandCull · k` **and** its world bounds pass `GeometryUtility.TestPlanesAABB`.
    - `k = lodBias · tan(38°) / tan(vFOV/2)`, the outlets' rule for behaving like a LODGroup.
    - The planes come from `CalculateFrustumPlanes(cam, Plane[6])`, the non-allocating overload.
    - The bounds are cached at `Attach`. Clocks never move, and they are not pile pieces.
- **Writes:**
  - The three global angles (with the settle) are computed **once per frame for all clocks**: every clock shows the same time.
  - A live clock gets its three `localRotation`s written only when the angles changed since the last frame, or when it has just become live.
  - In practice that is about 6 frames in every second (the 0.12 s settle at 60 fps, plus the step).
  - A dormant clock is skipped.
  - **No stale first frame.** The test runs before culling, so a clock that comes into view is posed in the same frame.
  - Every `MinuteChanged` writes **all** clocks, live or not. A reflection probe or another camera therefore never sees a clock more than a minute out.
- **Cost estimate with 20 clocks in range:**
  - each frame: 20 × (one distance test + at most one AABB test), about 5 µs;
  - writes: 60 rotations on about 6 frames per second.
  - Gate G4 measures it.
- **The `SecondChanged` event is for other consumers.** The driver does not need it.

### 6.5 Pause, EXAMINE and timeScale 0

- During the pause card and EXAMINE close-ups, `Time.timeScale` is 0 (`FrontRoomsInspect.cs:13-15`). `Update` and rendering still run, and `FrontRoomsRealTime` reads `DateTime.UtcNow`, so **the clock keeps real time in pause and in EXAMINE.** This is what D1 asks for: the clock is real time.
- **The EXAMINE freeze checks still hold.** They cover lamps, the Relay, doors, glow, `elapsed` and the tier clock (`inspect/10_design.md:955`), not prop transforms. Gameplay hashes (AT-08) do not include the hands.
- **A browser tab in the background** gets no frames. On return, the first frame snaps to the right time.

### 6.6 EXAMINE

- **How the close-up sees the hands.** The clock keeps its T1 block (`kind = flat`, size 0.32 × 0.32, dist 0.59, 40° up, `ship: false`). The flat kind moves the camera to the object (`inspect/10_design.md:181`), so the close-up looks at the real hands, which stay live (d < 3 m) and step with their settle.
- **The hands do not change the framing.** They are child renderers inside the body's bounds.
- **No catalog change is needed.** The T1 comment in `FrontRoomsInspectCatalog.cs` can say "live hands since 10-22".
- **Release builds** still offer no close-up on the clock. R6 and the §1.3 rule apply: a 0.32 m clock reads in place from the room. §12 notes this as optional; it is not a question for Red.

### 6.7 Interplay with other visual systems

- **Glass RT (G14, Metal):**
  - The hands must **not** be flagged Dynamic. A dynamic instance enters the accumulation signature (`FrontRoomsGlassRTSystem.cs:1185`), so a stepping hand would reset reflection accumulation every second near a clock.
  - As static instances, the hands' transforms are re-read in the slow round-robin (`:1051-1060`). A reflected hand may lag a few seconds, which at 2 mm is invisible.
  - The hand GameObjects must not be named `Key…` or sit under `Door hinge…` (the `Classify` rules at `:1039-1047`).
- **The lens** stays `Prop_Glass`, so the glass census and the prop look-dev keep finding it.
- **WebGL draw budget:** +2 draws per visible clock compared with today. Clocks are rare (×2 in a built map), so the WebGL tier needs nothing special. Its lodBias of 1 already culls the hands at 18 m.

### 6.8 A scale-up kept in reserve (not built)

If G4 ever failed, one fallback needs no GameObjects:
- one instanced draw per hand type for **all** clocks, since they share the time;
- submitted per camera from `beginCameraRendering`, as the outlets are.

It gives up the probe-lit look and the LODGroup consistency of real renderers, so it is not the plan.

## 7. WebGL local time

**What is known:**
- **The rule:** WebGL work must be WebGL-only. Editor Play mode and Mac/Win stay byte-for-byte on the desktop path.
- **The open question:** what `DateTime.Now` / `TimeZoneInfo.Local` return in a Unity 6000.3 IL2CPP WebGL build. Mono's Unix zone lookup reads zoneinfo files that a browser sandbox lacks, so UTC is a plausible answer.
- **What the search found** (2026-10-22): no conclusive answer.
  - A Unity 6 forum thread reports a 1-hour offset in a WebGL build, with no `TimeZoneInfo` output posted (discussions.unity.com/t/1548558).
  - An older tracker bug had `DateTime.Now` returning UTC on Android/iOS (.NET 4.6), fixed in 2017.4.
- **So the probe records the answer, and the design does not depend on it.**

**The plan:**

1. **`Assets/Plugins/WebGL/FrontRoomsRealTime.jslib`** (new folder; the `.jslib` importer includes it in WebGL builds only):
   ```js
   mergeInto(LibraryManager.library, {
     FR_LocalUtcOffsetMinutes: function () { return -(new Date()).getTimezoneOffset(); },
     FR_EpochMs: function () { return Date.now(); }   // diagnostic and fallback
   });
   ```
2. **In `FrontRoomsRealTime`:**
   - `#if UNITY_WEBGL && !UNITY_EDITOR`, `[DllImport("__Internal")] static extern int FR_LocalUtcOffsetMinutes();` and the same for `FR_EpochMs` (a double).
   - **On WebGL the offset always comes from the browser,** whatever `TimeZoneInfo` says, so the clock follows the zone the browser reports.
   - It is refreshed every minute (for DST) and on focus.
   - `DateTime.UtcNow` stays the time base, unless the probe shows it differs from `FR_EpochMs` by more than 1 s; then `FR_EpochMs` is used.
3. **Desktop, iOS and the Editor (any target)** compile none of this, so their path is unchanged.
4. **The probe (gate G6):** a minimal Unity project, **`W/clock_wgl_probe`**, not a full clone.
   - **Why a separate project:** it keeps `proj_clock` on `-buildTarget OSXUniversal`, and an empty project builds WebGL in minutes. It is the one deliberate `-buildTarget WebGL` job, and it counts toward the 2-job cap.
   - **Contents:**
     - main's `ProjectSettings/ProjectSettings.asset` WebGL player settings (high stripping, Wasm 2023, no threads, decompression fallback);
     - the two clock scripts and the `.jslib`;
     - a scene with a uGUI text and three plain quads driven by `FrontRoomsWallClock.TickAll`.
   - **It logs, on screen and to `document.title` for `--dump-dom`:**
     - `DateTime.Now`, `DateTime.UtcNow`;
     - `TimeZoneInfo.Local.Id` / `BaseUtcOffset`;
     - `FR_LocalUtcOffsetMinutes`, `FR_EpochMs`;
     - `FrontRoomsRealTime.LocalNow`, `TimeText` and `DateText`.
   - **How it runs:**
     - Served with `python3 -m http.server` on 127.0.0.1.
     - Opened in headless Chrome under two zones: the machine's (America/Los_Angeles) and `TZ=Asia/Shanghai`. If Chrome on macOS ignores `TZ`, the zone is set through CDP `Emulation.setTimezoneOverride`.
     - Also checked once in the built-in browser pane.
   - **Pass:**
     - in both zones, `LocalNow` equals JS `new Date()` local fields within 1 s;
     - the quads match;
     - the `DateTime.Now` and `TimeZoneInfo` values are recorded, as facts for the WebGL track.
   - **Cleanup:** remove Chrome's `code_sign_clone` copies from `/var/folders/…/X/` afterwards (the 10-15 leak).
5. **Not in this run:** the full game's WebGL build. It is the WebGL track's, at its next build. The real clock kit rides along with no WebGL-specific asset.

## 8. Other devices: what is wired now, and the contracts

**Wired in this run:** only the wall clock (everywhere it spawns: the waiting-room module, Level Designer previews, the station when the station is built, look-dev).

**Contracts for consumers when they are built** (none is in main today):

| Consumer | Builder | Reads | Rule |
|---|---|---|---|
| Terminal header | 游戏视觉 (screen renderer) + 平面视觉 · 信息设计 (art) | `TimeText` (right-aligned; fits 8 characters, `12:59 PM`) and `DateText` if a page shows a date | Re-render the FRONT RT on `MinuteChanged`: one render per minute per active terminal, never per frame. Hash and invariance tests pin the clock (§9, 系统设计's AT-T16) |
| TV off-air bars | 平面视觉 · 广告 (CG layer) + 游戏视觉 (runtime overlay on the screen RT) | `TimeText` (CG 48; the widest value, `12:59 PM`, is 8 characters, inside the slot's 12), `DateDashText` (CG 30, 8 characters) | The video is pre-rendered, so the time and date are a runtime CG overlay, redrawn on `MinuteChanged`. The sign-off plays only 02:00–06:00 local (叙事) |
| Station Daily Activity Report | 游戏视觉 (station paper, EXAMINE text) | `NightShiftDateText` | EXAMINE transcripts get a `{NIGHT_SHIFT_DATE}` token, replaced at open time. The entries stay fixed strings (叙事) |
| `Kit_TimeClock` (analog) | 游戏视觉 | the sidecar `clock` block | Same driver, different hand kits and anchors |
| `Kit_TimeClock` (digital), `Kit_CCTVMonitor9` stamp, a VCR | 游戏视觉 + 平面视觉 | the service's fields | Live time; never a blinking 12:00; the date in 1990 |
| Desk phone LCD | — | — | Stays dark (lamps dark, station S18). §12 O2 |

**Left as they are:** desk CRT ads and still screens, CRT TV ads, the copier counter, the vending price, the PC case, the caught card.

## 9. Docs to update, and the peer notes

### 9.1 Docs this workflow may edit (later stage)

- **`research/inspect/01_inventory.md` S5** (`:184-190`):
  - "Time has stopped" and "the hands frozen at 10:08:37" become "the real local time; the second hand steps (live since 2026-10-22, Red, 游戏视觉 chat)";
  - add one superseded line quoting Red;
  - close-up readiness: "yes; hero rebuild 10-22, the hands are separate kits";
  - tris: ≈ 3,950 LOD0.
- **`research/inspect/10_design.md` T1** (`:61`, `:237`): a note that the hands are live since 10-22. The test-bed status and the framing numbers are unchanged.
- **`research/live_clocks/`:** this file; `NOTES_for_peers.md`; later `20_build.md` and the VL rows.

### 9.2 Docs owned by others (notes only; never edited by this workflow)

| File | Owner | State at 14:4x | Still needed |
|---|---|---|---|
| `research/computer_ui/10_research.md:556` | 系统设计 | **Done 14:26** (superseded note) | — |
| `research/computer_ui/10_research.md:791` (ALARM HISTORY: "no live clock, so RELAX cannot be counted down") | 系统设计 | unchanged | The reason no longer holds. Keep the entries without timestamps (a timestamped RESET hands over the exact RELAX start) or reword: their call |
| `research/computer_ui/20_design.md` :571, :764, :849, :902, :1177, :1277 | 系统设计 | **Done 14:26** | — |
| `20_design.md` AT-T09 (`:1173`, "No RT render happens without an event (activation, open, turn, exit)") and AT-T10 ("0 UI renders on idle frames") | 系统设计 | unchanged | Add "the minute (header clock)" to the events, and "except the header's once-a-minute render" |
| `20_design.md` AT-T16 (`:1180`, the FRONT glance texture hash is identical across states) | 系统设计 | unchanged | The hash now changes each minute: the test pins `FrontRoomsRealTime` (or masks the header) |
| `research/computer_ui/00_interaction_inventory.md:463` ("the clock frozen at 10:08:37") | 系统设计 | unchanged | → "the wall clock shows the live time" |
| `research/computer_ui/SOURCES.md:201` | 系统设计 | citation | Optional "(superseded 10-22)" |
| `GRAPHIC_VISUAL_TASKS.md:370`, `graphic_visual_briefs/information_design.md:74` | 平面视觉 · 信息设计 | **Done 14:24** | — |
| `research/screens/30_screen_strings.md` §4.1, `research/station/01_research.md` §5–§6, `narrative/40_floors_exit_tutorial_strings.md:98` | 叙事设计 | **Done** (live calendar, Feb 29, printed ≤ 1989, DAR date rule) | — |
| `RELAY_PURSUIT_REDESIGN.md` §11 | 系统设计 | **Done** ("Countable RELAX", f95b5ce6) | — |
| `wallpaper_motion/30_narrative_phosphor.md:595`, `20_level_design_phosphor.md:867` ("because time here is wrong") | 叙事设计 / wallpaper line | unchanged | Their call: with clocks right, the line now stands on the self-test alone |
| `research/station/30_look.md` S30 | 游戏视觉 (the 游戏视觉 session itself; outside this stage's write list) | `Kit_WallClock Ø 0.32, 538, exists` | → "rebuilt 10-22: hero, live hands, ≈ 3,950 / 1,250 / 250" |
| `VISUAL_CHAT_TASKS.md` STATUS | 游戏视觉 session | — | Add the live-clocks line when it lands |

### 9.3 Draft of `NOTES_for_peers.md` (written by the docs stage)

- **系统设计 (FYI, nothing to decide):**
  - Live clocks are in; you kept RELAX as C01 (RELAY §11, 14:23); nothing else is asked.
  - Gameplay never reads `FrontRoomsRealTime`.
  - The remaining text in your files is the four rows marked "unchanged" in §9.2: :791, AT-T09/AT-T10, AT-T16 and the inventory :463.
  - AT-T16 can pin the clock with `FrontRoomsRealTime.Pin(10, 8, 37)` or `-frClock 10:08:37`.
- **平面视觉 · 信息设计:**
  - Your format is implemented as `TimeText` (`h:MM AM`, no seconds) and `DateText` (`MON 10/22/90`).
  - The header re-renders on `MinuteChanged`.
  - The widest value is `12:59 PM` (8 characters), right-aligned. Please confirm the header slot fits 8.
- **叙事设计:**
  - Your rules are implemented once in the service: Feb 29 → `02-28-90`, never ahead; the night-shift date rule as `NightShiftDateText` with your test cases (§5.3); arithmetic in the 1990 calendar, so Jan 1 gives `12-31-89` / `12-30-89`.
  - Our S5 entry drops "Time has stopped".
  - Your call: the "time here is wrong" lines (§9.2) and `R.M. 6/90` (A.11, frozen ink), which lies in the future of the live date from January to May.
- **平面视觉 · 广告:** if the off-air bars are ever built, the time and date are a runtime overlay fed by the service (§8). Nothing now.
- **声音设计 (FYI):**
  - No clock sound was added.
  - A 1 Hz quartz tick is period-true, but it could read as a timer cue: your call.
  - The hooks: `FrontRoomsRealTime.SecondChanged` and the `FrontRoomsWallClock` registry (positions).
- **关卡设计 (FYI):**
  - No module or map change.
  - `Kit_WallClock_Hand*` will show in the Level Designer palette, as the closer and lock parts do. Hiding kits tagged `part` is optional and yours.
  - `L0_WaitingRoom_4x3` gains +3 renderers.
- **WebGL track (FYI):** a new `Assets/Plugins/WebGL/FrontRoomsRealTime.jslib` and one `#if UNITY_WEBGL && !UNITY_EDITOR` branch; the probe results go in `research/live_clocks/`.
- **All capture harnesses (all visual lines, codex audit):**
  - Pin the clock in any deterministic capture or signature dump.
  - Kit_WallClock's signature changes once (tris, submeshes, children).

## 10. Gates

Timings are taken at a 1-minute load average; any above 32 is marked **UNDER LOAD**. The load was 122 at 14:21 and 40 at 14:39.

| ID | Gate | Method | Pass |
|---|---|---|---|
| G1 | **Hands match a known system time within 1 s** | Mac test player (bundle `com.redwang.frontroomsss.test.proj_clock`, `open -n`, quit at the end) and Editor Play mode. The reference is independent: the shell's `date` at launch plus a monotonic `Stopwatch`, not `DateTime.Now`. The harness decodes the time from the three `localRotation`s: second to the nearest step after the settle, then minute and hour from their angles. Taken at 3 moments, plus pinned crossings of 11:59:55 → 12:00 (AM/PM) and 23:59:55 → 00:00 (`DayChanged`, `EraToday`) | decoded time − reference ≤ 1 s; minute angle within 0.05° of 6·(m + s/60); hour within 0.01° |
| G2 | **The second hand steps** | Sample the second-hand angle every frame for 10 s at 60 fps and at 30 fps | exactly 10 steps of +6.000° (±0.01° settled); each settles within 0.12 s of the boundary; peak overshoot 0.5–1.0°; constant from 0.12 s to the next boundary (no sweep); the minute hand advances 0.1° per step |
| G3 | **Pause and EXAMINE keep time** | The game's pause card (timeScale 0) for 10 s, then an EXAMINE close-up on the clock (DevTargets) for 10 s | 10 ± 1 steps in each; the G1 decode passes at the end of each; `Time.time` and `elapsed` unchanged; the inspect freeze checks (`inspect/10_design.md:955`) still pass |
| G4 | **20 clocks cost** | A harness room with 20 `Kit_WallClock` spawned through `Spawn` within 10 m (in view) and 20 more out of view; 600 frames; a `ProfilerMarker` "FrontRoomsWallClock.Tick"; draws from `ProfilerRecorder` "Draw Calls Count" | Mac player: Tick mean ≤ 0.02 ms, max ≤ 0.1 ms (Editor figures recorded); +3 draws per visible clock for the hands; spawning one clock with its hands ≤ 0.3 ms |
| G5 | **No GC allocation** | `ProfilerRecorder` "GC Allocated In Frame" over 120 s across a minute boundary, with a `TimeText` reader active | 0 B in every frame, except at most one ≤ 64 B allocation on the minute frame the first time a new minute string is built; the Tick marker 0 B |
| G6 | **WebGL local time** | §7 probe, two browser zones | `LocalNow` = JS local time ± 1 s in both; quads match; `DateTime.Now` / `TimeZoneInfo` recorded; desktop untouched: all WebGL code sits behind `#if UNITY_WEBGL && !UNITY_EDITOR` (Roslyn compiles it with the WebGL define, and macOS + iOS compile without it), and the `.jslib` importer includes WebGL only |
| G7 | **Hero kit** | `FrontRoomsThreeView`; 0.3 m close-ups of LOD0 (body and hands); each LOD at its switch; `FrontRoomsCxVisLodDump`; the sidecars | budgets §4.6 ±15 %; LODGroup present with the §4.5 distances; no bezel facets or z-fighting at 0.3 m; LOD pop ≤ 2 % mean Y in a 64 px window; the hands cull within ±0.5 m of the body's LOD2 switch |
| G8 | **Regression** | Level Designer tests ("hung kit goes up 2.1 m"); Inspect tests (fallback `ship: false`); glass prop look-dev census; Roslyn macOS + iOS; Edit-mode `Spawn` twice; chunk unload | all pass; still 3 hands after a re-spawn; 0 clocks registered after the map tears down |
| G9 | **Determinism** | Two runs with `-frClock 10:08:37` | byte-identical clock captures; without the pin, live time |
| G10 | **Orientation** | Pinned 3:00:00 and 9:00:00, clock at yaw 0 / 90 / 180 / 270 | 3:00 puts the hour hand on the viewer's right and 9:00 on the left, in every yaw (catches mirror or sign errors) |

## 11. Package, VL rows and clone

**Paths the package may ship** (all visual-owned):

| Path | |
|---|---|
| `Tools/Blender/frontrooms_kit/assets/wall_clock.py` | rewritten |
| `…/wall_clock_common.py`, `wall_clock_hand_hour.py`, `wall_clock_hand_minute.py`, `wall_clock_hand_second.py` | new |
| `Assets/Resources/Props/Models/Kit_WallClock.fbx` / `.json` | replaced; both `.meta` GUIDs kept |
| `Assets/Resources/Props/Models/Kit_WallClock_Hand{Hour,Minute,Second}.fbx` / `.json` + `.meta` | new |
| `Assets/Scripts/Rendering/Clock/FrontRoomsRealTime.cs`, `FrontRoomsWallClock.cs` + `.meta` (and the folder `.meta`) | new |
| `Assets/Scripts/Office/FrontRoomsKitLibrary.cs` | the `clock` field in `Info`, and the `live_clock` hook in `Spawn` |
| `Assets/Plugins/WebGL/FrontRoomsRealTime.jslib` + `.meta` (and the folder `.meta`) | new |
| `Assets/Editor/Rendering/FrontRoomsLiveClockTests.cs` | Edit-mode tests: angles, the settle, the era calendar and the night-shift cases, the formats, pin, idempotent `Spawn` |
| Docs | `research/live_clocks/**`, `research/inspect/01_inventory.md` (S5), `research/inspect/10_design.md` (T1 note) |

**Not touched:**
- `L0_WaitingRoom_4x3.asset`, the Level Designer, `FrontRoomsMapWorld`, `FrontRooms3DGame`;
- `FrontRoomsInspectCatalog.cs` (at most a comment);
- `GRAPHIC_VISUAL_TASKS.md`, the briefs, `research/computer_ui/**`.

**VL rows** (only VL373–VL376, to `W/vl_pending/live_clocks.md`). Every image goes through `W/tools/shotframe/shotframe.py`, per `SHOT_FORMAT.md`.

| VL | Content | Gates |
|---|---|---|
| VL373 | Hero `Kit_WallClock` + hands: three-view (pinned 10:08:37), 0.3 m close-ups, LOD0/1/2 at their switches, orientation strip (3:00 / 9:00 at four yaws) | G7, G10 |
| VL374 | Live time: two close-ups at different real times captioned with the shell's `date`, plus the second-hand angle plot over 10 s (step and settle) | G1, G2 |
| VL375 | Pause and EXAMINE frames at t0 and t0 + 10 s; the 20-clock room with its cost numbers | G3, G4, G5 |
| VL376 | WebGL probe in two browser zones, beside the JS time | G6 |

**Clone:** `W/proj_clock` via `mkclone.sh` (OSXUniversal). The WebGL probe goes in `W/clock_wgl_probe` (§7). At most 2 Unity jobs on the visual line: check `pgrep -fl "Unity.*-projectPath /Users/redwang/FrontRoomsVisualWork" | wc -l` before each start.

## 12. Needs Red

**Nothing blocks.** Every choice above is inside Red's request and the 游戏视觉 decisions. Two optional points can be mentioned to Red; if he says nothing, the default holds. Each is written as the player would feel it:

- **O1. A minute track on the dial (60 fine ticks)?**
  - **Today's look and the default:** 12 bars, no numerals. The player reads "a bit past the 4th bar".
  - **With ticks:** a player standing under the clock can read the exact minute.
  - **Cost:** about 100 tris and half a day with checks, no risk.
  - **Recommendation:** keep as decided (bars only). It matches the clock Red saw and the catalogue look; the exact minute is on the terminal header.
- **O2. Should the desk phone's display show an idle date and time?**
  - **Background:** a 1990 multi-line key phone of this type shows date and time on its LCD when idle.
  - **Ours today:** dark glass with dark line lamps; the phones read as dead, matching the station's S18.
  - **If shown:** it adds one more live clock at every equipped desk (45 % of stations). That means a texture or glyph quad per phone, 平面视觉 type, about 1–1.5 days, and more screens telling the time in a game that rarely tells you anything.
  - **Recommendation:** keep dark (default).
- **Also, not a question:**
  - The clock stays a test-bed EXAMINE target in release builds (R6, already decided). A 0.32 m clock reads from the room.
  - Real time in a night-shift building is 叙事's model: "the clocks are right, the hours are not". It needs no decision.

## 13. Risks and checks for the build stages

1. **WebGL `DateTime` behaviour is unknown.** The `.jslib` path makes the clock independent of it, and G6 records the facts.
2. **`TimeZoneInfo.GetUtcOffset` may allocate on IL2CPP.** It is called once a minute only. If G5 shows bytes there, the refresh moves to every hour boundary (current DST changes happen on the hour) plus focus and resume.
3. **Existing deterministic harnesses with a clock in frame now vary run to run unless pinned:** the glass prop look-dev, the W2 L0 placement shots, the outlets designer on/off pairs, the codex-audit signatures. The service's `-frClock` handles it; §9.3 tells the harness owners.
4. **The hand kits appear in the Level Designer palette.** That is harmless, and hiding them is the map's option (§9.3).
5. **The hands, the glass RT and the reflection probes:** see §6.7. Never flag the hands Dynamic.
6. **Load:** the machine was at 122 / 40 load today, so the G4 and G5 numbers may need a re-run below 32.
7. **The station places the clock at 2.47 m over E1** (its own look-dev), not 2.1. The kit does not care; the height is the placer's.

## Sources read for this design

**Code:**
- `Tools/Blender/frontrooms_kit/assets/wall_clock.py`; `kitlib.py:1-40, 500-512, 640-830, 830-897`; `build_asset.py`; `interact_door_closer_arm.py:1-40`;
- `Assets/Resources/Props/Models/Kit_WallClock.json` and `.meta`; `Kit_DoorCloser_Arm.json`;
- `Assets/Editor/Rendering/FrontRoomsKitImporter.cs`;
- `Assets/Scripts/Office/FrontRoomsKitLibrary.cs:1-60, 180-300`; `FrontRoomsWallFixtureRenderer.cs:1-60`;
- `Assets/Scripts/Rendering/Inspect/FrontRoomsInspect.cs:1-120`; `FrontRoomsInspectCatalog.cs`;
- `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTSystem.cs:1036-1060, 1180-1192`;
- `Assets/Scripts/Media/FrontRoomsScreenVideo.cs:1-40`;
- `Assets/Levels/Modules/L0_WaitingRoom_4x3.asset`; `FrontRoomsMap.cs:123, 416-436`; `FrontRoomsMapWorld.cs:2370`;
- `Assets/Editor/FrontRoomsMap/FrontRoomsLevelDesignerTests.cs:200-208`, `FrontRoomsModuleEditing.cs:33-77`; `Assets/Editor/Rendering/FrontRoomsGlassVerification.cs:596-640`;
- `ProjectSettings/QualitySettings.asset` (lodBias per level, per-platform defaults).

**Docs:**
- `research/inspect/01_inventory.md` S4–S6, `10_design.md` §1.2, §3.3, §3.5, R6;
- `research/computer_ui/10_research.md:545-570, 785-792`, `20_design.md` §6.4, §8.1, §9.1, AT-T09–AT-T16, `00_interaction_inventory.md:461-464`;
- `research/screens/30_screen_strings.md` §4.1, §6; `SCREENS_VISUAL_SYSTEM.md` §2–§6;
- `research/station/01_research.md` §5–§6, `30_look.md` S10–S36, `data/FrontRoomsStationLookdev.cs.txt:915-920, 1676`;
- `research/narrative/40_floors_exit_tutorial_strings.md:94-99`; `RELAY_PURSUIT_REDESIGN.md:688-693`;
- `GRAPHIC_VISUAL_TASKS.md:362-372`; `graphic_visual_briefs/information_design.md:68-76`;
- `research/office_and_film/10_synthesis.md:330, 801, 1053`, `w2_props/20_build.md`; `research/room_visuals/run_b_production/10_plan.md` §1, T4;
- `Documentation/Takeover_2026-10-18/handoff/visual.md`, `graphic_info.md:76`, `system.md:221`; this folder's `NOTE_1022_READ_FIRST.md`.

**Web** (2026-10-22, read only, nothing downloaded):
- Lavet-type stepping motor: https://en.wikipedia.org/wiki/Lavet-type_stepping_motor
- US4351039, an 8 ms drive pulse: https://image-ppubs.uspto.gov/dirsearch-public/print/downloadPdf/4351039
- US6359840, a ~47 ms tick pulse that turns the rotor 180°: https://image-ppubs.uspto.gov/dirsearch-public/print/downloadPdf/6359840
- Unity forum, "Timestamp is offset in WebGL build" (Unity 6 tag): https://discussions.unity.com/t/timestamp-is-offset-in-webgl-build/1548558
- Unity issue tracker, `DateTime.Now` returning UTC (.NET 4.6, Android/iOS, fixed 2017.4): https://issuetracker.unity3d.com/issues/projects-built-with-net-4-dot-6-datetime-dot-now-always-returns-current-utc-time-regardless-of-your-current-local-time
