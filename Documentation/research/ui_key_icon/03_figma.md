# 03 — Key icon: hand-off to 平面视觉 (Figma stage)

**Status: DRAFT, 2026-10-03 23:0x.** Not FINAL yet. Per `figma_target.md`, the FINAL mark goes on top only after the critic and fix stages. The visual chat then pings 平面视觉.

**No key-HUD block was built in Figma by this workflow** (`figma_target.md`, 16:5x and 22:0x). 平面视觉 owns the native build: section `2532:4038` "FRONTROOMS · HUD KEY · UI VARIATIONS (平面视觉)" at (68657, 2000), library KV-LIB `2579:4775`, sync in `Frontrooms3D/Tools/figma/hud_key/`. This file is the source list for their re-sync.

What this stage did:
1. **Whole-number type sizes** in `composites.py` (Unity legacy `Text` takes an int `fontSize`): Courier Prime Bold 24.63 → **25**, IBM Plex Mono numerals 20.45 → **20**. All 188 HUD crops re-rendered; 113 changed (§3, §6).
2. **@3x exports** for the touch track: all 33 masters, both drop-in glyphs and the crosshair (§5).
3. **Durable dark background**: the dark frame lived only in the scratchpad. It is now also in `design/bg/` (same bytes, §4).
4. **Boards 01–10** re-rendered with the new sizes (§6).
5. **Verification** slides VL075–VL077 in the Figma VISUAL VERIFICATION LOG (§8).
6. **Checks against main** after Codex's commits (§9).

All paths below are under `Documentation/research/ui_key_icon/` unless they start with `Assets/`, `Tools/` or `$SP` (= the session scratchpad, `/private/tmp/claude-501/…/scratchpad`).

---

## 1. Recommended direction and states

**Recommendation: A · Cut key** (`02_design.md` §2). `Kit_Key_Zone`'s own outline, one paper colour.
- **Ship today:** `dropin/slot_40x22/HUD_KeyGlyph.png` in today's 40 × 22 rect. No code change.
- **Target:** 49 × 22 (`A_L`) + label "KEY" in Bayon 20 and the zone number in Courier Prime Bold 25. Needs the contract request in `02_design.md` §7.
- **Options:** A + tag chip (once zones carry tag data); B for 2x moments. C is not used.

States (final values; the opacities are engine values, blended in **linear light**, §2.4):

| State | Glyph | Label | Meta (IBM Plex Mono 13, muted) | Where |
|---|---|---|---|---|
| **Held, this zone** | `<dir>_held` at 100 % | paper `#F4F1E8` at 100 % | none (B: "OPENS DOORS OUT OF THIS ZONE") | bottom-left row |
| **Held, other zone** | `<dir>_held` at **40 %** | muted `#BDBAB0` at **70 %** | "NOT THIS ZONE", muted at 100 % (B: "OPENS ZONE 14 ONLY", at 70 %, no yellow rule) | bottom-left row |
| **Used** (only if keys become single-use) | `<dir>_muted` at 100 % (muted fill; B and chip tag fill at 55 % inside the glyph) | muted at 100 % | "DOOR OPENED", muted | bottom-left row |
| **Missing** (locked door) | `<dir>_missing` (outline 1.25 px, paper at **70 %**, inside the SVG); B and A + chip use `A_L_missing` | "LOCKED", Bayon 20, muted | "NEEDS KEY 14", centred | the prompt under the crosshair |

---

## 2. Every constant in `composites.py` (final values)

### 2.1 Layout (1080p canvas, 1x px; 2x = the same numbers × 2)

| Constant / rule | Value | Source in `composites.py` |
|---|---|---|
| Canvas | 1920 × 1080, CanvasScaler match 0.5 | docstring |
| Glyph x | **72** | `compact()`, `full()` |
| `ROW_Y` (glyph rect top, compact row) | **940** | constant |
| Row height | **22** | glyph height |
| `LABEL_BASE` (label baseline) | **958** (= `ROW_Y` + 18) | constant |
| Label x | glyph x + glyph width + **14** (A_S 126, A_L 135, C 126, A + chip rect 163 / round 155 / long 171) | `compact()` |
| "KEY" → numeral gap | "KEY" advance (Bayon 20: 25.79 px) + **6** | `draw_label()` |
| Meta after the label | label end + **12**, baseline **957** (`LABEL_BASE` − 1) | `compact()` |
| `FULL_Y` (full form top, B) | **914**, 48 high, same bottom edge (962) as the compact row | constant |
| Full form rule | **4 × 48** at x 72, accent `#F4DF3B` | `full()` |
| Full form glyph x | 72 + 4 + 14 = **90** (no rule: 72) | `full()` |
| Full form name | glyph x + glyph width + 14; baseline `FULL_Y` + 22 = **936** | `full()` |
| Full form meta | same x; baseline `FULL_Y` + 41 = **955** | `full()` |
| Prompt (missing) | glyph + 10 + "LOCKED", centred on x 960; glyph centre y **584**; "LOCKED" baseline **591**; meta centred, baseline 584 + max(30, glyph h / 2 + 16) = **614** | `prompt()` |
| Crosshair | 32 × 32 rect at (944, 524), the 64 × 64 sprite (dot Ø 16 on screen) | `crosshair()` |
| Room meta | IBM Plex Mono 13, muted, x 96, baseline **102** | `room_type()` |
| Room name | Source Serif 4, **50**, paper, x 96, baseline **165** | `room_type()` |

### 2.2 Type sizes (all whole numbers now)

| Use | Face | Size | Cap height | Was |
|---|---|---|---|---|
| Label words ("KEY", "LEVEL 0 KEY", "LOCKED") | Bayon Regular | **20** | 14.28 px | 20 |
| Key numeral (`COUR_SZ`) | Courier Prime Bold | **25** | 14.49 px (+0.21 vs Bayon) | 24.6335 |
| Numeral, Plex option and C zone labels (`MONO_SZ`) | IBM Plex Mono Regular | **20** | 13.96 px (−0.32) | 20.4546 |
| Meta | IBM Plex Mono Regular | **13** | — | 13 |
| Room name | Source Serif 4 (variable, default instance) | **50** | — | 50 |

`COUR_SZ_EXACT` (24.6335) and `MONO_SZ_EXACT` (20.4546) stay in the file for reference only; nothing draws with them.
In Unity: the numeral is a second `Text` element (legacy `Text` cannot switch fonts inside one string), Courier Prime Bold size 25, 6 px after "KEY". See `02_design.md` §3 and §7.

### 2.3 Glyph sizes (1x; the SVG masters' width × height)

| Glyph | Size | Note |
|---|---|---|
| `A_S_*` | 40 × 22 | key 40 × 18, centred; today's slot |
| `A_L_*` | 49 × 22 | key fills the row; target |
| `C_*` | 40 × 22 | plate 40 × 20, R 2 |
| `Achip_rect_*` / `round_*` / `long_*` | 77 / 69 / 85 × 22 | chip + 4 px + A_L key |
| `B_*` | 72 × 48 | key + ring + tag; needs the 48 px full form |

### 2.4 Colours and opacities

| Name | Value |
|---|---|
| Paper | `#F4F1E8` |
| Muted | `#BDBAB0` |
| Accent (B rule only) | `#F4DF3B` |
| Card (C dark plate) | `#141414` |
| Ink (C white plate key, B tag numbers) | `#1E1D1A` |
| Zone red / blue / white (`Prop_Plastic*` base colours, sRGB) | `#8C0F0D` / `#0F2973` / `#FFFFFF` |

| Opacity (linear-light engine value) | Where |
|---|---|
| **40 %** | other-zone glyph |
| **70 %** | other-zone label (and B's other-zone name + meta); the missing outline (paper, inside the SVG) |
| **90 %** | C dark plate (held) |
| **60 %** | C dark plate (used / muted) |
| **55 %** | used tag or chip fill over the paper rim (inside the B and A + chip muted glyphs) |
| 100 % | everything else |

**Blending.** The composites blend every layer in **linear light**, like Unity's overlay canvas in a linear-colour project (no post on the HUD). Figma blends in **sRGB**. The numbers above are the engine values. 平面视觉's `fits.py` fits an sRGB alpha per crop on that crop's own background pixels; keep doing that for every semi-transparent element (40 / 55 / 60 / 70 / 90 %).

---

## 3. What changed in this stage (for the re-sync)

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

## 4. The four background frames

| Key | File | Size | What |
|---|---|---|---|
| `lit` | `Documentation/research/room_visuals/images/room_map-l0-low_wide.jpg` | 1920 × 1080 | Level 0 low rooms, lit (worst contrast for paper, `#827042` under the glyph) |
| `wallpaper` | `Documentation/research/room_visuals/images/room_map-l0-standard_wide.jpg` | 1920 × 1080 | Level 0 standard rooms, wallpaper and door |
| `office` | `Documentation/research/room_visuals/images/room_map-office_wide.jpg` | 1920 × 1080 | Office zone |
| `dark` | `design/bg/dark_59_door_key_open_t0000ms.png` (copy of `$SP/audit_run3_frames/59_door_key_open_t0000ms.png`) | 1920 × 1080 | in-engine audit frame at a key door, dark cell |

Their Figma copies are in KV-SRC `2532:4039` (`figma_ids.json`). **For 平面视觉:** `fits.py` line 14 still points at the scratchpad copy of the dark frame. The scratchpad is temporary. Point it at `design/bg/dark_59_door_key_open_t0000ms.png` (same bytes).

2x crops: the HUD region (x 0–640, y 840–1080) is upsampled ×2 bilinear before the 2x sprites and the 2x type go on it (`frame(name, 2)`).

---

## 5. @3x exports (touch track)

Phones are @3x. `TOUCH_CONTROLS.md` puts the key under the zone block at (78, 72), in a 150 × 22 pt panel. A 22 pt row is **66 px** at @3x. Desktop output does not change.

| File | Size | Use |
|---|---|---|
| `design/png/<name>@3x.png` × 33 | 3 × the 1x size: 120 / 147 / 207 / 231 / 255 × 66 for the 22 px row, 216 × 144 for B | every master, every state |
| `design/dropin/slot_40x22/HUD_KeyGlyph@3x.png` | 120 × 66 | = `A_S_held@3x` |
| `design/dropin/slot_49x22/HUD_KeyGlyph@3x.png` | 147 × 66 | = `A_L_held@3x` (the recommended target) |
| `design/dropin/crosshair/HUD_Crosshair@3x.png` | 192 × 192, a white dot Ø 96 px, anti-aliased | the 32 pt crosshair rect = 96 px on a phone, so the sprite is 2 : 1 like the desktop one (64 px sprite in a 32 px rect). The dot shows at 16 pt |
| `design/dropin/crosshair/HUD_Crosshair.svg` | 32 × 32 units, `circle r 8` | vector source |
| `design/svg/*.svg` | 1x units | vector source for every glyph |

The desktop `Assets/Resources/UI/HUD_Crosshair.png` (64 × 64, Ø 32) is unchanged.

Checks (`$SP/keyicon_figma/export_3x_checks.json`, run on the files):
- **36 / 36 files pass:** all four corners have alpha 0; the one-colour files (A, the drop-ins, the crosshair) hold one RGB value in every pixel, transparent ones included, so filtering never pulls in a dark fringe.
- **@3x box-filtered to 1x vs the @1x file:** mean error per file 0.004–0.185 % (median 0.031 %). The worst single pixel is ≤ 4.5 %, except the nine B lockups (9.5 % on one ring pixel).
- **Crosshair:** area diameter 96.00 px, centre (95.5, 95.5). Box-filtered to 64 it matches the desktop sprite: mean 0.023 %, max 1.61 %.
- Import (when the touch track lands it): same as the desktop drop-in. sRGB, Alpha Is Transparency, Bilinear, no mipmaps, Compression None.

---

## 6. Boards and drop-in

- Boards 01–10 (`design/*.png`) were re-rendered at 23:0x with the whole sizes. They are review boards, not part of the sync.
- The drop-in files are unchanged except for the added @3x (§5). `dropin/slot_40x22/HUD_KeyGlyph.png` is still byte-identical to `png/A_S_held@2x.png`, and `slot_49x22` to `A_L_held@2x.png`. Nothing was copied into `Assets/`.

---

## 7. The variant list: one row per rendered file

Columns: direction × state × zone × label × background × scale, the glyph sprite (with its opacity when not 100 %), the text set (face, size, opacity when not 100 %), and the crop box in px **in that scale's frame** (x, y, w × h). "Changed" = pixels differ from the 22:16 set. The same rows, with the crop also in 1x units and an `ops_changed` column, are in `03_variants.tsv`.

Rows 1–182 are `composites.py` (what 平面视觉's `plan.py` records, in its order). Rows 183–188 are `composites_live.py` (main's live glyph and the three steps; not twins).

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
| 41 | `A_S_state_held_lit_1x.png` | A · 40×22 | held | one glyph, all zones | `A_S_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 1x | 40, 880, 600 × 120 | **yes** |
| 42 | `A_S_state_other_lit_1x.png` | A · 40×22 | other | one glyph, all zones | `A_S_held @40%` | KEY [Bayon 20 @70%] 14 [Courier Prime Bold 25 @70%] NOT THIS ZONE [Plex Mono 13] | lit | 1x | 40, 880, 600 × 120 | **yes** |
| 43 | `A_S_state_missing_lit_1x.png` | A · 40×22 | missing | one glyph, all zones | `A_S_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] | lit | 1x | 720, 500, 480 × 120 | no |
| 44 | `A_S_state_used_lit_1x.png` | A · 40×22 | used | one glyph, all zones | `A_S_muted` | KEY [Bayon 20] 14 [Courier Prime Bold 25] DOOR OPENED [Plex Mono 13] | lit | 1x | 40, 880, 600 × 120 | **yes** |
| 45 | `A_S_state_held_dark_1x.png` | A · 40×22 | held | one glyph, all zones | `A_S_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 1x | 40, 880, 600 × 120 | **yes** |
| 46 | `A_S_state_other_dark_1x.png` | A · 40×22 | other | one glyph, all zones | `A_S_held @40%` | KEY [Bayon 20 @70%] 14 [Courier Prime Bold 25 @70%] NOT THIS ZONE [Plex Mono 13] | dark | 1x | 40, 880, 600 × 120 | **yes** |
| 47 | `A_S_state_missing_dark_1x.png` | A · 40×22 | missing | one glyph, all zones | `A_S_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] | dark | 1x | 720, 500, 480 × 120 | no |
| 48 | `A_S_state_used_dark_1x.png` | A · 40×22 | used | one glyph, all zones | `A_S_muted` | KEY [Bayon 20] 14 [Courier Prime Bold 25] DOOR OPENED [Plex Mono 13] | dark | 1x | 40, 880, 600 × 120 | **yes** |
| 49 | `A_L_state_held_lit_1x.png` | A · 49×22 | held | one glyph, all zones | `A_L_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 1x | 40, 880, 600 × 120 | **yes** |
| 50 | `A_L_state_other_lit_1x.png` | A · 49×22 | other | one glyph, all zones | `A_L_held @40%` | KEY [Bayon 20 @70%] 14 [Courier Prime Bold 25 @70%] NOT THIS ZONE [Plex Mono 13] | lit | 1x | 40, 880, 600 × 120 | **yes** |
| 51 | `A_L_state_missing_lit_1x.png` | A · 49×22 | missing | one glyph, all zones | `A_L_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] | lit | 1x | 720, 500, 480 × 120 | no |
| 52 | `A_L_state_used_lit_1x.png` | A · 49×22 | used | one glyph, all zones | `A_L_muted` | KEY [Bayon 20] 14 [Courier Prime Bold 25] DOOR OPENED [Plex Mono 13] | lit | 1x | 40, 880, 600 × 120 | **yes** |
| 53 | `A_L_state_held_dark_1x.png` | A · 49×22 | held | one glyph, all zones | `A_L_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 1x | 40, 880, 600 × 120 | **yes** |
| 54 | `A_L_state_other_dark_1x.png` | A · 49×22 | other | one glyph, all zones | `A_L_held @40%` | KEY [Bayon 20 @70%] 14 [Courier Prime Bold 25 @70%] NOT THIS ZONE [Plex Mono 13] | dark | 1x | 40, 880, 600 × 120 | **yes** |
| 55 | `A_L_state_missing_dark_1x.png` | A · 49×22 | missing | one glyph, all zones | `A_L_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] | dark | 1x | 720, 500, 480 × 120 | no |
| 56 | `A_L_state_used_dark_1x.png` | A · 49×22 | used | one glyph, all zones | `A_L_muted` | KEY [Bayon 20] 14 [Courier Prime Bold 25] DOOR OPENED [Plex Mono 13] | dark | 1x | 40, 880, 600 × 120 | **yes** |
| 57 | `C_state_held_lit_1x.png` | C · plate | held | dark plate | `C_dark_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 1x | 40, 880, 600 × 120 | **yes** |
| 58 | `C_state_other_lit_1x.png` | C · plate | other | dark plate | `C_dark_held @40%` | KEY [Bayon 20 @70%] 14 [Courier Prime Bold 25 @70%] NOT THIS ZONE [Plex Mono 13] | lit | 1x | 40, 880, 600 × 120 | **yes** |
| 59 | `C_state_missing_lit_1x.png` | C · plate | missing | dark plate | `C_dark_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] | lit | 1x | 720, 500, 480 × 120 | no |
| 60 | `C_state_used_lit_1x.png` | C · plate | used | dark plate | `C_dark_muted` | KEY [Bayon 20] 14 [Courier Prime Bold 25] DOOR OPENED [Plex Mono 13] | lit | 1x | 40, 880, 600 × 120 | **yes** |
| 61 | `C_state_held_dark_1x.png` | C · plate | held | dark plate | `C_dark_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 1x | 40, 880, 600 × 120 | **yes** |
| 62 | `C_state_other_dark_1x.png` | C · plate | other | dark plate | `C_dark_held @40%` | KEY [Bayon 20 @70%] 14 [Courier Prime Bold 25 @70%] NOT THIS ZONE [Plex Mono 13] | dark | 1x | 40, 880, 600 × 120 | **yes** |
| 63 | `C_state_missing_dark_1x.png` | C · plate | missing | dark plate | `C_dark_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] | dark | 1x | 720, 500, 480 × 120 | no |
| 64 | `C_state_used_dark_1x.png` | C · plate | used | dark plate | `C_dark_muted` | KEY [Bayon 20] 14 [Courier Prime Bold 25] DOOR OPENED [Plex Mono 13] | dark | 1x | 40, 880, 600 × 120 | **yes** |
| 65 | `B_state_held_lit_1x.png` | B · key on tag | held | rect tag · red · 14 | `B_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | lit | 1x | 40, 860, 600 × 120 | **yes** |
| 66 | `B_state_other_lit_1x.png` | B · key on tag | other | rect tag · red · 14 | `B_rect_red_held @40%` | KEY [Bayon 20 @70%] 14 [Courier Prime Bold 25 @70%] OPENS ZONE 14 ONLY [Plex Mono 13 @70%] | lit | 1x | 40, 860, 600 × 120 | **yes** |
| 67 | `B_state_missing_lit_1x.png` | B · key on tag | missing | one glyph, all zones | `A_L_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] | lit | 1x | 720, 500, 480 × 120 | no |
| 68 | `B_state_used_lit_1x.png` | B · key on tag | used | rect tag · red · 14 | `B_rect_red_muted` | KEY [Bayon 20] 14 [Courier Prime Bold 25] DOOR OPENED [Plex Mono 13] | lit | 1x | 40, 860, 600 × 120 | **yes** |
| 69 | `B_state_held_dark_1x.png` | B · key on tag | held | rect tag · red · 14 | `B_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | dark | 1x | 40, 860, 600 × 120 | **yes** |
| 70 | `B_state_other_dark_1x.png` | B · key on tag | other | rect tag · red · 14 | `B_rect_red_held @40%` | KEY [Bayon 20 @70%] 14 [Courier Prime Bold 25 @70%] OPENS ZONE 14 ONLY [Plex Mono 13 @70%] | dark | 1x | 40, 860, 600 × 120 | **yes** |
| 71 | `B_state_missing_dark_1x.png` | B · key on tag | missing | one glyph, all zones | `A_L_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] | dark | 1x | 720, 500, 480 × 120 | no |
| 72 | `B_state_used_dark_1x.png` | B · key on tag | used | rect tag · red · 14 | `B_rect_red_muted` | KEY [Bayon 20] 14 [Courier Prime Bold 25] DOOR OPENED [Plex Mono 13] | dark | 1x | 40, 860, 600 × 120 | **yes** |
| 73 | `Achip_state_held_lit_1x.png` | A + chip | held | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 1x | 40, 880, 600 × 120 | **yes** |
| 74 | `Achip_state_other_lit_1x.png` | A + chip | other | rect tag · red | `Achip_rect_red_held @40%` | KEY [Bayon 20 @70%] 14 [Courier Prime Bold 25 @70%] NOT THIS ZONE [Plex Mono 13] | lit | 1x | 40, 880, 600 × 120 | **yes** |
| 75 | `Achip_state_missing_lit_1x.png` | A + chip | missing | one glyph, all zones | `A_L_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] | lit | 1x | 720, 500, 480 × 120 | no |
| 76 | `Achip_state_used_lit_1x.png` | A + chip | used | rect tag · red | `Achip_rect_red_muted` | KEY [Bayon 20] 14 [Courier Prime Bold 25] DOOR OPENED [Plex Mono 13] | lit | 1x | 40, 880, 600 × 120 | **yes** |
| 77 | `Achip_state_held_dark_1x.png` | A + chip | held | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 1x | 40, 880, 600 × 120 | **yes** |
| 78 | `Achip_state_other_dark_1x.png` | A + chip | other | rect tag · red | `Achip_rect_red_held @40%` | KEY [Bayon 20 @70%] 14 [Courier Prime Bold 25 @70%] NOT THIS ZONE [Plex Mono 13] | dark | 1x | 40, 880, 600 × 120 | **yes** |
| 79 | `Achip_state_missing_dark_1x.png` | A + chip | missing | one glyph, all zones | `A_L_missing` | LOCKED [Bayon 20] NEEDS KEY 14 [Plex Mono 13] | dark | 1x | 720, 500, 480 × 120 | no |
| 80 | `Achip_state_used_dark_1x.png` | A + chip | used | rect tag · red | `Achip_rect_red_muted` | KEY [Bayon 20] 14 [Courier Prime Bold 25] DOOR OPENED [Plex Mono 13] | dark | 1x | 40, 880, 600 × 120 | **yes** |
| 81 | `label_bayon_lit_1x.png` | A · 49×22 | held · label bayon | one glyph, all zones | `A_L_held` | KEY 14 [Bayon 20] | lit | 1x | 40, 920, 400 × 60 | no |
| 82 | `label_bayon_lit_2x.png` | A · 49×22 | held · label bayon | one glyph, all zones | `A_L_held` | KEY 14 [Bayon 20] | lit | 2x | 80, 1840, 800 × 120 | no |
| 83 | `label_bayon_dark_1x.png` | A · 49×22 | held · label bayon | one glyph, all zones | `A_L_held` | KEY 14 [Bayon 20] | dark | 1x | 40, 920, 400 × 60 | no |
| 84 | `label_bayon_dark_2x.png` | A · 49×22 | held · label bayon | one glyph, all zones | `A_L_held` | KEY 14 [Bayon 20] | dark | 2x | 80, 1840, 800 × 120 | no |
| 85 | `label_plex_lit_1x.png` | A · 49×22 | held · label plex | one glyph, all zones | `A_L_held` | KEY [Bayon 20] 14 [Plex Mono 20] | lit | 1x | 40, 920, 400 × 60 | **yes** |
| 86 | `label_plex_lit_2x.png` | A · 49×22 | held · label plex | one glyph, all zones | `A_L_held` | KEY [Bayon 20] 14 [Plex Mono 20] | lit | 2x | 80, 1840, 800 × 120 | **yes** |
| 87 | `label_plex_dark_1x.png` | A · 49×22 | held · label plex | one glyph, all zones | `A_L_held` | KEY [Bayon 20] 14 [Plex Mono 20] | dark | 1x | 40, 920, 400 × 60 | **yes** |
| 88 | `label_plex_dark_2x.png` | A · 49×22 | held · label plex | one glyph, all zones | `A_L_held` | KEY [Bayon 20] 14 [Plex Mono 20] | dark | 2x | 80, 1840, 800 × 120 | **yes** |
| 89 | `label_courier_lit_1x.png` | A · 49×22 | held · label courier | one glyph, all zones | `A_L_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 1x | 40, 920, 400 × 60 | **yes** |
| 90 | `label_courier_lit_2x.png` | A · 49×22 | held · label courier | one glyph, all zones | `A_L_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 2x | 80, 1840, 800 × 120 | **yes** |
| 91 | `label_courier_dark_1x.png` | A · 49×22 | held · label courier | one glyph, all zones | `A_L_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 1x | 40, 920, 400 × 60 | **yes** |
| 92 | `label_courier_dark_2x.png` | A · 49×22 | held · label courier | one glyph, all zones | `A_L_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 2x | 80, 1840, 800 × 120 | **yes** |
| 93 | `label_level_lit_1x.png` | A · 49×22 | held · label level | one glyph, all zones | `A_L_held` | LEVEL 0 KEY [Bayon 20] | lit | 1x | 40, 920, 400 × 60 | no |
| 94 | `label_level_lit_2x.png` | A · 49×22 | held · label level | one glyph, all zones | `A_L_held` | LEVEL 0 KEY [Bayon 20] | lit | 2x | 80, 1840, 800 × 120 | no |
| 95 | `label_level_dark_1x.png` | A · 49×22 | held · label level | one glyph, all zones | `A_L_held` | LEVEL 0 KEY [Bayon 20] | dark | 1x | 40, 920, 400 × 60 | no |
| 96 | `label_level_dark_2x.png` | A · 49×22 | held · label level | one glyph, all zones | `A_L_held` | LEVEL 0 KEY [Bayon 20] | dark | 2x | 80, 1840, 800 × 120 | no |
| 97 | `label_code_lit_1x.png` | A · 49×22 | held · label code | one glyph, all zones | `A_L_held` | LEVEL 0 / THE MAZE KEY [Bayon 20] | lit | 1x | 40, 920, 400 × 60 | no |
| 98 | `label_code_lit_2x.png` | A · 49×22 | held · label code | one glyph, all zones | `A_L_held` | LEVEL 0 / THE MAZE KEY [Bayon 20] | lit | 2x | 80, 1840, 800 × 120 | no |
| 99 | `label_code_dark_1x.png` | A · 49×22 | held · label code | one glyph, all zones | `A_L_held` | LEVEL 0 / THE MAZE KEY [Bayon 20] | dark | 1x | 40, 920, 400 × 60 | no |
| 100 | `label_code_dark_2x.png` | A · 49×22 | held · label code | one glyph, all zones | `A_L_held` | LEVEL 0 / THE MAZE KEY [Bayon 20] | dark | 2x | 80, 1840, 800 × 120 | no |
| 101 | `B_zone_rect_red_lit_1x.png` | B · key on tag | held · zone | rect tag · red · 14 | `B_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | lit | 1x | 40, 860, 600 × 120 | **yes** |
| 102 | `B_zone_rect_blue_lit_1x.png` | B · key on tag | held · zone | rect tag · blue · 14 | `B_rect_blue_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | lit | 1x | 40, 860, 600 × 120 | **yes** |
| 103 | `B_zone_rect_white_lit_1x.png` | B · key on tag | held · zone | rect tag · white · 14 | `B_rect_white_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | lit | 1x | 40, 860, 600 × 120 | **yes** |
| 104 | `B_zone_round_red_lit_1x.png` | B · key on tag | held · zone | round tag · red · 37 | `B_round_red_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | lit | 1x | 40, 860, 600 × 120 | **yes** |
| 105 | `B_zone_round_blue_lit_1x.png` | B · key on tag | held · zone | round tag · blue · 37 | `B_round_blue_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | lit | 1x | 40, 860, 600 × 120 | **yes** |
| 106 | `B_zone_round_white_lit_1x.png` | B · key on tag | held · zone | round tag · white · 37 | `B_round_white_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | lit | 1x | 40, 860, 600 × 120 | **yes** |
| 107 | `B_zone_long_red_lit_1x.png` | B · key on tag | held · zone | long tag · red · 08 | `B_long_red_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | lit | 1x | 40, 860, 600 × 120 | **yes** |
| 108 | `B_zone_long_blue_lit_1x.png` | B · key on tag | held · zone | long tag · blue · 08 | `B_long_blue_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | lit | 1x | 40, 860, 600 × 120 | **yes** |
| 109 | `B_zone_long_white_lit_1x.png` | B · key on tag | held · zone | long tag · white · 08 | `B_long_white_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | lit | 1x | 40, 860, 600 × 120 | **yes** |
| 110 | `C_zone_dark_lit_1x.png` | C · plate | held · zone | dark plate | `C_dark_held` | KEY [Bayon 20] 00 [Plex Mono 20] | lit | 1x | 40, 920, 400 × 60 | **yes** |
| 111 | `C_zone_red_lit_1x.png` | C · plate | held · zone | red plate | `C_red_held` | KEY [Bayon 20] 14 [Plex Mono 20] | lit | 1x | 40, 920, 400 × 60 | **yes** |
| 112 | `C_zone_blue_lit_1x.png` | C · plate | held · zone | blue plate | `C_blue_held` | KEY [Bayon 20] 37 [Plex Mono 20] | lit | 1x | 40, 920, 400 × 60 | **yes** |
| 113 | `C_zone_white_lit_1x.png` | C · plate | held · zone | white plate | `C_white_held` | KEY [Bayon 20] 08 [Plex Mono 20] | lit | 1x | 40, 920, 400 × 60 | **yes** |
| 114 | `B_zone_rect_red_dark_1x.png` | B · key on tag | held · zone | rect tag · red · 14 | `B_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | dark | 1x | 40, 860, 600 × 120 | **yes** |
| 115 | `B_zone_rect_blue_dark_1x.png` | B · key on tag | held · zone | rect tag · blue · 14 | `B_rect_blue_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | dark | 1x | 40, 860, 600 × 120 | **yes** |
| 116 | `B_zone_rect_white_dark_1x.png` | B · key on tag | held · zone | rect tag · white · 14 | `B_rect_white_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | dark | 1x | 40, 860, 600 × 120 | **yes** |
| 117 | `B_zone_round_red_dark_1x.png` | B · key on tag | held · zone | round tag · red · 37 | `B_round_red_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | dark | 1x | 40, 860, 600 × 120 | **yes** |
| 118 | `B_zone_round_blue_dark_1x.png` | B · key on tag | held · zone | round tag · blue · 37 | `B_round_blue_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | dark | 1x | 40, 860, 600 × 120 | **yes** |
| 119 | `B_zone_round_white_dark_1x.png` | B · key on tag | held · zone | round tag · white · 37 | `B_round_white_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | dark | 1x | 40, 860, 600 × 120 | **yes** |
| 120 | `B_zone_long_red_dark_1x.png` | B · key on tag | held · zone | long tag · red · 08 | `B_long_red_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | dark | 1x | 40, 860, 600 × 120 | **yes** |
| 121 | `B_zone_long_blue_dark_1x.png` | B · key on tag | held · zone | long tag · blue · 08 | `B_long_blue_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | dark | 1x | 40, 860, 600 × 120 | **yes** |
| 122 | `B_zone_long_white_dark_1x.png` | B · key on tag | held · zone | long tag · white · 08 | `B_long_white_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | dark | 1x | 40, 860, 600 × 120 | **yes** |
| 123 | `C_zone_dark_dark_1x.png` | C · plate | held · zone | dark plate | `C_dark_held` | KEY [Bayon 20] 00 [Plex Mono 20] | dark | 1x | 40, 920, 400 × 60 | **yes** |
| 124 | `C_zone_red_dark_1x.png` | C · plate | held · zone | red plate | `C_red_held` | KEY [Bayon 20] 14 [Plex Mono 20] | dark | 1x | 40, 920, 400 × 60 | **yes** |
| 125 | `C_zone_blue_dark_1x.png` | C · plate | held · zone | blue plate | `C_blue_held` | KEY [Bayon 20] 37 [Plex Mono 20] | dark | 1x | 40, 920, 400 × 60 | **yes** |
| 126 | `C_zone_white_dark_1x.png` | C · plate | held · zone | white plate | `C_white_held` | KEY [Bayon 20] 08 [Plex Mono 20] | dark | 1x | 40, 920, 400 × 60 | **yes** |
| 127 | `Achip_zone_rect_red_lit_1x.png` | A + chip | held · zone | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 1x | 40, 920, 400 × 60 | **yes** |
| 128 | `Achip_zone_rect_red_lit_2x.png` | A + chip | held · zone | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 2x | 80, 1840, 800 × 120 | **yes** |
| 129 | `Achip_zone_rect_blue_lit_1x.png` | A + chip | held · zone | rect tag · blue | `Achip_rect_blue_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 1x | 40, 920, 400 × 60 | **yes** |
| 130 | `Achip_zone_rect_blue_lit_2x.png` | A + chip | held · zone | rect tag · blue | `Achip_rect_blue_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 2x | 80, 1840, 800 × 120 | **yes** |
| 131 | `Achip_zone_rect_white_lit_1x.png` | A + chip | held · zone | rect tag · white | `Achip_rect_white_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 1x | 40, 920, 400 × 60 | **yes** |
| 132 | `Achip_zone_rect_white_lit_2x.png` | A + chip | held · zone | rect tag · white | `Achip_rect_white_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 2x | 80, 1840, 800 × 120 | **yes** |
| 133 | `Achip_zone_round_red_lit_1x.png` | A + chip | held · zone | round tag · red | `Achip_round_red_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] | lit | 1x | 40, 920, 400 × 60 | **yes** |
| 134 | `Achip_zone_round_red_lit_2x.png` | A + chip | held · zone | round tag · red | `Achip_round_red_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] | lit | 2x | 80, 1840, 800 × 120 | **yes** |
| 135 | `Achip_zone_round_blue_lit_1x.png` | A + chip | held · zone | round tag · blue | `Achip_round_blue_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] | lit | 1x | 40, 920, 400 × 60 | **yes** |
| 136 | `Achip_zone_round_blue_lit_2x.png` | A + chip | held · zone | round tag · blue | `Achip_round_blue_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] | lit | 2x | 80, 1840, 800 × 120 | **yes** |
| 137 | `Achip_zone_round_white_lit_1x.png` | A + chip | held · zone | round tag · white | `Achip_round_white_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] | lit | 1x | 40, 920, 400 × 60 | **yes** |
| 138 | `Achip_zone_round_white_lit_2x.png` | A + chip | held · zone | round tag · white | `Achip_round_white_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] | lit | 2x | 80, 1840, 800 × 120 | **yes** |
| 139 | `Achip_zone_long_red_lit_1x.png` | A + chip | held · zone | long tag · red | `Achip_long_red_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] | lit | 1x | 40, 920, 400 × 60 | **yes** |
| 140 | `Achip_zone_long_red_lit_2x.png` | A + chip | held · zone | long tag · red | `Achip_long_red_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] | lit | 2x | 80, 1840, 800 × 120 | **yes** |
| 141 | `Achip_zone_long_blue_lit_1x.png` | A + chip | held · zone | long tag · blue | `Achip_long_blue_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] | lit | 1x | 40, 920, 400 × 60 | **yes** |
| 142 | `Achip_zone_long_blue_lit_2x.png` | A + chip | held · zone | long tag · blue | `Achip_long_blue_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] | lit | 2x | 80, 1840, 800 × 120 | **yes** |
| 143 | `Achip_zone_long_white_lit_1x.png` | A + chip | held · zone | long tag · white | `Achip_long_white_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] | lit | 1x | 40, 920, 400 × 60 | **yes** |
| 144 | `Achip_zone_long_white_lit_2x.png` | A + chip | held · zone | long tag · white | `Achip_long_white_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] | lit | 2x | 80, 1840, 800 × 120 | **yes** |
| 145 | `Achip_zone_rect_red_dark_1x.png` | A + chip | held · zone | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 1x | 40, 920, 400 × 60 | **yes** |
| 146 | `Achip_zone_rect_red_dark_2x.png` | A + chip | held · zone | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 2x | 80, 1840, 800 × 120 | **yes** |
| 147 | `Achip_zone_rect_blue_dark_1x.png` | A + chip | held · zone | rect tag · blue | `Achip_rect_blue_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 1x | 40, 920, 400 × 60 | **yes** |
| 148 | `Achip_zone_rect_blue_dark_2x.png` | A + chip | held · zone | rect tag · blue | `Achip_rect_blue_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 2x | 80, 1840, 800 × 120 | **yes** |
| 149 | `Achip_zone_rect_white_dark_1x.png` | A + chip | held · zone | rect tag · white | `Achip_rect_white_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 1x | 40, 920, 400 × 60 | **yes** |
| 150 | `Achip_zone_rect_white_dark_2x.png` | A + chip | held · zone | rect tag · white | `Achip_rect_white_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 2x | 80, 1840, 800 × 120 | **yes** |
| 151 | `Achip_zone_round_red_dark_1x.png` | A + chip | held · zone | round tag · red | `Achip_round_red_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] | dark | 1x | 40, 920, 400 × 60 | **yes** |
| 152 | `Achip_zone_round_red_dark_2x.png` | A + chip | held · zone | round tag · red | `Achip_round_red_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] | dark | 2x | 80, 1840, 800 × 120 | **yes** |
| 153 | `Achip_zone_round_blue_dark_1x.png` | A + chip | held · zone | round tag · blue | `Achip_round_blue_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] | dark | 1x | 40, 920, 400 × 60 | **yes** |
| 154 | `Achip_zone_round_blue_dark_2x.png` | A + chip | held · zone | round tag · blue | `Achip_round_blue_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] | dark | 2x | 80, 1840, 800 × 120 | **yes** |
| 155 | `Achip_zone_round_white_dark_1x.png` | A + chip | held · zone | round tag · white | `Achip_round_white_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] | dark | 1x | 40, 920, 400 × 60 | **yes** |
| 156 | `Achip_zone_round_white_dark_2x.png` | A + chip | held · zone | round tag · white | `Achip_round_white_held` | KEY [Bayon 20] 37 [Courier Prime Bold 25] | dark | 2x | 80, 1840, 800 × 120 | **yes** |
| 157 | `Achip_zone_long_red_dark_1x.png` | A + chip | held · zone | long tag · red | `Achip_long_red_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] | dark | 1x | 40, 920, 400 × 60 | **yes** |
| 158 | `Achip_zone_long_red_dark_2x.png` | A + chip | held · zone | long tag · red | `Achip_long_red_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] | dark | 2x | 80, 1840, 800 × 120 | **yes** |
| 159 | `Achip_zone_long_blue_dark_1x.png` | A + chip | held · zone | long tag · blue | `Achip_long_blue_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] | dark | 1x | 40, 920, 400 × 60 | **yes** |
| 160 | `Achip_zone_long_blue_dark_2x.png` | A + chip | held · zone | long tag · blue | `Achip_long_blue_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] | dark | 2x | 80, 1840, 800 × 120 | **yes** |
| 161 | `Achip_zone_long_white_dark_1x.png` | A + chip | held · zone | long tag · white | `Achip_long_white_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] | dark | 1x | 40, 920, 400 × 60 | **yes** |
| 162 | `Achip_zone_long_white_dark_2x.png` | A + chip | held · zone | long tag · white | `Achip_long_white_held` | KEY [Bayon 20] 08 [Courier Prime Bold 25] | dark | 2x | 80, 1840, 800 × 120 | **yes** |
| 163 | `A_S_fullframe_lit.jpg` | A · 40×22 | held · full 1080p frame | one glyph, all zones | `A_S_held` | LEVEL 0 KEY [Bayon 20] | lit | 1x | 0, 0, 1920 × 1080 | no |
| 164 | `A_S_fullframe_wallpaper.jpg` | A · 40×22 | held · full 1080p frame | one glyph, all zones | `A_S_held` | LEVEL 0 KEY [Bayon 20] | wallpaper | 1x | 0, 0, 1920 × 1080 | no |
| 165 | `A_S_fullframe_office.jpg` | A · 40×22 | held · full 1080p frame | one glyph, all zones | `A_S_held` | LEVEL 0 KEY [Bayon 20] | office | 1x | 0, 0, 1920 × 1080 | no |
| 166 | `A_S_fullframe_dark.jpg` | A · 40×22 | held · full 1080p frame | one glyph, all zones | `A_S_held` | LEVEL 0 KEY [Bayon 20] | dark | 1x | 0, 0, 1920 × 1080 | no |
| 167 | `A_L_fullframe_lit.jpg` | A · 49×22 | held · full 1080p frame | one glyph, all zones | `A_L_held` | LEVEL 0 KEY [Bayon 20] | lit | 1x | 0, 0, 1920 × 1080 | no |
| 168 | `A_L_fullframe_wallpaper.jpg` | A · 49×22 | held · full 1080p frame | one glyph, all zones | `A_L_held` | LEVEL 0 KEY [Bayon 20] | wallpaper | 1x | 0, 0, 1920 × 1080 | no |
| 169 | `A_L_fullframe_office.jpg` | A · 49×22 | held · full 1080p frame | one glyph, all zones | `A_L_held` | LEVEL 0 KEY [Bayon 20] | office | 1x | 0, 0, 1920 × 1080 | no |
| 170 | `A_L_fullframe_dark.jpg` | A · 49×22 | held · full 1080p frame | one glyph, all zones | `A_L_held` | LEVEL 0 KEY [Bayon 20] | dark | 1x | 0, 0, 1920 × 1080 | no |
| 171 | `C_fullframe_lit.jpg` | C · plate | held · full 1080p frame | dark plate | `C_dark_held` | LEVEL 0 KEY [Bayon 20] | lit | 1x | 0, 0, 1920 × 1080 | no |
| 172 | `C_fullframe_wallpaper.jpg` | C · plate | held · full 1080p frame | dark plate | `C_dark_held` | LEVEL 0 KEY [Bayon 20] | wallpaper | 1x | 0, 0, 1920 × 1080 | no |
| 173 | `C_fullframe_office.jpg` | C · plate | held · full 1080p frame | dark plate | `C_dark_held` | LEVEL 0 KEY [Bayon 20] | office | 1x | 0, 0, 1920 × 1080 | no |
| 174 | `C_fullframe_dark.jpg` | C · plate | held · full 1080p frame | dark plate | `C_dark_held` | LEVEL 0 KEY [Bayon 20] | dark | 1x | 0, 0, 1920 × 1080 | no |
| 175 | `B_fullframe_lit.jpg` | B · key on tag | held · full 1080p frame | rect tag · red · 14 | `B_rect_red_held` | LEVEL 0 KEY [Bayon 20] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | lit | 1x | 0, 0, 1920 × 1080 | no |
| 176 | `B_fullframe_wallpaper.jpg` | B · key on tag | held · full 1080p frame | rect tag · red · 14 | `B_rect_red_held` | LEVEL 0 KEY [Bayon 20] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | wallpaper | 1x | 0, 0, 1920 × 1080 | no |
| 177 | `B_fullframe_office.jpg` | B · key on tag | held · full 1080p frame | rect tag · red · 14 | `B_rect_red_held` | LEVEL 0 KEY [Bayon 20] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | office | 1x | 0, 0, 1920 × 1080 | no |
| 178 | `B_fullframe_dark.jpg` | B · key on tag | held · full 1080p frame | rect tag · red · 14 | `B_rect_red_held` | LEVEL 0 KEY [Bayon 20] OPENS DOORS OUT OF THIS ZONE [Plex Mono 13] + 4 px rule | dark | 1x | 0, 0, 1920 × 1080 | no |
| 179 | `Achip_fullframe_lit.jpg` | A + chip | held · full 1080p frame | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | lit | 1x | 0, 0, 1920 × 1080 | **yes** |
| 180 | `Achip_fullframe_wallpaper.jpg` | A + chip | held · full 1080p frame | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | wallpaper | 1x | 0, 0, 1920 × 1080 | **yes** |
| 181 | `Achip_fullframe_office.jpg` | A + chip | held · full 1080p frame | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | office | 1x | 0, 0, 1920 × 1080 | **yes** |
| 182 | `Achip_fullframe_dark.jpg` | A + chip | held · full 1080p frame | rect tag · red | `Achip_rect_red_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | dark | 1x | 0, 0, 1920 × 1080 | **yes** |
| 183 | `live_code_lit_1x.png` | live / steps | today in main | - | `live HUD_KeyGlyph.png` | LEVEL 0 / THE MAZE KEY [Bayon 20] | lit | 1x | 40, 920, 400 × 60 | no |
| 184 | `live_code_dark_1x.png` | live / steps | today in main | - | `live HUD_KeyGlyph.png` | LEVEL 0 / THE MAZE KEY [Bayon 20] | dark | 1x | 40, 920, 400 × 60 | no |
| 185 | `live_code_wallpaper_1x.png` | live / steps | today in main | - | `live HUD_KeyGlyph.png` | LEVEL 0 / THE MAZE KEY [Bayon 20] | wallpaper | 1x | 40, 920, 400 × 60 | no |
| 186 | `cmp_live_wallpaper_1x.png` | live / steps | step 0 · today | - | `live HUD_KeyGlyph.png` | LEVEL 0 / THE MAZE KEY [Bayon 20] | wallpaper | 1x | 40, 880, 600 × 120 | no |
| 187 | `cmp_dropin_wallpaper_1x.png` | live / steps | step 1 · drop-in | - | `dropin/slot_40x22 (= A_S_held)` | LEVEL 0 / THE MAZE KEY [Bayon 20] | wallpaper | 1x | 40, 880, 600 × 120 | no |
| 188 | `cmp_target_wallpaper_1x.png` | live / steps | step 2 · target | - | `A_L_held` | KEY [Bayon 20] 14 [Courier Prime Bold 25] | wallpaper | 1x | 40, 880, 600 × 120 | **yes** |

### 7.1 Every crop box

One row per (background, crop box, scale). x, y, w and h are in px in that scale's frame (2x = the 3840 × 2160 frame). Use these to fit the sRGB alpha per background and per crop.

| Box (1x units) | Used by |
|---|---|
| 0, 0, 1920 × 1080 | full frames `<dir>_fullframe_<bg>` |
| 40, 880, 600 × 120 | compact-row crops (`*_held_*`, `*_state_{held,other,used}_*` of A, A + chip, C), `cmp_*` |
| 40, 860, 600 × 120 | B (48 px full form: 20 px more headroom) |
| 40, 920, 400 × 60 | `label_*`, `C_zone_*`, `Achip_zone_*`, `live_code_*` |
| 720, 500, 480 × 120 | `*_state_missing_*` (the prompt under the crosshair) |

| Background | Scale | x | y | w | h (px, in that scale's frame) | Files |
|---|---|---|---|---|---|---|
| dark | 1x | 0 | 0 | 1920 | 1080 | 5 |
| lit | 1x | 0 | 0 | 1920 | 1080 | 5 |
| office | 1x | 0 | 0 | 1920 | 1080 | 5 |
| wallpaper | 1x | 0 | 0 | 1920 | 1080 | 5 |
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
| dark | 1x | 720 | 500 | 480 | 120 | 5 |
| lit | 1x | 720 | 500 | 480 | 120 | 5 |

---

## 8. Figma: what this stage wrote, ids and slot map

**Key-HUD design: nothing.** Read only, for reference (平面视觉's, `Tools/figma/hud_key/figma_ids.json`):

| What | Id |
|---|---|
| Section "FRONTROOMS · HUD KEY · UI VARIATIONS (平面视觉)" | `2532:4038` (68657, 2000; 8280 × 11920) |
| KV-SRC (backgrounds + crosshair as image fills) | `2532:4039`; lit `2532:4040`, wallpaper `2532:4041`, office `2532:4042`, dark `2532:4043`, crosshair `2532:4044` |
| KV-LIB (the 33 masters as components) | `2579:4775`; sets A `2579:4809`, chip `2579:4810`, B `2579:4811`, C `2579:4812`; crosshair `2579:4813` |
| UI MOCKUP (HUD type and colour system; read only) | `2256:8`; HUD / Key `2256:161`; Overlay / Key chip `2256:191` |

**Verification log (visual chat's section `2595:6093` "FRONTROOMS · VISUAL VERIFICATION LOG").** Procedure from `Documentation/VERIFICATION_LOG.md`. Numbers claimed in its §3 index before building (VL069–VL074 had just been taken by the interactables G3 workflow, so these are VL075–VL077). The section was grown from 23,640 to **24,880** (20 rows) after a bounds check: nothing within 400 px below it.

| VL | Frame id | Cell (section-relative) | Title | Verdict | Slot → image |
|---|---|---|---|---|---|
| VL075 | `2605:7264` | (6240, 22480) | Main's key FBX vs the trace | PASS | `img:08_main_fbx_vs_trace` `2605:7272` → `images/08_main_fbx_vs_trace.png` |
| VL076 | `2605:7273` | (120, 23720) | Whole-number label sizes | PASS | `img:09_integer_type_sizes` `2605:7281` → `images/09_integer_type_sizes.png` |
| VL077 | `2605:7282` | (2160, 23720) | @3x exports for touch | PASS | `img:10_3x_exports` `2605:7290` → `images/10_3x_exports.png` |

Each slide: 1920 × 1080, running header in P1/Header/Meta (Plex Mono 13), title P1/Display/Title (Bayon 88), lede P1/Text/Body (Source Serif 4 24, two lines, bottom at y 225), statement P1/Text/Statement (Source Serif 4 50, one line at y 969), one image slot (72, 232, 1776 × 657, FIT). No Inter. One check screenshot each; text not clipped, images whole.

---

## 9. Checks against main after Codex's commits (2026-10-03 23:0x)

| Item | Main now (`75cfdff` + working tree) | Effect |
|---|---|---|
| Key icon files | Codex did not touch them. The audit agrees: `codex_audit/00_main_state.md` lists "C9 key icon: not touched" | none |
| `codex_audit/20_findings.md` | not written yet at 23:0x (the folder has `00_main_state.md`, `10_review_*.md`, `RED_DECISIONS.md`; no `contracts/`). No finding names the key icon | nothing to resolve; re-check before the FINAL mark |
| `Kit_Key_Zone.fbx` (+ `_Nickel`) | Codex's copy from `proj_int`. The audit's kit review (`10_review_kits.md`): G3's third pass changed only `Kit_KeyCabinet.fbx`; the other 41 key/tag/host files are final | the echo holds (VL075: IoU 0.99996) |
| `interact_key_common.py` | modified 22:14 (uncommitted): `profile_sweep` split into `_bridge_rings` + a new `rrect_sweep` for the cabinet's folded lips | the key blank is not built with it; outline unchanged |
| `Assets/Resources/UI/HUD_KeyGlyph.png`, `HUD_Crosshair.png` | unchanged since 17:22 / 10:06 | the drop-in plan in `02_design.md` §6 stands |
| HUD fonts (`Assets/Resources/Fonts`, `Assets/Fonts/Period1990/CourierPrime`) | unchanged | none |
| `FrontRooms3DGame.cs` HUD key code | unchanged (Codex touched only the title logo relay) | the line numbers in `02_design.md` §7 stand |

Nothing was written under `Assets/`. Unity was not opened. No clone was needed: this stage only reads main.

---

## 10. Open issues for the critic and fix stages

### 10.1 The missing-state outlines clip half their stroke at the glyph edges (found here)

The outline masters draw the key's own outline with a 1.25 px centred stroke (C plate: 1.0 px). Where the outline touches the glyph box, the outer half of the stroke falls outside the box and is cut off.

| Master | Path bounds | Box | Clipped sides | Visible stroke there |
|---|---|---|---|---|
| `A_L_missing` | x 0–49, y 0–22 | 49 × 22 | left, right, top, bottom | 0.625 px instead of 1.25 |
| `A_S_missing` | x 0–40, y 2–20 | 40 × 22 | left, right | 0.625 px |
| `C_dark_missing` (plate) | x 0–40, y 1–21 | 40 × 22 | left, right | 0.5 px |
| `B_rect_red_missing` | x 7.6–71, y 1–41.2 | 72 × 48 | none | — |

- **Effect:** in the locked-door prompt, the bow's top, bottom and back edge and the tip of A_L read half as thick as the cuts. Visible in `images/10_3x_exports.png` (A_L_missing) and in the `*_state_missing_*` crops.
- **Exact proposal (fix stage decides):** in `design.py`, build the `_missing` outline in a box 1 px larger on each clipped side and offset the path by +1 px (A_L: 51 × 24, path +1, +1; A_S: 42 × 22, path +1, 0; C: 42 × 22, plate path +1, 0). Then shift the prompt glyph by −1 px so the key itself stays where it is. Alternative: inset the stroke by half its width (keeps the box, shrinks the outline by 0.6 px).
- **Re-sync cost:** 3 SVG masters change (A_L_missing, A_S_missing, C_dark_missing) and their PNGs; the 10 `*_state_missing_*` crops; KV-LIB variants for those 3 masters.

### 10.2 Still open from the design stage

- Red's decisions (`02_design.md` §9): direction, label, chip, states, drop-in now.
- Map data and the HUD code change are contract requests for the map chat (`02_design.md` §7). Nothing was sent or applied.
- The in-game key is still the placeholder cube; the echo only works in play once `Kit_Key_Zone` replaces it.

---

## 11. Scripts (scratchpad, re-runnable)

| Script | What | Run with |
|---|---|---|
| `$SP/keyicon_design/design.py` | SVG + PNG @1x/@2x/@3x for all masters | Blender's Python 3.11 |
| `$SP/keyicon_design/composites.py` | all HUD crops (`all` / `held` / `states` / `labels` / `zones` / `full`) | Blender's Python 3.11 |
| `$SP/keyicon_design/composites_live.py` | `live_code_*`, `cmp_*` | Blender's Python 3.11 |
| `$SP/keyicon_design/export_3x.py` | the @3x drop-ins, the crosshair @3x + SVG, all @3x checks | Blender's Python 3.11 |
| `$SP/keyicon_design/rerender_rest.py` | the A + chip full frames and the dark-background byte check (this stage only: the full run was stopped after the A_S/A_L full frames came out byte-identical, because the machine's load was ~900) | Blender's Python 3.11 |
| `$SP/keyicon_design/boards.py` | boards 01–10 (headless Chrome) | Blender's Python 3.11 |
| `$SP/keyicon_design/gen_handoff.py` | §7 and `03_variants.tsv` from 平面视觉's `plan.py` output | `/usr/bin/python3` |
| `$SP/keyicon_design/make_verif.py` | `images/09_*`, `images/10_*` | `/usr/bin/python3` |

Backups of the pre-stage scripts: `composites.v2_pre_int.py`, `design.v2_pre_3x.py`, `boards.v3_pre_int.py`, `composites_live.v1.py`. The 22:16 design set is snapshotted in `$SP/keyicon_figma/design_before/`.
