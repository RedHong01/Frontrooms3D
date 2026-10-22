# Tutorial research · sources ledger

One row per source used in `01_research.md` and `10_proposal.md`.

**How each source was read:**
- **read (me)**: re-opened and read by 教程设计 on 2026-10-22.
- **read (wf)**: opened and read by an agent of the 10-22 research workflow `wf_98230184-c3f`, not re-opened by me.
- **search**: only a search-engine summary was read (page blocked, paywalled or too large).

**What was not done:**
- No media was downloaded. No images, clips or PDFs were saved.
- No quotes longer than a phrase were copied: the docs paraphrase.
- The workflow's raw results stay outside the repo, in 关卡设计's session folder (`subagents/workflows/wf_98230184-c3f/journal.jsonl`).

## 1. External sources

### Re-verified by 教程设计 (2026-10-22)

| ID | Source | URL | Read | Used for |
|---|---|---|---|---|
| T1 | Half-Life 2 developer commentary transcript (Combine OverWiki): nodes cn_134 Kirchmer (birds toward the Citadel), cn_097 Riller (players missed the opened gate), cn_127 Speyrer (a crate as a learned sign), cn_039 Smith (HUD markers ignored), cn_217 Speyrer (poison zombie heard before seen) | https://combineoverwiki.net/wiki/Developer_commentary/Half-Life_2 | read (me) | §3.2, R7, R8 |
| T2 | AudioGang (G.A.N.G.), interview with Phil Kovats and Jonathan Lanier on The Last of Us audio, 2014-09-06 | https://www.audiogang.org/?p=2615 | read (me) | §3.4 portal propagation, "lead the player into spaces" |
| T3 | Mobius Digital, "Separating the signal from the noise" (Alex Beachum, Outer Wilds Signalscope), 2016-09-23 | https://www.mobiusdigitalgames.com/news/separating-the-signal-from-the-noise | read (me) | §3.4 beacon failure modes |
| T4 | Wikipedia, *The Exit 8* | https://en.wikipedia.org/wiki/The_Exit_8 | read (me) | §5, R6 |
| T5 | Sebastian Long, "The last days of Control", Game Developer, 2020-06-18 | https://www.gamedeveloper.com/design/the-last-days-of-control | read (me) | §5 signs-only failed, screens, padlock |
| T6 | Philippe Morin, "Horror in the making: how Red Barrels outlasted Outlast", Game Developer, 2015-01-29 | https://gamedeveloper.com/design/horror-in-the-making-how-red-barrels-outlasted-i-outlast-i- | read (me) | §2 scare early, simplify door controls |
| T7 | Lightnow blog, "Left 4 Dead game uses lighting to instill fear and horror", 2009-07-08 (names no studio and no headlights) | https://lightnowblog.com/2009/07/left-4-dead-game-uses-lighting-to-instill-fear-and-horror | read (me) | §3.1 one warm glow |
| T8 | Tommy Thompson, "Revisiting the AI of Alien: Isolation", Game Developer, 2020-05-20 | https://www.gamedeveloper.com/design/revisiting-the-ai-of-alien-isolation | read (me) | §0.5, R8 staged hunter |
| T9 | Boris the Brave, "Dungeon generation in Unexplored", 2021-04-10 | https://www.boristhebrave.com/2021/04/10/dungeon-generation-in-unexplored/ | read (me) | §4 key→lock edge, "Pray for help" |
| T10 | Tommy Thompson, "Unexplored's secret: cyclic dungeon generation", Game Developer, 2021-01-28 | https://www.gamedeveloper.com/design/unexplored-s-secret-cyclic-dungeon-generation- | read (me) | §4 lock before key (path A / path B) |
| T11 | GamerGuides, Resident Evil 2 (2019) walkthrough, Leon A (gas station) | https://gamerguides.com/resident-evil-2-2019/guide/walkthrough/leon-a | read (me) | §2, §4 key round the corner, locked door |
| T12 | Inven Global, "Horror reimagined: why Resident Evil 7 had to get rid of zombies" (GDC 2017), 2017-03-06 | https://www.invenglobal.com/articles/1270/horror-reimagined-why-resident-evil-7-had-to-get-rid-of-zombies | read (me) | §2 fear drops once understood; no basic hints |
| T13 | PCGamesN, Amnesia: The Bunker map, updated 2023-06-07 | https://www.pcgamesn.com/amnesia-the-bunker/map | read (me) | §4, §5 wall map not carried; sub-maps at area entrances |
| T14 | Ubisoft, Rainbow Six Siege status report (sound propagation), 2020-06-11 | https://www.ubisoft.com/en-us/game/rainbow-six/siege/news-updates/HyDyYTuRkWtyYQ3WDFeEL/rainbow-six-siege-status-report | read (me) | §3.4 spaces + connection points |
| T15 | Audiokinetic, "Reverb needs spatialization too: rooms and portals in Wwise Spatial Audio" | https://www.audiokinetic.com/en/blog/rooms-and-portals-with-wwise-spatial-audio | search (403 twice) | §3.4 portal position, diffraction → low-pass |
| T16 | Valve, Steam Audio feature page (pathing, transmission, Unity and FMOD plug-ins) | https://partner.steamgames.com/doc/features/steam_audio | read (wf) | §3.4 |
| T17 | A.C.C. Warnock, *Factors affecting sound transmission loss*, Canadian Building Digest CBD-239, NRC, 1985 | https://web.mit.edu/parmstr/Public/NRCan/CanBldgDigests/cbd239_e.html | read (me) | §3.4 mass law: about 6 dB more loss per doubling of frequency |
| T18 | Stepan Boev, "Sound propagation in Hitman", GDC Europe 2015 slides | https://twvideo01.ubm-us.net/o1/vault/gdceurope2015/Boev_Stepan_Sound%20Propagation%20in.pdf | search | background: per-portal occlusion |
| T19 | GDC 2017, Gears of War 4 Project Triton session | https://gdcvault.com/play/1024008/-Gears-of-War-4 | search | background: precomputed wave acoustics |

### Read by the workflow (wf) or seen as search summaries

| ID | Source | URL(s) | Read | Used for |
|---|---|---|---|---|
| T20 | Wikipedia, *Dead Space* (2008); Engadget on Schofield's "no HUD, no cutscenes" (2008-06-24) | https://en.wikipedia.org/wiki/Dead_Space_(2008_video_game) · https://www.engadget.com/2008-06-24-scared-stiff-why-should-we-care-about-dead-space.html | read (wf) | §2, §6 |
| T21 | Game Developer, "Dead Space: storytelling through level design"; "Designing Dead Space's immersive user interface" (2013-06-12) | https://gamedeveloper.com/design/dead-space---storytelling-through-level-design · https://gamedeveloper.com/design/video-designing-i-dead-space-i-s-immersive-user-interface | read (wf) | §2 blood trail, locator |
| T22 | Silent Hill Memories, SH2 walkthroughs (east side; Wood Side Apartments) | https://www.silenthillmemories.net/sh2/walkthrough_01_east_south_side_en.htm · https://www.silenthillmemories.net/sh2/walkthrough_02_wood_side_apts_en.htm | read (wf) | §2, §5 map on the car seat, noticeboard |
| T23 | Automaton, SH2 remake tips (map annotations, radio); Gameblog preview | https://automaton-media.com/en/tips/5-essential-tips-for-exploring-the-silent-hill-2-remake/ · https://www.gameblog.fr/jeu-video/ed/impressions/silent-hill-2-remake-preview-impressions-ps5-598081 | search | §2 |
| T24 | Wikipedia, *Resident Evil 7: Biohazard* | https://en.wikipedia.org/wiki/Resident_Evil_7:_Biohazard | read (wf) | §2 tapes |
| T25 | Vandal, RE2 remake FAQ (red/blue rooms, items seen); GameRevolution (tried doors) | https://vandal.elespanol.com/en/resident-evil-2-remake/resident-evil-2-remake-frequently-asked-questions-and-answers.html · https://www.gamerevolution.com/?p=485615 | read (wf) | §4 the map remembers |
| T26 | GameSkinny, RE7 guest house; Samurai Gamers, RE7 tape locations | https://www.gameskinny.com/tips/resident-evil-7-exploring-the-guest-house/ · https://samurai-gamers.com/resident-evil-7/video-tape-locations | read (wf) | §2 tape shows the hidden route in the room you watch it in |
| T27 | Game Developer, "How Creative Assembly brought fear to Alien: Isolation"; "The perfect organism: the AI of Alien: Isolation" | https://gamedeveloper.com/design/how-creative-assembly-brought-fear-to-i-alien-isolation-i- · https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation | read (wf) | §2, §6 scripting would kill tension |
| T28 | GameCritics, Alien: Isolation review (timing to first glimpse); MCV, "18 things we learned about Alien: Isolation"; GosuNoob consoles | https://gamecritics.com/mike-suskie/alien-isolation-review/ · https://www.mcvuk.com/development-news/18-things-we-learned-about-alien-isolation-last-night/ · https://www.gosunoob.com/alien-isolation/encounters-third-mission-walkthrough/ | read (wf) | §2, §5 |
| T29 | Wikipedia, *Alien: Isolation* (save terminals, Nightmare map) | https://en.wikipedia.org/wiki/Alien:_Isolation | read (wf) | §5 |
| T30 | Thumb Culture, Amnesia: The Bunker review; Wikipedia, *Amnesia: The Bunker* | https://www.thumbculture.co.uk/amnesia-the-bunker-pc-review · https://en.wikipedia.org/wiki/Amnesia:_The_Bunker | read (wf) | §2 trench prologue, dying soldier |
| T31 | Gameranx, Bunker map parts; GamingBolt interview with Fredrik Olsson | https://gameranx.com/features/id/467195/article/amnesia-the-bunker-where-to-find-all-map-parts-cartographer-achievement-guide/ · https://gamingbolt.com/amnesia-the-bunker-interview-semi-open-world-structure-stalker-and-more | read (wf) | §5 |
| T32 | Game Developer, "True Detective meets Hearthstone: the inspirations behind Alan Wake II's Mind Place" | https://www.gamedeveloper.com/design/true-detective-meets-hearthstone-the-unlikely-inspirations-behind-alan-wake-ii-s-metaphyiscal-mind-place | read (wf) | §2, R7 |
| T33 | Christ and Pop Culture essay on Amnesia; Wikipedia, *Amnesia: The Dark Descent* | https://christandpopculture.com/when-games-matter-amnesia-bravery-and-the-time-i-wanted-to-quit/ · https://en.wikipedia.org/wiki/Amnesia:_The_Dark_Descent | read (wf) | §2 start card, note |
| T34 | DSOGaming / GamingBolt on Thomas Grip's critique of Alien: Isolation | https://www.dsogaming.com/news/frictional-games-dev-on-alien-isolation-horror-game-suffers-from-two-major-horror-design-issues · https://gamingbolt.com/frictional-games-dev-talks-about-alien-isolation-game-suffers-from-two-major-horror-design-issues | read (wf) | §6 no cutscene reveals |
| T35 | SOMA: RPGamer; GDC 2017 narrative review PDF; Wikipedia | https://rpgamer.com/2019/02/adventure-corner-soma/ · https://en.wikipedia.org/wiki/Soma_(video_game) | search | §2 safe apartment |
| T36 | PopMatters on P.T.; Wikipedia, *P.T.* | https://popmatters.com/pt-limits-secrets-community-review · https://en.wikipedia.org/wiki/P.T._(video_game) | read (wf) | §2, §3.4 |
| T37 | TheGamer on Village's opening; Press Start RE4 remake preview; NME Village review | https://www.thegamer.com/resident-evil-village-opening/ · https://press-start.com.au/features/2022/10/21/resident-evil-4-remake-hands-on-preview-a-much-spookier-trip-to-the-village/ · https://www.nme.com/reviews/game-reviews/resident-evil-village-review-open-world-theme-park-terror-2934027 | read (wf) | §2 readable end condition |
| T38 | CGMagazine, *The Evil Within* review | https://www.cgmagonline.com/reviews/evil-within-ps4-review | read (wf) | §2 |
| T39 | PlayStation Blog, Glen Schofield on The Callisto Protocol, 2022-08-23 | https://blog.playstation.com/2022/08/23/the-callisto-protocol-glen-schofield-interview/ | read (wf) | §2 |
| T40 | GamingBolt on Mr. X; PC Gamer interview with Kazunori Kadoi (footsteps are never faked) | https://gamingbolt.com/seven-years-later-mr-x-is-still-resident-evils-scariest-stalker · https://www.pcgamer.com/uk/resident-evil-2s-director-talks-mr-xs-ai-scary-footsteps-and-the-dmx-mod/ | read (wf) / search (PC Gamer 403) | §0.4 honest hunter sound |
| T41 | GosuGamers, "An ode to Silent Hill's unsung hero: creepy radio static" | https://www.gosugamers.net/entertainment/news/77521-halloween-gaming-an-ode-to-silent-hill-s-unsung-hero-creepy-radio-static | read (wf) | §3.4 proximity ≠ direction |
| T42 | Lethal Company Loud Horn: Steam community thread; Prima Games | https://steamcommunity.com/app/1966720/discussions/0/3958161899230735138 · https://primagames.com/tips/what-does-the-loud-horn-do-in-lethal-company-answered | search (Prima 403) | §3.4 a beacon with a cost |
| T43 | Yellow paint debate: GamesRadar (2023-10-06); Wccftech on the SH2 remake (2024-06-06, citing Famitsu); Dread Central | https://www.gamesradar.com/debate-over-resident-evil-4-remakes-yellow-paint-resurfaces-as-devs-explain-just-how-much-players-really-need-realism-is-less-immersive/ · https://wccftech.com/silent-hill-2-remake-faithful-thanks-bloober/amp/ · https://www.dreadcentral.com/editorials/492159/why-does-resident-evil-4-remake-have-yellow-paint/ | search (GamesRadar) / read (wf) | §3.5 |
| T44 | GosuNoob, RE7 Scorpion / Crow / Snake keys; Vandal, RE2 remake key locations | https://www.gosunoob.com/resident-evil-7/re7-scorpion-crow-snake-key-locations/ · https://vandal.elespanol.com/en/resident-evil-2-remake/resident-evil-2-remake-how-to-get-every-key-locations.html | read (wf) | §4 pairing on both halves; key pickup as event |
| T45 | GameRevolution, Village eye-socket door | https://www.gamerevolution.com/?p=672359 | read (wf) | §4 lock shows what is missing |
| T46 | Half-Life 2: Episode Two developer commentary (Combine OverWiki) | https://combineoverwiki.net/wiki/Developer_commentary/Half-Life_2:_Episode_Two | read (wf) | §3.3 glimpse the goal early |
| T47 | Robert Yang, "How to light a level", GDC 2018 slides and notes | https://www.blog.radiator.debacle.us/2018/03/gdc-2018-how-to-light-level-slides-and.html | read (wf) | §3.1 |
| T48 | Scott Rogers, "Everything I learned about level design I learned from Disneyland", GDC 2009 notes | https://craphound.com/disneylandleveldesign.txt | read (wf) | §3.1 squint test, exit dimming |
| T49 | 80.lv on Nicolas Oueijan, "Stop getting lost: make cognitive maps, not levels", GDC 2021 | https://80.lv/articles/thatgamecompany-s-designer-on-how-not-to-get-lost-in-game-using-cognitive-maps/ | read (wf) | §3.3 |
| T50 | Engadget on Elisabetta Silli (Mirror's Edge), 2010-08-17 | https://www.engadget.com/2010-08-17-overheard-gdc-mirrors-edges-odd-go-here-color.html | read (wf) | §3.5 |
| T51 | PlayStation, The Last of Us Part II accessibility | https://www.playstation.com/en-us/games/the-last-of-us-part-ii/accessibility/ | read (wf) | §3.6 optional aids |
| T52 | Marples, Gledhill and Carter, lighting, landmarks and auditory cues in virtual mazes (I3D 2020), Huddersfield repository | https://pure.hud.ac.uk/en/publications/the-effect-of-lighting-landmarks-and-auditory-cues-on-human-perfo/ | read (wf, abstract) | §3.7 cues compete |
| T53 | Pleszewska, coloured lighting and navigation choices, Aalborg MSc 2025 | https://projekter.aau.dk/the-influence-of-colored-lighting-on-navigational-decision-making-in-virtual-environments-24dded6f.html | read (wf, project page) | §3.7 |
| T54 | GameRevolution, Lethal Company terminal commands; Thunderstore mods LeadMeOut and ExitLineFromEnteredDoor | https://www.gamerevolution.com/guides/958910-lethal-company-computer-commands-list-terminal · https://thunderstore.io/c/lethal-company/p/Yakka_Productions/LeadMeOut/v/2.0.1/ | read (wf) | §4 counts, players mod in a way back |
| T55 | KeenGamer Returnal beginner's guide; GameRevolution (Atropian keys) | https://www.keengamer.com/articles/guides/beginners-guide-to-survive-the-brutality-of-returnal/ · https://www.gamerevolution.com/?p=680445 | read (wf) | §4 one key fits any lock |
| T56 | GamePretty, Escape the Backrooms walkthrough (Jan 2023) | https://gamepretty.com/escape-the-backrooms-new-update-part-2-walkthrough-jan-2023/ | read (wf) | §4 key slots; exits as lit anomalies |
| T57 | Phasmophobia objective board (wiki, via search); Destructoid map guide | https://phasmophobia.fandom.com/wiki/Objective_Board · https://www.destructoid.com/phasmophobia-6-tanglewood-drive-rework-map-guide/ | search / read (wf) | §4 |
| T58 | PowerPyx, SH2 remake Glimpses of the Past | https://www.powerpyx.com/silent-hill-2-remake-all-glimpses-of-the-past-locations/ | read (wf) | §4 pickup sting |
| T59 | Wikipedia, *SCP – Containment Breach* | https://en.wikipedia.org/wiki/SCP_%E2%80%93_Containment_Breach | read (wf) | §4 |
| T60 | Aftermath on The Exit 8's rules; 4Gamer IDC2024 report; Automaton interview with Kotake Create | https://aftermath.site/the-exit-8-japanese-subway-system-ocd-game-backrooms-liminal-space · https://www.4gamer.net/games/751/G075133/20241203035/ · https://automaton-media.com/en/interviews/interview-the-exit-8-developer-kotake-create-on-the-perks-of-being-a-solo-dev-and-how-platform-8-came-to-be/ | read (wf) | §5 |
| T61 | Automaton interview (Platform 8 signage added where testers struggled); Steam discussion | (as T60) · https://steamcommunity.com/app/2903560/discussions/0/6471190579020739863/ | read (wf) | §5 neutral display → command |
| T62 | Bullet Points Monthly on the SH2 remake (red pen) | https://bulletpointsmonthly.com/2024/10/28/but-you-never-did-silent-hill-2-2024 | read (wf) | §5 |
| T63 | Portal developer commentary (Combine OverWiki); Valve Developer Community, Test Sign | https://combineoverwiki.net/wiki/Developer_commentary/Portal · https://developer.valvesoftware.com/wiki/Test_Sign | read (wf) / search | §5 chamber sign |
| T64 | Combine OverWiki, Lambda logo | https://combineoverwiki.net/wiki/Lambda_logo | read (wf) | §5 one repeated mark |
| T65 | Steam discussion, Backrooms: Escape Together exit-sign level; GamerJournalist | https://steamcommunity.com/app/2141730/discussions/0/4845399912173028608 · https://gamerjournalist.com/how-to-escape-run-for-your-life-in-escape-the-backrooms/ | search | §5 |

### In the corpus, not cited in 01

| ID | Source | URL | Read |
|---|---|---|---|
| T66 | Game Developer on Limbo (Carlsen, GDC Europe 2010, 2010-08-16) | https://www.gamedeveloper.com/game-platforms/gdc-europe-i-limbo-i-s-carlsen-on-making-players-your-worst-enemy-and-your-best-friend | read (wf) |
| T67 | Wikipedia, *Firewatch*; DualShockers (map dot) | https://en.wikipedia.org/wiki/Firewatch | read (wf) |
| T68 | Gematsu, Far Cry 2 paper map (PAX 2008) | https://www.gematsu.com/2008/08/pax-08-far-cry-2-trailer | read (wf) |
| T69 | Game Informer on INSIDE (2016-07-01) | https://www.gameinformer.com/b/features/archive/2016/07/01/playdeads-inside-achieves-success-through-simplicity.aspx | read (wf) |
| T70 | Destructoid, R.E.P.O. extraction points | https://www.destructoid.com/repo-tips-to-make-money/ | read (wf) |
| T71 | Steam forum, Darkwood map | https://steamcommunity.com/app/274520/discussions/0/1693788384143538485 | read (wf) |

## 2. Project sources (in the repo or the work roots)

| ID | File | What it supplied |
|---|---|---|
| P1 | `Documentation/system_design/systems.json` (data 2026-10-22c) | C14, C15, C22 (and its "8 of 9" rationale), C23, C24 (EXIT 14–22 cells from the start door), C09, I1, I2, I6, N1–N4, D8, D9, L5 |
| P2 | `Documentation/MAP_GENERATION.md` | start flow, start door shut at 4 m, keys (look + E), Relay release 3 s after the shut, W1 lamps, zones and their size (one site per 8 × 8 chunk), revisit shifts |
| P3 | `Documentation/RELAY_PURSUIT_REDESIGN.md` §9 | stages, cue table, photosafety, "never a radar", §9.6 route |
| P4 | `Documentation/research/placard/10_spec.md` | placard size, mounts P1–P3, NYC LL5 1973 posting rule (S13), D17 kill switch |
| P5 | `Tools/print/ink/art_from_graphic/placard/README.md` + `placard_lit_preview.png` | the current art: start door labelled EXIT, YOU ARE HERE inside the start room, glow legend, footer |
| P6 | `Documentation/research/wallpaper_motion/30_narrative_phosphor.md` A.2, A.12; `research/relay_pursuit/30_narrative.md` §1–2 | EGRESS fiction; the Relay as the alarm's responder; placard strings v1 |
| P7 | `Documentation/research/screens/30_screen_strings.md` | WTCZ-TV 61, STAY TUNED., Front Rooms Furniture, the static office screens (§6), the emergency slate (§5) |
| P8 | `Tools/screens/promo_audio.py` | an original, synthesised FRONT ROOMS jingle (120 BPM), WTCZ ident, TV power-on/off and flyback whine; nothing downloaded |
| P9 | `Assets/StreamingAssets/FrontRooms_Ad_01.mp4`, `Tools/screens/out/FrontRooms_Ad_02/FrontRooms_Ad_02.mp4` (ffprobe 2026-10-22) | both ads are 16 s, video only, **no audio track** |
| P10 | `Assets/Scripts/Media/FrontRoomsScreenVideo.cs` | every Office CRT plays the ad, `audioOutputMode None`, power toggle |
| P11 | `Assets/Scripts/Audio/FrontRoomsRelaySound.cs` `UpdateOcclusion`; `FrontRoomsMapPathField` `DistanceTo` / `OpeningCostTo` | existing wall-count occlusion and the player-rooted 45 m path field a beacon can reuse |
| P12 | `Assets/Scripts/FrontRooms3DGame.cs` `CalmHint` | the only tutorial text today (9 s) |
| P13 | `Documentation/LEVEL_DESIGNER.md` (Markers) | KeySpot markers; module doors are map-made only |
| P14 | `Documentation/research/inspect/10_design.md` §2.5 row K | first-key close-up |
| P15 | `Documentation/research/wallpaper_motion/20_level_design_phosphor.md` §5 | the earlier "no HUD tutorial, staged beats" curve (first dark, fallback Flash line) |
| P16 | `Documentation/research/stairwell/01_research.md` §0 | UBC 1991 / NYC LL5: stair doors locked on the stair side; re-entry signs |
| P17 | `Assets/Resources/Props/Models/Kit_CRTTV.json` | a CRT TV kit with a screen anchor already exists |
| P18 | `Documentation/GRAPHIC_VISUAL_TASKS.md`; `Documentation/Takeover_2026-10-18/briefs/course.md` | round-two playtest numbers; testers unsure of sight vs hearing (testers not named here) |
| P19 | `Documentation/research/placard/50_tutorial_placard.md` (平面视觉, 10-22) and Figma `3459:7280` | the placard's three options, T1–T10, rules 1–6, guarantees G1–G7 / A1–A2 / B1 / C1–C3, the four calls (TP04) that §5 of the proposal defers to |
