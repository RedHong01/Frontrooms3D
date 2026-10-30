# D1.6 doors · locks · keys — package status: READY (package stage, 2026-10-29 20:0x)

Written by the package stage of x12-d16-fix3 (wf_8e10cad6-53b, 游戏视觉, account A, session local_5656cffd), after the fix:3 record below. Rule 22: no verify stage. This stage ran one fix round on the map contract only, then one test round. Nothing was applied, and LANDINGS / MERGED were not touched.

## Verdict: READY

**Visual package `W/apply/d16.sh`** (this line's one name):
- Rebuilt at 19:41 from the final clone by `W/d16_work/build_package_v5.py`. Bases are recorded on main `1831bded` + working tree (first room on).
- 207 paths: 125 new, 82 replaced, 71 kept `.meta`. The payload is byte-identical to fix 3's: clone = payload = `d16_expected.txt` on all 207 paths.
- The script's header is brought up to fix 3: 207 paths, 91 / 42 checks, the held fill, `ShowTagType` false, 6 SurfaceDefs.
- **DRY RUN OK** on main `1831bded` + tree, re-checked at the end of this stage (§ "Last checks" below).
- Apply: `bash /Users/redwang/FrontRoomsVisualWork/apply/d16.sh --apply`. Run the dry run first, without `--apply`. On a base mismatch, run `W/tools/rebase_apply.sh d16`.

**Map contract: now v9** (`data/contract_map_D16.diff` = `W/apply/d16_contract/contract_map_D16.diff`, README `W/apply/d16_contract/README.md`).
- v9 is v8 with one change. The M3 floor key now **hangs from the station KeySpot's hook**: `SpawnFloorKey` → `NewKey(..., hung: true)`.
- **Why:** this stage rendered the station's floor key at rest, which no fix-3 frame showed (they start at 0.55 s into the take).
  - v8 laid it flat (`KeyPlacement.Floor`) with its root on the KeySpot. That KeySpot is the key's place on the switch plate's hook, 1.42 m up.
  - So under v8 the key was a 4 mm sliver standing in mid-air, with nothing under it for 1,423 mm and its ring 26 mm off the wall.
  - station-look-v1's C-2 / plan P06 ask for D1.6's hung pose on that hook. v9 builds exactly that: ring on the hook, key tip down, 65 mm off the wall.
  - Sheet: `images/d16p_station_floor_key_rest.jpg`.
- Bases MapWorld `73688850` → `44faa325`, DoorShot `ef941ca9` → `e86c3836`, ModuleEditing `ed81b895` → `56bc77cf`. 198 lines added, 9 removed.
- `git apply --check` OK and `patch -p1 --dry-run` OK on scratch copies of main's files; the patched files = the applier (`data/apply_map_hunks_v6.py.txt`).
- Roslyn 6.6 `--all` on main + package + v9: **COMPILE OK on macOS and iOS** (`W/d16_work/pkg/roslyn_v9/`).
- v8 is kept as `data/fix3/contract_v8/contract_map_D16.v8.diff`.

**The test round on v9** (clone = main `1831bded` + tree + package + v9; Unity 6000.6.3f1, 19:52–19:58, UNDER LOAD at 33–103; 0 `error CS`):
- **p02** `StageBatch tests,mapsuites,rebuild`:
  - DoorRig 91 / 91, KeyAssembly 42 / 42.
  - DoorShot plain / doors 252 / 252; MapInteraction plain 373 / 373; rebuild PASS.
  - The dressed variants fail exactly fix 3's §13.3 cube-premise set and nothing else.
- **p03** the game's autopilot through the real station: 41 ok + the §13.3 autopilot check. The hung floor key was taken by E, and the EXIT unlocked with it.
- **p04** the floor-key hand-off: `pose Hung`, root on the KeySpot (0.0 mm), Rect / Red / 00 = `StubIdentity(FloorKeyZone)`. Swap jump 0.23 px.
- **p05 / p06** the floor key at rest, hung (above). Evidence: `data/pkg/`.

## Peer actions (this stage sends no messages; the 游戏视觉 session routes them)

1. **平面视觉 · 信息设计: sign off the 3D key-tag type** (the one peer action of the package). The request text, ready to send:

   > **D1.6 · sign-off request: the 3D key-tag type** (from 游戏视觉)
   > Sheet: `Frontrooms3D/Documentation/research/interactables/d16/images/d16x_tag_type_proof.jpg`. It shows the rect, round and long key tags with the floor tokens 00, B1, B10 and B74, in game light. Type is forced on for the proof.
   > The type follows your rules:
   > - face Courier Prime Regular (#23, Red-confirmed 10-22);
   > - from B10 on, the token is set at 0.8× on the same baseline and never scaled to fit (#33). That now holds on every shape: fix 3 removed the round tag's ×1.20 fit.
   >
   > One thing to judge: on the round tag, B10 and B74 at 0.8× reach the window's edge. The options are:
   > - (a) accept it as is;
   > - (b) a smaller factor for the round tag only;
   > - (c) a wider window in the round tag's insert. That is a model change on our side.
   >
   > What we need: your OK, or the change you want (size, baseline, tracking or ink) per shape.
   > Until then the game ships blank tags (`ShowTagTypeShipped = false`). The floor token appears only on the HUD glyph at the swap. On your OK we flip that one constant and nothing else changes.
   > Not part of this request: the door number plates and signs (`ShowKitType`). They stay held as before (`20_build.md` §12.3).
2. **关卡设计: land contract v9 after `d16.sh`**, together with the §13.3 test changes (README §4 has the current line numbers). Their open rows, none blocking:
   - C-2's "parent under `map.Station`" (optional; the dresser already finds the key);
   - C-3's `StationKeyTaken` timing (it stays at the press; `KeyTaken` moves to 0.25 s);
   - the 36 mm pivot OK.
3. **声音设计: for information only**, `31_for_sound.md`. No sound changes. Once v9 lands, `KeyTaken`, and with it their 2D `KeyPickup` Foley, comes at 0.25 s instead of at the press. `StationKeyTaken` stays at the press. The rig adds picture beats on the door events they already use.
4. **HUD key line (游戏视觉, not this package):** the per-shape O-16 factors and the 5–8° turn (`data/fix3/held_pose_measure_fix3.json`).

## Open decision for the 游戏视觉 session (not Red's; it blocks nothing here)

**The floor key's tag: Rect / Red (now) vs Long / White ×1.5 (station look).** The two decided specs disagree:
- station-look-v1 (`station/30_look.md` S11, `50_critic_fixes.md`, plan P06 / C-2) gives the stair key a **white Long tag at 1.5×**, and keeps red out of the take frame.
- The real key must equal the HUD glyph at the swap (KP-1 / #37). The HUD's `StubIdentity(FloorKeyZone)` hashes to **Rect / Red / 00**, and D1.6's `Stub` follows it. That is a hash result, not a choice.

As built, v9 hangs a Rect / Red tag on the hook. The options, as the player sees them:
- **(a) Keep Rect / Red.** No work. A red tag hangs in the station's take frame, which S11 wanted kept free of red.
- **(b) Long / White at 1×** for `FloorKeyZone` as one special case in both `FrontRoomsKeyIdentity.Stub` (visual package) and `FrontRoomsKeyHud.StubIdentity` (HUD line), with KeyAssemblyTests' 7,203-case agreement kept.
  - The swap stays exact, because Long is already measured at 0.04 px.
  - The tag is smaller than S11's 114 × 48 mm fob.
- **(c) Long / White at 1.5×.** Like (b), plus the HUD scales the floor key's glyph 1.5× at the swap (an O-16 factor) and the held pose is re-measured for it.

**Recommendation: (b).** It meets S11's white Long tag and red-free frame and keeps #37 exact. The 1.5× fob can follow as (c) if the station look still wants it after seeing (b).

**RED_ACTIONS: none.**

## Last checks (20:04–20:05)

- Main `1831bded` + tree. The bases are unchanged: MapWorld `73688850`, DoorShot `ef941ca9`, ModuleEditing `ed81b895`, Game `81fa71aa`, KeyHud `88cc6849`.
- Other lines landed on main at 19:59–20:01: Game.Baseline, RoomStream, ScreenVideo, Input, KeyBindingTests. None of them is a D1.6 or contract base.
- `bash W/apply/d16.sh`: **DRY RUN OK**. All 207 bases match; 71 kept `.meta` unchanged; 207 payload files match; 58 new GUIDs unused.
- Roslyn main (as of 20:04) + package + v9: **COMPILE OK on macOS and iOS** (`W/d16_work/pkg/roslyn_v9_2004/`).
- Details: `20_build.md` §14.

---

# Fix 3 record (2026-10-29 19:3x)


Written by the fix:3 stage of x12-d16-fix3 (wf_8e10cad6-53b, 游戏视觉, account A, session local_5656cffd), which continued account B's fix:3 (wf_06d6fb65-d5b, stopped at B's session limit 18:25) and redid nothing finished. COMMON rules 22 / 23: no verify stage; the gate is Roslyn macOS + iOS and the suites this change can break, all run on the final code below. Full record: `20_build.md` §13.

## The blockers of the 17:0x record, now

| Blocker | Now | Evidence |
|---|---|---|
| **S1** real key ≠ HUD glyph at the swap (64 px, Round unmeasured) | FIXED. Per-shape held offsets in `FrontRoomsKeyAssembly.PoseHeld` (2–3 mm in the held frame, depth unchanged); fix 2's 64 px was mostly the harness (4:3 batch screen vs a 16:9 capture), now pinned | Centroid jump at the swap, all three shapes: **desk 0.19 / 0.10 / 0.04 px, phone 0.85 / 0.82 / 0.72 px** (Rect / Round / Long); final code, station floor key 0.24 px. Sizes within ±5 % except the Round tag's height (−7 %), which is the HUD's O-16 factor to absorb (`data/fix3/held_pose_measure_fix3.json`). `images/d16x_hud_swap_desk.jpg`, `d16x_hud_swap_phone.jpg` |
| **S2** tag type unsigned; round B10+ ×1.20 vs #33 | FIXED. `ShowTagTypeShipped = false` (blank tag until signed off); B10+ at 0.8× on the same baseline on every shape (#33); face Courier Prime Regular (#23) | KeyAssemblyTests shipped 12 / 12 blank, proof 12 / 12 at crop ×1; proof sheet **`images/d16x_tag_type_proof.jpg`** |
| **E1** contract v7 failed on MapWorld; the floor key stayed a cube | FIXED. **Contract v8** `data/contract_map_D16.diff`: M3 in `NewKey` (zone keys and the M3 floor key), no 6 cm lift for a dressed root in both callers, `SpawnKeyHost` re-anchored; plates via `KeyZoneAt` (payload) | On main's MapWorld **`73688850`** (→ `be86cef7`; first room on, C-1), DoorShot `ef941ca9`, ModuleEditing `ed81b895`: `git apply --check` OK, `patch --dry-run` OK, = applier. Roslyn main + payload (+ v8) macOS and iOS: COMPILE OK. Station floor: the dressed floor key at the KeySpot (0.0 mm), Rect / Red / 00 = the HUD's `StubIdentity(FloorKeyZone)` |
| **E2** map suites not on the final code | PASS, except checks that assume the cube (listed for 关卡设计, `20_build.md` §13.3) | z01: DoorRigTests 91 / 91, KeyAssemblyTests 42 / 42; DoorShot plain / doors 252 / 252; MapInteraction plain 373 / 373; rebuild PASS. z02 CxSuites all PASS (MapVerification 100 / 100, LevelDesigner 146 / 146, FixtureTick 255 / 255, RelayNav 60 / 60 …). z05 station-on floor (clone only) 41 ok + 1 cube-premise check; the EXIT unlocked with the dressed floor key. z06 floor-key hand-off PASS. z03 iPhone touch playtest 0 failures (98 frames); z04 120 s play 0 errors / 0 exceptions / 0 pink. **Again on main with the first room switched on (19:17) and C-1 (19:22)**: f01 the same results as z01; f02 the game's autopilot through three EXITs with the real Station_FrontDesk_4x4: 41 ok + the same cube-premise check, the EXIT unlocked with the dressed floor key 5.7 s after the take; f03 the real station's floor key hand-off PASS, jump 0.23 px |
| **L1** held brass 0.070 linear at a level look | FIXED. Held fill in `FrontRoomsHeldItem` (0.05, range 0.6 m, only while a key is held, off on WebGL) | Level look **0.123 linear (−0.33 st vs behind)**, down look 0.217; metallic 0.7 reached only 0.095. `images/d16x_held_fill_sheet.jpg` |
| **L2** no hung-key lift | DONE (evidence) | Off a key-rack hook through the real presenter: 0 frames with the ring inside the hook after t0. `images/d16x_hook_lift_sheet.jpg` |

## Package

- **`W/apply/d16.sh`** (this line's one name; not `d16_doors_keys.sh`): 207 paths (125 new, 82 replaced), 71 kept `.meta`; **DRY RUN OK** (19:37, re-checked after the last Roslyn at 19:38) on main `1831bded` + tree (first room switched on).
- Apply: `bash /Users/redwang/FrontRoomsVisualWork/apply/d16.sh --apply` (dry run first without `--apply`; on a base mismatch `W/tools/rebase_apply.sh d16`).
- Visual-owned files only. Contract v8 is 关卡设计's to land after it, with the §13.3 test changes; until then main keeps its cubes.

## Peer and Red actions

- **Peer (the one action): 平面视觉 · 信息设计 signs off the 3D key-tag type** on `images/d16x_tag_type_proof.jpg`; then `ShowTagTypeShipped = true` (one constant). Routed by the 游戏视觉 session; this stage sends no messages.
- **关卡设计:** contract v8 + the §13.3 test changes, after the package lands.
- **RED_ACTIONS: none.**
- VL rows: batched after landing (rule 23). New sheets `images/d16x_*` (5). VL317–VL324 were reserved in 2026-10; read `Documentation/VERIFICATION_LOG.md`'s next free number before placing (VL452 at 18:40).

---

# History

## D1.6 doors · locks · keys — package status: BLOCKED (2026-10-29 17:0x)

Written by the Package stage of workflow x11-d16-doors-locks-keys (wf_ab7fda06-341, 游戏视觉, account B, session local_a9a1dce2), 2026-10-29 17:0x PDT, after verify on the fix-2 build.

Nothing new was packaged. This stage wrote no `d16_doors_keys.sh`, no `contract_map_D16.md`, no `31_for_sound.md` and no VL rows. The fix-2 package `W/apply/d16.sh` and the contract `data/contract_map_D16.diff` (v7) are unchanged. The record of 2026-10-22 06:51 is kept below as history.

## Verdicts (verify after fix:2)

| Lens | Verdict |
|---|---|
| spec | NOT READY |
| engineering | NOT READY |
| look | NOT READY |

All three lenses are NOT READY, so the rule applies: do not package; write the blockers here; return BLOCKED.

## Re-checked on disk, 17:03–17:06

- `bash /Users/redwang/FrontRoomsVisualWork/apply/d16.sh` (plain dry run): **DRY RUN OK** on main `c5e03368` + tree, which now holds 关卡设计's first-room group (16:58): MapWorld `6bbc3341`, FrontRooms3DGame.cs `2e93c426`. The visual package itself still applies.
- Contract v7 (`data/contract_map_D16.diff`, bases MapWorld `527ddb39`, DoorShot `ef941ca9`, ModuleEditing `ed81b895`): `patch -p1 --dry-run` on a scratch copy of main's three files. **MapWorld: 2 of 4 hunks fail.** DoorShot and ModuleEditing apply. Main's MapWorld now makes every key through `NewKey` (line 5053). Its callers are `SpawnKey` (5030) and `SpawnFloorKey` (5081).
- Payload `Assets/Scripts/Office/FrontRoomsInteractableKit.Door.cs:87`: `ShowKitTypeShipped = false, ShowTagTypeShipped = true`.
- `data/fix2/held_pose_measure.json` measures one shape only (Rect, edit mode): centre x 1262 against the hand point 1248; 200 × 123 px against the 3× glyph's 216 × 144.
- Visual-line Unity jobs: 0. The 1-minute load was 40.6 at 17:03, so any timing taken now counts as UNDER LOAD.

## Blockers

### spec

**S1. The real key does not hand off to the HUD key at the same spot (KP-1 / DECISIONS #37).**
- The verifier measured the raw Play frames of run k03 (`proj_d16/Verification/d16k_hud/1920x1080/lit/pickup/`, Long / White / 00).
  - The last 3D frames centre at about (1182, 743).
  - The first HUD frames centre at about (1245, 741), which is on the hand point (0.65, 0.30).
  - So at the swap the key jumps about 64 px to the right, about 30 % of its width. The sizes match within 3 %.
- Shapes come from the zone hash, so about one key in three (Long) jumps. Round was never measured.
- `20_build.md` §12.1 S1 ("same spot and size on the swap frame") and the sheet label "HUD KEY, SAME SPOT" (`images/d16h_hud_pickup_sheet.jpg`) are contradicted by these frames.

**S2. The printed key-tag type ships without 平面视觉's sign-off, and the round-tag B10+ rule contradicts DECISIONS #33.**
- `ShowTagTypeShipped = true` prints the floor token on every tag. `10_design.md` §9 sends type sign-off to 平面视觉 before landing. The proof `images/d16h_type_proof_tags.jpg` is itself captioned "For their sign-off". No sign-off on the 3D tag is recorded in `handoff/graphic.md` or `GRAPHIC_VISUAL_TASKS.md`.
- #33 (平面视觉, 10-22): from B10 on, the token is set at 0.8× size on the same baseline, never scaled to fit. The build scales round-tag B10+ into the window (×1.20) instead.
- Face: the build's Courier Prime Regular follows #23 (Red confirmed, 10-22), which supersedes G12's Bold. Nothing to change here, but the record should say so.

### engineering

**E1. The map contract v7 no longer applies to main.**
- See the re-check above. Hunk 1 (about line 2325) fails because the call-site `if` gained `!floorKey`. Hunk 4 (about line 4994) fails because `SpawnKey` builds its cube through `NewKey` now. Hunk 3 only lands with fuzz 2. The anchor applier `data/apply_map_hunks_v4.py.txt` stops with "anchor count 0".
- There is also a functional gap. Main's M3 floor key ("Key · floor") is built by `SpawnFloorKey` through `NewKey`, and station floors spawn no zone keys. With v7 that floor key stays a glowing cube, so on station floors the real key for P4 / #37 never appears. Main's own comment says D1.6's key model replaces the cube through `NewKey`.

**E2. The map suites have not run on the final code.** The last `mapsuites` runs (i01 / i02, `20_build.md` §11) were before the fix-2 changes. Note: the verifier's text for this item was cut off in the stage input; this summary is what the files show.

### look

**L1. The held key reads dark brown, not brass, at a level look.**
- In `proj_d16/Verification/d16kcap/fix2_held_050.jpg` the key bow at the hand averages sRGB (78, 60, 27), which is 0.070 linear. The same key on the floor (`close_key_Flat.jpg`) averages (211, 185, 114), pale brass.
- The round-2 bar was 0.10 linear. Fix 2 met it only by replacing it with a "within 1.5 stops of what is behind it" rule.
- The Play evidence (0.23 linear) comes from a floor pickup with the camera pitched down. A13 hangs each floor's one key at the station, at about eye height, so the decided P4 pickup mostly happens at a level look, which is the case that reads brown.

**L2 (evidence gap, not a blocker today).** No capture shows a key taken off a hook (the A13 case). When a hung key is lifted, `FrontRoomsKeyAssembly` blends its parts from `LayHung` (ring yawed 120° and lifted) while the map's presenter moves the root straight to the hand. Whether the ring passes through the hook lip is unverified. It cannot happen in game until M3 hosts and A13 land.

## What the next fix round must do (one round, then one verify round, then package)

1. **Contract v8 on MapWorld `6bbc3341`** (and on Game.cs `2e93c426` if a hunk needs it).
   - Put the dressed-key hook in `NewKey`, so zone keys and the floor key both get the kit.
   - For a dressed root, drop the cube's 0.06 m lift in both callers (`keyY + .06f` in `SpawnKey`, `at + Vector3.up * .06f` in `SpawnFloorKey`). The dressed root sits on the surface point.
   - Keep `SpawnKeyHost` on every build, re-anchored to the new `if (data.hasKey && !floorKey && ...)` line.
   - Check with `git apply --check` and `patch --dry-run` on scratch copies. Then run Roslyn on main + payload + v8 for macOS and `--ios`.
   - Re-run the real-key hand-off with a station floor on. The floor key's identity already agrees with the HUD (both hash `FloorKeyZone`).
   - Optional: the door plates' FaceIdentity stand-in can use main's `KeyZoneAt(cell)`.
2. **S1 fix.** In `FrontRoomsKeyAssembly.PoseHeld`, use per-shape held offsets so that each shape's pixel footprint is centred on the `KeyHandoff` viewport point (as `held_arrangement.json`'s glyph anchor (36, 24) intends).
   - Re-measure Rect, Round and Long in Play at the swap. Pass: centre within a few px, size within ±5 %.
   - If sizes still differ, give the HUD one O-16 factor per shape.
   - Correct the §12.1 S1 claim and the sheet label.
3. **L1 fix (visual-owned), one of:**
   - a weak held-key fill on the HeldItem layer (like V5);
   - `Prop_BrassSatin` metallic about 0.7 with a warmer base colour;
   - a tilt of about 30° on the held arrangement so its face catches the ceiling.

   Pass: the level look and the down look both reach at least 0.10 linear and read as brass. If the held pose changes, redo `data/fix2/held_pose_measure.json` (O-16) together with item 2.
4. **S2 fix.** Set round-tag B10+ to #33 (0.8× on the same baseline). Rebuild the proof sheet and record #23 (Regular) as the face rule. The 游戏视觉 session routes the rebuilt proof to 平面视觉 · 信息设计 for sign-off.
   - Until that sign-off is recorded, ship `ShowTagTypeShipped = false`, as the plates already do. The trade-off: the token then appears only on the HUD glyph at the swap.
   - Or keep `true` only with the sign-off recorded in `20_build.md` and the graphic handoff.
5. **E2.** Run the map suites (plain and dressed variants) on the final code with contract v8.
6. **Optional (L2).** Capture a hung-key lift (rack hook → hand) to check the ring against the hook lip.

When the package is built: the task names `W/apply/d16_doors_keys.sh`, but this line's package is `W/apply/d16.sh` (v2 convention). Keep one name and say which one in the report. VL317–VL324 were reserved in 2026-10; the VL counter has moved past VL440 since. Re-read `Documentation/VERIFICATION_LOG.md`'s next free number before placing any rows from `W/vl_pending/d16.md`.

## Peer and Red actions

- **Peer (平面视觉 · 信息设计, routed by the 游戏视觉 session; this stage sends no messages):** sign-off on the 3D key-tag type after the #33 fix (item 4).
- **RED_ACTIONS: none.** Every fix is visual-owned or contract text for 关卡设计.

---

# History

> **SUPERSEDED 2026-10-22 17:2x.** The Build stage re-run (account B, session local_a9a1dce2) built and packaged D1.6: see `20_build.md` (status READY: `W/apply/d16.sh` dry run OK; the map contract `data/contract_map_D16.diff` for 关卡设计). The text below is the 06:51 record of the first run, kept for history.

# D1.6 doors · locks · keys — package status: BLOCKED

Written by the Package stage of workflow wf_c071884a-103 (游戏视觉), 2026-10-22 06:51 PDT.
Nothing was packaged. No apply script, contract or VL spec was written.

## Verifier verdicts (round 2)

| Lens | Verdict |
|---|---|
| spec | NOT READY |
| engineering | NOT READY |
| look | NOT READY |

All three lenses are NOT READY, so the packaging rule applies: do not package. The rule says to write the blockers here and return BLOCKED.

## Cause

Every stage before fix:2 failed on a network outage. Each one ended with "API Error: Can't reach the API server ... (ENOTFOUND)". The journal is `~/.claude/projects/-Users-redwang-Desktop-ArtCenter-Fall26T7-EGAM-401A-01-Individual-Game-Project/c662b9be-caf5-4011-a0a6-49417021dfbd/subagents/workflows/wf_c071884a-103/journal.jsonl`. These stages are all marked `failed`:

- design
- build
- capture
- verify ×3, round 1
- fix:1
- verify ×3, round 2

Design only read files and wrote nothing. fix:2 ran at 06:32 with the API reachable again. It found no build to fix and changed nothing.

## Blockers (re-checked 06:50 PDT)

1. **No clone.** `/Users/redwang/FrontRoomsVisualWork/proj_d16` does not exist, and no Unity process is running on it. The only Unity process is the map chat's `~/FrontRoomsMapWork/proj2`.
2. **No visual-side code.** None of these exist anywhere, in main or in the clones:
   - `FrontRoomsDoorRig` on "Leaf rig" (IFrontRoomsDoorRig)
   - `FrontRoomsKeyAssembly` on "Key · zone {id}" (IFrontRoomsKeyRig)
   - the V3 HeldItem layer and the desktop pass with the 1.5 × HeldDistance depth rule
   - the V4 post driver: it reads FrontRoomsDoorShot.Weight and FrontRoomsGlassShot.Weight and forces LOD0 on the shot door
   - the V5 0.6 shot light during Unlock
   - the V6 notes
3. **The 3 RD10 materials are still missing.** `Door_Enamel`, `Prop_KeyTagNo` and `Prop_SignEngraved` are not in `Assets/Resources/Surfaces`. There is no RenderSetup SurfaceDefs hunk.
4. **No map-side contract.** No unified diff exists for any of these:
   - DressDoor returning "Leaf rig"
   - the "Key · zone {id}" hookup
   - the shot weight reads
   - the A2 hinge-pivot fix

   That also means no base sha1 re-check (MapWorld c0946262, CameraRig a77221fb, ShotTimings 865e3e10) and no `git apply --check`.
5. **No checks.** There is no compile log, GUID collision check, Roslyn check (macOS and --ios) or map-suite pass.
6. **No evidence.** This folder held no 20_build.md and no images before this file. `/Users/redwang/FrontRoomsVisualWork/vl_pending/d16.md` does not exist. VL317–VL324 stay reserved and empty.
7. **Red's A09 / SD04 decisions are unverified.** None can be checked against frames or logs:
   - doors swing in
   - 0.45 m pull step-back
   - deadbolt + knob
   - an unlocked door looks locked but opens on E
   - clear Lobby glass
   - bent-slat blinds
   - LOD0/1/2
   - dark-bronze Run frame
   - "key rack"
   - T trims
   - C14 look + E
   - C15 1 in 3
   - broken stays broken
   - B·4
   - no motor or closer

   The same goes for the 10_spec anchors and budgets, the KP-1 / DECISIONS #18 / #20 seam, and G12 Courier Prime Bold.

## Machine state

At 06:50 the load average was 135.72 / 192.96 / 337.78 (1, 5 and 15 min). The 1-minute figure is above 32, so any timing taken now counts as UNDER LOAD. The main load is macOS StorageManagement: ApplicationsStorageExtension at about 234 % CPU, StorageManagementService and CacheDelete `deleted`.

## What unblocks it

1. Stop this run.
2. Relaunch the d16-doors-locks-keys workflow from Design with the same inputs and the same VL317–VL324 range.
3. Build must create `proj_d16` with `mkclone.sh` and compile with 0 'error CS'. It must:
   - implement the D1.6 row of VISUAL_CHAT_TASKS.md §0a′ on visual-owned paths only
   - add the minimal SurfaceDefs hunk for the 3 materials
   - write the map contract diff, including the A2 hinge-pivot fix, against main's current sha1s
4. Capture and Verify then run on real frames and logs.
5. At package time, re-base onto outlets_p4b and w2_office_props with `tools/rebase_apply.sh`.

If possible, relaunch once StorageManagement load drops.

This stage created no clone, so it cannot collide with the relaunch.
