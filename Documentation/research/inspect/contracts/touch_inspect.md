# EXAMINE: contract for the touch session (`Assets/Scripts/Input/*`)

**Status 2026-10-22 (交互设计):** T1–T5 LANDED (see LANDINGS; `TOUCH_CONTROLS.md` §2.3, `INTERACTION_DESIGN.md` EXAMINE on phones). T6/T7 wait for the runtime transcript (`FrontRoomsInspectCatalog`, 游戏视觉; the placard's entry is an empty placeholder) and follow 平面视觉's G9/G10 (`research/desktop_ui/EXAMINE_TEXT_G9_G10.md`: TEXT chip in the pause slot, panel at the right 36 % of the safe area, x 542–812, y 64–280 on the phone). The map side's C7 (zoom wired, `examine_c7_game.diff` from 交互设计) and C14 (READ / BACK, done in the touch landing) are noted in `map_inspect.md`.

From the visual chat (游戏视觉), hand-off stage, 2026-10-14.
Change requests for files the touch session owns. The visual chat never edits them. No diff yet: the touch files change often, so the touch session writes the patch against its own live lines. Line numbers below were read on main 0a9ee7f on 2026-10-14; re-read before patching.

**What it is:** walk up to the evacuation plan, tap it (or USE), and the game pauses on a close-up of it. Design: `../10_design.md` §2, §5, §6 and §7.4. Map side: `map_inspect.md`.

## 1. What works on touch before T1–T7 (the interim in the map diff)

The map chat's `map_inspect_3_game.diff` already makes a close-up usable on a phone without any touch change:

| Action | Interim (map diff only) |
|---|---|
| Open | **tap the target** (C15: the tap ray asks `FrontRoomsInspect.Find` first, through the same gate as E). The prompt line shows `READ PLAN` under the dot, with no USE glyph |
| Look | right-zone drag, as today (on the placard: yaw −1.43° to +0.28°, pitch ±1.19°) |
| Exit | the left-stick **pull-back** (today's `ShotBackDepth`, gives `ShotBackDown`), the **pause button** (`PauseDown`), or **Android Back** (C15) |
| Menu state | stays `MenuState.Playing` in Inspect (C13 interim) |
| Zoom | none (`FrontRoomsInspect.ZoomInputWired` stays false, so no CLOSER hint) |

## 2. Requests (T1–T7)

| # | Where (2026-10-14) | Change | Why |
|---|---|---|---|
| T1 | `FrontRoomsTouchControls.cs:53` `MenuState`, `Begin` (`:678`) | Append `MenuState.Inspect`. Route: right-zone drag → Look; two-finger pinch → Zoom (continuous); double-tap → ZoomToggle; left zone: a hidden stick whose pull-back → `ShotBackDown` (exit); USE (shown as BACK) → Use (exit); no pause button, no sprint socket. **Reset any double-tap detector on every MenuState change** | The tap that opened a close-up must never also zoom it. The pause button is not needed: BACK exits |
| T2 | `:55` `UseKind`, `FrontRoomsTouchSprites`, the view | Append `UseKind.Read` and `UseKind.Back` (glyphs by 平面视觉, G10). In `MenuState.Inspect` the view shows USE as BACK whatever `prompt.Visible` says, **moved to the lower right** (proposed centre 768, 337 pt on the 874 × 402 phone layout; Ø88 hit spans y 293–381, inside the safe area). The map chat then adds C14 (prompt → `UseKind.Read`) | BACK clears the transcript panel (y 64–280 pt) and the framed plan |
| T3 | `FrontRoomsInput.cs` | A new `Zoom` action. Scroll y gives ±1 step per gesture, re-armed after 0.2 s with no scroll; RMB press toggles; pinch gives a continuous fraction of a step. `FrameSnapshot` gains `float Zoom; bool ZoomToggleDown;` (constructor params at the end, defaulted, so the touch playtest compiles) | **This turns the zoom on.** After T3 the map chat sets `FrontRoomsInspect.ZoomInputWired = true` and passes `input.Zoom`, `input.ZoomToggleDown` in C7 (one line). Desktop gets scroll and RMB from the same action |
| T4 | `FrontRoomsTouchControlsView.cs:712` `Washed` | `MenuState.Inspect` is not washed | No frost over the close-up |
| T5 | `FrontRoomsMobileInteractionEvents.cs` | `InspectOpened()` → one light haptic tick | The open is felt, like a door latch |
| T6 | view + controls | A **TEXT chip** in Inspect when the target has a transcript. It raises `TranscriptToggled`; the game flips `FrontRoomsInspect.ShowTranscript` | The transcript is the accessibility path (off by default on desktop) |
| T7 | layout | The transcript panel on handheld: right 36 % of the safe area, y 64–280 pt on the phone (scaled on iPad). Confirm it against your layout | 平面视觉 designs the panel (G9); you place it |

## 3. Numbers the touch side should know

| | Phone (2.17) | iPad (1.44) |
|---|---|---|
| Composition | A (plan in the left 62 %, wall on the right) at 0.360 m | B (centred) at 0.328 m |
| Zoom step (after T3) | 0.18 m, FOV 30.6° (the angle binds on main's sheet) | 0.184 m, FOV 51.4° (design value, not yet captured) |
| Smallest line at the zoom step | ≥ 18 arcmin (AT-06) | same |
| Cancel | inputs before 0.20 s are swallowed | same |
| Tag texture import cap | 2048 | 2048 |

## 4. Tests (AT-06, touch playtest harness; iPhone 16 Pro profile 2622 × 1206, and the iPad profile)

1. Tapping the target opens the close-up (through `pendingInspect`). Taps during the 0.40 s return never reach `map.Use`.
2. Pinch zooms continuously to the zoom step. A double-tap on the target opens it without also zooming it. Inside, double-tap toggles the zoom; drag pans or turns.
3. BACK, a left-zone pull-back and Android Back each exit after 0.20 s.
4. Android Back from Paused-from-Inspect returns to the close-up; no stale `pausedFrom` afterwards.
5. The TEXT chip opens the transcript. The panel and BACK hit rects do not overlap.
6. Angular check on the phone capture: at the zoom step every placard line ≥ 18 arcmin (±10 %).
7. No frost over the close-up. The Caught card never shows for Inspect.

Not yet run: the phone capture (AT-06) and T1–T7. The interim in §1 compiled for iOS (Roslyn, 0 errors) but was not played on a device.
