# FrontRooms look development: research and the URP upgrade

What the Backrooms actually look like, room by room, and how the 3D prototype
reproduces it. Research on 2026-10-01; implementation in URP 17.3.

## 1. Research

### Level 0 (Lobby, Shift, Exit)

- **The original photo.** Taken in 2002–03 in the back rooms of a former
  furniture store at 807 Oregon Street, Oshkosh, Wisconsin, during its
  conversion into a HobbyTown RC track. It shows wall-to-wall wallpaper, uniform
  beige carpet, a drop ceiling and rectangular fluorescent fixtures, with no
  doors, windows or furniture.
  ([Wikipedia](https://en.wikipedia.org/wiki/The_Backrooms))
- **The paper was never yellow.** The real wallpaper is a beige 1990s
  southwestern ikat chevron with pink and slate stripe bands. The "mono-yellow"
  is a white-balance error in a 2002 digital snapshot (a Sony Cyber-shot, per Wikipedia). Danny Vermette, the film's
  production designer: "It's just a bad photo. The white balance is off."
  ([Galerie](https://galeriemagazine.com/how-horror-hit-backrooms-terrorizes-viewers-with-design/),
  [The Credits](https://www.motionpictures.org/2026/06/how-production-designer-danny-vermette-made-backrooms-real-portals-platforms-practical-terror/))
- **The film (A24, Kane Parsons, 2026).**
  - Set: 30,000+ sq ft of printed yellow wallpaper (up to 50 camera tests to
    avoid striation on camera) and 27,000 sq ft of carpet, picked from 200
    samples, in two tones that could be painted and aged. Parsons
    "overemphasized" the drop ceiling and wall-to-wall carpet.
  - Lighting: cinematographer Jeremy Cox placed no film lights. He relied on the
    practicals ("we didn't place a single light"), using short Astera Titan
    tubes, and printed the paper slightly more yellow so skin tones survive.
  - Image: wide lenses (18 mm or wider; "a 14mm looks normal"), a haze that
    reads like a CRT, a green-yellow cast (our reading, not a Cox quote), and deliberately not
    "cinematic" or "glossy".
  ([W Magazine](https://www.wmagazine.com/culture/backrooms-a24-movie-set-design-details-interview),
  [ShotDeck](https://community.shotdeck.com/articles/an-interview-with-backrooms-cinematographer-jeremy-cox/),
  [Sony Cinematography](https://sony-cinematography.com/dp-jeremy-cox-and-venice-2-ground-the-extradimensional-reality-of-backrooms/))
- **Ceiling.** 2'×4' mineral-fibre acoustic tiles (fissured, pinholed) in a
  15/16" T-bar grid, with 2'×4' troffers and prismatic (K12) acrylic lenses.
  The Async series shows troffers, tiles and ballasts as the anatomy of the
  space. ([Backrooms web series](https://en.wikipedia.org/wiki/Backrooms_(web_series)))
- **Wiki canon.** Level 0 is "mono-yellow" paper, "old moist carpet",
  scattered electrical outlets, and inconsistently placed fluorescent lights at
  maximum hum-buzz.

### Level 4 (Office)

Canonically an empty office building, "almost completely devoid of
furniture". Most windows are "completely blacked out"; water coolers, vending
machines and fountains are scattered around
([Backrooms Wiki](https://backrooms-wiki.wikidot.com/level-4)). The period
dressing:

- painted drywall in greige with a knockdown texture;
- quarter-turned 24" carpet tiles in blue-grey with flecks;
- 2'×2' ceiling tiles with flat frosted (opal) troffer lenses — the film's fixtures and Red's target both show opal, not louvres;
- fabric cubicle panels.

### Level ! (Run For Your Life)

A long, hospital-like corridor with white walls, floors and ceilings, in dim
red light that comes from the exit signs hanging from the ceiling. Chairs and
hospital beds block the way while the chase goes on. The previous build's
utility room (pipes, junction boxes) was not the canonical look and is
replaced.

## 2. What the build now does

### Pipeline

- **Renderer:** URP 17.3 with a Forward+ renderer, 4× MSAA, HDR, and 2-cascade
  main shadows.
- **Point-light shadows:** soft shadows on every practical, with a 4096 point
  shadow atlas.
- **SSAO:** depth-normals source, 0.45 m radius, for contact darkness in
  corners, under furniture and at door gaps.
- **Setup:** one menu item, **FrontRoomsss → Rendering → Set up URP, post and
  surfaces** (or `-executeMethod FrontRoomsRenderSetup.RunBatch`). It creates
  `Assets/Settings/FrontRooms_URP*.asset`, the post profile and every surface
  material, and moves inline Standard materials to URP Lit. It is safe to
  re-run.

### Film look (`Resources/Rendering/FrontRoomsPost.asset`)

ACES tonemapping and a soft warm bloom (halation on the tubes). White balance
is +9 temperature and −7 tint toward green: FrontRooms' own stylistic choice (the film itself balanced 4000 K tubes at 4300 K; see research/office_and_film/01). The
remaining settings in the profile are:

| Setting | Value |
|---|---|
| Contrast | −6 |
| Saturation | −8 |
| Lift | slightly warm, lifted blacks (the CRT haze) |
| Film grain | Medium3, 0.22 |
| Vignette | 0.26 |
| Chromatic aberration | 0.06 |
| Lens distortion (wide-lens barrel) | −0.04 |

All of these are editable in the Inspector.

### Surfaces (`Resources/Surfaces/*.mat`, shader `FrontRooms/Surface`)

- **World-projected UVs.** Walls run along the wall with V up from the floor;
  floors and ceilings use world X/Z. The paper, carpet and ceiling grid are
  continuous across every slab at true print scale.
- **Tile sizes divide the 256 m rebase,** so a floating-origin shift never
  moves a pattern:
  - wallpaper: 256/373 m (one 27" roll);
  - carpet: 1 m;
  - ceiling, carpet tiles and VCT: 256/210 m (4 ft);
  - large-scale wear: 8 m and 12.8 m.
- **Large-scale wear in world space hides the tile repeat:** discolouration,
  dirt, tide-mark water stains on the ceiling, damp patches in the carpet
  (darker, flatter, glossier), grime along the floor, and water streaks from
  the ceiling grid.
- **Textures** come from `Tools/lookdev/gen_surfaces.py` (with `gen_common.py`; source image `Tools/lookdev/ref/Backrooms_Chevron_CC0.png`). They are seamless,
  at 2048 px for the main surfaces and 1024 px for the small ones, each with
  albedo, normal and mask (smoothness, cavity) maps:

| Room | Walls | Floor | Ceiling | Fixtures |
|---|---|---|---|---|
| Level 0 | CC0 recreation of the original chevron paper ([Wikimedia, CC0](https://commons.wikimedia.org/wiki/File:Backrooms_%27Chevron%27_Wallpaper.png)), graded to mono-yellow, with paper fibre, mottling, foxing and roll seams | sand loop-pile carpet | 2'×4' fissured tile with T-bar | prismatic K12 lens |
| Exit | the same paper from a cold print run | cold-tinted carpet | 2'×4' tile | prismatic lens |
| Office | greige knockdown drywall | quarter-turned 24" carpet tiles | 2'×2' tile | flat opal lens + Office zone grade |
| Run | white semi-gloss hospital paint | 12" VCT with chip pattern, heel scuffs and wax wear | white 2'×2' tile | red emissive EXIT signs |
| Shared | oak veneer doors, enamel steel (troffer pans), vinyl cove base | | | |

### Light

- **Independent fixtures.** Every fixture is its own ballast. After a door opens,
  each lamp strikes after its own delay (0.4–2.2 s), flickers its own count,
  rises at its own rate, and can be failing (sways and drops out forever) or
  dead. No lamp waits for the room.
- **Odds per profile:**

  | Profile | Dead | Failing |
  |---|---|---|
  | Lobby | 2% | 10% |
  | Shift | 10% | 32% |
  | Office | 3% | 6% |
  | Run | 55% | 40% |
  | Exit | 0% | 5% |

- **Grid-aligned troffers.** 2'×4' troffers snap to the world ceiling grid when
  a room is placed or recycled, so they always sit inside the printed T-bar
  cells.
- **Tube colour.** The tubes stay near cool-white; the yellow comes from the
  paper and the grade.
- **Run's red light.** Level ! is lit red by two hanging battery exit signs per
  room; its ballasts are mostly dead.

## 3. Wallpaper print layer (`_FR_PRINT`): binding contract

Landed on 2026-10-03 (wallpaper P0, approved by Red with emboss weight 0.60). The
Level 0 and Exit walls in §2 are now a "sandwich": a static paper that alone
drives the lighting, and a print that only ever reaches the albedo. The visual
chat owns the shader, `FrontRoomsRenderSetup` and `Tools/lookdev`. The wallpaper
chat owns the print content, `Tools/print` and the clock driver. Change a line
below only by agreement between the two.

- **Materials.** `L0_Wallpaper`, `L0_Wallpaper_Shift` and `Exit_Wallpaper` use the
  paper `Wallpaper_Paper_M/_N/_S` and the print `Wallpaper_Print_P`, with
  `_FR_PRINT` on. `FrontRoomsRenderSetup` writes the keyword, `_UsePrint`,
  `_PrintTex`, the palette and `_PrintAmount` on every material. Turn the print
  on and off at runtime through `_FR_PrintClock.w`, never through the keyword.
- **Paper.** `_BaseMap` is `_M` (linear): `(α, β, γ) = texel·(0.70, 0.14, 0.02) +
  (0.46, −0.001, −0.004)` and albedo = `print·α + tobacco·β + γ`, with tobacco
  #7A5B2A. `_N` and `_S` carry no ink: weave, fibre, seam and a linen + pebble
  emboss (`EMBOSS_LINEN` 0.25, `EMBOSS_COARSE` 0.60). The print never writes the
  normal, smoothness or cavity.
- **Print encoding.** R = density and G = cream. Both are parameters of the
  shader's LINEAR-light ramp (0 ground, 0.5 mid, 1 deep, then
  `lerp(ramp, cream, 0.55·G)`), not raw ink. Art made with the sRGB-value
  duotone goes through `print_encode` (`Tools/lookdev/gen_surfaces.py`, table
  `Tools/lookdev/print_encode_lut.json`). Encode at the frame's own resolution,
  then resample. B is reserved for the phosphor ink (0 in `_PrintTex`); A is
  unused. One tile is one 0.75 × 1.125 m roll.
- **Globals** (never in Properties):
  - `_FR_Print`: a Texture2DArray, one roll tile per slice, in the same encoding;
  - `_FR_PrintClock`: x = frame [0, n), y = n, z = per-roll phase in frames,
    w = live mix (0 or unset = static frame 0).
  - The roll strip is `floor(rollUV.x) & 3`.
  - The palette and the amount are in UnityPerMaterial (`_InkGround`, `_InkMid`,
    `_InkDeep`, `_InkCream`, `_PrintAmount`).
- **Print mips.** These are not box mips. On the ENCODED values:
  `c' = box(c)` and `d' = box((1 − 0.55c)·d) / box(1 − 0.55c)`.
  - `FrontRoomsPrintMips.Apply` runs on import for `*_P` under
    `Resources/Surfaces/Textures`.
  - Every live slice must be built with `FrontRoomsPrintMips.ApplySlice`. The
    mirrors are `gen_surfaces.print_mips` and the JSON's `"mips"` entry.
  - Slices are delivered encoded and without mips.
- **Sampler budget.** WebGL2 guarantees 16 fragment sampler units. The print
  variant binds 12 (10 base, plus `_FR_Print` and `_PrintTex`). Light cookies
  add 2 and reflection-probe blending adds 1, for 15. Any further print map must
  share an existing sampler or pack into `_FR_Print` B/A.
- **Import.** `_P` has a Standalone override to R8G8 (lossless, 16 MiB at
  2048×3072 with mips). The WebGL tab is untouched. 2048×3072 is NPOT, so BC7 is
  unavailable for every mipped wallpaper texture.
- **Legacy and tests.** `Wallpaper_Chevron(_Cold)_A/_N/_S` and the two NoHue
  references live in `Assets/Editor/Rendering/PrintP0/Ref/`: T1 only, never
  shipped. The gates are under FrontRoomsss → Rendering → Print P0 gates
  (`FrontRoomsPrintP0Test.RunBatch`).
