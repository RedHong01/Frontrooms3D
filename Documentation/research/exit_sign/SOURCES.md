# EXIT sign: sources ledger

One row per source used in this folder: what it is, the URL, when it was read, the licence or status, and how it was read.
- **read**: the page or document was opened and read.
- **[search]**: only a search-engine summary was read.
- **on disk**: media already on disk. Nothing new was downloaded for it.
- **via**: the fact comes from a secondary source that quotes or summarises the primary one. The primary was not read.

No media was downloaded for this folder. Wanted media is listed in `media_candidates.md`.

Two side effects to report:
- The two NIST reports below were read as plain text from `scratchpad/exitsign_research/nistir4399.txt` and `nistir4532.txt`. The interrupted first attempt of this workflow made those files at 17:32.
- At 22:1x a WebFetch call on each report URL (to confirm the address) cached the PDFs in the session's `tool-results/` folder (5.2 MB and 3.0 MB). They are public-domain US government documents, not media. They were not copied into the project.

---

## 1. Primary period documents (1985–1991)

| ID | Source | URL | Read | Licence / status | Used for |
|---|---|---|---|---|---|
| S1 | B. L. Collins, M. S. Dahir, D. Madrzykowski, *Evaluation of Exit Signs in Clear and Smoke Conditions*, NISTIR 4399, NIST, **August 1990** (research Aug 1989 – Jun 1990) | https://nvlpubs.nist.gov/nistpubs/Legacy/IR/nistir4399.pdf | read (text), 2026-10-03 22:0x | US government work, public domain in the US | sign types of 1989–90 (incandescent, fluorescent, EL, tritium); stencil face vs panel face; Table 1 per-letter luminance and chromaticity; the code text of the time (S3–S6 below) |
| S2 | B. L. Collins, P. J. Goodin, *Visibility of Exit Directional Indicators*, NISTIR 4532, NIST, **March 1991** | https://nvlpubs.nist.gov/nistpubs/Legacy/IR/nistir4532.pdf | read (text, abstract and §1–2), 22:0x | public domain (US) | the chevron: 2.25 in proposed for the next NFPA 101; width/height 0.29–0.43; arrows outside the legend |

## 2. Codes and standards of the period (via S1)

| ID | Source | How read | Facts used |
|---|---|---|---|
| S3 | NFPA 101 *Life Safety Code*, 1988 edition, §5-8 and §5-10 | via S1 pp. 16–17 | 1 fc (10.76 lx) at the floor of the egress path; signs visible from any direction of exit access; no point more than 100 ft (30.5 m) from a visible sign; letters ≥ 6 in (152 mm), strokes ≥ 3/4 in (19.1 mm); "every sign … illuminated by a reliable source"; internally lit signs must equal an externally lit sign at 5 fc with contrast ≥ 0.50 |
| S4 | UL 924 (1989), *Emergency Lighting and Power Equipment* | via S1 pp. 17–19 | letter width ≥ 2 in except "I"; spacing ≥ 3/8 in (9.5 mm); arrow ≥ 3/8 in from any letter, outside the legend, and not easy to reverse; unused openings covered; legend visible when not lit; contrast ≥ 0.5 lit and unlit; stencil (translucent letters, opaque background): max ≤ 300 fL (1,028 cd/m²), max/min ≤ 40 |
| S5 | IES *Lighting Handbook*, 1987 (IESNA) | via S1 pp. 21–22 | 6 in height, 3/4 in stroke, 2 in letter width, 3/8 in spacing; bottom of sign 2.0–2.3 m above the floor; signs ≤ 30 m apart; sign power must be independent of the main supply |
| S6 | US Army Corps of Engineers, Standard Drawings 40-06-04 (1985, 1986) | via S1 p. 22 | Type 602: aluminium stencil face, red diffuser, two 20 W incandescent lamps for normal use and two 20 W DC lamps for emergency. Type 604 (self-contained battery): red letters, white diffuser, two 20 W incandescent lamps plus two 5 W DC lamps. Type 605: two 20 W incandescent or two 8 W fluorescent, aluminium stencil face, red letters. Type 606: edge-lit, clear acrylic |
| S7 | Cohn (1978), cited in S1 p. 7 | via S1 | strokes on most signs were 0.5–0.75 in with 6 in letters: 1:12 to 1:8 |
| S8 | Rea, Clark, Ouellette (1985), NRC Canada, cited in S1 pp. 1–3 | via S1 | incandescent and fluorescent signs measured 14–1,277 cd/m²; a two-lamp 25 W incandescent red sign was among the best; protan observers see red signs about 20 % dimmer; stencil faces suggested over panel faces |
| S9 | Schooley and Reagan (1980), cited in S1 p. 6 | via S1 | a panel sign with two 25 W incandescent lamps: 166 cd/m² normal; legible at 150 ft in clear air |

## 3. Secondary references (read earlier by the room-visuals research, reused)

| ID | Source | URL | Status |
|---|---|---|---|
| S10 | Wikipedia, "Exit sign" | https://en.wikipedia.org/wiki/Exit_sign | read 2026-10-03 by `room_visuals/03_run_research.md`: incandescent signs common; red usual in the US (required in some cities); tritium signs since the 1970s |
| S11 | MeyerFire, NFPA 101 §7.10 summary (current edition) | https://meyerfire.com/university/how-occupants-know-where-exits-are-located | [search] via `room_visuals/SOURCES.md` |
| S12 | 1stdibs listing "Vintage lighted EXIT sign" (1950s, 7.75 × 18 × 5 in) | https://www.1stdibs.com/furniture/folk-art/signs/vintage-lighted-exit-sign/id-f_921033 | [search] 22:1x; commercial listing, facts only. A second-hand (pre-1960) sign is larger and deeper than ours |

## 4. Media on disk (credited from their own ledgers)

| File | What it shows | Source and licence |
|---|---|---|
| `Research/week02/phosphor-narrative/stills/gn_tritium_exit_sign.jpg` | A real self-luminous US EXIT sign of about the 1970s: red face, condensed letters, about 1:6 strokes, widths 0.33–0.38 H, a moulded bezel | Wikimedia Commons, *Tritium-exit-sign.jpg* (Gazebo), CC BY-SA 4.0. Ledger: `Research/week02/phosphor-narrative/SOURCES.md` row 18 |
| `Frontrooms3D/Documentation/research/room_visuals/images/run_ref_etb_levelbang_0094.jpg` | Canon: *Escape the Backrooms* Level !, a ceiling-hung "‹EXIT›" with both chevrons over a red troffer row (VHS stamp MAR. 07 1991) | ETB 1.0 launch trailer, https://www.youtube.com/watch?v=2ZLPWJ-vkBU, 94.6 s; on disk, research use. Ledger: `room_visuals/SOURCES.md` |
| `Frontrooms3D/Documentation/research/room_visuals/images/room_title-run_sign.jpg`, `room_title-run_wide.jpg` | Ours: today's code-built hanging sign in the Run room | our capture (`room_visuals/04_captures.md`) |
| `Research/week02/ip-research/stills/ir06_frontrooms_run_exit.jpg` | Ours: the Run room look-dev of 2026-10-01 | `Research/week02/ip-research/SOURCES.md` |
| `Frontrooms3D/Documentation/research/interactables/threeview/png/Kit_ExitSign_*.png`, `scratchpad/interact_previews/G3/G3_exit_sign_close.png` | Ours: the G3 `Kit_ExitSign` as built | `interactables/build_g3_*.md` |

## 5. Project files read (Red's working copy, read only, 2026-10-03 21:5x–22:1x)

- `Assets/Scripts/FrontRoomsRoomStream.cs`: `:16-18` (room 11.5 × 2.9 × 12 m), `:78` (`lobbyOnlyTitle`), `:152` (`LampState`), `:795-870` (lamp tick), `:1379-1393` (`ProfileLightRange`, `LampOdds`), `:1399` (`officeDarkMaterial = BlackedGlass`), `:1472-1493` (Run signs and red lights), `:1495-1498` (Exit room), `:1502-1517` (`RefreshRoomMaterials` repaints by object name), `:1620-1665` (`ConfigureLightProfile`, keys `sequence * 16 + lamp`), `:1843-1848` (`SignFace`).
- `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs`: `:311` (`Run_ExitSign` SurfaceDef, no `smooth`, so 1.0), `:443-505` (`EnsureSurfaces`: `_S` mask optional, white when missing), `:675-700` (texture import: `_A`/`_E` sRGB, clamp only for `Run_ExitSign*`).
- `Assets/Resources/Surfaces/Run_ExitSign.mat` (`_Smoothness: 1`, `_MaskMap` none, `_EmissionColor` 3.2).
- `Assets/Resources/Rendering/FrontRoomsSurface.shader:17, 35-37, 383-405, 468` (mask R = smoothness, G = cavity; emission = map × colour).
- `Assets/Resources/Rendering/FrontRoomsPost.asset` (bloom threshold 1.05, intensity 0.55; ACES).
- `Assets/Scripts/Office/FrontRoomsKitLibrary.cs` (Spawn, ApplyMaterials fallback to the imported material), `Assets/Editor/Rendering/FrontRoomsKitImporter.cs` (remap or RemoveRemap per sidecar slot; `lodDistances`).
- `Assets/Editor/FrontRoomsMap/FrontRoomsModuleEditing.cs:23-24, 105-120` (hung "Wall" pieces at y = min(2.1, ceiling − 0.05 − height)).
- `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs:1726-1765` (lamp modes, Reduce flashing), `:2860-2990` (`LampModeOf`, `LampLevel`, `LampBaseLevel`, `SetLampOverride`, `LampFx`, `ReducedFlashingEnvelopeSeconds`); `FrontRoomsRoomModuleData.cs:30-80` (`ModuleLamp`, `ModuleProp`, markers).
- `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTSystem.cs:680-760, 1028-1040` (only renderers named "Lens" re-read emission per frame).
- `Assets/Resources/Props/Models/Kit_ExitSign(.json|.fbx.meta)`, `Kit_ExitSign_Dead(.json|.fbx.meta)`; `Tools/Blender/frontrooms_kit/assets/interact_exit_sign.py`, `interact_key_common.py:211-330`, `kitlib.py:1-60, 132-175, 640-830`, `build_asset.py`.
- Docs: `research/interactables/10_spec.md` §2.4 and §9.3, `build_g3_*.md`; `research/glass/rt/11a_glossy_surfaces.md` §6 (M1, M2); `research/room_visuals/03_run_research.md`, `04_captures.md`, `10_run_directions.md`; `research/door_break/10_design.md` (EX-K row); `research/webgl/10_webgl_plan.md` §2; `RELAY_PURSUIT_REDESIGN.md` (photosafety rules); `office_and_film/22_era_lock.md`; `FONTS_PERIOD_1990.md`; `VERIFICATION_LOG.md`.
- git: `git -C Frontrooms3D log` and `diff 7320ed1 HEAD` for the files above.
