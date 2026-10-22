# M2 flash limiter: Office Stutter re-measure (C10 follow-up)

游戏视觉 workflow stage `m2-stutter-recheck`, 2026-10-22 15:14–16:xx (session local_5656cffd, account A). Asked by 关卡设计 after the M2 landing (LANDINGS 15:02). Same method, seed, views and tools as C10 (`10_lamp_look_check.md`), on a fresh clone of main with M2.

**Verdict: PASS (run A).** RUN_B_STATUS

TABLE_HERE

How to read it:
- **Before** = C10's capture (09:15–09:58, MapWorld 7ef0ca45, WarnLamps b11d8f93). **After** = this capture (M2). "–" = not captured.
- **Lamp columns** answer 关卡设计's per-lamp contract (§9.5): every lamp ≤ 2 flashes/s, none over 3. They count the lamp's LampLevel with the WCAG pair rule: a change is a swing ≥ 0.10 with the darker side < 0.80, and a flash is two opposing changes. The number is the most in any 1 s window.
- **10° field, share > 3/s** answers **WCAG 2.3.1** (general flash threshold). A 10° field is 1/3 × 1/3 of the screen. The test fails when ≥ 25 % of it flashes more than 3 times in 1 s.
- **1/4 field** is C10's stricter test (a large monitor up close). **Screen** is ITU-R BT.1702.
- **Share > 2/s** is the same area test at the design target of 2.
- **Flashes/s by area** counts instants where ≥ 25 % of a field changes at once (opposing instants only). It must stay ≤ 3.
- **Worst pixel** = the most changes one pixel (480 × 270 linear Y) makes in any 1 s. 4 changes = 2 flashes.

**Result against 关卡设计's expectation** ("every lamp at most 2 flashes per second, no lamp over 3"): **met.**
- Every lamp in every after sequence peaks at **2.0 flashes/s** with RF off and **≤ 1.0** with RF on.
- The Office Stutter lamp 277,209 drops from **9 to 2**.
- The wall it lit: from **> 3 flashes/s over 37 % of a 1/4 field and 21 % of a 10° field to 0 %**, and **0 % over 2**.
- The worst Office room found (5 Stutter/Failing lamps in view) is **0 % over 3 and ≤ 0.3 % over 2**.

## 1. What M2 does (code read in the clone)

Files, sha1 checked against main and LANDINGS:
- `FrontRoomsWarnLamps.cs` **72c6c5f4**, as landed for M2.
- `FrontRoomsMapWorld.cs` **6041880b**. This is M2's 223da401 plus A12's double-acting doors (landed 15:11 on top of M2). `diff` against `~/FrontRoomsMapWork/backup/main_before_a12_20261022` (223da401): A12 changed 204 lines, none of them in the lamp, flicker, limiter or fixture code.
- `FrontRooms3DGame.cs` 3b8f1cfd, `FrontRoomsMapHunter.cs` 080fe83f.
- Clone `W/proj_m2check`, mkclone 15:14 from main 9d28aaf9.

**How it caps flashes:**
- **Per lamp, not per field.** `Level(f)` → `Limit(f, v)` on each Stutter, Failing and Dead lamp's own level, before any override.
  - It runs a zigzag with FlashStep 0.10 and FlashDark 0.80, and keeps a ring of the last 6 change times.
  - A 7th change inside 1 s (FlashWindow) is held back, and the lamp keeps its last level. That is a hard cap of 3 flashes/s.
  - It runs in `TickFixtures` (desktop) and `TickFixturesNear` (WebGL), so it is in the capture path. The harness only reads.
- **The modes are redesigned to stay at ≤ 2 by themselves:**

  | Mode | Behaviour under M2 |
  |---|---|
  | **Stutter** | Episodes of 2–3 dips on 0.55 s slots. Each dip goes to ×0.50–0.60 over 60–80 ms ramps and holds 50–150 ms. An episode lasts ≤ 1.41 s. Episode start times come from the old RNG draws ("pinned to main"). |
  | **Failing** | Dropout to ×0.05, held ≥ 0.34 s. The next one starts ≥ 1.1 s after the last one ends. |
  | **Dead** | At most one strike per 4 s. No strike in the warning's room from stage 1 until stage 0 (`SetAmbientHold`, driven by WarnLamps). |

- **W1 dips are not limited by `Limit`.** Overrides multiply after it. They stay ≤ 2/s by W1's own design (0.55 s spacing), and Stutter/Failing lamps are still kept out of the burst set.
- **Reduce Flashing still changes things:**
  - a Stutter episode becomes one sag to ×0.70 with 0.5 s smoothstep ramps;
  - a Failing dropout becomes the same sag;
  - Dead strikes are not shown at all.
- **Measured: the limiter never had to act.** The capture logged each ambient lamp's hold counter (`LampAmbientForTools`) at the end of every sequence:
  - **0 frames held**, in all 360 lamp-sequence entries (111 ambient lamps in range: 66 Stutter, 30 Failing, 15 Dead; 6 sequences plus 3 warm-ups);
  - the redesigned modes do the work, and the cap at 3 is a safety net this capture never reached;
  - its own test is 关卡设计's `LimitFlashesForTools` suite (FixtureTickTests 205/205).

## 2. Method (like for like with C10)

- **Harness:** C10 harness v3 (373d638a), installed in the clone only, as `v3m` 6fe1e02b. Three additions; the capture path is unchanged:
  - `<kind>Nat`: C10 run 2's natural viewpoint. The search is run as before and kept if it lands within 2 cm of run 2, else pinned. Both L0 and Office landed on run 2's viewpoint, so they were kept as searched.
  - `OfficeSF`: the worst-case view (§4).
  - A limiter log line per sequence.
- **Capture:** compiled 15:18 (0 error CS). **Run A** ran 15:19–15:33: `-lwOnly L0Nat,OfficeNat,OfficeSF -lwEpisodes 3 -lwRF 0,1`, DONE, rc 0, 0 exceptions.
  - The sequence order repeats C10 run 2, so the Office sequences start at the same game time.
  - RUN_B_LINE
- **Timeline exact under load:** 1-minute load 37–302 during this stage. Following C10, `Time.captureDeltaTime = 1/60` is set before the first spot:
  - every frame advances the game 1/60 s whatever the wall time (measured steps 0.0166–0.0167 s in every log);
  - the harness renders and logs every Unity frame, and found **0 skipped frames** (frame-count gaps) in all sequences;
  - no wall-clock frame time is used anywhere.
  - Proof that the timeline matches C10:
    - the sequence lengths are identical (L0 1687 / 1786, Office 1593 / 1730 frames);
    - all 6 W1 bursts per sequence land on C10's times to the millisecond;
    - Stutter 277,209's episodes start where C10's did (5.08 → 5.2 s, 23.00 → 23.07 s; ±1 frame plus the 8 ms pre-lock offset C10 also had).
  - So "the same second" (22.55–23.55 s) is the same episode before and after.
- **Analysis:** C10's tools, unchanged.
  - `tools_v3/area3.py`: per pixel on 480 × 270 linear Y; concurrent area with direction; density > 3 and > 2; W1 attribution.
  - The `sum3.py` summary.
  - `tools_v2/analyse2.py` transition rule for the lamps.
  - New, clone-side: `m2_curve.py` (the density timeline behind area3's worst number, and the per-pixel maximum), `m2_lamps.py` (per-lamp before/after and dips per 10 s), `m2_table.py`, and `m2_sheet.py` (the VL385 sheet; `plot_photosafety3.py` is hard-wired to C10's 26 rows, so the sheet replaces it here).
  - The before numbers were recomputed from C10's own event arrays and reproduce C10's table: 21.3 % / 36.9 %, 11.3 % / 20.3 %, 11.4 % / 20.4 %.

## 3. Do the lamps still read as failing lamps?

**Yes, but softer.** Dips per 10 s from LampLevel (falls ≥ 0.10 with the darker side < 0.80), RF off:

| Lamp | Before M2 | After M2 |
|---|---|---|
| Stutter (277,209 · 273,199 · 274,209 · 276,202 · worst-case room) | 2.1–10.2 dips/10 s, each a strobe to ×0.05 | **1.7–4.1 dips/10 s** to ×0.47–0.56 |
| Stutter episodes per 10 s | 0.7–1.5 | **0.7–1.5, at the same times** |
| Failing (275,209 · 276,201 · 297,190 · 298,188) | 5.1–6.4 dips/10 s to ×0.03–0.04 | **4.9–6.2 dips/10 s** to ×0.03–0.04 (unchanged) |

- **What the player sees:** the wall Stutter 277,209 lights (C10's worst 1/4 field) dips **−40 %** in linear Y, three times, 0.55 s apart. Before, it strobed **−73 %**.
  - So a Stutter lamp still clearly fails, but as a tube that sags and recovers, not one that blinks out.
  - Whether that is broken enough is a look call (游戏视觉 / Red), not a safety one.
- **Steady between episodes:** episodes still come every 5–21 s (unchanged timers), so a Stutter lamp is steady most of the time, as before.
- **RF on:** each lamp makes one sag per episode, ≤ 1 flash/s. The largest one-frame step of a Stutter/Failing lamp is now **0.06**. In C10 it was **0.30** (Stutter) and 0.17–0.21 (Failing). C10's "RF ramps ≥ 0.5 s" request is met.

## 4. Worst case: several Stutter/Failing lamps in one view

- **Scan:** every Office room within 6 chunks of the start on the C10 seed (76 rooms), scored by the Stutter/Failing lamps on the room and a one-cell ring.
  - Distribution: 1–13 per room, median 7.
  - Worst: room **299,189** (2 × 4), with 13 Stutter/Failing lamps on room + ring (10 Stutter) and 30 within lightRadius.
  - The capture stood at 299,190 and took the view with the most of them: **5 in view** (Stutter 298,190 · 299,188 · 298,187; Failing 297,190 · 298,188), plus 3 W1 lamps.
- **Why rooms on one seed, not 30 seeds:** lamp modes are a per-cell hash of the seed. So 76 rooms on one seed sample the same distribution as 30 seeds × the nearest room. A seed scan would also need a fresh run start per seed in the capture, which was not cheap.
- **Result** (`OfficeSF_rf0`): every lamp ≤ 2.0 flashes/s. By area:
  - 0.0 % of any field over 3, 0.2 % (10°) / 0.3 % (1/4) over 2;
  - flashes/s by area 0.5 (10°) and 1.5 (1/4); the 1/4 one is W1;
  - worst pixel 5 changes in 1 s (2.5 flashes, on a single pixel, where two lamps' changes overlap).
- RF on: ≤ 1.0 per lamp, 0 % over 2.

## 5. For 关卡设计

> M2 re-measure on main (MapWorld 6041880b = your 223da401 + A12, WarnLamps 72c6c5f4), same seed, views and per-pixel method as C10, frame-locked at 1/60 s: **PASS, nothing to change for photosafety.**
> Every lamp peaks at 2.0 flashes/s RF off and ≤ 1.0 RF on (Office Stutter 277,209: 9 → 2). The Office wall that flashed > 3/s over 37 % of a 1/4 field and 21 % of a 10° field is at 0 % over 3 and 0 % over 2. The worst Office room of 76 (5 Stutter/Failing lamps in view) stays at 0 % over 3 and ≤ 0.3 % over 2.
> Your limiter held back 0 frames in every logged sequence (360 lamp entries, 111 ambient lamps): the redesigned modes keep ≤ 2 by themselves, and the cap at 3 was never reached (your unit tests are its proof).
> RF: one-frame steps 0.30 → 0.06; one sag per episode.
> Optional, not required:
> - (a) Failing dropouts still cut in one frame (step 0.55–0.64, RF off). That is within the count; a 60–80 ms ramp like Stutter's would make it consistent.
> - (b) There is no field-level limit. Two lamps on one wall can put 5 changes into a pixel's second (seen on ≤ 0.3 % of a field). Fine now; keep it in mind for M1, when the room set's broken lamps join the W1 dips.
> - (c) The Dead-strike hold during warnings was not re-measured here (no Placard view in this stage).
> Report: Frontrooms3D/Documentation/research/lamp_warn/20_m2_recheck.md.

## 6. Files

- **This report;** the sheet `images/20_m2_recheck.jpg` (Shot format, VL385; spec in `W/vl_pending/m2_stutter.md`).
- **Data** (LF only):
  - `data/20_m2_compare.tsv` (the table);
  - `data/20_m2_lamps.tsv` (per lamp, before and after);
  - `data/20_m2_area_pixel.tsv` (area3 rows after M2; the before rows are C10's `data/10_area_pixel.tsv`);
  - `data/m2/` (the M2 capture's frames/lamps/events logs, run log, spots).
- **Clone-side** (never merged): `W/lamp_warn_c10/m2check/`:
  - `tools/` (`m2_curve.py`, `m2_lamps.py`, `m2_table.py`, `m2_sheet.py`, `run_area_m2.zsh`), `run_unity.zsh`, `wait_run_b.zsh`;
  - `runA/` and `runB/` (raw JPEG frames), `analysis/` (y4, ev, curves), `logs/`;
  - the harness `W/proj_m2check/Assets/Editor/LampWarn/FrontRoomsLampWarnCapture.cs` (v3m 6fe1e02b).
