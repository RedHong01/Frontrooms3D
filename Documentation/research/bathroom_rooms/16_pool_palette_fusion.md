# Bathroom rooms · 16 · Colour and light for the pool family: the 1990 core fused with the Backrooms pool look

Date: 2026-10-31. Line: 浴室房间调研, K session (Red's API key, Cursor, `cursor_K_bathroom_1031`).

Red, 10-31 ~15:14 (K chat), after the first direction stills: "我需要你进一步深化，在怎么结合你之前做的那种和时代语境相符的设计内核的情况下，也要符合普遍的后室的对于泳池关卡的一个视觉形象和配色方案……因为现在的这一套方案似乎很还原，但是和之前的其他level没有很明显的配色方案的差异，并且整体的氛围也是米黄色的。我认为这会让玩家对着干房间的既有印象感到没有过多变化。"

Builds on `15_poolrooms.md` (canon, origin, grammar) and `40_direction_images.md` (the first stills). The water itself (reflection, ripples, ray tracing) is researched in a parallel sub-session: `17_water.md`.

## 0. Short answer

1. **Why every still reads beige: one ambient for the whole game.** `FrontRoomsLook.ApplyAmbient` sets a warm trilight (sky .20/.19/.15, equator .26/.24/.17, ground .40/.36/.24) and warm fog (.16/.15/.11, exp² .014) once at run start; zones only switch reflection cubes (`FrontRoomsZoneReflection`). Any new zone inherits Level 0's yellow, whatever its tiles and lamps. The pool family needs its own ambient, fog and grade: an ask for 游戏视觉 (§6).
2. **The genre's look is white tile, turquoise water and moving light.** Canon: white ceramic tile, "the only deviation from this color being the blue-green hue of the water" [PR1]. Descriptions agree: "serene blue-green waters, pristine white tiles" [PR10]; "desaturated aqua on warm off-white" [PR11]; palettes built from such images run tile white → pale aqua → turquoise water → grey-green grout → one deep teal for drains and dark corners, with at most one warm fluorescent glow [PR9]. The signature is the caustic light dancing on walls and ceiling, "the single image everyone has of this place" [PR12].
3. **The era has its own, very different colours.** 1990 commercial interiors: "mauve is out in '90s — teal, rose, copper are in" (Color Marketing Group, July 1990) [PR13]; late-'80s "ice cream pastels" (rose, peach, seafoam, mint, dusty blue), grey with peach or mint, brass and mirrors [PR15, PR14]; "flamingo pink and aqua turquoise", recycled for the 1990s [PR14]. Pools themselves: commercial basins white by health code [PR17], aqua waterline tile the oldest and commonest accent [PR16], "blue waters" in a white hall [PR18].
4. **The fusion rule: the era owns the objects, the genre owns the light and the water.** What things are (fittings, code, signs, the club's teal and rose) comes from 1990; how the place feels (cool light, turquoise water, caustics, high key, deep-teal darks) comes from the Backrooms pool look. Both agree on the white basin and the aqua waterline, which is where they join.
5. **The route becomes a colour-temperature descent.** Level 0's warm yellow → the club's cool-white fluorescent with 1990 teal and rose → the pool hall's white tile and turquoise water under metal halide → the pool level, high-key white and turquoise with caustics everywhere and no visible source → its dark pockets in deep teal. The player feels the change before reading any sign.
6. **Unlike every other level on five axes:** hue (cyan family, no yellow), value (high key), gloss (glazed tile against matte wallpaper and carpet), moving light (caustics against steady fluorescents) and sound (dampened, 15 §5).

## 1. Sources (new on 10-31; read as text, no image of any creator's work opened)

| # | Source | Date | What it gives | Status |
|---|---|---|---|---|
| PR9 | Every Hex Code, "Liminal Pool Rooms" palette, https://everyhexcode.com/palettes/liminal-pool-rooms | read 2026-10-31 | A community palette of the look: tile white, pale aqua, turquoise water, grey-green grout, one fluorescent yellow glow, deep teal "drain dark" | READ (low authority; corroboration only, values not copied) |
| PR10 | designboom, "diving into jared pike's imaginary liminal interiors & pool dreamscapes", https://www.designboom.com/art/diving-3d-artist-jared-pike-imaginary-liminal-interiors-pool-dreamscapes-02-18-2024/ | 2024-02-18 | "serene blue-green waters, pristine white tiles", light and shadow | READ (excerpt) |
| PR11 | The SideQuest Shop, "No Diving: Municipal Pool Props" colourways, https://thesidequestshop.itch.io/no-diving-municipal-pool-props | read 2026-10-31 | "Poolrooms: desaturated aqua on warm off-white"; "Night Swim: near-black deck, moonlit chrome, and water that glows" | READ (excerpt) |
| PR12 | glslop, "the pool rooms" shader notes, https://glslop.com/shader/yt9hscmb | read 2026-10-31 | Physics of the look: absorption makes shallow water over white tile turquoise, deep water not; caustics on walls and ceiling are the defining image; an accent course at the waterline; humid in-scattering puts shafts down the hall | READ (excerpt) |
| PR13 | Deseret News, "Mauve is 'out' in '90s - teal, rose, copper are 'in'", https://www.deseret.com/1990/7/8/18870572/mauve-is-out-in-90s-teal-rose-copper-are-in/ | 1990-07-08 | Color Marketing Group: teal, rose, copper lead the early 1990s; also indigo, ecru, brick red, terra cotta, periwinkle, sage | READ (excerpt) |
| PR14 | Christian Science Monitor, "They catch colors from the air and make trends", https://www.csmonitor.com/1988/1216/hpaint2.html | 1988-12-16 | Color Association of the US: 1990s "rising brights"; flamingo pink with aqua turquoise to return; deco grey, burgundy, rose, lavender | READ (excerpt) |
| PR15 | Home Accents Today, "A Colorful History", https://www.homeaccentstoday.com/styles-and-trends/colorful-history/ | read 2026-10-31 | Late-'80s ice-cream pastels, mauve and slate blue, grey with peach or mint, brass; early-'90s jewel tones | READ (excerpt) |
| PR16 | Landscaping Network, "Swimming Pool Tiles", https://www.landscapingnetwork.com/swimming-pool/tiles.html | read 2026-10-31 | Aqua waterline tile the earliest accent; 1980s hunter green and cobalt fashions, then back to blue waterline tile | READ (excerpt; residential focus) |
| PR17 | AQUA Magazine, "The Evolution of Concrete Color in Pools", https://www.aquamagazine.com/builder/article/15118567/the-evolution-of-concrete-color-in-pools | read 2026-10-31 | Most commercial pools must be white by health code | READ (excerpt) |
| PR18 | Christian Science Monitor, "Aquatics complex makes Indy great place to swim", https://www.csmonitor.com/1987/0810/ppool.html | 1987-08-10 | A 1982 natatorium: a white vaulted hall around "blue waters", the diving well "intensely blue" against the whiteness | READ (excerpt) |

## 2. Why the stills are beige (technical)

- Ambient and fog are global and warm (§0.1). A white tile lit by cool lamps still sits in a yellow-brown ambient and a yellow-brown haze, so its shadow side and every long view drift to cream.
- The stand-in's lamps were warm too: club troffers and strips at about 1.0/.96/.90, metal halide boxes at 1.0/.95/.87.
- The stand-in water is a dark teal at 30 % alpha (base .05/.20/.22): over white tile it reads grey, not turquoise.
- Caustics run at low levels (floor .9 only under the overheads, ceiling .3), so the defining moving light is nearly absent.

## 3. The fusion, space by space (the route's colour script)

| Space | Light (period device, colour) | Ambient and haze | Surfaces | Accent | Water | Darks |
|---|---|---|---|---|---|---|
| B-level corridor (unchanged) | Level 0 troffers, warm | Level 0 warm | Level 0 | Level 0 | none | brown-grey |
| Club entry and towel desk | Cool-white fluorescent (F40 "cool white", the period office lamp) | neutral, a touch cool | grey-mauve walls, grey vinyl tile, chrome, mirror | teal and rose (1990, PR13); one brass detail | none | neutral grey |
| Lockers and gang shower | Sealed fluorescent, cool white; vapour-tight globes warm | cool grey | white glazed tile to 1.5 m, teal lockers, grey floor tile | a rose or teal band | wet floor sheen | neutral |
| Pool hall | Indirect metal halide (cool white, slight green); wet-niche lamps warm, turned cyan by the water | cool aqua-grey, light humid haze | white tile to 2.4 m, white basin (code) | aqua waterline course (PR16); teal club band fades | turquoise over white | blue-grey |
| The turn (T1) | Light loses its source: valance or flush lenses, cool | cooler, brighter | tile climbs to the ceiling; teal and rose drop away | only the aqua waterline course remains | ankle water over the sill, pale turquoise | — |
| Pool level (T2 on, B, P, D) | Daylight-type fluorescent behind valances and flush lenses (cool, near 6500 K); caustics from every water surface | high key, faint aqua haze | white glazed tile everywhere, pristine | none but the waterline course | turquoise; deeper basins more saturated | deep teal, never grey-brown |
| Night or mains dip | Overheads out (metal halide restrike), wet-niche lamps only | near-black teal | — | — | the water is the light | black-teal; ceiling caustics carry the room |

Notes:
- **Warm is allowed once:** the wet-niche lamps are warm incandescent, but seen through water they read cyan (the water absorbs red, PR12), so the room stays cool; a single warm fluorescent glow is part of the genre palette too (PR9). Warm is never the room's ambient.
- **Era-correct cool light:** cool-white fluorescent was the common office lamp; metal halide is the pool hall's documented lamp (zone_lamps 10_pool §3); daylight lamps existed. Nothing here needs a post-1993 device.
- **The aqua waterline course is the hinge:** period-accurate in the club (PR16), and the genre's one accent (PR12). It survives the turn when every other colour drains away.

## 4. What makes the pool family unlike the other levels

| Axis | Level 0 / Office | Pool family |
|---|---|---|
| Hue | yellow wallpaper, beige carpet, ivory | white and turquoise, deep-teal darks |
| Value | mid-key, dim corners | high key in the level, deep pockets |
| Gloss | matte paper and carpet | glazed tile, water: everything reflects |
| Light | steady overhead fluorescents | sourceless wash plus moving caustics |
| Air | warm haze | cool humid haze |
| Sound | fluorescent hum | dampened, no tail (15 §5) |

## 5. Shown in the stills (sheets in `W/bathroom_dir/sheets/`, made with clone-only overrides)

The stills apply the fusion inside the clone only: per-shot ambient and fog overrides, a post volume (cooler white balance, slightly higher exposure), cool lamp colours, a turquoise water tint, stronger caustics, and an aqua waterline course. Options shown: F1 turquoise white (genre-forward), F2 1990 club colours into the turquoise level (era-forward), F3 night swim (wet-niche lamps only), each against the first pass and an actual Level 0 frame from the same run.

Delivered 10-31 15:2x (`-zlOption fusion`, 18 stills in `W/bathroom_dir/renders/pooldir/fusion/`, `FINISHED fail=0`):
- `D1_identity.jpg`: Level 0 (same run) | first pass | F1 chain | F1 hall | F1 pillar hall | F3 basin at night. The pool family now reads as a different place at a glance: Level 0 yellow and matte, the pool family white, turquoise and moving.
- `D2_F1_route.jpg`: F1 along the route (S1, S2, S3, S4, S5, S6, S7, S10). The club entry turns cool grey and plain under F1.
- `D3_F2_era_F3_night.jpg`: F2 club (grey-mauve entry, teal lockers with a rose band, teal hall band) draining to white and turquoise at T1; F3 night (hall, basin, stair).
- Clone values used (a starting point for 游戏视觉's zone profile, not a spec): trilight sky .15/.20/.22, equator .17/.23/.24, ground .21/.28/.28; fog .11/.16/.17 at .010; grade white balance -12 (the global film look has +9) and tint -2, post-exposure +0.45, contrast -4, saturation +4; lamps metal halide .90/.97/1, club cool white .93/.97/1, level daylight .86/.96/1 at 1.2×, wet-niche seen through water .55/.92/1; water base .16/.60/.62 at alpha .36; waterline course .40/.76/.77; caustic colour .32/.50/.50, ceiling levels .55–.65.
- Found while doing it: the global film look itself carries the yellow (white balance +9, tint -7, saturation -8 in `Resources/Rendering/FrontRoomsPost`), and the zone-grade path already exists (`FrontRoomsPostStack.EnsureZoneVolume` loads `FrontRoomsPost_<zone>`; Office and Run_B have one). A `FrontRoomsPost_Pool` profile plus a per-zone ambient and fog is all the build needs.

## 6. Asks for the build (all through 游戏视觉, owner of the shared rendering code)

1. **Per-zone ambient, fog and grade:** the pool family gets its own trilight, fog colour and density, and a grade (white balance, exposure), blended at zone borders like `SetZoneReflection` (`FrontRoomsLook`, `FrontRoomsRoomStream`, `FrontRoomsPostStack`).
2. **Lamp colours in the lamp kit:** cool white for the club, metal halide for the hall, daylight for the level, warm only for the wet-niche lamps.
3. **Water colour and caustics:** absorption-based turquoise and caustics on walls and ceilings (P4; see `17_water.md`).
4. **Tile set:** white glazed 6 in tile, an aqua waterline course, 1 in mosaic for basins, pristine in the level and worn in the club (P5).
