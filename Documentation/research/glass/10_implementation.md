# 10 — Glass and zone reflections: implementation (G1–G4, G6, G14 hook)

Date: 2026-10-02/03; fix pass 2026-10-03 11:00–12:15. Status: **in the clone, NOT signed off, not promoted.** The
verification is `20_verification.md` (run 3). Red still has to judge it. The promotion list and the hand-merges are
in `30_final.md`. Nothing in `Frontrooms3D/` was changed except this folder.

All work is in the private clone
`/private/tmp/claude-501/-Users-redwang-Desktop-ArtCenter-Fall26T7-EGAM-401A-01-Individual-Game-Project/5656cffd-bc90-45f6-86a3-09b26549df8d/scratchpad/proj_glass`
(**the clone**; paths below are relative to its root). Values and plan come from `interaction_audit/10_audit_report.md`
§4.1–4.3 and `Documentation/VISUAL_CHAT_TASKS.md` rows G1–G4, G6, G14.

Tags: **UNVERIFIED** = not confirmed by a run, the code, or a page I read.

---

## 0. What the fix pass changed (2026-10-03)

| Critic issue | Verdict | What was done |
|---|---|---|
| 1 G14 hook missing | true | Hook added (`FrontRoomsGlass.shader:101-106, 161-163, 168-178, 453-485`). The critic's plain uniform branch was **not** bit-identical: 49/90 checks failed by ≤ 9.5e-7. So the hook sits behind the global keyword `_FR_GLASS_RT`, the critic's fallback. Proof: 175/175 against today's shader and again against the final shader (`20_verification.md` §8.2) |
| 2 F5 merge / stale ambient | true | `FrontRoomsLook.cs` = main's 10:23 file + 2 code lines + comments (§2). G10 re-run: B = C within noise |
| 3 reflection energy ~3× low | true (arithmetic re-done: 0.082 / 0.109 / 0.157 / 0.385) | `#define _SPECULAR_SETUP 1` (`:127`), `_PaneF0` .08 (`:423`); window alpha .11 + .89 F⁵; props .12 + .88; bottle .30 + .70; `_ReflectionMin` 1.0 |
| 4 RT study wrong | true | `11_reflections_and_raytracing.md`: correction box, §0.1, §1 heading, §1.2, §1.5, §4.1 RT row, §4.2 (single hook), §4.3 (new IDs G15–G18) |
| 5 hook contract gaps | true | Contract in §8.1. Depth tag implemented in the one texture (A > 1 = 1 + eye depth); `_RTReceive`. Registration tested at render scale 1 and 0.75 |
| 6 `_ReflectionMin` divisor | true | Divides by `_FR_ZoneReflNominal` (`:258-263`, `FrontRoomsZoneReflection.cs:240, 252`). Dip test: glass now dips with the world (§6.3). Probe keywords added as `multi_compile_fragment` like URP Lit, **not** the critic's `#define` (reason in §3.1). `ReflectionProbe.intensity` measured: applied in **gamma** |
| 7 global cap in gamma | true | Zone intensities stored linear: `ZoneLinear` {.5, .5, .45, .5}, `MaxLinear` .5 (`FrontRoomsZoneReflection.cs:45-48`). The slider gets `LinearToGamma` (`:131`) |
| 8 pane does not read; grime cues wrong | true | Specks fade with distance, smears widened, rim .12 / corner .3 / 6 cm, scatter 1 (window) / .5 (props), faces offset, `_DustFilm` for props. The frame cue is map work (W1.4) |
| 9 stale cubes | true | The capture now draws panes as `Glass_Window`, writes `capture_manifest.txt`, and `WarnIfStale()` checks it. Cubes recaptured with the F5 ambient. RoomStream title hook (1 line) |
| 10 robustness | true | Scene-load reset, early-return re-check, Play-mode-stop release, capture-step/unscaled time, `FlipY = !graphicsUVStartsAtTop`. All tested in play mode (§6.3) |
| 11 WebGL / variants | partly done | Dead SH pragma and interpolator removed; RT keyword stripped from WebGL builds; shards tested and fixed. **Not done:** a WebGL build variant count and RGB9e5 in browsers (no WebGL build was made) |

---

## 1. Short answer

| Row | Result |
|---|---|
| G1 shader | `FrontRooms/Glass` (HLSL, URP lighting, 498 lines):<br>• transparent, premultiplied "Alpha + Preserve Specular", **specular workflow with two-surface reflectance `_PaneF0` .08**, ZWrite off, no shadow caster, Cull Back;<br>• grime laid out in pane metres; crack and palm hooks;<br>• the G14 RT/planar input behind `_FR_GLASS_RT`.<br>Compiles offline for WebGL 2 (GLES3x) and Metal, including the hook and probe-blending sets (`logs/compile_check_fixpass.txt`) |
| G2 grime maps | 6 CC0 ambientCG scans packed into 2 textures. Desktop: BC7 1024 / 512. WebGL: DXT5 at half size |
| G3 materials | `Glass_Window` (glass + grime), `Glass_Edge` (URP Lit opaque), `Glass_Shard` (URP Lit opaque, **base 0.85 × the Level 0 carpet**), and new `Glass_ShardClear` (transparent glass for desktop debris). Made by `FrontRoomsGlassSetup`, plus a one-line hook in RenderSetup |
| G4 prop glass | `Prop_Glass` and `Prop_BottleBlue` on `FrontRooms/Glass`, no grime. **Correction:** the run-2 "kit look holds" was wrong. The hutch read as solid doors. Run 3 fixes it with physical reflectance + a light dust film (`20_verification.md` §7) |
| G6 reflections | Four 256 px HDR cubes captured in the real map with the game's lamps and the **F5 ambient**, with panes drawn as `Glass_Window`. `SetZoneReflection` works at runtime (play-mode test, §6.3). Intensities are linear (0.5) |
| G14 hook | `_FR_GlassRTReflection` + `_FR_GlassRTWeight`. Keyword off = bit-identical (proved). Contract §8.1 |

Findings that change how to read the audit:

1. **Unity applies both reflection-intensity knobs in gamma:**
   - the Lighting slider: measured ratio 0.230 for a 0.735 → 0.368 halving, predicted 0.223
     (`logs/reflection_test_fixpass.txt`);
   - `ReflectionProbe.intensity`: 1 → 0.5 gives 0.216; gamma predicts 0.214, linear 0.5.

   The audit's "≤ 0.5" meant half the captured light. In the run-2 code it was 0.214. Main's
   `FrontRoomsLook.ReflectionIntensity` .3 is 0.073 linear.
2. **A pane has two surfaces.** One-surface F0 .04 (run 2) reflected a third of what real glass does (§3.3 of 20). With
   physical values the pane reflects ~8 % face-on and 14 % at 60°. Face-on it stays quiet in evenly lit rooms, as
   real glass does. The outline cue (glazing stop) is map-side.
3. **WebGL's automatic format for an HDR cube is DXT1 (LDR).** The cubes override WebGL to RGB9e5 (HDR, 4 B/px).
   Whether it loads in Chrome/Safari is UNVERIFIED (no WebGL build).

---

## 2. Files (all in the clone)

| File | New / changed | Lines | What |
|---|---|---|---|
| `Assets/Resources/Rendering/FrontRoomsGlass.shader` | new | 498 | `FrontRooms/Glass` |
| `Assets/Resources/Rendering/FrontRoomsReflectionBlend.shader` | new | 78 | `Hidden/FrontRooms/ReflectionBlend`: cube crossfade, one face and mip per draw |
| `Assets/Resources/Rendering/Reflections/Refl_{Level0,Office,Tall,DeadLamp}.exr` (+ .meta, folder .meta) | new | | 1.1–1.3 MB EXR each, recaptured 11:57 |
| `Assets/Resources/Rendering/Reflections/capture_manifest.txt` (+ .meta) | new | | what the cubes were captured from |
| `Assets/Scripts/Rendering/FrontRoomsZoneReflection.cs` | new | 391 | runtime zone reflection (snap, fade, dip, retarget), linear intensities, `_FR_ZoneReflNominal`, scene-load reset |
| `Assets/Scripts/Rendering/FrontRoomsZoneReflectionDriver.cs` | new | 14 | hidden `LateUpdate` tick: capture step if set, else unscaled time |
| `Assets/Scripts/Rendering/FrontRoomsGlassPane.cs` | new | 55 | map helpers: `ImpactUV`, `SetCrack`, material names |
| `Assets/Scripts/Rendering/FrontRoomsLook.cs` | **hand merge** | 62 | main's 10:23 file (F5 constants and comment kept) + `FrontRoomsZoneReflection.Set(zone, blendSeconds);` in `SetZoneReflection` (`:40`) + `FrontRoomsZoneReflection.Reapply();` after `DynamicGI.UpdateEnvironment();` (`:60`) + comments. **Never copy the clone's pre-fix file over main** |
| `Assets/Scripts/FrontRoomsRoomStream.cs` | **one line** (+1 comment) | `:351-352` | `if (Application.isPlaying) FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Level0, 0f);` at the end of `Initialize`. Title rooms reflect the Level 0 cube until title cubes exist |
| `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs` | **one line** | `:44` | `FrontRoomsGlassSetup.EnsureAll(); // GLASS HOOK (G1-G4) …` after `EnsureGlassMaterials();` |
| `Assets/Editor/Rendering/FrontRoomsGlassSetup.cs` | new | 257 | materials + importers; menu *FrontRooms → Rendering → Set up glass materials*; batch `RunBatch`; ends with `WarnIfStale()` |
| `Assets/Editor/Rendering/FrontRoomsReflectionCapture.cs` | new | 438 | cube capture in the real map (panes as `Glass_Window`, RT weight 0), manifest, `WarnIfStale` menu |
| `Assets/Editor/Rendering/FrontRoomsGlassVerification.cs` | new | 747 | play-mode proof (`RunReflectionTestBatch`, run **without** `-quit`), window/prop look-dev |
| `Assets/Editor/Rendering/FrontRoomsGlassCompileCheck.cs` | new | 82 | hook-order check + offline WebGL/Metal compile, including the RT and probe keyword sets |
| `Assets/Editor/Rendering/FrontRoomsGlassRTStripper.cs` | new | 27 | `IPreprocessShaders`: removes `_FR_GLASS_RT` variants of `FrontRooms/Glass` from WebGL builds only |
| `Assets/Resources/Surfaces/Glass_Window.mat`, `Glass_Edge.mat`, `Glass_Shard.mat`, `Glass_ShardClear.mat` (+ .meta) | new | | |
| `Assets/Resources/Surfaces/Prop_Glass.mat`, `Prop_BottleBlue.mat` | changed (GUID kept; .meta unchanged) | | on `FrontRooms/Glass` |
| `Assets/Resources/Surfaces/Textures/GlassGrime_M.png`, `GlassSmear_N.png` (+ .meta) | new | | packed maps |
| `Tools/lookdev/pack_glass_grime.py` | new | 73 | packs the CC0 scans |

**Promotion.**
- **`FrontRoomsLook.cs`:** main changed it at 10:23 (F5), so the old note "both patches apply cleanly" is **false**.
  Copying the clone's pre-fix copy would have raised the ground ambient by 55 %. The clone now holds the merged file:
  main's file plus the two calls. Promote by applying the two lines to main's file, not by copying.
- **`FrontRoomsRenderSetup.cs` and `FrontRoomsRoomStream.cs`:** their only difference from main is the hook lines
  (checked by diff at 12:1x).
- **Not promoted:** `Assets/Editor/Audit/*` (the G10 harness, `G10/FrontRoomsGlassBaseline.shader`, the BEFORE
  material copies).
- Exact list with md5: `30_final.md` §4.

---

## 3. G1 — `FrontRooms/Glass`

### 3.1 Render state, lighting and variants

- **Render state.** `Blend One OneMinusSrcAlpha`, `ZWrite Off`, `Cull [_Cull]` (Back) (`:91-93`). One pass,
  `UniversalForward`; no ShadowCaster, DepthOnly or MotionVectors pass.
- **Plain defines (no variants):** `_SURFACE_TYPE_TRANSPARENT`, `_ALPHAPREMULTIPLY_ON`, and since the fix pass
  `_SPECULAR_SETUP` (`:125-127`). With premultiply, URP multiplies only the diffuse by alpha (`BRDF.hlsl:71`), which
  is "Preserve Specular". With the specular setup, `InitializeBRDFData` uses `_PaneF0` for both the environment and the
  lamp highlights.
- **Lighting** is URP's `UniversalFragmentPBR`, with the `FrontRooms/Surface` keyword set minus SSAO. That includes
  Forward+ (`_CLUSTER_LIGHT_LOOP`), cookies, light layers and fog.
- **Ambient.** Per-pixel `SampleSH` (`:417`). The `EVALUATE_SH_MIXED/VERTEX` pragma and the `vertexSH` interpolator
  were removed. They were filled but never read.
- **Probe keywords (G7).** `multi_compile_fragment` for `_REFLECTION_PROBE_BLENDING`, `_BOX_PROJECTION` and `_ATLAS`
  (`:113-115`), exactly as URP Lit (`Lit.shader:142-144`).
  - Why not the critic's `#define _REFLECTION_PROBE_BLENDING 1`: it would force the cluster probe path even when the
    URP asset has blending off (`FrontRooms_URP.asset:54` today). URP then requests per-object probes instead
    (`UniversalRenderPipeline.cs:2092-2098`), so glass would read the wrong source.
  - The keywords follow the asset like Lit, and URP strips them while no asset enables them
    (`ShaderScriptableStripper.cs:578-585`).
  - The critic's underlying point is right and verified: without the keyword, `GlobalIllumination.hlsl:34-35` defines
    blending as 0, and Forward+ with blending on does not request per-object probes. So glass would have ignored G7's
    room probes.
- **G14 keyword.** `#pragma multi_compile_fragment _ _FR_GLASS_RT` (`:105`). It is a global keyword: the RT/planar
  source enables it only for the camera it feeds.
- **Fog** is applied for premultiplied output: toward fog colour × alpha (`:487`).
- **SRP Batcher.** Every material property sits in one `UnityPerMaterial` cbuffer (`:131-164`, 220 bytes compiled).
  Each texture has its own sampler (WebGL/GLES).

### 3.2 Values and where they act

| Input | Window (`Glass_Window`) | Shader |
|---|---|---|
| Reflectance | `_PaneF0` .08 (two surfaces), specular setup; URP's Pow4 Fresnel → 0.095 at 50°, 0.137 at 60°, 0.358 at 75° | `:423` |
| Alpha | `_AlphaFace` .11 + `_AlphaFresnel` .89 × F⁵ (+ .25 × dust, + .05 × smudge, cracks). T = .89 face-on | `:393` |
| Base | linear (.02, .025, .022) → `_DustColor` linear (.42, .40, .34) by dust | `:391` |
| Smoothness | .96 → .62 by smudge; → .45 by dust × .6 | `:388-390` |
| Normal | flat + .02 long-wave roll (period .37 m) + smear normal × .05 | `:334-337, 364-365` |
| Reflection floor | `_ReflectionMin` 1.0 linear = the full captured light (physical: the cubes were captured under the game's own lamps); divided by the steady zone intensity `_FR_ZoneReflNominal` | `:258-263, 442-452` |
| Grime scatter | `_Scatter` 1 (was 3) | `:431-432` |
| Edge faces | `_AutoEdge`: linear (.28, .42, .34), alpha .92, smoothness .6 | `:397-401` |
| RT receiver | `_RTReceive` 1 | `:161-163` |

Props:
- `Prop_Glass`: alpha .12 + .88 F⁵, smoothness .94, `_DustFilm` .12, `_Scatter` .5, `_ReflectionMin` 1, `_RTReceive` 0.
- `Prop_BottleBlue`: sRGB (.36, .58, .80), alpha .30 + .70 F⁵, smoothness .90.
- `Glass_ShardClear`: as `Prop_Glass` without the dust film and scatter.

Setup code: `FrontRoomsGlassSetup.cs:84-142`.

**Pane frame (any pane size).** In the vertex stage the thinnest scaled object axis is the pane normal, *v* is object
up and *u* the rest (`:235-243`). The map's pane is a scaled unit cube. A Y rotation is fine, because the axes come
from the object matrix. A mesh modelled in metres sets `_PaneSize`.

**Grime layout** (`_FR_GLASS_GRIME`, windows only, `:339-367`):

- **Dust:**
  - a thin film everywhere (.14);
  - dense in the bottom 14 cm and in the corners (12 cm, weight .3);
  - along the glazing stop (6 cm ramp, weight .12);
  - broken up by mottling.
- **Specks:** full strength at 0.8 m, gone by 2 m (`speckNear`, `:355-357`). Further out they read as marks on the
  floor behind the pane, and the thresholded mip shimmers.
- **Smears:** 0.80–1.65 m above `_FloorY`, in wider patches (`smoothstep(.35, .70)`, `:359`).
- **Prints:** within 22 cm of the side edges, 0.85–1.75 m high.
- **Per pane:** a random offset. **Per face:** an offset of ±0.53 m by the face sign (`:343`), so the two sides of a
  pane differ.

### 3.3 Art-directed terms that remain

- **`_Scatter` (window 1, props .5).** Dust and smudges add room light from both sides as emission (`:431-432`).
  It is worth about 0.6–1.0 ΔY face-on (`20_verification.md` §4).
- **`_DustFilm` (props .12).** An even dust film on kit glass. Kit meshes have no pane frame for the grime layout.
  Without the film, clear glass over a dark interior vanishes (the run-2 hutch).
- **`_ReflectionMin` is no longer art-directed:** 1.0 is the physical value. It exists only because the world's
  default reflection is capped at 0.5 linear (frozen print, no parallax yet; §6.1).
- `_GrimeDebug` (`:489`) shows the masks: R dust, G smudge, B crack.

### 3.4 Crack and palm hooks (placeholder until GD3's baked masks)

- **`_Crack` (0–1)** is the reach of 9–14 radial cracks from `_ImpactUV` (`CrackMask`, `:266`). Rings appear at .30 /
  .55 / .80, with a crushed spot at the impact, and the wedges tilt by up to ±0.03.
- **`_Palm` (0–1)** draws a hand smudge at `_ImpactUV` (`PalmMask`, `:299`).
- Both are ALU only and sit behind a uniform branch.
- The map drives them with `FrontRoomsGlassPane.SetCrack(renderer, crack, FrontRoomsGlassPane.ImpactUV(pane,
  hit.point), seed, palm)`, which uses a property block on that one renderer.
- GD3 plans to replace `CrackMask` with baked fracture meshes (`destruction/10_glass_destruction_plan.md` §4.2).

---

## 4. G2 — grime maps

- **Sources:** ambientCG, CC0 1.0, approved by Red on 2026-10-02 and listed in
  `Frontrooms3D/Tools/lookdev/cc0_src/SOURCES.txt`: Smear007, Fingerprints002, SurfaceImperfections001, 007, 013
  and 015. URLs `https://ambientcg.com/a/<name>` are taken from SOURCES.txt and were not re-opened.
- **Packing:** `python Tools/lookdev/pack_glass_grime.py <cc0_src> Assets/Resources/Surfaces/Textures` stretches each
  channel between its 2nd and 99.5th percentiles.

| Texture | Channels | Desktop import | WebGL override |
|---|---|---|---|
| `GlassGrime_M.png` 1024² RGBA, linear | R Smear007 opacity · G Fingerprints002 opacity · B max(SI007, 0.7·SI013) specks · A 0.6·SI015 + 0.4·SI001 mottling | BC7, 1024, mips, trilinear, aniso 4 | DXT5, 512 |
| `GlassSmear_N.png` 512² | Smear007 NormalGL | normal map, BC7, 512 | DXT5, 256 |

## 5. G3, G4 — materials

`FrontRoomsGlassSetup.EnsureAll()` writes every material in place, keeping the GUIDs. Colours are `Color.gamma` of the
linear values.

| Material | Shader | Values |
|---|---|---|
| `Glass_Window` | FrontRooms/Glass + `_FR_GLASS_GRIME` | §3.2 |
| `Glass_Edge` | URP Lit, opaque | base lin (.28, .42, .34), smoothness .6 |
| `Glass_Shard` | URP Lit, opaque | **base lin (.214, .162, .074) = 0.85 × the Level 0 carpet's mean albedo**, smoothness .95. The audit's near-black base read as black chips (97.6 % of shard pixels on Level 0 carpet). On Office carpet this base reads as pale chips, so other floors need their own instance (`30_final.md` §6). Shard meshes: faces `Glass_Shard`, edges `Glass_Edge` |
| `Glass_ShardClear` | FrontRooms/Glass, no grime | clear shard faces for desktop debris; reads as glass on both carpets tested (shard/carpet 0.96–0.98) |
| `Prop_Glass` | FrontRooms/Glass, no grime | §3.2 |
| `Prop_BottleBlue` | FrontRooms/Glass, no grime | §3.2 |

**G4.** The old generator `FrontRoomsRenderSetup.EnsureGlassMaterials` still runs first and writes URP Lit with
SrcAlpha. The hook then overrides it. `FrontRoomsGlassCompileCheck` ran exactly that order
(`logs/compile_check_fixpass.txt`). `EnsureGlassMaterials` and `GlassDefs` are dead code. Delete them once the
wallpaper workflow's RenderSetup edits have landed (owner: visual).

**Kit look.** Run 3 ([g10r3_07](images/g10r3_07_props.jpg)):
- the hutch's arched panes and the cabinet's shelves read as glass again;
- the bottle reads as before;
- the vending front is clearer;
- the desk tumbler shows a faint rim.

The run-2 sentence "the kit look holds" was wrong for the hutch.

---

## 6. G6 — zone reflection cubes and `SetZoneReflection`

### 6.1 Capture (`FrontRoomsReflectionCapture.RunBatch`, recaptured 2026-10-03 11:57)

1. An edit-mode build of the shipped profile's map (seed 20261001, 25 chunks, 1,597 lamps) through the map's own
   `BuildForCapture`. That path runs `FrontRoomsLook.ApplyAmbient`, which now uses main's F5 values.
2. **Every built pane (11) is drawn as the game will draw it:** `Glass_Window`, 6 mm, no shadow, via a capture copy
   with no floor (`SwapPanes`, `:147`). The old cyan `TransparentGlass` is no longer baked in. The RT weight and the
   nominal intensity are forced to 0 during the bake (`:100-102`).
3. One cell per zone type (`logs/reflection_capture_fixpass.txt`): Level0 (1, 4), Office (−5, 21), Tall (14, −15),
   DeadLamp (1, 6). The lamps are lit with the map's own `TickFixtures`.
4. A 256 px HDR cube is baked at eye height (1.62 m) with a Custom `ReflectionProbe` through
   `Lightmapping.BakeReflectionProbe`. First bounce only, no post.
5. Importer: Cube, specular convolution, 9 mips, BC6H 256 on desktop, RGB9e5 128 on WebGL.
6. **`capture_manifest.txt`** (`:210`) records:
   - the seed and the cells;
   - the pane material and count (`Glass_Window x11`);
   - the lens materials (**`Map / Level 0 lens, Troffer_Lens`**: Level 0 still has the map's own lens);
   - the wallpaper print state (static paper);
   - the ambient and fog constants;
   - the Surface shader md5;
   - the level profile.

   `WarnIfStale()` (`:246`, menu *Check zone reflection cubes are current*, also run at the end of the glass setup)
   warns when the ambient, fog, surface shader or profile differ.

**Recapture after:** the map's switch to `Glass_Window`; the Level 0 lens change to `Troffer_Lens` (F10, ×1.5 picked
by the visual chat); any wallpaper print change; any `FrontRoomsLook` ambient change.

**Caveat: frozen print (rewritten).** Each cube holds a frozen copy of the wallpaper print, and the print will animate
later.
- On window glass the frozen print shows at the pane's reflectance: about 8 % face-on, but **10–40 % at grazing
  angles** (two-surface Fresnel: 0.109 at 50°, 0.157 at 60°, 0.385 at 75°). The earlier "≈ 4 % face-on" understated
  it.
- On walls and floors the world reflection is capped at 0.5 linear. At that level the cube is barely visible in
  frames 01/34/35/37 (`20_verification.md` §5).
- A live print in glass needs a live reflection source: the RT path (G14, Mac) or the planar (G15).
- Everywhere else, either accept the frozen print or capture one cube per print state and crossfade them like the
  zones (`FrontRoomsZoneReflection.Draw` already lerps two cubes).

### 6.2 Runtime (`FrontRoomsZoneReflection.cs`)

- **How it reaches URP 17.** `RenderSettings.defaultReflectionMode = Custom` and `customReflectionTexture = cube`.
  - With probe blending off (today), renderers without a probe sample it as `unity_SpecCube0`.
  - With blending on (G7), URP binds it per camera as `_GlossyEnvironmentCubeMap`.
- **Intensities are linear** fractions of the captured light:
  - `ZoneLinear` {Level0 .5, Office .5, Tall .45, DeadLamp .5}, clamped to `MaxLinear` .5 (`:45-48`);
  - the slider gets `LinearToGamma(linear)` (`:131`); fades lerp in linear;
  - raise toward 1.0 linear once G7 box probes land, for the zones whose dead-lamp handling is right.
- **`_FR_ZoneReflNominal`** (global): the steady linear intensity.
  - Blend mode: the current value.
  - Dip mode: the *from* value before the midpoint and the *to* value after (`:240, 252`).
  - Reset to 0 on a scene load.
  - The glass reflection floor divides by it (shader `:258-263`), so glass dips with the world instead of cancelling
    the dip.
  - With G7 probes, give every room probe `intensity = LinearToGamma(zone linear)`. Probe intensity is applied in
    gamma (measured, §6.3), so the ratio stays valid.
- **Blend mode (desktop).** One HDR cube render texture (RGB111110Float, 256 px, 9 mips, ~2.1 MB) stays bound. A zone
  change redraws it each frame as lerp(from, to) for every face and mip (54 draws per frame, only while fading). A
  retarget mid-fade freezes the current mix first, so nothing pops.
- **Dip mode (WebGL, or no HDR render texture).** Intensity fades to 0, the cube is swapped at the midpoint, and
  intensity fades back.
- **Robustness (fix pass):**
  - `ResetStatics` at `SubsystemRegistration` (`:90`): releases old RTs, `FlipY = !SystemInfo.graphicsUVStartsAtTop`.
  - A Single scene load (R restart, `FrontRooms3DGame.cs:1567` in main) forgets the zone, so the run-start call snaps.
  - `Application.quitting → Release` (fires when Play mode stops in the editor), so no HideAndDontSave RTs or driver
    leak per Play session.
  - A same-zone call re-applies if something rewrote `RenderSettings` (`:140-145`).
  - The driver steps by `Time.captureDeltaTime` when a capture step is set, otherwise by unscaled time
    (`FrontRoomsZoneReflectionDriver.cs:13`). So a fade finishes while paused (`timeScale` 0) and lasts its real
    length in recordings.
- **API.**
  - `SetZoneReflection(zone, blendSeconds = .5f)`: the first call, any call outside Play mode, and `blendSeconds` 0
    snap. A same-zone call is a no-op.
  - `SetImmediate(zone)` always snaps.
  - `ApplyAmbient()` re-applies the zone (`FrontRoomsLook.cs:60`), so the map can call the two in either order.

### 6.3 Play-mode proof (`FrontRoomsGlassVerification.RunReflectionTestBatch`, Metal, M3 Max; `logs/reflection_test_fixpass.txt`, 12:09)

| Check | Result |
|---|---|
| Before any call | Skybox, `defaultTexture` = Default-Skybox-Cubemap |
| First `SetZoneReflection(Level0)` | Custom, our render texture; 11.6 /255 from the sky |
| Same zone again | no fade |
| 0.5 s fade to Office | t = 0 identical to A (0.00 /255); t = 0.25 s between A and B; finished after 40 frames |
| Render-texture path vs the plain cube asset | **0.06 /255, max 1** (orientation and HDR decode correct on Metal; FlipY true gives 5.67) |
| Slider halved 0.735 → 0.368 | ratio 0.230 / 0.223 (predicted gamma 0.223) |
| Linear intensities | Level0: slider 0.7354 = LinearToGamma(0.5), decode x 0.500, nominal 0.500; Tall: decode 0.450 |
| `ApplyAmbient()` after | identical (0.00 /255) |
| Dip mode | mid-dip mirror (0.031, 0.023, 0.008) vs end (0.226, 0.190, 0.092); dip end vs the RT path 0.04 /255 |
| **Glass in the dip** (a `FrontRooms/Glass` sphere, `_ReflectionMin` 1) | dip / steady: world mirror 0.017, **glass 0.037**: glass now dips with the world. The old divisor would have scaled the glass by min/decode ≈ 0.6/0.002 = 300× at mid-dip, keeping the reflection while the cube swapped (computed from the old code, not re-measured) |
| Retarget Office → Tall mid-fade | frame before = after (0.00); end = Tall snapped (0.00) |
| Fade with `timeScale` 0 | finishes, both with the capture step (45 frames) and in wall time (0.70 s, no capture step) |
| Same zone after `RenderSettings` was rewritten | back to Custom |
| R restart (scene-load handler invoked) | `Active` false, nominal 0; the next call snaps |
| `ReflectionProbe.intensity` 1 → 0.5 (Custom probe, Office cube) | ratio **0.216 / 0.214: gamma**, like the slider |

---

## 7. WebGL notes

- **Per pane.** One transparent draw (SRP Batcher), no depth, shadow or motion pass. Fragment cost: URP lighting +
  3 grime samples + 1 normal + 1 extra cube sample (the floor). The heaviest WebGL variant uses 7 of 16 texture
  units. Cost scales with pixels covered.
- **Variants.**
  - One local keyword (`_FR_GLASS_GRIME`, `shader_feature_local_fragment`).
  - The three probe keywords, as URP Lit. URP strips them per asset. Note that `FrontRooms_URP.asset:56` has the atlas
    on, so `_REFLECTION_PROBE_ATLAS` variants may ship.
  - `_FR_GLASS_RT`, stripped from WebGL builds by `FrontRoomsGlassRTStripper`.
  - The removed SH pragma takes out up to 3× vertex variants on tiers whose URP asset picks vertex/mixed SH.
  - **The real variant count in a WebGL build was not measured** (no WebGL build; the clone has one URP asset).
    Run it once with URP's shader variant log on, before the WG budget is signed.
- **Memory.**
  - WebGL: cubes 4 × 128 px RGB9e5 ≈ 4 × 0.52 MB; grime ≈ 0.44 MB.
  - Desktop: cubes 4 × 0.52 MB BC6H + the 2.1 MB blend texture.
- **Web reflections:** one global cube per zone type, crossfaded by the dip. A Relay-only overlay (G16) is the one live
  reflection the Web may get. It needs the stripper removed (`11_reflections_and_raytracing.md` §4.2).
- **Not verified:** the dip and blend paths in a browser, RGB9e5 in Chrome/Safari, `FlipY` on D3D12/Vulkan (run
  `RunReflectionTestBatch` there before the High tier ships on Windows), and frame time.

---

## 8. Contracts for other chats

### 8.1 G14 / planar track: the ONE reflection input

The shader side is done and proven (`20_verification.md` §8.2). The writer must follow this contract:

1. **Texture `_FR_GlassRTReflection`** (global; RGBAHalf or RGBAFloat, screen-sized):
   - **RGB** = the *unweighted* reflected radiance in linear HDR scene units, the same units as the camera colour
     buffer before post. Apply **no Fresnel and no strength**: the shader applies `_PaneF0` + URP's Fresnel and fades
     the input under smudges, dust, cracks and on the edge faces. With the thin-glass double image, RGB = the mean of
     the front- and back-surface reflections.
   - **A:**
     - 0 = none;
     - 0 < A ≤ 1 = coverage. This is the Relay-only overlay: the hook is then exactly lerp(cube, rt, A) inside the
       Fresnel;
     - A > 1 = 1 + the linear eye depth (m) of the glass surface the texel was traced for, coverage 1. A pane at
       another depth (more than 0.02 m + 1 % of the depth away) keeps its own cube reflection. This stops the near
       pane's reflection leaking onto a far pane, a vending front or a bottle seen through it.
2. **`_FR_GlassRTWeight`** (global float): 0 does nothing. **And the global keyword `_FR_GLASS_RT`:**
   - Enable it only while driving the glass. With it off, the output is bit-identical to the shader without the hook.
     With it on and weight 0, only the last float bits differ (≤ 3.8e-6).
   - Write the texture in a RenderGraph pass **at or before `RenderPassEvent.BeforeRenderingTransparents`**, inside
     that camera's graph, with `SetGlobalTexture` / `SetGlobalFloat` and the keyword (for example
     `GlobalKeyword.Create("_FR_GLASS_RT")` with the command buffer's `SetKeyword`).
   - In `RenderPipelineManager.beginCameraRendering`, set weight 0 and the keyword off for every camera except the
     ready main game camera, and also when the plugin is missing or not ready. This covers probe bakes, the
     reflection capture tool (which already forces weight 0), planar cameras and scene views.
   - Today's prototype must stop:
     - baking `mix(0.15, 1, fresnel) * strength` into the colour (`FrontRoomsMetalGlassRT.mm:300` in main);
     - writing alpha 1 (`:301`);
     - compositing after post (`FrontRoomsMetalGlassRTRendererFeature.cs:17`).
3. **Receivers only:** `_RTReceive` 1 on `Glass_Window`; 0 on `Prop_Glass`, `Prop_BottleBlue`, `Glass_ShardClear`
   (`FrontRoomsGlassSetup.cs:104`, and `Reset` at `:171` sets 0).
4. **Registration.** The shader samples at `GetNormalizedScreenSpaceUV(positionCS)`. The proof wrote a 2×2 texture with
   only the top-left texel covered, and only the screen's top-left quadrant changed, at render scale 1 and 0.75.
   **Acceptance test for the RT writer:** a screen-UV gradient written by the plugin must read back identically at the
   four corners, at render scale 1 and 0.75. This checks G14's suspected vertical flip and its FOV-as-radians defect.
5. **WebGL:** the stripper removes the keyword. When G16 (the Web overlay) lands, delete the stripper.

### 8.2 Map chat (G9 / W1.4)

- **Panes:**
  - Load `Resources.Load<Material>("Surfaces/Glass_Window")` (`FrontRoomsGlassPane.Window`) for panes and
    `Surfaces/Glass_Edge` for any separate edge or stop-side glass faces. Keep `TransparentGlass` only as a fallback.
  - The pane is a scaled unit-cube mesh, **6 mm** thick (`GlassThickness` .03 → .006), **shadows off**, and Y rotation
    only. A mesh modelled in metres sets `_PaneSize` on a material instance.
  - W1.4's 1.391 × 1.642 slab as a child of the pane works if the slab itself is the scaled unit cube.
  - Keep `FrontRoomsMetalGlassTarget` on whatever renderer is the visible glass.
- **Glazing stop / frame** (audit §4.1): 18 × 12 mm stops on both faces and four sides. This is now the largest
  missing cue (`20_verification.md` §9).
- **The smear band** assumes the floor is at world y = `_FloorY` (0).
- **Calls:**
  - `FrontRoomsLook.ApplyAmbient()` (already in Awake in main; it changes nothing on screen since F5) and
    `FrontRoomsLook.SetZoneReflection(zone)` at run start, then on zone and dead-lamp changes. Every frame is fine.
  - Pick DeadLamp from the local light level, not from one cell's lamp (`20_verification.md` §6).
- **Cracks:** `FrontRoomsGlassPane.SetCrack` / `ImpactUV` until GD3's meshes land.

### 8.3 Not done

| Item | Why / where |
|---|---|
| G5 `Kit_InteriorWindow` on the glass shader | queued |
| G7 per-room probes | desktop tier. The shader is ready (probe keywords). Probes need `intensity = LinearToGamma(zone linear)`. Enable blending + box projection in the desktop URP asset |
| G8 fracture variants, G12 staged fracture | in GD3 / superseded by GD3 |
| Title-stream cubes (Lobby, Shift, Office, Run, Exit) | the RoomStream hook uses Level0 until they exist |
| A WebGL build: variant count, RGB9e5 in Chrome/Safari | not run here |
| `FlipY` on D3D12/Vulkan | not run (no Windows machine) |
| Promotion to `Frontrooms3D/` | not done by this workflow (`30_final.md`) |

---

## 9. Sources and method

- **Package source** (clone `Library/PackageCache`, URP `@37e0d4fc2503`):
  - `UniversalRenderPipeline.cs:2085-2098`
  - `ShaderLibrary/GlobalIllumination.hlsl:34-40, 286-330, 420-455`
  - `Shaders/Lit.shader:142-144`
  - `Editor/ShaderScriptableStripper.cs:578-585`
  - `Runtime/Data/UniversalRenderPipelineAsset.cs:1418-1425`
  - `ShaderLibrary/BRDF.hlsl:65-72`
- **Offline Unity 6000.3 docs** (`/Applications/Unity/Hub/Editor/6000.3.10f1/Documentation/en/`):
  - `ScriptReference/Rendering.RayTracingAccelerationStructure.html` (re-read 2026-10-03)
  - `Manual/low-level-native-plugin-rendering-extensions.html` (read 2026-10-03)
  - earlier: `RenderSettings-customReflection.html`, `Lightmapping.BakeReflectionProbe.html`,
    `ReflectionProbe-defaultTexture*.html`, `TextureImporter.GetAutomaticFormat.html`,
    `Manual/texture-formats-reference.html`, `Manual/webgl-texture-compression.html`
- **Main, read-only** (12:0x):
  - `Assets/Scripts/Rendering/FrontRoomsLook.cs` (md5 2e3ae3bb…, 10:23)
  - `NativePlugin/FrontRoomsMetalGlassRT.mm:290-301`
  - `Assets/Scripts/Rendering/FrontRoomsMetalGlassRTRendererFeature.cs:17`
  - `FrontRooms3DGame.cs:280-296, 1567`
  - `FrontRoomsMapWorld.cs` (diff against the clone)
- **Web:** none this pass.
- **Runs this pass** (clone; logs copied to `logs/`):
  - hook proof ×3 (uniform branch, keyword vs today's shader, keyword vs the final shader);
  - glass setup ×4;
  - compile check;
  - cube capture;
  - G10 run 3 ×2 (the first had a contaminated BEFORE and was discarded);
  - reflection play-mode test ×2 (the first exposed the unscaled-time driver racing the test's capture step; fixed).
