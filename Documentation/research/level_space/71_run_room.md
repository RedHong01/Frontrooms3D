# Level space · 71 · The Run! room: order, length, the receding end, the red light, what it means

Date: 2026-11-03. Line: 夸张结构调研与设计 (`level_space`), login B, session `local_42b400e5`, continuing login G's session `local_012d68f4` (stopped by G's 5-hour limit at 19:23). Stage: design, workflow `levelspace_continue2` (`wf_34ae5778-063`): the one writer of this page and of the `SOURCES.md` rows from LS201. Then checked the same night by an adversarial critic (login B to its limit, then login G in `run-room-71-finish`), which corrected claims and numbers in place; every finding and edit is logged in `~/FrontRoomsVisualWork/level_space/drafts/run/critic71.txt`. A coverage check against Red's two messages followed; its findings were applied in place (log: `~/FrontRoomsVisualWork/level_space/drafts/run/coverage71.txt`). Red answered §8 on 11-03 at 23:18 ("全部按推荐"); a stage of login G's session `local_012d68f4` recorded the answer (§0.1; marks in §0 and §8; final cards in §9; SP7 agreed in §10) and brought the page's status notes up to date (log: `~/FrontRoomsVisualWork/level_space/drafts/run/apply_decisions.txt`). On 11-04 a cross-check stage of the same run pointed §0.1, §9 and §10 at `70` §9, where SP7 now stands, and corrected one owner note in §9 (same log).

**Inputs.** The six research drafts of the Run! rethink, all marked DONE, in `~/FrontRoomsVisualWork/level_space/drafts/run/`: `today.txt` (the room as built, with paths), `pacing.txt` (order and length), `endless.txt` (the unreachable end), `motion.txt` (the model transition), `red_light.txt` (the red atmosphere), `story.txt` (meaning, entry, reward). Each section below names the draft section it rests on; every game or film is tied to a source row in §11.

**What this page is.** A proposal, decided by Red on 11-03 (§0.1). This line proposes; 关卡设计 (map, generator, lengths), 游戏视觉 with 灯具适配 (look, lamps, model animation), 叙事设计 (meaning, strings), 系统设计 (gates, reward, fairness) and 声音设计 (events) decide how and build, through 总控 (RULES rule 3), from the cards in §9. Nothing here changes game code.

**Not done.** No code, no Unity, no clone, no download, no git, nothing played aloud (rule 24). Nothing was rendered: every number about light, time and distance is arithmetic from the code and the drafts, to be measured in the one combined test (rule 25) and walked in scene 4 (rule 5b). The web sources were read by this workflow's research stages on logins G and B; this writer did not fetch them again (§11 gives each row's mark). Inference is marked as inference.

**Reused, not redone.** `room_visuals/03_run_research.md` (10-03), `10_run_directions.md`, `20_run_red_ward.md`, `21_run_fix_1014.md`, `40_for_red.md` (B picked 10-21), `50_alarm_zone_tell.md`, `run_b_production/10_plan.md`, `20_build.md`, `60_production_66.md` (Run! B as built); this folder's `10` §6 (RN-1 to RN-5), `20` V6 and V7, `40` §7.3 and §11, `63` §3.1 and §3.6, `70` §7 (SP5). `progress.py docs find run room` lists the Run! pages above; none covers order, length, a receding end or what the room means, and `docs find run room receding pacing` found nothing, so this is a new page with the name the brief gave it. It does not replace them.

**Reading map** (the page is about 195 KB; read it by section with `sed -n`): §0 line 15 · §0.1 (Red's decisions) 27 · §1 52 · §2 81 · §3 119 · §4 228 · §5 334 · §6 424 · §7 514 · §8 (Chinese, for Red) 592 · §9 759 · §10 815 · §11 856 · §12 920.

## 0. Short answer

1. **Run! today** is built, landed and switched off (`runRooms` false): one fixed straight of 9 cells (27 m), placed by a dice roll per zone from tier 2, cool white while armed, warm battery pools once tripped. No player has walked it. The lamp chase Red likes is most likely its RESET re-strike (§2.5; Red confirms in §8, decision 7). **Decided 11-03: the RESET segment** (decision 7).
2. **Length grows with depth, in whole cells:** 7 (the run's first Run!), 9 (today's), 12 (late), 14 (deepest, where it fits). Time is the reason: a full sprint bar is 5 s = 27.5 m, so early rooms are crossed on one breath and late rooms end the breath in sight of the door. **The cap is 14 cells (42 m)**, where the planner's chunk rule, the 46 m far plane and the Relay's earliest arrival meet. It cannot be infinite as a corridor; it can *feel* endless for seconds (§3). **Decided 11-03** (decision 3).
3. **Order:** of this line's five levels (Level 0, Office, Run!, Pool, the well) the game has two, Level 0 and Office, as the zone kinds of every floor; in the game Run! is a room inside them, and where Pool and the well come in a run is open (§3.4; Red, §8 decision 1). Within a floor the tier keys set pieces and the floor keys structure. First Run! at tier 2 (7 cells, plain); the Run! ramp from tier 3 (1:8, at most 24 m and 3.0 m down, never a storey); a door entrance from tier 3; long and receding rooms from tier 4 and floor B2. At most one Run! a floor, dealt from a deck that holds "nothing", with a calm zone beyond its door (§3). **Decided 11-03** (decisions 1, 2, 8b; no changed door in T5's deck, since 8c is reading one; where Pool and the well come in a run stays open).
4. **The receding end, recommended: THE STRETCH (§4).** The map plans the room at 12 or 14 cells; the end stands at 27 m. Crossing a third of that first length (9 m, at the first fire-door frame) trips the alarm, and the end backs away 9 or 15 m: a two-cell flinch, steps, a heavy final seat, and a 6 m/s glide whenever a runner closes in. A walker sees the gap grow only in the flinch; one 6 m/s glide would flee from every player but hide T2's re-stack, so Red picks (§8 decision 4). From the trip a full bar still reaches a 12-cell door. Then the door is real, it stays where it stopped, and at the reset the white re-strike walks the whole length. Nothing in it reads the Relay. **Decided 11-03:** the stretch, stepped, tripping at 9 m (decision 4); 系统设计's chase-dive bot round comes first (§4.7).
5. **The model (§5):** a T1 后退 head (the whole end rolls back on an overhead track) on every tier, carrying a T2 重组 body on desktop and iPad (the end wall comes apart and re-stacks around the door, the door break's own method). T3 伸缩 (telescoping sleeves) is the deep-floor alternative; T4 折叠 (accordion fire doors) waits for a pattern-safety capture. **Decided 11-03** (decision 5).
6. **Red (§6):** one red-coated tube in each R-B strip, on the battery circuit; never an EXIT sign, never a grade. **Look B, ALARM PULSE, recommended:** the trip runs a dark band to the door and leaves the corridor crimson; then a slow dark pulse runs to the door every 2.5 s, 12 at most. Fallback A (the same without the pulse); C (RED TIDE) is the narrative alternative. The white RESET re-strike stays as built. Inside WCAG 2.x by the numbers; the Harding FPA or PEAT capture decides. **Decided 11-03:** look B, A as the fallback, C in scene 4 only (decision 6); **the red tube itself is not yet agreed to be built:** Red first sees one still of the armed strip with it and says yes or no (§9, 灯具适配 1).
7. **Meaning (§7):** Run! is a fire compartment already on the alarm, not a punishment room. Room one's "WALK, DO NOT RUN." fails here; its "CLOSE DOORS BEHIND YOU." saves you. The prize already exists in the director: shut the goal door before the Relay sees or hears you, it sweeps an empty compartment and withdraws, and the floor goes calm. Red's deceiving door works as an honest door that fools a habit, never as a forced trap. **Decided 11-03** (decision 8: 8a-8e as recommended; the deceiving door is reading one, D1).
8. **For Red:** nine decisions in Chinese in §8 ("全部按推荐" answers all but the red tube, which waits for Red to see one still). A scene-4 district, SP7 (§10), lets Red walk the lengths, the receding end, the bodies and the red looks. **Answered 11-03 at 23:18: "全部按推荐"** (§0.1); SP7 is agreed for scene 4 (decision 9).
9. **Flags:** the red tube changes the R-B strip Red approved on 11-03 ("就这样"), so it still waits for Red's yes on one still; the receding variant moves its trip from 1.5 m to 9 m (agreed 11-03); diving into a receding room while chased costs about 2-5 m of lead (to be measured with bots); `70` SP5 carried three conflicts with Red's calls (EXIT signs, the retired RN-X goal door, wraps instead of R-B strips), fixed by this line on 11-03 22:5x (§10.1).

### 0.1 Red's decisions, 2026-11-03

Red's answer to §8, verbatim (a plain user turn in this line's chat, 2026-11-03 at 23:18 PST; transcript `7da6f3fe`, 07:18:27Z on 11-04, checked under rule 15):

> 全部按推荐

§8's own reading rule says what that means: every recommendation is agreed, including the two changes to earlier calls (decision 4: the receding room trips at the 9 m frame; decision 8c: reading one, the Run door entrance); decision 7 reads as the RESET segment; decision 6's third red tube waits for one still. Where §2-§7 say Red picks, decides or confirms, the matching line below is the answer; their reasoning stands as written. Under rule 5b a new structure (the receding end, the Run door entrance, the red looks) is built inert and enters the game only after Red has walked it in scene 4 and said so.

- **1 · Order:** a deck per floor by §3.4's tier table: at most one Run! a floor, "nothing" in every deck, a plain Run! before any twist, a calm zone beyond the goal door; no changed door in T5's deck (8c). Still open: where Pool and the well come in a run (关卡设计 proposes, Red picks).
- **2 · Ramps:** public 1:12 ramps first, in quiet zones; the Run! ramp from T3, 1:8, at most 24 m and 3.0 m down, never a storey; it waits for per-cell levels.
- **3 · Length:** 7 cells for each run's first Run!, then 9, 12 from T4, 14 at T5 where it fits; cap 14 cells (42 m); by depth, never by performance.
- **4 · The receding end:** M1, the stretch, deep floors only (9 → 12 from T4 on B2 or deeper; 9 → 14 at T5 on B3 or deeper), stepped (a two-cell flinch, one-cell steps, a heavy final seat, the 6 m/s keep-away glide), not the single 6 m/s glide. **Agreed change to an earlier call:** it trips at the 9 m frame (FD1), not 1.5 m in. 系统设计's bot round of chase dives comes first (§4.7 (c)).
- **5 · The model:** the T1 后退 head on every tier and platform, carrying T2 重组 on desktop and iPad (phones: half-course strips; WebGL: T1 alone); T3 伸缩 the deep-floor alternative, not built now; T4 折叠 held for a pattern-safety capture.
- **6 · Red:** look B, ALARM PULSE; A (no pulse) if B's Harding FPA or PEAT capture fails; C in scene 4 only. **Agreed 11-04, after the stills (below):** the dark oxblood third tube in the R-B strip Red approved on 11-03 ("就这样"), every look's red source, at the brighter HDR level.
  - **Still delivered, 11-04:** 灯具适配's card 1 sheet `research/zone_lamps/sheets/zone_lamps_run_redtube.jpg` (06:00: R-B as built / + the red tube armed / under the alarm, eye 1.62 m and 0.35 m, cost row; clone-only mock-up, kit and rig in main unchanged). Red, 11-04 06:0x in the 灯具适配 chat (a plain user turn, checked under rule 15): "目前是有HDR的你可以思考一下更亮的红光"; its answer is `zone_lamps_run_redtube_hdr.jpg` (06:12: two HDR levels, REC BRIGHTER). **Answered 11-04:** Red, in 灯具适配's chat: "加，用更亮档" (a plain user turn, checked under rule 15 in this line's session), then "用这个点位吧" on the brightest column of the HDR sheet (as 灯具适配 records it, LANDINGS 11-04 06:33). So the third tube is built at the brighter level: 灯具适配 landed it in main at 06:33 (`ZL_STRIP_Red`, `FrontRoomsRunRig` `redTubeLinear` (3, 0, .012)); `70` SP7 shows it on arrival. Look B stays the look, A its fallback if B's Harding or PEAT capture fails.
- **7 · The lamp chase:** the RESET segment (17.9-22.5 s of the 10-31 film), the white light coming back cell by cell from the entrance; it stays as built.
- **8a · Meaning:** a fire compartment already on the alarm (EGRESS), not a punishment room; no new string.
- **8b · When:** after the first send, from T2 (about 150 new cells), a short plain Run! first; counted by new cells walked, never by doors opened.
- **8c · The deceiving door:** reading one (D1), an honest Run door (RN-F + F5, the tell at the door, the trip about 2.5 m in), from T3 after one plain Run!. **Agreed change to an earlier call:** Run! B today is entered by an arch. The changed door (D4) is not built.
- **8d · Entering by choice:** yes, when it pays (under pursuit, in a hurry, for the escape and calm); Run! stays optional, never forced. Nothing to build.
- **8e · Reward:** R2, escape and calm: shut the goal door before the Relay sees or hears you, it sweeps and withdraws, and the floor is calm (90 s at T2 down to 60 s at T5); watched through the lite (系统设计 checks rule 11); bots measure how often it works. R3, a rest room beyond the door, later and rare.
- **9 · Scene 4:** SP7 agreed, built per §10 (greybox, inert for the game); written into `70` as its §9 on 11-04, the spec scene 4 builds.

Not answered by "全部按推荐", since they asked for facts (none blocks a card): which Little Nightmares "隧道" Red meant (decision 4), which fan game's red (decision 6; Escape the Backrooms is this page's inference), how message 2 ends (decision 8).

## 1. Red's ask, and what was and was not done

Two messages, 2026-11-03 at about 18:35 and 18:37 local time, in this line's chat, by voice input. The research stages found both in transcript `7da6f3fe` (message 1 a plain user turn at 02:35:28Z; message 2 a `queued_command` attachment, origin human, at 02:37:14Z) (`today.txt` §0). The parent session checked at 19:4x that Red wrote nothing more there to the transcript's end at 19:26.

Dictation note: "国家设计" is the dictation of **关卡设计** (level design). The opening words "people then the 工作 will really" are dictation noise; this page offers no reading of them. Message 1 ends mid-phrase ("…3D 的模型动画的这种"); message 2 is cut off ("就比如说玩家进入到这个"), so the end of its last question is unknown. "ramp" is read two ways (§3.5).

Message 1, verbatim:

> people then the 工作 will really其实可以和国家设计一起去构思，就比如说，按照现在的国家设计它是怎样的，然后如果引入了这种，就比如说有些房间它就不像传统的一些房间那样子，但是我认为你们之间的融合应该是一个非常精密的那种融合，就比如说，不同类型的 level 它出现的一个次序，什么时候应该出现 ramp， 究竟有多长？比如说，如果要是早期玩家遇到这个 run 房间，它会不会短一点？然后在晚期遇到的房间，它会不会长一点？比如说 run 它是一条狭长的一个就是通道嘛，那它只有长边是这个 run 房间最多能够控制的这种变量，那它就是在长的时候它可以有多长？它可以无限长吗？然后以及像 run 这种房间，如果它是一个类似于永远到达不了的一个尽头，那有很多游戏，就比如说像《小小梦魇》那样的游戏，它就会做一些就是比如说像隧道啊，或者说永远到达不了的彼岸那样子的一个设计。我感觉很多恐怖游戏都做过。我希望你思考，比如说在 run 这个房间的情况下，当然这个也是要和游戏视觉一同协作，它怎么样可以通过，就比如说一个房间，如果玩家走到了它原本房间最初始长度的三分之一，哎，这个门它往后退了，它越退越远了，然后这个退呢，它肯定是有 transition， 它不只是 motion transition， 它也是模型上面，就比如说它有一些，像是我们之前有专门做过一个门的那种可破坏的这种地形嘛，那它是否有一个就比如说重组或者向后退避的这样子的一个3D 的模型动画的这种

Message 2, verbatim:

> 然后这个 run 的房间它是怎么，就比如说，就是现在目前我看很多你做的这个房间的颜色的这个灯光色调，它其实一个是普通状态下白色的色调，但是它失去了那种在后世同人的这个游戏当中的那种红色的非常就是庄严，但同时又非常危险的那种颜色的这种氛围。我认为你其实需要去进一步的去做调研，然后去。 优化这个房间它的氛围，但是我很喜欢，就是这个房间你刚刚之前做的那种，就是灯光，它会就是成隧道一样，成脉冲一样，就是去，就是相当于就是一个一个灯光熄灭，一个一个灯光在亮的这种效果，我觉得你做的很好。我觉得你可以思考一下它怎么样能够更配合上这个 narrative， 以及就是从叙事设计角度来说，这是一个什么样的一个信息？然后这个信息的话，对于玩家而言，比如说他在什么样的一个游戏的一个环境下，他会遇到 run 这个房间，会不会是玩家他打开的门的次数过多了，然后呢，他进入的区域过多，然后其中有一个区域他可能会被欺骗，他在打开门进入这个门之后，他发现连接上的并不是之前的正常的这种房间，而是 run 房间，因为现在我感觉很多时候玩家如果知道这个房间就是 run 房间的话，因为现在你还没有把它真正的合并到游戏的可玩这个关卡里嘛。 主要的这个 build 还是没有的。那当前玩家就是，我不确定他会不会主动选择去进到这个房间，以及这个房间能给他什么样的奖励？它是一个纯惩罚性的房间吗？就比如说玩家进入到这个

What Red asks, and where this page answers it:

| Red's question | Owners Red names | Here |
|---|---|---|
| The fusion with 关卡设计 must be precise ("你们之间的融合应该是一个非常精密的那种融合"), starting from the level design as it is ("按照现在的国家设计它是怎样的"; 国家设计 = 关卡设计) | 关卡设计 | §2 (the room and its planner as built), §3.6 (every rule as whole-cell integers the generator holds), §9 cards |
| The order in which level types appear; when a ramp appears and how long it is | 关卡设计 | §3.4, §3.5 |
| Shorter early, longer late; how long at most; "它可以无限长吗" | 关卡设计 | §3.2, §3.3 |
| The never-reached end ("永远到达不了的彼岸", Little Nightmares) | — | §4.1, §4.2 |
| The door backs away once the player reaches a third of the first length, "越退越远", with a 3D model animation (重组 or 向后退避), like the destructible door | 游戏视觉 | §4, §5 |
| Today's light reads white and has lost the fan games' red, "庄严…又非常危险"; research it and improve the mood | — | §6 |
| Keep the lamp chase ("一个一个灯光熄灭，一个一个灯光在亮") and tie it to the narrative | 叙事设计 | §6.2, §6.6 |
| What message Run! carries; when the player meets it (too many doors opened, too many zones, a door that deceives); will the player choose to enter; what it gives; is it pure punishment | 叙事设计 (系统设计, for the gates and the reward, is this page's reading: Red did not name it) | §7 |

Done: six research sweeps (real sources, read or marked), this synthesis, Chinese decisions for Red (§8), cards for the owners (§9), a scene-4 district (§10), SOURCES rows LS201-LS251 (§11). Not done: no build, no render, no measurement, no change to `70` (its SP5 conflicts are listed in §10.1 for this line's next step), no message to Red or to other lines (the parent session sends them).

## 2. What Run! is today (facts, with paths)

From `today.txt` §1-§9 (code and docs read 2026-11-03; paths from `Frontrooms3D/`, written FR3D). Every fact here is the code's or a landed doc's; nothing is proposed in this section.

**2.1 Live or not.** The map's Run! room ("Run! B · EMERGENCY POWER") is landed and inert. `FrontRoomsLevelProfile.runRooms` is the master switch, off by default (FR3D/Assets/Scripts/FrontRoomsMap/FrontRoomsLevelProfile.cs:49-55); the shipped profile `Assets/Levels/FrontRoomsLevel0.asset` does not serialise it, so it loads off. With it off "no FrontRoomsRunRooms exists at all" (FrontRoomsRunRooms.cs:10-11). It turns on only in tools (`-autopilotRunRooms`), the module preview and scene 3's interactables tour. 关卡设计 flips it after the combined test, which has not run (`handoff/visual.md`, `60_production_66.md` F.5, F.8). Red has seen Run! B only in the 28.5 s film `research/room_visuals/run_b_production/video/run_b_20261031.mp4`, the zone-lamp sheets and the 10-14 look rounds; this line showed Red no Run! picture before the ask (`today.txt` §4). The title stream's red "Ward" Run room (FrontRoomsRoomStream.cs:425-440, 1475-1516) is never reached in play (C40).

**2.2 The room.** One size: `FrontRoomsRunRooms.Length = 9` cells of 3 m, 27 m (FrontRoomsRunRooms.cs:171; `MapGrid.CellSize` 3, `ChunkCells` 8 = 24 m, FrontRoomsMap.cs:52-53). Clear width 2.84 m, Standard height 2.9 m, a 1.4 m arch at the entrance (`ArchWidth`, :173), the goal is the room's own door, RN-F with set A's 15 x 15 in F5 lite, never locked (C38). The trip: armed until the camera is 1.5 m in from either opening (`ArmDepth`, :175; the test at :131-135 allows both ends). Reset: under Relay v2 when the director's Away fires; v1 after 20 s of calm.

**2.3 Where and when.** A zone is decided once, when first met, by its stored tier (RunRooms.cs:140-161). Conditions: the run's first send has gone out; zone tier at least 2 (`minTier`); not the arrival zone; Standard height; Level 0, or Office (C39); a slot free in the one trigger budget shared with foiled windows and alarm doors; a roll under 1 / zonesPerDevice = 1/3, 1/2.5, 1/2, 1/1.75, 1/1.5 for T1-T5 (FrontRoomsLevelProfile.cs:128-141). The candidate: 9 Standard cells of the host's theme, 15+ cells from the start door, Relay entries at least 9 cells off, both ends off a chunk border so the room crosses exactly one border (RunRooms.cs:435-439), never the only way round. **Tier** = 1 + unique cells stood in / 150, never falls, invisible to the player (C04; FrontRoomsLevelProfile.cs:313-330: "nothing else raises it (no stall rise)"). The floor (LEVEL 0, B1, ...) is known to the game (`FloorChanged`) but the Run! planner only logs it. Nothing counts the doors the player opens (tools only).

**2.4 Light, state by state** (FR3D/Assets/Scripts/Rendering/FrontRoomsRunRig.cs; `today.txt` §3):

| State | What the player sees |
|---|---|
| Armed | Nine R-B bare two-tube strips with wire guards at z = 1.8 + 3k, cool white (.90, .95, 1.0); cell 5's tube stutters; red "AC ON" pilots on the battery units. Red: the sprinkler main, pull station, bell, extinguisher cabinet. No EXIT sign (C41). |
| Trip | The mains drop in 0.06 s, 0.35 s of black, then the battery heads rise in 0.18 s (stagger 0.004 s/m, so one change, not a sequence). |
| Tripped | Hard warm pools (1, .84, .64) every 6 m from T-A twin-heads and T-B remote heads; the goal head (gain 5) makes the goal door the brightest thing; the C42 light behind the goal wall lights the F5 lite. |
| Warn | The map's stage-1/2 burst: every lamp dips together, not in a chase. |
| Reset | Heads out in one change; one flick per strip rippling from the entrance at 0.025 s a cell; dark; then the **re-strike sweeps from 0.45 s at 0.15 s a cell** (a front at 20 m/s), each lamp a 0.40 s rise. About 2.4 s. Within 30 m of the viewer only. |

**2.5 The effect Red likes** ("一个一个灯光熄灭，一个一个灯光在亮") is most likely the RESET re-strike as Red saw it in the film (frames 538-675, 17.9-22.5 s): seen from the goal end, a line of light walks cell by cell down the tunnel toward the camera. The sprint through the tripped pools (one pool every 6 m at 5.5 m/s, the RUN segment 10.4-15.9 s) is the only other candidate; it was not settled (`today.txt` §3a) until Red's answer of 11-03, read as the RESET (§0.1, decision 7). In the fiction the reset is the restoral, the all-clear: the effect Red praises is today used for the moment the danger ends.

**2.6 Why it is white.** Red was in the concept from week 1 (Red's course slide, as quoted in `03_run_research.md` §1.2: "A red corridor. The hunter behind. Nothing to read."). A red direction, A · RED WARD, was built and measured: at the Relay's 2.05 m size it read 2.97 : 1 against a 4 : 1 bar, with 32 % of the frame saturated red and a +3.2 EV adaptation hack (`21_run_fix_1014.md`). Red picked B on 10-21 ("全部按推荐", `40_for_red.md`:3), whose rule was "Red stays on the EXIT signs and fire gear"; then C41 (10-31) removed every EXIT sign from Run! rooms, and with them the canon's red source. What red is left inside is small objects (`today.txt` §6), the only red light is the AC ON pilots, which go out at the trip, and the Run_B grade cools and desaturates the frame (saturation −10, white balance −6, `Resources/Rendering/FrontRoomsPost_Run_B.asset`; `red_light.txt` §1.1). The gap Red names is real.

**2.7 What it means today.** `system_design/systems.json` L7: "Stepping in calls the Relay, and the sprint to the door is the reward." Run! is the tier-2 trigger kind (RELAY_PURSUIT_REDESIGN.md §6, §12). Devices are "readable before you commit": from 2 m outside the player sees the ALARM ZONE card and the red-banded detector base; "the room arms 1.5 m inside"; "No route is ever a forced call" (RELAY :356-373). The fiction is EGRESS (Red's pick, 10-03): the Relay is the stage of the fire alarm that trips; ARMED reads as mains power, TRIP as the mains dropping, TRIPPED as emergency power, RESET as restoral (`research/relay_pursuit/30_narrative.md` §1-§2 for the frame; the light states read this way in Run! B's own direction, `room_visuals/10_run_directions.md` B and its state table). Room one's placard: "IN CASE OF FIRE: WALK, DO NOT RUN. CLOSE DOORS BEHIND YOU." Player text never says RUN or "relay" (`handoff/narrative.md`:48). There is no reward, no ordering, no early or late length and no deception: a rational player walks past, and the calm-route arrows add 30 m to avoid it (systems.json A54).

**2.8 Red's Run! decisions that bind** (verbatim where the files hold the words; `today.txt` §7, `handoff/level_space.md` §5):
- 10-21: Run! look = B Emergency Power ("全部按推荐").
- 10-31 00:31: C38-C43 ("全部按推荐"): own goal door, never locked; Office may host; the title-stream room stays Ward; **no EXIT sign anywhere in a Run! room** ("so EXIT stays the floor's one way down"); one rig-held light behind the goal wall.
- 10-31 ~15:2x: "run选择R-B" (armed: bare strips with wire guards); "这里应该是TA和TB相结合" (tripped: twin-heads plus remote heads). 11-03, on the as-built sheets: "就这样".
- 10-31 ~15:58: "全部保留，但是目前我感觉关于run 房间G1和G2不可取，因为它直接改变了整个房间的性质本身，看起来就不符合对于此关卡的描述了。" (a corridor stays a corridor), and Red picked round 2: "更深的楼层用更“不可能”的结构：错层、会移动的结构、埃舍尔式连接" (`handoff/archive/bathroom_history.md`, 10-31 ~15:58).
- 10-31 17:29: "办公室里/level 0还有level run里都不应该出现大型的螺旋滑梯".
- 10-30 11:29 (关卡设计 chat, via `story.txt` Part A): "我不希望在玩家onboarding的时候就被追逐，最开始的阶段应该是玩家开始熟悉游戏的阶段".
- 11-03 ~18:3x (module sizes, `40` §11.9): whole modules on one strict 3D grid, whole cells, levels in 0.30 m steps.
- Red's PT4 rule keeps Run! off in the playtest snapshot (as recorded by 关卡设计 and 系统设计; Red's own words not found, `today.txt` §7).

**2.9 This line's earlier Run! work.** `10` §6 RN-1 to RN-5 (the service spine; fire-door frames every 3 cells held open on magnets; compartments every 9 m; a bright release at the goal; a racetrack for later tiers), `20` V6 (the ramp, stills h1 and h2 kept) and V7 (the moving structure, "deep floors only"), `63` RN-SPINE (1 x 3 to 1 x 9 cells), `70` §7 SP5 (the 27 m corridor as a 1:8 ramp falling 3.0 m through fire-door walls FD1 at z 9 and FD2 at z 18 to a loading dock). SP5 as first written carried three conflicts with Red's calls; this line fixed them in `70` on 11-03 22:5x (§10.1).

## 3. Order and length, with 关卡设计

From `pacing.txt` A, C, D, E, F (numbers worked by hand from the code, not simulated) and `today.txt` §5. Proposals for 关卡设计 and 系统设计.

### 3.1 The clocks the game already has

| Clock | What it is | Source |
|---|---|---|
| Tier | 1 + unique cells stood in / 150, capped at 5, never falls, never shown. A quiet bot on seed 2554 reached T2 at 150 cells in 165 s (about 0.9 new cells a second); at that pace T3 comes near 5.5 min, T4 near 8 min, T5 near 11 min (extrapolated; real players are slower) | FrontRoomsLevelProfile.cs:313-330; RELAY :778 |
| Floor | LEVEL 0, B1, B2, ... down STAIR A; the tier carries down; the run ends only when caught (C22) | MAP_GENERATION.md:210-219 |
| First send | No trigger room before the Relay's first send (A40) | FrontRooms3DGame.RunRooms.cs:63-68 |
| Budget | One trigger slot per 3 / 2.5 / 2 / 1.75 / 1.5 zones by tier, shared by Run!, foiled windows and alarm doors | FrontRoomsLevelProfile.cs:128-141 |
| Depth already grows the floor | The start-to-EXIT walk is 14-18 cells on floor 1, 14-22 after: the one existing "deeper is longer" rule | MAP_GENERATION.md:212; FrontRoomsExitPlanner.cs:162-189 |

Red's "打开的门的次数过多…进入的区域过多" is close to what the tier already measures: Run! comes to the player who has walked into the most new ground (§7.2).

### 3.2 The length rule: early short, late long, capped

Specify the room in **seconds of the player's verbs**, not in metres (the rhythm-group method of Smith et al., [LS227]): the sprint bar is the beat. Sprint 5.5 m/s for a 5 s bar (27.5 m); walk 3.2 m/s; the bar refills after 1 s at 1 s per second, and a dry bar leaves the player winded until 1 s is back (FrontRooms3DGame.cs:403-410). Distances run from the trip line (1.5 m in) to the goal door's face (L − 0.08 m) (`pacing.txt` D1):

| Cells | Length | Trip to door | Best crossing | Walking | The bar at the door | Feel |
|---|---|---|---|---|---|---|
| 7 | 21 m | 19.4 m | 3.5 s | 6.1 s | 1.5 s left | one breath, kept |
| 9 (today) | 27 m | 25.4 m | 4.6 s | 7.9 s | 0.4 s left | one breath, just |
| 12 | 36 m | 34.4 m | 6.9 s | 10.8 s | dry 6.9 m short | the breath ends in sight of the door |
| 14 | 42 m | 40.4 m | 8.2 s | 12.6 s | dry 12.9 m short | two bursts with a forced rest |
| 15+ | 45 m+ | — | — | — | — | past the cap |

**Proposed table** (one per-room value in whole cells; today one constant, `FrontRoomsRunRooms.Length = 9`):
- the run's first Run! (tier 2): **7 cells**, the teaching room; it keeps its promise with a breath to spare;
- standard (tier 2 after the first, tier 3): **9 cells**, today's room;
- late (tier 4): **12 cells**, plain or receding 9 → 12;
- deepest (tier 5): **12, or 14 cells where 14 fits**; plain or receding 9 → 12 / 9 → 14.

Length follows depth only, never the player's performance. Resident Evil 4 adjusts a hidden difficulty scale from how the player is doing ([LS230]); this game's rule is the opposite, "the tiers make the Relay better, never the warnings shorter" (RELAY §12), and Left 4 Dead's director "adjusts pacing, not difficulty" ([LS224]).

**The cap: 14 cells, 42 m.** Three independent limits land on the same number (`pacing.txt` D2):
- (a) **Chunks.** The planner pins both ends off a chunk border, so the room crosses exactly one border: with the entrance at chunk-local e and the goal cell at x in the next chunk, L = x + 9 − e, with e and x in 1..6, so L runs 4 to 14. Axial positions per chunk pair: 7 cells 4, 9 cells 6, 11 cells 4, 12 cells 3, 13 cells 2, 14 cells 1. A 15th cell needs three chunks in a row that each hold a host-zone cell, rare with one zone site per chunk (FrontRoomsMap.cs:342-368); otherwise the room would write into a chunk already built.
- (b) **Sight.** The far plane is `buildRadius x 24 − 2` = 46 m (FrontRoomsMapWorld.cs:99-100), and the exponential-squared fog is only 34 % at 46 m (FrontRoomsLook.cs:21, :52-55), so the plane is a hard cut. A Run! must be readable from 2 m outside the entrance, far door included (`10_run_directions.md` §1.1): at 14 cells the door is 44 m from that point, at 15 cells 47 m, cut.
- (c) **Time.** Called from Away, the Relay's earliest arrival at the trip point is placement plus 2.0-3.0 s, 1.5 s of listening and a walk in at 2.6 m/s from at least 42 m of path: at least 19.7 s at T5 and 20.7 s at T1 (RELAY :311-337). A 14-cell room takes 12.6 s at a walk, so a player who keeps moving shuts the door unseen (a door shut unperceived is a wall to it, RELAY :323). A 22-cell room (20.1 s) loses that.

12 cells is the practical late length (three axial positions per chunk pair instead of one). Before anything promises 12 or 14, 关卡设计 measures the fit rate on 100 seeds with the existing `-autopilotRunRooms` check (`pacing.txt` F2).

**Precedents for "size grows by a table, inside a container":** The Binding of Isaac sizes a floor as random(2) + 5 + level x 2.6 rooms inside a fixed 9 x 8 grid, the grid itself the bound (the only stated cap is the Labyrinth curse's "max 45"; a plan with the wrong count is thrown away and retried) ([LS233]); Dead Cells sets each biome's length by hand in a concept graph ([LS226]). Ours: the table above, inside the chunk and the far plane.

### 3.3 "它可以无限长吗": not as a corridor; it can feel endless for seconds

Not as geometry: chunks, the far plane and the Relay's arrival stop a straight at 42 m (3.2). The design sources agree that the feeling is the promise of no end, broken by a real door; in a room of at most 42 m it is kept for seconds:
- Thomas Grip wanted Amnesia's chases to give the nightmare "when you are running and not going anywhere", and found chases lose force on repeat and need a clear route ([LS229]).
- The fan canon's Level ! is "ten kilometers" long only in its text; Escape the Backrooms built it as a scripted course of falling furniture and lights that ends at an exit door "right in front of you" (the guides give no length or time), placed just before the level called The End (11th of 22 in a 2023 guide); one fan game sells a literal ten-kilometre run as a novelty ([LS232]).
- Super Mario 64's endless stairs warp Mario back while he lacks 70 stars: endless until a condition the player already counts ([LS205]). The Exit 8 is endless until eight right calls, and the count is posted on the signs ([LS214]).

Three ways the room can feel endless (`pacing.txt` D3, `endless.txt` D6-D7):
1. **The receding end (recommended; §4).** The map holds the full 12 or 14 cells; the end stands at 27 m and backs away once. No chunk changes.
2. **A loop through its own goal door** (P.T.'s seam, [LS203]). Cheap in geometry, wrong in rules: the trip has sent the Relay to this very room, a second pass doubles the time inside and leads back to the arch it walks to, and the goal door stops being the escape verb, which changes what the room is (Red, 10-31, on G1/G2). Not for Run!.
3. **A streamed pocket outside the grid.** The title's room stream already recycles "five reusable room roots" behind a shut door (FrontRoomsRoomStream.cs:4-18): a service corridor of 9 m compartments could repeat for ever. It is a third streaming system the Relay cannot follow, with its own exit rule: a one-off for 叙事设计 and 系统设计 (for example a deep-floor descent in place of STAIR A), not a Run! variant.

### 3.4 Order: when Run!, ramps, drops and moving structure appear

Two keys: the **tier** for set pieces (as today) and the **floor** for structural vocabulary. Where both are named, a piece needs both, so a player who wanders to T4 on LEVEL 0 still meets no moving structure there: LEVEL 0 is where the honest building is learned. Red already chose "更深的楼层用更“不可能”的结构：错层、会移动的结构" (2.8). (`pacing.txt` E2, adjusted with `story.txt` E2 for the door entrance.)

**Red's "不同类型的 level".** This line's levels are Level 0, Office, Run!, Pool and the well (`40` §7). Today's game has two of them, as the two zone kinds the generator mixes on every floor (`ZoneTheme { Level0, Office }`, FrontRoomsMap.cs:16; floors LEVEL 0, B1, B2, ... down STAIR A, MAP_GENERATION.md "Level transitions" and "Floors"). In the game Run! is a room those zones host (2.3), not a floor of its own, and the Pool level and the well are not in the generator. In scene 4, `70` SP2 stacks four floors round the well (Level 0 on top, then an Office floor, a Run! service floor and the pool hall about 17 m down): a study for judging depth, not a run's order. Where the pool and the well come in a run is not settled here: 关卡设计 proposes and Red decides (§8 decision 1; Escape the Backrooms puts its Run level after the Poolrooms, just before The End, [LS232]). The table below orders what this page owns: the Run! variants and the vertical devices inside the Level 0 and Office zones of every floor.

| When | Run! (the floor's one Run!, dealt from the deck) | Ramps | Drops (V2) | Moving structure (V7) |
|---|---|---|---|---|
| T1, any floor (first ~3 min) | none (tier-2 kind, after the first send) | public 1:12 ramps of raised floors (≤ 0.76 m a run) | at most one terrain module per Level 0 zone | none |
| T2 (~3-5.5 min) | the run's first Run!: **7 cells, plain, arch-entered**; then 9 plain | as T1 | + sunken lounge, office pit (1.2 m) | none |
| T3 (~5.5-8 min) | deck {9 plain, 9 ramp, 9 door-entered, nothing} | + the Run! ramp (3.5) once the map has levels | + split suite (2.4 m, two ways up) | none |
| T4 (~8 min) | deck {12 plain, 9 ramp, 9 door-entered, **9 → 12 receding** (floor ≥ B2 only; else 12 plain), nothing} | as T3 | as T3 | the receding end |
| T5 (~11 min) | deck {12 or 14 plain, 9 ramp, 9 door-entered, **9 → 12 or 9 → 14 receding** (14 on floor ≥ B3, where it fits), the changed door if adopted (§7.2), nothing} | as T3 | as T3 | as T4 |

Rules of the deck (`pacing.txt` E3, F3-F5; `story.txt` E2, F9):
- **At most one Run! a floor.** Today's roll per zone can deal two in a row. Rarity is part of the design: chases lose force when repeated ([LS229]); long troughs and short peaks ([LS231]).
- **A deck, not a roll.** Left 4 Dead deals its boss events the same way: "Tank, Witch, and Nothing", shuffled, "Successive repeats are not allowed" ([LS224]). The floor's Run! takes the next card of its tier's deck when its host zone is decided (the decision order is already the player's path, RunRooms.cs:147-151); no variant twice in a row across floors; the receding end never on two floors in a row.
- **One twist per room.** Ramp, receding end, door entrance: never two in one room. The same room and the same rule, with one new issue per depth step, dealt at random: Bycer's reading of The Thing ([LS231]).
- **Plain first.** The run's first Run! is plain and arch-entered; no twist until one plain Run! has been met in this run (a run flag). A deception only reads against a learned baseline: The Exit 8's anomalies frighten because the corridor is known by heart ([LS214]).
- **A calm after it.** No other trigger kind in the Run!'s host zone or in the zone beyond its goal door, so the player who shuts the door gets the calm the rules promise; Left 4 Dead's director relaxes "for 30-45 seconds, or until Survivors have traveled far enough toward the next safe room" ([LS224]), and Alien: Isolation's director sends the alien away when its menace gauge peaks ([LS225]). A Run! is a peak the player chooses.
- **Teach in the trough, test in the peak.** Every vertical device first appears in a quiet zone (a public ramp, a pit) before the player meets it inside a Run!, so the first Run! ramp is a known shape used under pressure.

Why this order, by source: size grows by a table inside a container ([LS233], [LS226]); one new issue per step ([LS231], [LS224]); chases rare and short, ended by a door that opens ([LS229], [LS213]); Run! as a late peak right before a release, as Escape the Backrooms places it between the Poolrooms and The End ([LS232]); the moving structure for deep floors and for a player who has learned that this building tells the truth (`20` V7).

### 3.5 Ramps: when, and how long

Red's "什么时候应该出现 ramp， 究竟有多长" has two readings (`today.txt` §9): (a) this line's V6 ramp, the Run! variant that falls 1:8 (stills h1 and h2, kept by Red 10-31; SP5 in scene 4), or (b) a dictation slip for "run". Both are answered: (b) by 3.2, (a) here.
- **Public ramps first** (T1 on, quiet zones): 1:12, at most 0.76 m of rise a run (9.1 m long), landings 1.52 m (`40` §5.1; [LS47]).
- **The Run! ramp** (T3 on, once the map holds per-cell floor levels, `40` §11.7): 1:8, **at most 24 m and 3.0 m down** (10 level steps of 0.30 m, ports at 0 and −10 steps), whatever the corridor's length; extra cells stay level. **Never a storey** (3.9 m): STAIR A stays each floor's one way down ("Every exit leads further in, now literally down", `narrative/40_floors_exit_tutorial_strings.md`:26; C41) (`pacing.txt` D5).
- **Drops** (V2): the office pit (1.2 m) from T2, the split suite (2.4 m, two ways up) from T3 (`20` §2).
- Today every cell is level (`40` :511), so in the game every ramp waits for 关卡设计's per-cell level; in scene 4 SP5 already shows the 1:8 ramp.

### 3.6 As parameters the generator can hold

`40` §11.9 (Red 11-03): one whole module at its natural size, footprints in whole cells, levels in whole 0.30 m steps, ports on cell edges, so modules relate by integers. A Run! is one set piece placed whole, 1 x L cells, its ports on cell edges (the arch or door on the entrance edge, the goal door on the far edge), crossing one chunk border by its own planner's rule (the Run! planner already owns two chunks; LMS's "a room never crosses one" is for room modules).

| Parameter | Value | Today |
|---|---|---|
| `lengthCells` per room | 7 first, 9 standard, 12 late, 14 deepest where it fits | const 9 |
| `capCells` | 14 | — |
| Receding `L0` / `Lmax` | 9 / 12 (T4, floor ≥ B2); 9 / 14 (T5, floor ≥ B3, fit permitting, else 12) | — |
| Receding trigger | the end of cell 3 (z 9 m), the first compartment frame | — |
| Head seats | whole cells: 27, 33, 36 m (R 9); 27, 33, 36, 39, 42 m (R 15) | — |
| Ramp | 1 level cell, then 8 cells at 1:8 in three runs, −10 steps (3.0 m) | none |
| Per floor | at most 1 Run!; deck by tier (3.4); no repeat; receding not two floors running | dice per zone |
| Run flag | a plain Run! met this run before any twist | — |
| Release | no other trigger kind in the host zone or the zone beyond the goal door | — |
| Entrance | arch, or a Run door (RN-F + F5) on the entrance edge (§7.2) | arch |
| Unchanged | first send; tier ≥ 2; not the arrival zone; ≥ 15 cells from the start door; Relay entries ≥ 9 cells; never the only route; key never only inside | as is |

All of this is 关卡设计's code (`FrontRoomsMap/*`, rule 3), proposed through 总控.

## 4. The receding end

From `endless.txt` E1-E17 and its design notes D1-D14, `motion.txt` §3-§4 (M1-M9, R1-R8, L1-L9), `pacing.txt` D3-D4, `story.txt` E6. Red's numbers: the trigger is "它原本房间最初始长度的三分之一", and the door "往后退了，它越退越远了".

### 4.1 What games and films did (the precedents)

| Work | What the player or viewer meets | How it was made | Lesson for Run! | Row |
|---|---|---|---|---|
| Poltergeist (1982) | Diane runs to her child's room; the hallway stretches and the door does not come closer | A dolly zoom: the camera pulls back while the lens zooms in | Red's "门往后退" is this shot. The goal stays centred, lit and the same size; the stretch denies the arrival, it never hides the goal | [LS201] |
| Vertigo (1958) | The stairwell falls away | A model stairwell laid on its side, tracked and zoomed | In a game the camera is the player's head: a zoom the player did not ask for reads as nausea. Build it in the set, not the lens | [LS202] |
| Amnesia (2010) | Sanity effects | Grip tuned the screen distortion until it made him sick, then set it below what felt best | Any FOV accent stays small, below what feels best, and off under motion options | [LS212] |
| P.T. (2014) | One short hallway, looped through its far door, one change per pass | Door-to-door teleport; objects shown by state flags | A loop's seam belongs inside a door the player opens; a short hall shows every change | [LS203] |
| Layers of Fear (2016) | Rooms change when you look away | Triggers by spot or gaze; lights and sounds make you look away | Ahead of the player, change in plain sight or not at all; never mix in-view moves and unseen swaps | [LS204] |
| Little Nightmares II (2021) | Mono's dream hallway with a door at the end, never reached until late | Denied by an agent (Six pulls him back), not by geometry; no source read says the hall stretches | The far door can be one recognisable promise across the game; a denied door always shows one open way, lit | [LS206] |
| Silent Hill 3 (2003) | A red glow chases Heather through a half-maze; one wall moves | A fixed route; the trap graded by difficulty | One moving wall, once, on a fixed route keeps a chase learnable; red frightens when it moves | [LS208] |
| Silent Hill 4 (2004) | Room 302: the world seen through the peephole, never reached by the front door | — | Keep the lit "beyond" seen through the goal lite constant while the door moves | [LS208] |
| Control (2019), the Ashtray Maze | Walls fold and floors rise around the player, in time with a song | Modular, heavily animated; the most linear part of the game | Disorientation in what moves, never in where to go; modules on one grid; the stop is the safety | [LS209] |
| Dishonored 2 (2016), the Clockwork Mansion | Levers transform rooms; the player can go behind the walls | One source of movement drives the whole piece; the AI works in both configurations and while rooms move | One prime mover; show the mechanism in the seams; the Relay's graph valid before, during and after | [LS219], [LS37] |
| Portal 2 (2011) | Panels on robotic arms reshape chambers | Large moves only where the player's position was certain; crushers cut as "too punishing" when players were hit by what they were not watching | Move only ahead of the player, outside the walk volume, in view | [LS219] |
| The Haunting (1963) | A door bulges into the room | Real laminated wood pushed by a timber from behind; one cable inside a column shakes the whole stair; brightly lit sets | Real material under impossible force; one hidden mechanism; fear without darkness | [LS218] |
| Labyrinth (1986) | An endless walled passage; the Escher room | False perspective extended the mazes; Henson kept Sarah upright as the audience's fixed reference | Keep one thing steady (the goal door) while the rest misbehaves | [LS216] |
| Inception (2010) | The rotating hotel corridor | The ceiling fixtures were the practical light, so the light stayed true while the set turned | Lights ride with the architecture | [LS220] |
| A Nightmare on Elm Street (1984) | The stairs melt under Nancy | Pancake mix; a rotating room set | The floor that betrays the runner is the line a game must not cross: the floor may move away, never slow or trap | [LS217] |
| The Shining (1980) | Low, fast tracking shots down the hotel's corridors; a plan that makes no physical sense, felt rather than spotted | A wheelchair Steadicam rig run at repeatable speed; the discontinuity was on purpose (Harlan, via Wikipedia) | Quiet impossibility: a corridor a little longer than its building allows, and speed past a regular rhythm of lamps, are felt before they are seen | [LS215] |
| The Evil Within (2014) | The world shifts on a script | Scripted changes ended by cuts and teleports | A first-person, three-platform game has no cut; one rule, learnable in one pass | [LS207] |
| Antichamber (2013), Inside (2016) | The exit seen from room one; a far shore reached after the longest run, quiet and lit | Bruce refused to move places once players had learnt them | The receding end recedes the same way every time; the arrival is quiet and lit | [LS211], [LS210] |
| Outlast 2 (2017) | Chases built from beats | Light leads; a disruption at the moment of false safety; chases kept short | The recede is that disruption, once per room; the door stays the brightest thing | [LS213] |
| Real 1977-1990 hardware | Folding partitions on overhead trackways; accordion fire doors closing at 0.15-0.61 m/s (NFPA 80); jet bridges telescoping in nested tunnels; hold-open magnets that release on alarm | Patents, a code course, encyclopaedia | The parts are real; only the speed and the fact that a wall does it are impossible | [LS222], [LS239] |

What the record does not show: no source read shows a game that moves a far door back in view, as a model animation, during a first-person chase (`endless.txt` NOT SETTLED). Poltergeist (film), Control's maze (no chase) and Silent Hill 3's moving wall are the nearest. The mix below is new and is tested by walking it in scene 4 (§10).

### 4.2 Four mechanics

| | M1 · THE STRETCH (recommended) | M2 · THE LOOP | M3 · THE FAR SHORE | M4 · THE STREAMED POCKET |
|---|---|---|---|---|
| The player feels | The door backs away as they run at it, then stops; they reach it | The far door opens onto the same corridor again, one thing changed per pass | The door backs away and is not reached here; a side door that was there all along lights, and the run ends through it | A service corridor of fire compartments that never ends |
| Precedent | Poltergeist, Silent Hill 3, Control | P.T., Mario's stairs, The Exit 8 | Little Nightmares II, Silent Hill 4, Labyrinth's hidden opening | the title stream (FrontRoomsRoomStream.cs:4-18) |
| Build | the room planned at 12 or 14 cells; the end stands at 9 cells and backs away once | a door-seam teleport; a world counter | M1 plus a side door; C38 amended for that room | a third streaming system |
| Against the rules | none if §4.8 holds | the trip sent the Relay here; a second pass doubles the time inside; the goal door stops being the escape verb (Red 10-31 on G1/G2) | the way out is in doubt for a moment (Grip); C38 says the goal door is the exit | the Relay cannot follow: a sanctuary in effect |
| Verdict | **the Run! recede** | not for Run! | later, as 叙事设计's payoff: a late room where the same door is finally reached | a one-off for 叙事设计 and 系统设计, not a Run! variant |

### 4.3 The trigger at a third

A third of 27 m is **9 m: the end of cell 3**, where `10` RN-2 puts the first smoke-compartment frame (fire doors held open on magnets every 3 cells) and where SP5 already has FD1. Red's trigger and a real threshold coincide (`motion.txt` P5). Today's Run! B kit has no compartment frames (`motion.txt` U4), so 游戏视觉 builds that frame for the receding variant; the trigger plane is its plane.

**Gaze gate** (Portal 2's rule, [LS219]): the first step waits until the camera faces down the corridor, within 35° of the axis toward the goal; if the player never looks, it runs anyway when they pass two thirds of the first length (18 m). Once started, the schedule runs to the end whatever the player looks at.

### 4.4 The order of trip and recede

| Order | What happens | For | Against |
|---|---|---|---|
| A · trip, then recede (as today) | Trip at 1.5 m; the player passes 9 m 1.4 s into the sprint with 3.6 s of bar and the door 18 m off; the end backs away | The strongest nightmare: the breath runs out 6.9 m (12 cells) or 12.9 m (14 cells) short of a door that fled | Fair against a Relay called from Away (3.2 (c)), but not by construction; `pacing.txt` D4 would play it only when the trip calls the Relay from Away, and deciding that at the trip makes the recede report the Relay's state (rule 11; `motion.txt` U16) |
| B · recede, then trip | The room stays armed while the end steps back; the trip line moves with the end, 25.4 m before the seated door | Every body can be read at walking pace; the sprint is at most today's | Nothing is at stake while it moves; a walker ends 13.6-17.5 m from the door at the final seat, so the receding room is easier than a plain one (`motion.txt` R4, FD6) |
| **T · the third trips it (recommended)** | In the receding variant the arm line moves from 1.5 m to the frame at 9 m. Crossing it trips the alarm and starts the recede: one event, "the compartment goes into alarm and its end is pulled away" (one event for a player facing the goal; 4.3's gaze gate can hold the first step until the player looks, or until 18 m) | From the trip a full bar (27.5 m) reaches a 12-cell door (26.9 m, 0.1 s spare): today's promise exactly. A 14-cell door is 32.9 m off: the breath ends 5.4 m short, the late band (between an 11- and a 12-cell plain room). The alarm and the door's flight are the same moment. Nothing reads the Relay; the variant is dealt when the zone is decided, before the player sees it | It bends the written rule "the room arms 1.5 m inside" (RELAY :371), though only by giving 7.5 m more room to step back; the tell still comes before commit. A player learns that receding rooms trip late (learnable, so fair). The chase dive (4.7) |

Order T is this page's synthesis of the three drafts: it keeps pacing's single strong moment (`pacing.txt` D3), motion's rule that nothing reads the Relay (`motion.txt` R4) and story's "winnable from the trip with a full bar" (`story.txt` F3).

### 4.5 How far and how fast

- **How far:** R = 9 m (12 cells, T4) or 15 m (14 cells, T5); the head seats on whole cells (3.6).
- **How fast, in beats** (`motion.txt` R5, T2 timings; L4 "one change per beat"): S1 a two-cell flinch (tell 0.25 s: two magnet clacks and a 15 mm jolt toward the player; travel 1.1 s; seat 0.2 s), dwell 0.2 s; then one-cell steps (tell 0.15, travel 0.8, seat 0.2); the final step slower and seated heavily (tell 0.25, travel 1.0, seat 0.35). R 15 seats at 1.55, 2.90, 4.25 and 6.05 s; R 9 (the flinch and the final step) at 1.55 and 3.35 s. The T1 body (the whole end rolling) is quicker: R 15 seats at 1.15, 2.15, 3.15 and 4.85 s.
- **Keep-away** (`motion.txt` L9): whenever the head is between steps and the player is within 12 m of it, the rest runs as one glide at 6 m/s, just faster than the 5.5 m/s sprint, ending in the heavy seat. It reads the player's position only. It is required: the goal door is never locked (C38), so a door caught mid-recede would open onto the unrevealed length.
- **What each player sees** (from the trigger: player at 9 m, door at 27 m, gap 18 m; `motion.txt` R5, and this page's arithmetic for R 9):

| Player | R 9 (12 cells) | R 15 (14 cells), T2 body |
|---|---|---|
| Walks (3.2 m/s) | the gap grows to 19.7 m in the flinch ("越退越远"), 16.3 m at the final seat | grows to 19.7 m, then 19.0, 17.7, 16.4 m at the seats, 13.6 m at the final seat |
| Slow walk (1.44 m/s, the slow gait: `FrontRoomsTouchControls.SlowSpeedShare` .45 x 3.2) | the gap grows throughout | 21.8 → 24.3 m, growing throughout |
| Sprints with a full bar from the trip | 15.5 m at the flinch's seat, 8.6 m at the final seat; door reached 4.9 s after the trip with 0.1 s of bar left | 15.5 and 11 m at the first two seats, then the glide; seated at about 4.5 s, never closer than about 10 m to a moving head, 8.1 m from the door when it seats; door reached about 6.7 s after the trip |

- **Speed in perspective:** real accordion fire doors close at 0.15-0.61 m/s under NFPA 80 ([LS222]); by `motion.txt` R5's own rule (peak = step length / (travel − 0.175 s)) the T2 body peaks at 3.6-6.5 m/s in a step (the final step 3.6, a cell step 4.8, the flinch 6.5) and the T1 body at 4.1-10.4 m/s (its 0.75 s flinch 10.4), and the glide runs at 6 m/s: six to seventeen times the fastest real fire hardware (0.61 m/s). The parts are real; the speed is the impossible (`motion.txt` M7).
- **Against a sprinter no stepped schedule holds the gap** (that needs a cell every 0.55 s, under every body's minimum period); the glide is what makes the door flee from a runner. The flinch is what a walker sees flee.
- **For Red's "越退越远":** the end's place always moves back (27 → 36 or 42 m), but the gap to a walking player grows only in the flinch; only a slow (Shift) walker sees it grow throughout. A single 6 m/s move (`pacing.txt` D3) would make the door flee from every player while it moves (walker: the gap 18 → about 21 m at R 9 or 24 m at R 15, with the 0.25 s tell; sprinter: the gap grows 0.5 m a second) but leaves T2 no time to re-stack (5.3), so 越退越远 and 重组 trade against each other at these speeds; §8 decision 4 put it to Red, who took the steps on 11-03 (§0.1).

### 4.6 How it ends

- **A heavy final seat** at L_max: a bolt-home, the sweeps pressed, the extension's strips lit and steady; nothing in the room moves again this visit (`motion.txt` L5). In Control the places where the shifting stopped are the safe ones ([LS209]): the stop is the moment the goal becomes real.
- **The door is real and opens** (C38). A chase ends at a door that opens ([LS229], [LS213], [LS232]).
- **The record stays.** The room keeps its receded length for the floor; a rebuilt chunk is rebuilt receded, the way the door break keeps a 16-byte record and rebuilds only 48 m away (`door_break/50_rethink.md`:403-420). With the T1 head, a 200 mm "ghost band" of cleaner paint and unworn wax marks every place the end stood.
- **Never a second recede** in one room. Constantly slowing movement "can prevent the player having a feeling of progression" (Davies, [LS231]).
- **The reset:** when the director goes Away, the white re-strike Red likes walks the whole length from the entrance (14 cells: about 2.1 s after its 0.45 s start). After it, the room is a plain room of its new length that arms 1.5 m in.
- **The release side.** Before the recede, the frame at L_max shows a plain wall from beyond; the swap happens while the player is in the corridor (`motion.txt` R2). A player who reaches the release first finds a blind doorway where a door will be: the "two doors" case is 关卡设计's (`motion.txt` U7). Inference: a blind doorway seen from beyond is itself a quiet, honest sign that something here moves.

### 4.7 The Relay's place (rule 11)

- The schedule reads the player's crossing, gaze and distance to the head only. A recede that started, waited or stopped by where the Relay is would be radar (`motion.txt` R4, `story.txt` E6).
- The Relay is never in the hidden length: it is off its graph until each seat, and the head's travel envelope is closed to it. It never arrives from ahead: no Relay entry inside a Run! room or within a set distance beyond its goal door, and the goal door never on the route it is sent by (`story.txt` F4; the validator checks RELAY :369's automatic entries).
- **Called from Away** (the usual calm entry): its earliest arrival is at least 19.7 s after the trip. From the trip a walker reaches a 14-cell door in 10.3 s, so a player who stops for the whole 6 s recede still has more than 3 s in hand at T5 (this page's arithmetic on 3.2 (c)).
- **On the map** (BUILD-UP, RELAX): the trip re-dispatches it to the device; it walks in at investigate speed, 2.6 m/s, slower than the player's walk, with 12 m of calm sight and 15 m in a chase (RELAY :326, :755). Inference: a player who keeps moving is out of its sight before it reaches the arch. 系统设计 confirms with bots.
- **The exposure: a chase dive.** In a chase a device trip is "heard only as a noise" (RELAY :356-360), so a player who dives into a receding room with the Relay behind meets the recede at full price. With a full bar at the arch and the Relay at 4.4 m/s on the same line, the player's lead at the goal door is the lead at the arch plus 5.4 m in a plain 9-cell room, plus 2.4-3.5 m in a 12-cell receding room and plus 0.1-3.2 m in a 14-cell one (walking after the bar, or timing a second burst). So the recede costs a chased player about 2-5 m of lead (this page's arithmetic; `story.txt` E3 V4 calls the chase dive the most exciting Run! there is). Options for 系统设计: (a) accept it as the deep-floor price, made fair by an honest tell before commit (the head's overhead track, visible from the arch, §5.1); (b) recede only when the player crosses the third at a walk, which reads the player and not the Relay, so a sprinter, chased or not, gets the plain room; (c) measure first. **Recommended: (c), a bot round of chase dives at T4-T5 with runRooms on; then (a) if a dive with an 8 m lead still escapes, else (b).**

### 4.8 Fairness rules for the receding variant

1. **Readable before commit:** the ALARM ZONE card and the detector at the arch, as every Run!, plus the head's overhead track in the soffit corners, visible from the arch (§5.1).
2. **A step back:** nothing trips at the reveal; the trip is the frame at 9 m.
3. **Winnable from the trip:** a 12-cell receding door is one full bar from the trip; a 14-cell one is the late band, the breath ending 5.4 m short (between an 11- and a 12-cell plain room).
4. **The hunter never arrives from ahead** (validator).
5. **Never locked, never the only route, never the key alone** (C38; RELAY :363-370).
6. **One recede, at one moment, the same way every time.** When anything can change at any moment, the player stops reading the space (the draft's reading of The Evil Within's scripted shifts, [LS207]; one critic's complaint that it broke its own rules was seen in search only); Antichamber kept learnt places fixed ([LS211]).
7. **The goal is the constant:** door, lite, panic bar, goal head and C42 ride the head, keep their brightness and stay centred ([LS201], [LS216], [LS220]).
8. **Everything that moves is ahead of the player and outside the walk volume;** nothing moves behind or beside them; nothing can slow, trip, trap or crush ([LS219], [LS217]).
9. **No side door, branch or false door inside a receding room** (Run! B's side doors at z 5, 17 and 23 stay out): the way out is never in doubt ([LS229]).
10. **Nothing reads the Relay** (rule 11).
11. **No cut, no teleport, no FOV trick:** the change is in the set, in one continuous view ([LS202], [LS212], [LS207]).
12. **Three-platform parity:** timing and length identical on desktop, iPhone and iPad; only the model's detail scales (`motion.txt` L8).

## 5. The model transition: four bodies on one head

From `motion.txt` §1-§5 and its design notes FD1-FD11. Red: "它不只是 motion transition， 它也是模型上面…像是我们之前有专门做过一个门的那种可破坏的这种地形嘛…重组或者向后退避的这样子的一个3D 的模型动画". What carries over from this project's door break (`research/door_break/50_rethink.md`, `51_prototype.md`, `52_production.md`, `54_dust_grit.md`; `motion.txt` §1): pre-built pieces, never runtime cutting; one change per beat, hidden in the jolt; baked or analytic motion, merged into one static mesh at rest; a few bytes of record; dust as grit that reads only against light; desktop full, phones LOD1 with capped pieces, WebGL plain. And a scale rule: at 20-40 m a 50 mm detail is 1 px (1080 px at a 76° vertical FOV gives 691/d px per metre), so a transition seen down the corridor moves **whole modules, frames and lamps**, and light carries it; bolts and rollers are a bonus for the near cells.

### 5.1 The head (shared by all four)

Every option is a **head** (the end assembly) that steps back along the axis plus a **body** rule for how the new length appears behind it (`motion.txt` R1).
- **Parts:** the map's end wall and RN-F goal door made one movable assembly: block-faced wall, frame, door with its F5 lite and panic bar, **one twin-head unit carried above the door** (today's goal head is the +Z head of the unit at z 21, which would light empty corridor after a recede), and **C42's light in a small box behind the lite**, so it lights the lite and never floods through an open seam. Handing the goal light from wall unit to wall unit at each seat would be a light change per step, a flash per step: avoided. Aim (inference, to be checked in scene 4): today's goal head lights the door from 6 m away; a unit flush above the door would light it only at a grazing angle, so it sits on a short bracket about 1-1.5 m forward of the wall, aimed back at the leaf and the floor before it, and 游戏视觉 tunes it until the door is again the brightest thing.
- **Machine:** two trolleys on an **overhead track in the soffit corners** and a bottom guide in a 20 mm floor slot, as real folding partitions and accordion fire doors hang (overhead trackways, no floor track: [LS222]); **hold-open magnets** keep it seated, and their release is every step's tell; rubber edge sweeps with two poses (pressed when seated, relaxed when moving).
- **Proposed here as the honest tell** (§4.8 item 1): in a receding room the track is surface-mounted on the exposed soffit (`motion.txt` U11) from the 9 m frame to the end wall, in view from the arch. A careful player can read "this end runs on rails" before committing. Inference: at 15-25 m a 100 mm track is 3-5 px, readable as a line the way the sprinkler main reads today; to be checked in scene 4.
- **Cost:** about 8-10k tris LOD0 (door and frame 3-5k, against the door break's 1,746-tri body; the twin-head unit 3,706; slab, trolleys and seals about 1k) and about 8 draws, 2-4 more than today because the head can no longer be merged into its chunk (`motion.txt` §5 head). Hero LOD0 holds up at 0.3 m (the player ends at this door; sheets show a 0.35 m hero row); LOD1 and LOD2 for distance and WebGL (VISUAL_STANDARD §1.1; switching at about 20 m is this page's estimate, not the standard's).

### 5.2 T1 · 后退 · the end rolls back (the base; every tier, every platform)

- **The player sees:** a hard double clack from the end (the magnets let go); the whole end wall jolts about 15 mm toward the player and back; a line of grit drops from the soffit seam and glows in the goal lamp. Then the entire end (wall, door, frame, panic bar, the lit lite, the lamp above) rolls straight back, upright, without a wobble, like a stage wagon. It uncovers a 200 mm ghost band on walls, soffit and floor where it stood (cleaner paint, unworn wax), the track and the guide slot running on, and the next cell, lit by the lamp riding the head until its own strip strikes at the seat. The seat: a heavy thud with a 30 mm overshoot, the door rattles once in its frame, a puff of grit at the floor seam.
- **Build:** the GoalHead prefab under one root; sweeps as a blend shape; track and slot in the hidden cells' meshes; ghost bands authored as masks in the existing paint, VCT and soffit materials (no runtime decals), covered by the head while it stands there; one analytic curve per step from one trigger time; the head kinematic, its collider moving with it; at each seat the uncovered cell's renderers, colliders and lights turn on and the map adds its graph edges.
- **Cost:** the head only (5.1), textures ≤ 3 MB (track and seal trim, ghost-band masks), CPU nothing measurable; one strip strikes per seat (keep revealed strips unshadowed, or shadow only the newest, inside `10_plan.md` §3's 3 armed / 8 tripped shadowed lights).
- **Era:** every part is 1977-1990 hardware or the corridor's own kit; only the speed is impossible.
- **Risk:** the least "model" of the four: a rigid move with mechanical detail. It is the base the others build on and the fallback on phones and WebGL.

### 5.3 T2 · 重组 · the wall re-stacks (Red's word; the door break's kin)

- **The player sees:** the same tell. Then the end wall comes apart course by course from the top: each block lifts out of its bed by 20-40 mm, turns a few degrees and slides back along the side walls and the soffit in two streams (the left half left, the right half right, never across the centre line the player looks down). In the middle the door, on its steel subframe, rolls back on its own, upright, lit and untouched: the one thing that does not come apart. A cell or two farther the blocks drop back into courses from the bottom up, closing round the subframe, each landing with a small settle; mortar grit sifts from the joints and glows in the goal lamp. The new wall is the same wall: same blocks, same 1.2 m two-tone line. The last step closes round the real frame at L_max with one heavier knock.
- **Build:** the end wall as about 90 block pieces (0.40 x 0.20 m units, an estimate until the real Run_Block layout is counted: `motion.txt` U5), each a chamfered box of about 60 tris LOD0 / 12 LOD1 with object-space UVs matching the seated wall, swapped in at the first tell (the intact wall stays the plain slab); two Blender rigid bakes, a one-cell clip reused with a 3 m offset and a two-cell clip for the flinch, 30 Hz keys per piece plus a contact list for sound; playback as CPU transforms or one instanced draw per material; merged back to the slab at each seat. Streams stay within 0.35 m of the walls and above 2.45 m on the soffit (the services line, 0.40 m over the Relay's 2.05 m); no piece passes in front of the lite.
- **Cost:** in flight about 5.4k tris and 1-3 draws on top of the head; seated as T1; textures ≤ 2 MB; bake data about 100-120 KB per clip; grit as `54`'s GPU path, one sift per course landing, desktop only. Phones: half-course strips (about 28 pieces), no grit. WebGL: T1.
- **Era:** the material is exactly the period's painted block; the behaviour has no machine. Its visible order is its machine: top out, bottom in, left left, right right, every block landing on a beat.
- **Risks:** it reads supernatural rather than mechanical, so it fails `story.txt` E6's "honest machinery" rule (that rule is this line's proposal, not Red's; Red's own word is 重组). About 90 like units moving together is a moving regular pattern: one painted tone, well under a quarter of the view from 15 m, about 1 s a step; the Harding or PEAT capture is the gate. The re-stack needs at least 0.8 s of travel, so a runner sees it in the flinch and then gets the keep-away glide, in which the wall rides back whole, as T1.

### 5.4 T3 · 伸缩 · the corridor telescopes (deep floors)

- **The player sees:** before entering, the last cell already shows the machine: a stepped steel mouth of five nested rims, each 40 mm inside the last, the door at its bottom (the last cell is 2.44 x 2.70 m instead of 2.84 x 2.90). At the trigger the two innermost sleeves shoot out together 6 m on rollers turning in the top corners and stop with a ringing bang; then each next sleeve is drawn out in turn. Every joint is a lit steel rim; walls and ceiling step in 40 mm at each, a real taper that adds apparent length (film's forced-perspective corridors taper on purpose, [LS221]) and counts the metres. The floor is the hidden length's own VCT; the last step latches.
- **Build:** five sleeve prefabs (two wall panels, a ceiling panel, a rim with roller housings, one R-B strip each) nested in the last visible cell; one curve per sleeve, each leaving when the previous one stops; object-space UVs (world-projected finishes would swim on a moving mesh); services stop at the fixed corridor's end.
- **Cost:** about 1.7k tris LOD0 per sleeve, 8.5k in all, plus five strips; about 15 draws; textures ≤ 4 MB.
- **Era:** a real machine since 1958, the jet bridge: B encircles A, rollers at the upper corners ([LS222]). In an office service corridor it does not belong, which is the uncanny part; every part of it is true.
- **Risks:** it changes the last third's material and section, which Red's G1/G2 rule may count as changing the room's nature. It shows the trick from the entrance: fair, but no surprise.

### 5.5 T4 · 折叠 · the alarm doors unfold (deep floors; the building's own fire hardware)

- **The player sees:** in the last cell, tall panels finished like the block wall, each with a thin steel edge and a fire-rating label. At the trigger the magnets release and the pocket covers swing open; the end (a steel lead post carrying the goal door) runs back on the overhead track and draws out of both pockets two walls of folded corrugated steel, grey-beige 4 1/2 in pleats unfolding into the new corridor's walls as fast as it goes. The pleats catch the strips in fine stripes that open out as they stretch. The last stop latches the lead post into the jamb.
- **Read:** the alarm that calls the Relay is the same alarm that moves this building's fire doors, running along the corridor instead of across it, many times faster than any real fire door may run. With Order T (§4.4) that reading is literal.
- **Build and cost:** two pleated runs of about 140 panels (about 40 tris each) as per-instance matrices from the head's travel (about 280 a frame), 2 draws for the pleats and 2 for the covers plus the head, about 11k tris; textures ≤ 4 MB; pockets planned 0.35-0.45 m deep (a present-day product lists a 14 in stack, [LS222] search only; a 1990 unit not read).
- **Era:** the most period-specific of the four: fire-rated horizontal accordion doors passed UL tests in 1977, entered the 1988 Life Safety Code for selected egress uses, hide in pockets behind matching covers, and close on the alarm panel or a pull station, with battery back-up ([LS222]).
- **Risks:** two walls of fine regular stripes moving and changing pitch is the textbook pattern trigger (more than five light-dark pairs, changing: [LS250]); it must pass a pattern capture before anything else is decided. The extension's walls become corrugated steel (Red's G1/G2 rule).

### 5.6 Side by side (desktop; estimates except where cited)

| | T1 后退 | T2 重组 | T3 伸缩 | T4 折叠 |
|---|---|---|---|---|
| The corridor stays a corridor | yes | yes | the section narrows | the walls become steel |
| Model content | head, seals, track, ghost bands | about 90 blocks re-stack round the door | 5 sleeves, rims, rollers | about 280 pleats, pockets, lead post |
| Minimum travel / step period | 0.5 s / 0.8 s | 0.8 s / 1.1 s | 0.6 s / 0.9 s | 0.6 s / 0.9 s |
| Seen by a sprinter | all of it (rigid, readable at any speed) | the flinch, then the whole wall gliding | the sleeves following the head | the pleats following the head |
| Moving tris / draws (T2-T4 on top of the shared head, 5.1) | about 9k / 8 (the head) | +5.4k / +1-3 | +8.5k / +15 | +11k / +4 |
| New textures | ≤ 3 MB | ≤ 2 MB | ≤ 4 MB | ≤ 4 MB |
| Photosafety | the lamp beat only | + a moving block field | the lamp beat only | + a moving stripe field (HIGH) |
| Era | real hardware | real material, no machine | real machine, wrong building | real fire hardware, wrong direction |
| Tiers | all; the fallback | desktop, iPad; phones half-courses; WebGL T1 | deep floors | deep floors |

### 5.7 The light during the recede: one grammar

`red_light.txt` FD10 asks for one grammar. With Order T the recede runs in the tripped state, so it uses the red looks of §6, not `motion.txt` R7's white "pulse-push" (which belongs to Order B, the armed state):
- **the trip releases the hold-open magnets**, so the head's first tell starts at the trip (t = 0), as a real alarm panel cuts power to its door holders ([LS239]) (if 4.3's gaze gate holds the first step because the player is not facing the goal, the tell waits for that first look, or for 18 m; the times in 4.5 assume a player facing down the corridor at the trip); the trip's dark band (Look A or B) starts at the entrance as in every Run! and runs to the end at 20 m/s, reaching it about when the flinch seats (1.5 s): the alarm drives the door away;
- each revealed cell's strip **strikes once**, in the corridor's current colour (red after the trip), as the head seats: one-way, a rise, never a flash;
- the goal head and C42 ride the head and **never dip, pulse or change colour**;
- Look B's pulses begin after the final seat (at the later of 3.0 s after the trip and 1.0 s after the seat);
- Reduce Flashing: no dark band, each strip cross-fades over 0.5 s, the strikes are 0.5 s rises, no pulse;
- the keep-away glide carries no pulse.
Count on screen in the combined test (T9), then the Harding or PEAT capture of the recede at T5 (`motion.txt` FD9).

### 5.8 Engine notes for 游戏视觉 (`motion.txt` D2-D9, M8, K2, K5)

- URP motion vectors: the project upscales spatially (FSR or linear, `FrontRoomsQuality.cs`:227-231) and wall fixtures render camera-only motion vectors, so moving parts need nothing today; if TAA, STP or motion blur comes on, they need Per Object Motion or they smear ([LS223]).
- A moving part stays out of static batching and combined meshes until it seats; at the final seat it may be merged ([LS223]; the door break's practice).
- World-projected finishes swim on a moving mesh. T3's sleeves need object-space UVs; the T1/T2 end wall's face lies across the axis, so moving it along the axis does not swim (inference from the projection, to be checked).
- Keep moving parts out of the Mac's ray-traced glass acceleration structures (only the lite is glass).
- Probes: the corridor is self-similar, so one box-projected probe set per 9.6 m segment is reused and moved with its segment; no new bake per length.
- No per-frame C# particle simulation (the door break's dust cost 4.7 ms in the Editor): `54`'s GPU grit, lit by the goal lamp, gone within 3 s.

### 5.9 Recommendation, owners, build order

**Recommended:** the T1 head on every tier and platform, carrying **T2 重组** on desktop and iPad (phones: half-course strips; WebGL: T1 alone). T3 as the deep-floor alternative if Red wants the corridor itself to move. T4 held until a pattern-safety capture passes.
Why: T2 is the only body that answers "重组" while the corridor stays a corridor, and it is the door break's own studio method. T1 under it makes the honest machine (track, trolleys, magnets) visible and is the fallback.
**Honest flaw:** T2 has no visible machine (the blocks move themselves), so it reads as the building reorganising itself, not as hardware; a sprinter sees the re-stack only in the flinch. If Red prefers the honest machine, T1 alone, with T3 on deep floors, is the choice (`motion.txt` FD2, U17).
**Build order:** the head first (it serves all four), then one body (`motion.txt` FD1).
**Owners** (through 总控): 游戏视觉 the kit and the rig state (`RunRigState.Recede`, one timeline from one trigger time, the keep-away glide, the gaze gate, events), with 灯具适配 for the carried twin-head and the hidden strips; 关卡设计 the reserved length, the hidden cells, the movable goal, the goal point at each seat, colliders, walk volume and graph per seat, the release-side swap, persistence; 声音设计 binds the events (5.10); 系统设计 sets Order T and R per tier.

### 5.10 Events for 声音设计 (a list only; nothing is played here, rule 24)

T1: magnet release (two clacks), head unlatch jolt, trolley roll on the overhead track, sweep drag, seat thud with door and panic-bar rattle, grit patter, strip strike (starter tick, ballast hum rising), final bolt-home, a pressure "whump" as the space grows. T2 adds: mortar crack at the tell, block lift scrapes, the streams' clatter, a thud per course landing, mortar sift, subframe slide. T3: release clunk, roller trundle, ringing stop per sleeve, latch, the tube's boom. T4: pocket cover swing and slap, contactor clunk and battery gear-motor whine, pleat rattle rising with speed, leading-edge switch click, final latch. The rig raises C# events only (AUDIO_CONTRACT).

## 6. The red atmosphere, keeping the lamp chase

From `red_light.txt` §1-§8 and its design notes 1-12. Red: today's light "普通状态下白色的色调，但是它失去了那种在后世同人的这个游戏当中的那种红色的非常就是庄严，但同时又非常危险的那种颜色的这种氛围", and Red likes the lamps going "成隧道一样，成脉冲一样…一个一个灯光熄灭，一个一个灯光在亮".

### 6.1 What the research found

- **The fan games, not the canon text, hold the look Red remembers.** The canon's Level ! is lit by "a dim, red lightning which is emitted by exit signs" (A-Sync fan copy of the Fandom page). Escape the Backrooms makes every ceiling lens a flat saturated red panel, one row receding to a vanishing point, small greenish-white EXIT signs down the centre line (and one white downlight over the nearest) the only other lights, walls and floor near black-red; its guide adds "bright red lights and a continuous siren" (a frame on this disk, `room_visuals/images/run_ref_etb_levelbang_0094.jpg`, looked at; [LS232]). The public memory is "flashing red lights and blaring alarms" (Nerdist, [LS232]): the one version we may not ship. **What Red misses is exactly where ETB has it: on the ceiling, as a procession, after the trip.** Run! B already owns a lamp in every 3 m cell and a red line overhead (the sprinkler main), so red can return as light without changing the room (G1/G2 holds).
- **Film's solemn red is functional and comes from the set's own fixtures.** Das Boot: red was the boat's general-alarm and night light, from its own lamps, chosen by function (Vacano, [LS240]). Aliens: the colony's power dies and the scene plays in red emergency light; in the shooting script "Bars of light move rhythmically across her" as Ripley rides the freight lift down ([LS241]). The Hunt for Red October: the American boat's control room in red, one colour per boat (blue, red, green) so the cutting stays legible; a bent rule, since a submarine "would only 'rig for red' if it was going to periscope depth" ([LS242]). Alien's flashing finale now carries a photosensitivity advisory ([LS243]). The Terminator and The Abyss, listed from memory in `03_run_research.md` §3.6, show no verified red danger light in the texts read; their lesson is the same practical-source rule ([LS244]). Lesson: red reads grave when it has a job and a source; a grade with no fixture reads cheap.
- **A real 1990 US building lit no corridor red.** Every red it owned was a signal or a threshold: EXIT letters (banned in Run!, C41), red pre-ADA alarm flashers (the 1991 rule made new ones white strobes at 1-3 Hz, [LS239]), the darkroom safelight (a 15 W bulb behind deep red glass, [LS245]), the roof's obstruction beacon (deep red, 20-40 flashes a minute, [LS245]), battery "AC ON" pilots, fire-gear paint. A red corridor is film and fan language, chosen knowingly.
- **What makes red solemn, not cheap.** One continuous red that means "something is present" (the sanctuary lamp, [LS248]); dark red in low light against a neutral surround (Rothko's Seagram murals shown in reduced light, [LS248]); slow and regular (the beacon's 0.5 Hz); a procession on the centre line. Cheap: red over everything at high level, red flashing, red from no fixture, random flicker, big red bloom.
- **Our pipeline punishes bright red.** Bright red drifts toward yellow (Bezold-Brücke) and saturated colours look brighter than they are (Helmholtz-Kohlrausch) ([LS249]); through URP's ACES tonemapper, pure red at 2x scene level already renders orange-red and today's AC ON pilot (2.6, .20, .10) renders salmon, about (255, 137, 86) (`red_light.txt` §5.5, a port of the package source; [LS249]). A deep red must stay near-primary and moderate; it can be big, not hot.
- **Under red light, hue dies and protan players see dark.** Red paint and white paint reflect red light almost alike, so the red fire gear and the ALARM ZONE card stop reading as red inside a red fill; about 8 % of men have a colour-vision deficiency, almost all red-green, and for the protan minority among them a red-lit corridor is simply dark (`03_run_research.md` §5.7; the deutan majority do not see red as dark); in a dim frame the edges are rod vision, blind to red ([LS249]; `03_run_research.md` §5.7). **Red is mood; white is information:** every look below keeps a non-red light that carries value.

### 6.2 The red source: one red tube per strip

One **red-coated T12 tube per R-B strip**, inside the wire guard beside the two white tubes, on the strip's battery (emergency) circuit (`red_light.txt` 8.0). Armed, it is unlit: a dark oxblood tube between two lit white ones, the corridor's own sign that it has an alarm circuit. It lights when the alarm takes its cell. Its light is the strip's existing Light recoloured: no new Light, no new shadow map.
- Period kind: a red-emitting fluorescent tube for darkrooms was already known by 1989, and fluorescent tubes run on battery through an inverter were known by 1989 (two US patents of 1989, [LS246]); a red tube on the alarm circuit is film language (Aliens), chosen knowingly. How common such battery packs were in 1990 corridors is not verified.
- Never an EXIT sign (C41), never a grade or fog tint, never the room's shape.
- **FLAG:** Red approved the R-B strips on 11-03 ("就这样"). A third tube changes that kit; Red sees one still of the armed strip with its dark red tube before anything is built.

### 6.3 The base numbers (all three looks)

On-screen values come from a line-for-line port of the URP Run_B grade and ACES tonemapper (a scratchpad tool, not in the project), and wall light from a hand model of the strip Lights with assumed albedos (walls 0.65, floor 0.45), so they are **targets for 游戏视觉 to tune to on a capture**, not settings (`red_light.txt` §8). The project is in linear colour space, and `MaterialPropertyBlock.SetColor` takes sRGB, so each colour is given both ways ([LS251]).

| Item | Value | On screen |
|---|---|---|
| Alarm red | linear (1.00, 0.02, 0.01); type sRGB (1.000, 0.152, 0.100) | G and B each at most 0.05 of R |
| Red tube core | 0.85 x alarm red (never above 1.2, where it drifts orange; at most 1.0 for photosafety, 6.7) | (186, 6, 23), a deep crimson; display Y 0.106 |
| Strip Light in red | intensity 1.15 (armed white 5.5), range 10, cone 172/124, shadows as built | about 5 % of the armed white in luminance (a real red gel passes about 4 %) |
| Mid-wall | red k 0.13-0.16, never above 0.16 | (62-68, 5, 6); floor about (52, 4, 4); ceiling between strips (14, 2, 2) |
| Tripped ambient | linear (1.00, 0.30, 0.24), type sRGB (1.000, 0.584, 0.527), at .08 down / .06 side / .05 up | a wall lit by it alone is (16, 3, 2): maroon-black, never a black hole |
| Warm pools, goal head, C42 | unchanged (B's T-A and T-B heads every 6 m) | pools (118-177, 98-154, 70-122); the door about (228, 213, 185), display Y 0.68 |
| Greyscale order | door > pools > red lens row (0.11) > walls (0.013) | the red row looks bright but is mid-grey in luminance |
| Never | a grade change between states, an EXIT sign, a light that follows or answers the Relay, anything on the HUD | every clock starts at the trip |

### 6.4 Three looks, one kit

**A · ALARM CIRCUIT (the solemn one).** Step in and the far door's light comes up at once (the goal head, 0.35 s, as built). Then a dark band runs from the trip end to the door: ahead of it the white strips still burn; inside it the lamps are out; behind it each strip's red tube strikes and the battery heads throw their pools. Strip c (counted from the trip end) goes white 1 → 0 over 0.06 s at t = 0.15c (a front at 20 m/s, the reset's speed); its red strikes at 0.35 + 0.15c (0 → 0.35 at once, then a smoothstep to 1.0 over 0.40 s, the reset's own curve); the heads rise in the band's wake. The band is 7 m of dark; the corridor is all red at 1.95 s, then still: nine crimson bars on the centre line mirrored in the waxed floor, maroon walls, amber pools every 6 m, one white door. Reduce Flashing: no band; each strip cross-fades white to red over 0.5 s. Flaw: the danger lives only in the wave's 2 s.

**B · ALARM PULSE (solemn and dangerous; recommended).** Look A, then every 2.5 s, from 3.0 s after the trip, the lamps dip and come back one after another from the trip end to the door at the wave's speed: one dark beat running down the red tunnel, ahead of you as you run, toward you when you look back. Each strip falls 1.00 → 0.35 in 0.06 s, holds 0.10 s, rises over 0.40 s; one or two cells dark at a time; the pools, the door and C42 never move (`red_light.txt` 8.2), so the dip never darkens the floor the Relay is read against (rule 11). In a 9-cell room the corridor stands still for about 0.7 s between pulses (the last lamp is back 1.76 s after the first dips); in a 12-cell room for about 0.3 s (2.21 s); in a 14-cell room not at all (2.51 s, so the next pulse starts as the last lamp recovers). 24 a minute, slower than a roof beacon's usual 30 ([LS245]); it stops at the reset or after 12 pulses (30 s), counted from the trip, never from the Relay. Reduce Flashing: no pulse. Meaning: EGRESS's building-wide alarm is silent ("Silent building-wide alarm. No bell", `30_narrative.md`:138), and the pulse is that silent alarm made visible, running toward the door the building wants you to use. Flaw: it knowingly relaxes this project's own rule "red light, if any, is steady" (`03_run_research.md` §5.6); it is inside WCAG 2.x by the numbers in 6.7 and stands or falls on the measured capture.

**C · RED TIDE (the narrative one).** Step in: the far door's light comes up and nothing else changes. Then, from the opening you came through, the strips turn red one at a time at walking pace (3.2 m/s; strip c at 0.25 + (1.8 + 3c)/3.2 s, one lamp every 0.94 s), each cell's pools coming on with it; ahead the corridor is still white; all red at about 8.8 s. Walk, and the red stays 2.3 m behind you; run, and it is about 13 m behind when you reach the door. Meaning: "WALK, DO NOT RUN" visibly fails. Flaws: a runner mostly sees white ahead, so the red is in the glance back; a front that follows the player at a fixed speed can be mistaken for the hunter's reach, and if playtests show players read it so, it is radar in effect and goes.

### 6.5 Side by side

| | A ALARM CIRCUIT | B ALARM PULSE | C RED TIDE |
|---|---|---|---|
| Red in the forward view, running | all of it | all of it | little: ahead stays white |
| The lamp chase | once at the trip (2 s), and the reset | at the trip, every 2.5 s (12 at most), and the reset | once, slow (8.8 s), and the reset |
| 庄严 (steady, slow, symmetric) | strongest | strong | medium |
| 危险 | colour only | colour and beat | colour and pursuit |
| Photosafety margin | wide | narrowest; needs the FPA or PEAT capture | widest |
| "Red is steady" rule | kept | relaxed, by numbers | kept |
| Build | kit + one trip state | kit + trip + pulse state | kit + one trip state |

### 6.6 The lamp chase Red likes, kept and given a meaning

- **The RESET re-strike stays as built:** at its start the red tubes go out with the heads in one step; then the flick ripple, dark, and the white re-strike from the entrance at 0.15 s a cell (`red_light.txt` 8.0). In the fiction it is the restoral; a player who escaped and watches back through the goal door's lite sees the white line walk toward them: the building lets go.
- **Its danger twin is new:** A's and B's band, B's pulse, C's tide, each running from the trip end toward the goal door, the way out. A light running toward the goal is a real signal: airport approach lights flash in sequence toward the runway, "the rabbit", to mean "this way" ([LS247]). Same grammar, two meanings: in red, toward the door while you are inside = the alarm and the way out; in white, after = the all-clear. Every front and pulse ends at the goal door, never at a window or other glass (rule 11).
- If Red meant the sprint through the pools rather than the reset (2.5; read as the reset since Red's answer of 11-03, §0.1), all three looks still work: the pools stay as built.

### 6.7 Photosafety, as numbers (`red_light.txt` §7, 8.5)

The limits ([LS250]): WCAG 2.2 SC 2.3.1, no more than three general and three red flashes in any 1 s unless the combined flashing area is under 0.006 sr (25 % of any 10° field); a general flash is a pair of opposing changes of at least 10 % of maximum relative luminance with the darker state below 0.80; a red flash (WCAG 2.2 Note 3) needs a change of more than 0.2 in u'v' to or from a saturated red (R/(R+G+B) ≥ 0.8); the older metric that Harding FPA and Xbox Accessibility Guideline 118 apply counts a red flash when the change in (R − G − B) x 320 exceeds 20 (with either state a saturated red). ITU-R BT.1702-3 exempts a pattern that flows smoothly one way and warns that sequences longer than about 5 s may be a risk even when they comply. After the 1997 "Denno Senshi Porygon" broadcast, Japanese broadcasters limited red flashing to three a second ([LS250]).

| Place and change | Display Y | Red excess (linear) | Verdict |
|---|---|---|---|
| Mid-wall, dark → red k .15 (A, B trip) | .002 → .013 | .004 → .051 | below both lines |
| Mid-wall, red .15 → .05 (B pulse) | .013 → .005 | .051 → .016 | below both lines |
| Mid-wall at k .18 (the limit not to pass) | .016 | .064 | at the red line: stay at .16 or less |
| Lens, dark → red .85 (A, B trip) | .003 → .106 | 0 → .48 | one flash per lamp per trip |
| Lens, red .85 → .30 (B pulse) | .106 → .022 | .48 → .10 | a red flash by the older metric, 0.4 a second per lens; passes by area (the whole lens row is under 3 % of a 10° field) |
| Wall, white → red (C front) | .49 → .013 | 0 → .051 | one-way: no flash |
| Door, goal head, C42 | .68 / .76, steady | 0 | never change |

Rules to build to: large surfaces at red k 0.16 or less; lens cores at 0.85, never above 1.0; fronts and pulses one way only; at most one change per place per trip (A, C) or one dip per place per 2.5 s (B), 12 pulses at most; Reduce Flashing removes every dark gap and the pulse; T9 counts them on screen, then the Harding FPA or PEAT capture (RELAY_PURSUIT_REDESIGN.md:855 already requires it before any WebGL or public build). Reconciling two drafts: `motion.txt` M9's "any red that dips and returns is a red flash whatever its level" is stricter than both definitions (a dim red wall dipping without a hue change is no flash); its rule that red changes only by slow one-way fades during a recede stays a sound choice and is what 5.7 does.

### 6.8 Risks to measure first

- **The Relay read.** A Red Ward failed at 2.97 : 1 because its red spots lit the Relay from above (`21_run_fix_1014.md`:94-104); our red strips sit 0.85 m above a 2.05 m Relay. Keep B's pools (they carried 7.68 : 1), measure at 2.05 m with protan, and if it falls short lower the red first, never the pools.
- **Hue cues die inside red:** no cue in a tripped room may rely on a red object (fire gear, the main, the card).
- **Phones:** the dark reds (62-68, 5, 6) may vanish on a phone in daylight or at low brightness; check on the phone tier.
- **Sound:** whether a siren-like partner joins the red (ETB leans on its siren; EGRESS's building alarm is silent) is 声音设计's and 叙事设计's call.

### 6.9 Recommendation and cost

**Recommended: B, ALARM PULSE**, the same look in the receding variant (5.7). **Fallback: A** if B's capture fails (same kit, the pulse off). **C** is the narrative alternative, walked in scene 4 (§10). Optional, for 系统设计: the run's first Run! (the teaching room) with the pulse off (A), B after; the look may climb with depth, it never shortens a warning.
**Honest flaw:** B relaxes the project's own "red is steady" rule and asks Red to accept a third tube in the strip approved on 11-03.
**Cost** (`red_light.txt` 8.7): 9 red tubes per 9-cell room (a 1.22 m tube of about 150 tris, one shared emissive material, no new texture), +0 Lights, +0 shadow maps, phone and WebGL tiers as Run! B. Code in `FrontRoomsRunRig.cs` only (游戏视觉): a per-strip red level beside the white, the trip as a front, the pulse state, the slow front, the tripped ambient tint, the Reduce Flashing variants. Nothing for the map or the director. Measured in the one combined test along the film's camera path (walk in, trip, run, look back, reset), one sheet per look: tripped median Y, saturated-red share, the Relay read at 2.05 m with protan, the door brightest in greyscale, T9, then FPA or PEAT.

## 7. What Run! means, and when the player meets it

From `story.txt` Parts B-E and its design notes 1-10. Red: "从叙事设计角度来说，这是一个什么样的一个信息？", "他在什么样的一个游戏的一个环境下，他会遇到 run 这个房间", "会不会是玩家他打开的门的次数过多了…被欺骗", "我不确定他会不会主动选择去进到这个房间，以及这个房间能给他什么样的奖励？它是一个纯惩罚性的房间吗？". One correction to the draft: `story.txt` B8 and C2 quote an older tier rule (a rise every 4 new zones or after 120 s without one); the code has dropped it, the tier rises only with unique cells (FrontRoomsLevelProfile.cs:313-330).

### 7.1 The message: "the building's first rule fails here, and its second rule saves you"

- **Frame (EGRESS, Red's pick 10-03):** Run! is a fire compartment already on the alarm. Stepping in trips one of the alarm's initiating devices, a detector room ("An initiating device trips in a zone", CALLED in `30_narrative.md` §2; "Full alarm, locked to your compartment" is the later CHASE state, when the Relay has seen you). The building does what an honest building does: it puts the compartment on emergency power and lights the way out to the one door at the end. The sweep is coming to account for whoever is inside. **Not a punishment room: a gamble with a visible price and a real prize** (7.4).
- **The message:** room one posts the building's two rules, "WALK, DO NOT RUN." and "CLOSE DOORS BEHIND YOU." (`30_narrative.md` §6-§7). Run! is the one room where the first rule fails (walking is the riskier way: LEVELS_AND_ENTITIES.md:65-69 says of the title stream's Run, where the Relay is always awake, "This makes walking unsafe but gives a committed sprint a chance to create space"; on the map a walker who keeps moving still shuts a 14-cell door before a Relay called from Away arrives, 3.2 (c)), and its end is the second rule: pull the goal door shut and you are out. No sign ever says RUN (`handoff/narrative.md`:48): the lights say it.
- **What each light means, in EGRESS words:** armed = the compartment on normal power; the trip = the panel puts it on emergency power and releases its held-open doors (in a Run! only the end's hold-open magnets let go, §5.7; the compartment frames ahead stay open, since nothing may slow the runner, §4.8 item 8); the red band and pulse running to the door = the alarm showing the way out (§6.6); the receding end = the alarm's hardware pulling the compartment's end away (§5.5's reading, literal for T4, a reading for T1-T3); the reset's white re-strike = restoral, seen from safety. The information is always about the place (this compartment is alarmed, its way out is that door, now), never about where the hunter is: "No light reacts to where the Relay is" (`10_run_directions.md` §1).
- **Option for 叙事设计 (no new string):** hang room one's placard again a few cells inside the corridor, so the player reads "WALK, DO NOT RUN." in the second the lamps tell them to go. As the player feels it: a cold, solemn irony. Cost: one placard prop. Flaw: some will obey and walk; walking is the riskier way to the same door, and the warning ladder still gives at least 3 s before a lock-on, touching distance (1.5 m) excepted (systems.json glossary, the G3 gate).

### 7.2 When the player meets it, and the door that deceives

**Red's two counts already exist.** "进入的区域过多" is close to the tier (unique cells walked; Run! is the tier-2 kind and its odds rise with tier). "打开的门的次数过多" is close to door noise: easing a door +1, a barge +8, a slam +12 Attention (RELAY :352), which already calls the Relay. Nothing in the game counts the doors a player opens (tools only, `today.txt` §5c).

**"被欺骗": two readings of Red's words** ("他在打开门进入这个门之后，他发现连接上的并不是之前的正常的这种房间，而是 run 房间"):
- (a) a door the player expects to be ordinary opens onto a Run! corridor ("not the normal kind of room");
- (b) a door that led to a normal room before now opens onto a Run! ("not the normal room it connected to before"), which today's rules forbid: trigger rooms are "placed by coordinate and stable through revisit shifts" (RELAY :683, :753).

| Option | As the player feels it | Rules | Verdict |
|---|---|---|---|
| **D1 · An honest door, a fooled habit** (reading a) | A jolt ("this is not the room I expected"), then a choice. The second time they read the window first | The door is a Run door: RN-F with set A's 15 x 15 in window, the shared tell (red-banded detector base, ALARM ZONE card) at the door, the corridor's light in the window and under the door. The trip lies beyond the door's swing and the 0.15 s barge carry, about 2.5 m in (系统设计 sets it): opening and stopping costs nothing; barging through at a sprint pays a call, exactly what the placard warned | **Recommended** (from T3, after one plain Run!) |
| D2 · A hidden door-count dial | Nothing: an invisible rule reads as luck | Fights the map's determinism; an unarmed room with a tell would be a "harmless detector prop", which the vocabulary forbids ("No harmless detector props anywhere", RELAY :372) | Not recommended |
| D3 · A forced trap (the door trips it, or shuts behind you) | Betrayal with no choice; being caught ends the run (C22) | Breaks "readable before you commit", "the room arms 1.5 m inside", "No route is ever a forced call" (RELAY :356-373) | Never |
| D4 · The changed door (reading b) | "The building changed this door": on a revisit, a door used before now has the square Run window and the tell | Needs 系统设计 to bend the stability rule and 叙事设计 to own a building that lies about itself (`narrative/40` §1: "The building is honest about itself"); this line keeps "the building lying" for deep floors (`10` L0-7) | Only if Red meant (b): T5, floor ≥ B3, after two honest Run! rooms, at most once a floor (`pacing.txt` E3) |

Precedents: Dark Souls' Mimic lies, but three tells are always there (its chain, its breathing, its paler colour) and a thrown Lloyd's Talisman opens it without waking it; fooled once, the player learns to look ([LS235]). Hades marks each exit door with the kind of chamber behind it ([LS228]): doors that tell the truth are what make one surprising door legible, and set A's windows already do that here. The Exit 8's deceptions are fair because the rules are posted in room one and the corridor is learned by heart ([LS214]). The Backrooms canon already holds Red's idea with a lying sign: "Doors marked with a red exit sign lead here" ([LS232]); C41 forbids the lying sign, so ours keeps the surprise and moves the lie from the sign to the player's hope: the far door's glowing square promises a way out and is only the next compartment (`story.txt` E2).
D1's bonus: a Run! met from its goal end today makes the player run toward an arch with no door to shut (the room arms from either opening); with a Run door at both ends the race is the same whichever end is met first (关卡设计 to confirm how the lamps behave in that reverse case).
D1's flaw: a sprinting player barges doors faster than the project's 0.6 s reaction delay (RELAY :239), so the Run door must be readable from at least 5 m, not only 2 m (3.3 m covered at a sprint before acting).

**When, in a run** (with §3.4): never before the first send, in the first zone, within 15 cells of the start door or in an arrival zone; from tier 2; the first Run! plain and arch-entered; a door entrance, a ramp or a receding end only after one plain Run! has been met; never two of them in one room. Red's 10-30 rule holds: no chase while the player is still learning the game.

### 7.3 Voluntary or forced

| Entry | As the player feels it | Verdict |
|---|---|---|
| V1 · At an arch, voluntary (as built) | Temptation, dread, "my run". With no visible prize, careful players skip it, and the calm route makes skipping cheap (+30 m) | keep; needs 7.4's prize |
| V2 · Revealed behind a door, voluntary commit (D1) | The surprise Red asked for, then the same choice | recommended alongside V1 |
| V3 · Forced | Betrayal; it costs the run | never |
| V4 · Chosen under pursuit (exists, nothing to build) | A desperate gamble that pays: in a chase a device trip is only a noise, and the corridor is a long straight to a door you can shut | the strongest use of Run!; needs the hunter never to arrive from the goal side (§4.8 item 4); see §4.7 for the receding variant |

Precedents for a voluntary trigger: Lethal Company's Apparatus (a visible prize, a known price: pulling it turns the lights out and is said to spawn monsters; the guide's advice is to prepare first, hands empty, a teammate watching from the ship, then run for the nearest exit) ([LS237]); Pacific Drive (the player chooses when the storm starts; the escape and the reward are the same act) ([LS236]).

### 7.4 Reward, or pure punishment

| Option | As the player feels it | Status | Verdict |
|---|---|---|---|
| R0 · Pure punishment | Forced: the building cheated. Voluntary: "never go there", and the room becomes scenery | — | not recommended. Spelunky's ghost is lethal, but gems it touches turn to diamonds: "risky business, but a good way to make cash" ([LS234]) |
| R1 · Distance | "Worth it when I'm in a hurry": a straight through two chunks against a calm route 30 m longer | exists | keep; weak alone |
| **R2 · Escape and calm** | "If I make it, it loses me", then a breath | exists in the director: a goal door shut before the Relay sees or hears it is a wall to it (door licence); it sweeps the empty compartment, withdraws, and RELAX follows, 100 s at T1 and 60 s at T5 (systems.json glossary; 90 s at T2, where Run! starts: `relaxSeconds` 100 / 90 / 80 / 70 / 60, RELAY :286) | **recommended core**; make it legible (below) |
| R3 · A rest beyond the door | The strongest prize: a guaranteed rest earned by a sprint | needs dead-circuit rooms (systems.json L3) | later, rare (at most once a floor) |
| R4 · Knowledge beyond the door | A posted evacuation plan, a view (SP5's dock and the night street) | dressing | as dressing only; never marks trigger rooms, never the only place of the key |
| R5 · A hidden system relief | Nothing, unless shown | — | no |

**Making R2 legible without a HUD:** behind the goal door the player can peek back through its square window (peeks are live, C36: the Relay cannot see through it but can hear the player). They watch it search the red corridor while they hold still; then, when it withdraws, the corridor re-strikes white (the reset, v2 with director.Away). It reports a state the player has just survived, not a position; 系统设计 confirms that under rule 11. A bot round, proposed here, measures how often a sprint that reaches the door also escapes (A54 (c) schedules only the 100-seed Run! check with runRooms on; this would ride on it).
Precedents for priced rooms: The Binding of Isaac's curse-room door wears spikes and hurts on the way in and out; Hades' Infernal Gates and Dead Cells' timed doors state their price or clock on the door ([LS238]; those two seen in search only). In all three, failing costs the bonus, not the run; here failing costs the run (C22), which is why Run! stays optional and winnable (§4.8, 7.6).

### 7.5 Signals before it (part of the place, constant, never reacting to the hunter, never on the HUD)

- **S0 · room one, minutes before:** the placard's two rules make running through doors the risky habit and shutting doors the safe one, so D1's surprise and R2's escape are both taught before the first Run! exists.
- **S1 · approach, 5-15 m:** the Run door's square window and the light in it and under the door (set A: the window's shape tells where a door leads, systems.json A54 (a)); from an arch, the corridor and the glowing far door themselves; in a receding room, the head's track (§5.1).
- **S2 · threshold, 2 m:** the red-banded detector base and the ALARM ZONE card at every entrance, "readable in a dark room"; the ELECTRICAL ROOM plate (`narrative/95_world_text_localization.md`:56).
- **S3 · before the trip:** the armed corridor's hum (声音设计's; a property of the place).
- **S4 · the trip:** the devices' own local sounds only, no building bell (C07). Open for 声音设计 and 叙事设计: EGRESS's lock-on cue is a door-holder thunk plus one damped chime ("it sees you"); at a Run! trip the hunter has not seen the player, so either the thunk plays without the chime or the trip gets its own sound; thunk plus chime would lie about sight.
- **S5 · the clock:** the one-way lamps of §6 (the band and pulse ahead; nothing that tracks the hunter).

### 7.6 Fairness rules (`story.txt` E7; with §4.8 for the receding variant)

F1 telegraph readable before commit, in the taught vocabulary, from at least 5 m at a run; F2 a step back (the reveal never trips); F3 winnable from the trip with a full bar (the late bands of §3.2 are the stated exceptions, by depth); F4 the hunter never arrives ahead; F5 never locked, never the only route, never the key alone; F6 the same tempo every time, never reacting to the hunter; F7 no chains (a Run! goal door never opens into another trigger room's armed area or a dead end); F8 a death that teaches (the player sees why: Miyazaki's test, "when players are killed, and they can understand why", [LS235]); F9 order (plain first); F10 rarity (at most one a floor). 系统设计 adopts them into RELAY §6; 关卡设计's validator checks F4, F5 and F7.

### 7.7 Red's questions, one line each

- **The message** ("这是一个什么样的一个信息"): this compartment is on the alarm; its way out is that door; go now. The building's first rule fails here and its second saves you.
- **When** ("在什么样的一个游戏的一个环境下"): after the first send, from tier 2 (about 150 new cells), never in the first zone or near the start door; first a short plain one, later a ramp, a door, a receding end.
- **Too many doors, too many zones, deceived** ("打开的门的次数过多…进入的区域过多…被欺骗"): the two counts exist as the tier and door noise; the deception works as an honest door that fools a habit, never a forced trap; the changed door only if Red meant it, and only on deep floors.
- **Will they choose to enter** ("会不会主动选择去进到这个房间"): yes, when it pays: under pursuit, in a hurry, and for the escape and calm it buys.
- **Pure punishment** ("它是一个纯惩罚性的房间吗"): no; a gamble with a visible price and a real prize.

## 8. 给 Red（中文，按你问的顺序）

**你怎么回的（11 月 3 日 23:18）：** 下面 9 个决定按你问的顺序排：每个先说背景，再说选项玩起来的感觉和代价，最后是推荐和它诚实的缺点。你回了"全部按推荐"。按这里事先写好的读法，就是同意每一条的推荐，包括两件会改动你以前认可过的东西：会退的房间在 9 米那道防火门框触发（决定 4），和 Run 门入口（决定 8c 的理解一）。决定 7 按 RESET 那一段理解：警报解除后，白光从入口一格一格亮回来。只有一件还没定：在你 11 月 3 日说"就这样"的 R-B 灯架里加第三根暗红灯管（决定 6）。灯具适配先做一张图给你看：今天的灯架、加了暗红灯管的同一个灯架、红灯管在警报时亮起来的样子，三个并排，同一个镜头、同一个曝光，玩家眼高 1.62 米看一张、近看一张；你说可以才做，在那之前灯架不改。下面每条推荐都标了"11 月 3 日已定"，背景、选项和理由照原样留着。新结构照规则 5b，先在场景 4 的 SP7 里走过、你说可以，才进游戏。英文记录在 §0.1，发给各条线的任务在 §9。

### 决定 1 · 出现的次序：哪种关卡、哪种结构，什么时候出现

**背景：** 你问"不同类型的 level 它出现的一个次序"。这条线说的关卡有五种：Level 0、Office、Run!、泳池、井（穿过整栋楼的那口竖井，场景 4 的 SP2）。现在游戏里只有 Level 0 和 Office 两种，是每一层楼里混在一起的两种区域；Run! 在游戏里是这些区域里的一个房间，不是单独的一层；泳池和井还没有进生成器。今天 Run! 是每个区域单独掷骰子决定的。

**两把钥匙：** "档" = 这一局走过的新格子数，每 150 格升一档，最多 5 档，只升不降，玩家看不到，它管 Run!、坡道和下沉区什么时候出现；"楼层"（LEVEL 0、B1、B2……）管会动的结构。两个都写了的，两个都要满足：在 LEVEL 0 上就算到了第 4 档，也不会有会动的结构。

**两种发法（玩家感受）：**
- 今天的掷骰子：每个区域单独掷，越深越容易掷中；一层里可能连着出两个 Run!，第二个就没那么吓人了。
- 每层一副牌（推荐，11 月 3 日已定）：一层最多一个，牌里有"这次没有"，同一种不连着两层出；每次遇到都有一点不一样。

**推荐的次序（11 月 3 日已定）：**
- 第 1 档（前约 3 分钟）：没有 Run!；只有普通坡道（1:12），每个 Level 0 区域最多一个地形模块。
- 第 2 档（约 3-5.5 分钟）：这一局第一个 Run!：7 格、普通、拱门入口；之后 9 格普通。加下沉休息区、办公坑（1.2 米）。
- 第 3 档（约 5.5-8 分钟）：每层的 Run! 从一副牌里抽：9 格普通 / 9 格下坡 / 9 格门入口 / 这次没有。加错层套间（2.4 米，两条路上去）。下坡要等关卡设计做出"每格地面高度"（决定 2）。
- 第 4 档（约 8 分钟起）：牌是 12 格普通 / 9 格下坡 / 9 格门入口 / 9→12 会退（至少 B2 层，不够深就是 12 格普通）/ 这次没有。
- 第 5 档（约 11 分钟起）：牌是 12 或 14 格普通（14 格放得下才放）/ 9 格下坡 / 9 格门入口 / 9→12 或 9→14 会退（14 要至少 B3 层）/ 变了的门（只有你在 8c 选了"理解二"才有；8c 已定理解一，这张牌不放）/ 这次没有。
- 每层最多一个 Run!；同一种不连着两层出，会退的也不连着两层出；一间里最多一种变化（坡道、会退、门入口三选一）；这一局先遇到一个普通的，才有变化；Run! 所在的区域和它尽头门后面的区域不放别的触发房间，关上门就能喘口气。分钟数是一个机器人跑出来的，真人会更慢。

**还没定的（不在这一页）：** 泳池和井排在一局的第几层。这条线给场景 4 的方案 SP2 把它们叠在一起：最上面是 Level 0，往下是办公层、Run! 设备层，大约 17 米下面是泳池；那是为了让你判断深度，不是一局里的次序。由关卡设计提出、你来选。（《Escape the Backrooms》把它的 Run 关放在泳池关之后、最后一关之前。）

**代价：** 关卡设计改生成器：把每个区域掷骰子改成每层一副牌，再加一个"这一局已经遇到过普通 Run!"的记号。

**缺点：** 每层最多一个，Run! 会比今天少。这是有意的：追逐重复多了就不吓人（做 Amnesia 的 Thomas Grip 的经验）。

### 决定 2 · 坡道（ramp）什么时候出现、多长

**背景：** "什么时候应该出现 ramp，究竟有多长"有两种读法：一是 Run! 的下坡版本（SP5 那条 1:8 的坡道）；二是语音把"run"听成了"ramp"。第二种在决定 3 回答了，这里回答第一种。

**推荐（11 月 3 日已定）：** 先在安静的区域出现普通坡道（1:12，每段最长约 9 米、最多升高 0.76 米，段与段之间有 1.5 米的平台），让玩家先认识坡道。Run! 里的下坡从第 3 档（大约 5 分半）开始：1:8，最长 24 米，最多下降 3 米（按地图 0.3 米一级的地面高度算是 10 级，主要是坡，不是 10 级楼梯），永远不到一层楼高，因为往下一层只走 STAIR A。走廊更长时，多出来的部分保持水平。

**代价和缺点：** 游戏里现在每一格地面都是平的，要等关卡设计做"每格地面高度"才能进游戏。在那之前只能在场景 4 的 SP5 里走。

### 决定 3 · Run! 的长度：早期短、晚期长，最长多少，能不能无限长

**背景：** 现在 Run! 只有一种长度：9 格，27 米（一格 3 米）。这个长度是按冲刺算的：体力条满格能冲 5 秒，每秒 5.5 米，正好 27.5 米。所以今天的 Run! 是"一口气刚好跑到门口，还剩 0.4 秒"。

**选项（玩家感受）：**
- 7 格 21 米：一口气跑完还剩 1.5 秒。适合教学，安全感强。
- 9 格 27 米：今天的房间，一口气刚好。
- 12 格 36 米：体力在门前 7 米用完，门就在眼前，要喘一下再冲。第一次让玩家觉得"一口气不够"。
- 14 格 42 米：要冲两段，中间被迫喘气。

**推荐（11 月 3 日已定）：** 按深度给长度。一局里第一个 Run! 是 7 格；之后 9 格；第 4 档（按机器人的速度大约 8 分钟后）起出现 12 格；第 5 档最长 14 格（地图放得下才放）。长度只看深度，永远不看玩家表现好坏。这里的"早期""晚期"指一局里的早晚（走得多深），不是新玩家的头几局：每一局的第一个 Run! 都是 7 格。

**为什么最长 14 格（42 米）：** 三条限制正好都卡在这里。一是地图分块：Run! 只能跨一条区块边界，最多 14 格。二是视距：相机远裁剪面 46 米，14 格时站在入口外 2 米还能看见尽头的门，15 格就被裁掉了。三是时间：Relay 从地图外被叫来，最早约 20 秒才到；14 格走路 12.6 秒，走得完。

**能不能无限长：** 作为一条真的走廊不行。但能"感觉"无限长几秒钟：就是决定 4 的"门往后退"。另外两种（推开尽头的门又回到同一条走廊；地图外一条无限生成的走廊）都不适合 Run!，原因在 §3.3。

**代价：** 关卡设计改生成器：长度从常量变成按档位的表（次序和发牌见决定 1）。12 和 14 格要先在 100 个种子上测能放下多少。

**缺点：** 7 格也许太短，灯光的"隧道感"出不来，要你在场景 4 走过才知道。

### 决定 4 · 门往后退：怎么退

**背景：** 你想要：走到原本长度的三分之一，门往后退，越退越远，而且是模型本身在动，不只是镜头。我们查到最接近的：《鬼驱人》（Poltergeist，1982）里妈妈跑向孩子的房门，走廊被拉长，门一直在画面正中、一直亮着；《寂静岭 3》里一面墙在追逐中移动一次；《Control》的烟灰缸迷宫里建筑在玩家眼前重组。没有找到哪个第一人称游戏在追逐中让远处的门作为模型往后退：这是新的组合，要走过才知道好不好。（《小小梦魇 2》里走不到的那扇门，是被人物拉回来，不是走廊变长；我们读到的资料都没说走廊会伸长。）你说的《小小梦魇》里的"隧道"，我们没找到能对上的具体场景；如果你记得是哪一作、哪一段，告诉我们，我们再去查。

**四种做法（玩家感受）：**
- M1 拉长（推荐，11 月 3 日已定）：门往后退一次，然后停住，玩家最后能跑到。
- M2 循环：推开尽头的门又回到同一条走廊。不适合 Run!：Relay 正被叫到这里，多绕一圈等于送死；"关上门就安全"这条规则也会失效。
- M3 彼岸：门退了，这一间里永远到不了；旁边一扇一直都在的门亮起来，从那里出去。适合以后做成故事的回报（很后期终于走到那扇门）。现在不推荐：它和你 10 月 31 日定的规则冲突（Run! 尽头那扇门就是出口、永远不锁），而且门退走的那一刻，玩家会有一瞬间不知道出口在哪。
- M4 地图外的无尽走廊：能真的无限，但 Relay 跟不进去，等于安全区。只适合某一层一次性的特殊下楼方式，不是 Run!。

**推荐的 M1 具体是（11 月 3 日已定）：**
- 只在深层：第 4 档并且至少 B2 层，从 9 格退到 12 格；第 5 档并且至少 B3 层，退到 14 格。一局里第一个 Run! 永远不退；不和坡道、门入口出现在同一间。
- 触发点就是你说的三分之一：27 米的三分之一是 9 米，正好是第一道防火门框的位置（这道框要新做）。玩家面朝走廊时才开始，不会在背后偷偷动；一直不看的话，走到 18 米也会开始。
- 关键改动：这种房间的警报不在入口 1.5 米触发，而在这道 9 米的门框触发。跨过门框的那一刻，警报触发、门开始退，是同一件事："这一段防火分区进入警报，尽头被拉走了。"这样从触发开始，满体力冲刺正好够到 12 格的门（还剩 0.1 秒），和今天的承诺一样；14 格要多跑 5 米多，是深层的难度。另一种是照今天在入口 1.5 米就触发，跨过 9 米时门才开始退：最吓人（体力在逃走的门前约 7 米或 13 米用完），但只有 Relay 是从地图外被叫来时才公平；要保证这一点，门就得"看" Relay 在哪，违反规则 11，所以不推荐。
- 怎么退：先猛地一缩，一下退 2 格（6 米），再一格一格退，最后一下很重地落位。"越退越远"要说清楚：门的位置一直往后退（从 27 米退到 36 米或 42 米，12 格分 2 下，14 格分 4 下）；可是离你的距离只在第一下猛缩时变远（走路的人从 18 米拉到将近 20 米），之后人走得比门退得快，最后落位时走路的人离门约 16 米（12 格）或 13.6 米（14 格）；只有按住 Shift 慢走的人会看到门一直越来越远（14 格时从 21.8 米到 24.3 米）。冲刺的人如果靠近到 12 米以内，剩下的距离就以每秒 6 米（比冲刺快一点）一口气滑到底：玩家永远追不上一扇正在动的门，因为门不锁，半路抓住就会露出后面还没出现的走廊。
- 另一种退法（你来选；11 月 3 日按推荐定了一步一步退，不用这种）：一口气滑走。门在预兆之后以每秒 6 米连续退到底，12 格用 1.5 秒，14 格用 2.5 秒（研究阶段的另一个方案）。这样退的时候门对谁都在变远：走路的人从 18 米拉到约 21 米（12 格）或 24 米（14 格），冲刺的人也看到它每秒再远约 0.5 米，然后门停住。代价：决定 5 里 T2 的砖块每一下至少要 0.8 秒才看得清拆开再砌回，一口气滑走时整面墙只能像 T1 一样整体滑走，你要的"重组"几乎看不到。所以在这个速度下，"越退越远"和"重组"要二选一：我们推荐一步一步退（保住重组和每一下的节拍）；如果你更想要"越退越远"，就选一口气滑走，模型用决定 5 的 T1。
- 怎么结束：最后一下落位后，门是真门，能推开；这一层里这条走廊一直保持变长后的样子；门停过的地方留下一圈干净的"印子"；不会再退第二次。警报解除时，你喜欢的白光从入口一盏盏亮到尽头，覆盖整条变长的走廊。
- 绝不读 Relay：门退不退、什么时候退，只看玩家走到哪、看向哪、离门多远，永远不看 Relay 在哪（规则 11）。

**代价：** 关卡设计一开始就按最长长度规划走廊，后面几格先藏起来；尽头的墙和门做成能整体移动的部件；每次落位时打开那一格的碰撞和 Relay 路网。游戏视觉做模型和灯光（决定 5、6）。

**诚实的缺点：**
- 这种房间在 9 米才触发，和别的 Run! 不一样。玩家会学到"会退的房间触发得晚"（能学会，所以公平）。
- 被追着冲进一间会退的房间最吃亏：按我们的计算，比冲进普通 9 格房间少 2 到 5 米的领先距离。推荐（11 月 3 日已定）先让系统设计用机器人测一轮：如果领先 8 米冲进去仍然逃得掉，就照现在做；逃不掉，就改成"只有走着跨过三分之一时才退"（冲刺进来的人得到普通房间）。
- 从另一头先到的人，会在门的位置看到一个封死的门洞。这是关卡设计要处理的细节；我们觉得一个"封死的门洞"本身就是安静、诚实的异样。

### 决定 5 · 后退的 3D 模型动画（T1 后退 / T2 重组 / T3 伸缩 / T4 折叠）

**背景：** 你说"不只是 motion transition，也是模型上面……像那个可破坏的门……重组或者向后退避"。四种都用同一个"头"：尽头的墙、门、门上的小方窗、推杆、门上方跟着走的应急灯，挂在天花板两角的轨道上，用电磁门吸固定。门吸松开就是每一步的预兆："咔哒"两声，墙往玩家这边一震（1.5 厘米）。从入口就能看见天花板角上的轨道，细心的人能猜到"这面墙会动"。

**选项（玩家感受）：**
- **T1 后退：** 整面尽头墙像舞台车一样笔直往后滑，原地留下一圈更干净的油漆和没磨过的地蜡，像柜子挪走后墙上的印子。最好读，最"机械"，但模型感最弱。所有平台都用它做底；网页版只用它，手机在 T2 撑不住时也退回它。
- **T2 重组（推荐，11 月 3 日已定；电脑和 iPad；手机上砖块合并成半层的长条，约 28 块，没有粉末）：** 尽头的砌块墙从上往下一层层拆开，砖块沿两侧墙和天花板分成左右两股往后流；中间那扇门不拆，亮着、立着，自己往后滑；到了新位置，砖块再从下往上一层层落回去把门围住，灰浆的粉末在门灯里发光。就是可破坏门那套做法（预先做好碎块、提前烘焙路径、缝里掉粉）倒着放。
- **T3 伸缩：** 最后一格本来就是五层套在一起的钢套筒，像登机桥；触发后一节节伸出来，每节停下时"当"一声，每 3 米一圈亮着的钢环。1958 年就有的真机器，只是不该出现在办公楼里。
- **T4 折叠：** 两侧墙里藏着防火折叠门，触发后从墙洞里拉出两道波纹钢板墙，跟着尽头往后展开。最像 1990 年的真实消防设备（警报真的会让这种门动），但细密的条纹一起动，是最典型的光敏风险，要先过测试；墙也变成了钢板。

**代价（电脑版，估计）：** T1 约 9 千三角面、8 个绘制；在这个"头"之上，T2 再加约 5.4 千面、1-3 个绘制；T3 再加约 8.5 千面、15 个绘制；T4 再加约 1.1 万面、4 个绘制；新贴图都在 4 MB 以内。

**推荐（11 月 3 日已定）：** T1 的"头"所有平台都用；电脑和 iPad 上加 T2 的重组，手机用半层长条的简化重组，网页版只用 T1。深层如果想让走廊本身动，用 T3。T4 等光敏测试通过再说。

**诚实的缺点：** T2 看不到机器，是"大楼自己在重新排列"，偏超自然；砖块一起动属于"运动图案"，要过光敏测试；冲刺的人只看得到第一下重组，后面变成整面墙滑走。如果你更想要"看得懂的真机器"，就选 T1，深层再加 T3。T3 和 T4 会让走廊最后一段的材料或截面变掉，可能碰到你"不能改变房间性质"的那条（G1/G2）。

### 决定 6 · 红色的氛围

**背景：** 今天的 Run! 触发后一点红光都没有，只有暖色的应急光池。你说的是"后世同人的这个游戏"，没说是哪一个。我们推断是《Escape the Backrooms》：天花板上一排排红灯朝远处排过去，还有不停的警报声（那一关从头到尾都是红的，不是触发后才变红）；如果你想的是别的游戏，告诉我们是哪一个，我们再去查。我们以前做过"红色病房"版本（A），量下来 Relay 和背景的对比只有 2.97:1（要求 4:1）：红色铺满画面就看不清 Relay，所以当时选了 B。这次的做法：红色只做第二层，暖色光池照样切开它，尽头的门永远最亮。

**红色从哪来：** 每个 R-B 灯架里加一根红色灯管，接在应急电池回路上。平时不亮：两根白灯管中间夹一根暗红色的灯管，本身就是"这条走廊有警报回路"的提示；警报时才亮。不用 EXIT 灯（你定过 Run! 里不放 EXIT），也不用调色滤镜。1989 年已经有红色荧光灯管（暗房用）和电池供电的荧光灯；把它接到警报上是电影的做法，我们是有意这么选。

**三种灯光方案（玩家感受）：**
- **A 警报回路：** 一踏进去，一道暗带从脚下跑向尽头的门，暗带后面每根灯管变成深红，应急灯打出暖色光池；2 秒后整条走廊静止。最庄严；危险感只在那两秒。
- **B 警报脉冲（推荐，11 月 3 日已定）：** 在 A 的基础上，每 2.5 秒有一个暗脉冲沿灯管一根根暗下去（降到约三分之一亮度，不是全灭）又亮回来，跑向尽头的门；地上的暖色光池、尽头的门和门后的光一直不变；每分钟 24 次，比楼顶航空障碍灯常见的每分钟 30 次还慢，最多 12 次。它是这栋楼"无声警报"的心跳，也就是你喜欢的"一个一个熄灭、一个一个亮"在危险时的红色版本。
- **C 红潮：** 红色从你进来的入口以走路的速度一盏一盏追上来，前方还是白光。走路，红色一直在你身后 2.3 米；跑，就把它甩开。"WALK, DO NOT RUN"在这里失效。缺点：跑的时候眼前多半是白光；玩家可能把红潮当成追兵的位置，那就等于雷达，要拿掉。
- **按你说的"庄严"和"危险"来比：** A 最庄严，危险只靠颜色；B 也庄严，危险靠颜色加节拍；C 庄严一般，危险靠颜色加"从后面追上来"。

**你喜欢的效果不会丢：** 警报解除时，白光从入口一盏盏重新亮起（今天已经做好的），意思是"大楼放过你了"。红色的暗带和脉冲是它的"危险版双胞胎"，方向都朝着出口那扇门，就像机场跑道灯一串串朝跑道方向闪，意思是"往这边走"。

**光敏：** 墙面的红光很暗，脉冲的明暗变化低于 WCAG 的闪光线；灯管本身按旧的红闪标准算"红闪"，但整排灯管不到 10 度视野的 3%，按面积通过；"减少闪烁"模式下没有暗带、没有脉冲。最后要用 Harding FPA 或 PEAT 实测。

**代价：** 每间 9 根红灯管（每根约 150 面，一个共用材质，没有新贴图），不加新灯光、不加阴影；代码只在 Run! 的灯光脚本里（游戏视觉的）。

**推荐（11 月 3 日已定，红灯管除外）：** B；如果 B 的光敏实测不过，就用 A（同一套，关掉脉冲）。C 在场景 4 里可以切换着试。第三根红灯管还没定：先看图，你说可以才做（§9 灯具适配 1）。

**诚实的缺点：** B 放宽了我们自己"红光必须稳定"的规则；而且要在你 11 月 3 日说"就这样"的 R-B 灯架里多加一根红灯管。所以先给你看一张"平时状态下带一根暗红灯管的灯架"，你同意了再做。

### 决定 7 · 你喜欢的那段灯光是哪一段（只要确认）

你说"我很喜欢……一个一个灯光熄灭，一个一个灯光在亮"。10 月 31 日那段 28.5 秒的视频里有两段可能是：
- RESET 段（17.9-22.5 秒）：警报解除，白光从入口一格一格亮到尽头，像一个脉冲穿过隧道。我们认为是这段。**11 月 3 日已定：就是这段。**
- RUN 段（10.4-15.9 秒）：冲刺穿过一个个暖色光池。

不管是哪段，决定 6 的三种灯光方案都保留它。只是确认一下，免得理解反了。

### 决定 8 · Run! 的意义、什么时候遇到、"被骗的门"、会不会主动进去、奖励还是惩罚（8a-8e 可以分开回）

**背景：** 现在 Run! 一进去就叫来 Relay，唯一的"奖励"是一条直路通向一扇能关的门。理性的玩家会绕开它。

**8a 意义（推荐，11 月 3 日已定）：** 在我们的 EGRESS 故事里，Run! 不是惩罚房间，而是一段已经接上警报的防火分区。一踏进去，大楼把这一段切到应急电源，灯一路指向尽头那扇门，告诉你出口在哪；警报派来的东西会来"清点"还留在里面的人。它传达的信息是：大楼的第一条规矩"WALK, DO NOT RUN"在这里失效，第二条规矩"CLOSE DOORS BEHIND YOU"在尽头救你。不需要新的文字；可以选择在走廊里几格的地方再挂一次第一个房间的告示牌，形成一种冷冷的讽刺。

**8b 什么时候遇到（11 月 3 日已定）：** Relay 第一次被派出之后，从第 2 档（大约走过 150 个新格子）开始；不在第一个区域，不在起点门附近。先遇到一个短的普通 Run!，之后才有坡道、门入口、会退的门（次序见决定 1）。你说的"打开的门太多、进入的区域太多"，游戏里已经有对应：档位就是走过的新格子数；开门的声音会累积 Relay 的注意力。所以你的想法成立，只是用"走过的新地方"来数，不数开门次数：这一局走过约 300 个新格子（第 3 档）、并且已经遇到过一个普通 Run! 之后，一扇门才可能把你"骗"进 Run!（见 8c 的理解一）。

**8c "被欺骗的门"有两种理解：**
- 理解一（推荐，11 月 3 日已定）：玩家以为推开的是普通房间，门后却是 Run! 走廊。门本身不说谎：它有 Run 门特有的 15×15 英寸小方窗，门边有红色探测器底座和 ALARM ZONE 牌子，门缝下透出走廊的光。被骗的只是"不看门就冲进去"的习惯。触发线在门里面大约 2.5 米，开门看见了还可以退出去；但如果冲刺撞门进去，就会触发，正好应了第一条规矩。第二次玩家就会先看窗。从第 3 档开始出现。
- 理解二（11 月 3 日没有选，不做）：一扇以前通向普通房间的门，回来时变成通向 Run!（大楼改了这扇门）。这违反现在"触发房间位置固定、重访不变"的规则，也等于让大楼"说谎"。如果你指的是这个，可以只放在很深的层（第 5 档、B3 以后，见过两次普通 Run! 之后，每层最多一次），门上同样换成 Run 门的小方窗，让细心的人看得出来。

**不推荐：** 偷偷数开门次数来决定（玩家只会觉得是运气）；强制陷阱（玩家没有选择就被卷进追逐，而被抓就结束整局）。

**8d 会不会主动进去（11 月 3 日已定）：** 会，当它值得的时候。被追的时候冲进去最刺激（追逐中触发只算一个噪音，换来一条直路和一扇能关的门），今天的规则已经支持，大多数玩家也会先这样发现它；赶时间的时候它是一条近路（绕开要多走约 30 米）。没被追、也不赶时间的谨慎玩家多半会绕开：看不到好处，绕开又只多走 30 米。这没关系，Run! 一直是可选的；要让他们也愿意进去，就靠下面 8e 的奖励。

**8e 奖励（推荐，11 月 3 日已定）：** 冲到尽头，在它看见或听见你之前把门关上，那扇门对它就是一面墙。它会在空走廊里搜一圈然后撤走，整层安静一段时间（越深越短：第 2 档 90 秒，到第 5 档 60 秒）。你可以隔着门上的小方窗看它找不到你（它看不见你，但听得见，所以要安静），然后走廊的灯恢复白色。以后有"断电安全房"时，可以放在少数 Run! 的尽头，作为更大的奖励。

**代价：** 大多是已有的系统。门入口要关卡设计和游戏视觉做；"隔窗看着它撤走"要系统设计确认不算雷达。

**诚实的缺点：** 奖励到底成不成立（冲到门口的人有多少真能甩掉它）还没测，要机器人跑一轮。你的第二条消息停在"就比如说玩家进入到这个"，后面的问题我们不知道；如果还有，请补一句。

### 决定 9 · 先在场景 4 走一遍（SP7）

**背景：** 你定过：所有新结构先放进场景 4，你走过再决定哪些进游戏。

**推荐（11 月 3 日已定）：** 在场景 4 加一个区域 SP7"会退的门"：一条按 Run! B 做的 14 格走廊。可以用按键切换长度（7、9、12、14 格，普通或会退）、模型（T1 或 T2）、红色方案（今天的、A、B、C）、入口（拱门或 Run 门）、"减少闪烁"开关，还有重置。走廊里放一个不会动的 Relay 替身，用来检查红光下能不能看清它。

**做不到的：** 场景 4 里没有会动的 Relay，所以"追逐公不公平"要在游戏里用机器人测，场景 4 里感受不到；也没有声音。

**代价和缺点：** 总控的结构工作流按 §10 搭，灰盒、结构专用资源、不进游戏。红光状态和后退动画都是新东西，场景 4 里先是灰盒版本，最终效果要等游戏视觉正式做。

### 另外告诉你（不需要决定）

- 70 里的 SP5 原来和你之前的三个决定冲突：放了 EXIT 灯（你定过 Run! 里没有 EXIT）；尽头门用了已经退役的 RN-X；灯具是旧的灯罩，不是你选的 R-B 灯架和 TA+TB 应急灯。11 月 3 日 22:5x 已改好 70：去掉 EXIT，换成 RN-F 门（带 F5 小方窗、永不上锁），平时 R-B 灯架、触发后 TA 和 TB 应急灯。
- 和关卡设计怎么"精密地融合"：Run! 的每一条规则都写成生成器能直接用的整数：长度按整格（7、9、12、14，最多 14）；会退的从 9 格退到 12 或 14 格，触发在第 3 格末（9 米），门每次都停在整格上；坡道的高度按地图 0.3 米一级算；每层最多一个、按档发牌（§3.6 那张表）。这些由关卡设计写进生成器，我们只提方案，经过总控。分工：游戏视觉（加灯具适配）做尽头会动的"头"、T2 的砖块、红灯管和灯光状态；关卡设计做藏起来的格子、碰撞和 Relay 路网；系统设计定出现的门槛、9 米触发和奖励的规则；叙事设计定意义和文字；声音设计只接事件。
- 你说得对，Run! 还没进可玩的版本：Run! B 已经做好、合进了主工程，但总开关（runRooms）默认是关的，正常游戏里遇不到，还没有玩家走过；关卡设计要等合并测试之后才打开。所以"玩家会不会主动进去"还没有人真的试过：场景 4 只能看样子，机器人只能测逃不逃得掉，愿不愿意进去要等打开以后的试玩。
- 红色灯管、门入口、9 米触发这三件事会改动你以前认可过的东西。后两件回"全部按推荐"就算同意；红色灯管先给你看一张图再定。11 月 3 日你回了"全部按推荐"：门入口和 9 米触发已同意；红色灯管的图交给灯具适配做（§9）。

## 9. Asks to the owning lines (cards, through 总控)

**Final, 11-03.** Red answered §8 at 23:18 ("全部按推荐", §0.1), so every card below is final and can be sent now; the parent session routes them through 总控 (this page sent nothing). Each card names its owner (files by RULES rule 3; 叙事设计's strings and 系统设计's rules by their own docs), what to do, the decision it rests on and the section that argues it. Red has decided what; each owner decides how. **One card waits for Red: 灯具适配 1**, the red tube's still (WAIT-RED); every item that needs the tube (游戏视觉 3, the red checks in 游戏视觉 5, 系统设计 6, the tube hum in 声音设计 2) waits for Red's yes on it. New structures land inert: under rule 5b the receding end, the Run door entrance and the red looks enter the game only after Red has walked SP7 in scene 4 and said so. Measurements (the fit rates, the bot rounds) start at once.

**Answers from the lines, 11-04** (cross-session messages to this line; the files they name are the record):
- **叙事设计**, `research/narrative/96_run_room_meaning.md` (no new strings): card 1 signed, and room one's placard is *not* hung again inside the corridor ("a plan posted where obeying it costs you would teach players to distrust every plan"; the ALARM ZONE card is the room's sign); card 2 signed, D1 only, D4 not built; card 3 signed (the recede is the alarm's hardware, never the Relay; B's pulse is the silent alarm made visible, pointing out; the reset is restoral); with 声音设计 4: no siren, the trip plays the door-holder thunk without the chime. Peer question P1 (may a fire-door frame behind the runner swing shut at the trip): **no**, the frames behind stay open, only the goal end's hold-open magnets let go.
- **系统设计**, `systems.json` A60 (Red's 全部按推荐) and A61 (its rulings), RELAY §6 and its Decided block: cards 1 and 2 adopted (the receding variant arms at the 9 m frame; §4.8 and F1-F10 in RELAY §6); card 4 the Run door trips 2.5 m inside and reads from 5 m (starting values); card 5 rule 11 holds (the reset re-strike lights only the Run! room's lamps and only on Away; the glide reads the player's position only; C's tide may never follow the Relay's reach); card 6 the teaching room yes (the run's first Run! with the pulse off, look A), waiting on the tube; card 3 the bot rounds sent to 关卡设计调研, not run yet. Peer question P3 (door loudness at T5): **confirmed**, under v2 a slam carries 27 m x 1.3 = 35.1 m along the path against at least 49 m: a 14 m margin.
- **灯具适配**, card 1: the still delivered 06:00 and an HDR follow-up 06:12 on Red's comment (§0.1, decision 6).

**关卡设计** (owner of `Scripts/FrontRoomsMap/*`, `FrontRooms3DGame*.cs`, `Editor/FrontRoomsMap/*`, `Assets/Levels`; rule 3)
1. **Length by depth.** A per-room length table by tier: 7 cells for the run's first Run!, then 9, 12 from T4, 14 at T5 where it fits; cap 14 cells; never by the player's performance. First measure the 12- and 14-cell fit rates on 100 seeds with `-autopilotRunRooms`. Rests on decision 3. Source §3.2, §3.6.
2. **One Run! a floor, from a deck.** Replace the per-zone roll with a per-floor deck by tier (§3.4's table), "nothing" in every deck; no variant on two floors running, the receding end never on two floors running; one twist per room; the run flag (a plain Run! met before any twist); no changed door in T5's deck. Rests on decisions 1, 8b, 8c. Source §3.4, §3.6.
3. **A calm beyond the door.** No other trigger kind in the Run!'s host zone or in the zone beyond its goal door. Validator: no Relay entry inside or just beyond a Run! room, the goal door never on its route (F4); never the only route, never the key alone (F5); no chains (F7). Rests on decisions 1, 8e. Source §3.4, §4.7, §7.6.
4. **The receding variant** (9 → 12 from T4 on floor B2 or deeper; 9 → 14 at T5 on B3 or deeper where it fits, else 12). Reserve L_max at plan time; build the hidden cells with renderers, lights, colliders and graph edges off until each seat; the end wall and RN-F as one movable GoalHead; the goal point and normal per seat; the trip line at the 9 m frame (FD1) for this variant only; no side doors inside; the release side's blind doorway and swap; persist the steps done. Rests on decision 4. Source §3.6, §4.3-§4.8, §5.9.
5. **The Run door entrance** (D1; from T3, after one plain Run!). A Run door (RN-F with set A's F5 lite) on the entrance edge, as at the goal edge, the trip about 2.5 m in (系统设计 4 sets the depth); confirm what the lamps and the "door behind" do when a room is met from its goal end. Rests on decision 8c. Source §7.2, §3.6.
6. **Ramps.** Public 1:12 ramps first, in quiet zones; the Run! ramp (1:8, at most 24 m and 3.0 m down, never a storey, extra cells level) from T3, once the map holds per-cell levels (`40` §11.7). Rests on decision 2. Source §3.5.
7. **Pool and the well in a run.** Propose to Red where the pool level and the well come in a run; decision 1 leaves it open (`70` SP2's stack is a depth study, not an order). Rests on decision 1, its open part. Source §3.4.

**游戏视觉, with 灯具适配 for the lamps** (游戏视觉 owns `Scripts/Rendering`, with `FrontRoomsRunRig.cs` and `AlarmTell/`, `Resources/{Surfaces,Rendering,Props}` and `Tools/Blender/frontrooms_kit`; rule 3)
1. **The rig in cells.** Run! B's rig and dress parametric in cells for 7-14 cells: a strip every cell, heads every two, compartment frames every three, the goal assembly at the end; the RESET re-strike over the current length. Rests on decisions 3, 7. Source §3.2, §6.6, §10.
2. **The recede, head first.** `RunRigState.Recede` from one trigger time: the stepped schedule (a two-cell flinch, one-cell steps, a heavy final seat), the keep-away glide at 6 m/s within 12 m, the gaze gate (35°, fallback at 18 m), events for 声音设计; the GoalHead kit (track, trolleys, magnets, sweeps, the carried twin-head with 灯具适配, C42 in a light box, ghost bands) and the compartment frame at 9 m (FD1, the trigger plane); then T2's block pieces and two baked clips with contact lists (phones: half-course strips, no grit; WebGL: T1 alone); grit by `54`'s GPU path; the hidden cells' strips (with 灯具适配) striking once as each cell seats (§5.7); §5.8's engine notes. T3 is not built now; T4 waits for a pattern-safety capture. Rests on decisions 4, 5. Source §4.5, §5.1-§5.9.
3. **WAIT-RED (灯具适配 1) · The red looks.** After Red's yes on the still: 灯具适配 adds one red T12 tube per R-B strip, on the battery circuit, to the strip kit; 游戏视觉 adds to `FrontRoomsRunRig.cs` a per-strip red level beside the white, the trip front (A, B), B's pulse (every 2.5 s from 3.0 s after the trip, 12 at most), the tripped ambient tint and the Reduce Flashing variants, tuned to §6.3's targets on a capture. B is the look and A (the pulse off) its fallback; C stays in SP7's scene-4 driver. If Red says no, nothing red is built. Rests on decision 6. Source §6.2-§6.9.
4. **The Run door's tell.** For the door entrance: the ALARM ZONE card and the red-banded detector base at the door (`AlarmTell/FrontRoomsAlarmTell.cs`), the corridor's armed light in the 15 x 15 in lite and under the door, readable from at least 5 m (with 关卡设计 5). Rests on decision 8c. Source §7.2, §7.5.
5. **The combined test** (rule 25, one run, silent): the Relay read at 2.05 m under red with protan (at least 4 : 1); T9 over the trip, the pulse train, the recede and the reset; Harding FPA or PEAT on that capture; the T2 pattern; the dark reds on the phone tier; one sheet per look along the film's camera path. Its red items wait for 灯具适配 1. Rests on decisions 4, 5, 6. Source §5.7, §6.7-§6.9.

**灯具适配** (the zone_lamps line, which built and landed the R-B strip kit, `W/apply/zone_lamps_run`, 11-03, in 游戏视觉's paths by rule 3: `Resources/Props/Lamps`, `Scripts/Rendering`; with 游戏视觉 for the Run rig's light states)
1. **WAIT-RED · The red tube's still (decision 6).** One Shot-format sheet (`W/tools/shotframe/shotframe.py`, the FRONTROOMSSS mark, chip WAIT-RED; VISUAL_STANDARD §2.2-§2.3). Columns: TODAY, the armed R-B strip as built, the strip Red approved on 11-03 ("就这样", `research/zone_lamps/sheets/zone_lamps_run_armed_asbuilt.jpg`); the same strip with one dark oxblood third tube (a red-coated T12, 1.22 m x Ø 38 mm, inside the wire guard beside the two white tubes, on the battery circuit, unlit); and that strip in its lit alarm state (the whites out, the red tube at §6.3's core level, about (186, 6, 23) on screen, the strip's Light recoloured red at 1.15, the T-A and T-B heads as built). Same camera and exposure in every column, only the tube and its state change; rows at player eye 1.62 m and close up (about 0.35 m, LOD0); a COST row (about 150 tris a tube, one shared emissive material, no new texture, +0 Lights, +0 shadow maps, 9 tubes in a 9-cell room); the header sentence gives the recommendation (the tube is look B's red source) and its flaw (it changes the strip Red approved on 11-03); the fit check. Rendered in a clone the line already has (no new clone, VISUAL_STANDARD §6); nothing lands. 总控 puts it to Red (总控 2). **No change to the strip kit before Red says yes.** Rests on decision 6, its one exception. Source §6.2, §6.3, §6.9, §8 decision 6.

**叙事设计** (meaning and strings)
1. **The frame and the message.** Run! is a fire compartment already on the alarm (EGRESS): room one's "WALK, DO NOT RUN." fails here and "CLOSE DOORS BEHIND YOU." saves you; no new string. Your call: whether room one's placard hangs again a few cells inside. Rests on decision 8a. Source §7.1.
2. **The deception is D1.** An honest Run door that fools a habit (its lite, its tell and its light tell the truth); no new string; the changed door (D4) is not built, so "the building lying" stays a deep-floor idea (`10` L0-7). Rests on decision 8c. Source §7.2.
3. **What the recede and the red say.** The recede is the alarm's hardware pulling the compartment's end away, never the Relay; B's pulse is EGRESS's silent alarm made visible, running to the door; the white reset is the restoral. Whether a siren joins the red: with 声音设计 4. Rests on decisions 4, 6, 7. Source §5.5, §6.4, §6.6, §7.1.
Nothing open on the ALARM ZONE card: its face is final, wording signed by 叙事设计 on 11-01, `CardFaceIsStandIn` false (`FrontRoomsAlarmTell.cs`:41-43; `today.txt` §4 read an older note).

**系统设计** (gates, reward and fairness: RELAY §6, `systems.json`)
1. **Gates and the deck.** The gates by tier and floor, the deck and one Run! a floor; length follows depth, never performance. Rests on decisions 1, 3, 8b. Source §3.4, §3.6, §7.2.
2. **Order T.** The receding variant trips at the 9 m frame (it bends RELAY :371); adopt §4.8 and §7.6 into RELAY §6. Rests on decision 4. Source §4.4, §4.8, §7.6.
3. **Bot rounds with `runRooms` on** (now). How often a sprint that reaches the door also escapes (R2); chase dives into receding rooms at T4-T5, then (a) as designed if a dive with an 8 m lead still escapes, else (b) recede only when the third is crossed at a walk; trips with the Relay on the map. Rests on decisions 4, 8e. Source §4.7, §7.4.
4. **The Run door's numbers.** Its trip depth (about 2.5 m) and readable distance (at least 5 m). Rests on decision 8c. Source §7.2.
5. **Rule 11 checks.** The restoral watched through the goal lite; the keep-away glide (it reads the player's position only); C's tide read as the hunter's reach (a playtest question only if C leaves scene 4); the variant decided when the zone is decided. Rests on decisions 4, 6, 8e. Source §4.5, §4.7, §6.4, §7.4.
6. **Optional, your call: the teaching room.** The run's first Run! with the pulse off (A), B after; it waits for 灯具适配 1 like every red item. Rests on decision 6. Source §6.9.
7. **Door loudness at T5.** Confirm v2's (`pacing.txt` G10: 31.5 m against 31 m in the v1 audit). On paper v2 answers it: hearing runs along the path, a slam carries 27 m x 1.3 = 35.1 m at T5 (RELAY :400, :407), short of the at least 49 m of path in `pacing.txt` D2 (c). Rests on decisions 3, 8e. Source §3.2 (c).

**声音设计** (owner of `Scripts/Audio/*`, `FMOD/`, `Tools/audio/`; rule 3; we play nothing, rule 24)
1. **The recede's events.** Bind T1's and T2's events (§5.10); T3's and T4's wait with their bodies. Rests on decisions 4, 5. Source §5.10.
2. **The light's partners.** A relay click per cell as a front passes, the reset's clunks, and the red tubes' ballast hum once Red has said yes to the tube (灯具适配 1). Rests on decisions 6, 7. Source §5.10, §6.6.
3. **The trip's sound.** The door-holder thunk without the lock-on chime, or a device sound of its own; never thunk plus chime (it would lie about sight). Rests on decisions 4, 8a. Source §7.5 (S4).
4. **A siren, or none.** Whether a siren-like partner joins the red (EGRESS's building alarm is silent), with 叙事设计. Rests on decision 6. Source §6.8.

**总控**
1. **Route the cards.** Send each owner its cards above, with Red's answer quoted (§0.1). Rests on Red's answer of 11-03.
2. **The still to Red.** When 灯具适配 1's sheet is ready, put it to Red in one Chinese message, yes or no to the third tube; the answer goes into §0.1, decision 6. Rests on decision 6.
3. **Scene 4.** Build SP7 per `70` §9 (§10 below with Red's answer, written into `70` by this line on 11-04: the spec scene 4 builds) with the structures workflow (rule 5b: main's scene 4, greybox, inert for the game; Roslyn gate, CLAIM and LANDED). Rests on decision 9. Source `70` §9, §10.

Done, no longer cards: §8 was put to Red and answered on 11-03 (the old 总控 1); `70` SP5's three conflicts were fixed by this line on 11-03 22:5x (the old 总控 3, §10.1); this line's own step, not a card, is done too: §10 went into `70` as its §9, SP7, on 11-04.

## 10. A district for scene 4: SP7 · Run!: the door that backs away (agreed for scene 4, 11-03)

Rule 5b (Red 11-03): new structures go to scene 4 `FrontRoomsStructures.unity` first, inert for the game. This district walks the recommended version of §3-§6. It is written in `70`'s format so that, once Red has decided, it becomes `70`'s SP7 (or replaces SP5's Run! content); `70` is not edited here. `70` §1 (global frame: cells, placement, hub, signs, stand-ins, honesty, assets) and §2 (look rules) apply unchanged. **Agreed 11-03** (decision 9; Red: "全部按推荐", §0.1): SP7 goes into scene 4 as written below, built by 总控's structures workflow (§9, 总控 3), and becomes `70`'s SP7, beside SP5 and its ramp, at this line's next edit of `70`. Red walks the agreed version and may still change it; under rule 5b nothing in it enters the game until Red has walked it and said so. Its red tube is a scene-4 greybox stand-in: the strip kit's tube and the game rig's red states wait for Red's yes on 灯具适配 1's still (§0.1, decision 6), and a no takes the stand-in and looks A-C out of SP7 too. **Done 11-04:** `70` §9 is SP7, composed from this section with Red's answer; scene 4 builds `70` §9, and where the two differ (the items it marks composed, and the red tube on its own key, off on arrival) `70` §9 is the spec.

**What Red gets:** Run! B's service corridor at 14 cells (42 m) with a movable end. One key sets the variant: plain at 7, 9, 12 or 14 cells (the end placed at that cell line) or receding 9 → 12 / 9 → 14 (the end at 27 m backs away when Red crosses the frame at 9 m). Other keys switch the body (T1, T2), the light (today's Run! B, A, B, C), the entrance (arch or Run door) and Reduce Flashing. A static Relay stand-in for the look back under red. The corridor stays a corridor: no catwalk, no ramp here (SP5 has it), no side doors.

**Why this composition:** §3.2 (lengths), §4 (the stretch, Order T, the beats, the keep-away), §5 (the head, T1, T2), §6 (the red tube, three looks), §7.2 (the Run door), `10` RN-2 (compartment frames), `63` RN-SPINE (dressing), Run! B's rig and section (`run_b_production/10_plan.md` §1.3-1.4). The order on the grid and every item marked "composed" are this page's.

**Layout** (district-local metres, x east, z north; cell lines x = −1.5 + 3i, z = 3j, as SP5 with the 1.5 m root offset; spine axis x = 0; footprint x −4.5…4.5, z −3…51, y 0…5.4):
- **Vestibule** (i 0, j −1; z −3…0): one Standard Level 0 cell (L0 surfaces, one L03 troffer at lens 1.8), the host zone's side of the entrance. **Arrival** at (0, 1.62, −2.0), 2 m outside the entrance (the readability point, `10_run_directions.md` §1.1), facing north, level camera.
- **Entrance** (on z 0): Run! B's arch (x −0.7…0.7, head 2.2) with Run! B's tell (the ALARM ZONE card at 60 in beside the opening, the red-banded detector base 2.7 m in). Key G swaps the arch for a **Run door** (RN-F with set A's 15 x 15 in F5 lite, the same door as the goal door) on the same edge, with the card and detector at the door; with the door the trip moves to 2.5 m in (§7.2, composed).
- **Corridor** (i 0, j 0…13; z 0…42): Run! B's section exactly: 2.84 clear, Standard 2.9; RunB_BlockLower to 1.2, RunB_BlockUpper, bumper rail at 0.9, RunB_Cove; RunB_Floor with lane, accent and a wax band; RunB_Soffit; sprinkler main Ø 0.10 at 2.62 (x +0.55), tray at 2.70 (x −0.60), EMT on the west wall. Fire gear (pull station, bell, extinguisher cabinet) at Run! B's z. No side doors (§4.8 item 9).
- **Lamps:** 14 R-B strips (bare two-tube strips in wire guards, tube lens 2.6) at z = 1.8 + 3k, the stutter tube at z 13.8; each strip with a **third, red tube** (greybox: an emissive cylinder 1.22 m x Ø 38 mm inside the guard, dark oxblood when unlit). Tripped heads every 6 m on alternating walls, T-A units at the two ends and T-B remote heads between (Red's "TA和TB相结合"), extended from Run! B's Corridor1x9 pattern. Every lamp of a hidden cell stays dark until its cell is revealed.
- **Compartment frames** (`10` RN-2) on z 9 (FD1, **the trigger**) and z 18 (FD2), as SP5's: 0.16 block on the cell line, a pair of 1.0 m leaves in a 2.0 x 2.1 frame, held open on magnets. None beyond z 27: the head travels through the hidden length.
- **The head** (the movable end; §5.1): block-faced end wall, RN-F goal door with its F5 lite and panic bar, frame, a twin-head unit on a short bracket above the door, aimed back at the leaf (the goal head, §5.1), C42's light in a box behind the lite; two trolleys on a surface-mounted steel track in both soffit corners, from z 9 to z 42, and a 20 mm guide slot in the floor from z 27 to 42; hold-open magnets; rubber sweeps. Its rest position is the current variant's end line (z 21, 27, 36 or 42), always on a whole cell.
- **Hidden length:** the cells beyond the head are built in the same kit, renderers, colliders and lights off until revealed. Ghost bands (greybox: a strip of lighter paint and unworn wax 0.2 m deep on walls, soffit and floor) appear at every line the head has stood on.
- **Release hall** (i −1…1, j 14…16; z 42…51): 3 x 3 cells, Tall 5.4, RunB surfaces, lit like SP5's dock (one Run! B strip per cell at x1.6, LMS §4 Tall) so its light shows in the goal lite: the brightest thing at the end (RN-4). Its south wall holds the real goal frame on the axis; while the head stands short of z 42 the frame holds a blind block panel (the "blind doorway" of §4.6), swapped when the head seats.
- **Relay stand-in** (static, `70` §1): x −0.6, z 6.0, facing north: the look back from the goal end under each light, the 2.05 m read under red.

**The variants (key L cycles; a small plate beside the sign shows the current one):** plain 7 (end at z 21), plain 9 (z 27, today's room), plain 12 (z 36), plain 14 (z 42), receding 9 → 12, receding 9 → 14. Plain rooms arm 1.5 m in. Receding rooms arm at the FD1 plane (z 9) and run §4.5's schedule from the trip: gaze gate 35°, fallback at z 18; S1 a two-cell flinch, one-cell steps, a heavy final seat; the keep-away glide at 6 m/s when the walker is within 12 m of the head between steps; the record stays until key Y. In any variant that ends short of 14 cells (plain 7, 9 or 12, receding 9 → 12) the goal door does not open in scene 4 (only the length and the sprint are being tried; in the game the door is never locked, C38); the sign says so.

**Other keys** (district-scoped; if 总控's Plan has fixed other keys, they replace these): 7 arrival (the hub's next district key); T trip / reset by hand (as SP5); B body T1 / T2; C light: today's Run! B → A → B → C (§6.4); G entrance: arch / Run door; V Reduce Flashing on / off; Y put the head back at its rest line and re-arm (R stays SP1's).

**Light and timing** (a structures-only driver, scene 4 only; production is 游戏视觉's): the armed and tripped states as Run! B's rig; the reset re-strike with the rig's own timing (`RunRigLayout`: ripple 0.025 s a cell, start 0.45 s, 0.15 s a cell, 0.40 s rise), over the current length; looks A, B, C with §6.3-6.4's numbers; in receding rooms §5.7's grammar (the magnets drop at the trip, the band from the entrance reaches the end as the flinch seats, revealed strips strike once in red, the goal head and C42 never change, B's pulses after the final seat); Reduce Flashing as §6.7. Light budget: SP7's strips and heads stay on whenever the walker is in SP7 (its main view is 42 m deep, `70` §2).

**Main view on arrival:** (0, 1.62, −2.0) facing north: the arch with its card and detector; the corridor in armed white, the strips receding on the centre line, each with its dark red tube; FD1 and FD2 open; the end wall at 27 m with the goal lite lit by C42's box behind it (the hall's own light reaches the lite only when the head stands at z 42); in the receding variants the two track lines in the soffit corners running from FD1 to the end wall.

**Greybox and honesty:** the red tube is an emissive cylinder; T2's blocks follow simple programmed paths (top out, two streams along the walls, bottom in) instead of rigid bakes; track, trolleys, magnets, sweeps and the guide slot are plain steel shapes; ghost bands are a paint strip; no grit. The structures-only driver imitates the rig's states; the game's rig has none of the red or recede states yet. No moving Relay and no sound in scene 4: chase fairness (§4.7) and the sound questions (§7.5) are tested elsewhere. `BUILD_LOG.md` gets tris, renderers, draws, lights and texture MB, desktop Editor only (`70` §1).

Sign (SP7):
> **SP7 · Run!：会往后退的门**
> 游戏里的 Run!（Run! B 的设备走廊），一共 14 格、42 米。按 L 切换：普通的 7 / 9 / 12 / 14 格，或者"会退"的 9→12 / 9→14 格。会退的版本里，走到 9 米那道防火门框时警报触发，尽头的墙和门开始往后退：先猛地缩两格，再一格一格退，最后很重地停住，门就是真门。按 B 切换后退的方式：T1 整面墙滑走；T2 砖块拆开再砌回去。按 C 切换灯光：今天的样子、A 警报回路、B 警报脉冲、C 红潮。按 G 切换入口：拱门或带 15×15 英寸小方窗的 Run 门。按 V 开关"减少闪烁"。按 T 手动触发或恢复，按 Y 把门放回原位再试一次。
> 灰盒占位：红色灯管（用发光圆柱代替）；砖块重组的路径（用简单的程序动画代替烘焙）；轨道、门吸、密封条；墙上的"印子"；灯光状态由场景 4 专用的脚本模拟，正式效果要等游戏视觉做；没有声音。长度不到 14 格时，尽头的门在这里打不开（游戏里门永远不锁）。黑色人形是 Relay 的静态替身（2.05 米），只用来检查视线，不会动。
> 走过后告诉我们（11 月 3 日已按推荐定下，走过以后还能改）：1 长度（7、9、12、14 格）走起来对吗？7 格会不会太短？ 2 退的方式：T1 还是 T2？ 3 红色：A、B 还是 C？灯架里多一根暗红灯管可以吗？ 4 在 9 米触发、门往后退的节奏（先猛缩两格，再一格一格，最后重重落位）对吗？ 5 入口用 Run 门（"被骗的门"）可以吗？ 6 回头看时，红光下看得清 Relay 替身吗？

**Walk tests** (for the Review stage, as `70` §11): the end line is always on a whole cell; nothing moving ever enters the walk volume or passes the walker; the goal lite stays the brightest thing in every look and every state; the Relay stand-in reads against what is behind it under every look (with a protan check); no light runs toward a window or glass; FD1 and FD2 leave 2.0 x 2.1 m clear (the Relay's 2.05 m passes).

### 10.1 SP5 as written conflicts with three of Red's calls (for this line's next edit of `70`; not changed here)

From `today.txt` §9, checked against `70` §7 as it stands (lines 211-255): (1) "EXIT flag at z 13.5 on the west wall… the wall EXIT over the goal door" and "EXIT steady" break C41 (no EXIT sign anywhere in a Run! room); (2) the goal door "Run! B's RN-X" is retired: the goal is RN-F with set A's F5 lite, never locked (C38, systems.json A54 (a)); (3) "wraps on the soffit" and twin-heads at z 3.3 / 15 / 21 break Red's "run选择R-B" and "这里应该是TA和TB相结合" (landed 11-03, "就这样"). The "EXIT steady" words sit in SP5's trip-key text, not on its Chinese sign. If scene 4 is built from `70` as it stands, Red walks a Run! that contradicts three of Red's own decisions; SP7 above avoids all three. **Done 11-03 22:5x (this line, login G):** `70` SP5 now has no EXIT sign (C41), the RN-F goal door with its F5 lite, never locked (C38), R-B strips armed and T-A twin-heads with T-B remote heads tripped.

## 11. Sources

The rows below are also in `SOURCES.md` (section "The Run! room (71_run_room.md)"). They group the web sources of the six drafts; the draft and its entry are named in the Status column. **READ** = the page was fetched and read by this workflow's research stage (login G or B, 2026-11-03), not again by this writer; "in part" = only the first 100,000 characters; **VIA** = known through another source that was read; **[search]** = a search-result snippet only. Nothing was downloaded into the project, no video was played, nothing made a sound. Already registered and cited as they are: [LS37] (the Clockwork Mansion, GMTK), [LS47] (ADA 1991 ramps and stairs). Related rows already registered: [LS29] (Control, the Oldest House talk), [LS30] (Alien: Isolation), [LS31] (The Exit 8, interviews), [LS59] (mobile shelving), [LS186] (Spelunky's generator).

| # | Source | Date | What it gives | Status |
|---|---|---|---|---|
| LS201 | *Poltergeist*, the hallway: Wikipedia, "Poltergeist (1982 film)", https://en.wikipedia.org/wiki/Poltergeist_(1982_film) ; Wikipedia, "Dolly zoom", https://en.wikipedia.org/wiki/Dolly_zoom ; G. Edwards, "Revisiting Cinefex (10): Poltergeist and Firefox", https://graham-edwards.com/2011/11/01/revisiting-cinefex-10-poltergeist-and-firefox/ ; T. Moynihan, *Wired*, 2014-08-28 | 1982; 2011; 2014 | The hallway stretched by a dolly zoom; the door centred, lit, never hidden; no source ties a person or a rig to the shot | READ (endless E2, login G; Wikipedia in part); *Wired* VIA Wikipedia |
| LS202 | *Vertigo* and the dolly zoom: AFI Catalog, https://catalog.afi.com/Film/52788-VERTIGO ; Wikipedia, "Dolly zoom" (as LS201); TCM trivia, https://www.tcm.com/articles/89394/trivia-6-18-and-12-3-vertigo | 1958 | A one-tenth model stairwell laid on its side, tracked and zoomed; the subject held at one size while the space stretches | READ (endless E1, login G); the cost figure [search], not used |
| LS203 | *P.T.* (Kojima Productions): V. Eisenmann, Game Developer, https://www.gamedeveloper.com/design/p-t-silent-hills-teaser-game-analysis ; B. Hester, Game Informer, https://www.gameinformer.com/index.php/2020/10/14/looking-back-at-pt-kojimas-silent-hill-game-that-never-was ; A. Reiner, Game Informer, https://www.gameinformer.com/gamer-culture/2019/09/10/the-scariest-part-of-pt-was-just-revealed-through-a-hack ; H. Alexandra, Kotaku, https://kotaku.com/p-t-hacker-is-still-finding-gruesome-secrets-1838489446 ; Kalinowski, *Press Start* 5(1), https://press-start.gla.ac.uk/press-start/article/view/121 | 2014-2020 | A door-to-door loop; changes by state flags; the threat behind the player | READ (endless E3, login G); *Press Start* abstract only |
| LS204 | *Layers of Fear* (Bloober Team): J. Couture, Game Developer, https://www.gamedeveloper.com/audio/terrifying-players-with-unstable-level-design-in-i-layers-of-fear-i- ; E. David, SiliconANGLE, https://siliconangle.com/2015/11/13/layers-of-fear-dev-explains-why-the-game-is-based-on-the-mood-rather-than-jump-scares | 2015-16 | Changes triggered by a spot or a glance, made behind the player's back | READ (endless E4, login G); Den of Geek interview [search] |
| LS205 | *Super Mario 64*, the endless stairs: Super Mario Wiki, https://www.mariowiki.com/Endless_stairs | 1996 | A warp back by a few steps until 70 stars; fog hides the top | READ (endless E5, login G); the Shepard-tone reading [search] |
| LS206 | *Little Nightmares* I and II (Tarsier): Wikipedia, https://en.wikipedia.org/wiki/Little_Nightmares , https://en.wikipedia.org/wiki/Little_Nightmares_II ; C. Priestman, Game Developer, https://www.gamedeveloper.com/design/crafting-the-bizarre-off-kilter-gameworld-of-i-little-nightmares-i- ; Nintendo Life (D. Mervik), https://www.nintendolife.com/news/2021/03/feature_the_theme_is_escapism_and_its_many_forms_-_little_nightmares_ii_dev_on_ideas_and_inspirations ; gamepressure, https://www.gamepressure.com/little-nightmares-ii/door-puzzles/zae2a2 | 2017; 2021 | The dream hallway's door, denied by an agent and reached late; slamming doors always beside one lit door; no source read says the hall stretches | READ (endless E6); the Unreal developer interview [search] |
| LS207 | *The Evil Within* (Tango Gameworks): H. Aziz, Destructoid, https://destructoid.com/?p=151510 ; Wikipedia, https://en.wikipedia.org/wiki/The_Evil_Within | 2013-14 | Scripted shifts ended by cuts and teleports | READ (endless E7); the CinemaBlend stretching-hall report and the Engadget round-up [search] |
| LS208 | *Silent Hill* 2, 3, 4: silenthillmemories.net, https://www.silenthillmemories.net/sh2/walkthrough_08_historical_society_en.htm , https://www.silenthillmemories.net/lost_memories/guide/100_en.htm (fan translation), https://www.silenthillmemories.net/sh3/walkthrough_13_lakeside_amusement_park_en.htm , https://silenthillmemories.net/sh4/info_en.htm ; Wikipedia, https://en.wikipedia.org/wiki/Silent_Hill_2 , https://en.wikipedia.org/wiki/Silent_Hill_4:_The_Room | 2001-04 | SH3's red-glow chase with one moving wall; SH4's room with the world seen and never reached; SH2's long descent | READ (endless E8; SH2's Wikipedia page in part); TV Tropes [search] |
| LS209 | *Control* (Remedy): 80.lv, https://80.lv/articles/creating-control-s-ashtray-maze/ ; Game Developer, GDC 2020 listing, https://www.gamedeveloper.com/design/see-how-i-control-i-s-ashtray-maze-was-made-at-the-gdc-2020-summits- ; Source Gaming, https://sourcegaming.info/2025/09/08/center-stage-the-ashtray-maze-control/ ; B. Shea, Game Informer, https://gameinformer.com/2019/03/15/uncovering-the-mysteries-of-controls-the-oldest-house ; Game Developer, https://www.gamedeveloper.com/design/exploring-the-world-driven-game-design-of-i-control-i- and https://www.gamedeveloper.com/design/how-remedy-harnessed-the-ethereal-beauty-of-brutalism-to-shape-i-control-i- ; see [LS29] | 2019-25 | The Ashtray Maze moving in plain view, modular, in the most linear part of the game; Control Points where the shifting stopped | READ (endless E9, motion M3); how Remedy animated the shifts not found: GameSpot and Edge [search, 403] |
| LS210 | *Inside* (Playdead): Wikipedia, https://en.wikipedia.org/wiki/Inside_(video_game) ; B. Sheffield, Game Developer, GDC Europe 2010 (J. Carlsen on Limbo), https://www.gamedeveloper.com/pc/gdc-europe-i-limbo-i-s-carlsen-on-making-players-your-worst-enemy-and-your-best-friend | 2010; 2016 | A far shore reached, quiet and lit; failure that costs little and teaches | READ (endless E10); the soundscape paper [search, paywalled] |
| LS211 | *Antichamber* (A. Bruce): Wikipedia, https://en.wikipedia.org/wiki/Antichamber ; D. Riendeau, Engadget, https://www.engadget.com/2012-05-02-on-the-fringe-part-one-alexander-bruces-antichamber.html ; E. Eifler, KQED, https://www.kqed.org/arts/120000/antichamber_takes_gaming_into_the_fourth_dimension | 2012-13 | The exit visible from room one; places kept fixed once learnt | READ (endless E11); the GDC 2014 talk [search] |
| LS212 | *Amnesia: The Dark Descent*: T. Grip, "Game Design Deep Dive: Amnesia's 'Sanity Meter'", Game Developer, https://www.gamedeveloper.com/design/game-design-deep-dive-i-amnesia-i-s-sanity-meter- ; Wikipedia, https://en.wikipedia.org/wiki/Amnesia:_The_Dark_Descent | 2010; 2014 | Screen distortion set below what felt best, for nausea | READ (endless E12) |
| LS213 | *Outlast* and *Outlast 2* (Red Barrels): J. Couture, "The art of the chase: Level design and player orientation in Outlast 2", Game Developer, https://gamedeveloper.com/audio/the-art-of-the-chase-level-design-and-player-orientation-in-i-outlast-2-i- ; Wikipedia, https://en.wikipedia.org/wiki/Outlast | 2013; 2017 | Light leads; beats of false safety and disruption; chases kept short | READ (endless E13, pacing C8) |
| LS214 | *The Exit 8* (Kotake Create): Steam, https://store.steampowered.com/app/2653790/The_Exit_8/ ; Wikipedia, https://en.wikipedia.org/wiki/The_Exit_8 ; see [LS31] | 2023 | Rules posted in room one; endless until eight right calls; a chase of red water | READ (endless E14, story C1; Wikipedia in part); the red-water anomaly [search] |
| LS215 | *The Shining* (Kubrick): G. Brown, "The Steadicam and The Shining", *American Cinematographer* (Aug 1980; web text 2022), https://theasc.com/articles/steadicam-shining-revisited ; Wikipedia, https://en.wikipedia.org/wiki/The_Shining_(film) | 1980 | Corridors tracked at speed from a wheelchair rig; an impossible plan felt, not seen | READ (endless E15; Wikipedia in part) |
| LS216 | *Labyrinth* (Henson): R. Magid, "A Goblin World Created for Labyrinth", *American Cinematographer* (Aug 1986; web text), https://theasc.com/articles/jim-henson-labyrinth-making-of ; Wikipedia, https://en.wikipedia.org/wiki/Labyrinth_(1986_film) , https://en.wikipedia.org/wiki/Sarah_Williams_(Labyrinth) | 1986 | False perspective extending the mazes; one fixed reference in the Escher room | READ (endless E16; Wikipedia in part); the worm scene [search] |
| LS217 | *A Nightmare on Elm Street* (Craven): Wikipedia, https://en.wikipedia.org/wiki/A_Nightmare_on_Elm_Street | 1984 | The melting stairs; the rotating room set | READ (endless E17, in part); Rolling Stone 2014 [search] |
| LS218 | *The Haunting* (Wise): Wikipedia, https://en.wikipedia.org/wiki/The_Haunting_(1963_film) ; A. Passafiume, "Behind the Classics: The Haunting (1963)", TCM, https://www.tcm.com/articles/behind-the-classics/961615/behind-the-classics-the-haunting-1963 | 1963 | A laminated-wood door pushed from behind; one cable shaking the whole stair; brightly lit sets | READ (motion M5b; Wikipedia in part) |
| LS219 | Moving architecture in games: The Portal Wiki, "Portal 2 developer commentary", https://theportalwiki.com/wiki/Portal_2_developer_commentary ; Combine OverWiki, "Panel", https://combineoverwiki.net/wiki/Panel ; C. Carrier, "Level Design Deep Dive: Dishonored 2's Clockwork Mansion", Game Developer, https://www.gamedeveloper.com/design/level-design-deep-dive-i-dishonored-2-s-i-clockwork-mansion ; The Wizard's Code, https://thewizardscode.blogspot.com/2023/04/dishonored-2-devs-explain-clockwork.html ; see [LS37] | 2011; 2016 | Panels moved only where the player's position is certain; crushers cut as "too punishing"; one source of movement; AI in both configurations | READ (motion M1, M2); Valve Developer Community "Panels" [search, 403] |
| LS220 | *Inception* (Nolan): V. Wagh, No Film School, https://nofilmschool.com/how-inception-faked-zero-gravity ; AFI Catalog, https://catalog.afi.com/Catalog/moviedetails/66167 ; Art of VFX (P. Franklin), https://www.artofvfx.com/inception-paul-franklin-superviseur-vfx-double-negative/ | 2010 | The rotating corridor lit by its own ceiling fixtures; mostly in camera | READ (motion M4); other corridor lengths [search], not used |
| LS221 | Forced-perspective corridors: Camera Obscura and World of Illusions, Edinburgh, "What's an Ames room?", https://www.camera-obscura.co.uk/article/whats-an-ames-room | (Willy Wonka, 1971) | A corridor tapered for "a false sense of distance" | READ (motion M5); the 2024 recreation [search] |
| LS222 | Real moving corridors and walls: Wikipedia, "Jet bridge", https://en.wikipedia.org/wiki/Jet_bridge ; US 5,704,086 (FMC), https://patents.google.com/patent/US5704086 ; US 4,319,376 (Abex), https://patents.google.com/patent/US4319376 ; US 4,106,544 (Panelfold), https://patents.google.com/patent/US4106544 ; K. Tetlow, "Horizontal Sliding Fire Doors: Architectural Design Freedom", ENR continuing-education course (Won-Door), https://continuingeducation.bnpmedia.com/courses/wondoor-corporation/horizontal-sliding-fire-doors-architectural-design-freedom/ ; see [LS59] | 1958-1998; 2016 | Nested telescoping tunnels with rollers at the upper corners; partitions hung from overhead trackways; accordion fire doors: UL tests 1977, the 1988 Life Safety Code, NFPA 80 speeds 6-24 in/s, closed by the alarm panel or a pull station | READ (motion M6, M7; the ENR course is manufacturer-sponsored); the 14 in stack depth of a current product [search] |
| LS223 | Unity manuals: "Motion vectors in URP" (6000.4), https://docs.unity3d.com/6000.4/Documentation/Manual/urp/features/motion-vectors.html ; "Draw call batching" (6000.0), https://docs.unity.com/en-us/engine/6000.0/manual/analysis/graphics-performance-profiling/reduce-draw-calls/draw-call-batching/draw-call-batching | current | Per-object motion vectors only when a feature asks; static batching and moving parts | READ (motion M8) |
| LS224 | M. Booth, "The AI Systems of Left 4 Dead", AIIDE 2009 keynote slides, Valve, https://steamcdn-a.akamaihd.net/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf | 2009 | Build Up, Sustain Peak, Peak Fade, Relax for 30-45 s; "pacing, not difficulty"; boss events "Tank, Witch, and Nothing", shuffled, no repeats | READ (pacing C1, login G; the PDF's text layer) |
| LS225 | T. Thompson, "The Perfect Organism: The AI of Alien: Isolation", Game Developer, https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation ; see [LS30] | 2014 (game) | The director and the menace gauge; backing off at the peak; behaviours unlocked by progress, never by deaths | READ (pacing C2, login G) |
| LS226 | S. Benard, "Building the Level Design of a procedurally generated Metroidvania: a hybrid approach", Game Developer, https://www.gamedeveloper.com/design/building-the-level-design-of-a-procedurally-generated-metroidvania-a-hybrid-approach- | 2017 | Dead Cells' concept graph per biome: length and exits set by hand | READ (pacing C4, login G) |
| LS227 | G. Smith, M. Treanor, J. Whitehead, M. Mateas, "Rhythm-Based Level Generation for 2D Platformers", FDG 2009, https://expressiveintelligence.github.io/papers/smith-fdg-09.pdf | 2009 | Rhythm groups of 5-20 s joined by rest areas: length in the player's verbs | READ (pacing C5, login G; sections 3-5) |
| LS228 | *Hades* (Supergiant): Wikipedia, https://en.wikipedia.org/wiki/Hades_(video_game) ; GamePressure, https://www.gamepressure.com/hades/underworld-structure/zdebb4 ; DigiPen (E. Gorinstein), https://digipen.edu/showcase/news/eduardo-gorinstein-delivers-challenge-and-fun-hades | 2020 | Lands in a fixed order, rooms random inside; door signs show the chamber ahead | READ (pacing C6, login G); hades.fandom.com not read (402) |
| LS229 | T. Grip, "A History of violence. Part 3", Frictional Games blog, https://frictionalgames.com/?p=746 ; Game Developer, "GDC Europe: Amnesia's Grip delivers terrifying tales of immersion", https://gamedeveloper.com/business/gdc-europe-i-amnesia-i-s-grip-delivers-terrifying-tales-of-immersion | 2009; 2011 | Chases lose force on repeat and need a clear path; "running and not going anywhere"; a reward for getting through | READ (pacing C7, login G) |
| LS230 | Resident Evil 4's pacing: Wikipedia, "Dynamic game difficulty balancing", https://en.wikipedia.org/wiki/Dynamic_game_difficulty_balancing ; Wolfman_J, "Holism: Resident Evil 4 and…Everything", Source Gaming, https://sourcegaming.info/2016/10/26/holism-resident-evil-4-and-everything/ | 2005; 2016 | A hidden difficulty scale of 1 to 10; set pieces met once, each ended in relief | READ (pacing C9; an encyclopaedia and an editorial); the Future Press guide VIA Wikipedia |
| LS231 | M. Davies, "Examining Game Pace: How Single-Player Levels Tick", Game Developer, https://www.gamedeveloper.com/design/examining-game-pace-how-single-player-levels-tick ; J. Bycer, "The Balancing Act of Tension in Horror Game Design", Game Developer, https://www.gamedeveloper.com/design/the-balancing-act-of-tension-in-horror-game-design | 2009; 2015 | Action interjected with calm; constant slowing kills progression; build-up then release; one threat with new issues added | READ (pacing C10) |
| LS232 | The Backrooms Level ! "Run For Your Life!" and its games: PrinceBun, "A Long Forgotten Exclamation", http://backrooms-wiki.wikidot.com/long-forgotten-exclamation ; A-Sync fan copy, https://a-syncbackroomsinvestigationwiki.webador.es/level-run ; Steam, "Backrooms: Run For Your Life", https://store.steampowered.com/app/2559530/BackroomsRun_For_Your_Life/ ; O. Dincer, itemlevel.net, https://itemlevel.net/escape-the-backrooms-level-run-for-your-life-guide/ ; Gameplay Tips, https://gameplay.tips/guides/escape-the-backrooms-all-levels-guide.html ; Level !-! fan page, https://miravoltsyt.github.io/ASYNC-CONFIDENTIAL-FILES/level!-!.html ; Steam Community, "Tips and Tricks" (Backrooms: Escape Together), https://steamcommunity.com/app/2141730/discussions/0/4630357755180401782 ; L. Romain, Nerdist, https://nerdist.com/article/the-8-scariest-levels-of-the-backrooms-creepypasta/ ; frame `room_visuals/images/run_ref_etb_levelbang_0094.jpg` | 2020-2026 | Canon: a hall "ten kilometers" long, lit dim red by exit signs, entered by doors with a red exit sign; Escape the Backrooms: a red ceiling procession and a siren, a scripted course ending at the exit door (no length or time given), placed just before The End; the public memory "flashing red lights and blaring alarms" | READ (pacing C11, red_light §2); the frame looked at on disk; the original page (Aunwyk, 2020) and backrooms.fandom.com [search] (404, 402) |
| LS233 | Boris the Brave, "Dungeon Generation in Binding of Isaac", https://www.boristhebrave.com/2020/09/12/dungeon-generation-in-binding-of-isaac/ | 2020 (rev. 2025) | Rooms per floor = random(2) + 5 + level x 2.6, inside a fixed 9 x 8 grid; the only cap stated is the Labyrinth curse's "max 45"; a wrong count is retried | READ (pacing C12; re-read 11-03 22:2x by the critic, login G) |
| LS234 | Spelunky's ghost: M. Brown, "Spelunky survival guide - top PS Vita tips for new explorers", Pocket Gamer, https://www.pocketgamer.com/articles/053177/r/ | 2013 | A lethal ghost after 2:30, announced; gems it touches turn to diamonds | READ (story C2); the wikis' timings and D. Yu's words [search]; the generator is [LS186] |
| LS235 | *Dark Souls*: Fextralife, "Mimic", https://darksouls.wiki.fextralife.com/Mimic ; "Sen's Fortress", https://darksouls.wiki.fextralife.com/Sen's+Fortress ; B. Shea, Game Informer (H. Miyazaki), https://gameinformer.com/interview/2024/06/24/from-softwares-hidetaka-miyazaki-discusses-his-approach-to-difficulty | 2011; 2024 | A lying chest with tells; a hazard ramp that becomes a shortcut; deaths understood "in an instance" | READ (story C3); the Lower Undead Burg shortcut [search] |
| LS236 | *Pacific Drive*: Wikipedia, https://en.wikipedia.org/wiki/Pacific_Drive_(video_game) ; Z. Handley, "How to use gateways in Pacific Drive", Destructoid, https://www.destructoid.com/how-to-use-gateways-in-pacific-drive/ | 2024 | The player opens the gateway and the storm starts; the escape and the reward one act | READ (story C4); the failure penalty [search] |
| LS237 | *Lethal Company*: Wikipedia, https://en.wikipedia.org/wiki/Lethal_Company ; thealchemist, ESTNN, https://estnn.com/lethal-company-apparatus-what-is-it-and-how-to-find-it | 2023 | The Apparatus: a visible prize; pulling it puts the facility dark | READ (story C5); spawn odds disputed [search] |
| LS238 | Priced rooms: "Curse Room", The Binding of Isaac: Rebirth wiki, https://bindingofisaacrebirth.wiki.gg/wiki/Curse_Room | current | A spiked door that hurts on the way in and out | READ (story C6); Hades' Infernal Gates and Dead Cells' timed doors [search] |
| LS239 | The alarm machinery of a 1990 building: Wikipedia, "Electromagnetic door holder", https://en.wikipedia.org/wiki/Electromagnetic_door_holder ; US Access Board, ADAAG 1991, https://www.access-board.gov/adaag-1991-2002.html ; 1991 ADA Standards, §4.28.3 and A4.28.3, https://www.ada.gov/assets/pdfs/1991-design-standards.pdf (further sections to [LS47]) | 1991 | On alarm the panel cuts power to door holders and the closers shut the doors; new visual alarms are white xenon strobes, 1-3 Hz, pulses of 0.2 s at most | READ (story C9, red_light 4.1); NFPA 72 "door release service" [search] |
| LS240 | J. Vacano, "Photography for Das Boot", *American Cinematographer*, Dec 1982 (web 2023), https://theasc.com/articles/das-boot | 1982 | The boat's own lamps; red the general-alarm and night light, chosen by function | READ (red_light 3.1) |
| LS241 | *Aliens*: shooting script, https://imsdb.com/scripts/Aliens.html ; N. Oseman, "10 Clever Camera Tricks in Aliens", https://neiloseman.com/?p=11290 | 1986 | Mains die, then red emergency light; bars of light passing over a moving figure | READ (red_light 3.2) |
| LS242 | *The Hunt for Red October*: 100 Films, https://100films.co.uk/2018/08/31/the-hunt-for-red-october-1990/ ; W. Carroll, We Are The Mighty, https://www.wearethemighty.com/articles/20-technical-mistakes-and-3-fun-facts-in-the-hunt-for-red-october/ | 1990; 2018 | One colour per boat (blue, red, green) so the cutting stays legible; the US boat in red, a bent rule (a boat rigs for red only to go to periscope depth) | READ (red_light 3.3); Moviemistakes [search] |
| LS243 | *Alien*: "Behind the Scenes of Alien", *American Cinematographer*, Aug 1979 (web 2023), https://theasc.com/?p=10545 ; Disney+ listing, https://www.disneyplus.com/en-gb/movies/-/3gmwPpcKMkwS ; O'Bannon's early draft, https://imsdb.com/scripts/Alien.html | 1979 | The set's own fixtures as the light; a photosensitivity advisory on the flashing finale; the finale's colours unverified | READ (red_light 3.4); British Cinematographer [search, 403] |
| LS244 | *The Terminator*, *The Abyss*: Wikipedia, https://en.wikipedia.org/wiki/The_Terminator , https://en.wikipedia.org/wiki/The_Abyss | 1984; 1989 | No red emergency light described; practical light sources | READ (red_light 3.5, in part); *Lighting Design + Application*, Oct 1989 [search] |
| LS245 | Red light a building owned: Kodak, "Safelight Recommendations", https://www.kodak.com/content/products-brochures/Film/Safelight-Recommendations.pdf ; D. Larzelere, "What is an FAA L-864 obstruction light", https://flightlight.com/what-is-an-faa-l-864-obstruction-light/ ; Wikipedia, "Aviation obstruction lighting", https://en.wikipedia.org/wiki/Aviation_obstruction_lighting | practices before 1990; texts 2006-2026 | Darkroom red filters on a 15 W bulb; red roof beacons at 30 a minute (±3, the current FAA circular, as read; 20-40 in older listings, search) | READ (red_light 4.2, 4.3); FAA listings, studio "on air" lamps, nurse-call domes, a red gel's 4 % transmission [search] |
| LS246 | Red and battery fluorescent tubes before 1990: US 4,988,914 (GTE, filed 1989-04-24), https://patents.google.com/patent/US4988914 ; US 5,004,953 (The Bodine Company, filed 1989-06-30), https://patents.google.com/patent/US5004953 | 1989 | Red-emitting fluorescent lamps for darkrooms already known; fluorescent lamps run on battery through an inverter already known | READ (red_light 8.6); current one-lamp 90-minute packs [search] |
| LS247 | Wikipedia, "Approach lighting system", https://en.wikipedia.org/wiki/Approach_lighting_system | from c. 1956 | Sequenced flashers running toward the runway, "the rabbit": a travelling light that means "this way" | READ (red_light 8.6); FAA AIM 2-1-1 [search, 403] |
| LS248 | Solemn red: Code of Canon Law, can. 940, https://www.vatican.va/archive/cod-iuris-canonici/eng/documents/cic_lib4-cann879-958_en.html ; Wikipedia, "Sanctuary lamp", https://en.wikipedia.org/wiki/Sanctuary_lamp ; Tate St Ives, https://www.tate.org.uk/visit/tate-st-ives/display/mark-rothko-the-seagram-murals ; Tate Liverpool, https://www.tate.org.uk/whats-on/tate-liverpool/mark-rothko-seagram-murals | 1983 (code); murals 1958-59 | One continuous red lamp; dark reds in reduced light against grey walls, "solemn and meditative" | READ (red_light 5.1, 5.2); *Tate Etc.* 7 [search] |
| LS249 | Seeing red: Wikipedia, https://en.wikipedia.org/wiki/Bezold%E2%80%93Br%C3%BCcke_shift , https://en.wikipedia.org/wiki/Helmholtz%E2%80%93Kohlrausch_effect , https://en.wikipedia.org/wiki/Purkinje_effect , https://en.wikipedia.org/wiki/Adaptation_(eye) , https://en.wikipedia.org/wiki/Chromostereopsis ; StudioX ACES notes, https://sharktacos.github.io/OpenColorIO-configs/docs/chroma.html ; FAA Safety Team (AIM 8-1-6), https://www.faasafety.gov/SPANS/event_details.aspx?eid=48017 ; CFI Notebook, https://www.cfinotebook.net/notebook/aeromedical-and-human-factors/the-eyes-and-vision | current | Bright red drifts toward yellow; saturated colours look brighter; red at night keeps dark adaptation and washes out red marks; ACES skews bright red to orange | READ (red_light 5.4-6.2); the URP grade itself read in the package source on disk; AIM 8-1-6 VIA the FAA page (403) |
| LS250 | Photosensitive limits: W3C, Understanding WCAG 2.2 SC 2.3.1, https://www.w3.org/WAI/WCAG22/Understanding/three-flashes-or-below-threshold.html ; WCAG 2.2, https://www.w3.org/TR/WCAG22/ ; WCAG 2.0 and 2.1 glossaries, https://www.w3.org/TR/WCAG20/ , https://www.w3.org/TR/WCAG21/ ; ITU-R BT.1702-3, https://www.itu.int/dms_pubrec/itu-r/rec/bt/R-REC-BT.1702-3-202311-I!!PDF-E.pdf ; G. Harding et al., *Epilepsia* 46(9):1423-1425, https://research.aston.ac.uk/en/publications/photic-and-pattern-induced-seizures-expert-consensus-of-the-epile/ ; Microsoft, Xbox Accessibility Guideline 118, https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/118 ; Wikipedia, "Photosensitive epilepsy", https://en.wikipedia.org/wiki/Photosensitive_epilepsy ; Wikipedia, "Denno Senshi Porygon", https://en.wikipedia.org/wiki/Denn%C5%8D_Senshi_Porygon | 2005-2026 | General and red flash definitions, rate and area limits, the one-way pattern exemption, the 5 s caution, the older red metric | READ (red_light §7, story D1, motion M9, pacing C13; BT.1702 read by red_light, [search] in story and motion; the *Epilepsia* paper as an abstract); the Ofcom note [search, 403] |
| LS251 | Unity script reference: `GraphicsSettings.lightsUseLinearIntensity`, https://docs.unity.com/en-us/engine/current/script-reference/unityengine/rendering/graphicssettings/lightsuselinearintensity ; `MaterialPropertyBlock.SetColor`, https://docs.unity.com/en-us/engine/current/script-reference/unityengine/materialpropertyblock/setcolor | current | Light colours multiplied in linear; SetColor takes sRGB and converts it in a linear project | READ (red_light 8) |

**On this disk** (read in ranges by the research stages and this writer; paths from the project root; FR3D = `Frontrooms3D`):
- Code: FR3D/Assets/Scripts/FrontRoomsMap/`FrontRoomsRunRooms.cs`, `FrontRoomsLevelProfile.cs`, `FrontRoomsMap.cs`, `FrontRoomsMapWorld.cs`, `FrontRoomsExitPlanner.cs`, `FrontRoomsModuleUnits.cs`, `FrontRoomsMapHunter.V4b.cs`; FR3D/Assets/Scripts/`FrontRooms3DGame.cs`, `FrontRooms3DGame.RunRooms.cs`, `FrontRoomsRoomStream.cs`, `FrontRoomsQuality.cs`; FR3D/Assets/Scripts/Rendering/`FrontRoomsRunRig.cs`, `FrontRoomsRunRigBinder.cs`, `FrontRoomsLook.cs`; FR3D/Assets/Resources/Rendering/`FrontRoomsPost_Run_B.asset`, `FrontRoomsPost.asset`; FR3D/Assets/Levels/`FrontRoomsLevel0.asset`; the URP grade and ACES sources in FR3D/Library/PackageCache (ported to a scratchpad tool, not in the project).
- Project docs: FR3D/Documentation/`MAP_GENERATION.md`, `RELAY_PURSUIT_REDESIGN.md`, `LEVELS_AND_ENTITIES.md`, `system_design/systems.json` and `README.md`; research/`relay_pursuit/30_narrative.md`; research/narrative/`40_floors_exit_tutorial_strings.md`, `90_alarm_devices_and_door_strings_signoff.md`, `95_world_text_localization.md`; research/room_visuals/`03_run_research.md`, `10_run_directions.md`, `11_run_dir_house_lights.md`, `20_run_red_ward.md`, `21_run_fix_1014.md`, `40_for_red.md`, `50_alarm_zone_tell.md`, run_b_production/`10_plan.md`, `20_build.md`, `60_production_66.md`, `61_for_sound.md`, and the film `run_b_20261031.mp4` with its contact sheet (frames looked at, never played); research/door_break/`50_rethink.md`, `51_prototype.md`, `52_production.md`, `54_dust_grit.md`; research/zone_lamps/`10_run.md`, `40_cursor_brief.md`; this folder's `10`, `20`, `40`, `63`, `70`.
- Line records: Documentation/Takeover_2026-10-18/`RULES.md`, `VISUAL_STANDARD.md`, `handoff/level_space.md`, `handoff/archive/visual_history.md` (grep), `handoff/archive/bathroom_history.md` (115-135), `LANDINGS.md` and `RED_ACTIONS.md` (grep); transcript `7da6f3fe` through `session_digest.py` (never read whole).
- The drafts: `~/FrontRoomsVisualWork/level_space/drafts/run/` `today.txt`, `pacing.txt`, `endless.txt`, `motion.txt`, `red_light.txt`, `story.txt` (all DONE; read by this writer in ranges, with their own source tables T, PC/PF, E, M, RL and ST/P).

## 12. Limits

- **Nothing was built, rendered or measured.** Times and distances are arithmetic from the code's constants (sprint 5.5 m/s, walk 3.2 m/s, a 5 s bar, chase 4.2-4.4 m/s); gait changes are taken as instant (the controller's acceleration was not re-read, `pacing.txt` G5). The tier minutes extrapolate one bot run (seed 2554). The 12- and 14-cell fit rates are not measured (§3.2). Light values come from a port of the URP grade with assumed albedos and no shadows or bounce: targets, not settings. The motion bodies' minimum travel times and piece counts (T2's 90 blocks, T4's 280 pleats, the pocket depth) are estimates.
- **The chase-dive numbers (§4.7)** assume a full bar at the arch and the Relay running the same straight line; the real exposure needs 系统设计's bot round. Whether R2's escape happens with today's v2 numbers is not measured either.
- **Sources.** This writer did not fetch the web sources again; the marks are the research stages'. The critic stages (logins B and G, 11-03 21:4x-23:xx) re-read the WCAG 2.2 glossary, XAG 118, ITU-R BT.1702-3, Booth's slides, Grip's blog, Boris the Brave, the two ETB guides, Nerdist, the Wikipedia pages on Little Nightmares II and Poltergeist, Pocket Gamer, Game Informer's Miyazaki interview, Fextralife's Mimic page, ESTNN and Destructoid, looked again at the ETB frame and the code, and corrected the claims they did not support (log: `~/FrontRoomsVisualWork/level_space/drafts/run/critic71.txt`). Not reached or only searched: the original Level ! page and backrooms.fandom.com, the Hades chamber pages, how Remedy animated Control's shifts, the GMTK Clockwork Mansion transcript (via [LS37]), the broadcast pattern thresholds in the Ofcom note, a 1990 accordion fire door's stack depth, Alien's and The Terminator's red frames (no download), 1990 nurse-call domes, Poltergeist's actual rig, Inception's corridor length, RE4's pacing beyond an encyclopaedia and an editorial. No page pinned the "隧道" Red remembers from Little Nightmares to a scene; §8 decision 4 asks Red which. Red named no fan game for the red; ETB is this page's inference, and §8 decision 6 asks.
- **Red's words.** Message 2 is cut off ("就比如说玩家进入到这个"); "ramp" has two readings; which lamp effect Red likes (RESET or the pools) was unconfirmed until Red's answer of 11-03, read as RESET (§0.1); "被欺骗" has two readings; Red's own words for the PT4 rule were not found.
- **This page's own synthesis** (no single draft proposed it): Order T (the receding room trips at 9 m); the head's track as a tell before commit; the blind doorway on the release side; the band and the magnets at the trip in a receding room (§5.7); the door entrance from tier 3 (between `story.txt`'s tier 2 and `pacing.txt`'s tier 5); the teaching room with the pulse off as an option; SP7's movable end as the length key.
- **Cross-draft tensions settled here, with reasons:** `pacing.txt`'s single 6 m/s move against `motion.txt`'s steps (steps recommended, with the 6 m/s glide when a runner closes in; the single move stays Red's alternative in §8 decision 4, since at these speeds 越退越远 and 重组 trade against each other); `pacing.txt` D4's "only when the Relay is Away" gate (dropped under rule 11); `story.txt`'s honest-machinery rule against T2 (left to Red, T2 recommended on T1's visible machine); `motion.txt` M9 against `red_light.txt` on red dips (`red_light.txt`'s reading, with M9's one-way fades kept for the recede); `story.txt`'s outdated tier rule (corrected to the code).
- **Rule 11 judgments left to 系统设计:** the restoral watched through the goal window; C's tide; the keep-away glide (it reads the player's position only).
- **Not changed here:** `70` (SP5's three conflicts, §10.1, were fixed in `70` itself by this line on 11-03 22:5x, and SP7 entered it as §9 on 11-04); no game code; no other line's files. No sound was played or designed beyond event lists.
- **Photosafety** is arithmetic until the capture: Look B and the T2 and T4 bodies stand or fall on the Harding FPA or PEAT measurement.

