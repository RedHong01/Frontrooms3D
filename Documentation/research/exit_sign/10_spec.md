# EXIT sign: research and spec (model, textures, materials, behaviour, placement)

Date: 2026-10-03, 22:3x. Status: **SPEC DONE**. Build, integrate, critic and merge follow (`exit-sign` workflow).
Request (Red, ~18:1x): "Update the Exit warning sign too: the model, the textures and so on."
Standing orders: highest spec (hero LOD0 for 0.3 m inspection, plus LOD1/LOD2); desktop is the reference, WebGL is a separate track; era lock 1990; merge visual work as you go, verified first.

Images (all ours, constructed from the period numbers; support only, no external media): `images/es01_face_today_vs_1990.png`, `es02_lamp_states.png`, `es03_mounts.png`, `es04_letter_levels.png`, numbers in `es_diagrams_report.json`. Generator: `tools/make_diagrams.py`. Sources: `SOURCES.md` (S1–S12). Wanted media: `media_candidates.md` (E1–E12, nothing downloaded).

---

## 0. The short version

1. **A 1990 US EXIT sign is a stencil face, not a printed panel.** A 0.9 mm steel plate has the letters cut through it. A red plastic diffuser sits 6 mm behind it. Two 20 W lamps behind that make two hot spots. Letters are 6 in (152 mm) with 3/4 in (19 mm) strokes (NFPA 101-1988, UL 924-1989, both via the August 1990 NIST study S1).
2. **What we have is a printed panel at half size.** The game's Run signs are code-built boxes with a 76 mm "EXIT" texture. The kit's `Kit_ExitSign` (in main since Codex's commit `8ef5b64`) reuses that texture at 96 mm. Its strokes are 1:5.0; period signs were 1:8 to 1:12 (S7).
3. **The mirror bug is real and wider than reported.** `Run_ExitSign` has no smoothness map, so its smoothness is 1.0. The **non-letter part of the face is a perfect mirror whether the sign is lit or not**: emission covers only the letters. The code-built housings use `Office_BlackedGlass` (0.95), a black mirror (11a M1). `Kit_ExitSign_Dead` points at a material that does not exist, so it would show a blank grey face with no letters (derived from code).
4. **Spec:** one shared Blender module family. A stencil plate with real letter holes, a diffuser with a measured hot-spot field, real seams and screws, and four mounts: wall (P1), hanging double-face (P1, for the Run room), flush (P2) and end/flag (P2). New surfaces `ExitSign_Housing`, `ExitSign_Face`, `ExitSign_Diffuser` (+ `_Dead`). Smoothness 0.42 / 0.50 / 0.72. Emission only on the diffuser, only when lit.
5. **Behaviour:** a small driver, `FrontRoomsExitSign`. Five states: LIT, HALF (one lamp burnt out), LOOSE (one lamp drops out now and then), BATTERY (mains lost: two 5 W DC lamps near the ends, a different hot-spot pattern), DEAD. The odds follow the room's decay (RoomStream rule, map lamp mode). The red light follows the sign. REDUCE FLASHING removes every drop-out. Default flicker stays ≤ 2 flashes/s.
6. **Placement:** RoomStream Run (2 hanging signs, as today), RoomStream Exit (1 wall sign over the far door, new). No signs in Level 0 rooms (canon). Map: module props work with no contract once the sidecar is right. Two small contracts are proposed (§10.3).

---

## 1. Inputs, partial work and the state of main

- **Partial work from the interrupted attempt (17:3x–17:55) was continued, not redone.** It left four diagrams in `images/`, a diagram script and two NIST report texts in `scratchpad/exitsign_research/`. I re-checked all of it. Fixed:
  - the lamp field was too flat (within-letter variation 0.10–0.33 against NIST's 0.5–0.74), so it was re-fitted (§6.3);
  - the battery lamps were "2 × 3.6 W"; the period drawing says two 5 W DC lamps (S6);
  - today's stroke ratio was "1:5.7"; measured on the texture it is 1:5.0;
  - the hanging drop is now 0.40 m, so the bottom sits in the IES 2.0–2.3 m band (S5).
- **Main after Codex (git `7320ed1` → `75cfdff`):**
  - `Kit_ExitSign` and `Kit_ExitSign_Dead` (FBX + JSON) came in with `8ef5b64`. They are byte-identical to `proj_int` (sha1 `24ba79c2…`, `9c1a4869…`). Red's editor generated their `.meta` files at 18:57 with fresh GUIDs; `proj_3view_int` holds different ones. **Main's four GUIDs are canonical; keep them** when the rebuilt files are merged:
    - `Kit_ExitSign.fbx` `3892f75ef3c8f4dcdad5b4027b459e6f`, `Kit_ExitSign.json` `112bc21dd5dc740749da99d8fbb0056f`;
    - `Kit_ExitSign_Dead.fbx` `70298f9a316784b9ea493e2298196a7a`, `Kit_ExitSign_Dead.json` `cf96d527d53f74083ae1b95ec7b2f8fb`.
  - Codex's `FrontRoomsRoomStream.cs` diff is two lines, both away from the sign code (`DoorOpenSeconds` made public; a G6 reflection hook in `Initialize`). Its `FrontRoomsRenderSetup.cs` diff adds a glass hook and `Troffer_Lens_Cool`. No conflict with this spec.
  - `FrontRoomsSurface.shader` is unchanged since `7320ed1`.
  - Codex's `FrontRoomsKitImporter.cs` now reads `lodDistances` from the sidecar (`0` = never cull).
- **codex-audit:** at 22:3x the audit has written `00_main_state.md` (22:20) and `00_files.tsv`; `20_findings.md` does not exist yet. What it says about this area:
  - §3.3: the 106 interactables FBX/JSON (including `Kit_ExitSign`/`_Dead`) are W class (unfinished clone work), not live, with "a name clash with the exit-sign workflow's own `Kit_ExitSign*`". Owed: merge the verified files over Codex's copies and keep main's metas. This spec does exactly that (same names, main's GUIDs).
  - §3.7: a **KitImporter defect**. Codex's importer maps `lodDistances[i]` to `lods[i]`, so with a 3-entry sidecar `[d01, d12, dcull]` and a 2-LOD FBX the last LOD culls at d12. §4.3 picks a sidecar form that is right under both the current and a fixed importer.
  - `FrontRoomsMapWorld.Prewarm()` parses every sidecar in `Awake`, so K7 (the `null` entry) is parsed at run time already.
  - The build and merge stages must re-check `20_findings.md` before merging. Two items for the audit are in §14.
- **Not done here:** no Unity run, no clone (the build stage makes `proj_exitsign`), no edit to Red's project outside this folder, no Figma write, no download. No verification images were made, so there is no VERIFICATION_LOG row; the four diagrams are research support.

---

## 2. Period research: the 1990 US EXIT sign

The best primary source is on our side of the era line. **NISTIR 4399, *Evaluation of Exit Signs in Clear and Smoke Conditions* (NIST, August 1990, S1)** measured the signs on the market in 1989–90 and quotes the codes in force. NISTIR 4532 (March 1991, S2) covers the arrows.

### 2.1 What the codes asked for (1988–1989)

| Rule | Value | Source |
|---|---|---|
| Letter height | ≥ 6 in (152 mm) | NFPA 101-1988 §5-10; UL 924 (1989); IES 1987 (S3–S5) |
| Stroke | ≥ 3/4 in (19.1 mm) | same |
| Letter width | ≥ 2 in (50.8 mm), except "I" | UL 924 (S4) |
| Letter spacing | ≥ 3/8 in (9.5 mm) | UL 924 (S4) |
| Arrow | outside the legend, ≥ 3/8 in from any letter, not easy to reverse; unused openings covered | UL 924 (S4) |
| Unlit | the legend must stay visible unlit; contrast ≥ 0.5 lit and unlit | UL 924 (S4) |
| Stencil brightness | max ≤ 300 fL (1,028 cd/m²); max/min ≤ 40 across the letters | UL 924 (S4) |
| Spacing of signs | no point more than 100 ft (30.5 m) from a visible sign | NFPA 101-1988 (S3) |
| Mounting | bottom of the sign 2.0–2.3 m above the floor | IES 1987 (S5) |
| Power | from a source independent of the main supply | IES 1987 (S5) |

### 2.2 What the signs were

- **Stencil face vs panel face.** In a stencil face the letters are holes and glow; the plate is opaque. In a panel face the background glows. Stencil letters were the brighter of the two in S1's lab. S8 recommended stencil faces for visibility.
- **Lamps.** S1's four "conventional" signs were incandescent and fluorescent. Its eight new ones were electroluminescent. **No LED sign was in the 1990 sample.** So LED is not our default.
- **A period drawing set (USACE 40-06-04, 1985–86, S6)** gives the exact build of the time:
  - **Type 602:** aluminium stencil face, red diffuser, two 20 W incandescent lamps, plus two 20 W DC lamps for emergency;
  - **Type 604** (self-contained battery): two 20 W incandescent lamps, plus **two 5 W DC lamps** for emergency;
  - Type 605 is two 20 W incandescent or two 8 W fluorescent; Type 606 is edge-lit.
- **Strokes in practice:** 0.5–0.75 in on 6 in letters, **1:12 to 1:8** (Cohn, via S1, S7). Our face uses 1:8, the code minimum stroke.
- **Colour:** red was usual in the US and required in some cities (S10). The USACE drawings specify red letters only. Green existed; S1 tested both. The ISO running man is not US practice in 1990.
- **Arrows:** NFPA 101-1988 only said "a directional indicator" outside the legend. The 2.25 in **chevron** was the proposed revision in March 1991 (S2). A 1990 universal sign carries both chevrons as **knockouts**: you punch out the one you need, and the other stays as a scored outline.
- **On-disk real example:** `Research/week02/phosphor-narrative/stills/gn_tritium_exit_sign.jpg` (a US self-luminous sign of about the 1970s, CC BY-SA 4.0). Measured on the photo: condensed letters 0.33–0.38 H wide, a moulded bezel, a red face. Its strokes are about 1:6 (tritium signs need wide channels for the tubes).
- **Canon frame on disk:** `room_visuals/images/run_ref_etb_levelbang_0094.jpg` (ETB Level !, VHS stamp MAR. 07 1991): a ceiling-hung "‹EXIT›" with both chevrons, over a red troffer row.

### 2.3 Measured light of real 1990 signs (S1 Table 1, dark room)

| Sign in S1 | Letter mean (cd/m²) | Background (cd/m²) | Letters, relative to "I": E / X / I / T | Spread inside a letter (std ÷ mean) |
|---|---|---|---|---|
| 1, incandescent stencil, green | 21.5 | 0.1 | 0.61 / 0.62 / 1 / 0.76 | 0.45–0.88 |
| 2, incandescent panel, red | 80.6 | 342.6 | 0.58 / 0.70 / 1 / 0.48 | 0.51–0.66 |
| 3R, fluorescent stencil, red | 324.9 | 2.0 | 0.50 / 0.61 / 1 / 0.58 | 0.67–0.94 |
| Our target (mean of 2 and 3R) | — | — | **0.54 / 0.65 / 1 / 0.53** | **0.5–0.75** |

- Red letter chromaticity: x 0.6745, y 0.3193 (3R) and x 0.6925, y 0.3004 (2). Both lie **outside the sRGB gamut**; the nearest displayable colour is the sRGB red primary.
- The letters are far from uniform: hot spots over the lamps, dim ends. That is the look to reproduce. UL's max/min ≤ 40 is the only limit.
- S8: protan observers (about 1 in 50 men) see red signs about 20 % dimmer. Shape and position must carry the read (`room_visuals/03` §4).

---

## 3. What exists today, and what it lacks

### 3.1 In the game (RoomStream, the visual chat's file)

- **Run rooms** (`FrontRoomsRoomStream.cs:1472-1493`): two code-built hanging signs at z 3.4 and 9.2, centre 2.36 m.
  - Housing: a 0.40 × 0.21 × 0.05 box on `officeDarkMaterial` = `Office_BlackedGlass` (smoothness 0.95). A black mirror (11a M1).
  - Faces: two 0.36 × 0.18 quads on `Run_ExitSign` (smoothness 1.0, 11a M2).
  - Rods: two 0.012 × 0.43 boxes.
  - Light: one red point per sign, colour (1, .10, .06), intensity 3.6, range 9.5 m, soft shadows 0.85, at 1.96 m.
  - Always lit. No states. Never dims.
- **Exit rooms** (`:1495-1498`): no sign; a threshold marker only.
- **Today's face texture** (`Run_ExitSign_A/_E`, 1024 × 512 on 0.36 × 0.18 m; `es01` top):
  - letters 76 mm, half the code;
  - stroke 1:5.0; widths E 0.76 H, X 0.92 H, T 0.79 H (period: 0.33–0.40 H);
  - solid triangle arrows on both sides, not chevrons.
- **Nobody plays these rooms yet.** `lobbyOnlyTitle = true` (`:78`), so in play only Lobby stream rooms appear (`room_visuals/04` §0). Run and Exit rooms show only in the editor preview and the stream verification test. The sign matters now for the Run! directions and the Exit doors, and becomes visible when Red enables the playable sequence.
- **The map has no EXIT signs.**

### 3.2 The kit: `Kit_ExitSign` and `Kit_ExitSign_Dead` (in main, unused by any code)

What G3 built (`interactables/build_g3_*.md`, module `Tools/Blender/frontrooms_kit/assets/interact_exit_sign.py`):
- a black stamped-steel housing 0.330 × 0.200 × 0.060 with a raised-bead face frame, 4 slotted face screws and a conduit knockout;
- one flat face quad that crops the `Run_ExitSign` texture;
- 1,916 triangles, no LOD1, no Light.

What it lacks (each one is fixed by this spec):

| # | Gap | Effect |
|---|---|---|
| K1 | The face is a printed quad (panel look). No plate, no holes, no diffuser, no depth | No parallax and no cut edges at 0.3 m. Lit letters are flat, with no hot spots |
| K2 | Letters 96 mm, strokes 1:5.0 | Wrong era read: a modern grotesque at 2/3 size |
| K3 | Face on `Run_ExitSign`, smoothness 1.0 | The white face around the letters is a perfect mirror, lit or dead |
| K4 | `_Dead` uses slot `Run_ExitSign_Dead`, which has **no Unity material**. The importer removes the remap (`FrontRoomsKitImporter.cs`), and `FrontRoomsKitLibrary.ApplyMaterials` falls back to the FBX's imported material, i.e. the Blender preview colour (0.62, 0.60, 0.57), rough 0.5, **no texture** | A dead sign shows a **blank grey face with no letters** (derived from code; not seen in engine) |
| K5 | Housing on `Prop_SteelBlack` | The scan reads as leather grain in the three-view (`interactables/threeview/png/Kit_ExitSign_side.png`) |
| K6 | Sidecar: tags `interactable, exit_sign` → `placement: "Floor"`; origin at the centre (Y −0.100 to +0.102); `minCeiling: 0.152` | The Level Designer would treat it as a floor prop. It breaks the library rule "stands on y = 0" |
| K7 | `lodDistances: [3.0, 10.0, null]` (also in 4 window-frame sidecars) | Whether Unity's `JsonUtility` accepts `null` in a `float[]` is **UNVERIFIED**. Codex's importer and `FrontRoomsMapWorld.Prewarm()` (in `Awake`) both parse this field. Write `0` (= never cull) |
| K8 | Wall mount only, single face, no chevrons, no hanging or end versions, no states, no driver | Cannot replace the Run signs; cannot be dead or flicker |
| K9 | The door frame anchor `exit_sign_p` (Y 2.280) puts the sign's centre there | With the new 0.212 m housing the bottom would hit the head casing (top 2.177). The anchor must move (§4.5) |

### 3.3 Related work in clones (not in main)

- `proj_run_red_ward` (Run! direction A) built `Kit_ExitSign_Hanging` and `Kit_ExitSign_Wall` (white housing, 6 in face texture `Run_ExitSign_Ward`, two chrome stems, `run_exit_sign_*.py`). They are good mounting references.
- **Do not ship the name `Kit_ExitSign_Wall`.** `RoomStream.RefreshRoomMaterials` (`:1502-1517`) repaints every renderer under a room whose object name contains "wall", "seal", "partition" or "threshold marker" with the wallpaper, and "floor"/"carpet"/"ceiling" with those materials. A kit instance takes its label as its object name. **Rule: no EXIT-sign asset, label or child may contain wall, floor, carpet, ceiling, seal or partition.** The wall sign keeps the name `Kit_ExitSign`.

---

## 4. Model spec

### 4.1 The family

| Asset | Mount | Faces | Priority | Origin (Unity: +Z = the room / the front) | Tags → placement |
|---|---|---|---|---|---|
| `Kit_ExitSign` (rebuild; keep GUID `3892f75e…`) | back to the wall; also on the stream door header | 1 | **P1** | bottom-centre of the back plane (Y 0 = housing bottom, Z 0 = wall face) | `exit_sign`, `wall_decor` → **Wall** |
| `Kit_ExitSign_Dead` (rebuild; keep GUID `70298f9a…`) | VARIANT: `ExitSign_Diffuser` → `ExitSign_Diffuser_Dead` | 1 | **P1** | same | same |
| `Kit_ExitSign_Hanging` (new) | ceiling canopy, two stems, drop 0.400 (for 2.9 m ceilings) | 2 | **P1** (RoomStream Run) | centre of the canopy on the ceiling plane; the sign hangs into −Y; faces ±Z | `exit_sign`, `ceiling` → **Ceiling** |
| `Kit_ExitSign_Hanging_Dead` (new) | VARIANT, as above | 2 | **P1** | same | same |
| `Kit_ExitSign_HangingFlush` (+ `_Dead`) | canopy only, drop 0.025 (Low 2.4 m ceilings) | 2 | P2 | same | same |
| `Kit_ExitSign_End` (+ `_Dead`) | end/flag: the sign's end on a wall plate, projecting 0.36 m | 2 | P2 (Run! direction B) | the wall plane at the housing bottom; the sign along +Z, faces ±X | `exit_sign`, `wall_decor` |
| `…_ChevL`, `…_ChevR` | open left or right chevron (mesh variants, one module constant each) | — | P2, on demand | — | — |

- One common module, `assets/exit_sign_common.py`, plus thin modules per asset. `interact_exit_sign.py` is rewritten to call it, so `NAME = "Kit_ExitSign"` keeps its file names. **Never edit `kitlib.py`.**
- `es03_mounts.png` shows every mount against the game's heights:
  - A, over a map door at Low 2.40: sign 2.180–2.392, head casing top 2.177;
  - B, on the stream door header (2.66–2.90): 2.674–2.886;
  - C, hanging at 2.90: 2.288–2.500 (the diagram draws one stem; the asset has two);
  - D, flush at 2.40: 2.163–2.375;
  - E, end mount at 2.90: 2.294–2.506.
  - All are above the 2.03 m clearance for projections from a ceiling (current NFPA 101 §7.1.5; the 1988 clause is UNVERIFIED).

### 4.2 Construction (shared by every mount; mm unless noted)

| Part | Spec | Notes |
|---|---|---|
| Housing ("pan") | 346 W × 212 H outside; depth 64 (single face) / 76 (double face); 0.9 mm (20 ga) steel; drawn corners R 10, 8 segments; 2 mm radius on the front fold | Envelope ESTIMATE within the 12–14 × 8–9 in range of the period; E7/E8 would confirm. A 1950s second-hand sign was 7.75 × 18 × 5 in (S12): bigger and deeper |
| Face frame | 13 border; a raised bead 4 wide × 1.2 high, 6 from the outer edge; an inner lip returning 3 into the opening | The frame meets the pan with a **0.6 × 0.4 seam** all round (hero detail) |
| Opening | 320 × 186, corner R 3 | Fits the 292 mm legend with chevrons |
| Stencil plate | 0.9 thick, behind the frame lip; the letters are **through-holes** (§5); stamped inner corners R 0.5; 0.2 front chamfer on every hole | Painted after stamping, so the cut walls carry the enamel. Closed chevron knockouts are a 0.3 score in the texture, not geometry |
| Diffuser | 2.5 red acrylic sheet, **6 behind the plate**; its front face is one quad on `ExitSign_Diffuser` | The 6 mm gap gives parallax through the holes at 0.3 m |
| Spacer | a white 6 mm frame between plate and diffuser | Seen only at grazing angles, as a pale wall inside each hole |
| Face screws | 4 slotted oval-heads, Ø 6.2, dome 1.2, at x ±120 on the top and bottom borders; random slot angles | As G3 |
| Top knockout | 1/2 in trade-size concentric knockout, Ø 22 (wall sign); stem holes (hanging) | |
| Back (wall sign) | the pan's back with two keyhole slots on 89 mm (3.5 in) centres, for the outlet-box screws | Anchors `mount_l`, `mount_r` (§4.5): the door-break "drops onto one screw" beat (`door_break/10_design.md`, EX-K) |
| Hanging: canopy | 127 × 70 × 16 rectangular pan, 4 radius (ESTIMATE; a 5 in round canopy is the alternative) | The dead-sign variant shares it |
| Hanging: stems | 2 × Ø 12 chrome tube at x ±105, collars at both ends, a hex lock nut on top of the housing; drop 0.400 from the ceiling to the housing top | As Red Ward and today's two rods |
| End mount | an end plate 70 × 150 × 3 on the sign's wall end, onto a wall canopy 127 × 70 | |

**Hero details at 0.3 m (LOD0):** the frame seam; screw slots; bead highlights; hole walls with their chamfer; the diffuser moving behind the holes as you move; the white spacer at grazing angles; dust on the top fold; one or two paint chips; heat browning above the lamps; insects collected at the diffuser's bottom edge (texture, §6); fingerprints round the screws.

### 4.3 Triangle budgets and LODs

| Asset | LOD0 | LOD1 | LOD2 (needs P-1) | Switch distances (sidecar `lodDistances`) |
|---|---|---|---|---|
| `Kit_ExitSign` | **3,500** ±15 % | 1,100 | 200 | `[5, 0, 0]` today; `[5, 15, 0]` with P-1 |
| `Kit_ExitSign_Hanging` | **6,500** | 2,000 | 300 | same |
| `Kit_ExitSign_HangingFlush` | 5,800 | 1,800 | 300 | same |
| `Kit_ExitSign_End` | 6,000 | 1,900 | 300 | same |

- Breakdown of the wall LOD0 (ESTIMATE): pan 900; frame bead 900; plate with holes and cut walls 500; screws 4 × 150; knockout and keyholes 150; spacer 100; diffuser 2.
- **kitlib exports LOD0 and LOD1 only.** LOD1 is a collapse decimation, and parts marked `lod1_drop` are deleted. LOD2 needs pipeline item **P-1** (a `_LOD2` export), which is **not approved**. Until it is, LOD1 never culls.
- **Never cull an EXIT sign.** It is a wayfinding object and it glows. Write `0`, never `null` (K7).
- **Sidecar form:** `lodDistances: [5.0, 0, 0]` while the FBX has 2 LODs. Codex's importer (audit §3.7) maps entry i to LOD i, so LOD0 → LOD1 at 5 m and LOD1 never culls. A fixed importer reading `[d01, d12, dcull]` gives the same result (no LOD2, no cull). With P-1 and 3 LODs: `[5.0, 15.0, 0]`. Never `[5, 15, 0]` with 2 LODs: today's importer would cull LOD1 at 15 m.
- `lod1_drop`: screws, knockout, keyholes, collar bevels, the spacer, the fine bead segments.
- Keep in every LOD: the plate with its letter holes (about 300 triangles, cheap), the diffuser quad, the pan.
- Why 5 m: a 6.2 mm screw head is 0.8 px at 5 m (76° FOV, 1080 p).
- Letter size on screen (76° FOV, 1080 p): 17.6 px at 6 m, 8.8 px at 12 m, 3.5 px at 30 m. Past about 15 m the sign reads as a red bar, not a word (as `room_visuals/03` §2 found). That is correct.

### 4.4 Chevrons

- Default: **both chevrons closed** (scored outlines, unlit). This follows `10_run_directions.md` ("no arrows; the exit is straight ahead") and UL 924 ("unused openings covered").
- Open-chevron variants are mesh variants (a hole in the plate), one module constant each: `_ChevL` and `_ChevR`, P2.
- On a double-face sign the same hole points left on one face and right on the other. That is correct.
- Chevron: 57.15 high (2.25 in, S2), 24.6 wide (w/h 0.43, the top of S2's range), arm 11.1, 10 from the letters (UL ≥ 9.5).

### 4.5 Anchors, sidecar and the kit rules

- `kit.no_collider()` on every asset. All are above 2.16 m, and nothing collides with a sign.
- **Wall sign anchors** (Unity metres):
  - `hang` (0, 0, 0): makes the Level Designer put the sign's bottom at min(2.10, ceiling − 0.05 − 0.212) = **2.10 m** in every zone, with no map edit (`FrontRoomsModuleEditing.cs:105-120`);
  - `back_centre` (0, 0.106, 0);
  - `face` (0, 0.106, z_plate) and `face_dir`;
  - `diffuser` (0, 0.106, z_diff);
  - `lamp_a` and `lamp_b`: the two 20 W lamps inside, at (+0.054, 0.143, 0.030) and (−0.048, 0.143, 0.030), from the field fit (§6.3). Unity is left-handed: seen from the front (+Z), **+X is the reader's left**, so lamp A (behind E–X) has +X;
  - `glow` (0, −0.02, 0.06): where an optional spill light goes (§8.6);
  - `mount_l` (+0.0445, 0.140, 0) and `mount_r` (−0.0445, 0.140, 0), left and right as read from the front: the keyhole screws.
- **Hanging anchors:** `ceiling` (0, 0, 0), `centre` (0, −0.506, 0), `face_front`, `face_back`, `stem_l`, `stem_r`, `glow` (0, −0.94, 0) = 1.96 m under a 2.9 m ceiling (§9.1).
- **Sidecar extra** (`kit.meta["exitSign"]`): `{faces, openingM [0.320, 0.186], letterHeightM 0.1524, strokeM 0.01905, diffuserSlot "ExitSign_Diffuser", deadSlot "ExitSign_Diffuser_Dead", chevrons "closed" | "left" | "right", lampField {…§6.3}}`.
- The `minCeiling` that kitlib computes for a ceiling-origin asset is meaningless (0.05). The hanging signs are only for zones ≥ 2.9 m; Low zones use the flush one.
- **Door frame anchor (interactables, visual chat's own module `interact_door_common.py`):** move `exit_sign_p` from (−0.080, 2.280, 0.500) to **(−0.080, 2.180, 0.500)**, which now means the sign's origin (its bottom). The sign then sits 3 mm above the casing top (2.177), with its top at 2.392 under the Low 2.40 ceiling (`es03` A).

---

## 5. The letter face (geometry, not a font)

The legend is built from the code geometry, not set in a font (`es01`). It is the honest 1990 stencil: simple strokes that a die can punch. Per the type rule, **平面视觉 reviews this face before the build ships** (faces and lettering go through 平面视觉; this chat owns the numbers only).

| Element | Value (mm) | Check |
|---|---|---|
| Letter height H | 152.4 (6 in) | = code minimum |
| Stroke S | 19.05 (3/4 in), 1:8 | = code minimum stroke; period range 1:8 to 1:12 (S7) |
| E width | 54.0 (0.354 H); middle bar 48.0 long, centred | ≥ 50.8 (UL) |
| X width | 60.0 (0.394 H); two crossing bars, each 19.05 thick measured square to the bar | ≥ 50.8 |
| I width | 19.05 | — |
| T width | 54.0; stem centred | ≥ 50.8 |
| Letter gaps | 12.0 | ≥ 9.5 (UL) |
| Legend "EXIT" | 223.05 wide | — |
| With both chevron positions | 292.25 wide (chevron 24.6, gap 10.0) | inside the 320 opening with 13.9 margins |
| Baseline in the opening | 16.8 from the opening's bottom (centred) | — |

- "EXIT" has no closed counters, so the stencil needs **no bridges**. Every island of the plate stays joined.
- **Why not a font:**
  - today's texture is a grotesque bold at 1:5.0, with E 0.76 H and X 0.92 H;
  - `10_run_directions.md` proposed TeX Gyre Heros Bold Condensed. Measured, its "I" is 0.171 H wide (1:5.8), and E, X, T are 0.62 / 0.73 / 0.67 H;
  - both are heavier and wider than any 1990 EXIT legend (S7, the tritium photo).
  - Fallback if 平面视觉 prefers a typeface: Heros Cn Bold, scaled to 152 mm caps.
- The face bitmap (§6.1) is rendered from the same polygons as the plate holes. Holes and print can never drift apart.

---

## 6. Textures

All generated in Python (numpy/PIL) or Blender, with no external images. Stems go in `Assets/Resources/Surfaces/Textures/`.

### 6.1 `ExitSign_Face_A / _N / _S`: the stamped plate and frame

- **Size:** 2048 × 1024, decal UVs over the whole face including the border (0.346 × 0.212 m).
- **Texel density:** 5.9 px/mm across, 4.8 px/mm up. A 0.3 m inspection at 76° FOV and 1080 p needs 2.3 px/mm.
- **`_A`** (sRGB):
  - baked white enamel, sRGB (230, 226, 212), lightly yellowed;
  - heat browning, a soft −6 % blotch above each lamp position;
  - dust on the lower lip;
  - fingerprints round the screws (relamping);
  - 2–3 chips at sharp corners (bare steel (120, 118, 112) over a red-oxide primer edge);
  - the scored outlines of the closed chevrons (0.3 mm, −25 %).
  - Variants by SurfaceDef tint: black (28, 28, 28); almond (`Prop_SteelAlmond` value).
- **`_N`:** orange peel (0.5–1 mm cells, small amplitude); the scores; dents near two screws.
- **`_S`** (R smoothness, G cavity):
  - R mean 0.42: dust 0.25, fingerprints 0.55, chips 0.35;
  - G: cavity round the screws, the bead and the hole edges.

### 6.2 `ExitSign_Diffuser_A / _S / _E / _F`: the red sheet behind the holes

- **Size:** 1024 × 512 over the opening (0.320 × 0.186 m), 3.2 px/mm. The letter edges come from the geometry, so the field can be smooth.
- **`_A`** (unlit look through the holes):
  - red acrylic over a white interior: linear (0.15, 0.022, 0.018), sRGB about (108, 41, 37);
  - a fine frosted mottle;
  - **5–9 dead insects** (4–12 mm silhouettes) along the bottom 25 mm (a real neglected-sign detail; they read as dark shapes when lit);
  - a dust gradient at the bottom.
- **`_S`:** R mean 0.72 (smooth plastic), 0.45 where dusty.
- **`_E`** (sRGB, red): the **LIT** field (lamps A + B + fills) × the red colour. Used by any path that does not know lamp fields: the RT system (§7.3) and the fallback without the shader keyword.
- **`_F`** (linear; the stem does not end in `_A`/`_E`, so the importer keeps it linear):
  - R = lamp A field, G = lamp B field, B = the DC-pair field, each normalised so the LIT peak is 1;
  - A = transmission: 1, minus the insects and dust.

### 6.3 The lamp hot-spot field (fitted to S1)

- Each lamp is a 2D Gaussian on the opening (u, v in 0–1), plus a flat inter-reflection fill of 0.06 per working lamp.
- Lamp A at (0.33, 0.70), behind the E–X web. Lamp B at (0.65, 0.70), behind the I–T web.
- σu 0.08, σv 0.20. The lamps sit high, in sockets on the top wall.
- DC pair (BATTERY): (0.12, 0.70) and (0.88, 0.70), same σ, level 0.25. Two 5 W against two 20 W (S6); the positions are an ESTIMATE.

| Check | Ours (LIT) | Target (S1) | Pass |
|---|---|---|---|
| Letter means relative to "I": E / X / I / T | 0.51 / 0.59 / 1 / 0.58 | 0.54 / 0.65 / 1 / 0.53 | within 0.06 |
| Spread inside a letter (std ÷ mean) | 0.54–0.66 | 0.5–0.75 | yes |
| Max ÷ min over all letters | 9.3 | ≤ 40 (UL) | yes |

- The states (`es02`, `es04`, `es_diagrams_report.json`):

| State | E / X / I / T, relative to LIT "I" |
|---|---|
| HALF, lamp A out | 0.10 / 0.17 / 0.89 / 0.48 |
| BATTERY | 0.17 / 0.05 / 0.05 / 0.14. The end letters glow and the middle goes dim: a readable tell that the mains have failed |

- The earlier, flatter fit (σv 0.55) gave a spread of only 0.10–0.33. It is replaced.

### 6.4 `ExitSign_Enamel_A / _N / _S`: the housing

- 1024², tiling, metre UVs, tile 0.40 m (2.6 px/mm).
- White enamel with orange peel. Wear: edge-only rub on the front fold; a dust skin on top faces (`_S` R 0.30 there, 0.50 elsewhere).
- Variants by tint (black, almond), as §6.1.

### 6.5 Memory

- Face 3 × 2048 × 1024, diffuser 4 × 1024 × 512, housing 3 × 1024², BC7/BC5, with mips: about 14 MB on desktop.
- Shared by every asset in the family.

---

## 7. Materials and the mirror fix

### 7.1 New SurfaceDefs (`FrontRoomsRenderSetup.cs`, visual chat)

| Name | Texture stem | Tile / UV | Smoothness (× mask R) | Metallic | Emission |
|---|---|---|---|---|---|
| `ExitSign_Housing` | `ExitSign_Enamel` | 0.40 m, metre UVs | 1.0 × R (mean 0.50) | 0 | none |
| `ExitSign_Face` | `ExitSign_Face` | 0.346 × 0.212, decal (metre UVs over the face) | 1.0 × R (mean 0.42) | 0 | none |
| `ExitSign_Diffuser` | `ExitSign_Diffuser` | 0.320 × 0.186, decal | 1.0 × R (mean 0.72) | 0 | `_E`, colour linear (3.2, 0.013, 0.006); `lampField = "ExitSign_Diffuser_F"` (§7.2) |
| `ExitSign_Diffuser_Dead` | `ExitSign_Diffuser` | same | same | 0 | **none** (keyword off) |
| (existing) `Prop_Chrome` | — | — | 0.85 | 1 | none: stems, collars, screws |

- **4 slots per asset** (the kit rule): Housing, Face, Diffuser (or Dead), Chrome.
- Emission colour: S1's red is outside sRGB. The colour is the sRGB red primary with a trace of green, xy about (0.64, 0.33). Today's (1, 0.012, 0.006) is slightly orange.
- **Emission level:** start at today's peak of 3.2. With the field's letter mean of about 0.55, the hot spots pass the bloom threshold (1.05) and the dim ends do not. The bloom forms round the hot spots, as a camera sees a real sign.
- **Calibration test (§12, T2):** in a lit Lobby room, the LIT letter mean must be **1.5–2.5 × the luminance of a lit wall**. Basis: a real sign of about 100 cd/m² (S1, S9) next to a white wall at about 200 lx, which is about 50 cd/m².
- **Importer:** `FrontRoomsSurfaceTextureImporter.Configure` (`:688`) clamps only `Run_ExitSign*`. Add `|| stem.StartsWith("ExitSign_")`, so decal edges do not bleed at far mips.

### 7.2 The lamp-field keyword (`FrontRoomsSurface.shader`, visual chat)

- Properties:
  - `[Toggle(_FR_LAMP_FIELD)] _UseLampField` (default 0);
  - `_LampFieldMap` ("black");
  - `_LampField` (Vector, default (1, 1, 0, 0)), declared inside `UnityPerMaterial` so the SRP batcher stays valid.
- `#pragma shader_feature_local_fragment _FR_LAMP_FIELD`.
- Fragment, keyword on: `f = SAMPLE(_LampFieldMap, uv)`, then `s.emission = dot(f.rgb, _LampField.rgb) * f.a * _EmissionColor.rgb`. Keyword off: unchanged.
- Every material without the keyword is **bit-identical**. The cost is one sample and one dot product on sign diffusers only.
- `SurfaceDef` gains `string lampField`. When it is set, `EnsureSurfaces` assigns `_LampFieldMap` and enables the keyword.
- The driver writes only `_LampField` (a MaterialPropertyBlock on the sign's renderer).
- **Fallback:** if Red declines a shader change, the driver scales `_EmissionColor` only. The states then change brightness but not the hot-spot pattern (lower spec; BATTERY loses its tell).

### 7.3 The mirror fix: every surface

| Surface | Today | Fix |
|---|---|---|
| `Run_ExitSign` (old face; used by the code-built Run signs and by today's `Kit_ExitSign`) | 1.0, no mask: the face around the letters is a perfect mirror, lit or dead | `smooth = .38f` in its SurfaceDef **and** `_Smoothness: 0.38` in `Run_ExitSign.mat` (both, so a later RenderSetup run keeps it). Stays until nothing uses it |
| Code-built housing (`officeDarkMaterial` = `Office_BlackedGlass`, 0.95) | a black mirror (11a M1) | removed: the kit replaces the code-built sign |
| New face / housing / diffuser | — | 0.42 / 0.50 / 0.72 means, mask-driven |
| `_Dead` (blank grey fallback, K4) | no material | `ExitSign_Diffuser_Dead`: same albedo and smoothness, no emission |

- **Emission only when lit:**
  - the `_Dead` assets have no emission keyword at all;
  - a driven sign in DEAD writes `_LampField = 0`, and its light is disabled;
  - today's `_E` background (sRGB 13, 3, 2) leaked about 0.013 of emission on dead faces. The new `_E` is exactly 0 outside the field.
- **Ray tracing (G14, `FrontRoomsGlassRTSystem.cs`; another visual-chat workflow):**
  - The RT reads emission once per material, except for renderers named "Lens", which it re-reads each frame (`:1033`).
  - A sign that goes dead or flickers would still glow in the Run floor's traced reflection.
  - **Request to the RT owner:** classify renderers that carry `FrontRoomsExitSign` as `EmissiveLens`, and scale their emission by `FrontRoomsExitSign.Level` (0–1) instead of the MPB `_EmissionColor`.
  - Until that lands, accept LIT-only reflections, or set the RT off on Run floors.
- **Green option** (Red's call 10 on DW12; default red): `ExitSign_Diffuser_Green` with emission linear (0, 0.95, 0.03). S1 sign 1's green, x 0.237, y 0.655, is outside sRGB; this is the gamut-clipped colour at the red's luminance (Y ≈ 0.68). P2, only if Red picks green.

---

## 8. Behaviour: LIT, HALF, LOOSE, BATTERY, DEAD

### 8.1 States

| State | Lamp weights (A, B, DC) | Red light | What you see |
|---|---|---|---|
| **LIT** | 1, 1, 0 | 1.0 | Two hot spots behind E–X and I–T, dim ends |
| **HALF** (one lamp burnt out) | 1, 0, 0 or 0, 1, 0 | 0.55 | One end bright, the other dull red. Incandescent exit lamps burnt out often; that is why LED retrofits came in the 1990s |
| **LOOSE** (a loose socket) | one lamp follows the drop-out process (§8.3) | follows | Short drop-outs of one half, never a full blackout |
| **BATTERY** (mains lost) | 0, 0, 0.25 | 0.18 | Only the ends glow faintly (two 5 W DC lamps, S6) |
| **DEAD** (lamps and battery gone) | 0, 0, 0 | off (disabled) | The letters read as dark red holes: UL 924's unlit contrast ≥ 0.5 still holds against white enamel (§12, T3) |

### 8.2 Temperament and the room's lamp state

A sign has its own circuit (S5: "independent of the main supply"). Its lamps do not strike or sway with the troffers. It is tied to the room in three ways.

1. **Its odds follow the room's decay.** Rolled once from the same hash family as the room's lamps.
   - **RoomStream:** key `sequence * 16 + 12 + i` (lamps use 0–11, so 12–15 are free), rolled by a new `SignOdds(rule)` next to `LampOdds`:

| RoomRule | Signs | LIT | HALF | LOOSE | BATTERY | DEAD |
|---|---|---|---|---|---|---|
| Run | 2 hanging | 70 % | 15 % | 15 % | 0 | **0** (the level's red source must burn; canon) |
| Exit | 1 wall | 100 % | 0 | 0 | 0 | 0 (the true exit never fails) |
| Office (option O1, off by default) | 1 wall | 85 % | 8 % | 5 % | 0 | 2 % |
| Lobby, Shift | none (Level 0 has no exits) | — | — | — | — | — |

   - **Map:** odds by tier, with tier 0 like Office. Doubled for HALF/LOOSE/DEAD when `LampModeOf(cell)` of the sign's cell is Failing or Dead: a decayed corner has a decayed sign. A module may force a temperament (a prop field via the sidecar tag; no map edit).
2. **Its clock starts with the room's lamp clock.**
   - It glows from the moment the room is built: exit signs burn 24 hours, so it is lit behind a shut door. Opening a Run door shows a dark room with red signs before the troffers strike.
   - The LOOSE process runs only while the room's lamps are scheduled (after `ScheduleRoomLight`), so a flicker plays when someone is there.
3. **Its power follows power events, not troffer failures.**
   - `SetMains(level)`: 1 = normal.
   - Brownout 0.55–1: the AC lamps scale by level^3.4 (incandescent light output against voltage, standard approximation).
   - Below 0.55: the transfer relay drops to DC. The AC lamps cool (τ 40 ms), there is a 0.20 s black gap, then the DC lamps strike (τ 60 ms) → BATTERY.
   - Mains back: the reverse, with no gap.
   - **RoomStream:** mains stay at 1 until Red picks a Run! direction. Direction A (tubes cut, signs keep burning) does not touch the signs. Direction B (mains drop) calls `SetMains(0)` on its trip.
   - **Map:** `MainsLevel(cell)` (contract C-2, §10.3): the multiplier of active `Sag` overrides only. Dip and Warn are the lamps' business, not power, so the sign ignores them.

### 8.3 The LOOSE process (incandescent, not fluorescent)

- A filament loses contact and comes back. No ballast strobe.
- **Events:** Poisson, mean gap 3 s (clamped 0.6–8 s). Each event has 1–3 drop-outs.
- **Drop-out:** the lamp falls with τ_off 40 ms to 0.05, holds 80–300 ms, and recovers with τ_on 60 ms.
- **Spacing:** ≥ 0.5 s between drop-outs (≤ 2 flashes/s, the design target in `RELAY_PURSUIT_REDESIGN.md`). An episode lasts ≤ 3 s, then ≥ 4 s of quiet.
- Only one lamp is loose, so the sign never goes fully dark. Each drop-out still counts as a flash for the photosafety test: the red is saturated (WCAG 2.3.1 red flash), and within about 4 m the sign covers more than a quarter of a 10° field.

### 8.4 REDUCE FLASHING (`FrontRoomsSettings.ReduceFlashing`)

- **LOOSE:** no drop-outs. The loose lamp rests at 0.6, steady (HALF-like). The clock and RNG still advance, as the map's lamps do (`FrontRoomsMapWorld.cs:1729`), so turning the setting off resumes the same pattern.
- **Mains transfer:** a 0.5 s crossfade, no black gap (`ReducedFlashingEnvelopeSeconds` = 0.5).
- The red light follows the same curves.

### 8.5 Photosafety

- Default: ≤ 2 flashes/s from any one sign; episodes ≤ 5 s.
- Two signs in view are not synchronised, so in a Run room two flickering signs could reach 4 changes per second in the frame. Rule: **at most one LOOSE sign per room** (the roll enforces it).
- With REDUCE FLASHING: 0 flashes.
- Covered by the 60 Hz sampling test in §12, T5.

### 8.6 The driver: `FrontRoomsExitSign` (new, `Assets/Scripts/Rendering/`, visual chat)

```
public sealed class FrontRoomsExitSign : MonoBehaviour
  enum Temperament { Lit, HalfA, HalfB, Loose, Battery, Dead }
  void Configure(uint seed, Temperament t, Light redLight = null, float lightBase = 0)
  void SetMains(float level)            // 1 normal, < 0.55 transfers to DC
  void Tick(float dt)                   // called by RoomStream; map signs self-tick only while LOOSE or in a transition
  float Level { get; }                  // mean letter level vs LIT (for the light and the RT)
```

- It finds the diffuser submesh by material name (`ExitSign_Diffuser`) and writes a MaterialPropertyBlock with `_LampField`.
- It drives the light's intensity as `lightBase × Level` and disables the light at 0.
- No allocation per frame. A static sign (LIT, HALF, DEAD) does no per-frame work.
- **Spill light, optional, desktop only:** a real sign throws a little red on the wall and ceiling near it. About 6 cd from 0.06 m² at 100 cd/m² gives about 24 lx at 0.5 m.
  - Wall signs may get one unshadowed point at the `glow` anchor: colour (1, .13, .07), intensity 0.5, range 1.5 m, culled beyond 12 m.
  - Gated off on WebGL (`#if !UNITY_WEBGL`). The WebGL light budget is its own track (`webgl/10` §2).
  - Off by default until T2 shows it is needed.
- **How it attaches:** `FrontRoomsKitLibrary.Spawn` (Office/, visual chat) adds the component when the sidecar has the tag `exit_sign`. Every map-module or facade spawn then gets a working sign with no map edit. RoomStream configures its own signs after spawning.

---

## 9. Placement

### 9.1 RoomStream (ours)

| Room | What | Where (room-local) | Facing | Temperament |
|---|---|---|---|---|
| **Run** | 2 × `Kit_ExitSign_Hanging` (replace the code-built signs) | x 0, z 3.4 and 9.2, origin y = RoomHeight 2.9; housing 2.288–2.500 (centre 2.394, today 2.36) | faces ±Z, down the corridor | `SignOdds(Run)` |
| Run, red light | keep today's light per sign: colour (1, .10, .06), 3.6, range 9.5, soft shadows 0.85, **at y 1.96 as today** | the `glow` anchor | — | intensity × sign `Level` |
| **Exit** | 1 × `Kit_ExitSign` (new) | on the far door header's room face, centred on the 2.4 m opening: bottom 2.674, top 2.886 | facing −Z, toward the arriving player | always LIT |
| Office (option O1) | 1 × `Kit_ExitSign` over the far door | as Exit | −Z | `SignOdds(Office)` |
| Lobby, Shift | none | — | — | — |
| **Start rooms** (the title replicas; `lobbyOnlyTitle`) | none by default. **Option S1 (Red's call):** a `Kit_ExitSign_Dead` over the terminal door that shuts and locks behind the player at the handoff. "The last way out is already dead" | the terminal door's header | −Z | DEAD |

- The light keeps the room's look. The sign's height changes by 3 cm, so the light stays exactly where it is today.
- Labels: `"exit sign A"` / `"exit sign B"` / `"exit sign over door"`. Never "wall", "ceiling" and the like (§3.3).

### 9.2 Map (the map chat's files: contracts only)

| Spot | How | Contract? |
|---|---|---|
| **Module props** (Level Designer) | `Kit_ExitSign` as a `ModuleProp`: placement Wall + the `hang` anchor puts it at a 2.10 m bottom. `Kit_ExitSign_Hanging` needs `y` typed by hand (= the zone's ceiling height) until C-3 | none (C-3 optional) |
| **Run! alarm corridor (R1)** | per the chosen direction (`10_run_directions.md`): A = hanging signs at z 6, 12, 18, 24 plus a wall sign over the goal; B = one `_End` flag plus a wall sign; C = a wall sign over the goal. Built by the visual chat's `FrontRoomsRunRig` | the CR list already in `10_run_directions` §8 |
| **Exit doors EX-F / EX-K** (interactables P2) | `Kit_ExitSign` (lit) / `_Dead` at the frame's `exit_sign_p` (§4.5) | when the map has Exit doors (interactables P2) |
| **Office side of Office ↔ Level 0 doors** | a wall sign above the door on the Office side. The office is the "real" building with code signs, and its EXIT leads back into Level 0 | **C-1, proposal, off by default** |
| Level 0 halls | none (canon: no exits). An "EXIT over a wall with no door" error spot is an idea for Red, not specified | — |

---

## 10. Code changes

### 10.1 Ours (visual chat)

1. **`FrontRoomsRoomStream.cs`**
   - Run block (`:1472-1493`): replace the box, two `SignFace` quads, two rod boxes and the light setup with `FrontRoomsKitLibrary.Spawn("Kit_ExitSign_Hanging", props.transform, new Vector3(0f, RoomHeight, sz), 0f, null, false, "exit sign A"/"B")` plus the existing light (same colour, intensity, range, shadows, world y 1.96), then `Configure(...)`.
   - Exit block (`:1495-1498`): add the wall sign (§9.1).
   - New `SignOdds(RoomRule)` next to `LampOdds` (`:1383`); rolls in `ConfigureLightProfile` (`:1620`) with keys `sequence * 16 + 12 + i`; ≤ 1 LOOSE per room.
   - `TickRoomLights` (`:795`) ticks the room's signs.
   - `RefreshRoomMaterials` (`:1502`): no change needed if the labels follow §9.1. Add a comment naming the rule.
   - Remove `SignFace` (`:1843`) if nothing else uses it.
   - `profileVariants` builds all five rule variants per pooled room. The extra kit instances are inactive in other rules; that is acceptable (2 hanging + 1 wall per pooled room).
   - **Keep everything else identical** (the integrate stage proves it: stream verification, map interaction tests, 20 validator seeds).
2. **`FrontRoomsRenderSetup.cs`:** the four SurfaceDefs (§7.1), `lampField` support, the `Run_ExitSign` `smooth = .38f`, and the importer clamp rule (§7.1). Merge over Codex's version as a diff: its glass hook and `Troffer_Lens_Cool` stay.
3. **`FrontRoomsSurface.shader`:** the `_FR_LAMP_FIELD` keyword (§7.2).
4. **`Run_ExitSign.mat`:** `_Smoothness: 0.38` (the merge edits the asset by hand; Red's project must not run the RenderSetup menu from a script).
5. **New `Assets/Scripts/Rendering/FrontRoomsExitSign.cs`** (§8.6).
6. **`FrontRoomsKitLibrary.cs`** (Office/): attach the driver for the `exit_sign` tag. Merge over Codex's version: it added `lodDistances` to `Info`.
7. **Blender:** `assets/exit_sign_common.py` (new), `interact_exit_sign.py` (rewritten), `exit_sign_hanging.py`, `exit_sign_hanging_flush.py`, `exit_sign_end.py` (new); `interact_door_common.py` `exit_sign_p` (§4.5). Texture generator: `Tools/lookdev/gen_exit_sign.py` (new).
8. **Assets:** the FBX/JSON above; the textures of §6; materials generated in the clone, merged as files with their new `.meta`. **Keep main's GUIDs** for `Kit_ExitSign(.fbx|.json)` and `Kit_ExitSign_Dead(.fbx|.json)` (the four GUIDs in §1).

### 10.2 Things that are not ours, and what we ask

| To | Request |
|---|---|
| RT track (G14, `proj_rt`) | classify `FrontRoomsExitSign` renderers as `EmissiveLens`; scale by `Level` (§7.3) |
| 平面视觉 | review the letter face (§5) and the green option if Red picks it |
| interactables track | it owns `interact_door_common.py`; the `exit_sign_p` move (§4.5) is ours (same chat) but touches their module, so coordinate |

### 10.3 Contract requests to the map chat (exact proposals; no map file is edited)

- **C-1 (optional, Red's call): Office-side door signs.**
  - When `FrontRoomsMapWorld` builds a door on an Office ↔ Level 0 border, it spawns `Kit_ExitSign` on the Office side, centred over the door, bottom at the door top + 0.08 (2.18 m in Low zones), back on the wall face, through `FrontRoomsKitLibrary.Spawn(..., colliders: false)`.
  - Odds 1 in 2 doors, seeded by the door cell.
  - The driver reads `LampModeOf(cell)` and the tier itself. No other map change.
- **C-2: `public float MainsLevel(GridCoord cell)`** in `FrontRoomsMapWorld`.
  - Returns the lowest multiplier of active `LampFx.Sag` overrides on that cell, else 1. Dip and Warn are excluded.
  - Pure; no allocation. `FrontRoomsExitSign` polls it only while a `FixtureChanged` event or a `LampDipped` on its cell is fresh (≤ 2 s).
- **C-3 (optional): "Ceiling" placement in the Level Designer** (`Editor/FrontRoomsMap/FrontRoomsModuleEditing.cs:109`): `case "Ceiling": y = MapGrid.CeilingHeight(m.height)`. Hanging signs then snap to the ceiling. Signs tagged `exit_sign` + `ceiling` are only offered in Standard and Tall modules (Low uses the flush one).
- **C-4 (no change asked):** `FrontRoomsKitLibrary.Spawn` is already used by the module stamp (`FrontRoomsMapWorld.cs:1995`). The driver attaches there.

---

## 11. Budgets (desktop is the reference; WebGL is its own track)

| Item | Budget |
|---|---|
| Triangles | §4.3 (wall 3,500 / 1,100 / 200; hanging 6,500 / 2,000 / 300) |
| Draw calls | 4 submeshes per sign = 4 draws + shadows (the hanging sign is above 0.3 m, so it casts). The MPB on the diffuser takes that renderer out of SRP batching; with ≤ 3 signs per room this is negligible |
| Lights | Run: unchanged (2 shadowed points per room), now **off when DEAD**. Exit/Office wall signs: 0 by default; optional spill point (unshadowed, range 1.5, desktop only) |
| Textures | about 14 MB for the whole family (§6.5) |
| CPU | a static sign does no per-frame work; a LOOSE or transitioning sign: one MPB write per frame |
| Shader | one keyword, one sample + dot, diffusers only |
| WebGL | the WebGL track decides (it may drop the keyword and spill lights). Nothing on desktop is lowered for it |

---

## 12. Acceptance tests (build, integrate and critic stages)

| # | Test | Pass |
|---|---|---|
| T1 | **Mirror.** Every exit-sign surface: effective smoothness (mask R × `_Smoothness`) ≤ 0.6 on the face and housing, ≤ 0.8 on the diffuser. Frame: a DEAD sign at 1 m under a lit troffer | no sharp troffer reflection on the face; `Run_ExitSign` reads 0.38 |
| T2 | **Lit level.** Lobby-lit room, LIT sign at 3 m: letter mean luminance ÷ lit-wall luminance | 1.5–2.5; bloom only round the hot spots |
| T3 | **Unlit contrast (UL 924).** DEAD sign, Lobby light: (L_face − L_letters) ÷ L_face | ≥ 0.5 |
| T4 | **Hot spots.** Per-letter means and spread in a front capture | E/X/T within ±0.08 of 0.54 / 0.65 / 0.53; spread 0.4–0.75; max/min ≤ 40 |
| T5 | **Photosafety.** 60 Hz sampling of each sign's `Level` and a frame-luminance probe | default ≤ 2 flashes/s, episodes ≤ 5 s; REDUCE FLASHING: 0 |
| T6 | **Hero close-up** at 0.3 m, 50° FOV, front and 35° oblique | seams, screw slots, hole walls and diffuser parallax visible; no z-fighting; no texture stretch |
| T7 | **Distance.** 6 / 12 / 30 m in a Run room | word legible at ≤ 12 m; a red bar at 30 m; never culled |
| T8 | **Regression.** RoomStream verification, the map interaction tests, 20 validator seeds | identical apart from the signs |
| T9 | **Era.** No brand, no LED indicator, no date, no running man, no Arial-style legend; red letters | critic sign-off |
| T10 | **RT** (if G14 is on) | a DEAD sign does not glow in the Run floor reflection, or the gap is reported |
| T11 | **Names.** No exit-sign object name contains wall, floor, carpet, ceiling, seal or partition | grep the spawned hierarchy |

---

## 13. Calls for Red

1. **Letters:** red (default, period US practice and DW12's default) or green.
2. **Option S1:** a dead EXIT over the terminal door at the title handoff.
3. **Option O1:** wall signs over the far doors of Office stream rooms.
4. **C-1:** Office-side signs at Office ↔ Level 0 doors on the map.
5. **Shader keyword** (§7.2): yes = state-dependent hot spots (BATTERY has its own look); no = brightness-only states.
6. **Media batch** E1–E12 (`media_candidates.md`): approve or not.

---

## 14. Unverified, risks, and notes for codex-audit

- **UNVERIFIED:**
  - K4 (blank dead face) and the mirror read are derived from code and material files, not seen in the engine. T1 and T3 will show them;
  - `JsonUtility` and `null` in `lodDistances` (K7);
  - the lamp positions and the DC lamp positions (fit and ESTIMATE);
  - housing depth and canopy size (ESTIMATE; E7/E8 would confirm);
  - the 1988 NFPA clause number for headroom.
- **Risk: RT.** Until the RT owner adds the hook, traced reflections show LIT signs.
- **Risk: shader change in a shared file.** It is keyword-gated and bit-identical when off. The merge must diff it against `7320ed1` (Codex did not touch the shader).
- **For codex-audit (`20_findings.md`, exit-sign section):**
  - (a) `Kit_ExitSign_Dead` in main references a missing material (K4);
  - (b) six sidecars in main carry `null` in `lodDistances` (`Kit_ExitSign`, `Kit_ExitSign_Dead`, `Kit_WindowFrame_Alu`, `Kit_WindowFrame_Steel`, `Kit_WindowFrame_Steel_Enamel`, `Kit_WindowFrame_Wood`). Codex's importer change and `Prewarm()` both parse it. This adds to the audit's own importer finding (§3.7).
  - Both are left as they are in main until this workflow's verified merge replaces the two exit-sign files. The window frames belong to the windows track.
