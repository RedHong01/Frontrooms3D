# Showcase scenes · 10 · How demo scenes show every asset, and a layout standard for scenes 3 and 4

- 给 Red：这份调研回答"高规格资产包和官方样例怎么像博物馆一样摆放所有资产"。共读了 40 项来源（Unreal/Unity 官方样例、Fab 和 Asset Store 付费包、关卡设计的 gym 做法、博物馆展陈研究），出处全在 SOURCES.md；复核时逐条对了原文，改了几处引用，补读了两个付费模块化办公室套件的总览图。
- 共同点：它们把三件事分开——"数清有多少"（整齐排列的总览）、"亲手试"（编号展台）、"看它放进游戏像什么"（布置好的小场景）；入口是一个能看到所有去处的大厅。付费套件的总览图都放在图集最后、默认网格地面、不写名字，所以编号和名字要我们自己加。
- 场景 3：一个大厅加 4 个编号房间，按游戏里实际的 22 种可交互物分（1 门；2 钥匙与 EXIT 链；3 窗与警报；4 屏幕与纸张）。每间 4×4 格（12×12 米，层高 2.9 米）：两侧各 3 个一格宽的展位，尽头是 1990 办公室小角落（游戏灯光）；门和窗装在展位朝走道的那条格线上，后面那一格是能走进去、关门、回头看的小隔间。
- 每个展台同一套做法：说明牌永远在同一位置，写"编号和名称 / 怎么试 / 这是什么、游戏里用在哪 / 你在这里要决定什么"；变体排成一排，只差一个属性。
- 交互设计 17:12 已落地第一版场景 3：在游戏自己的一层地图里导览，因为门、窗、钥匙、EXIT 和警报装置都是 MapWorld 的记录，要靠游戏的操作循环才能用。这里的房间是你要的"博物馆"那一半，需要 MapWorld 能按一张表搭出来（Q8）；按键沿用导览的那套。
- 场景 4：按 level_space 70 定的六个区（SP1 滑梯到 SP6 深水池地形）：大厅里每区一个编号传送垫，另有一条把六区串起来的步行环线；落点、第一眼、中文说明牌、灯光和材质都以 70 为准，度量标记做成可开关的叠加物。
- 第 6 节有 8 个需要你拍板的问题，每个都附了建议。

Date: 2026-11-03. Line: 总控, workflow `showcase_research` (login G): three sweeps read their sources on 11-03, Unreal [E1-E15], Unity and level-design practice [U1-U9, G1-G5], museum and exhibition design [W1-W10]; then a critic pass the same evening re-checked every claim against those entries, read E16, and aligned §3 with the scene 3 tour that landed at 17:12 [P9] and §4-5 with level_space's district spec [P10]. FrontRooms' own numbers [P1-P10]. Ids, URLs and what was read: `SOURCES.md`; full entries and the critic's findings: `~/FrontRoomsVisualWork/showcase_scenes/` (`unreal.txt`, `unity.txt`, `museum.txt`, `critic.txt`). "(inf.)" marks a step beyond the sources. Research only: no code, no Unity run. Readers: the 场景 3 session (交互设计) lays scene 3 out by §3; the structures workflow and the level_space line use §4-5 with `level_space/70`.

## 1. What the best demo scenes have in common

- **Three jobs, kept apart:** count and compare (overview, "zoo"), try (demo, "museum", "gym"), feel (showcase, vignette). Epic's store defines overview, demo and showcase maps [E2]; Storm separates gym, zoo and museum levels [G3]; Synty ships an overview scene beside its demo scenes [U4, E15]; one interaction pack marks feature stations and its in-use corner with two pad colours [E13]; premium modular kits show the dressed place first and the overview last [E16].
- **One front door that names every destination:** a hub of portals each with a picture, a name and one line [E10], step-on teleport tiles [E4], a museum-like hub that is also a calibrated look-development room [U1], one hub so there is one level to remember [G3], a zone menu [U5]; Epic's welcome hall orients but opens nothing, maps load from the editor menu [E1]. Museums give "what is here" and "where am I" at the entrance and again at each section [W4], with a photo-keyed plan [W1].
- **Numbered stops that match a list:** stand numbers that match the docs table [E1] (zone.letter codes in E11); numbered lollipop signs [W4]; label codes in the text plan [W2]; the real asset name on a floor tag [E6]; one table of name, line and path [E10]; one record per exhibit with its own viewpoint [U6].
- **One station kit, repeated identically:** identical frames, so the eye compares contents [E1, E11, E13]; a three-tier sign of number and title / how to run it / what it shows [E1], or the verbs in brackets on a floor pad [E13]; the label in the same place every time [W1]; a text hierarchy fixed once and written in walking order [W2].
- **Variants in one row, one property changing:** captioned only with the setting that differs [E13]; three shapes that differ in one setting [U3]; a typological series, simple to complex [W8]; a single row, nothing behind anything [W1, W6]; a finish-by-function matrix when two things vary [E15]; twin lanes when the behaviour, not the look, varies [E14].
- **Bounded rooms with modest counts:** the seven best-used exhibitions shared one trait, fewer elements (Star Wars 37, Amber 34; Playground 7 kinds of activity), 12 of the 14 overloaded ones had more than 50, and a vast open hall was the least used of all [W3]; a single entry and exit, no free-standing islands [W3, W4]; the Sainsbury Wing has no loop through every room, and its visitors doubled back and missed rooms [W5].
- **Light chosen per hall, with one switch:** a neutral studio for overviews [E2]; lit per hall, not per exhibit [E1]; a day/night split [E13]; Dark / Light / Colorful floor tiles that restyle the whole level [E4]; several lighting set-ups in a zoo [G3].
- **Scale shown, not stated:** grid textures [E2, E4, E8, G2]; pieces at their true mounting height [E7, E13]; several body types on one course [E4]; a 1 m marker [G3]; the game's own room, door and ceiling sizes, never an open plane [G2].
- **Walk or jump, both offered:** hub portals and teleport tiles [E4, E10, G3]; a zone menu plus free roam [U5]; next / previous exhibit [U2]; four ways into a tour [W10].
- **Spaces shown as spaces, parts beside them:** environments dressed and lit, wide views down an axis to a lit focal point and close-ups in place [E12, E9]; kits as parts next to the assembled result [E3, E5]; kit overviews on the engine's default grid and sky, rows by kit role, props by kind and size, finishes as a swatch board, no labels [E16, E2]; kits proven by their awkward joins [G1, G2].
- **Completeness is maintained:** an asset missing from the overview is fixed like a bug [U4]; a gap in a series stays visible [W8].

## 2. Patterns

| Pattern | Who does it | Why it works | Cost here |
|---|---|---|---|
| Hub that shows every destination (picture, number, name, one line) | E10, E4, U1, G3 | one place to remember; choose before walking | one room; low |
| Teleport pads, each arriving at a set viewing point | E4, E10, G3, U2 | one step to anywhere; same on desktop and touch | low |
| Numbered stations, codes shared with a list | E1, W4, W2, E6 | "keep 1.2B" is a complete instruction | none (from the table) |
| Identical bay frame | E1, E11, E13 | the eye compares contents, not frames | one prefab |
| Three-tier sign: number and title / how to try / what it is | E1, E13, W1 | the "how" line is what a tester needs | text per station |
| Text where the eye already is (stage edge, rail, floor) | E11, E13, W1 | read while the object is in view | none |
| Live state readout beside the exhibit | E11 | a broken station shows at a glance | a small hook per stateful kind |
| Variant row, one property changing, captioned with it | E13, U3, W8, W1, W6 | the difference is the only change | layout rule |
| Matrix, rows one value and columns another (naming the axes is ours) | E15, W8, W5 | pick a cell; a missing combination shows as a gap | needs two real axes |
| Twin lanes for behaviour variants | E14 | the choice is felt side by side | objects doubled |
| Overview strip: every variant, no interaction, rows by kit role, finishes as swatches | E2, E6, E7, E16, U4, G3, W9 | count at a glance; completeness | static; low |
| In-use vignette or period room | E13, U3, U4, W7, E15 | shows whether the piece belongs | dressing; medium |
| One switch between neutral and game light | E13, E4, G3, U1 | judge the object alone and in the game | two light rigs; low-medium |
| Grid floor, 1 m ruler, a human-height figure | E2, E4, E8, G2, G3, G4 | sizes read before walking them | one material; low |
| Bounded room, one way in, loop back to the start | W3, W4, W5 | fewer missed stops, no backtracking | plan discipline |
| Landmark at the end of the entry axis | W4, W5, E12 | pulls the visitor through the room | layout only |
| Same structure in several sizes side by side | E4 | choose by walking, not imagining | geometry x2-3 |
| Parts tray beside the assembled piece | E3, E5 | parts and result read in one look | static; low |
| Dressed first view plus a few detail spots | E12, E9 | mood and material judged where they matter | dressing; high |
| Stress joins: loop back, stack, meet the standard door | G1, G2 | finds what is not ready for the game | extra modules; medium |
| Metric markers in green / orange / red | G3 | easy, possible and impossible read at a glance | markers; low |
| Play loop: goal, skill check, reward | G5 | shows how a structure plays at walking pace | layout |
| Overlook onto the whole before entering | W9, W5 | a map without a map | a rim or gallery; medium |
| Index or zone picker with descriptions | U2, U5, U6, W10 | jump straight to a viewing spot | UI; low-medium |

## 3. Layout standard: scene 3, `FrontRoomsScreenAdsDemo.unity` (every interactable)

**State [P9].** 交互设计's tour (landed 17:12) is a menu: 1 MEDIA BAY (the screen ads on their own rig), 2 FLOOR 1 AS SHIPPED, 3 EVERY DEVICE, 4 KEYS FLOOR, the last three loading the game's own floor, because doors, windows, keys, the EXIT and the alarm devices are FrontRoomsMapWorld records on cell edges, run by FrontRooms3DGame's player loop, and their states exclude each other. Its guide gives each of 22 kinds a how-to line and jumps to the nearest example. That is the in-context half (E9's playable level, E13's white pad). This section is the museum half: the same kinds in bounded rooms, side by side, out of the game's context, built from MapWorld cells and edges so the game's own loop still runs them (§3.10).

A **station** is one stop with one placard; it holds one kind or one row of variants. Code = room.station plus a variant letter, e.g. `1.2B` [E1, W2].

**3.1 Circulation.**
- Hub 0 with four numbered rooms round it, a cloverleaf: each room is a one-way loop whose exit opens back into the hub [W4, W5, E10, G3]. The board at the spawn names all four; rooms 2 and 3 open straight ahead, 1 and 4 at the hub's ends (inf.).
- In a room: enter by the door right of the hub wall's middle, with the alcove, the landmark, straight ahead [W4, W5]; go up the right wall, through the alcove, down the left wall, and out by the exit left of the middle, behind you as you enter, so no open door pulls you past the stations [W4]. A landmark pulls harder than the right-hand wall (W4's order of forces; half of W5's visitors walked the axis to the picture at its end), so floor numbers and an arrow on the inside room sign give the order [W4].
- Room 2 is the one strictly ordered run: ZONE KEY, LOCKED DOOR, KEY DOOR, KEY TAG, STAIR KEY, then the EXIT DOOR (padlock cut, chain fall), which is the room's exit to the hub [E9, W3, P8, P9].
- About 5 minutes a room and 20 for the scene [W3, W10].

```
                  +-------------+-------------+
                  | 2 Keys and  | 3 Windows   |
                  |  the EXIT   | and alarms  |
   +--------------+--X------E---+--X------E---+--------------+
   | 1 Doors      E                           X  4 Screens   |
   |              X     HUB 0  (8 x 4 cells)  E  and paper   |
   |              |  overview strip  ^ spawn  |              |
   +--------------+---------------------------+--------------+
   E entry, X exit (facing the room, E is right of the middle); every room 4 x 4 cells

       0         3                   9         12 m  (one room; hub side at the bottom)
   12  +---------+-------------------+---------+
       | closed  |  .4 alcove 6 x 3  | closed  |
    9  +---------+  1990 corner      +---------+
       |   .5    |                   |   .3    |
    6  +---------+       lane        +---------+
       |   .6    |      6 x 9 m      |   .2    |
    3  +---------+                   +---------+
       |   .7    |                   |   .1    |
    0  +---------+----X----+----E----+---------+
       bay fronts on x = 3 and x = 9; arrival inside E, facing .4; reset switch by X
```

**3.2 Rooms and stations.**
- The rooms hold the game's own 22 kinds [P9] plus the screen ads and the ALARM ZONE card [P8], grouped by verb and split where a room is full (inf.):
  - 1 Doors: E DOOR, BODY DOOR, DOOR LITE, DOOR SIGN, BROKEN DOOR, RUN! DOOR;
  - 2 Keys and the EXIT chain: ZONE KEY, LOCKED DOOR, KEY DOOR (key in hand), KEY TAG, STAIR KEY, EXIT DOOR;
  - 3 Windows and alarms: WINDOW, SASH LATCH, FOILED WINDOW, ALARM DOOR, the ALARM ZONE card;
  - 4 Screens and paper: OFFICE TERMINAL, STATION TV, the screen ads, EVACUATION PLACARD, STATION PAPER, DEV CLOSE-UPS (desk phone, wall clock, vending machine, copier: test-bed, never shipped).
  - OTHER CLOSE-UPS go to the hub's overview strip; lockers are in no tour ("none in the game", P9), so a locker keeps an empty frame with its code there [W8].
- Room = 4 x 4 cells (12 x 12 m), Standard 2.9 ceiling, about the game's run room (11.5 x 12 m) [P1, P3], so every exhibit is met at the game's distances and heights [G2].
- Bays are the room's outer cells, three on each side wall (.1-.3 right, .5-.7 left); the far row's middle two cells are the alcove (.4); the far corners are closed; the lane is the middle 6 x 9 m and stays open [W4], far wider than W1's 1.525 m route. So 7 stations a room (6 bays and the alcove), 28 in four, for the 23 exhibits the rooms hold: few, like the best-used exhibitions [W3]. A variant row counts as one station [W2, W8].
- A bay's front is its cell edge on the lane. Doors and windows stand in that edge, where MapWorld keeps them [P9], with the bay cell behind as a lit 3 x 3 m closet, so Red can walk through, shut the door and look back (the 1.2 m door clearance of P1 holds); wall pieces hang on a front wall; hand pieces stand on a plinth in an open front (inf.).
- A stop mark the size of W1's stop area (0.76 x 1.22 m) lies in the lane 1.5 m in front of each front (inf.). The prompt answers only what the aim ray meets within 2.4 m [P2], so exhibits never compete; the 3 m bay pitch keeps the next exhibit 3.4 m from a stop mark, out of reach (inf.).

**3.3 Staging.**
- In a front: whatever the game mounts in a wall (doors, windows and sashes, placards, the alarm card, screen ads, TVs) stands in a front of the game's own wall at true size and height [P1, E7, E13].
- On a plinth: hand-sized pieces (keys, papers, the desk phone), one row, nothing behind anything [W1, W6], top at about 1.15 m, W6's lowest comfortable show level (0.46 m below the eye) taken to the 1.62 m eye; W1's 1015 mm is a seated visitor's limit (inf.). Where a piece's low height in the game is the point, the placard says so [W6].
- Period corner: the alcove is a dressed 1990 corner where the room's kinds are used as in the game, in game light; it is the landmark that pulls Red through and the test of whether a piece belongs [W7, E13, U3, U4].
- No vitrines: everything can be touched (inf.).

**3.4 Placards: content and placement** (faces and sizes are 平面视觉's; language: Q3). Four kinds, fixed once and never added to [W2]:
- **A · Hub board**, facing the spawn and low enough not to hide rooms 2 and 3: scene name; a plan with each room's number, name and a picture of its alcove, drawn so up = forward [W1, W4, E10]; the controls on two lines, desktop and touch.
- **B · Room sign**, beside each entry on the hub side and again just inside, with the arrow to .1: number and name, the kinds it holds, how many stations, about 5 minutes [W2, W4]. The room number also large above the entry, to be read across the hub [W4].
- **C · Station placard**, on a rail at the front right corner of every bay (same side, same height every time, top at about 1.0 m, tilted to the eye), read from the stop mark [W1, E11, E13]. Five lines:
  1. code and kind: `3.2 SASH LATCH` [E1, P9];
  2. Try: the tour's how-to line for the kind [P9] with the touch steps beside it, one action per step [W1, E1, E13, P4];
  3. what it is and where the game uses it, one sentence that stands alone [W2, E1];
  4. **Red decides:** the open choice at this station ("keep or cut; A, B or C"; "is the order clear without text?") (inf., from the scene's purpose);
  5. small: kind and prefab names, status (in the game / candidate / test-bed / retired) and the live state as the exhibit's own script reports it (locked / unlocked / open) [E6, U6, E11].
  First sentence 16 words or fewer, whole placard about 50-60 words [W2].
- **D · Group card**, on the front at the left end of a variant row, centre about 1.37 m [W1]: the one property that changes and its letters ("Doors by wear: A new, B scuffed, C burnt" is an example) [W2, E13, W8]; under each variant a small floor tag with its letter and name [E6]. Neither premium kit read here labels its overview [E16]: names and codes are this scene's addition.
- These labels must not look like 1990 signage, because placards are themselves exhibits here (inf.; Q3).

**3.5 Lighting.**
- Two states for the whole scene, switched together, never per exhibit [E1, E13, E4, G3]:
  - **Reading** (default; Red asked to see the assets outside the game's context): even ceiling light, no fog, no film effects, fixed exposure, mid-grey floor and walls, no shadow on a placard [E2, W1, W7, U1].
  - **Game**: the game's troffers and post stack as in scene 1, lamps Steady [P6].
- The alcoves keep game light in both states, so each room compares neutral and in-game at once (inf., after W7).
- A lamp state that a kind is mostly seen under (Striking, Unstable, Dead [P6]) is a variant at that station, not a third mode (inf.).
- Switch: L (bound nowhere in the game or the tour [P4, P9]), plus a wall switch in the hub and at each room entry for touch.

**3.6 Scale.**
- The visitor is the game's player: eye 1.62 m, height 1.75 m, reach 2.4 m, Shift = slow walk, Ctrl = sprint [P1, P2, P4]. It replaces the old free camera at 0.7-2.2 m with Shift = faster [P5] in the museum, because only the real eye and reach judge an interactable as the player will meet it [G2].
- Floor: mid-grey with the 3 m cell grid drawn faintly and a 1 m tick ruler along each lane edge [E2, E8, G2, G3, G4].
- The game's door, window and ceiling sizes everywhere [P1]; a 1.75 m silhouette on the hub wall beside the board [G3, E4].

**3.7 Jump and reset.**
- Spawn in the hub, facing the board and rooms 2 and 3 [E10].
- Desktop keys follow the tour's scheme [P9], so scene 3 has one: F2 = the menu, where the museum is one more entry; in it [ ] step room to room, arriving inside E facing the alcove, 1-7 jump to that room's stations, 0 = the hub [E4, U2, U5, W5]; R = reset the room you are in (R is the game's END RUN key [P4]: the same idea at room scale); L = light (inf.).
- Touch (the tour is desktop-only [P9]): every room is a short walk from the spawn, so the doors are the jump; a reset switch by each room's exit resets that room (inf.). Desktop and touch reach the same functions; keys are only shortcuts.
- One station: use (E or tap) on its placard resets it: doors close and lock as authored, the sash drops, the key goes back on its plinth, the chain goes back on, screens go off (inf.; no source documents a reset control: E14's refill and W10's self-resetting stations are the nearest).
- Nothing resets on its own when Red walks away, so a door can be left open for comparison (inf.).

**3.8 Variants side by side.**
- One row per group: same height, light, backdrop and spacing; one property changes; letters A, B, C [W8, W1, W6, E13, U3]; never two deep [W1, W6].
- Two real axes make a matrix, rows one value and columns the other, the axes named once at the heads (E15's sheet has rows by finish and columns by function but no labels), e.g. doors with rows = wear and columns = type [E15, W8].
- Behaviour variants (prompt distance, tap or hold) as twin lanes with identical objects and a two-sentence board at each head [E14].
- A planned variant that does not exist yet keeps its place as an empty frame with its code [W8].
- Optional A/B stand: one spot that swaps variants in place from its placard [U6] (inf.).

**3.9 Left out.**
- No live Relay, no Warn lamps, no timer, no title, map or room stream: nothing chases or times Red [P5, P6, U5]. An alarm device that would call the Relay shows "tripped" on its readout instead (inf.).
- HUD: the game's own interaction prompt only, because it is part of what is judged; nothing else unless switched on (inf.).
- No free-standing islands, nothing two deep, no vitrines, no room over its 7 stations [W1, W3, W4, W6].

**3.10 Build and load.**
- One bootstrap object, the tour's `Interactables Showcase` [P9], beside the player rig and the light rig; nothing else saved in the scene (inf.: a small scene file opens fast and rarely conflicts).
- One table, one row per station: code, room, kind (the tour's name), staging (front / plinth / alcove), variant letter, try (desktop: the tour's line), try (touch), what it is, Red decides, status, start state. From it MapWorld builds the hub and rooms as cells and edges (bays, fronts, closets, the doors and windows on the fronts), so the game's own loop runs every door, window, key and the EXIT; inspect targets, the screen ads and the alarm card are kit objects placed on the fronts (inf., from P9). Adding a kind = adding a row; regrouping = editing the room column.
- Whether MapWorld can build such a fixed layout is 交互设计's to answer (Q8). Until it can, the tour carries §3.4's lines (its how-to strings), §3.7's jumps and the readouts.
- The same table prints the hub board, the room signs, the placards and Red's pick sheet, keyed by code and kind [E10, U6, W2, W10].
- Only the hub and the room Red stands in are awake; screens and video run only there (inf.; this matters on iPhone and iPad).
- Kit objects pivot at bottom centre with +Z toward the visitor at scale 1; wall and floor pieces pivot at a corner on the 3 m grid [U7, U8, P1].
- Completeness: a kind the tour's classifier knows with no row is a bug [U4]; the tour's list is the check (inf.).
- Inert for the game scenes and the shipped build [P7].

## 4. Layout standard: scene 4, `FrontRoomsStructures.unity` (every new structure)

What each district holds is level_space's [P10]: six districts in order, SP1 the slide on its wall, SP2 the well, SP3 the Level 0 field and its checks, SP4 the Office stack and pit, SP5 the Run! ramp to the dock, SP6 the deep pool terrain; each a skeleton of several types at several heights in one view, never one device in an empty box room (Red, 15:38 and 16:06, in P10). This section is the frame they sit in; where it and P10 differ, P10 wins. Same bootstrap and framework as scene 3; a second table, one row per district.
- **Circulation:** the hub holds one numbered pad per district, in an arc facing the spawn, each under a picture with number, name and one line on what to try [E10, E4]; one walking loop also joins SP1 to SP6 in order and returns to the hub [G5, W5], through passages long enough that no light, fog or geometry bleeds between districts; each district's ambient, fog and white balance switch at its door [P10].
- **Arrival:** a pad lands on P10's arrival point, facing its main view (SP1 on the west deck by the crossing-room arch; SP2 at Level 0's south gallery rail, which is also its overlook); the loop enters each district by a standard 1.0 x 2.1 m door off a 2.9 m passage, so it is judged against the game's ordinary room, never an open plane [G2, P1].
- **Signs:** P10's Chinese signs (what it is, where it would go, 灰盒占位, 要你决定) at the arrival point, one per event in SP3 [P10, E10, W2]. Faces and sizes: 平面视觉.
- **Look and light:** P10's surfaces only (the game's own; porcelain tile in the pool districts) and each district's own light; L steps through that district's keys (SP1 night, SP5 armed / tripped) (inf.); no neutral reading light here, because the mood is what Red judges [E12, E9].
- **Metrics:** floor rulers [G4] and green / orange / red markers [G3] as overlay objects, on M, bound nowhere like L [P4] (inf.), wherever P10 §8 still asks a number; never a grid material on a district (P10 §2), so the grid of E8 and G2 stays on grey proof blocks only.
- **Relay:** no live Relay; static 2.05 m stand-ins only where P10 tests a sight line.
- **Jump and reset:** keys 1-6 = SP n's arrival point; 0 = hub; R = back to this district's arrival point; a return pad beside each arrival point for touch [E4, E10]; a fall into water or a void puts Red back there at once [G5]; in SP1, Esc returns a rider to the tub [P10].
- **Variants:** a size still open stands side by side in the district where P10 builds it so (SP4's four panel heights) [E4].
- **Build and load:** districts are table rows that call level_space's builders [P7, P10]; only the hub and the current district are awake (inf.); tris, renderers and lights per district go to the build log [P10].

## 5. What scene 4's structures need beyond scene 3

1. **Whole-space scale.** A structure is judged at full size from a normal approach: the passage and door before the big space [G2, P1]. Main paths read visibly larger than side paths [G4].
2. **A walking route per district.** First view from the arrival point along a long axis to a lit focal point [E12, W5]; then P10's own route with a goal, a skill check and a reward on the way (SP1: up the tower or the stair core, the ride, out by the tiled steps; SP2: down floor by floor) [G5]; then back to the arrival point without retracing [W5, G5].
3. **Overlooks.** Where P10 gives a rim or gallery (SP2's Level 0 rim and pool gallery, SP6's dome gallery), Red sees the whole before walking down into it [W9, W5].
4. **Detail spots.** A few per district where one material reads at arm's length, lit for it (E12 shows close-ups in place; the count is ours): tile and grout, a dark nosing, a flume flange.
5. **Joins.** Each structure meets the game: stacked floors, a corridor looped back, the standard door [G1, G2]; P10's splices already carry most of them.
6. **Metrics.** Overlays only where a rise, drop, depth or width is still open (§4) [G3, G4].
7. **Safe failure.** Falling is cheap: one step back to the arrival point [G5].
8. **Pacing.** A quiet passage between districts, so each lands fresh (inf., after W1 and W5); a walk through all six will likely exceed the 20 minutes of one visit [W3, W10], so pads are the default and the loop is for a full walk (inf.).

## 6. Open questions for Red (each with a recommendation)

- **Q1 · How scene 3 is grouped.** Four rooms by the game's real kinds (1 Doors; 2 Keys and the EXIT chain; 3 Windows and alarms; 4 Screens and paper: 23 exhibits in 24 bays, plus the four alcoves), or one room per group as you listed them (ten rooms, several nearly empty: no locker exists in the game, phones are test-bed only)? Four rooms mean fewer walks and comparisons across related kinds; ten mean more walking past empty bays. **Recommend four**, splitting a room only when its kinds outgrow its 6 bays [W3].
- **Q2 · Default light.** Scene 3 opens in reading light with L to game light, the alcoves always in game light; scene 4 always in each district's own light, L stepping through that district's keys? Reading light shows each asset on its own, as you asked; a district's light is its mood, which is the point of a structure. **Recommend yes.**
- **Q3 · Label style and language.** Neutral showcase labels, plainly not 1990, designed by 平面视觉. Scene 4's signs are already in Chinese [P10] and the tour's guide is in English [P9]. **Recommend:** the lines for you (what it is, Red decides) in Chinese in both scenes; code, kind name and the Try line in English, as the game shows them.
- **Q4 · A separate "in use" room.** A whole 1990 office where every kind works together [E13, E15]? The tour already shows every kind on the game's own floor [P9] and each room's alcove shows its kinds together. **Recommend not now.**
- **Q5 · Scene 4 movement.** Pads only, or pads plus one walking loop through every district? Pads are fastest; the loop is how a player would meet these spaces. **Recommend both** [G5, W5].
- **Q6 · Metrics overlays in scene 4.** On every district, or only where a number is still open in P10 §8? Overlays everywhere clutter the spaces you judge by mood. **Recommend only where open.**
- **Q7 · How you record picks.** A keep / cut switch at each station, or a printed pick sheet by code that you fill after the walk? A switch saves a step but adds UI and state; the sheet needs nothing new. **Recommend the sheet**: you say "keep 1.2B" and the code and kind do the rest [E1, E6].
- **Q8 · Scene 3's museum rooms.** The tour shows every kind where the game puts it [P9]; the rooms here show them side by side outside the game's context, which is what you asked for, but they need MapWorld to build a fixed layout from a table (cost unknown until 交互设计 checks MapWorld). **Recommend:** keep the tour as menu entries 2-4 and add the museum as one more entry, room 1 first, since doors are the simplest records on cell edges.
