# Doors by body: final design

交互设计, 2026-10-31. For Red's ask in the 交互设计 chat (10-31): doors that answer how the player moves, with no button to open them.

**What this file is**
- **No draft existed.** The synthesize stage failed with ENOTFOUND at 14:45:17, so `20_design_draft.md` was never written. This file does that synthesis and applies the two critiques in the same pass.
  - Base: "Push Through, Pull Shut", which all three judges ranked first.
  - Grafts and fixes: listed in §14 (Critiques considered).
  - Inputs: `10_research.md` in this folder, plus the workflow journal `~/.claude/projects/-Users-redwang-Desktop-ArtCenter-Fall26T7-EGAM-401A-01-Individual-Game-Project/3ba61a5c-ab34-4d9d-8a9c-2d9803eae6e3/subagents/workflows/wf_062c2a1e-f3c/journal.jsonl`. Entries 8-11 are the code maps, 15-17 the designs, 21-23 the judges and 28-29 the critics.
- **Checked.** The code facts below were re-read read-only at 15:2x-15:4x PDT against main. `FrontRoomsMapWorld.cs` and `FrontRooms3DGame.cs` have been unchanged since 13:24:09.
  - Lines marked "per reader map" come from the workflow's code maps and were not re-read.
  - Nothing ran, and nothing in the project was touched.
- **When.** Under the THROTTLE (COMMON.md:31-35), no build starts before account B returns on Nov 3 at 17:00.
  - The 11-03/04 playtest (`Frontrooms3D/Documentation/research/tutorial/30_playtest_plan.md:28`) runs today's E doors.
  - This step lands no code, so it adds nothing to the rule-25 combined test. The coordinator's pacing line was waiting on it (`handoff/coordinator.md:5`, :356).
- **Red decided 1-11 on 10-31** (§00). Red picked 1C, 6B, 7B and 11B where the recommendation differed, so the rules below are rewritten for Red's picks. Text that held only for an option Red did not take is marked "Dropped" or "Not taken". 12 is unanswered and not approved. The picks do not lift the THROTTLE.

## 00. Red's decisions (2026-10-31)

Red answered in the Codex chat: parent thread `01a19402-0853-7eb1-9400-625ee68576ba`, Red's own turns at 23:12:53Z and 23:13:18Z (16:12-16:13 PDT), lines 349-357 of `~/.codex/sessions/2026/10/31/rollout-2026-10-31T15-08-43-01a19402-0853-7eb1-9400-625ee68576ba.jsonl` (checked by 交互设计, 16:3x PDT). Red pasted the chosen option headings from `30_for_red_zh.md`. Each heading appears under one decision only, so the mapping is exact. The runner's record: `~/FrontRoomsVisualWork/workspace_repo/remote/runner/out/door-decisions-20261031.md`.

| # | Decision | Red's pick (Red's words) | What it means here |
|---|---|---|---|
| 1 | Shut model | **C** "只回头拉，完全没有关门按键" | The pull (2.4) is the only shut. No aimed USE shut, no SHUT BEHIND, no tap on a leaf: USE never moves a door. |
| 2 | A squeeze stopped short | **A** "停在原地" | The crack stays where you leave it (1.7). |
| 3 | A door held against the Relay | **A** "顶门只能撑几秒" | A BREAK at 0.6 s, and a stopped throw is a catch (8.5). |
| 4 | Slow try on a locked door | **A** "不出声" | Silent (4.1). |
| 5 | "SLOW · UNHEARD" | **A** "改词" | Reworded to name steps; the key map says doors still click (6.13). |
| 6 | Start door and stairwell north door | **B** "做成真正的身体门" | Both become body doors (4.10). This replaces the 1 m walking-at-it opening. |
| 7 | A USE OPENS DOORS setting | **B** "不加" | Not built: `HandOpens` is `!BodyDoors` (1.9). |
| 8 | Switch-on | as recommended: "和 Relay v2 一起打开，在这三件事之后" | After the combined test, the body-door bot round and the chase bot round (barge, then the pull). It does not lift the THROTTLE. |
| 9 | Barge cone and release carry | **A** "60° 和 0.15 秒" | As in 1.5. |
| 10 | Alarm push-bar doors | **A** "不能撞开" | As in 4.6. |
| 11 | End of the unlock | **B** "开锁后门只弹开 10°，你再用身体推" | The unlock ends with the leaf 10° ajar, and the body opens it (4.2). DoorShotTests and the key bots change. |
| 12 | Watch the 11-03/04 testers | unanswered | Not approved; the moderator sheet does not change. |

**11-04, Red in the 交互设计 chat:** “目前门关闭和打开还是有一点奇怪，现在我一旦把门打开之后，再去碰门就没有那种能够把门给挤关闭上。我希望你能够重点研究最新的生化危机游戏安魂曲里的门的交互是怎么做的，然后优化现有的门的交互。” (opening and closing still feel odd: once a door is open, touching it again never squeezes it shut; study Resident Evil Requiem's doors and improve ours). The cause was this design, not a slip in the code: 1.6 made an open leaf at rest a wall, a leaf hanging toward you moved only when you crossed and then flipped through the frame (Through), and 2.1 said a body never shuts a door. §2b (push-shut, built 11-04, still no button) replaces those lines. Requiem's own close is a tap of Interact at the open door (`10_research.md`, [R1]); that, and a touch-to-close on the face you pushed, were decision 14 in `30_for_red_zh.md`. **Red, 11-04, in the 交互设计 sub-chat: "要，选 B"** = touch-close (§2b.6), LANDED 06:57.

**Open items from the picks.** Neither blocks the "build regardless" list.
- **O1. A USE SHUTS DOORS assist.** The ask listed one as needed for 1C ("需要一个"USE 也能关门"的辅助设置"). Red then declined the matching open assist (7B), and the runner's record says the shut assist is not approved. Default: not built. Asked Red once, in the 交互设计 chat.
- **O2. The start door while the map streams (6B).** The ask warned that it would look locked ("会像锁住了"). 4.10 keeps the latch holding with the strain cue, never the padlock or a rattle, until the map is ready. RoomStream is 游戏视觉's, so this needs its agreement.

## 0. One line

**Opening:** every unlocked door opens from the body alone.
- **Slow gait:** lean in and the door gives after a breath, then moves only as far as you push it.
- **Walk:** walk into it and it swings open ahead of you.
- **Full sprint:** it bursts open, and you lose no step.

**Shutting** is by the body only (Red: 1C): turn back, put the dot on the leaf and step back, and you pull it shut. No device has a door button.

**Slam:** a pull within 2 s of sprinting through that door, with the step back at walk speed or more, is a SLAM.

**What stays and when:** V5's 6/18/27 m, its licences and the broken-door rules stay. One rule set runs on every device; the turn before a pull is slower on a pad or touch than on a mouse, and Red accepted that with 1C (7.4). The feature lands switched off and goes live together with Relay v2.

## 1. Open rules

**1.0 When the rules apply.**
- A new level-profile flag, `bodyDoors`, is off by default. It follows the pattern of `sashPry`, `foil` and `alarmDoor` (FrontRoomsLevelProfile.cs:76-82: "Off: today's map and game").
- The game turns body doors on only if the flag is on and `FRONTROOMS_RELAY_V2` is compiled in. That define is not in ProjectSettings.asset today: the define list at :836-848 holds only DOTWEEN.
  - The check is one `const bool` set by `#if` in an always-compiled file. All body-door logic sits outside `#if` blocks, so the Roslyn gate compiles it.
- With the flag off, `Describe`, `Kind`, `Use`, the slam curve and `UpdateAim` are exactly today's.
- Tools and tests set `map.BodyDoors` directly.
- The touch driver and the bots get a `-bodyDoors` test switch. A v1 run with body doors forced on is not a supported mode (see 1.11).
- The solver runs once per map-play frame, after the gait and stamina are final (FrontRooms3DGame.cs:2387-2412) and right before `playerBody.Move` (:2416).
  - It sees only the input move, wish × (Run or Walk) × dt. It never sees the title glide, the pull, glass or unlock step-ins, or gravity (RELAY_PURSUIT_REDESIGN.md:620).
  - It is skipped under every move lock (rig shot, glass or door shot, floor swap, pull step, climb), in a close-up, for the autopilot unless `-autopilotBodyDoors` is set, and while the flag is off.

**1.1 Contact gate.** It is the same on every device and the same for every gait. A contact counts only when all four hold:
1. **Reach.** The body's next position (feet plus this frame's move) comes within 0.355 m of the leaf face. That is 0.30 radius + 0.03 skin + 0.025 half leaf: the CharacterController's real reach.
   - PlayerClearance 0.34 (FrontRoomsMapWorld.cs:4988) is not used for contact. Its 1 cm gap would read a leaning body as a push past the 0.5° latch (LatchedDegrees, :4991).
2. **Span.** The body centre, projected on the leaf's line, lies inside the leaf span. That runs from 0.15 m off the hinge to the latch edge, and is the opening's span for a latched leaf.
3. **Pressing.** The input move presses into the face:
   - its into-face share is at least 0.5, which means within 60° of the face normal;
   - its speed is at least 1.0 m/s;
   - it is deliberate: a pad tilt of 0.2 or more (drift sits near the dead zone), any key, or a touch thumb outside the stick's dead zone.
4. **Not excluded.** The door is not one of the special doors in §4 that answer with a bump.

Leaning with no input never moves a leaf. A latched leaf leaves the frame only through a verb.

**1.2 The lean (蹭门): every non-sprint open of a latched leaf starts with one.**
- **What holds.** The latch holds the leaf at exactly 0° while a lean clock runs:
  - 0.30 s at the slow gait;
  - 0.12 s at a walk;
  - 0.40 s for a blind push, where the face normal is more than 60° from the view (backing or sidling into a door);
  - 0.30 s at any non-sprint gait while USE offers PEEK on that door's vision lite (last frame's aim result).
- **Reset.** The clock resets after 0.10 s without pressing.
- **Cues while it holds:** a local latch-strain tick with no Relay noise, a 0.15° camera nudge and a Light 0.25 tap on phones.
- **PEEK survives.** Because the leaf stays at 0°, leaning never kills PEEK, which needs the leaf shut and within ShutDegrees (FrontRoomsDoorLite.cs:205-209).
- **Release.** In the lean's last 0.10 s the lever goes down. DoorHandleTurned gives the rig's lever, the Handle sound and today's 0.3° push jolt (FrontRooms3DGame.cs:1041). Then the latch gives: EASE, 6 m, +1, one DoorMoved.

**1.3 SQUEEZE (慢慢推开 / 挤开), at the slow gait (1.44 m/s).**
- After the latch gives, the leaf follows the body (new DoorMotion.Follow, the inverse of LimitByBody at :5366). It turns only as far and as fast as you push, at most 40° a frame (DoorMaxStepDegrees, :332).
- While you push, your move is scaled to 0.8 (1.15 m/s, the leaf's weight).
- When you stop, it coasts 5° or less and stays where you left it (1.7).
- **Not taken (Red: 2A):** the coast, where the leaf swung on to 95° when contact ended.

**1.4 PUSH, at a walk (3.2 m/s).**
- After the 0.12 s lean comes the same latch beat and EASE (6 m, +1), then today's 0.55 s ease-out to 95°. ShotTimings.Open is unchanged, so the Foley cut still holds.
- **Body floor.** Each frame the leaf is at least at the angle that clears the body's predicted position. The prediction looks 0.12 m plus this frame's move plus one physics step ahead.
  - The floor is applied as a max both in the solver and in TickSwing, so the undefined Update order of game and map cannot make the leaf jitter.
  - The leaf is never slower than you.

**1.5 BARGE (撞开), at a sprint (5.5 m/s; cone and carry: Red 9A).** This is Red's "完全没有停下，最快的速度" (never stopped, at top speed). The controller has no acceleration, so sprinting is top speed.
- **When.** You are sprinting at contact, or you let go of RUN less than 0.15 s before contact and are still moving forward at walk speed or faster. The contact passes the gate (within 60°).
- **Motion.** There is no lean and no lever wait: the latch bursts on OpenDoor's latchDrawn path (:5160).
  - A new DoorLatchBurst event gives the lever snap and the strike rattle on the rig, plus the EXIT's ExitBar.
  - FastSwing runs 0.3 s to the casing stop at 95.4° (DoorBreakStopDegreesDA, :328) with today's 2° overshoot. Contact is predicted ahead, so the leaf starts before the body arrives.
  - The body floor keeps the leaf ahead, and the body never slows.
- **Cost.** BARGE: 18 m, +8. Like any sprint, it needs stamina above 0 and not being winded (FrontRooms3DGame.cs:2387-2388).
- **Rule.** Any sprint contact that opens a door is a barge, so no sprint angle opens a door quietly.

**1.6 Leaves already out of the frame.**
- **At rest at 70° or more (DoorPassableDegrees, :324):** **changed 11-04.** From the face it hangs toward (the hinge pocket side) a press pushes it shut (§2b). From the face you pushed (behind it, against its stop) a press while facing it closes it (touch-close, Red's 14B, §2b.6); looking away it is a wall, as before.
- **Below 70°, in the doorway:** a leaf moves when a body pushes it. That covers the Relay's 30° Ajar and a crack you left.
  - The first movement in an episode raises one EASE (BARGE at a sprint). This keeps RELAY:627 ("The player opens it fully with E (an ease)") true.
  - No player-driven leaf movement is ever free.
- **Through (A12 rule 3).** Pushed from the side it hangs toward, the leaf runs back through the frame and opens away from you (INTERACTION_DESIGN.md:35). **Since 11-04 only a run (a sprint, or the 0.15 s carry) into the doorway does this; a slow or walking body pushes it shut instead (§2b).** It happens only when the body is crossing the doorway:
  - the centre is inside the opening's span; and
  - the move's through-share is at least 0.5, with the predicted position past the door line. The check is like TryStartClimb's gate against `map.CrossingPoint` (FrontRooms3DGame.cs:2714, :2723).
- **The body never carries a leaf past the frame in any other way.**

**1.7 Where the leaf rests (Red: 2A).**
- Every freed latch moves the leaf at least 3°, so a leaf is either latched or open by 3° or more. The sound layer's fallback LatchStrike, which fires at 1.5° or less (FrontRoomsDoorSound.cs:233-236), therefore cannot fire falsely.
- **At 70° or more:** Open.
- **Between 3° and 70°:** Ajar.
  - `door.open` is set false and the edge joins `ajarDoors`.
  - A new per-edge angle record lets BuildEdge restore the exact angle. Today it restores only 95° and 30° (:3160-3173). NextFloor (:2132) and ResetDoorForTools (:6489) clear the record.
  - SettleAjar (:3608) pulls any Ajar leaf wider than 30.5° back to 30° (:3613-3614). It must act only on a door the Relay itself pushed, never on a crack the player left and the Relay has not touched.
- **Walk pushes and barges always run on to 95°.** A body can stop them: they then rest on it and carry on once it is clear.
- **Not taken (Red: 2A):** the coast, with no partial rests. The unlock's 10° (4.2, Red: 11B) rests as Ajar too.

**1.8 One verb per contact episode per door.**
- An episode lasts while the body stays within 0.5 m of the leaf, and ends 0.5 s after it leaves. Stop-go inching never raises noise twice.
- Each verb raises exactly one DoorMoved at `door.position`, through BeginVerb (:5010) and RaiseDoorMoved (:5020). The Relay's door identity and its BREAK licences therefore work unchanged.
- `doorSides`, `openDoors` and TouchPassageRevision (:816) are written exactly as OpenDoor writes them.

**1.9 USE never moves a door for the player (Red: 1C and 7B).**
- E, pad A or Cross, the touch USE disc and a tap on the leaf never open a latched door and never shut an open one.
  - Pressed with the dot on a latched unlocked leaf within Reach 2.4 m, they show a hint for 1.0 s, for example "WALK INTO IT". On an unlatched leaf the hint names the pull, for example "STEP BACK TO SHUT". The wording is 叙事设计's, and the dot's motion is grown from the dot (平面视觉).
- `HandOpens` is `!BodyDoors`. There is no USE OPENS DOORS setting (7B). Tools, tests and bots keep today's Use and TryOpenDoor (:3493).
- USE still does PEEK, keys, UNLOCK, locked tries and broken tries.

**1.10 Solver API.** One input struct and one result struct:
- **Input:** `{feet, inputMove, dt, gait, sprintingNow, sinceSprint, rawTilt, viewForward, peekDoor, playerMove}`. `peekDoor` is last frame's aim result, because FrontRoomsInspect.Find runs in UpdateAim (FrontRooms3DGame.cs:2940), after the Move.
- **Output:** `{moveScale, door, verb, bumpKind, blocked}`.
- The game uses `door` and `bumpKind` to call `keyHud.NoKey` and `BrokenTry` on the aimed door, as UseDoor does today. It uses `blocked` for 6.10.

**1.11 Build matrix.**

| Build | bodyDoors | Behaviour |
|---|---|---|
| v1 (today, no define) | refused | today's game, byte for byte; v1 hears every door move as one 14 m × 1.4 = 19.6 m noise (FrontRooms3DGame.cs:2159; FrontRoomsHunter.cs:33, :35) |
| v2 (define on) | off | V5 as landed: E verbs by speed |
| v2 | on | this design |
| v1 + `-bodyDoors` test switch | forced | driver and MIT only. Not supported for play: a player crack would make the v1 hunter replan every frame (FrontRoomsMapHunter.cs:1813-1817) |

Under the define, the hunter's door step is FollowShutV2 / FollowAjarV2 (FrontRoomsMapHunter.cs:1771-1777). Gating on v2 therefore keeps the shipping v1 hunter untouched, and no v1 Ajar branch is needed.

## 2. Shut rules (Red: decision 1C, the pull only)

**2.1 Superseded 11-04 (Red): a body pushes a leaf shut from the face it hangs toward (§2b).** What still holds:
- After you pass a door you opened, the leaf is on your side, and the face you pushed points at the opening. Physically, walking back into that face only drives it toward its stop. Red's 14B (11-04) makes a facing press there close an open leaf anyway, as A12's shut with its step aside (§2b.6).
- The pull (2.4) still shuts from the doorway side. No device has a door button.
- No closer exists or is added. A12 rule 5: "Doors stay where they are left" (INTERACTION_DESIGN.md:37). The EXIT seal (4.5) and the scripted shut-and-lock of the start and stairwell doors (4.10) are the only doors that shut themselves.

**2.2 Dropped (Red: 1C): the aimed USE shut.** USE on an unlatched leaf gives only the pull hint (1.9). A12 rule 6's step aside therefore never runs for a map door (INTERACTION_DESIGN.md:38 changes; item 16).

**2.3 Dropped (Red: 1C): SHUT BEHIND.** No press shuts the door you just passed, on any device, and the touch USE disc never shows SHUT or SLAM. The pass tracker stays, because the pull's slam reads it (3.1).

**2.4 The pull (回头拉上): the only shut.** It needs:
- **Leaf.** An unlatched, still leaf, not broken, not being broken, not key-held and not the EXIT. An Ajar leaf counts: the Relay's 30°, your crack, or the unlock's 10°.
- **Position.** You stand on the leaf's doorway side, not ahead of it in the hinge pocket (AheadOfLeaf, :5080).
- **Dot.** The dot sits on the leaf's latch half within 1.4 m of the handle (the handle is 0.92 m from the hinge, 1.0 m up) and has rested on the leaf for 0.3 s.
- **Step.** You then step back within 40° of straight back and hold it for 0.12 s: S, the pad stick back, or the touch stick pulled back.
- **What happens.** ShutDoor runs with no DoorPulled, because your own step clears you. If the leaf meets your body, it waits on it (LimitByBodies, :5351) and carries on once you are clear.
- **Prompt.** After the 0.3 s rest, the prompt line names the step, for example "STEP BACK · SHUT DOOR" or "STEP BACK · SLAM DOOR" (wording 叙事设计; the motion is named per device). It never shows when a step back would not shut.
- **Speed.** A slow step back is always the quiet shut. For when it slams, see 3.1.
- **Disarm.** The rest clock resets when the dot leaves the leaf, and on any shot or close-up, a climb, a floor change, Caught, a teleport or the title rooms.

**2.5 Quiet shut = EASE.** A pull without momentum runs today's 0.55 s smoothstep and a soft latch (LatchStrike Normal tier): 6 m, +1.

**2.6 No buffer is needed.** The pull needs a still leaf, and a turn plus the 0.3 s rest takes longer than any open swing (0.3 s for a barge, 0.55 s for a push). A pull is never taken while the door's own swing runs.

**2.7 Ajar leaves shut and latch whole,** whether the Relay's 30°, your crack or the unlock's 10°. A latched door is the full barrier: +9 m, blocks sight, a WALL to a Relay without a licence (RELAY_PURSUIT_REDESIGN.md:626). A door left ajar is only +4 m.

**2.8 Backing away is not a pull.** Backing out of a doorway while watching it does nothing: the dot must have rested on the leaf's latch half for 0.3 s, the prompt shows that it has, and the step must be within 40° of straight back.

**Not built:** a USE SHUTS DOORS assist (open item O1, §00). The look-back (1D) stays rejected: a reflex glance back would shut or slam doors.

## 2b. Push-shut (Red 11-04: "再去碰门…把门给挤关闭上")

Built and LANDED 11-04 06:43 (compile gate macOS + iOS, 0 errors; not yet run, rule 25; sha1s in LANDINGS). Code: `FrontRoomsMapWorld.BodyDoors.cs` (PushBodyShut, BeginBodyShut, FloorBodyShut, BodyShutClearAngle, LatchBodyShut; the contact gate in SolveBodyContact), `FrontRoomsMapWorld.cs` (Door.bodyShut; TickSwing latches a body's Through), `FrontRoomsBodyDoorContact.cs` (the start pair and the stairwell north door, same rule).

**2b.1 Which face.** A leaf out of the frame has two faces. The one it hangs toward is the face a body ahead of it touches (AheadOfLeaf: the body's bearing from the hinge is past the leaf's angle), for example from the hinge pocket of an open door, or from in front of a leaf left ajar toward you. Pressing that face (the 1.1 gate: reach, span, 60° cone, 1 m/s, a deliberate tilt) pushes the leaf back toward its frame. Pressing the other face, the one you pushed, works as before (1.6).

**2b.2 By gait** (one act per contact episode, one DoorMoved):
- **SLOW, squeeze back (挤关).** The leaf follows the body toward the frame only as far as it is pushed, at most 40° a frame, with the squeeze's 0.8 weight (1.3). Stop, and it stays where it is (Red 2A). Within 3° of the frame it drops in and latches with a soft click (DoorLatched, EASE). EASE, 6 m, +1.
- **WALK, push shut (推关).** The leaf swings back to the frame ahead of the body and latches. It uses the body's planned swing (the barge law of 11-01: the least speed that keeps the leaf past the body on its straight path, never below the verb's own, stepped once a frame before the body moves). EASE, 6 m, +1.
- **SPRINT into the leaf's face, slam (甩上).** The same swing at the fast verb's speed, ending in SlamBang. SLAM, 27 m, +12. Running into the face of a door is a shut act, so 3.6's "no slam without a shut act" still holds.
- **SPRINT into the doorway (crossing): the barge goes through, as before.** The leaf runs back through the frame and opens away from you (Through, BARGE). In a chase you never stop for a door.

**2b.3 After the latch.** The door is shut. If you keep pressing, it is a latched door like any other: the lean (0.12 s walk, 0.30 s slow), then it opens away from you with its own noise. So walking at a leaf that hangs toward you shuts it with a click, and pressing on opens it to the far side.

**2b.4 Which leaves.** Whole leaves only: not broken (a broken leaf at rest stays a wall), not the EXIT (as for the pull), not being broken, not key-held, no armed alarm bar. A leaf the Relay is moving answers BLOCKED as before. A Relay standing in the sweep stops the leaf on its body and holds the body pressing it (BLOCKED). The leaf never swings into the player: it moves away from the body that pushes it.

**2b.5 Fixed on the way.** Before, a slow body crossing toward a leaf that hung toward it started the squeeze open (BeginBodyFollow) from the wrong side, and the floor swung the leaf through the body. Now a slow front press is the squeeze back; a slow crossing of a leaf no body may push shut (the EXIT) goes through as a walk does.

**2b.6 Touch-close: the face you pushed (Red, 11-04: "要，选 B" on decision 14; LANDED 06:57).**
- **When.** A plain leaf open at rest (70° or more, so it cannot go further) is pressed on the face the body pushed (1.1's gate), with the view facing that face (within 60°).
- **The lean.** The leaf holds for the walk's lean (0.12 s; 0.30 s slow) while the body is held.
- **The shut.** Then the leaf runs A12's shut (ShutDoor): it swings back to its frame and latches. The game steps the body clear (DoorPulled, A19's "站在门板那边关门时照现在让一步", at most 0.35 s at 4 m/s). Under the Relay's sight or in a chase there is no step, and the leaf waits on the body until it moves (A12 rule 6).
- **Noise and curve.** As the pull's (2.5, 3.1): a quiet EASE on the 0.55 s smoothstep, 6 m. It is a SLAM (0.3 s, 27 m) after a run through this door within 2 s, pressed at a walk or more.
- **No accidental close.** Looking away (sliding along the leaf, looking out of the doorway), the open leaf stays a wall.
- **Start pair and stairwell north door.** The same rule in FrontRoomsBodyDoorContact, but those doors have no step hook, so the leaf waits on the body.
- **Not taken.** Requiem's own close, a tap of USE at the open door (14C), would change 1C.

The pull (2.4) is unchanged.

**2b.7 Tests.** FrontRoomsBodyDoorTests gains two rows.
- **PushShut.** A real CharacterController opens and crosses the door, then presses the hanging face from the hinge pocket. A walk latches the leaf with one EASE noise and no stall. A slow press leaves it partway, where it stays (2A).
- **TouchClose.** After the crossing the body presses the pushed face. Looking away, nothing happens. Facing it, the door closes after the lean with one EASE noise and DoorPulled; the leaf latches once the body steps clear.

The combined test list is in `handoff/interaction.md` §7.

## 3. Slam rules

**3.1 Momentum.** A pull is a SLAM when both hold:
- your feet crossed this door's line sprinting within the last 2.0 s (recorded at the crossing, by edge); and
- the step back is at walk speed or more. A slow step back right after a run is quiet: you chose care.

The 2.0 s window replaces V5's 0.5 s after-sprint grace for the player (FastAfterSprintSeconds, FrontRooms3DGame.RelayV2.cs:49). A pad cannot meet 0.5 s if it has to turn first: 160°/s is the cap (FrontRoomsInput.cs:93). V5's grace stays for the bots' TryOpenDoor.

**3.2 The prompt shows what the pull will do.** While momentum is live, the pull's line reads SLAM ("STEP BACK · SLAM DOOR", with the motion named per device); otherwise it reads SHUT. A 27 m slam is never a surprise.

**3.3 The motion.**
- **Today:** the slam uses the smoothstep and reaches the frame at almost 0°/s, so its bang is only a sound.
- **With bodyDoors on:** it runs 0.3 s (FastSwing) on an ease-in curve, `angle = start × (1 − u²)`, which meets the frame at about 630°/s. FrontRoomsDoorSound's impact read then lands in its Slam tier, and SlamBang plays on DoorLatched(Slam). Still 27 m, +12.
- With the flag off, today's curve stays, so V5's DoorVerbs timing rows do not move.

**3.4 The chase pass (Red: 1C).**
- **The move.** Sprint into the door (BARGE) and keep going. Or stop just past it, turn, put the dot on the leaf for 0.3 s and step back hard: the door slams in the Relay's face. There is no slam without that stop and turn.
- **Its price is time, the turn and the door, not Attention:**
  - Attention is a build-up clock, frozen while the Relay is on the map (FrontRoomsRelayDirectorPolicy.cs:524-527).
  - A chasing Relay that sees you is deaf, and a door it sees move gives it a BREAK licence (FrontRoomsMapHunter.V4b.cs:691-705).
  - From the stop to the latch: the turn (mouse about 0.1-0.2 s, pad about 1.1-1.8 s, touch about 0.9-1.3 s; 7.5), the 0.3 s rest, the 0.12 s step and the 0.3 s slam. That is about 0.9 s on a mouse, 1.6-2.0 s on touch and 1.9-2.5 s on a pad (ESTIMATE).
  - In that time a Relay at 4.2-4.4 m/s (FrontRoomsLevelProfile.cs:345) closes about 4 m on a mouse player, 7-9 m on touch and 8-11 m on a pad. If it reaches the arc first, the leaf stops on its body and the slam is spent.
  - When the slam lands, it buys one break: 2.5 s at tier 1, down to 1.9 s (:346), roughly 8-11 m. The door is then lost for the run.
  - So the chase slam favours fast turners. Red accepted that with 1C; the same rule runs on every device.
- **Before switch-on:** a chase bot measures door chains with the pull at mouse, pad and touch turn rates (§10, item 23). If 系统设计 wants more price, the lever is stamina (0.4 s per slam, ESTIMATE; off by default), never Attention.

**3.5 A slam that stops on a body.** This covers the Relay's body, or yours in the sweep.
- A new DoorSlammedInto event, raised from TickSwing's blocked branch for a fast shut (FrontRoomsMapWorld.cs:5749-5753), gives an impact sound, a 0.6° jolt and a Heavy 0.8 haptic.
- Today a leaf that comes to rest mid-swing plays nothing (FrontRoomsDoorSound.cs:238).
- The slam's noise was already raised at the swing's start.
- On the Relay, the leaf does not shut, and the Relay breaks it with the licence the slam gave it.

**3.6 No slam without a shut act.**
- A sprint pass never slams the door by itself. That would be a closer (A12 rule 5), and it would charge 27 m to every sprint through a door.
- A look-back alone does nothing.
- Since 11-04 an open leaf answers a deliberate press on either face: the face it hangs toward pushes it shut (§2b; a run into it is a slam), and the face you pushed, faced, closes it after the lean (§2b.6; a slam only within the 2 s window). Each is a shut act; brushing past or looking away does nothing.

## 4. Special doors

1. **Locked, no key.**
   - **SLOW contact** is a silent try (Red: 4A): no rattle and no Relay noise, only the padlock HUD and a Light 0.3 haptic.
   - **WALK or SPRINT contact** gives one rattle per episode through Use's rattle path, DoorLocked + DoorRattled + the two kicks: 6 m, +1 under v2. Today's 0.5 s Rattle guard sits on top.
   - **Sprinting into it** stops you dead with a 1.0° jolt. There is no break and no barge, and 6.10 applies.
   - **USE** keeps today's deliberate rattle.
   - **EXIT before its floor key:** ExitChainTug plays, and its locked-try counter counts episodes, not frames.
2. **Locked, key in hand.**
   - Contact never starts the unlock shot: a 1.55 s move-locked shot set off by a bump in a chase would be a trap. Contact never rattles either.
   - A muted knock plays and "E · UNLOCK" pulses.
   - **The unlock ends at 10° (Red: 11B, "开锁后门只弹开 10°，你再用身体推").** The shot plays as built until the latch draws back (ReleaseKeyTurn → OpenDoor with DoorOpenSource.Unlock, FrontRoomsMapWorld.cs:4903-4908). Then the leaf pops 10° away from you and stops. From then on it is Ajar (1.7, 8.4): +4 m, blocks sight, and the Relay pushes it in 0.4 s. You open it the rest of the way with your body: a push or squeeze is the EASE, a sprint the BARGE (1.6).
   - **The open's noise moves to your push.** The pop raises no DoorMoved, so an unlock still costs exactly one door noise, and you choose which by how you push. The key turn's own sounds stay local.
   - DoorShotTests and the key bots change (§10, items 11, 23 and 27).
   - Body contact is ignored while the key turn holds the door (IsHeldForKey, FrontRoomsMapWorld.cs:4864).
3. **Broken** (open for the run, hanging crooked at 80°).
   - No verbs and never any noise (RELAY_PURSUIT_REDESIGN.md:628-641). The leaf never moves under a body.
   - A walk or sprint press plays the wobble: DoorBrokenTried, its 0.55 s guard, BrokenGrind and a Soft haptic, at 0 m and 0 Attention. SLOW contact does nothing.
   - USE still answers BROKEN / WON'T SHUT.
4. **Hands off.** These are solid, and nothing happens beyond the collider:
   - a door the Relay is breaking (IsBeingBroken, :5032);
   - a door held by a key turn, or a leaf in Lever, Throw or Bounce motion;
   - any Relay swing in progress, and everything while DoorsPaused.

   A leaf held by the Relay's body or arc raises the new DoorBodyBlocked event once per episode: a dull thud, a 0.4° jolt and a Medium 0.5 haptic. "It won't move" then reads as "something is behind it".
5. **EXIT** (steel, crash bar).
   - **From the maze side (N)** it is the most natural body door in the game. A walk or slow push presses the bar (DoorHandleTurned → ExitBar + BarPushed), and a sprint bursts it (BARGE, ExitBar on DoorLatchBurst).
   - **From X, before the seal,** a body push opens it into N, away from the pusher. That is A12 rule 1 for every door, the EXIT included (INTERACTION_DESIGN.md:33, :44). So a player who shut it behind them in X can always get back out.
   - **The seal.** Once you are 1.4 m into X (ExitSealDepth, FrontRooms3DGame.cs:356), the seal shuts and locks it as today (World verb). It stays the only door that shuts itself.
   - **Sealed:** solid and silent. A press gives a dull steel thud as sound only, with no Relay noise.
6. **Alarm push-bar doors** (C16 / V7b; `alarmDoor` is off today, FrontRoomsLevelProfile.cs:82) (Red: 10A).
   - An armed alarm door never barges, and never opens from a graze, a slide, your back or the slow gait.
   - Sprinting into it is a hard stop: the bar's strain cue, ALARMED shown without aiming, and 6.10.
   - It opens and trips only on a push you face (eye within 60° of the face normal), straight (move within 30°) and held 0.4 s while the bar visibly travels. The trip itself stays in OpenDoor.
   - The prompt reads ALARMED alone, with no "E · OPEN DOOR" (today: AlarmDoorPrompt, FrontRoomsMapWorld.cs:6823).
   - Also add the missing AlarmDoor case to UseKindOf (FrontRooms3DGame.Mobile.cs:479-495) and to KeyHud.AimOf (FrontRoomsKeyHud.cs:214-233). Both fall through to Hidden / None today.
7. **Run! goal doors** are never locked (LockedByRule), so the Run! sprint ends in a barge with no stop.
8. **Relay-ajar doors** (30°, `door.open` false, in `ajarDoors`).
   - A body push in the doorway opens it: one EASE, which keeps RELAY:627.
   - From the side it hangs toward, a crossing push is a Through (1.6).
   - The pull shuts it and latches it.
9. **Station.** Floor 1's entrance D is a static, render-only copy of the start door with no collider; the body meets the map's wall there (FrontRoomsStationDress.cs:237-242).
   - Optional polish: a soft thud when a body presses it (游戏视觉 + 关卡设计).
   - Map doors around the station follow the general rules.
10. **Start door and stairwell north door: real body doors (Red: 6B, "做成真正的身体门").** Neither is a map door today:
    - **The start door** is RoomStream's terminal door, a pair of leaves. It opens by proximity within 4 m in play (TickNearbyDoors, FrontRoomsRoomStream.cs:788-812).
    - **The stairwell north door** opens by itself once the floor is ready, or after StartDoorHoldLimit (FrontRooms3DGame.cs:1902-1913), and shuts 4 m out (:1920). Today it raises no door noise: the game calls `startStairwell.Open` and logs an event, nothing more.

    **The change.** Both stop opening by themselves and take the body like a map door:
    - **One solver.** The solver (§10, item 1) gets a small door adapter: hinge, leaf collider, swing call, latch state and "ready". Map doors, each start-door leaf and the stairwell north door all register through it. The gate (1.1), the leans (1.2), SQUEEZE, PUSH and BARGE (1.3-1.5) and the episode rule (1.8) are the same. No second solver is written.
    - **The pair.** Each start-door leaf answers its own half of the opening, and a body crossing the middle pushes both. Contact on one leaf never moves the other.
    - **While the map streams (open item O2).** The leaf stays latched until the map behind it is ready, as today's hold does (up to StartDoorHoldLimit). Contact gives the lean's strain cue for as long as the hold lasts, never the padlock, a rattle or the blocked thud, and the latch gives the moment the map is ready. A sprint into it while loading stops you like a lean, with no sprint drain (6.10). So it reads as a heavy door, not a locked one.
    - **Noise.** Neither door raises Relay noise. The start door opens before any Relay exists, and the north door opens out of the safe-zone stairwell, as today. Their wooden Foley (hinge, latch, swing) follows the body verbs (声音设计).
    - **The shut stays scripted.** The start door's shut-and-lock behind you (the title handoff) and the north door's 4 m shut stay exactly as today. With the EXIT seal, they are the only doors that shut themselves (2.1).
    - **Teaching.** The first door of every run is now a body door, so a player's first lesson is that doors open when you walk into them.
    - **Owners.** 游戏视觉: RoomStream's pair, its look and loading cue. 关卡设计: the adapter in the solver and the north door in Game.cs. 声音设计: the title door Foley on body verbs. 交互设计: driver rows. Costs are in §10, items 1, 15, 17, 18 and 28.
11. **Door peek (C36).**
    - It needs a leaf that is shut, still and within ShutDegrees (FrontRoomsDoorLite.cs:205-209), within 1.0 m.
    - You can stand 0.4-1.0 m away or lean with no input without unlatching the door. While PEEK is offered, every non-sprint lean is the 0.30 s one.
    - When not sprinting, USE on the lite is PEEK, as today. At a sprint it is not (6.6).
    - The live close-up locks movement (FrontRoomsInspect.cs:736-737), so nothing gets pushed during a peek.
    - A crack of 3° or more cannot be peeked; you look through the gap instead.
12. **A43.** No door verb pauses the game. The squeeze, the barge, the slam and the pull run live, and the unlock shot is live too.

## 5. Feel

**Chase.**
- You never stop for a door. With RUN held you hit it square, the latch bursts with a crack, the camera takes the shove and the leaf bangs off the casing while you keep full stride. The whole floor heard it.
- To shut it on the Relay you need the nerve to stop: turn, put the dot on the leaf and step back hard, and you pull it into its frame in the Relay's face. Then you see the Relay meet a shut door, and you know it heard.

**Sneak.**
- At the slow gait you lean into a door, and for a breath it holds. Then a soft click, and the leaf moves only as far as your body does. Stop, and it stops: a crack you can see through. Leave it, and it stays ajar, half a barrier.
- Turn, put the dot on the edge and ease back, and you draw it home until the latch catches.

**What doors tell you.**
- A door that will not move under your shoulder is the worst door in the building, because something is holding it from the other side.
- A door you hold against the Relay buys you a few blows, not safety.

## 6. Safeguards

1. **Strafing past a door.** The move runs parallel to the face, its into-face share is under 0.5, and there is no contact.
2. **Sliding along a wall into a door.**
   - Contact needs the body centre inside the leaf span and a press within 60° of the normal.
   - Slides shallower than 30° from the wall open nothing.
   - A deliberate 45° press into a latched door does open it: that is a real push, priced as a push or a barge.
   - A corridor run past a door that opened into the corridor never flips it. Since 11-04 (§2b) a run straight into the face it hangs toward slams it shut: a collision with the door, priced as a slam. On the face you pushed only a facing press closes it, after the lean (§2b.6). A slide along either face, under 30° from it, does nothing (the cone).
3. **Backing into a door while watching the Relay.**
   - You cannot sprint backwards (RunAhead, FrontRooms3DGame.cs:2273), so this is a blind push at most.
   - A soft knock and a 0.15° nudge tell you a door is at your back. It gives only after 0.40 s.
   - A locked door rattles once (silently at the slow gait) and stops you. An alarm door never trips from behind.
4. **Turning beside a door.** Turning moves only the camera. Nothing happens unless the camera-relative move actually drives your body into the face.
5. **Leaning on a door.** With no input there is no verb. Contact uses the real 0.355 m reach, so a leaning body never reads as a 0.7-2° push.
6. **PEEK and glass never steal a panic press.**
   - While you sprint, USE never opens a live close-up, PEEK included. The door peek has no Relay cancel (FrontRoomsInspect.cs:738-739).
   - The pull's rest never arms on a leaf you can PEEK: PEEK needs a shut leaf, and the pull an unlatched one.
   - A latched leaf under the dot then takes the press for the hint only.
   - Keys can still be taken. Reading close-ups, which pause the game (A43), are unchanged.
   - Panes keep E: aim wins, as today.
7. **Pad drift and gentle thumbs.**
   - Door contact ignores pad tilts under 0.2. Any tilt under 0.5 becomes the slow gait (GaitMove, FrontRooms3DGame.cs:2284-2289), so drift alone would otherwise creep you into a door.
   - The 0.30 s slow lean and its 0.10 s reset absorb a stumble.
   - On touch, a RUN-button run still needs a tilt of 0.5 or more to sprint (:2387), so a gentle thumb never barges. UNVERIFIED that tilt 0.5 is exactly the 36 pt ring.
8. **Pressing RUN at a door.**
   - The first sprint frame into it is a barge: the honest result of choosing to sprint.
   - There is deliberately no "sprint door bash" off switch (Call of Duty has one). With contact doors it would buy a silent sprint through every door. Letting go of RUN is the switch.
9. **No getting stuck.**
   - A leaf never enters a body (LimitByBodies, FrontRoomsMapWorld.cs:5351-5357). Once its latch is free, the body floor stops it from ever holding back the body that pushes it.
   - The only stops left are the lean, a locked, broken or sealed door, an armed alarm door, and a leaf held by the Relay. Each gives a cue, and you can always back away.
   - Two doors whose hinges meet at one cell corner could box a player in with their two open leaves (BuildEdge puts every hinge at the edge's start, :3147). The generator stops making that pair (§10, item 10).
10. **Blocked contact never drains you.** When the solver reports a blocked contact (locked, alarm resisting, Relay-held, sealed or broken), sprinting is cleared for that frame. No stamina drains and no sprint step fires (FrontRooms3DGame.cs:2391-2404, :2419-2440).
11. **No trapping.**
    - Doors never shove, launch or pin the player (the Gone Home failure, `10_research.md` §5).
    - No forced step is left: with no aimed shut (1C), A12's step aside never runs for a map door.
    - Holding a door against the Relay is answered by a catch, not a shove (8.5).
12. **Run forward only.** A barge happens only ahead of you (run within 70° of the view, contact within 60° of the face), so you always see what you barge. The pull needs a stop: turning with RUN and forward held steers you back through the doorway (movement follows the camera, :2390), past the open leaf, which is a wall at rest and is never pulled that way.
13. **"SLOW · UNHEARD" (Red: 5A, reworded).**
    - Slow steps stay silent, and a slow try on a locked door is silent.
    - A squeeze still clicks the latch: 6 m, +1. That is Red's own V5 figure ("慢慢推开：6 米内能听到").
    - The hint's own comment asks for a rewording if the creep ever makes noise (FrontRooms3DGame.Mobile.cs:34-39). 叙事设计 rewords it so it names steps, and the key map's SLOW line adds that a door still clicks.
14. **No noise spam.**
    - One verb per episode, one rattle per episode, and walls for open leaves.
    - The Relay's own door moves never raise DoorMoved (RelaySwing).
    - The Clea developer scrapped door-creak reactions for interrupting too often (`10_research.md` §5.10).
15. **Frame order and physics sync.**
    - `m_AutoSyncTransforms` is 0 (ProjectSettings/DynamicsManager.asset:22), and no execution order is set between MapWorld's and the game's Update.
    - The solver poses leaves for the body's predicted position: 0.12 m + this frame's move + one physics step ahead. A pose up to one step stale is therefore already clear.
    - There is no per-frame Physics.SyncTransforms by default. One SyncTransforms on a chunk-rebuild frame cost 15-20 ms of CPU and was removed (FrontRoomsGlassDebrisWorld.cs:2818-2819).
    - The play-mode no-stall row decides whether a sync is needed. If it is, it runs only on frames where a leaf moved, and that cost is measured on chunk-build frames.
16. **Tools and bots keep today's semantics.** HandOpens stays true for them, and the autopilot skips contact and keeps TryOpenDoor. MapInteractionTests, DoorShotTests and the Relay baselines are untouched until a bot round turns body doors on.

## 7. Parity

1. **One rule set.** No door rule is gated by `FrontRoomsHandheld.Active` (Red 10-30: "三端保持一致").
2. **Gaits → verbs.**

| Gait | Speed | Verb | Desktop | Pad | Touch |
|---|---|---|---|---|---|
| Slow | 1.44 m/s (× 0.8 while pushing) | SQUEEZE | Shift + W | stick tilt under 0.5 (SlowTilt, FrontRooms3DGame.cs:2266) | thumb inside the 36 pt ring (× 1.4 on iPad) |
| Walk | 3.2 m/s | PUSH | W | tilt 0.5 or more | thumb beyond 36 pt |
| Sprint | 5.5 m/s | BARGE | Ctrl (or its locked key) + W | LB or L3 + forward tilt 0.5 or more | past the 84 pt run line within 70° (RunHalfAngle, FrontRoomsTouchControls.cs:125), or RUN button + tilt 0.5 or more |

   - Winded refuses the sprint on every device.
   - The run-line haptic (Rigid 0.8) doubles as "the next door will barge".
3. **Thresholds are in degrees, metres and seconds, never frames.** Values: a 60° cone, 0.355 m reach, a 0.15 m hinge dead zone, at least 1.0 m/s and tilt 0.2, leans of 0.30, 0.12, 0.40 and 0.30 s with a 0.10 s reset, a 0.15 s barge carry, a 0.5 s and 0.5 m episode, the 2.0 s momentum window, the pull's 1.4 m, 0.3 s, 40° and 0.12 s, the alarm's 0.4 s, and the 0.6 s held door.
4. **The pull is the only shut on every device (Red: 1C).**
   - Desktop: S, or whatever key is bound to back. Pad: the left stick back. Touch: the stick pulled back. No device has a door button.
   - The rule and its numbers are the same everywhere; the turn before it is not (7.5). Red accepted that a fast turner slams sooner.
5. **The pull needs a turn.** The time it takes is honest per device:
   - **Mouse:** about 0.1-0.2 s for 180° (2.1° per input unit, FrontRoomsInput.cs:74, per reader map).
   - **Pad:** 1.125 s for 180° at the 160°/s full-tilt cap (FrontRoomsInput.cs:93). With the squared response, a diagonal takes about 1.24 s and 70% tilt about 1.8 s (the "Red-literal" design's figures, ESTIMATE).
   - **Touch:** 0.20° per point × LOOK SPEED (FrontRoomsTouchControls.cs:141), so 900 pt for 180° at 100%, about 2-4 strokes and 0.9-1.3 s (ESTIMATE).

   Add the 0.3 s rest and the 0.12 s step. A pull-slam fits the 2.0 s momentum window on a mouse, a pad at full tilt and touch at 100% LOOK SPEED or more. Touch at 60% misses it (ESTIMATE).

   Under Red's 1C this is a chase difference between devices. A mouse slams with time to spare; a pad at full tilt and touch at 100% LOOK SPEED just make the 2.0 s window; gentler tilts and slower look speeds get the quiet shut instead (ESTIMATE).
6. **A pull after a sprint needs a stop.** Stop within about 1 m past the door line, then turn. The pull needs the latch half within 1.4 m (2.4), and at a sprint you cover 1.4 m in about 0.25 s. Movement follows the view (:2390), so holding forward while turning walks you back through the doorway.
7. **Feedback.** Sound and camera are the same on all three platforms and carry every verb.
   - Haptics exist only on phones; iPad has no haptic engine.
   - Pads get no rumble today: nothing in Assets drives Gamepad rumble (per reader map). Optional pad rumble of 80-150 ms is in the plan.
   - No verb relies on haptics alone.
8. **Prompts per device.**
   - Desktop names the bound key.
   - With a pad in use on Mac or Windows, the desktop prompt should name the pad's button. Today it always names the keyboard key (DisplayPrompt, FrontRooms3DGame.Mobile.cs:86), and the pull's step must be named per device (S, STICK BACK, PULL BACK). This is a fix for 交互设计.
   - No device shows a prompt on an unlocked, latched door.
9. **Frame rate.**
   - The 40° per frame hinge cap (FrontRoomsMapWorld.cs:332) keeps a barge through the middle of the opening clean down to about 30 fps.
   - Below that, a sprint into the hinge third can touch the leaf for one frame: a hitch, not a stop. WebGL, which is a separate track, may see it.
   - ProMotion at 120 Hz is fine.

## 8. Systems impact

1. **Noise and Attention per verb are V5's, unchanged** (RELAY_PURSUIT_REDESIGN.md:622-627, :683).

| Verb | Hearing | Attention | Covers |
|---|---|---|---|
| EASE | 6 m | +1 | squeeze, push, Through, opening an ajar leaf (the first push after an unlock included, 11B), the quiet pull |
| BARGE | 18 m | +8 | |
| SLAM | 27 m | +12 | |
| RATTLE | 6 m | +1 | a walk or sprint bump, or a USE try |

   Zero-cost cases: the lean, a slow try on a locked door, a broken wobble, a key-in-hand bump, a blocked thud, and open leaves, which are walls.
2. **The event contract is unchanged:** exactly one DoorMoved per verb, at `door.position`. The Relay's door identity, its BREAK licences (heard, and SawDoorMoveV2, FrontRoomsMapHunter.V4b.cs:695) and its OPEN licences do not change. New events are appended: DoorOpenSource.Body, DoorMotion.Follow, DoorLatchBurst, DoorBodyBlocked, DoorSlammedInto and DoorThrowStoppedByPlayer.
3. **Barriers are unchanged:**
   - **Latched shut door:** +9 m, blocks sight, a WALL without a licence.
   - **Ajar door:** +4 m, blocks sight, and the Relay pushes through it in 0.4 s (RelayPushSeconds, FrontRoomsMapWorld.cs:3559).
   - **Broken door:** an Open passage at 0 m.
4. **Ajar gets wider (Red: 2A; and the unlock's 10°, 11B).**
   - A crack a player leaves between 3° and 70° is Ajar. The v2 hunter already handles Ajar: FollowAjarV2 (FrontRoomsMapHunter.V3b.cs:505) calls PushAjar (FrontRoomsMapWorld.cs:3585), which needs `door.open` false, so the rest sets it.
   - The v1 hunter never meets it, because body doors need v2 (1.11).
   - Ajar stops meaning only "the Relay came through" (RELAY:676: "an honest trace"). The playtest should check that players can still read "it has been here" from a door.
5. **The held door (new rule, Red: 3A).** Today a body can hold a door forever (read from the code, not run: UNVERIFIED at runtime):
   - FollowAjarV2 re-pushes a held leaf without end, "A leaf held short of passable (a body in the way, a rebuild): push again." (V3b.cs:520-521). It never reads `breakLicenceV2`: the licence is read at :102, :364 and :460 (FollowShutV2), never in FollowAjarV2 (:505-536).
   - BreakDoor throws the leaf away from the Relay, toward the holder (FrontRoomsMapWorld.cs:3445-3469). The blocked branch rests it on the body at BounceRestDeg 80° (:5756-5759; FrontRoomsShotTimings.cs:184).
   - A broken leaf under 70° reads as ClosedDoor (:3424-3425), and both hunters then wait at the stance (V3b.cs:450-455; FrontRoomsMapHunter.cs:1791-1797).
   - The stance is 0.45 m off the door line (BreakFaceDistance, FrontRoomsMapHunter.cs:75), so the bodies stay apart beyond the 0.7 m catch. A catch needs sight, or touch through an Open passage (:870-874; V4b.cs:341-360).

   The new rule has two parts:
   - **(a) v2.** A Relay OPEN or PUSH whose leaf has rested on the player's body for 0.6 s becomes a BREAK act at once, with no licence needed, and the Relay hears the player at the door. You feel each blow through the leaf.
   - **(b) Both builds.** A break throw stopped by the player's body, outside a sanctuary, is a catch.

   Holding a door therefore buys about 0.6 s plus one break (1.9-2.5 s), then it is over. Shutting the door and running is always better. No mover pushes the player, because the pull-step mover is cancelled under sight and in a chase (FrontRooms3DGame.cs:2377, :2689).
6. **Chase prices are in time and doors (3.4), not Attention.** Drop the "+20 Attention per panic pass" framing: Attention is frozen on the map (Policy.cs:527).
7. **PlayerFast stops mattering for the player** with body doors on. The solver classifies from the gait, and the game passes the shut's verb from the momentum record. RelayGaitV5 keeps setting PlayerFast for the bots' TryOpenDoor (FrontRooms3DGame.RelayV2.cs:337).
8. **The v1 runtime is unchanged:** body doors need v2. The held-door catch (5b) is the only change that also reaches v1 (Red: 3A).
9. **Bots keep TryOpenDoor,** so the baselines and G5 stay comparable. They measure nothing about body doors until the `-autopilotBodyDoors` round, which runs in a v2 clone.
10. **Expected mix.**
    - More barges: running into a door becomes the normal way through.
    - More EASEs: every door you walk into opens.
    - Fewer shuts: each costs a stop and a turn (1C). Chase slams favour fast turners; the chase bot measures how often they land per device.
    - G5's "shut the door and walk away" premise (RELAY:831, per reader map) needs re-checking by 系统设计 and 关卡设计调研.
11. **The swing lock.** V5's "no re-swing until the swing ends" (RELAY:625; IsSwingLocked, FrontRoomsMapWorld.cs:4829) still holds. It never blocks a body: a body-driven leaf is floored, not swing-locked. The pull needs a still leaf, so it never meets the lock (2.6).
12. **C16 alarm doors** keep their single trip on the first player open, now behind the committed 0.4 s push. The trigger budget is unchanged.
13. **The Relay's body still only blocks leaves** (A12 rule 10, INTERACTION_DESIGN.md:42). It never opens a door by walking into it, so the WALL and licence economy holds.

## 9. Feedback

**Sound** (声音设计; offline renders only, rule 24).

| Moment | Sound |
|---|---|
| Lean | LatchStrain (new, local, no Relay noise) |
| Squeeze | Handle + EaseClick at the latch, then the Swing bed at low angular velocity: pivot grain, at most a short dry stick-slip, never a horror creak |
| Push | Handle + EaseClick, then a mid-speed Swing |
| Barge | LatchBurst (new transient, FastSwing as its onset), a fast Swing, StopLimit at the casing |
| Quiet shut | Swing + LatchStrike at its Normal tier |
| Slam | fast Swing + SlamBang on DoorLatched(Slam) |
| Slam into a body | SlamInto impact (new) on DoorSlammedInto |
| Unlock pop (11B) | the key turn's latch draw, then a short swing to 10° |
| Start or stairwell door while the map streams (6B) | LatchStrain, held |
| Locked bump | Rattle (ExitChainTug on the EXIT) |
| Slow try | a muted knob stop |
| Broken | BrokenGrind |
| Blocked | a dull leaf thud |
| Sealed EXIT or static door | a dull thud |
| Brace | the existing blows through the leaf |

System fixes:
- VelocityFull goes from 320 (FrontRoomsDoorSound.cs:42) to about 700°/s, so slow, walk and sprint swings (about 165, 370 and 630°/s, ESTIMATE) sound different.
- Keep the Swing instance alive through short stalls.
- Take the verb from DoorSwingStarted.
- Guard the fallback LatchStrike (:233-236).
- Give the 45° snap rule a flag from the map for body sub-steps (:150, per reader map).
- Play ExitBar on DoorLatchBurst for the EXIT.
- The Swing bed has no tracks yet (Tools/audio/fmod_frontrooms.py:744-748: "cut from Freesound 683823 once it is downloaded"), so squeezes sound thin until that content exists.
- SoundDirector subscribes to DoorHandleTurned and DoorLatched (FrontRoomsSoundDirector.cs:641-642). DressEvents subscribes to those two plus DoorRattled, DoorBrokenFrom, DoorBrokenTried and DoorUnlocked (FrontRoomsDoorDressEvents.cs:28-33). Neither hears a barge, because a barge raises no DoorHandleTurned. That is why DoorLatchBurst is new.

**Camera** (FrontRooms3DGame.cs jolts; no door verb ever takes over the camera, A43).

| Moment | Jolt |
|---|---|
| Lean | 0.15° nudge |
| Latch beat | 0.3° (today's push, :1041) |
| Barge | a 1.0° forward shove, 0.15 s decay, no loss of speed |
| Quiet latch | 0.3° (:1042) |
| Slam latch | 0.8° |
| Slam into a body | 0.6° |
| Sprint into a locked door | 1.0° plus the two rattle kicks |
| Blocked | 0.4° |
| Blind bump | 0.15° toward the contact |
| Brace blows | as built (the "Momentum Doors" design says 0.2-0.6° within 8 m; UNVERIFIED) |

**Haptics** (phones, within 3.6 m = DoorJoltRange, FrontRooms3DGame.cs:2677; all ESTIMATE).

| Moment | Haptic |
|---|---|
| Lean | Light 0.25 |
| Squeeze | ticks rising from 0.2 to 0.4 |
| Push latch | Light 0.45 |
| Barge | Heavy 1.0, then Medium 0.6 at +0.12 s |
| Quiet latch | Light 0.5 (as today) |
| Slam latch | Heavy 1.0 |
| Slam into a body | Heavy 0.8 |
| Locked bump | 2 × Rigid 0.8 (as today) |
| Slow try | Light 0.3 |
| Broken | Soft 0.6 (as today) |
| Blocked | Medium 0.5 |
| Blind bump | Light 0.3 |
| Each brace blow | Medium 0.5 |

Optional pad rumble: 80-150 ms for barge, slam, blocked and blows (Gamepad.SetMotorSpeeds, stopped on pause and on focus loss; iOS UNVERIFIED).

**HUD** (types and motion with 平面视觉; HUD motion grows out of an existing element, COMMON.md:399).

| What is aimed or armed | What shows |
|---|---|
| Unlocked latched door | the plain dot; E on it shows the 1.0 s hint and moves the dot |
| Locked door | padlock + "LOCKED · NEEDS …" (unchanged) |
| Key in hand | "E · UNLOCK" |
| Alarm door | "ALARMED" |
| Unlatched leaf in aim | nothing until the dot has rested 0.3 s on its latch half, then "STEP BACK · SHUT DOOR" or "STEP BACK · SLAM DOOR" (the motion named per device); E shows the pull hint |
| Broken door | B·4 |
| Vision lite | PEEK (not at a sprint) |

- No new HUD element is needed: the pull is the prompt line's text.
- The pull's lines join the +35% slot pass owed for fixed-width chips (`handoff/settings_quality.md`, the "Owed" line of §0).

**Teaching** (教程设计 and 叙事设计, no pop-ups).
- The legend gains a line such as "WALK INTO DOORS · RUN BARGES · STEP BACK TO SHUT" (wording theirs). With no door button (1C), this teaching is the only way players learn the pull.
- The placard's "CLOSE DOORS BEHIND YOU." stays.
- The start door (6B) is the first body door of every run, so the open is learned before floor 1.
- Floor 1's station, with no hunter before 150 cells or 3 min (A40), is where players first squeeze, barge and pull in safety.
- The hint at a latched door teaches anyone who presses E out of habit.

**What a door tells you.**
- A leaf that won't move means something is on the other side.
- A door left ajar means someone came through.
- A padlock on aim means locked.
- No prompt means it opens if you walk into it.

## 10. Code plan

Owners follow COMMON.md:380-385. All costs are ESTIMATE, in working days. Line numbers are as of 15:40 today and will drift: MapWorld and Game.cs changed four times today. Re-grep every one at build time.

| # | Owner | File(s) | Change | Days |
|---|---|---|---|---|
| 1 | 关卡设计 | new `FrontRoomsMap/FrontRoomsMapWorld.BodyDoors.cs` (+ hand-written .meta); `partial` on the class at FrontRoomsMapWorld.cs:21 (sealed, not partial today) | `PushDoorsByBody(in BodyDoorInput) → BodyDoorResult` (1.10). Does: candidate doors on the edges of CellOf(feet) and CellOf(feet + move), at most 8 doorByEdge lookups; the gate (1.1); the leans (1.2); the latch release (lever beat or latchDrawn burst); Follow; the body floor with lookahead (inverse LimitByBody, at most 40° a frame, at most 95.4°); the Through gate and open-leaf walls (1.6); episodes (1.8); bumps (§4); the alarm rule; DoorBodyBlocked; `blocked` for 6.10. Both rest modes sit behind one constant (Red: 2A, the crack). A door adapter (hinge, leaf collider, swing call, latch state, ready) lets non-map doors register: the start-door pair and the stairwell north door (6B, 4.10) | 2.5 |
| 2 | 关卡设计 | FrontRoomsMapWorld.cs (Door :426, DoorMotion :492, DoorOpenSource :278, TickSwing blocked branch :5743-5761, BuildEdge :3160-3173, NextFloor :2132, ResetDoorForTools :6489, PushAjar :3585, SettleAjar :3608; per reader map where not listed in §1) | Door fields (lean clock, episode, angular velocity); Follow and its coast; the rest classification (Open / Ajar + `ajarAngles[edge]`); the slam ease-in under the flag; events DoorLatchBurst, DoorBodyBlocked, DoorSlammedInto, DoorThrowStoppedByPlayer (LimitByBodies reports which body stopped the leaf); SettleAjar only on a door the Relay itself pushed | 0.75 |
| 3 | 关卡设计 | FrontRoomsMapWorld.cs (Use :4764, Describe :4684, Kind :4722, AlarmDoorPrompt :6823) | `BodyDoors` and `HandOpens` (= `!BodyDoors`, 7B); with body doors on, Use never opens and never shuts a door (1C); `Pull(door, from, slam)` with Use's guards and no DoorPulled; Describe null and Kind None on a latched unlocked door; a pull kind on an unlatched leaf; ALARMED alone; the EXIT from X by body | 0.5 |
| 4 | 关卡设计 | FrontRooms3DGame.cs (UpdateMapPlay, before Move :2416) | Build the input struct (raw tilt, sinceSprint, view, last frame's PEEK door); call the solver; scale the wish by moveScale; clear sprinting on `blocked`; the skip list (1.0); the `bodyDoors && RelayV2Compiled` gate; the `-bodyDoors` test switch. Never pass glide, stepVelocity or fallSpeed | 0.5 |
| 5 | 关卡设计 | FrontRooms3DGame.cs (cell read :2454, UseDoor, UpdateAim :2924-3014, jolts :1041-1042) | Pass tracker by edge (the 2.0 s momentum for the pull's slam); no live close-up at a sprint; the 1.9 hints (open and pull); prompts; jolts (§9); `keyHud.NoKey` / `BrokenTry` for bumps on the aimed door. SHUT BEHIND and the buffered press are dropped (1C) | 0.6 |
| 6 | 关卡设计 | FrontRooms3DGame.cs (after UpdateAim) | The pull (2.4): 0.3 s rest on the latch half within 1.4 m, not AheadOfLeaf, a 40° step back held 0.12 s, slam on momentum at walk speed; its prompt and disarm. The only shut (1C) | 0.5 |
| 7 | 关卡设计 | FrontRoomsMapHunter.cs (always-compiled part) + one-line calls in FrontRoomsMapHunter.V3b.cs FollowAjarV2 :505 and the OPEN act | The held-door rule (8.5). A helper outside `#if` tracks how long a Relay leaf rests on the player; v2 calls it from FollowAjarV2 to begin a BREAK act at 0.6 s (BeginBreakV2, no licence) and hear the player; both builds turn DoorThrowStoppedByPlayer into Caught outside a sanctuary. V3b.cs and V4b.cs are entirely under `#if FRONTROOMS_RELAY_V2` (line 1 of each), so keep each `#if` call site to one line. Red: 3A | 0.75 |
| 8 | 关卡设计 | FrontRoomsShots/FrontRoomsShotTimings.cs | BodyDoor block, all ESTIMATE: Reach .355, HingeDeadZone .15, ConeShare .5, MinSpeed 1.0, MinTilt .2, Lookahead .12, LeanSlow .30, LeanWalk .12, LeanBlind .40, LeanPeek .30, LeanReset .10, BargeCarry .15, SqueezeMoveScale .8, CoastMaxDeg 5, CrackDeg 3, EpisodeSeconds .5, EpisodeMetres .5, ShutBehindSeconds 1.5, MomentumSeconds 2.0, PullReach 1.4, PullDwell .3, PullBackDeg 40, PullHold .12, AlarmHold .4, AlarmFacingDeg 60, AlarmStraightDeg 30, HeldBreakSeconds .6, SlamFrameDegPerSecond 630, HintSeconds 1.0, the jolts in §9, SlamStaminaSeconds 0 (系统设计's lever). Open and FastSwing unchanged | 0.1 |
| 9 | 关卡设计 | FrontRoomsMap/FrontRoomsMapWalker.cs (Move :126, per reader map) | Call the solver before its Move when BodyDoors is on, so the test scene and the Level Designer preview behave like the game | 0.1 |
| 10 | 关卡设计 | FrontRoomsMap/FrontRoomsMap.cs (Resolve :448-456) + FrontRoomsMapWorld.cs BuildEdge :3147 | Never build two doors whose hinges share a cell corner: flip one hinge end or keep one door. How often the generator makes the pair is UNVERIFIED | 0.25 |
| 11 | 关卡设计 | Editor/FrontRoomsMap/FrontRoomsMapInteractionTests.cs (16,025 lines; drives leaves with TickDoorsForTools :5634, no CharacterController) | New BodyDoors suite beside DoubleActing and DoorVerbs; existing suites untouched (HandOpens true). Leaning never unlatches; strafe, 25° slides and turning do nothing; the leans and their reset; PEEK still offered after a lean; squeeze follow + rest (both modes) + rebuild keeps the angle; walk = one Ease; sprint = Barge with no overlap frame and at most 40°/frame; Through only on a crossing; open leaves are walls; one DoorMoved per episode; locked rattle once per episode; silent slow try; broken, being-broken, key-held and sealed doors solid; alarm needs the 0.4 s facing push and never barges; the Pull slams with no DoorPulled and USE never shuts (1C); HandOpens false refuses hand opens; the unlock ends 10° Ajar and the first push raises the one EASE (11B); the corner row; held-door rows (far-side holds against OPEN, PUSH and BREAK in v1 and v2: BREAK begins by 0.6 s + slack, a stopped throw raises Caught, no stance wait beyond the break + 1 s). v2 rows run in a v2-define clone. ASCII only, no C# tuples (COMMON.md:405) | 1.4 |
| 12 | 关卡设计 | FrontRoomsMap/FrontRoomsLevelProfile.cs (:76-82) + FrontRooms3DGame.RelayV2.cs (comments only) | The `bodyDoors` flag, tooltip "Off: today's map and game"; no body-door logic in RelayV2.cs, which is entirely under `#if` (line 1) | 0.1 |
| 13 | 交互设计 | FrontRooms3DGame.Mobile.cs (handheld blocks: HandleMobileLookTap :312, UpdateMobileTouchPrompt :412, UseKindOf :479-495, DisplayPrompt :86) | UseKindOf: no open or shut kind on a door with body doors on (1C, 7B); AlarmDoor → Hidden; a tap on a leaf never opens or shuts it; the pull's line on the prompt; haptic subscriptions; desktop prompts name the pad's button and the pull's motion per device | 0.4 |
| 14 | 交互设计 | Input/FrontRoomsTouchControls.cs, FrontRoomsTouchControlsView.cs, FrontRoomsMobileHaptics.cs, FrontRoomsMobileInteractionEvents.cs, FrontRoomsKeyMapView.cs, FrontRoomsInput.cs | The stick's pull-back as the pull's step on touch (no new control); the haptic kinds (§9); key-map callouts (SLOW squeezes, SPRINT barges, STEP BACK shuts); optional pad rumble. No shut kinds or labels (1C) | 0.4 |
| 15 | 交互设计 | Input/FrontRoomsTouchPlaytestDriver.cs (door block :563-646, per reader map) + Editor/Input/FrontRoomsAdaptiveLayoutTests.cs (:425-426, per reader map) | Pick doors by Kind, not by "OPEN DOOR" (:569). HoldGait slow / walk / run into a leaf → Ease / Ease / Barge. The pull by stick pull-back → Ease, and within 2 s of a sprint pass → Slam. A USE tap on an open leaf does nothing. Locked and broken bumps. The start door by body (6B). The unlock's 10°, then a push (11B). **Play-mode no-stall row:** virtual pad and keyboard sprint into a latched door at targetFrameRate 60, 30 and 20; pass if flat speed stays at 5.3 m/s or more through the door line, no side contact with the leaf, exactly one DoorMoved; record frame time on frames where a push lands on a chunk build | 1.1 |
| 16 | 交互设计 | Documentation/INTERACTION_DESIGN.md (A12 rules 3, 6, 9, 11) + TOUCH_CONTROLS.md + research SOURCES.md | A "Body doors" section; A12 rule 3 for bodies (crossing only), rule 6 (no aimed shut, so no step aside for map doors), rule 11 (the body opens, the pull shuts, USE moves no door); the E row, haptics table and playtest list; SOURCES.md for 10_research.md (COMMON.md:392) | 0.5 |
| 17 | 游戏视觉 | Rendering/UI/FrontRoomsKeyHud.cs (AimOf :214-233) + Rendering/Doors/FrontRoomsDoorDressEvents.cs (:28-33) + FrontRoomsDoorRig.cs + FrontRoomsRoomStream.cs (:788-812) | AimOf: the AlarmDoor case; the pull's Shut and Slam states; the dot already shows for Kind None. DressEvents: the latch-burst beat on DoorLatchBurst (lever snap, strike rattle), the alarm bar's 0.4 s travel and spring-back, a render-only shudder after a barge, the unlock's 10° pop (11B). RoomStream's start door as a body door (6B, 4.10): both leaves registered with the solver's adapter, latched while the map streams with the strain cue, its look | 2.0 |
| 18 | 声音设计 | Audio/FrontRoomsDoorSound.cs + FrontRoomsSoundDirector.cs + Tools/audio/fmod_frontrooms.py + Editor/Audio/FrontRoomsFmodVerify.cs | §9's sounds and system fixes; bind EaseClick, FastSwing and SlamBang; LatchBurst, LatchStrain, thud and SlamInto; a body-push profile in the door scenario check; the title doors' wooden Foley on body verbs (6B); the unlock pop (11B); offline renders only | 1.75 |
| 19 | 系统设计 | Documentation/RELAY_PURSUIT_REDESIGN.md §10 (:612-686) + Documentation/system_design/systems.json (D1, D2, A19) | Rewrite the door verbs from "E while …" to contact gaits (SQUEEZE and PUSH = EASE, sprint contact = BARGE), the shut (the pull only, 1C; EASE or SLAM on 2.0 s crossing momentum), Ajar widening (2A; the unlock's 10°, 11B), the held-door rule (3A), silent slow tries (4A), the start and stairwell doors (6B); prices in time, turns and doors. No V5 noise or Attention number changes; the player's barge carry (0.15 s, 9A) and slam window (2.0 s) replace V5's 0.5 s grace | 0.5 |
| 20 | 叙事设计 + 平面视觉 | Assets/Resources/Language/ (9 tables) + the prompt constants (landed by 关卡设计) | The pull's lines ("STEP BACK · SHUT DOOR", "STEP BACK · SLAM DOOR", motion named per device), the 1.9 hints (open and pull), ALARMED alone, the legend line, the SLOW · UNHEARD rewording (5A); retire "E · OPEN DOOR" and "E · SHUT DOOR" for map doors; 平面视觉 grows the hint's dot motion from existing elements | 0.4 |
| 21 | 设置菜单与画质预设 | FrontRoomsSettingsMenu.cs (:300-301) + its language rows | GESTURES rows: "SLOW · UNHEARD" (:300, 5A) and "Tap USE, or the door itself, to open it." (:301) rewritten for walking into doors and the pull; no USE OPENS DOORS setting (7B); `language_tables.py check` (owner per `handoff/settings_quality.md`) | 0.15 |
| 22 | 教程设计 | placard / legend copy, tutorial notes | The legend line, the placard's door lesson with the pull (the only shut, 1C), floor 1's station as the safe school, the start door as the first body door (6B) | 0.35 |
| 23 | 关卡设计调研 | FrontRooms3DGame.Baseline.cs | `-autopilotBodyDoors` (bots walk into doors at their gait and shut by the pull) in a v2 clone; a **chase bot** that barges, stops, turns at mouse, pad and touch rates and pulls, against V2Chase 4.2-4.4 m/s and V2Break 2.5-1.9 s (FrontRoomsLevelProfile.cs:345-346); the key bots unlock, then push (11B); re-baseline | 1.25 |
| 24 | runner (Codex) | `W/workspace_repo/remote/runner/INBOX.md` cards (COMMON.md:40-47) | The MIT suites, the driver, the play-mode row and the bot rounds as cards: commands, the clone, pass criteria. Silent: `-audioCapture` plus `m_DisableAudio: 1` in the clone's AudioManager, or FrontRoomsCloneOnlySilence, because OnDoorMoved falls back to a Unity clip when FMOD is not ready (FrontRooms3DGame.cs:2152); the simulator's mute switch only. Feel checks stay for Red's playtest | 0.5 |
| 25 | tools (owner UNVERIFIED; 交互设计 made the 10-31 strict-parse change) | `~/FrontRoomsVisualWork/tools/roslyn_check.py` (flags :239) | A `--define` option, or a v2 reference clone, so the `#if FRONTROOMS_RELAY_V2` call sites compile before landing. No reference clone defines it today (proj_ref66 and proj_relayrig2 rsp: 0 matches). UNVERIFIED how V5 itself was compile-checked | 0.25 |
| 26 | 交互设计 + 关卡设计 | constants only | Tuning after the combined test (cones, leans, windows) on keyboard, pad and touch | 1.0 |
| 27 | 关卡设计 | FrontRoomsShots/FrontRoomsDoorShot.cs + the unlock open in FrontRoomsMapWorld.cs (ReleaseKeyTurn :4903-4908) + the DoorShotTests suite | 11B: the unlock ends with the latch drawn and the leaf 10° Ajar, raising no DoorMoved; the first body push raises the one EASE (BARGE at a sprint); DoorShotTests' end-state rows change | 0.5 |
| 28 | 关卡设计 | FrontRooms3DGame.cs (the stairwell north door, :1902-1920) | 6B: the north door registers with the solver's adapter; latched until the floor is ready or StartDoorHoldLimit, with the strain cue; the 4 m shut stays scripted | 0.5 |

**Landing checklist.**
- One package: items 1-16 + 20 + 24 + 27-28, so the solver, its MIT suite and the driver rewrite land together and dark. Item 17's RoomStream part can follow, because the start door keeps today's opening until it registers.
- Re-grep every line number. Hand-write the .meta for the new partial file and make the class `partial`.
- ASCII-only editor scripts with no tuples. LF line endings.
- CLAIM and LANDED lines in LANDINGS.md.
- Roslyn macOS + iOS with 0 errors, using a real argument array (COMMON.md:480-486). Then the v2 compile (item 25) for the `#if` call sites.

## 11. Cost

ESTIMATE, working days. Red's picks (§00) are re-costed row by row in §10. The 6B and 11B rows (1, 15, 17, 18, 27, 28) are 交互设计's estimates; their owners should re-estimate them.

| Set | Totals |
|---|---|
| Red's picks: 1C, 2A, 3A, 4A, 5A, 6B, 7B, 9A, 10A, 11B | about 19.6 d across lines: 关卡设计 9.05, 交互设计 2.4, 游戏视觉 2.0, 声音设计 1.75, 系统设计 0.5, strings 0.4, 设置 0.15, 教程设计 0.35, 关卡设计调研 1.25, runner 0.5, tools 0.25, tuning 1.0 |
| For the record: the recommended set (1B with 2a) | about 16.8 d |

Against the recommended set, 6B adds about 2.5 d (the adapter, RoomStream's pair, the north door, Foley and driver rows), 11B about 0.85 d (the shot's end, DoorShotTests and the key bots), and 1C with 7B saves about 0.5 d (no SHUT BEHIND and no setting).

**Critical path:** 关卡设计's 9.05 d, then the combined test, the bot rounds and tuning.

**Calendar** (ESTIMATE): the build starts Nov 3 17:00 at the earliest (THROTTLE). With the line free, the code lands around Nov 13-15. Body doors go live after the combined test, the bot rounds and Red's v2 flip, so mid to late November at the earliest.

## 12. Risks

1. **No door button (Red: 1C).** The pull is the only shut, and no game in the research ships it (`10_research.md` §4, §6); TLOU2 dropped every manual close scheme it tried (§3.5).
   - Players who never find the pull never shut a door. The legend, the placard, the start door and floor 1's station must teach it (§9).
   - An accidental pull needs 0.3 s with the dot on the leaf plus a step back, and the prompt announces it. Backing away while watching the doorway does nothing (2.8).
   - A USE SHUTS DOORS assist is not built (open item O1).
2. **Chase parity (1C).** A mouse slams about 0.9 s after the stop, touch after 1.6-2.0 s and a pad after 1.9-2.5 s (3.4, ESTIMATE). Red accepted it; the chase bot measures it per device before switch-on (8).
3. **Stale leaf pose and stutter.** Whether CharacterController.Move sees an unsynced leaf pose is UNVERIFIED, and so is whether the lookahead alone prevents stutter. The play-mode row (item 15) is the only real check: MIT has no CharacterController. SyncTransforms cost on iOS and WebGL is UNVERIFIED.
4. **Below about 30 fps,** one-frame hitches near the hinge. WebGL may see them.
5. **Ajar's meaning widens (2A, 11B).** A crack the player leaves, or an unlocked door at 10°, reads like the Relay's trace.
6. **V2-only code has no default compile gate.** V3b.cs and V4b.cs are whole-file `#if`, and no reference clone defines the symbol. The call sites stay one line each until item 25 exists.
7. **Body doors go live only with v2.** Red cannot feel them in a shipping build before the v2 flip: only in a `-bodyDoors` test build or a v2 clone.
8. **The cone and timing numbers are ESTIMATE.** They need the combined test on keyboard, pad and touch.
9. **The held-door catch is a new way to be caught (3A).** The blows and the felt beat must make it legible; it needs a playtest.
10. **Teaching.** There is no OPEN prompt and no SHUT press; the hints and the first doors carry both.
11. **Concurrent edits.** MapWorld (7,103 lines) and Game.cs (7,813 lines) changed at 12:43, 12:51, 13:05 and 13:24 today, and landing needs 3-way rebases.
12. **The start door while the map streams (6B, O2).** If the hold is long on a phone, the first door of the run feels stuck. How long it lasts on iPhone and iPad is UNVERIFIED (StartDoorHoldLimit, FrontRooms3DGame.cs:1907); the strain cue must carry it.
13. **11B changes the unlock's tests.** DoorShotTests' end state and the key bots' open step change (items 23 and 27); until they do, those suites fail with body doors on.
14. **Sound content.** The Swing bed waits on the Freesound 683823 download, so squeezes sound thin until then.
15. **The kit leaf can sit up to 53 mm off the collider** at wide angles (FrontRoomsDoorRig, per reader map), so contact can look a few cm off.
16. **Accessibility.** Players who struggle to steer into doors lose today's press-to-open from 2.4 m, and Red declined the assist (7B). Steering into a door is required on every device.
17. **Barging into a door with the Relay right behind it** stops the leaf on the Relay. Whether the catch test sees through that leaf is UNVERIFIED (the "Red-literal" design's risk 8).
18. **Rule 24.** Body-door test runs move many doors, and the Unity fallback clip plays when FMOD is not ready (FrontRooms3DGame.cs:2152). Clones must be silent.

## 13. Red fit

**What Red asked** (10-31):
- Speed sets the verb: a sprint that never stopped bashes the door open, and a slow, rubbing push squeezes it open.
- A slam when the player turns back.
- No button to open; every verb the result of the body meeting the door's collider.
- Research what AAA games do, and think about what Red really wants.

**What lands, verb by verb.**
- **撞开 (bash open):** hold RUN into a door, never stopping, and it bursts open with no button and no slowdown. Speed is the verb, as in MGS3 [S], MW2019 [V] and TLOU2 [V] (`10_research.md` §3.1).
- **慢慢推开 / 蹭门 / 挤开 (push slowly, rub, squeeze):** in SLOW the latch holds for a felt 0.30 s, gives with a soft click, and then the leaf moves only as far as you push it. Stop and it stops. It costs Red's own V5 figure, 6 m.
- **甩上 (slam):** the hard 27 m slam is kept, and both its act and its force come from the body (Red: 1C). Turn back, put the dot on the leaf and step back: within 2 s of sprinting through that door, with the step at walk speed or more, it slams; otherwise it is quiet.

**Departures from the letter, argued.**
- **A body can only push.** After you pass a door, the face you pushed points at the opening, so walking back into it pushes it further open. A shut by movement has to be a pull (the step back) or a look (the look-back).
- **No auto-slam on a sprint pass.** It would be a closer (A12 rule 5), and it would charge 27 m to every sprint through a door.
- **No press (Red: 1C).** The recommendation kept a press so the chase slam stayed equal on a pad turning at 160°/s. Red chose the body alone. The rules stay the same on every device; the turn time differs (7.4).
- **The look-back (1D) is not recommended.**
  - It turns the camera into a door control: a horror player's reflex glance back would shut or slam doors.
  - About ten hidden gates decide it, and a mouse turns 5-10 times faster than a pad.
  - No game in the research does it.
- Red also said that closing and slamming may differ ("关门和甩门不一定是").

**What Red really wants (our reading).**
- Doors are part of movement, not a menu, so the noise you make is the honest echo of how you moved.
- The chase is punctuated by the burst and the bang; the sneak holds its breath on the creeping leaf and the crack.
- Doors stay heavy and truthful: they resist, they stay where you leave them, and a leaf that won't move means something is behind it.

**Red's decisions** (10-31, §00; the ask, with details and costs, is `30_for_red_zh.md`):

| # | Decision | Recommended | Red |
|---|---|---|---|
| 1 | Shut model | B press + pull | **C pull only** |
| 2 | A squeeze stopped short | a stays where left | **A** |
| 3 | A door held against the Relay | a break at 0.6 s + a catch | **A** |
| 4 | Slow tries on locked doors | silent | **A** |
| 5 | The hint "SLOW · UNHEARD" | reworded | **A** |
| 6 | Start and stairwell doors | a 1 m walking-at-it opening | **B real body doors** |
| 7 | "USE OPENS DOORS" setting | yes, default off | **B no** |
| 8 | Switch-on | with Relay v2, after the combined test and two bot rounds | **as recommended** |
| 9 | Barge cone and release carry | 60° and 0.15 s | **A** |
| 10 | Alarm doors | facing 0.4 s push only | **A** |
| 11 | End of the unlock | unchanged | **B a 10° pop, then the body** |
| (opt.) | Watch the 11-03/04 testers | yes | unanswered (not approved) |

## 14. Critiques considered

These fixes were made before Red's decisions. Where one names SHUT BEHIND, the aimed shut or the 1 m start-door rule, Red's 1C or 6B has since replaced that path (§00).

Each critique below was fixed as asked, unless the line says how it was changed or why it was not taken.

**critic:break-it**
1. **Blocker: no draft.** Fixed by synthesizing here. Upstream stages were not re-run, because the quota is reserved for the combined test (COMMON.md:31-35).
2. **Blocker: holding a door is still a lock.** Fixed by 8.5: a BREAK act at 0.6 s, and a catch when a throw is stopped by the player's body.
   - Changed: the critic asked for 0.3 s. We use 0.6 s for an OPEN or PUSH so the brace is a felt beat. A stopped throw is caught at once; the break's 1.9-2.5 s of blows before it is the warning.
   - Not taken: the shove. The pull-step mover is cancelled under sight and in a chase (FrontRooms3DGame.cs:2377, :2689), and a new mover would move the player without input.
3. **Major: v1 noise, and live at landing.** Fixed by 1.0 and 1.11: a profile flag, honoured only with v2 compiled in, with the MIT suite and the driver rewrite in the same package.
4. **Major: E at the next door slams the one behind.** Fixed by 2.3: leaf ownership, the behind-view test, the hint, and a prompt only when E will act.
5. **Major: a touch look stroke slams the door.** Fixed by 2.3: SHUT BEHIND fires on release, as a tap.
6. **Major: PEEK freeze mid-chase.** Fixed by 6.6.
   - Changed: it is stricter than "the shut wins". This critique's fix and the "E at the next door" fix collide on the lite of a latched door. There nothing fires: no PEEK and no slam, only the hint. The prompt line shows beforehand whether E will act.
7. **Major: walking along a wall flips doors.** Fixed by 1.6: a Through only on a crossing.
   - Changed: the critic proposed that a leaf at 70° or more pushed from the pocket follows the body silently. We made open leaves at rest walls, as today. That adds no silent moves and no new partial rests, and the corner box is fixed in the generator instead (item 10).
8. **Major: SHUT BEHIND is a free escape.** Priced in time by 3.4: the swing must end first, the slam takes 0.3 s, a Relay at the arc spends the slam, and the door is lost. The chase bot gates the switch-on, and a stamina lever is left to 系统设计.
   - Not taken: "SHUT BEHIND only eases". It would remove Red's slam behind you and save only 0.25 s (0.55 s against 0.3 s).
9. **Major: no partial rests breaks "stop and it stops".** Put to Red as decision 2, recommending the crack. The walk push and the barge still run to 95° (the critic's own fallback).
10. **Major: angled sprints.** Fixed: any sprint contact within 60° is a BARGE, behind the span gate.
    - Changed: a 45° press inside the span still opens. cos 45° = 0.71 is above 0.5; it is a real push and is priced as one.
11. **Minor: the 0.65 s window at a sprint.** Fixed by measuring it in time only. We dropped the claim that a pad can turn and aim a slam, and wrote the stop-first rule in 7.6.
12. **Minor: a barge trips an alarm door.** Fixed in §4.6.
13. **Minor: the EXIT is one-way from X.** Fixed: a body push from X opens it into N (A12).
14. **Minor: accidental opens.** Fixed by the 0.30 s slow lean with reset and the 0.2 tilt floor.
    - BUTTON mode: the code already needs tilt 0.5 to sprint (FrontRooms3DGame.cs:2387). UNVERIFIED that this is the 36 pt ring.
15. **Minor: holding RUN into a stuck door.** Fixed by 6.10.
16. **Minor: the first doors teach the wrong rule.**
    - Taken: the 1 m walking-at-it opening (decision 6a) and the bump on doors that do not move.
    - Not taken: moving the streaming hold to a later door. It changes Red's title handoff and is 游戏视觉's file.
17. **Minor: two doors at one corner.** Fixed by the generator rule and an MIT row.
18. **Minor: the yank shuts while you back away.** Fixed by the 0.3 s rest on the leaf and the prompt.
    - Not taken: "never let it slam". The turn-back slam is Red's ask. It slams only at walk speed within 2.0 s, and says so first.
19. **Minor: a slam into the Relay is silent.** Fixed by DoorSlammedInto (3.5).

**critic:complete**
1. **Blocker: no draft.** As above. The final stage synthesized directly from journal entries 15-17 and 21-23.
2. **Major: live at landing, and a define cannot be gated.** Fixed by the runtime flag; no new define. We also found that the v2 hunters are whole-file `#if`, so call sites stay one line and item 25 is added.
3. **Major: the v1/v2 swing-lock and noise matrix.** Fixed by 1.11 and 2.6. The body path runs its own buffered-shut guard, and no logic goes in RelayV2.cs.
4. **Major: the held-door fix cannot use the pull-step mover.** Fixed: no mover (8.5). The new MIT rows replace the claim that RelayRipsHeldLeaf covers it (MIT:2238).
5. **Major: no test can prove "never slows".** Fixed by the play-mode row (item 15), written as a runner card. SyncTransforms becomes measured and optional (6.15).
6. **Major: PEEK and habit-E collide with SHUT BEHIND.** Fixed: see break-it 4 and 6. The window is also dropped on any close-up.
7. **Major: partial rests against A12 rule 5.** Put to Red with both options and their costs (decision 2), not adopted silently.
8. **Major: schedule and quota.** Fixed in the header, §11 and `30_for_red_zh.md`: start Nov 3 17:00, the playtest on E doors, re-costed line by line.
   - Changed: our total is about 16.8 d against the critic's 12-15 d, because the bot round, tuning and the pull are now costed.
   - The optional playtest observation is offered to Red.
9. **Major: Red is asked too little.** Fixed: all 11 decisions plus the optional one, each with background, feel, cost and a recommendation.
10. **Minors.**
    - Owners and files added: 设置菜单 for SettingsMenu.cs:300-301; 教程设计; the runner; 关卡设计调研; the docs. The door Foley and audio contract docs belong to 声音设计 under item 18.
    - The API structs (1.10).
    - The economy prices (3.4, 8.6).
    - Silent nudges: removed entirely, since open leaves are walls and ajar leaves always cost an EASE.
    - The window (2.3).
    - HUD and parity gaps (§9, 7.8).
    - Rule 24 and the runner (item 24).
    - The housekeeping checklist (§10).
    - The latent AlarmDoor cases (§4.6).
