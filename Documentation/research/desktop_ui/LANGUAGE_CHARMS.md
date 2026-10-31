# Language charms: landmark keychains replace the A · TAG tokens

平面视觉, 2026-10-31. Figma section `3862:9229`, LANGUAGE CHARMS (page 2099:76): LC00 rules and motion, LC01 the charms, LC02 the desktop row, LC03 the storyboard.

## Red's asks (10-31, 平面视觉 chat)

- **Landmarks, not flags.** Replace the current tag scheme with each language's landmark souvenir keychain. A landmark claims no country, so it raises no sovereignty question (Red's example: 繁體中文 as Taiwan).
- **Colour from option C only.** Take option C's coloured-keychain idea, but not its vertical list. Keep the earlier one-row language layout.
- **The key.** A key heads the row. Picking a language flies its charm onto the key, where it hangs.
- **Motion.** Keep the existing swing language. Today only a pick swings and hover doesn't, so add a hover swing.
- **English.** English is half and half: UK and US.

## Charms (v3, 2026-10-31)

Red's follow-ups:
- On the first set: "不一定用城市地标…类似于法国可以是一个可颂/法棍…综合思考哪种从远看更容易辨识" (it needn't be a city landmark; France could be a croissant or a baguette; think about what reads best from afar).
- On v2: "有一部分的图形语言是不够精致的…有部分不如建筑，比如说法国可以用埃菲尔铁塔…但是第一个的结合可以" (part of the drawing isn't refined enough; some are worse than the buildings, so France can use the Eiffel Tower; the first pairing works).
- "日本应该用富士山" (Japan should use Mount Fuji).

The rule now: use a building or an object, whichever reads better from afar. Every charm gets its own outline and its own dominant colour, and the drawing is refined with two-tone faces and fine metal detail lines.

| Code | Charm | Why this one |
|---|---|---|
| en | Elizabeth Tower + Statue of Liberty, side by side on one plinth (E2) | Red: "第一个的结合可以" (the first pairing works) |
| zh-Hans | Temple of Heaven | the only blue-and-red piece |
| ru | Matryoshka | the only red doll |
| es | Spanish guitar | one neutral Spanish; the stone Sagrada blended with the other stone pieces |
| pt-BR | Christ the Redeemer on Corcovado | a T on a peak |
| de | Brandenburg Gate | the only wide colonnade |
| fr | Eiffel Tower, two-tone with lattice lines | Red |
| ja | Mount Fuji | Red |
| ko | Hahoe mask | N Seoul Tower blended with the towers; a palace roof would echo the Temple of Heaven |
| zh-Hant | Bubble tea | a drink, not a place: no sovereignty question |

Not in the game yet: pl, tr, uk and it are v1 landmarks. Redraw them under this rule when those languages join (ideas: tr a nazar bead, uk a sunflower, it a pizza slice).

**Drawing:**
- **Canvas:** 64 × 72 units, with the A · TAG's ring tab at the top centre.
- **Inks:** one shared enamel palette, two tones per face where light falls.
- **Rim:** paper metal (#F4F1E8), 1.5 u outside and 0.55 u between fields (cloisonné).
- **Fields:** flat, no gradients. Fine metal lines carry detail: lattice, strings, wrinkles, balustrades.
- **Length:** each charm hangs at its own length.
- **Source:** `Tools/language_charms/charms.py`. Its default output is outside the project.

## Assets

- `language_charms/LangCharm_<code>@4x.png` and `@8x.png` (256 × 288 and 512 × 576), coloured on transparent. Unlike the old tag, they are not tinted.
- `language_charms/charms.json` holds, per charm:
  - the pivot, the ring hole at (32, 6.2) units, which is the swing and attach point;
  - `bboxUnits`, the die-cut silhouette, so gaps are measured between silhouettes and not canvases;
  - the scale per platform.
- `Resources/UI/KeyHud/KeyB_key@6x.png` is the header key. Its bow hole sits at (24, 54) px of its 240 × 108 crop, which is the image's alpha bbox.

## The row (the one-row strip stays)

| | Desktop px | Phone pt | Tablet pt |
|---|---|---|---|
| Charm scale (units → px or pt) | 0.6 (38.4 × 43.2) | 0.5 (32 × 36) | 0.55 (35.2 × 39.6) |
| Key | 48 wide (1.2 × the HUD key) | 40 | 44 |
| Bow hole (attach point), from the row's top-left | (14, 22) | (12, 19) | (13, 20.5) |
| Row height | 80 | 64 | 70 |
| Name: after the key's blade end | 16 | 13.6 | 15.2 |
| Strip gap between silhouettes | 10, closing to 2 when tight | same | same |

- **Current:** the charm on the key, and its name after the key, vertically centred, in Source Serif as today. The fill / outline states are retired: being on the key means current. A filled tag no longer exists.
- **The strip:** the other charms hang from one rail at the bow hole's height, right-aligned to the row's right edge, in list order.
- **Focus:** the row's yellow bar, as today.
- **Names:** a hovered or keyboard-focused charm shows its name centred under it (Source Serif 13 / 11 / 12), and the strip does not reflow. On touch, a tap picks.
- **Fit:** the gaps close first, then the current name shrinks (MinNameScale, as today). The strip keeps every charm.
- **Touch (交互设计, landed 13:22):** slide and lift. The press follows the finger along the strip, and the lift picks. Targets meet with no dead gap, each at least 24 pt wide, so the 10 pt silhouette gaps stay on touch.
- **A文 is retired:** the key is the option's header (Red). Where the word LANGUAGE can't fit, the key alone heads the row.
- **In-flight tilt:** the charm trails its ring, leaning counter-clockwise flying left to the key and clockwise flying home.

## Motion (FrontRoomsKeyHudMotion.SwingKick(t, a) = a · e^(−4t) · sin(2π · 2t), about the ring hole)

1. **Hover or focus (new):** SwingKick 4°, once per enter. It doesn't repeat while the pointer stays.
2. **Pick:** the charm lifts 4 px or pt (0.06 s). It then flies a quadratic arc from its rail point to the bow hole (0.36 s, ease in-out cubic). The arc's control point sits 28 px or pt of the row above the chord's midpoint. In flight the charm tilts up to 12° · sin(π u) into the turn.
3. **Clip on (t ≈ 0.42 s):** the key kicks −3° and follows with SwingKick 3° about its own bow hole. The charm swings SwingKick 9° for 1.5 s, which is today's pick swing (SwingDegrees, SwingSeconds).
4. **The old charm:** it lets go at the pick and flies home on an 18 px or pt arc (0.36 s), settling with SwingKick 4°. The strip's gaps open and close as today (OpenSeconds 0.16).
5. **Reduced motion:** no flight and no swing; the swap is instant.

## Owners

- 平面视觉: the charms, the key placement, this spec.
- 设置菜单与画质预设: FrontRoomsLanguageStrip.cs, the import into `Resources/Language/` (sprite, alpha, no tint, point or bilinear as the tags use), and the fit checks.
- 叙事设计: names and strings are unchanged.
