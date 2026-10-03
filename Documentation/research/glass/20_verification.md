# 20 — G10 in-engine verification: glass and zone reflections, BEFORE vs AFTER

Status: **run 3 (fix pass), 2026-10-03 about 12:05.** This is not a sign-off. Red has to judge the frames listed in
§2. Three capture runs were made on the private clone `proj_glass`:

- run 1 at 00:23;
- run 2 at 10:04, kept in the clone as `Verification/glass_g10_run2`;
- run 3 at 12:03, kept as `Verification/glass_g10_run3`. This report is about run 3.

Run 2's sheets (`images/g10_0*.jpg`, `g10_full_*`) and logs (`logs/g10_*.txt`) are kept for comparison. Run 3's are
`images/g10r3_*` and `logs/g10r3_*`. Nothing in `Frontrooms3D` changed except this folder. The real project was
not opened in Unity.

Tags: **UNVERIFIED** = not confirmed by a run, the code, or a page I read. "/255" = mean absolute difference of 8-bit
sRGB output after post.

---

## 0. Short answer (run 3)

1. **The veil is still gone.** BEFORE changes the view behind the pane by 27–41 /255. AFTER changes it by 2.7–4.2 /255
   (§3.1).
2. **The pane now reflects with physical energy.** Changes:
   - two-surface reflectance (`_PaneF0` .08, specular setup);
   - alpha tied to reflectance (.11 + .89·F⁵);
   - the reflection floor at the full captured light (`_ReflectionMin` 1.0).

   Face-on, the shader's reflectance is 0.080 against 0.082 for a real 6 mm pane. At 60° it is 0.137 against 0.157
   (§3.3). Run 2 reflected 0.024 face-on, a third of physical.
3. **What that does on screen:**
   - **At 1.5 m face-on**, the pane is now a faint, slightly lighter sheet:
     - mean |ΔRGB| vs no pane is 4.1 (Level 0) and 3.6 (Office), against 3.0 and 2.5 in run 2 with the same
       ambient;
     - contrast kept 0.85 / 0.82, against 0.89 / 0.87;
     - the ceiling lamps behind the camera show in its top edge.
   - **At 50° and 61°**, the ×6 difference images show the reflected wallpaper chevrons across the pane, and a lamp
     glint at the head of the frame ([g10r3_05](images/g10r3_05_diagnostics_L0.jpg)).
   - **Under a dead lamp (38)**, a lit troffer from the next cell reflects in the upper pane
     ([g10r3_06](images/g10r3_06_deadlamp_windows.jpg)).
   - **Honest limit:** in these evenly lit rooms a correct pane is still a quiet object face-on. Real interior glass
     between two equally lit rooms is too; the frame and stop are what tell the eye there is glass. The glazing stop
     (map side, W1.4) is still missing in every frame here.
4. **The hutch and display cabinet regression is fixed.** In run 2 the hutch's arched glass doors read as solid dark
   wood and the cabinet's glass shelves vanished. In run 3 both read as glass again
   ([g10r3_07](images/g10r3_07_props.jpg), last two rows). The causes were the 8 % room reflection plus a light dust
   film on case glass (`_DustFilm` .12, `_Scatter` .5).
5. **The grime no longer reads as floor debris or mould.**
   - Fine specks fade out between 0.8 and 2 m.
   - The rim band over the jamb is cut to a third (rim .35 → .12 over 6 cm, not 2 cm; corner .6 → .3), so the
     mottled dark band on the jamb at 0.7 m is gone.
   - Smears are wider.
   - The two faces carry different grime.
   - `_Scatter` 3 → 1.
   - The new pane changes fine structure *less* than the old values would (high-pass |Δ| 0.89 vs 0.95 at 1.5 m,
     variant VP in §4). Its extra visibility comes from the reflection, not from speckle.
6. **`ApplyAmbient()` now changes nothing on screen.** Main's F5 constants equal the scene's. B (scene ambient) vs C
   (after `ApplyAmbient`) differ only by the noise floor in every view (for example, 0.40 vs 0.43 /255 at Level 0
   1.5 m; §5). Run 2's "biggest visible change, Red must sign it off" came from the clone's old, brighter constants.
   It no longer applies. The merged `FrontRoomsLook.cs` keeps main's values (`10_implementation.md` §2).
7. **Walls and floors:**
   - The zone cube now runs at 0.5 linear (2.3× run 2's 0.214, because the slider is gamma; §5).
   - Frames 01/34/35/37 still change by only 0.75–1.46 /255 BEFORE → AFTER (noise 0.37–1.12).
   - No glossy floors glow, and the frozen wallpaper print is not visible on walls or floors.
8. **Shards** (new STAGED view; [g10r3_08](images/g10r3_08_shards.jpg)):
   - The audit's near-black `Glass_Shard` read as black chips: 97.6 % of shard pixels on Level 0 carpet, 85 % on Office
     carpet, were darker than 0.6 × the carpet.
   - The new opaque base (0.85 × Level 0 carpet) fixes Level 0 (ratio 0.91). It reads as pale yellow chips on Office
     carpet (ratio 1.27).
   - The transparent `FrontRooms/Glass` variant (new `Glass_ShardClear`) reads as glass on both floors (0.96–0.98).
9. **The G14 hook exists and is proven.**
   - `_FR_GlassRTReflection` / `_FR_GlassRTWeight` behind the global keyword `_FR_GLASS_RT`.
   - With the keyword off, every float of every test view equals the shader without the hook. 175 checks, 0 failed,
     run twice: against today's pre-fix shader, then against the final shader.
   - A plain uniform branch (the critic's first proposal) was **not** bit-identical: 49 of 90 checks failed by up to
     9.5e-7 (§8.2).

**Not done in this pass:**
- a built player;
- a WebGL build (the variant count; RGB9e5 in Chrome/Safari);
- the title-stream rooms in the capture;
- D3D12/Vulkan;
- frame time on a quiet machine.

---

## 1. Method

- **Harness:** `g10_harness/FrontRoomsGlassG10Capture.cs.txt`. It is a copy of the clone's
  `Assets/Editor/Audit/FrontRoomsGlassG10Capture.cs` (1911 lines), built from the audit harness
  (`../interaction_audit/05_in_engine_evidence.md` §1).
  - Same scene `Assets/Scenes/FrontRooms3D.unity`.
  - Seed `Random.InitState(4242)` before Space, giving run seed 516574485 and map root (−576, 0, −576).
  - Same title handoff, same camera (1920×1080 sRGB, 4× MSAA, post on), same target search.
  - The camera positions for 01, 04, 23, 34, 35 and 37 match the audit's `frames.txt`.
- **States per view.** Each view is one frozen moment (`Time.timeScale` 0), so lamp flicker matches between states.
  - **A BEFORE.** What the game draws today:
    - the map's runtime `TransparentGlass` 30 mm cube with shadows on;
    - the scene's ambient;
    - **the scene's own default reflection**: Skybox, slider 0.3, decode (2.526, 2.2, 0, 1), as logged in run 2;
    - copies of the shipped URP/Lit `Prop_Glass` / `Prop_BottleBlue`.

    Run 3 had to force the reflection back: the clone's RoomStream title hook (`FrontRoomsRoomStream.cs:351-352`)
    already puts the Level 0 cube in at title start. The first run-3 attempt missed this and its BEFORE was
    contaminated. It was discarded; see `logs/g10r3_log.txt` lines 15–16 for the restore.
  - **B AFTER.** Every built pane gets `Glass_Window`, is thinned to 6 mm and has shadows off. The zone cube is set
    through `FrontRoomsZoneReflection.SetImmediate`. The props get the new materials. The scene's ambient is kept.
  - **C AFTER + `ApplyAmbient()`.** Now equal to B within noise (§5). It is kept as a check.
  - **N no pane.** Window views only: the pane is hidden. It is rendered **in the same frame** as the state it is
    compared with, so frame-varying post noise (film grain) cancels.
- **Metrics inside the pane's screen quad** (inset 6 %), each state against N:
  - mean ΔY (Rec.709 on sRGB bytes);
  - ΔY std;
  - contrast kept σ(with)/σ(without);
  - mean |ΔRGB|;
  - **new in run 3:** HP|Δ| = mean |highpass(with) − highpass(without)| of luminance (highpass = Y minus its 9×9 box
    mean);
  - **new in run 3:** 1−SSIM = luminance SSIM with a 7×7 window, 8-bit constants (`Structure()`).

  Mean ΔY cancels by construction when the reflected room and the room behind are equally bright, so the run-2
  numbers alone could not show a reflection.
- **Diagnostics.** Five single-knob variants of `Glass_Window` on a temporary copy, each with its own same-frame
  no-pane reference. In run 2 the variants were compared with a no-pane frame from another frame, so grain inflated
  their std. That is fixed.
- **New STAGED view:** 14 procedural shards (3–7 cm, 6 mm thick) in a 0.5 m patch at 1 m and 2 m, on Level 0 carpet
  and on Office carpet. The mask comes from the same shards drawn unlit magenta.
- **`FrontRoomsMapWorld.cs` was not edited.** Panes are swapped at runtime and swapped back.
- **Run 2 vs run 3 comparability:**
  - BEFORE frames of the same view differ by 0.76–1.86 /255 between the runs (grain + JPEG + lamp flicker phase);
    see the sheet script's printout.
  - The comparison sheets use run 2's B frames (main's ambient) as "previous AFTER".

## 2. Images (JPEG q85, in `images/`)

| File | What |
|---|---|
| [g10r3_01_representative.jpg](images/g10r3_01_representative.jpg) | Audit frames 01, 34, 35, 37: BEFORE \| previous AFTER (run 2) \| NEW AFTER |
| [g10r3_02_window_L0.jpg](images/g10r3_02_window_L0.jpg) | Level 0 window (282,206): 1.5 m, ~50° (audit 04), 0.7 m, 61°: BEFORE \| previous \| NEW \| no pane |
| [g10r3_03_window_Office.jpg](images/g10r3_03_window_Office.jpg) | Office window (281,206): same four views (audit 23 at ~50°) |
| [g10r3_04_window_crops_1to1.jpg](images/g10r3_04_window_crops_1to1.jpg) | 1:1 crops at the pane centre, six views |
| [g10r3_05_diagnostics_L0.jpg](images/g10r3_05_diagnostics_L0.jpg) | Level 0 1.5 m / 50° / 61°: no pane, NEW, ×6 difference, previous values, `_PaneF0` .04, `_ReflectionMin` 0, `_Scatter` 0, grime off |
| [g10r3_05b_diagnostics_Office_deadlamp.jpg](images/g10r3_05b_diagnostics_Office_deadlamp.jpg) | The same for Office 50° and the two dead-lamp windows |
| [g10r3_06_deadlamp_windows.jpg](images/g10r3_06_deadlamp_windows.jpg) | Window (304,207)↔(305,207) from under a dead lamp (38) and from the lit side (39) |
| [g10r3_07_props.jpg](images/g10r3_07_props.jpg) | Bottle, vending front at 22° and 53°, STAGED desk glass, STAGED hutch + cabinet: BEFORE \| previous \| NEW |
| [g10r3_08_shards.jpg](images/g10r3_08_shards.jpg) | STAGED shards, Level 0 and Office carpet, 1 m and 2 m: none \| previous `Glass_Shard` \| new `Glass_Shard` \| `Glass_ShardClear` |
| [g14_hook_proof.jpg](images/g14_hook_proof.jpg) | Hook proof, final shader: baseline, weight 1 with RT = (2,0,0,1), depth-tagged |
| `g10r3_full_*` | Full-resolution NEW AFTER frames: Level 0 window at 1.5 m / 0.7 m / 50° / 61°, Office 50°, 39, hutch, 01, and shards S1 at 1 m on both carpets |

Every state and variant frame (A/B/C/N, `D_diff_x6`, `V*`, shard variants) is in the clone at
`proj_glass/Verification/glass_g10_run3/`.

---

## 3. Windows

### 3.1 Numbers (pane quad vs the same view with no pane, same frame)

Run 2 B = previous materials with main's ambient. Correction: run 2's report quoted state C, whose ambient was the
clone's old, brighter constants. Those numbers (+2.7 / +1.0 / +2.8 ΔY at Level 0) were about 2× too high, because the
grime scatter scales with the ambient SH.

| View | BEFORE \|ΔRGB\| / ΔY | run 2 B: ΔY / std / contrast / \|ΔRGB\| | NEW: ΔY / std / contrast / \|ΔRGB\| | NEW HP\|Δ\| / 1−SSIM (BEFORE) |
|---|---|---|---|---|
| Level 0, 1.5 m | 35.7 / +34.0 | +1.33 / 4.22 / .893 / 2.95 | +3.21 / 5.66 / .853 / **4.09** | .89 / .022 (1.61 / .133) |
| Level 0, ~50° (audit 04) | 28.7 / +24.7 | −0.40 / 3.23 / .920 / 2.19 | −0.19 / 4.26 / .908 / **2.70** | .98 / .014 (2.38 / .095) |
| Level 0, 0.7 m | 40.9 / +39.7 | +1.20 / 3.77 / .894 / 2.50 | +2.45 / 4.60 / .863 / **3.45** | .92 / .020 (1.79 / .148) |
| Level 0, 61° | 27.4 / +24.1 | +0.14 / 3.69 / .939 / 2.41 | +0.26 / 5.08 / .921 / **3.24** | 1.10 / .016 (2.43 / .091) |
| Office, 1.5 m | 34.5 / +33.1 | +0.40 / 3.35 / .874 / 2.45 | +2.36 / 4.22 / .821 / **3.64** | .96 / .014 (2.30 / .131) |
| Office, ~50° (audit 23) | 29.1 / +28.1 | −0.12 / 4.02 / .892 / 2.82 | +0.93 / 5.28 / .850 / **3.70** | 1.06 / .016 (2.65 / .117) |
| Office, 0.7 m | 33.4 / +32.8 | +0.71 / 3.78 / .889 / 2.80 | +2.19 / 4.81 / .843 / **4.23** | .92 / .016 (1.87 / .125) |
| Office, 61° | 30.1 / +29.1 | +0.64 / 4.63 / .916 / 3.02 | +1.48 / 6.39 / .897 / **4.24** | 1.05 / .021 (2.28 / .119) |
| Under a dead lamp, 1.5 m (38) | 10.4 / +4.7 | −1.89 / 3.75 / .920 / 3.01 | −1.41 / **8.32** / .917 / 4.15 | .85 / .017 (1.64 / .035) |
| Under a dead lamp, ~50° (38) | 9.8 / +3.7 | −2.87 / 2.58 / .943 / 3.26 | −4.82 / 2.75 / .932 / 4.80 | .79 / .007 (1.88 / .036) |
| Lit side, dead lamp beyond, 1.5 m (39) | 29.7 / +27.0 | +0.41 / 2.99 / .895 / 2.25 | +1.80 / 4.16 / .864 / **3.19** | 1.04 / .017 (2.20 / .100) |

Source: `logs/g10r3_metrics.txt` (lines "A BEFORE", "B AFTER") and `logs/g10_metrics.txt` ("B AFTER-gl").

### 3.2 What reads as glass now

- **The view through the pane is still correct.** Level 0 behind an Office window is yellow, not cyan. The far
  troffers keep their contrast.
- **Lamp reflections.** The ceiling lamps behind the camera show along the top of the pane at 1.5 m. At 50° a lamp
  glint sits at the head of the frame. Under the dead lamp (38) a lit troffer from the next cell reflects clearly in
  the upper pane, which is why ΔY std jumps from 3.75 to 8.32.
- **The reflected room at an angle.** In the ×6 difference images at 50° and 61°, the near wall's chevron print
  covers the whole pane ([g10r3_05](images/g10r3_05_diagnostics_L0.jpg), third column). In the frame itself it is a
  soft sheen, because the 256 px cube is convolved and the reflection is 10–14 % of the light.
- **A lighter sheet face-on.** About +2–3 ΔY. The pane keeps 82–86 % of the contrast behind it, against 87–89 % in
  run 2.
- **Grime.** At 0.7 m there is a dust strip above the sill and soft smears at hand height. Specks show only up close.
  The run-2 mottled band over the dark jamb (it read as mould) is gone ([g10r3_02](images/g10r3_02_window_L0.jpg),
  0.7 m row, right edge).

### 3.3 Why it is still quiet face-on (physics, not a bug)

Two-surface Fresnel for n = 1.52 (incoherent sum). My arithmetic: R_pane = 0.082 face-on, 0.109 at 50°, 0.157 at 60°,
0.385 at 75°, transmission the rest.

| Angle | physical R_pane / T | NEW shader R / T | run 2 shader R / T |
|---|---|---|---|
| 0° | .082 / .918 | .080 / .890 | .024 / .920 |
| 50° | .109 / .891 | .095 / .885 | .033 / .917 |
| 60° | .157 / .843 | .137 / .862 | .060 / .903 |
| 75° | .385 / .615 | .358 / .691 | .198 / .797 |

Shader R = URP's `EnvironmentBRDFSpecular` with F0 .08, smoothness .96 and the floor at 1.0. T = 1 − alpha
(.11 + .89·F⁵, which includes about 3 % absorption).

So run 2 removed 8 % of the view behind and added back only 2.4 %. It acted as a grey filter. The new pane removes
11 % and adds back 8 % of an equally bright room, so the face-on change is small by nature. **This corrects run 2's
§0.3 and §9.1** ("cube reflections cannot make this pane visible; do not tune the reflection numbers"). Those were
conclusions drawn from a pane reflecting a third of the physical light. The reflection is now the largest single
term (§4).

## 4. Which knob does what (run 3; each variant against its own same-frame no-pane reference)

ΔY / mean |ΔRGB| inside the pane quad:

| View | NEW AFTER | VP previous values | VF1 `_PaneF0` .04 | VR0 `_ReflectionMin` 0 | VS0 `_Scatter` 0 | VG0 grime off |
|---|---|---|---|---|---|---|
| Level 0, 1.5 m | +3.21 / 4.09 | +1.02 / 2.94 | −0.18 / 3.50 | −0.17 / 3.62 | +2.45 / 3.77 | +2.08 / 3.18 |
| Level 0, ~50° | −0.19 / 2.70 | −0.55 / 2.35 | −2.26 / 3.13 | −2.30 / 3.32 | −0.86 / 2.71 | −0.99 / 2.40 |
| Level 0, 0.7 m | +2.45 / 3.45 | +1.17 / 2.59 | −0.17 / 2.89 | −0.40 / 3.00 | +1.65 / 3.12 | +0.89 / 2.55 |
| Level 0, 61° | +0.26 / 3.24 | +0.01 / 2.50 | −1.23 / 3.27 | −2.22 / 3.28 | −0.37 / 3.22 | −0.76 / 2.84 |
| Office, 1.5 m | +2.36 / 3.64 | +0.13 / 2.48 | −1.16 / 3.01 | −1.24 / 3.04 | +1.64 / 3.31 | +1.41 / 2.77 |
| Office, ~50° | +0.93 / 3.70 | −0.39 / 2.87 | −1.84 / 3.81 | −2.18 / 3.86 | +0.23 / 3.61 | +0.21 / 3.15 |
| Office, 0.7 m | +2.19 / 4.23 | +0.57 / 2.80 | −0.84 / 3.26 | −1.06 / 3.33 | +1.40 / 3.88 | +1.08 / 3.20 |
| Office, 61° | +1.48 / 4.24 | +0.50 / 3.02 | −0.54 / 3.88 | −1.71 / 3.87 | +0.81 / 4.09 | +0.38 / 3.65 |
| Dead lamp, 1.5 m (38) | −1.41 / 4.15 | −2.00 / 3.06 | −3.75 / 4.79 | −3.78 / 4.82 | −1.97 / 4.48 | −1.22 / 4.00 |
| Lit side, 1.5 m (39) | +1.80 / 3.19 | +0.59 / 2.57 | −1.05 / 2.85 | −1.08 / 2.89 | +0.96 / 2.91 | +0.66 / 2.30 |

VP = one-surface F0 .04, alpha .08 + .55 F⁵, floor .6, `_Scatter` 3, on the new grime layout.

How to read it:
- **The reflection is now the largest term.** Going to one surface (VF1) or back to the world's 0.5 linear (VR0)
  drops ΔY by 1.5–3.4 in every lit view, and the pane turns into a slight darkening (ΔY < 0). In run 2 the floor was
  worth about +1.
- **The grime scatter is worth about 0.6–1.0 ΔY** (NEW vs VS0), down from about +4 in run 2's C state.
- **Clean glass (VG0) is still visible as a reflection**, unlike run 2's clean glass (−1.5 to −2.4 ΔY there).
- **Structure.** VP (old values, new layout) changes fine structure more than NEW: HP|Δ| .95–1.13 vs .89–1.10, and
  1−SSIM .020–.030 vs .014–.022. That is the old `_Scatter` 3 lighting up specks. NEW is the quieter, more
  glass-like pane.
- |ΔRGB| alone is not a visibility score: a darker, less reflective pane (VF1/VR0) also scores high.

## 5. The rest of the frame: zone cube and `ApplyAmbient`

| Frame | BEFORE → AFTER, run 3 (noise floor) | run 2 (B) | B → C (`ApplyAmbient` only), run 3 |
|---|---|---|---|
| 01 Level 0 | 1.46 (1.12) | 1.18 | 1.12 = noise |
| 34 Office | 0.80 (0.37) | 0.44 | 0.38 = noise |
| 35 Office cubicles | 0.75 (0.39) | 0.44 | 0.34 = noise |
| 37 dead lamp | 1.01 (0.98) | 1.01 | 0.98 = noise |

- **Zone cube.** Now at 0.5 linear (slider 0.735; decode x 0.500, `logs/g10r3_log.txt` env lines). That is 2.3× run
  2's 0.214. On walls, floors, carpet and the cubicle kit it is still just above the noise. These frames have no VCT
  and little metal, so audit §4.3's "high gain on VCT and metal" is still UNVERIFIED.
- **`ApplyAmbient`.** It changes nothing. SH0 red stays 0.064 in all states (`logs/g10r3_log.txt`). **Correction to
  run 2's §0.5/§5:** the "6–7 /255 brighter, Red must sign it off" came from the clone's pre-F5 constants
  (.22/.21/.17, .34/.31/.22, .62/.56/.40). Main has used the scene's values since 10:23 (F5), and the merged
  `FrontRoomsLook.cs` keeps them.

## 6. Dead lamp

- Frame 37 has no glass in view. The DeadLamp cube changes nothing measurable (1.01 vs a noise of 0.98).
- **38, from under a dead lamp.** The pane now reflects a lit troffer from the neighbouring cell in its upper part.
  It is the strongest "there is glass here" cue in run 3. At 50° the pane darkens the view by 4.8, because it
  reflects the darker DeadLamp cube.
- **The zone rule is still crude** (run 2 §6): the camera cell's own lamp is dead, but the Tall hall is lit by its
  neighbours. The map chat should pick DeadLamp from the local light level, not from one cell's lamp.
- **39 "lit side, dark beyond"** is still not a truly dark room (mean Y 103). G14 needs a staged dark room.

## 7. Props

Whole frame BEFORE → AFTER, run 3: bottle 0.97, vending front 2.24, vending 53° 1.60, desk 0.75, hutch 1.73 /255.

- **Water-cooler bottle.** A blue bottle before and after, slightly clearer. Fine.
- **Vending front.** Clearer than BEFORE, products saturated. At 53° there is a faint sheen and no distinct
  reflection. Fine.
- **STAGED desk glass.** The stand-in tumbler now shows a faint rim and sheen; in run 2 it almost vanished. It still
  needs real geometry (rim, thick base) to read as a drinking glass.
- **STAGED hutch + display cabinet: fixed.** The hutch's two arched glass panes read as glass over the dark interior
  again. The cabinet's glass shelves and side glass are back. That comes from the 8 % room reflection and the
  `_DustFilm` .12 / `_Scatter` .5 film on `Prop_Glass` ([g10r3_07](images/g10r3_07_props.jpg)). This corrects
  `10_implementation.md`'s "kit look holds" (it did not in run 2).

## 8. Other checks

### 8.1 Shards (STAGED; first test of `Glass_Shard`)

| Floor, distance | previous `Glass_Shard` (near-black): shard/carpet Y, dark chips | new `Glass_Shard` (0.85 × L0 carpet) | `Glass_ShardClear` (transparent) |
|---|---|---|---|
| Level 0 carpet, 1 m | 0.38, 97.6 % | 0.91, 0 % | 0.96, 0 % |
| Level 0 carpet, 2 m | 0.38, 97.5 % | 0.91, 0 % | 0.96, 0.2 % |
| Office carpet, 1 m | 0.45, 85.0 % | 1.27, 0 % (pale yellow chips) | 0.98, 0 % |
| Office carpet, 2 m | 0.45, 85.7 % | 1.27, 0 % | 0.98, 0 % |

Dark chips = shard pixels darker than 0.6 × the carpet under them. `logs/g10r3_metrics.txt` §50/§51.

The audit's dark shard reads as black chips. An opaque shard needs one colour per floor; the floor means are in
`30_final.md` §6. The transparent variant reads as glass on any floor. It shows its thin edges and a glint, and the
carpet shows through. GD3's plan uses transparent for desktop; this supports it.

### 8.2 G14 hook proof (`FrontRoomsGlassG10Capture.RunProofBatch`)

- **Setup.** Seed 4242, `timeScale` 0, post off, ARGBFloat target with 4× MSAA, URP HDR buffer 64-bit, readback
  RGBAFloat, exact float comparison.
- **Views.** 01, 34, 35, 38 ×2, 39, the Level 0 and Office windows at 1.5 m / 0.7 m / 50° / 61°, and the hutch.
  Every prop with `Prop_Glass` / `Prop_BottleBlue` in the built map is on the hook or baseline shader in every view.
  The props test hides every pane and checks that nothing else changes.
- **Pass 1:**
  - baseline twice (determinism);
  - (i) the hook shader with the globals never set.
- **Pass 2:**
  - (ii) weight 0 with a 1×1 red A = 1 texture;
  - (iii) weight 1 with A = 0;
  - the same with the keyword on;
  - P1 weight 1 with RT (2,0,0,1);
  - props with every pane hidden;
  - P2 orientation (2×2 texture, top-left texel only);
  - P3 depth tag at 1000 m;
  - P4 depth tag = the pane's own eye depth;
  - P5 tag + 0.5 m;
  - P6 = P4 at render scale 0.75.

| Run | Shader under test vs baseline | Result | Log |
|---|---|---|---|
| 1 | plain uniform branch, as the critic first wrote it, vs today's shader (md5 7fedbee6) | **49 of 90 checks failed.** (i) differs in 13–95 k pixels by 6e-8 to 9.5e-7 (the last bits). The hook's mere presence changes the compiled code | `logs/g14_hook_proof_1_uniform_branch_*` |
| 2 | `#pragma multi_compile_fragment _ _FR_GLASS_RT` (the critic's fallback) vs today's shader | **175 checks, 0 failed.** (i)–(iii) with the keyword off are exact in every view. Keyword on + weight 0 differs only in the last bits (≤ 9.5e-7, info). P1 changes 87–100 % of every pane mask and nothing on props. P2 changes only the top-left quadrant. P3/P5 change nothing. P4/P6 change the pane like P1 | `logs/g14_hook_proof_2_keyword_*` |
| 3 | the final shader (all fix-pass changes) vs itself with the five `[G14-HOOK]` blocks stripped (`g10_harness/make_baseline.py`) | **175 checks, 0 failed.** Keyword on + weight 0: ≤ 3.8e-6, info only | `logs/g14_hook_proof_3_final_shader_*`, [g14_hook_proof.jpg](images/g14_hook_proof.jpg) |

So the RT source must keep the keyword **off** whenever it is not driving the glass. "Weight 0 with the keyword on"
is visually identical but not bit-identical.

### 8.3 Shadows, sorting, cost

- **Shadows.** The AFTER pane casts none: renderer shadows off, no ShadowCaster pass.
- **Sorting.** No artefacts with one or two panes in view. Not a stress test.
- **Cost.** Render + GPU sync, median of 30, editor, Metal:
  - 01: 17.6 → 18.8 ms;
  - Level 0 window 1.5 m: 24.2 → 19.5 ms;
  - 0.7 m: 20.6 → 19.9 ms.

  Another Unity process was running (`proj_rs`), so this shows no difference beyond noise and nothing more.
- **Not tested:** a built player; WebGL in a browser; the title-stream rooms; the crossfade in motion (the play-mode
  test covers it, `10_implementation.md` §6.3); the hold and crack stages.

## 9. What the next pass should do (ranked; from these measurements)

1. **The glazing stop and frame (map chat, W1.4 / G9).** It is now the biggest missing cue. Every AFTER frame shows a
   pane with no outline.
2. **Judge the reflection level with Red at 50°/61° and under the dead lamp** (g10r3_02/03/06). The values are now
   physical (§3.3). If Red wants more, raise only the world zone intensity toward 1.0 linear where G7 box probes
   exist. Do not push glass above physical.
3. **Sharper lamp reflections** need G7 box-projected room probes (correct parallax) or the RT input (G14, Mac) /
   planar (G15, other desktops). The 256 px convolved cube gives a soft glint.
4. **Shards:** GD3 should use `Glass_ShardClear` on desktop and give opaque shards a per-floor colour (§8.1).
5. **Desk glassware:** geometry (rim, thick base) in the kit.
6. **Dead-lamp zone rule from the local light level** (§6).

## 10. G14 (Metal RT) and the hook

- **The hook now exists.**
  - Code: `FrontRoomsGlass.shader:101-106` (keyword), `:161-163` (`_RTReceive`), `:168-178` (globals), `:453-485`
    (the swap).
  - It feeds RT radiance through the same `EnvironmentBRDFSpecular` (Fresnel), fades it under grime and on the edge
    faces, and runs before fog, tonemap, grade and bloom.
  - Contract: `10_implementation.md` §8.1.
- **What G14 must change in ChatGPT's prototype** before it uses the hook (read-only check of main, 12:0x):
  - `FrontRoomsMetalGlassRT.mm:300` bakes a Fresnel and a strength into the colour (`color *= mix(0.15f, 1.0f,
    fresnel) * strength`). Remove both: the shader applies R_pane.
  - `:301` writes alpha 1. Write coverage or the depth tag instead.
  - `FrontRoomsMetalGlassRTRendererFeature.cs:17` composites at `AfterRenderingPostProcessing`. Replace that with a
    pass at or before `BeforeRenderingTransparents` that only publishes the texture.
- **What the measurements predict:**
  - A perfect reflection of these evenly lit rooms adds about what the physical cube adds face-on: +2–3 ΔY.
  - RT wins at angles and on moving or uncaptured things: the Relay, opened doors, the player's light, and a dark
    room behind a lit window.
  - So G14's acceptance frames should be oblique and dark-beyond views. A dark room must be staged; none exists near
    the start at seed 4242.
  - Any harness that swaps pane materials must keep the `FrontRoomsMetalGlassTarget` component.

## 11. Limits

- Seed 4242 near the start only (25 chunks), one Level 0 window and one Office window, as in the audit.
- The AFTER pane is 6 mm with shadows off, but has no glazing stop or stool trim.
- The props in 41/42 and the shards are staged by the harness. The rest are real placements.
- The window metrics are on 8-bit sRGB after post. The proof is on float HDR with post off.
- The harness reads private `FrontRoomsMapWorld` / `FrontRooms3DGame` members by reflection (`built`, `windows`,
  `pane`, `windowByCollider`, `fixtures`, `mode`, `phase`, `playerRoot`, `yaw`, `pitch`, …). A rename breaks the
  tool, not the game.
- The clone's gameplay files (`FrontRooms3DGame.cs`, `FrontRoomsMapWorld.cs`, the scene) are older than main's
  09:49–10:01 versions.
  - The scene's Lighting values are identical (checked by diff).
  - Main's `FrontRoomsMapWorld.cs` does change lamps: lamp modes now roll per difficulty tier, there is a WebGL-only
    near-lamp tick, and the start lamps are held. It also adds the Metal RT hooks.
  - So every BEFORE/AFTER pair here is internally consistent, but which lamps are dead or flickering at seed 4242 can
    differ in main's game. Re-capture after promotion.

## 12. Reproduce

In a clone with the glass work (never in the real project):

1. Copy `g10_harness/FrontRoomsGlassG10Capture.cs.txt` to `Assets/Editor/Audit/FrontRoomsGlassG10Capture.cs`, and
   `g10_harness/G10Before_*.mat.txt` to `Assets/Editor/Audit/G10/*.mat`.
2. For the proof, write `Assets/Editor/Audit/G10/FrontRoomsGlassBaseline.shader` with
   `python3 g10_harness/make_baseline.py Assets/Resources/Rendering/FrontRoomsGlass.shader <that path>`.
3. Run `Unity -batchmode -projectPath <clone> -executeMethod FrontRoomsGlassG10Capture.RunBatch -logFile <log>`, or
   `.RunProofBatch`. Run without `-quit`; the harness exits itself. It takes about 3 minutes and writes to
   `Verification/glass_g10/` (or `glass_g10_proof/`).
4. Build the sheets with
   `/usr/bin/python3 g10_harness/g10r3_sheets.py <run3 dir> <run2 dir> <out>` (needs Pillow).
