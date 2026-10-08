# 05 — Windows: what to look at, and what you decide (for Red)

Status: r6, 2026-10-07. For Red. 中文读者：每节第一句是结论。

You asked: "build the window's real structure, not one pane of glass" (re-raised 2026-10-03: "the windows are still just one pane of glass").

**Nothing in this folder lands until you confirm the doors + windows proposal** (Figma, page "Undergoing Game Projects", section `FRONTROOMS · DOORS + WINDOWS · PROPOSAL (rebuilt 2026-10-07)`, 2748:6099; windows on DW08–DW09, your calls on DW11–DW12).

---

## 0. Short answer

- **The frames are already in your game.** On 2026-10-03 at 19:10 Codex copied our unfinished window files into main. Since then every map window has a real frame in Play: walnut in Level 0 rooms (W-L0), dark-bronze pressed steel in Office rooms (W-OF), and a 6 mm glass slab. The map's old trim boxes are still drawn under the frame.
- **We re-tested all of it on today's main (`279c144`).** Gameplay is unchanged: 411 windows in 10 seeds, every aim ray, Relay sight line and climb is the same as without the kit. The real game in Play passes 26 / 26 checks. Every number matches the 2026-10-04 run.
- **What is left to land is small** (§4): a corrected facade header and a kill switch, the map's guarded call (R1, R2), and, if you say yes, the trims removal (T) and a tan putty glazing line.
- **The frames are right. The look is not finished.** At play distance (1–3 m) the windows still read as dark picture frames around a view:
  1. the glass does not read as glass;
  2. the walnut reads as one flat block;
  3. the steel reads as black plastic;
  4. no wear shows;
  5. the frames sit on the wall like stickers.
  Each has an owner and a test (§3). Three need your call.

---

## 1. Frames to look at

All in `images/`. BEFORE = the kit switched off (what the windows were on 2026-10-03 noon: the map's trim boxes and a 30 mm milky pane). AFTER = the kit with the full landing (R1 + R2 + T + putty). Same camera, eye height 1.62 m, today's main. The r6 frames match the 2026-10-04 r5 frames (`images/r5_*`); the measured difference is in `tf_logs/r6/frames_r6.txt`.

| # | File | Look for |
|---|---|---|
| 1 | `r6_L0_A_front_1.5m_pair.jpg` | Level 0 room side: walnut casing, stool with nosing, apron. The milky veil is gone |
| 2 | `r6_OF_A_front_1.5m_pair.jpg` | Office side: dark-bronze steel face band and channel stop |
| 3 | `r6_L0_B_front_1.5m_pair.jpg`, `r6_OF_B_front_1.5m_pair.jpg` | The hall side (face B) |
| 4 | `r6_L0_A_45deg_2.4m_pair.jpg`, `r6_OF_A_45deg_2.4m_pair.jpg` | At an angle: does the glass show at all? (§3 A1) |
| 5 | `r6_L0_A_close_sill_0.3m_pair.jpg` | 0.3 m: stool, nosing, stops, glazing line |
| 6 | `r6_L0_A_close_horn_0.3m_insp_pair.jpg` | Stool horn and the square-cut apron end (§3 A9) |
| 7 | `r6_L0_B_close_head_0.3m_pair.jpg` | Head mitre; the casing and liner as one block (§3 A3) |
| 8 | `r6_OF_A_close_screws_0.3m_insp_pair.jpg` | The 30 screws and the groove in the steel stop (§3 A11) |
| 9 | `r6_OF_B_close_head_0.3m_pair.jpg` | Round lamp highlights on the glass (§3 A2); dashes on the stop (§3 A7) |
| 10 | `r6_L0_broken_A_front_1.5m_pair.jpg`, `r6_OF_broken_A_front_1.5m_pair.jpg` | After the break the frame stays and the opening is clear |
| 11 | `r6_L0_hall_tall_5.0m_pair.jpg` | From the tall hall. Note the door at the right keeps the map's flat box trims (§2.4) |
| 12 | `r6_OF_deadlamp_A_front_2m_pair.jpg` | Steel under a dead lamp melts into the dark (§3 A4) |
| 13 | `r6_play_L0_play_intact.jpg`, `_broken`, `_climbing` (and `OF`) | The real game: before the break, after it, mid-climb |
| 14 | `r5_L0_A_close_stop_0.3m_insp_rubber_vs_putty.jpg` | Black rubber (main today) vs tan putty (§2.3) |
| 15 | `r6_OF_B_close_head_0.3m_shadow_diag_sheet.png` | The dash pattern on the steel stop, 5 ways (§3 A7) |

In Figma they are in `FRONTROOMS · VISUAL VERIFICATION LOG` as VL106–VL109 (VL059–VL063 hold the 2026-10-03 run).

---

## 2. Your decisions

### 2.1 The gate: confirm the doors + windows proposal

Nothing below lands before you confirm it. If you change a call there (Lobby glass, blinds, LODs), the window items follow it.

### 2.2 T: remove the map's trim boxes under framed windows — recommend YES

- **Today:** the map still draws 3 flat trim boxes per window, hidden under the kit casing (1,233 boxes in our 10 test seeds). They cost overdraw and 273 shadow casters, and their faces sit 0.5 mm from the kit's faces (a flicker risk).
- **With T:** a window gets the map's trims only if its kit frame failed. Doors keep theirs. Nothing visible changes today.
- **Why it matters later:** the walnut casing was shaped only to hide these boxes, and that is why it reads as one flat block (§3 A3). With T, the casing can get a real reveal. The true 2-inch steel profile also needs them gone.
- **If no:** nothing breaks. The frames stay as they are now.

### 2.3 The glazing line on walnut: tan putty or black rubber — recommend PUTTY

- Period wood-stop windows were bedded in tan compound or putty. A black line reads as a modern rubber gasket.
- Putty: sRGB 150 / 140 / 118, smoothness 0.3, walnut windows only. Steel keeps the black tape, which is right for steel.
- Frame: `r5_L0_A_close_stop_0.3m_insp_rubber_vs_putty.jpg` (VL109).

### 2.4 Windows before doors — recommend (a)

The windows went live before the G1 door frames. Doors in the same halls still have the map's flat box trims (`r6_L0_hall_tall_5.0m_pair.jpg`). That breaks "one grammar" for doors and windows.
- **(a) Recommended:** keep the windows live. Record the mismatch as accepted in the proposal. Land the rest of the window items in the same batch as the G1 door frames.
- **(b)** Switch the window kit off until the doors land. That brings back the milky 30 mm pane and the trims. It also gives the ray-traced glass (G14) nothing to trace.

### 2.5 Wear (W2) — your approval needed

The Blender models carry painted wear: hand grime on the jambs, finish worn off the stool nosing, grit along the stops, the steel sill worn to primer. The game shader ignores it, so every frame looks factory-new in a 1990 building. Approve W2: the surface shader reads the vertex colour (white = no change for every other model). Then we re-shoot the 0.3 m frames.

### 2.6 One profile for doors and windows — decide together

- **Walnut:** a real casing with a 4 mm reveal and a true ranch slope (after T). Use it for the door frames too at their next pass, or keep the windows door-matched and accept the flat read.
- **Steel:** with the trims gone, the true 2-inch hollow-metal face can replace today's 75.5 mm box-like sleeve. Decide it with the steel doors.

### 2.7 Already in the proposal (no new question)

- Call 6, Lobby glass (clear vs hammered).
- Call 7, Office blinds: `Kit_MiniBlind_Raised` and `_Lowered` are in main but not placed.
- Call 8, the LOD change. The importer fix for frames and doors is already in main (frames switch at 4 m and are never culled).

---

## 3. What still looks wrong: owner and test

A1 and A7 were measured again on today's frames. The other numbers come from the 2026-10-03 art review; today's frames match those frames (§1), and the models and materials have not changed since.

| # | What you see | Cause (checked) | Owner | Test before it is called done |
|---|---|---|---|---|
| **A1** | The intact glass does not read as glass. At 1.5 m, intact vs broken differs by only **3.2–5.4 luma** (the old milky cube: 10.4–36.6). In the real game at 1 m: **3.2 (W-L0) / 2.8 (W-OF)**, the same as on 2026-10-04 (2.9 / 2.7), even though G14's ray-traced prepass is now in main (VL107) | The only reflection is the zone cube: no box projection, no blending (`FrontRooms_URP.asset`), so lamps and the room are not reflected where they are. G14 now can trace window glass, but it showed no measurable change in our Play frames | visual chat (probe box projection + blending, desktop only), glass track, G14 | intact vs broken ≥ 12 luma at 1.5 m; one lamp-shaped reflection at 1.5 m and at 45°; not milkier than today |
| **A2** | Lamp highlights on the glass are round bloomed discs (`r6_OF_B_close_head_0.3m_pair.jpg`) | The glass shader lights it with the lamps as points; the real lamps are rectangular troffers | glass track | no round disc at 0.3 m |
| **A3** | The walnut jamb reads as one block: no reveal, the slope rises 2 mm over 55 mm, the back band stands 1 mm | G4 shaped it only to hide the map trims (needs T first) | G4, after T | the reveal reads at 0.3 m and 1.5 m |
| **A4** | The steel reads as matte black plastic. At 1.5 m it sits at luma 22–33 against the wall at 126–141 (about 4.3 stops darker; real dark-bronze paint would be about 3.3) | `Prop_SteelBrown`: smoothness about 0.43, noise normal reads as hammertone | visual chat (the slot is shared with the key door frame) | smoothness 0.65–0.72, albedo luminance 0.045–0.055, orange-peel normal; door readability harness, then the 0.3 m screw frame without the inspection light |
| **A5** | No wear anywhere | the shader ignores vertex colour | visual chat, **your approval (§2.5)** | 0.3 m sill, horn and screw frames |
| **A6** | Frames sit on the wall like stickers: the wall under the stool is only 2–12 % darker | SSAO radius 0.45 m is tuned for rooms, not 25–40 mm relief | visual chat, desktop only | ≥ 15 % darker in the first 1 cm under the stool at 1.5 m |
| **A7** | A ladder of dashes on the 16 mm steel stops at 0.3 m | **Confirmed: shadow-map resolution/filtering.** It is gone with that lamp's shadows off, with hard shadows and with the shadow resolution doubled, and fades with 3× bias (VL108). One shadow map over a 162° lamp cone gives several-mm texels | visual chat (lamp look), desktop only | High shadow tier (or a tighter shadow cone) for shadow lamps within about 3 m of the player; re-shoot `OF_B_close_head_0.3m` |
| **A8** | Up close the glass looks like dirty aquarium glass: 25–50 mm blotches, a green-grey band along the sill | grime prints too big and hard-edged; a 14 cm dust band on vertical glass | glass track | 15–20 mm prints, fewer, softer; band ≤ 0.3; ledge dust moves to the stop tops |
| **A9** | The apron ends are square-cut; long grain on the end faces | not modelled yet (the module text says "returned ends") | G4 | mitred returns at 0.3 m |
| **A10** | A black rubber line on walnut | the kit's glazing slot | visual chat | **done in the test copy** (putty); your call §2.3 |
| **A11** | An extra groove along the steel stop reads as decorative reeding | G4 cut it only to keep the screw heads behind the stop face | G4 + map chat (ask S2 in `03` §5) | groove gone, screws on a plain stop |
| **A12** | Beyond about 1 m the walnut reads as brown paint | grain contrast is about 5 % of the mean; cathedral figure on narrow casing stock | visual chat (shared with the wood door frame) | 2–3× contrast, straight grain on casings and stops |
| **A13** | — (future) | G14 already registers LOD0 only and reads every slot's albedo, so frames will not reflect white or doubled | G14 | one ray-traced frame of each member before G14's acceptance |
| **A14** | Doors keep box trims next to framed windows | promotion order | **you (§2.4)** | — |

---

## 4. What lands, in order (after §2.1)

Details: `04_promotion.md`. Contract for the map chat: `03_contract_map.md`.

1. **Facade r6** (visual chat): a header that says what main does; on a failure it restores the pane; a kill switch. Nothing changes on screen.
2. **Putty** (visual chat), if you pick it (§2.3): the walnut glazing line turns tan.
3. **Map R1 + R2** (map chat): a frame failure logs one warning instead of one error per window; the ray-tracing hint follows the visible glass. Nothing changes on screen.
4. **Map T** (map chat), if you say yes (§2.2): the hidden trim boxes go. Nothing visible changes.
5. **G4 rebuild** of the window models from main's newer Blender modules, then the profile pass (A3, A9, A11).

Already done in main: the importer fix (frames switch to LOD1 at 4 m and are never culled).

---

## 5. Tests behind this (today, main `279c144`)

| Check | Result |
|---|---|
| Windows checked | 411 in 10 seeds (366 walnut, 45 steel; 45 on chunk borders) |
| Aim rays, Relay sight lines, opening after the break, climbs | identical with and without the kit, ray for ray |
| A kit failure | the chunk still builds (25 / 25); that window keeps the old trims and pane; 1 warning |
| A chunk revisited after the map shifts | broken windows come back framed and open (283 / 283) |
| The map chat's own suites | all pass (map 100 / 100, Relay nav 60 / 60, interaction 143, glass shot 20, designer 138, fixtures 28, camera 25, captions 9) |
| The real game in Play | 26 / 26: prompt, crack, break, rebuild, climb |
| Room triangle budgets | worst Office room 85,556 / 120,000; worst other 47,126 / 60,000 |
| Frame rate | 59 fps, p99 17–20 ms with the kit (2026-10-04). **Must be re-measured:** since then the kit's glass also turns on ray tracing in Play, and today's machine was too busy (load 280–660) to measure |
