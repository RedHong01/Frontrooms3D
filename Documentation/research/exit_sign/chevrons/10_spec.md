# EXIT chevrons: spec (`_ChevL` / `_ChevR` of the hanging signs)

Date: 2026-10-22. Status: **SPEC DONE**. Build, verify and package follow (workflow `exit-sign-chevrons`, VL333–VL336 reserved).
Decision: Red, C23, 2026-10-22 (via 系统设计, verified): "C23 按游戏视觉的两步方案：现在门上常亮一块，楼梯间做完后在出口区域里挂箭头吊牌". Red, 2026-10-22: start every planned step now, whatever it costs in days. Step 1 (the LIT-locked door sign) is `70_lit_lock.md`. This is step 2, 游戏视觉's part: the chevron mesh variants of both hanging kits. 关卡设计 places them (C-3 ceiling placement, exit-zone route distance, chevron side).
Inputs read: `../10_spec.md` (§2.1, §2.2, §4.1, §4.3, §4.4, §8), `../20_build.md`, `../30_integration.md`, `../40_merged.md`, `../60_fix1.md`, `../70_lit_lock.md`, `../SOURCES.md`; main's `exit_sign_geom.py`, `exit_sign_common.py`, `exit_sign_hanging.py`, `exit_sign_hanging_flush.py`, `kitlib.py` (LOD and sidecar code), `build_asset.py`, `Tools/lookdev/gen_exit_sign.py` (the score and its tabs), the two hanging sidecars, `FrontRoomsKitLibrary.cs` (Spawn, Info), `FrontRoomsModuleEditing.cs` and `FrontRoomsLevelDesignerTests.cs` (palette, Ceiling kits); `systems.json` C23; the canon frame `room_visuals/images/run_ref_etb_levelbang_0094.jpg`.
Images (ours, built from `exit_sign_geom.py`, research support, no VL row): `images/ch01_two_face_rule.png`, `images/ch02_knockout_and_light.png`. Generator `tools/make_diagrams.py`; field numbers `tools/field_means.json`.
Nothing else was written. No Unity run, no clone yet, no download, no git write.

---

## 0. The short version

1. **The chevron is a punched knockout: a hole in the stencil plate, lit by the existing diffuser.** It is the face's own scored knockout (already printed in `ExitSign_Face_A` since 10-07), cut through the 0.9 mm plate with the letters' chamfer and stamped corner, plus three broken hold-tab stubs where the score leaves its tabs. No new material, texture, slot, shader, RenderSetup hunk or driver change.
2. **Size and place are unchanged numbers from `exit_sign_geom.py`:** 57.15 × 24.6 mm (2.25 in, w/h 0.43), stroke 10.0, centred on the letter band, 10.0 from the E or T. UL 924 (1989): outside the legend, ≥ 3/8 in from any letter, not easy to reverse, unused opening covered: all met. NFPA 101-1988 asks only for "a directional indicator". The 2.25 in size is the March 1991 NIST proposal (S2), measured on signs already on the market.
3. **One physical end is punched, on both faces.** `_ChevL` = the hole at local **+X**: the front face (+Z, `face_front`) reads **‹EXIT**, the back face reads **EXIT›**, and both point to local +X. `_ChevR` is the mirror (local −X; front EXIT›, back ‹EXIT). `_ChevR` is `_ChevL` turned 180° about Y.
4. **What the map picks:** `s = Dot(signWorldRotation * Vector3.right, d)`, with d the unit world direction toward the next cell on the exit route. `s ≥ 0.707` → `_ChevL`, `s ≤ −0.707` → `_ChevR`, otherwise no chevron sign. Which side the player approaches from never matters.
5. **Four new kits:** `Kit_ExitSign_Hanging_ChevL`, `…_Hanging_ChevR`, `Kit_ExitSign_HangingFlush_ChevL`, `…_HangingFlush_ChevR`. No `_Dead` variants. Tags `exit_sign`, `ceiling`, `exit_chevron` → placement Ceiling; minCeiling 2.65 / 2.27, as their parents.
6. **Budgets:** LOD0 = parent + ≤ 300 triangles (estimate +284), LOD1 ≈ 2,090 / 2,030, the hole kept in LOD1. No LOD2, like every sign kit; `lodDistances [5, 0, 0]`, never culled.
7. **Light:** the chevron sits far from both lamps. It is lit by the cavity fill: 0.22 × the "I" when LIT (0.43 × the E), and it is the brightest hole on BATTERY. That is period-honest, and it lands at hue ≈ 7° in the game's grade. Tests CH-9 and CH-10 decide whether this is enough. If not, fallback F1 brightens only diffuser regions that are hidden on every closed sign.

---

## 1. How the chevron is made: stencil knockout geometry

### 1.1 Decision

The open chevron is **geometry**: a through-hole in the stencil plate, built by `exit_sign_common.build_plate(kit, face, chevrons)`, which already takes `"left"` / `"right"` and passes them to `exit_sign_geom.hole_polys`. The light comes from the diffuser 6 mm behind the plate, through `ExitSign_Diffuser` and its lamp field (`_E`, `_F`), exactly as for the letters.

### 1.2 Why, and why not a face texture

| | Knockout geometry (chosen) | Printed or emissive chevron in a face texture (rejected) |
|---|---|---|
| Period truth | A 1990 universal sign is stamped with both chevron knockouts. The installer punches out the one needed (`../10_spec.md` §2.2). The hole is the same construction as the letters | A lit print on an opaque face is the panel look, the K1 failure that the stencil rebuild removed |
| Lamp states | HALF, LOOSE, BATTERY and DEAD reach the chevron through the same `_LampField` MPB, for free. BATTERY even gets a stronger tell (the DC lamps sit behind the ends) | Needs its own emission path. It could not follow the lamp field without a shader or driver change |
| Files and owners | Mesh and sidecar only. Nothing in `FrontRoomsRenderSetup.cs` (outlets_p4b, d16 tonight), `FrontRoomsExitSign.cs` or `FrontRoomsExitSignTests.cs` (exit-sign-lit-lock tonight) | A new face texture per side (+2 × ~5 MB BC7), a fifth slot or a variant material, and a RenderSetup SurfaceDef hunk |
| Hero read at 0.3 m | Parallax: the diffuser moves behind the hole; real chamfer and cut wall; tab stubs | Flat |
| Drift | Hole and printed score come from the same polygon (`letter_polys_open`), so they cannot drift apart | Two sources to keep aligned |
| Slots | 4, the kit rule | 5, or a material variant per kit |

### 1.3 The cut (hero details)

- **Polygon:** `exit_sign_geom.letter_polys_open()["<"]` or `[">"]`: 6 vertices; the inner notch is a reflex corner, filleted R 0.5 like the letters (`fillet_reflex`, 3 points). Same 0.2 front chamfer and 0.9 cut wall (`build_plate`). The cut walls face into the hole (`_fix_hole_walls`).
- **Broken hold-tabs (new):** `gen_exit_sign.py` leaves three 1.5 mm tabs unscored per chevron, at the midpoints of edges 0, 2 and 4 of the CCW reader-frame polygon. Reader mm: ‹ at (−127.06, −28.58), (−128.29, 14.29), (−139.36, 14.29); › mirrored. At each tab the hole gets a **stub of plate 1.5 wide × 0.35 proud into the hole**: 4 points, sharp corners, no fillet (a broken tab is not stamped). The stubs land exactly on the printed score's gaps.
- **Score line:** the face texture's 0.3 mm score (−25 % albedo, −0.08 mm height) now lies on the chamfer ring: the break line. Nothing to change.
- **Paint:** the knockout is cut, tabs and all, at stamping, before painting, so the cut walls carry enamel, as the letters' do (texture unchanged). Bare steel at the tab breaks would need a texture change. Not done: it is 1.5 × 0.35 mm.
- **The unused knockout** stays a scored outline (texture), as on the parents. UL 924's "unused openings covered" is met.
- **Code:** new `exit_sign_geom.chevron_tab_stubs(side)`. `hole_polys("left"|"right")` returns the filleted chevron with the stubs inserted after the fillet. `hole_polys("closed")` is byte-for-byte unchanged, so every existing kit rebuilds identically (CH-1).

---

## 2. Size and position against the codes (era lock 1990)

| Rule (period) | Requirement | Ours |
|---|---|---|
| NFPA 101-1988 §5-10 (S3, via S1) | "a directional indicator" where the way to the exit is not apparent; no shape or size given | chevron |
| UL 924-1989 (S4, via S1) | outside the legend | at the ends of the legend line |
| UL 924 | ≥ 3/8 in (9.525 mm) from any letter | **10.0** (`CH_GAP`) |
| UL 924 | not easy to reverse | a mesh variant per side, one module constant each; physically, a punched knockout cannot be closed again |
| UL 924 | unused openings covered | the other knockout stays scored and closed |
| UL 924 | stencil max/min ≤ 40 across the legend | **9.27** with the chevron (min 0.120 on the chevron, max 1.116 on the I; `tools/field_means.json`) |
| NISTIR 4532 (S2, March 1991) | chevron 2.25 in, proposed for the next NFPA 101; w/h 0.29–0.43 | **57.15** high × **24.6** wide (0.43); arm 11.07 across, stroke 10.0 square, slope 64.7° |

- **Era reading.** The 1991 report proposed a size for chevrons that universal signs of 1989–90 already carried as knockouts. So the form is period-correct for 1990, and the size is the best-measured figure near it. The canon frame (ETB Level !, VHS stamp MAR. 07 1991) shows a ceiling-hung "‹EXIT›".
- **We open one chevron, not both.** The canon frame lights both. UL 924 wants unused openings covered, and C22 gives one exit per floor, so a sign always points one way. No both-open variant is built.
- **Position:** vertically centred on the letters (chevron v 0.346–0.654 of the opening, centre = letter centre); x 13.9–38.5 mm from the opening's edge at each end. These are the positions the score is printed at today.
- **Screen size** (vFOV 76°, 1080 p): chevron 13.2 px tall at 3 m, 6.6 px at 6 m (stroke 1.15 px), 4.4 px at 9 m, 3.3 px at 12 m. Past about 6 m the direction reads from **which end glows** (CH-10), not from the shape. That fits C23's "short range, not a radar".
- **Typeface rule:** the arrow shape goes to 平面视觉 (50_for_red: "平面视觉 checks letterform and arrow"). Its weight (stroke 10.0 against the letters' 19.05) is the existing printed knockout. If 平面视觉 asks for a heavier chevron, that changes `CH_*` in `exit_sign_geom.py`, and with it the score in `ExitSign_Face_*` on every sign: a separate texture item, not this build.

---

## 3. Which face shows which chevron

### 3.1 Definition (the sign's own frame, Unity part space)

- Front = the face whose outward normal is **+Z** (anchor `face_front`). Seen from the front, **+X is the reader's left** (Unity is left-handed; `exit_sign_geom.py` header and `Face(..., r=(-1,0,0))`).
- **`_ChevL`**: the knockout at the **+X end** is punched on both faces.
  - front face: the left chevron (‹EXIT), `hole_polys("left")`;
  - back face (`face_back`, −Z, reader's right = +X): the right chevron (EXIT›), `hole_polys("right")`;
  - both point to **local +X**. Sidecar `pointLocal [1, 0, 0]`.
- **`_ChevR`**: the −X end. Front EXIT›, back ‹EXIT; both point to **local −X**. `pointLocal [-1, 0, 0]`.
- The suffix names what the **front** face reads. That is the same as the local direction the sign points (L = +X).
- **Lamps:** lamp A sits at local +X (anchor `lamp_a`), so `_ChevL`'s chevron is the lamp-A end. The back diffuser's UVs are mirrored, so both faces of one physical chevron sample the same field. Both chevrons sit so far from the lamps that **either HALF roughly halves either chevron** (0.126 → 0.060–0.066). The map needs no HALF-side rule.
- **Equivalence:** `_ChevR` = `_ChevL` turned 180° about Y, up to lamp naming and screw-slot angles. CH-3 checks it. Both names ship as asked, so the map can keep its own yaw convention.

### 3.2 What the map must pick (关卡设计; their files, a contract only)

```csharp
// d: unit horizontal WORLD direction from the sign toward the next cell on the exit route
// (the cell one step closer to the exit). ceilingH: the cell's ceiling height.
static string ChevronKit(Quaternion signWorldRotation, Vector3 d, float ceilingH)
{
    string body = ceilingH >= 2.65f ? "Kit_ExitSign_Hanging" : ceilingH >= 2.27f ? "Kit_ExitSign_HangingFlush" : null;
    if (body == null) return null;                                  // too low for any hung sign
    float s = Vector3.Dot(signWorldRotation * Vector3.right, d);
    if (s >= 0.7071f) return body + "_ChevL";
    if (s <= -0.7071f) return body + "_ChevR";
    return null;   // the route runs along the faces' normal: turn the sign 90 deg, or hang the closed parent (EXIT straight ahead)
}
```

- `signWorldRotation` is the sign's **world** rotation: `parent.rotation * Quaternion.Euler(0, yaw, 0)` for `FrontRoomsKitLibrary.Spawn(name, parent, pos, yaw, …)`. With yaw ψ and an unrotated parent, `right = (cos ψ, 0, −sin ψ)`:

| yaw ψ | faces look | d = +X | d = −X | d = +Z | d = −Z |
|---|---|---|---|---|---|
| 0° | ±Z | ChevL | ChevR | — | — |
| 90° | ±X | — | — | ChevR | ChevL |
| 180° | ±Z | ChevR | ChevL | — | — |
| 270° | ±X | — | — | ChevL | ChevR |

- **Hang:** origin on the ceiling plane, the sign hanging into −Y. Spawn at `y = MapGrid.CeilingHeight(cell)` (C-3), with `colliders: false`.
- **Orientation (recommended; the map's call):** local X along d, so the faces look across the route, along the wall that holds the opening, like a flag sign at a side door. Centre on the opening's centre line, at least 0.6 m into the cell from the wall face (the punched end then clears the wall by about 0.43 m). The faces then serve both approach directions along that wall, and both point at the opening. A player walking straight at the opening sees the sign edge-on, and sees the opening.
- **Heights:** Hanging housing 2.288–2.500 under 2.9 m; Flush 2.163–2.375 under 2.4 m. Both are above door tops (2.10) and arch tops (2.20), and above the 80 in headroom (2.032).
- **Never pointing at windows or glass:** with `p = signWorldRotation * pointLocal` (= ±right), the half-line in plan from the sign's centre along p must reach the route opening before any window, glazing or glass partition. The opening itself must not be a window. The exit-zone route is glass-free by C22's placement rule; this check also catches an incidental window beyond a turn.
- **Temperament** (C23, static per seed): `Configure(hash(seed, cell, edge), t, null, 0f)` with t = Lit 70 % / HalfA or HalfB 15 % / Loose 15 %, rolled once. **At most one LOOSE sign in view per room** (photosafety, `../10_spec.md` §8.5). No sign light: map signs carry none (`../10_spec.md` §8.6). The driver is attached by the kit library (tag `exit_sign`). REDUCE FLASHING is the driver's.
- **Label:** a name with none of wall, floor, carpet, ceiling, seal or partition (T11; for example `"exit chevron sign"`).
- **Open for 关卡设计 / Red:**
  - (a) **Tall zones (5.4 m).** The Hanging sign would sit at 4.79–5.00 m. Default: no chevron sign in Tall cells. Option: a P3 long-stem `Kit_ExitSign_HangingLong` (drop about 2.9 m).
  - (b) **Straight-through openings** (|s| < 0.707). Default: the closed parent `Kit_ExitSign_Hanging(Flush)`, which reads "EXIT, straight ahead" (`../10_spec.md` §4.4), or no sign.

---

## 4. Budgets, LODs and the sidecar

### 4.1 Triangles against the existing kits

| Kit | LOD0 | LOD1 | LOD2 | lodDistances |
|---|---|---|---|---|
| `Kit_ExitSign` (wall, reference) | 3,136 | 984 | none | [5, 0, 0] |
| `Kit_ExitSign_Hanging` (parent, built) | 6,502 | 1,999 | none | [5, 0, 0] |
| **`Kit_ExitSign_Hanging_ChevL/_ChevR`** | **≤ 6,802** (parent + ≤ 300; estimate 6,786); must stay inside the parent band 6,500 ± 15 % (`check_budget`) | **≤ 2,100** (ratio 0.3077; estimate 2,090) | **none** | **[5, 0, 0]** |
| `Kit_ExitSign_HangingFlush` (parent, built) | 6,254 | 1,940 | none | [5, 0, 0] |
| **`Kit_ExitSign_HangingFlush_ChevL/_ChevR`** | **≤ 6,554** (estimate 6,538); band 5,800 ± 15 % = ≤ 6,670 | **≤ 2,040** (ratio 0.3103; estimate 2,029) | **none** | **[5, 0, 0]** |

- **Δ per face (estimate):** chevron 8 points + 3 stubs × 4 = 20 points. Walls: 3 bands × 20 quads = 120 triangles. Plate front fill: about +22. That is about 142 per face, 284 per sign. The build measures it. If the Flush variant would leave its band, the stubs go to 2 points each (Δ about 200).
- **LOD1 keeps the hole.** The plate is under 12 mm thick, so kitlib marks it `fr_lod_keep` and the collapse never touches it. The direction must survive LOD1, because LOD1 starts at 5 m, where the chevron matters (CH-5, CH-12).
- **No LOD2, the same as the whole sign family.**
  - P-1 is approved (outlets_lods landed on 10-21). But the old 300-triangle LOD2 cannot hold the stencil plates: one plate with its holes is already about 300 triangles.
  - A LOD2 without holes shows either a solid red panel or no glow at all.
  - Two signs in one room should switch the same way, and the parents have no LOD2.
  - A family LOD2 (all 10 sign kits, hand-built, holes kept, about 1,000 triangles, `[5, 15, 0]`) is a separate P3 item, only if a perf need appears. Chevron signs exist only in the exit zone.
- **Never cull** (wayfinding, glows): the last entry stays 0.
- **Draws and memory:** 4 submeshes per sign, the same as the parents. Zero new textures. 4 FBX at about 0.3 MB each.

### 4.2 Sidecar (written by kitlib from the module; parents unchanged)

```json
"tags": ["exit_sign", "ceiling", "exit_chevron"],      // placement "Ceiling" (kitlib: tag "ceiling")
"minCeiling": 2.65,                                     // 2.27 for the HangingFlush pair
"lodDistances": [5.0, 0.0, 0.0],
"slots": ["ExitSign_Housing", "ExitSign_Face", "ExitSign_Diffuser", "Prop_Chrome"],
"noCollider": true,
"exitSign": { "...parent fields...": "unchanged",
  "chevrons": "left",            // what face_front shows: "left" (ChevL) | "right" (ChevR)
  "chevronBack": "right",        // what face_back shows
  "pointLocal": [1.0, 0.0, 0.0], // Unity part space; [-1, 0, 0] for ChevR
  "closedKit": "Kit_ExitSign_Hanging",
  "mirrorKit": "Kit_ExitSign_Hanging_ChevR",
  "tabStubs": 3 }
```

- `exit_chevron` lets 关卡设计 find the kits by tag (`Info.HasTag`). The palette already hides Ceiling kits (`PaletteKits`), so random module props never pick them.
- `FrontRoomsKitLibrary.Info` does not parse `exitSign` (JsonUtility ignores it), so nothing in C# changes. `chevrons` keeps the meaning it had in `../10_spec.md` §4.5, now defined as the front face.
- Anchors: the same as the parents (`ceiling`, `centre`, `face_front`, `face_back`, `stem_l/_r`, `lamp_a/_b`, `glow`). No `inspect` block (not inspectable, like the parents).
- **Names:** none of the 4 names, part names or anchors carries wall, floor, carpet, ceiling, seal or partition in an object name. The tag `ceiling` and the anchor `ceiling` are data, not objects, as on the parents.

---

## 5. Light through the chevron

From the analytic lamp field (`exit_sign_geom`: lamps A and B, fill 0.06 per lamp, DC pair 0.25), meaned over each hole polygon. The values match `ExitSign_Diffuser_F` (letters E/X/I/T 0.51 / 0.59 / 1 / 0.58, as in `../20_build.md`).

| Hole | LIT | HALF, A out | HALF, B out | BATTERY |
|---|---|---|---|---|
| ‹ (lamp A end) | **0.22** | 0.10 | 0.11 | **0.23** |
| E | 0.51 | 0.10 | 0.41 | 0.12 |
| I | 1.00 | 0.89 | 0.11 | 0.00 |
| › (lamp B end) | 0.21 | 0.11 | 0.10 | 0.23 |

(relative to the LIT "I"; full table in `tools/field_means.json`, chart in `ch02`)

- **LIT:** the chevron is lit by the cavity fill, not by a hot spot. Its emission is about 3.2 × 0.126 = 0.40 linear, which the grade maps to about sRGB (140, 36, 23): hue 6.8°, saturation 0.84, inside 平面视觉's targets (`../60_fix1.md` §3.3 ladder). It is the deepest red on the sign. In a dark room it reads as a lit red chevron. In a lit room it reads as a dark red shape on white enamel (UL contrast).
- **BATTERY:** the chevron is the brightest hole (0.23 against E 0.12). In a power failure, the arrow is what is left. That is a free tell.
- **Decision: keep the shared field; no texture change.** It is period-honest: real two-lamp signs have dim ends. It keeps the lit-lock and every closed sign untouched. CH-9 and CH-10 judge it.
- **Fallback F1 (only if CH-9 or CH-10 fails):**
  - In `Tools/lookdev/gen_exit_sign.py` (clone), add an end-return term to `_F`: channel R for u < 0.135 (the lamp-A end in front-reader UV), channel G for u > 0.865. Edges ramp inside the plate-only bands u 0.120–0.151 and 0.849–0.880.
  - Regenerate `_E` the same way. Size the term so the LIT chevron reaches ≥ 0.35 × I.
  - These regions are **behind the plate on every closed sign**, so no existing sign changes.
  - Gates: closed-sign renders identical within 1/255; the driver's Level weights (letter means) unchanged; textures replaced in place with their GUIDs; no RenderSetup or material change.
  - It is a shared-texture replace, so 游戏视觉 lands it after any pending exit-sign package.

---

## 6. Build plan (clone only)

- **Clone:** `W/tools/mkclone.sh proj_chev --feature exit_sign` (TOOLS.md §5). Compile with `-buildTarget OSXUniversal`; expect 0 `error CS`. Never two Unity processes on it. Edit-mode tools only (no Play), so the scripts-not-attached issue does not apply.
- **Blender** (4.3, `-b --factory-startup`, `sys.dont_write_bytecode`), clone copies under `Tools/Blender/frontrooms_kit/assets/`:
  - `exit_sign_geom.py`: `chevron_tab_stubs(side)`; tabbed chevron in `hole_polys("left"|"right")`; "closed" unchanged.
  - `exit_sign_common.py`: `exit_meta(kit, faces, chevrons="closed", chevron=None)` writes the §4.2 fields only when `chevron` is set. `build_face` already passes `chevrons`.
  - `exit_sign_hanging.py`: `build_hanging(kit, drop, budget, collars=True, ceiling_m=2.9, chevron=None)`. `chevron "L"`: front `"left"`, back `"right"`; `"R"`: the reverse. When set, it also adds the tag `exit_chevron`. Budget check against the parent target.
  - New thin modules: `exit_sign_hanging_chevl.py` (`NAME = "Kit_ExitSign_Hanging_ChevL"`), `exit_sign_hanging_chevr.py`, `exit_sign_hanging_flush_chevl.py`, `exit_sign_hanging_flush_chevr.py`. Each one is `build(kit)` → `build_hanging(…, chevron=…)`, with the parent's BUDGET, LOD1 ratio and SMOOTH_ANGLE.
  - `kitlib.py` and `build_asset.py` are untouched (material VARIANTS cannot make mesh variants, hence one module per kit).
- **Unity assets:** 4 × (`.fbx`, `.json`) in `Assets/Resources/Props/Models/`, with new `.meta` files whose GUIDs are not used in main (grep). The importer remaps the 4 existing materials.
- **Editor test (mergeable, new file):** `Assets/Editor/Rendering/FrontRoomsExitSignChevronTests.cs` (+ `.meta`). ASCII only, no C# tuples, no menu item, no InitializeOnLoad. `-executeMethod FrontRoomsExitSignChevronTests.RunBatch -quit`; output `Verification/exit_sign/chevrons/tests`. It calls no driver API beyond `GetComponent<FrontRoomsExitSign>()`, so the lit-lock edits tonight cannot break it.
- **Capture harness (clone-only, not merged):** `Assets/Editor/Rendering/FrontRoomsExitSignChevronCapture.cs`, from `../fix1/code/FrontRoomsExitSignFix1Capture.cs.txt`. ARGBHalf target with its own sRGB encode (VL268: never ARGB32 for graded numbers); ID pass; raw HDR pass. Measuring: a copy of `exit_sign_measure.py` with the chevron polygons, in `chevrons/code/`.
- **Not touched:** `FrontRoomsRenderSetup.cs`, `FrontRoomsExitSign.cs`, `FrontRoomsExitSignTests.cs`, the shader, any `.mat` or texture (unless F1), RoomStream, map files.
- **Package (later):** `W/apply/exit_sign_chevrons` (convention v2). M: `exit_sign_geom.py`, `exit_sign_common.py`, `exit_sign_hanging.py`. A: 4 modules, 8 asset files + 8 `.meta`, the test + `.meta`. Dry run first; `rebase_apply.sh` on a base mismatch.

---

## 7. Test plan

| # | Test | Where | Pass |
|---|---|---|---|
| CH-1 | **Parents unchanged.** Re-export `Kit_ExitSign_Hanging(_Dead)` and `…HangingFlush(_Dead)` with the edited modules | Blender, clone | sidecars byte-identical to main; LOD0/LOD1 mesh hash (positions 1e-6, indices, UVs, slots) identical; not shipped |
| CH-2 | **Geometry.** For each of the 4 kits | Blender | exactly one open chevron per face; the front hole at local +X (L) or −X (R); the back hole at the same physical end; hole = `hole_polys(side)` with 3 stubs on the texture's tab midpoints (±0.01 mm); cut walls face into the hole; 0 bad loops; Δ ≤ 300; LOD0/LOD1 inside §4.1 |
| CH-3 | **Mirror.** `_ChevR` against `_ChevL` turned 180° about Y | Blender or Unity | plate-hole vertices match within 1e-4 m |
| CH-4 | **Sidecar contract** | Editor test | tags {exit_sign, ceiling, exit_chevron}; placement Ceiling; minCeiling 2.65 / 2.27; lodDistances [5, 0, 0]; slots = the parent's; noCollider; the §4.2 chevron fields agree with each other and with the mirror kit |
| CH-5 | **Ray test through the imported meshes**, LOD0 and LOD1: from 0.5 m off each face, rays at each chevron centroid and at the E (control) | Editor test (ray–triangle over the mesh; submesh → material) | open end hits `ExitSign_Diffuser`; closed end hits `ExitSign_Face`; E hits Diffuser; on both faces, all 4 kits |
| CH-6 | **Pick rule.** The reference `ChevronKit` of §3.2 over yaw 0–345° (step 15°) × 8 directions, plus a parent yawed 90° | Editor test | the chosen kit's world pointing (`rot * pointLocal`) · d ≥ 0.707, else null; the §3.2 table reproduced exactly |
| CH-7 | **Spawn.** `FrontRoomsKitLibrary.Spawn` of each kit | Editor test | `FrontRoomsExitSign` attached; the diffuser renderers carry `ExitSign_Diffuser` on LOD0 and LOD1; 4 real materials (no preview fallback); spawned names clean (T11) |
| CH-8 | **Hero 0.3 m**, front and 35° oblique: `_ChevL` both faces, `_ChevR` front, LIT and DEAD | capture (HDR path) | hole, chamfer, stubs, score on the chamfer, diffuser parallax; no z-fight, no light leak at the cut |
| CH-9 | **Chevron light**, dark corridor (no sign light, troffers off), 1.5 m | capture + measure | LIT: chevron body hue 0–9°, sat ≥ 0.80, raw mean ÷ E raw mean 0.35–0.55 (field: 0.42); HALF A and B: ≥ 0.4 × the LIT chevron; BATTERY: chevron ≥ 1.5 × the E; max/min over all holes ≤ 40 |
| CH-10 | **Direction read**, dark corridor at 3 / 6 / 9 / 12 m, front and back; lit Lobby at 3 / 6 m | capture + measure | dark: open-end ÷ closed-end raw Y (projected chevron boxes dilated 2 px, letters masked) ≥ 5 at 3 m, ≥ 3 at 6 m, ≥ 2 at 9 m, 12 m reported; lit: (L_plate − L_chevron) ÷ L_plate ≥ 0.5 (UL), chevron hue ≤ 10° |
| CH-11 | **Two faces, one world.** `_ChevL` at yaw 0 shot from +Z and −Z | capture | the lit chevron's projected world point is on world +X in both frames (image left from the front, image right from the back) |
| CH-12 | **LOD1 and shadow.** LOD0 vs forced LOD1 at 5.5 m; a troffer 0.6 m off the sign, the shadow on the wall behind it | capture | chevron region mean within 5 %; the shadow matches the parent's within 1/255 (the chassis and diffuser back still close the hole) |
| CH-13 | **Regression** | clone | `FrontRoomsExitSignTests` all PASS (12/12, or 13/13 once lit-lock lands); `FrontRoomsLevelDesignerTests` PASS (Ceiling kits 4 → 8, palette unchanged); `roslyn_check.py --all` 0 errors, macOS and iOS; `.meta` GUIDs unused in main |
| CH-14 | **Sheets and sign-off** | three-view + review | `FrontRoomsThreeView` of the 4 kits (front, persp, top, side); 平面视觉 signs off the open chevron (shape, weight, stubs); era critic: no brand, no LED, no running man, one chevron, red |

### Verification log slots (VL333–VL336 only, specs to `W/vl_pending/exit_chevrons.md` when the images exist)

- **VL333** "Chevron is a punched knockout": CH-8 hero board.
- **VL334** "Both faces point the same way": CH-11, plus the CH-5 and CH-6 summary and ch01.
- **VL335** "Chevron reads at 6 m": CH-9 and CH-10, the distance ladder and state strip.
- **VL336** "Chevrons keep LOD1 and shadow": CH-12, CH-13 and the three-views.

---

## 8. Risks and notes

- **Dim chevron (§5)** is the main risk. F1 is ready and touches no visible pixel of any closed sign.
- **Stubs and triangle_fill:** the 4-point stubs sit on short edges near the chamfer offsets (−0.35 mm support ring). If `offset_poly` folds a stub, the stub shrinks to 0.3 proud or becomes a 2-point burr. CH-2 catches it.
- **Parallel work:** none of this stage's files is touched by exit-sign-lit-lock, outlets_p4b or d16. The three edited Blender modules are 游戏视觉-owned. The only packages that ever touched them (`exit_sign`, `exit_sign_fix1a`) are applied (`W/apply/MERGED.md`); the lit-lock payload (`W/es_litlock`) has only the two `.cs` files.
- **RT (G14):** the same as the parents (the EmissiveLens request is still open).
- **WebGL:** nothing new. The variants are ordinary kits; the WebGL track decides.
- **UNVERIFIED until the build:** triangle Δ, the LOD1 count, and the graded chevron colour (predicted from the fix1 ladder, not rendered).
