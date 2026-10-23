# GD3c build: the debris world, floor glass that stays, kept progress (build report)

> **Superseded where it differs by `30_fix1.md` (fix round 1: code 18:06, final take v2eng 20:15–20:19, package rebuilt 21:06).** Both reviews of this build were NOT READY; the fix round answers every defect there. In short: the map contract is **LANDED** (关卡设计 15:22, MapWorld `9b82c286`), the S1 twist now ships **OFF**, handheld is **not** on the reduced tier, F4's cause is found (the floor glass was shaded in the window's upright frame, with a sharp read of the reflection cube) and the cream, opaque pieces are gone (one small piece per view can still read pale where it catches the lamp: `30_fix1.md` §2), and the numbers below were re-measured. The package to land is the fix round's (`W/apply/gd3c_debris.sh`, rebuilt 21:06).

游戏视觉 workflow `glass-debris-progress`, build stage. Started on account B (session local_a9a1dce2, 14:0x–14:49, cut off by the session limit) and finished on account A (session local_5656cffd, 15:03–15:2x). The finished work of the dead run was checked and reused, not redone. This stage builds `10_design.md`.

**Red, 2026-10-22 13:0x:** broken glass stays on the floor unless the player walks far away (then it may be cleared to save memory), and a hold let go early keeps its progress instead of turning back into plain glass.

## 0. Short answer

- **Broken glass now stays.** On the shatter frame the debris world takes over the pieces at their stage-2 pose. They fall in waves, settle, and merge into one static floor-glass mesh plus one teeth mesh on the window root: 2 draws, no collider. At +10 s the glass is still on the floor (VL361). In main today the frame is empty.
- **"Too far" = the chunk release** (57 m measured, always beyond the 46 m far plane). The meshes go with the chunk and a 1.9 KB record stays. Coming back, the teeth and floor glass return from the record, without re-simulating, before the player is within 46 m (D step: done at 49.1 m). See VL362.
- **Kept progress:** with the map contract (`CONTRACT_map.diff`, the map line's to land), letting go keeps exactly 0.200 / 0.500 / 0.800, three 0.20 s holds reach S1, and a rebuilt pane resumes where it was (VL359, VL360). Below Crack1 the pane keeps a sleeve smear in its dust, and S1 twists so it glints (VL364, **WAIT-RED**: both are new looks).
- **The package is ready, not landed:** `W/apply/gd3c_debris.sh`, 22 visual-owned paths, DRY RUN OK on main `9d28aaf9` + tree. It compiles and runs with or without the map contract.
- **Open:** F4 pale shards under a bright ceiling; merge slice 3.3 ms against a 1.0 ms bar; floor triangles 9.6 % over target; a restore under load finishing 1.7 m inside the far plane. Everything was measured UNDER LOAD (§5, §6).

## 1. What changed

### 1.1 Visual-owned (package `W/apply/gd3c_debris`, ships through the 游戏视觉 session)

| Path | | What |
|---|---|---|
| `Assets/Scripts/Rendering/Glass/FrontRoomsGlassDebrisWorld.cs` | new | The `ShatterHandler` and the new `RestoreHandler`, installed at load (`RuntimeInitializeOnLoadMethod`). It runs a separate physics scene (`LocalPhysicsMode.Physics3D`, stepped only here), release waves, settle, the job merge, and teeth snapping on `PlayerClimbed`. On `Released` it keeps the record and lets the meshes go with the chunk. It also handles the return from the record, the neighbourhood check after a shift, arming (Crack2 or within 4 m of a stage-2 pane, at most 2), and JIT warm-up |
| `FrontRoomsGlassDebris.cs` | new | Records with a budget of 512 (oldest released first, live never dropped, `KeepFloorRecords` = Red's literal rule as a switch), the `ShardImpact` / `TeethSnap` events, and `IsGlassAt` |
| `FrontRoomsGlassDebrisPack.cs` | new | The compact record: 44 B header + 16 B per shard + 12 B per crumb cluster (2,076 B at 121 bodies) |
| `FrontRoomsGlassDebrisJobs.cs` | new | Hull baking on worker threads (F1), and the merge in a job with `AllocateWritableMeshData` and a 44 B vertex (F2, F6) |
| `FrontRoomsGlassDebrisLite.cs` | new | The reduced tier: a scripted integrator, one skinned draw, teeth and floor in one mesh |
| `FrontRoomsGlassFloorGlass.cs` | new | Teeth and floor glass on the window root: LOD0/LOD1, non-readable after the merge, no collider, with `FrontRoomsMetalGlassTarget` |
| `FrontRoomsGlassShard.cs`, `FrontRoomsGlassVfx.cs` | new | The pooled shard and the particles (v1, with the plan's counts) |
| `Assets/Editor/Rendering/Glass/FrontRoomsGlassDebrisTests.cs` | new | Edit-mode checks R1–R9 (13) |
| `FrontRoomsGlassBreakable.cs` | replaced, meta kept | Public events unchanged. Adds `RestoreHandler`, `Released`, `ReleaseWorkingSet()`, the stage-3 pattern regenerated in slices on return, the pattern armed for a pane rebuilt with kept progress, and the sleeve wipe in `_ImpactUV.zw` |
| `FrontRoomsGlassFractureMeshes.cs` | replaced, meta kept | The S1 fin twist (10–20°, fin triangles unchanged, M7 kept) |
| `FrontRoomsGlassTypeProfile.cs` | replaced, meta kept | `ReducedTier` = the existing `#if UNITY_WEBGL && !UNITY_EDITOR` gate, plus `FrontRoomsHandheld.Active` (plus a lab-only switch). Desktop and the Editor's normal Play Mode never take it |
| `Assets/Resources/Rendering/FrontRoomsGlass.shader` | replaced, meta kept | The sleeve wipe in ForwardLit, as a uniform branch with no new variant. The RT prepass and the G14 hooks are unchanged |

**How the visual side reads the progress.** It reads `Window.hold`, which main already has. The design planned a reflection accessor on `GlassBreakRecord.hold`; the build does not need it. So the package compiles and runs without the map hunk: until the hunk lands, the wipe and the arming simply see no kept hold.

### 1.2 Map contract (not in the package; LANDED by 关卡设计 at 15:22: MapWorld 9b82c286, tests 8f302e14)

- `CONTRACT_map.diff` is unchanged since rev 2 (13:43). It makes 5 code changes in `FrontRoomsMapWorld.cs`, edits 2 checks in `FrontRoomsMapInteractionTests.cs` and adds 1 new check.
- **Re-checked 15:0x** on main `9d28aaf9` + tree (MapWorld `223da401`, tests `31b991d1`): `git apply --check` passes on scratch copies. Every hunk applies with an offset and no fuzz.
- In the clone, the map files are exactly base + this diff.
- `README_CONTRACT_map.md` was corrected: the visual side reads `Window.hold`, not a reflection accessor.
- No hunk is needed in `FrontRooms3DGame*.cs` or `FrontRoomsShots/*`.

### 1.3 Clone only (never shipped)

- The harnesses `Assets/Editor/Audit/FrontRoomsGd3cBuild.cs` (Play), `FrontRoomsGd3cEditTests.cs` and `FrontRoomsGd3cRepro.cs`. Copies are in `W/gd3c/harness/`.
- One change in this run: the wipe was made 2.5–4× stronger (`W/gd3c/FrontRoomsGlass.shader.b4_1448` is the earlier one). At the b4 strength the smear was below the frame noise at 0.6 m (max 21/255, 290 px > 8/255).

## 2. Tests

| Suite | Result | When |
|---|---|---|
| `FrontRoomsGlassDebrisTests` (edit mode) | **13 passed, 0 failed.** R1 pack round trip 2,076 B (0.079 mm / 0.000°). R2 sizes. R3 budget eviction. R4 half floats. R5a–e F5/LOD1/44 B/V9 counter/bounds. R6 tier gate. R7 fin twist. R8 teeth mask. R9 shader | 15:18 (b12) |
| `FrontRoomsGlassStagesTests` (edit mode) | **12 passed, 0 failed** (S1a/S1b skipped: no reference copy in this clone). Includes Metal 20/20 and WebGL2 6/6 shader variants compiling, and M1–M8 | 15:18 |
| `FrontRoomsMapInteractionTests` (map, edit mode) | **176 passed, 0 failed** with the contract (map files unchanged since) | 14:48 (b09) |
| `FrontRoomsGd3cBuild` (Play, the game's own E path, seed 516574485, all steps K/C/D/T8/L/E/N/R) | All its checks met except those listed in §6 | 15:13–15:16 (b11, tag b6) |
| Roslyn on main `9d28aaf9` + tree + payload | macOS 0 errors (7 assemblies); iOS defines 0 errors; WebGL player defines (Assembly-CSharp, UNITY_EDITOR removed; approximate) 0 errors | 15:10 |
| Apply package | Dry run OK on main. `rebase_apply.sh --no-install` PASS (nothing to rebase). 6 scratch checks: dry run OK, `--apply` MANIFEST OK, ALREADY APPLIED; moved base, changed kept .meta and half-applied all REFUSED (`W/tools/_test/gd3c_debris_1022/`) | 15:14–15:15 |

## 3. The design's checks (`10_design.md` §8), measured

| # | Bar | Result |
|---|---|---|
| T1 | Map contract, 176/176 | PASS |
| T2 | Kept progress in play | PASS. Kept 0.200 / 0.500 / 0.800. 3 × 0.20 s gives 0.200 → 0.400 (S1 on press 2) → 0.600. 0.50 + 0.30 s gives 0.800 (S2). The impact stays at 1.335 m |
| T3 | Debris isolation | PASS. Z1: 0 default-scene colliders in the opening at the shatter, +0.05 s, +0.30 s and settled. The E ray passes the opening (hits at 2.52 m). Relay sight is clear. 0 colliders under the root |
| T4 | V8 / V9 / V10 | V8 PASS: first landings p50 0.52 s (min 0.37, p90 0.68). 83 of 110 shards, 73.5 % of the area, land on the far side. V9 PASS: 0 vertices in the clear zone (F3: 1 shard laid flat out of the opening). V10 was **not measured on the player's side** (`IsGlassAt` at the far-side wall base: true) |
| T5 | Cost | Build slices: worst 1.35 ms (bar 1.5): PASS. **Merge: worst slice 3.26 ms (bar 1.0): FAIL, UNDER LOAD** (0.98 ms on the T8 window). Physics p90 0.66 ms per 1/120 s step (bar 1.0): PASS. Shatter frame: handler 1.4–3.1 ms (bar: under 4 ms over base): PASS |
| T6 | Persistence | Record 1,900 B (bar 2.5 KB): PASS. Restore done at 49.1 m (bar: before 46 m): PASS. Pixels: the frame and far-carpet views are within noise; the near carpet differs on 0.022 % and the far-side view on 0.315 % of pixels > 12/255. Cause: crumbs are re-scattered from their seed by design (floor 8,217 → 8,173 tris). Not byte-identical |
| T7 | Shift (40 s away, 36 chunks shifted) | The C window survived: teeth kept; 7 shards that would sit inside new colliders were dropped. The two windows that vanished draw nothing. **The restore finished at 44.3 m, 1.7 m inside the far plane** (UNDER LOAD 190, harness walk about 9 m/s) |
| T8 | Kept progress × beats | PASS. A pane rebuilt at 0.200 has its pattern armed in 7 frames (worst slice 1.10 ms); the next press cracks with `ForcedAt` empty. A stage-2 pane rebuilt at 0.800 has its debris armed 1 frame after coming within 4 m; its shatter is not forced (handler 0.73 ms) |
| T9 | Wipe and S1 | Wipe at 0.6 m, lit room: 599 px > 8/255 at 0.10 s, 3,928 px at 0.34 s; it is lost at 1.2 m face-on. S1 at 1.2 m, lit: 647 px > 16/255 (diagnosis: 74 px), 2 bright lines plus about 4 faint ones, so the "≥ 3 bright lines" bar is **borderline**. Dead-lamp room: nothing glows (V3). WAIT-RED |
| T10 | 20 windows | 40 persistent draws, 18.42 MB GPU, CPU copies 0, records 37.6 KB. Triangles if every window is at LOD0: **190,551 (budget 190k)**; LOD1 8,463 |
| T11 | Reduced tier | Lab switch in the Editor: 40 bodies, 1 skinned draw, integrator p90 0.024 ms per step, settled in 1.51 s, 1 merged draw (logged: floor 1,711 tris, teeth 136). GPU 155 KB **plus a 155 KB CPU copy** (kept readable on purpose for the climb snap). Record 780 B. **No WebGL player build and no iOS device run**: approximate WebGL compile, WebGL2 shader variants, and R6 (the Editor is never on the reduced tier) only |
| T12 | Climb | PASS. 4 bottom teeth snap (1,400 → 1,224 tris), `TeethSnap` raised once, the mask is in the record |

## 4. Numbers (b6 run, 15:13–15:16, load 164 → 190: UNDER LOAD)

| | Desktop (reference) |
|---|---|
| Main thread, shatter frame | Debris handler 1.37 ms (internal) / 3.14 ms (harness probe). **0 B GC alloc** |
| Main thread while settling (157 frames, shatter → merge) | Debris-world LateUpdate p50 1.66 / p90 2.11 / max 4.40 ms. Physics per 1/120 s substep p50 0.37 / p90 0.66 / max 2.84 ms (294 substeps) |
| Before the shatter (from Crack2) | Build: 8 slices, 6.70 ms in all, worst 1.35 ms |
| Merge | 7 slices, worst 3.26 ms (items 3.26, record 2.19, apply 1.51, done 1.35) |
| Settle | Reaches the 2.5 s cap every run. 110 shards, 31 teeth, 298 crumbs merged |
| Per window, persistent | Floor LOD0 8,217 tris / 17,031 verts (bottom faces dropped: 1,895). LOD1 422. Teeth 1,400 tris. **950 KB GPU, 0 CPU copy, 2 draws**. Record 1,900 B |
| 20 windows | 40 draws, 18.42 MB GPU, 190,551 tris all at LOD0. Records 37.6 KB. Idle debris cost p50 0.007 ms |
| Release / return | Released at 57.0 m (E step: 64.2 m). Rebuilt at 55.0 m, restored by 49.1 m (D). Restore CPU 2.38 ms in all, worst slice 1.94 ms. Pattern restore worst slice 2.09 ms |
| Reduced tier (Editor lab switch) | Handler 0.06–0.82 ms, integrator p50 0.018 ms per step, merge worst 0.89 ms. One build slice spiked to 3.83 ms under load (b4: 0.26 ms) |
| Sound hooks (FYI 声音设计) | 638 `ShardImpact` raised for the C shatter, 97 dropped by the 16-per-frame cap. `TeethSnap` once |

## 5. Frames (VL359–VL364, Shot format; rows in `W/vl_pending/glass_debris.md`)

Each VL keeps its diagnosis image (the "before") and gains a build image (the "after"):

| VL | Build image | Shows |
|---|---|---|
| VL359 | `images/VL359_build_knocks_kept.jpg` | Holds of 0.20 / 0.50 / 0.80 s: the held frame, face-on 1.2 m after release, and 0.6 m close |
| VL360 | `images/VL360_build_kept_progress.jpg` | 3 × 0.20 s, 0.50 + 0.30 s, rebuilt panes, rebuilt + 0.20 s → Crack1 |
| VL361 | `images/VL361_build_shatter_stays.jpg` | The shatter frame, then +0.05 / 0.15 / 0.30 / 0.60 / 1.00 / 2.50 s (merged) and +10 s, plus the far carpet |
| VL362 | `images/VL362_build_walk_away_back.jpg` | Four views before the walk away and after the return |
| VL363 | `images/VL363_build_rooms_tier_f4.jpg` | A lit and a dead-lamp window, settled; the F4 probe; the reduced tier at +0.30 s and settled (**FLAG**: F4) |
| VL364 | `images/VL364_build_wipe_s1_twist.jpg` | The wipe at 2× around the impact with the difference ×8, lit and dead-lamp, and S1 at 1.2 m (**WAIT-RED**) |

Raw PNGs are in `W/proj_gd3c/Verification/gd3c_build_b6/` (136 frames). Data is in `data/build_*` (metrics, records, frames, log, pixel metrics, edit and map test output).

## 6. Open defects

1. **F4: pale shards.** On the C window's far-side carpet, under a bright ceiling, flat shards read as pale, near-opaque pieces, almost like paper (VL361 last tile, VL363).
   - The crush-rim hypothesis is refuted: with dust, scatter, the reflection top-up, smudge, and all four off, the shards stay pale.
   - In the lit and dead-lamp windows of the L step, they read as glass.
   - Next step: check what the floor glass's environment term samples (the zone reflection cube under the lamp grid, convolved) against a photo reference of glass on carpet under fluorescent panels. It is not hidden.
2. **Merge slice 3.26 ms** against the F2 bar of 1.0 ms (the "items + count" and "record" steps). UNDER LOAD. Re-measure quiet, then split those two steps.
3. **Floor LOD0 8,217 tris** against the ~7.5k target. With all 20 windows at LOD0 that makes 190.6k against 190k.
4. **Settle always ends at the 2.5 s cap** (v1: 1.76 s). One shard or more never meets the sleep thresholds. It is frozen at the cap, so nothing shows, but physics runs about 0.7 s longer.
5. **E-return restore finished 1.7 m inside the 46 m far plane**, UNDER LOAD 190 and at the harness's ~9 m/s. The D-return finished at 49.1 m. A restore still unfinished at 20 m completes at once, by design.
6. **A return is not byte-identical:** crumbs are re-scattered (by design, −44 tris). The two near views differ on 0.02–0.3 % of pixels.
7. **The reduced tier keeps a 155 KB CPU copy** per window, for the snap. 20 windows come to about 6.2 MB against the 6 MB cap.
8. **The wipe is faint**, and S1 at 1.2 m is borderline. Both are WAIT-RED, see `FOR_RED.md`.
9. **V10** (glitter at the wall base on the player's side) was not measured.
10. **No WebGL player build and no iOS device run.**
11. **G14:** verified with `-frGlassRT off`, on the clone's pre-landing G14 files.
    - Main now has G14 (landed 14:58); this package keeps the shader's RT prepass and G14 hooks unchanged.
    - Floor glass in RT reflections is a note for the G14 track: register the teeth and floor after `Settled`, both carry `FrontRoomsMetalGlassTarget`. Not implemented.
12. **All timings UNDER LOAD** (load 38–190). Re-measure on a quiet machine before quoting them as final.

## 7. Landing (out of date: see `30_fix1.md` §7)

```
bash /Users/redwang/FrontRoomsVisualWork/apply/gd3c_debris.sh            # dry run: DRY RUN OK (main 9d28aaf9 + tree, 15:14)
bash /Users/redwang/FrontRoomsVisualWork/apply/gd3c_debris.sh --apply
```

- The package is safe without the map contract, and safe before or after it.
- Red's rule 10-21 ("验证通过就直接合进主工程，不用等 Alpha") covers visual work, but this package changes what every shattered window does in the game on the Alpha day. The 游戏视觉 session decides the timing.
- On a base mismatch, run `W/tools/rebase_apply.sh gd3c_debris` first.
