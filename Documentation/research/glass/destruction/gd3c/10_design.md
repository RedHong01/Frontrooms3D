# GD3c: broken glass stays on the floor, and a knock is never undone (diagnosis + design)

游戏视觉 workflow `glass-debris-progress`, diagnose-and-design stage, 2026-10-22 13:05–13:45. Clone `W/proj_gd3c` (main `2c450e11`, synced 13:08). Nothing in main was changed except this folder.

**Red, 2026-10-22 13:0x (游戏视觉 chat):** "当前被击碎的玻璃首先应该留在地面上而不是消失（除非玩家走的离这个地方太远，则为节省内存会把玻璃这种场景破坏的模型给清除）但是如果玩家没有走太远它应该会保持在地上，第二点就是，当前玩家如果敲玻璃，如果比如说没有长按足够多的秒数，玻璃会保留被敲碎的进度，而不是恢复成普通的玻璃。"

In short:
1. Broken glass must stay on the floor. It may be cleared only when the player is far away, to save memory.
2. A hold that ends early must keep its progress. The pane must not go back to plain glass.

Files in this folder:

| File | What |
|---|---|
| `10_design.md` | This document |
| `CONTRACT_map.diff` | The map-owned hunks for 关卡设计 (D-c). `git apply --check` passes on main `2c450e11` |
| `README_CONTRACT_map.md` | Why each hunk exists |
| `images/VL359–VL364_*.jpg` | Verification images in Shot format. Their rows are in `W/vl_pending/glass_debris.md` |
| `data/` | Harness logs and records for both runs (`base_*` is main as it is; `kept_*` has the contract applied in the clone), plus the map test result |

---

## 0. Short answer

**Both findings are confirmed, with numbers.** One correction matters for the design (D-d, §5).

- **Shatter.** No `ShatterHandler` is registered in main. On the shatter frame, `FrontRoomsGlassBreakable` switches off everything it draws, and nothing takes its place. At +0.1, +1, +3 and +10 s:
  - 0 renderers lie low within 2.5 m of the window;
  - 0 particle systems are playing;
  - under the window root, only the wooden frame draws.
  - See VL361. Red's screenshot is exactly this.
  - When the chunk is rebuilt, a stage-3 window shows its frame and nothing else.
- **Progress.** `ReleaseHold` resets the hold to the last beat. Measured, the hold after letting go:

  | Held | Kept after letting go |
  |---|---|
  | 0.20 s | 0.000 |
  | 0.50 s | 0.350 |
  | 0.80 s | 0.700 |
  | Three holds of 0.20 s on one pane (0.60 s in all) | 0.000 each time; the pane stays plain glass |
  | 0.50 s, then 0.30 s | 0.350, then 0.650 → 0.350; stays at S1 |

  A chunk rebuild also resets the hold to the last beat, and only stages 1 and 2 come back. See VL359, VL360 and VL362.
- **Why Red sees "plain glass" even after S1.** After a 0.50 s hold, the S1 crater and its one visible silver ribbon cover **74 px** of a 1080p frame at 1.2 m, face-on (VL364). From where the player stands, a cracked pane therefore reads as plain glass. Below 0.35 s nothing at all is left.
- **The contract works.** The map contract (`CONTRACT_map.diff`: 5 code changes, plus 2 test edits and 1 new test) was applied in the clone. Results:
  - every second of holding is kept;
  - three 0.20 s holds reach S1 on the second press;
  - 0.50 + 0.30 s reaches S2;
  - a rebuild restores 0.200 / 0.500 / 0.800 / 0.600 / 0.800 exactly;
  - the impact no longer drifts between presses (it moved 1.335 → 1.407 m without the contract);
  - the map's own `FrontRoomsMapInteractionTests`: **176 passed, 0 failed** (VL360, `data/kept_*`).
- **The debris world mostly exists already.** It is the 10-14 draft `FrontRoomsGlassDebrisWorld` v1 in `W/proj_gd3`. It never landed: the weekly limit stopped GD3 after step 5. In its lab run (10-14 18:12, under load 134):
  - 147 pieces become 114 shards and 32 teeth;
  - they move in a separate physics scene, which the gameplay rays cannot see (9/9 isolation tests, and E1/E2/R1/N1/Z1 120/120);
  - everything settles 1.76 s after the shatter;
  - the result merges into floor glass of 10,284 triangles (LOD1: 766) plus teeth of 1,456 triangles, 1,392 KB (VL363).
  - This design keeps v1 as the base for desktop. It fixes v1's four measured defects and adds what v1 lacked: persistence (D-b), the distance and memory rules (D-a), and arming for kept progress.
- **D-d needs a correction.** Under Design 2 ("Brace, strike, flinch"), **no strike reaches the glass before 0.35 s**. The time from 0 to 0.29 s is the brace and wind-up, 0.29–0.35 s is the first strike in flight, and that strike lands *at* Crack1. So "a bruise from the first strike" is just S1, which is already permanent. What can honestly persist below 0.35 s is the brace's contact: a sleeve wipe in the pane's dust. What makes kept progress visible is S1 legibility (§5).

---

## 1. Reproduction (Play mode, the game's own E path)

**Harness.** `W/proj_gd3c/Assets/Editor/Audit/FrontRoomsGd3cRepro.cs` (clone only, ASCII, no tuples; a copy is in `W/gd3c/harness/`).
- It starts a run through the title, with seed 516574485 and the Relay held dormant.
- It stands 0.9 m from a Level 0 pane, aimed 1.3 m up, as the editor autopilot does.
- It presses E through `FrontRooms3DGame.EDown/EHeld`, using the autopilot fields. So `UpdateAim` → `BeginGlass` → `map.Hold` → `ReleaseHold` run exactly as they do for a key, including the step-in and the shot.

**Run conditions.**
- Captures are 1920 × 1080 with 4× MSAA through the game camera and its post stack.
- Fixed views use the same camera, moved for that one render.
- Each game frame is 1/60 s.
- `-frGlassRT off` was used, because the G14 track is mid-fix in another clone. G14 does not change anything measured here.
- Runs: base 13:21–13:23 (97 s, start load 25); kept rev 1 13:27–13:28 (67 s); map tests rev 1 13:31 (23 s, load 107: **UNDER LOAD**).
- Contract rev 2 (13:4x): the progress is written on release, pause and teardown instead of per tick (关卡设计's note). It was re-verified with map tests at 13:42 (176/176, load 269–289: **UNDER LOAD**) and the kept run at 13:42–13:44 (load 132–269: **UNDER LOAD**). The holds were identical to rev 1. `data/kept_*` and VL360/VL362 are from the rev 2 run.

**Windows.** All are Level 0, Low ↔ Tall:
- A1–A3 and C are in chunk (35, 25);
- B1–B2 are in chunk (35, 26).

### 1.1 Holds (`data/base_repro_log.txt`, `data/kept_repro_log.txt`)

| Case | Frames held | `window.hold` while held | After letting go: main today | After letting go: with the contract | What the pane shows after letting go |
|---|---|---|---|---|---|
| A1 0.20 s | 12 | 0.200 | **0.000** | 0.200 | S0, plain glass. The bow (0.17 mm at the release frame) springs flat in 0.2 s |
| A2 0.50 s | 30 | 0.500 | **0.350** | 0.500 | S1: fins and crater, 290 tris |
| A3 0.80 s | 48 | 0.800 | **0.700** | 0.800 | S2: 8,308 tris |
| B1 3 × 0.20 s | 12 + 12 + 12 | 0.200 each time | **0.000 after every press** | 0.200 → 0.400 (S1) → 0.600 | Today: plain glass after 0.60 s of holding. Kept: S1 |
| B2 0.50 + 0.30 s | 30 + 18 | 0.500, then 0.650 | **0.350, 0.350** | 0.500, then 0.800 (S2) | Today: S1. Kept: S2 |

More findings from the holds:
- **The impact moves between presses at stage 0 today.** B1's impact went from y 1.335 to 1.407 m on the second press, because the step-in moves the eye. With the contract it stays at 1.335 m.
- **The pattern costs** 2.7–8.7 ms CPU, plus 2.6–4.0 ms for the meshes. The worst slice is 1.6–2.2 ms. Nothing was forced (no `ForcedAt`), because every pane's pattern was still alive from its first press (§6.4).
- **Shatter.** The pane goes on held frame 60 (59 + the press frame, 1.000 s). On that frame and at +0.1 / +1 / +3 / +10 s the breakable reports `GD3 S3`, with no renderer of its own on.

### 1.2 Walking away and back

| Step | Main today | With the contract |
|---|---|---|
| Walk away, one cell every 6 frames | The shattered window's chunk was released at a player distance of **57.0 m** (player chunk (32, 25), window chunk (35, 25)). The other tracked windows went at 63–74 m, on the same frame | Same |
| Back after 3.4 s (no shift) | A1 0.000 (S0) · A2 0.350 (S1 restored at once) · A3 0.700 (S2) · B1 0.000 · B2 0.350 · C: frame only | A1 0.200 · A2 0.500 · A3 0.800 · B1 0.600 (S1) · B2 0.800 (S2) · C: frame only |
| Back after 40 s away (shiftAfterSeconds 30) | 20 chunks shifted. A1–A3 and C are still windows. **B1 and B2's edges are no longer windows** (chunk (35, 26) came back rearranged), and their records stay behind as orphans | Same, with the kept holds |

**What this means for D-a and D-b.**
- The map releases a chunk only when the player's chunk is more than 2 away on either axis. Straight-line, that is **48–96 m** along an axis and up to about 136 m on a diagonal.
- It rebuilds the chunk when it comes back within 2 chunks. That is at least 48 m, which is beyond the camera's far plane: `SightDistance` = 2 × 24 − 2 = **46 m**.
- So a release-based clear can never happen in view, and a rebuild has at least 2 m of walking before the glass could be seen.
- A chunk left for 30 s or more comes back shifted. A broken window may survive (zone-border windows did) or vanish.

---

## 2. Debris world, desktop (the reference, highest spec)

**Base: v1** (`W/proj_gd3/Assets/Scripts/Rendering/Glass/FrontRoomsGlassDebrisWorld.cs` 1,521 lines, `FrontRoomsGlassFloorGlass.cs` 239, `FrontRoomsGlassDebris.cs` 170, `FrontRoomsGlassShard.cs` 20, `FrontRoomsGlassVfx.cs` 590, plus `FrontRoomsGlassBreakable.ReleaseWorkingSet()`). All of these are visual-owned paths. Against main's current breakable, v1 differs by that one method only, and `FractureMeshes` is byte-identical.

### 2.1 Local physics scene or scripted ballistics

**Use the local physics scene on desktop** (plan §4.3, option B).
- `SceneManager.CreateScene(..., LocalPhysicsMode.Physics3D)` creates it, and only `PhysicsScene.Simulate` steps it, in fixed 1/120 s substeps (at most 8 per frame).
- Every gameplay query uses the static `Physics.*` calls with `~0`. Those read only the default scene, so no shard, tooth or proxy can block the E ray, the Relay's sight or nav probe, or the climb. v1's lab proved it: isolation 9/9, and E1 pane hit 60/60, E2 E-ray through the moving debris 120/120, R1 Relay sight 120/120, N1 nav capsule 120/120, Z1 no default-scene collider in the opening 120/120.
- Scripted ballistics (option C) cannot pile shards, slide them against furniture, or give real contacts for the sound chat's `ShardImpact`. It stays the reduced tier (§3).

**What goes into the debris scene.**
- One convex hull per shard: the un-bevelled prism at its stage-2 pose. Mass = area × 6 mm × 2,500 kg/m³. `contactOffset` is 0.002, and detection is `ContinuousSpeculative`.
- The chunk's collision cropped to a 6 × 3.2 × 6.4 m box round the window, plus the boxes of nearby furniture and doors, plus a thick safety floor (v1: 4 proxies, 356 cropped tris).
- The teeth as static hulls.

### 2.2 Release waves (v1 = plan §2.3, unchanged)

1. On the shatter frame, the shard renderers and the teeth come on at exactly the stage-2 pose, so there is no gap and no ghost.
2. The glass holds still for the shot's object stop: `GlassBreak.ObjectStop[2]` = 50 ms.
3. Then it lets go in waves:
   - inner shards (r < 0.15 m) at once;
   - the middle band 30–120 ms later;
   - the outer, non-tooth shards 120–300 ms later, tipping out of the stop.
4. Speeds:
   - each wave leaves at 2–4 m/s × e^(−r / 0.25 m), along the pane normal toward the far side, with 0.18 of radial spread;
   - the minimum tip-out is 0.12–0.35 m/s.
5. Backward fragmentation: 6 / 22 / 48 % of the inner / middle / outer shards fall toward the breaker instead.
6. `IGlassShotClock.ShardTimeScale` (the shot's shard-only slow-motion, an A/B switch, off by default) scales only the debris step. When the shot is demoted or skipped, the rate is 1.

v1 measured these results:
- 63.1 % of the area and 92 of 114 shards landed on the far side. The plan asks for "about two thirds" (V8).
- 820 `ShardImpact` events were raised, and the 16-per-frame cap dropped 147 more.

### 2.3 Teeth left in the stops

- The generator's tooth pieces (32 in the lab, 25–43 for Annealed6) become one static mesh on the window root from the shatter frame on.
- It is render only, at rest pose, inside the tooth band: jambs X ±0.600–0.6835, head Y 1.900–1.9835, sill Y 0.3665–0.390. The stops hide the roots.
- On `PlayerClimbed`, the bottom teeth snap off. v1 smoke: 7 teeth, 1,456 → 1,136 tris, `TeethSnap` raised once.
- Persistence keeps a 64-bit snapped mask (§4).

### 2.4 Floor glass, settle, merge

**Settle.**
- A shard freezes after 0.3 s below 0.05 m/s and 0.6 rad/s.
- From 1.5 s after the shatter (`GlassBreak.SettleToStatic`), anything below 0.15 m/s and 1.5 rad/s freezes.
- Everything freezes at 2.5 s at the latest.
- v1 settled at 1.758 s (by the cap), after 205 steps.

**Merge.**
- Every shard at its settled pose, plus the resting crumbs, merges into **one static mesh** on the window root, in the root's frame, with an LOD1 that keeps the pane faces only.
- On the frame the floor glass appears, the shard objects go back to the pool, the proxies go, and `ReleaseWorkingSet()` returns the breakable's 2.46 MB working set and 0.95 MB of stage meshes.
- Floor glass has **no collider** in the game's physics, so it never blocks the player or the Relay and never enters nav. The map's passage and path field do not change: the opening is already open once `brokenWindows` holds the edge.
- Material: the shared `Glass_Fracture` (Glass_Window + `_PaneFromUV`) with `FrontRoomsMetalGlassTarget`.

**v1 defects to fix before it lands (measured on 10-14, UNDER LOAD 134; re-measure on a quiet machine).**

| # | Defect | Measured | Fix |
|---|---|---|---|
| F1 | Build spike | 58.1 ms CPU over 14 frames, **worst frame 11.1 ms** against a 1.5 ms budget. `Physics.BakeMesh` runs on the main thread (DebrisWorld.cs:624, :747) | Bake hulls in an `IJobParallelFor` (`Physics.BakeMesh` is thread-safe), started at `MeshesReady`. The main thread then only creates components: ≤ 1.5 ms per slice |
| F2 | Merge spike | 6 frames, 20.4 ms, **worst 12.9 ms** | Build the merged mesh with `Mesh.AllocateWritableMeshData` in a job, and swap it in with `ApplyAndDisposeWritableMeshData`: ≤ 1 ms on the main thread |
| F3 | V9 FAIL | **312 of 21,582** floor-glass vertices inside the clear zone (shards resting in the opening on the sill) | After the settle, drop any shard whose bounds enter X ±0.60 × Y 0.39–1.90 × \|Z\| < 0.05 to the floor below, or turn it into a crumb cluster |
| F4 | Upright white shards at the wall base (C4 frame, VL363) | By eye | Hypothesis, to verify: the S2 crush rim (`_Crush` = 1, in pane UV round the impact) is carried onto shards much larger than the ≤ 15 mm crater. Limit the crush to crater pieces after the shatter |
| F5 | Floor triangles over budget | 10,284 against the plan's 9k | Drop the bottom face and bottom bevel of shards lying within 10° of flat (invisible on carpet). That is about 30 % of their vertices, for **~7.5k tris** |
| F6 | Memory | 1,392 KB GPU, plus the same again as a readable CPU copy | Make the meshes non-readable after the merge, unless G14 P4 needs them readable (note for G14). Optional 44 B vertex layout (normal SNorm16, uv0 half): **≤ 1.0 MB GPU, no CPU copy** |

### 2.5 Particles and sound (v1 `FrontRoomsGlassVfx`, plan §6.5)

- **Counts in the v1 lab:** S1 23, S2 28, shatter 230, plus 20 at the teeth snap.
- **Hooks for 声音设计** (FYI only, no new beat):
  - `FrontRoomsGlassDebris.ShardImpact(pos, mass, speed)` and its detailed form with the surface, for the muffled-carpet rule;
  - `TeethSnap(pos)`;
  - `IsGlassAt(pos, r)` for glass footsteps, a 5 cm cell hash (445 cells in the lab).

---

## 3. Reduced tier: WebGL and iOS

| | Desktop (reference) | WebGL / iOS |
|---|---|---|
| Gate | none | `FrontRoomsGlassTypeProfile.ForTier`: `#if UNITY_WEBGL && !UNITY_EDITOR` (exists), plus `FrontRoomsHandheld.Active` (true on iOS and Android players, and in the Editor only in the handheld preview). Desktop and Editor output do not change by one byte |
| Profile | Annealed6 (110–150 pieces, 79–121 bodies) | Annealed6_WebGL (50–64 pieces, 35–40 bodies, 11–23 teeth, 732–944 stage-2 tris), no bevel |
| Motion | local PhysX scene, convex hulls | scripted integrator: gravity, spin, bounce on the analytic floor plane on both sides and on the wall plane with its opening, restitution 0.2. No hull cooking, no physics scene |
| Moving shards | one renderer per shard (≤ 121 draws for ≤ 2.5 s) | one SkinnedMeshRenderer, one bone per body (≤ 40), 1 draw. WebGL2 skins on the CPU (≈ 2k verts) |
| Settle | §2.4 | the same thresholds, cap 1.5 s |
| Persistent | teeth + floor = 2 draws, LOD0 ~7.5k + 1.5k tris | teeth and floor merged into **1 mesh, 1 draw, ≤ 2.5k tris** |
| Particles | 230–400 at the shatter | ≤ 60, floor plane only |
| Record | §4, ~2.1 KB | §4, ≤ 0.7 KB (≤ 40 shards) |

Plan §4.8 still holds: no desktop value is ever lowered to save WebGL cost, and a desktop capture must be byte-identical before and after the reduced-tier code (plan step 10).

---

## 4. The compact record (D-b) and its size

**Rule.** When a chunk with a shattered window is rebuilt, its teeth and settled floor glass are rebuilt from a compact record. Nothing is simulated again. This matches the existing "cracks do not heal" restore of S1 and S2.

**What carries over from the map.**
- The map's `GlassBreakRecord` (stage, seed, impact, rotation, side, plus `hold` with the contract) already regenerates the pattern byte for byte. The generator is deterministic, with 20 goldens and 1,015 cross-checks.
- So the teeth need nothing more than a snapped mask, and each floor shard needs only its pose.

**Layout.** `FrontRoomsGlassDebris.SettledRecord` is replaced by a packed blob per edge, in visual-side session memory:

| Field | Bytes |
|---|---|
| edge (long) | 8 |
| copy of seed, impact (half2), rotation (half), side, tier: to detect a mismatch with the map's record | 12 |
| floorY (float), break time (float), env hash (uint: count and bounds of default-scene colliders within 2.5 m at the settle) | 12 |
| teeth snapped mask (ulong) | 8 |
| shard count (ushort), crumb-cluster count (byte), flags (byte) | 4 |
| **per shard:** piece index (byte), flags (byte: side, upside down, on the sill, smallest-three index), position int16 × 3 in 0.1 mm in the root frame (±3.27 m covers v1's ±3.0 × 1.6 × 3.2 m box), rotation smallest-three int16 × 3 (error ≈ 0.003°) | **14**, padded to 16 |
| **per crumb cluster** (≤ 8): centre half2, extent half2, count ushort, seed ushort. Crumbs (2–9 mm) are scattered again from the seed, because their exact positions cannot be seen | 12 |

**Size per window:**
- Annealed6 at its maximum of 121 bodies: 44 + 121 × 16 + 8 × 12 = **2.1 KB**. v1's lab break (114 shards) gives 1.96 KB.
- Cinematic (132 bodies): 2.3 KB.
- Reduced tier (40 bodies): 0.7 KB.
- v1's `SettledRecord` is about 20 KB of managed lists for the same break.

**Restore** on `WindowBuilt` with `record.stage == 3` and a debris record for the edge:
1. Regenerate the pattern in slices of ≤ 1 ms (warm p50 3.4 ms / p90 6.6 ms, cold 17 ms; `04_stages.md` §5).
2. Build LOD1 first (pane faces only, ~1 ms).
3. Rebuild the teeth (minus the snapped mask) and LOD0 at the stored poses through the same job merge as F2.
4. In total about 25–30 ms of CPU, spread over about 30 frames.

**Timing of the restore.**
- The chunk is built at least 48 m away, beyond the 46 m far plane, so a sliced restore always finishes before the glass can be seen.
- The window is usually also behind walls.
- If the player reaches 20 m before LOD0 is done (impossible at walking pace: 28 m in 30 frames would be 50 m/s), the rest finishes at once.

**After a shift** (the chunk was away for 30 s or more and came back rearranged):
- If the edge is no longer a window, no `WindowBuilt` comes for it, and the record is ignored. It is dropped when its edge is next built as a non-window, or by the budget.
- If the window survived but the env hash changed, every floor shard is checked with `Physics.CheckBox` against the default scene, about 0.1 ms for 120 boxes. A shard that would sit inside a new prop is dropped. The teeth always come back.

**Fallback.** If D-b ever costs too much, Red's literal rule (the chunk release clears everything, and the window comes back with teeth only) is one switch: `FrontRoomsGlassDebris.KeepFloorRecords = false`. At 2.1 KB and about 30 ms of sliced CPU per restore, it does not cost too much, so D-b stays.

---

## 5. The kept-progress trace (D-d, corrected)

**What the body does to the pane before Crack1** (Design 2 timings, `FrontRoomsShotTimings.GlassBreak`; bow from `FrontRoomsGlassBreakable.TickBow`):

| Total hold | Phase | Contact with the glass | Bow while held | Today, after letting go | Proposed, after letting go |
|---|---|---|---|---|---|
| **0.10 s** | Plant and brace (0–0.25) | The forearm or sleeve is braced on the pane (the brace preload) | 0.26 mm | Nothing. Plain glass | **A sleeve wipe in the pane's dust**, at the impact, on the struck face: an oval of about 7 × 20 cm, wiped to 50 % (strength = min(1, hold / 0.20)) |
| **0.20 s** | Wind-up 1 starts (0.20–0.29); the arm leaves the glass | It ends | 0.67 mm | Nothing | The wipe at 100 % |
| **0.34 s** | Strike 1 in flight (0.29–0.35), 10 ms before contact | None yet | 1.90 mm | Nothing | The wipe at 100 %. Unchanged from 0.20, because nothing has hit the glass. See open question Q1 |
| 0.35 s and later | Strike 1 lands | Crack1 | — | S1 kept (crater ≤ 15 mm) | S1 kept, plus the legibility fix below |

**What the trace is and costs.**
- The wipe is drawn by the existing glass shader in pane-space UV, in the grime and dust layer that stays continuous across swaps (V7).
- It is driven only by `Record.hold`, `Record.impact` and `Record.side`, which the map record carries once the contract lands. So it needs **no new state** and survives a rebuild for free.
- In time it replaces the palm print, which stays off: Design 2 has no hand on the glass, and the print read as floating ovals (VL264). The bow still springs flat in 0.2 s, as real glass does.
- No new sound beat. 声音设计 gets an FYI: an optional soft fabric-on-glass tick at E-down.

**S1 legibility** (VL364: 74 px at 1.2 m).
- The physics sets the crater: ≤ 15 mm, with V5 passed. The radials are perfectly flat planes through the 6 mm, so face-on they are edge-on and vanish.
- Real cracks twist through the thickness (twist hackle), so part of each crack always faces the viewer and glints.
- **Proposal:** give each S1 fin segment a ±10–20° twist about its own length. This is a mesh change in `FrontRoomsGlassFractureMeshes` (visual-owned); the fins stay ≤ 400 tris, and G14's range layout is unchanged.
- Bar for VL acceptance: S1 is visible as at least 3 broken bright lines at 1.2 m face-on, in a lit Level 0 room.
- This is a change to the look, so the S1/S2 look approval (VL262, WAIT-RED) has to cover it.

---

## 6. Distance release and memory budget (D-a), with numbers

### 6.1 "Too far"

- **Too far** = the map releases the window's chunk (`WindowReleased` from `Unregister`). That happens when the player's chunk is more than 2 chunks away on either axis: 48–96 m straight-line (measured 57.0 m here).
- At that moment the floor glass and teeth meshes are destroyed with the chunk root, and the compact record stays.
- Floor glass is **never** cleared on a timer, and never while its chunk is built. The camera's 46 m far plane is shorter than the 48 m release distance, so clearing is never seen.

### 6.2 Live meshes (only windows in built chunks; 5 × 5 chunks = 120 m square)

| | Desktop | WebGL / iOS |
|---|---|---|
| Per broken window | ≤ 1.0 MB GPU (F6), 0 CPU copy, ~15 KB managed (record + `IsGlassAt` cells) | ≤ 0.15 MB GPU |
| 20 broken windows in range | ≤ 20 MB GPU, 0.3 MB managed | ≤ 3 MB |
| Safety cap | 32 MB | 6 MB |
| Over the cap | The oldest-broken window more than 24 m away drops to LOD1 only (45 KB). Its LOD1 is what would draw at that distance anyway: it switches below 6 % of screen height | The same, with the merged mesh |

### 6.3 Compact records (session memory, kept after release)

- Cap: **512 records ≈ 1.1 MB desktop** (0.35 MB reduced).
- When it is exceeded, the oldest *released* record goes first. That window comes back with teeth only, which is Red's literal rule, and only for the oldest of more than 512 broken windows.
- The map's own `glassBreaks` dictionary is unchanged: about 40 B per struck window.

### 6.4 Working sets (the breakable)

- **Today**, a struck window keeps its working set (2.46 MB managed) and its stage meshes (0.95 MB) until its chunk drops. With the contract, a pane can be struck and left at any progress, so this matters more.
- **Rule:**
  - keep them while the chunk is built and `0 < hold < 1`, because a resume may reach a beat within 1–3 frames (§7);
  - free them at the merge for a shattered window (v1);
  - free them on release for every window.
- **Spares** stay at `MaxSpares` 2.
- **Arming** (needed by kept progress, §7):
  - a stage-0 window rebuilt with `record.hold > 0` starts its pattern in slices on `WindowBuilt`, at least 48 m away, instead of at E-down;
  - a stage-2 window gets its debris shard set built when the player comes within 4 m, or at Crack2. At most 2 windows are armed at once, least recently used goes first, about 1 MB each while armed.

---

## 7. The exact map hunks (D-c) and what they change on the visual side

`CONTRACT_map.diff` makes 5 code changes in `FrontRoomsMapWorld.cs` (base sha1 `7ef0ca45`), plus doc comments, and edits 2 checks in `FrontRoomsMapInteractionTests.cs` (base `d9b51f23`), plus 1 new check. `README_CONTRACT_map.md` explains each one.

| # | Hunk | Line (base) | Why |
|---|---|---|---|
| C1 | `GlassBreakRecord` gets `public float hold;` | 354–362 | The progress travels with the record (survives release, drop and rebuild). It is also what the visual side reads for the wipe (§5) and for arming (§6.4) |
| C2 | New `KeepHold(Window)` next to `StageFloor`, called from `ReleaseHold`, `PauseHold` and `RaiseWindowReleased` | 3111, 3209, 3225, 1553 | Stores `record.hold` when it changed: on a release, a pause and the teardown, never per `Hold` tick (关卡设计's requirement, note of 13:3x in this folder) |
| C3 | `ReleaseHold()`: delete `window.hold = StageFloor(window.stage);` (and call `KeepHold`) | 3209 | Red's rule: letting go keeps the progress. `PauseHold` (tap mode) already did this |
| C4 | Chunk build: `window.hold = Mathf.Max(StageFloor(window.stage), record.hold);` | 1523 | A rebuilt pane resumes exactly where it was, and never below its stage |
| C5 | `BeginGlassHold()`: `if (record.stage == 0 && record.hold <= 0f)` | 3125 | The impact is fixed from the first strike, not from Crack1. Otherwise the kept progress (and its wipe) would jump to wherever the next press aims: measured 72 mm on B1 |

Tests:
- 2 existing checks move to the new rule: "a release keeps the progress (0.500)" and "a rebuild resumes from the kept progress (0.767)".
- 1 new check: "a 0.2 s knock is kept and a second strike keeps its impact".
- Clone result: 176 passed, 0 failed (`data/kept_map_interaction_tests.txt`).

**Merge safety.** `git merge-file` against 关卡设计's three pending MapWorld diffs gives 0 conflicts, one at a time and chained in its landing order (M2 → A12 → glass → C22). Details are in `README_CONTRACT_map.md`.

**The visual package must be safe to land before the map hunk** (关卡设计 lands the hunk after the Alpha). So the visual code never names `GlassBreakRecord.hold` at compile time: it reads the field through one cached `FieldInfo`, which returns 0 while the field is missing. The debris world (D-a, D-b) does not depend on it.

**No hunk is needed in `FrontRooms3DGame*.cs` or `FrontRoomsShots/*`.**
- `BeginGlass` already starts the shot at `window.hold / HoldSeconds`, and `FrontRoomsGlassShot.Begin` takes any progress and does not replay beats behind it.
- The mobile beat haptic fires every 0.2 of progress, as before.
- The beats stay where they are (sound sync).

**Stale comments, for the map line's own pass** (they are not wrong code):
- the game's `// Every pane not aimed at has no progress` (FrontRooms3DGame.cs ~1528);
- the shot's "the next hold resumes from the stage reached".

**Visual-side follow-ups the contract enables** (ours, `Assets/Scripts/Rendering/Glass`):
- (a) `OnWindowBuilt`: if `record.stage == 0 && record.hold > 0`, start the pattern in slices (no `FinishNow`);
- (b) `OnHoldReleased` at stage 0 keeps the scene-colour request off (unchanged), and `ApplyBlock` gains the wipe term from `Record.hold`;
- (c) the S1 fin twist;
- (d) the debris world arming.
- `FrontRoomsGlassBreakable`'s public events and `IFrontRoomsGlassShatterHandler` stay as they are.

---

## 8. Tests

| # | Test | Where | Bar |
|---|---|---|---|
| T1 | Map contract | `FrontRoomsMapInteractionTests` (edit mode) | The 3 glass checks above pass; the suite passes 176/176 (done in the clone) |
| T2 | Kept progress in play | `FrontRoomsGd3cRepro -gd3cTag kept` | A1 0.200 kept · B1 S1 on the 2nd press · B2 S2 · rebuild restores every hold · impact pinned (done in the clone, VL360/VL362) |
| T3 | Debris isolation | v1 lab tests, then the repro harness | E1 pane hit at every stage 60/60 · E2 E-ray through moving debris · R1 Relay sight · N1 nav capsule · Z1 no default-scene collider inside X ±0.70 × Y 0.35–2.00 · no collider under the root after the merge |
| T4 | Real glass | Plan §5.1, captures at 1.00–2.50 s, settled | V8 first landings 0.27–0.64 s after release, ~⅔ on the far side · V9 0 teeth or floor vertices in the clear zone (F3) · V10 glitter at the wall base on the player's side |
| T5 | Cost (quiet machine) | Harness timing | Build and merge slices ≤ 1.5 / 1.0 ms (F1, F2) · physics p90 ≤ 1.0 ms per 1/120 s step · no frame > 4 ms over the base on the shatter frame |
| T6 | Persistence | Repro harness D step | Shatter, walk until `WindowReleased` (≥ 48 m), come back: C1/C3 frames pixel-identical to before the release (≤ the 1.2/255 noise floor) · record ≤ 2.5 KB · restore finished before the player is within 46 m |
| T7 | Shift | Repro harness E step | A surviving window keeps its teeth; no floor shard intersects a default-scene collider; a vanished window leaves nothing drawn |
| T8 | Kept progress × beats | Harness: rebuilt pane left at 0.30, then pressed; stage-2 pane left at 0.95, then pressed | `ForcedAt` empty on the Crack1 frame; debris takes over 3 frames after E-down with no frame > 4 ms |
| T9 | Wipe and S1 legibility | Captures at 1.2 m face-on and 45°, lit and dead-lamp Level 0 | Wipe visible at 0.10 / 0.20 / 0.34 s as specified; S1 ≥ 3 bright broken lines at 1.2 m; nothing glows in a dead-lamp room (V3) |
| T10 | Budgets | Harness breaks 20 windows within the build radius | Persistent draws +40 (desktop) / +20 (reduced); triangles and GPU memory within §9; frame time delta logged |
| T11 | Reduced tier | WebGL build and iOS (handheld) preview | Integrator ≤ 0.2 ms, 1 skinned draw, ≤ 60 particles; desktop captures byte-identical before and after |
| T12 | Climb | Autopilot glass scenario | The climb completes; the bottom teeth snap; `TeethSnap` raised once |

---

## 9. Budgets

| | Desktop, per broken window | Desktop, 20 broken windows in range | WebGL / iOS, per window | WebGL / iOS, 20 windows |
|---|---|---|---|---|
| Persistent draws | 2 (teeth, floor; LODGroup on the floor) | 40 | 1 | 20 |
| Persistent triangles | LOD0 ≤ 7.5k floor + ≤ 2k teeth (v1: 10,284 + 1,456); LOD1 ≤ 0.8k + teeth | worst case all at LOD0: ≤ 190k. Typical (2–3 windows near, the rest at LOD1 or culled): ~50k | ≤ 2.5k | ≤ 50k |
| GPU memory | ≤ 1.0 MB (v1 1.39 MB + 1.39 MB CPU copy) | ≤ 20 MB (cap 32 MB) | ≤ 0.15 MB | ≤ 3 MB (cap 6 MB) |
| Managed memory | ~15 KB live; 2.1 KB record after release | 0.3 MB live; records 42 KB | ~5 KB; 0.7 KB | 0.1 MB |
| Transient draws (≤ 2.5 s after a shatter, one window at a time) | +≤ 121 shard renderers (SRP batcher, same material) + 3–4 particle systems | the same (one at a time) | +1 skinned + 1–2 particle systems | the same |
| Transient CPU | physics p50 1.03 / p90 1.28 ms per 1/120 s step (v1, UNDER LOAD 134) = about 2–2.6 ms per 60 Hz frame for ≤ 1.8 s; build and merge ≤ 1.5 / 1.0 ms slices after F1/F2 | the same | integrator ≤ 0.2 ms per frame | the same |
| Working set while armed or cracked | 2.46 MB managed + 0.95 MB meshes per struck window (freed at the merge or on release); +~1 MB per armed debris set (≤ 2) | bounded by the windows struck in range | the WebGL profile's (smaller) | — |

---

## 10. Open questions (only real ones)

1. **A let-go close to a beat (Red, with 关卡设计 for the shot rig).** Kept progress (D-c) means a resume can start a few frames before a beat. Examples:
   - a pane left at 0.34 s lands Crack1 1 frame after E-down;
   - one left at 0.69 s lands Crack2 1 frame after E-down.

   At that moment the shot's 0.25 s blend-in has about 1 % weight, so the hit lands with no visible wind-up and no camera impact. Only the sound and the swap happen. The options:
   - (a) **accept it** (simplest, no change);
   - (b) **commit the swing:** letting go during a strike's flight (0.29–0.35, 0.64–0.70 or 0.94–1.00) still lands that strike, at most 60 ms later. That is a map rule, and it makes a 0.30 s hold leave S1;
   - (c) **re-wind on resume:** the map holds progress still for up to 0.15 s while the shot replays the wind-up. That adds up to 0.15 s to the next beat; the beats themselves do not move.

   Recommendation: **(b)**. It is what a body does (a swing in flight cannot stop 1 cm from the glass), and it is the only option that gives a sub-0.35 s let-go a crack instead of only a dust wipe. It needs Red's yes, because it changes gameplay timing at the margin.
2. **The look of the two new traces** (Red, look). Both are new looks and need Red's approval:
   - the sleeve wipe in the dust below 0.35 s;
   - the S1 fin twist, so S1 reads at standing distance (§5).

   Until they are approved, the contract alone keeps the progress, and below 0.35 s it stays invisible.

**Notes, not questions:**
- **G14:** floor glass and teeth should become 2 static TLAS instances after the merge (plan §4.7, "after settle = 2"). The merged meshes are made non-readable unless P4 needs them readable. Not implemented here (G14 is mid-fix elsewhere).
- **声音设计:** `ShardImpact`, `TeethSnap` and `IsGlassAt` are ready in v1. An optional fabric tick at E-down. No beat changes.
- **关卡设计:** `CONTRACT_map.diff` lands after the Alpha with A12 and C22, per the map line's queue. It does not depend on the debris world.
