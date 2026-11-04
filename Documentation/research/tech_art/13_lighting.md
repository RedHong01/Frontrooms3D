# 13 · Lighting: baked vs realtime, probes, reflections, light counts, shadows

- Line: 技术美术调研 (ledger `tech_art`), session local_ad6f36ec. Topic agent 13, source prefix L.
- Stage: research draft (complete: all sections filled; 40 web sources, 3 of them snippet-only, plus 8 internal).
- Date: 2026-11-04.
- Red's ask (via 总控, 2026-11-04): "在做无限走廊的时候，我需要你也开启一个task用来 research专业的Technical artist会怎么做，提高画质表现的同时，节省性能。"
- Done: web reading only (WebSearch / WebFetch), main project read only (grep and sed of scripts, scene YAML, the URP asset and the installed URP / Core 17.6 package source in `Library/PackageCache`), measured numbers taken from `tech_art/MEASURED.md` [L44]. No Unity run, nothing downloaded, nothing written outside `drafts/`.
- Not done: no new measurement. Every saving below is ESTIMATE unless it says "measured" and points at [L44] or [L41].
- Version: the project is Unity 6000.6.3f1 with URP 17.6.0 (`package.json` of the installed package [L45]). Unity docs cited are the 6000.6 manual unless the row says otherwise.

## 0. Short answer

1. **Pros bake what does not change and keep live what the player watches change.** Indirect bounce, sky and static occlusion go into lightmaps or probe grids; direct light from lamps that switch, flicker or move, and the shadows of moving things, stay realtime [L19][L27][L28][L29]. Unity's Mixed modes are exactly this split: Baked Indirect keeps direct light and shadows realtime and bakes the bounce; Shadowmask also bakes static shadows, at most 4 overlapping lights per texel [L19][L22]. FrontRooms bakes nothing today: no lightmaps, no light probes, no APV; ambient is a Trilight gradient and specular is four per-zone baked cubes [L42][L47].
2. **What survives a module that is moved and recycled.** Data stored on the mesh travels with it: a lightmap is looked up through the renderer's own UV2 scale/offset, which a script may set at runtime [L15]; vertex colours and baked AO maps travel too. Probes do not: Light Probe Groups are one tetrahedral mesh rebuilt on scene load [L16], and APV is one world-space brick structure per scene or Baking Set; "URP can't share the data or blend between Lighting Scenarios if you move geometry between bakes" [L10]. APV can only be shifted as a whole, by an undocumented runtime World Offset in the Probe Volumes Options override [L46]. So the 5 recycled corridor rooms and the runtime maze can only carry baked data in their own meshes; APV fits only geometry that never moves. Because every lamp here can flicker, dim or die, only lamp-independent terms (occlusion, cavity, a sky/ambient term) are safe to bake (inference).
3. **URP 17.6 does not officially ship screen-space reflections.** The 6.6 manual and "New in Unity 6.6" list no SSR [L2][L5]; Unity staff target 6.7 and say "No plans to backport" [L1]. But the installed URP 17.6.0 package does contain a complete `ScreenSpaceReflectionRendererFeature`, wrapped in `#if URP_SCREEN_SPACE_REFLECTION`, a define no asmdef sets; Surface Cache GI is compiled out the same way behind `#if SURFACE_CACHE` [L45]. Using either on 6000.6 means enabling an undocumented define: UNCONFIRMED FOR 6000.6 as a supported feature. The supported next step for VCT, chrome and CRT glass is box-projected room probes reusing the captured zone cubes (glass line's G7) [L43]; `FrontRooms/Surface` still lacks the box-projection keyword, so it compiles to 0 on the 83 of 86 surface materials that use that shader [L45][L47][L44].
4. **Forward+ removes the 8-lights-per-object limit, not the per-camera limit:** 256 visible lights on desktop, 32 on mobile, 16 on OpenGL ES 3.0 [L6][L9]. In URP 17.6 iPhone and iPad builds fall in the 32 class (`SHADER_API_MOBILE`), as do WebGL2 and WebGPU [L45]. Measured: all 87 maze lights pass URP culling in every view, because 162-degree cones touch the frustum from behind [L44]. So on iPhone, iPad and WebGL, URP shades only about 32 of them, chosen in culling order, not distance order [L41][L45] (derived, not yet observed on a device). Pros budget lights by hand: a few shadowed, more unshadowed, far lamps emissive only, with fades [L22][L40].
5. **Measured shadow results decide most of this file [L44].** In the title corridor, every spot shadow off gives no visible change from eye-level views and saves 20-41 % of draws. The directional fill's shadow off is invisible in both corridor and maze and saves 12-16 % of draws and 19-44 % of shadow casters. In the maze, lamp shadows are visible (up to 31.8 % of pixels change). The room behind a shut door keeps 12 lights (4 shadowed) on, also with no visible effect.
6. **Why corridor shadows vanish and maze shadows do not (inference from code and arithmetic).** In the maze the shadows are cast by walls between 3 m cells: metre-scale occluders that stop lamps lighting through walls. In the corridor every troffer and everything it lights sit inside one open 11.5 x 12 m box, so the only shadows are contact shadows of props and door leaves. URP sizes the caster bias from the spot's frustum: at 162 degrees and 10 m range on a 1024 tile it is about 9.3 cm (soft-shadow kernel 2.5: the lamps are bare `AddComponent<Light>()`, so they use the per-light default, not the asset's High; claim check 15 #8), and a floor texel under the lamp is about 3.5 cm [L45][L42]. Contact shadows are erased, and the 8 unshadowed troffers in the same room fill what is left. Contact darkening in the corridor comes from SSAO, which is clearly visible (3-10 % of pixels) and no longer costs extra draws [L44].
7. **URP 17.6 has no cached or on-demand punctual shadows.** The only shadow cache in the URP runtime is for the XR quad-view inner pass [L45][L3]; HDRP has Every Frame / On Enable / On Demand update modes [L25]. "Update shadows only when a lamp changes" therefore needs custom code or a fork; what URP offers is fewer shadowed lights, toggled by script with intensity fades [L22].
8. **Best rows (§6):** drop the fill's shadow (L101, measured invisible, -12..-16 % draws, all tiers); shadows in the corridor only where an occluder is worth it, and with tight cones when they are kept (L102, L103); switch off the room behind a shut door (L104); box-projected per-room-type probes (L105, desktop gain if a capture proves it); lamp lenses on RSUV instead of property blocks (L108); a nearest-N light budget for the 32-light tiers (L107, Mobile/WebGL only).

## 1. Baked vs realtime in shipped games and in Unity 6

### 1.1 What studios bake, and what they keep live

- Remedy's Quantum Break used precomputed irradiance volumes for indirect light; the team found other methods too expensive for the quality they wanted ([search] snippet of [L29]; the notes PDF was too large to read).
- Treyarch presented "Volumetric Global Illumination at Treyarch": indirect diffuse from irradiance volumes as an alternative to lightmaps [L28].
- Naughty Dog: most of The Last of Us Part II is overcast, so "ambient lighting was a crucial component", and the talk is about "improvements of our baked ambient lighting system" that kept 30 fps "within our limited memory budget" on PS4 [L27].
- Naughty Dog's Uncharted 4 talk covers an almost entirely deferred lighting pipeline and a cube-map specular occlusion technique [L28].
- Activision (Call of Duty: Infinite Warfare, a 60 Hz title): existing GI methods were "expensive, in both memory and runtime performance"; the talk covers "the light grid system responsible for providing indirect lighting for the dynamic entities" and "automatically generating indirect illumination probes" [L33].
- Remedy's Control (Northlight): precomputed voxel-based GI, path traced from static objects and selected light sources; the ray-traced near-field diffuse falls back to the pre-baked GI when no nearby surface is found ([search] snippet of [L39], the chapter PDF was too large to read).
- The common pattern in these talks: the slow, low-frequency term (bounce, sky, ambient occlusion) is precomputed; direct light is computed every frame so that lights can move or change (inference from [L27][L28][L29][L33][L39]).
- No primary source on lighting technique was found for P.T., The Exit 8, Layers of Fear or Alien: Isolation (searched; only press and forum coverage). The closest indoor-horror primary is Frictional's light masking (§4.4) [L38].

Why it works: indirect light changes slowly across a surface and costs many rays per pixel to compute, so a bake buys a lot. Direct light carries the hard edges, flicker and motion the eye reads first, so it is kept live (inference).

### 1.2 Unity's own split: Mixed lights and Lighting Modes (6000.6)

| Lighting from Mixed lights | Baked Indirect | Shadowmask | Distance Shadowmask | Subtractive |
|---|---|---|---|---|
| Direct | realtime | realtime | realtime | realtime on dynamic, baked on static |
| Indirect | baked | baked | baked | baked |
| Static shadows within Shadow Distance | realtime | baked, max 4 lights per object | realtime | baked |
| Static shadows beyond Shadow Distance | none | baked | baked | baked |

Source: [L19] (table reconstructed from the page's cells). Shadowmask "stores up to four lights per texel"; beyond four overlapping lights Unity bakes the rest fully [L19][L22]. Unity recommends Shadowmask "in combination with Light Probes" [L22].

Limits for FrontRooms (inference):
- Every option above bakes against static geometry at fixed positions. The corridor's rooms move (recycling, rebase every 192 m) and the maze is generated at runtime [L47].
- A baked lamp is frozen. FrontRooms' lamps flicker, dim and die on their own schedules [L47], and a dead lamp must also lose its bounce. APV Lighting Scenarios can switch or blend whole baked states, including "lights being on and off" [L14], but a scenario is a whole scene state, not one lamp, and blending needs compute (§2.2).
- So the bakeable part here is the lamp-independent part: occlusion and cavity.

### 1.3 What FrontRooms has today (code, 2026-11-04)

- No `ReflectionProbe`, no `LightProbeGroup`, baked and realtime GI off, no static flags in any of the five scenes (grep of the scene YAML) [L47]. The October audit found the same [L42].
- Probes made by code at runtime (grep, 2026-11-04) [L47]:
  - Structures (Pool terrain, Pool route, Well, Slide): `FrontRoomsStructuresLook.Probe` adds a Realtime, ViaScripting, No Time Slicing, HDR, box-projected probe per hall at 512 or 1024 px, and renders all pending probes in one frame a frame or two after the build (`FrontRoomsStructuresLook.cs:172-192, 267-275`).
  - Stairwell A: a 64 px realtime box probe rendered once when lit; the reduced tier gets a fixed cube (`FrontRoomsStairwellDresser.cs:697-720`).
  - Run B: per-state custom box probes (armed/tripped) exist in code but are not shipped: "Dropped 2026-10-31 without a measurement ... T8 never showed a visible gain" (`FrontRoomsRunRig.cs:1296-1340`).
  - Because the URP asset has box projection off and `FrontRooms/Surface` lacks the keyword (§3.1), the `boxProjection = true` these probes set cannot take effect on Surface materials; with blending off, URP picks one probe per object (inference from [L45][L20]; confirm in the Frame Debugger).
- The URP asset uses the legacy light probe system (`m_LightProbeSystem: 0`), probe blending off, box projection off, probe atlas on [L47].
- `FrontRooms/Surface` takes indirect diffuse from SH only (`inputData.bakedGI = SampleSH(...)`, line 1008) and has no lightmap or APV variants, so it could not receive either even if baked [L47][L42].
- Specular: one baked HDR cube per zone type (`Refl_Level0`, `Refl_Office`, `Refl_Tall`, `Refl_DeadLamp`) set as the custom default reflection, cross-faded on zone change, intensity capped at 0.5 linear "until per-room box-projected probes land (G7)" (`FrontRoomsZoneReflection.cs`) [L47][L43].

### 1.4 Realtime GI in Unity 6.6 and 6.7 (status check)

- The 6.7 beta manual lists a "Surface Cache Global Illumination Volume Override reference for URP" [L4]; the 6.6 manual has no Surface Cache page [L5].
- The 6000.6.0 release notes already contain Surface Cache items ("Use hardware raytracing when available in Surface Cache", "Reduced light leaking issues with the Surface Cache") [L3].
- The installed URP 17.6.0 has `SurfaceCacheGIRendererFeature.cs` with `#if SURFACE_CACHE` as its first line; nothing defines it [L45].
- The 6.7 beta page: "Surface Cache Global Illumination (GI) produces real-time indirect diffuse lighting in URP, without baking or precomputation"; it "replaces baked global illumination rather than adding to it" and gives transparents nothing; it needs static batching off, Default Mesh Buffer Target = Raw and compute shaders; "Unity supports hardware ray tracing only on DirectX 12", other APIs use "a compute shader fallback, which is slower" [L48].
- Conclusion: UNCONFIRMED FOR 6000.6. A runtime GI that needs no bake is the only GI that suits recycled rooms and a generated maze, and FrontRooms already meets the static-batching requirement (no static batching anywhere [L44]), so it is worth testing when the project moves to 6.7 (inference). On the Mac it would run on the slower compute fallback [L48]; WebGL2 has no compute shaders, so it would be desktop and iOS only (inference).

## 2. Adaptive Probe Volumes (URP 17.6) and modules that move

### 2.1 How APV works

- "An Adaptive Probe Volume is a group of Light Probes that Unity places automatically based on the geometry density in your Scene, to create baked indirect lighting" [L10].
- Bricks: "Each brick contains 64 Light Probes, arranged in a 4 × 4 × 4 grid"; brick sizes follow geometry density; default probe spacing "1, 3, 9, or 27 m" [L10].
- Sampling: "Each pixel of a GameObject samples lighting data from the eight closest Light Probes around it", against per-object sampling for Light Probe Groups [L10]. Unity's blog: APV gives "better directionality compared to Lightmaps" and also lights VFX Graph particles in URP [L14].
- Setup: Light Probe System = Adaptive Probe Volumes in the URP asset, a Global volume, lights Mixed or Baked, meshes with Contribute GI, receivers set to Light Probes [L11].
- Limits: "You can't adjust the locations of Light Probes inside an Adaptive Probe Volume" and Light Probe Groups cannot be converted [L10].

### 2.2 Baking Sets, Lighting Scenarios, blending, sky occlusion, streaming, memory

- A Baking Set is one or more scenes plus one set of settings [L10]. Data is split into shared data (subdivision, probe placement) and per-scenario data (the lighting) [L10].
- Lighting Scenarios switch or blend baked states, including lights on/off; "Lighting scenarios only manage baked APV light probe data; other elements need to be handled manually" [L14].
- Sky occlusion "exclusively handles sky lighting" and does not manage indirect light [L14]. URP gained scenario blending and APV sky occlusion in Unity 6.0 [L2].
- Streaming: disk to CPU and CPU to GPU for large worlds [L14][L10]. In the Core 17.6 source, scenario blending and disk streaming are switched off when `SystemInfo.supportsComputeShaders` is false (`ProbeReferenceVolume.cs:1046, 1057`); basic data upload has a non-compute path (`ProbeBrickPool.cs:152, 546`) [L46].
- Memory: the URP asset exposes a probe-volume memory budget (FrontRooms: 1024 = Medium) and a blending budget (256), L1 SH bands, streaming and scenarios off [L47]. Unity's pages give no MB or ms figures [L14]; measure in the Memory Profiler if APV is ever used.

### 2.3 Leak reduction

- Tools: Virtual Offset, Dilation, Probe Adjustment Volumes, Rendering Layers (up to four masks) and two leak-reduction modes [L14].
- "Performance" warps the sampling uvw and samples once; "Quality" samples 1 to 3 times [L12]. Quality is the default and "may introduce a slight overhead ... especially noticeable on lower-end platforms" [L14].
- For FrontRooms this would matter: the corridor's walls are 26 cm thick (`FrontRoomsRoomStream.cs` `WallThickness = .26f` [L47]), far thinner than the smallest default probe spacing of 1 m [L10], so probes on both sides of a wall would mix unless the leak tools are tuned (inference).

### 2.4 Cost and platforms

- Metal desktop and Metal iOS: compute available, so all APV features can run (inference from [L46]). Per-pixel cost is 8-probe trilinear sampling, 1-3 times with Quality leak reduction [L10][L12].
- D3D12 / Vulkan Windows: same as Mac (inference).
- WebGL2: Unity's APV pages say nothing about platforms [L10][L11][L13]. From source: no scenario blending and no disk streaming without compute; base sampling may work (UNCONFIRMED; needs a WebGL build test). Unity Web otherwise "only supports Baked Global Illumination" with non-directional lightmaps (6000.3 WebGL page, via [L40] T4).

### 2.5 Can baked lighting follow a module that is instantiated, moved and recycled?

| Baked data | Where it is stored | Survives translation | Survives recycling to a new slot | Survives the 192 m rebase | Notes |
|---|---|---|---|---|---|
| Lightmap (UV2) | per-renderer `lightmapIndex` + `lightmapScaleOffset` into a shared texture | yes, if a script re-applies the indices after instantiate [L15] | yes, same mesh, same UVs | yes | `lightmapScaleOffset` "is only serialized when building the player"; otherwise "a custom script that brings external lightmapping data" must set it [L15]. No rotation problem for whole-room moves. Bakes the lamps' light too, so lamp states freeze unless only occlusion is baked |
| Shadowmask | same UV layout as the lightmap [L19] | yes, same as lightmap | yes | yes | Bakes which lamp shadows a texel; meaningless when lamps die at random |
| Baked AO / cavity in UV2 or in a prop's own mask map | the mesh's texture | yes | yes | yes | Lamp-independent; the standard asset-side answer (inference) |
| Vertex colour AO | the mesh | yes | yes | yes | Needs enough vertices; the room shells are boxes (inference) |
| Light Probe Group | one scene-wide tetrahedral mesh | no; Unity re-tetrahedralizes on additive scene load [L16] | no | no | Probe positions are world space |
| Custom SH per renderer | `LightProbeUsage.CustomProvided`: "values are extracted from the material property block set on the renderer" [L17]; `CopySHCoefficientArraysFrom` fills `unity_SHAr..unity_SHC` [L18] | yes (stored with the room) | yes | yes | Property blocks are "not compatible with SRP Batcher" [L18], so this costs batching on every renderer that uses it |
| APV | one world-space brick structure per scene or Baking Set [L10] | only all at once: a runtime "World Offset" exists in Probe Volumes Options, "not considered while baking", marked as an additional property and not in the manual [L46][L12] | no: one offset for the whole set, not per room | yes in principle, by moving the global offset with the rebase (UNVERIFIED) | "URP can't share the data or blend between Lighting Scenarios if you move geometry between bakes" [L10] |
| Reflection probe (baked or custom cube) | a component with its own transform and box | the box follows its transform (UNVERIFIED for baked probes moved at runtime; test in the clone) | yes, as a child of the room root | yes | The cube content is the captured room; box projection keeps parallax right if the box moves with the room |

Pointer only (not a source of facts): community "prefab lightmapping" scripts store `lightmapIndex`/`lightmapScaleOffset` per renderer and re-apply them on instantiate, and the Bakery lightmapper has a "Lightmapped Prefab" component ([search] snippets, [L30]). These confirm the pattern of [L15], nothing more.

### 2.6 What this means for the two FrontRooms spaces (inference)

- **Title corridor:** each room family (Lobby, Office, Run, Pool) has a fixed size (11.5 x 12 x 2.9 m) and a designed layout [L47]. Each family can be baked once in an editor scene, and its occlusion can live in its own meshes and travel with the recycled root. The 12 troffers' light must stay realtime because lamps flicker and die per sequence [L47].
- **Maze:** cells, edges and dressing are chosen at runtime [L47]. Only per-module data survives: AO baked into each prop and module mesh, never across modules. Cross-cell occlusion (a wall darkening the next cell) has to come from realtime shadows or SSAO.
- **APV:** fits only geometry that is static in a scene. All five scenes are built at runtime today (no static flags, structures scene 5 KB) [L47], so APV has no target now. It becomes useful only if a fixed hand-built space is added.

## 3. Reflections

### 3.1 Reflection probes in URP 17.6

- Each probe has a box; "When a pixel of an object is outside of any reflection probe volume, Unity uses the skybox reflection" [L20].
- With Probe Blending on, URP weights each probe "for each individual pixel", unlike the Built-in pipeline's per-object choice [L20]. At most two probes affect an object, chosen by Importance, then smallest box, then surface area [L20].
- Forward and Deferred pick probes per object on the CPU; "Unity supports reflection probes with indirect draws in the Forward+ and Deferred+ rendering paths" [L20]. Forward+ is the path to choose if you "need to blend more than 2 Reflection Probes" [L8].
- In URP 17.6 probes are Forward+ items next to lights: `maxVisibleReflectionProbes = Math.Min(maxVisibleAdditionalLights, 64)` (`UniversalRenderPipeline.cs:253`) [L45]. On the 32-light tiers, lights and probes share the tile word budget; the glass line read in the 17.3 source that over 32 items URP needs two words per tile and doubles the tile size until it fits [L43].
- Box projection: in the cluster path it is a branch on `_REFLECTION_PROBE_BOX_PROJECTION`, which `GlobalIllumination.hlsl` defines to 0 when a shader does not declare the keyword (lines 39-40, 314) [L45]. `FrontRooms/Glass` declares it; `FrontRooms/Surface` does not [L47]. So walls, VCT floors and props cannot box-project today even if the asset is switched on.
- Cost: a baked or custom probe costs no rendering. Realtime probes are "not compressed in memory"; Every Frame is "the most resource-intensive option"; time slicing spreads one update over 9 frames (all faces at once) or 14 frames (individual faces) [L21]. The glass line measured, in the editor, +47.5 ms per frame for every-frame refresh at 128 px, a +40-70 ms hitch for one one-shot unsliced refresh at 128 or 256 px, and no clear difference between 128 and 256 px (the cost is six camera renders, not pixels) [L43]. The Structures code renders several such probes in one frame after a build (§1.3), so that frame pays the hitch once per probe (ESTIMATE, not measured).

### 3.2 Planar reflections

- URP has no built-in planar reflection; it takes an extra camera. The glass line measured a full planar at 12 m at 12-20 % of a frame, and a Relay-only planar at about 7 %, the fixed cost of one extra camera [L43]. Worth it only for one hero surface held by the player (the glass line's plan) [L43].
- Pointer: Microsoft's "Optimized pixel-projected reflections for planar reflectors" makes sharp dynamic reflections for planar surfaces from the colour buffer and artist-defined reflective regions, without an extra camera [L33]. It is a custom pass (not in URP); a candidate for flat VCT floors only if SSR on 6.7 is not enough (inference).

### 3.3 Screen-space reflections in URP 17.6: verified status

- Unity's preview thread: "ship in Unity 6.7"; testing needed "at least Unity 6000.6.0a7"; a fix "Should be fixed in 6000.6.0b5" (2026-07-14); "No plans to backport" (2026-09-01); "Unity 6000.7.0b1 is now available", with "Compatibility with MSAA" (2026-09-18) [L1].
- The 6.6 manual's Lighting-in-URP pages have no SSR page [L5]; "New in Unity 6.6" lists no SSR [L2]. The 6.7 beta manual's Lighting-in-URP index has none either [L4].
- The 6000.6.0 release notes do contain one line: "URP: Adjusted SSR roughness to blur curve to more closely match reflection probe's" [L3]. The code exists in the 6.6 cycle.
- The installed URP 17.6.0 has `Runtime/RendererFeatures/ScreenSpaceReflectionRendererFeature/` (7 files, 1,255 lines), a Volume override "Lighting/Screen Space Reflection", `ComputeScreenSpaceReflection.shader`, and `_SCREEN_SPACE_REFLECTION` in `Lit.shader`. Every runtime file starts with `#if URP_SCREEN_SPACE_REFLECTION`; no asmdef `versionDefines` entry sets it and the project's Player defines do not [L45][L47].
- What the 17.6 code says about cost and platforms [L45]: modes Disabled / Opaques Only / Opaques And Transparents; resolution and upscaling (Bilinear, Bilateral) options; Linear or Hierarchical marching, and Hierarchical falls back to Linear "on platforms without compute shader support"; rough reflections by box or Gaussian blur; presets Fast to Best Quality; no orthographic cameras; falls back to probes when a ray misses.
- Verdict: **URP 17.6 does not ship SSR as a supported feature.** It can be compiled in on 6000.6 by adding the scripting define `URP_SCREEN_SPACE_REFLECTION`, at your own risk, undocumented and unsupported (UNCONFIRMED FOR 6000.6). The supported route is Unity 6.7 [L1].
- Where SSR helps here: glossy floors (VCT, damp carpet) reflecting troffer rows that are on screen. Where it does not: head-on windows (rays leave the screen) and glass as a transparent ("you can only have 1 reflection per pixel") [L43][L1]. `FrontRooms/Surface` would need the SSR sample and its DepthNormals pass would need smoothness (glass line analysis) [L43].
- The custom "FrontRooms Metal Glass RT" feature traces glass reflections in hardware on the Mac only [L43]; it does not cover floors or chrome.

### 3.4 What each shiny period surface wants (inference, desktop first)

| Surface | Best supported option on 6000.6 | Later |
|---|---|---|
| VCT floor tiles, damp carpet sheen | box-projected room probe with the zone cube (parallax-correct troffer rows) | SSR on 6.7 |
| Window and door glass | zone cube + room probe; Mac RT glass feature; planar only for the held pane [L43] | — |
| Chrome, door hardware, rails | room probe (small, curved, rough: SSR adds little [L43]) | — |
| CRT screens | room probe at low intensity on the glass plus the screen's own emission | SSR does nothing for a screen faced head-on (inference, same reason as windows) |

## 4. Forward+ and light counts

### 4.1 Limits (URP 17.6)

- Forward: "a maximum of 9 lights for each GameObject" (1 main + 8 additional) [L6].
- Forward+: "no limit to the number of lights that can affect each GameObject. There's still a limit on the number of lights visible per-camera"; it "divides the screen into tiles" and lights a pixel only with the lights of its tile [L9]. It ignores the asset's per-object limit [L9] (FrontRooms' `m_AdditionalLightsPerObjectLimit: 8` is ignored [L47]).
- Per camera: "Desktop and console platforms: 256", "Mobile platforms: 32", "OpenGL ES 3.0 and earlier: 16" [L6]. In Forward+ the main light counts too, "so the per-camera limits are one light less" [L8].
- In code: `maxVisibleAdditionalLights` returns 32 when `SHADER_API_MOBILE` is defined (iOS, Android) or the device is OpenGL Core, GLES3 or WebGPU; 16 on GLES 3.0 mobile; else 256 [L45]. WebGL2 is GLES3, so 32 [L41].
- The limit can be lowered or raised only by embedding the `universal-config` package and editing `MAX_VISIBLE_LIGHT_COUNT_*` and `k_MaxVisibleLightCount*`; Unity calls it "a workaround for a limitation in the URP design" and it must be redone on every upgrade [L7]. Unity 6.0 added that config package [L2].
- When there are more visible lights than the cap, URP keeps the first N of its culled list; the culled order is not distance order [L41] (measured October: a lamp 0.8-1.3 m from the camera was typically dropped).

### 4.2 Cost model

- Forward+ (AMD, 2012): the screen is divided into tiles (e.g. 16 x 16 pixels), "for each tile, a list of overlapping lights is computed", and the forward shader loops over that list; it beat compute-based deferred lighting in total time, more so at low memory bandwidth [L31].
- Clustered shading (Chalmers, HPG 2012) also splits by depth, which gives "a better mapping of light sources to view samples than tiled shading" and "better worst case behaviour" at "large discontinuities in depth" [L32]. Infinity Ward's "z-binning" (SIGGRAPH 2017) improves light culling for tiled and clustered renderers [L33].
- URP 17.6 Forward+ is tiles plus z-bins, built on the CPU each frame by Burst jobs (`Runtime/Tiling/TilingJob.cs`, `ZBinningJob.cs`, `LightMinMaxZJob.cs`) [L45]. Sizes: `lightsPerTile = ((max + 31) / 32) * 32`, `maxTileWords = (max <= 32 ? 1024 : 4096) * 4`, `maxZBinWords = 1024 * 4` [L45]. Spot lights are tested as cones, not spheres (`TilingJob.cs:82-104, 121-162`) [L45], so a narrower cone covers fewer tiles.
- So the GPU cost grows with lights per tile and per z-bin, not lights per scene. Lights that do not reach a tile cost that tile nothing (inference from [L31][L45]).
- Metal: Apple GPUs can cull lights per tile inside the render pass with tile shading and keep culled light lists in persistent threadgroup memory [L34]. URP does not use that path; it does its culling in the CPU jobs above (inference from [L45]).
- What makes FrontRooms expensive is the shape: a 162-degree, 10 m cone reaches about 9.6 m along the floor (ESTIMATE: sqrt(10^2 - 2.85^2)), so each lamp overlaps many tiles, and lamps up to ~10 m behind the player still light the floor in view [L41][L44]. Epic's advice for its own shadowed local lights reads the same way: avoid having "the majority of pixels on screen taken up by several large lights" [L37].

### 4.3 Light layers and cookies

- The asset has light layers off (`m_SupportsLightLayers: 0`) and cookies on with a 2048 atlas [L47]. `FrontRooms/Surface` already compiles `_LIGHT_LAYERS` and `_LIGHT_COOKIES` variants [L47].
- Rendering Layers let "certain Lights to affect only specific GameObjects"; "When using Rendering Layers only for Lights in the Forward Rendering Path, the performance impact is insignificant", with steps up at 9, 17, 25 layers [L35]. Forward+ is not mentioned (UNCONFIRMED FOR Forward+ cost).
- The same mask filters shadow casters: a renderer outside a light's mask "does not cast shadow from" that light, unless Custom Shadow Layers adds its layer back [L36]. So layers are a per-light caster filter as well as a light filter.
- Unity: "point or spot lights with light cookies can serve as a substitute for lights that cast real-time shadows" where things are static [L22]. A cookie has no depth test, so it can only carry the fixture's own shape (louver, housing), never furniture [L40] T4.

### 4.4 How pros budget practical lights

- Unity's own advice: toggle shadow-casting lights by distance with `Light.enabled`, and "fade in and out the intensity property of a light" to avoid popping; fewer shadow casters; point-light shadows cost about six spot lights [L22].
- The internal WebGL plan already designed the budget, and it is reused here, not redone [L40] T3-T5: nearest/visible-first selection; 4 shadowed lamps at 512 px; at most ~20 unshadowed; the rest emissive-only (beyond 16 m the maze already does this); 0.5 s fades with hysteresis; Unreal's per-light `MaxDrawDistance` / `MaxDistanceFadeRange` and Doom 3's light-area lists as precedent.
- Measured October budget for WebGL: at most 28 real lights and 2 shadowed lamps, no directional shadow [L41].
- Frictional Games (Amnesia) faced the same leak FrontRooms has: without shadows "light bleeds through walls, and show up in other rooms close by", and shadows "can be pretty expensive (especially for point lights)". Their fix was light masking: a light's range is limited by a box so it is "cut off before reaching an adjacent area"; it "pretty much does the job of shadows, but is much cheaper"; boxes only, and it was meant for static lights [L38].
- Epic, for many shadowed local lights: "Some projects disable dynamic shadows on distant lights and rely on screen-space Contact Shadows instead" [L37].

### 4.5 FrontRooms counts today

- Maze, measured 2026-11-04: 87 visible lights in all 3 views, 8-15 shadowed; ~12.8k live renderers, 2.5-3.2k in the frustum, ~935 with a property block [L44].
- Corridor: 20 / 24 / 35 visible lights (6 / 9 / 13 shadowed) [L44].
- Desktop (Mac, Windows) is under the 256 cap, so nothing is dropped there. iPhone/iPad builds compile the 32 cap (`SHADER_API_MOBILE`) [L45], so on handhelds the maze would drop about 55 of 87 lights in culling order, the same failure the October audit described for WebGL [L41] (derived from the source and the measured count; UNVERIFIED on a device). This is a Mobile-tier finding; nothing in `TickFixtures` branches for handhelds today (only a WebGL near-only tick) [L47].

## 5. Shadows

### 5.1 The directional fill: verified, still there today

- `FrontRooms3DGame.ApplySceneLighting` builds "Soft ambient direction": directional, intensity 0.22, colour D6D3B4, soft shadows, strength 0.18, bias 0.045, normal bias 0.28 (lines 557-565); it is called by the editor build tool `FrontRooms3DBuild.cs:25` [L47].
- The shipped scene has its own copy: `FrontRooms3D.unity` line 2255 "Soft ambient direction", `m_Type: 1` (directional), `m_Intensity: 0.16`, `m_Shadows: m_Type: 2` (soft), `m_Strength: 0.18` [L47].
- As the main light it pays for the main-light shadow map: 2048, 2 cascades, 40 m (asset) [L47]. At 18 % strength it still lights 82 % through every ceiling and wall [L42].
- Measured: its shadow off changes 0.00-0.08 % of pixels (noise floor 0.07 %) in 3 corridor and 3 maze views, and removes 12-16 % of draws and 19-44 % of shadow casters [L44].

### 5.2 Spot shadow resolution and bias: verified cones today

- Corridor troffers: `spotAngle = 162f`, `innerSpotAngle = 96f`, range 10 m (6.5 m in Run), the 4 centre-column fixtures `LightShadows.Soft`, strength 0.92 (`FrontRoomsRoomStream.cs:1201-1212, 1502`). The Run exit-sign spot is also 162 / 140 degrees and soft-shadowed (`:2146, 2171-2178`) [L47].
- Maze lamps: 162 / 96 degrees, range 10 m (12 m in tall zones), about 34 % may cast, soft shadows within `shadowRadius` 9 m (`FrontRoomsMapWorld.cs:3719-3730, 3859`; `FrontRoomsLevelProfile.cs:37-39`); pooled fixtures take the same angles (`FrontRoomsMapWorld.Pool.cs:715-731`) [L47].
- No script sets a per-light shadow tier for these lamps, so they use URP's default High tier, 1024 [L47][L42]. At SHADOWS HIGH on desktop `FrontRoomsQuality` raises the atlas to 8192 (64 tiles of 1024, about 128 MB at 16-bit, per its own comment) [L47].
- URP 17.6 bias, unchanged from 17.3 (`ShadowUtils.cs:390-446`) [L45]: `frustumSize = tan(spotAngle/2) * range`; `texelSize = frustumSize / resolution`; depth and normal bias = bias x texelSize (asset bias 0.6 / 0.6); soft shadows multiply both by the kernel radius of the light's own Soft Shadow Quality, 3.5 High (7x7), 2.5 Medium (5x5), 1.5 Low (3x3); "Use Pipeline Settings" and a missing light component both give 2.5 (`ShadowUtils.cs:426-446`; claim check 15 #8). The code comment calls it "a poor approximation" because URP biases on the caster side.

Arithmetic (ESTIMATE, all at a 1024 tile). Today's lamps use kernel 2.5 (column "x2.5"); x3.5 applies only if a light is set to High per light (claim check 15 #8):

| Cone / range | frustumSize | texelSize | bias (x0.6) | x2.5 (today's lamps) / x3.5 (High per light) | floor texel under the lamp (2.85 m) |
|---|---|---|---|---|---|
| 162° / 10 m (today) | 6.314 x 10 = 63.1 m | 6.17 cm | 3.70 cm | **9.3 cm** / 12.9 cm | 2 x 2.85 x 6.314 / 1024 = **3.51 cm** |
| 162° / 10 m at a 2048 tile | 63.1 m | 3.08 cm | 1.85 cm | 4.6 / 6.5 cm | 1.76 cm |
| 162° / 6.5 m (Run) | 41.0 m | 4.01 cm | 2.40 cm | 6.0 / 8.4 cm | 3.51 cm |
| 140° / 10 m | 2.747 x 10 = 27.5 m | 2.68 cm | 1.61 cm | 4.0 / 5.6 cm | 1.53 cm |
| 120° / 10 m | 1.732 x 10 = 17.3 m | 1.69 cm | 1.01 cm | 2.5 / 3.6 cm | 0.96 cm |
| 120° / 7 m | 12.1 m | 1.18 cm | 0.71 cm | 1.8 / 2.5 cm | 0.96 cm |
| 90° / 7 m | 7.0 m | 0.68 cm | 0.41 cm | 1.0 / 1.4 cm | 0.56 cm |

Reading: the cone, through tan(angle/2), dominates. Going from 162 to 120 degrees divides bias and floor texel by 3.6; doubling the tile only by 2, at 4x the atlas memory (ESTIMATE). Range scales bias linearly but not the floor texel.

Where the texels go (ESTIMATE): the shadow frustum is a square pyramid, so at the floor (2.85 m below the lamp) it is 2 x 2.85 x tan(angle/2) wide. At 162° that is 36.0 m, a 1,296 m² square, while a corridor room is 11.5 x 12 m = 138 m²: about 11 % of the floor-level texels land inside the lamp's own room, the rest behind its walls. The light's 10 m range reaches only about 9.6 m along the floor (sqrt(10² - 2.85²)), a 289 m² disc. At 120° the square is 9.9 m wide (97 m²) and lies almost entirely inside the room.

### 5.3 Measured: corridor shadows invisible, maze shadows visible [L44]

| Variant | Corridor (3 views) | Maze (3 views) |
|---|---|---|
| every spot shadow off | draws 846→675, 833→588, 952→566 (-20 / -29 / -41 %); casters 308→137, 348→103, 507→121; pixels changed 0.04 / 0.07 / 0.00 % (noise) | draws 5,328→3,837, 4,842→3,280, 7,379→3,688 (-28 / -32 / -50 %); pixels changed 1.9 / 2.1 / **31.8 %** |
| directional fill shadow off | draws -16 / -12 / -13 %; casters -44 / -30 / -24 %; 0.00 / 0.07 / 0.00 % | draws -16 / -14 / -12 %; casters -36 / -30 / -19 %; 0.00 / 0.06 / 0.08 % |
| lights of the room behind a shut door off | draws = / 833→716 / 952→836; lights 24→13, 35→24; noise | — |
| SSAO off | -4 draws; pixels 9.6 / 5.4 / 3.4 % (visible) | -4 draws; 6.2 / 4.9 / 7.4 % |

Derived (ESTIMATE): the corridor pays about 31-34 draws per shadowed troffer (171/5, 245/8, 386/12), the maze about 213-264 per shadowed lamp (1,491/7, 1,562/7, 3,691/14). Maze casters per lamp are consistent with "every renderer within the 10 m range": 12.8k renderers over 25 chunks of 24 x 24 m is ~0.89 per m², and a 9.6 m floor radius holds ~290 m², so ~257 (ESTIMATE).

Why the difference (inference from the code [L47], URP source [L45] and the measurements [L44]):
1. **What the shadows fall on.** Maze shadows come from walls between 3 m cells. Without them each lamp lights neighbouring cells through the walls (the map code itself notes an unshadowed lamp "lights straight through the rooms' walls" [L42]), so dark bands fill with light. That is metre-scale, and a 13 cm bias cannot hide it. A corridor room is one open box: no wall stands between a troffer and the floor or walls it lights. Its only shadows are contact shadows of furniture legs, door leaves, frames and baseboards: centimetre to decimetre scale.
2. **Bias and texel size erase exactly that scale.** At 162 degrees, 10 m and 1024 px the caster is pushed about 9 cm (kernel 2.5) and a floor texel is 3.5 cm; with the 7x7 tent the penumbra is roughly 7 x 3.5 ≈ 25 cm wide at the floor (ESTIMATE). A table leg's contact shadow is smaller than the bias. And only about 11 % of each map's floor-level texels fall inside the lamp's own room (§5.2), so the 8192 atlas buys resolution mostly for places the lamp cannot light.
3. **Fill.** Each room has 12 troffers and only the 4 centre ones cast. A shadow from one lamp removes only that lamp's share; the 8 unshadowed side troffers, the Trilight ambient and SSAO fill the rest.
4. **The views.** The measured views are eye-level, all rooms Lobby (few props). Office rooms with cubicle partitions would put wall-like occluders inside a room, and the result could differ: measure each family before deciding (§6, L102).

### 5.4 Atlas sizing and soft-shadow cost

- One atlas for all spot and point shadows; "an atlas of size 1024 × 1024 can fit: Four shadow maps of 512 × 512 pixels" [L23]. Pick the atlas so that tiles are not reduced [L23]; when it overflows, URP lowers every slice's resolution and then drops maps with a warning (17.3 source, [L40] §1.8).
- Soft shadows "might have a significant performance impact on platforms that use tile-based rendering, such as mobile platforms" [L22]. URP 17.6 supports per-light soft shadow quality (`SupportsPerLightSoftShadowQuality`, off only on XR mobile) [L45], so the kept lamps can run High while others run Medium or Low.
- Point lights render six maps, "comparable with rendering shadows from six spot lights" [L22]; FrontRooms already moved the Run exit sign from a point light to one spot map [L47].

### 5.5 Cached shadows and update-on-change

- HDRP: Update Mode Every Frame / On Enable / On Demand; "HDRP caches the shadow map when the shadows update, and uses the cached version between updates"; point and area lights have their own cached atlas [L25].
- URP 17.6: `useCachedShadowMap = supportShadowMapCaching && cameraData.xr.isQuadViewInnerPass` (`UniversalRenderPipeline.cs:1057`), and both shadow caster passes only skip their clear when that flag is set [L45]. The 6000.6.0 notes describe it as "shadow map caching for Quad Views in XR" [L3]. No per-light update mode exists for normal cameras [L45][L2].
- Unreal's Virtual Shadow Maps show what pros get from caching: "Reusing shadow map pages from previous frames is key to maintaining high performance"; "Any light movement or rotation will invalidate all cached pages for that light"; static and dynamic casters are kept as "two copies of depth" so a moving object does not force static pages to re-render [L37]. id's DOOM (2016) cached the static part of its shadow maps (frame analysis, via [L40] T4).
- For FrontRooms a cache would pay off in the corridor and the maze alike, because lamps do not move: only flicker (intensity, no shadow change), the doors, the Relay and the player invalidate (inference).
- So "render a lamp's shadow only when it or its casters change" is not available in URP 17.6. A custom Render Graph pass that keeps a private atlas would be needed; the October WebGL plan rated that "L effort" and did not recommend it [L40] T4. What URP does offer: fewer shadowed lights, switched by script, with strength fades [L22].

### 5.6 Contact shadows without shadow maps

- URP has no contact-shadow feature; HDRP does (the 6.6 notes fix "HDRP contact shadows quantizing to 2x2 pixel blocks") [L3]. URP's SSAO is listed under Shadows in URP [L24].
- Measured: SSAO is clearly visible (corridor 3.4-9.6 %, maze 4.9-7.4 % of pixels) and, unlike October, switching it off removes only 4 draws; normals from depth instead of depth-normals changes 0.24-0.29 % [L44]. So SSAO is already the contact-shadow source in the corridor, at GPU cost, not draw cost.
- Unity's other shadow-cost tools: a simplified shadow-only mesh for complex casters, and Cast Shadows Off for small objects [L22].

## 6. Applied to FrontRooms

Desktop (Mac/Win) is the reference: every row must leave it unchanged or better. Tier "all" means the change is the same on every platform and was measured invisible. Change ids run L101-L113 so they never clash with the source ids L1-L48.

| id | change | what the player sees | expected saving | risk | tier | owner | desktop look change |
|---|---|---|---|---|---|---|---|
| L101 | Directional "Soft ambient direction": keep the light, set its shadows to None (scene object and `ApplySceneLighting`) | no visible change (measured 0.00-0.08 %, 6 views) | measured -12..-16 % draws, -19..-44 % shadow casters; frees the 2-cascade 2048 main-light map (ESTIMATE 2048² x 2 B = 8 MB at 16-bit) and the per-pixel cascade lookup | low | all | 游戏视觉 (scene lighting; `FrontRooms3DGame.cs` is shared, coordinate) | none |
| L102 | Corridor: troffer shadows per room family, not per fixture index. Lobby: off. Office/Run/Pool: on only for fixtures above an occluder that matters (partitions, doors, the Relay's path), after a per-family capture | Lobby: no visible change (measured); other families: decided by capture | Lobby measured -20..-41 % draws, -56..-76 % casters; ~31-34 draws per shadowed troffer | low for Lobby, medium elsewhere | all | 游戏视觉 (`FrontRoomsRoomStream.cs`) | none (Lobby, measured); others: none or gain by capture |
| L103 | The corridor shadows that stay: tight shadowed cone (about 120° outer) and the range the room needs (about 7 m), High soft only on those; side columns keep 162° unshadowed | gain: contact shadows under desks, door leaves and baseboards start to land | per kept lamp: bias 9.3 → 1.8 cm with lamps on pipeline settings (12.9 → 2.5 cm only if set to High per light), floor texel 3.5 → 0.96 cm; casters per map down (frustum footprint at the floor 1,296 → 97 m², ESTIMATE) | medium: a narrower cone removes the centre lamps' light from the lower walls 5.75 m away (63-81° from nadir); needs an A/B capture | Desktop first, then all | 游戏视觉 | gain (to be proven by capture) |
| L104 | Room behind a shut door: fade its 12 lights off when the door closes behind the player (it is recycled anyway) | no visible change (measured) | measured -14 / -12 % draws in 2 of 3 views; visible lights 24→13, 35→24; shadowed 9→5, 13→9 | low | all | 游戏视觉 | none |
| L105 | Box-projected reflection probe per room type: a Custom cube captured once per family (lit state) on a probe that is a child of the room root; turn on probe blending + box projection in the desktop asset; add the two keywords to `FrontRooms/Surface` | gain expected on VCT, carpet sheen, chrome and CRT glass (the room's own troffer rows with parallax); the Run B attempt was dropped because it "never showed a visible gain" (§1.3), so a capture must prove it first | no rendering; per pixel a box test + 1-2 cube samples; ESTIMATE ~1 MB per 128 px HDR cube | medium: probe seams at doors, the frozen wallpaper print in the capture, Surface sampler budget (16 on WebGL) | Desktop (Mobile later; WebGL only if lights + probes ≤ 32) | 游戏视觉 (asset, shader, corridor); 关卡设计 (maze probes) | gain if the capture shows one, else do not ship |
| L106 | Maze: tight culling for lamp shadows: casters within the light's range only, narrower shadowed cone only if a capture shows no change in spill | none intended | maze ~213-264 draws per shadowed lamp today; ESTIMATE a 120° shadowed cone cuts the caster footprint by about two thirds | high: maze shadows are visible (31.8 %); any cone change alters spill through arches | Desktop measure first | 关卡设计 (`FrontRoomsMapWorld.cs`) | none (or do not ship) |
| L107 | Nearest/visible-first light budget with fades (the internal T3 design) for the 32-light tiers | gain on handhelds and WebGL: no dropped overhead lamps, no reshuffle while turning | handheld: 87 → ≤ 31 shaded lights chosen by distance instead of culling order | medium | Mobile, WebGL | 关卡设计 (`TickFixtures`), 游戏视觉 (policy); 设置菜单 (preset hook) | none (desktop path untouched) |
| L108 | Lamp lenses: per-lamp level through Renderer Shader User Value instead of a property block every frame | no visible change | ESTIMATE: up to ~935 maze renderers back into the SRP Batcher; "significantly faster than using Material Property Block" [L26]; measure | low-medium (lens shader edit) | all | 关卡设计 (tick), 游戏视觉 (lens shader) | none |
| L109 | Baked room-local occlusion that travels with the room: AO baked per room family into the shell's UV2 (or into each prop's mask map), read by `FrontRooms/Surface` as indirect occlusion only (lamps stay realtime) | gain: corners, under desks and behind door frames read heavier and steadier than SSAO alone | adds one texture sample on shells; could allow a smaller SSAO radius later (measure) | medium: UV2 on runtime shells, Surface edit, seams at room joins | Desktop first, all later | 游戏视觉 | gain |
| L110 | APV only for geometry that is static in a scene (none today) | — | — | — | — | — | not applicable now |
| L111 | SSR | — | — | undocumented define on 6000.6 | — | — | wait for 6.7 (§3.3) |
| L112 | Maze research spike: light masking by cell reach (Frictional's box masking [L38] on the 3 m grid): each lamp lights only the cells its light can reach through open edges, so unshadowed lamps stop lighting through walls; then fewer lamps need shadow maps for the wall-occlusion look | gain: the ~66 % of maze lamps that never cast shadows stop leaking through walls (dark bands become correct) | if wall occlusion no longer needs maps: part of the measured all-lamp-shadows-off saving (-28..-50 % draws); ESTIMATE about half, to prove by capture | high: per-light data in URP's visible-light order, arches and doors, a custom light loop in `FrontRooms/Surface` | Desktop first | 游戏视觉 (shader, renderer feature), 关卡设计 (reach data) | gain (needs Red's eye: leaked light disappears) |
| L113 | Structures probes (Pool terrain, Pool route, Well, Slide): capture each structure type's cube offline (Custom probe) or render the realtime ones one per frame with Individual Faces slicing, at 256 px or less | no visible change (same lit content; resolution made no clear difference in [L43]) | removes a build-time spike of roughly one +40-70 ms editor hitch per probe rendered in the same frame (ESTIMATE from [L43]; measure) | low | all | 关卡设计 (`FrontRoomsMap/Structures/*`) | none |

How each row would be built, and what to measure in the clone:

**L101.** In `Assets/Scenes/FrontRooms3D.unity`, light `Soft ambient direction` (line 2255 onward): `m_Shadows: m_Type: 2` to `0`. In `FrontRooms3DGame.ApplySceneLighting` (`Assets/Scripts/FrontRooms3DGame.cs:557-565`): `fill.shadows = LightShadows.None`, so the editor build tool (`Assets/Editor/FrontRooms3DBuild.cs:25`) writes the same. Keep intensity, colour and direction: the light's job is its direction, and at 18 % strength its shadow was invisible anyway. Measure: Frame Debugger shows no MainLightShadow pass; the 6 MEASURED views again (pixel diff at noise); one handheld frame. With the fill unshadowed, the `_MAIN_LIGHT_SHADOWS_CASCADE` variants stop being selected at runtime (inference).

**L102.** `FrontRoomsRoomStream.cs:1210` today casts when `fixtureIndex < FixtureZ.Length` (the centre column). Replace it with a per-family decision, e.g. a `ProfileShadowMask(room.rule)` that names the fixtures allowed to cast, defaulting to none for Lobby; re-evaluate when a room changes profile while out of sight. Measure per family (Lobby, Office, Run, Pool) with the MEASURED probe: spot shadows on/off pixel diff from eye-level and from a crouch, plus one view facing a desk, a partition and a door leaf.

**L103.** For the fixtures L102 keeps: `light.spotAngle` about 120 and `light.innerSpotAngle` about 90 on those lights only, `light.range` about 7 m (the far floor corner of a 6 m reach is sqrt(2.85² + 6²) = 6.6 m, ESTIMATE), and per-light soft-shadow quality via `UniversalAdditionalLightData` (which `FrontRoomsStationTvLight.cs:92-99` already does for another light). Leave the 8 side troffers at 162°. Measure: A/B captures of floor pools and lower walls (the centre lamps stop reaching the lower walls 5.75 m away); contact shadows under a desk; casters per shadow map in the Frame Debugger. If the wall change is visible, compensate with the side troffers' intensity, not with the shadowed cone. Optional on top: one Rendering Layer per pool slot (5 slots, under the 8-layer step [L35]) so a room's lamps neither light nor draw casters from the next room [L36]; door leaves and frames carry both neighbours' layers. Test the threshold of an open door for a dark seam before keeping it.

**L104.** The door-shut event that already starts recycling (`FrontRoomsRoomStream`) also ramps the room-behind's 12 lights to 0 over about 0.3 s and disables them; a recycled room re-arms its lights through the existing `ResetRoomLights`. Measure: the MEASURED `behind_lights_off` variant during motion, not only frozen views. The bigger win in the same place, deactivating renderers behind a shut door (-50 to -71 % draws, measured [L44]), belongs to the visibility research, not to this file.

**L105.** Desktop URP asset `Assets/Settings/FrontRooms_URP.asset`: `m_ReflectionProbeBlending: 1`, `m_ReflectionProbeBoxProjection: 1`. `Assets/Resources/Rendering/FrontRoomsSurface.shader`: add `#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING` and `_REFLECTION_PROBE_BOX_PROJECTION`, as `FrontRoomsGlass.shader:169-171` already has (watch the sampler comment at lines 288-290). Corridor: one `ReflectionProbe` (mode Custom, the captured cube for the room's family) as a child of each room root, box = the room interior, blend distance about 0.5 m, `intensity = LinearToGamma(zone linear)` per the glass line; capture with `Assets/Editor/Rendering/FrontRoomsReflectionCapture.cs`. Then raise `FrontRoomsZoneReflection.MaxLinear` only where dead-lamp handling is right [L43]. Measure: probes follow the room after recycle and rebase (UNVERIFIED for baked/custom probes); seam at door thresholds; atlas memory; Forward+ tile cost with probes added.

**L106.** In `FrontRoomsMapWorld.TickFixtures` (`:3839-3866`) nothing changes on desktop until measured. Experiment in the clone: count casters per shadowed lamp in the Frame Debugger; compare with renderers within 10 m of that lamp. If URP draws casters beyond the range, a per-block `shadowCastingMode` switch for blocks out of every shadowed lamp's range is the cheap fix (map code). The narrower cone is a look change in the maze; ship it only if captures show no change.

**L107.** As designed in [L40] T3, under a handheld/WebGL branch of `TickFixtures` whose `else` is today's code; `FrontRoomsQuality` exposes the cap per preset. Measure on an iPhone build: visible lights ≤ 31 every frame, no lamp within 3 m unlit, no popping while turning.

**L108.** `FrontRoomsMapWorld.TickFixtures` writes `_EmissionColor` and `_FR_LampDip` through `SetPropertyBlock` every frame for every fixture (`:3862-3865`). Instead: `MeshRenderer.SetShaderUserValue(packed)` with the lamp level (and dip) packed into 32 bits, read in the lens shader from `unity_RendererUserValue` (`UnityInput.hlsl:177` in URP 17.6) [L26][L45]. Measure: SetPass and draws in the maze views; the lens look at 0.04 floor glow (the minimum the code writes).

**L109.** Bake AO per room family in an editor scene (the family layouts are fixed), store it in a UV2 channel generated for the shell meshes, and multiply only the indirect (ambient and probe) term in `FrontRooms/Surface`, never the lamp's direct light, so dead lamps stay right. Measure: A/B with SSAO on (no double darkening), seams at the door pair between rooms.

**L112.** The reach set per lamp is the "light reach" flood fill already designed in [L40] T3 (from the lamp's cell through non-wall edges out to the light's range, recomputed when a door changes). Encode it as a bitmask over the 7 x 7 cells around the lamp (49 bits, two uints). In `FrontRooms/Surface`'s additional-light loop (which already filters lights for `_FR_OWN_LIGHTS`, lines 49 and 666 [L47]), the pixel computes its cell offset from the lamp's cell, nudged along the surface normal so a wall face belongs to its own cell, and skips the light if the bit is clear. The hard part is getting per-light data to the shader in URP's visible-light order; a renderer feature would have to rebuild it every frame from URP's culled light list (UNVERIFIED that this order is stable within a frame). Rendering Layers [L35][L36] are the supported filter, but they work per renderer, and the maze merges walls of several cells into one 6 m block mesh [L40] §1.2, so they cannot express per-cell reach without splitting meshes. Measure first in the clone: the maze views with shadows off plus masking against base (pixel diff), and draws.

**L113.** `FrontRoomsStructuresLook.Probe` (`:172-192`) and its flush (`:267-275`): either keep Realtime but render one pending probe per frame with `ReflectionProbeTimeSlicingMode.IndividualFaces`, or capture each structure type once with `FrontRoomsReflectionCapture` and switch the probe to Custom with that cube. The callers pass 512 (`FrontRoomsStructuresPoolTerrain.cs:78`, `FrontRoomsStructuresPoolRoute.cs:268`) or 1024 (`FrontRoomsStructuresWell.cs:90`, `FrontRoomsStructuresSlide.cs:113`). Measure: the worst frame after a structure build, before and after, and a pixel diff of a glossy surface in each structure. This only matters for the look once L105's asset and shader changes make box projection work.

**Not recommended now.** APV (nothing static to bake, §2.6). Realtime reflection probes in play (+47 ms unsliced, [L43]). Compiling in URP 17.6's hidden SSR or Surface Cache (undocumented defines, §1.4, §3.3). A custom cached-shadow pass (L effort, §5.5).

## Sources

| id | what (title, author/studio, venue, year) | URL | how read | notes |
|---|---|---|---|---|
| L1 | "Preview of Screen Space Reflections for URP", Unity Discussions thread opened by Unity staff (Pema-Malling), 2026-05-29 to 2026-09-23 | https://discussions.unity.com/t/preview-of-screen-space-reflections-for-urp/1721494 | read (pages 1-3) | Unity staff statements only are used: target 6.7, preview from 6000.6.0a7, "No plans to backport", 6000.7.0b1 with MSAA |
| L2 | "New in Unity 6.6", Unity Manual 6000.6 (and the "New in Unity 6.0 Preview" page under the same manual) | https://docs.unity.com/en-us/engine/6000.6/manual/whats-new/unity66 | read | no URP SSR, no cached shadows; dynamic batching obsolete; Unity Compute Light Baker; 6.0 items: APV scenario blending and sky occlusion in URP, config package for light limits |
| L3 | Unity 6000.6.0 release notes | https://unity.com/releases/editor/whats-new/6000.6.0 | read (characters 0-200,000 of 593,229) | "Adjusted SSR roughness...", Surface Cache items, XR quad-view shadow caching, HDRP contact shadow fix, GLES light count fix |
| L4 | "Lighting in URP" index, Unity Manual 6000.7 (beta) | https://docs.unity.com/en-us/engine/6000.7/manual/lighting-overview/lighting | read | no SSR page; Surface Cache GI override page exists |
| L5 | "Lighting in URP" index, Unity Manual 6000.6 | https://docs.unity.com/en-us/engine/6000.6/manual/lighting-overview/lighting | read | no SSR page, no Surface Cache page |
| L6 | "Light limits in URP", Unity Manual 6000.6 | https://docs.unity.com/en-us/engine/6000.6/manual/lighting-overview/lighting/light-limits-in-urp | read | 9 per object (Forward); 256 / 32 / 16 per camera |
| L7 | "Troubleshooting the Forward+ rendering path in URP", Unity Manual 6000.6 | https://docs.unity.com/en-us/engine/6000.6/manual/render-pipelines/universal-render-pipeline/introduction/urp-concepts/rendering-paths/forward-plus-rendering-path-limitations | read | changing MAX_VISIBLE_LIGHT_COUNT via embedded config package |
| L8 | "Choose a rendering path in URP", Unity Manual 6000.6 | https://docs.unity.com/en-us/engine/6000.6/manual/render-pipelines/universal-render-pipeline/introduction/urp-concepts/rendering-paths/comparison | read | comparison table; Forward+ for blending more than 2 probes |
| L9 | "Forward and Forward+ rendering paths in URP", Unity Manual 6000.6 | https://docs.unity.com/en-us/engine/6000.6/manual/render-pipelines/universal-render-pipeline/introduction/urp-concepts/rendering-paths/forward-rendering-paths | read | tiles; settings Forward+ ignores |
| L10 | "Introduction to Adaptive Probe Volumes", Unity Manual 6000.6 | https://docs.unity.com/en-us/engine/6000.6/manual/lighting-overview/lighting/probevolumes/concept | read | bricks 4x4x4, spacing 1/3/9/27 m, 8-probe per-pixel sampling, moved geometry breaks shared data |
| L11 | "Use Adaptive Probe Volumes", Unity Manual 6000.6 | https://docs.unity.com/en-us/engine/6000.6/manual/lighting-overview/lighting/probevolumes/use | read | setup steps |
| L12 | "Probe Volumes Options Override reference", Unity Manual 6000.6 | https://docs.unity.com/en-us/engine/6000.6/manual/lighting-overview/lighting/probevolumes/options-override-reference | read | leak reduction modes; World Offset not documented |
| L13 | Adaptive Probe Volumes (APV) in URP, section index, Unity Manual 6000.6 | https://docs.unity.com/en-us/engine/6000.6/manual/lighting-overview/lighting/probevolumes | read | no requirements/platform page |
| L14 | "New ways of applying global illumination to your worlds in Unity 6", Steven Kent (Unity), Unity Blog, 2024-07-11 | https://unity.com/blog/engine-platform/new-ways-of-applying-global-illumination-in-unity-6 | read | scenarios incl. lights on/off, sky occlusion, streaming, leak tools; no memory or platform figures |
| L15 | `Renderer.lightmapScaleOffset`, Unity Scripting Reference 6000.6 | https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/renderer/lightmapscaleoffset | read | set by "a custom script that brings external lightmapping data" |
| L16 | `LightProbes.Tetrahedralize`, Unity Scripting Reference 6000.6 | https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/lightprobes/tetrahedralize | read | rebuilt after additive loading |
| L17 | `LightProbeUsage`, Unity Scripting Reference 6000.6 | https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/rendering/lightprobeusage | read | CustomProvided from the property block |
| L18 | `MaterialPropertyBlock`, Unity Scripting Reference 6000.6 | https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/materialpropertyblock | read | "not compatible with SRP Batcher"; CopySHCoefficientArraysFrom |
| L19 | "Lighting Mode", Unity Manual 6000.6 | https://docs.unity.com/en-us/engine/6000.6/manual/lighting-overview/lighting-light-sources/components/configuring/lighting-mode/lighting-mode | read | modes table; 4 lights per texel |
| L20 | "Reflection Probes in URP", Unity Manual 6000.6 | https://docs.unity.com/en-us/engine/6000.6/manual/lighting-overview/lighting/reflection-probes/introduction | read | per-pixel blending, 2 per object, Forward+ indirect draws |
| L21 | "Optimize reflections", Unity Manual 6000.6 | https://docs.unity.com/en-us/engine/6000.6/manual/lighting-overview/reflections/ref-probe-performance | read | realtime cost, time slicing 9 / 14 frames |
| L22 | "Optimize shadow rendering in URP", Unity Manual 6000.6 | https://docs.unity.com/en-us/engine/6000.6/manual/lighting-overview/lighting/shadows-in-urp/shadows-optimization | read | toggle + fade lights, cookies, point = 6 spots, Shadowmask, soft shadows on TBDR |
| L23 | "Configure shadow resolution in URP", Unity Manual 6000.6 | https://docs.unity.com/en-us/engine/6000.6/manual/lighting-overview/lighting/shadows-in-urp/shadow-resolution-urp | read | atlas packing |
| L24 | "Shadows in URP" index, Unity Manual 6000.6 | https://docs.unity.com/en-us/engine/6000.6/manual/lighting-overview/lighting/shadows-in-urp | read | SSAO filed under shadows; no caching page |
| L25 | "Update shadows less frequently", HDRP 17.0.4 manual | https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.0/manual/shadow-update-mode.html | read | HDRP update modes and cache |
| L26 | "Introduction to RSUV" (Renderer Shader User Value), Unity Manual 6000.6 | https://docs.unity.com/en-us/engine/6000.6/manual/materials-and-shaders/shaders/renderer-shader-user-value/intro | read | per-renderer uint, "significantly faster than using Material Property Block" |
| L27 | "Naughty Dog at SIGGRAPH 2020" (abstract of "Lighting Technology of The Last of Us Part II", Hawar Doghramachi), Naughty Dog blog, 2021-02-11 | https://www.naughtydog.com/blog/naughty_dog_at_siggraph_2020 | read | baked ambient lighting at 30 fps, PS4 memory |
| L28 | Advances in Real-Time Rendering, SIGGRAPH 2016 course page (Treyarch volumetric GI; Naughty Dog "Deferred Lighting in Uncharted 4"; id "The devil is in the details") | https://advances.realtimerendering.com/s2016/index.html | read (abstracts) | abstracts only |
| L29 | "Multi-Scale Global Illumination in Quantum Break", Ari Silvennoinen and Ville Timonen (Remedy), SIGGRAPH 2015 Advances | https://www.advances.realtimerendering.com/s2015/SIGGRAPH_2015_Remedy_Notes.pdf | [search] snippet only | PDF over the fetch size limit; irradiance volumes |
| L30 | Bakery GPU Lightmapper "Lightmapped Prefab" component; community prefab-lightmapping scripts | https://geom.io/bakery/wiki/ ; https://github.com/unitycoder/PrefabLightmapping | [search] snippet only | pointer only, no fact relies on it |
| L31 | "Forward+: Bringing Deferred Lighting to the Next Level", Takahiro Harada, Jay McKee, Jason C. Yang (AMD), Eurographics 2012 short paper (author page) | https://takahiroharada.github.io/forward+ | read | 16 x 16 tiles, per-tile light lists, faster than compute deferred |
| L32 | "Clustered Deferred and Forward Shading", Ola Olsson, Markus Billeter, Ulf Assarsson, HPG 2012 | https://research.chalmers.se/en/publication/161725 | read (abstract) | depth clusters beat tiles at depth discontinuities |
| L33 | Advances in Real-Time Rendering, SIGGRAPH 2017 course page: "Precomputed Lighting in Call of Duty: Infinite Warfare" (Iwanicki, Sloan, Activision); "Improved Culling for Tiled and Clustered Rendering" (Drobot, Infinity Ward); "Optimized pixel-projected reflections for planar reflectors" (Cichocki, Microsoft) | https://advances.realtimerendering.com/s2017/index.html | read (abstracts) | light grid and probes for dynamic entities at 60 Hz; z-binning; PPR |
| L34 | "Harness Apple GPUs with Metal", Apple, WWDC20 session 10602 | https://developer.apple.com/videos/play/wwdc2020/10602/ | read (transcript) | tile shading for light culling, tile memory, MSAA resolve on chip |
| L35 | "Introduction to Rendering Layers in URP", Unity Manual 6000.6 | https://docs.unity.com/en-us/engine/6000.6/manual/lighting-overview/lighting/rendering-layers/introduction | read | lights-only cost "insignificant" in Forward; steps at 9/17/25 layers |
| L36 | "Enable Rendering Layers for Lights" (Custom Shadow Layers), Unity Manual 6000.6 | https://docs.unity.com/en-us/engine/6000.6/manual/lighting-overview/lighting/rendering-layers/lights | read | the layer mask also filters shadow casters |
| L37 | "Virtual Shadow Maps", Unreal Engine 5.8 documentation, Epic Games | https://dev.epicgames.com/documentation/en-us/unreal-engine/virtual-shadow-maps-in-unreal-engine | read | page caching and invalidation, static/dynamic split, local-light advice |
| L38 | "Tech feature: Light Masking", Thomas Grip, Frictional Games blog, 2010-12-02 | https://frictionalgames.blogspot.com/2010/12/tech-feature-light-masking.html | read | box-limited lights instead of shadows against bleeding through walls |
| L39 | Ray Tracing Gems II, chapter 46 (ray tracing in Control, NVIDIA and Remedy), 2021 | https://developer.download.nvidia.com/ray-tracing-gems/rtg2-chapter46-preprint.pdf | [search] snippet only | PDF over the fetch size limit; precomputed voxel GI from static objects and selected lights |
| L48 | "Surface Cache Global Illumination Volume Override reference for URP", Unity Manual 6000.7 (beta) | https://docs.unity.com/en-us/engine/6000.7/manual/lighting-overview/lighting/surface-cache-gi-volume-override-reference | read | realtime indirect diffuse without baking; compute required; HW RT only on DX12 |
| L40 | internal: `Frontrooms3D/Documentation/research/webgl/04_techniques.md` §1.3, §1.8, T3-T5 (2026-10-02) | project file | read | light budget, shadow caching, light LOD; reused, not redone |
| L41 | internal: `Frontrooms3D/Documentation/research/webgl/03_measured_budgets.md` §0, §3-4 (measured 2026-10-02/03) | project file | read | 32-light cap analysis, per-lever deltas |
| L42 | internal: `Frontrooms3D/Documentation/research/interaction_audit/03_rendering_quality.md` §1.6-1.8 (2026-10-02) | project file | read | bias arithmetic, GI/probe state |
| L43 | internal: `Frontrooms3D/Documentation/research/glass/11_reflections_and_raytracing.md` §0, §2.1-2.5 (2026-10-02/03) | project file | read | zone cubes, G7, planar and realtime-probe costs, SSR analysis |
| L44 | internal: `/Users/redwang/FrontRoomsVisualWork/tech_art/MEASURED.md` (clone proj_build, 2026-11-04) | local file | read | the measured shadow, light and SSAO rows |
| L45 | internal: URP 17.6.0 package source, `Frontrooms3D/Library/PackageCache/com.unity.render-pipelines.universal@327d93d1cbc8/` (`ShadowUtils.cs`, `UniversalRenderPipeline.cs`, `UniversalRenderPipelineCore.cs`, `ShaderLibrary/Input.hlsl`, `GlobalIllumination.hlsl`, `UnityInput.hlsl`, `RendererFeatures/ScreenSpaceReflectionRendererFeature/*`, `RendererFeatures/SurfaceCacheGIRendererFeature/*`, `Runtime/*.asmdef`), and `com.unity.render-pipelines.universal-config@561422b74439/Runtime/ShaderConfig.cs` | project package cache | read (grep) | the version actually compiled into this project |
| L46 | internal: Core 17.6 package source, `Frontrooms3D/Library/PackageCache/com.unity.render-pipelines.core@02d75770e4b3/Runtime/Lighting/ProbeVolume/` (`ProbeReferenceVolume.cs`, `ProbeBrickPool.cs`, `ProbeVolumesOptions.cs`) | project package cache | read (grep) | APV compute gating, World Offset |
| L47 | internal: project code and assets read 2026-11-04: `FrontRooms3DGame.cs:552-566`, `FrontRooms3D.unity:2255-2296`, `FrontRoomsRoomStream.cs:15-68, 1181-1213, 1502, 2146, 2171-2178`, `FrontRoomsMap/FrontRoomsMapWorld.cs:3712-3730, 3822-3866`, `FrontRoomsMapWorld.Pool.cs:715-735`, `FrontRoomsLevelProfile.cs:37-39`, `FrontRoomsQuality.cs:205-241`, `Rendering/FrontRoomsZoneReflection.cs:1-50`, `FrontRoomsMap/Structures/FrontRoomsStructuresLook.cs:160-192, 267-275` and its four callers, `Rendering/FrontRoomsRunRig.cs:1296-1340`, `Rendering/Stairwell/FrontRoomsStairwellDresser.cs:692-720`, `Rendering/FrontRoomsStationTvLight.cs:92-107`, `Resources/Rendering/FrontRoomsSurface.shader:40-52, 177-206, 288-290, 666, 1008`, `FrontRoomsGlass.shader:169-171`, `Settings/FrontRooms_URP.asset:35-84`, `ProjectSettings/ProjectSettings.asset:836-850`, scene YAML of all five scenes | project files | read (grep, sed) | facts about FrontRooms today |
