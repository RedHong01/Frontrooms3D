# C10 W1 warning lamps: lamp-look check (analysis)

游戏视觉 workflow, analysis stage, 2026-10-22 08:2x–08:5x. Input: the capture stage's 8 sequences (`data/`, `images/`, `video/` in this folder; raw frames in `W/lamp_warn_c10/final/frames`). Code read in main (read-only): `FrontRoomsWarnLamps.cs` (1f631b10), `FrontRoomsMapWorld.cs` lamp tick and override layer (7ef0ca45), `FrontRoomsPlacardGlow.cs`, `FrontRoomsPhosphor.cs`, `FrontRoomsZoneReflection.cs`, `FrontRoomsExitSign.cs`, `GlassRT/FrontRoomsGlassRTSystem.cs`, `FrontRoomsLook.cs`, `Resources/Rendering/FrontRoomsPost.asset`. Rules: MAP_GENERATION.md "Warning lamps (W1…)"; RELAY_PURSUIT_REDESIGN.md §9.2–§9.6; VISUAL_CHAT_TASKS.md F7.

**Load.** The capture ran UNDER LOAD (1-min load 213–742) and so did part of this analysis (44–180). Nothing here depends on wall time: every capture frame advanced the game by exactly 1/60 s (measured: 0.01660–0.01670 s steps in all 8 logs) and no Unity frame was dropped. So the envelopes, rates and gaps are game-clock values.

**Tools (clone side, never merged):** `W/lamp_warn_c10/tools/pixflash.py` (per-pixel transition area), `pixblocks.py` + `areaflash.py` (concurrent-area flash count), `analyse.py` (bursts, lamps, jumps), `plots10.py` (the `10_*` images and `data/10_numbers.json`). Outputs: `data/10_bursts.tsv`, `10_lamps.tsv`, `10_seq.tsv`, `10_jumps.tsv`, `10_area.tsv`, `10_numbers.json`; images `images/10_*`.

## Verdicts

| # | Item | Verdict | In one line |
|---|---|---|---|
| 1 | Photosafety | **FAIL** (Reduce Flashing only) | W1's dips never make a general flash (0 in a WCAG 10° field, 0 screen-wide). But with Reduce Flashing on, the held-Steady snap still moves a Failing lamp +0.65 in one frame, then the sag pulls it back down: one flash per episode. With RF off, the snap takes a Failing lamp to 2.5–3.0 flashes/s (design target 2, hard limit 3). |
| 2 | Readability | **FLAG** | Lit rooms dim 13–21 % linear (6–10 % Y′), a dead-lamp cell 9–10 %. The lit lenses do not visibly dim except in the Office. From the lamps alone, stage 2 looks the same as stage 1. |
| 3 | Consistency | **FAIL** | The light and the lens emission value follow LampLevel exactly, but the rendered lens stays white (−1 % in L0 and Tall) while the room darkens. The zone reflection stays flat. Failing lamps pop at the first dip and again at stage 0. A room-set Stutter lamp outside the live set strobes inside the warning. |
| 4 | Look | **FLAG** | Not a disco strobe. But the sources stay white while everything else dims, and the dips are evenly spaced V shapes, so it reads more as a picture pulse than as 1990 ballasts sagging. |
| – | Rules: not a radar, darkness never cover | PASS | The dip's depth, hold and gaps come from the run's own RNG and the stage only. The hunter's senses never read lamps (grep: no LampLevel / Light in `FrontRoomsMapHunter.cs`). Each dip goes no lower than 0.53 of a lamp's level and lasts at most 0.47 s. |

## 1. Photosafety

**Method.** WCAG 2.3.1 as in §9.5. A transition is a swing of at least 0.10 of full level, measured between hysteresis turning points, whose darker end is below 0.80. A flash is a pair of opposing transitions, counted in every sliding 1 s window. This is applied at four levels:

- **Pixels:** per-pixel relative luminance (sRGB decode, Rec.709) from the raw JPGs at 1/4 scale, and the share of a field that is in a transition on each frame.
- **Concurrent area:** 64×60 px blocks. A transition counts for a field only when blocks covering 25 % of that field change within ±0.1 s of each other. Three field sizes:
  - 1/3 × 1/3 of the screen (WCAG's 341×256 at 1024×768, the standard 10° field);
  - 1/4 × 1/4 (a large monitor up close);
  - the whole screen (ITU-R BT.1702 / Ofcom).
- **Each lamp's LampLevel.**
- **Each lamp's rendered lens** (the stricter local count).

| Sequence | Worst 10° field in transition (1/3 · 1/4) | Concurrent flashes / 1 s (1/3 · 1/4 · screen) | Worst live lamp, flashes / 1 s | Worst lens on screen | Notes |
|---|---|---|---|---|---|
| L0 rf0 | 17 % · 30 % | 0 · 1.5 · 0 | **2.5** (Failing 276,201) | 1.0 | 1/4-field peak = Failing ambient fall + snap + first dip (0.73–1.73 s) |
| L0 rf1 | 12 % · 22 % | 0 · 1.0 · 0 | 1.5 (Failing: snap + sag) | 1.0 | |
| Office rf0 | 24 % · 35 % | 0 · 1.0 · 0 | **3.0** (Failing 275,209, 0.72–1.68 s) | 1.0 | 6 transitions in 0.96 s: ambient, snap, 2 dips |
| Office rf1 | 10 % · 16 % | 0 · 0 · 0 | 1.5 (Failing: snap + sag) | 1.0 | |
| Tall rf0 | 18 % · 28 % | 0 · 0 · 0 | 2.0 | **9.0** (Stutter 282,210, not in the burst) | see §3, stutter leak |
| Tall rf1 | 9 % · 15 % | 0 · 0 · 0 | 1.0 (Failing 2.0 incl. snap) | 1.0 | |
| Placard rf0 | **92 %** · 98 % | **1.0** · 1.5 · 0 | 2.0 | (no lens) | the Dead door-cell lamp's blink at 7.48 s, not W1 |
| Placard rf1 | 0 % · 0 % | 0 · 0 · 0 | 1.0 | (no lens) | |

- **W1 dips alone are within WCAG with margin.** Frame mean and brightest 10 % never make a ≥ 0.10 transition. In any WCAG field the dips never reach 25 % concurrent area. A steady lamp gives exactly 2 flashes/s at most (dips 0.55 s apart): the design target, under the limit of 3. Episodes last ≤ 1.6 s, well under §9.5's 5 s.
- **The snap costs one extra transition per episode.** At the first dip a burst's Failing lamp jumps to Steady in one frame (L0 0.26 → 0.98, Office 0.50 → 1.00, Tall 0.56 → 0.98). Added to the Failing lamp's own fall just before and the burst's dips, that lamp reaches **3.0 flashes/s in Office** (0.72–1.68 s) and 2.5 in L0. That is still not "more than three", but it breaks the ≤ 2 target that W1 meets everywhere else. It is local to one lamp: at most 1.5 concurrent flashes/s in a 1/4 field. `images/10_photosafety.png`, `images/10_failing_snap_curves.png`, `images/L0_rf0_failing_snap.jpg`.
- **Reduce Flashing: the sag itself is right, the snap breaks it (FAIL).**
  - Every lamp makes one sag per burst to 0.700 × base (±0.0001). It falls for 0.567 s and recovers for 0.567 s, measured between 0.99 and the trough, which matches 0.6 s ramps. Gaps are ≥ 3.9 s, and a steady lamp counts 1 flash per window.
  - But the Failing hold is a mode switch, not an override, so the ≥ 0.5 s RF envelope never applies to it:
    - at the first burst, L0 0.33 → 0.98, Office 0.31 → 1.00, Tall 0.44 → 1.00 in one frame;
    - at stage 0, back to 0.60 / 0.37 / 0.33 in one frame.
  - The snap plus the sag that follows is a pair of opposing changes ≥ 0.10 with the darker end < 0.8: one flash per episode for the RF player, with a hard step in it. Fix M1 below.
- **Not W1, but inside its episodes** (§3 has the detail):
  - Tall: a Stutter lamp of the room set outside the live set chattered at 9 flashes/s, lens on screen, twice during the warning. Its area is small, so it is not a WCAG general flash. §9.5's ambient limiter for mode 1 has not landed: `Level()` still runs `PerlinNoise(t·23)`.
  - Placard: the Dead lamp over the player blinked 0 → 0.8 for about 0.13 s, 0.1 s before burst 2. It covered 92 % of a 10° field: one isolated flash, which WCAG allows. Together with the W1 trough just before it, the brightest-10 % swing reached 0.115.

## 2. Readability

Medians over a room's bursts with RF off (n = 3; Placard stage 1 n = 2, because the dead-lamp blink spoils burst 2). The drop is measured from the 0.5 s before the burst to its trough. `images/10_readability.png`, `images/overview_steady_vs_trough.jpg`, `images/*_s1.jpg`, `video/*.mp4`.

| Room · stage | Lamp level | Frame mean, linear | Frame mean, Y′ | Brightest 10 %, linear | Rendered lens, linear |
|---|---|---|---|---|---|
| Level 0 room · 1 | −42 % | −14 % | −6.7 % | −6.8 % | −1.3 % |
| Level 0 room · 2 | −39 % | −13 % | −6.0 % | −6.0 % | −1.3 % |
| Office · 1 | −39 % | −21 % | −10.0 % | −18 % | −7.5 % |
| Office · 2 | −38 % | −20 % | −9.8 % | −18 % | −13 % |
| Tall hall · 1 | −40 % | −19 % | −8.2 % | −17 % | −0.8 % |
| Tall hall · 2 | −39 % | −18 % | −7.8 % | −16 % | −0.7 % |
| Placard (dead-lamp cell) · 1 | −44 % | −10 % | −4.8 % | −9 % | no lens on screen |
| Placard (dead-lamp cell) · 2 | −40 % | −9 % | −4.5 % | −7 % | no lens on screen |

- **Lenses.** These are the medians for steady live lenses on screen, bursts 2–6, in sRGB code value and in linear:

  | Room | Median code drop (linear) | Max code drop (linear) |
  |---|---|---|
  | L0 | 0.9 % (2.1 %) | 1.2 % (2.8 %) |
  | Office | 3.3 % (7.2 %) | 9.6 % (20 %) |
  | Tall | 1.0 % (2.3 %) | 3.3 % (7.4 %) |

  The lens emission is 3.3 HDR (Office 2.2) going through ACES (`FrontRoomsPost` tonemap mode 2), so a 45 % emission dip lands on the shoulder.
  - Even a Failing lens at level 0.26 still renders at 0.57 code against 0.77 at full (`10_failing_snap_curves.png`, red).
  - The heat maps (`*_diff.png`) show the same thing: lens interiors do not change; their bezels, the gaps between Office louvres and the walls do.
- **At a glance.** A whole-room dimming of 6–10 % Y′ over 0.1 s is noticeable in the Office and the Tall hall. In a Level 0 room (−6 to −7 % Y′) and a dead-lamp cell (−5 %) it is subtle against film grain (0.22) and the lamps' own ±2–3 % shimmer.
  - Above all, the thing a player looks at to see that the lights dipped, the lamp, does not change outside the Office. So it reads as "the picture pumped", not "the lights sagged".
  - Level 0's first burst is weaker still (−10 % linear against −14 to −15 %): its Failing lamp snapped up to full and added light.
- **Too strong:** none. The largest frame drop is Office stage 2 at −24 % linear (17.8 cd/m² on the brightest 10 % at 200 cd/m² white).
- **Stage 1 against stage 2.** The envelope and depth are the same in both stages. Only the count and rhythm differ: stage 1 has 2–3 dips, stage 2 always 2, and the gaps differ. From the constants (Monte Carlo, `images/10_stage_rhythm.png`):

  | | Start-to-start period |
  |---|---|
  | Stage 1 | 4.0–8.1 s, mean 6.1 s |
  | Stage 2 | 3.5–5.1 s, mean 4.3 s |

  - 70 % of stage-2 periods fall where a 2-dip stage-1 burst could also land, and 15 % of stage-1 bursts look like stage 2.
  - Stage 2 is also short in play: from 18 m to the 12 m calm sight is about 2.3 s at 2.6 m/s. So a player rarely sees more than its entry burst.
  - This capture: stage-1 periods 4.8–7.3 s, stage-2 periods 3.6–5.0 s.
  - **Not learnable from the lamps.** That is by design: v2 keeps the lamps meaning only "near", and the steps (W2, sound chat) carry stage 2. For the C10 playtest, before W2, stage 2 has no separate cue. The playtest form should say so.

## 3. Consistency

`images/10_follow.png` shows one stage-1 burst per room, every reader divided by its own pre-burst value. `Tall_window_g14.png` and the `*_curves.png` images cover the whole sequences.

| Reader | Reads | Through a dip (RF off) | Verdict |
|---|---|---|---|
| Light.intensity | baseIntensity × LampLevel × fade (map) | equal to the formula within 5e-4 absolute in every lamp and frame; −37 to −45 % in the bursts above | PASS |
| Lens `_EmissionColor` (property block) | authored × max(0.04, LampLevel) (map) | within 2e-4 everywhere | PASS |
| **Rendered lens** | the above through ACES and bloom | L0 −1.3 %, Tall −0.8 %, Office −6 to −13 % linear, while the frame dims 14–21 % | **FAIL** (visual side) |
| Zone reflection (`RenderSettings.reflectionIntensity`) | the zone cube and LampModeOf at the player's cell (Dead → DeadLamp) | flat: 0.50 (0.45 Tall) through every dip; never reads LampLevel | FLAG (visual side) |
| Ambient trilight (`FrontRoomsLook.ApplyAmbient`) | constants (the warm bounce) | flat; it is the room's own bounce light, so it should follow | FLAG (visual side) |
| G14 RT glass | each lamp's `Light.intensity` per frame (`BuildLamps`) | window region −9 to −14 % linear (r 0.992 with LampLevel), against −28 to −41 % on the lamp-lit wall. The gap is the flat env cube and the view through the glass to lamps outside the burst, which is correct | PASS |
| EXIT sign | `SetMains` (no caller on the map) | none in the scene; the map never places one | N/A (guard M4) |
| Placard glow (FrontRoomsPhosphor) | the cell's and four neighbours' LampLevel | glow cell Dead (Ls 0), so the charge eases while 2 neighbours dip: G −2 to −6 % per burst RF off, −2 to −4 % RF on, recovering with τ 2 s; no pulse (0.6 s hold, slew 2/s) | PASS |
| Wallpaper print | the scene's lights (lit surface; no lamp term); the walls' phosphor ink is not in main (Q2 paused) | walls (middle third) −7 to −14 % decoded, in step with the frame | PASS |

- **Pops.**
  - At the first dip, every Failing lamp in a burst goes to full in one frame (L0 +0.72, Office +0.49, Tall +0.42; RF on +0.65 / +0.69 / +0.56).
  - At stage 0 it drops back in one frame. Office 275,209 landed in a dropout: 0.98 → 0.02, with its lens 0.82 → 0.35 code, for 0.18 s.
  - Stutter lamps rarely show the switch, because their steady level is the same.
  - So the restore happens on the very frame the stage goes to 0 (L0 27.116 s, Office 25.550 s, Tall 28.166 s). That is a visible all-clear: "the broken lamp is flickering again, so it has left". Q3c keeps RESTORE audio-only: "no clunk, no lamp change".
- **Held Steady as specified:** yes for every live Stutter/Failing lamp, in all 6 lit-room sequences. They go Steady at the first WarnBurst and return at stage 0.
- **Not held:** Tall 282,210 is a Stutter lamp in the 42-cell room set but outside the live set (15.9 m, fade 0.04). Its lens still renders, because the lens emission is not faded by distance. It played its ambient stutter at 5.87–6.43 s and 23.22–23.97 s, during stages 1 and 2: 18 transitions in 0.6 s, 9 flashes/s, lens 206 → 67 code. `images/10_tall_stutter_leak.png`, `.jpg`.
  - It reads as part of the warning, which is exactly what the hold is there to stop ("so no ambient flicker shows through").
- **Left lit or dark after the episode:** none. On the last frame of all 8 sequences, every lamp's level equals its base level, every mode equals its starting mode, and 0 overrides are active.

## 4. Look: 1990 ballasts or a picture pulse?

**Background (not measured here; from general lighting engineering).** If the art direction pursues this further, the research chat should source it (SOURCES.md).

- Most 1990 US offices ran T12 tubes on magnetic ballasts.
- A fluorescent arc follows its supply within about a half cycle (8 ms at 60 Hz), and the phosphor afterglow lasts milliseconds. So during a voltage sag the light falls almost as fast as the voltage, then recovers with the supply. A motor-start sag (elevator, compressor) recovers over tenths of a second, fast at first with a tail.
- Light output is roughly proportional to voltage, so a level of 0.55–0.70 means a deep sag. In a deep sag the weakest tube drops out first; it does not heal.
- The room's bounce light and the troffers' own brightness fall together.

**Against that:**
- **Good:** dips are synchronous (±40 ms), never to black, at most 1.8 Hz, and 2–3 per burst with quiet gaps. It is not a strobe and not a disco effect.
- **Wrong way round:** the source stays white while the walls dim (§2), which is the opposite of a sag. This is the main reason it reads as a post or UI pulse. Fix V1.
- **Too regular:** dips start every 0.55 s exactly, and each one is a symmetric-looking linear V: 0.10 s down, 0.06–0.15 s held, 0.22 s up, with depths drawn independently. That reads as a metronomic breathing pulse. A sag has a near-instant fall, a quick recovery with a tail, and its first hit is the deepest. `images/10_envelope_proposal.png` shows the landed shape, a ballast-sag shape and the RF sag. Fix M3.
- **Failing lamp heals:** at the first dip a Failing tube goes to full and steady, the opposite of a brown-out. Fix M1 (hold it dim).
- **Colour shift and recovery flicker:** not needed. A small colour shift under a sag is real but marginal behind a prismatic lens. Recovery after a sag that does not extinguish the arc is smooth, so do not add a recovery flicker. The ballast character belongs to the WarnBurst groan (sound chat).

## Proposed changes, owner by owner

### 关卡设计 (map; their files, my proposals)

- **M1. The hold without a pop, and without an all-clear.** Fixes item 1 (RF) and item 3 (pops).
  - **Failing:** hold it at Dim (`ModuleLamp.Dim`, 0.42 ± 0.03), not Steady. A failing tube under a sag sits dim.
  - **Stutter:** hold it Steady as now.
  - **Ramp every hold and restore.** Start the hold at the stage 0 → 1 entry, which is 0.3–1.0 s before the first burst, with a ≥ 0.3 s crossfade (≥ 0.5 s with RF) from the lamp's current level. The first dip then starts from a settled lamp.
  - **Restore at a random 2–6 s after stage 0,** with a ≥ 1.0 s crossfade. Never on the stage-0 frame.
  - The cleanest place for this is a per-fixture mode crossfade in `LampTick`: blend the old mode's `Level()` into the new one over T. It also removes pops from every other `SetLampMode` kill or promotion.
  - **Expected:** the one-frame +0.42…+0.72 jumps and the stage-0 drop (Office 0.98 → 0.02) are gone. The Failing lamp's W1 count falls from 2.5–3.0 to ≤ 2 flashes/s, and RF's "one sag, no step" holds.
- **M2. Hold scope.** From the hold to the restore, hold every Stutter and Failing lamp of the room set, not only the live ones, and suppress Dead-mode blinks in the room set.
  - Evidence: Tall 282,210 at 9 flashes/s (lens on screen) inside stage 1 and stage 2; the Placard Dead-lamp blink 0.1 s before burst 2.
  - Separate and older than W1: land §9.5's ambient limiter for mode 1 (2–3 dips on 0.5 s slots, floor 0.5, 60–80 ms ramps). Today's mode-1 stutter is 9 flashes/s wherever it plays.
- **M3. A ballast-sag envelope** (look, constants only):

  | Constant | Now | Proposed |
  |---|---|---|
  | `DipAttack` | 0.10 | 0.05 |
  | `DipHoldMin` / `DipHoldMax` | 0.06 / 0.15 | 0.04 / 0.08 |
  | `DipRelease` | 0.22 | 0.30 |
  | Depth | independent draws | first dip of a burst U(0.55, 0.62), later dips U(0.65, 0.75) |
  | `DipSpacing` | 0.55 | 0.55 (keep: it is the photosafety constant) |

  - A dip then lasts ≤ 0.43 s, still shorter than the 0.55 s spacing, so dips never merge. A stage-1 burst lasts ≤ 1.61 s (spec ≤ 1.6 s).
  - Photosafety is unchanged: still ≤ 2 dip starts/s, each dip still one flash, and RF is untouched.
  - Optional code: an ease-out (exponential-tail) release in `TickLampOverrides` instead of the linear one.
  - Do not deepen the dips. The visual fixes below give the readability without touching the photosafety margins.
- **M4. Guard for C24 (EXIT on each floor).**
  - The true EXIT is LIT-locked and ignores mains; keep it that way.
  - No other EXIT sign may take the Warn dip through `SetMains`. At 0.55 × base ≤ 0.55, a dip crosses the sign's 0.55 `TransferLevel`. That means a 0.2 s black gap and a DC transfer on a red sign at every deep dip: up to 4 transitions per dip.
  - Warn is a troffer-circuit event, and the sign's own spec says "troffer failures never reach it".

### 游戏视觉 (visual; mine)

- **V1. Make the lens dip.** Fixes item 3 (rendered lens), item 2 and item 4.
  - Data hook (one line, 关卡设计): in both `TickFixtures` paths, add `block.SetFloat("_FR_LampDip", level / max(baseLevel, 1e-3))` to the lens's property block. It is 1 whenever no override is active, so idle lamps and the ambient Stutter / Failing / Dim looks are unchanged.
  - Shader (visual): in the troffer lens path of FrontRooms/Surface, multiply the emission by `pow(_FR_LampDip, k)`. Calibrate k (start at 2.5; sweep 1.5–4) on a clone lookdev capture so that the rendered lens's display-linear luminance loses ≥ 0.8 × (1 − dip), i.e. ≥ 36 % at dip 0.55. Bloom follows by itself.
  - Desktop first. WebGL decides separately.
- **V2. Make the room's bounce follow its lamps.**
  - Compute r = Σ wᵢ·LampLevel / Σ wᵢ·LampBaseLevel over the lamps within lightRadius of the camera, with wᵢ = fade / max(d², 4). r is exactly 1 when idle.
  - Scale `RenderSettings.ambientProbe` (the trilight bounce) and the zone reflection intensity by 1 − 0.8·(1 − r).
  - Keep `_FR_ZoneReflNominal` at the steady value, so FrontRooms/Glass and G14 dip with the room. Fog colour by the same factor is optional.
  - Reads LampLevel / LampBaseLevel only, which is the F7 rule. No map edit.
  - Lamps near the Relay never dip, so a view near it keeps r ≈ 1 ("never at the Relay").
- **Targets after V1 + V2,** at the landed depths, RF off, in every lit room type: frame mean ≥ 20 % linear (≥ 9 % Y′) and ≤ 35 % linear; rendered lens ≥ 30 % linear. Dead-lamp cells will stay lower by nature.
  - Re-run the analysis tools on a fresh capture and check:
    - the 10° field concurrent count stays ≤ 2 flashes/s (V1 makes lens blocks transition, and W1's 0.55 s spacing keeps it ≤ 2);
    - RF stays at one sag;
    - G14's window ratio rises toward the wall's.
- **No change:** G14 already follows LampLevel through `Light.intensity`. The placard glow follows its neighbours slowly, with no pulse.

### Others (not mine)

- **Sound chat:**
  - the WarnBurst ballast groan, which roughly doubles the cue's legibility in an L0 room or a dead-lamp cell;
  - W2 steps, the only stage-2 signal.
- **Playtest form:** note that before W2, stage 2 has no separate signal from stage 1.

## Corrections to the capture stage's VL329–VL331 drafts (`W/vl_pending/c10_lamps.md`)

- **VL329:** "lens pixels drop ≤ 9 %; L0 203 → 185" is a window artifact. The 185 is the Failing lamp's lens before the first burst, measured inside the stage-1 window.
  - Steady lit lenses dip by a median 0.9 % (L0), 3.3 % (Office) and 1.0 % (Tall) in code value; 2.1 / 7.2 / 2.3 % linear.
  - "Rooms dim 7–11 %" is the frame-mean Y′ at the deepest trough. The linear medians are −13 to −14 % (L0), −20 to −21 % (Office), −18 to −19 % (Tall) and −9 to −10 % (dead-lamp cell).
- **VL331:** add the per-lamp counts:
  - Failing lamps reach 2.5 (L0) and 3.0 (Office) flashes/s with RF off, because of the snap;
  - with RF on there is one flash per episode (snap plus sag);
  - Tall's room-set Stutter lamp reaches 9 flashes/s on screen.
  - The rest of the row stands: the WCAG-field and screen counts for W1's dips are 0.
