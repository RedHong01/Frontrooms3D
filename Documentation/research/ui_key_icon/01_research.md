# 01 — Key icon: research (game key icons, 1990 key graphics, our HUD, our key model)

Status: DONE 2026-10-03 (research only). Nothing in the game, the Unity project or Figma was changed. No media was downloaded.

Red's request (2026-10-03 ~12:05): merge the key icon's graphic design into the UI design, research it, and make it echo the key model.

**Files in this folder**
- `01_research.md`: this report.
- `images/`: our own evidence (renders, HUD crops, a silhouette test). Provenance is in `SOURCES.md`.
- `SOURCES.md`: the ledger (URL, read date, licence) for every source cited here, and the on-disk media this report reuses.
- `media_candidates.md`: real screenshots and photos we still want. Nothing is downloaded until Red approves the batch.
- `figma_target.md`: where the KEY ICON block goes in Figma (written by the visual chat).

**Tags**
- **[read]**: I read the page.
- **[search]**: the claim comes from a search-engine summary only (the page refused me or would not load). Treat it as UNVERIFIED until a screenshot backs it.
- **[on disk]**: a file already in `Research/week01`, `Research/week02` or the project.
- **[code]** / **[Figma]**: read from the project code or the Figma file (read-only).
- **[test]**: measured by me in this run (`images/01`, `images/02`).
- **UNVERIFIED**: from memory or inference, not checked against a source.

---

## 0. Answers in one table

| Question | Answer | § |
|---|---|---|
| What is wrong with today's icon? | Four things. (1) The live PNG has no alpha, so the game draws a **white 40 × 22 box**. (2) The drawing is a generic web-UI key (ring bow, two teeth hanging down), not our key: our key has a **rounded-square paddle bow, six cuts on top and a bevelled tip**. (3) The label is fixed to "LEVEL 0 KEY", also in Office zones. (4) The runtime panel has neither the 4 px yellow rule nor the meta line that the Figma HUD / Key component has. | 1 |
| What should the icon be traced from? | `Kit_Key_Zone`'s real outline (58 × 26 mm), side view, tip right, cuts up. Exaggerate only two features: the **cuts ×3** and the **ring hole ×1.5**. At true depth the cuts are 1.45 px at 40 px wide, so they vanish. | 2, 4 |
| How do games tie the icon to the model? | One symbol on three surfaces: **the key, the lock or door, and the UI**. RE2 remake uses card suits (key, door, map). RE7 uses animals (key bow, door). RE 1996 uses engraved emblems. Silent Hill 2 names keys by room. Dead Space remake uses clearance levels on the door and the suit. | 3 |
| Our symbol? | Already specified in the kit: **zone number (00–99) + tag colour (red / blue / white) + tag shape (rect / round / long)**. It is on the tag and on the locked door's number plate. The HUD should be the third surface: "KEY 14" in the label, plus the tag's shape and colour next to the key. | 2.2, 6 |
| Filled or outline? Mono or colour? | **Filled**, one colour (paper `#F4F1E8`), like the 1974 AIGA/DOT key pictogram and like the flat brass plate of the model. Colour only for the tag chip, and only inside a paper edge: the tag plastics measure 1.2–2.5:1 against the HUD backgrounds (WCAG 1.4.11 asks 3:1). | 4.4, 5.4 |
| Brass-coloured icon? | **No.** Brass `#B08A4A` is 1.69:1 on lit Level 0 carpet: it disappears, the same camouflage problem `03` found for the 3D key. | 4.2 |
| Pixel size? | Keep the 22 px row height. The key's 2.23:1 aspect fills about **48 × 22** (or 40 × 18 inside today's 40 × 22 rect). Author a vector master. At 1440p the HUD scales by 1.333 and at 4K by 2, so a 40 × 22 PNG is resampled everywhere except 1080p. | 4.3 |
| Period language? | US public signs of the period: the 1974 AIGA/DOT symbols (public domain) draw a key as a **solid silhouette, round bow, off-centre hole, V-cut bitting, angled tip**. Plastic key tags with paper inserts and typed numbers. Numbered hook boards and key cabinets. ISO 7001's lockers symbol dates from 2007, so it is **not** period. | 5 |
| Who changes what? | Figma first (visual chat, inside UI MOCKUP 2256:8 per `figma_target.md`). Red reviews. Then the PNG or SVG (visual chat) and a contract request to the map chat for `FrontRooms3DGame.cs` (panel size, label text, rule, meta line). | 6.4 |

---

## 1. What we have today

### 1.1 The runtime HUD key panel [code]

All values are from `Assets/Scripts/FrontRooms3DGame.cs`, read 2026-10-03 12:1x (the map chat edits this file, so line numbers drift).

| Item | Value | Where |
|---|---|---|
| Canvas | Screen Space Overlay, `pixelPerfect = true`; CanvasScaler Scale With Screen Size, reference 1920 × 1080, match 0.5 | `BuildHud`, ~1410–1411 |
| Panel `HUD / Key` | anchor bottom-left (0, 0), position (72, 118), size 147 × 22; its own CanvasGroup fades with the gameplay HUD | ~1462–1463 |
| Glyph `Key glyph` | Image 40 × 22 at (0, 0), sprite `Resources/UI/HUD_KeyGlyph`, `preserveAspect`, colour white | ~1464–1466 |
| Label `Key label` | at (54, 0), 110 × 22, **Bayon 20**, colour paper `#F4F1E8`, text fixed **"LEVEL 0 KEY"**, overflow on | ~1467–1471 |
| Font rule | `UiFont()` gives Bayon to any element whose name contains "Key". So the crosshair prompt (named "Key prompt") is Bayon too. That matches `UI_SYSTEM.md` (prompts are Bayon). | ~1142–1147 |
| When shown | `play && !inStartRooms && map.HasKeyFor(zone.id)`: only while you hold **this zone's** key. So the HUD never lists keys: one slot is enough. | ~1620 |
| Pickup feedback | `OnKeyTaken` flashes "KEY / OPENS THIS ZONE'S DOORS" in the bottom context card (Source Serif 24) for 3 s | ~828–832 |
| Caught screen | counts "N KEYS" | ~1549 |
| Palette | paper `#F4F1E8`, muted `#BDBAB0`, accent `#F4DF3B`, card `rgba(20,20,20,.9)` | ~1411–1414 |

Texture import of `HUD_KeyGlyph.png` (`.meta`): Sprite, bilinear, no mipmaps, **compression Normal**. `UI_SHARPNESS.md` imports the brand PNG uncompressed "for clean alpha edges"; the glyph should get the same.

**The white box.** The PNG's alpha was flattened (task `F2` in `VISUAL_CHAT_TASKS.md`). A fixed-alpha copy is in the scratchpad (`hud/HUD_KeyGlyph.png`), waiting on Red. `images/03_runtime_key_panel_x4.png` shows the box at 4×, cropped from audit frame 58.
- The colour fringe in that crop is **not** in the game. The audit harness switched the overlay canvas to Screen Space Camera for its `_hud` frames, so the post stack (chromatic aberration 0.06, grain, vignette) hit the HUD (`interaction_audit/05_in_engine_evidence.md` lines 78–81). In the game the overlay has no post.

**The crosshair** is now a plain white dot, 16 px at 1080p (Red's call, `F2`). The key icon should match its colour logic: white/paper, no yellow.

### 1.2 The Figma UI MOCKUP (2256:8) [Figma, read-only]

The HUD / Key component (symbol `2256:161`, 291 × 48) is the design target the runtime never fully got. See `images/04_figma_ui05_key_panel_x3.png`.

| Part | Value |
|---|---|
| Layout | auto-layout row, gap 14, centred |
| Rule | 4 × 48 yellow `#F4DF3B` (the HUD's only persistent accent; `UI_SYSTEM.md`: "A 4 px yellow rule is the only persistent threat accent") |
| Glyph | frame `key glyph` 40 × 22. Vector: ring circle r 7.5, stroke 3.5, centre (10, 11); shaft 21 × 4.5 at y 9; teeth 4 × 7 and 4 × 5 hanging down. Paper `#F4F1E8`. Same drawing as the PNG. |
| Name | "LEVEL 0 KEY", text style **P1/Display/Label** = Bayon 20/20, tracking 3 % (0.6 px), paper |
| Meta | "OPENS DOORS OUT OF THIS ZONE", text style **P1/Header/Meta** = IBM Plex Mono 13/16, muted `#BDBAB0` |

Other key moments in the mockup:
- **UI01 · UI inventory:** Key = NEW, "bottom left while you hold the zone key"; Take key = NEW ("E, when the key is under the crosshair"); Locked door = NEW ("greyed, and names the key you are missing").
- **UI04 / UI05 · Play:** the panel bottom-left at (72, 940) on the 72 px margin. UI04 uses the small 147 × 22 form; UI05 the full 291 × 48 form with rule and meta.
- **UI06 · Prompts:** a Bayon keycap "E" in a 2 px outlined box, then "TAKE KEY". The locked prompt is greyed `#BDBAB0` with Plex Mono "NEEDS THE LEVEL 0 KEY" under it.
- **Naming clash:** `Overlay / Key chip` (2256:191) is a **keyboard** keycap chip used on pause/settings/caught. The door-key icon must never look like a keycap (an outlined square), and the Figma component could be renamed "Keycap chip" to avoid confusion (a Figma-only suggestion).

### 1.3 The HUD's type and colour system (what the icon must sit in)

From `Documentation/UI_SYSTEM.md` [read] and the code:
- Bayon 20 for labels and prompts, Plex Mono 13 for meta, Source Serif 4 for room names and context. These UI fonts are **a separate system from the 1990 period fonts** (`FONTS_PERIOD_1990.md`: "The UI fonts … are a separate system and are not changed by this kit"). So Courier Prime / VT323 belong on the **3D tag**, not on the HUD label.
- 72 px outer margin, 24 px rhythm, 12 columns of 126 px with 24 px gutters.
- Typography floats over the world with **no card** (only the context card and notes have dark containers).
- One yellow accent (the rule, threat chips). Everything else is paper or muted.

Measured backgrounds behind the key panel (1 × 1 area average of a 200 × 60 crop) [test]:
- lit Level 0 carpet `#79693C` (audit frame 56);
- dark cell `#25200F` (audit frame 58);
- the Figma mockup's dark floor `#17140C`.

---

## 2. Our key model (what the icon must echo)

### 2.1 `Kit_Key_Zone` [code: `Tools/Blender/frontrooms_kit/assets/interact_key.py`, `interact_key_common.py`; spec `interactables/10_spec.md` §3.2–3.3]

See `images/05_model_kit_key_zone_side.jpg` (side, nickel above / brass below) and `images/07_model_kit_key_zone_34.jpg` (3/4).

| Feature | Model value | What it means for a 2D outline |
|---|---|---|
| Overall | 58 × 26 mm, 2.2 mm thick; aspect **2.23 : 1** | a long, low silhouette |
| Bow | rounded-square "paddle" 30 × 26 mm, corner R 8 mm; generic, no maker's outline | the key's main mass: 52 % of the length. A ring bow (today's glyph) is wrong. |
| Ring hole | Ø 4.8 mm, centre 6.5 mm from the bow's far end, on the centreline | small: 18 % of the bow height |
| Shoulder | neck top 3.9 mm above the blade top; bottom almost flush | a step on the **top** edge where the bow meets the blade |
| Blade | 25 mm long, 8.6 mm tall | 43 % of the length |
| Bitting | **6 cuts on the top edge** (US pins-up), depths 0.96–2.1 mm, 50° flanks, 3.8 mm pitch; one bitting for every zone key | shallow saw-tooth on top |
| Tip | bevel from the top edge down to 0.5 mm above the axis | a pointed nose that drops toward the spine |
| Material | brass `Prop_Brass` (sRGB 0.69/0.54/0.29 = `#B08A4A`); nickel variant for a master key | see §4.2: not a HUD colour |

The unlock shot inserts the key **teeth up** (`10_spec.md` §3.4). A HUD drawing in side view, **tip right, cuts up** is the same object as the one the player sees at the lock.

### 2.2 Ring, tags and the zone identity [spec `10_spec.md` §4.1, §2.4; `03` §2.5; previews]

See `images/06_model_key_tags_front.jpg`.

- **Split ring** Ø 25 mm, flat wire 1.6 × 0.9 mm, chrome. Thin: about 1 px at HUD sizes.
- **Plastic tag with a paper insert** (平面视觉's binding era note): `Kit_KeyTag_Rect` 57 × 29 mm, `_Round` Ø 38, `_Long` 76 × 22. Each has a ring tab (R 5.5 mm, Ø 5 hole) and a recessed window with a typed number in **Courier Prime** (`Prop_KeyTagNo`, 10 × 10 atlas "00"–"99").
- Colours: `Prop_PlasticRed` `#8C0F0D`, `Prop_PlasticBlue` `#0F2973`, `Prop_PlasticWhite` `#FFFFFF` (sRGB material values; green optional). No yellow, orange or manila (they melt into Level 0).
- **Identity = number + colour + shape.** 3 shapes × 3 colours = 9 identities; the number makes each key unique. Two zones that share a door never share both colour and shape.
- **The door repeats it:** `Kit_DoorNumberPlate` (100 × 50 mm) on the locked door, in the tag's colour family, with the same typed number.
- The spec already asks for the HUD label to read **"KEY 14"** instead of "LEVEL 0 KEY" (`10_spec.md` §4.1; `03` §2.5).

A tag is the same size as the key (57 mm vs 58 mm). In 3D it is the readable part at 3–8 m (`03` §1.1 table: a 0.10 m fob is recognised to 8.6 m; the key alone to 5.2 m).

### 2.3 How the 3D key reaches the HUD [`interaction_audit/10_audit_report.md` §3.1]

The proposed pickup shot (0.95 s): the key lifts, flies to a held pose 0.35 m ahead at the lower right, **turns its paper tag to the lens**, the tag swings twice, the key drops out of frame (pocketed), and at 0.95 s **the HUD key panel fades in**. So the HUD icon is the last frame of a motion the player just watched. If the icon draws the same key and the same tag, the hand-off reads as one object. If it draws a generic key, the link breaks.

---

## 3. How games draw key items (primary references)

Screenshots for every row are listed in `media_candidates.md` (none on disk yet). Two on-disk frames are relevant: Dark Deception's HUD map counter (`Research/week01/assets/clips/DD_2_counter.jpg`) and Lethal Company's scan labels and "Grab : [E]" prompt (`LC_1_scan.jpg`) [on disk].

| Game | The key object | How the UI shows it | How icon, model and door tie together | Source |
|---|---|---|---|---|
| **Resident Evil (1996)** | Sword, Armor, Shield, Helmet keys; the Shield Key is a large bronze key with a shield carved on one side | 2D item icon in an 8-slot inventory; examine view | the emblem on the key's bow is the emblem on the door; the item text names the engraving | evilresource Shield Key [read]; RE wiki door file [search] |
| **Resident Evil 2 remake (2019)** | Spade, Diamond, Heart, Club keys: the bow **is** the suit shape | 3D-rendered item icon in the grid; examine rotates the model; when every door for a key is open, a **red check mark** appears on the icon and the key can be discarded | locked doors carry the suit symbol; the map marks them | evilresource Spade Key [read]; check mark: gamewatcher, gamerevolution, RE wiki [search]; btxx.org essay on the discard prompt [read] |
| **Resident Evil 7 (2017)** | Scorpion, Crow, Snake keys: animal-headed keys | first-person; items can be rotated and examined; grid inventory of rendered icons | the same animal is **nailed to the door** (two-headed crow + inverted triangle = the Crow Key's bow) | gosunoob guide [read]; evilresource Crow Key [read]; Wikipedia RE7 [search] |
| **Resident Evil Village (2021)** | Iron Insignia Key, Courtyard Key and more | examine/rotate in the inventory | insignia key ↔ "Iron Insignia" doors on the map | gamewatcher, shacknews [search] |
| **Silent Hill 2 (2001) / remake (2024)** | keys named by place: "Apartment 212 Key", "2F Hallway Key", "Lyne House Key" (marked with the owner's name) | 2001: 3D item models in the inventory (UNVERIFIED, memory) | **identity by number/name**, the same principle as our zone number | silenthillmemories remake walkthrough [read]; Lyne House Key wiki [search] |
| **Alien: Isolation (2014)** | keycards, entry codes, the access tuner, the welding torch | lo-fi 1979 rule: nothing a 1979 film set could not have built; information kept "in world" (motion tracker) | doors show what they need on the door panel | hudsandguis (Jono Yuen, 2014-06-03) [read]; Wikipedia [search] |
| **Outlast (2013)** | no key inventory; the only equipped item is the camcorder | camcorder overlay; notes and documents | the world carries the state | Wikipedia via audit `04` S5 [read there]; giantbomb/wiki [search] |
| **Amnesia: The Dark Descent (2010)** | Wine Cellar Key, Machine Room Key, Rusty Key | Tab inventory of item icons; a key is used on its door | each key names its door | amnesia wiki [search; pages returned HTTP 402] |
| **Half-Life (1998)** | no key items | retinal scanners and keypads worked by NPCs (guards, scientists) | the lock is an NPC puzzle, not an icon | TWHL / Valve dev wiki [search] |
| **Dead Space remake (2023)** | security **clearance levels 1–3** plus Master Override replace key items | clearance shown on the suit/HUD and on the door lock | one number on the door and on the player | PC Gamer, Prima [search]; diegetic UI: audit `04` S10/S11 |
| **Escape the Backrooms (2022)** | physical keys (Level 0 silver key; four Floor 2 keys in wardrobes) | keys do not take inventory space | key ↔ named door | playnews, gameplay.tips [search] |
| **Dark Deception (our primary ref)** | shards, not keys | a handheld map device counts what is left (`DD_2_counter.jpg`) | the counter is a device in the hand | [on disk] |
| **Lethal Company** | scrap | scan labels give name + value; the prompt is "Grab : [E]" (`LC_1_scan.jpg`) | label sits on the object | [on disk] |

### 3.1 Patterns

- **P1. One symbol on three surfaces.** Every key system that teaches itself puts one symbol on the key, on the lock or door, and on the UI (RE 1996, RE2R, RE7, Dead Space remake). The player matches shapes, not words.
- **P2. Inventory icons are the model; HUD indicators are pictograms.** RE games draw the inventory icon as a render of the 3D model and let you rotate it. Persistent HUD indicators (when a game has one) are flat glyphs. FrontRooms has no inventory screen, so the HUD glyph is the only 2D picture of the key: it must carry the model's silhouette by itself.
- **P3. Identity by number.** Silent Hill 2 names keys by room; facilities in 1990 did the same with tag numbers (§5.1). Our "KEY 14" is in that line.
- **P4. Lifecycle feedback.** RE2R's check mark tells you a key is spent. Our keys are per zone and the panel only shows while the key is valid here, so the lifecycle is "appears on pickup, hides when you leave the zone". A spent state is not needed.
- **P5. Minimal-HUD horror puts state in the world.** Alien: Isolation, Outlast and Dead Space keep the screen clean and show lock state on the door. Our kit already does that (number plate, visible deadbolt). The HUD key should stay small and quiet: one row, paper colour.
- **P6. Prompt grammar.** RE: examine the object. Lethal Company: name label + "Grab : [E]". Our mockup: keycap "E" + Bayon verb "TAKE KEY". With identity, the verb can carry the number: "TAKE KEY 14"; the locked prompt: "LOCKED · NEEDS KEY 14".

---

## 4. What makes a key icon read at 20–40 px

### 4.1 The silhouette test [test]

I traced five drawings and rasterised them at HUD sizes with 8 × 8 supersampling, in paper `#F4F1E8` on the two measured backgrounds. Script: scratchpad `keyicon/silhouette_test.py` (Blender 4.3 + numpy; it imports read-only copies of the key modules).

- `images/01_silhouette_sizes.png`: rows A–E top to bottom. Columns: sizes 20, 24, 32, 40, 48 px wide (row E: 40, 48, 64, 80, 96), each on lit (left) and dark (right). Each tile is magnified ×6 with the true-size copy under it.
- `images/02_silhouette_at_40x22.png`: the 40 × 22 column only (row E at 80 × 22).

| Row | Drawing |
|---|---|
| A | today's glyph (Figma 2256:153 / the PNG) |
| B | `Kit_Key_Zone` true outline (`bow_outline()` + `top_profile()`) |
| C | B with the cuts ×3 deep and the ring hole ×1.5 |
| D | the 1974 AIGA/DOT "Baggage lockers" key (key part only; geometry read from the public-domain SVG path) |
| E | C + split ring + `Kit_KeyTag_Rect` in red, side by side (tag left) |

### 4.2 Findings in numbers

Feature sizes of the **true** model at each icon width [test]:

| Key width | px per mm | Deepest cut (2.1 mm) | Blade (8.6 mm) | Ring hole (4.8 mm) |
|---|---|---|---|---|
| 20 px | 0.34 | 0.72 px | 3.0 px | 1.7 px |
| 24 px | 0.41 | 0.87 px | 3.6 px | 2.0 px |
| 32 px | 0.55 | 1.16 px | 4.7 px | 2.7 px |
| 40 px | 0.69 | 1.45 px | 5.9 px | 3.3 px |
| 48 px | 0.83 | 1.74 px | 7.1 px | 4.0 px |
| 64 px | 1.10 | 2.32 px | 9.5 px | 5.3 px |
| 96 px | 1.66 | 3.48 px | 14.2 px | 7.9 px |

1. **The true cuts are invisible below ~64 px.** At 40 px the deepest cut is 1.45 px: row B reads as a paddle with a flat stick. A detail needs about 2 px to be seen and 8 px to be recognised (Johnson's criteria, as used in `03` §1.1).
2. **×3 cuts fix it.** Row C at 40 px shows six notches of 2–4 px: it reads as a cut key and still traces the model. The 1974 DOT key does the same: its notches cut up to 33 % into a blade that is half the bow's height; our deepest cut is 24 % of a blade that is a third of the bow's height.
3. **The hole needs ×1.5.** At 40 px the true hole is 3.3 px and fills in with anti-aliasing; at ×1.5 it is 5 px and stays open. DOT's hole is 26 % of its bow height; ours is 18 %.
4. **The bow is the recognition feature.** Rows B–D read as "key" mainly from bow + neck + long blade. The paddle bow is our model's signature; a ring bow (row A) is the generic web/emoji key.
5. **Filled beats outline at this size.** Row A's ring is a 3.5 px stroke. At 20–24 px it drops to ~2 px and the inside closes up. Filled shapes (B–D) survive down to 20 px. GitHub's Octicons use 1.5 px strokes but draw separate 16 and 24 px versions to keep them clean [read]; one filled silhouette is simpler for a single-size HUD icon.
6. **The full assembly (row E) does not fit a 22 px row.** The tag is as big as the key, so key + ring + tag needs about 80–96 × 22 px, and the ring is 1 px. The tag dominates and the key shrinks to 18 px. Use the assembly only at 2× sizes (pickup card, pause), and use a small **tag chip** at 1×.

Contrast of candidate icon colours against the measured HUD backgrounds (WCAG relative luminance) [test]:

| Colour | on lit `#79693C` | on dark `#25200F` | Verdict |
|---|---|---|---|
| paper `#F4F1E8` | 4.77 : 1 | 14.39 : 1 | use for the key |
| white `#FFFFFF` (white tag) | 5.39 : 1 | 16.25 : 1 | ok |
| accent `#F4DF3B` | 3.97 : 1 | 11.97 : 1 | reserved for the rule (threat accent) |
| chrome `#D8D8D8` | 3.78 : 1 | 11.40 : 1 | ok, but not a UI token |
| muted `#BDBAB0` | 2.77 : 1 | 8.37 : 1 | fails on lit carpet: meta text only, not icon |
| brass `#B08A4A` | **1.69 : 1** | 5.09 : 1 | **fails** on lit Level 0 |
| tag red `#8C0F0D` | **1.78 : 1** | **1.69 : 1** | **fails** both |
| tag blue `#0F2973` | **2.46 : 1** | **1.23 : 1** | **fails** both |

WCAG 2.1 SC 1.4.11 asks 3:1 for graphics needed to understand the UI [read]. So the zone colour can live in the HUD only as a fill **inside a paper edge** (the paper edge carries the contrast; the fill carries the identity), and never as the only cue (number and shape go with it, as the kit already rules).

### 4.3 Pixel hinting and scaling

- The canvas scales by **screen height / 1080** on 16:9 screens (match 0.5 → √(W/1920 · H/1080)). So a 40 × 22 icon is drawn at 26.7 × 14.7 (720p), 28.4 × 15.6 (1366 × 768), **40 × 22 (1080p)**, 53.3 × 29.3 (1440p) and 80 × 44 (4K).
- Pixel-hinting a 40 × 22 PNG helps only at 1080p, Red's launch resolution (`UI_SHARPNESS.md`). Everywhere else the sprite is resampled.
- So: **draw a vector master on a 1080p pixel grid** (edges on whole pixels at 1×, as Octicons [read] and Lucide [search] advise), then either bake PNGs at 1×/1.5×/2× or ship an SVG. The project already imports SVGs (`Resources/Brand/*.svg`, svgType 3, target resolution 1080) for the vector logo [code]; a uGUI Image still needs a sprite, so the SVG route needs a sprite-type import or a 2× PNG. That is a build decision for later.
- Import the PNG uncompressed, like the brand logo.
- Light-on-dark icons look heavier than their geometry (irradiation). Google's Material Symbols has a "grade" axis for exactly this; negative grade reduces glare on dark backgrounds [read]. In practice: on a dark scene, keep strokes and gaps 0.5 px more open than on paper.

### 4.4 Rules that follow

- **R-shape:** trace `Kit_Key_Zone` (paddle bow, top shoulder step, 6 top cuts, bevelled tip). Exaggerate cuts ×3 and the hole ×1.5. Simplify to 3–4 notches only below 24 px.
- **R-fill:** one solid fill in paper `#F4F1E8`, the hole as negative space, no outline, no gradient, no brass.
- **R-orientation:** side view, tip right (pointing at the label), cuts up (as at the lock).
- **R-tag:** the zone's tag is a separate small chip (its real outline: rect / round / long with the tab) in a paper edge, filled with the zone colour; the number goes in the Bayon label, not inside a 22 px chip (a 15 mm typed digit would be ~6 px).
- **R-no keycap:** never put the key inside an outlined square; that shape means "keyboard key" in this HUD.

---

## 5. 1990 key graphics (the period language)

The HUD is not era-locked (its fonts are modern UI faces). But the icon draws a 1990 object, and its graphic grammar can come from 1990 US buildings.

### 5.1 Key tags

- **Plastic ID tags with a paper insert under a clear window.** Lucky Line Products has been family owned since 1961; its current ID tags still have a removable paper insert under a clear cover, in several colours for colour coding [search; modern catalogue, timeless form]. This is the form of our `Kit_KeyTag_*` (平面视觉's binding era note).
- **Motel fobs (1955–1980).** The Henry Ford holds two [read]:
  - Sea Breeze Motel key, 1955–1970: reddish-brown and cream plastic fob, 2.125 × 4.8125 in, room number "11" printed under the motel name, with a "drop in any mail box" return line.
  - Tivoli Motel key, 1955–1980: green and white plastic fob, 1.5625 in wide, 3.5 in overall.
  - Other museums list similar objects: the Albuquerque Museum's Trade Winds Motor Hotel key with an orange diamond tag (c. 1959) and the Heinz History Center's Penn-Irwin Motel keys with white elliptical fobs and hand-painted red numbers [search].
- **Film:** the Overlook's "ROOM No 237" key fob in *The Shining* (1980) is the period's most famous number fob; the replica is engraved plastic, about 1¼ × 3½ in [search].
- What these teach the icon: **a big flat coloured shape + a number** is how a 1955–1990 key says "which one". The key itself is generic.

### 5.2 Key cabinets and boards

- Facility keys hang on numbered hooks in a steel wall cabinet with an index card, or on a hook board with painted or taped numbers (`02` §9; Telkee trademark 1928 [search, in `02`]).
- "Tag code = sign code" was the period system: the cabinet indexes keys by room number (`02` §8.2). Our door number plate does the same.
- Our kit has both hosts (`Kit_KeyBoard`, `Kit_KeyCabinet`).

### 5.3 Key blanks and duplicates

- A brass duplicate cut from a hardware-store blank is period-true (`02` §8.1). A 1970s Mister Minit duplicate on a Dominion B41B blank is on Commons (CC BY 4.0) [read metadata].
- Trade signs: mid-century key-shaped metal signs advertised "keys made" in hardware stores; key boards with hundreds of blanks covered shop walls [search: Locksmith Ledger "Blankety Blanks"]. An oversized key silhouette was the period shop sign for "keys here".
- Commons has modern photos of blanks (anodised colour blanks and plastic-head blanks, 2013, CC BY-SA 3.0) that show colour-coded keys [read metadata]. Whether coloured aluminium blanks were common in US stores in 1990 is UNVERIFIED.
- No trademarks: no maker's bow outline, logo or keyway name on the model or the icon (`02` §8.1).

### 5.4 Period pictograms: AIGA/DOT 1974–79 and ISO 7001

- **AIGA/DOT Symbol Signs:** 34 symbols in 1974, 16 more in 1979, by Roger Cook and Don Shanosky for the US Department of Transportation. As US government works they are public domain [read: Wikipedia, AIGA]. They became the off-the-shelf symbols of US sign companies, so they hung in 1990 US public buildings.
- **"Baggage Lockers" (1974 set) contains a key** [read: SVG path]. Its geometry (key part only):
  - a **solid round bow** Ø 119 units with an **off-centre hole** Ø 31 (on the far side of the bow, on its centreline);
  - a straight blade with a **saw-tooth bitting on the bottom edge** (3 V notches between 4 teeth, cut 24–33 % into the blade height) and an **angled tip** that falls from the top edge;
  - overall about 2.5 : 1; **filled silhouette, hole as negative space, no outline**; drawn above a locker box with a suitcase.
  - This is a modern cylinder key, not a skeleton key. It is the period US public-sign grammar for "key": exactly the grammar rule R-fill asks for.
- **ISO 7001:** first edition October 1980 [read: Wikipedia]. Its lockers symbol (PF 013, a bag in a rectangle with a key in the gap) was registered in **2007** [read], so it is **not** period. Use AIGA/DOT, not ISO, as the 1990 reference.
- Lineage: Cook and Shanosky surveyed existing pictograms, including Tokyo airport and the 1972 Munich Olympics [read: AIGA/Wikipedia summary].

### 5.5 Period on-screen text

The camcorder and VHS date stamps in the Backrooms canon are the period's own "HUD": blocky character-generator digits (ETB "MAR. 07 1991", Kane "06/19/1990") [on disk: `Research/week02/ip-research/stills/ir03_etb_vhs_mar07_1991.jpg`, `ir03_kane_emg_vhs_06-19-1990.jpg`; and `Research/week01/assets/clips/ETB_3_run.jpg`]. 平面视觉's stand-in for that look is VT323 (`FONTS_PERIOD_1990.md`). It belongs to in-world screens and to the tag; our HUD keeps Bayon / Plex Mono.

---

## 6. Brief for the design stage (requirements, not designs)

### 6.1 What the icon must do

1. **Echo the model:** the silhouette is `Kit_Key_Zone`'s, traced, with only the two exaggerations of §4.4. Side by side with `images/05`, a viewer should see the same key.
2. **Echo the tag and the door:** the zone's tag shape and colour as a chip, the zone number in the label ("KEY 14"); the same number and colour on the door's number plate and in the locked prompt ("NEEDS KEY 14").
3. **Sit in the HUD system:** paper fill, Bayon label (P1/Display/Label), Plex Mono meta (P1/Header/Meta), the 4 px yellow rule only if the panel uses the full form, 72 px margin, 22 px row.
4. **Read at 20–40 px** on lit and dark scenes (§4.2), and scale cleanly at 1440p and 4K (§4.3).
5. **Never** a keycap box, never brass, never yellow (yellow is the threat accent and is "in the walls" in Level 0).

### 6.2 Three directions worth drawing (for the next stage)

| Dir | Idea | Strength | Risk |
|---|---|---|---|
| D1 · Model silhouette | `Kit_Key_Zone` traced, cuts ×3, hole ×1.5, solid paper; tag chip beside it | closest echo of the 3D key; reads at 20 px | the paddle bow is less "iconic" than a ring; needs the tag chip for identity |
| D2 · Key on its tag | the real assembly (ring + tag + key) as one mark; 1× compact (tag chip overlapping the bow), 2× full with the typed number | echoes the pickup shot (tag to the lens) and the door plate | crowded at 22 px (row E); colour contrast needs the paper edge |
| D3 · 1974 sign grammar | our key redrawn in AIGA/DOT construction: round-cornered geometric bow, even teeth, flat fills, consistent corner radii | period public-sign language; very robust | moves away from the exact model; may read as "signage", not "my key" |

### 6.3 States and sizes to cover

- **1×** HUD row 22 px: icon ~48 × 22 (or 40 × 18 inside the current 40 × 22 rect) + tag chip ~10–12 px + label "KEY 14"; optional meta "OPENS THIS ZONE'S DOORS" (only once keys really open doors: audit F1).
- **2×** (88–128 px): pickup card / pause / caught screen; the full assembly with the Courier Prime number visible.
- **Prompt states:** "E · TAKE KEY 14" (world prompt), "LOCKED · NEEDS KEY 14" (greyed, with the chip).
- **Zone variants:** 3 shapes × red / blue / white chips (+ green if the slot is added).
- **Backgrounds:** test on `#79693C`, `#25200F` and the Office scene.

### 6.4 Who changes what (after Red approves)

- Figma: the KEY ICON block in UI MOCKUP 2256:8 (or the fallback section), per `figma_target.md`. Visual chat.
- `Assets/Resources/UI/HUD_KeyGlyph.png` (or a new SVG): visual chat, with real alpha and no compression.
- `FrontRooms3DGame.cs` (panel size 40 → 48, the tag chip Image, label "KEY nn", the rule and meta line, the "TAKE KEY nn" prompt): **map chat's file → a contract request**, not an edit.
- Tag colours in the HUD: the HUD reads the zone's tag colour/shape from the same source the kit's `DressKey` uses (map → facade), so the icon can never disagree with the 3D tag.

---

## 7. Open questions for Red

1. **Label wording:** "KEY 14" (spec), "ZONE 14 KEY", or the zone name (audit F16)? The number matches the tag and the door plate.
2. **Tag chip in the HUD:** yes (identity on screen) or no (key only, identity only in the world)?
3. **Panel form:** the small row (UI04: icon + label) or the full form (UI05: yellow rule + meta line)? The rule is the threat accent, so the small row may be cleaner.
4. **Direction:** D1, D2 or D3 (§6.2), after the Figma frames.
5. **Media batch:** approve the downloads in `media_candidates.md` (priority A first) so the Figma research frames can lead with real screenshots.

---

## 8. Sources

Every URL, read date and licence is in `SOURCES.md`. Main ones:
- Games: evilresource.com (RE1 Shield Key, RE2R Spade Key, RE7 Crow Key); gosunoob RE7 key guide (Stefan Djakovic, 2017-01-31); btxx.org "This key is useless now. Discard?" (2024-08-28); silenthillmemories.net SH2 remake walkthrough; hudsandguis.com Alien: Isolation (Jono Yuen, 2014-06-03).
- Period: The Henry Ford, Sea Breeze Motel Key (THF 2011.81.1) and Tivoli Motel Key (THF 2011.82.1); Wikipedia "DOT pictograms" and "ISO 7001"; Commons `Aiga_baggagelockers.svg` (public domain).
- Legibility: W3C WCAG 2.1 SC 1.4.11; GitHub Primer Octicons design guidelines; Google Material Symbols guide; Lucide design principles.
- Project: `interactables/02`, `03`, `10_spec.md`; `interaction_audit/05`, `10`; `UI_SYSTEM.md`; `UI_SHARPNESS.md`; `FONTS_PERIOD_1990.md`; Figma 2256:8.
