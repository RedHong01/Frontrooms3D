# Settings regroup + render-quality presets: research (2026-10-24)

Line: 设置菜单与画质预设 (account A, `local_a77b85b3`; started on account B as `local_2f54ad43`). Status: **research and design done; nothing built.** The build waits for Red's picks in [`DECISION.md`](DECISION.md).

Red, 2026-10-24 ~07:15, in the 交互设计 fold-transition chat: "目前在内屏游戏的帧率不是特别高，是否是分辨率的问题？我希望在游戏build里（mac/iphone/ipad全平台）都有调整不同渲染质量预设/细节参数的选择，我希望首先你能够给设置菜单里面的列表进行打组，以及必要的时候不要制作成点击切换，可以适当的利用滚动滑条等一系列不同的component来处理不同的设置内容，这部分我希望你能够线research，基于我已有的typographic system和grid system去深化+优化，并且参考当前现有的游戏设置UI案例".

| File | What it holds |
|---|---|
| `README.md` (this file) | The frame-rate answer with numbers, and what the research found |
| [`IA.md`](IA.md) | One settings system for Mac, Windows, iPhone, iPad and both Duo displays: tabs, rows, components, presets, input, layout, how it is built in URP |
| [`DECISION.md`](DECISION.md) | Five decisions for Red, with player feel, cost, recommendation and what each holds up; Chinese summary at the end |
| [`SOURCES.md`](SOURCES.md) | Every source opened, by report |
| `reports/R1_pc_console.md` | 20 PC and console settings screens (C1–C20) |
| `reports/R2_mobile.md` | 18 mobile games and ports (M1–M18), Apple and Android platform guidance, power |
| `reports/R3_access_components.md` | Accessibility guidance (GAG, Xbox XAG, Apple, Microsoft), 15 accessible-menu cases, input patterns, a component rule table |
| `reports/R4_unity_runtime.md` | What Unity 6.3 URP can switch at runtime on Metal; a 37-row table; the recommended preset architecture |
| `perf/probe_v3_results.csv`, `perf/FrontRoomsRenderCostProbe3.cs.txt` | The render-cost probe's raw numbers and its clone-only harness |

Figma: file `0tCbAiVUlrPId3RWd9LRif`, page 2099:76, section **`3688:7842`** "FRONTROOMS · SETTINGS · GROUPS + RENDER QUALITY · PROPOSAL", under DUO FLOWS: KIT `3692:7842` (switch, segment cell, slider, tab, cost bars, frame meter, focus bar, BACK, keycap at desktop px, phone pt and tablet pt), DESKTOP S1–S3, HANDHELD H1–H6, DECISIONS (D1 options B and C, D2–D5, the preset ladder), RULES.

How it was made: the account-B session gathered the material on 2026-10-24 07:25–07:50 (four research agents, two probe runs) and stopped at B's weekly limit before anything was written. The account-A session wrote the four reports from the agents' saved fetches (no re-searching), ran probe v3, and did the IA, Figma and decision page. No file was downloaded; third-party media is linked only.

## 1. The frame-rate answer

**Is the inner display slow because of resolution? Mostly yes.** It draws 1.76× the outer display's pixels, and every pixel pays for desktop-grade settings. That moves the GPU's picture work from about 7–9 ms to about 10–11 ms of a 16.7 ms frame on the A20 Pro (estimates, lower bounds). A fixed per-frame cost that no resolution change removes sits on top. **Render scale is the one lever that clearly moves the frame:** 75–80 % on the inner display brings it back to about the outer display's cost. Single features each moved it by less than the probe's noise.

### 1.1 What the game asks for

| Fact | Value | Where |
|---|---|---|
| Frame-rate target | 60 fps on every platform | `Assets/Scripts/FrontRooms3DGame.cs:496` `Application.targetFrameRate = 60` |
| ProMotion | off: iOS caps the game at 60 Hz | `ProjectSettings/ProjectSettings.asset:257` `appleEnableProMotion: 0` |
| Resolution scaling | native | `ProjectSettings.asset:163` `resolutionScalingMode: 0` |
| iPhone / iPad quality level | 2 (Medium) | `QualitySettings.asset` `m_PerPlatformDefaultQuality` |
| URP asset | one for every quality level | `Assets/Settings/FrontRooms_URP.asset`: HDR (32-bit), MSAA 4×, render scale 1, main shadow map 2048, additional-light atlas 4096, 2 cascades, 40 m, soft shadows High |
| Renderer features | SSAO full resolution, 12 samples, bilateral blur; bloom high-quality filtering | `FrontRooms_URP_Renderer.asset`, `Resources/Rendering/FrontRoomsPost` |

So "not high" means frames miss the 16.7 ms budget. With vsync a missed frame waits for the next refresh, so play judders between 60 and 30.

### 1.2 Pixels per display

| Display | Pixels | Megapixels | × Duo outer |
|---|---|---|---|
| iPhone Duo outer | 2034 × 1398 | 2.84 | 1.00 |
| iPhone Duo inner (panel) | 2670 × 1878 | 5.01 | **1.76** |
| iPhone Duo inner (iOS 27.1 Simulator) | 2853 × 2007 | 5.73 | 2.02 |
| iPhone 17 Pro | 2622 × 1206 | 3.16 | 1.11 |
| iPad 11" | 2360 × 1640 | 3.87 | 1.36 |
| MacBook Pro 14" | 3024 × 1964 | 5.94 | 2.09 |

Panels: Apple's iPhone Duo spec page [P-S1] (outer 1398 × 2034 at 460 ppi, inner 1878 × 2670 at 430 ppi, ProMotion up to 120 Hz, A20 Pro with a 7-core GPU). Unity sizes the iOS drawable by `nativeScale` [R4 Q7.1], so the device draws the panel size.

### 1.3 Measurement (probe v3)

- **Where:** private clone `~/FrontRoomsVisualWork/proj_settings_perf` (main 7802f3ee + working tree), Apple M3 Max, Metal, Unity 6000.3.10f1, 2026-10-24 08:02–08:04. Harness `perf/FrontRoomsRenderCostProbe3.cs.txt` (clone-only; never in main).
- **Method:** the game's own camera in the main scene (autopilot seed 2554, frozen 6 s after the start door shuts), 4 view directions, 2 rounds, 12 synced renders per view. Each configuration is timed at full size and at 1/16 of the pixels; the difference × 16/15 is the **pixel-bound GPU cost**, so the CPU cost of culling and draw submission cancels. The 20th percentile per view resists load spikes from other processes.
- **Earlier runs** (kept for the record): v1 (median of synced renders) showed the resolution trend, but CPU noise hid every feature; v2 (FrameTimingManager in a batch run) got 0–27 GPU samples per configuration and contradicted itself, so it is discarded.
- **Result:** picture cost scales with pixels: ≈ 0.85 ms + 0.73 ms per megapixel on the M3 Max (fit over 9 device sizes).
- **To the phone:** Geekbench 6 Metal, M3 Max (40-core GPU) 158,466 (leaked, [P-S2]) vs A20 Pro 64,069 (preliminary, [P-S3]): ratio ≈ 2.5. Phones have far less memory bandwidth, and HDR, MSAA and the post passes are bandwidth-heavy, so the phone figures are **lower bounds**.

| Configuration (iPhone path: quality 2, no glass RT) | M3 Max picture ms | A20 Pro estimate ms |
|---|---|---|
| Duo outer, as shipped | 3.5 | ≈ 7–9 |
| Duo inner (panel), as shipped | 4.0–4.4 | ≈ 10–11 |
| Duo inner (Simulator size), as shipped | 5.5 | ≈ 13.5 |
| Inner, render scale 85 % | 3.6 | ≈ 9 |
| Inner, render scale 75 % | 2.6 | ≈ 6.5 |
| Inner, 75 % + FSR 1 | 2.5 | ≈ 6 |
| Inner, 50 % | 2.2 | ≈ 5.5 (floor: passes at output size) |
| Inner, "balanced" (80 %, MSAA 2×, SSAO half / medium, bloom LQ, soft shadows medium) | 2.8 | ≈ 7 |
| Inner, "battery" (67 % + FSR, MSAA off, SSAO off, bloom LQ, soft low, 25 m) | 2.8 | ≈ 7 |
| Outer, "balanced" | 1.5 | ≈ 3.6 |

Single features at the inner size (MSAA 2× or off, HDR off, SSAO off or half, bloom off or LQ, soft shadows off, additional-light shadows off, shadow distance 20 m, post off) each moved the picture cost by less than 1 ms on the M3 Max, inside the noise: under about 2.5 ms each on the phone. They are not free, but only render scale clearly beats the noise.

**The fixed cost.** 1,770–4,470 draw calls and 660–1,760 SetPass calls per frame depending on the view (148k–368k triangles), plus the 2048 main shadow map and the 4096 additional-light atlas. In the Editor that is about 13 ms of each frame, Editor overhead included; on the A20 Pro it is unknown without a device capture. If it is large, render scale alone will not reach 60, and draw-call work (游戏视觉) matters.

### 1.4 What each option buys

1. **Render scale per display (biggest win).** RESOLUTION AUTO: the same rendered-pixel budget on every handheld screen, so the Duo inner draws at 80 %. Expected: the inner display costs about what the outer costs today. Look: slightly softer edges at arm's length on a 430 ppi panel; UI stays native.
2. **A 30 / 60 choice and Low Power Mode.** A steady 30 beats a 30–60 judder. ProMotion stays off (60 Hz cap) unless Red picks D4 · B (30 · 40 · 60 · 120).
3. **Quality presets** (MSAA, SSAO, bloom, soft shadows) add a little and protect thermals in long sessions.
4. **Draw calls** (batching, fewer SetPass calls) are 游戏视觉's work, not a setting; only a device capture shows whether they matter.

### 1.5 What is not known, and the cheapest check

- The phone's real GPU / CPU split. The Simulator's numbers are not representative; the Mac Editor is a different machine.
- Check (Red, when convenient): the Metal Performance HUD, Settings → Developer → Graphics HUD → Show Graphics HUD, shows average FPS, GPU time and frame interval. Apple: it "appears for apps you build and install yourself to your development devices" [P-S4], so it needs a development build installed from Xcode, not TestFlight. Parked in `RED_ACTIONS.md`.

## 2. What the research found

53 cases (20 PC / console, 18 mobile and Apple ports, 15 accessible menus), platform guidance from Apple, Android, Microsoft, Xbox (XAG) and the Game Accessibility Guidelines, and Unity's URP source. Full text and every citation in `reports/`.

### 2.1 Structure (R1, R3)

- A top **tab strip** with two key hints (Q / E or bumpers) is standard; depth stops at two levels. A single scrolling page with headings is enough up to about 30 rows. [R1 synthesis 2]
- Order: screen, scaling, quality with the preset first, expensive extras, camera and comfort last; comfort sits beside graphics, not in a separate "accessibility" page. [R1 1]
- Category tabs on wide layouts; a category list then a page on narrow ones; B goes up one level; same order on every screen. [R3 rule 7]

### 2.2 Presets and cost (R1, R2)

- The preset row opens the quality group and **reads "Custom"** after a detail changes; 4–6 levels are normal. Reviewers find presets buy little speed (6–15 %) and low steps look bad, so each step needs design. Presets must not silently change another setting (upscaler, AO). [R1 3]
- Mobile games lead with a preset and **pick the default from the device**, often marked "Recommended" or "For this Mac". Two naming families: quality (Low–High) and intent (Battery Saver / Performance; Energy Saving … High Definition). [R2 synthesis 1–3]
- Cost hints: per-option words or bars, a global budget bar, or a benchmark; mobile uses live load meters ("Device Load"). [R1 4, R2 7]
- Frame rate is its own control with few steps; Apple's real display steps are 120, 60, 40, 30, and Low Power Mode or heat removes 120. [R2 4, B4]
- No mobile case in the opened material shows a percent slider for render scale; it is named steps, pixel sizes or fixed. [R2 5] (The proposal keeps AUTO as the default and the percent behind FIXED.)

### 2.3 Explanation and preview (R1, R3)

- A right-hand panel with a title, one to three sentences and a picture is the norm; a "hide menu" key shows the live scene. [R1 5]
- A one-line description follows focus; preview visual effects in context (XAG 114). [R3 rule 8]

### 2.4 Components (R1, R3)

- On/off → a switch in the row; the label never changes with state; state not by colour alone. [R3 rule 1]
- Two named options that are not on/off → two segments. Three to five → segments; under four values Microsoft says radio buttons, not a slider. [R3 rules 2–3]
- Approximate amounts with a live effect → a slider with a value label, a tick at the default, and tap-to-step (XAG 107: alternatives to path gestures). On a controller, stack rows vertically so ← → only changes the value. [R3 rule 4]
- Long lists → a pop-up list. Steppers for small exact changes. [R1 6, R3 rules 5–6]
- Reset: mark changed rows with more than colour; hold-to-reset-all (God of War); avoid a lone Reset button (NN/g). Display changes get keep-or-revert. [R3 rules 11–12]
- REDUCE FLASHING says what is reduced and never claims "epilepsy safe". [R3 rule 9]

### 2.5 What Unity 6.3 URP can switch on Metal (R4)

- Public setters, next frame: render scale, upscaling filter, MSAA, HDR, shadow distance / resolution / cascades (render targets reallocate).
- No public setter: main / additional / soft-shadow switches and asset-wide soft-shadow quality (soft quality is per light).
- SSAO off: the build strips the off variant, so use a second renderer without SSAO and `SetRenderer`.
- Swapping URP assets rebuilds the pipeline: a loading-cover event, not a slider.
- Upscaling: FSR 1, bilinear, STP (forces MSAA off). **No MetalFX in Unity 6.3.**
- iOS frame rate is `Application.targetFrameRate` only; 40 and 120 need `appleEnableProMotion`.
- Low Power Mode: `UnityEngine.iOS.Device.lowPowerModeEnabled`. Thermal state: a native plugin.
- Architecture: desktop asset untouched, zero-write reference preset; three iOS-only tier assets each with an SSAO and a no-SSAO renderer; a preset = tier + sparse overrides. See `IA.md` §7.

### 2.6 Gaps

- No opened source on a settings menu for a foldable, on phone power for 30 vs 60 vs 120 fps, or on render scale across a foldable's two displays.
- Game UI Database and Digital Foundry blocked the fetch tool; several cases have text only.
- Unverified (tests listed in R4): two-asset stripping, `SetRenderer` hitch on a device, 40 fps with ProMotion, Editor GPU time.
