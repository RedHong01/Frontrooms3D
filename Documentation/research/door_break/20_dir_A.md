# 20 — Direction A · "Jamb Split": pre-render (DB1)

Pre-render only. Nothing landed in the game or in Red's project. The only writes into `Frontrooms3D` are this report, the JPGs / PNG in `images/`, a new section in `media_candidates.md` and the VL180 row in `Documentation/VERIFICATION_LOG.md`.

Made 2026-10-07/08. The first build (2026-10-03/04) lived in the session scratchpad, which the 2026-10-05 reboot wiped. Its scripts were rebuilt from the agent transcripts (replayed and diffed against the last file reads: exact match), and then revised three times after looking at the frames (§7). Verification slide: **VL180** (`2845:6151`) in FRONTROOMS · VISUAL VERIFICATION LOG.

Work folder: `/Users/redwang/FrontRoomsVisualWork/door_break/A/` (`scripts/`, `kit/`, `models/`, `tex/`, `renders/`, `review/`). Concept assets only: nothing here is a kit module yet.

---

## 0. Short answer

- **The door fails where a real 1990 door fails: at the latch.** The leaf stays whole. Wood splits along the grain of the jamb; steel dents, bows and crushes at the lock edge while its frame spreads.
- **Light is the only hole.** Each blow flashes a line of light along the latch edge. From D1 the line stays (15 %), at D2 it widens and two bright wedges open at the top and bottom latch corners (40 %), at FINAL the whole edge flares (100 %), and at the break the far room floods the opening with the Relay back-lit in it.
- **Each blow leaves a mark that stays,** like a glass crack: a print with lacquer crazing (wood) or a dent with enamel star-crazing (steel) on the struck face, then splits, the lifted casing, splinter teeth, and the torn strip.
- **Best read: 1–4 m in L-B** (dim near room, lit far room). In a dead far room (L-D) the light cue is gone, and at 12 m (V6) the line is a 1 px core (26× the door's luminance) inside a ~10 px glowing veil: it still reads as a lit seam, but the damage does not. Sound and the reveal carry those cases.
- **Cheapest of the three to build:** two stage leaves per member, one strike-zone insert per frame type, one casing transform, ≤ 40 baked debris pieces, one leak-card strip and one pooled breach light (§5, §6).

---

## 1. Files

All in `Documentation/research/door_break/images/` (JPG q85; the 12 m loupe is PNG, nearest-neighbour).

**Lead set (`dir_A_*`, for the slides):**

| File | What |
|---|---|
| `dir_A_strip.jpg` | **The lead strip.** L0-F BURST under L-B at the slide FOV (V1 45°), 3840 × 720: S0 → M2 → D1 → D2 → FINAL → S4 |
| `dir_A_D2_V1-45_LB_wood.jpg`, `dir_A_D2_V1-45_LB_steel.jpg` | Heroes: D2 under L-B, the steady line, the two corner wedges, the dust veil |
| `dir_A_REVEAL_V1-45_LB.jpg` | 0.1 s after the break: leaf at 100°, debris in the air, Giant B in the doorway, back-lit, lens on |
| `dir_A_D1_V2_wood.jpg`, `dir_A_D2_V2_wood.jpg` | Latch side at 2.2 m: D1 (splits, strike crack) and D2 (splinter teeth, lifted casing, torn paper) |
| `dir_A_D2_V3_wood.jpg`, `dir_A_D2_V3_steel.jpg` | Close-ups at 0.55 m: veneer splits, splinters, strike on one screw / crushed lock edge, gypsum breakout |
| `dir_A_S4_V5_wood.jpg`, `dir_A_S4_V5_steel.jpg` | The floor after the break: strip, strike, screws, splinters / chips, crumbs, silencer |
| `dir_A_D2_V4L_relay.jpg` | P side, latch-side three-quarter: Giant B mid-blow, fist on the leaf 0.25 m from the latch edge at 1.30 m |
| `db_A_review_heroes_D2_LB.jpg`, `db_A_review_it1_it4.jpg`, `db_A_review_12m_x4.png` | Review sheets on VL180: the two heroes; the passes 1→2 and 3→4 before/after; the 12 m read at 4× (nearest) |

**Stage strips (3840 × 720, six 640 × 720 panels, V1):**

| File | Panels |
|---|---|
| `dir_A_strip.jpg` | L0-F BURST, L-B, V1 45°: S0, M2, D1, D2, FINAL, S4 |
| `db_A_L0F_burst_strip_V1-76_LA.jpg` | L0-F BURST, L-A, V1 76° (the game's view): S0, M2, D1, D2, FINAL, S4 |
| `db_A_OFK_burst_strip_V1-76_LC.jpg` | OF-K BURST, L-C, V1 76°: S0, M2, D1, D2, FINAL, S4 |
| `db_A_L0F_rip_strip_V1-76_LA.jpg` | L0-F RIP seen from P, L-A, V1 76°: S0, M1, D1, D2, FINAL, S4 |
| `db_A_OFK_rip_strip_V1-76_LC.jpg` | OF-K RIP seen from P, L-C, V1 76°: S0, M1, D1, D2, FINAL, S4 |

**Frames (1920 × 1080, `db_A_<member>_<burst|rip>_<stage>_<view>_<light>.jpg`):**

26 frames. Brief item → files:

| Brief | Wood (L0-F) | Steel (OF-K) |
|---|---|---|
| 3 HERO, V1 45°, D2, L-B | `db_A_L0F_burst_D2_V1-45_LB` | `db_A_OFK_burst_D2_V1-45_LB` |
| 4 V2, D1 / D2 (+ FINAL) | `db_A_L0F_burst_{D1,D2,FINAL}_V2_LA` | `db_A_OFK_burst_{D1,D2,FINAL}_V2_LC` |
| 5 V3, D1 / D2 / S4 | `db_A_L0F_burst_{D1,D2,S4}_V3_LA` | `db_A_OFK_burst_{D1,D2,S4}_V3_LC` |
| 6 V4, D2 BURST, empty (P-face prints, stops intact) | `db_A_L0F_burst_D2_V4-45_LA` | `db_A_OFK_burst_D2_V4-45_LC` |
| 6 V4 with the Relay mid-blow | `db_A_L0F_burst_D2_V4-76_LA_relay`, `db_A_L0F_burst_D2_V4L_LA_relay` | — |
| 7 V5, S4 | `db_A_L0F_burst_S4_V5_LA` | `db_A_OFK_burst_S4_V5_LC` |
| 8 Reveal | `db_A_L0F_burst_REVEAL_V1-45_LB` | — |
| 9 V6, S0 / D2, L-B (S0 with the 1.80 m person) | `db_A_L0F_burst_{S0,D2}_V6_LB` | — |
| extra: RIP from P under L-B (the slot along the stop) | `db_A_L0F_rip_D2_V1-45_LB` | `db_A_OFK_rip_D2_V1-45_LB` |
| extra: the weak case, far room dead (L-D) | `db_A_L0F_burst_D2_V1-45_LD` | — |

Render settings: Blender 4.3 Cycles (Metal GPU), 1920 × 1080 at 256 samples (strip panels 640 × 720 at 192), adaptive sampling 0.008, OpenImageDenoise (albedo + normal), AgX view transform, mild bloom (threshold 1.5) for the game's halation, −22 % saturation on Office S views (FrontRoomsLook). No motion blur (the game has none).

**Exposure, calibrated to the in-engine audit frame `77_relay_broken_witness.png`:** measured on the final frames, the L-A wallpaper's median linear luminance is 0.21–0.26 against 0.18–0.24 on frame 77's walls: −0.05 stop (V1 strip), +0.06 (V4) and +0.13 (V2), inside ±0.3 stop at exposure −1.15. L-B frames are lifted 0.25 stop only (−0.9), so the dim near room stays dim.

---

## 2. What was modelled

### 2.1 Set and intact kit

- **Set** (10_design §8.0): a 0.16 m wall on a cell line, the door centred in a 3 m cell edge. P = Level 0 Low (2.4 m ceiling), S = Level 0 Standard (2.9 m) for L0-F, Office (2.9 m, drywall, carpet tile, louvre lamps) for OF-K. Each side 6 × 6 m (Z −1 → 5). A 12 m corridor on S for V6. Troffers 0.6 × 1.2 m (area light + emissive lens, colour (1, 0.96, 0.88), 95 W per lamp, Office ×1.1). Thin dust volume in both rooms (density 0.0035; 0.006 and a denser 0.04 band at the door under L-B). One 1.80 m person silhouette in V6 S0. Surfaces are the project's own `Assets/Resources/Surfaces` textures (Chevron wallpaper, loop-pile carpet, fissured ceiling, drywall, carpet tile, 2 × 2 ceiling, troffer lens, louvre, DoorVeneer, PaintedMetal), frozen on 2026-10-07.
- **Intact kit** built with Blender from the current modules in `Tools/Blender/frontrooms_kit/assets/` (read-only import; last changed 2026-10-03, commit ef061ae), placed by the spec's facade table:

| Kit part | Tris | Member |
|---|---|---|
| `Kit_DoorFrame_Wood` (52 parts kept separate: casings, linings, stops, hinges) | 3,152 | L0-F |
| `Kit_DoorLeaf_Veneer` (44 mm solid core, 31 parts) | 2,464 | L0-F |
| `Kit_Lock_Rose` ×2, `Kit_Lock_Lever` ×2, `Kit_Lock_Latchbolt_Bored`, `Kit_Lock_StrikeBored` | 2,728 / 3,974 / 476 / 674 | L0-F |
| `Kit_DoorFrame_Steel` (48 parts) | 3,866 | OF-K |
| `Kit_DoorLeaf_Steel` (SDI hollow metal, 45 parts) | 3,930 | OF-K |
| Escutcheon, knob, cylinder shell, plug (×2 faces), deadbolt, mortise latch, mortise strike | 3,176 / 4,608 / 2,708 / 1,884 / 380 / 600 / 988 | OF-K |
| Closer body, arm, forearm, shoe; door sign, number plate (×2 faces) | 3,236 / 1,112 / 940 / 570; 664 / 480 | OF-K |
| Giant B "Night Shift" (`creatures/giant_b_night_shift.py`): pose `door`, and `blow` (the `low` pose, left fist on the leaf at Z 0.745 (0.25 m from the latch edge), Y 1.30; right hand braced lower) | 15,260 / 15,328 | Relay frames |

The Relay's materials are the creature module's preview look, lens emission on. If Red picks another body in W1, only the four Relay frames need re-rendering.

### 2.2 Wood: L0-F (W-W), stages

All positions in the door root frame (Unity m: +Z along the opening to the latch, +Y up, +X = S). Every stage contains the earlier ones.

| Stage (tier 1) | What changes |
|---|---|
| **S0** | The kit door. |
| **M1** 0.5 s | Print 1 on the P face (Y 1.36, Z 0.80; 150 × 190 mm; 1.2 mm deep) with lacquer alligator crazing and a stress-whitened centre. Leaf on its jolt (latch 4 mm toward S, top corner +5, bottom +4). S casing lifts 0.5 mm. Light pulse 100 %. Dust from the head and a fibre puff at the strike. |
| **M2** 1.0 s | Print 2 (Y 1.18, Z 0.84; 1.5 mm). A 100 mm varnish hairline on the jamb at the strike, a 120 mm hairline in the wallpaper at the casing edge. Casing 1 mm. Jolt 5 mm. Pulse 100 %. |
| **D1** 1.5 s | Print 3 (2.0 mm). **Three veneer splits on the S face** along the grain (Z 0.905 / 0.948 / 0.872; 130, 130, 90 mm long; V-section cuts 3 mm deep, 0.3–0.8 mm jagged walls, whitened lips). **Jamb crack** 300 mm above and below the strike on the S side of the mortise, running 120 mm past the strike-zone insert. **Strike tilted** 2.5° / 2.5° and out 1.2 mm. **S latch casing lifted 2.5 mm** with a split along its grain 6–14 mm in from the edge (420 mm). Leaf at rest 1.5 mm off. Light steady 15 %. |
| **D2** 2.0 s | Print 4 (2.5 mm). A fourth split (Z 0.930, 140 mm). **Jamb strip loose:** 24 × 20 × 460 mm (Y 0.775–1.235), a separate body hinged on its top nails, open 4 mm at its foot. **Strike on one screw** (turned 24° about its top screw). **S casing 16 mm off the wall, tilted 1.5°,** its inner edge split part-depth (4–10 mm) over Y 0.80–1.24. **6 splinters** 50–210 mm long, 4.6–6.8 mm thick, lifting 15–30° toward S, plus 9 short teeth (12–35 mm) along the split. **Wallpaper torn** 3–20 mm along the casing edge over 1.0 m. Latch edge rubbed and dented 1.5 mm at 1.0 m. Leaf at rest 4 mm off at the latch, the top corner 14 mm, the bottom 12 mm: the two **corner wedges**. Light steady 40 %. |
| **FINAL** 2.4 s (shown 0.05 s after the blow) | The leaf is still shut, at full jolt (8 mm; corners 23 / 20 mm). **In flight toward S:** the strip with the strike (2.6 m/s, spin 14 rad/s), 4 screws (2 brass strike screws, 2 steel), 12 splinters (30–160 mm, 3–9 mm thick, 2–4.5 m/s), 10 veneer flakes. The casing split goes full depth (10–20 mm). 7 teeth stand in the notch. Light 100 %. |
| **S4** aftermath | Leaf at 80° on S. **Top hinge torn:** its frame half follows the leaf, 2 screws hang out with fibres on the threads, 3 torn screw holes; the leaf **drops 0.8° at the latch** (the top of the hinge edge opens 24.9 mm). **Raw notch** 20 mm deep, 460 mm tall, with teeth (the wall end is recessed 25 mm behind the insert: CONTRACT 7.1 item 8). **Hinge-side S casing cracked** (550 mm crack and a branch) where the leaf hit it at ~105°. **On the S floor within 1 m of the latch jamb:** the strip with the strike still on one screw, 5 screws (3 with fibres), 13 splinters (30–180 mm), 9 veneer flakes, 2 bent nails. Nothing in the opening (§4). |
| **RIP** (from P) | The player's lever jerks 30° (M1) and 32° (M2), comes back 3° low (D1), hangs 15° low and loose (D2, S4). The leaf leaves the P stop: 3.5 mm at 1.0 m (D1), 7.5 mm (D2): a slot of light along the stop. Lacquer crazing round the S rose (the Relay's grip). |

### 2.3 Steel: OF-K (S-S), stages

| Stage | What changes |
|---|---|
| **M1** | Dent 1 on the P face (Y 1.34, Z 0.79; Ø 180 mm, 2.5 mm deep; a smooth dish) with **enamel star-crazing**: 7–11 straight radial cracks, 2–3 broken ring arcs, a few chips lifted to grey primer at the centre. The S face bulges 45 % of the depth (1.1 mm) with no paint damage (10_design §3.3: marks on the struck face only). Jolt 3 mm. Pulse 100 %. |
| **M2** | Dent 2 (Ø 220 mm, 3.5 mm). |
| **D1** | **The broad dish round the lock** (Ø 380 mm, 5 mm) and dent 4. **Leaf bowed 7 mm** (a sine along the lock stile, Z 0.30 → 0.97). **Frame spread 4 mm** at the strike (Gaussian, σ 0.35 m about Y 0.97). **One silencer pops** (Y 1.30) onto the P floor. **Diagonal drywall cracks from both head corners** (220 and 260 mm, both faces) and joint cracks along both face bands. Hairline reverse-impact stars on the S bulges. More chips to primer on P. Light 15 %. |
| **D2** | Dent 5 (Ø 240 mm, 4 mm). **Bow 9 mm, spread 6 mm.** **Lock edge crushed 6 mm** over Y 0.73–1.27. **The S face sheet tears from the lock seam and folds out** 24–40° over ~0.42 m (0.9 mm sheet, enamel outside, primer and bare steel inside); the notch behind it is up to 17 mm deep and shows the **brown 12 mm kraft honeycomb** (real cell walls, crushed toward the edge). Armor front pushed in. Strike bent 5°. Head-corner cracks grow to 380 / 440 mm. Four short cracks and a **gypsum breakout** at the latch face band, 7 gypsum lumps pushed out of the wall, 10 crumbs and powder on the S floor. Light 40 %. |
| **FINAL** | **Deadbolt bent 14° and sheared** (its tip stays in the strike), latch bent 11°. In flight: 22 paint chips (almond over primer), 9 gypsum crumbs, 1 silencer. Light 100 %. |
| **S4** | Leaf at 80°, **hanging square** (no drop: the welded hinges hold). Bow 10 mm, spread 7 mm, crushed edge, honeycomb, sheared bolt tip in the strike. **Closer forearm torn from its shoe,** hanging from the arm. **Both floors:** S 46 chips, 16 crumbs (3 large with the paper face), a silencer, powder; P 22 chips, 9 crumbs, a silencer, powder. |
| **RIP** | The player's knob twists 4.5° and stops (M); turns loose 7° / 3° (D2, S4). Hairline star round the S escutcheon from D1. The slot along the stop as in wood. |

### 2.4 Light (render stand-ins for the in-game leak)

- **Leak cards:** emissive strips in each gap's exit plane: the latch edge (S face plane, the full 2.07 m, brightness following the real gap width and pinched to 10 % where the bolt still crosses it), the head and sill **wedges** (Z 0.80 → 0.995, brightness ∝ s³ toward the latch corner), and in RIP the slot along the P stop. Strength = 70 × stage level × far lamp level × gain. Stage level: M 100 % (the pulse), D1 15 %, D2 40 %, FINAL 100 %. Gain 0.22 when both rooms are lit (a lit seam, not a neon tube), 0.35 under L-B.
- **Under L-B:** the breach spot (260 W, 650 W at the reveal) 1.2 m behind the door at 1.8 m, aimed through it. At the gap: a wide, weak slit light spills light onto the casing and carpet; a thin **emission-volume veil** (dust lit by the gap, fading within ~0.17 m and fanning toward +Z) stands in for the game's additive beam cards; the head wedge throws a streak up onto the near ceiling and the sill wedge a small fan onto the carpet.
- **Puffs:** a dust puff at the head and a fibre (wood) or gypsum (steel) puff at the strike on the blow frames (M, FINAL).

---

## 3. Counts per stage

Counted from the saved stage models (`door_break/A/models/*.blend.json`, script `stats_models.py`; the same numbers as the render sidecars): only the door's own objects (kit + damage), not the set, the lamps, the Relay or the dust. Cells read **pieces / triangles**. These are **render meshes**: the leaf slab is cut to ~41k (wood) / ~58k (steel) triangles and the steel kick plates to 3.4k each, so the prints, dents and the bow can be pushed into real geometry. The game budgets are in §6.

| Member | Case | Stage | Door tris (render) | Leaf | Strike zone | Casings | Hardware | Splinters / teeth | Debris (flying / floor) | Leak cards | Cracks |
|---|---|---|---|---|---|---|---|---|---|---|---|
| L0F | BURST | S0 | 65,150 | 31 / 41,164 | 4 / 3,218 | 6 / 3,426 | 51 / 17,342 | — | — | — | — |
| L0F | BURST | M1 | 65,710 | 31 / 41,164 | 4 / 3,218 | 6 / 3,426 | 51 / 17,342 | — | — | 3 / 560 | — |
| L0F | BURST | M2 | 65,810 | 31 / 41,164 | 4 / 3,218 | 6 / 3,426 | 51 / 17,342 | — | — | 3 / 560 | 2 / 100 |
| L0F | BURST | D1 | 66,448 | 31 / 41,202 | 4 / 3,218 | 6 / 3,426 | 51 / 17,342 | — | — | 3 / 560 | 6 / 700 |
| L0F | BURST | D2 | 69,182 | 31 / 41,508 | 4 / 3,218 | 6 / 4,304 | 51 / 17,342 | 15 / 1,730 | — | 3 / 560 | 4 / 520 |
| L0F | BURST | FINAL | 80,660 | 31 / 41,508 | 4 / 3,218 | 6 / 4,426 | 51 / 17,342 | 7 / 886 | fly 26 / 12,200 | 3 / 560 | 4 / 520 |
| L0F | BURST | S4 | 90,114 | 31 / 41,462 | 4 / 3,218 | 6 / 4,426 | 59 / 22,478 | 7 / 886 | floor 38 / 16,850 | — | 9 / 794 |
| L0F | RIP | D2 | 68,944 | 31 / 41,510 | 4 / 3,218 | 6 / 4,304 | 51 / 17,342 | 15 / 1,730 | — | 1 / 320 | 4 / 520 |
| OFK | BURST | S0 | 106,502 | 45 / 67,770 | 1 / 620 | — | 66 / 38,112 | — | — | — | — |
| OFK | BURST | M1 | 107,062 | 45 / 67,770 | 1 / 620 | — | 66 / 38,112 | — | — | 3 / 560 | — |
| OFK | BURST | M2 | 107,062 | 45 / 67,770 | 1 / 620 | — | 66 / 38,112 | — | — | 3 / 560 | — |
| OFK | BURST | D1 | 128,582 | 45 / 67,770 | 1 / 22,100 | — | 65 / 38,032 | — | floor 1 / 120 | 3 / 560 | — |
| OFK | BURST | D2 | 136,954 | 47 / 74,780 | 8 / 22,660 | — | 65 / 38,032 | — | floor 12 / 922 | 3 / 560 | — |
| OFK | BURST | FINAL | 137,370 | 47 / 74,782 | 9 / 22,672 | — | 65 / 38,032 | — | fly 32 / 1,324 | 3 / 560 | — |
| OFK | BURST | S4 | 139,304 | 47 / 74,784 | 9 / 22,672 | — | 65 / 38,032 | — | floor 97 / 3,816 | — | — |
| OFK | RIP | D2 | 136,712 | 47 / 74,778 | 8 / 22,660 | — | 65 / 38,032 | — | floor 12 / 922 | 1 / 320 | — |

How to read it:
- **Wood (L0-F):** the stages add little geometry until the break. M1 adds the 3 leak cards (560 tris); the prints are displacement on the slab. D1 adds 3 split cutters (boolean, +38 tris on the slab) and 6 crack strips. D2 adds the loose strip (inside the strike-zone insert), **15 standing splinters / teeth (1,730 tris)** and the lifted, split casing (+878). FINAL throws **26 pieces (12,200 tris)**: the strip, the strike, 4 screws, 12 splinters, 10 veneer flakes. S4 leaves **38 floor pieces (16,850 tris)**, 7 teeth in the notch, the torn top hinge (+8 hardware pieces: screws, the frame half) and 9 crack strips.
- **Steel (OF-K):** D1 densifies the latch jamb for the 4 mm spread (+21.5k, render-only; the game bakes it into the strike-zone insert) and pops a silencer (1 floor piece). D2 adds the folded seam lip and the honeycomb (+7k on the leaf), 7 stuck gypsum lumps and 12 floor pieces. FINAL throws **32 pieces (1,324 tris)**: 22 chips, 9 crumbs, a silencer. S4 leaves **97 floor pieces (3,816 tris)** on both floors. The drywall cracks are in the wall's damage atlas, not geometry.
- **RIP** differs from BURST by the missing P-face prints / dents, the player's lever or knob, and one slot card along the P stop (320 tris) instead of the three S-side cards.
- The render sidecars of the 26 frames and 30 strip panels (`renders/*/*.png.json`) hold the same per-object counts for every image. The OF-K RIP S0 and S4 strip panels are from the pass-3 build (kick plates not yet subdivided, −6.8k); they were not re-rendered because their plates show no fault.

**Stage models (concept assets):** `door_break/A/models/DB_A_<member>_<burst|rip>_<stage>.blend` (16 files: BURST S0, M1, M2, D1, D2, FINAL, S4 and RIP D2 for each member; 6.6–13.1 MB each), each the full set with the staged door, plus a `.blend.json` with the per-object triangle counts. They are built by the same scripts as the frames (`dbA_job.py` with `save_blend` and `no_render`). The intact kit parts the stages start from are in `door_break/A/kit/` (`Kit_*.blend` + `.json`). Textures are referenced from the work folder (`tex/` and the frozen `Surfaces` copies), not packed.

---

## 4. Checks (10_design §8.0)

| Check (10_design §8.0) | Result | How |
|---|---|---|
| After the break nothing in the 1.0 × 2.1 opening except the leaf at 80° | **PASS** (both members) | `check_s4.py` builds S4 and tests every vertex of every non-leaf mesh against the clear passage inside the stops (X ±0.08, Y 0.02–2.08, Z 0.02–0.98). Wood: 0 intrusions. Steel: the first run found the strike hanging 2 cm into the opening (8 vertices); fixed (§7 pass 3), re-run: 0 |
| Leaf angle at S4 | **PASS**: 80.0° both | same script (rig rotation) |
| Wood leaf drops ~1° at the latch, torn top hinge, 20–30 mm gap at the top of the hinge edge | **PASS**: drop 0.8°, gap 24.9 mm | same script (drop pivot, hinge-edge top corner) |
| Steel leaf hangs square | **PASS**: drop 0.0°, gap 0 mm (the welded hinges hold) | same script |
| Stages cumulative | **PASS** | each stage builder adds to the previous ones by stage index (`si >= n`), never replaces; checked by eye on every strip: every mark of a stage is still there in the next |
| Damage on the correct side (BURST: splinters, debris toward S; marks on the struck face) | **PASS** | splinters lift 15–30° toward S; FINAL throws move +X (S); S4 floor debris lies within 1 m of the latch jamb on S (wood) and on both floors (steel: chips and a silencer fall on P too). Prints and dents on P (V4 frames); S face shows only the reverse bulges and hairline stars |
| RIP: lever jerk 20–35° in M; slot 2–5 mm (D1) → 5–10 mm (D2) along the stop at 1.0 m | **PASS**: lever 30° (M1) / 32° (M2); slot 3.5 mm (D1), 7.5 mm (D2) | stage numbers in `dbA_marks.py`; the slot card follows the gap |
| Nothing after 1990, no brands | **PASS** | door sign and number plates blank; no logos, labels or printed dates on any part; lockset, closer and hardware are the kit's period parts (era lock `22_era_lock.md`) |
| Colliders truthful | **PASS (by design)** | the leaf stays whole and keeps its 0.05 × 2.08 × 0.98 box; all damage and debris is render-only; the opening stays clear after the break |
| Exposure within ±0.3 stop of audit frame 77 | **PASS**: −0.05 to +0.13 stop | §1 |
| Kick plates stay on the face as the leaf bows / jolts | **FAIL→FIXED** (pass 4) | §7 pass 4; 16 steel frames re-rendered, checked on every steel frame |

**Light line contrast** (median over the door's upper band, line column vs the door 12–40 px to its left; linear luminance):

| Strip | S0 | M2 | D1 | D2 | FINAL |
|---|---|---|---|---|---|
| `dir_A_strip` (L0-F, L-B, 45°) | — | 12.3× | 22.9× | 25.0× | 20.6× |
| `db_A_L0F_burst_strip_V1-76_LA` (L-A, 76°) | — | 4.4× | 2.6× | 3.9× | 4.4× |
| `db_A_OFK_burst_strip_V1-76_LC` (L-C, 76°) | — | 3.2× | 1.4× | 2.0× | 2.8× |

Under L-B the M2 pulse also lifts the near room (the door goes from 0.024 to 0.059), so its ratio is lower than D1's although the line is brighter. In lit rooms the line is a lit seam (2.6–4.4× wood, 1.4–3.2× steel); OF-K D1 under L-C (1.4×) is the weakest read in the set.

---

## 5. How it would be built for real

Nothing here is in the game. This is the plan if Red picks A. It follows 10_design §4 and changes none of its rules.

### 5.1 Stage meshes (one model per state, swapped on a blow)

| Asset | Made from | What changes | Notes |
|---|---|---|---|
| `Kit_DoorLeaf_Veneer_D1 / _D2 / _Broken` | `interact_door_leaf_veneer.py` + a `STAGE` parameter | P-face prints (dish 1.2–2.5 mm), S-face veneer splits (V-cuts 3 mm deep), latch edge rub and dent at 1.0 m | Outside the damage zone (Z < 0.62 or Y outside 0.6–1.7) the vertices, UVs and slots stay identical to the intact leaf, so a swap only changes the damage zone |
| `Kit_DoorLeaf_Steel_D1 / _D2 / _Broken` | `interact_door_leaf_steel.py` + `STAGE` | dents (smooth dishes), the lock-stile bow (sine, 7–10 mm), the crushed lock edge, the folded S sheet, the honeycomb as real geometry | The bow is baked into the vertices; no blend shapes (the Animation module is not installed). The kick plates and any other face-mounted plate must take the same bow, or the face cuts through them (the pass-4 fault, §7) |
| `Kit_DoorFrame_Wood_StrikeZone_D0…_Broken` | new module, the latch jamb Y 0.62–1.38 | split line, strip, splinter teeth, raw notch | Needs the 25 mm wall-end recess (CONTRACT 7.1 item 8) for real depth |
| `Kit_DoorFrame_Steel_StrikeZone_D0…_Broken` | new module | spread 4–7 mm, bent strike, popped silencer, sheared bolt tip | |
| `Kit_DoorCasing_Wood_LatchS` | split out of `Kit_DoorFrame_Wood` | transform only: lift 0.5 → 16–17 mm, tilt up to 1.8° | no new triangles; the part-depth split is in the `_D2` insert |
| `Kit_DoorFrame_*_HingeZone_Broken` | new | cracked hinge-side casing (wood), torn top hinge screws (wood), dented face band (steel) | S4 only |

### 5.2 Pre-fracture and debris (baked, like RE7)

- The jamb strip, the strike, 2–4 screws and 8–15 splinters are **pre-cut pieces** of the strike-zone insert. They exist from D2 (the strip, the teeth) and are released at FINAL.
- Throws are baked in Blender (transform keys, 30 Hz, ≤ 1.5 s): outward from the strike plus the leaf's own speed, toward S (the rule the FINAL and REVEAL renders use).
- After landing, the floor pieces merge into one static mesh per door (1 draw) and come back the same after a chunk rebuild.

### 5.3 Marks (the pops on M blows)

- One 1024² atlas `Door_DamageMarks` with the channels these renders use: R crack line, G whitening / grey primer / torn paper fringe, B raw wood / bare steel / backing, A height.
- Render-only overlay quads, 1 mm proud of the faces, alpha-clipped. ≤ 4 per blow, ≤ 24 per door, 1 draw. No URP decals and no shader change (10_design §4.2).

### 5.4 Particles

- Per blow: dust from `head_dust_a/b` and a puff at the strike (fibre in wood, gypsum in steel). At FINAL: the big burst. Needs the particle module (the shared ask with the glass). Without it: camera-facing cards.

### 5.5 Light

- **Leak cards** in the gaps' exit planes, as rendered. Brightness = far lamp colour × far lamp level × stage curve (pulse 100 % on a blow, decay 0.12 s; steady 15 / 40 / 100 %). 1 draw.
- **Beam cards** (additive) hugging each gap, fading over ~0.2 m (the renders' emission veil).
- **Breach light** (desktop only): one pooled shadowed spot 1.2 m behind the door at 1.8 m, from D1 to the reveal, flaring at the break.
- **Bloom** is the game's own halation (FrontRoomsPostStack). The renders add a mild bloom to match it.

---

## 6. Costs (ESTIMATES; desktop is the reference, WebGL a separate reduced tier)

The renders' meshes are densified for displacement (the veneer slab is cut to ~41k tris so the prints and the bow can be pushed into it); the game assets would bake the same shapes into the budgets of 10_design §6.

| Item | A, wood (L0-F) | A, steel (OF-K) | WebGL |
|---|---|---|---|
| Stage leaves (`_D1`, `_D2`, `_Broken`) | ≤ 4k / 6k / 7.5k tris (intact 2.5k) | ≤ 6k / 9k / 10k (intact 3.9k; the honeycomb is ~0.9k) | LOD1 each, ≤ 1.5k / 2k |
| Strike-zone insert (`_D0…_Broken`) | 0.4k / 1.2k / 3k / 2.5k | 0.4k / 0.8k / 1.5k / 1.5k | ≤ 0.6k |
| Casing | transform only | — | same |
| Standing splinters / teeth (D2) | 15 pieces, ~1.7k tris (rendered: 15 / 1,730) | — | 6 pieces, card-like |
| Marks atlas | 1 × 1024², ≤ 24 quads, 1 draw | same | 512² |
| Leak cards | 3 strips (latch, head, sill), 1 draw (rendered: 560 tris) | same | same |
| Beam cards | 1–3 additive cards at D2 | same | none or 1 |
| Breach light | 1 shadowed spot, D1 → reveal (≤ 3 s), desktop only | same | none |
| Baked debris at FINAL | 26 pieces (strip, strike, 4 screws, 12 splinters, 10 flakes; rendered 12.2k tris, game LOD ≤ 4k) | 32 pieces (22 chips, 9 crumbs, silencer; ~1.3k tris) | ≤ 12, one draw |
| Persistent floor (S4) | 38 pieces merged, rendered 16.9k tris → game ≤ 3k, 1 draw | 97 pieces (68 chips, 25 crumbs, 2 silencers, powder) merged, rendered 3.8k tris, 1 draw | ≤ 800 tris |
| Build work (visual chat, ESTIMATE) | stage parameter in the leaf module, a strike-zone insert module, debris bake, marks atlas: ~3 days | dents/bow/seam in the leaf module, frame insert, debris: ~2 days | shared |

**Why A is the cheapest:** no hole through the leaf (the collider and the E ray stay true), no new rig beat (B's peek) and no wall or ceiling overlays (C). The most expensive new thing is the breach light, and A needs it most, because its drama is light.

---

## 7. Iteration log (look, judge, fix)

Every frame was looked at. Review sheets are in the work folder `review/` (passes 1–3) and `review/final3`, `review/final4` (pass 4); the before/after pairs are on the verification log (VL180, `2845:6151`).

**Pass 1 (2026-10-07 16:30–17:46, recovered 2026-10-03/04 scripts, previews 960 × 540 at 48 samples):** the stages were all there, but six things read wrong:
1. The leak was a white neon tube even with both rooms lit (L-A): a 3 mm gap into an equally lit room shows a lit seam, not a light source.
2. Under L-B the "beam" was a broad smudge on the wallpaper (a 2 m slit light with a 14° cone lit the dust in a blotchy patch).
3. Large arcs on the walls in V4 and V6: a dust volume's face sat exactly on a wall (z-fighting). Fixed at the end of pass 1 (volumes inset 3 mm).
4. The steel star-crazing read as cobwebs (many wiggly branched rays and a dotted centre net), and it was on the S face from the first blow, against 10_design §3.3.
5. Wood splinters read as pale hairs (thin, 40–60° out) and fresh-split wood was paper-white.
6. The steel lock-edge seam and the gypsum breakout were invisible or read as a white lace patch.

**Pass 2 (17:00–19:25, previews):**
1. Leak gain 0.22 in lit rooms; the head and sill wedges shortened (Z 0.80 → 0.995) and made steeper (brightness ∝ s³).
2. The long beam replaced by a local spill (wide, weak slit light) plus a thin glowing veil of dust at the gap (the game's beam cards); the head wedge now throws light up onto the ceiling and the sill wedge down onto the carpet.
3. Steel marks rewritten: 7–11 straight, tapering rays, 2–3 broken ring arcs, chips only at the crushed centre and the outer ring, a thin dark rim round each chip; S face gets hairline stars only, from D1.
4. Splinters 9–15 mm wide, 4.6–6.8 mm thick, lifting 15–30° along the grain; raw wood darkened (ramp 0.30 → 0.56 linear).
5. Steel D2: the notch up to 17 mm deep with the honeycomb inside, the S face sheet folded out as a real 0.9 mm lip, the breakout darker with 7 gypsum lumps pushed out of the wall.
6. Exposure calibrated to frame 77 (−0.35 stop), L-B lifted 0.25 stop only.
7. Motion blur dropped (the game has none; its Metal kernels took > 40 min to compile on the shared machine).

**Pass 3 (finals, from 19:23):**
1. The L-B lead strip showed M2 and D1 as the same white line. L-B leak gain lowered to 0.08 so the stages separate: M2 a bright pulse with flashing corners, D1 a thin dim line, D2 the line with two wedges and the dust veil, FINAL the brightest with the debris. M2 and D1 re-rendered.
2. `check_s4.py` found the steel strike, hanging on one screw at S4, 2 cm inside the clear opening (8 vertices). At S4 it now hangs flatter on its screw; recheck: nothing in the opening.
3. Review of the finals (2026-10-07 23:2x, an interrupted attempt; picked up 2026-10-08): every final frame and strip looked at, as sheets and with 2× latch crops.

**Pass 4 (2026-10-07 23:30 → 2026-10-08 00:05, finals):**
1. **Steel kick plates (fault).** The two 0.9 m stainless kick plates were moved as rigid followers by the leaf field at their centroid, while the slab under them bowed (5–10 mm, a sine along the lock stile) and jolted (3–8 mm at the latch). From M2 on the S face poked through the S plate at the latch end: a curled-sheet shape in V2 (it read as damage), a plate cut short in the heroes and the strips; on P the plate's hinge end was cut on a slant. Now the plates are subdivided (20 mm grid) and deformed per vertex by the same field as the slab (`deform_like` in `dbA_stage.py`). Test at 48 spp, then 16 steel frames re-rendered at full quality: the hero, V2 D1 / D2 / FINAL, V4-45, V5 S4, RIP D2 V1-45, BURST strip M2–S4 and RIP strip M1–FINAL. Checked on every one: the plates are whole and flat on the face. Before / after: `db_A_review_it1_it4.jpg`, bottom row.
2. Nothing else needed a change. The wood stages and the L-B lead strip read as intended; the honest weak spots (steel from S in lit rooms, 12 m, dead far room) are listed in §8.

---

## 8. Open issues

1. **The beams are art direction.** A 44 mm deep, 3–5 mm wide gap lets almost no direct light through from a lamp 1.2 m behind the door's centre; what a real gap shows is the far side's lit surfaces (a glowing seam) plus a little spill. The renders keep that honest seam, and add only a thin glowing veil of dust within ~0.2 m of the gap (the in-game "beam cards", 10_design §4.5). Long beams across the room would need the breach light at the latch side and a real slot: a staging choice for Red.
2. **The light cue needs a lit far room.** In L-D (far room dead) A has nothing to show from S until the reveal, and the Relay's lens only appears at the break. B (the lock hole) and C (the frame) keep a visible cue in the dark; this is A's main risk (10_design §8.1).
3. **12 m read (V6, L-B, D2):** the door is ~60 × 125 px; the line is a 1 px core at 26× the door's luminance (0.52 against 0.02) with a ~10 px dust veil at ~5×. It reads as "something is lit behind that door", not as damage: the splinters and the lifted casing are 1–2 px. S0 → D2 is still an obvious change at 12 m (`db_A_review_12m_x4.png`). Under L-A the same line is only ~3× the door, so at 12 m in a lit corridor A is close to invisible: sound has to carry it.
4. **Steel D2 from S is subtle.** The folded sheet and the honeycomb are in plain view only from V2/V3 (1–2.5 m). From V1 the steel door says "dented and bowed" mostly through the line's width (the bow widens the gap to 6–9 mm at mid-height) and the cracked drywall. A stronger read would need the seam to open on the S face (an ESTIMATE until `da01` is seen).
5. **Steel marks are on the struck face.** Per 10_design §3.3 the star-crazing is on P (BURST) and only hairline stars appear on the S bulges from D1. The player on S therefore sees almost no paint damage before D2. This is honest, and it leaves the steel M blows to sound and light.
6. **Enamel star-crazing and the honeycomb look** are from coatings practice and the SDI construction, not from a photo (10_design §11 item 4). The wish list (`media_candidates.md` §20 A: `da01`, plus `db10`/`db11`) would confirm them.
7. **The Relay** is Giant B "Night Shift" in its preview materials (W1 still open). The `blow` pose is the `low` pose with the left fist placed on the leaf at 0.25 m from the latch edge, 1.30 m high; from V4 at 76° its body hides the fist, so a latch-side three-quarter view (V4L) was added.
8. **Kick plates.** The hard-edged dark shape on the steel leaf's satin-stainless kick plates (`Prop_Aluminium`, roughness 0.38) in the pass-3 frames was not a reflection: it was the bowed slab cutting through the rigidly moved plates (fixed in pass 4, §7). The plates now read as a plain satin band that mirrors the dark carpet tile. If that still reads too dark in game, the kit owner may want a rougher, brushed value; nothing in A depends on it.
9. **The wood leaf's DoorVeneer stripes** come from the project texture (as in game, see `ingame_door_gap_16…`). At 0.55 m (V3) they read strong in raking troffer light.
10. **CONTRACT dependency:** the 20 mm notch needs the 25 mm wall-end recess at the strike (10_design §7.1 item 8). Without it the strike-zone insert can only be 15 mm deep and the notch would show the wall's cavity face.
11. **Not rendered:** OF-F (W-S, optional in the brief) and tiers 2–5 (they only drop M stages; the stage meshes are the same).
12. **The door modules are moving.** The interactables fix pass (2026-10-08, uncommitted in main at 00:10, 48 files under `Tools/Blender/frontrooms_kit/assets/`) changes finishes only: the steel frame's threshold saddle becomes dark-bronze anodised (it was bright `Prop_Aluminium`, as in these renders), the oak leaf becomes its own U-grain build. No dimension that A's stages depend on changed. A's kit was built from the committed 2026-10-03 modules (`ef061ae`); rebuild `kit/` from the new modules before any implementation.
13. **Codex audit:** `codex_audit/20_findings.md` still does not exist (2026-10-08 00:1x), and none of the `10_review_*.md` files names door-break. Nothing to resolve for this track.

---

## 9. Sources and references

- Design and research: `10_design.md` (§1–§8.1 Direction A brief), `01_aaa_doors.md`, `02_real_door_failure.md`; the glass plan `../glass/destruction/10_glass_destruction_plan.md` and `01_conference_destruction.md` (reused, not repeated).
- Project files read (read-only): `Tools/Blender/frontrooms_kit/assets/interact_door_*.py`, `interact_lock_*.py`, `interact_door_closer_*.py`, `kitlib.py`, `creatures/giant_b_night_shift.py`; `Assets/Resources/Surfaces/Textures/*` (frozen copies in the work folder); `../interactables/00_map_constraints.md`, `05_locked_door_type.md`, `10_spec.md`; `../interaction_audit/FrontRoomsShotTimings.proposal.cs.txt`.
- On-disk references used for the look: `../interaction_audit/images/70–80` (today's inert break) and `77_relay_broken_witness.png` (exposure target); `../interactables/proposal/media/kane_emg_0053_oak_door_lever.jpg` and `ingame_door_gap_16_sideB_hinge_inline_darknear.png` (credits in `../interactables/proposal/media/SOURCES.md`).
- Real-world lead references (not downloaded; pending Red's approval): `df01`, `df02`, `db05`, `df05` in `media_candidates.md`; three new wishes `da01–da03` in its "20 — Direction A" section.
- Scripts (work folder `scripts/`): `build_assets.py` (kit + Relay poses), `dbA_lib.py` (frames, set, materials, lights, render), `dbA_door.py` (intact assembly), `dbA_stage.py` (stage builders, light, debris), `dbA_parts.py` (splinters, screws, chips, honeycomb, crack strips, leak cards), `dbA_marks.py` (all stage numbers), `dbA_tex.py` (mark atlases), `dbA_job.py` (one frame), `final_jobs.py` / `run_par.py` (job lists, runner), `assemble.py` (JPG export, strips), `check_s4.py`, `stats_from_json.py` / `stats_models.py` (counts), `jobs_kick.json` (pass-4 re-renders), `jobs_models.json` (stage models).
