# Level transitions · 15 · V5 Light lead: pre-render report

Date: 2026-10-03. Variation key `lightlead` (plan `10_transition_plan.md` §3 V5, §7.5).
Everything here was built and rendered in a private clone. Nothing in Red's project changed except the files listed in §12. No downloads and no Figma writes.

## 0. Short answer (for Red)

- **The idea works as briefed.** At every frameless crossing (open edge, arch) the Level 0 cell goes dark. The Office beyond is lit in its own cool white. The surface cut now sits where the light changes, and mostly in shadow.
- **Contrast flips.** Before, the Level 0 side was *brighter* than the Office seen through the opening (shot1: Office 72 vs Level 0 114 luma). After, the Office is 1.6x to 2.2x brighter than the dark Level 0 cell (shot1: 70 vs 43; shot2: 83 vs 38).
- **The Office reads cool.** Blue/red ratio on Office walls rises from 0.80-0.98 to 0.97-1.19 in every shot. Level 0 stays warm (0.49-0.63).
- **"Brighter" needs a footnote.** Office lamps are 9 % brighter, but Office areas next to a border measure 1-7 % *darker* than before. They lost the spill from the Level 0 lamp that is now dead. The Office is brighter *relative* to the Level 0 side, not in absolute terms.
- **Doors and windows are subtle.** The dim rule (shot3) makes the Level 0 Low room 10 % darker. It stays warm. It reads, but quietly.
- **It hides, it does not fix.** Close up, the split arch jamb and the flat seams are still there (see `images/var_lightlead_close_arch.jpg`). They are now in shadow.
- **B0 is in and works.** Corner posts no longer z-fight. In shot4 the grey stripe on the yellow corner is gone (`images/var_lightlead_close_post.jpg`).
- **Cost is near zero.** 0 extra triangles from the light. 0 extra draws. 2-5 fewer lit lights near the eye. 0 MB textures. One new material (2 KB). B0 adds about 1.3 % triangles (two skins on height-border walls).
- **Gameplay flags.** 2.0 % of Level 0 cells go dead, 2.8 % go dim. The Relay's lamp warning cannot show in a dead cell. The phosphor ink gate would open fully in every dead border cell. Both need a decision (§9).
- **My read:** keep the cool Office colour and lens (cheap, exact for 1990, reads in every frame). Use the dead-cell rule sparingly, or use `lightlead_soft`, so darkness does not become a tell. This matches the provisional pick in `10_transition_plan.md` §8.

## 1. Images

All 1920 px wide, JPG q85, in `images/`.

| File | What |
|---|---|
| `var_lightlead_shot1.jpg` .. `shot4.jpg` | The four fixed shots, harness output unchanged (same cameras as `shot<k>_before.jpg`). |
| `var_lightlead_sheet.jpg` | Four rows, BEFORE left, LIGHT LEAD right. |
| `v_lightlead_shot1.jpg` .. `shot4.jpg` | Byte-identical copies of `var_lightlead_shot1..4` (the workflow asked for both names). |
| `v_lightlead_shot5.jpg` | Extra 1: from a lit Level 0 cell, through the dead border cell, to the Office open edge on X = 798. |
| `v_lightlead_shot6.jpg` | Extra 2: from the Office, back through the shot1 arch into the dead cell and the warm Level 0 beyond. |
| `v_lightlead_vs_before.jpg` | Six rows: BEFORE, LIGHT LEAD, SOFT (dim instead of dead), for the four shots and the two extras. |
| `var_lightlead_close_post.jpg` | B0.1 close frames: post (5,7) seed 20388, before and after, plus zooms and the shot4 corner. |
| `var_lightlead_close_arch.jpg` | `cut_arch_2` re-rendered: the arch from the Office side, with the dead Level 0 cell behind it. |

### What each shot had to show

| Shot | Brief | Result |
|---|---|---|
| shot1 | Eye cell (264,202) dark; Office beyond both arches cool and brighter; each arch a lit opening seen from shadow | Yes. Eye-cell walls -62 % luma. Both Office openings cool (blue/red 0.98 -> 1.19 and 0.81 -> 0.98). Each arch reads as a lit opening. |
| shot2 | (255,194) and (255,195) dark; Office corridor past Z = 588 lit cool | Yes. Near Level 0 walls -64 %. The corridor past Z = 588 is cool (0.80 -> 0.97). The open edge on X = 765 at left also opens onto cool light. |
| shot3 | Office cell steady and cool; Level 0 Low room through the door dim and warm | Yes, but quiet. Office walls cool (0.85 -> 1.04), steady. Level 0 through the door -10 %, still warm (0.66). The dim lens (level 0.42) still blooms near white. |
| shot4 | (264,202) and (265,201) dark; near Level 0 lit warm; Office cool in the distance | Yes. Both lenses read dead. Wall round the far arch -30 %, wall by (265,201) -39 %. Near room lit warm (-10 %, from the two dead neighbours). Office at right cool (0.69 -> 0.82). |

All lamps are frozen at clock 0. Dead lamps (mode 3) are dark at clock 0: their first blink is 2-16 s away. Dim lamps sit at 0.42 ± 0.03.

## 2. What was built

There is no geometry change and no surface change besides B0.

1. **Lamp colour per theme.** `ThemeMaterials.lampColor`; `BuildFixture` sets `light.color` from it.
   - Level 0: (1.00, 0.96, 0.88) at 5.0 (unchanged).
   - Office: (0.90, 0.96, 1.00) at 6.0 (was 5.5, so +9 %).
2. **Cool Office lens** `Resources/Surfaces/Troffer_Lens_Cool.mat`: a copy of `Troffer_Lens` with emission (1.989, 2.121, 2.210). Same luminance as the original (2.200, 2.100, 1.800): both 2.100. Same textures. The Level 0 lens is unchanged.
3. **Border lamp rule** in `BuildFixture`, after the tier roll, Auto lamps only (module lamps keep theirs). It uses `Cache.Edge` and `ZoneOf` on the four neighbours. The start area counts as wall.

| Cell | Rule | Mode |
|---|---|---|
| Level 0, with an Open or Arch edge into Office | frameless crossing | 3 dead (rare blink stays) |
| Level 0, whose only crossings into Office are doors or windows | framed crossing | 4 dim |
| Office, with any crossing into Level 0 | any crossing | 0 steady |
| Wall-only contact | — | its own roll |

4. **`lightlead_soft`** (optional tag, rendered): every Level 0 border cell dim (mode 4) instead of dead.
5. **B0.1 corner ownership.** At a corner post where pieces of different finish meet, no two pieces overlap in the 0.16 x 0.16 m post square. A finish is the face's material plus its ceiling class (so a grime band at a different height counts too).
   - Straight run (collinear pair): both pieces stop on the cell line, each covering its half of the post.
   - Every perpendicular piece stops at the post face.
   - L corner: the owner's **outside skin** wraps the whole post; its inside skin stops at the post face; the other piece runs to the cell line. Owner = the piece whose outside face is Office; on a tie, the east-west piece.
   - Free wall ends and one-finish corners are unchanged.
6. **B0.2 grime band per side.** Each face of a height-border wall is built in its own side's height-class block, so `_CeilingHeight` is its own ceiling. Same-material height borders now get two 0.08 m skins.

### Iterations

| Round | Change | Why |
|---|---|---|
| r1 | All of the above, B0.1 per piece (the owner piece extended both skins over the post) | First pass, as written in the brief |
| r1 check | Close frame `ev_post_20_Z`: z-fight gone, but a clean 0.08 m strip of the *inside* paper showed on the outside corner (the owner's split end cap) | Shot4's yellow corner at right of centre still showed a thin grey strip |
| r2 | B0.1 per skin: the owner's outside skin wraps the post, the inside skins meet in the inside corner | The corner now reads as one finish wrapping round. shot1-3 unchanged byte for byte; shot4 changed only at that corner |
| r2 | Extra shots moved: a corridor view 6 m back (r1) changed little (the far corridor was already dim), so extra 1 now looks along row 201 into the dead cell; extra 2 moved 3.3 m closer to the arch | Show the idea more directly |
| r2 | `lightlead_soft` re-rendered with the r2 B0 | Same B0 in every frame |

## 3. Measurements

### 3.1 Lamp lines (harness `.txt`) and lights within 46 m

| Shot | Before: lit / shadowed | Light lead | Soft |
|---|---|---|---|
| shot1 | 84 / 8 | 81 / 7 | 85 / 8 |
| shot2 | 82 / 7 | 77 / 6 | 83 / 7 |
| shot3 | 83 / 7 | 79 / 7 | 83 / 7 |
| shot4 | 88 / 7 | 86 / 7 | 89 / 7 |

Within 46 m of the eye (own stats method, all non-directional lights) the numbers are the same as the harness lines. Light lead has 2-5 fewer lit lights. Soft has 0-1 *more*: a rolled-dead border lamp becomes dim, and rolled stutter or failing Office border lamps become steady.

### 3.2 Picture (sRGB luma 0-255, region means; blue/red ratio for colour)

| Shot | Region | Before | Light lead | Soft | Blue/red before | after |
|---|---|---:|---:|---:|---:|---:|
| shot1 | Level 0 eye cell, left wall | 114 | 43 (-62 %) | 77 (-32 %) | 0.57 | 0.62 |
| shot1 | Office through arch Z 609 | 72 | 70 (-3 %) | 72 (-1 %) | 0.98 | 1.19 |
| shot1 | Office through arch X 795 | 124 | 115 (-7 %) | 120 (-3 %) | 0.81 | 0.98 |
| shot2 | Level 0 cells 194-195, right wall | 107 | 38 (-64 %) | 73 (-32 %) | 0.52 | 0.53 |
| shot2 | Office corridor past Z 588 | 87 | 83 (-5 %) | 86 (-1 %) | 0.80 | 0.97 |
| shot3 | Office walls | 71 | 73 (+2 %) | 73 (+2 %) | 0.85 | 1.04 |
| shot3 | Level 0 Low room through the door | 117 | 104 (-10 %) | 104 (-10 %) | 0.63 | 0.66 |
| shot4 | Near Level 0 floor | 125 | 112 (-10 %) | 118 (-5 %) | 0.54 | 0.53 |
| shot4 | Wall round the far arch, cell (264,202) | 94 | 66 (-30 %) | 80 (-15 %) | 0.53 | 0.53 |
| shot4 | Level 0 wall by (265,201) | 114 | 70 (-39 %) | 95 (-17 %) | 0.49 | 0.43 |
| shot4 | Office past X 798 | 107 | 106 (-1 %) | 109 (+2 %) | 0.69 | 0.82 |
| extra 1 | Office through the centre opening | 89 | 107 (+20 %) | — | 0.67 | 0.86 |
| extra 2 | Office walls | 90 | 90 (0 %) | — | 0.84 | 1.02 |
| extra 2 | Level 0 through the arch | 105 | 93 (-11 %) | — | 0.63 | 0.65 |

### 3.3 The rule, per chunk

Over the 25 built chunks of each fixed-shot build (two distinct builds):

| Build round chunk | Auto lamps (L0 / Office) | Dead | Dim | Office steady | Level 0 cells dark (mode 3), before -> after |
|---|---|---|---|---|---|
| (33,25): shots 1, 4 | 1594 (1282 / 312) | 37 (1.48 per chunk) | 40 (1.60) | 74 (2.96) | 61 -> 95 of 1282 (4.8 % -> 7.4 %) |
| (31,24): shots 2, 3 | 1591 (1289 / 302) | 33 (1.32) | 39 (1.56) | 71 (2.84) | 63 -> 95 of 1289 (4.9 % -> 7.4 %) |

Over 20 seeds x 81 chunks (1620 chunks, generator only, the same sample as `03`):
- Dead 1.08 per chunk, dim 1.52, Office steady 2.49.
- Forced dead: 2.0 % of Level 0 cells (1.7 % of all lamps). Forced dim: 2.8 % of Level 0 cells (2.4 % of lamps).
- 56.1 % of chunks have at least one lamp the rule sets.
- The plan's estimate was 1.1 dead and 1.6 dim per chunk: confirmed.
- In the built chunks only 2 of the 70 forced-dead cells were already dead by roll, so nearly all of the 2.0 % is new darkness. Dark Level 0 cells rise from 4.8 % to 7.4 % there.

### 3.4 B0 counts (1620 chunks, generator only)

- Corner posts with a wall piece: 55.0 per chunk. Mixed-finish posts: 12.2 per chunk (theme mix 4.9, ceiling class only 7.3).
- Posts B0.1 rebuilds: 12.1 per chunk. Straight run 2.4, T 5.5, cross 2.2, L 2.0 (1.6 of them ties, where the east-west piece owns).
- `03` counted 3.17 visible z-fight posts per chunk (exposed faces only). After B0.1 no two pieces overlap at any mixed post, so that count is 0 by construction.
- Height-border wall edges built as two skins in their own blocks (B0.2): 10.74 per chunk (matches `03` cut L).

### 3.5 Cost

Own stats method (`FrontRoomsLightLeadTools.Stats`), same builds and poses as the harness. "Off" = the same clone with `-transitionsOff` (byte-identical to the frozen base).

| Shot | Renderers <= 46 m: off -> B0 -> light lead | Triangles <= 46 m: off -> B0 -> light lead | In-frustum renderers (draw proxy): off -> light lead | Materials <= 46 m |
|---|---|---|---|---|
| shot1 | 1801 -> 1800 -> 1800 | 175,766 -> 177,890 -> 177,890 (+1.2 %) | 563 -> 559 | 42 -> 42 |
| shot2 | 1779 -> 1793 -> 1793 | 162,760 -> 165,076 -> 165,076 (+1.4 %) | 621 -> 622 | 42 -> 42 |
| shot3 | 1756 -> 1765 -> 1765 | 178,600 -> 180,436 -> 180,436 (+1.0 %) | 565 -> 563 | 42 -> 42 |
| shot4 | 1791 -> 1793 -> 1793 | 172,554 -> 174,702 -> 174,702 (+1.2 %) | 566 -> 561 | 42 -> 42 |

- **The light itself costs nothing:** B0-only and light-lead numbers are identical. All added triangles and renderers come from B0.2 (two skins on height-border walls, sometimes in a new block).
- **Lights:** 2-5 fewer lit, 0-1 fewer shadowed within 46 m. A small gain on desktop and on WebGL.
- **Materials:** the Office lens swaps 1:1 (`Troffer_Lens` -> `Troffer_Lens_Cool`), so the count stays 42.
- **Texture memory added: 0 MB.** The cool lens shares the `TrofferLens` textures. New asset: one 2.1 KB material.
- **Draw calls:** `UnityStats` reads 0 in batch mode, so I report renderers inside the shot frustum as the proxy. Change: -5 to +1.
- **Build time** per 25-chunk build: 190-356 ms with light lead + B0 vs 189-317 ms with everything off (same session). Within noise. B0.1 does four `Cache.Edge` lookups per wall end; the real version should precompute a post table per chunk.
- **WebGL:** nothing new per frame. `TickFixturesNear` is untouched; fewer lit lamps near borders.

## 4. Draft contract (map-file diff against the clone base)

Base: `proj_trans` `FrontRoomsMapWorld.cs` md5 `e5204f9c7a7e07f9fa6d584915e6297e`. Clone after: md5 `89a04f1adfa20eb78f6f77921303eba3`. 111 lines added, 13 removed, all marked `// TRANSITION lightlead`. No other map file changed. The full diff is in the appendix.

| Hook | Where | What |
|---|---|---|
| H1 | `ThemeMaterials` | `public Color lampColor` (default the old warm value) |
| H2 | `BuildMaterials` | Level 0 and Office `lampColor` and `lampIntensity` from the kit; Office lens = `Troffer_Lens_Cool` when present |
| H3 | `BuildFixture` | `light.color = theme.lampColor` |
| H4 | `BuildFixture`, after the tier roll | Auto lamps only: `fixture.mode = BorderMode(rolled, theme, neighbour themes, neighbour edge kinds)`; the roll is still drawn, so no other lamp changes |
| H5 | new `BorderThemes(cell, kinds)` | the four neighbours' themes and edge kinds; the start area counts as wall |
| H6 | `BuildInto` | passes each skin's block (`BlockOf` with that side's ceiling) to `BuildEdge` (B0.2) |
| H7 | `BuildEdge` | per-skin reach at each corner from the kit (B0.1); two skins when blocks or reaches differ |
| H8 | new `CornerReach`, `PostPieceAt` | the four pieces round a post, as `BuildInto` builds them (start-area rules included) |

**Rebase on Red's current file.** Red's `FrontRoomsMapWorld.cs` (read only, mtime 12:38 today, md5 `a05546db9f10dc23aa9b241bca2ce23e`) already has lamp interface v1: `SetLampMode`, `lampModes`, `LampModeOf`, `LampFx`. Recommendation for the real version:
- Put the border rule in `BuildFixture` **after the Auto roll and before the module and `SetLampMode` overrides**, as here. A run-time `SetLampMode` must still win.
- **Mirror the rule in `LampModeOf(cell, withSet)`.** It is the pure predictor of a built lamp's mode. Without the mirror it disagrees with built border lamps, and so do the ink gate and the tests that use it.
- **Do not route the rule through `SetLampMode`.** That call is for run-time kills and promotions. It stores modes by cell for good and wins over module lamps. Using it would fill `lampModes` with every border cell and override module-authored lamps, which the brief excludes.

## 5. New assets and files (clone only)

| Path in the clone | Size | Owner later |
|---|---|---|
| `Assets/Scripts/Rendering/Transitions/FrontRoomsTransitionKit.cs` | 5.3 KB, 119 lines | visual (pure B0.1 corner rule; flags `-transitionsOff`, `-b0Off`) |
| `Assets/Scripts/Rendering/Transitions/FrontRoomsTransitionLightLead.cs` | 3.6 KB, 76 lines | visual (colours, intensities, lens name, the pure border rule; flags `-lightleadOff`, `-lightleadSoft`) |
| `Assets/Resources/Surfaces/Troffer_Lens_Cool.mat` | 2.1 KB | visual (real version: a `SurfaceDef` row in `FrontRoomsRenderSetup`) |
| `Assets/Editor/Transitions/FrontRoomsLightLeadTools.cs` | 17 KB | tools only: `CreateCoolLens`, `Stats` |
| `Assets/Editor/Transitions/FrontRoomsLightLeadPostCounts.cs` | 7 KB | tools only: B0 counts |
| `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs` | hooks H1-H8 | map chat (contract) |

The harness `Assets/Editor/Audit/FrontRoomsTransitionAudit.cs` is unchanged (md5 `76a624e3217630e637bdb381af7ca5b1`). `shots.json` is unchanged.

## 6. Change list (every clone change, and why)

1. `ThemeMaterials.lampColor` (H1): the lamp look per theme; before, both themes used one hard-coded colour.
2. `BuildMaterials` (H2): the V5 values and the cool lens. Level 0 stays as before.
3. `BuildFixture` light colour (H3): reads the theme's colour.
4. `BuildFixture` border rule (H4, H5): the V5 rule; Auto lamps only; the roll is still drawn, so lamps away from borders stay identical.
5. `BuildInto` side blocks (H6): B0.2.
6. `BuildEdge` per-skin reach (H7, H8): B0.1.
7. Two runtime kit files: the visual-owned, pure logic, so the map-side diff stays small.
8. One material: the cool lens.
9. Two editor tool files: asset prep and measurements. Not for the real project.
10. Inert check: with `-transitionsOff` every hook is off. The four frames are byte-identical to `shot<k>_before.jpg` (checked twice, after r1 and r2). So the hooks change nothing until switched on.

## 7. Deviations from the brief

1. **B0.1 collinear pieces.** The brief says straight runs "keep their 0.08 extensions". Two collinear pieces that both keep them overlap over the whole post square, and that is the z-fight on a flat seam. I followed the brief's first rule ("no two pieces may overlap") instead: a collinear pair meets on the cell line, each piece covering its own half. The run still covers the whole post.
2. **B0.1 L corners per skin.** The brief extends the owner *piece*. That leaves its split end cap on the outside corner: 0.08 m of the inside paper (seen in r1). I extend the owner's *outside skin* and stop its inside skin at the post face. Same ownership, no strip.
3. **Finish includes the ceiling class.** B0.2 makes same-material height borders two-skin pieces, so B0.1 treats a different grime-band height as a different finish. This adds the height-only mixed posts (7.3 per chunk) to the rule. Tie at an L: the east-west piece owns.
4. **Draw calls are a proxy.** `UnityStats` reads 0 in batch mode. I report renderers inside the shot frustum.
5. **Two name sets.** The workflow asked for `var_lightlead_*` and `v_lightlead_*` images and for `15_var_lightlead.md` and `20_variation_lightlead.md`. Both exist. `v_lightlead_shot1-4` are byte copies of `var_lightlead_shot1-4`; `v_lightlead_shot5-6` are the extras. `20_variation_lightlead.md` is a short summary that points here.
6. **Wanted media are not in `media_candidates.md`.** The protocol allows no other files in Red's project. The rows this variation needs already exist there (§10).

## 8. Era check (1990)

- Cool-white F40 tubes were the standard US office lamp through the 1970s and 1980s. Warm-white tubes and incandescent were the retail, hotel and home choice. So a cooler office next to a warmer Level 0 is exact for 1990.
- The colour values are look values, not a measured colour temperature. After the game's green-yellow film grade, (0.90, 0.96, 1.00) reads as neutral-cool white, not blue.
- A dead tube in a lay-in troffer (grey diffuser, rare flicker) is period-correct.
- Nothing printed, no trademarks, no new props. Nothing dated after 1990.

## 9. Gameplay notes (for 系统设计 and the map chat)

- **Visibility near borders.** Walls in a dead border cell drop 62-64 % in luma (shots 1-2). Neighbouring lamps still give some light, so the cell is dim, not black. Dead lamps keep their rare blink (0.06-0.18 s every 8-28 s), which briefly lights the border.
- **The Relay's lamp warning (`LampFx.Warn`) cannot show in a dead cell.** Level = temperament x the lowest override multiplier, and a dead lamp's temperament is 0. Dim cells show it at 0.42. Frameless borders are where chases cross zones. **Flag for 系统设计.** Options: (a) let a Warn on a dead border lamp force a blink; (b) use `lightlead_soft` (dim, 0.42, so Warn still shows); (c) keep the colour change only.
- **Darkness becomes a tell.** Every frameless border is dark, so a player can learn "dark cell = zone change ahead". This could help wayfinding, but it is a pattern. Soft is gentler (border walls -32 % instead of -62 %).
- **Phosphor ink (wallpaper chat).** The ink gate opens fully where the lamp level is under 0.15 and partly under 0.55 (`../wallpaper_motion/20_level_design_phosphor.md` §4). Under V5 the ink would be fully visible in every dead border cell and partly in every dim one. That turns borders into ink sites. It is a conflict with the "ink appears where light dies" beat unless the wallpaper chat accepts it.
- **Sound.** A dead lamp has no hum, so frameless borders go quiet as well as dark. It fits the idea; the sound chat should know.
- **Colliders, navigation, Relay paths:** unchanged. Layout freeze holds: every `-plan` file is identical to `logs/shot<k>_plan.json`.

## 10. Research basis

From `01_ip_research.md` (patterns P6 light leads and P7 dark buffer):
- Kane Pixels, *Found Footage*, 2:09-2:21: same paper and carpet, green light, then a dark corridor to a lit doorway. Light is the zone code. 4:44-4:52: a very dark space with a lit area far off (the dark lead-in).
- Kane Pixels, *Found Footage #3*, 34:18-34:28: the white corridor beyond the recess has its own brighter, cooler light.
- Kane Pixels, *Informational Video*, 7:08-7:24: teal and red light codes.
- Kane Pixels, *Static Dead End*, 2:10: a door marks a light zone, not a material zone.
- *The Complex: Found Footage*, 4:46: a dark room with a far lit area.
- *POOLS*: light from the next space spills through first.
- Backrooms Wikidot, Blackout Zones: darkness as its own zone.
- 1990 practice: cool-white F40 tubes in offices, warmer tubes in retail (§8).

Media: none downloaded. The stills this variation needs are already listed in `media_candidates.md`: `lt_kane_ff_green_pocket` (2:08-2:20), `lt_kane_ff3_neck` (34:18-34:30, cooler light beyond), `lt_kane_sde_door_light` (1:56-2:14), `lt_kane_ff_fall_dark` (4:32-4:54), `lt_pools_ladder_stairs` (54:38-54:58). Still wanted: one 1980s fluorescent lamp catalogue page showing Cool White vs Warm White F40 tubes (a primary source for §8).

## 11. What the real implementation needs

**Map chat (contract):**
1. `ThemeMaterials.lampColor` and its use in `BuildFixture` (H1, H3), rebased on Red's 12:38 file.
2. The border rule in `BuildFixture` after the Auto roll, before the module and `SetLampMode` overrides, **and the same rule in `LampModeOf`** (§4). Auto lamps only.
3. B0.1 and B0.2 (H6-H8), ideally with a per-chunk post table instead of four `Cache.Edge` calls per wall end.
4. Tests: lamp-mode distribution and `LampModeOf` tests must include the rule; a B0 test that no two wall boxes of different finish overlap in any post square.
5. A switch in the level profile (off / soft / dead) if Red wants the strength per tier.

**Visual chat:**
1. `Troffer_Lens_Cool` as a `SurfaceDef` row in `FrontRoomsRenderSetup` (emission (1.989, 2.121, 2.210), texture `TrofferLens`).
2. The colour and intensity values (the lamp look), then a check of the Office post grade (`FrontRoomsPost_Office`) so its white balance does not cancel the cool shift.
3. Optional: a dim-lens look (the 0.42 lens still blooms near white, so dim cells read weaker than they are).

**Others:** 系统设计 (Relay Warn in dead cells), wallpaper chat (ink gate at border cells), sound chat (no hum at frameless borders).

## 12. Outputs in Red's project

Only these, all under this folder: `15_var_lightlead.md`, `20_variation_lightlead.md`, and in `images/`: `var_lightlead_shot1-4.jpg`, `var_lightlead_sheet.jpg`, `var_lightlead_close_post.jpg`, `var_lightlead_close_arch.jpg`, `v_lightlead_shot1-6.jpg`, `v_lightlead_vs_before.jpg`.

## 13. Open issues

1. Pick dead, soft or colour-only (Red).
2. Relay Warn in dead cells (系统设计).
3. Ink gate at border cells (wallpaper chat).
4. Close up, the arch jambs and flat seams are still split. V5 only puts them in shadow. Pair it with V1 Frame or V3 Neck to fix them.
5. Free wall ends (cut E, 0.17 per chunk) keep their split cap; B0 does not cover them.
6. Dim cells read weakly in stills; check them in motion in Play.
7. True draw-call counts need a Play-mode frame capture (batch mode gives none).

## 14. Logs and reproduction

Clone: `SCR/proj_trans_lightlead` (SCR = `/private/tmp/claude-501/-Users-redwang-Desktop-ArtCenter-Fall26T7-EGAM-401A-01-Individual-Game-Project/5656cffd-bc90-45f6-86a3-09b26549df8d/scratchpad`).

| Step | Log | Output |
|---|---|---|
| Basecheck (before any edit): 4/4 byte-identical, plans identical | `SCR/ll_logs/basecheck.log` | `SCR/shots_ll_basecheck/` |
| Cool lens | `SCR/ll_logs/coollens.log` | the material |
| r1 render | `SCR/ll_logs/lightlead_r1.log` | `SCR/shots_lightlead_r1/shots_lightlead/` |
| r2 render (final) | `SCR/ll_logs/lightlead_r2.log` | `SCR/shots_lightlead/` |
| Inert checks (4/4 identical, twice) | `SCR/ll_logs/inert.log`, `inert_r2.log` | `SCR/shots_ll_inert2/`, `shots_ll_inert3/` |
| Soft (final) | `SCR/ll_logs/soft2.log` | `SCR/shots_lightlead_soft/` |
| Extras, after and before | `SCR/ll_logs/extra2_ll.log`, `extra2_before.log` | `SCR/shots_ll_extra2/`, `shots_ll_extra2_before/`; poses in `SCR/ll_extra_shots2.json` |
| Stats | `SCR/ll_logs/stats_ll2.log`, `stats_b0only.log`, `stats_off.log` | `SCR/ll_stats/stats_*.txt` |
| B0 counts | `SCR/ll_logs/postcounts.log` | `SCR/ll_stats/b0_counts.txt` |
| Close frames (EvidenceBatch -perType 3 -noCount) | `SCR/ll_logs/evidence2.log` | `SCR/evidence_lightlead/` |
| Sheets | `SCR/ll_sheets.py` | `SCR/ll_out/` |

Extra shot poses (map space, seed 516574485, FOV 76, eye 1.62): extra 1 eye (789.6, 604.5) yaw 90 pitch 2, cell (263,201) Level 0; extra 2 eye (793.8, 613.9) yaw 180 pitch 3, cell (264,204) Office.

## Appendix: full diff of `FrontRoomsMapWorld.cs` (clone base -> lightlead)

```diff
@@ -217,6 +217,8 @@
     {
         public Material wall, floor, ceiling, lens;
         public float lampIntensity;
+        // TRANSITION lightlead: lamp colour per theme (the look is the visual chat's; the constant lives here).
+        public Color lampColor = new Color(1f, .96f, .88f);
     }
@@ -832,14 +834,21 @@
             var startSide = InStartArea(cell) != InStartArea(east);
+            // TRANSITION lightlead (B0.2): each face of a height-border wall goes in its own side's
+            // height-class block, so its _CeilingHeight (grime band) is its own ceiling. -1: the edge's block.
+            var sideBlocks = FrontRoomsTransitionKit.B0 && !InStartArea(cell);
+            var eastSide = sideBlocks && !InStartArea(east);
+            var northSide = sideBlocks && !InStartArea(north);
             BuildEdge(chunk, eastKind, cell, east, new Vector3((i + 1) * cs, 0f, j * cs), Vector3.forward,
                 eastHeight, eastA, eastB, BlockOf(i, j, eastHeight), Get, Solid, origin,
                 !BothInStartArea(new GridCoord(cell.x, cell.y - 1), new GridCoord(cell.x + 1, cell.y - 1)),
-                !BothInStartArea(new GridCoord(cell.x, cell.y + 1), new GridCoord(cell.x + 1, cell.y + 1)) && !(startSide && cell.y + 1 == startArea.yMax));
+                !BothInStartArea(new GridCoord(cell.x, cell.y + 1), new GridCoord(cell.x + 1, cell.y + 1)) && !(startSide && cell.y + 1 == startArea.yMax),
+                eastSide ? BlockOf(i, j, height) : -1, eastSide ? BlockOf(i, j, MapGrid.CeilingHeight(eastZone.height)) : -1);
             BuildEdge(chunk, northKind, cell, north, new Vector3(i * cs, 0f, (j + 1) * cs), Vector3.right,
                 northHeight, northA, northB, BlockOf(i, j, northHeight), Get, Solid, origin,
                 !BothInStartArea(new GridCoord(cell.x - 1, cell.y), new GridCoord(cell.x - 1, cell.y + 1)),
-                !BothInStartArea(new GridCoord(cell.x + 1, cell.y), new GridCoord(cell.x + 1, cell.y + 1)));
+                !BothInStartArea(new GridCoord(cell.x + 1, cell.y), new GridCoord(cell.x + 1, cell.y + 1)),
+                northSide ? BlockOf(i, j, height) : -1, northSide ? BlockOf(i, j, MapGrid.CeilingHeight(northZone.height)) : -1);
@@ -935,7 +944,45 @@
     bool BothInStartArea(GridCoord a, GridCoord b) => InStartArea(a) && InStartArea(b);
+
+    // TRANSITION lightlead (B0.1): the four pieces round corner post k (lattice point k = the south-west
+    // corner of cell k), as BuildInto builds them, and how far the one in `role` may run past the corner
+    // in half thicknesses (+1 today's overlap, 0 to the cell line, -1 to the post face).
+    readonly FrontRoomsTransitionKit.PostPiece[] postPieces = new FrontRoomsTransitionKit.PostPiece[4];
+
+    void CornerReach(GridCoord k, int role, out int reachA, out int reachB)
+    {
+        GridCoord sw = new GridCoord(k.x - 1, k.y - 1), se = new GridCoord(k.x, k.y - 1), nw = new GridCoord(k.x - 1, k.y), ne = k;
+        postPieces[FrontRoomsTransitionKit.South] = PostPieceAt(sw, se, false);
+        postPieces[FrontRoomsTransitionKit.North] = PostPieceAt(nw, ne, false);
+        postPieces[FrontRoomsTransitionKit.West] = PostPieceAt(sw, nw, true);
+        postPieces[FrontRoomsTransitionKit.East] = PostPieceAt(se, ne, true);
+        FrontRoomsTransitionKit.CornerReach(postPieces, role, out reachA, out reachB);
+    }
+
+    FrontRoomsTransitionKit.PostPiece PostPieceAt(GridCoord a, GridCoord b, bool northward)
+    {
+        var za = Cache.ZoneOf(a);
+        var zb = Cache.ZoneOf(b);
+        var ha = MapGrid.CeilingHeight(za.height);
+        var hb = MapGrid.CeilingHeight(zb.height);
+        var h = Mathf.Max(ha, hb);
+        Material wa = Theme(za.theme).wall, wb = Theme(zb.theme).wall;
+        var kind = StartAreaEdge(Cache.Edge(a, b), a, b, northward, ref h, ref wa, ref wb);
+        if (kind == EdgeKind.Open) return default;
+        var startEdge = InStartArea(a) || InStartArea(b);
+        var ca = HeightClass(startEdge ? h : ha);
+        var cb = HeightClass(startEdge ? h : hb);
+        return new FrontRoomsTransitionKit.PostPiece
+        {
+            present = true,
+            finishA = ((long)wa.GetInstanceID() << 4) | (long)ca,
+            finishB = ((long)wb.GetInstanceID() << 4) | (long)cb,
+            officeA = wa == office.wall,
+            officeB = wb == office.wall,
+        };
+    }
@@ -944,29 +991,51 @@
     void BuildEdge(BuiltChunk chunk, EdgeKind kind, GridCoord a, GridCoord b, Vector3 start, Vector3 along, float height,
         Material wallA, Material wallB, int blockIndex, BuilderFn get, SolidFn solid, Vector3 origin,
-        bool mayExtendStart = true, bool mayExtendEnd = true)
+        bool mayExtendStart = true, bool mayExtendEnd = true, int blockA = -1, int blockB = -1)
     {
         if (kind == EdgeKind.Open) return;
         var length = MapGrid.CellSize;
         // Positive "across" points from cell a into cell b.
         var across = new Vector3(along.z, 0f, along.x);
+        // TRANSITION lightlead (B0.2): the block each skin goes in (its own side's ceiling class).
+        var sideA = blockA >= 0 ? blockA : blockIndex;
+        var sideB = blockB >= 0 ? blockB : blockIndex;
+        // TRANSITION lightlead (B0.1): how far each end runs past its corner. Today every end that reaches
+        // a corner runs half a thickness past it; at a corner of mixed finish the kit decides (+, 0 or -).
+        // Per skin (A = cell a's side, B = cell b's side), in metres.
+        float startA = WallThickness * .5f, startB = startA, endA = startA, endB = startA;
+        if (FrontRoomsTransitionKit.B0)
+        {
+            var alongX = along.x > .5f;
+            CornerReach(alongX ? new GridCoord(a.x, a.y + 1) : new GridCoord(a.x + 1, a.y), alongX ? FrontRoomsTransitionKit.East : FrontRoomsTransitionKit.North, out var sa, out var sb);
+            CornerReach(new GridCoord(a.x + 1, a.y + 1), alongX ? FrontRoomsTransitionKit.West : FrontRoomsTransitionKit.South, out var ea, out var eb);
+            startA *= sa; startB *= sb; endA *= ea; endB *= eb;
+        }
         void Piece(float from, float to, float bottom, float top, bool extendStart, bool extendEnd)
         {
             if (from > 0f || !mayExtendStart) extendStart = false;
             if (to < length || !mayExtendEnd) extendEnd = false;
-            var f = from - (extendStart ? WallThickness * .5f : 0f);
-            var t = to + (extendEnd ? WallThickness * .5f : 0f);
-            if (t - f < .05f || top - bottom < .05f) return;
-            var center = start + along * ((f + t) * .5f) + Vector3.up * ((bottom + top) * .5f);
-            if (wallA == wallB)
+            if (top - bottom < .05f) return;
+            var fA = from - (extendStart ? startA : 0f);
+            var tA = to + (extendEnd ? endA : 0f);
+            var fB = from - (extendStart ? startB : 0f);
+            var tB = to + (extendEnd ? endB : 0f);
+            if (wallA == wallB && sideA == sideB && fA == fB && tA == tB)
             {
-                var size = along * (t - f) + across * WallThickness + Vector3.up * (top - bottom);
+                if (tA - fA < .05f) return;
+                var center = start + along * ((fA + tA) * .5f) + Vector3.up * ((bottom + top) * .5f);
+                var size = along * (tA - fA) + across * WallThickness + Vector3.up * (top - bottom);
                 solid(blockIndex, wallA, center, Abs(size), WallpaperRepeat);
                 return;
             }
-            var half = along * (t - f) + across * (WallThickness * .5f) + Vector3.up * (top - bottom);
-            solid(blockIndex, wallA, center - across * (WallThickness * .25f), Abs(half), WallpaperRepeat);
-            solid(blockIndex, wallB, center + across * (WallThickness * .25f), Abs(half), WallpaperRepeat);
+            void Skin(int block, Material material, float f, float t, float side)
+            {
+                if (t - f < .05f) return;
+                var center = start + along * ((f + t) * .5f) + Vector3.up * ((bottom + top) * .5f) + across * (WallThickness * .25f * side);
+                solid(block, material, center, Abs(along * (t - f) + across * (WallThickness * .5f) + Vector3.up * (top - bottom)), WallpaperRepeat);
+            }
+            Skin(sideA, wallA, fA, tA, -1f);
+            Skin(sideB, wallB, fB, tB, 1f);
         }
@@ -1202,7 +1271,7 @@
-        light.color = new Color(1f, .96f, .88f);
+        light.color = theme.lampColor; // TRANSITION lightlead: was new Color(1f, .96f, .88f) for both themes
@@ -1219,6 +1288,14 @@
         fixture.mode = (tierRules ?? new FrontRoomsTierRules()).At(tier).LampMode(roll);
+        // TRANSITION lightlead: border lamp rule, Auto lamps only (the roll above is still drawn, so other lamps stay put).
+        if (lamp == ModuleLamp.Auto && FrontRoomsTransitionLightLead.On)
+        {
+            var rolled = fixture.mode;
+            var self = Cache.ZoneOf(cell).theme;
+            fixture.mode = FrontRoomsTransitionLightLead.BorderMode(rolled, self, BorderThemes(cell, borderKinds), borderKinds);
+            FrontRoomsTransitionLightLead.Record(cell, self, rolled, fixture.mode);
+        }
         // A module's lamp: Steady..Dim map onto modes 0..4.
         if (lamp != ModuleLamp.Auto) fixture.mode = (int)lamp - 1;
@@ -1226,6 +1303,22 @@
+    // TRANSITION lightlead: the four neighbours of a lamp's cell (E, W, N, S) and what stands between,
+    // as BuildInto builds it (the start area is walled off, so it is never a crossing).
+    readonly ZoneTheme[] borderThemes = new ZoneTheme[4];
+    readonly EdgeKind[] borderKinds = new EdgeKind[4];
+
+    ZoneTheme[] BorderThemes(GridCoord cell, EdgeKind[] kinds)
+    {
+        for (var k = 0; k < 4; k++)
+        {
+            var n = k == 0 ? new GridCoord(cell.x + 1, cell.y) : k == 1 ? new GridCoord(cell.x - 1, cell.y) : k == 2 ? new GridCoord(cell.x, cell.y + 1) : new GridCoord(cell.x, cell.y - 1);
+            borderThemes[k] = Cache.ZoneOf(n).theme;
+            kinds[k] = InStartArea(n) || InStartArea(cell) ? EdgeKind.Wall : Cache.Edge(cell, n);
+        }
+        return borderThemes;
+    }
@@ -2058,6 +2151,16 @@
             lens = officeLens != null && officeLens.HasProperty("_EmissionColor") ? officeLens : level0Lens,
             lampIntensity = 5.5f,
         };
+        // TRANSITION lightlead: Office lamps cool white and 9 % brighter, with the cool lens; Level 0 unchanged.
+        if (FrontRoomsTransitionLightLead.On)
+        {
+            level0.lampColor = FrontRoomsTransitionLightLead.Level0Color;
+            level0.lampIntensity = FrontRoomsTransitionLightLead.Level0Intensity;
+            office.lampColor = FrontRoomsTransitionLightLead.OfficeColor;
+            office.lampIntensity = FrontRoomsTransitionLightLead.OfficeIntensity;
+            var coolLens = FrontRoomsTransitionLightLead.OfficeLens;
+            if (coolLens != null && coolLens.HasProperty("_EmissionColor")) office.lens = coolLens;
+        }
```
