# Hunter B "Night Shift": build report (H1–H3, the swap, the gates of this stage)

Status: BUILT and VERIFIED in the clone, packaged as the v2 apply package `hunter_b` (**DRY RUN OK** on main, not applied). 2026-10-22 13:09–14:25, 游戏视觉 workflow `hunter-b-night-shift`, build stage, re-run on account B after account switch #2. Plan: `10_plan.md`.

Every run in this stage was **UNDER LOAD** (1-minute load 28–150; only the 14:05 test run started under 32). Timings below compare only relatively.

**Still OPEN for Red (nothing here decides them; each stays one number):** the long craned neck or the face about 0.3 m higher (`faceLift` on the rig, default 0); the catch distance 0.7 or about 1.3 m (map-owned); whether every door is a squeeze (the body always squeezes; only its time changes). **Not yet tested:** T1, the doorway-at-3 m cold read that decides B or the fallback A (§4).

---

## 0. Where the earlier run stopped (inventory at 13:09)

The build stage first started at 08:17 on account A and died about 08:50 when that account hit its session limit. Found and checked:

| What | State at 13:09 | Used? |
|---|---|---|
| `production_b/10_plan.md` | complete (513 lines), 08:14 | yes, the spec |
| Clone `W/proj_hunterb` | made 08:16 from main `0ea38bb7`, compiled once (`s1_compile`, 0 errors), HunterLookdev baseline of the primitive Relay (`s1_lookdev_baseline`) | kept; re-synced to main at 13:15 (below) |
| Blender generator `Tools/Blender/frontrooms_kit/creatures/relay_night_shift.py` + `creature_rig.py` (in the clone) | written and running: body, mirrored 36-bone armature, weights, 10 pose keys, bakes, LODs, FBX + poses JSON. Last runs b10/b11 08:47–08:50 were `--quick` (2048 bakes) | kept and continued |
| Its outputs in the clone (`Resources/Creatures/Relay_NightShift.*`, 10 `Creature_*` textures) | written 08:50, never imported by Unity (no `.meta`) | rebuilt |
| T5 envelope (`hunterb/blender/t5_envelope.json`) | door_mid **FAIL**: an arm at \|x\| 0.50 in the door slab, left wrist 9 cm out of reach | fixed (§1.1) |
| Unity side: materials, rig swap, tests, runs | **not started** | built here |

So the stop was between "Blender module working" and "anything in Unity". The 08:5x partial outputs are kept in `W/_partial/hunterb_blender_0850/`.

**Re-sync.** Main had moved (HEAD `2c450e11` plus peers' uncommitted touch/mobile files). The clone's five folders were re-synced from main's working tree at 13:15 with the new creature files protected; everything rsync replaced or removed went to `W/_partial/hunterb_resync_20261022-131557/` (63 files). After the re-sync the clone's Assets equal main's except the shared tools and this package.

---

## 1. What was built

### 1.1 The body (Blender, `relay_night_shift.py`, full build 83 s)

- **Mesh:** LOD0 **11,110** tris (budget ≤ 12 k), LOD1 **5,500**, LOD2 **2,200**; 3 materials (twill, lens, trim) on every LOD, the lens protected in every decimation.
- **Armature:** 36 bones (root + 35 deform), left side built and symmetrised (mirror error 0.0000 m); weights ≤ 4 influences, mean 1.88; neck telescope (slide + stretch ≤ 1.25 ×).
- **Keys:** std, listen_std, low, tall, door_in, door_mid, door_out, plus std / listen_std / low at face +0.3.
- **Door keys re-authored.** The module's right glove gripped the *far* casing at x 0.84: legal at the key's own s, but it cut the jamb while the body was still behind the door. All arms now stay inside the opening (\|x\| ≤ 0.45), with elbows tucked down and knees along the crossing. A new **T5 sweep** checks every door key over the whole s range the runtime uses it for. Results (search runs e1–e6, e5 kept): door_in clean, door_mid ≤ 0.025 m, door_out ≤ 0.067 m at the start of its range. Pure keys, so this check is conservative; the Unity walk with blends is clean (§2.3).
- **Envelope (Blender, before the pin):** std top 2.927, lens 1.78 / 2.08 (face +0.3); low 2.469, lens 1.42 / 1.72; tall 3.260, lens 2.92; door_in top 2.080, slab \|x\| 0.364; door_mid 2.147 / 0.353; door_out 2.334 / 0.479.
- **Textures:**
  - twill 4096² A/N/S: baked normal (seams, pleats, creases, placket, flaps) + 2/1 weave; cloth p99 sRGB 0.227 (≤ #3B);
  - lens 512 × 1024 A/N/S/E: the troffer strip u 0.17–0.50; band/gap **4.16 : 1** (was 2.84; deeper gaps, §3); stronger face-shadow tell;
  - trim 1024² with the stock number **114** (provisional chain stitch; the lettering is 平面视觉's).

### 1.2 Runtime (new, visual-owned, ASCII, no tuples)

- **`Assets/Scripts/Rendering/Relay/FrontRoomsRelayPoseSet.cs`.** Parses the poses JSON once. It **measures the Blender → model axis map from the 36 bind heads** (a signed permutation × uniform scale; measured: x → −x, z → y, y → −z, scale 1.000, residual 0) and converts every key. So the FBX importer's axis conversion and bone rolls never matter.
- **`FrontRoomsRelayBody.cs`.** Per frame:
  - **room blend:** low / std / tall from CeilingHeight smoothed (τ 0.25 s down, 1.0 s up); the Listen key (glove on a tile) in std while listening; faceLift between the long and face +0.3 keys;
  - **door / header phase:** DoorSqueeze, plus a **header probe** (§3);
  - **gait arm swing**, the BreakDoor hammer, Stagger;
  - **latched Listen/Search head** (15° steps, holds 0.4–0.9 s, 0.08 s snaps, one lens starter pulse per snap); the Run head-lock (±70°, direction only);
  - **the catch:** camera clearance (lens ≥ 0.30 m, every vertex ≥ 0.15 m), the hood looking at you, the reach;
  - **constraints:** the ceiling pin (pelvis −0.12 … +0.02 m, top at h − 0.02), world-locked stance feet and analytic two-bone leg IK;
  - **lens property block + spot self-light;** a flat standby emission map (no tube bands) at Listen;
  - **contacts:** Step at each plant (the boot's ball), CeilingBrush at mid-swing while pinned (≥ 0.5 s apart), Squeeze from overlap spheres (fallback: the old threshold rule);
  - **WebGL** (`#if UNITY_WEBGL && !UNITY_EDITOR`): LOD0 off (LOD1 cap), no Walk light;
  - **cost:** 0 bytes allocated per tick.
- **`FrontRoomsRelayLens.cs`.** The state-only envelope (Listen 0.06 standby, Search pulses, Walk 0.25, the Run strike 0.9 / 0.4 / 1.0, BreakDoor shudder −8 % and the final-blow dip, Stagger). The lens and the light each pass through their own photosafety limiter: ≤ 3 transitions per sliding 1 s; Reduce Flashing turns every swing into a ramp of at least 0.52 s that is never cut short.
- **`FrontRoomsRelayGlassMirror.cs`.** Ray-traced glass registers MeshRenderers only. In Play on a Mac where G14 runs, a hidden stand-in (renderingLayerMask 0, no shadows, not under the rig) carries a ≤ 15 Hz bake of LOD1 into the TLAS through the **public** `FrontRoomsGlassRT.Register / RefitMesh` API (Dynamic | Relay | Deforming). **No GlassRT file changes** (§3).
- **`Assets/Scripts/FrontRoomsRelayRig.cs`** (base 64b9de9c → f01cafc3). The public API is unchanged (reflection test A1). Two new serialised fields: `useProductionBody` (true) and `faceLift` (0). `EnsureRig` instantiates `Resources/Creatures/Relay_NightShift` as the child `RELAY BODY / Night Shift` (`DontSave` in edit mode; a stale preview is replaced). The 16 primitives are only hidden (`forceRenderingOff`, not serialised), so the scene file never changes. `TickAnimation` forwards to the body, and `DoorBlow` forwards too. `ApplyMaterials` now touches only the primitives. A missing asset gives exactly the old rig.

### 1.3 Materials, shader, import (visual-owned)

- **`FrontRoomsRenderSetup.cs`** (256cf3a4 → 3ffc96ea): **only** the three `Creature_*` SurfaceDef lines and their comment, in their own hunk:
  - mesh UV, tile 1, occlusion 1;
  - every world-projected term 0 (macro tone and dirt, stains, damp, floor and ceiling grime);
  - the lens's emission map black at rest.
- **`FrontRoomsSurface.shader`** (a82fe187 → 8af5163e): the **`_FR_SHEEN`** local keyword (`shader_feature_local_fragment`; `_Sheen` in the CBUFFER for SRP-batcher layout). With it on:
  - metal comes per texel (mask B × _Metallic);
  - a fabric sheen is added at grazing view angles, lit by the scene's own lights (a white, rough copy through URP's own loop × mask A).

  Only the creature twill (sheen 0.35) and trim (metal from mask B) enable it, so every other material is unchanged.
- **`Assets/Editor/Rendering/FrontRoomsCreatureImport.cs`** (new; runs after the kit importer):
  - **FBX:** Generic without an avatar, the Animator stripped by name (the project has the Animation module off; the kit's "None" dropped the skin to MeshRenderers), read/write on for BakeMesh;
  - **textures:** clamped atlases; the **WebGL tab only** capped (twill 2048, trim 512, lens 1024);
  - **materials:** `_FR_SHEEN` re-applied idempotently whenever RenderSetup rewrites the creature materials.
- **Materials** `Creature_TwillSpruce / LensOpal / TapeSilver.mat` (FrontRooms/Surface), generated by RenderSetup in the clone; the FBX remaps its slots to them (KitImporter, from the sidecar's slot list).

### 1.4 Tests and tools

- **`Assets/Editor/Rendering/FrontRoomsRelayRigTests.cs`** (shipped): 15 checks, A1–G2 (§2.2). Writes `Verification/hunter_b/tests/{tests.json, log.txt, lens_series.csv}`.
- **Clone-only, never shipped:**
  - `Assets/Editor/Rendering/FrontRoomsRelayBodyBuild.cs`: Setup, Lookdev, DoorWalk, TallWalk, DebugArm;
  - `Assets/Scripts/HunterBClone/FrontRoomsRelayPlayLog.cs`: the `-hbPlayLog` Play logger.

---

## 2. Results

### 2.1 Compile

- **Unity (OSXUniversal):** 0 errors in every run since 13:43.
- **One error caught only by Unity:** `Animator` (Animation module off). Roslyn passed it, because its references are wider than the project's.
- **Roslyn on main + payload:** `--all` COMPILE OK, and `--all --ios` COMPILE OK (Assembly-CSharp 106 files, Editor 64).
- **WebGL branches:** compiled with the guards forced on: 0 errors.

### 2.2 FrontRoomsRelayRigTests: **15/15 PASS** (t4, 14:19)

| Check | Result |
|---|---|
| A1 API vs 64b9de9c | all 3 events, 11 properties, 5 methods, 24 serialised fields, the enum: unchanged |
| A2 binding | 3 skinned LODs, 36 bones, Creature_* materials, last LOD transition 0 (never culled), 16 primitives kept and hidden |
| A3 fallback | `useProductionBody` off → no body, 16 primitives draw; on again → body back |
| P1 envelope (BakeMesh LOD0) | Low top 2.375, pin −0.095, lens 1.326 (face +0.3: 1.626) · Std 2.848, pin −0.075, lens 1.704 (face 2.027) · Tall 3.260, lens 2.921; PressesCeiling true in Low / Std, false in Tall |
| D1 door sequence | weight 1.0, approaching → leaving in order, 0 after |
| C1 the catch | 0.7 m long neck: lens 0.319 m, nearest vertex 0.202 m (pull 0.31 m) · 1.3 m: 0.512 / 0.496 · face +0.3: 0.368 / 0.315 and 0.717 / 0.697 |
| L1 photosafety | max 3 transitions per sliding 1 s, lens and light (69 / 65 in the hostile 60 s script) |
| L2 Reduce Flashing | every swing ≥ 0.5 s (lens 0.50, light 0.48 at 60 Hz sampling) |
| L3 no radar | camera at 2 m vs 30 m: lens and light series identical (max diff 0.000000) |
| L4 levels | Listen 0.06 (no light) · Search 0.06 (no light) · Walk 0.25 (light 0.40, r 1.6 m) · Run 1.00 (light 1.60, r 3 m) |
| E1 CeilingBrush | Std and Low walking 2.00/s, min gap 0.500 s; Tall 0; standing 0 |
| E2 Squeeze (no colliders) | 4 events at DoorSqueeze 0.017 / 0.333 / 0.650 / 0.950 |
| E3 Step | alternating, 2.17/s at 2.6 m/s (expected 2.28), at the boot (≤ 0.012 m from the toe) |
| G1 allocation | 0 bytes over 600 ticks with state, ceiling and door changes |
| G2 cost | body tick 0.014 ms mean, 0.020 worst (edit mode, load 29: the only reading not under load) |

### 2.3 Look-dev and T5 walks (`FrontRoomsRelayBodyBuild`, edit mode, renders through the game's URP + post)

- **Door walk** (1.0 × 2.1 door, 2.6 m/s, DoorSqueeze as the map computes it, LOD0 baked every 0.1 m from s −1.2 to +1.2):
  - with the header probe on: **0 of 25** poses in the wall slab;
  - without it (no colliders): **9 of 25, worst 0.717 m**.
- **Opening walk** (1.6 m wide, no DoorSqueeze at all, the map's arch case): **0 of 25**.
- **Low → Std → Tall walk:** pin error 0.000 m once settled; 2.33 steps/s, alternating; brush 2.00/s over the 6 s pinned; slow-foot drift ≤ 5 mm per frame.
- **Images** (Shot format, VL346/VL347 pending): `images/vl346_t5_rooms.jpg`, `vl346_t5_squeeze.jpg`, `vl346_t5_close.jpg`, `t4_catch_prelim.jpg` (VL345, preliminary), `vl347_t6_lens.png`, `vl347_t7_ingame.jpg`.

### 2.4 Map suites (`FrontRoomsCxSuites -cxOnly suites`, 42 s): all PASS

- FrontRoomsRelayNavTest: **PASS · 60/60** hunts arrived, 0 pass-throughs, 0 frames in architecture, 0 exceptions, door rule PASS.
- MapVerification 100/0.
- MapInteractionTests 175/0. Its console shows 2 errors and 4 exceptions: the test's own injected failures ("tools: forced build failure", "test: a broken glass kit"), the same as main's K2 baseline.
- LevelDesigner 140/0, FixtureTick 28/0, CameraRig 33/0, Captions 9/0, GlassShot 20/0, Stream 5/0, GlassCompileCheck no exception.

### 2.5 In the game (Play mode, seed 516574485, `-hbPlayLog`)

| Run | Result |
|---|---|
| `FrontRoomsCxPlayRun` hb1 / hb2 / hb3, 120 s each, `-cxRelayDelay 8` (hb3 = final code) | 120 map seconds, 7,307 frames each. Console: only the two expected exceptions (FMOD bank, QuickSearch) and the FMOD file error; 0 pink renderers; G14 on (High). hb3 Relay: IdleListen 13.5 s, Walk 85.5 s, Search 4.5 s, 21 state changes; 173 steps, 146 ceiling brushes, 2 squeezes; lens ≤ 0.25 (no chase in this seed); body tick 0.038 ms mean, 3.98 ms max (UNDER LOAD) |
| `FrontRoomsInteractionAudit` (60 s, DONE, 80 frames) | the map's Relay **breaks the door (BreakDoor), squeezes through, chases (Run, lens 1.0) and catches at 0.681 m**. The `114` patch is readable in the catch frame (`79_relay_broken_+1000ms.png`). Frames in `vl347_t7_ingame.jpg` |

- **hb1 found a real bug:** the first header probe (one ray at the body top) fired at things that were not doors, so the body ducked at nothing. hb2 showed the cause: the map's **doorless arches** (1.1–1.8 m wide, top 2.2 m), which never raise DoorSqueeze.
- **hb3 runs the final probe** (§3). In its log, every probe-driven duck but one is followed by a crossing (s turns positive within 0.5 s). The exception: at 34.0 s the Relay stopped to search and turned away, and the duck released. The one map door shows DoorSqueeze 0.68.

---

## 3. Changes against the plan (each with its reason)

1. **The full lens level is 0.36 × Troffer_Lens per channel, not 0.6 ×** (`FrontRoomsRelayLens.FullEmission`).
   - Measured at 0.6 ×: the lens sat at 205/255 everywhere (the game's grade clips near 210). It showed no tube band and no tell: a flat white panel, which is exactly rule 4's screen read, and it broke the bloom bar (lens p99 under the troffers').
   - Now the band peaks near white and the gaps sit ~2.9 : 1 lower on screen at the catch (158 vs 98 of 255).
   - The emission map's gaps were also deepened (4.16 : 1), and the tell made stronger.
2. **The self-light is a wide spot from the lens (140°), not a point light 0.25 m out.** The point light blew the hood's own rim out (lit at 10–20 cm). A spot along the lens normal leaves the rim dark and still lights the chest, gloves and floor ahead. Intensity as planned (1.6 full, 0.4 Walk).
3. **The door keys no longer put a glove on the casing** (§1.1). A world-anchored hand needs hand IK to a contact point; that is a later refinement.
4. **The early-warning probe became a header probe, and it also drives crossings the map never reports.**
   - Rays: 2.3 m at the centre and ±0.95 m to the side, plus 1.95 m and 1.0 m that must be clear. They detect a door head or an arch header ahead; the distance is dead-reckoned between the 10 Hz probes, and at the plane a crossing starts.
   - This was not in the plan: the plan covered doors and windows, but the giant also has to pass the map's arches.
5. **No GlassRT hunk.** The mirror uses the existing public API from a new file, so `FrontRoomsGlassRTSystem.cs` (busy with `glass-rt-g14-universal`) is not touched.
6. **`_FR_SHEEN` also carries per-texel metal** (mask B). The trim atlas mixes dielectric tape with the brass ring; the shader's metal is one uniform otherwise.
7. **BreakDoor and Stagger are procedural layers,** not baked additive keys. **The low-zone tripod gallop is not built:** Chase in Low is the squat walk at the run cadence.
8. **No shader-variant prewarm asset yet**, and **no change to the WebGL quality level.** The LOD1 cap is done at runtime under the WebGL define.
9. **Edit-mode tools must set `forceMatrixRecalculationPerRender`** to render several poses in one editor frame (found here: every render showed the first pose). Play mode needs nothing.
10. **Import is Generic + Animator stripped**, because the project has the Animation module off. The FBX is read/write for the mirror's BakeMesh (+ about 1 MB CPU copy).

---

## 4. Open issues and risks (for the next stage and for Red)

- **T1 is not run.** This is the cold read that decides B or the fallback A.
  - At the catch and in Chase the lens still reads as a pale, rounded, portrait panel in a dark hood (`t4_catch_prelim.jpg`, `79_relay_broken_+1000ms.png`). That is the phone / TV risk rule 1 names.
  - The bands now show but the tell is faint at 1.3 m.
  - **Recommendation:** run T1 before any more look work. If it fails, Red's rule says switch to A; the armature, blend, rig code, lens, tests and contracts all carry over.
- **T2 (2.05 m contrast per room type), T3 (silhouettes) and T4 proper** (low pose, frame band, cold read) are not made. Note the stairwell study: the pale prototype head read 1.05–1.11 : 1 against Level 0 paper. The B body is dark twill with a pale lens, so the whole-figure 4 : 1 test at the EXIT and on stair landings belongs to T2.
- **Without colliders the door clips** (9 of 25, worst 0.72 m). The probe depends on the map's wall and header colliders, which exist in the game (hb3 shows the probe working). Map contract **M2** (SqueezeReach 1.2 m) would make it a backup only.
- **Windows** (head 2.0 m) would clip about 8 cm in the door keys if the Relay ever crosses one (M2's window part).
- **The ray-traced reflection of the body is not visually verified.** The mirror registers in Play, but no glass receiver was on screen in these runs (glassPixels 0).
- **`RefitMesh` allocates** a vertex array per refit (≤ 15 Hz, about 33 KB). That is in GlassRT's API, and only while G14 runs.
- **Memory:** twill 4096² A/N/S is about 3 × 16 MB BC7 with mips on desktop. Handheld memory is flagged to App Store · 构建 (iOS tab, never Mac or Windows).
- **The edit-mode preview** of the scene's Hunter instantiates a `DontSave` body when the scene opens in the editor. The scene file never changes.
- **This stage briefly broke the two-job rule.** At 14:16 a third Unity process on the visual line ran for 19 s (`l7_lookdev`); the runner printed the count but did not wait. `hunterb/run_unity.zsh` now waits for a free slot (anchored match on the Unity binary, never two on this clone).

---

## 5. Map contracts (关卡设计): notes only, nothing edited

The plan's M1–M8 stand as written (`10_plan.md` §8). Updates from this build:

| # | Ask | Status after the build |
|---|---|---|
| M1 | CeilingHeight = the lowest ceiling over this cell and the next cell along Heading within 1.0 m | still useful; the 0.25 s fold covers most crossings |
| M2 | SqueezeReach 0.6 → 1.2 m, and window crossings into DoorSqueeze | the rig's header probe now covers doors and arches where colliders exist; M2 removes the dependence on colliders and handles windows |
| M3 | catch distance: Red's open call | both work (C1) |
| M5 | arrival troffer strike | unchanged (map) |
| M6 | drop `C("A99E78")` from `CreateHunter` | unchanged; only the fallback rig uses it |
| M7 | door slow-down: Red's open call | the body squeezes at any speed |
| M8 | BreakDoor standoff 0.45 m vs the giant's blow | seen in InteractionAudit (frames 66–73): the body reaches the leaf; a pose offset may still be wanted after T7 review |
| **M9 (new)** | doorless arches: report them like doors (an `ArchSqueeze`, or DoorSqueeze for Arch edges), or pass their header height | without it the rig relies on its probe for every arch |

---

## 6. Sound hand-off (H4), for 声音设计 (written here; no message was sent)

The final body is as planned in `10_plan.md` §7: a ~3.25 m giant (2.86 m pinned) in a 65/35 twill coverall, about 0.45 m rubber-soled boots, canvas gloves, a brass key ring and a tool pouch, dragging its shrug and hood along acoustic tiles. Measured hook behaviour:

- **`Step`:** at each foot plant, the ball of the boot.
  - The cadence follows the measured ground speed: 2.17–2.33 steps/s at Hunt pace (2.6 m/s in tests, about 2.1 m/s in game); the run steps are 1.45 m (std).
  - Feet always alternate.
  - Steps continue in the squeeze, at reduced arm swing.
- **`CeilingBrush`:** while pinned and moving, at mid-swing, 2.0/s at most (≥ 0.5 s apart); never in Tall; never standing. In game about 1.2/s (146 in 120 s).
- **`Squeeze`:** in Play, from real contact with the jamb (overlap spheres at the leading shoulder, hood and hips), then each +0.3 of DoorSqueeze. Without contact, the old threshold rule.
  - Arches give no DoorSqueeze, so there is **no Squeeze event at arches**. If 声音 wants one, ask the rig for an additive `HeaderSqueeze` event; it is not built.
- **Rig `State`:** unchanged. The lens follows it (Listen standby, Search starter pulses on each latched head snap, Walk steady, the Run strike, the BreakDoor shudder and the final-blow re-strike): the starter-click grammar of §7.
- A read-only `LensLevel01` on the rig is still proposed only.

---

## 7. The package `hunter_b` (W/apply, convention v2)

- **51 paths, 3 kept `.meta`:**
  - 48 new: 4 runtime .cs + metas and the folder meta; FBX + 2 JSON + metas and the folder meta; 3 .mat and 10 textures + metas; 2 editor .cs + metas; 2 Blender .py;
  - 3 replaced: FrontRoomsRelayRig.cs, FrontRoomsRenderSetup.cs, FrontRoomsSurface.shader;
  - 24 new GUIDs, all unused in main's Assets.
- **Bases** (re-checked at 14:23 against main `6521e6ad` + working tree): rig `64b9de9c`, RenderSetup `256cf3a4`, shader `a82fe187`.
- **Dry run** on main: **DRY RUN OK**.
- **Scratch `--apply`** (APFS copy of main in `W/tools/_test/hunter_b_1022/target`): MANIFEST OK, then ALREADY APPLIED.
- **Roslyn** main + payload `--all` and `--ios`: COMPILE OK.
- **Write order:** metas → textures → materials → shader → FBX → JSON → Tools → C#.
- **For the 游戏视觉 session** (claim in LANDINGS.md before `--apply`):
  - RenderSetup and the shader are busy files tonight (`outlets_p4b`, `d16`, glass). On any base mismatch run `W/tools/rebase_apply.sh hunter_b`.
  - The shader hunk needs one Unity batch compile in a clone after a rebase (Roslyn cannot compile shaders).
  - Recommendation: **land after T1**, because a T1 fail switches to A and changes the asset, not the code. If Red wants the body in the next snapshot anyway, it is safe for main's compile; the old rig is one flag away (`useProductionBody`).

---

## 8. Files (sha1, as in `hunter_b_expected.txt`)

| Path | sha1 |
|---|---|
| `Assets/Scripts/FrontRoomsRelayRig.cs` | f01cafc36229e4d2946b6a1e599c2e38bd911828 |
| `Assets/Scripts/Rendering/Relay/FrontRoomsRelayBody.cs` | f472b3524c69489dc36c9fa75039e9fb31527c7d |
| `Assets/Scripts/Rendering/Relay/FrontRoomsRelayPoseSet.cs` | de7e6497dff45f784977a4915340b0237976e703 |
| `Assets/Scripts/Rendering/Relay/FrontRoomsRelayLens.cs` | ce4e390d5c3219925332f36730abe91c49ecb270 |
| `Assets/Scripts/Rendering/Relay/FrontRoomsRelayGlassMirror.cs` | 639e39f290f2e5e286c6ec4d7522f6859f5e6e2b |
| `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs` | 3ffc96ea4c4ecfc1c31d189c1b1fa16f80ddfbcc |
| `Assets/Resources/Rendering/FrontRoomsSurface.shader` | 8af5163ecaf7914ab8ed0a194ac365cdf50e5ec5 |
| `Assets/Editor/Rendering/FrontRoomsCreatureImport.cs` | 5778cd15ea531a83209bc193dee6a564ecd0adb5 |
| `Assets/Editor/Rendering/FrontRoomsRelayRigTests.cs` | ed33d8b91c4df9e0fd5d42afeab2f9ab06837bf4 |
| `Assets/Resources/Creatures/Relay_NightShift.fbx` | 789c8fa45c89ae0ea18a07a3bfdef062b9cf38cd |
| `Assets/Resources/Creatures/Relay_NightShift.json` | a2e8433b827ebb8779b536b95d5a3ec21360dfe4 |
| `Assets/Resources/Creatures/Relay_NightShift_Poses.json` | 38f9e06b2cab77cd6a5910beaae8dc2026450b14 |
| `Assets/Resources/Surfaces/Creature_TwillSpruce.mat` / `LensOpal.mat` / `TapeSilver.mat` | 6ca0647d… / 7c7507d4… / 24a369d9… |
| `Tools/Blender/frontrooms_kit/creature_rig.py` | 330e1fa85e7ad8f66af608f9afa6bda06382f4fb |
| `Tools/Blender/frontrooms_kit/creatures/relay_night_shift.py` | 82931c5ab2b852cf6897ae0d08b6195ff580ec02 |

The full list, with every texture and `.meta`, is in `W/apply/hunter_b_expected.txt`.

**Work and logs:**
- work dir: `W/hunterb/` (logs b12–b14 Blender, s2–s6 setup, t1–t4 tests, l1–l7 look-dev, c1 suites, p1–p3 play, a1 audit; `figs/` plots and frame script; `build_package.py`);
- clone outputs: `W/proj_hunterb/Verification/{hunterb, hunter_b/tests, cx_audit/play_hb1..3, interaction_audit}`;
- VL specs: `W/vl_pending/hunter_b.md` (VL346, VL347; VL345 preliminary; VL342–VL344 for the next stage).
