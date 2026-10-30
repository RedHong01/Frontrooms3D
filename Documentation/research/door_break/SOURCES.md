# Door break research: media sources ledger

Started 2026-10-08 by `21_C_realism_research.md` (Direction C realism). Earlier door-break files (`01`, `02`, `10`, `20_dir_*`) credit their sources inside their own "Sources" sections. This ledger covers **media files** (images and clips) that the door-break documents use as evidence. Web pages that were only read are listed in each document's Sources table (for `21`, §9).

**Nothing was downloaded for this ledger.** Every outside file below was already on disk. Its credit is copied from the ledger that first recorded it. Media still waiting for Red's approval is listed in `media_candidates.md`, not here.

## Outside media already on disk (reused by `21`)

| File | On disk | Source | Licence | First ledger | What `21` uses it for |
|---|---|---|---|---|---|
| `ir01_dsc00161_20020612_082113.jpg` (the Level 0 photo) | `Research/week02/ip-research/stills/` | Wikimedia Commons, "HobbyTown USA Oshkosh interior under construction 2002 (The Backrooms).jpg", https://commons.wikimedia.org/wiki/File:HobbyTown_USA_Oshkosh_interior_under_construction_2002_(The_Backrooms).jpg ; Sony Cyber-shot, 2002-06-12 08:21:13 | Copyrighted free use (Commons) | `Research/week02/ip-research/SOURCES.md` | Level 0 partitions are papered drywall: a stripe-chevron paper, a wall end (outside corner), an outlet (`21` §1.6, §7.1) |
| `ir01_dsc00159_20020612_082017.jpg` | `Research/week02/ip-research/stills/` | Wikimedia Commons, "Dsc00159.jpg …", same floor 56 s earlier, https://commons.wikimedia.org/wiki/File:Dsc00159.jpg_B%E1%BB%A9c_%E1%BA%A3nh_th%E1%BB%A9_2_trong_2_b%E1%BB%A9c_%E1%BA%A3nh_ngu%E1%BB%93n_g%E1%BB%91c_c%E1%BB%A7a_The_Backrooms.jpg | Copyrighted free use (Commons) | same | Painted drywall arches and soffits: where corner bead sits (`21` §1.6) |
| `a24_trailer_0115_blue_painters_tape.jpg` | `Research/week02/kit-references/a24/` | A24, *Backrooms* Official Trailer HD, https://www.youtube.com/watch?v=0HjdiohVOik , 115 s | © A24; research reference only | `Research/week02/kit-references/SOURCES.md` | A papered partition under a lay-in ceiling in the IP; the source of `Kit_DoorwayStuds` (`21` §7.1) |
| `kane_emg_0053_oak_door_lever.jpg` | `Documentation/research/interactables/proposal/media/` | Kane Pixels, "Backrooms - Everything Must Go", https://www.youtube.com/watch?v=ewZx0bnBb30 , 0:53 | © Kane Parsons; research reference only | `../interactables/proposal/media/SOURCES.md` | The IP's cased wood door in a papered wall: casing and mitres (`21` §1.6) |
| `rb_10_interiors9006_p048_049_vinyl_wallcovering.jpg` | `Documentation/research/level_transitions/images/` | *Interiors*, June 1990, pp. 48–49 (a trade advertisement), https://www.usmodernist.org/I/I-1990-06.pdf | © publisher; research use only (Red-approved download 2026-10-02) | `../level_transitions/02_real_buildings.md` (S24) | 1990 commercial vinyl wallcovering samples; a curled sample shows its white woven backing (`21` §1.7). Brand names on the page are research context only (era lock: no trademarks in the game) |

## Our own images used as evidence

| File | What | Made by |
|---|---|---|
| `images/db_C_L0F_burst_S4_V1_LA.jpg` | The frame Red judged on 2026-10-08 | Direction C pre-render, `20_dir_C.md` |
| `images/db_C_L0F_burst_S4_V2Hf50_LA.jpg`, `images/db_C_L0F_burst_S4_V5f62_LA.jpg`, `images/dir_C_strip.jpg` | The same break from V2H and V5 and the stage strips (the diagnosis in `21` §5) | same |
| `../interactables/threeview/png/Kit_DoorwayStuds_*.png` | The kit's framed opening (stud and header proportions) | interactables three-view sheets |

Crops of these frames used while writing `21` stayed in the session scratchpad. None were published.

## Research 2026-10-30 (current)

Lens: OUR CURRENT STATE (audit), workflow stage of 游戏视觉 (account A, session local_5656cffd). Read-only: no Unity, no main writes except this section, nothing downloaded. Line numbers are main's working tree on 2026-10-30.

**Main code read (Assets/Scripts/)**
- `FrontRoomsMap/FrontRoomsMapHunter.cs` L49-57 (BlowInterval, FinalBlowLead, DoorFallSeconds), L102 (BlowAudibleMetres 36, BreakAudibleMetres 40), L137 `blowsStruck`, L349 `BlowCount`, L353 `BreakSeconds`, L368 `DoorBlow`, L1240-1250 (break start: SwingAwayFrom, MarkBeingBroken, blowsStruck = 0), L1623-1627 `StopBreaking`, L1630-1669 `TickBreak`.
- `FrontRoomsMap/FrontRoomsMapWorld.cs` L83 `DoorBroken`, L91 `DoorBrokenFrom`, L197 `DoorBrokenTried`, L200 `UseResult`, L253-255 (throw 105 deg, bounce 0.18 s), L437-446 `GlassBreakRecord`, L555 `brokenDoors`, L2766-2783 (rebuild: cube "Door leaf", broken = 80 deg + crooked 3 deg), L2809 (DressDoor on chunk build), L3057-3079 `BreakDoor`, L4265-4275 (Use on a broken door), L4494 `MarkBeingBroken`, L4729 `JoltForBlow`, PoseLeaf (crooked about the top of the hinge edge), L4935-4955 (glass stage commit), L5060-5083 (Throw / Bounce), L5838 (ResetDoorForTools).
- `FrontRoomsShots/FrontRoomsShotTimings.cs` L103-106 `BrokenDoor`, L166-175 `DoorBreak` (Damage1At / Damage2At / RevealHold unread).
- `FrontRoomsMap/FrontRoomsLevelProfile.cs` L263 `V2Break`, L328-332 (v1 tier break factors).
- `FrontRooms3DGame.cs` L1011, L1039-1048, L2330; `FrontRoomsRelayRig.cs` L39, L166-222.
- `Audio/FrontRoomsSoundDirector.cs` L540-546, L621-628 `OnDoorBroken`, L740-753 `OnDoorBlow`; `Audio/FrontRoomsSoundIds.cs` L19-23, L98-99; `Audio/FrontRoomsDoorSound.cs` L72 `MarkBroken`.
- `Rendering/Doors/FrontRoomsDoorDressEvents.cs` L31-32, L53-54; `Rendering/Doors/FrontRoomsDoorRig.cs` L106-121, L338-358 `DetachStrike`, L385 `BeatWobble`, L563-573 `ApplyStrike`; `FrontRoomsMap/FrontRoomsKitSeam.cs` L19-36 `IFrontRoomsDoorRig`.
- `Office/FrontRoomsInteractableKit.Door.cs` L54 `DressOnWebGL = false`, L62 `DressOnHandheld = true`, L64-68, L135-176 `DressDoor`, L337-344 parts rows.
- `Rendering/UI/FrontRoomsKeyHud.cs` L41, L101, L218, L277 (B.4); `Input/FrontRoomsTouchControls.cs` L126; `Input/FrontRoomsMobileHaptics.cs` L92.
- `Rendering/Glass/FrontRoomsGlassDebrisWorld.cs` header L1-50 and L58-92 (switches, ArmDistance 4, MaxArmed 2); `FrontRoomsGlassDebrisPack.cs` L1-40 (record layout); `FrontRoomsGlassVfx.cs` L1-20 (mesh particles, no Particle System module).
- `Resources/Rendering/FrontRoomsSurface.shader` (no damage layer; `_FR_MESH_UV`), `Resources/Surfaces/Door_Veneer.mat` (mesh UV, tile 1.12 x 2.62 m, 1024 x 2048 maps), `Packages/manifest.json` (no particlesystem module; no URP Decal feature asset found).
- Kit sidecars `Assets/Resources/Props/Models/Kit_DoorLeaf_Veneer_DA.json`, `Kit_DoorFrame_Wood_DA.json`; `Tools/Blender/frontrooms_kit/assets/interact_door_da_common.py` L237, L363-364 (anchors `damage_latch`, `head_dust_a/b`, read by no code).

**Docs and work read**
- `door_break/40_for_red.md` (DB1 page, 10-30), `01_aaa_doors.md` §0-§3, §8, `21_C_diagnosis.md` §0, `22_C_rework.md` header, `SOURCES.md`, `media_candidates.md` headings.
- `interactables/d16/10_design.md` §4-§10, `30_status.md`; `glass/destruction/gd3c/10_design.md` §3, §5, §6, §9.
- `/Users/redwang/FrontRoomsVisualWork/door_break/survey/SURVEY_2026-10-22.md`; DB1 work `/Users/redwang/FrontRoomsVisualWork/door_break/DB1_page/` (scripts, logs, `renders/final/stats_*.json`); `images/db1_overview.jpg` (viewed).

**Outside sources cited in the findings (all first read 2026-10-03 for `01_aaa_doors.md`; not re-read today)**
- [V1] Valve Developer Community, prop_door_rotating (L4D damage1-3 models): https://developer.valvesoftware.com/wiki/Prop_door_rotating
- [V2] Valve Developer Community, L4D Level Design / Breakable Walls: https://developer.valvesoftware.com/wiki/L4D_Level_Design/Breakable_Walls
- [F1] Frictional Games, Amnesia: The Dark Descent source, LuxProp_SwingDoor.cpp: https://github.com/FrictionalGames/AmnesiaTheDarkDescent/blob/master/amnesia/src/game/LuxProp_SwingDoor.cpp
- [F2] same repository, LuxEnemy_Grunt.cpp (damage on the BreakDoor animation's impact frame): https://github.com/FrictionalGames/AmnesiaTheDarkDescent/blob/master/amnesia/src/game/LuxEnemy_Grunt.cpp
- [F4] Frictional Games, Amnesia: The Bunker - Breaking down doors (official clip): https://www.youtube.com/watch?v=adCV8RXTWdo
- [C1] Capcom, CEDEC 2017 (RE7 baked breakables), CGWorld report: https://cgworld.jp/feature/201709-cedec2017-capcom.html
- [U1] Julien L'Heureux, The Art of Destruction in Rainbow Six: Siege, GDC 2016: https://media.gdcvault.com/gdc2016/Presentations/LHeureux_Julien_Art_Of_Destruction.pdf
- [L1] Left 4 Dead 2 Facts #73 - Common Infected vs Doors (fan footage), 0:35-0:54: https://www.youtube.com/watch?v=UHE_5DaZXk4
- [S4] The Shining (1980) - Here's Johnny! Scene (7/7), Movieclips, 1:24-2:08: https://www.youtube.com/watch?v=WDpipB4yehk
- [R1] W Tool Battering Ram Lock Side (WeddleToolCompany): https://www.youtube.com/watch?v=bAyNzsuWBgw
- Unity Scripting API, LODGroup.ForceLOD: https://docs.unity3d.com/ScriptReference/LODGroup.ForceLOD.html ; PhysicsScene.Simulate: https://docs.unity3d.com/ScriptReference/PhysicsScene.Simulate.html (engine docs, not re-read today)

No new media is wanted from this lens.

## Research 2026-10-30 (model)

Lens: MODEL STRUCTURE of destructibles (how breakable doors, walls and props are built as meshes), workflow stage of 游戏视觉 (account A, session local_5656cffd). Web pages were read, nothing was downloaded into the project. Video timestamps are given only where they were checked; YouTube captions could not be read from this session, so talk sections are cited by slide or by article, not by time.

**Engines and tools (docs)**
- [M-UE1] Epic, Unreal Engine, Fracturing Geometry Collections user guide (hierarchy = root bone + child bones, one level per fracture pass, Uniform / Cluster / Radial / Planar / Slice / Brick / Mesh / Custom tools, noise on cuts, Grout): https://dev.epicgames.com/documentation/en-us/unreal-engine/fracturing-geometry-collections-user-guide
- [M-UE2] Epic, Destruction Overview (connection graph from nearest neighbours, strain per connection, clustering joins mass into one rigid body, cache system for replay): https://dev.epicgames.com/documentation/en-us/unreal-engine/destruction-overview
- [M-UE3] Epic, Python API GeometryCollection (`damage_threshold` per cluster level, size-specific thresholds): https://dev.epicgames.com/documentation/en-us/unreal-engine/python-api/class/GeometryCollection?application_version=5.1
- [M-UE4] Epic, Geometry Collections user guide (duplicated material slots for internal faces) and FUniformFractureDataflowNode `InternalMaterialID`: https://dev.epicgames.com/documentation/en-us/unreal-engine/geometry-collections-user-guide ; https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Plugins/GeometryCollectionNodes/FUniformFractureDataflowNode
- [M-UE5] Epic issue UE-358545 (remove-on-break timers leave a collection rendering but not interactable): https://issues.unrealengine.com/issue/UE-358545
- [M-HO1] SideFX, Houdini destruction: wood (RBD Material Fracture, grain cuts in two directions, cut planes across the grain, splinter density/length, clusters, glue strength between grain lines and cut planes): https://www.sidefx.com/docs/houdini/destruction/wood.html
- [M-HO2] SideFX, Houdini to Unreal Chaos integration (`unreal_gc_piece`, `unreal_gc_cluster`, two-level example): https://www.sidefx.com/docs/houdini/unreal/chaosintegration.html
- [M-HO3] SideFX, Vertex Animation Textures in Unity (rigid-body sims baked to position/rotation textures, URP VAT 3.0 package): https://www.sidefx.com/tutorials/vertex-animation-textures-in-unity/
- [M-RF1] RayFire for Unity: launch coverage (Shatter pre-fracture incl. slabs and wood splinters; runtime demolition only on Windows/macOS/Linux at launch, pre-fracture on all platforms): https://www.cgchannel.com/2019/07/rayfire-studios-releases-rayfire-for-unity/ ; Reference Demolition workflow (prefragment, export meshes, prefab, swap on demolition), Qiita: https://qiita.com/azarashin/items/621c9d13c1f559f0ab5a
- [M-UN1] Unity Scripting API, HingeJoint / Joint.breakForce (joint breaks above a force; OnJointBreak): https://docs.unity3d.com/ScriptReference/HingeJoint.html ; https://docs.unity3d.com/ScriptReference/Joint-breakForce.html (not re-read today)

**Shipped games (talks, slides, articles)**
- [M-R6] Julien L'Heureux (Ubisoft Montreal), The Art of Destruction in Rainbow Six: Siege, GDC 2016. Slides read for this lens (text of the PDF): procedural vs pre-fragmented (slide ~3), "objects are separated into different parts based on their physical material" (~29), hierarchical decomposition + connection-based leaf graph (~30-32), 2D polygon cutters on planar surfaces, Weiler-Atherton + ear clipping, extrude to 3D (~35-40), texture cutter in UV (~42), cut decorations vs feature-bound decorations on edges (~44-47), collision from simplified 2D convex shapes (~50-51), debris = no cut dynamic fragments, well-placed replacements, instanced, recycled (~52), budget ~6 ms per wall for 2 procedural layers + pre-fragmented (~56), benchmarks incl. 2 drywall + 2 wood layers 8.1 ms PC (~72), pre-destruction synced to end of animation (~62), RNG seeded by impact position (~79). PDF: https://media.gdcvault.com/gdc2016/Presentations/LHeureux_Julien_Art_Of_Destruction.pdf ; Vault: https://gdcvault.com/play/1023003/The-Art-of-Destruction-in ; Ubisoft interview: https://news.ubisoft.com/en-us/article/4GHX2yepSaKkflLjLAlpwO/the-art-of-destruction-in-rainbow-six-siege-an-interview-with-julien-lheureux
- [M-FB1] Robert Kihl (DICE), Destruction Masking in Frostbite 2 using Volume Distance Fields, SIGGRAPH 2010 (slide 7: Frostbite 1 needed a UV-mapped mask per destructible part; slide 9: designer-placed spheres -> SDF volume ~2 m/texel; slides 12-18: forward vs deferred vs triangle culling, 0.20-0.77 ms PS3): https://www.slideshare.net/DICEStudio/siggraph10-arrdestruction-maskinginfrostbite2
- [M-FB2] EA, Battlefield Labs: destruction (staged walls, persistent surface damage before breaking, rubble stays): https://www.ea.com/games/battlefield/battlefield-6/news/battlefield-labs-destruction ; EA, How Battlefield 6 redefined destruction (part-based destruction and debris spawning): https://www.ea.com/news/how-battlefield-6-redefined-destruction
- [M-CT1] Johannes Richter (Remedy), Destructible Environments in Control: Lessons in Procedural Destruction, GDC Summer 2020 (30:10; section timestamps NOT checked). Video: https://www.youtube.com/watch?v=kODJsQGXanU ; Vault: https://www.gdcvault.com/play/1026820/Destructible-Environments-in-Control-Lessons
- [M-CT2] CGWorld report of [M-CT1] (Japanese): material metadata per minimal unit, nested groups of pre-joined pieces, Houdini HDA, three effect tiers (rigid bodies / particles + rigid hierarchies + decals / flipbooks), active rigid bodies capped at 200, figure 07a-c: https://cgworld.jp/feature/202008-gdccontrol.html
- [M-CT3] Johannes Richter, Creating Reactive Environments for CONTROL (Houdini HIVE 2020): https://www.youtube.com/watch?v=VFUqylhCWY4 (not watched)
- [M-FN1] SideFX, Making the procedural buildings of THE FINALS using Houdini (Embark, 2026-02): pre-fractured in Houdini, seams hidden until damaged, watertight input so interior faces are generated, door frames and mouldings merged into the building mesh so they follow the fracture, merged convex hulls: https://www.sidefx.com/community/making-the-procedural-buildings-of-the-finals-using-houdini/
- [M-FN2] Isaksson (Embark), Engineering Mayhem: Technical Deep-Dive into Environmental Destruction in THE FINALS, GDC 2024 (abstract only): https://gdcvault.com/play/1034280/Engineering-Mayhem-Technical-Deep-Dive
- [M-RFG1] Chad Greene (Volition), The destructible world building of Red Faction: Armageddon, Game Developer, 2010 (hidden shards unhidden on destruction, shard size per material, "world anchoring"): https://gamedeveloper.com/design/the-destructible-world-building-i-red-faction-armageddon-i-
- [M-RFG2] Red Faction Wiki, Geo-Mod 2.0 (pre-broken meshes driven by stress and impact; secondary source): https://www.redfactionwiki.com/wiki/GeoMod_2.0 ; James Hague interview (structures stressed when supports go): https://www.redfactionwiki.com/wiki/Interview_with_James_Hague
- [M-TD1] Steven Wittens, Teardown Frame Teardown (each object = 3D volume texture, 1 byte per voxel into a 256-entry colour+material palette; blasted objects split into disconnected chunks, each a new object): https://acko.net/blog/teardown-frame-teardown/
- [M-TD2] Software Engineering Daily, Teardown and voxel-based rendering with Dennis Gustafsson (transcript): https://softwareengineeringdaily.com/wp-content/uploads/2024/12/SED1772-Teardown.txt
- [M-RE7] Capcom, CEDEC 2017 RE7 baked breakables (CGWorld): https://cgworld.jp/feature/201709-cedec2017-capcom.html (= [C1] in `01`)
- [M-L4D] Valve, prop_door_rotating (L4D damage1-3 door models) and L4D Breakable Walls (hidden broken-edge model): https://developer.valvesoftware.com/wiki/Prop_door_rotating ; https://developer.valvesoftware.com/wiki/L4D_Level_Design/Breakable_Walls (= [V1][V2] in `01`)
- [M-AM] Frictional, LuxProp_SwingDoor.cpp (intact + DamageMesh1/2 + BrokenEntity): https://github.com/FrictionalGames/AmnesiaTheDarkDescent/blob/master/amnesia/src/game/LuxProp_SwingDoor.cpp (= [F1] in `01`)
- [M-HLA] Valve Developer Community, Half-Life: Alyx Workshop Tools, Advanced Breakable Props and Level Design/Doors: https://developer.valvesoftware.com/wiki/Half-Life:_Alyx_Workshop_Tools/Modeling/Advanced_Breakable_Props ; https://developer.valvesoftware.com/wiki/Half-Life:_Alyx_Workshop_Tools/Level_Design/Doors (both returned HTTP 403 to this session; NOT read)

**Real door construction**
- [M-WD1] BNP Media continuing education, What Architects Need to Know About Specifying Wood Doors (3/5/7-ply, five-ply standard for class A offices, crossbands, particleboard / SCL / stave cores, five-ply stiles 1-3/8 in, bonded core: stiles and rails glued to the core before the plies): https://continuingeducation.bnpmedia.com/courses/icynene/what-architects-need-to-know-about-specifying-wood-doors/2
- [M-WD2] WDMA, The Door Glossary: https://www.wdma.com/the-door-glossary ; VT Industries, architectural wood door construction poster: https://absupply.net/pdf/VT_Industries_AWD_Door_Construction_Poster.pdf (search hits, not read)

**Footage with checked timestamps (from `01`, reused)**
- Left 4 Dead 2 Facts #73 (damage-model door holes), 0:35-0:54: https://www.youtube.com/watch?v=UHE_5DaZXk4
- The Shining (1980), bathroom door, 1:24-2:08: https://www.youtube.com/watch?v=WDpipB4yehk
- Resident Evil 7, Jack's wall entrance (one-frame burst, hole stays), 0:09-0:24: https://www.youtube.com/watch?v=5xEJMY0EcFE

## Research 2026-10-30 (reference)

Lens: REAL DOOR FAILURE + FILM/GAME STAGING. Workflow stage of 游戏视觉 (account A, session local_5656cffd). Nothing downloaded. Builds on `02_real_door_failure.md` (S1-S37) and `01_aaa_doors.md` ([V1]-[R3]); those ids are reused, not repeated. Status: [read] = page text read; [search] = search-result text only; [frames] = frames looked at in the built-in browser; [title] = title/description only, not viewed.

**Real doors: construction and failure (new)**
- [RF16] "More to Pivots Than Meets the Eye", *Locksmith Ledger*, 2006-01-01: bottom pivot carries the whole leaf weight, top pivot only keeps alignment; center-hung pivots are the double-acting kind. https://www.locksmithledger.com/door-hardware/article/10231101/more-to-pivots-than-meets-the-eye [read]
- [RF17] Hager 451 double-lipped strike for center-hung doors ("opened in both directions without damage to frame"). https://www.hagerco.com/commercial-hinges/pivots/451-double-lipped-strike-center-hung-doors [search; page refused the reader]
- [RF18] r/accesscontrol, "Strike or lock recommendation", 2024-10-12: a beveled latch can be pushed through a double-acting strike, so double-acting doors need a deadbolt. https://www.reddit.com/r/accesscontrol/comments/1g25irl/strike_or_lock_recommendation [search]
- [RF19] ABH 012 center-hung pivot + combination double-lipped strike "rescue stop" set (door edge bevel 1/8" in 2"). https://fbisecurity.com/abh-012-center-hung-pivot-combination-double-lipped-strike-rescue-stop-set.html [search]
- [RF20] US 4,832,388, door assembly resisting forced entry (battering "adjacent to the door lock assembly, or adjacent to the center hinge"). https://patents.google.com/patent/US4832388 [search]
- [RF21] Rixson 340 center-hung top pivot cut sheet. https://accesshardware.com/wp-content/uploads/2014/08/Rixson-340-Top-Pivot-Center-Hung-is.pdf [search]
- [RF22] NYC HPD Standard Specification 8B, Solid Core Wood Doors. https://www.nyc.gov/assets/hpd/downloads/pdfs/services/08b-solid-core-wood-doors.pdf [search]. Its hollow-core sibling (= S22) gives lock block 20" x 2-1/4" min, one each side; honeycomb cells max 2" x 2" [search].
- [RF23] "Reinventing the flush door", *FDMC* / Woodworking Network (crossband added "to eliminate telegraphing"; core calibrated, then veneer). https://www.woodworkingnetwork.com/magazine/fdmc-magazine/reinventing-flush-door [search]
- [RF24] Newfoundland & Labrador master spec 08 14 16 (solid particleboard core: stile and rail frame bonded to core, wood lock blocks, 7-ply). https://www.gov.nl.ca/ti/files/works-masterspec-081416.doc [search]
- [RF25] Kuiken Brothers / REEB flush and molded doors catalog (solid particle core with wood stiles and rails and a triple bottom rail "for additional screw holding power"). https://www.kuikenbrothers.com/wp-content/uploads/KUIKEN-BROTHERS-REEB-FLUSH-AND-MOLDED-DOORS-CATALOG.pdf [search]
- [RF26] SDI 108-2023, Recommended Selection and Usage Guide for Standard Steel Doors (Level 2 heavy duty = 18 ga, 0.042"). https://www.steeldoor.org//wp-content/uploads/2020/02/SDI_108.pdf [search; PDF text not extractable here]
- [RF27] Bond University, "Into the breach" (ram with a force transducer against a breaching door with graded inserts), 2026-06. https://tru.bond.edu.au/article/breach [search; reader refused, results not seen]
- [RF28] TUFF STRIKE, "Door Reinforcement Kick In Test", 2018-10-08 (a door kicked in during an earlier demo, frame repaired). https://www.youtube.com/watch?v=pJrHemhnyKQ [read description]
- [RF29] Beyond Locks, "Just how easy is it to kick in a door", 2016-07-14, 2:03. https://www.youtube.com/watch?v=zwij_VG09Ws [title; frames did not load]

**Film (new)**
- [RF1] TCM, "Behind the Classics: The Haunting (1963)": Wise on the buckling door (laminated wood, "a strong prop man on the other side who would push it"); ghost sounds played back on set. https://www.tcm.com/articles/behind-the-classics/961615/behind-the-classics-the-haunting-1963 [read]
- [RF2] DVD Journal quick review, *The Haunting* (1963): the oak door "bulges inward". https://www.dvdjournal.com/quickreviews/h/haunting63.q.shtml [search]
- [RF3] *The Haunting (1963 film)*, Wikipedia. https://en.wikipedia.org/wiki/The_Haunting_(1963_film) [search]
- [RF4] *Halloween (1978)*, Halloween Specials Wiki (Michael tears a hole in the closet door; the hanger). https://halloweenspecials.fandom.com/wiki/Halloween_(1978_film) [search]; Creepy Catalog, louvered closet doors: https://creepycatalog.com/creeptober-night-1-halloween-1978/ [search]

**Games (new)**
- [RF5] Dead by Daylight Wiki, "Breakable Walls" (2.34 s break, was 2.6 s before 6.1.0; Killer only; coloured like Pallets; Alert perk on Break; added 3.6.0). https://deadbydaylight.wiki.gg/wiki/Breakable_Walls [read]
- [RF6] DbD, "Here's Hunty! New breakable wall animations". https://www.youtube.com/watch?v=c1icEd48TsU [title]; all-killer compilation https://www.youtube.com/watch?v=GfOyCa1tZ9I [title]
- [RF7] SCP: Secret Laboratory BWIKI, "Door Mechanics" (door HP: EZ 90, LCZ/HCZ 150, armouries 200, SCP-106 700, warhead 725; Normal/Strong/Very Strong tiers). https://wiki.biligame.com/scpsl/Door_Mechanics [read; page marked outdated]
- [RF8] SCP:SL 10.0.2 notes (PrySpeed; 096 charges through the surface gate) https://wiki.biligame.com/scpsl/Updates/10.0.2 [search]; 10.2 (two control-room doors "require several strikes from SCP-096") https://www.player.one/scp-secret-laboratory-update-102-door-coding-changed-new-admin-console-and-138609 [search]
- [RF9] Outlast Wiki, "Prison" (Walker heard screaming and bashing a door). https://outlast.fandom.com/wiki/Prison [search]; Portforward, Outlast Ch. 4 Male Ward ("when you hear Chris Walker start banging on a door ... sprint"). https://portforward.com/games/walkthroughs/Outlast/Chapter-4-Male-Ward-.htm [search]; "Outlast on Insane: Chris Walker prison block" (wait for his first hit before opening). https://www.youtube.com/watch?v=aIwmpB223oI [search snippet]
- [RF10] SI Videogames, Outlast Trials beginner tips (barred doors: DOOR BASHER = 2 bashes, SMASH = 1). https://videogames.si.com/guides/outlast-trials-beginners-guide-tips-trick [search]
- [RF11] Bear Gaming Asia, "Can you stop the Chainsaw Man from breaking the door?" (RE4 2023), 1:02. https://www.youtube.com/watch?v=Se9tHEvff40 [frames 0:06-0:54]
- [RF12] GamesRadar, "How to survive the Resident Evil 4 Remake village" (barricading a house; enemies destroy the main door, which then serves as an exit). https://gamesradar.com/resident-evil-4-village [search]
- [RF13] Steam discussion, Alien: Isolation locked rooms (~15 s lock, no vent; noise at the locked door sends the Alien to a vent). https://steamcommunity.com/app/214490/discussions/0/3416559391442773949 [search]
- [RF14] Friday the 13th: The Game, Steam achievements ("This Door Won't Open": break down 100 doors). https://steamcommunity.com/stats/438740/achievements [search]; "STUN JASON EVERYTIME He Breaks a Door". https://www.youtube.com/watch?v=w33jQ_s-PAs [title]

**Reused with checked timestamps:** The Shining [S4] 1:16-2:10; L4D2 [L1] 0:34-0:56; RE7 [C4] 0:09-0:24; Amnesia: The Bunker [F4] 0:19-0:30; W Tool ram [R1]; flat charge slow motion [R2]; Amnesia TDD source [F1][F2].

## Research 2026-10-30 (texture)

Lens: TEXTURE / MATERIAL STRUCTURE of destructibles (inner-face materials, damage masks, decals, trim sheets, detail normals, stage packing, persistence and LODs, shader cost), workflow stage of 游戏视觉 (account A, session local_5656cffd). Web pages were read; nothing was downloaded into the project. Two GDC/SIGGRAPH PDFs were fetched by the web tool into the session's tool-results cache only (not into the project) so their slide text could be read; slide numbers below are PDF page numbers measured from that text. No video timestamps were checked for this lens; talks are cited by slide.

**Shipped games and talks**
- [T-FB] Robert Kihl (DICE), Destruction Masking in Frostbite 2 using Volume Distance Fields, SIGGRAPH 2010 Advances course. Slide 7: Frostbite 1 used a UV-mapped mask per destructible part ("time consuming workflow"); slide 9: designer spheres -> signed distance field in a volume texture, ~2 m per texel, one per masked object; slide 10: detail texture added to the distance before the threshold, saturate(d * mult + offset); slide 11: atlas with padding; slides 12-13: forward branch vs deferred volume decal; slide 14: projected detail normal needs tangents; slide 16: compute mip in shader at mask borders; slide 18: PS3 cost 0.77 / 0.45 / 0.31 / 0.20 ms. Slides: https://www.slideshare.net/DICEStudio/siggraph10-arrdestruction-maskinginfrostbite2 ; course PDF: https://www.advances.realtimerendering.com/s2010/Kihl-Destruction%20in%20Frostbite(SIGGRAPH%202010%20Advanced%20RealTime%20Rendering%20Course).pdf
- [T-R6] Julien L'Heureux (Ubisoft Montreal), The Art of Destruction in Rainbow Six: Siege, GDC 2016 (PDF pages): 29 parts split by physical material; 42 texture cutter (tileable motif generated in UV space, then mapped to surface space; artists author vector coordinates); 45 decorations vs GPU decals (decorations are real geometry, preserved through destruction, cost more CPU/render/memory); 46 cut decorations (planar meshes cut along with the surface); 47-48 feature-bound decorations (on edges and vertices, protrude, vanish with their feature); 54 debris = no procedurally cut dynamic fragments, instanced replacements; 58 budget ~6 ms per wall, GPU memory 25 MB, RAM 200 MB data; 74 benchmarks (bullet hole 0.33 ms PC; 2 drywall + 2 wood layers 8.1 ms PC); 76 replicate events, not mesh states; 87 rollback needs a full surface backup per step. https://media.gdcvault.com/gdc2016/Presentations/LHeureux_Julien_Art_Of_Destruction.pdf
- [T-CT] CGWorld report of Remedy's Control GDC 2020 talk: dents and holes on non-destructible floors/walls are decals made in Houdini or Substance Painter; three effect tiers, figures 07a-c (rigid body / + particles / + decals). https://cgworld.jp/feature/202008-gdccontrol.html (talk: see [M-CT1] above)
- [T-ID] Adrian Courreges, DOOM (2016) graphics study: decals stored in a texture atlas and applied while meshes are drawn in the clustered forward pass, up to 256 decals per cluster. https://www.adriancourreges.com/blog/2016/09/09/doom-2016-graphics-study/
- [T-TR] Morten Olsen (Insomniac), The Ultimate Trim: Texturing Techniques of Sunset Overdrive, GDC 2015 (Vault abstract): https://gdcvault.com/play/1022323/The-Ultimate-Trim-Texturing-Techniques ; CG Channel summary (standard strip layout, 45-degree bevel normals): https://www.cgchannel.com/?p=105538

**Engines and tools (docs)**
- [T-UE1] Epic, Geometry Collections user guide (internal faces get their own duplicated material slots): https://dev.epicgames.com/documentation/en-us/unreal-engine/geometry-collections-user-guide
- [T-UE2] Epic, GeometryCollectionNodes (InternalMaterialID for new internal faces; select internal faces; auto-unwrap UVs per channel, up to 8; outside/inside material node deprecated in 5.6): https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Plugins/GeometryCollectionNodes ; https://dev.epicgames.com/documentation/unreal-engine/API/Plugins/GeometryCollectionNodes/FUniformFractureDataflowNode
- [T-UE3] Epic, PlanarCut EBakeAttributes in FractureAutoUV.h (None, DistanceToExternal, AmbientOcclusion, Curvature, NormalX/Y/Z, PositionX/Y/Z) and FTextureAttributeSettings (ToExternal_MaxDistance, AO rays/blur): https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Plugins/PlanarCut/UE__PlanarCut__EBakeAttributes ; https://dev.epicgames.com/documentation/unreal-engine/API/Plugins/PlanarCut/FTextureAttributeSettings
- [T-UE4] Epic, ApexDestruction FFractureMaterial::UVScale (interior UV scale, geometric distance per unit texture distance, default 100,100): https://dev.epicgames.com/documentation/unreal-engine/API/Plugins/ApexDestruction/FFractureMaterial/UVScale?application_version=5.2
- [T-UE5] Epic, UDK Fracture Tool (UE3: interior faces textured through a second UV set of the source mesh): https://api.unrealengine.com/udk/Three/FractureTool.html
- [T-UE6] Epic community tutorial, UE4 height-blended materials (vertex paint drives HeightLerp): https://forums.unrealengine.com/t/610658
- [T-HO1] SideFX, RBD Material Fracture (Interior Detail noise scaled by distance to surface; wood cuts along the longest axis) and destruction overview: https://www.sidefx.com/docs/houdini/nodes/sop/rbdmaterialfracture-.html ; https://www.sidefx.com/docs/houdini/destruction/overview.html ; forum, interior UVs via the Inside/Outside groups: https://www.sidefx.com/forum/topic/90654/
- [T-BL] Blender Cell Fracture, "Material index for interior faces" (2012 commit) and how-to: https://lists.blender.org/pipermail/bf-extensions-cvs/2012-July/003110.html ; https://www.blendernation.com/2020/05/03/daily-blender-tip-cell-fracture-inner-material/ ; Blender 4 bug report: https://projects.blender.org/blender/blender-addons/issues/105231
- [T-RF] RayFire for Unity release thread (inner material and mapping scale in the Interior group; developer reply): https://discussions.unity.com/t/released-rayfire-for-unity-runtime-demolition-and-fragmentation-plugin/742490
- [T-UN1] Unity 6.6 manual, Decal Renderer Feature reference (DBuffer: DepthNormal prepass, no OpenGL/GLES, Albedo/Normal/MAOS; Screen Space: after opaques, normal reconstruction Low/Medium/High): https://docs.unity.com/en-us/engine/6000.6/manual/visual-effects/decals/renderer-feature-decal/reference
- [T-UN2] URP 17.6.0 package source (read locally in a clone's PackageCache): Shaders/Lit.shader `multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3`, Shaders/LitForwardPass.hlsl `ApplyDecalToSurfaceData`; main's FrontRoomsSurface.shader has neither (checked 2026-10-30).
- [T-UN3] Unity Discussions, URP decals on WebGL (DBuffer unsupported on GL backends; Screen Space only): https://discussions.unity.com/t/urp-decals-on-webgl/881748
- [T-UN4] Unity manual, material properties and batching (a MaterialPropertyBlock removes SRP Batcher compatibility): https://docs.unity3d.com/6/Documentation/Manual/DrawCallBatching-Properties.html ; https://docs.unity3d.com/Manual/SRPBatcher-Incompatible.html
- [T-UN5] Unity manual, sample a 2D texture array in a shader: https://docs.unity.com/en-us/engine/6000.3/manual/materials-and-shaders/textures/class-texture2darray/use-in-shader
- [T-UN6] Unity URP Lit shader, Detail Inputs (mask in alpha, overlay blend): https://docs.unity3d.com/Manual/urp/lit-shader.html
- [T-UN7] Unity HDRP Layered Lit (layer mask channels, vertex colour Multiply/Add, height-based blend with Height Transition): https://docs.unity3d.com/Packages/hdrp/manual/Layered-Lit-Shader.html

**Real 1990 door materials (what the inner faces must show)**
- [T-WD] Section 081416 Flush Wood Doors (East Central College; a modern ARCOM spec, text read from the PDF): particleboard core ANSI A208.1 LD-1/LD-2; 5 in blocking at top rail for closers; five or seven plies; stiles and rails bonded to the core, then abrasive planed before veneering; telegraphing limit 0.01 in in 3 in. https://eastcentral.edu/wp-content/uploads/sites/20/2014/11/081416-FL-FLUSH-WOOD-DOORS.pdf (period equivalent: the 1998 Spec Disk sections already cited in `02`)
- [T-PB] Wang et al., Effect of crack length on fracture behaviour of particleboard, BioResources 18(1) 2023 (three-layer board: fine face layers, coarse core; face layer denser and stronger) (search result summary, PDF not opened): https://bioresources.cnr.ncsu.edu/wp-content/uploads/2022/11/BioRes_18_1_175_Wang_K_Effect_Crack-Length_Fracture_Behavior_Particleboard_20444.pdf
- [T-HM] Hollow metal cores (search result summaries, not opened): University of Houston master spec 08 11 13 (kraft honeycomb, polystyrene, polyurethane, polyiso, mineral board or vertical steel stiffeners): https://www.uh.edu/facilities-services/departments/fpc/master-specs/08-11-13-hollow-metal-doors-and-frames.pdf ; Florida Tech 08110 (20 ga Z stiffeners at no more than 6 in o.c.): https://www.fit.edu/media/site-specific/wwwfitedu/facilities/standards/8/08110-Hollow-Metal-Doors--Frames.pdf ; Allegion, SDI 100 core letters are obsolete: https://kc.allegion.com/kb/article/what-are-the-door-cores-a-b-c-d-e-f-per-sdi-100 ; Beacon, 5 core types: https://beaconcdl.com/blogs/news/5-core-types-for-hollow-metal-doors ; cheap steel door = two 20 ga sheets and cardboard: https://idighardware.com/2017/03/ff-wobbly-door-video/
- [T-WF] Wood failure types (splintering tension vs brash tension) (search result summaries): https://web.mst.edu/jthomas/classes/2211/lessons/wood/failure/index.html ; https://www.fwrc.msstate.edu/lumber-testing/splintering-tension.php

## Research 2026-10-30 (dust and grit)

Stage "research + spec: dust and grit" of the door-break production workflow (游戏视觉, account A, session local_5656cffd). Nothing downloaded; footage was viewed in the YouTube player as contact sheets and not saved. The web sources and footage times are listed with tags [DG-F*], [DG-G*], [DG-M*], [DG-P*] and [DG-B*] in `54_dust_grit.md` §6 and §2.1. The wanted media is in `media_candidates.md`, "Research 2026-10-30 (dust and grit)". Our own evidence: prototype frames `W/door_break/DB2/out/full/S1_C1_000752.jpg`, `S3_C2_000681.jpg`, `S3_C2_000748.jpg`, `S3_C2_000768.jpg` (W = `/Users/redwang/FrontRoomsVisualWork`).
