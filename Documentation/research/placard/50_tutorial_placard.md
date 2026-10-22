# 50 — The evacuation plan as the tutorial (平面视觉, 2026-10-22)

**Status: SHEET A PICKED (Red, 2026-10-22, in this chat: "A 先上"); art handed off (§12).** **Calls 2–4 answered: Red, 2026-10-22, in the 平面视觉 chat, "全部按推荐"**: the enemy is neither drawn nor named; the v3 wall legend stays off until the wall arrows are live; the strings are now signed by 叙事. Figma TP04 calls 2–4 marked decided. Three sheets (A, B, C) plus a Phase 0 variant of sheet A in Figma. They are called "sheet A/B/C" so they are never confused with 教程设计's tutorial Option A (the station keeps the stair key). Nothing in the game, the texture or `Tools/` has changed. **Strings signed by 叙事 on 2026-10-22** (`research/narrative/40_floors_exit_tutorial_strings.md` §3; §7 below), and sheets A and Phase 0 were regenerated with them; sheets B and C keep the drafts until they are used.

**Ask (Red, 2026-10-22, after the teacher's review):** everything on the start-door evacuation plan should teach the game's main points, briefly: how to play, which enemy to watch for, why you look for keys. No tutorial made of text pop-ups.

**Who owns what.**
- **教程设计** (`local_c160c950…`, docs in `research/tutorial/`): what the sheet must teach and that it is true in play. The content contract is `research/tutorial/10_proposal.md` §5.2 (draft, not yet Red's).
- **平面视觉** (this doc): layout, type, information design and the art generator.
- **关卡设计**: map and game code. Every guarantee in §5 reaches it as an implementation request.
- **游戏视觉**: the prop, the texture pack and the EXAMINE catalog.

**v1 → v2 (07:4x).**
- v1 (07:3x, VL332) was built before 教程设计 existed.
- v2 follows their contract:
  - at most four short imperatives plus a floor number;
  - never RUN or HIDE, never the word "relay";
  - the stair door is locked, and its key is kept at the attendant station, where the only TV plays.
- v1's generator and renders are kept (`tutorial_placard_v1.swift`, `out_v1/`, `images/tp_*`).

**Where things are**
- Figma `0tCbAiVUlrPId3RWd9LRif`, page `2099:76`, section **FRONTROOMS · TUTORIAL PLACARD · OPTIONS** `3459:7280` (x 7897, y 38900):
  - TP00 brief `3459:7281`;
  - TP01 option A `3459:7407`, TP02 option B `3460:7314`, TP03 option C `3460:7411`;
  - TP04 guarantees and Red's calls `3463:7315`, TP05 conventions and sources `3463:7447`;
  - TP06 sheet A, Phase 0 `3471:7309` (no station, no padlock; ships before the stair locks).
- Verification:
  - VL337 `3470:7315` (v2);
  - VL332 `3464:7321` (v1).
- Generator, outside the repo: `~/FrontRoomsVisualWork/placard_tutorial/tutorial_placard.swift <outdir> <A|B|C>`.
  - It uses the shipped placard's sheet (432 × 279 mm), faces (TeX Gyre Heros = Helvetica) and render path (6 px/mm print plus a glow mask).
  - It writes `opt<X>.json` with every text run (cap, measured width) and the rule callouts.
  - `simsheet.swift` makes the nearest-neighbour distance strips.
- Renders: `images/tp2_opt{A,B,C}_{lit_6pxmm,glow_6pxmm,dark_2pxmm,distance_strip}.png`.
- Sources: `SOURCES.md` §7.

## 1. What the sheet must teach (教程设计's curriculum, `10_proposal.md` §1)

| Code | Lesson | On the sheet | Live 10-22 |
|---|---|---|---|
| L0 | Where you are (the placard's own) | YOU ARE HERE at the posting spot; the plan turned so the start door is up | yes |
| L1 | One way on: the EXIT stair | The stair at the route's end, labelled EXIT under its lit sign; the door you came in by drawn shut, NO REENTRY | building (N1, C22, C23) |
| L2 | The stair door is locked; the key is where the TV plays | A padlock on the stair door; the attendant station (desk, TV, key) in the legend; line 4 | design (tutorial option A) |
| L4 | Walk: running is heard | Line 1 `WALK, DO NOT RUN.` | yes (sprint 26 m) |
| L5 | Shut doors slow it | Line 2 `CLOSE DOORS BEHIND YOU.` | yes (S1, D1) |
| L6 | Lamps dipping together means it is near | Line 3 `IF THE LIGHTS DIM, LEAVE THE AREA.` | yes (W1) |
| L9 | The building goes down | `LEVEL n` in the header and title block; a copy on every landing | design (C22) |
| L10 | (later) In the dark the walls point the way | The glow legend, only in a build that ships W6 | clone only |

Not on the sheet:
- L3, look + E: the crosshair prompts teach it.
- L7, being caught ends the run: the first chase teaches it.
- L8: glass.

## 2. Rules every option keeps

1. **Four numbered lines and a floor number, no more** (the Exit 8 limit, `10_proposal.md` §5.2).
2. **Writing rules** (RELAY §9.4, `10_proposal.md` §5.2):
   - never `RUN` or `HIDE`;
   - nothing that says darkness hides you;
   - never the word "relay";
   - no live threat information. Line 3 says "near", never "where".
3. **One symbol, one rule.**
   - The padlock means "a key opens this", and nothing else.
   - The start door is drawn shut with NO REENTRY, never with a padlock: no key opens it.
4. **Turned to the reader** (ISO 23601 §5 j and §7.3 a; Levine 1982; Levine, Marchon and Hanley 1984).
   - Up on the plan is the wall the placard hangs on, so the start door is at the top and north points down.
   - v3 was misaligned on purpose (10_spec §1.6); a tutorial cannot be.
5. **1990 US print:**
   - Heros Bold caps;
   - black, and white on safety red, on off-white;
   - the existing 17 × 11 in snap frame;
   - no brands; the enemy never drawn and never named in the four lines.
6. **Never a radar:** nothing on the sheet moves or knows where the hunter is. There is no route through glass, and windows draw as wall.
7. **Same kit, same glow component:** a new sheet is a texture swap (10_spec D17).

## 3. The three sheets (v2) and Phase 0

All three share the header (`EVACUATION PLAN` and `LEVEL n`) and the legend (YOU ARE HERE; STAIR (EXIT) with the padlock; SECURITY DESK (KEY), formerly ATTENDANT STATION). The four lines sit in a red block (`IN CASE OF ALARM`, 268 × 51 mm) with the title block beside it. They differ only in the plan.

**Sheet A · Route diagram** (TP01, recommended for the next build).
- A typical floor, marked TYPICAL FLOOR · NOT TO SCALE and THIS FLOOR MAY DIFFER FROM PLAN.
- The red route runs from YOU ARE HERE into the attendant station (desk, TV, key, with sound rings), then to STAIR A's padlocked door under a red EXIT.
- It reads as a loop from 1.2 m. In the dark the dot, the route and the stair door glow.

**Sheet B · True plan** (TP02).
- It draws only what every seed guarantees:
  - the start wall and the shut double door;
  - the dot;
  - the map's 3 m cells (bubbles A–F, 1–2).
- The route crosses the match line (SEE SHEET A-3). A drafting break marks NOT TO SCALE, and the padlocked stair door with its EXIT sign waits at the far end.
- The station appears in the legend only.

**Sheet C · Seeded sheet, one per landing** (TP03, after the Alpha).
- Drawn from the floor's own map when the floor starts, in a 7 × 5-cell window round the arrival door:
  - real walls;
  - real locked doors (none on Floor 1 under tutorial option A);
  - the station, if it is in view;
  - a red bearing to STAIR A · EXIT on the sheet's edge, fixed at arrival.
- `LEVEL n` counts the floors. The legend adds LOCKED DOOR.
- The TP03 plan is an illustration, not seed data.

**Sheet A · Phase 0** (TP06; 教程设计 `10_proposal.md` §5.5). Every fix the art alone can make, true in today's game:
- no station and no padlock; the route runs straight to the stair door under its EXIT sign;
- the legend has two symbols (YOU ARE HERE, STAIR (EXIT));
- line 4 reads `EXIT BY STAIR,` / `ONE FLOOR DOWN.`
- It becomes sheet A (TP01) once tutorial Option A lands. It still needs G1 and the C22 stairwell before it ships.

## 4. Type and reading distance (the placard's derived print system)

Derived from the shipped sheet and the EXAMINE readability rule (`research/inspect/10_design.md` §3.4: a cap reads at ≥ 18.6′, 12 px on the 1080p reference).

| Role | Setting | Read in |
|---|---|---|
| Header, floor number | Heros Bold 15 mm caps, +0.6 mm, in a 24 mm band (ISO §6 b: ≥ 19.5 mm band, ≥ 11.7 mm caps on a 279 mm sheet) | walk-up (reads from 0.86 m) |
| The four lines | Heros Bold **5.0 mm** caps, +0.1 mm, white on safety red (5.9:1), numbered, 7.6 mm line pitch | EXAMINE base: 12.75 px (bar 4.7 mm) |
| Legend labels, YOU ARE HERE, EXIT | Heros Bold 5.0 mm caps | EXAMINE base |
| Block head, room labels, NO REENTRY | Heros Bold 3.6–4.4 mm caps, +0.3–0.5 mm | zoom step |
| Fine print: scale, sheet refs, title block | Heros 2.8–3.6 mm | zoom step (5.0 px/mm) |
| Pictograms | 8–40 mm; strokes ≥ 0.6 mm; walls 1.8 mm poché (ISO §6 c, d) | walk-up as shapes; EXAMINE in detail |

**Measured** (VL337; print-path renders, not Unity frames):
- **3 m** (0.23 px/mm): the red block (62 × 12 px), the frame and the route read.
- **1.2 m** (0.58 px/mm): the header, the drawn plan and the block's shape read; the words do not.
- **EXAMINE base:** every line reads in all three options.
- **Phones** (angle × 0.45): the four lines need the pinch step, which the EXAMINE design already provides.

**Glow mask** (power failure): YOU ARE HERE, the route, and the stair door with its padlock and EXIT (C: also the padlock and STAIR A · EXIT). The red block does not glow. k stays 0.158 (Red 10-21).

## 5. What the game must guarantee (implementation requests to 关卡设计)

| ID | Guarantee | Owner | A | B | C |
|---|---|---|---|---|---|
| G1 | The placard hangs at P1 west on every seed (beside the start door, facing into the map), so the dot is true; otherwise one sheet per mount | 关卡设计 (keep D's west edge Open in `MapRootFor`), 游戏视觉 (mount) | ● | ● | ● |
| G2 | The start door shuts and locks behind you, and never shows the padlock or an unlock prompt | 关卡设计 | ● | ● | ● |
| G3 | One EXIT per floor: a locked stair door under a lit EXIT sign (C23), one floor down, locking behind you (C22); a copy of the plan on each landing | 关卡设计, 游戏视觉 | ● | ● | ● |
| G4 | The floor's key hangs at the attendant station, where the only playing TV is, and opens the stair door (tutorial option A; Floor 1 has no other locked doors). The padlock appears only on doors a key opens | 关卡设计, 教程设计 | ● | ● | ● |
| G5 | The four lines are true in that build: sprinting is heard (26 m), a shut door adds 9 m, the lamps dip when it is near (W1). A line that is not yet true is reworded or cut | 关卡设计, 声音设计 | ● | ● | ● |
| G6 | EXAMINE gets the readable regions and the transcript (`placard_q16`) from the generator's layout data, once 叙事 signs the strings | 游戏视觉 (catalog), 平面视觉 (data) | ● | ● | ● |
| A1 | Only the station's TV plays (heard far off) until its key is taken | 关卡设计, 声音设计 | ● | ● | ● |
| B1 | The five cells past the start door stay Standard Level 0 (`MapRootFor` already does this) | 关卡设计 | | ● | |
| C1 | A read-only floor-start API: a 7 × 5-cell window (walls, doors and arches; windows as wall), the locked doors, the station cell if it is inside, and one stair bearing from C24's path field | 关卡设计 | | | ● |
| C2 | The sheet is drawn once per floor at 6 px/mm (texture or mesh); it never reads the hunter and never updates | 游戏视觉 | | | ● |

**P1 vs P2** (sent to 教程设计 at 07:4x). `10_proposal.md` §5.3 prefers P2, because the door's shut 4 m in turns the player round.
- From the door centre +4 m, facing the door:
  - P1 (on the facade, 1.73 m beside the door) is 23° off the view axis and seen about 23° oblique, nearly face-on;
  - P2 (on the side wall, 1.42 m off-axis, 1.5 m in) is 30° off-axis but seen about 60° oblique, half its width foreshortened.
- P2 is better only while walking in. By §5.3's own rule ("the mount closest to the view after the turn") that is P1, which also keeps plan-up = the start door.
- If P2 is pinned anyway, I draw a P2-rotated sheet.

## 6. Recommendation and Red's calls (TP04)

**Recommendation:**
- **Sheet A for the next build** (Phase 0 first if the stair exists before the lock): one static texture through 游戏视觉's `pack_evac_plan.py`; no code, same kit.
- **Sheet C after the Alpha.**
- **Sheet B** if Red wants a plan that draws only what is certain.

**Calls:**
1. Which sheet?
2. Draw or name the enemy? Recommended: neither; the lines say "near", never what.
3. The v3 wallpaper legend? Recommended: off the sheet until W6 is live.
4. The strings? Recommended: keep the DRAFT lines as placeholders until 叙事 reviews.

Line 4 assumes 教程设计's option A (Red's T1 in `10_proposal.md` §12). Until the stair door locks, line 4 reads `EXIT BY STAIR.`

## 7. Strings: signed by 叙事 on 2026-10-22 (was the DRAFT for 叙事; contract wording from `10_proposal.md` §5.2)

**Sign-off** (`research/narrative/40_floors_exit_tutorial_strings.md` §3). Three changes; everything else is signed as drafted:
- line 1 `1 WALK, DO NOT RUN.`: a comma, the set phrase of Red's A.12 footer;
- line 4 `4 STAIR DOORS ARE LOCKED.` / `KEY AT SECURITY DESK.`;
- the start-door label `NO REENTRY`, with no hyphen. This is the 1973 code form (NYC LL5 §C26-608.5, `SOURCES.md` M5), set by 叙事 at 10:2x, the same spelling as the stair plates. Sheets B and C keep the old hyphenated form until they are used.
- legend `SECURITY` / `DESK (KEY)` and plan label `SECURITY DESK`. They replace "attendant station" wherever a player can read it, because that is not a 1990 office-floor term. Internal names (`Station_*`) stay.

**Pending Red, in 叙事's chat; ship the "until Red" form:**
- The floor token. 叙事 recommends `LEVEL 0`, then `LEVEL B1`, `B2`, …; Floor 1 stays `LEVEL 0` either way.
- The optional fine-print line `ON ALARM, A FLOOR WARDEN CHECKS EVERY ROOM.` Not added.

The draft as it stood:

- **Lines:** `IN CASE OF ALARM`; `1 WALK. DO NOT RUN.`; `2 CLOSE DOORS BEHIND YOU.`; `3 IF THE LIGHTS DIM,` / `LEAVE THE AREA.`; `4 STAIR DOORS ARE LOCKED.` / `KEY AT ATTENDANT STATION.`
- **Legend:** `LEGEND`, `YOU ARE HERE`, `STAIR (EXIT)` / `ONE FLOOR DOWN`, `ATTENDANT` / `STATION (KEY)`, and in C `LOCKED DOOR`.
- **Plan:**
  - `NO REENTRY`, `EXIT` (the sign), `STAIR A`, `ATTENDANT STATION`;
  - A: `TYPICAL FLOOR · NOT TO SCALE`, `THIS FLOOR MAY DIFFER FROM PLAN`;
  - B: `MATCH LINE — SEE SHEET A-3`, `NOT TO SCALE`, `BEYOND MATCH LINE: NOT TO SCALE`;
  - C: `STAIR A · EXIT`, `SHEET C-3 BEYOND THIS EDGE`, `PRINTED FOR LEVEL 3` (signed: `PRINTED FOR LEVEL B3`).
- **Kept from A.12:** `EVACUATION PLAN`, the title block (`LEVEL n` is now the floor count). The approved footer's two lines became lines 1 and 2.

## 8. After Red picks

1. Port the chosen layout into `Tools/print/ink/art_from_graphic/placard/placard.swift` (平面视觉's generator) at 2592 × 1674 plus the glow mask. Keep v3 in `v3/`. Export the EXAMINE regions (sheet mm plus smallest cap) and the transcript lines from the layout data.
2. 游戏视觉 re-packs with `Tools/lookdev/pack_evac_plan.py`, re-renders the three-view (K81 `2948:6863`) and re-checks the dark ratio (≥ 1.30). Figma T24 `2504:8` is re-imaged.
3. Landing copies: one texture per `LEVEL n` (or a number decal). Needs G3 and 关卡设计's stairwell.
4. A new VL row for the in-engine check at the chosen mount (walk-up and EXAMINE).

## 9. Open

- 教程设计's contract and its option A (the station keeps the stair key) are not yet Red's (`10_proposal.md` §12 T1).
- P1 vs P2 (§5): **settled 07:5x, P1 west** (教程设计 withdrew P2; `10_proposal.md` §5.3 uses G1). 关卡设计 can add "E(D, D−x) Open" to `MapRootFor` with a 100-seed check (its row M2 in `research/tutorial/20_requests.md`); it waits for Red's sheet pick.
- Media for the research frame waits on Red's OK (`SOURCES.md` §7.3).

## 10. Answers and requests received (2026-10-22, 07:5x)

**From 教程设计** (pending Red's T1 = its Option A):
- The EXIT door itself is locked, and one floor key opens it, so A1 always holds.
- One station per floor: the only playing TV until its key is taken. The key is a floor key, not a zone key.
- Only W1 is live in main; steps (W2) and chime (W3) stay off the sheet.

**From 关卡设计** (main as of 08:10):
- **Mount:** P1 west is not guaranteed today (the mount ranks P1 west, P1 east, P2, P3), but it can be added in `MapRootFor`. Floors 2+ get their own arrival door; a placard there is a 游戏视觉 mount request.
- **Lines 1–3 are true today:**
  - desktop walking makes no noise for it, sprinting does (a phone's walk makes 9 m);
  - a shut door or window adds 9 m;
  - W1 bursts: stage 1 ≤ 30 m of path, stage 2 ≤ 18 m, 2–3 dips per burst, one slow sag with Reduce flashing.
- **G2 holds today.** G3 is C22 (not built yet).
- **G4 is not true yet:** keys are walk-over and not in TV rooms, and locks are off.

**Requests to me** (`research/tutorial/20_requests.md` §平面视觉):
- **G1 (phase 0):** the chosen sheet, with tutorial Option A's changes (already in v2).
- **G2 (phase 1, after T1 = A):** the STAIR (EXIT) and station pictograms (done in v2's legend), plus the HUD key panel string for one floor key. Decided: the tag shows the floor token (`00`, then `B1`, `B2` …; C28) in the panel's existing Courier Prime numerals, so the HUD tag and the placard's `LEVEL 3` match. The words are 叙事's.
- **G3 (phase 2):** the stairwell copy (the next sheet, with its LEVEL) and the station plan (zone blocks, YOU ARE HERE at the station, STAIR marked, drawn at run time). That is sheet C's grammar.

## 11. Red's tutorial decisions (2026-10-22) and the type specs they trigger

**The decisions.** Red's own words in the 教程设计 chat (session `local_c160c950-9b0e-4f15-ad77-95c2e4930c87`, a user turn read with `list_events` on 2026-10-22 08:1x): "全部按推荐（T1=A，T2=E1，T3 静止屏，T5 去掉文字卡，T6 标题电视带声音）". They answer 教程设计's questions, not my TP04 calls, which are still open.
- **T1 = A:** the stair door is locked, and one floor key hangs at the station. Sheet A's line 4 and its padlock are now the target. TP06 (Phase 0) holds until the lock lands.
- **G2 is a go:** the HUD key tag shows the floor number.
- **T5:** 关卡设计 drops the 9 s CalmHint card and puts a controls legend on the title and pause cards.

**G2 spec: the floor key on the HUD** (words are 叙事's; owners: 游戏视觉 for KeyHud B, 关卡设计 for `FrontRooms3DGame.cs`):
- **KeyHud B** (when it lands): the tag shows the floor token, `00` on Level 0, then `B1`, `B2` … (B levels, C28, 叙事 v1.1 §2/§4; was `00`, `03` before C28). It uses the tag's existing Courier Prime numerals, size and position (`hud_build/10_design.md`); `B10` and deeper are set at 0.8× in the same tag (`FIX_CARD_VL217.md` §B), so the HUD tag and the placard's `LEVEL B1` say the same floor.
- **Main's A panel until then:** today `keyText.text = KeyLabel(zone)` = `ZoneName + " KEY"` (`FrontRooms3DGame.cs:2154`, `:2574`), set in Bayon 20.
  - Bayon draws 0 as O and 1 as I, so `LEVEL 0 KEY` reads `LEVEL O KEY` on the first floor.
  - Interim label (DRAFT): `STAIR KEY`, no digits.
  - If the floor number must show before KeyHud B, put it in a second Text in IBM Plex Mono 13 (the HUD meta face), never in Bayon.

**T5 spec: the controls legend on the title and pause cards** (Info type system, HUD prompt grammar):
- **Desktop:** one line, set entirely in IBM Plex Mono 13 caps, paper ink.
  - Two spaces between the key and its verb, four between pairs: `WASD  MOVE    MOUSE  LOOK    SHIFT  SPRINT    E  USE`.
  - That is the grammar the pause card already uses (`PauseText()`, `FrontRooms3DGame.cs:1983`). Plex Mono has the `·` that Bayon lacks.
- **Handheld:** keep the existing touch line (`DRAG · LOOK`, …) at the touch ramp's 11.
- **No dark card behind it:** on a light ground, a 1 px #141414 keyline at 85 %.
- **Title:** centred under the title prompt, one step quieter than it **through face and size only**: 100 % paper with the keyline.
  - **Revised 2026-10-22 ~15:3x.** The earlier "paper at 77 %" is withdrawn. Unity's `Outline` draws under the fill, so a 77 % fill shows the ink keyline through and the paper reads light grey and soft (交互设计's X2 board 3564:7362 renders it). That also breaks "no translucent ink in game UI".
  - It stays until the first input, with no fade-in animation.
- Steady text, never blinking (photosafety).
- `SPRINT` names a control; the never-RUN rule covers advice strings (the placard, the cards), not key names.

**X2 type answers: the phone control hints that replace the CalmHint card** (平面视觉, 2026-10-22 09:3x).
- Questions from 交互设计, `~/FrontRoomsInteractionWork/x2/X2_PHONE_HINTS_SPEC.md` §5.
- These are my calls as owner of the type, not Red's. Red may veto them in the 平面视觉 chat. Strings stay 交互设计's (`SLOW · UNHEARD`, `STICK HINTS`), with a copy check by 叙事.
- **Q1, the legend face (title and pause together): IBM Plex Mono 11, caps.** It is the touch ramp's Meta, as written above, and the twin of desktop's Plex Mono 13. On iPad it is the tablet Meta 12.
  - **Glyphs stay at their present size.** Text drops to the meta weight, but the glyph is the mnemonic and keeps its size, centred on the label's cap middle. The 6 pt glyph-to-label gap and the 26 pt gap between entries stay.
  - **Colour:**
    - on the pause card's frost: Ink, as now;
    - on the title: **100 % paper with the keyline** (revised 15:3x; not 77 %, see the T5 Title line). It is quieter than TAP TO START through face and size (Plex Mono 11 against Bayon 17).
- **Q2, separators:**
  - In the legend, Plex Mono has its own `·`, so nothing falls back.
  - For Bayon strings (the stick slot's `SLOW · UNHEARD`, desktop prompts such as `E  ·  OPEN DOOR`), the fix is **one importer change**: Bayon's font fallback → IBM Plex Mono Regular. Until then the OS face shows.
  - Legacy `Text` can't mix faces inline, so never split a string just for the dot.
  - The importer change is a visual asset, so it goes through 游戏视觉.
- **Q3, the stick's state-label slot: the keyline, no chip.** Red's 10-14 rule covers every text backing, and the slot is a label, not a control. One treatment for all its words, in Bayon 17 (the touch Label):
  - `RUN` while running: yellow #F4DF3B;
  - the `RUN` preview, `WINDED` and `SLOW · UNHEARD`: paper;
  - all of them with the 1 pt #141414 keyline at 85 % (Unity `Outline`, 1 × L.Scale).
  - The RUN preview can be built now.
- **Q4, the slow glyph: approved in principle.** The stick ring, an inner ring and a centre dot.
  - Same stroke weight and outer diameter as the 10-08 Run glyph.
  - The inner ring at the true slow / run proportion: Ø72 slow ring against the r 84 run line, so the inner radius ≈ 0.43 of the outer.
  - The dot is the knob, filled. No motion marks; the sprite keyline like the other glyphs.
- **X2 board sign-off, 2026-10-22 ~15:3x** (交互设计's board `3564:7362`, gate 2 of X2 spec v3.2 §8):
  - **Title lockup: (c).** The whole lockup moves up 14 pt, so TAP TO START is centred at y 306; the internal gaps stay. Row B clears the iPhone home inset by 8.3 pt and Android 20:9 by 14.3 pt. As specified, (a) and (b) put row B 5.7 pt inside the iPhone inset.
  - **The title legend is 100 % paper with the keyline,** not 77 % (see the T5 Title line).
  - **Confirmed as drawn:**
    - the slot words T1–T5 (Bayon 17, 1 pt keyline, no chip; "·" in Plex Mono until 游戏视觉's Bayon fallback);
    - T5's BUTTON-mode word below the stick (the RUN button's hit radius);
    - the pause legend in Plex Mono 11 ink on the frost, rows where they are;
    - the flow S02 / S03 / S05.
  - The S05 key panel still shows today's `LEVEL 4 / OFFICE KEY` in Bayon digits. That is FIX_CARD A4's (关卡设计), not X2's.
  - **Signed off 2026-10-22 09:5x** (Figma board `3503:7311` on the TK master `2528:5495`; glyph `3503:7312` at ×1 and `3503:7320` at ×4).
    - It matches the Run glyph: the stick ring r 10 at 35 %, the knob a filled Ø4.5, all strokes 1.5.
    - The slow ring r 4.29 at 100 %. At ×1 the knob keeps a ≈ 1.3-unit gap inside it, so it reads as "stay in the middle" next to Run's "push past the line".
    - The title row (paper plus the 85 % ink copy) is right.
- **Q5, scope:**
  - **TAP TO START loses its ink chip.** On the title the whole screen is the tap target, so the chip is a text backing, not a button. It becomes Bayon 17 paper with the keyline, matching the desktop title prompt.
  - **QUIT TO TITLE keeps its dark fill.** A button's fill is the button itself (the 10-08 exit weight), as with the USE disc. The rule covers backings for text, not controls.
  - **The title is one lockup** with one keyline treatment:
    - TAP TO START, centred where it is now;
    - legend row A, with its cap top 16 pt below TAP TO START's baseline;
    - row B under it, at a row pitch = glyph box + 8 pt.
  - **The glass `Flash()` card loses its dark card, on phones and desktop:** paper text with the keyline in the same slot.
    - Desktop is 关卡设计's (`FrontRooms3DGame.cs` contextText).
    - Phones are 交互设计's.
- **Keyline (as cited in X2 §5: confirmed):**
  - 1 pt #141414 at 85 % (Unity `Outline`, distance 1 × L.Scale, effectColor #141414 α .85);
  - on glyph sprites, an ink copy 1 px larger in one 85 % group;
  - never the view's Ink #0A0A0A;
  - use **one shared constant** with the HUD key B build. Its capture stage calibrates `KeylineAlpha` in linear light (O-6, DECISIONS #22), and the touch layer takes the same value.

## 12. Hand-off after Red's pick (2026-10-22)

**Red, in this chat:** "A 先上". Sheet A is the placard, and sheet C stays the later step (call 1).

**Art.** In `Tools/print/ink/art_from_graphic/placard/` (see that README §Sheet A), new subfolders only; the v3 files at the top are untouched.
- `sheetA_phase0/`: print md5 `1e5d244b…` (regenerated 10-22 12:3x for the warden fine print; before that `60fd4cf1…` (10:2x, `NO REENTRY`), `dcf46c7a…` (09:1x) and `c3bd0af5…`), glow `0f42609b…` (unchanged). In-game swap when the C22 stairwell (the EXIT) is in main **and** G1 holds.
- `sheetA/`: print md5 `79b46369…` (regenerated 10-22 12:3x for the warden fine print; before that `59705857…` (10:2x, `NO REENTRY`), `96f4b0df…` (09:1x) and `b819a8a9…`), glow `99ea4ac6…` (unchanged). Swap when the stair lock and the station (tutorial Option A) are live.
- Each folder has the v3 file names for `pack_evac_plan.py` (2592 × 1674 print and glow mask, 3 px/mm previews) and `examine.json`.
- The print is byte-identical to the reviewed render (VL337, TP01/TP06).

**EXAMINE data** (`examine.json`, from the generator's own layout):
- **Regions,** as `[x, y, w, h]` in sheet mm with the smallest cap:
  - plan `[20, 46, 268, 158]`, 2.8 mm (DN);
  - legend `[296, 46, 118, 158]`, 2.8 mm (notes);
  - alarm `[20, 208, 268, 51]`, 4.4 mm;
  - title block `[296, 241, 118, 20]`, 3.6 mm.
- **Transcript:** 15 lines (Phase 0: 14). The EXIT line is the only glow line.
- **Both replace** the v3 legend rect that was measured by eye (`evac_placard.py`). The kit keeps `texelsPerMetre` 7330.

**Not for today's Alpha:** both sheets print a stair EXIT that is not in main yet. v3 stays in the Alpha build.

**Generator in the repo:** `Tools/print/ink/art_from_graphic/placard/tutorial_placard.swift`. Working copies and the v1 history stay in `~/FrontRoomsVisualWork/placard_tutorial/`.

**G1 timing (关卡设计, 2026-10-22):** accepted; it lands right after the C22 stairwell, with a 100-seed check, because C22 is reworking `MapRootFor` now (one change on top, no 3-way merge). 关卡设计 messages 平面视觉 and 游戏视觉 when both are in main; that is the trigger for `sheetA_phase0/`.

**Packing (游戏视觉, 2026-10-22):** accepted sheet A, both folders, with the conditions above (it checked Red's words in this chat). Two apply packages are pre-built with `pack_evac_plan.py` (k 0.158), with a dark-ratio re-check (≥ 1.30) logged as a VL. `examine.json` goes into the kit's `kit.inspect` (`texelsPerMetre` stays 7330), and the 15-line transcript goes into `placard_q16`, shipping with the same sheet. 游戏视觉 sends 平面视觉 the packed in-game renders before each landing: **check them against Figma 3459:7280 (TP01 / TP06) under the game's light.**

## 13. Narrative v1.1 (Red, 2026-10-22 12:15, in the 叙事设计 chat: "全部按推荐（Q1–Q7、照片下载、LOBBY 改名）")

- **Placard fine print** `ON ALARM, A FLOOR WARDEN CHECKS EVERY ROOM.` (叙事 `40_floors_exit_tutorial_strings.md` §3). It names a role only, is not a numbered line, and the four-line limit stands.
  - **Set:** right-aligned on the `IN CASE OF ALARM` row, x 140.7–282 mm, Heros Bold 3.6 mm caps, +0.2 mm tracking, white on safety red. It is not in the glow mask.
  - **Reads:** in EXAMINE base it is 14.2′ ("with effort", 9 px at 1080p); at the zoom step it is 27.9′. The alarm region's smallest cap is now 3.6 mm, so the zoom step reaches it.
  - **Where:** on every sheet (A, Phase 0, B, C, and later the stairwell copies), because `alarmBlock()` is shared.
  - **Records:** hand-off md5s in §12; review images, TP01 / TP02 / TP03 / TP06 and VL337's slots refreshed in place. The verdict is unchanged: every numbered line is still 5.0 mm.
- **Figma strip slots get the plain tiles.**
  - The strip slots on the boards and in VL337 are 4.7 : 1, so they now hold the plain nearest-neighbour tiles (`images/tp2_opt{A,A0}_distance_tiles.png`).
  - The Shot-format strips (`tp2_opt{A,A0}_distance_strip.png`, 3.3 : 1) stay as the standalone images; FILL had cropped their header in the board slots.
- **CAUGHT subtitle, HUD room line, tag token:** specs in `research/desktop_ui/FIX_CARD_VL217.md` A7, A8 and §B (owners 关卡设计, 交互设计, 游戏视觉).
  - The CAUGHT stats line leaves Bayon for Plex Mono 20, because Bayon's digits misprint.
  - `TIER` on CAUGHT: resolved 12:4x. C04 c holds, so CAUGHT has no TIER (系统设计; 叙事 40 §6 v1.1 fix: `LEVEL B2  /  123 S  /  5 ZONES  /  1 KEY  /  0 DOORS BROKEN`).
- **Wall glow, settled 12:34:** B, the walls never glow. Red, in the 叙事设计 chat: "Q4 保持 B：墙不发光" (verified in that transcript), given after VL350 showed glow A no longer hides FLOW.
  - **Rule for the future wall legend** (it returns only with W6): the cue swatches print as printed only. There is no `IN POWER FAILURE` / glowing-wall swatch (v3 had one).
  - The placard's own glow mask (dot, route, stair door, EXIT) is unchanged.
  - Until W6, the wall legend stays off the sheet.

## 14. The stairwell copy, LEVEL B1 (staged 2026-10-22 13:3x)

游戏视觉 asked for it before its first stairwell step, which dresses C22's 3 × 3 vestibule. The copy hangs by the next floor's start door there, and later by the walkable stair's far door.
- **Generator:** `tutorial_placard.swift` takes `level=B1` (header + title block). At LEVEL 0 it reproduces `sheetA/` and `sheetA_phase0/` byte for byte (checked).
- **Staged:**
  - `Tools/print/ink/art_from_graphic/placard/sheetA_B1/`: print 2bcf6c87…, glow 99ea4ac6…, examine 16c7f397…;
  - `sheetA_phase0_B1/`: print ce35c086…, glow 0f42609b…, examine e5bfc405….
  - The glow masks equal LEVEL 0's: the header does not glow.
- **Conditions:** the same mount rule (P1 west of the arrival door). The full or Phase 0 variant is chosen by the same lock condition as Level 0.
- **Sheet C's illustration** now reads `LEVEL B3` / `PRINTED FOR LEVEL B3` (C28). TP03 and VL337's C images and board texts were refreshed.

## 15. Print date `PRINTED 11/89` (2026-10-22 14:3x)

- **The rule:** 叙事 40 §3 (540c7c2a) makes every printed date 1989 or earlier. Red asked (游戏视觉 chat, 14:1x) that clocks and computers show the real month and day in 1990, so a March 1990 print date would be in the future for anyone playing in January or February.
- **Change:** the title block reads `PRINTED 11/89` on sheet A, Phase 0, both B1 copies, and B and C.
- **New md5s (print / glow / examine):**
  - `sheetA` d55a4460 / 99ea4ac6 / cc7edfc5;
  - `sheetA_phase0` 9d52bba0 / 0f42609b / fe327e70;
  - `sheetA_B1` cceb8267 / 99ea4ac6 / de2fdeff;
  - `sheetA_phase0_B1` 15092ba4 / 0f42609b / 9acf42c4.
  - These supersede §12–§14's md5s.
- **Refreshed:** the review images, Shot strips and tiles, Figma TP01 / TP02 / TP03 / TP06 and VL337 (verdict unchanged).
- **Not changed:** today's in-game v3 keeps `03/90` until sheet A replaces it.

