# Surface detail · 10 · How AAA games put information into a surface, and how it holds up under ray tracing

Line 游戏视觉 (`visual`), workflow `surface_detail`, stage Research, 2026-11-04. Red's asks are quoted at the head of `00_inventory.md`; read together they say: surfaces read too flat, too even and too neat, worst where light and reflections hit them (edges too crisp, faces too flat); learn how AAA games add information with normal maps and everything around them, make it hold up with ray tracing, and revise every room's surfaces, the pool tile first after POOLS.

**Done here:** web pages, papers and talk pages read as text (ids D1-D37 in `SOURCES.md`; "[search]" there means read through a search summary, not the page itself); POOLS' 13 store frames looked at in the browser pane at 1:1 (nothing downloaded, as `bathroom_rooms/17_water.md` W36). **Reused, not redone:** `wall_material/10_research.md` §2 (The Order: 1886's layers, The Last of Us Part I's vertex-colour sets, Uncharted 4's micro-shadowing, CoD WWII's gloss from normal length, Dishonored 2's rule against walls that look like grids; sources there), `bathroom_rooms/70_look_build.md` §4.1 (period tile T1-T9), `17_water.md` §10.2 (POOLS' water and camera).

## 0. The answer

A surface reads as real when its **light** varies the way a made, used object varies, at every distance the player sees it from. Shipped games get there with a short list of things, in this order of weight for our rooms:

1. **Roughness that varies.** The specular is where the eye reads a material. Wiped, worn, wet, touched, laid by hand: all of it is a change of gloss before it is a change of colour. Our smoothness is constant to +-0.01 on almost every material (00 §2).
2. **Relief that survives distance.** A normal map flattens as it mips; a glossy, bumpy surface then turns into a mirror far away (D2). Shipped answer: push the lost normal variance into roughness when building the mips (Toksvig, LEAN, CoD WWII), and carry the large-scale relief (waviness, warpage, lippage, bow) at a scale the screen can resolve.
3. **Edges that catch light.** Nothing real has a knife edge. A 1-3 mm round on a tile, a drywall corner or a door reveal catches a line of highlight and a line of shade; a box edge shows neither. Bevel in geometry where the silhouette or the highlight matters, in the normal map where it does not.
4. **Repetition broken at every scale.** Per-tile and per-instance hashes, hex or stochastic tiling, macro maps at metres, vertex-colour or mask blends.
5. **Wear with a cause, placed.** Curvature and cavity masks put wear on edges and dirt in recesses; authored decals put stains, scuffs and tide marks where something happened.
6. **Occlusion in the small.** Cavity and micro-shadowing so a groove darkens under direct light, specular occlusion so a recess does not reflect the sky.
7. **All of it filtered.** Specular anti-aliasing and normal-map filtering so the detail does not sparkle, shimmer or vanish while walking.

Ray tracing reflects and shadows whatever is there. It does not add information: it makes a perfect surface look perfectly CG, and it makes a varied surface look varied (section 10).

## 1. Normal maps: baked, then detailed

- **Baking from high poly.** The low poly gets a normal map baked from a sculpt or a high-poly model through a **cage** (an inflated copy that bounds the projection rays). The bake and the engine must share one tangent basis, or every smoothing split shows as a seam: the de facto standard is **MikkTSpace** (Mikkelsen 2008), Unity's default tangent calculation on model import (D21), and bakers (Marmoset, Substance, xNormal) are set to it (D22).
- **What it means here.** World-planar materials (walls, floors, ceilings: no keyword) build their tangent frame in the shader (`PlanarFrame`: T along the wall, B up), so a map authored in that frame needs no mesh tangents. Mesh-UV materials (`_FR_MESH_UV`: doors, props, tile trims, coping) use the mesh's tangents: the FBX kit imports with Unity's MikkTSpace, and `kitlib.py` (Blender) must export with matching tangents for any future baked map. The generators in `Tools/lookdev` write procedural normals in the surface's own UV frame (OpenGL, Y up), which is correct for both paths.
- **Detail normals.** A second, small-scale normal map tiled much finer and blended over the base. The blend matters: linear and overlay flatten; **whiteout** (`normalize(float3(n1.xy + n2.xy, n1.z * n2.z))`, 7 ALU) keeps detail; **Reoriented Normal Mapping** (RNM, `normalize(t * dot(t, u) - u * t.z)`, 8 ALU) rotates the detail onto the base and keeps both at full strength (D1, Barré-Brisebois and Hill, 2012). We already use URP's `BlendNormalRNM` for the paper's substrate.
- **Distance fade.** Detail normals fade out with distance (texel density or view distance) and their variance goes into roughness (CoD WWII "detail normal gloss", D2 §2.4), so the far surface keeps the right sheen instead of shimmering.

## 2. Height: parallax only where the silhouette does not matter

- **Parallax occlusion mapping** (Tatarchuk 2006, D10) ray-marches a height field in tangent space, with soft self-shadowing; real recesses (grout lines, panel seams, deep emboss) gain parallax and contact shadow without geometry. Its edges stay flat (the silhouette never changes), it costs 8-32 texture fetches per pixel, and it fights shallow viewing angles.
- Studios keep POM for real thickness: The Last of Us Part I's gates, Part II's rivets and panel lines (wall_material §2). For thin goods (paper, glaze waviness) they use normal maps. For a 3 mm grout line at 2-4 mm per pixel, POM pays only at the hero distance; joint depth reads better through tone and micro-shadowing (section 6), and the tile's large-scale relief through set and bow in the normal.

## 3. Masks: curvature, cavity, edge wear

- Baked **curvature** (convex edges light, concave recesses dark) and **ambient occlusion** drive edge wear and dirt: Substance's wear generators take position, curvature, AO and world-space normal as inputs, with curvature weight defining where edges wear (D23).
- For tile, the curvature mask is the cushion edge and the grout: the edge is where the glaze runs thin, chips first and catches the highlight line; the joint is where dirt, scale and water sit. Our pool maps already hold glaze thickness (G) and dirt potential (S.B), so the mask exists; what is missing is a **roughness response** to it (wear dulls the edge, dirt dulls the joint) and the same idea on every other family (no curvature or wear mask on walls, doors, desks, cove).

## 4. Repetition: per-instance, per-tile, hex tiling, macro

- **Per-instance and per-cell hashes** give each tile, plank, ceiling panel or block its own small shade, gloss and tilt (our `_FR_TILE` does this for shade and set; the paper does it per strip). The Order: 1886 is 90-95 % tiling with up to four layers, decal projectors and masks whose job is to vary tiled textures (wall_material §2).
- **Stochastic texturing** (Heitz and Neyret 2018, D8) blends three randomly offset copies of a texture on a triangle grid with a histogram-preserving operator, so a tiled texture never shows its repeat, at about 3x the fetches. **Hex-tiling** (Mikkelsen 2022, D9) drops the histogram transform and samples the source texture directly with a variance-preserving blend and random rotation per hex: three lookups, and normal maps keep their quality.
- **Macro variation**: maps at metres (ours: 8 m and 12.8 m) that modulate tone and, in better pipelines, **roughness** and relief strength, so a wall is not uniformly anything.

## 5. Roughness: the carrier of information

- Normal maps store macro-scale normals, gloss maps store the microfacet distribution; "normals become microfacet orientations when viewed from afar" (D2, Chan 2018). The rest of this section follows from that one sentence.
- A real wall, floor or glaze has a gloss that varies with use: cleaning streaks, hand height, foot traffic lanes, splash zones, sun-faded or waxed areas. In shipped PBR art the roughness map is where those stories are painted; colour changes far less (D23's generators all output roughness first).
- For ray tracing it is the whole story: a reflection ray's spread comes from roughness, and RT reflections on a constant, high smoothness are perfect mirrors (section 10).
- Our numbers (00 §2): smoothness std 0.001-0.02 on walls, carpets, block, laminate; the pool glaze 0.935 +- 0.006; scene 4's tile 0.85 exactly.

## 6. Occlusion in the small: cavity, micro-shadowing, specular occlusion

- **Micro-shadowing.** A cavity or AO map dims indirect light only, in plain PBR. Uncharted 4 applies it to direct light too ("BRDF Micro Shadowing", D12); CoD WWII auto-generates a cavity map for every normal-mapped surface and turns material occlusion into a visibility cone for direct light: `cosCone = sqrt(1 - ao)`, `vis = saturate(NdotL / cosCone)` (D2 §4, listing 3), so a groove darkens when the light grazes it and opens when the light faces it. That is exactly what a tile joint, a carpet tuft and a ceiling fissure need under our overhead lamps. Ours: `cavity` goes to `occlusion`, which URP applies to indirect light and SSAO only (wall_material §5: "Lamps never see a groove").
- **Specular occlusion.** A recess must not reflect the bright probe. GTSO (Jimenez et al. 2016, D11) derives specular occlusion from a bent normal and visibility cone; CoD WWII uses a cone-based environment-BRDF lookup with a gloss-dependent cone adjustment (D2 §4, listing 3). Ours: URP multiplies the probe's whole contribution (diffuse and specular) by the material occlusion (`GlobalIllumination.hlsl`: `return color * occlusion`), so the cavity already dims a grout line's probe reflection, but without a view-dependent cone; the lamps' highlights are never occluded.
- **SSAO** sees geometry only (our DepthNormals pass writes the geometric normal), so contact darkening appears at real geometric corners: one more reason edges and joints that matter want geometry or a micro-shadow term.

## 7. Specular anti-aliasing and normal-map filtering

- **The problem.** When a normal map mips, its normals average and shorten; if the gloss stays, the far surface turns mirror-like, and on the way it sparkles and shimmers (D2 §2: a bumpy, glossy surface becomes ever more mirror-like as it mips into the distance).
- **Toksvig** (2004, D3) widens the specular lobe by the length of the averaged normal; **LEAN** mapping (Olano and Baker 2010, D4) stores first and second moments so filtering hardware averages them correctly; **CLEAN** stores fewer; **CoD WWII** maps gloss directly to average normal length per mip at bake time, no extra channel (D2). **Kaplanyan et al. 2016** (D5) filter the NDF in screen space, stable for deferred shading and normal maps; **Tokuyoshi and Kaplanyan** (2019, 2021, D6) add geometric specular AA in projected space; Unity HDRP ships geometric specular AA as a per-material option (screen-space variance and a clamp, D7). Vlachos's GDC 2015 variant clamps roughness from ddx/ddy of the normal (via D6's survey).
- **For us.** The paper's averaged normal stays at 0.994 (Toksvig would add nothing, wall_material §5); the pool glaze is the opposite case (high gloss, 2.6-6.9 deg relief that mips away): it needs its normal variance pushed into roughness per mip in the generator (zero shader cost) plus a cheap geometric term in the shader for the tile's highlight lines at distance.

## 8. Authored decals, trim sheets, vertex-colour blends

- **Decals**: DOOM (2016) places mesh and projected decals by hand (bolts, grilles, stains) that alter normal, roughness and colour, drawn in the same clustered loop as its lights (D13); tech_art row 10 proposes URP DBuffer decals in `FrontRooms/Surface` (desktop DBuffer, phones and WebGL screen-space) to replace shader-drawn wear bands with placed wear (Red's "staged assets, not shader tricks", VISUAL_STANDARD 1.1).
- **Trim sheets**: Insomniac's Ultimate Trim (Olsen, GDC 2015, D14) lays a sheet of strips with fake bevels in the normal map, so architecture and props get high-poly-looking edges without high poly; tech_art row 14 (cove, casing, T-bar, chair rail).
- **Vertex-colour and mask blends**: The Last of Us Part I's RGB vertex-colour texture sets, The Order's height-aware blends (wall_material §2). The map builds its own meshes, so a per-vertex channel (damp, grime, age) is available to it at no texture cost.

## 9. Bevels: geometry or normal map

- Real edges are rounded: a pressed tile's cushion edge (D33), a drywall corner bead, a painted door's arris, a laminate desk's edge band. The round catches a thin highlight line on one side and a soft shade on the other; that line is how the eye reads "edge" in soft overhead light.
- Shipped practice: **geometric bevels** (with face-weighted normals) where the silhouette or a highlight on the edge matters and the vertex cost is affordable (mid-poly modelling, D24); **normal-map bevels** (Ultimate Trim, D14) where only the face's shading matters. A normal-map bevel never changes the silhouette, the shadow or the RT reflection of the edge's shape.
- Ours: the props are bevelled 3-4 mm in geometry (`kitlib.py`); the architecture is not bevelled at all (00 §2): every wall corner, column, arch reveal, door reveal, step and pool edge is a knife edge. Cheapest fix with the largest effect: a 3-10 mm chamfer or round on the map's exposed corners (a few hundred triangles per room, one renderer each as today), and an edge-highlight term for the remaining ones.

## 10. What ray tracing changes

1. **RT reflections reflect the shading normal and the roughness.** A reflection ray leaves along the normal-mapped normal and spreads by roughness (Stachowiak 2015 SSSR, D15; SEED's PICA PICA hybrid pipeline traces reflections from arbitrary normal-mapped normals and varying roughness, with spatiotemporal filtering, D16). So the waviness, set and lippage in a tile's normal map bend a reflected lamp exactly as real tile does, and roughness variation is what keeps RT reflections from being perfect mirrors. Engines also stop tracing rough pixels (Lumen traces below roughness 0.4 by default, D20), so the roughness map decides where RT even acts.
2. **RT shadows and AO see geometry.** A normal-mapped bump never shadows itself in a traced shadow; only geometry, POM or a micro-shadow term gives contact detail. Shading normals that disagree with the geometry also produce a hard **shadow terminator** on low poly (Chiang, Li and Burley 2019, D17; Hanika's offset of the shadow-ray origin along the interpolated normal, Ray Tracing Gems II ch. 4, D18).
3. **Ray offsets use the geometric surface.** New rays start from the geometric hit, pushed off the triangle's plane (Wächter and Binder 2019, D19): a crisp box edge reflects and shadows as a crisp box edge, whatever its normal map says.
4. **So, for RT to look right:** the information has to be in roughness and in the normal at scales the reflection can show (millimetres to decimetres), the edges that matter have to exist in geometry, and the detail must be filtered (section 7) so the denoiser and the eye do not see noise as sparkle.
5. **Ours.** GlassRT traces mirror rays from glass only; its hit shading of `FrontRooms/Surface` uses the base map, mask smoothness and the normal map (`FrontRoomsGlassRT.metal` lines 278-354), so improved surfaces show in RT glass too, provided the hit shader learns any new term (and it lacks `_FR_TILE` today, 00 §3). The water's planar mirror on desktop is the most "RT-looking" path Red sees in the pool rooms, and it mirrors the tile's knife edges and flat faces in full.

## 11. POOLS (Tensori, 2024): how its tiled rooms read

**What it is** (D25-D29, `17_water.md` §10.2): Unity, with URP and HDRP builds and no Asset Store plugins (D26); the developer confirms baked lighting, with screen-space GI as the most expensive setting (Steam, 2024-04-12, D25); light probes and adaptive probe volumes, reflection probes, post-processing. **No real-time ray tracing.** The developer adds that realistic lighting took far more effort in Unity than it would in Unreal (D25). The look is reachable on phones (it ships on iPhone, iPad, Switch and PS VR2).

**What its tile does** (13 store frames looked at 1:1, D28; ids by the frame's hash):
- **Light breaks tile by tile.** On the floor of the white hall (`faeeaedd`) the reflected ceiling lamp is not one shape: it is a mosaic of tile-sized patches, each bright at a slightly different place, some tiles inside the reflection nearly dark. On a wall seen at a grazing angle (`cd0d56ba`) a window's reflection becomes columns of per-tile patches, each soft-edged within its tile. That needs per-tile tilt and bow large enough to move a reflection by a tile or more at 3-5 m: on the order of +-0.3-0.6 deg (2 x tilt x 4 m = 40-80 mm of reflection shift), about twice our 0.18 deg.
- **The set-out is hand-made.** On columns at 0.5-1 m (`919d1f55`, `b1d6a882`) joint lines wobble and vary in width; rows do not quite line up; joints are mid-grey, a little recessed, never ruled lines.
- **Tone varies per tile, a little on white, a lot on colour.** White field tiles step slightly in tone; the teal mosaic band is a blend of several glaze colours (dark teal to light aqua) tile by tile.
- **The gloss is high but not a mirror.** Reflections inside one tile are soft at the edges: roughness well above a polished mirror, and different from tile to tile.
- **Soft, bounced light does the rest.** Baked GI and SSGI give every wall a gradient and every corner its dark; the camera adds bloom on glints, grain and chromatic aberration (17 §10.2).

**What the glaze line found** (`W/level_space/drafts/glaze/ref_pools.md`, `ref_physics.md`, `ref_engine.md`, `design.md` §1, 11-04; reused, not redone): the shimmer on POOLS' walls is the lamps mirrored in the glaze and broken tile by tile, not light thrown by the water; a wall needs lamp images 50-200x its own luminance to show it (our lenses are 2-5x); our Surface shader never box-projects its probe, so the images sit at infinity and slide; an 8-bit normal map steps in 0.45 deg, so sub-step tilts must come from the shader; real walls set at about 0.15-0.3 deg sd.

**Lesson for us.** POOLS' tile reads real because each tile is a slightly different mirror, set by a hand, under soft bounced light, filmed by a camera. Three of those are surface data (set, bow and gloss per tile; joints with width and wobble; tone per tile), one is light (GI), one is the camera. None of it needs ray tracing; all of it is visible in a planar or RT reflection when it exists.

## 12. Real pool tile, in numbers

| Property | Value | Source |
|---|---|---|
| Field tile | 150 mm (6 in) and 108 mm (4 1/4 in) the period's commonest; 1 x 1 in mosaic on pool shells | 70 §4.1 T1-T6 |
| Joints | 3 mm (1/8 in) wall, 2 mm mosaic, 4 mm deck; grout struck slightly concave below the cushion | 70 §4.1 T1; D33 (cushion edges leave the grout line slightly recessed) |
| Warpage, curvature (glazed wall tile, BIII) | about +-0.3 to +-0.5 % of the diagonal / edge (secondary sheets disagree: +-0.3/0.5 vs +-0.40); 150 mm tile, 212 mm diagonal: 0.6-1.1 mm | D31 |
| ANSI A137.1 wall tile | wedging +-0.25 %; diagonal warpage -0.30 / +0.40 % (manufacturer sheets citing it) | D32 |
| Lippage allowed | joints 3 to < 6 mm: 1 mm plus the tile's inherent warpage; >= 6 mm: 2 mm plus warpage (ANSI A108.02-2023 §4.4); side light shadows the grout joint and makes tile look more uneven than it measures | D30 |
| Cushion edge | pressed (non-rectified) tile, edges curve into a slightly recessed joint; hides small lippage | D33 |
| Glaze surface | orange peel = finely dimpled glaze surface (D35); appearance metrology splits it into short wave 0.3-1.2 mm (read at about 30 cm) and long wave 1.2-12 mm (read at 2-3 m); structure below 0.3 mm sets distinctness of image (D34) | D34, D35 |
| Gloss | no gloss-unit figure for period wall tile found; the smoothness spread below is a judgement made against POOLS' frames | - |
| Waterline | a white calcium line where evaporation concentrates minerals, grey silicate scale, body-oil grime, scale wicking into grout; a falling level leaves a scum ring (TM 5-662) | 70 §4.1 T2, T9 |

**Translated to the screen** (1080p, 76 deg; 4.3 mm per pixel at 3 m): a 0.6-1.1 mm bow or warpage on a 150 mm tile tilts its faces by 0.3-0.6 deg at the edges, which moves a reflected lamp 40-80 mm at 4 m: visible as the per-tile break POOLS shows. 1 mm lippage is a sub-pixel step but a visible highlight or shade line under side light. Orange peel at 0.3-1.2 mm is invisible as relief past 1 m; it must be in the gloss (a broader, mottled lobe), and the long wave (1.2-12 mm) is what wobbles a reflection at 2-3 m.

## 13. What follows for our rooms (input to `20_design.md`)

1. Put **roughness variation** into every family, from causes (use, cleaning, splash, traffic, sun), at two scales: per element (tile, panel, strip, plank) and per metre (macro).
2. **Filter**: generator mips that push normal variance into roughness for every map; a geometric specular-AA term in the shader for high-gloss materials (tile glaze, VCT wax, chrome, the water's walls).
3. **Micro-shadowing** on direct light from the cavity we already have (CoD WWII's cone, a few ALU), and **specular occlusion** from the same cavity.
4. **Edges**: geometric chamfers on the map's exposed corners and on pool edges, steps and coping; an edge-highlight term for joints and reveals the map cannot bevel.
5. **Detail normals** with RNM and distance fade for walls, carpets and ceilings at hero distance; their variance into roughness far away.
6. **Break the grid**: per-tile set and bow bigger (POOLS), joint width and wobble per row and column, multi-tone glaze lots; hex-tiling or a second macro map on carpets and ceilings.
7. **Placed wear** (tech_art row 10 decals) for stains, tide marks, scuffs; trim sheets (row 14) for cove, casing and chair rail with real bevel normals.
8. **Make RT see it**: teach GlassRT's hit shader every new term (and `_FR_TILE`), keep the water's planar pass on the same shader, and give the pool's reflections a varied surface to reflect.
