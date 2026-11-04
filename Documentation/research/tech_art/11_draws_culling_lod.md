# 11 · Draw calls, culling, LOD/HLOD, texture streaming and budgets

- Line: 技术美术调研 (ledger `tech_art`), session local_ad6f36ec. Topic agent 11 (source prefix D).
- Stage: research draft (complete; every section filled).
- Date: 2026-11-04.
- Red's ask (via 总控, 2026-11-04), verbatim: "在做无限走廊的时候，我需要你也开启一个task用来 research专业的Technical artist会怎么做，提高画质表现的同时，节省性能。"
- What was done: web reading only (WebSearch / WebFetch); main read only (project files and the local URP / Core 17.6.0 package source under `Frontrooms3D/Library/PackageCache`, nothing written there); the clone measurements in `tech_art/MEASURED.md` (DI5, made by another agent on 2026-11-04) are used in §6 wherever they apply.
- What was not done: no Unity run, no clone measurement by this agent, no download, no git writes. Savings not covered by DI5 are ESTIMATE with the arithmetic shown.
- Incidents: (1) the work stopped once on a network outage (ENOTFOUND, about 10:47-11:25) and resumed from this file. (2) One WebFetch of a SIGGRAPH PDF (D33) returned binary, and the WebFetch tool itself auto-saved a copy in this session's tool-results cache (`~/.claude/projects/.../tool-results/webfetch-1793817562713-2rkd4u.pdf`). It was not opened or used; it is reported so 总控 can decide about it (not deleted: deleting is Red's).
- Id convention in this file: sources are `D1`..`D46` (web and package source) and `DI1`..`DI5` (internal documents); candidate changes in §6 are `DA1`..`DA13`.
- Unity docs note: Unity moved its manual to `docs.unity.com/en-us/engine/6000.6/...` during 2026. The 6000.6 pages (labelled "Current" when read; a 7000.0 manual also exists) were read wherever they exist, so most Unity facts below are stated for 6000.6, the project's version.
- Corrections to the common brief found while reading main: (a) the URP asset's LOD cross-fade dither is Blue Noise, not Bayer (`m_LODCrossFadeDitheringType: 1`, `Assets/Settings/FrontRooms_URP.asset:34`; enum order BayerMatrix, BlueNoise, Stencil in URP 17.6 `Runtime/Data/UniversalRenderPipelineAsset.cs:369-380` [D13]). (b) The three NPOT 2048x3072 wallpaper maps are no longer in `Resources`; the shipped wallpaper set is POT now (`Wallpaper_Paper_M/N/S`, `Wallpaper_Print_P` 2048², `Wallpaper_Substrate_N` 1024x512, PNG headers read 2026-11-04); the Chevron maps sit under `Assets/Editor/Rendering/PrintP0/Ref/`, as reference copies outside `Resources`. 12 NPOT prop maps remain (§5.3). (c) SSAO no longer doubles draws: SSAO off removes 4 draws (DI5).

---

## 0. Short answer

1. **In the title corridor, "closed door = closed portal" is the largest saving with no visible change.** Rooms behind a shut door still draw today, and still cast into shadow maps, because Unity culls by frustum only. Switching those rooms off cut draw calls by 50 %, 71 % and 50 % in three measured views, with image change at the noise floor (DI5). This is the rule shipped portal engines use: a Doom 3 visportal inside a door closes with the door (via DI2), and a Source areaportal follows its door's state (D20).
2. **GPU Resident Drawer (GRD) and GPU occlusion culling are Unity 6's built-in answer on Metal (Mac, iPhone, iPad), D3D12 and Vulkan, but not WebGL2** (D1, D4, D13). Today GRD cannot start: BatchRendererGroup variants are stripped (`m_BrgStripping: 0`) and `FrontRooms/Surface`, used by 83 of 86 surface materials, has no DOTS-instancing variant (DI5, D19). Any renderer with a MaterialPropertyBlock (MPB) also falls back and gets no GPU occlusion culling (D1, D3); about 935 of ~3,000 in-frustum maze renderers have one (DI5).
3. **MPBs are the project's main batching breaker.** The SRP Batcher refuses renderers with an MPB (D6). Unity's own URP advice is to avoid the MPB API and use material variants instead (D8). Two patterns cause most of them: the maze shell's per-renderer `_CeilingHeight` and the per-lamp emission blocks. Both can be replaced with no visible change (DA5, DA6).
4. **Nothing in the project is statically batched or instanced** (DI5). With GRD on, static batching must stay off; it is "not compatible with BRG or GPU Resident Drawer" (D8). On WebGL, which has no GRD, merging each room's static meshes is the way to cut draws, because a room is culled as a unit by the door rule anyway (DA10).
5. **Shadow maps are where the draws go.** Turning off the shadow of the directional fill light ("Soft ambient direction", shadow strength 0.18) removed 12-16 % of draws in both the corridor and the maze, below the 8/255 diff threshold in all six views (DI5). Spot shadows were invisible in the empty Lobby corridor views but clearly visible in the maze (DI5). These need Red's eye and a finer diff before anything is removed (DA7, DA8; lighting detail in `13_lighting.md`).
6. **LOD today is hard swaps.** Every LODGroup has fade mode None, and no project shader has a `LOD_FADE_CROSSFADE` variant, although the URP asset has cross-fade on (§4.1). Turning on cross-fade is a desktop quality gain that costs a few extra draws during fades (DA9). Mesh LOD (new in Unity 6.2) is optional for kits without authored LODs. Switch points should follow the one-pixel-error rule pros use (D31, D32). Indoors, HLOD is not needed once door/portal culling exists.
7. **Textures.** Mipmap streaming cannot serve the world-projected `FrontRooms/Surface` walls, floors and ceilings: Unity cannot compute a mip level for shaders that do not use UV0, and falls back to low mips (D35). So use streaming only for UV-mapped kit props, and only on handhelds (DA12). 12 NPOT prop maps (54.6 MiB if held as RGBA32) should become POT (DA11). Formats: BC7 / BC5 on desktop, ASTC on iOS, and dual DXT + ASTC Web builds (D37-D39). On macOS browsers, BC7 is decompressed to RGBA32 at load (D38).
8. **Budgets.** Apple publishes no per-device memory limit. An app reads its headroom with `os_proc_available_memory` and is killed on "per-process-limit" (D40, D42). The WebGL heap is capped at 1024 MB in this project (`Assets/Editor/FrontRoomsCloudBuild.cs:204`); Unity's default maximum is 2048 MB and its ceiling is 4 GB (D43).

---

## 1. Draw-call cost model

### 1.1 What a draw costs, and why state changes cost more than draws

- Unity splits a draw into two steps. "Update the render state" (shader code, textures, buffers) is what Unity calls "the most CPU-intensive step"; "Submit a draw call" is the other (D14).
- The SRP Batcher's author says the same: "the real CPU cost comes from that setup, not from the GPU DrawCall itself" (D15).
- Metal: Apple says argument buffers "use less overhead than assigning each resource individually". Encoding resources into them "eliminates the need for the Metal driver to capture state and track residency" for each bound resource (D16). So the cost Apple targets is binding and state tracking, not the draw itself.
- D3D12 and Vulkan bake state into pipeline state objects (PSOs). NVIDIA advises to "Arrange draw calls by PSO". Switching between compute and graphics, or turning tessellation on or off, are "heavyweight" switches. PSO creation stalls on shader compiles, so create PSOs early on worker threads (D17). In Unity terms, a PSO build is a shader-variant first-use hitch, separate from the per-frame SetPass cost (inference).
- WebGL2: each GL call crosses from WebAssembly into JavaScript and the browser's validating GL layer. In this project that measured about 40 us of main-thread time per draw; the title corridor was bound by draw count (DI3 §7, §9.1).
- The FrontRooms numbers today (DI5, M3 Max editor, desktop Ultra):

  | Frame | Draw calls | SetPass | Draws per SetPass |
  |---|---:|---:|---:|
  | Title corridor, 3 views | 833-952 | 74-104 | 9.2-11.3 |
  | Level 0 maze, 3 views | 4,842-7,379 | 1,176-1,564 | 4.1-4.7 |

  The maze changes state more than twice as often per draw (ESTIMATE from the table: 4.1-4.7 against 9.2-11.3). That pattern fits MPB renderers leaving the SRP Batcher path; inference, to be confirmed by DA5/DA6.
- No static batching, no GPU instancing and no dynamic batching happen anywhere: Unity's batched-draw counters are 0 in every view, and everything goes through the SRP Batcher (DI5).

### 1.2 What the SRP Batcher batches

- It does not reduce the number of draws. It "reduces render-state changes between draw calls" by grouping `bind` + `draw` command sequences into SRP batches (D5).
- Material data persists in GPU memory: "If the material content doesn't change, the SRP Batcher doesn't make any render-state changes". Per-object engine data goes into "a large per-object GPU constant buffer" (D5).
- The batch key is the shader variant, not the material: "use as few shader variants as possible. You can still use as many different materials with the same shader as you want" (D5). In the 2019 blog's words, when "the shader variant has changed, ... we have to break the batch" (D15).
- Measured gain Unity published: 1.2x to 4x faster CPU rendering, depending on the scene. Book of the Dead on PS4 was 1.47x faster, with no FPS change because it was GPU bound (D15). This is render-thread CPU time, not frame rate.

### 1.3 What breaks it

- Renderer side: "The GameObject mustn't use MaterialPropertyBlocks". It must be a mesh or skinned mesh, not a particle (D6).
- Shader side: all built-in engine properties must sit in one `UnityPerDraw` constant buffer, and all material properties in one `UnityPerMaterial` buffer (D6). URP Lit and Unlit qualify; their particle versions do not (D6).
- Variant side: each keyword set is a different variant, so a keyword change between consecutive draws starts a new SRP batch. The Frame Debugger names the reason, for example "Nodes have different shaders". Many small SRP batches "often" mean too many shader variants (D7).
- Unity's URP advice: avoid the MaterialPropertyBlock API. For the same mesh with different properties, use Material Variants (D8).

### 1.4 How to check

- Shader: the shader Inspector shows whether the shader is SRP Batcher compatible (D6).
- Frame: Frame Debugger > Render Camera > Render Opaques > `RenderLoopNewBatcher.Draw`. Each "SRP Batch" entry shows its draw count, its keywords and why it did not join the previous batch (D7).
- CPU: Unity's SRPBatcherProfiler script splits "SRP Batcher code path" time from "Standard code path" time. In a fully compatible scene the standard path should be close to zero (D15).
- Project facts (read-only): `FrontRooms/Surface` declares one `CBUFFER_START(UnityPerMaterial)` in the SubShader-wide `HLSLINCLUDE`, so every pass shares one layout (`Assets/Resources/Rendering/FrontRoomsSurface.shader:105-159`). Its comments say properties sit "in every pass, so the SRP batcher layout never changes" (`:144-151`). The shader is built to be compatible; what breaks batching here is MPBs on renderers. A smaller breaker: `_FR_MESH_UV` is a `multi_compile_local` keyword (`:177`), so mesh-UV and world-projected materials of this shader are different variants and cannot share an SRP batch.

### 1.5 Platform notes

- The SRP Batcher can run multithreaded only on "DX12, Vulkan, and compatible console APIs ... if you enable Graphics Jobs" (D8). On Metal (Mac, iOS) it runs on the render thread.
- WebGL2: whether the SRP Batcher path is active was UNVERIFIED in October (DI2 T7), and it is UNCONFIRMED FOR 6000.6 here: the 6000.6 page lists support per pipeline only (D5). Check for "SRP Batch" entries in a WebGL development build.

---

## 2. GPU Resident Drawer, GPU occlusion culling, static batching, instancing

### 2.1 What the GPU Resident Drawer is

- It is a GPU-driven path for ordinary MeshRenderers. It converts GameObjects into BatchRendererGroup (BRG) draws with instancing. The conversion is persistent and incremental: it runs once per frame after LateUpdate and updates only changed objects (D11).
- Runtime content is in scope. "If you change or create GameObjects each frame, the GPU Resident Drawer updates with the changes" (D1). The rooms of this game are built at runtime, so that matters. Static and dynamic MeshRenderers both qualify; no static flag is needed (D1, D11).
- In the Frame Debugger its draws are named "Hybrid Batch Group" (D2).
- The cost of registering a large batch of new renderers in one frame is not published; UNCONFIRMED FOR 6000.6. A maze chunk build already took about 18 ms in October (DI3 §2.1), so measure a chunk-build frame with GRD on and off.

### 2.2 Requirements (Unity 6.6)

- Pipeline: renderers on Forward+ or Deferred+ only; graphics APIs with compute shaders, "except OpenGL ES and VisionOS"; Enlighten realtime GI off (D1).
- Per MeshRenderer: no MaterialPropertyBlock; Light Probes not set to "Use Proxy Volume" and no Anchor Override; default sorting layer and order; materials that support BRG; at most 128 materials; no Text Mesh (D1). No MonoBehaviour with `OnWillRenderObject`, `OnBecameVisible` or `OnBecameInvisible` (D1), or any other per-instance callback such as `OnRenderObject` (D3). The object must not move between two cameras' renders (D3).
- The 6000.6 page's list also includes "GameObjects that are in the hierarchy of Animation or Animator components" among the things GRD works with (D1). That reads like a missing "not"; treat animated hierarchies as UNCONFIRMED FOR 6000.6. Skinned meshes, VFX Graph and particles are not supported (D11).
- Shader: "Uses a shader that supports DOTS instancing" (D3). "Every shader that BRG uses must support DOTS Instancing" (D19). In URP that is one `#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"` per pass. It adds `#pragma multi_compile _ DOTS_INSTANCING_ON`, with shader target 4.5 on Metal, D3D12 and Vulkan and 3.5 on Android, UWP and WebGL other than WebGPU (URP 17.6 `ShaderLibrary/DOTS.hlsl`, used by `Shaders/Lit.shader:186, 224, 324...` [D13]).
- Project setting: Graphics > Shader Stripping > "BatchRendererGroup Variants" = Keep All; the SRP Batcher on; the URP asset's GPU Resident Drawer = Instanced Drawing; Forward+ (D1). URP 17.6's validator refuses to start GRD when the stripping mode is not Keep All (`GPUResidentDrawer.Validator.cs:61-66` in Core 17.6.0 [D13]). It also refuses on any platform whose `BatchRendererGroup.BufferTarget` is not RawBuffer (`:44-49` [D13]), which excludes the constant-buffer path GLES and WebGL use (inference from D1 and D13).
- Unsupported features: LOD Group animated cross-fading "falls back to static, distance-based cross-fading"; `Light.shadowMatrixOverride` does not affect shadow-caster culling (D1).
- Side effects: longer builds, because all BRG variants compile. Probe Atlas Blending is used by default with Forward+ and GRD (D1). FrontRooms uses legacy light probes, so check whether this changes probe-lit objects (inference).
- Opt-out per object: add a "Disallow GPU Driven Rendering" component (D3).
- FrontRooms today: GRD mode 0 (`Assets/Settings/FrontRooms_URP.asset:86`), GPU occlusion off (`:88`), `m_BrgStripping: 0` (`ProjectSettings/GraphicsSettings.asset:49`). No project shader includes `DOTS.hlsl` or declares `DOTS_INSTANCING_ON` (grep of `Assets/**/*.shader`, `*.hlsl`). In the clone, GRD logged that BatchRendererGroup Variants "must be Keep All" (DI5). No script in `Assets/Scripts` uses `OnBecameVisible`, `OnBecameInvisible`, `OnWillRenderObject` or `OnRenderObject` (grep), so that rule blocks nothing.

### 2.3 Measured savings Unity published

- Unity's Unity 6 beta highlights (a Unity staff post): "up to 50%" lower CPU frame time for GameObjects in large, complex scenes, and an image of the Fantasy Kingdom demo dropping from 137K to 13K batches (D12; search snippet only).
- Unity's engineering thread: the gain scales with how much can be instanced. Large scenes may roughly halve CPU frame time; small scenes gain little; CAD-like scenes of many unique meshes gain little (D11).
- Unity 6.6 manual: GRD is most effective with "numerous objects" and when many objects share "the same mesh and the same material shader variant" (D2). It "slightly increases GPU workload", more on lower-end mobile, so a GPU-bound app can get slower (D2). Merged instanced draws can raise overdraw on non-tiled desktop GPUs; Unity suggests Depth Priming Auto or Forced with Forward+ (D2). This project runs MSAA 4x, and URP does not support depth priming with MSAA (via DI2 T10), so that remedy is not available on desktop (inference).
- What this means here (inference): maze shell meshes are unique per 6 m block, so GRD cannot instance them; it only lowers their submission cost. Repeated kit meshes (troffer housings, chairs, monitors, door plates) are the instancing candidates.

### 2.4 GPU occlusion culling (Unity 6.6)

- How it works: the GPU makes depth textures from each camera's view and uses "the depth textures from the current and previous frames to cull objects. Unity renders only objects that are tested as unoccluded in either frame" (D4).
- Test shape: each object is a bounding sphere ("thin or elongated objects have a much coarser spherical representation"). The sphere is tested against a level of a downsampled depth buffer chosen by its projected size (D4).
- URP 17.6 runs it in two phases inside the depth prepass when one exists. First "test everything against previous frame final depth pyramid", then "re-test culled against current frame intermediate depth pyramid" (`Runtime/UniversalRendererRenderGraph.cs:1088-1089`, repeated at `:1172-1173` and `:1221-1222`; `:1040-1050` is the prepass condition [D13]). Ubisoft presented the same idea for Assassin's Creed Unity in 2015: occlusion depth from a downsampled depth buffer combined with last frame's reprojected depth, and "two-phase occlusion culling" in the course agenda. Their PS4 cost was about 100 us to downsample depth, 50 us to combine it with the reprojected depth and 50 us to build the hierarchy (D33, search snippet only).
- It helps when many objects share a mesh, when there is a lot of occlusion (above all of high-vertex objects), and when occluded objects are small on screen. Where occlusion is small, "rendering time might increase" (D4).
- Requirements: GRD, "and restrictions apply similarly" (D4); enable GPU Occlusion Culling on the URP Asset under GPU Resident Drawer (`m_GPUResidentDrawerEnableOcclusionCullingInCameras`, `FrontRooms_URP.asset:88`); the 6000.6 manual names the renderer, but URP 17.6 has no renderer-level field (`UniversalRenderPipelineAssetUI.Drawers.cs:193`; claim check 15 #2). URP 17.6 also turns it off on every Qualcomm GPU, by vendor id, after a Vulkan driver issue (`UniversalRendererRenderGraph.cs:729-748` [D13]). That matters only on Qualcomm GPUs (for example Snapdragon Windows laptops); Macs, iPhones and iPads are not affected.
- Scope (inference from D1, D4 and the URP setting name `m_GPUResidentDrawerEnableOcclusionCullingInCameras`): it culls GRD instances for cameras. It does not cull MPB renderers, lights, or shadow-caster passes.
- For FrontRooms (inference): a shut door leaf fills the doorway in depth, so floors, ceilings, props and troffers behind it are what GPU occlusion culling removes. Walls are long thin boxes (11.5 x 2.9 x 0.26 m) and are poor occludees as spheres, but they still occlude, because occluders are whatever reached the depth buffer. It does nothing for lights or shadow maps, which is where the maze spends its draws (DI5). So it complements portal culling of lights (DA13); it does not replace it.

### 2.5 Static batching and `StaticBatchingUtility.Combine` at runtime

- Static batching "combines multiple static meshes into one vertex buffer and one index buffer, using coordinates in world space". Each buffer holds up to 64,000 vertices, and Unity can create several batches. Mesh Renderers only (D9).
- At runtime, `StaticBatchingUtility.Combine` prepares already-built objects. It is "especially useful for procedurally generated meshes". The meshes must have read/write enabled. The batch root's transform can move; the children's cannot (D10).
- The originals stay "still present in the Scene", so Unity still culls them one by one (D18).
- Conflicts: not compatible with GRD or BRG (D8). Mesh LOD always uses LOD0 under static batching (D26). Unity's 6000.6 advice for URP is SRP Batcher + GRD, with static batching off (D8).
- Fit for FrontRooms (inference): a pooled stream room moves its root on recycle (`FrontRoomsRoomStream.cs:997`), which a static batch allows. But its door leaves swing, its fixtures are re-aligned on every recycle (`AlignFixtures`, `:1244-1256`), and its profile variants switch on and off. Only the shell boxes and reveals would stay put, and kit meshes would need read/write copies in memory. On desktop and iOS, GRD instancing is the better tool. On WebGL, which has no GRD, a manual per-room merge beats static batching, because the per-draw call is the WebGL cost and a room is culled as a unit anyway (DA10).

### 2.6 GPU instancing

- URP advice: leave the material GPU Instancing checkbox off "to avoid extra shader variants", and let the SRP Batcher and GRD do the work (D8).
- Explicit instancing still fits content that has no GameObjects. The project already does this: wall plates and conduit are drawn as `Graphics.RenderMeshInstanced` buckets (`Assets/Scripts/Office/FrontRoomsWallFixtureRenderer.cs:1-35, 663, 1133`). Those draws are outside GRD and GPU occlusion culling, which handle MeshRenderers (D1), and keep their own 12-40 m distance culling. They skip any set that is not `isActiveAndEnabled` (`:961, 971`), so they follow the door rule if the room's set is disabled (DA1). Mesh LOD does not choose levels for `RenderMeshInstanced` unless `forceMeshLod` is set (D26).

---

## 3. Culling for a streamed corridor of rooms

### 3.1 What shipped games do (reused from DI1 / DI2, plus what was missing)

- **Precomputed sets.** Quake precomputed a potentially visible set per BSP leaf at map compile time, which needs a static, precompiled level (via DI2 T1). Unreal's precomputed visibility stores visibility for non-movable actors in cells, generated offline during a lighting build. Epic calls it "ideal for lower-end hardware and mobile devices" (D21). FrontRooms builds its rooms at runtime, so neither applies.
- **Runtime portals.** Thief walked portals breadth-first from the camera's cell every frame (via DI2 T1). Doom 3 projected portal windings to screen space at runtime, reshaped the frustum at each portal, and used the same area list to pick lights (via DI2 T1). Godot 3 shipped Rooms and Portals, with "the lion's share" of the gain "when you have created just a few rooms" (via DI2 T1).
- **Doors as portals.** "If a vis portal is placed inside a door, the door will automatically close the portal when the door is closed" (Doom 3, via DI2 T2). In Source, a closed areaportal blocks visibility of what lies behind it, and a door-linked areaportal's initial state should match its door (D20; Valve's wiki, search snippet only).
- **Queries and distance.** Unreal's defaults are frustum culling plus hardware occlusion queries; query results arrive a frame late, so actors can "pop" when the camera moves fast. Its HZB occlusion is "more conservative". Cull Distance Volumes use "an array of distances and sizes" (D21). Epic's UE3 guidance notes that occlusion costs more than distance culling, and when cull distance volumes overlap the engine takes the most aggressive value (D22; search snippet only).
- **For FrontRooms:** the corridor is the simplest possible portal graph: a line of convex rooms joined by one door each. The October maze design (DI1 §2) is the general case: 3 m cells, a 2D frustum clipped through portals, exact at eye height. Measured on the real generator, 12.9 cells were visible on average against ~355 in the frustum (DI1 §0).

### 3.2 The corridor rule: closed door = closed portal

- Visible set = the camera's room, plus every room reached through doors that are open, opening, closing or broken, in both directions. A room behind any shut door is not drawn, casts no shadows, and has its lights off (inference from DI2 T1-T2, D20).
- Today: rooms ahead of a shut door have their lights off but all their renderers live and casting into shadow maps. The room behind the player keeps 12 lights on, 4 of them shadowed, until it is recycled (DI5). In code: the next room's lamps are only cued when the door has fully opened (`FrontRoomsRoomStream.cs:820-832`, `ScheduleRoomLight` `:945-952`). A recycled room has its lights reset (`:1009`). A door behind the camera snaps shut once the camera is 4 m past it (`:834-843`), but that room's lights stay on.
- Measured in the clone: rooms behind a shut door deactivated → draw calls 846 → 419, 833 → 244, 952 → 480 (−50 %, −71 %, −50 %), shadow casters 308 → 203, 348 → 103, 507 → 259, visible lights 24 (9 shadowed) → 12 (5) and 35 (13) → 23 (9), image change 0.02 / 0.07 / 0.00 %, at the noise floor of identical frames (DI5).
- Hook already in the code: `FirstClosedDoorSequence()` returns the first room ahead whose far door is shut and not moving (`FrontRoomsRoomStream.cs:520-526`). The door behind is known from the shut-behind branch (`:834-843`).

### 3.3 Unity's tools, and why most do not fit runtime rooms

- **Baked occlusion (Umbra):** "If your Project generates Scene geometry at runtime, then Unity's built-in occlusion culling is not suitable for your Project". "Dynamic GameObjects cannot occlude other GameObjects". Its runtime CPU work "can offset the CPU time that it saves" (D24). The camera asks for occlusion culling (`Assets/Scripts/FrontRooms3DGame.cs:518`), but no scene has baked data: all five scenes in `Assets/Scenes` have `m_OcclusionCullingData: {fileID: 0}` (checked 2026-11-04; same finding as DI1 §1.4), so the flag does nothing.
- **CullingGroup:** you give it bounding spheres; it reports visibility per camera from "frustum culling and static occlusion culling", plus distance bands. It "will not take dynamic objects into account as potential occluders", and visibility updates only during culling, so the design must be asynchronous (D25). Use it for distance bands, such as waking lamp logic by distance; it cannot see runtime walls.
- **`Camera.layerCullDistances`:** a 32-entry array of per-layer cull distances, where 0 means the far plane, planar by default or spherical with `layerCullSpherical` (D23). It is "very useful to cull small objects early on" (D23). In FrontRooms, LODGroup cull heights already do this for kit props (§4.1), so a layer pass adds little (inference).
- **`Renderer.forceRenderingOff`:** Unity documents it for "custom visibility culling" (via DI2 T1). The project's GlassRT treats `forceRenderingOff` and inactive objects as hidden (`Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTSystem.cs:1286, 2360`), so ray-traced glass will not reflect a culled room. That is right, since a culled room is behind a shut door.
- **GPU occlusion culling:** runtime, and it works on runtime geometry, but only for GRD-compatible renderers, not for lights, and not on WebGL (§2.4).

### 3.4 Lights are the part engines do not cull for you

- URP culls lights by their bounds against the frustum, with no occlusion. All 87 maze lights pass URP culling, because the 162° cones touch the frustum even from behind (DI5). In the corridor, the room behind keeps 4 shadowed lights that cost shadow maps; switching off only those 12 lights removed 117 and 116 draws in two views, with no visible change (DI5).
- Doom 3 chose lights from the same portal walk as geometry (via DI2 T1). For FrontRooms this means: the door rule decides lights in the corridor, and the cell-and-portal walk with light prediction (DI1 §3.3) decides them in the maze, on every tier where the result is pixel-identical (DA1, DA13).

### 3.5 Platform matrix

| Technique | Metal desktop | Metal iOS / iPadOS | D3D12 / Vulkan (Windows) | WebGL2 |
|---|---|---|---|---|
| Door / portal culling in C# | yes | yes | yes | yes (DI1, DI2) |
| GPU occlusion culling (needs GRD) | yes | yes | yes (off on Qualcomm GPUs) | no (D1, D13) |
| Baked Umbra occlusion | not for runtime geometry (D24) | same | same | same |
| CullingGroup / layer cull distances | yes | yes | yes | yes (D23, D25) |

---

## 4. LOD and HLOD

### 4.1 LOD Group, and cross-fade

- LOD Group "provides more optimization options than the Mesh LOD feature, but has a larger memory footprint and computational overhead" (D29).
- Smooth transitions: set Fade Mode to Cross Fade and enable LOD Cross Fade in the URP asset. Unity passes the blend factor in `unity_LODFade` and enables the `LOD_FADE_CROSSFADE` keyword (D30). Two timing modes: Fade Transition Width (a share of each LOD's range; distance based), or Animate Cross-fading (the transition starts at the threshold and lasts `LODGroup.crossFadeAnimationDuration`) (D30). With GRD, animated cross-fading falls back to the distance-based kind (D1).
- During a transition both LOD levels draw (Unity 6000.1 LOD Group page, via DI2 T6), so a fade costs extra draws for its duration.
- FrontRooms today:
  - Every kit LODGroup gets `fadeMode = LODFadeMode.None` from the importer (`Assets/Editor/Rendering/FrontRoomsKitImporter.cs:111, 118`). The troffer housing prefab has `m_FadeMode: 0` (`Assets/Resources/Props/Troffer/Prefabs/Troffer_L03.prefab:318`). Swaps are hard pops.
  - No project shader has a `LOD_FADE_CROSSFADE` variant (grep of the 17 project shaders), while the URP asset has cross-fade on with Blue Noise dither (`FrontRooms_URP.asset:33-34`).
  - Kit switch distances are authored in metres at the reference 76° FOV and converted as screenHeight = size / (2 d tan 38°) (`FrontRoomsKitImporter.cs:93-108`). Example: the troffer housing (size 1.224 m; thresholds 0.196 / 0.065 / 0.017) switches at 4 m and 12 m and culls at 46 m (prefab `:317-331`; ESTIMATE check: 1.224 / (2 × 0.196 × 0.781) = 4.0 m).
  - Unity's LOD Bias multiplies those distances. The per-platform default quality levels are Standalone = Ultra (LOD Bias 2.0), iPhone = Medium (0.7), WebGL = High (1.0) (`ProjectSettings/QualitySettings.asset:301, 142, 195, 328-343`). `FrontRoomsQuality.cs` does not change the Unity quality level or LOD Bias (grep). So the desktop already switches at twice the authored distances: the housing at 8 m and 24 m, culled at 92 m (ESTIMATE: × 2).

### 4.2 Mesh LOD (Unity 6.2 and later)

- Version: the Mesh LOD manual pages exist from 6000.2 on; the same 6000.1 URL returns 404 (D27). It applies to 6000.6 (D26, D28).
- What it is: "Mesh LOD creates LODs automatically on model import and stores each LOD in the index buffer of the original mesh". It makes no new GameObjects, gives no per-LOD material choice, and has "a smaller memory footprint compared with the LOD Group feature" (D26).
- Runtime choice is by on-screen size, a project-wide Quality setting "Mesh LOD Threshold" (higher favours coarser LODs), and per-renderer LOD Override and LOD Selection Bias (D28).
- Limits (D26): triangle topology only; particles, VFX Graph and static batching always use LOD0; cross-fade only with GRD enabled, and no custom transition zones; combining it with LOD Group "might lead to unexpected outcomes and is not recommended". For skinned meshes the generator ignores skin weights and blend shapes, and deformation still runs on LOD0. `Graphics.RenderMeshInstanced` and `RenderMeshPrimitives` need `forceMeshLod`.
- FrontRooms today: every quality level carries `meshLodThreshold: 1` (`QualitySettings.asset:37...`), but none of the 292 FBX imports under `Assets/Resources` generates Mesh LODs (`generateMeshLods: 0`, grep). Kits with authored `_LOD0/_LOD1` should keep LOD Group; Mesh LOD fits kits that have no authored LODs (inference from D26).

### 4.3 How pros set switch points and triangle budgets

- Simplygon (Microsoft's LOD tool) sets each switch so the error is at most about 1 pixel by default. Its SDK ties deviation to on-screen size: a LOD made for an on-screen size of 100 px should look like the original wherever it covers at most 100 px (D31; search snippet only).
- Simplygon advises against triangle-reduction targets: "we suggest using either screen size or max deviation" (D31). Their transition guide: keep the surface "within roughly 1 pixels distance" of the original. A common chain makes "each LOD-zone start twice as far away" with half the triangles. LODs under about 1k triangles are often the last useful level, and far content goes into remeshed proxies, "a common approach for large outdoor games" (D32).
- For FrontRooms (inference): the desktop rule "no visible change" maps exactly to Simplygon's 1-pixel criterion. Red's "real hardware geometry at hero LOD0" holds when LOD0 stays on screen wherever a lower LOD would differ by more than a pixel at 4K. The October Office frame was the triangle budget (546k triangles, with kit props casting into every shadow map) (DI3 §2.3); the corridor and the maze are draw-bound, not triangle-bound (DI3 §7: about 13 triangles per draw in the title). So LOD work pays in Office and dressed rooms, and on iPhone, more than in the empty corridor.

### 4.4 HLOD for far rooms

- Unreal's HLOD will "combine them into a single proxy mesh and Material at long view distances" and "reduces the number draw calls per frame"; Epic presents it for "large open worlds" (D44). Unity's own HLODSystem lives on Unity Technologies' GitHub, last stated for 2021.x editors (D45; search snippet only).
- FrontRooms (reused from DI2 T8, applied to the corridor): with the door rule, rooms past the first shut door are not drawn at all. At most one room beyond the current room is visible through an open door, within about 24 m. Nothing is far enough for a proxy to pay. In the maze, a far-ring per-chunk merge is the only HLOD-like option, and only if the portal walk leaves far-ring draws (DI2 T8).

---

## 5. Texture budgets and mip streaming

### 5.1 Mipmap streaming (Unity 6.6)

- How it works: for each texture in view, Unity loads mip levels only up to a computed level. The level comes from the meshes that show the texture (Mesh Filter or Skinned Mesh Renderer only), their UV layouts, and each camera's position and settings. A texture on several objects gets the highest level any of them needs (D34).
- Budget: Quality > Textures > Memory Budget covers streamed mips and all non-streamed textures together. Over budget, Unity loads lower mips (textures "pop or load slowly") and drops unused mips. "Max Level Reduction" sets the smallest level kept, which is also what loads at first startup. Size the budget from `Texture.desiredTextureMemory` at the heaviest spot, slightly above it (D36). Streaming can be on for all cameras or per camera with a Streaming Controller, which also has a Mipmap Bias (D36).
- Per texture: Generate Mipmap on and "Stream Mipmap Levels" on. Android builds also need LZ4 or LZ4HC (D35).
- Limits that matter here (D34, D35):
  - A texture drawn without a Mesh Filter, for example via `Graphics.DrawMeshNow`, gets the lowest mip the quality settings allow (D34). FrontRooms draws wall plates with `Graphics.RenderMeshInstanced` (§2.6), so streaming would blur plate textures unless they are excluded or overridden (inference).
  - Runtime-built meshes need `Mesh.RecalculateUVDistributionMetrics`, or Unity "loads the wrong mipmap levels" (D35). Stream rooms and maze chunks build their meshes at runtime (`FrontRoomsRoomStream.cs:2073-2090`, `FrontRoomsMapWorld.cs:2786-2797`).
  - Unsupported: "Shaders that use UV texture coordinates in a channel other than Mesh.uv (also called UV0)", and "Shaders that change texture coordinates other than scale and translation" (D35). `FrontRooms/Surface` takes walls, floors and ceilings from "the world position and the face normal" (`FrontRoomsSurface.shader:1-8`, `_FR_MESH_UV` off by default `:22`). Those materials cannot stream correctly; Unity would use low mips, "which might display as blurry" (D35).
  - No streaming for texture arrays, cubemap arrays or 3D textures (D34).
  - Loading through `Resources.Load` is not singled out on the 6000.6 pages read; whether such textures stream is UNCONFIRMED FOR 6000.6.
- FrontRooms today: streaming is off in every quality level (`QualitySettings.asset:40, 93, 146, 199, 252...`, budget left at 512 MB), no texture has streaming on (`12_content_kits.md`, DI4), and the game forces `globalTextureMipmapLimit = 0` at start (`FrontRooms3DGame.cs:588`).
- Conclusion (inference): on desktop, streaming adds risk (blur, pops) and saves memory the Mac does not need. On iPhone and iPad, streaming fits only UV-mapped kit props (Office kit, troffers, doors, signs). World-projected surfaces should instead get a plain size cap on the iOS platform tab (DA12). On WebGL it saves no download, because the whole data file is fetched before start (DI2 T11).

### 5.2 Formats per platform

- Desktop (DX11-class and newer): RGB DXT1 at 4 bpp, RGBA BC7 (higher quality, slower to compress) or DXT5 at 8 bpp, HDR BC6H at 8 bpp (D37). Two-channel BC5 at 8 bpp exists for RG data such as normal maps (D38).
- iOS and tvOS: ASTC on A8 (2014) and newer. Block size sets the trade-off, from 8 bpp (4x4) down to 0.89 bpp (12x12) (D37, D38). Apple GPUs list no BC formats in Unity's support table (D38).
- Web: make two builds, DXT for desktop browsers and ASTC for mobile browsers, and pick the data file in the page template. If the format is not supported, Unity decompresses in software, which "uses more memory and might run slower" (D39).
- Web traps (D38, notes 1-2): BC6H and BC7 are not supported "on macOS when using WebGL or OpenGL" and get decompressed to RGBA Half and RGBA32 at load. With linear rendering, browsers without sRGB DXT also decompress DXT to RGBA32. FrontRooms renders in Linear, so a Mac browser running the DXT build would hold BC7 maps at 4x their size (inference: 8 bpp → 32 bpp).
- Project today: default compression "High quality" on 313 files, with per-platform overrides on WebGL (103 files), iOS (40) and Android (31) (DI4, from `12_content_kits.md`). The wallpaper maps carry 2048 max size on the iOS and WebGL tabs with no override (`Wallpaper_Paper_M.png.meta:125-146`).

### 5.3 NPOT textures

- Measured in October: NPOT textures with Non Power of 2 = None (`nPOTScale: 0`) loaded uncompressed as RGBA8 in the editor and in the WebGL build (DI3 §5). The 6000.6 importer reference pages read here state no rule for that; the cause is UNSOURCED. Unity documents the scaling options None / To nearest / To larger / To smaller, and that PVRTC needs square textures (D46, legacy 5.6 page, search snippet only).
- Today: the wallpaper is POT (see the head). 12 NPOT prop maps remain, all `nPOTScale: 0`: `Prop_KeyboardKeys_A/N/S` 2048x768, `Prop_PlateBusinessHours_A/N/S` 1832x612, `Prop_ScreenCRT_A/E/S` 1024x768, `Prop_VendingHeader_A/E/S` 1024x96 (PNG headers and `.meta` read 2026-11-04). Held as RGBA32 with mips they are 54.6 MiB (DI4, ESTIMATE w × h × 4 B × 4/3).

### 5.4 Texel density

- No primary studio standard for first-person texel density turned up in one search; the figures found were tutorials and forum posts (UNSOURCED). So the project should derive its own from screen pixels (method from DI2 T11):
  - Screen pixels per metre at distance d = H / (2 d tan(FOV/2)). With FOV 76°: 1080p → 691/d, 1440p → 921/d, 2160p → 1382/d (ESTIMATE).
  - A 2048 px map on the 1.2 m tile has 1707 texels/m. Its top mip is sampled only closer than 0.40 m at 1080p, 0.54 m at 1440p and 0.81 m at 2160p (ESTIMATE: 691/1707, 921/1707, 1382/1707). The player can stand about 0.3 m from a wall (DI2 T11). So on desktop, 2048 on world tiles is not waste. It is the hero-distance detail Red asks for.
  - On a phone whose screen is H ≈ 1179 px tall (a 6.1-inch iPhone panel in landscape; UNSOURCED device figure), the same 2048 map's top mip is used only closer than 0.44 m (ESTIMATE: 1179 × 1.2 / (2 × 2048 × 0.781)). A 1024 cap would be softer only inside that distance.

### 5.5 Atlases and texture arrays

- The SRP Batcher batches by shader variant, not by material or texture, so atlases do not raise SRP batching (D5).
- GRD instancing groups objects with the same mesh and the same material variant (D2). There, an atlas or array that lets props share one material does reduce draws (inference).
- Trade-off: streaming does not support texture arrays (D34). Per-room WebGL merges (DA10) need one material per merged mesh, which is the case where atlases pay (inference).

### 5.6 Memory budgets

- iPhone and iPad: Apple publishes no per-device limit. `os_proc_available_memory` returns "the number of bytes the app may allocate before it hits its memory limit". It is advisory, not to be cached, and "Memory limits can change during the app life cycle" (D40). The Increased Memory Limit entitlement (iOS 15+) raises the limit "only ... on some device models" (D41). A jetsam report's reason "per-process-limit" means the app crossed the limit "imposed by the system on all apps" (D42). Budget rule (inference): read `os_proc_available_memory` at start, through a small native plugin, and set the handheld texture budget from it, rather than from a device table.
- WebGL: the Unity heap can grow to 4 GB depending on Maximum Memory Size. The default maximum is 2048 MB, and heap growth "can cause your application to crash" if the browser cannot find a contiguous block (D43; search snippet only). FrontRooms sets 1024 MB (`Assets/Editor/FrontRoomsCloudBuild.cs:204`). GPU textures do not live in the wasm heap (DI3 §5).
- Desktop: the October map used 335 MB of textures and 170 MB of render targets at 1080p (DI3 §5); that was before the wallpaper fix. That is no constraint on a desktop GPU, so desktop texture work is about load time and download size, not budget (inference).

---

## 6. Applied to FrontRooms

Numbers marked MEASURED come from DI5 (clone proj_build, 2026-11-04, M3 Max editor, 1920x1080, desktop Ultra, counts exact, times not valid as ms). "Image change" is the share of pixels whose largest channel moved by more than 8/255; identical frames measure 0.00-0.07 % (DI5).

| id | change | what the player sees | expected saving | risk | tier | owner | desktop look change |
|---|---|---|---|---|---|---|---|
| DA1 | Title corridor door-state culling: every room behind a shut door has its renderers, shadow casters, wall-fixture set and lights off | no visible change (MEASURED 0.00-0.07 %) | MEASURED −427 / −589 / −472 draws (−50 / −71 / −50 %), shadow casters −105 / −245 / −248, visible lights −0 / −12 / −12. WebGL ESTIMATE −17 to −24 ms main thread (× 40 us); desktop ESTIMATE −0.9 to −1.2 ms CPU (× 2.1 us per batch, DI3 fit) | a one-frame pop if a room switches on after culling in the frame its door starts to move; activation hitch | all | 游戏视觉 | none |
| DA2 | Minimal form of DA1: only the 12 lights of the room behind a shut door off | no visible change (MEASURED 0.07 / 0.00 %) | MEASURED −117 and −116 draws (−14 %, −12 %) where a door behind is shut; visible lights 24 (9) → 13 (5), 35 (13) → 24 (9) | lamp clocks must resume if that door opens again in play | all | 游戏视觉 | none |
| DA3 | Turn on GRD and GPU occlusion culling on Metal (Mac, iPhone, iPad) and D3D12/Vulkan, after DA4 | no visible change expected | CPU render time: Unity reports up to ~50 % in large scenes (D11, D12); GPU: occluded opaque GRD instances skipped. ESTIMATE 1,100-1,900 maze renderers eligible per view (2.5-3.2k in frustum − ~935 MPB − 380-456 transparent) | probe-atlas blending default (D1); GlassRT with BRG draws UNVERIFIED; slightly more GPU work (D2); longer builds | Desktop + Mobile (not WebGL) | 游戏视觉 (asset, renderer); 设置菜单 if per-preset | none (must pixel-diff) |
| DA4 | GRD prerequisites: BRG Variants = Keep All; `DOTS.hlsl` include in each pass of `FrontRooms/Surface` and the other opaque project shaders | no visible change | none by itself; unblocks DA3 | variant count and build time up (D1) | Desktop + Mobile (WebGL: no effect, never engages) | 游戏视觉 | none |
| DA5 | Maze shell: replace the per-renderer `_CeilingHeight` MPB with a vertex channel (or one material per height class) | no visible change (same value, new source) | SetPass: ESTIMATE up to several hundred per maze view (maze draws per SetPass 4.1-4.7 against 9.2-11.3 in the corridor); makes shells GRD- and GPU-occlusion-eligible | must cover every ceiling height, including stairwells | all | 关卡设计 (`FrontRoomsMapWorld.cs`), 游戏视觉 (shader) | none |
| DA6 | Lamp emission MPB only while a lamp is changing (strike, rise, unstable, warning); steady lamps on shared per-zone/profile materials; same for beams | no visible change | corridor beams alone: MEASURED upper bound −8 to −18 SetPass (beams_off); maze: the lamp-panel share of ~935 MPB renderers leaves the slow path (ESTIMATE, split to count) | a renderer moving in and out of GRD at state changes (cost UNVERIFIED) | all | 关卡设计 (maze lamps), 游戏视觉 (stream lamps) | none |
| DA7 | Directional fill "Soft ambient direction" (strength 0.18): shadow off | MEASURED below threshold in 6 views (≤ 0.08 %) | MEASURED −137 / −103 / −121 corridor draws (−16 / −12 / −13 %), −836 / −678 / −851 maze draws (−16 / −14 / −12 %) | a 0.18-strength shadow may differ by less than 8/255 yet be seen; needs a finer diff and Red's eye | all | 游戏视觉 | none, to confirm |
| DA8 | Spot shadows only where they can show: drop the 4 shadowed troffers in empty Lobby rooms; keep them in furnished profiles and the maze | MEASURED no change in empty Lobby views; maze lamp shadows ARE visible (1.9-31.8 %) | MEASURED −171 / −245 / −386 corridor draws (−20 / −29 / −41 %) in empty rooms | furnished rooms not measured; contact shadows under props and door leaves matter to Red's look | Desktop rule needs Red; Mobile/WebGL first | 游戏视觉 | none in empty rooms; unknown elsewhere |
| DA9 | LOD cross-fade: kit LODGroups and troffer housing to Cross Fade; `LOD_FADE_CROSSFADE` in `FrontRooms/Surface` forward, shadow and depth passes | gain: LOD pops gone | cost, not saving: both LODs draw during a fade (ESTIMATE a few draws at a time) | variant count up; with GRD use distance-based fade (D1) | all (WebGL: Bayer, DI2 T6) | 游戏视觉 | gain |
| DA10 | WebGL only: merge each stream room's static shell and reveals per material; lenses and beams into one mesh each, with a per-room lamp-level array | no visible change | ESTIMATE ≈ 30 colour-pass draws per visible room (shell+reveals 13 → 4, lenses 12 → 1, beams 12 → 1), plus the depth-prepass share; 2 visible rooms ≈ −60 to −100 draws ≈ −2.4 to −4.0 ms main thread (× 40 us) | flicker must match the CPU lamp logic exactly; merged meshes rebuilt on recycle | WebGL | 游戏视觉 | none (WebGL-only path) |
| DA11 | The 12 NPOT prop maps to POT (author POT, or Non Power of 2 = To nearest) | no visible change (resample only) | ESTIMATE −41 MiB when Office props are loaded (54.6 MiB RGBA32 → 13.7 MiB at BC7 8 bpp); iOS ASTC 6x6 ≈ 6.1 MiB | UV mapping must stay normalised (it does for mesh-UV props) | all | 游戏视觉 (textures) | none |
| DA12 | Handheld texture tier: mipmap streaming only for UV-mapped kit textures; 1024 cap on world tiles on the iOS platform tab; budget from `os_proc_available_memory` | none beyond 0.44 m on a phone (ESTIMATE §5.4) | memory: ESTIMATE −4.2 MB per 2048² map capped (5.6 → 1.4 MB at ASTC 4x4 with mips) | world-projected materials must NOT stream (D35) | Mobile | 设置菜单 (budget), 游戏视觉 (import) | none (platform tab only) |
| DA13 | Maze cell-and-portal walk (DI1) also choosing lights, on all tiers once it is proven pixel-identical | no visible change (by construction, DI1 §3) | lights: 87 pass URP culling today (DI5), against a mean of 9 lamps with a visible pool (DI1 §0, October); draws: DI1 ESTIMATE about 10x fewer in-frustum draws | light-prediction misses (DI1 measured about 2 reveals/min on the WebGL design) | WebGL first; Desktop/Mobile after proof | 关卡设计 (hooks), 游戏视觉 (visibility) | none |

**DA1, how to build.** In `Assets/Scripts/FrontRoomsRoomStream.cs`, add one visibility pass per frame, after `TickNearbyDoors` (`:790`) and before rendering. Mark the camera's slot, walk forward while each door is open, opening, closing or broken, and walk backward the same way; mark every other slot hidden. `FirstClosedDoorSequence()` (`:520-526`) is the forward half. A hidden slot gets three things. Its renderers go off: deactivate the render-only children, or set `Renderer.forceRenderingOff`, which GlassRT honours (`FrontRoomsGlassRTSystem.cs:1286, 2360`). Its `FrontRoomsWallFixtureSet` is disabled, because the instanced plates skip sets that are not `isActiveAndEnabled` (`FrontRoomsWallFixtureRenderer.cs:961, 971`). Its `Light`s go off, and its lamp states are kept. Re-show a slot in the same frame its door starts to move: `BeginDoorOpening` (`:2023-2037`) already lifts the next room's rear seal at that moment, and that is the hook. The terminal room and the frozen-play rule stay as they are (`EndStreamAt`, `:540-578`; `:834-843`). **Measure in the clone:** a frame-by-frame capture through one full door opening, one recycle (`MaintainPool`, `:965-1012`) and one 192 m rebase (`:1030-1060`), diffed against today's build (no frame above the noise floor). The activation frame's main-thread ms in a non-batch editor or player, since GPU timers are unavailable in batch mode (DI5). The same three views in the WebGL build, to turn the ESTIMATE into ms.

**DA2, how to build.** In the shut-behind branch of `TickNearbyDoors` (`:834-843`), disable the `Light` components of the room whose door just shut. Keep `lampState` and the timers, so that if the door opens again (frozen play, a Relay break via `BreakDoor`, `:1091-1110`) the lamps continue rather than restart. **Measure:** corridor1 and corridor2 views (DI5 already shows −14 % and −12 % with no visible change), plus a turn-around in play to check that nothing goes dark that can be seen.

**DA3, how to build.** After DA4: in `Assets/Settings/FrontRooms_URP.asset` set GPU Resident Drawer to Instanced Drawing (`:86`) and enable occlusion in cameras (`:88`), the only switch in 17.6. WebGL needs nothing, because GRD never engages there (D1, D13). If WebGL uses the same asset, the validator only logs. Leave static batching off (Player settings), as D2 advises. Lamp lenses, beams and anything else that keeps an MPB stay on the old path and are not occlusion-culled (D1, D4), which is why DA5 and DA6 come first. **Measure:** Frame Debugger "Hybrid Batch Group" entries (D2); Rendering Statistics SetPass; main- and render-thread ms with GRD on and off in a non-batch editor or a player; the GRD registration cost on a maze chunk-build frame and on a corridor recycle and rebase frame; a pixel diff in all six DI5 views, because probe-atlas blending becomes the default (D1); GlassRT reflections on the Mac with GRD on.

**DA4, how to build.** `ProjectSettings/GraphicsSettings.asset:49`: set `m_BrgStripping` to Keep All (Graphics > Shader Stripping > BatchRendererGroup Variants). In `Assets/Resources/Rendering/FrontRoomsSurface.shader`, add `#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"` to ForwardLit (`:161-1053`), ShadowCaster (`:1056-1072`), DepthOnly (`:1075-1089`) and DepthNormals (`:1092-1106`). The passes already use URP transform helpers and `UNITY_SETUP_INSTANCE_ID`, so this is likely the whole change; a clone patch exists (DI5). Then the other opaque shaders: FrontRoomsGlass, FrontRoomsDoorDustSkin, FrontRoomsScreenVideo, FrontRoomsLampGlow, RunSoffitHalo, RunBeamCone. **Measure:** the variant count and build time before and after; a Roslyn and shader compile on mac and iOS (lean-verification gate); a desktop pixel diff with GRD still off, which must be zero.

**DA5, how to build.** In `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs` `AddRenderer` (`:2786-2797`), stop creating the `_CeilingHeight` property block. Either write the ceiling height into a vertex channel the `MeshBuilder` already owns (for example UV2.x), with `FrontRoomsSurface.shader` (`:912`) reading that channel when a keyword is on; or, if `ceilingWorldY` only ever takes the three class heights (`:2775-2776`), use three cached material instances per surface material. The vertex channel is the safer choice, because a world height differs on raised floors. **Measure:** the count of MPB renderers in the frustum by name (shell vs lamp) before and after; SetPass and draws in the three maze views; the pixel diff (must be noise).

**DA6, how to build.** Maze: `FrontRoomsMapWorld.cs` `TickFixtures` (`:3840`, block writes `:3862-3865`) and `WriteEmission` (`:3909-3916`). When a lamp is in a steady state, give its panel and glow the zone's shared lit material and call `SetPropertyBlock(null)`; write a block only while its level is moving. Stream: `FrontRoomsRoomStream.cs` `ApplyFixtureVisual` (`:1998-2021`), with blocks made in `CacheRoomLightVisuals` (`:1797-1830`). Steady lamps take a per-profile lens material and a per-profile beam material. The emission formula (`:2007`) must give the same colour as the shared material. **Measure:** SetPass and draws in all six views; the count of MPB renderers in the frustum; a pixel diff during strikes and flicker against steady lamps; with GRD on, the frame cost when many lamps change state at once (title start, door openings).

**DA7, how to build.** `Assets/Scripts/FrontRooms3DGame.cs:557-565` creates the fill light with `LightShadows.Soft` and strength 0.18. The candidate is `LightShadows.None`, or a per-preset choice in `FrontRoomsQuality.cs`. **Measure:** the same six views with a finer diff (mean absolute difference and the share above 2/255, not only above 8/255), plus tall halls, windows and stairwells, where a directional shadow could read. Then Red's eye on a side-by-side. This overlaps the lighting draft (`13_lighting.md`).

**DA8, how to build.** `FrontRoomsRoomStream.cs:1210` gives fixtures 0-3 `LightShadows.Soft`. A profile switch would set `LightShadows.None` for Lobby rooms (empty) and keep shadows in Office, Run and Pool rooms, which have props and door leaves under the lamps. Apply it in `RefreshRoomMaterials` (`:1721-1795`), where the profile is applied on recycle. **Measure:** the furnished-profile views that DI5 did not cover, with the same finer diff as DA7. The maze stays as it is (DI5 shows its lamp shadows are visible).

**DA9, how to build.** `Assets/Editor/Rendering/FrontRoomsKitImporter.cs:111, 118`: set `LODFadeMode.CrossFade`, with Animate Cross-fading and 0.3-0.5 s (DI2 T6). If DA3 is on, use Fade Transition Width instead, since GRD drops animated fades (D1). The troffer prefab (`Troffer_L03.prefab:318-319`) is set the same way. In `FrontRoomsSurface.shader`, add `#pragma multi_compile _ LOD_FADE_CROSSFADE` and URP's `LODFadeCrossFade(positionCS)` clip to the forward, shadow and depth passes (DI2 T6; URP `ShaderLibrary/LODCrossFade.hlsl`). **Measure:** a capture of a slow walk past props at the LOD switch distances (pops gone, dither visible only during the fade); draws during fades; variant count.

**DA10, how to build.** Inside `#if UNITY_WEBGL && !UNITY_EDITOR` in `FrontRoomsRoomStream.BuildRoom` (`:1149-1230`), merge the shell boxes and reveals per material into one mesh per slot with `Mesh.CombineMeshes`. The WebGL tier already uses the lens-only phone troffer (`FrontRoomsTrofferKit.cs:19-21`). Merge the 12 lenses and 12 beams into one mesh each, with the lamp index in a vertex channel, and pass a 12-entry level array through one block per merged renderer (one MPB per room instead of 24). Door leaves stay separate (they swing). **Measure:** GL draws per frame and main-thread ms in the WebGL build (DI3's method: a GL wrapper for counts, a plain run for times), plus a pixel diff against the unmerged WebGL build during flicker.

**DA11, how to build.** Re-export the 12 `Prop_*` maps listed in §5.3 at POT sizes (2048x1024, 2048x512 or 1024x1024 as the artist prefers), or set Non Power of 2 = To nearest on their `.meta`. **Measure:** `Texture.graphicsFormat` at runtime and the October texture-memory probe (DI3 §5) in an Office room; a lookdev capture of keyboards, CRT screens, vending headers and the business-hours plate.

**DA12, how to build.** iOS platform-tab overrides (max size 1024) for world-tile maps of `FrontRooms/Surface` materials; streaming left off for them. "Stream Mipmap Levels" on for kit prop maps only. `streamingMipmapsActive` turned on only for the handheld quality level, with the budget set at start from `os_proc_available_memory` (D40) and `Texture.desiredTextureMemory` (D36). Desktop and WebGL tabs untouched. **Measure:** on device, the memory footprint and jetsam headroom in the heaviest Office view; `desiredTextureMemory`; lookdev captures of wall and prop texture sharpness at 0.3, 0.5 and 1 m.

**DA13, how to build.** The design is DI1 §2-§3 (portal walk, light prediction, hysteresis). Today it is scoped WebGL-only (DI1 §5). Under the 2026-11-04 brief, savings that do not lower desktop quality are allowed on desktop too, once proven pixel-identical. On desktop and iOS, GPU occlusion culling (DA3) covers opaque GRD renderers; the walk is still needed for lights and shadow maps, which GPU culling does not touch (§2.4, §3.4). **Measure:** DI1's 3,000-view statistic re-run on today's generator; real lights and shadowed lights per frame in the three maze views; reveal events per minute in an autopilot walk; a pixel diff on desktop.

---

## Sources

| id | what (title, author/studio, venue, year) | URL | how read | notes |
|---|---|---|---|---|
| D1 | GPU Resident Drawer (requirements, enable), Unity 6.6 Manual, 2026 | https://docs.unity.com/en-us/engine/6000.6/manual/analysis/graphics-performance-profiling/in-urp/reduce-draw-calls-urp/reduce-rendering-work-on-cpu/gpu-resident-drawer | read | 6000.6 "Current"; MPB, Forward+, Keep All, LOD cross-fade fallback |
| D2 | GPU Resident Drawer performance considerations, Unity 6.6 Manual | https://docs.unity.com/en-us/engine/6000.6/manual/analysis/graphics-performance-profiling/in-urp/reduce-draw-calls-urp/reduce-rendering-work-on-cpu/gpu-resident-drawer-performance | read | GPU cost up, static batching off, "Hybrid Batch Group" |
| D3 | Make a GameObject compatible with the GPU Resident Drawer in URP, Unity 6.6 Manual | https://docs.unity.com/en-us/engine/6000.6/manual/analysis/graphics-performance-profiling/in-urp/reduce-draw-calls-urp/reduce-rendering-work-on-cpu/make-object-compatible-gpu-rendering | read | DOTS instancing shader, no MPB, Disallow GPU Driven Rendering |
| D4 | GPU occlusion culling in URP, Unity 6.6 Manual | https://docs.unity.com/en-us/engine/6000.6/manual/analysis/graphics-performance-profiling/in-urp/gpu-culling | read | current + previous frame depth, spheres, when it helps |
| D5 | Introduction to the SRP Batcher, Unity 6.6 Manual | https://docs.unity.com/en-us/engine/6000.6/manual/analysis/graphics-performance-profiling/in-urp/reduce-draw-calls-urp/srpbatcher/srpbatcher | read | batches by shader variant; persistent material buffers |
| D6 | Check whether a GameObject is compatible with the SRP Batcher in URP, Unity 6.6 Manual | https://docs.unity.com/en-us/engine/6000.6/manual/analysis/graphics-performance-profiling/in-urp/reduce-draw-calls-urp/srpbatcher/srpbatcher-materials | read | no MPB; UnityPerDraw / UnityPerMaterial |
| D7 | Troubleshoot the SRP Batcher in URP, Unity 6.6 Manual | https://docs.unity.com/en-us/engine/6000.6/manual/analysis/graphics-performance-profiling/in-urp/reduce-draw-calls-urp/srpbatcher/srpbatcher-profile | read | Frame Debugger SRP Batch reasons |
| D8 | Choose a method for optimizing draw calls, Unity 6.6 Manual | https://docs.unity.com/en-us/engine/6000.6/manual/analysis/graphics-performance-profiling/reduce-draw-calls/optimizing-draw-calls-choose-method | read | method table; static batching not compatible with GRD; avoid MPB |
| D9 | Introduction to static batching, Unity 6.6 Manual | https://docs.unity.com/en-us/engine/6000.6/manual/analysis/graphics-performance-profiling/reduce-draw-calls/draw-call-batching/draw-call-batching | read | world-space buffers, 64,000 vertices |
| D10 | Enable static batching (runtime StaticBatchingUtility), Unity 6.1 Manual | https://docs.unity3d.com/6000.1/Documentation/Manual/static-batching-enable.html | read | read/write meshes; root may move; 6000.1 page |
| D11 | "GPU Driven Rendering In Unity", Unity staff engineering thread, Unity Discussions, 2023 | https://discussions.unity.com/t/gpu-driven-rendering-in-unity/930675 | read (summary) | Unity engineers' post, used as Unity primary; 2023.3 alpha era, check against D1 for 6000.6 |
| D12 | Unity 6 beta (2023.3) Feature Highlights, Unity staff post, Unity Discussions, 2023-2024 | https://discussions.unity.com/t/unity-6-beta-2023-3-feature-highlights/936841 | [search] snippet only | "up to 50%" CPU; 137K → 13K batches image |
| D13 | URP 17.6.0 and Core RP 17.6.0 package source (Unity), local `Frontrooms3D/Library/PackageCache/com.unity.render-pipelines.universal@327d93d1cbc8`, `...core@02d75770e4b3` | (local files) | read (file:line) | GRD validator; DOTS.hlsl; two-phase occlusion; LOD dither enum |
| D14 | Introduction to optimizing draw calls, Unity 6.6 Manual | https://docs.unity.com/en-us/engine/6000.6/manual/analysis/graphics-performance-profiling/reduce-draw-calls/optimizing-draw-calls | read | render-state update is the costly step |
| D15 | "SRP Batcher: Speed up your rendering", Arnaud Carré, Unity Blog, 2019-02-28 | https://unity.com/blog/engine-platform/srp-batcher-speed-up-your-rendering | read | 1.2x-4x CPU; Book of the Dead 1.47x; SRPBatcherProfiler |
| D16 | Improving CPU performance by using argument buffers, Apple Developer Documentation (Metal) | https://developer.apple.com/documentation/metal/improving-cpu-performance-by-using-argument-buffers | read (.md variant) | per-resource binding and residency tracking cost |
| D17 | "Advanced API Performance: Pipeline State Objects", Tim Cheblokov, NVIDIA Technical Blog, 2023-07-18 | https://developer.nvidia.com/blog/advanced-api-performance-pipeline-state-objects | read | sort by PSO; heavyweight switches; creation stalls |
| D18 | StaticBatchingUtility.Combine, Unity 6.2 Scripting Reference | https://docs.unity3d.com/6000.2/Documentation/ScriptReference/StaticBatchingUtility.Combine.html | read | originals stay and are culled individually |
| D19 | DOTS Instancing shaders (BatchRendererGroup), Unity 6.6 Manual | https://docs.unity.com/en-us/engine/6000.6/manual/analysis/graphics-performance-profiling/in-urp/reduce-draw-calls-urp/batch-renderer-group/writing-shaders/dots-instancing-shaders | read | every BRG shader must support DOTS instancing |
| D20 | func_areaportal / Areaportal, Valve Developer Community (Valve) | https://developer.valvesoftware.com/wiki/Func_areaportal | [search] snippet only | fetch returned 403; closed portal blocks visibility; state follows door |
| D21 | Visibility and Occlusion Culling in Unreal Engine, Epic Games, UE 5.8 docs | https://dev.epicgames.com/documentation/en-us/unreal-engine/visibility-and-occlusion-culling-in-unreal-engine | read | HW queries one frame late; precomputed visibility static only; cull distance volumes |
| D22 | Visibility Culling (UE3/UDK), Epic Games | https://docs.unrealengine.com/udk/Three/VisibilityCulling.html | [search] snippet only | fetch 403; most aggressive overlapping volume; occlusion costs more than distance culling |
| D23 | Camera.layerCullDistances, Unity 6.6 Scripting Reference | https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/camera/layerculldistances | read | 32 values, 0 = far plane, spherical option |
| D24 | Occlusion culling, Unity 6.6 Manual | https://docs.unity.com/en-us/engine/6000.6/manual/cameras/occlusion-culling/occlusion-culling | read | not suitable for runtime geometry; dynamic objects cannot occlude |
| D25 | CullingGroup API, Unity 6.6 Manual | https://docs.unity.com/en-us/engine/6000.6/manual/cameras/occlusion-culling/culling-group-api/api | read | spheres; frustum + static occlusion; asynchronous |
| D26 | Introduction to Mesh LOD, Unity 6.6 Manual | https://docs.unity.com/en-us/engine/6000.6/manual/analysis/graphics-performance-profiling/lod/mesh-lod/mesh-lod-introduction | read | index-buffer LODs; limits; not with LOD Group |
| D27 | Introduction to Mesh LOD, Unity 6.2 Manual (and the 6000.1 URL, 404) | https://docs.unity3d.com/6000.2/Documentation/Manual/lod/mesh-lod-introduction.html | read | dates Mesh LOD to 6.2 (6000.1 page absent) |
| D28 | Mesh LOD runtime quality, Unity 6.6 Manual | https://docs.unity.com/en-us/engine/6000.6/manual/analysis/graphics-performance-profiling/lod/mesh-lod/mesh-lod-quality | read | Mesh LOD Threshold; LOD Override, Selection Bias |
| D29 | LOD Group, Unity 6.6 Manual | https://docs.unity.com/en-us/engine/6000.6/manual/analysis/graphics-performance-profiling/lod/lod-group | read | more options, larger footprint than Mesh LOD |
| D30 | Make LOD Group transitions smooth, Unity 6.6 Manual | https://docs.unity.com/en-us/engine/6000.6/manual/analysis/graphics-performance-profiling/lod/lod-group/lod-transitions-lod-group | read | Cross Fade; transition width vs animated; unity_LODFade; LOD_FADE_CROSSFADE |
| D31 | Simplygon (Microsoft): "Control LOD switch distances in Simplygon's Unreal Engine plugin" and SDK 9.1 "Deviation and size on screen" | https://simplygon.com/posts/dd10914d-b7a4-4a78-9b3c-35eec441495d ; https://documentation.simplygon.com/SimplygonSDK_9.1.42900.0/concepts/deviationscreensize.html | [search] snippet only | 1 px default switch error; screen size over triangle targets |
| D32 | "What to think about when it comes to LOD transitions", Gustaf Johansson, Simplygon blog, 2021 (upd. 2024) | https://simplygon.com/posts/51aba9d5-bafd-459d-94b8-718273fdf092 | read | ~1 px; distance doubles, triangles halve; ~1k-tri last LOD; proxies |
| D33 | "GPU-Driven Rendering Pipelines", Ulrich Haar and Sebastian Aaltonen (Ubisoft), SIGGRAPH 2015 Advances course | https://advances.realtimerendering.com/s2015/aaltonenhaar_siggraph2015_combined_final_footer_220dpi.pdf | [search] snippet only | PDF not readable as text through WebFetch (see head, incident 2); PS4 depth timings |
| D34 | Introduction to mipmap streaming, Unity 6.6 Manual | https://docs.unity.com/en-us/engine/6000.6/manual/materials-and-shaders/textures/texture-loading/texture-streaming/introduction | read | how levels are chosen; no Mesh Filter → lowest mip; no arrays |
| D35 | Enable mipmap streaming, Unity 6.6 Manual | https://docs.unity.com/en-us/engine/6000.6/manual/materials-and-shaders/textures/texture-loading/texture-streaming/use | read | custom meshes need UV metrics; unsupported shaders (non-UV0) |
| D36 | Configure mipmap streaming, Unity 6.6 Manual (updated 2026-10-30) | https://docs.unity.com/en-us/engine/6000.6/manual/materials-and-shaders/textures/texture-loading/texture-streaming/configure | read | budget covers all textures; Max Level Reduction; desiredTextureMemory |
| D37 | Choose a GPU texture format by platform, Unity 6.6 Manual | https://docs.unity.com/en-us/engine/6000.6/manual/materials-and-shaders/textures/textures-getting-started/texture-compression-formats/texture-choose-format-by-platform | read | BC7/DXT/BC6H desktop; ASTC iOS |
| D38 | GPU texture formats reference, Unity 6.6 Manual | https://docs.unity.com/en-us/engine/6000.6/manual/materials-and-shaders/textures/textures-getting-started/texture-compression-formats/texture-formats-reference | read | bpp table; BC7 on macOS WebGL decompressed; linear + sRGB DXT note |
| D39 | Texture compression in Web, Unity 6.6 Manual | https://docs.unity.com/en-us/engine/6000.6/manual/platform-specific/webgl/develop/texture-compression | read | dual DXT/ASTC builds; software fallback |
| D40 | os_proc_available_memory, Apple Developer Documentation | https://developer.apple.com/documentation/os/os_proc_available_memory | read (.md variant) | advisory headroom; limits change |
| D41 | com.apple.developer.kernel.increased-memory-limit, Apple Developer Documentation | https://developer.apple.com/documentation/bundleresources/entitlements/com.apple.developer.kernel.increased-memory-limit | read (.md variant) | iOS 15+; some device models only |
| D42 | Identifying high-memory use with jetsam event reports, Apple Developer Documentation (Xcode) | https://developer.apple.com/documentation/xcode/identifying-high-memory-use-with-jetsam-event-reports | read (.md variant) | per-process-limit; footprint in pages |
| D43 | Memory in Unity Web (6000.3) and PlayerSettings.WebGL.maximumMemorySize (6000.6), Unity | https://docs.unity3d.com/Manual/webgl-memory.html ; https://docs.unity3d.com/6000.6/Documentation/ScriptReference/PlayerSettings.WebGL-maximumMemorySize.html | [search] snippet only | heap to 4 GB; default max 2048 MB; growth can fail |
| D44 | Hierarchical Level of Detail in Unreal Engine, Epic Games, UE 5.8 docs | https://dev.epicgames.com/documentation/en-us/unreal-engine/hierarchical-level-of-detail-in-unreal-engine | read | proxy mesh + material; fewer draws; open worlds |
| D45 | HLODSystem, Unity Technologies, GitHub | https://github.com/unity-technologies/hlodsystem | [search] snippet only | README states 2021.x editors; maintenance status unknown |
| D46 | Texture types (Non Power of 2 options), Unity 5.6 Manual (legacy) | https://docs.unity.cn/560/Documentation/Manual/TextureTypes.html | [search] snippet only | NPOT scaling options; PVRTC square; old version, cause of uncompressed NPOT not stated |
| DI1 | 02 — Visibility for the maze (cell-and-portal PVS), visibility agent, `Frontrooms3D/Documentation/research/webgl/02_visibility_portals.md`, 2026-10-02 | (internal) | read §0, §1.4, §2 | measured PVS statistics on the real generator; WebGL-scoped design |
| DI2 | 04 — Techniques (WebGL2), techniques agent, `Frontrooms3D/Documentation/research/webgl/04_techniques.md`, 2026-10-02 | (internal) | read §0, T1, T2, T6, T7, T8, T10, T11 | carries the Thief, Quake, Doom 3, Godot, Pesce, forceRenderingOff, LOD Group and URP depth-priming citations reused here |
| DI3 | 03 — Measured budgets, budgets agent, `Frontrooms3D/Documentation/research/webgl/03_measured_budgets.md`, 2026-10-03 | (internal) | read §0, §2, §5, §7, §9 | one month old; 40 us/draw WebGL; 2.1 us/batch fit; NPOT finding |
| DI4 | tech_art inventory and content drafts, `tech_art/drafts/00_inventory.md` and `12_content_kits.md`, 2026-11-04 | (internal) | grep only | texture inventory (NPOT 54.6 MiB), door and troffer renderer counts |
| DI5 | tech_art measured frame breakdown, `tech_art/MEASURED.md`, clone proj_build, 2026-11-04 | (internal) | read in full | corridor and maze variants; counts exact, times not valid as ms |
