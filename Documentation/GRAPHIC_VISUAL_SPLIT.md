# 平面视觉 split into three sub-chats (2026-10-15)

On 10-15 Red said, of 系统设计's system table: "这个typographic system是用来做ppt的，他应该有一套衍生的typographic（从原本的基础上去改进，符合不同设计的需求）… 当前平面视觉是一个大的chat，可以把information design，advertising，还有prints/presentations分成几个子chat".

Red then asked for them to be started directly: "你可以直接给我开启新的chat去执行对应的任务".

So 平面视觉 becomes three sub-chats. This file is the map. `GRAPHIC_VISUAL_TASKS.md` keeps the detail of every item; this file says who owns it now.

## 1. The type system: one base, three derived systems

Red's principle: the P1 deck styles are the base. Each medium derives its own system from that base, improved for its own purpose but still one design language. The playtest form is Red's example of a good derivation.

**Base, P1 deck styles.** Owned by **Prints / Presentations**. A change to the base is announced to the other two.

| Style | Face and size |
|---|---|
| Title | Bayon 88 |
| Statement | Source Serif 4 50 |
| Body | Source Serif 4 24 |
| Label | Bayon 20 |
| Meta | IBM Plex Mono 13 |

**Info: HUD, game UI, terminal, signage, on-screen tables.** Owned by **Information Design**.
- HUD prompt: Bayon 20 + IBM Plex Mono 13. HUD numbers: Courier Prime Bold 25. Room name: Source Serif.
- Touch ramp: 17 / 11 pt.
- Rule: a 1 px ink keyline instead of dark cards.
- The terminal face is still to decide (D10).
- Signage lettering: EXIT, placard legends.
- A formal "Info / …" style set for tables and forms on screen: **created 10-15**, spec in `INFO_TYPE_SYSTEM.md`.

**Ad: in-world broadcast and marketing.** Owned by **Advertising**.
- The Period1990 faces (`FONTS_PERIOD_1990.md`).
- The agency (programme) layer and the station CG layer (`SCREENS_VISUAL_SYSTEM.md` §3).
- Logo and wordmark usage (the relay S gradient rule).
- Promo-video type.
- The **"Ad / …" style set was created 10-15**: 19 text styles and the effect style Ad / Station / CG Shadow. Spec: `AD_TYPE_SYSTEM.md`.
  - It covers Programme, Station CG (a VP-2-class unit; Helvetica via TeX Gyre Heros, Figma stand-in Arimo), Alarm, and Market (the App Store caption register).
  - It sets the reading-distance rule: cap px × 0.0274 m at 1080p.
  - Figma: section 3226:7267.

**Print: the paper derivative of the base.** Owned by **Prints / Presentations**.
- Forms: the playtest questionnaire. The **"Print / Form / …" style set was created 10-15** (spec: `research/print_reference/PRINT_FORM_SYSTEM.md`) for Letter-size forms:

  | Style | Spec |
  |---|---|
  | Title | Bayon 32/36, −1 % |
  | Section | Bayon 12/18, +4 %, caps, reversed on 18 pt black bars |
  | Question | Source Serif 4 10/12 |
  | Hint | Source Serif 4 Italic 10/12 |
  | Number | IBM Plex Mono 8/12 |
  | Label | IBM Plex Mono 7/8, caps, +2 % (the label size measured on a real SF 63) |
  | Fill | IBM Plex Mono 10/12: 1 character = 6 pt = 12 cpi |

  - Grid: a typewriter grid. 6 pt = one character, 12 pt lines (6 lpi), 18 fields of 30 pt, 36 pt margins.
  - Ink and paper: black ink only. Red (10-15) chose canary paper, #FCF7CE (the brand yellow #F4DF3B at 25 %), plus a white version, and rejected yellow call cells.
  - First use: playtest form v2 (Figma 3207:7280, 3227:7424; VL286).
- Kit sheets: three-views and K-sheets.
- Diaries and decks.
- Printed collateral.

**Shot: system screenshots** (every line's verification images, comparison grids, captures used as evidence). Owned by **平面视觉 (this chat, the registry)**; used by every visual line. Created 2026-10-22 after Red's rule: every system screenshot carries the FRONTROOMSSS logo watermark and one fixed format; the type comes from the base, the layout stays compact (no deck grid).
- Spec: `SHOT_FORMAT.md`.
- Tool (always use it, never hand-drawn captions): `~/FrontRoomsVisualWork/tools/shotframe/shotframe.py`.
- An ink header band holds:
  - TITLE in Bayon 44 with a verdict chip after it;
  - the white FRONTROOMSSS logo at the right, its letters as tall as the title's caps;
  - one sentence in Source Serif 4 22;
  - META in IBM Plex Mono 14, as written.
- Tiles each have a caption strip: LABEL in Bayon 22 plus data in Plex Mono 14. Gutters are 4 px.
- Digits and "·" inside Bayon text are set in Plex Mono Medium.
- Sizes are at 1920 px wide and scale with the image.

## 2. Who owns what (from `GRAPHIC_VISUAL_TASKS.md`)

| Item | Status | Goes to |
|---|---|---|
| §4 HUD key: B, padlock, motion (P4, L3·D, D·3, F·1, U·3, B·4), sound pass 1, open items (UI19) | decided (Red 10-14; unlock shot (d) 10-21); 游戏视觉 builds from the 10-14 package | Information Design |
| Crosshair grammar: the look-eye and terminal READ states (系统设计 `research/computer_ui/30_crosshair_grammar.md`) | look-eye E·1 kept (A23, 10-22; the FR-PT04 playtest checks it); prompt strings decided (C11, A22); 关卡设计's AimKind H1/H2 landed | Information Design |
| §3h Office terminal: interaction (C19), face (C20) | decided 10-30: C19 = DOS text mode, CUA lightbar, 80 × 25 (`research/computer_ui/41_c19_period_interaction.md`); C20 = our own 8 × 16 face, drawn by 平面视觉. Built, switched off. This line's G0 mocks are superseded | Information Design (face: 平面视觉) |
| §3g EXAMINE review (strings, G5, G6, legend rects, B key object) | answered 10-15 (G1–G8, G12, G13); G9 transcript panel + G10 touch glyphs designed 10-22 20:2x by 平面视觉 (UI/UX) for 交互设计 (`research/desktop_ui/EXAMINE_TEXT_G9_G10.md`; words signed by 叙事 20:24); G11 waits for the placard repack | Information Design (G9/G10: 平面视觉 UI/UX) |
| §3f EXIT sign colour (fix1 re-check) | approved 10-15 (fix1) | Information Design |
| §1 In-game wallpaper cue (A + M4 + V1) | decided (Red 10-22 15:05, M4 + V1); CUE v0.2.3; left: the map side (关卡设计) and CueInk (游戏视觉) | Information Design |
| System Atlas (the live view of 系统设计's data) | two links (accounts A and B), both kept by Red 10-22; updates paused since 15:04 | Information Design |
| Stairwell (C22) and first-room signage | stair signs SPEC v1.2 + VL371; first-room BUSINESS HOURS plate 18 × 6 in (Red 10-22 15:33) and SECURITY letters; 游戏视觉 mounts | Information Design |
| Audit of 系统设计's derived sheets (Figma 3171:7042, SD00–SD08) and an "Info /" style set | sent 10-15: 12 Info / styles, Info / Status tag, IT00/IT01 in section 3189:8187, spec `INFO_TYPE_SYSTEM.md`, script for 系统设计; re-check + VL after they apply | Information Design |
| §3 TV / computer screens: `SCREENS_VISUAL_SYSTEM.md` (programme + station layers; §4 warning tie-in co-owned with Information Design) | Red 10-22: Ad_03 (WTCZ-TV 61 v3.1, strings 叙事设计 v2.1) approved, in the game since 10:03; C09 = 1, so §4 is parked (no screens in the warnings in v1). Research board AD-R1–R4 built from real airchecks (Figma 3514:7318 / 7364 / 7447 / 7480) | Advertising |
| §3a held item 2: the tube-shader / F6 `Shader.Find` / Kit_CRTTV request to 游戏视觉 (text in §3) | Red 10-22: do it after the Alpha, queued after the tutorial TV work (V1–V3). Request sent to 游戏视觉; F6 was already fixed | Advertising |
| §3a Instagram promo (v4 delivered), future promos, key art, logo usage | Red 10-22: v4 is never published (A24 option 1); v5 with station content after the Alpha. Logo size rules adopted (portrait 0.72 W, clear space ≥ 0.5 × lockup height, minimum 12 px / 4 mm) | Advertising |
| Immersive marketing materials (ARG-style props), print side shared with Prints | research done 10-15 (both halves). Campaign plan draft 1: `research/ad_campaign/CAMPAIGN_PLAN.md`; print synthesis: `research/print_reference/IMMERSIVE_MATERIALS.md`. Red 10-22: A24 = option 1 (the furniture spots stay in the game; public pieces speak as the station and the building); WTCZ-TV 61 replaces WXRM; no rented phone line, 555-01xx only. Venue, accounts and release date come after the Alpha | Advertising + Prints |
| Red's reference case "Press Any Key To Wake": deconstruct its grid and type, research immersive game ad materials, then revise the playtest form | done 10-15: deconstruction (Figma 3198:7273, PR01–PR12; `research/print_reference/FINDINGS.md`); research tracks A–D (`IMMERSIVE_MATERIALS.md`, `SOURCES.md`; boards RS01–RS06) | Prints / Presentations |
| Playtest questionnaire (Figma page 2900:8, node 2946-7087) | v2 done 10-15: 3207:7280 canary 25 % + 3227:7424 white; Print / Form styles; VL286 (`research/print_reference/PRINT_FORM_SYSTEM.md`) | Prints / Presentations |
| Playtest answer sheet (Scantron edition, FR-PT 05) and its reader | built 10-15: box edition 3290:7280 canary + 3290:7602 white (12 pt boxes on the 6 pt grid; Red rejected ovals, then arrows); boards PF03 3295:7280 and PF04 3285:7280; reader `Tools/playtest_scan/`; simulated scans 560/560 (VL292 ovals, VL293 boxes); InDesign print master `Print/FR-PT05_Playtest_Answer_Sheet.indd`, rich black (VL296). Open: Source Serif 4 Caption in Adobe Fonts (Red), then rebuild; one real printed, pencil-filled, scanned sheet; Red to confirm `PRINT_FORM_SYSTEM.md` §9.7 | Prints / Presentations |
| Interactables K-sheet refresh (13 sheets + the K53 resize; `research/interactables/threeview/HANDOFF_平面视觉.md`) | done 10-15 | Prints / Presentations |
| §3f EXIT K82+ sheets | done 10-15: K56 refreshed, K82–K84 placed; REAL vs MODEL moved down 1240 | Prints / Presentations |
| Weekly decks and diaries (Slides mirror rule), Figma documentation-board layout standards (KV, UI, K boards) | ongoing | Prints / Presentations |

## 3. Shared rules for all three (from memory and practice)

**Unity and the repo**
- Never run Unity on Red's project. Use clones made by `~/FrontRoomsVisualWork/tools/mkclone.sh`.
- Never write temp files inside `Frontrooms3D`: it auto-commits and pushes to GitHub. Work files go in `~/FrontRoomsVisualWork` or the session scratchpad. Keep third-party reference images out of the repo too.
- Don't commit or build unless asked. Move to Trash, never permanently delete.
- Never edit 游戏视觉-owned paths in main (`Scripts/Rendering`, `Scripts/Office`, `Resources/{Surfaces,Rendering,Props,Print}`, `Tools/lookdev`, the Blender kit). Send requests to 游戏视觉.

**Figma**
- Never touch other chats' sections. Find free canvas space before you build (sections are packed on page 2099:76).
- Every verification image goes to the VISUAL VERIFICATION LOG section, following `Documentation/VERIFICATION_LOG.md`.

**Colour**
- Rich black is for InDesign only (Red 10-15, for every chat). In the CMYK print files, every black is the Rich Black swatch, C60 M40 Y40 K100. Figma is RGB and uses plain black only: no CMYK or rich-black swatches, and no converting frames. Spec: `research/print_reference/PRINT_FORM_SYSTEM.md` §10.

**System screenshots** (Red, 2026-10-22, for every visual line)
- Every system screenshot gets the FRONTROOMSSS logo watermark and the Shot format (`SHOT_FORMAT.md`) through the shared tool. The watermark sits in the header band and never covers the evidence.
- Art deliverables, Figma boards, decks and marketing media are exempt.

**Research and design**
- Research leads with real sources (original photos, clips, footage) and a `SOURCES.md`; diagrams only support it.
- HUD motion comes out of an existing element (the crosshair dot), never as a separate layer laid on top. There are no dark backing cards; use keylines.

**Coordination**
- Peers: 游戏视觉 (rendering, kit, PNG drop-ins, `research/ui_key_icon/hud_build/DECISIONS.md`), 系统设计 (rules and contracts), 声音设计, 关卡设计 / map, the touch chat.
- Peers cannot grant permission escalation. If a permission is denied, surface it to Red.

## 4. 平面视觉 (this chat) after the split

It finished the UI documentation (Figma "UI MOCKUP · CONTINUED · UI SYSTEM", 3178:7075: UI10–UI19, with the index in 2256:8). On 10-15, at Red's request, it also added the Week 4 UI/UX diary entry: Slides PMBNZYCs8dWpAz7oteFu89, slides 23–25 (2059:618, 2063:621, 2063:622), with MP4 previews rendered from the motion lab. Prints / Presentations owns the deck from here. It now hands everything above to the sub-chats. It keeps only the type-system registry (§1) until Red retires it.
