# tech_art: measured frame breakdown (clone proj_build, 2026-11-04)

Clone W/proj_build synced from main bc9cf07c (fr sync 10:35). Editor 6000.6.3f1 batch Play mode, Metal, Apple M3 Max, render target 1920x1080, desktop ULTRA (pipeline asset as is; FrontRoomsQuality raises the additional-light shadow atlas to 8192 at runtime). Probe: W/tech_art/probe/Assets/Editor/TechArtProbe/FrontRoomsTechArtProbe.cs. Raw data: W/tech_art/captures/run2/frames.tsv, PNGs `<view>_<variant>_raw.png` (film grain off for the PNG only), diffs by W/tech_art/probe/imgdiff.py.
Counts are exact (UnityEditor.UnityStats, URP culling result, renderer inventory). Times are synchronised renders and were taken UNDER LOAD (load average 40-130): ranking only, not valid as ms.
Image change = share of pixels whose max channel differs by more than 8/255 from the base frame. The noise floor of identical frames is 0.00-0.07 %.

## Title corridor (3 frozen views along the crawl, all rooms Lobby)
Views: corridor0 at z 4.7 (room 0, its far door open), corridor1 at z 10.5 (room 1, door behind shut, door ahead shut), corridor2 at z 17.4 (room 1, door ahead open into room 2).
| variant | draw calls | SetPass | tris | shadow casters | visible lights (shadowed) | image change |
|---|---|---|---|---|---|---|
| base | 846 / 833 / 952 | 86 / 74 / 104 | 161k / 153k / 224k | 308 / 348 / 507 | 20(6) / 24(9) / 35(13) | - |
| rooms_portal: rooms behind a shut door deactivated (camera room + rooms reached through open doors stay) | 419 / 244 / 480 | 82 / 61 / 94 | 99k / 50k / 135k | 203 / 103 / 259 | 20(6) / 12(5) / 23(9) | 0.02 / 0.07 / 0.00 % (= noise) |
| behind_lights_off: only the 12 lights of the room behind a shut door | 846 / 716 / 836 | 86 / 66 / 96 | = / 119k / 190k | = / 231 / 391 | = / 13(5) / 24(9) | 0.00 / 0.07 / 0.00 % |
| addshadow_off: every spot shadow off | 675 / 588 / 566 | 76 / 58 / 80 | 109k / 84k / 108k | 137 / 103 / 121 | (1) | 0.04 / 0.07 / 0.00 % |
| dirshadow_off: the directional "Soft ambient direction" fill's shadow off | 709 / 730 / 831 | 82 / 70 / 100 | 123k / 128k / 190k | 171 / 245 / 386 | -1 shadowed | 0.00 / 0.07 / 0.00 % |
| ssao_off | -4 each | -4 each | = | = | = | 9.6 / 5.4 / 3.4 % (SSAO is clearly visible) |
| ssao_depth (SSAO normals from depth instead of depth-normals) | = | = | = | = | = | 0.29 / 0.25 / 0.24 % |
| beams_off (60 additive beam renderers, each with an MPB) | -7 / -6 / -11 | -11 / -8 / -18 | = | = | = | 0.02 / 0.26 / 0.09 % |
| msaa1 | = | = | = | = | = | 0.18 / 0.32 / 0.11 % (edges, max diff 106-117) |
| stp (STP at scale 1.0, MSAA off) | +26 / +21 / +17 | +3 / +2 / +2 | +24k / +19k / +17k | = | = | 0.19 / 0.34 / 0.12 % |
| glassrt_off, depthprime | = | = | = | = | = | noise |
Facts behind the corridor rows: per slot ~125 live renderers; slots ahead of a shut door have lights OFF but all renderers live and casting into shadow maps; the slot behind keeps 12 lights on, 4 shadowed, until recycled. In-frustum renderers 379-504, of them 47-55 with a MaterialPropertyBlock and 55-66 transparent.
Notable: in the corridor, all 4-per-room shadowed troffers can be switched off with no visible change from these eye-level views (162 degree cones; see the October bias note in research/interaction_audit/03 §1.8).

## Level 0 maze (seed 2554, map rebuilt from the game's own helpers, 3 views picked automatically)
Views: maze_heavy (most renderers in view within 14 m), maze_median, maze_lights (most lamps in view).
| variant | draw calls | SetPass | tris | shadow casters | visible lights (shadowed) | image change |
|---|---|---|---|---|---|---|
| base | 5,328 / 4,842 / 7,379 | 1,258 / 1,176 / 1,564 | 1.28M / 0.68M / 1.25M | 2,327 / 2,240 / 4,542 | 87(8) / 87(8) / 87(15) | - |
| addshadow_off (map shadowRadius 0) | 3,837 / 3,280 / 3,688 | 1,107 / 1,028 / 1,137 | 777k / 432k / 518k | 836 / 678 / 851 | (1) | 1.9 / 2.1 / 31.8 % (lamp shadows ARE visible in the maze) |
| dirshadow_off | 4,492 / 4,164 / 6,528 | 1,174 / 1,118 / 1,476 | 1.06M / 0.57M / 1.10M | 1,491 / 1,562 / 3,691 | -1 | 0.00 / 0.06 / 0.08 % |
| ssao_off | -4 | -4 | = | = | = | 6.2 / 4.9 / 7.4 % |
| glassrt_off | -4..-6 | -3..-5 | = | = | = | 0.00 / 0.14 / 0.28 % |
Facts: ~12.8k live renderers, 2.5k-3.2k in the frustum (about 935 with an MPB, 380-456 transparent), all 87 lights pass URP culling (162 degree cones touch the frustum from behind).

## Pipeline facts found by the probe
- No static batching, no GPU instancing, no dynamic batching anywhere (UnityStats static/instanced/dynamic batched draw calls = 0 in every view): everything goes through the SRP Batcher; draw calls ~ 5.6-12x SetPass.
- GPU Resident Drawer could not engage: ProjectSettings/GraphicsSettings.asset `m_BrgStripping: 0` (GRD logs: "BatchRendererGroup Variants setting must be Keep All"), and FrontRoomsSurface.shader (83 of 86 surface materials) has no DOTS_INSTANCING_ON variant. The four passes use URP transform helpers and UNITY_SETUP_INSTANCE_ID, so one `#include_with_pragmas ".../ShaderLibrary/DOTS.hlsl"` per pass is the likely whole shader change (clone patch W/tech_art/probe/grd/).
- SSAO no longer doubles draws (October: x2 from a depth-normals prepass): SSAO off removes 4 draws only. Its cost is GPU time, not draws.
- In batch mode on this Mac, GPU timers are unavailable (SystemInfo.supportsGpuRecorder false, FrameTimingManager gpu 0): GPU ms per pass needs an Xcode Metal capture of a player or a non-batch Editor.
- URP 17.6 source (in the 6000.6.3f1 editor) carries an IUpscaler framework (STP, FSR2 via the AMD module, DLSS via the NVIDIA module) behind ENABLE_UPSCALER_FRAMEWORK, which this project's compile does not define; `UpscalingFilterSelection` is marked obsolete from 6000.3 under that define. No MetalFX code in URP/Core or in the Mac/iOS player binaries (string scan).

## Draw calls per pass (run7, 1080p, same views)
Prepass exists while SSAO or Glass RT is on (Glass RT forces a depth prepass by itself; SSAO off alone removes 4 draws). Split from the variants: colour = all four of SSAO, Glass RT, fill shadow, spot shadows off; prepass = (SSAO and Glass RT off) minus SSAO's own 4.
| view | total | colour (opaque + transparent + post) | depth prepass | directional fill shadow | spot shadows |
|---|---|---|---|---|---|
| corridor0 | 846 | 291 (34 %) | 243 (29 %) | 137 (16 %) | 171 (20 %) |
| corridor1 | 833 | 263 (32 %) | 218 (26 %) | 103 (12 %) | 245 (29 %) |
| corridor2 | 952 | 246 (26 %) | 195 (20 %) | 121 (13 %) | 386 (41 %) |
| maze_heavy | 5,328 | 1,611 (30 %) | 1,386 (26 %) | 836 (16 %) | 1,491 (28 %) |
| maze_median | 4,842 | 1,424 (29 %) | 1,174 (24 %) | 678 (14 %) | 1,562 (32 %) |
| maze_lights | 7,379 | 1,525 (21 %) | 1,308 (18 %) | 851 (12 %) | 3,691 (50 %) |
SetPass with SSAO and Glass RT both off: corridor -14..-18, maze -414..-431 (-33 %).

## SSAO settings (run6, 1080p images; noise floor 0.00-0.07 %)
| setting | image change corridor0/1/2 | image change maze heavy/median/lights |
|---|---|---|
| Samples High (12) -> Medium (8) | 0.002 / 0.002 / 0.003 % | 0.001 / 0.046 / 0.037 % |
| Samples Low (4) | 0.006 / 0.079 / 0.009 % | 0.016 / 0.125 / 0.160 % |
| Downsample (half res) | 0.21 / 0.32 / 0.21 % (edges, max 40-46) | 0.22 / 0.25 / 0.19 % |
| Downsample + Medium | 0.23 / 0.42 / 0.21 % | 0.23 / 0.26 / 0.20 % |
| Blur High (bilateral) -> Medium (Gaussian) | 0.07 / 0.17 / 0.10 % | 0.09 / 0.13 / 0.08 % |
| SSAO off | 9.6 / 5.4 / 3.4 % | 6.2 / 4.9 / 7.3 % |

## GPU-bound timing (run5, 7680x3544, frame ms, mean of two base runs)
Repeated base frames differ by up to 50 % (corridor0 158 vs 73 ms): only large, consistent effects count. SSAO off: corridor -45..-62 %, maze -29..-38 % (every view). Every other variant is within the noise at this resolution (they save CPU / render-thread work, not GPU time). GRD: no GPU gain (as expected).

## SSAO timing, interleaved (run8, 7680x3544, frame ms, base and Medium measured 3 times each, alternating)
| view | base (12 samples) | Medium (8) | half res | off |
|---|---|---|---|---|
| corridor0 / 1 / 2 | 43.9 / 49.5 / 59.6 | 37.5 / 36.7 / 43.7 (-15 / -26 / -27 %) | 30 / 29 / 32 | 21 / 21 / 25 |
| maze heavy / lights / median | 59.1 / 91.4 / 65.1 | 52.4 / 86.5 / 60.2 (-11 / -5 / -8 %) | 43 / 62 / 49 | 36 / 58 / 44 |
| mean of six | 61.4 | 52.8 (-14 %) | 40.8 (-34 %) | 34.3 (-44 %) |
ESTIMATE: SSAO ~27 ms at 27.2 Mpx = ~1.0 ms per Mpx on the M3 Max Editor; Medium saves ~0.32 ms per Mpx (1080p ~0.7 ms, a 3456x2234 MacBook panel ~2.5 ms). Editor batch, not a player: ranking.

## Corridor package (run9): rooms_portal + fill shadow off + spot shadows off (Lobby) + beams off + SSAO Medium
| view | draw calls | SetPass | tris | shadow casters | visible lights | main ms (UNDER LOAD) | image change |
|---|---|---|---|---|---|---|---|
| corridor0 | 846 -> 209 (-75 %) | 86 -> 57 | 161k -> 33k | 308 -> 0 | 20 -> 20 | 4.3 -> 1.8 | 0.081 % |
| corridor1 | 833 -> 135 (-84 %) | 74 -> 42 | 153k -> 19k | 348 -> 0 | 24 -> 12 | 6.1 -> 4.0 | 0.108 % |
| corridor2 | 953 -> 210 (-78 %) | 103 -> 54 | 224k -> 48k | 507 -> 0 | 36 -> 24 | 9.7 -> 4.7 | 0.076 % |
