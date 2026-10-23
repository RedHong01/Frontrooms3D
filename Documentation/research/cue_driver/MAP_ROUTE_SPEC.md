# Wall-cue route: map-side spec (关卡设计调研, v1.0.2, 2026-10-22)

The route the wallpaper arrows point along, as data the map hands the cue driver. It implements Red's decisions **C24 = A** (2026-10-22) and its amendment **A20** (2026-10-22, "选 1：拿钥匙前箭头指向值班台，拿到后指向 EXIT"). It builds on C22, where each floor has an EXIT that leads one floor deeper.

**v1.0.2 (2026-10-22 16:5x).** Follows three answered decisions and the pre-review of 关卡设计's first Wayfinding draft (clone `~/FrontRoomsMapWork/proj3`, 16:08). Changes:
- C29: forged FLOW is retired (§7).
- C31: the motion is A + M4 + V1 (owner row).
- A26: Floor 1's station is the first room (§1, R6).
- "Beyond the cap" now includes the built cells the target pass never reaches (§2, §3).
- A restricted frontier pass allows a diagonal step only when both of its halves are in the pass (§3).
- `pathMetres` is defined (§5).
- R1's step bound is fixed for an octile field, and R4's pass rule now holds on real maps (§8).

The rules for where the arrows point are otherwise unchanged.

| Owner | What |
|---|---|
| 关卡设计调研 | This spec and its rules, plus review of the implementation |
| 关卡设计 | The code (`FrontRoomsWayfinding`, `FrontRoomsWayCueDriver`, shared with tutorial M5 `BeaconPath`), after the Alpha |
| 平面视觉 · 信息设计 | The driver interface (`CUE_DRIVER_INTERFACE.md` v0.2.4) and the timing table. The cue motion is A + M4 + V1 (C31, Red 10-22 15:05), which does not change the per-cell data this spec defines |
| 系统设计 | The pursuit rules (`RELAY_PURSUIT_REDESIGN.md` §9.6) |

Where this spec and §9.6 differ, §9.6 decides the *pursuit* behaviour (when cells turn, step or retract). This spec decides *where the arrows point*.

It replaces §9.6's interim target, "the nearest unvisited threshold", together with its fallback ladder. That rule needs no implementation, unless Wayfinding ships before C22's EXIT (see §7).

## 1. Targets

| Floor state | Target cells (the field's seeds) | Source |
|---|---|---|
| The floor has a station and its key is not taken | The station TV's cell (tutorial M4/M5) | A20 |
| The key is taken (`StationKeyTaken`), or the floor has no station | Cell **N**: the maze cell on the EXIT door's edge N→X. X itself is a reserved cell, not built for nav or hearing (C22 plan v2 D3/D6) | C24 = A |
| No EXIT exists (before C22 lands) | None. Message 0 everywhere, so the lamps and groan carry stage 1 | §7 |

Why the station comes first:
- **Floor 1 (A26; M4 as amended 10-22):** the station is the first room, the first map room behind the start door. The EXIT is 3–6 cells from its doorway, in a straight sight line.
  - Until the take, the station room's cells point at the TV.
  - After the take, every cell points at the EXIT.
  - The Relay is not released before the take and the player leaving that room (M7), so stage 1 cannot start earlier. In play, Floor 1's arrows therefore only ever point at the EXIT. The station field still feeds the beacon (§5).
- **Floor 2+ (M4):** the station is 10–20 cells from the arrival, off the EXIT's shortest route, and at least 6 cells from it.
- The arrows and the TV beacon never pull two ways (A20), because both read the same field (§5).

## 2. The field

A path field over **built cells only**. It never makes the map generate a chunk. It uses the hunter's own machinery (`FrontRoomsMapPathField`, octile, distances in metres), with these edge rules:

| Edge | Cost | Note |
|---|---|---|
| Open, Arch | 0 | |
| Door: open or broken | 0 | Open means at `DoorPassableDegrees` or more, as `OpeningCostBetween` |
| Door: ajar | +4 m | Hearing's cost |
| Door: shut | +9 m | Hearing's cost |
| Door: locked, and the player has no key that opens it | impassable | M3: the floor key opens every locked door on its floor. Floor 1 has no locked interior doors. The station is always reachable without keys (M4 / L5) |
| Window: intact **or broken** | impassable | "The cue never points at glass" (§9.6). A broken frame is climbable, but pointing at it would steer the player into a noise device and the climb. This rule differs from hearing, where an intact window is +9 |
| Wall | impassable | |
| Into X (the stairwell cell) | impassable | N is the seed; the arrow in N points at the EXIT door |
| Into a trigger room's armed area (v2 `ModuleTrigger`) | +30 m per entry (SP) | Never through a device room unless there is no other way. 30 m is the stage-1 band, so a detour of up to 30 m is preferred over the device |

- **Never reads the Relay.** No input depends on its position, state or route. That is what makes the calm route not a radar. (The chase pressure route is §6 and is allowed to read it.)
- **Cap:** 150 m (SP). That covers the built 5×5 chunks at buildRadius 2 (120 m square) with room for detours. M5's beacon needs about 90 m. A cell beyond the cap gets the frontier rule (§3), and failing that message 0.
  - **Beyond the cap** means not reached from the target within 150 m.
  - That includes the built cells the target pass **never reaches**: a part of the built map that joins the target only through unbuilt chunks, glass or a locked door.
  - Those cells take the frontier rule too. They are not message 0 just because the target is built.
- **Seeds:** cost 0 at each target cell.
- **Rebuild only on a trigger, never per frame:**
  - `PassageRevision` changes (streaming, door and window state);
  - the target switches (`StationKeyTaken`, `FloorChanged`);
  - the target's chunk becomes built.

  For cost, measure it in the bots' `pathFieldMeanMs`. At a 60 m cap the hearing field measured 0.65 ms mean and 1.4 ms max over 340 cells (10-08). At 150 m expect about 2–4 ms desktop. On WebGL it may be split across frames behind `#if UNITY_WEBGL && !UNITY_EDITOR`, with desktop unchanged.

## 3. Target not built yet (the frontier rule)

C24 says: "while the EXIT's chunk is unbuilt, the nearest built border doorway toward it". The EXIT cell is known before its chunk exists, because `FrontRoomsExitPlanner` chooses it from data (C22 plan D4). The station is kept through revisit shifts (M4), so its cell is known too.

**Rule:**
1. Seed every **frontier cell** instead of the target. A frontier cell is a built cell with a walkable edge (§2, not glass) to an unbuilt cell.
2. Give each seed the cost h = the straight-line metres from that unbuilt neighbour's centre to the target cell.
3. Run the same Dijkstra.

When the target is built, this pass covers only the cells beyond the cap (§2). The target pass keeps the rest.
- In a restricted pass, a diagonal step into a cell is allowed only when **both orthogonal halves are in the pass too**.
- Otherwise a cell reached diagonally may have no lower neighbour in its own pass. Its dir would then point uphill, and the next cell's dir straight back, making a two-cell loop at the cap's edge (R1, R2).

Each cell then points along the way that minimises *walk to the edge of the built map + straight-line remainder*.

Why this rule:
- It is C24's rule made exact. Of the border doorways it prefers near ones, and only those that actually lower the distance to the target, because a doorway on the far side carries a large h.
- h never overestimates the real walk, so as chunks stream in the route only gets more exact. A rebuild when the target chunk arrives switches to the real seed.
- Seeds on the far side of glass are excluded with the glass edge itself.

## 4. Per-cell direction (what goes into `_FR_WayCells`)

- **dir:** for a reached cell c, look at its four orthogonal neighbours n that are reachable over an allowed edge. Pick the one with the least field(n) + stepCost(c→n).
  - On a tie, keep c's previous dir if it is still among the best (hysteresis: arrows never flip between equal routes).
  - Otherwise use the fixed order +x, +z, −x, −z.
  - Diagonal steps in the octile field come out as one of their two orthogonal halves. So a walk along the dirs can take up to √2 × field / 3 steps, not field / 3 (R1).
- **message:**
  - 1 (FLOW) where c was reached, by the target or the frontier seeds;
  - 0 where it was not (cap, sealed pockets, glass-only exits);
  - 0 in X and in the start area.
- **Target cell itself:**
  - in N, the dir points through the EXIT door into X;
  - in the station cell, the dir points at the TV's wall if the TV hangs on a cell edge, otherwise message 0 (the TV is in view there).
- **Room set:** the cue turns only the player's room set, using §9.2's shared `RoomSet`. This spec provides the dir for every built cell, so a room-set change never needs a rebuild.

## 5. Shared with the TV beacon (tutorial M5)

`BeaconPath(playerCell, out pathMetres, out shutOpenings, out portalPos)` reads **the same field** while the key is not taken (M5).
- Its `portalPos` is the first opening that leaves the player's room set along these dirs.
- Its `pathMetres` is the field's walking metres from the player's cell to the TV **without** the +4/+9 door costs (S2 counts those through `shutOpenings`).
  - That means octile metres along the route the field found, plus the straight remainder beyond the built map.
  - It is **not** 3 m per orthogonal step along the dirs, which overstates a diagonal route by up to √2 (41 %), and S2 turns metres into gain and low-pass.
  - SP.
- After the take, the beacon is off and the field switches to the EXIT.

One field, two readers, so the arrows and the sound can never disagree (A20).

## 6. During a chase (unchanged from §9.6)

The pressure route still points at shuttable doors that lead away from the Relay (LD R6). It may read the Relay, because in a chase the player has seen it. Its fallback is the calm field above (not the old ladder). The cue never points at glass in a chase either.

## 7. Before C22 lands (interim)

If Wayfinding ships before the EXIT, the floor has no target, so every cell gets message 0 and stage 1 is carried by the lamps and groan. The old "nearest unvisited threshold" ladder is **not built**; it would be thrown away within days.

Forged FLOW from tier 4 (EGRESS, 10-03) is **retired** (C29, answered in 叙事设计's chat; systems.json: "the arrows always tell the truth"). A forged arrow would have pulled away from the station and the EXIT, against A20. This spec defines only the true route, and nothing else writes message 1.

## 8. Acceptance tests (for 关卡设计's implementation)

Run these in the map validator, 100 seeds × floors 1–3, in key-not-taken and key-taken states.

| # | Test | Pass |
|---|---|---|
| R1 | **Follow the arrows.** From every built cell with message 1, step along dir | Reaches a target or frontier seed within ⌈√2 × field / 3⌉ + 2 steps (v1.0.2: an orthogonal walk of an octile field; was ⌈field/3⌉ + 2); never crosses a window edge; never enters X except as the last step from N; never loops |
| R2 | **Monotone** | field strictly decreases along dir, ignoring the +4/+9 door costs taken on that step |
| R3 | **Never reads the Relay** | Build the field with the Relay at 5 random positions and states: the field hashes are identical |
| R4 | **Frontier** | With the target chunk unbuilt: the R1 walk ends on a frontier seed, and its last step leaves the built map over a walkable border edge that is not glass. Log the share of walks whose seed is straight-closer to the target than the start; below 90 %, report it. (v1.0.2: "closer than the start" is no longer a per-walk pass rule. A pocket that opens only backwards rightly ends further away) |
| R5 | **Key switch** | After `StationKeyTaken` no cell's dir follows the station field, and the room set re-points within one rebuild |
| R6 | **Station first** | **Floor 2+**, key not taken: R1 from the arrival cell (in front of the stairwell's door) ends at the station TV, not at N. **Floor 1 (A26)**: R1 from every cell of the first room ends at the TV. After the take, R1 from the room's doorway on the EXIT sight line reaches N within 8 steps and crosses no window |
| R7 | **Message-0 share** | Per zone height, log the share of built cells with message 0, with frontier-routed cells counted on their own. Above about 25 % in Tall zones, report it (§9.6: fix it with a map rule, not the cue) |
| R8 | **Determinism and cost** | Same seed, same field; rebuild ≤ 4 ms desktop at buildRadius 2 (log max) |

Notes for the tests:
- **Until M4 lands** there is no station, so key-not-taken equals key-taken (target N). R5 and R6 need M4.
- **Tie hysteresis** makes a dir depend on the build before it. Compare hashes (R3, R8) between **fresh** field instances built in the same order, not between one instance's successive builds.
- **Bot runs plan no EXIT** unless `-autopilotExit` is passed (C22). In a plain baseline run every cell is message 0. The follower below runs with `-autopilotExit`.

**In play,** once it lands, a **follower** bot mode (RELAY §13 lists it; not built yet) will walk the arrows from the start door and report:
- time to the station and to the EXIT;
- whether it ever met glass;
- stage-1 seconds with message 0, by zone height.

The bots are mine; I build the follower when Wayfinding lands.

## 9. Sources

- Red's decisions: `system_design/systems.json` C22, C24, A20 (2026-10-22g); C29, C31, A26, A27 (2026-10-22z14).
- Pursuit rules: `RELAY_PURSUIT_REDESIGN.md` §9.2, §9.6.
- The exit: 关卡设计's `~/FrontRoomsMapWork/notes/c22_stairwell_plan_v2.md` D3–D6; `FrontRoomsExitPlanner.FrontOf` (N = X + (0, −1), so N's dir is +z).
- Station, beacon, key: tutorial `research/tutorial/20_requests.md` M3–M8 (M4 and M7 as amended 10-22 for the first room).
- Driver: `CUE_DRIVER_INTERFACE.md` v0.2.4.
- Pre-review of the first draft: `~/FrontRoomsResearchWork/review/way_draft_1608/` (the draft's two files as of 16:08, and a Python port of its field that checks the two §2/§3 fixes).
- Costs: `FrontRoomsMapWorld.OpeningCostBetween`, `FrontRoomsMapPathField`.

SP = spec proposal: a number not yet confirmed by Red or the peer, which may be tuned.
