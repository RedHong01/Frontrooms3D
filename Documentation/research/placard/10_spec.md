# 10 — Evacuation-plan placard (Q16): prop, glow and placement spec

Status: **SPEC v1, 2026-10-03 (17:1x PDT).** Research, prop, glow component, placement contract and WebGL path.
- Nothing was built and nothing in the real project was changed, apart from this folder (`10_spec.md`, `SOURCES.md`, `media_candidates.md`).
- Unity was not opened on Frontrooms3D, and no media was downloaded.

**Binding inputs, in order of authority**
1. **Red:** Q16 approved (VISUAL_CHAT_TASKS.md row Q16). Highest spec: a hero LOD0 that holds up at 0.3 m, plus LOD1 and LOD2. Desktop is the reference; WebGL is a separate, reduced track.
2. **The artwork** (平面视觉, strings from narrative A.12): `Tools/print/ink/art_from_graphic/placard/`.
   - `placard_print_lit.png` and `placard_glow_mask.png`: 2592 × 1674 px, 432 × 279 mm at 6 px/mm.
   - **It is not changed here.** Re-packing it for Unity (§3.3) only adds one edge row top and bottom.
3. **The glow rules:** `research/wallpaper_motion/20_level_design_phosphor.md` Rev 3, R1, R2, R15 and §9.
   - **Gate:** `smoothstep(0.55, 0.15, Ls)`.
   - **Charge:** rises toward the wall light W with τ 2 s, decays as `C₀ / (1 + C₀·t / 8 s)`.
   - **Exceptions:** stutter lamps never glow; Reduce flashing applies.
   - **Lamp interface:** "Lamp overrides (interface v1)" in MAP_GENERATION.md, built in main.
4. **Code as it stands**, read 16:3x–17:0x and re-cited at 17:2x (md5 in `SOURCES.md` §6):
   - `FrontRoomsMapWorld.cs` and `FrontRooms3DGame.cs`: the map chat saved both at 17:16:59 while this spec was being written. The lines moved but nothing used here changed meaning; every cite is to the 17:16:59 version.
   - `FrontRoomsRoomStream.cs` (Oct 2, 23:35).
5. **Conventions:**
   - `research/interactables/10_spec.md` §1.8 and §8: the part frame, wear, the LOD convention and P-1;
   - `research/outlets/10_spec.md` §1.1: wall-flush kits, hand-built LODs;
   - `office_and_film/22_era_lock.md`.

**Tags**
- **ESTIMATE**: a number of mine with no source.
- **UNVERIFIED**: not confirmed in a source or a run.
- **SP**: a starting point from the phosphor doc, to tune in playtest.
- **CONTRACT**: an exact change proposed for a map-owned file.
- **NEEDS APPROVAL**: a change to a shared pipeline file (`kitlib.py`, the importer, `FrontRoomsRenderSetup.cs`, a shader). The visual chat decides.

---

## 0. Decisions in one table

| # | Question | Decision | § |
|---|---|---|---|
| D1 | What the object is | A 17 × 11 in evacuation plan in a **1 in clear-anodised aluminium snap frame** with a **non-glare clear lens**, screwed flat to the wall. Snap frames are US products from 1977 on (S1–S5); the term "snap frame" is in a 1980 US design patent (S2) | 1.1 |
| D2 | Kit names | `Kit_EvacPlacard` (frame + printed sheet) and `Kit_EvacPlacardLens` (lens only, transparent). There are two assets so the lens can have its own renderer, sort order and shadow mode, and so WebGL can skip it | 2 |
| D3 | Size | Sheet **432.0 × 279.0 mm** (the artwork; 17 × 11 in is 431.8 × 279.4). Frame outside **466.8 × 313.8 × 13.5 mm**. Sight opening **416.0 × 263.0 mm** | 2.2 |
| D4 | Profile | 25.4 mm face, 13.5 mm deep. A smooth bullnose clip rail with no finger lip (the 1983 tamper-proof type, S4) on a fixed base. Four rails, **mitred 45°**, with a 0.5 mm seam at each mitre and along the hinge | 2.3 |
| D5 | Lens | **1.0 mm** clear non-glare plastic: smoothness 0.55, F0 0.039 (acrylic, n = 1.49). The brief's 2–3 mm acrylic is framing-shop glazing. Snap-frame lenses are thin sheets, about 0.4 mm today (S11); 1.0 mm is the modelling floor. Glass: never | 1.3 |
| D6 | Origin and axes | **The wall-face point at the frame centre.** +Z out of the wall (kit front −Y), +Y up. Nothing lies behind z = 0. The kit is authored with `U()` (interact_key_common.py) | 2.1 |
| D7 | Mounting | Four screws through the base, **hidden under the closed rails**, as on real snap frames (S8: 2 slots per long side). Not modelled; anchors only. No standoffs: snap frames sit flat on the wall | 1.5, 2.6 |
| D8 | Materials | `Prop_Aluminium` (existing; isotropic satin, mean smoothness 0.56). **New:** `Prop_EvacPlan` (the printed sheet, FrontRooms/Surface with the glow mask as its emission map) and `Prop_LensNonGlare` (URP Lit transparent, black base, α 0.04, no shadow caster) | 3 |
| D9 | LODs | Hand-built. LOD0 **1,600** / LOD1 **120** / LOD2 **28** tris; lens 10 tris. Switches at **2 / 6 / 30 m** (lodBias 1, FOV 76°). Until P-1 lands, the FBX holds LOD0 only, as the convention says | 2.7 |
| D10 | Colliders, shadows | Render-only: `kit.no_collider()`, `Spawn(..., colliders: false)`. **ShadowCastingMode.Off on every renderer** (KitLibrary would turn shadows on, because the frame is over 0.3 m: the spawner turns them off). Receives shadows | 2.6, 4.6 |
| D11 | Glow | A component, **`FrontRoomsPlacardGlow`**, not a shader path. It reads the logical `LampLevel` of the placard's cell and its 4 neighbours, runs the R1/R2 charge and gate, and writes `_EmissionColor = tint × PeakEmission × G` into **one material instance per placard**. It uses `SetVector` with linear values, never a MaterialPropertyBlock | 4 |
| D12 | Tint | ZnS:Cu pale yellow-green, sRGB **(0.78, 0.95, 0.58)** = linear (0.5705, 0.8900, 0.2957) | 4.4 |
| D13 | Photosafety | Stutter cells never glow. Ls τ 0.35 s; a G slew limit of 2/s. **Reduce flashing:** Ls τ 1.0 s, failing cells read a 3 s mean, slew 0.5/s | 4.5 |
| D14 | Where | **Map side of the start door**, centre **1.524 m** above the floor (60 in, the US sign height, S15; NYC 1973 caps sign tops at 6 ft, S13). Three mounts, picked from the seed: **P1** on the facade beside the door, **P2** on the side wall facing the door cell, **P3** on the facade past an arch pier | 5.2 |
| D15 | Who places it | The visual chat's code places it: RoomStream's terminal facade is visual-owned, and the rule reads only public map data. **The map chat gets one contract:** `KeepClearAtStart(Rect)`, so no furniture lands in front of it | 5.3, 5.4 |
| D16 | WebGL | Same glow code. No lens renderer (`#if (UNITY_WEBGL && !UNITY_EDITOR) \|\| FRONTROOMS_WEBGL_PREVIEW`). Textures capped at 1024 in the WebGL import tab only. 2 draws | 6 |
| D17 | Hint-language risk | Q2 is on hold, and the legend may be redrawn (`32_pattern_native_hints.md` §5b). Glow option B ("no glow") is still open. So: the glow has a kill switch, and new artwork of the same size drops in with no mesh or code change | 4.7 |

---

## 1. Period research: what a 1990 US building would hang

### 1.1 The snap frame existed, in aluminium, with mitred corners

| Year | Evidence | What it proves |
|---|---|---|
| 1975/76 | Kapstad, US 3,955,298 (S6) | Extruded-aluminium poster frames with L-shaped corner keys and a "clear plastic protective sheet" |
| **1977/79** | Marketing Displays Inc., US 4,145,828 (S1) | **The snap frame itself.** Extruded front cover portions are hinged to back portions, and a flat leaf spring gives the snap. Sections are mitred at 45° on a rigid backing (Masonite or aluminium). The listed uses include "building walls" and "indoors" |
| **1980/82** | Beier, US D263,571, "Snap frame" (S2) | The name was in use by 1980. The drawings show the frame open, half open and closed |
| 1983/85 | MDI, US 4,519,152, tamper-proof (S4) | A smooth, rounded front member that fingers cannot grip; it opens only with a wedge tool. Aluminium or rigid PVC; walls in stations, subway terminals and theatres |
| 1983/85 | MDI, US 4,512,094 (S5) | A cheaper plastic version, also 45° mitred. It calls the extruded-aluminium frames the prior art |
| 1985/87 | M&M Displays (Philadelphia), US 4,702,025 (S3) | A second US maker: aluminium front and rear, a cylindrical hinge, a stainless spring |

**Verdict.** A snap frame on a 1990 office wall is in period. It was designed ≥ 13 years before "now", well inside the era lock (newest design 1993).
- **Exists:** VERIFIED (patents, read).
- **Common in offices by the mid-1980s:** UNVERIFIED. No period photo was found (`media_candidates.md`, "not found"), but the patents name walls and public interiors.
- **The front member:** the tamper-proof rounded type is the one that suits a life-safety sheet in a public corridor (D4).

### 1.2 Sizes and profile (modern makers fix the numbers; the period values are ESTIMATE)

| Item | Value | Source |
|---|---|---|
| Profile face width | 25 mm (1 in) for small posters; 30–32 mm for big ones | S8, S9, S10 |
| Profile depth | **13.5 mm** for a 25 mm profile | S8 (read) |
| Frame past the poster edge | 15.5 mm per side (25 mm profile) | S8: A4 210 → 241 outside |
| Frame over the poster | 9.5 mm per side | S8: A4 210 → 191 copy area |
| 11 × 17 in, 1 in profile | outside 12.22 × 18.22 in, viewable 9.78 × 15.78 in (15.5 mm over the poster) | S10 (snippet) |
| Corners | mitred; the front rails open one by one | S1, S5, S8 |
| Finish | silver (clear) anodised; black as an option | S8, S9 |
| 11 × 17 in | ANSI B, the US tabloid copier size; a plan is copied, not printed by a sign shop | general knowledge, not fetched |

**Our choice (D3).** A 25.4 mm face, but **8.0 mm** over the sheet (not 9.5–15.5 mm).
- Why: the artwork's outer rule sits 11.67–12.33 mm in from the sheet edge (measured).
- With 9.5 mm the rule would sit 2.2 mm from the lip; with 15.5 mm it would be hidden.
- At 8.0 mm a 3.7 mm strip of paper shows outside the rule. Real frames vary this much between makers.

### 1.3 The lens: thin plastic, not glass, not thick acrylic

- **Snap frames always use a plastic lens.** The rails clamp a flexible sheet: today 0.4 mm anti-glare PVC or semi-matte PET (S8, S11). The 1990 lens material and thickness are UNVERIFIED.
- **Glass:** never in a snap frame (it would crack under the rails).
- **Acrylic:**
  - Plexiglas has been a trademark since 1933 and was made commercially from 1936 (S18), so it is in period.
  - n = 1.4905 gives F0 = ((n − 1)/(n + 1))² = **0.039**.
  - The lens lies on the paper with no air gap, so only its front face reflects: about 4 % face-on.
  - Acrylic blocks UV only below about 300 nm (S18). The 365–450 nm light that charges ZnS:Cu gets through, so the ink charges under the lens. That fits the fiction.
- **Non-glare:** matte non-glare acrylic exists for framing (S19, date UNVERIFIED). Snap-frame lenses today are anti-glare (S8, S9).

**Our choice (D5).**
- A 1.0 mm non-glare lens: smoothness 0.55, F0 0.039.
- Physically 0.4–1.0 mm; 1.0 mm keeps the slab safe from depth-precision and shadow-bias trouble.
- **Deviation from the brief:** the brief said 2–3 mm acrylic. That thickness belongs to back-loaded sign frames and framing-shop glazing. The lens's edges hide under the rails either way, so the visible difference is about 1 mm of reflection parallax.

### 1.4 How the sheet sits (the stack)

From the wall out (S1, S7, S8). The heights are the w axis of §2.3, in mm above the wall.

| Layer | From | To | Modelled? |
|---|---|---|---|
| Base profile bed (aluminium) | 0.00 | 1.20 | only its outer strip (visible below the hinge seam) |
| Backing, 1/8 in tempered hardboard (S1: "Masonite"; today PS) | 1.20 | 4.40 | no (hidden) |
| Printed sheet (0.15 mm paper) | 4.40 | **4.55** | yes: one quad at w = 4.55 |
| Lens, 1.0 mm | 4.55 | **5.55** | yes: a separate asset, top face and edges only |
| Rail lip nose | touches the lens at w = 5.55 | | yes |

### 1.5 Mounting and protrusion

- **Screws.** A snap frame is screwed to the wall through slots in the base profile, **under** the front rails. Today's makers fit 2 slots per long side, with 4 screws and 4 plugs (S8). Closed, no screw shows.
  - So the placard models no screws: they would be hidden triangles.
  - The sidecar records `screw_0…3` anchors, for a later open-frame variant.
- **Standoffs:** not used with snap frames.
- **Protrusion:** 13.5 mm, against the UFAS 1984 limit of 4 in (102 mm) for wall objects 27–80 in above the floor (S14).

### 1.6 Evacuation plans in US buildings before 1990

- **NYC Local Law 5 of 1973** (S13, read in the primary PDF). Office buildings with more than 100 occupants above or below street level, or more than 500 in all, must post on every floor at the elevator landing:
  - `IN CASE OF FIRE, USE STAIRS UNLESS OTHERWISE INSTRUCTED`, in red block letters ≥ ½ in on white;
  - **"a diagram showing the location where it is posted"** (the origin of `YOU ARE HERE`) and the stairs;
  - size ≥ 10 × 12 in, top ≤ 6 ft above the floor, "securely attached";
  - or separate diagram signs ≥ 8 × 12 in in conspicuous places.
  - Our 17 × 11 in sheet with its top at 1.68 m (5.5 ft) meets every number.
- **Sign height and side.**
  - UFAS 1984 and ADAAG 1991 §4.30.6 (S15) put door signs **60 in (1,524 mm) to the centreline**, on the latch side.
  - At double-leaf doors (ours, §5.1) they go on **the nearest adjacent wall**, where a reader can come within 3 in "without … standing within the swing of a door".
  - ADAAG is from 1991, a year after "now", but UFAS (1984) carries the same rule (wording UNVERIFIED). Either way 60 in was the period norm.
- **Alignment.**
  - Levine, Marchon and Hanley (1984, S16) studied how often real you-are-here maps were hung misaligned with the reader's view, which sends people the wrong way.
  - The placard is "wrong" in the same period-true way. Its `YOU ARE HERE` sits in the start room, a room the reader on the map side has already left. The plan's door is on the room's east wall, while the game's start door is on the stream room's north wall.
  - The narrative chat made the first choice on purpose (`32_pattern_native_hints.md` §5b). The second fits it. Neither is a prop problem.

### 1.7 The ink on the sheet

- **Pigment:** ZnS:Cu, peak about 531 nm. It charges in light and fades over tens of minutes. Strontium aluminate is too late (1993–94) (S20).
- **By day:** the legend ink is #D6DEBD (measured mean under the mask: RGB 214, 222, 189).
  - The on-disk photo `gn_photoluminescent_exit_sign.jpg` shows a modern photoluminescent sign with the same pale cream-green body by day.
  - Its printed caution asks for "minimum 54 lux … on the sign face at all times": the "no light in, no light out" rule the charge model encodes.
- **Real brightness:** real afterglow is about 25 mcd/m² at 10 minutes (S20). A lit office wall is about 100 cd/m², so the real ratio is about 0.03 %.
  - The game shows the glow at up to 12 % of a lit wall (LD §9), a deliberate stylisation of a dark-adapted eye. The placard keeps the walls' cap (§4.4).

### 1.8 Findings for other chats (the prop does not depend on them)

| # | For | Finding |
|---|---|---|
| F1 | Narrative, 平面视觉 | **Footer string conflict.** The artwork's footer is `IN CASE OF FIRE DO NOT USE ELEVATORS`. The relay-pursuit narrative's verified table (`research/relay_pursuit/30_narrative.md` §7, "Consequences for the strings") wants `IN CASE OF FIRE: WALK, DO NOT RUN.` / `CLOSE DOORS BEHIND YOU.` on this placard, and calls the elevator line non-code: use the NYC wording or drop it. S13 confirms the NYC wording from 1973. Changing it is a re-run of `placard.swift`; the prop needs no change |
| F2 | Narrative, 平面视觉 | The plan draws a **single** door with one swing arc. The real start door is a **2.4 m double door** (two 1.12 m leaves, §5.1). It is optional to fix; a plan simplifies |
| F3 | Red, wallpaper chat | Under the pattern-native hint proposal (`32_pattern_native_hints.md` §5b), the legend would be redrawn as WP03 chevron transforms, and glow option B means no glow at all. The prop is built so both are a texture swap or a flag (D17, §4.7) |

---

## 2. The kit: `Kit_EvacPlacard` and `Kit_EvacPlacardLens`

Modules:
- `Tools/Blender/frontrooms_kit/assets/evac_placard.py` (`NAME = "Kit_EvacPlacard"`);
- `Tools/Blender/frontrooms_kit/assets/evac_placard_lens.py` (`NAME = "Kit_EvacPlacardLens"`);
- shared numbers in `evac_placard_common.py` (no NAME, no build).

They reuse `interact_key_common.py` (`U`, `profile_sweep`, `quad_uv`, `wear_all`, `lod_meta`, `eval_bounds_unity`, `check_budget`), as `interact_exit_sign.py` does. **No `kitlib.py` change is needed.** New slots come in through `kitlib.register_slot(...)` (kitlib.py:132-134).

### 2.1 Frame and origin

- **Part frame** (as interactables §1.2 and `interact_exit_sign.py`): Unity part-local metres, converted with `U(X, Y, Z)`.
- **Origin = the point on the wall face at the frame centre.** The frame's back plane is z = 0.
- **Axes:** +Z out of the wall (the kit front, Blender −Y), +Y up. **Seen from the front, the viewer's right is −X** (the order `interact_exit_sign.py` uses in `quad_uv`).
- **Nothing lies behind z = 0.** Placement needs only the face point and the outward normal (§5).
- The placard is symmetric in X and Y, so the placement code never mirrors it. The sheet is not symmetric, so it is never scaled −1.

### 2.2 Dimensions (mm; asserted by the module, ±0.05)

| Item | Value |
|---|---|
| Sheet | 432.0 × 279.0, top face at w = 4.55 |
| Frame outside | **466.8 × 313.8** (sheet + 2 × 17.4) |
| Rail face width | 25.4 |
| Rail over the sheet | 8.0 (sheet edge at inset 17.4; sight edge at inset 25.4) |
| Sight opening | **416.0 × 263.0** |
| Depth (wall to crown) | **13.5** |
| Lens | 432.0 × 279.0 × 1.0, w 4.55–5.55; edges under the rails |
| Visible paper outside the printed rule | 3.67 per side (the rule is at 11.67–12.33 from the sheet edge) |
| Mitre seam | 0.5 wide V (each rail end chamfered 0.25 × 45°), rails 0.05 apart |
| Hinge seam | 0.40 tall V, 0.35 deep, on the outer side at w 2.80–3.20 |

### 2.3 Profile section (one rail, LOD0)

Coordinates: **u** = inset from the frame's outside edge toward the centre; **w** = height above the wall (mm). Use the `profile_sweep` convention: inset > 0 is inward.

| Part | Points / curve | Segments (LOD0) |
|---|---|---|
| Base, outer strip | (0.00, 0.00) → (0.00, 2.80) | 1 |
| Hinge seam V | (0.00, 2.80) → (0.35, 3.00) → (0.00, 3.20) | 2 |
| Rail, outer side | (0.00, 3.20) → (0.00, 8.50) | 1 |
| Bullnose | arc R 5.00, centre (5.00, 8.50), from (0.00, 8.50) to the crown (5.00, 13.50); tangent at both ends | **16** |
| Face | one convex arc R ≈ 29.6 (centre about (5.00, −16.11)), from the crown down to the lip nose, tangent at both ends; it leaves the crown level and meets the nose at about 40° down | **20** |
| Lip nose | arc R 0.80, centre (24.60, 6.35); innermost point (25.40, 6.35) = **the sight edge**; lowest point (24.60, 5.55) **touches the lens** | **8** |
| Underside (hidden) | (24.60, 5.55) → (18.00, 5.75), open edge | 1 |

- **The base** under the rails (bed, screw slots, hinge bead) is never seen with the frame shut, so it is not modelled. Only its outer strip is: the band below the hinge seam.
- **Mitres.** Build the four rails as four straight extrusions, each cut at 45° at both ends (e.g. `bmesh.ops.bisect_plane`). Then chamfer each visible mitre edge 0.25 mm. The base strip is one closed `profile_sweep` round the rectangle.
- **Tamper-proof reading (S4).** The bullnose and face run in one smooth convex curve with no step a finger could catch. The only breaks in the silhouette are the hinge seam and the mitres.

At 0.3 m (1080p, FOV 76°: about 2.3 px/mm) the 13.5 mm profile is about 31 px tall. A bullnose segment is then 1.1 px and a face segment 1.0 px, so nothing facets. That holds at 1440p as well.

### 2.4 Parts per LOD

| Level | Distance | Rails | Base strip | Sheet | Seams |
|---|---|---|---|---|---|
| LOD0 | 0–2 m | 4 mitred extrusions, the §2.3 profile (about 48 points) | yes | 1 quad, UV 0–1 (§2.5) | mitre and hinge seams as real V geometry |
| LOD1 | 2–6 m | **one** closed `profile_sweep`, 14 points (outer side 2, bullnose 4, face 5, nose 2, underside 1) | 2 spans | 1 quad | none (sub-pixel: 0.5 mm is 0.2 px at 2 m) |
| LOD2 | 6–30 m, culled beyond | one closed sweep, 4 points: (0, 0), (0, 9.0), (6.0, 13.5), (25.4, 5.55) | none | 1 quad | none |

The lens asset `Kit_EvacPlacardLens` has one part:
- a top quad at w = 5.55 plus 4 edge quads down to w 4.60, about 10 tris;
- **no bottom face**, so it is never coplanar with the opaque sheet, which would z-fight.

### 2.5 Slots and the sheet's UVs

| Slot | Parts | Status |
|---|---|---|
| `Prop_Aluminium` | rails, base strip | existing (`FrontRoomsRenderSetup.cs:327`); isotropic satin, so the metre UVs need no grain direction |
| `Prop_EvacPlan` | the sheet quad | **new** (§3.1) |
| `Prop_LensNonGlare` | the lens (separate asset) | **new** (§3.2) |

**Sheet UVs:** `quad_uv` with explicit corners. With the X convention of §2.1:

| Corner (Unity part space, m) | UV |
|---|---|
| (+0.216, −0.1395, 0.00455) | (0, v0) |
| (−0.216, −0.1395, 0.00455) | (1, v0) |
| (−0.216, +0.1395, 0.00455) | (1, v1) |
| (+0.216, +0.1395, 0.00455) | (0, v1) |

- v0 = 1/1676 and v1 = 1675/1676 skip the padding rows of §3.3.
- **The check:** in Unity, `EVACUATION PLAN` reads left to right at the top-left, and the red footer is at the bottom-left.

**Preview colours** (Blender only), via `register_slot`:
- `Prop_EvacPlan`: (0.957, 0.945, 0.910), roughness 0.85, metallic 0.
- `Prop_LensNonGlare`: (0.90, 0.92, 0.92), roughness 0.45, metallic 0.

The kit's Cycles preview will show the sheet as a flat colour. The three-view for 平面视觉 needs the artwork on it (§7, B4).

### 2.6 Sidecar

- `kit.no_collider()`.
- **Tags:** `placard`, `sign`, `wall_flush`. **Not** `wall_unit` or `wall_decor`, which would make the Level Designer snap it 0.03 m off the wall (outlets §1.1).
- **Anchors** (Unity part space, metres):

| Anchor | Position | Use |
|---|---|---|
| `back` | (0, 0, 0) | the mount point |
| `face` | (0, 0, 0.00455) | the sheet centre |
| `face_dir` | (0, 0, 0.10455) | the facing direction |
| `lens_top` | (0, 0, 0.00555) | |
| `legend_centre` | (−0.1374, −0.0033, 0.00455) | the centre of the glow mask's bounding box, measured at sheet x 300–406.8 mm and y 56.7–228.8 mm from the top-left. For lookdev cameras |
| `screw_0…3` | (±0.150, ±0.1459, 0.0012) | hidden; for a future open-frame variant |

- **Meta:**
  - `kit.meta["placard"] = {"sheet": [0.432, 0.279], "sight": [0.416, 0.263], "frame": [0.4668, 0.3138], "depth": 0.0135, "slotPrint": "Prop_EvacPlan", "uvV": [1/1676, 1675/1676]}`;
  - `lod_meta(kit, (2.0, 6.0, 30.0), (1600, 120, 28))`.

### 2.7 LOD budgets and switches

| Asset | LOD0 | LOD1 | LOD2 | Switch (m, lodBias 1, FOV 76°) |
|---|---|---|---|---|
| `Kit_EvacPlacard` | 1,600 ± 15 % | 120 | 28 | 2.0 / 6.0 / cull 30 |
| `Kit_EvacPlacardLens` | 10 | — | — | cull 6.0 |

**Screen sizes.** The placard is 0.314 m tall, so its screen-height fraction is 0.201/d:
- 10 % at 2 m (108 px at 1080p);
- 3.3 % at 6 m;
- 0.67 % at 30 m (7 px).

Standalone Ultra runs lodBias 2, which doubles every distance.

**Deviation.** Red's LOD rule of thumb is LOD1 at 40–50 % and LOD2 at 10–20 %. That is a decimation guide. These LODs are built by hand, and LOD0's extra triangles are all fillet segments that are sub-pixel beyond 2 m (the profile is about 4.7 px tall there). So the ratios come out at 7.5 % and 1.8 %. Red can ask for a denser LOD1 if a capture shows a pop.

**Pipeline state.** P-1 (LOD2 and per-asset distances in kitlib and the importer; interactables §8) has not landed.
- `kitlib.py` has only `make_lod1`.
- `FrontRoomsKitImporter.cs:75-76` sets 10 % and 3 % fixed.
- So, per the convention:
  - `LOD1 = None`;
  - mark parts with `obj["fr_lods"] = "0"`, `"1"`, `"2"` or a mix such as `"012"`;
  - build the LOD1 and LOD2 parts **only when `hasattr(kitlib.Kit, "make_lods")`**.
- The FBX holds LOD0 only until then. There is one placard per run, so that costs nothing.

### 2.8 Module asserts

- Bounds: X 0.4668 ± 1e-4, Y 0.3138 ± 1e-4, Z ∈ [0, 0.0135 + 1e-4].
- No vertex at z < −1e-6.
- Sight opening (the inner edge of the lip noses) 0.4160 × 0.2630 ± 1e-4.
- The lowest point of the lip nose is at w 5.55 ± 0.02 mm.
- The sheet quad is at w 4.55, its UV corners are as in §2.5, and the slot is `Prop_EvacPlan`.
- `check_budget(kit, 1600)`. The lens asset: bounds 0.432 × 0.279, z 4.60–5.55 mm, ≤ 12 tris.

### 2.9 Wear (W1 now; W2 waits for P-3)

- **Geometry:** none beyond the seams. A clean, recently hung frame suits a "printed 03/90" plan.
- **`fr_wear`:**
  - white everywhere;
  - 0.85 on the outer 20 % of the top rail's crown (dust settles on the top edge);
  - 0.90 on the bottom rail's face centre ±60 mm (thumbs, from opening it).
- **Lens grime:** none in v1. If the lens moves onto FrontRooms/Glass (§3.2, option), its grime map gives smears at hand height for free.

---

## 3. Materials and textures (visual-owned; built in the clone, listed for promotion)

### 3.1 `Prop_EvacPlan` (FrontRooms/Surface, opaque)

New row in `FrontRoomsRenderSetup.SurfaceDefs` (NEEDS APPROVAL; a one-line data row):

```csharp
// Q16 evacuation placard: the printed sheet (research/placard/10_spec.md §3.1). The phosphor legend is the
// emission map; FrontRoomsPlacardGlow drives _EmissionColor per placard (black at rest).
new SurfaceDef { name = "Prop_EvacPlan", texture = "Prop_EvacPlan", tile = Vector2.one, meshUV = true, smooth = .15f, macroTone = 0f, macroDirt = .02f, emission = "Prop_EvacPlan", emissionColor = Color.black },
```

- **Smoothness 0.15:** matte offset print. The lens carries the sheen.
- **Macro wear:** tone 0, dirt 0.02. The world-space macro map (FrontRoomsSurface.shader:189-198) must not stain the print.
- **`_EMISSION` stays on at black.** It is a `shader_feature_local_fragment` (:97), so the keyword in the material asset is what keeps the variant in builds.
- **No `_N` and no `_S`.** The mask defaults to white, so smoothness = `_Smoothness` and cavity = 1.

### 3.2 `Prop_LensNonGlare` (URP Lit, transparent)

New entry in `GlassDefs`, plus a shadow-caster switch for this material only (NEEDS APPROVAL):

```csharp
("Prop_LensNonGlare", new Color(0f, 0f, 0f, .04f), .55f),   // Q16: non-glare snap-frame lens (10_spec §3.2)
// in EnsureGlassMaterials, after the loop body sets up m:
if (name == "Prop_LensNonGlare") m.SetShaderPassEnabled("ShadowCaster", false);
```

- **Black base, α 0.04.** The lens takes away about 4 % of the light (the front-face Fresnel) and adds back only its specular. URP 17 keeps specular un-faded on transparents (preserve specular lighting on).
- **Why not a pale base.** The interaction audit showed what a pale base does: URP Lit at α 0.28 with a light colour reads as a "milky teal veil" (`interaction_audit/05_in_engine_evidence.md` §0.3). A black base at α 0.04 cannot veil.
- **Smoothness 0.55:** non-glare. Troffers reflect as a broad soft smear, never a sharp image, so the glow is never washed out by a mirror image of a lit neighbour.
- **No shadow caster.** Otherwise a shadowing lamp would cast the lens's shadow onto the paper 1 mm below. DepthOnly is already off for glass defs.
- **Option, after the glass track is promoted** (`research/glass/30_final.md` §4): move this slot onto `FrontRooms/Glass` with `_PaneF0` 0.039 (one reflecting face) and the glass grime map. Not in v1.

### 3.3 Texture pack (new tool `Tools/lookdev/pack_evac_plan.py`, visual-owned)

**Inputs:** the two artwork PNGs. Record their md5 in the output report: print 7b96ea08…, mask 5ee79d13….

**Outputs:**

| File | Content | Size |
|---|---|---|
| `Assets/Resources/Surfaces/Textures/Prop_EvacPlan_A.png` | RGB of `placard_print_lit.png` (alpha dropped; it is 255 everywhere) | **2592 × 1676** |
| `Assets/Resources/Surfaces/Textures/Prop_EvacPlan_E.png` | RGB greyscale from the mask's R channel. R = G = B; white = phosphor | 2592 × 1676 |

**Padding:** one row is copied at the top and one at the bottom (edge replicate), so that 1676 is a multiple of 4 for block compression. 1674 is not.
- The artwork pixels are untouched. The script asserts that rows 1–1674 equal the source byte for byte.
- The UVs skip the padding (§2.5). The edges sit under the rails anyway.

**Importer rule** in `FrontRoomsSurfaceTextureImporter` (`FrontRoomsRenderSetup.cs:617-642`; NEEDS APPROVAL), for stems that start with `Prop_EvacPlan`:
- `npotScale = None`. The default would rescale to 2048 × 2048 and stretch the type.
- `wrapMode = Clamp`.
- `_A`: sRGB, CompressedHQ (BC7, 5.8 MB with mips).
- `_E`: **linear** (the mask is coverage; an sRGB decode would thin the anti-aliased edges), Compressed (BC1, 2.9 MB with mips).
- About 8.7 MB in total on desktop, for the one hero sign.

**Why not R8/BC4 for the mask.** The shader reads `_EmissionMap.rgb` (FrontRoomsSurface.shader:252). A one-channel texture returns (r, 0, 0), so only red would glow.

### 3.4 Promotion list for §3

- `FrontRoomsRenderSetup.cs`: the `Prop_EvacPlan` SurfaceDef row; the `Prop_LensNonGlare` GlassDefs row and its shadow-caster line; the importer rule.
- `Tools/lookdev/pack_evac_plan.py` (new).
- `Assets/Resources/Surfaces/Textures/Prop_EvacPlan_A.png` and `_E.png`, with their `.meta`.
- `Assets/Resources/Surfaces/Prop_EvacPlan.mat` and `Prop_LensNonGlare.mat`, with their `.meta`. RenderSetup regenerates them, but the GUIDs must travel with the FBX remaps.

---

## 4. The glow: `FrontRoomsPlacardGlow`

### 4.1 A component, not a shader path

- **The wall ink's shader path is on hold.** Q2 is ON HOLD (VISUAL_CHAT_TASKS.md row Q2), and its globals (`_FR_CellState`, `_FR_Ink`, `_FR_InkColor`) do not exist.
- **The placard is one object** with a hand-drawn mask. It needs no per-cell texture, no InkShape and no new shader keyword. The stock `_EMISSION` path does it: `emission = _EmissionMap.rgb × _EmissionColor.rgb` (FrontRoomsSurface.shader:252), fogged by `MixFog` (:256) like the walls.
- **The maths is shared.** It sits in a small static `FrontRoomsPhosphor` class, so the wall driver can call the same functions if Q2 resumes. Walls and placard then glow alike.

### 4.2 Inputs (logical lamp levels only)

| Read | API | Notes |
|---|---|---|
| The placard cell's level L | `map.LampLevel(cell)` (FrontRoomsMapWorld.cs:2851) | Overrides included (sags, dips, Warn bursts). `NoLamp` (−1) reads as 0 |
| The 4 neighbours | `map.LampLevel(n)` | Start-area cells and Off modules return −1 and count as 0. The stream room's lamps behind the facade never charge the placard |
| The temperament | `map.LampModeOf(cell)` (:2782) | Pure; re-read at 1 Hz, so a live `SetLampMode` is picked up |
| Built? | `map.IsBuilt(cell)` (:774) | Until the chunk is built, hold the last state (the door is shut then anyway) |

**Never read:**
- `light.enabled`, `light.intensity` or the lens emission;
- the start-area hold `StartLampsNorth` (:1493), which sits outside `f.level` (LD R1).

**The start-door case.** While the door is shut the placard's lamp is held dark visually but reads as lit logically. So G = 0 and nothing glows unseen. When the door opens, the lamps rise over 0.6 s and G stays 0: no false glow at the reveal.

### 4.3 Per-frame model (LD R1, R2, §4; numbers SP)

| Step | Formula | Constant |
|---|---|---|
| Wall light | `W = min(1, L + 0.2 · Σ L_neighbours)` | 0.2 |
| Lamp low-pass | `Ls += (L − Ls)(1 − e^(−dt/τ))` | τ 0.35 s (Reduce flashing: 1.0 s) |
| Read level | `Lr = Ls`; under Reduce flashing in a **Failing** cell, `Lr` = the 3 s box mean of L (30 samples at 10 Hz) | |
| Charge up | if W > C: `C += (W − C)(1 − e^(−dt/2))` | τ 2 s |
| Charge down | else `C = C / (1 + C · dt / 8)` | 8 s. Exact for `C₀/(1 + C₀t/8)` at any frame rate |
| Gate | `gate = smoothstep(0.55, 0.15, Lr)`: 0 at Lr ≥ 0.55, 1 at Lr ≤ 0.15 | |
| Glow | `G = C · gate · 1.0`; **0 if the mode is Stutter** (R15) | weight 1: every legend glyph is a message |
| Slew | `shown = MoveTowards(shown, G, rate · dt)` | 2.0/s (Reduce flashing: 0.5/s) |
| Emission | `_EmissionColor = TintLinear · PeakEmission · shown` | §4.4 |

**Initial C**, set on the first frame the cell is built (LD §4):

| Mode | Initial C |
|---|---|
| Steady | 1.0 |
| Stutter | 1.0 |
| Failing | 0.75 |
| Dim | 0.5 |
| Dead | W (its spill equilibrium) |
| Off (module) | 0, then charges toward W |

**What each temperament does to the placard:**

| Placard cell's lamp | Odds at generation tier 1 (LD §4) | What the player sees |
|---|---|---|
| Steady 0.98 | 62 % | No glow. The legend shows faintly by day as #D6DEBD |
| Stutter | 20 % | No glow, ever |
| Failing 0.25–0.70 at 0.49 Hz with dropouts | 10 % | Breathes: readable for about 1 s every about 2 s. Reduce flashing: a steady, faint read |
| Dead, blinks to 0.8 every 8–28 s | 5 % | Steady spill glow. C ≈ 0.39 with 2 lit neighbours (Legible ≥ 0.25). A blink dips G to about 0.6 C for about 0.3 s; with Reduce flashing there is no visible dip |
| Dim 0.42 | 3 % | gate ≈ 0.25, so G ≈ 0.25 C: a faint short-range read both lit and dark |
| A sag or dip passing | | The legend blooms with the dip and fades over the release |

So in most runs the placard never glows. That is the honest reading of "its glow legend lights only when its lamp is off" (narrative A.2). §5.6 gives Red the option of a pinned Dim lamp.

**The 2/s slew is new** (it is not in LD), for photosafety on the placard:
- A train of Warn bursts (0.08/0.2 s envelopes; MAP_GENERATION "Lamp overrides") can push Ls across the gate erratically (LD R1).
- The slew turns any train into one swell, the same "one smooth rise and fall per burst" rule as the walls' gasp (LD R15).
- A real afterglow appears instantly when the light goes. The 0.5 s ramp reads as the eye adjusting.

### 4.4 Emission value

- **Tint.** sRGB (0.78, 0.95, 0.58), from `Tools/print/ink_tool.py:586` and VISUAL_CHAT_TASKS Q2. Linear: **(0.5705, 0.8900, 0.2957)**, Rec.709 luminance 0.779.
- **Setting it.** Use `material.SetVector(_EmissionColor, linear)`. `SetColor` would gamma-convert the *product* `tint × k × G` channel by channel, which is wrong.
- **`PeakEmission` k = 0.13** (ESTIMATE). Then a full-charge glyph adds 0.10 linear luminance.
- **Where 0.10 comes from.** A Steady lamp (intensity 5, a 162° spot about 2.2 m from the placard; `FrontRoomsMapWorld.cs:1452-1462`) lights the sheet to about 0.6–1.0 linear with neighbour spill. So 0.10 is about 12 % of the lit sheet, the cap in LD §9.
- **Lookdev sets the final k:**
  - measure the lit sheet's luminance `L_lit` in a Steady cell at 1.5 m;
  - set `k = 0.12 × L_lit / 0.779`;
  - accept when a full-charge Dead cell with 2 lit neighbours gives glyph/paper ≥ 1.3, and a Failing cell at Ls 0.3 gives ≥ 1.15 (LD §9).
- **Bloom.** 0.10 is far below the post stack's bloom threshold, so the placard never haloes.

**One material instance per placard:**
- `new Material(shared) { name = "Prop_EvacPlan (Placard)" }`, put into every LOD renderer's `Prop_EvacPlan` slot through `sharedMaterials`.
- **Never** `renderer.material`, which leaks one copy per renderer. **Never** a MaterialPropertyBlock: it takes the renderer off the SRP Batcher.
- `_EmissionColor` is in `UnityPerMaterial` (FrontRoomsSurface.shader:69), so changing it keeps SRP batching.
- Write only when a channel changes by more than 1/1024.
- Destroy the instance in `OnDestroy`. **Never touch the shared `Prop_EvacPlan.mat`.**

### 4.5 Reduce flashing and photosafety (one setting: `FrontRoomsSettings.ReduceFlashing`)

| Item | Normal | Reduce flashing |
|---|---|---|
| Ls τ | 0.35 s | 1.0 s |
| Failing cell | Ls (breathes about 0.5 Hz) | 3 s mean (steady, faint) |
| G slew | 2.0/s | 0.5/s (a full swing takes ≥ 2 s) |
| Stutter | G = 0 | G = 0 |

- Subscribe to `FrontRoomsSettings.Changed` and re-read the setting there.
- **Worst case:** under the normal settings, G changes at ≤ 0.49 Hz from lamps, and any faster input is slewed. The P2 gate (≤ 3 flashes/s; LD §9) holds by construction.
- The placard covers under 2 % of the screen beyond 1.5 m, so it adds nothing to the flash audit beyond the lamps.

### 4.6 Lifecycle

**1. `FrontRoomsRoomStream.EndStreamAt`** (visual-owned, :503) calls `FrontRoomsPlacard.Prepare(terminal.root.transform, doorPointWorld, facadeFaceZWorld)` after `BuildFacade` (:532).
- `doorPointWorld = terminal.root.transform.TransformPoint(0, 0, RoomLength)`.
- `facadeFaceZWorld` = the world z of the "far side" reveal plane, `RoomLength + DoorWallDepth/2 + 0.002` local (:1231).
- It adds an inactive child `Placard mount` with a `FrontRoomsPlacardMount` component, which subscribes to `FrontRooms3DGame.MapRunStarted`.

**2. `MapRunStarted(map, relay)`** fires in `StartRunInPlace` (FrontRooms3DGame.cs:684), after `EndStreamAt` (:597) and `map.Begin` (:653).
- Rooms are dressed one per frame from `Update`, so this is early enough for the keep-clear (§5.4).
- The mount:
  - resolves the spot (§5.2);
  - calls `map.KeepClearAtStart(rect)`, if the contract method exists;
  - spawns `FrontRoomsKitLibrary.Spawn("Kit_EvacPlacard", mount, pos, rot, colliders: false)`;
  - spawns the lens asset the same way (not on WebGL);
  - **sets `shadowCastingMode = Off` on all their renderers.** `ApplyMaterials` would have set On (FrontRoomsKitLibrary.cs:236);
  - adds `FrontRoomsPlacardGlow` and binds it (map, relay, cell, renderers);
  - logs `[Placard] mount P1 west, cell (x, y)`.

**3. `relay.Caught`** freezes the glow: no more updates, and the current emission is kept. The phosphor doc requires the ink to freeze on Caught (LD §4).

**4. Teardown.** The mount is a child of the terminal room, so the placard lives exactly as long as the facade.
- It is destroyed by `StopTitleCorridor` once every chunk around the start area has dropped (FrontRooms3DGame.cs:855-858), or by a restart.
- `OnDestroy` unsubscribes from every static event and destroys the material instance.

**5. Determinism.** The spot depends only on the seed (`map.Cache.Edge`). There is no `UnityEngine.Random` and no `_Time`. The glow depends on the lamp timeline, as the walls' does (LD R16).

### 4.7 Kill switch and artwork swap (D17)

- **`public static bool FrontRoomsPlacardGlow.Enabled = true`.**
  - If Red picks glow option B ("no glow") for the pattern-native hints, set it to false: emission stays black and the legend reads only in light.
  - When Q2 resumes, the wall driver can own this flag.
- **A redrawn legend** (`32_pattern_native_hints.md` §5b) arrives as new `placard_print_lit.png` / `placard_glow_mask.png` at the same 432 × 279 mm. Re-run `pack_evac_plan.py`. No mesh, UV or code change.

### 4.8 Lookdev hooks

- `public static void Preview(Material instance, float g)` sets the emission for a given G with no map. The kit lookdev captures call it (§7).
- **`ForceLevel`** (editor-only, `#if UNITY_EDITOR`): overrides L, to film breathing cells without hunting for a seed.

### 4.9 Code sketch (the build task writes it; the names are binding)

```csharp
// Assets/Scripts/Rendering/FrontRoomsPhosphor.cs: shared ZnS:Cu rules (LD R1/R2), pure functions.
public static class FrontRoomsPhosphor
{
    public const float GateLit = .55f, GateDark = .15f, Spill = .2f, RiseTau = 2f, DecayT = 8f;
    public static float LsTau => FrontRoomsSettings.ReduceFlashing ? 1f : .35f;
    public static float Gate(float ls) { var t = Mathf.Clamp01((ls - GateLit) / (GateDark - GateLit)); return t * t * (3f - 2f * t); }
    public static float Step(float c, float w, float dt) => w > c ? c + (w - c) * (1f - Mathf.Exp(-dt / RiseTau)) : c / (1f + c * dt / DecayT);
    public static float Initial(ModuleLamp mode, float w) => mode switch
    {
        ModuleLamp.Failing => .75f, ModuleLamp.Dim => .5f, ModuleLamp.Dead => w, ModuleLamp.Off => 0f, _ => 1f,
    };
}

// Assets/Scripts/Rendering/FrontRoomsPlacardGlow.cs (excerpt)
void Update()
{
    if (!Enabled || map == null || frozen) return;
    var dt = Time.deltaTime;
    var calm = FrontRoomsSettings.ReduceFlashing;
    if ((modeAge -= dt) <= 0f) { mode = map.LampModeOf(cell); modeAge = 1f; }
    if (mode != ModuleLamp.Off && !map.IsBuilt(cell)) return;
    var l = Level(cell);
    var w = Mathf.Min(1f, l + FrontRoomsPhosphor.Spill * (Level(cell.x + 1, cell.y) + Level(cell.x - 1, cell.y) + Level(cell.x, cell.y + 1) + Level(cell.x, cell.y - 1)));
    if (!primed) { charge = FrontRoomsPhosphor.Initial(mode, w); ls = l; FillMean(l); primed = true; }
    ls += (l - ls) * (1f - Mathf.Exp(-dt / FrontRoomsPhosphor.LsTau));
    PushMean(l, dt);                                    // 3 s box, 10 Hz
    charge = FrontRoomsPhosphor.Step(charge, w, dt);
    var read = calm && mode == ModuleLamp.Failing ? mean3s : ls;
    var g = mode == ModuleLamp.Stutter ? 0f : charge * FrontRoomsPhosphor.Gate(read);
    shown = Mathf.MoveTowards(shown, g, (calm ? .5f : 2f) * dt);
    var e = TintLinear * (PeakEmission * shown);        // TintLinear = (0.5705, 0.8900, 0.2957)
    if (Mathf.Abs(e.x - last.x) + Mathf.Abs(e.y - last.y) + Mathf.Abs(e.z - last.z) < 1f / 1024f) return;
    instance.SetVector(EmissionColorId, new Vector4(e.x, e.y, e.z, 1f));
    last = e;
}
float Level(GridCoord c) { var v = map.LampLevel(c); return v < 0f ? 0f : v; }
```

**Cost:** 5 dictionary lookups and about 30 flops per frame, one object. Below measurement on both platforms.

---

## 5. Placement contract

### 5.1 What the start door really is (code facts)

**The door is the stream's, not the map's.** It is the terminal stream room's end-wall door. The map builds nothing on the start area's north side: "the stream room's end wall and door stand there" (`StartAreaEdge`, FrontRoomsMapWorld.cs:1067-1083).

**The opening and leaves** (FrontRoomsRoomStream.cs):
- a **2.4 m** opening (:19) with **two 1.12 m leaves** hinged at x = ±1.12 from the door centreline (:23-24);
- no casing ("an ordinary, flush double door", :1244);
- the leaves swing **into the map side (+Z) by 88°** (:114-116, :1780). The open leaves stand at |x| ≈ 1.08–1.16 and reach 1.12 m out from the wall;
- the header is at 2.66–2.90 m (:32-33).

**The map-side face** is the "far side" reveal plane, 0.022 m north of the door line (doorZ). It spans |x| 1.2–5.75 (:1230-1231, :1241-1243). The facade extensions carry it out to ±7.57 m (:593-603; FrontRooms3DGame.cs:597).

**The row the door opens onto.** The door opens onto cell **D = `startDoorCell`** (FrontRooms3DGame.cs:590). `MapRootFor` guarantees that the 5 cells of D's row are **Standard-height Level 0** (FrontRooms3DGame.cs:691-704), so the edges between them are only Open, Arch or Wall. Door and Window edges need a height change (FrontRoomsMap.cs:364-368).

**Map walls meeting the facade.**
- A wall on D's side lines (x = ±1.5 from the centreline) **stops on the door line**, where the facade closes the corner (FrontRoomsMapWorld.cs:979-989).
- So when D and its side neighbour are walled apart, a wall 0.16 m thick (faces at |x| 1.42 and 1.58) meets the facade.
- An arch on that line leaves a pier of 0.2–1.7 m at the facade end (ModuleUnits.cs:50).

**The keep-clear that exists today:**
- `Dress` keeps D's doorway clear: D's 3 m width, `DoorClearDepth + WallHalf` = 1.28 m deep (FrontRoomsMapWorld.cs:1757-1765).
- Nothing keeps the neighbouring cells' stretch of facade clear.

### 5.2 The rule (deterministic from the seed)

**Terms:**
- **s** = the side: **west** (−X) first, because it is the reader's right as they face the door from the map, the convention for double doors (S15). East second.
- **Dc** = D's centre (x = the door centreline).
- **E(s)** = `map.Cache.Edge(D, D + s)` (FrontRoomsMap.cs:796, public).

| Rank | Mount | When | Placard origin (world) | Faces | Glow cell | Keep-clear rect (world XZ) |
|---|---|---|---|---|---|---|
| 1 | **P1, facade** | E(west) = Open, else E(east) = Open | x = Dc.x + s·**1.7334** (inner frame edge on the cell line, 1.500); y = **1.524**; z = doorZ + 0.0225 (0.5 mm proud of the reveal) | +Z (into the map) | D + s | x ∈ [Dc.x + s·1.35, Dc.x + s·2.117], z ∈ [doorZ, doorZ + 0.90] |
| 2 | **P2, side wall** | no Open side; E(west) = Wall, else E(east) = Wall | x = Dc.x + s·**1.4195** (0.5 mm proud of the 1.42 face); y = 1.524; z = doorZ + **1.500** (mid-cell) | −s·X (into D) | D | x ∈ [Dc.x + s·0.52, Dc.x + s·1.42], z ∈ [doorZ + 1.117, doorZ + 1.883] |
| 3 | **P3, past the pier** | both sides Arch | x = Dc.x − **1.9134** (west; 0.10 m past the pier's far face at 1.58); y = 1.524; z = doorZ + 0.0225 | +Z | D − x | x ∈ [Dc.x − 2.297, Dc.x − 1.53], z ∈ [doorZ, doorZ + 0.90] |

(Each rect is written with its two bounds in either order; the code takes min and max.)

**Clearances:**
- **P1:** 0.300 m from the jamb, 0.34 m from the open leaf's outer face. Never in a swing: the leaves sweep only |x| ≤ 1.12.
- **P2:** the frame spans z 1.267–1.733 from the door line. The open leaf's tip is at about 1.14, so the placard is never behind the leaf, and it is 0.26 m clear of the leaf in x.
- **All:** top 1.681 m, bottom 1.367 m. That is clear of the leaf handles (1.13–1.31 m, :1260-1263) and under the 2.66 m header.
- **P3:** the pier may hide the placard from inside D when the arch gap starts far from the facade.
  - Refinement for later: use `OpeningSpan` (the hook proposed in LD §11.1 Step 3) and mount on the pier face toward D when the pier is ≥ 0.80 m.

**How often each mount comes up** (ESTIMATE). From the generator's odds (FrontRoomsMap.cs:89-96, 360-373):
- about 56 % of edges are maze-tree edges (Open 70 % / Arch 30 %);
- the rest are Wall 82 % / Arch 10 % / Open 8 %;
- rooms open about 21 % of edges.

So one side edge is Open about 55 %, Arch about 17 % and Wall about 28 % of the time. Treating the two sides as independent gives **P1 about 80 %, P2 about 17 %, P3 about 3 %**. The map chat's 100-seed run measures it (§5.5).

### 5.3 Who does what

**Visual chat (no map file touched):**
- `FrontRoomsPlacard` / `FrontRoomsPlacardMount` / `FrontRoomsPlacardGlow` / `FrontRoomsPhosphor` in `Assets/Scripts/Rendering/`;
- the `Prepare` call in RoomStream's `EndStreamAt`;
- the resolver above, which reads only public API: `CellOf`, `Cache.Edge`, `IsBuilt`, `LampLevel`, `LampModeOf` and `ModuleUnits`.

**The mount lives under the terminal room.** That is right for P1 and P3 (the facade is the room's) and works for P2 too.
- The P2 wall belongs to D's chunk. If that chunk drops while the room still stands, the placard hangs unseen until `StopTitleCorridor` removes the room.
- That gap can only open far from the player: the room is removed once every chunk round the start area is gone (`StartAreaBuilt`, FrontRoomsMapWorld.cs:632-649).

**Map chat (关卡设计):**
- one CONTRACT method, `KeepClearAtStart`;
- the confirmations in §5.5.

**Option B, if 关卡设计 would rather own the rule** (narrative A.12 gives it placement): move the resolver into the map as `public PlacardSpot StartPlacardSpot()` with the same table. The visual spawner then calls it and nothing else changes.

### 5.4 CONTRACT REQUEST Q16-1 (exact; `FrontRoomsMapWorld.cs`, map-owned)

```csharp
// Next to `startDoorCell` (:388):
// World-XZ rects the start area keeps free of furnishing (visual Q16, research/placard/10_spec.md §5.4).
readonly List<Rect> startKeepClear = new List<Rect>();

/// <summary>
/// Keep furnishing (office kits, piles and module props) out of a world-XZ rect
/// while the start area stands: the evacuation placard's wall and the floor in
/// front of it. Cleared by SetStartArea and ClearStartArea. MapRunStarted is
/// early enough: rooms are dressed one a frame from Update.
/// </summary>
public void KeepClearAtStart(Rect worldXZ) => startKeepClear.Add(worldXZ);

// SetStartArea (:608): add `startKeepClear.Clear();` as its first line.
// ClearStartArea (:620): becomes `{ hasStartArea = false; startKeepClear.Clear(); }`.

// Dress (:1757), inside `if (hasStartArea) { ... }`, after the doorway block (:1765):
foreach (var w in startKeepClear)
{
    var a = chunk.root.transform.InverseTransformPoint(new Vector3(w.xMin, 0f, w.yMin));
    var b = chunk.root.transform.InverseTransformPoint(new Vector3(w.xMax, 0f, w.yMax));
    var local = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.z, b.z), Mathf.Max(a.x, b.x), Mathf.Max(a.z, b.z));
    if (local.Overlaps(new Rect(room.x * cs, room.y * cs, room.w * cs, room.h * cs))) clear.Add(local);
}
```

- **The frame.** `clear` is chunk-local metres: the doorway rect uses `(cell − o) * cs` (:1763), and the chunk root sits at that origin (:928). So `InverseTransformPoint` on the chunk root is exact.
- **With no rects registered,** dressing is bit-identical. The chunk determinism check in `FrontRoomsMapValidator.cs` is unaffected.
- **Size:** about 12 lines. No generator change and no seed-set change.

### 5.5 Tests the map chat runs after the patch

1. **100 seeds.** Using the same verification seeds, tally E(west) and E(east) for D, and the mount the §5.2 rule picks. Report P1/P2/P3 shares against the estimate (80 / 17 / 3 %).
2. **Furniture.** On those seeds, dress the rooms round the start door with the rects registered. Assert that no module-prop or kit footprint overlaps a placard rect, and that nothing the map builds lies within 0.02 m of the placard's volume:
   - P1/P3: the frame box 0.4668 × 0.3138 × 0.0135 at the origin;
   - P2: the same box rotated.
3. **Lifetime.** `ClearStartArea` empties the list. A run without a placard dresses exactly as today.
4. **Confirm in writing:**
   - outlets and module wall props never use the facade face or the D-side face of a D side wall between 1.2 and 1.9 m above the floor;
   - the outlet planner already skips start-area walls (outlets spec D11). The P2 face is an ordinary map face, but a receptacle at 0.31 m or a switch topping out at 1.28 m clears the placard's 1.367 m bottom.

### 5.6 Decision for Red (optional; default = no)

**Pin the placard cell's lamp to Dim**, so the legend always reads faintly, lit and dark (G ≈ 0.25 C; §4.3). The visual spawner would call the public `map.SetLampMode(cell, ModuleLamp.Dim)`. The call is persistent and does nothing in the start area (MAP_GENERATION "Lamp overrides").

| | Default (the map's own roll) | Pinned Dim |
|---|---|---|
| For | The honest reading: the legend glows only when that lamp dies or sags. It is a find, not a tutorial | It pre-teaches the glow at the start of every run |
| Against | | It changes a gameplay lamp, so it needs Red's and the map chat's consent, and it teaches glyphs that Q2 / §5b may still replace |

---

## 6. WebGL path (a separate track; desktop never changes)

| Item | Desktop (reference) | WebGL only (gated) |
|---|---|---|
| Lens | `Kit_EvacPlacardLens`, transparent, 1 draw | **Not spawned**: `#if (UNITY_WEBGL && !UNITY_EDITOR) \|\| FRONTROOMS_WEBGL_PREVIEW` in `FrontRoomsPlacardMount` |
| Draws | 2 opaque (aluminium + sheet) + 1 transparent | 2. The live build is draw-call bound (about 2,300 a frame, WebGL perf analysis) |
| LOD | 2 / 6 / 30 m at lodBias 2 (Ultra: 4 / 12 / 60 m) | the same table at lodBias 1 (WebGL quality: High) |
| Textures | `_A` BC7 and `_E` BC1 at 2592 × 1676 | **WebGL import tab only:** max size 1024 for both stems. Check in the WebGL import that the format stays compressed; if Unity falls back to RGBA32 on the non-multiple-of-4 downscale, ship a pre-scaled 1296 × 840 pair and swap it in from `FrontRoomsPlacardGlow` under the same `#if` |
| Glow | `FrontRoomsPlacardGlow`, every frame | Identical. `TickFixturesNear` keeps `f.level` fresh for every built fixture (LD §11.1 Step 2), so `LampLevel` reads the same |
| Shader | FrontRooms/Surface `_EMISSION` variant | the same variant (kept by the material asset) |

**Never:** a WebGL change in the desktop path, or a desktop downgrade for WebGL.

---

## 7. Build tasks and acceptance (for the build stage)

Everything happens in the private clone `scratchpad/proj_placard` (create it as the task says; copy the capture tools from `proj_audit/Assets/Editor`). Never open Unity on Frontrooms3D.

| Task | Owner | Output |
|---|---|---|
| **B1 Blender** | visual | `evac_placard_common.py`, `evac_placard.py`, `evac_placard_lens.py`. Written in the clone; added to the real `Tools/Blender/frontrooms_kit/assets/` only if the task allows. FBX, sidecars, previews; all §2.8 asserts pass |
| **B2 Unity materials** | visual | §3: the pack script, the RenderSetup rows, the importer rule, then *FrontRoomsss → Rendering → Set up* in the clone |
| **B3 Code** | visual | §4.9 and §5.3: `FrontRoomsPhosphor`, `FrontRoomsPlacardGlow`, `FrontRoomsPlacard` + `FrontRoomsPlacardMount`, the one-line `Prepare` call in `EndStreamAt`. In the clone, the contract patch §5.4 is applied **only to test**; it is promoted by the map chat |
| **B4 Lookdev** | visual | the captures below. A three-view for 平面视觉 (front 1:2; section A-A through a rail at 4:1 with the §2.2 numbers; side), rendered with the artwork on the sheet via `Tools/three_view` |

**Acceptance captures:**
- **Harness:** `FrontRoomsKitLookdev` / `FrontRoomsLookdevCapture`, and the in-engine seed framing of `interaction_audit/05_in_engine_evidence.md`.
- **Map roots:** on multiples of 192 m (captures use 4992).
- 1080p and 1440p, desktop Editor.

| # | Shot | Pass when |
|---|---|---|
| A1 | 0.3 m face-on, Steady cell | The bullnose highlight is smooth (no facets). Mitre and hinge seams read as fine lines. `EVACUATION PLAN` reads left to right. The printed rule sits 3–4 mm inside the lip. The lens sheen is barely there |
| A2 | 0.3 m at 60°, toward a lit troffer | The lens shows a broad soft smear, not a mirror image. The paper is not veiled |
| A3 | 1.5 m face-on, lit | The legend is faintly visible as pale #D6DEBD, with no glow |
| A4 | 1.5 m, placard lamp `SetLampMode(Off)`, 2 lit neighbours, after 10 s | The legend glows pale yellow-green (not screen green). Glyph/paper ≥ 1.3. Glow luminance ≤ 12 % of the A3 sheet |
| A5 | A Failing cell, 10 s clip, normal then Reduce flashing | Normal: it breathes ≤ 0.5 Hz. Reduce flashing: a steady faint read with no pulse |
| A6 | A Stutter cell, 10 s clip | Never glows |
| A7 | A Warn burst train in the cell (`SetLampOverride(Warn)` × 6 at ≤ 3 Hz) | G rises and falls once (slew), with no per-dip flicker |
| A8 | Walk-out from 0.5 m to 8 m (LOD0 → 1 → 2), frame-by-frame | No pop in the glow (shared instance). The silhouette holds at the switches |
| A9 | One seed each for P1, P2 and P3, from the map side, door open and shut | Placed as in §5.2. Nothing overlaps it. P2 is visible past the open leaf |
| A10 | WebGL build | 2 draws for the placard, no lens, 1024 textures, the glow works |

---

## 8. Every change, listed for promotion

| File | Change | Owner / approval |
|---|---|---|
| `Tools/Blender/frontrooms_kit/assets/evac_placard_common.py`, `evac_placard.py`, `evac_placard_lens.py` | new | visual |
| `Assets/Resources/Props/Models/Kit_EvacPlacard.fbx/.json`, `Kit_EvacPlacardLens.fbx/.json` (+ `.meta`) | new (generated) | visual |
| `Tools/lookdev/pack_evac_plan.py` | new | visual |
| `Assets/Resources/Surfaces/Textures/Prop_EvacPlan_A.png`, `_E.png` (+ `.meta`) | new (generated) | visual |
| `Assets/Resources/Surfaces/Prop_EvacPlan.mat`, `Prop_LensNonGlare.mat` (+ `.meta`) | new (generated) | visual |
| `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs` | 1 SurfaceDef row, 1 GlassDefs row, 1 shadow-caster line, 1 importer rule | visual, **NEEDS APPROVAL** |
| `Assets/Scripts/Rendering/FrontRoomsPhosphor.cs`, `FrontRoomsPlacardGlow.cs`, `FrontRoomsPlacard.cs` (+ the mount) | new | visual |
| `Assets/Scripts/FrontRoomsRoomStream.cs` | one `FrontRoomsPlacard.Prepare(...)` call in `EndStreamAt` | visual (RoomStream is the visual chat's) |
| `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs` | §5.4 `KeepClearAtStart` | **CONTRACT Q16-1**, map chat |
| `Documentation/VISUAL_CHAT_TASKS.md` row Q16 | point at this spec | visual chat (not this workflow) |

Not changed: `kitlib.py`, `build_asset.py`, `FrontRoomsKitImporter.cs`, `FrontRoomsSurface.shader`, `FrontRooms3DGame.cs`, any map generator code, the artwork.

---

## 9. Open items and UNVERIFIED

1. **The 1990 lens** material and thickness (PVC, acetate or acrylic). Modern lenses are 0.4 mm PVC/PET (S11, snippet). We model 1.0 mm.
2. **Whether snap frames were common** on office walls by 1990. They existed (patents read); no period photo was found. `media_candidates.md` lists the gap.
3. **UFAS 1984 §4.30.6 wording.** Only the ADAAG 1991 wording was seen, and only as a search summary (S15).
4. **Levine 1984:** bibliography only; its numbers are not quoted (S16).
5. **`PeakEmission` 0.13** and the luminance estimate behind it (§4.4) are desk numbers. Lookdev sets the final value (A3/A4).
6. **The mount frequencies** (80 / 17 / 3 %) are estimates. The map chat's test 1 measures them.
7. **The WebGL texture format** after the 1024 downscale (§6).
8. **The decisions waiting elsewhere** that can change the placard's look but not the prop:
   - F1 (the footer string);
   - Q2 / §5b (the legend language; glow A or B);
   - §5.6 (pin Dim).

## 10. Sources

Full ledger with URLs, read status and licences: `SOURCES.md` (S1–S20). Media wanted for the research frame: `media_candidates.md` (M1–M10, none downloaded).
