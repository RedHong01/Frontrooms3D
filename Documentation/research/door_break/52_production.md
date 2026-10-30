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
