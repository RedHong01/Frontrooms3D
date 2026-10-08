# Relay pursuit: audit and redesign (v2)

Status: **proposal v2, 2026-10-03. Runtime v2 is not enabled.** On 2026-10-04 the systems pass landed an isolated, pure-C# pacing policy core (`Assets/Scripts/FrontRoomsMap/FrontRoomsRelayDirectorPolicy.cs`); it is not wired to the current timed-release hunter and does not count as runtime v2. Every number in §4–§12 is a starting point for playtest (**SP**) unless it is marked **fixed**. v2 replaces v1 (same day) wholesale; §15 lists what changed and why.

**Systems landing 2026-10-04.** The policy core covers the director vocabulary and deterministic rules for `Arm`/first-run RELAX, Attention gains and decay, weighted call estimates, PENDING/drop handling, queued device calls, quiet-player sweep requests, Pressure, peak/cap → SUSTAIN → FADE, handoff budget, and `NotifyAway` → RESTORE/RELAX. It is deliberately an adapter seam: no old release/leash behavior, map navigation, sound implementation, lamp look, wallpaper, narrative, or mobile build was changed. Wiring waits for Step 0 and the map-owned path/Arrive contracts.

**Level-design Step 1 landing 2026-10-04.** `FrontRoomsMapPathField` now supplies the player-rooted 45 m octile field, opening costs, bounded source fields, Relay-rooted estimates, `WarningDistance`, hysteretic `WarnStage`, and the 10 Hz `PlayerSeesRelay` hook. These values are measurement-only: no v2 director, lamp, audio, sight-lock, or movement behavior is enabled. The field invalidates on player-cell, streaming-topology, and door/window edge-state changes. Step 0 remains the only reconciled runtime evidence (54/54 PASS); Step 1 runtime acceptance is still pending the Unity compile/test pass.

Written by the chat 怪物追捕机制设计审计 (systems), with:
- **关卡设计** (map / Relay code owner): level design, feasibility. Peer chapter `research/relay_pursuit/20_level_design.md`.
- **Design the narrative of the phosphor wallpaper print** (narrative): fiction. Peer chapter `research/relay_pursuit/30_narrative.md`.
- v2 was produced by a workflow: code verification of every v1 claim, a research pass (sources in §17), three competing proposals, a judge panel (player experience, buildability, robustness), and a review round of 50+ findings. All blockers and majors from that round are applied here.

Peer chapters are **out of date** against v2. `20_level_design.md` still has the per-Relay-cell field cache, the Omen/Dip/Sag reach rows, `CopyPathAhead`, the herald, the 30-cell Summon rule and "window = the only way into a tall zone". `30_narrative.md` still has "leaves the way it came", the EAR GROUND glow, the ALARM ROUTE ink, "the lamps dim because it draws power", and foil on the frame. Both owners update them (§13).

Figma: design file `0tCbAiVUlrPId3RWd9LRif`, page 2099:76, section `FRONTROOMS · RELAY PURSUIT · AUDIT + REDESIGN` (2441:3804) at x 7897, y 26369, frames RP01–RP10 (2441:3805 … 2441:3883). **Updated to v2 on 2026-10-03**: every frame, with new diagrams from `research/relay_pursuit/sim/mksvg_v2.py` (octile path field, the W bands, the v2 approach timeline, the ±60° cone, the v2 Attention curve and pacing). The audit numbers keep the caveats in §2.3.

**Red's brief (2026-10-03).** Right now the player has no room to explore the Backrooms and has to keep running. Pursuit should follow a logic:
- the Relay starts tracking the player's sound only after something calls it, such as entering certain rooms or breaking glass;
- when it comes, the lamps where it is start to flicker irregularly.

**Red's follow-up, the same day.** Given in the wallpaper chat and relayed to this one; it replaces the second point.
- Make it a **staged warning**. The lights do **not** dim where the monster is.
  1. When it is fairly close, the lamps in the **player's** room flicker.
  2. Closer still, the player hears **muffled footsteps**.
  3. When it **sees** the player, a clear, recognisable but non-jarring sound says "you are targeted". Then the chase formally begins.
- All Relay state text stays off the HUD (assist only).
- The chase wave fires on CHASE only.
- Narrative: **EGRESS**.

---


> **Landed after this draft was written (2026-10-03, 关卡设计, uncommitted):**
> - **Lamp override v1, MapWorld side:**
>   - `LampLevel` / `LampBaseLevel` (NoLamp −1);
>   - a pure `LampModeOf`;
>   - persistent `SetLampMode`;
>   - `SetLampOverride` with `LampFx {Dip, Sag, Warn}`. Defaults Dip 0.3/1.2, Sag 0.6/1.5, Warn 0.08/0.2. Stacking is MIN; ReduceFlashing floors attack and release at 0.5 s;
>   - `LampDipped` and `FixtureChanged` (0.02 hysteresis);
>   - WebGL parity 28/28, MapInteractionTests 126/126.
>
>   The hunter side's Step 1 measurements (`WarnStage`, `PathDistanceToPlayer`, `PathOpeningCost`, `WarningDistance`, `PlayerSeesRelay`) are now landed. `TargetAcquired` and the Warn scheduler still wait for the v2 director/sight slices.
> - **The empty-stamina Shift leak is fixed:** a dry player is winded until 1 s of stamina is back.
> - The v1 doc is kept as `RELAY_PURSUIT_REDESIGN.v1.md`.
> - **Two Relay bugs fixed ahead of v2** (Red's go; landed in main 2026-10-03 ~22:53, uncommitted for Red to commit; by 关卡设计):
>   1. **Wander and search never break a door.** `planThroughDoors` is kept by every replan (`Replan`). A door shut across a no-doors leg is walked round; if there is no way round, the leg ends (Wander → Listen, Search → next spot). `TickSearch` skips spots it can no longer reach. `TickBreak` resumes the state that started the break.
>   2. **LoseTrack uses only what it saw.** The per-frame real-cell door feed is gone. A crossing counts only between two ticks that both saw the player. It follows through a door only if it saw the crossing within 1.0 s of its last sight; otherwise it goes to the last-seen point.
>
>   Verified by this chat on the copy whose hunter file is byte-identical to main: interaction tests 143/143 (7 new checks), RelayNavTest 60/60 with the rewritten door rule PASS (seen and unseen), autopilot 2554 PASS with 0 errors. 关卡设计 also reports 20388, the glass scenario, seeds 100/100 and the designer, fixture, rig and caption suites passing.
>
>   **Still true after the fix:** a door the player shuts *within earshot* is a 14 m noise that sends the Relay into Hunt, and a Hunt can break it. A door shut during the 1.5 s of chase without sight is not heard (§2.2). v2 §5 and §7 address both.

> - **Wallpaper hints are being redirected (Red, via the wallpaper and narrative chats):** the hint becomes the wallpaper pattern's own elements turning, not overlay glyphs (`research/wallpaper_motion/32_pattern_native_hints.md`). The timing and commit rules for the ink in §7 and §9 carry over; the glyph wording will follow that doc.

## 1. Summary

1. **Why the player always runs** (§2). The Relay never leaves. It is released on a timer, placed 27–45 m of walking behind you, and re-placed behind you the moment you outrun the built map. It hears a sprint step through any wall within 36 m and goes to your exact feet. It sees 360° with no reaction time. Every door you shut is both a beacon and something it breaks. Its 2D stingers, drone, heartbeat and tension bed tell you it is near at any range. Walking, the one quiet verb, is never taught.
2. **The rule the player learns:** *Noise and alarms call it, and walking is quiet. Your room's lights, then its muffled steps, then one clear chime warn you before any chase. Once it loses you, it gives up and leaves.*
3. **Two brains** (§4), as in Alien: Isolation. A new pure-C# **director** (the 1990 alarm panel) owns the run's pacing: Attention, Pressure, phases, budgets, RELAX. The **hunter** owns senses and movement and never receives the player's position, only a call estimate.
4. **The loop** (§5): Dormant → Away → (called) Arrive → Investigate → cautious look, or lock-on → Chase → search → give-up → Withdraw → Away → RELAX. Three states are appended: Away 7, Arrive 8, Withdraw 9. Both leashes are deleted.
5. **What calls it** (§6): the panel's Attention (sprinting, barging and slamming doors add up; walking and easing doors are almost free), and readable alarm devices (glass, foiled windows, alarm doors, detector rooms…), which call to the **device**, never to you. A quiet player gets a supervisory sweep of a neighbouring zone after a dry spell (Red's Q2).
6. **Its ears** (§7): one path field, rooted at the player, serves hearing, the warnings and the Relay's audio. Sound gives an estimate, not GPS. Shut doors cost +9 m.
7. **Its eyes** (§8): a ±60° cone round a head it owns, a spotting meter, a gate that holds every lock-on until you have had ≥ 3 s of lamps and ≥ 1 s of steps, a fixed 0.7 s lock-on hold, and a lose meter that a glimpse no longer resets.
8. **The ladder** (§9): banded with hysteresis, never on at stage 0, never at the Relay's own lamps. Each cue has one meaning and one sound family; the lock-on chime is the only tonal chime in the game.
9. **It leaves** (§11), always: a Withdraw that cannot soft-lock (timeout and a terminal fallback), then a guaranteed RELAX of 100 s at tier 1 in which noise cannot call it.
10. **Tiers make it better, never the warnings shorter** (§12). Tier = unique cells explored / 150. The stall rise is deleted. TIER leaves the always-on HUD.
11. **Step 0 first** (§13): seeded bots measure today's game before any change. Every number here is a guess until then.

---

## 2. Audit, corrected

Every v1 claim was re-checked against the code. Line numbers are those of the brief (code at `aba5f77`, 12:26) unless marked *wt* (working tree, about 13:00). They drift; owners find code by symbol.

### 2.1 What the code does now

| # | Fact (corrected wording) | Where | Effect on the player |
|---|---|---|---|
| A1 | Released `releaseDelaySeconds` = 3 s (scene value; no tier changes it) after the start door has **fully** swung shut. The door only starts to shut once the player is outside the start rooms and 4 m past it. Release then waits for `Arrive` to find a valid spot. v1's "6.6–11.8 s into play" is dropped: the one kept report predates the door gating and shows 3.1 s. The tooltip "in the first furnished room" is stale | Game.cs:779-780, :124; MapHunter.cs:167-172; FrontRoomsHunter.cs:15 | No calm opening. |
| A2 | `Arrive` places it 9–15 cells (27–45 m) of walking from the player's cell, **counting shut doors as passable**, so it can start just behind a shut door. The spot must be built, fit its body, and keep its 1.6 m eye and 1.95 m head out of the player's view. Order: an entry marker behind the player; a random cell behind; the nearest entry marker anywhere in the band, **possibly ahead** (no behind test, added in c69c7d7); the nearest in-band cell | MapHunter.cs:405, :413-427, :429-441 | It can appear close in a straight line, or ahead of you. |
| A3 | `Noise(source, radius)` tests flat straight-line distance only; walls, doors and floors don't count. Walking is silent. Door and glass noises come from the door or window. Ignored in Dormant, Chase and BreakDoor. Tier 1: sprint step 26 × 1.4 = 36.4 m every 0.3 s; door open **or** shut 14 × 1.4 = 19.6 m; glass 40 × 1.4 = 56 m. Tier 5 (×1.6): 58 / 31 / 90 m. Hearing 1.4 and the radii are **serialized in the scene**; glass is a const | MapHunter.cs:289-296; FrontRooms3D.unity ~1062-1077; Game.cs:174 *wt* (`GlassNoiseRadius`) | Hears you through any wall. |
| A4 | A heard noise sends it to the exact source point | `HuntToward(world.CellOf(source), source)` | Sound is GPS. |
| A5 | Sight: one ray from its 1.6 m eye to the camera (1.62 m), 12 m in 3D, not tier-scaled, every direction, no reaction time, no light term. Any collider but the player or its rig blocks it, unbroken glass included. On the frame it sees you it switches to Chase and moves at chase speed. A door being broken is finished first; it resumes Chase only if the break began in Chase, else Hunt | MapHunter.cs:208-212, :248-249, :976-1002 | No "almost seen" moment. |
| A6 | **Fixed 2026-10-03 (see header); as audited:** Wander picks a cell 6–14 cells of walking from **itself**, avoiding shut doors at planning time only. `Follow` breaks any shut door on its route **in any state**; replans call `Plan` with `throughDoors = true`; `TickBreak` resumes Hunt for every state but Chase. So a wandering Relay breaks doors and turns into Hunt by itself | MapHunter.cs:473-487, :526, :559, :563-583, :570, :588, :953 | Idle roaming sounds like pursuit. |
| A7a | Timed leash: every second in Listen/Wander/Hunt/Search, if it has neither seen nor heard you for 45 s (**counted from release**) and is > 30 cells of door-ignoring walking away, or unreachable, it re-Arrives 9–15 cells from you | MapHunter.cs:91, :175-176, :193 | Getting away doesn't last. |
| A7b | **Unbuilt-cell leash (missing in v1):** in **any** state, Chase and BreakDoor included, it re-Arrives the same frame its cell stops being built. With buildRadius 2 that is 3 chunks off your chunk on one axis (17–24 cells, 51–72 m), so A7b binds long before A7a | MapHunter.cs:192; MapWorld.cs:687-694 | A tether that drags it behind you. |
| A8 | No state ever takes it off the map | state machine | No exploration window. |
| A9 | Tier = 1 + (new zones − 1) / 4: tier 2 on the **5th** new zone. It also rises after every 120 s without a new zone; the clock restarts after each rise and a rise is never undone, so a player who waits climbs T1→T5 in 8 min. Same-height zone borders are invisible. v1's "tier 3 at 60–74 s" can't be checked (no tierLog in the repo) | LevelProfile.cs:183; Game.cs:910-917, :1646-1650 | Lingering is punished, and so is walking on. |
| A10 | Each lamp's temperament is rolled once from the odds of the tier **at which its chunk was generated**. A chunk regenerated by a revisit shift (after 30 s) takes the tier current then. T1 62/20/10/5/3 (steady/stutter/failing/dead/dim), T5 40/25/18/12/5. Lights fade between 13 and 16 m (XZ) and are off beyond; the lens glow flickers at any range. No link to the Relay | MapWorld.cs:44-53, :1218-1223, :1388-1407 | Backtracking shows worse lamps; lamps mean nothing. |
| A11 | **Refuted as written.** The Relay readout (state text including CHASE and BREAKING DOOR, plus `RELAY nn M`) is already an assist, off by default, toggled with T in Display Settings (O), saved in PlayerPrefs. The HUD still shows `ZONE nn / TIER n / <height>` at all times | Game.cs:91-94, :333, :1597-1600; `roomMetaText` Game.cs:1845 *wt* | TIER reads as an escalation meter. |
| A12 | The CalmHint "Shift to sprint. About 5 seconds, and it hears every step" shows only for the first 9 s of the run, usually before release. The pause card also lists E DOOR and HOLD E BREAK GLASS, but both are noises (19.6 m and 56 m at T1) and shut doors get broken. Walking is silent and faster than hunt (3.2 vs 2.6 m/s) and is never taught | Game.cs:148-149, :1547, :1603 | The escape verb is also the beacon. |
| SPEEDS | Chase 4.2 m/s at T1, tier-scaled to 4.49 / 4.79 / 5.08 / **5.42** at T2–T5, against sprint 5.5 and walk 3.2. Hunt 2.6, wander 2.08 and search walk 1.82 are not tier-scaled | LevelProfile.cs:166-170; Game.cs:153 | Sprint margin falls from 1.3 to 0.08 m/s. |
| STAMINA | 5 s of stamina, refilling after 1 s at 1/s. **Since `ef061ae` (12:56):** run dry, the player is winded until 1 s of stamina is back, so holding Shift no longer leaks one-frame sprints | Game.cs:170-178, :939-953 *wt* | Best sustained average ≈ 4.25 m/s: below chase from T2. |
| LOSTSIGHT | **Door feed fixed 2026-10-03 (see header); as audited:** After 1.5 s without sight in Chase (any glimpse resets the timer) it runs LoseTrack. If the player crossed a door edge between 1 s before the last sighting and that moment, up to 1.5 s after it lost sight, it hunts through that door, breaking it if shut; else the last-seen point. `doorInto` is written every frame from the player's real cell: a live feed | MapHunter.cs:180-185, :207, :304-312 | Doors don't break pursuit. |
| SEARCH | Listens 1.1 s on arrival, walks (1.82 m/s) to up to 3 spots in that room, listens 1.1 s at each, gives up after 15 s, listens 2 s, wanders. Tiers stretch it ×1.15–1.6, up to 1.76 s per listen and 24 s | FrontRoomsHunter.cs:34-36; LevelProfile.cs:141-142 | It lingers one room away. |
| CATCH | `catchDistance` 0.7 m and requires `SeesPlayer` | MapHunter.cs | — |
| BREAKDOOR | T1: blows at 0.5, 1.0, 1.5, 2.0 and 2.4 s; the door gives at 2.5 s; it moves on at 2.75 s. Tiers: 2.2 / 1.9 / 1.6 / 1.3 s. A broken door can never be shut again | MapWorld.cs:1146-1156 | Each encounter strips cover. |
| S1 | The presence drone runs whenever Released, any state, within 30 m in a straight 3D line through walls; occlusion only −6 to −12 dB | RelaySound.cs:59, :62-71, :91-95, :114 | "It is near" all the time. |
| S2 | The 2D heartbeat follows straight-line proximity within 20 m, through walls | SoundDirector.cs:414-422 | Same. |

### 2.2 More causes the verifiers found

| Cause | Evidence | Effect |
|---|---|---|
| The unbuilt-cell re-Arrive (A7b) is a short tether | MapHunter.cs:192; MapWorld.cs:687-694 | After ~25 s of walking away, it is put back 27–45 m behind you. |
| A wandering or searching Relay breaks doors and becomes Hunt by itself | MapHunter.cs:563-583, :953; SoundDirector.cs:444, :526, :541-548 | Door blows, then Hunt, Search and Lost stingers with no player action. |
| Shutting a door on its wander route provokes it | Game.cs:813-816; MapWorld.cs:1882-1888 | The hide verb calls or invites it. |
| The 45 s leash clock starts at release; any noise resets it | MapHunter.cs:91, :175-176, :193, :294 | No grace built into the leash. |
| Arrive ignores shut doors; the entry fallback has no behind test | MapHunter.cs:405, :434-441 (c69c7d7) | It can appear just past a door or ahead. |
| Release clicks, presence, heartbeat and tension all start on release and never return to the floor | SoundDirector.cs:378-396, :414-422, :549-553; RelaySound.cs:59, :114 | Permanent "near". |
| Release doesn't depend on the player | MapHunter.cs:167-170; Game.cs:925-929 | The first sprint or door becomes a Hunt. |
| Holding Shift on an empty bar leaked sprint frames | Game.cs:869-879 (fixed by the winded rule in `ef061ae`) | A panicking player stayed a beacon. |
| A Shift tap is heard at once (`stepTime` carries over from walking); sprinting into a wall makes noise | Game.cs:887-894 | No short, safe burst. |
| Shutting a door is a beacon, not a barrier | MapWorld.cs:1882-1887; Game.cs:813-817; MapHunter.cs:391, :526 | Doors backfire. |
| 2D stingers say "it heard you" at any range | SoundDirector.cs:541-548; fmod_frontrooms.py:824 | The audio is a readout. |
| Tension never returns to calm after release; any Hunt lifts it | SoundDirector.cs:378-396; fmod_frontrooms.py:657 | No all-clear. |
| Relay footsteps carry 42 m straight-line through walls | fmod_frontrooms.py:801; RelaySound.cs:62-71 | Wander (59 % of its time) is audible. |
| Every proximity cue is straight-line | RelaySound.cs:114; SoundDirector.cs:383, :421 | Far behind a wall sounds close. |
| Latest noise wins: a sprint drags its target at 3.3 Hz, then a long search | MapHunter.cs:289-296 | Keep moving or run again. |
| Stalling raises the tier, and with it hearing | Game.cs:910-917; LevelProfile.cs:139, :167-170 | Standing still is punished too. |
| A misleading comment says hunter hearing uses a wall test | RelaySound.cs:88-89 | Tuning mistakes. |
| Tier scaling erases the sprint margin | LevelProfile.cs:166-170 | Running must start earlier and never stop. |
| A shut door delays a chase by ~2.75 s and LoseTrack follows through it | MapHunter.cs:304-312, :526 | Doors only slow it. |
| Broken doors stay open for good | MapWorld.cs:1146-1156 | Cover shrinks every encounter. |
| Hiding behind a door you shut calls it to that door | Game.cs:813-817; MapHunter.cs:291-295 | The worst move looks like the best. |
| A one-frame glimpse restarts the 1.5 s lose timer | MapHunter.cs:207 | Corners rarely work. |
| Search lingers in the room you fled into, longer at higher tiers | MapHunter.cs:319-341, :371-387 | It stays next door 15–24 s. |
| Chase starts at full speed with no "it saw me" beat | MapHunter.cs:208-212, :248-249 | Players move pre-emptively. |
| TIER and zone count on the HUD; stall rises have no in-world sign | Game.cs:1845 *wt*, :911-917 | "Keep moving". |
| Backtracking shows worse lamps; ambient ticks are false alarms | MapWorld.cs:1221; SoundDirector.cs:358-374 | Lamps mean nothing. |
| After the LoseTrack fix (Red's go, 2026-10-03), a door shut during the 1.5 s of chase without sight is not heard, because `Noise()` ignores Chase. The old real-cell door tracking used to hide this | MapHunter.cs `Noise` (State == Chase) | A shut door in that window goes unnoticed. v2 §7 covers it: in Chase without sight it hears, and only moves the last known point. |

### 2.3 Measurements: what each number is and is not

| v1 number | What it is | What it is not |
|---|---|---|
| Spawn median 24 m straight line (p10 17, p90 32) | The distribution of every cell in the 9–15-cell ring (`sim/hearing_stats.py`) | The real spawn distribution: Arrive's filters (built, body fit, visibility, behind preference, markers) are ignored. |
| "465 cells inside the sprint radius" | The area of a 36.4 m disc in 3 m cells (≈ π·12.1²) | A property of the map. |
| "98.5 % of spawn cells in earshot (min 85 %)" | Ring sampling, same caveat as above | Measured spawns. |
| "Sprint heard over a median of 30 cells (p90 42); glass ≈ 430 cells" under the new model | `sim/new_hearing.py` with +9 m on **every** door edge | Reality: open and Ajar doors and modules are not modelled; the real spread is larger. |
| Autopilot state shares (Wander 59 %, Listen 18 %, Hunt 13 %, Search 8 %, BreakDoor 1.5 %, Chase 0.5 %); 9/17 hunts at 13–19 s; 3/17 caught | A trace of 17 runs, 16 of them from other sessions' scratchpads (`figdata.json` is the only snapshot) | Difficulty data: the autopilot never hides or shuts doors; the route RNG is unseeded; the runs predate today's tier, readout and entry changes. |
| "Tier 3 at 60–74 s (20 seeds)" | 关卡设计's report | Checkable: no tierLog in the repo. |
| Map table (7 % dead ends, 16 loops per zone, 22 border edges and 7 doors per zone, 39 loops per 100 cells, zone median 63 cells) | The Python port `mapgen.py` on 20 seeds | The generator: modules, columns and keys are left out. |
| "A field recompute visits 300–700 cells, like one chase Plan" | An estimate | Profiled, on desktop or WebGL. Step 0 profiles it. |
| Arches 1.1–1.8 m × 2.2 m; 1.57 m panels don't block sight, 1.65 m would | Reported | Checked. The 1.65 m panel clears the 1.6/1.62 m ray by ~3 cm, and Arrive also tests a 1.95 m head. |
| v1 tier-1 cycle times and G1 targets (60 % / 35 % calm) | Design estimates | Predictions. No simulation. |
| "A warning burst touches ≤ 20 fixtures" | An estimate | Checked against large carved rooms or tall halls. |
| "Nav test 60/60", "map 100/100" | Last known results | Re-run for this doc. |
| Fire-alarm facts (verification, pre-alarm, door-holder release, coded chimes, glass-break detectors, ALARM WILL SOUND bars) | The narrative chapter's research, now checked (`30_narrative.md` §7, with URLs) | Verified for 1990: door-holders released on alarm, alarm verification, amber TROUBLE lamps, coded chimes, window foil and sash reed switches, combined fire and burglary panels, alarm push-bar exits, shock and acoustic glass-break detectors. Not verified: pre-alarm on US panels in 1990 (the concept exists from 1985), the exact wordings 'ALARM WILL SOUND' and 'WALK, DO NOT RUN' before 1990, and that foil was silver. The door-holder 'thunk' is a Foley choice: the magnet only lets go, and the sound comes from the closer and latch. |

### 2.4 What changed in the code today (after the brief)

- **`aba5f77` (12:26):** `FrontRoomsSettings` with **REDUCE FLASHING** and **CAPTIONS** rows; hunter, game, world and nav-test edits.
- **`ef061ae` (12:56): lamp override interface v1 has landed** in `FrontRoomsMapWorld`: `LampFx { Dip, Sag, Warn }`, `SetLampOverride(cell, kind, multiplier, hold, envelope?)`, `RemoveLampOverride`, `LampLevel`, `LampBaseLevel`, `SetLampMode`, `FixtureChanged`, `LampDipped` (fires on **every** override start). Level = temperament × the **lowest** active multiplier. Default envelopes Dip 0.3/1.2 s, Sag 0.6/1.5 s, Warn 0.08/0.2 s; with Reduce Flashing no attack or release is under 0.5 s. Nothing raises an override yet. AUDIO_CONTRACT lists these as "in main". The same commit added the **winded** stamina rule (above) and 154 lines of interaction tests.
- **Working tree, uncommitted:** `FrontRoomsCaptions.cs` (untracked) and a caption panel in `FrontRooms3DGame.cs`: up to three lines, each tagged AHEAD / LEFT / RIGHT / BEHIND from the player's view. Another session owns this; check before touching Game.cs.
- Step 1's measurement hooks (`WarnStage`, `WarnStageChanged`, `PathDistanceToPlayer`, `PathOpeningCost`, `WarningDistance`, `PlayerSeesRelay`) now exist in `FrontRoomsMapHunter`; they are not consumed by v2 behavior. `TargetAcquired`, `Attention`, `Summon` and the director remain absent.

### 2.6 Step 0 baseline, measured (2026-10-04 runs, audited 2026-10-07)

The full report is `Verification/relay-baseline/summary.md`. Two auditors checked it, and I recomputed the headline numbers. The Relay measured is the v1 hunter. The warning stages are v2's bands computed on v1 movement. n = 3 seeds per cell, so every figure is an order of magnitude.

| Measure | Value | Reading |
|---|---|---|
| Runs ending in a catch | 45 / 52 (the seed-7 door-spammer pair excluded) | The bots barely flee; this describes the rules, not players |
| Chases ending caught / lost | 45 / 9 of 54 | Escape is rare |
| Chase start distance | median 5.9 m walking, 7.7 m sprinting (2.2–12 m) | No warning before contact: 360° sight at 12 m |
| Chases with no call (from Wander or Listen) | 16 of 31 for walking bots | Half the walking player's chases have no cause to name (G2) |
| Quiet bot | caught 6/6; 6.4 chases and about 20 re-arrivals per 10 min | v1 never leaves a quiet player alone (G8) |
| Real hunts | 62 (reports say 110: 48 were door-break resumes): 32 door, 21 sprint (inferred), 9 after a lost chase | Sprint noise never reached the counters |
| Re-arrivals under 42 m walking | 51 of 86 (17 at ≤ 30 m) | v2's W ≥ 42 m rule is binding |
| Doors broken | 57, every attempt succeeded, 48 during Hunt | v2's licence and WALL rules target this |
| Evader escapes (chases > 0.6 s) | 0.3 s reaction: 2/3 T1, 1/3 T5; 0.6 s: 1/3 T1, 0/3 T5 | Below G5; T5 is hopeless under v1 |
| Stage-0 share, on map (pooled) | walking 0.81 T1 / 0.78 T5; sprinting 0.64 / 0.60; noisy 0.74 / 0.73 | The bands are not always on; the problem is sudden contact, not constant nearness |

**Not measured by Step 0:** glass (the noisy bot never tried a window), sealed zones (the closed-zone bot never shut a door), single-door spam, sprint as a logged cause, the G3 gate, and G8 stage-1 entries. The 15 measurement fixes are in `summary.md` §13 and were sent to 关卡设计. "54 PASS" checked only that the map ran and the Relay was released, not that each bot did its job.

### 2.5 What already works and is kept

- The Relay body, A* detours, door breaking and `CrossingPoint` routing (nav test, last known 60/60).
- The rig hooks: `SetListenTarget`, `DoorBlow`, `Step`, `DoorSqueeze`, `CeilingHeight`.
- Audio contract events `StateChanged`, `DoorBlow`, `Caught`, `Arrived` (exists, Hunter.cs:157; v2 moves when it fires, §5), `TierChanged` (exists, Game.cs:47).
- The lamp override layer from `ef061ae`, with base × MIN stacking (v2 builds on it unchanged, §9).
- Single-acting hinged doors (landed 2026-10-02).

---

## 3. Design goals, fixed constraints and tests

### 3.1 Fixed constraints (inputs, not choices)

- **Red's brief and staged warning** (header). Lamps never dim where the Relay is.
- **No Relay state on the HUD.** The readout is an assist, off by default. The phosphor ink's chase wave fires on CHASE only. Fiction: EGRESS.
- **Contracts.**
  - `HunterState` is append-only (Dormant 0 … Wander 6 keep their numbers); v2 adds Away 7, Arrive 8, Withdraw 9. The director's phase is not a `HunterState`. `Passage` gains `Ajar` at the end.
  - Sound only through C# events and read-only properties (`Documentation/AUDIO_CONTRACT.md`), no new AudioSources. 声音设计 owns `Assets/Scripts/Audio`; 关卡设计 owns `FrontRoomsMapHunter`, `FrontRooms3DGame`, `FrontRoomsMapWorld`; 游戏视觉 owns the lamp look; the wallpaper chat owns the ink.
  - WebGL-only optimisations are platform-gated; logic is identical on every platform.
  - Photosensitivity: ≤ 3 flashes per second with **default** settings.
- **Map facts:** 3 m cells, 8×8-cell chunks (24 m), buildRadius 2 (5×5 chunks built), ≈ one Voronoi zone per chunk (mean ≈ 64 cells), doors and windows only on zone borders (a tall zone's borders are windows; tall–tall borders resolve to maze edges), arches inside zones.
- **Before any landing:** check Codex and other-session activity on the files touched (memory rule; three sessions edited Game.cs and MapWorld.cs today).

### 3.2 Goals

| Goal | Test (SP) |
|---|---|
| G1 **Room to explore** | Time at warning stage 0 (what the player perceives) ≥ 60 % at T1 and ≥ 35 % at T5 for the noisy bot; quiet and evader bots reported separately; the Away share reported beside it |
| G2 **Cause → effect** | Every call logged with its cause; ≥ 8/10 playtesters name the cause after a chase |
| G3 **Readable approach** | 100 % of lock-ons, re-acquisitions included, preceded by ≥ 3 s of continuous stage ≥ 1 **and** ≥ 1 s of continuous stage 2 (touch lock-ons logged separately); StateChanged(Chase) never before the cue ends |
| G4 **Sound is an estimate** | Estimate error grows with path distance and openings (§7) |
| G5 **Escape is a decision** | A quiet walker who breaks sight escapes ≥ 50 % of chases at T1 and ≥ 35 % at T5 **with a 0.6 s reaction delay and a 2 s stamina bar**, using corners and eased doors only (no sanctuaries in v2.0); "shut the door and walk away" ends an investigation |
| G6 **It leaves, and you can tell** | Every encounter ends in Away within budget; Withdraw reaches Away within 25 s; RESTORE plays exactly when stage ≥ 1 was felt and it is truly Away |
| G7 **The ladder never lies** | No stage while not OnMap; no Warn override and no WarnBurst at stage 0; no burst lamp within 2 cells of the Relay; no stage-2 gap > 1.2 s; no 2D stinger calls; no wake reprint at stage 0 |
| G8 **Quiet is not empty** | Quiet bot: ≥ 1 stage-1 encounter per 10 min at T1 and ≥ 2 at T5 (needs the sweep, Q2) |
| G9 **No new contracts broken** | §3.1 |

---

## 4. Pacing director (new `FrontRoomsRelayDirector`)

**Rules.**
- **Two brains.** `FrontRoomsRelayDirector` is a new pure-C# class in `Assets/Scripts/FrontRoomsMap` (map chat), ticked by `FrontRooms3DGame` before the hunter, testable in EditMode. It owns Attention, Pressure, the phase, the encounter budget, the RELAX and first-run clocks, the dry-spell clock, the queued trigger and the sweep. The hunter owns senses, movement, the warning stage and **Arrive placement** (placement needs physics and the player's eye; it reads the player's position only to stay away from it, never to target it).
- **Interface** (on the hunter): `Arm()`, `Dispatch(estimate, errorCells, cause)`, `Sweep(zone)`, `SendAway(reason)`, plus events. The director hands the hunter a call estimate or a sweep zone, never the player's cell. The hunter still runs without a director (nav and interaction tests): no budget, no FADE, no sweep; Away only through Withdraw or a test call.
- **Phases** (director only; `Phase`, `PhaseChanged` for tests and bots):
  - **RELAX:** the Relay is Away. Noise adds **nothing** to Attention (it stays 20). Devices still call at once (RELAX protects you from noise, never from alarms you choose to trip). No dry clock.
  - **BUILD-UP:** Away. Attention at the threshold calls. The dry clock runs, paused in sanctuaries.
  - **PENDING:** a call is waiting for a valid Arrive (§5). Dry clock paused, no sweep, Attention held at the threshold. At most 10 s, then the fallback in §5. Every pending second is logged.
  - **ENCOUNTER:** from Arrive to the end of search or chase.
  - **SUSTAIN:** 4 s after a Pressure peak; whatever the Relay is doing continues.
  - **FADE:** no new calls or re-dispatches. The Relay finishes its current look or chase. Sight switches to the **Withdraw rules** (forward cone, spot time ×2, lock-on only within 6 m). A chase lost in FADE ends with a cautious look (1 spot), not a full search. A noise gets one cautious look at most. Speed never changes.
  - Then the give-up beat, a patrol from tier 3 (not after FADE), Withdraw, Away, RELAX.
- **Pressure** is fed only by cues the player perceived. Highest applicable rate: stage 1 +1/s; stage 2 +2.5/s; the player sees the Relay (`PlayerSeesRelay`, §8) +4/s; almost seen (meter ≥ 0.5, counted only while the stage-2 audio is audible or `PlayerSeesRelay`) +5/s; Chase +6/s; TargetAcquired +15 once. No decay inside an encounter; 0 when RELAX starts.
- **The encounter goes to FADE, or ends, on the first of:**
  - Pressure ≥ peak → SUSTAIN → FADE;
  - on-map seconds ≥ onMapCap → FADE; past **onMapCap + 15 s**, any chase ends on the first 0.5 s of broken sight, and Withdraw ignores lock-on and leaves on its first occluded frame (§11);
  - lock-ons reach lockOnCap (only chases lasting ≥ 5 s count) → FADE after that chase;
  - one chase lasts chaseCap → FADE, that chase's lose threshold drops to 1.0 s (a logged backstop);
  - the look or search finds nothing → give-up beat (the normal end);
  - Withdraw reaches Away, or the Relay's cell stops being built (→ Away at once).
- **Away reasons** (explicit): exit reached; build edge; unbuilt cell; watched 6 s then occluded; Withdraw timeout. A **handoff is not an Away** (§5).
- **On Away:** `Restore` if the player felt stage ≥ 1 this encounter (§9); Attention = 20; Pressure = 0; RELAX. After an unbuilt-cell Away (you outran it), RELAX is **half** length: running off is not the best reset.
- **Re-dispatches** are on-map device triggers and handoffs. Attention is frozen while the Relay is in Arrive or on the map; its own ears take over, so nothing is heard twice.
- **Queued trigger:** one slot. A device tripped when it cannot re-dispatch (budget spent, FADE, Withdraw) sounds at once and is queued; it goes out the moment the Relay is Away, with the CALLED tick (§9) so the arrival can be traced to it. Latest trip wins.
- **First run:** Dormant until the start door has fully shut, then `Arm()`: Armed and Away, and a 60 s RELAX. The 3 s timed release is deleted. No trigger rooms in the first zone.
- **Predicates:** `Armed` = not Dormant (the autopilot reads it). `OnMap` = Listen, Wander, Hunt, Search, Chase, BreakDoor or Withdraw. `Released` becomes an alias of `OnMap`, read by the rig, the door leaf's Relay body, the audio and the assist.

**Numbers.**

| Parameter | T1 / T2 / T3 / T4 / T5 (SP) | Why |
|---|---|---|
| peakThreshold | 100 / 105 / 110 / 115 / 120 | At T1 ≈ 10 s of chase after a lock-on, or ≈ 40 s at stage 2, peaks it. |
| Pressure rates | stage 1 +1/s; stage 2 +2.5/s; you see it +4/s; almost +5/s; Chase +6/s; TargetAcquired +15; no decay in an encounter | Only perceived cues; no farming by dipping to stage 0. |
| sustainSeconds | 4 s | L4D Sustain Peak 3–5 s. |
| onMapCap | 75 / 80 / 90 / 100 / 110 s; hard end at cap + 15 s | A noisy player can never rebuild "never away". |
| Re-dispatch budget | 1 / 1 / 2 / 2 / 3 per encounter | Couvidou: cap resets. |
| lockOnCap | 2 / 2 / 2 / 3 / 3 (chases ≥ 5 s) | Getting tagged on purpose doesn't farm FADE. |
| chaseCap | 25 / 27 / 30 / 32 / 35 s | Logged backstop; never changes speed. |
| relaxSeconds | 100 / 90 / 80 / 70 / 60 s (half after an unbuilt-cell Away) | 60–120 s suits exploration (Q1). |
| Attention in RELAX | gains 0; stays 20 | No loaded call when RELAX ends. |
| First-run grace | 60 s RELAX from Armed | A calm opening. |
| PENDING | ≤ 10 s, then fallback | No silent dead calls. |
| Calm share (estimate) | noisy T1 ≈ 70 %, quiet T1 ≈ 85 %, noisy T5 ≈ 50 % | From the cycle, not simulated; Step 0 measures. |

**Sources.** Booth, L4D (AIIDE 2009); Thompson, *The Perfect Organism* and *Revisiting the AI of Alien: Isolation*; MCV/Develop, *A tiger in the office*; Shacknews on Evolve; Couvidou, Dishonored 2 (Game AI Pro Online 2021).

---

## 5. Loop, states, Arrive, handoff and doors

**Rules.**
- **States** (append-only): Away 7, Arrive 8, Withdraw 9. Wander stays as PATROL (sweep and tier-3+ patrol only). Hunt stays as INVESTIGATE (docs name only).
- **Readout names for the in-between beats** (no new states): the lock-on hold reports the state it began in with `LockingOn = true`; the "almost" stop reports its state with `Spotting ≥ 0.5`; the give-up beat reports Listen and raises `GaveUp(pos)`.
- **Loop:**
  ```
  Dormant ─(start door shut: Arm)─▶ Away ─(call / sweep)─▶ [PENDING] ─▶ Arrive (backstage)
     ─(step-in: Arrived)─▶ Listen 1.5 s ─▶ Hunt (to the estimate)
     ─▶ Search: cautious look ── nothing ──▶ give-up beat ─▶ [Patrol T3+] ─▶ Withdraw ─▶ Away ─▶ RELAX
             │ meter full (gate passed)
             ▼
        lock-on hold 0.7 s ─▶ Chase ⇄ BreakDoor ─(LoseTrack)─▶ full search ─▶ give-up beat …
  Warnings run beside every OnMap state (§9). Arrive and Away are stage 0.
  ```
- **Arrive is backstage**: placement plus the arrive delay. Not OnMap: rig hidden, no audio, stage 0. `Arrived(pos, tag)` fires at **step-in** (today it fires at placement; ping 声音设计).
- **Leashes:** the 45 s leash (A7a) and the unbuilt-cell re-Arrive (A7b) are deleted. An unbuilt cell under the Relay now means Away at once (outside Chase), or LoseTrack in Chase and Away if the last known point is unbuilt too. Only a fresh call, a budgeted handoff or the sweep places the Relay near the player.
- **Arrive placement** (in the hunter).
  - Hard rules, never relaxed: built; the body fits; unseen from the player's eye at its 1.6 m eye and 1.95 m head; warning distance **W ≥ 42 m** (§7: path field with shut doors at +9 m, or three times the straight line, whichever is less); a route from the entry to the estimate exists with **shut doors passable** (they become OPEN licences, below).
  - Soft rules, dropped in this order: (a) Relay entries before plain cells; (b) its route never comes closer to the player than the estimate; (c) ≥ 3 cells inside the built window; (d) far side of the estimate from the player; (e) outside the player's forward half-plane (dot < 0.3); (f) 8–16 cells of walking from the estimate, widened last to 6–20.
  - The behind-less nearest-entry fallback (MapHunter.cs:434-441) is removed.
  - Retry every 0.5 s while PENDING. At 10 s: the best candidate with all soft rules dropped and W ≥ 36 m (still beyond the stage-1 exit), unseen. If even that fails, the call is dropped and logged: Attention = threshold − 20 (no refund of the cue already played); a dropped device call goes to the queued-trigger slot.
  - Step-in re-check when the delay ends: W ≥ 39 m and unseen, else re-pick through the same retry.
- **Handoff** (replaces the leash): only while investigating; the new estimate (an on-map device trigger) is > 20 cells of walking away; the Relay is unseen, at stage 0 and W ≥ 30 m. It is a **direct OnMap → Arrive** transition: no Away hooks, no Restore, Pressure and the on-map clock kept, one re-dispatch spent. Otherwise it walks to the new target (also one re-dispatch). Never in Chase, BreakDoor, FADE, Withdraw or a search after a sighting.
- **Doors: three actions.**
  - **OPEN** (new): latch click and a 1.0 s push, leaving the door Ajar (30°). Heard by the player only under the stage-2 rules; source Relay (no Attention). Licensed for the doors that were shut on its route **at the moment of dispatch**, and for an exit door in Withdraw (§11). This is how a call reaches a zone the player has closed.
  - **BREAK** (blows, breakDoorSeconds, the door ends broken): licensed only by perception: in Hunt, a door the heard sound came through (on the route to a heard estimate at the moment of hearing); in Hunt or Chase, a door it saw the player cross or heard move.
  - **WALL**: any other shut door (one shut later, unperceived). Plan around if a route ≤ 1.5× exists. Otherwise stop at the door and listen 2 s, then: in Hunt, the give-up beat and Withdraw ("shut the door and walk away ends an investigation"); in Patrol, Search or Withdraw, drop that leg.
  - `Follow`'s replans keep the original plan's `throughDoors` flag. `TickBreak` resumes the state that started the break. A lock-on during BreakDoor abandons the break (door stays shut) and starts the hold. All asserted in the nav test.
- **Windows:** the Relay crosses a broken or pried window with a 0.6 s frame step, matching the player's 0.6 s climb.
- **Speeds:** investigate 2.6, patrol and sweep 2.1, search walk 1.8, withdraw 2.6 m/s, none tier-scaled; all below the 3.2 m/s walk. Chase per §8.
- **After a catch:** today a catch ends the run; the next run starts with the first-run grace. If catches stop being lethal: Away, a 120 s RELAX, the next entry different from the last two, a re-randomised search order.

**Numbers.**

| Parameter | Value (SP) | Why |
|---|---|---|
| arriveDelay T1..T5 | 3.0 / 2.75 / 2.5 / 2.25 / 2.0 s | Kept. |
| Arrive distance | W ≥ 42 m; ≥ 39 m at step-in; ≥ 36 m in the 10 s fallback | Outside the 36 m stage-1 exit: stage 1 always comes first. |
| From the estimate | 8–16 cells of walking (6–20 last) | It comes to the noise; the walk in takes ≈ 10–18 s. |
| PENDING | retry every 0.5 s; 10 s timeout; drop → Attention = threshold − 20 | No permanent dead call; noise is never free. |
| Listen on step-in | 1.5 s | A beat to orient. |
| Handoff | > 20 cells; unseen; stage 0; W ≥ 30 m; −1 re-dispatch; no Away hooks | One threshold everywhere (LD chapter's 30 goes). |
| OPEN | 1.0 s, latch audible under stage-2 rules, leaves Ajar 30° | Calls reach closed zones without breaking the door verb. |
| WALL door | replan if ≤ 1.5×, else listen 2 s, then give up (Hunt) or drop the leg | A shut door is a real barrier to what didn't perceive you. |
| Relay window step | 0.6 s | Windows are not a chase trap. |
| Speeds | 2.6 / 2.1 / 1.8 / 2.6 m/s | Walking escapes anything but a chase. |

**Sources.** Thompson (Alien: teleports twice in 12–18 h; backstage nudges); Vandal, RE2 Mr. X (walks the real path); Booth (Relax); Welsh, *Looking for Trouble*.

---

## 6. What calls it

**Rules.**
- **The panel** (Attention 0–100, in the director) hears the whole building only while the Relay is Away. The game reports `Disturb(kind, point, source)` with source Player, Relay or World; only Player counts. Door moves by the Relay (OPEN, push, break) and the timed swing after a key unlock are not Player.
- **Gains:** walking, turning, looking, picking up a key, crossing an open, broken or pried window: 0. Ease a door +1; key unlock +1; sprint step +3 (first 0.3 s into a sprint, then every 0.3 s; needs ≥ 0.5 m of movement); barge +8; slam +12; window pry +8; inside a sanctuary 0; anything during RELAX 0.
- **Decay:** −3/s at T1 down to −2/s at T5, 3 s after the last gain.
- **Calling (BUILD-UP):** Attention reaching threshold[tier] dispatches to an **estimate**: the strength-weighted centroid of the last 10 s of Player disturbances, **snapped to the nearest reachable disturbance cell** (never a wall, unbuilt cell, start area, sanctuary or sealed tall zone). Error by kind: sprint 2 cells, door or pry 1, device or glass 0. Each repeat call into the same zone within 180 s shrinks the error by 1 cell (Alien's tightening stalk).
- **CALLED** (outside Red's three stages; Q3b): an Attention call plays one dry, non-tonal relay latch tick from the ceiling of the player's room. The first 3 calls of a run add a short annunciator buzz (explicit first, subtle later). A device call plays the device's own local sound at the device instead. A queued device call plays the tick when it finally goes out.
- **Devices** (instant triggers), readable before you commit: smashing a window; a foiled window opened or broken; an alarm push-bar door; a detector room; an electrical room; a glass floor. They call to the **device** (error 0), and the device always sounds at once.
  - BUILD-UP or RELAX: dispatch now (no floor).
  - On the map in Listen, Hunt, Search or Patrol: a re-dispatch while the budget lasts.
  - In Chase or BreakDoor: heard only as a noise (moves the last known point when it has no sight); no budget.
  - Budget spent, FADE, Withdraw, PENDING: queued (§4). Worst case: the call goes out when the Relay is Away (≤ onMapCap + 15 s + the walk out after the trip).
- **Trigger kinds unlock by tier** (§12): windows plus the kind Red picks at tier 1 (Q6), then one more per tier.
- **The quiet player: a supervisory sweep** (Q2, asked before Step 0). After drySpell[tier] of BUILD-UP with no call, outside a sanctuary, the director sends a sweep to a zone next to the player's, **chosen along the player's recent heading** (so walking forward doesn't dodge it). Arrive hard rules apply. It patrols 4 waypoints for sweepSeconds; every waypoint and leg keeps W ≥ 21 m from the player when planned, the nearest at W 21–30 m: aimed at stage 1, planned to avoid stage 2. Ears and eyes work as usual; a sighting makes it an ordinary encounter. Then the give-up beat and Withdraw. No extra tell: the lamps are the tell.
- **Placement rules** (zone-local validator, landed before any trigger content):
  - a trigger room is never an articulation point of its zone's cell graph;
  - the zone key is never only inside a trigger room or a sanctuary;
  - an alarm door is never the only crossing of a keyed border;
  - every border between a tall zone and a neighbour that has windows keeps ≥ 1 plain window, and every tall zone keeps ≥ min(2, its window count) plain windows (§10);
  - foiled windows take a slot in the trigger budget; plain ones don't;
  - every zone has ≥ 2 Relay entries ≥ 10 cells apart (auto entries at dead ends and corridor ends);
  - no triggers in the first zone or within 15 cells of the start door;
  - the device is visible from 2 m outside an entrance; the room arms 1.5 m inside;
  - every trigger room carries one signature: a red-banded detector base and an emissive **ALARM ZONE** card at every entrance (readable in a dark room). No harmless detector props anywhere.
  No route is ever a forced call.

**Numbers.**

| Parameter | T1 / T2 / T3 / T4 / T5 (SP) | Why |
|---|---|---|
| callThreshold | 60 / 56 / 52 / 48 / 45 | One full 5 s sprint (≈ 48) never calls at T1; two within ≈ 15 s do. |
| Decay | −3.0 / −2.75 / −2.5 / −2.25 / −2.0 per s after 3 s | A short burst you walk away from is forgiven. |
| Gains | walk 0; ease +1; unlock +1; sprint step +3; barge +8; slam +12; pry +8; sanctuary 0; RELAX 0 | Quiet is free; the loud version of each verb costs. |
| Call error | sprint 2, door/pry 1, device 0 cells; −1 per repeat into the zone within 180 s; snapped to a reachable disturbance cell | Roughly known; repeats home in. |
| CALLED | latch tick per Attention call; buzz on the first 3; device sound at the device; tick when a queued call goes out | Every call traceable when it happens. |
| drySpell | 240 / 210 / 180 / 160 / 140 s | A quiet walker brushes stage 1 every ≈ 5–7 min (estimate). |
| Sweep | 40 / 45 / 50 / 55 / 60 s; 4 waypoints; W ≥ 21 m, nearest 21–30 m | Aims for stage 1; survivable by staying quiet. |

**Sources.** Thompson (director nudges toward the area; tightening radius); GamingBolt, Amnesia: The Bunker (tiered, readable noise); Walsh, Splinter Cell Blacklist (attributable calls, tiered feedback); Couvidou; Booth; PC Gamer, Kadoi (senses sharpen after a dry spell).

---

## 7. Hearing and the path field

**Rules.**
- **One propagation model** for AI hearing, the warnings and the Relay's audio (Walsh's TEAS, Thief's room database, Sharp's DX:IW). A Dijkstra in whole metres over the cell graph, **8-neighbour (octile)**: orthogonal steps 3 m; a diagonal step 4.24 m only when both orthogonal edges and both side cells are open (inside a room), so an open hall measures close to the straight line.
  - open edge, arch, open door, broken or pried window: +0; Ajar door +4; shut or locked door +9; intact window +9; walls block.
- **Player field F:** rooted at the player's cell, capped at 45 m, in its own persistent arrays (never the Plan scratch), rebuilt on a player cell change or any door or window edge change inside it. It gives `PathDistanceToPlayer`, `PathOpeningCost`, and the distance tests.
- **Warning distance W = min(F, 3 × straight-line 3D distance).** A Relay pacing behind a thin wall is near, and the ladder says so; a far-off one is never promoted. W drives the stages (§9), Arrive, the sweep and Withdraw. Hearing uses F alone.
- **Source fields:** door, window and device noises use a one-off field rooted at the edge's two cells (edge cost 0). Picking which side of an opening a heard estimate falls on uses a one-off **Relay-rooted** field.
- `Noise(point, radius)` stays, so the tests compile. Internally it uses F when the source is the player's cell and radius × hearing ≤ 45 m, else a one-off field.
- **Loudness** (metres of path, × hearing): walk 0 at T1–T2; **walk 3 / 4 / 5 m at T3 / T4 / T5, heard only by the on-map Relay, never by the panel**; sprint step 27; ease 6; key unlock 6; barge 18; slam 27; pry 8; glass 60; foil or alarm device 45; anything the Relay causes: never. Heard when path d ≤ loudness × hearing.
- **Tuning and scene:** base hearing becomes 1.0. Re-serialize the scene hunter block (FrontRooms3D.unity ~1062-1077). `releaseDelaySeconds`, `sprintNoiseRadius` and `doorNoiseRadius` stay as `[HideInInspector]` legacy fields (still in `CopyFrom`), because the legacy `FrontRoomsHunterBrain`, `Editor/FrontRoomsStreamVerification.cs` and MapInteractionTests use them. The new loudness fields sit beside them. Glass moves from the `GlassNoiseRadius` const into `FrontRoomsHunterTuning`. Every new field goes into `CopyFrom`.
- **A sense link, not GPS.** A heard noise creates {estimate cell, time, margin = loudness·hearing − d}. Error k = round(2·d/(L·h)) cells, +1 through a shut opening when d > L·h/2, max 3. k = 0 is the exact point, so radius-1000 test noises stay exact.
- **Which noise wins:** a new noise replaces the link only if its margin ≥ current margin − 1.5 m/s × age; at most one re-target per 1.5 s. `DebugPlace(feet, facing)` clears the link (and the meter, gate, stage holds and lock-on), so nav trials don't inherit an old margin.
- **Repeated noise:** a second noise within 6 s and 2 cells of the estimate halves k and turns a cautious look into a full search.
- **When it listens:** Listen, Patrol, Hunt, Search; Chase without sight (moves the last known point only); BreakDoor (queued until the break ends); FADE (one cautious look at most); **Withdraw at half range** (one cautious look under FADE rules, then the walk out resumes). Deaf in Away, Arrive and in Chase with sight.
- **LoseTrack uses only what it perceived:** `doorInto` and `playerCellBefore` are deleted. It goes to the last-seen point, or to the far side of a door it saw the player cross in the last 1.0 s of sight, or one it heard move after losing sight. No intuition window (Welsh's 2–3 s considered, declined).
- **Tier hearing:** ×1.0 / 1.07 / 1.14 / 1.21 / 1.3.
- **Cost:** the field rebuild is unprofiled. Step 0 profiles it; any WebGL reduction is platform-gated and keeps the logic.

**Numbers.**

| Parameter | Value (SP) | Why |
|---|---|---|
| Graph | 8-neighbour, diagonal 4.24 m only through open cells | 12 m of sight is ≈ 12–15 m of F, not 17–21 m. |
| Opening costs | 0 / Ajar +4 / shut +9 / window +9 / wall ∞ | Sharp's ordering; one table for all. |
| W | min(F, 3 × straight line) | Warnings for a Relay behind a thin wall. |
| Loudness | walk 0 (T3–T5 on-map: 3/4/5); sprint 27; ease 6; unlock 6; barge 18; slam 27; pry 8; glass 60; device 45 | On the map it tracks a sprint at about the stage-1 band; an eased door carries 2 cells. |
| Hearing | ×1.0 / 1.07 / 1.14 / 1.21 / 1.3 | Tiers lean on pacing, not ears. |
| Field F | player-rooted, 45 m cap, own storage, rebuilt on cell or edge change | Never stale after a shut. |
| Estimate error | k = round(2d/(L·h)), +1 through a shut opening past half range, max 3 | Far and muffled is vague. |
| Re-target | margin decay 1.5 m/s; ≤ 1 per 1.5 s | Ends the 3.3 Hz drag. |
| Withdraw hearing | half range; one cautious look | Noise behind it is never free. |

**Sources.** Walsh (TEAS); Leonard, Thief postmortem and *Building an AI Sensory System*; Sharp, DX:IW Dev Diary #2; Welsh, *Looking for Trouble*; Thompson, TLOU (noise grows with speed); Thompson, Alien (tightening radius).

---

## 8. Sight, lock-on, chase and search

**Rules.**
- **Head.** The hunter owns a logical `LookYaw`, clamped ±70° from the body. It canvasses with TLOU-style turns in Listen and Search, sweeps ±40° while walking, tracks the player once the meter reaches 0.5, locks forward in Chase. The rig draws it: **body follows `Heading`, head follows `LookYaw`** (today the body snaps to face the player on any glimpse).
- **Calm sight** (Listen, Patrol, Hunt, Search, BreakDoor): ±60° H, ±40° V around `LookYaw` (up to ±130° of the body; rear blind arc ≥ 100°), 12 m in 3D at every tier, no light term (dark is never cover), any collider but the player blocks it. `SeesPlayer` keeps meaning "ray and cone pass this frame".
- **`PlayerSeesRelay`** (new, one test reused everywhere): frustum test plus 2 rays to its 1.6 m and 1.95 m points, at 10 Hz, platform-identical. Read by Withdraw, FADE, Pressure, the stage-2 footstep rule and Arrive.
- **Touch:** within 1.5 m on the flat, 360°, only when the two cells are the same or joined by an **Open** passage (open, arch, broken, pried) **and** a capsule cast between the bodies is clear. Fills the meter at 4/s (never instant). Exempt from the gate. Off when `sightRange` is 0. Never through a shut or Ajar door or a window.
- **Spotting meter (0–1):** fills while the ray and cone pass, full in T(d) = (0.30 + 0.04·d) × spotMul[tier]. **Freeze:** half rate once the player has stood still ≥ 0.5 s at d > 6 m (taught on the pause card; kept only if Step 0 shows no abuse). Drains 0.5/s after a 0.3 s hold. **No fill while the player's cell is a sanctuary cell.**
- **"Almost" (meter 0.5):** the Relay stops and the head tracks you; one positional **scuff-and-turn** plays from its body (under the stage-2 path rules) and is captioned. If the meter drains from ≥ 0.5 without filling, it takes a cautious look at the glimpse point.
- **G3 gate, per lock-on:** the meter is held at ≤ 0.9 until stage ≥ 1 has been on **continuously** for ≥ 3 s **and** stage 2 continuously for ≥ 1.0 s (the run timers reset when the stage drops). Touch exempt. If the meter is held at the gate for > 1.5 s, the Relay drops the stare and takes a cautious look at the glimpse point (it walks closer, so the ladder catches up honestly). Every gate hold is logged.
- **Lock-on:**
  - The meter fills: `TargetAcquired(pos)` fires, the cue plays (0.6 s, §9), Pressure +15.
  - The Relay stands and squares up for **0.7 s at every tier** (cue + 0.1 s). It cannot catch during the hold, and the hold is committed: losing sight doesn't cancel it.
  - At the end of the hold `StateChanged(Chase)` fires: chase bed, heartbeat, the chase wave. Speed ramps to chase speed over 0.6 s.
  - Re-acquisition in the same encounter: short cue (thunk only, 0.25 s), hold 0.35 s.
  - Camera shots cancel on `TargetAcquired` or `Spotting ≥ 0.5`, not on any glimpse.
- **Chase:** sight ±110° around the body, 15 m. **Lose meter:** fills 1/s while it can't see you, drains 0.5/s while it can; LoseTrack at **2.0 s at every tier** (1.0 s past chaseCap; 0.5 s of broken sight past onMapCap + 15 s). Also LoseTrack when F > 24 m for 1 s **and it does not see you**, with licensed doors counted at 0 for this test (it knows where you went). Breaks a door only with a licence (§5).
- **After LoseTrack**, outside FADE: walks at 2.6 m/s to the last known point, or through a licensed door; then the **full search**: spots hidden from that point, weighted to the half-plane ahead of the last heading it saw; the radius shrinks each pass; slightly sub-optimal order (second-best 30 % of the time, one backtrack); listens 1.2 s per spot; up to searchMax. Then the give-up beat, a patrol from T3, Withdraw. **In FADE:** LoseTrack → a cautious look (1 spot) → give-up beat → Withdraw.
- **Cautious look** (after a noise only, or a glimpse): walk to the estimate; listen 1.5 s with a head canvass; check 1 spot hidden from it; listen 1.2 s; give up. Uses the Search state.
- **Catch:** within 0.7 m on the flat, only in Chase, with sight or touch, never while the player's cell is a sanctuary cell.
- **Tier-5 escape, worked estimate (not simulated).** Lock-on at 9 m in a hall; the player has a 2 s bar and reacts 0.4 s after the cue starts. Hold 0.7 s: the player sprints for the last 0.3 s (gap 10.7 m). Ramp 0.6 s at ≈ 2.2 m/s average vs 5.5 (gap ≈ 12.6 m). 1.1 s of bar left (gap ≈ 13.8 m). Winded 2 s at 3.2 vs 4.4 (gap ≈ 11.4 m). The Relay needs ≈ 2.6 s to reach a corner the player rounds at that gap, beyond the 2.0 s lose threshold: **one corner within ≈ 12 m escapes, with ≈ 0.6 s to spare.** In an open hall with no corner, the player is caught. Hence: chase is capped at 4.4 m/s in v2.0, the lose threshold doesn't scale, and the validator measures every cell's distance to a sight-breaking edge (§12). Step 0 runs the evader bot with 0.3 and 0.6 s reaction delays; G5 at T5 must hold at 0.6 s.
- **Nav test door rule, rewritten:** start the Relay facing the player; keep the player in view long enough for the gate (3 s stage ≥ 1, 1 s stage 2), the spot and the hold; count the give-up Listen or Withdraw as giving up; keep "never within 6 m of where the player really went".

**Numbers.**

| Parameter | Value (SP) | Why |
|---|---|---|
| Calm cone and range | ±60° H, ±40° V round LookYaw; head ±70° of body; 12 m (fixed across tiers) | Approach from behind is possible. |
| Chase cone | ±110°, 15 m | Corners work. |
| Touch | 1.5 m; Open passage + capsule; 4/s; off when sightRange 0 | No lock-on through a door leaf. |
| spotMul T1..T5 | 1.0 / 0.95 / 0.9 / 0.85 / 0.8 (12 m: 0.78 → 0.62 s) | The tier term v1 lacked. |
| Freeze | ×0.5 when still ≥ 0.5 s, d > 6 m | A learnable verb. |
| Drain | 0.5/s after 0.3 s | Peeking adds up. |
| G3 gate | ≤ 0.9 until stage ≥ 1 for 3 s **and** stage 2 for 1 s, continuous; held > 1.5 s → cautious look | Red's order holds for every lock-on. |
| lockOnSeconds | 0.7 s every tier (fixed); re-acquire 0.35 s; ramp 0.6 s | The stage-3 window never shrinks. |
| Lose meter | fill 1/s, drain 0.5/s; LoseTrack at 2.0 s (fixed); 1.0 s past chaseCap; or F > 24 m for 1 s unseen | A glimpse no longer resets it. |
| chaseSpeed T1..T5 | 4.2 / 4.3 / 4.4 / 4.4 / 4.4 m/s (v2.0; T4–T5 may rise to 4.6 only after sanctuaries and a passing G5 at T5) | Sprint keeps ≥ 1.1 m/s. |
| Full search T1..T5 | 15 / 15 / 16 / 17 / 18 s; 3 / 3 / 3 / 4 / 4 spots | Searches better, not longer beside you. |
| Cautious look | 1.5 s, 1 spot, 1.2 s | Go, look, leave. |
| Catch | 0.7 m, Chase only, sight or touch, not in a sanctuary | No catch in the hold. |

**Sources.** CritPoints, *Stealth Game Spotting Deconstruction*; DBD Wiki, Chase; Couvidou; Welsh; Thompson, Alien (rear sensor, sub-optimal search); Metal Gear Informer, MGSV Reflex Mode; Dark Deception wiki, Murder Monkeys; Thompson, TLOU (canvassing).

---

## 9. Staged warnings, cues and photosafety

### 9.1 Stages

- From **W** (§7). Only while OnMap; Away and Arrive are stage 0. Same bands at every tier.
  - Stage 0: W > 36 m after hysteresis, or not on the map.
  - **Stage 1, lights:** enter W ≤ 30 m, leave > 36 m, hold ≥ 4 s.
  - **Stage 2, steps:** enter W ≤ 18 m, leave > 24 m, hold ≥ 3 s. Includes stage 1.
  - **What "hold" means** (clarified 2026-10-07): a **minimum time in the stage, counted from entry**, never a delay before entry.
    - A stage starts on the first field sample inside its band. If W jumps straight to ≤ 18 m (a door opens), stage 2 starts at once and the stage-1 run timer starts with it.
    - Once entered, stage 1 lasts ≥ 4 s and stage 2 ≥ 3 s.
    - After that, a stage ends once W has stayed above its exit band (36 / 24 m) for 1.0 s, to absorb ±3 m of cell jitter.
    - Stage 2 drops to 1, not 0, unless W is also above 36 m.
    - Why not an entry delay: the Relay walks in at about 2.6 m/s. A 4 s delay would start stage 1 near 20 m, and a 3 s delay would start the steps near 10 m, inside the 12 m calm sight. The G3 gate would then hold almost every lock-on, and the steps would come too late to warn.
  - **Stage 3, lock-on:** latched from `TargetAcquired` to the end of the chase.
- Shutting a door between you honestly adds 9 m.

### 9.2 Stage 1: your room's lamps

- **Your room** (v1 §7.1, kept): the intact carved or module room you stand in; otherwise a BFS over Open edges, depth ≤ 2, ≤ 9 cells, arches and doors as walls. Start rooms excluded. Recomputed on a cell change.
- **Never at the Relay:** remove from the set every lamp within 2 cells of the Relay's cell or in the Relay's room. Asserted.
- **Live lamps:** lamps in the set within `lightRadius` (16 m) whose mode is lit. Fewer than 2 → widen within the zone over Open and Arch edges (depth 3, ≤ 12 cells) → else **audio only**. Inside a sanctuary: audio only (its circuit is dead).
- **Built on the landed layer, unchanged:** each dip is `SetLampOverride(cell, Warn, m, hold, envelope 0.10 / 0.22 s)` with base × MIN stacking (the caller passes the envelope; `DefaultEnvelope` stays). To stop an ambient stutter showing through, the burst set's lamps are put to `SetLampMode(cell, Steady)` from the first burst and restored to their own mode in the first quiet slot after stage 0. The ink's burst gate keeps working: it detects a Warn burst as `LampLevel` below `LampBaseLevel` with no ink override. Under the v2 burst, the 0.55 low-pass gate would never open (Ls stays 0.68–0.84), so the wallpaper chat retuned it by burst depth in `20_level_design_phosphor.md` rev 4: G ≈ 0.02 at 0.70×, 0.14 at 0.62×, 0.24 at 0.55×. That is one GROUND breath per burst, at ≤ 0.5 Hz. Its stage-0 power sags use attack 1.6 s / release 1.5 s, and its chase-wave dips 0.3 / 1.2 s, explicitly and on CHASE only.
- **Burst:** every live lamp dips together, ±40 ms jitter from its cell hash.
  - Stage 1: 2–3 dips; stage 2: 2 dips. Each dip to 0.55–0.70 × base (visual chat may go to 0.45): attack 0.10 s, hold 0.06–0.15 s, release 0.22 s (≤ 0.47 s), dips start 0.55 s apart (longer than a dip, so they never merge). Burst ≤ 1.6 s (stage 1), ≤ 1.05 s (stage 2).
  - **Quiet gap after a burst ends:** stage 1 3–6.5 s; stage 2 2.5–4 s; random each time. First burst 0.3–1.0 s after stage 1 starts; one burst on entering stage 2 if ≥ 2.5 s since the last ended.
  - A room-set limiter delays the **whole** burst together (never one lamp) if a lamp's flash budget is near.
- **Audio half (every mode):** `WarnBurst(roomCentre, lampCount)` drives a **ballast brown-out groan** on the 2D room-tone HumBed (−3 to −6 dB, −40 to −100 cents for the burst). The HumBed parameter is driven by `WarnBurst` only, never by ambient lamps. Per-fixture hums keep following their own level and are outside the invariant.
- **Ambient flicker** stays single-lamp, unsynced, with no HumBed change.
- **Invariant (test):** at stage 0, no Warn override is ever active and `WarnBurst` never fires; no two lamps of the room set start an override within 0.2 s of each other with an attack under 1.5 s. The ink's player-centred Sag and pressure-wave Dips must meet this (attack ≥ 1.5 s, or ≤ 1 lamp of the room set): the landed Dip default (0.3 s) does not, so the wallpaper chat passes its own envelope. An appended event `LampOverrideStarted(cell, pos, LampFx)` lets the sound chat buzz only for the right kind; `LampDipped` stays.
- **The wake reprint** ("where it just walked", v1 §7.5) is out of v2: no reprint at stage 0 (wallpaper chat).
- **Fiction:** pre-alarm (a concept since 1985; whether 1990 US panels had it is unverified): the panel switches your compartment to emergency power and tests it. "It draws power" is dropped.

### 9.3 Stage 2: its steps

- The Relay's steps play only at stage ≥ 2, or while `PlayerSeesRelay`.
- **Gain:** 0.55 + 0.45 × (1 − F/18) inside stage 2 (a floor, so the first steps are audible), fading to 0 across the 18–24 m exit. **Muffle** from `PathOpeningCost`: 0.3 Ajar, 0.55 one shut opening, 0.85 two or more; plus a distance low-pass always, so the first steps read muffled (Red's word). From its real position.
- The existing Presence instance becomes a continuous **coil hum** at stage 2, same gain and muffle (Q3d), so a still Relay (Listen, a look, a break wind-up, the hold) stays audible: no gap > 1.2 s.
- **FMOD:** the RelayFootstep (max 42 m) and RelayPresence (max 30 m) events lose their straight-line distance attenuation (bank change in `fmod_frontrooms.py`) and take a `PathGain` parameter; added to `fmod_check contract`.
- Acceptance: at the stage-2 entry distance through one shut door, step peaks are ≥ 3 dB above room tone (SP; sound chat measures).

### 9.4 Stage 3 and the cue sheet

- **Lock-on cue** (fired by `TargetAcquired`, never by `OnRelayState`): a magnetic door-holder release plus **one damped chime strike**. Door-holders released on alarm and coded chimes are both verified for 1990; the release 'thunk' itself is a Foley choice (the magnet only lets go; the closer and latch make the sound). 0.6 s, attack ≥ 15 ms, peak 3–6 dB above room tone, from the ceiling over you. The **only tonal chime in the game.**
- At lock-on: no new bursts; the current one finishes. The chase wave takes the lamps at chase start, **centred on the player's room set, never the Relay's cell** (v1 30_narrative §2 said "from its cell"; that breaks §3.1). Lamps never snap to steady as a "found" signal.
- Chase bed and heartbeat start at `StateChanged(Chase)` and fade over 3 s at LoseTrack. No Lost stinger.
- **RESTORE** (Q3c): only on a true Away, only if stage ≥ 1 was felt. **Audio only:** the HumBed returns to normal pitch and +3 dB over 1.5 s (the ballast recovering). No clunk, no lamp change.
- **Tension** follows the stage only: 0.05 / 0.2 / 0.4 / 1.0 for stage 0 / 1 / 2 / 3; rise 1/s, fall 0.25/s. Heartbeat from exertion and Chase only.

| Cue | Means | Sound family (one each) | Where | Caption |
|---|---|---|---|---|
| Lamp burst + ballast groan | it is near | brown-out groan (HumBed) | your room | [lights dip] |
| Steps + coil hum | it is close, there | heavy muffled steps, low hum | its position | [muffled heavy steps] |
| Scuff-and-turn | it nearly saw you | one foot scuff | its position | [it stops and turns] |
| Lock-on | it sees you | thunk + the only chime | ceiling over you | [alarm chime: you are seen] |
| CALLED | your noise called it | dry latch tick (+ buzz ×3) | ceiling of your room | [alarm panel clicks] |
| Device | you tripped this | the device's own sound | the device | [door alarm] / [glass breaks] … |
| RESTORE | it has left | hum returning | your room (2D) | [alarm panel resets] |
| Give-up beat | — (no cue: steps stop, then recede) | — | — | — |
| Tier rise | — (no sound; more devices are the tell) | — | — | — |

Player-facing text never uses the word "relay" for panel sounds. Captions feed the in-flight `FrontRoomsCaptions` (its AHEAD/LEFT/RIGHT/BEHIND tag applies to positional cues only).

### 9.5 Photosafety and accessibility

- **Limiters:** per fixture for ambient lamps; per room set for Warn. A change that would make a 4th flash in any 1 s window is delayed. A flash = a pair of opposing changes ≥ 10 % of full level with the darker state < 0.8 (WCAG 2.3.1). **Episode** = flashes less than 1 s apart; ≤ 5 s. Design target ≤ 2 flashes/s for every mode. Bit-identical when nothing is limited (fixture parity test).
- **Ambient lamps with default settings:** mode 1 (stutter, today PerlinNoise(t·23) between 0.95 and 0.05) becomes 2–3 dips on **0.5 s** slots, floor 0.5, 60–80 ms ramps, episode ≤ 1.5 s, then ≥ 2 s steady. Mode 2: dropouts held ≥ 0.34 s, ≤ 1/s. Mode 3: one strike, ≤ 1 per 4 s.
- **REDUCE FLASHING** (exists, `aba5f77` / `ef061ae`: overrides ramp ≥ 0.5 s; ambient soft dip to 70 %): a burst becomes one sag to 0.7 over 0.6 s and back; no ink gasp; the chase wave a static sag.
- **LAMPS STILL** (proposed third value): no lamp level changes; stage 1 by the groan only.
- Never "epilepsy safe"; name the effect.
- **Audio:** a "Warning cue volume" slider; the lock-on cue never below room tone.
- **Assists, off by default:** captions (existing row) in the classes above; "Visual sound cues", a small edge marker for stage-2 steps and device trips (Q3e); with captions on, a rumble or vignette pulse (never a flash) at lock-on.

### 9.6 The wallpaper arrow motion in pursuit (decided 2026-10-07 with 平面视觉)

Red wants the pattern-native hint in the game: the wall's arrows click round to point a route in three 30° ratchets, then the rows step along it. These rules govern it during pursuit:

| Moment | Arrows | Why |
|---|---|---|
| Stage 1 and 2 (`WarnStage`) | Only the walls of the player's room set click round, never in the Relay's room or within 2 cells of it. They point the **calm FLOW route** (the nearest unvisited threshold, which never looks at the Relay). No stepping | A local warning about you, and no position readout (LD R5) |
| Lock-on hold (`TargetAcquired` → +0.7 s) | No change | The chime is the only stage-3 signal |
| Chase (`StateChanged(Chase)`, after the hold) | A travelling wave from the player's room set outward at 8 m/s. The arrows may switch to the **pressure route** (doors away from it that you can shut; LD R6) and the rows step | The same moment as the LD chase wave |
| Chase ends (`StateChanged` Chase → Search) | The wave retracts over 2–3 s, back to the calm route | The hint lasts as long as the chase |
| Withdraw, Restore | No change | "It has gone" is audio only (Q3c); the paper is never an all-clear |

- **Rhythm:** fixed, at 平面视觉's two step speeds. Never tied to the Relay's speed or distance, which would make the step rate a proximity readout.
- **Tiers:** no tier gate. Tiers make the Relay better, never the hints fewer. After it lands, the evader bots measure it; if chase escape is above 50 % at T1 or 35 % at T5, shorten the pressure-route reach at T4–T5.
- **Safety:** whole-wall stepping is a moving repeated pattern (Game Accessibility Guidelines). With Reduce Motion on, the arrows click round but never step, and the chase wave is a single static reveal.
- **Never:** marks the Relay's position, marks a trigger room, or reacts on walls the Relay walked past.

**Driver contract (checked 2026-10-07 against 平面视觉's `research/cue_driver/CUE_DRIVER_INTERFACE.md` v0.1, §3).** Its mapping matches the table above. These points make it exact:
- **Keys.** The wave keys on `StateChanged(Chase)`, which exists today. In v2 the same event fires at the end of the 0.7 s hold, so the driver never subscribes to `TargetAcquired`. Today Chase starts on first sight with no hold and no G3 gate, so a preview from the current build shows the wave without the warning ladder.
- **Chase ⇄ BreakDoor.** A door break inside a chase is still the chase. `StateChanged(BreakDoor)` changes nothing, and the `StateChanged(Chase)` that follows the break must not restart the wave (idempotent).
- **Retract on any exit from the chase**, not only Chase → Search. Today the unbuilt-cell re-arrive also takes Chase → Listen, and a test placement goes to Search. v2 deletes the re-arrive, but the driver should not depend on that.
- **Retract lands on the stage.** When the wave retracts while `WarnStage ≥ 1` (the usual case right after a chase), the room set ends on the calm FLOW route, turned and not stepping. Only cells outside the room set go back to 0. If the pressure dir and the FLOW dir differ, the cell turns back first, then makes the stage-1 turn.
- **Precedence:** Caught (freeze) > wave > stage. A `WarnStageChanged(0)` during a wave is held until the wave has retracted, so the walls never jump mid-chase. Today the stage can drop during a long chase; in v2 stage 3 latches.
- **Stage 2 ↔ 1 changes nothing.** Only 0 turns the set back.
- **The room set follows the player.** It is the same function the stage-1 lamp burst uses (§9.2: the room you stand in, otherwise BFS over Open edges, depth ≤ 2, ≤ 9 cells, start rooms excluded), owned by 关卡设计 and shared, so lamps and walls always agree. The lamp widening rule (zone, depth 3) is for lamps only. When the player changes room set at stage ≥ 1, the old set turns back and the new set turns.
- **The Relay exclusion is tested when a cell starts to turn.** A turned cell holds when the Relay later comes within 2 cells. Turning back as it nears would be a position readout. The wave has no exclusion, because in a chase you can see it.
- **No route, no turn.** Until the exit (§4 Q1 of the driver doc) is defined, the interim FLOW target is the nearest unvisited threshold: a zone-border door or arch/open edge the player has not crossed (agreed with 关卡设计). Windows and pry-only edges never count, and a trigger room's armed area costs high, so the route never steers into a device. 关卡设计 keeps the set of crossed border edges. **Fallback ladder** (Tall zones border non-Tall zones only through windows): (1) the nearest unvisited threshold anywhere in the player's walkable component (door, arch and open edges; not only the current zone); (2) else the nearest walkable threshold, visited or not, usually the way in; (3) else message 0, with lamps and groan only. The chase pressure route (shuttable doors away from it) falls back to the same ladder. **The cue never points at glass,** in any stage or in a chase. The share of stage ≥ 1 seconds with message 0 is logged by zone height; above ~25 % in Tall halls, revisit through a map rule (a Tall–non-Tall door), not the cue. The search covers built cells only (`IsBuilt`, about 40 cells across at buildRadius 2) and never makes the map generate; a threshold past the built edge falls to rung 2. The target is re-evaluated only on a player-cell or `PassageRevision` change, so it never flickers per frame; only cells whose dir changed turn back and re-turn. With `doorsNeedKeys` on, a door the player has no key for counts as a wall. With no target the cell stays at 0, and the lamps and groan still carry stage 1.
- **No darkness gate** for the pattern-native cue. Under the v2 burst, lamp level stays 0.68–0.84, so a 0.55 gate would never open in a lit room. It would also tie the cue to lamps, and v2 keeps lamps meaning only "near". The 0.55 gate belongs to the old glow-ink hint.
- **WebGL** may be turn only. That is the Reduce Motion path and carries the same information (direction), so the rules stay identical. Gate it by platform; desktop is unchanged.
- **Reset** to all-0 on run restart, map rebuild and `ResetWarningMetrics`.
- **Invariant (added to G7):** at stage 0 with no chase, every cell is at state 0 or retracting. Ambient keyframes never use cue vocabulary.

**Numbers.**

| Parameter | Value (SP) | Why |
|---|---|---|
| Stage 1 band | enter W ≤ 30, leave > 36, hold ≥ 4 s (fixed across tiers) | DBD bands; never strobes at an edge. |
| Stage 2 band | enter W ≤ 18, leave > 24, hold ≥ 3 s (fixed) | Covers 12 m sight in F terms. |
| Dip | 0.55–0.70 ×; 0.10 / 0.06–0.15 / 0.22 s; starts 0.55 s apart | ≤ 1.8 flashes/s; dips never merge. |
| Burst | 2–3 dips ≤ 1.6 s (stage 1); 2 dips ≤ 1.05 s (stage 2) | Short episodes. |
| Quiet gap | 3–6.5 s (stage 1); 2.5–4 s (stage 2) | Two readable rates, not a radar; ≥ 2.5 s steady. |
| Exclusion | no burst lamp within 2 cells or in the Relay's room | Red: never where it is. |
| Widening | < 2 live lamps in 16 m → zone, depth 3, ≤ 12 cells → audio only | A warning never fails silently. |
| Ballast groan | −3 to −6 dB, −40 to −100 cents | Present in every mode. |
| Stage-2 audio | gain 0.55–1.0 inside, fade 18–24 m; muffle 0.3 / 0.55 / 0.85; gap ≤ 1.2 s | Audible from the first step. |
| Lock-on cue | 0.6 s, ≥ 15 ms attack, +3–6 dB, the only chime | Clear, never a jump scare. |
| RESTORE | hum +3 dB, pitch back over 1.5 s; audio only | Lamps mean only "near". |
| Ambient mode 1 | 0.5 s slots, floor 0.5, ≤ 1.5 s, then ≥ 2 s steady | Today's strobe fails by default. |

**Sources.** DBD Wiki, Terror Radius and Chase; PC Gamer, Kadoi and GamingBolt on Mr. X; Phasmophobia Steam thread; MCV/Develop, *18 things we learned about Alien: Isolation*; GosuGamers on Silent Hill; Leonard, Thief; Couvidou; Sharp; Walsh; W3C WCAG 2.3.1; Xbox XAG 118; Game Accessibility Guidelines; HardingFPA; Push Square on TLOU2 assists.

---

## 10. Player verbs

**Rules.**
- **Walk** at 3.2 m/s is silent to the panel at every tier and faster than every non-chase speed. From T3 the on-map Relay hears walking within 3–5 m of path (§7).
- **Speed classification** uses input-driven velocity only (walk or sprint wish), never the door pull step (`PullStepSpeed` 4 m/s) or the title glide.
- **Doors** (on the single-acting doors):
  - **EASE:** E while walking or standing. Today's 0.55 s swing (unchanged, so `FrontRoomsShotTimings` and the door Foley segments hold) and a soft latch click. 6 m, +1.
  - **BARGE:** E to open while fast (sprinting, or within 0.5 s of the last sprint frame). A new 0.3 s swing. 18 m, +8.
  - **SLAM:** E to shut while fast. 0.3 s swing and a frame bang. 27 m, +12.
  - A door can be swung again only once its swing has finished (no door spam). A quick re-shut while walking stays an ease.
  - A shut door is a barrier: +9 m to all hearing and warnings, blocks sight, a WALL to a Relay without a licence (§5).
  - **Ajar** (`Passage.Ajar`, new): the rest state of a rehung door or one the Relay OPENed. +4 m, blocks sight. The Relay pushes through in 0.4 s (source Relay). The player opens it fully with E (an ease).
  - **Rehang:** during Away, each broken door more than 24 m of F from the player and out of sight is rehung Ajar.
- **Windows** (two targets, two inputs):
  - The **pane**: "HOLD E · BREAK GLASS", 1.0 s as today: an instant call.
  - The **sash latch** (its own collider on the meeting rail): "HOLD E · PRY SASH (quiet)", 4 s, 8 m, +8, leaves it open for good. Progress banks between holds; in TAP TO BREAK mode taps bank pry progress. Plain windows only.
  - **Foiled windows:** foil tape on the glass perimeter (detects breakage) and a magnetic reed contact on the sash (detects opening). Both are verified as common by 1987; that the foil was silver is not. Prompt on the latch: "HOLD E · PRY SASH · ALARMED". Any way through calls.
  - **Plain-window rule:** every tall–neighbour border with windows keeps ≥ 1 plain window; every tall zone keeps ≥ min(2, window count) plain. Foil is a pure function of the edge hash and the **tall zone's stored tier** (a window border always has exactly one tall side): rank the tall zone's window edges by hash and keep the lowest ones plain. Validator: identical across rebuild and revisit shift.
- **Stamina:** adopt the landed winded rule (empty → winded until 1.0 s of stamina is back, i.e. 1 s delay plus 1 s at 1/s); a held Shift resumes the sprint then (no fresh press). `stepTime` resets when a sprint starts; a sprint step needs ≥ 0.5 m of movement since the last.
- **Teaching walking:** the footer of the evacuation plan Red approved at the **start door, map side** (narrative A.12, 平面视觉 layout T24; the start rooms are the title stream and carry no signs), "IN CASE OF FIRE: WALK, DO NOT RUN. / CLOSE DOORS BEHIND YOU." (standard US wording; the exact pre-1990 text of "WALK, DO NOT RUN" is unverified), with a copy at sanctuaries. CalmHint: "Walking is quiet. Running and slammed doors are heard." for 10 s at Armed, and once at the first noise call. After the first RESTORE, once: "It's gone. Walk, and it may not come back." Pause card: WALK quiet · SHIFT run (heard) · E door (ease quiet, slam when running) · HOLD E latch: pry (quiet, slow) · HOLD E glass: break (calls it) · STAND STILL: harder to see. The warnings are explained once in the pause-card legend, never live.
- **Sanctuaries, "dead circuit" rooms** (v2.1; Q7c):
  - Props: an amber TROUBLE lamp, a detector hanging by its wires, a bell with no clapper, an OUT OF SERVICE tag, and an entrance placard "ALARM CIRCUIT OUT OF SERVICE · NO DETECTION IN THIS ROOM". One-time hint on first entry: "Dead circuit. It won't come in here."
  - ≤ 12 cells, one entrance, ≈ 1 per 3–4 zones at every tier, never a trigger room or next to one, ≥ 15 cells from the start, never the only place a key lies. **Placed by coordinate and stable through revisit shifts** (a shift may re-dress furniture, never add, remove or move it). Same for trigger rooms.
  - The Relay never plans into their cells. No catch and no spotting fill while you stand in one. Panel gains 0 inside; noises inside still reach its ears.
  - A chase that reaches one ends at the threshold: it watches up to 6 s in the "almost" pose (stage 2 goes on), then the give-up beat and Withdraw. Never straight to Away.
  - Stage 1 inside is audio only. The dry clock pauses inside.
- **Keys:** pick-up silent; an unlock is an ease; the timed swing is source World.

**Numbers.**

| Parameter | Value (SP) | Why |
|---|---|---|
| Ease / barge / slam | 0.55 s, 6 m, +1 / 0.3 s, 18 m, +8 / 0.3 s, 27 m, +12 | Closing doors behind you is free; panic costs. |
| Fast test | input velocity: sprinting or ≤ 0.5 s after a sprint frame | Pull steps never read as slams. |
| Ajar | +4 m, blocks sight; Relay push 0.4 s | The LD door state machine. |
| Rehang | during Away, F > 24 m, out of sight | Cover recovers. |
| Pry | latch, hold 4 s, 8 m, +8, banks progress | A tall zone is never a forced call. |
| Plain windows | ≥ 1 per tall border; ≥ min(2, count) per tall zone | Pure, per-zone, stable. |
| Stamina | landed winded rule; held Shift resumes; first noise at 0.3 s; ≥ 0.5 m per step | No beacon, no dead controls. |
| Sanctuary | ≤ 12 cells, one entrance, ≈ 1 per 3–4 zones, watch 6 s | RE2's save rooms. |

**Sources.** Sharp, DX:IW (doors and windows set attenuation); Vandal, RE2 Mr. X (save rooms; he waits outside); Walsh (teach explicitly, then subtly); Couvidou (readable rules invite luring); Thompson, TLOU (noise grows with speed); GameCritics, Amnesia: Rebirth (islands of safety).

---

## 11. Withdraw and relax

**Rules.**
- **Withdraw starts** when a look or search ends with nothing, FADE ends, a sanctuary watch ends, a sweep's time runs out, or a cut-off investigation gives up at a WALL door. An unbuilt cell goes straight to Away.
- **Sequence:** the give-up beat (2.0 s, head canvass, no cue); from T3 and not after FADE, a patrol of patrolSeconds in its zone (W ≥ 21 m when planned, no shut doors, counted against onMapCap); then the walk out at 2.6 m/s.
- **Exit, in order:**
  1. the entry it arrived by;
  2. the Relay entry farthest from the player;
  3. the build edge on the far side;
  4. the best unseen cell ≥ 10 cells of F from the player.
  Options 1–2: every route cell > 15 m of W from the player when planned, ≥ 4 cells inside the built window, no shut door — or, failing that, with **one OPEN** on its own route.
  5. **Terminal fallback:** if nothing qualifies within 2 s, walk to the reachable cell with the largest F that the player cannot see, and go Away there on the first frame it is occluded by geometry.
- **Timeout:** 20 s after Withdraw starts, the terminal fallback applies from wherever it is. Invariant test: Withdraw reaches Away within 25 s.
- **Senses on the way out:** hears at half range (one cautious look under FADE rules, counted toward lockOnCap, no re-dispatch); sight is the forward cone, spot ×2, lock-on only within 6 m. A lock-on here runs under FADE rules, then the withdraw resumes. Past onMapCap + 15 s it ignores lock-on.
- **Being seen while leaving** (`PlayerSeesRelay`): seen 2 s → re-pick the nearest exit that is unseen and away from you; watched 6 s → Away on the first frame it is **occluded by geometry** (both rays blocked, not merely off-frustum) **and** W ≥ 21 m or a door or corner lies between. It never vanishes in view, and never because you glanced away.
- **On Away:** `Withdrew(pos)`; Restore if earned; Attention 20; Pressure 0; RELAX.
- **RELAX:** relaxSeconds[tier]; noise adds nothing; devices call at once; no dry clock.
- For the narrative: "it leaves the way it came when that way keeps clear of you, otherwise by the farthest way out."

**Numbers.**

| Parameter | Value (SP) | Why |
|---|---|---|
| Give-up beat | 2.0 s, no cue | A visible, audible stand-down when close. |
| patrolSeconds T1..T5 | 0 / 0 / 15 / 25 / 35 s | Tiers stretch the hunt within the cap. |
| Exit route | > 15 m of W; ≥ 4 cells inside; one OPEN allowed | The walk out never re-enters stage 2. |
| Fallback / timeout | 2 s / 20 s; invariant 25 s | No soft-lock, no endless stage 1. |
| Seen while leaving | 2 s re-pick; 6 s → occluded Away | A follower can't pin it. |
| Withdraw hearing | half range | Noise is never free. |
| relaxSeconds | 100 / 90 / 80 / 70 / 60 s | §4. |

**Sources.** Thompson, *Revisiting the AI of Alien: Isolation* (retreat parameters, never despawns in view); Couvidou (give-up beat); Booth (Relax); Leonard, Thief (audible stand-down); Shacknews, Evolve; GameCritics, Amnesia: Rebirth; GamingBolt, Outlast.

---

## 12. Tiers and escalation

**Rules.**
- **Counting:** tier = 1 + floor(unique `GridCoord`s stood in outside the start area / 150), capped at 5. A HashSet of coordinates: regenerated chunks never count twice; backtracking can't farm tiers. The tier never falls; catches never count. The 120 s stall rise is deleted (Game.cs:910-917); the dry-spell sweep answers stalling.
- **Stored first-generation tier:** each chunk coordinate and each zone stores the tier at its first generation (a zone at its first `ZoneOf` query). Lamps, devices, trigger kinds, the budget and foil read it, so a revisit shift never makes an area look worse or changes a border. Sanctuaries and trigger rooms are placed by coordinate (§10).
- **What scales:** call threshold and decay; RELAX (shorter); dry spell (shorter); sweep (longer); peak and onMapCap (longer); re-dispatch budget; lockOnCap; chaseCap; arrive delay; spotMul; hearing (to 1.3); chase speed (to 4.4 in v2.0); break time (to 1.9 s); search; patrol; trigger kinds and density; lamp odds.
- **What never scales (fixed):** the warning bands and holds; burst shapes and gaps; the lock-on cue and the **0.7 s hold**; the **2.0 s lose threshold**; the G3 gate; calm sight 12 m, chase sight 15 m; touch 1.5 m; Arrive W ≥ 42 m; the 24 m chase exit; walk and sprint; ease loudness; sanctuary density. *The tiers make the Relay better, never the warnings shorter.*
- **HUD:** the always-on line becomes `ZONE nn / <height>` (the ceiling-height readout stays). TIER moves into the off-by-default Relay readout assist, and off the CAUGHT overlay (Q5c).
- **In-world tells, silent:** newly generated zones carry the new tier's devices (one more trigger kind per tier after Red's pick; the budget rises from one device per 3 zones to one per 1.5), plus harmless **non-detector** fire-alarm hardware (annunciator plates, more magnetic door-holders, zone labels on panels). No tier chime.
- **Lamp odds** at T5 capped at stutter ≤ 20 %, failing ≤ 12 % (for example .51 / .20 / .12 / .12 / .05).
- **Sight-breaking cover (validator measure):** every cell's path distance to an edge or corner that breaks a 12 m sight line is reported per zone; target ≤ 15 m in tall and large carved rooms. If Step 0 shows open-hall catches, the visual chat adds partitions or the map adds a door.
- **One tier table** (`FrontRoomsTierRules`) holds every per-tier value; every field in `CopyFrom`.

**Numbers.**

| Parameter | T1 / T2 / T3 / T4 / T5 (SP) | Why |
|---|---|---|
| Tier step | 150 unique GridCoords (T5 at 600) | Zones are too small to count. |
| Trigger budget | one device per 3 / 2.5 / 2 / 1.75 / 1.5 zones | Escalation you can see. |
| breakDoorSeconds | 2.5 / 2.3 / 2.1 / 1.9 / 1.9 s | A shut door still buys time at T5. |
| chaseSpeed | 4.2 / 4.3 / 4.4 / 4.4 / 4.4 m/s | Escape holds at T5 (§8). |
| Summary row | threshold 60→45; relax 100→60 s; dry spell 240→140 s; peak 100→120; onMapCap 75→110 s; arrive delay 3.0→2.0 s; spotMul 1.0→0.8; hearing 1.0→1.3; search 15→18 s | All read by the director and hunter. |

**Sources.** PC Gamer, Kadoi (senses sharpen, capped); Thompson, *The Perfect Organism* (unlocks; deaths never count); Wikipedia, Hello Neighbor (invisible adaptation feels unfair); Wikipedia, Hitman (2016) (readable behaviour over a meter).

---

## 13. Implementation map by owner

Steps are ordered so the game always has a working Relay between landings. A scripting define `FRONTROOMS_RELAY_V2` keeps today's timed release and leashes compiled and default until step 4 is green; the sound chat gates its removals on a hunter property `UsesArrive`, so either chat can land first.

| # | Owner | Change | Days (SP) |
|---|---|---|---|
| 0 | 关卡设计 | **Step 0 baseline, no gameplay change** (after Red's yes on Q8, and Q2 asked first). Seeded route RNG. Bot modes: quiet, noisy, evader (reaction 0.3 / 0.6 s), staller, follower, shift-holder, door-spammer, relax-sprinter, build-edge runner, closed-zone (shuts its zone's doors, then sprints), sanctuary-camper (v2.1). Each at T1 and T5. Report (§14). Profile the path field on desktop and WebGL | 1.5 |
| 1 | 关卡设计 | **Landed measurement slice:** path field F (octile, own storage, rebuilt on cell or edge change), W, one-off source and Relay-rooted fields, `PathDistanceToPlayer`, `PathOpeningCost`, `WarningDistance`, `WarnStage`/`WarnStageChanged`, and `PlayerSeesRelay` (10 Hz). Event/log consumers and runtime warning use remain later slices. | 2 |
| 2 | 关卡设计 (+ 声音设计 ping) | Behind the define: `FrontRoomsRelayDirector` (phases incl. PENDING, Attention with `Disturb(kind, point, source)`, Pressure, budgets and caps, RELAX, first-run grace, queued trigger, `Called`/`Restore`, the dry clock and the sweep if Q2 = yes); `HunterState` Away 7, Arrive 8, Withdraw 9; `Arm`/`Dispatch`/`Sweep`/`SendAway`; Armed/OnMap/Released alias; Arrive hard and soft rules, PENDING fallback, step-in re-check, `Arrived` at step-in; handoff; OPEN/BREAK/WALL door licences, `Follow` keeps `throughDoors`, `TickBreak` resumes its origin; Withdraw exits, fallback, timeout; both leashes deleted. Tier columns in `FrontRoomsTierRules`; legacy fields kept `[HideInInspector]`; scene re-serialized (check Codex first); glass into tuning. Autopilot: a debug Summon 20 s after Armed; PASS = reached OnMap at least once | 4 |
| 3 | 关卡设计 | Hearing: sense links, error, side-correct estimate, margin, re-target limit, repeats, BreakDoor queue, half-range Withdraw hearing, walking loudness T3+; `DebugPlace(feet, facing)` resets. Sight: `LookYaw`, cones, touch (Open passage + capsule), meter with freeze and drain, per-lock-on G3 gate and its look fallback, fixed hold, ramp, lose meter, 24 m exit, chase-cap backstop, cautious look and full search, catch rules. Nav door-rule rewrite; interaction tests updated (release → call-driven Arrive; tier checks) | 3.5 |
| 4 | 关卡设计 | Flip the define after Step-0 bots, nav, map 100/100 and fixture parity pass. Remove v1 paths in a later clean-up | 0.5 |
| 5 | 关卡设计 | `FrontRooms3DGame`: stamina (landed winded rule kept; `stepTime` reset; 0.5 m step test); ease/barge/slam from input velocity with `Disturb` sources; swing lock; CalmHint, RESTORE line, pause card; tier by unique GridCoords; stall rule deleted; HUD line and CAUGHT overlay without TIER; `TierChanged` (exists) unchanged. Check the captions session's diff first | 1.5 |
| 6 | 关卡设计 (+ 声音设计, camera rig) | Doors and windows: `Passage.Ajar` with its own branch in `Follow` and Search; Relay OPEN and push; rehang; the 0.3 s fast swing (ping 声音设计 for its Foley; `FrontRoomsShotTimings`, `FrontRoomsDoorSound`, `DOOR_FOLEY_SEGMENTS` are dependencies); the sash-latch collider and pry with banked progress and tap mode; Relay window frame step; interaction tests incl. "4 s on the latch never raises GlassBroken" and "player behind a shut door, Relay at 1.0 m: no TargetAcquired" | 4.5 |
| 7 | 关卡设计 (logic) + 游戏视觉 (look) | Lamps on the landed layer: room set, Relay exclusion, live-lamp count in `lightRadius`, widening, burst scheduler with explicit envelope, `SetLampMode(Steady)` suspension, `WarnBurst`, `LampOverrideStarted`; per-room-set and per-fixture limiters; mode 1 and 2 rewrite; stored first-generation tier per chunk and zone; T5 odds cap; 60 Hz test | 2.5 + look |
| 8 | 关卡设计 | Level content v2.0: validator rules first (articulation point, keys, alarm door, plain windows per border and zone, ≥ 2 entries per zone ≥ 10 cells apart, cover measure); foiled windows in the budget; Red's first trigger room (Q6) with `RoomTriggered` → director; auto entries | 2.5 |
| 9 | 声音设计 | **Audio clean-up and new cues** (below). Verify with `fmod_check contract` and a traced autopilot run | 4 |
| 10 | 游戏视觉 | Warn burst look (default, REDUCE FLASHING); props: the evacuation-plan footer at the start door (map side), foil on glass perimeter and sash contact, sash latch, ceiling detector with red-banded base and ALARM ZONE card, push-bar alarm door, electrical-room sign, glass-floor decal, non-detector tier hardware, dead-circuit kit (v2.1) | 3 |
| 11 | wallpaper chat | Ink Dip and Sag meet the stage-0 invariant (own envelope ≥ 1.5 s or ≤ 1 lamp of the room set); burst gate re-checked on the explicit Warn envelope; wake reprint out of v2; `20_level_design_phosphor.md` Warn table matched to §9.2 (2–3 dips at 0.55–0.70, not 2–4 at 0.3–0.65); note that the Relay now OPENs doors (its "the Relay never shuts doors" line still holds) | 1 |
| 12 | narrative chat | One stage-1 reading; withdraw wording; EAR GROUND and ALARM ROUTE out; chase wave centred on the player; checked the 待核 items (done 2026-10-03; see §2.3); update `30_narrative.md` | 0.5 |
| 13 | 关卡设计 + 声音设计 | Settings and assists: Warn sag under REDUCE FLASHING; LAMPS STILL; cue volume slider; caption classes; Visual sound cues (if Q3e); lock-on rumble or vignette | 1.5 |
| 14 | 关卡设计 | v2.1 after Red's answers: sanctuaries (module type, never-enter cells, watch, dry-clock pause, shift-stable placement); remaining trigger kinds by tier with the rising budget; chase 4.6 at T4–T5 only if G5 at T5 still passes | 3 |
| 15 | 关卡设计 | Docs: this file into `Documentation/RELAY_PURSUIT_REDESIGN.md`; `20_level_design.md` (player-rooted octile field, no omen or herald, one 20-cell handoff, zone-local validator, windows no longer forced calls); Figma section 2441:3804 to v2 | 0.5 |
| 16 | 关卡设计 + Red | Acceptance (§14) | 2.5 |
| — | Red | Answer §16 (Q2 and Q8 before step 0) and approve v2 | 0.5 |

Total ≈ 35 working days across chats; the map chat's share ≈ 26 (v1 said 9–11; the door extensions, the director and the tests were under-counted).

**Audio clean-up list for 声音设计** (all gated on `UsesArrive`):
1. Remove the 2D Hunt, Search, Lost and Chase `RelayStinger`s (`OnRelayState`, SoundDirector.cs:541-548).
2. Remove the `RelayClicks` one-shot on release (:549-553); no replacement.
3. The always-on presence drone (RelaySound.cs:59, :114) becomes the stage-2 coil hum.
4. Remove the straight-line 2D heartbeat (SoundDirector.cs:421-422); heartbeat from exertion and Chase only.
5. Remove the tension state weights and the post-release floor (:378-396); tension by stage.
6. Relay footsteps: stage-gated, path-driven (F, `PathOpeningCost`), 24 m, gain floor.
7. Replace every `Vector3.Distance` proximity with the hunter's path properties.
8. Drop door blows heard past F 45 m.
9. Bank change: flatten distance attenuation on RelayFootstep and RelayPresence; add `PathGain`.
10. Fix the misleading comment at RelaySound.cs:88-89.
11. New: ballast groan on HumBed (from `WarnBurst` only), lock-on cue (full and short), scuff-and-turn, CALLED tick (+ buzz ×3), RESTORE hum return, device sounds (door horn chirp, detector chirp, foil bell tap), fast-swing door Foley, Relay OPEN latch and push, captions posted to `FrontRoomsCaptions`.

**Hooks handed to 声音设计.** New on the hunter: `WarnStage`, `WarnStageChanged(int)`, `PathDistanceToPlayer`, `PathOpeningCost`, `Spotting`, `LockingOn`, `TargetAcquired(Vector3)`, `GaveUp(Vector3)`, `Withdrew(Vector3)`, `RelayDoorOpened(Vector3)`, `OnMap`, `Armed`, `UsesArrive`, `PlayerSeesRelay`. Changed: `Arrived(pos, tag)` now fires at step-in. From the map: `WarnBurst(Vector3, int)`, `LampOverrideStarted(GridCoord, Vector3, LampFx)` (new); `LampDipped`, `FixtureChanged` (exist). From the director: `Called(cause, explicit, pos)`, `Restore()`. From the game: `TierChanged(int)` (exists).

---

## 14. Acceptance tests

**Step 0 report** (per bot mode, T1 and T5; run before any change and after each step): warning-stage-0 share and Away share; time to first call; calls per minute by cause; PENDING seconds and dropped calls; queued triggers; encounter length and how it ended (search, peak, cap, lock-on cap, sanctuary, unbuilt, timeout); stage-1 share of on-map time; stage before every lock-on and every gate hold; chases per encounter; heard re-targets per encounter; chase-cap fires; catches; Withdraw time to Away; field CPU per rebuild.

The editor-only harness lives in `Assets/Scripts/FrontRooms3DGame.Baseline.cs`; it does not alter the legacy hunter when no bot arguments are present. One case can be run from the project root with `Tools/relay_baseline_run.sh 2554 quiet 1 4`. The serial matrix is `Tools/relay_baseline_matrix.sh` (54 cases: seeds `2554 20388 7`, current nine modes, T1/T5); set `SEEDS`, `MODES`, or `TIERS` for a smoke subset. Reports are written to `Verification/relay-baseline/` and should be copied into an evidence folder before starting Step 1.

**Step 0 evidence (2026-10-04):** the independent clean-clone matrix produced **52 PASS / 2 FAIL**; the two seed-7 `edgerunner` cases were rerun after the bot was made to clear one full door cell before selecting a lateral edge route, bringing the reconciled result to **54 PASS / 0 FAIL**. All 54 JSON reports are retained in `Verification/relay-baseline/`. **Audit (2026-10-07):** PASS did not check mode fidelity, sprint noise skipped the counters, and door-break resumes were counted as hunts. The trusted numbers and the 15 harness fixes needed before these bots can grade v2 are in `Verification/relay-baseline/summary.md` and §2.6.

| Test (SP) | Pass |
|---|---|
| G1 | warning-stage-0 share ≥ 60 % at T1, ≥ 35 % at T5 (noisy bot) |
| G2 | every call logged with a cause; queued and dropped calls logged |
| G3 | 100 % of lock-ons and re-acquisitions after ≥ 3 s continuous stage ≥ 1 and ≥ 1 s continuous stage 2; `StateChanged(Chase)` never before the cue's end |
| Stage-1 share | ≤ 40 % of on-map time for the walk-away bot |
| G5 | evader bot (0.6 s reaction, 2 s bar) escapes ≥ 50 % of chases at T1, ≥ 35 % at T5, corners and eased doors only; "shut the door and walk away" ends an investigation |
| G6 | every encounter ends in Away; Withdraw → Away ≤ 25 s; RESTORE exactly when felt and truly Away |
| G7 invariants | no stage while not OnMap; no Warn override or `WarnBurst` at stage 0; no burst lamp within 2 cells of the Relay; stage-2 gap ≤ 1.2 s; no 2D stinger calls; no wake reprint at stage 0; no TargetAcquired through a shut door (touch test); wallpaper cue: every cell at state 0 or retracting at stage 0 with no chase, and no cell starts a turn within 2 cells of the Relay outside Chase (§9.6) |
| G8 | quiet bot: ≥ 1 stage-1 encounter per 10 min at T1, ≥ 2 at T5 (with the sweep) |
| Closed-zone bot | no call PENDING > 10 s; no Withdraw soft-lock |
| Regression | nav test 60 trials with the door rule rewritten, incl. breaks from both sides; map 100/100 incl. foil and plain windows identical across rebuild and shift; fixture parity; interaction tests (pry latch, touch through doors, Ajar, OPEN); autopilot PASS = OnMap reached |
| **Photosafety** | 60 Hz `LampLevel` sampling in `FrontRoomsFixtureTickTests` for every mode, Warn, REDUCE FLASHING, every tier, both stages, per lamp and room mean: fail on > 3 flashes in any 1 s or an episode > 5 s. A frame-luminance probe on a worst-case **T5** autopilot capture (stuttering room, stage-2 bursts, the lock-on, the chase wave, the ink gasp), desktop and WebGL. **Harding FPA or PEAT** on that capture before any WebGL or public build, recorded in Documentation |
| **Tier 5** | Step 0 and acceptance runs at T5 for every bot; open-hall catches reported against the cover measure (§12) |
| Playtest, 10 players | ≥ 8 name the cause; ≥ 7 read a burst as "it is near" unprompted; ≥ 8 recognise the lock-on cue on its second play, none call it a jump scare; an A/B listening test: each cue in the cue sheet identified by ≥ 8/10, and the CALLED tick never confused with lock-on or RESTORE |

---

## 15. What changed from v1, and why

### 15.1 v1 critic items

| Item | Resolution |
|---|---|
| D1 · A11 HUD readout said unbuilt; TIER on HUD | A11 rewritten as landed (assist, off by default). TIER moves to the assist and off the CAUGHT overlay; height readout stays (Q5c). |
| D2 · A7 missed the unbuilt-cell leash; 45 s clock from release | Both leashes deleted; A7b added; an unbuilt cell means Away (half RELAX). |
| D3 · A6: `Follow` breaks doors in any state; replans through doors; `TickBreak` → Hunt | OPEN / BREAK / WALL licences; replans keep `throughDoors`; `TickBreak` resumes its origin; asserted in the nav test. |
| D4 · A1 timing not reproducible | Timed release deleted; Armed at door shut + 60 s RELAX; A1 restated; legacy field kept hidden. |
| D5 · A2 Arrive through shut doors; behind-less fallback | Fallback removed; W ≥ 42 m with doors at +9; shut doors passable for routing via OPEN. |
| D6 · A9/A10 tier formula, stall rise, worse lamps on backtrack | Unique GridCoords / 150; stall rise deleted; stored first-generation tier per chunk and zone. |
| D7 · A12 hint timing, untiered speeds, walking untaught | Corrected; chase capped 4.4 m/s; placard, CalmHint at Armed, RESTORE line, pause card. |
| D8 · LoseTrack's live door feed | Deleted; perceived doors only; no intuition window. |
| D9 · field root and omen/herald rows in the LD chapter | Player-rooted octile field; LD chapter to be updated (§13 step 15). |
| D10 · ambient lamps already break ≤ 3/s; Reduce Flashing said built | REDUCE FLASHING exists since `aba5f77`; modes 1–2 rewritten for defaults; limiters; Harding/PEAT in acceptance. |
| D11 · lamp override written as if built | **Now true: it landed in `ef061ae`.** v2 builds on it unchanged (base × MIN); stutter suppression via `SetLampMode(Steady)`; explicit Warn envelope from the caller. |
| D12 · spot time with no tier term; ±70° frame | T(d) × spotMul; cone ±60° round `LookYaw`, head ±70° of body; sight range fixed. |
| D13 · Released vs Arrive; sound announces arrivals | OnMap/Armed split; Arrive backstage; removal list gated on `UsesArrive`. |
| D14 · narrative withdraw rule, EAR, ALARM ROUTE, stage-1 reason | One rule each; EAR and ALARM ROUTE out; narrative chapter to be updated. |
| D15 · hearing serialized in the scene; glass a const | Scene re-serialized; glass into tuning; legacy fields kept for compilation. |
| E1 · a quiet player never meets it | Sweep along the player's heading (v2.0 if Q2 = yes), walking heard on-map from T3, G8 floor. Open until Q2. |
| E2 · triggers and windows can be mandatory | Validator rules; quiet pry on its own latch target; plain windows per border and per zone. |
| E3 · it can arrive next to you | W ≥ 42 m, step-in re-check, per-lock-on gate incl. stage 2. |
| E4 · stage 1 a radar; stage 2 silent when still | Two fixed gaps, hysteresis; cautious looks; coil hum; stage-1 share target. |
| E5 · no-lamp rooms | Live-lamp count in 16 m, widening, audio-only groan; T5 odds cap. |
| E6 · Attention exploits | Landed winded rule; `stepTime` reset; source flags; RELAX gains 0; reset points defined. |
| E7 · no encounter cap | Pressure, onMapCap (+15 s hard end), budget, lockOnCap (≥ 5 s chases), chaseCap. |
| E8 · Withdraw/Away edge cases | Exit rules, OPEN on exit, terminal fallback, 20 s timeout, occluded-only Away, local RESTORE. |
| E9 · escape unproven at T5 | Fixed 0.7 s hold and 2.0 s lose threshold; chase 4.4; cone ±110°; worked estimate with reaction delay; G5 at T5 without sanctuaries. |
| E10 · stalling and the tier counter | Unique cells; stall rule deleted; sweep answers stalling. |
| E11 · audio vs AI distance models | One field; FMOD attenuation flattened; `PathGain`. |
| E12 · keys and door verbs vs Attention | Ease/barge/slam; unlock = ease; shut door = barrier. |
| Judges · nav 60/60 would fail | Touch off at sightRange 0; k = 0 at radius 1000; `DebugPlace` resets; door rule rewritten. |
| Judges · autopilot PASS; reveal clicks | Armed predicate; debug Summon; PASS = OnMap reached; clicks removed with states. |
| Judges · tier-dependent borders break 100/100 | Foil from the tall zone's stored tier only; validator. |
| Judges · peak slowdown reads scripted | Speed never changes; FADE uses Withdraw sight; chase-cap backstop logged. |
| Judges · threshold − 1 clamp; cancelled calls | RELAX gains 0; PENDING with a 10 s fallback and threshold − 20 on drop. |
| Judges · stale field; shared scratch; coupled ambient lamps | Own storage, edge-change rebuild; no neighbour coupling. |

### 15.2 v2 review round (blockers and majors)

| Finding | Resolution |
|---|---|
| **Blocker** · sealed zones make calls pend forever | Shut doors passable for Arrive routing via OPEN; ≥ 2 entries per zone; PENDING phase with 10 s fallback, then drop at threshold − 20; device calls queued. |
| **Blocker** · step order: no Relay between landings; step 2 needs steps 4–5 | Reordered (field → director/states behind a define → senses → flip); `UsesArrive` gate; autopilot debug Summon. |
| **Blocker** · lamp layer and winded rule already landed, conflicting | Rebased on `ef061ae`: MIN kept, Steady mode for stutters, explicit envelope; landed winded rule adopted, fresh-press dropped; escape maths redone. |
| **Blocker** · pry and smash share hold E | Separate targets: sash latch (pry, 4 s, banked) and pane (smash, 1 s). Test. |
| Withdraw soft-lock; Hunt cut off by a door | Exit 5, 20 s timeout, 25 s invariant; WALL door → listen 2 s → give up. |
| T5 hopelessness | Hold fixed 0.7 s; lose fixed 2.0 s; chase 4.4 in v2.0; reaction-delay bots; cover measure. |
| G3 gate cumulative, not per chase; partition stare | Continuous, per lock-on; stage 2 required; held > 1.5 s → cautious look. |
| Cue timbres collide; tier chime vs lock-on | Cue sheet: lock-on the only chime; CALLED non-tonal; RESTORE hum only; no give-up cue; no tier sound. |
| FADE not a ceiling; farming FADE | FADE uses Withdraw sight; hard end at cap + 15 s; lockOnCap counts ≥ 5 s chases; cautious look after a FADE loss. |
| Touch through doors | Open passage + capsule cast; test. |
| Sanctuary catch, lock-on, vanish, dead-circuit lamps | No catch or fill inside; watch in "almost" pose; always give-up + Withdraw; audio-only stage 1 inside. |
| Revisit shift moves sanctuaries; two-zone foil tier | Placed by coordinate, shift-stable; foil from the tall zone's tier. |
| Forced call via foiled border | ≥ 1 plain window per tall border. |
| Harmless detector props | Removed; ALARM ZONE signature on trigger rooms; non-detector tier hardware. |
| Quiet player bored; sweep deferred | Sweep in v2.0 if Q2 = yes, along the heading; walk heard on-map from T3; G8. |
| "Away on next unseen frame" | Geometry occlusion only, plus W ≥ 21 m or a door/corner; one `PlayerSeesRelay` test. |
| Held Shift dead mid-chase | No fresh press; held Shift resumes after the winded state. |
| F diagonal overestimate skips stage 2; chase exit while seen | Octile field; W; stage 2 at 18/24 m; gate needs stage 2; exit requires "unseen". |
| Handoff ran Away hooks | Direct OnMap → Arrive; explicit Away reasons. |
| Burst cadence vs ≥ 2.5 s steady; merged dips | Gap measured after the burst; 0.55 s spacing > dip length; episode defined; mode 1 on 0.5 s slots; room-set limiter. |
| Truth invariant broken by ink and fixture hums | Groan on HumBed from `WarnBurst` only; `LampOverrideStarted` with kind; ink envelope rule. |
| On-map hearing too short to track sound | Sprint 27 m, slam 27, barge 18. |
| Triggers dropped or delayed silently | Queued slot; CALLED tick when it goes out; no RELAX floor for devices. |
| Foil vs plain-count purity | Rank the tall zone's windows by hash; stored zone tier. |
| Director/hunter interface undefined; nav margin bug | `Arm`/`Dispatch`/`Sweep`/`SendAway`; placement in the hunter; named in-between beats; `DebugPlace` resets. |
| Door extension cost | Ease keeps 0.55 s; only a fast swing added; `Passage.Ajar`; dependencies listed; 4.5 days. |
| Lamps at the Relay's own cell; chase wave origin | Exclusion within 2 cells / its room; chase wave player-centred. |
| Stage-2 first steps inaudible | Gain floor 0.55 and a low-pass; dB acceptance check. |
| Cue overlaps the chase start | Hold = cue + 0.1 s; invariant. |
| Lamps mean three things | Lamps mean only "near"; RESTORE audio-only; no steady snap at lock-on. |
| "Almost" reads as de-escalation | Positional scuff-and-turn, captioned. |
| Withdraw deaf | Half-range hearing, one cautious look. |
| Wake reprint off-ladder | Out of v2; stage-0 invariant. |
| Deferred device call untraceable | No floor; queued calls play the CALLED tick. |
| v2.0 relied on v2.1 | G5 at T5 without sanctuaries; E1 open until Q2. |
| Q2 wording | Reworded (§16). |
| Captions lack a lock-on class; "relay" | Classes per cue sheet; no "relay" in player text. |

**Minors applied:** relax-sprinter (RELAX gains 0); triggers during Chase (noise only); slammed door ends a chase (licensed doors at 0, "unseen"); window chase trap (0.6 s frame step); pull step read as slam (input velocity); limiter desync and dark-hall lamps (room-set limiter, 16 m count); legacy compile (hidden fields); rig snap and shot cancel; `PlayerSeesRelay` defined once; FMOD attenuation; estimate in a wall (snap) and side-picking field; autopilot PASS; sweep scope and existing hooks; FADE termination and "almost" Pressure; worked example labelled; calm metric = stage 0; Ajar and re-shut readability, freeze taught; sanctuaries taught; question bundling (lettered items); thin-wall silence (W); foil facts 待核; stale lines → symbols; free sprinting on the map (half RELAX after an unbuilt Away). **Applied differently:** the audio-only stage-1 fallback is the brown-out groan on the room-tone bed, not a fixture tick (a tick is lost among ambient ticks).

---

## 16. Open questions for Red

**Answered by Red, 2026-10-04:** Q2 = **yes** (the quiet-player sweep is in v2.0); Q8 = **yes** (run the Step 0 baseline first; 关卡设计 has the brief). The other six stay open.

Lettered items are separate yes/no decisions.

1. **Relax.** After each withdraw you get 100 s of guaranteed calm at tier 1 (60 s at tier 5): noise cannot call it. Glass and alarm devices still do, at once, because you chose to trip them. Keep 100 s? (yes, or a number from 60 to 120)
2. **Quiet player** — **answered yes (2026-10-04).** After 240 s with no call at tier 1 (140 s at tier 5), the panel sends it to patrol a zone next to yours, in the direction you are heading, close enough that your lamps flicker but planned to stay out of footstep range. It is aimed at your area, never at your exact spot. (yes/no; if no, quiet players meet it only through devices, which get denser with the tier, and goal G8 is dropped)
3. **Cues outside the three stages** (each tells cause or stand-down, not position):
   a. the tripped device's own local sound;
   b. a dry ceiling latch tick when your noise calls it (with a buzz the first 3 times);
   c. the room hum returning when it has truly left, only if you felt stage 1;
   d. a faint coil hum with the steps at stage 2, so a still Relay stays audible;
   e. an off-by-default assist: a small edge marker pointing at heard steps and device trips.
4. **Panel meter.** Keep it (sprinting, barging and slamming add up; walking and easing doors are almost free), alongside glass and devices? (yes; no means only devices call it)
5. **Tiers.**
   a. Count unique cells explored, one tier per 150?
   b. Delete the 120 s stall rise?
   c. Move TIER off the always-on HUD and the CAUGHT screen into the off-by-default readout (ZONE and ceiling height stay)?
6. **First trigger room** beside windows: 1 alarm push-bar door, 2 detector room, 3 electrical room, 4 glass floor? The others unlock one per tier.
7. **New verbs.**
   a. A quiet window pry (hold E on the sash latch, 4 s) on plain windows?
   b. Foil-taped windows that always alarm?
   c. Dead-circuit sanctuary rooms it will not enter (v2.1)?
8. **Baseline first** — **answered yes (2026-10-04):** the map chat adds the seeded bots and runs Step 0 (no gameplay change) before any of this lands.

---

## 17. Sources

All marked [verified] were fetched by the research pass; [unverified] were seen only as search snippets or abstracts.

| Source | URL | Status |
|---|---|---|
| Booth, *The AI Systems of Left 4 Dead*, AIIDE 2009 (text mirror) | https://www.readkong.com/page/the-ai-systems-of-left-4-dead-michael-booth-valve-9664541 | verified |
| Shacknews, *How Evolve learned from Left 4 Dead's AI director* | https://shacknews.com/article/82787/how-evolve-assures-action-peaks-and-valleys | verified |
| Thompson, *The Perfect Organism: The AI of Alien: Isolation*, Game Developer 2017 | https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation | verified |
| Thompson, *Revisiting the AI of Alien: Isolation*, Game Developer 2020 | https://www.gamedeveloper.com/design/revisiting-the-ai-of-alien-isolation | verified |
| MCV/Develop, *A tiger in the office* | https://www.mcvuk.com/a-tiger-in-the-office-how-alien-isolations-xenomorph-took-shape/ | verified |
| MCV/Develop, *18 things we learned about Alien: Isolation* | https://www.mcvuk.com/development-news/18-things-we-learned-about-alien-isolation-last-night/ | verified |
| PC Gamer, Kadoi on Mr. X's AI and footsteps | https://www.pcgamer.com/resident-evil-2s-director-talks-mr-xs-ai-scary-footsteps-and-the-dmx-mod/ | verified (the /uk/ mirror's "footsteps never faked" line is snippet-only: unverified) |
| GamingBolt, *Seven Years Later, Mr. X Is Still Resident Evil's Scariest Stalker* | https://gamingbolt.com/seven-years-later-mr-x-is-still-resident-evils-scariest-stalker | verified |
| Vandal, *RE2 Remake: how to escape Mr. X* | https://vandal.elespanol.com/en/resident-evil-2-remake/resident-evil-2-remake-how-to-escape-mr-x-and-stop-him-chasing-you.html | verified |
| Frictional wiki, HPL2 Amnesia script functions | https://wiki.frictionalgames.com/index.php/hpl2:amnesia:script_functions | verified |
| GamingBolt, *Amnesia: The Bunker interview* | https://gamingbolt.com/amnesia-the-bunker-interview-semi-open-world-structure-stalker-and-more | verified (the tiered-noise detail is from an unfetched secondary source: unverified) |
| GameCritics, *Amnesia: Rebirth review* | https://gamecritics.com/sparky-clarkson/amnesia-rebirth-review/ | verified |
| GamingBolt, Outlast detailed (Morin) | https://gamingbolt.com/upcoming-first-person-survival-horror-outlast-finally-gets-detailed | verified |
| Dark Deception wiki, *Murder Monkeys* | https://darkdeception.fandom.com/wiki/Murder_Monkeys | verified |
| Wikipedia, *Hello Neighbor* | https://en.wikipedia.org/wiki/Hello_Neighbor | verified |
| Automaton, *The Exit 8* development | https://automaton-media.com/en/news/20240214-27192 | verified |
| W3C, *Understanding WCAG 2.1 SC 2.3.1* | https://www.w3.org/WAI/WCAG21/Understanding/three-flashes-or-below-threshold.html | verified |
| Microsoft, Xbox Accessibility Guideline 118 | https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/118 | verified |
| Game Accessibility Guidelines, *Avoid flickering images…* | https://gameaccessibilityguidelines.com/avoid-flickering-images-and-repetitive-patterns/ | verified |
| Cambridge Research Systems, HardingFPA | https://www.hardingfpa.com/ | verified |
| games.gg, Phasmophobia ghost behaviours | https://games.gg/phasmophobia/guides/phasmophobia-ghost-behaviors-secret-abilities-guide/ | verified (Fandom details: unverified) |
| Steam, Phasmophobia hunt warning signs thread | https://steamcommunity.com/app/739630/discussions/0/2956040588014783432 | verified |
| DBD Wiki, *Terror Radius* | https://deadbydaylight.wiki.gg/wiki/Terror_Radius | verified |
| DBD Wiki, *Chase* | https://deadbydaylight.wiki.gg/wiki/Chase | verified |
| GosuGamers, *Silent Hill's creepy radio static* | https://www.gosugamers.net/entertainment/news/77521-halloween-gaming-an-ode-to-silent-hill-s-unsung-hero-creepy-radio-static | verified |
| Leonard, *Postmortem: Thief: The Dark Project* (1999) | https://www.gamedeveloper.com/design/postmortem-i-thief-the-dark-project-i- | verified |
| Leonard, *Building an AI Sensory System* (2003) | https://www.gamedeveloper.com/programming/building-an-ai-sensory-system-examining-the-design-of-i-thief-the-dark-project-i- | verified |
| Wikipedia, *Dark Engine* | https://en.wikipedia.org/wiki/Dark_Engine | verified |
| Sharp, *Deus Ex: Invisible War Dev Diary #2: Sound Propagation* (2004) | http://www.gamebanshee.com/ka3h4 | verified |
| Walsh, *Modeling Perception and Awareness in Splinter Cell Blacklist*, Game AI Pro 2 ch. 28 | http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter28_Modeling_Perception_and_Awareness_in_Tom_Clancy's_Splinter_Cell_Blacklist.pdf | verified |
| Welsh, *Looking for Trouble*, Game AI Pro 2 ch. 27 | http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter27_Looking_for_Trouble_Making_NPCs_Search_Realistically.pdf | verified |
| Couvidou, *Flooding the Influence Map for Chase in Dishonored 2*, Game AI Pro Online 2021 ch. 6 | http://www.gameaipro.com/GameAIProOnlineEdition2021/GameAIProOnlineEdition2021_Chapter06_Flooding_the_Influence_Map_for_Chase_in_Dishonored_2.pdf | verified |
| Thompson, *Endure and Survive: the AI of The Last of Us* | https://www.gamedeveloper.com/design/endure-and-survive-the-ai-of-the-last-of-us | verified |
| Push Square, TLOU2 listen mode | https://www.pushsquare.com/guides/the-last-of-us-2-can-listen-mode-be-disabled | verified |
| CritPoints, *Stealth Game Spotting Deconstruction* | https://critpoints.net/2015/03/30/stealth-game-spotting-deconstruction/ | verified |
| Metal Gear Informer, MGSV Reflex Mode | https://www.metalgearinformer.com/metal-gear-solid-v-will-reward-you-for-not-using-reflex-mode/ | verified |
| Wikipedia, *Hitman (2016 video game)* | https://en.wikipedia.org/wiki/Hitman_(2016_video_game) | verified |
| Thompson, *The AI of Hitman (2016)* | https://www.gamedeveloper.com/design/the-ai-of-hitman-2016- | verified (consulted, not leaned on) |
| Switchblade Gaming, Lethal Company monster sounds | https://www.switchbladegaming.com/co-op-games/lethal-company-monster-sounds/ | verified, low authority (consulted, not leaned on) |
| Isla, *Third Eye Crime: Building a Stealth Game Around Occupancy Maps*, AIIDE 2013 | https://ojs.aaai.org/index.php/AIIDE/article/view/12663 | abstract only: unverified body (consulted, not leaned on) |

Fire-alarm facts used in the fiction were checked by the narrative chat on 2026-10-03 (`research/relay_pursuit/30_narrative.md` §7, each with a URL). §2.3 lists which hold for 1990 and which stay unverified (pre-alarm on 1990 US panels, the exact ALARM WILL SOUND and WALK, DO NOT RUN wordings, silver foil). The door-holder 'thunk' is a Foley choice, not a historical sound.

Project sources: `relay_v2_brief.md` §A–§H and `wf1.json` (workflow wf_4f02221c-e1c); `Documentation/AUDIO_CONTRACT.md`; `Documentation/DISPLAY_SETTINGS.md`; git `aba5f77` (12:26) and `ef061ae` (12:56), 2026-10-03; `research/relay_pursuit/20_level_design.md` and `30_narrative.md` (out of date, see header); `research/wallpaper_motion/20_level_design_phosphor.md`.
