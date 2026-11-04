# Showcase scenes · 10 · How demo scenes show every asset, and a layout standard for scenes 3 and 4

- 给 Red：这份调研回答"高规格资产包和官方样例怎么像博物馆一样摆放所有资产"。共读了 39 项来源（Unreal/Unity 官方样例、Fab 和 Asset Store 付费包、关卡设计的 gym 做法、博物馆展陈研究），出处全在 SOURCES.md。
- 共同点：它们把三件事分开——"数清有多少"（整齐排列的总览）、"亲手试"（编号展台）、"看它放进游戏像什么"（布置好的小场景）；入口都是一个能一眼看到所有去处的大厅。
- 场景 3：一个大厅加 4 个编号房间（1 门、窗与储物柜；2 钥匙与 EXIT 链；3 屏幕；4 纸张、标牌、警报卡、电话），每间约 12×12 米、层高 2.9 米，和游戏里的房间一样大；每间 9 个展台，放不下就拆出新房间。
- 每个展台同一套做法：说明牌永远在同一位置，写"编号和名称 / 怎么试 / 这是什么、游戏里用在哪 / 你在这里要决定什么"；变体排成一排，只差一个属性；每间尽头是一个 1990 办公室小角落，用游戏灯光，让你判断它放进游戏合不合适。
- 操作：数字键 1–4 直达房间，0 回大厅，R 重置当前房间，对着说明牌按 E 重置单个展台，L 切换"阅读光 / 游戏光"；触屏用门和墙上的开关做同样的事。
- 场景 4：大厅里每个结构一个编号传送垫，另有一条把所有结构串起来的步行环线；每个结构先从高处或窗口看到全貌再走进去，默认游戏灯光、真实尺寸，尺寸还没定的就并排做 2–3 个。
- 两个场景都只放一个启动物体，按一张表在 Play 时生成全部房间、展台和说明牌；没有 Relay，也没有多余的 HUD。
- 第 6 节有 7 个需要你拍板的问题，每个都附了建议。

Date: 2026-11-03. Line: 总控, workflow `showcase_research` (login G), synthesis stage. Built on three sweeps that read their sources on 11-03: Unreal [E1-E15]; Unity and level-design practice [U1-U9, G1-G5]; museum and exhibition design [W1-W10]; plus FrontRooms' own numbers [P1-P8]. Ids, URLs and what was read: `SOURCES.md`; full entries: `~/FrontRoomsVisualWork/showcase_scenes/` (`unreal.txt`, `unity.txt`, `museum.txt`). "(inf.)" marks a step of mine beyond the sources. Research only: no code, no Unity run. Readers: the 场景 3 session lays scene 3 out by §3; the structures workflow and the level_space line use §4-5 for scene 4.

## 1. What the best demo scenes have in common

- **Three jobs, kept apart:** count and compare (overview, "zoo"), try (demo, "museum", "gym"), feel (showcase, vignette). Epic's store defines overview, demo and showcase maps [E2]; Storm separates gym, zoo and museum levels [G3]; Synty ships an overview scene beside its demo scenes [U4, E15]; one interaction pack marks feature stations and its in-use corner with two pad colours [E13].
- **One front door that names every destination:** a welcome hall [E1], a hub of portals each with a picture, a name and one line [E10], step-on teleport tiles [E4], a museum-like hub that is also a calibrated look-development room [U1], one hub so there is one level to remember [G3], a zone menu [U5]. Museums give "what is here" and "where am I" at the entrance and again at each section [W4], with a photo-keyed plan [W1].
- **Numbered stops that match a list:** SECTION.ITEM stands cross-referenced to the docs table [E1, E11]; numbered lollipop signs [W4]; label codes in the text plan [W2]; the real asset name on a floor tag [E6]; one table of name, line and path [E10]; one record per exhibit with its own viewpoint [U6].
- **One station kit, repeated identically:** identical frames, so the eye compares contents [E1, E11, E13]; a three-tier sign of number and title / how to run it / what it shows [E1], or the verbs in brackets on a floor pad [E13]; the label in the same place every time [W1]; a text hierarchy fixed once and written in walking order [W2].
- **Variants in one row, one property changing:** captioned only with the setting that differs [E13]; three shapes that differ in one setting [U3]; a typological series, simple to complex [W8]; a single row, nothing behind anything [W1, W6]; a matrix with named axes when two things vary [E15]; twin lanes when the behaviour, not the look, varies [E14].
- **Bounded rooms with modest counts:** the best-used exhibitions had 34-37 elements, 12 of the 14 overloaded ones had more than 50, and a vast open hall was the least used of all [W3]; a single entry and exit, no free-standing islands [W3, W4]; a loop that passes every room and returns to the start [W5].
- **Light chosen per hall, with one switch:** a neutral studio for overviews [E2]; lit per hall, not per exhibit [E1]; a day/night split [E13]; Dark / Light / Colorful floor tiles that restyle the whole level [E4]; several lighting set-ups in a zoo [G3].
- **Scale shown, not stated:** grid textures [E2, E4, E8, G2]; pieces at their true mounting height [E7, E13]; several body types on one course [E4]; a 1 m marker [G3]; the game's own room, door and ceiling sizes, never an open plane [G2].
- **Walk or jump, both offered:** hub portals and teleport tiles [E4, E10, G3]; a zone menu plus free roam [U5]; next / previous exhibit [U2]; four ways into a tour [W10].
- **Spaces shown as spaces, parts beside them:** environments are shown dressed and lit, with a designed first view and close details [E12, E9]; kits stand as parts next to the assembled result [E3, E5]; kits are proven by their awkward joins [G1, G2].
- **Completeness is maintained:** an asset missing from the overview is fixed like a bug [U4]; a gap in a series stays visible [W8].

## 2. Patterns

| Pattern | Who does it | Why it works | Cost here |
|---|---|---|---|
| Hub that shows every destination (picture, number, name, one line) | E10, E4, E1, U1, G3 | one place to remember; choose before walking | one room; low |
| Teleport pads, each arriving at a set viewing point | E4, E10, G3, U2 | one step to anywhere; same on desktop and touch | low |
| Numbered stations, codes shared with a list | E1, E11, W4, W2, E6 | "keep 1.4B" is a complete instruction | none (from the table) |
| Identical bay frame | E1, E11, E13 | the eye compares contents, not frames | one prefab |
| Three-tier sign: number and title / how to try / what it is | E1, E13, W1 | the "how" line is what a tester needs | text per station |
| Text where the eye already is (stage edge, rail, floor) | E11, E13, W1 | read while the object is in view | none |
| Live state readout beside the exhibit | E11 | a broken station shows at a glance | a small hook per stateful kind |
| Variant row, one property changing, captioned with it | E13, U3, W8, W1, W6 | the difference is the only change | layout rule |
| Matrix with the two axes named once | E15, W8, W5 | pick a cell; a missing combination shows as a gap | needs two real axes |
| Twin lanes for behaviour variants | E14 | the choice is felt side by side | objects doubled |
| Overview strip: every variant, no interaction | E2, E6, E7, U4, G3, W9 | count at a glance; completeness | static prefabs; low |
| In-use vignette or period room | E13, U3, U4, W7, E15 | shows whether the piece belongs | dressing; medium |
| One switch between neutral and game light | E13, E4, G3, U1 | judge the object alone and in the game | two light rigs; low-medium |
| Grid floor, 1 m ruler, a human-height figure | E2, E4, E8, G2, G3, G4 | sizes read before walking them | one material; low |
| Bounded room, one way in, loop back to the start | W3, W4, W5 | fewer missed stops, no backtracking | plan discipline |
| Landmark at the end of the entry axis | W4, W5, E12 | pulls the visitor through the room | layout only |
| Same structure in 2-3 sizes side by side | E4 | choose by walking, not imagining | geometry x2-3 |
| Parts tray beside the assembled piece | E3, E5 | parts and result read in one look | static; low |
| Dressed first view plus 2-3 detail spots | E12, E9 | mood and material judged where they matter | dressing; high |
| Stress joins: loop back, stack, meet the standard door | G1, G2 | finds what is not ready for the game | extra modules; medium |
| Metric markers in green / orange / red | G3 | easy, possible and impossible read at a glance | markers; low |
| Play loop: goal, skill check, reward | G5 | shows how a structure plays at walking pace | layout |
| Overlook onto the whole before entering | W9, W5, E5 | a map without a map | a balcony or window per structure; medium |
| Index or zone picker with descriptions | U2, U5, U6, W10 | jump straight to a viewing spot | UI; low-medium |

## 3. Layout standard: scene 3, `FrontRoomsScreenAdsDemo.unity` (every interactable)

A **station** is one stop with one placard; it holds one exhibit or one row of variants. Code = room.station plus a variant letter, e.g. `1.4B` [E1, W2].

**3.1 Circulation.**
- Hub 0 with four numbered rooms around it, a cloverleaf: each room is a one-way loop whose exit opens back into the hub [W4, W5, E10, G3]. All four room doors are in view from the spawn [E10].
- In a room: enter at the middle of the hub-side wall, on axis with the far-wall alcove, which is the landmark [W4, W5]; turn right and follow the walls in station order; leave by the exit in the same wall, which is behind you as you enter, so an open door does not pull you past the stations [W4].
- Room 2 is the one strictly ordered run: key, padlock, chain, EXIT door; the EXIT door is its exit to the hub [E9, W3, P8].
- About 5 minutes a room and 20 for the scene [W3, W10].

```
                +------------+------------+
                | 2 Keys and | 3 Screens  |
                | EXIT chain |            |
   +------------+--d------d--+--d------d--+------------+
   | 1 Open and d                         d 4 Read and |
   |   pass     |   HUB 0  (8 x 4 cells)  |   call     |
   |            d    board    ^ spawn     d            |
   +------------+-------------------------+------------+
   d = door (each room: one entry, one exit); every room 4 x 4 cells

                 +-------- alcove 6 x 3 m --------+
                 |  .5  1990 corner, game light   |
   +------+------+                                +------+------+
   |      |  .6  |   landmark on the entry axis   |  .4  |      |
   +------+------+--------------------------------+------+------+
   |  .7  |                                              |  .3  |
   +------+                                              +------+
   |  .8  |          open lane, no islands               |  .2  |
   +------+                                              +------+
   |  .9  |                                              |  .1  |
   +------+--- exit ------------- entry -----------------+------+
         reset switch           arrival point (key n), faces .5
   one room seen from above, hub side at the bottom; route .1 to .9
```

**3.2 Rooms and stations.**
- Default rooms, to check against the real count and regroup in the table: 1 Open and pass (doors, windows and sashes, lockers); 2 Keys and the EXIT chain; 3 Screens (terminals and CRT screens, TVs and screen ads); 4 Read and call (papers, placards, the alarm card, phones) (inf., following the sweeps' groupings; the alarm card is signage, not part of the chain [P8]).
- Room = 4 x 4 cells (12 x 12 m) with the 2.9 m ceiling, about the game's run room (11.5 x 12 m) and its standard ceiling [P1, P3], so every exhibit is seen at the distances and heights the game uses [G2].
- Bays one cell (3 m) wide: 3 on each side wall and 2 on the far wall beside the 6 x 3 m alcove, so 9 stations a room [E1, E13] and 36 in four rooms, close to the 34-37 elements of the best-used exhibitions [W3]. A variant row counts as one station [W2, W8]. The centre stays open [W4]; the lane is far wider than the 1.5 m minimum [W1]. An exhibit wider than 2 m takes two bays.
- A room whose families do not fit splits (doors first, inf.); the new room takes the next free number. Kinds beyond what the rooms hold go to a plain overview strip along the hub wall, no interaction, one name tag each, rather than lengthening the walk [W9, E2, E6, W6].
- Each bay has a stop point marked on the floor 1.5 m in front of the exhibit [W1]. The next bay's centre is then 3.4 m away, beyond the 2.4 m reach [P2], so one prompt shows at a time (inf.).

**3.3 Staging.**
- In a wall: whatever the game mounts in a wall (doors, windows and sashes, placards, the alarm card, screen ads, TVs, wall phones) stands in a short length of the game's own wall at true size and height [P1, E7, E13]. Doors get 1.2 m clear on both sides, so Red can walk through and look back [P1].
- On a plinth: hand-sized pieces (keys, papers, handsets) on a plinth with its top at about 1.0 m, one row, nothing behind anything [W1, W6]; if their low height in the game is the point, the placard says so [W6].
- Period mini-room: the far-wall alcove is a dressed 1990 corner where the room's families are used as in the game, in game light; it is the landmark that pulls Red through and the test of whether a piece belongs [W7, E13, U3, U4].
- Within a bay, back to front: wall pieces at mounting height, floor pieces in the middle, hand pieces at the front [E7, E13]. No vitrines: everything can be touched (inf.).

**3.4 Placards: content and placement** (faces and sizes are 平面视觉's). Four kinds, fixed once and never added to [W2]:
- **A · Hub board**, facing the spawn and low enough not to hide the doors: scene name; a plan with each room's number, name and a picture of its alcove, drawn so up = forward [W1, W4, E10]; the controls on two lines, desktop and touch.
- **B · Room sign**, beside each entry on the hub side and again just inside: number and name, the families it holds, how many stations, about 5 minutes [W2, W4]. The room number also large above the entry, to be read across the hub [W4].
- **C · Station placard**, on a rail at the front right corner of every bay (same side, same height every time, top at about 1.0 m, tilted to the eye), read from the stop point [W1, E11, E13]. Five lines:
  1. code and name: `1.4 Sash window` [E1];
  2. Try: steps of one action each, desktop and touch together ("Look at the sash. E / tap to lift. E / tap to lower.") [W1, E1, E13, P4];
  3. what it is and where the game uses it, one sentence that stands alone [W2, E1];
  4. **Red decides:** the open choice at this station ("keep or cut; A, B or C"; "is the order clear without text?") (inf., from the scene's purpose);
  5. small: prefab name, status (in the game / candidate / retired) and the live state as the exhibit's own script reports it (locked / unlocked / open) [E6, U6, E11].
  First sentence 16 words or fewer, whole placard about 50-60 words [W2].
- **D · Group card**, on the back wall at the left end of a variant row, centre about 1.37 m [W1]: the one property that changes and its letters ("Doors by wear: A new, B scuffed, C burnt" is an example) [W2, E13, W8]; under each variant a small floor tag with its letter and prefab name [E6].
- These labels must not look like 1990 signage, because placards are themselves exhibits here (inf.; Q3).

**3.5 Lighting.**
- Two states for the whole scene, switched together, never per exhibit [E1, E13, E4, G3]:
  - **Reading** (default; Red asked to see the assets outside the game's context): even ceiling light, no fog, no film effects, fixed exposure, mid-grey floor and walls, no shadow on a placard [E2, W1, W7, U1].
  - **Game**: the game's troffers and post stack as in scene 1, lamps Steady [P6].
- The alcoves keep game light in both states, so each room compares neutral and in-game at once (inf., after W7).
- A lamp state that a family is mostly seen under (Striking, Unstable, Dead [P6]) is a variant at that station, not a third mode (inf.).
- Switch: L (bound nowhere in the game [P4]), plus a wall switch in the hub and at each room entry for touch.

**3.6 Scale.**
- The visitor is the game's player: eye 1.62 m, height 1.75 m, reach 2.4 m, Shift = slow walk, Ctrl = sprint [P1, P2, P4]. It replaces the current scene 3 rig, a free camera at 0.7-2.2 m with Shift = faster [P5], because only the real eye and reach judge an interactable as the player will meet it [G2].
- Floor: mid-grey with the 3 m cell grid drawn faintly and a 1 m tick ruler along each lane edge [E2, E8, G2, G3, G4].
- The game's door, window and ceiling sizes everywhere [P1]; a 1.75 m silhouette on the hub wall beside the board [G3, E4].

**3.7 Jump and reset.**
- Spawn in the hub, facing the board and the doors [E10].
- Desktop keys: 1-4 = arrive at room n's entry, looking down its axis to the alcove, with station .1 first on the right [E4, U2, U5, W5]; 0 = the hub; R = reset the room you are in (R is the game's END RUN key [P4]: the same idea at room scale); L = light. No number key is bound in the game [P4]; the bootstrap owns these keys in scenes 3 and 4 (inf.).
- Touch: every room is a short walk from the spawn, so the doors are the jump; a reset switch by each room's exit resets that room (inf.). Desktop and touch reach the same functions, keys are only shortcuts.
- One station: use (E or tap) on its placard resets it: doors close and lock as authored, the sash drops, the key goes back on its plinth, the chain goes back on, screens go off (inf.; no source documents a reset control: E14's refill and W10's self-resetting stations are the nearest).
- Nothing resets on its own when Red walks away, so a door can be left open for comparison (inf.).

**3.8 Variants side by side.**
- One row per group: same height, light, backdrop and spacing; one property changes; letters A, B, C [W8, W1, W6, E13, U3]; never two deep [W1, W6].
- Two real axes make a matrix with the axes named once, e.g. doors with rows = finish or wear and columns = type [E15, W8].
- Behaviour variants (prompt distance, tap or hold) as twin lanes with identical objects and a two-sentence board at each head [E14].
- A planned variant that does not exist yet keeps its place as an empty frame with its code [W8].
- Optional A/B stand: one spot that swaps variants in place from its placard [U6] (inf.).

**3.9 Left out.**
- No Relay, no Warn lamps, no hunter, no timer, no title, map or room stream: nothing chases or times Red [P5, P6, U5].
- HUD: the game's own interaction prompt only, because it is part of what is judged; nothing else unless switched on (inf.).
- No free-standing islands, nothing two deep, no vitrines, no room over its 9 stations [W1, W3, W4, W6].

**3.10 Build and load.**
- One bootstrap object in the scene, beside the player rig and the light rig; nothing else is saved in the scene file (inf.: a small scene file opens fast and rarely conflicts).
- One table, one row per station: code, room, family, prefab, staging (wall / plinth / alcove), variant letter, try (desktop), try (touch), what it is, Red decides, status, start state. At Play one framework builds the hub, rooms, bays, placards, switches and readouts from it [U2, U6, E10]. Adding an interactable = adding a row; regrouping = editing the room column.
- The same table prints the hub board, the room signs, the placards and Red's pick sheet, keyed by code and prefab name [E10, U6, W2, W10].
- Only the hub and the room Red stands in are awake; screens and video run only there (inf.; this matters on iPhone and iPad).
- Prefabs: interactables pivot at bottom centre with +Z toward the visitor at scale 1; wall and floor pieces pivot at a corner on the 3 m grid [U7, U8, P1].
- Completeness: an interactable kind in the game with no row is a bug [U4]; a check in the existing tests can list such kinds (inf.).
- Inert for the game scenes and the shipped build [P7].

## 4. Layout standard: scene 4, `FrontRoomsStructures.unity` (every new structure)

What each structure is comes from level_space's district spec (`research/level_space/70_structures_districts.md`, being written); this is the frame it sits in. Same bootstrap and framework as scene 3, a second table with one row per district (a district = one structure and its approach).
- **Circulation:** the hub holds one numbered pad per district, in an arc facing the spawn, each under a picture with number, name and one line on what to try [E10, E4]; one walking loop also joins the districts in order and returns to the hub [G5, W5]; between two districts runs a short quiet passage (inf., after W1 and W5).
- **District size:** the structure at full size plus a one-cell apron, approached along a standard 2.9 m corridor through a standard door, so it is judged against the game's ordinary room, never on an open plane [G2, P1] (the open-plain gym of E8 does not suit big enclosed spaces, inf.).
- **Exhibits:** one structure per district [E4, W7]; 2-3 sizes side by side only where a size is still open [E4]; for kit-built structures (terraces, stairs) a parts tray outside the door, rows labelled once on the floor [E3, E5].
- **Staging:** in place, dressed and lit as in the game [E12, E9]; the grey version with grid by switch where a number is being set [E4, E8, G2].
- **Placard:** one district sign at the arrival point: number and name, the route to walk ("in at the top, down the large stairs, out by the slide"), and what Red decides (keep or cut, which size, where it could sit in the game) [E10, W2]; metric markers where a number is open, green / orange / red = easy / possible / not possible [G3]; 1 m ticks on risers and edges [G4]. Faces and sizes: 平面视觉.
- **Lighting:** game light by default, because the mood is what Red judges here [E12, E9]; the same reading / grey switch as scene 3 [E4, G3].
- **Scale:** the same player rig [P1, P2]; a standard door frame at every threshold [G1, P1]; grid on grey faces [E8, G2].
- **Jump and reset:** keys 1-9 = district n's arrival point at its overlook (§5); 0 = hub; R = back to this district's arrival point; a return pad beside each arrival point for touch [E4, E10]; a fall into the pool, the well or a void puts Red back there at once [G5].
- **Variants:** sizes side by side [E4]; a look A/B by switch [U6].
- **Left out:** as scene 3 [P5, P6, U5]; no open plain [W3].
- **Build and load:** districts are table rows that call level_space's builders [P7]; only the hub and the current district are awake (inf.).

## 5. What scene 4's structures need beyond scene 3

1. **Whole-space scale.** A structure is judged at full size from a normal approach: a 2.9 m corridor and a 1.0 x 2.1 m door before the big space [G2, P1]. Main paths are visibly larger than side paths, so the space reads at once [G4].
2. **A walking route per structure.** First view from the threshold along a long axis that ends on a lit focal point [E12, W5]; then a goal, a skill check and a reward on the way down, e.g. terraces, then the large stairs, then the slide into the pool, the well as a setpiece [G5]; then back to the arrival point without retracing [W5, G5].
3. **Overlooks.** Red sees each structure whole before entering it: from a balcony or gallery above (pool, terraces, large stairs, the well's rim) or through a window across (a dramatic space) [W9, W5, E5]. The arrival point is the overlook. A hub balcony that sees several districts at once can replace a map [W9].
4. **Detail spots.** Two or three per structure where one material reads at arm's length, lit for it [E12].
5. **Joins.** Each structure is shown meeting the game: stacked on another floor, looped back to a corridor, meeting the standard door, because those joins show what is not ready [G1, G2].
6. **Metrics.** Colour-coded markers [G3] and floor rulers [G4] wherever a rise, drop, depth or width is still open; one grid on all grey faces [E8, G2].
7. **Safe failure.** Falling is cheap: one step back to the arrival point [G5].
8. **Pacing.** A quiet passage between districts, so each lands fresh (inf., after W1 and W5); a full walk of about 20 minutes [W3, W10].

## 6. Open questions for Red (each with a recommendation)

- **Q1 · How scene 3 is grouped.** Four rooms by verb (open and pass; keys and the EXIT chain; screens; read and call), or one room per family (ten rooms)? Four rooms means fewer walks and comparisons across related kinds; ten rooms means emptier rooms and more walking. **Recommend four**, splitting a room only when its families no longer fit its 9 stations [W3].
- **Q2 · Default light.** Scene 3 opens in reading light, scene 4 in game light, both with L to switch? Reading light shows each asset on its own, as you asked; game light shows mood, which is the point of a structure. **Recommend yes**, and the scene-3 alcoves always in game light.
- **Q3 · Label style.** Showcase labels in one neutral style that is plainly not 1990, or labels made as 1990 signs? In-world signs feel nicer but blur with the placard exhibits. **Recommend neutral showcase labels** designed by 平面视觉, in English like the game; a Chinese line can come from the same table if you want it.
- **Q4 · A separate "in use" room.** A whole 1990 office where every family works together [E13, E15]? It adds a room of dressing; the per-room alcoves and scene 1 already show context. **Recommend not now**; add it if you miss it after the first walk.
- **Q5 · Scene 4 movement.** Pads only, or pads plus one walking loop through every district? Pads are fastest; the loop is how a player would meet these spaces. **Recommend both** [G5, W5].
- **Q6 · Grey versions and joins in scene 4.** For every structure, or only where a number or a join is still open? Doing all of them adds a grey proof and the loop, stack and door-frame tests to every structure [G1, G2]. **Recommend only where still open.**
- **Q7 · How you record picks.** A keep / cut switch at each station, or a printed pick sheet by code that you fill after the walk? A switch saves a step but adds UI and state; the sheet needs nothing new. **Recommend the sheet**: you say "keep 1.4B" and the code and prefab name do the rest [E1, E6].
