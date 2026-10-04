# G4 build note: windows (frame family + glass-track interface)

Status: **pass 3, 2026-10-03 22:0x–23:xx** (correction pass). Red: "Codex has already landed part of this work in the main project; correct the errors and finish everything that is not done." Earlier passes: pass 1 (12:xx) built the six assets; pass 2 (16:4x–17:4x) fixed the empty blind turntables and stopped before its check table was filled in. This pass re-read the spec, audited what landed in the main project, fixed the errors listed in §0.2, rebuilt everything into the private clone `proj_int`, and re-ran every §10.4 check. The main project's `Assets/` was not touched, and Unity was not run.

Spec: `10_spec.md` §5, §2.3 (ranch casing), §9.4, §10.0, §10.4. Detail: `06_period_windows.md` §3–§4. Sweep pattern: `assets/interior_window.py`.

**Interface numbers for the glass track: none changed.** Every §5.2/§5.4/§5.5 number is built and asserted, and all 19 anchors in every frame sidecar match the spec to 1e-6. The old "tape free play" FLAG is now a note, because §5.4 already allows what it warned about (§2).

---

## 0. Pass 3: what landed, what was wrong, what changed

### 0.1 What the main project holds (read-only audit, 22:0x)

- **All six G4 assets are in `Assets/Resources/Props/Models/`.** They landed in commit `8ef5b64` (19:10) and the lowered blind in `9e754a2` (19:33, "Complete clone asset promotion").
  - The FBXs are byte-identical to the 16:45–16:48 clone builds.
  - **The JSONs were hand-edited**: `"placement": "Floor"` became `"Wall"` in all six (`VISUAL_CHAT_TASKS.md` line 29: "the main project's Wall-kit placement convention"). Nothing else in them differs.
- **The window facade is live.** `Assets/Scripts/Office/FrontRoomsInteractableKit.Window.cs` spawns `Kit_WindowFrame_Wood` (Lobby) and `Kit_WindowFrame_Steel` (Office) (`:44`, `:100`). The map calls it for every window (`FrontRoomsMapWorld.cs:1300`). The alu frame and the enamel variant are listed but not used yet (no Run or Exit windows).
- **S1 is approved.** The facade records it: "the stop band is the 16 mm inside the wall cut (S1, approved by the map chat 2026-10-03)" (`FrontRoomsInteractableKit.Window.cs:30-31`). This closes pass 2's open item 9.
- **The wood frame's glazing line swaps to putty in Unity.** The facade swaps `Prop_Rubber` → `Resources/Surfaces/Prop_Putty` for W-L0 once that surface exists (`:104`, `:133`). The mesh keeps `Prop_Rubber`, as §5.1 says.
- **The importer now reads `lodDistances` from the sidecar** (`Assets/Editor/Rendering/FrontRoomsKitImporter.cs:73-90`, landed in `8ef5b64`). See §0.3 A: it culls 2-LOD kits too early.

### 0.2 Errors found and fixed (module sources; rebuilt into `proj_int`)

| # | Error | Fix | Effect on the landed files |
|---|---|---|---|
| 1 | **The "Wall" placement was a hand patch.** kitlib derives `placement` only from tags (`kitlib.py:820`). Any rebuild of the modules (by the visual chat, the three-view stage, or a re-promotion) would write `"Floor"` again, and the Level Designer palette would offer window frames and blinds as floor furniture again (`window_landing/02_tests_frames.md` §6 item 3). | New `interact_window_common.wall_placement(kit)` adds the `wall_decor` tag, kitlib's own route to `"Wall"`. It is called by all five modules. Runtime code never reads `placement` or that tag; only the editor palette and the map tests do (grep of `Assets/`). | JSON: `placement` stays `"Wall"`, `service` stays 0.0; `tags` gain `wall_decor` |
| 2 | **"No cull" was written as JSON `null`** in the frames' `lodDistances` (`[4, 12, null]`). `FrontRoomsKitLibrary.Info.lodDistances` is a `float[]` documented as "0 means never cull" (`FrontRoomsKitLibrary.cs:59`). Whether `JsonUtility` accepts a `null` in a float array is UNVERIFIED; G1's door sidecars avoid it with `-1.0`. | `lod_meta()` writes `None` as `0.0`. The frames now carry `[4.0, 12.0, 0.0]`. The blinds were already numeric (`[3, 10, 30]`). | JSON only |
| 3 | **Steel screw slots were too narrow.** They were 0.8 mm wide. A #6 slotted oval head (Ø ≈ 6.6–7 mm, like these) has a slot of about 1.0–1.2 mm (standard machine-screw tables; UNVERIFIED in this pass, no page opened). The landing report found the screws "legible only with the inspection light" at 0.3 m (`02_tests_frames.md` §6 item 5). | `SLOT_W = 0.0011` (1.1 mm). Same triangle count. The heads stay in the frame's paint, which is period-right for field-painted hollow metal. | Steel + Enamel FBX LOD0: slot walls move 0.15 mm; LOD1 identical |
| 4 | **The alu frame's LOD1 was 98 % of LOD0** (1,064 of 1,088 tris): the beads, gaskets and pinned shell left the collapse nothing to remove. So the LODGroup saved nothing and added the importer's cull (§0.3 A). | `LOD1 = None`, which §1.8/§9.4 allow but do not require. With no collapse to fear, the wear stations come back: 9 on the sill (the climb plant: palms, rubbed arrises, boot scuffs) and 6 per jamb at grab height. Two plain soffit points at Z ±0.0225 carry a dirt line at the bead foot. **Tris 1,088 → 2,066.** | Alu FBX: one mesh, no `_LOD0/_LOD1`; JSON tris |
| 5 | Docstrings were wrong in four places. Steel and alu said the sill band starts at Y 0.2745; the swept 0.0755 face gives 0.275. The raised blind said "9 mm hex" for the spec's "Ø 8 mm hex" (it is 7.8 across flats, 9.0 across corners). The steel docstring did not explain the face-B stop root bend that the landing measured at \|Z\| 0.0224 (it is the 1.5 mm inside bend where the stop meets the soffit; it is clear of the glass and the clear zone). | Docstrings corrected. | none |
| 6 | **This note** left pass 2's check table as placeholders (`ALU_A`, `RB_D`…) and kept two stale items: the tape FLAG asked for more than §5.4 does, and S1 was listed as pending. | §3 holds the full pass-3 results. §2 corrects the tape note. §6 is updated. | — |

### 0.3 Found, not mine to fix (flagged)

**A. The importer culls every 2-LOD kit at its d12 distance, not at dcull** (visual chat; `FrontRoomsKitImporter.cs:81-86`; NEEDS APPROVAL; UNVERIFIED in Unity, found by reading the code).

- **How it happens.** The importer sets `lods[i]` from `lodDistances[i]`. kitlib exports only LOD0 and LOD1 today (P-1 not landed), so `lods[1]` is the last LOD, and its transition is the cull. It gets `lodDistances[1]`, which the spec defines as the LOD1→LOD2 switch (§1.8: `(d01, d12, dcull)`), not the cull.
- **What breaks:**

  | Kits | Cull | Spec cull |
  |---|---|---|
  | G4 window frames (wood, steel) | 12 m | none |
  | G4 blinds | 10 m | 30 m |
  | G1 door frames and leaves (`[4, 12, -1]`) | 12 m | none |

  Standalone Ultra doubles these distances (lodBias 2). In a long tall hall the frames would vanish at 12–24 m while the glass slab, which has no LOD, stays. That is the landing's finding 2 again, at a shorter distance.
- **The fix (one line in the importer):** for the last exported LOD, read the last sidecar entry: `var distance = i == lods.Length - 1 ? authored.lodDistances[authored.lodDistances.Length - 1] : authored.lodDistances[i];`.
- **Why I did not work around it in the data.** That would mean writing `[4, 0]` instead of the spec's triple. It would hide the bug only for G4, and it would lose d12 for P-1.
- **Is the live game affected yet?** UNVERIFIED. It depends on whether the landed window FBXs were re-imported after the importer change. Their `.meta` files are dated 18:55; the importer landed in the 19:10 commit.

**B. Tracked `__pycache__`.** `Tools/Blender/frontrooms_kit/assets/__pycache__/*.pyc` are tracked in git. Every kit build rewrites them, so `git status` shows them modified after any build. Housekeeping for whoever owns `.gitignore`.

### 0.4 What to re-promote into the main project

Copy from `proj_int/Assets/Resources/Props/Models/`. Keep the main project's `.meta` files; they hold the GUIDs.

| File | Why | Mesh change vs landed (Blender re-import, `g4/pass3/fbx_cmp_final.json`) |
|---|---|---|
| `Kit_WindowFrame_Steel.fbx` + `.json` | slots 1.1 mm; `wall_decor` tag; `lodDistances` 0.0 | LOD0 max vertex delta 0.15 mm (slots only); LOD1 identical |
| `Kit_WindowFrame_Steel_Enamel.fbx` + `.json` | as steel | as steel |
| `Kit_WindowFrame_Alu.fbx` + `.json` | no LOD1; wear stations; 2,066 tris | new mesh (one object, no LOD group) |
| `Kit_WindowFrame_Wood.json` | `wall_decor` tag; `lodDistances` 0.0 | FBX geometry identical (delta 0.0); copying it is harmless (header timestamp and IDs only) |
| `Kit_MiniBlind_Raised.json`, `Kit_MiniBlind_Lowered.json` | `wall_decor` tag | FBX geometry identical (delta 0.0) |

Final clone hashes (sha256, first 12):

| Asset | FBX | JSON |
|---|---|---|
| Wood | `8925071c0e80` | `73baed13e611` |
| Steel | `d0ec04e5a95a` | `bd956ed25b2a` |
| Steel_Enamel | `78fcc8f5c673` | `c24e089de171` |
| Alu | `1e7c93522e6b` | `89c28425fe7e` |
| MiniBlind_Raised | `f571722a9def` | `72aefdeeb142` |
| MiniBlind_Lowered | `c21f42d8cf9b` | `89bd5aeb892a` |

The module sources in `Tools/Blender/frontrooms_kit/assets/interact_window_*.py` and `interact_mini_blind_*.py` are edited in place and show as modified in git. They are the source of truth: a rebuild reproduces the table above.

---

## 1. Assets

| Asset | Module | Tris LOD0 / LOD1 (budget §9.4) | Slots (submesh order) | Unity bounds (window root) | Priority |
|---|---|---|---|---|---|
| `Kit_WindowFrame_Wood` | `interact_window_frame_wood.py` | **2,322 / 1,044** (2,600 / 1,000: −10.7 % / +4.4 %) | WoodWalnut, Rubber | X ±0.800, Y 0.2485–2.0765, Z ±0.121 | P1, live (Lobby) |
| `Kit_WindowFrame_Steel` | `interact_window_frame_steel.py` | **3,862 / 1,438** (3,600 / 1,300: +7.3 % / +10.6 %) | SteelBrown, Rubber | X ±0.775, Y 0.275–2.075, Z ±0.105 | P1, live (Office) |
| `Kit_WindowFrame_Steel_Enamel` | VARIANT of the steel module | as steel | Door_Enamel, Rubber | as steel | P2 member, built |
| `Kit_MiniBlind_Raised` | `interact_mini_blind_raised.py` | **2,572 / 876** (2,400 / 800: +7.2 % / +9.5 %) | SteelAlmond, PlasticWhite | local X ±0.775, Y −1.055–+0.005, Z −0.0475–+0.0287 (window root: Y 1.14–2.200, Z 0.080–0.156) | P1 option, not placed (Red's yes) |
| `Kit_WindowFrame_Alu` | `interact_window_frame_alu.py` | **2,066 / none** (2,800 / 1,000: **−26.2 %**, see below) | Aluminium, Rubber | X ±0.775, Y 0.275–2.075, Z ±0.105 | P2 |
| `Kit_MiniBlind_Lowered` | `interact_mini_blind_lowered.py` | **6,376 / 1,594** (6,000 / 1,500: +6.3 % / +6.3 %) | SteelAlmond, PlasticWhite | local X ±1.20, Y −1.303–+0.005, Z −0.0475–+0.0287 | P2 decor |
| — | `interact_window_common.py` | helper, no NAME | — | — | — |

**Budget deviation (flag): the aluminium frame is 26 % under its LOD0 budget,** outside §10.0's ±15 %.
- Everything §5.1 and `06` §3.4 list is built: the wrap section, the shadow groove, 0.5 mm arrises, the 45° snap beads, the gasket wedges with their lip, the setting blocks, the V-hairline mitres, and now the wear stations.
- A clip-on anodised extrusion has no fasteners or mouldings left to spend triangles on. I did not pad it.
- `06` estimated about 1,500 for this member.

**Every asset:**
- render-only: `kit.no_collider()`, so the sidecar has `noCollider: true` and `colliders: []`; no lights;
- `fr_wear` on every part (0 unpainted elements on the joined mesh, §3), exported as a CORNER byte colour (checked by re-import);
- the dominant slot first;
- `kit.meta["lodDistances"]` and `["lodRatios"]`, with `fr_lod2_drop` on screws, tape/compound/gasket, setting blocks, slat slabs, ladder cords and sill guards;
- placement `"Wall"` (§0.2 item 1).

**Tags:**
- frames: `interactable`, `window`, `frame_wood` / `frame_steel` / `frame_alu`, `wall_decor`;
- blinds: `interactable`, `window`, `blind` (+ `decor` on the lowered blind), `wall_decor`.

**LOD distances:** frames 4 / 12 / none (written `0.0`); blinds 3 / 10 / 30.

## 2. The glass-track interface (window root W, Unity metres)

These numbers are built and asserted in `interact_window_common.py`, whose module-level asserts tie them to each other. They are exported as `meta["glassSlab"]` and `meta["glassInterface"]`.

| Item | Value |
|---|---|
| Lining / soffit face | \|X\| 0.6995, head Y 1.9995, sill Y 0.3505 |
| Stop line = sight line | X ±0.6835, Y 0.3665 / 1.9835 |
| Stops | 16 × 16 on both faces, all four sides, Z ±(0.006 → 0.022) |
| Glass edge in the pocket | X ±0.6955, Y 0.354 / 1.996, glass Z ±0.003; bite 12.0 mm at the jambs, 12.5 mm at head and sill |
| Slab | `meta["glassSlab"] = [1.391, 1.642, 0.006]`, anchor `glass_slab` (0, 1.175, 0) |
| Face band / casing | X 0.6995 → 0.775, Z 0.080 → 0.105 (the wood casing uses the shared §2.3 profile, 77 mm, so its outer edge is X 0.7765 and its head top Y 2.0765) |
| Pane UV | u = X + 0.6955, v = Y − 0.354 |

**Anchors** (all 19 exact in all four frame sidecars, `g4/pass3/verify_sidecars.out`):
- `glass_slab` (0, 1.175, 0);
- `stop_l/r/t/b` (∓0.6835, 1.175, 0), (0, 1.9835, 0), (0, 0.3665, 0);
- `pocket_l/r/t/b` (∓0.6955, 1.175, 0), (0, 1.996, 0), (0, 0.354, 0);
- `tooth_band_l/r/t/b` (∓0.600, 1.175, 0), (0, 1.900, 0), (0, 0.390, 0);
- `face_a` (0, 1.175, 0.105);
- `blind_rail` (0, 2.195, 0.1275);
- `sill_plant_a/b` (0, 0.3505, ±0.05);
- `floor_a/b` (0, 0, ±0.45).

**Note for the glass track (no number changed): the tape fills the tape zone.**
- **What the frames build.** Each frame fills Z ±(0.0035 → 0.006) over the pocket band with tape, compound or gasket (`Prop_Rubber`, opaque, render-only). Its front edge is flush with the stop line. The slab therefore has 0.5 mm of free play per face before it touches the tape, and 3 mm before the stop face.
- **Why the tape is there.** It hides the glass edge at 60°. A straight ray that slips under a bare 16 mm stop reaches the slab's back face 15.6 mm past the stop line. With the tape, the edge stays hidden up to 61.6° (computed). Measured: hidden at 61°, first exposure at 62° (alu: 61°).
- **What the glass track may do.** §5.4 lets the stage-2 push (0.5–3 mm toward the far side, falling off as e^(−r/0.35 m); `../glass/destruction/04_unity_implementation.md` table row "2 Spiderweb") reach "the 3 mm tape zone up to the stop face". Inside the hidden pocket band, a pushed piece may sink up to about 1.2 mm into the opaque tape. It reads as compressed tape, and nothing shows.
- **The hard limit is the stop face, \|Z\| 0.006.** No piece may pass it.
- **Optional.** If the glass track wants zero mesh intersection, clamp the push to ≤ 0.5 mm over the last 20 mm before the stop line. This is optional; pass 2 called it required.

## 3. Self-checks (spec §10.4), pass 3, final build

Run by `scratchpad/g4/pass3/g4_check3.py` (pass 2's checker, plus a shot filter and face-based blind checks). It builds each module exactly as `build_asset.py` does and checks LOD0, then LOD1 (`--lod1`). Results are in `scratchpad/g4/pass3/checks/*_check.json`.

| Check | Wood | Steel | Alu | Raised blind | Lowered blind |
|---|---|---|---|---|---|
| Tris LOD0 / LOD1 | 2,322 / 1,044 | 3,862 / 1,438 | 2,066 / — | 2,572 / 876 | 6,376 / 1,594 |
| (a) vertices inside X ±0.6835 × Y 0.3665–1.9835, any Z, LOD0 / LOD1 | 0 / 0 | 0 / 0 | 0 / — | — | — |
| (b) rays escaping: 2,126 exposed points on the trim stand-in (jambs 0.07 × 0.20, Y 0.35–2.0; head 1.54 × 0.07 × 0.20, Y 2.00–2.07), ≤ 60 hemisphere rays each, against frame + wall stand-in, LOD0 / LOD1 | 0 / 0 | 0 / 0 | 0 / — | — | — |
| (c) slab 1.391 × 1.642 × 0.006 at `glass_slab`: BVH overlaps, LOD0 / LOD1 | 0 / 0 | 0 / 0 | 0 / — | — | — |
| (c) clearance slab → frame: face A / face B / every edge | 0.50 / 0.56 / 3.5 mm | 0.50 / 0.56 / 3.5 mm | 0.51 / 0.52 / 3.5 mm | — | — |
| (c) visible edge samples (13,664 per set: 4 edges × 61 × 7, 8 azimuths) at 0° / 30° / 60°, face A and face B, LOD0 and LOD1 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 | — | — |
| (c) first exposure | 62° | 62° | 61° (0.6 mm gasket lip) | — | — |
| (d) faces over the opening (\|X\| < 0.70) below Y 2.0, LOD0 / LOD1 | — | — | — | RB_D | (decor; n/a) |
| (e) lowest point over the opening, LOD0 / LOD1 | — | — | — | RB_E | (decor; n/a) |
| `fr_wear` unpainted elements on the joined mesh (LOD0) | 0 of 1,386 | 0 of 2,462 | 0 of 1,264 | RB_W | LB_W |
| Sidecar: anchors, `glassSlab`, tags, `placement` "Wall", no collider, `lodDistances` (`verify_sidecars.py`) | OK | OK (and Enamel) | OK | OK | OK |

**Module-level asserts** run on every build:
- the §2.3 casing rule (w ≥ 0.0215 on u ∈ [0.0015, 0.0735]);
- the clear zone and the envelope;
- the 30-screw count and spacing (≤ 9" centres);
- the blind's opening clearance and lowest point;
- the blind stays in front of the frame's head band (Z > 0.105 below Y 2.076).

**Renders (§10.4 f).** In context: a wall with the opening, the map trims in red (they must never show), the 6 mm stand-in slab as real glass, a floor and a ceiling. Materials use the Unity surfaces' measured mean albedo (`10_spec.md` §7.3). All are in `scratchpad/interact_previews/G4/`:

| Asset | Renders |
|---|---|
| Steel (pass 3, 1.1 mm slots) | `Kit_WindowFrame_Steel_faceA_1p5m.png`, `_faceB_1p5m.png`, `_corner_0p3m.png`, `_screws_0p3m.png`, `_screwrow_0p3m.png`, `_screwslot_0p12m.png`, `_headscrews_0p3m.png`, `_headB_0p3m.png` |
| Alu (pass 3) | `Kit_WindowFrame_Alu_faceA_1p5m.png`, `_faceB_1p5m.png`, `_corner_0p3m.png`, `_faceband_0p3m.png`, `_sill_0p5m.png` |
| Wood (pass 2; the LOD0 mesh is identical, delta 0.0) | `Kit_WindowFrame_Wood_faceA_1p5m.png`, `_faceB_1p5m.png`, `_stoolA_0p3m.png`, `_headmitreA_0p3m.png`, `_stopcornerB_0p3m.png` |
| Raised blind | `Kit_MiniBlind_Raised_blind_1p5m.png`, `_bracket_wand_0p4m.png`, `_stack_0p3m.png` (pass 2, identical mesh); `_wand_tassel_0p4m.png` (pass 3) |
| Lowered blind | `Kit_MiniBlind_Lowered_lowered_2m.png` (pass 3); `_slats_ladder_0p4m.png`, `_bottom_wand_0p5m.png` (pass 1, identical mesh) |
| Every asset | kit turntables `<Asset>_a.png` / `_b.png` from this pass's `build_asset.py` run |
| Wear and culling | `*_wear.png` (the exported `fr_wear` colours, Workbench); `*_cull_*.png` (backface-culled FBX) |

## 4. Per-asset notes

### `Kit_WindowFrame_Steel` (Office) + `_Enamel` (Run)
- **Section.** One pressed-steel section swept round four sides: 16 ga, 1.5 mm inside bends, so the convex arrises show R3.0 and the concave ones R1.5. Welded-and-ground mitres show as closed 0.3 × 0.3 mm V hairlines, because a through-gap would expose the map trims' corners.
  - Face B: the integral formed stop. Its root bends reach \|Z\| 0.0224 next to the soffit (intended, §0.2 item 5).
  - Face A: a loose channel stop. Its sight-line face carries a formed 8.8 × 1.6 mm channel with **30 slotted oval-head screws**: 8 per jamb at Y 0.4175 … 1.9325 (216 mm centres) and 7 on head and sill at X −0.6325 … 0.6325 (211 mm centres). The first and last screws are 51 mm from the corners.
- **Screws.** Ø 7 mm, 1.5 mm dome, **1.1 mm slot**, random slot angles, three paint-filled slots, in the frame's paint. The channel keeps the domes 0.1 mm behind the stop line (check a) while the screws stay on the face a real loose stop is screwed through.
- **Tape and blocks.** Black glazing tape on both faces; two neoprene setting blocks.
- **LOD1 (0.38).** The screws and blocks drop, the tape stays, and nothing decimates.
- **Enamel variant.** It needs `Resources/Surfaces/Door_Enamel.mat` (P-4). Until that exists the importer keeps the FBX's embedded material, and `Painted_Metal` is a Unity-side fallback.

### `Kit_WindowFrame_Wood` (Lobby)
- **Casing.** The shared §2.3 ranch casing on both faces (asserted), with 0.2 mm V-hairline head mitres. Casing A, the liner and casing B are one welded sweep per side, so no seam can open under the LOD1 collapse.
- **Stool.** A through-stool with horns: X ±0.800, Z ±0.121, Y 0.3255–0.3505, with a 12.5 mm half-round nosing returned round the horns. Its top is the climb plant. It is gridded 11 × 5 so palm grime can sit at the centre.
- **Apron.** On both faces: the casing profile, X ±0.7765, returned ends.
- **Stops.** 16 × 16 walnut stops with a 3 mm quirked ovolo and a 0.5 mm ease at the sight line.
- **Compound and blocks.** A compound line (`Prop_Rubber`; the facade swaps it to `Prop_Putty`) and two setting blocks.
- **Deviation.** The 77 mm §2.3 profile puts the bounds at Y 0.2485–2.0765, not the spec's 0.250–2.075 (1.5 mm at each end).

### `Kit_WindowFrame_Alu` (Exit, P2)
- **Section.** A clear-anodised wrap section with a 6 × 2.5 mm shadow groove 40 mm from the opening edge. Its floor is at Z 0.1025, still clear of the trim face at 0.100. Crisp 0.5 mm arrises; V-hairline mitres.
- **Beads and gaskets.** 45° snap beads; black vinyl gasket wedges with a 0.6 mm lip; setting blocks.
- **New this pass:** no LOD1, plus wear stations: palms, rubbed arrises and boot scuffs on the sill, grab grime on the jambs, cavity grime in the groove and at the bead foot (`Kit_WindowFrame_Alu_wear.png`).

### `Kit_MiniBlind_Raised` (Office face A, optional)
- **Origin.** The frame's `blind_rail` anchor; place it with identity rotation relative to the frame.
- **Head rail.** 35 × 35, in box brackets on spacer blocks.
- **Stack.** Y 2.035–2.160: 12 grouped slabs with real slat noses, and a plain block for LOD1. 4 ladders with slack loops at about 21 mm pitch.
- **Bottom rail.** 38 × 29 with caps and sill guards (lowest point 2.003).
- **Wand and cords.** A hex wand 7.8 mm across flats at X +0.72 to Y 1.14; cords and tassel at X +0.74 to Y 1.20. Both hang beside the opening (\|X\| ≥ 0.715).
- **Anchors.** `blind_rail` (0, 0, 0), `wand_tip` (0.72, −1.055, 0.0245), `tassel` (0.74, −0.995, 0.0225), `stack_bottom` (0, −0.189, 0), all local. `meta["mount"]` names the frame.
- **Era note.** The spec's Y 1.14 is a 40" wand. The Levolor table gives 50" for this drop (about Y 0.88). Red's call.

### `Kit_MiniBlind_Lowered` (decor for `Kit_InteriorWindow`, G5, P2)
- **Overall.** 2.40 m wide, 1.30 m drop.
- **Slats.** 56 real crowned slats at 22 mm pitch, tilted 45°.
- **Cords and controls.** 5 ladders with rungs, 3 lift cords, a 30" wand and a tassel.
- **Anchors.** `blind_rail` (0, 0, 0), `wand_tip`, `tassel`, `bottom_rail`.
- `meta["decorOnly"]`: it must never go on a breakable map window.

## 5. The ray-traced glass track, from the windows' side (unchanged from pass 2)

`Assets/Scripts/Rendering/FrontRoomsMetalGlassRT.cs` (read-only):
- **`:149` registers every enabled MeshRenderer.** The wood, steel and blind LODGroups would put both LODs in the TLAS (**G14-K1**).
- **`:192` reads only submesh 0.** Every module adds its dominant slot first, so only the black tape line is missing in reflections (**G14-K2**).
- **`:322` shades hits by `_BaseColor`.** Dark frames would reflect white (**G14-K3**). The measured means are in `10_spec.md` §7.3.

This kit models no glass. The RT target belongs on the visible pane, never on the disabled `Window pane` cube (**G14-K5**).

## 6. Open items

1. **Importer 2-LOD cull** (§0.3 A): visual chat; one line; NEEDS APPROVAL.
2. **Re-promote** the files in §0.4. Owner: whoever runs the main-project merge (visual chat or Codex).
3. **The alu frame is −26 % on budget** (§1): accept it, or name the extra real detail wanted.
4. **`Door_Enamel.mat`** (P-4) for the `_Enamel` variant; `Prop_Putty` for the wood glazing line (the facade waits for it).
5. **Wand length:** the spec's 40" against the Levolor table's 50" (Red).
6. **The bent slat (HR03):** not built. A raised stack hides it. Red's call.
7. **Raised blind placement:** built, not placed. It needs Red's yes and a share by edge hash (`06` §3.2).
8. **Hammered or wired glass:** glass-track or Red items (`06` §6). The frames do not change for either.
9. **LOD2:** declared only (`fr_lod2_drop`, `lodRatios`). It waits for P-1.
10. **Hanging-asset turntables:** `lift_preview()` is the module-side workaround. A kitlib preview floor at the bounds minimum would be the clean fix (NEEDS APPROVAL).
11. **Preview directory:** the task's build line said `interactables_prev/g3` (copied from G3). I used `interact_previews/G4`, as its BUILD section says.

Closed this pass: S1 (approved, §0.1); the "Floor" placement (§0.2 item 1); the steel face-B \|Z\| 0.0224 measurement (intended, §0.2 item 5); the landing's "G4 is still iterating" finding (the frames' 16:45 FBXs were final; this pass's changes are listed in §0.2).

## 7. History

- **Pass 1 (12:xx). Four defects fixed.**
  1. Open mitre hairlines exposed the trim corners; they became closed V-grooves.
  2. Ear-clipped concave caps bridged the pocket; they are now tessellated in profile space.
  3. The LOD1 collapse opened see-through specks; fixed by welded wood sweeps, `lod_keep()` on stops and tape, and no-collapse LOD1 ratios.
  4. Pinning every seam starved the collapse; pinning became opt-in.
- **Pass 2 (16:4x).**
  - The two blind turntables were empty, because kitlib's preview floor sits at the blinds' rail-top origin. `lift_preview()` fixes this on the kit instance, after export.
  - Anchors were re-read and three screw close-ups re-rendered.
  - Its check table was left unfilled.
- **Pass 3 (this note):** §0.
