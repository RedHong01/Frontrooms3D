# 03 — Key icon: hand-off to 平面视觉

**Status: FINAL, 2026-10-07 (fix stage).** The critic (2026-10-07) and fix stages are done. The visual chat now pings 平面视觉 with §0. Red's decisions are in `04_for_red.md` §4; nothing is installed in the game.

**No key-HUD block was built in Figma by this workflow** (`figma_target.md`). 平面视觉 owns the native build: section `2532:4038` "FRONTROOMS · HUD KEY · UI VARIATIONS (平面视觉)" at (68657, 2000), library KV-LIB `2579:4775`, sync in `Frontrooms3D/Tools/figma/hud_key/`. This file is the source list for their re-sync.

Paths are under `Documentation/research/ui_key_icon/` unless they start with `Assets/`, `Tools/` or `$W` (= `/Users/redwang/FrontRoomsVisualWork`, the persistent work root that replaced the wiped `/private/tmp` scratchpad). Main checked at `279c144`.

---

## 0. Fix stage changes (2026-10-07): the re-sync list for 平面视觉

### (a) `composites.py`, old → new

| Constant / function | Old (2026-10-03) | New |
|---|---|---|
| `prompt()` | no ground; glyph + "LOCKED" **muted** + meta muted; glyph at its box origin | the same row on a **card** `CARD` `#141414` at `CARD_OP` **90 %** (= the HUD hint card, `media` in `BuildHud`), padding `CARD_PAD_X` **12** × `CARD_PAD_Y` **8** px around the glyph row and the meta line; "LOCKED" in **paper**; meta stays muted; new keyword arguments for the phone (`cx`, `cy`, `label_sz`, `meta_sz`, `gap`, `meta_dy`, `pad`). The old function is kept as `prompt_v1` (not called by any entry point) |
| `OUTLINE_PAD` (new) | — | `A_L_missing` (1, 1), `A_S_missing` (1, 0), `C_dark_missing` (1, 0), `B_rect_red_missing` (0, 0): `compact()`, `full()` and `prompt()` draw these sprites that many px left and up, so the key stays where it was |
| `compact()`, `full()` | meta always `MUTED` | new `meta_col` argument (default `MUTED`) |
| States (new `state()`) | other: `<dir>_held` at **40 %**, label `MUTED` at **70 %**, meta muted. Used: `<dir>_muted` at 100 %, label muted, meta muted | other: the **outline** `<dir>_missing` at 100 % (`OTHER_OUTLINE`; A + chip has none, so `Achip_rect_red_held` at `FLOOR_OP` **70 %**), label `PAPER` 100 %, meta `PAPER`. Used: `<dir>_held` at `FLOOR_OP` **70 %**, label `PAPER`, meta `PAPER` |
| `STATE_BGS` (new) | states on lit, dark | held / other / used on lit and dark; **missing on lit, dark, wallpaper, office, leaf** |
| `PROMPT_BOX` (new) | missing crops at (720, 500, 1200, 620) | (720, **512**, 1200, **632**): the card runs to y 622 |
| `BGS` | lit, wallpaper, office, dark | + `leaf` = `design/bg/leaf_48_almond.png`. `BG_ROW` = the four room frames (the `held` loop, unchanged set). `BG_MEASURE_ONLY` = `leafwood` (never cropped) |
| Entry points | `all`, `held`, `states`, `labels`, `zones`, `full` | + **`phone`** (skipped by a recording run: `L.__name__` is not `iconlib`). `full` also writes `A_L_fullframe_target_wallpaper.jpg` (board 10 hero) |
| New helpers | — | `today_prompt()` (the code's prompt string; Bayon has no "·", drawn with Plex Mono), `phone_frame()`, `disc()`, `phone_hud()`, the `MEASURE` hook (inactive unless set) |

Unchanged: `ROW_Y` 940, `LABEL_BASE` 958, `FULL_Y` 914, `PAPER`, `MUTED`, `ACCENT`, `COUR_SZ` 25, `MONO_SZ` 20, `DIRS`, `MISSING`, `ROOM`, every other crop box and every old output stem. Their `plan.py` (read-only copy, `KD` repointed) records **198 outputs** (was 182) and op counts text 430, sprite 198, rect 57, crosshair 46 (was 396 / 182 / 32 / 30).

### (b) Files

| Change | Files |
|---|---|
| **SVG masters changed (13)** | `A_L_missing` (box 49 × 22 → **51 × 24**), `A_S_missing` (40 → **42** × 22), `C_dark_missing` (40 → **42** × 22): the outline's centred stroke no longer runs out of the box. The 10 `Achip_*` masters: a new first layer `ring` (1 px paper wire, an arc from the tag hole to the bow hole); sizes unchanged (77 / 69 / 85 × 22). Their @1x / @2x / @3x PNGs (39) change with them. KV-LIB: re-import these 13 |
| **HUD crops changed (80)** | every `*_state_{other,used,missing}_{lit,dark}_1x` (30); `Achip_state_held_*` (2), `Achip_held_*` (8), `Achip_fullframe_*` (4), `Achip_zone_*` (36). Per file: `changed` in `03_variants.tsv` |
| **HUD crops added (22)** | `<dir>_state_missing_{wallpaper,office,leaf}_1x.png` (15), `A_L_fullframe_target_wallpaper.jpg` (1), `phone_{today,target}_{held,locked}_<bg>_3x.jpg` (6, not twins) |
| Backgrounds added | `design/bg/leaf_48_almond.png`, `design/bg/leaf_48_wood.png` (§4) |
| Drop-in added | `dropin/slot_40x22/HUD_KeyGlyph_Touch.png` (120 × 66), `dropin/slot_49x22/HUD_KeyGlyph_Touch.png` (147 × 66), `dropin/prompt/HUD_KeyOutline.png` (102 × 48), `HUD_KeyOutline_Touch.png` (153 × 72), `HUD_KeyOutline.svg`, `dropin/crosshair/HUD_Crosshair_Touch.png` (144 × 144) |
| Boards | all 10 re-rendered (header date Oct 7; text and layout fixes on 01, 03, 05, 07, 09, 10). Not part of the sync |
| Removed | none |
| **Not part of the set** | 9 iCloud conflict copies tracked in git (`03c43ed`, `956b786`): `design/hud/A_L_fullframe_lit 2.jpg`, `A_S_fullframe_{dark,lit,office,wallpaper} 2.jpg`, `Achip_fullframe_{dark,lit,office,wallpaper} 2.jpg` (the `Achip_* 2.jpg` are the old 17:16 renders, numeral 24.63). Ignore them; the iCloud chat owns them |

### (c) Your tools (`Tools/figma/hud_key/`, yours, not edited here)

- `plan.py:11` and `fits.py:6`: `KD` points at the wiped `/private/tmp/…/scratchpad/keyicon_design`. Repoint to **`/Users/redwang/FrontRoomsVisualWork/keyicon_design`**.
- `fits.py:14`: `BGS["dark"]` → `Documentation/research/ui_key_icon/design/bg/dark_59_door_key_open_t0000ms.png` (same bytes).
- `fits.py`: add `BGS["leaf"]` = `design/bg/leaf_48_almond.png`; 5 new missing crops sit on it.
- The prompt now has a `rect` op (the card, `#141414`, opacity 0.9): fit its sRGB alpha per crop like the other semi-transparent elements.
- The outline sprites carry a 1 px offset (`OUTLINE_PAD`); `plan.py` records the shifted x / y.

---

## 1. Recommended direction and states

**Recommendation: A · Cut key** (`02_design.md` §2). `Kit_Key_Zone`'s own outline, one paper colour, like the HUD type; the crosshair dot stays white.
- **Ship today:** `dropin/slot_40x22/HUD_KeyGlyph.png` in today's 40 × 22 rect. No code change, but on desktop the panel only shows after the anchor fix (`04_for_red.md` §1).
- **Target:** 49 × 22 (`A_L`) + label "KEY" in Bayon 20 and the zone number in Courier Prime Bold 25. Contract request in `04_for_red.md` §5.3.
- **Options:** A + tag chip (once zones carry tag data); B for 2x moments. C is not used.

States (final; opacities are engine values, blended in **linear light**, §2.4):

| State | Glyph | Label | Meta (IBM Plex Mono 13) | Where |
|---|---|---|---|---|
| **Held, this zone** | `<dir>_held` at 100 % | paper `#F4F1E8` | none (B: "OPENS DOORS OUT OF THIS ZONE", muted) | bottom-left row |
| **Held, other zone** | the outline `<dir>_missing` at 100 % (its stroke is paper at 70 % inside the SVG); A + chip: `Achip_*_held` at **70 %** | paper | "NOT THIS ZONE", **paper** (B: "OPENS ZONE 14 ONLY", no yellow rule) | bottom-left row |
| **Used** (only if keys become single-use) | `<dir>_held` at **70 %** | paper | "DOOR OPENED", paper | bottom-left row |
| **Missing** (locked door) | `<dir>_missing` (B and A + chip use `A_L_missing`) | "LOCKED", Bayon 20, **paper** | "NEEDS KEY 14", centred, muted | under the crosshair, on the card `#141414` 90 % |

The `_muted` masters (5) stay in the library but no state uses them.

---

## 2. Every constant in `composites.py` (final values)

### 2.1 Layout (1080p canvas, 1x px; 2x = the same numbers × 2)

| Constant / rule | Value | Source in `composites.py` |
|---|---|---|
| Canvas | 1920 × 1080, CanvasScaler match 0.5 | docstring |
| Glyph x | **72** (outline sprites: 72 − pad) | `compact()`, `full()` |
| `ROW_Y` (glyph rect top, compact row) | **940** (outline sprites: 940 − pad) | constant |
| Row height | **22** | glyph height |
| `LABEL_BASE` (label baseline) | **958** (= `ROW_Y` + 18) | constant |
| Label x | glyph x + glyph slot width + **14** (A_S 126, A_L 135, C 126, A + chip rect 163 / round 155 / long 171) | `compact()` |
| "KEY" → numeral gap | "KEY" advance (Bayon 20: 25.79 px) + **6** | `draw_label()` |
| Meta after the label | label end + **12**, baseline **957** (`LABEL_BASE` − 1) | `compact()` |
| `FULL_Y` (full form top, B) | **914**, 48 high, same bottom edge (962) as the compact row | constant |
| Full form rule | **4 × 48** at x 72, accent `#F4DF3B` | `full()` |
| Full form glyph x | 72 + 4 + 14 = **90** (no rule: 72) | `full()` |
| Full form name / meta | glyph x + glyph width + 14; baselines **936** / **955** | `full()` |
| Prompt row | glyph + **10** + "LOCKED", centred on x 960; row centre y **584**; "LOCKED" baseline **591** | `prompt()` |
| Prompt meta | centred on x 960, baseline 584 + max(30, glyph h / 2 + 16) = **614** | `prompt()` |
| Prompt card | x: min(glyph x, meta x) − 12 … max(row end, meta end) + 12; y: glyph top − 8 … meta baseline + 8 = **565 … 622** (A_L: x **888 … 1032**, 144 × 57) | `prompt()` |
| `PROMPT_BOX` (missing crops) | **720, 512, 480 × 120** | constant |
| Crosshair | 32 × 32 rect at (944, 524), the 64 × 64 sprite (dot Ø 16 on screen) | `crosshair()` |
| Room meta | IBM Plex Mono 13, muted, x 96, baseline **102** | `room_type()` |
| Room name | Source Serif 4, **50**, paper, x 96, baseline **165** | `room_type()` |

**Phone** (`phone_hud()`, pt, drawn @3x; TestFlight layout at `279c144` + the TOUCH_CONTROLS §4 ramp): canvas 874 × 402; zone rule 3 × 40 at (79.5, 16); meta Plex 11 at x 91, baseline 26; zone name Serif 26, baseline 59; key row top-left (78, 72), glyph 49 × 22, label x 139, Bayon **17** + Courier Prime Bold **21**, 5 pt gap; crosshair dot Ø **12** at (437, 201); prompt row centre **233** (32 under the dot), label 17, meta 11 at +26, gap 8, card padding **10 × 7**. "Today" draws main's 40 × 22 px sprite stretched ×3, label Bayon 11 at x 132, and the 64 px crosshair sprite in the 72 px rect.

### 2.2 Type sizes (whole numbers)

| Use | Face | Size | Cap height |
|---|---|---|---|
| Label words ("KEY", "LEVEL 0 KEY", "LOCKED") | Bayon Regular | **20** (phone 17) | 14.28 px |
| Key numeral (`COUR_SZ`) | Courier Prime Bold | **25** (phone 21) | 14.49 px |
| Numeral, Plex option and C zone labels (`MONO_SZ`) | IBM Plex Mono Regular | **20** | 13.96 px |
| Meta | IBM Plex Mono Regular | **13** (phone 11) | — |
| Room name | Source Serif 4 (variable, default instance) | **50** (phone 26) | — |

In Unity: the numeral is a second `Text` element (legacy `Text` cannot switch fonts inside one string).

### 2.3 Glyph sizes (1x; the SVG masters' width × height)

| Glyph | Size | Note |
|---|---|---|
| `A_S_held`, `A_S_muted` | 40 × 22 | key 40 × 18, centred; today's slot |
| `A_S_missing` | **42 × 22** | 40 × 22 slot + 1 px left and right |
| `A_L_held`, `A_L_muted` | 49 × 22 | key fills the row; target |
| `A_L_missing` | **51 × 24** | 49 × 22 slot + 1 px all round |
| `C_*` | 40 × 22 | plate 40 × 20, R 2; `C_dark_missing` **42 × 22** |
| `Achip_rect_*` / `round_*` / `long_*` | 77 / 69 / 85 × 22 | chip + 4 px + A_L key; ring wire inside the box |
| `B_*` | 72 × 48 | key + ring + tag; needs the 48 px full form |

### 2.4 Colours and opacities

| Name | Value |
|---|---|
| Paper | `#F4F1E8` |
| Muted | `#BDBAB0` (prompt meta, room meta, B's held meta) |
| Accent (B rule only) | `#F4DF3B` |
| Card (prompt ground, C dark plate) | `#141414` |
| Ink (C white plate key, B tag numbers) | `#1E1D1A` |
| Zone red / blue / white (`Prop_Plastic*` base colours, sRGB) | `#8C0F0D` / `#0F2973` / `#FFFFFF` |

| Opacity (linear-light engine value) | Where |
|---|---|
| **90 %** | the prompt card; C dark plate (held) |
| **70 %** | `FLOOR_OP`: the used glyph, the A + chip other-zone glyph; the outline stroke (inside the SVG) |
| **60 %** | C dark plate (muted master, unused) |
| **55 %** | tag or chip fill in the muted masters (unused) |
| 100 % | everything else |

**Blending.** The composites blend every layer in **linear light**, like Unity's overlay canvas in a linear-colour project (no post on the HUD). Figma blends in **sRGB**: keep fitting an sRGB alpha per crop for every semi-transparent element (90 / 70 %).

### 2.5 Contrast (WCAG ratio, p10 / median of the ground pixels under each element; `measure.py`)

Locked prompt, per ground (today's code prompt has no glyph):

| Ground | Today: text | Oct 3: key | Oct 3: LOCKED | Oct 3: meta | **Fix: key** | **Fix: LOCKED** | **Fix: meta** |
|---|---|---|---|---|---|---|---|
| lit low room | 2.45 / 2.84 | 1.80 / 8.79 | 6.98 / 7.47 | 2.31 / 3.02 | 7.16 / 11.31 | 15.75 / 15.88 | 7.23 / 7.81 |
| wallpaper | 2.45 / 2.76 | 1.99 / 2.14 | 1.72 / 2.04 | 1.56 / 2.13 | 7.54 / 7.80 | 11.23 / 11.96 | 6.29 / 7.05 |
| office | 4.03 / 4.21 | 10.97 / 11.26 | 4.22 / 8.99 | 5.35 / 7.28 | 11.61 / 11.64 | 14.50 / 16.22 | 8.81 / 9.21 |
| dark cell | 13.30 / 13.80 | 9.46 / 9.73 | 7.61 / 7.92 | 7.61 / 7.92 | 11.42 / 11.46 | 15.92 / 16.00 | 9.26 / 9.30 |
| **almond locked leaf** | 1.45 / 1.45 | 1.31 / 1.31 | 1.19 / 1.19 | 1.19 / 1.19 | 5.92 / 5.92 | 8.04 / 8.04 | 4.68 / 4.68 |
| wood locked leaf (today) | 13.13 / 13.63 | 9.40 / 9.70 | 7.61 / 7.83 | 7.61 / 7.83 | 11.41 / 11.45 | 15.92 / 15.97 | 9.26 / 9.29 |

Bottom-left row (lit carpet = the worst frame; dark for reference):

| Case | Key, lit | Label "KEY", lit | Meta, lit | Key, dark |
|---|---|---|---|---|
| A held | 4.10 / 4.28 | 4.04 / 4.16 | — | 13.18 / 13.35 |
| A other, Oct 3 | 2.24 / 2.31 | 1.95 / 1.99 | 2.16 / 2.29 | 5.87 / 5.94 |
| **A other, fix** | 3.16 / 3.29 | 4.04 / 4.16 | 3.72 / 3.93 | 9.50 / 9.62 |
| A used, Oct 3 | 2.38 / 2.49 | 2.35 / 2.42 | 2.22 / 2.32 | 7.67 / 7.77 |
| **A used, fix** | 3.17 / 3.30 | 4.04 / 4.16 | 3.82 / 3.99 | 9.53 / 9.65 |
| B other, fix | 3.12 / 3.25 | 3.93 / 4.04 | 3.82 / 3.99 | 9.39 / 9.62 |
| C other, fix | 3.16 / 3.29 | 4.04 / 4.16 | 3.77 / 3.93 | 9.50 / 9.62 |
| A + chip other, fix | 3.17 / 3.25 | 3.87 / 4.04 | 3.67 / 3.87 | 9.44 / 9.65 |

Bars: 3 : 1 for graphics (WCAG 1.4.11), 4.5 : 1 for text under 24 px. Paper on lit carpet is itself 3.9–4.3 : 1, so no small paper text reaches 4.5 : 1 there without a ground (a HUD-system choice). Raw rows: `$W/keyicon_figma/measure_fix.json`.

---

## 3. History: what the Figma stage changed (2026-10-03, for the record)

### (a) Constants, old → new

| File | Constant | Old | New |
|---|---|---|---|
| `composites.py` | `COUR_SZ` | 24.6335 (float, from the cap-height match) | **25** (int) |
| `composites.py` | Plex numeral size (inline `mono_sz` in the `labels` and `zones` sections) | 20.4546 | **20**, via the new constant `MONO_SZ` |
| `composites.py` | new: `COUR_SZ_EXACT`, `MONO_SZ_EXACT` | — | 24.6335, 20.4546 (reference only) |
| `composites.py` | `BGS["dark"]` path | `$SP/audit_run3_frames/59_door_key_open_t0000ms.png` | `design/bg/dark_59_door_key_open_t0000ms.png` (same bytes; re-render checked byte-identical) |
| `composites.py` | `full` section | wrote `*_fullframe_*.png` (the JPGs were converted by hand at 17:16) | writes PNG, converts to JPG (macOS `sips`, quality 92, as before) and removes the PNG. A recording run (their `plan.py`) never converts or deletes: guarded by `L.__name__ == "iconlib"` and the file existing |
| `design.py` | `emit(png_sizes=…)` | (1, 2) | **(1, 2, 3)** |

**Unchanged:** every other constant (`ROW_Y` 940, `LABEL_BASE` 958, `FULL_Y` 914, `PAPER`, `MUTED`, `ACCENT`, `DIRS`, `MISSING`, `ROOM`, all crop boxes), the entry points (`all`, `held`, `states`, `labels`, `zones`, `full`), the function names and every output file stem. 平面视觉's `plan.py` was run on the new file (read-only, output in `$SP/keyicon_figma/plan_after.json`): 182 outputs, the same op counts (396 text, 182 sprite, 32 rect, 30 crosshair), and only size or x-position changes.

### (b) Files

**Changed (113 HUD crops).** Every crop that sets a numeral changed, plus the text set after it. 82 of them are 1x twins, 26 are 2x, 4 are full frames, 1 is a step crop:

| Family | Files |
|---|---|
| `A_S_state_{held,other,used}_{lit,dark}_1x`, same for `A_L`, `Achip`, `B`, `C` | 30 |
| `Achip_held_<bg>_{1x,2x}` (4 backgrounds) | 8 |
| `Achip_fullframe_<bg>.jpg` | 4 |
| `Achip_zone_<shape>_<colour>_{lit,dark}_{1x,2x}` | 36 |
| `B_zone_<shape>_<colour>_{lit,dark}_1x` | 18 |
| `C_zone_<plate>_{lit,dark}_1x` | 8 |
| `label_courier_*`, `label_plex_*` (lit, dark, 1x, 2x) | 8 |
| `cmp_target_wallpaper_1x` (`composites_live.py`, not a twin) | 1 |

Per file: column `changed` in `03_variants.tsv` and the table in §7.

**Unchanged (75):** all `*_held_*` of A_S, A_L, B and C (Bayon-only labels), every `*_state_missing_*`, `label_bayon_*`, `label_level_*`, `label_code_*`, the A_S, A_L, B and C full frames (re-rendered byte-identical), `live_code_*`, `cmp_live_*`, `cmp_dropin_*`.

**Added:**
- `design/png/<name>@3x.png` × 33 (§5);
- `design/dropin/slot_40x22/HUD_KeyGlyph@3x.png` (120 × 66) and `design/dropin/slot_49x22/HUD_KeyGlyph@3x.png` (147 × 66);
- `design/dropin/crosshair/HUD_Crosshair@3x.png` (192 × 192) and `HUD_Crosshair.svg`;
- `design/bg/dark_59_door_key_open_t0000ms.png` (the dark background, 1920 × 1080);
- `images/09_integer_type_sizes.png`, `images/10_3x_exports.png` (verification);
- `03_variants.tsv` (this file's §7 as a table).

**Re-rendered (not part of their sync):** boards `design/01_overview.png` … `10_recommendation.png` (the numeral span is now 1.25 em of the 20 px label = 25 px; board 04's text names the whole sizes).

**Removed:** none.

### (c) SVG masters

**None changed.** `design.py` was re-run for the @3x PNGs; all 33 SVGs and all 66 @1x/@2x PNGs came out byte-identical (`diff -rq` against the 22:16 snapshot). `design_meta.json` is identical too.
One master issue is open for the fix stage (§10.1). If it is fixed, the A and C missing masters change and the KV-LIB variants need re-importing.

---

---

## 4. The background frames

| Key | File | Size | What |
|---|---|---|---|
| `lit` | `Documentation/research/room_visuals/images/room_map-l0-low_wide.jpg` | 1920 × 1080 | Level 0 low rooms, lit (worst contrast for paper) |
| `wallpaper` | `Documentation/research/room_visuals/images/room_map-l0-standard_wide.jpg` | 1920 × 1080 | Level 0 standard rooms, wallpaper and door |
| `office` | `Documentation/research/room_visuals/images/room_map-office_wide.jpg` | 1920 × 1080 | Office zone |
| `dark` | `design/bg/dark_59_door_key_open_t0000ms.png` | 1920 × 1080 | in-engine audit frame at a key door, dark cell |
| **`leaf`** (new) | `design/bg/leaf_48_almond.png` | 1920 × 1080 | in-engine audit frame 48 (`interaction_audit/images/48_door_locked_before_hud.png`, a locked door) with the leaf quad re-coloured to the spec's almond enamel `#CDC5B0` at full albedo (the brightest locked leaf the spec allows), gentle shading kept. Made by `make_leaf_bg.py` |
| `leafwood` (measured only) | `design/bg/leaf_48_wood.png` | 1920 × 1080 | the same frame, today's wood leaf, with the baked old HUD painted out |

Their Figma copies are in KV-SRC `2532:4039` (`figma_ids.json`); `leaf` is new there.

2x crops: the HUD region (x 0–640, y 840–1080) is upsampled ×2 bilinear before the 2x sprites and the 2x type go on it (`frame(name, 2)`).

---

## 5. @3x and touch exports

Phones are @3x. TestFlight (`279c144`) puts the key under the zone block at (78, 72) pt in a 150 × 22 pt panel, with a **40 × 22 pt** glyph rect, and the crosshair in a **24 pt** rect (dot 12 pt). The game loads one sprite per element on every platform, so the @3x files below can only load under their own names (the `_Touch` files).

| File | Size | Use |
|---|---|---|
| `design/png/<name>@3x.png` × 33 | 3 × the 1x size | every master, every state (vector source: `design/svg/`) |
| `design/dropin/slot_40x22/HUD_KeyGlyph@3x.png`, `…/HUD_KeyGlyph_Touch.png` | 120 × 66 | = `A_S_held@3x`; the `_Touch` copy is the name to load |
| `design/dropin/slot_49x22/HUD_KeyGlyph@3x.png`, `…/HUD_KeyGlyph_Touch.png` | 147 × 66 | = `A_L_held@3x` (the target) |
| `design/dropin/prompt/HUD_KeyOutline_Touch.png` | 153 × 72 | = `A_L_missing@3x` (51 × 24 pt rect) |
| `design/dropin/crosshair/HUD_Crosshair_Touch.png` | 144 × 144, dot 72 | **for the shipped 24 pt rect**: 2 : 1 like the desktop sprite, dot 12 pt |
| `design/dropin/crosshair/HUD_Crosshair@3x.png` (+ `.svg`) | 192 × 192, dot 96 | for a 32 pt rect only. Correction: the 2026-10-03 note said the phone rect was 32 pt and the dot 16 pt; the shipped rect is 24 pt, where this file would be shrunk 2.67 : 1 with no mipmaps. Use `_Touch` |

The desktop `Assets/Resources/UI/HUD_Crosshair.png` (64 × 64, Ø 32) is unchanged.

Checks (`$W/keyicon_figma/export_3x_checks.json`, `export_touch_checks.json`):
- **36 / 36 @3x files and 5 / 5 `_Touch` files pass:** corners alpha 0; one-colour files hold one RGB value in every pixel.
- **@3x box-filtered to 1x vs @1x:** mean error per file 0.004–0.185 % (median 0.06 %, was 0.03 % before the ring wire and the outline boxes); worst single pixel ≤ 5.05 % (on the chip's ring wire), the B lockups 9.5 % on one ring pixel.
- **Touch crosshair:** area diameter 72.00 px.
- Import (when it lands): Sprite (2D and UI), sRGB, Alpha Is Transparency, Bilinear, no mipmaps, Compression None, Max Size 256.

---

## 6. Boards and drop-in

- Boards 01–10 (`design/*.png`) were re-rendered on 2026-10-07: board 01 and 10 wording (the crosshair dot stays white), 03 / 07 / 09 state rows ("IF KEYS BECOME SINGLE-USE", the carded prompt), 05 (the ring wire), 07 (yellow rule = your decision), 10 (hero = the target A_L + KEY 14 with a 1 : 1 inset; the file list and the meta label moved into the docs). Not part of the sync.
- Drop-in: `slot_40x22/HUD_KeyGlyph.png` is still byte-identical to `png/A_S_held@2x.png`, `slot_49x22` to `A_L_held@2x.png`. New: the `_Touch`, `prompt/` files (§5). Nothing was copied into `Assets/`.

---

## 7. The variant list: one row per rendered file

Columns: direction × state × zone × label × background × scale, the glyph sprite (with its opacity when not 100 %), the text set (face, size, opacity when not 100 %), and the crop box in px **in that scale's frame** (x, y, w × h). "Changed" = pixels differ from the 2026-10-03 set (**new** = no Oct 3 file). The same rows, with the crop also in 1x units and an `ops_changed` column, are in `03_variants.tsv`. Rows 1–198 are `composites.py` (what `plan.py` records, in its order); then `composites_live.py` and the phone frames (not twins).

| # | File | Direction | State | Zone | Glyph | Label (face size) | Bg | Scale | Crop x, y, w × h (px) | Changed |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `A_S_held_lit_1x.png` | A · 40×22 | held | one glyph, all zones | `A_S_held` | LEVEL 0 KEY [Bayon 20] | lit | 1x | 40, 880, 600 × 120 | no |
| 2 | `A_S_held_lit_2x.png` | A · 40×22 | held | one glyph, all zones | `A_S_held` | LEVEL 0 KEY [Bayon 20] | lit | 2x | 80, 1760, 1200 × 240 | no |
| 3 | `A_S_held_wallpaper_1x.png` | A · 40×22 | held | one glyph, all zones | `A_S_held` | LEVEL 0 KEY [Bayon 20] | wallpaper | 1x | 40, 880, 600 × 120 | no |
| 4 | `A_S_held_wallpaper_2x.png` | A · 40×22 | held | one glyph, all zones | `A_S_held` | LEVEL 0 KEY [Bayon 20] | wallpaper | 2x | 80, 1760, 1200 × 240 | no |
| 5 | `A_S_held_office_1x.png` | A · 40×22 | held | one glyph, all zones | `A_S_held` | LEVEL 0 KEY [Bayon 20] | office | 1x | 40, 880, 600 × 120 | no |
| 6 | `A_S_held_office_2x.png` | A · 40×22 | held | one glyph, all zones | `A_S_held` | LEVEL 0 KEY [Bayon 20] | office | 2x | 80, 1760, 1200 × 240 | no |
| 7 | `A_S_held_dark_1x.png` | A · 40×22 | held | one glyph, all zones | `A_S_held` | LEVEL 0 KEY [Bayon 20] | dark | 1x | 40, 880, 600 × 120 | no |
| 8 | `A_S_held_dark_2x.png` | A · 40×22 | held | one glyph, all zones | `A_S_held` | LEVEL 0 KEY [Bayon 20] | dark | 2x | 80, 1760, 1200 × 240 | no |
| 9 | `A_L_held_lit_1x.png` | A · 49×22 | held | one glyph, all zones | `A_L_held` | LEVEL 0 KEY [Bayon 20] | lit | 1x | 40, 880, 600 × 120 | no |
| 10 | `A_L_held_lit_2x.png` | A · 49×22 | held | one glyph, all zones | `A_L_held` | LEVEL 0 KEY [Bayon 20] | lit | 2x | 80, 1760, 1200 × 240 | no |
| 11 | `A_L_held_wallpaper_1x.png` | A · 49×22 | held | one glyph, all zones | `A_L_held` | LEVEL 0 KEY [Bayon 20] | wallpaper | 1x | 40, 880, 600 × 120 | no |
| 12 | `A_L_held_wallpaper_2x.png` | A · 49×22 | held | one glyph, all zones | `A_L_held` | LEVEL 0 KEY [Bayon 20] | wallpaper | 2x | 80, 1760, 1200 × 240 | no |
| 13 | `A_L_held_office_1x.png` | A · 49×22 | held | one glyph, all zones | `A_L_held` | LEVEL 0 KEY [Bayon 20] | office | 1x | 40, 880, 600 × 120 | no |
| 14 | `A_L_held_office_2x.png` | A · 49×22 | held | one glyph, all zones | `A_L_held` | LEVEL 0 KEY [Bayon 20] | office | 2x | 80, 1760, 1200 × 240 | no |
| 15 | `A_L_held_dark_1x.png` | A · 49×22 | held | one glyph, all zones | `A_L_held` | LEVEL 0 KEY [Bayon 20] | dark | 1x | 40, 880, 600 × 120 | no |
| 16 | `A_L_held_dark_2x.png` | A · 49×22 | held | one glyph, all zones | `A_L_held` | LEVEL 0 KEY [Bayon 20] | dark | 2x | 80, 1760, 1200 × 240 | no |
| 17 | `C_held_lit_1x.png` | C · plate | held | dark plate | `C_dark_held` | LEVEL 0 KEY [Bayon 20] | lit | 1x | 40, 880, 600 × 120 | no |
| 18 | `C_held_lit_2x.png` | C · plate | held | dark plate | `C_dark_held` | LEVEL 0 KEY [Bayon 20] | lit | 2x | 80, 1760, 1200 × 240 | no |
| 19 | `C_held_wallpaper_1x.png` | C · plate | held | dark plate | `C_dark_held` | LEVEL 0 KEY [Bayon 20] | wallpaper | 1x | 40, 880, 600 × 120 | no |
| 20 | `C_held_wallpaper_2x.png` | C · plate | held | dark plate | `C_dark_held` | LEVEL 0 KEY [Bayon 20] | wallpaper | 2x | 80, 1760, 1200 × 240 | no |
| 21 | `C_held_office_1x.png` | C · plate | held | dark plate | `C_dark_held` | LEVEL 0 KEY [Bayon 20] | office | 1x | 40, 880, 600 × 120 | no |
| 22 | `C_held_office_2x.png` | C · plate | held | dark plate | `C_dark_held` | LEVEL 0 KEY [Bayon 20] | office | 2x | 80, 1760, 1200 × 240 | no |
| 23 | `C_held_dark_1x.png` | C · plate | held | dark plate | `C_dark_held` | LEVEL 0 KEY [Bayon 20] | dark | 1x | 40, 880, 600 × 120 | no |
| 24 | `C_held_dark_2x.png` | C · plate | held | dark plate | `C_dark_held` | LEVEL 0 KEY [Bayon 20] | dark | 2x | 80, 1760, 1200 × 240 | no |
| 25 | `B_held_lit_1x.png` | B · key on tag | held | rect tag · red · 14 | `B_rect_red_held` | LEVEL 0 KEY [Bayon 20] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | lit | 1x | 40, 860, 600 × 120 | no |
| 26 | `B_held_lit_2x.png` | B · key on tag | held | rect tag · red · 14 | `B_rect_red_held` | LEVEL 0 KEY [Bayon 20] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | lit | 2x | 80, 1720, 1200 × 240 | no |
| 27 | `B_held_wallpaper_1x.png` | B · key on tag | held | rect tag · red · 14 | `B_rect_red_held` | LEVEL 0 KEY [Bayon 20] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | wallpaper | 1x | 40, 860, 600 × 120 | no |
| 28 | `B_held_wallpaper_2x.png` | B · key on tag | held | rect tag · red · 14 | `B_rect_red_held` | LEVEL 0 KEY [Bayon 20] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | wallpaper | 2x | 80, 1720, 1200 × 240 | no |
| 29 | `B_held_office_1x.png` | B · key on tag | held | rect tag · red · 14 | `B_rect_red_held` | LEVEL 0 KEY [Bayon 20] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | office | 1x | 40, 860, 600 × 120 | no |
| 30 | `B_held_office_2x.png` | B · key on tag | held | rect tag · red · 14 | `B_rect_red_held` | LEVEL 0 KEY [Bayon 20] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | office | 2x | 80, 1720, 1200 × 240 | no |
| 31 | `B_held_dark_1x.png` | B · key on tag | held | rect tag · red · 14 | `B_rect_red_held` | LEVEL 0 KEY [Bayon 20] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | dark | 1x | 40, 860, 600 × 120 | no |
| 32 | `B_held_dark_2x.png` | B · key on tag | held | rect tag · red · 14 | `B_rect_red_held` | LEVEL 0 KEY [Bayon 20] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | dark | 2x | 80, 1720, 1200 × 240 | no |
| 33 | `Achip_held_lit_1x.png` | A + chip | held | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 1x | 40, 880, 600 × 120 | **yes** |
| 34 | `Achip_held_lit_2x.png` | A + chip | held | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 2x | 80, 1760, 1200 × 240 | **yes** |
| 35 | `Achip_held_wallpaper_1x.png` | A + chip | held | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | wallpaper | 1x | 40, 880, 600 × 120 | **yes** |
| 36 | `Achip_held_wallpaper_2x.png` | A + chip | held | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | wallpaper | 2x | 80, 1760, 1200 × 240 | **yes** |
| 37 | `Achip_held_office_1x.png` | A + chip | held | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | office | 1x | 40, 880, 600 × 120 | **yes** |
| 38 | `Achip_held_office_2x.png` | A + chip | held | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | office | 2x | 80, 1760, 1200 × 240 | **yes** |
| 39 | `Achip_held_dark_1x.png` | A + chip | held | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 1x | 40, 880, 600 × 120 | **yes** |
| 40 | `Achip_held_dark_2x.png` | A + chip | held | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 2x | 80, 1760, 1200 × 240 | **yes** |
| 41 | `A_S_state_held_lit_1x.png` | A · 40×22 | held | one glyph, all zones | `A_S_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 1x | 40, 880, 600 × 120 | no |
| 42 | `A_S_state_other_lit_1x.png` | A · 40×22 | other | one glyph, all zones | `A_S_missing` | KEY [Bayon 20] 14 [Courier Prime Bold 25] NOT THIS ZONE [Plex Mono 13] | lit | 1x | 40, 880, 600 × 120 | **yes** |
| 43 | `A_S_state_missing_lit_1x.png` | A · 40×22 | missing | one glyph, all zones | `A_S_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] + 4 px rule | lit | 1x | 720, 512, 480 × 120 | **yes** |
| 44 | `A_S_state_used_lit_1x.png` | A · 40×22 | used | one glyph, all zones | `A_S_held @70%` | KEY [Bayon 20] 14 [Courier Prime Bold 25] DOOR OPENED [Plex Mono 13] | lit | 1x | 40, 880, 600 × 120 | **yes** |
| 45 | `A_S_state_held_dark_1x.png` | A · 40×22 | held | one glyph, all zones | `A_S_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 1x | 40, 880, 600 × 120 | no |
| 46 | `A_S_state_other_dark_1x.png` | A · 40×22 | other | one glyph, all zones | `A_S_missing` | KEY [Bayon 20] 14 [Courier Prime Bold 25] NOT THIS ZONE [Plex Mono 13] | dark | 1x | 40, 880, 600 × 120 | **yes** |
| 47 | `A_S_state_missing_dark_1x.png` | A · 40×22 | missing | one glyph, all zones | `A_S_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] + 4 px rule | dark | 1x | 720, 512, 480 × 120 | **yes** |
| 48 | `A_S_state_used_dark_1x.png` | A · 40×22 | used | one glyph, all zones | `A_S_held @70%` | KEY [Bayon 20] 14 [Courier Prime Bold 25] DOOR OPENED [Plex Mono 13] | dark | 1x | 40, 880, 600 × 120 | **yes** |
| 49 | `A_S_state_missing_wallpaper_1x.png` | A · 40×22 | missing | one glyph, all zones | `A_S_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] + 4 px rule | wallpaper | 1x | 720, 512, 480 × 120 | **new** |
| 50 | `A_S_state_missing_office_1x.png` | A · 40×22 | missing | one glyph, all zones | `A_S_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] + 4 px rule | office | 1x | 720, 512, 480 × 120 | **new** |
| 51 | `A_S_state_missing_leaf_1x.png` | A · 40×22 | missing | one glyph, all zones | `A_S_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] + 4 px rule | leaf | 1x | 720, 512, 480 × 120 | **new** |
| 52 | `A_L_state_held_lit_1x.png` | A · 49×22 | held | one glyph, all zones | `A_L_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 1x | 40, 880, 600 × 120 | no |
| 53 | `A_L_state_other_lit_1x.png` | A · 49×22 | other | one glyph, all zones | `A_L_missing` | KEY [Bayon 20] 14 [Courier Prime Bold 25] NOT THIS ZONE [Plex Mono 13] | lit | 1x | 40, 880, 600 × 120 | **yes** |
| 54 | `A_L_state_missing_lit_1x.png` | A · 49×22 | missing | one glyph, all zones | `A_L_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] + 4 px rule | lit | 1x | 720, 512, 480 × 120 | **yes** |
| 55 | `A_L_state_used_lit_1x.png` | A · 49×22 | used | one glyph, all zones | `A_L_held @70%` | KEY [Bayon 20] 14 [Courier Prime Bold 25] DOOR OPENED [Plex Mono 13] | lit | 1x | 40, 880, 600 × 120 | **yes** |
| 56 | `A_L_state_held_dark_1x.png` | A · 49×22 | held | one glyph, all zones | `A_L_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 1x | 40, 880, 600 × 120 | no |
| 57 | `A_L_state_other_dark_1x.png` | A · 49×22 | other | one glyph, all zones | `A_L_missing` | KEY [Bayon 20] 14 [Courier Prime Bold 25] NOT THIS ZONE [Plex Mono 13] | dark | 1x | 40, 880, 600 × 120 | **yes** |
| 58 | `A_L_state_missing_dark_1x.png` | A · 49×22 | missing | one glyph, all zones | `A_L_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] + 4 px rule | dark | 1x | 720, 512, 480 × 120 | **yes** |
| 59 | `A_L_state_used_dark_1x.png` | A · 49×22 | used | one glyph, all zones | `A_L_held @70%` | KEY [Bayon 20] 14 [Courier Prime Bold 25] DOOR OPENED [Plex Mono 13] | dark | 1x | 40, 880, 600 × 120 | **yes** |
| 60 | `A_L_state_missing_wallpaper_1x.png` | A · 49×22 | missing | one glyph, all zones | `A_L_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] + 4 px rule | wallpaper | 1x | 720, 512, 480 × 120 | **new** |
| 61 | `A_L_state_missing_office_1x.png` | A · 49×22 | missing | one glyph, all zones | `A_L_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] + 4 px rule | office | 1x | 720, 512, 480 × 120 | **new** |
| 62 | `A_L_state_missing_leaf_1x.png` | A · 49×22 | missing | one glyph, all zones | `A_L_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] + 4 px rule | leaf | 1x | 720, 512, 480 × 120 | **new** |
| 63 | `C_state_held_lit_1x.png` | C · plate | held | dark plate | `C_dark_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 1x | 40, 880, 600 × 120 | no |
| 64 | `C_state_other_lit_1x.png` | C · plate | other | dark plate | `C_dark_missing` | KEY [Bayon 20] 14 [Courier Prime Bold 25] NOT THIS ZONE [Plex Mono 13] | lit | 1x | 40, 880, 600 × 120 | **yes** |
| 65 | `C_state_missing_lit_1x.png` | C · plate | missing | dark plate | `C_dark_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] + 4 px rule | lit | 1x | 720, 512, 480 × 120 | **yes** |
| 66 | `C_state_used_lit_1x.png` | C · plate | used | dark plate | `C_dark_held @70%` | KEY [Bayon 20] 14 [Courier Prime Bold 25] DOOR OPENED [Plex Mono 13] | lit | 1x | 40, 880, 600 × 120 | **yes** |
| 67 | `C_state_held_dark_1x.png` | C · plate | held | dark plate | `C_dark_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 1x | 40, 880, 600 × 120 | no |
| 68 | `C_state_other_dark_1x.png` | C · plate | other | dark plate | `C_dark_missing` | KEY [Bayon 20] 14 [Courier Prime Bold 25] NOT THIS ZONE [Plex Mono 13] | dark | 1x | 40, 880, 600 × 120 | **yes** |
| 69 | `C_state_missing_dark_1x.png` | C · plate | missing | dark plate | `C_dark_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] + 4 px rule | dark | 1x | 720, 512, 480 × 120 | **yes** |
| 70 | `C_state_used_dark_1x.png` | C · plate | used | dark plate | `C_dark_held @70%` | KEY [Bayon 20] 14 [Courier Prime Bold 25] DOOR OPENED [Plex Mono 13] | dark | 1x | 40, 880, 600 × 120 | **yes** |
| 71 | `C_state_missing_wallpaper_1x.png` | C · plate | missing | dark plate | `C_dark_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] + 4 px rule | wallpaper | 1x | 720, 512, 480 × 120 | **new** |
| 72 | `C_state_missing_office_1x.png` | C · plate | missing | dark plate | `C_dark_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] + 4 px rule | office | 1x | 720, 512, 480 × 120 | **new** |
| 73 | `C_state_missing_leaf_1x.png` | C · plate | missing | dark plate | `C_dark_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] + 4 px rule | leaf | 1x | 720, 512, 480 × 120 | **new** |
| 74 | `B_state_held_lit_1x.png` | B · key on tag | held | rect tag · red · 14 | `B_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | lit | 1x | 40, 860, 600 × 120 | no |
| 75 | `B_state_other_lit_1x.png` | B · key on tag | other | rect tag · red · 14 | `B_rect_red_missing` | KEY [Bayon 20] 14 [Courier Prime Bold 25] OPENS ZONE 14 ONLY [Plex Mono 13] | lit | 1x | 40, 860, 600 × 120 | **yes** |
| 76 | `B_state_missing_lit_1x.png` | B · key on tag | missing | one glyph, all zones | `A_L_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] + 4 px rule | lit | 1x | 720, 512, 480 × 120 | **yes** |
| 77 | `B_state_used_lit_1x.png` | B · key on tag | used | rect tag · red · 14 | `B_rect_red_held @70%` | KEY [Bayon 20] 14 [Courier Prime Bold 25] DOOR OPENED [Plex Mono 13] | lit | 1x | 40, 860, 600 × 120 | **yes** |
| 78 | `B_state_held_dark_1x.png` | B · key on tag | held | rect tag · red · 14 | `B_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | dark | 1x | 40, 860, 600 × 120 | no |
| 79 | `B_state_other_dark_1x.png` | B · key on tag | other | rect tag · red · 14 | `B_rect_red_missing` | KEY [Bayon 20] 14 [Courier Prime Bold 25] OPENS ZONE 14 ONLY [Plex Mono 13] | dark | 1x | 40, 860, 600 × 120 | **yes** |
| 80 | `B_state_missing_dark_1x.png` | B · key on tag | missing | one glyph, all zones | `A_L_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] + 4 px rule | dark | 1x | 720, 512, 480 × 120 | **yes** |
| 81 | `B_state_used_dark_1x.png` | B · key on tag | used | rect tag · red · 14 | `B_rect_red_held @70%` | KEY [Bayon 20] 14 [Courier Prime Bold 25] DOOR OPENED [Plex Mono 13] | dark | 1x | 40, 860, 600 × 120 | **yes** |
| 82 | `B_state_missing_wallpaper_1x.png` | B · key on tag | missing | one glyph, all zones | `A_L_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] + 4 px rule | wallpaper | 1x | 720, 512, 480 × 120 | **new** |
| 83 | `B_state_missing_office_1x.png` | B · key on tag | missing | one glyph, all zones | `A_L_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] + 4 px rule | office | 1x | 720, 512, 480 × 120 | **new** |
| 84 | `B_state_missing_leaf_1x.png` | B · key on tag | missing | one glyph, all zones | `A_L_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] + 4 px rule | leaf | 1x | 720, 512, 480 × 120 | **new** |
| 85 | `Achip_state_held_lit_1x.png` | A + chip | held | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 1x | 40, 880, 600 × 120 | **yes** |
| 86 | `Achip_state_other_lit_1x.png` | A + chip | other | rect tag · red | `Achip_rect_red_held @70%` | KEY [Bayon 20] 14 [Courier Prime Bold 25] NOT THIS ZONE [Plex Mono 13] | lit | 1x | 40, 880, 600 × 120 | **yes** |
| 87 | `Achip_state_missing_lit_1x.png` | A + chip | missing | one glyph, all zones | `A_L_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] + 4 px rule | lit | 1x | 720, 512, 480 × 120 | **yes** |
| 88 | `Achip_state_used_lit_1x.png` | A + chip | used | rect tag · red | `Achip_rect_red_held @70%` | KEY [Bayon 20] 14 [Courier Prime Bold 25] DOOR OPENED [Plex Mono 13] | lit | 1x | 40, 880, 600 × 120 | **yes** |
| 89 | `Achip_state_held_dark_1x.png` | A + chip | held | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 1x | 40, 880, 600 × 120 | **yes** |
| 90 | `Achip_state_other_dark_1x.png` | A + chip | other | rect tag · red | `Achip_rect_red_held @70%` | KEY [Bayon 20] 14 [Courier Prime Bold 25] NOT THIS ZONE [Plex Mono 13] | dark | 1x | 40, 880, 600 × 120 | **yes** |
| 91 | `Achip_state_missing_dark_1x.png` | A + chip | missing | one glyph, all zones | `A_L_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] + 4 px rule | dark | 1x | 720, 512, 480 × 120 | **yes** |
| 92 | `Achip_state_used_dark_1x.png` | A + chip | used | rect tag · red | `Achip_rect_red_held @70%` | KEY [Bayon 20] 14 [Courier Prime Bold 25] DOOR OPENED [Plex Mono 13] | dark | 1x | 40, 880, 600 × 120 | **yes** |
| 93 | `Achip_state_missing_wallpaper_1x.png` | A + chip | missing | one glyph, all zones | `A_L_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] + 4 px rule | wallpaper | 1x | 720, 512, 480 × 120 | **new** |
| 94 | `Achip_state_missing_office_1x.png` | A + chip | missing | one glyph, all zones | `A_L_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] + 4 px rule | office | 1x | 720, 512, 480 × 120 | **new** |
| 95 | `Achip_state_missing_leaf_1x.png` | A + chip | missing | one glyph, all zones | `A_L_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] + 4 px rule | leaf | 1x | 720, 512, 480 × 120 | **new** |
| 96 | `label_bayon_lit_1x.png` | A · 49×22 | held · label bayon | one glyph, all zones | `A_L_held` | KEY 14 [Bayon 20] | lit | 1x | 40, 920, 400 × 60 | no |
| 97 | `label_bayon_lit_2x.png` | A · 49×22 | held · label bayon | one glyph, all zones | `A_L_held` | KEY 14 [Bayon 20] | lit | 2x | 80, 1840, 800 × 120 | no |
| 98 | `label_bayon_dark_1x.png` | A · 49×22 | held · label bayon | one glyph, all zones | `A_L_held` | KEY 14 [Bayon 20] | dark | 1x | 40, 920, 400 × 60 | no |
| 99 | `label_bayon_dark_2x.png` | A · 49×22 | held · label bayon | one glyph, all zones | `A_L_held` | KEY 14 [Bayon 20] | dark | 2x | 80, 1840, 800 × 120 | no |
| 100 | `label_plex_lit_1x.png` | A · 49×22 | held · label plex | one glyph, all zones | `A_L_held` | KEY [Bayon 20] 14 [Plex Mono 20] | lit | 1x | 40, 920, 400 × 60 | no |
| 101 | `label_plex_lit_2x.png` | A · 49×22 | held · label plex | one glyph, all zones | `A_L_held` | KEY [Bayon 20] 14 [Plex Mono 20] | lit | 2x | 80, 1840, 800 × 120 | no |
| 102 | `label_plex_dark_1x.png` | A · 49×22 | held · label plex | one glyph, all zones | `A_L_held` | KEY [Bayon 20] 14 [Plex Mono 20] | dark | 1x | 40, 920, 400 × 60 | no |
| 103 | `label_plex_dark_2x.png` | A · 49×22 | held · label plex | one glyph, all zones | `A_L_held` | KEY [Bayon 20] 14 [Plex Mono 20] | dark | 2x | 80, 1840, 800 × 120 | no |
| 104 | `label_courier_lit_1x.png` | A · 49×22 | held · label courier | one glyph, all zones | `A_L_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 1x | 40, 920, 400 × 60 | no |
| 105 | `label_courier_lit_2x.png` | A · 49×22 | held · label courier | one glyph, all zones | `A_L_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 2x | 80, 1840, 800 × 120 | no |
| 106 | `label_courier_dark_1x.png` | A · 49×22 | held · label courier | one glyph, all zones | `A_L_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 1x | 40, 920, 400 × 60 | no |
| 107 | `label_courier_dark_2x.png` | A · 49×22 | held · label courier | one glyph, all zones | `A_L_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 2x | 80, 1840, 800 × 120 | no |
| 108 | `label_level_lit_1x.png` | A · 49×22 | held · label level | one glyph, all zones | `A_L_held` | LEVEL 0 KEY [Bayon 20] | lit | 1x | 40, 920, 400 × 60 | no |
| 109 | `label_level_lit_2x.png` | A · 49×22 | held · label level | one glyph, all zones | `A_L_held` | LEVEL 0 KEY [Bayon 20] | lit | 2x | 80, 1840, 800 × 120 | no |
| 110 | `label_level_dark_1x.png` | A · 49×22 | held · label level | one glyph, all zones | `A_L_held` | LEVEL 0 KEY [Bayon 20] | dark | 1x | 40, 920, 400 × 60 | no |
| 111 | `label_level_dark_2x.png` | A · 49×22 | held · label level | one glyph, all zones | `A_L_held` | LEVEL 0 KEY [Bayon 20] | dark | 2x | 80, 1840, 800 × 120 | no |
| 112 | `label_code_lit_1x.png` | A · 49×22 | held · label code | one glyph, all zones | `A_L_held` | LEVEL 0 / THE MAZE KEY [Bayon 20] | lit | 1x | 40, 920, 400 × 60 | no |
| 113 | `label_code_lit_2x.png` | A · 49×22 | held · label code | one glyph, all zones | `A_L_held` | LEVEL 0 / THE MAZE KEY [Bayon 20] | lit | 2x | 80, 1840, 800 × 120 | no |
| 114 | `label_code_dark_1x.png` | A · 49×22 | held · label code | one glyph, all zones | `A_L_held` | LEVEL 0 / THE MAZE KEY [Bayon 20] | dark | 1x | 40, 920, 400 × 60 | no |
| 115 | `label_code_dark_2x.png` | A · 49×22 | held · label code | one glyph, all zones | `A_L_held` | LEVEL 0 / THE MAZE KEY [Bayon 20] | dark | 2x | 80, 1840, 800 × 120 | no |
| 116 | `B_zone_rect_red_lit_1x.png` | B · key on tag | held · zone | rect tag · red · 14 | `B_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | lit | 1x | 40, 860, 600 × 120 | no |
| 117 | `B_zone_rect_blue_lit_1x.png` | B · key on tag | held · zone | rect tag · blue · 14 | `B_rect_blue_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | lit | 1x | 40, 860, 600 × 120 | no |
| 118 | `B_zone_rect_white_lit_1x.png` | B · key on tag | held · zone | rect tag · white · 14 | `B_rect_white_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | lit | 1x | 40, 860, 600 × 120 | no |
| 119 | `B_zone_round_red_lit_1x.png` | B · key on tag | held · zone | round tag · red · 37 | `B_round_red_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | lit | 1x | 40, 860, 600 × 120 | no |
| 120 | `B_zone_round_blue_lit_1x.png` | B · key on tag | held · zone | round tag · blue · 37 | `B_round_blue_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | lit | 1x | 40, 860, 600 × 120 | no |
| 121 | `B_zone_round_white_lit_1x.png` | B · key on tag | held · zone | round tag · white · 37 | `B_round_white_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | lit | 1x | 40, 860, 600 × 120 | no |
| 122 | `B_zone_long_red_lit_1x.png` | B · key on tag | held · zone | long tag · red · 08 | `B_long_red_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | lit | 1x | 40, 860, 600 × 120 | no |
| 123 | `B_zone_long_blue_lit_1x.png` | B · key on tag | held · zone | long tag · blue · 08 | `B_long_blue_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | lit | 1x | 40, 860, 600 × 120 | no |
| 124 | `B_zone_long_white_lit_1x.png` | B · key on tag | held · zone | long tag · white · 08 | `B_long_white_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | lit | 1x | 40, 860, 600 × 120 | no |
| 125 | `C_zone_dark_lit_1x.png` | C · plate | held · zone | dark plate | `C_dark_held` | KEY [Bayon 20] 00 [Plex Mono 20] | lit | 1x | 40, 920, 400 × 60 | no |
| 126 | `C_zone_red_lit_1x.png` | C · plate | held · zone | red plate | `C_red_held` | KEY [Bayon 20] 14 [Plex Mono 20] | lit | 1x | 40, 920, 400 × 60 | no |
| 127 | `C_zone_blue_lit_1x.png` | C · plate | held · zone | blue plate | `C_blue_held` | KEY [Bayon 20] 37 [Plex Mono 20] | lit | 1x | 40, 920, 400 × 60 | no |
| 128 | `C_zone_white_lit_1x.png` | C · plate | held · zone | white plate | `C_white_held` | KEY [Bayon 20] 08 [Plex Mono 20] | lit | 1x | 40, 920, 400 × 60 | no |
| 129 | `B_zone_rect_red_dark_1x.png` | B · key on tag | held · zone | rect tag · red · 14 | `B_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | dark | 1x | 40, 860, 600 × 120 | no |
| 130 | `B_zone_rect_blue_dark_1x.png` | B · key on tag | held · zone | rect tag · blue · 14 | `B_rect_blue_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | dark | 1x | 40, 860, 600 × 120 | no |
| 131 | `B_zone_rect_white_dark_1x.png` | B · key on tag | held · zone | rect tag · white · 14 | `B_rect_white_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | dark | 1x | 40, 860, 600 × 120 | no |
| 132 | `B_zone_round_red_dark_1x.png` | B · key on tag | held · zone | round tag · red · 37 | `B_round_red_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | dark | 1x | 40, 860, 600 × 120 | no |
| 133 | `B_zone_round_blue_dark_1x.png` | B · key on tag | held · zone | round tag · blue · 37 | `B_round_blue_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | dark | 1x | 40, 860, 600 × 120 | no |
| 134 | `B_zone_round_white_dark_1x.png` | B · key on tag | held · zone | round tag · white · 37 | `B_round_white_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | dark | 1x | 40, 860, 600 × 120 | no |
| 135 | `B_zone_long_red_dark_1x.png` | B · key on tag | held · zone | long tag · red · 08 | `B_long_red_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | dark | 1x | 40, 860, 600 × 120 | no |
| 136 | `B_zone_long_blue_dark_1x.png` | B · key on tag | held · zone | long tag · blue · 08 | `B_long_blue_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | dark | 1x | 40, 860, 600 × 120 | no |
| 137 | `B_zone_long_white_dark_1x.png` | B · key on tag | held · zone | long tag · white · 08 | `B_long_white_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | dark | 1x | 40, 860, 600 × 120 | no |
| 138 | `C_zone_dark_dark_1x.png` | C · plate | held · zone | dark plate | `C_dark_held` | KEY [Bayon 20] 00 [Plex Mono 20] | dark | 1x | 40, 920, 400 × 60 | no |
| 139 | `C_zone_red_dark_1x.png` | C · plate | held · zone | red plate | `C_red_held` | KEY [Bayon 20] 14 [Plex Mono 20] | dark | 1x | 40, 920, 400 × 60 | no |
| 140 | `C_zone_blue_dark_1x.png` | C · plate | held · zone | blue plate | `C_blue_held` | KEY [Bayon 20] 37 [Plex Mono 20] | dark | 1x | 40, 920, 400 × 60 | no |
| 141 | `C_zone_white_dark_1x.png` | C · plate | held · zone | white plate | `C_white_held` | KEY [Bayon 20] 08 [Plex Mono 20] | dark | 1x | 40, 920, 400 × 60 | no |
| 142 | `Achip_zone_rect_red_lit_1x.png` | A + chip | held · zone | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 1x | 40, 920, 400 × 60 | **yes** |
| 143 | `Achip_zone_rect_red_lit_2x.png` | A + chip | held · zone | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 2x | 80, 1840, 800 × 120 | **yes** |
| 144 | `Achip_zone_rect_blue_lit_1x.png` | A + chip | held · zone | rect tag · blue | `Achip_rect_blue_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 1x | 40, 920, 400 × 60 | **yes** |
| 145 | `Achip_zone_rect_blue_lit_2x.png` | A + chip | held · zone | rect tag · blue | `Achip_rect_blue_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 2x | 80, 1840, 800 × 120 | **yes** |
| 146 | `Achip_zone_rect_white_lit_1x.png` | A + chip | held · zone | rect tag · white | `Achip_rect_white_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 1x | 40, 920, 400 × 60 | **yes** |
| 147 | `Achip_zone_rect_white_lit_2x.png` | A + chip | held · zone | rect tag · white | `Achip_rect_white_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 2x | 80, 1840, 800 × 120 | **yes** |
| 148 | `Achip_zone_round_red_lit_1x.png` | A + chip | held · zone | round tag · red | `Achip_round_red_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] | lit | 1x | 40, 920, 400 × 60 | **yes** |
| 149 | `Achip_zone_round_red_lit_2x.png` | A + chip | held · zone | round tag · red | `Achip_round_red_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] | lit | 2x | 80, 1840, 800 × 120 | **yes** |
| 150 | `Achip_zone_round_blue_lit_1x.png` | A + chip | held · zone | round tag · blue | `Achip_round_blue_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] | lit | 1x | 40, 920, 400 × 60 | **yes** |
| 151 | `Achip_zone_round_blue_lit_2x.png` | A + chip | held · zone | round tag · blue | `Achip_round_blue_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] | lit | 2x | 80, 1840, 800 × 120 | **yes** |
| 152 | `Achip_zone_round_white_lit_1x.png` | A + chip | held · zone | round tag · white | `Achip_round_white_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] | lit | 1x | 40, 920, 400 × 60 | **yes** |
| 153 | `Achip_zone_round_white_lit_2x.png` | A + chip | held · zone | round tag · white | `Achip_round_white_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] | lit | 2x | 80, 1840, 800 × 120 | **yes** |
| 154 | `Achip_zone_long_red_lit_1x.png` | A + chip | held · zone | long tag · red | `Achip_long_red_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] | lit | 1x | 40, 920, 400 × 60 | **yes** |
| 155 | `Achip_zone_long_red_lit_2x.png` | A + chip | held · zone | long tag · red | `Achip_long_red_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] | lit | 2x | 80, 1840, 800 × 120 | **yes** |
| 156 | `Achip_zone_long_blue_lit_1x.png` | A + chip | held · zone | long tag · blue | `Achip_long_blue_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] | lit | 1x | 40, 920, 400 × 60 | **yes** |
| 157 | `Achip_zone_long_blue_lit_2x.png` | A + chip | held · zone | long tag · blue | `Achip_long_blue_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] | lit | 2x | 80, 1840, 800 × 120 | **yes** |
| 158 | `Achip_zone_long_white_lit_1x.png` | A + chip | held · zone | long tag · white | `Achip_long_white_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] | lit | 1x | 40, 920, 400 × 60 | **yes** |
| 159 | `Achip_zone_long_white_lit_2x.png` | A + chip | held · zone | long tag · white | `Achip_long_white_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] | lit | 2x | 80, 1840, 800 × 120 | **yes** |
| 160 | `Achip_zone_rect_red_dark_1x.png` | A + chip | held · zone | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 1x | 40, 920, 400 × 60 | **yes** |
| 161 | `Achip_zone_rect_red_dark_2x.png` | A + chip | held · zone | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 2x | 80, 1840, 800 × 120 | **yes** |
| 162 | `Achip_zone_rect_blue_dark_1x.png` | A + chip | held · zone | rect tag · blue | `Achip_rect_blue_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 1x | 40, 920, 400 × 60 | **yes** |
| 163 | `Achip_zone_rect_blue_dark_2x.png` | A + chip | held · zone | rect tag · blue | `Achip_rect_blue_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 2x | 80, 1840, 800 × 120 | **yes** |
| 164 | `Achip_zone_rect_white_dark_1x.png` | A + chip | held · zone | rect tag · white | `Achip_rect_white_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 1x | 40, 920, 400 × 60 | **yes** |
| 165 | `Achip_zone_rect_white_dark_2x.png` | A + chip | held · zone | rect tag · white | `Achip_rect_white_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 2x | 80, 1840, 800 × 120 | **yes** |
| 166 | `Achip_zone_round_red_dark_1x.png` | A + chip | held · zone | round tag · red | `Achip_round_red_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] | dark | 1x | 40, 920, 400 × 60 | **yes** |
| 167 | `Achip_zone_round_red_dark_2x.png` | A + chip | held · zone | round tag · red | `Achip_round_red_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] | dark | 2x | 80, 1840, 800 × 120 | **yes** |
| 168 | `Achip_zone_round_blue_dark_1x.png` | A + chip | held · zone | round tag · blue | `Achip_round_blue_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] | dark | 1x | 40, 920, 400 × 60 | **yes** |
| 169 | `Achip_zone_round_blue_dark_2x.png` | A + chip | held · zone | round tag · blue | `Achip_round_blue_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] | dark | 2x | 80, 1840, 800 × 120 | **yes** |
| 170 | `Achip_zone_round_white_dark_1x.png` | A + chip | held · zone | round tag · white | `Achip_round_white_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] | dark | 1x | 40, 920, 400 × 60 | **yes** |
| 171 | `Achip_zone_round_white_dark_2x.png` | A + chip | held · zone | round tag · white | `Achip_round_white_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] | dark | 2x | 80, 1840, 800 × 120 | **yes** |
| 172 | `Achip_zone_long_red_dark_1x.png` | A + chip | held · zone | long tag · red | `Achip_long_red_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] | dark | 1x | 40, 920, 400 × 60 | **yes** |
| 173 | `Achip_zone_long_red_dark_2x.png` | A + chip | held · zone | long tag · red | `Achip_long_red_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] | dark | 2x | 80, 1840, 800 × 120 | **yes** |
| 174 | `Achip_zone_long_blue_dark_1x.png` | A + chip | held · zone | long tag · blue | `Achip_long_blue_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] | dark | 1x | 40, 920, 400 × 60 | **yes** |
| 175 | `Achip_zone_long_blue_dark_2x.png` | A + chip | held · zone | long tag · blue | `Achip_long_blue_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] | dark | 2x | 80, 1840, 800 × 120 | **yes** |
| 176 | `Achip_zone_long_white_dark_1x.png` | A + chip | held · zone | long tag · white | `Achip_long_white_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] | dark | 1x | 40, 920, 400 × 60 | **yes** |
| 177 | `Achip_zone_long_white_dark_2x.png` | A + chip | held · zone | long tag · white | `Achip_long_white_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] | dark | 2x | 80, 1840, 800 × 120 | **yes** |
| 178 | `A_S_fullframe_lit.jpg` | A · 40×22 | held · full 1080p frame | one glyph, all zones | `A_S_held` | LEVEL 0 KEY [Bayon 20] | lit | 1x | 0, 0, 1920 × 1080 | no |
| 179 | `A_S_fullframe_wallpaper.jpg` | A · 40×22 | held · full 1080p frame | one glyph, all zones | `A_S_held` | LEVEL 0 KEY [Bayon 20] | wallpaper | 1x | 0, 0, 1920 × 1080 | no |
| 180 | `A_S_fullframe_office.jpg` | A · 40×22 | held · full 1080p frame | one glyph, all zones | `A_S_held` | LEVEL 0 KEY [Bayon 20] | office | 1x | 0, 0, 1920 × 1080 | no |
| 181 | `A_S_fullframe_dark.jpg` | A · 40×22 | held · full 1080p frame | one glyph, all zones | `A_S_held` | LEVEL 0 KEY [Bayon 20] | dark | 1x | 0, 0, 1920 × 1080 | no |
| 182 | `A_L_fullframe_lit.jpg` | A · 49×22 | held · full 1080p frame | one glyph, all zones | `A_L_held` | LEVEL 0 KEY [Bayon 20] | lit | 1x | 0, 0, 1920 × 1080 | no |
| 183 | `A_L_fullframe_wallpaper.jpg` | A · 49×22 | held · full 1080p frame | one glyph, all zones | `A_L_held` | LEVEL 0 KEY [Bayon 20] | wallpaper | 1x | 0, 0, 1920 × 1080 | no |
| 184 | `A_L_fullframe_office.jpg` | A · 49×22 | held · full 1080p frame | one glyph, all zones | `A_L_held` | LEVEL 0 KEY [Bayon 20] | office | 1x | 0, 0, 1920 × 1080 | no |
| 185 | `A_L_fullframe_dark.jpg` | A · 49×22 | held · full 1080p frame | one glyph, all zones | `A_L_held` | LEVEL 0 KEY [Bayon 20] | dark | 1x | 0, 0, 1920 × 1080 | no |
| 186 | `C_fullframe_lit.jpg` | C · plate | held · full 1080p frame | dark plate | `C_dark_held` | LEVEL 0 KEY [Bayon 20] | lit | 1x | 0, 0, 1920 × 1080 | no |
| 187 | `C_fullframe_wallpaper.jpg` | C · plate | held · full 1080p frame | dark plate | `C_dark_held` | LEVEL 0 KEY [Bayon 20] | wallpaper | 1x | 0, 0, 1920 × 1080 | no |
| 188 | `C_fullframe_office.jpg` | C · plate | held · full 1080p frame | dark plate | `C_dark_held` | LEVEL 0 KEY [Bayon 20] | office | 1x | 0, 0, 1920 × 1080 | no |
| 189 | `C_fullframe_dark.jpg` | C · plate | held · full 1080p frame | dark plate | `C_dark_held` | LEVEL 0 KEY [Bayon 20] | dark | 1x | 0, 0, 1920 × 1080 | no |
| 190 | `B_fullframe_lit.jpg` | B · key on tag | held · full 1080p frame | rect tag · red · 14 | `B_rect_red_held` | LEVEL 0 KEY [Bayon 20] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | lit | 1x | 0, 0, 1920 × 1080 | no |
| 191 | `B_fullframe_wallpaper.jpg` | B · key on tag | held · full 1080p frame | rect tag · red · 14 | `B_rect_red_held` | LEVEL 0 KEY [Bayon 20] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | wallpaper | 1x | 0, 0, 1920 × 1080 | no |
| 192 | `B_fullframe_office.jpg` | B · key on tag | held · full 1080p frame | rect tag · red · 14 | `B_rect_red_held` | LEVEL 0 KEY [Bayon 20] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | office | 1x | 0, 0, 1920 × 1080 | no |
| 193 | `B_fullframe_dark.jpg` | B · key on tag | held · full 1080p frame | rect tag · red · 14 | `B_rect_red_held` | LEVEL 0 KEY [Bayon 20] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | dark | 1x | 0, 0, 1920 × 1080 | no |
| 194 | `Achip_fullframe_lit.jpg` | A + chip | held · full 1080p frame | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 1x | 0, 0, 1920 × 1080 | **yes** |
| 195 | `Achip_fullframe_wallpaper.jpg` | A + chip | held · full 1080p frame | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | wallpaper | 1x | 0, 0, 1920 × 1080 | **yes** |
| 196 | `Achip_fullframe_office.jpg` | A + chip | held · full 1080p frame | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | office | 1x | 0, 0, 1920 × 1080 | **yes** |
| 197 | `Achip_fullframe_dark.jpg` | A + chip | held · full 1080p frame | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 1x | 0, 0, 1920 × 1080 | **yes** |
| 198 | `A_L_fullframe_target_wallpaper.jpg` | A · 49×22 | held · KEY 14 · full 1080p frame (board 10 hero) | one glyph, all zones | `A_L_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | wallpaper | 1x | 0, 0, 1920 × 1080 | **new** |
| 199 | `live_code_lit_1x.png` | live / steps | today in main | - | `live HUD_KeyGlyph.png` | LEVEL 0 / THE MAZE KEY [Bayon 20] | lit | 1x | 40, 920, 400 × 60 | no |
| 200 | `live_code_dark_1x.png` | live / steps | today in main | - | `live HUD_KeyGlyph.png` | LEVEL 0 / THE MAZE KEY [Bayon 20] | dark | 1x | 40, 920, 400 × 60 | no |
| 201 | `live_code_wallpaper_1x.png` | live / steps | today in main | - | `live HUD_KeyGlyph.png` | LEVEL 0 / THE MAZE KEY [Bayon 20] | wallpaper | 1x | 40, 920, 400 × 60 | no |
| 202 | `cmp_live_wallpaper_1x.png` | live / steps | step 0 · today | - | `live HUD_KeyGlyph.png` | LEVEL 0 / THE MAZE KEY [Bayon 20] | wallpaper | 1x | 40, 880, 600 × 120 | no |
| 203 | `cmp_dropin_wallpaper_1x.png` | live / steps | step 1 · drop-in | - | `dropin/slot_40x22 (= A_S_held)` | LEVEL 0 / THE MAZE KEY [Bayon 20] | wallpaper | 1x | 40, 880, 600 × 120 | no |
| 204 | `cmp_target_wallpaper_1x.png` | live / steps | step 2 · target | - | `A_L_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | wallpaper | 1x | 40, 880, 600 × 120 | no |
| 205 | `phone_today_held_wallpaper_3x.jpg` | phone · TestFlight today | held | - | `live HUD_KeyGlyph.png stretched x3` | LEVEL 0 / THE MAZE KEY [Bayon 11] | wallpaper | @3x | 0, 0, 2622 × 1206 | **new** |
| 206 | `phone_target_held_wallpaper_3x.jpg` | phone · proposal (A_L) | held | - | `A_L_held` | KEY [Bayon 17] 14 [Courier Prime Bold 21] | wallpaper | @3x | 0, 0, 2622 × 1206 | **new** |
| 207 | `phone_target_held_lit_3x.jpg` | phone · proposal (A_L) | held | - | `A_L_held` | KEY [Bayon 17] 14 [Courier Prime Bold 21] | lit | @3x | 0, 0, 2622 × 1206 | **new** |
| 208 | `phone_today_locked_leaf_3x.jpg` | phone · TestFlight today | locked | - | `live HUD_KeyGlyph.png stretched x3` | LOCKED · NEEDS THIS ZONE'S KEY [Bayon 17] | leaf | @3x | 0, 0, 2622 × 1206 | **new** |
| 209 | `phone_target_locked_leaf_3x.jpg` | phone · proposal (A_L) | locked | - | `A_L_missing` | LOCKED [Bayon 17] + NEEDS KEY 14 [Plex Mono 11] on #141414 90 % | leaf | @3x | 0, 0, 2622 × 1206 | **new** |
| 210 | `phone_target_locked_dark_3x.jpg` | phone · proposal (A_L) | locked | - | `A_L_missing` | LOCKED [Bayon 17] + NEEDS KEY 14 [Plex Mono 11] on #141414 90 % | dark | @3x | 0, 0, 2622 × 1206 | **new** |

### 7.1 Every crop box

| Box (1x units) | Used by |
|---|---|
| 0, 0, 1920 × 1080 | full frames `<dir>_fullframe_<bg>`, `A_L_fullframe_target_wallpaper` |
| 40, 880, 600 × 120 | compact-row crops (`*_held_*`, `*_state_{held,other,used}_*` of A, A + chip, C), `cmp_*` |
| 40, 860, 600 × 120 | B (48 px full form: 20 px more headroom) |
| 40, 920, 400 × 60 | `label_*`, `C_zone_*`, `Achip_zone_*`, `live_code_*` |
| **720, 512, 480 × 120** | `*_state_missing_*` (was 720, 500: the card runs to y 622) |
| 874 × 402 pt @3x | `phone_*` |

| Background | Scale | x | y | w | h (px, in that scale's frame) | Files |
|---|---|---|---|---|---|---|
| dark | 1x | 0 | 0 | 1920 | 1080 | 5 |
| lit | 1x | 0 | 0 | 1920 | 1080 | 5 |
| office | 1x | 0 | 0 | 1920 | 1080 | 5 |
| wallpaper | 1x | 0 | 0 | 1920 | 1080 | 6 |
| dark | 1x | 40 | 860 | 600 | 120 | 13 |
| lit | 1x | 40 | 860 | 600 | 120 | 13 |
| office | 1x | 40 | 860 | 600 | 120 | 1 |
| wallpaper | 1x | 40 | 860 | 600 | 120 | 1 |
| dark | 2x | 80 | 1720 | 1200 | 240 | 1 |
| lit | 2x | 80 | 1720 | 1200 | 240 | 1 |
| office | 2x | 80 | 1720 | 1200 | 240 | 1 |
| wallpaper | 2x | 80 | 1720 | 1200 | 240 | 1 |
| dark | 1x | 40 | 880 | 600 | 120 | 16 |
| lit | 1x | 40 | 880 | 600 | 120 | 16 |
| office | 1x | 40 | 880 | 600 | 120 | 4 |
| wallpaper | 1x | 40 | 880 | 600 | 120 | 7 |
| dark | 2x | 80 | 1760 | 1200 | 240 | 4 |
| lit | 2x | 80 | 1760 | 1200 | 240 | 4 |
| office | 2x | 80 | 1760 | 1200 | 240 | 4 |
| wallpaper | 2x | 80 | 1760 | 1200 | 240 | 4 |
| dark | 1x | 40 | 920 | 400 | 60 | 19 |
| lit | 1x | 40 | 920 | 400 | 60 | 19 |
| wallpaper | 1x | 40 | 920 | 400 | 60 | 1 |
| dark | 2x | 80 | 1840 | 800 | 120 | 14 |
| lit | 2x | 80 | 1840 | 800 | 120 | 14 |
| dark | 1x | 720 | 512 | 480 | 120 | 5 |
| leaf | 1x | 720 | 512 | 480 | 120 | 5 |
| lit | 1x | 720 | 512 | 480 | 120 | 5 |
| office | 1x | 720 | 512 | 480 | 120 | 5 |
| wallpaper | 1x | 720 | 512 | 480 | 120 | 5 |

---

## 8. Figma: what this workflow wrote, ids and slot map

**Key-HUD design: nothing.** Read only, for reference (平面视觉's, `Tools/figma/hud_key/figma_ids.json`):

| What | Id |
|---|---|
| Section "FRONTROOMS · HUD KEY · UI VARIATIONS (平面视觉)" | `2532:4038` (68657, 2000; 8280 × 11920) |
| KV-SRC (backgrounds + crosshair as image fills) | `2532:4039`; lit `2532:4040`, wallpaper `2532:4041`, office `2532:4042`, dark `2532:4043`, crosshair `2532:4044` |
| KV-LIB (the 33 masters as components) | `2579:4775`; sets A `2579:4809`, chip `2579:4810`, B `2579:4811`, C `2579:4812`; crosshair `2579:4813` |
| UI MOCKUP (HUD type and colour system; read only) | `2256:8`; HUD / Key `2256:161`; Overlay / Key chip `2256:191` |

**Verification log (`2595:6093` "FRONTROOMS · VISUAL VERIFICATION LOG").** Procedure: `Documentation/VERIFICATION_LOG.md`. The fix stage claimed VL100–VL105, grew the section from 31,080 to **33,560** (27 rows; the band x 76937–86017, y 33080–35960 was checked clear), and refreshed the images of VL075–VL077 (slots re-fitted to the new images' aspect).

| VL | Frame id | Cell (section-relative) | Title | Verdict | Slot → image |
|---|---|---|---|---|---|
| VL075 | `2605:7264` | (6240, 22480) | Main's key FBX vs the trace | PASS | `img:08_main_fbx_vs_trace` `2605:7272` → `images/08_main_fbx_vs_trace.png` |
| VL076 | `2605:7273` | (120, 23720) | Whole-number label sizes | PASS | `img:09_integer_type_sizes` `2605:7281` → `images/09_integer_type_sizes.png` |
| VL077 | `2605:7282` | (2160, 23720) | @3x exports for touch | PASS | `img:10_3x_exports` `2605:7290` → `images/10_3x_exports.png` |
| VL100 | `2760:6093` | (120, 31160) | Locked prompt on a hint card | PASS | `img:12_locked_prompt_card` `2760:6101` → `images/12_locked_prompt_card.png` |
| VL101 | `2760:6102` | (2160, 31160) | Key states dim by shape | PASS | `img:13_states_by_shape` `2760:6110` → `images/13_states_by_shape.png` |
| VL102 | `2760:6111` | (4200, 31160) | Outline stroke no longer cut | PASS | `img:14_outline_stroke` `2760:6119` → `images/14_outline_stroke.png` |
| VL103 | `2760:6120` | (6240, 31160) | Phone key HUD and prompt | FLAG | `img:15_phone_hud` `2760:6128` → `images/15_phone_hud.png` |
| VL104 | `2760:6129` | (120, 32400) | Key pipeline rebuilt | PASS | `img:11_pipeline_recovered` `2760:6137` → `images/11_pipeline_recovered.png` |
| VL105 | `2760:6138` | (2160, 32400) | Tag chip hangs on a ring | WAIT-RED | `img:16_chip_ring` `2760:6146` → `images/16_chip_ring.png` |

VL075–VL077 (2026-10-07): images rebuilt with Bayon labels only (no captions, no numbers on the image), 08 re-run on main (IoU 0.99996 again) and re-laid out 2 × 2, 09 without the overlapping header (its "before" crops regenerated and checked pixel-identical to the 2026-10-03 sheet), 10 with the fix-stage masters; VL077's statement now says median 0.06 %.

---

## 9. Checks against main (`279c144`, 2026-10-07)

| Item | Main now | Effect |
|---|---|---|
| `FrontRooms3DGame.cs` key panel (`c1f2d31`, 2026-10-04) | `HUD / Key` anchored top-left on every platform; desktop keeps (72, 118) → **off-screen on desktop** | contract item 1 (`04_for_red.md` §1). Lines now: panel 1883–1892, `UpdateHud` key logic 2208–2215, `KeyLabel` 2253, room meta 2182, `UiFont` 1482–1485 |
| Phone HUD (`c1f2d31`) | canvas 874 × 402, key at (78, 72) pt, label Bayon 11, crosshair rect 24 pt, one sprite per element | §5, contract item 3 |
| `FrontRoomsMapWorld.cs` | `KeysHeld` 57, `HasKeyFor` 830, locked prompt string 2203, `SpawnKey` 2852 (still the 0.32 × 0.12 × 0.12 m yellow cube) | contract items 5, 6, 8 |
| `Kit_Key_Zone.fbx` (+ `_Nickel`) | no commit since `75cfdff` | VL075 re-run: IoU 0.99996 |
| `interact_key*.py` | `interact_key_common.py` changed in `03c43ed` (`orient` helper, `rrect_sweep` for the cabinet) | the key blank is not built with them; geometry re-dumped identical |
| `Assets/Resources/UI/` | no commit since `7320ed1`; `HUD_KeyGlyph.png` GUID `eb266a1345aeb45f69bf39f9c6d34fb4` | drop-in plan stands |
| Codex audit | `20_findings.md` never written; `00_main_state.md`: "C9 key icon: not touched"; no `10_review_*.md` names K1 | nothing to resolve |

Nothing was written under `Assets/`. Unity was not opened.

---

## 10. Open issues

- **10.1 (2026-10-03) — the outline masters clipped half their stroke:** fixed (§0, VL102).
- Red's decisions (`04_for_red.md` §4): direction, prompt card vs shadow, other-zone panel, label, drop-in now, chip.
- 平面视觉's decisions: the prompt card (their UI06 mockup has the same contrast problem), B's yellow rule (stamina / hold-bar accent in a calm state), the "used" column.
- Map data and the HUD code: contract request (`04_for_red.md` §5.3), not sent or applied.
- The almond `leaf` ground is a worst case; replace it with an in-engine capture of an L0-K door once the kit doors are in play.

---

## 11. Scripts

Rebuilt on 2026-10-07 from the transcripts (`wf_05944ce4-1a3`: full-file checkpoints from `cat` outputs + every later patch, replayed in order by `$W/keyicon_design_recover/replay.py`). The rebuilt originals reproduce the 2026-10-03 files byte for byte (VL104). Durable copies of the final versions: `scripts/*.py.txt` (+ `shot.sh.txt`).

| Script (`$W/keyicon_design/`) | What | Run with |
|---|---|---|
| `dump_geometry.py` | `geometry_mm.json` from read-only copies of the G3 modules (`mods/`, from `ef061ae`) | Blender `-b --python` |
| `design.py` | SVG + PNG @1x/@2x/@3x for all masters (fix: `emit_outline`, `ring_link`) | Blender's Python 3.11 |
| `make_leaf_bg.py` | `design/bg/leaf_48_almond.png`, `leaf_48_wood.png` | `$W/venv/bin/python` |
| `composites.py` | all HUD crops (`all` / `held` / `states` / `labels` / `zones` / `phone` / `full`) | Blender's Python 3.11 |
| `composites_live.py` | `live_code_*`, `cmp_*` | Blender's Python 3.11 |
| `export_3x.py`, `export_touch.py` | the @3x and `_Touch` drop-ins, the crosshairs, the checks | Blender's Python 3.11 |
| `boards.py` (+ `shot.sh`) | boards 01–10 (headless Chrome); model panels from `model_renders.py` / `model_renders_true.py` (+ `$W/g3/g3_scene.py`, textures from `$W/g3/gen_textures_g3.py`) | `/usr/bin/python3` |
| `measure.py` | §2.5 contrast table → `measure_fix.json` | Blender's Python 3.11 |
| `make_cells.py`, `make_verif2.py` | verification images 08–16 | Blender's Python / `$W/venv/bin/python` |
| `gen_handoff.py` | §7 and `03_variants.tsv` from `plan.py`'s record | `/usr/bin/python3` |
| `$W/keyicon_resume/verify_main_fbx.py` | main's FBX vs the trace (VL075) | Blender `-b --python` |

Set `KEYICON_DES` to render into a scratch folder instead of `design/`.

