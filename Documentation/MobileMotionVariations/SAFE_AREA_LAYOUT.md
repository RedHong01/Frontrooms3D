# iPhone safe-area correction

The mobile Figma master is a full 874 × 402 pt phone frame. Its visible content already reserves the horizontal 62 pt insets and the 21 pt bottom inset. The previous runtime put the entire Figma frame inside a parent whose anchors were also set to `Screen.safeArea`, applying those insets twice. That made the settings card, menu wash, chips, and some touch controls drift or clip.

The correction keeps `Phone Frame` at the full CanvasScaler frame and uses `Screen.safeArea` only when calculating dynamic action positions:

- USE center: safe-area edge −72 pt, bottom +119 pt → full-frame center (740, 262).
- Pause hit: safe-area edge −28 pt, top −34 pt → full-frame hit target around (784, 12).
- Fixed stick: safe-area edge +88 pt, bottom +123 pt → full-frame center (150, 258).
- Settings card: (66, 10, 742, 369).
- Settings CLOSE chip: center (749.5, 38), visual size 65 × 40.
- Hint card: (211, 311, 452, 60), bottom anchor 31 pt.
- Optional captions: 214 pt rail above the hint card, bottom anchor 99 pt;
  this prevents the accessibility text from overlapping the hint surface.

The touch state machine uses the same safe-area-derived targets when its visual bindings are unavailable, so fallback hit testing no longer disagrees with the rendered controls.
