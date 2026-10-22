# Computer screens: research (2026-10-08)

*系统设计. Workflow `computer-ui-research` (wf_aa6d8749-f97): four independent sweeps, then a critic that checked sources, era claims and project-file claims. The critic's corrections are in the last section and override the sweeps where they disagree. Links only; nothing was downloaded. Footage links were confirmed by title, not watched, unless the text says so.*

## Reference games: walking up to a computer and reading it

### Alien: Isolation (2014): terminals are menu-driven CRT screens used in real time. The alien keeps hunting while you read, and exiting has failed for some players.

TRIGGER: walk up and press interact. Terminals and wall-mounted rewire stations have to boot before you can use them, which reviewers call a source of tension. CAMERA: rewire panels and lockers use a scripted 'cinematic camera' with limited look-around; the source is a mouse-fix mod's description of the camera's smoothing, sensitivity and look limits, and that page was search excerpt only. The exact camera move at an Access Terminal is UNVERIFIED in text sources; check the footage. INPUT: Shacknews says there is no touch or point-and-click interface for any computer, so navigation is by menu and keys. The default keyboard exit is Q. WORLD AND THREAT: Gamereactor says that, as with terminals and tuners, using rewire stations does not pause the game. Sketches of Time says you are locked into the action for at least a handful of seconds and can be killed while going through a computer. The AI behind this has two layers: a director that always knows where the player is, and an alien that has to find the player by its senses (footsteps, gunshots, the motion tracker at about 1.5 m). Noise triggers its searches, and a 'menace' gauge makes the director back off once it peaks. LEAVING: two Steam threads (2014, 2017) report players stuck inside a terminal. In one, binding Close to Space broke the exit; in the other, Q 'sometimes just makes a noise' and the player had to reload. READABILITY: the screens are green, flickering monochrome CRTs. Terminals mainly use a custom pixel font, 'Jixellation', modelled on low-resolution MS Sans Serif. The UI was recorded to VHS and filmed on old CRTs with magnets to make it glitch. MOBILE: Feral's iOS/Android port (Dec 2021) ships two default touch schemes and lets players resize and move every on-screen input, tuned for fast actions such as hacking a door. How terminals work on touch is UNVERIFIED. FOOTAGE: the YouTube links below; titles and channels were confirmed via oEmbed, but I did not watch them.

- [Gamereactor: Alien: Isolation review](https://www.gamereactor.eu/alien-isolation-review/) (review): 'As with terminals and tuners, using these doesn't pause the game'
- [Sketches of Time: Alien: Isolation](https://sketchesoftime.com/video-game-reviews/alien-isolation/) (review): Locked into the action for a handful of seconds; can be gutted while ripping through a computer
- [Shacknews: Alien Isolation review: Live, die, repeat](https://www.shacknews.com/article/86549/alien-isolation-review-live-die-repeat) (review): Green flickering monochrome CRTs; no touch or point-and-click interface for any computer; three terminals for one task
- [PlayStation LifeStyle: Alien: Isolation review](https://www.playstationlifestyle.net/2014/10/11/alien-isolation-review-just-give-up-ps4/) (review (search excerpt)): Terminals and rewire stations have to boot up
- [Steam: Used a terminal - how do I exit from it?!](https://steamcommunity.com/app/214490/discussions/0/619569608634557717/) (forum): Q exits by default; binding Close to Space broke the exit (Oct 2014)
- [Steam: How to exit terminals?](https://steamcommunity.com/app/214490/discussions/0/1291816880496062953/) (forum): Q unreliable, forced reload (May 2017)
- [Nexus Mods: Alien Isolation Mouse Fix](https://www.nexusmods.com/alienisolation/mods/173) (mod page (search excerpt, 403 on fetch)): A 'cinematic camera' with look limits at rewire panels and lockers
- [Fonts In Use: Alien: Isolation](https://fontsinuse.com/uses/77513/alien-isolation-video-game) (typography reference): Jixellation pixel font used mainly on terminals; custom Helvetica Bold for body text
- [Engadget: Tube TVs, VCRs and magnets give Alien: Isolation its signature look](https://www.engadget.com/2014-10-07-alien-isolation-launch-interview.html) (developer interview): UI recorded to VHS and filmed on CRTs with magnets (Alistair Hope)
- [Game Developer: The Perfect Organism: The AI of Alien: Isolation (Tommy Thompson, 2017)](https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation) (AI analysis): Two layers (director and alien), menace gauge, senses, noise-triggered searches
- [Gfinity: Alien: Isolation Mobile interview with Feral (2021-12-15)](https://www.gfinityesports.com/alien-isolation-mobile/interview/) (developer interview): Touch schemes, customizable input size and position, fast-action priority
- [YouTube: Alien: Isolation (PC) Part 6, Alternate Comm Terminal (Jaded Jake)](https://www.youtube.com/watch?v=5zyxdk_v5Q0) (gameplay footage): Terminal use under threat
- [YouTube: Alien: Isolation (PC) Part 26 'Spaceflight Terminal' no commentary (Daelyria)](https://www.youtube.com/watch?v=A1QMFT3B-5w) (gameplay footage): Terminal camera and UI
- [YouTube: Navigating Sevastopol Terminal in #AlienIsolation (HorrorCultFilms)](https://www.youtube.com/watch?v=FfTVsayYcuw) (gameplay footage): Terminal menu navigation

### Doom 3 (2004): you use a screen just by looking at it, no button. Your view ray becomes the mouse cursor within 80 units, and the game stays fully first person and real time.

Confirmed from id's GPL source code (Player.cpp). TRIGGER: idPlayer::UpdateFocus casts a ray 80 units from the eye ('end = start + viewAngles.ToForward() * 80.0f') and runs GuiTrace against GUI entities. The hit point is sent to the GUI as a mouse-move event ('GenerateMouseMoveEvent( pt.x * SCREEN_WIDTH, pt.y * SCREEN_HEIGHT )'), so where you look is the cursor. In the GUI script, onActivate fires 'when the user first puts his cursor over the gui'. CAMERA: no camera move, and the crosshair is not drawn while a GUI is active (DrawHUD only redraws it 'if ( !GuiActive() )'). INPUT: Weapon_GUI lowers the weapon and stops firing, and the attack button sends mouse-button-1 click events to the GUI. WORLD AND THREAT: focus is handled inside the normal player update, so the world runs on. That is inferred from the code; no pause exists in this path. LEAVING: look away or step back past 80 units, and focus times out after FOCUS_GUI_TIME. READABILITY: GUIs are authored on a 640x480 virtual desktop. Readability comes from the short range, which forces you to stand close; converting 80 units to about 2 m (1 unit = 1 inch) is UNVERIFIED. GUIs can open doors and run map scripts, and the engine can render camera views onto GUI surfaces (useful for a CCTV idea). PLATFORMS: D3wasm runs Doom 3 in the browser via WebAssembly, so render-to-surface world GUIs work in a browser; on its HN thread, players praise the crosshair-to-cursor feel. No mobile version.

- [id Software DOOM-3 GPL source: neo/game/Player.cpp](https://raw.githubusercontent.com/id-Software/DOOM-3/master/neo/game/Player.cpp) (source code): 80-unit focus trace, GuiTrace, cursor from hit point, weapon lowered, attack button sent as click, crosshair hidden while a GUI is active
- [iddevnet (dhewm3 mirror): Doom 3 GUIs](https://iddevnet.dhewm3.org/doom3/guis) (developer documentation): 640x480 desktop rect, onActivate on cursor-over, nocursor, onAction on left click
- [Battleteam: Fully Interactive Surfaces](https://battleteam.net/tech/fis) (modding write-up): Doom 3 can render camera views onto GUI surfaces
- [Hacker News: D3wasm 0.4 - Doom 3 in WASM (2022-09-28)](https://news.ycombinator.com/item?id=33006523) (forum): 'As your crosshair approaches the screen, it turns into a mouse cursor'; browser port; Prey 2017 'still sometimes locks on'
- [YouTube: Doom 3: GUI usage examples (CruiseLinerMod)](https://www.youtube.com/watch?v=fJsn1DOz4Ho) (gameplay footage): In-world GUI interaction

### SOMA (2015): Doom 3-style screens, except you first click the terminal to engage it. The GUI is an immediate-mode layout drawn onto a flat screen mesh, with a cursor on the screen.

TRIGGER: Thomas Grip says SOMA's terminals 'work pretty much the same way as in Doom 3 expect that you need to click one before you can interact'. TECH (Frictional wiki): only the Prop_Terminal entity supports in-scene GUI. The GUI is drawn onto a 'ConnectedSubmesh': a flat quad with 4 vertices and 0-1 UVs, with ScreenSize set in pixels at the same aspect ratio. A per-frame OnGui script plus Enter and Leave callbacks drive it. Layout uses normalized 0-1 coordinates, and the sample sets font size to 4% of screen height. If 'allowInteraction' is off, players 'can only look at it': a built-in split between display-only and interactive screens. INPUT: on PC a cursor moves on the screen (a walkthrough step says to 'move the cursor around the screen to tune into the channel'). The Sixth Axis says the PS4 version uses one gesture for every interactive object: hold the right trigger and move the right stick. CAMERA: an Unreal forum poster describes SOMA as 'get close hit E and camera focus on widget and spawn mouse cursor'. That is a user's description; the camera focus is UNVERIFIED by any Frictional source. WORLD AND THREAT while at a terminal: UNVERIFIED. LEAVING: the GuiLeave callback runs 'whenever the player leaves the terminal'; which input does this is UNVERIFIED. MOBILE: none. A community VR mod lists beam-to-cursor alignment on terminals as a problem area.

- [DSOGaming: SOMA has been inspired by Doom 3... (2015-07-27)](https://www.dsogaming.com/news/soma-has-been-inspired-by-doom-3-bioshock-spec-ops-the-line-silent-hill/) (developer interview): Grip: terminals inspired by Doom 3; must click one before interacting
- [Frictional Wiki: HPL3/Gui](https://wiki.frictionalgames.com/page/HPL3/Gui) (developer documentation): Prop_Terminal, ConnectedSubmesh flat quad, ScreenSize in pixels at the same ratio
- [Frictional Wiki: HPL3/SOMA/Scripting/Terminals Overview](https://wiki.frictionalgames.com/page/HPL3/SOMA/Scripting/Terminals_Overview) (developer documentation): OnGui, Enter and Leave callbacks; normalized layout; allowInteraction off means look only
- [GameFAQs: SOMA Guide and Walkthrough (nyiaor)](https://gamefaqs.gamespot.com/pc/735002-soma/faqs/74225?page=2) (walkthrough): Moving the cursor around a terminal screen
- [The Sixth Axis: SOMA review](https://www.thesixthaxis.com/2015/09/21/soma-review/) (review): Right trigger plus right stick for terminals and every interactive object
- [Unreal Engine forums: SOMA like UMG](https://forums.unrealengine.com/t/soma-like-umg/45004) (forum): User's description of the SOMA camera focus and cursor (unverified)
- [GitHub: phunkaeg/SOMAVR](https://github.com/phunkaeg/SOMAVR) (mod repository (search excerpt)): Terminal beam and cursor alignment issues in VR
- [YouTube: SOMA - Gameplay Trailer (frictionalgames)](https://www.youtube.com/watch?v=syhcF0Mx0j0) (official trailer): Terminal interaction footage
- [YouTube: SOMA Gameplay (PC HD) (Throneful)](https://www.youtube.com/watch?v=WLAcM1lFKKI) (gameplay footage): Terminal interaction footage

### Prey (2017): computers are in-world touchscreens you use without leaving 'game space'. The use key presses what the crosshair points at, an optional key locks the view to the screen, and a thrown foam dart can press a screen from a distance.

TRIGGER AND INPUT: Kotaku says you interact with computers 'without leaving the “game space” and entering a “computer space”', and 'you can hit the Z button to lock your view to the computer screen'. An early forum report says the left mouse button does not press screen buttons; you use the use key. A community mod (PRIC) adds proper mouse-cursor support, Doom 3-style attack-key control, and a camera lock with a free cursor, which implies the stock game has none of these. Arkane intended screens to be pressed from range by shooting a Huntress Boltcaster foam dart at them: Seth Shain, quoted via a Red Bull interview (search excerpt; the page did not load), and walkthroughs that shoot an 'Unlock Door' button through a window. CAMERA: stays first person unless you lock with Z. WORLD AND THREAT: implied by 'without leaving the game space'; an explicit no-pause statement is UNVERIFIED. CONTENT: email threads carry the story and puzzle codes, and most passwords are randomized per playthrough and placed on sticky notes near desks. READABILITY: the optional view lock is the readability aid. MOBILE: none.

- [Kotaku: Prey's Mouse And Keyboard Controls Are Really Good (Kirk Hamilton, 2017-05-05)](https://kotaku.com/prey-s-mouse-and-keyboard-controls-are-really-good-1794963218) (review/feature): No separate computer space; Z locks view to the screen
- [Giant Bomb forums: Prey General Impressions](https://www.giantbomb.com/prey/3030-54211/forums/prey-general-impressions-1809854/) (forum (search excerpt)): Left mouse does not press screens; use key does
- [Steam: [PRIC] Interface, Controls and QoL mod](https://steamcommunity.com/app/480490/discussions/0/2579854400739359065/) (mod page/forum): Mod adds mouse cursor, attack-key control, camera lock with free cursor
- [Red Bull: Prey tips guide (Seth Shain)](https://www.redbull.com/gb-en/prey-tips-guide) (developer interview (search excerpt; fetch failed)): Touch screens can be operated; Boltcaster darts work on screens out of reach
- [Samurai Gamers: Through a Glass Darkly walkthrough](https://samurai-gamers.com/prey/main-walkthrough-glass-darkly) (walkthrough): Shooting a screen through a window to unlock a door
- [Shacknews: Where to Find All of Prey's Workstation Passwords](https://shacknews.com/article/99861/where-to-find-all-of-preys-workstation-passwords) (guide): Randomized passwords; sticky notes near workstations
- [YouTube: Prey - 8 Minutes of Gameplay (Bethesda Softworks)](https://www.youtube.com/watch?v=81Ofcp8Pp_U) (official gameplay footage): Workstation and screen use

### Observation (2019) and Stories Untold (2017), both by No Code (led by Alien: Isolation's former UI lead): the screen and the camera are the whole game. Free text input was cut for consoles.

OBSERVATION: you play SAM, the station AI. You act through the station's camera feeds, panning and switching cameras to highlight objects. You 'pair' with devices by entering a code, and each device has its own UI to work out. Space opens SAMOS for modules, files and crew. PCWorld says it simulates 'the rudimentary software of the '80s and '90s', and a new camera feed 'pops and distorts like a worn VHS tape before slowly clearing'. In Edge (2019, PressReader excerpt only), Jon McKellan says they planned for console from the start and that text input from Stories Untold 'just doesn't translate to console'. STORIES UNTOLD: you sit at a desk in front of a CRT and play a text adventure, and the real room changes around the desk (the lamp sparks and dies; the photos change). The Switch version (2020) swaps typing for a list of collected words and phrases shown in context; that comes from GamesRadar via search excerpt, as the page body did not load. HG101 reports that the knob and dial controls in later episodes were awkward because raising the cursor pushes the camera too high. THREAT: neither game has a hunter in the Relay sense, so 'world continues' only means scripted events. MOBILE: none found.

- [bit-tech: Observation review](https://bit-tech.net/reviews/observation-review/1/) (review): Camera feeds, pairing by code, device-specific UIs, SAMOS on spacebar
- [PCWorld: Observation review](https://www.pcworld.com/article/3396640/observation-review-science-fiction.html) (review): '80s/'90s software simulation; VHS-like camera switch
- [Edge 'Post Script' (2019-06-20) via PressReader](https://www.pressreader.com/australia/edge/20190620/283970358958585) (developer interview (search excerpt; 403 on fetch)): McKellan: console from the start; text input doesn't translate to console
- [Zinio: PC PowerPlay interview (McKellan, former UI designer on Alien: Isolation)](https://www.zinio.com/publications/pc-powerplay/5641/issues/461539/articles) (developer interview (excerpt)): Lineage from Alien: Isolation UI to No Code
- [GamesRadar: Stories Untold gets an unlikely Switch port](https://gamesradar.com/stories-untold-switch-version) (news (search excerpt)): Switch replaces typing with a contextual phrase list
- [Wikipedia: Stories Untold](https://en.wikipedia.org/wiki/Stories_Untold_(video_game)) (encyclopedia): Platforms and dates; room changes after power cut
- [Hardcore Gaming 101: Stories Untold](https://www.hardcoregaming101.net/stories-untold/) (critique): Episode one played on a screen; awkward knob and cursor camera
- [YouTube: Observation - Behind the Schemes with No Code (GameTrailers)](https://www.youtube.com/watch?v=6RqmAkPkHVY) (developer video): Interface footage and developer commentary
- [YouTube: Stories Untold - The House Abandon](https://www.youtube.com/watch?v=30IwOqlbj2Y) (gameplay footage): Seated desk and CRT presentation

### Signalis (2022): the camera cuts from top-down to first-person close-ups of consoles that you point and click through. A major review criticized its fuzzy, low-resolution text.

TRIGGER AND CAMERA: Thumbculture describes the switch to 'first-person close ups of computer consoles, cockpits, and exteriors', and calls the slow zoom into a console 'often mesmerising'. A Destructoid preview (search excerpt) says looking into a room can switch to first person, where you are 'point-and-clicking through interfaces'. READABILITY: PC Gamer's review (search excerpt; the page body did not load) praises the vibe but warns of 'getting a headache reading fuzzy low-res text'. That is the cautionary case for running a CRT look over body copy. Whether the threat keeps acting during close-ups is UNVERIFIED. PLATFORMS: PC, PS4, Xbox One, Switch; no mobile.

- [Thumbculture: Signalis Switch review](https://www.thumbculture.co.uk/?p=58228) (review): First-person close-ups of consoles; slow zoom
- [Destructoid: Signalis review/preview](https://destructoid.com/reviews/review-signalis) (review (search excerpt; 403 on fetch)): Transition to first person, then point-and-click through interfaces
- [PC Gamer: Signalis is a brutally stunning slice of sci-fi survival horror](https://www.pcgamer.com/signalis-survival-horror) (review (search excerpt)): UI 'too aesthetic for its own good'; fuzzy low-res text causes headaches
- [YouTube: SIGNALIS - Official Launch Trailer (rose-engine)](https://www.youtube.com/watch?v=4KFiOp2o4L8) (official trailer): Console close-ups

### Duskers (2016): the terminal is the whole game. You type commands in real time while enemies attack your drones, and the video feed is deliberately degraded.

INPUT: bit-tech says the game is 'almost entirely controlled through the command line interface'. Arrow keys drive drones by hand, and every other action is typed ('gather', 'gather all', 'Navigate 1 r3'). Aliases work like macros. WORLD AND THREAT: enemies 'destroy your poor little drones faster than you can type out an “Abort” command', so time runs while you type. That is inferred from the review; no source states 'no pause' outright. READABILITY AND DREAD: the drone feed 'is full of glitches and static', and it cuts out when the drone you are watching is attacked. Degraded information is itself the warning. PLATFORM: PC only.

- [bit-tech: Duskers review](https://bit-tech.net/reviews/gaming/duskers-review/2/) (review): Command-line control, arrow keys, example commands
- [Indie Game Reviewer: Duskers](https://indiegamereviewer.com/review-duskers/) (review): Aliases; drones die faster than you can type Abort; glitchy feeds
- [YouTube: Fantastic Arcade 2016: Misfits Attic's Sci-Fi Drone Sim DUSKERS](https://www.youtube.com/watch?v=xoUOmw6IOas) (developer talk/demo): Developer walkthrough of the CLI
- [YouTube: Duskers PC Gameplay 60fps 1080p](https://www.youtube.com/watch?v=nWZL0neI9O4) (gameplay footage): CLI in play

### Iron Lung (2022): in-world panels are clicked with a cursor, and the only view outside is low-quality photos on a screen. The developer deliberately avoided direct control.

The sub has no viewing ports, and the low-quality camera is 'the player's only way of seeing outside'. You steer with navigation equipment at the front, shown through a minimalist interface. You click panel buttons with the mouse. When a player asked for key bindings, the developer replied: 'I wanted to avoid having you controlling the sub directly'. A review (search excerpt) says the only two points of interaction are the front control panel and a big photo button at the back, and you walk the cabin between them. THREAT: diegetic, everything shown is ambiguous. MOBILE: Android port on 12 Dec 2025; Switch 2022; PS5 Oct 2025.

- [Wikipedia: Iron Lung (video game)](https://en.wikipedia.org/wiki/Iron_Lung_(video_game)) (encyclopedia): No viewports; camera is the only outside view; platforms and dates including Android
- [Steam: Iron Lung controls discussion (developer reply 2022-03-10)](https://steamcommunity.com/app/1846170/discussions/0/5757294878574699289) (developer forum reply): Mouse-click control; dev wanted to avoid direct control
- [Bloody Disgusting: Iron Lung review](https://bloody-disgusting.com/video-games/3707036/iron-lung-review/) (review (search excerpt; 403 on fetch)): Two points of interaction; walking the cabin between them
- [YouTube: Iron Lung - Full Game Playthrough (No Commentary)](https://www.youtube.com/watch?v=t7BW5zqaq8Y) (gameplay footage): Panel clicking and photo screen

### Hypnospace Outlaw (2019) and Her Story (2015): the computer is the entire screen. Hypnospace maps a cursor to gamepad sticks and Switch touch; Her Story shipped on iOS with an option to turn off the CRT glare.

HYPNOSPACE OUTLAW: the game is a late-'90s OS. On Switch, the left stick moves the cursor, L and R switch tabs, and the d-pad scrolls. The Xbox/PC controller map is right stick for quick mouse, A or RT to click, and B to exit. Text goes in through an on-screen keyboard. The developer says the Switch control scheme applies to all consoles and PC 'other than touch and motion support', so the Switch version adds touch. HER STORY: an old desktop with files and programs, where you type words from transcripts to search a police video database. It released 24 June 2015 on iOS, OS X and Windows, and on Android in 2016. On iOS the monitor glare is on by default, and an 'Anti-Glare Filter' setting removes it. Indie Fund's Ron Carmel recalls the CRT glare and Windows 3.0-style text interface as what caught his eye. THREAT: none in either game.

- [Nintendo World Report: Hypnospace Outlaw (Switch) review](https://www.nintendoworldreport.com/review/54664/hypnospace-outlaw-switch-review) (review): Left-stick cursor, L/R tabs, d-pad scroll
- [TrueAchievements: Hypnospace Outlaw hints and tips](https://www.trueachievements.com/game/Hypnospace-Outlaw/walkthrough/2) (guide (search excerpt)): Xbox mapping: right stick quick mouse, A/RT click, B exit
- [Steam: Hypnospace Outlaw controls thread (developer reply)](https://steamcommunity.com/app/844590/discussions/0/2916598577633088999) (developer forum reply): On-screen keyboard; Switch adds touch and motion
- [Kotaku: Hypnospace Outlaw's Switch port supports keyboard and mouse](https://kotaku.com/hypnospace-outlaws-switch-port-supports-keyboard-and-mo-1844781183) (news): Keyboard and mouse on Switch
- [Wikipedia: Her Story (video game)](https://en.wikipedia.org/wiki/Her_Story_(video_game)) (encyclopedia): Old desktop; database search; iOS/OS X/Windows 2015, Android 2016
- [TidBITS: FunBITS: Her Story for iOS and Mac](https://tidbits.com/2015/07/13/funbits-her-story-for-ios-and-mac/) (review): Glare on by default; Anti-Glare Filter toggle
- [Indie Fund: Now Backing Her Story](https://indie-fund.com/articles/Indie-Fund-Now-Backing-Her-Story) (funder statement (search excerpt)): CRT glare and Windows 3.0-style interface
- [YouTube: Hypnospace Outlaw - Announcement Trailer - Nintendo Switch (Nintendo of America)](https://www.youtube.com/watch?v=tmutIXGhgB8) (official trailer): Desktop UI on console
- [YouTube: Her Story iPhone/iPad HD Gameplay Trailer (TouchGameplay)](https://www.youtube.com/watch?v=DVhUvp_h-7k) (gameplay footage): Touch desktop UI on iOS

### Five Nights at Freddy's (2014): the monitor is the core loop. You raise a full-screen camera overlay that costs power while the animatronics keep moving, and the game shipped on mobile within weeks.

You are confined to an office. Cameras, doors and lights all drain a limited power supply, and when it runs out the cameras fail and the doors open. Most animatronic movement happens off-screen. TRIGGER: hover the arrow strip at the bottom of the screen to raise the monitor. The fan wiki's monitor page could not be fetched (402); these rules come from its search excerpt. RULES WHILE ON THE MONITOR: watching some animatronics freezes them. A move while you watch can cut the feed. While the monitor is up, the main three cannot kill you until you lower it, but Foxy can. LEAVING: lower the monitor the same way. MOBILE: Android 26 Aug 2014, iOS 11 Sep 2014. The fan wiki notes FNAF3's mobile buttons still say 'click', a porting oversight to avoid.

- [Wikipedia: Five Nights at Freddy's (video game)](https://en.wikipedia.org/wiki/Five_Nights_at_Freddy%27s_(video_game)) (encyclopedia): Confined to office; cameras/doors/lights drain power; off-screen movement; mobile port dates
- [FNaFapedia: Monitor](https://fnafapedia.fandom.com/wiki/Monitor?oldid=2664) (fan wiki (search excerpt; 402 on fetch)): Hover-strip trigger; freeze while watched; feed cut; Foxy exception
- [FNAF Wiki: Five Nights at Freddy's 3 (Mobile)](https://freddy-fazbears-pizza.fandom.com/wiki/Five_Nights_at_Freddy%27s_3_(Mobile)) (fan wiki (search excerpt)): 'Click' wording left on touch buttons
- [App Store: Five Nights at Freddy's](https://apps.apple.com/app/id912536422) (store page): iOS version exists
- [YouTube: Five Nights at Freddy's - Night 1 Walkthrough (No Commentary)](https://www.youtube.com/watch?v=w6_TjqA6IA8) (gameplay footage): Monitor loop

### Amnesia: The Bunker (2023): not a computer, but the information station is fixed in place. You cannot carry the map, and the ever-present stalker answers noise and darkness.

The map cannot be carried; sub-maps sit in a Clerk's Office at the entrance of each area, and those offices are often barricaded, locked or booby-trapped. Creative lead Fredrik Olsson calls the 'ever-present stalking monster' a first for Frictional, and has compared it to Alien: Isolation's xenomorph. With the lights on, the beast 'generally only emerges from boltholes in the wall when you make too much noise', and it prefers to hunt in darkness. This maps directly onto FrontRooms' lamp-based warnings. Whether reading a note or the map pauses the game is UNVERIFIED.

- [PCGamesN: Amnesia: The Bunker map](https://www.pcgamesn.com/amnesia-the-bunker/map) (guide): Map can't be carried; sub-maps in Clerk's Offices
- [Epic Games Store: Amnesia: The Bunker is a chilling underground obstacle course of horror](https://store.epicgames.com/news/amnesia-the-bunker-is-a-chilling-underground-obstacle-course-of-horror) (developer feature (search excerpt)): Olsson: ever-present stalker is new for Frictional
- [GamesRadar: Amnesia: The Bunker's generator...](https://gamesradar.com/amnesia-the-bunker-generator) (feature (search excerpt)): With lights on, emerges only on noise
- [GamingBolt: Amnesia: The Bunker interview (Olsson)](https://gamingbolt.com/amnesia-the-bunker-interview-semi-open-world-structure-stalker-and-more) (developer interview (search excerpt)): Stalker roams and reacts; prefers darkness
- [YouTube: Amnesia: The Bunker - Official Developer Gameplay Overview (GameTrailers)](https://www.youtube.com/watch?v=Y_fxzeCAfq8) (developer video): Developer walkthrough
- [YouTube: The AI of Amnesia: The Bunker | AI and Games #82](https://www.youtube.com/watch?v=SqVKuTy6eYc) (AI analysis): Stalker AI behaviour

### Lethal Company (2023) and Welcome to the Game II (2018): a typed terminal or desktop inside a shared, dangerous space. The threat reaches you while you are focused on the screen.

LETHAL COMPANY: approach the ship terminal and press E, then type commands; HELP lists them. 'view monitor' toggles the ship's map camera on the terminal. Leaving with TAB is UNVERIFIED (search-summary claim only). That the terminal operator is safe inside the ship is a player's opinion on Steam; not designed rule. WELCOME TO THE GAME II: a horror game played at a computer while threats come into the house. Fan-wiki sources (weak) say power cuts stop you using the computer, and a panel on the PC screen warns when more dangerous threats are active. PARATOPIC and MOUTHWASHING: no in-world computer interface was found in the sources checked. Paratopic is exploration, object interaction and dialogue. Mouthwashing's one documented screen is a cockpit reader that shows a code. Treat both as weak references for this feature; that neither has a computer interface is UNVERIFIED, not confirmed.

- [GameWatcher: Lethal Company terminal commands (2023-11-23)](https://www.gamewatcher.com/lethal-company-terminal-commands) (guide): Approach and press E; View Monitor command
- [GameRevolution: Lethal Company computer commands list (2023-12-05)](https://www.gamerevolution.com/guides/958910-lethal-company-computer-commands-list-terminal) (guide): HELP; view monitor toggles the map camera
- [Steam: Lethal Company terminal discussion](https://steamcommunity.com/app/1966720/discussions/0/4032473196482519925) (forum (search excerpt)): Player opinion on terminal-operator safety
- [YouTube: Lethal Company Advanced Tips: Terminal Dots Explained (Eurogamer)](https://www.youtube.com/watch?v=hLXlv01lhDk) (gameplay footage): Terminal use footage
- [Welcome to the Game Wiki: Welcome to the Game II](https://welcometothegame.fandom.com/wiki/Welcome_to_the_Game_II?oldid=4234) (fan wiki (search excerpt)): Power cut blocks computer use; threat panel
- [Wikipedia: Paratopic](https://en.wikipedia.org/wiki/Paratopic) (encyclopedia): Gameplay is exploration, object interaction, conversation
- [Wikipedia: Mouthwashing (video game)](https://en.wikipedia.org/wiki/Mouthwashing_(video_game)) (encyclopedia): Exploration, dialogue, item puzzles
- [Echo Boomer: Mouthwashing review](https://echoboomer.pt/?p=217634) (review (search excerpt)): Cargo code visible only on a cockpit reader

### Readability check for FrontRooms' own CRT: at normal walk-up distances, 1990 80x25 text on the 0.28 x 0.19 m monitor quad is unreadable. Body text needs a focus framing, and phones need a larger character cell.

This is a DERIVED CALCULATION, not a game source. Inputs: the existing screen quad is 0.28 x 0.19 m (SCREENS_VISUAL_SYSTEM.md section 1); the map walker camera's vertical FOV is 72 degrees (FrontRoomsMapWalker.cs line 72); VGA 80x25 text mode is 720x400 with 9x16 cells. Projected screen height on 1080p and text height per row (of 25): 1.0 m gives 141 px and 5.6 px/row; 0.6 m gives 235 px and 9.4; 0.4 m gives 353 px and 14.1; about 0.35 m gives 403 px and 16.1, which is VGA's 16-line cell at 1:1. On a phone with 402 pt of landscape height: 0.6 m gives 3.5 pt/row; a focus framing where the CRT fills 80% of the height gives about 12.9 pt/row (1080p: about 34.6 px/row). Reading the screen at a distance works only for very large type (Doom 3-style glance). Reading 25-row body copy needs the screen to fill most of the view, and phones need fewer rows or columns.

- [Wikipedia: VGA text mode](https://en.wikipedia.org/wiki/VGA_text_mode) (reference): 80x25 text mode at 720x400, 9x16 cells
- [Project: Documentation/SCREENS_VISUAL_SYSTEM.md](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/SCREENS_VISUAL_SYSTEM.md) (project file): 0.28x0.19 m screen quad, 640x434 RT video, E toggles power within 2.4 m
- [Project: Assets/Scripts/FrontRoomsMap/FrontRoomsMapWalker.cs](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/FrontRoomsMap/FrontRoomsMapWalker.cs) (project file): view.fieldOfView = 72
- [Project: Documentation/TOUCH_CONTROLS.md](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/TOUCH_CONTROLS.md) (project file): 402 pt phone height used for touch type sizing

**Recommendations from this sweep:**

- Use two layers. (1) GLANCE, Doom 3 style: no button. Within about 0.9 m, the view ray hits the CRT, the crosshair becomes a cursor on the screen, and the screen shows only headline-size content (about 8-10 rows) that reads at walk-up distance. (2) FOCUS, Alien/SOMA style: E or a tap starts a short dolly (about 0.3 s) to a framing where the CRT fills about 75-80% of view height. 25-row body copy needs this, per the readability calculation.
- Never pause the game for a computer, as in Alien: Isolation, Doom 3, Duskers and FNAF. Keep the Relay's staged warnings perceivable in FOCUS. Leave a visible margin of the player's room so lamp flicker can be seen at the edges. Keep 3D audio unmuffled so muffled steps stay readable. Treat the lock-on chime as an automatic exit from FOCUS, or at minimum make sure the one-press exit always works. Avoid FNAF's 'monitor up means safe' rule unless it is designed in on purpose.
- Make exiting unbreakable: E again, Esc/Back/B, any movement input, a large touch EXIT button, and an automatic exit on the chime or on contact. Keep the exit binding independent of remapping conflicts; Alien: Isolation players got stuck when Close shared Space or Q failed.
- Input per platform: desktop uses a mouse cursor clamped to the screen rectangle plus era keyboard navigation (arrows, Enter, F-keys, number menus). Gamepad uses d-pad focus navigation with an optional stick cursor (Hypnospace's console mapping). Touch taps the CRT image directly in FOCUS, plus a soft-key strip that mirrors F1-F10. Do not ask for free text: enter codes and passwords from a collected-word list or a numeric keypad, as Stories Untold did on Switch.
- The computer's own UI should be a 1990 DOS/text-mode office system (80x25 grid with 9x16 cells, or a 40-column mode for phones). Render it into a RenderTexture at the native grid with point filtering. Keep the CRT effect (curvature, glare, scanlines) as a separate thin layer with an accessibility toggle; Her Story added an Anti-Glare Filter, and Signalis was criticized for fuzzy text. Faces, sizes and lettering go through 平面视觉; this sweep only sets the grid and legibility targets.
- Add a per-computer flag for display-only versus interactive, matching SOMA's allowInteraction. Most desks just play ambient content (the existing ad loop and screensavers), and a few are interactive stations that matter. Interactive stations can work like Amnesia: The Bunker's fixed map and Alien's objective terminals: they pull the player out of safe rooms and into the Relay's space.
- Tie computer use to the Relay's honest senses: typing and reading stay quiet, so walking up and reading is safe while stage 0 holds. Discrete loud acts (the power-on POST beep, a dot-matrix printer, the door-release command) are noise events the Relay hears, the way noise triggers the alien's searches in Alien: Isolation. This is a design proposal, not sourced behaviour.
- WebGL tier: drive only one live GUI RenderTexture (the focused or nearest interactive screen) and show static or baked frames elsewhere, gated by platform under the WebGL-only rule. D3wasm shows that Doom 3's render-to-surface world GUIs run in a browser.
- Before locking the spec, verify the UNVERIFIED items by watching the listed footage: Alien: Isolation's exact terminal camera move and exit prompt; SOMA's camera focus and whether monsters act while a terminal is in use; whether Prey pauses at screens; Lethal Company's exit key; whether The Bunker pauses while reading.

## Real office computer UIs, 1989–1993

### Not every 1990 office desk had a PC, and many that did were terminals. A cubicle floor of that year mixes VGA PCs, green-screen terminals and empty desks.

In the October 1989 Current Population Survey, about 37% (36.8%) of employed US adults used a computer at work, up from 25% in 1984. IBM PS/2 (April 1987) sold mainly to corporations: 3 million in 18 months by Sept 1988. VGA arrived with it and was ubiquitous in PC compatibles within three years. The 8086 PS/2 Models 25/30 had MCGA, not VGA. Monitors were the 8512 (14-inch) and 8513 (12-inch), both 640x480, 1987. Mainframe and mini terminals (IBM 3270/5250, DEC VT) were still the standard office screen in big companies (see the terminal finding). Unverified: the exact 1990 split between PCs, terminals and Macs on US desks; I found no figure.

- [Census Statistical Brief SB91-11: The Growing Use of Computers (Oct 1989 CPS)](https://www.census.gov/library/publications/1991/demo/sb91-11.html) (government report): Source series for the 1989 at-work computer-use figure
- [Access and Use Issues (cites Census 1989: 36.8%)](https://bcn.boulder.co.us/community/resources/harsh/access.html) (secondary): 36.8% of employed adults used computers at work in 1989
- [Tech Monitor: 37% of 115m workers used a computer in 1989](https://techmonitor.ai/?p=163367) (period trade press): 37% in 1989 vs 25% in 1984
- [IBM Personal System/2 (Wikipedia)](https://en.wikipedia.org/wiki/IBM_Personal_System/2) (encyclopedia): PS/2 April 1987; corporate success; MCGA vs VGA; 8512/8513 640x480 1987
- [Video Graphics Array (Wikipedia)](https://en.wikipedia.org/wiki/Video_Graphics_Array) (encyclopedia): VGA 1987, ubiquitous within three years, 18-bit DAC

### MS-DOS 4.01 and its DOS Shell are the correct DOS for 1990. MS-DOS 5.0 (11 June 1991) is post-1990. The bare default prompt is 'C>', not 'C:\>'.

DOS Shell debuted in MS/PC-DOS 4.0 (June 1988). 4.01 was a bug-fix release in Nov 1988 (one chronology says Dec 1988). MS-DOS 5.0 launched 11 June 1991. I checked the 4.01 Shell 'Start Programs' screen on TechRepublic's gallery:
- a top bar with the date at left, the title 'Start Programs' in the centre and the time at right ('6:08 pm');
- a menu line 'Program  Group  Exit' with 'F1=Help';
- the heading 'Main Group' and the instruction 'To select an item, use the up and down arrows. To start a program or display a new group, press Enter.';
- items 'Command Prompt / File System / Change Colors / DOS Utilities...';
- blue text on white, under a dithered blue title bar.
The gallery says there were four fixed colour schemes and no custom ones, navigation used F10, Tab and the arrow keys (a mouse needed a driver), and F3 exited. The DOS COMMAND.COM default prompt is $N$G, which shows 'C>'. 'C:\>' needs PROMPT $P$G in AUTOEXEC.BAT. Unverified: whether the DOS 4 or 5 installers wrote $P$G by default.

- [Dinosaur Sightings: DOS 4.01 Shell (TechRepublic gallery, Greg Shultz)](https://www.techrepublic.com/pictures/dinosaur-sightings-dos-401-shell/) (screenshots): Start Programs layout, Main Group items, four colour schemes, F10/Tab/F3 keys (checked visually in browser)
- [DOS Shell (Wikipedia)](https://en.wikipedia.org/wiki/DOS_Shell) (encyclopedia): Debut in DOS 4.0, June 1988; TUI/COW; graphics modes on VGA
- [Computer Hope: Microsoft DOS history](https://www.computerhope.com/history/dos.htm) (reference): 4.01 Nov 1988; 5.0 June 1991
- [UPI 11 June 1991: Microsoft unveils new software (MS-DOS 5.0)](https://www.upi.com/Archives/1991/06/11/Microsoft-unveils-new-software/1031676612800/) (period news wire): MS-DOS 5.0 release date
- [jdebp FGA: command prompts](https://jdebp.uk/FGA/command-prompts.html) (technical FAQ): PC/MS-DOS COMMAND default prompt is $N$G, i.e. 'C>'

### WordPerfect 5.1 for DOS (6 Nov 1989) was the office word processor of 1990. Its look: light text on a blue field, an empty page, and a one-line status line at the bottom.

In 1990, 70% of law firms used WordPerfect (estimate) and 46% of respondents in an AICPA survey did. I checked the Commons screenshot visually:
- full-screen blue background, white text in 80 columns;
- the optional pull-down bar 'File Edit Search Layout Mark Tools Font Graphics Help (Press F3 for Help)' in grey on the top row;
- the bottom row: filename at left ('A:\DEC-IND.TXT') and 'Doc 1 Pg 1 Ln 1" Pos 1"' at right.
Keys: F3 Help, F1 Cancel, Reveal Codes (Alt-F3) as a split code view. Colours are set at Shift-F1 > Display > Colors/Fonts/Attributes. Mendelson's WPDOS pages call blue-with-white 'the familiar blue screen'. A 4.x document says the factory default was white on black, with blue as a setup option. Unverified: the 5.1 factory default colour, and whether the menu bar was hidden by default. Caveat: the Commons file is a 640x400 DOSBox capture, which means 8-dot cells. Real VGA text is 720x400 with 9-dot cells.

- [File:Wordperfect-5.1-dos.png (Wikimedia Commons)](https://commons.wikimedia.org/wiki/File:Wordperfect-5.1-dos.png) (screenshot): Blue screen, menu bar text, status line 'Doc 1 Pg 1 Ln 1" Pos 1"', 640x400 DOSBox capture (checked visually)
- [WordPerfect (Wikipedia)](https://en.wikipedia.org/wiki/WordPerfect) (encyclopedia): 5.1 Nov 1989; 1990 law-firm and AICPA usage; F3/F1 keys; Reveal Codes
- [WordPerfect for DOS Updated: text mode](https://mendelson.org/wpdos/textmode.html) (enthusiast documentation): Text mode, 25/50-line modes, status line
- [WPDOS 4.x notes (mendelson.org)](https://mendelson.org/wpdos/WP4X.pdf) (manual-derived PDF): Default white on black, with classic white on blue via setup (4.x; search snippet only)
- [WordPerfect 5.1 (DOS) at WinWorld](https://winworldpc.com/product/wordperfect/5x-dos) (software archive): Release context, screenshots
- [Computer Chronicles 'Macros' (1991; WordPerfect and 1-2-3 macros)](https://archive.org/details/macros_2) (period TV footage): Period on-screen footage (not watched; content per listing)

### Lotus 1-2-3 was the spreadsheet of 1990. Release 2.2 (1989) dominated, R3/R3.1 were current, and R2.3 WYSIWYG (1991) is too late.

Release dates: R2.01 July 1986; R2.2 1989; R3 1989; R3.1 1990 (WYSIWYG); R2.3 1991 (WYSIWYG). In a 1990 AICPA survey, 62% of spreadsheet users used 1-2-3. I checked the Commons R3 screenshot (640x480) visually:
- three-line control panel. Line 1 is the cell address and contents 'A:A1: 'EMP', with the mode indicator at top right ('MENU'; READY sits in the same spot);
- line 2 is the slash menu 'Worksheet Range Copy Move File Print Graph Data System Quit';
- line 3 is the submenu 'Global Insert Delete Column Erase Titles Window Status Page Hide';
- the frame is a cyan bar of column letters plus a cyan column of row numbers;
- data is light grey on black.
The 'A:' sheet prefix marks this as R3; the uploader first labelled it 2.2. R2.x shows a plain cell address.

- [File:Lotus-123-3.0-MSDOS.png (Wikimedia Commons)](https://commons.wikimedia.org/wiki/File:Lotus-123-3.0-MSDOS.png) (screenshot): Control panel, MENU indicator, slash menu text, cyan frame (checked visually)
- [Lotus 1-2-3 (Wikipedia)](https://en.wikipedia.org/wiki/Lotus_1-2-3) (encyclopedia): Release dates, 1990 AICPA 62%, slash menu, F1 help
- [CHM: Lotus 1-2-3 Release 2.2 package (1989)](https://www.computerhistory.org/collections/catalog/102778230) (museum artifact): R2.2 dated 1989, keyboard template in box
- [123elf wiki: Getting Started](https://github.com/taviso/123elf/wiki/Getting-Started) (documentation): '/' opens menu, first-letter selection, cell pointer

### Green-screen terminals were the most common office screen in large organisations: IBM 3270 (mainframe), IBM 5250 (System/36, AS/400) and DEC VT. Their colours were set by field attributes, not by the app.

IBM 3270 (1971) is the 'green screen'. 3278 sizes: Model 2 80x24, Model 3 80x32, Model 4 80x43, Model 5 132x27. The 3279 (1979) had 4 base or 7 extended colours. Late models had 24 PF keys and 3 PA keys. In base-colour mode the colour comes from field attributes:
- protected and intensified: white;
- unprotected and intensified: red;
- protected, normal: blue;
- unprotected, normal: green, so green marks where you type.
Office app: PROFS (1981) was renamed OfficeVision/VM in 1989. It had over a million users by the late 1980s, and its first screen is a menu of functions. IBM 5250: 24x80 green on black. The sign-on fields are System, Subsystem, Display, User, Password, Program/procedure, Menu and Current library. DEC VT220 (Nov 1983): 80x24 on a 12-inch tube, with white, green or amber phosphor and 8x10 glyphs in 10x10 cells. The VT320 came in 1987 and the VT420 in 1990. DEC's office suite was ALL-IN-1 (1982/83). The 3270 PC (1983) and IRMA boards let PCs emulate a 3270.

- [IBM 3270 (Wikipedia)](https://en.wikipedia.org/wiki/IBM_3270) (encyclopedia): Green screen, 3278 model sizes, 3279 colours, PF/PA keys, IRMA; photos File:IBM_3278_terminal.jpg, File:IBM-3279.jpg
- [IBM 3205 announcement (1984), functionally equal to 3279-2C](https://sharktastica.co.uk/resources/docs/txt/IBM_184-149_3205_1984_IBM.txt) (IBM announcement): Base-colour mapping from protect and intensify attributes
- [IBM 3179 terminal notes (argecy)](https://argecy.com/3179) (vendor documentation): Base colours white/red/blue/green by field attribute
- [IBM OfficeVision (Wikipedia)](https://en.wikipedia.org/wiki/IBM_OfficeVision) (encyclopedia): PROFS 1981, OfficeVision name May 1989
- [PROFS announcement, June 1983 (bitsavers)](https://bitsavers.mirrorservice.org/pdf/ibm/370/VM/SP/PROFS/profs_ann_198306.txt) (IBM announcement): First screen is a selection list of major functions
- [IBM 5250 (Wikipedia)](https://en.wikipedia.org/wiki/IBM_5250) (encyclopedia): 5251 24x80 green; 3180/5292 colour by attribute
- [IBM Support: interactive sign-on screen](https://www.ibm.com/support/pages/interactive-sign-screen) (vendor documentation): Sign On field list
- [VT220 (Wikipedia)](https://en.wikipedia.org/wiki/VT220) (encyclopedia): 80x24, white/green/amber, 8x10 in 10x10 cell, VT320 1987, VT420 1990
- [ALL-IN-1 (Wikipedia)](https://en.wikipedia.org/wiki/ALL-IN-1) (encyclopedia): DEC office automation 1982/83

### Novell NetWare was the office LAN of 1990. A networked PC boots DOS, loads the NetWare shell and logs in from drive F: with LOGIN.

NetWare held 53% of network operating systems in 1988 (IDC) and an estimated 40–60% in 1989. NetWare 386 v1.0 came in 1989 and was renamed 3.0 in 1990. Versions 3.10 and 3.11 are 1991 and 4.0 is 1993, so a 1990 office runs NetWare 2.x or 386/3.0. The DOS client loads LSL, IPXODI and NETX. LOGIN.EXE runs from the first network drive (F:) and asks for any name not given on the command line. SUPERVISOR and GUEST are created at install. The DOS login cue is a PC-speaker beep. In footage of a real CRT (NetWare 3.12), I read 'F:\SYSTEM>login supervisor'. Unverified: the exact prompt strings 'Enter your login name:' and 'Enter your password:'. I found no manual scan, so check the NetWare 3.x Utilities Reference before using them.

- [NetWare (Wikipedia)](https://en.wikipedia.org/wiki/NetWare) (encyclopedia): Version dates, 1988–89 market share
- [Novell TID: NetWare 3 Login Script Fundamentals](https://support.novell.com/techcenter/tips/ant19960703.html) (vendor documentation): LOGIN sets default server; SUPERVISOR/GUEST created at install
- [3Com remote boot readme (NetWare 3.10/3.11 files)](https://files.retropc.se/unpacked/hardware/net/novell/3cboot.exe/3cboot.txt) (period readme): LSL.COM, IPXODI.COM, NETX.COM client stack
- [iXBT NetWare FAQ ch.2](https://www.ixbt.com/comm/ev/nq02_q.html) (FAQ): LOGIN.EXE from first network drive; prompts for missing user/server
- [VOGONS thread on NetWare login sound](https://www.vogons.org/viewtopic.php?p=1091134) (forum): DOS client login cue is a PC-speaker beep
- [NetWare 3.12 Login (YouTube, MegaTech)](https://www.youtube.com/watch?v=UbpstCo7TYY) (footage): Real CRT showing 'F:\SYSTEM>login supervisor' (checked frame visually)

### Email and databases in 1990: cc:Mail on DOS, Lotus Notes 1.0 (Dec 1989, rare and costly), dBase III Plus and IV. I found no verified screenshots of cc:Mail or dBase screens.

cc:Mail was a DOS store-and-forward LAN mail system by Concentric/PCC Systems. Lotus bought it in 1991, so a 1990 cc:Mail screen carries no Lotus branding. Lotus Notes 1.0 shipped in December 1989 (7 Dec, per a secondary slide deck) with an OS/2 PM client and OS/2 server. 1.1, with a Windows 3.0 client, came in August 1990. Launch coverage cited an 80286 requirement and a $62,500 starting price, which makes it rare on a 1990 desk. Notes appears in Computer Chronicles 'Low Cost LANs' (c.1989). dBase IV shipped 24 Oct 1988. Its Control Center has six panels: Data, Queries, Forms, Reports, Labels, Applications. It also has the dot prompt, and ASSIST returns to the Control Center. dBase III Plus used 'The Assistant' and the dot prompt. Unverified: the cc:Mail DOS inbox layout and the dBase screen colours.

- [cc:Mail (Wikipedia)](https://en.wikipedia.org/wiki/Cc:Mail) (encyclopedia): DOS origin; Lotus acquisition 1991
- [TidBITS 25 Mar 1991: Microsoft calls Lotus's bet](https://tidbits.com/1991/03/25/microsoft-calls-lotuss-bet/) (period press): cc:Mail purchase weeks before March 1991
- [Lotus Notes (EDM2)](https://www.edm2.com/index.php/Lotus_Notes) (wiki): 1.0 Dec 1989; 1.1 Windows 3.0 client Aug 1990
- [ServerWatch: Whatever happened to Lotus Notes](https://www.serverwatch.com/guides/whatever-happened-to-lotus-notes/) (secondary): Notes 1.0 feature set; DOS/OS/2 clients
- [Computer Chronicles 'Low Cost LANs' (demos Lotus Notes)](https://archive.org/details/CC713_lans) (period TV footage): On-air demo of Notes (not watched)
- [Ashton-Tate dBASE IV press materials (CHM archive)](https://archive.computerhistory.org/resources/access/text/2017/02/102770558-05-01-acc.pdf) (press release): Ship date 24 Oct 1988; Control Center; dot prompt
- [About dBase IV (Pitt course notes)](https://people.cs.pitt.edu/~chang/156/06dbase.html) (course guide): Six Control Center panels; ASSIST

### Windows 3.0 (22 May 1990) and Mac System 6 are period-correct GUIs. Windows 3.1 (Apr 1992) and System 7 (13 May 1991) are not.

Windows 3.0 sold about 2 million copies in its first six months. I checked the Commons workspace screenshot visually:
- grey desktop with white inactive title bars and a blue active title bar;
- Program Manager 'Main' group: File Manager, Control Panel, Print Manager, Clipboard, DOS Prompt, Windows Setup;
- 'Accessories': Write, Paintbrush, Terminal, Notepad, Recorder, Cardfile, Calendar, Calculator, Clock, PIF Editor;
- Reversi as the game.
That screenshot's File Manager shows an 'MS-DOS_5' volume label, so it was captured later; don't copy that. The base 3.0 had no screen savers, TrueType or Minesweeper; all three arrived in 3.1. Computer Chronicles covered Windows 3.0 in autumn 1990. Mac System 6 shipped April/June 1988, with 6.0.8 in May 1991. Its window frames are black-and-white and the desktop is an 8x8 tiled pattern. Chicago (Susan Kare, 1984) was a single 12-point bitmap used for menus, dialogs and window titles. Its outline version (Bigelow & Holmes) is 1991. Unverified: Windows 3.0's exact default RGB values.

- [Windows 3.0 (Wikipedia)](https://en.wikipedia.org/wiki/Windows_3.0) (encyclopedia): 22 May 1990; 2M in 6 months; Program Manager; 16 colours
- [File:Windows 3.0 workspace.png (Wikipedia)](https://en.wikipedia.org/wiki/File:Windows_3.0_workspace.png) (screenshot): Default look, group contents, Reversi (checked visually)
- [Windows 3.1 (Wikipedia)](https://en.wikipedia.org/wiki/Windows_3.1) (encyclopedia): 6 Apr 1992; screensavers, TrueType, Minesweeper introduced
- [Computer Chronicles: Windows 3.0 (1990)](https://archive.org/details/windows30) (period TV footage): 1990 coverage of Windows 3.0 and rivals
- [System 6 (Wikipedia)](https://en.wikipedia.org/wiki/System_6) (encyclopedia): 1988 release; 6.0.8 May 1991; B/W frames
- [System 7 (Wikipedia)](https://en.wikipedia.org/wiki/System_7) (encyclopedia): 13 May 1991
- [Chicago (typeface) (Wikipedia)](https://en.wikipedia.org/wiki/Chicago_(typeface)) (encyclopedia): Kare 1984; single 12 pt bitmap for menus/titles; 1991 outline

### Screen savers: a 1990 office PC either blanked the screen or showed burn-in. Flying Toasters was Mac-only in 1990, and Windows savers are 1991–92.

John Socha's SCRNSAVE (Softalk, Dec 1983) blanked an IBM PC screen after 3 minutes. Screen savers existed to stop phosphor burn-in. Wikipedia says monochrome monitors were especially prone, but that line is tagged 'citation needed', so treat it as unverified. After Dark came to the Mac in 1989. Its default module 'Starry Night' is a pixel city skyline. The Flying Toasters art is © 1990. The Windows version is dated 1991 in the lead and 1992 (v2.0) in the release history. Windows built-in savers came only with 3.1 (1992).

- [Screensaver (Wikipedia)](https://en.wikipedia.org/wiki/Screensaver) (encyclopedia): Socha 1983, 3-minute blank; burn-in purpose
- [After Dark (software) (Wikipedia)](https://en.wikipedia.org/wiki/After_Dark_(software)) (encyclopedia): Mac 1989; Windows 1991/1992; Starry Night default; Flying Toasters
- [Aggressively Stupid: The Story Behind After Dark (Low End Mac)](https://lowendmac.com/2007/aggressively-stupid-the-story-behind-after-dark/) (retrospective): Origins; Windows port history
- [The Return of the Flying Toasters (masswerk)](https://www.masswerk.at/flyer) (retrospective): Toaster art © 1990 Berkeley Systems
- [Monochrome monitor (Wikipedia)](https://en.wikipedia.org/wiki/Monochrome_monitor) (encyclopedia): Burn-in susceptibility (citation needed), ghosting

### The best match for the EGRESS fiction is a real PC fire-alarm graphics workstation from 1989–91. Their screens are documented in patents: floor plans, a red/yellow/green state, flashing unacknowledged events and an event queue.

US5189394 (Grinnell, priority 22 Dec 1989):
- an IBM PC/AT-compatible with a colour monitor, connected to the fire panel by an interface card;
- building floor plans stored as bitmaps, with sensors green when inactive, red in alarm and yellow in fault;
- unacknowledged devices flash; the active sensor is circled by a flashing red ring;
- bottom-left box shows device details, bottom-right box shows alarm and fault counts plus panel messages;
- function keys or a touchscreen to zoom or go to the next alarm; disk logs and a printer; up to 190 alarms.
US5297252 (General Signal, filed 7 May 1991):
- nine fixed windows: title, status, time/date, main graphic, two secondary graphics, command menu and two message windows;
- status background red for alarm, yellow for trouble, green for standby;
- four queues (alarm, trouble, supervisory, monitor) stepped with NEXT/PREVIOUS;
- an Elographics touchscreen;
- strings 'SYSTEM IN ALARM', 'TOUCH SCREEN TO ACTIVATE SYSTEM' and 'Select point to be acknowledged'.
Notifier AM2020/AFP1010 panels drove CRT-2 serial terminals (up to 25) and a printer log. Display grammar from the manual: 'ALL SYSTEMS NORMAL' plus time and date, status words ALARM/TROUBL/NORMAL/DISABL, addresses such as L01D02 or A01P01, and 'POINT TROUBLE'. That manual is rev K from 2001; the AM2020 launch year is unverified. Building automation: JCI says it introduced Metasys in 1990, joining fire, security, HVAC and lighting with a PC host (company and secondary claims). Andover filed the 'Infinity' BAS trademark in April 1990.

- [US5189394A Fire alarm display (Grinnell, 1989 priority)](https://patents.google.com/patent/US5189394) (patent): PC floor-plan alarm display, colours, flashing, info boxes
- [US5297252A Color graphics terminal for monitoring an alarm system (General Signal, 1991)](https://patents.google.com/patent/US5297252) (patent): Nine-window layout, status colours, queues, touch, on-screen strings
- [Notifier AM2020/AFP1010 manual (rev K)](https://www.manualslib.com/manual/1007977/Notifier-Am2020.html) (manual): CRT-2 terminals, ALL SYSTEMS NORMAL, status words, addresses, printer
- [Siemens Desigo CC: Notifier AM events](https://mybuilding.siemens.com/D025938170736/Help/EngineeringHelp/en-US/23072064395.html) (vendor documentation): 'CRT Terminal Offline' event
- [Johnson Controls: Metasys made for control freaks](https://www.johnsoncontrols.com/building-insights/feature-story/metasys-made-for-control-freaks-by-control-freaks) (company history): Metasys introduced 1990; unified fire safety, security, HVAC
- [cbnme.com on METASYS 1990 PC host](https://cbnme.com/?p=17894) (secondary trade press): PC used as BMS host in 1990
- [Andover Controls trademarks (Justia)](https://trademarks.justia.com/owners/andover-controls-corporation-503637) (trademark record): Infinity filed 6 Apr 1990

### Colour palettes: PCs use one fixed 16-colour RGBI text palette with a special brown. VGA can redefine it from 262,144 colours, but office software used the defaults.

The default 16 colours (CGA, kept by EGA and VGA):
- 0 #000000, 1 #0000AA, 2 #00AA00, 3 #00AAAA, 4 #AA0000, 5 #AA00AA, 6 #AA5500 (brown), 7 #AAAAAA;
- 8 #555555, 9 #5555FF, 10 #55FF55, 11 #55FFFF, 12 #FF5555, 13 #FF55FF, 14 #FFFF55, 15 #FFFFFF.
Brown is the exception: IBM's monitor circuitry turned dark yellow #AAAA00 into brown, and many clone monitors showed #AAAA00. Each text cell is a character byte plus an attribute byte with 4-bit foreground, 3-bit background and a blink bit. The VGA DAC is 18-bit (6 bits per channel, 262,144 colours), so 'AA' is really 42/63, about 170. A WordPerfect classic-mode guide suggests R,G,B 0,0,168 for 'DOS blue'.

- [Computer Hope: CGA](https://www.computerhope.com/jargon/c/cga.htm) (reference): 16-colour hex values
- [EGA Palette (ModdingWiki)](https://moddingwiki.shikadi.net/wiki/EGA_Palette) (technical wiki): EGA default = CGA 16; #AAAA00 clone-monitor brown
- [Color Graphics Adapter (Wikipedia)](https://en.wikipedia.org/wiki/Color_Graphics_Adapter) (encyclopedia): Brown circuitry; per-cell fg/bg; 80x25 8x8
- [VGA text mode (Wikipedia)](https://en.wikipedia.org/wiki/VGA_text_mode) (encyclopedia): Attribute byte, blink, 16-colour BIOS table
- [Wikibooks mirror: tweaking WP Classic Mode](https://kwx.liehendr.com/content/wikibooks_en_all_maxi_2021-03/A/WordPerfect) (how-to): DOS blue = RGB 0,0,168

### Monochrome phosphors: P1 green (525 nm), P3 amber (602 nm) and P4 'page white'. The IBM 5151's P39 green has a long afterglow that smears on scroll.

P1 is Zn2SiO4:Mn, peak 525 nm. P3 is Zn8BeSi5O19:Mn, peak 602 nm (some tables call it yellow). P4 is the black-and-white TV white. Amber was sold as lower-fatigue and became a standard in parts of Europe (COMPUTE! 1982; Grandjean 1986). The IBM 5151 used P39: a bright green with high persistence, chosen to hide 50 Hz flicker. It smears on scroll, and one owner saw near 20 s to full decay in a cold room (anecdote). Approximate sRGB, converted from wavelength rather than measured on a display: P1 about #33FF33 to #08FF35; P3 about #FFB000 to #F67800. Treat these as starting points to tune by eye.

- [Monochrome monitor (Wikipedia)](https://en.wikipedia.org/wiki/Monochrome_monitor) (encyclopedia): P1 green, P3 amber, P4 white; ergonomics; ghosting
- [IBM 5151 (Wikipedia)](https://en.wikipedia.org/wiki/IBM_5151) (encyclopedia): P39, high persistence, smearing, 50 Hz
- [CRT phosphor research (Jankowiak, PDF)](https://ww.labguysworld.com/crt_phosphor_research.pdf) (technical compilation): P1 525 nm, P3 602 nm, compositions
- [What Color Are Phosphors? (xybre notes)](https://publish.obsidian.md/xybre/permalink/e397c38e-aa6f-45eb-831f-f02a3836abc4) (secondary compilation): Wavelength-to-hex approximations for P1/P3
- [cctalk: IBM 5151 common faults (decay anecdote)](https://classiccmp.org/pipermail/cctalk/2014-November/000346.html) (mailing list): Near 20 s decay anecdote
- [COMPUTE! Oct 1982: amber screen monitor](https://www.atarimagazines.com/compute/issue29/423_1_New_Products_Amber_Screen_Monitor.php) (period magazine): Low-fatigue amber adopted as standard in European nations

### Text grid and fonts: VGA text is 80x25 cells of 9x16 (720x400, 70 Hz) stretched onto a 4:3 tube, so the pixels are tall. Legal stand-ins exist: Px437 IBM VGA 9x16 (CC BY-SA 4.0) and VT323 (OFL, already in the kit).

Cell sizes by adapter: VGA 9x16 (720x400), MDA/Hercules 9x14 (720x350), EGA 8x14 (640x350), CGA 8x8 (640x200). Glyphs are 8 dots wide. The 9th column is blank, except that C0–DF repeat column 8 so box-drawing lines join. The cursor is full cell width. Fonts are CP437 from the adapter ROM. On a 4:3 tube a 720x400 pixel is about 1.35x taller than wide; int10h quotes a pixel aspect of about 0.75. The int10h 'Ultimate Oldschool PC Font Pack' by VileR has Px437/Ac437 IBM VGA 9x16 (Ac = aspect-correct) under CC BY-SA 4.0. VT323 (Peter Hull, OFL 1.1, Reserved Font Name 'VT323') is traced from DEC VT320 glyphs and is already in Assets/Fonts/Period1990 for CG/teletext. Chicago belongs to Apple and cannot ship.

- [VGA text mode (Wikipedia)](https://en.wikipedia.org/wiki/VGA_text_mode) (encyclopedia): 80x25, 9x16, 720x400, 9th column rule, cursor, ROM fonts
- [INT 10h mode table (RBIL mirror)](https://mirror.cs.msu.ru/oldlinux.org/Linux.old/docs/interrupts/int-html/rb-0069.htm) (technical reference): Cell sizes per adapter for modes 02/03/07
- [int10h Oldschool PC Fonts readme](https://int10h.org/oldschool-pc-fonts/readme/) (font documentation): CC BY-SA 4.0, Px/Ac/Mx naming, 720x400 on 4:3 aspect
- [FreshPorts: oldschool-pc-fonts](https://www.freshports.org/x11-fonts/oldschool-pc-fonts) (package metadata): Licence CC-BY-SA-4.0; IBM VGA 9x16 files
- [VT323 (Font Squirrel, OFL)](https://www.fontsquirrel.com/fonts/vt323) (font): OFL licence; DEC VT320 origin
- [Chicago (typeface) (Wikipedia)](https://en.wikipedia.org/wiki/Chicago_(typeface)) (encyclopedia): Apple-owned system bitmap face

### IBM Common User Access (CUA, Dec 1987) is a real period rule-set for a consistent 'computer UI system'.

CUA (part of IBM SAA) is a 328-page specification. Its rules:
- every action works by keyboard or mouse; F10 opens the menus and Alt plus a letter opens one;
- File and Edit come first and Help is last;
- commands that need more input end in '...';
- F1 gives context help; Esc cancels and Enter confirms; Tab and Shift+Tab move between fields;
- Shift+Del, Ctrl+Ins and Shift+Ins cut, copy and paste.
OS/2, Windows, OS/400 and the MS-DOS 5 editor followed it. The DOS 4.01 Shell uses F10 and F1=Help. WordPerfect did not follow CUA (F3 = Help), which shows how real 1990 apps disagreed with each other.

- [IBM Common User Access (Wikipedia)](https://en.wikipedia.org/wiki/IBM_Common_User_Access) (encyclopedia): 1987 SAA spec and its key conventions; WordPerfect non-CUA
- [Dinosaur Sightings: DOS 4.01 Shell](https://www.techrepublic.com/pictures/dinosaur-sightings-dos-401-shell/) (screenshots): F10/F1/F3 usage in the Shell

### Anachronism check against the 1990 lock (and the hard limit of 1993).

Fits 1990:
- MS-DOS 3.3/4.01 and the DOS Shell;
- WordPerfect 5.0/5.1, Lotus 1-2-3 R2.2/R3/R3.1, dBase III Plus/IV;
- cc:Mail without Lotus branding, PROFS/OfficeVision, ALL-IN-1;
- 3270/5250/VT220/VT320 terminals;
- NetWare 2.x/386 3.0;
- Windows 3.0 without screen savers;
- Mac System 6 with Chicago;
- PC fire-alarm graphics and Metasys-era building automation.
In period but rare: Lotus Notes 1.0 and the Windows 3.0 desktop (most office PCs were still DOS-first; unverified share).
After 1990 but before 1993, so avoid in a 1990 snapshot:
- MS-DOS 5.0 (Jun 1991) and 6.0;
- Windows 3.1 (Apr 1992: Minesweeper, TrueType, built-in savers);
- System 7 (May 1991);
- NetWare 3.11 (1991), 4.0 (1993);
- 1-2-3 R2.3 WYSIWYG (1991);
- After Dark for Windows (1991/92);
- 'Lotus cc:Mail' (1991).
Also flag: the Commons WordPerfect capture (2015) and the Windows 3.0 capture (MS-DOS 5 label) were made later. Use them for layout only, not for pixels.

- [Windows 3.1 (Wikipedia)](https://en.wikipedia.org/wiki/Windows_3.1) (encyclopedia): Apr 1992 features
- [System 7 (Wikipedia)](https://en.wikipedia.org/wiki/System_7) (encyclopedia): May 1991
- [NetWare (Wikipedia)](https://en.wikipedia.org/wiki/NetWare) (encyclopedia): 3.11 1991, 4.0 1993
- [Lotus 1-2-3 (Wikipedia)](https://en.wikipedia.org/wiki/Lotus_1-2-3) (encyclopedia): R2.3 1991
- [Computer Hope: DOS history](https://www.computerhope.com/history/dos.htm) (reference): DOS 5.0 June 1991

**Recommendations from this sweep:**

- Build the in-game computer UI as one VGA text-mode system:
- 80x25 CP437 cells at 9x16 into a 720x400 RenderTexture, shown on the 4:3 tube face (tall pixels, as on hardware);
- the 16-colour default palette;
- redraw only when a cell changes.
This is cheap enough for the WebGL tier and touch (cells map to tap targets), and gives one grammar for every screen.
- Follow IBM CUA (1987) as the house rule-set: menu bar row 1 with F10/Alt, Help last, F1 help, Esc cancel, Enter OK, a function-key legend on row 25. On touch, mirror the F-key legend as on-screen buttons. One fictional in-house suite then reads like 1990 software without copying a product.
- Ship fictional software only. WordPerfect's blue field with 'Doc 1 Pg 1 Ln 1" Pos 1"', 1-2-3's control panel and slash menu, and the NetWare/IBM names are real trade dress and trademarks. That is the same P1 problem SCREENS_VISUAL_SYSTEM.md raised for LEVITZ. Borrow the grammar (status line, three-line control panel, mode indicator, field colours), not the names or exact strings.
- Make screen content follow desk type:
- most cubicles: off, or a blanked monitor with a faint burn-in ghost;
- some: a DOS 'C>' prompt with blinking cursor, a word processor mid-document, or a spreadsheet at READY;
- a few: green or amber terminals on 3270-style colour rules (green = typeable field);
- a networked PC: a NetWare-style F: login.
Verify the NetWare prompt strings before using them; they are unverified.
- For EGRESS, give one special desk (security or engineering office, not every cubicle) a fire-alarm graphics workstation app based on the 1989–91 patents and the Notifier CRT terminal: floor plan, red/yellow/green status window, flashing unacknowledged points, ALARM/TROUBLE/SUPERVISORY queues with NEXT/PREVIOUS, and a time/date window. It could carry the Relay's staged warnings in its own fiction. This is a proposal only: the narrative chat writes the strings and Red and the system owners decide the hook.
- Phosphor tiers:
- colour VGA for PCs;
- amber P3 (start near #FFB000, tune by eye) and green P1 (start near #33FF33) for terminals and cheap monochrome PCs;
- a P39-style persistence/smear parameter for green tubes.
The hex values are wavelength conversions, not measurements; check them against footage of a real tube.
- Fonts (route through 平面视觉 per the type rule):
- Px437/Ac437 IBM VGA 9x16 from the int10h pack is the reference PC face, but it is CC BY-SA 4.0, outside the kit's OFL/GUST/Apache set, so it needs a licence decision before it ships;
- VT323 (OFL, already in Assets/Fonts/Period1990) covers DEC-terminal screens;
- do not ship Chicago; if a Mac desk is wanted it needs a licensed stand-in, or leave Macs out of the US office.
- Hold the 1990 snapshot strictly. Avoid MS-DOS 5 or 6, Windows 3.1 savers or Minesweeper, System 7, NetWare 3.11+, 1-2-3 R2.3 WYSIWYG, Lotus-branded cc:Mail, and Flying Toasters (Mac-only and branded). A plain blank-after-N-minutes is the correct idle state for a 1990 DOS PC.
- Before the spec is final, check these against primary sources:
- the NetWare 2.x/386 LOGIN prompt wording (Utilities Reference);
- WordPerfect 5.1's factory default colours and menu-bar visibility;
- a cc:Mail DOS screen;
- a 1990 5250 sign-on photo;
- the AM2020 launch year;
- Computer Chronicles footage (archive.org windows30, macros_2, CC713_lans) for frames of real office CRTs in use.

## What the computers should say (fit inside this project)

### Hard limits on any computer content, all drawn from existing project decisions

Seven rules bind the computers.
1. **No Relay state.** No position, distance, state, Attention or Pressure readout, in text or as a meter. All Relay state stays behind the off-by-default assist.
2. **No live warning text.** The warnings are explained once, in the pause-card legend, never live.
3. **No word "relay".** Player-facing text never uses it for panel sounds.
4. **Dark is never cover (LD R13).** No screen may say or imply it.
5. **Era and brands.** 1990 US caps, no real brands, printed dates up to 1990.
6. **Ownership.** Narrative owns every on-screen string. 平面视觉 owns faces, sizes, layout and separators.
7. **A.11 is frozen.** Its ink strings can be quoted on a screen (SHEET A-3 OF 4, OCCUPANT LOAD) but never changed. Computer strings are a new narrative item, not an A.11 edit.

30_screen_strings.md §7 already applies these rules to screens. EGRESS adds one more: "The fiction lives in objects and signs. The HUD never names the system." A diegetic terminal may therefore name the alarm system, but the HUD may not.

- [RELAY_PURSUIT_REDESIGN.md:193 (no Relay state on HUD)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/RELAY_PURSUIT_REDESIGN.md#L193) (project-file): Relay readout is an assist, off by default
- [RELAY_PURSUIT_REDESIGN.md:509 and :593](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/RELAY_PURSUIT_REDESIGN.md#L509) (project-file): Never 'relay' in player text; warnings explained once in the pause card, never live
- [research/relay_pursuit/30_narrative.md:134-141 (writing rules)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/relay_pursuit/30_narrative.md#L134) (project-file): Dark is never cover; warnings never a stealth rule; period and brands; fiction lives in objects; HUD never names the system
- [research/screens/30_screen_strings.md:4 and :95-99](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/screens/30_screen_strings.md#L95) (project-file): Narrative owns words, 平面视觉 owns type; screens never give Relay position or post-1990 dates
- [research/wallpaper_motion/30_narrative_phosphor.md:341-343 (A.11 frozen)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/wallpaper_motion/30_narrative_phosphor.md#L341) (project-file): Strings frozen; any change needs a new A.11 revision; type provisional with 平面视觉
- [research/wallpaper_motion/20_level_design_phosphor.md:181-192 (R13, R14)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/wallpaper_motion/20_level_design_phosphor.md#L181) (project-file): Dark is never cover; all Relay text hidden; threat reaches the player only through the staged warning

### Computers in play exist only in Office zones, and almost every desk has one, so only a designated few can carry information

**Where they are.**
- The Office is a theme on 30 % of Standard-height zones (`officeShare: 0.3`). The HUD calls it "LEVEL 4 / OFFICE".
- `BuildStation` puts a monitor, keyboard and mouse on every station except a 12 % roll. A phone appears on 45 % of stations and binders on 35 %.
- The RoomStream Office profile is unreachable today.
- The title-stream rooms must stay free of notes.

**What they do now.** Every Office monitor gets `FrontRoomsScreenVideo`, plays the same ad, and toggles power on E.

**What this means for design.**
- The inspect design's rarity rule applies (P11: Control's sequel was faulted for "reading breaks"). A computer needs content that cannot be read at 0.8 m before it earns a close-up.
- It also needs a tell that reads from the prompt range, so the player knows it is worth the walk.
- So most Office CRTs must stay ambient (off, idle, DOS prompt, locked). About one information terminal per Office zone should carry content.
- That terminal needs a distinct look from about 3 m. Being switched on is not enough, because the screens plan already puts about 1 set in 3 on.

- [Assets/Scripts/Office/FrontRoomsOfficeKit.cs:862-880 (BuildStation)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/Office/FrontRoomsOfficeKit.cs#L862) (project-code): Monitor on all but a 12 % roll of stations; phone 45 %, binders 35 %; AttachScreen on every monitor
- [Assets/Levels/FrontRoomsLevel0.asset:43 (officeShare 0.3)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Levels/FrontRoomsLevel0.asset#L43) (project-data): Office share of Standard zones
- [Assets/Scripts/FrontRooms3DGame.cs:1490-1498 (ZoneName)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/FrontRooms3DGame.cs#L1490) (project-code): Office zones are named LEVEL 4 / OFFICE on the HUD
- [research/inspect/01_inventory.md:140-149 (S1 Office CRT)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/inspect/01_inventory.md#L140) (project-file): Every monitor plays the ad; E toggles power; 88 % of stations; stream profiles unreachable (01 §1)
- [research/inspect/10_design.md:71-75 (rarity and readable-from-range rule)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/inspect/10_design.md#L71) (project-file): Close-up only for rare assets whose text misses 18.6′ at 0.8 m, and each must read as worth the walk from the prompt range
- [SCREENS_VISUAL_SYSTEM.md:74 (about 1 set in 3 on)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/SCREENS_VISUAL_SYSTEM.md#L74) (project-file): Being on is not a unique tell
- [research/relay_pursuit/30_narrative.md:141](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/relay_pursuit/30_narrative.md#L141) (project-file): Title stream rooms stay free of notes

### The Office is exactly where the wallpaper guidance cannot speak, so terminals can fill that gap

**Why the walls are silent in the Office.**
- Office walls are drywall (`Office_Wall`), not WP03 paper.
- The glow ink's carrier rule (LD R3) excludes Office drywall.
- The pattern-native cue's carriers are WP03's own chevron stacks and motif arrows, so it has no carrier in the Office either. This is inferred from the two docs; no doc states it outright.
- EGRESS already explains the gap: "Office zones are tenant fit-out: drywall, not on the base-building plan."

**Why a terminal may show a route when the TV may not.** The TV's slate rule says "No arrows or directions, because a TV doesn't know the route." A terminal on the building's own network is the building talking, and EGRESS says the building knows the building but not the monster. A terminal could therefore carry the plan inside the Office.

**A fiction-consistent detail.** The tenant suite itself can appear on the plan as an unplanned blank, such as "TENANT — NOT SHOWN". This matches the placard's period-true imperfection (the misaligned you-are-here maps of Levine 1984).

- [LEVEL_MODULE_SPEC.md:58 (Office = drywall)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/LEVEL_MODULE_SPEC.md#L58) (project-file): Office walls are Office_Wall drywall, no wallpaper
- [research/wallpaper_motion/20_level_design_phosphor.md:93-94 (R3 carriers)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/wallpaper_motion/20_level_design_phosphor.md#L93) (project-file): Only wallpaper carries ink; no ink on Office drywall
- [research/wallpaper_motion/32_pattern_native_hints.md:24-25](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/wallpaper_motion/32_pattern_native_hints.md#L24) (project-file): Cue carriers are WP03 elements (so none on drywall: inference)
- [research/wallpaper_motion/30_narrative_phosphor.md:79-80 and :210-212](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/wallpaper_motion/30_narrative_phosphor.md#L210) (project-file): Office is tenant fit-out, off the base-building plan; ink knows the building not the monster
- [research/screens/30_screen_strings.md:79](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/screens/30_screen_strings.md#L79) (project-file): A TV doesn't know the route, so no arrows on the broadcast slate
- [research/placard/10_spec.md:144-149 (misaligned YOU ARE HERE, Levine 1984)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/placard/10_spec.md#L144) (project-file): Period-true imperfection of posted plans is an accepted design move

### A floor plan of THIS generated map can be honest only about what the generator keeps stable

**What stays true.**
- Zones (Voronoi), ceiling heights, border edges and corners, and keys are pure functions of the seed and world coordinates.
- A revisit shift rebuilds the maze, rooms and interior walls, but "Borders, zones and keys stay the same."

**What the plan can show, then.**
- **Must:** zone outlines (compartments) with their height class, and border exits. Low/Standard borders open as doors; any border with a Tall side opens as windows. The plan must not draw interior walls, which the next revisit would make false.
- **Can:**
  - **YOU ARE HERE** at the terminal's own position. This is the NYC Local Law 5 (1973) idea of a diagram showing "the location where it is posted", which the placard spec cites.
  - Visited compartments stamped as cleared, read from `zonesVisited`. EGRESS: "A compartment you have left counts as evacuated."
  - Optionally a WAY ON route.

**What the route must do.** It must come from the same shared map function as the wall cue: `FrontRoomsWayfinding`, which is not built and waits for Red's go. That way the plan and the walls always agree, as lamps and walls share one RoomSet. It inherits the cue's rules:
- the nearest unvisited threshold;
- never glass;
- built cells only, never forcing generation;
- doors without the key count as walls.

**Two open dependencies.**
- "What is an exit?" was answered on 2026-10-22: each floor has an EXIT that leads one floor deeper (C22), and the route points at the station TV until the floor key is taken, then at the EXIT (C24 = A + A20; rules in `research/cue_driver/MAP_ROUTE_SPEC.md`). The terminal still never invents an exit of its own: it may only show that one.
- Any glyph on the plan must use the placard's legend grammar (WAY ON / EXIT / NO EXIT), so one legend teaches walls, placard and screen.

- [MAP_GENERATION.md:104-117 (zones, heights, keys, guarantees, revisit shift)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/MAP_GENERATION.md#L104) (project-file): Doors between Low/Standard, windows at Tall; one key per Low/Standard zone; borders pure functions of seed; revisits keep borders, zones, keys
- [Assets/Scripts/FrontRooms3DGame.cs:201 and :1072-1086 (zonesVisited)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/FrontRooms3DGame.cs#L1072) (project-code): The game already tracks visited zones (data for a 'cleared' stamp)
- [research/wallpaper_motion/30_narrative_phosphor.md:198](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/wallpaper_motion/30_narrative_phosphor.md#L198) (project-file): A compartment you have left counts as evacuated
- [RELAY_PURSUIT_REDESIGN.md:545-547 (shared room set; cue never points at glass; built cells only)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/RELAY_PURSUIT_REDESIGN.md#L545) (project-file): One shared function so lamps and walls agree; target ladder; never glass; no generation forced
- [research/cue_driver/CUE_DRIVER_INTERFACE.md:57 and :82](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/cue_driver/CUE_DRIVER_INTERFACE.md#L82) (project-file, v0.2.2): 'What is an exit?' was answered on 2026-10-22 (C22; C24 = A + A20); FrontRoomsWayfinding is not built yet (after the Alpha)
- [research/placard/10_spec.md:133-137 (NYC LL5 1973)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/placard/10_spec.md#L133) (project-file): Period code: a diagram showing the location where it is posted
- [research/wallpaper_motion/30_narrative_phosphor.md:481](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/wallpaper_motion/30_narrative_phosphor.md#L481) (project-file): No placard glyph may differ from the wall grammar

### Things the plan must never mark

**Never mark, each because it becomes radar or an all-clear:**
- **The Relay itself, or any trace of it.** It knows the building, not the monster (LD R5; A.3: "treat the plan as a map, never as a warning").
- **Relay entries (dead-end tips).** These are where Arrive places it, so marking them reveals where it comes from. This reasoning is mine, by analogy with the rules below.
- **Trigger rooms and devices.** "The wallpaper never marks a trigger room. That would make it radar." A terminal list of detector rooms would be the same radar. Devices are already readable on site by rule: an ALARM ZONE card and a red-banded detector visible from 2 m outside.
- **The quiet-player sweep target.** RELAY §6: "No extra tell: the lamps are the tell."

**Truth rules (LD R9, Exit 8 tell rule).**
- Before T4 nothing lies.
- If anything on a computer ever lies, it must have a tell and must not be the building's plan.

- [research/relay_pursuit/30_narrative.md:76-79](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/relay_pursuit/30_narrative.md#L76) (project-file): Trigger rooms readable before entry; wallpaper never marks them (radar)
- [RELAY_PURSUIT_REDESIGN.md:333 and :340-343](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/RELAY_PURSUIT_REDESIGN.md#L333) (project-file): Sweep has no extra tell; ≥ 2 Relay entries per zone; device visible from 2 m; ALARM ZONE signature
- [research/wallpaper_motion/20_level_design_phosphor.md:112-122 and :147-148 (R5, R9)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/wallpaper_motion/20_level_design_phosphor.md#L147) (project-file): Calm routes never read the Relay; truth before T4
- [research/wallpaper_motion/30_narrative_phosphor.md:121 and :226](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/wallpaper_motion/30_narrative_phosphor.md#L226) (project-file): Every lie has a tell (Exit 8); plan as a map, never a warning

### An alarm event log can teach cause and effect, but only as history the player already perceived

**What exists.** Narrative already wrote a static log line: `ALARM LOG 03/90` / `ZONE 04  TROUBLE  ACK`, "Never live, never the player's zone."

**What a stronger version could do.** It could list the player's own past calls by cause, in EGRESS steps (device or noise → verified → reset). That directly serves goal G2 ("≥ 8/10 playtesters name the cause after a chase"). The data already exists as the director event `Called(cause, explicit, pos)` and `Restore()`.

**Leak rules it must keep:**
- Only closed encounters the player perceived: stage ≥ 1 felt, or a device they tripped.
- Never an entry for an open encounter, a PENDING or queued call, a sweep, Attention or Pressure.
- No visible RESET or all-clear at the moment it leaves. RESTORE is audio-only and plays only when stage ≥ 1 was felt; "the paper is never an all-clear".
- No live clock. A timestamped RESET would let a player count down the fixed RELAX (100 s at T1). Use the frozen-time fiction instead: the wall clock is stopped at 10:08:37, and "time here is wrong". *(Superseded 2026-10-22: Red asked in the 游戏视觉 chat that clocks and computers show real-world time, "希望新的钟表是可以正常走动，并且呼应现实世界的时间的（钟表只是一点，还有游戏里面的电脑等一系列有时间显示相关的设备的）". RELAX stays countable by design; see RELAY §11's note.)*

**Period facts.**
- Verified in the project: the amber TROUBLE lamp (Fire Engineering, April 1990); pre-alarm as a concept (1985 patent); alarm verification (1986).
- UNVERIFIED: a 1990 US panel's event-history printout or log. My search found only current products (Honeywell PRN-6/F, Dahua). The one older hit, Autronica 116-BS-100 "tested to BS 5839-4:1988", is undated and British.
- So the log should be framed as invented, like the underprint.

- [research/screens/30_screen_strings.md:83-93 (office monitor lines)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/screens/30_screen_strings.md#L83) (project-file): Existing alarm log, print queue, idle and DOS lines; static, deterministic, no live threat info
- [RELAY_PURSUIT_REDESIGN.md:207 (G2) and :721 (Called/Restore hooks)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/RELAY_PURSUIT_REDESIGN.md#L207) (project-file): Every call logged with cause; director events available
- [RELAY_PURSUIT_REDESIGN.md:494 and :531 (RESTORE audio only; never an all-clear)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/RELAY_PURSUIT_REDESIGN.md#L494) (project-file): Restore only on true Away if felt; no visual all-clear
- [research/relay_pursuit/30_narrative.md:149-158 (verified alarm facts)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/relay_pursuit/30_narrative.md#L149) (project-file): Trouble lamp April 1990, verification 1986, pre-alarm concept 1985 with URLs
- [Fire Engineering, 1990-04-01, Increasing your fire alarm literacy](https://www.fireengineering.com/firefighting/increasing-your-fire-alarm-literacy-3/) (trade article (cited in project)): Amber trouble light and audible signal at the panel, 1990
- [research/inspect/01_inventory.md:176-187 (wall clock frozen 10:08:37)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/inspect/01_inventory.md#L180) (project-file): Time has stopped in the existing props
- [Autronica 116-BS-100 listing (search result only, not fetched)](https://www.lavastica.com/fire-gas/Autronica-116-BS-100/) (web (search snippet)): UNVERIFIED: historical event logging + optional printer, BS 5839-4:1988; undated, British
- [Honeywell PRN-6/F panel printer (search result only, current product)](https://buildings.honeywell.com/us/en/products/by-category/control-panels/parts-and-accessories/printers/printer-9-pin-dot-matrix) (web (search snippet)): Panel event printers exist today; NOT evidence for 1990

### Showing dead circuits on a terminal would locate sanctuaries, which is a design decision, not a content default

**What a sanctuary is.** A v2.1 "dead circuit" room carries an amber TROUBLE lamp and the placard "ALARM CIRCUIT OUT OF SERVICE · NO DETECTION IN THIS ROOM". The Relay never enters it.

**The temptation.** A real panel's trouble list names out-of-service zones, so a terminal could truthfully list sanctuary compartments.
- It is building knowledge, not monster knowledge, so it passes the LD R5 test.
- But it would turn the plan into a safe-room finder. That conflicts with the sanctuary density (about 1 per 3–4 zones) and with G5, which must hold with no sanctuaries in v2.0.

**Status.** Sanctuaries themselves are Red's open Q7c.

**Three options for Red:**
1. Never list dead circuits.
2. List only the terminal's own and adjacent compartments.
3. List all of them.

Whichever is chosen, keep the existing rule's spirit: "the zone key is never only inside a trigger room or a sanctuary". By the same logic, an information terminal should never stand inside a trigger room (reaching it would arm the room 1.5 m inside), and should never be the only way to learn something essential.

- [RELAY_PURSUIT_REDESIGN.md:594-599 (sanctuaries)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/RELAY_PURSUIT_REDESIGN.md#L594) (project-file): Dead-circuit props, density, never the only key place, v2.1
- [RELAY_PURSUIT_REDESIGN.md:336 and :342 (placement rules)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/RELAY_PURSUIT_REDESIGN.md#L336) (project-file): Key never only in trigger room/sanctuary; trigger arms 1.5 m inside
- [RELAY_PURSUIT_REDESIGN.md:870 (Q7c open)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/RELAY_PURSUIT_REDESIGN.md#L870) (project-file): Sanctuaries still Red's open question
- [research/relay_pursuit/30_narrative.md:88](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/relay_pursuit/30_narrative.md#L88) (project-file): Dead-circuit room as panel trouble signal on a dead zone

### Three numbering and naming systems already collide, and a terminal plan would expose the collision

**The three systems.**
1. **The HUD's "ZONE nn"** is a count of zones visited (`zonesVisited.Count.ToString("00")`), not an id. RELAY v2 keeps that line.
2. **Door number plates and key tags** carry a 2-digit 00–99 number per zone. The HUD would say "KEY 14". This waits on D1.5, and `doorsNeedKeys` is 0 today.
3. **Level names differ.** The HUD calls Office zones "LEVEL 4 / OFFICE" (Backrooms-wiki numbering), while the EGRESS drawing set says "LEVEL 0 | SHEET A-3 OF 4".

**Why it matters.** A terminal plan that labels compartments "ZONE 07" while the HUD reads "ZONE 03" would confuse players.

**Proposal for narrative, 平面视觉 and the map chat.**
- One compartment number per zone, used everywhere: the key tag, the door plate, the plan and the log.
- Rename or clarify the HUD count.
- Decide whether the terminal's title block says LEVEL 0 (the base building) and treats the Office suite as TENANT.

- [Assets/Scripts/FrontRooms3DGame.cs:2288 (HUD ZONE = visited count)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/FrontRooms3DGame.cs#L2288) (project-code): roomMetaText uses zonesVisited.Count as 'ZONE nn'
- [RELAY_PURSUIT_REDESIGN.md:661](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/RELAY_PURSUIT_REDESIGN.md#L661) (project-file): HUD becomes ZONE nn / height; TIER moves to assist
- [research/inspect/01_inventory.md:98-112 (door plate and key tag 00–99)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/inspect/01_inventory.md#L98) (project-file): Plate matches the key's 2-digit number and colour; HUD 'KEY 14'
- [Assets/Levels/FrontRoomsLevel0.asset:50 (doorsNeedKeys 0)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Levels/FrontRoomsLevel0.asset#L50) (project-data): Keys open nothing today
- [research/wallpaper_motion/30_narrative_phosphor.md:406-410 (LEVEL 0 stamps)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/wallpaper_motion/30_narrative_phosphor.md#L406) (project-file): Drawing set is LEVEL 0 SHEET A-n

### Mail and memos are the human layer: they can teach the verbs and carry the 'other occupants', but must not become a journal

**Period fit.** LAN email on DOS desktops is period-correct.
- cc:Mail was "originally developed for MS-DOS … in the 1980s" around a file-server "post office" (Wikipedia, fetched).
- A January 1990 Datapro report sized the microcomputer email market. This was seen in search only, so it is UNVERIFIED.

**Content that fits.**
- **Facilities memos in EGRESS's deadpan life-safety English (A.8).** They teach the same rules the placard footer and CalmHint teach: walk; close doors behind you; breaking glass sets off the alarm; an "emergency power test may dip your lights" (the stage-1 reading). Each must match the cue sheet's one meaning per cue.
- **Messages between the scratch-through initials** `R.M. 6/90` and `D.K. 11/90` (A.11 M9). They are already canon and inside the era.
- **From T4, a sender who is "OCCUPANT LOAD 2".** This matches the forger. If it lures, it lures only toward a pocket, with a tell.

**Limits.**
- The research's P9 is "No journal for prints". Alien: Isolation players "access all the files, then read them afterwards", which drains the reason to stand at the object. So no mail is copied to a notes screen.
- UI_SYSTEM.md's "Tab opens notes" line is already stale.
- No Async or wiki lore, per the IP rule.

- [Wikipedia, cc:Mail (fetched)](https://en.wikipedia.org/wiki/Cc:Mail) (encyclopedia): DOS LAN email developed in the 1980s; shared-file post office on the LAN server
- [Datapro Reports on PC and LAN Communications (search result only, not opened)](https://kuiper.mirrorservice.org/sites/www.bitsavers.org/pdf/datapro/Datapro_Reports_on_PC_and_LAN_Communications_1992/Vol1_535.pdf) (period trade report (search snippet)): UNVERIFIED: Jan 1990 micro e-mail market ≈ $25M (1989)
- [research/wallpaper_motion/30_narrative_phosphor.md:273 and :281-292 (M9, run arc, writing rules)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/wallpaper_motion/30_narrative_phosphor.md#L273) (project-file): R.M. 6/90, D.K. 11/90, OCCUPANT LOAD 2 at T4, deadpan US life-safety English
- [RELAY_PURSUIT_REDESIGN.md:593 (teaching walking)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/RELAY_PURSUIT_REDESIGN.md#L593) (project-file): Footer, CalmHint, pause card wording the memos must agree with
- [research/inspect/02_game_research.md:200-202 and :418-420 (Alien deferral, P9)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/inspect/02_game_research.md#L418) (project-file): No journal; live terminals make players defer reading
- [Steam Community, Alien: Isolation 'Is the game paused when you access terminals?' (2015-02-28)](https://steamcommunity.com/app/214490/discussions/0/617329150702516350) (forum (cited in project as S22)): Terminals on active game time; players read files later
- [research/wallpaper_motion/30_narrative_phosphor.md:88 (IP)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/wallpaper_motion/30_narrative_phosphor.md#L88) (project-file): Concepts only, no wiki text, no Async

### Login, idle and corrupted screens are best used as ambient states and tier tells, not puzzles

**Strings and art that already exist.**
- Approved strings: `C:\>_`, `SYSTEM IDLE` / `OCCUPANT LOAD 1` (2 from T4), and the print queue `PLAN SHEET A-3 OF 4` → `PRINTING SHEET A-1114 OF 4` from T2.
- An unused CRT emission texture already exists: `C:\>DIR /W … Directory of C:\LEVEL4 … FLOOR.DAT EXIT.BAT … C:\>EXIT.BAT / Bad command or file name`.
  - Its "every exit leads in" joke fits.
  - "Bad command or file name" is MS-DOS's own error text. That is my claim, unsourced here, and narrative should rule on it as near-brand wording.

**Tier tells.** TIER leaves the always-on HUD, and RELAY v2 wants silent in-world tells. A print queue or idle line is one.
- It should read the zone's **stored first-generation tier**, not the live run tier, so a revisit never changes it.

**Corruption.** Treat it as the Exit 8-style anomaly (A-1114), quantized and hard-cut (平面视觉's rule).
- It must meet the photosafety limits: ≤ 3 flashes/s, and Reduce Flashing / Reduce Motion means static.

**Screen savers.**
- Search summaries say CRT burn-in was common before about 1990 and that DOS, Windows 3.0 and the Mac had no built-in screen saver then (After Dark shipped on the Mac in 1989). These are UNVERIFIED; the pages were not fetched.
- So a building-network blanker would be a custom program. A switched-off screen could show a burn-in ghost of the shell's header; the tube spec already plans a ghost for the station bug.

**Login.** A password puzzle is not recommended.
- It needs notes placed elsewhere in a procedural map.
- The paper stacks and binders are blank.
- Close-ups are kept rare.
- Locked terminals can show a login prompt as ambience, while the one information terminal is "still logged in".

- [research/screens/30_screen_strings.md:85-93](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/screens/30_screen_strings.md#L85) (project-file): Approved DOS, print queue, idle, alarm log strings; tier-dependent if the generator can read tier
- [research/inspect/01_inventory.md:147 (unused DOS texture)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/inspect/01_inventory.md#L147) (project-file): Existing EXIT.BAT / Bad command texture
- [RELAY_PURSUIT_REDESIGN.md:658-662 (stored tier; silent in-world tells)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/RELAY_PURSUIT_REDESIGN.md#L658) (project-file): Per-zone first-generation tier; TIER off HUD; silent tells
- [RELAY_PURSUIT_REDESIGN.md:511-519 (photosafety)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/RELAY_PURSUIT_REDESIGN.md#L511) (project-file): Flash limits and Reduce Flashing behaviour
- [SCREENS_VISUAL_SYSTEM.md:45-51 (tube: burn-in ghost, power collapse, V-hold on sag)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/SCREENS_VISUAL_SYSTEM.md#L45) (project-file): Tube layer already specified
- [Wikipedia, Screensaver (search summary only)](https://en.wikipedia.org/wiki/Screensaver) (encyclopedia (search snippet)): UNVERIFIED: pre-1990 burn-in; no built-in DOS/Win 3.0/Mac saver c.1990; After Dark 1989
- [research/inspect/01_inventory.md:215 and :251 (blank pages)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/inspect/01_inventory.md#L215) (project-file): No notices or memos exist; paper stacks and binders blank

### Screens must join the warning language exactly as the TV plan does, and the close-up rules already settle what happens near the Relay

**One event table for all screens** (SCREENS_VISUAL_SYSTEM §4, which still needs Red's go):

| Event | Screens |
|---|---|
| Stage 1 | Interference only (hum bars, roll), no text, and only on sets in the player's room set |
| Chase wave | Cut to the emergency slate `ALARM IN PROGRESS` / `CLOSE DOORS` / `BEHIND YOU` |
| Wave retracts | Snow, then the screen comes back |
| Withdraw, Restore | No change |

- Brightness follows room power (Sag/Dip events), which matches stage 1's EGRESS reading: your compartment switched to emergency power.
- A terminal is the more natural carrier of the building's own slate than the TV.

**Close-up rules already designed (EXAMINE).**
- No prompt while the Relay chases, breaks a door, or is in view. The v2 rule is stage ≥ 3, spotting ≥ 0.5, `PlayerSeesRelay` or BreakDoor.
- Never gated on the silent `SeesPlayer`, which would make the prompt a detector.
- Stages 1–2 are allowed and frozen.
- Pause neutrality is the key test.

**Noise.** Reading or looking adds 0 Attention. A computer should never count as a Disturb or a trigger device. LD lists a phone as a possible device marker, so keep phones and terminals separate.

**Sound.** Events only, no new AudioSources (AUDIO_CONTRACT).

- [SCREENS_VISUAL_SYSTEM.md:53-65 (screens in the warning language)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/SCREENS_VISUAL_SYSTEM.md#L53) (project-file): Event→screen mapping; proposal pending Red
- [research/screens/30_screen_strings.md:56-79 (emergency slate)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/screens/30_screen_strings.md#L56) (project-file): Slate strings and rules (never RUN/HIDE, no dark-cover, no arrows on TV)
- [research/inspect/10_design.md:153-158 (rows C and W)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/inspect/10_design.md#L157) (project-file): Relay gating on perceivable cues only; warnings allowed and frozen
- [research/inspect/10_design.md:517-530 (pause cannot cheat)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/inspect/10_design.md#L517) (project-file): Pause neutrality invariant
- [RELAY_PURSUIT_REDESIGN.md:323 (Attention gains)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/RELAY_PURSUIT_REDESIGN.md#L323) (project-file): Looking adds 0
- [research/relay_pursuit/20_level_design.md:42 (device marker can be a phone)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/relay_pursuit/20_level_design.md#L42) (project-file): Phones are candidate alarm devices
- [Documentation/AUDIO_CONTRACT.md:20](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/AUDIO_CONTRACT.md#L20) (project-file): No new AudioSources; raise events for the sound chat

### 'Directly see the screen' needs two reading depths, because a full text page on this monitor is legible only up close

**The numbers.** The screen face is a 0.28 × 0.19 m quad. The inspect design's legibility line is 18.6 arcmin (12 px on the 1080p reference).

**My estimate (ESTIMATE).**
- An 80 × 25 text page gives about 7.6 mm rows and about 4.9 mm caps at a 0.65 cap/row ratio.
- That is 21′ at 0.8 m but only 17′ at 1.0 m, so on desktop it reads only inside about 0.9 m.
- Inspect's phone factor (×0.45) puts phones at roughly 0.4 m, which is impractical.
- A 12-row large-type layout gives about 10 mm caps, which reach 18.6′ at about 1.9 m on desktop and about 0.86 m on a phone.

**Proposal.**
- **Glance layer:** a ≤ 12-row headline layout, readable in place while walking up. This is Red's "directly see".
- **Detail pages:** the plan, log and mail go through the EXAMINE `panel` kind, with a transcript option for accessibility.

**Blockers already logged.**
- The CRT reads `Keyboard.current.eKey` with its own raycast. That bypasses the game's input, the HUD prompt and the touch layer. Contract C26 is blocking.
- `Shader.Find` fails in player builds (F6).
- The anchor is hard-coded to `Kit_CRTMonitor`.

**Platforms.**
- Touch should tap the screen itself, as doors are tapped, with UV hit regions per SCREEN_ADS.
- WebGL is its own tier with no VideoPlayer, so pages should be pre-rendered static textures, gated by platform.

- [SCREENS_VISUAL_SYSTEM.md:12-15 (0.28×0.19 m quad, 18 m decode, 2.4 m E)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/SCREENS_VISUAL_SYSTEM.md#L12) (project-file): Screen face size and current interaction
- [research/inspect/10_design.md:30-31 and :77-83 (18.6′ rule, phone ×0.45)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/inspect/10_design.md#L30) (project-file): Angular legibility threshold and phone distance factor
- [research/inspect/10_design.md:915-923 (CRT owner, C26)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/inspect/10_design.md#L915) (project-file): Private E path problem; C26 blocking; later the screen becomes a panel target
- [Assets/Scripts/Media/FrontRoomsScreenVideo.cs:18-19, :43, :214-216](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/Media/FrontRoomsScreenVideo.cs#L214) (project-code): 18 m / 2.4 m distances, TURN ON/OFF prompt, Shader.Find at runtime
- [SCREEN_ADS.md:50-56 (UV hit regions; poster for far screens)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/SCREEN_ADS.md#L50) (project-file): Screen-only collider + textureCoord overlay; live decode only for near screens
- [TOUCH_CONTROLS.md:39 (tap the object)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/TOUCH_CONTROLS.md#L39) (project-file): Tapping an object takes the same road as E
- [SCREENS_VISUAL_SYSTEM.md:72-73 (WebGL flipbook only)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/SCREENS_VISUAL_SYSTEM.md#L72) (project-file): WebGL is a separate tier without VideoPlayer

### Terminal placement must be deterministic and survive revisit shifts, or players lose 'that computer'

**The problem.** Office dressing is part of the interior that a revisit shift rebuilds. A terminal tied to one desk instance would move or vanish when the player returns.

**What existing rules do.**
- Keys are placed "in its cell nearest the site".
- Sanctuaries and trigger rooms are placed by coordinate; "a shift may re-dress furniture, never add, remove or move it".
- LD R16 asks for hashed, `UnityEngine.Random`-free choices.

**Proposal.**
- Key the terminal's identity and content to the zone: hash(seed, zone id, salt).
- Host it on the station nearest the zone site, re-chosen each revision.
- Its content (compartment number, plan sheet, mail thread, log) is then the same on every visit.
- The plan updates only where the truth changed: cleared stamps, and the route target when a player cell or passage changes.

- [MAP_GENERATION.md:110 and :117](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/MAP_GENERATION.md#L110) (project-file): Key at the cell nearest the site; revisits rebuild interiors
- [RELAY_PURSUIT_REDESIGN.md:596](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/RELAY_PURSUIT_REDESIGN.md#L596) (project-file): Placed by coordinate and stable through revisit shifts
- [research/wallpaper_motion/20_level_design_phosphor.md:201-209 (R16 determinism)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/wallpaper_motion/20_level_design_phosphor.md#L201) (project-file): Hashed choices from seed and cell; no UnityEngine.Random

### Ownership and visual-system fit

**Who owns what.**
- **Narrative:** every string (the plan legend words, compartment labels, memos, log phrasing, cleared stamp).
- **平面视觉:** the UI system: grid, faces, sizes, inverse bars, glyphs.
- **游戏视觉:** the tube shader, the F6 fix and per-kit anchors.
- **系统设计 / 关卡设计:** data hooks (zones, visited, `FrontRoomsWayfinding`, `Called`/`Restore`, stored tier) and EXAMINE contracts.
- **声音设计:** sound events.

**Type.**
- The HUD faces (Bayon, IBM Plex Mono, Source Serif 4) are a separate system.
- On-screen type ships from Period1990. Its kit already lists `VT323` / `CourierPrime` for CG, typewriter and teletext. VT323 is a plausible terminal stand-in, but 平面视觉 decides.

**Colour, a risk to flag.** The glow ink is pale yellow-green (#B6F5A0–#C9F7B0) and means "building guidance in the dark". A green-phosphor terminal could blur that meaning. Keeping one colour = one meaning matches RELAY's "each cue has one meaning". This is 平面视觉's call.

**Motion vocabulary.** The cue's moves are reserved: ambient keyframes never use them. A terminal animation must not imitate the wallpaper's turns or straight-match slips.

- [SCREENS_VISUAL_SYSTEM.md:76-84 (owners)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/SCREENS_VISUAL_SYSTEM.md#L76) (project-file): Owner split for screens
- [SCREENS_VISUAL_SYSTEM.md:26 (P2 type outside system)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/SCREENS_VISUAL_SYSTEM.md#L26) (project-file): Screen faces go through 平面视觉 and ship from Period1990
- [FONTS_PERIOD_1990.md:7 and :46](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/FONTS_PERIOD_1990.md#L46) (project-file): UI fonts separate; VT323 / Courier Prime for CG, typewriter and teletext
- [research/wallpaper_motion/30_narrative_phosphor.md:336](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/wallpaper_motion/30_narrative_phosphor.md#L336) (project-file): Ink colour pale yellow-green
- [RELAY_PURSUIT_REDESIGN.md:65](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/RELAY_PURSUIT_REDESIGN.md#L65) (project-file): Each cue has one meaning
- [research/inspect/10_design.md:532-534 and :903-913 (UI owners)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/inspect/10_design.md#L903) (project-file): 平面视觉 designs type/graphics; map chat builds HUD; touch session builds touch UI

**Recommendations from this sweep:**

- Designate one information terminal per Office zone. Key it to the zone id and host it on the station nearest the zone site, so it survives revisit shifts. Every other CRT stays ambient (off, idle blanker, `C:\>_`, a locked login prompt, or the station). Give the terminal a tell that reads from about 3 m, such as a distinct inverse header bar; being switched on is not enough.
- Build the terminal UI as one fictional building-services shell with at most four pages:
1. FLOOR PLAN, the main page;
2. ALARM HISTORY;
3. MAIL;
4. PRINT QUEUE / IDLE.
No password puzzle. The information terminal is 'still logged in'.
- FLOOR PLAN content. Show:
- the compartments (zones) with their height class and border doors/windows, which the generator keeps stable;
- YOU ARE HERE at the terminal;
- cleared stamps from `zonesVisited`;
- the Office suite left blank as TENANT.
Add a WAY ON route only if Red approves. It must come from the shared `FrontRoomsWayfinding` target, never glass, built cells only. Its other condition, that 'what is an exit' be decided, has been met since 2026-10-22 (C22, C24 = A + A20). Never mark the Relay, Relay entries, trigger rooms or sweep targets. Use only the placard's legend glyphs.
- ALARM HISTORY content. List only closed encounters the player perceived, by cause, in EGRESS steps (device or noise, verified, reset). No live or pending entries, no sweep, no Attention or Pressure, no visible RESET at the moment it leaves, and no live clock, so RELAX cannot be counted down. Frame the log as invented: a 1990 panel event log is unverified.
- Ask Red to choose how dead circuits (sanctuaries) appear on terminals: never, own and adjacent compartments only, or all. Tie the choice to Q7c. Also rule that an information terminal never stands inside a trigger room and is never the only source of anything essential.
- MAIL content (narrative). Write deadpan facilities memos that teach WALK, CLOSE DOORS BEHIND YOU and that glass sets off the alarm, plus the stage-1 'emergency power test' reading, word for word consistent with the placard footer and cue sheet. Add a thread between R.M. and D.K. (A.11 initials). From T4 add an OCCUPANT LOAD 2 sender whose lure has a tell. Never copy mail into a notes screen (P9).
- Use the print queue (A-3 → A-1114) and OCCUPANT LOAD as silent tier tells, read from the zone's stored first-generation tier. Corruption is quantized, hard-cut and photosafe, and static under Reduce Motion.
- Join the screens' warning table:
- stage 1: interference only, room set only;
- chase wave: the building's emergency slate;
- wave retract: snow, then back;
- Withdraw and Restore: no change.
Computer use adds 0 Attention and is never a trigger device. Sound goes through events only.
- Two reading depths. A glance layer of 12 rows or fewer, readable in place while walking up, answers Red's 'directly see'. The plan, log and mail go through the EXAMINE `panel` close-up, with its existing Relay gating, frozen warnings and transcript option. Land C26 first (the CRT's private E path) and the F6 `Shader.Find` fix.
- Resolve numbering before any plan art: one compartment number per zone shared by the key tag, door plate, plan and log. Rename or clarify the HUD's 'ZONE nn' visited count. Decide LEVEL 0 (drawing set) against the HUD's 'LEVEL 4 / OFFICE' on the terminal title block.
- Route type and colour through 平面视觉: Period1990 faces (VT323 or Courier Prime as candidates), not HUD fonts. Avoid the glow ink's pale yellow-green so one colour keeps one meaning. No terminal animation may use the wallpaper cue's reserved moves.
- Platforms. Touch taps the screen directly, using UV hit regions. WebGL uses pre-rendered static page textures with no VideoPlayer, gated by platform. Desktop stays the reference.
- Before any string ships, narrative should rule on the unused texture's 'Bad command or file name' (MS-DOS's own error text). Mark the screen-saver and LAN-mail period claims unverified until the pages are fetched (only cc:Mail's Wikipedia page was read).

## Engine feasibility in this codebase

### 1. A CRT screen layer already exists. It is a video billboard with a private power toggle, not an interactive UI.

FrontRoomsScreenVideo is attached to every Kit_CRTMonitor that the office kit spawns. That is about 88% of stations, because desk gear is skipped when rng.Chance(.12f).

What it builds:
- A runtime quad of 0.28 x 0.19 m at a hard-coded anchor (0, .248, .194), 4 mm in front of the glass.
- A per-screen Material instance and a trigger BoxCollider.
- A shared VideoPlayer bank that writes a 640x434 ARGB32 sRGB render texture with point filtering and no mips.

It also runs its own static focus ray from Camera.main (QueryTriggerInteraction.Collide, 2.4 m). It reads Keyboard.current.eKey directly to toggle power.

IsFocused, IsPoweredOn and InteractionPrompt are public, but nothing consumes them. A grep finds references only in FrontRoomsOfficeKit.cs:848-851. The 'E TURN ON/OFF' string is never shown.

The header comment (:7-13) and SCREEN_ADS.md:15-18 and :48-56 explicitly plan for a separate interactive overlay on the same collider, mapped through RaycastHit.textureCoord.

The working tree has an uncommitted change to this file. It loads the new untracked Resources/Rendering/FrontRoomsScreenVideo.shader instead of using Shader.Find (the F6 fix). That shader is target 3.5, has a UniversalForward pass only, and samples _BaseMap times _BaseColor.

- [FrontRoomsScreenVideo.cs:7-13, 27-28, 41-44, 124-143, 163-182, 288-303](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/Media/FrontRoomsScreenVideo.cs) (code): quad size and anchor, own ray and raw E key, unused public hooks, 640x434 point-filtered RT
- [FrontRoomsOfficeKit.cs:848-851, 863, 875](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/Office/FrontRoomsOfficeKit.cs) (code): AttachScreen runs on every spawned monitor; 12% of stations get no desk gear
- [FrontRoomsScreenVideo.shader:1-71 (untracked)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Resources/Rendering/FrontRoomsScreenVideo.shader) (code): target 3.5 unlit screen shader loaded from Resources
- [SCREEN_ADS.md:15-18, 48-56](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/SCREEN_ADS.md) (project doc): intended split between the media layer and a separate interaction overlay

### 2. The game's interaction path cannot see a monitor. Prompt, touch USE, tap-to-use and the input snapshot all lack a 'computer' case.

Desktop: UpdateAim raycasts with QueryTriggerInteraction.Ignore and asks map.Describe. Describe knows doors and windows only (doorByCollider and windowByCollider), so a monitor gets no prompt. The monitor's body BoxColliders, added by KitLibrary.AddColliders, also absorb the aim ray.

Touch:
- The USE button is driven only by that prompt (UpdateMobileTouchPrompt).
- Tap-the-object routes only to doors (HandleMobileLookTap returns when Describe is empty).
- UseKind has no READ or BACK verb.

Input: FrontRoomsInput.FrameSnapshot has Move, Look, Use, Pause, Start, Settings, Restart and ShotBack. There is no Navigate, Submit, Cancel, Point or Zoom. S is bound both to Move-down and to ShotBack.

ScreenVideo's raw E key also bypasses rig.Consume and has no phase or pause gate. The EXAMINE research already flags this as blocking contract C26: its owner is unclear, and it toggles monitors during close-ups and pause.

- [FrontRooms3DGame.cs:1351-1394 (UpdateAim), 1243-1249 (EDown)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/FrontRooms3DGame.cs) (code): aim ray ignores triggers; prompts come only from map.Describe
- [FrontRoomsMapWorld.cs:2366-2383 (Describe), 2393 (Use), 413/431](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs) (code): interactables limited to doors and windows
- [FrontRooms3DGame.Mobile.cs:241-251, 284-306](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/FrontRooms3DGame.Mobile.cs) (code): tap-to-use and USE verb are door-only
- [FrontRoomsTouchControls.cs:55, 116, 131, 839-843](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/Input/FrontRoomsTouchControls.cs) (code): UseKind enum; LookTapped gives a viewport point on a quick tap (<=0.22 s)
- [FrontRoomsInput.cs:15-50, 230-277](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/Input/FrontRoomsInput.cs) (code): snapshot fields; S bound to Move-down and to ShotBack
- [research/inspect/10_design.md:870-923 (C26, section 7.6); 01_inventory.md:34, 238](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/inspect/10_design.md) (project doc): CRT private E path is a known blocker; owner unclear

### 3. The camera transition should reuse the shot rig and the EXAMINE close-up system. EXAMINE already lists the Office CRT as a future `panel (screen)` target.

FrontRoomsCameraRig.BeginShot(ShotSpec) already provides:
- A world target pose, Func<Pose>, followed every frame.
- blendIn and blendOut.
- fovDelta, clamped to ±15° (MaxFovDeltaDeg).
- lockMove, lockLook and lookCone.
- Cancel flags PlayerInput and Relay, with CancelForRelay.
- HudFade, which the game applies to the HUD.
- A 0.1 m spherecast that keeps the camera out of walls.

Gameplay reads only BaseEye. The Relay therefore still sees the body, not the camera. At Camera motion Off a shot plays in place.

游戏视觉's EXAMINE prototype (clone-only, 2026-10-08) adds ShotKind.Inspect, Phase.Inspect with timeScale 0, a framing solver, an angular zoom and Esc-to-exit. It passed 93/93 EditMode and 89/89 PlayMode tests. Its row L1 is the Office CRT monitor as a `panel (screen)` target. It is deferred because the CRT's E is the power toggle, and it waits on C26 and on removing the real LEVITZ trademark.

Not in main yet: merging needs the map chat's rig contract C16–C19 first (contracts/map_inspect.diff, 37 hunks). Building a second, terminal-only camera system would duplicate this work.

- [FrontRoomsCameraRig.cs:6, 26-49, 78-81, 177-185, 221-226, 277-296, 444-457](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/FrontRoomsShots/FrontRoomsCameraRig.cs) (code): ShotSpec fields, Relay cancel, Consume, world clamp, Camera motion Off behaviour
- [FrontRoomsShotTimings.cs:13-16](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/FrontRoomsShots/FrontRoomsShotTimings.cs) (code): MaxFovDeltaDeg 15, WorldClampRadius 0.1, HudFadeSeconds
- [MAP_GENERATION.md:43](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/MAP_GENERATION.md) (project doc): BaseEye-only gameplay; shots are picture only; a chasing or seeing Relay frees the player
- [research/inspect/20_prototype.md:1-18, 166-178](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/inspect/20_prototype.md) (project doc): EXAMINE prototype tests and merge order
- [research/inspect/10_design.md:56-66 (row L1), 341-350, 453-480](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/inspect/10_design.md) (project doc): CRT planned as a panel(screen) target; controls inside the close-up; pause semantics

### 4. Text you can read at a glance from the desk and text that needs a close-up are two layers with very different capacity. These are derived numbers.

All numbers below are my own calculation from code geometry. They are not measured in game.

Inputs:
- The camera's vertical FOV is 76° (Game.cs:280). Unity's fieldOfView is vertical.
- The nearest standing spot is the desk front plus the 0.3 m capsule radius.
- At that spot the screen glass is 0.77–0.86 m away and seen from 41–47° above, so the face is foreshortened to 0.68–0.76 of its height.
- Readability rule: EXAMINE's angular rule, a cap height of at least 18.6′ (12 px on the 1080p, 24-inch, 60 cm reference).
- Phone: 1 pt = 0.166 mm at 30 cm.
- I assumed cap height ≈ 0.6 × an 8x16 cell (UNVERIFIED).

Glance layer (standing): the face covers about 224x103 to 250x129 px at 1080p. That holds about 4–6 rows of large type on desktop and only 2–3 rows on a phone.

Engaged close-up (camera on the screen normal):

| Camera | Face at 1080p | 25-row cell | Cap angle (desktop) |
|---|---|---|---|
| 0.30 m, FOV 61° (76 − 15) | 855x580 px | 23 px | ≈22′ |
| 0.35 m, FOV 61° | 733x497 px | 20 px | ≈19′ |
| 0.30 m, FOV 76° | 645x437 px | 17.5 px | ≈17′ |

Phone: the same 25 rows reach only about 10′ at the 0.30 m framing (face ≈54% of view height). They reach about 17′ only when the face fills about 90% of the view height. Handheld therefore needs 16 rows or fewer, or a full-height framing. EXAMINE already dollies to 0.18 m and narrows FOV on phones.

The HFES minimum and preferred figures are cited second-hand. The project doc marks them UNVERIFIED.

- [FrontRooms3DGame.cs:280 (FOV 76, near clip 0.06)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/FrontRooms3DGame.cs) (code): camera FOV and near plane
- [FrontRoomsOfficeKit.cs:854-877; FrontRoomsModuleUnits.cs:92; Kit_CRTMonitor.json (screen anchor 0,0.248,0.19)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/Office/FrontRoomsOfficeKit.cs) (code): station geometry, player radius 0.3, eye 1.62
- [Unity Scripting API: Camera.fieldOfView](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Camera-fieldOfView.html) (unity-docs): fieldOfView is the vertical FOV
- [research/inspect/10_design.md:249-270 (section 3.4 angular rule)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/inspect/10_design.md) (project doc): 18.6′ cap rule; phone ×0.45 angular class; HFES cite marked UNVERIFIED
- [TOUCH_CONTROLS.md:89](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/TOUCH_CONTROLS.md) (project doc): 1 pt ≈ 0.166 mm on iPhone 16 Pro at about 30 cm
- [Activu, Optimal Font Size (16′ minimum, 20′ optimal), seen only via search summary](https://activu.com/uploads/Optimal-Font-Size.pdf) (external (UNVERIFIED, not opened)): character-height legibility thresholds

### 5. Render path: recommend UI Toolkit drawing into a PanelSettings.targetTexture with no extra camera. This is already proven in this codebase and in 平面视觉's clone.

Unity documents PanelSettings.targetTexture for UI on 3D geometry. It renders without a camera.

Colour space:
- The project is Linear (m_ActiveColorSpace: 1), so Unity's documented format is R8G8B8A8_SRGB.
- 平面视觉's promo harness made a different, deliberate choice: a UNorm target with PanelSettings.forceGammaRendering, decoded in the tube shader by _SignalDecode. This matches Figma and broadcast alpha. Linear light would lift a 25% ink to about 50%.
- forceGammaRendering is a public property in Unity 6.3.

Precedents:
- FrontRooms3DGame already builds a runtime PanelSettings and UIDocument for the vector logo (ScriptableObject.CreateInstance).
- The promo harness rendered UI Toolkit into a 1280x960 RT with mips onto a CRT shader.

Alternatives:
- uGUI in Screen Space - Camera with camera.targetTexture needs an extra URP base camera, with its own cull and render loop. Render-texture cameras run before screen cameras. Unity's own camera-performance series measures extra cameras as avoidable cost. WebGL here is per-draw bound at about 3,200 draws per frame. Not recommended.
- A uGUI World Space canvas, or Unity 6.3's PanelRenderMode.WorldSpace, would avoid the RT. However, the picture would then sit outside the CRT material: no phosphor, curvature or glass pass. Not recommended for the hero screen.

Gap: the project has no .tss, .uss, .uxml or UI Toolkit font asset. The current vector logo uses images, not text. Text on a runtime PanelSettings probably needs a themeStyleSheet or textSettings, or an explicit FontDefinition (UNVERIFIED; test first).

- [Unity Scripting API: PanelSettings.targetTexture](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/UIElements.PanelSettings-targetTexture.html) (unity-docs): UI on 3D geometry via RT; SRGB format for linear projects
- [Unity Scripting API: PanelSettings (6.3)](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/UIElements.PanelSettings.html) (unity-docs): forceGammaRendering, themeStyleSheet, textSettings, renderMode are public
- [Unity Scripting API: PanelRenderMode](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/UIElements.PanelRenderMode.html) (unity-docs): WorldSpace render mode exists
- [FrontRooms3DGame.cs:1643-1696 (BuildVectorLogo)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/FrontRooms3DGame.cs) (code): runtime PanelSettings/UIDocument precedent
- [~/FrontRoomsVisualWork/proj_pgcue2/Assets/Editor/CuePreview/FrontRoomsPromoCapture.cs:228-312](file:///Users/redwang/FrontRoomsVisualWork/proj_pgcue2/Assets/Editor/CuePreview/FrontRoomsPromoCapture.cs) (code (clone)): UI Toolkit to 1280x960 mipped RT, forceGammaRendering, signal-space composite
- [ProjectSettings.asset:50, 969](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/ProjectSettings/ProjectSettings.asset) (config): linear colour space; active input handling = Both
- [URP: render a camera to a Render Texture](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/rendering-to-a-render-texture.html) (unity-docs): RT cameras render before screen cameras
- [Unity blog: Optimize game performance with Camera usage, Part 2](https://unity.com/blog/games/part-2-optimize-game-performance-with-camera-usage) (vendor blog): extra cameras have measurable, avoidable cost in URP
- [uGUI 2.0 manual: Canvas render modes](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/UICanvas.html) (unity-docs): Screen Space-Camera and World Space canvas behaviour
- [research/webgl/00_measured_baseline_2026-10-02.md:32, 92-93](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/webgl/00_measured_baseline_2026-10-02.md) (project measurement): WebGL is per-draw bound, about 3,200 draws per frame; MPB UBO uploads

### 6. Alternative renderer for the WebGL/handheld tier and the glance layer: a text-mode character-grid shader. It needs no extra pass and is period-exact.

VGA text mode (IBM, 1987) stores two bytes per cell: a character code and an attribute. The default is 80x25 on a 720x400 raster with 9x16 cells. That is inside the 1990 era lock.

How it would work here:
- The terminal state writes an 80x25 (or 40x25) char/attribute texture, about 2,000 texels, with SetPixelData and Apply only when the content changes.
- The tube shader decodes glyphs from a font atlas in the same pass that draws the CRT.

Cost: no panel render, no camera, no mip generation per change, and at most one tiny upload.

Limits: monospace bitmap glyphs only, so no proportional Period1990 faces or vector logos.

Font: none of the Period1990 stand-ins is a terminal face. The Ultimate Oldschool PC Font Pack (IBM VGA 8x16 and 9x16) is CC BY-SA 4.0. Whether a game embedding it triggers ShareAlike needs 平面视觉's and Red's licence review (UNVERIFIED).

- [Wikipedia: VGA text mode](https://en.wikipedia.org/wiki/VGA_text_mode) (external reference): 1987 introduction, 2 bytes per cell, 80x25, 720x400 with 9x16 cells
- [int10h.org Oldschool PC Font Pack readme](https://int10h.org/oldschool-pc-fonts/readme/) (external (licence)): IBM VGA 8x16/9x16 included; CC BY-SA 4.0; attribution 'VileR'
- [FONTS_PERIOD_1990.md:7, 19](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/FONTS_PERIOD_1990.md) (project doc): 平面视觉 owns faces; original period faces cannot ship

### 7. Use one live RT, bound only to the terminal in use. Idle screens get static frames. The resolution follows the 4:3 broadcast raster.

Only one terminal can be in use at a time, so one live RT is enough. Swap the focused screen's material texture to it on engage and back on exit. All other monitors show off, the ad bank, or a static idle frame (a shared Texture2D or Texture2DArray slice per state).

Each screen already has its own Material instance (ScreenVideo:174, :222). A per-material slice index therefore stays SRP Batcher-compatible. Avoid MaterialPropertyBlock: Unity's roadmap states MPBs currently break the SRP Batcher, and the WebGL baseline already counts that UBO cost.

Raster: lay content on SCREENS_VISUAL_SYSTEM's 4:3 raster with 80% title-safe and 90% action-safe areas, cropped by the tube's overscan.

RT size:
- Desktop: 1280x960. This gives about 1.5× headroom over the 855x580 px face at the close-up for curvature and mask resampling, and matches the promo precedent. About 4.7 MiB, about 6.3 MiB with mips.
- Handheld and WebGL: 640x480. About 1.2 MiB, about 1.6 MiB with mips.

Filtering: bilinear plus mips. The current ad RT is point-filtered with no mips, and the audit predicts shimmer beyond about 2 m.

UNVERIFIED: whether a targetTexture panel repaints every frame or only when dirty. Disable the idle UIDocument and measure UIR markers in the Profiler.

- [SCREENS_VISUAL_SYSTEM.md:44, 69-74](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/SCREENS_VISUAL_SYSTEM.md) (project doc): 4:3 raster and safe areas; desktop live-decode budget; WebGL flipbook-only tier
- [Unity roadmap: Material Property Block SRP Batcher Compatibility](https://activation.unity3d.com/roadmap/75-material-property-block-srp-batcher-compatibility) (vendor roadmap): MPBs currently break the SRP Batcher
- [research/codex_audit/10_review_glass-look.md:93-121 (F7 and notes)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/codex_audit/10_review_glass-look.md) (project audit): point filter/no mips shimmer risk; flat quad covers the curved bezel corner
- [FrontRoomsScreenVideo.cs:174, 208-231](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/Media/FrontRoomsScreenVideo.cs) (code): per-screen material instance

### 8. The CRT effect belongs inside the screen material, as one pass. The existing tube prototype is target 4.5 and will not run on WebGL2 or GLES 3.0.

The clone-only FrontRooms/PromoScreen shader already implements the look SCREENS_VISUAL_SYSTEM §3 specifies:
- slot mask and beam-shaped scanlines that fade once finer than the output pixel (so no moiré);
- convergence error, barrel curvature and a rounded tube face;
- hum bars and roll;
- power-on/off collapse;
- glass Fresnel reflection with grime.

It samples the frame texture directly, with no extra blit.

But it declares #pragma target 4.5 and uses reflection-probe-blending, additional-light and Forward+ keywords. Target 4.5 maps to OpenGL ES 3.1. WebGL2 roughly matches OpenGL ES 3.0, which is target 3.5. The production tube therefore needs a target-3.5 SubShader or fallback for WebGL and older Android. The new FrontRoomsScreenVideo.shader (target 3.5, loaded from Resources so it is always in the build) can serve as that floor.

Per the memory rules, any WebGL simplification must be a WebGL-only branch.

Owner: the tube shader, the material and the F6 fix are 游戏视觉's.

- [~/FrontRoomsVisualWork/proj_pgcue2/Assets/Promo/FrontRoomsPromoScreen.shader:1-40](file:///Users/redwang/FrontRoomsVisualWork/proj_pgcue2/Assets/Promo/FrontRoomsPromoScreen.shader) (code (clone)): existing CRT pass features, target 4.5, keywords, _SignalDecode
- [Unity Manual: #pragma target](https://docs.unity3d.com/6000.3/Documentation/Manual/SL-Pragma-target.html) (unity-docs): 3.5 is OpenGL ES 3.0; 4.5 is OpenGL ES 3.1
- [Unity Manual: WebGL2](https://docs.unity.cn/Manual/WebGL2.html) (unity-docs): WebGL 2.0 capabilities almost match OpenGL ES 3.0
- [SCREENS_VISUAL_SYSTEM.md:45-51, 76-84](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/SCREENS_VISUAL_SYSTEM.md) (project doc): tube layer spec and owner table
- [WEBGL_BUILD.md:15-18](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/WEBGL_BUILD.md) (project doc): WebGL profile (Wasm, no threads, generic texture subtarget)

### 9. Cursor on the screen: work out the UV from the hit point and pick in panel space yourself. Don't rely on UI Toolkit's pointer pipeline or on textureCoord from the kit FBX.

Option (a), recommended: transform hit.point into the screen quad's local space and get u = x/w + 0.5, v = y/h + 0.5. This works with the existing BoxCollider. It must undo the deliberate U mirror in MakeQuad (:196-200), or the UI quad gets its own UVs.

Option (b): RaycastHit.textureCoord needs a MeshCollider. In builds it returns (0, 0) unless the mesh has Read/Write enabled.
- The runtime quad is created by script, so it is readable.
- Kit_CRTMonitor.fbx.meta has isReadable: 0. Using the curved Prop_ScreenCRT glass would need a 游戏视觉 import change, plus 0–1 UVs on that submesh.

Option (c): Unity's documented PanelSettings.SetScreenToPanelSpaceFunction pattern raycasts and returns textureCoord in texel space. It still depends on UI Toolkit's event system reading pointer input. The game locks the cursor during play (Game.cs:2180), and the project has no EventSystem and no .inputactions asset.

Picking: convert UV to panel pixels and call IPanel.Pick(point), which takes panel space. Then toggle a hover class, and activate the item on USE. No synthetic events are needed.

Per platform:
- Desktop and WebGL: the crosshair is the cursor, so the cursor never unlocks. Re-locking on the web needs a user gesture, and Esc releases it.
- Touch: a tap gives a viewport point (LookTapped). ViewportPointToRay then gives the same UV. Alien: Isolation's iOS port turned terminals into direct touchscreens.

- [Unity Scripting API: RaycastHit.textureCoord](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/RaycastHit-textureCoord.html) (unity-docs): MeshCollider required; (0,0) in builds without Read/Write
- [Unity Scripting API: PanelSettings.SetScreenToPanelSpaceFunction](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/UIElements.PanelSettings.SetScreenToPanelSpaceFunction.html) (unity-docs): documented RT-on-3D input mapping pattern
- [Unity Scripting API: IPanel.Pick](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/UIElements.IPanel.Pick.html) (unity-docs): top-most element at a panel-space point
- [Unity Manual: UI Toolkit runtime event system](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-Runtime-Event-System.html) (unity-docs): default event system vs EventSystem; input comes from the active input backend
- [Unity Manual: Web input (cursor lock)](https://docs.unity3d.com/Manual/webgl-input.html) (unity-docs): cursor lock needs user input on the web; Esc unlocks
- [Kit_CRTMonitor.fbx.meta:53 (isReadable: 0)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Resources/Props/Models/Kit_CRTMonitor.fbx.meta) (config): kit mesh not readable in builds
- [FrontRoomsScreenVideo.cs:184-206 (MakeQuad, U flip)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/Media/FrontRoomsScreenVideo.cs) (code): quad UVs mirrored in U for the video
- [research/touch/01_shipped_games.md:145 (Alien: Isolation iOS: terminals become direct touchscreens, App Store review)](https://itunes.apple.com/us/rss/customerreviews/id=1573029040/sortby=mosthelpful/json) (user review): touch precedent for tapping terminal screens

### 10. Controls inside the terminal: selection-driven, no free text. Exit keys must fit the existing E/S/Esc scheme.

Free text cannot work everywhere. TouchScreenKeyboard supports only native iPhone, Android and Windows Store apps; WebGL is not listed. Passwords and typed commands would break on WebGL, and would collide with gameplay bindings (WASD, E, R, O, S) on desktop.

Recommended scheme:
- Menus and number-key-style choices.
- Navigate with the crosshair or tap hover, plus W/up and S/down, which are free because the shot locks movement.
- Select with USE.
- Exit with Esc or BACK, as EXAMINE does: Esc leaves a close-up instead of opening pause.

Two conflicts to resolve:
- S is bound to both Move-down and ShotBack, which is EXAMINE's exit. Either S-down cannot also scroll, or the terminal scrolls with arrow keys or W only.
- FrameSnapshot needs new fields. EXAMINE's T3 already proposes a Zoom action. A terminal adds Navigate, Submit and Cancel, append-only at the end of the constructor.

Period precedent: 1990 DOS menus were keyboard-driven, so this scheme is also historically fitting.

- [Unity Scripting API: TouchScreenKeyboard](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/TouchScreenKeyboard.html) (unity-docs): supported only on native iPhone, Android and Windows Store apps
- [FrontRoomsInput.cs:230-277](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/Input/FrontRoomsInput.cs) (code): all bindings; S is in both Move and ShotBack
- [research/inspect/10_design.md:19-46 (item 10 Esc), 341-350, section 7.4 T1-T7](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/inspect/10_design.md) (project doc): exit inputs, touch MenuState.Inspect, UseKind.Read/Back, Zoom action

### 11. Pause or live is a design call for Red. Both are feasible. A live terminal must keep the Relay v2 warnings perceivable.

EXAMINE pauses: Phase.Inspect with timeScale 0, per Red's request for close-ups. Shipped terminal precedents run live:
- Alien: Isolation terminals are on active game time.
- Lethal Company locks the camera to the CRT and prints '[ESC]', with the world still running (local capture D3; no URL).

If live, the hooks exist:
- ShotCancel.Relay and CancelForRelay snap the player out.
- The Relay's state is exposed through MapHunter.WarnStage, WarnStageChanged and StateChanged.
- Lamp state is exposed through MapWorld.FixtureChanged and LampDipped.
- SCREENS_VISUAL_SYSTEM §4 proposes that screens join the warning language: interference at stage 1, the emergency slate on the chase wave. This is still a proposal awaiting Red.

The framing must then leave some of the room's lamps in view, or the screen itself must show the flicker. The v2 first warning is the lamps in your room.

If paused: terminal animations must not use game time (UNVERIFIED whether UI Toolkit transitions honour timeScale). The C26 rule (pause VideoPlayers while inspecting) also applies.

- [Steam Community: Alien: Isolation, is the game paused at terminals? (2015)](https://steamcommunity.com/app/214490/discussions/0/617329150702516350) (player forum): terminals run on active game time
- [research/inspect/02_game_research.md:101, 112, 285](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/inspect/02_game_research.md) (project research): terminal precedents (Alien: Isolation; Lethal Company local capture D3)
- [FrontRoomsMapHunter.cs:167, 210, 215](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/FrontRoomsMap/FrontRoomsMapHunter.cs) (code): WarnStage, StateChanged, WarnStageChanged
- [FrontRoomsMapWorld.cs:3298, 3300](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs) (code): FixtureChanged and LampDipped events
- [SCREENS_VISUAL_SYSTEM.md:53-65](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/SCREENS_VISUAL_SYSTEM.md) (project doc): screens joining the warning language (proposal)
- [research/inspect/10_design.md:453-480](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/inspect/10_design.md) (project doc): what stops and runs under Phase.Inspect, including screen ads

### 12. Existing per-monitor costs and defects to fix before adding a terminal layer.

VL115 (FAIL, still open in the working tree): the screen quad is in no LOD. The monitor culls at 11.18 m (lodBias 1; about 16.8 m at Very High, 22.4 m at Ultra) and the lit quad floats on alone, out to the 18 m visibility gate. Audit F7 gives the fix: add the quad renderer to every LOD of the monitor's LODGroup. The current diff contains only the F6 shader change.

Per-monitor overhead today:
- 1 extra draw (the quad), 1 Material and 1 runtime Mesh per monitor. MakeQuad builds a new Mesh per screen, and OnDestroy destroys only the material. Mesh instances therefore probably accumulate as chunks rebuild until assets are unloaded. This is PLAUSIBLE; not measured.
- 1 Update per screen.

WebGL is per-draw bound, so a shared static quad mesh and a WebGL flipbook tier (no VideoPlayer, as already planned) matter.

Added cost of the live terminal (ESTIMATE): one panel render into one RT, a handful of UI Toolkit draws (UNVERIFIED count; check with the Frame Debugger), one mip generation, and tube ALU on about 20–24% of screen pixels at the 0.30 m framing.

Flat-quad caveat: up close the flat quad cuts across the bezel's rounded inner corner (audit note). Drawing through the model's own Prop_ScreenCRT slot fixes that, but needs the UV and Read/Write work from finding 9.

- [VERIFICATION_LOG.md:404 (VL115)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/VERIFICATION_LOG.md) (verification log): quad outlives monitor LOD; 138 px at 14 m
- [research/codex_audit/10_review_glass-look.md:93-121](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/codex_audit/10_review_glass-look.md) (project audit): F7 cause and LODGroup fix; bezel-corner note
- [FrontRoomsScreenVideo.cs:90-97, 99-114, 171-176](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/Media/FrontRoomsScreenVideo.cs) (code): mesh per screen, not destroyed; per-screen Update
- [QualitySettings.asset (lodBias 0.3–2 per tier)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/ProjectSettings/QualitySettings.asset) (config): cull distance scales with lodBias

### 13. Who owns which files: the terminal touches five chats, and one file has no owner.

游戏视觉 (Red-approved rule, 2026-10-07):
- Assets/Scripts/Office/*: OfficeKit.AttachScreen (:851, :875), KitLibrary.Spawn and TryAnchor, DressJob `then`.
- Resources/Rendering: the FrontRoomsScreenVideo.shader that another session just added untracked. Route or approve it through 游戏视觉.
- Resources/Props: Kit_CRTMonitor.json screen anchor; the fbx.meta Read/Write setting.
- Resources/Surfaces: Prop_ScreenCRT, Prop_ScreenCRT_On, Prop_GlassCRT.
- Rendering/Inspect (EXAMINE), the tube shader, and the Blender kit anchors (e.g. Kit_CRTTV).

关卡设计 (map chat):
- FrontRoomsMap/*: Describe/Use, the office Dress reflection call (:1926-1938), and the Relay and lamp events.
- FrontRooms3DGame.cs: UpdateAim, SetPhase/cursor, HUD prompt.
- FrontRoomsShots/*: the rig contract went to the map chat; EXAMINE contract C1–C26 is addressed to it.

Interaction-design (touch) chat: Input/* and FrontRooms3DGame.Mobile.cs (TOUCH_CONTROLS §8), including EXAMINE T1–T7.

平面视觉: the terminal's visual system, raster, faces and glyphs.

Narrative chat: every on-screen string.

系统设计 / 关卡设计: event hooks and budget gating.

Media/FrontRoomsScreenVideo.cs has NO assigned owner (EXAMINE §7.6), and it currently carries uncommitted edits. Red must assign it before anyone builds on it.

- [memory: game-visual-owned-paths.md (Red-approved 2026-10-07)](file:///Users/redwang/.claude/projects/-Users-redwang-Desktop-ArtCenter-Fall26T7-EGAM-401A-01-Individual-Game-Project/memory/game-visual-owned-paths.md) (project rule): 游戏视觉-owned paths
- [SCREENS_VISUAL_SYSTEM.md:76-84 (owners table)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/SCREENS_VISUAL_SYSTEM.md) (project doc): screen owners per layer
- [VISUAL_CHAT_TASKS.md:204 (R1: contract incl. camera rig went to the map chat)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/VISUAL_CHAT_TASKS.md) (project doc): camera rig with the map chat
- [TOUCH_CONTROLS.md:177-197 (section 8 table)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/TOUCH_CONTROLS.md) (project doc): Input/* and Mobile.cs owned by the interaction-design chat
- [research/inspect/10_design.md:913-923 (section 7.6, unclear owner)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/research/inspect/10_design.md) (project doc): Media/FrontRoomsScreenVideo.cs has no owner
- [FrontRoomsMapWorld.cs:1926-1938](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs) (code): map calls the office Dress/BeginDress by reflection

### 14. Terminal state needs a stable identity outside the monitor GameObject, because office chunks are destroyed and rebuilt.

The office kit is a pure function of the seed: a destroyed chunk rebuilds the same furniture. The monitor GameObject, its material and any state stored on FrontRoomsScreenVideo die with the chunk.

The spawn callback (`then`, Action<GameObject>) passes only the instance. It carries no room, cell or station index. FrontRoomsScreenVideo.Attach takes only an ad file name.

A terminal needs an ID such as map seed + cell + station index, or the world position rounded to cm. Its state (screen, page, what was read) should live in a map-side registry, following the existing WindowBuilt/WindowReleased + windowByCollider pattern. That registry also decides which stations are live terminals, which keeps level design in 关卡设计 and away from 游戏视觉's furniture code.

The screen anchor should come from the kit sidecar via Info.TryAnchor("screen") instead of the hard-coded Vector3 (problem P6). This also covers the ±9° yaw and the PC-case height variant (+0.13 m).

- [FrontRoomsOfficeKit.cs:13-15, 866-875](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/Office/FrontRoomsOfficeKit.cs) (code): seed-pure rebuild; monitor yaw and PC-case height
- [FrontRoomsDressJob.cs:231, 277-284](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/Office/FrontRoomsDressJob.cs) (code): `then` callback receives only the GameObject
- [FrontRoomsKitLibrary.cs:83-90](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/Office/FrontRoomsKitLibrary.cs) (code): TryAnchor reads sidecar anchors
- [FrontRoomsMapWorld.cs:125-135, 1431-1440 (WindowBuilt/Released registry pattern)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs) (code): precedent for map-side registries of built interactables
- [SCREENS_VISUAL_SYSTEM.md:30 (P6)](file:///Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01%20Individual%20Game%20Project/Frontrooms3D/Documentation/SCREENS_VISUAL_SYSTEM.md) (project doc): anchor and size hard-coded to Kit_CRTMonitor

### 15. Doom 3 is the classic precedent for interactive in-world screens. Use it only as a reference for the interaction model.

id Tech 4's GUI system drives menus, the HUD and the control panels placed around levels. World GUIs are handled differently from menu GUIs.

This supports the crosshair-as-cursor, screen-in-place model as the lightest option. It needs no camera move and works only for large type, which is the glance layer in finding 4.

I did not verify footage of Doom 3's exact cursor behaviour on world screens.

- [id Software Doom 3 SDK docs (dhewm3 mirror): GUIs](https://iddevnet.dhewm3.org/doom3/guis) (developer docs (mirror)): GUI system drives in-level control panels; world vs menu GUIs

**Recommendations from this sweep:**

- Build the computer as an EXAMINE `panel` target with a `screen` extension (EXAMINE's row L1), not as a second camera system. Order: the map chat lands C16–C19, 游戏视觉 lands inspect.sh, then the terminal. Until then, prototype only in a clone.
- Before any terminal work, Red assigns an owner to Media/FrontRoomsScreenVideo.cs. The minimum C26 patch lands first: skip its ray and E while inspecting or paused, and pause VideoPlayers. Then the power toggle moves onto the shared aim path: a map-side registry plus Describe/Use, so the HUD prompt, the touch USE verb and tap-to-use all work. Also land the VL115/F7 LODGroup fix and a shared static quad mesh.
- Use two layers. Glance layer: within 2.4 m and in view, the screen wakes and shows large type, at most 4–6 rows on desktop and 2–3 rows on a phone, with no input. Engaged layer: E or a tap starts the close-up. Desktop aims for camera ≈0.30–0.35 m on the screen normal at FOV −15°, a 25-row raster and caps ≈19–22′. Handheld uses ≤16 rows or a full-height framing.
- Render with UI Toolkit into a PanelSettings.targetTexture: one live RT bound only to the terminal in use. Use 1280x960 on desktop and 640x480 on handheld/WebGL, on the 4:3 raster with title/action-safe areas, bilinear filtering and mips. Follow the promo precedent: UNorm + forceGammaRendering + _SignalDecode, so Figma alpha matches. Idle monitors show off, the ad, or a static per-state frame selected per material, never via MaterialPropertyBlock.
- Put the CRT look in the screen material as one pass. 游戏视觉 promotes PromoScreen into a production tube with a target-3.5 SubShader for WebGL2 and GLES3. Any reduced WebGL path is a WebGL-only branch, per Red's rule. Consider the text-mode character-grid shader for the WebGL tier and the glance layer.
- Cursor: the crosshair is the cursor on desktop and WebGL, so the cursor is never unlocked. Get UV from the existing quad's local hit point and undo the U mirror. Convert to panel pixels and call IPanel.Pick, then set hover; USE activates. On touch, a LookTapped viewport point goes through ViewportPointToRay to the same UV. Use selection-only menus and no typed input, because TouchScreenKeyboard does not support WebGL.
- Terminal state lives in a map-side registry keyed by seed + cell + station, owned by 关卡设计, which also chooses which stations are live terminals. The screen anchor comes from the kit sidecar (TryAnchor "screen"). Strings come from the narrative chat. 平面视觉 designs the raster, faces and glyphs, and decides on licensing for any VGA bitmap font.
- Red decides: does the terminal pause the world (EXAMINE semantics) or stay live (Alien: Isolation / Lethal Company)? If live, keep ShotCancel.Relay, frame so some room lamps stay visible, and route WarnStageChanged and StateChanged to screen interference, per SCREENS_VISUAL_SYSTEM §4. If paused, run terminal animation on unscaled time.
- Verify in a clone before committing:
- UI Toolkit text on a runtime PanelSettings: theme, textSettings or FontDefinition.
- RT orientation on Metal, Vulkan, GLES and WebGL.
- Whether a targetTexture panel repaints every frame.
- Draw count and ms in the Frame Debugger and Profiler, on Mac, iPhone and Chrome WebGL.
- That the rig's 0.1 m world-clamp spherecast reaches the 0.30 m pose past chairs and panels in real office seeds.
- Route file changes by owner:
- 游戏视觉: Office/*, Resources/Rendering (including the untracked FrontRoomsScreenVideo.shader), Props sidecar/import, Surfaces, tube shader.
- 关卡设计: FrontRoomsMap/*, FrontRooms3DGame.cs, FrontRoomsShots/*.
- Interaction-design chat: Input/* and FrontRooms3DGame.Mobile.cs.
- 平面视觉: visuals and type.
- Narrative chat: strings.

## Critic: corrections

- **games · Readability check for FrontRooms' own CRT (FOV 72, 141/d px)**: The FOV comes from FrontRoomsMapWalker.cs:72. That camera is the 'Map test camera' of the standalone FrontRoomsMapTest scene and spawns only when `standalone` is true (FrontRoomsMapWorld.cs:624-626). The shipped game camera is 76° (FrontRooms3DGame.cs:280, CreateCamera; the rig's BaseFov). The calculation also ignores foreshortening: the glass sits 0.5–0.63 m below the 1.62 m eye. It also assumes stand-offs of 0.35–0.6 m, but the desk alone keeps the eye 0.77–0.86 m from the glass. **Fix:** Recompute with 76°. Face height at 1080p is about 131/d px (not 141/d), times a foreshortening factor of 0.68–0.9. Use the real stand-off: at least 0.77 m with no chair, and typically 1.2–1.6 m because chairs block the stand spot (see the tech correction). Result: the glance view holds about 4–6 rows of 12 px caps at the desk edge and about 3–4 rows from behind the chair. 25-row body copy needs the close-up.
- **games · Recommendation: GLANCE within about 0.9 m showing 8–10 rows**: 0.9 m is barely reachable: 0.77–0.86 m is the floor, and only on the 15% of stations without a chair. At that distance 8–10 rows give caps of about 6–8 px, below the project's 12 px / 18.6′ rule (research/inspect/10_design.md:34, :255). The proposal also adds a new 0.9 m threshold, although the game's aim Reach and the CRT's own focus distance are both 2.4 m. **Fix:** Wake the glance view at the existing 2.4 m Reach and cap it at about 4 short lines on desktop and 1–2 on a phone. Alternatively, guarantee a clear stand spot at information terminals (no chair, or the chair pushed in) and re-measure in seeded offices.
- **games · Signalis: 'PC Gamer's review' criticises fuzzy low-res text**: The quote is verified, but the piece is a preview of the demo, not a review. The text says 'Signalis' demo is short… ahead of the full game's release on October 27'. URL: https://www.pcgamer.com/signalis-survival-horror/ **Fix:** Relabel it as a 2022 demo preview. The readability warning still stands, but it is weaker evidence than a review.
- **games · Lethal Company: 'Leaving with TAB is UNVERIFIED'**: The project already has a frame in Documentation/research/inspect/images/r_03_lethal_terminal_live.jpg, taken from the Zeekerss trailer (Research/week01/assets/clips/LC_5_bestiary.jpg). It shows the camera locked to the CRT and 'Quit terminal : [ESC]' printed on screen, and is cited as D3 in research/inspect/02_game_research.md:112 and :285. The sweep didn't use it. **Fix:** Cite the project frame: exit = Esc, as printed on screen. Note that it is a trailer frame with no public URL in the sweep. Drop the TAB claim unless footage supports it.
- **games · Alien: Isolation mobile: 'How terminals work on touch is UNVERIFIED'**: research/touch/01_shipped_games.md:145 already cites an App Store user review saying the in-game computer terminals 'become direct touchscreens'. The tech sweep cites the same source (itunes RSS id=1573029040). The two sweeps contradict each other. **Fix:** Mark this as weakly sourced (one user review) rather than unverified, and use the same wording in both sweeps.
- **games · SOMA: font size set to '4% of screen height'**: The Frictional wiki comment reads `ImGui_NrmSizeKeepRatio(0.04)` 'Sets the font size to 4% of the screen size' and does not name a dimension. **Fix:** Write '4% of screen size (dimension unspecified)'.
- **games · Input recommendation: gamepad uses d-pad focus plus Hypnospace's stick cursor**: FrontRooms has no gamepad layer. FrontRoomsInput.cs binds only <Gamepad>/rightStick (to Look). Move, Use, Pause, Start and ShotBack are keyboard-only, and gamepad is not among the stated platforms (desktop, iOS/Android touch, WebGL). **Fix:** Mark gamepad as out of scope, or list it as a future item. Spec only keyboard and mouse, touch, and WebGL.
- **games · Exit list includes 'E again'**: This conflicts with the tech sweep, which says 'Select with USE'. It also conflicts with EXAMINE, where E, S, Esc, Back and the touch pull-back all exit a panel close-up and touch USE turns into BACK (research/inspect/10_design.md:347; T1/T2 at :895-896). **Fix:** Pick one rule for the terminal: either USE selects and S/Esc/BACK exit, or USE exits and selection uses click, tap, Enter or digit keys. Record the choice in the EXAMINE contract.
- **era · Default prompt is 'C>', not 'C:\>'**: The claim is technically right for COMMAND.COM with no PROMPT command, but the practical 1990 look is different. COMPUTE! (Sept 1991) says 'PROMPT $P$G, which is found on most hard disk systems' (https://www.atarimagazines.com/compute/issue133/74_Introdos.php). So narrative's approved `C:\>_` (research/screens/30_screen_strings.md:87) and the unused `C:\>DIR /W` texture are period-plausible. Read as a correction, the sweep would wrongly push narrative to change approved strings. **Fix:** Keep `C:\>`. Use `C>` only as a deliberate variant, such as a floppy-booted or unconfigured machine.
- **era · NetWare client (LSL/IPXODI/NETX) and the 'F:\SYSTEM>login supervisor' frame**: These sources date from 1991 onward. The 3Com readme lists NetWare 3.10/3.11 files, and the footage is NetWare 3.12 (1993). For a 1990 office the documented pattern is an SHGEN-linked IPX.COM plus a DOS-version shell, NET3/NET4.COM, where 'x refers to DOS version' (https://www.ou.edu/class/telecomm/lect05h.htm). F:\SYSTEM is a post-login admin directory. LOGIN.EXE lives in SYS:LOGIN (Novell client docs, https://www.novell.com/documentation/lg/noclienu/noclienu/data/a4zwzii.html), so a pre-login screen sits in the login directory. That directory shows as F:\LOGIN> with $P$G, but this is unverified. **Fix:** For 1990 boot text use IPX.COM + NET3/NET4.COM. Don't copy the 3.12 frame. Keep every NetWare prompt string marked UNVERIFIED until checked against a 2.x/3.0 manual.
- **era · Fire-alarm graphics workstation presented as 'real' 1989–91 screens**: US5189394's assignee is Grinnell Asia Pacific Pty Ltd (Australia; priority 22 Dec 1989, filed 19 Dec 1990), not a US Grinnell product. US5297252 was filed 7 May 1991 by inventor Don Becker and assigned to General Signal in June 1991. Patents record claimed inventions, not screens of shipped products, and the second one is post-1990 (still within the 1993 limit). The strings and layout were verified on Google Patents. **Fix:** Call them 'patent-documented concepts, 1989–91'. Mark the 1991 filing as post-snapshot. Avoid implying that a 1990 US office had this exact screen.
- **era · WordPerfect 'DOS blue' source (Wikibooks mirror)**: https://kwx.liehendr.com/content/wikibooks_en_all_maxi_2021-03/A/WordPerfect returns HTTP 401, so it is a dead or private mirror. **Fix:** Cite the original en.wikibooks.org WordPerfect book, or drop the RGB 0,0,168 claim and use the VGA default #0000AA (= 42/63) that the sweep already sources.
- **era · Phosphor tiers: green P1 near #33FF33 for terminals**: This clashes with the glow ink: pale yellow-green #B6F5A0–#C9F7B0 meaning 'building guidance in the dark' (research/wallpaper_motion/30_narrative_phosphor.md:336) under RELAY's 'each cue has one meaning' (RELAY_PURSUIT_REDESIGN.md:65). The content sweep flags the clash; the era sweep recommends green anyway. **Fix:** Hand the choice to 平面视觉. Amber P3 or colour VGA for interactive terminals avoids the clash; green could be kept for ambient-only screens.
- **era · Recommendation: one VGA text-mode system (80x25 CP437) for every screen**: The content sweep makes a FLOOR PLAN its main page, and the era sweep's own fire-alarm references draw CAD bitmap floor plans. An 80x25 character grid can show a plan only as box-drawing art. **Fix:** Decide before choosing a render path. Either allow a VGA graphics page (640x480, 16 colours, available since 1987) for the plan, or spec the plan as CP437 box-drawing. The grid-shader option in the tech sweep only covers the second.
- **content · 'Directly see the screen' needs two reading depths (21′ at 0.8 m; 12 rows readable at 1.9 m; phone at 0.86 m)**: The sweep treats 18.6′ as an in-world angle. The project rule is a displayed angle: 12 px at 1080p on a 24-inch monitor at 60 cm (10_design.md:34, :255). With the 76° camera, one in-world radian is about 691 px, or about 0.32 displayed radians, so every figure is about 3× too generous. An 80x25 page's 4.9 mm caps come out at about 4 px (about 7′) at 0.8 m. A 12-row layout's 10 mm caps reach 12 px only within about 0.58 m, before foreshortening, not at 1.9 m. The phone figures inherit the error. **Fix:** Use the tech sweep's pixel-based figures instead: about 4–6 lines at the desk edge, about 3–4 from behind a chair, and 1–2 on a phone. Twelve rows or more need the close-up. Re-measure in engine at real stand spots.
- **content · Phone on 45% of stations, binders on 35%**: Both rolls happen only inside the monitor branch (FrontRoomsOfficeKit.cs:862-877), which runs on 88% of stations. **Fix:** About 40% of all stations get a phone and about 31% get binders.
- **content · ALARM HISTORY: 'The data already exists as the director event Called(cause, explicit, pos) and Restore()'**: `Called` exists only on FrontRoomsRelayDirectorPolicy, an isolated seam whose header says 'The current game still uses FrontRoomsMapHunter directly' (FrontRoomsRelayDirectorPolicy.cs:158-164). Only its own tests use it. The payload, FrontRoomsRelayCall, carries Estimate (a world position), Cause, Explicit, Queued and AttentionAtCall. **Fix:** Mark the page as depending on director integration (RELAY build step 2). Add a rule that the log never prints Estimate, Queued or AttentionAtCall, because they leak where the Relay was sent and its pressure.
- **content · Line citations into RELAY_PURSUIT_REDESIGN.md**: The file has uncommitted edits, so several citations have drifted. Sanctuaries are now at :600-605 (cited :594-599). Stored tier is at :669 (cited :658-662). Called/Restore are at :700 and :732 (cited :721). Line :593 is now windows. The wording 'warnings explained once in the pause card, never live' does not appear as quoted; the pause card is mentioned at :92, :406 and :703. **Fix:** Cite by section (§5 sanctuaries, §9.4–9.6 cues and accessibility, the hooks list) instead of line numbers, and re-quote the pause-card rule from its real source or mark it as paraphrase.
- **content · Blocker: 'Shader.Find fails in player builds (F6)'**: An F6 fix is already in the working tree, uncommitted. FrontRoomsScreenVideo.cs now loads Resources/Rendering/FrontRoomsScreenVideo.shader first; the shader is untracked, target 3.5, with a UnityPerMaterial CBUFFER. That folder is a 游戏视觉-owned path. **Fix:** Restate as 'F6 fix pending: needs 游戏视觉 sign-off and a commit', not as an open blocker with no fix.
- **content · 'A terminal on the building's own network is the building talking' (it can carry the plan)**: EGRESS says Office zones are tenant fit-out, off the base-building plan (30_narrative_phosphor.md:79, :211). A 1990 tenant cubicle PC would not be on the building's fire-alarm or building-automation network, and the era sweep places such a workstation in a security or engineering office, not a cubicle. The two sweeps conflict. **Fix:** Narrative and Red pick a fiction: (a) a distinct building-services station inside the Office zone, such as a fire or building-automation graphics PC or a dumb terminal on the building host, as its own kit or desk dressing; or (b) tenant PCs show only tenant content (mail, memos, print queue), and the building plan stays on the placard.
- **content · Screens warning table attributed to SCREENS_VISUAL_SYSTEM §4 (stage 1 'only on sets in the player's room set'; 'Withdraw, Restore: no change')**: The SCREENS §4 table (lines 57-63) says 'Screens in the same cells' and has no Withdraw/Restore row. 'Withdraw, Restore: No change' comes from the wallpaper table in RELAY §9.6 (line 531). **Fix:** Cite each row to its real source. Keep the note that Red has not yet approved screens joining the warning language (SCREENS:65).
- **tech · Finding 4 and recommendation: stand spot = desk front + 0.3 m capsule (glass at 0.77–0.86 m)**: That is the best case. 85% of stations spawn a collidable Kit_TaskChair (collider 0.62×0.95×0.66 m) whose centre is 0.15–0.55 m in front of the desk at x ±0.25 m (FrontRoomsOfficeKit.cs:883-887). That is directly in front of the monitor, which sits at x ≈ ±0.26 m (stationW ≈ 1.6 m × 0.16). Typical eye-to-glass distance is about 1.2–1.6 m (derived from the code, not measured), and the dolly path crosses the chair. **Fix:** Report a distance range, best case and typical. Add a placement rule for information terminals (chair pushed in or absent). Test the 0.1 m world-clamp spherecast against chairs in real seeds, as the sweep already suggests.
- **tech · Finding 6: 'none of the Period1990 stand-ins is a terminal face'**: VT323 ships in Assets/Fonts/Period1990/VT323 (OFL). It is traced from DEC VT320 terminal glyphs and already mapped for CG and teletext (FONTS_PERIOD_1990.md:46). **Fix:** VT323 is the in-kit terminal face. What is missing is an IBM PC VGA face such as Px437 (CC BY-SA 4.0), which still needs a licence decision.
- **tech · Finding 7: 'Unity roadmap: MPBs currently break the SRP Batcher'**: https://activation.unity3d.com/roadmap/75-material-property-block-srp-batcher-compatibility returns HTTP 404. **Fix:** Cite the Unity manual instead: https://docs.unity3d.com/6000.3/Documentation/Manual/SRPBatcher-Materials.html ('The GameObject mustn't use MaterialPropertyBlocks').
- **tech · Finding 9 and recommendation: 'the crosshair is the cursor' in the engaged close-up**: During shots the crosshair fades out: ApplyGameplayHudAlpha sets crosshairHudGroup.alpha = gameplay × (1 − rig.HudFade) (FrontRooms3DGame.cs ~2397-2402). EXAMINE's panel kind maps the mouse to orbit (±20° yaw, ±10° pitch; 10_design.md:345), which keeps the centre ray on the target, and lockLook freezes the view. So in a close-up the centre ray cannot sweep across the screen. **Fix:** In the engaged layer, draw a virtual cursor in panel space, driven by Look deltas and clamped to the screen rect, while the OS cursor stays locked (good for WebGL). Use the ray-hit UV only for the glance layer and for touch taps.
- **tech · Finding 7 and recommendation: lay content on SCREENS' 4:3 raster with title/action-safe areas cropped by overscan; 1280x960 RT**: (a) Computer CRTs underscan, showing the full image with a black border, unlike TVs (https://en.wikipedia.org/wiki/Overscan; that paragraph is tagged citation-needed). NTSC safe areas would cut the CUA menu bar on row 1 and the status or F-key line on row 25. (b) The existing quad is 0.28×0.19 m, a ratio of 1.47:1 that matches the 640x434 RT. A 4:3 RT on it is stretched about 10% horizontally. **Fix:** Computer screens use the full raster plus a black underscan border; keep NTSC safe areas for TV and station content only. Define a 4:3 active area of about 0.253×0.19 m, or rebuild the quad from the kit's screen anchor (P6).
- **tech · Finding 10: free text fails because TouchScreenKeyboard doesn't list WebGL**: The reason given is partly wrong. Unity's Web manual says mobile browsers show a soft keyboard for UI input fields by default (WebGLInput.mobileKeyboardSupport; https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-input.html). **Fix:** Keep the no-free-text rule, but justify it with binding collisions (WASD/E/R/O/S; Space/Enter = Start), a soft keyboard covering the CRT on phones, and the era target (menus and function keys).
- **tech · Finding 10: in-terminal keys**: The arrow keys are already bound to Move (FrontRoomsInput.cs:236-240), Enter and Space to Start (:261-263), and Esc to Pause. Unity's Web manual warns that Esc 'might not always' be forwarded to the app, and Esc also releases pointer lock. **Fix:** While the terminal owns input: navigate with Move (arrows, W, and S only once the S/ShotBack conflict is resolved), select with Enter, USE, or digits 1–9 (currently unbound), and on WebGL make E, BACK or S the exit as well as Esc.
- **tech · Finding 14: a map-side registry (关卡设计) plus the existing BoxCollider for hit-testing**: This contradicts EXAMINE item 11: 'Hit targets come from a visual-owned registry of boxes, tested against the aim ray after the physics ray. No colliders and no layers' (10_design.md:41). **Fix:** Split it explicitly: terminal identity and state go in a map-side registry, and the hit box goes in EXAMINE's visual registry. Get Red to confirm, so two registries don't get built.

## Critic: gaps a designer still needs

- Trigger semantics: Red wrote 'walk up and then directly see'. That could mean the screen reads on approach with no button, or that arriving moves the view to the screen automatically. No sweep asks Red. Auto-engage on approach would take the camera from players walking past the 88% of stations that have monitors, so if chosen it must be limited to designated terminals and blocked during a chase or any perceivable threat.
- Stand-spot rule: a collidable chair stands in front of the monitor at 85% of stations, so the glance distance and dolly path are worse than any sweep assumes. Information terminals need a level-design rule (chair pushed in or absent, a clear 0.6 m stand box), and readability must be re-measured in engine at real stand spots across seeds.
- Pause vs live is still unresolved, and the sweeps contradict each other: games says 'never pause', content routes detail pages through EXAMINE, which is paused, and tech leaves it to Red. If live: EXAMINE's sound snapshot S1 (SFX paused, AMB −10 dB) must not be used, or the stage-2 muffled steps vanish. The dry clock keeps running, so long reading invites the supervisory sweep (RELAY §'quiet player'). Standing still at more than 6 m halves spotting, so reading is a partial hiding pose. Exit fires on TargetAcquired or Spotting ≥ 0.5. If paused: the pause-neutrality test (AT-01g) and the no-prompt-in-chase gate apply, stage 1–2 warnings freeze, and VideoPlayers pause (C26).
- Is terminal use a noise? Games proposes the POST beep, degauss or printer as Relay-audible; content says 0 Attention. The RELAY Attention gains table lists neither power-on, keyboard clacks nor printing. Needs a 关卡设计 + 声音设计 ruling and AUDIO_CONTRACT events; no new AudioSources.
- What E does on a monitor: today E privately toggles power on every CRT. The spec must say whether ambient monitors keep the power toggle, what the info terminal's verb is (READ / USE TERMINAL; strings from narrative, verb glyphs from 平面视觉), and whether power-off shows the burn-in ghost.
- Captions overlap: the desktop caption panel is bottom-centre at y=208, 920×50 px (FrontRooms3DGame.cs:1984). It survives HudFade, so in a framing where the CRT fills 75–80% of the height it covers the CRT's bottom rows (the status and F-key line). The terminal framing needs a reserved caption band, or the captions move, as EXAMINE did with its right-side transcript panel.
- Accessibility is underspecified. The settings have no text-size option. Terminal pages need a transcript or TEXT chip (EXAMINE C24/T6) and a CRT-effect toggle (Her Story's Anti-Glare Filter). Red/yellow/green alarm states need redundant words (Notifier-style ALARM / TROUBLE / NORMAL) for colour-blind players. Blinking (the VGA blink attribute, flashing unacknowledged points) must obey the ≤ 3 flashes/s limit and the WCAG 2.3.1 red-flash rule, and turn static under Reduce Flashing. Camera motion Off should be a hard cut. If terminals run live, offer an option for reading without time pressure (EXAMINE's alternative mode C).
- Touch layout conflict: the left half of the screen is the move-stick zone, and LookTapped fires only for touches that start in the look zone (FrontRoomsTouchControls.cs ~825-843). Taps on the left of a centred CRT therefore never reach the UI, and EXAMINE's left-zone pull-back exit could fire by accident. The terminal needs its own MenuState that gives the whole CRT rect to taps, plus a left-handed mirror and a placed BACK button.
- Touch target size: Apple HIG asks for at least 44×44 pt, and TOUCH_CONTROLS already uses a 44 pt pause hit and an 88 pt USE hit. A 25-row grid at 80–90% of a 402 pt phone height gives rows of about 13–15 pt, so a phone menu can hold only about 7–8 tappable items per page. The phone layout needs larger rows or a soft-key strip, not a scaled desktop grid.
- Function keys on the platforms: Mac F-keys default to system functions unless the user holds Fn (https://support.apple.com/102439), and browsers reserve F5, F11, F12 and sometimes Esc. A CUA or WordPerfect-style F-key legend must therefore be clickable or tappable soft keys, mirrored by unbound keys (digits), and never the only way to act.
- The render path decision depends on content: the floor-plan page needs bitmap graphics (VGA 640x480x16) or a box-drawing spec, while the rest can be a text grid. Decide which, then pick UI Toolkit to an RT or the char-grid shader (or both) per tier.
- Era cursor and mouse: in DOS text mode the mouse pointer was a software text cursor (inverse cell), and many 1990 apps had no mouse at all (1-2-3 R2.2); DOS Shell 4.01 and WP 5.1 did. Spec whether the fictional building software supports a mouse and what its cursor looks like. The text-cursor detail (INT 33h) is unverified; check Ralf Brown's list.
- Power and darkness: say whether an info terminal dies or sags when its room's lamps Sag or Dip, when the room is dark, or in a dead-circuit room. A glowing CRT in a dark room must not imply it attracts the Relay or that darkness hides you (LD R13; the Relay's sight has no light term).
- Persistence: the info terminal is re-hosted on the station nearest the zone site at each revision, so after a revisit shift 'that computer' moves. The player needs a tell from about 3 m, and the spec must say whether read state, cleared stamps and tier tells persist per run or reset on Caught/restart.
- The idle string 'SYSTEM IDLE' (a floating-text blanker, approved by narrative) conflicts with the era sweep's 'plain blank is correct for a 1990 DOS PC'. It needs a pre-1991 DOS floating-text blanker source, or a switch to a plain blank or burn-in ghost.
- Acceptance tests not yet listed: readability measured at real stand spots (desktop and phone); touch hit coverage across the CRT rect, including the left half; pause neutrality if paused, or Relay-exit timing if live; photosafety sampling of screen blink; WebGL draw count and ms with one live RT; and no stretching of the 4:3 active area on the 1.47:1 quad.
- Unverified footage items still block parts of the spec: the camera move and exit prompt at Alien: Isolation's terminals, SOMA's camera focus and monster behaviour while at a terminal, and whether Prey or The Bunker pause. A short footage pass should be logged to the VERIFICATION_LOG Figma section per project rules.
