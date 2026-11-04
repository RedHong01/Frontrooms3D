# Language charms: flag keychains on the key

平面视觉, 2026-10-31, flags since 2026-11-01. Figma section `3862:9229`, LANGUAGE CHARMS (page 2099:76): LC00 rules and motion, LC01 the charms as in game (redrawn 11-03 22:5x: eight flags, zh-Hans and zh-Hant on their 10-31 charms until VL490), LC02 the desktop row, LC03 the storyboard. The flag tags come from option C, section `3834:8767`.

## Red's asks

- **11-01 18:58, 交互设计 chat (supersedes the 10-31 "landmarks, not flags"):** "然后目前语言选择的时候，我选择对应的钥匙链，但是语言名称应该显示在钥匙链旁边（就像是更早的方案，然后我希望你使用国旗钥匙扣方案而不是现在的物品钥匙扣方案，繁体中文的旗帜用香港/澳门/台湾合体旗帜）". So: flag keychains, the name beside each charm as in the earlier scheme, and 繁體中文 one combined Hong Kong / Macau / Taiwan flag.
- **11-01, same chat:** "在figma上已经有国旗钥匙扣了呀" and "只有没有的方案需要重新绘制". So the tag is option C's PRINT FLAG tag, reused; only the missing flags are new.
- **11-01 19:26, 交互设计 chat:** "西班牙墨西哥各一半" (es: Spain and Mexico in halves).
- **11-01 19:31, 交互设计 chat, on the HK / Macau / Taiwan tag:** "为了避免中国争议，我需要你research相关的做法，来保证我可以在中国区域上架". So zh-Hans and zh-Hant keep their 10-31 charms (Temple of Heaven, bubble tea) while 交互设计 researches the China-region rules; the repo holds no China or Taiwan flag image meanwhile.
- **10-31, kept:** the key heads the row and a pick flies the charm onto it; colour from option C; keep the swing language and add a hover swing; English is half and half, UK and US.
- **10-31, retired:** "Landmarks, not flags" (the landmark and object charms v1–v3, below in short).

## Flags (2026-11-01)

| Code | Flag | Note |
|---|---|---|
| en | United States above the fly-to-hoist diagonal, Union Flag below | option C's split, kept; the Union Flag's white saltire parts the halves, so no extra line |
| zh-Hans | **paused**: the 10-31 Temple of Heaven stays | China flag drawn, kept outside the repo until the research answers |
| ru | Russia | |
| es | Spain above the diagonal, Mexico below | English's split; Mexico's middle at 0.75 of the field so its white band and arms (eagle on the nopal, snake, lake, oak and laurel, drawn plain) sit inside its half; a 0.9 u paper-metal line parts them |
| pt-BR | Brazil | rhombus, globe, white band and 27 stars; no motto (its letters are under a pixel) |
| de | Germany | |
| fr | France | |
| ja | Japan | disc 3/5 of the height |
| ko | Korea | taegeuk tilted 33.69°, trigrams geon / gam / ri / gon at their corners |
| zh-Hant | **paused**: the 10-31 bubble tea stays | the Hong Kong / Macau / Taiwan thirds are drawn and kept outside the repo (`~/FrontRoomsVisualWork/lang_charms/flags.py`) until the research answers |

Not in the game yet: pl, tr, uk, it. Their files in `language_charms/` are the retired v1 landmarks; draw their flags when those languages join.

**Drawing:**
- **Tag:** option C's tag, from the B key's own tag vectors (Figma `2579:4780`), scaled so the rim is 62 u wide: rim (1, 11.1)–(63, 42.6), corner 5.44; tab arch r 5.98 round the ring hole r 2.72 at (32, 6.2); paper metal #F4F1E8.
- **Field:** inset 1.58 u (the old outer rim was 1.5), corner 3.86, 58.85 × 28.39 u (2.07 : 1, as in option C for every flag). Flags keep their own construction at the field's height; a 3 : 2 flag's extra fly is plain field.
- **Inks:** option C's spot inks (red 200 16 46, navy 15 41 115, gold 224 173 43, green 31 107 59, paper, ink 20 20 20), plus each flag's own where option C had none: Macau jade, ROC blue, Korea's legislated pair, Brazil's green, gold and blue. Flat, hard-edged, no gradients.
- **Keyline:** 0.4 u (204 197 180) where the rim meets the field, so the white fields of ja, ko, ru and en keep their edge.
- **One die-cut:** every flag hangs the same length; `bboxUnits` is [1, 0.22, 63, 42.64] for the eight flags (zh-Hans and zh-Hant keep their 10-31 die-cuts).
- **Source:** `Tools/language_charms/flags.py` (supersampled 32 px per unit, premultiplied box filter to 8 and 4). Its default output is outside the project. The landmark generator `charms.py` stays as the record of v1–v3.
- **Legibility (VL478):** at desktop 1x (0.6 px per unit, the texture's mip 3) every flag still reads by its colour pattern; China's small stars and Korea's trigrams turn to texture there and read at 2x and on phones. Sheet: `images/vl478_flag_charms.jpg`.

**Sources** (geometry read in the browser, nothing downloaded; flags are public domain): Wikimedia Commons SVGs `Flag_of_Hong_Kong.svg` (from the ordinance's construction sheet), `Flag_of_Macau.svg`, `Flag_of_the_Republic_of_China.svg`, `Flag_of_South_Korea.svg`, `Flag_of_Brazil.svg`; Wikipedia "Flag of South Korea", "Flag of Hong Kong", "Flag of Macau", "Flag of Taiwan" for the stated ratios.

## Before: landmark and object charms (10-31, retired 11-01)

v1 landmarks, v2 objects allowed, v3 refined: Elizabeth Tower + Statue of Liberty, Temple of Heaven, matryoshka, Spanish guitar, Christ the Redeemer, Brandenburg Gate, Eiffel Tower, Mount Fuji, Hahoe mask, bubble tea. Red's words then: "不一定用城市地标…综合思考哪种从远看更容易辨识", "法国可以用埃菲尔铁塔", "日本应该用富士山". Generator `Tools/language_charms/charms.py`; art in the 10-31 13:23 LANDED row.

## Assets

- `language_charms/LangCharm_<code>@4x.png` and `@8x.png` (256 × 288 and 512 × 576), coloured on transparent, not tinted. The in-game copy is the @4x as `Resources/Language/LangCharm_<code>.png` (same GUIDs since 10-31).
- `language_charms/charms.json` holds the canvas, the pivot (the ring hole at (32, 6.2), the swing and attach point), the units per px or pt per platform, the tag's geometry, and per charm `bboxUnits`, the die-cut, so gaps are measured between silhouettes and not canvases.
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
- **Names:** Red 11-01 wants each name beside its charm, as in the earlier scheme; 交互设计 lays it out in `FrontRoomsLanguageStrip.cs`, and this section follows when it lands. Until then a hovered or keyboard-focused charm shows its name (Source Serif 13 / 11 / 12), and the strip does not reflow. On touch, a tap picks.
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

- 平面视觉: the charms (`flags.py`), the key placement, this spec.
- 交互设计 and 设置菜单与画质预设: FrontRoomsLanguageStrip.cs (its silhouette table mirrors `charms.json`), the import into `Resources/Language/` (sprite, alpha, no tint, point or bilinear as the tags use), and the fit checks.
- 叙事设计: names and strings are unchanged.
