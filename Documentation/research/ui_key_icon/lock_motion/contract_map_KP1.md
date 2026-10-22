# KP-1: hand the pickup key from the 3D shot to the HUD (游戏视觉 → 关卡设计, 2026-10-14)

Status: proposal. It goes to the map chat when that session is back. Nothing is built yet. It depends on 平面视觉 confirming P4's start point.

## Why

On 10-14 Red chose the HUD key direction B (the key on its tag) and the pickup motion **P4**. In P4 the HUD glyph starts at 3× HUD size (216 × 144 at 1080p) and flies on a curve to its slot at (90, 914). The flight takes 720 ms with ease in-out (`lock_motion/README.md` §2). 平面视觉 drew P4 starting at the screen centre.

The map's pickup shot is step 5 in main, behind keys. It never brings the key to the centre (`FrontRoomsShotTimings.KeyPickup`, `FrontRoomsDoorShot.cs` around line 525):

| t (s) | What the key does |
|---|---|
| 0 – 0.25 | travels from where it lay to the held pose |
| 0.25 – 0.75 | held at viewport (0.65, 0.30), 0.35 m deep; on a 1080p frame that is about (1248, 756) from the top-left |
| 0.75 – 0.95 | drops out of the bottom of the frame to viewport y −0.25 (the "pocket") |
| 0.95 | the HUD key panel fades in (`PanelFadeIn` 0.25 s) |

## Size check

The held assembly (`Kit_Key_Zone` 58 mm, `Kit_KeyTag_Rect` 57 × 39 mm, `Kit_KeyRing` 25 mm) is about 0.11 m wide.

At a vertical FOV of 76° and 0.35 m depth, the frame is 2 · 0.35 · tan 38° = 0.547 m tall. On 1080 px that is about 1975 px/m, so the assembly is about 217 px wide.

That is the 3× glyph's width (216). The two sizes match at any aspect ratio, because both scale with screen height. Only the position differs.

## Proposal

1. **P4 starts from the held pose:** viewport (`HeldViewportX`, `HeldViewportY`) at 3×, at `HoldUntil` (0.75 s).
2. **On that same frame the 3D key is hidden** (`KeyRig.Release()` + renderers off, or the root destroyed). The `PocketStart..PocketEnd` drop is skipped, because the HUD flight replaces it.
   - `PocketSound` moves to 0.75 s, or to P4's landing. That is the sound chat's call.
3. **The hand-off signal**, map chat's pick:
   - an event such as `KeyHandoff(GridCoord zone, Vector2 viewport, float t)`;
   - or the HUD reads the `KeyPickup` constants directly.
4. **P4's own table replaces `PanelFadeIn` 0.95.** The rule grows over 160 ms at 680 ms, the label slides in from −8 px over 220 ms at 740 ms, and the meta appears at 840 ms (README §2).
5. **Tag swing.** The 3D tag swings at 25°, 2.2 Hz, decay 2.5/s from 0.25 s. By 0.75 s that is about 7° left. P4's swing is driven by its landing, so the glyph can start still and no phase hand-off is needed.
6. **If FOV ever becomes a player setting,** derive the start size from the projected bounds rather than a fixed 3×.

## Open question for the map chat

Who owns the HUD key panel code?
- If it is the HUD part of `FrontRooms3DGame.cs` (map-owned): 游戏视觉 supplies the spec and PNGs (B + padlock, @1x/2x/3x, keyline baked in), and the map chat implements P4.
- If it lives in a visual-owned Rendering/UI file: 游戏视觉 implements it.

In both cases all key and lock motion runs on unscaled time, so it keeps moving under timeScale 0 (EXAMINE, pause). That rule is in README §3.
