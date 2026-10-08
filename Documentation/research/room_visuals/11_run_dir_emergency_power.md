# 11 — Run! direction B · EMERGENCY POWER: pre-render

Date: 2026-10-08 (final round r8, renders 00:36–00:39, exports 00:41, check images 01:15). Brief: `10_run_directions.md` §1, §3, §7, plus Red's 2026-10-03 decision (the Run! floor is glossy waxed VCT with box-projected probes).
Clone-only. Nothing was promoted. Red's project was not opened in Unity. Only this folder was written, plus the two rows VL194–195 in `Documentation/VERIFICATION_LOG.md` and their Figma slides.

**One line.** A back-of-house service corridor on the alarm circuit: two-tone painted block, a dark soffit, a red sprinkler main overhead, four wall-mounted twin-head battery units. Step 1.5 m in and the mains drop: 0.3 s of black, then the sealed-beam heads snap on and throw hard warm pools every 6 m, with full-width dark bands between them. Red is only on fire equipment and the EXIT signs.

**Verdict.** 4 of the 7 §7.5 bars pass (S2 read, Relay, no pockets, photosafety). Three are partial: the goal is the brightest object but not the brightest region (a near head's own wall scallop is brighter), the janitor cart averages out against its background (1.16 : 1; its outline reads 1.84–2.22 : 1), and two direction targets do not fit together (p95/p5 ≥ 8 and saturated red 1–3 % against "pools ≈ 0.25 × armed"). See §4.

**New finding for every Run! direction (VL195).** In this build, box-projected probes only reach the room if their box overhangs the shell, and a batch look-dev cannot switch a probe's state between shots. Both are fixed here; the production rig needs the same care (§3 D10, D11).

---

## 1. Images

All in `images/`, JPG q85, 1920 px wide.

| Id | File | State | What it shows |
|---|---|---|---|
| S1a | `run_dir_emergency_power_s1a.jpg` | armed | The service corridor from the trip line: cool wraparounds, two-tone block, red main, the goal door small at 25 m |
| S1b | `run_dir_emergency_power_s1b.jpg` | tripped, t +3 s | The Run look: warm pools, dark bands, the goal in the hottest pool |
| S2 | `run_dir_emergency_power_s2.jpg` | armed | From Level 0, 2 m outside the arch: the NOTICE plate, the dark soffit, the red line, the twin-heads, the far door |
| S3 | `run_dir_emergency_power_s3.jpg` | tripped, 50° | The hero twin-head unit at z 9: heads, cookie pools, beam cones, pilot off |
| C1 | `run_dir_emergency_power_c1.jpg` | t −0.5 s | Stepping in |
| C2 | `run_dir_emergency_power_c2.jpg` | t +0.20 s | The blackout: only the EXIT faces and the goal lite |
| C3 | `run_dir_emergency_power_c3.jpg` | t +2.0 s | Mid-run at z 12.5: the flag EXIT's red glow on the block, the goal pool ahead |
| C4 | `run_dir_emergency_power_c4.jpg` | t +4.0 s | Looking back from z 23.5: the Relay proxy at z 11.5, backlit by the z 12 pool |
| Greys | `…_s1a_grey`, `…_s1b_grey`, `…_s2_grey`, `…_c4_grey` | | Linear Rec. 709 Y, re-encoded to sRGB |
| Protan | `…_s1b_protan`, `…_c4_protan` | | Machado 2009, severity 1.0, in linear RGB |
| Sheet | `run_dir_emergency_power_sheet.jpg` | | 18 tiles: 8 shots, 6 derived, 4 close-ups |
| Extras | `…_x_unit_armed`, `…_x_unit_tripped`, `…_x_armed_unit`, `…_x_goal`, `…_x_pull`, `…_x_wrap`, `…_r_cart`, `…_r_truck` | | Unit close-ups (pilot on / heads on), the goal at 5.5 m, the pull station and bell, a wraparound, the obstacles at the 8 m read distance |
| Matte | `run_emergency_power_<shot>_matte.jpg` for s1a, s1b, s2, s3, c1, c3, c4, x_goal, r_cart, r_truck | | The same build with the earlier matte floor (Red's rule: keep the matte versions) |
| Checks | `…_v_reflections.jpg`, `…_v_probefix.jpg`, `…_v_readdist.jpg` | | Verification images (VL194–195, §6) |

The 8 shots are also saved as `images/run_emergency_power_<shot>.jpg` (the stage's own naming). These are the **glossy** versions.

---

## 2. What was built, and every value used

Corridor-local metres: +Z runs from the entrance to the goal, x = 0 is the corridor axis. Root at world (4992, 0, 4992).
Base: Red's main as of 2026-10-07 16:3x (rsync into `W/proj_audit` → `W/proj_run_emergency_power`), plus the clone-only change list in `20_run_emergency_power.md` §2. Main's 19:22 hard-edge wallpaper print (Q1b) is not in this base, so the Level 0 wallpaper in S2 and C1 is the previous print.

### 2.1 Hero vs blockout

| Object | Quality | Source |
|---|---|---|
| Twin-head battery unit (signature) | **Hero, LOD0**: cabinet 3,706 tris, yoke 916, lamp head 2,480 (10,498 per unit with two heads) | clone-only Blender modules `run_twinhead.py`, `run_twinhead_yoke.py`, `run_twinhead_lamp.py` → `Kit_EmergencyTwinHead`, `_Yoke`, `_Lamp`. The rig aims each head like an installer: the yoke swivels on its boss, the head tilts in the yoke |
| 4 ft wraparound | Kit model, 200 tris | `run_wrap_4ft.py` → `Kit_WrapFixture_4ft` |
| Sealed-beam lens, wrap lens, unit legend, EXIT face, plates | Generated textures | `gen_run_b.py` (TeX Gyre Heros Bold Condensed / Bold) |
| Goal door RN-X | **Blockout** (correct size and albedo): main's `Prop_WoodLaminate` leaf, `Prop_SteelBrown` frame, lite beads, wired-glass stand-in, crossbar, kick plate, closer | harness |
| Side doors RN-K, pull station, bell, extinguisher cabinet, sprinkler main and heads, cable tray, EMT, bumper rail, chair dolly, janitor cart, hand truck | Blockout | harness |
| Relay | The brief's proxy | harness |

### 2.2 Space and surfaces

| Item | Value |
|---|---|
| Level 0 anteroom | x −4.5…4.5, z −6…0, ceiling 2.9. `L0_Wallpaper` / `L0_Carpet` / `L0_Ceiling` via `FrontRoomsSurfaces.Room(RoomRule.Lobby, slot)`, walls 0.16, cove base. 6 map lamps: lens 0.6 × 0.025 × 1.2 at the cell centre + 0.3 z, emission (1, .96, .84) × 2.6; spot 162° / 96°, (1, .96, .88), 5, range 10; the 2 nearest the arch soft-shadowed. NOTICE plate (0.254 × 0.178) on the Level 0 face, 1.10 m right of the axis, centre 1.5 m |
| Stub | One Level 0 cell z 27…30, one map lamp (unshadowed) |
| Corridor | x ±1.5 (faces ±1.42), z 0…27, soffit 2.9. Arch x −0.7…0.7, head 2.2, dark-bronze cased frame (50 mm face) and stainless guards on the corridor side |
| Walls | `Run_Block` (painted CMU 8 × 16 in, running bond, tooled joints in N; world tile 0.4 m). Two slabs per wall: lower 0–1.2 m #8E958B, upper #DAD7CB. Charcoal vinyl bumper rail #3A3A38, 0.15 tall at 0.9 m, on standoffs every 1.2 m; cove base #34332F |
| Floor (glossy) | `Run_ServiceVCT` A/N on mesh UVs (3.0 × 1.5 m tile): #B9B4A6 field, every 4th row #7E7A70, a burnished centre lane, wax build-up at the walls. **Mask `Run_ServiceVCTGloss_S`: smoothness field ~.80, lane ~.85, seams ~.38** |
| Floor (matte, for `_matte`) | Mask `Run_ServiceVCT_S`: smoothness .21–.80, lane ~.72 |
| Soffit | `Run_SoffitPaint` #45463F, form seams and tie holes, tile 2.4 m |
| Sprinkler main | Ø 0.10, #A3201A (smoothness .48), y 2.62, x +0.55, from the entrance face to the goal face. Upright heads every 3 m (z 1.5 + 3k): red tee boss, brass frame, red bulb, deflector. Band hangers on threaded rod every 3 m |
| Cable tray | Galvanized ladder tray 0.30 × 0.08 at y 2.70, x −0.60, rungs every 0.23 m, with cables |
| EMT | Three 3/4 in conduits on the left wall at **2.71–2.81** (deviation D6), straps every 1.5 m, a 4 in junction box every 3 m |

### 2.3 Lights and states

| Light | Value |
|---|---|
| Wraparounds (armed) | `Kit_WrapFixture_4ft` at (0, 2.9, 1.8 + 3k), k = 0…8, axis along Z. Spot 0.08 under the lens: **172° / 124°** (deviation D2), (0.90, 0.95, 1.00), 5.5, range 10. Soft shadows on k = 0, 3, 6. Cell 5 (k = 4) at 0.78 (the brief's stutter tube, rendered steady dim). An additive soffit scallop over each fixture (the light its side panels throw on the deck) |
| Mains drop | t = 0: every wrap 1 → 0 in 0.06 s. Lens glow 0.03, decaying with τ 0.5 s. The four red AC ON pilots go out |
| Twin-head units | Box centre 2.45 on the wall face. z 3 R, 9 L, 15 R, 21 L |
| Flood heads (7) | Pitch −30°, each aimed at the floor across the axis (x = −side × 0.55), so the beam edge clears the block beside the unit (yaw ~25° off the wall). Spot **100° / 96°** with cookie `Cookie_SealedBeam_S`, which carries the 50° / 20° beam plus ~2 % stray light out to 45° (deviation D3). (1.00, 0.84, 0.64), **intensity 10** (brief 4.0 SP; D4), range 11, soft shadows .95 |
| Goal head (unit 21, +Z) | A spot sealed beam aimed at the leaf, pitch −13°: **26° / 22°**, core cookie `Cookie_SealedBeamCore_S`, gain 5 (the same lamp in a narrow beam: (tan 25° / tan 12°)² ≈ 4.9 × the centre candela), range 13 |
| Trip | Head z rises 0 → 1 over 0.18 s (ease-in) from t = 0.35 + 0.004 z: 0.362 / 0.386 / 0.410 / 0.434 s for units 3 / 9 / 15 / 21 |
| Beam cones | Additive frustum per head (`RunBeamCone.shader`, the stream's VolumetricBeam idea), 2.5 m, **density .05** (brief .02; D5), gain 16 |
| EXIT signs | Flag sign at z 13.5 R, double-faced, faces ±Z, bottom 2.32, projecting from the wall; wall sign over the goal, top at 2.52. Housing **0.43 × 0.22 × 0.06** (D7). Face `Run_ExitSignB` (152 mm TeX Gyre Heros Bold Condensed letters, red on off-white, no arrows), emission 4.0. Glow: **two spots** in front of the flag faces (±Z, 170° / 120°, (1, .13, .07), 0.30, range 2.4) and one in front of the goal sign (0.6) (D8) |
| Floor bounce (look-dev fill) | One up-facing unshadowed spot 0.6 m under each pool (z 6, 12, 18, 25.6): 150° / 110°, (1, .86, .70), 1.0, range 5.5, scaled by the heads. The floor faces away from it, so it lights only what a lit pool would light by reflection: the main's underside, the tray, the soffit, the lower walls (D9) |
| Directional fill | None (audit F5). The game's "Soft ambient direction" (0.16) is left out |

### 2.4 Ambient, reflections, post

| Item | Value |
|---|---|
| Corridor ambient | `lightProbeUsage = CustomProvided`, SH through a MaterialPropertyBlock. **It works in URP 17.3** (no Light Probe Group fallback). Bounce model (D9): armed down .40 / side .25 / up .06, tint (.93, .97, 1.00) (≈ 0.6–6 × the global trilight per direction); tripped down .07 / side .05 / up .025, neutral (≈ 0.10–0.7 ×). The units get a local tripped term (down .20, side .12, up .07, tint (1, .90, .78)): their own heads' bounce |
| Global ambient (reference) | trilight SH up (.043, .038, .022), side (.061, .052, .027), down (.097, .079, .037) |
| Box probes (glossy) | **3 per state**, one per 3 cells (z 0–9, 9–18, 18–26.92), centre y 1.45, box **3.16 × 3.22 × 9.6** (the shell plus 0.16 m on x and y; 0.3 m overlap along z), blend 0.15, box projection on, 256 px HDR, baked with `Lightmapping.BakeReflectionProbe` in the armed (t −1) and tripped (t +3) states. Each state renders in its own Unity run with only its own set (D10, D11). URP asset: probe blending and box projection on; Surface shader: `_REFLECTION_PROBE_BLENDING` and `_REFLECTION_PROBE_BOX_PROJECTION` keywords (clone-only) |
| Blackout and "probes off" runs | No probes; the default reflection is a near-black cube (.002) |
| Matte runs | One flat custom cube per state, no box projection (the r1–r2 path) |
| Post `FrontRoomsPost_Run_B` | Lift (1, 1, 1, **0**) (brief −0.01; D1); contrast +12; saturation −10; white balance temperature −6; post-exposure −0.1; bloom threshold 1.0, intensity 0.6; vignette 0.32. `FrontRoomsPostStack.EnsureZoneVolume(corridor, "Run_B", 3.0 × 2.9 × 27, 1.0)`, then priority 2 |
| Camera | `FrontRoomsPostStack.ConfigureCamera`, clear #22231C, near 0.06, far 80, 1920 × 1080 ARGB32, 4× MSAA, rendered twice per shot. The G14 RT glass opt-in is removed from the look-dev camera and the RT feature is off in the clone's renderer (it stalled the first render > 20 min in batch) |

### 2.5 Signs, fire equipment, doors, props

| Item | Value |
|---|---|
| Pull station | z 1.0 R, centre 1.2 m: 130 × 180 × 60 mm, red #B01E18, face "FIRE" / "PULL DOWN" (white on red), handle #C42A20. 6 in bell above at 2.3 m (gong #A8231C, black striker) |
| Extinguisher cabinet | z 10.2 L, centre 1.1 m: semi-recessed 0.24 × 0.61, 70 mm proud, white #E4E2DA, clear glass, vertical red "FIRE EXTINGUISHER" legend, red extinguisher inside |
| Side doors RN-K (locked) | z 5.0 R "ELECTRICAL ROOM / AUTHORIZED PERSONNEL ONLY" plate; z 17.0 L "STAFF ONLY"; z 23.0 R stencilled "NOT AN EXIT". Almond enamel leaf (`Prop_SteelAlmond`) in a dark-bronze frame, stainless kick plate, lever with key cylinder, stainless jamb guards |
| Goal RN-X | 1.0 × 2.1, shut. Wood-grain laminate leaf, dark-bronze frame (50 mm face), 0.25 × 0.75 lite at 1.15–1.90 with steel beads, wired-glass stand-in: URP Lit transparent (.80, .85, .86, α .25), smoothness .9, plus a 12.5 mm wire grid. Crossbar at 1.0 on the corridor face, stainless kick plate, parallel-arm closer |
| Chair dolly | (1.0, 0, 6.4): 8 folded grey steel chairs (#5F615E) on a low dolly. Lane left 2.0 m |
| Janitor cart | (−1.13, 0, 12.0) on the left wall: **light platinum-grey** structural-foam plastic #ABA9A0 (r8; D12), dark vinyl bag, yellow bucket and wringer #D9AE14, mop, spray bottles. Lane left 1.9 m |
| Hand truck | (1.05, 0, 18.2), leaning 16° back on the right wall: black frame, 3 **white corrugated cartons** #D6D3C8 (r8; D12) with tape. Lane left 2.0 m |
| Relay | The brief's proxy at (0, 0, 11.5), facing +Z: capsule r 0.24, top 1.73; shoulders 0.78 × 0.24 × 0.30 at 1.72; head Ø 0.28 at 1.60, 0.12 forward; body #2B2928 (.25), head #D8D4C8 (.35); casts shadows |

### 2.6 Shots

As §7.4, 72° vertical FOV except S3 (50°): S1a (0, 1.62, 1.5) → (0, 1.30, 27), t −1; S1b same at t +3; S2 (0.35, 1.62, −2.0) → (0, 1.25, 27), t −1; S3 (0.6, 1.62, 6.8) → (−1.34, 2.45, 9.0), t +3 (mirrored, D13); C1 (0, 1.62, 0.8) t −0.5; C2 (0, 1.62, 1.5) t +0.20; C3 (0, 1.62, 12.5) t +2.0; C4 (0, 1.62, 23.5) → (0, 1.40, 0) t +4.0, Relay on. Extras: unit close-ups at 26° and 50°, the goal from z 21.5 (60°), the pull station (55°), a wraparound (60°), and **r_cart / r_truck**: tripped, on the axis, 8.0 m before the cart (eye z 4.0) and the hand truck (eye z 10.2). A 30 fps timeline at the C2 pose, t −0.5…+2.0, 480 × 270.

---

## 3. Deviations from the brief, and why

| # | Brief | Used | Why |
|---|---|---|---|
| D1 | Post lift w −0.01 | 0.0 | A −0.01 offset clips every pre-tonemap value under 0.01 to black (the soffit, the main, every gap), which breaks the 40 : 1 rule by construction. 0.0 still sits 0.035 under the global film lift |
| D2 | Wraps 162° / 96° | 172° / 124° | Tuned in the lost 2026-10-04 rounds; kept because the armed block reads evenly lit up to the soffit. The reason was not recorded in a surviving file |
| D3 | Heads 50° / 20° | A 100° / 96° spot whose cookie draws the 50° / 20° beam plus ~2 % stray to 45° | A real sealed beam leaks light around its hot core; the stray light shows the wall and soffit beside each unit, so the unit is never a black box in a black band. Spill was .06 on 10-04 and cut to .02 (blown scallops) |
| D4 | Head intensity 4.0 (SP) | 10 (goal head × 5) | Tuned on 10-04 (r13–r15) and checked again here: pool centres land at .17–.49 × the armed floor (target ≈ .25). At 4.0 the pools were a fifth of that |
| D5 | Beam density .02 | .05 | At .02 the cones do not show at this exposure |
| D6 | EMT at 2.55–2.65 | 2.71–2.81 | The left-wall units' heads reach 2.69; at 2.55–2.65 the conduits ran through them |
| D7 | EXIT housing 0.33 wide | 0.43 | 152 mm TeX Gyre Heros Bold Condensed "EXIT" with margins does not fit 0.33 |
| D8 | One point (1, .13, .07) 0.6, range 1.8 per sign | Two face spots 0.30 (flag), one 0.6 (goal), range 2.4 | A point at the housing sits 0.2 m from the block and paints a hot red disc on it (C3). Light leaves an EXIT through its faces, so it is split into spots in front of them |
| D9 | Corridor ambient 0.6 / 0.10 × global | A bounce model (absolute SH targets per direction) + four floor-bounce spots | URP has no realtime bounce. The brief's scale of the global trilight leaves the ceiling-facing sides as bright as the floor-facing ones; the bounce model lights the soffit and the main from the lit floor below, the way baked probes or an APV "tripped" scenario would |
| D10 | P2: one probe at the corridor centre; the Surface shader has no box projection | 3 box-projected probes per state, each box overhanging the shell by 0.16 m, blend 0.15 | Red's 10-03 decision. Measured r4: with the boxes sized exactly to the shell (blend 0.3), every floor, wall and soffit pixel sat on a box face, where URP's weight (distance to face / blend) is 0, so they all fell back to the default cube; probes ×0, ×1 and ×3 changed S1b by at most 9 of 765 levels. The overhang costs a 6 % parallax error across the corridor width |
| D11 | (not in the brief) | One Unity run per state (`-runState armed / tripped / none`), each with only its own probe set | Measured r5–r6: in batch, `ReflectionProbe.enabled`, `.intensity` and `.customBakedTexture` changes made after the first render never reach URP 17.3's Forward+ probe atlas (it caches each probe by instance id and re-blits the texture it first saw; no editor frame passes between shots). r5's tripped frames still reflected the armed cube (bluish far walls); with probes disabled before the first render, r6 rendered identical to probes on. The blackout and the timeline run without probes |
| D12 | Janitor cart "grey"; cartons unspecified | Cart light platinum-grey #ABA9A0; white corrugated cartons | Mid grey #5C5E5B and charcoal #3A3B39 both averaged out against the pool-lit block (1.04–1.20 : 1); kraft cartons were darker than the wall in S1b and lighter at 8 m. Both changes are period-plausible |
| D13 | S3 eye (−0.6, 1.62, 6.8) → unit at (1.34, 2.45, 9.0) | Mirrored: (0.6, 1.62, 6.8) → (−1.34, 2.45, 9.0) | The brief's unit list puts z 9 on the LEFT wall; (1.34, 2.45, 9.0) is bare block |
| D14 | The scene's Hunter if it can be instanced | The brief's proxy | The game's prototype Relay rig is 3.05 m tall and pushes through the 2.9 m soffit (the Relay contract is 2.05 m) |

Additions not in the brief: the r_cart / r_truck read-distance shots; the outline-contrast measure (§4.2 bar 4); unit close-ups; matte comparison runs.

---

## 4. Measurements and pass / fail

`run_research/measure.py` statistics on every frame (linear Rec. 709 Y). Contrast = (p95 + 0.05) / (p5 + 0.05), as in `measure.py`. Sat. red = R / (R + G + B) ≥ 0.8; red-dominant = > 0.6. Full data: `harness/emergency_power/metrics_final.json`.

### 4.1 Frame statistics (glossy, r8)

| Frame | Y p5 | Y p50 | Y p95 | Y p99.5 | Contrast | Sat. red % | Red-dominant % |
|---|---|---|---|---|---|---|---|
| S1a armed | 0.0003 | **0.0896** | 0.520 | 0.644 | 11.34 | 0.56 | 0.64 |
| **S1b tripped** | 0.0000 | 0.0033 | 0.116 | 0.487 | **3.33** | **0.03** | 0.08 |
| S2 armed | 0.0100 | 0.139 | 0.369 | 0.558 | 6.98 | 0.00 | 0.07 |
| S3 tripped | 0.0000 | 0.0080 | 0.140 | 0.523 | 3.80 | 0.05 | 0.15 |
| C1 (t −0.5) | 0.0003 | 0.0815 | 0.527 | 0.629 | 11.47 | 0.63 | 0.69 |
| C2 (t +0.20) | 0.0000 | 0.0000 | 0.0003 | 0.0003 | 1.01 | 0.04 | 0.05 |
| C3 (t +2.0) | 0.0000 | 0.0063 | 0.106 | 0.496 | 3.13 | 0.08 | 2.66 |
| C4 (t +4.0) | 0.0000 | 0.0033 | 0.050 | 0.427 | 2.00 | 0.04 | 0.07 |
| Goal close (t +3) | 0.0000 | 0.0050 | 0.089 | 0.533 | 2.78 | 0.18 | 0.36 |
| r_cart (8 m) | 0.0000 | 0.0161 | 0.095 | 0.295 | 2.90 | 0.03 | 0.18 |
| r_truck (8 m) | 0.0001 | 0.0171 | 0.093 | 0.312 | 2.85 | 0.18 | 1.23 |
| S1b protan | 0.0000 | 0.0033 | 0.109 | — | — | — | — |
| C4 protan | 0.0000 | 0.0033 | 0.048 | — | — | — | — |
| S1a matte | 0.0003 | 0.0936 | 0.515 | 0.643 | 11.22 | 0.6 | 0.6 |
| S1b matte | 0.0000 | 0.0033 | 0.106 | 0.458 | 3.12 | 0.0 | 0.1 |

### 4.2 The §7.5 bars

| # | Bar | Measured | Result |
|---|---|---|---|
| 1 | S2 readable before entry: twin-heads, the red main and the goal door in frame; door ≥ 40 px | Twin-heads 285 px, main 2,023 px, goal door 54 px tall (1,552 px), all unoccluded | **PASS** |
| 2 | S1b grey: the goal region (door + lite + sign) is the brightest region, or second after a fixture | Goal region mean Y **0.178** (area with the lit ring 0.160): brighter than every floor pool (0.10–0.20 on the axis) and every object. But the near wall scallop beside unit z 3 (0.43 in a same-size window) and that head's floor glint (0.60) are brighter. They are the fixture's own spill, 2 m from the camera | **PARTIAL** |
| 3 | C4 Relay ≥ 4 : 1 vs a 12 px ring (ID mask), also protan; report the glare | Proxy **8.06 : 1** (body 0.014 vs ring 0.117); upper ring 11.75, lower ring 3.49; protan **8.12 : 1**; outline median 14.9. Glare: Y > 0.5 on 0.29 % of the frame (6,087 px), in blobs at the units (the nearest 145 px from the Relay, the others 340–430 px) | **PASS** |
| 4 | Each obstacle ≥ 2 : 1 vs its background (S1b) | S1b: dolly **5.93**, cart **1.16**, truck **1.87**. At the 8 m read distance: cart **1.33**, truck **4.39**. Outline (median of object vs background in a 7 × 7 window along the edge): S1b dolly 1.64, cart 1.84, truck 2.52; 8 m cart 2.22, truck 1.84 | **PARTIAL** (cart) |
| 5 | No pockets: floor Y on the axis every 0.5 m, min ≥ max / 40 | min 0.0089 (z 9, under unit 9) / max 0.198 (z 18) = **22.4 : 1** | **PASS** |
| 6 | Photosafety: every change in 0–2 s; ≤ 3 per second | Table 4.3: one flash; every lamp changes once | **PASS** |
| 7 | B targets (S1b): armed median ≥ 0.09; pool ≈ 0.25 × armed; gap ≥ pool / 40; p95/p5 ≥ 8; saturated red 1–3 % | Armed S1a median **0.0896** (Level 0 Standard 0.091); pools .17 / .33 / .49 / .27 × armed at z 6 / 12 / 18 / 25.5 (mean .32); gaps 22 : 1; contrast **3.33**; saturated red **0.03 %** | **PARTIAL** (see below) |

Bar 7, the two misses:
- **p95/p5 ≥ 8** (Kane's lit back-of-house corridors) cannot hold together with "pool ≈ 0.25 × armed". With the 0.05 flare offset, 8 needs p95 ≥ 0.35 when p5 is ~0: 5 % of the frame at three times the pool level. The tripped frame's highlights do reach it: p99.5 0.487 gives 10.7. I kept the pools.
- **Saturated red 1–3 %.** The paint reds (#A3201A main, #B01E18 fire equipment) sit at R / (R + G + B) ≈ 0.72–0.74 even fully lit, so they never count as "saturated" by the WCAG rule; only the emissive EXIT letters do, and at 12–25 m they are small. Red-dominant (> 0.6) reaches **2.66 %** in C3, where the flag EXIT's glow lies on the block. In S1b the main reads as a dark red line (Main class mean Y 0.0012).
- z 18 at .49: the box probes add the far floor's reflection of the lit end wall at a grazing angle. It is the glossy floor doing what Red asked; it also lightens the dark band at z 20–24 to ~.055 (still 22 : 1 overall).

### 4.3 Photosafety: every change in the first 2 s after the trip

| Lamp or term | Changes 0–2 s | Detail |
|---|---|---|
| 9 wraparounds | 1 | 1 → 0 at t 0 over 0.06 s, all together (cell 5 from 0.78) |
| Their lenses | (the same change) | glow 0.03, decaying τ 0.5 s |
| 4 AC ON pilots | 1 | on → off at t 0 (red area shrinks; no red flash) |
| 8 heads | 1 each | 0 → 1 over 0.18 s (ease-in), from 0.362 / 0.386 / 0.410 / 0.434 s |
| 3 EXIT glow spots, faces | 0 | steady (emergency circuit) |
| Frame mean Y (timeline, 30 fps, C2 pose, no probes) | 2 transitions = **1 flash** | 0.181 → 0.064 (t 0.033) → 0.0001 (t 0.067–0.367) → 0.0232 by t 0.6, steady to t 2.0 |
| Saturated-red area | one step down | 0.53 % → 0.05 % at t 0.03; no rise |

---

## 5. Cost of what was rendered

| | Armed | Tripped (t +3) | Blackout (t +0.2) |
|---|---|---|---|
| Lights on (whole scene) | 19 (shadowed 5) | 22 (shadowed 10) | 10 (shadowed 2) |
| Corridor only | 12 = 9 wraps + 3 EXIT glows | 15 = 8 heads + 3 EXIT glows + 4 bounce fills | 3 EXIT glows |
| With the anteroom (6) and the stub (1) | 19 | 22 | 10 |

The brief's table: armed 9, tripped 10 in the corridor. The difference: +1 EXIT glow (two face spots on the flag sign, D8) and the 4 look-dev bounce spots (D9), which production would replace with baked probes.

| Item | Value |
|---|---|
| Renderers | 670 (corridor 638); submeshes 715 (corridor 683) = draws before batching, one pass |
| Merged-draw estimate | 49 distinct static materials + 38 rig-driven renderers ≈ **87 draws** for the corridor with static batching |
| Triangles | 74,392 in the scene (corridor 74,018); hero units 4 × 10,498 = 42,000 of it |
| Textures | 57 in the scene, 439 MB editor runtime size (mostly the Level 0 wallpaper maps). New Run B textures and cookies 81 MB at editor runtime size (28 textures) (largest: `Run_ServiceVCT` A/N/S 5.3 MB each at 2048 × 1024 BC7, the plates and EXIT face as RGBA32 3–8.5 MB each: compress them before production) |
| Probes | 3 per state × 256 px HDR cubes, 1.0 MB each at runtime (6 total), plus the Forward+ probe atlas |
| Per-frame extras | One SH set per corridor renderer per state change; beam-cone overdraw (8 additive cones, 2.5 m) |

---

## 6. Verification log

Placed in Figma, section FRONTROOMS · VISUAL VERIFICATION LOG (`2595:6093`, page `2099:76`), and recorded in `Documentation/VERIFICATION_LOG.md` §3 (task N2), 2026-10-08 01:3x.

| VL | Figma frame | Check | Verdict | Images (slot order) |
|---|---|---|---|---|
| VL194 | `2855:6151` | Emergency Power bars | PARTIAL | `run_dir_emergency_power_sheet`, `…_s1b_grey`, `…_c4_protan`, `…_v_readdist` |
| VL195 | `2855:6177` | Box probes reach the floor | PASS | `run_dir_emergency_power_v_reflections`, `…_v_probefix` |

The cover (VL000, `2597:6093`) now reads 193 checks, 577 images, 24 tasks; the N2 legend reads `VL039–042, 166–195 · 13 checks` (it still read 7: the House Lights stage's VL189–192 had not been added).

---

## 7. Files

- Harness: `harness/FrontRoomsRunLookdev_emergency_power.cs.txt`.
- Rig, shaders, post profile, Blender modules, kit sidecars, texture generator, measurement and run scripts, clone-only diffs, metrics, stats, timeline and change CSVs: `harness/emergency_power/`.
- Build log, change list, costs, needs and open issues: `20_run_emergency_power.md`.
