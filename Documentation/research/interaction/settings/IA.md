# Settings: one information architecture for Mac, Windows, iPhone, iPad and iPhone Duo (proposal, 2026-10-24)

Status: **BUILT 2026-10-24** on Red's word ("目前我希望你把这个做到当前的build里"), with Red's same-morning notes folded in (below). Was: proposal. Research behind each rule: `README.md` §2 (R1 PC/console, R2 mobile, R3 accessibility and components, R4 Unity runtime). Build waits for Red's pick (decision page). Type and sizes are provisional until 平面视觉 signs off. Figma: section `3688:7842` (page 2099:76), under DUO FLOWS.

## 0. Red's notes while it was built (2026-10-24, this line's chat)

- "这里放这些数字有些不知所云，用户很难知道具体这些数值是什么含义，我希望能够直接放更简单直的horizontal scroll bar" → no numbers in segments: CONTROLS SIZE, CONTROLS OPACITY, LOOK SPEED, CAMERA MOTION and RENDER SCALE are plain sliders; ANTI-ALIASING reads OFF · LOW · HIGH.
- "如果用户调整的是视觉相关的设置，我希望在右侧可以有一个预览图直接清楚的给玩家overview当前设置下的游戏状态，玩家点击之后能够直接暂时退出设置菜单，在点击一下之后回归" → GRAPHICS shows a live preview at the top of the right column; a tap (desktop: a click or SPACE) sets the sheet aside to show the game full screen; another tap brings it back. Narrow sheets offer the same from the explanation under the row.
- Outer display per Figma H2 (`3697:8211`) and the Duo give-way / safe-area records; every edge part per the rounded-corner spec (`research/interaction/duo/rounded_corners`: 16 pt from the curve).

## 1. Principles

1. **One system.** Same tabs, same order, same words on every device. A row that cannot work on a device is hidden, never greyed out (as HAPTICS on iPad today).
2. **Groups are tabs; one group per page.** GRAPHICS · COMFORT · SOUND · CONTROLS · ASSIST. Desktop: a tab row under the title, switched with Q / E (LB / RB on a controller). Touch: the same tab row in the sheet's header, tapped.
3. **A component per kind of row; tap-to-cycle only for two values.**
   - On/off → SWITCH. The word ON / OFF always shows; state never by position or colour alone.
   - 2–5 named choices → SEGMENTED. Every choice visible; one tap or one ← → per step.
   - An ordered number range with more than 5 steps → SLIDER with visible steps, a taller tick at the default, the value in Plex Mono, and tap-to-step (tap left or right of the thumb, or a tick). Fewer steps → SEGMENTED, even for numbers (Microsoft: under four values use radio buttons, not a slider; XAG 107: give an alternative to drag paths).
   - A long list (desktop window size, later language) → LIST that opens in place.
   - The preset → PRESET STRIP (segmented, the width of the control column) + FRAME METER.
   - An action → TEXT BUTTON (RESET GRAPHICS, KEEP / REVERT).
4. **Every row explains itself.** The focused (or last touched) row shows what it does, what it costs and its default: desktop, iPad and the Duo's inner display in a right-hand column; iPhone and the Duo's outer display in a two-line strip under the list.
5. **Changed rows show it.** A row that differs from the device default shows a small ● before its control (shape, not colour). RESET GRAPHICS (and each tab's reset) is held for 1 s, not a lone tap button (God of War hold-to-reset; NN/g on reset buttons). Backspace / X resets the focused row.
6. **Changes apply at once.** Only the desktop's DISPLAY MODE and WINDOW SIZE ask KEEP / REVERT with a 15 s countdown (reverts by itself).
7. **See it before leaving.** Hold PEEK (desktop: Space; touch: press and hold the sheet's title) to hide the sheet and see the game with the new setting, with the measured frame time.
8. **Presets set the details; touching a detail makes CUSTOM.** The other details stay as they were. Picking a preset again overwrites every detail.
9. **The desktop is the visual reference.** ULTRA = the shipped desktop look. Handheld presets are reductions of it; WebGL keeps its own tier and never shares these rows' code paths (rule: WebGL is a separate track).

## 2. Tabs and rows

Legend: ● shown · — hidden · (new) not in the game today · P1/P2 build phase.

### GRAPHICS

| Row | Component | Values (default first) | Mac | Win | iPhone | iPad | Duo | Phase |
|---|---|---|---|---|---|---|---|---|
| QUALITY | Preset strip; RECOMMENDED under the device's default | LOW · MEDIUM · HIGH · ULTRA · CUSTOM | ● ULTRA | ● ULTRA | ● HIGH | ● HIGH | ● HIGH | P1 |
| FRAME | Frame meter (read-only) | measured ms of a 60 fps frame | ● | ● | ● | ● | ● | P1 |
| RESOLUTION (new) | Segmented | AUTO · FIXED | ● AUTO | ● AUTO | ● AUTO | ● AUTO | ● AUTO | P1 |
| RENDER SCALE (new) | Slider (only when FIXED) | 50–100 % in 5 % steps | ● | ● | ● | ● | ● | P1 |
| UPSCALING (new) | Segmented (only below 100 %) | SHARP (FSR 1) · SOFT (bilinear) | ● | ● | ● | ● | ● | P1 |
| FRAME RATE (new) | Segmented | handheld 30 · 60; desktop 30 · 60 · 120 · MAX | ● 60 | ● 60 | ● 60 | ● 60 | ● 60 | P1 |
| ANTI-ALIASING (new) | Segmented | OFF · LOW (2×) · HIGH (4×) | ● | ● | ● | ● | ● | P1 |
| AMBIENT OCCLUSION (new) | Segmented | LOW (half res) · HIGH (no OFF: its variant is stripped) | ● | ● | ● | ● | ● | P1 |
| SHADOWS (new) | Segmented | LOW · MEDIUM · HIGH | ● | ● | ● | ● | ● | P1 |
| BLOOM (new) | Segmented | LOW · HIGH | ● | ● | ● | ● | ● | P1 |
| HDR RENDER | Switch | ON · OFF | ● | ● | ● | ● | ● | moves here |
| GLASS REFLECTIONS (new) | Switch | ON · OFF (ray-traced glass, Metal desktop) | ● | — | — | — | — | P2 |
| DISPLAY MODE (new) | Segmented | FULL SCREEN · WINDOW | ● | ● | — | — | — | P2 |
| WINDOW SIZE (new) | List | the display's sizes | ● | ● | — | — | — | P2 |
| VSYNC (new) | Switch | ON · OFF | ● | ● | — | — | — | P2 |
| LOW POWER MODE (new) | Segmented | FOLLOW · IGNORE (iOS Low Power Mode or a hot device → 30 fps + LOW, one step at a time with hysteresis; Apple B4/B5) | — | — | ● | ● | ● | P2 |
| RESET GRAPHICS | Text button, held 1 s | back to the device's default | ● | ● | ● | ● | ● | P1 |

**RESOLUTION AUTO** keeps the same rendered-pixel budget on every handheld screen: render scale = min(100 %, √(3.2 Mpx ÷ display pixels)), recomputed on every fold, unfold and rotation. Result: Duo outer 100 %, iPhone 17 Pro 100 %, iPad 11" 90 %, **Duo inner 80 %**, iPad 13" 75 %. On desktop AUTO = 100 %. This is the targeted fix for the inner display (see the frame-rate answer).

### COMFORT

| Row | Component | Values | Where | Phase |
|---|---|---|---|---|
| CAMERA MOTION | Slider | OFF · 50 % · 100 % (iOS Reduce Motion → OFF at first launch) | all | built |
| REDUCE FLASHING | Switch | OFF · ON; the explanation says exactly what is reduced (lamp strobes become soft dips) and never claims to be "epilepsy safe" (GAG) | all | P1 |

### SOUND (new, needs 声音设计: FMOD buses or VCAs)

| Row | Component | Values | Phase |
|---|---|---|---|
| MASTER | Slider | 0–100 % in 10 % steps | P2 |
| EFFECTS | Slider | 0–100 % | P2 |
| AMBIENCE | Slider | 0–100 % | P2 |

Hidden until built (rule 1). Captions stay in ASSIST.

### CONTROLS

| Row | Component | Desktop | Touch | Phase |
|---|---|---|---|---|
| MOUSE SENSITIVITY (new) | Slider 25–200 % | ● | — | P2 |
| INVERT LOOK (new on desktop) | Switch | ● | ● | P2 / exists |
| LOOK SPEED | Slider, five stops 60 · 80 · 100 · 120 · 150 % | — | ● | built |
| GYRO LOOK | Segmented OFF · WHILE TOUCHING · ALWAYS | — | ● | exists → segmented |
| STICK | Segmented FLOATING · FIXED | — | ● | exists → segmented |
| RUN | Segmented STICK · BUTTON | — | ● | exists → segmented |
| STICK HINTS | Switch | — | ● | exists |
| CONTROLS SIZE | Slider 80 · 100 · 120 · 140 % | — | ● | built |
| CONTROLS OPACITY | Slider 20 · 34 · 50 · 65 % | — | ● | built |
| LEFT-HANDED | Switch | — | ● | exists |
| HAPTICS · GAMEPLAY | Switch | — | ● (not iPad) | exists |
| HAPTICS · CONTROLS | Switch | — | ● (not iPad) | exists |
| BREAK GLASS | Segmented HOLD · TAP | ● (E) | ● (USE) | exists → segmented, moves from INPUT |

### ASSIST

| Row | Component | Values | Phase |
|---|---|---|---|
| CAPTIONS | Switch | OFF · ON | exists |
| RELAY READOUT | Switch | OFF · ON (off by default, Red 2026-10-03) | exists |

## 3. Presets (render-quality ladder)

Values map to the one URP asset, the SSAO renderer feature and the post volume. "Today" = what every platform ships now.

| Detail | LOW | MEDIUM | HIGH | ULTRA (desktop reference) | Today |
|---|---|---|---|---|---|
| Resolution | AUTO, budget 2.0 Mpx | AUTO, 3.2 Mpx | AUTO, 3.2 Mpx | 100 % | 100 % |
| Upscaling | SHARP (FSR 1) | SHARP | SHARP | — | — |
| Frame rate | 30 | 60 | 60 | 60 (desktop may pick 120 / MAX) | 60 |
| Anti-aliasing (MSAA) | OFF | 2× | 4× | 4× | 4× |
| Ambient occlusion | OFF | LOW (half res, 8 samples) | HIGH (full res, 12) | HIGH | HIGH |
| Shadows | LOW (soft low, 25 m) | MEDIUM (soft medium, 40 m) | HIGH (soft high, 40 m) | HIGH | HIGH |
| Bloom | LOW | LOW | HIGH | HIGH | HIGH |
| HDR render | ON | ON | ON | ON | ON |
| Glass reflections (Mac) | — | — | — | ON | ON (Mac) |

**Defaults per device:** Mac and Windows ULTRA. iPhone, iPad and iPhone Duo HIGH with RESOLUTION AUTO (on the Duo's inner display this renders at 80 %). The probe estimates the inner display at HIGH + AUTO at about the outer display's cost today. LOW is the battery and heat choice.

## 4. Input behaviour per component

| | Keyboard | Mouse | Controller (later) | Touch |
|---|---|---|---|---|
| Tabs | Q / E (and Tab / Shift-Tab) | click a tab | LB / RB | tap a tab |
| Rows | ↑ ↓ (W S); wraps at the ends | hover focuses | D-pad / left stick | tap the label: focus + explanation |
| Switch | Enter / Space or ← → | click the row | A | tap anywhere on the row |
| Segmented | ← → one step (stops at the ends) | click a cell | D-pad ← → | tap a cell (44 pt target) |
| Slider | ← → one step; hold repeats (timing to tune; no source fixes it) | drag the thumb, click the track | ← → directly (rows are stacked vertically, so focus is never trapped) | drag horizontally on the track; a vertical drag scrolls the list; tap left / right of the thumb to step; tap a tick to jump |
| Preset strip | ← → | click | ← → | tap |
| Reset this row | Backspace | — | X | — (RESET GRAPHICS button) |
| Peek | hold Space | hold Space | hold Y | press and hold the title |
| Back | Esc | click BACK | B | BACK, Android Back |

Focus is the 4 px yellow bar on the row's left edge plus the label in full paper; yellow means focus or the primary action only.

## 5. Layout per display (existing grids)

- **Desktop 1920 × 1080** (12 columns of 126 px, 24 px gutters, 72 px margin, 24 px rhythm): title row (Bayon 88/80) at the margin; tab row 24 px below; list in columns 1–7 (rows 48 px: label Bayon 20, control column in columns 5–7); detail in columns 9–12 (row name Bayon 20, explanation Source Serif 24, cost and default in Plex Mono 13); key hints at the bottom margin in Plex Mono 13.
- **iPad and Duo inner landscape** (Touch/Tablet styles): header (SETTINGS · tabs · BACK); list on the left 60 %, detail on the right 40 %.
- **Duo inner upright, iPhone, Duo outer** (Touch/Phone or Tablet styles): header; one list; a two-line detail strip above the home inset.
- All handheld sheets keep the Duo rules: full bleed, black frost at 86 %, rows give way only where they meet a camera or corner (`TOUCH_CONTROLS.md` §3.1).

## 6. Who builds what (after Red picks)

- Settings data + preset logic (`FrontRoomsSettings.cs`, new `FrontRoomsQuality`): 交互设计 with 游戏视觉 for every URP / renderer-feature write (render paths are 游戏视觉's).
- Desktop panel (`FrontRooms3DGame.cs`): 关卡设计 owns the file; the panel code goes through it.
- Touch sheet (`Input/FrontRoomsTouchControlsView.cs`): 交互设计.
- SOUND rows: 声音设计 (FMOD VCAs).
- Type and component visuals: 平面视觉 review before build.

## 7. How it is built in Unity 6 URP (from R4; build waits for Red)

- **Desktop is the reference and gets zero writes.** Mac and Windows keep `FrontRooms_URP.asset` and its renderer exactly as they are; the ULTRA / reference preset writes nothing. New graphics code is guarded to handhelds (`FrontRoomsHandheld.Active` / `#if UNITY_IOS`), so the desktop stays byte-identical until a desktop player changes a desktop row.
- **Handheld tiers are their own URP assets** (iOS/iPadOS only, excluded from desktop builds): HIGH, MEDIUM, LOW, each with two renderers, `Full` (SSAO) and `Lite` (no SSAO, no glass RT). The current build strips SSAO's off variant (`m_PrefilteringModeScreenSpaceOcclusion: 2`), so AMBIENT OCCLUSION OFF must pick the `Lite` renderer (`SetRenderer`), not disable the feature. Tier changes rebuild the pipeline: apply them behind the paused sheet's frost.
- **Fine rows use public setters on the tier asset:** render scale, upscaling filter, MSAA, HDR, shadow distance / resolution / cascades take effect next frame (render targets reallocate). Soft-shadow quality is per light. Bloom quality goes through a second, higher-priority global Volume that stays disabled on desktop.
- **A preset is a tier + a sparse override table.** Saved as `presetId` + only the rows the player changed (PlayerPrefs); CUSTOM shows when overrides exist; picking a preset clears them. Re-applied on every start.
- **RESOLUTION AUTO** is a render-scale setter run on every fold, unfold and rotation; the UI stays at native size (render scale does not touch the canvas).
- **Upscaling:** FSR 1 (SHARP) or bilinear (SOFT). There is no MetalFX in Unity 6.3; STP forces MSAA off and is untested with the glass feature, so it stays off the menu.
- **Device health:** Low Power Mode is `UnityEngine.iOS.Device.lowPowerModeEnabled`; thermal state needs a small native plugin (no Apple Adaptive Performance provider in 6.3). Step down with hysteresis, frame cap first, then render scale; never swap tiers by itself.
- **Frame rate:** iOS ignores vSync; `Application.targetFrameRate` 30 / 60. 40 and 120 need `appleEnableProMotion` (decision D4).
- **Tests before relying on it** (R4 list): tier-asset stripping, `SetRenderer` hitch on a device, 40 fps with ProMotion, a desktop pixel diff before and after.

