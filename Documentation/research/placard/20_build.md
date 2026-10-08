# 20 — Evacuation placard (Q16): build report

Status: **BUILT in the private clone, 2026-10-07 (continuation after the 2026-10-05 wipe).** Art **v2**.
- Clone: `/Users/redwang/FrontRoomsVisualWork/proj_placard` (main 16f520e + this work; compiles, 0 errors, `-buildTarget OSXUniversal`). Main is now a5262fb: the commits since 16f520e touch touch-controls, the HUD key glyph, outlets and the placard's own `LEGEND_BOX` line only, so nothing the placard depends on moved (checked 2026-10-07 18:2x).
- Red's project: only these were written: this folder, the Q16 rows of `Documentation/VERIFICATION_LOG.md` (VL123–125, VL127), and one data line in `Tools/Blender/frontrooms_kit/assets/evac_placard_common.py` (the art-v2 legend box; Red's auto-commit 5e6e7f9 already holds it). Unity was never opened on Frontrooms3D.
- Last run in the clone: 2026-10-07 17:45–17:47 (`lookdev3`: simulation, 100-seed placement and the Unity three-view, exit 0, 0 compile errors).
- Spec: `10_spec.md`. Where it assumed the v1 artwork, this report says so (§2).

## 0. Summary

| Item | Result |
|---|---|
| Kit | `Kit_EvacPlacard` (frame + sheet) and `Kit_EvacPlacardLens` (lens), built from the main modules into the clone |
| Triangles | LOD0 **1,754** (budget 1,600 ± 15 %: +9.6 %). LOD1 **106** (budget 120). LOD2 **26** (budget 28). Lens **10** |
| LOD in the FBX | LOD0 only, by the kit convention, until P-1 (`kitlib.make_lods`) lands. LOD1/LOD2 are built and reviewed (§3.3) |
| Geometry checks | Bounds 466.8 × 313.8 × 13.5 mm, sight inset 25.400 / 25.400 mm, lip nose on the lens at 5.550 mm, every face oriented. FBX round trip: 128 face-arc loops, worst normal error **0.000°** |
| Materials | `Prop_EvacPlan` (FrontRooms/Surface, art as base map, glow mask as emission map) and `Prop_LensNonGlare` (URP Lit, black base α 0.04, preserve specular, no shadow caster). Both made by *Set up* in the clone |
| Glow | `FrontRoomsPlacardGlow` + `FrontRoomsPhosphor(Cell)`. Simulation: **14 / 14 PASS** (normal and Reduce flashing) |
| Peak emission | **k = 0.158** (was the spec's estimate 0.13). Measured in engine: the full glow adds **12.0 %** of the lit sheet, the walls' cap |
| Dark legibility | Glyph/paper **1.23** in the A4 cell (spec bar 1.30): **FLAG** (§5.4) |
| Placement | 100 seeds: P1 **87 %**, P2 **9 %**, P3 **4 %** (spec estimate 80 / 17 / 3) |
| In-engine looks | A1 (0.3 m), A2 (60°), A3 (lit), A8 (2 m, 6 m), no-lens control: PASS. A4: works, but faint with lit neighbours (FLAG above) |
| Three-view (Unity) | Front, side, top and ¾ of both kits at 4,000 px/m: v2 art square in the sight, 13.5 mm bullnose profile: PASS (§3.5) |
| Figma | VL123, VL124, VL125, VL127 in FRONTROOMS · VISUAL VERIFICATION LOG (§10) |

## 1. Recovery (what this continuation started from)

The 2026-10-05 reboot wiped the old clone, previews and tools. Recovered in this order:
1. **Blender modules**: in main since Codex's commit 03c43ed (`evac_placard.py`, `evac_placard_common.py`, `evac_placard_lens.py`). They are the round-3/4 versions the last build used (checked against that build's own diff of main vs clone).
2. **C# (4 runtime files + the lookdev tool)**: from the last build agent's own `cat` of each file (transcript `wf_c1037b24-7a9/agent-a40907fce1fe078a8`, steps 23–26; sizes match its `ls` byte for byte), then its four later edits replayed (steps 61, 63, 64, 66). The tools-recovery stage rebuilt the same five files independently (`W/tools/feature/placard`): **byte-identical**.
3. **The RenderSetup and RoomStream edits**: from that agent's `diff -u` (step 22), now an idempotent patch script (§6).
4. **`pack_evac_plan.py`**: the original Write (agent a15e4839…, step 49), 3,966 bytes.
5. **The Blender review tool**: `review.py` + 4 replayed edits → `review3.py` (15,372 bytes, as before).

Nothing was rebuilt from memory. Every result below was re-run on the recovered code.

## 2. Art v2: what changed against `10_spec.md`

平面视觉 redrew the art on 2026-10-07 (Red: "按新墙面提示重画"). Same paths, same size (2592 × 1674, 6 px/mm).

| Spec assumption (v1) | v2 fact | What the build did |
|---|---|---|
| Legend = InkShape glyphs, FLOW arm 32.5° | WP03 swatches at 1/25 (AS PRINTED; IN POWER FAILURE; WAY ON; EXIT; NO EXIT). The 32.5° arm is retired | Nothing in the prop depends on the glyphs. Mesh and UVs unchanged |
| Glow mask = the legend glyphs (1.81 % of the sheet) | Mask = the head, the labels and the one turned or flattened band per swatch (**0.47 %**). The placard's own overprint per A.12, **independent of the walls' §8 option A/B**, still gated by the lamp | `FrontRoomsPlacardGlow` doc updated. The kill switch `Enabled` stays, but nothing sets it now (it no longer follows wall option B) |
| `legend_centre` anchor (−0.1374, −0.0033) from the v1 mask box | v2 mask box: sheet x 300.3–410.7 mm, y 90.5–201.8 mm | `LEGEND_BOX` updated in `evac_placard_common.py`. Anchor now **(−0.1395, −0.0067, 0.00455)**. The lookdev reads it from the sidecar |
| Input md5s print 7b96ea08…, mask 5ee79d13… | v2: print **e39befc6…**, mask **c7e22e2f…** | `pack_evac_plan.py` records v2 as the verified input (v1 md5s kept in its comment) |
| Footer `IN CASE OF FIRE DO NOT USE ELEVATORS` (finding F1) | `IN CASE OF FIRE: WALK, DO NOT RUN.` / `CLOSE DOORS BEHIND YOU.` | F1 is closed by 平面视觉 |
| Glyph ink by day #D6DEBD ≈ paper | The glowing bands are printed in the paper's grey and pink: by day they are **0.81** of the paper's luminance | This lowers dark contrast (§5.4) |

Also open, not for the prop: `NOTE_s8_option_A_hides_flow_cue.md` (option A would hide the wall's FLOW cue).

## 3. The kit

### 3.1 Files and how to build

Modules (main): `Tools/Blender/frontrooms_kit/assets/evac_placard.py`, `evac_placard_lens.py`, `evac_placard_common.py` (helper, no NAME).

```
/Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup --python-expr "import sys; sys.dont_write_bytecode = True" \
  --python ".../Frontrooms3D/Tools/Blender/frontrooms_kit/build_asset.py" -- evac_placard evac_placard_lens \
  --out-root /Users/redwang/FrontRoomsVisualWork/proj_placard --preview-dir /Users/redwang/FrontRoomsVisualWork/placard_prev/rA
```
`dont_write_bytecode` keeps `.pyc` noise out of Red's tree (the codex audit flagged it). Exit 0; all §2.8 asserts pass.

### 3.2 Parts and triangles (LOD0)

| Part | Tris | Slot | Notes |
|---|---|---|---|
| rail top | 380 | Prop_Aluminium | §2.3 profile, 48 points (bullnose 16, face 20, nose 8), both ends mitred 45° and chamfered 0.25 mm |
| rail bottom | 572 | Prop_Aluminium | + 2 rings at ±60 mm for the thumb wear |
| rail left / right | 380 / 380 | Prop_Aluminium | |
| base strip | 40 | Prop_Aluminium | the outer strip below the hinge seam, swept with 45° corners |
| printed sheet | 2 | Prop_EvacPlan | w 4.55 mm, UV v 1/1676 … 1675/1676 |
| **LOD0** | **1,754** | | +9.6 % on 1,600 (inside ±15 %) |
| LOD1 (hand-built) | 106 | | one 14-point sweep + the sheet |
| LOD2 (hand-built) | 26 | | one 4-point sweep + the sheet |
| Lens (own asset) | 10 | Prop_LensNonGlare | top at 5.55 mm, edges down to 4.60, no bottom face |

- **Face arc (deviation from §2.3, kept):** with the crown level and the R 0.8 nose fixed, "tangent at both ends" gives R **34.22 mm** (centre (5.00, −20.72)), not "about 29.6". It meets the nose 35.9° below horizontal ("about 40" in the spec).
- **Shading:** every profile point carries the curve's true normal (custom split normals), so LOD0/1/2 shade from one curve. Chamfer rings are doubled so the band normals never merge into the chamfer fan.
- **LOD status:** `kitlib.py` still has only `make_lod1` (P-1 not approved). So, per the interactables convention, LOD1/LOD2 parts are built only when `Kit.make_lods` exists and the FBX holds LOD0. One placard per run: the cost is 1,754 tris and 2 draws (+1 lens). The sidecar already carries `lodDistances` 2 / 6 / 30 m, which today's importer (663e858) honours once the FBX has LODs.
- **Screws/standoffs:** not modelled (spec D7): a closed snap frame hides its 4 screws under the rails, and snap frames sit flat (no standoffs). Their positions are anchors (`screw_0…3`).

### 3.3 Review rounds

| Round | When | What changed | Evidence |
|---|---|---|---|
| r1 | 2026-10-03 17:3x | first build: profile, mitres, seams, lens, LODs | lost in the wipe |
| r2a | 22:0x | true analytic normals on the profile (r1's LOD1 shaded its 5-segment face differently and popped at 2 m) | lost |
| r2b | 22:2x | doubled chamfer rings (r2a showed a light band along every rail above the lip); each base-strip side its own strip (r1's LOD1 "pillow" corners) | lost |
| r3 / r4 | 22:5x – 10-04 08:2x | final geometry; FBX round trip clean | lost |
| **rA** | **2026-10-07 16:39–17:08** | **this run:** rebuilt from main's modules + v2 art; re-reviewed every view: front, ¾, the player's 0.3 m view, corner, mitre, section, section 3D, lip, LOD0/1/2 at 2 and 6 m, LOD corners, 45°/60° oblique, glow | `images/q16_b_*.jpg` |

rA verdicts (Cycles, wall + one troffer-like light; images in `images/`):
- **Profile and corners:** bullnose highlight smooth at 0.3 m (`q16_b_game03`, `q16_b_corner`); mitre and hinge seams read as fine lines (`q16_b_mitre`). PASS.
- **Lens edge:** the lip nose rests on the lens, no gap, no visible lens edge (`q16_b_lip`, `q16_b_section3d`). PASS.
- **Proportions:** 25.4 mm rail on a 467 mm frame; 3.7 mm of paper shows outside the printed rule (`q16_b_front`). PASS.
- **LODs:** LOD1 matches LOD0's shading at 2 m; LOD2 is flat-topped and has a 1 mm gap at the lip, both sub-pixel past 6 m (`q16_b_lod_corners`, `q16_b_lod_sheet`). PASS (not shipped yet).
- **Glow (v2 mask):** head, three bands and labels glow pale yellow-green (`q16_b_glow_v2`). PASS.

No geometry change was needed in rA. The two in-engine rounds (U1, U2, §5) were the iterations this run needed.

### 3.4 Sidecar

Tags `placard`, `sign`, `wall_flush` (not `wall_unit`/`wall_decor`). No collider. Anchors (Unity part space, m):

| Anchor | Position |
|---|---|
| `back` | (0, 0, 0) |
| `face` | (0, 0, 0.00455) |
| `face_dir` | (0, 0, 0.10455) |
| `lens_top` | (0, 0, 0.00555) |
| `legend_centre` | (−0.1395, −0.00667, 0.00455) (art v2) |
| `screw_0…3` | (±0.150, ±0.1459, 0.0012) |

Meta `placard`: sheet 0.432 × 0.279, sight 0.416 × 0.263, frame 0.4668 × 0.3138, depth 0.0135, `slotPrint` Prop_EvacPlan, `uvV` [1/1676, 1675/1676], face arc R 34.224 mm.

### 3.5 Three-view (Unity, for 平面视觉's kit sheet)

Run 2026-10-07 17:46–17:47 in the clone: `FrontRoomsPlacardLookdev.RunBatch -placardSimOnly -placardSeeds 100 -placardThreeView -threeViewOut W/placard_work/threeview -threeViewOnly Kit_EvacPlacard,Kit_EvacPlacardLens` (the lookdev now calls the shared `FrontRoomsThreeView.RunBatch` by reflection). 4,000 px/m PNGs, alpha.

| Asset | Size (m) | Tris | Slots | Views |
|---|---|---|---|---|
| `Kit_EvacPlacard` | 0.4668 × 0.3138 × 0.0135 | 1,754 | Prop_Aluminium, Prop_EvacPlan | front 1900 × 1288, side 86 × 1288, top 1900 × 86, ¾ 1152 × 864 |
| `Kit_EvacPlacardLens` | 0.432 × 0.279 × 0.0009 (top at 5.5 mm) | 10 | Prop_LensNonGlare | the same four; alpha ≤ 10/255, as it should be |

Review:
- **Front:** the v2 sheet sits square in the 416 × 263 mm sight; the printed rule shows 3–4 mm inside the lip on all four sides; the legend swatches, the IN POWER FAILURE head and the two-line footer read. The top rail's lower face is darker than the bottom rail's: the convex face arc lit from above, as expected. PASS.
- **Side and top (×4 strips):** flat back, the hinge seam as one fine line, the bullnose at the front; depth 13.5 mm. PASS.
- **¾:** mitred corners close; the silhouette matches the Blender review (§3.3). PASS.
- The three-view manifest says `placement: Floor`: that is the tool's default field, not the placard's mount. The sidecar tag `wall_flush` is what the game reads.

Files: `W/placard_work/threeview/*.png` + `manifest.json` (working copies); slide copies in `images/q16_tv_front.jpg`, `q16_tv_persp.jpg`, `q16_tv_profile.jpg` (flattened on #E4E2DD; the profile strips are ×4 Lanczos crops of the top and side views).

## 4. Materials and textures (clone)

| Asset | Settings | GUID (clone, new) |
|---|---|---|
| `Surfaces/Textures/Prop_EvacPlan_A.png` | RGB of the v2 print, 2592 × 1676 (one edge row top and bottom; rows 1–1674 byte-equal to the art). sRGB, CompressedHQ (BC7), no NPOT rescale, clamp | a46c31881f6594e9b88e6b55d2e3697e |
| `Surfaces/Textures/Prop_EvacPlan_E.png` | the mask as grey RGB, **linear**, Compressed (BC1), no NPOT rescale, clamp | 9ef8f2e31869f4601bf45b79e43ed07e |
| `Surfaces/Prop_EvacPlan.mat` | FrontRooms/Surface, smoothness 0.15, macro tone 0, dirt 0.02, `_EMISSION` on, emission map `_E`, `_EmissionColor` black | 0cfd51a38ad384cf1b3737c9ec6f6b8d |
| `Surfaces/Prop_LensNonGlare.mat` | URP Lit transparent, base (0, 0, 0, 0.04), smoothness 0.55, preserve specular (`_ALPHAPREMULTIPLY_ON`, Src One), ShadowCaster and DepthOnly off | f2e52cfd329764516a815a1b5b901e9e |
| `Props/Models/Kit_EvacPlacard.fbx` / `.json` | material remaps Prop_Aluminium, Prop_EvacPlan (after a forced re-import once the materials existed) | 9422a5e2b89a940cfb03eacb688a6ae6 / 82d65f55861124b4087b9265df8e0e63 |
| `Props/Models/Kit_EvacPlacardLens.fbx` / `.json` | remap Prop_LensNonGlare | 0d394adff2bf447e4baf2f33656d2d96 / 37c4f31b90bad4428a5b19cba6be26c5 |

- Pack: `python Tools/lookdev/pack_evac_plan.py` (clone). Output md5: `_A` 835c650b…, `_E` 34f02b94…; mask cover 0.47 %.
- In-engine check (lookdev report): the frame's print slot is the per-placard instance `Prop_EvacPlan (Placard)`, `_EMISSION` True, base map 2592 × 1676; every placard renderer has shadows Off.
- WebGL import tab: not touched (WebGL track, spec §6).
- **Order note for promotion:** import the textures and run *Set up* before the FBX import, or re-import the FBX after, so its `.meta` gets the Prop_EvacPlan/Prop_LensNonGlare remaps.

## 5. The glow

### 5.1 Behaviour (code as built)

`FrontRoomsPlacardGlow` reads the placard cell's **logical** lamp level `map.LampLevel(cell)` and its 4 neighbours (NoLamp = 0), the temperament `LampModeOf` once a second, and does nothing until `IsBuilt(cell)`. Per frame (`FrontRoomsPhosphorCell.Tick`):

| Step | Rule | Normal | Reduce flashing |
|---|---|---|---|
| Wall light | W = min(1, L + 0.2 Σ neighbours) | | |
| Lamp low-pass | Ls → L | τ 0.35 s | τ 1.0 s |
| Failing cell reads | Ls | | 3 s box mean, then a 6 s low-pass |
| Charge | rises to W with τ 2 s; decays C₀/(1 + C₀t/8) | | |
| Gate | smoothstep(0.55, 0.15, read) | | |
| Target | C · gate; 0 under a Stutter lamp | | |
| Peak hold | the target is held at its peak | 0.6 s | 1.5 s |
| Slew | shown G moves at most | 2.0 /s | 0.5 /s |
| Emission | `_EmissionColor` = (0.5705, 0.8900, 0.2957) × k × G, linear, `SetVector`, one material instance per placard, written only on a change > 1/1024 | | |

- Freezes on `relay.Caught`. Subscribes to `FrontRoomsSettings.Changed`. Destroys its instance in `OnDestroy`; the shared material is never touched; no MaterialPropertyBlock.
- **Changes from the spec, kept from the 2026-10-03 build:** the peak hold (the spec's slew alone gave 5 pulses at 3 Hz on a Warn train) and the 6 s low-pass on the calm failing read (the 3 s box alone left a 0.21 swing).
- Kill switch `FrontRoomsPlacardGlow.Enabled`; lookdev hooks `Preview(material, g)` and editor-only `ForceLevel`.

### 5.2 Simulation (the component's own code, the map's lamp curves, 60 fps, 2 lit neighbours)

| Case | Normal | Reduce flashing |
|---|---|---|
| Steady | max G 0.000: PASS | 0.000: PASS |
| Stutter | 0.000: PASS | 0.000: PASS |
| Failing | breathes 0.55 turns/s, swing 0.80, max dG/dt 2.0/s: PASS (P2 ≤ 3) | mean 0.095, 2 s window 0.034 (≤ 0.05), 0.033 turns/s, 0.5/s: PASS |
| Dead (blinks) | G(10 s) 0.385 ≈ W 0.392, no dip in blinks: PASS | same, 0.5/s: PASS |
| Dim | G 0.198: PASS | 0.196: PASS |
| Off (A4) | G(10 s) 0.385: PASS | 0.385: PASS |
| Warn train ×6 at 3 Hz | one swell, max G 0.196: PASS | one swell, max G 0.556: PASS |

Curves: `images/q16_sim_glow_curves.png`. Note: A5's wording "breathes ≤ 0.5 Hz" is 10 % over (0.55): the lamp itself runs at 0.49 Hz and its dropouts add turns. Photosafety (≤ 3 /s) holds by a wide margin.

### 5.3 Peak emission k (§4.4), measured

Linear HDR, post off, the legend at 0.6 m, a Steady Level 0 cell with two lit neighbours:
- Lit paper L_lit = **0.738**. The spec's desk formula gives k = 0.12 × 0.738 / 0.779 = 0.114, but in engine that adds only 8.6 % (the mask's anti-aliased edges and the lens take the rest).
- Measured: the full glow adds **0.755 k** of linear luminance over the glowing pixels. So **k = 0.158** puts the full glow at **12.0 %** of the lit sheet, the walls' cap (LD §9). Set in `FrontRoomsPlacardGlow.PeakEmission`.

| k | A4 glyph/paper (G 0.389) | G 1 | Failing Ls 0.3 | Full glow / L_lit |
|---|---|---|---|---|
| 0.114 (spec formula) | 1.117 | 1.576 | 0.932 | 0.086 |
| 0.130 (spec estimate, U1) | 1.159 | 1.683 | 0.949 | 0.098 |
| **0.158 (U2, built)** | **1.231** | **1.869** | **0.977** | **0.120** |

### 5.4 FLAG: the two legibility bars do not fit under the cap with art v2

- **A4 bar 1.30:** needs k ≈ 0.185, which puts the full glow at **14 %** of the lit sheet (2 points over the cap).
- **Failing bar 1.15 at Ls 0.3:** needs k ≈ 0.33 (25 %). A half-lit own lamp outshines any phosphor; that bar cannot be met.
- **Why:** in the A4 cell the two lit neighbours still light the paper to 11.5 % of lit, and the v2 bands are printed darker than the paper (0.81).
- **Where it reads well:** right after the own lamp dies, and in cells with dark neighbours.
- **Decision for Red:** keep the 12 % cap (built), or allow 14 % (k 0.185) so A4 reaches 1.30. A one-number change.

## 6. Placement (§5)

100 seeds, the game's `MapRootFor` by reflection, door at x 256.5:
- P1 west 61, P1 east 26 → **P1 87 %**; P2 west 4, P2 east 5 → **P2 9 %**; P3 west 4 → **P3 4 %**.
- Side edges seen: Open/Open 40, Wall/Open 18, Open/Arch 12, Open/Wall 9, Arch/Open 8, Arch/Wall 5, Arch/Arch 4, Wall/Arch 3, Wall/Wall 1.
- Seeds for the A9 shots: P1 = 1, 2, 4; P2 = 5, 20, 28; P3 = 3, 41, 50.
- Per-seed table (mount, side, edges, door and glow cells, glow-cell temperament, position): `placement.csv` in this folder (re-run 2026-10-07 17:45 in the clone, same counts as above; 100 rows).
- **CONTRACT Q16-1** (`KeepClearAtStart`, map-owned): not applied. The mount calls it by reflection only if the map has it, so today it is a no-op and furniture can still land in front of P2/P3. Note: narrative A.12 gives placement to 关卡设计; spec §5.3 option B (move the resolver into the map) is still open.

## 7. In-engine captures (clone, `FrontRoomsPlacardLookdev`, 1920 × 1080, 4× MSAA, game post, Level 0 room)

| Shot | Verdict | Image |
|---|---|---|
| A1 0.3 m face-on | PASS. Bullnose smooth, seams fine lines, `EVACUATION PLAN` reads left to right, the rule 3–4 mm inside the lip, lens sheen barely there | `q16_u1_A1_face_0p3m.jpg`, `q16_u1_A1_corner_0p3m.jpg` |
| A1 control without the lens | the same picture: the lens does not veil | `q16_u1_W_nolens_0p3m.jpg` |
| A2 0.3 m at 60° | PASS. A soft smear, no mirror image, the paper is not veiled | `q16_u1_A2_60deg_0p3m.jpg` |
| A3 1.5 m lit; legend 0.6 m | PASS. The legend shows faintly in its pale ink, no glow | `q16_u1_A3_lit_1p5m.jpg`, `q16_u1_A3_legend_0p6m.jpg` |
| A4 own lamp off, 2 lit neighbours | glow visible but faint at 1.5 m; reads at 0.6 m. FLAG §5.4 | `q16_u2_A4_dark_1p5m.jpg`, `q16_u2_A4_legend_0p6m.jpg`, `q16_u2_legend_lit_k013_k158.jpg` |
| A8 2 m, 6 m | PASS. Silhouette holds; LOD0 only, so no pop | `q16_u1_A8_walk_2m.jpg`, `q16_u1_A8_walk_6m.jpg` |

**Observation (not changed, shared slot):** `Prop_Aluminium` reads as dark, slightly grimy pewter at 0.3 m in Level 0 (it reflects the yellow room, and its macro dirt shows as smudges). It is the shared slot of every aluminium kit, so this build does not change it. If Red wants a cleaner "recently hung" frame, the fix is a placard-only slot (e.g. `Prop_AluminiumClear`, macro dirt 0) through the same slot path.

**Left for the render stage:** A5–A7 as in-engine clips, A9 (seeds above, in game, door open and shut) and A10 (WebGL). The 平面视觉 three-view is done (§3.5).

## 8. Every change, for promotion

| File (clone path = main path) | Change | Owner / approval |
|---|---|---|
| `Tools/Blender/frontrooms_kit/assets/evac_placard_common.py` | `LEGEND_BOX` → art v2 (already in main, 5e6e7f9) | visual |
| `Assets/Resources/Props/Models/Kit_EvacPlacard.fbx/.json`, `Kit_EvacPlacardLens.fbx/.json` (+ `.meta`) | new (generated) | visual |
| `Tools/lookdev/pack_evac_plan.py` | new | visual |
| `Assets/Resources/Surfaces/Textures/Prop_EvacPlan_A.png`, `_E.png` (+ `.meta`) | new (generated) | visual |
| `Assets/Resources/Surfaces/Prop_EvacPlan.mat`, `Prop_LensNonGlare.mat` (+ `.meta`) | new (generated by *Set up*) | visual |
| `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs` | 1 SurfaceDef row, 1 GlassDefs row, the lens blend/shadow block, 1 importer rule | visual, **NEEDS APPROVAL** |
| `Assets/Scripts/Rendering/FrontRoomsPhosphor.cs`, `FrontRoomsPlacardGlow.cs`, `FrontRoomsPlacard.cs`, `FrontRoomsPlacardMount.cs` (+ `.meta`) | new | visual |
| `Assets/Scripts/FrontRoomsRoomStream.cs` | one `FrontRoomsPlacard.Prepare(...)` call at the end of `EndStreamAt` | visual |
| `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs` | `KeepClearAtStart` (spec §5.4) | **CONTRACT Q16-1**, map chat |
| `Assets/Editor/Rendering/FrontRoomsPlacardLookdev.cs` | clone tool only, never main | — |

- **Patch script for the two edited files:** `promote_src/apply_placard_patches.py.txt` (`python3 apply_placard_patches.py <project> [--dry-run]`). Idempotent; every anchor must match once. Dry-run against main a5262fb: all 5 edits apply (re-run 2026-10-07 18:2x: same; neither file is modified in Red's working tree).
- **Bases (main, 2026-10-07 17:2x):** RenderSetup md5 eb5e4d4b… (last commit 8ef5b64), RoomStream md5 0eb71527… (fc9e2ca).
- **Code copies** (so a wipe cannot take them again): `promote_src/*.cs.txt`, `pack_evac_plan.py.txt`, the two `.diff` files, and the Blender review tool `review_blender.py.txt`. The four runtime `.cs.txt`, the pack script and the apply script were re-compared byte for byte with the clone at 18:2x; `FrontRoomsPlacardLookdev.cs.txt` was refreshed (it gained the `-placardThreeView` hook at 17:4x).
- The `.meta` GUIDs in §4 are new: main has none of these paths. Script GUIDs (clone, new): `FrontRoomsPhosphor.cs` 392b8fec8140e47a7a8c6546b1d14d0b, `FrontRoomsPlacardGlow.cs` e94b78d55f1de48ee84ea20b9f0f6fd6, `FrontRoomsPlacard.cs` 74ab23ac6066944c88f747670a6e88ab, `FrontRoomsPlacardMount.cs` 765915f1ff2654b8d9ae622a3a2873a6.

## 9. Codex audit

`codex_audit/20_findings.md` does not exist (re-checked 2026-10-07 18:2x). `00_main_state.md` lists the placard as "not touched" by Codex, and `10_review_docs.md` / `RED_DECISIONS.md` only note the stray `evac_placard*.cpython-311.pyc`. Those three files date from 2026-10-03 22:15–22:49 (the first build) and are now tracked in main; this run wrote none (`dont_write_bytecode`). They are stale bytecode, harmless (Python checks the source time), and left for whoever cleans the `.pyc` noise; not deleted here. No finding to resolve for this track.

## 10. Verification images

Saved in `images/` (JPG q85 ≤ 1920 wide, PNG for the curves). Figma slides in FRONTROOMS · VISUAL VERIFICATION LOG (`2595:6093`), rows in `../../VERIFICATION_LOG.md`:

| VL | Node | Check | Verdict | Images |
|---|---|---|---|---|
| VL123 | 2778:6099 | Placard frame at 0.3 m (Blender rA) | PASS | `q16_b_game03`, `q16_b_corner`, `q16_b_mitre`, `q16_b_section`, `q16_b_lip` |
| VL124 | 2778:6122 | Placard in engine, lit | PASS | `q16_u1_A1_face_0p3m`, `q16_u1_A1_corner_0p3m`, `q16_u1_A2_60deg_0p3m`, `q16_u1_W_nolens_0p3m`, `q16_u1_A8_walk_6m` |
| VL125 | 2778:6145 | Placard glow under the cap | FLAG | `q16_u2_A4_legend_0p6m`, `q16_u2_legend_lit_k013_k158`, `q16_sim_glow_curves` |
| VL127 | 2783:6099 | Placard kit three-view | PASS | `q16_tv_front`, `q16_tv_persp`, `q16_tv_profile` |

The cover was recounted from the canvas after VL127: 124 placed checks, 398 images, 18 tasks; Q16 legend "VL123–125, 127 · 4 checks".
