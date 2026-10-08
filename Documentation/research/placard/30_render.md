# 30 — Evacuation placard (Q16): in-game render

Status: **RENDERED in the private clone, 2026-10-07.** Art **v2**. Build report: `20_build.md`. Spec: `10_spec.md`.
- Clone: `/Users/redwang/FrontRoomsVisualWork/proj_placard` (main 16f520e + the placard build). Unity was never opened on Frontrooms3D.
- Captures: 19:38–20:01, three play-mode runs (P1, P2, P3). Door-position tally: 23:38.
- The first render attempt stopped on a usage limit at 20:06, after all captures and images were done. This run continued from its outputs: it reviewed every image, added one crop sheet and two chart crops, ran one extra placement tally, placed the Figma slides and wrote this report. Nothing was re-captured.
- Red's project: only this folder (`images/`, `render_data/`, `render_src/`, this file) and `Documentation/VERIFICATION_LOG.md` (rows VL169–173, section height, cover note) were written.

## 0. Summary

| Check | Result | VL |
|---|---|---|
| Lit, by the real start door, 3 m / 1 m / 0.3 m corner | **PASS.** Square on the facade, legend faint by day, mitre a hairline, lens quiet | VL169 |
| Dark, own lamp off (+15 s) and power failure (+3 s) | **FLAG.** The legend reads well: 1.72× the paper (bar 1.30), 2.60× in a power failure. But at the real spot the full glow is **32 %** of the lit sheet, not the 12 % the build measured in the look-dev room | VL170 |
| 6-frame lamp-off sequence, normal and Reduce flashing | **PASS.** Peaks 0.92 at 1 s, then decays to the neighbours' hold of 0.58. Reduce flashing never changes faster than 0.5 per s | VL171 |
| Failing, stutter and Warn lamps (A5–A7) | **PASS.** Stutter: glow 0. Warn ×6: one swell. Failing under Reduce flashing: 2 s swing 0.017 | VL172 |
| Placement P1 / P2 / P3 in the game's own flow (A9) | **FLAG.** All placed exactly by §5.2, nothing overlaps. P2 reads past the open leaf. The open west leaf hides P1 from the east half of the doorway, and the arch pier hides P3 from the door cell | VL173 |
| How often each mount comes up | **New number.** At real stream door positions: P1 **76 %**, P2 **22 %**, P3 **2 %** (400 runs). The build's 87 / 9 / 4 used a door position the game never makes | — |
| Shadows / colliders | Shadows Off on both placard renderers, 0 colliders, in all three runs | — |
| WebGL (A10) | Not done in this stage | — |

**Two decisions for Red** (details in §9): the glow level k, and the map contract Q16-1, which now matters in about 1 run in 4.

## 1. Method

**The harness** (`render_src/FrontRoomsPlacardRender.cs.txt`, clone only, never for main):
- Opens the game scene and enters Play mode.
- Picks a seed in the same frame as Space: it builds the map the game is about to attach behind the live terminal door, runs the §5.2 rule, and takes the first seed that gives the wanted mount.
- Presses Space through the game's own `RequestTitleStart`. So the placard is placed by the real path: `RoomStream.EndStreamAt` → `FrontRoomsPlacard.Prepare` → `MapRunStarted` → `FrontRoomsPlacardMount.Place`.
- Renders with the game's first-person camera: its URP camera data and full post, FOV 76, 1920 × 1080, 4× MSAA. The camera is moved for each shot and moved back after it.
- Fixes time at 1/60 s per frame once the door is open, so "+1 s" is exactly 60 game frames.
- Holds the Relay.
- Switches Reduce flashing by reflection (the property plus its `Changed` event). It never calls `SetReduceFlashing`, which writes PlayerPrefs that the Editor shares with Red's project. Red's own setting at start: off.
- Writes `series.csv` (every frame: G shown, charge C, smoothed lamp Ls, lamp L, wall light W), `frames.txt`, `log.txt` and `report.txt`. Copies are in `render_data/main_P1`, `a9_P2`, `a9_P3`.

**The light measure** (A4): legend at 0.6 m, FOV 40, linear HDR, post off. The glow-mask pixels (3,871) are compared with the paper pixels (35,378).

**Clone-only test patch.** Contract Q16-1 (`KeepClearAtStart`, spec §5.4) was applied to the clone's `FrontRoomsMapWorld.cs` for these runs only. It compiled and worked ("KeepClearAtStart in this map: True" in all three runs). It was removed afterwards: the clone's file is byte-identical to main again (md5 07ce188a). The exact diff is `render_src/contract_q16_1_applied_in_clone.diff`.

**Two harness problems the first attempt fixed:**
- The first run stalled for about 25 minutes compiling Surface shader variants. The clone's shader cache was seeded from identical clones (same shader source and package lock), and the run went on.
- Building and releasing the throw-away seed maps destroyed shared materials in `FrontRoomsSurfaces.Lit`'s cache, so the real map drew magenta. The harness now drops those entries from the cache too. This is the same engine hazard as codex-audit VL126 (zone key turns pink after R); no new finding.

**Main moved during this stage** (a5262fb → ee5c9bb): the Q1b print array and hard-edge wallpaper, the window facade, touch controls and iOS builds. None of it touches the placard's files. The wallpaper in these frames is the clone's (pre-Q1b) print; the placard does not depend on it.

## 2. Seeds and spots

| Run | Seed | Mount | Glow cell | Origin (world) | Faces | Lamps as generated |
|---|---|---|---|---|---|---|
| main_P1 | 2 | P1 west | (20, 70) | (254.7666, 1.524, 18.0225) | +Z | own Steady; E/W Steady, N Stutter (forced Steady for the run) |
| a9_P2 | 3 | P2 east | (149, 70) | (257.9195, 1.524, 19.5000) | −X | own Stutter (forced Steady for the lit shots) |
| a9_P3 | 15 | P3 west | (212, 134) | (254.5866, 1.524, 18.0225) | +Z | own Dim (forced Steady for the lit shots) |

- The door is at x 256.5, z 18 (stream room 1, 8 start rows). Every origin equals the §5.2 numbers exactly: 1.7334, 1.4195 / 1.500 and 1.9134 from the door centreline, centre height 1.524 m.
- The main run forced the glow cell and its 3 lamp neighbours (E, W, N) to Steady, so A4 had a known start. The south neighbour lies in the start area and has no lamp.

## 3. Lit (A1, A3 in the game)

Images: `q16_r_lit_3m.jpg`, `q16_r_lit_1m.jpg`, `q16_r_lit_corner_0p3m.jpg`, `q16_r_lit_corner_zoom.jpg` (VL169).

- **3 m and 1 m face-on** (eye 1.62 m): the placard sits square on the facade, right of the double door as seen from the map. The v2 legend shows faintly in its pale ink, with no glow (A3).
- **0.3 m from the top corner**, plus the same eye at FOV 30: the bullnose is smooth, the mitre is a fine line, and the sheet sits inside the lip. The lens adds no visible veil (A1).
- The shared aluminium still reads dark and a little grimy at 0.3 m (as the build noted, `20_build.md` §7).
- **Not the placard:** the pale bar floating in the 3 m frames is the map's spinning zone key (`FrontRoomsMapWorld` keys spin 1.05 m above their cell).

## 4. Dark: does only the legend glow, and how bright? (A4)

Images: `q16_r_glow_crops.jpg`, `q16_r_views_sheet.jpg`, `q16_r_dark_powerfail_corner_0p3m.jpg`, `q16_r_dark_ownoff_corner_zoom.jpg`, plus the single frames `q16_r_dark_*` (VL170).

| State | G | Paper (linear) | Glyph / paper | Bar |
|---|---|---|---|---|
| Lit, Steady | 0 | 0.2937 | 0.77 (bands print darker than the paper) | — |
| Own lamp Off (`SetLampMode`) +15 s, 3 Steady neighbours | 0.578 | 0.0592 (20 % of lit) | **1.72** | 1.30: PASS |
| Power failure: the cell and every lamp within 3 cells Off, +3 s | 0.727 | 0.0387 (13 % of lit) | **2.60** | PASS |
| Power failure +15 s | 0.348 | — | — | — |

**What the frames show:**
- At 1 m and 0.3 m the glowing parts read clearly: the IN POWER FAILURE heading, the WAY ON, EXIT and NO EXIT labels, and the one turned or flattened band in each swatch. The tint reads pale mint, not screen green. Nothing blooms or haloes.
- At 3 m the glow is faint; in the crop it shows as pale green marks.
- **The sheet never goes black.** The room's ambient and the lamps further away keep the unlit paper at 20 % of lit (own lamp off) and 13 % (power failure). So the whole placard stays visible, with the glow on top. "Only the legend" would need a darker room ambient. That is the game's look, not the placard's, and this stage did not change it.

**The finding: the in-game lit sheet is 2.5× darker than in the look-dev room.**
- The build measured the lit paper at 0.738 in the look-dev room and set k = 0.158 so that the full glow is 12.0 % of it.
- At the real P1 spot the lit paper is **0.294**. Most likely because the facade sits at the cell's edge, so its lamp reaches the sheet at a grazing angle from about 2 m (not measured separately).
- So the same k = 0.158 makes the full glow add 0.0948, which is **32.3 %** of the in-game lit sheet. That is 2.7 times the walls' 12 % cap (LD §9, "~12 % of a lit wall's luminance").
- The build's flag (glyph/paper 1.23, under the 1.30 bar) does not happen at the real spot: there it is 1.72.

**The k options at the real spot** (scaled from the measured numbers above, since the glow adds linearly with k; A4 = own lamp off, 3 lit neighbours, +15 s):

| k | Full glow / in-game lit sheet | A4 glyph/paper | Power failure +3 s |
|---|---|---|---|
| 0.059 | 12 % (the cap) | 1.14 (fails 1.30) | 1.47 |
| 0.087 | 17.7 % | 1.30 (just meets) | 1.79 |
| **0.158 (built)** | **32 %** | **1.72** | **2.60** |
| 0.185 (the build's "14 %" option) | 38 % | 1.88 | 2.89 |

- With only 2 lit neighbours (the spec's A4 case) the estimate at k 0.158 is about 1.5 (ESTIMATE: the paper falls to about 0.052 and G to 0.39).
- The build's question "12 % or 14 %" is replaced by the table above.

## 5. Lamp-off sequence (normal and Reduce flashing)

Images: `q16_r_curves_lampoff.png`, `q16_r_lampoff_sequence.jpg` (VL171). Full chart: `q16_r_glow_curves_ingame.png`.

Own lamp cut at t 0, eye 1 m, 3 lit neighbours. G is the glow shown (0–1).

| t | 0 (lit) | +0.25 s | +0.5 s | +1 s | +3 s | +15 s |
|---|---|---|---|---|---|---|
| Normal | 0.00 | 0.07 | 0.57 | **0.92** | 0.73 | 0.58 |
| Reduce flashing | 0.00 | 0.00 | 0.00 | 0.20 | **0.81** | 0.58 |

- **Normal.** The glow rises within 1 s as the lamp fades (Ls τ 0.35 s), at most 2.0 per s. Then it follows the charge, C = 1 / (1 + t/8): 0.889 at 1 s, 0.727 at 3 s. It stops at 0.578, where the 3 lit neighbours hold it (W = 0.2 × their sum).
- **Reduce flashing.** The same curve, slower: the lamp low-pass is 1.0 s and the glow never moves faster than 0.5 per s. A full swing takes at least 2 s.
- **Relight.** G returns to 0 at the same rates (normal 2.0 per s, Reduce flashing 0.5 per s).
- **Power failure** (no lit neighbours): +15 s G 0.348 = 1 / (1 + 15/8). The decay runs on.

## 6. Lamp temperaments (A5, A6, A7)

Images: `q16_r_curves_lamps.png`, `q16_r_A5_failing_strip.jpg`, `q16_r_A6_stutter_dip.jpg`, `q16_r_A6_stutter_12s.jpg` (VL172). Numbers from `render_src/stats.py.txt` over `series.csv`.

| Case | Length | Max G | Turns per s | Max change per s | Largest 2 s swing |
|---|---|---|---|---|---|
| Failing, normal | 12 s + 30 s | 0.92 | 0.53–0.58 | 2.0 | 0.91 |
| Failing, Reduce flashing | 12 s + 30 s | 0.087 | 0 | 0.23 | 0.017 |
| Stutter (3 bursts down to L 0.05) | 12 s + 40 s | **0** | 0 | 0 | 0 |
| Warn ×6 at 3 Hz, normal | 9 s | 0.20 | one swell | 2.0 | — |
| Warn ×6 at 3 Hz, Reduce flashing | 7 s | 0.58 | one swell | 0.5 | — |

- **Failing, normal.** The glow pulses about every 2 s. That is photosafe (the gate is ≤ 3 per s). The spec's A5 wording "≤ 0.5 Hz" is 6–16 % over, because the lamp itself runs at 0.49 Hz and its dropouts add turns; the build's simulation found the same (0.55).
- **In the failing frames you see the lamp pulse, not the glow.** A half-lit lamp outshines the phosphor, so the glow only shows during the lamp's dropouts.
- **Failing, Reduce flashing.** A steady, very faint read: G creeps up to 0.087 over 30 s (about 3 % of the lit sheet at k 0.158). In practice the legend does not read in a failing cell with Reduce flashing on.
- **Stutter.** No glow at all, even mid-burst (`q16_r_A6_stutter_dip.jpg`: L 0.05, G 0).
- **Warn train.** One smooth rise and fall in both modes, with no flicker per dip. Reduce flashing gives the larger swell (0.58 against 0.20): its slower lamp low-pass keeps the read level low through the whole train. It is slower, so it stays photosafe. No change proposed.

## 7. Placement in the game's flow (A9)

Images: `q16_r_A9_sheet.jpg`, `q16_r_A9_P2_open_pastleaf.jpg`, `q16_r_A9_P2_corner_0p3m.jpg`, `q16_r_A9_P3_corner_0p3m.jpg` (VL173); single frames `q16_r_A9_*`.

**Placed and clear.**
- Every origin equals the §5.2 numbers (§2).
- The only things inside the frame box (+0.02 m) and the keep-clear floor rect are the wall each placard hangs on: the facade's "door reveal / far side" and "door wall return" for P1 and P3, and the Level 0 wallpaper blocks for P2. No furniture, with Q16-1 applied.
- Door shut and open both work. `CloseTerminalDoor` was used for the shut views.

**Seen from the map side** (eye 3.5 m north of the door, at x 257 or 256):
- **P2 PASS.** It reads past the open leaf, as the spec asks.
- **P1: hidden behind the open west leaf** from the east half of the doorway. The leaf stands 1.12 m out from the facade at x ≈ 255.4, which is on the sight line to the placard (x 254.5–255.0). It reads from its own cell (3 m face-on) and whenever the door is shut. The spec's clearance (0.34 m from the leaf) holds; this is about the view, not a clash.
- **P3: hidden by the arch pier** from the door cell. It reads from its own cell (1 m and 0.3 m). The spec already names this case (§5.2 "P3: the pier may hide the placard"), with the pier-face refinement still open.

**How often each mount comes up (new numbers).**
- The build's 100-seed tally put the door at z 48. The stream's rooms are 12 m long and the first terminal door is at z 18, so real doors stand at z 18, 30, 42, 54 and so on. z 48 never happens.
- This stage re-ran the same tally (`FrontRoomsPlacardLookdev.Placement`, 100 seeds per door) at real door positions. The z 18 table gives exactly the seeds the in-game runs picked (2 = P1 west, 3 = P2 east, 15 = P3 west), so the tally matches the game.

| Door z (stream rows) | P1 | P2 | P3 |
|---|---|---|---|
| 18 (8) | 74 | 25 | 1 |
| 30 (12) | 81 | 18 | 1 |
| 42 (12) | 74 | 24 | 2 |
| 54 (12) | 74 | 21 | 5 |
| **Real positions, 400 runs** | **303 (76 %)** | **88 (22 %)** | **9 (2 %)** |
| 48 (12): the build's, not a real door | 87 | 9 | 4 |

- The real shares match the spec's estimate (80 / 17 / 3) well.
- **P2 or P3 comes up in about 1 run in 4.** Those are the two mounts that need contract Q16-1 to keep furniture out of the way.
- The placard cell's own lamp over the 400 runs: Steady 61 %, Stutter 22.5 %, Failing 8.3 %, Dead 5.3 %, Dim 3.0 %. So, without power events, the legend glows on its own in about 16 % of runs (Failing, Dead or Dim), and never under Stutter.
- Tables: `render_data/placement_doors/placement_z18.csv` … `placement_z54.csv` (and `_z48`, identical to the build's `placement.csv`). The clone tool gained a `-placardDoors z:rows,…` argument for this (`render_src/FrontRoomsPlacardLookdev.cs.txt`; clone only).

## 8. Not done in this stage

- **A10 WebGL** (2 draws, no lens, 1024 textures, the glow works). It needs a WebGL build of the clone. Not run.
- **A2** (60° toward a troffer) and **A8** (walk-out) were passed in the build stage's look-dev room and were not repeated in the game.
- No new captures after the restart, by Red's merge rule (c).

## 9. Decisions for Red

1. **Glow level k** (one number, `FrontRoomsPlacardGlow.PeakEmission`). See the table in §4.
   - **Keep 0.158 (recommended).** The legend reads at 1.72× in a dark cell with lit neighbours and 2.60× in a power failure, and in the frames it stays a faint pale mint, never a light. It is 32 % of the in-game lit sheet, but it only shows when the placard's own lamp is below 0.55, so it never competes with the lit look.
   - Or **0.087**: just meets the 1.30 bar (17.7 %).
   - Or **0.059**: holds the walls' 12 % cap, but then the legend fails the bar (1.14) whenever the neighbours are lit.
2. **Contract Q16-1** (`KeepClearAtStart`, map-owned). It is now needed in about 24 % of runs (P2 22 %, P3 2 %), not the 13 % the build reported. It was tested in the clone: compiles, and nothing was dressed into the rects.
3. **P1 behind the open leaf, P3 behind the pier.** Accept as is (both read from their own cell and when the door is shut), or ask for the P3 pier-face refinement (§5.2).
4. **From the build, still open:** the `FrontRoomsRenderSetup.cs` edits need approval; a placard-only clean aluminium slot is optional.

## 10. Promotion

- **This stage adds no promotable change.** Its two tools are clone only: the render harness (`Assets/Editor/Audit/FrontRoomsPlacardRender.cs`) and the `-placardDoors` option in `FrontRoomsPlacardLookdev.cs`. Copies are in `render_src/`.
- The build's patch script still applies to current main: `promote_src/apply_placard_patches.py.txt <project> --dry-run` against **ee5c9bb** (23:2x) gives all 5 edits (4 in `FrontRoomsRenderSetup.cs`, 1 in `FrontRoomsRoomStream.cs`). `FrontRoomsRenderSetup.cs` has changed since the build's recorded base (Q1b), but every anchor still matches once. It is not yet a v2 package under `W/apply/`; the merge stage makes that.
- Codex audit: `codex_audit/20_findings.md` still does not exist; the other audit notes have no finding for this track.

## 11. Files

**Images** (`images/`; JPG q85–88, PNG for charts):

| Image | What | VL |
|---|---|---|
| `q16_r_lit_3m.jpg`, `q16_r_lit_1m.jpg`, `q16_r_lit_corner_0p3m.jpg`, `q16_r_lit_corner_zoom.jpg` | Lit, Steady lamp | VL169 (2829:6157) |
| `q16_r_glow_crops.jpg` | Placard crops: lit / own lamp off +15 s / power failure +3 s, at 3 m and 1 m (made from the full frames by `render_src/crops.py.txt`) | VL170 (2829:6177) |
| `q16_r_views_sheet.jpg` | Lit / own lamp off / power failure × 3 m, 1 m, 0.3 m | VL170 |
| `q16_r_dark_powerfail_corner_0p3m.jpg`, `q16_r_dark_ownoff_corner_zoom.jpg` | Dark close-ups | VL170 |
| `q16_r_dark_ownoff_{3m,1m,corner_0p3m}.jpg`, `q16_r_dark_powerfail_{3m,1m,corner_zoom,t15s_1m}.jpg` | The other dark frames | (in the sheets) |
| `q16_r_curves_lampoff.png`, `q16_r_lampoff_sequence.jpg` | Lamp-off curves and the 6-frame sequence, normal and Reduce flashing | VL171 (2829:6193) |
| `q16_r_curves_lamps.png`, `q16_r_A5_failing_strip.jpg`, `q16_r_A6_stutter_dip.jpg`, `q16_r_A6_stutter_12s.jpg` | Failing, stutter, Warn | VL172 (2829:6203) |
| `q16_r_glow_curves_ingame.png` | Both chart rows in one image (split by `render_src/split_curves.py.txt`) | (source of VL171–172) |
| `q16_r_A9_sheet.jpg`, `q16_r_A9_P2_open_pastleaf.jpg`, `q16_r_A9_P2_corner_0p3m.jpg`, `q16_r_A9_P3_corner_0p3m.jpg` | Placement P1–P3 | VL173 (2829:6219) |
| `q16_r_A9_P{1,2,3}_*` (other) | Single placement frames | (in the sheet) |

**Figma:** VL169–173 in FRONTROOMS · VISUAL VERIFICATION LOG (`2595:6093`). The section grew to 44 rows (54,640; bounds checked clear). The cover now reads 22 tasks, 172 checks, 528 images; Q16 = VL123–125, 127, 169–173, 9 checks.

**Data and code:** `render_data/main_P1`, `a9_P2`, `a9_P3` (series, frames, logs, reports), `render_data/placement_doors/`, `render_src/` (harness, look-dev tool, post-processing, stats, crops, the Q16-1 test diff).

**Working files** (not an archive): `/Users/redwang/FrontRoomsVisualWork/placard_work/render/` (raw PNG frames in `out/`), logs in `/Users/redwang/FrontRoomsVisualWork/logs/placard_render/`.
