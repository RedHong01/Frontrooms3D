# NOTE 2026-10-22 16:4x · 叙事设计's review of `61_for_red_first_room.md` (story and strings)

Reviewed by 叙事设计 (account B, `local_12e8bc8c…`) against `narrative/40_floors_exit_tutorial_strings.md` §1 and §9, `station/01_research.md` §5–§6 (the live-clock time model) and `20_layout.md` v1.4. **Verdict: OK to go to Red after R7–R11.** The layout in §3 is accepted (20 v1.4 §4.5, Plan FD). Each fix is a small wording change; nothing changes the build.

| # | Where in 61 | Now | Change to | Why |
|---|---|---|---|---|
| R7 | §1, "前 10 秒", last bullet | "回头看，门上挂着牌子：营业时间内此门不锁。现在不是营业时间。" | "回头看，门上挂着牌子：`THIS DOOR TO REMAIN UNLOCKED DURING BUSINESS HOURS`（营业时间内此门不得上锁）。可它锁上了。" | Live clocks (Red 14:1x, 游戏视觉 chat): the clock may say 3 PM. The time model is "the clocks are right, the building's hours are not" (`01` §5). We never claim it is after hours; the lock is the wrongness |
| R8 | §1, "第一分钟", the take | "报警面板上的绿灯还亮着（警报已经发出去了）" | "报警面板上的绿色电源灯还亮着（面板醒着）" | The lit lamp is the POWER jewel (`30_look.md` §4.3), not an "alarm sent" lamp. Its meaning is "the panel is awake" (20 G5) |
| R9 | §1, "第一分钟", last bullet | "这时怪物还**没有**出来，屋里是安全的。" | "这时怪物还**没有**派出：要等你走出这个房间（第 13 节的 N）。" | The first room is not a safe room (20 v1.4 §4.5, condition 3). The stairwell is the only safe zone (Red 15:47). 61 §12 itself says the Relay can enter the room later |
| R10 | §2, "真实的 1990 年", second and third bullets | "1991 年《统一建筑规范》把消防中控放在大堂；纽约 1973 年的 LL5 也一样。所以…第一个房间就是警卫的房间…" | "1991 年的建筑规范还要求一个单独上锁的消防中控室，位置由消防部门批准，通常在消防入口旁边（UBC §1807(f)）。它和正门口的警卫岗是两个房间。所以'进了正门，第一个房间就是警卫的房间'是真实的做法。" | Our fiction keeps the control station as a separate locked room by the fire entrance. It is why the stair doors never unlock in an alarm: nobody is at its controls (`40` §1, after 总控's consistency check). The guard post you start at only has the annunciator |
| R11 | §3 last line and §10, 叙事 row | "这一节是排布提案…" / the 叙事 row's to-do | "叙事已接受岗位墙（`20_layout.md` v1.4 §4.5），附三个条件：岗位墙后面的角落灯是暗档、不能关、南面整面敞开、从 D 看得见；门在绕过柜台时关上，玩家回头正好看到门上的牌子；第一个房间的布置和文字都不能暗示'这里安全'。" Mark the 叙事 row done | Done in 20 v1.4 (G1 measured from D in this module; G2, G5, G7 read as written there) |

**Checked and fine:**
- The header's "唯一'有人打理'的房间" (the one kept room) and "大楼入口处的警卫岗" (the building's security post at its entrance) match `NOTE_1022_first_room_narrative.md` §1.
- The chair turned away from the visitor, `SECURITY` on the desk, `PLEASE SIGN IN`, the blank register line, the plate on D's station face, `LEVEL 0 / FRONT ROOMS`, `PRINTED 11/89`, and the key-board plate `STAIR KEY` / `SILENT ALARM: REMOVAL SIGNALS PANEL` (EXAMINE only) all match `40` §9.
- §13 (N, Red's call): the options are described as the player feels them. I agree with the recommendation, option 1 (the first cell out of E1 or E2).
- §14 downloads are Red's call; nothing of this line depends on them.
