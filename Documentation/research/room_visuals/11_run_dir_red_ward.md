# 11 — Run! direction A · RED WARD: pre-render

Date: 2026-10-07 (final round v5-6, renders 19:52, exports 19:58). Brief: `10_run_directions.md` §1, §2, §7.
Clone-only. Nothing was promoted. Red's project was not opened in Unity. Only this folder was written (plus the three claimed rows in `Documentation/VERIFICATION_LOG.md`).

**One line.** A white 1990 hospital ward corridor at night, with a hanging double-faced EXIT sign every 6 m. Step 1.5 m in and the white tubes cut in one step. The steady red sign row is left as the only fill. The only white light is the wired-glass lite of the door you must reach.

**Verdict.** 5 of the 7 §7.5 bars pass. Two are partial: the goal box rule (the lite is the brightest object, but the small far door box averages below the near red pools) and the bare IV pole (1.67 : 1 on its own, 2.23 : 1 with its wheelchair). See §4.

---

## 1. Images

All in `images/`, JPG q85, 1920 px wide.

| Id | File | State | What it shows |
|---|---|---|---|
| S1a | `run_dir_red_ward_s1a.jpg` | armed | The night ward from the trip line: half the tubes lit, cool white; red glints already hang in it |
| S1b | `run_dir_red_ward_s1b.jpg` | tripped, t +3 s | The Run look: red pools under the sign row, the goal lite the only white |
| S2 | `run_dir_red_ward_s2.jpg` | armed | From Level 0, 2 m outside the arch: the white ward, the EXIT row and the far door |
| S3 | `run_dir_red_ward_s3.jpg` | tripped, 50° | The hero sign at z 12: stencil face, rods, red lobes on the tiles, the rail below |
| C1 | `run_dir_red_ward_c1.jpg` | t −0.5 s | Stepping in (QUIET PLEASE placard by the entrance) |
| C2 | `run_dir_red_ward_c2.jpg` | t +0.10 s | The cut: tubes dead, lens afterglow, eye not yet adapted |
| C3 | `run_dir_red_ward_c3.jpg` | t +2.0 s | Mid-run at z 12.5: the goal lite reads through the red |
| C4 | `run_dir_red_ward_c4.jpg` | t +4.0 s | Looking back from z 23.5: the Relay proxy at z 11.5, 12 m behind |
| Greys | `…_s1a_grey`, `…_s1b_grey`, `…_s2_grey`, `…_c4_grey` | | Linear Rec. 709 Y, re-encoded to sRGB |
| Protan | `…_s1b_protan`, `…_c4_protan` | | Machado 2009, severity 1.0, in linear RGB |
| Sheet | `run_dir_red_ward_sheet.jpg` | | 16 tiles: the 8 shots, 3 extras, 5 derived |
| Extras | `…_s3x.jpg`, `…_s3x_armed.jpg`, `…_s2_tripped.jpg` | | The hero sign at 1.9 m (30°) tripped and armed; S2's camera after the trip |
| Checks | `…_v_signrim.jpg`, `…_v_rimdiag.jpg`, `…_v_ambsweep.jpg`, `…_v_tripstrip.jpg` | | Verification images (VL166–168, §6) |

The same 8 shots are also saved as `images/run_red_ward_<shot>.jpg` (the stage's own naming).

---

## 2. What was built, and every value used

Corridor-local metres: +Z runs from the entrance to the goal, x = 0 is the corridor axis. Root at world (4992, 0, 4992).
Base: Red's main as of 2026-10-07 16:34 (rsync into `W/proj_audit` → `W/proj_run_red_ward`), plus the clone-only change list in `20_run_red_ward.md` §2.

### 2.1 Hero vs blockout

| Object | Quality | Source |
|---|---|---|
| Hanging EXIT sign (signature) | **Hero, LOD0** (2,212 tris) | clone-only Blender module `run_exit_sign_hanging.py` → `Kit_ExitSign_Hanging` |
| Wall EXIT sign over the goal | Kit model (836 tris) | clone-only `run_exit_sign_wall.py` |
| Sign face texture | Hero (4 px/mm) | `make_textures.py`, TeX Gyre Heros Bold Condensed |
| Goal door RN-X | Kit: main's `Kit_DoorFrame_Steel` + `Kit_ExitDevice_Crossbar`, clone-only leaf `Kit_DoorLeaf_Ward_Lite` | interactables G1 + `run_door_leaf_ward_lite.py` |
| Closer on the goal door | Blockout (correct size, aluminium) | harness |
| Side doors RN-K | Kit: main's frame, steel leaf, G2 lock trim | interactables G1/G2 |
| Stretcher, 3 visitor chairs, wheelchair, IV pole, linen cart | Kit models, LOD0 | clone-only `run_*.py` modules |
| Handrails, smoke detector, troffers, arch frame | Blockout or copied stream fixture (correct size and albedo) | harness |
| Door plates, QUIET PLEASE placard | Generated faces (TeX Gyre Heros Bold) | `make_textures.py` |
| Relay | The brief's proxy (§2.5) | harness |

### 2.2 Space

| Item | Value |
|---|---|
| Level 0 anteroom | x −4.5…4.5, z −6…0, ceiling 2.9. `L0_Wallpaper` / `L0_Carpet` / `L0_Ceiling` through `FrontRoomsSurfaces.Room(RoomRule.Lobby, slot)`. Walls 0.16 on the cell lines, rubber cove, an aluminium carpet bar under the arch. The ceiling print is shifted 0.3 m so the tile grid sits as in the map |
| Map lamps (6 + 1 in the stub) | Lens 0.6 × 0.025 × 1.2 at the cell centre + 0.3 z. URP Lit albedo (1, .98, .92), smoothness .1, emission (1, .96, .84) × 2.6. Spot 0.06 m under the lens, 162° / 96°, (1, .96, .88), intensity 5, range 10. **All 7 cast soft shadows** (deviation D1) |
| Corridor | x ±1.5 (faces ±1.42), z 0…27, ceiling 2.9. `Run_Wall` / `Run_Floor` / `Run_Ceiling` through `RoomRule.Run`. The ceiling print is shifted half a tile so the troffer row sits on the grid |
| Arch | x −0.7…0.7, head 2.2, in the z = 0 wall. Dark-bronze `Prop_SteelBrown` cased frame, 0.05 m face, on the corridor side |
| Handrails | Both walls. Top 0.92, 0.14 tall, 0.06 proud. Almond vinyl #C9C3B2, smoothness .42, round grip. Aluminium brackets every 1.2 m. Returns at every end; cut 0.62 m each side of a side door |
| Stub beyond the goal | One Level 0 cell, z 27…30, one map lamp |

### 2.3 Goal door and side doors

| Item | Value |
|---|---|
| RN-X goal | 1.0 × 2.1, shut, centred in the z = 27 wall. Hinge on the right seen from the corridor; it swings away from the runner, into the stub |
| RN-X leaf | `Kit_DoorLeaf_Ward_Lite` (clone-only): wood-grain laminate (RN-F's FREE value), 0.25 × 0.75 lite at 1.15–1.90, stainless kick plate. 4,376 tris |
| RN-X hardware | `Kit_ExitDevice_Crossbar` on the corridor face at 1.0 m. Parallel-arm closer (blockout, aluminium) on the corridor face. The far-side lever trim is not placed (the far face is never in frame) |
| Wired-glass stand-in | URP Lit transparent (.80, .85, .86, α .25), smoothness .9 (the leaf's glass submesh). Wire: alpha-tested 12.5 mm grid, 0.6 mm lines, #6E6E6A, smoothness .35, metallic .6, in the glass mid-plane (deviation D7) |
| RN-K side doors | Locked. z 4.5 left, 10.5 right, 16.5 left, 22.5 right. `Kit_DoorFrame_Steel` (dark bronze) + `Kit_DoorLeaf_Steel` (enamel → `Prop_SteelAlmond`) + `Kit_Lock_Escutcheon` / `_Knob` / `_CylinderShell` on the corridor face |
| Plates | Engraved phenolic (dark brown face, cream core), TeX Gyre Heros Bold, on the latch side at 1.52 m: "4-11", "STAFF ONLY", "4-13", "LINEN". Plus "QUIET PLEASE" by the entrance (§2.3 option) |

### 2.4 Lights

| Light | Value |
|---|---|
| Troffers | The stream fixture at (0, 2.9, 1.8 + 3k), k = 0…8: `Painted_Metal` pan 0.588 × 0.03 × 1.188, lens 0.54 × 0.006 × 1.14, `VolumetricBeam` frustum, density .012 (lit fixtures only) |
| Troffer lens (Run A copy) | `Troffer_Lens` with smoothness .55 and base .6 (deviation D5) |
| Armed (night switching) | k = 0, 2, 4, 6, 8 lit: spot 162° / 96°, (0.93, 0.96, 1.00), intensity 3.5, range 9, soft shadows (high tier). Odd k off: lens glow .04 |
| Trip at t = 0 | Lit troffers 1 → 0 over 0.08 s, all together. Lens glow decays with τ 0.12 s to .04 |
| Hanging EXIT signs | Centres at z 6, 12, 18, 24, housing centre y 2.42 (bottom 2.32). Always on, always steady |
| Sign spot (per hanging sign) | At (0, 2.31, z), pointing down. (1.00, 0.13, 0.07), intensity 2.4, range 6.5, 170° / 120°, soft shadows (high tier) |
| Ceiling glow (per hanging sign) | **Two spots** (deviation D2): 8 cm out from each face centre at y 2.42, aimed 45° up and out along ±Z, 130° / 40°, (1.00, 0.13, 0.07), 0.42 each, range 2.2, no shadow |
| Wall sign over the goal | `Kit_ExitSign_Wall`, top 2.38, face toward the runner. Its spot at (0, 2.165, 26.86), tilted 60° from vertical toward −Z, intensity 2.0, range 6.5, 170° / 120°, soft shadows |
| Smoke detector | Ceiling at z 3.0, Ø 0.15 (white body, black vent ring). Red LED (1, .05, .03) × 6: armed, on for 0.12 s every 8 s; tripped, steady on |
| Directional fill | None (audit F5). The game's "Soft ambient direction" (0.16) is left out |

### 2.5 Surfaces and props

| Item | Value |
|---|---|
| Walls / floor / ceiling | `Run_Wall` (white #E3E1D9, luminance 0.738), `Run_Floor` (12 in VCT with the wax lane), `Run_Ceiling` (2 × 2 ft tile), all through `RoomRule.Run` |
| Hanging sign housing | Painted steel bezel 0.330 × 0.200 × 0.060, 12 mm bezel, faces 0.306 × 0.176 recessed 4 mm, a 2 mm lip return, access plate and two captive screws underneath. Ceiling canopy 0.27 × 0.07 × 0.016. Two chrome stems Ø 12 mm, 0.21 m apart, with collars and lock nuts; drop 0.38 m |
| Sign face `Run_ExitSign_Ward` | FrontRooms/Surface on mesh UVs, smoothness .30. Albedo `RunA_ExitFace_A` (1224 × 704, 4 px/mm): off-white enamel (.905, .890, .848), slight heat yellowing over the two lamps, red diffuser (.70, .13, .10) in the stencil cuts with a 1 mm darker cut edge. Emission `RunA_ExitFace_E`: letters only, two lamp hot spots, 1 px diffuser soft edge |
| Face legend | "EXIT", TeX Gyre Heros Bold Condensed, 152 mm cap, no arrows. No Arial, no brand, no date |
| Face emission | (3.2, 0.19, 0.16): 3.2 in red, green and blue held at 6 % and 5 % (deviation D3) |
| Stretcher | `Kit_Stretcher` at (0.74, 0, 7.0), turned 12° into the lane. 2.0 × 0.70, chrome frame, **navy vinyl pad #2A303B** (deviation D6), white and blue linen. Lane left 1.6 m. 7,568 tris |
| Visitor chairs | `Kit_VisitorChair3` at (−1.03, 0, 12.5): three ganged chrome sled-base chairs, teal vinyl (.20, .36, .35). Lane left 2.1 m. 5,904 tris |
| Wheelchair + IV pole | `Kit_Wheelchair` at (0.80, 0, 18.4), turned 30°: folding chrome, black sling #1C1B1A. `Kit_IVPole` at (1.12, 0, 19.45): 1.9 m chrome pole, 5 casters, clear PVC bag (transparent stand-in). Lane left 1.7 m. 8,816 + 2,132 tris |
| Linen cart | `Kit_LinenCart` at (−1.08, 0, 23.5), long side to the wall: aluminium frame, canvas bag #D2CFC2, linen. Lane left 2.0 m. 4,348 tris |
| Relay | The brief's proxy at (0, 0, 11.5), facing +Z: capsule body r 0.24, top 1.73; shoulder box 0.78 × 0.24 × 0.30 at 1.72; head sphere Ø 0.28 at 1.60, 0.12 forward; body #2B2928 (smoothness .25), head #D8D4C8 (.35); casts shadows (deviation D8) |

### 2.6 Ambient, post, reflections, eye

| Item | Value |
|---|---|
| Corridor ambient | Renderers set `lightProbeUsage = CustomProvided`, SH through a MaterialPropertyBlock (`CopySHCoefficientArraysFrom`). **It works in URP 17.3**: no Light Probe Group fallback was needed. Armed 0.6 × the global trilight SH. Tripped **0.25** × global, tinted (1.0, 0.35, 0.30) (deviation D4). Blended by the troffer level |
| Global ambient (for reference) | trilight SH DC (L0) = (0.0643, 0.0541, 0.0274) |
| Sign-face bounce | A uniform SH term added on the 5 sign renderers only: armed 0.4 × troffer white, tripped 0.035 × sign red (deviation D3) |
| Haze | Fog colour armed = `FrontRoomsLook.FogColor`; tripped (0.10, 0.018, 0.014) |
| Post `FrontRoomsPost_Run_A` | Lift (1, 1, 1, 0); white balance temperature 0; contrast +8; saturation 0; bloom threshold 1.0, intensity 0.70; vignette 0.30. Added with `FrontRoomsPostStack.EnsureZoneVolume(root, "Run_A", bounds 3 × 2.9 × 27, 1.0)`, then priority 2 |
| Eye adaptation | **+3.2 EV** on the Run_A volume's post exposure after the cut: delay 0.15 s, τ 0.5 s (deviation D4). The sign faces hold their on-screen value: emission × 2^(−E × 1.15) |
| Reflections | The game's zone path: the default reflection is a corridor-centre cube (512 px HDR, corridor only, black environment), one capture armed and one tripped, linear 0.5. The three box-projected probes are bake-only (deviation D9) |
| Camera | `FrontRoomsPostStack.ConfigureCamera`, clear #22231C, near 0.06, far 80, 1920 × 1080 ARGB32, 4× MSAA, rendered twice per shot, Ultra quality level |

### 2.7 Shots

As §7.4, all 72° vertical FOV except S3 (50°): S1a (0, 1.62, 1.5) → (0, 1.30, 27), armed (t −8); S1b same at t +3; S2 (0.35, 1.62, −2.0) → (0, 1.25, 27), armed; S3 (0.9, 1.62, 9.3) → (0, 2.42, 12.0), t +3; C1 (0, 1.62, 0.8) at t −0.5; C2 (0, 1.62, 1.5) at t +0.10; C3 (0, 1.62, 12.5) at t +2.0; C4 (0, 1.62, 23.5) → (0, 1.40, 0) at t +4.0, Relay on. Extras: S3x (0.55, 1.95, 10.2) → (0, 2.36, 12), 30°, tripped and armed; S2 tripped; C4 with the game rig; a 91-frame trip sequence (t −0.30…+2.70 at 30 fps, 640 × 360).

---

## 3. Deviations from the brief, and why

| # | Brief | Used | Why |
|---|---|---|---|
| D1 | The 2 map lamps nearest the arch cast soft shadows | All 7 map lamps cast soft shadows (high-resolution tier for the lamp nearest the arch and the stub lamp, medium for the other 5) | Unshadowed map spots light through the 0.16 m walls into the corridor (warm stripes on the white wall). In the game the map's own shadow policy decides; flagged as an open issue |
| D2 | One unshadowed ceiling-glow **point** per sign, 0.5, range 2.2 | **Two unshadowed spots** per sign, 0.42 each, 45° up and out, 130° / 40° | An unshadowed point lights every surface that faces it, through the housing. At the housing centre and 3 cm above it alike, it lit the bezel's inner lips: a white-hot rim round both faces once the eye adapted. Turning the glow light off removed the rim; turning reflections off did not (`v_rimdiag`). An upward spot above the housing fixed the rim but drew one hard disk over the sign. Two out-aimed spots match how light leaks from a stencil face (along the face normal), keep the sign and its lips outside the cone, and give the ceiling two lobes fore and aft. 0.42 gives the ceiling 0.5 m out from a face the same light as the brief's 0.5 point. Cost: +4 unshadowed lights (§5) |
| D3 | Face emission 3.2 (white) | (3.2, 0.19, 0.16); faces held through the adaptation; a sign-face bounce term | A white emission × red diffuser albedo went peach once the eye opened up. Holding G/B keeps the letters red. URP has no realtime bounce, so the off-white face rendered mid grey (sRGB ~90 on the panel at 1.9 m) beside a lit troffer; a bounce of 0.4 × troffer white on the sign renderers brings it to a light off-white (~137) |
| D4 | Tripped ambient 0.15 × global tinted; no eye adaptation | Tripped ambient 0.25 tinted; post exposure +3.2 EV after the cut | With the brief's light values and no adaptation, tripped S1b has median Y 0.0005 (`dbg_s1b_noexp`): the ETB "too dark" band. Adaptation brings it into the 0.015–0.04 target. At 0.15 the adaptation needed (+3.585 EV) left the tripped frame brighter on average than the armed ward (mean 0.064 vs 0.054) and the Relay at 5.8 : 1. At 0.25 / +3.2 EV every target is met and the mean (0.055) equals armed (`v_ambsweep`). 0.35 / +3.0 drops saturated red to 20 % (under 25 %) |
| D5 | `Troffer_Lens` as in the stream | A copy with smoothness .55 and base .6 | The stream lens is a mirror (smoothness 1, white). After the trip, at +3 EV, the dead lens next to a red sign mirrored it white and read as still lit |
| D6 | Stretcher pad white vinyl | Navy vinyl #2A303B | The white pad under red light read 1.2 : 1 against the red floor (fails the 2 : 1 obstacle bar). Navy is period-correct and reads 2.51 : 1 |
| D7 | Wire quad 2 mm behind the glass | Wire in the glass mid-plane | The kit leaf carries its glass as a submesh; the wire sits inside the 6 mm pane, as in real wired glass |
| D8 | The scene's Hunter if it can be instanced, else the proxy | The proxy; the game rig rendered once as reference (`c4_gamerig`) | `FrontRooms3DGame.CreateHunter` instances, but in the empty look-dev scene its prototype rig spans 4.2 m of bounds (y −1.12…3.04) and its limbs float apart. A broken silhouette is no test. Its reading is reported in §4 |
| D9 | P2: a box-projected corridor probe | The zone-cube path (default reflection per state) | The box probes did not reach the FrontRooms/Surface walls in this build (S1b with vs without them: max difference 7 of 765 levels), and the sky fallback put grey dashes on the upper walls and white glints on chrome after the trip |

Additions not in the brief: the haze colour follows the state; the trip-sequence frames; S3x close-ups; `c4_gamerig`.

---

## 4. Measurements and pass / fail

`run_research/measure.py` statistics on every frame (linear Rec. 709 Y). Contrast = (p95 + 0.05) / (p5 + 0.05), as in `measure.py`; the raw ratio is given too. Full data: `harness/red_ward/metrics_final.json`.

### 4.1 Frame statistics

| Frame | Y p5 | Y p50 | Y p95 | Contrast | Raw p95/p5 | Sat. red % |
|---|---|---|---|---|---|---|
| S1a armed | 0.0008 | 0.0357 | 0.1614 | 4.16 | 202 | 0.5 |
| **S1b tripped** | 0.0027 | **0.0164** | 0.2532 | **5.76** | 94 | **26.8** |
| S2 armed | 0.0100 | 0.1379 | 0.3246 | 6.24 | 33 | 0.0 |
| S3 tripped | 0.0003 | 0.0231 | 0.3518 | 7.99 | 1159 | 25.8 |
| C1 | 0.0011 | 0.0600 | 0.2684 | 6.23 | 239 | 0.2 |
| C2 (t +0.10) | 0.0000 | 0.0005 | 0.0154 | 1.31 | 1539 | 21.6 |
| C3 (t +2.0) | 0.0035 | 0.0352 | 0.1995 | 4.66 | 56 | 50.3 |
| C4 (t +4.0) | 0.0013 | 0.0301 | 0.2065 | 5.00 | 161 | 55.7 |
| S3x tripped | 0.0029 | 0.0189 | 0.3037 | 6.68 | — | 21.9 |
| S3x armed | 0.0009 | 0.0084 | 0.2273 | 5.44 | — | 7.7 |
| S1b protan | 0.0017 | 0.0112 | 0.1624 | 4.11 | 94 | — |
| C4 protan | 0.0007 | 0.0182 | 0.1251 | 3.46 | 181 | — |
| S1b, no adaptation (debug) | 0.0000 | 0.0005 | 0.0150 | 1.30 | — | 21.5 |

### 4.2 The §7.5 bars

| # | Bar | Measured | Result |
|---|---|---|---|
| 1 | S2 readable before entry: the sign row and the goal door in frame, door ≥ 40 px | Door 56 px tall (1,677 px visible); hanging signs 711 / 181 / 98 / 60 px, wall sign 40 px | **PASS** |
| 2 | S1b grey: the goal region (door + lite + sign) is the brightest region, or second after a fixture | Lite mean Y **0.852** (max 0.942): the brightest object; next object the IV pole 0.178; the frame's max outside the goal is 0.912 on 4 px. But the 34 × 70 px goal box (mean 0.128) ranks below 4,764 of 29,810 same-size windows: the near red wall pools (best non-fixture window 0.434) | **PARTIAL** |
| 3 | C4 Relay ≥ 4 : 1 vs a 12 px ring (ID mask), also protan; expect ≥ 6 | Proxy **6.85 : 1** (body 0.038 vs ring 0.258); protan **9.79 : 1**. Game rig reference 3.15 : 1 | **PASS** (proxy) |
| 4 | Each obstacle ≥ 2 : 1 vs its background (S1b) | Stretcher 2.51, chairs 3.41, wheelchair 3.17, linen cart 3.00; IV pole alone **1.67**; wheelchair + IV pole (one row in the brief) 2.23 | **PARTIAL** (IV pole) |
| 5 | No pockets (B only) | — | n/a |
| 6 | Photosafety: the trip is one step only; ≤ 3 changes per second per lamp | Table 4.3 | **PASS** |
| 7 | A targets, tripped S1b: median Y 0.015–0.04; p95/p5 ≥ 5; saturated red 25–50 %; goal the brightest | 0.0164; 5.76 (raw 94); 26.8 %; see bar 2 | **PASS** (goal: partial) |

### 4.3 Photosafety: every change in the first 2 s after the trip

| Lamp or term | Changes 0–2 s | Detail |
|---|---|---|
| 5 lit troffers | 1 | 1 → 0 over 0.08 s, all together |
| Their lenses | (the same change) | 2.6 → 0.04, afterglow τ 0.12 s |
| 4 unlit troffers | 0 | |
| 4 hanging signs (spot + 2 glow spots each), wall sign | 0 | steady |
| Smoke detector LED (Ø 6 mm) | 1 | 8 s blink → steady on at t 0 |
| Eye adaptation | 1, smooth | 0 → +3.2 EV, monotone, ~2.5 s |
| Frame mean Y (trip sequence, 30 fps) | 2 transitions in 2.7 s | 0.0535 → 0.0032 in 0.13 s; then a monotone rise to 0.0552 by t 2.7 |
| Saturated-red area | one step, then one swell | 0.5 % → 21.6 % at the cut; rises to 63.5 % at t 0.7 as the eye opens; settles to 27.7 %. No flash, no repeat |

`v_tripstrip.jpg` shows t −0.10, 0, +0.03, +0.07, +0.10, +0.20, +0.50, +1.0, +2.0, +2.7.

---

## 5. Cost of what was rendered

Lights (from `harness/red_ward/run_costs.txt`):

| | Armed | Tripped |
|---|---|---|
| Corridor | 18 = 5 troffers + 4 sign spots + 8 glow spots + 1 wall-sign spot | 13 |
| With the anteroom (6) and the stub (1) | 25 | 20 |
| Shadowed | 17 (6 anteroom + 5 troffers + 5 sign spots + 1 stub) | 12 |

The brief's table had 14 armed / 9 tripped in the corridor; the 4 extra are the second glow spot per sign (D2, unshadowed, range 2.2).

Per shot (estimates from the harness): S1a 229 draws + ~1,200 shadow-caster draws, 125,624 tris in view; S1b 224 + ~751, 125,604 tris; S2 261 draws; C3 141; C4 207. The corridor holds 191 renderers and 153,774 tris. Scene textures: 54, 491 MB (mostly the Level 0 wallpaper maps). New Run A textures: 32 MB uncompressed in the clone (two 1224 × 704 face maps, five plates, the wire grid).

---

## 6. Verification log

Claimed in `Documentation/VERIFICATION_LOG.md` §3 (task N2): **VL166** Red Ward pre-render bars (PARTIAL), **VL167** Sign rim was the glow light (PASS), **VL168** Tripped fill vs adaptation (PASS). Slide ids and placement: see that file.

---

## 7. Files

- Harness: `harness/FrontRoomsRunLookdev_red_ward.cs.txt`; rig, Blender modules, texture and analysis scripts, the post profile, the clone-only diffs, metrics and the trip CSV under `harness/red_ward/`.
- Build log and change list, costs, needs and open issues: `20_run_red_ward.md`.
