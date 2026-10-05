# 30 — Glass track: final state after the fix pass

Date: 2026-10-03, 12:15. Status: **ready to promote, not signed off.** Red has to judge the frames in §2.

Details:
- implementation: `10_implementation.md`;
- measurements: `20_verification.md` (G10 run 3);
- corrected RT study: `11_reflections_and_raytracing.md`.

Everything was built and tested in the private clone
`/private/tmp/claude-501/-Users-redwang-Desktop-ArtCenter-Fall26T7-EGAM-401A-01-Individual-Game-Project/5656cffd-bc90-45f6-86a3-09b26549df8d/scratchpad/proj_glass`
(**the clone**). Nothing in `Frontrooms3D/` was changed except this folder. A text copy of every promotable source,
material and `.meta` is in `promote_src/` (suffix `.txt`); the binaries are listed with md5 in §4.

---

## 1. What Red will see

**Windows (`Glass_Window`, once the map loads it):**

- **Clear glass, not a milky cyan veil.** The room behind the pane keeps its colour and contrast.
- **It reflects like real glass.** About 8 % face-on, rising to 10 % at 50°, 14 % at 60° and 36 % at 75°. Real
  6 mm glass reflects 8.2 / 10.9 / 15.7 / 38.5 %. The old shader reflected 2.4 % face-on, a third of real glass.
- **Where you notice it:**
  - the ceiling lamps behind you show along the top of the pane;
  - at 50–60° the near wall's wallpaper shows across the glass as a soft sheen;
  - from under a dead lamp, a lit troffer in the next room reflects clearly.
- **Face-on in an evenly lit room it is still a quiet pane.** Real glass between two equally lit rooms is too. What
  tells the eye "glass" there is the frame and the glazing stop, which the map does not build yet (W1.4). This is
  the biggest missing piece.
- **Grime:**
  - a dust strip above the sill;
  - soft smears at hand height;
  - prints near the side edges;
  - fine specks only when you are within ~2 m (they used to look like debris on the floor beyond);
  - no dark mottled band on the jamb (it used to look like mould);
  - the two sides of a pane are dirty differently.
- **Zone reflections fade in 0.5 s** when you cross into Office, a tall hall or under a dead lamp. On the Web the fade
  is a quick dip.
- **The title rooms** reflect the Level 0 room instead of Unity's blue default sky. This comes from a one-line
  RoomStream hook. The title rooms were not in the capture, so this is UNVERIFIED on screen.

**Props:**

- **Hutch and display cabinet:** the arched glass doors and the glass shelves read as glass again. In the previous
  pass they read as solid wood.
- **Vending front:** clearer, products more saturated.
- **Water-cooler bottle:** unchanged look, slightly clearer.

**Walls, floors, ambient:**

- **Walls and floors** reflect the zone cube at half the captured light (2.3× the previous pass). In the audit frames
  this is just above the noise: nothing glows, and the frozen wallpaper print does not show.
- **Ambient:** no change. `ApplyAmbient()` now leaves the image exactly as the scene file has it (main's F5).

**Shards (for GD3):**

- The audit's near-black shard read as black chips on carpet.
- The new opaque `Glass_Shard` matches Level 0 carpet, but reads as pale yellow chips on Office carpet.
- The new transparent `Glass_ShardClear` reads as glass on every floor tested.

## 2. Before / after images (`images/`)

| File | Shows |
|---|---|
| `g10r3_02_window_L0.jpg` | Level 0 window, 1.5 m / 50° / 0.7 m / 61°: BEFORE \| previous AFTER \| NEW AFTER \| no pane |
| `g10r3_03_window_Office.jpg` | Office window, same four views |
| `g10r3_04_window_crops_1to1.jpg` | 1:1 crops at the pane centre, six views |
| `g10r3_05_diagnostics_L0.jpg`, `g10r3_05b_diagnostics_Office_deadlamp.jpg` | the pane region with ×6 difference images (the reflected wallpaper at 50°/61°) and one knob changed at a time |
| `g10r3_06_deadlamp_windows.jpg` | windows seen from under a dead lamp (troffer reflection) and from the lit side |
| `g10r3_07_props.jpg` | bottle, vending front at 22° and 53°, staged desk glass, staged hutch + cabinet |
| `g10r3_08_shards.jpg` | shards on Level 0 and Office carpet at 1 m and 2 m, three shard materials |
| `g10r3_01_representative.jpg` | audit frames 01 / 34 / 35 / 37 (walls and floors) |
| `g14_hook_proof.jpg` | the RT input: baseline, input on, depth-tagged |
| `g10r3_full_*` | full-resolution NEW AFTER frames for judging at 1:1 |

The run-2 sheets (`g10_0*.jpg`, `g10_full_*`) stay as the previous state.

## 3. What changed in this pass (one line each; reasons in `10_implementation.md` §0)

- **G14 hook:** `_FR_GlassRTReflection` + `_FR_GlassRTWeight` behind the global keyword `_FR_GLASS_RT`.
  - Bit-identical with the keyword off: 175/175 checks, run against today's shader and again against the final shader.
  - A plain uniform branch failed (49/90, last-bit differences).
- **Physical reflectance:** two surfaces (`_PaneF0` .08, specular setup); alpha = absorption + reflectance;
  reflection floor 1.0 linear.
- **`_ReflectionMin`** divides by the steady zone intensity `_FR_ZoneReflNominal`, so glass dips with the world on
  the Web.
- **Zone intensities are linear** (0.5); the slider gets `LinearToGamma`.
- **Measured: `ReflectionProbe.intensity` is applied in gamma.**
- **Grime:** distance-faded specks, wider smears, rim and corner cut, `_Scatter` 3 → 1, per-face offset;
  `_DustFilm` .12 + `_Scatter` .5 on `Prop_Glass`.
- **Variants:** probe-blending/box-projection keywords as URP Lit (G7-ready); the dead SH pragma and interpolator
  removed.
- **Cubes recaptured:**
  - with main's F5 ambient and panes drawn as `Glass_Window`;
  - `capture_manifest.txt` + `WarnIfStale()`.
- **Runtime robustness:**
  - R-restart reset;
  - rewrite re-check;
  - no RT leak per Play session;
  - fades finish while paused;
  - `FlipY` from the platform.
- **`FrontRoomsLook.cs`:** merged by hand onto main's F5 file.
- **RoomStream:** one line, so title rooms reflect the Level 0 cube.
- **`Glass_Shard`:** base now 0.85 × Level 0 carpet. New `Glass_ShardClear`.
- **`11_reflections_and_raytracing.md`** corrected: ray tracing is possible on the Mac (native Metal plugin) and on
  Windows DX12 without HDRP; one input for RT and planar; new row IDs G15–G18.

---

## 4. Files to promote into `Frontrooms3D/` (relative paths; copy each **with its `.meta`**)

Source: the clone. Copy after Red approves. The materials reference the shader and texture GUIDs in these `.meta`
files.

**Copy as-is (new files):**

| Path | md5 (first 8) | Bytes |
|---|---|---|
| `Assets/Resources/Rendering/FrontRoomsGlass.shader` + `.meta` | 3e4bdc16 | 29,095 |
| `Assets/Resources/Rendering/FrontRoomsReflectionBlend.shader` + `.meta` | 81dc1692 | 3,170 |
| `Assets/Resources/Rendering/Reflections.meta` (folder) | — | — |
| `Assets/Resources/Rendering/Reflections/Refl_Level0.exr` + `.meta` | d6acc839 | 1,323,897 |
| `Assets/Resources/Rendering/Reflections/Refl_Office.exr` + `.meta` | cc315e5a | 1,193,004 |
| `Assets/Resources/Rendering/Reflections/Refl_Tall.exr` + `.meta` | c0f1b6e7 | 1,119,177 |
| `Assets/Resources/Rendering/Reflections/Refl_DeadLamp.exr` + `.meta` | 44d83e56 | 1,319,555 |
| `Assets/Resources/Rendering/Reflections/capture_manifest.txt` + `.meta` | f34a1523 | 946 |
| `Assets/Scripts/Rendering/FrontRoomsZoneReflection.cs` + `.meta` | 42d371db | 16,596 |
| `Assets/Scripts/Rendering/FrontRoomsZoneReflectionDriver.cs` + `.meta` | 0b3329bb | 702 |
| `Assets/Scripts/Rendering/FrontRoomsGlassPane.cs` + `.meta` | b2792eef | 2,789 |
| `Assets/Editor/Rendering/FrontRoomsGlassSetup.cs` + `.meta` | c43bf659 | 12,964 |
| `Assets/Editor/Rendering/FrontRoomsReflectionCapture.cs` + `.meta` | ceec26df | 23,501 |
| `Assets/Editor/Rendering/FrontRoomsGlassVerification.cs` + `.meta` | 2aa3e10b | 41,128 |
| `Assets/Editor/Rendering/FrontRoomsGlassCompileCheck.cs` + `.meta` | 0d1144b1 | 5,478 |
| `Assets/Editor/Rendering/FrontRoomsGlassRTStripper.cs` + `.meta` | 2e22d16d | 1,172 |
| `Assets/Resources/Surfaces/Glass_Window.mat` + `.meta` | f6285cd6 | 1,950 |
| `Assets/Resources/Surfaces/Glass_Edge.mat` + `.meta` | 0ffba08c | 3,840 |
| `Assets/Resources/Surfaces/Glass_Shard.mat` + `.meta` | a8ff20b2 | 3,845 |
| `Assets/Resources/Surfaces/Glass_ShardClear.mat` + `.meta` | 92a4c8ef | 1,801 |
| `Assets/Resources/Surfaces/Textures/GlassGrime_M.png` + `.meta` | 5ce543f2 | 3,116,990 |
| `Assets/Resources/Surfaces/Textures/GlassSmear_N.png` + `.meta` | 8de9065b | 496,860 |
| `Tools/lookdev/pack_glass_grime.py` (no `.meta`, outside Assets) | fe7682ff | 3,143 |

**Overwrite (existing assets, GUIDs and `.meta` unchanged):**

| Path | md5 (first 8) |
|---|---|
| `Assets/Resources/Surfaces/Prop_Glass.mat` | 0583e4e9 |
| `Assets/Resources/Surfaces/Prop_BottleBlue.mat` | 85bf5dbb |

**Hand-merge into main's current file (never copy the file):**

1. **`Assets/Scripts/Rendering/FrontRoomsLook.cs`** (main md5 2e3ae3bb…, 10:23, F5). Keep the F5 constants and their
   comment. Then:
   - in `SetZoneReflection`, replace the stub body with `FrontRoomsZoneReflection.Set(zone, blendSeconds);`;
   - in `ApplyAmbient`, after `DynamicGI.UpdateEnvironment();`, append `FrontRoomsZoneReflection.Reapply();`.

   Optional: the clone's doc comments (above `ReflectionIntensity`: ".3 is 0.073 linear"; the `SetZoneReflection`
   summary). The full merged file is `promote_src/Assets/Scripts/Rendering/FrontRoomsLook.cs.txt` (the clone's
   `:40` and `:60`).
2. **`Assets/Editor/Rendering/FrontRoomsRenderSetup.cs`**: the one-line hook, §5.
3. **`Assets/Scripts/FrontRoomsRoomStream.cs`**: at the end of `Initialize`, after `initialized = true;`:

   ```csharp
   // GLASS G6 HOOK: no title-stream cubes yet, so the title rooms reflect the Level 0 cube, not Unity's default sky.
   if (Application.isPlaying) FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Level0, 0f);
   ```

   Main's file was identical to the clone's before this line (md5 48893264…).

**Do not promote:** `Assets/Editor/Audit/` (the G10 harness, `G10/FrontRoomsGlassBaseline.shader`,
`G10/G10Before_*.mat`), anything under `Verification/`.

**After promoting:**
1. In Unity, run *FrontRoomsss → Rendering → Set up glass materials*. It rewrites the materials in place and ends with
   the cube staleness check.
2. Run *FrontRoomsss → Rendering → Check zone reflection cubes are current*.
3. Recapture the cubes (*Capture zone reflection cubemaps*) once the map uses `Glass_Window` and the Level 0
   `Troffer_Lens` ×1.5 has landed (§6).

## 5. The one-line RenderSetup hook

In `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs`, `SetUp()`, directly after `EnsureGlassMaterials();`
(the clone's `:43` → `:44`):

```csharp
FrontRoomsGlassSetup.EnsureAll(); // GLASS HOOK (G1-G4): Assets/Editor/Rendering/FrontRoomsGlassSetup.cs makes Glass_* and moves Prop_Glass/Prop_BottleBlue onto FrontRooms/Glass after the URP Lit pass above
```

Nothing else in RenderSetup or `FrontRoomsSurface.shader` was touched. The wallpaper workflow owns both.

## 6. What the map chat must do (G9 / W1.4)

1. **Load the materials by name**, instead of building `TransparentGlass` in code (`FrontRoomsMapWorld.cs:2070-2087`
   in main):
   - `Resources.Load<Material>("Surfaces/Glass_Window")` (= `FrontRoomsGlassPane.Window`) for the pane;
   - `Resources.Load<Material>("Surfaces/Glass_Edge")` for any separate glass edge faces.

   Keep `TransparentGlass` only as a null fallback.
2. **The pane:**
   - **6 mm** thick (`ModuleUnits.GlassThickness` .03 → .006);
   - a scaled unit-cube mesh (Y rotation is fine; a mesh in metres sets `_PaneSize`);
   - **shadows off** (`ShadowCastingMode.Off`);
   - keep `FrontRoomsMetalGlassTarget` on the visible glass renderer.

   W1.4's 1.391 × 1.642 slab works if it is that unit cube.
3. **Glazing stop** (audit §4.1): 18 × 12 mm, both faces, four sides, plus the stool trim. It is the main missing
   "there is glass here" cue.
4. **Calls:**
   - `FrontRoomsLook.ApplyAmbient()` at run start (already in Awake in main; it changes nothing on screen);
   - `FrontRoomsLook.SetZoneReflection(zone)` at run start and on every zone / dead-lamp change (every frame is fine).
     Zones: Level0, Office, Tall, DeadLamp.
   - Pick DeadLamp from the local light level, not from the one cell's lamp. A dead lamp in a lit tall hall is not
     dark.
5. **Cracks:** `FrontRoomsGlassPane.SetCrack(renderer, crack, FrontRoomsGlassPane.ImpactUV(pane, hit.point), seed,
   palm)` until GD3's fracture meshes replace it.
6. **Tell the visual chat** when the panes use `Glass_Window` and when the Level 0 lens changes. The zone cubes must
   then be recaptured (the manifest records `Map / Level 0 lens`).

**For GD3 (shards):**
- Faces `Glass_ShardClear` on desktop (reads as glass on every floor), edges `Glass_Edge`.
- If WebGL keeps opaque shards, use one `Glass_Shard` instance per floor with base = 0.85 × that floor's mean albedo.
  These are linear values from each base map's mean × `_BaseColor`; macro maps are ignored:

| Floor | Linear | sRGB swatch |
|---|---|---|
| Level 0 carpet (the shipped `Glass_Shard`) | .214 / .161 / .074 | .500 / .438 / .301 |
| Office carpet tile | .074 / .088 / .104 | .301 / .328 / .355 |
| Run VCT | .573 / .555 / .493 | .781 / .770 / .731 |
| Shift carpet | .177 / .127 / .050 | .458 / .392 / .247 |
| Exit carpet | .084 / .077 / .037 | .321 / .307 / .213 |

## 7. Task-queue text (for the visual chat to paste into `Documentation/VISUAL_CHAT_TASKS.md`; this track does not edit that file)

- **G1–G4, G6:** "DONE in proj_glass after the fix pass (`research/glass/30_final.md`); waiting for Red's look
  sign-off and promotion. Physical two-surface reflectance; the hook for G14 is in and proven bit-identical at
  weight 0."
- **G7, amend:** "Shader side done: `FrontRooms/Glass` and `FrontRooms/Surface` both need
  `multi_compile_fragment _REFLECTION_PROBE_BLENDING / _BOX_PROJECTION` (glass has them). Give every room probe
  `intensity = Mathf.LinearToGammaSpace(zone linear)`: URP applies probe intensity in gamma (measured). Enable blending
  and box projection only in the desktop URP asset."
- **G10:** "Run 3 DONE (`research/glass/20_verification.md`). The next sign-off needs the map's glazing stop."
- **G14, add:** "Hook contract: `research/glass/10_implementation.md` §8.1. RGB unweighted, A = coverage or
  1 + eye depth, write before transparents, keyword `_FR_GLASS_RT` only for the main camera. Remove the
  prototype's Fresnel/strength (`.mm:300`), alpha 1 (`:301`) and the after-post composite (`RendererFeature.cs:17`)."
- **New G15:** "Held-pane planar reflection for High-tier GPUs without RT (Windows/Linux, or the Mac when the plugin
  is missing). It writes `_FR_GlassRTReflection` with A = 1 + the held pane's eye depth. Quarter to half resolution,
  shadows off, far 12 m, 15–30 Hz, only during a hold (`11_reflections_and_raytracing.md` §2.4). visual + map hold
  event. QUEUED."
- **New G16:** "Relay-only planar overlay, Web tier. It writes `_FR_GlassRTReflection` with A = the Relay's coverage,
  only during a hold. Delete `FrontRoomsGlassRTStripper` when it lands. Measure in a browser first (≈ 7 % in the editor
  bench). visual + Relay layer (ask Red). QUEUED."
- **New G17:** "Floor SSR after a move to Unity 6.7 (desktop only). QUEUED/LATER."
- **New G18:** "Cinematic one-shot probe re-capture at hero windows. LATER."
- **GD3 note:** "Desktop shard faces: `Glass_ShardClear`. The opaque `Glass_Shard` needs one instance per floor
  (`research/glass/30_final.md` §6)."

## 8. Remaining work

| Row | State | Next step |
|---|---|---|
| **G5** `Kit_InteriorWindow` | not started | Put its pane on `FrontRooms/Glass` (no grime, `_PaneSize` for the kit mesh, `_RTReceive` 0). Add an interior-mapping back layer behind it, so the opaque black slab becomes a lit room. Needs the kit's pane sub-mesh size |
| **G7** room probes | shader ready | Desktop URP asset: blending + box projection on. Map: a Custom `ReflectionProbe` per built room, 64–128 px, box = the room, `intensity = LinearToGamma(zone linear)`, nearest 8–12 rooms. Then raise `MaxLinear` toward 1.0 for zones with correct dead-lamp handling, and re-run G10 with VCT and metal in view |
| **G8** fracture variants | in GD3 | GD3 replaces `CrackMask` with baked fracture meshes. Keep `_Palm`, `_ImpactUV` and the grime. The RT input stays on `Glass_Window` only (or a dedicated `Glass_Fracture` instance with `_RTReceive` 1) |
| **G12** staged fracture | superseded by GD3 | — |
| WebGL | not measured | One WebGL build with URP's shader variant log: the `FrontRooms/Glass` count against the WG budget (expect the `_FR_GLASS_RT` variants stripped); load an RGB9e5 cube in Chrome and Safari; the dip in a browser |
| Windows | not measured | Run `FrontRoomsGlassVerification.RunReflectionTestBatch` on D3D12 (and Vulkan) to confirm `FlipY` |
| Title cubes | not started | Capture Lobby / Shift / Office / Run / Exit cubes for RoomStream; until then the Level 0 hook |
| Look sign-off | waiting on Red | §2 frames. Then the next G10 run with the map's stop and frame in |

## 9. UNVERIFIED in this pass

- The title rooms with the Level 0 cube (no title frame was captured).
- Anything in a built player or a browser.
- Frame time on a quiet machine. The editor numbers show no difference beyond noise; other Unity processes were
  running.
- Main's newer lamp code (difficulty tiers, the WebGL near-lamp tick) was not in the clone. Re-capture G10 after
  promotion.
