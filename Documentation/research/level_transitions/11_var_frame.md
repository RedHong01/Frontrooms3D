# Level transitions · 11 · V1 Frame: pre-render report

Date: 2026-10-03. Variation key `frame`, with its step 0 `b0` (plan `10_transition_plan.md` §3 V1, §7.1).
Everything here was built and rendered in the private clone `proj_trans_frame`. Nothing in Red's project changed except the files listed in §13. No downloads and no Figma writes.
This run was cut by the usage limit at 17:54 and resumed at 21:55. The resumed run checked the half-finished work, re-rendered the final set, added a collision check and wrote the outputs (§2.3).

## 0. Short answer (for Red)

- **Every Office | Level 0 change now sits on a built piece.** The finishes still change on the cell line, but always on an object that the Office fit-out owns:
  - arches are cased;
  - open edges get a portal with a 0.35 m header;
  - doors and windows get dark-bronze steel frames;
  - free wall ends get an Office end cap;
  - Office outside corners get beige corner guards;
  - every Office wall has a 0.10 m rubber base.
- **It reads as built, not as a texture seam.** Shot2 is the clearest case: the corridor's paper now stops on two pilasters under a header, and an aluminium bar marks the carpet change (`images/var_frame_sheet.jpg`, row 2).
- **B0 alone fixes the z-fight.** In shot4 the grey stripe on the yellow corner is gone. Shot2's corridor corners lose their paper sliver (`images/var_b0_sheet.jpg`).
- **No layout, collision or lamp change.** The plan files match BEFORE in all 4 shots. The collision meshes hash the same in off, b0 and frame mode. Lit lamps stay 84 / 82 / 83 / 88.
- **Cost (within 46 m of the eye, frame vs today):**
  - triangles +21 to +24 % (175,766 → 216,076 in shot1);
  - renderers in the view frustum (the draw proxy) +34 to +45, of which 23-28 are kit renderers;
  - 0 MB of new textures and 0 extra lights;
  - one new 2.1 KB material.
  - About half the added triangles are the base, which runs on every Office wall: 1.7 km of base in 25 chunks.
- **Risk, as the plan said: it is tidy.** Frame fixes the cut. It adds no mood. It pairs well with V5's cool Office light (colour only).
- **Main changed while this ran.** Codex put Light lead's B0 and V5 code into Red's project at 19:10, live by default. Frame was not promoted. If Red picks Frame:
  - merge it into main's `FrontRoomsTransitionKit.cs`; never copy it over that file;
  - pick one B0 rule;
  - follow main's single-acting doors;
  - drop the window proxy, because main already frames every window (§11).

## 1. Images

All 1920 px wide, JPG q85, in `images/`.

| File | What |
|---|---|
| `var_b0_shot1.jpg` .. `shot4.jpg` | Step 0: B0 only (corner ownership + grime band per side). Harness output, same cameras as `shot<k>_before.jpg`. |
| `var_b0_sheet.jpg` | Four rows, BEFORE left, B0 right, labelled. |
| `var_frame_shot1.jpg` .. `shot4.jpg` | B0 + V1 Frame, the four fixed shots (harness output, unchanged cameras). |
| `var_frame_sheet.jpg` | Four rows, BEFORE left, FRAME right, labelled. |
| `v_frame_shot1.jpg` .. `shot4.jpg` | Byte-identical copies of `var_frame_shot1..4` (the workflow asked for both names). |
| `v_frame_shot5.jpg` | Extra 1: portal close. The west pilaster, the header and wall angle on Z = 588, base wrapping the pilasters, the cased arch on X = 765 (row 195) at left. Eye (767.35, 1.62, 585.5), yaw −40, pitch 4. |
| `v_frame_shot6.jpg` | Extra 2: both shot1 arches close. Office reveals, beads, bars, the base wrapping into the reveals. Eye (792.7, 1.62, 607.1), yaw 41.3, pitch 11.6. |
| `v_frame_vs_before.jpg` | Six rows, BEFORE left, FRAME right: the four fixed shots and the two extras. |
| `var_frame_close_post.jpg` | Cut D (`cut_post_1` camera): one Office post, a corner guard, no stripe. |
| `var_frame_close_wallend.jpg` | Cut E (`cut_wallend_1` camera): an Office end cap with beads and a guard; it was half paper, half paint. |
| `var_frame_close_seam.jpg` | Cut C (`cut_seam_1` camera): the paper stops on a portal pilaster. |
| `var_frame_close_door.jpg` | Cut F (`cut_door_1` camera), Level 0 side: steel casing, fluted saddle, leaf open into Level 0. |
| `var_frame_close_window.jpg` | Cut G (`cut_window_1` camera): steel casing with glazing stops; the split sill is covered. |
| `var_frame_close_sheet.jpg` | The five close frames, BEFORE (`cut_*.jpg`) left, FRAME right. |

The close frames are the cut-evidence candidates re-rendered with `EvidenceBatch -perType 3 -noCount`. The layout is frozen, so each camera is the same as the matching `cut_*.jpg`.

### What each shot had to show

| Shot | Brief | Result |
|---|---|---|
| shot1 | Both arches cased, Office paint reveal, beads, bar; Office base beyond | Yes. Arch Z = 609 ahead: the reveal is one Office paint with a bead on the paper side, and a bar runs on the line. Arch X = 795 at right: cased, base on the Office wall. |
| shot2 | Portal on Z = 588 (pilasters ≈ X 765.08-765.20 and 767.80-767.92, header 2.55-2.90); both corridor walls change paper on the pilasters; bar on the line; at left, the row-194 portal and the cased row-195 arch | Yes for Z = 588: pilasters at exactly those X values, header 2.55-2.90, wall angles, bar. At left, the row-195 arch reveal is now all Office paint (before: half paper, half paint). The row-194 portal on X = 765 is built (listed in the stats run, §4.3), but it stands beside the eye, behind the camera plane, so this camera cannot see it. |
| shot3 | Door in a dark-bronze frame with casing, stop and saddle; base stops at the casing | Yes. The old `Cove_Base` trim is hidden inside the casing. The base stops 5 mm short of the casing. |
| shot4 | Cased arch far ahead, portal on X = 798 at right, Office base | Yes. The B0 fix also removes the grey stripe on the yellow corner at centre right. |

## 2. What was built

### 2.1 B0 (step 0, also inside Frame)

- **B0.1 Corner ownership.** At a lattice corner where the finishes differ and two or more walls meet, no wall piece enters the 0.16 × 0.16 m post square for rendering. The post is built as four render-only 0.08 m quarters. Each quarter carries the finish and height block of the cell it faces. Corners with one finish and start-area corners are unchanged. 596-628 quarters (149-157 posts) in the 25 built chunks.
- **B0.2 Grime band per side.** Each face of a height-border wall is built in its own side's height-class block, so `_CeilingHeight` is that side's ceiling. Where both sides share a material, two 0.08 m skins are still built.
- Collision keeps today's boxes, in today's order.
- See §8 for how the B0.1 rule here differs from the brief and from the B0 that is now in main.

### 2.2 V1 Frame (the kit)

All numbers are metres unless marked. Everything is render-only. Office pieces use `Office_Wall`.

| Piece | Built | Count in 25 chunks (shot1/4 set · shot2/3 set) |
|---|---|---|
| **Cased arch** | Wall pieces beside a border arch stop 0.012 short of the opening (`BeadInset`); the head piece starts 0.012 higher. A lining fills the gap: jambs and head, 0.16 deep, 0.012 returns on both faces, 6 mm quarter-round beads (8 segments) on both arrises, mitred. The Level 0 paper stops at the bead. | 10 · 10 |
| **Binder bar** | Aluminium, 0.035 wide × 0.005 high, 3 mm rounded top, on the cell line, the clear width of the opening, shadows off. | 40 bars, 93.4 m · 36, 82.4 m |
| **Portal** | On every border open edge: 0.12 × 0.20 pilasters against the wall that continues at each end (0.02 proud of each face), a 0.20-deep header from ceiling − 0.35 (2.55 on Standard) to the ceiling, 6 mm beads, a 22 × 22 × 1.5 mm white wall angle on both header faces at the ceiling, the bar between the pilasters. | 30 portals, 50 pilasters · 26, 44 |
| **Free post** | Where border open edges meet with no wall: one post, 0.24 along a run of openings (shared pilaster of an arcade), 0.20 otherwise, beaded, base on four faces. | 5 (all shared) · 4 (3 shared) |
| **Door frame** | No R3 FBX in the clone, so a proxy swept from R3's sleeve section (`interactables/10_spec.md` §1.4, OF-F): casing 0.0795-0.105 off the wall centre on both faces, 0.075 return, jambs 0-2.175, head 2.098-2.175, mitred, 3 mm / 1.5 mm radii, a 16 mm stop on the Office (push) side. Fluted aluminium saddle 0.152 × 0.012, 1:2 bevels, 13 flutes. `Prop_SteelBrown` (dark bronze). | 39 + 39 saddles · 45 + 45 |
| **Window frame** | The same sleeve as a closed loop round the 1.4 × (0.35-2.0) opening, with two glazing stops instead of the door stop. | 10 · 0 |
| **End cap** | A free border wall end: the wall is inset 0.012 and an Office lining with beads wraps the 0.16 end face. Base across it; a guard on the Office arris. | 2 · 3 |
| **Corner guard** | Exposed Office outside corners on border walls (L corners and end caps): 0.05 × 0.05 wings, 2 mm, 3 mm nose, 1.22 tall above the base, beige vinyl (`Prop_PlasticBeige`). | 5 · 1 |
| **Pilaster cap** | Not built: there is no case for it (see §8). | 0 · 0 |
| **Cove base** | 0.10 × 6 mm rubber profile (3.2 mm web, 2.6 mm cove toe) on every Office wall face in the built chunks, shadows off. Stops 5 mm short of casings. Wraps outside corners, portal pilasters, free posts, end caps and arch reveals. No base on Level 0 walls. | 1,256 runs, 1,724.7 m · 1,265 runs, 1,688.4 m |

- **Lamps, floors (other than bars and saddles) and ceilings are unchanged.**
- **Geometry.** Every piece is a 2D profile swept in C# (`FrontRoomsTransitionKit.Sweep`), with real fillets and mitred joints. No FBX and no Blender module were needed.
- **Draws.** Office-wall pieces (linings, portals, end caps) join the existing wall mesh of their 6 m block, so they add no draw and keep the block's grime band. The other materials make one renderer per chunk × material × shadow flag, with no `MaterialPropertyBlock`.

Triangles per piece, computed from the profiles:

| Piece | Triangles |
|---|---|
| Cased arch | 196 (lining 114 + bar 82) |
| Portal | about 360 (Office_Wall 118, wall angles 68, bar 82, pilaster base 96) |
| Door frame | 362 (sleeve 198, saddle 164) |
| Window frame | 344 |
| Corner guard | 88 |
| Free post | 136 |
| Base | 16 per run (+9 per cut end) |

The measured totals (§4) agree within 7 %.

### 2.3 Iterations

| Run | Change |
|---|---|
| r1 (17:16) | Full kit. Bars and saddles in `Prop_Aluminium` read as bright white lines at grazing angles. |
| r2 (17:25) | Close-frame fixes (end caps and guards). The fixed shots were unchanged. |
| r3 (17:38) | New `Trans_AluminiumSatin` (Prop_Aluminium's maps, satin). It was too dark: the bars almost vanished. |
| r4 (17:46) | Satin tuned to base 0.80 / 0.80 / 0.78, smoothness 0.80. Bars read as thin metal lines and the saddle reads as fluted aluminium. |
| final (22:05, resumed run) | The clone, untouched since 17:54, was re-rendered: shot1 and shot2 are byte-identical to r4; shot3 and shot4 differ by at most 2 / 255 per channel (GPU and JPEG noise). The final set is r4. |

What was checked after r4:
- I looked at every frame and at zoomed crops: no floating, clipping, z-fighting or near-plane contact.
- A bright speck under the left arch head in extra shot6 is a troffer beyond the arch. It is also in the BEFORE frame.
- The base on the arch reveals ends flush at the Level 0 face plane, as a cut rubber base does.

## 3. Checks

| Check | Result |
|---|---|
| Harness unchanged | `FrontRoomsTransitionAudit.cs` md5 `76a624e3217630e637bdb381af7ca5b1`; `shots.json` unchanged (11:55). |
| Clone base | `proj_trans` MapWorld md5 `e5204f9c7a7e07f9fa6d584915e6297e`; never re-synced. |
| Basecheck before any edit (16:44) | All four `shot<k>_basecheck.jpg` are byte-identical to `images/shot<k>_before.jpg` (`cmp`). |
| Kit off (`-transMode off`, 17:49; also the BEFORE of the extras) | All four frames byte-identical to BEFORE, so the hooks are inert when off. |
| Layout freeze | `shot<k>_plan.json` identical to `logs/shot<k>_plan.json` in off, b0, frame and the final rerun. |
| Lamp lines | lit 84 / 82 / 83 / 88 and shadowed 8 / 7 / 7 / 7 in b0 and frame, as BEFORE. |
| Collision (new, 22:14) | `FrontRoomsTransitionCollisionCheck`: all collider meshes of the 25-chunk build, hashed (FNV-1a over vertices and indices). Off, b0 and frame give the same hash, 25 colliders, 76,344 / 77,124 triangles. shot1/4 seed set `2a7c446954f37a6f`, shot2/3 `cb089f536519ec9b`. |
| Logs | Errors in the logs are only the usual licensing-token and Android adb lines. |

## 4. Measurements

### 4.1 Within 46 m of each eye (`FrontRoomsTransitionStats`, own editor method, not the harness)

| Shot | Mode | Renderers | Shadow casters | Materials | Triangles | Lights on | Texture MB (materials in range) | Mesh MB (25-chunk build) | Kit renderers / tris | In frustum: renderers / kit / tris |
|---|---|---|---|---|---|---|---|---|---|---|
| shot1 | off | 1,801 | 1,042 | 42 | 175,766 | 84 | 637.4 | 59.51 | 0 / 0 | 563 / 0 / 28,848 |
| shot1 | b0 | 1,830 | 1,071 | 42 | 182,198 | 84 | 637.4 | 60.73 | 0 / 0 | 572 / 0 / 31,452 |
| shot1 | frame | 1,864 | 1,080 | 43 | 216,076 | 84 | 637.4 | 64.08 | 32 / 30,898 | 597 / 23 / 55,412 |
| shot2 | off | 1,779 | 1,018 | 42 | 162,760 | 82 | 637.4 | 38.39 | 0 / 0 | 621 / 0 / 96,406 |
| shot2 | b0 | 1,818 | 1,057 | 42 | 168,712 | 82 | 637.4 | 39.52 | 0 / 0 | 641 / 0 / 98,854 |
| shot2 | frame | 1,854 | 1,064 | 43 | 197,474 | 82 | 637.4 | 42.72 | 35 / 25,964 | 666 / 24 / 122,166 |
| shot3 | off | 1,756 | 995 | 42 | 178,600 | 83 | 637.4 | 38.39 | 0 / 0 | 565 / 0 / 56,208 |
| shot3 | b0 | 1,794 | 1,033 | 42 | 184,324 | 83 | 637.4 | 39.52 | 0 / 0 | 579 / 0 / 58,164 |
| shot3 | frame | 1,836 | 1,043 | 43 | 216,844 | 83 | 637.4 | 42.72 | 40 / 29,022 | 609 / 28 / 84,218 |
| shot4 | off | 1,791 | 1,033 | 42 | 172,554 | 88 | 637.4 | 59.51 | 0 / 0 | 566 / 0 / 31,112 |
| shot4 | b0 | 1,818 | 1,060 | 42 | 178,830 | 88 | 637.4 | 60.73 | 0 / 0 | 573 / 0 / 33,572 |
| shot4 | frame | 1,853 | 1,069 | 43 | 213,190 | 88 | 637.4 | 64.08 | 33 / 31,490 | 600 / 25 / 58,638 |

"Lights on" counts enabled lamps within 46 m. It matches the harness lit count in every shot.

### 4.2 Deltas

**B0 vs off:**
- triangles +3.2 to +3.7 % (quarters and the second skin);
- renderers +27 to +39 (faces moved into another height block make new block × material meshes);
- frustum renderers +7 to +20;
- mesh +1.1 to +1.2 MB.

**Frame vs off:**

| | shot1 | shot2 | shot3 | shot4 |
|---|---|---|---|---|
| Triangles in range | +40,310 (+22.9 %) | +34,714 (+21.3 %) | +38,244 (+21.4 %) | +40,636 (+23.6 %) |
| Renderers in range | +63 | +75 | +80 | +62 |
| Draw proxy (renderers in frustum) | +34 | +45 | +44 | +34 |
| … of which kit renderers | 23 | 24 | 28 | 25 |
| Shadow casters | +38 | +46 | +48 | +36 |

- Lights: 0 change. Materials: +1 (`Trans_AluminiumSatin`).
- **Texture memory added: 0 MB.** The materials in range hold 637.4 MB in all three modes. The satin aluminium reuses `Prop_Aluminium`'s four maps. `Prop_SteelBrown`, `Prop_PlasticBeige`, `Prop_PlasticWhite` and `Cove_Base` are already loaded by the office kit.
- **Mesh memory:** +4.3 to +4.6 MB for the whole 25-chunk build (+7.7 to +11 %), including B0.
- **Over 25 chunks:** 44,094 triangles in kit renderers + 4,716 merged into Office_Wall = 48.8 k, about 1.95 k per chunk.
  - The base is about 21 k of that (16 per run, 1,256 runs, plus end caps), about 0.85 k per chunk.
  - The plan estimated 0.3 k per chunk for the base; it is higher because the base runs on every Office wall, not only near borders.
- **Draws vs the plan's estimate.** The plan estimated +5-15 extra draws near borders; measured +23-28 kit renderers in the frustum.
  - The base puts a `Cove_Base` renderer in every chunk that has Office walls.
  - Kit renderers are per chunk, not per 6 m block.
  - On WebGL that is about +0.8 % of ~3,200 draws. See §12 for ways to cut it.
- **Batch mode reads 0 draw calls** (`UnityStats`), so true draw counts need a Play capture. The in-frustum renderer count is the proxy, as for V5.

### 4.3 Border pieces near each eye (stats log)

shot1/4 set, 4 cells round the eye:
- doors (266,200)|(267,200), (267,200)|(267,201), (268,200)|(268,201), (268,201)|(269,201), (268,202)|(269,202), (268,204)|(269,204);
- open (265,201)|(266,201) and (263,204)|(263,205), (262,206)|(262,207);
- arches (264,202)|(265,202) and (264,202)|(264,203).

shot2/3 set:
- open (252,192)|(253,192), (254,192)|(254,193), (254,194)|(255,194) = the row-194 portal on X = 765, (255,195)|(255,196) = the shot2 portal, (249,198)|(249,199);
- arches (254,193)|(255,193), (254,195)|(255,195) = the row-195 arch;
- doors (250,198)|(250,199), (250,199)|(251,199), (253,199)|(253,200) = the shot3 door.

## 5. Draft contract (map-file diff against the clone base)

`FrontRoomsMapWorld.cs`: 11 marked hooks (`// TRANSITION frame`), +220 / −18 lines. Full diff in the appendix. No other map file changed.

| # | Where | What it does | Why |
|---|---|---|---|
| 1 | `MeshBuilder.Append` (new) | Appends a kit mesh to a block's wall builder | Office-wall pieces join the block mesh: 0 extra draws, same grime band |
| 2 | `BuildInto`, before the cell loop | B0 switch, per-chunk corner cache (`TransitionCornerAt`), the kit sink | One corner descriptor per lattice point, shared by the 2-4 edges that meet there |
| 3 | `BuildInto`, per edge | Per-side height blocks (B0.2) and a `TransitionEdge` descriptor (kind, cells, themes, both ceilings, wall height, start, along, across, both corners) for edges outside the start area | B0.2; the hook's input |
| 4 | `BuildEdge` calls | Pass blockA, blockB, collision, descriptor, sink | |
| 5 | `BuildInto`, per cell | B0.1 post quarters at mixed corners with ≥ 2 walls; the corner hook (free posts, end caps, guards) | B0.1; V1 corner pieces |
| 6 | `BuildInto`, after the loop | Office-wall kit meshes into block builders; other kit meshes as `Transition <material>` renderers under `Transition pieces` | Draw control; no property blocks |
| 7 | `TransitionCornerAt`, `TransitionSink` (new) | The corner descriptor; the sink keyed by block, material and shadow flag | |
| 8 | `BuildEdge` signature | `blockA`, `blockB`, `collision`, `t`, `sink` (all optional) | |
| 9 | `BuildEdge`, open edges | The kit sees open edges (portals) before the early return | V1 portals |
| 10 | `Piece` | Collision boxes exactly as today. Render extents: B0.1 post-face stop, end-cap inset, bead insets, head raise; two skins in their own blocks | Render-only change; collision verified by hash |
| 11 | `BuildEdge`, walls and openings | Base hook on walls; BeadInset on border arches; the BuildEdge hook with width, centre, top and sill | V1 |

Constants the kit uses, proposed for `ModuleUnits`:
- `BeadInset` 0.012;
- `PortalPilaster` 0.12, `PortalDepth` 0.20, `PortalHeaderDrop` = `BulkheadDepth` 0.35, `SharedPilaster` 0.24;
- `BarWidth` 0.035, `BarHeight` 0.005.

The base uses the existing `CoveHeight` 0.10 and `CoveProud` 0.006.

## 6. New assets and files (clone only)

| File | Size | What |
|---|---|---|
| `Assets/Scripts/Rendering/Transitions/FrontRoomsTransitionKit.cs` (+ .meta, GUID `bdbf6eaf04ef848da8a5683d3a474d24`) | 48 KB, 1,016 lines | The kit: modes (`-transMode off/b0/frame`), descriptors, sweep and ear-clip mesher, profiles, pieces, counters |
| `Assets/Resources/Surfaces/Trans_AluminiumSatin.mat` (+ .meta) | 2.1 KB | `Prop_Aluminium` maps, base (0.80, 0.80, 0.78), smoothness 0.80, mesh UV |
| `Assets/Editor/Transitions/FrontRoomsTransitionAssets.cs` | 1.4 KB | Asset prep (writes the material) |
| `Assets/Editor/Transitions/FrontRoomsTransitionStats.cs` | 9.5 KB | The §4 stats |
| `Assets/Editor/Transitions/FrontRoomsTransitionCollisionCheck.cs` | 4.7 KB | The §3 collision hash (added in the resumed run) |

- No textures, no FBX, no Blender modules.
- Mesh data is generated at build time: +4.3 to +4.6 MB per 25-chunk build.

## 7. Change list (every clone change, and why)

1. `FrontRoomsMapWorld.cs`: the 11 hooks in §5. Why: B0 and the V1 BuildEdge, corner and base hooks; render extents split from collision extents.
2. `FrontRoomsTransitionKit.cs` (new): all V1 logic, visual-owned. Why: keep the map diff to hooks (plan §2.4).
3. `Trans_AluminiumSatin.mat` (new): satin bars and saddles. Why: `Prop_Aluminium` read as a white line (r1).
4. Editor tools (new, tools only): asset prep, stats, collision check.
5. Nothing else. The harness, `shots.json`, scenes, the level profile, modules and every other file match `proj_trans`.

## 8. Deviations from the brief

1. **B0.1 at straight runs.**
   - The brief says collinear pieces keep their 0.08 m extensions. Two collinear pieces that both keep them overlap over the whole post square, so with different finishes their faces z-fight. That breaks the brief's own first rule.
   - Here, at a mixed corner with two or more walls, every piece stops at the post face. Four render-only quarters fill the post, each in the finish of the cell it faces. The finish still changes on the cell line, and nothing overlaps.
   - At an L corner, the brief's rule moves the change to the arris (the Office outside piece wraps). This rule keeps it on the cell line, 0.08 m from the arris. The corner guard (0.05 m wings) sits on that arris.
   - Light lead's B0, now in main, solved the same conflict a third way (collinear pieces meet on the cell line). The real version needs one rule (§11).
2. **Frame material.** `Prop_SteelBrown` instead of `Painted_Metal` tinted (0.20, 0.17, 0.13). `Painted_Metal` would load about 8 MB more maps within range. `Prop_SteelBrown` renders near-black bronze (sRGB about 0.12 / 0.14 / 0.10 under the grade in shot3). A tinted copy is a one-line change if Red wants it lighter.
3. **Bars and saddles** use `Trans_AluminiumSatin`, not `Prop_Aluminium` (§2.3).
4. **Wall angles** use `Prop_PlasticWhite` (white, already loaded) instead of a painted-steel material.
5. **Corner guards** use `Prop_PlasticBeige`, an existing beige plastic already in range, instead of `Prop_Vinyl` or a new beige copy (no new material).
6. **Door frame** is a proxy built to R3's envelope: the clone has no `Kit_DoorFrame_Steel`. Main has it now (§11).
7. **Pilaster caps are not built.** On today's map a flat seam cannot exist without a portal:
   - the paper on one face of a straight wall changes only if the perpendicular arm at that corner separates two themes;
   - an arm that is not a wall is an open edge, so it is a border open edge;
   - so a portal pilaster always lands on the seam.
   - Counts: 0 caps needed in both 25-chunk builds, and every seam frame (`var_frame_close_seam.jpg`) shows a pilaster.
8. **Free posts** were added where border open edges meet with no wall. The brief only names shared pilasters on one line; an L of open edges needed the same post (1 of 9).
9. **The base runs on all faces of portal pilasters and free posts, including the Level 0 side.** They are Office objects. Level 0 walls get no base.
10. **Extra shots** keep the harness, root, seed and FOV; they are new cameras (`extra_shots.json`).

## 9. Era check (1990)

Every piece was standard in commercial interiors by 1990. Nothing is printed, and there are no marks or trademarks.

| Piece | In use by |
|---|---|
| Hollow-metal frames with stops | 1950s |
| Metal corner bead with a painted drywall return | 1950s |
| Rubber cove base | 1950s |
| Aluminium carpet binder bars and fluted saddles | 1950s |
| Steel wall angle for lay-in ceilings | 1960s |
| Vinyl corner guards | 1969 |

The bronze finish and the beige guards fit a 1990 fit-out.

## 10. Gameplay notes

- **No collider changes** (hash-verified). Navigation, the Relay and the map tests see the same world.
- **Portal pilasters** stand 0.12 m out from the side wall, inside the player's 0.30 m radius band, so the player never reaches them. Clear width in a corridor: 2.44 m.
- **Free posts (0.20-0.24 m) stand in open floor and have no collider.** The player can walk through them: 5 per 25-chunk build here. They need a collider from the map chat, or they should be dropped (open issue 1).
- **The saddle is 12 mm and render-only.** No step, no snag.
- **Cased arches narrow nothing:** the lining sits in the 0.012 m the walls give up.

## 11. Main changed during this run (Codex), and what it means for Frame

- **What happened.** At 19:10 (`8ef5b64`) Codex committed level-transition code into Red's project.
  - `Scripts/Rendering/Transitions/FrontRoomsTransitionKit.cs` and `FrontRoomsTransitionLightLead.cs` are the Light lead clone's files: kit md5 `7a8c8af12d4cca733d6242a06f772d6e`, the same as `proj_trans_lightlead`.
  - `FrontRoomsMapWorld.cs` gained hooks marked `N1/B0.1`, `N1/B0.2` and `N1/V5`; main's MapWorld md5 is now `a152e6bbcd129d727ddcac973cb98964`.
  - Red has not picked a variation.
  - Codex read this clone at 18:42-18:46 but did not promote Frame. It only read it (file reads, a diff, git status). Nothing in this clone changed, and no Frame file is in main.
- **I did not touch main.** Per the rules, I report rather than revert. The `codex-audit` workflow owns the check of that code.
- **If Red picks Frame:**
  1. **Do not copy this clone's kit file into main.** Main's `FrontRoomsTransitionKit.cs` (GUID `b215858f1c0014f87ae44abb9c680391`) holds the same class name. Merge Frame's code into main's file, keep main's GUID, and keep `B0` and `CornerReach` working for V5.
  2. **One B0.1 rule.** Main uses per-skin reach (owner's outside skin +1, collinear 0, perpendicular −1). Frame's hooks assume the quarter post (render end mode 1) and the end-cap inset (end mode 2). On main, the end cap becomes a reach of `−BeadInset` on both skins of a free border end. Either rule passes the shots; the map chat should pick one.
  3. **Rebase on Red's 12:21 single-acting doors and 12:38 lamp interface v1.**
     - The door frame's stop must sit on the side the leaf closes against.
     - In the clone the leaf swings into Level 0, so the stop faces the Office.
     - With main's single-acting doors, the push side must come from the door's swing data, not from the theme.
  4. **Use R3's frame.** Main now has `Kit_DoorFrame_Steel.fbx` (and `_Alu`, `Kit_WindowFrame_Steel_Enamel`), promoted from `proj_int` before its G2/G3 rebuild finished. Once the audit clears them, place them as OF-F (`interactables/10_spec.md` §1.4) instead of the proxy. The proxy's envelope matches.
  5. **Windows are already framed in main.** Codex's direct `DressWindow` call dresses every map window with the window track's kit frame: steel in Office rooms, walnut in Level 0. It is live now (audit `00_main_state.md` §3.4). Frame's window casing would double it. On main, drop Frame's window proxy and let the window track's frame own border windows. The window-landing workflow owns removing the map's own trims under it.
  6. **R3 frame LODs.** The audit found that `FrontRoomsKitImporter` maps the cull distance to the wrong LOD. After a reimport, door and window frames would vanish at 12 m (§3.7). That must be fixed before Frame relies on `Kit_DoorFrame_Steel`.
- **B0 is live in Red's game now.** Main's B0 (Light lead's per-skin rule) is on by default (audit §3.5). Codex added it as its fix for the corner-post seam Red asked about at 13:09. The audit suggests offering B0 on its own as the seam fix. `var_b0_sheet.jpg` shows what this clone's B0 does on the four shots. I did not render main's rule: that needs a fresh clone of main, and the audit owns main.
- **Audit status.** At 22:25, `codex_audit/` held `00_main_state.md` (22:20) and a map-chat note. `20_findings.md` did not exist yet, and nothing in `00_main_state.md` is a finding against Frame. The facts in points 5 and 6 and the bullet above come from it.

## 12. What the real implementation needs

**Map chat (contract requests):**
1. The `BuildEdge` hook (§5, rows 3, 9 and 11) for every edge outside the start area. Border edges get pieces; Office faces get the base.
2. A per-chunk corner descriptor (arms, four themes, ceilings, finishes, start-area flag).
3. Render extents split from collision extents in `Piece`. Keep collision byte-identical (the hash test in §3 can become a map test).
4. `BeadInset` on pieces beside border arches, and the head raise.
5. B0.1 (one rule, see §11) and B0.2.
6. `MeshBuilder.Append` for Office-wall kit meshes, and per-chunk kit renderers without property blocks.
7. The `ModuleUnits` constants in §5.
8. A decision on free-post colliders (0.20-0.24 m box).
9. `LEVEL_MODULE_SPEC.md`: props against Office walls clear the 6 mm base, and props near border open edges clear the pilasters (0.12 m) and header (0.35 m drop).
10. A rerun of the 66 interaction tests.

**Visual chat:**
1. Merge the kit into main's file (§11).
2. Add `Trans_AluminiumSatin` as a `SurfaceDef` row in `FrontRoomsRenderSetup` (or tune `Prop_Aluminium`).
3. Pick the frame finish (Prop_SteelBrown or a tinted copy) and the base colour.
4. Swap in R3's frame and window FBX after the audit.
5. WebGL tier: merge the base into the block's existing `Cove_Base` trim mesh (one draw fewer per chunk, but the base would cast shadows) or drop the base beyond 10 m. Gate both to WebGL.

**Others:** none required.
- 平面视觉: nothing is printed.
- Sound chat: optional aluminium footstep on bars and saddles.
- 系统设计: no gameplay change.

## 13. Outputs in Red's project

Only these files were written:
- `level_transitions/images/var_b0_shot1..4.jpg` and `var_b0_sheet.jpg`;
- `var_frame_shot1..4.jpg` and `var_frame_sheet.jpg`;
- `v_frame_shot1..6.jpg` and `v_frame_vs_before.jpg`;
- `var_frame_close_post.jpg`, `_wallend`, `_seam`, `_door`, `_window` and `var_frame_close_sheet.jpg`;
- `level_transitions/11_var_frame.md` (this file) and `20_variation_frame.md`.

## 14. Verification log

The FRONTROOMS · VISUAL VERIFICATION LOG section is not on the Figma canvas yet (`VERIFICATION_LOG.md`: `section id: —`), and this brief allows no Figma writes. So the frames are listed here for the placing workflow. No VL number was claimed.

| Check | Verdict | Question | Statement | Images (slot order) |
|---|---|---|---|---|
| B0 alone | PASS | Does B0 alone remove the corner-post stripe? | B0 removes the stripe for +3.2-3.7 % triangles. | `images/var_b0_sheet.jpg`, `images/var_b0_shot4.jpg` |
| V1 Frame | WAIT-RED | Does a built Office piece at every border sell the change? | Every border change now sits on a built piece. | `images/v_frame_vs_before.jpg`, `images/var_frame_sheet.jpg`, `images/v_frame_shot5.jpg`, `images/v_frame_shot6.jpg` |
| V1 Frame close frames | PASS | Are posts, wall ends, seams, doors and windows clean close up? | No stripe, no split cap, no bare seam close up. | `images/var_frame_close_sheet.jpg`, `images/var_frame_close_post.jpg`, `images/var_frame_close_wallend.jpg`, `images/var_frame_close_door.jpg` |

## 15. Open issues

1. **Free posts have no collider** (5 per 25 chunks): add a collider (map chat) or drop them.
2. **The base is a strong dark line** (`Cove_Base` 0.23 / 0.19 / 0.14) on every Office wall. Black, brown and grey were all 1990 options. Red to judge.
3. **Tidy.** Frame fixes the cut but adds no mood. It pairs with V5's cool Office colour (no dead cells needed).
4. **The B0.1 rule** differs from main's (§11.2).
5. **Door push side** must follow main's single-acting door data (§11.3).
6. **Stop vs leaf** is checked only with the leaf open (pre-render). Check the closed leaf against the 16 mm stop in Play.
7. **Draws** +23-28 kit renderers in view: fine on desktop; WebGL options in §12.

## 16. Logs and reproduction

SCR = the session scratchpad. Clone: `SCR/proj_trans_frame`. Unity calls through `SCR/fw3/run_unity.sh` (waits while another Unity holds the clone).

| Step | Command (method · args) | Log | Output |
|---|---|---|---|
| Basecheck | `FrontRoomsTransitionAudit.ShotsBatch -shots DOC/shots.json -tag basecheck -plan` | `SCR/fw2/basecheck.log` | `SCR/shots_frame_basecheck2` |
| Asset prep | `FrontRoomsTransitionAssets.Prepare` | `SCR/fw2/assets.log`, `assets2.log` | `Trans_AluminiumSatin.mat` |
| Kit off | `… ShotsBatch -tag off -plan -transMode off` | `SCR/fw2/off_final.log` | `SCR/shots_off_final` |
| B0 | `… ShotsBatch -tag b0 -plan -transMode b0` | `SCR/fw2/b0_final.log` | `SCR/shots_b0_final` |
| Frame iterations | `… ShotsBatch -tag frame -plan` | `SCR/fw2/frame_r1..r4.log` | `SCR/shots_frame_r1..r4` |
| Frame final | same | `SCR/fw3/frame_final.log` | `SCR/shots_frame_final` |
| Extras | `… ShotsBatch -shots SCR/fw2/extra_shots.json` | `SCR/fw2/extras_off.log`, `extras_r4.log` | `SCR/extras_off`, `SCR/extras_r4` |
| Close frames | `FrontRoomsTransitionAudit.EvidenceBatch -out SCR/evidence_frame4 -perType 3 -noCount` | `SCR/fw2/evidence_final.log` | `SCR/evidence_frame4` |
| Stats | `FrontRoomsTransitionStats.Run -shots DOC/shots.json -out …` | `SCR/fw2/stats_final.log` | `SCR/fw2/stats_final.txt` |
| Collision | `FrontRoomsTransitionCollisionCheck.Run -shots DOC/shots.json -out …` | `SCR/fw3/collision.log` | `SCR/fw3/collision.txt` |
| Sheets | `/usr/bin/python3 SCR/fw3/sheets.py` | | `SCR/fw3/out` |

## Appendix: full diff of `FrontRoomsMapWorld.cs` (clone base → frame)

```diff
--- proj_trans/Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs	2026-10-03 09:49:55
+++ proj_trans_frame/Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs	2026-10-03 17:22:46
@@ -199,6 +199,16 @@
             uvs.Add(uv / repeat);
         }
 
+        // TRANSITION frame: merge kit geometry (Office lining, portals, posts) into this block's wall mesh.
+        public void Append(FrontRoomsTransitionKit.TransitionMesh m)
+        {
+            var offset = vertices.Count;
+            vertices.AddRange(m.vertices);
+            normals.AddRange(m.normals);
+            uvs.AddRange(m.uvs);
+            foreach (var t in m.triangles) triangles.Add(t + offset);
+        }
+
         public Mesh ToMesh(string name)
         {
             var mesh = new Mesh { name = name };
@@ -800,6 +810,20 @@
             collision.Box(center, size, origin, 1f);
         }
 
+        // TRANSITION frame: B0 (corner ownership B0.1, grime band per side B0.2) and the
+        // V1 transition kit. With -transMode off the build is exactly today's.
+        var b0 = FrontRoomsTransitionKit.B0On;
+        var corners = b0 ? new Dictionary<GridCoord, FrontRoomsTransitionKit.TransitionCorner>() : null;
+        FrontRoomsTransitionKit.TransitionCorner CornerAt(GridCoord p, Vector3 local)
+        {
+            if (corners.TryGetValue(p, out var known)) return known;
+            var c = TransitionCornerAt(p);
+            c.at = local;
+            corners[p] = c;
+            return c;
+        }
+        var sink = FrontRoomsTransitionKit.FrameOn ? new TransitionSink { blockOf = BlockOf, officeWall = office.wall } : null;
+
         for (var j = 0; j < n; j++)
         for (var i = 0; i < n; i++)
         {
@@ -832,15 +856,72 @@
             // A wall's ends reach half a thickness past its corner, except into the start area.
             // The start area's side walls also stop on the door line, where the stream room's end wall closes the corner.
             var startSide = InStartArea(cell) != InStartArea(east);
+            // TRANSITION frame: each side's own height-class block (B0.2) and the edge descriptor for B0.1 and the kit.
+            int eastBlockA = BlockOf(i, j, eastHeight), eastBlockB = eastBlockA, northBlockA = BlockOf(i, j, northHeight), northBlockB = northBlockA;
+            FrontRoomsTransitionKit.TransitionEdge eastT = null, northT = null;
+            if (b0)
+            {
+                if (sink != null) { sink.i = i; sink.j = j; }
+                if (!InStartArea(cell) && !InStartArea(east))
+                {
+                    var hb = MapGrid.CeilingHeight(eastZone.height);
+                    eastBlockA = BlockOf(i, j, height);
+                    eastBlockB = BlockOf(i, j, hb);
+                    eastT = new FrontRoomsTransitionKit.TransitionEdge
+                    {
+                        kind = eastKind, a = cell, b = east, themeA = zone.theme, themeB = eastZone.theme, heightA = height, heightB = hb, height = eastHeight,
+                        start = new Vector3((i + 1) * cs, 0f, j * cs), along = Vector3.forward, across = Vector3.right, officeWall = office.wall,
+                        c0 = CornerAt(new GridCoord(cell.x + 1, cell.y), new Vector3((i + 1) * cs, 0f, j * cs)),
+                        c1 = CornerAt(new GridCoord(cell.x + 1, cell.y + 1), new Vector3((i + 1) * cs, 0f, (j + 1) * cs)),
+                    };
+                }
+                if (!InStartArea(cell) && !InStartArea(north))
+                {
+                    var hb = MapGrid.CeilingHeight(northZone.height);
+                    northBlockA = BlockOf(i, j, height);
+                    northBlockB = BlockOf(i, j, hb);
+                    northT = new FrontRoomsTransitionKit.TransitionEdge
+                    {
+                        kind = northKind, a = cell, b = north, themeA = zone.theme, themeB = northZone.theme, heightA = height, heightB = hb, height = northHeight,
+                        start = new Vector3(i * cs, 0f, (j + 1) * cs), along = Vector3.right, across = Vector3.forward, officeWall = office.wall,
+                        c0 = CornerAt(new GridCoord(cell.x, cell.y + 1), new Vector3(i * cs, 0f, (j + 1) * cs)),
+                        c1 = CornerAt(new GridCoord(cell.x + 1, cell.y + 1), new Vector3((i + 1) * cs, 0f, (j + 1) * cs)),
+                    };
+                }
+            }
             BuildEdge(chunk, eastKind, cell, east, new Vector3((i + 1) * cs, 0f, j * cs), Vector3.forward,
                 eastHeight, eastA, eastB, BlockOf(i, j, eastHeight), Get, Solid, origin,
                 !BothInStartArea(new GridCoord(cell.x, cell.y - 1), new GridCoord(cell.x + 1, cell.y - 1)),
-                !BothInStartArea(new GridCoord(cell.x, cell.y + 1), new GridCoord(cell.x + 1, cell.y + 1)) && !(startSide && cell.y + 1 == startArea.yMax));
+                !BothInStartArea(new GridCoord(cell.x, cell.y + 1), new GridCoord(cell.x + 1, cell.y + 1)) && !(startSide && cell.y + 1 == startArea.yMax),
+                eastBlockA, eastBlockB, collision, eastT, sink);
             BuildEdge(chunk, northKind, cell, north, new Vector3(i * cs, 0f, (j + 1) * cs), Vector3.right,
                 northHeight, northA, northB, BlockOf(i, j, northHeight), Get, Solid, origin,
                 !BothInStartArea(new GridCoord(cell.x - 1, cell.y), new GridCoord(cell.x - 1, cell.y + 1)),
-                !BothInStartArea(new GridCoord(cell.x + 1, cell.y), new GridCoord(cell.x + 1, cell.y + 1)));
+                !BothInStartArea(new GridCoord(cell.x + 1, cell.y), new GridCoord(cell.x + 1, cell.y + 1)),
+                northBlockA, northBlockB, collision, northT, sink);
 
+            // TRANSITION frame: the post at this cell's south-west corner. B0.1: where finishes
+            // differ and two or more walls meet, no piece enters the 0.16 m post square; it is
+            // built as four render-only quarters, each faced in the finish of the cell it faces.
+            // The kit adds free posts, end caps and corner guards there.
+            if (b0)
+            {
+                var c = CornerAt(cell, new Vector3(i * cs, 0f, j * cs));
+                if (!c.special && c.Mixed && c.Arms >= 2)
+                {
+                    var hq = c.MaxArmHeight;
+                    for (var q = 0; q < 4; q++)
+                    {
+                        var sx = (q & 1) != 0 ? 1f : -1f;
+                        var sz = (q & 2) != 0 ? 1f : -1f;
+                        Get(BlockOf(i, j, c.ceiling[q]), c.wall[q]).Box(c.at + new Vector3(sx * WallThickness * .25f, hq * .5f, sz * WallThickness * .25f),
+                            new Vector3(WallThickness * .5f, hq, WallThickness * .5f), origin, WallpaperRepeat);
+                    }
+                    FrontRoomsTransitionKit.Count.quarters += 4;
+                }
+                if (sink != null) FrontRoomsTransitionKit.Corner(c, sink, office.wall);
+            }
+
             if (data.pillar[i + j * (n + 1)] && !TouchesStartArea(cell.x, cell.y, cell.x, cell.y))
             {
                 // No column, cove or bulkhead reaches into the start area.
@@ -853,6 +934,16 @@
             if (!reserved) BuildFixture(chunk, cell, cellCenter, height, theme, data.lamp[index], data.tier);
         }
 
+        // TRANSITION frame: Office-wall kit geometry joins the block's wall mesh; the rest
+        // (steel, aluminium, base, guards, wall angles) gets its own renderers, no property block.
+        var kitRenderers = new List<(int block, Material material, bool shadows, FrontRoomsTransitionKit.TransitionMesh mesh)>();
+        if (sink != null)
+            foreach (var pair in sink.meshes)
+            {
+                if (pair.Key.material == office.wall && pair.Key.shadows) Get(pair.Key.block, office.wall).Append(pair.Value);
+                else kitRenderers.Add((pair.Key.block, pair.Key.material, pair.Key.shadows, pair.Value));
+            }
+
         for (var b = 0; b < builders.Length; b++)
         {
             if (builders[b].Count == 0) continue;
@@ -865,6 +956,23 @@
                 if (mesh != null) chunk.meshes.Add(mesh);
             }
         }
+        if (kitRenderers.Count > 0)
+        {
+            var kitRoot = new GameObject("Transition pieces");
+            kitRoot.transform.SetParent(chunk.root.transform, false);
+            foreach (var k in kitRenderers)
+            {
+                if (k.mesh.vertices.Count == 0 || k.material == null) continue;
+                var kgo = new GameObject("Transition " + k.material.name + (k.shadows ? "" : " (no shadow)"));
+                kgo.transform.SetParent(kitRoot.transform, false);
+                var kmesh = k.mesh.ToMesh("Chunk " + coord + " transition " + k.material.name, k.material.IsKeywordEnabled("_FR_MESH_UV"));
+                kgo.AddComponent<MeshFilter>().sharedMesh = kmesh;
+                var kr = kgo.AddComponent<MeshRenderer>();
+                kr.sharedMaterial = k.material;
+                kr.shadowCastingMode = k.shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
+                chunk.meshes.Add(kmesh);
+            }
+        }
         var col = new GameObject("Collision");
         col.transform.SetParent(chunk.root.transform, false);
         var shell = col.AddComponent<MeshCollider>();
@@ -936,6 +1044,50 @@
 
     bool BothInStartArea(GridCoord a, GridCoord b) => InStartArea(a) && InStartArea(b);
 
+    // TRANSITION frame: what meets at the lattice point that is the south-west corner of cell p.
+    FrontRoomsTransitionKit.TransitionCorner TransitionCornerAt(GridCoord p)
+    {
+        var c = new FrontRoomsTransitionKit.TransitionCorner { point = p };
+        var cells = new[] { new GridCoord(p.x - 1, p.y - 1), new GridCoord(p.x, p.y - 1), new GridCoord(p.x - 1, p.y), p };
+        for (var q = 0; q < 4; q++)
+        {
+            var z = Cache.ZoneOf(cells[q]);
+            c.theme[q] = z.theme;
+            c.ceiling[q] = MapGrid.CeilingHeight(z.height);
+            c.wall[q] = Theme(z.theme).wall;
+            if (InStartArea(cells[q])) c.special = true;
+        }
+        // Arms: W, E, S, N.
+        c.arm[0] = Cache.Edge(cells[0], cells[2]);
+        c.arm[1] = Cache.Edge(cells[1], cells[3]);
+        c.arm[2] = Cache.Edge(cells[0], cells[1]);
+        c.arm[3] = Cache.Edge(cells[2], cells[3]);
+        c.armHeight[0] = Mathf.Max(c.ceiling[0], c.ceiling[2]);
+        c.armHeight[1] = Mathf.Max(c.ceiling[1], c.ceiling[3]);
+        c.armHeight[2] = Mathf.Max(c.ceiling[0], c.ceiling[1]);
+        c.armHeight[3] = Mathf.Max(c.ceiling[2], c.ceiling[3]);
+        return c;
+    }
+
+    // TRANSITION frame: the kit's meshes for one chunk, keyed by block, material and shadow casting.
+    sealed class TransitionSink : FrontRoomsTransitionKit.ISink
+    {
+        public Func<int, int, float, int> blockOf;
+        public int i, j;
+        public readonly Dictionary<(int block, Material material, bool shadows), FrontRoomsTransitionKit.TransitionMesh> meshes =
+            new Dictionary<(int, Material, bool), FrontRoomsTransitionKit.TransitionMesh>();
+
+        public Material officeWall;
+
+        public FrontRoomsTransitionKit.TransitionMesh Mesh(Material material, float ceiling, bool castShadows)
+        {
+            // Office-wall pieces join their block's wall mesh (its _CeilingHeight); the rest is one renderer per chunk and material.
+            var key = (material == officeWall && castShadows ? blockOf(i, j, ceiling) : -1, material, castShadows);
+            if (!meshes.TryGetValue(key, out var m)) meshes[key] = m = new FrontRoomsTransitionKit.TransitionMesh();
+            return m;
+        }
+    }
+
     /// <summary>
     /// One 3 m edge from <paramref name="start"/> along <paramref name="along"/>.
     /// Where the two sides are different themes the wall is split in two
@@ -944,32 +1096,76 @@
     /// </summary>
     void BuildEdge(BuiltChunk chunk, EdgeKind kind, GridCoord a, GridCoord b, Vector3 start, Vector3 along, float height,
         Material wallA, Material wallB, int blockIndex, BuilderFn get, SolidFn solid, Vector3 origin,
-        bool mayExtendStart = true, bool mayExtendEnd = true)
+        bool mayExtendStart = true, bool mayExtendEnd = true,
+        int blockA = -1, int blockB = -1, MeshBuilder collision = null, FrontRoomsTransitionKit.TransitionEdge t = null, TransitionSink sink = null)
     {
-        if (kind == EdgeKind.Open) return;
+        // TRANSITION frame: the kit sees every edge (border pieces, Office base). Open edges stop here.
+        if (kind == EdgeKind.Open) { if (t != null) FrontRoomsTransitionKit.Edge(t, sink); return; }
         var length = MapGrid.CellSize;
         // Positive "across" points from cell a into cell b.
         var across = new Vector3(along.z, 0f, along.x);
-        void Piece(float from, float to, float bottom, float top, bool extendStart, bool extendEnd)
+        // TRANSITION frame: render extents at each end. 1 = B0.1 post face (the post is built as
+        // quarters), 2 = V1 end cap (inset for the Office lining). Collision keeps today's extents.
+        int EndMode(FrontRoomsTransitionKit.TransitionCorner cc) =>
+            cc == null || cc.special ? 0 : cc.Mixed && cc.Arms >= 2 ? 1 : FrontRoomsTransitionKit.EndCapAt(cc) ? 2 : 0;
+        var startMode = t != null ? EndMode(t.c0) : 0;
+        var endMode = t != null ? EndMode(t.c1) : 0;
+        void Piece(float from, float to, float bottom, float top, bool extendStart, bool extendEnd, float insetFrom = 0f, float insetTo = 0f, float raise = 0f)
         {
             if (from > 0f || !mayExtendStart) extendStart = false;
             if (to < length || !mayExtendEnd) extendEnd = false;
             var f = from - (extendStart ? WallThickness * .5f : 0f);
-            var t = to + (extendEnd ? WallThickness * .5f : 0f);
-            if (t - f < .05f || top - bottom < .05f) return;
-            var center = start + along * ((f + t) * .5f) + Vector3.up * ((bottom + top) * .5f);
-            if (wallA == wallB)
+            var t0 = to + (extendEnd ? WallThickness * .5f : 0f);
+            if (t0 - f < .05f || top - bottom < .05f) return;
+            var center = start + along * ((f + t0) * .5f) + Vector3.up * ((bottom + top) * .5f);
+            if (t == null)
             {
-                var size = along * (t - f) + across * WallThickness + Vector3.up * (top - bottom);
-                solid(blockIndex, wallA, center, Abs(size), WallpaperRepeat);
+                if (wallA == wallB)
+                {
+                    var size = along * (t0 - f) + across * WallThickness + Vector3.up * (top - bottom);
+                    solid(blockIndex, wallA, center, Abs(size), WallpaperRepeat);
+                    return;
+                }
+                var half = along * (t0 - f) + across * (WallThickness * .5f) + Vector3.up * (top - bottom);
+                solid(blockIndex, wallA, center - across * (WallThickness * .25f), Abs(half), WallpaperRepeat);
+                solid(blockIndex, wallB, center + across * (WallThickness * .25f), Abs(half), WallpaperRepeat);
                 return;
             }
-            var half = along * (t - f) + across * (WallThickness * .5f) + Vector3.up * (top - bottom);
-            solid(blockIndex, wallA, center - across * (WallThickness * .25f), Abs(half), WallpaperRepeat);
-            solid(blockIndex, wallB, center + across * (WallThickness * .25f), Abs(half), WallpaperRepeat);
+            // Collision: today's boxes, in today's order.
+            if (wallA == wallB) collision.Box(center, Abs(along * (t0 - f) + across * WallThickness + Vector3.up * (top - bottom)), origin, 1f);
+            else
+            {
+                var half = Abs(along * (t0 - f) + across * (WallThickness * .5f) + Vector3.up * (top - bottom));
+                collision.Box(center - across * (WallThickness * .25f), half, origin, 1f);
+                collision.Box(center + across * (WallThickness * .25f), half, origin, 1f);
+            }
+            // Render: B0.1 / cap ends, bead insets.
+            var rf = f;
+            var rt = t0;
+            if (from <= 0f && mayExtendStart) rf = startMode == 1 ? from + WallThickness * .5f : startMode == 2 ? from - WallThickness * .5f + FrontRoomsTransitionKit.BeadInset : rf;
+            if (to >= length && mayExtendEnd) rt = endMode == 1 ? to - WallThickness * .5f : endMode == 2 ? to + WallThickness * .5f - FrontRoomsTransitionKit.BeadInset : rt;
+            rf += insetFrom;
+            rt -= insetTo;
+            var rb = bottom + raise;
+            if (rt - rf < .005f || top - rb < .005f) return;
+            var rc = start + along * ((rf + rt) * .5f) + Vector3.up * ((rb + top) * .5f);
+            if (wallA == wallB && blockA == blockB)
+            {
+                get(blockA, wallA).Box(rc, Abs(along * (rt - rf) + across * WallThickness + Vector3.up * (top - rb)), origin, WallpaperRepeat);
+                return;
+            }
+            // B0.2: each skin in its own side's height-class block, even when both sides share a finish.
+            var skin = Abs(along * (rt - rf) + across * (WallThickness * .5f) + Vector3.up * (top - rb));
+            get(blockA, wallA).Box(rc - across * (WallThickness * .25f), skin, origin, WallpaperRepeat);
+            get(blockB, wallB).Box(rc + across * (WallThickness * .25f), skin, origin, WallpaperRepeat);
         }
 
-        if (kind == EdgeKind.Wall) { Piece(0f, length, 0f, height, true, true); return; }
+        if (kind == EdgeKind.Wall)
+        {
+            Piece(0f, length, 0f, height, true, true);
+            if (t != null) FrontRoomsTransitionKit.Base(t, sink);
+            return;
+        }
 
         float width, c, openingTop, sill = 0f;
         if (kind == EdgeKind.Arch)
@@ -990,11 +1186,26 @@
             openingTop = WindowTop;
             sill = WindowSill;
         }
-        Piece(0f, c - width * .5f, 0f, height, true, false);
-        Piece(c + width * .5f, length, 0f, height, false, true);
-        Piece(c - width * .5f, c + width * .5f, openingTop, height, false, false);
+        // TRANSITION frame: a border arch is cased (V1): the pieces beside it stop BeadInset short
+        // and the head starts BeadInset higher; the kit's Office lining fills the gap.
+        var bead = t != null && kind == EdgeKind.Arch && t.Border && FrontRoomsTransitionKit.FrameOn ? FrontRoomsTransitionKit.BeadInset : 0f;
+        Piece(0f, c - width * .5f, 0f, height, true, false, 0f, bead);
+        Piece(c + width * .5f, length, 0f, height, false, true, bead, 0f);
+        Piece(c - width * .5f, c + width * .5f, openingTop, height, false, false, -bead, -bead, bead);
         if (sill > 0f) Piece(c - width * .5f, c + width * .5f, 0f, sill, false, false);
 
+        // TRANSITION frame: the BuildEdge hook. Kind, cells, themes, heights, opening width,
+        // centre, top, sill, along, across and start go to the kit.
+        if (t != null)
+        {
+            t.width = width;
+            t.centre = c;
+            t.top = openingTop;
+            t.sill = sill;
+            FrontRoomsTransitionKit.Edge(t, sink);
+            FrontRoomsTransitionKit.Base(t, sink);
+        }
+
         if (kind == EdgeKind.Arch) return;
 
         // Frame: two jambs and a head in the trim colour.
```
