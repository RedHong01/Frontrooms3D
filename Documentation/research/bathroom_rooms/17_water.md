# Bathroom rooms · 17 · Water for the pool family: reflection, ripples, colour, and ray-traced water

Date: 2026-10-31. Line: bathroom (浴室房间调研), **sub-session of the K session (line bathroom)**, Cursor on Red's API key. Research only: no code, no Unity, no other project file edited.

Red, 2026-10-31 (K chat): "我需要你开一个新的子对话，专门用于研究，这个泳池房间的水体应该是如何（让这个水体相关的research利用对应对话的全部context，水体的反射，波动的感觉，以及水的颜色-在这部分水的颜色上其实和我刚刚要求你做的research有所呼应。因为水和灯光的颜色很大程度上会影响玩家对于这个房间的印象，因此我也需要你进行research电影级规格的3A游戏渲染下带光追，水体要如何制作。）"

**Reads with.** "10", "15", "16", "30" are the files with those numbers in this folder: 10 §3 (water logic and the technique survey; not redone here), 15 §2 and §4 (the canon and its 1990 grammar), 16 (the palette, written in parallel), 30 §2 (P0, P4, P4b). Also `research/zone_lamps/20_design.md` §3 and `10_pool.md` §5 (the stand-in pool and how light behaves in a pool hall), and the G14 glass-RT documents under `research/glass/`. All paths are under `Frontrooms3D/Documentation/`.

**Labels.** [EST] marks my estimate or my own computation. A number without it comes from the cited source. Sources W1–W41 are in §1 (W36–W41 added 2026-11-04 for §10).

**How the colour numbers were made [EST].**
- Pope & Fry's pure-water absorption (W27) is multiplied by blackbody lamp spectra: 2800 K for the wet-niche lamps, 4200 K for metal halide, 6500 K for daylight fluorescent.
- The result goes through the CIE 1931 colour-matching functions (the W28 fit) into linear sRGB, white-balanced to the lamp unless stated.
- Submerged tile: white, albedo 0.80, Lambertian, lit by a diffuse ceiling.
- Internal reflection at the water surface is computed for n = 1.333. It sends p = 0.475 of the upwelling diffuse light back down (W26's mechanism). Beer–Lambert absorption applies on every path.
- Hex values are sRGB targets for look-dev, not measurements. The script stayed in a scratch folder; §4.2 gives the formula.

---

## 0. Short answer

1. **The water is mostly a mirror of the ceiling.** From a standing player the water reflects 2–8 % within 3 m (a window onto white tile) and 34–40 % at 10 m. Wading in the deep end (eye 0.25 m above the water) it reflects 60 % at 3 m. So the ceiling and its lamps carry the pool's look, and the live reflection matters most when the player is in the water (§2.1).
2. **Lamp reflections become streaks through geometry, not a texture trick.** A streak's width/length equals the sine of the sight line's elevation (W20). In the pool level's 2.4 m rooms, a lamp 20–30 m away gives a streak 5–8 times longer than wide [EST]. The ripple slope sets the length. The player's own wake breaks the streaks up; standing still lets them re-form (§2.2).
3. **Water colour comes from absorption, and depth decides it.** The effective sRGB absorption is (0.29, 0.048, ≈0) per metre [EST, from W27]. Light trapped between the surface and the white floor roughly doubles the tint. Over white tile:
   - 0.12 m reads almost colourless, #D6E4E7;
   - 1.07 m reads pale turquoise, #8AD0E7;
   - 1.37 m reads #7ACBE7 [EST].
   **The canon's blue-green ankle water is not physical.** It needs an art call (Q1, §6.5).
4. **Warm 300 W wet-niche lamps look warm only at the lens.** They turn neutral after about 2 m of water and mint to aqua beyond 4 m. The basin's glow reads aqua only when the white balance follows those lamps (at night). Under the metal-halide balance it reads pale mint. This is an ask to 16's per-zone grade (§4.3).
5. **Ripples should be near-still, the same in every direction (isotropic), and dispersive (each wavelength moves at its own speed).**
   - A minimal constant capillary ripple (the canon), with no wind direction and no current. Wind-like motion indoors is exactly what a POOLS player complained about (W34).
   - A strong local response to the player and the Relay.
   - Rings spread at 0.2–0.45 m/s, with crests running through them.
   - Walking through ankle water leaves a 23.5° V; a body wading the basin leaves a 19.5° wake (the classic V behind a boat, the Kelvin wake) [EST, §3.1].
6. **Build the motion in three layers:**
   - a baked, looping ripple-normal sequence generated with the ocean-style FFT method (all tiers);
   - a few analytic dispersive rings (all tiers);
   - on desktop, a two-band simulated wave field in a window around the camera, like Sea of Thieves' 512 × 1024 deck simulation (about 0.2 ms on Xbox One, W11).

   Ripples smaller than a pixel must turn into roughness with distance (W23), because the game has no TAA (temporal anti-aliasing).
7. **Reflection stack:**
   - desktop Ultra: a planar reflection at ½ resolution with overscan, plus the box-projected probe and per-pixel lamp highlights from the ripple normal;
   - desktop High: a ⅓–¼ planar at half rate;
   - handheld and WebGL: the probe plus G11's Relay-only planar overlay.

   The overlay keeps **the Relay visible in the water on every platform**, matching the mirrored-ray rule of 10 §3.3. One planar camera serves a whole flooded level, because connected water has one surface height.
8. **No engine shortcut exists on our Unity version.** URP 6000.6 has no screen-space reflections (SSR): 6.7, still in beta, has them, with no backport (W5). URP also has no volumetric fog (W35), and Unity's ray tracing has no hardware path on Metal even in 6.7 (W6). So ray-traced water can only be an extension of the GlassRT plugin:
   - one mirror ray per water pixel;
   - +3–5 ms at 1440p on the M3 Max [EST, scaled from G14's measured cost];
   - Macs with 16 or more GPU cores only (M3/M4 Pro and Max), after G14 and its D1 (c) gate land.

   Recommended as a later step, not v1 (§5.3).
9. **Caustics need compact light sources; diffuse ceiling light makes almost none.**
   - Centimetre ripples focus about 0.1–0.3 m down, which suits the ankle-water floors; 10 cm ripples focus near 1.3 m, which suits the basin floor [EST].
   - Desktop caustics should come from the simulated surface; phones keep the flipbook.
   - Ray-traced caustics (0.5–2 ms on RTX GPUs, W29) add nothing for a box-shaped basin (§4.4).
10. **Estimate impact:** v1 water grows by about 21–33 PAH [EST]: P0 +1–3, P4 +4–6, P4b +4–6, plus a new desktop package P4c of 12–18. Ray-traced water would be a later P4d of 16–26 PAH (§6.3).

---

## 1. Sources

All read as text on 2026-10-31. Nothing was played. One data file was downloaded (W27).

| # | Source with URL | Date | What it gives | Status |
|---|---|---|---|---|
| W1 | Unity Manual 6000.6, "Render to a render texture outside the URP rendering loop", https://docs.unity.com/en-us/engine/6000.6/manual/cameras/urp/multiple/user-render-requests ; ScriptReference `RenderPipeline.SubmitRenderRequest` (6000.6) | page updated about 2026-10-23 | `UniversalRenderPipeline.SingleCameraRequest` + `SubmitRenderRequest` render one camera into a texture outside the loop; wait for frame end or `endContextRendering` before reading it | EXCERPT |
| W2 | Unity Discussions, "Two log error in planar relection with render graph mode on", https://discussions.unity.com/t/two-log-error-in-planar-relection-with-render-graph-mode-on/1624974 | 2025-04-03 | A URP planar ported to Render Graph logs errors every frame from `SubmitRenderRequest` called inside a camera callback | EXCERPT |
| W3 | GitHub, Unity-Technologies/BoatAttack issue #206, https://github.com/Unity-Technologies/BoatAttack/issues/206 | 2025-07-31; reply 2025-12-06 | On 6000.0.45f1 the suggested replacement throws "Recursive rendering is not supported in SRP"; a workaround calls the planar from the water's own callback | EXCERPT |
| W4 | Unity, "The path to a single Render Pipeline in Unity 7", https://unity.com/topics/the-path-to-a-single-render-pipeline-in-unity-7 | read 2026-10-31 ("6.7 is currently in beta") | URP in 6.7: SSR, GTAO, AgX, Surface Cache GI, UnifiedRayTracing, inline ray tracing in own shaders | EXCERPT |
| W5 | Unity Discussions, "Graphics updates with the Unity 6.7 Beta release", https://discussions.unity.com/t/graphics-updates-with-the-unity-6-7-beta-release-we-want-to-hear-your-feedback/1737140 ; "Preview of Screen Space Reflections for URP" p. 52, https://discussions.unity.com/t/preview-of-screen-space-reflections-for-urp/1721494/52 | 2026-09-21; 2026-09-01 | URP SSR marches screen space and falls back to a probe or the sky off screen; staff: "No plans to backport" | EXCERPT |
| W6 | Unity Discussions, "Introducing the UnifiedRayTracing API", https://discussions.unity.com/t/introducing-the-unifiedraytracing-api/1680597 | 2025 (API from 6000.3.0a) | Hardware path only where `RayTracingAccelerationStructure` works ("mostly GPUs that expose their HW raytracing acceleration through DX12"); compute fallback elsewhere; other APIs "a long-term undertaking" | EXCERPT |
| W7 | keijiro, "URP Metal Path Tracer", https://github.com/keijiro/metal-rt-test | read 2026-10-31 | Unity's RT APIs unsupported on Metal as of 6.3; a native Metal plugin traces inside URP through a Render Graph unsafe pass; M3 or later; opaque only | EXCERPT |
| W8 | Crest Water 5 docs: Water Renderer, https://docs.crest.waveharmonic.com/Components/System/WaterRenderer.html ; Reflections, https://docs.crest.waveharmonic.com/Manual/Appearance/Reflections.html | read 2026-10-31 | Planar reflections "for all render pipelines": layers, resolution, overscan ("especially when looking down"), refresh every N frames, oblique clip; dynamic waves with their own resolution and update rate | EXCERPT |
| W9 | Unity HDRP 17.7 manual: water simulation, capabilities, quality and performance, https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.7/manual/water-water-system-simulation.html and sibling pages | read 2026-10-31 | FFT bands, Pool = one ripple band; water receives ray-traced reflections but cannot appear in them; no motion vectors; MSAA incompatible; caustics resolution setting | EXCERPT |
| W10 | Epic, "Single Layer Water Shading Model" and "Lumen Global Illumination and Reflections" (UE 5.8), https://dev.epicgames.com/documentation/unreal-engine/single-layer-water-shading-model-in-unreal-engine , https://dev.epicgames.com/documentation/unreal-engine/lumen-global-illumination-and-reflections-in-unreal-engine | read 2026-10-31 | Opaque single-layer water with per-RGB absorption and scattering, PhaseG, colour scale behind water; Lumen reflections on it "forced to mirror" | EXCERPT |
| W11 | Ang, Catling, Ciardi, Kozin (Rare), "The Technical Art of Sea of Thieves", SIGGRAPH 2018 Talks, https://doi.org/10.1145/3214745.3214820 ; talk transcript, https://www.youtube.com/watch?v=y9BOz2dFZzs (text only) | 2018 | FFT ocean after Tessendorf; planar water reflections, no SSR; water "almost eight milliseconds" of a ~20 ms Xbox One frame at 900p when the view is all water; deck shallow-water sim on a 512 × 1024 target, two passes, "about 0.2 milliseconds" per ship | READ (abstract, transcript) |
| W12 | C. Gonzalez-Ochoa (Naughty Dog), "Rendering Rapids in Uncharted 4", SIGGRAPH 2016 Advances, https://advances.realtimerendering.com/s2016/ ; SideFX interview, https://www.sidefx.com/community/fx-adventures-in-uncharted-4-a-thiefs-end/ | 2016 | Water built from cheap procedural parts; flow maps advect wave-particle grids; flow in RG, wave height in B | EXCERPT |
| W13 | 80.lv, "How Naughty Dog Created the Immersive World of The Last of Us Part II", https://80.lv/articles/how-naughty-dog-created-the-immersive-world-of-the-last-of-us-part-ii | 2020-12-08 | Flow and surface shapes authored in Houdini; a material-mod buffer for wetness. No public talk on interactive ripples found | EXCERPT |
| W14 | M. Müller-Fischer, "Fast Water Simulation for Games Using Height Fields", GDC 2008, https://archive.org/stream/GDC2008Fischer/GDC2008-Fischer_djvu.txt | 2008 | Height-field wave equation with one speed c; stable only for Δt < h/c; object coupling | EXCERPT |
| W15 | C. Yuksel, D. House, J. Keyser, "Wave Particles", SIGGRAPH 2007, https://www.cemyuksel.com/research/waveparticles/waveparticles.pdf | 2007 | Lagrangian wave particles splatted into a height field; reflections off boundaries; unconditionally stable | EXCERPT |
| W16 | Wave Harmonic (Crest), "water-resources", https://github.com/wave-harmonic/water-resources | read 2026-10-31 | Reading list: Tessendorf 2001, iWave/eWave, dispersion kernels (Canabal 2016), wave packets (Jeschke and Wojtan 2017), water surface wavelets (2018) | EXCERPT |
| W17 | J. Tessendorf, "Simulating Ocean Water" (SIGGRAPH course notes) and "Interactive Water Surfaces" (Game Programming Gems 4) | 2001; 2004 | FFT ocean on the dispersion relation, loopable by quantised frequencies; iWave for interaction | NOT READ (method only, via W16, W11) |
| W18 | A. Vlachos (Valve), "Water Flow in Portal 2", SIGGRAPH 2010, https://alex.vlachos.com/graphics/Vlachos-SIGGRAPH10-WaterFlow.pdf | 2010 | Flow maps: two layers half a phase apart, noise against pulsing; flow map ~4 texels/m; +2 fetches and +21 ALU over two scrolled maps | EXCERPT |
| W19 | M. Mihelich, T. Tcheblokov, "Wakes, Explosions and Lighting: Interactive Water Simulation in 'Atlas'", GDC 2019, https://www.gdcvault.com/play/1025819/ | 2019 | Interactive wakes on a game ocean | NOT READ (listing only; no numbers used) |
| W20 | NOAA Physical Sciences Laboratory, "Glittering Light on Water", https://psl.noaa.gov/outreach/education/science/glitter/ | read 2026-10-31 | Glitter length = 4 × the maximum wave slope (high source); width/length = sine of the source elevation; streetlights and windows make it too | EXCERPT |
| W21 | D. Lynch, D. Dearborn, J. Lock, "Glitter and glints on water", Applied Optics 50(28) F39, https://doi.org/10.1364/AO.50.000F39 | 2011 | Most glitter is on capillary waves, not gravity waves; a low source gives a narrow vertical stripe | EXCERPT |
| W22 | C. Cox, W. Munk, "Measurement of the Roughness of the Sea Surface from Photographs of the Sun's Glitter", JOSA 44(11) 838, https://doi.org/10.1364/JOSA.44.000838 | 1954 | Slopes near Gaussian; surface slicks cut mean square slope by a factor of two or three | EXCERPT |
| W23 | E. Bruneton, F. Neyret, N. Holzschuch, "Real-time Realistic Ocean Lighting using Seamless Transitions from Geometry to BRDF", Computer Graphics Forum 29(2), https://hal.science/inria-00443630/ | 2010 | Each wavelength passes from geometry to normal map to BRDF slope variance as distance grows; anisotropic variances | EXCERPT |
| W24 | C. Braun, S. Smirnov, "Why is water blue?", J. Chem. Educ. 70(8) 612, full text https://www.webexhibits.org/causesofcolor/ref/Braun-Smirnov.html | 1993 | Water's colour is vibrational absorption in the red (a band tail from 760 nm, weaker bands at 660 and 605 nm) | EXCERPT |
| W25 | Wikipedia, "Color of water", https://en.wikipedia.org/wiki/Color_of_water | read 2026-10-31 | White-sided pools look cyan even indoors; deeper pools are more intense | EXCERPT |
| W26 | J. Lekner, M. Dorf, "Why some things are darker when wet", Applied Optics 27(7) 1278, https://doi.org/10.1364/AO.27.001278 | 1988 | Total internal reflection at the water–air interface returns diffuse light to the surface: A = (1 − R1) a / [1 − p (1 − a)] | EXCERPT |
| W27 | R. Pope, E. Fry, pure-water absorption 380–727.5 nm (Appl. Opt. 36, 8710), https://omlc.org/spectra/water/data/pope97.txt (S61 in `SOURCES.md`) | 1997 | The absorption spectrum used in §4 | READ. Downloaded to `<作业根>/Research/bathroom_rooms/water/pope97.txt`, 2,134 bytes |
| W28 | C. Wyman, P.-P. Sloan, P. Shirley, "Simple Analytic Approximations to the CIE XYZ Color Matching Functions", JCGT 2(2) | 2013 | The CMF fit used in the §4 computation | NOT READ (coefficients from memory) |
| W29 | X. Yang, Y. Ouyang, "Real-Time Ray Traced Caustics", Ray Tracing Gems II ch. 30, https://doi.org/10.1007/978-1-4842-7185-8_30 ; NVIDIA blog, "Generating Ray-Traced Caustic Effects in Unreal Engine 4, Part 2", https://developer.nvidia.com/blog/generating-ray-traced-caustic-effects-in-unreal-engine-4-part-2/ | 2021; 2020-12-08 | Ray-guided water caustics: the water is rasterised from the light, rays follow refracted or reflected directions, a caustics buffer is denoised; "less than 13% of frame time, typically 0.5–2 ms on NVIDIA RTX GPUs"; a pool test with a 1024 × 1024 map; the photon-difference mode suits pools | EXCERPT |
| W30 | H. Gruen, "Ray-Guided Volumetric Water Caustics in Single Scattering Media with DXR", Ray Tracing Gems ch. 14, https://doi.org/10.1007/978-1-4842-4427-2_14 ; NVIDIA blog on the GDC19 talk, https://developer.nvidia.com/blog/working-with-ray-traced-water-caustics-in-dxr | 2019 | Caustics mapping with DXR; volumetric caustics; why rasterisation cannot see from the water's many viewpoints | EXCERPT |
| W31 | Ray Tracing Gems II contents, https://rtg.realtimerendering.com/rtg2/index.html | 2021 | Ch. 29 "Hybrid Ray Traced and Image-Space Refractions" (Parhizgar, Svensson) | EXCERPT |
| W32 | Alan Wake 2: GDC 2024 talk listing (Kandar, Sjöholm), https://www.nvidia.com/en-us/on-demand/session/gdc24-gdc1003/ ; Digital Foundry, https://www.digitalfoundry.net/articles/digitalfoundry-2023-alan-wake-2-rt-deep-dive ; Tom's Hardware, https://www.tomshardware.com/features/alan-wake-2-will-punish-your-gpu | 2024; 2023; 2023-10-31 | Path tracing with OMM, SER, DLSS Ray Reconstruction; RT High = three bounces; an RTX 3080 falls to about 30 fps with path tracing + RR; screen-space lake reflections vanish when the source leaves the screen | EXCERPT |
| W33 | Forza Support, "NVIDIA DLSS, AMD FSR and DirectX Ray Tracing Improvements in Forza Horizon 5", https://support.forza.net/hc/en-us/articles/10944080013843 | 2022-11-09 | Ray-traced reflections on the player car only (Ultra, Extreme), half or full resolution by preset; nothing on water | EXCERPT |
| W34 | POOLS: Tensori (Antti Järvinen) on Steam, https://steamcommunity.com/app/2663530/discussions/0/6361972321502338713/ ; EntreMandos interview, https://entremandos.com/entrevistamos-a-antti-jarvinen-desarrollador-de-pools-y-ceo-de-tensori/ ; PlayStation Blog, https://blog.playstation.com/2025/11/21/pools-brings-liminal-terror-to-ps5-and-ps-vr2-on-nov-25/ | 2024-04-12; n.d.; 2025-11-21 | Unity, baked lighting, precomputed reflections, SSGI, no real-time RT. Players asked for water "perfectly still to begin with" that answers the player; the developer kept the motion because "moving water looks better". A custom water renderer for PS VR2 | READ (Steam); EXCERPT (others) |
| W35 | Unity Manual 6000.0, "Render pipeline feature comparison", https://docs.unity.com/en-us/engine/6000.0/manual/render-pipelines/choose-a-render-pipeline/feature-comparison ; roadmap card "Volumetric Fog" (URP, Under Consideration), https://unity.com/19-volumetric-fog ; Unity Discussions, "URP 6.3 still has no native volumetric fog", https://discussions.unity.com/t/urp-6-3-still-has-no-native-volumetric-fog/1735239 ; community URP volumetric light (Render Graph, no WebGL), https://github.com/fantastic-jam/Unity-URP-Volumetric-Light | read 2026-10-31; card 2025-04-02; 2026-08-31 | URP has only linear, exponential and exp² fog; volumetrics are HDRP only; community Render Graph passes exist | EXCERPT |
| W36 | POOLS on Steam (store page and its screenshots), https://store.steampowered.com/app/2663530/POOLS/ | looked at 2026-11-04 | 13 store frames: green-black body, sharp HDR mirror of windows and lamps broken into glint columns, a medium ripple that keeps moving, a found-footage camera (bloom, chromatic aberration, grain) | SEEN (browser pane, nothing downloaded) |
| W37 | Tensori, itch.io profile, https://itch.io/profile/tensori | comment "1 year ago" as read 2026-11-04 | "near photorealistic lighting and reflections in Unity, both in URP and HDRP", no Asset Store plugins, Unity 2021 to 6; keywords: baked lighting, light probes and adaptive probe volumes, reflection probes, post-processing | READ |
| W38 | Wikipedia, "Pools (video game)", https://en.wikipedia.org/wiki/Pools_(video_game) | read 2026-11-04 | Tensori, Unity; Windows and Linux 2024-04-26 (Bleeding Cool), App Store 2025-11-05/06 (Games Press), PS5 with PS VR2 2025-11-24 (PS Store), Switch 2026-02-26 (Nintendo Life) | READ |
| W39 | Game Developer (press release), "POOLS arriving on the App Store", https://www.gamedeveloper.com/press-release/pools-arriving-on-the-app-store | 2025-10-08 (Helsinki) | App Store and Mac App Store from 2025-11-06: iPhone, iPad (also Vision Pro), Mac; minimums iOS 13.0, macOS 13.4.1; nothing on rendering | READ |
| W40 | Red's screenshot of POOLS' store frame 2, sent with Red's message of 2026-11-04 (夸张结构调研与设计 chat) | 2026-11-04 | The reference for the glazed walls' shimmer and the water's mirror | SEEN |
| W41 | Wikimedia Commons, Fish74, "Innenansicht des Rheinhessen-Bades", https://commons.wikimedia.org/wiki/File:Innenansicht_des_Rheinhessen-Bades.jpg ; category "Interiors of indoor swimming pools in Germany" | photo 2006-07-10; CC BY-SA 4.0 | Still indoor water: a near-perfect mirror from about 2 m, objects meet their reflections at the line, only the first metre or two shows the tile | SEEN (browser pane, nothing downloaded) |

**Project documents used, not redone:**
- 10 §3.1, §3.3, §3.6–§3.8: rules, the reflection leak, other games, wave speeds, Fresnel, the starting absorption, tiers, the four spikes.
- `SOURCES.md` S30 and S61–S68: URP SSR preview, dispersion, Lagarde wet surfaces, A17 Pro/M3 RT, on-tile rendering, the planar pattern, SSPR, GPU Gems caustics and Evan Wallace's WebGL Water.
- `research/glass/11_reflections_and_raytracing.md` §0 and §2.2–§2.4b: probes, custom SSR, planar costs, the Relay-only overlay.
- `research/glass/rt/03_research.md` §2.3–§2.4, §3, §4: the M3 Max RT bench, hybrid RT in shipped games, the DX12 path.
- `research/glass/rt/11b_multibounce_research_bench.md` §2: receivers in shipped games, Control's costs.
- `research/glass/rt/40_for_red.md`, `30_final.md` §16, `NOTE_1022_dylib_not_in_player.md`: G14's measured cost, D1/D6/D2, the plugin's player status.

---

## 2. Reflection

### 2.1 A window near, a mirror far

Fresnel reflectance of water (n = 1.333) by horizontal distance from the eye [EST]:

| Eye height above the water | 1 m | 3 m | 6 m | 10 m | 20 m |
|---|---:|---:|---:|---:|---:|
| Club deck: eye 1.62 m + deck 0.2 m (10 §3.2) = 1.82 m | 2.1 % | 5.5 % | 17.8 % | 33.7 % | 57.1 % |
| Pool level, ankle water 0.12 m: 1.50 m | 2.2 % | 7.7 % | 23.3 % | 40.4 % | 62.9 % |
| Wading in the deep end: 0.25 m | 23.3 % | 59.8 % | 77.1 % | 85.5 % | 92.4 % |

These numbers agree with 10 §3.8 and zone_lamps `10_pool.md` §5.

What makes a pool read as real, by distance:
- **Near (0–3 m): the floor through the water.** The tile grid is visible and wobbles. In ankle water, refraction shifts the floor's image 13 cm at 3 m and 28 cm at 5 m [EST]. The shift moves with the ripples, which is the cheapest strong "this is water" cue.
- **Middle (2–6 m): the body colour** (§4).
- **Far (> 6 m): a mirror of the ceiling and its lamps.** The lamps appear as streaks (§2.2), and the far walls and pillars appear doubled.
- **Lamp colour is kept.** Water's Fresnel reflectance is spectrally flat, so streaks keep the lamp's colour; the water's body colour never tints a reflection.
- **Contact at the waterline.** A pillar or a leg must meet its reflection at the waterline. Planar and ray-traced reflections do this; a probe does not, so pillars standing in the level's water "float" with probes alone.

When wading, everything beyond 2 m is mostly reflection. That is also where the reflection leak of 10 §3.3 lives: the Relay seen in the water before it is seen directly.

### 2.2 Ripples, roughness and lamp streaks

- **Ripples smaller than a pixel become roughness.** Bruneton et al. (W23) move each wavelength from geometry to the normal map to a BRDF slope variance as the camera pulls away.
  - At 1440p with the game's 76° vertical field of view (`glass/rt/03_research.md` §2.3), one pixel spans 0.053°.
  - 10 m from a deck eye, a pixel covers about 5 cm of water along the view and 0.9 cm across it [EST].
  - From a wading eye 6 m out, it covers 13 cm along the view [EST].
  - So capillary ripples (λ < 2 cm) become sub-pixel along the view within a few metres. Their slope must go into an **anisotropic** roughness, stretched along the view, and that stretch is the streak.
- **The sparkle is capillary.** Most glitter comes from capillary waves, not gravity waves (W21). The ripple layer must reach millimetre-to-centimetre wavelengths, even if those wavelengths survive only as roughness.
- **Streak shape** (W20): width/length = sin(elevation of the sight line to the lamp's mirror image) [EST, computed for our rooms]:

| Room, eye, lamp | 5 m | 10 m | 20 m | 30 m |
|---|---:|---:|---:|---:|
| Pool hall: deck eye 1.82 m, ceiling 5.6 m above the water | 0.83 | 0.60 | 0.35 | 0.24 |
| Pool hall: wading eye 0.25 m, same ceiling | 0.76 | 0.50 | 0.28 | 0.19 |
| Pool level: eye 1.50 m, lamp 2.28 m above ankle water | 0.60 | 0.35 | 0.19 | 0.13 |

- **Streak length** is about 4 × the maximum slope (W20).
  - Calm water with slopes of 2–3° gives 8–12° streaks, about 10–16 % of the screen height [EST].
  - A disturbed surface (5–10°) gives broken columns 20–40° long.
  - So the low-ceilinged level turns distant lamps into long thin lines. The player's own wake breaks them, and they re-form when the player stops.
- **Without TAA this filtering is not optional.** FrontRooms runs MSAA 4× with no TAA (`glass/rt/03_research.md` §6.2). Full-strength ripple normals at a distance shimmer like noise.
  - The roughness has to rise with distance, from the variance of the normal map's mips (the Toksvig idea, as W23 in practice).
  - Check it with the shimmer test GD3c used: one-frame blinks over 24/255 (`research/glass/destruction/gd3c/30_fix1.md` row 6).

### 2.3 What our Unity (6000.6.3f1, URP 17.6) can do

| Layer | Status here | Cost evidence | Fit for pool water |
|---|---|---|---|
| Box-projected probes | Available (blending and box projection must be on, `glass/11_reflections_and_raytracing.md` §2.2) | No rendering; a per-pixel probe loop | Baseline everywhere. Static content: no Relay, no contact, lamp deaths only through a dead-lamp twin crossfade |
| Planar camera | Doable: `SingleCameraRequest` + `SubmitRenderRequest` outside the render loop (W1). Errors when called from camera callbacks under Render Graph (W2, W3). Crest ships planar reflections for all pipelines (W8) | G11 editor bench: about 2.5 ms fixed plus about 20 µs per renderer seen; a 12 m quarter-resolution planar took 4.3–9.0 ms (12–20 % of an editor frame). Draw distance and shadows are the levers; resolution barely matters (`glass/11_reflections_and_raytracing.md` §2.4) | Exact for flat water, shows the Relay, one camera per water height |
| SSR | Code present behind `URP_SCREEN_SPACE_REFLECTION`, unsupported (10 §3.8). Ships in 6.7 beta (W4, W5) | — | Poor for pools. Looking down, reflected rays go up and off screen to the ceiling, which is the main content (W32: lake reflections vanish when the source leaves the screen). Transparent surfaces get one reflection per pixel (G11 §2.3) |
| Hardware ray tracing | None through Unity on Metal, even in 6.7 (W6); the GlassRT plugin works (G14) | G14 added frame time at 1440p High, quiet runs: +3.17–3.52 ms for a pane 1.5 m away, +4.72–4.99 ms when the pane fills the frame (`glass/rt/40_for_red.md`) | Exact ripple distortion at any water height. Mac M3 and newer only; a later step (§5.3) |
| Per-pixel lamp specular | URP Lit, Forward+ | Part of shading | The lamps' glints and streaks on every tier, from the ripple normal |

**Planar recipe (for spike 1 and P4b):**
- **Submit from `LateUpdate`** after the camera has moved (W1), never from `beginCameraRendering` (W2, W3).
- **Renderer:** a second renderer without SSAO (10 §3.8).
- **Clipping:** an oblique near plane at the water plane, which "clips anything below the surface for free" (W8). Mirror with the reflection matrix and inverted culling (G11 §2.4).
- **Layers:** the hall's shells, lamps and lenses, and the Relay.
- **Settings:** shadows off; fog per zone; far clip at the room's length.
- **Overscan** 1.1–1.2, against edge artefacts "especially when looking down" (W8).
- **Roughness:** a mip chain, so the water blurs the reflection by its roughness.
- **High tier:** refresh every second frame (W8).

**One plane per water height.**
- The club's pool is one plane.
- A flooded level is also one plane wherever its water connects (water finds one level), so one camera covers every room in view.
- Basins at other heights fall back to probes.

### 2.4 What shipped titles do with water reflections

| Title or engine | Reflection on water | Numbers | Source |
|---|---|---|---|
| Sea of Thieves (UE4, 2018) | Planar water reflections, no SSR | Water ≈ 8 ms of ≈ 20 ms on Xbox One at 900p when the view is all water | W11 |
| Crest 5 (Unity asset) | Planar (above and below), plus probes or SSR | Overscan, refresh every N frames | W8 |
| Unity HDRP water | Probes, SSR, receives ray-traced reflections but never appears in them | Pool = one ripple band | W9 |
| UE5 Single Layer Water + Lumen | Lumen reflections forced to mirror; hardware RT for quality mirrors | — | W10; `glass/rt/03_research.md` §3 |
| Battlefield V (2018) | SSR first, rays where it fails; "More Rays on Water" | Ray budget 15–40 % of screen pixels by preset | `03_research.md` §3 |
| Control (2019) | One ray per glossy pixel; one mirror ray for transparent surfaces | Opaque RT reflections 3.2–3.3 ms at 1440p on an RTX 3090; transparent 0.4–1.5 ms | `11b_multibounce_research_bench.md` §2 |
| Cyberpunk 2077 PT, Alan Wake 2 | Path tracing with NVIDIA's DLSS Ray Reconstruction denoiser | AW2: RT High = 3 bounces; RTX 3080 ≈ 30 fps with PT + RR | W32; `03_research.md` §3 |
| Forza Horizon 5 | Ray-traced player car only; nothing on water | Half or full resolution by preset | W33 |
| POOLS (Unity, 2024) | Baked lighting and precomputed reflections, SSGI, no real-time RT | — | W34 |

Lessons:
1. Where ray tracing is absent, flat still water gets a planar reflection (Sea of Thieves, Crest, Unity's own BoatAttack).
2. Ray-traced titles treat water as a mirror receiver (Lumen forces a mirror, Control fires one mirror ray, BFV spends extra rays).
3. Every rough reflection leans on temporal denoisers, which this game does not have.
4. The one liminal-pool game we know of shipped baked reflections.

---

## 3. Ripples and the feel of the water

### 3.1 Real scales

Capillary–gravity dispersion with depth [EST, from the relation in S61]:

| Wavelength | Phase speed (crests) | Group speed (ring growth) | Period |
|---|---:|---:|---:|
| 5 mm | 0.31 m/s | 0.45 m/s | 0.016 s |
| 1 cm | 0.25 | 0.31 | 0.041 |
| 1.7 cm (slowest wave) | 0.23 | 0.23 | 0.074 |
| 3 cm | 0.25 | 0.19 | 0.12 |
| 10 cm | 0.40 | 0.21 | 0.25 |
| 30 cm | 0.68 | 0.35–0.37 | 0.44 |
| 1 m, basin / ankle | 1.25 / 1.00 | 0.63 / 0.85 | 0.80 / 1.00 |

- **Long waves** travel at √(gh): 1.08 m/s in 0.12 m of water, 3.24–3.67 m/s in 1.07–1.37 m. The basin sloshes along its length with a 10.4 s period and across it with 5.2 s (10 §3.7).
- **Inside a ring:** waves shorter than 1.7 cm run ahead of the ring, because their group is faster than their crests. Longer waves have crests that travel forward through the ring and die at its front. A scaled ring texture has neither behaviour, which is why it reads fake.
- **Amplitudes [EST; no measurement of indoor pool water found]:**
  - the constant ripple: 0.05–0.3 mm high at 1–5 cm wavelength (slopes 1–3°);
  - a step in ankle water: 2–10 mm rings;
  - a wading body: a 1–3 cm bow wave;
  - a plunge: a slosh of a few centimetres.
- **Wakes** [EST]: in ankle water every walking speed is faster than √(gh), so the wake is a V whose angle narrows with speed. In the basin a wader is slower than √(gh), so the wake is the 19.5° Kelvin pattern.

| Mover | Depth | Speed | Speed / √(gh) | Wake |
|---|---:|---:|---:|---|
| Player walk, Ankle (× 0.85) | 0.12 m | 2.72 m/s | 2.51 | V, half-angle 23.5° |
| Player run, Ankle | 0.12 | 4.68 | 4.31 | V 13.4° |
| Relay investigate, Ankle | 0.12 | 2.21 | 2.04 | V 29.4° |
| Relay chase, Ankle | 0.12 | 3.74 | 3.45 | V 16.9° |
| Player wade, basin (× 0.5) | 1.22 | 1.60 | 0.46 | Kelvin 19.5°, crosswise waves 1.64 m apart |
| Relay investigate, basin | 1.22 | 1.30 | 0.38 | Kelvin 19.5°, 1.08 m |
| Relay chase, basin | 1.22 | 2.20 | 0.64 | Kelvin 19.5°, 3.1 m |

A V's angle shows speed only to someone who sees it: wakes move at ≤ 1.1 m/s and stay inside the room, so they are never a radar (10 §3.7).

### 3.2 Models, and which to use

| Model | What it gives | Shipped or used by | For us |
|---|---|---|---|
| Two scrolled normal maps | Cheap; every wavelength at one speed, so it reads as sliding | The stand-in today | Replace |
| Flow maps (two phases + noise) | Directed flow without visible restarts | Valve, L4D2 and Portal 2 (W18); Uncharted 4 with wave particles (W12) | Only near a return inlet, if at all |
| Sum of trochoid (Gerstner) or sine wave trains with ω(k) | Dispersive, analytic, cheap on every tier | Bruneton 2010 (W23) | The rings and the slosh |
| FFT spectrum (Tessendorf) | Dispersive and statistically right; loops cleanly with quantised frequencies | Sea of Thieves (W11); HDRP Pool = one ripple band (W9); W17 | The base ripple layer, baked as a loop |
| Height-field wave equation | Interaction, reflection off walls; one speed per solver | Müller GDC 2008 (W14); iWave (W17); Crest dynamic waves (W8); SoT's deck sim, 512 × 1024 at ≈ 0.2 ms on Xbox One (W11) | The desktop wake window, two bands (§3.4) |
| Wave particles and packets | Cheap far-field waves, stable | Yuksel 2007 (W15); Uncharted (W12); Jeschke and Wojtan 2017 (W16) | Optional: a Relay wake that starts outside the window |
| Shallow-water flow | Water moving across a floor | SoT's deck (W11) | Not needed: the water level is static (10 §3.5) |

### 3.3 Avoiding the scrolling-texture look

1. **Dispersion.** Each wavelength moves at its own speed (§3.1), so crests change shape. Bake the FFT with capillary–gravity ω(k), not ω = √(gk).
2. **Isotropy.** No dominant direction: indoor pools have no wind (W34's complaint). Spread energy over all directions.
3. **Long loops.** Loop the baked sequence over ≥ 30 s with quantised frequencies (W17), or run it live on Ultra.
4. **Patchiness.** Real surfaces have calm patches: slicks cut mean square slope by a factor of two to three (W22). Modulate amplitude with a slow, faint, low-frequency mask.
5. **Distance to roughness, anisotropic** (§2.2).
6. **Scale lock.** Wavelengths in world metres, read against the 0.6 m tile grid, so ripples keep their size as the camera moves.
7. **Two time scales.** A fast capillary shimmer (periods 0.02–0.12 s) and a slow breathing slosh (5–10 s) after any disturbance.
8. **Honest rings.** Amplitude falls as 1/√r with damping, and rings reflect off basin walls. The simulation gives reflections for free; the ring list can mirror a source across the nearest wall [EST].

### 3.4 Wakes and disturbance: parameter sheet [EST]

| Band | Speed c | Cell | Window | Rate | Use |
|---|---|---|---|---|---|
| Ripple | 0.5 m/s. A non-dispersive solver makes a V of half-angle asin(c/U), so 0.5 m/s gives a wader at 1.6 m/s about 18°, near the true Kelvin 19.5°; rings grow at 0.5 m/s | 1.2–2.4 cm | 12 m square around the camera | 1 step per frame at 60 Hz (stable while Δt < cell / (c√2)) | Feet, legs, the Relay, drips; damping so a ring dies in 4–8 s |
| Long (ankle water only) | √(gh) = 1.08 m/s | 6 cm | 24 m | 1 step per frame | The real walking V (§3.1) and the slosh across a flooded room |
| Basin slosh | Analytic modes, 10.4 s and 5.2 s | — | — | — | After a plunge; fades over about 30–60 s |

- **Inputs:** the player's feet (Ankle) or torso (Wade) as moving discs; the Relay's legs; drips; the plunge.
- **Output:** height turned into a normal and a slope variance, added to the FFT layer.
- **Visual only.** No gameplay reads the simulation, and WebGL gets rings only (COMMON rule 5).
- **Cost** [EST]: a 1024² + 512² two-channel half-float pair is about 15 MB of memory traffic per frame, ≤ 0.2 ms on the M3 Max. For scale, SoT's 512 × 1024 took about 0.2 ms on a 2013 console GPU (W11).

### 3.5 Calm, then uneasy

- **What the canon and players ask for.** The canon wants "a minimal and constant rippling" (15 §2). POOLS players asked for water "perfectly still to begin with" that answers the player; the developer kept outdoor-like motion because still water "just doesn't look good" (W34, 2024-04-12).
- **Our split:** the constant ripple is capillary-scale only, sparkle with no visible waves. Anything larger is caused by someone.
- **Unease without lying (COMMON rule 12):**
  1. The water keeps moving after you stop: a plunge's slosh still rocks the reflections half a minute later.
  2. Drip rings from condensation (zone_lamps `10_pool.md` §5) appear at random times and never correlate with the Relay.
  3. Reflections re-form when you stop moving, which is when the mirrored Relay can appear (10 §3.3).
  4. In the level, the shimmer is identical in rooms nobody has entered: the water ripples by itself (canon).
- **Never:** directional wind waves, currents (except at a return inlet), ocean swell, or rings faster than 0.5 m/s, which read as a shock wave (10 §3.7).
- **Safety:**
  - Reduce Motion halves ring speed, slows the FFT layer by 50 %, and freezes caustics and thrown light (10 §3.8 plus the new layer).
  - Large-area brightness change from caustics and thrown light stays at or below 3 Hz [EST target], inside the RELAY §9.5 limits.

---

## 4. Colour, absorption, caustics, haze

### 4.1 Absorption

- **Mechanism.** Water absorbs red through overtones of its O–H vibrations (W24); blue passes.
- **Effective sRGB coefficients** from Pope & Fry (W27), per metre [EST]:

| Lamp | R | G | B |
|---|---:|---:|---:|
| Metal halide 4200 K, paths 0.25–5 m | 0.28–0.29 | 0.046–0.049 | ≈ 0 |
| Daylight 6500 K | 0.29–0.33 | 0.048–0.051 | ≈ 0 |

- **Use (0.29, 0.048, 0.006) per metre.** The small B value keeps deep water from ever brightening. This replaces 10 §3.8's starting value (0.25, 0.057, 0.011); the two are close.
- **Same model as the big engines.** UE's Single Layer Water uses per-RGB absorption, scattering and a phase term on an opaque single layer (W10); HDRP water belongs to the same family (W9).
- **Scattering:** clear pool water should scatter very little. A faint in-scattering term gives the "milky turquoise" of Anemoiapolis (10 §3.6) if wanted.

### 4.2 Why white tile under water turns turquoise, and how much

- **Trapping (W26's mechanism).** Upwelling diffuse light hits the water surface from below. p = 0.475 of it is reflected back down [EST], meets the floor again, and travels another long, slanted path. So the light seen from above has crossed the water several times.
- **Formula.** Per wavelength, the floor seen from above, relative to the dry tile:

  (1 − F̄) T_in · (1 − F_view) T_out / (n² (1 − ρ p T_trapped))

  T are Beer–Lambert transmissions along the entry, exit and trapped paths. The denominator lifts green and blue more than red, so the colour saturates about twice as fast as single-path Beer–Lambert.

Tile seen straight down, white-balanced, dry tile = 0.80 [EST]:

| Depth | Single-path Beer–Lambert | With trapping (use this) | Brightness vs dry |
|---|---|---|---:|
| 0.12 m | #E0E6E7 | #D6E4E7 | 0.83 |
| 1.07 m | #AEDDE7 | #8AD0E7 | 0.81 |
| 1.37 m | #A0DAE7 | #7ACBE7 | 0.81 |

Floor plus the mirrored white ceiling, by distance from the eye [EST; ceiling luminance 1, floor lit at 0.48 of it]:

| Case | 1 m | 3 m | 6 m | 10 m |
|---|---|---|---|---|
| Ankle 0.12 m, eye 1.50 m | #DCEAED | #DEEAED | #E3EBED | #E6ECED |
| Basin 1.07 m, deck eye 1.82 m | #94D7ED | #99D8ED | #B3DDED | #C8E2ED |
| Basin 1.37 m, deck eye | #85D2ED | #8CD3ED | #ABDAED | #C4E0ED |
| Basin 1.37 m, wading eye 0.25 m | #B5DCED | #DBE7ED | #E4EAED | #E8ECED |

How to read it:
1. **Physical ankle water is almost colourless:** a faint cyan with the tile at 83 % of its dry brightness. The refraction wobble (§2.1) and the reflections say "water", not the colour.
2. **The basin is pale turquoise and the deep end is more saturated.** The colour lives 1–6 m from a standing player; far water is ceiling-white with lamp streaks. When wading, the pool takes the colour of the ceiling.
3. **The stand-in reads grey for two reasons.** Its water is a dark teal at a constant 30 % alpha over white (16 §2). Its basin also slopes 1.1–2.4 m (`zone_lamps/20_design.md` §3), deeper than the decided 1.07–1.37 m, so the real pool will be paler than the stand-in suggests.
4. **The shader model follows from this:**
   - the water surface carries only reflection (alpha = Fresnel);
   - everything below `_WaterY` gets the trapping tint in its own shader: the analytic basin, `FrontRooms/Surface`, the Relay (10 §3.8).
   - On tile GPUs no opaque texture is needed (S65).

**The art call (Q1).** One choice is physical water everywhere: the turquoise lives in the club's basins, the waterline tile and the light. The other makes the level's water absorb more than real water, a quiet "wrong" against the club's honest water. Ankle water with absorption × 3 reads #BCDEE7, × 5 reads #A8D9E7, and × 9 reads #8AD0E7, as turquoise as the real 1.07 m basin [EST]. Recommendation: the club stays physical; the level uses × 3–5, with × 9 reserved for one or two deeper-looking rooms. It is a per-zone `_WaterAbsorbScale`.

### 4.3 Lamp colour through and off the water

- **Off the water:** reflections keep the lamp's colour (§2.1). Metal halide gives slightly green-white streaks; daylight fluorescent gives blue-white; cool-white fluorescent in the club gives neutral-cool.
- **Through the water:** the floor shows lamp colour × the trapping tint (§4.2). A cool lamp over turquoise gives the genre's cool look; a warm one would go olive. Keep warm light off the water's floor except at the niche lamps.
- **The 2800 K wet-niche lamp seen through water** (direct path) [EST]:

| White balance | At the lens | 1 m | 2 m | 4 m | 8 m |
|---|---|---|---|---|---|
| Metal halide 4200 K (overheads on) | #FFD28F | #FFE8A4 | #FEFFBB | #D2FFC7 | #8DFFDF |
| Daylight 6500 K | #FFB660 | #FFCA70 | #FFDF80 | #F2FF9D | #A3FFB0 |

- **The basin's glow from its own lamps** [EST model: a 9 × 18 × 1.22 m white box with a mean chord of 2.03 m; about 3 bounces before escaping]:
  - #9ED9FF (pale aqua) with the white balance at the lamps' own 2800 K, as at night;
  - #E2FFCD (pale mint) at 4200 K;
  - #FFFA9F (yellow-white) at 6500 K.
- **What this means:**
  - With the overheads on, the niche lamps are warm at the lens and minty in the water, so keep them subtle.
  - In the night or mains-dip state (16 §3, "the water is the light"), the grade's white balance must follow the niche lamps (about 2800–3200 K) for the pool to glow aqua. Ceiling caustics thrown by those lamps then read near-white, since that light crossed only 0.7–2 m of water.
  - The palette's "warm is allowed once" (16 §3) holds: warm only at the lens.

### 4.4 Caustics

**Physics** [EST, standard optics]:
- A caustic net is ripple curvature focusing a compact source. An extended source blurs it by roughly its angular size × depth, so the indirect ceiling wash of P1-A makes almost no floor caustics. That matches "faint with the overheads on" (zone_lamps `10_pool.md` §5).
- So the genre's moving light needs a few compact sources: the P2-A niche lamps in basins, and hidden compact lamps aimed at the level's water. The general wash stays sourceless.
- **Focal depth** f ≈ n / ((n − 1) a k²):

| Ripple | Max slope | Focus |
|---|---|---|
| λ 1–3 cm, 0.1–0.3 mm high | 3.6° | 0.10–0.30 m: crisp nets on ankle-water floors |
| λ 10 cm, 0.8 mm | 2.9° | 1.27 m: nets on the basin floor |
| λ 20 cm, 1.5 mm | 2.7° | 2.7 m: thrown light on the ceiling, softened by distance |

**Techniques:**

| Technique | Matches the actual ripples | Cost | Who |
|---|---|---|---|
| Scrolled or flipbook caustic texture | No; can read as sliding | ~0 | Stand-in (`zone_lamps/20_design.md` §3); 10 §3.8 phone/WebGL |
| Projected from the height field (refract a grid through the surface; brightness from the area change) | Yes, including footstep rings | Small [EST 0.1–0.3 ms desktop] | GPU Gems ch. 2 and Evan Wallace's WebGL Water (S68) |
| HDRP water caustics from a simulation band | Yes | Resolution setting | W9 |
| Ray-guided water caustics (caustic map from the light, traced refracted rays, denoise) | Yes, any geometry | < 13 % of a frame, 0.5–2 ms per light on RTX; 1024² map in a pool scene | W29, W30 |

For us:
- **Desktop Ultra:** projected caustics from the FFT and simulated surface, for up to two compact sources per room (floor nets) plus the niche lamps' thrown light on ceiling and walls.
- **High:** the same at half resolution and half rate.
- **Phone:** the flipbook.
- **WebGL:** the two scrolled layers.
- **Ray-traced caustics:** not needed; the basin is a box and the projection is exact enough.
- **Safety:** large-area caustic and thrown-light modulation comes only from waves of 20 cm and longer (periods ≥ 0.36 s, ≤ 2.8 Hz [EST]). The capillary band's fast nets stay small-scale and low-contrast (§3.5).

### 4.5 Haze and shafts

- **URP 6000.6 has no volumetric fog:** only linear, exponential and exp² fog; volumetrics are HDRP only (W35).
- **Use instead:**
  - the pool family's own exp² fog colour and density (16 §6 ask 1);
  - an analytic height-fog layer from the water to about 1.5 m, where chloramines and humidity sit (PL28 in `10_pool.md`), computed in the water and Surface shaders.
- **Visible beams** appear only faintly, under strong HID heads (zone_lamps `10_pool.md` §5). A community Render Graph volumetric pass exists (W35, no WebGL); keep it as an optional Ultra item after v1.

### 4.6 How the water fits 16's colour script

| Space (16 §3) | Water and light, from this study |
|---|---|
| Club basin, overheads on | Pale turquoise near (#94D7ED at 1 m), ceiling-white far; metal-halide streaks; niche lamps warm at the lens only |
| Club basin, night or mains dip | Aqua glow when the white balance follows the niche lamps; white thrown caustics on the ceiling; the water is the light |
| Turn (T1) and pool level | Ankle water: physical = near-white with wobble and long thin lamp lines; Q1 decides × 3–5 "wrong" turquoise; caustics only where a compact lamp hits the water |
| Dark pockets | Deep-teal darks come from the grade and fog (16), not from the water; the water there is mostly mirror |

---

## 5. Cinematic ray-traced water: practice and numbers

### 5.1 What high-end engines and titles trace, part by part

| Part | Practice | Sources |
|---|---|---|
| Reflection | Hybrid: screen space first, rays where it fails (BFV, Metro); rays everywhere glossy (Control, Cyberpunk); path tracing (Cyberpunk Overdrive, Alan Wake 2). Water is a mirror receiver: Lumen forces mirror on Single Layer Water; HDRP water receives but never appears in reflections | `03_research.md` §3; `11b` §2; W9, W10, W32 |
| Refraction | Mostly screen-space (Single Layer Water, HDRP's refractive transparency). Traced refraction is rare: PICA PICA glass, RTG II ch. 29 hybrid ray-traced and image-space refractions | W9, W10, W31; `03_research.md` §3 |
| Caustics | Ray-guided water caustics (NVIDIA's UE4 branch, "featured in a few games since 2019"); HDRP raster caustics from the simulation | W29, W30, W9 |
| Volume and translucency | Analytic absorption and scattering on one layer (Single Layer Water, HDRP); no real-time title path-traces the water volume, as far as found | W10, W9 |
| Denoising | NRD or DLSS Ray Reconstruction (NVIDIA only), Lumen's temporal filter; mirror reflections need none (Capcom, SEED) | W32; `03_research.md` §3 |
| Fallback | Probes or sky on a miss; screen space first | `03_research.md` §3; W5 |

### 5.2 What it costs (published)

- Control: 3.2–3.3 ms of opaque RT reflections at 1440p on an RTX 3090; transparent reflections 0.4–1.5 ms (`11b` §2).
- BFV: rays on 15–40 % of the screen's pixels by preset (`03_research.md` §3).
- Alan Wake 2: path tracing with Ray Reconstruction takes an RTX 3080 to about 30 fps (W32).
- Ray-traced water caustics: 0.5–2 ms per light, under 13 % of a frame, on RTX (W29).
- Sea of Thieves: water without RT took about 8 ms of a 20 ms Xbox One frame when the view was all water (W11). Water in a water game can take a large share of the frame.
- Red's M3 Max, measured for G14:
  - the bare trace: about 1.0–1.6 billion simple rays per second (`03_research.md` §2.3);
  - production shading of a held pane: about 1–1.4 ms at 1440p (§2.4 there);
  - the live plugin in the player: +3.2–3.5 ms (pane at 1.5 m) and +4.7–5.0 ms (pane filling the frame) at 1440p High in quiet runs, up to 13 ms in disturbed runs (`40_for_red.md`).

### 5.3 What is feasible for FrontRooms

- **The Mac: water as a GlassRT receiver.** G14 already traces mirror rays from a G-buffer of receiver pixels, shades hits with lamps and emissive lenses, and falls back to the zone cube. Its P1b design adds non-glass receivers through a `FrontRooms/Surface` hook (`research/glass/rt/11_secondary_reflections.md`). Water fits that design:
  1. a water prepass writes the rippled normal;
  2. one mirror ray per water pixel;
  3. no denoiser (mirror rays are noise-free);
  4. distance roughness as a blur by roughness and hit distance (BFV's image-filter idea; `11b` §2 lesson 3).
  - It brings exact ripple distortion of lamps and ceiling, any number of water heights, contact at the waterline, and the Relay without a planar camera.
  - **Cost [EST]:** water covers 40–60 % of the frame in the hall, so +3–5 ms at 1440p High on the M3 Max when quiet, more under load.
  - **Budget:** shared with glass, one ray budget per frame; pool rooms have few panes.
- **Gating:** Macs with 16 or more GPU cores, by D1 (c) (`40_for_red.md`); a D6-style lamp budget on hits.
- **Windows** follows G14's DX12 path (`03_research.md` §4).
- **iPhone and iPad:** A17 Pro and M-series iPads have hardware RT (S64), but not the budget [EST].
- **WebGL:** never.
- **Unity's own route does not help.** UnifiedRayTracing in 6.7 has no hardware path on Metal (W6). keijiro's URP Metal Path Tracer (W7) is a second proof that a native Metal plugin inside a Render Graph pass works.
- **Refraction and caustics by ray:**
  - The analytic basin (10 §3.8) already gives exact refraction of a box for free.
  - Traced refraction would add only legs, the Relay and ladders under water, for roughly the same cost again [EST]. Skip it; tint and darken below `_WaterY` in the Relay's shader instead (10 §3.8).
  - Ray-traced caustics: not needed (§4.4).
- **Risks specific to RT water:**
  - the plugin was missing from every player build until the universal-dylib package (NOTE_1022);
  - store builds wait for D1 (c);
  - no TAA, so mirror rays only;
  - the Surface hook is unbuilt;
  - the water's ray budget competes with glass.

**Verdict:** desktop v1 uses the planar. RT water is a later, gated Mac upgrade (P4d), after G14, D1 (c) and the Surface hook land, and after a timing on a pool-sized quad (10 §3.8).

---

## 6. FrontRooms per-tier recommendation, P4/P4b deltas, risks

### 6.1 Per tier

| Layer | Desktop Ultra (1440p reference) | Desktop High | Handheld (iPhone/iPad) | WebGL |
|---|---|---|---|---|
| Normals | Baked dispersive FFT loop 512² × 64 frames (or live 256²), 16 rings, two-band simulation, distance roughness | Baked loop, 8 rings, ripple-band simulation 512² | Baked loop 256² × 32, 4–6 rings | Baked loop 256² × 16–32, 4 rings |
| Reflection | Planar ½ resolution, overscan 1.15, mips; probe; lamp specular | Planar ⅓–¼, every 2nd frame; probe; lamp specular | Probe; lamp specular; Relay-only overlay | Probe; lamp specular; Relay-only overlay (G11 Web plan) |
| Refraction and colour | Analytic basin with the trapping LUT; ankle tint in Surface; opaque-texture refraction wobble for ankle water | Same | Analytic basin; ankle tint; no wobble (S65) | Same as handheld |
| Caustics | Projected from the surface, ≤ 2 compact sources per room, plus thrown light | Same at ½ resolution, ½ rate | Flipbook | Two scrolled layers |
| Wakes | Two-band simulation | Ripple band | Rings | Rings |
| Haze | Per-zone exp² fog + height fog over water | Same | Same | Same |
| Later | GlassRT water (Mac, 16+ GPU cores) replaces the planar on water; DXR on Windows | — | — | — |
| Budget [EST] | ≤ 4–5.5 ms (shading 1–2, planar 1.5–3, simulation + caustics ≤ 0.5); RT option +3–5 | ≤ 2.5–3.5 ms | ≤ 2 ms at device scale, set by P0 spike 4 | No new draw calls except the overlay camera; visible lights + probes ≤ 32 (G11 §3) |

Where the budgets come from:
- Sea of Thieves spent about 40 % of a frame on water when the view was all water (W11).
- Ray-traced caustics stay under 13 % of a frame (W29).
- G14's bars are 2.0 ms (High, 1.5 m, 1440p) and 5 ms (break shot).
- G11's planar took 12–20 % of an editor frame.
- The frame without RT runs 6.9–7.3 ms at 1440p on this Mac in quiet runs (`40_for_red.md`).

### 6.2 Why this stack

- The planar is the shipped answer for still water without RT (§2.4). It is exact for one plane, carries the Relay, and one camera covers a whole flooded level.
- Handheld and WebGL keep probes, but the Relay-only overlay keeps the horror beat "seen in the water first" on every platform. It also keeps the mirrored-ray rule of 10 §3.3 honest: the logic never says "seen" when nothing could be seen.
- The baked FFT loop costs almost nothing per frame on any tier and removes the scrolling look. The desktop simulation adds the decided "player and Relay disturb the water".
- Colour lives in the submerged shading, not in the water surface, which removes the stand-in's grey.

### 6.3 Changes to the estimate (30 §2) [EST]

| Package | 30 says | Change | New |
|---|---:|---|---:|
| P0 spikes | 9 | +1–3: spike 2 adds the ankle-floor tint and the trapping LUT; an optional spike 5 times GlassRT rays on a pool-sized quad, only if P4d is wanted | 10–12 |
| P4 water look | 20–28 | +4–6: baked dispersive FFT loop and distance roughness (+3–4); absorption LUT with trapping and a per-zone scale (+1); dispersive ring packets (+0–1) | 24–34 |
| P4b planar | 6–10 | +4–6: overscan and roughness mips (+1–2); Relay layer, and the Relay-only overlay for handheld, WebGL, Medium and Low (+3–4, reusing G11 §2.4b) | 10–16 |
| P4c desktop water (new) | — | Two-band wave window (6–9); surface-projected caustics and thrown light (4–6); opaque-texture refraction for ankle water (1–2); Reduce Motion and photosensitivity check (1) | 12–18 |
| P4d Mac RT water (later, new) | — | Water as a G14 receiver through the Surface hook (8–12); hit-shading parity and a shared glass-and-water budget (4–8); timing and art checks (4–6) | 16–26, not v1 |

- v1 total: about 129–183 → 150–216 PAH.
- The critical path does not move: P4 and P4c run on the greybox beside P5 (40–62), the largest package.
- P4c can slip to v1.1 without blocking anything.

### 6.4 Risks

| Risk | Effect | Mitigation |
|---|---|---|
| Planar camera under Render Graph errors (W2, W3) | No live reflection | Spike 1; submit outside the loop (W1); fallback probe + overlay; RT later |
| Shimmer on far water without TAA | Sparkling noise | Distance roughness (§2.2); GD3c shimmer test |
| Ankle water is physically colourless | The genre look is missing | Q1: per-zone absorption scale |
| Night glow turns mint or yellow | Wrong mood | Night white balance follows the niche lamps (16 grade) |
| Caustics need compact sources | No moving light under a sourceless wash | Lamp plan adds compact sources (A9) |
| Photosensitivity | Caustics and thrown light flicker at 2–25 Hz | Large-area change only from waves ≥ 20 cm (≤ 3 Hz); Reduce Motion freezes them |
| Handheld cost (large transparent area, many lamps) | Frame drops | Probe-first tier, P0 spike 4 |
| The Relay seen in the planar but not in probes | Platform difference in a cue | Relay-only overlay on probe tiers |
| Moving to Unity 6.7 for SSR | Engine churn mid-project | Not needed: SSR is a poor fit for pools (§2.3) |
| RT water inherits G14's open items | Delay | Keep it as a later P4d |

### 6.5 Questions for Red

- **Q1.** Ankle water colour: physical (near-white), or "wrong" turquoise in the level (absorption × 3–5, × 9 in one or two rooms)? Recommendation: × 3–5 in the level; the club stays physical.
- **Q2.** Desktop wake simulation and surface caustics (P4c) in v1, or v1.1? Recommendation: v1, per "desktop at the highest spec".
- **Q3.** Ray-traced water on the Mac as a later P4d after G14 lands: yes or no? Recommendation: yes, later, gated.
- **Q4.** Night state: let the grade's white balance follow the warm niche lamps so the pool glows aqua? Recommendation: yes; it belongs with 16's grade ask.

---

## 7. Asks for 游戏视觉 (owner of the shared rendering code)

1. **Water surface = reflection only** (alpha = Fresnel): planar or probe, plus lamp specular from the ripple normal. Retire the stand-in's constant 0.3 alpha and teal base. Owner: the water material and `FrontRooms/Surface`.
2. **Submerged shading** below `_WaterY` in `FrontRooms/Surface`, the analytic basin and the Relay's shader:
   - absorption (0.29, 0.048, 0.006) per metre;
   - a small depth LUT with the trapping term (§4.2);
   - the per-zone `_WaterAbsorbScale` (Q1);
   - caustics; the wet band and meniscus (10 §3.8).
3. **Ripple normals:** a baked looping FFT sequence.
   - Capillary–gravity dispersion, isotropic, λ from 5 mm to 30 cm, calm slopes 1–3°.
   - Normal variance stored for distance roughness (anisotropic along the view).
   - Disturbance raises amplitude locally.
4. **Rings:** dispersive packets growing at 0.2–0.5 m/s with crests running through them. 4–16 by tier, mirrored at basin walls.
5. **Desktop wave window** (P4c): two bands as in §3.4; visual only; basin slosh modes after a plunge.
6. **Planar camera** per §2.3: submitted from `LateUpdate`, ½ / ⅓–¼ resolution, overscan 1.1–1.2, oblique clip, layers including the Relay, roughness mips. One camera per water height, one for a whole connected level. Quality tiers go through 设置菜单与画质.
7. **Relay-only overlay** for probe tiers (G11 §2.4b), so the Relay shows in the water on every platform (with 追猎's mirrored ray, 30 P3).
8. **Caustics:** projected from the surface on desktop (floor nets from compact sources; thrown light on ceiling and walls from niche lamps); the flipbook on phones; scrolled layers on WebGL; ≤ 3 Hz large-area modulation; Reduce Motion freezes them.
9. **Lamp kit:** a few compact sources where moving light is wanted (P2-A niche lamps in basins; hidden compact lamps over the level's water). Keep the general wash diffuse (`zone_lamps/20_design.md` §3).
10. **Grade and fog** (with 16 §6): the pool family's cool fog; a height-fog layer over water; a night-state white balance that follows the niche lamps (about 2800–3200 K).
11. **Probes:** `Refl_Pool` with its dead-lamp twin, per-room probes in the level with blending, and a crossfade when a room's lamps die (30 P4).
12. **Later (P4d):** water as a G14 receiver through the Surface hook (`glass/rt/11_secondary_reflections.md`), mirror rays only, one ray budget shared with glass, Mac 16+ GPU cores, after a timing on a pool-sized quad.

---

## 8. Not covered

- **Not in scope:** underwater views and swimming (never in v1), splash and spray particles, foam, sound (10 §3.4).
- **Not measured** (all costs here are published numbers or [EST]): no in-engine verification, and no measurement on iPhone, iPad or WebGL.
- **Not researched:** Spider-Man and Cyberpunk water specifics beyond `03_research.md` §3; Apple's MetalFX denoiser for rough RT reflections (needs motion vectors, which the game does not have); how 6.7's SSR treats transparent water.
- **Not read:** the Atlas talk (W19); Tessendorf's papers (W17, method only); the CMF fit (W28, coefficients from memory).
- **Approximations in the colour model:** lamp spectra are blackbody stand-ins; real metal-halide and fluorescent spectra are spiky and shift the tint slightly. The cavity glow model of §4.3 is rough.
- **No measured amplitude of still indoor pool ripples was found;** §3.1 amplitudes are [EST].

---

## 9. 中文摘要（给 Red）

1. 水在远处主要是"天花板的镜子"：站着看，3 米内只反射 2–8%，看见的是池底白砖；10 米外反射 34–40%；涉水时眼睛离水面 0.25 米，3 米外就有 60% 是倒影。所以泳池的样子很大程度由天花板和灯决定。
2. 灯的倒影会自然拉成竖条：比例由视线仰角决定，2.4 米层高的泳池关里，20–30 米外的灯会变成 5–8 倍长的细线。玩家自己走动会把它们打碎，停下来又重新聚拢。
3. 水的颜色来自吸收，深度决定一切。0.12 米的脚踝水在白砖上几乎无色（#D6E4E7）；1.07 米淡青绿（#8AD0E7）；1.37 米更饱和（#7ACBE7）。光在水面和白池底之间来回反射，会让颜色比简单计算浓一倍。
4. 所以"后室泳池关"那种青绿色的浅水并不真实，需要你拍板（Q1）。建议会所的水保持真实，关卡里的水吸收 ×3–5，像"不对劲的水"，个别房间 ×9。
5. 300W 暖色水下灯只在灯罩处是暖的，2 米后变中性，4 米外偏薄荷或水青。夜里要让调色白平衡跟随这些暖灯，整池才会发出水青色的光（Q4，与 16 的调色方案相关）。
6. 波动：基本静止，只有极细的、各向同性的毛细波纹（符合正典"轻微而持续的波纹"），不要风向或水流（POOLS 玩家正是抱怨这一点）。玩家和 Relay 走过时才有明显涟漪和 V 形尾迹；涟漪扩散速度 0.2–0.45 米/秒。
7. 做法分三层：烘焙的循环 FFT 波纹（全平台）、少量解析涟漪环（全平台）、桌面端跟随镜头的波动模拟（参考 Sea of Thieves，约 0.2 毫秒）。远处的细波纹要转成粗糙度，否则没有 TAA 会闪烁。
8. 反射分档：桌面 Ultra 用半分辨率平面反射 + 反射探针 + 灯的高光；High 降分辨率、隔帧更新；手机和 WebGL 用探针 + 只渲染 Relay 的小平面反射，保证所有平台都能在水里先看见 Relay。
9. 焦散需要"集中的光源"，漫射天花板几乎不产生焦散。桌面端从真实水面算焦散，手机用序列帧。
10. 光追：Unity 在 Mac 上没有硬件光追（6.7 也没有），只能扩展现有的 GlassRT 插件，约 +3–5 毫秒，只给 16 核以上 GPU 的 Mac，建议作为后续一步（Q3），v1 先用平面反射。
11. 工作量：v1 水体约增加 21–33 PAH，其中新增桌面包 P4c 12–18（Q2 决定放 v1 还是 v1.1）；光追水体以后另计 16–26。
12. （2026-11-04 补充，§10）开光追后画质"掉一截"的原因：GlassRT 只追玻璃，水既不接收光追反射，也不在光追看到的场景里。开光追后玻璃变成精确的镜子，水还是一张探针图加冻结的 13° 大波纹，同一帧里差距就显出来了；光追本身没有降低任何东西。真实的水靠四样：水面上方有暗部、灯的倒影比瓷砖亮得多、约 2° 的细小波纹一直在动、远处是锐利的镜面而近处有折射。第 4 个结构 scene 里按 K 换水面方案、J 切换光追，左下角看数据。推荐：关卡的水用"亮青绿"，会所用真实清水，暗色镜面留给本来就暗的房间；游戏视觉的 pool_water 落地后做第二步。

---

## 10. Water that reads real at first-person scale, and what the ray tracing does to it (line water, 2026-11-04)

Line 水体仿真调研 (ledger key `water`): account G, session local_0d9085f6, started by 夸张结构调研与设计 on Red's words of 2026-11-04; continued on account B, session local_15593e17, after G's plan limit. Stage: research, code reading and a live preview in scene 4; options are described, not yet rendered. Main was read only except the preview landing (§10.4); no Unity ran on main; nothing was downloaded (POOLS' store frames were looked at in the browser pane, Red's own screenshot came with Red's message); no render was made (no workstation was free, and Red chose the live scene, §10.4). Reused, not redone: §1-§7 of this file (physics, colour, ripple scales, reflection stack, tiers, costs), `70_look_build.md` §5-§6 (游戏视觉's ripples and water surface, staged, not landed), `research/glass/rt/*` (GlassRT).

### 10.1 What the eye uses first

Ranked by what a standing or wading player reads first. "TODAY" is the structures scene's stand-in (`FrontRoomsPoolMaterials.WaterOf`); "pool_water" is 游戏视觉's water surface (`70` §6, with the Submerged stage still running).

| # | Cue | Why it says "water" | TODAY | pool_water |
|---|---|---|---|---|
| 1 | **Two images at once that move differently** | The floor (at ¾ of its depth, §2.1) and the mirrored room (at mirrored depth) slide against each other when the head moves. Nothing else in a room does that | No: one blurred probe image, no refraction | Yes on desktop High/Ultra (mirror + refraction); probe + lamps on phones, WebGL, Medium/Low |
| 2 | **A sharp mirror at grazing angles** | Calm water's roughness is about that of glass (α 0.012, `70` §6.3): the far water is the sharpest mirror in the room, sharper than any polished floor | Sharp probe, but box-projected from one point and broken by a 13° (mean) normal into blobs (§10.3) | Exact for the plane (planar ½ res on Ultra) |
| 3 | **Glints much brighter than what surrounds them** | A lamp's reflection keeps the lamp's radiance (§2.1); a real troffer lens is 10-40× its floor (`70` §6.8), so its streaks are the brightest thing in view and bloom. "Sparkling" (波光粼粼) is these glints moving | Lens drive 1.8 ≈ 1.25× its floor: glints are dull | Same lenses: correct, but dull (`70` §6.8) |
| 4 | **A body darker than the room** | Reflections read only against a darker body. In POOLS the body is near-black green (§10.2). In real white-tiled pools the body is pale under bright light and the reflections carry the far water | Constant teal veil at alpha 0.36 over the floor: neither dark nor clear | Physical: in the white, high-key level the floor through the water (171-209 on screen) outshines the reflection (23-150), so the water nearly vanishes (`70` §6.8) |
| 5 | **Motion that never stops** | Filters, return jets and people keep indoor water moving a little; POOLS' developer kept the motion because "moving water looks better" (W34). Frozen water reads as glass | Frozen: a static normal | Baked dispersive ripple loop, rings for the player and the Relay (`70` §5) |
| 6 | **Wobble of the floor's lines** | Lane lines and joints bend and swim with the ripple; the strongest near-field cue (§2.1) | None (alpha blend, no refraction) | Yes, with apparent depth (`70` §6.4) |
| 7 | **Contact at every waterline** | A pillar meets its reflection at the line; a wet band and a meniscus sit above it; under the line the pillar is tinted. Probes cannot do the first, so pillars float (§2.1) | None | Mirror contact on High/Ultra; wet band and meniscus in the Surface shader (Submerged stage) |
| 8 | **Colour that grows with depth** | Turquoise saturates with depth (§4.2); a step or a deep end shows depth by colour | One constant colour at every depth | Submerged stage: absorption with the trapping term, `absorbScale` per body (Q1) |
| 9 | **Light thrown by the water** | Caustic nets on the floor under compact lamps; moving light on walls and ceiling near the water (the glaze line's 波光粼粼 on tile lives here too) | A greybox cell texture scrolled on ceilings | Submerged stage: floor caustics from up to two compact sources, the niches' thrown light |
| 10 | **A camera that records it** | Bloom on glints, a little grain and chromatic aberration make the glints photographic (POOLS' found-footage look, §10.2) | The film look (post stack) | Same |

Under the water (Snell's window, total internal reflection, blue-green fog) stays out of scope as in §8.

**The water shows what is above it.** A real indoor pool photographed with the water still (W41, Rheinhessen-Bad, 2006) is a near-perfect mirror from about 2 m on: windows, columns, ducts and baffles reflect crisply and meet their objects at the waterline; only the first metre or two shows the tile, faintly teal. Where it mirrors the dark roof structure the water looks dark; where it mirrors windows, bright. So beyond a few metres the water's colour is the colour of the ceiling and the upper walls, and its sparkle is the lamps (cues 2-4). Two consequences for us:
- A uniformly bright white room above the water (white tile walls, a white ceiling lit by an indirect wash such as zone_lamps' P1-A, "水里没有灯的倒影") gives a uniform pale mirror: the water vanishes against white tile, however exact the mirror. Darks above the water (a darker ceiling, shadowed upper walls, unlit openings) and compact bright sources are what make it read.
- Under the water, a dark floor or lane (POOLS' teal-black mosaic) or more absorption gives the near field the contrast the mirror gives the far field.
Real daylight pools with white basins (W41's sister frames in the same Commons category) read the other way: bright turquoise bodies with sun glints. Both are real; they are two looks, not one right answer (§10.5).

### 10.2 POOLS (Red's reference)

**What it is.** Tensori (Finland), Unity, released 2024-04-26 on Steam; on the App Store for iPhone, iPad and Mac since 2025-11-06, PS5 / PS VR2 2025-11-24, Switch 2026-02-26 (W38, W39). The developer: near-photorealistic lighting and reflections "in Unity, both in URP and HDRP", no Asset Store plugins, and the keywords baked lighting, light probes and adaptive probe volumes, reflection probes, post-processing (W37); baked lighting and SSGI on PC, SSGI the most demanding setting; the water's motion kept on purpose and "really easy to adjust" (W34). No real-time ray tracing (W34). So the look is reachable on phones: it rests on art direction and baked light, not on RT.

**What its frames show** (13 store frames looked at 2026-11-04 in the browser pane, W36; Red's screenshot is frame 2, W40):
- **The body is dark.** Green-black water over a dark teal mosaic; the floor shows only within about a metre of the eye, already darkened. Against it, every reflection reads.
- **The mirror is sharp and HDR.** Arched windows and lamps reflect as white shapes broken into columns of glints (frames 3, 5, 7, 13); white walls reflect as soft bright bands; dark objects (the slide's pipe, pillars) reflect as dark masses that meet the object at the waterline (frame 7).
- **The ripple is medium-scale and always moving.** Highlight contours a few tens of centimetres across, irregular, never a visible repeat; near the camera the floor's mosaic wobbles under it.
- **The camera is a found-footage camera.** Strong bloom on glints, chromatic aberration at the frame's edge, grain, a warm green grade.
- **Players' complaints point the other way too:** "We're indoors, yet the water moves as if we were outdoors" (W34): the motion must stay small and directionless (§3, isotropic, no wind).

**Lesson.** POOLS reads real because of contrast (dark body, HDR glints), sharpness and motion, then the camera. Two of those are lighting and art direction, not water technique: the glints need lamps far brighter than their walls, and the dark body is a choice of floor, absorption and light under the water.

### 10.3 Today's water in the structures scene, from the code

- **Material:** URP Lit, transparent premultiplied, `_BaseColor` (0.16, 0.60, 0.62, 0.36) level / (0.30, 0.68, 0.70, 0.22) club / (0.10, 0.45, 0.50, 0.36) dark, smoothness 0.95, `_BumpScale` 0.5 / 0.35 (`FrontRoomsPoolMaterials.cs` WaterOf).
- **The ripple is frozen and about ten times too steep.** The runtime normal is a sum of four sines, one repeat per 2 m (wavelengths 0.63, 0.45, 0.34, 0.28 m), never animated. Its slopes, computed from the generator's constants (8-bit packing and `_BumpScale` included): level and dark water mean 13.4°, p95 23.4°, max 28.6°; club mean 9.6°, p95 16.9°, max 20.9°. Calm indoor water: 1-3° (§3.1, §7 item 3). So the lamps and the probe image break into oval blobs on a regular 2 m lattice (review frames SP1_s02, SP1_ride_end, SP6_s06): "hammered glass", not water.
- **What it reflects:** each pool hall's box-projected realtime probe, 1024², rendered once after the district is built (`FrontRoomsStructuresLook.Probe`), plus the lamps' specular; elsewhere the game's default cube. No Relay, no contact, no parallax beyond the box projection.
- **What shows through:** the floor at alpha 0.64 under a constant teal veil: no refraction, no depth.
- **Not measured in a render at first** (rendered since: §10.7). The slopes are computed; the frame share of the water, its luminance with and without the water and the reflection-only view wait for a workstation (harness ready in `W/water/harness`: stops in P, SP1, SP6, SP2 x RT off / reflection / dry / High / Ultra).

### 10.4 GlassRT and the water

**Read from the code (main, 2026-11-04), path by path.** GlassRT (desktop Macs with 16 or more GPU cores at quality level 3 and up, D1 (c); `FrontRoomsGlassRT.cs`) fires one mirror ray per pixel of a *receiver*, shades the hits with lamps and lenses and falls back to the zone cube on a miss (`research/glass/rt/`).
- **The water is not a receiver.** Receivers are FrontRooms/Glass materials with `_RTReceive = 1` (prepass bit 30) or renderers given bit 29 through the API (`FrontRoomsMetalGlassRTRendererFeature.cs`, the prepass). The water's URP Lit material is neither, so no ray ever leaves the water: its reflection stays the probe's, whatever RT does.
- **The water is not in the scene the rays see.** `RegisterRenderer` keeps a transparent material only when it is glass; "other transparents (beams, decals): not traced" (`FrontRoomsGlassRTSystem.cs` line 1229). So a ray from a window passes through the water to the basin floor: **in a traced glass reflection the pool is dry.**
- **Nothing in the water's shader reads RT.** Only `FrontRoomsGlass.shader` uses `_FR_GLASS_RT` / `_FR_GlassRTReflection`; the keyword is switched off again after the transparents.
- **RT needs the game's map.** Registration walks the chunks of a `FrontRoomsMapWorld`. In scene 4 only district P (the pool route, the game's own map code) has one; SP1-SP7, the hub and the kit row have none, so RT registers nothing there and the glass guards keep their zone cube. The 11-04 look review (`W/level_space/renders/scene4/review1/`) ran at Ultra with RT's default High but never visited P.
- **SSR, planar, probes:** URP 17.6 has no SSR (§2.3); today's water has no planar camera; it reflects the hall's box-projected realtime probe, rendered once after the district's build (`FrontRoomsStructuresLook.Probe`, 1024², box projection on).

**So why the image "drops a big notch" with RT.** RT sharpens what it touches and leaves the water exactly as it was: with RT on, the glass becomes a live, exact mirror (lamps, the Relay, contact), while beside it the water stays a probe image broken by a frozen 13° ripple and a teal veil (§10.3). The gap between the two surfaces in one frame is what reads as lower quality; RT itself lowers nothing (its own cost is +3.2-5.0 ms at 1440p High where it traces, `glass/rt/40_for_red.md`). In P, where it traces, the glass's reflections also show the pool without water. **RT will not fix the water by being on; the water has to become good on its own (pool_water, 游戏视觉) and, on the Mac, a receiver (P4d).**

**Live check in scene 4 (Red's choice of where to look, 2026-11-04: "不需要写在克隆里，坐在结构scene里这样我就能预览了").** Landed 2026-11-04 10:17 (LANDINGS): `Assets/Scripts/FrontRoomsMap/Structures/FrontRoomsStructuresWater.cs`, Editor only, added at Play beside scene 4's bootstrap; the game scene, the Level Designer and every build never create it, and no existing file changed. In any water district (SP1, SP2, SP6, P):
- **K** cycles the water's look on the shared stand-in materials (`WaterOf`): today; calm (a moving isotropic ripple of about 2° RMS in two layers, 1.6 m tiles of 0.12-0.8 m waves drifting 6 cm/s and 0.7 m tiles of 2.5-12 cm waves drifting 4 cm/s across them, everything else as today); dark glass (the calm ripple on a near-black green body, alpha 0.84, smoothness 0.985: the POOLS direction). Shift K goes back. These are stand-in materials, not 游戏视觉's water surface: they show the effect of slope, motion and body darkness alone.
- **J** cycles GlassRT Off / High / Ultra (restored when the scene ends); the bottom-left panel shows GlassRT's own numbers for the frame: visible receivers, glass pixels, instances in the traced scene, its GPU time.
What the code predicts the panel will show, not yet seen by anyone: 0 receivers in SP1, SP2 and SP6 (nothing traced, the frame does not change), the traced windows counted in P, and the water unchanged between Off and High everywhere. Red's walk gives the first live numbers.
A note for anyone driving URP Lit's detail layer from code: URP samples it at the base UV times `_DetailAlbedoMap_ST` (`LitInput.hlsl`, `InitializeStandardLitSurfaceData`), so the calm look sets the detail scale to 1.6 / 0.7 and takes the large layer's drift out of the detail offset; the compiler cannot catch a mistake here (the first version gave a 1.12 m detail tile riding on the large layer).

**What would change it (asks, §10.6):**
1. P4d (Red's Q3 "v1", 10-31; nobody builds it yet, 游戏视觉's queue after the pool packages): water as a receiver, one mirror ray per water pixel from the rippled normal, any number of water heights, the Relay and contact without a planar camera; +3-5 ms at 1440p [EST, §5.3].
2. Water in the traced scene: a ray that meets a water plane takes its Fresnel mirror (one more ray, or the planar image where it exists) and its tint, so glass sees the pool wet. Small in rays; needs a water instance type in the plugin's hit shading.
3. Scene 4's districts without a map: either they register through `FrontRoomsGlassRT.Register` (the API the destruction track uses) or the scene accepts that RT runs only in P.

### 10.5 Options for Red

Two steps. Step 1 is live in scene 4 now (K, §10.4) on the stand-in materials. Step 2 puts the same looks on 游戏视觉's water surface (`pool_water`: planar mirror, refraction, Fresnel, rings, absorption) when it and its Submerged stage land; the pick follows step 2. Rendered since on `sheets/water_looks.jpg` with a film (§10.7, run s10b); the cost column is from `70` §6 and §5 of this file.

| Look | What the player sees | Where it fits | Cost | Honest flaw |
|---|---|---|---|---|
| TODAY | A teal veil over the floor; frozen 13° bumps break lamps and the probe image into blobs on a 2 m lattice | Nowhere, as is | Probe + lamp specular only | Reads as hammered glass (§10.3) |
| Calm (step 1) | The same veil, but the surface moves and the reflections hold together: lamp streaks and the hall's probe image wobble a little instead of breaking | Shows what slope and motion alone do | One more normal sample; two 256² half-float textures | Still no mirror of what is near, no refraction, no depth |
| Dark glass (step 1) | A near-black green body; every lamp and bright wall reflects, and the floor shows only near the feet | POOLS' reading; dark or night rooms | As calm | In our white rooms the body is darker than anything real under white tile; it reads as a different liquid |
| Clear, physical (step 2) | Water as Red's Q1 asked for the club: nearly colourless near, turquoise with depth, a sharp mirror far | The club, daylight rooms | pool_water: planar ½ res on Ultra, refraction from the opaque texture | In white high-key rooms the floor outshines the mirror and the water nearly vanishes (`70` §6.8) unless the room above the water has darks |
| Lit turquoise (step 2) **REC for level water** | Red's Q1 "wrong" turquoise (absorption x3-5): a saturated body that gives the reflections something to read against, with the mirror and the moving glints of clear water | The level's pools (Q1) | As clear, plus absorption per body | A look, not physics; it needs bright compact sources (glints 10-40x their floor, `70` §6.8) or it turns flat |

**Recommendation and why.** Lit turquoise for the level's pools, clear for the club, dark glass kept for rooms that are dark by design. All three water looks fail in a uniformly white, evenly lit room, because the water shows what is above it (§10.1): the step that makes any of them read real is darks above the water (a darker ceiling or shadowed upper walls, unlit openings) and lamps whose reflections are much brighter than the tile. That is a room and lamp question for 夸张结构调研与设计 and the zone lamps, listed in §10.6.

### 10.6 Asks for the owners

1. **游戏视觉 (rendering, GlassRT):** P4d, the water as a GlassRT receiver (§10.4 item 1, Red's Q3 "v1"); a water instance type in the traced scene so glass sees the pool wet (item 2); step 2's material hookup in scene 4 when pool_water lands (this line writes the scene-4 side, 游戏视觉 owns the shader).
2. **夸张结构调研与设计 (rooms, tile glaze, thrown light):** the darks above the water (§10.1) in the pool districts; the glaze shimmer builds on FRSubThrown, the one caustic source (agreed 11-04).
3. **Zone lamps / 游戏视觉 (lamp lenses):** pool lamps whose lens drive gives glints 10x or more their floor, so the water sparkles (cue 3).
4. **关卡设计 (scene 4):** scene 4's districts without a map register with GlassRT through `FrontRoomsGlassRT.Register`, or RT stays in P only (§10.4 item 3).
5. **总控:** a workstation for the measurement run and the option sheet (§2.2 rows: player eye, wading, 3/4 at 3 m, under a lamp, RT off and on, COST).

### 10.7 Rendered: the five looks and what RT does (run s10b, 2026-11-04)

Account A (local_d012b32d), workstation `build` lent by 总控, 12:50-12:59. Clone-only harness `W/water/harness/FrontRoomsWaterRTProbe.cs`; stage 2 (`W/water/stage/water_stage2`) and 游戏视觉's pool_water runtime set (from proj_unlock, FrontRoomsSurface 7be4e235) were copied into the clone only, then the clone was synced back to main. Unity batch with graphics, Metal, M3 Max, quality Ultra, 1920x1080, ripple clock fixed at 20 s in every still: fail 0, 56 stills, 600 film frames, no shader error. Run s10a (account B, 11:56) is superseded: its pool_water mirrors rendered at 368x276 (sized from the batch window), R1 came out at the probe tier, the paper walls of P's entry corridor failed to compile (magenta: the `sub` collision under FR_SUBSTRATE, fixed by 游戏视觉 at 12:48), and its RT frames were RT Off in effect (below).

Images (Shot format): `sheets/water_looks.jpg` (WAIT-RED; rows R1-R7 x TODAY, W-A calm, W-B dark glass, W-C clear x1, W-D turquoise x4, then COST), `sheets/water_rt.jpg` (FLAG), `video/water_looks.mp4` (20 s at R3, 4 s per look, video only, no audio track).

**The looks (the author's one look; Red's eye is the check):**
- TODAY: the 2 m lattice of blobs (§10.3) in every deep row (R1, R3, R5). In P (R6) it is the only look that makes the flooded floor read as water at all, because its blobs are bright.
- W-A calm: the blobs go; the surface reads as a polished wet floor, not as a body of water (no mirror of what is near, no depth).
- W-B dark glass: POOLS' darkness, but in our white rooms it reads as a dark slab or oil; the blue patch at the far end of R3 is the hall probe showing through.
- W-C clear x1 and W-D turquoise x4 (pool_water): the walls mirror and meet their reflections at the waterline (R1, R3, R5), the wall lamps break into moving glints (R5), the body takes colour with depth (R3, R4); at the probe tier (R7) the colour stays and the mirror softens to the probe. W-D is more saturated and bluer than W-C; the two differ little in shallow water.
- **Flaw found: ankle-deep water in P (R6, `Kind.Ankle`, about 0.12 m) nearly vanishes with W-C and W-D.** A white, evenly lit office ceiling mirrored over white tile, with 0.12 m of colour, reads as a clean wet floor: §10.1's rule (the water shows what is above it) at its strongest. For level water that floods office floors the fix is the room (darks above, compact bright lamps) or a stronger colour or film term for ankle water in the Submerged stage (游戏视觉's call), not RT.
- Cost (COST row): pool_water in SP1 = 7 bodies, +7 draws, +14 tris, 2 mirror images at 1104x621 on Ultra (half resolution plus 15 % overscan); 1 mirror at 736x414 in the 1280x720 film. GPU time not measured (rule 25).

**What RT does** (P stops 9 L1 hall, 8 gallery, 5 corner, 7 M0; TODAY and W-D; RT Off, High, Ultra; 120 frames each with the stream-in gate off, so RT shows at once):
- GlassRT traced (1.2-4.3 ms GPU at High, TLAS 2,048-4,096 instances, 3-21 receivers inside the frustum) but found **0 glass pixels on screen at every stop**: P's receivers are clear-glass panes behind walls and pillars, and the gallery's round and square "observation windows" are emissive panels (`FrontRoomsPoolMaterials.Glow`), not glass. SP1, SP2 and SP6 have 0 receivers (s10a), as §10.4 predicted.
- RT Off against High, both blurred over 7 px so the film grain (which moves with time) drops out, pixels changed by more than 4/255: on the water 0.0-3.6 %, elsewhere 0-19 %, and the latter is the ceiling's greybox caustic texture, which scrolls with time between two stills. Ultra gives the same.
- So in scene 4 J changes nothing that RT draws, and the image does not drop with RT here. The drop Red described is the water beside surfaces that look right: the stand-in's frozen blobs and teal veil and, in the game, the RT glass beside it (§10.4). Both step-2 looks fix the deep water; ankle water needs the room.
- Measured in the batch capture path (the camera rendered into a texture), not in the Editor's Game view.
- Harness note: with no glass on screen GlassRT's stream-in gate never opens (its per-camera coverage reading needs glass pixels; by design), so a capture that waits for weight 1 waits for ever. s10a's rt pass did that at every stop.

**Next:** stage 2 lands as soon as pool_water is in main; then Red walks scene 4 with K (five looks) and J. New ask for 游戏视觉 (§10.6 item 1): ankle water's colour or film term in white rooms (R6).
