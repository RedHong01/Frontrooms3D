# 20 — Outlets C1: the code, the map contract, and the tests

Status: **C1 DONE, 2026-10-04 08:xx. Re-verified on main `ee5c9bb`, 2026-10-07 (§12), and packaged for landing: `bash /Users/redwang/FrontRoomsVisualWork/apply/outlets_c1.sh` prints DRY RUN OK (§9).** Written by the outlet workflow's code task (C1, `10_spec.md` §5.4).
- 2026-10-07: the 2026-10-05 reboot wiped the old clone. The code was restored byte-for-byte from `data/20_*.cs.txt`, the clone was rebuilt as `W/proj_outlet` (`W` = `/Users/redwang/FrontRoomsVisualWork`) and re-synced to main twice (a5262fb6 at 18:16, ee5c9bb at 23:36). No code changed; every test was re-run (§12).
- 2026-10-04: all code lived in the private clone `scratchpad/proj_outlet` (re-synced to main **`d610d3a`** at 06:27 that day; wiped 2026-10-05). It now lives in `W/proj_outlet` and in the apply payload `W/apply/outlets_c1_payload/`.
- In Red's real project, only this folder changed (this file, `data/20_*`, `data/21_*`, `images/20_*`, `images/21_*`) plus the VL093/VL094 rows of `Documentation/VERIFICATION_LOG.md`. Unity was never opened on Frontrooms3D.
- The map part is a **CONTRACT** (§6): an exact diff for 关卡设计, tested in the clone. Nothing map-owned was edited in the real project.
- The RenderSetup lines are a **proposal** (§8), NEEDS APPROVAL. They were not applied anywhere.

**Load rule.** Every millisecond below has the 1-minute load average next to it. Numbers taken at load > 32 are marked **measured under load, re-measure**. This morning the machine was quiet for about an hour, so the key timings were re-taken at load 6–15.

---

## 0. Results in one table

This table is the 2026-10-04 run (final code, low load). **The 2026-10-07 re-run on main `ee5c9bb` (§12) reproduced every count exactly** (faces, fixtures, rule counts, draws, triangles, colliders, renderers, suite totals). Its milliseconds were all taken at load 170–800 and are marked *measured under load*; the timings below stay the valid ones.

| Check | Result | Load (1 min) |
|---|---|---|
| Contract probe, **100 seeds × 25 chunks** | **PASS.** 315,052 faces, 0 missing, 0 extra, 0 kind mismatches. 84,809 fixtures, all on solid shell (worst 0.4 mm). Every rule count of spec §4.4 is **0** | 35 → 253 (counts are exact) |
| Determinism | 300 rebuilds: 0 diffs. 400 revisit shifts: 1,258 border-face fixtures compared, 0 diffs. Re-plan of all 2,500 chunks: 0 diffs. 5 independent builds: 0 diffs | — |
| Colliders and renderers added | **0 and 0** (same seed built with and without outlets, 5 seeds). 0 colliders under any set. 0 GameObjects added by drawing (T-R2) | — |
| Install per chunk (the map's one call, inside the real build) | median **0.149 ms**, p95 0.275, max 0.90 (first chunk, cold). Target ≤ 0.5 ms: **met** | 12.4–15.3 |
| Plan alone / face collection | median 0.092 ms / 0.033–0.049 ms per chunk; **0 bytes** of garbage per `Plan` | 12.4–15.3 |
| R3 submit per camera (real draws, 44 captures) | median **0.070 ms**, max 0.094 ms; 0.47 ms once (the first draw loads the kits). Target ≤ 0.15 ms: **met** | 7.9–12.4 |
| Draws per pass (1,000 random views, 10 seeds) | mean **32.6**, p95 56, max 87 today (one draw per instance, no instancing flag yet). With P-6: mean 14.3, p95 27, max 38 | counts |
| Triangles per view | mean 23.6 k, p95 39.9 k, max 61.9 k (every kit draws its LOD0 to 12 m until P-1 lands) | counts |
| Relay navigation | **60/60** with outlets and without | — |
| Map interaction tests | **143/143** with and without (the suite grew from 44 since the spec) | — |
| Level Designer tests | **138/138** with and without (grew from 85) | — |
| Map verification / fixture ticks | 100/100 seeds / 28/28, with and without | — |
| Autopilot, seeds 2554 and 20388 (with graphics) | **58.7–59.0 fps, p99 17.4–17.7 ms** with outlets; 58.5–58.6 fps, p99 17.6–18.2 ms without. Max scene colliders 401 / 401 and 389 / 389 | 5.7–7.3 |
| T-R2: edit-mode `Camera.Render` at root 4992 | **22 / 22** shots draw outlets; 0 scene GameObjects, colliders or renderers added. The Level Designer preview draws them too (109–706 px change per module view) | 7.9–12.4 |
| R-4: stage-2 decision | **Stage 2 is needed, and runs from the Office kit (visual-owned). No map call (C-4 not requested).** 32 plates in 20 seeds met a wall unit's mesh; all are now hidden. 0 cuts on drawn plates in 17,106. 161 / 165 desks have power within 2 m (§5) | — |

---

## 1. What happened to this task, and the base it now stands on

| When | What |
|---|---|
| 2026-10-03 16:46–18:02 | First C1 run in `proj_outlet` against the 12:38 map. The usage limit hit at 17:54, before the report |
| 19:10–19:41 | Codex committed copies of other clones into main (`7320ed1` → `75cfdff`). **No outlet code was among them.** The Codex audit lists outlets (D3) as "not touched" (`codex_audit/00_main_state.md`) |
| 21:57–22:06 | Second run: the clone was rebuilt from main `75cfdff` (old clone kept as `scratchpad/proj_outlet_pre1910`); the work was re-applied; stage 2 was added. The 100-seed probe was cut off at 23:15 |
| 2026-10-04 06:27 | This run: main had moved to **`d610d3a`**. Only 4 files under `Assets/` changed since `75cfdff`, all the map chat's (the Relay two-bug fix): `FrontRoomsMapHunter.cs`, `FrontRoomsRelayNavTest.cs`, `FrontRoomsMapInteractionTests.cs`, `Kit_MiniBlind_Lowered.fbx.meta`. They were copied into the clone. The clone's `.git` is now a copy of main's, so `git diff` in the clone shows exactly this task's change |
| 06:37–07:27 | Every test re-run on the final code (§7) |

**Merge rules (brief rules 1–4).**
- Main holds **no outlet code** (no Codex copy), so there is nothing to merge over. The new files keep their clone GUIDs (§2). No path in main has these GUIDs.
- `MapWorld`, `RoomStream`, `OfficeKit` and `RenderSetup` in main are unchanged since `75cfdff`. The diffs in `data/` are against main `d610d3a` and apply cleanly (checked with `patch --dry-run` at 08:25).
- `codex_audit/20_findings.md` does not exist yet (checked 06:20 and 08:09). The audit's other files name no outlet finding. Re-check before landing.
- The outlet kits (FBX, JSON, `.meta`) exist **only in the clone**. The O1–O3 Blender groups own them and were rebuilding some this morning (O1 FBX at 06:23). Main has the Blender modules (`outlet_*.py`) but no FBX.

---

## 2. Files

Text copies of every file are in `data/`, so the visual chat can land them without the clone.

| File (clone) | Owner | Lines | What it is | Copy in `data/` |
|---|---|---|---|---|
| `Assets/Scripts/Office/FrontRoomsWallFixtures.cs` (new, GUID `14ab7ea1bd08347a69aaf5807ec8a980`) | visual | 1,142 | Structs (spec §3.2, unchanged), rules, `Plan` (pure), `PlanLocal`, `Install`, `InstallLocal`, `Refine` (map stub, off), stage 2 from the Office kit, keep-outs, kit-presence cache | `20_FrontRoomsWallFixtures.cs.txt` |
| `Assets/Scripts/Office/FrontRoomsWallFixtureSet.cs` (new, GUID `6f896ecc41ab64abeae58086b3349d55`) | visual | 171 | `[ExecuteAlways]` component on the chunk root or a stream variant: faces, fixtures, keep-outs, occluders, `Registry`, `AddExtra`, `AddOccluder`, `WorldPositions` (sound chat), the renderer's cache | `20_FrontRoomsWallFixtureSet.cs.txt` |
| `Assets/Scripts/Office/FrontRoomsWallFixtureRenderer.cs` (new, GUID `e2a606786677e4ab0b1e341c96429309`) | visual | 914 | R3: the instanced, culled, shadowless, colliderless submitter; LOD; fallbacks; placeholders; WebGL gate; stats | `20_FrontRoomsWallFixtureRenderer.cs.txt` |
| `Assets/Scripts/FrontRoomsRoomStream.cs` (changed, +50 −14) | visual | — | 45 placeholder plates (each with a BoxCollider) out; `StreamFaces` + `InstallLocal`; Exit branch reachable again | `20_RoomStream.diff.txt` (against main `d610d3a`) |
| `Assets/Scripts/Office/FrontRoomsOfficeKit.cs` (changed, +66 −3) | visual | — | Pod floor feeds; one `Kit_CubiclePanel_Powered` spine panel per pod; stage 2 (wall units hide plates; wall-row desks get power) | `20_OfficeKit.diff.txt` (against main) |
| `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs` (clone-only patch) | **map** | — | The contract (§6) plus two `// PROBE` timers | `20_contract.diff.txt` (the request, no timers), `20_contract_patch.py.txt` |
| `Assets/Editor/OutletCensus/FrontRoomsOutletContractProbe.cs` (clone-only) | test | 691 | Probe v4: contract, rules, determinism, stage 2, cost | `20_FrontRoomsOutletContractProbe.cs.txt` |
| `Assets/Editor/OutletCensus/FrontRoomsOutletCapture.cs` (clone-only) | test | 721 | T-R2 and R-4 captures and the R-4 geometry analysis | `20_FrontRoomsOutletCapture.cs.txt` |
| `Assets/Editor/OutletCensus/FrontRoomsOutletPlayStats.cs` (clone-only) | test | 96 | R3 cost and object counts during the autopilot | `20_FrontRoomsOutletPlayStats.cs.txt` |
| `Assets/Editor/OutletCensus/FrontRoomsOutletTestSwitch.cs` (clone-only) | test | 17 | `OUTLETS_OFF=1` runs any suite as the "before" baseline in the same clone | `20_FrontRoomsOutletTestSwitch.cs.txt` |
| `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs` | visual | — | **Not changed.** P-4b + P-6 as a proposal (§8) | `20_rendersetup_p4b_p6.diff.txt`, `20_rendersetup_patch.py.txt` |

Results: `20_probe_100seeds.json`, `20_probe_timing_10seeds.json`, `20_capture_tr2_r4.json`, `20_playtest_{on,off}_{2554,20388}.report.json`, `20_playstats_*.json`, and `20_timeline_with_load.txt` (every Unity run with its start and end load).

---

## 3. API

The structs and the four spec functions are **exactly** spec §3.2–§3.3; the map contract depends on them. Nothing was renamed. `[Serializable]` was added to the three structs so a set survives a domain reload in edit mode.

```csharp
// FrontRoomsWallFixtures (static) — spec §3.2–3.3, unchanged
public struct WallFace { ... }   public struct Fixture { ... }   public struct KeepOut { public Rect xz; public float y0, y1; }
public enum FixtureKit, Finish, FixtureFlags, FixtureClass
public static void Plan(int seed, IReadOnlyList<WallFace> faces, MapChunk data, RectInt startArea,
    IReadOnlyList<ZoneTheme> cellTheme, IReadOnlyList<bool> roomDressed, IReadOnlyList<KeepOut> keepOut, List<Fixture> into);
public static void PlanLocal(int seed, IReadOnlyList<WallFace> faces, FixtureClass cls, List<Fixture> into);
public static FrontRoomsWallFixtureSet Install(Transform chunkRoot, List<WallFace> faces, int seed, MapChunk data,
    RectInt startArea, List<Mesh> ownedMeshes);                       // the map's one call (§6)
public static FrontRoomsWallFixtureSet InstallLocal(Transform root, List<WallFace> faces, int seed, FixtureClass cls, Material wallMatch);
public static void Refine(Transform chunkRoot, int room, Rect floor, IReadOnlyList<Rect> keepClear, IReadOnlyList<Rect> footprints); // map stage 2 (C-4): stub, off

// Added (visual-owned; no contract impact)
public static FixtureClass StreamClass(RoomRule rule);        // RoomRule -> StreamLobby/Shift/Office/Run/Exit
public static FixtureClass Classify(in WallFace f);
public static bool KitExists(string name);                     // cached and silent (no "missing model" warning for kits not built yet)
public static bool Suppressed { get; set; }                    // tools/tests: Install and InstallLocal add nothing
public const bool RefineEnabled = false;
// Stage 2 from the Office kit (§5)
public const float StationReach = 2f;
public static bool IsOccluded(FrontRoomsWallFixtureSet set, in Fixture x);
public static bool EnsureReceptacleNear(FrontRoomsWallFixtureSet set, Vector3 deskWorld);
public static Bounds PlateBounds(in Fixture x);                // the §2.9 volume: plate + 0.02 m each side, 0.012 m out
public static bool OcclusionEnabled { get; set; }              // tools: before/after captures

// FrontRoomsWallFixtureSet (component on the chunk root / stream variant root)
public static readonly List<FrontRoomsWallFixtureSet> Registry;   public static int OccluderVersion;
public List<WallFace> faces; public List<Fixture> fixtures; public List<KeepOut> keepOut; public List<Bounds> occluders;
public int seed, extraCount;
public void AddExtra(in Fixture x);                            // Office-kit floor feeds (root space)
public void AddExtra(FixtureKit kit, Transform space, Vector3 position, Vector3 normal, Finish plate = Stainless, Finish device = Ivory, FixtureClass cls = Floor);
public void AddOccluder(Transform piece, Vector3 localMin, Vector3 localMax);   // stage 2, rule 1
public bool Occluded(int i);
public void WorldPositions(List<Vector3> into, FixtureKit kit = None);          // drawn fixtures only (sound chat, debug)
public void MarkChanged();

// FrontRoomsWallFixtureRenderer (static, R3)
public static bool Enabled, DryRun;                            // tests: draw nothing / count without drawing
public static void Submit(Camera camera);                      // the beginCameraRendering hook calls this
public static FrameStats Last, Accumulated; public static void ResetStats(); public static void ClearCache();
public static string DrawnKit(FixtureKit k);                   // what the fallback chain draws for a kit
public static Matrix4x4 FixtureMatrix(in Fixture x, float plateH);
```

---

## 4. What the code does, and what changed from the reference planner

### 4.1 Planner (`FrontRoomsWallFixtures.cs`)

**Kept exactly** (spec §2.13, §3.3):
- **Plan order:** foreign faces first (no keep-outs, no cap) → own faces → room-side guarantee → lone switch → back-to-back → columns → floor boxes.
- **Hashes:** face `MapHash.Hash(seed, (int)edgeId, (int)(edgeId >> 32), 409 + 16·field + side)`; line `MapHash.Hash(seed, line·2 + orientation, segment, 425 + side)`. Salts start at 409. **No revision** anywhere.
- Every rule number of spec §2: end margin 0.30, arch clearance 0.20, back-to-back 0.61 with side 0 yielding, 0.15 m grid, the heights, the densities, caps 10 / 24 / 160.

**Changed for speed.** The plan itself does not change: the probe re-plans every chunk and compares (0 diffs in 2,500 chunks).
- No lambdas: `Snap(…, Func<float,bool>)` became two plain loops (`SnapSameFace`, `SnapBackToBack`).
- No allocation per call: room counts, twin indices, room-side records, cell themes and the "room dressed" flags live in one `[ThreadStatic]` scratch object. It is cleared, not reallocated.
- The `Dictionary`s in `ForceRoomSides` and `ResolveBackToBack` became a sorted list and a twin index. The map lists both faces of an edge next to each other, so the twin is found in O(1).
- Kit footprints for keep-outs are cached per kit name. `RoomModuleData.Bounds` is inlined without its two temporary arrays (same corners, same order).
- Result: **0 bytes** of garbage per `Plan` call; Install 0.149 ms median per chunk (load 12–15).

**Added:**
- `InstallLocal`, `AddExtra`, `Refine` (stub, off), `StreamClass`, `Suppressed`, `KitExists`.
- **Per-class finish override** (`ClassRule.fixedFinish`): `StreamRun` plates and devices are always **white**. The reference rolled Level 0 colours for it. Lobby and Exit stay stainless through `pSteel = 1`.
- **Cap fix (a rule change; it says so here).** In the reference, a receptacle added just under a room's cap could still bring its phone and data plates, so a room could end 1–2 over its cap. Companions are now added only while the room has room under its cap. A forced receptacle (room-side guarantee) may still sit on a full room, as the spec intends. Measured: the largest Level 0 room holds 10 and the largest Office room 24, exactly the caps.
- **Matrices per fixture.** The brief asked for "local matrices per (kit, LOD)". A fixture's matrix is the same for its three LOD meshes, so the set caches one root-space matrix per fixture, plus one *recipe* per (kit, LOD, plate material, device material): the list of (mesh, submesh, material) buckets. They are rebuilt only when the plan, the kit library or the root changes.

### 4.2 Renderer (`FrontRoomsWallFixtureRenderer.cs`, R3, spec §3.4)

- **Hook.** One static submitter on `RenderPipelineManager.beginCameraRendering`: `[RuntimeInitializeOnLoadMethod(AfterAssembliesLoaded)]` in players, `[InitializeOnLoad]` in the editor. The hook is idempotent.
- **Per camera.** Preview and Reflection cameras are skipped. `lodScale = tan(fov/2) / tan 38° / QualitySettings.lodBias`. Reach = min(**40 m**, dcull / lodScale). Whole sets out of reach are skipped by their bounds.
- **Per fixture.** A distance test; a **0.1 m sphere** against the 6 frustum planes (0.15 m for floor boxes, 0.12 m for 2-gang); then LOD0 < 1.5 m < LOD1 < 4 m < LOD2 < 12 m. Floor boxes use 2 / 6 / 15 m. A kit's sidecar `lodDistances` overrides the defaults (a value ≤ 0 keeps the default: `Kit_ConduitStrap` writes −1).
- **Buckets** per (mesh, submesh, material). Matrix arrays grow 32 → 511 and flush at 511.
  - `Graphics.RenderMeshInstanced` when the material has instancing on.
  - Otherwise `Graphics.RenderMesh` per instance (the SRP Batcher path). That is every palette material today, until P-6.
  - Wall-match (papered-over) buckets are **always** one draw per instance.
- **`RenderParams`:** the camera; the set's layer; `ShadowCastingMode.Off`; `receiveShadows = true`; light probes Off; reflection probes Off; motion vectors Camera; the bucket's world bounds.
  - **Deviation (it says so here):** spec §3.4 fixes layer 0. The map's and the Level Designer's capture tools move their whole build to layer 31 and render with that culling mask. Drawing on the set's own layer makes outlets show in those captures too (T-R2: 109–706 px per designer view). In the game every set is on Default (layer 0), so nothing changes there.
- **Screws** (`Kit_OutletScrew`) sit at the plate's `screw_0…3` anchors with the plate's random slot angle, at LOD0 only, and never for `NoScrew`.
- **Missing kits** (spec §1.4): Jumbo, Cracked, Scorched, Bare → Duplex; HandyBox → Steel; Toggle2 → Toggle1; data jacks, 4-prong phone and floor boxes → nothing. A kit at the end of a chain that is not built draws a **placeholder of the spec's dimensions**: 6 shared meshes in all, never one per chunk. Today every P1 and P2 kit exists in the clone, so only `_Scorched` (P3) falls back, to Duplex.
- **Palette:** plate, device and screw materials come per fixture from spec §1.5, with the listed fallbacks until P-4b (`Prop_PlasticBeige`, `Prop_PlasticPutty`, `Prop_LaminateBeige`). IG orange draws ivory until `Prop_PlasticOrange` exists. Papered-over plates use the face's wall material.
- **WebGL gate** (`#if UNITY_WEBGL && !UNITY_EDITOR` only): cull at 8 m, no screws, papered plates drawn ivory. Desktop is untouched.
- **Stage 2, rule 1:** a `hidden` flag per fixture, refreshed when any occluder changes. The refresh tests only the set's own occluders and those of sets whose occluders come within 0.4 m of it. Matrices and recipes are not rebuilt.
- It **never** creates a GameObject, Collider, Renderer or MaterialPropertyBlock, and never a per-chunk mesh. So `FreeMeshes` has nothing new to free; `ownedMeshes` stays for a future combined-mesh fallback.

### 4.3 Title stream (`FrontRoomsRoomStream.cs`, spec §3.5)

- **Out:** the 45 placeholder boxes (3 per Lobby/Shift/Exit variant × 5 slots, each with a `BoxCollider` the E ray could hit) and their material.
- `StreamFaces(rule)` builds the faces once per rule and shares them:
  - the two side walls' inner faces (x = ±5.62, four 3 m faces each, normals ±X);
  - the far end wall's two spans beside the double door (x ±1.2 … ±5.62, on the door reveal plane z = 11.978, normal −Z);
  - `baseTop = 0.26`, `edgeId` = face index.
- `InstallLocal(props.transform, StreamFaces(rule), 7919·sequence + 104729, StreamClass(rule), roomWall)` runs for every variant, before the Office kit dresses it, so the pods and wall units find the set. A recycled slot keeps its plates (the same seed as the Office dressing).
- `Exit` left the first branch, so the "exit threshold marker" is built again.
  - **Change from spec §3.5 (it says so here):** the marker's `BoxCollider` is removed. It is a 3 cm floor strip; keeping the collider would add up to 5 colliders to the stream.
- Measured (probe, 20 slot sequences per rule): fixtures per room Lobby 6.8, Shift 7.9, Office 15.6, Run 5.35, Exit 3.0. Lowest centre 0.38 / 0.38 / 0.41 / 0.46 / 0.38 m. 0 below the baseboard clearance, 0 outside the margins, 0 Run plates that are not white.

### 4.4 Office kit (`FrontRoomsOfficeKit.cs`)

- `BuildRun` (pods only, not wall rows): one `Kit_FloorBoxTombstone` at each pod end, on the spine line, 0.15 m outside the end panel, receptacle toward the panel.
  - It goes through `GetComponentInParent<FrontRoomsWallFixtureSet>(true)?.AddExtra(...)`. `includeInactive` matters: stream variants are dressed while hidden.
  - With no set (the look-dev hall), it spawns the kit without colliders, if the kit exists.
- One spine panel per pod becomes `Kit_CubiclePanel_Powered` when that kit exists. It has the same bounds and collider as `Kit_CubiclePanel`. Not for tall pods.
- Stage 2 (§5): `TryWallAt` reports each wall unit as an occluder; `PlaceWallRows` calls `PowerRow` after each row.
- No random draw was added, so every existing layout is unchanged (the probe's on/off builds: 0 renderer and 0 collider difference).

---

## 5. Stage 2 and render check R-4: the decision

**Question (spec §2.9, §7 R-4).** Does stage 1 alone look right behind furniture? Spec §2.9 lists three conditions that would switch stage 2 on.

**What was run.** `FrontRoomsOutletCapture` (07:26, load 7.9–12.4):
- 16 eye-height captures: 8 dressed Office rooms (seeds 2554, 20388) and **6** pile rooms. Only 14 pile rooms exist in 20 seeds, and only 6 had a wall plate facing the pile within 9 m with a clear camera spot.
- 4 close-ups at 0.3 m and 2 before/after pairs.
- A mesh-accurate analysis over every dressed room of 20 seeds: 100 Office rooms, 14 pile rooms, 4,690 furniture meshes, 17,106 wall plates.

| Condition (spec §2.9) | Stage 1 alone | With stage 2 |
|---|---|---|
| (a) A fixture cut by furniture | **32 plates** in 20 seeds have furniture triangles inside the plate body. All are behind **wall units** the Office kit stands against the wall: the water cooler's condenser coil reaches the wall plane, and the copier's cord runs 10 mm past it. Spec D8 assumed furniture always stands ≥ 0.03 m off the face; the sidecar bounds of these units do not | **0 cuts** on drawn plates. 71 plates behind wall units are not drawn |
| (b) A plate seen through a gap < 0.05 m | Not seen in any capture. The nearest furniture in front of a drawn plate is a wall-row back panel 12 mm off the face; the plate stands 7.5 mm out, so they never touch, and the panel hides the plate | Same |
| (c) A powered desk with no power within 2 m | 100 seeds: pod desks **632 / 632** (floor feeds); wall-row desks needed help | 20 seeds: **161 / 165** desks; 100 seeds: pods 632 / 632, wall rows **109 / 118** |

**Decision.**
- **Stage 2 is ON for rules 1 and 2. It runs from the Office kit, which is visual-owned. The map needs no new call, so C-4 is not requested.** `Refine` stays a stub (`RefineEnabled = false`).
  - Rule 1: each wall unit the Office kit places adds an occluder box (its sidecar bounds) to the set. A plate whose volume meets one is not drawn. The plan does not change.
  - Rule 2: after each wall row, every desk without a drawn receptacle or floor box within 2 m gets one receptacle on the nearest own Wall or Arch face behind it. It keeps every stage-1 rule: 0.15 m grid, end margins, arch clearance, same-face gap, back-to-back, keep-outs and occluders. It never goes on Door or Window faces or through a wall. It is deterministic and uses no random draw.
  - Rule 3 (IG + phone pairs at desk walls) stays off: Office rooms already carry IG on 1/3 and phones beside 1/2.
- Map-owned furniture (piles, module props) cut no plate: 0 in 20 seeds.
- **Why not visible in the captures:** the 32 cuts sit behind the units, so the before/after pair (`r4_cut_2554_0_stage1/2`) looks the same. The decision rests on the geometry count, as condition (a) calls a cut "a bug, since geometry forbids it".

**Known limit (FLAG).** **9 of 118 wall-row desks** (100 seeds) stand against a **chunk-border wall** that the west or south neighbour built. That face is the neighbour's "foreign" face: it is planned at corridor density, and this chunk's set does not hold it, so rule 2 cannot add a plate there. This is spec §2.13 / open item 6 (border rooms). Two ways to fix it, both left for a decision:
1. Let rule 2 add the receptacle to the **neighbour's** set. It needs a lifetime rule: the plate is lost when the neighbour rebuilds and the desk's chunk does not.
2. The map's cell-based collection (`02` §8, option B), so a room owns all four of its walls. That is a map-side change.

---

## 6. The contract: the exact clone-only MapWorld diff (CONTRACT for 关卡设计)

**Same request as spec §4.1** in shape and content: the same struct, the same two signatures, the same call point. What is new since the spec:
- It targeted **main `d610d3a`** on 2026-10-04 (MapWorld there is byte-identical to `75cfdff`); on 2026-10-07 it was re-made on **main `ee5c9bb`** with the same lines (below). Codex's B0.2 gave `BuildEdge` two optional parameters (`int blockA = -1, int blockB = -1`), so the outlet parameters come **after** them, and both calls pass `data, wallFaces` after the B0 arguments.
- `data/20_contract_patch.py.txt` anchors on text and handles both shapes (the 12:38 file and main). It writes nothing unless every anchor matches exactly once. On main it produces exactly `data/20_contract.diff.txt` (checked with `cmp`).
  - `python3 20_contract_patch.py.txt <project>/Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs` — the request.
  - `--probe` adds the clone-only timers (`// PROBE` lines and the `OutletProbe` class). They are test instrumentation, not part of the request.
- It composes with the window-landing contract (`FrontRoomsMapWorld.window-kit.r4-all.diff`, both the `win_r4` copy and the newer `win_r5` copy of 08:16 today): each was applied to main `d610d3a` together with this patch in both orders at 08:23, and both orders gave the identical file. The outlet line sits just above `if (kind == EdgeKind.Arch) return;`; the window hunks start at that line and below.

**`data/21_contract.ee5c9bb.diff.txt`** (122 lines, the request itself, no test instrumentation). Re-made 2026-10-07 against **main `ee5c9bb`** (MapWorld sha1 `9abacc6f`, unchanged since `a5262fb6`). Its +/− lines are byte-identical to the 2026-10-04 request (`data/20_contract.diff.txt`, main `d610d3a`); only the hunk line numbers moved (+2 to +42), because the map chat's `b5f381f` (2026-10-04 14:04, `PassageRevision`) added lines above them. `data/20_contract_patch.py.txt` still produces it exactly on main (checked 2026-10-07 23:4x). It still composes with the window-landing contract (`window_landing/FrontRoomsMapWorld.window-kit.r6-all.diff`): applied to main's MapWorld in both orders, the two results are identical.

```diff
--- a/Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs
+++ b/Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs
@@ -391,6 +391,8 @@
     bool hasStartArea;
     GridCoord startDoorCell;
     readonly List<GridCoord> scratch = new List<GridCoord>();
+    // Outlets (FrontRoomsWallFixtures): every wall face BuildEdge built for the chunk being built.
+    readonly List<FrontRoomsWallFixtures.WallFace> wallFaces = new List<FrontRoomsWallFixtures.WallFace>();
     readonly List<Door> movingDoors = new List<Door>();
     Transform player;
     MaterialPropertyBlock block;
@@ -980,6 +982,7 @@
             return builder;
         }
         var collision = new MeshBuilder();
+        wallFaces.Clear();
         int BlockOf(int i, int j, float ceiling) => (i / BlockCells + j / BlockCells * blocks) * heights + HeightClass(ceiling);
 
         void Solid(int blockIndex, Material material, Vector3 center, Vector3 size, float repeat)
@@ -1029,12 +1032,14 @@
                 eastHeight, eastA, eastB, BlockOf(i, j, eastHeight), Get, Solid, origin,
                 !BothInStartArea(new GridCoord(cell.x, cell.y - 1), new GridCoord(cell.x + 1, cell.y - 1)),
                 !BothInStartArea(new GridCoord(cell.x, cell.y + 1), new GridCoord(cell.x + 1, cell.y + 1)) && !(startSide && cell.y + 1 == startArea.yMax),
-                eastSide ? BlockOf(i, j, height) : -1, eastSide ? BlockOf(i, j, MapGrid.CeilingHeight(eastZone.height)) : -1);
+                eastSide ? BlockOf(i, j, height) : -1, eastSide ? BlockOf(i, j, MapGrid.CeilingHeight(eastZone.height)) : -1,
+                data, wallFaces);
             BuildEdge(chunk, northKind, cell, north, new Vector3(i * cs, 0f, (j + 1) * cs), Vector3.right,
                 northHeight, northA, northB, BlockOf(i, j, northHeight), Get, Solid, origin,
                 !BothInStartArea(new GridCoord(cell.x - 1, cell.y), new GridCoord(cell.x - 1, cell.y + 1)),
                 !BothInStartArea(new GridCoord(cell.x + 1, cell.y), new GridCoord(cell.x + 1, cell.y + 1)),
-                northSide ? BlockOf(i, j, height) : -1, northSide ? BlockOf(i, j, MapGrid.CeilingHeight(northZone.height)) : -1);
+                northSide ? BlockOf(i, j, height) : -1, northSide ? BlockOf(i, j, MapGrid.CeilingHeight(northZone.height)) : -1,
+                data, wallFaces);
 
             if (data.pillar[i + j * (n + 1)] && !TouchesStartArea(cell.x, cell.y, cell.x, cell.y))
             {
@@ -1075,6 +1080,7 @@
         }
         RegisterRelayEntries(chunk, data);
         AddZoneGrades(chunk, data);
+        FrontRoomsWallFixtures.Install(chunk.root.transform, wallFaces, Cache.Generator.Seed, data, hasStartArea ? startArea : default, chunk.meshes);
         Furnish(chunk, data);
         built[coord] = chunk;
     }
@@ -1176,7 +1182,8 @@
     /// </summary>
     void BuildEdge(BuiltChunk chunk, EdgeKind kind, GridCoord a, GridCoord b, Vector3 start, Vector3 along, float height,
         Material wallA, Material wallB, int blockIndex, BuilderFn get, SolidFn solid, Vector3 origin,
-        bool mayExtendStart = true, bool mayExtendEnd = true, int blockA = -1, int blockB = -1)
+        bool mayExtendStart = true, bool mayExtendEnd = true, int blockA = -1, int blockB = -1,
+        MapChunk faceData = null, List<FrontRoomsWallFixtures.WallFace> faces = null)
     {
         if (kind == EdgeKind.Open) return;
         var length = MapGrid.CellSize;
@@ -1222,7 +1229,12 @@
             Skin(sideB, wallB, fB, tB, 1f);
         }
 
-        if (kind == EdgeKind.Wall) { Piece(0f, length, 0f, height, true, true); return; }
+        if (kind == EdgeKind.Wall)
+        {
+            Piece(0f, length, 0f, height, true, true);
+            AddWallFaces(faces, faceData, kind, a, b, start, along, 0f, 0f, 0f, 0f);
+            return;
+        }
 
         float width, c, openingTop, sill = 0f;
         if (kind == EdgeKind.Arch)
@@ -1247,6 +1259,7 @@
         Piece(c + width * .5f, length, 0f, height, false, true);
         Piece(c - width * .5f, c + width * .5f, openingTop, height, false, false);
         if (sill > 0f) Piece(c - width * .5f, c + width * .5f, 0f, sill, false, false);
+        AddWallFaces(faces, faceData, kind, a, b, start, along, c - width * .5f, c + width * .5f, sill, openingTop);
 
         if (kind == EdgeKind.Arch) return;
 
@@ -1370,8 +1383,45 @@
             if (type != null && typeof(Component).IsAssignableFrom(type)) { glassBreakable = type; break; }
         }
         return glassBreakable;
+    }
+
+    /// <summary>
+    /// Outlets: list both faces of an edge BuildEdge just built, for
+    /// FrontRoomsWallFixtures. Faces round the start area are left out. Side 0
+    /// faces into a (this chunk's cell), side 1 into b; b is in the next chunk
+    /// on the east and north border ("foreign": no room, seed-pure inputs only).
+    /// Door and Window faces are listed with their kind; the planner puts nothing on them.
+    /// </summary>
+    void AddWallFaces(List<FrontRoomsWallFixtures.WallFace> faces, MapChunk data, EdgeKind kind, GridCoord a, GridCoord b,
+        Vector3 start, Vector3 along, float openFrom, float openTo, float openBottom, float openTop)
+    {
+        if (faces == null || data == null || InStartArea(a) || InStartArea(b)) return;
+        var across = new Vector3(along.z, 0f, along.x);
+        var edge = EdgeId(a, b);
+        var o = data.Origin;
+        for (var side = 0; side < 2; side++)
+        {
+            var cell = side == 0 ? a : b;
+            var zone = Cache.ZoneOf(cell);
+            var normal = side == 0 ? -across : across;
+            int li = cell.x - o.x, lj = cell.y - o.y;
+            var own = li >= 0 && li < MapGrid.ChunkCells && lj >= 0 && lj < MapGrid.ChunkCells;
+            var room = -1;
+            if (own) for (var r = data.rooms.Length - 1; r >= 0; r--) if (data.rooms[r].Contains(li, lj)) { room = r; break; }
+            faces.Add(new FrontRoomsWallFixtures.WallFace
+            {
+                start = start + normal * ModuleUnits.WallHalf, direction = along, length = MapGrid.CellSize, normal = normal,
+                kind = kind, openFrom = openFrom, openTo = openTo, openBottom = openBottom, openTop = openTop,
+                edgeId = edge, side = (byte)side, cell = cell, foreign = !own, room = room,
+                zone = zone.id, theme = zone.theme, height = zone.height, ceiling = MapGrid.CeilingHeight(zone.height),
+                zoneBorder = Cache.ZoneOf(side == 0 ? b : a).id != zone.id, baseTop = 0f,
+            });
+        }
     }
 
+    /// <summary>The world edge id shared by both faces of a wall (outlets hash it with the face side).</summary>
+    public static long WallEdgeId(GridCoord a, GridCoord b) => EdgeId(a, b);
+
     static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
 
     /// <summary>
```

**Nothing else in MapWorld changes.** `Unregister`, `Drop`, `RebuildChunk`, `FreeMeshes`, colliders, nav, `IsArchitecture` and `Prewarm` stay as they are. The outlet set is a component on `chunk.root`, so it dies with the chunk (registry 25 → 25 after rebuilds → 0 after destroy, on all 100 seeds).

**What the map chat runs after applying it:** spec §4.5, with today's suite sizes (§7).

**Phase 2 proposals stay as spec §4.3:** C-2 Level Designer pins per edge (`ModuleOutletPin`, `outletDensity`), C-3 switches beside doors, C-5 lamp temperament per cell. **C-4 is not requested** (§5).

---

## 7. Tests (all in the clone, final code)

Times and loads come from `data/20_timeline_with_load.txt`. Load = the 1-minute average at the start and end of the run.

### 7.1 Contract probe, 100 seeds (`OUTLET_PROBE_SEEDS` = 100 seeds; 06:37–06:48, load 35 → 253)

Counts are exact; the run's milliseconds are **measured under load, re-measure** (re-measured in §7.2).

| Check | Result |
|---|---|
| Faces from `BuildEdge` vs an independent walk over `Cache.Edge` | **315,052**; 0 missing, 0 extra, 0 kind mismatches. 31,117 foreign (9.9 %) |
| Arch spans vs `CrossingPoint` | 98,772 arches, 0 mismatches, worst 0.49 mm |
| Fixtures on solid shell (raycast from 0.30 m in front; from above for floor boxes) | **84,809 / 84,809**, worst 0.4 mm |
| Door/Window faces, start area, end margins, arch clearance, same-face gap < 0.30, back-to-back < 0.61 | **0** each |
| Heights (0.31 / 0.41 / 0.46 / 1.07 / 1.22, clear of a base trim), one receptacle per face, room caps, chunk cap, columns, cove | **0** each. Largest rooms: Level 0 10, Office 24 (the caps) |
| Furniture colliders inside a plate volume | **0** (furniture bounding boxes near plates: 208, all box-only; mostly wall-row back panels 11 mm off the face) |
| Rebuild (300 chunks) / revisit shift (400 chunks) | 0 diffs / 1,258 border-face fixtures, 0 diffs; 400 / 400 interiors re-planned |
| Re-plan vs installed plan (2,500 chunks); independent second build (5 seeds) | 0 diffs; 0 diffs |
| Registry | 25 → 25 → 0 on every seed |
| With vs without outlets (5 seeds, same build) | **0** collider difference, **0** renderer difference |
| Totals | 33.9 fixtures per chunk, max 68. By kit: Duplex 57,668; Duplex20 3,934; Steel 4,459; Cracked 3,299; Jumbo 3,213; Scorched 1,456 (draws Duplex); Blank 2,446; Bare 2,007; Toggle1 2,120; Toggle2 227; JackPhone 2,487; JackData 330; JackBNC 304; FloorBox 807; FloorBox2 52 |
| Metres of solid wall per receptacle (target) | Level 0 corridor **11.3** (10.5), Level 0 room **4.3** (4.0), Office corridor **9.7** (9.0), Office room **3.5** (3.3), Tall **7.9** (7.5). As in the spec, slightly sparse: some faces have no valid point, and back-to-back drops remove 2.1 % |
| Wear and detail | IG 1,253; companions 3,121; off-height 2,344 (915 sideways); ground-up 11,834; no screw 6,405; papered over 2,807; aged 13,255; back-to-back moved 5,610, dropped 1,748; room-side forced 657; kept out 54 |
| Stage 2 | 1,140 wall-unit occluders; 277 plates hidden; 4 receptacles added; pods 632 / 632 and wall rows 109 / 118 desks with power within 2 m |
| Title stream | per room Lobby 6.8, Shift 7.9, Office 15.6, Run 5.35, Exit 3.0; 0 below base clearance, 0 outside margins, 0 Run plates not white |

### 7.2 Timing (10 seeds, 07:26:54–07:27:26, load **12.4 → 15.3**: valid)

| What | Median | p95 | Max |
|---|---|---|---|
| `Install` per chunk, inside `BuildInto` (the map's call) | **0.149 ms** | 0.275 ms | 0.896 ms (first chunk of the first seed: kit footprints and materials load) |
| `Install` per chunk, warm (1,250 calls) | 0.115 ms | 0.199 ms | 0.558 ms |
| `Plan` alone (2,500 calls) | 0.092 ms | 0.161 ms | 0.535 ms |
| Face collection in `BuildEdge`, per chunk | 0.033–0.049 ms | | |
| Garbage per `Plan` | 0 bytes | | |

Under load (100 seeds, load 35–253) the same call measured median 0.65 ms, p95 5.1 ms: **measured under load**, kept only for the record.

### 7.3 Render cost (R3)

| What | Value | Load |
|---|---|---|
| Submit per camera, real draws (44 T-R2/R-4 captures) | median **0.070 ms**, max 0.094 ms; 0.473 ms on the first shot (the kits load) | 7.9–12.4 |
| Submit per camera, dry run (1,000 random views) | mean 0.04 ms, p95 0.02 ms | 12.4–15.3 |
| Fixtures drawn per view | mean 8.4, p95 14, max 23 | counts |
| Draws per pass today (palette not instanced: one `RenderMesh` per instance and submesh) | mean **32.6**, p95 56, max 87 | counts |
| Draws per pass with P-6 (instancing on) | mean **14.3**, p95 27, max 38 | counts |
| Triangles per view (LOD0 at every distance until P-1) | mean 23.6 k, p95 39.9 k, max 61.9 k | counts |
| Captures, draws per shot | median 35 (17.5 with P-6), max 63 (28); triangles median 24.9 k, max 43.8 k | counts |

### 7.4 The map chat's suites, with outlets (on) and with `OUTLETS_OFF=1` (off)

| Suite | On | Off | Load |
|---|---|---|---|
| `FrontRoomsRelayNavTest.RunBatch` | **60/60** hunts, 0 pass-throughs, door rule PASS | 60/60 | 253 → 268 / 34 |
| `FrontRoomsMapInteractionTests.RunBatch` | **143/143** | 143/143 | 268 → 125 / 25 |
| `FrontRoomsLevelDesignerTests.RunBatch` | **138/138** | 138/138 | 125 → 107 / 20 |
| `FrontRoomsMapVerification.RunBatch` | PASS, 100/100 seeds, 924 modules placed | same | 107 → 83 / 17 |
| `FrontRoomsFixtureTickTests.RunBatch` | 28/28 | 28/28 | 83 → 47 / 18 |

The suites grew since the spec (interaction 44 → 143, designer 85 → 138). "Unchanged" here means on = off.

### 7.5 Autopilot (`FrontRoomsMainScenePlaytest.RunBatch`, with graphics; 07:10–07:15, load **5.7–7.3**: valid)

| Seed | Mode | Result | Avg fps | p95 / p99 ms | Worst frame | Max scene colliders / renderers |
|---|---|---|---|---|---|---|
| 2554 | on | PASS, ended by time (75 s) | **59.0** | 16.8 / **17.4** | 686 ms (map zone hitch at 11.8 s) | 401 / 3,892 |
| 2554 | off | PASS, caught (27 s) | 58.5 | 16.8 / 18.2 | 434 ms (zone hitch) | 401 / 3,892 |
| 20388 | on | PASS, caught (48 s) | **58.7** | 16.8 / **17.7** | 397 ms (zone hitch) | 389 / 3,762 |
| 20388 | off | PASS, caught (35 s) | 58.6 | 16.8 / 17.6 | 437 ms (zone hitch) | 389 / 3,793 |

- Gate ≥ 55 fps, p99 ≤ 33 ms: **met** on both seeds.
- Colliders are equal. The renderer maxima differ only because the runs took different paths (the Relay caught the autopilot at different times). The exact on/off comparison is §7.1 (same build: 0 and 0).
- **Caveat.** In batch mode the game camera renders only for the playtest's screenshots: R3 saw **0.02 cameras per frame**. So the fps gate measures CPU, not outlet rendering. The render cost is §7.3. R3's worst submit in these runs was 0.21 ms. The 4 worst frames are the map's zone hitch, which shows in the off runs too.

### 7.6 T-R2 and the Level Designer preview (07:26, load 7.9–12.4)

- `FrontRoomsOutletCapture.RunBatch`: edit mode, the game's URP path with post, 1920 × 1080, 4× MSAA, map at root (4992, 0, 4992), seeds 2554 and 20388, plus the title stream's edit-mode rooms.
- Every shot is rendered with R3 on and off. **22 / 22** shots draw outlets: Lobby, Shift, Office and Run stream rooms at 3.1 m and 0.3 m; a duplex dolly at 0.3 / 1.4 / 2.8 / 6.1 m (LOD0 + screw, LOD0, LOD1, LOD2); Toggle1, the phone + data + duplex group, and a floor box at 0.5 m.
- Scene GameObjects, colliders and renderers before and after every block of shots: **equal** (for example the stream block 1,162 / 471 / 793 → 1,162 / 471 / 793). No capture-only GameObject fallback was needed.
- The harness now counts scene objects only. The first run of the day counted the kit FBX prefabs that `Resources.Load` brings in on the first draw (9 assets) and so reported a false "objects added".
- **Level Designer** (`FrontRoomsLevelDesigner.CaptureBatch`, on vs `OUTLETS_OFF`): every module's eye view changes in the lower wall band only: L0 waiting room 176 px, Office bullpen 706 px (plates and a pod floor feed), Low storage 614 px, Tall pillar hall 109 px.

---

## 8. RenderSetup proposal: P-4b + P-6 (NEEDS APPROVAL; not applied)

`data/20_rendersetup_p4b_p6.diff.txt` (75 lines) against main `d610d3a`; `data/20_rendersetup_patch.py.txt` applies it by text anchors. **2026-10-07:** main's RenderSetup changed (Q1b print array, `4393964c`); the same script still applies, and `data/21_rendersetup_p4b_p6.ee5c9bb.diff.txt` is the result on main `ee5c9bb` (the same +/− lines; only context moved).
- **P-4b, five tint-only `SurfaceDef` lines** like `Prop_PlasticRed`, all `meshUV`, all instanced:

| Slot | Tint | Smoothness |
|---|---|---|
| `Prop_ThermosetIvory` | #E6DDC2 (.902, .867, .761) | .75 |
| `Prop_ThermosetAged` | #CDBF98 (.804, .749, .596) | .60 |
| `Prop_ThermosetAlmond` | #E3D7BE (.890, .843, .745) | .70 |
| `Prop_NylonIvory` | #E2D9BF (.886, .851, .749) | .55 |
| `Prop_PlasticOrange` | #D8642A (.847, .392, .165) | .55 |

- **P-6:** a `bool instancing` field on `SurfaceDef`, `m.enableInstancing = d.instancing` in the material build, and `instancing = true` on the existing slots the outlets draw: `Prop_LaminateBeige`, `Prop_PlasticBeige`, `Prop_PlasticBlack`, `Prop_Aluminium`, `Prop_Chrome`, `Prop_Brass`, `Prop_Ceramic`, `Prop_PlasticPutty`, `Prop_CeramicGlaze`.
  - The shader already has `multi_compile_instancing` in all four passes. Renderers keep the SRP Batcher either way.
  - Effect, measured by the dry run: draws per pass 32.6 → **14.3** mean, 87 → 38 max.
- Tune the tints in look-dev L-1: under Level 0 light a plate should read about 11 % darker and warmer than the paper beside it.

---

## 9. How to land it (for the visual chat)

**2026-10-07: one apply package does steps 1 and 2** (apply convention v2, `W/apply/README.md`). `W` = `/Users/redwang/FrontRoomsVisualWork`.

```
bash /Users/redwang/FrontRoomsVisualWork/apply/outlets_c1.sh            # dry run: DRY RUN OK on main ee5c9bb (23:38) and 22bb75f (2026-10-08 00:05)
bash /Users/redwang/FrontRoomsVisualWork/apply/outlets_c1.sh --apply    # writes; backs main's two changed files up to W/backup/outlets_c1_<stamp>/
```

| Path | Action | Base (sha1, main `ee5c9bb`) | Payload (sha1) |
|---|---|---|---|
| `Assets/Scripts/Office/FrontRoomsWallFixtures.cs` + `.meta` (GUID `14ab7ea1…`) | add | absent | `50d1bc4a` / `866ac5cb` |
| `Assets/Scripts/Office/FrontRoomsWallFixtureSet.cs` + `.meta` (GUID `6f896ecc…`) | add | absent | `618e3cc1` / `41daa154` |
| `Assets/Scripts/Office/FrontRoomsWallFixtureRenderer.cs` + `.meta` (GUID `e2a60678…`) | add | absent | `66e4cef8` / `6a65a96a` |
| `Assets/Scripts/FrontRoomsRoomStream.cs` | replace (.meta kept) | `67b2bd60` | `df68c1d2` |
| `Assets/Scripts/Office/FrontRoomsOfficeKit.cs` | replace (.meta kept) | `7c63d918` | `b7a4d62b` |

- The `.meta` files are written before the scripts, so an open Unity never mints its own GUID for them. No path in main uses these three GUIDs (searched 2026-10-07).
- Kept, never written: `FrontRoomsRoomStream.cs.meta` (`629abdbd`), `FrontRoomsOfficeKit.cs.meta` (`ae758f35`), and `FrontRoomsMapWorld.cs` (`9abacc6f`, the contract's base: a note only if it moves).
- Checked: the dry run on main (DRY RUN OK); `--apply` on a scratch copy (`APPLY_REAL`, MANIFEST OK) and a second dry run there (ALREADY APPLIED); Roslyn on main `ee5c9bb` + payload, 0 errors for OSXUniversal and for the iOS defines (`data/21_roslyn_ee5c9bb.txt`); a Unity batch compile of the clone on main `ee5c9bb` + payload, 0 errors (§12).
- On a base mismatch later, run `W/tools/rebase_apply.sh outlets_c1` first (v2: `outlets_c1_base/` and `outlets_c1.diff` are in the package).

**What changes in the game when it lands, before anything else does:**
- **Title corridor:** the 45 placeholder boxes with `BoxCollider`s are gone. R3 draws the plates instead, with no collider. Until the integrate stage promotes the kits, main has no `Kit_Outlet*` FBX, so R3 draws its **spec-size placeholder plates** (plate, device face and dark slots as boxes; 6 shared meshes). The Exit threshold marker is built again (without its collider).
- **Office kit:** no new objects until the kits exist (`Kit_CubiclePanel_Powered` and the floor box are used only when present). Stage 2 still records wall-unit occluders.
- **Map:** nothing. `Install` is never called until the map chat applies §6.

**Then, in order:**
1. The integrate stage promotes the outlet kits from `W/proj_outlet` (FBX, JSON, `.meta`; main has none, so no GUID clash).
2. Send §6 to 关卡设计 as the contract (`data/21_contract.ee5c9bb.diff.txt`, or `python3 data/20_contract_patch.py.txt <MapWorld.cs>`).
3. Decide P-4b/P-6 (§8, now `data/21_rendersetup_p4b_p6.ee5c9bb.diff.txt`) and P-1 (LODs in the FBX). R3 picks both up with no code change.
4. Do not copy `Assets/Editor/OutletCensus/*` (clone-only tests) unless the visual chat wants the probe in the project.

---

## 10. Open items

1. **Border-wall desks** (§5): 9 / 118 wall-row desks, 100 seeds. Pick fix 1 (visual-owned) or fix 2 (map, option B).
2. **P-1 / P-1b** (hand-built LOD1 and LOD2 in the FBX). Until then every kit draws LOD0 to 12 m: mean 23.6 k triangles per view.
3. **P-4b / P-6** (§8).
4. **Playtest render cost** in a real player or Play mode with a window (batch mode renders 0.02 cameras per frame).
5. Spec §8 items 1, 2, 7 and 8 are unchanged: measure a real plate; the 5-20R arm; the bare plate read; the emergency-receptacle colour.

---

## 11. Verification images

All in `images/`, JPG q85, ≤ 1,920 px wide. Figma rows: see the VERIFICATION_LOG table below.

| Image | What |
|---|---|
| `20_tr2_sheet.jpg` | T-R2: 4 stream rooms; duplex dolly 0.3 / 1.4 / 2.8 / 6.1 m; Toggle1, phone + data + duplex, 2 floor boxes |
| `20_duplex_0p3m_seed2554.jpg` | A Level 0 duplex at 0.3 m in the map (LOD0 + screw) |
| `20_dolly_seed20388.jpg` | The same dolly on seed 20388 |
| `20_stream_lobby_steel_0p3m.jpg`, `20_stream_office_0p3m.jpg` | Title stream plates at 0.3 m (Lobby stainless; Office) |
| `20_office_phone_data_duplex.jpg`, `20_lone_switch_toggle1.jpg` | Office companions; the Level 0 lone switch at 1.22 m |
| `20_designer_on_off.jpg` | Level Designer preview, outlets on vs off (4 modules) |
| `20_r4_sheet.jpg` | R-4: 8 Office rooms, 6 pile rooms, 4 close-ups at 0.3 m, the cut pair (stage 1 / stage 2) |

**2026-10-07 images (C1 re-run, §12).** VL093 (`2637:6093`) now holds the re-run's four images; the verdict did not change, so the newest run replaced the old images (VERIFICATION_LOG §1.1). VL094 keeps its image and got a note: the re-run's R-4 numbers are identical.

| Image | What | Figma |
|---|---|---|
| `21_tr2_sheet_1007.jpg` | T-R2 with the final O1–O3 kits: 4 stream rooms; duplex dolly 0.3 / 1.4 / 2.8 / 6.1 m (seed 2554); Toggle1, phone + data + duplex, 2 floor boxes | VL093 `2637:6104` |
| `21_designer_on_off_1007.jpg` | Level Designer preview, outlets on (20:09) vs off (20:06), back to back | VL093 `2637:6105` |
| `21_map20388_duplex_0p3m.jpg` | A Level 0 duplex at 0.3 m in the map, seed 20388 (LOD0 + screw) | VL093 `2637:6106` |
| `21_stream_lobby_steel_0p3m.jpg` | Lobby stream room, stainless plate at 0.3 m | VL093 `2637:6107` |

---

## 12. Re-verification on main `ee5c9bb` (2026-10-07)

**Why.** The 2026-10-05 reboot wiped the 2026-10-04 clone. Main also moved (about 30 commits). This run checks that the unchanged C1 code still holds on today's main, and packages it.

**Base and clone.**
- Clone `W/proj_outlet` (made 16:32 from `W/proj_audit` + main `279c144` worktree). The three new Office files were restored from `data/20_*.cs.txt` (byte-identical). RoomStream, OfficeKit and the MapWorld contract were re-applied from the `a5262fb6` diffs.
- Re-synced from main twice, non-outlet files only: `a5262fb6` at 18:16, **`ee5c9bb` at 23:36**. `apply_local_patches.py`: nothing to do.
- Main's `FrontRoomsRoomStream.cs`, `FrontRoomsOfficeKit.cs` and `FrontRoomsMapWorld.cs` are unchanged from `a5262fb6` to `ee5c9bb`. The clone's three files equal main + `data/20_RoomStream.a5262fb6.diff.txt`, main + `data/20_OfficeKit.a5262fb6.diff.txt` and main + the contract with `--probe` (checked with `cmp`, 23:37).
- Between `279c144` and `ee5c9bb`, main changed no map, Levels, Relay or Office-kit file. It changed the Q1b print array, RenderSetup (print import rules), the window facade r6, touch/handheld UI and `FrontRooms3DGame.cs` (handheld UI switches only).
- Codex audit: `codex_audit/20_findings.md` still does not exist. `00_main_state.md` lists D3 outlets as "not touched", and no review file names an outlet finding (checked 23:3x).

**Results.** Load = the 1-minute average at the start → end of the run. Every millisecond taken today was at load > 32, so all of today's milliseconds are **measured under load, re-measure**. The valid low-load timings are still the 2026-10-04 ones (§7.2, §7.3: load 8–15). The code has not changed since.

| Check | Base | Result | Load |
|---|---|---|---|
| Unity batch compile (`-buildTarget OSXUniversal`) | `ee5c9bb` + C1 | **0 errors** (compile_e) | 273 → 404 |
| Roslyn, main tree + payload, outside Unity | `ee5c9bb` | **0 errors**, Assembly-CSharp 76 files + Editor 51 files, OSXUniversal and iOS defines (`data/21_roslyn_ee5c9bb.txt`) | — |
| Roslyn again after main moved to **`22bb75f`** (23:46: `FrontRooms3DGame.cs` handheld code + touch `.meta` files) | `22bb75f` | **0 errors**, OSXUniversal. The final dry run on `22bb75f` (00:05): DRY RUN OK, all 8 bases match | — |
| Contract probe, 10 seeds | `ee5c9bb` | **PASS.** 250 chunks, 31,890 faces, 8,881 fixtures: every count equals 2026-10-04. 0 collider and 0 renderer difference on vs off. 0 diffs between independent builds. 0 bytes garbage per `Plan` | 404 → 382 |
| Install per chunk, the map's call | `ee5c9bb` | median 1.80 ms, p95 19.4, max 20.4 — **measured under load, re-measure** (2026-10-04 at load 12–15: **0.149 ms**) | 404 → 382 |
| Install warm / Plan alone | `ee5c9bb` | median 0.311 / 0.241 ms — **measured under load, re-measure** (2026-10-04: 0.115 / 0.092 ms) | 404 → 382 |
| Contract probe, **100 seeds** | `279c144` worktree | **PASS.** 315,052 faces, 84,809 fixtures, every rule count of spec §4.4 at 0; byte-identical totals to 2026-10-04 (`data/21_probe_100seeds_1007.json`) | 185 → 536 |
| `FrontRoomsRelayNavTest.RunBatch` | `279c144` wt | **60/60** on and off, 0 pass-throughs, door rule PASS | 536 → 528 / 677 → 793 |
| `FrontRoomsMapInteractionTests.RunBatch` | `279c144` wt | **143/143** on and off | 528 → 553 / 793 → 644 |
| `FrontRoomsLevelDesignerTests.RunBatch` | on `279c144` wt, off `a5262fb6` | **138/138** on and off | 553 → 660 / 636 → 567 |
| `FrontRoomsMapVerification.RunBatch` | on `279c144` wt, off `a5262fb6` | **PASS** 100/100 seeds, 924 modules placed, on and off | 660 → 580 / 567 → 573 |
| `FrontRoomsFixtureTickTests.RunBatch` | on `279c144` wt, off `a5262fb6` | **28/28** on and off | 580 → 677 / 573 → 534 |
| T-R2 + R-4 captures (`FrontRoomsOutletCapture.RunBatch`, graphics) | `a5262fb6` + final O1–O3 kits | **22 / 22** shots draw outlets; scene GameObjects / colliders / renderers equal before and after every block. R-4 analysis identical to 2026-10-04 (17,106 plates, 71 hidden, 32 cuts all on hidden plates, 0 on drawn, 161 / 165 desks powered) | 534 → 799 |
| Draws and triangles per shot (44 shots) | same | draws median **35**, max 63 (with P-6: 17.5 / 28); triangles median **24.9 k**, max 43.9 k; same as 2026-10-04 within 0.1 % (the kits were rebuilt) | counts |
| R3 submit per camera (44 shots) | same | median 0.137 ms, max 0.973 ms — **measured under load, re-measure** (2026-10-04: 0.070 ms) | 534 → 799 |
| Level Designer preview, on vs off (back to back) | `a5262fb6` | change only in the low wall band: 184 / 775 / 758 / 156 px (L0 waiting, Office bullpen, Low storage, Tall pillar hall) | 572 → 169 |
| Autopilot 2554, on / off (`FrontRoomsMainScenePlaytest.RunBatch`, graphics, 75 s) | `ee5c9bb` | **PASS / PASS** (ended by time, not caught). 48.8 / 43.1 fps, p99 111.7 / 150.6 ms — **measured under load, re-measure**: the gate (≥ 55 fps, p99 ≤ 33 ms) cannot be judged at this load; outlets on was not slower than off. Max scene colliders 393 / 411, renderers 3,926 / 3,902 (different paths; the exact on/off count is the probe's 0 / 0). R3: 27 sets, ≤ 861 fixtures, ≤ 16 draws per frame | 382 → 317 / 320 → 360 |
| Autopilot 20388, on / off | `ee5c9bb` | **PASS / PASS** (ended by time). 40.1 / 54.7 fps, p99 176.0 / 63.7 ms — **measured under load, re-measure** (load rose to 569 during the on run). Max scene colliders 396 / 396, renderers 3,802 / 3,747. R3: 27 sets, ≤ 996 fixtures, ≤ 13 draws per frame, submit mean 0.018 ms, max 1.44 ms per frame (under load; 2026-10-04: max 0.21 ms at load 6–7) | 360 → 569 / 576 → 484 |
| Autopilot, earlier pairs | `a5262fb6` | 20388 on / off PASS (19:46–19:52, load 760–815, 54.7 / 49.0 fps). 2554 on and off ran their 75 s but reported FAIL (19:40–19:46): 1 error each, a `BankLoadException`, because the clone had no FMOD banks (finding 2). The pair was re-run above with the banks | 760 → 815 |

**Findings of the re-run.**
1. **Cold-import artefact in the clone (not an outlet effect).** The first Level Designer on/off pair after the 18:16 resync (19:30, 19:36) drew the waiting room's ladder chairs, side table, wall clock and outlet plate in one flat red, because their textures were still importing in that batch session. It hit on and off alike. The repeats at 20:06 and 20:09 draw correctly, and every other module view is pixel-identical between repeats (`data/21_designer_onoff_1007.txt`).
2. **FMOD banks are not in `W/proj_audit`.** The first autopilot pair on seed 2554 (19:40) logged `BankLoadException … Master.strings.bank` at start-up, so both runs reported FAIL. The banks (`FMOD/FrontRooms/Build/Desktop`, 1.5 MB) were copied from main into the clone at 19:45; later runs load them. Other clones made from `W/proj_audit` will hit the same failure (TOOLS.md §2 mentions it).
3. **No timing could be taken at low load today.** The machine ran at load 170–800 all evening. The counts (faces, fixtures, draws, triangles, colliders, renderers) are exact and match 2026-10-04; the milliseconds above are for the record only.

**Files written by this run.**
- Apply package (v2): `W/apply/outlets_c1.sh`, `outlets_c1_bases.txt`, `outlets_c1_keep.txt`, `outlets_c1_expected.txt`, `outlets_c1_payload/` (8 files), `outlets_c1_base/` (2 files), `outlets_c1.diff`.
- `data/21_contract.ee5c9bb.diff.txt` (the contract on today's main), `data/21_rendersetup_p4b_p6.ee5c9bb.diff.txt` (proposal, NEEDS APPROVAL), `data/21_roslyn_ee5c9bb.txt`, `data/21_probe_timing_10seeds_ee5c9bb.json`, `data/21_probe_timing_10seeds_a5262fb6.json` (19:52, load 815 → 568: Install median 8.97 ms, under load), `data/21_probe_100seeds_1007.json`, `data/21_capture_tr2_r4_1007.json`, `data/21_designer_onoff_1007.txt`, `data/21_playtest_*_1007.report.json`, `data/21_timeline_with_load_1007.txt`.
- `images/21_*.jpg` (4), placed in VL093.
