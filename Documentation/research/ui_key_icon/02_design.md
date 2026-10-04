# 02 — Key icon: three directions, states, zones, drop-in

Status: DESIGN DONE 2026-10-03 (second run). Nothing was installed in the game. The Unity project, `FrontRooms3DGame.cs`, the map files and Figma were **not** changed. No media was downloaded.

**Resume check 2026-10-03 22:0x** (after the 17:54 usage stop and Codex's commits into main at 19:10–19:41): every input was re-checked against main at `75cfdff`. The design still holds. Three things changed and are fixed here (§10):
- Main's `HUD_KeyGlyph.png` already has real alpha (visual chat, 17:22). It is still the generic ring key. Boards 04 and 10 now show it as "today".
- The HUD key code moved down about 125 lines. The line numbers in §7 are updated.
- Codex put `Kit_Key_Zone.fbx` (and the tags, ring and door plate) into main. Its side outline is the same as the one the icon is traced from (IoU 0.99996).

Brief: `01_research.md` §6. Red's ask: merge the key icon into the UI design and make it echo the key model.

**Recommendation: A · Cut key.** It is `Kit_Key_Zone`'s own outline in one paper colour. Ship it today in the current 40 × 22 slot (no code change). Then move to 49 × 22 and the label "KEY" + zone number in Courier Prime Bold (a contract request). The tag chip is an option once zones have tag data. B suits 2x screens. C is not used.

---

## 0. Files

All paths are under `design/`.

| What | Where |
|---|---|
| 10 review boards, 1920 × 1080, deck grammar | `01_overview.png` … `10_recommendation.png` |
| Vector masters (33) | `svg/` — 1x units, one `<path>` per layer, named layers |
| PNG renders (99), real alpha | `png/<name>@1x.png`, `@2x.png`, `@3x.png` (@3x added in the Figma stage for the touch track) |
| HUD composites on game frames (188) | `hud/` — `<dir>_held_<bg>_<1x/2x>.png`, `<dir>_state_<state>_<bg>_1x.png`, `<dir>_zone_…`, `label_…`, `<dir>_fullframe_<bg>.jpg`; resume pass: `live_code_<bg>_1x.png` (main's live glyph + today's label) and `cmp_<live/dropin/target>_wallpaper_1x.png` (the three steps on one frame) |
| **Drop-in (recommended, NOT installed)** | `dropin/slot_40x22/` and `dropin/slot_49x22/` (§6), each now with `HUD_KeyGlyph@3x.png`; `dropin/crosshair/` (touch @3x crosshair + SVG) |
| Verification image (resume) | `../images/08_main_fbx_vs_trace.png`: the trace source render, main's FBX outline, and the 15 px where they differ (red) |

Board list:

| # | Board | Shows |
|---|---|---|
| 01 | Overview | A, B, C as vectors + the same HUD row on a lit and a dark frame |
| 02 | A · traced | model render → trace overlay → vector master at one scale; 1x / 2x on lit and dark; 1x pixels × 8 |
| 03 | A · in the HUD | 4 game frames at 1:1, 2x crops, 4 states × lit / dark |
| 04 | A · label | "LEVEL 0 KEY" (today's default text) vs "KEY 14" in Bayon, Plex Mono, Courier Prime Bold; today's code label with the glyph live in main |
| 05 | A · option · tag chip | the three tag models vs the chips; 3 shapes × 3 colours in the HUD |
| 06 | B · key on its tag | model assembly (key + ring + tag, one scale) vs vector; 3 × 3 zone grid; 1x / 2x |
| 07 | B · in the HUD | as 03, full 48 px form (2x crops shown at 72 % so the meta line is not cut) |
| 08 | C · sign plate | model key + door number plate vs vector on its pixel grid; plates in 4 colours |
| 09 | C · in the HUD | as 03 |
| 10 | Recommended | full 1080p frame; three steps on one frame (main today → drop-in → target); drop-in files; contract request |

Scripts (scratchpad, re-runnable with Blender's Python 3.11 + numpy):
- `keyicon_design/design.py`: SVG + PNG for all directions.
- `composites.py`: the HUD crops.
- `boards.py`: the boards (HTML → headless Chrome).
- `iconlib.py`: rasteriser, PNG io and a TrueType reader for the real HUD fonts.
- `geometry_mm.json`: the key, tag and plate outlines dumped from read-only copies of the G3 Blender modules.
- Resume pass: `composites_live.py` (crops with main's live glyph) and `keyicon_resume/verify_main_fbx.py` + `bounds_main.py` (main's FBX against the trace).

---

## 1. What the icon is traced from

**The model.** `Kit_Key_Zone` (`Tools/Blender/frontrooms_kit/assets/interact_key.py` + `interact_key_common.py`; geometry unchanged since the research). I checked the live modules against the copies used here: only a placement helper (`orient`) was added.
- Outline: 58 × 26 mm, aspect 2.23 : 1. A rounded-square paddle bow (30 × 26 mm, R 8) with a Ø 4.8 mm hole.
- A 3.9 mm shoulder step on the top edge.
- A 25 × 8.6 mm blade with 6 cuts on top (0.58–2.1 mm deep) and a tip bevel from 21.5 mm.

**The renders.** The model renders on boards 02, 05, 06 and 08 are orthographic Blender renders of the built FBX at a known scale (20 px/mm): key side view, the three tags, the ring and the door number plate. The icon is drawn from the same outline numbers, so the trace overlay on board 02 lines up exactly.

**Checked against main (22:0x).** Codex copied the built kit into main (`8ef5b64`, from `proj_int`, built 16:45). I rasterised main's `Kit_Key_Zone.fbx` in the same side view and compared it with the render the icon was traced from (`images/08_main_fbx_vs_trace.png`):

| File in main | Result |
|---|---|
| `Kit_Key_Zone.fbx` | 58.0 × 26.0 × 2.2 mm, 2,184 tris. IoU **0.99996**: 15 px of 374,705 differ, all on the anti-aliased edge |
| `Kit_Key_Zone_Nickel.fbx` | same outline, IoU 0.99997 |
| `Kit_KeyTag_Rect / Round / Long`, `Kit_KeyRing`, `Kit_DoorNumberPlate` | bounds identical to the renders on boards 05, 06, 08 (to 0.01 mm) |
| `interact_key*.py`, `interact_door_number_plate.py` | unchanged since 12:56 (`ef061ae`) |

So the echo holds in main. The interactables rebuild (G2/G3) was not finished. If it changes the key outline later, re-dump `geometry_mm.json` and re-run `design.py`, `composites.py` and `boards.py`: the icon is built from those numbers.

**View.** Side view, tip right, cuts up. This is how the key enters the lock in the unlock shot (`10_spec.md` §3.4). The tip points at the label.

**Two exaggerations only** (research §4.2):
- Cuts × 2.5 deep. At true depth they are 0.5–1.8 px at 1x and vanish.
- Hole × 1.5. At true size it is 4 px and fills in with anti-aliasing.

**Hinting.** Every structural edge is drawn on the 2x grid at an even value, so at 1x it lands on a whole pixel.

---

## 2. The three directions

### A · Cut key (recommended)

| | A-L (target) | A-S (today's slot) |
|---|---|---|
| 1x / 2x size | **49 × 22** / 98 × 44 | **40 × 22** / 80 × 44 (key 40 × 18, centred) |
| Scale | 0.845 px/mm | 0.69 px/mm |
| Bow | 25 × 22 px, R 7 | 21 × 18 px |
| Shoulder step | 4 px on the top edge | 3 px |
| Blade | 21 × 7 px | 17 × 6 px |
| Cuts | 6, model order, 1.2–4.4 px deep, 40° flanks | 1.0–3.6 px |
| Hole | Ø 6 px | Ø 5 px |
| Colour | paper `#F4F1E8`, one fill, hole as negative space | same |

- **Echo:** the bow, step, blade, cut order and tip bevel are the model's (board 02, dashed line = true model).
- **Fit:** one row of 22 px, like the crosshair dot and the HUD type. No new colour on screen.
- **Identity:** the zone number goes in the label (§3). The world carries the colour (tag, door plate).
- **Contrast** of the paper key, measured on the pixels under the 49 × 22 rect:

  | Frame | Contrast |
  |---|---|
  | lit low rooms (`#827042`) | **3.9–4.3 : 1** |
  | wallpaper and door | 7.8–8.9 : 1 |
  | office | 7.8–9.4 : 1 |
  | dark cell | 12.9–13.4 : 1 |

  The worst case passes WCAG 1.4.11 (3 : 1). It equals the Bayon label beside it, so the icon is never weaker than its text.

### A + tag chip (option, board 05)

- The zone's tag as a small solid chip on the **bow side**, where the real tag hangs off the ring. Gap 4 px, then the A-L key.
- True relative tag sizes at 0.42 px/mm (+ tab):
  - rect 24 × 12 px; whole glyph 77 × 22;
  - round Ø 16 px; whole glyph 69 × 22;
  - long 32 × 9 px; whole glyph 85 × 22.
- Tab and hole × 1.3 so the hole stays open.
- 1 px paper rim, zone-colour fill. Rim vs fill: red 8.5 : 1, blue 11.7 : 1.
- The rim carries the contrast against the scene. The fill carries identity only: tag red and blue are 1.2–2.5 : 1 on our frames, so colour is never the only cue. The white tag reads as a white shape, so it relies on its outline.
- No number on the chip (digits would be 6 px). The number is in the label.
- **Needs data the map does not have yet** (§7).

### B · Key on its tag (board 06–07)

- `Kit_Key_Zone` + `Kit_KeyRing` (Ø 25 mm) + `Kit_KeyTag_*`, all at 0.69 px/mm. This is the pickup shot's last frame (`interaction_audit/10` §3.1).
- Glyph: **72 × 48** (1x), 144 × 96 (2x).
- Tag colour inside a 1 px paper rim; paper insert; the number typed in Courier Prime at the model's ink height.
- Digit height at 1x: rect 10.2 px, long 7.9 px, round 7.7 px. **Only the rect tag reads easily at 1080p.**
- Needs the full panel (Figma HUD / Key 2256:161): 4 px yellow rule, 14, glyph, 14, name Bayon 20, meta Plex Mono 13. So the panel grows from 22 to 48 px.
- Strongest echo of the tag and the door plate. But it adds the only saturated colours to a quiet HUD and needs the 48 px panel.

### C · Sign plate (board 08–09)

- Our key in the 1974 AIGA/DOT grammar (public domain; on US public signs through 1990):
  - rounded-square bow, R = 1/3 of its height;
  - the shoulder step;
  - three even 90° V cuts on the top edge;
  - a straight tip bevel.
- On a 40 × 20 px plate, R 2: the door number plate's 2 : 1.
- Glyph 40 × 22. The key is about 28 × 12 px.
- Plate colours:
  - default `#141414` at 90 % (the HUD card colour);
  - red and blue zone plates;
  - white plates with an ink key `#1E1D1A`.
- Contrast is safe on every frame (the plate brings its own ground). But the key shrinks to 28 px and drifts from the model (3 cuts, not 6).
- A filled box next to Bayon type reads as a badge or a keycap (`Overlay / Key chip` 2256:191).
- ISO 7001's lockers symbol (2007) is not period, so it is not used.

### Verdict

| | A | A + chip | B | C |
|---|---|---|---|---|
| Echo of the key model | **exact outline** | exact + tag shape | exact + ring + tag | redrawn (3 cuts) |
| Echo of the tag / door plate | number in the label | number + shape + colour | number + shape + colour | colour (plate) |
| Fits the 22 px row | yes | yes | no (48 px) | yes |
| New colours on the HUD | none | 1 small chip | tag colours + yellow rule | plate colours |
| Works today without new map data | **yes** | no | no | yes |
| Worst contrast | 3.9 : 1 | 3.9 : 1 (rim) | 3.9 : 1 (rim) | plate |

---

## 3. The label in the HUD face (board 04)

- The label is Bayon 20, paper, 14 px after the glyph (`FrontRooms3DGame.cs`: `Key label` at x 54, 110 × 22, MiddleLeft).
- The default text is "LEVEL 0 KEY". At runtime, `KeyLabel(zone)` writes the zone name + " KEY": "LEVEL 0 / THE MAZE KEY", "LEVEL 0 / LOW ROOMS KEY", "LEVEL 4 / OFFICE KEY".
- **Bayon draws 1 like I and 0 like O.** "KEY 14" reads "KEY I4". "LEVEL 0" already reads "LEVEL O".

| Option | Result |
|---|---|
| all Bayon | "KEY I4": ambiguous |
| Bayon KEY + Plex Mono 20 px | readable, but the Regular cut is too light next to Bayon |
| **Bayon KEY + Courier Prime Bold 25 px** | readable; the same face as the typed tag insert (`Prop_KeyTagNo`), so the label quotes the tag. **Recommended.** |

- Sizes are matched to Bayon 20's cap height (14.28 px), then rounded to whole sizes, because Unity's legacy `Text` only takes an int `fontSize` (Figma stage, 2026-10-03): Courier Prime Bold **25** (cap 14.49 px), Plex Mono **20** (cap 13.96 px). The composites and boards use these sizes.
- Unity's legacy `Text` cannot switch fonts inside one string. The number needs its own Text element.
- `UiFont()` gives Bayon to every element whose name contains "Key". So the number element needs another name or an explicit font.
- Courier Prime is in `Assets/Fonts/Period1990/`, not in `Resources/`. The HUD needs a reference to it.
- **Until zones have a fixed tag number**, keep today's zone-name label.
- **Warning:** the room meta's "ZONE 06" is `zonesVisited.Count`, a visit counter, not an ID. If the key says "KEY 14" while the meta says "ZONE 06", the player sees two numbers for one zone. The fixed tag number should replace the counter or be clearly different.

---

## 4. States (boards 03, 07, 09)

Rule: brightness says how much the key matters **here**.

| State | Today in code | Design |
|---|---|---|
| **Held, this zone** | panel shown (`map.HasKeyFor(zone.id)`); glyph = the generic ring key, real alpha since 17:22 | paper glyph + "KEY 14" |
| **Held, other zone** | panel hidden | glyph at 40 %, label muted `#BDBAB0` at 70 %, meta Plex Mono 13 "NOT THIS ZONE" (B: "OPENS ZONE 14 ONLY", no yellow rule) |
| **No key** | panel hidden; door prompt "LOCKED  ·  NEEDS THIS ZONE'S KEY" (Bayon) | panel stays hidden. At a locked door the prompt shows the **outline** key (1.25 px stroke, paper 70 %) + "LOCKED" (muted Bayon) + Plex Mono "NEEDS KEY 14", as in Figma UI06. B and the chip option use A's outline in the prompt: their 48 px or coloured glyphs are too heavy beside the crosshair. |
| **Used** | no such state: keys are never spent (`keysHeld` is a set; a held zone key opens every door of that zone) | muted glyph + muted label + "DOOR OPENED". Only needed if keys become single-use (RE2R's check-mark idea). Otherwise keep "held". |

Also drawn: the today label vs the target label on the same frames (board 04), and full 1080p frames with the real room typography for every direction (`hud/*_fullframe_*.jpg`):
- room meta: "ZONE 06  /  TIER 1  /  STANDARD  2.9 M" (format from `FrontRooms3DGame.cs` line ~1970 in main at `75cfdff`);
- room names: from `ZoneName()`;
- the crosshair: today's 16 px dot (`HUD_Crosshair.png`, 64 px sprite in a 32 px rect).

---

## 5. Zone variants

- The model's identity system is **number + tag colour + tag shape** (`10_spec.md` §4.1): 3 shapes × red / blue / white.
- The key itself has one bitting for every zone. It also has a nickel master-key variant, which the HUD does not need.
- Drawn as below.

| Direction | Zone variants |
|---|---|
| A | label number only (all zones share one glyph) |
| A + chip | 9 glyphs `Achip_<rect/round/long>_<red/blue/white>_held` + HUD crops `hud/Achip_zone_*` (lit, dark, 1x, 2x) |
| B | 9 glyphs `B_<shape>_<colour>_held` (numbers 14 / 37 / 08) + HUD crops `hud/B_zone_*` |
| C | 4 plates `C_<dark/red/blue/white>_held` + HUD crops `hud/C_zone_*` |

---

## 6. Drop-in asset (recommended direction A; NOT installed)

| Folder | File | Size | Use |
|---|---|---|---|
| `dropin/slot_40x22/` | `HUD_KeyGlyph.png` | 80 × 44 | replace `Assets/Resources/UI/HUD_KeyGlyph.png` **today**; draws in the existing 40 × 22 rect, no code change. Main's file already has real alpha (visual chat F2, 17:22, commit `df4cb03`), but it is still the generic ring key: this file changes the shape to our key (board 10, bottom row) |
| | `HUD_KeyGlyph@1x.png` | 40 × 22 | the exact 1080p pixels (reference) |
| | `HUD_KeyGlyph.svg` | 40 × 22 | vector master |
| `dropin/slot_49x22/` | `HUD_KeyGlyph.png` | 98 × 44 | **after** the contract change (rect 49 × 22) |
| | `HUD_KeyGlyph@1x.png` | 49 × 22 | the exact 1080p pixels (reference) |
| | `HUD_KeyGlyph.svg` | 49 × 22 | vector master |

Checks (run on the files in `dropin/`):
- **Real alpha:** all four corners are 0. 41–50 % of pixels are 0, 46–54 % are 255, 5 % are edge pixels.
- **One RGB value** (`#F4F1E8`) in every pixel, transparent ones included. Bilinear filtering can never pull in a dark fringe.
- **2x → 1x:** at 1080p, the 2x file drawn in its 1x rect is a 2 : 1 bilinear sample, which averages 2 × 2 texels. That average matches the 1x file with a mean error of 0.03 % and a worst pixel of 2 %. So one 2x file serves:
  - 1080p (exact);
  - 4K and Retina 2x (native);
  - 1440p (1.5 : 1, slightly soft).

Import settings for the PNG:
- sRGB on, Alpha Is Transparency on;
- Bilinear, no mipmaps;
- **Compression None** (today's `.meta` says Normal; `UI_SHARPNESS.md` imports the brand PNG uncompressed);
- Max Size 128.

The sprite is made in code by `LoadHudSprite()` from the whole texture, so the PNG's size sets nothing but resolution.

Owner: `Resources/UI/HUD_KeyGlyph.png` is the visual chat's file. Red reviews Figma first; only then does anything get copied in. When it lands: overwrite the PNG only and keep main's `.meta` (GUID `eb266a1345aeb45f69bf39f9c6d34fb4`); change only its import fields (compression None, max size 128).

---

## 7. Contract request for the map chat (exact proposal; not applied)

`Assets/Scripts/FrontRooms3DGame.cs` in main at `75cfdff`: `BuildHud()` key panel at lines 1828–1838, `UpdateMapPlay` key update at 1995–2002, `KeyLabel()` at 2035, room meta at 1970. (Codex's commits changed only the title logo code in this file, so the key code is the same; it sits about 125 lines lower than when the research read it.)
1. `Key glyph` size `new Vector2(40, 22)` → `new Vector2(49, 22)`. `Key label` position `new Vector2(54, 0)` → `new Vector2(63, 0)`. Panel `HUD / Key` width 147 → wide enough for the label (≥ 200 for "KEY 14", ≥ 300 for today's zone-name label).
2. When zones have a fixed tag number: add a second Text after `Key label` for the number:
   - Courier Prime Bold, size 25, paper;
   - 6 px after "KEY";
   - named without "Key" so `UiFont()` does not set Bayon.
   - Load the font from a `Resources` copy or a serialized reference.
   - `KeyLabel(zone)` → "KEY" + the number.
3. Map data (`FrontRoomsMap.cs` `ZoneInfo`): a fixed tag number (00–99), tag colour (red / blue / white) and tag shape (rect / round / long) per zone. Use the same source that dresses the 3D key, its tag and the door number plate, so the HUD can never disagree with the world. Decide what the room meta's "ZONE nn" (a visit counter today) shows next to it.
4. Optional states:
   - "other zone": show the panel muted when `KeysHeld > 0` and `!HasKeyFor(zone.id)` (`FrontRoomsMapWorld.cs` lines 57 and 790).
   - Locked prompt: `Describe()` (`FrontRoomsMapWorld.cs` line 2160) "LOCKED  ·  NEEDS THIS ZONE'S KEY" → "LOCKED" + a meta line "NEEDS KEY nn", with the outline glyph (an Image beside the prompt Text).
5. Tag chip (option): an Image left of the key glyph. One rim sprite per shape + one fill sprite tinted with the zone colour. Label x moves by chip width + 4.

Dependency: the in-game key is still a placeholder cube (`FrontRoomsMapWorld.cs` `SpawnKey`, line 2804: 0.32 × 0.12 × 0.12 m, `keyGlow` material; re-checked in main at 22:0x). `Kit_Key_Zone.fbx` is now in main, but nothing spawns it yet. The icon echoes `Kit_Key_Zone`. The echo only works in play once the kit key replaces the cube.

---

## 8. Checks against the house rules

- **Never brass, never yellow, never a keycap box:**
  - A, A + chip and B: met.
  - C is a filled plate, not an outlined square, but it still reads close to a keycap. That is one reason it is not recommended.
- **Era:** the HUD fonts are a separate system (`FONTS_PERIOD_1990.md`). The object drawn is period: a cut brass blank and a plastic tag with a typed paper insert. Courier Prime is a revival of Courier (1955), the period's typewriter face; `FONTS_PERIOD_1990.md` lists it for typed tags. C uses the 1974 AIGA/DOT grammar, not ISO 7001 (2007).
- **HUD system:** paper `#F4F1E8`, muted `#BDBAB0`, Bayon 20 label, Plex Mono 13 meta, 72 px margin, bottom-left at y 940. The yellow rule is used only in B's full form (it is the threat accent).
- **Composites:** blended in linear light like Unity's overlay canvas (no post on the HUD). Backgrounds are in-engine captures:
  - `room_visuals/images/room_map-l0-low_wide.jpg`;
  - `room_map-l0-standard_wide.jpg`;
  - `room_map-office_wide.jpg`;
  - audit frame `59_door_key_open_t0000ms.png`.
- **Fonts:** the real files, `Assets/Resources/Fonts/` (Bayon, IBM Plex Mono, Source Serif 4) and `Assets/Fonts/Period1990/CourierPrime/`.

## 9. Open questions for Red

1. Direction: A (recommended), A + chip, B or C?
2. Label: "KEY 14" with the number in Courier Prime Bold, once zones have numbers? Until then, keep the zone-name label?
3. Tag chip in the HUD: yes or no?
4. States: show "other zone" muted, or keep the panel hidden as today? Do keys stay valid (no "used" state)?
5. Drop-in: may the visual chat copy `dropin/slot_40x22/HUD_KeyGlyph.png` into `Resources/UI` now? The white box is already gone in main; this changes the generic ring key into our traced key. No code change.

---

## 10. Resume check against main (2026-10-03 22:0x)

The workflow stopped at 17:54 while board 10 was being written. Codex then committed into main (19:10–19:41). What I checked, and what it means for this design:

| Item | Main now | Effect here |
|---|---|---|
| Partial work in `design/` | all 10 boards, 33 SVG, 66 PNG, 182 HUD crops, 6 drop-in files were complete. Checked after this pass: 253 PNG (CRC + full inflate), 35 SVG (XML parse), 23 JPG (end marker), 0 bad; drop-in alpha corners 0, one RGB value, 2x→1x alpha error 0.04 % mean | nothing redone |
| `Assets/Resources/UI/HUD_KeyGlyph.png` | the generic ring key with real alpha since 17:22 (`df4cb03`, visual chat F2). Codex did not touch it. Import still Compression Normal | "today" was shown as our key on board 04 and only in words on board 10. **Fixed:** board 04 "TODAY IN CODE" and board 10's bottom row now use main's real file (`hud/live_code_*`, `hud/cmp_*`). §6 and question 5 updated |
| `FrontRooms3DGame.cs` | Codex range touched only the title logo relay (`a7b0dbb`). HUD key code, fonts, sizes and colours unchanged | line numbers in §4 and §7 updated |
| `Kit_Key_Zone` (+ `_Nickel`), tags, ring, door plate FBX | in main since `8ef5b64` (from `proj_int`) | same outline as the trace (§1): the echo holds |
| `interact_key*.py` | unchanged since 12:56 | none |
| HUD fonts (`Resources/Fonts`, `Fonts/Period1990`) | unchanged | none |
| In-game key / `ZoneInfo` | still the placeholder cube; `ZoneInfo` still has no tag number, colour or shape | §7 items 3 and dependency still stand |
| Codex audit (`research/codex_audit/20_findings.md`) | the folder exists since 22:14 but is still empty (no `00_main_state.md`, no `20_findings.md`, no `contracts/`) | no finding to resolve for this workflow yet; the next stage re-checks before it finishes |
| Board 07 | its 2x crops cut the meta line ("OPENS DOORS OUT OF TH…") | **Fixed:** shown at 72 % and labelled so |
| Board 04 | "TODAY · LEVEL 0 KEY" looked like a glyph label | renamed "LEVEL 0 KEY · TODAY'S DEFAULT TEXT" |

Still open for the Figma hand-off stage (not this stage, per `figma_target.md`): integer font sizes in `composites.py` (Courier Prime Bold 25, Plex numerals 20) with a re-render, and the @3x exports for the touch track.

**For 平面视觉's re-sync** (`figma_target.md` update 22:0x: they built section 2532:4038 from our 17:09 SVGs, 17:10 `composites.py` and 17:16 PNGs; `Tools/figma/hud_key/`):
- (a) Constants: **none changed.** `composites.py` was not edited (entry points, names and output stems are as they synced).
- (b) Files: **6 added** in `design/hud/`, all made by a new, separate script `composites_live.py` (so their `plan.py` does not see them): `live_code_lit_1x.png`, `live_code_dark_1x.png`, `live_code_wallpaper_1x.png`, `cmp_live_wallpaper_1x.png`, `cmp_dropin_wallpaper_1x.png`, `cmp_target_wallpaper_1x.png`. The `live_*` and `cmp_live_*` crops use main's live `Assets/Resources/UI/HUD_KeyGlyph.png`, which is not one of our SVG masters; a twin would need that PNG as an image fill. `cmp_dropin_*` uses `dropin/slot_40x22/HUD_KeyGlyph@1x.png` (= `png/A_S_held@1x.png`, same bytes). Same layout constants as `composites.py` (row top 940, label baseline 958, glyph at x 72, label 14 px after it; crop box x 40–440 × y 920–980 for `live_code_*`, x 40–640 × y 880–1000 for `cmp_*`). Changed: boards 04, 07 and 10 (not part of their sync). Added: `images/08_main_fbx_vs_trace.png`. Removed: none.
- (c) SVG masters: **none changed.**

Verification images from this pass: `images/08_main_fbx_vs_trace.png` (main FBX vs trace, verdict PASS), `design/04_A_label.png` and `design/10_recommendation.png` (re-rendered). The Figma VISUAL VERIFICATION LOG section is not on the canvas yet (`VERIFICATION_LOG.md`: section id —), so they are listed here and in the run report.

---

## 11. Figma stage update (2026-10-03 23:0x)

The Figma stage built no key-HUD block (平面视觉 owns it, `2532:4038`). It wrote the hand-off `03_figma.md` and changed only these:
- **Whole type sizes** (Unity legacy `Text` takes an int): Courier Prime Bold 24.63 → 25, Plex Mono numerals 20.45 → 20. 113 of 188 HUD crops and the 10 boards were re-rendered. §3 above is updated.
- **@3x** for the touch track: `png/*@3x.png` (33), `dropin/slot_*/HUD_KeyGlyph@3x.png`, `dropin/crosshair/HUD_Crosshair@3x.png` (192 × 192, Ø 96) + `HUD_Crosshair.svg`.
- **Dark background** copied to `design/bg/` (it lived only in the scratchpad).
- SVG masters: unchanged.
- Verification: VL075–VL077 in the Figma VISUAL VERIFICATION LOG.
- **Open:** the missing-state outlines A_L, A_S and C clip half their stroke at the glyph box (`03_figma.md` §10.1).

