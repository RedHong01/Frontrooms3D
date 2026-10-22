# Deny() channels: mapping from the lab (平面视觉, 2026-10-14)

This applies when Red picks one of D·1–D·4 and its `DENY` function from `~/FrontRoomsVisualWork/keyicon_motion/lab_template_v2.html` is ported into `FrontRoomsKeyHud.Deny()`. The first build has only a no-op stub, which writes 0 to every channel.

| Lab value | Meaning (SVG: y points down) | FrontRoomsKeyHud channel | Unity UI sign |
|---|---|---|---|
| `P.x` | lock x offset (D·4 trembles it ±0.3 px at 26 Hz) | lock x offset | same |
| `P.tx` | text x offset: the same value shifts both prompt lines | text x offset | same |
| `P.shY` | shackle offset relative to the lock; negative = up | shackle y offset | **flip** (Unity +y is up) |
| `P.y` | recoil = a whole-lock y offset, shackle included; positive = down, at most 0.8 px | recoil | **flip**; **no scale** (leave any scale term at 1) |

- On screen the shackle moves by `P.y + P.shY`.
- Only D·1 and D·3 use recoil.
- D·4 writes `P.x` but leaves the text in place.
- The window is 500 ms on unscaled time.
