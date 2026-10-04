# G4 build note: windows (frame family + glass-track interface)

Status: DONE, 2026-10-03; re-verified in a second pass at 16:4x the same day (Red: "Try again", see §0). Six assets plus one variant, exported into the private clone `proj_int` only (`proj_int/Assets/Resources/Props/Models/`). Nothing under the real project's `Assets/` was touched, and Unity was not run.

Spec: `10_spec.md` §5, §2.3 (ranch casing), §9.4, §10.0, §10.4. Detail: `06_period_windows.md` §3–§4. Sweep pattern: `assets/interior_window.py`.

**Interface numbers for the glass track: none changed.** Every §5.2/§5.4 number is built and asserted exactly. One clarification the glass track must take into account is flagged in §2 below (the tape fills the "3 mm tape zone").

---

## 0. Second pass (2026-10-03 16:4x–17:xx, Red: "Try again")

The first pass's files were all in place and had been consumed by the window landing (`window_landing/01_integration.md`, PASS 38,538 / 0). So this pass re-did the build and the verification from scratch on the final modules, and fixed what the review found.

1. **Rebuilt all five modules** with `build_asset.py` into `proj_int` (log `scratchpad/g4/rebuild/build1.log`). Every triangle count matches §1, so the modules reproduce exactly.
2. **Fixed: the two blind turntables were empty.** `Kit_MiniBlind_Raised_a/_b.png` and `Kit_MiniBlind_Lowered_a/_b.png` showed only the floor.
   - Cause: kitlib's `preview()` stands every asset on a floor at Blender z = 0 (`kitlib.py:847-856`). The blinds' origin is the rail top (spec §9.4, origin P = `blind_rail`), so each blind hung under that floor.
   - Fix: `interact_window_common.lift_preview(kit, lift)` wraps `preview()` on that one kit instance. It lifts the finished mesh for the two stills and then restores the mesh and the bounds. kitlib is not edited.
   - `build_asset.py` previews after the FBX, the JSON and every variant are written (`build_asset.py:54-64`). Both blind JSONs are byte-identical before and after the fix (sha256 checked).
3. **Re-ran every §10.4 check on the final build, on LOD0 and on LOD1** (`scratchpad/g4/g4_check2.py`, results in `scratchpad/g4/checks/`). One check is new:
   - `fr_wear` coverage on the **joined** mesh. A part without the attribute would join with alpha 0, and black reads as fully worn.
   - Result: 0 unpainted elements in every asset (table below).
4. **Re-rendered every in-context close-up on the final build.** The first pass rendered some of them before its last three internal changes. Three screw shots are new:
   - `Kit_WindowFrame_Steel_screwslot_0p12m.png`: one jamb screw, square to its seat. The dome, the saw cut and the formed channel all read.
   - `_screwrow_0p3m.png`: the jamb stop at 0.3 m. At the real 216 mm centres only one screw fits in the frame, which is correct for the spec size (Ø 7 mm).
   - `_headscrews_0p3m.png`: the head stop seen from below.
5. **Anchors re-read from the exported JSON.** All 19 are exact (to 1e-6) in all four frame files. The blind anchors are as listed in §4.

| Second-pass check (final build) | Wood | Steel | Alu | Raised blind | Lowered blind |
|---|---|---|---|---|---|
| Tris LOD0 / LOD1 | 2,322 / 1,044 | 3,862 / 1,438 | 1,088 / 1,064 | 2,572 / 876 | 6,376 / 1,594 |
| (a) clear-zone vertices, LOD0 / LOD1 | 0 / 0 | 0 / 0 | ALU_A | — | — |
| (b) rays escaping from 2,126 trim points, LOD0 / LOD1 | 0 / 0 | 0 / 0 | ALU_B | — | — |
| (c) slab overlaps, LOD0 / LOD1 | 0 / 0 | 0 / 0 | ALU_C | — | — |
| (c) edge samples visible at 0° / 30° / 60°, both faces, LOD0 and LOD1 | 0 | 0 | ALU_E | — | — |
| (c) first exposure | 62° | 62° | ALU_F | — | — |
| (d) vertices inside the opening volume | — | — | — | RB_D | — |
| (e) lowest point over the opening | — | — | — | RB_E | — |
| `fr_wear` unpainted elements | 0 | 0 | ALU_W | RB_W | LB_W |

---

## 1. Assets

| Asset | Module | Tris LOD0 / LOD1 (budget §9.4) | Slots (submesh order) | Unity bounds (window root) | Priority |
|---|---|---|---|---|---|
| `Kit_WindowFrame_Wood` | `interact_window_frame_wood.py` | **2,322 / 1,044** (2,600 / 1,000: −11 % / +4 %) | WoodWalnut, Rubber | X ±0.800, Y 0.2485–2.0765, Z ±0.121 | P1 |
| `Kit_WindowFrame_Steel` | `interact_window_frame_steel.py` | **3,862 / 1,438** (3,600 / 1,300: +7 % / +11 %) | SteelBrown, Rubber | X ±0.775, Y 0.275–2.075, Z ±0.105 | P1 |
| `Kit_WindowFrame_Steel_Enamel` | VARIANT of the steel module | as steel | Door_Enamel, Rubber | as steel | P2 member, built now |
| `Kit_MiniBlind_Raised` | `interact_mini_blind_raised.py` | **2,572 / 876** (2,400 / 800: +7 % / +10 %) | SteelAlmond, PlasticWhite | local X ±0.775, Y −1.055–+0.005, Z −0.0475–+0.0287 (window root: Y 1.14–2.200, Z 0.080–0.156) | P1 (option) |
| `Kit_WindowFrame_Alu` | `interact_window_frame_alu.py` | **1,088 / 1,064** (2,800 / 1,000 ESTIMATE: −61 % / +6 %) | Aluminium, Rubber | X ±0.775, Y 0.275–2.075, Z ±0.105 | P2 |
| `Kit_MiniBlind_Lowered` | `interact_mini_blind_lowered.py` | **6,376 / 1,594** (6,000 / 1,500: +6 % / +6 %) | SteelAlmond, PlasticWhite | local X ±1.20, Y −1.303–+0.005, Z −0.0475–+0.0287 | P2 (decor) |
| — | `interact_window_common.py` | helper, no NAME | — | — | — |

The three LOD0 counts the task bounds (wood, steel, raised blind) are all inside ±15 %, and so is every LOD1 against §9.4's LOD1 column. The aluminium frame is far under its LOD0 ESTIMATE. A clip-on extrusion has no fasteners, mouldings or hardware to spend triangles on, so I left the budget unused rather than pad it.

Every asset:
- `kit.no_collider()` (sidecar `noCollider: true`, `colliders: []`) and no lights.
- An `fr_wear` point colour attribute on every part. It survives the FBX round trip on LOD0 and LOD1 as a CORNER byte colour (checked by re-import).
- The dominant slot first.
- `kit.meta["lodDistances"]` and `kit.meta["lodRatios"]`.
- `fr_lod2_drop` on screws, tape/compound/gasket, setting blocks, slat slabs, ladder cords and sill guards. Frames: tags `interactable`, `window`, `frame_wood` / `frame_steel` / `frame_alu`; LOD distances 4 / 12 / none. Blinds: tags `interactable`, `window`, `blind` (+ `decor` on the lowered one); LOD distances 3 / 10 / 30.

## 2. The glass-track interface (window root W, Unity metres)

Built and asserted in `interact_window_common.py` (module-level asserts tie the numbers to each other):

| Item | Value |
|---|---|
| Lining / soffit face | \|X\| 0.6995, head Y 1.9995, sill Y 0.3505 |
| Stop line = sight line | X ±0.6835, Y 0.3665 / 1.9835 |
| Stops | 16 × 16 on both faces, all four sides, Z ±(0.006 → 0.022) |
| Glass edge in the pocket | X ±0.6955, Y 0.354 / 1.996, glass Z ±0.003; bite 12.0 mm jambs, 12.5 mm head and sill |
| Slab | `meta["glassSlab"] = [1.391, 1.642, 0.006]`, anchor `glass_slab` (0, 1.175, 0) |
| Face band / casing | X 0.6995 → 0.775, Z 0.080 → 0.105 (the wood casing uses the shared §2.3 profile, 77 mm, so its outer edge is X 0.7765 and its head top Y 2.0765) |
| Extra sidecar field | `meta["glassInterface"]`: sight line, glass edge, bite, lining face, stop Z, tape Z, tooth band, pane-UV rule |

Anchors (all 19 verified exact in every frame's exported JSON): `glass_slab`; `stop_l/r/t/b` (∓0.6835, 1.175, 0), (0, 1.9835, 0), (0, 0.3665, 0); `pocket_l/r/t/b` (∓0.6955, 1.175, 0), (0, 1.996, 0), (0, 0.354, 0); `tooth_band_l/r/t/b` (∓0.600, 1.175, 0), (0, 1.900, 0), (0, 0.390, 0); `face_a` (0, 1.175, 0.105); `blind_rail` (0, 2.195, 0.1275); `sill_plant_a/b` (0, 0.3505, ±0.05); `floor_a/b` (0, 0, ±0.45).

> **FLAG for the glass track (not a changed number).** Each frame fills Z ±(0.0035 → 0.006) over the whole pocket band with tape, compound or gasket (`Prop_Rubber`), its front edge flush with the stop line. So the free play between the 6 mm slab and the tape is **0.5 mm per face**, not the 3 mm `10_spec.md` §5.4 assumes for the stage-2 push.
> - The tape is what hides the glass edge at 60°. A straight ray that slips under a bare 16 mm stop reaches the slab's back face 15.6 mm past the stop line, so without the tape the back 2 mm of the edge shows at 60°. With the tape, the edge stays hidden up to 61.6° (computed; measured: hidden at 61°, first exposure at 62°).
> - What the glass track must do: fracture pieces must stay within Z ±0.0035 inside the pocket band (\|X\| > 0.6835, Y > 1.9835, Y < 0.3665) and at the stop line. The simplest form is a push clamp that falls to ≤ 0.5 mm over the last ~20 mm before the stop line. Today's falloff, 3 mm × e^(−r/0.35) with the impact ≥ 0.2 m from the stop line, still gives up to 1.7 mm at the stop line, so it would sink the piece edges into the tape.
> - Real glass behaves the same way: the edge is held in the tape.
> - If the glass track needs more room, the tape can move back to 0.0038 (0.8 mm free play). The edge is then hidden only to about 60.5° at the jambs.

## 3. Self-checks (spec §10.4) — all pass

Run by `scratchpad/g4/g4_check.py` on the final build, exactly as `build_asset.py` builds it, **on LOD0 and again on LOD1** (`--lod1`). Results are in `scratchpad/g4/checks/*_check.json` and `*_LOD1_check.json`. The numbers are identical for both LODs of every frame.

| Check | Wood | Steel | Alu | Raised blind |
|---|---|---|---|---|
| (a) vertices inside X ±0.6835 × Y 0.3665–1.9835 (any Z) | 0 | 0 | 0 | — |
| (b) trim stand-in enclosed: 2,126 exposed sample points on the jamb trims (0.07 × 0.20, Y 0.35–2.0) and the head trim (1.54 × 0.07 × 0.20, Y 2.00–2.07), up to ~60 hemisphere rays each, against the frame plus a wall stand-in | 0 rays escape | 0 | 0 | — |
| (c) slab 1.391 × 1.642 × 0.006 at `glass_slab`: BVH overlaps | 0 | 0 | 0 | — |
| (c) clearances slab → frame | 0.5 mm per face, 3.5 mm at every edge | same | same | — |
| (c) edge samples visible (13,664 per view set: 4 edges × 61 × 7 points, 8 azimuths), face A and face B, at 0° / 30° / 60° | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | — |
| (c) first exposure | 62° | 62° | 61° (0.6 mm gasket lip) | — |
| Same checks on the LOD1 mesh | all pass | all pass | all pass | (d) 0 |
| (d) blind inside the opening volume (\|X\| < 0.70, Y < 2.0) | — | — | — | 0 vertices |
| (e) lowest point over the opening | — | — | — | Y 2.003 (sill guards); bottom rail 2.006 |
| Backface culling on the exported FBX (Workbench, culled): no visible face missing | yes | yes | — | — |

Module-level asserts (they run on every build): the §2.3 casing rule (w ≥ 0.0215 on u ∈ [0.0015, 0.0735]), the clear zone, the envelope, the 30-screw count and spacing (≤ 9" centres), the blind's opening clearance and lowest point, and that the blind stays in front of the frame's head band (Z > 0.105 below Y 2.076).

Four failures found and fixed on the way:
1. **Open mitre hairlines exposed the map trims.** The jamb trim's corner (X 0.70, Y 0.35 / 2.00) lies exactly on the 45° mitre plane, so any through-gap at a shell mitre shows it. Shell and casing mitres are now closed V-grooves: 0.3 × 0.3 mm on steel and alu, 0.2 mm on the wood casing, whose §2.3 round-over passes only 0.4 mm from that corner. Stops keep open 0.3–0.4 mm joints, because only the liner or soffit is behind them.
2. **Ear-clipped concave mitre caps bridged the glass pocket.** `bmesh.ops.triangulate` produced triangles outside the polygon. Caps are now filled with `mathutils.geometry.tessellate_polygon` in profile space.
3. **The LOD1 collapse opened see-through specks.** Two causes:
   - it opened the open seam between the wood casing and liner, so dots of the trim behind showed along the reveal;
   - dropping the tape at LOD1 left the empty pocket reading as a slot.

   Fixes:
   - the wood liner and both casings are now one welded sweep per side;
   - stops and tape/compound/gasket are protected with kitlib's own `fr_lod_keep` vertex group (`lod_keep()`; no kitlib edit) and stay in LOD1;
   - steel and the raised blind use a LOD1 ratio that needs no collapse at all.
4. **Pinning every seam starved the collapse.** It then crushed the wood stool and warped the aluminium sill. Pinning is now opt-in (`sweep_side(pin=True)`) and used only on the aluminium shell, which has no other part to starve. The aluminium sill's wear stations were removed, because collapsed they shaded as long slivers. Re-checked by rendering every LOD1 (`*_LOD1_4m.png`, `*_LOD1_1m_close_lod1.png`) and by running (a)–(c) on LOD1.

## 4. Per-asset notes

### `Kit_WindowFrame_Steel` (Office) + `_Enamel` (Run)
- **Section.** One pressed-steel section swept round four sides. 16 ga with 1.5 mm inside bends, so the convex arrises show R3.0 and the concave ones R1.5.
  - Face B: the integral formed stop.
  - Face A: a loose channel stop. Its sight-line face carries a formed 8.8 × 1.6 mm screw channel with **30 slotted oval-head screws**: 8 per jamb at Y 0.4175 … 1.9325 (216 mm centres) and 7 on the head and sill at X −0.6325 … 0.6325 (211 mm centres).
- **Screws.** Ø 7 mm, 1.5 mm dome, 0.8 mm slot, a random slot angle each, and three paint-filled slots. They are in the frame's paint (the 06 §3.2 LOD0 detail), so they share `Prop_SteelBrown`.
- **Why the channel.** The screws sit on the face a real loose stop is screwed through, which faces the opening. A 1.5 mm dome on a flat stop face would break the sight line (check a), so the domes sit in the channel with their tops 0.1 mm behind the stop line.
- **Tape and blocks.** Black tape on both faces, and two neoprene setting blocks.
- **LOD1 (0.38).** Screws and setting blocks drop, the tape stays, and nothing needs decimating.
- **Variant.** `_Enamel` swaps `Prop_SteelBrown` → `Door_Enamel` (registered with an almond preview colour).
  - **Open:** `Resources/Surfaces/Door_Enamel.mat` does not exist yet (P-4).
  - Until it does, `FrontRoomsKitLibrary.ApplyMaterials` (`Assets/Scripts/Office/FrontRoomsKitLibrary.cs:215-239` in `proj_int`) keeps the FBX's embedded material. The importer only remaps a slot when the surface material exists (`Assets/Editor/Rendering/FrontRoomsKitImporter.cs:36-46`).
  - The spec's `Painted_Metal` fallback has to be done Unity-side.
- Renders:
  - `Kit_WindowFrame_Steel_faceA_1p5m.png`, `_faceB_1p5m.png`;
  - `_corner_0p3m.png` (the sill/jamb corner on face A with the screw row, the V hairline and the tape line);
  - `_screws_0p3m.png` (looking along the jamb stop);
  - `_headB_0p3m.png`;
  - `_cull_*.png` (culled FBX).

### `Kit_WindowFrame_Wood` (Lobby)
- **Casing.** The shared §2.3 ranch casing with back band and quirk on both faces, asserted, with V-hairline head mitres (0.2 mm). The jamb casings land on the stool.
- **Liners.** Walnut liners over the reveal. Casing A, the liner and casing B are one welded sweep per side.
- **Stool.** A through-stool with horns, X ±0.800, Z ±0.121, Y 0.3255–0.3505, with a 12.5 mm half-round nosing returned round the horn ends (10 segments). It is the climb-plant surface, gridded at 11 × 5 stations for the hand-grime wear at the centre.
- **Apron.** On both faces, made from the casing profile, X ±0.7765, with returned ends.
- **Stops.** 16 × 16 walnut stops with a 3 mm quirked ovolo and a 0.5 mm ease at the sight line.
- **Compound and blocks.** A black compound line and two setting blocks.
- **Grain.** `fr_grain` runs along each piece.
- **LOD1 (0.45, 1,044 tris).** The setting blocks drop. The stops and compound are protected. The casing, stool and apron lose their wear stations and some arc segments.
- **Deviation.** The apron runs Y 0.2485–0.3255, because the §2.3 profile is 77 mm wide (06 wrote Y 0.250 → 0.3255).
- Renders:
  - `Kit_WindowFrame_Wood_faceA_1p5m.png`, `_faceB_1p5m.png`;
  - `_stoolA_0p3m.png` (horn, nosing, casing foot, apron);
  - `_headmitreA_0p3m.png`;
  - `_stopcornerB_0p3m.png`;
  - `_cull_*.png`.

### `Kit_MiniBlind_Raised` (Office, face A, optional)
- **Origin.** The frame's `blind_rail` anchor; place it with identity rotation relative to the frame.
- **Head rail and mounting.** A 35 × 35 head rail in box brackets on spacer blocks, from the wall Z 0.080 to the bracket back at 0.1085, above the head band.
- **Stack.** A raised stack, Y 2.035–2.160, of 12 grouped slabs. Each holds 7 nested crowned slats with real noses and gaps on the front face, and each slab carries a little scatter. A plain stack block hidden inside the slabs is what LOD1 shows when the slabs drop.
- **Ladders.** 4 ladders (Levolor table: 4 for 60–84") with slack cord loops on the stack front.
- **Bottom rail.** 38 × 29 with end caps and two sill guards.
- **Wand and cords.** A 9 mm hex wand at X +0.72 down to Y 1.14. Two 1.4 mm lift cords at X +0.74 to a tassel ending at Y 1.20. Both hang beside the opening (\|X\| ≥ 0.715) and in front of the face band (Z ≈ 0.15).
- **Anchors.** `blind_rail` (0, 0, 0), `wand_tip`, `tassel`, `stack_bottom`, plus `meta["mount"]`.
- Renders:
  - `Kit_MiniBlind_Raised_blind_1p5m.png`;
  - `_bracket_wand_0p4m.png`;
  - `_stack_0p3m.png`;
  - `_wand_tassel_0p4m.png` (on the steel frame FBX).
- **Era note.** The Levolor table gives a 50" wand for a 67–82" blind. This blind's drop is about 71", but the spec's Y 1.14 is a 40" wand, so I kept the spec. The period-exact end would be about Y 0.88. That is Red's call.

### `Kit_WindowFrame_Alu` (Exit, P2)
- **Section.** A clear-anodised wrap section with a 6 × 2.5 mm shadow groove 40 mm from the opening edge (floor Z 0.1025, still clear of the trim face at 0.100). It has crisp 0.5 mm arrises and V-hairline mitres.
- **LOD1 (0.60, 1,064 tris).** The shell's mitre rings and wall edges are pinned, and the beads and gaskets are protected, so LOD1 is close to LOD0. That is cheap enough at about 1,100 LOD0 triangles.
- **Beads and gaskets.** 45° snap beads on both faces; black vinyl gasket wedges with a 0.6 mm lip; setting blocks.
- Renders: `Kit_WindowFrame_Alu_faceA_1p5m.png`, `_faceB_1p5m.png`, `_corner_0p3m.png`, `_faceband_0p3m.png`.

### `Kit_MiniBlind_Lowered` (decor for `Kit_InteriorWindow`, G5, P2)
- **Overall.** 2.40 m wide with a 1.30 m drop.
- **Slats.** 56 real crowned slats, 0.25 mm thick, at 22 mm pitch, tilted 45° front edge down.
- **Cords.** 5 ladders with a rung under every slat, and 3 lift cords.
- **Rails and controls.** Bottom rail with caps and sill guards; a 30" wand and a tassel at the right end.
- **Origin.** It uses the raised blind's frame convention: the rail top centre on the rail's mid plane. G5 sets the stand-off on `Kit_InteriorWindow`.
- `meta["decorOnly"]` says it must never go on a breakable window.
- Renders: `Kit_MiniBlind_Lowered_lowered_2m.png`, `_slats_ladder_0p4m.png`, `_bottom_wand_0p5m.png`.

All renders are in `scratchpad/interact_previews/G4/`.
- The kit's own turntables (`<Asset>_a.png`, `_b.png`) come from the final `build_asset.py` run.
- The in-context Cycles close-ups were rendered before the last three internal changes. Those changes are the welded wood liner, the removed alu sill stations and the steel setting blocks hidden in the pocket; none alters what LOD0 looks like.
- `*_LOD1_*.png` and `*_LOD0cmp_*.png` are the LOD1 and LOD0 comparisons (Workbench, backface-culled, on the exported FBX). The close-ups are rendered in context: a wall with the opening, the map trims in red (they must never show), the 6 mm stand-in slab as real glass, a floor and a ceiling. Materials use the Unity surfaces' measured mean albedo (`10_spec.md` §7.3).

## 5. The ray-traced glass track (spec §7), from the windows' side

The track is `10_spec.md` §7: a native Metal plugin with its own BLAS/TLAS, `G14` in the visual queue. I re-read the controller (`Assets/Scripts/Rendering/FrontRoomsMetalGlassRT.cs`, real project, read-only) and confirmed the three registration rules that touch these assets:

1. **`:149` registers every enabled MeshRenderer.**
   - All three frames and both blinds ship LOD0 and LOD1. A LODGroup does not disable the inactive LOD's renderer, so each would enter the TLAS twice.
   - This is **G14-K1**: register LOD0 only, or the active LOD.
2. **`:192` reads only `sharedMaterial` (submesh 0).**
   - Every module adds its dominant slot first, so reflections see steel, walnut or aluminium. Only the black tape line (submesh 1) is missing in reflections. That is **G14-K2**.
3. **`:322` shades hits by `_BaseColor`.**
   - `Prop_SteelBrown` and `Prop_WoodWalnut` keep their colour in `_BaseMap`, so the dark frames would reflect as white.
   - On a window this is the worst case: the frame is the object nearest the glass and the first thing a grazing reflection shows.
   - This is **G14-K3**. The measured means are in `10_spec.md` §7.3: SteelBrown sRGB (58, 45, 36), WoodWalnut (73, 47, 31), Aluminium (184, 183, 180).

This kit models no glass. The RT target belongs on the visible pane: the glass track's pane, or the map's interim slab at `glass_slab`. It must never go on the disabled `Window pane` cube (**G14-K5**). No new task row is needed from G4 beyond §7.5's K1–K5. G4 confirms K1–K3 apply to these assets.

## 6. Open items

1. **The tape free play (§2 flag):** the glass track's push clamp in the pocket band.
2. **`Door_Enamel.mat` (P-4):** the `_Enamel` variant needs it, or the Unity-side `Painted_Metal` fallback.
3. **Wand length:** the spec's 40" against the Levolor table's 50" (above).
4. **The bent slat (HR03):** not built. A raised stack hides it, so it needs either a slat sticking out of the stack or the lowered decor blind. Red's call.
5. **Hammered glass and wired glass:** glass-track or Red items (06 §6). The frames are unchanged by either.
6. **Screw spacing:** the first and last screws on each side sit 51 mm from the stop-line corner (the spec numbers), 0.2 mm over the UH spec's "2 inches" (50.8 mm). I kept the spec values.
7. **LOD2 and switch distances:** declared only. `kitlib` exports LOD1 today, and LOD2 waits for P-1. `fr_lod2_drop` is set on the parts listed above.
   - For P-1's author: `make_lod1`'s global collapse opens seams between separate pieces and redistributes the reduction when vertices are protected.
   - A per-part ratio, or a boundary-locked collapse, would let future modules skip the welding and pinning workarounds above.
8. **Preview directory:** the computed task's build line said `interactables_prev/g3`, which looks copied from G3. I used `interact_previews/G4`, which the task's BUILD section names. (Same in the second pass.)
9. **The S1 stop band still needs the map chat's yes.**
   - `00_map_constraints.md` says glazing and frame "must stay outside the 1.4 × (0.35–2.0) opening" once the pane is gone.
   - Every frame here puts 16 mm of render-only stop inside that edge, by design: spec §5.2, S1. The alternative, S0, loses the stops.
   - Spec §6.6 item 2 asks for the clarification. The window landing measured no gameplay effect: the climb and the Relay probe pass at 411 / 411 windows. Until the map chat confirms, treat S1 as pending.
10. **Kit turntables of hanging assets.** Any future asset whose origin sits above its geometry (blinds, signs hung from a rail) hits the same empty-turntable problem. `lift_preview()` is the module-side workaround. A kitlib option, such as a preview floor at the bounds minimum, would be the clean fix (NEEDS APPROVAL, visual chat).
