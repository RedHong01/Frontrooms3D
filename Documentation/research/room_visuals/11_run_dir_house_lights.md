# 11 — Run! direction C · HOUSE LIGHTS: pre-render

Date: 2026-10-08 (final round r9, renders 00:1x, exports 00:3x). Brief: `10_run_directions.md` §1, §4, §7.
Clone-only. Nothing was promoted. Red's project was not opened in Unity. Only this folder was written (plus rows VL189–VL192 in `Documentation/VERIFICATION_LOG.md` and their Figma slides). Build log, change list, costs and needs: `20_run_house_lights.md`.

**One line.** A glazed-tile link corridor lit hard and white by bare 8 ft fluorescent strips, every other strip off for economy. Step 1.5 m in and every strip comes to full in a front that runs ahead of you to the exit at 8 m/s; small red flashers start, synced, at 1 Hz; the fire-door pairs held open on magnets let go and swing shut ahead of you and behind you in 4 s. Nothing goes dark. The horror is exposure.

**Verdict.** The brightness, contrast, obstacle and photosafety bars pass. The Relay bar passes off axis (5.2 : 1 through a lite) but **cannot be measured as posed**: a Relay on the axis is completely hidden by the shut pair's meeting stiles. Two readability bars are partial: the magnets are hard to see from the corridor (a holder sits behind its leaf), and after about 3 s the goal is hidden behind the next shut pair (the door you see is the pair, at 6.9 : 1). Three brief values were changed to meet the brief's own targets (§3).

---

## 1. Images

All in `images/`, JPG q85, 1920 px wide.

| Id | File | State | What it shows |
|---|---|---|---|
| S1a | `run_dir_house_lights_s1a.jpg` | armed | From the trip line: strips 1, 3, 5, 7, 9 lit (70 %), 2, 4, 6, 8 dark; the held pairs open; the goal at 25 m |
| S1b | `run_dir_house_lights_s1b.jpg` | tripped, t +3 s | All 18 strip lights at full; the z 9 pair has swung shut (10°): the only large dark shape |
| S2 | `run_dir_house_lights_s2.jpg` | armed | From Level 0, 2 m outside: a white slot in a yellow wall, the held z 0 pair, the far door |
| S3a | `run_dir_house_lights_s3a.jpg` | armed, 50° | The held left leaf of the z 9 pair (the brief's camera) |
| S3b | `run_dir_house_lights_s3b.jpg` | t +1.0 s, 50° | The same camera: the leaves at 63°, the flasher in its off half |
| C1 | `run_dir_house_lights_c1.jpg` | t −0.5 s | Stepping in |
| C2 | `run_dir_house_lights_c2.jpg` | t +0.9 s | The front at z 6.4: compartment 1 at full, the rest still economy; the z 9 pair at 66° |
| C3 | `run_dir_house_lights_c3.jpg` | t +2.0 s | Mid-run at z 12.5: the z 18 pair at 37°, the goal through the gap |
| C4 | `run_dir_house_lights_c4.jpg` | t +4.0 s | Looking back from z 23.5 at the shut z 18 pair; the Relay proxy on the axis at z 11.5 is behind the meeting stiles |
| Greys | `…_s1a_grey`, `…_s1b_grey`, `…_s2_grey`, `…_c4_grey`, `…_c4b_grey` | | Linear Rec. 709 Y, re-encoded to sRGB |
| Protan | `…_s1b_protan`, `…_c4_protan`, `…_c4b_protan`, `…_c4p_protan` | | Machado 2009, severity 1.0, in linear RGB |
| Sheet | `run_dir_house_lights_sheet.jpg` | | 16 tiles: the shots, the hero extras, two derived |
| Extras | `…_s3b2` (t +1.4, flasher on), `…_s3c` (the holder, hero, armed), `…_s3e` (the holder at t +0.4, leaf at 79°, armature free), `…_s3d` (a flasher at t +0.5), `…_c4b` (Relay 0.65 m off axis), `…_c4p` (off axis, the z 9 pair still swinging behind it after its push), `…_c4p_ng` (C4p without glass), `…_c4r` (from inside the z 18 pair, no door between), `…_x1` / `…_x1b` (strip 7: the dead tube and its cathode glow), `…_x2` (the goal RN-X at t +3), `…_p1` / `…_p1off` (a flasher at 2 m, on and forced off) | | |
| Checks | `…_v_levels`, `…_v_glass`, `…_v_exitface`, `…_v_gamerig` | | Verification images (§3, `20` §1.1); Figma VL189–VL192 |

The nine fixed shots are also saved as `images/run_house_lights_<shot>.jpg` (the stage's own naming; the same pixels).

---

## 2. What was built, and every value used

Corridor-local metres: +Z runs from the entrance to the goal, x = 0 is the corridor axis. Root at world (4992, 0, 4992).
Base: Red's main as of 2026-10-07 16:34 (`W/proj_audit` → `W/proj_run_house_lights`), plus the clone-only change list in `20_run_house_lights.md` §2.

### 2.1 Hero vs blockout

| Object | Quality | Source |
|---|---|---|
| Magnetic door holder + armature plate (signature) | **Hero, LOD0** (2,204 + 828 tris): 115 mm wall plate, 38 mm extension tube, 100 × 100 × 80 mm hammertone housing, magnet face (outer pole, coil ring, inner pole) 0.28 m out, release button; zinc mounting plate, swivel stud, 70 mm armature disc | `run_mag_holder.py`, `run_mag_armature.py` |
| 8 ft two-lamp bare strip (signature) | **Hero, LOD0** (2,904 tris): 2.40 m white enamel channel 0.10 × 0.055, end plates, two-piece ballast cover with its seam, knockouts, phenolic lampholders | `run_strip_fixture.py` |
| F96T12 tube | **Hero, LOD0** (964 tris): Ø 38 mm glass, cathode bands, ferrules, single pins | `run_t12_tube.py` |
| Fire-door pair (signature) | **Hero, LOD0**: RN-F pair leaf 3,188 tris (laminate, lite 0.30 m from the meeting edge, push and kick plates, 3 butts); pressed-steel pair frame 1,548 tris (dark bronze, double rabbet, silencers); closer body 1,024 tris | `run_door_leaf_pair.py`, `run_door_frame_pair.py`, `run_closer.py` |
| Closer arms | Blockout boxes (aluminium), solved per frame by the rig (two-bar linkage, shoe on the frame head) | harness + rig |
| Red flasher | **Hero, LOD0** (2,974 tris): 4 in red cast box, spun bezel, ribbed red dome Ø 100 mm, chrome wire guard | `run_flasher_lamp.py` |
| Goal RN-X | Kit models: leaf 4,540 tris (laminate, lite, kick plate, crossbar exit device at 1.0 m, rim latch), frame 1,656 tris; parallel-arm closer (body kit + arm blockout) | `run_door_leaf_x.py`, `run_door_frame_x.py` |
| EXIT sign over the goal | Kit model, 686 tris (a quick clone-only sign; the exit-sign track's family replaces it) | `run_exit_sign.py` |
| Pull station, floor burnisher, folded wheelchair | Kit models a step past blockout: 558, 6,036 and 7,468 tris, correct size and albedo | `run_pull_station.py`, `run_burnisher.py`, `run_wheelchair_folded.py` |
| Header plates, C-LINK plaque | Quads with generated faces on a white backing (TeX Gyre Heros Bold) | harness + `gen_run_c.py` |
| Wainscot cap and cove | Extruded profiles (quarter-round bullnose r 0.02, cove r 0.025) with the wall's world UVs | harness |
| Relay | The brief's proxy (D5) | harness |

### 2.2 Space and finishes

| Item | Value |
|---|---|
| Level 0 anteroom | x −4.5…4.5, z −6…0, ceiling 2.9. `L0_Wallpaper` / `L0_Carpet` / `L0_Ceiling` through `FrontRoomsSurfaces.Room(RoomRule.Lobby, slot)`; walls 0.16 on the cell lines; rubber cove |
| Map lamps (6 + 1 in the stub) | Lens 0.6 × 0.025 × 1.2 at the cell centre + 0.3 z (`FrontRoomsSurfaces.TrofferLens`); spot 0.06 m under it, 162° / 96°, (1, .96, .88), intensity 5, range 10. The 2 lamps nearest the entrance cast soft shadows |
| Corridor | x ±1.5 (faces ±1.42), z 0…27, walls 0.16, ceiling 2.9 |
| Cross walls | 0.16 at z 9 and 18 (and the corridor half of the z 0 wall): opening 2.24 × 2.1, transom above 2.14 (frame head), jambs x 1.16…1.42 |
| Wainscot | Glazed structural tile to 1.524 m: 8 × 16 in units, running bond, glaze #E6E1D2, grout #9C978A (1/4 in), proud 0.02, bullnose cap, cove base. `Run_C_GlazedTile` (FrontRooms/Surface, tile 0.8127 × 0.8128 m = 2 units × 4 courses), mask smoothness ≈ .85 |
| Upper walls | Eggshell plaster #EEEBE2, `Run_C_Plaster`, no ceiling-grime band (r5) |
| Ceiling | Flat painted gypsum #EDEBE4, taped joints on a 4 × 8 ft board, `Run_C_Ceiling` |
| Floor | Waxed 12 in VCT #D9D5C8 (Red's 10-03 decision): smoothness scale 1.56 on a mask averaging .525 → lane ≈ .82, bands ≈ .80, wall strips ≈ .74. Two charcoal bands #4A4844 (tinted to .34 linear), 0.30 wide, 0.30 from each wall. Tile grid world-aligned to the 192 m map |
| Door laminate | Wood-grain HPL `Run_DoorLaminate`, satin (smoothness scale .75, ≈ .47), FREE value |
| Lite glass | **A copy of the game's `Glass_Window`** (FrontRooms/Glass: face-on alpha .11, pane F0 .08, grime maps; dust .45, smear .5, prints .8, RT receive off) + an alpha-tested 12.5 mm wire grid quad 2 mm behind it (D4) |

### 2.3 Lights and rig states

| Item | Value |
|---|---|
| Strips | 9 × `Kit_StripFixture_8ft` on the axis, channel at 2.9 (tubes at 2.82), 3 per compartment, centres z = 9k + 4.5 + (−2.44, 0, +2.44); numbered 1–9 from the entrance |
| Strip lights | 2 spots per strip at ±0.6 m from its centre (18), at **y 2.86** (inside the channel line), **straight down, 179° / 170°**, colour (0.94, 0.97, 1.00), range 9, **intensity 0.8 × level** (D1). Soft shadows on every third spot (6) |
| Tubes | `Run_TubeGlass` emission (0.94, 0.97, 1.0) × 9 × level; cathode bands × 0.30; off tubes `Run_TubeGlassOff` (.78 grey). Channel self-light 0.8 × level (enamel emission) |
| Armed (economy switching) | Strips 1, 3, 5, 7, 9 at **0.7** (D2); 2, 4, 6, 8 off (grey tubes). Strip 7's tube B dead: grey glass, cathode glow (1, .28, .05) × 0.7 |
| Strike front | Off strip i starts at t = 0.1 + z_i / 8: one 60 ms flick to 0.30, then a 0.2 s rise to 1.0. Lit strips step 0.7 → 1.0 as the front passes. Under Reduce Flashing: a 0.5 s ramp |
| Flashers | `Kit_FlasherLamp` at 2.6 m on the approach (−Z) face of each pair's header (z 0: the Level 0 side) and over the goal (4). From t = 0.3: 1 Hz, 50 % duty, synced; 35 ms filament decay. Dome emission (1, .07, .035) × 2.0 when on. Point light (1, .12, .06), range 0.8, intensity 0.3 × 0.8 / 4.5 = 0.053 (the brief's ratio to the strips) |
| Door pairs | z 0, 9, 18. Leaves 1.112 wide (hinges at x ±1.116), swing toward +Z, held at 90°. Holders on the side walls at 1.95 m, 1.13 m past each cross wall. Trip at t = 0: 90° → 10° linearly over 3.0 s, then 10° → 0° over 1.0 s (cosine ease). Closer arm solved per frame |
| Corridor ambient | `lightProbeUsage = CustomProvided` + SH through a MaterialPropertyBlock. **It works in URP 17.3** (no fallback). Armed 1.0 × the global trilight SH; tripped 1.2 ×. Plus a white-room bounce: an SH lobe from below, 0.6 × intensity × the strips' mean level, and a flat 0.06 × the same, tint (.92, .93, .92) (D3). Measured: S1a's mean Y drops by 0.0315 without the ambient |
| Reflections | Three box-projected probes, one per compartment (2.84 × 2.9 × 9.16, capture at y 1.40, 512 px HDR, blend 0.25), re-baked for every rig state. The anteroom and stub reflect the Level 0 zone cube |
| Post `FrontRoomsPost_Run_C` | Post-exposure +0.25; contrast +10; saturation −22; white balance −10 / +4; lift (1, 1, 1, 0); highlights (1, 1, 1, −0.05); bloom threshold 1.1, intensity 0.35; grain 0.20. `EnsureZoneVolume(root, "Run_C", bounds 3 × 2.9 × 27, 1.0)`, then priority 2 |
| Light layers | Corridor lights and renderers on `RunCorridor`, the stub on `RunStub`; the two shadowed anteroom lamps reach the corridor, the strips' shadowed spots reach the anteroom |
| Directional fill | None (brief §7.1; audit F5) |
| Camera | `FrontRoomsPostStack.ConfigureCamera`, clear #22231C, near 0.06, far 80, 1920 × 1080 ARGB32, 4× MSAA, each shot rendered twice. `-frGlassRT off` |

### 2.4 Signs and props

| Item | Value |
|---|---|
| Header plates | "FIRE DOORS — DO NOT OBSTRUCT", red caps (178, 26, 22) on white plastic, TeX Gyre Heros Bold, 0.60 × 0.075 m at 2.30, both faces of every pair. Satin (.55) |
| C-LINK plaque | Engraved two-ply laminate (bronze face, cream core), 0.30 × 0.10 at 1.5 m, Level 0 face, right of the entrance |
| EXIT over the goal | Housing 0.330 × 0.200 × 0.060 at 2.28, white enamel; face "EXIT", 6 in red letters, TeX Gyre Heros Bold Condensed, no arrows; **eggshell .25, glow (1, .075, .056) × 1.6** (D6) |
| Pull station | Red cast body 105 × 135 × 50, "FIRE / PULL DOWN", right wall, **z 1.45** (D7), handle at 1.2 m |
| Floor burnisher | 20 in, grey deck, **safety-orange** housing (.88, .45, .10), cord coiled; at (1.02, 0, 5.5), turned 200°. Lane left ≈ 2.1 m |
| Folded wheelchair | At (−1.21, 0, 22.0) against the left wall, turned 4°. Lane left ≈ 2.4 m |
| Relay | The brief's proxy: capsule body r 0.24, top 1.73; shoulder box 0.78 × 0.24 × 0.30 at 1.72; head sphere Ø 0.28 at 1.60, 0.12 forward; body #2B2928 (.25), head #D8D4C8 (.35); casts shadows. At (0, 0, 11.5) facing +Z; C4b/C4p at x −0.65 |

### 2.5 Shots

As §7.4: S1a (0, 1.62, 1.5) → (0, 1.30, 27), armed (t −1); S1b same at t +3; S2 (0.35, 1.62, −2.0) → (0, 1.25, 27), armed; S3a (0.7, 1.62, 6.6) → (−1.12, 1.6, 9.6), 50°, armed; S3b same at t +1.0; C1 (0, 1.62, 0.8) at t −0.5; C2 (0, 1.62, 1.5) at t +0.9; C3 (0, 1.62, 12.5) at t +2.0; C4 (0, 1.62, 23.5) → (0, 1.40, 0) at t +4.0, Relay on. All 72° except S3.
Extras: S3b2 (S3b at t +1.4); S3c (−0.30, 1.78, 20.15) → (−1.30, 1.93, 19.13), 40°, armed; S3e (S3c at t +0.4); S3d (0.45, 1.70, 15.6) → (0, 2.50, 17.92), 50°, t +0.5; C4b (Relay at x −0.65); C4p (C4b with the z 9 pair at 66°, 0.9 s after a push); C4p_ng (C4p without glass); C4r (0, 1.62, 17.2) → (0, 1.30, 0), t +4; X1 (0.60, 1.70, 18.55) → (0, 2.82, 19.90), 45°; X1b (0.22, 2.25, 18.30) → (0, 2.78, 18.95), 40°; X2 (0.35, 1.62, 23.9) → (0, 1.45, 26.92), 50°, t +3; P1 / P1off (0, 2.6, 6.92) → (0, 2.6, 8.92), t +0.35 (2.0 m from the z 9 flasher).

---

## 3. Deviations from the brief, and why

| # | Brief | Used | Why |
|---|---|---|---|
| D1 | Strip spots 165° / 120°, intensity 4.5, below the strip | **179° / 170°, intensity 0.8, straight down from y 2.86** (inside the channel line), unshadowed channel | At 4.5 the tripped S1b median is **0.645** and p95/p5 is 2.3: a washed-out white (`v_levels`; target 0.30–0.40, ≥ 10). 165° / 120° cones (r1, tilted 30° toward the walls) drew hard V-shaped cone edges across the doors and cross walls. A bare strip lights the walls up to the ceiling; spots below the channel left the top 0.14 m of every wall dark (r3). 0.8 puts S1b at 0.327 |
| D2 | Lit strips at 0.9 when armed | **0.7** | At 0.9, armed S1a has median 0.155 = 1.7 × Level 0 Standard (0.091), above the brief's own "≈ 1.3 ×" (≈ 0.12). At 0.7 it is **0.116 = 1.28 ×** (`v_levels`). Tripped is unchanged; the step at the front becomes 0.7 → 1.0 (still one change) |
| D3 | Ambient 1.0 / 1.2 × global | The same, **plus a white-room bounce** (an SH lobe from below + a flat term, scaled by the strips' level) | URP has no realtime GI. In a white corridor the ceiling's light is mostly interreflection: the strips' spots barely reach it (the channel shields it, and the walls get the light at grazing angles). Without the bounce the ceiling margins read as a dark crown band (r3). With the up lobe at 0.6 the ceiling reads like painted gypsum over bare tubes |
| D4 | Wired-glass stand-in: URP Lit transparent (.80, .85, .86, α .25) + wire grid | **A copy of the game's `Glass_Window`** (FrontRooms/Glass, face-on alpha .11, F0 .08), less dust and smear; the same wire grid | The stand-in's lit white veil lifted the Relay's dark body behind a lite: **3.2 : 1** (fails 4 : 1). With the game's glass, **5.4 : 1**; without glass 14.9 : 1 (`v_glass`, same pose). The game glass is also what production would use until `Glass_Wired` exists |
| D5 | The scene's Hunter if it can be instanced, else the proxy | **The proxy** (as direction A); the game rig rendered once as reference | The game's `FrontRoomsRelayRig` (Run pose) instances, but in the empty look-dev scene its limbs float apart (`v_gamerig`). A broken silhouette is no test, and A also used the proxy, so the three C4 reads compare |
| D6 | (EXIT face not specified beyond "red letters, off-white face") | Eggshell face (.25), glow (1, .075, .056) × 1.6 | At smoothness .9 a strip highlight sat on the T's crossbar from the run line ("EXI\|"); at .5 the face mirrored the white corridor over the letters (salmon, sRGB 207 / 111 / 114). At .25 they read red (204 / 74 / 74) (`v_exitface`). The glow level hardly mattered (E2) |
| D7 | Pull station at z 1.0, right | z **1.45** | The held z 0 leaf lies along the right wall from z 0.08 to 1.20 and hid it |
| D8 | Burnisher grey / orange | Safety orange (.88, .45, .10) | A fire-red orange (.78, .30, .07) read red through the cool post and broke C's palette rule (red only in the flashers and the pull station) |
| D9 | S3b at t +1.0 "flasher on" | S3b at t +1.0 (flasher off) **and** S3b2 at t +1.4 (flasher on) | The flasher starts at 0.3 s with a 50 % duty at 1 Hz: on 0.3–0.8, 1.3–1.8. t +1.0 is in its off half |

---

## 4. Measurements and pass / fail

`run_research/measure.py` statistics on every frame (linear Rec. 709 Y). Contrast = (p95 + 0.05) / (p5 + 0.05), as in `measure.py`. Region ratios = mean Y inside an ID mask vs a 12 px ring outside it (plain ratio; the WCAG form (L1 + .05) / (L2 + .05) in brackets). Full data: `harness/house_lights/metrics_final.json`.

### 4.1 Frame statistics

| Frame | p5 | p50 | p95 | p95 / p5 | Contrast | Saturated red |
|---|---|---|---|---|---|---|
| S1a armed | 0.016 | **0.116** | 0.258 | 16.2 | 4.67 | 0.001 % |
| S1b tripped | 0.031 | **0.327** | 0.558 | **17.9** | 7.49 | **0.001 %** |
| S2 | 0.010 | 0.105 | 0.288 | 28.8 | 5.64 | 0 % |
| S3a / S3b | 0.004 / 0.006 | 0.086 / 0.117 | 0.246 / 0.384 | 62 / 67 | 5.48 / 7.79 | 0.06 / 0.05 % |
| C1 / C2 / C3 | 0.018 / 0.023 / 0.012 | 0.131 / 0.257 / 0.184 | 0.264 / 0.494 / 0.435 | 14 / 22 / 36 | 4.59 / 7.46 / 7.79 | ≤ 0.001 % |
| C4 | 0.019 | 0.329 | 0.563 | 29.7 | 8.88 | 0.02 % |
| S1b protan / C4 protan | 0.030 / 0.018 | 0.328 / 0.330 | 0.560 / 0.565 | 18.5 / 31.1 | 7.60 / 9.02 | 0 % |
| Reference: Level 0 Standard (`03` §6) | 0.013 | 0.091 | 0.313 | | 5.72 | 0 % |

### 4.2 The §7.5 bars and the §4.1 targets

| # | Bar | Measured | Result |
|---|---|---|---|
| 1 | S2: signature and goal door in frame, unoccluded; door ≥ 40 px | The held z 0 pair fills the opening (both leaves, closers, the plate and the flasher); strips 1–3 and the far pairs read; the goal door is **60 px** tall (2.06 : 1). The magnets: the z 0 holders are hidden behind the jambs (each holder sits between its leaf and the wall); the z 9 holders show as 80 px, 6 px tall (1.5 : 1) | **PASS** door and pairs; **PARTIAL** magnets |
| 2 | S1b: the door's contrast against its surround | At t +3 the goal is hidden behind the z 9 pair (136 px through the 5 mm meeting gap). The door the runner sees is the shut z 9 pair, the only large dark shape: **6.87 : 1** (3.39) vs a 12 px ring, 7.68 : 1 vs a 48 px ring; the same in the greyscale copy. The goal itself: S1a 2.46 : 1 (69 px), C3 1.49 : 1 through the closing z 18 pair, X2 at 3.5 m | **PASS** (the pair); the goal is not the frame's door after ~3 s (§5) |
| 3 | C4: Relay ≥ 4 : 1 vs a 12 px ring through the lites (ID mask), also protan | **As posed (on the axis): not measurable.** The meeting stiles hide the whole Relay: 0 px inside the lites, 229 px (2 px wide) through the 5 mm meeting gap. **0.65 m off axis (C4b): 5.20 : 1**, protan 5.19 : 1. **C4p** (the honest chase state: the z 9 pair still swinging shut behind the Relay): 5.39 : 1, protan 5.41 : 1. Without glass: 14.9 : 1. From inside the z 18 pair (C4r, no door between): 27.5 : 1 | **N/A as posed; PASS off axis** |
| 4 | Obstacles ≥ 2 : 1 | Burnisher: S1b 3.62 : 1, S1a 3.22 : 1. Wheelchair: C4 5.49 : 1, S1a 2.51 : 1 (34 px at 20 m). (In S1b and C3 the wheelchair is behind a shut pair) | **PASS** |
| 5 | Armed S1a median ≈ 1.3 × Level 0 Standard (≈ 0.12) | **0.116** (1.28 ×) with D2. At the brief's 0.9: 0.155 (1.7 ×) | **PASS** (with D2) |
| 6 | Tripped S1b median 0.30–0.40; p95/p5 ≥ 10; saturated red ≤ 0.5 % | **0.327**; **17.9**; **0.001 %** | **PASS** |
| 7 | Photosafety: ≤ 2 changes per strip; flasher area in px at 2 m | Table below: every economy-off strip changes twice (flick, rise), every lit strip once; the flashers 2 changes per second, synced. Flasher at 2.0 m (P1 vs P1off, t +0.35): dome **1,706 px**; pixels whose Y changes by ≥ 0.10 **13,881 px = 0.67 %** of the frame, of which saturated red **165 px**. The WCAG area limit (25 % of a 10° field) is 230,175 px at 1080p: the flash uses 6 % of it | **PASS** |
| 8 | Corridor ambient via CustomProvided SH, or a fallback | CustomProvided SH through a MaterialPropertyBlock: works; no fallback | Reported |

### 4.3 Photosafety table (first 4.5 s after the trip)

| Lamp | Changes | Times (s) | Max in any 1 s |
|---|---|---|---|
| Strip 1 (lit) | 1 | 0.358 | 1 |
| Strip 2 (economy off) | 2 | 0.663, 0.723 | 2 |
| Strip 3 (lit) | 1 | 0.968 | 1 |
| Strip 4 (economy off) | 2 | 1.483, 1.543 | 2 |
| Strip 5 (lit) | 1 | 1.788 | 1 |
| Strip 6 (economy off) | 2 | 2.093, 2.153 | 2 |
| Strip 7 (lit) | 1 | 2.608 | 1 |
| Strip 8 (economy off) | 2 | 2.913, 2.973 | 2 |
| Strip 9 (lit) | 1 | 3.218 | 1 |
| Flashers (4, synced) | 9 | 0.300, 0.825, 1.301, 1.825, 2.301, 2.825, 3.301, 3.825, 4.300 | 2 |

Door leaf angle: t −1.0 90°; 0.0 90°; 0.5 76.7°; 0.9 66.0°; 1.0 63.3°; 1.4 52.7°; 2.0 36.7°; 3.0 10.0°; 3.5 5.0°; 4.0 0°.
At a sprint (5.5 m/s) from the trip line you reach the z 9 pair at 1.4 s (leaves at 53°) and the z 18 pair at 3.0 s (10°): you push through the second pair.

---

## 5. What this direction shows, honestly

- **It is the fairest of the three.** The tripped corridor is evenly bright (p50 0.33), the proxy reads 27.5 : 1 against the white corridor, and every obstacle clears 2 : 1.
- **It is the least dark.** The dread has to come from the exposure, the doors and the sound. In the frames the doors carry it: the dark wood leaves are the only large dark shapes, and they move.
- **The doors hide the goal.** The runner sees a white corridor ending in a shut pair, not the goal, from about t +3 s. That is the design (you push through), but rule 5 ("the door is the most legible thing") then applies to each pair in turn.
- **Sight through a shut pair is narrow.** The lites show a strip 1.2 m off axis at 12 m. A Relay on the axis behind a pair is invisible, and so is a player on the axis to the Relay. This needs a rule in CR-7.
- **The magnets are the least visible signature.** They are only seen from beyond a pair (S3c, S3e) or close to the wall.

---

## 6. Research behind it

`03_run_research.md` (C: Kane's lit tunnel; the A24 trailer's run at 0:27, median Y 0.35, contrast 10.6), `media_candidates.md` A9–A10 (door-holder magnets, not downloaded), `interactables/10_spec.md` D5 (RN-F, RN-X), `relay_pursuit/30_narrative.md` (EGRESS), `office_and_film/22_era_lock.md` (1990: the glazed tile is second-hand mid-century, the strips and holders 1970s–80s, no brands, no dates). No new media was downloaded.
