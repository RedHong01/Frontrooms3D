# Settings regroup + render quality: decisions for Red (2026-10-24)

Red, 2026-10-24 ~07:15 (交互设计 fold-transition chat): "目前在内屏游戏的帧率不是特别高，是否是分辨率的问题？我希望在游戏build里（mac/iphone/ipad全平台）都有调整不同渲染质量预设/细节参数的选择，我希望首先你能够给设置菜单里面的列表进行打组，以及必要的时候不要制作成点击切换，可以适当的利用滚动滑条等一系列不同的component来处理不同的设置内容，这部分我希望你能够线research，基于我已有的typographic system和grid system去深化+优化，并且参考当前现有的游戏设置UI案例".

Nothing is built yet. Each decision below says what the player would feel, what it costs, what I recommend and what it holds up. Figma: section **3688:7842** on page 2099:76 (under DUO FLOWS): KIT, DESKTOP S1–S3, HANDHELD H1–H6, DECISIONS. Research and numbers: `README.md` and `SOURCES.md` in this folder.

## Background in five lines

1. **Frame rate.** Yes, mostly resolution. The inner display has 1.76× the outer display's pixels (5.0 vs 2.8 megapixels), and every pixel pays for the desktop's settings (HDR, MSAA 4×, full-resolution SSAO, high soft shadows, high-quality bloom). Estimated GPU picture work on the A20 Pro: about 10–11 ms on the inner display against 7–9 ms on the outer, out of 16.7 ms for 60 fps, plus a fixed cost that does not shrink with resolution. Render scale is the one setting that clearly moves the frame: 75–80 % on the inner display brings it back to the outer display's cost. Single features each moved it by less than the probe's noise.
2. **Today's menu.** Six desktop rows and thirteen touch rows, all "tap to step", in two columns; groups are small Plex Mono captions.
3. **What well-regarded games do** (53 cases on PC, console and mobile, plus accessibility guidance): a tab strip; the quality preset first, reading CUSTOM when you change a detail; a component per kind of setting; an explanation panel for the focused row; cost hints; a way to see the game behind the menu. Mobile games pick the default from the device and mark it "recommended"; frame rate is its own control.
4. **The proposal.** Five tabs (GRAPHICS · COMFORT · SOUND · CONTROLS · ASSIST), the same on every device. Switch for on/off, segments for 2–5 choices, a slider only where numbers run on (render scale, look speed, volumes). A preset strip with a measured FRAME meter. RESOLUTION AUTO gives every handheld screen the same pixel budget, so the inner display renders at 80 %. Hold PEEK to see the game with the new setting.
5. **One check only you can do, when convenient.** Real FPS and GPU time on the Duo need the Metal Performance HUD (Settings → Developer → Graphics HUD → Show Graphics HUD). Apple's page says it appears only for apps built and installed from Xcode onto a development device, so it will not show on the TestFlight build. When you next run a development build on the Duo from Xcode, turn it on and screenshot one minute on each display. If you want, I prepare that build in a clone with its own test bundle id (needs your OK; listed in `RED_ACTIONS.md`).

## D1 · How the groups are shown

| | What the player feels | Cost | Holds up |
|---|---|---|---|
| **A · Tabs (recommended)** — Figma H3, H4, S1 | One short page per topic. Q / E or a tap switches pages. Each page nearly fits one screen, and the explanation has a column (or opens under the row on small phones). | Medium: a tab strip and one page per group, in both menus. | The menu code structure (desktop panel and touch sheet). |
| B · One long list with headings — Figma D1·B | Closest to today: everything in one scroll. On phones about 30 rows to scroll; the explanation has to follow the scroll. | Lowest. | Same. |
| C · Group menu first, then a page — Figma D1·C | Like iOS Settings: five groups with their current values; tap to open. One more tap to reach anything; BACK goes up a level first. | Medium, plus navigation states. | Same. |

Why A: the cases use tabs almost everywhere, the pages stay short on the 402 pt iPhone, and Q / E maps to LB / RB if a controller comes later.

## D2 · Preset names and the default

| | What the player feels | Cost | Holds up |
|---|---|---|---|
| **A · LOW · MEDIUM · HIGH · ULTRA · CUSTOM, with RECOMMENDED under the device's default (recommended)** | Words every player knows, the same on Mac and phone. The explanation says what each one trades (LOW: "saves battery and heat"). | Lowest. | Strings and the preset strip width. |
| B · Intent names on every device: BATTERY · BALANCED · QUALITY · ULTRA | Says why you would pick it; mobile games (Alien: Isolation, Sky) do this. On a Mac "BATTERY" reads odd. | Low. | Same. |
| C · Quality names on desktop, intent names on handhelds | Each platform reads naturally, but the same preset has two names. Breaks "one system". | Low, plus two string sets. | Same. |

Defaults: Mac and Windows ULTRA (the reference look); iPhone, iPad and iPhone Duo HIGH with RESOLUTION AUTO.

## D3 · The fix for the inner display

| | What the player feels | Cost | Holds up |
|---|---|---|---|
| **A · RESOLUTION AUTO on by default (recommended)** | Unfolding keeps the frame steady. The inner picture is slightly softer at arm's length (80 % of 2670 × 1878 on a 430 ppi panel); the HUD and text stay sharp. The outer display does not change. | Small: URP's render scale has a public setter that takes effect next frame; one mobile-only component sets it on every fold, unfold and rotation (gated to handhelds; desktop and WebGL unchanged; the UI stays at native size). Needs 游戏视觉 (render path owner). | Can ship before the menu rebuild, with no new rows. |
| B · Keep 100 % by default; the player can lower it | Sharpest picture; the frame stays as it is now unless the player finds the setting. | Same code, different default. | Same. |
| C · Drop the whole preset to MEDIUM when unfolded | Steadier frame, but also less ambient occlusion and softer shadows, which barely move the frame. | Small. | Same. |

## D4 · Frame rate, battery and heat

| | What the player feels | Cost | Holds up |
|---|---|---|---|
| **A · Keep the 60 Hz cap; FRAME RATE 30 · 60; FOLLOW LOW POWER MODE (recommended)** | 60 when the phone can, a steady 30 by choice; iOS Low Power Mode drops the game to 30 and LOW by itself. | Small: Low Power Mode is a Unity call (`Device.lowPowerModeEnabled`); stepping down for heat needs a small native plugin (optional). | The FRAME RATE row. |
| B · Turn on ProMotion: 30 · 40 · 60 · 120 | 40 fps is a real middle step on ProMotion screens (120 ÷ 3), smoother than 30 and cheaper than 60; 120 looks smoothest and costs the most battery and heat. | Medium: a player setting plus the Info.plist key, and testing on a device. | Same, and needs a device test (yours). |

## D5 · Build order

| | What the player feels | Cost | Holds up |
|---|---|---|---|
| **A · Two steps (recommended): first only RESOLUTION AUTO on handhelds, then the menu rebuild in phases** | The inner display gets better in the next TestFlight build; the new menu follows. | Step 1 small: one render-scale setter on handhelds, about half a day plus a device check (游戏视觉 + 交互设计). Phase 1 (tabs, components for the existing rows, GRAPHICS presets) about 4–6 days across 交互设计, 关卡设计 (desktop panel file) and 游戏视觉, who builds three iOS-only tier assets (each with an SSAO and a no-SSAO renderer) and tests stripping. The desktop asset gets zero writes. Phase 2 (SOUND with 声音设计, desktop DISPLAY rows, mouse sensitivity, PEEK). | Nothing for the Alpha; all of it lands after the Alpha unless you say otherwise. |
| B · Everything in one rebuild after the Alpha | One change, one test round; the inner display waits for the whole menu. | Same total. | Same. |
| C · Wait until after the Beta | Nothing changes for players now. | None now. | — |

## Also needs your eye (no decision required now)

- The components (switch, segments, slider, tabs, cost bars, frame meter) use your existing faces and sizes only; 平面视觉 reviews them before build.
- Games commonly dim rows that cannot be used; FrontRooms hides them (your existing rule, kept).
- The SOUND tab stays hidden until 声音设计 adds the volume buses.

## 中文摘要（给 Red）

- 内屏帧率低，主要是分辨率问题。内屏像素是外屏的 1.76 倍，每个像素都在跑桌面级效果。
- 估算：内屏每帧画面工作约 10–11 毫秒，外屏约 7–9 毫秒，60 帧的预算是 16.7 毫秒。
- 最有效的是渲染比例。内屏用 80% 左右，成本就回到外屏的水平。单个特效开关的影响都很小。
- 方案：设置分成五个标签页（画面、舒适、声音、操作、辅助），每台设备都一样。开关用开关，2–5 个选项用分段按钮，连续数字才用滑条。
- 画质预设 LOW / MEDIUM / HIGH / ULTRA，改了细节就显示 CUSTOM，并实时显示帧耗时。
- 需要你决定五件事：D1 分组方式（推荐标签页），D2 预设名字（推荐 LOW–ULTRA 加"推荐"标记），D3 内屏默认自动降分辨率（推荐开启），D4 帧率选项（推荐保持 60Hz 上限，提供 30/60，跟随低电量模式），D5 先做什么（推荐先只做内屏自动分辨率，再分阶段重做菜单，都在 Alpha 之后）。
- 真实帧率需要你在 Duo 上测：设置 → 开发者 → 图形 HUD。苹果说明它只在用 Xcode 安装的开发版里显示，TestFlight 版不显示。需要的话我在副本里准备开发版（要你同意）。
- Figma 在 3688:7842，DUO FLOWS 下面。
