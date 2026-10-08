# 25 · G14 P0 + P1 verify: art direction

2026-10-08 · visual chat (游戏视觉) · workflow glass-rt-track, verify stage (art-direction verifier, retry) · **verdict: PARTIAL. The strength, grade and lamp behaviour are right. The traced image itself has two visible defects (AD-1, AD-2) and one minor softness issue (AD-3). The showcase cases RT exists for have no frames yet (AD-6).**

Inputs:
- `10_rt_glass_design.md` (§0, §1.5–1.7, §4.2);
- `20_implementation.md` (all of it);
- `03_research.md` §5 (real-world expectations; the brief calls it "section 4", which is item 4 of that report);
- the code in `W/proj_rt` (`NativePlugin/FrontRoomsGlassRT.metal`, `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTSystem.cs`, `Assets/Resources/Rendering/FrontRoomsGlass.shader`, `Assets/Resources/Surfaces/Glass_Window.mat`);
- every image in `rt/images/` (§5).

Pixel work used the full-resolution run r8 frames in `W/rt_work/r8_out/` (1920×1080 PNG, the same captures the `20_*` sheets were made from), not the downscaled sheets.

`W` = `/Users/redwang/FrontRoomsVisualWork`.

**Nothing in the project was edited, and Unity was not run.**
- The harness's views are exactly the r8 frames reviewed here.
- The cases still missing (§3, AD-6) need new harness poses, and this stage may not change code.
- The machine was also carrying 15–25 other Unity jobs.

Analysis scripts: `W/rt_work/verify_ad/` (`stats.py`, `dmap.py`, `sheets.py`).

New evidence sheets, made from the r8 frames: `rt/images/25_AD_*.jpg` (JPG q85, ≤ 1600 px).

---

## 0. Short answer (for Red)

1. **The physics and the grade are right.**
   - Face-on in lit rooms the reflection is nearly invisible, as real 8 % glass is: −1.0 to −1.3/255 on average, p99 +4.6 to +7.0.
   - Only the troffer rows show, and they are the brightest reflected shapes.
   - At 65° the opposite wall's wallpaper becomes legible as a faint second pattern.
   - The reflection is graded with the scene: no colour cast.
   - Reflected lamps flicker on the same frame as the real ones.
   - Where RT is darker than the old zone cube, the planar-mirror reference agrees with RT; the cube was the one that was too bright.
2. **The traced image has two defects you will see in exactly the windows RT is for.** Today they hide only because a lit-to-lit pane multiplies them by 8 %. In a dark-beyond window, at grazing angles, or on the Relay they will show:
   - **AD-1 speckle.** The smudge blur scatters carpet-coloured specks into dark reflected objects and dark specks around them: the Relay's body and boots, the test cube, pillar edges.
   - **AD-2 hard shadow wedges.** Straight-edged light wedges appear on the reflected carpet where the real room has soft shadows. They can also pop as you walk.
3. **AD-3 (minor).** Reflected troffers are a little soft: 24–35 % larger and 11–16 % dimmer at the core than a perfect mirror. The cause is that the whole pane is blurred, not only the smudges. This loses the "sharp glass, blurred smudge" contrast that sells a real surface.
4. **What you have not seen yet:**
   - the 45–65° view with a troffer in the reflection;
   - the Relay in a dark window;
   - a moving shot with lamps in the reflection;
   - the thin-glass double image;
   - a smudge over a lamp.

   None of these has a frame (AD-6). The art sign-off for "highest spec" waits for them.
5. **Merge implication** (this verifier's view; the merge stage decides). The clone is clearly better than what main runs today: main traces the wallpaper as red paper (codex F8) and has no fail-closed guard. So these findings do not argue against promoting it. They argue against calling G14 done. AD-1 and AD-2 are small kernel changes (§2).

---

## 1. Verdict per art criterion

| Criterion (brief) | Verdict | Evidence (r8 unless stated) |
|---|---|---|
| Reflection strength vs what is behind the glass | **PASS** | Pane mean ΔY High − Off: v1 −0.99, v2 −1.21, v3 −1.29, v4 −4.20, v5 −0.52 /255; p99 +4.6 / +7.0 / +5.0 / +6.8 / +4.9. Pixels brighter by > 5/255: 0.8–2.7 %. Dark-beyond window: the room appears (lens grid, pillar, floor) at pane mean Y 35.8, against a lit wall at 95.6, i.e. ~11 % linear, which is the two-surface Fresnel at 0–50°. RT/planar-mirror block ratio median 1.03–1.05 (B2). `25_AD_delta_maps.jpg` |
| Troffer lenses the brightest reflected shapes | **PASS** (soft: AD-3) | v1/v2 reflected lenses: final Y mean 147 / 141, p90 180 / 170, on a ceiling background of 60 / 57. Real lenses through the same glass: 203 / 201. `25_AD_lens_edges.jpg` |
| Wallpaper legible but not mirror-sharp | **PASS** | Face-on: invisible (correct for lit-to-lit). 50°/65°: the chevrons read as a faint second pattern (±10/255, `25_AD_delta_maps.jpg` v4). Dark-beyond 50°: legible but dim (pane Y 44.3; it reads clearly only with a ×3 stretch; AD-4) |
| Double image subtle | **NOT VERIFIABLE** | The back-surface image is on (High and Ultra), but no frame has a bright edge within ~1 m at an oblique angle. At face-on the offset is 0, and at 50°/65° only wallpaper is reflected. P1-B5 is unmeasured (AD-6) |
| Grime breaking the reflection | **FAIL (mechanism)** | Smudges and the hand-height smear band do break the reflection, but as speckle, not as a soft smear (AD-1). The edge, crack and roll-wave hooks behave (roll × 0.075 removes the cube's wavy bands). No B7 frame of a smudge over a lens (AD-6) |
| No noise / flicker in the moving sequence | **PASS where tested, OPEN where it matters** | Strafe at 1 cm/frame (v3, wallpaper and floor only): relative crawl 4.4 %, against 8.7 % for the 4× MSAA raster. Still-camera accumulation start/stop: frame 0 vs 33 differ by 0.51/255 in the pane, the same as the 0.48 grain floor outside it, so there is no pop. Lamp flicker: same-frame r 1.000. **Untested:** lens edges, the Relay silhouette and reflected-floor shadow pops in motion. These are where AD-1's screen-fixed noise and AD-2's budget pops live (AD-6) |
| No colour mismatch with the scene grade | **PASS** | The reflection enters before tonemapping. Pane mean RGB v2 Off 104.5/96.5/56.8 → High 103.0/95.2/57.0 (slightly cooler, ≤ 1.5/255 per channel); Office v2 ±0.2/255. The beige of the traced-radiance panels is only the sheet encoding (×2, Reinhard, ungraded; AD-8) |
| (extra) Realism cues that work | **PASS** | (a) The window stop is reflected along the pane's edges, a darker inner rim of 6–14 % of the pane (v4 65°: −17/255 mean, p5 −36), as real glass shows a doubled frame at grazing angles. (b) The Relay hides four reflected troffers when it stands behind you. (c) A dead lamp leaves the reflection on the same frame |

---

## 2. Issues (most severe first)

### AD-1 · major · the smudge blur bleeds across reflected silhouettes as speckle

**Frames:** `25_AD_speckle_bleed.jpg` (the zooms below); `20_P0_relay_moving.jpg` top-right "traced radiance"; `20_P0_orientation.jpg` middle panel; `20_P1_v2_headon_centre.jpg` traced-radiance panels.

**What is wrong:**
- **Relay.** On the Relay's lower body and both boots, light carpet-coloured specks are scattered inside the dark surfaces, and a dithered dark halo surrounds the left boot. Raw frame: `P1_relay_behind_rt.png` x 900–1040, y 330–560.
- **Test cube.** The green test cube has a hairy fringe, with stray green specks up to ~12 px from its edge and carpet specks along its bottom. The planar mirror shows a crisp 1 px edge. Raw frame: `P0_orientation_rt_raw.png` x 734–874, y 325–449.
- **Pillar and far wall.** The v2 pillar edge and the far wall's base show the same dithered fringe (`v2_headon_centre_rt_high.png` x 700–900, y 330–470).
- **Where it shows.** The smear band sits at 0.8–1.65 m above the floor, which is where things at eye height behind the player reflect. So the Relay's body will always be seen through the worst of it. Today the 8 % Fresnel of a lit-to-lit window hides it. In a dark-beyond window, at 60°+, or once Red asks for the Relay in reflections, it shows.

**Why:**
- `fr_resolve` smudge blur (`FrontRoomsGlassRT.metal:784–805`) takes 12 taps plus the centre over a radius of up to 24 px at 1440p (18 px at 1080p). Each pixel uses its own random rotation (`fract(52.98…)` hash of the pixel).
- Taps are accepted only by the pane's depth tag, never by hit distance. So the bright floor 6 m behind the Relay is averaged into the Relay 3.5 m away, and the reverse.
- With 13 samples the result is noise, not a blur.
- The pattern is fixed in screen space, so in motion it crawls along every high-contrast reflected edge.

**Fix (visual / glass-rt-track, kernel only):**
1. Make the blur depth-aware on the reflected path. Accept a tap only if `|hitDist_tap − hitDist_centre| ≤ 0.15·hitDist_centre + 0.05 m` (`aux.x` already holds it).
2. Replace the 12-tap rotated disc with a smooth, deterministic filter:
   - either a two-pass separable bilateral Gaussian inside the tag, with radius from the existing formula;
   - or a small mip pyramid of the RT radiance sampled at the radius (the SSR-style roughness mip).

   Either gives a smooth smear with no screen-fixed noise.
3. **Acceptance:**
   - in `P1_relay_behind_rt` and `P0_orientation_rt_raw`, 0 isolated specks inside the dark body or cube (a speck = a pixel more than 3 local MADs from both 4-neighbours) and none more than radius + 1 px outside an edge;
   - on the strafe of AD-6(3), lens-edge crawl ≤ the raster's.

### AD-2 · major · hard-edged light wedges on the reflected carpet

**Frames:**
- `25_AD_carpet_wedges.jpg`;
- `20_P1_office_v5_close55.jpg`, traced radiance: a large lighter parallelogram across the reflected corridor floor with straight diagonal edges, while the planar mirror shows a soft shadow band;
- `20_P1_v5_close55.jpg` and `20_P1_v2_headon_centre.jpg`, traced radiance: a diagonal step and a lighter wedge on the carpet (raw `v2_headon_centre_rt_high.png` around x 1010–1130, y 485–550);
- `20_P1_parity.jpg`, ×8 difference: a fan of straight-edged wedges on the floor to the right of the pillar, raw `P1_parity_rt.png` x 450–1150, y 700–1080. RT − raster on that floor: mean +2.1/255, p95 +6.9.

**What is wrong:** the reflected room has hard, straight-edged light and shadow shapes on the carpet that the real room (soft shadow maps) does not have. In a mirror-like (dark-beyond) window the reflected floor would not match the floor you just walked on.

**Why:** two causes.
- **Hard shadows.** Shadow rays go to the lamp's point position. The raster's shadows are soft PCF shadow maps, so every wall corner casts a razor-sharp wedge in RT.
- **Budget leaks.** `FRShade` (`FrontRoomsGlassRT.metal:402–446`) spends High's 4-ray budget in list order. The list is sorted by distance to the **camera** (`FrontRoomsGlassRTSystem.cs:1686`), not to the hit. A reflected floor point several metres away therefore gets no shadow test for the shadowed lamp above it, which then shines through walls, unshadowed. The leak's edge is the wall's straight shadow line.
- **Measured:** Ultra (up to 16 rays) brings the carpet per-pixel error from 4.2 % to 3.0 % but still shows the wedges (both panels of `20_P1_v5_close55.jpg`), so the hard-shadow part remains.
- **In motion:** the budget assignment changes as the camera-distance order changes, so the leaks can switch on and off (a pop). This is inferred from the code and untested (AD-6(3)).

**Fix (kernel):**
1. Pick the budgeted lamps per hit by contribution: keep the top N shadowed contributions while looping, then trace them. Do not give a lamp the full unshadowed light just because the budget ran out. Use a neutral factor instead (e.g. `1 − 0.5·shadowStrength`), which never creates a lit edge.
2. Soften the shadow rays to match the raster's penumbra:
   - aim at a stratified point on the troffer's emitting rectangle (0.6 × 0.6 m lens, or the size that matches URP's soft-shadow filter at that range);
   - High: 2 points, Ultra: 4, in a fixed pattern;
   - the depth-aware blur of AD-1 then smooths the rest.
3. **Acceptance:**
   - parity carpet mean ≤ 3 % and p95 ≤ 8 % at High (today 4.2 % / 13.1 %);
   - no straight-edged step > 3 % luminance on the reflected carpet in office v5;
   - on a 4 cm/frame walk, reflected-floor frame-to-frame change ≤ the raster floor's.

**Cost:** the §5.3 ablation already shows lamps and shadows dominate the break shot (D6). Spend the budget on soft, contribution-ranked rays rather than on more lamps or Ultra's extra AA rays (AD-7).

### AD-3 · minor · troffer reflections soft and fringed; the clean pane is blurred everywhere

**Frames:** `25_AD_lens_edges.jpg`; `20_P1_v2_headon_centre.jpg` and `20_P1_v1_headon_level.jpg` (top of the pane).

**What is wrong (raw v1/v2, the RT radiance decoded against the planar mirror radiance):**
- reflected lens footprint (radiance > 0.8): 2,619 vs 2,107 px (+24 %) and 3,729 vs 2,763 px (+35 %);
- core (radiance > 3) mean 5.4 vs 6.0 and 5.1 vs 6.1 (−11 %, −16 %); peak 8.8 / 7.8 vs 9.2;
- total lens energy is the same (+1 %, +3 %).

In the final frame each reflected troffer has a ~4–5 px soft edge with a faint dithered rim. The real troffers seen through the same glass have crisp 1 px edges.

**Why:**
- The blur radius comes from the prepass smoothness. The pane's own 0.96, lowered by the dust film everywhere (`smooth = lerp(smooth, 0.45, dust·0.6)`, dust 0.04–0.14 mid-pane), gives 1.5–3 px for hits 5 m+ away.
- Real float glass with light dust keeps a sharp specular image and adds a faint veiling haze. It does not blur geometrically.
- Blurring the whole pane removes the "sharp glass, blurred smudge" contrast that `03_research.md` §5.3 names as the strongest real-surface cue.

**Fix:**
- Apply the resolve blur only where the prepass smoothness is below ~0.85 (smudges, prints, the bottom dust band), or zero any radius below 1.5 px.
- Let the shader's existing scatter haze carry the light dust film.
- **Acceptance:** reflected lens footprint within +10 % of the planar mirror and core within 5 %, measured on v1/v2.
- The "not mirror-sharp" wallpaper look stays: it comes from the 8 % contrast and the smudges, not from blurring clean glass.

### AD-4 · minor (glass track, shows more with RT) · dark-beyond windows: the lit grime band competes with the reflection

**Frames:** `25_AD_dark_and_relay.jpg` top row; `20_P1_dark_beyond_v2_headon_centre.jpg` and `20_P1_dark_beyond_v3_angle50.jpg`, the middle third of the pane (raw y ≈ 330–620).

**What is wrong:**
- Off and High both carry a band of light mottled specks across the pane at hand height. It is about as bright as the reflected room, so the one-way-mirror read becomes "a dim mirror seen through a flecked haze".
- At 50° the reflected wallpaper reads only with a ×3 stretch: pane Y 44.3, std 7.9, against a wall at ~96.
- The reflection level itself is physically right (§1).

**Why:**
- The glass's lit haze `s.emission = scatter·_DustColor·0.5·(SampleSH(n) + SampleSH(−n))` lights the far side with the zone's ambient probe even when the room beyond is dark.
- The fine specks (`gB.b · speckNear`) are still at ~40 % at 1.5 m. G10 warned that fine bright specks read as marks on the floor behind the pane.

**Fix (glass track, `FrontRooms/Glass`):**
- Light the far-side half of the haze from the room behind the pane, not the zone probe. A per-pane "beyond dark" value from the room data would do; it is a map-chat contract if the map has to supply it.
- Cut speck brightness in dark-beyond panes.
- Route it through the visual chat; it is a glass-look change, not an RT one.

### AD-5 · decision for Red · the Relay is effectively invisible in lit-to-lit windows

**Frames:** `25_AD_dark_and_relay.jpg` bottom row; `20_P0_relay_moving.jpg`.

**Measured:** on the 26,173 px where the traced reflection shows the Relay's dark body, the final frame changes by −1.8/255 on average (median −0.7, p5 −6.4). The only readable cues are the four reflected troffers it hides and a faint pale disc where its head is.

**Is it right?** It is physically right (8 % F0 against a bright room beyond; `03_research.md` §5.2), and real glass behaves the same.

**Options, not defects:**
- stage Relay-behind-you moments at dark-beyond windows (map / level design), where the pane is a mirror;
- or accept the troffer-occlusion cue as the subtle tell.

Do not raise the Fresnel: it would turn every lit window into a mirror.

The same frames show the D2 question: in a dark window the room appears but the player has no body. That is the most noticeable "this is a game" cue once a window is a mirror. A reflection-only body (destruction plan option b) or a deliberate "you have no reflection" beat is Red's call.

### AD-6 · coverage gap (blocks the "highest spec" sign-off) · the RT showcase cases have no frames

The art review can only judge what was captured. Missing, each needing a harness pose (code; this verifier may not add it):
1. **Oblique 45° and 60° with troffers in the reflection.** v3/v4 reflect only the opposite wall and floor. This is the case `03_research.md` §5.4 names first, and the one where RT clearly beats the cube.
2. **The Relay crossing 2 m behind in the dark-beyond window**, Off / High / Ultra, plus a 30-frame walk of it. This is where AD-1 and AD-5 decide the look.
3. **A moving shot with lamps in the reflection:** v2, 60 frames at 2 cm/frame, every frame saved, with crawl measured on the lens edges and the reflected floor. Today's strafe (v3) has no lens and no shadow edge in its reflection.
4. **P1-B5 back-surface image:** a lens edge at 0.5 m at 30–45°, ×4 zoom; expected offset 2–4 px at 1440p, weight 0.45–0.50.
5. **P1-B7:** the smear band over a reflected troffer row (it must blur the live lens, not swap to the cube).
6. **The HDR-off display frame and Office parity** (drywall, carpet tile, door veneer, cove base, kit props), all named in design §4.2.

### AD-7 · note (input to D1) · Ultra looks the same as High

- High vs Ultra in all five views: |Δ| mean 0.48–0.57/255, p99 1.9–2.0/255. The Ultra frames in every `20_P1_*` sheet cannot be told apart from High.
- Ultra costs +0.6 to +1.6 ms at 1080p (`20_implementation.md` §5.1) and does not remove AD-2's wedges.
- **Recommendation:** High as the default everywhere. Spend the Ultra budget on AD-2's soft, contribution-ranked shadow rays instead of the extra AA rays and the 16-lamp shadow list.

### AD-8 · docs · how the sheets present the result

These do not change the verdicts, but a reader comparing sheets can misjudge colour or alignment.
- **Traced radiance panels.** They are ungraded, ×2 exposure, Reinhard. They look beige and pink beside the graded scene. Label them "ungraded" in `Tools/rt/compose_rt_sheets.py`.
- **Planar mirror reference panels.** The mirror camera also renders the window frame posts that sit in front of the mirror plane, and the void outside the level (black bottom strip in v2/v5). These are reference artefacts, not RT errors. They probably cost v1 some of its B2 blocks (89.1 %).

---

## 3. What to run next (for the implement stage that fixes AD-1 to AD-3)

1. Kernel: AD-1 (depth-aware smooth blur), AD-2 (per-hit contribution ranking + area shadow rays), AD-3 (no blur on clean glass).
2. Harness poses AD-6(1)–(5). Then re-run views, relay, dark, temporal, parity.
3. Art re-check on:
   - the new oblique-lens and dark-Relay frames;
   - the moving lens sequence;
   - office v5 (wedges gone);
   - the Relay radiance (no specks);
   - v1/v2 lens footprint (+10 % / 5 % bars).
4. Glass track: AD-4 (far-side haze, speck brightness in dark-beyond panes), through the visual chat.

---

## 4. Verification log

This stage did not edit `Documentation/VERIFICATION_LOG.md` or Figma: this verifier may write only this report and its images. For the stage that owns the log:
- **VL184** "Dark room and Office look" (`2847:6191`): change PENDING to **PARTIAL**. The dark-beyond pane becomes a readable but dim mirror under the grime band (AD-4). The Office windows are correctly near-invisible. The Relay is hidden in lit-to-lit windows (AD-5).
- **New row (proposed):** check "RT glass art direction", task G14, verdict **PARTIAL**, question "Does traced glass read as real interior window glass?". Images in slot order: `rt/images/25_AD_speckle_bleed.jpg` (large), `25_AD_carpet_wedges.jpg`, `25_AD_lens_edges.jpg`, `25_AD_dark_and_relay.jpg`, `25_AD_delta_maps.jpg`. All 2026-10-08 00:41, JPG q85, 1213–1600 px wide.

---

## 5. Frames reviewed

- **Judged:**
  - views: `20_P1_v1_headon_level`, `v2_headon_centre`, `v3_angle50`, `v4_steep65`, `v5_close55`;
  - zones: `20_P1_office_v2_headon_centre`, `office_v5_close55`, `dark_beyond_v2_headon_centre`, `dark_beyond_v3_angle50`;
  - checks: `20_P0_relay_moving`, `occluder`, `broken_pane`, `stream_test`, `orientation`, `20_P1_parity`, `temporal_still`, `edge_crawl`;
  - charts: `lamp_sync_chart`, `walk_chart`, `frame_delta_chart`, `cost_chart`, `cost_p10_chart`;
  - plus their raw r8 PNGs (`W/rt_work/r8_out/`, 72 frames).
- **P0 lifetime frames:** occluder (the panel is clean), broken pane (the reflection leaves on the break frame) and destroyed mesh (no ghost) look right. Orientation is correct; its cube fringe is AD-1.
- **Seen, not judged:** they show ChatGPT's prototype or the secondary-reflection benches, not this build:
  - `01_review_trace_output.jpg`;
  - every `02_*.jpg` (prototype: flat white shading, blue sky);
  - `11_*`, `11b_*`, `11r1_*`, `11r2_*`, `11r3_*` (P1b glossy-floor and two-bounce benches; their art review belongs to `11_secondary_reflections.md`).
