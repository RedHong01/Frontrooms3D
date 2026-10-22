# C10 W1 warning lamps: lamp-look check after the 10-22 fix (analysis)

游戏视觉 workflow, analysis stage, 2026-10-22 10:0x–10:3x (session local_5656cffd), **corrected 10:5x after the verify stage** (see "Verify-stage corrections" below). It replaces the 08:43 report on the pre-fix build, which now sits in `prefix_1f631b10/analysis_prefix/` with its `10_*` data and images.

**Input.** The recapture on current main, 20 sequences at 60 fps, 1920 × 1080, through the game camera and post, seed 516574485:
- run 2 `p11_full` (09:15–09:29): natural spots, 8 sequences;
- run 3 `p13_pinned_ctl` (09:38–09:58): the pre-fix viewpoints pinned (`*Pre`), plus the same views with no warning (`*PreCtl`), 12 sequences.

**Code checked in main (read-only, sha1 re-checked 10:5x):**
- `FrontRoomsWarnLamps.cs` b11d8f93 (committed in 18acaa16, 09:48), `FrontRooms3DGame.cs` 88c93b09, `FrontRoomsMapWorld.cs` 7ef0ca45, `FrontRoomsMapHunter.cs` fb2fe3dd;
- `MAP_GENERATION.md` 47cc5b54, `FrontRoomsMapInteractionTests.cs` d9b51f23;
- `FrontRoomsExitSign.cs` 4848e486, `FrontRoomsPhosphor.cs` 257086e7, `FrontRoomsPlacardGlow.cs` c03448c9.

Nothing lamp-related has landed since 09:11 (LANDINGS.md read 10:05 and 10:5x).

**Load.** Both captures and this analysis ran UNDER LOAD: 1-minute load 41–248 during the captures, 33–121 during the analysis and the correction. Nothing below depends on wall time. Every capture frame advanced the game by 1/60 s (measured steps 0.0166–0.0167 s in all 20 logs), so every rate and envelope is on the game clock. No Unity job ran in the analysis or the correction.

## After-fix verdict (for the Alpha snapshot)

**W1 as it is in main is safe to ship in the Alpha. Photosafety: PASS.**
- The Failing-lamp snap that failed Reduce Flashing is **FIXED**: the step at the first dip and at the stage-0 return is now 0.011 or less, in all three rooms, RF off and on (it was 0.36–0.96).
- The lamps W1 dips peak at 2 flashes/s with RF off (the design target) and 1 with RF on.
- **By area a W1 dip is a WCAG general flash**, but never more than 2 a second: up to 35 % of a 10° field changes at once (L0 natural), 1.0 flash/s in the 10° field and 2.0 in the stricter 1/4 field. No W1 dip makes a screen-wide flash. The limit is 3.

**Pre-existing, not W1, and more urgent than this report first said:** a Stutter lamp lights a whole wall. In the Office natural view, Stutter 277,209 makes more than 3 flashes/s over 37 % of a 1/4 field (38 % at full resolution) and 21 % of a 10° field (21.5 % at full resolution). That fails the 1/4-field test and passes WCAG's 10° test by only 3.5 points. No W1 dip falls in that second, the Office no-warning control flickers the same way (20 % of a 1/4 field), and Stutter lamps flicker like this in every build. The fix is M2 (§9.5 ambient limiter), first in line after the Alpha.

What is still open for W1 is look and consistency, not safety, and it does not block the snapshot:
- the lit lenses stay white while the room dims;
- the zone reflection and the bounce stay flat;
- the room's broken tubes keep flickering through a warning (their flicker predates W1).

All of these are the agreed after-Alpha items (M1–M4, V1–V2).

**Open call for Red (does not block W1):** the Office Stutter flicker above is in every build, with or without W1. If the Alpha should also pass the stricter 1/4-field test, M2 has to land before the snapshot. Recommended: ship as is, because the WCAG 10° test passes, and land M2 first after the Alpha.

| Old finding (08:43 report, WarnLamps 1f631b10) | Now (b11d8f93) | Evidence |
|---|---|---|
| **Photosafety FAIL with Reduce Flashing:** the held-Steady snap moved a Failing lamp +0.65…+0.69 in one frame at the first dip and −0.38…−0.67 at stage 0: one hard-stepped flash per episode | **FIXED.** Edge steps RF on: L0 +0.001 / −0.005, Office +0.009 / +0.007, Tall −0.001 / +0.011. The RF burst is one sag to ×0.70 lasting 1.10–1.13 s. Sags start ≥ 3.9 s apart (start to start), so the quiet time between two sags is ≥ 2.7 s | `images/10_failing_edges_before_after.png`, `10_failing_snap_frames.jpg`, `video/10_L0_rf1_before_after.mp4` |
| **Failing lamps at 2.5 (L0) and 3.0 (Office) flashes/s with RF off** (snap + dips + the lamp's own fall) | **FIXED.** Stutter/Failing lamps take 0 dips (0 `LampDipped` in 14 W1 sequences, against 12–91 pre-fix) and 0 mode changes. Their counts equal the no-warning control. The lamps W1 dips peak at 2.0/s RF off, 1.0 RF on | `images/10_photosafety.png` |
| **Pops / an all-clear:** a Failing lamp went to full at the first dip and dropped back on the stage-0 frame (Office 0.98 → 0.02) | **FIXED.** Edge steps RF off: L0 +0.008 / −0.002, Office −0.003 / +0.004, Tall −0.003 / −0.010. The largest one-frame step of a dipping lamp is 0.104 (the 0.10 s attack). Every lamp ends at its base level and mode, with 0 overrides | `images/10_failing_edges_before_after.png`, `video/10_Office_rf0_before_after.mp4` |
| **Consistency FAIL:** the rendered lens stays white while the room dims | **OPEN** (V1, after the Alpha). Lens centre −0.7 to −6.6 % linear against LampLevel −39 to −45 %. Crops (L0 / Office / Tall): lens −7 / −18 / −5 %, surround −24 / −47 / −8 % | `images/10_lens_steady_vs_trough.jpg`, `10_follow.png` |
| Zone reflection and ambient bounce flat | **OPEN** (V2, after the Alpha). 0.50 / 0.45 through every dip | `images/10_follow.png` |
| Tall: a room-set Stutter lamp outside the burst set chattered at 9 flashes/s inside the warning | **Pre-existing, and now in every room:** no Stutter/Failing lamp is held any more. 5–11 flashes/s per Stutter lamp RF off inside warnings, 6–14 in the controls. A Stutter lamp lights a whole wall, so it is not a small-area flicker: Office natural > 3 flashes/s over 37 % of a 1/4 field and 21 % of a 10° field (limit 25 %), Office pinned 20 % / 11 % with and without a warning. §9.5 limiter = M2, first after the Alpha | `images/10_photosafety.png`, `10_ambient_through_warning.png` |
| Placard view: the Dead lamp's strike near a burst | **Pre-existing.** 2 strikes in this capture (4.43 s and 14.37 s, 2.0 s and 0.23 s from a burst). About 90 % of a 10° field, one isolated flash each (1.0/s, also when the second one ends on a W1 dip start at 14.65 s) | `images/10_placard_dead_blink.jpg` |
| Readability FLAG | **Still FLAG.** A little weaker where a broken tube is in view: the fix shrank the burst set (L0 6 → 4 live lamps, Office 8 → 5, Tall 28 → 21) | `images/10_readability.png` |
| Look FLAG (even V dips, white sources) | **Still FLAG.** The W1 constants did not change | `images/10_envelope_proposal.png`, `10_stage_rhythm.png` |
| Rules: not a radar, darkness never cover | **PASS, unchanged.** Depth, hold and gaps come from the run's RNG and the stage only. The hunter reads no lamp (0 hits for LampLevel / LampModeOf / intensity in `FrontRoomsMapHunter.cs`). A dip goes no lower than ×0.53 and lasts ≤ 0.47 s | code |

| # | Item | Verdict | One line |
|---|---|---|---|
| 1 | Photosafety | **PASS** (the snap: FAIL→FIXED) | W1 lamps ≤ 2.0 flashes/s RF off and 1.0 RF on. By area a dip is a general flash: ≤ 1.0/s in the 10° field (35 % of it in L0 natural), ≤ 2.0/s in the 1/4 field, 0 screen-wide. RF on: one sag per burst, no step, the 10° field never reaches 25 %. Above 2 is only ambient Stutter flicker, the same with no warning, and it exceeds 3/s over 37 % of a 1/4 field in Office natural (pre-existing, M2). |
| 2 | Readability | **FLAG** | Rooms dim 15–24 % linear (6–11 % Y′) from the natural spots. But where a broken tube is in view (L0 and Office pinned), a dip is no bigger than that tube's own flicker in the control. The lenses barely change, and stage 2 looks like stage 1. |
| 3 | Consistency | **FAIL** (visual side; not a blocker) | Light, lens emission value, walls, G14 glass and the placard glow follow LampLevel. The rendered lens and the zone reflection do not. EXIT signs: none on the map. Stutter/Failing lamps are left out as now specified, so their flicker shows through. |
| 4 | Look | **FLAG** | Not a strobe and not a UI pulse in timing. But the sources stay white, the dips are even metronomic Vs, and broken tubes ignore the brown-out, so it reads as the picture pumping, not 1990 ballasts sagging. |
| – | Rules | **PASS** | Not a radar; darkness is never cover. |

### Verify-stage corrections (applied 10:5x)

The verify stage re-measured the natural L0, Office, Tall and Placard sequences and the pinned views with their controls from the raw JPEGs and logs, with its own zigzag counter. It confirmed the ship verdict, the lamp counts, the edge steps, the readability medians, G14 and the placard. It corrected the following, now applied throughout:
1. **W1 dips are general flashes by area.** The first version said no W1 dip makes a 10° field flash; the old header of `10_photosafety.png` and VL331 said so too. Per pixel, the third dip of L0 natural's first burst puts 35 % of a 10° field in transition at once (1.0 flash/s), and a burst reaches 2.0/s in the 1/4 field. W1 also reaches 25 % of a 1/4 field in 8 other sequences (for example OfficePre_rf0 at its 15.567 s dip and Placard_rf0 at 14.65 s), not only in L0. Still ≤ 2/s, so PASS holds.
2. **The 64 × 60 px block method is retired.** It merged instants 0.1 s apart and ignored direction:
   - it undercounted fast flicker (Office_rf0: Stutter 277,209 at 22.55–23.55 s is > 3 flashes/s over 37 % of a 1/4 field, which the old table showed as 2.5, amber);
   - it overcounted same-direction instants (Office_rf1: two falls at 2.25 s and 2.37 s counted toward 1.5/s; it is 0.5).

   §1 now uses the verify stage's per-pixel method for every one of the 20 sequences and the 6 pre-fix ones.
3. **The frame-mean test cannot fire here.** Frame means are 0.045–0.110 linear, too low to swing by 0.10, so "0 frame-mean flashes" was no evidence and is dropped. The brightest 10 % (0.17–0.43) can swing and is kept.
4. **"One fixture each, so never a 10° or screen flash" was wrong.** A Stutter lamp lights a whole wall (item 2 above). Pre-existing and not W1, so the W1 verdict stands, but M2 is now marked as the first after-Alpha item.
5. **"Gaps ≥ 3.9 s" under RF was the start-to-start period.** The quiet time between two sags is ≥ 2.7 s (2.7–6.6 s; sags last 1.10–1.13 s).

### Message for 关卡设计 (exact)

> W1 lamp check on your 09:11 fix (WarnLamps b11d8f93, main 18acaa16): photosafety PASS, safe to ship in the Alpha, nothing to change on your side before it.
> FIXED: the one-frame Stutter/Failing snap at the first dip and at stage 0 (edge steps ≤ 0.011, were 0.36–0.96, RF off and on).
> FIXED: the Reduce Flashing hard step. RF is one ×0.70 sag per burst (1.1 s), sags ≥ 3.9 s apart start to start, ≥ 2.7 s quiet between them.
> FIXED: the extra flash on Failing lamps (the burst set peaks at 2.0 flashes/s RF off, 1.0 RF on) and the stage-0 all-clear (0 mode changes, every lamp back at base, 0 overrides).
> By area a dip is a WCAG general flash (35 % of a 10° field in L0), but ≤ 1.0/s in the 10° field and ≤ 2.0/s in a 1/4 field, so under the limit of 3.
> Pre-existing, not W1, more urgent than we said: a Stutter lamp lights a whole wall. In Office natural, Stutter 277,209 makes > 3 flashes/s over 37 % of a 1/4 field and 21 % of a 10° field (limit 25 %); the Office no-warning control flickers the same way.
> Still open, yours, after the Alpha as agreed (constants and table in report §4):
> - M2 first: the §9.5 ambient limiter (Stutter ≤ 3 flashes/s; RF ramps ≥ 0.5 s instead of one-frame steps of 0.30 Stutter / 0.17–0.21 Failing), plus no Dead strikes in the room set during an episode.
> - M1: crossfade the room's Failing lamps to Dim (0.42) and Stutter to Steady (≥ 0.3 s, ≥ 0.5 s RF) from the stage 0→1 entry, let them join the dips, restore 2–6 s after stage 0 over ≥ 1 s (burst set back to 6 / 8 / 28).
> - M3: DipAttack 0.10 → 0.05, DipHoldMin/Max 0.06/0.15 → 0.04/0.08, DipRelease 0.22 → 0.30 (exponential tail τ ≈ 0.09 s optional), depth first dip U(0.55, 0.62) then U(0.65, 0.75); keep DipSpacing 0.55.
> - M4: never wire SetMains to EXIT signs.
> - V1 hook: block.SetFloat("_FR_LampDip", level / max(baseLevel, 1e-3)) on the lens property block in both TickFixtures paths (1 when idle); the shader side is ours.
> Report: Frontrooms3D/Documentation/research/lamp_warn/10_lamp_look_check.md.

**Doc (系统设计, separately):** RELAY_PURSUIT_REDESIGN.md (bcdcef2b) §9.2's "Built on the landed layer" bullet still says the burst set is put to Steady from the first burst. That contradicts the Live-lamps bullet and b11d8f93.

## 1. Photosafety — PASS

**Method** (the verify stage's per-pixel method, run over all 26 sequences; `data/10_area_pixel.tsv`).
- **Flash rule:** WCAG 2.3.1 as in §9.5. A transition is a swing of ≥ 0.10 of full level between turning points, with the darker end < 0.80. A flash is a pair of opposing transitions, counted in every sliding 1 s window.
- **Levels measured:**
  1. the brightest 10 % of the frame (relative luminance, sRGB-decoded Rec.709). The frame mean (0.045–0.110 linear) cannot swing 0.10, so it is not a test here;
  2. per pixel, on a 480 × 270 linear-Y image (4 × 4 box mean), each pixel's own zigzag;
  3. **concurrent area:** a field flashes when ≥ 25 % of its pixels change within ±0.1 s. Each such instant gets its dominant direction, and only opposing instants pair up. Window stride 8 px;
  4. **flash density:** the share of a field whose pixels make ≥ 7 transitions (> 3 flashes) in one 1 s window. This catches fast flicker that the instants merge;
  5. each lamp's LampLevel (unchanged, verified);
  6. each lamp's rendered lens.
- **Fields:** the 1/3 × 1/3 screen (WCAG's 10° field at a standard distance), a stricter 1/4 × 1/4 (a large monitor up close), and the whole screen (ITU-R BT.1702).
- **Attribution:** an instant counts as W1 when it lies inside a dip (LampDipped −0.12 to +0.60 s RF off, to +1.30 s RF on). This counts ambient lamps that coincide with a dip as W1, so it errs against W1.

| Sequence | Brightest 10 % (flashes/1 s) | Field changing at once, worst (10° · 1/4) | Concurrent flashes/1 s, all (10° · 1/4 · screen) | of which inside W1 dips (10° · 1/4) | Share with > 3 flashes/1 s (10° · 1/4) | Burst-set lamps | Ambient Stutter/Failing (in the warning) | Worst lens on screen |
|---|---|---|---|---|---|---|---|---|
| L0_rf0 | 0 | 35 % · 55 % | 1.0 · 2.0 · 0 | 1.0 · 2.0 | 2 % · 3 % | 2.0 | 5.0 (273,199 Stutter) | 5.0 |
| L0_rf1 | 0 | 6 % · 10 % | 0 · 0 · 0 | 0 · 0 | 0 · 0 | 1.0 | 1.0 | 0 |
| Office_rf0 | 1.0 | 24 % · 41 % | 0 · **> 3** · 0 | 0 · 1.0 | **21 % · 37 %** (full res 21.5 · 38) | 2.0 | 9.0 (277,209 Stutter) | 9.0 |
| Office_rf1 | 0.5 | 15 % · 26 % | 0 · 0.5 · 0 | 0 · 0.5 | 0 · 0 | 1.0 | 1.0 (275,209 Failing) | 1.0 |
| Tall_rf0 | 0 | 19 % · 25 % | 0 · 0.5 · 0 | 0 · 0.5 | 1 % · 2 % | 2.0 | 8.0 (282,211 Stutter) | 8.0 |
| Tall_rf1 | 0 | 12 % · 18 % | 0 · 0 · 0 | 0 · 0 | 0 · 0 | 1.0 | 2.0 (281,213 Failing) | 1.0 |
| Placard_rf0 | 1.0 | 93 % · 98 % | 1.0 · 1.0 · 0 | 0.5 · 0.5 | 0 · 0 | 2.0 | – (Dead lamp, not logged) | – |
| Placard_rf1 | 0 | 0 % · 0 % | 0 · 0 · 0 | 0 · 0 | 0 · 0 | 1.0 | – | – |
| L0Pre_rf0 | 0 | 18 % · 30 % | 0 · 1.5 · 0 | 0 · 0.5 | 2 % · 5 % | 2.0 | 6.0 (276,202 Stutter) | 1.5 |
| L0Pre_rf1 | 0 | 16 % · 28 % | 0 · 1.0 · 0 | 0 · 1.0 | 0 · 0 | 1.0 | 2.0 (276,201 Failing) | 2.0 |
| OfficePre_rf0 | 0 | 22 % · 26 % | 0 · 1.0 · 0 | 0 · 1.0 | 11 % · 20 % | 2.0 | 10.0 (277,209 Stutter) | 11.0 |
| OfficePre_rf1 | 0 | 9 % · 14 % | 0 · 0 · 0 | 0 · 0 | 0 · 0 | 1.0 | 1.0 | 1.0 |
| TallPre_rf0 | 0 | 19 % · 25 % | 0 · 0.5 · 0 | 0 · 0.5 | 2 % · 3 % | 2.0 | 11.0 (285,215 Stutter) | 11.0 |
| TallPre_rf1 | 0 | 13 % · 18 % | 0 · 0 · 0 | 0 · 0 | 0 · 0 | 1.0 | 2.0 (281,213 Failing) | 1.0 |
| L0PreCtl_rf0 (no warning) | 0 | 17 % · 30 % | 0 · 1.0 · 0 | – | 4 % · 7 % | – | 6.0 (276,202 Stutter) | 1.5 |
| L0PreCtl_rf1 | 0 | 15 % · 27 % | 0 · 0.5 · 0 | – | 0 · 0 | – | 2.0 (276,201 Failing) | 2.0 |
| OfficePreCtl_rf0 | 0 | 14 % · 25 % | 0 · 0.5 · 0 | – | 11 % · 20 % | – | 14.0 (274,209 Stutter) | 14.0 |
| OfficePreCtl_rf1 | 0 | 2 % · 4 % | 0 · 0 · 0 | – | 0 · 0 | – | 1.5 (275,209 Failing) | 1.5 |
| TallPreCtl_rf0 | 0 | 4 % · 5 % | 0 · 0 · 0 | – | 2 % · 3 % | – | 11.0 (281,210 Stutter) | 11.0 |
| TallPreCtl_rf1 | 0 | 2 % · 3 % | 0 · 0 · 0 | – | 0 · 0 | – | 2.0 (281,213 Failing) | 2.0 |

Pre-fix, at the pinned viewpoints, the burst-set lamps reached 2.5 / 1.5 (L0 RF off / on), 2.5 / 1.5 (Office) and 2.0 / 1.5 (Tall). The 08:43 report's 3.0 for Office also counted the Failing lamp's own fall just before the snap. By area, pre-fix, the same method gives ≤ 1.0/s in any field and 0 % > 3 flashes in Office (the old Steady hold silenced the Stutter lamp during warnings).

**W1 itself.**
- **Count:** every lamp W1 dips peaks at exactly 2.0 flashes/s with RF off (dips 0.55 s apart, one flash each) and 1.0 with RF on: the design target, under the limit of 3. Dip starts per lamp ≤ 2 a second. A burst lasts ≤ 1.58 s, under §9.5's 5 s episode.
- **Area: a dip is a general flash, at ≤ 2 a second.** In L0 natural, the third dip of the first burst (LampDipped 2.33 s) puts 35 % of a 10° field in transition at once: 1.0 flash/s in the 10° field. Two dips 0.6 s apart (8.28–9.23 s) make 2.0/s in the 1/4 field. W1 reaches 25 % of a 1/4 field in 9 of 14 W1 sequences (L0, Office, Tall and Placard natural; L0, Office and Tall pinned), at 0.5–2.0/s, and of a 10° field in L0 natural and Placard. In Placard the second Dead-lamp strike ends on the 14.65 s dip start (1.0/s in total). 0 screen-wide flashes.
- **Frame level:** the brightest 10 % flashes 1.0/s in Office_rf0 (the in-view Failing lamp's return from a dropout at 1.75 s lands on the release of W1's second dip) and in Placard_rf0 (the Dead lamp's strikes), and 0.5/s in Office_rf1. The frame mean is not a test here (see Method).
- **Reduce Flashing:**
  - every dipping lamp makes one sag per burst to ×0.69–0.71 of its base, lasting 1.10–1.13 s. Sags start 3.9–7.8 s apart (start to start), so the quiet time between two sags is ≥ 2.7 s (2.7–6.6 s; `tools_v3/rf_quiet.py`);
  - the largest one-frame step of a dipping lamp is 0.044 (its own shimmer plus the slope; 0.032 in the control);
  - by area: the 10° field never reaches 25 % (worst 16 %); the 1/4 field does in Office_rf1 (26 %, 0.5/s) and L0Pre_rf1 (28 %, 1.0/s). That is one sag, one flash.

  The old FAIL was entirely the Stutter/Failing snap, and that is gone (`10_failing_edges_before_after.png`: pre-fix red against fixed black at both edges, all three rooms, RF off and on).

**Not W1 (pre-existing, inside warnings because they are everywhere):**
- **Stutter lamps, RF off:** one-frame 0.90–0.95 swings, 5–11 flashes/s per lamp inside warnings and 6–14 in the controls at the same views. **A Stutter lamp lights a whole wall**, so by area:
  - Office natural: Stutter 277,209 at 22.55–23.55 s, > 3 flashes/s over 37 % of a 1/4 field (38 % at full resolution, `W/lamp_warn_c10/verify_v2/office_rf0_stutter_check.jpg`) and 21 % (21.5 %) of a 10° field. It fails the 1/4-field test and passes WCAG's 10° test by 3.5 points. No dip falls in that second;
  - Office pinned (Stutter 274,209 and 277,209 in view): 20 % / 11 %, with a warning and in the no-warning control alike.
- **Failing lamps:** L0 pinned 1.5/s in a 1/4 field from Failing 276,201 at 15.4–16.4 s, not in a dip (control 1.0).
- **Under RF:** Stutter lamps still step 0.25–0.30 in one frame (1.0 ↔ 0.7) and Failing lamps 0.17–0.21. That is not the "soft dip" RF promises (same in the controls).
- **Placard view:** the Dead lamp over the door cell strikes for 0.2 s (frame mean +32 % Y′, about 90 % of a 10° field), once in stage 1 and once 0.23 s before a stage-2 burst. One isolated general flash each (1.0/s), which WCAG allows.
- All of this is §9.5's ambient limiter (M2). It is unchanged by W1 and in every build, and the Office case makes M2 the first after-Alpha item.

## 2. Readability — FLAG

Medians over a room's bursts (n = 3 per stage), RF off. Each drop is measured from the 0.5 s before the burst to its trough. The pinned rows add the pre-fix medians at the same viewpoint and the no-warning control's 95th-percentile drop on the same measure (`data/10_ctlnoise.tsv`), i.e. how much that view dims on its own. See `images/10_readability.png`, `10_follow.png`, `overview_steady_vs_trough.jpg`, `*_s1.jpg`, `*_s2.jpg` and `video/*.mp4`. The verify stage reproduced these medians from the raw JPEGs.

| Room · stage | LampLevel | Frame mean, linear | Frame mean, Y′ | Brightest 10 %, linear | Lens centre, linear |
|---|---|---|---|---|---|
| L0 natural · 1 | −45 % | −22 % | −10.8 % | −14 % | −1.3 % |
| L0 natural · 2 | −39 % | −19 % | −9.0 % | −12 % | −1.3 % |
| Office natural · 1 | −39 % | −24 % | −11.1 % | −23 % | −5.7 % |
| Office natural · 2 | −38 % | −18 % | −9.0 % | −12 % | −5.1 % |
| Tall natural · 1 | −41 % | −15 % | −6.1 % | −16 % | −3.6 % |
| Tall natural · 2 | −39 % | −16 % | −6.9 % | −16 % | −3.4 % |
| Placard (dead-lamp cell) · 1 | −40 % | −9 % | −4.4 % | −7 % | no lens on screen |
| Placard · 2 | −38 % | −14 % | −6.6 % | −12 % | no lens on screen |
| L0 pinned · 1 | −45 % | −11 % (pre-fix −14; control p95 10.0) | −5.5 % (pre-fix −6.7; control 3.9) | −11 % | −1.5 % |
| L0 pinned · 2 | −39 % | −8 % (pre-fix −13) | −3.7 % | −7 % | −1.6 % |
| Office pinned · 1 | −39 % | −14 % (pre-fix −21; control 11.1) | −6.6 % (pre-fix −10.0; control 5.8) | −14 % | −6.6 % |
| Office pinned · 2 | −38 % | −13 % (pre-fix −20) | −5.9 % | −13 % | −5.9 % |
| Tall pinned · 1 | −41 % | −15 % (pre-fix −19; control 3.0) | −6.2 % (pre-fix −8.2; control 1.3) | −14 % | −0.7 % |
| Tall pinned · 2 | −39 % | −17 % (pre-fix −18) | −6.9 % | −16 % | −0.7 % |

With RF on (one ×0.70 sag), frame mean, linear:
- natural: L0 −14 %, Office −16 %, Tall −11 %, Placard −9 %;
- pinned: L0 −5 % (control 6.6), Office −10 % (3.6), Tall −11 % (1.6).

**At a glance.**
- **Natural spots:** a 15–24 % linear (6–11 % Y′) whole-room dimming over 0.1 s reads in every lit room type. The dead-lamp cell (−4 to −7 % Y′) is subtle.
- **Smaller than before at the same viewpoints:**

  | Room (pinned) | Frame mean, linear, pre-fix → now |
  |---|---|
  | L0 | −14 → −11 % |
  | Office | −21 → −14 % |
  | Tall | −19 → −15 % |

  The hotfix took the Stutter/Failing lamps out of the burst set, and they used to dip too.
- **Lost in the noise where a broken tube is in view:** in the L0 and Office pinned views, a stage-1 dip is only 1.1–1.3 × the view's own flicker (control p95). A stage-2 dip in L0 is below it (0.8 ×). So the room's failing tube makes dips of the same size all the time. What still separates W1 is the pattern: all the lit lamps together, 2–3 times, 0.55 s apart. A player who has not learned that may read it as more of the same flicker. Tall pinned (no broken tube in view) has a clear margin: 4.9–5.5 × its control.
- **Lens:** the troffer, which is what a player looks at to see whether the lights dipped, barely changes: lens centre −0.7 to −6.6 % linear. It reads as "the picture pumped" more than "the lights sagged" (§3).
- **Too strong:** none. The largest single trough is −31 % linear (Office natural, stage 1, where the Failing lamp's own dropout adds to it). The spec range for a frame-level target is 20–35 % linear.

**Stage 1 against stage 2.** The constants did not change (Monte Carlo, `images/10_stage_rhythm.png`).

| | Dips per burst | Start-to-start period | Seen in this capture |
|---|---|---|---|
| Stage 1 | 2–3 | 4.0–8.1 s, mean 6.1 s | 4.43–7.25 s |
| Stage 2 | 2 | 3.5–5.1 s, mean 4.3 s | 3.57–5.03 s |

- 70 % of stage-2 periods also fit a 2-dip stage-1 burst.
- **Not learnable from the lamps,** by design (v2: the lamps mean "near"; W2 steps mean stage 2).
- W2 is CLONE_ONLY until after the Alpha cut (LANDINGS 10:00), so **in the Alpha playtest stage 2 has no separate signal**. The playtest form should say so. It should also note that in rooms with broken tubes, the ambient flicker can be mistaken for a warning.

## 3. Consistency — FAIL (visual side, OPEN; not a blocker)

`images/10_follow.png` shows one stage-1 burst per room, with every reader divided by its own pre-burst value. `images/10_lens_steady_vs_trough.jpg` shows the lens against its surround, and `*_curves.png` the whole sequences.

| Reader | Reads | Through a dip (RF off) | Verdict |
|---|---|---|---|
| Light.intensity | baseIntensity × LampLevel × fade (map) | equal to the formula within 5e-4 for every dipping lamp, every frame, 20 sequences | PASS |
| Lens `_EmissionColor` (property block) | authored × max(0.04, LampLevel) | within 2e-4 | PASS |
| **Rendered lens** | the above through ACES and bloom | lens centre −1.3 / −5.7 / −3.6 % (natural L0 / Office / Tall), −1.5 / −6.6 / −0.7 % pinned, while LampLevel falls 39–45 % and the frame 11–24 %. Crops round the brightest troffer: lens −7 / −18 / −5 %, surround −24 / −47 / −8 % | **FAIL** (V1) |
| Zone reflection | the zone cube + LampModeOf at the player's cell | flat: 0.50 (0.45 Tall) through every dip | FLAG (V2) |
| Ambient trilight | constants (`FrontRoomsLook.ApplyAmbient`) | flat (not a lamp reader today) | FLAG (V2) |
| G14 RT glass | each lamp's `Light.intensity` | Tall natural: window region −25 % against the lamp-lit wall −34 % (RF on −19 / −24 %), r = 0.994 / 0.996 with LampLevel, ≤ 92 lamps, 12 189 glass px | PASS |
| EXIT sign | `SetMains` (no caller on the map) | 0 `FrontRoomsExitSign` in the map scene in all 20 sequences (only RoomStream rooms build them) | N/A (guard M4) |
| Placard glow (`FrontRoomsPlacardGlow` / `FrontRoomsPhosphor`) | the cell's and 4 neighbours' LampLevel | the glow cell is the Dead door lamp (never dips). Shown −3.3 % (stage 1) / −4.1 % (stage 2) RF off, −3.8 / −3.3 % RF on, as 2 neighbours dip. Slow, no pulse. The Dead lamp's own strikes push Ls to 0.33 (pre-existing) | PASS |
| Wallpaper print | the scene's lights (lit surface; the phosphor ink is not in main) | walls, middle third: −16 / −15 / −7 % linear (natural L0 / Office / Tall, stage 1), in step with the frame | PASS |
| EXAMINE look (`FrontRoomsInspectLook`) | LampLevel of the cell + neighbours (code) | not captured here | not measured |

- **Pops: none from W1.** The largest one-frame step of a dipping lamp is 0.104, which is the first frame of the 0.10 s attack (14 such frames in all 20 sequences, each on the frame its `LampDipped` fires or the next). Stutter/Failing lamps:
  - 0 dips and 0 mode changes in 14 W1 sequences (pre-fix: every one switched mode twice);
  - their steps at the burst edges are ≤ 0.011;
  - the large steps left in `10_failing_edges_before_after.png` (Office +0.75 s, L0 stage 0 +0.95 s) are the Failing lamps' own dropouts, the same size as in the controls (0.54–0.60).
- **Lamps left lit or dark after the episode: none.** On the last frame of all 20 sequences every lamp is at its base level and its starting mode, with 0 overrides.
- **Stutter/Failing "held Steady as specified":** the specification changed with the hotfix. They are now left out, like Dead and Off, and keep their own mode (MAP_GENERATION.md 47cc5b54, RELAY §9.2 "Live lamps"). Measured: that is exactly what they do.
  - The consequence is the one §9.2 had the hold for: ambient flicker shows through the warning (`10_ambient_through_warning.png`). Example: in L0 pinned, Stutter 276,202 chatters during the third dip of the first burst (2.0–2.4 s).
  - RELAY §9.2's "Built on the landed layer" bullet still describes the old Steady hold (for 系统设计).

## 4. Look: 1990 ballasts or a picture pulse? — FLAG

**Background** (general lighting engineering, not measured here; the research chat should source it if the art direction pursues it):
- 1990 US offices mostly ran T12 tubes on magnetic ballasts, and their output tracks supply voltage within a half cycle.
- A motor-start sag falls almost at once and recovers over tenths of a second, fast at first and then with a tail. The first hit is the deepest.
- The weakest tube on the circuit drops out first, and the room's bounce falls with the troffers.

**Against that:**
- **Good:** the dips are synchronous (±40 ms), never black (≥ ×0.53), ≤ 1.8 Hz and grouped 2–3 per burst with quiet gaps. Not a strobe, not a disco effect. The Reduce Flashing sag reads as a slow brown-out.
- **Wrong way round (unchanged):** the source stays white while the walls dim. This is the main reason it reads as a post or UI pulse. Fix: V1.
- **Too regular (unchanged):** a symmetric-looking linear V every 0.55 s, with depths drawn independently. Fix: M3.
- **New with the hotfix:** in a real sag the failing tube is the first to drop. Here broken tubes ignore the brown-out and keep their own sine-and-dropout or chatter, sometimes in the middle of a dip. That does not break the fiction (they are broken), but it weakens "the circuit sags". Fix: M1 (held Dim, then dipping with the rest).
- **Not needed:** a colour shift (marginal behind a prismatic lens) and a recovery flicker (a sag that does not extinguish the arc recovers smoothly). The ballast character belongs to the WarnBurst groan (sound).

**M3, the proposed envelope** (`images/10_envelope_proposal.png`; photosafety-neutral):

| Constant (`FrontRoomsWarnLamps`) | Now | Proposed |
|---|---|---|
| `DipAttack` | 0.10 | 0.05 |
| `DipHoldMin` / `DipHoldMax` | 0.06 / 0.15 | 0.04 / 0.08 |
| `DipRelease` | 0.22 linear | 0.30 with an exponential tail (τ ≈ 0.09 s) in `TickLampOverrides`, or 0.30 linear if the code change waits |
| Depth | U(0.55, 0.70) for every dip | first dip of a burst U(0.55, 0.62), later dips U(0.65, 0.75) |
| `DipSpacing` | 0.55 | 0.55 (keep: it is the photosafety constant) |

- A dip is then ≤ 0.43 s, so dips never merge, and a stage-1 burst is ≤ 1.61 s.
- The simulated burst keeps 6 transitions and ≤ 2 flashes/s; RF is untouched.
- Do not deepen the dips for the Alpha. After V1, V2 and M1, re-measure. Only if a broken-tube room still sits under its ambient p95 should DipDepthMin go toward §9.2's allowed 0.45.

## Proposed changes, owner by owner

**Nothing is needed before the Alpha** (see the open call for Red above). All of these are the agreed after-Alpha follow-ups. The numbers are updated to this recapture.

### 关卡设计 (map; their files)

- **M2 first: ambient limiter (§9.5) and hold scope.**
  - A Stutter lamp lights a whole wall: in Office natural it makes > 3 flashes/s over 37 % of a 1/4 field and 21 % of a 10° field (limit 25 %), with or without a warning. Stutter mode 1 runs at 5–14 flashes/s per lamp RF off in every build. The limiter should hold every Stutter lamp at ≤ 3 flashes/s (≤ 2 to match W1's target).
  - With RF on, Stutter steps 0.30 and Failing 0.17–0.21 in one frame. RF's ambient dip should ramp like the overrides (≥ 0.5 s).
  - During an episode, suppress Dead-mode strikes in the room set (the Placard view: about 90 % of a 10° field, 0.23 s before a burst).
- **M1: hold and rejoin, without a pop.** Fixes the item-2 shrinkage, item-3 show-through and item-4 "broken tubes ignore the sag".
  - From the stage 0 → 1 entry (0.3–1.0 s before the first burst), crossfade the room set's Failing lamps to Dim (0.42 ± 0.03) and its Stutter lamps to Steady: ≥ 0.3 s, ≥ 0.5 s with RF.
  - Once held, they join the burst set. Each dip is then one flash on them too, still ≤ 2/s.
  - Restore at a random 2–6 s after stage 0 with a ≥ 1.0 s crossfade, never on the stage-0 frame (RESTORE stays audio-only, Q3c).
  - Implement it as a per-fixture mode crossfade in `LampTick`, which also takes the pop out of every other `SetLampMode`.
  - Expected: burst set back to 6 / 8 / 28 at the old viewpoints, so the frame dip goes back to about −14 / −21 / −19 % linear, with no ambient flicker during the episode and 0 edge steps.
- **M3:** the envelope table in §4 (constants; the release shape optional).
- **M4:** never wire `SetMains` to EXIT signs. At ×0.55 a dip crosses the sign's 0.55 `TransferLevel`, giving a black gap and a DC transfer at every deep dip. The true EXIT stays LIT-locked.
- **V1 hook:** in both TickFixtures paths, `block.SetFloat("_FR_LampDip", level / max(baseLevel, 1e-3))` on the lens property block. It is 1 when idle, so it is bit-identical outside overrides.

### 游戏视觉 (visual; ours)

- **V1: make the lens dip** (`Assets/Resources/Rendering/FrontRoomsSurface.shader`, a82fe187; needs the map's V1 hook).
  - In the troffer lens path, emission × `pow(_FR_LampDip, k)`. Start at k = 2.5 and sweep 1.5–4 on a clone lookdev capture.
  - Calibrate so that the rendered lens loses ≥ 0.8 × (1 − dip) of its display-linear luminance, i.e. ≥ 36 % at ×0.55 (today −0.7 to −6.6 %). Bloom follows by itself.
  - Desktop first; WebGL decides separately.
- **V2: make the bounce follow its lamps** (`Assets/Scripts/Rendering/FrontRoomsLook.cs` `ApplyAmbient`, `Assets/Scripts/Rendering/FrontRoomsZoneReflection.cs`).
  - r = Σ wᵢ·LampLevel / Σ wᵢ·LampBaseLevel over the lamps within lightRadius of the camera, with wᵢ = fade / max(d², 4). r = 1 when idle.
  - Scale the ambient trilight and the zone reflection intensity by 1 − 0.8·(1 − r).
  - Keep `_FR_ZoneReflNominal` steady so that FrontRooms/Glass and G14 dip with the room.
  - It reads only LampLevel and LampBaseLevel (the F7 rule). Lamps near the Relay never dip, so r stays ≈ 1 there.
- **Targets after V1 + V2 (+ M1),** RF off, every lit room type:
  - frame mean 20–35 % linear (≥ 9 % Y′);
  - rendered lens ≥ 30 % linear;
  - ≥ 2 × the view's ambient p95.
- **Re-check after V1 + V2 with the per-pixel method** (`W/lamp_warn_c10/tools_v3/area3.py`, not the retired block method): V1 makes the lenses and V2 the whole room change together, so the share of a field changing at once will grow. W1 must stay ≤ 2/s in the 10° and 1/4 fields, and RF must stay one sag.

### Others

- **系统设计:** align RELAY_PURSUIT_REDESIGN.md §9.2's "Built on the landed layer" bullet with the hotfix. It still describes the Steady hold.
- **Sound (声音设计):** the WarnBurst ballast groan and the W2 steps. Both are CLONE_ONLY until after the Alpha cut, and W2 is the only stage-2 signal.
- **Playtest form:** in the Alpha, stage 2 looks like stage 1, and a broken tube's own flicker can be mistaken for a warning.

## VL329–VL331

Rewritten with verdicts in `W/vl_pending/c10_lamps.md` (slot images largest first, by pixel area):

| VL | Verdict | Slot images |
|---|---|---|
| VL329 | FLAG | `10_readability.png`, `10_stage_rhythm.png`, `10_envelope_proposal.png` |
| VL330 | `FAIL lens, reflection / PASS light, G14, placard` | `10_lens_steady_vs_trough.jpg`, `10_follow.png` |
| VL331 | PASS | `10_ambient_through_warning.png`, `10_failing_edges_before_after.png`, `10_photosafety.png`, `10_failing_snap_frames.jpg`, `10_placard_dead_blink.jpg` |

## Files

- **This report and its outputs** (all text LF only):
  - `data/10_bursts.tsv`, `10_lamps.tsv`, `10_seq.tsv`, `10_jumps.tsv`, `10_ctlnoise.tsv`, `10_numbers.json`;
  - `data/10_area_pixel.tsv` (per-pixel concurrent area, W1 attribution and > 3 / > 2 flash density for all 26 sequences) and `10_photomatrix_pixel.json` (the cells of `10_photosafety.png`). They supersede `10_area.tsv`, `10_attribution.tsv` and `10_numbers.json`'s `photoMatrix` (block method, kept for the record);
  - `images/10_*.png|jpg` (Shot format). `10_photosafety.png` was redrawn at 10:54 with the per-pixel method and a corrected header; the block-method version is in `W/_partial/c10_lamp_warn_1022/`;
  - `video/10_L0_rf1_before_after.mp4` and `video/10_Office_rf0_before_after.mp4`: side by side, pre-fix on the left and fixed on the right, first burst −0.75…+2.0 s then stage 0 −0.5…+1.0 s, 60 fps.
- **Capture stage** (unchanged, chips PENDING): `data/*_frames.tsv`, `*_lamps.tsv`, `summary.tsv`, `pinned_*`; `images/<seq>_*`; `video/<seq>.mp4`.
- **Tools** (clone side, never merged):
  - `W/lamp_warn_c10/tools_v2/`: `analyse2.py` (bursts, lamps, jumps, control noise), `pixflash.py`, `pixblocks.py`, `attribute.py`, `plots10v2.py` (the other images); `areaflash2.py` (block method) is retired;
  - `W/lamp_warn_c10/tools_v3/`: `area3.py` (the verify stage's per-pixel zigzag, concurrent area with direction, density, W1 attribution), `run_area3.zsh`, `sum3.py`, `rf_quiet.py`, `plot_photosafety3.py`;
  - the verify stage's own scripts and decoded frames: `W/lamp_warn_c10/verify_v2/`.

  Work files are in `W/lamp_warn_c10/analysis_v2/` and `analysis_v3/`.
