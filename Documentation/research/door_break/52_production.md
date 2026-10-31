# 52: Door break production log (from 2026-10-30)

Owner: 游戏视觉 (game visuals), account A, session local_5656cffd, door-break production workflow (wf_2028e947-01b). Red approved the design on 2026-10-30, verbatim "1a，2 要，3 要，4 预算，5 要" (`50_rethink.md` §6). Each stage appends what it built, the files, the budgets, and what the next stage must know. LF only. Main is read-only; the work is in the clone `W/proj_doorbreak` (W = `/Users/redwang/FrontRoomsVisualWork`).

## Stage: research + spec, the dust and grit of a door break (2026-10-30, about 12:15-13:10)

Red, 2026-10-30, verbatim: "同时目前门被撞之后掉落下来的碎沙，我希望能够有更多颗粒，这部分需要你research一下，现在白色的原型雾气看起来不是特别的realistic".

**Built:** documents only.
- `54_dust_grit.md`: the research and the spec.
- `media_candidates.md`: a new section, "Research 2026-10-30 (dust and grit)".
- `SOURCES.md`: a pointer section.

No clone code was changed, no Unity job was run, and nothing was downloaded. Footage was viewed muted in the browser pane as contact sheets and the tab was closed.

**Clone re-sync (start of stage):**
- Ledger check: `progress.py check --session local_5656cffd visual` gave YOURS.
- No main file in the synced folders was iCloud-dataless (0 found).
- `rsync -a --checksum` of main's `Assets/Scripts`, `Assets/Editor`, `Assets/Resources` and `Tools` into `W/proj_doorbreak`, without `--delete`. It brought in:
  - the landed double-door hardware: `FrontRoomsDoubleDoorKit.cs`, `Kit_DoorKickPlate(_B)`, `Kit_DoorPushPlate`, `Prop_KickPlate630(B)`, `Prop_PushPlate630`, `Prop_Stainless630` and their textures;
  - the plate generators in `Tools/Blender/frontrooms_kit/assets/`;
  - two `.pyc` files.
- Then `install_tools.py proj_doorbreak` (silencer and test bundle id; 42 files unchanged). Main HEAD is still `3ea3d7bb`; main moves by apply scripts, not commits.
- The prototype's own files were kept.

**The finding:**
- The prototype's dust is round white puffs: a radial-blob texture, a pale beige Simple Lit material, 0.1 to 0.36 m cards that barely fall, 16 per blow.
- The 0.1 to 2 mm grit Red asks for does not exist in it.
- Real breaks give size-sorted grains (crumbs land in about 0.5 s, grit in 1 to 2 s, a fine sift for 3 to 6 s) and a thin haze that is visible only when back-lit.

**The spec, in one line:** GPU-drawn grit streaks (1 px floor, opacity carries the coverage, palettes sampled from our door textures), 3 crumb materials, chip slivers, a thin forward-scattering haze with an analytic slot or notch light shaft, a persistent dust skin on the floor, and a sand trickle from the slot and notch after D2 and FINAL.

**Counts, desktop / phone (T1):**

| Type | Desktop | Phone | Live cap (desktop / phone) |
|---|---|---|---|
| Grit | ≈ 5,900 | ≈ 1,475 | 6,144 / 1,536 |
| Crumbs | 544 | 181 | |
| Chips | 190 | 63 | |
| Haze wisps | ≈ 45 | 19 | |

Per beat: `54` §4.3.

**Budgets:**
- Draws: ≤ 7 while live, +1 at rest (the skin).
- Haze fill: ≤ 0.15 screen at 3 m, ≤ 1.0 screen worst.
- CPU: ≤ 0.2 ms main + ≤ 0.3 ms jobs.
- Memory: ≤ 0.6 MB shared + 12 KB per door.
- Record: +0 B.

**The next stage must know:**
1. Delete the prototype's dust as listed in `54` §4.10. Move the old `FrontRoomsDoorBreakVfx.cs` to `W/_partial/door_break_proto_dust_20261030/` first.
2. Grit uses `GraphicsBuffer` + `Graphics.RenderPrimitives`, which no main script uses yet: compile-check the mac and iOS defines early.
3. Keep crumbs on FrontRoomsSurface with random metric UV offsets. Do not add vertex colour to the shared shader.
4. Use the GD3c keyed hash `FrontRoomsGlassFractureMeshes.Rand01`, not `System.Random`. The phone tier is a prefix of the desktop lists.
5. Merge in slices of 1 ms or less (the GD3c lesson).
6. Gates DG1 to DG6 (`54` §4.11) belong to the LAND stage's one test round.
7. Optional sound ask S6 (a sift layer) goes into the handoff only.
8. Red's open choices to show in the next video: `GritVisualScale` (1.5 at 3 m), the ceiling sift (off), and six-way flipbooks (not now).
9. Estimate: about 1.5 days inside P1 and P5.

## Stage: P1 veneer family + P2 surfaces and shader + the dust spec (2026-10-30, about 13:00-13:40)

Red's approval (50 §6, verbatim "1a，2 要，3 要，4 预算，5 要") and the dust ask (54) are built in the clone `W/proj_doorbreak`. **Nothing landed in main.** Compile-checked with Roslyn (mac and iOS defines: 0 errors). One smoke of the film harness on the veneer leaf, run three times while fixing (`-dbMode smoke`). No review stage, no full test round (rule 23: that is the LAND stage's).

**Start of stage:**
- Ledger `progress.py check visual` gave YOURS.
- 0 dataless files in main's synced folders.
- `rsync --checksum` from main brought in the Relay v2 files (`FrontRooms3DGame(.RelayV2).cs`, `FrontRoomsHunter.cs`, `FrontRoomsMapHunter(.V2).cs`, `FrontRoomsRelayDirector.cs`) and three map editor tests. Then `install_tools.py` was run on the clone.
- The prototype's files were copied to `W/_partial/door_break_proto_20261030_p1p2_pre/` before the rewrite. The old dust VFX is also in `W/_partial/door_break_proto_dust_20261030/`, as `54` §4.10 asks.

### What was built

**P1, the veneer family:**
- **One family with three split motifs.** The family is split into these files:
  - `Kit_DoorLeafBreak_Veneer_DA` (base): the body at LOD0/1/2 and the Ward armor outside the window. Its sidecar holds the anchors, the 3 motifs, the stage info, the palettes and the budgets.
  - `_M0.._M2`: the cassettes M/D1/D2/TORN at LOD0/1/2, the leak cards, and the Ward armor inside the window, cut along each motif's split.
  - `_Deb0.._Deb2`: the FINAL debris per motif. These are readable, for the floor merge.
- **LOD2 cassettes:**
  - M and D1 are the plain window block (12 tris).
  - D2 is a coarse slot (48 tris).
  - TORN is the notch (32 tris).
- **Four baked throws** (`_throw0..3`), all ballistic. Every gate passes: 0 keys below the floor, 0 in the wall. They settle in 1.07 / 1.16 / 0.99 / 0.99 s.
  - The keys play *relative* to each motif's rests, so one throw fits all three motifs.
  - The splinter and crumb shapes are the family's; only their roots follow the motif.
- **The motif and the throw come from the edge hash.** Over the 90 dressed doors in the smoke run, the motifs split 31/29/30 and the throws 22/24/22/22.
- **Mirror bursts.** The kit, debris and VFX roots are scaled by (burst, 1, 1). The smoke's mirrored take flips the burst sign and swaps every stage on its blow frame.
- **The "slot swap".** The family hangs on Veneer_DA, Veneer_Oak_DA and Ward_DA. At the swap, the break wears the intact leaf's own face, hardware and plate surfaces:
  - the walnut `Door_Veneer`, or the Ward leaf's laminate;
  - brass, or satin chrome on the OF-F and EX-F rows;
  - the Ward leaf's stainless armor, which becomes its own parts.
  - Oak rows hang Veneer_DA since door_wood_walnut, so they get the walnut.
- **The floor merge:**
  - Merges 31 pieces and 734 crumbs and chips into one floor object, one draw per material (5).
  - The merge runs in slices: debris pieces, then crumbs and chips 160 at a time, then the near mesh, then the far mesh.
  - The floor has its own LOD. Near (up to the leaf's 4 m switch) is everything. Far is the pieces only, and it culls with the leaf.
- **`FrontRoomsDoorBreakRecord`, 16 B** (checked with `Marshal.SizeOf`): edge, stage, burst, motif, throw, flags (broken, pose B, settled, env set, hard floor, blows struck in bits 8-11), and a 16-bit env hash.
  - The env hash takes the place of the design's break time, which nothing reads.
  - Keyed by (map, floor seed, edge). Cleared on `MapRunStarted` and `FloorChanged`.
  - A capped side **floor pack** (skin bytes + resting solids, about 21.7 KB per broken door, at most 48 packs) makes a restore exact.
- **Restore on `DressDoor`.** One try/catch-wrapped call in `FrontRoomsInteractableKit.Door.cs`:
  - stage > 0: body + the kept cassette;
  - broken: TORN, the debris at the last key, the floor pack, then the merge, pose (a) and the drooped levers;
  - a map-reset door drops its record.
- **The env-hash check after a shift** (GD3c's rule). It runs 3 frames after a restore and before the merge: a piece that would sit inside a new prop is dropped.
- **Kept stage** (visual side of M2): shown = max(kept, mapped). A kept stage never re-emits its split or notch dust, and the head sift keeps emptying (blows struck are in the record).
- **Pose (a)** and the leak card as approved. The leak also drives the dust's light shaft.
- **The driver** (`FrontRoomsDoorBreakDriver`) uses only existing events, so there is **no map edit**: `MapRunStarted`, `FloorChanged`, `MapHunter.DoorBlow` + `BlowIndex`/`BlowCount`/`Position`, and `DoorBrokenFrom`. The door is the dressed rig at the blow's position that `IsBeingBroken`. Map ask M1 would make this lookup exact.
- **Fixed: the prototype's E→M shading shift** (51 §4.1).
  - Cause: smoothing by angle bent the body's top and bottom face rings 11.25° into the 22.5° arris band. That put a hot specular spot on the top 0.4 m of the leaf from the first swap on.
  - The intact kit's faces are flat. Measured on LOD0/1: 0.04°.
  - The generator now writes flat custom normals on the face planes, and the hotspot is gone in the smoke frames.
  - The interface faces between the body and the window are deleted (50 §3.7 item 1).

**P2, surfaces and shader:**
- **`FrontRoomsSurface.shader`** gets `#pragma shader_feature_local_fragment _FR_DAMAGE`, with `_FR_OWN_LIGHTS` kept.
  - Three CBUFFER fields, present in every pass: `_FR_DamageStage`, `_FR_DamageRect`, `_FR_DamageFines`.
  - Two textures on one sampler.
  - The onset mask is sampled in **leaf object space**, which is canonical: the mirror is in the object matrix. The left half of the map is the burst face, the right half the push face.
  - Reveal: `saturate((stage - onset) * 12 + 1 + noise)`.
  - B is lighter, matte lifted fibre (never dark ink). A is the fines wipe. G becomes cavity, and the normal map's dish deepens with the stage.
- **8 shared stage materials** (assets, so `shader_feature` ships them): `Door_Veneer_Damage_{M,D1,D2,TORN}` and `Prop_WoodLaminate_Damage_{…}`.
  - The burst side needs no second set, because object space is canonical. So the "×2" is the two faces a broken leaf can wear: walnut and the Ward laminate.
  - No MaterialPropertyBlock, so the SRP Batcher holds.
  - Only the cassettes wear them; the body keeps the plain face.
- **SurfaceDefs in `FrontRoomsRenderSetup.cs`:** `Door_CoreWood`, `Door_CoreStile`, `Door_CoreSteel` (new texture: 18 ga skin, primer, kraft honeycomb), and the 8 stage defs via `DoorBreakDefs()`.
  - New entry point `EnsureDoorBreakSurfaces()`; the harness uses it now.
  - The damage keyword and properties are written only on the stage materials. Every existing `.mat` is byte-identical to main (rsync dry-run: 0 differences).
- **Importer rules:**
  - `Door_Damage_*`, `Door_DustWisp*` and `Door_Grain*` are clamped and linear.
  - The kit importer makes `Kit_DoorLeafBreak_*_Deb*` readable. No other kit changes, and the importer version was not bumped.
- **The walnut face adopted.** The break's face is the intact leaf's `Door_Veneer` (walnut), and the cut strip's veneer layer is now walnut (sRGB 140, 88, 66).

**The dust (54 §4, veneer family):**
- **`FrontRoomsDoorBreakVfx` rewritten:**
  - **Grit** (0.1-2 mm, 45 % fine sift): `GraphicsBuffer` + `Graphics.RenderPrimitives`, submitted per camera in `beginCameraRendering`, so the harness's multi-camera renders get it.
  - **Crumbs and chips:** 3 dynamic meshes (core, stile, face) on FrontRoomsSurface with random metric UV offsets. Vertices are shared per piece.
  - **Haze:** at most 64 wisps, one procedural draw.
  - **Dust skin:** 128 × 100 R8 at 16 mm.
  - **Slot and notch trickles** (τ 0.35 s, cut at 1.0 / 1.2 s).
  - **Emitters:** the head sift (0.6^k), split breath, split through, slot open, notch burst, top-pivot tear, and an edge shake at the stop.
  - **The gust** on the throw curve. The leaf is a kinematic plane, and the notch is open from FINAL.
  - Every random value is `Rand01(seed, emitter*128 + j, beat, i)`; the phone tier is a prefix.
  - Palettes are sampled from our textures into the sidecar.
- **Shaders:** `FrontRooms/DoorGrit`, `FrontRooms/DoorHaze`, `FrontRooms/DoorDustSkin` and the include `FrontRoomsDoorDust.hlsl` (HG g 0.6 capped at ×6, the analytic leak shaft).
  - Grit: alpha-to-coverage, with a coverage-hash keep when MSAA is 1.
  - Haze: up to 4 shadowed lights per pixel, soft depth fade over 0.15 m, camera fade 0.6 → 0.25 m, alpha ≤ 0.30, eroding filaments. Phones use a vertex-lit path.
  - All exclude GLES3: WebGL does not dress doors.
- **Textures:** `Door_DustWisp_D` (8 filament frames, never round) and `Door_Grain_D` (8 silhouettes), from `Tools/door_break/make_dust_textures.py`, which also writes `palettes.json`.

### Files (clone; the packaging stage diffs them)
- **Changed main files.** Bases are main's current files; their mtimes predate this stage's sync. md5 of main's base:

  | File | Base md5 |
  |---|---|
  | `Assets/Resources/Rendering/FrontRoomsSurface.shader` | `1885f5b3…` |
  | `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs` | `72b764c3…` |
  | `Assets/Editor/Rendering/FrontRoomsKitImporter.cs` | `a5409ef3…` |
  | `Assets/Scripts/Office/FrontRoomsInteractableKit.Door.cs` (the restore call) | `c6aabf60…` |

  Re-check them before packaging: a double-door-hardware workflow may touch RoomStream, DoubleDoorKit or StationDress, and these files may move.
- **New runtime:** `Assets/Scripts/Rendering/Doors/FrontRoomsDoorBreakVisual.cs`, `FrontRoomsDoorBreakVfx.cs`, `FrontRoomsDoorBreakRecord.cs`, `FrontRoomsDoorBreakDriver.cs`.
- **New shaders:** `Assets/Resources/Rendering/FrontRoomsDoorGrit.shader`, `FrontRoomsDoorHaze.shader`, `FrontRoomsDoorDustSkin.shader`, `FrontRoomsDoorDust.hlsl`.
- **New materials:** `Assets/Resources/Surfaces/Door_Core{Wood,Stile,Steel}.mat` and the 8 `*_Damage_*.mat`.
- **New textures** (`Assets/Resources/Surfaces/Textures/`): `Door_Core{Wood,Stile,Steel}_{A,N,S}`, `Door_Damage_Veneer_{O,N}`, `Door_DustWisp_D`, `Door_Grain_D`.
- **New models** (`Assets/Resources/Props/Models/`): `Kit_DoorLeafBreak_Veneer_DA{,_M0..2,_Deb0..2}.fbx/.json` and `_throw0..3.json`. The disk total is 2.8 MB; the textures add 2.1 MB.
- **Tools:**
  - `Tools/Blender/frontrooms_kit/assets/interact_door_break_common.py`, `interact_door_leaf_veneer_da_break.py`, `Tools/Blender/frontrooms_kit/door_break_bake.py` (`--variants 4`, ballistic by default);
  - `Tools/door_break/make_core_textures.py`, `make_dust_textures.py`, `palettes.json`.
- **Clone-only:** `Assets/Editor/DoorBreak/FrontRoomsDoorBreakFilm.cs` (new `smoke` mode).
- **W:**
  - `W/door_break/P1P2/run_smoke.sh`, `sheet.py`, `probe_normals.py`;
  - logs under `W/door_break/P1P2/logs/`;
  - frames, stats and contact sheet under `W/door_break/P1P2/out/`.
- **Asset run order:**
  1. `make_core_textures.py`
  2. `make_dust_textures.py` (palettes)
  3. Blender `interact_door_leaf_veneer_da_break.py -- --out-root <clone>`
  4. Blender `door_break_bake.py -- --out-root <clone> --variants 4`

### Budgets (measured in the smoke at 1080p, desktop, Editor; motif 0)

**Generator triangles (LOD0 / LOD1 / LOD2):**

| Part | LOD0 | LOD1 | LOD2 |
|---|---|---|---|
| body | 1,746 | 866 | 242 |
| cas_M | 504 | 280 | 12 |
| cas_D1 | 728 | 412 | 12 |
| cas_D2 | 1,960 | 900 | 48 |
| cas_TORN | 1,516 | 780 | 32 |

- Debris: 1,180 tris in 31 pieces.
- Leak cards M/D1/D2: 372 / 500 / 772.
- Ward armor: 40 per LOD outside the window; inside the window 24 / 144 / 400 / 248 at LOD0 for M / D1 / D2 / TORN.
- Every part is inside 50 §3.7's budget except the LOD2 body: 242 against an estimate of 160. The window splits the slab's middle band in three; the intact LOD2 is 148.
- The seam at the window lines matches to 0 m.

**Drawn per stage (break parts, tris / draws):**

| Stage | LOD0 | LOD1 | LOD2 |
|---|---|---|---|
| M | 2,622 / 7 | 1,518 / 7 | |
| D1 | 2,974 / 7 | 1,778 / 7 | |
| D2 | 4,478 / 8 | 2,538 / 8 | |
| FINAL (debris in flight until the merge) | 4,442 / 40 | 2,826 / 40 | |
| BROKEN, merged | 11,410 / 11 | 2,826 / 11 | 1,454 / 9 |

- At BROKEN, the near floor alone is 8,148 tris in 5 draws. That is over 50 §3.9's "≤ 3k floor" estimate, because 54 added 734 crumbs and chips. The far floor (pieces only) takes over from 4 m.

**Dust (desktop, T1), per beat E / M / D1 / D2 / FINAL / BREAK:**

| | E | M | D1 | D2 | FINAL | BREAK | Total |
|---|---|---|---|---|---|---|---|
| Grit | 260 | 456 | 794 | 956 | 2,334 | 800 | 5,600 |
| Crumbs and chips | 0 | 34 | 90 | 150 | 420 | 40 | 734 |
| Haze | 3 | 6 | 8 | 10 | 16 | 8 | 51 |

- The grit counts equal `54` §4.3 exactly.
- Peaks: grit 4,495 (cap 6,144), solids 734 (cap 1,200), haze 44 (cap 64). Dropped: 0.
- Haze alive at BREAK + 3 s: **0** (DG6).
- Grit still sifting at BREAK + 3.1 s: 954.
- Draws while live: ≤ 6.

**CPU (Editor Mono, not a player):**
- VFX main thread: peak 4.4-5.5 ms on the break frame (4,469 grit and 734 solids in flight), 0.73 ms with 954 grains. **Over 54 §4.8's 0.2 ms:** the job/Burst port is not done.
- Merge slices: first 3.0 ms (JIT), 0.9 ms on a restore, over 8-9 frames.
- Restore: 2.4-3.3 ms inside `DressDoor`, which runs 48 m or more away.

**Memory:**
- One motif in use is about 14.3k tris (base + motif + debris), about 1.4 MB of mesh (ESTIMATE). All three motifs are about 37k tris.
- Records: 16 B each. Floor packs: about 21.7 KB per broken door, at most 48.

**Smoke reads at 3 m** (C1 lock box, changed pixels at luma Δ > 6):

| Change | Pixels |
|---|---|
| S0 → E | 34 |
| E → M | 4,012 |
| M → D1 | 519 |
| D1 → D2 | 2,926 |
| D2 → FINAL | 4,488 |
| FINAL → BROKEN | 27,336 |

- M → D1 fell from 2,915 in the prototype, because the white dust cards had inflated it. It is still above 400.
- Every stage swaps on its blow frame. Hit-stop: 2 / 2 / 3 frames.
- Restore vs settled: 155-188 of 28,956 px.
- Kept stage: E M D1 → the Relay gives up → rebuild → D1 → resumed blows show D1 D1 D1 D2 FINAL.
- Contact sheet: `W/door_break/P1P2/out/smoke_sheet2.jpg`. The grit reads as falling specks and the haze stays invisible under front-lit troffers, which is the design.

### Not done here, or a stand-in
1. **No Burst/job port of the grit step.** The Editor peak is 4-5 ms. Do it in P5, or measure a player first.
2. **`FrontRoomsDoorBreakDustBake.cs` (offline patches) is not built.** The floor pack's exact skin bytes and solids replace it. A door whose pack was evicted (more than 48 broken doors) comes back with its debris pieces and torn leaf only.
3. **Ward is not filmed.** There is no RN-F door near the start of the smoke seed, so the armor parts are compile- and generator-checked only. The LAND stage should film one Ward door.
4. **The phone tier paths are written but not exercised:** LOD1 cassettes at every distance, the phone dust counts, the vertex-lit haze, no breach light.
5. **Uncalibrated:** the leak HDR, `ShaftGain` 3.0 and the breach intensity.
6. **Red's open choices stay at their defaults:** `GritVisualScale` 1.5 beyond 3 m, ceiling sift off.
7. **The throws are ballistic.** The prototype's Blender rigid-body bake failed its gates.

### The next stage (steel, P3) must know
- `FrontRoomsDoorBreakVisual.FamilyNames` already lists `Kit_DoorLeafBreak_Steel_DA`. `FamilyFor(leafKit)` picks the family whose sidecar `hangsOn` holds the intact kit, and a missing family is skipped (today's break).
- Give the steel base sidecar the same keys as the veneer one: `hangsOn` (Steel_DA, SteelLite_DA), `motifs[]` (`model` / `debris` names, `ys` / `zs`, `stageInfo` with `splitY` / `gapAtBore`), `throws[]` and `palettes`.
- Steel needs its own face mapping. Today:
  - `ApplySurfaces` maps the kit slot `Door_Veneer` to the intact face.
  - `ReadIntactSurfaces` finds the face by the prefixes `Door_Veneer` / `Prop_Wood`.
  - `PaletteArray` puts `veneer` / `laminate` in slot 2.
  
  For steel: add `Door_Enamel` there; add `Door_Enamel` to `FrontRoomsRenderSetup.DoorBreakFaces` with its own onset map (`DoorBreakDefs` hard-codes `Door_Damage_Veneer`, so make it per face); and put the `steel` palette in slot 2.
- Map the `_FR_DAMAGE` channels the same way: R onset, G the dent and bow (height), B crazing and primer, A fines.
- The VFX emitters are wood-specific: `Split` / `Notch` mixes and the trickles. Steel needs 54 §4.3's steel lines: enamel flakes, kraft bits, gypsum at FINAL, haze at 40 %. Add a material parameter to the VFX constructor.
- The stage clock handles any blow count. With 6 blows (Red's Q3, steel +0.5 s) it plays E E M D1 D2 FINAL; the extra time needs map ask M7.
- **Family-agnostic as they stand:** the merge, record, restore, driver, LODs, mirror, leak and shaft. Steel's debris FBX must be named `*_Deb<k>` to be readable.
- Bake with `door_break_bake.py --variants 4`. Its `launch()` is wood-tuned, so tune it for steel.

### Asks (handoff text only; no messages)
- **Map, unchanged from 50 §3.10:**
  - M1 `DoorBlowStruck` (the driver matches by position meanwhile);
  - M2 kept blows in gameplay (the visual side is done);
  - M3 pose (a) for the collider;
  - M4, M5, M6;
  - M7 (steel +0.5 s).
- **Sound:** S1-S5, and the optional S6 sift layer. The throw contact lists are ready for S3. Nothing makes a sound (rule 24).

## Stage: P3 steel family + P4 frame strike-zone inserts (2026-10-30, about 13:40-14:50)

Built in the clone `W/proj_doorbreak` on top of P1/P2. **Nothing landed in main.** Roslyn: mac and iOS defines, 0 errors. One smoke run of the film harness (`-dbMode smoke3`, run twice: the first found a shading pop, fixed below). No review stage, no full test round (rule 23).

**Start of stage:**
- Ledger `progress.py check visual` gave YOURS. 0 dataless files in main's synced folders.
- `rsync --checksum` from main brought in `FrontRooms3DGame.Baseline.cs` and `Tools/relay_baseline_run.sh`. **Incident:** the four main files P2 had changed in the clone (`FrontRoomsSurface.shader`, `FrontRoomsRenderSetup.cs`, `FrontRoomsKitImporter.cs`, `FrontRoomsInteractableKit.Door.cs`) were overwritten by main's copies, because a zsh variable holding the `--exclude` list did not word-split. They were rebuilt by replaying P2's four recorded edit scripts from the P1P2 stage transcript; line anchors match P2's files exactly. Main's bases are unchanged (`1885f5b3`, `72b764c3`, `a5409ef3`, `c6aabf60`, re-checked at the end of this stage). The next re-sync should pass the excludes as an array, or check these four md5s afterwards.
- `install_tools.py` (silencer, test bundle id). Clone files changed here were copied first to `W/_partial/door_break_p3p4_pre_20261030/`.

### What was built

**A different method from P1: cut the intact kit, don't loft a copy.** A steel leaf dents, bows and opens its seam but stays whole (50 §3.5), so its break is the INTACT kit cut and deformed. New shared module `interact_door_break_kitcut.py`:
- runs the intact D1.6 generator in the same Blender (kitlib, its own recipe), takes its finished LOD0/1/2 meshes as bmesh;
- cuts a window out with planes (bmesh bisect, no booleans). The body is the intact mesh minus the window: same vertices, UVs, `fr_wear`, kick plates, pivot plates and screws;
- refines the window with a plane grid (only the slots that bend) and deforms it per stage with analytic fields that are zero, with zero slope, on the window's boundary, so body + window close exactly;
- rebuilds every cut piece's normals with the kit's own recipe (smooth, sharp at 35°, face-area weighted normal). The first smoke showed why: a plane cut interpolates the kit's *encoded* custom normals, and the body below the window went 8 levels darker at the swap (142 → 135). After the fix the body is 141-142 before and after, and the window matches the intact leaf (197.5 vs 198.4).

**P3, the steel family (`interact_door_leaf_steel_da_break.py`):**
- Window Z 0.60-0.995 × Y 0.50-1.22. The top stays under SteelLite's lite kit (Y 1.229), so both leaves share the cassettes.
- Fields per stage (amplitudes ±10 % per motif; 3 motifs move the bow centre, the dish, the crush peak and the peel span):

  | Stage | Bow toward the burst side | Push-face dish (contact Y ~1.08, Z ~0.745) | Lock edge crush (latch gap) | Edge seam |
  |---|---|---|---|---|
  | M | 0 | 3 mm, R 100 mm | 0 | closed |
  | D1 | 8 mm | 6 mm | 1.5 mm (4.5 mm) | closed |
  | D2 | 15 mm | 10 mm | 6 mm (9 mm) | opens under the armor front (Y ~0.60-0.855): the burst skin's lip peels 20 mm; the parted seam walls show kraft honeycomb (`Door_CoreSteel`) |
  | TORN | 18 mm | 11 mm | 9 mm (12 mm) | 30 mm, plus a 14 mm peel above the armor front |

- The armor front is sliced with the slab, so it bends with the crushed edge.
- Leak cards `leak_D1`, `leak_D2`: an emissive strip in the latch gap, on the burst side of the brush fin, following the crush. No TORN card: it would glow on the thrown leaf.
- Debris per motif: 10 enamel chips + 6 gypsum crumb clusters from the frame anchors (16 pieces, 404 tris). The armor front's screws were dropped: the kit cuts them into the plate (`host=`), so they cannot come out cleanly.
- `Kit_DoorLeafBreak_SteelLite_DA`: base only (SteelLite minus the same window). Its sidecar points at the Steel family's motifs, debris and throws.
- Throws: `door_break_bake.py --family steel --variants 4` (new flag: steel launch speeds, no notch in the leaf). All 4 ballistic, 0 keys below the floor, 0 in the wall, settle 1.02-1.03 s.
- **Runtime (`FrontRoomsDoorBreakVisual`):**
  - `FamilyNames` + SteelLite; the sidecar's `"material": "steel"` gives `Steel`.
  - Face = `Door_Enamel` / `Painted_Metal` (read from the intact leaf); `Prop_Chrome` → the intact hardware; `Ward` is wood-only now (a steel leaf's stainless kick plates are its body's).
  - Palette slots for steel: kraft, glue, enamel (with the primer entry), the frame's own, steel, gypsum, settled, glue.
  - `ApplySteelParts`: the escutcheon, cylinder, plug, knob, number plates and sign ride the bow, and on the push face the dish too, using the same field numbers read from the sidecar (`bowY/bowZ/dishY/dishZ/crushPk`, `stageInfo` bow/dish/dishR/crush). The latch and deadbolt ride the crush and bend 22° / 14° at FINAL; they stay in the leaf (no strip).
  - Slot / gap shaft from the TORN stage info at FINAL.
- **P2 additions:** `Door_Enamel` joins `DoorBreakFaces`, giving 12 stage materials. Its own onset map `Door_Damage_Enamel_{O,N}` covers Y 0.50-1.22 (a per-SurfaceDef `damageRect`):
  - R onset; B crazing (lighter, matte): the star at M, rings at D1 around the dish, and the bow's tension crazing at the lock edge on the burst face;
  - A = chips down to the primer (the fines colour is primer, linear 0.270, 0.181, 0.117) at D1-TORN;
  - G hairline cavity.
  
  New surface `Door_CoreGypsum` (texture `Door_CoreGypsum_{A,N,S}`, the steel break's crumbs). `make_core_textures.py` and `make_dust_textures.py` use their own seeds for these, so every earlier texture is byte-identical (md5 checked). Two palettes were appended: `kraft`, `steelframe`.
- **The dust for steel (54 §4.3, `FrontRoomsDoorBreakVfx`, new `steel` / `woodFrame` constructor flags):**
  - M: 40 enamel flakes (flat 1-6 mm, flutter; 60 % off the push-face dish) + 200 paint/primer grit;
  - D1: 80 flakes + 300 grit;
  - D2: 40 flakes + 120 grit, and the seam: 60 kraft paper bits + 200 grit;
  - FINAL: 40 flakes + 100 grit, and the anchors: 60 gypsum crumbs + 800 grit (gypsum 60 %);
  - haze at about 40 % of wood;
  - no notch in the leaf collision; the head sift has no wood core; the pivot tear sheds gypsum only.

**P4, the frame strike-zone inserts (`interact_door_frame_da_break.py`):** zone = the latch jamb Y 0.70-1.30, everything past Z 0.985.
- `Kit_DoorFrameBreak_Wood_DA` (L0-F):
  - `ins_INTACT`;
  - `ins_SPLIT` at FINAL: the lining is ripped along X 0.03, just past the strike pocket. The burst-side strip and its casing (Y 0.79-1.21, 420 mm) are pried 10-16 mm toward the burst side and 7 mm into the opening, still held at their ends; the casing lifts off the wall;
  - `ins_SPLIT_teeth`: torn crack walls (`Door_CoreStile`) and 14 splinter teeth.
- `Kit_DoorFrameBreak_Steel_DA` (Steel_DA and the `_Alu_DA` variant by slot swap):
  - `ins_INTACT`;
  - `ins_D2` at D2: the soffit dents 3 mm round the strike, the burst face band flares 2 mm, the brush latch fin bends 35°;
  - `ins_TORN` at FINAL: 7 mm, 5 mm, 70° (the fin is the DA frame's "silencer").
- **Runtime:**
  - from the frame's first changed stage, the intact frame's LOD renderers are switched off, and body + insert show under "Break frame" with the intact's LOD heights and shadow mode;
  - only the insert mirrors with the burst (the body never flips its texture);
  - slot swaps from the sidecar (walnut → `Door_Frame`, chrome → satin, sleeve → aluminium on `_Alu_DA`);
  - restore and kept stage are covered (the frame follows `Stage`); `ResetVisual` puts the intact frame back;
  - the strike (the rig's part) turns 2.5° / 6° and shifts 1.2 / 3 mm toward the burst side at D1 / D2, and `DetachStrike` drops it from there at the break (checked: detached, it falls).
- **Head dust (P4):** new emitter `FrameJoint`. Every blow from M sheds gypsum and settled dust from the head casing's joint (70 % to the burst side) and, from D2, from the latch jamb's joint by the strike:
  - 40 / 70 / 110 / 180 grains at M / D1 / D2 / FINAL, × 0.6 on a steel sleeve;
  - 1 wisp from D2;
  - keyed by the blow index, so a kept stage that is struck again sheds new grains.

**Harness (clone-only):** mode `smoke3`. It picks a locked Steel_DA door, falling back to dressing the picked L0-F door as L0-K through the kit's tools hook `DoorLockedOverride` (dressing only: no locked Steel_DA door was within 60 m on seed 516574485). Then:
- the steel T1 with Red's Q3 ladder: 6 blows (E E M D1 D2 FINAL), break at 3.0 s;
- stills; the merge; a restore through DressDoor; the steel kept stage;
- one L0-F door's T1 for the wood jamb split.

### Files (clone)
- **New tools:** `Tools/Blender/frontrooms_kit/assets/interact_door_break_kitcut.py`, `interact_door_leaf_steel_da_break.py`, `interact_door_frame_da_break.py`.
- **Changed tools:** `Tools/Blender/frontrooms_kit/door_break_bake.py` (`--family steel`), `Tools/door_break/make_core_textures.py`, `make_dust_textures.py`, `palettes.json` (+ kraft, steelframe).
- **Runtime:** `FrontRoomsDoorBreakVisual.cs`, `FrontRoomsDoorBreakVfx.cs`. **Editor:** `FrontRoomsRenderSetup.cs` (main file; base `72b764c3` unchanged in main).
- **Clone-only harness:** `Assets/Editor/DoorBreak/FrontRoomsDoorBreakFilm.cs` (`smoke3`, `Setup()`, `MainTakes()`, `Pick(fam)`).
- **Models** (`Assets/Resources/Props/Models/`): `Kit_DoorLeafBreak_Steel_DA{,_M0..2,_Deb0..2}` (.fbx/.json), `_throw0..3.json`, `Kit_DoorLeafBreak_SteelLite_DA`, `Kit_DoorFrameBreak_Wood_DA`, `Kit_DoorFrameBreak_Steel_DA`. Disk: steel 3.9 MB + throws 0.25 MB, frames 0.83 MB.
- **Textures:** `Door_CoreGypsum_{A,N,S}`, `Door_Damage_Enamel_{O,N}` (0.82 MB).
- **Materials** (written by `EnsureDoorBreakSurfaces`): `Door_Enamel_Damage_{M,D1,D2,TORN}.mat`, `Door_CoreGypsum.mat`. Every existing `.mat` is byte-identical to main (cmp: 0 differ). LF only.
- **Asset run order:**
  1. `make_core_textures.py`
  2. `make_dust_textures.py`
  3. Blender `interact_door_leaf_steel_da_break.py -- --out-root <clone>`
  4. Blender `door_break_bake.py -- --out-root <clone> --variants 4 --family steel`
  5. Blender `interact_door_frame_da_break.py -- --out-root <clone>`
- **W:** `W/door_break/P3P4/run_smoke.sh`, `sheet.py`, `logs/`, `out/smoke3/` (frames, `stats.json`, `film_log.txt`), `out/smoke3_sheet.jpg`, `out/smoke3_crops.jpg`.

### Budgets (smoke, 1080p, desktop, Editor; steel motif 0)
- **Generator, steel (LOD0/1/2):**
  - intact Steel_DA 5,524 / 1,892 / 292;
  - body 5,262 / 1,870 / 286;
  - each cassette 3,706 / 812 / 6 (M, D1: the plain window at LOD2) or 120 (D2, TORN);
  - debris 404.
  - The design's 2.9k body estimate was the veneer's; the steel intact is heavier.
- **Drawn, steel break parts (tris / draws):**

  | Stage | LOD0 | LOD1 | LOD2 |
  |---|---|---|---|
  | M | 8,968 / 5 (1.62× intact) | 2,682 / 4 | |
  | D1 | 9,180 / 6 | 2,894 / 5 | |
  | D2 | 9,276 / 7 | 2,990 / 6 | |
  | FINAL (debris in flight) | 9,372 / 32 | 3,086 / 31 | |
  | BROKEN, merged | 12,968 / 9 | 3,086 / 8 | 810 / 6 |

  Merged floor: 16 pieces + 360 flakes, bits and crumbs → 4,000 tris, 3 draws.
- **Frame parts (body + insert, tris / draws):**

  | Frame | Intact (LOD0/1/2) | LOD0 | LOD1 | LOD2 |
  |---|---|---|---|---|
  | Steel D2/TORN | 2,634 / 1,032 / 504 | 4,390 / 5 | 1,408 / 5 | 632 / 4 |
  | Wood SPLIT | 2,186 / 1,388 / 460 | 3,588 / 7 | 1,806 / 7 | 544 / 5 |

- **Reads at 3 m (C1 lock box, changed px):**

  | Change | Steel | Wood (with the frame split) |
  |---|---|---|
  | E → M | 387 | |
  | M → D1 | 888 | |
  | D1 → D2 | 1,285 | |
  | D2 → FINAL | 2,409 | 7,002 |
  | FINAL → BROKEN | 27,221 | |

  - Every steel stage swaps on its blow frame; hit-stop 2 / 2 / 3 frames.
  - The frame outside the zone is unchanged at its swap (luma 62.2 → 62.3 steel, 47.2 → 48.8 wood).
- **Dust, steel, per beat E / M / D1 / D2 / FINAL / BREAK** (E is two blows):

  | | E | M | D1 | D2 | FINAL | BREAK | Total |
  |---|---|---|---|---|---|---|---|
  | Grit | 416 | 318 | 398 | 420 | 1,028 | 800 | 3,380 |
  | Solids | 0 | 40 | 80 | 100 | 100 | 40 | 360 |
  | Haze | 5 | 4 | 4 | 6 | 9 | 8 | 36 |

  - Peaks: grit 3,086, solids 360, haze 28. Dropped 0. Haze alive at BREAK + 3.2 s: 0.
  - Wood T1 with the frame joints added: grit 260 / 496 / 864 / 1,066 / 2,514 / 800 (6,000; +40 / 70 / 110 / 180 over P1).
- **CPU (Editor Mono):**
  - VFX peak 3.1 ms steel, 4.3 ms wood: still no Burst port;
  - merge worst slice 2.8 ms (the first, JIT) over 7 frames;
  - steel restore 1.4 ms; restore vs settled: 116 of 28,956 px;
  - steel kept stage: E E M D1 D2 → give up → rebuild → D2, with the frame's D2 insert.

### Not done, or a stand-in
1. **No real L0-K door filmed.** Seed 516574485 has no locked Steel_DA door within 60 m of the start, so the smoke dressed an L0-F door as L0-K (dressing only). The LAND stage should film a real L0-K door, plus P1's Ward door. SteelLite (no row uses it) and the Painted_Metal variant (no stage materials: its cassettes show the plain face) are generator-checked only. The `_Alu_DA` frame swap is not filmed.
2. **The steel read at 3 m is quiet by design** ("the bow in the reflections", 50 §3.5): G4 passes (888 / 1,285 / 2,409 px), but the seam peel is on the latch end and edge-on from C1. If Red wants more from the front, the knobs are `AMP` at the top of the steel generator (peel, crush, bow) and `STEEL_AMP` / `wood_split_field` in the frame generator.
3. Primer chips (A channel) read as darker brown flecks at close range: the push-face dish's, and on the burst face around the lock edge. Calibrate with Red.
4. No Burst/job port of the grit step (P5 or a player measurement), and the phone paths for steel and frames are unexercised.
5. The screw debris was dropped (see above).

### The next stage (P5 phone tier) must know
- **Steel on phones:**
  - leaf: the LOD1 cassette (812) + body LOD1 (1,870) ≈ 2.7k tris, ≤ 6 draws;
  - frame: the insert's L1 (steel 432, wood 470 + teeth 32) + frame body L1;
  - both go through `LodAllowed` / `Phone` exactly like P1's cassettes: frame `_L1` parts join LOD0 on phones.
- **VFX phone counts are coded** (`N(desktop, phone)` prefixes):
  - steel flakes 13 / 27 / 13 / 13, grit 50 / 75 / 30 / 25, seam 20 bits + 50 grit, anchors 20 crumbs + 200 grit, haze 1-3;
  - FrameJoint 25 %.
  - Not exercised.
- **Leak cards:** steel's are `leak_D1` / `leak_D2` with no LOD suffix, so they are near-only (LOD0/1 lists) like the veneer's. The breach light is desktop-only, as before.
- **Draws while live** are ≤ 6 (grit, haze, 3 solid meshes, skin) for both families; the frame adds its body + insert (5-7 draws) from its swap stage.
- WebGL still does not dress doors.

### Asks (handoff text only; no messages)
- **Map, M7 (now concrete):** a LOCKED Steel_DA door's `TickBreak` uses 6 / 5 / 5 / 4 / 4 blows (T +0.5 s; T1 = 0.5, 1.0, 1.5, 2.0, 2.5, 2.9, break 3.0), so a dent always shows before the break. Without M7, steel plays the wood ladder (E M D1 D2 FINAL) and works. M1-M6 are unchanged.
- **Sound, S1:** `DoorType` = 1 for the steel family (`FrontRoomsDoorBreakVisual.Steel`). The steel throw contact lists (chips, gypsum) are in `Kit_DoorLeafBreak_Steel_DA_throw*.json` for S3. Frame layers for S4: the wood jamb split at FINAL, and the steel strike dent and fin crush at D2 / FINAL. Nothing makes a sound (rule 24).

## Stage: P5 phone tier + the desktop byte-identical check (2026-10-30, about 14:25-15:05)

Built in the clone `W/proj_doorbreak` on top of P1-P4. **Nothing landed in main.** Roslyn: mac and iOS defines, 0 errors (`W/door_break/P5/roslyn_final_mac`, `roslyn_final_ios`). Smoke runs only (`-dbMode smoke3` / `smoke`, desktop and phone); no review stage, no full test round (rule 23). Nothing made a sound (clone silencer re-installed after the re-sync; batch Unity only).

**Start of stage:**
- Ledger `progress.py check visual` gave YOURS. 0 dataless files in main's synced folders. Main HEAD is now `a75af64b`.
- Re-sync with the excludes passed **as a zsh array** this time (the P3/P4 incident). It brought 42 items (Relay V3b, `FrontRoomsMapWorld/MapHunter(.V2)`, `FrontRoomsRelayDirector*`, the Inspect terminal and `Rendering/Screens/*`, `Resources/Terminal/*`, two `.pyc`). The four P2-changed main files were kept: their clone md5s were identical before and after (`2025ae53`, `c33ba469`, `242684fa`, `4b4fd98b`). The item list is in `W/_partial/door_break_p5_pre_20261030/resync_items.txt`. Then `install_tools.py`.
- Pre-P5 copies of every file this stage changed: `W/_partial/door_break_p5_pre_20261030/`.
- Main kept moving during the stage (`FrontRooms3DGame.Baseline.cs`, Inspect/Terminal files; nothing door-related). The four base md5s were re-checked at the end and are unchanged: `1885f5b3`, `72b764c3`, `a5409ef3`, `c6aabf60`.

### What was built

**The phone gate (`FrontRoomsDoorBreakVisual`).** `Phone = PhoneTier`, where `PhoneTier = DebugForcePhoneTier || (PhoneTierOnHandheld && FrontRoomsHandheld.Active)`. `PhoneTierOnHandheld` is true by default, because handheld doors are dressed (`DressOnHandheld`). `DebugForcePhoneTier` is a lab switch, never set by the game, and is reset at `SubsystemRegistration`. Every P5 runtime change sits behind `Phone` or behind the VFX tier, except the haze cull below, which leaves the image unchanged.

**LOD1-only cassettes and frame inserts.** P1's `LodAllowed` path is unchanged: the `_L0` parts never show on a phone, and the `_L1` parts fill both LOD0 and LOD1. The tools counters `CountDrawn` / `CountFrame` now report what LOD0 actually shows on a phone (the `_L1` set).

**Combined debris (50 §3.9: phones ≤ 12 live, one combined dynamic mesh):**
- At FINAL, the phone keeps the strip plus the most visible pieces, ranked by the product of their two largest rest extents (from the sidecar's `debrisRest`). Ties break by name. The cap is `PhoneDebrisCap` = 12.
  - Veneer keeps the strip and the 11 largest splinters; the 16 crumb clusters are dropped (the VFX solids still bring crumbs).
  - Steel keeps the 6 gypsum clusters and the 6 largest enamel chips.
- The pieces that are not kept are switched off, so the merge, the env check and the counters skip them.
- The kept pieces keep their transforms (the latch still rides the strip; the merge and the env check read them) but their renderers are off. One mesh, "Break debris (phone, combined)", under the debris root draws them, one submesh per material.
- Its vertices, normals and tangents are re-posed on the CPU from the baked keys every frame until the keys end (2.0 s). It is dropped when the floor merge finishes.
- Restore and the env check update it too. `CheckNeighbourhood` uses mesh-derived world bounds on phones, because the piece renderers are off there.
- Tools: `DebrisShown`, `DebrisCombinedDraws`, `CombinedWorstMs`.

**VFX caps.** P1's phone counts and caps (grit 1,536, solids 400, haze 24, `N(desk, phone)` prefixes) were exercised for the first time; no count changed. The new part is the **haze fill cap**:
- `FrontRooms/DoorHaze` now collapses, in the vertex stage, any wisp whose four corners all lie nearer than the camera fade's zero. Every fragment of such a wisp used to be discarded, so the image does not change. It is 0.25 m on desktop.
- Phones fade from `FrontRoomsDoorBreakVfx.PhoneHazeFadeStart` = **0.80 m** (full at 1.15 m) through `_FR_HazeParams.w`. Desktop keeps its constant-0.25 expression in its own branch.
- Why: the first phone smoke rasterised 3.3-4.0 screens of haze quads at C2 (1 m, in the leaf's sweep, where the gust pushes the wisps toward the lens), against the phone budget of ≤ 0.35. The probe tried fade starts of 0.25 / 0.45 / 0.60 / 0.80 m, which gave 3.34 / 0.72 / 0.49 / **0.28** screens at C2.
- This is a phone-only look change: haze within 0.8 m of a phone camera is gone. The knob is `PhoneHazeFadeStart`.

**The wisp atlas on phones (54 §4.8).** `FrontRoomsRenderSetup.Configure` (a main file) adds iOS and Android tabs for `Door_DustWisp*`: 512 px, ASTC 6×6 (about 0.08 MB with mips, an ESTIMATE from the format). The desktop and WebGL tabs are unchanged. The harness re-imported the texture once, so `Door_DustWisp_D.png.meta` now carries the two tabs.

**No breach light on phones.** `EnsureBreach` returns on `Phone`; that code is pre-existing and unchanged.

**Harness (clone-only, `FrontRoomsDoorBreakFilm.cs`, ASCII, no tuples):**
- `-dbPhone 1` runs any mode on the phone tier. It sets the switch by reflection, so the harness also compiles against pre-P5 code. Output goes to `<mode>_phone`.
- Phone renders use an RT without MSAA, as the handheld AA-OFF presets do, so the grit takes its coverage-keep path.
- Every render writes the raw-pixel md5 and a **state digest** to `frames.tsv`. The digest has two parts:
  - geo: the door root's enabled renderers, their meshes, materials, door-local matrices and runtime vertices, plus the VFX's GPU grain and wisp data, the shaft geometry and the skin;
  - lit: runtime colours, light levels and the lamp colour.
- **Frame lock.** `Time.captureDeltaTime` is fixed from the first frame, and every wall-clock-dependent segment (streaming, the sliced merge, a restore) ends on a sync frame (the next multiple of 300, at least 60 frames on).
  - Without it, two runs of the same code differed in every frame (mean 1.7 levels, max 13-17: time-seeded post grain and lamp temperaments).
  - With it, two pre-P5 runs of smoke3 were identical in 27 of 27 frames (pixels, JPG bytes, state).
  - The stills now render on later frames than P3/P4's, so their frame numbers do not match P3/P4's.
- A phone haze-fill probe, phone runs only: each wisp quad clipped to the screen, measured raw, after the cull, and weighted by the fade, for C1-C4.
- **W:**
  - `W/door_break/P5/run_smoke.sh <mode> <label> [phone]`, `cmp.py <dirA> <dirB>`, `sheet.py`;
  - `logs/`;
  - `out/base_smoke3_a|b`, `out/base_smoke_a` (pre-P5), `out/p5_smoke3`, `p5_smoke` (desktop), `p5_smoke3_phone`, `p5_smoke_phone`;
  - superseded runs in `out/_noisy_v1`, `out/_p5_v1..v3`;
  - the contact sheet `out/p5_desk_vs_phone.jpg`.

### The desktop byte-identical proof

| Comparison (rows matched by take, still and stage) | Frames | Pixel md5 | JPG bytes | State geo / lit |
|---|---|---|---|---|
| pre-P5 smoke3 run A vs run B (determinism) | 27 | 27 | 27 | 27 / 27 |
| pre-P5 smoke3 A vs **P5 final** smoke3 | 27 | **27** | **27** | **27 / 27** |
| pre-P5 smoke A vs **P5 final** smoke | 20 | **20** | **20** | **20 / 20** |

- The final P5 code includes everything above: the phone gate, the combined debris, the haze cull and phone fade, and the re-imported wisp `.meta`.
- The two modes cover the steel T1 (6 blows), the steel merge, restore and kept stage, the wood T1 with the frame split, the veneer T1, the merge, restore, kept stage and mirrored burst.
- The desktop VFX counts are unchanged: wood grit 6,000 / solids 734 / haze 53; steel 3,380 / 360 / 36; dropped 0.
- One run-noise event: an intermediate P5 run (before the haze cull) differed from the baseline in a single frame per mode. The difference was one tile (16 px tall, about 120 px, ≤ 4 levels), with identical state digests. A re-run of the same code matched the baseline 20 of 20, so this was GPU run noise, not P5.
- Scope: Editor, Metal, the OSXUniversal target, quality High. Windows was not run. The `#if` defines are unchanged; the phone gate is false wherever `FrontRoomsHandheld.Active` compiles to false (Mac/Win players).

### Handheld budgets (measured, phone tier forced in the Editor; 1080p RT, no MSAA; motif 0)

**Break parts (tris / draws), at the near LOD:**

| Stage | Veneer, phone | Veneer, desktop LOD0 | Steel, phone | Steel, desktop LOD0 |
|---|---|---|---|---|
| M | 1,518 / 7 | 2,622 / 7 | 2,682 / 4 | 8,968 / 5 |
| D1 | 1,778 / 7 | 2,974 / 7 | 2,894 / 5 | 9,180 / 6 |
| D2 | 2,538 / 8 | 4,478 / 8 | 2,990 / 6 | 9,276 / 7 |
| FINAL (debris in flight) | 2,262 / 10 | 4,442 / 40 | 3,014 / 8 | 9,372 / 32 |
| BROKEN, merged (near) | 4,594 / 10 | 11,410 / 11 | 4,214 / 8 | 12,968 / 9 |

- **Debris in flight:**
  - veneer: 12 pieces, 616 tris, 4 draws (desktop 31 pieces, 1,180 tris);
  - steel: 12 pieces, 332 tris, 3 draws (desktop 16 pieces, 404 tris).
  - The combined update costs 0.06-0.09 ms worst (Editor Mono).
- **Merged floor:**
  - veneer: 12 pieces + 244 crumbs and chips → 2,948 tris, 4 draws (desktop 8,148 / 5);
  - steel: 12 pieces + 119 → 1,532 tris, 3 draws (desktop 4,000 / 3).
  - Worst slice 1.0-1.1 ms (the first, JIT), over 5-6 frames.
- **Frame, body + insert:** steel D2/TORN 1,408 / 5 (desktop 4,390 / 5); wood SPLIT 1,806 / 7 (desktop 3,588 / 7).
- **Against 50 §3.9's phone line:**
  - Leaf ≤ 2.3k: veneer passes (1,518-2,538). Steel is 2,682-3,014, because its intact LOD1 is already 1,892 (P3's note).
  - "+2 draws": the leak cards and the Ward/steel parts make 4-8, as on desktop's LOD1.
  - Live debris ≤ 12, combined: passes.

**Dust, phone T1, per beat (E, M, D1, D2, FINAL, BREAK):**

| | Grit | Total grit | Solids | Haze | Peaks (grit / solids / haze) | Caps | Dropped |
|---|---|---|---|---|---|---|---|
| Wood | 65 / 124 / 217 / 267 / 628 / 200 | 1,501 (25.0 % of desktop) | 244 (33 %) | 24 (45 %) | 1,230 / 244 / 22 | 1,536 / 400 / 24 | 0 |
| Steel | 104 / 80 / 99 / 104 / 257 / 200 | 844 (25 %) | 119 (33 %) | 17 | 769 / 119 / 13 | 1,536 / 400 / 24 | 0 |

- VFX draws while live: ≤ 6. Haze alive at BREAK + 3 s: 0.

**Haze fill, phone peaks** (screens, quads clipped to the screen, after the cull; budget ≤ 0.35):

| View | Fill |
|---|---|
| C1 (3 m) | 0.08 |
| C2 (1 m) | 0.27-0.28 |
| C3 | 0.02 |
| C4 | 0.24-0.26 |

Before P5, C2 would have rasterised 3.3-4.0 screens.

**Other checks on the phone tier:**
- Every stage swaps on its blow frame; hit-stop 2 / 2 / 3.
- Restore vs settled: 137 (steel) and 152 (veneer) of 28,956 px.
- Kept stage: E E M D1 D2 → D2 (steel); E M D1 → D1 → D1 D1 D1 D2 FINAL (veneer).
- Mirrored burst: OK. Pieces below the floor: 0.

**CPU and memory:**
- Phone VFX main thread, Editor Mono: peak 1.34 ms (1,190 grit + 244 solids + 20 haze; desktop's same run 4.25 ms), and 0.15-0.40 ms at BREAK + 3 s.
- **The device budget (≤ 0.12 ms main) is not verified:** no device run, and the Burst/job port is still open.
- Memory: grit buffer 48 KB, haze buffer 1.2 KB, wisp atlas about 0.08 MB on iOS/Android. The combined debris mesh is about 600 vertices (ESTIMATE).
- **The LOD0 meshes are still loaded on phones**, because they live in the same FBX as LOD1. 50 §3.9's "LOD1 set ≤ 0.4 MB" needs a phone-only asset split; not done.

### Files (clone; the packaging stage diffs them)
- **Runtime:** `Assets/Scripts/Rendering/Doors/FrontRoomsDoorBreakVisual.cs` (the phone gate, combined debris, tier-aware counters), `FrontRoomsDoorBreakVfx.cs` (`PhoneHazeFadeStart`, `_FR_HazeParams.w`).
- **Shader:** `Assets/Resources/Rendering/FrontRoomsDoorHaze.shader` (the near cull, the phone fade start).
- **Main file:** `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs` (+ the wisp phone tabs). Base `72b764c3` is unchanged in main; the clone md5 is now `cf658d5c`.
- **Meta:** `Assets/Resources/Surfaces/Textures/Door_DustWisp_D.png.meta` (iOS/Android 512 ASTC 6×6).
- **Clone-only:** `Assets/Editor/DoorBreak/FrontRoomsDoorBreakFilm.cs`. LF only; no new non-ASCII lines.
- **No change to:** the map, sound, the other main files, models or textures.

### The next stage (LAND) must know
1. Re-sync with the excludes as an array (`EX=(--exclude=/Office/FrontRoomsInteractableKit.Door.cs --exclude=/Rendering/FrontRoomsKitImporter.cs --exclude=/Rendering/FrontRoomsRenderSetup.cs --exclude=/Rendering/FrontRoomsSurface.shader)`, `"${EX[@]}"`), then check the clone md5s `2025ae53` / `cf658d5c` / `242684fa` / `4b4fd98b`.
   - Main has moved again since this stage's sync (Inspect/Terminal and `FrontRooms3DGame.Baseline.cs`).
   - Re-check the bases `1885f5b3` / `72b764c3` / `a5409ef3` / `c6aabf60` right before packaging.
2. **The desktop identity tool is ready:**
   - run `run_smoke.sh smoke3 <label>` and `smoke <label>` before and after any change;
   - compare with `cmp.py`;
   - the pre-P5 baselines are `P5/out/base_smoke3_a` and `base_smoke_a`.
   - The harness's frame lock makes repeat runs byte-identical, so any later desktop change shows up.
3. **Desktop haze fill at C2 is probably over 54 §4.8's "≤ 1.0 screen worst".** On phones, with about 45 % of the wisps, C2 rasterised 3.3-4.0 screens behind the 0.25 m cull. Desktop was not measured, because the identity check kept the desktop path untouched. Measure it in the LAND round (`HazeFillDetail` is phone-only now; drop that gate for a desktop measuring run). A desktop fix is a look change for Red: for example a later desktop fade start.
4. **The phone fade start (0.80 m) is a phone look choice** (`PhoneHazeFadeStart`). Show it in the next video if Red wants to see phones.
5. **Not covered:**
   - no device run (the CPU and GPU budgets, ASTC, the handheld quality presets; the smoke used quality High with a no-MSAA RT);
   - no Burst port;
   - Android GLES gets no grit or haze (the shaders exclude GLES) while the sim still runs; Vulkan and Metal are fine;
   - the phone L0 meshes stay loaded;
   - the Ward and real L0-K doors are still unfilmed (P1/P3 notes).
6. The Figma verification log entries are batched for LAND (rule 23); the sheet is `W/door_break/P5/out/p5_desk_vs_phone.jpg`.
7. **Asks:** none new. The map (M1-M7) and sound (S1-S6) asks are unchanged. The combined debris keeps the piece transforms, so the throw contact lists for S3 are unchanged.

## P6: hand-offs, one test round, package, video (2026-10-30, account B)

The stage finished its work at 15:52. Its agent died on a network error (ENOTFOUND) before it could report, so the session read the results from disk and landed `W/apply/door_break.sh` at 17:3x.

- **Package:** 181 paths, 177 new + 4 replaced (FrontRoomsSurface.shader, FrontRoomsRenderSetup.cs, FrontRoomsKitImporter.cs, FrontRoomsInteractableKit.Door.cs). MANIFEST OK; Roslyn macOS + iOS OK on main after the apply.
- **Contract:** `W/apply/door_break_contract/` (README + CONTRACT_map.diff, M1-M7, 25 hunks over 6 map files, `regen_contract.sh --check` OK on main). Main + payload + contract also compile on macOS and iOS, and with `FRONTROOMS_RELAY_V2` on. Sound asks: `53_for_sound.md`.
- **Tests (clone):** MapInteractionTests 412/0, with the contract 464/0. DoorShot 252/0 both. Station + exit run PASS with the contract. Live Relay, kept blows and a locked steel door: see the README table.
- **Budgets still open:** the break frame's dust costs 4.7 ms (veneer) and 2.8 ms (steel) on the main thread, measured in the Editor, against a 0.2 ms budget. The grain simulation is plain C# with no Burst / jobs port. It is one frame per broken door, so it did not hold the landing.
- **Video:** `W/door_break/video/door_break_production_20261030.mp4` (copy in `video/`), 9.9 MB, no audio.
