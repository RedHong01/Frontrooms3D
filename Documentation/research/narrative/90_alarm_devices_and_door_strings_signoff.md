# 90 · Sign-offs: annunciator, ALARM ZONE, sash, alarm door, body doors, B-floor sheets (叙事设计, 2026-11-01)

Six asks from 10-31, checked against main as it stands today (body doors and Relay v2 live since 10-31 21:01; `sashPry` 0; `alarmDoor` not set). Words are this line's; faces, layout and code stay with their owners. Rules used: `40` header and §1, and `RULES.md` 11.

| # | Ask | Result | Owner of the next step |
|---|---|---|---|
| 1 | Annunciator labels | **Direction A, with strip 7 changed** | 平面视觉 renders |
| 2 | ALARM ZONE card | **Signed as `ALARM` / `ZONE`** | 游戏视觉 flips `CardFaceIsStandIn` |
| 3 | PRY SASH | **Signed; the sash gets a flash** | 关卡设计, at switch-on |
| 4 | Alarm door | **`ALARM WILL SOUND` signed; prompt is `ALARMED`** | 平面视觉 · 信息设计 at P4 |
| 5 | Body doors | **Live drafts signed, with 3 rewords; 4 key-map lines must lose "Relay"** | 交互设计, 设置菜单, 关卡设计, 教程设计 |
| 6 | B-floor sheets | **No change; pairing is right** | none |

## 1. Annunciator labels

| Strip | Words |
|---|---|
| 1 | `LEVEL 0` |
| 2–6 | `LEVEL B1`, `LEVEL B2`, `LEVEL B3`, `LEVEL B4`, `LEVEL B5` |
| 7 | **`SPARE`** |
| 8 | `SPRINKLER FLOW` |
| 9 | `TROUBLE` |
| POWER | `POWER` |

Zone numbers stay on strips 1–8 as in `draft_A` (`1  LEVEL 0`, `7  SPARE`). No number on TROUBLE or POWER.

- **A, not B.** `40` §1 already says "the panel's zones are floors". B breaks two signed things: `LOBBY` is banned (`40` §6, §9), and a label for one floor's rooms is false on a generated floor whose plan may differ, on every desk of every floor.
- **`STAIR A` comes off.** `40` §1 and §5: the stairwell is safe because "a stair enclosure belongs to none of them". A lens marked STAIR A would make the safe zone an alarm zone, and a careful reader would be right to distrust it.
- **`SPARE` in its place.** Real panels were bought with unused zones, and leaving the label reading `SPARE` is ordinary trade practice [memory, not source-read; harmless if wrong]. It says the building has room for more floors and never wired them.
- **It stops at B5 on purpose.** The panel states what it was wired for. A player on B6 reads a panel with no lens for the floor they stand on, the same quiet wrongness as `THIS FLOOR MAY DIFFER FROM PLAN`. Nobody says so. It implies a bottom only to someone who has not yet passed it.
- Every desk on every floor carries the same panel (`80` §2). Nothing lights in play (`station/30_look.md`: a lit zone would be a radar).

## 2. ALARM ZONE card

`ALARM` / `ZONE`, two lines, no zone number, no device line: signed.
- It says what the room does before you commit, as RELAY §6 asks, and it never reads as EXIT.
- `ALARMED AREA` is closer to the game's prompt word, but it would shrink the caps below 42.9 mm, and the phone read is already only 1.3 m. Legibility wins.
- The panel's zones being floors does not clash: the card marks a detector's room inside a floor. HUD `ZONES` is a run count that no one reads against a red sign.
- In-world English, no language row (Red, 10-30 20:47: "world stays English").

## 3. PRY SASH

| Slot | String | Status |
|---|---|---|
| Latch, hold | `HOLD E  ·  PRY SASH` | Signed |
| Latch, tap mode | `TAP E  ·  PRY SASH` | Signed |
| Foiled latch | `HOLD E  ·  PRY SASH  ·  ALARMED`, `TAP E  ·  PRY SASH  ·  ALARMED` | Signed |
| Sash gives, flash, desktop | **`SASH OPEN  /  WALK INTO THE FRAME TO CLIMB THROUGH`** | New |
| Sash gives, flash, handheld | **`SASH OPEN  /  WALK INTO THE FRAME\nTO CLIMB THROUGH`** | New |

- **Never "(quiet)".** The spec's word is a design note. On screen it would be advice, and an 8 m pry with +8 Attention is not quiet.
- **Yes to the flash.** A player whose first window is pried has never seen the glass flash and has the same question: what now? It is a verb hint, the class `tutorial/10_proposal.md` keeps. The caption `[SASH SLIDES UP]` stays.
- **Rows owed at switch-on:** `PRY SASH  ·  ALARMED`, both flash lines. `PromptWords` translates everything after the first dot as one key.

## 4. Alarm door

- **Placard: `ALARM WILL SOUND`, one line. Signed.** Never the word EXIT (`51`, C41), so `EMERGENCY EXIT ONLY` from `relay_pursuit/30` §4 stays off.
  - True: the device always sounds at once. The ban on "SOUNDS" in `handoff` §6 is about the silent key plate, not this door.
  - **Era:** the device is verified for 1977 and common by 1986 (`relay_pursuit/30` §7). The sign text is on Detex's current EAX-500 as "Emergency Exit/Alarm Will Sound" [search, 2026-11-01, https://locksmithledger.com/home/article/10231131/new-detex-exit-alarms-eax-500-and-eax-2500]. **The exact pre-1990 wording stays UNVERIFIED.** Accepted: three plain words, no brand, and nothing dates them.
  - In-world English, no language row. Lettering goes to 平面视觉 · 信息设计 when P4 starts.
- **Prompt: `ALARMED` alone** (Red's 10A, `doors/20_design.md` §4.6). Body doors are live, so this is what the player sees, and the row exists. `E  ·  OPEN DOOR  ·  ALARMED` is signed only for the path with body doors off; no row needed unless that path ever ships.
- Locked alarm doors keep the key's words. After the trip the door is a plain body door.

## 5. Body doors

Rows are in main as "draft 2026-10-31" and in the four-target build. Checked in `FrontRooms3DGame.BodyDoors.cs`, `FrontRoomsMapWorld.BodyDoors.cs`, `FrontRoomsSettingsMenu.cs:314-315`, `FrontRoomsKeyMapView*.cs`.

**Signed as they stand:** `SLOW · QUIET STEPS`, `HOLD · QUIET STEPS`, `HALF TILT · QUIET STEPS`, `Slow footsteps are not heard. Doors still make a sound.`, `STEP BACK  ·  SHUT DOOR`, `STEP BACK  ·  SLAM DOOR`, `ALARMED`, `BLOCKED`, `SEALED`, `DOORS`, `BODY`.
- `STEP BACK` is one string on every device: S and a stick pulled back are both a step back. No per-device motion words.
- `BLOCKED` shows only at contact with the held leaf, so it is touch, not a radar (rule 11).
- `E  ·  OPEN DOOR` and `E  ·  SHUT DOOR` are retired for map doors: only the body-doors-off path still returns them. Tools and bots keep them. No strings for a USE OPENS DOORS setting (7B).

**Changes:**

| # | Where | Now | Change to | Why |
|---|---|---|---|---|
| D1 | `FrontRooms3DGame.BodyDoors.cs:131` + tables | `WALK INTO IT` | **`WALK INTO DOOR`** | In this game "it" is the hunter (`DOORS IT BROKE`, `IT  23 M`). The noun also matches the legend and the pull's prompt |
| D2 | `FrontRoomsSettingsMenu.cs:315` + tables | `Walk into a door to push it. Turn, aim at the leaf and step back to pull it shut. USE only unlocks.` | `Walk into a door to push it. Turn, aim at the door and step back to pull it shut. USE only unlocks.` | "Leaf" is carpenter's and design-doc language |
| D3 | not built | (E on an unlatched leaf shows nothing until primed) | **`STEP BACK TO SHUT`**, the 1.0 s hint of `20_design` §1.9 | Signed for when 交互设计 / 关卡设计 add it; today the fallback is `""` |
| D4 | not built | legend draft `WALK INTO DOORS · RUN BARGES · STEP BACK TO SHUT` | **`WALK INTO DOORS  ·  STEP BACK TO SHUT`** | Never RUN in teaching text (`40` rules). The barge teaches itself the first time you sprint at a door, and the SLAM prompt names its cost. Placement and measure: 教程设计, 平面视觉 |

**Key map, live now and wrong. Player text says "Relay" (`40`: never), so these change whether or not the doors do:**

| # | Where | Change to |
|---|---|---|
| K1 | `FrontRoomsKeyMapView.cs:280` SLOW | `Hold it to walk slowly. Slow steps are not heard; a normal walk is. A door still makes a sound.` (also carries 5A's "doors still click") |
| K2 | `FrontRoomsKeyMapView.cs:283`, `.Pad.cs:188` SPRINT | `Hold it to run, forward only. Running is loud: it carries further than a walk.` |
| K3 | `FrontRoomsKeyMapView.cs:272` END RUN | `In the pause, ends the run and goes straight back to the title; nothing asks first. Once you are caught, the same key is TRY AGAIN.` |
| K4 | `FrontRoomsKeyMapView.Pad.cs:191` MOVE | `The left stick walks. Tilted less than halfway it is the slow walk, whose steps are not heard; further, a normal walk, which is.` |
| K5 | `FrontRoomsKeyMapView.cs:268`, `.Pad.cs:174` USE | `Keys, locks and anything you can read. At a window, hold it to break the glass; BREAK GLASS on the CONTROLS page can make that a tap.` ("Doors" is false under 1C) |
| K6 | `FrontRoomsKeyMapView.cs:868`, `.Pad.cs:324` | `{0} OR ESC BACK`, `{0} OR {1} BACK` (Now `STEP BACK`, which the pull's prompt uses for a different act. `.Pad.cs:176` made this fix for the pad's BACK row and missed these) |

K1 and K4 drop "about 9 m": the figure is the hunter's, and the v2 hearing model is not this line's to quote. If the old `UNHEARD` path stays for body doors off, it keeps its own branch and takes K1's first two sentences.

## 6. B-floor sheets (A35)

**No change.** Placard README :81-84 (516617fa pairing) and `placard/60_landing_copies_bs6.md` §1 agree with `40` §3: `map.FloorHasStation && map.ExitLocked` hangs `sheetA_B<n>`, a no-fit floor hangs `sheetA_phase0_B<n>`, chosen once per floor. With BS4 live, B floors with a station show the desk, the padlock and `KEY AT SECURITY DESK.`, all true there. `40` §3's "today that is LEVEL 0 only" is updated in place (v1.5).

## 7. Door lesson, touch legend (DL2, DL3 from `tutorial/20_requests.md`, signed 2026-11-01)

| # | Ask | Result |
|---|---|---|
| a | With body doors on, `USE · DOOR` leaves touch legend row B and D4 `WALK INTO DOORS  ·  STEP BACK TO SHUT` takes its place; no bare `USE` entry | **Signed.** Under Red's 1C, USE never opens or shuts a door, so `USE · DOOR` would be false in a body-door run; D4 is the line that is true. A bare `USE` entry is not needed: desktop's `E  USE` already says it, and K5 (`USE`: keys, locks, anything you can read) is where the touch key map explains it. Row B stays two entries. Body doors off keeps `USE · DOOR` unchanged |
| b | With body doors on, touch row A reads `SLOW · QUIET STEPS` (`40` §4) instead of `SLOW · UNHEARD` | **Signed.** Already signed in `40` §4 (Red's 5A, `90` §5); the defect is that `l.Slow` is set once at build, so the legend never switches. Body doors off keeps `SLOW · UNHEARD`. Whoever lands DL2 adds `SLOW · QUIET STEPS` as a key row beside D4 in the nine tables (`passed_keys.txt`, `language_tables.py check`), after 设置菜单's CLAIM; the zh, ja and ko words are checked against the faces it re-cut |

No new string, no other wording change. Owners: 交互设计 (DL2, the touch legend and the driver check at `:3126-3127`), the table rows with whoever lands DL1 and DL2.

## 8. Settings: the RESOLUTION note for AUTO (设置菜单, 2026-11-03)

`FULL WINDOW` becomes **`100 %`**. It does not fit the desktop AUTO segment (about 79 px against 67).
- It is true: AUTO draws at the window's full size, and it pairs with FIXED's `50–100 %`.
- No translation: numerals and a percent sign, like `50–100 %`, so it is the same in all ten languages.
- `NATIVE` would claim the display's native resolution, which a window is not. `WINDOW` names the place, not the size.
