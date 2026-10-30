# 70 · A13: the EXIT's lock in the world (build + test round, 2026-10-29)

**Status: READY, not applied.**
- The package is `W/apply/a13_exit_chain.sh`. Its dry run against main gives DRY RUN OK (18:5x; MapWorld 5c57cfa5, Game 527ce171).
- Roslyn on main + the package: COMPILE OK on macOS and on iOS (scratch copy `W/a13_work/scratch_main`, 18:5x).
- Apply: `bash /Users/redwang/FrontRoomsVisualWork/apply/a13_exit_chain.sh --apply`
- The 游戏视觉 session lands it. LANDINGS.md and MERGED.md were not touched. No game or map change is needed, so there is no contract. (`W = /Users/redwang/FrontRoomsVisualWork`.)

**Why now.** 关卡设计's first-room group is in main, switched off (MapWorld 5c57cfa5). With the station on, floor 1's EXIT is locked from the floor's start and E · UNLOCK lifts the lock at 0.87 s. Nothing in main subscribed to ExitLockChanged, so the locked EXIT showed only the crosshair padlock. "See the lock before the key" needs A13 in the world before the joint switch-on.

**Spec.** `10_directions.md` §1.2 (A13 states, maze side, one state change, no blink) and §7 (the map API: ExitDoorBuilt, ExitLocked, ExitLockChanged). System rule (系统设计 10-22): every floor's EXIT starts LOCKED once the floor key exists; with no lock it reads UNLOCKED. Red's A35 / Q1 = B (10-29): below LEVEL 0 in a station run there are no keys, so the EXIT there is unlocked.

## 1. What lands (9 new paths, nothing replaced)

| Path | What |
|---|---|
| `Assets/Scripts/Rendering/Exit.meta` | the folder |
| `Assets/Scripts/Rendering/Exit/FrontRoomsExitChain.cs` (+ meta) | the A13 dresser, one per map |
| `Assets/Scripts/Rendering/Exit/FrontRoomsExitChainKit.cs` (+ meta) | the meshes, cached per session, and the two materials |
| `Assets/Resources/Surfaces/Prop_ChainOxide.mat` (+ meta) | the chain: #34302B (L* 20.1), smoothness .35, metallic .55 |
| `Assets/Resources/Surfaces/Prop_SteelLaminated.mat` (+ meta) | the padlock and eye bolt: #9A9C97 (L* 64.1), smoothness .55, metallic .85 |

**The hook needs no game change.** `RuntimeInitializeOnLoadMethod` subscribes to `FrontRooms3DGame.MapRunStarted`. The game raises it right after `map.Begin`, and Begin builds no chunk, so the subscription is in place before the first ExitDoorBuilt. That holds for the played runs: all 4 EXIT builds of each run were dressed. `Attach(map)` also catches up an EXIT that was built before it (tools).

**Each ExitDoorBuilt** dresses the fresh build from scratch:
- **LOCKED** hangs on `leaf` (the unscaled "Leaf rig", never `door.leaf`). The ½ in proof coil (wire 12.7 mm, link 64 × 44 mm, 35 links, 1.43 m) is wrapped twice round the map's "EXIT crossbar", 0.09–0.19 m in from its latch case. It runs out to an eye bolt in N's wall face beside the latch jamb (0.14 m past the opening, 1.0 m up). The two ends are joined under the bar by a 7-plate laminated padlock (52 × 46 × 26 mm, the HUD padlock 3113:6999's family).
- **UNLOCKED** sits on `frame` ("EXIT frame N-X"), in the door's shut frame (`hinge.localPosition`, `door.closed`). It is the same length of chain, coiled on the floor against the wall on the latch side, with the padlock lying open beside it.
- **The eye bolt** is on the frame and stays in both states.
- **Both states are built with the door.** ExitLocked is read inside the handler, which covers PlanExit's lock set before Begin. ExitLockChanged switches the state with SetActive in the same frame: one change, never neither and never both. LateUpdate re-reads ExitLocked every frame, because it resets without an event on SetExitArea / ClearExitArea / NextFloor. That read is one bool and allocates nothing.
- **When LOCKED shows.** Only on a shut, whole EXIT that the map holds locked. An open or broken EXIT shows the chain on the floor.
- **What is not touched.** The EXIT sign stays the map's and stays LIT in both states. Nothing of the map's leaf, frame, crossbar or sign changes.

**Where it comes from.** It was written from the A13 part of the rescued 10-22 code (`W/stairwell_a/rescued_1022_1603`: the dresser's BuildLockStates / ApplyLock and the StairKit chain and padlock). It is re-based on main's MapWorld API. It has no GetInstanceID, no tuples, and is ASCII and LF only.

## 2. Proofs

The clone is `W/proj_a13` = main (Game 527ce171, MapWorld 5c57cfa5, KeyHud 60f28e7f) + the payload + the clone-only harness `Assets/Editor/Audit/A13/` (copy in `W/a13_work/harness/`, never packaged). Unity 6000.6.3f1, silent (FrontRoomsCloneOnlySilence), all runs under load. Logs are in `W/logs/a13/`, raw data in `W/a13_work/runs/`.

| | Proof | Result |
|---|---|---|
| (a) | Locked from the start | **Edit (desktop + phone):** SetExitLocked before Begin, as PlanExit does. The first build is dressed LOCKED inside the ExitDoorBuilt call, before any frame (builds 1, switches 0). **Play, station on** (`-autopilotExit -autopilotStation 4`, seed 2554, the map's tools stand-in for the station module): MapRunStarted f104 ExitLocked True. ExitDoorBuilt f112 dressed LOCKED. Every one of 4,273 frames (4,236 with the EXIT built) showed exactly the map's state, 0 bad. The EXIT first entered the frustum at f316, 27.6 m away behind the shut start door, already LOCKED. |
| (b) | Streaming | **Edit (desktop + phone):** RebuildChunk dresses once more, the old rig is gone and LOCKED is kept. Drop + stream back leaves nothing behind and dresses LOCKED again. NextFloor ×3 dresses each new EXIT once, and the old one is gone. **Play:** 4 builds → 4 dressings per run. Leftover checks 6/0 (on) and 7/0 (off). Never two of a state and never both states shown (bad 0). |
| (c) | The unlock | **Play:** ExitLockChanged(false) once, at shot clock 0.874 s (f895). The screen shows UNLOCKED in the same frame (1 switch, no blink). The leaf rig moves 0.000 m before the swap. At the swap it moves 1.5 mm, which is the map's own bolt-draw shift (`Unlock.LeafShiftMetres` .0015), not ours. Then the leaf swings with UNLOCKED on the floor. **Edit:** a rattle's jolt moves the LOCKED chain with the leaf rig (0.2 mm off the rig) and the eye bolt stays put. A 95° swing leaves the coil unmoved. Locked while open, it stays on the floor until shut. ClearExitArea (no event) is read on the next frame. |
| (d) | Reads at distance | §4 below: Shot-format images, measured as `data/measure_fix.py.txt` does. The lock matches the study in size and value. Leaf / lock is in the study's veneer-leaf (C) class. |
| (e) | Budget | Per state: LOD0 5,376 tris (chain 5,040 + padlock 336), LOD1 2,432, LOD2 1,452, never culled; eye bolt 528. 7 renderers per EXIT build, 2 drawn (3 draws with the sub-meshes). 2 materials. The mesh cache holds 7 meshes after 6 builds. The per-frame read allocates 0 B over 10,000 calls. Note: this is above the blockout's estimate (§2.3: about 3,500 / 1,000 / 100 for both states and the padlock). One EXIT is in a floor, so the cost is under 6k tris and 3 draws. LOD2 keeps the links because the lock must read at 30 m. |
| (f) | Main's suites in the clone | MapVerification PASS 100/100 seeds (300 EXITs) + start group 1,600/0. MapInteractionTests 359/0. DoorShotTests 252/0 (main's counts: MIT 5b1b9094, DoorShotTests def59d5f). `-autopilotExit` **PASS** with the station on (42 EXIT/STATION rows ok, 0 FAIL) and with it off (27 ok, 0 FAIL). InspectPlaytest 134/0. Counts are unchanged: the package adds no test and no suite reads its objects. |
| (g) | Roslyn | main + package: COMPILE OK on macOS and on iOS, 0 errors, nothing from ExitChain (`W/logs/a13/roslyn_final/`). |

The first B round's two `-autopilotExit` runs (18:08, 18:09) said FAIL. Their only error was the FMOD `BankLoadException`, because the clone had no FMOD banks then (TOOLS.md §2). The banks were copied at 18:10, and the final round (18:44–18:47) passes.

## 3. What changes at landing, and at the switch-on

- **On landing (station still off):** every floor's EXIT is unlocked, so it shows UNLOCKED: the eye bolt beside the latch jamb, the chain coiled on the floor, and the padlock open. This is the decided interim reading (10_directions §7: "reads UNLOCKED"). If Red would rather keep the EXIT bare until the station is on, land this package together with the switch-on instead. No code change is needed for that.
- **What 关卡设计 must flip at the joint switch-on:** one line, `Assets/Levels/FrontRoomsLevel0.asset` → `FrontRoomsLevelProfile.stationModule` = the Front Desk station module (station-look-v1's asset). **Nothing to flip for A13.** It is on from landing and follows ExitLocked:
  - floor 1's EXIT shows LOCKED from its first build;
  - the chain drops at the 0.87 s commit of E · UNLOCK;
  - floors B1+ show UNLOCKED (A35 / Q1 = B; checked on floors 2–4 in the station-on run).
- **Optional asserts for their joint run** (public, tools): `FrontRoomsExitChain.Attach(map)` returns the component.
  - `ShowingLocked` is true on floor 1 until the commit and false after it and on every floor below.
  - `LockSwitches` == 1 after the unlock.
  - `BuildsDressed` == the number of EXIT builds.
- **Keep these map names and values** (the apply script refuses if they change):
  - "Leaf rig", "EXIT crossbar", "EXIT frame " + a + "-" + b;
  - `ExitCrossbarHeight` 1 and `ExitCrossbarLatchInset` .1 (the wrap is built round them);
  - `ModuleUnits.DoorWidth` 1;
  - `ExitDoorBuilt` / `ExitLockChanged` / `ExitLocked` / `IsExitDoor` / `ExitDoor`, and `MapRunStarted`.

## 4. The lock at distance (d)

Ladder: the map's own EXIT door (veneer leaf) in the study's Level 0 ladder hall (tier 5, exit cells held). Desktop is 1920 × 1080; phone is 2622 × 1206 on the handheld path. The study numbers are from 10_directions §5.4 and `data/metrics_fix_ladder_hold.json`.

| LOCKED, 0° | 5 m | 8.6 m | 12 m | 20 m | 20 m 45° | 30 m |
|---|---|---|---|---|---|---|
| lock px, desktop / phone / study | 781 / 963 / 820 | 242 / 305 / 266 | 124 / 158 / 118 | 46 / 57 / 52 | 40 / 48 / 44 | 19 / 26 / 26 |
| lock mean Y, desktop (study A) | .031 | .036 | .047 (.045) | .041 (.053) | .035 | .039 (.092) |
| leaf / lock, desktop | 2.01 | 2.41 | 2.11 | 2.77 | 3.11 | 3.24 |
| leaf / lock, phone | 1.98 | 2.77 | 2.17 | 2.96 | 3.70 | 2.89 |
| study C (veneer leaf) | 2.86 | 2.79 | 3.08 | 3.04 | 3.26 | 2.67 |
| study A (putty steel leaf) | 4.77 | 4.77 | 5.04 | 4.91 | 6.00 | 3.22 |

UNLOCKED leaves 194 px (8.6 m) to 10 px (30 m) on desktop: the coil at the foot of the wall, the open padlock and the eye bolt. Nothing is on the bar.

**What this means:**
- **The lock is as decided.** It covers the study's pixels: −12 % to +5 % on desktop at 5–20 m and −27 % at 30 m (19 px against 26); phone is 0 to +34 %. Its luminance matches the study's (.036–.047 against A's .045–.053 at 12–20 m).
- **The contrast is set by the leaf.** Main's EXIT leaf is the maze's Door_Veneer. It reads Y .12–.13 in this hall, against the study's C veneer at .164 and A's putty steel at .29. So leaf / lock is in C's class: 2.1–3.2 : 1 desktop, 2.0–3.7 : 1 phone.
- **Where it falls short.** It is lower than C at 8.6 m and 12 m on desktop (2.41 / 2.11 against 2.79 / 3.08), because main's leaf is darker.
- **A's 4.8–5.0 : 1 needs A's steel leaf.** That leaf is not in this package (§5).

**Images:** listed in `W/vl_pending/a13_exit_chain.md` (Shot format, "VL (to assign)"). Rule 22: placed after landing, with `a13_d_vs_sw08.jpg` next to SW08 `3527:7362`.
- `W/a13_work/images/a13_ac_play.jpg`
- `a13_d_ladder_{desktop,phone}.jpg`
- `a13_d_full_{desktop,phone}.jpg`
- `a13_d_vs_sw08.jpg`

## 5. Left out, for Stairwell A production

The rescued dresser also did the following. None of it was verified in this pass, so all of it is left out:
- the A steel finishes on the EXIT's cube leaf (putty enamel, #B7B09E, L* 72) and frame (#5F5A50). This is what lifts leaf / lock to A's 4.8–5.0;
- hinge knuckles;
- the kick plate;
- the STAIR A letter plate (push face);
- the NO REENTRY plate and the regular-arm closer (pull face);
- the EXIT sign shadow re-check (the map already sets it);
- the stairwell shell, its lights, rendering-layer separation and reflection probe.

The chain rattle on a locked push and the chain drop on unlock are 声音设计's events (`DoorRattled` and `ExitLockChanged` already fire).

## 6. Notes and limits

- **"First in view" is a frustum test.** On floor 1 the EXIT first entered the frustum behind the shut start door. The proof that matters is that LOCKED was shown from the build frame (f112) and that every later frame was correct. There is no played frame of the locked EXIT seen from the maze at distance; the ladder shows it on the same map door.
- **Phone.** The phone tier is covered by the edit proofs (all 13 phone rows pass; the code has no tier branch) and by the phone ladder. There was no phone play run.
- **A broken locked EXIT** (a Relay break) shows the chain on the floor, as any open EXIT does. Edge case only.
- **History.**
  - Account B built this from 17:00 and ran edit proofs e2, the suites, two play runs without banks, and InspectPlaytest; it stopped at 18:25 (session limit).
  - Account A finished it at 18:4x:
    - re-synced the 4 main files that had moved (KeyHud O-30, settings);
    - fixed a doc comment in the kit (no behaviour change);
    - ran e4 + the two play runs with FMOD banks;
    - rebuilt the package, ran the dry run, scratch apply and Roslyn.
  - The clone's replaced copies are in `W/_partial/proj_a13_resync_1029_1850/`.
