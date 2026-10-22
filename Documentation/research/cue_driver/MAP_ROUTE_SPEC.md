# Wall-cue route: map-side spec (关卡设计调研, v1.0.1, 2026-10-22)

The route the wallpaper arrows point along, as data the map hands the cue driver. It implements Red's decisions **C24 = A** (2026-10-22) and its amendment **A20** (2026-10-22, "选 1：拿钥匙前箭头指向值班台，拿到后指向 EXIT"). It builds on C22, where each floor has an EXIT that leads one floor deeper.

| Owner | What |
|---|---|
| 关卡设计调研 | This spec and its rules, plus review of the implementation |
| 关卡设计 | The code (`FrontRoomsWayfinding`, `FrontRoomsWayCueDriver`, shared with tutorial M5 `BeaconPath`), after the Alpha |
| 平面视觉 · 信息设计 | The driver interface (`CUE_DRIVER_INTERFACE.md` v0.2.2) and the timing table; the cue motion (A+M4+V1) is not yet confirmed by Red (10-22: "再看一次其他方案"), which does not change the per-cell data this spec defines |
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
- On Floor 1 the station is 2–6 cells past the EXIT (M4). The arrows lead the player past the EXIT to the station. After the take they turn back to the EXIT 2–6 cells away.
- On Floor 2+ the station is 10–20 cells from the arrival and off the EXIT's shortest route (M4).
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

Each cell then points along the way that minimises *walk to the edge of the built map + straight-line remainder*.

Why this rule:
- It is C24's rule made exact. Of the border doorways it prefers near ones, and only those that actually lower the distance to the target, because a doorway on the far side carries a large h.
- h never overestimates the real walk, so as chunks stream in the route only gets more exact. A rebuild when the target chunk arrives switches to the real seed.
- Seeds on the far side of glass are excluded with the glass edge itself.

## 4. Per-cell direction (what goes into `_FR_WayCells`)

- **dir:** for a reached cell c, look at its four orthogonal neighbours n that are reachable over an allowed edge. Pick the one with the least field(n) + stepCost(c→n).
  - On a tie, keep c's previous dir if it is still among the best (hysteresis: arrows never flip between equal routes).
  - Otherwise use the fixed order +x, +z, −x, −z.
  - Diagonal steps in the octile field come out as one of their two orthogonal halves.
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
- After the take, the beacon is off and the field switches to the EXIT.

One field, two readers, so the arrows and the sound can never disagree (A20).

## 6. During a chase (unchanged from §9.6)

The pressure route still points at shuttable doors that lead away from the Relay (LD R6). It may read the Relay, because in a chase the player has seen it. Its fallback is the calm field above (not the old ladder). The cue never points at glass in a chase either.

## 7. Before C22 lands (interim)

If Wayfinding ships before the EXIT, the floor has no target, so every cell gets message 0 and stage 1 is carried by the lamps and groan. The old "nearest unvisited threshold" ladder is **not built**; it would be thrown away within days.

Forged FLOW from tier 4 (EGRESS, 10-03) is **in question; see C29** (systems.json 2026-10-22j). 叙事设计 found that a forged arrow would pull away from the station and the EXIT, against A20. Its recommendation is to retire forged FLOW, and Red decides in 叙事设计's chat. Whatever the answer, this spec defines only the true route.

## 8. Acceptance tests (for 关卡设计's implementation)

Run these in the map validator, 100 seeds × floors 1–3, in key-not-taken and key-taken states.

| # | Test | Pass |
|---|---|---|
| R1 | **Follow the arrows.** From every built cell with message 1, step along dir | Reaches a target or frontier seed within ⌈field/3⌉ + 2 steps; never crosses a window edge; never enters X except as the last step from N; never loops |
| R2 | **Monotone** | field strictly decreases along dir, ignoring the +4/+9 door costs taken on that step |
| R3 | **Never reads the Relay** | Build the field with the Relay at 5 random positions and states: the field hashes are identical |
| R4 | **Frontier** | With the target chunk unbuilt, the R1 walk ends on a built-map edge cell whose straight distance to the target is less than the start's |
| R5 | **Key switch** | After `StationKeyTaken` no cell's dir follows the station field, and the room set re-points within one rebuild |
| R6 | **Station first** | With the key not taken, R1 from the floor's start door ends at the station TV, not at N |
| R7 | **Message-0 share** | Per zone height, the share of built cells with message 0 is logged; above about 25 % in Tall zones, report it (§9.6: fix it with a map rule, not the cue) |
| R8 | **Determinism and cost** | Same seed, same field; rebuild ≤ 4 ms desktop at buildRadius 2 (log max) |

**In play,** once it lands, a **follower** bot mode (RELAY §13 lists it; not built yet) will walk the arrows from the start door and report:
- time to the station and to the EXIT;
- whether it ever met glass;
- stage-1 seconds with message 0, by zone height.

The bots are mine; I build the follower when Wayfinding lands.

## 9. Sources

- Red's decisions: `system_design/systems.json` C22, C24, A20 (2026-10-22g).
- Pursuit rules: `RELAY_PURSUIT_REDESIGN.md` §9.2, §9.6.
- The exit: 关卡设计's `~/FrontRoomsMapWork/notes/c22_stairwell_plan_v2.md` D3–D6.
- Station, beacon, key: tutorial `research/tutorial/20_requests.md` M3–M8.
- Driver: `CUE_DRIVER_INTERFACE.md` v0.2.2.
- Costs: `FrontRoomsMapWorld.OpeningCostBetween`, `FrontRoomsMapPathField`.

SP = spec proposal: a number not yet confirmed by Red or the peer, which may be tuned.
