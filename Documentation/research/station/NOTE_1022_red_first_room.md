# NOTE 2026-10-22 15:2x · Red: the station is its own room, and the FIRST room the player enters

**Red, in the 游戏视觉 chat (account A, session `local_5656cffd…`), 2026-10-22 ~15:20 PDT, Red's own words:**

> 同时我需要你调研，当前接待室/警卫室应该是有一个单独的房间墙体设计，而不是直接利用 level 0，要做区分，并且这个房间从关卡设计的角度来说，应该是玩家第一个进入的房间，这样能够让玩家首先对游戏有所认识，这也是从教程设计的角度来说

What it means for this folder:
1. **The station room gets its own wall design.** It does not reuse Level 0's walls (wallpaper print, carpet, troffers). It must read as a different room at a glance.
2. **The station is the first room the player enters,** from the level-design and the tutorial point of view, so the player learns the game there first.

This changes two premises of `20_layout.md` v1.2 (a station placed on Floor 1 off the route) and of the tutorial order in C32 (the locked EXIT is seen on the way to the station, before the key).

- **For the running fix stage (wf_f7becae2-bb3):** finish the r7 reconciliation as planned; its data stays the baseline. In `40_for_red_draft.md`, mark every question about placement on Floor 1, the walk from the start to the station, and "accept PARTIAL on the Floor 1 exits" as **superseded by Red's 15:2x direction (being researched)**. Do not ask Red those questions. The light-leak / rendering-layer question stays: a separate room with its own shell makes it more relevant, not less.
- **Research workflow:** `station-first-room` (游戏视觉, launched 15:2x). It covers real 1990 reception and guard rooms, game first rooms, the level and tutorial order, and the wall directions. It writes `60_first_room_research.md` and a Chinese decision page, `61_for_red_first_room.md`.
- **Other lines (叙事设计, 关卡设计, 教程设计 via 总控, 系统设计):** put your constraints or ideas in `NOTE_1022_first_room_<line>.md` in this folder. The synthesis stage reads every such note before it writes.
