# Zone lamps · 40 · Execution brief for the API-key session (Cursor)

Written by 游戏视觉, 2026-10-31. Red, verbatim: "我需要你写一个关于不同当前房间和level布局下，适配不同灯research的方案，按照年代，后室主题关卡，r让cursor来执行".

**Goal.** Every room type and every Backrooms-style level in FrontRooms gets a lamp that a US office building of c.1990 (≤ 1993, no brands) really had in that kind of space. The lamp stays inside the game's lamp logic: flicker, dim, dead, the Relay warning and the C22 blackout.

**Budget.** You run on Red's API promo credit, so the steps below need no new research. All of it is already on disk.

## 1. Read, do not redo

| What | Where |
|---|---|
| Every zone, its fixture, its owner | `00_inventory.md` |
| Period research per family | `10_office.md`, `10_pool.md`, `10_run.md`, `10_other.md` |
| 2–3 options per zone, built and rendered | `20_design.md`; kits and generators in `/Users/redwang/FrontRoomsVisualWork/zone_lamps/kit/`; the clone `/Users/redwang/FrontRoomsVisualWork/proj_zonelamps`; sheets in `.../zone_lamps/sheets/` |
| What Red is asked to pick | `30_for_red.md` |
| The ceiling troffer study (L00–L10) | `/Users/redwang/FrontRoomsVisualWork/lamp_options/docs/30_for_red.md` |
| The landed Level 0 lamp | L03 via `Assets/Scripts/Rendering/FrontRoomsTrofferKit.cs`; lens 1.8; `L03_DARK_BANDS` 0.85 (Red 10-31: "中间档没有问题") |
| The landed Run! / stairwell kits | `research/room_visuals/run_b_production/` (wrap, twin-head, sealed-beam) |
| The pool level | line `bathroom` (yours); `research/bathroom_rooms/` |

## 2. The mapping (by level and room, period-true)

The picks marked **Red?** are open in `30_for_red.md`. Get them first (§3, step 0).

| Level / room (Backrooms theme) | Ceiling | Lamp | State |
|---|---|---|---|
| Level 0, yellow rooms, Standard 2.9 m | lay-in 2'×2' (Red, route B) | **L03** air-handling 2×4 troffer, lens 1.8 | LANDED, no change |
| Level 0 Low 2.4 m | lay-in | L03 in rooms (LO-A); the 4 ft wraparound only in modules 关卡设计 tags back-of-house / storage (LO-C; the wrap kit is in main) | **DECIDED, Red 10-31: "低矮区按推荐"** (needs a back-of-house module tag from 关卡设计) |
| Tall hall 5.4 m | high deck | **H-C: keep today's L03** (no change; no MH family, no ceiling holes) | **DECIDED, Red 10-31: "这里用当前现有的"** (on the tall-hall sheet) |
| Office floors, Standard | lay-in 2'×2' | **O-C** lensed-indirect pendant (`ZL_PEND`): an 8 ft body hung with its bottom at 2.30 m; the ceiling glows evenly with a brighter patch over each lamp, carried on the lamp's own renderer so a dead lamp darkens its patch; no hole | **DECIDED, Red 10-31: "办公室选择O-C"** (this drops the 10-02 opal-lens decision) |
| Run! armed / tripped | painted concrete | **armed: R-B bare-tube strips with wire guards** (64 / 2274·822·14 / 42 tris, 0.83 MiB; every dead or blackened tube end shows; replaces the landed wrap in Run! rooms) / **tripped: T-A + T-B combined** — the self-contained twin-head battery units (T-A, landed) stay at the entry and the goal ends; the heads between are T-B remote heads on brackets fed by EMT along both walls from the entry cabinet; 8 heads and the warm floor pools unchanged | **DECIDED, Red 10-31: armed "run选择R-B", tripped "这里应该是TA和TB相结合"** |
| Stairwell (safe zone) | concrete | twin-head + 4 ft wrap (LANDED), wrap lens stays 2.4 (the 1.8 question only arose to match R-A's wrap; Run! now uses R-B) | no change unless Red reopens |
| Station, exit vestibule, start room, title corridor | lay-in | L03 | no change |
| Pool level (L8, line `bathroom`) | per D1 | **P1-A** indirect MH wall boxes + **P2-A** 300 W wet niches every 3 m with caustics | Red? (rec), after D1 |

**O-C notes (from `30_for_red.md`):**
- 8 / 776·328·96 / 20 tris, 1.67 MiB, 3 draws per lamp (2 on phones).
- It needs a new additive glow shader in main. That is shared rendering code: card 游戏视觉 before you write it.
- The pendant clears the 2.03 m headroom rule, but no collider may hang over chase routes.
- GlassRT does not see the additive layer: list it for the combined test.
- Era: best practice of the day, though uncommon in ordinary office buildings.

**Era rule for any new level:** take the fixture the period used in that kind of space from the `10_*.md` tables, and keep it inside one lens renderer so the lamp logic works.

## 3. Steps

0. **Ask Red once** (Chinese, background first, each option as the player feels it, cost, recommendation) for the picks still open above (Office is decided: O-C), or accept "全部按推荐". Quote the answer in your handoff.
1. **Promote the picked kits** from `zone_lamps/kit` into `Assets/Resources/Props/Lamps/<Family>/`: LOD0 that holds at 0.3 m, LOD1/2, a phone mesh, materials, `.meta` GUIDs unused in main.
2. **Generalize the factory.** Turn `FrontRoomsTrofferKit` into a family registry, for example `FrontRoomsLampKit.Build(zone, height, theme, …)`, that returns the lens renderer the lamp logic drives. Keep `FrontRoomsTrofferKit.Build` working for the landed callers. Each family carries its own state curve (metal halide: a re-strike delay after dead or blackout, no instant flicker). Phones get the reduced tier. WebGL stays on its own track. Count GlassRT instances against the High cap (2,048).
3. **Write a map contract for 关卡设计** (`/Users/redwang/FrontRoomsVisualWork/apply/zone_lamps_contract/`): `BuildFixture` passes zone height and theme (and module tags for back-of-house) to the factory, plus the ceiling holes the hole families need. Collision stays whole. They land it, not you.
4. **Ask other lines:**
   - 声音设计: ballast hum per family and the MH re-strike sound.
   - 系统设计: the MH re-strike duration as a rule.
   - 平面视觉: nothing, unless a lamp carries a label.
5. **Package** one v2 package per family (layout and REFUSED/ALREADY gate of `apply/door_dust_throw.sh`). The gate is Roslyn macOS + iOS 0, with each `--override` quoted separately, plus a dry run and a rehearsal. Add a CLAIM / LANDED in LANDINGS. Tests wait for the combined round (Red: "节流留给统一测试").

## 4. Ownership for this task

- 游戏视觉 hands you, for this task only: `FrontRoomsTrofferKit.cs` and a new `FrontRoomsLampKit.cs`, `Assets/Resources/Props/Lamps/**`, and the RoomStream lamp code.
- Before you change `FrontRoomsSurface.shader`, `FrontRoomsRenderSetup.cs` or anything GlassRT, send a card to 游戏视觉 (`remote/zone_lamps/OUTBOX.md`) and wait for the answer.
- Map files are 关卡设计's: contracts only.

## 5. Report (cheap; `/Users/redwang/FrontRoomsVisualWork/workspace_repo/PROTOCOL.md`)

- Overwrite `/Users/redwang/FrontRoomsVisualWork/workspace_repo/remote/zone_lamps/STATUS.md` (≤ 12 lines) at each step end.
- Cards to local lines go in `remote/zone_lamps/OUTBOX.md`.
- Read `remote/zone_lamps/INBOX.md` at each step start.
- No acknowledgements.
