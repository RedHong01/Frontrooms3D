# 20 · Ranked changes: raise image quality, cut cost (技术美术调研)

Line 技术美术调研 (`tech_art`), stage "measure + rank", 2026-11-04. Red's ask (in 总控, 2026-11-04): "在做无限走廊的时候，我需要你也开启一个task用来 research专业的Technical artist会怎么做，提高画质表现的同时，节省性能。"

Done: six research docs (00 inventory, 10-14 topics) and a claim check of the 15 external claims this list leans on (15: 13 confirmed, 2 partly wrong and corrected in the drafts), eight probe runs in the workstation clone `W/proj_build` (main bc9cf07c), Shot-format evidence sheets in `sheets/`. Not done: nothing landed in main (this line lands no rendering file); no Mac/iOS/Windows player capture, so there are no per-pass GPU milliseconds yet (§5); no download. Reused, not redone: `research/webgl/02`, `03`, `04` (October portal, budget and technique work, WebGL-only), `research/interaction_audit/03` (October quality audit).

Desktop is the reference. Every row below either leaves the desktop frame pixel-identical (measured), or raises desktop quality. Where a row would change the desktop frame by more than noise, it says so and is marked for Red's eye.

## 0. How to read

- §1 is today's frame, measured. §2 is the ranked list (one line each). §3 explains every row: what pros do, how it would be built here, what was measured, what is still open. §4 lists the clone prototypes. §5 says what was not measured.
- "Image change" = share of pixels whose largest channel moved more than 8/255 against the same frozen frame; identical frames give 0.00-0.07 % (the wall TV's video is the 0.07). Film grain is off in compared frames only.
- Counts are exact (UnityStats, URP culling). Milliseconds from the clone are marked UNDER LOAD or GPU-BOUND RANKING; they rank costs, they are not budgets.
- Tiers: D = desktop Mac/Win (the reference), M = handheld iPhone/iPad (`FrontRoomsHandheld.Active`), W = WebGL. "all" = the same change on every tier.
- Owners follow RULES rule 3: 游戏视觉 lands rendering files, `FrontRoomsRoomStream.cs`, `Resources/*`; 关卡设计 lands `FrontRooms3DGame*.cs` and `Scripts/FrontRoomsMap/*`; 设置菜单 owns quality presets and the shared URP asset's tier values.

## 1. The frame today (measured 2026-11-04, Editor batch, Metal, M3 Max, 1920x1080, desktop ULTRA)

Title corridor (three frozen views of the crawl; all rooms are Lobby replicas today):
| view | draw calls | SetPass | tris | shadow casters | visible lights (shadowed) | live renderers |
|---|---|---|---|---|---|---|
| corridor0 (room 0, door ahead open) | 846 | 86 | 161k | 308 | 20 (6) | 587 |
| corridor1 (room 1, both doors shut) | 833 | 74 | 153k | 348 | 24 (9) | 591 |
| corridor2 (room 1, door ahead open) | 952 | 104 | 224k | 507 | 35 (13) | 602 |

Level 0 maze (seed 2554; heaviest view, median view, most-lamps view):
| view | draw calls | SetPass | tris | shadow casters | visible lights (shadowed) | in-frustum renderers (with MPB, transparent) |
|---|---|---|---|---|---|---|
| maze_heavy (Office) | 5,328 | 1,258 | 1.28M | 2,327 | 87 (8) | 3,210 (934, 456) |
| maze_median | 4,842 | 1,176 | 0.68M | 2,240 | 87 (8) | 2,485 (936, 378) |
| maze_lights | 7,379 | 1,564 | 1.25M | 4,542 | 87 (15) | 3,014 (932, 429) |

Where the draws go (split by switching passes off one at a time):
| view | colour passes | depth prepass (kept alive by SSAO or Glass RT) | fill-light shadow | spot shadows |
|---|---|---|---|---|
| corridor0 / 1 / 2 | 34 / 32 / 26 % | 29 / 26 / 20 % | 16 / 12 / 13 % | 20 / 29 / 41 % |
| maze heavy / median / lights | 30 / 29 / 21 % | 26 / 24 / 18 % | 16 / 14 / 12 % | 28 / 32 / 50 % |

GPU time: at 7680x3544 (GPU-bound), SSAO is the one large, consistent cost: switching it off cuts the frame by 45-62 % in the corridor and 29-38 % in the maze. Every other variant saves draws, which is CPU and render-thread time (it matters most on iPhone and in WebGL, where a draw costs about 40 µs of main thread, `research/webgl/03` §0).

Facts that shape the list (00_inventory, MEASURED.md):
- Rooms ahead of a shut door keep every renderer live and cast into shadow maps; only their lamps are off. The room behind keeps its 12 lamps (4 shadowed) on until it is recycled (`FrontRoomsRoomStream.cs:837-845`).
- No static batching, no GPU instancing, no GPU Resident Drawer anywhere: the SRP Batcher alone. About 935 in-frustum maze renderers carry a MaterialPropertyBlock (MPB), which takes them out of the SRP Batcher; the maze does 4.1-4.7 draws per SetPass, the corridor 9.2-11.3.
- `FrontRooms/Surface` (219 of 230 surface materials) has no DOTS-instancing, LOD cross-fade, DBuffer decal, box-projection, lightmap/APV or `DEBUG_DISPLAY` variant; every LODGroup hard-swaps (fade mode None).

## 2. The ranked list

Rank = (quality gain or zero visible change) x measured saving / risk. Corridor rows first where they tie.

| # | change | what the player sees | saving | risk | tier | owner | desktop look |
|---|---|---|---|---|---|---|---|
| 1 | Corridor: rooms behind a shut door are not drawn and their lamps are off (closed door = closed portal) | nothing changes (measured 0.00-0.07 %) | MEASURED −50 / −71 / −50 % draws, −34 / −70 / −49 % shadow casters, visible lights 24→12 and 35→23. Grows with every furnished room family the corridor gets (a Run! B dress was +666 draws per slot) | low | all | 游戏视觉 | identical |
| 2 | Directional "Soft ambient direction": keep the light, switch off only its 0.18-strength shadow | nothing changes (no pixel > 8/255 in 6 views; faint 1-3/255 shapes remain) | MEASURED −12..−16 % draws, −18..−44 % shadow casters; frees the 2048, 2-cascade main shadow map | low (needs Red's eye on the 1-3/255) | all | 关卡设计 (scene + `ApplySceneLighting`) | identical within 3/255 |
| 3 | SSAO samples High (12) → Medium (8) | nothing changes (0.001-0.046 %, at the noise floor) | MEASURED (GPU-bound ranking) −14 % frame on average, about a third of the AO pass, which is the largest GPU item | low | all | 游戏视觉 (renderer asset); 设置菜单 for presets | identical |
| 4 | Corridor spot shadows per room family: none in empty Lobby/Shift rooms; in furnished rooms only lamps over occluders, with a tight shadowed cone (~120°, ~7 m) | Lobby: nothing changes (measured); furnished rooms: contact shadows under desks, doors and baseboards start to land (GAIN) | MEASURED −20 / −29 / −41 % draws in empty rooms; kept lamps: bias 9.3 → 1.8 cm | low (Lobby) / medium (look round per family) | all | 游戏视觉 | gain |
| 5 | MaterialPropertyBlock diet: lamp level through Renderer Shader User Value (Unity 6.3+), steady lamps on shared materials, maze shell `_CeilingHeight` into a vertex channel | nothing changes | up to ~935 maze renderers back into the SRP Batcher (SetPass ESTIMATE several hundred per maze view); corridor beams alone −8..−18 SetPass; prerequisite for #6 | medium (lamp logic in two files) | all | 游戏视觉 + 关卡设计 | identical |
| 6 | GPU Resident Drawer + GPU occlusion culling: BRG Variants = Keep All, one `DOTS.hlsl` include per pass of `FrontRooms/Surface` | almost nothing (0.1-0.7 % moved: troffer housing LOD choice, one fill-shadow edge) | MEASURED −48..−52 % corridor draws, −23..−27 % maze; with occlusion −31..−43 % triangles. CPU-side saving | medium (look check, variant count, build size) | D + M (never W) | 游戏视觉 | needs look check |
| 7 | LOD cross-fade: LODGroups to Cross Fade, `LOD_FADE_CROSSFADE` in `FrontRooms/Surface` | GAIN: no more popping props, lamps and door plates | small cost: both LODs draw during a fade | low | all | 游戏视觉 | gain |
| 8 | Box-projected reflection probe per room type, captured once per family, travelling with the room root; ships only if an A/B capture shows the gain (Run B's room probes were dropped for "no visible gain") | GAIN expected: VCT floors, chrome, CRT glass reflect their own room instead of one zone cube | small ALU + one cube per family | low-medium | D (M optional) | 游戏视觉 | gain (to prove) |
| 9 | Corridor beams: drop the 60 additive VolumetricBeam renderers from the default rooms; rebuild depth-faded shafts only where a family wants dust | nothing changes in Lobby rooms (0.02-0.26 %); visible shafts only where designed | MEASURED −7..−11 draws, −8..−18 SetPass per corridor view; fewer transparent layers | low | all | 游戏视觉 | identical (Lobby) |
| 10 | Authored decals instead of shader-drawn wear: DBuffer support in `FrontRooms/Surface`, one Decal feature on Automatic (DBuffer desktop, Screen Space phone/web), one decal atlas | GAIN: stains, tide marks and scuffs sit where a cause is (Red's "staged assets, not shader tricks") | cost: a decal pass + one draw per decal (instanced); saves the band maths in every surface pixel | medium | all | 游戏视觉 | gain |
| 11 | Lamp tick on CPU: update only lamps near or visible (the WebGL near-only path exists) | nothing changes | October: 1.5-2.7 ms of main thread per frame for ~1,600 fixtures (re-measure) | low-medium | all | 关卡设计 | identical |
| 12 | Light budget for the 32-light tiers: nearest/visible first, with fades | GAIN on iPhone, iPad, WebGL: overhead lamps stop dropping out and reshuffling | 87 → ≤ 31 shaded lights chosen by distance instead of culling order | medium | M + W | 关卡设计 | n/a (desktop untouched) |
| 13 | Handheld renderer: SSAO from depth, After Opaque, Medium samples, so URP drops the depth prepass on iPhone/iPad (Glass RT is Mac-only); bloom from quarter resolution | almost nothing on a phone screen (desktop untouched); needs a device A/B (After Opaque can over-darken) | ESTIMATE −26..−44 % handheld draws (the prepass share, §1), about −75 % bloom pixels | medium | M | 游戏视觉 + 设置菜单 | n/a (desktop untouched) |
| 13b | Upscaling per tier: STP on handhelds when RESOLUTION AUTO drops below 100 % (instead of FSR 1); first add a MotionVectors pass to `FrontRooms/Surface`, and set MSAA to Disabled in that tier (with MSAA on, STP silently does not run) | GAIN on iPad / large phones: steadier edges | a few draws (motion vectors) + one compute pass; post then runs at output size | medium (ghosting on flicker; no dynamic resolution with STP) | M (D opt-in TEMPORAL only) | 设置菜单 + 游戏视觉 | n/a |
| 14 | Trim sheet + prop swatch atlas | nothing changes, or cleaner bevels | 3.29 → ~1.5 material slots per prop: ~1.8 fewer draws per prop per pass | medium (content work, kit by kit) | all | 游戏视觉 | identical or gain |
| 15 | Measurement path: `DEBUG_DISPLAY` in `FrontRooms/Surface`, dev stats overlay with Unity 6.6's split draw counters, Xcode capture kit, fixed capture views in players, silent player switch | nothing changes | none by itself; it gives per-pass GPU ms and valid overdraw maps | low | all | 游戏视觉 (+ 声音设计 for the switch) | identical |

| 16 | Check first: give every runtime lamp a `UniversalAdditionalLightData` on pipeline settings when it is created (`FrontRoomsRoomStream.cs:1200`, `:2167`; `FrontRoomsMapWorld.cs:3717`; `FrontRoomsMapWorld.Pool.cs:725`) | if the inference holds: the shipped Mac/Win game gets the same High soft-shadow filter the Editor shows today (GAIN in the player; the Editor look is unchanged) | none; High PCF costs a little more GPU in the player than the Low it may use now | low (verify on a Mac player capture first) | all | 游戏视觉 + 关卡设计 | gain in the player |

Further rows, not ranked into the top 15, are in the topic docs: depth-prepass review (§3.15), 12 NPOT prop maps to POT (−41 MiB, 11 DA11), handheld texture tier and iOS overrides (11 DA12, 12 CA10), per-language WorldText shipping (12 CA11, 87 MB), baked per-family AO in UV2 (13 L109), detail normals for close-ups (12 CA9), WebGL per-room static merge (11 DA10), the maze cell-and-portal walk (`research/webgl/02`, 11 DA13), URP Tile-Only Mode for iOS (14), and the not-yet-supported URP 17.6 features behind undocumented defines (SSR, GTAO, Surface Cache GI: wait for 6.7, 13 §3.3).

## 3. Each row

### 3.1 Corridor door-state culling (rank 1)
What pros do: portal engines tie visibility to doors. A Doom 3 visportal inside a door closes with the door; a Source areaportal follows its door's state (11 §3.1-3.2, D20). Unity's baked occlusion cannot help runtime-built rooms, so the stream must say it itself.
Here: in `FrontRoomsRoomStream.cs`, a slot is visible when it is the camera's room or is reached through a door that is open, opening, broken or whose lite is LIVE (`FrontRoomsDoorLite`, live once the next room is connected or the door moves, `:625-632`). Every other slot: renderers and wall-fixture draws off, lamps faded off; colliders and audio untouched, so play and the body doors do not change. The hooks exist: `FirstClosedDoorSequence` (`:520`), the shut-behind branch (`:834-843`), the seal lift in `BeginDoorOpening` (`:2023`). Lamp clocks must resume when a door re-opens.
Measured (probe variant `rooms_portal`, roots deactivated): see sheet `ta01_corridor_rooms_behind_doors.jpg`. The minimal form (only the lamps of the room behind) gives −14 / −12 % where a door behind is shut.
Why first: zero pixels, the biggest saving, and the title_stream work is about to put Office, Run! and Pool rooms into the same pool; hidden furnished rooms are pure cost (title_stream survey: a Run! B slot costs +666 draws, +97 SetPass, +245k tris).
Open: re-measure after the families land; check the handoff to the map (`EndStreamAt`) keeps today's behaviour.

### 3.2 Fill-light shadow off (rank 2)
The scene's directional "Soft ambient direction" (intensity 0.16, soft shadow at strength 0.18, `Assets/Scenes/FrontRooms3D.unity:2243-2340`; 0.22 in `FrontRooms3DGame.cs:552-565` for fresh scenes) is URP's main light, so it pays for a 2048, 2-cascade, 40 m shadow map and draws every caster into it. Its shadow only darkens by 18 % where it falls, under ceilings that already block it. Measured: no pixel moves by more than 8/255 in six views; faint 1-3/255 differences remain (sheet `ta03_directional_fill_shadow.jpg`). Change: `m_Shadows: m_Type: 0` on the scene light and `fill.shadows = LightShadows.None` in code. The light itself stays.
Flaw to show Red: the 1-3/255 shapes are below what one sees side by side, but this is a look call, so it goes to Red with the sheet.

### 3.3 SSAO: same look, fewer samples (rank 3)
SSAO is visible (3-10 % of pixels change without it) and must stay on desktop. It is also the largest GPU item (GPU-bound runs, §1). Pros keep AO and buy it back with fewer samples, half-resolution with a depth-aware upsample, or temporal accumulation (14; GTAO, Jimenez et al. SIGGRAPH 2016, is what URP 17.6 carries behind `MODERN_SSAO`, not supported yet). Measured on the same frames: Medium samples (8 instead of 12) change 0.001-0.046 % of pixels (noise floor); half resolution 0.19-0.32 % (edges); Low samples up to 0.16 %; Gaussian instead of bilateral blur 0.07-0.17 %. Sheet `ta07_ssao_settings.jpg`.
Recommendation: desktop Medium samples (identical look). Handheld already runs half resolution at LOW/MEDIUM; add Medium samples there too.
Timing (run8, GPU-bound 7680x3544, base and Medium alternated three times each): Medium cuts the frame by 5-27 %, 14 % on average over the six views; half resolution by 34 %; SSAO off by 44 %. ESTIMATE: SSAO costs about 1.0 ms per million pixels on the M3 Max and Medium saves about 0.32 ms per million pixels (about 0.7 ms at 1080p, about 2.5 ms on a 3456x2234 MacBook panel). Editor numbers rank; the player capture (rank 15) gives the real figure.

### 3.4 Corridor shadows per room family (rank 4)
Why corridor shadows show nothing: every troffer and everything it lights sit in one open 11.5 x 12 m box, so the only shadows are contact shadows; URP sizes spot-shadow bias from the spot's frustum (tan(81°) x 10 m / 1024 = 6.2 cm texel, about 9 cm bias: soft-shadow kernel 2.5, the per-light default these bare lamps use), which erases them (13 §0.6, 13 L103; claim check 15 #8). In the maze, walls between 3 m cells are metre-scale occluders, so lamp shadows show (up to 31.8 % of pixels, sheet `ta02_shadows_corridor_vs_maze.jpg`).
Change: shadows by room family, not by fixture index. Lobby and Shift (empty): none. Office, Run!, Pool: only lamps above an occluder that matters, as a tight shadowed cone (about 120° outer, about 7 m range: bias 9.3 → 1.8 cm, floor texel 3.5 → 0.96 cm) with the side columns unshadowed at 162°. That is a quality gain where furniture stands. Built in the title_stream family work, captured per family.

### 3.5 MaterialPropertyBlock diet (rank 5)
An MPB takes a renderer out of the SRP Batcher (11 D6; Unity's URP advice is material variants instead, D8) and out of the GPU Resident Drawer. Two patterns make most of them: the maze shell's per-renderer `_CeilingHeight` (`FrontRoomsMapWorld.cs:2792-2797`, set once) and the per-lamp emission blocks written every frame on desktop (`FrontRoomsMapWorld.cs:3861-3865`, `FrontRoomsRoomStream.cs:2003-2020`). Unity 6.3+ gives each renderer a 32-bit Renderer Shader User Value read in the shader that keeps SRP batches (12 C14; URP 17.6 already declares `unity_RendererUserValue`). Steady lamps can share per-zone materials; only a lamp that strikes, flickers or dies needs its own value.
Saving: ESTIMATE several hundred SetPass per maze view; corridor beams alone measured −8..−18 SetPass. Clone check: count SetPass with the change.

### 3.6 GPU Resident Drawer + GPU occlusion culling (rank 6)
Unity 6's BatchRendererGroup path: renderers are uploaded once and drawn with instancing; GPU occlusion culling tests them against the previous and current depth pyramid (11 §2). It runs on Metal (Mac, iPhone, iPad), D3D12 and Vulkan, never on WebGL2.
Blocked today by `m_BrgStripping: 0` (Unity logs "BatchRendererGroup Variants setting must be Keep All") and by `FrontRooms/Surface` lacking DOTS instancing. Clone prototype (`W/tech_art/probe/grd/`): one `#include_with_pragmas ".../ShaderLibrary/DOTS.hlsl"` after each of the four `#pragma multi_compile_instancing` lines, BRG Variants = Keep All. Sheet `ta05_gpu_resident_drawer.jpg`.
Measured: corridor 846 → 405 draws, maze 4,842 → 3,843; with occlusion culling triangles −31..−43 %. No GPU-time gain at 7680x3544 (it saves CPU). 0.1-0.7 % of pixels moved: inside troffer housings (GRD picks LODs itself) and along one fill-shadow edge. Order: after rank 5 (MPB renderers are skipped), and the LOD question with rank 7. Cost to check: shader variant count, build size and build time with Keep All.

### 3.7 LOD cross-fade (rank 7)
Every LODGroup has fade mode None and no project shader has `LOD_FADE_CROSSFADE`, although the URP asset has cross-fade on (dither: Blue Noise) (11 §4.1). Props, troffer housings and door plates therefore pop. Change: Cross Fade on the kit LODGroups and the variant in `FrontRooms/Surface`'s forward, shadow and depth passes. Also review: LOD distances were authored for LOD bias 1, desktop ULTRA runs bias 2 (00 §E), so LOD0 holds twice as far as the kit author planned; keep 2 on desktop (quality), use 1 on handheld.

### 3.8 Box-projected reflection probes per room type (rank 8)
Today one baked zone cube serves every shiny surface (`FrontRoomsZoneReflection.cs`), probe blending and box projection are off, and `FrontRooms/Surface` has no box-projection keyword (13 L105). Pros place box-projected probes per room (13 §3). Here: one cube per room family, captured once in the lit state, on a probe that is a child of the room root so it travels with recycling and rebase; desktop asset turns blending and box projection on. Gain: VCT tiles, chrome, CRT glass and the wet carpet sheen reflect their own room's troffers. The Glass RT glass is untouched. Honest flaw: Run B's per-room probes were dropped for "no visible gain" (13 §3), so this ships only after an A/B capture per family proves it. Related: the runtime Structures probes (realtime, 512/1024, all rendered in one frame) ask for box projection they cannot get; render them time-sliced or capture offline (13 L113).

### 3.9 Corridor beams (rank 9)
60 additive beam renderers (12 per room, each with an MPB), density 0.012; their output peaks at about 1 % of the lamp colour (00 waste list). Removing them changes 0.02-0.26 % of pixels in the Lobby views and saves 8-18 SetPass. Keep visible shafts only where a room family is dusty, rebuilt with a depth and view-angle fade (`RunBeamCone` style, one shared material, no MPB) (12 CA5).

### 3.10 Authored decals (rank 10)
DOOM (2016) and DOOM Eternal draw decals in the same clustered loop as lights, with authored placement (12 C6, C7). `FrontRooms/Surface` draws tide marks, grime bands and ceiling streaks from smoothstep bands of one macro map, which Red's bar calls fake. URP decals need `_DBUFFER_MRT1/2/3` and `ApplyDecalToSurfaceData` in the receiving shader for the DBuffer technique; "Automatic" picks DBuffer on desktop and Screen Space on iOS and WebGL (12 CA1, claim check 15 #11). Gain: wear placed where its cause is. Keep the macro tone and damp.

### 3.11 Lamp tick (rank 11)
`TickFixtures` touches every fixture of every built chunk every frame on desktop (~1,600) and writes an MPB to each lens (`FrontRoomsMapWorld.cs:3840-3867`); October measured 1.5-2.7 ms per frame. The WebGL near-only tick already exists (`:3833`). Run the near/visible set every frame and the rest on change, on all tiers; flicker of unseen lamps is invisible by definition.

### 3.12 32-light budget on handheld and WebGL (rank 12)
Forward+ removes the per-object limit, not the per-camera one: 256 on desktop, 32 on iPhone and iPad (`SHADER_API_MOBILE`) and on WebGL2 (`SHADER_API_GLES3`) (13 §0.4, claim check 15 #5, #13 correction). All 87 maze lamps pass culling, so on a phone URP shades 32 in culling order, not distance order: overhead lamps can go dark and reshuffle while turning. Choose them by hand (nearest and visible first, fades), per `research/webgl/04` T3-T5. Desktop untouched.

### 3.13 Handheld renderer and upscaling (ranks 13, 13b)
Handheld renderer (14 U-A1): on iPhone and iPad the depth prepass exists only for SSAO (Glass RT is Mac-only). SSAO from Depth with After Opaque lets URP drop the prepass; Apple's guidance is that hidden surface removal makes a prepass pointless for speed on its GPUs (14). Unity warns After Opaque can over-darken, so it needs a device A/B; build it as a handheld URP asset and renderer chosen at boot, never as runtime writes into the desktop asset. Bloom from quarter resolution with fewer iterations (Run zones already use LQ bloom).
Upscaling: RESOLUTION AUTO drops an iPad or a Duo inner panel to 80-90 % and upscales with FSR 1 (spatial). STP is Unity 6's spatial-temporal upscaler (compute, Metal OK, not WebGL2); at the same input it holds edges better, but it needs motion vectors and `FrontRooms/Surface` has no MotionVectors pass (14 U-A5), it does not work with dynamic resolution, and it upscales before bloom and the uber pass, so post runs at output size (about +56 % post pixels on the Duo inner screen, ESTIMATE). MetalFX is absent in 6000.6.3f1 (URP/Core source, Mac/iOS players, script reference 404); `UnityEngine.MetalFXModule` and an SRP Core upscaler first appear in the 6.7 beta docs (14). FSR 2 and DLSS are HDRP-only in 6000.6 (the URP code sits behind a framework define the project does not set). Desktop at 100 % keeps MSAA 4x; STP stays an opt-in TEMPORAL option there, never the default. Note: URP 17.6 applies film grain after FXAA, RCAS, TAA and STP, so the comment in `FrontRoomsPostStack` that MSAA is needed to keep grain sharp no longer holds (14).

### 3.14 Trim sheet and swatch atlas (rank 14)
Pros share texture space through trim sheets and kits (Insomniac's Ultimate Trim; Skyrim's modular kits, 12 C1-C3). The troffer kit already does it; the 255 prop models average 3.29 material slots each, and each slot is a draw in every pass. Collapse flat-colour slots into one swatch-atlas material per prop; one architectural trim sheet for cove base, casing, T-bar, chair rail. Done kit by kit as kits are touched.

### 3.15 Measurement path (rank 15) and the prepass
URP's Rendering Debugger overdraw view is not valid on `FrontRooms/Surface` (no `DEBUG_DISPLAY` hook: lit colour leaks into the count, sheet `ta06_overdraw.jpg`). Unity 6.6 renamed the draw counters (split SRP Batcher / Standard / BRG counters; "Draw Calls Count" and "Batches Count" read 0), and GPU timers are off in Editor batch mode (10 §6). Per-pass GPU ms therefore need an Xcode capture of a Mac player (10 §6.3, P-2..P-4) with a silent switch for players (P-5, 声音设计).
Prepass: a depth prepass is kept alive by SSAO (depth-normals) and, on Mac, by Glass RT; it is 20-29 % of all draws. With SSAO on desktop it is needed; on a tier without SSAO and without Glass RT it disappears by itself. Depth priming cannot use it while MSAA is on (00 §D). No change proposed; recorded so nobody adds a second prepass (decals in rank 10 must reuse it).

### 3.16 Lamps may be softer in the Editor than in the shipped game (row 16, found by the claim check)
URP 17.6 source (claim check 15, extra finding; INFERENCE, not yet seen at runtime): a light created with `AddComponent<Light>()` and no `UniversalAdditionalLightData` sends soft-shadow value 1.0 to the shader in a player, which is Low filtering (`ShadowUtils.cs:648-658`, `Shadows.hlsl:139, 294-305`), not the asset's High. In the Editor, URP's light-extraction delegate (`UniversalRenderPipelineCore.cs:1624-1638`, `#if UNITY_EDITOR`) adds the component with "Use settings from Render Pipeline Asset", so Editor and clone captures may show High filtering that a desktop player does not. Every troffer and maze lamp is created bare. If a Mac player capture confirms it, the fix is to add the component at creation and leave it on pipeline settings: High filter, bias kernel unchanged at 2.5. This protects the desktop reference itself, so it is checked before anything else ships.

## 4. Prototypes in the clone (W/proj_build, clone only)
- Probe `W/tech_art/probe/Assets/Editor/TechArtProbe/FrontRoomsTechArtProbe.cs` (never lands; private productName, silent). Variants `rooms_portal`, `behind_lights_off`, `addshadow_off`, `dirshadow_off`, `ssao_*`, `beams_off`, `grd`, `grd_occ`, `stp`, combos with `&`.
- GRD prototype `W/tech_art/probe/grd/` (FrontRoomsSurface diff, BRG Keep All, CLONE_CHANGES.txt).
- Data `W/tech_art/captures/run1-run8`, summary `W/tech_art/MEASURED.md`. Sheets `sheets/ta01-ta07`.

## 5. Not measured, and why
- Per-pass GPU milliseconds: Editor batch on this Mac has no GPU timers; the 7680x3544 timings drift ±30-50 % between repeats. Needs a Mac player Xcode capture (rank 15).
- iPhone / iPad: no device run (store hold, rule 12; no silent player path yet).
- Windows: no PC here.
- Furnished corridor families (Office, Run!, Pool): not built yet (title_stream).
- WebGL: covered by `research/webgl/*`; nothing new measured.
