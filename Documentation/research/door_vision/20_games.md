# 20 — What shipped games show through a door window, and how they hide the room behind it (door vision, DV)

Status: RESEARCH, 2026-10-30. Workflow stage of 游戏视觉 (account A, session local_5656cffd), door-vision-panel workflow. Read-only: no Unity, no edits to main except this new file, nothing downloaded. Line numbers refer to main's working tree on 2026-10-30.

Task (Red, 2026-10-30, verbatim): "所有双向门都加，包括地图上的门。最符合当年规范，玩家能隔着门窗偷看，也可能透过窗看见 Relay，是很经典的恐怖桥段。然后relay默认看不到，以及我需要你research一下如何解决门后房间加载的问题，看现有带有窗户的游戏都是怎么处理这个部分的去进行优化". Minutes later: "…同时玩家可以靠近窗户去偷看窗户外部，具体的方式和玩家close up看撤离等一系列intractable是一样的".

This file answers the second half of the first message: **how do shipped games handle what the player sees through a window in a door, when the room behind it might not be loaded, and how do they show a monster through glass?** The period rules for the lite itself (size, wired glass, frame, UBC/Title 8) are another stage's file in this folder. The lite stays outside the door-break lock zone (`../door_break/52_production.md`); that is not repeated here.

**Tags**

| Tag | Meaning |
|---|---|
| **[DOC]** | Official engine or tool documentation that I opened and read (Valve Developer Community, Unity manual, DunGen docs). |
| **[DEV]** | A developer's own words (talk, post, article by the author of the technique) that I opened and read, or a press article that quotes them. |
| **[PRESS]** | A press article or review that I opened and read. |
| **[WIKI]** | A fan wiki. Good for "what happens", weak for "why". |
| **[SEARCH]** | Taken from a search-engine summary; the page itself was not opened (or was behind a bot check). |
| **[CODE]** | Our own code in main, read on 2026-10-30. |
| **UNVERIFIED** | Not confirmed by a source I read. |
| **PROPOSAL** | My suggestion for FrontRooms. Not a decision. |

Full source list with URLs: §8. No footage was watched for this file; clips worth checking are listed in §7.

---

## 0. Short answer for Red

1. **Every engine's cheapest trick is "a shut door hides the room, so do not draw (or even load) it".** Quake-era cell/portal visibility [P1], Doom 3 visportals that close automatically inside a shut door [P6], Source areaportals linked to a door [P3][P4], Unity's Occlusion Portal [P7], DunGen's "Cull Behind Closed Doors" for procedural dungeons [P13], Metroid Prime's doors that stay shut until the next room has loaded [S1]. **A window in the door breaks every one of these**, because a shut door is no longer "out of sight". That is the real problem behind "门后房间加载".
2. **Valve shipped the standard answer for windows: `func_areaportalwindow`.** Near the window you see the real room. As you back away, the window fades to an opaque "rendered window" brush, usually black or a glass texture that shows the nearest cubemap, and only then does the portal close and the room stop drawing [P5]. In plain words: **real room up close, a dark reflective glass card far away.** This is the best-proven pattern for us.
3. **Fakes exist, but they lie when the door opens.** Interior mapping (Joost van Dongen 2008; SimCity 2013; very likely Marvel's Spider-Man) draws a convincing room behind a window with no geometry [G1]. It works for buildings you never enter. For a door you can open, the fake room and the real room must match, and the author lists its own failure modes (doors where windows should be, the same room seen differently from two sides) [G1]. **Not recommended for our doors.**
4. **Our map does not actually have the "unloaded room" problem for geometry.** Chunks are 24 m, the build radius is 2 chunks, the camera far plane is 46 m, and new chunks are built 48 m away [CODE MapWorld 12–14, 71–76; GlassRT 60–63]. Any room the player can peek into from a door is already built. What can lag is **dressing, lamps and reflections**, and only after a jump (run start, C22 floor change, a rebuild). Our rendering already draws rooms behind shut doors (no baked occlusion is possible for a generated map), so **a lite adds almost no draw calls**, only the glass and the pixels seen through it.
5. **Where we *do* have the problem: the title/station room stream and the Relay's "unseen" rules.**
   - `FrontRoomsRoomStream` keeps five rooms. It recycles or re-dresses a room only when it is "out of sight", and today "out of sight" means "its door is shut" [CODE RoomStream 5–14, 653–654, 958–964]. With a lite, a shut door is a window. A room could visibly vanish, move or change dressing while the player watches through glass.
   - The Relay arrives and re-checks its step-in "where the player cannot see it", using a raycast that every collider blocks, door leaves included [CODE Hunter 1010–1018, 1974–1999; V2 249–251, 397–401]. With a lite, **the Relay could appear in a room the player is looking at through the glass.** That is the monster version of pop-in.
6. **One-way sight is always a game rule, never physics.** The classic window scares are built on it: the dogs crashing through the corridor windows in Resident Evil (1996) [M1], the Licker through the interrogation room's two-way mirror in Resident Evil 2 [M2], the harmless "Window" watcher in Roblox DOORS [L1]. Engines make it a per-object switch: Unreal's sight sense traces on a channel that glass can block or ignore [M4]. Real glass gives a physical half of it for free: **the brighter side sees its own reflection and the darker side sees through** [G8]. That fits our existing `RelayLit` rule: a Relay standing in the dark behind a lite stays hidden, a lit one shows.
7. **Ranked recommendation (§5):** (1) the real room behind every lite, behind a "lite-ready" gate; (2) the areaportalwindow-style fallback of dark wired glass with reflection (no fake interior); (3) fix the room stream's "out of sight" test to include lites; (4) make the Relay's "player can see" tests treat an intact lite as transparent (player → Relay) while its own sight stays blocked (Relay → player, Red's default); (5) the peek close-up as an EXAMINE shot whose camera sits at the glass and cuts the reflection, the way you cup your hands against a window. Not recommended: interior mapping, a render-texture camera per lite, Unity baked occlusion / Occlusion Portal, Unity GPU occlusion for this job.

---

## 1. What our game does today (the facts that decide the fit) [CODE]

| Fact | Where | Why it matters for lites |
|---|---|---|
| Map cell 3 m, chunk 8×8 cells = 24 m | `FrontRoomsMap.cs` 52–55 | A door edge always lies inside the player's chunk or a neighbour. |
| Build radius 2 chunks (profile default), one new chunk per frame, nearest first; chunks outside the radius are dropped | `FrontRoomsLevelProfile.cs` 27; `FrontRoomsMapWorld.cs` 12–14 | The room behind any door within reach is always built. |
| Camera far plane = `BuildRadius × 24 − 2` = 46 m | `FrontRoomsMapWorld.cs` 75–76 | Even a long view through two lites in a row ends before unbuilt chunks. |
| Rooms are dressed one per frame after their chunk is built | `FrontRoomsMapWorld.cs` 658–662 | Dressing can lag a few frames after a burst of builds. |
| Fixture lights beyond 16 m are off (3 m fade); shadows only within 9 m | `FrontRoomsLevelProfile.cs` 36–39; `FrontRoomsMapWorld.cs` 3283–3316 | A deep view through glass ends in darkness on its own. This is period-natural and hides depth. |
| A chunk revisited after ≥ 30 s away is rebuilt shifted, but only when it is built again at the radius edge | `FrontRoomsLevelProfile.cs` 31; `FrontRoomsMapWorld.cs` 2265 | The shift happens ≥ 48 m away, beyond the far plane, so no lite can watch it. |
| `camera.useOcclusionCulling = true`, but a generated map has no baked occlusion data | `FrontRooms3DGame.cs` 482 | Rooms behind shut doors are already drawn when in the frustum, so a lite adds no draw calls. |
| URP: Forward+ (`m_RenderingMode: 2`), GPU Resident Drawer off, probe blending off, box projection off | `FrontRooms_URP_Renderer.asset` 51; `FrontRooms_URP.asset` 54–55, 86 | No GPU occlusion today. Per-room box-projected probes are a planned item (G7 in the zone-reflection notes). |
| Glass reflection: one baked HDR cube per zone type, crossfaded; desktop RT glass with a stream-in gate that shows the zone cube until near chunks are scanned | `FrontRoomsZoneReflection.cs` 1–30; `FrontRoomsGlassRTSystem.cs` 56–86 | The RT gate is already a "ready" test we can reuse for lites. |
| Title/station stream: 5 pooled rooms, "a room only changes profile while it is out of sight"; prepared rooms are re-dressed "only while they are still sealed behind … [a] closed door"; recycle waits "until the player can no longer look into it through the door", tested as `behind.doorOpen` | `FrontRoomsRoomStream.cs` 5–14, 653–654, 958–964 | **Sight = door open.** A lite breaks this invariant. The stream doors also line up on one axis, so their lites line up into a tunnel. |
| Relay sight and every "player cannot see it" test use `Visible()`: `Physics.RaycastNonAlloc(..., ~0, Ignore)`, any collider blocks | `FrontRoomsMapHunter.cs` 1974–1999 | A door leaf has a collider, so the lite blocks both ways today. |
| `Arrive` (first release and every relay) places the Relay "where … the player cannot see it"; V2 step-in and candidates do the same | `FrontRoomsMapHunter.cs` 1010–1018, 1031, 1043, 1169; `FrontRoomsMapHunter.V2.cs` 249–251, 397–401, 851, 884 | **The Relay could arrive in plain view through a lite.** |
| `RelayOnScreen` (the UI threat gate `ThreatPerceivable`) also uses `Visible()` | `FrontRoomsMapHunter.cs` 269–293, 340–341 | A Relay seen through a lite would not count as "on screen", so the EXAMINE prompt gate would not see it. |

---

## 2. Technique families: what the player sees, cost, failure modes, fit

### 2.1 Cell-and-portal visibility, and doors as portals

**Origin.** Teller and Séquin (SIGGRAPH 1991) split an architectural model into cells along opaque walls, connected by portals (openings). They precompute which cells can see which, then cull against the view at runtime [P1]. This line of work led to Quake's PVS [P1, patent citations, SEARCH].

**Doors in shipped engines.**
- **Doom 3 (id Tech 4):** a visportal placed inside a `func_door` "will automatically close … when the door is closed" [P6, SEARCH]. Behind a shut door, nothing draws.
- **Source (Half-Life 2, Portal 2):** a `func_areaportal` can name a linked door, "whose open/closed state controls the on/off state of the portal". When it is closed, the portal "cannot be seen through at all" [P3, DOC]. The Areaportal guide tells mappers to set the portal's initial state to the door's [P4, DOC].
- **Unity:** the Occlusion Portal component is "open" or "closed", set from the door script. It only works with baked occlusion data [P7, SEARCH].
- **DunGen (Unity procedural dungeons):** "Adjacent Tile Depth" plus "Cull Behind Closed Doors". The system "relies on a simple adjacency check rather than true camera frustum culling". It "works particularly well when doorways have doors that automatically close behind the player" [P13, DOC]. A portal mode through SECTR VIS makes a portal per doorway and closes it when `Door.IsOpen` is false [P14, SEARCH]. Lethal Company's interiors are DunGen-style tiles. The popular CullFactory mod replaces culling with "which other tiles are visible to the camera through doorways" [P15, DOC].

**What a window does to this.** All of these treat "door shut" as "portal closed". None of the docs I read covers a door with glass in it. **Inference:** a linked areaportal on a glazed door would black out the window whenever the door shuts, so a glazed door needs an open portal sized to the glass, or Valve's window entity (§2.4a).

| | |
|---|---|
| Player sees | The real room through the opening; nothing behind a shut opaque door. |
| Cost | A cheap graph walk per frame. Our grid already *is* the cell/portal graph (cells and edges). |
| Failure modes | Pop-in if the culler is coarser than what the eye sees (DunGen's depth culling makes "hallways disappear", per CullFactory's page [P15]). A door with a lite must become a *narrow* portal (the lite rectangle), not a closed one. |
| Fit for our maze | **Good, later, and only as a WebGL performance item.** We do not cull behind doors today, so lites cost nothing extra. A grid portal walk with lites as narrow portals could cut WebGL draws (the per-draw bound in the WebGL perf memo). By the WebGL-only rule it must be gated to the WebGL build. |

### 2.2 Precomputed and GPU occlusion culling

- **Unreal Precomputed Visibility Volumes** store per-cell visible actors, built in the lighting build, and do not handle movable actors [P12, SEARCH].
- **Unity baked occlusion (Umbra)** is baked in the Editor and saved per scene. A Unity forum thread confirms the bake API is Editor-only, so it cannot run in a build for a level generated at runtime [P8, SEARCH].
- **Unity 6 GPU occlusion culling** uses "the depth textures from the current and previous frames to cull objects" with bounding spheres. It needs the GPU Resident Drawer [P9, DOC], which needs Forward+ and compute shaders, not OpenGL ES [P10, SEARCH]. WebGL 2 has no compute shaders (my knowledge, UNVERIFIED), so it falls back to normal drawing there. Dynamic objects can be occludees only [P11, SEARCH].

| | |
|---|---|
| Player sees | No difference when it works. |
| Cost | Bake: impossible for us. GPU path: extra GPU setup each frame, which can make things slower in scenes with little occlusion [P9]. |
| Failure modes | Conservative spheres; a one-frame lag can pop thin objects. |
| Fit | **Baked: no** (procedural map). **GPU path: possible later on desktop only.** It is not the answer to "what is behind a lite", because it only culls what is truly hidden. |

### 2.3 Streaming triggers and preload rings around doors

| Game / engine | What it does at a door | Source |
|---|---|---|
| **Metroid Prime** (2002) | Rooms load through invisible triggers; at most two rooms are loaded (the current one and the next). A door's shield drops when shot, but the door "will keep her safe" and opens "only when the next room has finished loading". Morph-ball tunnel doors were added so a fast player cannot outrun the load. | [S1] PRESS (2022-11-15, citing developer Zoid Kirsch); two-room limit and shield detail [S2] SEARCH |
| **Resident Evil** (1996) | The door-opening cutscene *is* the loading screen. | [S7] SEARCH |
| **Resident Evil 2** (2019) | No loading at doors; the police station plays as one space; enemies follow you between rooms and some break doors. No technical source on how RE Engine streams it. | [S8] SEARCH |
| **Half-Life / Half-Life 2** (Source) | A "seamless" level change copies the same geometry into both maps around the door. HL2's first map ends just past a doorway; after the load, "from the other side of the door, it appears the old level is still there". | [S9] DOC |
| **Unreal level streaming volumes** | A level loads while the viewpoint is inside any of its volumes and unloads with a 2 s hysteresis. Volumes should be sized so loading is done before the player arrives. | [S3] SEARCH |
| **Unreal World Partition** | Cells load within a loading range around "streaming sources" (the player, or extra sources placed to preload, e.g. a teleport target). | [S4] SEARCH |
| **The Last of Us Part II** | Kurt Margenau: "We used squeeze-throughs to valve the player…"; "Any valve means you can dump the level behind", "and start loading the level ahead", "But only after the squeeze is finished." He adds that squeezes were mainly for drama. | [S5] PRESS quoting the developer (2022-10-23) |
| **God of War Ragnarök / Rise of the Tomb Raider** | Squeezes read as hidden loads. A Tomb Raider developer: "Squeezes were almost never used to load things ahead of them." | [S5] PRESS |
| **Alien: Isolation** | A design blog reads the short delay before an unlocked door opens as a hidden load. No developer confirmation. | [S10] SEARCH, UNVERIFIED |
| **Prey (2017)** | Talos I is continuous, but moving between major areas shows long loading screens (reviews report up to two minutes on PS4). | [S11] SEARCH |
| **Dishonored** | UE3. Harvey Smith defended its loading screens between the streets, the mansion grounds and the foyer. | [S12] SEARCH |
| **Control** | Reviewers report stutters while new areas load on PC. No source on door-level streaming. | [S13] SEARCH |
| **Half-Life: Alyx, Hitman** | I found no official technical source on door-level streaming or windows; only forum claims (Alyx: map loads at transitions, optional streaming in Source 2; Hitman: mission loaded as a whole). | not cited |
| **Elevators, long corridors, airlocks** | The genre-wide habit: hold the player for a known time while the next part loads. MapCore calls these "airlocks"; a short hitch is the accepted fallback. | [S6][S16] SEARCH |

**Lesson for a door with a lite.** Every system above preloads so the room is ready **before the door opens**. With a lite, the room must be ready **before it can be seen through the glass**, which happens earlier: from across the room, along the lite's view cone. The trigger moves from "door reach" to "lite in view within peek distance". Our map already satisfies this for geometry (§1). The title stream does not (§4.3).

### 2.4 What games show through glass when the real room is not there

**a) Distance fade to an opaque "rendered window" (Source `func_areaportalwindow`)** [P5, DOC]
- What it does: "creates an areaportal that automatically closes as the camera moves away, fading a second, opaque brush in to fill the gap." On HL2's d2_coast_09, close up "the player can see through the window", and backing away replaces the outside with "a hazy low-res texture". The rendered window is usually `toolsblack`, which hides everything, or `dev_window`, which shows the nearest `env_cubemap` so "you can still see the world behind". There is a "Translucency limit" for windows of opaque glass. Keys: `FadeStartDist`, `FadeDist`.
- Known failure: the cubemap is taken from the *viewer's* side, so outside objects can appear projected "inside"; the page says it rarely matters because it is only seen far away [P5].
- Fit: **Excellent.** It is distance-gated real geometry with an honest dark-glass fallback. For us the fallback can also be gated by "ready" (dressed, lamps evaluated, RT gate clear), not only by distance.

**b) Interior mapping (fake rooms in a shader)** [G1][G2][G3][G4]
- What the player sees: a convincing room with depth behind every window and no geometry. The shader traces the view ray against virtual floors, ceilings and walls at regular spacing. Shipped in SimCity (2013, a programmer told the author) and in UE4/UDK as a stock function. Insomniac has not described Marvel's Spider-Man's method, but the author and the press read it as interior mapping [G1][G4].
- Cost: one shader per window, almost free.
- Failure modes, from the author: furniture painted flat on the walls; "doors where windows should be"; a different interior seen from two corners; walls showing past building corners; every room the same depth; repeated textures [G1]. The press adds the corner-room inconsistency in Spider-Man [G4].
- Fit: **Poor for our doors.** The fake room must match the real room the moment the door swings, and our rooms are generated, furnished, lit and sometimes shifted. A mismatch on opening is a lie the player sees. Red's realism rules and the Backrooms premise (shifts must be intentional) both argue against it. It might suit exterior or unreachable windows elsewhere, which is not this task.

**c) A captured cubemap of the real room, box-projected onto the glass** [G5][G6]
- What the player sees: a frozen, slightly soft copy of the real room. Box projection (Lagarde and Zanuttini, SIGGRAPH 2012) makes it line up with the room's walls, and our rooms *are* boxes on a 3 m grid.
- Cost: one capture per room. Unity probes support "Via Scripting" refresh and time slicing over 9 or 14 frames [G6]. URP needs box projection switched on in the asset (off today, §1). On WebGL, render-to-cube may be unsupported, which is why our zone reflection already has a "Dip" fallback [CODE ZoneReflection].
- Failure modes: frozen, so lamps flickering or the Relay moving do not show in it; stale after a shift or rebuild unless it is re-captured.
- Fit: **Good as the *far* or *not-ready* layer only**, under (a). It must never carry the Relay, because a frozen Relay would be a lie.

**d) Impostor cards (pre-rendered views)** [G7]
- Octahedral impostors (Ryan Brucks, 2018) bake a mesh from many angles into one card. That suits trees and props. A room through a lite needs a per-room bake.
- Fit: **No.** It is the same problem as (c) at a higher cost.

**e) Darkness, dirty or obscure glass, and reflection hiding the inside** [G8][G9]
- Physics: a one-way mirror works only when one side is much brighter. The bright side sees its own reflection, the dark side sees through, and with equal light it behaves like a window both ways [G8, SEARCH]. Plain glass reflects about 4 % per surface (my knowledge, UNVERIFIED). That is enough to hide a dark room from a lit corridor.
- Source mappers fake one-way glass by texturing one face glass and the other opaque [G9, DOC].
- What the player sees: a dark pane with their own room reflected in it, wire mesh and grime. Only lit things behind it read clearly.
- Cost: ours already exists (zone cube everywhere; RT glass on desktop).
- Fit: **Excellent, and period-true.** It is the natural fallback for (a). It also gives "you may see the Relay" a physical rule: you see it when its side is lit (`RelayLit`).

**f) Covering the glass (curtains, blinds, paper)**
- A common real-world fix, and a game trick to make a window a wall. A 1990 office door lite would more likely have a taped paper notice than a blind (UNVERIFIED; period photos are the other stage's job). Any paper with type goes through 平面视觉.
- Fit: **A per-door variant at most**, for set-dressing or to deliberately blind one door. Not a streaming fix.

**g) A render-texture camera per window (a "portal" view)**
- Fit: **No.** Every lite would cost a full extra scene render. That is unaffordable with every double-acting door glazed, and pointless when the real room is already there.

### 2.5 Backrooms-likes, liminal and procedural horror with doors

- **DOORS (Roblox):** a linear chain of rooms, generated one after another at each door (community recreations describe "when the player opens the next door, load a new room" [L4, SEARCH]). Its doors are opaque. Its **"Window"** entity is a harmless figure that may appear "watching through the glass" of small to medium windows and vanishes after the next lightning strike [L1, WIKI/SEARCH]. A guide claims a later update made it chase the player, so the sources conflict.
- **The Backrooms Game (Favel Studio):** its windows are traps that the player may climb through [L2, SEARCH]. **Backrooms Doors:** keys and locked doors, opaque [L3, SEARCH].
- **Lethal Company / DunGen-style generated interiors:** opaque doors and doorway-based culling [P13][P15].
- **Finding:** I found **no Backrooms-like or procedural horror game whose doors you can look through.** The procedural ones keep doors opaque, which fits their generators and cullers (§2.1). A see-through door is therefore a real point of difference for FrontRooms. It also means there is no shipped procedural recipe to copy, so we combine Source's window entity (§2.4a) with our grid.

### 2.6 Monsters seen through glass, and one-way sight

| Beat | What the player sees | Rule underneath | Source |
|---|---|---|---|
| Resident Evil (1996) corridor windows | A quiet, bright hallway; a crash, a sting, a dog bursts through one window, then another | The glass is where the threat comes *from*; a scripted break, not sight | [M1] PRESS (Kotaku, 2018-04-04) |
| Resident Evil 2 interrogation room | A calm room with a two-way mirror; on the way out, a Licker crashes through it | The player is lulled by "safe" glass | [M2] PRESS (Destructoid, 2014-10-28) |
| DOORS "Window" | A figure staring in through the glass, gone after lightning | A pure look, harmless | [L1] WIKI |
| Outlast | A figure running past a window before you enter; being thrown through a window | Scripted window beats | [M5] SEARCH |
| Alien: Isolation | The menace gauge rises when the player has line of sight to the alien | Player → monster sight is tracked separately from monster → player sight | [M3] PRESS (Tommy Thompson, 2017-10-31) |
| Unreal AI sight | Sight is a line trace on a configurable collision channel; an object (e.g. glass) can block or ignore it | One-way sight = a per-object collision response | [M4] SEARCH |

**Lesson.** No game relies on physics for "I can see it, it cannot see me". It is a rule in the sight code, usually a per-object or per-channel switch. The scare works because the glass reads as **safe**, and then it is either looked through (a glimpse) or broken (the burst). Red's default, "Relay cannot see through a lite", is the safe-glass half. The door-break cassettes (wired glass cracks and holds) are the "broken" half.

---

## 3. Comparison for a procedural, chunk-streamed maze

Scores are for FrontRooms (five = best fit). Desktop is the reference tier and WebGL a reduced tier, per the visual rules.

| Technique | What the player sees | Cost (desktop / WebGL) | Main failure | Fit |
|---|---|---|---|---|
| Real room behind the lite (no culling) | The truth | ~0 extra draws (already drawn) / same | Not-ready dressing or lamps after a jump | ★★★★★ |
| Grid portal walk, lite = narrow portal | The truth | Small CPU walk / could *save* draws on WebGL | Coarse culling pops a room seen through a lite | ★★★★ (WebGL track, later) |
| Distance/ready fade to dark glass (areaportalwindow) | Real up close, dark reflective pane far away or not ready | ~0 | A visible fade if tuned short | ★★★★★ |
| Box-projected capture of the real room | A frozen soft copy | 1 capture per room / Dip fallback | Frozen: no flicker, no Relay; stale after a shift | ★★★ (far layer only) |
| Interior mapping | A plausible but fake room | ~0 | Does not match the room behind the door | ★ |
| Octahedral impostor / per-room card | A frozen card | Bake per room | Same as above, at higher cost | ★ |
| Render-texture camera per lite | The truth | One scene render per lite | Cost × the number of doors | ★ |
| Unity baked occlusion / Occlusion Portal | — | Editor-only bake | Impossible for a generated map | ✗ |
| Unity GPU occlusion | — | Compute; not on WebGL | Only culls what is truly hidden | — (perf only) |
| Paper or blind on the lite | Nothing | ~0 | Kills the feature | per-door dressing only |

---

## 4. Where FrontRooms needs work (fit findings)

### 4.1 Map doors (D1.6, `Kit_DoorLeaf_*_DA`, `FrontRoomsDoorRig`, `InteractableKit.Door`)
- Geometry behind a lite is always built (§1). The risk is only **not-ready** content right after a burst build (run start, C22 floor change, a jump, `RebuildChunk`): undressed rooms, lamps not yet evaluated, RT reflection holes. The GlassRT stream gate already detects this window for reflections [CODE GlassRT 60–86].
- The lamp radius (16 m) and the shadow radius (9 m) are measured from the player. Through a lite at 3–8 m, the room behind is inside both.
- The 30 s revisit shift happens only at the 48 m build edge, so it is never visible through a lite.

### 4.2 The Relay
- **Today the lite is opaque to every sight test, both ways** (door-leaf collider, `~0` mask). That already gives Red's default ("Relay cannot see the player through a lite").
- **But three player-side tests must start seeing through an intact lite**, or the feature breaks:
  1. `Arrive` and the V2 step-in/candidate checks ("the player cannot see it"). Otherwise the Relay can arrive in a room the player is watching through glass.
  2. `RelayOnScreen` / `ThreatPerceivable`. Otherwise a Relay seen through the lite does not count as a threat.
  3. `PlayerSeesRelay` (V2) wherever it uses `Visible(playerEye, …)`.
- What a lite is changes with damage: intact glass passes sight for the player only; a cracked lite (break cassette) still passes the player's sight and still blocks the Relay's; a burst-open door is just an opening.
- These belong to 系统设计 / 关卡设计 (Relay sight is theirs). I only list them here; the contract goes into this workflow's design stage.

### 4.3 The title and station room stream (`FrontRoomsRoomStream`, `FrontRoomsDoubleDoorKit`)
- The invariant "a room only changes while it is out of sight" is tested as "its door is shut" (`behind.doorOpen`, line 964). With lites:
  - **Behind the player:** the room they just left stays visible through the shut door's lite. Recycling it would make it vanish in view.
  - **Ahead:** the stream doors line up on one axis, and so do their lites. Through three aligned lites the player can see the prepared rooms and the end of the pool (the rear seal).
  - **Re-dressing** a prepared room "while still sealed behind the handoff room's closed door" (line 653) can now be watched.
- The stream is owned by its own line (title/station). This file only names the conflict.

### 4.4 Cost
- Desktop: the rooms are already drawn. A lite adds one transparent glass draw (two leaves → two lites per double door) and the fragments seen through it. RT glass reflections apply as for any glass.
- WebGL: same draws. The WebGL perf memo says WebGL is per-draw bound (~3,200 draws), and lites do not add to that. A future WebGL-only grid portal culler (§2.1) could *remove* draws behind shut doors. With lites, those doors become narrow portals instead of closed ones.

---

## 5. Ranked recommendation for FrontRooms (PROPOSAL)

1. **R1 — Real room behind every lite, with a "lite-ready" gate.** No fake interiors anywhere in the map or the stream. A lite shows the live room only when the cell behind it is built **and** dressed **and** its lamps have been evaluated **and** (desktop) the RT stream gate is open. The map makes this true almost always. The gate covers jumps. *Why first:* it is free, it is the truth, and it keeps the Relay visible exactly when it is really there. *Source pattern:* Metroid Prime's "door waits for the room" [S1], moved from the door to the glass.

2. **R2 — Fallback = dark wired glass that reflects the player's side (the `func_areaportalwindow` pattern [P5]).** When not ready, or (WebGL tier) beyond a peek distance, the pane fades to dark glass showing the zone cube or RT reflection of the viewer's side, plus wire and grime. Fade over ~0.3–0.5 s, never pop. Optional far layer: a box-projected capture of the real room (§2.4c), desktop only, captured once when the room is first ready and again after a rebuild. It never shows the Relay. *Why:* this is what real glass does when the far side is dark [G8], it suits a 1990 office corridor, and it hides every not-ready frame.

3. **R3 — Room stream: "out of sight" must include lites.** A stream room may recycle or re-dress only when its door is shut **and** none of the lites between the camera and that room is in the camera's view within peek range. A simple test: the lite rectangle's four corners against the frustum, plus the distance. The end of the aligned-lite tunnel should be **dark** (the next prepared room's lamps off until the player is one room away), so the deep view ends in a natural dark pane rather than the pool's seal. *Owner:* the stream's line, through 关卡设计/总控. This is a contract item.

4. **R4 — Relay sight through lites: asymmetric, a rule, not physics.** One switch, `RelaySeesThroughLites = false` by default (Red). Player → Relay tests (`Arrive`/step-in "unseen", `RelayOnScreen`, `PlayerSeesRelay`) treat an intact or cracked lite as transparent. Relay → player tests treat it as opaque. Keep `RelayLit`: a Relay in an unlit room behind the glass is not perceivable, which matches what glass physically shows. *Source pattern:* per-object sight responses (Unreal [M4]); Alien: Isolation's separate player → alien line of sight [M3]. *Owner:* 系统设计 / 关卡设计 (contract only; I do not edit map files).

5. **R5 — The peek is an EXAMINE close-up whose camera sits at the glass.** E·1 on aim at a lite within reach, E opens the shot, BACK leaves, the shot cancels on the existing threat rule, and phones follow 交互设计's touch rules. In the shot, the camera is ~8–12 cm from the pane and the glass reflection is turned down, as when you cup your hands against a window to see into a dark room. The view is the live room (R1), never the fallback. *Open question for Red* (Q1, §6): when the Relay is in the peek frame, does the shot end at once (today's EXAMINE behaviour) or hold for a beat (the scare)?

6. **R6 — Later, WebGL-only: grid portal culling with lites as narrow portals.** Only if the WebGL profile asks for it, and scoped to the WebGL build per the WebGL rules. *Source pattern:* DunGen adjacency / SECTR portals [P13][P14], with Source's lesson that a glazed door is a narrow open portal, not a closed one.

**Not recommended:** interior mapping (it lies when the door opens [G1]); a render-texture camera per lite (cost × doors); Unity baked occlusion / Occlusion Portal (needs an Editor bake [P7][P8]); Unity GPU occlusion for this job (not on WebGL, and it does not decide what the glass shows [P9][P10]); a blind or paper over every lite (it kills the feature).

---

## 6. Questions for Red (each blocks one item)

- **Q1 (blocks R5's last step).** In the peek close-up, the player may see the Relay through the glass. Option A: the shot ends at once, as every EXAMINE does when a threat is perceivable today. The player is snapped back, which is safe and consistent. Option B: the shot holds ~0.6–1.0 s so the player really *sees* it, then ends. This is the classic beat, but it makes the peek the only close-up that does not cancel immediately. Cost: B needs one timing exception in the shot rig. **Recommendation: B**, because the glimpse is the point of the feature and the Relay cannot see them (R4).
- **Q2 (blocks R2 on WebGL).** On the reduced WebGL tier, beyond peek distance, lites show dark reflective glass instead of the live room. From across a corridor the player sees a dark pane; up close it clears. Cost: none. **Recommendation: yes** (desktop keeps the live room at every distance).
- **Q3 (blocks R3).** In the title stream, the aligned lites let the player see 2–3 rooms ahead. Option A: allow it, with the far rooms dark (R3). Option B: offset the lites (left leaf only on alternate doors) so they do not line up. B breaks the "every double-acting door has period lites" rule. **Recommendation: A.**

---

## 7. Footage worth checking (not watched, nothing downloaded)

| Clip to look for | What to read off it |
|---|---|
| Half-Life 2, d2_coast_09, backing away from the window | The areaportalwindow fade distance and what the faded pane looks like [P5] |
| Resident Evil (1996) / REmake (2002), the window-dogs corridor | How the glass reads before the break; the timing of the burst [M1] |
| Resident Evil 2 (1998), the interrogation-room mirror | Whether the glass shows anything before the Licker [M2] |
| Alien: Isolation, sliding doors with windows | Whether the alien is shown through door glass, and whether it reacts |
| Outlast, the asylum's wired-glass door lites | Wired-glass look, and peeking through a door before opening it [M5] |
| Marvel's Spider-Man, close to an office window | Interior mapping failure modes at close range [G1][G4] |

---

## 8. Sources

| ID | Tag | Source | URL |
|---|---|---|---|
| P1 | SEARCH | Teller & Séquin, "Visibility Preprocessing for Interactive Walkthroughs", SIGGRAPH 1991 (abstract page) | https://history.siggraph.org/?p=120572 |
| P3 | DOC | Valve Developer Community, func_areaportal | https://developer.valvesoftware.com/wiki/Func_areaportal |
| P4 | DOC | Valve Developer Community, Areaportal | https://developer.valvesoftware.com/wiki/Areaportal |
| P5 | DOC | Valve Developer Community, func_areaportalwindow | https://developer.valvesoftware.com/wiki/Func_areaportalwindow |
| P6 | SEARCH | id Software / iddevnet, "Making DOOM 3 Mods: Vis Portals"; modwiki Visportal | https://iddevnet.dhewm3.org/doom3/visportals.html ; https://modwiki.dhewm3.org/Visportal |
| P7 | SEARCH | Unity Manual, Occlusion Portal | https://docs.unity3d.com/Manual/class-OcclusionPortal.html |
| P8 | SEARCH | Unity Manual, occlusion culling and scene loading; Unity Discussions, "Occlusion culling in levels generated at runtime" | https://docs.unity3d.com/Manual/occlusion-culling-scene-loading.html ; https://discussions.unity.com/t/occlusion-culling-in-levels-generated-at-runtime/19887 |
| P9 | DOC | Unity 6.6 Manual, GPU occlusion culling in URP | https://docs.unity.com/en-us/engine/6000.6/manual/analysis/graphics-performance-profiling/in-urp/gpu-culling |
| P10 | SEARCH | Unity 6.3 Manual, GPU Resident Drawer (requirements) | https://docs.unity3d.com/6000.3/Documentation/Manual/urp/gpu-resident-drawer.html |
| P11 | SEARCH | Unity 6.3 Manual, occlusion culling and dynamic GameObjects | https://docs.unity3d.com/Manual/occlusion-culling-dynamic-gameobjects.html |
| P12 | SEARCH | Epic, Precomputed Visibility Volumes | https://dev.epicgames.com/documentation/en-us/unreal-engine/precomputed-visibility-volumes-in-unreal-engine |
| P13 | DOC | DunGen 2.18 docs, Culling | https://dungen-docs.aegongames.com/2.18/optimization/culling/ |
| P14 | SEARCH | DunGen 2.18 docs, SECTR integration | https://dungen-docs.aegongames.com/2.18/integrations/sectr/ |
| P15 | DOC | Thunderstore, CullFactory (Lethal Company mod) | https://thunderstore.io/c/lethal-company/p/fumiko/CullFactory/ |
| S1 | PRESS | Siliconera, "Here is why Metroid Prime had Morph Ball doors" (2022-11-15) | https://www.siliconera.com/here-is-why-metroid-prime-had-morph-ball-doors/ |
| S2 | SEARCH | TASVideos forum post on Metroid Prime loading; VGFacts trivia | https://tasvideos.org/Forum/Posts/389488 ; https://www.vgfacts.com/game/metroidprime/trivia-16440 |
| S3 | SEARCH | Epic, Level Streaming Volumes reference | https://dev.epicgames.com/documentation/en-us/unreal-engine/level-streaming-volumes-reference-in-unreal-engine |
| S4 | SEARCH | Epic, World Partition | https://dev.epicgames.com/documentation/en-us/unreal-engine/world-partition-in-unreal-engine |
| S5 | PRESS | Push Square, God of War Ragnarök squeezes and hidden loading (Oct 2022), quoting Kurt Margenau's post of 2022-10-23 | https://www.pushsquare.com/news/2022/10/god-of-war-ragnaroks-ps5-ps4-squeeze-gaps-spark-insight-into-hidden-loading-screens |
| S6 | SEARCH | MapCore, "Creative Airlocking: Streaming in Action Games" (bot check, not opened) | https://www.mapcore.org/articles/development/creative-airlocking-streaming-in-action-games-r87 |
| S7 | SEARCH | Game Developer, "Resident Evil — Loading Screens and Doors" | https://www.gamedeveloper.com/design/resident-evil---loading-screens-and-doors |
| S8 | SEARCH | GamingBolt, Resident Evil 2: 15 more things you need to know | https://gamingbolt.com/resident-evil-2-15-more-things-you-need-to-know-before-you-buy-it |
| S9 | DOC | Valve Developer Community, Level Transitions | https://developer.valvesoftware.com/wiki/Level_Transitions |
| S10 | SEARCH | "Alien: Isolation Design Masterpost" (blog) | https://ministry.format.com/blog/alien-isolation-design-masterpost |
| S11 | SEARCH | GameRevolution Prey review; PowerPyx Prey PS4 review | https://www.gamerevolution.com/review/332077-prey-2017-review ; https://www.powerpyx.com/prey-ps4-review/ |
| S12 | SEARCH | PC Gamer, Dishonored dev defends loading screens | https://pcgamer.com/games/action/dishonored-dev-defends-loading-screens-i-actually-have-a-soft-spot-for-all-that/ |
| S13 | SEARCH | DSOGaming, Control PC performance analysis | https://www.dsogaming.com/pc-performance-analyses/control-pc-performance-analysis/ |
| S16 | SEARCH | Giant Bomb, Elevators That Mask Loading Times | https://giantbomb.com/wiki/Concepts/Elevators_That_Mask_Loading_Times |
| G1 | DEV | Joost van Dongen, "Interior Mapping: rendering real rooms without geometry", Game Developer (2018-09-25) | https://www.gamedeveloper.com/programming/interior-mapping-rendering-real-rooms-without-geometry |
| G2 | SEARCH | van Dongen, "Interior Mapping", CGI 2008 (Crossref record) | https://api.crossref.org/works/10.1145%2F2669024.2669029 |
| G3 | SEARCH | UDK Gems, Interior Mapping | https://docs.unrealengine.com/udk/Three/DevelopmentKitGemsInteriorMapping.html |
| G4 | SEARCH | Automaton, "Are there really rooms behind every window in Marvel's Spider-Man?" (2023-12-01) | https://automaton-media.com/en/news/20231201-23558/ |
| G5 | SEARCH | Lagarde & Zanuttini, "Local image-based lighting with parallax-corrected cubemaps", SIGGRAPH 2012; Lagarde blog 2012-09-29 | https://history.siggraph.org/?p=140207 ; https://seblagarde.wordpress.com/2012/09/29/image-based-lighting-approaches-and-parallax-corrected-cubemap/ |
| G6 | SEARCH | Unity Manual, Reflection Probe; ScriptReference ReflectionProbe | https://docs.unity3d.com/Manual/class-ReflectionProbe.html ; https://docs.unity3d.com/ScriptReference/ReflectionProbe.html |
| G7 | SEARCH | Impostor baker for UE4 (80.lv), after Ryan Brucks' octahedral impostors (shaderbits.com, 2018) | https://80.lv/articles/impostor-baker-for-ue4/ |
| G8 | SEARCH | Live Science, "How does a one-way mirror work?"; HowStuffWorks | https://www.livescience.com/32280-how-does-a-one-way-mirror-work.html ; https://science.howstuffworks.com/question421.htm |
| G9 | DOC | Valve Developer Community, WiseOneWayGlass | https://developer.valvesoftware.com/wiki/WiseOneWayGlass |
| L1 | WIKI / SEARCH | DOORS Wiki, List of Entities ("Window") | https://doors-game.fandom.com/wiki/List_of_Entities |
| L2 | SEARCH | Favel Studio, The Backrooms Game 0.2 devlog | https://favelstudio.itch.io/the-backrooms-game/devlog/809626/the-backrooms-game-02-update |
| L3 | SEARCH | Backrooms Doors (Steam) | https://store.steampowered.com/app/2526580 |
| L4 | SEARCH | Godot Forum, "Doors/Pressure (from roblox) room generation" | https://forum.godotengine.org/t/doors-pressure-from-roblox-room-generation/76896 |
| M1 | PRESS | Stacie Ponder, Kotaku, "Resident Evil's window dogs set the standard…" (2018-04-04) | https://kotaku.com/resident-evils-window-dogs-set-the-standard-for-video-g-1824258916 |
| M2 | PRESS | Destructoid, "Fangs for the Memories: Two Way Mirror of Terror" (2014-10-28) | https://www.destructoid.com/fangs-for-the-memories-two-way-mirror-of-terror/ |
| M3 | PRESS | Tommy Thompson, "The Perfect Organism: The AI of Alien: Isolation", Game Developer (2017-10-31) | https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation |
| M4 | SEARCH | Epic, AI Perception; Unreal forum "Perception - Sight" (sight collision channel) | https://dev.epicgames.com/documentation/en-us/unreal-engine/ai-perception-in-unreal-engine ; https://forums.unrealengine.com/t/perception-sight/343797 |
| M5 | SEARCH | Giant Bomb Outlast user review; Game Informer PAX East 2013 preview | https://www.giantbomb.com/outlast/3030-40089/user-reviews/2200-27564/ ; https://www.gameinformer.com/games/outlast/b/pc/archive/2013/03/24/outlast-offers-genuine-scares-at-pax-east.aspx |

Our code read for §1 and §4 [CODE]: `Assets/Scripts/FrontRoomsMap/FrontRoomsMap.cs`, `FrontRoomsMapWorld.cs`, `FrontRoomsLevelProfile.cs`, `FrontRoomsMapHunter.cs`, `FrontRoomsMapHunter.V2.cs`, `Assets/Scripts/FrontRoomsRoomStream.cs`, `Assets/Scripts/FrontRooms3DGame.cs`, `Assets/Scripts/Rendering/FrontRoomsZoneReflection.cs`, `Rendering/GlassRT/FrontRoomsGlassRTSystem.cs`, `Rendering/FrontRoomsDoubleDoorKit.cs`, `Rendering/Doors/FrontRoomsDoorRig.cs`, `Assets/Settings/FrontRooms_URP.asset`, `FrontRooms_URP_Renderer.asset`.
