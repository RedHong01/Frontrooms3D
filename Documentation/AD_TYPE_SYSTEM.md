# Ad type system (平面视觉 · 广告)

Owner: 平面视觉 · 广告, since the split of 2026-10-15 (`GRAPHIC_VISUAL_SPLIT.md`).

Red's principle: the P1 deck styles are the base, and each medium derives its own system from them. A derived system is improved for its purpose but stays in one design language. Prints / Presentations owns the base; Information Design owns the Info system (`INFO_TYPE_SYSTEM.md`).

This file is the Ad derivative. It covers:
- in-world broadcast: the programme (agency) layer and WXRM's station layer on every in-game screen;
- the alarm slate's layout on those screens;
- out-of-world marketing;
- logo and wordmark usage.

**Figma:** file `0tCbAiVUlrPId3RWd9LRif`, page `2099:76`, section **FRONTROOMS · SCREENS · BROADCAST SYSTEM v2 · STATION + AD TYPE** `3226:7267` (x −1109, y 18920).
- **Slides:**
  - S09 `3227:9136`: the style specimen at 1×;
  - S10 `3227:9028`: WXRM rebuilt;
  - S11 `3227:9067`: off air;
  - S12 `3227:9113`: the dealer tag.
- **Masters (components, 640 × 480 at 1×):** station ID `3227:7273`, ID with slogan `3227:7287`, stand-by `3227:7295`, sign-off `3227:7310`, bars + clock `3227:7322`, dealer tag `3227:7352`.
- **The v1 system** (Ad_02's cards, 2026-10-08) stays in `2800:6096`.
- **Verification:** VL287 `3230:8391`.

**Generator:** `Tools/screens/broadcast.py` holds v1, which is what ships in Ad_02. v3 is in the dev copy `~/FrontRoomsVisualWork/screens_dev/` until Red approves it.
- The dev copy adds the CG renderer, the EG 1-1990 bars and the v3 states.
- `test/check_safe.py` checks every CG line against title safe.

**Research:** `~/FrontRoomsVisualWork/reference/ad_reference/`. Each topic has a NOTES.md with numbered sources:
- `station_1990`: 156 sources;
- `analog_horror`: 83;
- `inworld_ads`;
- `args_channels`: 108.

## 1. Derivation

**Carried over from the base:**
- **Roles.** Display (few big words), Label (spaced caps), Meta (mono: IDs, numbers, tags). In-world, Display becomes the agency headline and price, Label the spaced caps under a logotype, and Meta the station CG. Text has no in-world role: a 1990 spot has no body copy.
- **Colour.** One ink (#0A0A0A), the deck's chip yellow (#F4DF3B, which becomes the price yellow) and cream (#F4F0EC).
- **Colour space.** The Ad system is RGB: broadcast, screens, video and store assets. Figma uses plain black only (#0A0A0A, or #000 for CG edges). Any printed campaign piece belongs to the Print system, where InDesign uses Rich Black (C60 M40 Y40 K100). Red, 10-15: rich black is for InDesign only, never in Figma.
- **The logo,** with its relay S rule (§6).

**Derived:**
- **Faces.** In-world registers use only the Period1990 stand-ins (`FONTS_PERIOD_1990.md`). The base faces are post-2010 designs and would break 1990. Marketing uses only the base faces.
- **Units.** In-world sizes are raster px on the 640 × 480 broadcast master, the generator's unit and Figma's unit at 1×.
- **Edges.** Period conventions replace flat deck type:
  - CG lines get a 2-line black edge and a 4-line hard drop shadow;
  - agency supers get a 2–6 px hard shadow;
  - prices and phone numbers also get a 2–3 px black outline.
- **Boxes.** The HUD's "keylines, not dark cards" rule is for game UI. A 1990 spot puts supers on solid ink bars, so in-world graphics keep the period's boxes.

### 1.1 The raster
- **Master:** 640 × 480, 4:3, square pixels.
- **Safe areas: SMPTE RP 27.3-1989** (approved 29 Mar 1989), with rounded corners:
  - action safe x 32–608, y 24–456, corner radius 115;
  - title safe x 66–574, y 48–432, corner radius 101.
- **Overscan.** Sets of the period overscan about 4–5 % per edge, up to 10 % on a badly adjusted set. Nothing that must be read goes outside title safe; crawls may run edge to edge.
- **Levels.** Colours stay broadcast-legal: bars are 75 %, large fields 75–85 %, and CG colours come from the VP-2's 512 (3 bits a channel).

### 1.2 Reading distance: the rule behind every in-world size
The camera's vertical FOV is 76° (`FrontRooms3DGame.cs:280`), and `Kit_CRTMonitor`'s face is 0.19 m tall for 480 raster lines. On a 1080-line display, one raster px is 0.274 ÷ d display px at d metres. A line reads once its cap height reaches about 10 display px, allowing for the tube's blur.

**Reading distance (m, 1080p) = raster cap height × 0.0274.** It doubles on a 4K display. `Kit_CRTTV`'s larger screen only helps.

| Tier | Raster cap height | Reads from | What may use it |
|---|---|---|---|
| FAR | ≥ 92 px | ≥ 2.5 m | One item per card: the price, the phone number |
| MID | ≥ 48 px | ≥ 1.3 m | Headline, script brand, station numeral |
| NEAR | ≥ 18 px | 0.5–0.9 m, walk-up and EXAMINE | Supers, CG lines, slate lines |
| — | < 18 px | never | Only parts of a logotype, seen as shape |

The floor of 18 is the VP-2's own smallest standard size (Helvetica Medium, 18 lines).

Cap heights per em, measured from the shipped files:

| Face | Cap height / em |
|---|---|
| Anton | 0.860 |
| Archivo Black (and Italic) | 0.688 |
| Yellowtail | 0.729 |
| Michroma | 0.750 |
| TeX Gyre Heros Bold | 0.729 |
| Courier Prime Bold | 0.580 |
| VT323 | 0.560 |

## 2. Programme: the agency layer

Ad_02's settings (Red approved Ad_02 on 10-14), now named as local text styles. Sizes are raster px.

| Style | Face | Size | Track | Edge in the generator | Cap | Reads to |
|---|---|---|---|---|---|---|
| Ad / Programme / Phone | Anton | 132 | 0 | 3 outline + 6 shadow | 113.5 | 3.1 m |
| Ad / Programme / Price | Anton | 124, with the $ at 44 raised to the cap line | 0 | 3 outline + 5 shadow | 106.6 | 2.9 m |
| Ad / Programme / Brand Script | Yellowtail | 84 | 0 | 3 shadow | 61.2 | 1.7 m (read as shape) |
| Ad / Programme / Headline | Anton | 58 | 0 | 3 shadow | 49.9 | 1.4 m |
| Ad / Programme / Lead | Archivo Black Italic | 44 | 0 | 3 shadow | 30.3 | 0.8 m |
| Ad / Programme / Super | Archivo Black Italic | 31 | 0 | 3 shadow, or reversed on an ink bar | 21.3 | 0.6 m |
| Ad / Programme / Support | Archivo Black Italic | 28 | 0 | 2 shadow | 19.3 | 0.5 m |
| Ad / Programme / Flag | Archivo Black | 28 | +4 % | on a flag colour | 19.3 | 0.5 m |
| Ad / Programme / Brand Caps | Archivo Black | 20 | +40 % | none | 13.8 | logotype part only |

**Rules:**
- One FAR item per card.
- Supers sit on a full-width ink bar inside title safe, or carry a hard shadow, never a soft one.
- The $ (and cents, when used) is raised to the cap line at about 35 % of the figure size.
- **FRONT ROOMS FURNITURE's identity** is a 1990 lockup, never the game's logo: the 55° chevron mark (cream / pink / grey bands), the Yellowtail script and Archivo Black FURNITURE spaced +40 %.

## 3. Station: WXRM's CG and its states

### 3.1 How a 1990 UHF independent looked
Research: `station_1990/NOTES.md` §1.

- **The channel number is the brand.** Call letters come second: "TV 38", "Chicago 66", "Boston's movie station". No source shows "UHF" on screen, so WXRM never says it.
- **Legal ID** (47 CFR 73.1201, unchanged 1983–91):
  - the call letters immediately followed by the city of licence. Only the licensee name or the channel number may go between them;
  - visual-only IDs are legal;
  - required at sign-on, at sign-off and hourly at a natural break;
  - "TRI-COUNTY" and "STAY TUNED." sit outside that string. **WXRM has no city of licence yet** (narrative chat).
- **No permanent corner bug.** The US networks adopted them in autumn 1993. **No corner time and temperature** either: in 1990 the time showed only over the off-air bars (WFLD 32, 1988–89). WeatherStar was cable only. The power-off "burn-in ghost of the station bug" is gone for the same reason.
- **Stand-by:** the ID slide or a text card with "TECHNICAL DIFFICULTIES / PLEASE STAND BY", silent or over a music bed, for 20–90 s. Sometimes a few seconds of bars or a frozen bumper come first.
- **Sign-off order:**
  1. the late movie;
  2. PSAs;
  3. Thought For Today;
  4. the sign-off notice (standards line, "mechanically reproduced", home reception, legal ID, return time);
  5. the anthem or "America the Beautiful" over scenic film;
  6. bars with time and date, or carrier off to snow.
- **Dealer tag:** the last 3–7 s of a 30-second spot: store name in the CG face (often in colour), address, town, a 7-digit phone number, then hours.

### 3.2 The character generator
A Chyron VP-2-class unit, the common local CG of the late 1980s.
- **Resolution.** 1,510 positions across × 482 scan lines, so type steps in whole scan lines down the screen.
- **Faces.** Helvetica Medium and Bold, the VP-2's standard faces. TeX Gyre Heros is the shipping stand-in (`FONTS_PERIOD_1990.md`).
- **Edges.** A 2-line black edge plus a 4-line hard drop shadow (Dubner's presets; the VP-2 has border and drop-shadow modes).
- **Colour.** 16 colours from 512. CG white is (219, 219, 219); CG yellow is (219, 219, 36).
- **No smoothing.** A cheap unit has no anti-aliasing. The generator renders at CG resolution and box-filters across only, so scan-line steps stay.
- **VT323 retires from the station layer.** It is a terminal face. The office monitors' face is Information Design's (D10).

### 3.3 Styles
Sizes are cap heights in scan lines.

**Figma stand-in:** Arimo, since Heros and Helvetica can't load in Figma. It is sized to Heros' em (cap 0.729), so line widths match the generator, and Arimo's own caps come out about 6 % shorter. Every CG node carries a 2 px black stroke (OUTSIDE) and the effect style **Ad / Station / CG Shadow**: a hard black shadow 4 px across and 4 px down.

| Style | Generator face | Lines (cap) | Figma size | Track | Reads to |
|---|---|---|---|---|---|
| Ad / Station / ID Numeral | Michroma (Eurostile-type logo face) | 72 px type, cap 54 | 72 | 0 | 1.5 m |
| Ad / Station / ID Call | Michroma | 40 px type, cap 30 | 40 | +8 % | 0.8 m |
| Ad / Station / CG 48 | Heros Bold | 48 | 65.8 | +4 % | 1.3 m |
| Ad / Station / CG 36 | Heros Bold | 36 | 49.4 | +4 % | 1.0 m |
| Ad / Station / CG 30 | Heros Bold | 30 (the VP-2's Bold size) | 41.2 | +5 % | 0.8 m |
| Ad / Station / CG 24 | Heros Regular | 24 | 32.9 | +6 % | 0.66 m |
| Ad / Station / CG 18 | Heros Regular | 18, the floor (the VP-2's Medium size) | 24.7 | +8 % | 0.49 m |

### 3.4 States (v3, in the dev generator and the Figma masters)

| State | Layout |
|---|---|
| Station ID | The 61 ring (the numeral now inside it; Ad_02's ran over it), WXRM-TV, the CG 30 market line |
| Station ID, slogan | The same, with CG 36 "STAY TUNED." in CG yellow |
| Stand-by | The ID slide at 72 %, CG 36 "PLEASE STAND BY" over CG 24 "TECHNICAL DIFFICULTIES" |
| Sign-off | The ID slide at 60 %, the CG 30 notice and a CG 18 line (raised from v1's sub-floor 30 px VT323) |
| Off air | SMPTE EG 1-1990 bars (§3.5) with the call (CG 30), the time (CG 48) and the date (CG 30) keyed in. **The time and date are samples** until the narrative chat writes them |
| Dealer tag | FRONT ROOMS (CG 36) / FURNITURE (CG 24, +20 %) in CG yellow, the address, the phone number (CG 48) and the hours. The store name runs over two lines so it stays inside title safe |
| Power off | Dark glass only |

**Check (VL287):** all 15 CG lines sit inside title safe, measured from the generator's own ink.

### 3.5 Colour bars: SMPTE EG 1-1990
Approved 28 March 1990, revising ECR 1-1978.

**Rows:**

| Row | Height | Pixel rows at 640 × 480 | Contents, left to right |
|---|---|---|---|
| Bars | 67 % | 0–321 | Gray, yellow, cyan, green, magenta, red, blue (75 %). Columns 0 / 91 / 183 / 274 / 366 / 457 / 549 / 640 |
| Reverse castellations | 8 % | 322–359 | Blue, black, magenta, black, cyan, black, gray |
| Bottom | 25 % | 360–479 | −I, 100 % white, +Q and black (each 5/4 bar wide), then the PLUGE in thirds under red, then black. Columns 0 / 114 / 229 / 343 / 457 / 488 / 518 / 549 / 640 |

**Levels, as a calibrated monitor shows them:**
- setup maps to 0;
- the 75 % bars are 191;
- −I is (0, 15, 61) and +Q is (34, 0, 94);
- the PLUGE −4 step sits at black and the +4 step shows at 11.

**Tone:** a steady 1 kHz, or silence. Never next to the alarm slate.

## 4. Alarm slate (layout here; the face is Information Design's signage)

| Style | Face | Size | Track | Treatment | Cap | Reads to |
|---|---|---|---|---|---|---|
| Ad / Alarm / Slate | TeX Gyre Heros Bold, the placard's EGRESS face (Figma: Arimo at Heros' em, 46.0) | 46 | +6 % | EXIT green (199, 242, 148), phosphor bloom | 33.5 | 0.9 m |

From across a room the slate is read as an event: the cut to green. Its words are for walk-up.

For the words to read at 2 m, the cap must reach 73, so size ≈ 100 with one line per card. That is a timing change for Information Design and 系统设计.

**Hard rules for the slate and anything alarm-like** (`analog_horror/NOTES.md` §1.7, §4). The FCC fines simulated alert tones even in obvious fiction: the Olympus Has Fallen trailer, $1.93M; ABC, $395K; ESPN, $147K in 2024.
- **Voice.** The slate speaks only for the building, never for a region or an authority.
  - No EBS/EAS/WEA words: "test", "not a test", "we interrupt", "civil emergency", "take shelter", "issued by".
  - No area lists, expiry times, seals or agency names, real or invented.
  - "STAY TUNED." stays on station IDs only.
- **Look.** No red EAS-style crawl, no WARNING banner, no Civil Defense triangle, no plain white-on-black EBS card, no phone alert look.
- **Sound** (with 声音设计). A period building bell or horn.
  - Never 853 / 960 Hz, 1050 Hz, data-burst chirps or the WEA cadence.
  - Never alarm effects sourced from YouTube.
- **Marketing.** The slate appears only inside a TV in game footage, under a burned-in title and fiction mark. Never full-screen, never the first frame, never a standalone loop.

## 5. Marketing (out of world)

The base faces only, re-derived per format. These settings come from the App Store chat's `~/FrontRoomsVisualWork/appstore/tools/captions.py` (landscape set, 10-14). Every value is base × u:
- u = frame height ÷ 1320 for landscape phone;
- u = frame height ÷ 1600 for 4:3 tablet;
- u = frame height ÷ 1440 for 16:10 Mac (2880 × 1800 gives u = 1.25: headline 148, line 72, tick 120 × 5). Reviewed and approved 2026-10-15.

| Style | Face | Size | Colour | Setting | From the base |
|---|---|---|---|---|---|
| Ad / Market / Headline | Bayon | 118u | paper #F4F1E8 | caps; +3 %; one line, never wrapped | P1 Title (88 at 1080, so 107.6 at 1320) +10 % for phone viewing |
| Ad / Market / Line | Source Serif 4, opsz 20 | 58u | paper #F4F1E8 | one sentence, one line | P1 Statement (50 at 1080, so 61 at 1320) × 0.95 |
| Ad / Market / Tick | rule, 96u × max(3, 4u) | — | #F4DF3B | its top 30u above the headline's ink top | the deck's yellow chip, reduced to a rule |

**Layout:**
- The stack sits at x = 128u.
- The line's ink bottom sits 112u above the frame bottom.
- 26u from the headline's ink bottom to the line's ink top.

**Scrim:** #0E0E0D, alpha 0 down to 52 % of the frame height, then 0.72 × t^1.6 to the bottom. It never runs under the logo.

**No chips.** A solid chip with paper Bayon is the game's UI (TAP TO START).

**Open with Red:** Codex's 10-10 portrait screenshots use Anton + Crimson Pro, the in-world faces, and are probably still live in App Store Connect. The App Store chat and I recommend the landscape set.

**Campaign rules** (`inworld_ads`, `analog_horror` and `args_channels` NOTES):
- **Ordinary in-world spots** (a furniture ad, a station ID) may run unbranded, as Stranger Things 3's 1985 Starcourt Mall spot did. A caption line or end slate ties them to the game.
- **Anything alarm-like, glitched or takeover-like** carries a burned-in "FRONTROOMS · a video game · fiction" mark on every frame. The mark must survive 9:16, 4:5 and 1:1 crops.
- **Each spot sells one real mechanic,** as Portal 2's Investment Opportunity ads and Fallout 4's S.P.E.C.I.A.L. films do.
- **In-world numbers** come only from 555-0100–0199. No 800 or 900 numbers and no URLs on 1990 screens.
- **IP distance.** WXRM is broadcast TV. Stay out of Kane Pixels' camcorder and Async grammar and Local 58's seals. Never use "the Frontrooms" in its Backrooms-lore sense in copy.
  - Kane Pixels' Async story is set partly in 1990, and its episode 25, "Everything Must Go" (18 Aug 2026), has a furniture-store set: Cap'n Clark's Ottoman Empire (印刷与演示, track C §1.7).
  - FRONT ROOMS FURNITURE therefore stays a 1990 *local broadcast* advertiser: agency layer, station CG, dealer tag. Never camcorder or found footage of a store set, and never that episode's title, which the narrative chat had already dropped.
  - **A24's *Backrooms* film campaign already ran our format** (Apr–May 2026; ARGNet, 27 May 2026; 印刷与演示 track A §4.3).
    - It opened with a grainy *local furniture-store TV commercial*, "Cap'n Clark's Ottoman Empire", with a fax icon by its number.
    - Faxing the number printed store flyers dated 17–18 May 1990, then pointed to a 1991 BBS. A24 sells the store's tee.
    - So a 1990 local furniture spot on a TV in a Backrooms-like room now reads as a riff on A24, even though ours came independently from the same Oshkosh furniture-store origin.
    - **Rules until Red decides:**
      - no fax, BBS or retail-flyer endpoints;
      - any phone or paper endpoint speaks for the building (EGRESS, fire safety, building management), never for the store;
      - public pieces lead with WXRM's station identity and the building's voice, not the furniture spots;
      - whether FRONT ROOMS FURNITURE changes category is Red's call, with the narrative chat.

## 6. Logo and wordmark usage

**The relay S rule (Red, 10-08):** each echo S fades from the background into the logo's ink. S1 reaches full ink from a stop at 0.15; S2 reaches 25 %.

**Versions by background (Red's, in Figma):**
- white on black: the game SVGs and icon 2687:10459;
- black on white: 2235:44;
- white on yellow: 2687:10454;
- yellow on black, with an opaque black start: 2687:10464;
- photo or pattern: a transparent start, 2687:10469.

**Video (Red, 10-14):** the 16:9 master's lockup, centred, with height = frame height ÷ 7.5.

**Compositing:** composite in display (gamma) space, as Figma does. Unity's linear-light UI lifts S2 to about 50 %. Video uses signal-space compositing; the promo's `_SignalDecode` is the reference.

**Never:**
- flip the ramp;
- run S2 to full ink;
- use the white version on a light ground;
- add outlines or glows;
- put the game's logo inside the 1990 world.

**Audit, 2026-10-15** (all 17 instances on page 2099:76):
- The questionnaire chip had S2 at 100 %. 系统设计's nine SD sheets are fixed and verified. The playtest form v1 (2946:7087, now in 印刷与演示's section) keeps the old chip as history; v2 (3207:7280 canary, 3227:7424 white) uses Red's light-paper lockup and passes.
- Everything else passes.

**Proposals, not yet Red's:**
- portrait-video lockup width = 0.72 × frame width;
- clear space ≥ 0.5 × lockup height;
- minimum lockup height 12 px on screen, 4 mm in print.

## 7. Open (Red / narrative chat)
- **WXRM's name.** WXRM-LP is a licensed low-power FM station: Cape May Court House, NJ, South Jersey Christian Academy, facility 197239, licensed 2017. No TV station holds WXRM.
  - In-game use is low risk.
  - A public campaign would point players at a real school's station.
  - The real channel 61 of the period was WTIC-TV, Hartford.
- **Strings for the narrative chat:**
  - a city of licence;
  - the off-air time and date;
  - the sign-off notice and Thought For Today;
  - the dealer tag's town.
- **Adoption.** v3 replaces Ad_02's station card and adds the new states. Red approves; then the generator moves into `Tools/screens` and 游戏视觉 swaps the StreamingAssets file.
