# 50 · Security desk: what the two critics found and what changed (游戏视觉, 2026-10-22)

Workflow `station-look`, fix stage, run on account B after account switch #2. Written by 游戏视觉.
- **Critics:** era and look (21 items), gameplay and tutorial (16 items). Every item is listed below with what was done and the evidence.
- **Runs:** clone `W/proj_station` (`W` = `/Users/redwang/FrontRoomsVisualWork`), harness r7 (`data/FrontRoomsStationLookdev.cs.txt`; the r6 harness is kept as `data/FrontRoomsStationLookdev_r6.cs.txt`).
  - r7a: the V3 light grid at the take (7 light options × 5 cards).
  - r7: the 30-seed sweeps per module (real generator).
  - r7b: the final hero plans `Wp4`, `Wp4o`, `Wp4s`, `Dp4`, `H`.
  - r7c–r7e and two isolated shadow tests: the light-leak diagnosis (§4, gameplay 11).
  - Numbers: `data/blockout_metrics_r7.json`. Runs: `data/runs.md`. Images: `images/station_r7_*` (VL354–VL358 updated, VL372–VL375 new).
- **Load:** every run was UNDER LOAD (1-minute load 45–127).
- **Not done here:** no Unity on `Frontrooms3D`, no git writes, no downloads. The module assets are still V1's job after the Alpha.

## 1. The plans after the fixes

| Plan | Use | What changed from r6 |
|---|---|---|
| **W′ rev 4** (`Wp4`) | B · Floor 1 and Level 0 floors | The guard's chair leaves the staff strip: it stands at the counter's north end, turned to the set, 22° off its axis, with his jacket on it. One tandem row is turned along the S wall with the beam ashtray table; the other faces the set. The key cabinet moves to the strip's E wall, door shut. Cell (1,1) Dim instead of Off (W1). The set's bottom at 2.15 m. E1's corridor side Steady, E2's Dim (the Floor 1 rule, §2) |
| `Wp4o` | W1 comparison | As `Wp4` with (1,1) Off |
| `Wp4s` | exits comparison | As `Wp4` with E2's corridor side and the junction cell beyond E1 also Steady |
| **D′ rev 4** (`Dp4`) | A · Office floors | E2 moves from the E wall to the N wall's west cell (two escape directions, 103° apart at the take). The desk moves ~1 m toward the set; the chair is pulled out to face it, 2.6 m from the screen, in its wash. Two sled chairs and a laminate side table along the W wall replace the four task chairs; the sign-in ledge moves to the W wall's middle. The cabinet goes to the E wall's middle, door shut |
| H | long rooms only | Only the shared fixes |

**Shared by all plans (the take cluster and the light):**
- The annunciator is a putty cabinet with a narrow putty door frame and no glazing. It has round frosted lamp lenses (Ø 14 mm, never lit), narrow engraved label strips (#C9C3B2 on putty) and an incandescent POWER pilot (green glass jewel with a faint warm filament).
- The annunciator sits 1.0 m from the key, on the side nearer the screen's plane, so the set does not wash it. It is still in the take frame, 49° off the view.
- The key hangs on its switch plate on a 0.30 × 0.40 m painted-steel backer. The tag is white: `Kit_KeyTag_Long_White` at 1.5× (114 × 48 mm, an oversized desk fob). The final tag colour is 平面视觉's.
- The EMT leaves the plate box from below, runs 0.25 m under the panel and enters its bottom knock-out with a connector and 3 straps.
- The bracket set's power cord and RG-59 coax run down the wall to a duplex outlet and an F-jack plate.
- **V3 r7 light** (§3).

## 2. Gameplay and tutorial critic (16 items)

| # | Finding | What we did | Evidence (r7b unless marked) | Now |
|---|---|---|---|---|
| G1 | The nearer exit at the take leads away from the EXIT, toward Floor 1's hunter | Measured as a Floor 1 rule. Geometry cannot fix it: G1 puts E1 on the set's face and G4 puts the key beside the set, so the take is always far from E1 (W′ 7.1 m to E1, 5.1 m to E2). So: (1) light: E1's corridor side Steady, E2's Dim, so E1 is the brighter opening; (2) ask 关卡设计 to extend M7 (§5) | E1 just brighter than E2 seen from the board (0.098 vs 0.094); 27 of 31 sweep seeds. With E2's corridor also Steady (`Wp4s`) E2 wins (0.147 vs 0.098) | Light helps, the map must guarantee it (M7 extension) |
| G2 | Hand-picked seeds; M4 rules missing | Proposed M4 rules (i)–(iii) plus a sight-line test (§5). Swept 30 random seeds per module that pass them, in the real generator, plus one seed where E2 is a map door | W′: 31 seeds of 1,201 scanned; D′: 31 of 4,192. Junction 29/29 and 16/16 where a line of sight exists; from E1 31/31; wiggle 30/31; paths 29/31 and 30/31. Exits after the take seen: PARTIAL most seeds (§4). VL372 | Done; rule (i) needs the sight-line test |
| G3 | A Tall neighbour can put a window in a station wall | M4 rule (ii) and a checklist line: no glass on any station wall | The sweep rejected 1 seed for glass in W′ (on E2) and 6 in D′ (5 on other walls, 1 on E2) | Rule written (§5) |
| G4 | D′ rev 3's exits are one escape direction | E2 moved to the N wall's west cell. New G2 measure: exits ≥ 60° apart from the take and outside cells ≥ 3 cells apart by walking | 103° apart; 22 cells round. W′ 88°, 17 cells; H 90°, 8 cells | Fixed |
| G5 | Take timing clashes with the first-key hold and C18; sound 0.4 s off the picture | One timing contract (§6). The §4.2 sentence about C18 framing the tag as it goes dark is withdrawn (`30_look.md`) | 7 frames t 0–1.0 s: the board light is at 29 % at 0.40 s, 13 % at 0.50 s, 0 by 0.60 s, while the key is still in hand. VL373 | Contract proposed to 关卡设计, 声音设计, 教程设计 |
| G6 | G5 fallback passes only on the mean card and only in E | Check 3 is now the displayed board luminance on the worst of the five cards. V3 changed (§3) | Worst card seen: W′ 73 %, D′ 81 %, H 73 % (ΔL* ≥ 16; E drop 83 / 80 / 83 %). With V3 as specified, the same rooms lose only 26 / 37 / 26 % on the dark card. VL356 | PASS |
| G7 | Key visible but not readable; panel competes; red tag; cabinet beside the plate | White oversized fob, backer, matte unlit panel 1.0 m away out of the wash, cabinet off the plate's wall and shut. Legibility bar: board ≥ 1° from E1, key + tag ≥ 150 px at 3 m | Board 3.0° (W′), 3.1° (D′), 2.0° (H) from E1. Key + tag 218–220 px at 3 m. VL375 | PASS |
| G8 | The first W1 burst is weakest in the Floor 1 module | Check 7 added. (1,1) Dim | Pixels changing ≥ 10 % at a dip to 0.70 (0.55): W′ take view 41 % (56 %), turn to the set 31 % (42 %); with (1,1) Off only 7 % (21 %). D′ 72 % / 19 %, H 42 % / 3 %. The set's corner is Off by design, so the turn toward it barely changes. VL373 | PARTIAL: the ballast groan has to carry it (声音设计) |
| G9 | W′ rev 3 fails G6's 1.2 m | The chair is out of the strip. Check 5's bar is G6's 1.2 m | Every in-room route 1.4 m; the strip 1.4 m; full take→E2 1.2 m (E2's hashed arch 1.29 m). Sweep: 29/31 (s86 1.0 m beside the counter end; the door seed with its leaf shut) | PASS |
| G10 | Phone view unchecked; walk-over take breaks the beat | Phone captures at 2622 × 1206 and the iPhone quality level, with the stick and USE drawn as outlines from `TOUCH_CONTROLS.md` §3. Rule: the station key on phones by look + tap (X1's key tap, already in 交互设计's queue after the Alpha). Panel and clipboard now ≥ 1.0 m and ≥ 1.2 m from the key | From the junction the screen is 1.05 mm tall on a 6.3 in phone (D′ 1.11, H 0.83); the board from E1 2.6–2.8 mm. The take drop reads: 73 % (W′), 81 % (D′) on the dark card. VL375 | FINDING: the junction read is weak on phones; the sound carries the lure there |
| G11 | Performance and light leaks not measured | Leak measured three ways (§4). Static counts in place of a GPU capture (none in batch) | **New finding:** with the map built, the capture harness renders no additional-light shadows at all, so the set's light passes walls: 0.142 E on the floor beyond W′'s N wall (19 % of that room's light), 0.061 beyond D′'s E wall (6 %), 0.114 in H (13 %). Counts: the blockout uses 178 (W′) / 118 (D′) primitive renderers in the footprint; the hero tier is ~30 kits (≤ ~90 renderers, `30_look.md` §1.5) | FLAG: fix by rendering layers on every tier (§4); autopilot acceptance goes to V1 |
| G12 | Floor 2+ station can become a through-route | M4 rule (§5): compute the arrival→EXIT route with the station walkable; reject spots it passes through | — | Rule written |
| G13 | The "read it afterwards" path is unproven | EXAMINE close-ups with the inspect adapt (+1 EV, contrast +12) | Plate face after the take: L* 15 (W′), 9 (D′); the placeholder sign's glyph contrast 1.3–1.6 : 1. Before the take on the dark card: L* 44 / 30, 2.0 / 1.7 : 1. VL375 | FLAG: 平面视觉's plate face needs ≥ 3 : 1 (light letters on a dark plate or the reverse), not a lamp |
| G14 | Combined photosafety untested | One capture: three W1 dips 0.55 s apart, the middle one on the loop wrap, 30 fps | At most 2.0 flashes a second in any 10° field (limit 3). The raw count is 3–9.5 because the capture swings 8–21 % frame to frame in dark 10° areas before anything happens (the post's grain); a 2-frame mean removes it. VL358 | PASS; the grain swing is flagged for the post (not the station) |
| G15 | The Relay passes under the set with 6 cm to spare | Set bottom raised to 2.15 m. Clip test of the rig | The prototype rig (`FrontRoomsRelayRig`, bodyScale 1) is 3.03–3.15 m tall in every state, taller than the 2.9 m ceiling itself, so it cannot be clip-tested against the set. The contract is `LEVEL_MODULE_SPEC.md` §7 (≤ 2.05 m walking). Re-test on the final rig | Raised; the rig test waits for the final Relay |
| G16 | Doc fixes before the draft | Done in our files; the rest listed for their owners (§7) | `30_look.md` §10, this file | Done / passed on |

## 3. Era and look critic (21 items)

| # | Finding | What we did | Evidence | Now |
|---|---|---|---|---|
| E1 | The annunciator reads as a modern black keypad | No glazing (a narrow putty frame), round frosted lenses Ø 14 mm, narrow label strips #C9C3B2, matte. The specular hot spot is gone with the glass | VL374 crops r6 / r7 | Fixed |
| E2 | The stair key's tag is red | White tag (oversized fob). Final colour is 平面视觉's | VL374 | Fixed |
| E3 | "−86 %" is E, not what the player sees | The bar is now displayed luminance (with L* and E beside it), worst card. G5 depends on V3 r7 (§3). Slides and VL356 reworded | Worst card seen 73 / 81 / 73 % | Fixed |
| E4 | Check 1c PASS is E only | 1c judged on the displayed frame, lamp lenses excluded, E1 and E2 apart. Ask 关卡设计 for Steady corridor-side lamps (§5) | Exits vs room seen: W′ 1.34× (top-2 % cells in exits 7 %), D′ 1.65× (9 %), H 2.09× (53 %). With both corridor sides and the junction Steady W′ reaches 1.74× (19 %) but then E2 outshines E1 | W′ FAIL, D′ PARTIAL, H PASS (§4) |
| E5 | SD01 argues against B | Proposed for 叙事: B is the floor reception the night guard took over, with his own things moved in (jacket on his chair, clipboard, charger, cup on the counter's north end). SD01's statement reworded to "A floor's night post: a desk someone just left." | SD01, SD03 | For 叙事 to confirm |
| E6 | The conduit runs the wrong way | From below: out of the box's bottom, 0.25 m under the panel, into the bottom knock-out, 3 straps | VL374 | Fixed |
| E7 | W′ rev 3's check 2 sat on the bar; the 0.3 target dropped | Check 2 now needs > 50 % on the darkest card and reports the 0.3 target | Darkest-card share 83 / 80 / 83 %; E on it 0.40 / 0.31 / 0.37 (target met) | PASS |
| E8 | B's turned chair cannot see the set | Chair at the counter's north end, 22° off the screen axis, 2.3 m away, facing it | VL374 plan | Fixed (layout is 叙事's to accept) |
| E9 | D′'s hero image not in the render | Draped jacket (sloped shoulders, a sleeve over the arm), lighter uniform tone #59647A, chair pulled into the set's wash | VL374 | Fixed at blockout grade |
| E10 | D′'s guest chairs read as a TV lounge | Two sled chairs and a side table along the W wall, facing into the room, not the set | SD02, VL374 | Fixed |
| E11 | B stacks two A24 signatures | One row along the S wall with the beam table, the rack on the W wall | VL374 | Fixed (G10 is 叙事's) |
| E12 | The set's light reads as a purple ceiling fixture | Cosine cookie with an up-clip (fades from 10° to 30° above the horizontal) and 60 % toward the set's white | Ceiling 0.7 m in front of the bracket set: E 2.09 without the cookie, 0.08 with it | Fixed |
| E13 | The jewel is a modern LED green | An incandescent pilot (green glass, warm filament), less emission | Displayed hue / saturation: W′ 140° / 0.54, H 139° / 0.54, D′ 160° / 0.88, against the glow ink's 99–104° / 0.29–0.35. VL374 swatch | PASS on separation; the hero pass tunes one colour against both zone grades |
| E14 | SD01's evidence is thin | "Era unverified (photo 2008)" on the Faraday chip; a line on why the beacon is 21 in; reference link cards with scene, year and source (time codes to be checked by 叙事, `02_refs.md` says they are memory); the Reagan Library and Toronto Star archive asks listed for Red (draft §6) | SD01 | Done; time codes with 叙事 |
| E15 | Palette grounding is mostly memory | Navy vinyl, oak, grey laminate and putty marked ESTIMATE on SD02 / SD03; catalogue reading (1988–91) asked of 叙事 | SD02, SD03 | Done |
| E16 | "Silent alarm" is intrusion vocabulary | For 叙事 (strings are theirs): keep it knowingly, or "SUPERVISORY" wording | — | Passed on |
| E17 | "Heavy hook, strap cut" implies violence | S26 is now an empty heavy hook (or the strap's buckle loop left on it) | `30_look.md` §10, SD07 | Fixed |
| E18 | Period cues missing | Power cord and coax in the blockout; new kit rows S37–S40 (cords, phone cords, NOV 1990 blotter and phone list under glass, notice board); the CRT hero surface (curvature, glass reflection, bloom) noted for S01 | `30_look.md` §10 | Added |
| E19 | The open key cabinet tells an unwritten story | Shut (a stand-in door: the kit has no separate door part) and moved off the plate's wall | VL374 | Fixed |
| E20 | SD01 credits and privacy | Authors added: BrokenSphere (Faraday, 2008), Degen Earthfast (Detex open, 2014). The CC0 annunciator photo's street address is blurred in the board copy (`W/station/board_stills/annunciator_2020_redacted.jpg`) and re-uploaded | SD01 | Fixed |
| E21 | Shot-format details | New r7 images; every sheet listed in its VL row; the walk map's meta says "orthographic"; tile data shortened so the deciding numbers show; chips match the slide verdicts; the load range is one figure per run | `images/station_r7_*` | Fixed |

## 4. What still does not pass (said plainly)

1. **Exits after the take (check 1c).** Seen with the lamp lenses left out, the exits are brighter than the room in every plan, but they are the brightest shapes only in H.
   - W′ 1.34× (bar 1.5); D′ 1.65×; the sweep's median 1.63× (W′) and 1.99× (D′).
   - The brightest surfaces are wall patches under the two Steady entrance lamps that tutorial V1 asks for.
   - On Floor 1 we want E1 brighter than E2 as well. Making both corridor sides Steady (`Wp4s`) lifts the exits to 1.74× but lets E2 win.
   - **Our call:** keep `Wp4` (E1's corridor Steady, E2's Dim) on Floor 1, and accept PARTIAL. The map, not the light, has to keep Floor 1's hunter off E2's side (§5, M7).
2. **W1 seen after the take (check 7).** PARTIAL in the Floor 1 module: 41 % of the take view changes at the shallowest dip, 56 % at the deepest. The turn toward the dead set barely changes (the set's corner is Off by design). The ballast groan (sound) has to carry W1 there.
3. **Light leak.** In this harness the map's additional lights cast no shadows once the map is built.
   - An isolated wall: lit 0.512 in front, 0.041 behind.
   - The same rig built 20 m above the map: 0.592 in front, 0.232 behind.
   - A closed test box, a fresh light and every other light off changed nothing.
   - So the r6 "soft shadows" were never in effect, and every capture lets the set's light through walls.
   - Whether Play mode and builds have the same problem is not tested here. It matters for every shadowed map lamp too.
   - **Our call:** do not rely on the shadow. Give the station's shell its own renderer (关卡设计 already agreed for WebGL) and use URP rendering-layer bit 1 "Station" on **all tiers**, so the set's light only reaches the station. That is a change to the 10-22 WebGL-only plan, and it needs `m_SupportsLightLayers` on in the URP asset. The V3 workflow checks Play mode first.
4. **EXAMINE after the take.** The plate needs 平面视觉's high-contrast face.
5. **Phones.** From the junction the screen is about 1 mm tall. The beacon's sound and the 1.5 m view carry the lure on a phone.
6. **The Relay clip test** waits for the final rig.

## 5. Asks to 关卡设计 (map), written as M4 / M7 rules (no Red decision)

1. **M4 (i):** E1's outside cell and the next cell out on E1's axis are Open or Arch at the same height, **and a straight line of sight runs from 4.5 m out to the screen.** The sweep shows why the last part matters: an open next cell with no line of sight in 2 / 31 W′ seeds and 15 / 31 D′ seeds.
2. **M4 (ii):** reject a spot where any station perimeter edge other than E2 lies on a height or chunk border, or where the map reopens a station wall. **No glass on any station wall.** The sweep rejected 25 seeds for a station wall on a height border, 3 for a reopened wall and 7 for glass.
3. **M4 (iii):** set E1 to 1.6–1.8 m and E2 to 1.1–1.4 m through the coming `archWidth` field. As hashed today E1 runs 1.10–1.79 m; only 6 / 31 W′ and 2 / 31 D′ seeds fall in range by chance.
4. **E2 as a map door:** acceptable (swing clear, leaf 0.98 m). But:
   - shut, it is darker than the room from the take (0.033 vs 0.048);
   - in D′ the open leaf hides the key from 1 of 5 eye spots at E2.
   - Prefer an arch when M4 can choose; if a door, swing it out of the station.
5. **Loops:** M4's loop rule (both outside cells join without the station). The hero seeds loop in 17 (W′) and 22 (D′) cells. In the sweep only 24 / 31 (W′) and 15 / 31 (D′) loop within 14 cells, so the validator needs a real search radius.
6. **M7 (Floor 1):** keep the arrival cells and the first Hunt leg ≥ 2 cells from E2's loop as well as from the station→EXIT path, and let the Hunt enter by the opening away from the walk back (E2). The map guarantees this; the light only nudges.
7. **M4 (Floor 2+):** compute the arrival→EXIT route with the station walkable and reject spots it passes through.
8. **Lamps:** a Steady lamp on the corridor side of E1. On Floor 2+ also on E2's corridor side. On Floor 1, E2's corridor side Dim.
9. **The shell split** for the rendering layer (§4.3), now on all tiers.

## 6. The take, one timing (for 关卡设计 M6, 游戏视觉 V2, 声音设计 S4, 教程设计 / inspect row K)

| t | What happens |
|---|---|
| 0 | E pressed (desktop) or the key tapped (phone, X1) |
| 0.25 s | `KeyTaken`: the key leaves the hook into the hand; **the jingle cut, the power-off zip, the picture's collapse and the light's fall all start here** (S4 and V2 move from +0.4 s) |
| 0.25–0.60 s | The light falls (τ 0.12 s) and reaches zero by 0.60 s, while the key is still in hand |
| 0.75 s | The pocket beat starts (`HoldUntil`) |
| first key only | E · LOOK opens the close-up only from `HoldUntil` (0.75 s), so a close-up never splits the one change |

## 7. For other lines (no Red decision)

- **叙事:**
  - SD01's reworded statement and B's identity (E5);
  - the turned chair and the rows (E8, E11; G10 is yours);
  - "SILENT ALARM" vs supervisory (E16);
  - time codes for the reference cards (E14) and the 1988–91 catalogue reading (E15);
  - `20_layout.md` G5 still says "dispatched from ≥ 42 m": on Floor 1 it is 22–30 m since C32;
  - G1 now reads "the set within about 12° of E1's axis" and G2 gains "exits ≥ 60° apart at the take, outside cells ≥ 3 cells apart by walking".
- **教程设计:** two approved lines change (draft §3, call 2):
  - tutorial step 3 / §4.6 "a cold, moving light spills from one doorway" becomes "the playing screen shows through the doorway when you stand on its axis";
  - §4.7 "the picture collapses" is out of view at the take; the light on the key going out is the frame.
- **系统设计:** C32's walk-back estimate (3.5–7.5 s) leaves out the walk inside the room: 7.1 m from the take to E1 in W′, 10.1 m in H, so 2.2–3.2 s more.
- **声音设计:**
  - S4's zip moves to `KeyTaken` (0.25 s);
  - the ballast groan carries W1 in the Floor 1 module.
- **平面视觉:**
  - the plate face at ≥ 3 : 1;
  - the tag colour;
  - the annunciator label strips;
  - the KEY CONTROL door art;
  - the oversized fob's `STAIR A`.
- **交互设计:** the station key by look + tap on phones before the station ships (X1). EXAMINE-able props ≥ 1.2 m from the key on the plan (the panel is 1.0 m away and has no prompt).
- **Relay rig owner:** re-run the clip test under a set bottom of 2.15 m on the final rig (the prototype is 3.1 m tall).
- **游戏视觉 (own rows):**
  - V3 r7 (§3 of `30_look.md` §10);
  - the rendering-layer leak fix on all tiers;
  - the grain swing check on the post (8–21 % frame to frame in dark 10° fields in captures).
