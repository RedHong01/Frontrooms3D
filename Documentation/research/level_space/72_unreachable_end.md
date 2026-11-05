# Level space · 72 · The unreachable end: how games deny the far door, and five treatments

Date: 2026-11-04. Line: 夸张结构调研与设计 (`level_space`). Status: FINISHED 2026-11-04; **BUILT IN SCENE 4 20:11** (Red 13:2x: "把每一个都在场景4里做出来看看"; §7); **RED PICKED #3 光走了 (20:2x, "我喜欢第三个，以这个去深化", with the last frame of #3's film): deepening it (§8 to come).** Placed in `research/level_space` by the line's session on login A from the drafts in `~/FrontRoomsVisualWork/level_space/drafts/run/` (`unreachable_72_draft.md`, `unreachable_72_zh.md`); §6 is the Chinese page for Red, unchanged. Written by login A (session `local_138b3879`) as the synthesis stage of the workflow login G started (cut by its plan limit at 10:01) and login B continued (cut at 12:31).

**Red's ask, 2026-11-04, verbatim** (a queued message in this line's chat, origin human, 16:45:11Z, transcript `7da6f3fe`; checked under rule 15 by this stage):

> 1. 你说的《小小梦魇》"隧道"是哪一作、哪一段；这个我不知道，但是目前我感觉不像是那种经典的隧道变远的效果，我需要你给我research所有有这种 design的游戏对应的cutscne/playable level都可以，然后根据他们的做法，总结出五个不同处理方式的variation，让我检阅之后我告诉你对应的方向是什么

**What it answers, 2026-11-03** (voice, message 1, verbatim excerpts from `71` §1): "就比如说像《小小梦魇》那样的游戏，它就会做一些就是比如说像隧道啊，或者说永远到达不了的彼岸那样子的一个设计"; "如果玩家走到了它原本房间最初始长度的三分之一，哎，这个门它往后退了，它越退越远了"; "它不只是 motion transition， 它也是模型上面，就比如说它有一些，像是我们之前有专门做过一个门的那种可破坏的这种地形嘛，那它是否有一个就比如说重组或者向后退避的这样子的一个3D 的模型动画的这种".

**What Red already agreed** (`71` §0.1 and §8, "全部按推荐", 11-03 23:18): the receding variant only on deep floors (9 → 12 cells from T4 on B2 or deeper, 9 → 14 at T5 on B3 or deeper); it trips at the 9 m fire-door frame FD1 (a third of 27 m); the end recedes as a model, a T1 rolling head carrying a T2 re-stacking block wall (desktop and iPad; phones half-course strips; WebGL T1 alone), stepped (a two-cell flinch, one-cell steps, a heavy final seat, a 6 m/s keep-away glide); look B ALARM PULSE on the red tube at the brightest level (11-04); the white RESET re-strike Red likes stays; the goal door is never locked (rule C38), there is no EXIT sign (rule C41), nothing reads the Relay (rule 11). Scene 4's SP7 (`FrontRoomsStructuresRunRecede.cs`) builds that recede today.

**What was done.** Four research agents read the web and wrote four drafts (`unreachable_tarsier.md`: Little Nightmares, Tarsier and side view; `unreachable_horror.md`: first- and third-person horror; `unreachable_liminal.md`: Backrooms, liminal, puzzle and classics; `unreachable_technique.md`: how each effect is built, comfort and cost). One catalogue merged them (`unreachable_catalogue.md`: 80 game rows C1-C80, 9 films F1-F9, 7 technique references R1-R7, an 11-treatment taxonomy). Three independent proposals each chose five treatments for Run! from it (`unreachable_propose_player.md`, player first; `unreachable_propose_build.md`, build first; `unreachable_propose_meaning.md`, meaning first). This synthesis picks the final five and grafts the best of each angle onto them. Web reading only: no downloads, no Unity, no clone, no code; nothing built, rendered, measured or played aloud (rule 24).

**Confidence (rule 26: no fact-check or critic stage follows this page).** Every game claim comes from the catalogue, with its mark: a claim with no mark is CONFIRMED there (two or more independent sources); **ONE SOURCE**, **UNSURE** and **NOT FOUND** are written where they apply; **71 READ** means the claim is known only from the sources `71` §4.1 lists as read, which carry no mark of their own. A claim the catalogue does not hold was dropped, or is marked as this page's inference. Every time and distance is this page's arithmetic on the room's numbers (walk 3.2 m/s, sprint 5.5 m/s for a 5 s bar = 27.5 m, slow gait 1.44 m/s; trip at 9 m; door at 27 m; R 9 or 15 m); nothing was walked. **No stage watched a video**: how any precedent looks in motion is unverified.

**Naming.** C-numbers are catalogue rows. "Rule C38" (the goal door is never locked), "rule C41" (no EXIT sign) and "C42" (the goal-wall light) are FrontRooms rules; row C38 is Metal Gear Solid 4.

---

## 0. Short answer

**Little Nightmares.** The only Little Nightmares corridor in any source read that ends in a door the player cannot reach is **Little Nightmares II (2021), Mono's dream hallway and the eye door**: an opening dream, playable TV visits, the door opened on the third visit in Chapter 4 "The Pale City". It is not the classic stretch: Six pulls Mono out, he runs dream-slow (ONE SOURCE), and each visit brings the door closer (ONE SOURCE). Red's "永远到达不了的彼岸" fits two other beats better: Little Nightmares II's ending run, where the path breaks after Six crosses and she lets go of Mono's hand, and Little Nightmares III's mirror (§1).

**The five** run from "I watch the end move" to "the end never moves":

| # | Treatment | Kind (catalogue §2) | The player, in one line | Does the door back away? |
|---|---|---|---|---|
| 1 | **THE BUILDING TAKES IT BACK** · 机械后退 | SET | "I watch the building pull its end back on rails: the wall comes apart, the door rolls back lit, the wall rebuilds round it, step by step." | yes, in view, in steps; **this is today's SP7** |
| 2 | **THE FAR SHORE** · 彼岸漂走 (**REC**) | APART | "The far third of my corridor breaks off at its fire joint and sails away like a lit train leaving; I cross the raw gap it leaves." | yes: a whole lit compartment leaves, faster than a sprint |
| 3 | **THE LIGHT LEAVES** · 光走了 | DARK + FAR | "The lamps ahead die one by one; the door's light slides away into the black; then a red runway lights to the door." | yes, seen only as a light in the dark |
| 4 | **DON'T LOOK BACK** · 别回头 | PERCEPT + FLEES | "It never moves while I watch it; each time I look back at the hunter, the door is farther when I turn round." | yes, but never while seen |
| 5 | **THE DREAM RUN** · 梦里跑不动 | PULLED + SLOWED | "The door never moves; the floor runs back like a down escalator; I sprint and barely gain." | **no**: Little Nightmares II's own way |

- **Closest to today's SP7 mechanical recede:** #1 is SP7 as built (71's M1, which the catalogue files under SET). #2 is its nearest relative: SP7's head and trigger, widened to a whole compartment.
- **The classic stretch:** none of the five. The Poltergeist stretch (the hall deepens while the door stays centred and lit) is rare in games, never the main denial, and not what Little Nightmares does; in first person its lens version reads as a burst of speed and is an accessibility setting (§2.2, STRETCH; §4.3). #1 is the nearest to it: it gives the classic's result (the door gets farther in view) without the classic's look. If Red's "不像…经典的隧道变远" was a reaction to SP7, #1 is the option most at risk. A minutes-long reference mode in SP7 can show the classic so Red can rule it out or in (§4.2).
- **Recommendation: #2 THE FAR SHORE** (all three proposals chose it independently). It is Red's 彼岸 in one image, meets 往后退, 越退越远 and the model animation at full strength, keeps every agreed number and rule, and grows out of SP7. **Its honest flaw:** it is the least proven and the most expensive: no game does it in a first-person run (its precedents, ICO's bridge and the endings of Little Nightmares II and Kingdom Hearts, are story climaxes), it gives up the agreed steps and T2's re-stack for one continuous departure, and its raw gap may count as changing the room under Red's G1/G2 rule. Runner-up: #3, cheaper than today's SP7 and built on the lamp chase Red likes (§4).

---

## 1. Red's question: which Little Nightmares "tunnel"

From catalogue §3 (row C30, built on the tarsier draft's ten pages for this scene).

**The scene.** Little Nightmares II (2021, Tarsier Studios; Bandai Namco): **Mono's dream hallway and the eye door.**
- **The opening dream** (cutscene, before Chapter 1 "The Wilderness"): a slow shot down a long, warped, blue-lit corridor to a door bearing an eye; Mono wakes before the door is shown up close.
- **The TV visits** (playable): Mono tunes a television and pushes through the screen into the hallway; the player runs at the door, dream-slow (ONE SOURCE), and Six pulls him out. One Steam guide places the denied visits in the Pale City apartments and the Hospital (Chapter 3); Gamepressure and Cultured Vultures confirm the Hospital visit. The number and places of the denied visits: UNSURE.
- **The third TV**, Chapter 4 "The Pale City" (playable): Mono runs up the hall, jumps for the handle and opens the door on the Thin Man in his chair, which lets the Thin Man out.
- **The ending**, Chapter 5 (cutscene): after Six lets him fall, Mono ends up in the room behind the door for good.
- Sources: https://en.wikipedia.org/wiki/Little_Nightmares_II ; https://www.gamepressure.com/little-nightmares-ii/first-floor/z5e286 ; https://confluence.gallatin.nyu.edu/context/independent-project/stagnant-dreams-isolation-and-surveillance-in-little-nightmares-ii (the NYU paper). No video found or watched.

**Is it the classic tunnel-getting-farther?** No. None of the pages read says the hall stretches or the door backs away. The door is kept from Mono by Six pulling him out (PULLED), by slow-motion dream running (SLOWED; ONE SOURCE) and by a cut out of the broadcast; across the visits the door comes **closer** (ONE SOURCE, the NYU paper), the inverse of a stretch (PROMISE). The only geometric touch is a still warp in the opening shot (a blocked guide's snippet says tuning the TV straightened it; UNSURE). Red's sense, "不像是那种经典的隧道变远的效果", matches the sources: nothing in the Little Nightmares cluster uses the classic stretch or a dolly zoom.

**If what Red remembers is "永远到达不了的彼岸" rather than a corridor,** two other beats fit those words better:
- **Little Nightmares II's ending run** (C14, end of Chapter 5, the Signal Tower collapsing; a playable run, then a cutscene): chased by the tower's flesh mass, the narrow path breaks after Six crosses; Mono jumps; Six catches his hand, holds him a moment and lets go. He never reaches the far side. https://www.gamepressure.com/little-nightmares-ii/ending/zce2a4
- **Little Nightmares III** (2025, Supermassive Games; C24): the window of daylight in a mirror, dreamed from the first minute; Low pulls Alone through and she is thrown out as the mirror breaks (how the shattering is staged: UNSURE). https://en.wikipedia.org/wiki/Little_Nightmares_III

Little Nightmares (2017), its DLC and Very Little Nightmares have nothing of the kind in any source read; the first game's ending is a far shore that **is** reached (C74, ONE SOURCE).

**Confidence.** The hallway, the eye door, the TV visits, Six pulling Mono out, the reach on the third visit and the ending room: CONFIRMED (five or more sources). The slow-motion running and "closer and closer": ONE SOURCE each. That this is the scene behind Red's "隧道": UNSURE (Red does not remember the entry or the segment, and no page ties the word to a scene). How the camera frames the TV visits: unknown (the Little Nightmares Fandom wiki and Heroes Wiki returned HTTP 402; TV Tropes, GameRevolution, Twinfinite and GameSpot 403).

**Where the five use it.** #5 carries C30's mechanism (the door never moves; the runner is held back). #2 carries C14's image (the way to the far side breaks). C30's structure across visits (PROMISE: the same far door, nearer each time, reached late) can be layered on any of the five (§4.4).

---

## 2. What games do: the taxonomy and its strongest instances

One axis: **what keeps the runner from the goal, as the player feels it.** Eleven treatments in five families: the world moves in view (STRETCH, SET, APART); the goal acts (FLEES, REFUSED); the runner loses (PULLED, SLOWED); the space repeats or is too long (LOOP, FAR); the eye is denied or misled (DARK, PERCEPT). Two extra tags are structures, not treatments: PROMISE (the same far goal shown again and again, reached late) and REACHED (a contrast: the far shore reached plainly). **Only STRETCH is the classic** (catalogue §2).

### 2.1 Counts (catalogue §2, over 80 game rows)

| treatment | classic? | main | secondary | rows carrying it |
|---|---|---|---|---|
| STRETCH: the stretch | **CLASSIC** | 4 | 1 | 5 (+ films F1-F3) |
| SET: the set moves | no (the classic's result, not its optics) | 7 | 2 | 9 |
| APART: the way pulls apart | no | 3 | 0 | 3 |
| FLEES: the goal flees | no | 4 | 3 | 7 |
| REFUSED: denied at arrival, or a false exit | no | 11 | 11 | 22 |
| PULLED: pulled back, progress taken away | no | 7 | 6 | 13 |
| SLOWED: the runner slowed | no | 3 | 3 | 6 |
| LOOP: the hidden loop | no | 23 | 3 | 26 |
| FAR: too far, or infinity shown | no | 5 | 3 | 8 |
| DARK: darkness swallows | no | 3 | 7 | 10 |
| PERCEPT: perception only | no | 3 | 7 | 10 |
| PROMISE (a structure) | — | 0 | 8 | 8 |
| REACHED (a contrast) | — | 3 | 1 | 4 |
| none found (checked, nothing of the kind) | — | 4 | — | 4 |

### 2.2 The strongest instances, and how they were built

| treatment | games and segments (row; mark) | how built (as the sources say) | link |
|---|---|---|---|
| **STRETCH** (CLASSIC): the space to the goal deepens in one continuous move while the goal stays centred, lit and whole | Poltergeist (1982 film), Diane's run to the children's door (F1; the definition). Max Payne (2001, Remedy), Part 2 Prologue nightmare: the hallway stretches into a thin blood trail over a void (C1; the clearest game case, third person). The Path (2009, Tale of Tales), Grandmother's house: corridors that extend when entered (C2; ONE SOURCE). Amnesia: The Dark Descent (2010), an early hallway (C4; UNSURE) | film: a dolly zoom (the direction is disputed between sources; only dolly in plus zoom out makes the door farther). Games: geometry that lengthens in view or on entry; a lens version through FOV scripts (Amnesia's HPL2 `FadePlayerFOVMulTo`, CONFIRMED; Thomas Grip set its distortion below what felt best, ONE SOURCE) | https://www.ludo.guide/guide/max-payne/max-payne-part-12 ; https://en.wikipedia.org/wiki/Dolly_zoom |
| **SET**: real architecture moves in view; the lens and the runner's footing stay true | Control (2019, Remedy), the Ashtray Maze: walls fold and floors rise around a one-way path (C5; 71 READ; the GDC talk's scope ONE SOURCE). Dishonored 2 (2016, Arkane), the Clockwork Mansion (C8; 71 READ). Portal 2 (2011, Valve), panels on robotic arms (C9; 71 READ). Silent Hill 3 (2003), one wall moves, once, in a red-glow chase (C6; 71 READ). The Devil in Me (2022, Supermassive), walls run by a killer at a console (C7). LSD: Dream Emulator (1998), the Long Hallway: the only game row where the end itself comes apart in view (C11; ONE SOURCE) | modular, heavily animated pieces; one prime mover with the AI valid before, during and after (Dishonored 2); big moves only where the player's position is certain, in view (Portal 2 cut crushers that hit players from outside their view); disorientation in what moves, never in where to go (Control) | https://80.lv/articles/creating-control-s-ashtray-maze/ ; https://www.gamedeveloper.com/design/level-design-deep-dive-i-dishonored-2-s-i-clockwork-mansion ; https://theportalwiki.com/wiki/Portal_2_developer_commentary |
| **APART**: the ground between splits and drifts; both sides stay whole and lit | ICO (2001, Team Ico), the main gate and the stone bridge: the gate opens, the bridge splits and Yorda's half drifts back, the gap widening by the second; control comes back with no cut, and the player may still jump (C12). Kingdom Hearts (2002, Square), the ending (C13; cutscene). Little Nightmares II, the ending run (C14) | no build source in any draft; in the technique draft's terms, SET applied to the floor ahead. All three are story climaxes, two of them endings | https://gameinformer.com/b/features/archive/2014/02/04/moments-the-bridge-scene-in-ico.aspx ; https://www.khwiki.com/Kingdom_Hearts_(game) |
| **FLEES**: the goal is an agent or a light that moves away | Dark Souls (2011, FromSoftware), Dark Sun Gwyndolin's hall: he teleports farther each time you close in, pillars give cover between jumps, he stops at the far end (C15). Braid (2008), the final level: the princess was fleeing (C16). Silent Hill (1999), Cheryl in the fog (C17). NaissanceE (2014), a light that leaves (C70; ONE SOURCE) | a fixed number of jumps, cover between them, and a stop (Gwyndolin); a reversal that re-reads the run (Braid) | https://darksouls.wiki.fextralife.com/Dark+Sun+Gwyndolin ; https://en.wikipedia.org/wiki/Braid_(video_game) |
| **REFUSED**: the goal is reached, or nearly, and refuses | Little Nightmares II, Chapter 5: the Signal Tower's middle door slams in front of you while the right door opens (C19). Hello Neighbor (2017): the three-lock door in Act 1, open in Act 3 (C20; Act 3 ONE SOURCE). The Exit 8 (2023): the counterfeit staircase (C41). Bendy and the Ink Machine (2017), Chapter 1: the floor gives way at the exit (C22). Little Nightmares II's eye door, opened on the Thin Man (C30 c) | a door state, a collapse or a swap: trivial in cost. Paired with a way on in the same beat (C19) or paid off later (C20, C30) | https://www.gamepressure.com/little-nightmares-ii/door-puzzles/zae2a2 ; https://en.wikipedia.org/wiki/The_Exit_8 |
| **PULLED**: the runner's ground is taken; the input points at the goal and loses | Little Nightmares II's dream hallway (C30). Shadow of the Colossus (2005), the finale: the player pushes away from a pull of light and loses (C31; the struggle CONFIRMED, its detail UNSURE). Outlast 2 (2017), "Inner Demon": the exit is a dead end and Blake is sent back (C32; the demon's throw ONE SOURCE). Portal's revoked surface (C34; ONE SOURCE) | a drag against the input (comfort moderate: the view moves without the player); a blink or a cut back to the start | https://en.wikipedia.org/wiki/Shadow_of_the_Colossus ; https://www.gamepressure.com/outlast-2/inner-demon/z49b54 |
| **SLOWED**: the runner's own body fails | Journey (2012), the last climb in wind and snow toward the light seen all game (C37). Metal Gear Solid 4 (2008), Act 5, the microwave hallway (row C38). Unravel (2016), the thread runs out (C39). Little Nightmares II, the slow dream run (C30; ONE SOURCE) | a speed multiplier, or a rising input cost near the end; it works for seconds, not minutes (Unravel's backtracking and Neverending Nightmares were criticised for it) | https://en.wikipedia.org/wiki/Journey_(2012_video_game) ; https://www.metalgearinformer.com/great-moments-in-metal-gear-the-microwave-hallway/ |
| **LOOP**: a hidden return to the start; nothing recedes | P.T. (2014), the L-shaped hallway (C40). The Exit 8 (C41). Super Mario 64 (1996), the Endless Stairs (C43). Little Nightmares II, the Signal Tower's doors, found by music (C51). Batman: Arkham Asylum (2009), the morgue (C46) | a silent warp by one module with the seam in a door or a turn (Mario 64's warp distance: ONE SOURCE); fog hides the top; one readable change per pass | https://www.mariowiki.com/Endless_stairs ; https://www.gamedeveloper.com/design/p-t-silent-hills-teaser-game-analysis |
| **FAR**: absurdly far but real, or infinity shown | The Neverhood (1996), the Hall of Records (C64). Portal (2007), facing portals (C66). Manifold Garden (2019) (C65). The Backrooms canon's Level ! "Run For Your Life" and its fan game: a never-ending, red-lit hallway (C63; ONE SOURCE each) | real length; portal recursion through the stencil buffer (capped at nine levels, ONE SOURCE): the most expensive technique on phones and WebGL; GPU instancing to the view distance (Manifold Garden) | https://theportalwiki.com/wiki/Portal_developer_commentary ; https://nerdist.com/article/the-8-scariest-levels-of-the-backrooms-creepypasta/ |
| **DARK**: the end withheld by darkness, fog or lights going out | Silent Hill 2 (2001; remake 2024), the stair down under the Historical Society (C68). A F.E.A.R. title, Interval 03 "Terminus": lights go out in sequence while Alma comes closer (C69; ONE SOURCE; which title not verified). NaissanceE (C70; ONE SOURCE). Super Mario 64's fog (C43) | fog and fewer lights are cheap everywhere; duration and sound do the work the stretch does in film. Photosafety (XAG 118): more than three flashes a second over 20% of the screen fails; saturated red fails sooner | https://www.theodysseyonline.com/silent-hill-2-level-design ; https://en.wikipedia.org/wiki/NaissanceE |
| **PERCEPT**: nothing moves that the runner can catch | Layers of Fear (2016, Bloober Team): the house changes behind the player once light or sound has drawn the eye (C71). Antichamber (2013): turn around and a new path has formed (C72; the mechanism NOT FOUND). Superliminal (2019/2020), Chapter 7 Labyrinth (C53). The Evil Within 2 (2017), Chapter 2: a painted door (C73; ONE SOURCE) | out-of-view swaps need the player to look away, and a sprinter looks at the door, so a swap can only change what lies behind; forced perspective placed by raycasts (Superliminal) | https://gamedeveloper.com/audio/terrifying-players-with-unstable-level-design-in-i-layers-of-fear-i- ; https://en.wikipedia.org/wiki/Antichamber |
| PROMISE (a structure) | Little Nightmares II's eye door, closer on each visit, reached on the third (C30; "closer" ONE SOURCE). Hello Neighbor's door, Act 1 to Act 3 (C20). Journey's mountain (C37). Inside's diorama (C75) | the same far goal shown again and again: denied, nearer, reached late | https://en.wikipedia.org/wiki/Inside_(video_game) |
| REACHED (a contrast) | Little Nightmares (2017), the walk up into sunlight (C74; ONE SOURCE). Inside (2016), the shore (C75). Escape the Backrooms (2022), Level ! "Run For Your Life!": a finite red-lit run to a door that is reached (C76) | nothing recedes; the quiet, lit arrival after the longest run | https://itemlevel.net/escape-the-backrooms-level-run-for-your-life-guide/ |

### 2.3 What the research found, in three lines

- **The central gap.** No source in any draft shows a first-person game in which a goal door recedes in view, by lens or by set, while the player runs at it (catalogue §4). The nearest are a film (Poltergeist), a maze with no chase (Control), one moving wall in a chase (Silent Hill 3), a third-person stretch with no chaser (Max Payne) and an end room coming apart (LSD, ONE SOURCE). So #1 and #2 are new and need the walk; #3, #4 and #5 use denials that have shipped in first person (#3: F.E.A.R.'s hall, NaissanceE; #4: Layers of Fear, Antichamber; #5: Outlast 2's corridor, LSD's hallway).
- **What games use most** is LOOP (26 rows) and REFUSED (22). LOOP was ruled out for Run! by Red's 11-03 agreement (71 §4.2, M2: the trip has sent the Relay here, and the goal door would stop being the escape verb). REFUSED inside a room whose door is never locked becomes "the door opens on more corridor"; it is set aside in §4.3 with its reasons.
- **Where 71 and SP7 sit.** 71's M1 is named "THE STRETCH" but is built as the end backing away on a track: SET, the classic's result without its lens. SP7's rolling head carrying the re-stacking wall is SET with its mechanism in view, and no game precedent for it was found (catalogue §2.13).

---

## 3. The five treatments

Chosen to be as different as possible in what the player meets, one per kind of denial the research found, from "the end moves in plain view" (#1, #2) through "it moves as a light in the dark" (#3) and "it moves only unseen" (#4) to "it never moves; the runner is held back" (#5). Each section: the player's beats from the 9 m trigger; the games it comes from; how it sits in our Run! room; the build; the cost on desktop, iPad, iPhone and WebGL; the risks (photosafety, motion comfort, Relay navigation, rule C38, agreed rules it bends); and what scene 4's SP7 could show. Where the three proposals differed, the choice made here is said. Numbers proposed by this page and not agreed are listed in §5.

### 3.0 What every treatment builds against (facts, with paths)

- **The room.** Run! B's service corridor: 2.84 m clear, 2.9 m high, 3 m cells; R-B strips on the soffit centre line (z = 1.8 + 3k) with the red tube; T-A twin-heads and T-B remote heads every 6 m; fire-door frames FD1 at 9 m and FD2 at 18 m (open leaves on hold-open magnets); the goal door RN-F with its F5 lite, lit by the goal head and C42, never locked (rule C38); no EXIT sign (rule C41). The receding variant is planned at 12 or 14 cells; the end stands at 27 m; R = 9 m (T4, B2 or deeper) or 15 m (T5, B3 or deeper); never a run's first Run! (71 §0.1).
- **The player.** Walk 3.2 m/s; sprint 5.5 m/s for a 5 s bar (27.5 m), refilling after a 1 s breather (`FrontRooms3DGame.cs`:408); the slow gait 1.44 m/s. Movement follows the view's yaw and the sprint runs forward only, so a look back in mid-sprint already costs ground (player proposal, from the same file). From the trip with a full bar: the 27 m door in 3.3 s; a 36 m door in 4.9 s with 0.1 s of bar; a 42 m door in about 6.7 s, the breath ending 5.4 m short. A walker: 5.6, 8.4 and 10.3 s.
- **The Relay.** Called from Away it arrives no sooner than 19.7 s after the trip; it chases at 4.4 m/s; it never arrives from ahead; nothing may read it (rule 11). Its field runs over built 3 m cells (`FrontRoomsMapPathField.cs`). A chase dive into SP7's recede costs about 2-5 m of lead against a plain room (71 §4.7; 系统设计's bot round comes first).
- **Look B and the RESET** (agreed). At the trip a dark band runs from the entrance at 20 m/s and leaves the corridor crimson; slow dark pulses run to the door every 2.5 s from the later of 3.0 s after the trip and 1.0 s after the final seat, 12 at most; the goal head and C42 never change. The RESET Red likes: the white re-strike walks from the entrance at 0.15 s a cell.
- **Light budget.** One URP renderer in Forward+ (`Assets/Settings/FrontRooms_URP_Renderer.asset`, `m_RenderingMode: 2`), so URP's per-object additional-light limit does not bind; the per-camera cap does (32 on mobile and WebGL, the technique draft's reading of Unity's manual; the URP version was not checked). Run! B's WebGL tier culls lights beyond 10 m, cap 31 (`room_visuals/run_b_production/10_plan.md`), so on WebGL anything farther than 10 m is lit by its own emissive and probes only, as Run! B's far door already is.
- **The head.** The build proposal corrects 71 §5.1's estimate of SP7's head (8-10k tris LOD0): the twin-head unit alone is 10,128 / 3,266 tris at LOD0 / LOD1 (`room_visuals/run_b_production/20_build.md`), so the head is nearer **14-16k tris LOD0, about 5k LOD1** (estimate). The figures below use the corrected one.
- **Comfort and flashing.** The game has a Camera Motion % setting and Reduce Flashing (`FrontRoomsSettings.cs`); no FOV slider was found. Photosafety is XAG 118's (a flash is a 10% luminance change; more than three a second over 20% of the screen fails; saturated red fails at a smaller change). Red's 11-04 pick put the red tube at the brightest level, so every red change in view needs the Harding FPA or PEAT capture with that tube.
- **Parity.** The same timing and length on desktop, iPhone and iPad; only model detail scales (71 §4.8 item 12); WebGL never pulls desktop down.
- **71 §4.8's fairness rules** (agreed) that the five bend are named by number: 6 one recede, at one moment, the same way every time; 7 the goal is the constant; 8 everything that moves is ahead of the player and outside the walk volume, and nothing can slow, trip, trap or crush; 9 no side door, branch or false door; 11 no cut, no teleport, no FOV trick.
- **Scene 4.** SP7's keys are V (variant), B (body), L (look), H (red tube), O (entrance), F (Reduce Flashing), T (trip / reset) and Y (head back) (`FrontRoomsStructuresRunRecede.cs`:1833-1881). The host also routes **N** to districts and SP7 does not use it (`FrontRoomsStructures.cs`:309), so one key can cycle the treatments without touching the host. Scene 4 has a static Relay stand-in and no sound, so chase fairness and sound are judged elsewhere (bots in the game; 声音设计).

### 3.1 · THE BUILDING TAKES IT BACK · 机械后退 · SET (today's SP7)

**In one line.** I watch the building pull its end back on rails: the wall comes apart, the door rolls back lit and upright, the wall rebuilds round it, step by step, and stops.

**Where it sits.** SET: real architecture moves in plain view (catalogue §2.2). **This is today's SP7**, 71's M1 as agreed on 11-03. It is not the classic: there is no lens and the hall's perspective never stretches; it gives the classic's result (the door gets farther in view, centred and lit). Its meaning, in the meaning proposal's words: indifference, a procedure you happen to be inside, the same every time whoever is inside.

**As the player feels it** (the T2 body; 71 §4.5):
- *Before:* two steel track lines in the soffit corners run from FD1 to the end wall: "this end runs on rails", readable from the arch.
- *t 0, crossing FD1:* the trip. Two magnet clacks at the end; the end jolts 15 mm toward the player; grit falls from the soffit seam and glows in the goal lamp; look B's dark band leaves the entrance.
- *0.25-1.55 s, the flinch:* the block wall lifts out course by course and streams back along both walls and the soffit; the door rolls back two cells on its subframe, lit and upright; the blocks drop back into courses round it.
- *Then:* one-cell steps and a slower, heavy final seat. R 15 seats at 1.55, 2.90, 4.25 and 6.05 s (door at 42 m); R 9 at 1.55 and 3.35 s (door at 36 m). Whenever the player is within 12 m of the head between steps, the rest runs as one 6 m/s glide with the wall riding back whole.
- *Sprinter:* the gap never grows (about 15.5 m at the flinch's seat); the door at 4.9 s with 0.1 s of bar (R 9) or about 6.7 s with the breath ending 5.4 m short (R 15).
- *Walker:* the gap grows only in the flinch (18 → 19.7 m), then closes (16.3 m at R 9's last seat, 13.6 m at R 15's). *Slow gait:* it grows all the way (18 → 24.3 m at R 15).
- *After:* B's pulses; ghost bands of cleaner paint and unworn wax mark every line the end stood on; at the reset the white re-strike walks the whole new length.
- *Feel:* mechanical menace, the building as the opponent; the best readability and fairness of the five (the agreed numbers); moderate longing (the door is watched from 15-18 m); the least 彼岸.

**The games it comes from.**
- C5 Control (2019, Remedy), the Ashtray Maze: walls fold and floors rise around a one-way path in time with a song; it began as a gate to block progress (moving architecture in view: 71 READ; the GDC 2020 talk's scope ONE SOURCE). https://80.lv/articles/creating-control-s-ashtray-maze/
- C8 Dishonored 2 (2016, Arkane), the Clockwork Mansion: levers transform whole rooms; one prime mover; the AI works in both configurations and while rooms move (71 READ). https://www.gamedeveloper.com/design/level-design-deep-dive-i-dishonored-2-s-i-clockwork-mansion
- C9 Portal 2 (2011, Valve): panels on arms rebuild chambers only where the player's position is certain, in view; crushers were cut as too punishing (71 READ). https://theportalwiki.com/wiki/Portal_2_developer_commentary
- C6 Silent Hill 3 (2003, Team Silent): in a red-glow chase one wall moves, once, on a fixed route (71 READ). https://www.silenthillmemories.net/sh3/walkthrough_13_lakeside_amusement_park_en.htm
- C7 The Dark Pictures Anthology: The Devil in Me (2022, Supermassive): moving walls in a replica murder hotel, run by a killer from a control room: a machine with an author. https://www.supermassivegames.com/games/the-devil-in-me
- C11 LSD: Dream Emulator (1998), the Long Hallway: at the end a room-like object comes apart into blue sky (ONE SOURCE). https://dreamemulator.fandom.com/wiki/Long_Hallway
- Films and hardware (71 READ): F4 The Haunting (real wood under impossible force), F6 Inception (the lights ride the moving set), R7 overhead trackways and hold-open magnets of 1977-1990.

**In our Run! room.** As agreed and as SP7 stands: corridor unchanged; the head (block-faced end wall, RN-F door with its F5 lite and panic bar, the carried twin-head on a short bracket, C42 in a box behind the lite) on the soffit-corner track and a floor slot; the trip plane at FD1; the gaze gate (the first step waits until the camera faces within 35° of the goal, or the player reaches 18 m); FD2 open; the revealed strips strike once in red; pulses after the final seat. The Relay is never in the hidden length and joins each cell at its seat. The bar: one full bar reaches a 12-cell door; a 14-cell door is the late band.

**Build.** Built in scene 4. For the game, as 71 §5 and §9's cards: 游戏视觉's `RunRigState.Recede` (one timeline from one trigger time), the head, T2's bakes (about 90 block pieces, two rigid-body bakes, merged back to the slab at each seat); 灯具适配's carried twin-head and hidden strips; 关卡设计's reserved length, hidden cells, graph per seat and the record; events for 声音设计.

**Cost** (71 §5, with §3.0's correction; estimates).
- Desktop: head about 14-16k tris LOD0, about 8 draws (2-4 more than a plain room), textures ≤ 3-5 MB; T2 in flight +5.4k tris and +1-3 draws, bakes about 100-120 KB a clip; GPU grit.
- iPad: as desktop (T2 per decision 5), without grit.
- iPhone: the head at LOD1 (about 5k), T2 as half-course strips (about 28 pieces), no grit.
- WebGL: T1 alone (the whole end rolls); lights beyond 10 m culled by the tier.
- No new lights (each revealed strip is its cell's own); CPU not measurable.

**Risks.**
- *Photosafety:* the band, a red strike per seat, the pulse, and T2's field of about 90 like blocks moving together (a moving regular pattern): the capture with the brighter tube is the gate.
- *Motion comfort:* low; no camera change; the fastest thing on screen moves away from the player (the 6 m/s glide; T1's flinch peaks near 10 m/s, 71 §4.5).
- *Relay navigation:* off its graph in the hidden length, edges join per seat; a chase dive costs 2-5 m of lead (bots first).
- *Rule C38:* held: the keep-away glide means nobody catches the door moving.
- *Red's 11-04 line:* if "不像…经典的隧道变远" was a reaction to this recede (catalogue §3, second reading), this is the option most at risk, because it is the closest of the five to the classic's result.
- *Other:* T2 has no visible machine (the blocks move themselves), so it reads supernatural while its meaning claims hardware; a sprinter sees the re-stack only in the flinch, then a whole-wall glide; the spectacle happens 15-18 m away, impressive and impersonal.

**Scene 4.** Built: SP7 opens on 9 → 14, look B, T2 (V, B, L switch the variants). It is the baseline the other four are walked against. The classic reference (§4.2) is a speed change of its glide.

### 3.2 · THE FAR SHORE · 彼岸漂走 · APART (the recommendation)

**In one line.** The far third of my corridor breaks off at its fire joint, door and lamps and all, and sails away like a lit train leaving the platform; I cross the raw gap it leaves to reach it.

**Where it sits.** APART: the way between the runner and the goal splits and drifts in view while both sides stay whole and lit (catalogue §2.3), carried by SET's machinery. Not the classic: the corridor never stretches; it parts. The catalogue found APART only in story climaxes (C12-C14); this puts it into a playable first-person run. It is #1's nearest relative (SP7's head and trigger, widened to a whole compartment), but it is felt differently: in #1 the end is hauled back; here a lit place leaves, and the player has to cross the no-man's land after it. Its meaning: longing; the far shore is real and lit, and the way to it is taken away. All three proposals recommended it (as THE FAR SHORE DRIFTS, THE COMPARTMENT CASTS OFF and THE FAR SHORE). It is not 71 §4.2's M3 of the same name (a door that is never reached, with a side door lit instead, kept for a later payoff room): here the door is always reached.

**The structure** (the meaning proposal's reading). The Run! corridor is three 9 m fire compartments: FD1 at 9 m (a third: the trip Red set), FD2 at 18 m (two thirds: the joint), the door at 27 m. The third compartment is one assembly, the island: FD2's frame with its open leaves, three cells with their strips and red tubes, the T-B head between them, and the door with its goal head and C42.

**As the player feels it** (R 15; the island's move proposed here, from the player proposal: a 0.25 s tell, 0.5 s up to 8 m/s, cruise, 0.5 s braking, a 0.35 s heavy seat):
- *Before, from the arch:* FD2 is not like FD1. A steel expansion-joint collar runs round walls, soffit and floor (a steel threshold plate about 200 mm wide across the floor); the soffit track starts there; the dry sprinkler main is joined at the collar by a fat grooved coupling (the build proposal's tell). "This corridor is two pieces."
- *t 0, crossing FD1:* the trip; look B's band leaves the entrance. Nine metres ahead at the collar, two hard clanks as its latches let go; the main's coupling parts with a hiss and a puff of rust; grey fire-stop wool pulls out of the joint in strands; the island jolts 15 mm away.
- *0.25-0.75 s:* the island starts to move as one piece; the seam opens into a dark slot. A sprinter closes in: the door is about 16 m away.
- *0.75-2.1 s:* it runs away at 8 m/s, faster than a sprint. The door opens out from about 16 to about 19 m away: the player is losing the place they were running into. Behind the island's mouth the raw shell appears: unpainted grey block, a bare slab with two guide rails, the deck and services exposed, caged work lights on a yellow cord already burning warm (uncovered, not switched on, so no flash) and swinging in the air the island pushed; T-B battery heads on the raw block throw amber pools. The band catches the island and turns it crimson as it goes.
- *1.6 s:* a sprinter reaches the old seam at 18 m and runs onto raw concrete, with about 9 m of raw corridor ahead and still growing.
- *2.1-3.0 s:* the island brakes and seats with a heavy knock, the collar's rubber sweeps pressed; the work lights swing and settle; grit falls through their light. The door is about 18.6 m away: **farther than at the trip**.
- *3.0-4.4 s:* 15 m of raw corridor under warm bulbs; over the threshold plate and through FD2's open leaves (a proscenium) back into crimson. *About 6.7 s:* the door at 42 m (the bar ends at 36.5 m; the last 5.5 m walked).
- *R 9:* the island's mouth seats at 27 m and the door at 36 m; a sprinter sees the door go from about 16 to 17.4 m away during the run and 16.7 m at the seat, and reaches it at 4.9 s with 0.1 s of bar.
- *Walker:* the island shrinks into the distance; the gap grows from 18 to about 21 m (R 9) or 24.6 m (R 15) and is still growing at the seat. *Slow gait:* about 29 m (R 15). Everyone sees it leave.
- *Safety:* the cruise outruns the sprint, so no runner comes within about 7 m of the moving mouth; nothing moves under the runner; the door is reached only on a seated island.
- *After:* B's pulses run on both shores and skip the gap, whose work lights are not on the alarm circuit ("the alarm lives on both shores, not on the way between", meaning proposal). At the RESET the white re-strike walks the near shore, crosses the warm gap and walks the island to the door. The gap stays raw for the rest of the floor: a scar.
- *Feel:* the strongest longing of the five (not a door but a lit room leaves; the departing train everyone has run for); high fear (the building comes apart along its own joints); high readability (one big, straight motion; the far shore whole, lit and centred); fairness as #1; comfort low to moderate.

**The games it comes from.**
- C12 ICO (2001, Team Ico / SCE Japan Studio), the main gate and the stone bridge, late game: the gate opens and freedom is in sight; halfway across, the bridge splits down its centre and Yorda's half drifts back, the gap widening by the second; the cutscene hands control back without a cut and the player can jump back. The emotional model: hope first, then the structure takes it back. https://gameinformer.com/b/features/archive/2014/02/04/moments-the-bridge-scene-in-ico.aspx ; https://destructoid.com/aaamaazing-ico-yorda-and-the-bridge
- C14 Little Nightmares II (2021, Tarsier), the ending run at the end of Chapter 5: the path breaks after Six crosses; Mono jumps; she catches his hand and lets go; he never reaches the far side. The series Red named, and the Little Nightmares beat that fits "彼岸" best (catalogue §3). https://www.gamepressure.com/little-nightmares-ii/ending/zce2a4
- C13 Kingdom Hearts (2002, Square), the ending: the ground splits as the worlds are rebuilt and Sora and Kairi are pulled apart across the darkness (cutscene; that Sora sprints over before the islands split: ONE SOURCE). https://www.thegamer.com/kingdom-hearts-heartbreaking-moments/
- C11 LSD: Dream Emulator (1998), the Long Hallway: the end comes apart in view (ONE SOURCE).
- C22 Bendy and the Ink Machine (2017), Chapter 1: the exit is reached and the floor collapses under Henry. The line this treatment never crosses: the floor under the runner stays. https://en.wikipedia.org/wiki/Bendy_and_the_Ink_Machine
- R7 jet bridges telescoping in nested tunnels (the lining sliding inside a raw shell) and F6 Inception (the lights ride the moving set) (71 READ).

**In our Run! room.**
- *Corridor:* compartments 1 and 2 (0-18 m) as built. The hidden length (27-42 m) and the ground under the island are built as the raw shell, its section 50 mm wider each side and its soffit higher than the finished lining (player proposal), so the gap is still the same corridor, unfinished, and never opens sideways onto a void or a plant room (Red's G1/G2 rule).
- *Floor:* solid and still under the runner everywhere (71 §4.1's Elm Street line: "the floor may move away, never slow or trap"; here the floor ahead moves away and the floor underfoot never moves).
- *Door:* rides the island's far end exactly as SP7's head does (RN-F, F5 lite, panic bar, carried goal head, C42 in its box); it stays the brightest thing and opens once seated (rule C38).
- *Lamps:* the island's three R-B strips (red tubes included) and its heads ride with it, so no lamp switches while it moves; the gap gets one caged work lamp per cell on its own always-on circuit and T-B heads every 6 m on the raw block (amber when tripped).
- *Look B:* the band runs from the entrance into the departing island; pulses after the seat on both shores, skipping the gap; the RESET as above.
- *Trigger:* FD1 at 9 m, as agreed. SP7's gaze gate is kept (35°), with its fallback moved from 18 m to 12 m (6 m short of the joint) so the seam never opens at the runner's feet (meaning proposal). The player proposal would drop the gate (a runner crossing FD1 almost always faces the door); scene 4 can show both.
- *Relay:* nothing reads it. The gap's cells join its graph as the island clears each one; the island's own cells are closed while it moves and join at the seat; it never comes from ahead. Crossing the gap it is lit by warm pools (an easier read than red, player proposal); its read at 2.05 m there, protan included, is to be measured.
- *Bar:* as #1 (R 9: one bar; R 15: the late band). No EXIT sign, no side door.
- *Fiction:* compartmentation taken literally; R.M.'s memo, already among the terminal strings ("Remodeling continues on several floors. Until it is finished, a floor may differ from its posted plan."; `research/narrative/70_terminal_strings.md` §6, terminal set 1, as `96_run_room_meaning.md` quotes it), explains the raw between without the building lying (meaning proposal; no new string).

**Build.**
- 游戏视觉 and 灯具适配: SP7's head extended to a three-cell kinematic assembly (FD2's collar and leaves, wall and soffit liners, a 10 mm steel floor plate carrying its VCT on the guide rails, three strips with their red tubes and Lights, the T-B head, the far-end head); one analytic curve from one trigger time; out of static batching while it moves, merged per material at the seat; its own box-projected probe; the toothed joint, about a dozen fire-stop wool strands as cloth cards with a two-pose blend, mortar grit on desktop.
- 关卡设计: the raw-shell cells (unpainted block, bare slab with rails, exposed deck, the main and tray carried on); the graph cell by cell; the record (the gap stays raw for the floor); the release side as SP7's.
- 叙事设计: the remodeling reading (no new string). 声音设计 (events only; nothing is played, rule 24): latch clanks, the coupling's hiss, wool tearing, trolley roll, the air's "whump", cord rattle, the seat's knock.

**Cost** (estimates; the proposals' ranges differ).
- Desktop: the island and head about 25-50k tris LOD0 moving (the meaning proposal says 25-40k; the build proposal 45-50k, counted from Run! B's kit table); +10-20 draws in flight over #1 if merged per material (60-90 unmerged), back to merged at the seat; the raw shell 5-8k tris and 3-4 draws; new textures ≤ 4-10 MB (or an existing concrete); 6-8 moving lights. The most expensive of the five.
- iPad: as desktop, with the strands as one card each and no grit; count visible lights against the 32 per camera.
- iPhone: the island at LOD1 (it is at least 7 m away while it moves); work lamps emissive plus one real light; no strands, no grit.
- WebGL: the island merged to a few draws at LOD2, textures ≤ 1024; beyond 10 m it is lit only by its own emissive and probes; no shadows on moving lights.
- CPU: one transform and its lights.

**Risks.**
- *Proof:* no game does this in a first-person run (the catalogue's central gap); its precedents are story climaxes, two of them cutscenes. Only walking it tells whether it works.
- *Red's G1/G2 rule:* the gap must read as the same corridor unfinished, in period (1977-1990 unpainted block, slab, caged incandescent lamps), never as missing textures or a void. Fallback: the gap in the finished kit under its heads only, which loses the torn read.
- *Agreed choices it changes:* decision 4's steps and decision 5's T2 re-stack give way to one continuous departure; its 重组 is the tearing joint at the start, readable at any speed because it happens first and in one place.
- *Photosafety:* the gap is uncovered, not struck: no flash; the island's lamp row moves smoothly away; look B's band and pulses as ever; the capture with the brighter tube.
- *Motion comfort:* low to moderate: a large object leaves at up to 8 m/s in the centre of the view (the GAG list names objects quickly moving away from the player, a VR trigger); keep the 0.5 s ramps and the motion dead straight; the walls beside the player never move. 6 m/s (the build and meaning proposals) is the tamer setting, but then the door gets farther from a sprinter by only 0.5 m a second while it runs and seats nearer than it started.
- *Relay navigation:* cell by cell as above; the dim gap bends the Run! rule "darkness sits between pools, never in pockets (dark is never cover)" (`room_visuals/03_run_research.md` §0.8), which the work lamps answer; a chase dive costs what #1 costs (2-5 m of lead; bots first).
- *Rule C38:* held: the island seats before anyone reaches it.
- *Fiction:* the uncanny moves into the building itself (`story.txt` E6, `96`), on deep floors only, where "the building lying" was reserved; 叙事设计 must accept it.
- *Engine:* the biggest moving assembly of the five: batching, probes and lights moving with it; the room crosses one chunk border (71 §4.6), which the island may cross while it moves, so it belongs to the Run! room, not to a chunk.

**Scene 4.** A third body (on B: T2 / T1 / APART) or a mode on N: parent cells 7-9's dress, strips and heads to one island root; a greybox raw-shell material for the hidden cells and the ground under the island, one work lamp per cell, T-B heads every 6 m; one continuous run instead of the steps; the collar on z 18. One to two days of 总控's structures workflow (estimate). Walk it beside #1; measure the visible-light count at the trip, mid-move and seat against the phone tier's 32; put the Relay stand-in in the gap.

### 3.3 · THE LIGHT LEAVES · 光走了 · DARK + FAR + FLEES

**In one line.** The lamps ahead die one by one; the door's light slides away into the black; my own light keeps one cell ahead of me; then a red runway lights to the door.

**Where it sits.** DARK (main): the way is withheld by darkness and the light that showed it leaves (catalogue §2.10); FAR (second): with nothing to count, the length reads endless; FLEES (second): the light that marks the goal moves away. Not the classic. The end moves as in #1's T1 body, but the player sees it only as a light in the dark. Its mechanism is Red's own image of the lamp chase Red likes, "一个一个灯光熄灭，一个一个灯光在亮" (71 §1, message 2), turned into the denial (player proposal).

**As the player feels it** (R 15; the door's glide as #2's: a 0.25 s tell, 0.5 s up to 8 m/s, cruise, braking, a heavy seat):
- *Before:* as #1: the soffit track is the tell that the end moves.
- *t 0, crossing FD1:* the mains drop. Behind the player (entrance to FD1) the red tubes strike and the warm pools rise. Ahead, the white strips go out one after another, a front running away toward the door at 12 m/s; in 1.5 s the corridor ahead is black. Only the door is lit: its goal head on the bracket, C42 in the lite.
- *0-0.25 s:* two clacks from the dark end.
- *0.25-2.6 s:* the door's light pulls away: the lit leaf, its frame and its warm pool on the floor glide back into the black. No walls, no lamps to count: it could be 20 m or 100. Even at a sprint the light grows smaller (the gap about 16 → 19 m).
- *While running:* each strip strikes red as the player comes within 1.5 m of passing under it and stays on behind them with its warm pool: they run in a moving pocket of red, and the floor just ahead is always lit.
- *About 2.6-3.0 s:* the light stops; a heavy thud in the dark.
- *About 3.0 s, the runway:* from the player's cell (about 25.5 m for a sprinter) to the door, the strips strike red one after another toward the door at 20 m/s (about 0.8 s for about 16.5 m). The corridor's real length appears, crimson, the door at its end: the airport approach lights' grammar (71 READ, [LS247]).
- *About 4.0 s:* B's pulses begin (1.0 s after the seat). *About 6.7 s:* the door at 42 m. R 9: the door at 36 m, reached at 4.9 s.
- *Walker:* the light pulls hard away (18 → 24.6 m); *slow gait:* about 29 m. "越退越远", in the dark.
- *After:* the white RESET walks the whole length from the entrance: the runway's white twin.
- *Feel:* high fear (darkness ahead, an unknown length, the hunter lit behind: you can see it coming but not where you are going); very high longing (the door is the only light in the world, leaving like a ship's light leaving harbour); readability low during the glide by design and total at the runway; fairness: the hunter's side stays lit and the floor ahead is lit one cell in front, the open question being hesitation; comfort: low risk.
- *A harsher variant, not chosen* (the build proposal's SWALLOWED): the band also takes the door's own light for about 2 s while the end moves unseen, and the lamps then find it farther. It breaks agreed rule 7 (the goal is the constant), so it is only a scene-4 comparison.

**The games it comes from.**
- C70 NaissanceE (2014, Limasse Five): keep up with a light that leaves, or be left in darkness (ONE SOURCE); its players' photosensitivity complaints are CONFIRMED, a warning for our own lamps. https://en.wikipedia.org/wiki/NaissanceE
- C69 a F.E.A.R. title, Interval 03 "Terminus" (F.E.A.R. 2005 or Extraction Point 2006: not verified): a long hall whose lights go out in sequence while Alma comes closer each time (ONE SOURCE). https://fear.fandom.com/wiki/List_of_Alma_Sightings
- C68 Silent Hill 2 (2001, Team Silent; remake 2024, Bloober Team): through a burnt breach under the Historical Society, a stair going down into darkness with no bottom in sight. https://www.theodysseyonline.com/silent-hill-2-level-design ; https://www.killscreen.com/basements-basement/
- C43 Super Mario 64 (1996, Nintendo EAD), the Endless Stairs below 70 stars: fog hides the top, so nothing ahead gives the distance. https://www.mariowiki.com/Endless_stairs
- C17 Silent Hill (1999), the opening alley: Cheryl at the edge of visibility as the alley darkens. https://en.wikipedia.org/wiki/Silent_Hill_(video_game)
- C63 the Backrooms canon's Level ! "Run For Your Life" and the fan game "Backrooms: Run For Your Life!": a never-ending hallway under flashing red lights and alarms (ONE SOURCE each): the run our room is named after. https://nerdist.com/article/the-8-scariest-levels-of-the-backrooms-creepypasta/ ; https://store.steampowered.com/app/2559530/BackroomsRun_For_Your_Life/
- C45 The Legend of Zelda: Breath of the Wild's Lost Woods: the fog thickens and whitens as a graded warning before the reset (two guide snippets). https://www.zeldadungeon.net/wiki/Lost_Woods_(Breath_of_the_Wild)

**In our Run! room.**
- *Corridor:* unchanged. The head as SP7's T1, gliding (T2's re-stack is left out: nobody would see it in the dark); the hidden cells are revealed in the dark, so nobody sees them appear.
- *Lamps:* three new rig states: *leave* (the strips ahead switched off as a front from FD1 to the end), *carry* (each strip strikes red as the player comes within 1.5 m of it), *runway* (a front from the player's cell to the door at the seat). The T-A and T-B heads behind the player rise with their strips: they carry the Relay read and keep protan players from losing the hunter in red.
- *Door:* the goal head and C42 ride the head and never dip (rule 7 holds): the far shore is the door's own light, alone.
- *Look B:* changed at the trip: its band is held as darkness ahead until the seat; then B's pulses as agreed.
- *Relay:* nothing reads it; the light follows the player's position only; the hunter never comes from ahead, so the dark hides nothing that matters.
- *Fog:* the tripped fog colour must be near black, or the black end reads as grey haze (build proposal).
- *Bar, rule C38, no EXIT sign:* as #1; the glide outruns a sprint, so nobody reaches the door moving.

**Build.** In `FrontRoomsRunRig` (游戏视觉, 灯具适配): the leave, carry and runway states; the T1 head with the 8 m/s glide and SP7's gaze gate (35°, fallback at 18 m); Reduce Flashing turns every front into 0.5 s cross-fades. 关卡设计: the hidden cells and their graph as #1. 声音设计 (events only): the clacks, the glide in the dark, the thud, ballast ticks as strips strike.

**Cost.** Cheaper than #1: the T1 head only (about 14-16k tris LOD0, about 8 draws, ≤ 3 MB), no T2, no new textures; fewer lights lit at once during the glide (a saving on phones and WebGL); three rig states in code. **Desktop, iPad, iPhone and WebGL: identical** (on WebGL the far strikes show as lit lenses, which is how they read anyway).

**Risks.**
- *Photosafety:* the largest area change of the five: the whole forward view goes dark and then red. Each strip changes twice per trip (white to dark, dark to red), each as part of a one-way front, never more than one change per strip in a second; the capture decides. NaissanceE (C70) is the warning.
- *Motion comfort:* low; no camera change; the moving thing is a small light.
- *Relay navigation:* the graph as #1. Players slow down in the dark, a chase cost the bots cannot show; the light one cell ahead is the mitigation; it needs a human playtest. Called from Away, the Relay's 19.7 s still covers a long hesitation.
- *Rule C38:* held.
- *Agreed rules bent:* it changes how look B plays at the trip (decision 6); the dark ahead bends "darkness sits between pools, never in pockets" for about 3 s; rule 7 holds.
- *Other:* the darkness may first read as a power cut or a bug, so the door's pool sliding on the floor has to read unmistakably as "it is leaving"; Red's model animation is thinnest here: one rigid glide seen as a lit fragment, no 重组.

**Scene 4.** Three driver states and the 8 m/s T1 glide, as a mode on N; under a day (estimate). Check the tripped fog colour, the Relay stand-in behind the player in red, and Reduce Flashing's cross-fades.

### 3.4 · DON'T LOOK BACK · 别回头 · PERCEPT + FLEES

**In one line.** The door never moves while I watch it; every time I look back at the hunter, it is farther when I turn round, and I catch it just settling.

**Where it sits.** PERCEPT (main): nothing moves that the runner can catch; the world changes out of view (catalogue §2.11). FLEES (second): the goal steps away a fixed number of times, then stops (Gwyndolin's model, §2.4). Not the classic. Its meaning: the building watches you back; the door is yours only while you hold it in your eyes.

**The rule** (the player and build proposals merged). After the trip, each time the camera turns 90° or more off the corridor's axis for 0.10 s (a look back, or a hard look sideways), the end steps back out of view: two cells the first time, then one cell per look, up to R. A fixed angle makes the rule the same on every screen: at the game's 76° vertical FOV even an iPhone's half-width of about 59° leaves the end well outside the view at 90° (build proposal's arithmetic); a frustum check is the safety (never step while any part of the end is on screen). One step per look: the view must come back within 35° of the axis before the next look counts. No step once the player is within 9 m of the door. A player who never looks back meets a plain room.

**As the player feels it** (R 15):
- *t 0, crossing FD1:* the trip; the band; crimson. The door at 27 m, lit, still. The track lines are in the soffit (the same tell as #1), but nothing moves.
- *If they never look back:* a plain room; the door at 3.3 s with 1.7 s of bar left.
- *About 1.2 s:* they flick round to check behind them (the Relay, or the empty arch). Because the run follows the view, the flick already costs ground. While they look, out of sight, the end steps back two cells: two clacks and a heavy thud behind their back.
- *About 1.8 s:* they turn forward. The door is 6 m farther than it should be (seen from 12 m, it is now a third smaller), and in the first half-second they catch the tail: the last blocks of the top course dropping into place, the sweeps pressing home, grit falling through the goal lamp, the panic bar's last rattle, a ghost band where it stood, the new strips already red. "It moved while I wasn't looking."
- *Each later look:* one more cell, up to 15 m in all (2 + 1 + 1 + 1 cells).
- *Arrival:* no looks 3.3 s; one look about 4.4 s; two about 4.9 s; three about 5.8 s; four about 6.7 s (as #1 at R 15), each plus the time the looks themselves cost.
- *Every second:* the Orpheus bargain: look back and see the hunter, or keep the door and trust the dark behind you.
- *Chase dive:* the lead at the door falls from L + 5.4 m (no looks, a plain room) to about L + 0.1 m (four looks) (player proposal's arithmetic).
- *Feel:* the highest fear of the five (uncanny, paranoid: it "knows" when you look away); high longing; readability medium (learned in one or two looks); fairness in the player's hands and bounded by R (never more than #1 costs); comfort: the safest, nothing ever moves on screen.

**The games it comes from.**
- C71 Layers of Fear (2016, Bloober Team; remake 2023): light or sound draws the eye and the house changes behind the player; the open window shuts as you reach it; tuned to the most common player behaviours (CONFIRMED; its one line about a hallway that seems to stretch: ONE SOURCE). https://gamedeveloper.com/audio/terrifying-players-with-unstable-level-design-in-i-layers-of-fear-i-
- C72 Antichamber (2013, Alexander Bruce): walk on and you never arrive; turn around and a new path has formed (CONFIRMED; the mechanism NOT FOUND). https://www.stripes.com/living/antichamber-playing-puzzled-in-a-house-of-madness-1.211052
- C15 Dark Souls (2011, FromSoftware), Dark Sun Gwyndolin's hall: each time you close in he is farther down the hall; a fixed set of jumps, cover between them, a stop at the far end: the fairness model. https://darksouls.wiki.fextralife.com/Dark+Sun+Gwyndolin
- C73 The Evil Within 2 (2017, Tango Gameworks), Chapter 2 "Something Not Quite Right": turning round, the door you came in by is the painted door 102 (ONE SOURCE per beat). https://www.gamepressure.com/evil-within-2/chapter-2-something-not-quite-right/zda42f
- C40 P.T. (2014, Kojima Productions): the hallway changes while the player is elsewhere, one change per pass. https://www.gamedeveloper.com/design/p-t-silent-hills-teaser-game-analysis
- C69 the F.E.A.R. "Terminus" hall: a far figure that moves only while the hall is dark (ONE SOURCE): the same rule turned round.

**In our Run! room.** The corridor and head as SP7 (the T1 head; T2 only as a short tail clip). Each revealed cell appears already in the corridor's state (crimson), so no strike and no flash. Look B unchanged; the sounds behind the player and B's pulses, which start at the entrance behind them, can draw the look back the way Layers of Fear lures the eye (a design reading, untested). The rule reads the player's camera only, never the Relay (rule 11 holds); the Relay never comes from ahead; its graph edges switch on with each step. The bar: with no looks, the plain room; otherwise as #1 at worst. Rule C38: the door never moves while seen and never within 9 m of the player. No EXIT sign.

**Build.** The T1 head relocated in one frame (its seat only) when the rule fires; in the same frame the revealed cells' renderers, colliders, lights (in the current state) and Relay graph edges turn on; a 0.5 s tail clip (the last course landing, the door's rattle, grit; about ten blocks) starts at the step so that a fast turn catches it; sound events at the step, behind the player's head. 游戏视觉 the rig state; 关卡设计 the cells; 系统设计 the rule and its bots, counting look-backs per input kind.

**Cost.** The cheapest option that still has a head: the T1 head (about 14-16k tris LOD0, about 8 draws, ≤ 3 MB) plus a tail clip under 1k tris; no new lights; one dot product a frame. **Desktop, iPad, iPhone and WebGL: identical.**

**Risks.**
- *Photosafety:* nothing changes in view: the safest of the five.
- *Motion comfort:* none.
- *Relay navigation:* it taxes the look back a chase depends on (the hunter is shown by the building; players may stop checking it), so it lands hardest on the chased; edges per step.
- *Rule C38:* held.
- *Agreed rules bent:* 71 §4.8 item 6 (one recede at one moment: here one step per look, at moments the player chooses) and item 11 (no teleport, the change in one continuous view: the step is an unseen relocation).
- *Parity:* on phones a look back is a long swipe or the gyro, so touch players look back less and meet an easier room; the rule is identical, the experience is not.
- *Other:* a first-time player may read the steps as random (the 6 m first step, the caught tail and the sound behind them are there to tie cause to effect, and Red should see whether they do); an expert who never looks back gets a plain room; it is a lying building, which this line keeps for deep floors only.
- *A sibling rule for comparison only* (the meaning proposal's WALK, DO NOT RUN): the end glides back only while the player sprints and seats when they stop, so room one's placard becomes physics. It reverses the agreed meaning 8a (Run! is not a punishment room; "WALK, DO NOT RUN" fails here) and needs 叙事设计 to re-sign it.

**Scene 4.** A yaw test and an instant step in the driver; hours (estimate). Check at 16:9, 4:3 and 19.5:9 that a step is never seen, and that the tail is caught on a normal turn.

### 3.5 · THE DREAM RUN · 梦里跑不动 · PULLED + SLOWED (Little Nightmares II's own way)

**In one line.** The door never moves; from the fire-door frame on, the floor runs back toward the entrance like a down escalator; I sprint and barely gain.

**Where it sits.** PULLED (main): the world takes the runner's ground while the goal stays where it was, the input points at the goal and loses (catalogue §2.6); SLOWED (second): the distance grows only in effort (§2.7). It is Little Nightmares II's own answer in the dream hallway (§1: the door is kept from Mono by being pulled out and by slow dream running, never by the corridor stretching), translated to a first-person run without a cut (player proposal). Not the classic. **The one option in which the door does not back away**: if Red's "不像…经典的隧道变远" means "not a door that gets farther at all", this, with the REFUSED options set aside in §4.3, is the answer.

**As the player feels it** (belt 1.0 m/s at T4, 1.25 m/s at T5; proposed here):
- *Before:* from FD1 on, the floor is tile-faced pallets, with a yellow-edged steel comb plate across the floor at FD1 and another at the door's threshold: "this floor is a machine", readable from the arch.
- *t 0, crossing FD1:* the trip; the band. A deep clunk underfoot ahead: the tile courses lift 10 mm along the lane lines into pallets, the comb teeth rise, and the floor starts to flow toward the player, reaching 1.0 m/s in 1.0 s.
- *Just after:* they step off FD1's comb plate onto it. Their sprint now nets 4.5 m/s. The lamps overhead pass more slowly than their legs say; the tiles stream under them faster than the walls; the red strips' reflections in the wax stay put while the tiles slide through them.
- *0-4.0 s:* the door, still, lit and centred, grows slowly. The dream run: fast legs, slow arrival.
- *About 4.0 s:* the far comb plate; step off; push the bar (never locked) with about 1 s of bar left. At T5: about 4.2 s.
- *Walker:* nets 2.2 m/s (T4) or 1.95 m/s (T5): the door in about 8-9 s (5.6 s in a plain room). *Slow gait:* nets 0.44 m/s (T4) or 0.19 m/s (T5): almost standing still; the door simply never comes.
- *The Relay* is carried too: its legs run at full speed and its ground speed drops as the player's does. While both are on the belt the gap between them does not change (a sprint still gains 1.1 m/s on it), and the player can see the hunter struggle too.
- *Chase dive:* the lead at the door is L + 0.8 to L + 2.6 m at T4 and L − 0.7 to L + 1.6 m at T5, for L between 4 and 12 m: about what #1 costs (player proposal's arithmetic).
- *The RESET:* as the white re-strike walks over it, the belt slows and locks; the pallets settle flush with one clack each course. The floor is a floor again.
- *Feel:* medium fear (frustration can win over dread); high longing (the door is close, big and still: "so near"); high readability (you can see why you are slow, a sensation everyone knows); fairness neutral between the player and the Relay while both are on the belt, harsher on slow walkers; comfort moderate.

**The games it comes from.**
- C30 Little Nightmares II (2021, Tarsier), Mono's dream hallway: the eye door never moves; Mono runs at it dream-slow (ONE SOURCE) and Six pulls him out; it is reached on the third visit. The scene the catalogue matches to Red's "隧道" (that it is the one Red meant: UNSURE). https://en.wikipedia.org/wiki/Little_Nightmares_II ; https://www.gamepressure.com/little-nightmares-ii/first-floor/z5e286
- C31 Shadow of the Colossus (2005, Team Ico; remake 2018, Bluepoint), the finale in the Shrine of Worship's long hall: a whirlwind of light drags Wander toward the pool; the player pushes away and loses (CONFIRMED that the struggle is playable; its detail UNSURE). https://en.wikipedia.org/wiki/Shadow_of_the_Colossus ; https://www.avclub.com/reliving-the-thrill-and-heartbreak-of-shadow-of-the-col-1798277953
- Row C38 Metal Gear Solid 4: Guns of the Patriots (2008, Kojima Productions), Act 5, the microwave hallway: Snake stumbles, then crawls, while the player keeps tapping; the cost of each metre rises toward the end; reached barely. https://www.metalgearinformer.com/great-moments-in-metal-gear-the-microwave-hallway/
- C37 Journey (2012, thatgamecompany), the last climb: deep snow and high wind toward the light seen all game; the traveller collapses short of it. https://en.wikipedia.org/wiki/Journey_(2012_video_game) ; https://kotaku.com/did-journey-blow-its-ending-5921086
- C35 Gris (2018, Nomada Studio), the windswept desert: gusts blow her back from the far building (ONE SOURCE). https://gamerwalkthroughs.com/gris/windswept-desert/
- C32 Outlast 2 (2017, Red Barrels), the St. Sybil vision "Inner Demon" (first person): the emergency exit at the corridor's end is locked and Blake is sent back to the far end (CONFIRMED; that a demon throws him: ONE SOURCE). https://www.gamepressure.com/outlast-2/inner-demon/z49b54
- C11 LSD: Dream Emulator, the Long Hallway (first person): the walker is blown back to the start (ONE SOURCE).

**In our Run! room.** The door and end wall never move: no head, no track, no reserved length. It works in a plain 9-cell room; depth sets the belt speed instead of R. Lamps and look B unchanged; the RESET locks the belt. The Relay is carried by the belt; nothing reads it. The bar: a full bar from the trip reaches the door with about 1 s to spare. Rule C38: the cleanest fit of the five, since the door is never anywhere else. No EXIT sign. Red's 往后退 / 越退越远: not met, except that a slow walker loses ground. Era: a moving walkway is a real kind of machine out of place in an office (this page's inference; no source was read on it).

**Build.** A floor module from FD1 to the threshold: tile-faced pallets (a scrolling UV on the existing floor material with low-contrast pallet seams, or real pallets for the two cells nearest the player), two comb-plate props, the lift into pallets as a short blend at the trip (游戏视觉); the belt's velocity added to the player's motion and to the Relay's while grounded on it (系统设计, 关卡设计); one ramp up at the trip and one down at the RESET; events for 声音设计 (the clunk, motor hum, pallet rattle, comb ticks, the lock).

**Cost.** The least new content of the five: no head, no hidden length, no moving lights; a floor material with a scroll, a seam texture ≤ 1 MB, two comb plates (about 1k tris each), one velocity term in the player's and the Relay's movement. **Desktop, iPad, iPhone and WebGL: identical.**

**Risks.**
- *Agreed rules broken by design:* 71 §4.8 item 8 (nothing may slow, trip or trap the runner, and everything that moves stays outside the walk volume) and 71 §4.1's Elm Street line ("the floor may move away, never slow or trap").
- *Motion comfort:* moderate: the camera is carried without the player's input (a 1 s ramp, then constant speed, which VR guidance treats as the comfortable case; flat screens are not measured in anything read); no Reduce Motion variant is possible without changing the timing that parity fixes.
- *Photosafety:* the moving floor fills the lower half of the view, so the pallet seams and tile grid must stay low-contrast (a moving regular pattern); the capture checks it.
- *Relay navigation:* the belt must carry it too, and the bot rounds rerun; the slow gait is nearly held still (about 40 s to the door at T4 and about 90 s at T5, longer than the Relay's 19.7 s), so a player creeping at the slow gait must walk or sprint: a trap for a cautious habit.
- *Rule C38:* held.
- *Red's image:* the door backing away is traded for Little Nightmares'.

**Scene 4.** A belt mode on N: the scroll and seams on cells 4-9, two comb plates, the lift blend, and the belt's velocity on the scene-4 walker (whether the walker takes an external velocity was not checked; small to moderate, estimate). Scene 4 cannot show the Relay being carried (its stand-in is static).

### 3.6 Side by side

| | 1 BUILDING TAKES IT BACK (SET) | 2 THE FAR SHORE (APART) | 3 THE LIGHT LEAVES (DARK) | 4 DON'T LOOK BACK (PERCEPT) | 5 THE DREAM RUN (PULLED) |
|---|---|---|---|---|---|
| What moves | the end wall and door, in steps | the far third of the corridor, as one island | the end, seen only as its light | the end, only while unseen | the floor under the runner |
| The player's verb | watch it be taken | watch the place leave, then cross | run into the dark toward a leaving light | resist the look back | push against the floor |
| 往后退 (the door backs away) | yes | yes | yes | yes, unseen | **no** |
| 越退越远 for a sprinter | no (the end's place, not the gap) | yes, while it runs; at R 15 it seats farther than at the trip | yes, as a light | only if they look back | no |
| 越退越远 for a walker | in the flinch only | yes, the whole time it moves | yes | only if they look back | only at the slow gait |
| 3D model animation (重组 / 向后退避) | the most: T1 roll + T2 re-stack | large: the joint tears, a compartment leaves, a raw shell appears | the least: a glide seen as light | a tail only | the floor turns into a machine |
| 彼岸 | weak (a machine) | **the strongest**: a lit place departs | very strong (the only light) | strong (yours while you look) | strong ("so near") |
| Fear | mechanical menace | the building comes apart | darkness ahead, hunter lit behind | **the highest**: uncanny, paranoid | frustration over dread |
| Readability | the best | high | low until the runway | medium | high |
| Photosafety | band, strikes, pulse, a moving block field | as 1, plus a lamp row moving away | **the largest area change** | nothing in view | a moving floor pattern |
| Motion comfort | low | low to moderate | low | none | **moderate** (camera carried) |
| Relay | edges per seat; 2-5 m chase-dive cost | per cell; same cost; lit warm in the gap | as 1, plus hesitation | taxes the look back; edges per step | carried by the belt; bots rerun |
| Agreed rules bent | none | decisions 4-5 (steps, T2); G1/G2 to watch | look B's trip; dark pocket | 71 §4.8 items 6, 11 | 71 §4.8 item 8 |
| First-person precedent for the denial | none (central gap) | none (story climaxes only) | yes (C69, C70) | yes (C71, C72) | yes (C32, C11) |
| Cost against SP7 | = SP7 (built) | **the most** | less | less | the least new content |
| Scene-4 work (estimate) | built | 1-2 days | under a day | hours | small to moderate |

---

## 4. The recommendation

### 4.1 REC: #2 THE FAR SHORE

Walk it in scene 4 beside #1 (SP7 as built), with the classic as a minutes-long reference (§4.2). All three proposals reached it independently (player, build and meaning first).

**Why.**
1. **It is the far shore Red described.** "永远到达不了的彼岸" is a shore, not a door. A whole lit compartment leaving, its fire doors open like a proscenium and the door shining at its far end, with raw no-man's land to cross after it, is the strongest picture of a far shore a first-person corridor can give (player proposal); the feeling is universal: running for a train that has started to move. It is what Little Nightmares does for 彼岸 (C14: the path breaks, Mono never reaches the far side) and what ICO does with its bridge (C12).
2. **Red's 11-03 words, each at full strength.** 往后退: the compartment with its door backs away. 越退越远: with #3 (which shares its 8 m/s run but shows it only as a light), the one option in which the gap grows for a walker the whole time it moves and for a sprinter while it runs; at R 15 the door seats farther from a sprinter than it was at the trip. 模型动画, 向后退避: the largest model move of the five (the collar lets go, the main parts at its coupling, the wool tears out of the joint, a compartment leaves, a raw shell appears behind it).
3. **It is not the classic.** Nothing stretches, the lens never moves, the door never shrinks alone: a place departs.
4. **It keeps every agreed number and rule:** the 9 m trip at FD1, R 9 / 15, the bar's arithmetic, rule C38, look B, the RESET, rule 11, no cut, teleport or FOV change, and the floor never fails under the runner (the line 71 draws from Bendy and Elm Street).
5. **It grows out of what is built.** SP7's head becomes the island's far end; the track, magnets, sweeps, ghost bands and per-cell reveal carry over. Its story is already in the game's text: R.M.'s remodeling memo makes the raw between an unfinished building, not a lying one.

**Its honest flaw.** It is the least proven and the most expensive of the five. No game does it in a first-person run: its precedents (ICO's bridge, the endings of Little Nightmares II and Kingdom Hearts) are story climaxes, two of them cutscenes, so nobody can say it works until it is walked. It moves the most (about three cells of corridor with 6-8 lights; 25-50k moving triangles by the proposals' estimates), it is tight on the phones' per-camera light cap, and on WebGL the island is lit only by its own glow beyond 10 m. It gives up two things Red agreed on 11-03, the stepped recede (decision 4) and T2's re-stack (decision 5), for one continuous departure. Its raw gap may count, for Red, as changing the room's nature (the G1/G2 rule that cut the plant-room catwalk).

**If it fails the walk.** If the raw gap changes the room for Red: the gap in the finished kit under its heads (it loses the torn read), or #1 stays the base. If Red wants the mood more than the model: #3 (the runner-up: cheaper than SP7, and Red's own lamp chase turned into the denial). If Red wants Little Nightmares' own feeling: #5. If Red wants a door that never moves in view: #4.

### 4.2 What to show Red next (scene 4, rule 5b)

- **After Red picks:** build the pick in SP7 beside #1, on the free **N** key (§3.0), greybox first. Whatever Red picks enters the game only after Red has walked it in scene 4 and said so (rule 5b).
- **The classic as a control (⓪, minutes; build proposal):** SP7's glide driven at the player's own forward speed, so the door keeps its size on screen until R is used: the Poltergeist signature in first person, built in the set, with no FOV change. A sprinter holds it 1.6 s (R 9) or 2.7 s (R 15). Not for the game (it reads as running in place, and under a chase it is the worst of all), but it lets Red feel what "经典的隧道变远" is and rule it out, or in.
- **If Red would rather see before picking**, the cheapest first (estimates): ⓪ minutes; #4 hours; #3 under a day; #5 small to moderate; #2 one to two days. Then a five-panel still strip per treatment in Shot format (`W/tools/shotframe`): arrival, trip, mid-move, seat, door.
- **The one combined test (rule 25)**, desktop Editor first: draws, triangles, visible lights and shadow casters at the trip, mid-move and seat (#2 above all, against the phone tier's 32); on-screen change counts, then the Harding FPA or PEAT capture with the brighter red tube (#3 first, then #1 and #2); the 2.05 m Relay stand-in in #2's gap and #3's dark; whether #4's step is ever seen at 16:9, 4:3 and 19.5:9; the tripped fog colour for #3. Chase fairness needs 系统设计's bots in the game (scene 4 has no moving Relay), and nothing is played aloud (rule 24).

### 4.3 Left out, and why

- **STRETCH, the classic** (F1 Poltergeist, C1 Max Payne, C2 The Path, C4 Amnesia). The lens version: in first person a door that keeps its size while you sprint needs the FOV to widen, and a widening FOV while sprinting is the stock speed cue (R2, Minecraft), so it reads first as "I sped up" (the technique draft's analysis, untested); XAG 117 wants forced camera and FOV changes optional; Amnesia's designer set his distortion below what felt best (ONE SOURCE); tied to the Camera Motion setting it would vanish at 0%, so two players would see different rooms; agreed rule 11 forbids FOV tricks. The geometry version (a rubber stretch of the far section) slides the near walls past the runner, stretches textures and colliders, and spreads the lamps off the 3 m pitch, against the whole-cell rule. Kept only as the control ⓪.
- **LOOP** (C40 P.T., C41 The Exit 8, C43 Super Mario 64, C51 Little Nightmares II's Signal Tower doors). The most common treatment in games (26 rows) and nearly free, but Red's 11-03 agreement ruled it out for Run! (71 §4.2, M2): the trip has sent the Relay here, a second pass doubles the time inside, the goal door stops being the escape verb, and a straight lit corridor has no hidden seam except the goal door itself.
- **REFUSED** (C19 Little Nightmares II's slammed door, C41 The Exit 8's false staircase, C53 Superliminal's fire doors, C21 Outlast 2, C20 Hello Neighbor, C46 Arkham Asylum's morgue door). Two proposals offered it: the build proposal's THE FALSE SHORE (the door at 27 m opens on more dark corridor, and the lamps run on to the real door) and the meaning proposal's EVERY EXIT LEADS FURTHER IN (a line of never-locked doors one cell apart, the floor's thesis "Every exit leads further in" played in one room). Left out of the five because it has no recede and no model animation (Red's 11-03 asks), it breaks agreed rule 9 (no false door) or bends its reason (the way out in doubt), and it wears out once learned (The Exit 8 keeps its loop fresh with many anomalies; here there is one shape). It remains the cheapest possible scene-4 mode, and 71's M3 (a later room where the same door is finally reached) remains 叙事设计's.
- **FAR by portal or instancing** (C65 Manifold Garden, C66 Portal): one more full scene render per portal per recursion level, the most expensive technique on phones and WebGL, and the 46 m far plane still cuts. #3 gives the endless feeling with darkness instead.
- **SLOWED by a plain speed cut**, or by the stamina bar alone: dream molasses with nothing to show why, unfair in a chase. #5 gives the same feeling through a visible machine.

### 4.4 A layer for any of the five: the door that comes closer (PROMISE)

Little Nightmares II's real structure (C30): one far door shown again and again, denied, nearer each visit, reached late, and reaching it is the turn. Across a run, every receding room could end in the same recognisable door (the meaning proposal: its lite glows sodium orange where every other Run! door glows white). On the last visit it comes to meet the player (in #2 the island comes back and docks and the gap closes; in #1 the recede clip runs backwards) and opens on the loading dock, with the night street seen and not taken. It needs a per-run counter and a dock release; it bends 71 §4.8 item 6 (the same way every time) by design, and on its last visit reverses 71's rule that R grows with depth; most runs meet only one or two receding rooms, so the payoff is rare. It is 叙事设计's and 系统设计's to decide, after Red picks a treatment.

---

## 5. Limits: what was not verified

- **No video was watched** by any stage, and no timestamp is verified anywhere; every precedent's motion is known from text. For Red to judge the treatments by eye, the open item is a video pass on ICO's bridge (C12), Little Nightmares II's TV hallway (C30) and ending run (C14), Gwyndolin's hall (C15), NaissanceE (C70) and Layers of Fear (C71).
- **Web reading only**, by the four research agents; this synthesis read no new web pages. The shared search budget ran out near the end of all four drafts; catalogue §4 lists the leads not checked, among them The Evil Within (2014)'s stretching hall (a CinemaBlend snippet only: the best open lead for a game STRETCH), Visage's reddening hallway, Layers of Fear 2's chase doors, and a Roblox developer-forum thread about a door that moves away as the player gets close (https://devforum.roblox.com/t/help-door-that-moves-away-as-the-player-gets-close/3639352, not read).
- **Little Nightmares:** how the TV visits are framed, and whether the hallway ever moves, were described by no page read (the Fandom wiki and Heroes Wiki returned 402; TV Tropes, GameRevolution, Twinfinite and GameSpot 403). That C30 is the scene behind Red's "隧道": UNSURE.
- **Arithmetic, not measurement.** Every time and gap is computed from 3.2 / 5.5 / 1.44 m/s, the 5 s bar and the agreed lengths; the chase-dive leads assume the player walks after the bar and the Relay keeps 4.4 m/s on the same line. Hesitation in #3 and look-back frequency in #4 need human playtests; no data on how often players look back was found.
- **Numbers proposed here, not agreed:** the 8 m/s cruise with 0.5 s ramps (#2, #3); #2's gaze-gate fallback at 12 m; #3's 12 m/s dark front and 1.5 m lead light; #4's 90° / 0.10 s / 35° / 9 m and its two-then-one cells; #5's 1.0 / 1.25 m/s belt. Each was chosen so that its cost to the player stays near the agreed recede's.
- **Costs are estimates.** #2's moving-triangle range differs between the proposals (25-50k); the head's 14-16k is the build proposal's correction from Run! B's twin-head count; the 32-light cap for mobile and WebGL is the technique draft's reading of Unity's manual, and the URP version in use was not checked.
- **Comfort:** no study found measures sickness from these effects on a flat screen; whether the game should honour iOS Reduce Motion for them was not verified.
- **The central gap stands:** no source shows a first-person game moving a goal door back in view during a chase, so #1 and #2 are new; their test is the walk in scene 4.
- **One source page was hostile:** a page read for the liminal draft (tcrf.net, The Stanley Parable) returned text addressed to AI agents; the drafts ignored it, and it is not used here.
- **This stage** edited nothing in `research/level_space`, sent nothing to Red or to other lines, and ran no ledger, git or clone action; Red's 11-04 words were checked in transcript `7da6f3fe` (a queued message, origin human).

---

## 6. 给 Red（中文）：永远到不了的门：五种做法，请你挑一个方向

### 背景

11 月 3 日你说：玩家走到 Run! 原本长度的三分之一，尽头的门往后退，越退越远，而且要有模型上的动画；你还提到《小小梦魇》那种"永远到达不了的彼岸"。
11 月 4 日你说，感觉不像那种经典的"隧道变远"，要我们查遍做过这种设计的游戏，总结五种不同的做法给你挑。
我们派了四路人去网上查（只读网页，没下载，没开游戏引擎），整理出 80 个游戏片段，再从"玩家感受""好不好做""讲什么故事"三个角度各提了五种，最后合成下面这五种。

先说两点：
- 五种都不是经典的"隧道变远"。
- 第 1 种就是今天场景 4 里已经做好的那种，其余四种都是新的。

几个数字（五种都一样）：一格 3 米；门本来在 27 米；跨过 9 米那道防火门框就触发警报；深层的房间门最多退 9 米或 15 米；冲刺每秒 5.5 米，体力条 5 秒；走路每秒 3.2 米。

---

### 第 1 种 · 机械后退（今天场景 4 里做的那种）

**玩家的感受：** 跨过 9 米的门框，警报响。尽头那面砖墙一层层拆开，砖块沿两边墙和天花板往后流。门亮着、立着，自己往后滑。到了新位置，砖块再一层层砌回去，把门围住。先猛地一缩，再一格一格退，最后很重地落定。你看着大楼把出口收了回去。

**出自哪些游戏：**
- 《控制》（Control）的烟灰缸迷宫：墙在你眼前折叠，地板升起，路只有一条。https://80.lv/articles/creating-control-s-ashtray-maze/
- 《耻辱 2》（Dishonored 2）的发条宅邸：拉一根杆，整个房间变形。https://www.gamedeveloper.com/design/level-design-deep-dive-i-dishonored-2-s-i-clockwork-mansion
- 《传送门 2》（Portal 2）：机械臂上的墙板重新拼出房间。https://theportalwiki.com/wiki/Portal_2_developer_commentary
- 《寂静岭 3》（Silent Hill 3）：一段红光追逐里，有一面墙动了一次。https://www.silenthillmemories.net/sh3/walkthrough_13_lakeside_amusement_park_en.htm
- 《LSD: Dream Emulator》的长走廊：走到尽头，那个房间在你眼前散开（只有一个来源）。https://dreamemulator.fandom.com/wiki/Long_Hallway

**放进 Run! 的样子：** 和 11 月 3 日定的一样。天花板两角的轨道是预兆，细心的人从入口就看得到。门一直最亮。最后门是真门，能推开。门停过的地方留下一圈干净的印子。

**成本：** 已经做好。电脑和平板有拆砖重砌；手机简化成半层的长条；网页版只有整面墙往后滑。

**缺点：**
- 它最接近经典效果的"结果"：门在眼前变远。如果你说"不像经典"是在说它，那它就是最不对的一种。
- 冲刺的人只看得到第一下拆砖，后面变成整面墙滑走。
- 一大片砖块一起动，要先过光敏测试。

---

### 第 2 种 · 彼岸漂走（推荐）

**玩家的感受：** 这条走廊本来就是三段防火分区：9 米一道门框，18 米一道门框。你跨过 9 米那道，警报响。前面 18 米那道门框"咔咔"两声松开，消防水管在接头处断开，喷出一口锈，接缝里的防火棉被扯成一缕一缕。走廊最后那一段，连着灯、连着门，整段往后开走，像一节亮着的车厢离站，比你冲刺还快。它走过的地方露出没装修的毛坯：灰砖、水泥地、挂在线上的工作灯被风带得直晃。它开走的那一两秒里，就算你在冲刺，门也在变远；走路的人看着那道缝一直变宽。在最深的那种房间里，它停下时，门比你刚跨过门框时还远。你要跑过这片毛坯，才能回到那段亮着的走廊，推开门。

**出自哪些游戏：**
- 《古堡迷踪》（ICO）：城门打开，看得见自由；桥从中间裂开，女孩那一半往后漂，缝一秒比一秒宽。https://gameinformer.com/b/features/archive/2014/02/04/moments-the-bridge-scene-in-ico.aspx
- 《小小梦魇 2》（Little Nightmares II）的结尾：路在 Six 跑过去之后断了，她抓住 Mono 的手，又松开。Mono 永远没到对岸。https://www.gamepressure.com/little-nightmares-ii/ending/zce2a4
- 《王国之心》（Kingdom Hearts）的结尾：地面裂开，两个人被分开（过场动画）。https://www.thegamer.com/kingdom-hearts-heartbreaking-moments/
- 《LSD: Dream Emulator》：走廊尽头在你眼前散开（只有一个来源）。https://dreamemulator.fandom.com/wiki/Long_Hallway

**放进 Run! 的样子：**
- 还是 9 米触发，你定的一样。
- 你脚下的地面永远不动，只有前面那段在走。
- 毛坯那段和走廊一样宽、一样高，还是同一条走廊，只是没装修完。
- 门跟着那节"车厢"走，一直最亮，停稳了才推得开；门永远不锁。
- 警报的红色脉冲在两岸跑，跳过毛坯那段。警报解除时，你喜欢的白光从入口一格一格亮到裂缝，跨过去，再亮到门口。
- 这一层里，那段毛坯一直留着，像一道疤。

**成本：** 五种里最贵。整整三格走廊带着灯一起动，还要一套毛坯材质。手机上要简化；网页版上远处的车厢只靠门自己的光。

**缺点：**
- 没有任何游戏在第一人称跑步时这样做过；它的来源都是剧情高潮或结尾的过场。必须先在场景 4 走过才知道好不好。
- 它用"一口气开走"换掉了你 11 月 3 日定的"一格一格退"和"拆砖重砌"；它的模型动画是开头那一下撕开接缝。
- 毛坯那段可能碰到你定过的"走廊要一直是走廊、不能改变房间性质"那条。
- 那么大一段东西快速离开，少数人可能会不舒服。

---

### 第 3 种 · 光走了

**玩家的感受：** 跨过 9 米，身后的灯变红，前面的白灯一盏一盏往门那边熄灭。前面全黑，只剩门还亮着。然后门的光往后飘走，像夜里离港的船灯。没有墙、没有灯可以数，你不知道走廊还有多长。你头顶的灯在你跑到时才变红，脚下一直有光。门停下后，红灯从你脚下一盏一盏亮到门口，像机场的跑道灯，你才看见走廊真的有多长。

**出自哪些游戏：**
- 《NaissanceE》：跟不上前面的光，就被留在黑暗里（只有一个来源）。https://en.wikipedia.org/wiki/NaissanceE
- 《F.E.A.R.》的一条长走廊：灯一段一段灭，人影一次比一次近（只有一个来源；是哪一作也没核实）。https://fear.fandom.com/wiki/List_of_Alma_Sightings
- 《寂静岭 2》（Silent Hill 2）：一段往下的楼梯，伸进黑暗，看不到底。https://www.theodysseyonline.com/silent-hill-2-level-design
- 《超级马力欧 64》（Super Mario 64）的无尽楼梯：雾挡住顶端，怎么走都到不了。https://www.mariowiki.com/Endless_stairs
- Backrooms 的 Level !「Run For Your Life」：红灯、警报、永远走不完的走廊（只有一个来源）。https://nerdist.com/article/the-8-scariest-levels-of-the-backrooms-creepypasta/

**放进 Run! 的样子：** 走廊不变。门的灯一直亮着，不会熄。尽头其实只是整面墙往后滑，你只看得到门那一团光。你喜欢的"一个一个熄灭，一个一个亮"在这里就是机制本身。门停下以后，你选的警报脉冲照常开始。

**成本：** 比今天的还便宜。主要是灯光程序，没有新模型。四个平台一样。

**缺点：**
- 黑暗一开始可能被当成停电或者出错了。
- 玩家在黑暗里会犹豫、变慢，机器人测不出来，要真人试。
- 前面一大片先变暗再变红，是五种里光敏风险最大的，要先过测试。
- 模型动画最少，没有拆砖重砌。
- 它改了你定的警报脉冲在触发那一刻的样子。

---

### 第 4 种 · 别回头

**玩家的感受：** 你看着门，门就不动。你一回头看身后有没有追兵，门就趁你不看往后退。你转回来，门远了，还刚好看到最后一块砖落下、灰在门灯里飘。第一次退两格，之后每回头一次退一格，退到底就不再退。你一直在选：回头看追兵，还是盯住门。

**出自哪些游戏：**
- 《层层恐惧》（Layers of Fear）：用光或声音引你看别处，再改掉你身后的房间。https://gamedeveloper.com/audio/terrifying-players-with-unstable-level-design-in-i-layers-of-fear-i-
- 《Antichamber》：一转身，新的路出现了。https://www.stripes.com/living/antichamber-playing-puzzled-in-a-house-of-madness-1.211052
- 《黑暗之魂》（Dark Souls）的葛温德林：你一靠近，他就瞬移到走廊更远处；次数固定，最后停下。https://darksouls.wiki.fextralife.com/Dark+Sun+Gwyndolin
- 《恶灵附身 2》（The Evil Within 2）第 2 章：一回头，进来的那扇门变成了墙上的画（只有一个来源）。https://www.gamepressure.com/evil-within-2/chapter-2-something-not-quite-right/zda42f
- 《P.T.》：你不在的时候，走廊变了，每一圈变一样。https://www.gamedeveloper.com/design/p-t-silent-hills-teaser-game-analysis

**放进 Run! 的样子：** 走廊和今天一样。门在你眼前永远不动；你离门 9 米以内，它也不会再退。从不回头的人，遇到的就是一间普通的 Run!。

**成本：** 几乎不花钱：用今天的尽头，加一个"玩家有没有回头"的判断。四个平台一样。

**缺点：**
- 它惩罚"回头看追兵"，被追的人最吃亏。
- 手机上回头要划很长一下，手机玩家回头少，房间就等于更简单。
- 第一次遇到的人可能以为是随机的。
- 它打破了你定的两条：门不能"瞬移"，一间只退一次。

---

### 第 5 种 · 梦里跑不动

**玩家的感受：** 门一直不动。跨过 9 米，脚下的地板"咔"一声抬起来，变成往回走的传送带，像逆着走下行的自动扶梯。你拼命冲刺，门只是慢慢变大。离得那么近，就是到不了。慢慢走的人几乎原地不动。

**出自哪些游戏：**
- 《小小梦魇 2》（Little Nightmares II）Mono 梦里的走廊：门从来不动，是 Mono 跑得像慢动作（只有一个来源），Six 把他拉回来；第三次才打开门。https://en.wikipedia.org/wiki/Little_Nightmares_II
- 《旺达与巨像》（Shadow of the Colossus）的结尾：你拼命往外推，还是被光吸回去。https://en.wikipedia.org/wiki/Shadow_of_the_Colossus
- 《合金装备 4》（Metal Gear Solid 4）的微波走廊：越接近尽头，越难往前爬。https://www.metalgearinformer.com/great-moments-in-metal-gear-the-microwave-hallway/
- 《风之旅人》（Journey）最后的雪山：风雪把你压住，光就在前面。https://en.wikipedia.org/wiki/Journey_(2012_video_game)
- 《逃生 2》（Outlast 2）：快到出口时，被甩回走廊另一头（"被甩回去"只有一个来源）。https://www.gamepressure.com/outlast-2/inner-demon/z49b54

**放进 Run! 的样子：** 尽头不用会动，普通的 9 格就能做。越深，传送带越快。追你的 Relay 站上去，也一样被往回带。门就在原地，永远不锁。警报解除时，白光走过去，传送带停下锁死，又变回地板。

**成本：** 五种里新东西最少：一段会动的地板、两块梳齿钢板、一点移动的计算。四个平台一样。

**缺点：**
- 门不往后退。你要的"往后退、越退越远"没有了，换成了《小小梦魇》自己的做法。
- 它打破了你定的"不能让跑的人变慢、被困住"。
- 镜头被地板带着走，有人会晕。
- 慢慢走的人在传送带上几乎走不动，可能被困住。

---

### 我们推荐：第 2 种「彼岸漂走」

**为什么：**
- 三个角度各自独立提案，最后都选了它。
- 它就是你说的"彼岸"：不是一扇门在退，是一整段亮着的地方离你而去，中间留下一片要跑过去的空地。
- 它就是《小小梦魇 2》结尾和《古堡迷踪》的桥在做的事。
- 你 11 月 3 日说的三件事都做满：门往后退；越退越远（它开得比冲刺快，冲刺的人也看得到门变远）；有很大的模型动画（接缝撕开、水管断开、整段走廊开走）。
- 它不是经典的走廊变远：走廊没有被拉长，是断开了。
- 你定的都守住：9 米触发、门永远不锁、警报脉冲、白光亮回来、不看 Relay 在哪、不切镜头、不变焦。
- 它是从今天场景 4 的"会退的门"长出来的：尽头那套轨道和门直接变成那节"车厢"的尾巴。

**诚实的缺点：** 它最没有先例，也最贵。没有游戏在第一人称跑步时这样做过，它的来源都是剧情高潮。所以只有在场景 4 里走过才知道它好不好。它还用"一口气开走"换掉了"一格一格退、拆砖重砌"，毛坯那段也可能碰到"走廊要一直是走廊"那条。如果你走过觉得不对，第 1 种还在，照样能用。

**第二选择：** 第 3 种「光走了」。比今天场景 4 里的还便宜，而且就是你喜欢的那种灯一盏一盏灭、一盏一盏亮。你更想要气氛而不是模型的话，选它。

**还可以加一层（不管选哪种）：** 《小小梦魇 2》的做法是同一扇门出现好几次，每次更近，最后终于打开。我们可以让一局里每间会退的房间都是同一扇认得出的门，一次比一次退得少；最后一次它反过来朝你迎过来，门后是装卸平台，看得见夜里的街，但还是出不去。这要叙事设计来定。

**如果你想先看再选：** 在场景 4 里，"经典隧道变远"的对照版几分钟就能做；第 4 种估计几小时，第 3 种不到一天，第 2 种一到两天（都是估计）。可以先做出来，每种给你五张截图，再一起走一遍。

---

### 你问的《小小梦魇》"隧道"

资料里唯一对得上的，是《小小梦魇 2》里 Mono 梦里那条通向"眼睛门"的长走廊（开场的梦、几次钻进电视、第 4 章第三次才打开门）；它不是走廊变远，是 Six 把他拉回来、梦里跑不动，而且每次都更近一点；"永远到不了的彼岸"更像《小小梦魇 2》结尾：路断了，Six 抓住 Mono 的手又松开（https://en.wikipedia.org/wiki/Little_Nightmares_II）。

---

### 怎么回

- 回一个编号，比如"2"。
- 或者组合两个，比如"2，加上 3 的灯光"。
- 或者回"按推荐"，就是第 2 种。
- 也可以回"先在场景 4 做出来看看"，我们先做便宜的几种给你走。

---

## 7. Built in scene 4 (SP7), 2026-11-04

Red, 13:2x in the level-space chat: "把每一个都在场景4里做出来看看". All five are in `FrontRoomsStructuresRunRecede.cs` (the X selector, the treatment interface) and `FrontRoomsStructuresRunEnd*.cs` (one file per treatment), landed 20:11 (fr land run_ends, gate mac + iOS 0 errors). SP7 opens on #1 (today), variant 9 -> 14, look B; X cycles 1-5 (Shift+X back), Y puts the room back, F is Reduce Flashing, N is the current treatment's own option. The treatments act in the receding variants (V); plain variants stay today's room.

| # | What was built | Walk / sprint to the door (s after the trip) | Read |
|---|---|---|---|
| 1 机械后退 | today's T1 head + T2 re-stack | 10.12 / 7.23, door at 42 | the reference |
| 2 彼岸漂走 (REC) | FD2 collar; at the trip the far third (17.8-27 m, lamps, dress, door) leaves as one island at 8 m/s and seats at 42 (R 15); a raw block gap under five work lamps opens behind it | 10.12 / 7.23, door at 42 | reads as §3.2; the collar (the tell) reads from FD1, not from the arch: a design call |
| 3 光走了 | lamps ahead go out toward the door, the door's light drifts back into the dark, the lamp over the walker turns red as they reach it, then a red runway to the door | 10.12 / 7.23, door at 42 | reads as §3.3 |
| 4 别回头 | the end steps away only while the walker looks back (two cells, then one per look, capped), never within 9 m; the door opens onto a lit 3 m stand-in room | no looks: 5.43 / 3.28, door at 27 | reads; the settling blocks are small from 19 m |
| 5 梦里跑不动 | from FD1 the floor is a belt running back (comb plates), the door never moves; a walker always arrives | 8.48 / 4.97, door at 27; slow gait 85.32 | reads in motion; the end wall's top slots show as bare notches |

Sheets: `sheets/sp7_run_ends.jpg` (5 x 5: trip, mid, climax, door, look back; WAIT-RED) and `sheets/sp7_run_ends_extras.jpg`. Films (video only, tone mark): `video/sp7_end_e1_f1.mp4` (today, sprint), `sp7_end_e2_fa.mp4` (#2, walk-sprint-walk), `sp7_end_e3_f1.mp4` (#3, sprint), `sp7_end_e4_f1.mp4` (#4, sprint with four looks back), `sp7_end_e5_f2.mp4` (#5, a walk on the belt); every film in `~/FrontRoomsVisualWork/level_space/renders/scene4/run_ends4/`. Walked in the clone: every door opened, through and back, nothing stuck (`renders/scene4/run_ends_walk3/walk_sp7_ends.tsv`). Photosafety: no film over three flashes in one second (WCAG 2.3.1); the busiest windows are #4's walks into the white hall at 42; the saturated-red capture (Harding FPA or PEAT) is still owed. Fixed during the render: #2's gap had rendered near black (raw block tinted twice, weak work lamps); #3's floor ahead between strips; the stand-in room's fill; FD1 / FD2 casings flickered against the wall opening (also in today's #1; now 4 mm proud). Logs: `~/FrontRoomsVisualWork/level_space/run_ends/log_*.txt`, `FRAME.txt`.

What to try first: press 9 for SP7, X once for #2, stand behind the arch, hold Left Ctrl and run; watch the island pull away at FD1, cross the gap, E at the door at 42; 9 again to return; then walk it and stop under a work lamp in the gap, look up and back. Then X for #3 and sprint; #4: sprint and flick the view back twice, then a run that never looks back (the door at 27 opens); #5: walk, sprint, hold Shift on the belt, then stand still.
