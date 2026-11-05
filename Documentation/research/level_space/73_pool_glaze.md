# Level space · 73 · Pool tile glaze and shimmer: three options (design, renders, choice)

**Status 2026-11-04 16:4x: RED PICKED GZ-C** ("选这个", in the level-space chat, with a screenshot of the sheet's GZ-C column; K not named, so K1 stands). Landing: GZ-A's parts (ours) in scene 4 first; B and C through 游戏视觉's tile package (halo weight 0.4 -> about 0.2, phones from baked cubes). Rendered 16:0x (§11 is the Chinese page for Red). Sheets in `sheets/glaze_options.jpg` (main: TODAY, FIELD = 游戏视觉's game tile as reference, GZ-A, GZ-B, GZ-C REC, on scene 4's own light with the water bounce I on), `sheets/glaze_light_key.jpg` (K1 REC / K2) and `sheets/glaze_options_zone.jpg` (FLAG: the same options under 游戏视觉's PoolLevel zone look, whose ambient washes the porcelain grey); films `video/glaze_options_film_dolly.mp4` and `_strafe.mp4` (2 x 2 grid TODAY, GZ-A / GZ-B, GZ-C, re-encoded at 2560 with the tone mark; full-size originals and per-column films in `~/FrontRoomsVisualWork/level_space/renders/glaze/glaze2/`). Rendered in the clone `proj_levelspace` (run log `~/FrontRoomsVisualWork/level_space/glaze/render_log.txt`, run book `RUN.txt`); shader compile 93 OK (Metal macOS, Metal iOS, GLES3x); nothing of the glaze landed in main. Failures written on their tiles: B_r3 white wire joints in the SP6 corridor; C_r5 halo hides the core at 0.35 m (halo weight 0.4 -> about 0.2 when it lands); r6 phones show no lamp images in any column (realtime probes off at Medium and Handheld); calm water shows faint bands. 游戏视觉 folds Red's pick into their tile stage (their message 14:0x).


Line 夸张结构调研与设计 (`level_space`), workflow `pools_glaze`, design step, 2026-11-04 (account B, session local_42b400e5's workflow).
Red, 2026-11-04, with a POOLS (Tensori) Steam screenshot (verbatim): "然后我需要你reference pool这里，墙面上釉面的纹理，那种波光粼粼的感觉".
Red, 10-31 17:19: "我希望底色不应该是偏暖的米白，而是和dream pool更相似的瓷白". 10-31 17:24 (a pick, no verbatim text on file): stairs porcelain, only a dark nosing.

What this step did: read the three reference notes in this folder (`ref_pools.md`, `ref_physics.md`, `ref_engine.md`; reused, not redone), today's renders (`W/level_space/renders/scene4/review1/SP1_s00, s02, s03, SP6_s00..s08`), our code in main (read only), the URP 17.6 / core RP source in `W/proj_levelspace/Library/PackageCache` (read only), and 游戏视觉's staged pool look (`W/pool_look/to_main/.../70_look_build.md` §4, §7; `71_look_for_red.md`; the staged shader and water scripts in `W/proj_unlock`, read only). No Unity ran, nothing landed, nothing was downloaded; no file outside this folder was changed (a numpy check wrote only to the session scratchpad, §1.4).

Files read in main, sha1 (first 8): `FrontRoomsSurface.shader` 8f3587e0, `FrontRoomsPoolMaterials.cs` ebfd675b, `Structures/FrontRoomsStructuresLook.cs` a09486d6, `…Kit.cs` 005de1a4, `…Mesh.cs` edf63e7a, `…PoolTerrain.cs` d332838e, `…Slide.cs` 96eac967, `…Water.cs` 69973b9f, `…Walker.cs` 11845da9, `Assets/Structures/Textures/LS_PoolTile150_N.png` ae076a0c (+ `.meta` c114374c), `Settings/FrontRooms_URP.asset` 18584f2b; generator `W/level_space/tile/make_pool_tile.py` 41f69eb4.

Option names on the sheet: **GZ-A, GZ-B, GZ-C** (GZ = glaze; "A" alone is already taken by other scene-4 questions).

---

## 0. The answer in six lines

1. The shimmer on POOLS' walls is the **lamps mirrored in the glaze**, broken tile by tile because every tile sits at its own small angle and is slightly domed. It is not light thrown by water (`ref_physics` §4, §6.4; `ref_pools` 5, 8).
2. Our walls show **no lamp at all** today, for three reasons found in the code: the lamp lenses are only 1.4-2.3 (2-5x a lit wall, physics wants 50-200x); our shader reads the hall probe **without box projection** (the image sits "at infinity"); the probes are captured once, with the walker's distance budget switching far lamps off.
3. **GZ-A** fixes the tile and makes the lamps visible to the glaze, in our own files only. **GZ-B** adds what holds the lamp images in the right place and stops the crawl (a pool-only shader keyword in 游戏视觉's shader, per-room probes in ours). **GZ-C** adds moving water light on the lower walls and pool floors and a two-lobe porcelain glaze.
4. **Recommendation: GZ-C**, built in the order A, B, C. The sheet renders first, in the clone, without waiting for Red; A depends on nobody and lands as soon as Red picks; B needs 游戏视觉's shader; C needs 游戏视觉's `pool_water` (in Package today) and the water line's bodies in scene 4.
5. 游戏视觉 already has a pool tile (`pool_tile`, `_FR_TILE`) and the whole water light (`pool_water`: floor nets, thrown light from niches) staged for main. Nothing here duplicates it: GZ-A's numbers go into their generator too, GZ-B's code goes inside their `_FR_TILE`, GZ-C reuses their water light and adds the one term they list as missing (light thrown up by the water onto the lower walls).
6. A precondition Red should know: **no visible bright lamp, no shimmer.** Under P1-A (indirect uplights, the zone-lamp recommendation for the game's pool hall) the walls have almost nothing to reflect; P1-C (sealed 8 ft fluorescent strips) is the POOLS look. That stays Red's P1 choice; it is not asked again here.

---

## 1. What the shimmer is, and what is missing

### 1.1 What it is (from the three refs)

| What | Number | Source |
|---|---|---|
| The glaze is optically a mirror | Ra 0.016-0.017 µm; smoothness 0.97-1.0 at the micro scale | `ref_physics` §3 |
| Face-on reflectance | F0 0.04 (glaze n 1.5); 9 % at 60°, 17 % at 70°, 39 % at 80°, 56 % at 84° | `ref_physics` §4.3 |
| Per-tile tilt of a real wall | σ 0.15-0.3° (lippage ≤ 1 mm + warpage) | `ref_physics` §2 |
| Per-tile bow | 0.15-0.6 mm on 150 mm (0.1-0.4 %); edge slope 0.25-0.9° | `ref_physics` §2, §6.1 |
| Glaze waviness | orange peel 3-10 mm, a few µm high; slope 0.1-0.3° | `ref_physics` §3 |
| Lamp in the glaze vs the white tile | 2-9x face-on, 13-45x at 75°, 30-100x at 84° | `ref_physics` §4.3 |
| What it takes in HDR | emitters 50-200x the lit wall, or the glint cannot read | `ref_physics` §6.2 |
| POOLS, measured on Red's frame | a lamp's reflection covers about 9 x 8 tiles as a checker; peaks 2-4x the wall (linear); glints do not clip; lamp cores clip and bloom | `ref_pools` 5 |
| POOLS' tiles | about our size (13-15 cm); grout light, 6-15 % darker than the face | `ref_pools` 4 |
| POOLS' tech | Unity; baked lighting + reflection probes, SSR on the water; no ray tracing | `ref_pools` 1-3 |
| Sparkle needs thin lamps | a 50 mm strip gives sparse glints at 15 m; a 600 mm panel gives a solid jagged patch | `ref_physics` §4.1 |
| By distance | 2 m: an almost unbroken line stepped at each joint, cushion hairlines; 5 m: one dash per tile; 15 m: 2-4 glints per tile in a 10-30 cm band | `ref_physics` §4.2 |

### 1.2 Today, from the renders and the code

Renders (read 11-04): SP1_s00, s02, s03; SP6_s00 … s08.

1. **No lamp in any wall** (SP1_s00, SP6_s00, s03, s06). The only wall highlights are the **square wire grid of cushion edges** around each wall box (SP6_s01, s02, s05, s07, SP1_s02): the box's point light caught by every cushion inside its lobe. It reads as a wireframe of light and will crawl in motion.
2. **Floors already do it right**: a lamp's highlight breaks into tile-sized pieces (SP6_s02, s05, s07; SP1_s03). The 0.5° per-tile tilt works where the source is a punctual light's highlight.
3. **The grid reads as dark wire** at 2-5 m (SP6_s01, s04, s06): grout 1.6 mm deep with a 0.7 mm step below the cushion foot, cavity 0.45 x occlusion 0.8, plus heavy SSAO in corners. POOLS' grout is 6-15 % darker than the face.
4. **The rooms read teal-grey, not porcelain white**: the structures pool preset (fog .11/.16/.17, trilight .30-.44, white balance −12) tints every wall. Not changed here (it is the palette's, and 游戏视觉's `pool_zone_look` replaces it in the game); named so Red does not read the grey as the glaze.
5. **No moving water light** in scene 4 at all (the greybox caustic quads exist only in the map's pool pass, `FrontRoomsMapWorld.Pool.cs:1006`).
6. **SP6_s08's floor** reads at about 4x the tile pitch: a mesh whose UVs are not in metres. Not a glaze question; passed to the scene-4 fix review.

Code (main, read only):
- Lens emission of every pool lamp: down-fill (1.4, 1.5, 1.55), F5 sealed lens (1.5, 1.6, 1.65), wall box (1.6, 1.7, 1.75), underwater (1.8, 2.2, 2.3) (`Kit.cs` 1142-1155, `PoolTerrain.cs` 127). A lit wall is about 0.3-0.6 linear (estimate from the lamps' intensities and ranges, not measured): the lenses are 2-5x the wall.
- `FrontRooms/Surface` declares neither `_REFLECTION_PROBE_BOX_PROJECTION` nor `_REFLECTION_PROBE_BLENDING`; URP 17.6 `GlobalIllumination.hlsl` lines 35-41 then define both as 0. The probes are made with `boxProjection = true`, but our tile never projects (verified in the package source).
- Forward+ with blending off binds **one probe per renderer** (`UniversalRenderPipeline.GetPerObjectLightFlags`, line 2377). `FrontRoomsStructuresMesh` merges a district's tile into one renderer per material and shell group (ceiling height, grime, shadows), and each such renderer spans several rooms, so **each of SP6's few tile renderers takes one of its five probes for all the rooms it spans**; the SP6 corridor has no probe of its own.
- Probes render once, two frames after the build (`Look.cs` Tick). By then the lamp budget (on within 16 m of the walker, 3 m fade) has switched far lamps off, so the probe sees dark walls far from the arrival point.
- The tile normal is 8-bit: one step is atan(2/255) = **0.45°**. Today's 0.2-0.5° tilts are 0 or 1 step per axis, so every tile is one flat plane in the data; a gentle bow alone would terrace into 2-3 straight bands per tile unless something varies the normal across each step (in v2 the peel does, §1.4).
- The 1.5 m sheet repeats its 100 tilts every 1.5 m: a long wall shows the same glint pattern six times down a 9 m corridor.
- No specular anti-aliasing anywhere in URP (`ref_engine` §1.3); 4x MSAA does nothing for it.

### 1.3 What is already in flight elsewhere (do not duplicate)

- **游戏视觉 `pool_look`** (journal `W/pool_look/journal.txt`; Package stage running 11:41 today; main's shader still 8f3587e0):
  - `pool_tile`: `Tools/lookdev/pool_tile/gen_pool_tile.py`, maps `PoolTile_<set>_{D,N,S}`, `_FR_TILE` in `FrontRooms/Surface` (per-tile set by a hash of the cell, ±0.18°, never repeating; lots; wet band; meniscus). Field tile: 3 mm joints, cushion r 1.8 mm, tilt σ 0.10°, lippage σ 0.10 mm, bow 0.05-0.20 mm, saddle σ 0.04 mm, peel 2.5 mm at 4.5 µm and swell 12 mm at 10 µm, smoothness 0.935. D8 (grout, course, deck) is waiting on Red in `71_look_for_red.md`.
  - `pool_water`: `FrontRoomsWaterSubmerged` (floor nets from ≤ 2 compact sources per body, `SetSources`; thrown light from wet niches, `RegisterNiche`, max 16 lamps, 12 bodies), caustic loops `FR_Caustic_Fine/Long` from the ripple surface, baked pool cubes `Refl_Pool*`. Their own list of what is missing: "reflected caustics from compact lamps above the water (a follow-up: the same lens on the reflected ray)" (70 §7.11).
  - `71_look_for_red.md` §3.5 keeps P1 (the pool hall's overhead lamps) open: P1-A indirect uplight (recommended there; "水面没有灯的倒影"), P1-B high-bay, P1-C sealed fluorescent strips.
- **水体仿真调研** (`handoff/water.md`): the water itself; stage 2 puts 游戏视觉's water bodies into scene 4 through `FrontRoomsStructuresWater.cs` once `pool_water` lands. The boundary sent on 11-04: the tile glaze and the shimmer on walls and ceilings are this line's; the caustic source is shared.

### 1.4 Scratch check of the v2 numbers (numpy, not the generator; nothing written outside the scratchpad)

Script and images: `/private/tmp/claude-501/-Users-redwang-Desktop-ArtCenter-Fall26T7-EGAM-401A-01-Individual-Game-Project/13854946-cf2a-4e06-8483-08cf570ac38c/scratchpad/glaze_design/preview.py` (height fields of today's and v2's sheet at 2048 px, normals, 8-bit encoding, and a mirror preview of one 0.43 x 0.40 m lens with a Gaussian lobe of 1.4 α).
- Face normals (inside the cushions): today mean 0.35°, p95 0.50°, max 1.07°, **2 normal codes per tile** in 8 bit (every tile one flat plane); v2 mean 0.47°, p95 0.92°, max 2.14°, **about 22 codes per tile**.
- 3 m, 5 texels per pixel, lens 4.2 m out: today at 0.85 = a soft round blob over about 3 x 3 tiles that the tiles hardly change (the "plastic" read); v2 at 0.87 = a smaller blob, each tile a little different; **v2 at 0.93 = a compact patch of about 2 x 2 tiles, each tile its own piece, edges cut at the joints**: the jagged patch the physics predicts for a 0.5 m lens. A sparse sparkle row would need thin lamps (§0 item 6).
- Hero, eye 0.35 m, lens 1.35 m out: at 0.93 a crisp lamp image with gently wavy edges, the two tiles under it offset from each other; 8-bit without dither keeps that with a few small edge steps; with TPDF dither the edge turns to grain. A 3 mm peel band read ragged ("hammered"), hence 5 mm.
- Limits: a Gaussian stands in for GGX and for the probe's mip blur; no mips, no compression, no lighting other than the lens.

---

## 2. The three options at a glance

| | GZ-A Glaze in the tile | GZ-B Lamps held in the glaze | **GZ-C Glaze and water light (REC)** |
|---|---|---|---|
| What Red sees | Walls near lamps carry glints broken tile by tile; floors show a dash per tile with wobbling edges; the joints stop reading as black wire. Walking, the wall glints slide with the camera instead of staying on the lamps, and far walls crawl a little. | The lamp images sit in the walls where the lamps are and move correctly as you walk: a row of ceiling lenses becomes a row of broken patches in each wall; down the SP6 corridor a streak of lamp images recedes, broken tile by tile; far walls twinkle without crawling; the sconce wire grid becomes thin hairlines. | All of B, plus slow bands of moving light on the lower walls next to the water and nets on the pool floors (strong when the water is disturbed), the niches' glow on ceilings at night, and at arm's length each lamp image has a sharp core inside a soft glow: porcelain, not plastic. |
| What changes | tile maps (bow, saddle, peel, swell, grout level), material numbers, reflection-only lamp proxies, probe capture | A + `_FR_GLAZE` (box projection for the pool tile only, specular AA, per-tile set), per-room probes and renderers, SP6 corridor probe | B + two-lobe environment glaze + 游戏视觉's water light in scene 4 + the reflected band on the lower walls |
| Whose files | ours only | ours + 游戏视觉's shader | ours + 游戏视觉's shader + `pool_water` + 水体仿真调研's bodies |
| GPU per pool-tile pixel | 0 | about +40 ALU, 0 fetches | B + 1 cube fetch + about 20 ALU; + ≤ 4 taps + about 60 ALU within 3 m of a water |
| Memory | 0 (same map sizes) | +16 MiB (one 512 HDR probe, desktop) | B + 游戏视觉's caustic loops (6 MiB desktop, 2.5 MiB phone, shared) |
| Honest flaw | reflections unanchored (sliding); crawl beyond about 8 m; glint pattern repeats every 1.5 m | box projection is exact only in box-shaped rooms; probes are static (the Relay is not reflected); needs 游戏视觉 to land shader code | depends on two other lines' landings; in calm water the bands are faint (physics); caustic light is not shadowed by pillars and rails |

Order note: the example order in the brief put water light before the reflection upgrade. This design swaps them, because the physics says the POOLS shimmer is the reflection (§1.1), the reflection fix costs almost nothing on every platform, and the water light is the costliest and most dependent layer.

---

## 3. GZ-A · Glaze in the tile (ours only)

### 3.1 Generator v2 (`W/level_space/tile/make_pool_tile.py`, also the base for GZ-B and GZ-C)

The same 2048 px = 1.5 m sheet, 10 x 10 tiles of 150 mm, 0.732 mm per texel, seamless, numpy + Pillow, `SS = 2`.

| Constant | Today | v2 | Why |
|---|---|---|---|
| `N`, `SHEET_MM`, `TILES` | 2048, 1500, 10 | same | the sheet divides 3 m (LMS 5 TileAlign) |
| `GROUT_MM` | 5.0 | 5.0 | joint width converges with 游戏视觉's 3 mm (⅛ in) only when the tiles merge (§7), not in this comparison |
| `CUSHION_MM`, `CUSHION_DROP_MM` | 2.2, 0.9 | 2.2, 0.9 | radius about 3.1 mm, normals 0-39°: the hairline glint on each tile |
| `GROUT_DEPTH_MM` (flat floor) | 1.6 | grout **struck concave**: 0.9 at the cushion foot, 1.3 at the joint centre (`GROUT_FOOT_MM = 0.9`, `GROUT_DIP_MM = 0.4`) | ANSI A108.10 5.3.4 (grout finished to the depth of the cushion); removes the 0.7 mm step that draws the dark wire |
| Tilt | 0.5° x U(0.4, 1), random direction | per axis Gaussian **σ 0.25°**, magnitude clipped at **0.6°** (`TILT_SD_DEG = 0.25`, `TILT_MAX_DEG = 0.6`) | real walls σ 0.15-0.3°; with the bow below the normals spread about ±1° across the field, the checker POOLS shows |
| Dish → **bow** | ±0.035 mm paraboloid | convex **U(0.15, 0.40) mm** on 80 % of tiles, concave **U(0.10, 0.25) mm** on 20 %; `h = s (1 − r² / 75²)` on the face (`BOW_CONVEX = (0.15, 0.40)`, `BOW_CONCAVE = (0.10, 0.25)`, `BOW_CONCAVE_SHARE = 0.2`) | ISO 13006 / ANSI A137.1 limits 0.3-0.5 %; edge slope 0.23-0.61°, corner 0.33-0.86°; turns the stepped line into a dash per tile |
| **Saddle** (new) | none | `h = w (x / 75)(y / 75)`, **w ~ N(0, 0.05 mm)** (`SADDLE_SD_MM = 0.05`) | slants the dashes, as warped tiles do |
| **Orange peel** (new) | none | band noise centred on **5.0 mm**, log-Gaussian width 0.45 (游戏视觉's `noise()`), **std 3.0 µm** (`PEEL_MM = 5.0`, `PEEL_UM = 3.0`) | lamp edges wobble gently at arm's length (15 px per wavelength at the 0.35 m hero); a 3 mm band read ragged in the check (§1.4) |
| **Swell** (new) | none | band noise on **14 mm**, **std 6 µm** (`SWELL_MM = 14`, `SWELL_UM = 6`) | the lamp image wanders inside a tile; peel + swell together **0.21° rms per axis** (measured on the field), inside the physics' ≤ 0.3° |
| Peel and swell fade | – | `x smoothstep(0, 1.8 mm, distance into the face)` | the cushion stays a clean rim |
| Normal encoding | rounded to 8 bit | rounded to 8 bit, **no dither** | the 8-bit step is 0.45°; the peel already spreads each tile over about 22 normal codes (today: 2), so the bow does not terrace into straight bands; a TPDF dither was tried and sprays grain along the lamp's edge at 0.93 (§1.4) |
| Tone, grout colour, cushion shade | ±1.2 %; linear (.36, .41, .39); 6 % | same (tones may re-roll: the RNG order changes) | grout colour is D8's (游戏视觉's T1 grout .66/.70/.67 gamma is about ours) |
| Mask R: face / cushion shoulder / grout | 1.0 / 0.90 / 0.30 | 1.0 / **0.93** / **0.28** | the glaze runs onto the rim, so its hairline glints |
| Mask G: joint / cushion | 0.45 / −0.15 | **0.62** / **−0.10** | the joint reads by its colour, not as a black wire |
| Seed | 1990 | 1990 | |

Repository: the N map grows from 89 KB to about 8-10 MB (noise does not compress). Import settings unchanged; in the `.meta`, pin **BC5** for Standalone and **ASTC 4x4** for iOS on N (whatever Unity's automatic normal format picks today, DXT5nm's green has 6-bit endpoints).

### 3.2 Material (runtime, structures only)

Set by the structures bootstrap on `FrontRoomsPoolMaterials.Tile` and `.Band` right after `SetTileSet` (our file; `FrontRoomsPoolMaterials.cs` and the map's pool keep today's values):

| Value | Today | GZ-A |
|---|---|---|
| `_Smoothness` (x mask R) | 0.85 | **0.87** (no AA yet, so only a small step) |
| `_BumpScale` | 1 | 1 |
| `_OcclusionStrength` | 0.8 | **0.7** |
| `_BaseColor` | (.93, .95, .96) | same (Red's porcelain white) |

### 3.3 Lamps the glaze can see: reflection proxies (`Structures/*.cs`, ours)

The direct-view lenses keep Red's levels (1.4-2.3; Level 0's L03 is 1.8 by Red's word). The glaze needs a lamp 50-200x the wall, so each pool-family lens gets a **reflection proxy**: a second quad of the lens's size, 2 mm in front of it, on **layer 27** (unused in the project today: the game uses 28-31), seen only by reflection probes.
- Emission: **lamp tint x 30** (linear), about 60x a lit wall at the pool preset's levels. Reflected at F0 0.04 that is about 1.2 against a wall of 0.3-0.6: 2-4x, POOLS' measured glint. Tune 15-60 by eye on the sheet.
- On: F5 sealed lens (0.43 x 0.40 m lens face), down-fill (0.39 x 0.36), underwater niche (0.26 x 0.24), step lights, laylights, the oculus. Not on the wall boxes (their lens faces the lid; the lit lid is captured as it is).
- The proxy follows its lamp's **state** (night key, dead, flicker; driven by the same property block) but not the walker's distance budget: a lamp that is on is on in every reflection.
- `FrontRoomsStructuresWalker` view: `cullingMask &= ~(1 << 27)`; the review harness's cameras the same. Every other camera that renders the scene must skip layer 27 too, or it shows a lamp at 30x: 游戏视觉's water mirror cameras (`FrontRoomsWaterPlanar`, once `pool_water` is in scene 4) and GlassRT's instance list (J key); checked in the clone run (§8), raised with 游戏视觉 if either picks the proxies up.
- Why not brighter lenses: the lens itself would bloom hard (bloom threshold 1.05, intensity 0.55, scatter 0.72 in `FrontRoomsPost.asset`) and change Red's lamp look.

### 3.4 Probes (`FrontRoomsStructuresLook.cs`, ours)

- Capture on **arrival** in a district (`Arrived`), not once after the build: two frames after arrival, every lamp of the district forced on for one frame (the `view` flag, as the review harness's `ForceLamps` does), `RenderProbe()` on each of its probes, then the budget returns.
- Re-capture when the district's look or a lamp state changes (SP1's N night key and F flume; SP7's looks; the dark zone).
- Unchanged: SP1 one probe 21 x Lid x 21 m at 1024; SP6 five at 512 (the room bounds in `PoolTerrain.cs` 66-72); HDR; importance 2; near 0.1; far max(20, box diagonal); `NoTimeSlicing`.

### 3.5 Files and owners

| File | Change | Owner |
|---|---|---|
| `W/level_space/tile/make_pool_tile.py`, `README.txt`, `out/LS_PoolTile150_{A,N,S}.png` | v2 | ours |
| `Assets/Structures/Textures/LS_PoolTile150_{A,N,S}.png` + N `.meta` (format pins) | swap, same GUIDs | ours (landed 07:14 by this line) |
| `Structures/FrontRoomsStructuresKit.cs` | proxies in `Lamp()`, `DownFill`, `Underwater` | co-claimed 关卡设计 + this line |
| `Structures/FrontRoomsStructuresLook.cs` | capture on arrival and on state change; proxy state | co-claimed |
| `Structures/FrontRoomsStructuresWalker.cs` | culling mask | co-claimed |
| `Structures/FrontRoomsStructures.cs` (bootstrap) | material values after `SetTileSet` | co-claimed |

Gate: Roslyn macOS + iOS 0 errors; `fr land` with CLAIM as 关卡设计 + 夸张结构调研与设计.

### 3.6 Cost

| | Desktop | iPhone | iPad | WebGL |
|---|---|---|---|---|
| Scene 4 (Editor only, never in a build) | 0 GPU per frame; textures the same size; a probe capture per arrival (SP1 6 faces at 1024, not timed); 0 draws in view (proxies are culled) | – | – | – |
| In the game (when the numbers go into 游戏视觉's Field tile, §7) | 0 | 0 | 0 | 0 |

### 3.7 What it fixes, and its honest flaw

Fixes: lamps visible in the glaze; dash per tile with wobbling edges instead of a flat mirror per tile; the dark wire; probes that miss far lamps.
Flaw: the lamp images come from an **unprojected** probe, so on the walls they are in the wrong place and slide with the camera; SP6's merged tile renderers still take one probe each for several rooms; no AA, so the cushions and far walls **crawl** beyond about 8 m in motion; the tilt pattern **repeats every 1.5 m**.

---

## 4. GZ-B · Lamps held in the glaze (A + 游戏视觉's shader + ours)

### 4.1 Shader: `_FR_GLAZE` in `FrontRooms/Surface` (游戏视觉's file)

A `shader_feature_local_fragment _FR_GLAZE` that only the pool tile materials enable (their pattern for `_FR_TILE`, `_FR_OWN_LIGHTS`). Every other material compiles exactly as before; the URP asset is **not** touched.
1. **Box projection for this material only:** before the lighting includes,
   `#if defined(_FR_GLAZE)` / `#define _REFLECTION_PROBE_BOX_PROJECTION 1` / `#endif`.
   URP 17.6's guard (`GlobalIllumination.hlsl` 39-41) then keeps it 1, and `BoxProjectedCubemapDirection` uses the per-object probe's box (bound in Forward+ with blending off, `UniversalRenderPipeline.cs` 2377-2380; the probe's `boxProjection` flag arrives as `unity_SpecCube0_ProbePosition.w`). No game-wide change, none of `ref_engine`'s blending trap. To verify in the clone: a lamp image in a wall stays on the lamp while the camera strafes.
2. **Specular anti-aliasing:** `s.smoothness = ProjectedSpaceGeometricNormalFiltering(s.smoothness, inputData.normalWS, _GlazeAA.x, _GlazeAA.y)` on the mapped normal (core RP `CommonMaterial.hlsl` 224; Tokuyoshi 2021). `_GlazeAA` = (**0.10, 0.18**): screen-space variance and threshold, HDRP's defaults are 0.1 / 0.2. Inside a tile the normal hardly changes between pixels, so per-tile glints survive; joints and cushions under a pixel get blurred instead of crawling.
3. **Per-tile set:** `cell = floor(metres / 0.15)` (wrapped every 192 m), hash → `nTS.xy += (h2 * 2 − 1) * tan(0.18°)` on the glaze (the same hash and amount as 游戏视觉's `FRTileSurface`). Precision-free and never repeating: the 1.5 m repeat of the map's tilts disappears under it.
- Cost: box projection about 12 ALU, AA about 15 ALU, set about 12 ALU; 0 fetches; 2 variants, pool tile only.
- In the long run this code belongs **inside `_FR_TILE`** (游戏视觉's pool keyword) when scene 4 moves to their Field tile (§7); `_FR_GLAZE` is only needed while scene 4 keeps the LS set.

### 4.2 Material

| Value | GZ-A | GZ-B |
|---|---|---|
| `_Smoothness` | 0.87 | **0.93** (the AA keeps it from crawling; 游戏视觉's glaze is 0.935) |
| `_GlazeAA` | – | (0.10, 0.18) |
| keyword | – | `_FR_GLAZE` |

### 4.3 Probes and renderers (ours)

- **One renderer per probe box:** `FrontRoomsStructuresMesh` `GroupKey` gains the index of the probe box that holds a part's centre. Districts declare their probe boxes before `kit.Finish()` (today they call `Probe()` after it). Each of SP6's shell groups splits along its six probe boxes (five rooms and the corridor), so every room's walls take their own room's probe.
- **SP6 corridor probe:** box x 3..6, z −3..6, y 0..2.9 (local), 512, HDR.
- Every probe box = the inner faces of its room (box projection treats the walls as the box). SP6's five boxes in `PoolTerrain.cs` 66-72 already are; SP1's (21 x Lid x 21 m) covers the hall only.
- No probe blending: it would be a game-wide URP asset change (`m_ReflectionProbeBlending`, 设置菜单's and 游戏视觉's call) plus the keyword trap; per-room renderers do the same job here.
- Game tiers (when 游戏视觉's pool takes it): realtime per-room probes at 256 on iPhone and iPad; WebGL takes 游戏视觉's baked 128 px RGB9E5 cubes.

### 4.4 Files and owners

GZ-A's files, plus: `Assets/Resources/Rendering/FrontRoomsSurface.shader` (`_FR_GLAZE`; **游戏视觉**; sent as a spec + the clone prototype's diff, `W/apply/glaze_b_contract/`), `Structures/FrontRoomsStructuresMesh.cs` and the district files that declare probe boxes (co-claimed).

### 4.5 Cost

| | Desktop | iPhone | iPad | WebGL |
|---|---|---|---|---|
| Per pool-tile pixel | about +40 ALU, 0 fetches | same | same (about 1.8x iPhone's pixels) | same (derivatives are core in WebGL2) |
| Draws | SP6 +10-20 (estimate; the district's cost line counts them) | game: per-room chunks already | same | same |
| Memory | +16 MiB (SP6 corridor probe at 512 HDR: 6 x 512² x 8 B + mips) | probes 256: 4 MiB each | same | baked 128 px cubes |
| Frame time | not measured (rule 25) | not measured | not measured | not measured |

### 4.6 What it fixes, and its honest flaw

Fixes: lamp images anchored in the walls with correct parallax; one probe per room; crawling cushions and far-wall noise; the 1.5 m glint repeat.
Flaw: box projection is exact only in **box-shaped rooms** (SP1's arcades, SP6's apse and dome bend lamp images wrongly near curved walls); probes are **static** (re-captured on state changes; the Relay and anything moving is not reflected; nothing off-probe is reflected: no SSR in Unity 6.6, URP's ships with 6.7); the shader part waits on 游戏视觉.

---

## 5. GZ-C · Glaze and water light (B + water light + two lobes) · REC

### 5.1 Two-lobe glaze (游戏视觉's shader, inside `_FR_GLAZE` / `_FR_TILE`)

Real glaze is a mirror whose waviness gives a sharp core inside a soft halo (Barla et al. 2018, "hazy gloss"; `ref_engine` §2.1). Environment only, so punctual lights keep one lobe:
- Environment: two box-projected fetches of the same probe, **core smoothness 0.97, weight 0.6** and **halo 0.62, weight 0.4** (each through the AA filter). Energy stays at F0 0.04.
- Punctual lights: one GGX at **0.92** after AA (a 0.97 point highlight would be a firefly).
- Implementation: an `FRGlazePBR` copy of `UniversalFragmentPBR` with `GlobalIllumination` replaced, as `FROwnLightsPBR` already is. Not URP's clear coat (twice the material cost, a second GGX per light, and its base F0 under the coat goes to 0 for our 0.04: `ref_engine` §2.2).
- Cost: +1 cube fetch, about 20 ALU.

### 5.2 Water light on the tiles

| Layer | Where | How | Owner |
|---|---|---|---|
| Floor nets and submerged walls | under the water | 游戏视觉's `FRSubmergedPBR` (absorption, glaze under water at a tenth); ≤ 2 compact sources per body (`SetSources`): 50 mm aperture, 54° beam, intensity 7.5, (.90, .97, 1), at the ceiling's edge aimed across the water | 游戏视觉 (`pool_water`); placed by 水体仿真调研's bodies; the sources' fixtures are 灯具适配's kit (hidden until then, as 游戏视觉's harness does) |
| Ceilings and upper walls over the water | above the water, in the body's room | 游戏视觉's `FRSubThrown` from wet niches: our `Kit.Underwater` calls `FrontRoomsWaterSubmerged.RegisterNiche(light, intensity)` for the current district's nearest 16 (their cap; SP6 has 25 niches, SP1 3) | ours (Kit, Look) + 游戏视觉 |
| **Lower walls lit by lamps across the water (new)** | faces looking at the water, 0-3 m above it | `FRSubReflected` (spec below): the band every indoor pool shows, strongest at the waterline | 游戏视觉 (their listed follow-up, 70 §7.11) on our spec |

**`FRSubReflected`, per compact source S above the water (≤ 2 per body), for a tile point P:**
- S' = S mirrored in the water plane; B = where S'P crosses the plane; skip if B is outside the body's water rectangle or P is under the water.
- Light = I_S x F_water(θ_B) x cos θ_P / |S'P|², with water's exact Fresnel (2 % face-on, 6 % at 60°, 13 % at 70°, 35 % at 80°; `ref_physics` §4.3); only where N_P · (B − P) > 0.2.
- Caustic = 游戏视觉's long loop at B's xz, **lens distance D = 2 x |BP|** (a reflection turns a slope by twice its angle), held at the last slice (1.8 m) beyond; mip from the source's blur (its angular radius x |BP|).
- Fade: Fresnel and distance give it; a hard stop at 3 m above the water, a soft roll-off from 2 m.
- Added as diffuse light on the albedo (no fake highlight), like `FRSubThrown`.
- What it looks like (`ref_physics` §5): mean 5-45 % of the source's direct light on that wall (source 5-20 m away), peaks 1.5-3x the mean; cells 10-40 cm on the wall; apparent speed 0.3-1.0 m/s on the ripple's clock (loop 2.56 s); large-area modulation ≤ 3 Hz (photosafety, rule 11); Reduce Motion freezes it with the ripple.
- Cost: per source 2 taps (desktop, two layers) or 1 (phone loop, WebGL scrolled) + about 30 ALU; only on pool-room pixels within 3 m above a water.

**Why not a light cookie** (the brief asked for cookie size and speed; this is the comparison): a spot light per pool side with a 256² RenderTexture cookie, 60° cone, the cookie tile about 1.2 m at a 3 m throw, two layers scrolling 0.04 and 0.06 m/s. Rejected: URP lights have no "specular off", so a hidden caustic light puts a **fake hot spot** in a 0.9-0.93 glaze; it takes one of the 32 visible light slots on iPhone, iPad and WebGL (6 shadow maps if shadowed); it cannot tell above from below the water; a scroll reads as a slide, not as water (`ref_engine` §4.2 A).

### 5.3 Files and owners

GZ-B's, plus: `FrontRoomsSurface.shader` (two lobes, `FRSubReflected`; 游戏视觉), `pool_water` landed (游戏视觉), bodies in scene 4 (水体仿真调研, `FrontRoomsStructuresWater.cs`), niche registration and the compact sources' positions (ours, `Kit.cs`, district files), the sources' fixture kit (灯具适配).

### 5.4 Cost

| | Desktop | iPhone | iPad | WebGL |
|---|---|---|---|---|
| Ours per pool-tile pixel | B + 1 cube fetch + about 20 ALU; + ≤ 4 taps + about 60 ALU near water | B + 1 fetch; band 1 tap per source | as iPhone, 1.8x the pixels: the heaviest | B + 1 fetch; band 1 scrolled tap per source |
| 游戏视觉's water stack (their numbers, UNDER LOAD, 2560 x 1440) | hall +1.89 ms, rooms +0.75-1.05 ms (submerged + caustics) | phone paths, not timed | not timed | two scrolled layers, not timed |
| Memory | caustic loops 4.0 + 2.0 MiB (shared) | 0.5 + 2.0 MiB ASTC | same | phone loop + 64 px long loop, RGBA32 |
| Frame time of our terms | not measured (rule 25) | not measured | not measured | not measured |

### 5.5 What it fixes, and its honest flaw

Fixes: moving water light where it really is (floors, submerged walls, the band on the lower walls, the niches' ceilings), the plastic single lobe, and Red's "波光粼粼" in its literal sense on top of POOLS' glaze glints.
Flaw: it **depends on two other lines' landings** (`pool_water`, the water line's stage 2) and on new shader work from 游戏视觉; in the calm level water the bands are **faint** and wake only when the water moves (physics; 游戏视觉 measured the same for floor nets, 71 §3.4); caustic light is **not shadowed** by pillars, rails or the Relay (room test only); the costliest on iPad.

---

## 6. Recommendation: GZ-C, landed in the order A, B, C

- Red asked for two things in one sentence: POOLS' glaze (the lamps in the tiles, A + B) and the sparkling-water feeling (C). Only C gives both, and the desktop is the reference (VISUAL_STANDARD 1.1).
- Each step lands on its own and is useful alone: **A** as soon as Red picks (our files, 0 GPU; the sheet's TODAY column is rendered before it lands, or from a kept copy of today's maps); **B** as a contract to 游戏视觉 (the clone prototype's shader diff) plus our probe and renderer changes; **C** after `pool_water` lands and the water line's bodies are in scene 4.
- Its honest flaw, said once more: until B's shader lands the glints slide, and until `pool_water` lands there is no water light; in calm water the bands are faint by physics.
- If the sheet shows C's bands too faint to matter, B alone is the fallback and nothing is wasted.

---

## 7. Fit with 游戏视觉's pool tile: one tile, not two

Two pool tiles exist: ours (`LS_PoolTile150`, 5 mm joints, plain shader path, scene 4) and 游戏视觉's Field (`PoolTile_Field`, 3 mm joints, `_FR_TILE`, the game). The game will use theirs; scene 4 should move to it once `pool_tile` lands, so Red judges one tile. Whatever Red picks here is carried into their set:

| GZ value | Ours (v2) | Into 游戏视觉's `gen_pool_tile.py` Field / `_FR_TILE` |
|---|---|---|
| Tilt in the map | σ 0.25°, max 0.6° | `tilt_sd_deg` 0.10 → 0.20 (their in-shader set ±0.18° stays) |
| Bow | 0.15-0.40 convex / 0.10-0.25 concave (20 %) | `bow_lo, bow_hi` 0.05, 0.20 → 0.15, 0.40 (+ a 20 % concave share) |
| Saddle | σ 0.05 mm | `saddle_sd` 0.04 → 0.05 |
| Peel, swell | 5.0 mm at 3.0 µm; 14 mm at 6 µm (0.21° rms per axis) | theirs is stronger (2.5 mm at 4.5 µm, about 0.65° rms; 12 mm at 10 µm); the scratch check read a short band as ragged at 0.93: suggest ours, their call after the sheet |
| Grout level | 0.9 mm at the foot, 0.4 mm dip | theirs already struck concave (`g_edge` 0.7, `g_dip` 0.6) |
| Smoothness | 0.93 (B), 0.97 / 0.62 two lobes (C) | glaze 0.935 stays; the two lobes are shader values |
| Box projection, AA | `_FR_GLAZE` | inside `_FR_TILE` |
| Lamp proxies, probe capture | scene 4's lamps | the lamp line's kits / 游戏视觉's capture tool (`Refl_Pool*`) |

Grout colour, joint width, course and deck are D8, already put to Red by 游戏视觉 (`71_look_for_red.md` §3.1). This design does not ask them again.

---

## 8. Render plan: the option sheet (VISUAL_STANDARD §2.2)

### 8.1 Where and when
- Clone **`W/proj_levelspace`** (this line's; no mkclone). It is the `scene4_fix` workflow's walk-test clone today: render only when that run has finished with it (one Unity process per line), checked with `W/tools/unity_jobs.sh` and the ledger.
- Before the run: save clone-only code (`Assets/Editor/LevelSpaceReview/*`), `fr sync proj_levelspace` from main if main moved (scene 4 must be main's; the fix package if landed), then `install_tools.py`. Check `find Assets -flags +dataless | wc -l` is 0 on main before syncing.
- Silent (rule 24): the clone's silencer and `m_DisableAudio: 1`. Private product name (rule 18): `PlayerSettings.productName = "FR LevelSpace Glaze (clone)"`.
- C needs `pool_water` in the clone: after it lands, the sync brings it. If it has not landed when the sheet runs, copy 游戏视觉's staged files read-only from `W/pool_look/pkg/overlay` into the clone only, and say so on the sheet (meta and a FLAG chip on the C column).

### 8.2 The clone-only editor script
`W/proj_levelspace/Assets/Editor/LevelSpaceReview/FrontRoomsGlazeOptionShots.cs` + `.meta` (fresh GUID; ASCII, no C# tuples; never landed). Built like `FrontRoomsStructuresReviewShots.cs` (same plumbing: `RunBatch`, `SessionState`, `EditorApplication.update` coroutine stack, 1920 x 1080 ARGB32 sRGB RT with the asset's MSAA, `Random.InitState(9001)`, render twice, JPG q95, `shots.tsv`, `log.txt`).
- Env: `GLZ_RUN` (default `glaze1`), `GLZ_COLS` (default `TODAY,A,B,C`), `GLZ_PHONE` (0/1), `GLZ_FILM` (0/1), `GLZ_KEY` (0/1, sheet 2).
- Per column, in one Unity start (the columns differ only in textures, material values, keywords and scene-4 runtime state):
  - **TODAY:** main's LS set and values; no proxies; probes as built.
  - **A:** `SetTileSet` with the v2 maps imported under `Assets/GlazeOptions/v2/` (same import settings as main's set, plus the N format pins); material values of §3.2; proxies (a clone-only helper adds them to the district's lamps through `Look.lamps` by reflection, layer 27, walker mask); probes re-captured with lamps forced on.
  - **B:** A + the clone's shader copy with `_FR_GLAZE` (§4.1, the prototype that becomes 游戏视觉's contract diff); `_Smoothness` 0.93; per-probe renderers (the clone's `Mesh.cs` copy) and the SP6 corridor probe.
  - **C:** B + two lobes + `FRSubReflected` in the clone's shader copy; 游戏视觉's water bodies (the water line's stage-2 placement), niches registered, two compact sources per body.
- After each column switch: re-capture probes, wait 45 frames, then each row.
- Phone row: a second Unity start with `GLZ_PHONE=1`; the static constructor calls `FrontRoomsHandheld.Force(FrontRoomsHandheld.IPhonePro)` (2622 x 1206, as `FrontRoomsLampOptionsShots` does), quality Medium, the phone tier's probes at 256 and C's phone loop.

### 8.3 Rows (the same camera in every column; only the subject changes)

| Row | Camera | Why |
|---|---|---|
| 1 SP1 arrival (the hall) | SP1 stop 0 "到达点：西边的平台" at the walker's eye (1.62 m, vFOV 76), as `SP1_s00` | the hall's walls and floor with the down-fill lenses |
| 2 SP1 catch pool edge | SP1 stop 2 "落水池边", feet (3, 0, 10.4) local, looking at (−2.5, 0.8, 15.5), as `SP1_s02` | lamps in the walls over the water, the lower-wall band, the floor nets |
| 3 SP6 arrival | SP6 stop 0 "到达点：瓷砖走廊", feet (4.5, 0, 1.5), looking at (4.5, 1.62, 21), as `SP6_s00` | the corridor's three F5 lenses in both walls at grazing angles: the streak |
| 4 SP6 grotto (added: POOLS' flooded corridor) | SP6 stop 6 "水洞尽头的壁龛", as `SP6_s06` | the frame closest to Red's screenshot: tile walls over dark water |
| 5 Hero 0.35 m | SP6 corridor west wall: a raycast −x at eye height from (4.5, 1.62, 1.5) finds the face; eye 0.35 m off it, yaw facing the wall, pitch +20° up, vFOV 60 (as 游戏视觉's S10). The lens at (4.5, 2.85, 1.5) mirrors to about 0.25 m above the eye, about 15° above the frame centre: an image of about 9-11 cm on one or two tiles | bow, peel, cushion hairline, the two lobes |
| 6 Phone tier | row 2's camera at the iPhone profile (second run) | the phone paths |
| 7 COST | text tiles per column: GPU per pixel, draws, memory, files and owners; "frame time not measured" | §2, §3.6, §4.5, §5.4 |

### 8.4 Film (the shimmer is motion; stills cannot show the crawl or the bands)
- `GLZ_FILM=1`: `Time.captureFramerate = 30`; two moves per column: **SP6 corridor dolly** z −2.5 → 5.0 at 0.9 m/s (8.3 s), eye 1.62, looking +z with a ±8° yaw sweep; **SP1 pool-edge strafe** 3 m at 0.5 m/s (6 s) from stop 2, looking at the pool and its far wall.
- 1920 x 1080 frames to `W/level_space/renders/glaze/<run>/film_<col>_<move>/`, then ffmpeg H.264 CRF 18, `-an` (no audio track): `film_<col>_<move>.mp4` and one 2 x 2 grid `film_grid_<move>.mp4` at 3840 x 2160. Played back only as frames for checking (rule 24: nothing makes a sound).
- Photosafety check on C's films with 游戏视觉's method (whole frame, quarters, the water band: 0 flashes per second).

### 8.5 Sheet assembly
- `W/venv/bin/python W/tools/shotframe/shotframe.py grid W/level_space/sheets/glaze_options.jpg --cols 4 --width 3840 --tile "<col>_<row>.jpg|<LABEL>|<data>" …`, then `wrap` with the title "POOL TILE GLAZE · GZ OPTIONS", chip **WAIT-RED**, the sentence "GZ-C REC: lamps held in the glaze, broken tile by tile, plus moving water light; flaw: it needs two other lines' landings and faint bands in calm water", meta `VL### · <date time> · LEVEL SPACE · W/level_space/sheets/glaze_options.jpg · proj_levelspace, seed 9001, desktop High + phone IPhonePro, nothing landed in main`.
- Column labels: `TODAY`, `GZ-A GLAZE IN THE TILE`, `GZ-B LAMPS HELD`, `GZ-C GLAZE + WATER LIGHT · REC`. English only in the image.
- Fit check at full size: `W/level_space/sheets/glaze_options_fit.txt` ends `ALL LABELS FIT`. Any failed framing is written on its tile.
- Sheet 2 (optional, same run, `GLZ_KEY=1`): **light key**, the water line's finding (handoff 1a). Two columns, REC as built and REC with a dark lid (K2: wall boxes x 0.4, trilight sky x 0.6, lenses and proxies unchanged), rows 1-3. Red's choice, separate from GZ.
- Then a VL row (next free number) or `W/vl_pending/glaze_options.md`, and the Figma slide in the line's daily batch. The images Red keeps go to `Frontrooms3D/Documentation/research/level_space/sheets/` and `video/` with one LANDED row.

### 8.6 Not measured on the sheet
Frame time of every option (rule 25), the iOS and WebGL shader compiles (the clone compiles Metal for macOS only), the phone build's real ASTC result on the N map, and Play-mode probe re-capture timing.

---

## 9. Asks to other lines (through 总控; none blocks GZ-A)

1. **游戏视觉:** `_FR_GLAZE` (or the same three items inside `_FR_TILE`): box projection by a local define, `ProjectedSpaceGeometricNormalFiltering` (0.10, 0.18), the per-tile set for the plain path; for GZ-C the env two lobes and `FRSubReflected`. Spec and clone diff in `W/apply/glaze_b_contract/` and `glaze_c_contract/` when built.
2. **游戏视觉:** the GZ tile numbers for `gen_pool_tile.py` Field (§7) after Red picks; the reflection proxies' idea for their `Refl_Pool*` capture (lamps at physical brightness in the cube only).
3. **水体仿真调研:** stage-2 bodies in scene 4 call `SetSources` with two compact sources per body; we register the niches.
4. **灯具适配:** the compact caustic sources need a 1990 low-voltage downlight kit (50 mm aperture); and Red's P1 pick decides how much shimmer the game's hall can show (§0 item 6).
5. **Scene-4 fix review:** SP6_s08's floor UVs (§1.2 item 6).

## 10. Sources
- This folder: `ref_pools.md` (POOLS: S1-S23, D1-D5, C1), `ref_physics.md` (§2-§6, sources in its §8), `ref_engine.md` (W1-W3, URP 17.6 source).
- URP 17.6 / core RP source in `W/proj_levelspace/Library/PackageCache` (`GlobalIllumination.hlsl` 35-41, 242-260, 423-474; `UniversalRenderPipeline.cs` 2362-2380; `CommonMaterial.hlsl` 182-228), read 11-04.
- 游戏视觉's staged pool look: `W/pool_look/to_main/Documentation/research/bathroom_rooms/70_look_build.md` §4, §7 and `71_look_for_red.md` §3.4-§3.5; `W/proj_unlock` `gen_pool_tile.py`, `FrontRoomsSurface.shader` (staged), `Scripts/Rendering/Water/FrontRoomsWaterSubmerged.cs`; read 11-04.
- State files: `handoff/level_space.md` (§3 item 1a), `handoff/water.md`.

---

## 11. 给 Red（中文）：泳池瓷砖的釉面和"波光粼粼"：对比图出来了，请你选

夸张结构调研与设计，2026-11-04 15:59 更新（第一版 12:10 只有设计，没有图）。英文详细版是同一文件夹的 `design.md`；这一页是给你做选择用的。

你的原话：11-04 配 POOLS 截图说"然后我需要你reference pool这里，墙面上釉面的纹理，那种波光粼粼的感觉"；10-31 说"我希望底色不应该是偏暖的米白，而是和dream pool更相似的瓷白"。水体线转来的两句（13:2x）："目前水面的光没有给整个房间晕染上一层反射光所以看起来非常奇怪，光影水反应该是在一起相互影响的"、"如果水是青色，这个颜色是否会影响房间？"

这一轮只在克隆里渲染（`proj_levelspace`），主工程没有改任何东西，没有合进任何东西。所有视频都没有声音轨。

### 先说结论

- **推荐 GZ-C（釉面 + 水光）**，按 A、B、C 的顺序一步一步落地。三列里只有 C 同时做到：灯在墙上的倒影按砖碎开、瓷白最白、水的光进到房间里。
- C 现在有两处要调，图上能看出来：凑近 0.35 米时外圈的柔光太强，把灯影清楚的芯盖住了（建议把柔光的比例从 0.4 降到大约 0.2）；平静的水面上，水光的光带很淡（物理就是这样，水动起来才明显）。
- **手机上三个方案都看不到墙上的灯影**：iPhone 用的画质档位（Handheld）和图上手机行用的 Medium 都把"实时反射球"关了。手机要有灯影，得用游戏视觉那种提前烘好的泳池反射图，或者改画质设置。这是 B 和 C 落地前要和游戏视觉、设置菜单线一起定的事，不影响你现在选哪个。
- 第二张图（灯光基调）：推荐 **K1 照现在**，K2 的差别很小。
- 游戏视觉的泳池区域外观会让墙变成灰蓝，所以主图用的是场景 4 自己调好的灯光（你现在走场景 4 看到的那种瓷白）。区域外观那张图单独放着作参考，游戏视觉已经知道这件事。

### 1. 怎么看这张图

主图：`glaze_options.jpg`（4800 像素宽，放大看）。

- **五列**，从左到右：TODAY（现在的砖）、FIELD · GAME TILE (REF)（游戏视觉做的、游戏里会用的砖，只作参考，不是选项）、GZ-A、GZ-B、GZ-C（推荐）。图上 C 的标题缩写成"GZ-C + WATER · REC"，全名写在图顶的说明里。
- **七行**，每一行五列都是同一个机位，只有砖和灯的做法不同：
  - R1 SP1 到达点：大厅的墙、地面和顶上的灯。
  - R2 SP1 落水池边：水池后面那面墙上的灯影、池边下半截墙、池底。
  - R3 SP6 瓷砖走廊：三盏顶灯在两边墙上斜着看到的倒影。
  - R4 SP6 水洞尽头：最像你那张 POOLS 截图的一格，白砖拱顶加下面的水。
  - R5 凑近 0.35 米看走廊墙：一盏灯在一两块砖上的倒影，看砖面的鼓、釉面的细小波纹、砖边的细亮线。
  - R6 手机：R2 的机位用 iPhone 的画面比例和手机画质。
  - COST：每一列的代价（文字格）。
- 每一格上面那条字：先是列名，再是行名；**哪一格没做到它要展示的东西，就直接写在那一格上**（比如"halo hides core""probes off"）。
- 所有格子：场景 4 自己的灯光（Q 关着），加上水的反光（I 开着，第 1 档：水把一点青色的光反回房间）。种子 9001，桌面画质 High。

视频（没有声音，可以直接看）：

- `film_grid_dolly.mp4`：SP6 走廊里往前走，每秒 0.9 米，2 x 2 四格（TODAY、GZ-A / GZ-B、GZ-C）。看灯影是不是钉在墙上、远处的墙会不会"爬"。
- `film_grid_strafe.mp4`：SP1 落水池边往右横移 3 米（后半段镜头越过池边，停在水面上方），四格同上。看水面、池边墙的光有没有动起来，墙上的灯影是不是跟着走。
- `film_pair_dolly.mp4`、`film_pair_strafe.mp4`：FIELD（游戏的砖）和 GZ-C 并排。
- 每一列单独的视频也在：`film_<列>_dolly.mp4`、`film_<列>_strafe.mp4`。
- 闪烁检查（WCAG 2.3.1，整帧、四个角、水面那一条、上半部分）：九段视频 0 次；只有 GZ-C 的走廊视频右下角有一次很慢的明暗变化（镜头经过那块牌子，1.5 秒变暗、1.3 秒变亮），GZ-B 是同样的变化、只差一点没到门槛。不是水光造成的，WCAG 的上限是每秒 3 次。

### 2. 每一列你走进去会看到什么

**TODAY（现在）**：墙是瓷白的，但墙上几乎看不到灯的倒影，只有地面上的灯光按砖碎开。砖缝在 2 到 5 米看起来是一圈圈深色细线。整体像一面平的、哑一点的白墙。

**FIELD · 游戏的砖（参考）**：游戏视觉做的泳池砖（3 毫米砖缝，每块砖有自己的小角度），贴在同样的墙上。离得远的时候和 TODAY 几乎一样：墙上也没有灯影，因为它用的是游戏现在的反射方式。凑近 0.35 米能看到一个柔和的灯影，和 GZ-A 差不多。放这一列是为了让你知道：**不管选哪个，最后都只有一种砖**，你在这里选的釉面数值会写进游戏视觉的砖里，场景 4 之后也换成它。

**GZ-A 釉面做进砖里**：砖的贴图换成新的（每块砖微微鼓起、表面有很细的波纹、砖缝浅一点）。灯旁边的墙上出现软软的亮块，按砖稍微碎开；砖缝变成浅色细线，不再是深色线。缺点：灯影不在正确的位置，你走动时它跟着你滑；凑近时灯影是一团软光。

**GZ-B 灯被釉面"钉住"**：在 A 的基础上，灯的倒影停在墙上该在的位置，走动时像真的镜面。SP1 后墙上是一排排灯影，每块砖一小段、彼此错开一点，这就是 POOLS 那种碎光（R2 最清楚）；SP6 水洞的拱顶上，灯影按砖切成一块一块（R4）；凑近 0.35 米，灯影是一块清楚的亮片，边缘有轻轻的波纹，在砖缝处断开、两边错开（R5，这一格最像真釉面）。**缺点**：在 SP6 走廊里（R3），每条砖缝两边的圆角都反射了亮的天花，变成一圈很亮的白线，墙面本身反射走廊尽头，看起来偏灰，瓷白在这一格没守住。

**GZ-C 釉面 + 水光（推荐）**：B 的全部，加上：釉面有两层反光（一个清楚的芯加一圈柔光），所以墙看起来最白、最像瓷器，走廊里的砖缝是浅色细线，不是白线（R3）；水下壁龛灯的光从水里透出来，给水洞右边的墙和拱顶染上一层淡淡的青色（R4，很淡，和 B 对比才看得出）；水池里的光会随着水波动（看横移视频）。**缺点**：凑近 0.35 米时（R5）那圈柔光太亮，把墙抬到接近画面最白，灯影清楚的芯看不出来了；平静的水面上，池底的光网和池边墙上的光带很淡，静止的截图里几乎看不到。

**手机这一行（R6）**：TODAY、FIELD、A、B、C 看起来差不多。原因是手机画质档位关了实时反射球（每次渲染日志都写着反射球 0/1 拍成），所以 A、B、C 的灯影在手机上都没有。这不是机位问题，是平台设置的事实。

### 3. 代价

| | 每个瓷砖像素的 GPU | 显存 | 要改谁的文件 |
|---|---|---|---|
| TODAY | 现在 | 现在 | — |
| GZ-A | 不增加 | 不增加（贴图同样大小） | 只有我们自己的（贴图、材质数值、只给反射球看的"灯的替身"、反射球拍摄时机） |
| GZ-B | 大约多 40 条计算，不多读贴图；SP6 多 19 个渲染物体 | 多 16 MB（SP6 走廊多一个反射球） | 加上游戏视觉的 shader（一个只给泳池砖用的开关，别的材质完全不变） |
| GZ-C | B 再多读一次反射、约 20 条计算；水边 3 米内再多一点 | 加游戏视觉的水光贴图（桌面 6 MB，和游戏共用） | 加上游戏视觉的水（pool_water，已经在主工程）和水体线放进场景 4 的水 |

帧时间三个方案都**没有测**（规则 25）。游戏视觉测过他们的水光：桌面大厅大约多 1.9 毫秒（机器负载很高时测的，只能参考）。手机那一行是在 Mac 上模拟 iPhone 的画面和画质，不是真机。

### 4. 老实说的缺点（全部）

1. **B 在走廊里砖缝是白线**（R3）：盒投影让砖边的小圆角反射出亮的天花和灯。C 的两层反光把它压下去了，所以推荐 C 而不是 B。
2. **C 凑近时柔光太强**（R5）：灯影的芯被柔光盖住。要调：柔光比例 0.4 降到约 0.2，或者灯的替身亮度从 30 倍降到 15 到 20 倍。没在这一轮调，因为要你先选方向。
3. **平静的水上光带很淡**：池底光网和池边墙上的光带在静止截图里几乎看不到，水动起来（跳水、滑梯落水、踩水）才明显。物理如此，游戏视觉测他们自己的水也是这样。
4. **手机上没有灯影**：实时反射球在手机画质档位是关的。手机要灯影，需要烘焙好的反射图（游戏视觉的 Refl_Pool* 做法）或改画质设置，这要和游戏视觉、设置菜单线一起定。
5. **盒投影只在方盒子房间里最准**：拱廊、半圆的房间、穹顶附近，灯影会有一点弯错。
6. **反射球是静止的**：Relay 和会动的东西不会出现在墙上的倒影里。
7. **水光不会被墙和柱子挡住**：靠近别的房间水池的墙，可能会带上一点那边的水光。
8. SP1 落水池边的横移视频，原来的路线（向左 3 米）会走进拱廊、后半段被柱子挡住，已经改成向右横移 3 米重新渲染（主图的视频）。区域外观那一套视频还是旧路线，后半段被柱子挡住，只作参考。

### 5. 推荐：GZ-C，按 A、B、C 一步步落地

- 你那句话里有两件事：POOLS 墙上釉面的碎光（A 加 B 做到），和"波光粼粼"的水光（C 做到）。只有 C 两样都有，而且墙最白。
- 每一步单独也有用：A 只改我们自己的文件，你选了就能合进主工程；B 和 C 的 shader 部分交给游戏视觉（合同和差异文件已经准备好）；水光用的是已经在主工程的 pool_water。
- C 落地时一起调两个数：柔光比例（0.4 改成约 0.2），手机的反射方式（和游戏视觉、设置菜单线一起定）。
- 如果你看视频觉得 C 的水光太淡、不值得，就退回 B；前面做的都不浪费。

### 6. 第二张图：灯光基调（`glaze_light_key.jpg`）

- 水体线之前发现：天花和墙上半截太亮，会把水面和光斑冲淡。K2 是"天花暗一点"：墙灯往上打的光乘 0.4，照天花的环境光乘 0.6。灯罩和替身不变。
- 两列都是 GZ-C，三行（SP1 到达、SP1 落水池边、SP6 走廊）。
- 在场景 4 自己的灯光下，**K2 的差别很小**：瓷砖亮度只低 1% 到 2%，主要是墙灯附近的上半截墙和天花暗了一点，下面的瓷白一样；走廊里几乎没变（那里没有墙灯）。
- **推荐 K1（照现在）**。如果你想要更暗的天花、让水面更突出，可以选 K2，但它不会让光斑明显变多。

### 7. 游戏视觉的泳池区域外观那张图（`glaze_options_zone.jpg`，带 FLAG）

- 游戏视觉的泳池区域外观（PoolLevel，12:52 起在主工程，游戏里的泳池层现在就是这样）放进场景 4 以后，**瓷砖从瓷白变成灰蓝**：瓷砖亮度低 11% 到 39%。原因是它的环境光大约只有场景 4 今天调好的环境光的三分之一，调色本身差不多。
- 所以场景 4 的泳池区域外观从 15:14 起**默认关着**（按 Q 打开），主图也用场景 4 自己的灯光，保住你要的瓷白。
- 那张图用的是 15:05 在区域外观下渲染的同一套机位，没有重拍。在那张图上：C 的水下壁龛光是关的（PoolLevel 把这个光设成 0）；C 的 R1、R2、R6 里 SP1 落水池是"干"的，那是我们渲染工具的一个错误（主图已修好），不是方案的问题。
- 这个灰蓝的发现已经告诉游戏视觉（数字和对比帧都给了）：要么 PoolLevel 的环境光在这种灯光的房间里要提高大约 3 倍，要么泳池层的灯要自己补上反光。

### 8. 水是青色，会不会影响房间？

会。主图每一格都开着水体线的 I（第 1 档）：水把一点青色的光反回墙和天花。测出来，在水附近的墙和天花上，蓝和绿比红多一点，最亮的地方大约多 16 到 26 个亮度级（在区域外观下测的；场景 4 自己的灯光下大约一半）。第 1 档在截图里很淡，第 2 档是两倍。它和釉面是两回事：釉面决定墙上能不能看到灯影，I 决定房间有没有被水染上一点青色。两样一起，才是你说的"光影水反应该是在一起相互影响"。如果你觉得还不够，可以单独说"I 开到第 2 档"。

### 9. 怎么回复

- 回"按推荐"，就是：GZ-C（落地时把柔光调到约 0.2），K1。
- 或者直接写你选的：GZ-A / GZ-B / GZ-C，K1 / K2。
- 可选：水的反光 I 要不要开到第 2 档。

### 10. 文件

- 主图：`research/level_space/sheets/glaze_options.jpg`（检查文件 `glaze_options_fit.txt`，ALL LABELS FIT）
- 灯光基调：`research/level_space/sheets/glaze_light_key.jpg`
- 区域外观参考（FLAG）：`research/level_space/sheets/glaze_options_zone.jpg`
- 视频（缩小到 2560 宽、带水印，放在文档旁边）：`research/level_space/video/glaze_options_film_dolly.mp4`、`glaze_options_film_strafe.mp4`
- 视频：`W/level_space/renders/glaze/glaze2/film_grid_dolly.mp4`、`film_grid_strafe.mp4`、`film_pair_dolly.mp4`、`film_pair_strafe.mp4`（和每列单独的视频）；区域外观那一套在 `glaze1/`
- 所有截图原图：`W/level_space/renders/glaze/glaze2/`（主图）和 `glaze1/`（区域外观）
