# Sources: Level 0 wall material research

Collected by workflow `wf_c56b2d96-a47` (2026-10-30, read-only, nothing downloaded). 19 of 24 spot-checked claims held up as written or with a correction; corrected wording is in `10_research.md`. `kind` is the source type: primary, secondary or forum. Reference only: no image, scan or text from these sources ships.

## AAA and high-end games

| Claim | Kind | Source |
|---|---|---|
| The Order: 1886 environments were 90-95% tiling materials with up to 4 runtime layers blended by vertex colour plus height maps, extended by runtime decal projectors. | primary | https://history.siggraph.org/wp-content/uploads/2022/10/2015-Talks-Phail-Liff_Crafting-Victorian-London-The-Environment-Art-and-Material-Pipelines-of-The-Order-1886.pdf |
| The Order's runtime layering (up to 4 layers, alpha from vertex colours, blend maps or runtime maps) exists specifically to break up large tiled surfaces; offline comp… | primary | https://blog.selfshadow.com/publications/s2013-shading-course/rad/s2013_pbs_rad_notes.pdf |
| Gears of War 4 built a layered material system in UE4 where artists stack many library materials with masks/blend modes, then cook the result for 60 fps. | primary | https://history.siggraph.org/learning/gears-of-war-4-creating-a-layered-material-system-for-60fps |
| Naughty Dog (TLOU Part I) blends texture sets by painting the RGB channels of vertex colour, e.g. to add cracks, dirt and wetness. | primary | https://www.adobe.com/products/substance3d/magazine/designing-the-last-of-us-3d-materials-and-shaders.html |
| Naughty Dog uses height map + parallax occlusion mapping only where real thickness must be faked (ornate gate), not for flat surfaces. | primary | https://www.adobe.com/products/substance3d/magazine/designing-the-last-of-us-3d-materials-and-shaders.html |
| TLOU Part II vehicle art used POM decals for small relief (rivets, vents, panel lines) on top of mask-based layered shaders. | primary | https://trevelyan.artstation.com/projects/ELL4gv |
| Alan Wake 2 environment materials were combined in Northlight's in-engine layered system using height data and painted masks. | primary | https://blog.adobe.com/en/publish/2024/08/07/remedy-entertainments-environment-art-technology-workflow-alan-wake-2 |
| Control's tileable architecture materials were all authored at a fixed 4 x 4 m scale, with heightmaps built from a mid-grey baseline so they blend together. | primary | https://www.adobe.com/products/substance3d/magazine/control-how-one-senior-environment-artist-created-entire-brutalist-material-library.html |
| Arkane explicitly avoided walls that read as regular grids, raised texel density for Dishonored 2, and kept a reusable library of moisture, mold, fingerprint and foots… | primary | https://80.lv/articles/asset-material-production-in-dishonored-2/ |
| Hitman 2 interiors relied on decals, edge decals and alpha blending, with strict texel density. | primary | https://80.lv/articles/environment-production-for-hitman-2 |
| IOI's trim materials are layered stacks (base, peeling paint layers, dirt), built from a shared base-material library. | primary | https://magazine.substance3d.com/mood-and-realism-in-hitman-3/ |
| The 'Ultimate Trim' technique (standardised trim layout plus normal-map bevel structure, UV script, variation shader) originated at Insomniac for speed, memory and per… | primary | https://gdcvault.com/play/1022323/The-Ultimate-Trim-Texturing-Techniques |
| RE7's RE Engine pipeline was photogrammetry-heavy (a little over half of assets) and included scouting real Southern US houses, but no source found describes how the B… | secondary | https://80.lv/articles/resident-evil-7-the-use-of-photogrammetry-for-vr/ |
| Naughty Dog's 'BRDF micro shadowing' (Uncharted 4) shades direct light using the material's AO and normal maps, so small relief reads without geometry; Unity HDRP ship… | primary | https://docs.unity3d.com/Packages/hdrp/manual/Override-Micro-Shadows.html |
| Call of Duty: WWII auto-generated cavity maps from every normal map and used them for occlusion/indirect lighting in micro-cavities, and tied gloss to average normal l… | primary | https://research.activision.com/publications/archives/material-advances-in-call-of-dutywwii |
| Doom 2016 applies decals inside the clustered forward pass from a texture atlas, with up to 256 decals per cluster. | secondary | https://www.adriancourreges.com/blog/2016/09/09/doom-2016-graphics-study/ |
| Doom Eternal's renderer budgets decals in the hundreds of thousands per level. | primary | https://advances.realtimerendering.com/s2020/RenderingDoomEternal.pdf |
| URP decals: the DBuffer technique writes Albedo, Albedo+Normal, or Albedo+Normal+MAOS before opaque shading; Screen Space applies after opaques with normals reconstruc… | primary | https://docs.unity3d.com/Manual/urp/renderer-feature-decal-reference.html |
| URP decals do not use the SRP Batcher; batching relies on GPU instancing and atlasing decal textures with projector UV offsets. | primary | https://docs.unity.com/en-us/engine/6000.0/manual/visual-effects/decals/renderer-feature-decal/decal |
| URP Lit's Detail Inputs overlay a higher-frequency albedo and normal through a mask stored in alpha; Tiling/Offset do not affect the mask. | primary | https://docs.unity.com/en-us/engine/6000.0/manual/materials-and-shaders/built-in/shaders-in-universalrp/reference/lit-shader |
| Unity HDRP Layered Lit offers up to four layers with a layer mask, vertex colour modes (None/Multiply/AddSubstract), optional height-based blending with a transition w… | primary | https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition%4015.0/manual/layered-lit-tessellation-shader.html |
| Unreal's documented vertex-paint blend uses vertex colour as the Lerp alpha, with up to 4 (ARGB) or 5 (ARGB-1) textures. | primary | https://dev.epicgames.com/documentation/en-us/unreal-engine/setting-up-a-texture-blended-material-for-vertex-weights-painting-in-unreal-engine |
| Unreal's Detail Texturing layers a heavily repeated diffuse+normal over the base maps for close range, but only hides pixelation to a point and adds texture lookups. | primary | https://dev.epicgames.com/documentation/en-us/unreal-engine/adding-detail-textures-to-unreal-engine-materials |
| UE5 DBuffer decals store base colour, normal and roughness before the base pass; cost scales with screen coverage and material complexity, and receiving materials shou… | primary | https://dev.epicgames.com/documentation/en-us/unreal-engine/decal-materials-in-unreal-engine |
| Unreal mesh decals (decal-material geometry floating over the surface) wrap around edges and give feathered soft transitions that projected or masked decals cannot. | primary | https://dev.epicgames.com/documentation/en-us/unreal-engine/using-mesh-decals-in-unreal-engine |
| Peeling paint and wallpaper in games is commonly built as modelled alpha cards / floating planes over a decal base layer. | forum | https://polycount.com/discussion/comment/1966308 |
| Unreal WorldAlignedTexture tiles in world units independent of mesh size/rotation, but textures 'swim' on moving objects. | primary | https://dev.epicgames.com/documentation/en-us/unreal-engine/texturing-material-functions-in-unreal-engine |
| Epic's own POM documentation calls the technique expensive, step-limited and inaccurate at oblique angles. | primary | https://docs.unrealengine.com/udk/Three/DevelopmentKitGemsParallaxOccludedMapping.html |
| Height-based splat blending: add each layer's height to its weight and blend only within a 'depth' band below the max, so transitions follow texture relief. | primary | https://www.gamedeveloper.com/programming/advanced-terrain-texture-splatting |
| Texture bombing scatters small images at irregular intervals on a cell grid to hide repetition. | primary | https://developer.nvidia.com/gpugems/gpugems/part-iii-materials/chapter-20-texture-bombing |
| Unity's Mikkelsen published a practical hex-tiling adaptation of Heitz-Neyret by-example noise, aimed especially at normal maps, that samples the original texture with… | primary | https://jcgt.org/published/0011/03/05/paper-lowres.pdf |
| Commercial vinyl install rules: rolls hung in reverse sequence, textured or random patterns reverse-hung on alternate strips and double-cut, joints kept 6 in from corn… | primary | https://cdn2.wolfgordon.com/production/documents/Vinyl_Install.pdf |
| Seams become visible through shading and sheen differences rather than lines: out-of-sequence strips shade at the seam, and adhesive residue leaves a dull area. | primary | https://koroseal.com/getmedia/60b2af67-1c75-47c5-bab0-c26fc450c292/WC-HANGING_INSTRUCTIONS_NONREVERSIBLE |
| Burnishing seams with a roller changes their gloss and makes them noticeable. | primary | https://carnegiefabrics.com/media/catalog/product/download/Wood-Look-Wallcovering-Installation.pdf |
| Type II commercial vinyl comes about 54-55 in wide and is designed to be overlapped and double-cut, with random textures reversed strip by strip. | primary | https://wallquest.com/images/uploaded/TypeII_PTS.pdf |
| A representative commercial vinyl is 54 in wide, 20 oz/yd, canvas-embossed, with about 9 GU gloss at 60°: low sheen, not dead-flat paper. | primary | https://www.surfacematerials.com/cdn/shop/files/Level_PNSLightWeightPositionable_ProductSpecs_2026.pdf |
| Commercial vinyl wallcovering weight classes: minimum total weight 7, 13 and 22 oz/yd² for Type I, II, III. | primary | https://cdn.ymaws.com/www.wallcoverings.org/resource/resmgr/WA_W-101_2025_Final_.pdf |
| US residential roll widths are usually 20.5 in (sometimes 27 in), i.e. about 0.52 m or 0.686 m strips, not 0.75 m. | secondary | https://tothesource.com/glossary-of-terms/2021/01/19/american-single-roll |
| Curling edges and open seams come from moisture (the backing expands faster than the face) and from re-pasting seams. | secondary | https://www.resene.co.nz/homeown/probsolv/wallpaper/curling-edges.htm |
| Conservation surveys of real aged wallpaper find losses from furniture abrasion and water staining concentrated at corners. | primary | https://cool.culturalheritage.org/jaic/articles/jaic20-02-008_2.html |
| A UE4 interior artist made a tiled wallpaper read as real paper by adding seams (plus dirt) so it looked like individual strips, and used decals for the ghosts of remo… | primary | https://80.lv/articles/creating-an-abandoned-home-interior-in-ue4 |
| First-person texel density is commonly set around 1024 px/m (10.24 px/cm). | secondary | https://help.maxon.net/c4d/2026/en-us/Content/html/Texel_Density.html |

## Real 1985–1993 office wallcovering

| Claim | Kind | Source |
|---|---|---|
| US commercial wallcovering classes: ASTM F793 Categories I-VI; Categories IV/V/VI = Type I/II/III vinyl-coated; minimum weights 7/13/22 oz/sq yd excluding coatings; wi… | primary | https://online2.ogs.ny.gov/dnc/masterspec24/docs/Division09Finishes/097200.0WallCoverings.docx |
| Type II (20-32 oz per 54 in linear yard) is the most widely specified commercial class; Type I (<=19 oz) is used in office buildings and hotel rooms; Type III (>33 oz)… | primary | https://www.wolfgordon.com/resources/wallcovering-101 |
| CCC-W-408D definitions: Type I light duty on scrim or non-woven (min 7 oz/sq yd); Type II medium duty on osnaburg, drill or non-woven (min 13 oz/sq yd). Glossary defin… | primary | https://wolfgordon.com/resources/wallcovering-glossary |
| CCC-W-408D requirement table: lightfastness 200 fading hours (carbon arc) for all types; scrubbability 200/300/500 cycles; abrasion 200/300/1000; maximum shrinkage 2%… | primary | https://texdecor.com/assets/pdf/tech-docs/FR/PVFEU-LINO-FR.pdf |
| A real Type II office vinyl: 54 in wide, 13.0 oz/sq yd = 20.3 oz per linear yard, 'Linen2' emboss, non-woven backing 1.8 oz/sq yd, average total thickness 16 mils (0.4… | primary | https://cdn.shopify.com/s/files/1/0441/9226/8445/files/Homestead_Spec_Data.pdf |
| Sheen: a 20 oz, 54 in commercial vinyl with a 'Sand' emboss measures 6.4 gloss units at 60 degrees (matte). The VA federal spec requires a factory-applied clear delust… | secondary | https://www.lexjet.com/vescom-sand-20oz-non-woven |
| Deep-textured commercial vinyl (1978-80 patent): embossed foamed PVC plastisol outer layer 30-70 mils over a 4-6 mil scrim and a 2-3 mil film, for commercial and indus… | primary | https://patents.google.com/patent/US4196244A/en |
| Paste-the-product embossed wallcoverings reach emboss depths over about 0.5 mm (patent background, 1987-1990). | primary | https://patents.google.com/patent/US4894274A/en |
| American single roll: usually 20.5 in wide (up to 36 in), 4-7 yd long, 34-36 sq ft. | secondary | https://tothesource.com/glossary-of-terms/2021/01/19/american-single-roll |
| How commercial vinyl seams are made: matched patterns are butted; textured or random patterns are reverse-hung, overlapped and double-cut over a 2-3 in protective stri… | primary | https://cdn2.wolfgordon.com/production/documents/Vinyl_Install.pdf |
| Double-cut overlap on 54 in commercial goods is about 2 in, cut down the middle of the double thickness. | primary | https://koroseal.com/getmedia/4cdd6ebc-bfd6-450a-bc1a-17f53c39d5c8/wc-54_inch_mylar_laminated_wallcovering |
| Deep-embossed vinyl is butted, not double-cut, and its edges are trimmed at the bottom of a groove. At corners it is trimmed to 16-20 in and wrapped at least 8 in each… | primary | https://cdn2.wolfgordon.com/production/product/documents/DeepEmboss_Vinyl_Install.pdf |
| Federal spec rules: no horizontal seams; reverse alternate strips except on match patterns; overlapped seams not allowed; no seam within 2 in of inside or 6 in of outs… | primary | https://www.wbdg.org/FFC/VA/VAASC/VA%2009%2072%2016.pdf |
| NY master spec execution: match the pattern 72 in above the finished floor; seams vertical and plumb, at least 6 in from outside and 3 in from inside corners; butt sea… | primary | https://online2.ogs.ny.gov/dnc/masterspec24/docs/Division09Finishes/097200.0WallCoverings.docx |
| Near-period (May 1997) university spec: only vertical joints; double-cut with a straightedge; rotate alternate sheets so the same roll edge meets the same edge, to avo… | primary | https://dc.ufs.arizona.edu/sites/default/files/2025-05/Section-09950-Wall-Coverings-05.97.pdf |
| Seam artifacts from installation: adhesive left on the face 'may discolor or leave a dull area at the seam'; drywall joint compound against the face paper shows 'stron… | primary | https://koroseal.com/getmedia/60b2af67-1c75-47c5-bab0-c26fc450c292/WC-HANGING_INSTRUCTIONS_NONREVERSIBLE |
| Shading: 'edgeshade' is a colour change from one side of a strip to the other, plus roll-to-roll variation within a batch; most visible looking along a wall; reverse h… | primary | https://www.resene.co.nz/homeown/problem-solver/wallpaper/shading.htm |
| Industry acceptance standard: shading = variation in color, texture or gloss within one strip; judged from at least 39 in (1 m) at eye level under the finished lightin… | primary | https://www.pcapainted.org/wp-content/uploads/2022/01/PCA-P6.pdf |
| Paper-backed wallcoverings expand 1-2% while relaxing after pasting; too little relaxing, stretching or forcing seams together makes joins peak, and drying shrinkage o… | primary | https://www.resene.co.nz/homeown/problem-solver/wallpaper/peaking-joins.htm |
| Seam line colors: on dark grounds the cut edges show as a white line; adhesive forced through the join by a seam roller can tear ink off, leaving white marks; too much… | primary | https://www.resene.co.nz/homeown/problem-solver/wallpaper/white-edge.htm |
| An aged paper-backed vinyl seam had opened 1/8 in and gone brittle; the vinyl dried out, shrank and pulled the paint off the drywall at the seam. | primary | https://wallpaperinstaller.com/seam_repair.html |
| Period case (1989-1992 Florida hotels): in the first summer, vinyl wallcovering peeled and mold grew at the vinyl/gypsum interface, concentrated where partitions meet… | primary | https://web.ornl.gov/sci/buildings/conf-archive/1992%20B5%20papers/070.pdf |
| Vinyl wallcovering is a vapor retarder: Type I 5 oz about 4 perms; Type I 12 oz about 1 perm; Type II 20 oz under 1 perm (ASTM E96 wet cup). Hotel mold damage on inter… | primary | https://www.buildingscience.com/documents/building-science-insights/bsi-130-hotel-mold |
| Mold under wallcoverings shows as spots of grey, green, blue, black, pink and yellow; black spots usually toward the ceiling. | primary | https://www.resene.co.nz/homeown/problem-solver/wallpaper/mould.htm |
| Water stains come through wallcovering as a lighter area with a dark brown perimeter line; a stain that crosses a seam comes from the wall, not the wallcovering. | primary | https://www.resene.co.nz/homeown/problem-solver/wallpaper/bleed-through-ghosting.htm |
| Tide lines on cellulose: dirt and degradation products carried by spreading water are deposited at the wet/dry edge; color ranges from barely visible tan to coffee brown. | primary | https://cool.culturalheritage.org/jaic/articles/jaic22-02-001_1.html |
| Lignin-containing paper yellows, and UV from sunlight or even indoor fluorescent lighting speeds it up. | secondary | https://www.printing.org/content/2018/03/28/what-causes-paper-to-yellow-over-time |
| 80-90% of combusted cigarette nicotine adsorbs to surfaces; residue on walls and drywall is hard to remove. | primary | https://pmc.ncbi.nlm.nih.gov/articles/PMC4765971 |
| Cleaning wear on vinyl: abrasive cleaners 'mar and burnish' the surface; dirt lodges in the crevices of deeply textured patterns; walls are washed bottom-up to prevent… | primary | https://arc-com.com/pdf/WC-Cleaning-Guide.pdf |
| Office-chair contact heights (derived): BIFMA G1-2002 minimum seat-height range 15-19.9 in and armrest 6.9-10.8 in above the seat put arm tops about 22-31 in (0.55-0.7… | secondary | https://knoll.com/document/1352940926523 |
| Electrical boxes are air-leak paths; dust carried by leaking air leaves dark 'ghosting' marks around outlets and switch plates. | secondary | https://www.greenbuildingadvisor.com/article/air-leakage-at-electrical-switches-and-outlets |
| Period context: commercial vinyl wallcovering was established practice well before 1985 — a 1977 Construction Specifier guide; real vinyl sample books from 1986 (Venet… | secondary | https://firedoc.nist.gov/article/UXcyXYQBWEcjUZEYM1oE |
| Real photos of the original Backrooms rooms (yellow wallpapered upper floor of the former Rohner's furniture store, Oshkosh WI, photographed 2002/03 during the HobbyTo… | primary | https://archive.org/details/backrooms-photos |
| More real aging photo sources: peeling wallpaper (Wikimedia Commons category, 7 files, residential/abandoned); mold found behind newly installed vinyl wallcovering in… | secondary | https://commons.wikimedia.org/wiki/Category:Peeling_wallpaper |
| Emboss names on current commercial and residential vinyl include linen, sand, sisal/grasscloth and suede. | secondary | https://www.lexjet.com/dreamscape-bondi-sands-20oz |

## Backrooms-genre and horror references

| Claim | Kind | Source |
|---|---|---|
| The founding Backrooms text names smell/moisture of carpet, the 'mono-yellow' and fluorescent hum as the core traits, not the wallpaper's pattern. | secondary | https://en.wikipedia.org/wiki/The_Backrooms |
| The source photo's wallpaper was reportedly a beige chevron pattern (Borden Home Wallcoverings); the film's production designer says the yellow comes from bad white ba… | secondary | https://galeriemagazine.com/how-horror-hit-backrooms-terrorizes-viewers-with-design/ |
| Vermette: the real room was probably beige, and the target was a set that 'still feels like a Blender rendering'. | primary | https://www.motionpictures.org/2026/06/how-production-designer-danny-vermette-made-backrooms-real-portals-platforms-practical-terror/ |
| Kane Parsons describes the look he is evoking as camera-flash, gross lighting and off white balance on yellow walls. | primary | https://vice.com/en/article/is-the-backrooms-real |
| The A24 film tested up to 50 yellow chevron variations so the pattern would not striate (moiré) on camera across large wall areas. | primary | https://www.dezeen.com/2026/05/29/backrooms-production-design-danny-vermette-interview/ |
| The film's wallpaper printer needed 30–40 colour iterations, because the yellow read differently in-house, on screen and on camera under the set lighting. | primary | https://dailyhive.com/vancouver/vancouver-backrooms-wallpaper |
| The film's carpet was chosen for visual distortion, tonality and pile, and was aged and painted; two carpet tones transition along a hallway. | primary | https://galeriemagazine.com/how-horror-hit-backrooms-terrorizes-viewers-with-design/ |
| The A24 Backrooms wallpaper sold to the public comes as self-adhesive yellow panels 30 inches wide, about the same width as FrontRooms' 0.75 m roll tile. | secondary | https://www.brandsuntapped.com/a24-launches-backrooms-wallpaper/ |
| Kane's realism is credited largely to the VHS distortion supplying 'implied detail', not to surface modelling. | secondary | https://aaronordover.substack.com/p/implied-detail |
| Liminal 'real photo' renders use camera-emulation artefacts (soft focus, then sharpening, JPEG compression, bloom) to read as early-2000s photos. | secondary | https://80.lv/articles/blender-artist-uses-camera-tricks-to-create-a-liminal-dreamcore-scene |
| The Exit 8 chose realistic graphics on purpose and built the corridor from real-life examples, with a wide-angle lens and camera shake. | secondary | https://automaton-media.com/en/news/20240214-27192 |
| The Exit 8 / Platform 8 developer relies on bought assets, including photogrammetric 3D models. | primary | https://automaton-media.com/en/interviews/interview-the-exit-8-developer-kotake-create-on-the-perks-of-being-a-solo-dev-and-how-platform-8-came-to-be/ |
| The Fox Engine (later used for P.T.) got photorealism from lighting-neutral textures, measured real-world material parameters and linear rendering; it was benchmarked… | secondary | https://game.watch.impress.co.jp/docs/news/593640.html |
| A fan rebuild of the P.T. hallway matched its look with crack and dirt decals, and lighting that is mostly direct with little indirect. | secondary | https://www.gamedeveloper.com/production/i-recreated-the-pt-hallway-in-unity |
| RE7 built a little over half its assets with photogrammetry, including location shoots in the Southern USA. The team stressed realism because the first-person view let… | secondary | https://80.lv/articles/resident-evil-7-the-use-of-photogrammetry-for-vr/ |
| Remedy authored Alan Wake 2 materials slightly wetter than normal, as a cue players sense without noticing. | primary | https://blog.adobe.com/en/publish/2024/08/07/remedy-entertainments-environment-art-technology-workflow-alan-wake-2 |
| Capcom's Resident Evil 2 (2018) team named 'wetness' and 'darkness' as its graphic keywords. | secondary | https://wccftech.com/resident-evil-2-graphics-movie-like/amp/ |
| Photo-sourced surfaces read as real because wear and dirt settle in structured, non-random places the brain notices. | primary | https://www.theastronauts.com/2014/03/visual-revolution-vanishing-ethan-carter/ |
| Trade guidance: any sheen above matte rakes light sideways and reveals every seam and dimple; flat finishes make a wall read as one even surface. | secondary | https://compositepaint.com/guides/paintable-wallpaper/ |
| Common hobby Backrooms builds use a plain photo wallpaper texture with box projection, with no seams or wear modelled. | secondary | https://80.lv/articles/blender-guru-shares-how-to-create-the-backrooms-using-blender |
| Community-standard Level 0 trait list: yellowed or worn mono-yellow wallpaper, damp or moist carpet, electrical outlets, inconsistently placed fluorescents. | forum | https://store.epicgames.com/p/the-backroom-project-b6b434?lang=en-US |
| In later Kane episodes, wallpaper is deliberately shown with texture-mapping errors (stretched like a broken UV map) as a sign of degraded space. | secondary | https://taylorholmes.com/2026/06/02/explaining-the-backrooms-youtube-episodes-18-22/ |
| Escape the Backrooms markets 'realistic graphics' but publishes no material detail. | primary | https://store.steampowered.com/app/1943950/Escape_the_Backrooms/ |
| POOLS began from an artist with an architectural-visualisation background, an archviz realism lineage rather than game-texture conventions. | secondary | https://backloggd.com/company/tensori/ |
| Silent Hill 2 remake's only first-party environment-art talk found covers the rusty, organic Otherworld, not apartment wallpaper. | primary | https://forums.unrealengine.com/t/talks-and-demos-creating-the-otherworld-of-silent-hill-2-unreal-fest-orlando-2025/2672459 |

## Audit of our own wall (web sources only)

| Claim | Kind | Source |
|---|---|---|
| Commercial Type II vinyl is sold 52-54 in wide and hung reverse, random match | secondary | https://www.designerwallcoverings.com/products/dwk-30709-koroseal-koroseal-type-2-vinyl-2d21-18-destiny-mineral |
| Real wallcoverings shade differently at strip edges; installers reverse-hang strips to manage it | secondary | https://www.wallcover.com/blog/guide-reverse-hanging-wallpaper |
| Mipmapping a normal map averages bumps away, so distant surfaces look glossier than they should | primary | https://blog.selfshadow.com/2011/07/22/specular-showdown/ |
| Wallpaper damage breakdowns treat seams as a damage category of their own | secondary | https://80.lv/articles/creating-peeled-wallpaper-with-custom-made-houdini-system |
| US federal wallcovering specs set rules for wrapping corners and placing seams that a world-grid seam cannot follow | primary | https://www.wbdg.org/FFC/VA/VAASC/VA 09 72 16.pdf |

## Sheets and seams (second pass, 2026-10-30)

| Topic | Kind | Source |
|---|---|---|
| Lap widths, butt and double-cut history, filling back, trimming | secondary (Kelly 2011) | https://www.slideshare.net/RobertMKelly/historic-paperhanging-techniques-a-bibliographic-essay |
| Wire lap, seam behaviour | secondary, undated | https://www.millardlumber.com/HowTo/wpaper/howwall.htm |
| Pattern matching, stretch, expansion | primary (Resene) | https://www.resene.co.nz/homeown/probsolv/wallpaper/pattern-matching.htm |
| Opening seams | primary (Resene) | https://www.resene.co.nz/homeown/problem-solver/wallpaper/opening-seams-joins.htm |
| Trimming errors | primary (Resene) | https://www.resene.co.nz/homeown/probsolv/wallpaper/trimming-errors.htm |
| Out of square walls | primary (Resene) | https://www.resene.co.nz/homeown/probsolv/wallpaper/out-of-square.htm |
| Lapped seams, white edge, curling | primary (Resene) | https://www.resene.co.nz/homeown/probsolv/wallpaper/lapped-seams.htm |
| Bias-printed stock, defects | secondary | https://wallpaperinstaller.com/wallpaper_defects.html |
| Drop match, basic technique | secondary | https://www.finehomebuilding.com/project-guides/painting/basic-wallpapering-techniques |
| Contract mismatch allowance (snippet) | primary | https://cdn2.wolfgordon.com/production/documents/Digital_Install.pdf |
| Pattern repeats and matches, no horizontal seams | secondary | https://thatcherstudio.com/pages/pattern-repeats-and-pattern-matches-commercial-wallcovering |
| Terminus Hotel room, gaps and flaps | photo | https://lostcollective.com/products/terminus-hotel-wallpapered-guest-room |
| Conservation history, discolouring | secondary | https://www.wallpaperhistorysociety.org.uk/conservation-history |
| Dirty wallpaper, seam grime | secondary | https://www.moneypit.com/dirty-wallpaper-remove-or-clean/ |
| Chevron alignment (analogy, flooring) | secondary | https://mrsander.co.uk/articles/floor-sanding-articles/chevron-install-the-three-alignment-checks-that-matter/ |
| Peeled wallpaper as a damage class | secondary | https://80.lv/articles/creating-peeled-wallpaper-with-custom-made-houdini-system |

## Surface textures (second pass, 2026-10-30)

| Topic | Kind | Source |
|---|---|---|
| Type II thickness across embosses | primary | https://dreamscapewalls.freshdesk.com/support/solutions/articles/12000054876-dreamscape-20-ounce-type-ii-spec-sheet |
| Vinyl and backing layers, spanishing (2001) | secondary | https://www.azom.com/article.aspx?ArticleID=1016 |
| Wallcovering glossary | primary | https://www.wolfgordon.com/resources/wallcovering-glossary |
| Wallcovering dictionary, in-register emboss | secondary | https://www.wallcoveringinstallers.org/consumers/wallcovering-resources/wallcovering-dictionary/ |
| Blown vinyl (1982 patent) | primary | https://patents.google.com/patent/US4427731 |
| Linen-weave emboss (1993 patent) | primary | https://patents.google.com/patent/US5364681 |
| Matte topcoat gloss (1987 patent) | primary | https://patents.google.com/patent/US4804572 |
| Emboss before or after printing (1999 patent) | primary | https://patents.google.com/patent/US6180243B1/en |
| Burlap thread pitch | secondary | https://atsacoustics.com/wholesale-burlap-fabric.html |
| Stucco "heavy depth" | secondary | https://parallaxdigital.com/digital-wall-covering/ |
| Period-style chevron sample, seller claim | photo | https://www.ebay.de/itm/306387074829 |
| Modern 54 in burlap swatch | photo | https://www.commercialwalldecor.com/product/burlap-texture-commercial-wallcovering-10285.aspx |
| A24 film wallpaper still | photo | https://www.highsnobiety.com/p/a24-backrooms-wallpaper-merch-supreme-brick/ |

