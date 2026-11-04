# Waiting station print: the NO SMOKING plate and the magazine covers

From 平面视觉 · 信息设计, 2026-11-04 (account G, local_4aa27f23).
- **Ask:** 总控, from 游戏视觉's BS4 hero kits (`research/station/72_hero_kits.md` §2.2, §2.3, §3). The kits landed 11-04 06:1x with two blank faces that are this line's. Red 11-04: "继续所有昨晚暂停的工作".
- **Rules kept:** era c.1990, and every printed date is 1989 or earlier (`station/01` §5). No real brands, and no red, amber or glow-ink yellow-green (`30_look` §2).
- **Language:** world text follows the language (`narrative/95` frame A). Words are 叙事设计's; faces and layout are this line's.

## NO SMOKING plate (W17, `Kit_SignNoSmoking`)

Landed 2026-11-04 06:23. Package `W/apply/bs4_no_smoking_plate` (21 paths, MANIFEST OK); record VL501.
- **Fixed at 06:49 by 游戏视觉 (`bs4_no_smoking_fix`).** My packager wrote each PNG as the text of its own source path, so the ten textures landed as pointer files.
  - 游戏视觉 re-landed the real images (byte-identical to `out/no_smoking`, checked 06:5x) and kept every .meta.
  - The packager now copies files and writes text in two separate calls, and asserts the PNG signature of every image it copies.

To re-bake:
```
swiftc -O stair_signs.swift -o stair_signs && ./stair_signs out 0 nosmoking
```
(`W/info_type/stair_signs`; it writes the English base and the nine copies under `out/no_smoking/`.)

| Path in main | What |
|---|---|
| `Resources/Surfaces/Textures/Prop_SignNoSmoking_A.png` | 1024 x 256, sRGB, no alpha, fe7a341e. Its .meta copies `Prop_StationKeyPlate_A.png.meta`'s settings (GUID `01feb813…`) |
| `Resources/Surfaces/Prop_SignNoSmoking.mat` | `_BaseMap` set to that texture, `_BaseColor` white; smoothness 0.35 and the .meta are 游戏视觉's, unchanged |
| `Resources/WorldText/<code>/Surfaces/Textures/Prop_SignNoSmoking_A.png` | The nine copies at the base's size; the manifest lists them on the next import |

- **Plate:** engraved two-ply, 10 x 3 x 1/8 in, with two D7 chrome oval heads 15 mm in from the ends on the centre line (`sign_no_smoking.py`).
  - The decal maps the whole plate, 254 x 76 mm (kitlib `_uv_decal` maps the part's own bounds). So the art is laid out in plate mm and drawn at 4.03 x 3.37 px/mm.
  - u runs along Blender +x, which is Unity −x (`_station_common.B`), so the words read left to right from the room.
- **Colourway:** as the station's key plate (`Prop_StationKeyPlate_A`, 平面视觉): a #2B2925 cap with the letters cut to the #E2DDCF core. The same sign shop made both.
  - SPEC §3's pale "no" plates belong to the stair system; this plate sits in the station, beside the key plate's family.
- **Type:** TeX Gyre Heros Bold Condensed, caps 1 in (25.4 mm), +0.02 em, one line, the cap band on the plate's centre line.
  - In Heros Bold the line runs 237 mm and cannot clear the screws. Condensed runs 193.0 mm and stays 12.0 mm (0.47 cap) clear of each screw head (SPEC §3: condensed at the same cap when Bold overruns).
- **Relief:** _N and _S landed 07:02, at 游戏视觉's request (`W/apply/bs4_no_smoking_relief`, 40 paths, MANIFEST OK).
  - Files: `Prop_SignNoSmoking_N.png` (GUID `f4de404f…`, normal map) and `_S.png` (`d0604b4c…`, linear), plus a pair in each language's WorldText folder.
  - Recipe: W10's (`nosmoking_ns.py`). The letters sit 0.3 mm below the 3.2 mm face, blurred 0.3 mm. Gradients are taken in plate mm at this texture's 4.03 x 3.37 px/mm. S's R is 0.45 on the face and 0.25 in the letters.
  - The mesh carries the eased edges and the screws, so the maps have neither. The mask is read from each albedo.
  - 游戏视觉 wires `_BumpMap` and `_MaskMap` in its generator and the .mat.

### The ten languages (95 §5.2)

| Language | Words | Set |
|---|---|---|
| en | NO SMOKING | Heros Bold Condensed, 25.4 mm caps, one row |
| zh-Hans / zh-Hant | 禁止吸烟 / 禁止吸菸 | Noto Sans SC / TC Bold, ideographs 32.8 / 32.6 mm (the shared rule: em = cap / 0.70), no tracking |
| ja / ko | 禁煙 / 금연 | Noto Sans JP / KR Bold, 32.6 mm |
| de / fr | RAUCHEN / VERBOTEN; DÉFENSE / DE FUMER | Heros Bold, 20.2 mm caps, two rows 1.5 caps apart, 1/2 in above and below |
| es / pt-BR | PROHIBIDO / FUMAR; PROIBIDO / FUMAR | as de |
| ru | НЕ КУРИТЬ | Roboto Condensed Bold, 25.4 mm caps, one row (PT Sans waits for Red's download OK) |

- A Latin phrase that fits neither face at 1 in (270–330 mm) breaks at its word space. This is layout (SPEC §3: "the line breaks are layout"), and no word changes.
  - One row would need 16–17 mm caps; two rows keep 20.2 mm.
  - The screw heads fall in the gap between the rows.

### Reading (the line's model: 691 px/m at 1 m, 1080p, vFOV 76)

| Caps | Desktop, 18.6′ / 14′ | iPad 11-inch | Phone |
|---|---|---|---|
| 25.4 mm (en, ru) | 1.46 / 1.94 m | 1.17 / 1.56 m | 0.66 / 0.88 m |
| 20.2 mm (de, fr, es, pt-BR) | 1.17 / 1.55 m | 0.93 / 1.24 m | 0.52 / 0.70 m |

- At the 2.5 m view over the stand (W_EV_S) the caps are about 7 px. The dark plate reads as a sign above the ashtray; the words read from about 1.5 m.
- Not checked: the lit in-engine frame, which runs at the combined test (游戏视觉's list).
- The check image (VL501) projects the art into 游戏视觉's W_H_SG and W_EV_S renders, lit from the blank plate (`W/info_type/magazines/plate_mock.py`).

## Magazine covers (`Prop_MagazineCover_A`, `Kit_MagazineRack`)

Landed 2026-11-04 06:58. Package `W/apply/bs4_magazine_covers` (19 paths, MANIFEST OK); record VL502.
- **What landed:** the English atlas over 游戏视觉's placeholder at the same path, with its .meta and GUID kept, plus nine WorldText copies.
- **Checks:** every PNG was checked in main with `file` and against the baked art. The dry run now refuses a payload .png that is not a PNG (negative-tested).
- **Words:** 叙事设计 signed them on 11-04, recorded in `narrative/95` §5.6.
  - KEYSTROKE's masthead is signed only because the rack always hides it (a 1983 software trademark). If it ever shows, ask 叙事 for a new name.
  - Eight lines carry a signed fallback ("A / B"). None was needed: every primary kept at least 85 % of its cap.
- **To re-bake one language:** `./covers out <code> 8 && W/venv/bin/python atlas.py out/<code> out/<code>/Prop_MagazineCover_A.png`. The words live in `strings/<code>.json`, parsed from the 95 table; English is in `covers.swift`.
- **游戏视觉 (asked 06:58):** their `gen_bs4_waiting_surfaces.py` still draws the placeholder into this file and must stop writing it.

- **Tools** (`W/info_type/magazines`):
  - `covers.swift <out> <lang> [px/mm]` draws each cover in its own millimetres (215.9 x 279.4, spine left) at 8 px/mm.
  - `atlas.py` squashes each cover into its 512² cell in linear light (Lanczos).
  - `rack_mock.py` projects the atlas into the station renders.
- **Atlas:** 1024², cell n row-major from the top-left. Cell 0 is the top-left pocket, 1 middle-right, 2 bottom-left, 3 bottom-right (`magazine_rack.py`).
  - Each cover maps its whole cell: 2,370 px/m across and 1,835 px/m up.

| Cell | Masthead (stays English) | Kind, date | Image | Faces |
|---|---|---|---|---|
| 0 | FLOORPLATE | Commercial-space trade monthly, Nov 1989 | Axonometric CAD line drawing of an open-plan floor, white on navy | Jost ExtraBold masthead, EB Garamond Italic deck |
| 1 | TRI-COUNTY BUSINESS | Regional business monthly, Dec 1989 | 3-D bar chart '85–'90, the '90 bar hatched as a forecast | Libre Franklin Black / Bold |
| 2 | WEEKENDS | Travel monthly, Oct 1989 | Blue-hour lake, pines, crescent moon (no amber) | Playfair Display Black masthead, Jost lines, Playfair Bold Italic deck |
| 3 | KEYSTROKE | Computing monthly, Sep 1989 | Beige PC on a blue field with a perspective grid | Avant Garde (Adventor) masthead, Oswald Bold line |

- **The rack decides the layout.**
  - Each magazine leans 18°.
  - Every cover is crossed by its pocket's lip wires about 40 and 82 mm from its bottom.
  - The lower pockets are also crossed by the upper pocket's tray wires (about 200–225 mm) and its lower lip wire (about 250–265 mm).
  - Cell 3's top fifth sits behind cell 1, so its masthead never shows in the rack, and its main line sits in the band that does.
- **Didone masthead:** Playfair Display Black. Bodoni Moda's hairlines fade to under 1 px at 2.4 px/mm and break WEEKENDS into fragments (checked 11-04).
- **Languages:**
  - Mastheads are proper names and stay English. Prices and UPC digits stay. Cover lines, decks and months follow the language.
  - CJK lines use the shared rules (em = cap / 0.70, no tracking, 85 % wide before smaller). A Black headline's Gothic gets a 0.03–0.05 cap stroke so it weighs as the Latin. Lines wrap with kinsoku.
  - CJK decks are set in the Mincho or Song (Noto Serif); ru in the Cyrillic cuts of the same period faces.
  - A CJK headline's ideographs hang below the baseline, so the deck under it drops a quarter of the CJK body.
  - A two-row Latin headline splits where the rows balance and never leaves a word of two letters or fewer at the end of row one (ru: ПРОГНОЗ / НА 1990).
- **Reading in the rack** (mocks, VL502):
  - From C2, just inside E1 at 1.5–1.8 m, the four read as four magazines by colour and masthead.
  - At 2.5 m (W_EV_M) the mastheads are about 8 px tall.
  - At 0.35 m (W_H_RK) FLOORPLATE reads whole except its first side line, which sits under the lip wires.
  - The coated stock's sheen (smoothness 0.6) veils the top-tier cover in the render. The real look is the combined test's.
