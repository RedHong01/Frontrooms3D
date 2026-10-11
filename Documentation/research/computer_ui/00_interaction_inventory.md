# Player interactions: what exists today (2026-10-08)

*系统设计. Answers Red's question: "can the player interact with objects, such as reading the evacuation route map or computer screens?" Workflow `interaction-inventory` (wf_5a42ea48-411): five readers, one skeptic per reader re-checking every live claim in code, then a merge. Main at bc0aa79 plus the working tree. The evidence is file:line; owners come from git history and doc headers.*

## Short answers

### The evacuation route map

Short answer: you can SEE the evacuation route map (逃生路线图) in the game, but you cannot interact with it. There is no prompt, no E, and no close-up or zoom.

**Where it is:**
- It landed in main in commit bc0aa79 (2026-10-08 01:47).
- Every run spawns one framed 17x11 in placard on the MAP side of the start door, centre 1.56 m above the floor.
- The seed picks one of three mounts:
  - **P1**, on the facade beside the door: 76 % of runs.
  - **P2**, on the side wall of the door cell: 22 %.
  - **P3**, on the facade past the arch pier: 2 %.
  - These rates come from 400 runs at real door positions (research/placard/30_render.md:19).
- It faces into the map, so as you walk out of the start door it is behind you. You have to turn round, walk up to it and read it with the normal first-person camera.

**Reading distance** (my estimate):
- The title reads from about 1-1.5 m.
- YOU ARE HERE, the footer and the legend labels need about 0.5 m.
- The smallest lines need about 0.3 m.
- By day the legend labels are pale ink that barely shows.

**Why there is no interaction:**
- It is spawned with colliders off (Rendering/FrontRoomsPlacardMount.cs:75, :85; Kit_EvacPlacard.json noCollider), so the aim ray passes straight through it to the wall.
- The game's only use verb, MapWorld.Describe/Use (FrontRoomsMapWorld.cs:2366-2384), answers only for doors and window panes.
- Touch is the same.

**What it shows:**
- A fixed printed sheet, identical every run. It is not the generated level: one room, YOU ARE HERE, a dashed route to EXIT, and a wallpaper legend (AS PRINTED; IN POWER FAILURE: WAY ON / EXIT / NO EXIT).
- The footer reads 'IN CASE OF FIRE: WALK, DO NOT RUN. CLOSE DOORS BEHIND YOU.'
- The legend glows pale green only when its cell's lamp goes dark (FrontRoomsPlacardGlow.cs). In most runs it reads as an ordinary sign.
- The WAY ON / EXIT / NO EXIT wall cues it explains do not exist in the game yet, and the game has no reachable exit.

**It is removed** together with the start rooms once you are far into the map.

**Uncommitted work in the working tree** (Red's auto-commit should carry it):
- A swatch recolour of the art.
- The map's KeepClearAtStart, which keeps furniture from standing in front of P2/P3.

**The interaction Red means** ('walk up, press E, the world pauses, the camera frames the sheet, zoom') is EXAMINE.
- 游戏视觉 designed it on 2026-10-07.
- It was built and tested only in private clones (~/FrontRoomsVisualWork/proj_inspect*) on 2026-10-08, with EditMode 93/93 and PlayMode 89/89 passing, and is marked 'not merge-ready'.
- Main has no Phase.Inspect, no ShotKind.Inspect and no Rendering/Inspect code (FrontRooms3DGame.cs:31; FrontRoomsCameraRig.cs:6).
- It waits on the map chat's rig and timing contract C16-C19 and on Red's decisions R1-R9 (research/inspect/20_prototype.md:3-18; 10_design.md:1053-1067).

### Computer screens

Partly. Computer screens exist and play video, but the only thing a player can do with one is a hidden power toggle. Nobody can use, read or operate a computer.

**What is live** (main scene, title to map, desktop, Editor Play Mode):
- **Where the screens are:** map Office zones only. That is about 16 % of zones, never the start rooms, and depends on the seed. About 88 % of cubicle workstations get a beige CRT monitor, about half of those on a PC case, plus a keyboard and mouse (Office/FrontRoomsOfficeKit.cs:854-889).
- **What they show:** every desk monitor gets FrontRoomsScreenVideo (OfficeKit.cs:851, :875). It plays the same 12 s silent looping ad, FrontRooms_Ad_01.mp4, in sync on a flat unlit quad whenever you are within 18 m.
- **What the player can do:** aim at a monitor within 2.4 m and press the keyboard E key to turn that screen off or on (Media/FrontRoomsScreenVideo.cs:124-154).
  - No prompt is ever shown: the 'E TURN ON/OFF' string is unused, and the game's own aim ignores monitors.
  - It makes no sound.
  - It reads the raw keyboard outside the game's input system, so it also fires while the game is paused or caught.
  - It does not work on touch.
  - From the code, it should silently fail across Office post-grade trigger-box seams. This was not tested.
- **TVs:** CRT TVs, and monitors that land in furniture piles, are dead dark glass.
- **Other computer props:** the PC case and keyboard have no interaction.

**Problems:**
- **The ad shows a real trademark:** its first card shows LEVITZ, which must not ship.
- **Player builds show no ad:** in builds cut from HEAD, including today's classroom Mac and Windows builds, the screen shader is missing (audit F6), so no ad appears. The fix, an untracked Resources/Rendering/FrontRoomsScreenVideo.shader, is uncommitted.
- **Floating quad:** between 11 and 18 m the ad quad floats in mid-air after the monitor's LOD culls (F7/VL115).

**Exists but not reachable in normal play:**
- The Screen Ads demo scene (E toggle, disabled in Build Settings).
- The map test and Level Designer scenes.
- The title-stream Office profile room with CRTs, which is never activated.
- The unused DOS-prompt material Prop_ScreenCRT_On.
- The dead legacy FrontRoomsOfficeFurniture.

**Designed only:**
- Ad_02 (WXRM-TV 61, a fictional brand), with station and alarm slates and amber office screens (C:\>_, PRINT QUEUE, ALARM LOG). They are rendered in Tools/screens/out; the swap is held for Red.
- A CRT tube shader, power variation and a decode budget.
- Screens joining the Relay warning language. This is a proposal, and Red decides.
- An interactive overlay layer (SCREEN_ADS.md).
- The CRT as an EXAMINE 'E · LOOK' target (L1), deferred until LEVITZ is gone and C26 lands.

**Absent, not even designed:** typing, a terminal, logging in, and screen sounds.

### Did 系统设计 build any of these?

No. The 系统设计 chat (this session, transcript 95375b1f) built none of these interactions.

**What it did write** (per the history reader's transcript audit):
- Documentation/RELAY_PURSUIT_REDESIGN.md (31 edits), plus the RELAY_PURSUIT_REDESIGN*.md variants and research/relay_pursuit/sim.
- Its own memory file.
- Figma RP01-RP10 (section 2441:3804).

It never wrote under Frontrooms3D/Assets. On 2026-10-03 it told 关卡设计, in Chinese, that it was doing design only, with no code changes. A code check found nothing that contradicts this; the claims about the transcript itself were not re-checked in the verify pass.

**Code tied to its designs:** the only code is Assets/Scripts/FrontRoomsMap/FrontRoomsRelayDirectorPolicy.cs, the disturbance kinds EaseDoor/Barge/Slam/Pry and the sanctuary flag. A Codex thread wrote it at Red's request (04c3a6a, 2026-10-04). It is unwired and used only by EditMode tests.

**Who built what:**
- **Doors, glass and climbing:**
  - Codex wrote the first door and glass prompts in the 3D prototype (c8349b3, 2026-09-30).
  - The original Claude main chat, then 关卡设计, built the map doors, glass hold, climb and keys.
  - 关卡设计 also built the glass shot and the uncommitted key-turn door shot.
- **Placard, kits and EXAMINE:** 游戏视觉 built the evacuation placard and its glow (bc0aa79), the window-frame kit, the office kit and furniture piles, and the EXAMINE design and clone prototype.
  - 平面视觉 made the placard art, and the narrative chat wrote its strings.
- **Office screens:** Codex built the CRT ad screens, the E toggle and the demo scene (thread 01a107c3, 04c3a6a and b5f381f, 2026-10-04, at Red's request).
- **Touch:** the touch-design chat built the USE button and look-tap.

**Answer for Red's '你做了吗？':** this chat did not make any of it. The things you can touch were made by 关卡设计 (doors, glass, keys), 游戏视觉 (placard, office props) and Codex (screen ads and toggle).

## Live in normal play (main scene, default settings)

### Map doors: E opens / E shuts

- **Player:** Aim the centre dot at a map door leaf from 2.4 m or closer. The prompt reads 'E · OPEN DOOR' or 'E · SHUT DOOR', and E toggles the door. This is a single toggle: there is no separate ease or slam. Opening from the swing side steps the body out of the leaf's arc, except while the Relay sees you or is chasing. The camera jolts on the handle and the latch. Every open or shut is a 14 m noise the Relay can hear. A broken door, or one the Relay is breaking, shows no prompt.
- **Where:** Map doors only. The generator makes a Door edge only on a border between a Low zone and a Standard zone. Same-height borders become an arch, an opening or a wall, and Tall borders become windows. The start door and the title-stream doors are not E doors (see the next item).
- **Owner:** Codex wrote the first prompt in the 3D prototype (c8349b3, 2026-09-30). The original Claude main chat made the map version (0a53cfd, 2026-10-01), and 关卡设计 (the map chat) has maintained it since (aba5f77, 6c6fe81).
- **Notes:** This is the only general use verb in the game. Describe returns null for every collider that is not a door or a pane, so no other object ever shows a prompt. Gamepad has look only, with no use binding (FrontRoomsInput.cs:247-248). The UseDoor wrapper exists only in the uncommitted working tree.
- **Evidence:** Assets/Scripts/FrontRooms3DGame.cs:1351-1394 (UpdateAim, E calls UseDoor), :186 (Reach 2.4), :1292-1298 (UseDoor), :1167-1177 (OnDoorPulled step), :922-932 (door noise), :692-695 (jolts); Assets/Scenes/FrontRooms3D.unity:1074 (doorNoiseRadius 14); Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs:2366-2384 (Describe), :2393-2431 (Use), :2684-2729 (Open/ShutDoor); Assets/Scripts/FrontRoomsMap/FrontRoomsMap.cs:363-367 (Door vs Window edge rule); Assets/Scripts/Input/FrontRoomsInput.cs:254-256 (Use bound only to <Keyboard>/e). HEAD bc0aa79 has the same verb (map.Use at :1300).

### Title-stream doors and the start door open by proximity (no E)

- **Player:** There is no verb. Space or Enter on the title gives you control where you stand (on touch, tap the title). Walk to within 4 m of a stream door and it swings open by itself in 0.9 s. The start door into the map stays shut until the map behind it is built (at most 3 s), then opens the same way. It shuts for good once you are 4 m or more into the map. It shows no prompt.
- **Where:** The title and start rooms of every run.
- **Owner:** The title-to-map handoff that replaced the noclip white-out on 2026-10-02 (TerminalDoorHeld first appears in 1cd253a). Line of the main chat / 关卡设计.
- **Notes:** The evacuation placard hangs on the map face of this door.
- **Evidence:** Assets/Scripts/FrontRoomsRoomStream.cs:49 (DoorOpenSeconds 0.9), :500-503, :728-760 (TickNearbyDoors <4 m); Assets/Scripts/FrontRooms3DGame.cs:138 (StartDoorShutDistance 4, StartDoorHoldLimit 3), :578-583, :847-899 (UpdateStartRooms), :2232 (Space/Enter); Assets/Scripts/Input/FrontRoomsInput.cs:264-268; FrontRoomsMapWorld.cs:2366-2384 (stream doors are not in doorByCollider)

### Window glass: hold E (or tap E) to break

- **Player:** Aim at a pane from 1.2 m or closer. The prompt reads 'HOLD E · BREAK GLASS', or 'TAP E · BREAK GLASS' in tap mode. Hold E for 1.0 s, or tap about 3 times. The body steps in to 0.55 m and the glass camera shot plays (brace, strikes, crack stages, shatter) while a hold bar fills. Then a 40 m Relay noise sounds and a flash reads 'GLASS BROKEN / WALK INTO THE FRAME TO CLIMB THROUGH'. Moving lets go. S cancels a shot that allows it. A Relay that sees or chases you demotes the shot.
- **Where:** Map windows only, on edges where a Tall zone meets a Low or Standard zone (tallShare 0.1, borderOpening 0.15, or a required gate), so they are rare. Each has a render-only window-frame kit (walnut in Level 0, steel in Office).
- **Owner:** The hold-E verb came from the main chat (0a53cfd). 关卡设计 wrote the glass shot to 游戏视觉's glass plan (df4cb03, 2026-10-03). Tap mode came in aba5f77, 04c3a6a and 16f520e. 游戏视觉 made the window-frame kit, and Codex promoted it (8ef5b64).
- **Notes:** FrontRoomsGlassBreakable is not in main, so the map's pane cube carries the collider.
- **Evidence:** FrontRoomsMapWorld.cs:1416 (pane colliders), :1432-1436 (DressWindow), :2377-2381 (prompt); FrontRooms3DGame.cs:1362-1366 (1.2 m reach, tap prompt), :1395-1442 (hold/tap), :1264-1286 (BeginGlass), :934-945 (noise + flash), :1759 (setting row); Assets/Scripts/FrontRoomsShots/FrontRoomsShotTimings.cs:107,111 (HoldSeconds 1.0, Reach 1.2); FrontRoomsMap.cs:362-367; Assets/Scripts/Office/FrontRoomsInteractableKit.Window.cs:8-36

### Climb through a broken window

- **Player:** There is no button. Once the glass is broken, walk into the frame from 0.95 m or closer (within 0.45 m sideways, moving toward it). A 0.6 s climb lifts you 0.35 m and ducks you 0.55 m.
- **Where:** Any broken map window.
- **Owner:** Main chat / 关卡设计 (4722918, 2026-10-02)
- **Notes:** This is a movement verb, not an object interaction.
- **Evidence:** FrontRooms3DGame.cs:1052, :1193-1240 (TryStartClimb/Climb), :194 (Climb constants), :943 (only taught by the shatter flash); FrontRoomsMapWorld.cs:885 (IsBrokenWindow)

### Zone keys: walk-over pickup (glowing placeholder cube)

- **Player:** There is no button. Come within 0.9 m (horizontal) of a glowing cube, about 0.32 m long, that spins 1.05 m above the floor or rests on a module key spot. It is collected automatically and the FMOD KeyPickup sound plays. While you stand in that key's zone, a key panel shows '<ZONE> KEY'. The caught card counts 'N KEYS'. The key opens nothing, because no door is ever locked.
- **Where:** One key per non-Tall zone, never in the start area.
- **Owner:** Main chat / 关卡设计 (0a53cfd, 2026-10-01). 声音设计 made the pickup sound. The key panel came from the HUD key track.
- **Notes:** This is the only pickup in the game. There is no prompt, no TAKE verb and no pickup shot. The authored key, ring and tag kits are not used.
- **Evidence:** Assets/Scripts/FrontRoomsMap/FrontRoomsMap.cs:648-670 (PlaceKey; skips Tall zones); FrontRoomsMapWorld.cs:937, :1163-1168, :3178-3202 (SpawnKey: primitive cube, collider removed), :3520-3536 (0.9 m auto); FrontRooms3DGame.cs:690, :947-952, :2314-2321 (key panel), :2201 (caught stats); Assets/Scripts/Audio/FrontRoomsSoundDirector.cs:565; Assets/Levels/FrontRoomsLevel0.asset:50 (doorsNeedKeys 0)

### Evacuation-route placard (逃生路线图): look only

- **Player:** Look only. Walk out of the start door, turn round, walk up to it and read it with the normal first-person camera. There is no prompt, no E, no highlight, no zoom and no close-up. It has no collider, so the aim ray passes through to the wall behind it.
- **Where:** One per run, on the MAP side of the start door, centre 1.56 m above the floor. The seed picks the mount: P1 on the facade beside the door (76 %), P2 on the side wall facing the door cell (22 %), or P3 on the facade past the arch pier (2 %). Those rates come from 400 runs at real door positions; the earlier 87/9/4 figure used a door position the game never makes. It faces into the map, so a player walking out has their back to it. It is parented to the terminal stream room and is removed with the start rooms. Desktop and touch are the same; WebGL has no lens and a texture capped at 1024.
- **Owner:** 游戏视觉 built the prop, mount and glow (Q16) and landed it with W/apply/placard_q16.sh in Red's auto-commit bc0aa79 (2026-10-08 01:47). 平面视觉 made the art (v2/v3), and the narrative chat wrote the strings (A.12).
- **Notes:** From the east half of the doorway the open west leaf hides P1, and from the door cell the arch pier hides P3. The art file Prop_EvacPlan_A.png has an uncommitted swatch recolour (pink/grey to mustard). 40_merged.md still says 'not applied', but the files are in HEAD.
- **Evidence:** Assets/Scripts/FrontRoomsRoomStream.cs:539-542 (EndStreamAt calls FrontRoomsPlacard.Prepare); FrontRooms3DGame.cs:640 (EndStreamAt), :740 (MapRunStarted), :912-919 (StopTitleCorridor); Assets/Scripts/Rendering/FrontRoomsPlacardMount.cs:22-26 (no lens on WebGL), :59-99, :75 and :85 (KitLibrary.Spawn(..., false) = no colliders); Assets/Resources/Props/Models/Kit_EvacPlacard.json:4,84 (colliders [], noCollider true); Assets/Scripts/Rendering/FrontRoomsPlacard.cs:30 (CentreHeight 1.56), :88-128 (P1/P2/P3); Documentation/research/placard/30_render.md:19 (76/22/2); FrontRoomsMapWorld.cs:2366-2384

### Placard content: a fixed printed sheet, not the generated map

- **Player:** Read it by walking up to it.
- **Where:** On the placard sheet (Prop_EvacPlan_A.png, 4096x2048; glow mask Prop_EvacPlan_E.png). It is the same in every run.
- **Owner:** Art by 平面视觉. Strings by the narrative chat (A.12). Packing by 游戏视觉 (Tools/lookdev/pack_evac_plan.py).
- **Notes:** It shows 'EVACUATION PLAN', one room on column grid A/B 1/2, a YOU ARE HERE dot, and a dashed route to a door marked EXIT. It has a north arrow, the scale line and MATCH LINE - SEE SHEET A-3. The legend has four wallpaper swatches (AS PRINTED; IN POWER FAILURE: WAY ON / EXIT / NO EXIT). The red footer reads 'IN CASE OF FIRE: WALK, DO NOT RUN. CLOSE DOORS BEHIND YOU.' The title block reads LEVEL 0 / SHEET A-2 OF 4 / PRINTED 03/90.
- **Evidence:** Assets/Resources/Surfaces/Textures/Prop_EvacPlan_A.png; Documentation/research/placard/10_spec.md:137-140 (the mismatch with the level is deliberate); FrontRoomsPlacard.cs:88-128 (reads map data only to choose a mount)

### Placard phosphor glow

- **Player:** Passive. When the lamp of the placard's glow cell is logically dark, dim, failing or out in a power failure, the IN POWER FAILURE legend glows pale yellow-green. It does not glow under a normal lit lamp, and the start-area lamp hold counts as lit, so in most runs the player sees an ordinary printed sign. The glow freezes when the Relay catches you, and with Reduce flashing on it stays steady.
- **Where:** The same placard. The glow cell is the side cell for P1/P3 and the door cell for P2.
- **Owner:** 游戏视觉. The glow strength k = 0.158 is Red's call.
- **Notes:** This is the only phosphor ink in the game today.
- **Evidence:** Assets/Scripts/Rendering/FrontRoomsPlacardGlow.cs:5-37, :85-116 (Bind; Caught freezes it), :131-159 (LampModeOf/LampLevel drive _EmissionColor); Assets/Scripts/Rendering/FrontRoomsPhosphor.cs:12-16,38-39; Assets/Resources/Surfaces/Prop_EvacPlan.mat:14-16,36-37 (_EMISSION on); Documentation/research/placard/30_render.md:164 (glows on its own in ~16 % of runs)

### Reading the placard at walk-up distance (no zoom)

- **Player:** To read it you walk right up. The eye can get to about 0.3 m (capsule radius 0.3, eye 1.62 m, plate centre 1.56 m). By my estimate at 1080p and FOV 76: the title reads from about 1-1.5 m; YOU ARE HERE, the footer and the legend labels need about 0.5 m; the 3 mm scale line needs about 0.3 m. By day the legend labels are pale ink on cream and nearly invisible until they glow.
- **Where:** At the start-door placard.
- **Owner:** 游戏视觉 (prop) and 平面视觉 (type sizes)
- **Notes:** The px figures are the reader's estimate; the only measured numbers are for the clone-only EXAMINE close-up (inspect/20_prototype.md:118-127). With the WebGL 1024 cap, small type stays soft even close up.
- **Evidence:** FrontRooms3DGame.cs:280 (FOV 76); Assets/Scripts/FrontRoomsMap/FrontRoomsModuleUnits.cs:92; FrontRoomsPlacard.cs:30-32 (frame 0.467x0.314 m); Documentation/research/placard/30_render.md:61-62, 20_build.md:221; 10_spec.md:150

### KeepClearAtStart map contract (keeps furniture off the placard)

- **Player:** Indirect. It stops office furniture from being dressed in front of a P2/P3 placard.
- **Where:** Start-area rooms. It exists in the working tree only.
- **Owner:** 关卡设计 (map-owned file), requested by 游戏视觉 in placard/40_contract_map.md
- **Notes:** In HEAD the reflection lookup returns null, so furniture can block a P2/P3 placard in about 1 run in 4. Red's next auto-commit should carry the fix.
- **Evidence:** Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs:440-454 ([Preserve] KeepClearAtStart), :2054; FrontRoomsPlacardMount.cs:101-115 (looked up by reflection). `git show HEAD:` finds no KeepClearAtStart, so it is uncommitted.

### Office desk workstations: CRT monitor, PC case, keyboard, mouse (scenery)

- **Player:** You walk among them. Monitors and PC cases have box colliders and block movement; keyboards and mice do not. There is no pick-up, inspect or use, apart from the hidden CRT screen toggle (separate item).
- **Where:** Map Office zones only. Office applies to Standard zones at officeShare 0.3, about 16 % of all zones, and dressOffices is 1. About 88 % of workstations get a Kit_CRTMonitor, and about half of those sit on a Kit_PCDesktop case. Rooms that touch the start area are never dressed, and the seed is random every run, so many runs show no Office near the start.
- **Owner:** The office kit is 游戏视觉's (4722918, 2026-10-02). The map chat wired it in. The time-sliced dresser is 游戏视觉's (bc0aa79).
- **Notes:** The PC's power and HDD LED anchors are not used by any code. Monitors in the Office_Bullpen_4x4 module (Office fill) also get screens.
- **Evidence:** Assets/Scripts/Office/FrontRoomsOfficeKit.cs:50-53, :854-889 (BuildStation: :863 88 %, :867-874 PC, :875 monitor with AttachScreen, :876-877 kb/mouse no colliders); Assets/Scripts/Office/FrontRoomsDressJob.cs:305-308; FrontRoomsMapWorld.cs:1956-1964, :2103-2116; FrontRoomsMap.cs:306-307; Assets/Levels/FrontRoomsLevel0.asset:43,102; Assets/Scenes/FrontRooms3D.unity:1078

### Ad video on every Office desk CRT (FrontRooms_Ad_01.mp4)

- **Player:** Passive. Within 18 m, and while the screen is in view, every Office desk monitor plays the same 12 s silent ad loop, in sync, on a flat unlit quad. Every screen starts powered on. The ad's cards read LEVITZ / FRONTROOMS FURNITURE $799 (the type runs off the frame) / CALL NOW / YOU WILL LOVE IT.
- **Where:** Office desk CRTs in map Office zones, in Editor Play Mode. Pile CRTs, Kit_CRTTV and the start rooms never show it. In player builds from HEAD (including today's classroom builds, Mac 0852 and Win 0932) the screen material fails and no ad shows.
- **Owner:** Codex (thread 01a107c3, 2026-10-04, at Red's request; commits 04c3a6a and b5f381f). The uncommitted shader fix is the untracked Assets/Resources/Rendering/FrontRoomsScreenVideo.shader, from the Codex mobile-build session of 2026-10-08.
- **Notes:** LEVITZ is a real trademark and must not ship. The picture stays at full brightness even in dark rooms, every set shows the same channel, and there is no CRT look. Nothing gates it by platform.
- **Evidence:** Office/FrontRoomsOfficeKit.cs:851, :875; Assets/Scripts/Media/FrontRoomsScreenVideo.cs:17 (stream file), :31 (startsPowered), :99-114 (18 m visibility gate), :163-182 (0.28x0.19 m quad), :210-221 (working-tree shader lookup), :273-324 (shared decoder, loop, audio None); Assets/StreamingAssets/FrontRooms_Ad_01.mp4; Documentation/research/codex_audit/10_review_glass-look.md:23,43 (F6: URP/Unlit missing in player builds); Documentation/VERIFICATION_LOG.md:443 (VL154 LEVITZ FAIL)

### E toggles an Office CRT screen on/off (hidden interaction)

- **Player:** Desktop with a physical keyboard: aim the camera centre at an Office desk monitor (any face of its collider, back included) from 2.4 m or closer and press E. The ad quad hides or comes back. There is no prompt, no sound and no HUD feedback, and nothing tells the player it exists.
- **Where:** Office desk CRTs in map Office zones, in Editor Play Mode on desktop. In HEAD player builds there is no ad picture to switch (F6), so nothing visible changes. Touch cannot reach it; an iPad with a hardware keyboard can.
- **Owner:** Codex (TogglePower 04c3a6a; E-key path 4448df9/b5f381f, 2026-10-04)
- **Notes:** It bypasses FrontRoomsInput, the HUD prompt, rig.Consume and the game phase, so it also toggles while paused, caught or during a shot. The ray counts triggers, and Office post-grade volumes are trigger boxes (FrontRoomsPostStack.cs:52-53; MapWorld.cs:2248-2290). Reading the code, E should silently fail when you and the monitor are in different grade rectangles (chunk seams every 24 m, or row changes); this was not run. The Screen Ads demo scene has the same toggle.
- **Evidence:** Media/FrontRoomsScreenVideo.cs:43 (InteractionPrompt 'E TURN ON/OFF' is never read), :113, :116-122 (Camera.main is null because the scene camera is Untagged, FrontRooms3D.unity:260; falls back to FindFirstObjectByType), :124-143 (its own ray: ~0 mask, QueryTriggerInteraction.Collide, GetComponentInParent; raw Keyboard.current.eKey at :141), :145-154 (TogglePower); FrontRooms3DGame.cs:1351-1392 (the game's own aim ignores the monitor)

### Ad quad floats in mid-air past the monitor's LOD cull (bug F7)

- **Player:** Passive bug. Between about 11.2 m and 18 m the monitor's LOD culls it, but the ad quad keeps drawing as a floating rectangle.
- **Where:** Office desk CRTs seen from 11-18 m (Editor play).
- **Owner:** Introduced by Codex (04c3a6a). The fix is assigned to 游戏视觉.
- **Notes:** It was measured in a clone. The code that causes it is unchanged in HEAD and in the working tree.
- **Evidence:** Documentation/VERIFICATION_LOG.md:404 (VL115 FAIL); codex_audit/10_review_glass-look.md:23,44; Media/FrontRoomsScreenVideo.cs:163-182 (no LODGroup handling), :18/:29 (visibleDistance 18)

### CRT TVs and CRT monitors inside furniture piles (dead glass)

- **Player:** Passive scenery. Dark, non-emissive glass, with no video, toggle or prompt.
- **Where:** Level 0 large halls (4x4 cells or more) that roll a furniture pile: pileChance 0.35, and at least 0.6 in Tall zones. Kit_CRTTV appears only through piles.
- **Owner:** 游戏视觉 (kits and FurniturePile)
- **Notes:** Kit_CRTTV cannot carry the video because the quad anchor is hard-coded to the monitor (SCREENS_VISUAL_SYSTEM.md:30, P6).
- **Evidence:** Assets/Resources/Props/Models/Kit_CRTTV.json:42-48 (pile cls Screen), Kit_CRTMonitor.json (pile); Assets/Scripts/Office/FrontRoomsFurniturePile.cs:255-259 (spawned with no screen callback), :296-322, :366-390; FrontRoomsMapWorld.cs:2116-2128; Assets/Resources/Surfaces/Prop_ScreenCRT.mat:67 (emission 0)

### Touch / handheld interaction path (USE button, look-tap)

- **Player:** iOS and Android builds only. A contextual USE button appears when the dot is on a door or pane: tap it for doors, hold it for glass. A quick tap on the look side uses a door under the finger (never glass). Prompts drop the key name ('OPEN DOOR', 'HOLD · BREAK GLASS'). Haptics fire on latch, rattle, crack, shatter and lock-on. Climbing and key pickup work as on desktop.
- **Where:** Only while FrontRoomsHandheld.Active is true: iOS and Android players, or the Editor handheld preview. It is false on Mac, Windows and WebGL, even WebGL on a phone.
- **Owner:** The touch-design chat (iOS 和 Android 触控设计), which replaced Codex's 2026-10-04 first pass (look-tap b5f381f)
- **Notes:** Same verbs as desktop, but there is no CRT toggle and no placard interaction.
- **Evidence:** Assets/Scripts/Input/FrontRoomsHandheld.cs:50-64; Assets/Scripts/FrontRooms3DGame.Mobile.cs:82-89, :94-111, :241-251 (look-tap: doors only), :284-313 (USE kinds), :341-389 (haptics); Assets/Scripts/Input/FrontRoomsTouchControls.cs:55, :690-700, :836-844

### HUD teaching (prompt line, hold bar, pause card, hints)

- **Player:** A prompt sits under the centre dot with a 120x4 hold bar. A 9 s hint, 'Shift to sprint. About 5 seconds, and it hears every step.', shows at the start of EVERY run, not just the first. Flashes cover the glass. The Esc pause card lists 'WASD MOVE · MOUSE LOOK · SHIFT SPRINT · E DOOR · HOLD E (or TAP E) BREAK GLASS · R RESTART · O SETTINGS · ESC RESUME'. Touch has its own rows and hint.
- **Where:** In play and on the pause card.
- **Owner:** Main game UI (map chat / 关卡设计)
- **Notes:** Nothing teaches keys, climbing (apart from the flash), the CRT toggle, or reading or inspecting anything.
- **Evidence:** FrontRooms3DGame.cs:162-163 (CalmHint), :735 (elapsed resets each run), :1973-1983 and :2296-2303 (prompt, hold bar), :1818-1824 (PauseText), :2195 (mobile rows), :1756-1761 (settings), :2201-2208 (caught card); FrontRooms3DGame.Mobile.cs:32-34

### Furniture and shut doors as cover from the Relay's sight (emergent)

- **Player:** There is no button. Any non-trigger collider between you and the Relay (a shut door, a 1.65 m tall cubicle spine panel, a desk) breaks its single sight ray. Shutting a door (E) cuts its sight, but costs the 14 m door noise.
- **Where:** Everywhere once the Relay is released (after the start door shuts). It matters most in Office pods with tall panels.
- **Owner:** Map chat (hunter sight) and 游戏视觉 office dressing (tall panels)
- **Notes:** This is an object relationship, not a verb, and nothing teaches it. It is the only way furniture affects play today.
- **Evidence:** Assets/Scripts/FrontRoomsMap/FrontRoomsMapHunter.cs:276, :1280-1305 (Sees/Visible raycast, triggers ignored); Office/FrontRoomsOfficeKit.cs:45 (PanelTall 'blocks the Relay's 1.6 m sight ray'), :840-842; Office/FrontRoomsKitLibrary.cs:228

## Built, but not reachable in normal play

### Screen interaction prompt and focus state (InteractionPrompt, IsFocused, IsPoweredOn)

- **Player:** None. The 'E TURN ON / E TURN OFF' string and the focus state exist, but nothing displays them.
- **Where:** Not shown anywhere.
- **Owner:** Codex (2026-10-04)
- **Notes:** SCREEN_ADS.md:16-18 says they are there for a future overlay.
- **Evidence:** Media/FrontRoomsScreenVideo.cs:41-44; a grep finds no consumer outside the file; the HUD prompt (FrontRooms3DGame.cs:1973, :2298) shows only map.Describe text

### Unused interaction hooks: touch UseKind.Take, ShotKind.KeyPickup and the other ShotKinds that are never started

- **Player:** None. The touch layer maps a 'TAKE' prompt that is never emitted. The KeyPickup timings (0.95 s lift, travel, pocket) and the Open, Rattle, Climb, DoorBreak and Caught shot kinds exist, but nothing starts them. Only the Glass and Unlock shots ever run.
- **Where:** Not reachable.
- **Owner:** Audit and shot-system work (map and touch chats)
- **Notes:** These are scaffolding only. The look-and-press key pickup itself is designed-only (separate item).
- **Evidence:** Input/FrontRoomsTouchControls.cs:55; FrontRooms3DGame.Mobile.cs:292-293; FrontRoomsShots/FrontRoomsCameraRig.cs:6; FrontRoomsShots/FrontRoomsShotTimings.cs:20-35, :149, :170; FrontRoomsGlassShot.cs:122 and FrontRoomsDoorShot.cs:229 (the only shots started); FrontRoomsMap/FrontRoomsKitSeam.cs:42-55 (IFrontRoomsKeyRig, untracked, 'not used yet')

### Unspawned interactable kits: door signs, door number plates, zone key + ring + tags, key hook board / cabinet, lock hardware, kit door leaves and frames, Kit_ExitSign / _Dead / ExitDevice_Crossbar

- **Player:** None. Nothing spawns them, so the player never sees them. Map doors are code-built cube leaves and keys are primitive cubes.
- **Where:** Their FBX and JSON are in Assets/Resources/Props/Models only.
- **Owner:** 游戏视觉 built the interactables kit, and Codex promoted it (8ef5b64, 2026-10-03). Spawning waits on Red's D1.5 and on 关卡设计's D1.6.
- **Notes:** The materials Prop_SignEngraved and Prop_KeyTagNo are missing, so sign and tag faces would render blank (inspect/20_prototype.md:168). These kits are the MUST targets of EXAMINE.
- **Evidence:** git ls-files lists Kit_DoorSign, Kit_DoorNumberPlate*, Kit_Key_Zone*, Kit_KeyRing, Kit_KeyTag_*, Kit_KeyHookBoard, Kit_KeyCabinet, Kit_Lock_*, Kit_DoorLeaf_*, Kit_ExitSign*, Kit_ExitDevice_Crossbar; a grep in Assets/Scripts/Levels/Editor finds 0 references; FrontRoomsMapWorld.cs:255 (door rig null until D1.6); FrontRoomsMap.cs:184,682,711 (keyHost data, used only by the Unreal exporter); codex_audit/00_main_state.md:30

### Title-stream furnished profiles (Office room CRTs with ad screens, Run room hanging EXIT signs) never activated

- **Player:** None in normal play. All five profile variants are built for every stream room, but only the Lobby variant is ever active, so the Office-variant monitors and Run-variant exit signs never wake up (no quad, no decoder, no E).
- **Where:** FrontRoomsRoomStream profile variants. You can see them only in the edit-mode scene preview or through the editor stream verification.
- **Owner:** The stream/title handoff rewrite (4722918) orphaned them. The office kit is 游戏视觉's.
- **Notes:** This is why the only live computer screens are in map Office rooms.
- **Evidence:** Assets/Scripts/FrontRoomsRoomStream.cs:78, :238, :316 (lobbyOnlyTitle), :408-416 (RuleForStreamSequence returns Lobby unless IsPlayable), :616-631 (BeginPlayableSequence; its only caller is Assets/Editor/FrontRoomsStreamVerification.cs:80), :1412-1445 (Office variant calls OfficeKit.Dress), :1446-1497 (Run exit signs), :1553 (SetActive only for room.rule); the call was removed in 4722918 (2026-10-02)

### Prop_ScreenCRT_On material (DOS prompt: C:\>DIR … EXIT.BAT / Bad command or file name)

- **Player:** None.
- **Where:** The material is in Resources/Surfaces, but no kit slot or script uses it.
- **Owner:** 游戏视觉 / 平面视觉 (CRT textures)
- **Notes:** It carries narrative text the player never sees.
- **Evidence:** Assets/Resources/Surfaces/Prop_ScreenCRT_On.mat:37,68; Assets/Resources/Surfaces/Textures/Prop_ScreenCRT_E.png; Assets/Editor/Rendering/FrontRoomsRenderSetup.cs:382 (the only reference); Kit_CRTMonitor.json:70-74 (uses Prop_ScreenCRT)

### Legacy FrontRoomsOfficeFurniture (procedural CRTs)

- **Player:** None.
- **Where:** Dead code with no callers.
- **Owner:** Earlier stream-office work (session unknown)
- **Evidence:** Assets/Scripts/FrontRoomsOfficeFurniture.cs:4-17, :54-57, :259-271; a grep outside the file finds nothing

### Relay director policy core (Attention panel, disturbance kinds EaseDoor/Barge/Slam/Pry, sanctuary flag)

- **Player:** None. This is a scoring layer for future noise verbs (gains 1, 8, 12 and 8) and sanctuary rooms. Nothing in play calls it, and the game still uses the v1 timed-release hunter.
- **Where:** A pure C# class exercised only by EditMode tests.
- **Owner:** 系统设计 designed it (RELAY_PURSUIT_REDESIGN.md). A Codex thread wrote the code at Red's request (04c3a6a, 2026-10-04), touching only this file, its tests and the doc.
- **Notes:** RELAY_PURSUIT_REDESIGN.md:3 says 'Runtime v2 is not enabled'. Wiring it is 关卡设计's job, behind FRONTROOMS_RELAY_V2.
- **Evidence:** Assets/Scripts/FrontRoomsMap/FrontRoomsRelayDirectorPolicy.cs:5-10 ('does not drive the current timed-release hunter'), :30-40, :182, :281-283, :593-604; Assets/Editor/FrontRoomsMap/FrontRoomsRelayDirectorPolicyTests.cs:27,39,78-80 (only consumer); FrontRooms3DGame.cs:1328-1333

## Demo scenes, editor tools and test paths only

### Locked doors, rattle, 'LOCKED · NEEDS THIS ZONE'S KEY', head-dip key-turn unlock shot

- **Player:** Only with keys switched on. E on a locked door without the zone key rattles it (two camera jolts, no Relay noise). With the key, E plays a 1.55 s head-dip at the keyhole, then the door opens; S or a second E cancels.
- **Where:** Never in normal play or any build. FrontRoomsLevel0.asset has doorsNeedKeys = 0, so no door is ever locked. It is reachable only through the Editor menu 'FrontRooms/Map/Play with keys on' (SessionState, #if UNITY_EDITOR), the -autopilotDoors autopilot, the touch playtest driver, or the Level Designer preview.
- **Owner:** 关卡设计. The lock prompt and rule are in HEAD (f5e99e1, a2c7b35, aba5f77). The unlock shot and keys-on menu are uncommitted work from today (workflow wf_1bb74b51).
- **Notes:** `git log -S BeginUnlock` is empty. The working-tree FrontRooms3DGame.cs depends on the untracked DoorShot file. W5 proposes lockedDoorShare ≈ 0.35, and Red picks.
- **Evidence:** Assets/Levels/FrontRoomsLevel0.asset:50; Assets/Scripts/FrontRoomsMap/FrontRoomsLevelProfile.cs:32-33; FrontRoomsMapWorld.cs:2374, :2407-2426, :2440 (LockedByRule), :3449-3453; FrontRooms3DGame.cs:653-669 (#if UNITY_EDITOR keys-on, doorShot), :1306-1325 (BeginUnlock), :1338-1348 (RattleNoiseRadius 0), :3248-3249 (autopilot); Assets/Editor/FrontRoomsMap/FrontRoomsKeysOnPlay.cs:14 (untracked); Assets/Scripts/FrontRoomsShots/FrontRoomsDoorShot.cs (untracked); Input/FrontRoomsTouchPlaytestDriver.cs:446-450; FrontRoomsModulePreview.cs:144

### Screen Ads demo scene

- **Player:** Editor only. Open the scene and press Play: a WASD/mouse rig, three CRTs playing Ad_01, and E toggles a screen within 3.2 m.
- **Where:** Assets/Scenes/FrontRoomsScreenAdsDemo.unity, disabled in Build Settings. Only FrontRooms3D.unity ships.
- **Owner:** Codex (thread 01a107c3, 2026-10-04, 4448df9/b5f381f), at Red's request
- **Evidence:** ProjectSettings/EditorBuildSettings.asset:8-12 (demo enabled: 0); Assets/Editor/FrontRoomsScreenAdsDemoScene.cs:10, :21-90; Assets/Scripts/Media/FrontRoomsScreenAdsDemoRig.cs:4-9, :69

### Map test walker and Level Designer preview

- **Player:** A debug first-person walker: WASD, mouse, Shift; E on doors; hold E on glass; a debug HUD. Office rooms there also get the ad CRTs, so E toggles them too.
- **Where:** Assets/Scenes/FrontRoomsMapTest.unity and FrontRoomsLevelDesigner.unity. Neither is in Build Settings.
- **Owner:** 关卡设计 / the Level Designer plan (P1-P4)
- **Notes:** It uses legacy Input (activeInputHandler 2).
- **Evidence:** Assets/Scripts/FrontRoomsMap/FrontRoomsMapWalker.cs:4-9, :69-82, :132-166; FrontRoomsMapWorld.cs:572-592 (walker only when standalone; the game uses CreateEmbedded); Assets/Scripts/FrontRoomsMap/FrontRoomsModulePreview.cs:242,345

### Maze / Race generators

- **Player:** Not playable. The keyed-door and loud-window 'decisions' are on paper only.
- **Where:** Editor builders only.
- **Owner:** Early prototype passes (chats unknown)
- **Notes:** Stale docs.
- **Evidence:** Documentation/FRONTROOMS_MAZE_LEVEL_DESIGN.md:13,28; Documentation/RACE_SLICE.md:3,10-11; Assets/Editor/FrontRoomsMaze/FrontRoomsMazeBuild.cs; Assets/Editor/FrontRoomsRace/FrontRoomsRaceVerification.cs

### Instagram promo: a CRT TV playing the WXRM loop with a tube look

- **Player:** None in the game. It is an offline rendered video.
- **Where:** Clone harness in proj_pgcue2. Output is Research/week03/instagram_promo/FrontRooms_promo_WXRM_v4_{HDR10,SDR}.mp4.
- **Owner:** 平面视觉 (2026-10-08, Red's request)
- **Notes:** This is the only place a TV shows content.
- **Evidence:** Documentation/GRAPHIC_VISUAL_TASKS.md:119-162 (§3a)

## Designed only (docs or private clones)

### EXAMINE close-up inspect (placard, key + tag, door sign; clock, vending machine, copier and phone as test beds)

- **Player:** Designed: within 1.6 m and 60 degrees of the placard the prompt reads 'E · READ'. E freezes the world (Phase.Inspect, timeScale 0) and the camera travels to a framed pose in 0.40-0.55 s, with ±3° look, pan and one zoom step to 0.184 m. E, S or Esc returns. There is also a Text transcripts setting and a touch TEXT chip. Today none of this exists in Red's repo.
- **Where:** NOT in main. Built and tested only in private clones (~/FrontRoomsVisualWork/proj_inspect*, inspect_work): EditMode 93/93, PlayMode 89/89, 'Not merge-ready yet'. The test-bed props are ship:false DevTargets in the clone.
- **Owner:** 游戏视觉: design 2026-10-07, prototype 2026-10-08 01:50. 关卡设计 owes contracts C1-C26 (C16-C19 rig and timings first). Also: touch chat T1-T7, 声音设计 S1-S5, 平面视觉 verbs, hints and glyphs, and Red R1-R9.
- **Notes:** This is exactly what Red's question describes, and it is not in the game. There is no W/apply/inspect.sh yet. The diff must be re-anchored, because the working tree has moved a long way past bc0aa79 (about 2,500 uncommitted lines across 56 files).
- **Evidence:** FrontRooms3DGame.cs:31 (Phase has no Inspect); FrontRoomsShots/FrontRoomsCameraRig.cs:6 (ShotKind has no Inspect); no Assets/Scripts/Rendering/Inspect folder (grep finds 0 hits); Documentation/research/inspect/01_inventory.md:5 (Red's request, 2026-10-07), 10_design.md:1-48, :53-69, :1036-1067 (build order; decisions R1-R9), 20_prototype.md:3-40, :48, :118-128; contracts/map_inspect.diff (untracked, anchored on bc0aa79)

### Look-and-press key pickup ('E · TAKE KEY') with pickup shot and first-key examine

- **Player:** Designed: aim at the key within 2.4 m and the prompt reads 'E · TAKE KEY'. It lifts to 0.35 m ahead, turns its tag to the lens, and is pocketed in 0.95 s. On the first key of a run the pose holds with 'E · LOOK'. Today you walk over a cube to collect it.
- **Where:** Nowhere. Only the scaffolding exists (see the unused-hooks item).
- **Owner:** Interaction audit (2026-10-02) plus the map chat. It lands with D1.5 and D1.6, and D1.5 waits on Red.
- **Notes:** It needs a key collider, which the kit does not add (interactables/10_spec.md:620).
- **Evidence:** Documentation/research/interaction_audit/10_audit_report.md:317, :383-397; 01_interaction_inventory.md:427 (G12); research/inspect/10_design.md:54, :159, :1062 (R4); FrontRoomsMapWorld.cs:3184-3187 (the key collider is destroyed), :3520-3535

### CRT monitor as an EXAMINE panel target (L1) and the C26 screen patch

- **Player:** Designed: 'E · LOOK' on a monitor opens a paused close-up of the screen, and the power toggle moves onto the shared aim path.
- **Where:** Not built, not even in the clone (its catalog has no Kit_CRTMonitor).
- **Owner:** 游戏视觉 (EXAMINE). The owner of FrontRoomsScreenVideo is unclear in the docs; the Codex logs show Codex wrote it.
- **Notes:** It comes after the LEVITZ ad is removed and C26 lands.
- **Evidence:** Documentation/research/inspect/10_design.md:65 (L1, deferred because 'its E is the power toggle'), :915-921 (C26), :1049

### Interactive overlay on screens (hover / press / reward regions)

- **Player:** Proposed: aim at a screen and press regions of a world-space overlay (idle, hover, pressed, disabled and reward states), hit-tested by screen UV.
- **Where:** Not in the game.
- **Owner:** Codex screen-ads v0 notes; not assigned
- **Notes:** No Red decision or task entry found.
- **Evidence:** Documentation/SCREEN_ADS.md:15-18, :48-56; Media/FrontRoomsScreenVideo.cs:8-13, :41-43

### Replacement screen content: Ad_02 WXRM-TV 61 (fictional brand), flipbook, station and alarm slates, amber office screens (C:\>_, PRINT QUEUE, SYSTEM IDLE, ALARM LOG)

- **Player:** None in the game. These are finished offline renders and strings, all read in place.
- **Where:** Tools/screens/out/FrontRooms_Ad_02/ (mp4 640x480, 4x4 flipbook, cards, states). StreamingAssets still has only Ad_01.
- **Owner:** 平面视觉 (generator, layout); the narrative chat (strings); Red (approve the swap)
- **Notes:** Dropping Ad_02 into StreamingAssets is the smallest change that removes the LEVITZ trademark. There is no flipbook runtime path.
- **Evidence:** Tools/screens/broadcast.py:1-20, :285-288; Tools/screens/spec/wxrm_v1.json; Documentation/research/screens/30_screen_strings.md:56-93; Documentation/GRAPHIC_VISUAL_TASKS.md:163-166 ('Held for Red': the swap was blocked on 2026-10-08)

### Screens visual system: CRT tube look, power variation, decode budget, joining the Relay warning language

- **Player:** No new verb. Planned: phosphor, scanlines, curvature, a burn-in ghost when off, and a line-and-dot power collapse; brightness that follows the lamps; about 1 set in 3 on; live decode only on the nearest 2 sets, with a flipbook elsewhere (flipbook-only on WebGL). Proposed: hum bars on WarnStage 1, and the emergency slate 'ALARM IN PROGRESS / CLOSE DOORS / BEHIND YOU' as the chase wave passes.
- **Where:** Plan documents only. The tube shader exists only in a clone (FrontRooms/PromoScreen).
- **Owner:** 平面视觉 (programme and station layers, look spec). 游戏视觉 (tube shader, F6/F7 fixes, Kit_CRTTV anchor). 系统设计 and 关卡设计 (events). Red (go or no-go).
- **Evidence:** Documentation/SCREENS_VISUAL_SYSTEM.md:21-30 (P1-P6), :32-51, :53-65 (proposal; Red decides go or no-go), :67-74, :76-94; Documentation/GRAPHIC_VISUAL_TASKS.md:92-117; FrontRoomsScreenVideo.cs subscribes to no Relay or lamp events

### Wall cues the placard legend explains (WAY ON / EXIT / NO EXIT wallpaper bands)

- **Player:** None. The legend describes wall states the walls never show, so today it refers to nothing the player can find.
- **Where:** Only in the private clone proj_pgcue / proj_pgcue2 (CueInk shader, _FR_WayCells) and in preview videos.
- **Owner:** 平面视觉 (art, flipbook); 关卡设计 + 系统设计 (logic); Red (exit definition, narrative §8)
- **Notes:** 'Nothing gets built until Red and all three chats agree.'
- **Evidence:** Documentation/GRAPHIC_VISUAL_TASKS.md:6-30, :41 ('FrontRoomsWayfinding does not exist yet'), :45-46; a grep for _FR_WayCells/FrontRoomsWayfinding/_FR_CellState/CueInk in Assets finds nothing

### EXIT sign v2 (FrontRoomsExitSign driver; hanging and door-header signs, LIT / HALF / LOOSE)

- **Player:** None in main.
- **Where:** Built and verified in a clone (10/10 tests). The package W/apply/exit_sign.sh is ready but not applied, and even once applied it changes only the stream Run and Exit rooms that normal play never reaches.
- **Owner:** 游戏视觉 exit-sign workflow; the apply needs Red or the visual chat
- **Evidence:** Documentation/research/exit_sign/30_integration.md:7-27, :153-162, :191; Assets/Scripts/Rendering/FrontRoomsExitSign.cs does not exist; there is no applied manifest

### Noise-graded door and window verbs: ease / barge / slam, the Ajar state, quiet sash pry, foiled (alarmed) windows

- **Player:** Designed: E while walking eases the door (0.55 s, heard at 6 m, +1); E to open while sprinting barges (18 m, +8); E to shut while sprinting slams (27 m, +12). An Ajar resting state. 'HOLD E · PRY SASH (quiet)' takes 4 s, is heard at 8 m and costs +8. Foil-taped windows call the Relay. Today a door has one toggle and one fixed 14 m noise, and glass can only be broken.
- **Where:** Design docs only. The player side has no input, prompt or door behaviour.
- **Owner:** 系统设计 design (v2, 2026-10-03). 关卡设计 implements steps 5-8. 游戏视觉 sash-latch and foil props. 声音设计 Foley. Red Q4, Q7a, Q7b.
- **Notes:** The scoring side exists as the unwired director policy (separate item).
- **Evidence:** Documentation/RELAY_PURSUIT_REDESIGN.md:579-591, :606, :610, :692-695, :862 (Q4), :869-870 (Q7a, Q7b); FrontRooms3DGame.cs:922-932 (single door noise); FrontRoomsMapWorld.cs:2377-2381, :2393-2431; FrontRoomsShotTimings.cs:58 (unused Ajar timings)

### Alarm devices and trigger rooms (alarm push-bar door, detector room, electrical room, glass floor, Run! alarm corridor) and dead-circuit sanctuary rooms

- **Player:** Designed: stepping into a readable trigger room, or pushing an 'EMERGENCY EXIT ONLY — ALARM WILL SOUND' door, sounds the device and sends the Relay to it. The Run corridor trips an alarm and you sprint about 25 m to a door. Sanctuary rooms ('ALARM CIRCUIT OUT OF SERVICE') are rooms the Relay never enters, with a copy of the evacuation placard.
- **Where:** Not in the game: there is no ModuleTrigger, detector, push-bar or sanctuary room code.
- **Owner:** 系统设计 + 关卡设计 (steps 8 and 14), 游戏视觉 props, narrative; Red Q6, Q7c, N2
- **Notes:** The pull station and bell are decor; the player does not pull them.
- **Evidence:** Documentation/RELAY_PURSUIT_REDESIGN.md:327-344, :593-599, :695, :701, :867 (Q6), :871 (Q7c); Documentation/research/relay_pursuit/30_narrative.md:74-88; research/room_visuals/10_run_directions.md:3-4, :193; Documentation/VISUAL_CHAT_TASKS.md:133 (N2 WAIT-RED)

### Stand-still 'freeze' and v2 teaching text

- **Player:** Designed: standing still for 0.5 s or more at over 6 m halves the Relay's spotting rate. New lines: 'Walking is quiet. Running and slammed doors are heard.', a RESTORE line, and a pause-card legend for every verb.
- **Where:** Not in the game. CalmHint is still the sprint line.
- **Owner:** 关卡设计 (steps 3 and 5), from 系统设计's design
- **Notes:** The placard footer 'WALK, DO NOT RUN' carries the same idea, but it is too small to read without EXAMINE.
- **Evidence:** Documentation/RELAY_PURSUIT_REDESIGN.md:406, :430, :593, :692; FrontRooms3DGame.cs:162, :1818-1824

### Reading notes / memos ('hold E reads', 'Tab opens notes', readable memos, scratched wall messages)

- **Player:** From the early UI doc: hold E to read, and Tab opens notes or a map. Memos on paper stacks would be an EXAMINE L3 target. Nothing in code handles any of it.
- **Where:** Nowhere: there is no Tab binding and no notes code, and the pages are blank.
- **Owner:** UI_SYSTEM.md (early, author unknown); narrative + 平面视觉; Red
- **Notes:** The UI_SYSTEM.md entry is stale. It is conditional on the narrative chat writing content.
- **Evidence:** Documentation/UI_SYSTEM.md:27-34; research/interaction_audit/10_audit_report.md:844 (Q11 open); research/inspect/02_game_research.md:418-422 (recommends no journal), 10_design.md:67, :1080; Input/FrontRoomsInput.cs:155-178 (no Tab); Documentation/VISUAL_CHAT_TASKS.md:249 (Q2 on hold)

### Aim highlight / prompt fades, touch INTERACTION ASSIST and EDIT LAYOUT

- **Player:** Designed: a rim or highlight on the aimed door or pane, prompts that fade, and on touch a look that slows near handles and panes plus a layout editor.
- **Where:** Not built. The prompt pops in, and the only fade is the HUD fade during camera shots.
- **Owner:** Map chat (UI) + 游戏视觉 (rim shader); touch chat; Red Q9
- **Evidence:** research/interaction_audit/01_interaction_inventory.md:433 (G18), 10_audit_report.md:758; FrontRooms3DGame.cs:1966-1977, :2362; Documentation/TOUCH_CONTROLS.md:135-136, :210, :222 (Q9)

## Absent (neither built nor designed)

### Visible office and module props with no interaction (vending machine, copier, desk phone, wall clock, PC case, filing cabinets, water cooler, binders, paper)

- **Player:** None. They can only be looked at, and their printed details read close up (for example the clock frozen at 10:08:37). There is no prompt and no use. No phone call, vending purchase or copier use exists or is designed.
- **Where:** Office dressing (vending 70%/30% by room size, copier 60%/25%, phone on 45% of equipped stations), modules (Kit_WallClock only in L0_WaitingRoom_4x3, Kit_Copier in Office_Bullpen_4x4) and furniture piles.
- **Owner:** 游戏视觉 (kits, office kit, piles)
- **Notes:** EXAMINE lists the clock, vending machine, copier and phone as ship:false test beds, and only in the clone (see EXAMINE). research/inspect/01_inventory.md:30 notes there are no notices, memos or posters anywhere in the game.
- **Evidence:** Office/FrontRoomsOfficeKit.cs:45-63, :598-603, :863-880; Assets/Levels/Modules/L0_WaitingRoom_4x3.asset:56; Assets/Levels/Modules/Office_Bullpen_4x4.asset:32; FrontRoomsMapWorld.cs:2366-2384 (Describe is doors and panes only); no Rigidbody/OnTrigger/OnCollision code in Assets/Scripts

### Office interior windows (Kit_InteriorWindow): look like glass, cannot be broken

- **Player:** None. They have no collider, so there is no prompt, no hold-E break and no climb.
- **Where:** About half of Office rooms with a ceiling of 2.6 m or more.
- **Owner:** 游戏视觉 office kit
- **Notes:** Only Tall-border map windows can be broken. Worth knowing if Red expects every glass window to respond to E.
- **Evidence:** Office/FrontRoomsOfficeKit.cs:63, :600, :683-686 (colliders = !isWindow); Assets/Resources/Props/Models/Kit_InteriorWindow.json (noCollider true); FrontRoomsMapWorld.cs:2378 (only map Window edges register panes)

### Plan of the real generated map / minimap / live YOU-ARE-HERE

- **Player:** None. The placard sheet never changes with the seed or the player's position, and the game has no map UI.
- **Where:** Nowhere.
- **Owner:** Nobody. No doc proposes it; the spec makes the mismatch period-true on purpose (placard/10_spec.md:137-140).
- **Evidence:** FrontRoomsPlacard.cs:88-128 (reads only CellOf/Cache.Edge to choose a mount); FrontRoomsPlacardGlow.cs:131-171 (writes only emission); a grep for minimap/ShowMap/compass in Assets/Scripts finds nothing

### CRT screen toggle on touch

- **Player:** On a phone with no hardware keyboard, screens cannot be turned off or on. The touch USE button reaches doors and panes, and the look-tap reaches doors only.
- **Where:** iOS and Android builds.
- **Owner:** n/a
- **Evidence:** Media/FrontRoomsScreenVideo.cs:141 (Keyboard.current.eKey only); FrontRooms3DGame.Mobile.cs:241-251; FrontRoomsMapWorld.cs:2366-2384

### Using a computer (typing, terminal, log-in, menus, readable screen text)

- **Player:** None. The only E on a computer is the power toggle. No design anywhere covers typing or operating a terminal.
- **Where:** Nowhere.
- **Owner:** Nobody.
- **Notes:** If Red wants it, it needs a new design. EXAMINE's 'panel' kind would be the natural base.
- **Evidence:** Media/FrontRoomsScreenVideo.cs:141-154 (the only computer handler); FrontRoomsMapWorld.cs:2366-2384; Documentation/research/office_and_film/03_ip_canon.md:105 (canon note only)

### Screen sounds (power-on clunk, static, hum)

- **Player:** None. Toggling a screen is silent, and the video's audio track is off.
- **Where:** Not in the game.
- **Owner:** n/a (would belong to 声音设计)
- **Notes:** The synthesised TV power sounds exist only in the Instagram promo mix.
- **Evidence:** Media/FrontRoomsScreenVideo.cs:284 (audioOutputMode None), :145-154 (no audio); no CRT entry in Assets/Scripts/Audio/*.cs or SOUND_COVERAGE_AUDIT.md

### EXIT signs in the map or at the start door

- **Player:** None. The only 'EXIT' a player sees in normal play is the word printed on the evacuation placard.
- **Where:** The generated map and the start-door facade.
- **Owner:** Only proposed: C-1 Office door signs (Red's call), C-3 a Ceiling placement type
- **Evidence:** Documentation/research/exit_sign/10_spec.md:109, 30_integration.md:191; no exit-sign spawn in Assets/Scripts/FrontRoomsMap

### A reachable exit / goal

- **Player:** None. The placard marks an EXIT the game does not have.
- **Where:** Nowhere; Phase has no escape state.
- **Owner:** Red (definition), then the map chat
- **Evidence:** FrontRooms3DGame.cs:31 (enum Phase { Title, Playing, Paused, Caught }); Documentation/GRAPHIC_VISUAL_TASKS.md:236 (the exit definition is waiting on Red)

### Other operable fixtures: player-pulled fire-alarm station, phones, elevators, light switches, hiding spots, crouch, first-person hands

- **Player:** None, and none is designed as a verb.
- **Where:** Nowhere.
- **Owner:** Nobody (Red rejected hands)
- **Evidence:** Documentation/RELAY_PURSUIT_REDESIGN.md:327 (no pull station in the v2 device list); research/room_visuals/10_run_directions.md:193 (pull station as a prop only); research/outlets/10_spec.md:49 (switches are decor); Input/FrontRoomsInput.cs:133-178 (no crouch); Documentation/VISUAL_CHAT_TASKS.md:282 (Red: head-dip, no hands)

## Gaps before "inspect an object" is a real verb

- There is no inspectable target kind in the shared aim/use path. MapWorld.Describe/Use answers only for door leaves and window panes, so no other object can show a prompt or take E. **Owner:** 关卡设计 (map chat): contracts C1-C19 in the EXAMINE map diff *(Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs:2366-2384, :2393-2431; Assets/Scripts/FrontRooms3DGame.cs:1351-1394; Documentation/research/inspect/contracts/map_inspect.diff (untracked))*
- Inspectable objects have no hit target. The placard is spawned without colliders and its kit is marked noCollider, so the ray passes through. It needs an inspect-only collider or layer that does not block movement or Relay sight. **Owner:** 游戏视觉 (placard kit, mount) together with 关卡设计 (layer and ray mask) *(Assets/Scripts/Rendering/FrontRoomsPlacardMount.cs:75, :85; Assets/Resources/Props/Models/Kit_EvacPlacard.json:4,84; Documentation/research/inspect/03_code_survey.md §0)*
- There is no Inspect phase and no world-pause semantics. Phase is { Title, Playing, Paused, Caught }, and pause is only a phase gate. EXAMINE needs Phase.Inspect with timeScale 0 and audio and Relay neutrality. **Owner:** 关卡设计 (FrontRooms3DGame). Red decides R9 (whether Esc from Playing also freezes time). *(Assets/Scripts/FrontRooms3DGame.cs:31; Documentation/research/inspect/10_design.md:1053-1067)*
- There is no camera shot for a close-up: no ShotKind.Inspect and no inspect timings. The rig and timing contract C16-C19 must land in main before the prototype can merge. **Owner:** 关卡设计 (camera rig / FrontRoomsShotTimings) *(Assets/Scripts/FrontRoomsShots/FrontRoomsCameraRig.cs:6; Assets/Scripts/FrontRoomsShots/FrontRoomsShotTimings.cs; Documentation/research/inspect/20_prototype.md:18)*
- The EXAMINE prototype is not merged. FrontRoomsInspect*.cs lives only in private clones, there is no W/apply/inspect.sh, and the contract diff is anchored on bc0aa79 while main's working tree has moved a long way since (about 2,500 uncommitted lines across 56 files). It needs re-anchoring, a dry run and a 3-way rebase. **Owner:** 游戏视觉 (visual chat), after the 关卡设计 contract lands *(Documentation/research/inspect/20_prototype.md:3-40, :176-178; ~/FrontRoomsVisualWork/proj_inspect*/Assets/Scripts/Rendering/Inspect/; no Assets/Scripts/Rendering/Inspect in main)*
- Red's design decisions R1-R9 are open: close-ups blocked near the Relay, placard framing A, first-key-only key examine, door signs read in place, no SHOULD props in v1, and Esc freezing time. **Owner:** Red *(Documentation/research/inspect/10_design.md:1053-1067)*
- No player-facing words or teaching exist for reading or inspecting: no 'E · READ' or 'E · LOOK' prompt strings, no pause-card line, no transcript setting, no glyphs. The pause card teaches only doors and glass. **Owner:** 平面视觉 (verbs, glyphs, hint layout) + narrative chat (transcript text); 关卡设计 owns the HUD code *(Assets/Scripts/FrontRooms3DGame.cs:1818-1824, :2296-2303; Documentation/research/inspect/10_design.md (T/S/strings sections))*
- Touch has no inspect route. The look-tap reaches doors only, and USE kinds cover doors and panes only; the TAKE kind is never emitted. **Owner:** Touch-design chat (EXAMINE touch items T1-T7) *(Assets/Scripts/FrontRooms3DGame.Mobile.cs:241-251, :284-313; Assets/Scripts/Input/FrontRoomsTouchControls.cs:55)*
- There are no inspect sounds: no enter/exit cue, no page or handling Foley. **Owner:** 声音设计 (EXAMINE S1-S5) *(Documentation/research/inspect/10_design.md (sound contract S1-S5); no inspect entries in Assets/Scripts/Audio)*
- Before a CRT can be a 'look at the screen' target, four things have to happen. The power toggle has to move off its private raw-keyboard ray onto the shared aim path (C26). The LEVITZ trademark ad has to be replaced (the Ad_02 swap is held). The build shader fix (F6) has to be committed. The floating-quad LOD bug (F7) has to be fixed. **Owner:** 游戏视觉 (C26, F6, F7, tube shader); 平面视觉 (Ad_02 content); Red (approve the swap); Codex originally wrote FrontRoomsScreenVideo *(Assets/Scripts/Media/FrontRoomsScreenVideo.cs:43, :124-154, :210-221; untracked Assets/Resources/Rendering/FrontRoomsScreenVideo.shader; Documentation/research/inspect/10_design.md:65, :915-921; Documentation/GRAPHIC_VISUAL_TASKS.md:164-165; codex_audit/10_review_glass-look.md F6/F7)*
- Most designed inspect targets are not in the world. Door signs, number plates and the key, ring and tag kits are never spawned (doors and keys are cubes). The sign and tag materials are missing, so their faces would render blank. **Owner:** Red (door/key decision D1.5), then 关卡设计 (DressDoor and key-host spawn, D1.6) and 游戏视觉 (materials, kit hand-off) *(Assets/Resources/Props/Models/Kit_DoorSign*.json, Kit_KeyTag_*.json; FrontRoomsMapWorld.cs:255, :3178-3202; Documentation/research/inspect/20_prototype.md:168; Documentation/VISUAL_CHAT_TASKS.md:108-111)*
- Even with EXAMINE in place, the placard's content points at things the game lacks. The WAY ON / EXIT / NO EXIT wall cues do not exist (no FrontRoomsWayfinding), and there is no reachable exit. It is also not a plan of the generated level (that choice was deliberate). **Owner:** 平面视觉 (cue art) + 关卡设计 + 系统设计 (cue logic); Red (exit definition, narrative §8) *(Documentation/GRAPHIC_VISUAL_TASKS.md:6-46, :236; Assets/Scripts/FrontRooms3DGame.cs:31; Documentation/research/placard/10_spec.md:137-140)*
- The placard's clear-floor contract is uncommitted. In HEAD, furniture can block a P2/P3 placard in about 1 run in 4. **Owner:** 关卡设计 (it will land with Red's next auto-commit) *(Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs:440-454 (working tree only); Assets/Scripts/Rendering/FrontRoomsPlacardMount.cs:101-115)*
