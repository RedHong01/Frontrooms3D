# 01 · The real 1990 security desk, and what our station takes from it (叙事设计, 2026-10-22)

**Status: v1.** This is the curated version. Research agent A's full notes are in `~/FrontRoomsNarrativeWork/station/A_practice.md` (outside the repo), with about 110 sources, 45 media links and a gaps list. `P`-numbers here are that file's S-numbers and are listed in `SOURCES.md` §P.

**Tags:**
- [read]: page fetched and read;
- [search]: snippet only;
- [memory]: unverified, a lead and not a citation;
- (brand: research only): a real name used to identify an object; the game never shows it.

Nothing was downloaded.

## 0. In one paragraph

Around 1990 the night "security desk" of a US store or office building was **one contract guard's post**.
- **The kit:** a phone, a shift log, a post-orders binder, a key cabinet with a sign-out sheet, a watchclock, and in bigger buildings two or three black-and-white CCTV monitors and the fire-alarm annunciator [read P01, P13, P53, P62].
- **The desk was empty for part of every hour.** The guard carried the clock on his rounds to the key stations [read P53, P55].
- **His own small TV** could pull in the local UHF independent, because every set sold after 1964 had to tune UHF [read P40]. His post orders usually said no TV.
- **The codes say almost nothing about the guard desk.** The code-defined object is the high-rise *central control station*, a separate room for the fire department [read P01].

So the station reads truest as a **post: a working place someone just left**, not a reception desk.

## 1. Forms, sizes, materials

**Six real "desks" (do not conflate them; P01, P05, P30–P35, P93):**

| Type | Where | Kit |
|---|---|---|
| Lobby security desk or console | ground-floor lobby, facing the entrance and the elevators | phone, register, CCTV, door release; annunciator and elevator recall switch nearby |
| Lobby reception or concierge desk | single-tenant lobby, hotel | phone or attendant console, badges, directory |
| Floor reception counter | inside a tenant suite door; day-staffed, empty at night | key phone, waiting chairs, magazines |
| Guard booth | parking or yard gate | shelf counter under windows, phone, log, gate button |
| Receiving or dock desk | the back of a store or warehouse, at the dock doors | receiving log, time clock, phone, dock-door buttons. Often the night guard's post or a round stop |
| High-rise central control station / fire command station | a lobby room approved by the fire department | the code list in §4 |

- **A furniture store with stock rooms would have one guard post** (main office, employee entrance or dock), not one per floor [memory/inference].
- Our "one per floor" is a game convention. It reads truest if each floor's desk borrows a **different** real type (see `10_directions.md`, direction C).

**Dimensions to model with:**

| Element | Value | Source |
|---|---|---|
| Transaction (visitor) ledge | ~42 in (1.07 m); guides 42–48 in | [read, post-era] P33; [search] P34 |
| Guard's work surface | 29–30 in (0.74–0.76 m) | [read, post-era] P33 |
| Ledge depth / desk depth | ~10–14 in / ~30 in + ledge | [memory] |
| Single-guard desk length | ~6–10 ft (1.8–3.0 m) | [memory] |
| ADAAG §7.2 accessible portion | ≤ 36 in high, ≥ 36 in long. Only on desks built after the 26 July 1991 standards (new construction from 1993), so a 1990 desk lacks it | [read] P20 |
| Knee clearance (ADAAG §4.32) | ≥ 27 × 30 × 19 in | [read] P20 |

`Kit_OfficeDesk` (1.524 × 0.762, top 0.741) already matches the work surface.

**Materials and colours, 1985–93 back-of-house** [memory unless tagged]:
- desks in oak, walnut or teak veneer, or grey or "bone" laminate (colour-through laminate was popular with designers in the 1980s [read P36]);
- putty or almond plastic on phones and terminals;
- black or grey metal CCTV cases;
- burgundy, navy or grey upholstery;
- 2×4 prismatic troffers;
- grey office tones, with mauve and teal as the decade's stereotype pair [read P37, P38].

## 2. Equipment and what it says

| Item | The period facts | Station use |
|---|---|---|
| **The guard's TV** | Desk sets were 5–12 in black-and-white portables: a telescoping VHF rod, a UHF loop, two rotary tuners (UHF 14–83) [memory]. Sony's 2 in Watchman reached North America about 1984 (brand: research only) [read P39]. UHF tuners were mandatory after 1964 [read P40] | `Kit_CRTTV` (a 21 in flat-square set, 1987–99) is the approved beacon TV. It reads as "a set brought from home or the break room", which suits a post where TV was against the rules |
| **CCTV** | 9–12 in black-and-white monitors in metal cases on a sequential switcher, dwelling seconds per camera, plus a time-lapse VCR. Colour CCTV and quad splitters were rare at small posts [read P26; search P27–P29] | Dressing only, and still (V7 is not chosen). Never a recorder with a date overlay: that is Kane Pixels' grammar (`02_refs.md` §0) |
| **Key telephone** | The 1A2 code (brand: research only): lamp off = idle, steady = in use, **slow flash = ringing, fast wink = on hold** [read P41] | Optional detail: one line lamp **winking on hold**, a call nobody came back to. It must be a slow wink under 3 Hz on a tiny lamp, with **no bell**, because the lock-on chime is the game's only tonal sound |
| **Intercom and door release** | a master station, a door-strike button under the counter edge [read P43; memory] | A dead button under the ledge (no gameplay) |
| **Radio and desk charger** | a brick handheld in a drop-in charger with a charge light, and a spare battery [search P45; memory] | **An empty charger slot: the radio went with him** |
| **Fire-alarm annunciator** | A red lamp per zone, amber trouble, green power. Alarm silence, trouble silence, reset, a held lamp-test, behind a locked glass door. It cannot say *which* device tripped. *Fire Engineering*, 1 Apr 1990 [read P13] | **Colour conflict:** in our game amber TROUBLE is the sanctuary lamp (RELAY §10) and red is the ALARM ZONE card (RELAY §6). So an annunciator at the station shows **only its green power lamp**: dark zones, never lit by gameplay. A lit zone would be a radar |
| **Elevator recall key switch** | Firefighters' recall since the 1973 elevator code; a lobby Phase I switch [read P19, P21] | Not on our floors (no elevators). Skip it |
| **Key control** | A shallow steel wall cabinet, beige or grey, with numbered hooks and tags and an index chart; a **key sign-out log** beside it. Stair and master keys on a separate board or in a locked drawer [search P51, P52; memory] | `Kit_KeyCabinet` (steel almond, 24 hooks) and `Kit_KeyHookBoard`. The stair key is the **one key on its own board**, apart from the cabinet: why it matters is visible |
| **Watchclock (guard tour)** | A ~5 lb clock in a leather pouch on a shoulder strap. Numbered keys on 12 in chains in small hinged boxes along the route (12–40 stations). Each key stamps a time tape; **a missed hour shows as blank paper**. Typically hourly at night. A monitored tour more than 15 min late raised a **delinquency** signal at the central station [read P53, P55, P14; search P57] | **The clock is gone with him.** Its empty hook, or the empty pouch strap, is the strongest single prop. Chained key boxes along corridors are a period-true trail (Phase 2, `20_layout.md` §6) |
| **Paperwork** | Post orders (general and special) in a three-ring binder. A bound visitor register. A clipboard Daily Activity Report with timed entries. A phone list under the desk glass [read P62, P65; search P63, P64; memory] | **The guard's voice is paper** (§5): the log, the orders, the sign-out sheet |
| **Personal items** | A D-cell flashlight (from 1979) [read P66]; a newspaper [search M24]; a radio, coffee, a lunch bag, a paperback, the uniform jacket on the chair back [memory]. Ashtrays were common at a closed night post; workplace bans were spreading in 1986–90 [read P67–P69] | Jacket on the chair, cup, newspaper, an ashtray **under a NO SMOKING sign** (a period-true irony). The flashlight is **missing** too: he took it |

## 3. Staffing: why the desk is empty

- **Who:** mostly contract guards by 1990: about 1.5 million in private security, about 1 million of them contract, roughly three times the public police [read P70, P73; search P71].
  - Complaints: low pay first, then boredom, weak training and high turnover [search P72, P74].
  - Pay ran near the minimum wage ($3.35 → $3.80 from April 1990) [memory].
- **Shifts:** the graveyard shift, 2300–0700. A small store had a night watchman from closing to opening [read P55; memory].
- **Rounds:** hourly at night, 12–40 stations, 20–40 minutes each, so a one-guard desk stands empty for a third to two-thirds of the night [read P53, P55; inference].
- **Who notices he didn't come back:** the central station after 15 minutes (a delinquency call to the desk phone), or the opening shift reading the blank tape [read P14, P55; inference].
- **Culture:**
  - the "rent-a-cop" [search P80, P81];
  - guards found asleep (Penn, 1993–94, post-era [read P78, P79]);
  - the bored guard with the forbidden TV.
- **Do not make him heroic, and do not echo a real death.** A 1986 OSHA file records a night guard who died going back in to phone the fire department [search P96]. We use no real case.

## 4. Codes that shape our fiction

| Code | What it says | What it gives us |
|---|---|---|
| **UBC 1991 §1807(f)** central control station (B-2 office buildings with floors over 75 ft) [read P01] | A ≥ 96 sq ft room approved by the fire department, holding 12 items, including: voice-alarm and PA panels, the FD comms panel, the annunciators, the elevator status display, **controls for unlocking all stair doors at once**, standby-power status, an FD phone, schematic floor plans, a work table | The building *could* unlock every stair door from one place, but nobody is at that console. Our floor desk is **not** that room. It keeps only the honest half: the key |
| **UBC 1991 §1807(j)** [read P01] | Stair doors locked from the stair side must unlock together on a signal from the central control station. Where stair doors may be locked, a two-way phone at least every fifth floor | Real stair-door locking is from the **stair** side. Ours is locked from the floor side, so **this building is out of code**. EGRESS says the building is honest about itself: the placard admits it (`STAIR DOORS ARE LOCKED.`) |
| **UBC 1991 §3304(c), exception 1** [read P01] | A Group B main exit may be key-locked if it carries **"THIS DOOR TO REMAIN UNLOCKED DURING BUSINESS HOURS"** | A real 1990 sign for "why this door is locked tonight". **Since 2026-10-22 15:2x (Red: option 1, the station is the first room), it hangs on the first room's locked main entrance**, the doors the code wrote it for, no longer on the stair door (`narrative/40` §9). It is night, and business hours don't come |
| **UBC 1991 §3304(e)** delayed-egress locks (B-2 only, which includes retail) [read P01] | A 15 s release that sounds an alarm at the door, signed "KEEP PUSHING. THIS DOOR WILL OPEN IN __ SECONDS. ALARM WILL SOUND." | The period grammar of "taking this sets off the alarm". Borrowed for the key board's plate (§6) |
| **UBC 1991 §702(c)2** [read P01] | Retail stock rooms are separated from the sales floor by one-hour walls and fire doors, unless small or sprinklered | The front rooms and back rooms are a code seam: rated walls and steel doors between showroom and stock |
| **NYC Local Law 5 (1973)** [read P05, P07; search P12] | A lobby fire command station with a fire safety director. **Fire warden teams on each floor**, and a **warden station within view of each floor's elevator lobby** | **A period-true reason for one station per floor.** If Red picks the role name `FLOOR WARDEN` (叙事 Q2), the station is the floor's warden post too |
| **ADAAG 1991** [read P20] | Counters: a portion ≤ 36 in high and ≥ 36 in long, in new construction after its 1991 publication | A 1990 desk has no low section (pre-ADAAG): a small dating truth |

## 5. Our fiction, grounded (narrative synthesis; proposal)

- **The station is the floor's night post.** The building's plan calls it the `SECURITY DESK`.
  - Under LL5-style practice it is also where the floor's warden reports (if Red names the role).
  - One per floor, near the floor's main route, like a warden station in view of the lobby.
- **The guard is out on his round, and he has been gone a long time.**
  - Gone with him: the watchclock, the radio (an empty charger), the flashlight.
  - Left: the jacket on the chair, the coffee, the open log, the TV he wasn't supposed to have, tuned to WTCZ-TV 61.
  - We never say what happened to him. There is no body and no blood, and the hunter is never shown as the guard.
- **Floor 1: the station is the first room** (Red, 2026-10-22 15:2x, 叙事设计 chat: "按推荐，第一个房间选方案 1"). It is the building's security post at its entrance: the one kept room (Red's walls 2 · Post Room, 15:4x: blue-grey paint, crash rail, VCT, the same on every station), with the yellow back rooms beyond. The fiction is in `NOTE_1022_first_room_narrative.md` §1–§2. Floor 2+ stations are the night posts in the back rooms.
- **Why the stair key is here.**
  - After hours the stair doors are locked. That is out of code, and the building says so plainly. The key hangs on its own board at the desk, under the sign-out sheet.
  - The main entrance's plate says it should be unlocked during business hours (moved from the stair door, 2026-10-22, option 1).
- **Why taking it calls the hunter.**
  - The key board is on the alarm panel as a device: lifting the key opens its contact. The panel drops the desk's non-emergency circuit, so the TV dies, and sends its responder to the device (`relay_pursuit/30_narrative.md` §1; tutorial `10_proposal.md` §4.1).
  - The plate warns you honestly, in the building's voice, if you stop to read it (`SILENT ALARM: REMOVAL SIGNALS PANEL`, §6).
- **The clock** (rewritten 2026-10-22 after Red's live-clock rule, in the 游戏视觉 chat at 14:1x: "…如果是旧的我希望新的钟表是可以正常走动，并且呼应现实世界的时间的（钟表只是一点，还有游戏里面的电脑等一系列有时间显示相关的设备的）"; `research/live_clocks/`). Clocks and computers show the player's real local time; dates are the real month and day, in 1990.
  - **The building's clocks are right. Its hours are not.** Whatever the clock says, the building is still running its night: the stairs locked, the desk on its night post, nobody else in. At 3 AM that is ordinary; at 3 PM on a Tuesday it is the wrongness, and nobody says so.
  - The guard went out on the third round of a night shift and never came back. The log is always dated to a night already over (the rule in §6), so he has been gone for hours, and no day shift came to relieve him.
  - The plate `THIS DOOR TO REMAIN UNLOCKED DURING BUSINESS HOURS` stays, now on the first room's main entrance (option 1). At night it explains the lock; by day it is the building breaking its own sign. Either way it is true.
  - **Date rule:** every *printed* date is 1989 or earlier, so it can never lie in the future of the live calendar. Anything that has to look recent (the log, a TV sign-off) is computed from the live date, with weekdays from the 1990 calendar. **Feb 29:** 1990 has none, so a real Feb 29 shows `02-28-90` (a Wednesday), never `03-01-90`: the in-game date never runs ahead of the real one, and the building quietly repeats a day. The shared real-time source applies this once for every device and for the log's date arithmetic. **Future dates** (an announcement such as a memo's next drill) print the **month and day only, never a year**, so in late December nothing reads 1991 (era lock: no printed date after 1990). Weekday and date come from the **in-game (1990) calendar's** next occurrence, so they agree with weekdays shown elsewhere, e.g. `MON 10/22/90`; in late December the next Tuesday is `JANUARY 8` (added 10-22, from 信息设计's terminal mocks, `g0_pages.py`).
- **Hunter tie (Night Shift, Red's pick B):** the hunter is the building's night electrician (`research/hunter/10_hunter_directions.md` §4). Guard and electrician are the two night staff. The log can carry one plain line about a lamp call to a stock room, and nothing more. The player draws the line between them; we don't.

## 6. Strings for the station (DRAFT; 叙事 owns the words, 平面视觉 the type, 游戏视觉 the props)

| Where | String | Note |
|---|---|---|
| Plate over the key board | `STAIR KEY` / `SILENT ALARM: REMOVAL SIGNALS PANEL` | The building's honest warning, in the grammar of §3304(e). **Decided by 教程设计 2026-10-22 12:1x** (`tutorial/10_proposal.md` §4.7): kept, on the **close-reading layer only** (EXAMINE / E · READ, or within ~1 m; not legible from the counter or the first view); it stays after the take so the player can come back and read why; never glows, never changes, the same on every floor. **Wording v2 (12:1x):** the take makes no sound in the room (the set dies, the dispatch is silent), so v1's `ALARMED: REMOVAL SOUNDS ALARM AT PANEL` could read as "a bell will ring here". `SILENT ALARM` says nothing rings and someone is told; `SIGNALS PANEL` keeps the panel as the place that knows. "Silent alarm" was everyday security language well before 1990 [memory]. **Kept knowingly (v1.2, 游戏视觉's critic E16):** "silent alarm" is intrusion vocabulary, and that is the point. The key board is a supervised device on the building's **combination fire and burglar panel**, a type verified from 1971 and 1981 patents (`relay_pursuit/30_narrative.md` §7). Key removal is an intrusion-side signal, so the plate is honest. The fire-side word, `SUPERVISORY`, is jargon most players can't read. Type size: 平面视觉, under the EXAMINE readability rule |
| Key cabinet door | `KEY CONTROL` / `ALL KEYS MUST BE SIGNED OUT` | Period key-control practice [search P52; memory] |
| Sign-out sheet header | `DATE  KEY NO.  NAME  OUT  IN  INIT.` | The stair key's line is the last one, and its OUT is blank |
| Post-orders binder spine | `POST ORDERS` | No company name (no brands) |
| Desk tent card | `PLEASE SIGN IN` | — |
| Visitor register headers | `DATE  NAME  COMPANY  VISITING  TIME IN  TIME OUT` | Rows blank after 6 PM |
| Daily Activity Report (EXAMINE text, if 游戏视觉 makes it readable) | `<MM-DD-90>  NIGHT  DESK`<br>`2300  ON DUTY  D.K.`<br>`0000  TOUR 1 COMPLETE  ALL SECURE`<br>`0100  TOUR 2 COMPLETE  ALL SECURE`<br>`0112  BLDG SVCS CALLED  LAMPS OUT STOCK RM 3`<br>`0200  TOUR 3` | **Date (live-clock rule):** the night shift that began at 2300 on the day before the in-game date when the in-game time is 07:00 or later, otherwise two days before; so its last entry is always at least 5 h in the past. Times are fixed. The last line has no "COMPLETE". Initials only, never a name. `D.K.` is also a writer in the terminal memo thread. **Decided 10-22 (叙事, for 系统设计's computer_ui):**
  - R.M. and D.K. stay as **two ordinary memo writers, initials only**: R.M. = facilities or building management (drill notices, posted rules), D.K. = the night desk (the same initials as this log).
  - Their dates follow §5.
  - No other occupant, no lure, no hint of the guard's fate beyond the plain duties.
  - The memo rules of `computer_ui/20_design.md` §8.6 apply: no cues, no keys, no door advice before RELAY step 4. `0112` is the only hint of the night electrician. The date is computed by whoever builds the EXAMINE text (游戏视觉's station paper), from the shared real-time source |
| Wall sign over the ashtray | `NO SMOKING` | Period irony |
| ~~EXIT door, maze face~~ → **the first room's main entrance doors** (moved 2026-10-22 15:2x, option 1) | `THIS DOOR TO REMAIN UNLOCKED DURING BUSINESS HOURS` | UBC §3304(c) exception 1 wording [read P01], written for a Group B building's main exit. It explains why the way back out is locked tonight, without a word of fiction |

**Avoided:**
- RUN, HIDE and "relay";
- the word STATION on any sign (Escape the Backrooms, `02_refs.md` §3.11);
- brand names;
- a full name for the guard;
- any line that says where the hunter is.

## 7. What carries into the layout (`20_layout.md`)

1. **It is a post with a counter.** The key is *behind* the counter, so taking it means stepping into the guard's place.
2. **The TV faces the guard's chair, not the door.** The player sees its light on the walls first and the picture last (`02_refs.md` §3.4).
3. **The TV can sit in a sloped well or on the desk's back shelf,** like period monitor wells [memory P31].
4. **The posted evidence is all within 2 m of the chair:** the log, the sign-out sheet and the empty charger. That is the EXAMINE cluster.
5. **The empty desk is the normal state, not a mystery.** The mystery is how long it has been empty: blank tape, the unfinished tour line, the cold coffee.
