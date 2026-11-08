# 光影水反 · light_design · sources and media candidates

Sources and media candidates of `10_research.md` in this folder (line 光影水反, ledger key `water`, 2026-11-06). Ids `F` are project files, `S` web sources; how each was read is in its row. Nothing listed under media candidates was downloaded: each download needs Red's OK with its name, source and size.

## SOURCES

Path keys: `M` = `Frontrooms3D` (the main project); `S` = `M/Assets/Scripts`; `RR` = `M/Assets/Resources/Rendering`; `R` = `M/Documentation/research`; `D` = `Documentation/Takeover_2026-10-18`; `W` = `/Users/redwang/FrontRoomsVisualWork`; `U` = `M/Library/PackageCache/com.unity.render-pipelines.universal@327d93d1cbc8` (URP 17.6.0); `CORE` = the matching `com.unity.render-pipelines.core` 17.6 package. Line numbers are as read on 2026-11-05.

| id | Source | Used for |
|---|---|---|
| F00 | This line's research notes, `W/light_design/research_notes/光影水反 AAA 照明设计研究/`: `frontrooms_current_stack.md` (read-only audit of main, 2026-11-05, with texture means measured by 128 × 128 downsampling), `gi_colour_bleeding.md`, `interior_lighting_design.md`, `volumetric_light.md`, `water_reflection_interplay.md`, `windows_daylight_glass.md` | [EST] arithmetic, measured texture means, inferences |
| F01 | `M/Assets/Settings/FrontRooms_URP.asset:19-31, 45-82` | desktop and WebGL URP asset; probe blending 0, box projection 0 (:55-57) |
| F02 | `M/Assets/Settings/FrontRooms_URP_Handheld.asset:19-82`; `FrontRooms_URP_Handheld_Renderer.asset:72-83` | handheld pair; SSAO after opaque from depth |
| F03 | `M/ProjectSettings/QualitySettings.asset:187, 293, 347, 394-398` | platform defaults; realtime probes off on Handheld |
| F04 | `S/FrontRoomsQuality.cs:26-30, 150-162, 259-318, 454-483` | presets; handheld 3.2 Mpx budget |
| F05 | `RR/FrontRoomsSurface.shader:246-283, 294-316, 830-831, 2749-2750, 2789-2823` | Surface keywords, SH-only indirect, height fog, pool layers |
| F06 | `S/Rendering/FrontRoomsLook.cs:12-32, 46-64` | Level 0 Trilight, fog, reflection level, DeadLamp cube choice |
| F07 | `M/Assets/Scenes/FrontRooms3D.unity:17-39, 53-54, 2255-2296` | scene ambient, no lightmaps, the fill light |
| F08 | `S/Rendering/ZoneLook/FrontRoomsZoneLook.cs:8-36, 589-593`; `RR/ZoneLook/FrontRoomsZoneLook_<zone>.asset:15-34` | pool-zone ambient; `SetLampShare` unused by the map |
| F09 | `S/FrontRoomsMap/FrontRoomsMapWorld.cs:3716-3765, 3895-3935, 4798, 4859, 7012-7047` | map lamps, the 22-lamp handheld budget, zone volumes, lamp colours |
| F10 | `S/Rendering/FrontRoomsZoneReflection.cs:10-48, 131` | seven zone cubes, cap 0.5 |
| F11 | `RR/Reflections/capture_manifest.txt:2-13`; `M/Assets/Editor/Rendering/FrontRoomsReflectionCapture.cs:15-35, 432-434` | capture date and conditions |
| F12 | `S/Rendering/FrontRoomsTrofferKit.cs:5-42` | lens drive 1.8 (gamma); Relay lens below the troffers' p99 |
| F13 | `RR/FrontRoomsPost.asset:13-313`; `RR/FrontRoomsPost_<zone>.asset` | film look, bloom, exposure, white balance, zone grades |
| F14 | `S/FrontRoomsRoomStream.cs:101-103, 1243-1267, 2063-2069` | beams removed; corridor lamps; dust shafts opt-in and unused |
| F15 | `RR/FrontRoomsDustShaft.shader:1-17` | the dust shaft |
| F16 | `RR/FrontRoomsWater.shader:4-23, 191-197, 266-291` | reflection-only water; per-lamp glints on the probe tier |
| F17 | `S/Rendering/Water/FrontRoomsWaterPlanar.cs:10-79` | planar mirror tiers |
| F18 | `RR/FrontRoomsWaterSubmerged.hlsl:10-42, 490-676` | submerged light, thrown light, the water-light mirror and bounce |
| F19 | `S/Rendering/Water/FrontRoomsWaterSubmerged.cs:473-625` | statics default and "Recommended" |
| F20 | `S/FrontRoomsRoomStream.TitleRooms.cs:800-869`; `S/FrontRoomsMap/Structures/FrontRoomsStructuresWater.cs:101-123` | who switches the water light on |
| F21 | `R/bathroom_rooms/70_look_build.md` §2.3, §4.3, §6.8 (:776), §7.6-7.7, §12 (:1278-1388) | lens 1.25 × floor, water-light shares and costs, pool look |
| F22 | `R/bathroom_rooms/17_water.md` §0, §4.2, §10 | water cues, POOLS, GlassRT and water, s10b renders |
| F23 | `R/tech_art/13_lighting.md` §0, §1.3, §2, §5.2, §6 (L101-L113) | probes, shadows, light budgets, rows L102-L112 |
| F24 | `W/tech_art/MEASURED.md:3-74` | measured pixel changes and draws |
| F25 | `R/tech_art/21_measured.md:84-104` | Mac player timings under load |
| F26 | `R/interaction_audit/03_rendering_quality.md` §0, §3, §5, §6 | October audit rows and cost classes |
| F27 | `R/interactables/window_landing/05_for_red.md` §0, §2.7, §3, §5 | window counts, mini-blind kit, A2, GlassRT costs |
| F28 | `S/Office/FrontRoomsInteractableKit.Window.cs:12-26`; `S/FrontRoomsMap/FrontRoomsMapWorld.cs:2375-2379, 3270-3320` | interior windows between cells |
| F29 | `S/Rendering/FrontRoomsMetalGlassRTRendererFeature.cs:1-24`; `S/Rendering/GlassRT/FrontRoomsGlassRTSystem.cs:204-228, 923, 1229, 1263`; `S/Rendering/Glass/FrontRoomsGlassBreakable.cs:882-908` | GlassRT scope and receivers |
| F30 | `R/glass/rt/03_research.md` §3, §5 | pane Fresnel, the one-way mirror, shipped RT glass |
| F31 | `R/glass/11_reflections_and_raytracing.md` §0, §2.5 | probe re-render costs |
| F32 | `D/handoff/visual.md:15-30` | P1-A and P2-A pending, photosafety flash count, water-light fix |
| F33 | `D/VISUAL_STANDARD.md:12-13, 23` | desktop reference; Red's lens 1.8 |
| F34 | `W/water_light/for_water_line.md` (lines 9, 16, 36, 39) | water-light constants and costs |
| F35 | `S/Rendering/FrontRoomsRunRig.cs:206-209, 1725-1790, 2752-2772` | per-renderer SH in the Run! rig |
| F36 | `U`: `ShaderLibrary/GlobalIllumination.hlsl:35-41, 423-494`; `ShaderLibrary/UnityInput.hlsl:110-177`; `ShaderLibrary/RealtimeLights.hlsl:54-79`; `ShaderLibrary/Lighting.hlsl:48-70`; `Runtime/UniversalRenderPipelineCore.cs:1683-1691`; `Runtime/RendererFeatures/SurfaceCacheGIRendererFeature/SurfaceCacheGIRendererFeature.cs:1`; `Runtime/Overrides/` | URP 17.6 behaviour |
| F37 | `CORE`: `Runtime/Lighting/ProbeVolume/ProbeVolume.hlsl:988-993`; `ShaderLibrary/CommonLighting.hlsl:269-342`; `Editor/Lighting/IESImporter.cs:29-105` | normalisation, occlusion helpers, IES import |
| F38 | `R/codex_audit/30_fix_other_visual.md:53` | wallpaper raster mean albedo, 2026-10-08 |
| F39 | `S/FrontRoomsMap/FrontRoomsMapWorld.Pool.cs:818-918, 1042-1106` | pool lamp stand-ins |
| F40 | `R/zone_lamps/30_for_red.md:3, 50-51`; `R/zone_lamps/10_office.md:134-142` | Red's P1-A and P2-A picks; troffer luminance by angle |
| F41 | project memory `three-platform-parity.md` (Red, 2026-10-30) | "三端保持一致" |
| S01 | [Wikipedia, Fresnel equations](https://en.wikipedia.org/wiki/Fresnel_equations) | water reflectance by angle (computed) |
| S02 | [iadpython, Integrating sphere basics](https://iadpython.readthedocs.io/en/stable/sphere-basics.html) | interreflection gain, bounce colour |
| S03 | [Cooper Lighting, Metalux 2G-240A125 photometric report (2020 reissue)](https://webtools.cooperlighting.com/Public/files/ies/instabase/METALUX/RECESSED%20STATIC/G/2%20X%204/2G-240A125.pdf) | period 2 × 4 lensed troffer: candela, lens luminance |
| S04 | [Adobe, The PBR Guide part 2](https://www.adobe.com/learn/substance-3d-designer/web/the-pbr-guide-part-2) | dielectric F0 2-5 %, base-colour range |
| S05 | [Architect Magazine, Richard Kelly's three tenets](https://www.architectmagazine.com/projects/richard-kellys-three-tenets-of-lighting-design_o) (search summary) | Kelly's three layers |
| S06 | [EC&M, Psychology of light](https://www.ecmag.com/magazine/articles/article-detail/lighting-psychology-light) (search summary) | Flynn's studies |
| S07 | [Bernecker, LEDucation 2020, Luminance and Exitance](https://leducation.org/wp-content/uploads/2023/06/LEDucation2020-Luminance-and-Exitance.pdf) | Cuttle's mean room surface exitance |
| S08 | [Building, Office lighting, 2004-12-07](https://www.building.co.uk/office-lighting/3044112.article) | CIBSE LG7 wall and ceiling targets, reflectances |
| S09 | [EC&M, Knisley, Making office lighting work, 2004](https://www.ecmweb.com/content/making-office-lighting-work) | IES RP-1 ratios and ambient goal |
| S10 | [Wikipedia, CIECAM02](https://en.wikipedia.org/wiki/CIECAM02) | degree of adaptation, white balance |
| S11 | [Zucconi, Valve's flickering lights, 2021](https://www.alanzucconi.com/2021/06/15/valve-flickering-lights/) | Quake light styles |
| S12 | [80.lv, Learning lighting for video games (Betancourt)](https://80.lv/articles/learning-lighting-for-video-games/) | key-first practice |
| S13 | [GDC Vault, Ding 2014, In-Game and Cinematic Lighting of The Last of Us](https://gdcvault.com/play/1020475/In-Game-and-Cinematic-Lighting) | three TLOU lighting techniques |
| S14 | [Game Developer, Remedy and brutalism in Control, 2019](https://gamedeveloper.com/design/how-remedy-harnessed-the-ethereal-beauty-of-brutalism-to-shape-i-control-i-) | bare walls carried by lighting |
| S15 | [GDC Vault, Goudreault 2023, Dead Space: light and darkness](https://gdcvault.com/play/1029020/-Dead-Space-Harnessing-the) | fixture as a system |
| S16 | [80.lv, Lighting environments: tips and tricks (Yue)](https://80.lv/articles/lighting-environments-tips-and-tricks) | lock exposure early |
| S17 | [Awards Daily, Severance cinematography, 2022-06-10](https://awardsdaily.com/?p=176012) | about 90 SkyPanels |
| S18 | [Wikipedia, The Backrooms](https://en.wikipedia.org/wiki/The_Backrooms) | the 2002 photograph |
| S19 | [Sweets, Armstrong TechZone with Optima](https://sweets.construction.com/manufacturer/armstrong-world-industries--inc-nst3824/products/techzone-with-optima--1410---acoustical-ceiling-tile-nst584346-p) (search summary) | ceiling tile reflectance |
| S20 | [physicallybased.info API](https://api.physicallybased.info/materials) | porcelain, concrete albedo |
| S21 | [McGuire, DDGI overview, 2019](https://morgan3d.github.io/articles/2019-04-01-ddgi/overview.html) | DDGI cost and lag |
| S22 | [Epic, Lumen technical details](https://dev.epicgames.com/documentation/en-us/unreal-engine/lumen-technical-details-in-unreal-engine) | Lumen cost, walls, forward shading |
| S23 | [Epic, Lumen GI and reflections](https://dev.epicgames.com/documentation/en-us/unreal-engine/lumen-global-illumination-and-reflections-in-unreal-engine) | propagation time |
| S24 | [Unity Discussions, Surface Cache GI preview](https://discussions.unity.com/t/surface-cache-gi-preview/1720494) | Surface Cache GI status |
| S25 | [Wccftech, Kingdom Come: Deliverance SVOGI, 2015](https://wccftech.com/kingdom-come-deliverance-gets-svogi-onoff-comparison-performance-hit-is-minimal/) (search summary) | SVOGI hit and lag |
| S26 | [Kaplanyan, Light Propagation Volumes, SIGGRAPH 2009](https://advances.realtimerendering.com/s2009/Light_Propagation_Volumes.pdf) (search summary) | LPV budget |
| S27 | [Unity 6000.6, Enlighten realtime GI](https://docs.unity.com/en-us/engine/6000.6/manual/lighting-overview/direct-and-indirect-lighting/lightmapping/realtime-gi-using-enlighten/enlighten) | Enlighten limits |
| S28 | [Valve, Shading in Valve's Source Engine, SIGGRAPH 2006](https://cdn.akamai.steamstatic.com/apps/valve/2006/SIGGRAPH06_Course_ShadingInValvesSourceEngine.pdf) | ambient cube |
| S29 | [GDC Vault, Stefanov 2016, GI in The Division](https://gdcvault.com/play/1023273/Global-Illumination-in-Tom-Clancy) (search summary) | relightable transfer probes |
| S30 | [GDC Vault, Deferred Radiance Transfer Volumes (Far Cry 3), 2012](https://gdcvault.com/play/1015326/Deferred-Radiance-Transfer-Volumes-Global) | relit probes |
| S31 | [Unity 6000.6, render pipeline feature comparison](https://docs.unity.com/en-us/engine/6000.6/manual/render-pipelines/choose-a-render-pipeline/feature-comparison) | URP feature gaps |
| S32 | [Unity 6000.6, LightProbes scripting reference](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/lightprobes) | runtime probe edits |
| S33 | [Unity 6000.6, Moving light probes](https://docs.unity.com/en-us/engine/6000.6/manual/lighting-overview/direct-and-indirect-lighting/light-probes/moving) | runtime probe moves |
| S34 | [Unity Asset Store, Radiant GI](https://assetstore.unity.com/packages/slug/225934) (search summary) | commercial SSGI |
| S35 | [c0de517e, Envmap lighting, 2014](https://c0de517e.blogspot.com/2014/06/oh-envmap-lighting-how-do-we-get-you.html) | why cubes look wrong |
| S36 | [Lazarov, Black Ops II, SIGGRAPH 2013 slides v2](https://blog.selfshadow.com/publications/s2013-shading-course/lazarov/s2013_pbs_black_ops_2_slides_v2.pdf) | reflection normalisation |
| S37 | [Activision, Rendering of COD: Infinite Warfare, 2017](https://www.activision.com/cdn/research/2017_DD_Rendering_of_COD_IW.pdf) (search summary) | probe scaling by light grid |
| S38 | [Lagarde, IBL and parallax-corrected cubemap, 2012](https://seblagarde.wordpress.com/2012/09/29/image-based-lighting-approaches-and-parallax-corrected-cubemap/) | normalisation survey, seams |
| S39 | [Epic, Reflection captures](https://dev.epicgames.com/documentation/en-us/unreal-engine/reflections-captures-in-unreal-engine) | lightmap mixing, smooth surfaces |
| S40 | [HDRP 17.0, Probe Volumes options override](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.0/manual/probevolumes-options-override-reference.html) | decrease-only normalisation |
| S41 | [Unity Graphics, CommonLighting.hlsl](https://raw.githubusercontent.com/Unity-Technologies/Graphics/master/Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl) | specular and horizon occlusion |
| S42 | [UploadVR, POOLS VR review, 2025-06-03](https://www.uploadvr.com/pools-vr-review/) | one-eye reflections |
| S43 | [PC Games Hardware, The Last of Us Part II PC, p.2, 2025](https://www.pcgameshardware.de/The-Last-of-Us-Part-2-Remastered-Spiel-74482/Specials/PC-Release-Review-Steam-Test-Benchmarks-1469049/2/) | cube-map water, SSR for characters |
| S44 | [NVIDIA, Alan Wake 2 full ray tracing, 2023](https://www.nvidia.com/en-us/geforce/news/alan-wake-2-dlss-3-5-full-ray-tracing-out-this-week/) (vendor) | path tracing, flickering lights |
| S45 | [Digital Trends, Alan Wake 2 PC performance, 2023](https://www.digitaltrends.com/computing/alan-wake-2-pc-performance/) | puddles without RT |
| S46 | [Recreation Management, Natatorium lighting design, 2019](https://recmanagement.com/articles/152591/natatorium-lighting-design) | pool-hall lighting practice |
| S47 | [Lagarde, Water drop 3b, 2013](https://seblagarde.wordpress.com/2013/04/14/water-drop-3b-physically-based-wet-surfaces/) | wet-surface model |
| S48 | [Lagarde, Water drop 2b, 2013](https://seblagarde.wordpress.com/2013/01/03/water-drop-2b-dynamic-rain-and-its-effects/) | puddles, rain cost |
| S49 | [ColinLeung-NiloCat, URP mobile screen-space planar reflection](https://github.com/ColinLeung-NiloCat/UnityURP-MobileScreenSpacePlanarReflection) (search summary) | SSPR cost on a phone |
| S50 | [Wronski, Volumetric fog, SIGGRAPH 2014](https://bartwronski.com/wp-content/uploads/2014/08/bwronski_volumetric_fog_siggraph2014.pdf) | AC4 froxels |
| S51 | [Hillaire, Unified volumetric rendering in Frostbite, 2015](https://www.slideshare.net/DICEStudio/physically-based-and-unified-volumetric-rendering-in-frostbite) | Frostbite froxels and costs |
| S52 | [Epic, Volumetric fog](https://dev.epicgames.com/documentation/en-us/unreal-engine/volumetric-fog-in-unreal-engine) | UE cost, trails, water fog |
| S53 | [Epic, Rendering features for mobile](https://dev.epicgames.com/documentation/unreal-engine/rendering-features-for-mobile-games-in-unreal-engine) | no volumetric fog on mobile |
| S54 | [HDRP 17.0, Fog volume override](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.0/manual/fog-volume-override-reference.html) | HDRP fog controls |
| S55 | [HDRP 17.5, Volumetric lighting](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.5/manual/Volumetric-Lighting.html) | reprojection and dynamic lights |
| S56 | [Glatzel, Volumetric lighting in Lords of the Fallen, 2014](https://www.slideshare.net/slideshow/volumetric-lighting-for-many-lights-in-lords-of-the-fallen/34507223) | per-light march cost |
| S57 | [Valient, Killzone Shadow Fall graphics, GDC 2014](https://www.guerrilla-games.com/media/News/Files/GDC2014_Valient_Killzone_Graphics.pdf) | half-resolution shafts |
| S58 | [GPU Gems 3, ch. 13, Volumetric light scattering post-process](https://developer.nvidia.com/gpugems/gpugems3/part-ii-light-and-shadows/chapter-13-volumetric-light-scattering-post-process) | screen-space god rays |
| S59 | [ACM SIGGRAPH Blog, volumetric effects of TLOU Part II, 2021](https://blog.siggraph.org/?p=11909) | over 5 M froxels |
| S60 | [Remedy, How Northlight makes Alan Wake 2 shine](https://www.remedygames.com/article/how-northlight-makes-alan-wake-2-shine) | fog with transparents and water |
| S61 | [GameSpot, Dead Space remake](https://www.gamespot.com/articles/dead-space-remake-will-cut-some-elements-that-dont-work/1100-6494338/) | room depth and air |
| S62 | [Valve Developer Wiki, env_volumetric_fog_controller](https://developer.valvesoftware.com/wiki/Env_volumetric_fog_controller) | Alyx fog volumes |
| S63 | [Valve Developer Wiki, light_environment (Source 2)](https://developer.valvesoftware.com/wiki/Light_environment_(Source_2)) | Alyx volumetric lights |
| S64 | [mamoniem, Behind the Pretty Frames: Resident Evil, 2022](https://mamoniem.com/?p=2601) | RE Engine stages |
| S65 | [EPA, BASE study, 100 office buildings](https://www.epa.gov/sites/default/files/2014-08/documents/base_a2_395.pdf) | office PM2.5 |
| S66 | [EPA, IMPROVE Appendix C](https://www.epa.gov/sites/default/files/2015-05/documents/appc.pdf) | extinction efficiencies |
| S67 | [Wikipedia, Visibility](https://en.wikipedia.org/wiki/Visibility) | Koschmieder relation |
| S68 | [IIBEC, natatorium paper, 2025](https://iibec.org/wp-content/uploads/2025/09/Smegal-FINAL.pdf) | pool-hall humidity |
| S69 | [AMS Glossary, Steam fog](https://glossary.ametsoc.org/wiki/Steam_fog) | when mist forms |
| S70 | [Andrews et al., JGR 2006](https://gml.noaa.gov/aftp/aerosol/doc/articles/Andrews_2006_JGR_AIOP_asymmetry.pdf) | aerosol anisotropy |
| S71 | [AMS Glossary, Asymmetry factor](https://glossary.ametsoc.org/wiki/Asymmetry_factor) | droplet anisotropy |
| S72 | [Unity Discussions, URP 6.3 still has no native volumetric fog, 2026-08-31](https://discussions.unity.com/t/urp-6-3-still-has-no-native-volumetric-fog/1735239) | URP status |
| S73 | [CristianQiu, Unity-URP-Volumetric-Light](https://github.com/CristianQiu/Unity-URP-Volumetric-Light) | MIT URP volumetric pass |
| S74 | [CristianQiu, issue #7 (WebGL)](https://github.com/CristianQiu/Unity-URP-Volumetric-Light/issues/7) | browser freeze |
| S75 | [Unity Asset Store, Volumetric Light Beam](https://assetstore.unity.com/packages/vfx/shaders/volumetric-light-beam-99888) | beams on every platform |
| S76 | [LBNL, Robinson and Selkowitz, Tips for Daylighting with Windows, 2013](https://www.osti.gov/servlets/purl/1167562) | daylit depth, placement, WWR |
| S77 | [Wikipedia, Lux](https://en.wikipedia.org/wiki/Lux) | outdoor and indoor light levels |
| S78 | [PPG, Monolithic glass comparisons (2005)](https://syracuseglass.com/E-DOCS/Architectural%20Glass/EDOCS/PPG%20Monolithic%20Glass%20Comparisons.pdf) | VLT and reflectance of clear and tinted glass |
| S79 | [Sweets / Vitro, Solarcool](https://sweets.construction.com/swts_content_files/510/P142032.htm) | reflective coatings, 1972 |
| S80 | [Biddle, A short history of the mirrored glass façade, PIN-UP](https://archive.pinupmagazine.org/articles/panorama-a-short-history-of-the-mirrored-glass-facade-buildings-ouida-biddle) | mirror-glass offices |
| S81 | [GSA lighting documents, CDC Stacks](https://stacks.cdc.gov/view/cdc/179853) (search summary) | post-1973 federal light levels |
| S82 | [Unity 6.6, Light component](https://docs.unity.com/en-us/engine/current/manual/lighting-overview/lighting/light-component) | cookies, area lights, wide spots |
| S83 | [Heitz et al., LTC, SIGGRAPH 2016](https://www.advances.realtimerendering.com/s2016/s2016_ltc_rnd.pdf) (search summary) | rectangle lights |
| S84 | [van Dongen, Interior mapping, 2018](https://joostdevblog.blogspot.com/2018/09/interior-mapping-real-rooms-without.html) | views out |
| S85 | [PC Games Hardware, Hitman 3 ray tracing, 2022-06-01](https://www.pcgameshardware.de/Hitman-3-Spiel-72804/Specials/Hitman-3-Raytracing-Nvidia-Geforce-AMD-Intel-Benchmarks-Review-1396024/) | planar and RT glass |
| S86 | [Game Developer, GDC 2016: Mirror's Edge Catalyst lighting](https://www.gamedeveloper.com/programming/gdc-2016-see-how-the-striking-look-of-i-mirror-s-edge-catalyst-i-was-achieved) | realtime radiosity |
| S87 | [Backrooms Wiki, Level 4](https://backrooms-wiki.wikidot.com/level-4) | blacked-out windows |
| S88 | [Backrooms Wiki, Level 684](https://backrooms-wiki.wikidot.com/level-684) (search summary) | unreachable view |
| S89 | [cpu-monkey, Apple M3 Max 40-core GPU](https://www.cpu-monkey.com/en/igpu-apple_m3_max_40_core) | FP32, bandwidth |
| S90 | [cpu-monkey, A17 Pro vs M4 Pro](https://www.cpu-monkey.com/en/compare_cpu-apple_a17_pro-vs-apple_m4_pro_12_cpu_16_gpu) | A17 Pro throughput |
| S91 | [cpu-monkey, Apple M4 10-core GPU](https://www.cpu-monkey.com/en/igpu-apple_m4_10_core) | iPad throughput |
| S92 | [GDC Vault, Rendering the World of Far Cry 4](https://gdcvault.com/play/1022234/Rendering-the-World-of-Far) (second-hand, search summary) | relit cube maps |
| S93 | [HDRP 17.4, Troubleshoot fog](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.4/manual/troubleshoot-fog.html) | flicker fix, memory |
| S94 | [Epic, Auto exposure](https://dev.epicgames.com/documentation/en-us/unreal-engine/auto-exposure-in-unreal-engine) (search summary) | adaptation speeds |
| S95 | [General Glass International, wire glass data sheet, 2022](https://www.generalglass.com/wp-content/uploads/2022/08/GGI-Wire-Glass_Data-Sheet.pdf) (search summary) | wired glass VLT |
| S96 | [Valve Developer Wiki, env_volumetric_fog_volume](https://developer.valvesoftware.com/wiki/Env_volumetric_fog_volume) | Alyx fog clamped to volumes |

## Media candidates

Nothing below was downloaded. Each download needs Red's OK with its name, source and size; "page" means a web page whose images can be viewed in a browser without saving anything.

| # | What it shows | Source | Size | Helps |
|---|---|---|---|---|
| M1 | polar plot, lens luminance and CU tables of a period 2 × 4 lensed troffer | [Metalux 2G-240A125 PDF](https://webtools.cooperlighting.com/Public/files/ies/instabase/METALUX/RECESSED%20STATIC/G/2%20X%204/2G-240A125.pdf) | 309 KB | LW3 |
| M2 | the same for a three-lamp T8 lensed 2 × 4 | [Metalux 2GR8-332A125MIN PDF](https://webtools.cooperlighting.com/Public/files/ies/instabase/METALUX/RECESSED%20STATIC/GR8/2%20X%204/2GR8-332A125MIN.pdf) | 312 KB | LW3 |
| M3 | room-exitance scale, luminance renderings, rating charts | [Bernecker, LEDucation 2020 PDF](https://leducation.org/wp-content/uploads/2023/06/LEDucation2020-Luminance-and-Exitance.pdf) | 2.0 MB | LW2, LW3 |
| M4 | office lighting quality report (scanned, unread) | [NIST IR 5119 PDF](https://nvlpubs.nist.gov/nistpubs/Legacy/IR/nistir5119.pdf) | 4.9 MB | background |
| M5 | the 2002 Oshkosh photograph | [Wikipedia, The Backrooms](https://en.wikipedia.org/wiki/The_Backrooms) | page | look reference |
| M6 | liminal-space photographs and Backrooms stills | [ArchDaily, why liminal spaces are eerie](https://www.archdaily.com/996668/why-) | page | look reference |
| M7 | before/after lit interiors in the Prey and Alien: Isolation manner | [80.lv, Mimicking Prey and Alien: Isolation lighting](https://80.lv/articles/mimicking-prey-and-alien-isolation-lighting-in-ue4/) | page | LW2, LW3 |
| M8 | talk videos on TLOU and Dead Space lighting (may need membership) | [GDC Vault, TLOU](https://gdcvault.com/play/1020475/In-Game-and-Cinematic-Lighting); [GDC Vault, Dead Space](https://gdcvault.com/play/1029020/-Dead-Space-Harnessing-the) | video | LW3 |
| M9 | animated light-style strings | [Zucconi 2021](https://www.alanzucconi.com/2021/06/15/valve-flickering-lights/) | page | LW3 |
| M10 | Cycles renders of measured materials (porcelain, concrete, paper) | [physicallybased.info renders, 600 px](https://raw.githubusercontent.com/AntonPalmqvist/physically-based-api/main/images/renders/cycles/600/porcelain.jpeg) | about 600 px each | LW2 |
| M11 | Severance's Lumon ceiling of soft panels | [Awards Daily, 2022-06-10](https://awardsdaily.com/?p=176012) | page | LW3 |
| M12 | froxel fog: temporal jitter before/after (p60), sky light in fog (p52-54) | [Wronski, SIGGRAPH 2014 PDF](https://bartwronski.com/wp-content/uploads/2014/08/bwronski_volumetric_fog_siggraph2014.pdf) | ≈5.5 MB | LW6 |
| M13 | AC4 fog in interiors and exteriors | [Wronski, GDC 2014 PDF](https://bartwronski.com/wp-content/uploads/2014/03/ac4_gdc.pdf) | ≈4.1 MB | LW6 |
| M14 | froxel diagrams, local-light volumetric shadows, 8 × 8 against 16 × 16 | [Hillaire 2015 slides](https://www.slideshare.net/DICEStudio/physically-based-and-unified-volumetric-rendering-in-frostbite) | slides | LW6 |
| M15 | interleaved sampling before/after blur, half against quarter resolution | [Glatzel, Lords of the Fallen slides](https://www.slideshare.net/slideshow/volumetric-lighting-for-many-lights-in-lords-of-the-fallen/34507223) | slides | LW6 |
| M16 | TLOU Part II sharp shafts and fog with transparents | [Naughty Dog, SIGGRAPH 2020 page](https://www.naughtydog.com/blog/naughty_dog_at_siggraph_2020) | ≈1.2 GB slides | LW6 |
| M17 | INSIDE's local shadowed volumetrics and dithering | [Playdead slides PDF](https://loopit.dk/rendering_inside.pdf); [GDC Vault video](https://gdcvault.com/play/1023783/Low-Complexity-High-Fidelity-INSIDE) | over 10 MB | LW6 |
| M18 | half-resolution volumetric pass and bilateral upscale | [Killzone Shadow Fall GDC 2014 PDF](https://www.guerrilla-games.com/media/News/Files/GDC2014_Valient_Killzone_Graphics.pdf) | unknown | LW6 |
| M19 | Hellblade 2's low-resolution fog artefacts | [Digital Foundry review via a mirror](https://venvegame.netlify.app/host-https-www.eurogamer.net/digitalfoundry-2024-senuas-saga-hellblade-2-is-a-defining-moment-in-the-evolution-of-real-time-graphics) | page, video | LW6 |
| M20 | Alan Wake 2 fog with transparents | [Remedy Northlight article](https://www.remedygames.com/article/how-northlight-makes-alan-wake-2-shine) | page | LW6 |
| M21 | URP shafts from spot and point lights | [CristianQiu README](https://github.com/CristianQiu/Unity-URP-Volumetric-Light) | page | LW6 |
| M22 | soft-intersection beams on mobile and WebGL | [Volumetric Light Beam store page](https://assetstore.unity.com/packages/vfx/shaders/volumetric-light-beam-99888) | videos | LW6 |
| M23 | shadowed god rays in a browser | [three-good-godrays demo](https://github.com/ameobea/three-good-godrays) | page | LW6 (WebGL) |
| M24 | POOLS: sharp HDR mirror over a dark body (13 frames) | [Steam store](https://store.steampowered.com/app/2663530/POOLS/) | page | LW1, LW4 |
| M25 | POOLS VR reflections | [UploadVR review](https://www.uploadvr.com/pools-vr-review/) | page | LW4 |
| M26 | path-traced neon in reflections, coloured bounce | [NVIDIA, Cyberpunk 2077 Overdrive](https://www.nvidia.com/en-us/geforce/news/cyberpunk-2077-ray-tracing-overdrive-update-launches-april-11/) | page, video | LW9 |
| M27 | Alan Wake 2 path tracing on/off; puddles with and without RT | [NVIDIA](https://www.nvidia.com/en-us/geforce/news/alan-wake-2-dlss-3-5-full-ray-tracing-out-this-week/); [Digital Trends](https://www.digitaltrends.com/computing/alan-wake-2-pc-performance/) | pages | LW4 |
| M28 | cube-map water reflections and rain ribbons in TLOU Part II | [PCGH, p.2](https://www.pcgameshardware.de/The-Last-of-Us-Part-2-Remastered-Spiel-74482/Specials/PC-Release-Review-Steam-Test-Benchmarks-1469049/2/) | page | LW4 |
| M29 | one mesh under three bakes before and after normalisation (slides 14-16) | [Lazarov 2013 slides v2 PDF](https://blog.selfshadow.com/publications/s2013-shading-course/lazarov/s2013_pbs_black_ops_2_slides_v2.pdf) | ≈3.2 MB | LW4 |
| M30 | reflection capture shapes and lightmap mixing | [Epic, Reflection captures](https://dev.epicgames.com/documentation/en-us/unreal-engine/reflections-captures-in-unreal-engine) | page | LW4 |
| M31 | APV normalisation on/off on a kitchen cabinet (page not confirmed) | [HDRP 17.0, APV use](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.0/manual/probevolumes-use.html) | page | LW4 |
| M32 | puddles, ripples, wet surfaces; parallax-corrected cube diagrams | [Lagarde 2b](https://seblagarde.wordpress.com/2013/01/03/water-drop-2b-dynamic-rain-and-its-effects/); [Lagarde 3b](https://seblagarde.wordpress.com/2013/04/14/water-drop-3b-physically-based-wet-surfaces/); [Lagarde 2012](https://seblagarde.wordpress.com/2012/09/29/image-based-lighting-approaches-and-parallax-corrected-cubemap/) | pages | LW4, LW8 |
| M33 | mobile screen-space planar reflection GIFs | [SSPR README](https://github.com/ColinLeung-NiloCat/UnityURP-MobileScreenSpacePlanarReflection) | page | LW1 (handheld mirror) |
| M34 | stochastic SSR (Mirror's Edge Catalyst) | [slides](https://www.slideshare.net/slideshow/stochastic-screenspace-reflections/51840375) | slides, unopened | LW4 |
| M35 | real-time underwater caustics | [80.lv, Timothy Cole](https://80.lv/articles/working-with-underwater-caustics-in-real-time) | page, unopened | LW5 |
| M36 | daylight penetration, strip against punched windows, shading, clerestory, light shelf (Figures 4, 5, 9, 11-14) | [LBNL 2013 PDF](https://www.osti.gov/servlets/purl/1167562) | 5.2 MB | LW7 |
| M37 | interior with and without light portals; rectangle-light examples | [Epic, Lightmass portals](https://dev.epicgames.com/documentation/unreal-engine/lightmass-portals-in-unreal-engine); [Epic, Rect lights](https://dev.epicgames.com/documentation/en-us/unreal-engine/rectangular-area-lights-in-unreal-engine) | pages | LW7 |
| M38 | interior mapping diagrams and GIFs; Spider-Man's parallax windows | [van Dongen 2018](https://joostdevblog.blogspot.com/2018/09/interior-mapping-real-rooms-without.html); [Kotaku 2018](https://kotaku.com/spider-mans-windows-are-amazing-1828975723) | pages | LW7 |
| M39 | Hitman 3 RT on/off on glass, mirrors and puddles | [PCGH 2022-06-01](https://www.pcgameshardware.de/Hitman-3-Spiel-72804/Specials/Hitman-3-Raytracing-Nvidia-Geforce-AMD-Intel-Benchmarks-Review-1396024/) | page | LW7 |
| M40 | Mirror's Edge Catalyst lighting talk | [GDC Vault video](https://www.gdcvault.com/play/1023284/Lighting-the-City-of-Glass); [DICE slides](https://de.slideshare.net/DICEStudio/lighting-the-city-of-glass) | video, slides | LW7 |
| M41 | mirrored-glass façades (Bell Labs, Century City, Bonaventure) | [PIN-UP](https://archive.pinupmagazine.org/articles/panorama-a-short-history-of-the-mirrored-glass-facade-buildings-ouida-biddle) | page | LW7 |
| M42 | blacked-out and unreachable office windows (fan canon) | [Level 4](https://backrooms-wiki.wikidot.com/level-4); [Level 684](https://backrooms-wiki.wikidot.com/level-684) | pages | LW7 |
| M43 | VLT and reflectance table for clear and tinted glass | [PPG table PDF](https://syracuseglass.com/E-DOCS/Architectural%20Glass/EDOCS/PPG%20Monolithic%20Glass%20Comparisons.pdf) | 84 KB | LW7 |
