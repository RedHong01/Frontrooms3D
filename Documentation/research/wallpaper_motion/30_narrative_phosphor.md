# EGRESS: the building's exit plan, in its own paper (narrative design)

*Was "The glow ink: narrative design for the phosphorescent underprint (EGRESS)" up to revision 3. The file name is kept so links still work.*

Status: **revision 4, 2026-10-22, 叙事设计.** EGRESS stays the direction. Revision 4 rebuilds it for what v1 ships: walls that never glow, a pattern-native cue that turns only in the player's rooms during a warning or a chase, a route to the security desk and then the EXIT, and no forgery or human marks. Nothing here is code.

**Scope.**
- This document owns the fiction of the wallpaper, its cue and the placard, and the strings the paper needs (A.8). The strings for floors, the EXIT, the stairs, the placard and CAUGHT are in `research/narrative/40_floors_exit_tutorial_strings.md`; the station's are in `research/station/01_research.md` §6.
- **Mechanics belong elsewhere, and nothing here changes them:**
  - when the arrows turn, step or retract: `RELAY_PURSUIT_REDESIGN.md` §9.6 (系统设计);
  - where they point: `research/cue_driver/MAP_ROUTE_SPEC.md` (关卡设计调研), code by 关卡设计;
  - the driver: `research/cue_driver/CUE_DRIVER_INTERFACE.md` (平面视觉 · 信息设计);
  - the cue geometry and the reserved vocabulary: `32_pattern_native_hints.md` §9;
  - the print, its keyframes and slices: `20_level_design_phosphor.md` (LD; 动画和动态图形模型技术).

**History.**
- Revisions 1–2 developed four directions. Revision 3 (2026-10-03) recorded EGRESS as a glowing ZnS:Cu underprint with ten marks, a forger, and frozen ink content (A.11).
- The same week Red redirected the cue to the pattern's own elements (quote below). The overlay ink was never built.
- Revision 4 (2026-10-22) follows Red's answers of that day. Revision 3's §1, §2 and §4 are kept as **Appendix R3**. B–D remain the unchosen directions, and E the comparison.

**Red's decisions.**

| Date (where) | Decision |
|---|---|
| 2026-10-03 (relayed by the wallpaper-print chat) | Direction **EGRESS**. All Relay state text hidden from the HUD (assist toggle only). Chase wave on CHASE only. Visible paper WP03 "Hard edge" (55°). The warning is staged (your lamps, steps, lock-on). Placard YES (Q10). Q11 accepted. EAR kept for later (Q8). Forged FLOW ON: **retired 2026-10-22** |
| 2026-10-03/04 (this line's chat) | Pattern-native cues replace the overlay glyphs: "希望类似于墙纸本身的pattern内部的graphical elements发生了motion transition（形变，旋转，位置移动）". GN07–GN09 replaced ("渲完直接放进figma替换GN07到GN09") |
| 2026-10-07 (C08, systems.json) | Build the arrows: scale-fit A, staggered V1, a 30° ratchet, **no dark gate**, WebGL turns only. The motion was re-opened on 2026-10-22 (C31, open: "再看一次其他方案") |
| 2026-10-21/22 (系统设计 and others; systems.json) | C22: an EXIT per floor, a stair one floor down. C24 = A + A20: the arrows point at the station until the floor key is taken, then at the EXIT. C32: Floor 1's first dispatch arrives 1 s after the take, 22–30 m away |
| 2026-10-22 12:15 (叙事设计 chat) | "全部按推荐（Q1–Q7、照片下载、LOBBY 改名）": forged FLOW retired (C29); scratch-throughs cut; no T4 figure layer; floors `LEVEL 0`, `B1`, `B2`… (C28); the placard line `ON ALARM, A FLOOR WARDEN CHECKS EVERY ROOM.`; CAUGHT + `OCCUPANT ACCOUNTED FOR` |
| 2026-10-22 12:34 (叙事设计 chat) | "Q4 保持 B：墙不发光": **the walls never glow.** Given after Red was told that glow A no longer hid the cue (VL350), so B stands for cost and simplicity |

---

## 1. How to read this

- **§1.1** is what the paper does in v1, in one table.
- **§2** lists the rules the fiction must honour.
- **§4** is the fiction: A.1 pitch, A.2 fiction, A.3 trust, A.4 the moving print, A.5 names, A.6 what the player learns, A.7 writing rules, A.8 strings index, A.9 strengths and risks, A.10 the staged warning.

### 1.1 What the paper does in v1

| Layer | When | What the player sees | Fiction (A.4) |
|---|---|---|---|
| Ambient print faults (keyframes K01–K07: plate shift, block swap, missing slate plate …) | At stage 0, as unseen jumps | The décor is misprinted a little, differently in different rooms and visits | The building re-papers rooms unseen; each re-papering is a fresh run with its own fault |
| K00 hold | While WarnStage ≥ 1 or a chase runs, until every cue cell is back to rest | The paper settles to its clean print | The paper stops wandering when it has something to say |
| **WAY ON** (the calm FLOW route) | Stages 1–2. Only the walls of the player's room set; never in the Relay's room or within 2 cells of it | The lower grey bands click round to point along the route: to the security desk until the stair key is taken, then to the EXIT | Pre-alarm: the panel puts your compartment's paper into its egress register |
| **ALARM ROUTE** (the pressure route) | Chase only: a wave from the player's room set outward at 8 m/s; the rows step | The turned rows step toward doors you can shut, away from it | Full alarm: the plan sends you through a door you can close between you and the origin |
| Retract | When the chase ends | The wave pulls back over 2–3 s to WAY ON (or to rest at stage 0) | The alarm stands down to pre-alarm |
| Nothing | Withdraw, restore, stage 0 | The paper never says "all clear" | The panel resets without telling the occupants |
| No glow | Blackout, dead lamps | Dark walls. Only the placard and the EXIT sign glow | The plan on the wall must be lit to be read; the placard is its phosphorescent copy |

Everywhere: the calm route never reads the Relay and never points at glass; no tier gate; with Reduce Motion (and on WebGL) the bands turn but never step, and the chase wave is one static reveal.

---

## 2. What EGRESS must honour

| Rule (source) | Consequence for the fiction |
|---|---|
| The paper speaks only with its own elements: no overlay ink, no words on the walls (Red 10-03/04; 平面视觉's rules: only existing parts move, quantized, at most one change per roll strip, inside 0.8–2.0 m) | No written wall mark, ever. Words live on the placard, the plates, the station paper and the HUD |
| No dark gate (C08); the walls never glow (Red, 10-22 12:34) | The plan is a daylight object. Darkness neither guides you nor hides you |
| The calm route goes to the station until the key is taken, then to the EXIT; it never reads the Relay and never points at glass (C24, A20, `MAP_ROUTE_SPEC.md`) | It knows the building and its own locked stairs, not the monster |
| It turns only the player's room set at stage ≥ 1, never in the Relay's room or within 2 cells; a turned cell holds when the Relay later comes near (RELAY §9.6) | A local warning about **you**, never a position readout |
| The pressure route runs in a chase only and may read the Relay (LD R6, §9.6) | In full alarm the building knows where the alarm came from |
| The arrows never lie (C29, 10-22) | No forgery, no other occupant's marks |
| Tiers make the Relay better, never the hints fewer (§9.6) | The paper has no tier arc. Escalation lives in the Relay and the floors |
| Withdraw and restore are audio only (§9.6) | The fiction never offers "safe" |
| Dark is never cover: the Relay's sight is a 12 m ray with no light term (LD R13; RELAY A5) | Nothing may imply that darkness hides you |
| All Relay state text is hidden (Red, 10-03) | The paper, the lamps and the sound are the player's only information |
| Era lock 1990 (nothing after 1993); no real brands; IP concepts only (`office_and_film/22_era_lock.md`, `03_ip_canon.md` §5) | 1990 US life-safety English. Phosphor (ZnS:Cu) only on the placard. No Async, no wiki text |
| Photosafety and Reduce Motion (§9.6) | Turns without steps; a static wave reveal |

---

## 3. Anchors (verified; URLs in §9)

### 3.1 Charlotte Perkins Gilman, "The Yellow Wall-paper" (*New England Magazine*, January 1892; public domain)

| Theme | Exact text |
|---|---|
| The sub-pattern | "a kind of sub-pattern in a different shade". You see it "only … in certain lights, and not clearly then" |
| Night and day | "By daylight she is subdued, quiet." By moonlight "it becomes bars!" |
| The front pattern moves | "The front pattern does move—and no wonder! The woman behind shakes it!" |
| Many women | "Sometimes I think there are a great many women behind." "so many of those creeping women" |
| Trying to get out | "nobody could climb through that pattern—it strangles so" |
| Eyes in the pattern | "two bulbous eyes stare at you upside-down" |
| The trail | "low down, near the mopboard": "a long, straight, even smooch". Later, "my shoulder just fits in that long smooch" |
| The ending | "I've got out at last". She is still in the room and has to "creep over him every time!" |

### 3.2 Kane Parsons' *Backrooms*
- **Ep 13 (2022-05-20, unlisted, 33 s)** is titled 9780415263573.
  - Wikipedia calls this the ISBN of Gilman's story.
  - Open Library identifies the exact edition: Catherine J. Golden's critical edition (Routledge, 2004).
  - On screen, the episode shows only highway surveillance footage of a car disappearing into the road.
- **Ep 24 "Static Dead End" (2025-02-12):** stretched wallpaper, irregular floor depth, furniture phased into the walls. Canon treats the paper as a skin that can deform.
- **Ep 16 "Found Footage #2":** green cracks glow in the walls. Our green must read as printed, not as cracks.
- **Ep 1 "Found Footage":** a graffiti-covered wall comes before the creature.

### 3.3 The Exit 8 (2023) / Platform 8 (2024)
- A repeating corridor with a stable baseline: an anomaly means turn back, none means go on.
- The rules are on a sign on the wall at the start.
- Anomalies run from a changed sign message to a power cut.
- Kotake added a "don't get off" warning sign so that one anomaly would not feel unfair. This is our rule: **every lie has a tell**.

### 3.4 Haunted Mansion (Disneyland, open to all guests 1969-08-12)
- The corridor portraits flicker into macabre versions of themselves when the lightning flashes.
- The original mechanism is unverified.
- What we take: the paper shows its other face in a lamp's gasp.

### 3.5 Material and period

*Revision 4: the walls never glow, so ZnS:Cu now matters only for the placard, a phosphorescent evacuation-plan copy charged by its own lamp.*

**ZnS:Cu:**
- Sidot, 1866.
- Green, with a peak at about 531 nm.
- It charges in light and fades over tens of minutes. One pigment measures 25 mcd/m² at 10 min and 2.5 at 60 min.
- Strontium aluminate is about 10× brighter and longer-lasting, and dates from 1993–94.

**Luminous paint.** There are three families:

| Family | How it glows |
|---|---|
| Fluorescent | Only under UV |
| Phosphorescent | Glow-in-the-dark, after light |
| Radioluminescent | Self-luminous |

- **Radium dial paint ("Undark")** was radium plus zinc sulfide. It glows with no charging. It was used through most of the 20th century and replaced by tritium in the 1970s. Old radium clocks were ordinary second-hand stock in 1990.
- **The Radium Girls' poisoning** is a warning, never a motif.

**Egress by 1990:**
- **Tritium EXIT signs:** self-luminous, green, familiar in aircraft. Museum example: about the 1970s.
- **Aircraft floor-path marking:** 14 CFR 25.812(e) (Oct 1984) followed the Air Canada 797 fire (June 1983), with retrofit by 1986. The 1990 strips were **electric**; photoluminescent strips came in 1997.
- **German glow-sign standard:** DIN 67510, from 1958.
- **Charging:** a modern glow exit sign needs light on its face during occupancy and takes about 60 min to charge.
- **NO EXIT:** "NO EXIT" signs (NFPA 101; OSHA "Not an Exit" since 1974) and limits on dead-end corridors.
- **Self-tests:** NFPA 101 §7.9.3 requires 30 s every 30 days and 1.5 h a year. This is the current edition; the 1990 wording is unverified.
- **MS *Scandinavian Star*:** fire on 7 April 1990, 159 dead (one paper says 156). SOLAS was amended in 1992; IMO A.752(18) (1993) allows "photoluminescent indicators".

**Childhood objects that pre-teach the rules:**
- glow-in-the-dark ceiling stars (1970s on, one source);
- Glo Friends (1985–86);
- Hypercolor (1991; it shows handprints, fine under the ≤ 1993 limit but not in a 1990 room);
- blacklight/DayGlo, which glows only while the UV is on, the contrast case;
- Tyler's 1979 random-dot autostereogram; Magic Eye (Japan 1991, US 1993); Brewster's 1844 "wallpaper effect", where repeated patterns float in depth.

**Marking traditions:**
- **Hobo signs** (1870s on): chalked to alert the next traveller.
- **Trail blazes:** a blaze was first a slash in bark; paint is now most common.
- **The wall follower rule:** it fails in mazes with loops.
- **Trémaux's method:** you mark the path as you go.
- **Drag hunting:** hounds hunt an artificially laid scent line. It was popular at 19th-century Oxford and Cambridge and with the Household Cavalry from 1863.
- **Fox-hunting terms:** a "check" is when hounds lose the scent; the fox "goes to ground" in a den.
- **"Relay":** the word first meant fresh hounds placed along the line of a chase (`research/hunter/03` §6).

---

## 4. EGRESS: the building's own exit plan (chosen; revision 4)

*Author: the place, as a machine.*

### A.1 Pitch

The building's evacuation plan is printed into its wallpaper. It is honest about the building and blind to whatever lives in it. When the alarm comes to your rooms, the décor re-registers and points the way out, and every way out leads one floor further down.

**Deck line:** *The wallpaper is a fire-exit plan for a building with no outside.*

### A.2 Fiction

None of this is stated in the game. It is the reason each rule is the way it is, so that strings, props and peers stay consistent.

**The building.**
- A 1990 US office building: `LEVEL 0`, and basements below it, `B1`, `B2`, … (C28), all on one plan.
- After hours its stair doors are locked. That is out of code, and the building says so plainly:
  - the placard's line 4, `STAIR DOORS ARE LOCKED.` / `KEY AT SECURITY DESK.`;
  - the EXIT door's plate, `THIS DOOR TO REMAIN UNLOCKED DURING BUSINESS HOURS` (UBC 1991 §3304(c)).
- The stair key hangs at the security desk on the alarm loop: `SILENT ALARM: REMOVAL SIGNALS PANEL` (`station/01_research.md` §6).

**The paper.**
- WP03's hard-edge chevrons are the décor and the plan at once.
- In normal time they are décor.
- When the panel puts a compartment into alarm, the paper there re-registers into its egress register: the lower grey bands click round and point the route.
- The game never explains this. The player learns it as "the paper points when it is near", because it turns together with the lamp dips.
- The underprint product of revision 3 is gone. Nothing in the walls glows.

**Why the route goes to the desk first** (A20).
- The building knows its stairs are locked, so its plan routes you through key control first and the stair second.
- It is the evacuation procedure for a locked building: honest, and absurd.

**Why it turns only in an alarm, and only around you.**
- The building evacuates only a compartment that is in alarm.
- At pre-alarm (stage 1) the panel tests your compartment's emergency power (the lamps dip) and switches its paper to the plan.
- It never turns where the responder stands: that alarm zone belongs to the responder, not to you.

**The chase.**
- Alarm verified: full alarm spreads outward from its occupant.
- The plan switches to ALARM ROUTE, the doors you can shut between you and the origin, and the rows step.
- It does not know the Relay breaks doors in 2.5 s.

**The responder.**
- The panel's answer to an alarm is the **floor warden**. The placard says so in fine print: `ON ALARM, A FLOOR WARDEN CHECKS EVERY ROOM.`
  - Period fire plans gave wardens exactly this job: sweep every room and account for every occupant (OSHA's emergency-action guidance; NYC high-rise warden teams; `narrative/40_floors_exit_tutorial_strings.md` sources).
- What answers looks like the building's night electrician, Direction B "Night Shift" (`research/hunter/10_hunter_directions.md` §4).
  - The guard and the electrician were the building's two night staff.
  - The guard left on `TOUR 3` at 0200 on a night already over, and never logged it complete (`station/01_research.md` §5–§6).
  - The electrician still answers the panel.
- We never say they are one and the same. The player draws the line.
- CAUGHT reads `OCCUPANT ACCOUNTED FOR`: the warden's report, in the building's voice.

**Blackout** (Red, 2026-10-22).
- The walls never glow. The plan on the wall is a lit object.
- Only two things glow:
  - the placard, which is a phosphorescent copy charged by its own lamp (ZnS:Cu, era-true, §3.5);
  - the EXIT sign.
- The dark makes you worse off, never safer.

**Every exit leads further in.**
- This is now literal. Each floor's EXIT is a fire stair one floor down (C22).
- Behind you the door reads `NO REENTRY`.
- The plan is redrawn floor by floor: the placard's title block reads `LEVEL B1 | SHEET A-2 OF 4` in the stair down to B1.

**Time** (Red's live-clock rule, 2026-10-22: clocks and computers show real local time; dates are the real month and day in 1990).
- The building's clocks are right. Its hours are not: whatever they say, the building runs its night. That is ordinary at 3 AM and wrong at 3 PM, and nobody says so.
- Printed dates are 1989 or earlier; anything recent (the guard's log) is computed from the live date (`station/01_research.md` §5–§6).

**Print faults.**
- The building re-papers rooms unseen: Gilman's "the front pattern does move".
- Each re-papering is a fresh print run with its own one-step fault: a plate shift, a block swap, a missing slate plate.
- Faults never use the plan's moves (`32_pattern_native_hints.md` §9.6, reserved vocabulary): décor never pretends to be guidance.

### A.3 Trust

- Every turn of the paper is the building's, and true about the building.
- The calm plan knows nothing about the hunter and can lead you past it. Its turning only says "near", because it comes with the lamps.
- In a chase it knows where the alarm came from and sends you to a door you can shut.
- **There is no forger** (C29, 2026-10-22). The other occupant survives only as atmosphere: at most one distant door slam far below in the stairwell, never a mark and never a cue (Q7's approved option; 声音设计's to make; `40` §5).
  - The paper never lies, so the player can always trust its direction.
  - The player must judge danger from the lamps and the sound.
  - This keeps one meaning per cue, which the warning design relies on.

### A.4 The moving print

| Print event | Fiction |
|---|---|
| Unseen jumps between ambient keyframes | The building re-papers, and every run has its own fault |
| K00 hold during a warning or a chase | The paper stops wandering when it has something to say |
| The turn at stage 1–2 (WAY ON) | Pre-alarm: the compartment's paper shows its plan |
| The wave and the steps in a chase (ALARM ROUTE) | Full alarm, spreading out from its occupant |
| The retract after a chase | The alarm stands down to pre-alarm |
| Nothing at withdraw or restore | The panel resets without telling the occupants |

### A.5 Names

- **WAY ON**: the calm route (FLOW).
- **ALARM ROUTE**: the pressure route, in a chase.
- These two are the only messages the v1 driver carries (`_FR_WayCells` message 0 / 1, with the pressure dir in a chase).
- Revision 3's EXIT (HERE), NO EXIT (STOP) and OUT OF SERVICE (BREACH) are not in v1. If they return, their names in Appendix R3 A.6 are reusable. GROUND, EAR, THIS WAY ON, SCRATCH-THROUGH and FORGERY are retired.
- **Placard legend** (only when the wall arrows ship, W6; 平面视觉 sets it): two swatches, printed, never glowing:
  - `AS PRINTED`;
  - `IN ALARM` (the turned bands).

  No `IN POWER FAILURE` swatch: the walls never glow.

### A.6 What the player learns, in order

| Where | What the player meets | What they realise |
|---|---|---|
| LEVEL 0, the start | The placard (four lines, the warden line, `YOU ARE HERE`); the start door behind, `NO REENTRY` | It is an evacuation, and the building talks in rules |
| LEVEL 0, the maze | The locked EXIT (`LOCKED` / `NEEDS STAIR KEY`); a TV heard through the doorways | The way out is locked; the key is somewhere |
| The security desk | An empty post and a set still playing; the key. Taking it kills the set. The plate `SILENT ALARM: REMOVAL SIGNALS PANEL` (read up close) | Taking the key called something |
| The first warning (C32: 1.3–2 s after the take) | The lamps dip, and the paper in your rooms turns toward the EXIT | The paper points when it is near |
| The stair | `NO REENTRY` / `NEAREST REENTRY ON LEVEL B1` | There is no up, only down |
| B1, B2, … | The same building one floor down; the arrows lead to its desk, then its EXIT | Every exit leads further in |
| Caught | `CAUGHT` / `OCCUPANT ACCOUNTED FOR` / `LEVEL B2  /  123 S  /  …` | The building counted you |

- Tiers change only the Relay. The paper is the same at T0 and T5.
- With no human marks, the traces of anyone else are the guard's leftovers at the security desk (the log, initials `D.K.`, `0200 TOUR 3`) and, at most once per stair, a door slamming somewhere far below.
- **Why the alarm never unlocks the stairs:** by code the release comes from the central control station on the street floor (UBC 1991 §1807(f), (j)). Nobody can reach that floor, and nobody is at the controls at night (`40` §1).

### A.7 Writing rules

- The building speaks deadpan 1990 US life-safety English, in capitals. It is never warm and never says "help".
- No words on the walls.
- One name per thing (`40_floors_exit_tutorial_strings.md` §7): STAIR A, SECURITY DESK, the LEVEL token, REENTRY.
- Never RUN or HIDE as advice (`WALK, DO NOT RUN` is the negated period phrase). Never the word "relay". Nothing that implies the dark hides you. No live threat information.
- Dates ≤ 1990. No brands, no mill or company names.
- Type, faces and sizes are 平面视觉's.

### A.8 Strings index (what this line owns, and where it lives)

| Surface | Where |
|---|---|
| Placard: four lines, the warden line, legend, title block | `research/narrative/40_floors_exit_tutorial_strings.md` §3 (art: `research/placard/50_tutorial_placard.md`) |
| EXIT, key, HUD prompts | `40` §4 |
| Stairwell plates | `40` §5 |
| CAUGHT (desktop and touch) | `40` §6 |
| Start-room line `LEVEL 0 / FRONT ROOMS` | `40` §6 |
| Security desk paper and plates | `research/station/01_research.md` §6 |
| TV, station ID, sign-off | `research/screens/30_screen_strings.md` |
| The placard legend, when W6 ships | A.5 above |
| Retired | Revision 3's A.11 ink content and the hint-card line "WHERE THE LIGHT DIES, THE PAPER POINTS" (it says the dark guides you; never built) |

### A.9 Strengths, risks, costs

**Strengths:**
- Every rule has a reason inside the world: the locked stairs, the desk first, the turn only in alarm, the warden, the count.
- The paper never lies, so the player learns one clean meaning per cue.
- It ties the station, the floors, the placard and the hunter's look into one building.
- **It fixes nothing about the Relay** beyond its role.

**Risks:**
- The wall's only voice is direction, so the dread rests on the Relay, the station and the floors.
- The cue appears only when the hunter is near. Players may first read the paper as a warning and only later as a guide. Both readings are true.
- The placard glows in the dark, so some players will expect the walls to glow. The placard's legend must never show a glowing wall.

**Costs:** nothing beyond what is built or planned (the cue slices, the driver, the route field, the placard). Revision 3's ink textures are not needed.

### A.10 The staged warning (EGRESS reading)

系统设计 owns the warning (`RELAY_PURSUIT_REDESIGN.md` §9). The full fire-alarm reading is in `research/relay_pursuit/30_narrative.md` §5. The lamps never dim where the Relay is: every warning happens around **you**.

| Stage | Game | EGRESS reading | The paper |
|---|---|---|---|
| 1 | Your room's lamps dip in short bursts (W1) | Pre-alarm: the panel tests your compartment's emergency power | Your room set's bands click round to WAY ON (the desk, then the EXIT). Never in the Relay's room or within 2 cells |
| 2 | Muffled footsteps (W2) | The warden's sweep is on your floor | No change (2 ↔ 1 changes nothing) |
| 3 | A door-holder thunk and one chime (W3); a 0.7 s hold | The alarm is verified in your compartment | No change during the hold |
| Chase | It hunts you in sight | Full alarm around its occupant | A wave outward from your room set at 8 m/s; ALARM ROUTE; the rows step |
| Chase ends | It loses you | It stands down to pre-alarm | The wave retracts over 2–3 s, to WAY ON if stage ≥ 1 |
| Withdraw, restore | The hum returns (audio only) | The panel resets without telling the occupants | Nothing: the paper is never an all-clear |

Revision 2's ink halo around the Relay and revision 3's GROUND gasp at stage 1 are both withdrawn. None of the stages is a stealth rule (LD R13).

---

## 5. WP03 and the cue (a note for the visual chat)

- In revision 3 the guidance was a separate glowing layer, and §5 separated it from the décor by orientation, medium and angle. **That separation is gone:** in v1 the guidance *is* the décor.
- It is told apart from décor in two ways:
  - **by move:** ambient keyframes never straight-match, turn 90° or 180°, flatten, or change a single row (`32_pattern_native_hints.md` §9.6);
  - **by time:** the paper holds clean K00 while a cue runs.
- EGRESS gives this a reason: the plan can only move in ways the décor never does, so guidance can never be mistaken for a print fault.

---

## 6. Coordination log

*Entries before 2026-10-22 use revision 3's section numbers (now Appendix R3).*

**With the wallpaper-print chat, 2026-10-03.**
- Adopted:
  - the two-layer close-up pipeline (§1.1);
  - field 1.0 / type 0.55;
  - the T2 variant layer and the forged layer (bit 6);
  - the placard as a prop with an InkShape legend;
  - the Flash fallback "WHERE THE LIGHT DIES, THE PAPER POINTS";
  - the wording "a compartment you haven't been in";
  - EAR is local and P2.
- The print chat relayed Red's decisions (header).
- The FLOW-vs-WP03 cues are recorded in LD §9.
- **Next step:** it renders the ink content from A.11.

**With the systems chat (系统设计), 2026-10-03.**
- For the Relay pursuit redesign, the narrative chat wrote `research/relay_pursuit/30_narrative.md`. It covers the fire-alarm reading of the loop, the trigger rooms and the staged warning.
- The systems chat folded it into `RELAY_PURSUIT_REDESIGN.md` §7 and §10.
- The herald, omen and restrike are deleted there. Burst flicker is a smooth sag under 3 Hz, so the ink's stage 1 gasp (A.10) works.

**Asks:**

| Chat | Ask |
|---|---|
| Print | Render A.11; add layer 9 to the README layer table |
| Visual | Unmirrored u for the type fetch; layer 9 shader rule (A.11); a placard prop; a green that reads as print, not Kane cracks; the §5 glyph angle |
| Map | Placard placement (Q10); the Flash line on the hint card (`FrontRooms3DGame.cs:1546`), used only if playtest needs it; the optional Caught line |
| Sound | The ink stays silent. No alarm bell. Lock-on cue references are in `relay_pursuit/30_narrative.md` §5 |

**2026-10-22 (叙事设计, account B; revision 4).**
- Red's answers (12:15, 12:34; header) folded in. Peers told with the quote: 系统设计 (C28, C29 recorded, data 22w), 关卡设计, 交互设计, 平面视觉, 教程设计, 游戏视觉; the wallpaper line through its brief Inbox.
- The glow question was held for 19 minutes: my ask said glow A hid the cue, and the wallpaper line's VL350 (09:10) had shown that it no longer did on the A+M4+V1 geometry. Red re-confirmed B with that corrected.
- For the wallpaper line (`20_level_design_phosphor.md` is theirs): its glow-A notes, the R1/R2 ink gate and the Flash line "WHERE THE LIGHT DIES, THE PAPER POINTS" need a status line saying they are retired.

---

## 7. Open questions for Red

None open. Answered on 2026-10-22 in the 叙事设计 chat:
- the T4+ figure layer: not in v1 (12:15);
- the Caught line `OCCUPANT ACCOUNTED FOR`: yes, under CAUGHT (12:15);
- revision 3's frozen strings (A.11) and the M9 scratch lines: moot, cut with the overlay (12:15);
- the wall glow: B, the walls never glow (12:34).

Still open elsewhere: the cue motion pick (C31, the wallpaper line asks Red in its own chat). It does not change this document: the fiction only needs the bands to turn and step.

## 8. Not done / limits

- **Figma GN01–GN09** (section 2471:3804) still show revision 3's glowing underprint and its concept renders, except GN07–GN09, which hold the pattern cues. A note on the section saying "revision 3, superseded by revision 4" is still to do (this line's Figma work, next time Figma is open).
- **The egress register of the paper is invented.** No such 1990 product existed. The parts it borrows (egress chevrons, NO EXIT, evacuation placards, floor wardens, key control on an alarm loop) are real and from the period.
- **Unverified:** the Haunted Mansion's original portrait mechanism; the 1990-edition NFPA wording; glow-star dates (one source); fire-alarm and hunting terms (listed in `relay_pursuit/30_narrative.md` §7).
- **A WebFetch call auto-cached one state PDF** (about 145 KB) in the session's tool-results folder on 2026-10-03. It was deleted unread, and nothing reached the project.

## 9. Sources

### 9.1 Ledger (fetched 2026-10-03)
- **Gilman:**
  - https://www.gutenberg.org/cache/epub/1952/pg1952-images.html
  - https://www.gutenberg.org/cache/epub/1952/pg1952.txt
  - https://en.wikipedia.org/wiki/The_Yellow_Wallpaper
- **Kane:**
  - https://en.wikipedia.org/wiki/Backrooms_(web_series)
  - https://openlibrary.org/isbn/9780415263573
  - ep 13: https://youtu.be/a7ckzgIgx_o (linked from https://www.youtube.com/watch?v=ywVxpZ4XUBM)
  - ep 24: https://www.youtube.com/watch?v=ZbPaWvqAEq4
- **The Exit 8:**
  - https://en.wikipedia.org/wiki/The_Exit_8
  - https://automaton-media.com/en/interviews/interview-the-exit-8-developer-kotake-create-on-the-perks-of-being-a-solo-dev-and-how-platform-8-came-to-be/
  - https://www.youtube.com/watch?v=pDTFOTTlw7I
- **Haunted Mansion:**
  - https://en.wikipedia.org/wiki/The_Haunted_Mansion
  - https://wdwnt.com/2019/01/video-lightning-strike-changing-portrait-scene-gets-an-upgrade-in-the-haunted-mansion-at-the-magic-kingdom/
  - https://wdwnt.com/?p=1045810
- **Phosphors:**
  - https://en.wikipedia.org/wiki/Phosphor
  - https://en.wikipedia.org/wiki/Zinc_sulfide
  - https://en.wikipedia.org/wiki/Phosphorescence
  - https://www.mphotoluminescent.com/ms-series-sulfide-based-msgg-4d.html
  - https://en.wikipedia.org/wiki/Strontium_aluminate
  - https://www.nemoto.co.jp/?p=437
  - https://en.wikipedia.org/wiki/Super-LumiNova
  - https://en.wikipedia.org/wiki/Luminous_paint
- **Radium:**
  - https://en.wikipedia.org/wiki/Undark
  - https://en.wikipedia.org/wiki/Radium_Girls
  - https://en.wikipedia.org/wiki/Radium_dial
- **Egress:**
  - https://en.wikipedia.org/wiki/Air_Canada_Flight_797
  - https://www.law.cornell.edu/cfr/text/14/25.812
  - https://www.govinfo.gov/content/pkg/FR-1999-06-23/html/99-15928.htm
  - https://rosap.ntl.bts.gov/view/dot/12801
  - https://en.wikipedia.org/wiki/MS_Scandinavian_Star
  - https://trid.trb.org/View/444262
  - https://iadclexicon.org/low-location-lighting-lll/
  - https://en.wikipedia.org/wiki/Tritium_radioluminescence
  - https://orau.org/health-physics-museum/collection/radioluminescent/tritium-exit-sign.html
  - https://idighardware.com/2009/11/not-an-exit/
  - https://www.osha.gov/laws-regs/regulations/standardnumber/1910/1910.37
  - https://meyerfire.com/daily/corridor-with-a-non-exit-door-at-end-a-dead-end
  - https://www.mphotoluminescent.com/ul-924-photoluminescent-exit-sign.html
  - https://iaeimagazine.org/magazine/2003/september2003/photoluminescent-exit-signs/
  - https://www.dinmedia.de/en/standard/din-67510/2003509
  - https://www.inspectpoint.com/resources/articles/emergency-lighting-and-exit-sign-testing-requirements
- **Period objects:**
  - https://en.wikipedia.org/wiki/Hypercolor
  - https://www.mentalfloss.com/culture/fashion-beauty/hypercolor-clothing-fad
  - https://en.wikipedia.org/wiki/Fluorescence
  - https://en.wikipedia.org/wiki/Blacklight_poster
  - https://en.wikipedia.org/wiki/DayGlo
  - https://en.wikipedia.org/wiki/Magic_Eye
  - https://en.wikipedia.org/wiki/Autostereogram
  - https://thehustle.co/originals/youngest-female-inventor
  - https://en.wikipedia.org/wiki/Glo_Friends
  - https://en.wikipedia.org/wiki/Glo_Worm
  - https://en.wikipedia.org/wiki/Laser_tag
  - https://en.wikipedia.org/wiki/Laser_Quest
- **Marking and hunting:**
  - https://en.wikipedia.org/wiki/Hobo
  - https://en.wikipedia.org/wiki/Trail_blazing
  - https://en.wikipedia.org/wiki/Maze-solving_algorithm
  - https://en.wikipedia.org/wiki/Drag_hunting
  - https://en.wikipedia.org/wiki/Fox_hunting
- **Project:**
  - `20_level_design_phosphor.md`
  - `10_synthesis.md`
  - `research/hunter/03_gameplay_tech.md` §6
  - `Tools/print/patterns/hard_edge.py`

### 9.2 Media for the research page (downloaded 2026-10-03 after Red's approval; ledger `Research/week02/phosphor-narrative/SOURCES.md`)

Target: `Research/week02/phosphor-narrative/` (`stills/`, `clips/`, `SOURCES.md`). Raw YouTube downloads stay in the session scratchpad.

| File | Source | License | Size |
|---|---|---|---|
| `gn_gilman_hatfield_1892.png` | Commons, *Joseph Henry Hatfield Yellow Wallpaper.png* (1892 illustration) | Public domain | 119 KB |
| `gn_undark_ad_1921.jpg` | Commons, *Undark (Radium Girls) advertisement, 1921.jpg* | Public domain | 1.2 MB |
| `gn_radium_dial_uv.jpg` | Commons, *Radium Dial UV.jpg* (Arma95) | CC BY-SA 3.0 | 94 KB |
| `gn_tritium_exit_sign.jpg` | Commons, *Tritium-exit-sign.jpg* (Gazebo), 1920 px thumb | CC BY-SA 4.0 | ≈ 0.6 MB |
| `gn_photoluminescent_exit_sign.jpg` | Commons, *Photoluminescent Exit Sign.jpg*. Modern, used for the physics only | CC BY-SA 4.0 | ≈ 0.5 MB |
| `gn_scandinavian_star_1990-04.jpg` | Commons, *MS Scandinavian Star 001.jpg* (Terje Fredh / Sjöhistoriska museet) | CC BY-SA 4.0 | 602 KB |
| `gn_haunted_mansion_portraits.jpg` | Commons, *Hall of portraits, Haunted Mansion, Disneyland.jpg* | CC BY-SA 3.0 | 1.7 MB |
| `gn_hypercolor.jpg` | Commons, *Generra Hypercolor 2.jpg* | Public domain | 693 KB |
| `gn_autostereogram_shark.png` | Commons, *Stereogram Tut Random Dot Shark.png* | CC BY-SA 3.0 | 183 KB |
| `gn_hobo_signs_museum.jpg` | Commons, *Hobo signs and symbols.jpg* (Ryan Somma, National Cryptologic Museum) | CC BY 2.0 | ≈ 0.8 MB |
| `gn_hobo_markings_new_orleans.jpg` | Commons, *HoboMarkingsCanalStFerryCropped.jpg* (Infrogmation) | CC BY 2.5 | 120 KB |
| `gn_painted_blaze.jpg` | Commons, *Painted blaze.JPG* (Daniel Case) | CC BY-SA 3.0 | 200 KB |
| `gn_drag_de_pau_1863.jpg` | Commons, *Album de chasse le drag de Pau, Planche 14* (1863) | Public domain | 350 KB |
| `gn_kane_ep24_stretched_wallpaper` (mp4 + gif + poster) | YouTube ZbPaWvqAEq4, Kane Pixels, *Static Dead End* (242 s) | © (research excerpt) | raw ≈ 13 MB; kept ≈ 4 MB |
| `gn_kane_ep13_isbn_still.jpg` | YouTube a7ckzgIgx_o, Kane Pixels, *9780415263573* (33 s) | © | raw ≈ 2 MB; kept ≈ 0.2 MB |
| `gn_kane_ep16_green_cracks` (mp4 + gif + poster) | YouTube sA5PxGHqpTo, Kane Pixels, *Found Footage #2* (803 s) | © | raw ≈ 22 MB (480p); kept ≈ 4 MB |
| `gn_exit8_rules_sign` (mp4 + gif + poster) | YouTube pDTFOTTlw7I, IGN, *The Exit 8 PlayStation launch trailer* (75 s) | © | raw ≈ 17 MB; kept ≈ 4 MB |
| `gn_haunted_mansion_lightning` (mp4 + gif + poster) | YouTube IYczePEqTHA, cthulhufae, *Lightning Portrait Hall* (44 s) | © | raw ≈ 8 MB; kept ≈ 4 MB |

---

## Appendix R3. Revision 3 (2026-10-03): EGRESS as a glowing underprint (retired 2026-10-22)

*Kept for history. Nothing below ships: the walls never glow (Red, 10-22), the cue is pattern-native, and forged FLOW, the scratch-throughs and the T4 figures are cut (Red, 10-22). Section numbers are revision 3's.*

### R3 · 1. How to read this

- **§4 is the build spec.** A.1–A.9 cover the fiction, trust and forger, the moving print, the LD slots, the marks, the run arc and the rules. A.10 is the staged warning. A.11 is the frozen texture content.
- **§1.1 lists the ten marks.** Each sits on a mechanical slot that the shader's procedural shapes draw (LD §9). The fiction changes only the names and what you see up close.

#### 1.1 The ten marks (mechanical slots)

| # | LD code | Where and when | What you see at range |
|---|---|---|---|
| M1 | FLOW | First-dark beat, T0: the end wall 6–9 m ahead as a lamp dies in view; then every on-route dark cell | Open chevrons, 400 mm tall, 8 per 3 m wall, in the 0.8–2.0 m band, pointing along the wall |
| M2 | HERE | The wall beside a door, or a window, on the route | Two vertical bars, 100 × 900 mm, framing the door (0.8 m beside a window) |
| M3 | STOP | The mouth of a dead-end pocket | Two closed horizontal bars, 600 × 120 mm, on every roll |
| M4 | BREACH | Both faces of a door the Relay broke (permanent) | HERE bars plus a 100 mm diagonal bar |
| M5 | Pressure | Committed as the chase wave front passes (Chase only) | Doubled chevrons to a door, then doubled bars at it |
| M6 | GROUND | Any charged dark cell that is off the route | Faint, even glow with no direction (weight 0.35) |
| M7 | EAR bloom (P2, Q8) | The dark cells round a noise the Relay accepted (source cell plus open neighbours) | The ground glow swells over 1.5 s |
| M8 | FLOW, T2+ variant | Every FLOW from T2 on | Unchanged. Only the close-up content changes |
| M9 | GROUND scratch, T3+ | Rare: about 1 in 8 GROUND cells, one tile per face | As M6, with scratches up close |
| M10 | Forged FLOW (T4+) | A lit, steady cell at an unmarked branch mouth, pointing into the pocket | FLOW chevrons glowing **under a working lamp** (G = 0.8 whatever the lamp does). This is the far tell |

**Where the close-up content lives.** These are the two global arrays in `Tools/print/README.md` § "Planned: glow-ink textures" (spec v1, agreed in principle with the visual chat):

| Global | Coordinates | Holds |
|---|---|---|
| **`_FR_InkType`** | Glyph-local: u = metres along the wall / 0.75; v = (height − 0.8 m) / 0.75 | One layer per message or variant |
| **`_FR_InkSubstance`** | Print UV, one roll tile | One GROUND layer per run tier |

Both are fetched only inside the glow mask. Every layer must meet three rules:
- mean ≥ 0.6 in any 100 mm square inside strokes (field 1.0, knockouts about 0.55);
- detail strokes ≥ 2.5 mm, so the 4× mips keep them;
- legible within about 1.5 m.

**Human marks never use the procedural shapes** (chevrons, bars, brackets), so they cannot be confused with the grammar.

---

### R3 · 2. What EGRESS must honour

| Rule (source) | Consequence |
|---|---|
| Glows only where the cell's own lamp is dead, dying or sagging; a lit wall shows nothing (LD R1) | The product is invisible by day |
| Fresh dark is bright; starved dark (a dead cell among dead neighbours) is blank (LD R2) | It needs light to charge: "no light in, no light out" |
| Only wallpaper carries it: no Office drywall, doors, start area or title stream rooms (LD R3) | Base-building paper only. Office is tenant fit-out; the front rooms are off the plan |
| Calm routes lead to the nearest door or window into an **unvisited zone**. They ignore the Relay and never lead back (LD R5) | It knows the building, not the monster, and leads away from evacuated compartments |
| Pressure routes appear **on CHASE only**, pointing to doors away from the Relay. The wave runs outward from the player's room set at 8 m/s (LD Rev 4; relay v2) | Full alarm around the occupant |
| Truthful before T4. From T4 only FLOW is forged, and a forgery glows under a lit lamp (LD R9; Red: ON) | The other occupant forges it, and only says "go" |
| A message never changes in view (LD R10) | Changes happen unseen, under light, or at a sag or wave front |
| **Dark is never cover.** The Relay's sight is a 12 m ray with no light term (LD R13) | No line, mark or prop may imply that darkness, low light or tall halls hide you |
| **All Relay state text is hidden** (Red; assist toggle only) | The ink, the staged warning and sound are the player's only information. The ink never becomes radar |
| The staged warning flickers the player's own room (systems doc) | The ink gasps beside the player (GROUND only), if the flicker is a smooth sag under 3 Hz (A.10) |
| Era lock: 1990, nothing designed after 1993 (`office_and_film/22_era_lock.md`) | ZnS:Cu, not strontium aluminate (invented 1993, patented 1994). Dates ≤ 1990. Period type |
| IP: concepts only. No wiki text, no Async (`03_ip_canon.md` §5) | Original wording, no real brands, no real names |
| The Relay's look is still open (`research/hunter/10`, `11`) | EGRESS fixes its role (the alarm's responder, `research/relay_pursuit/30_narrative.md`), not its body |

---

### R3 · 4. EGRESS: the building's own exit plan (chosen)

*Author: the place, as a machine.*

#### A.1 Pitch
The underprint is the building's power-failure exit guidance. It is honest about the building and blind to whatever lives in it. Every exit it shows is a way into a compartment you haven't been in yet, and every one leads further in.

**Deck line:** *The wallpaper is a fire-exit plan for a building with no outside.*

#### A.2 Fiction
**Who printed it, and when.**
- A contract-wallcovering mill (unnamed) printed a non-radioactive, light-charged ZnS:Cu underprint under the WP03 hard-edge print, in 1990.
- It was sold as base-building life safety: "invisible by day, there when the power fails".
- Each roll was drawn for one position on the floor plan. The roll stamp reads `LEVEL 0 · SHEET A-3 OF 4` / `ROLL 0417 · PRINTED 03/90 · OCCUPANT LOAD 1` (A.11).
- The underprint is invented. Its parts (pigment, chevron egress marking, NO EXIT, self-tests) are real and from the period.

**Why only in the dark.**
- Afterglow is swamped under a lamp.
- The product is meant to stay hidden until the power fails.
- This is Gilman's sub-pattern, seen "only … in certain lights".

**Why it needs light first (starved cells).** This is the spec sheet's install rule: the paper needs normal light to charge. Players can repeat it as "no light in, no light out", and glow stars already taught it.

**Why it knows the way.**
- When the building rearranges itself unseen, it re-papers, and the plan is redrawn every time the room is.
- Egress leads away from the compartment you occupy. A compartment you have left counts as evacuated, so the plan never sends you back.

**Chase** (the wave fires on CHASE only).
- When the Relay commits to a chase, a silent alarm spreads outward **from your room set**: the building alarms around its occupant (relay v2 / LD Rev 4: the wave never starts at the Relay's cell).
- The lamps drop to emergency level so the paper can be read. **The dark is for the paper, not for you.**
- The plan switches to ALARM ROUTE: doors you can shut, putting a compartment between you and the origin.
- It does not know the Relay breaks doors in 2.5 s.

**Sags** are the emergency-lighting self-test. It is meant to run every 30 days; here it runs every minute or two, because time here is wrong.

**EAR** works like a fire panel's annunciator: the dark cells round a logged noise light up. The paper is reporting, not listening.

**Blind places.**
- Office zones are tenant fit-out: drywall, not on the base-building plan.
- The front rooms (title stream) are not on the plan.

**First dark.** A ballast dies, and the fresh-charged plan blooms.

**Legend (Q10).** A framed evacuation-plan placard on the map side of the start door:
- `YOU ARE HERE`, a start room, and `SEE SHEET A-3`;
- its glow legend lights only when its lamp is off.

**WP03 tie.** The mill drew the egress chevrons wider and blunter than the décor's 55° bands, because guidance must never be mistaken for decoration (see §5).

#### A.3 Trust and the forger
**Trust.**
- Every mark is the building's and true about the building.
- It knows nothing about the occupant who hunts, and calm routes can lead you into it.
- With all Relay state text hidden from the HUD, the player learns to treat the plan as a map, never as a warning. Warnings come from the staged warning (A.10).

**The forger: the other occupant.**
- From T4 the roll stamp reads `OCCUPANT LOAD 2`. Someone else has learned the ink and wants company.
- **Why only FLOW:** they want you to come to them, and the building's "go" is the only word they have taught themselves.
- **Why it shows under light:** real afterglow can never show under the lamp that feeds it. A FLOW you can see in full light is giving off its own light. It is too bright to be paper.
- **The close tell:** the type is hand-drawn imitation, `THIƧ WAY OUT`: uneven, with the S of THIS reversed (A.11, layer 8).

#### A.4 Link to the moving print

| Print event | Fiction |
|---|---|
| Unseen jumps | The building re-papers, and the plan is redrawn underneath: one act, two layers |
| Slips in a lamp's gasp | The décor moves at night (Gilman). The plan holds still where it is speaking (no slips under messages, LD R11) |
| Crawl at scripted beats (GROUND only) | The paper settling during a self-test |
| Chase wave reprint | The alarm redraws the plan outward from your compartment |
| Subliminal drift | The building breathing |

#### A.5 LD §12 slots

| Slot | Answer |
|---|---|
| What it is, who printed it | A 1990 contract-mill phosphor egress underprint, base-building only |
| Why NEW thresholds, never back | Drawn to the current plan; leads away from compartments already evacuated |
| Why it needs light; why dark clusters are blank | The pigment must be charged: "no light in, no light out" |
| Why it reacts to the chase | A silent alarm spreading from your compartment, with routing to doors you can shut (the Relay may open doors and leave them ajar; an ajar door can still be shut) |
| Help or herding | Both. A perfect evacuator with nowhere to send you |
| Content up close | US life-safety sign type (TeX Gyre Heros Bold, as a stand-in for Helvetica) and roll stamps |
| Glyph names | WAY ON / EXIT / NO EXIT / OUT OF SERVICE / ALARM ROUTE |
| Sags | The emergency-lighting self-test |
| First dark | A ballast dies and the plan blooms |
| EAR | The annunciator: answering, not listening |
| Forger | The other occupant: too bright to be paper, hand-drawn type |
| Gilman | The sub-pattern seen only in certain light; "got out at last" while going round |

#### A.6 Ten marks

| # | Name | Up close | What it means here |
|---|---|---|---|
| M1 | WAY ON | `THIS WAY OUT` knocked out of the strokes in repeating lines | The route to a new compartment runs this way |
| M2 | EXIT | `EXIT` running down the bars. Beside a window: `EXIT · BREAK GLASS` | Leave here. The building doesn't know who hears glass |
| M3 | NO EXIT | `NO` set larger above `EXIT`, as the code arranges it | A pocket. Always true |
| M4 | OUT OF SERVICE | `OUT OF SERVICE · DOES NOT CLOSE` | The damage log. Over a run, a map of dead doors |
| M5 | ALARM ROUTE | `ALARM · THIS WAY OUT`; on the door bars, `FIRE DOOR · KEEP CLOSED` | A door you can shut between you and the origin |
| M6 | ROLL STAMP | Register crosshairs + `LEVEL 0 · SHEET A-3 OF 4` / `ROLL 0417 · PRINTED 03/90 · OCCUPANT LOAD 1` | The substrate. It carries the escalation |
| M7 | LOGGED | No new text | It was heard here, so leave quietly |
| M8 | THIS WAY ON | `THIS WAY ON`; the stamp reads `SHEET A-1114 OF 4` | The plan has stopped pretending there is an outside. An Exit 8-style changed sign |
| M9 | SCRATCH-THROUGH | A tally and words scratched through the décor: `IT ONLY GOES IN`, `SAME ROLL AGAIN`, `COUNTED 41 DOORS`, `R.M. 6/90`, `D.K. 11/90` | Other occupants were here. No gameplay meaning |
| M10 | FORGERY | Hand-drawn imitation `THIƧ WAY OUT`; the stamp reads `OCCUPANT LOAD 2` | Not the building's. It leads into a NO EXIT pocket |

#### A.7 Run arc

| Tier | Content | What the player realises |
|---|---|---|
| T0 | M1–M3, M6 | It's a safety system |
| T1 | + M4, the first self-test, the first alarm route; the stamp reads `SHEET A-4 OF 4` | It knows the building, not the monster |
| T2 | M8, `SHEET A-1114 OF 4` | Every exit leads in |
| T3 | M9 | Others followed it before me |
| T4+ | `OCCUPANT LOAD 2`, M10. Optional GROUND figures are not in v1 (§7) | Someone else writes in it |

Optional Caught line: `OCCUPANT ACCOUNTED FOR` (map chat's HUD).

#### A.8 Writing rules
- The building speaks deadpan US life-safety English, in caps. It is never warm and never says "help".
- Type is TeX Gyre Heros Bold, with strokes ≥ 2.5 mm.
- Human marks (M9, and M10's imitation) are words and tallies only.
- Dates ≤ 1990. No mill name.

#### A.9 Strengths, risks, costs
**Strengths:**
- Every LD rule has a reason inside the fiction.
- Grounded in real 1990 objects.
- Uses the Backrooms' own institutional register without Async.
- **Fixes nothing about the Relay.**

**Risks:**
- Quieter dread. The late tiers (M8–M10) carry the scare.

**Costs:**
- 10 type layers, 5 substance layers and a placard prop (A.11).

#### A.10 The staged Relay warning (EGRESS reading)

The systems chat owns the warning (`Documentation/RELAY_PURSUIT_REDESIGN.md`). The full fiction is in `research/relay_pursuit/30_narrative.md` §5. In short, the lamps never dim where the Relay is: the warnings happen around **you**.

| Stage | Game | EGRESS reading | The ink |
|---|---|---|---|
| 1 | Within about 30 m of walking, your room's lamps flicker irregularly as a group | Pre-alarm: the panel moves your compartment to emergency power and tests it before verification | The paper beside you gasps green: **GROUND only, no message** (LD R5). It is the annunciator lighting *your* zone, a local warning and not radar. It shows only if the flicker is a smooth sag under 3 Hz, because stutter cells force the ink to 0 (LD R15) |
| 2 | Within about 15 m, muffled footsteps | The sweep is on your floor | Nothing new |
| 3 | It sees you: a lock-on cue of about 0.6 s, then Chase | Alarm verified, full alarm locked to your compartment | The chase wave (CHASE only) commits ALARM ROUTE (M5) |

Revision 2 had an ink halo around the Relay (the herald brown-out). It is **withdrawn**. None of the three stages is a stealth rule (LD R13).

#### A.11 Frozen ink content v1 (for `_FR_InkType` / `_FR_InkSubstance`)

**Audit result (平面视觉, 2026-10-03), all accepted:**
- **Kept:**
  - Heros Bold;
  - caps;
  - 22 mm cap, 37.5 mm pitch, half-phrase brick;
  - rotated vertical bars reading top to bottom.
- **Changed by 平面视觉** (parameters go to the print chat):
  - no `·` separators: phrases stack in two lines or use an em dash (`EXIT — BREAK GLASS`);
  - one fixed tracking of +1 mm, with the cycle fitted only by the gaps between repeats;
  - FIRE DOOR / KEEP CLOSED, OUT OF SERVICE / DOES NOT CLOSE and ALARM / THIS WAY OUT become two-line blocks. This drops the two earlier fit exceptions;
  - layer 1 uses exactly layer 0's rhythm;
  - NO 32 mm over EXIT 16 mm, the code's 2:1;
  - the roll stamp becomes a US drawing title block: a 2.5 mm rule grid, values in Heros Bold 14 mm (`LEVEL 0 | SHEET A-3 OF 4` / `ROLL 0417 | PRINTED 03/90 | OCCUPANT LOAD 1`);
  - register crosshairs move to the roll selvedge (x 14 / 736 mm);
  - layers 8 and 9 are hand-drawn SVGs by 平面视觉;
  - the ink colour is pale yellow-green, about #B6F5A0–#C9F7B0, faint, with soft bloom and screen-print edges (visual chat).
- **String change accepted by narrative:** `OCCUPANTS n` → `OCCUPANT LOAD n`, the US code-analysis term for a building's rated head count.

The tables and JSON below are v1. Where the audit differs, the audit wins until 平面视觉's parameters are merged here.

**Strings are frozen; typography is provisional.** Red, 2026-10-03: type is decided with 平面视觉, which is auditing every typographic parameter below (face, sizes, spacing, layout, lettering, stamp) against its visual system (keep / change / cut). Its changes land through the print chat's generator. The strings stay narrative's.

Frozen 2026-10-03 so that the wallpaper-print chat can render without guessing. **Rendered the same day:** `Tools/print/ink/out/` (`ink_preview.png`, `ink_report.json`). Every layer is seamless, and strokes keep a mean ≥ 0.84 per 100 mm square. Two fit exceptions were approved: layers 5 and 7, noted in the table. Layer numbers follow `Tools/print/README.md` § "Planned: glow-ink textures". Red may rewrite any string before render; anything else needs a new revision of this section.

**Common to every type layer:**

| Setting | Value |
|---|---|
| Font | TeX Gyre Heros Bold (`Assets/Fonts/Period1990/TeXGyre/`), caps only |
| Pixel scale | 1024 px per 750 mm tile = 1.365 px/mm |
| Values | Field 1.0; type knocked out to 0.55. The stroke outline is the shader's InkShape, not the texture |
| Repeat | Every line or column repeats its phrase in a cycle, fitted to exactly 750 mm so the tile is seamless. Choose the repeat count that needs the least tracking change, keeping tracking within −1 to +4 mm per character. Never break a word at the tile edge |
| Separator | `·` (U+00B7) with one space each side |
| Reading direction | u must not be mirrored for type. Text reads left to right on every face, whichever way a FLOW points (shader note: use an unmirrored face u for the type fetch, separate from FaceSign) |
| Horizontal layers | Line pitch 37.5 mm (20 lines per tile). Each line is offset by half a phrase from the line above (brick), so strokes clipped by InkShape show varied fragments |
| Rotated layers (vertical bars) | Text runs down the bar, rotated 90° clockwise, so it reads top to bottom. Column pitch 37.5 mm. Columns are brick-offset by half a phrase. This survives any bar position. If the shader later supplies bar-local u, horizontal stacked words centred in the bar may replace it |

**`_FR_InkType` layers:**

| Layer | Mark | Orientation | Cap height | Phrase (one cycle) |
|---|---|---|---|---|
| 0 FLOW | M1 | horizontal | 22 mm | `THIS WAY OUT` |
| 1 FLOW T2+ | M8 | horizontal | 22 mm | `THIS WAY ON` |
| 2 HERE door | M2 | rotated | 22 mm | `EXIT` |
| 3 HERE window | M2 | rotated | 22 mm | `EXIT · BREAK GLASS` |
| 4 STOP | M3 | horizontal, pairs | `NO` 30 mm over `EXIT` 18 mm | See the STOP note below |
| 5 BREACH | M4 | rotated | 22 mm | `OUT OF SERVICE · DOES NOT CLOSE` (one cycle: tracking +4 mm, and the rest in word spaces, +3.37 mm each; approved after render) |
| 6 Pressure chevron | M5 | horizontal | 22 mm | `ALARM · THIS WAY OUT` |
| 7 Pressure door | M5 | rotated | **20 mm** | `FIRE DOOR · KEEP CLOSED` (two cycles, tracking −0.96 mm; 20 mm cap approved after render so the US phrase stays whole) |
| 8 Forged FLOW | M10 | horizontal, hand-lettered | ≈ 22 mm | `THIƧ WAY OUT` (see below) |
| 9 GROUND scratch *(new; uses a reserved slot)* | M9 | wall-anchored, one cluster | 25–35 mm | See the scratch note below |
| 10–15 | reserved | | | |

- **STOP (layer 4).** Cells of 125 × 62.5 mm (6 × 12 per tile). In each cell, `NO` (30 mm cap) is centred above `EXIT` (18 mm cap) with a 6 mm gap, so a pair is 54 mm tall. This follows the code's arrangement (NO larger, above EXIT). Alternate rows are offset by half a cell. A 120 mm bar always contains at least one whole pair (62.5 + 54 ≤ 120).
- **Forged (layer 8).** The same phrase and pitch as layer 0, but hand-lettered, not set in Heros:
  - a monoline marker stroke of 2.5–4 mm;
  - each glyph rotated ±5°, baseline jitter ±2.5 mm, glyph size ±8 %, uneven spacing ±3 mm;
  - line pitch drifting ±4 mm, and random per-line offsets instead of the brick offset;
  - the S of `THIS` mirrored (Ƨ) in every phrase;
  - fixed seed 1990, so it is deterministic. It must still tile seamlessly.
- **Scratch (layer 9).**
  - **Values:** background 0, scratches 1.0, composited as ink = max(substance, scratch).
  - **Look:** hand-scratched: monoline 2.5–3 mm, ragged ends, each item rotated ±6°, different hands.
  - **Layout:** one cluster in the lower part of the tile (v 0.1–0.9), never crossing the tile edge. It contains:
    - a tally of 4 gates (four uprights crossed by a diagonal) plus 3 strokes, 40 mm tall;
    - `IT ONLY GOES IN`;
    - `SAME ROLL AGAIN`;
    - `COUNTED 41 DOORS`;
    - `R.M. 6/90` and `D.K. 11/90`, at 25 mm cap.
  - **Shader rule (ask, visual chat):**
    - show layer 9 only from T3, only on GROUND cells where `Hash(seed, cell & 63, 977) < 0.125`;
    - only on one 0.75 m tile per face (picked by hash), and only for v ∈ [0, 1), i.e. 0.8–1.55 m.
  - The same cluster repeating on different walls is intended ("SAME ROLL AGAIN").

**`_FR_InkSubstance` layers** (print UV, one 750 × 1125 mm roll tile; README tiers 1–5 = LD T0–T4+):

| Setting | Value |
|---|---|
| Field | 0.7. This leaves headroom for scratches at 1.0 |
| Marks | Knocked out to 0.4 |
| Register crosshairs | Two, at (60, 60) mm and (690, 1065) mm from the tile's top left. Each is a Ø 16 mm circle plus a 30 mm cross, both with a 2.5 mm stroke |
| Stamp block | Heros Bold, 20 mm cap, tracking +1 mm, left-aligned at x = 90 mm, baselines at y = 560 and 590 mm |

| Layer | LD tier | Stamp line 1 | Stamp line 2 |
|---|---|---|---|
| 0 | T0 | `LEVEL 0 · SHEET A-3 OF 4` | `ROLL 0417 · PRINTED 03/90 · OCCUPANT LOAD 1` |
| 1 | T1 | `LEVEL 0 · SHEET A-4 OF 4` | `ROLL 0417 · PRINTED 03/90 · OCCUPANT LOAD 1` |
| 2 | T2 | `LEVEL 0 · SHEET A-1114 OF 4` | `ROLL 0417 · PRINTED 03/90 · OCCUPANT LOAD 1` |
| 3 | T3 | `LEVEL 0 · SHEET A-1114 OF 4` | `ROLL 0417 · PRINTED 03/90 · OCCUPANT LOAD 1` |
| 4 | T4+ | `LEVEL 0 · SHEET A-1114 OF 4` | `ROLL 0417 · PRINTED 03/90 · OCCUPANT LOAD 2` |

Notes:
- "SHEET A-1114 OF 4" is the T2 anomaly, Exit 8-style: a sheet number larger than the set.
- Every roll carries the same roll number, which is deliberate.
- T3 changes nothing in the stamp, because the scratches come from layer 9.
- The optional T4+ creeping-figure layer is **not in v1** (§7, Q1).

```json
{
  "spec": "glow-ink content v1 (EGRESS), 30_narrative_phosphor.md A.11, frozen 2026-10-03",
  "type_common": {"font": "TeXGyre/texgyreheros-bold.otf", "px_per_mm": 1.3653, "field": 1.0, "knockout": 0.55,
    "separator": " · ", "pitch_mm": 37.5, "brick_offset": "half phrase", "fit": "exact 750 mm cycle, tracking -1..+4 mm/char",
    "u_mirrored": false, "rotated": "90deg clockwise, reads top to bottom"},
  "ink_type": [
    {"layer": 0, "mark": "M1 FLOW", "orient": "horizontal", "cap_mm": 22, "phrase": "THIS WAY OUT"},
    {"layer": 1, "mark": "M8 FLOW T2+", "orient": "horizontal", "cap_mm": 22, "phrase": "THIS WAY ON"},
    {"layer": 2, "mark": "M2 HERE door", "orient": "rotated", "cap_mm": 22, "phrase": "EXIT"},
    {"layer": 3, "mark": "M2 HERE window", "orient": "rotated", "cap_mm": 22, "phrase": "EXIT · BREAK GLASS"},
    {"layer": 4, "mark": "M3 STOP", "orient": "pairs", "cell_mm": [125, 62.5], "top": {"text": "NO", "cap_mm": 30},
      "bottom": {"text": "EXIT", "cap_mm": 18}, "gap_mm": 6, "row_offset": "half cell"},
    {"layer": 5, "mark": "M4 BREACH", "orient": "rotated", "cap_mm": 22, "phrase": "OUT OF SERVICE · DOES NOT CLOSE", "tracking_mm": 4.0, "extra_word_space_mm": 3.37},
    {"layer": 6, "mark": "M5 pressure chevron", "orient": "horizontal", "cap_mm": 22, "phrase": "ALARM · THIS WAY OUT"},
    {"layer": 7, "mark": "M5 pressure door", "orient": "rotated", "cap_mm": 20, "phrase": "FIRE DOOR · KEEP CLOSED", "tracking_mm": -0.96},
    {"layer": 8, "mark": "M10 forged FLOW", "orient": "horizontal", "cap_mm": 22, "phrase": "THIƧ WAY OUT",
      "hand": {"stroke_mm": [2.5, 4], "rot_deg": 5, "baseline_mm": 2.5, "scale": 0.08, "spacing_mm": 3, "pitch_drift_mm": 4, "seed": 1990}},
    {"layer": 9, "mark": "M9 GROUND scratch", "orient": "cluster", "value_bg": 0.0, "value_mark": 1.0, "composite": "max",
      "items": ["TALLY 4x5+3", "IT ONLY GOES IN", "SAME ROLL AGAIN", "COUNTED 41 DOORS", "R.M. 6/90", "D.K. 11/90"],
      "cap_mm": [25, 35], "stroke_mm": [2.5, 3], "rot_deg": 6, "v_range": [0.1, 0.9],
      "show": "tier>=T3 && GROUND && Hash(seed, cell&63, 977) < 0.125 && one tile per face && v in [0,1)"}
  ],
  "ink_substance": {"tile_mm": [750, 1125], "field": 0.7, "marks": 0.4,
    "register": [{"xy_mm": [60, 60]}, {"xy_mm": [690, 1065]}], "register_shape": "circle d16 + cross 30, stroke 2.5",
    "stamp": {"cap_mm": 20, "tracking_mm": 1, "x_mm": 90, "baselines_mm": [560, 590]},
    "tiers": [
      {"layer": 0, "ld": "T0", "lines": ["LEVEL 0 · SHEET A-3 OF 4", "ROLL 0417 · PRINTED 03/90 · OCCUPANT LOAD 1"]},
      {"layer": 1, "ld": "T1", "lines": ["LEVEL 0 · SHEET A-4 OF 4", "ROLL 0417 · PRINTED 03/90 · OCCUPANT LOAD 1"]},
      {"layer": 2, "ld": "T2", "lines": ["LEVEL 0 · SHEET A-1114 OF 4", "ROLL 0417 · PRINTED 03/90 · OCCUPANT LOAD 1"]},
      {"layer": 3, "ld": "T3", "lines": ["LEVEL 0 · SHEET A-1114 OF 4", "ROLL 0417 · PRINTED 03/90 · OCCUPANT LOAD 1"]},
      {"layer": 4, "ld": "T4+", "lines": ["LEVEL 0 · SHEET A-1114 OF 4", "ROLL 0417 · PRINTED 03/90 · OCCUPANT LOAD 2"]}
    ]}
}
```


#### A.12 The placard: strings v1 (Q10 approved)

A framed evacuation plan on the map side of the start door. Ownership:

| Part | Owner |
|---|---|
| Strings (below) | Narrative |
| Typography and layout | 平面视觉 |
| The prop | Visual chat |
| Placement | 关卡设计 |

**Behaviour.**
- **Under light:** the printed plan reads.
- **In the dark:** only the legend glows. It uses the same InkShape silhouettes and is gated by the placard cell's lamp, like the walls.

| Part | String |
|---|---|
| Title | `EVACUATION PLAN` |
| Title block (replaces the sub-line; 平面视觉) | `LEVEL 0` \| `SHEET A-2 OF 4` \| `PRINTED 03/90`, the same title block as the wall stamp |
| On the plan | `YOU ARE HERE` (start room); `EXIT` (the door); beyond the door a blank area marked `SEE SHEET A-3` |
| Legend head (glows) | `IN POWER FAILURE` |
| Legend rows (glow) | chevrons `WAY ON` · two bars `EXIT` · closed bars `NO EXIT` |
| Footer (printed) | `IN CASE OF FIRE: WALK, DO NOT RUN.` / `CLOSE DOORS BEHIND YOU.` (relay v2 §10: it teaches walking and doors. Wording 待核) |
| Secondary line (optional, layout permitting) | `IN CASE OF FIRE, USE STAIRS UNLESS OTHERWISE INSTRUCTED` (NYC code wording in force by 1990; there are no stairs either). Footer wording verified, `relay_pursuit/30_narrative.md` §7 |

- The sheet is A-2, so the walls' first stamp, A-3, is "the next sheet".
- No glyph on the placard may differ from the wall grammar.

---

### R3 · 5. WP03 and the glyphs (a note for the visual chat)

WP03's décor already contains chevron bands at 55° and small arrows in the motif band. In failing cells both layers breathe together. Three cues separate them, recorded by the print chat in LD §9:
1. **Orientation.** WP03's chevrons point *up* the wall; FLOW chevrons point *along* it, toward the route.
2. **Medium.** The décor is lit albedo and vanishes in the dark; FLOW is dark-only emission.
3. **Angle (this document's ask).** FLOW arms about 30–35° from horizontal, isolated in the 0.8–2.0 m band. The décor's are continuous mitred bands. The final angle is the visual chat's call.

EGRESS gives the difference a reason: life-safety guidance must never be mistaken for decoration (A.2).

---


## Appendix B. Alternative: SUB-PATTERN, those behind the paper

*Author: the people the place has kept.*

### B.1 Pitch
Everyone the place has kept is behind the front pattern. In the dark, they show you where they were going when it caught them.

**Deck line:** *The pattern has people behind it, and they only move in the dark.*

### B.2 Fiction
**Who printed it, and when.**
- Nobody. The WP03 paper was hung in 1990 like any contract paper.
- Behind its front pattern there is a sub-pattern nobody printed: the people the place has kept. Like Gilman's women, they are "all the time trying to climb through", and "nobody could climb through that pattern—it strangles so".
- They are not in the walls. They are in the *print*.

**Why only in the dark.**
- "By daylight she is subdued, quiet." They move only when the lamp dies.
- Their green is light the paper took in. The lamps feed them, and in the dark they give it back.

**Why it needs light first.**
- They can only show you the light they were given.
- A dark cluster is where they have been hungry too long, and there is nothing left to show.

**Why it knows the way.**
- They are in every sheet of paper, joined wall to wall, and they have walked every room.
- They point to rooms you haven't been in because they were all heading somewhere new when it caught them. Nobody who was running went back.

**Why the marks are shapes.**
- They can only move along the geometry of the print they are trapped in.
- Their trails bend into chevrons. Where one was cornered, the pattern "becomes bars!" They hold a door frame as two uprights.
- The grammar is the shape of the trap.

**Chase.**
- In calm play they cannot feel the Relay, because it walks softly.
- When it runs, they panic and flee ahead of it. The wave is them.
- They point at doors because a shut door is what each of them wished they had reached. This says nothing about the dark: they were caught in the light too.

**Sags.** Many of them press forward at once to look at you, and the lamps sag. The front pattern bulges.

**EAR.** They turn to look where it heard you. They are watching, not listening.

**Blind places.**
- Office drywall has no pattern to be behind.
- The front rooms' paper is new: nobody is behind it yet.

**First dark.** A lamp dies, and they wake bright and fresh.

**Legend (Q10).** A torn corner of paper beside the start door, as Gilman tears the paper. In the dark the flap shows one chevron and one pair of bars.

**WP03 tie.** The décor's mitred bands are the bars of their cage. Their own chevrons are blunter, because hands round off corners.

### B.3 Trust and the forger
**Trust.**
- They want you out because they never got out.
- They don't know the Relay is coming until it runs.
- They are honest and frightened, which is not the same as safe.

**The forger: the one who wants company.**
- Not all of them want you out. Some want you in with them, as Gilman's narrator ends up among the creeping women.
- **Why only FLOW:** they only beckon, and never warn.
- **Why it shows under light:** she has come up to the surface, close enough to the front of the paper to show through even under a lamp. She is no longer subdued by daylight.
- **The close tell:** the fingertip trails inside the stroke face *back at you*, curling inward. They beckon instead of reaching away.

### B.4 Link to the moving print

| Print event | Fiction |
|---|---|
| Unseen jumps | They move. "The woman behind shakes it" |
| Slips in a lamp's gasp | Gilman's night movement. They stay still where they are pointing (LD R11) |
| Crawl at scripted beats | Creeping (GROUND only) |
| Chase wave reprint | Them fleeing ahead of it |
| Relay wake reprint (removed in relay v2) | — |
| Subliminal drift | Breathing |

### B.5 LD §12 slots

| Slot | Answer |
|---|---|
| What it is, who printed it | Nobody. The kept, behind the front pattern |
| Why NEW thresholds, never back | They were all heading somewhere new when it caught them |
| Why it needs light; why dark clusters are blank | They only have what the lamps fed them |
| Why it reacts to the chase | They flee when it runs; they show the doors they never reached |
| Help or herding | Help from the frightened, and a lure from the lonely |
| Content up close | Fingertip trails, handprints, knuckle and forearm marks |
| Glyph names | THE WAY THEY RAN / THE DOOR THEY HELD / THE BARS / THE SCRATCH / BOTH HANDS |
| Sags | They crowd forward to look |
| First dark | They wake |
| EAR | They watch the spot it heard |
| Forger | The one at the surface, who wants company |
| Gilman | Literal: sub-pattern, bars, creeping, climbing through |

### B.6 Ten marks

| # | Name | Up close | What it means here |
|---|---|---|---|
| M1 | THE WAY THEY RAN | Four parallel fingertip streaks along each stroke, thinning in the direction of travel | Someone ran this way, toward new ground |
| M2 | THE DOOR THEY HELD | Handprints stacked up both bars, palms toward the opening. At a window, prints spread flat on the glass side | Through here |
| M3 | THE BARS | Knuckle and forearm prints across the bars, as if gripping them | Someone was cornered here. Always true |
| M4 | THE SCRATCH | Four drag lines across the diagonal | It came through. The door will never shut |
| M5 | BOTH HANDS | Two sets of streaks side by side, smeared from speed | Run, and shut that door |
| M6 | THE PRESSED | Faint overlapping palm prints, as if from the other side of the paper | They are here |
| M7 | WATCHING | The palms turn toward the noise source | It heard you here |
| M8 | CLOSER | The streaks resolve into whole hands | They are nearer the surface |
| M9 | THE EYES | Gilman's "two bulbous eyes" in the GROUND repeat, upside down, one pair per roll | They see you. No gameplay meaning |
| M10 | THE BECKONING | Fingertips curl back toward you, toward the pocket | She wants company |

### B.7 Run arc

| Tier | Content | What the player realises |
|---|---|---|
| T0 | M1–M3, M6 | Someone left trails |
| T1 | + M4, the first sag, the first chase | They know the rooms, and they are afraid of it too |
| T2 | M8 | They are coming closer to the surface |
| T3 | M9 | They are watching me |
| T4+ | M10; GROUND procession of small creeping figures tiled per roll, seen in a sag from ≥ 6 m (Gilman's "great many women"; the Brewster/Magic Eye hidden figure) | Not all of them want me out |

Optional Caught line: none, or the GROUND procession gains one figure on the next run's title (the visual chat decides).

### B.8 Writing rules
- **They are voiceless:** no words anywhere, only bodies and trails. This is the opposite of A.
- **No faces before T3**, and even then only Gilman's eyes. No gore, no "help me", never seen whole except at a distance.
- **They never point at the Relay.** Their light is the lamps' light, so nothing implies the dark protects you.
- **No names.** They are "they", and "she" for the forger, after Gilman.

### B.9 Strengths, risks, costs
**Strengths:**
- The strongest dread.
- Gilman used literally.
- The forger and the Caught implication are emotionally clear.
- No text to localise.

**Risks:**
- Explaining crisp chevrons as hand trails is a stretch. The "shape of the trap" covers it.
- The "friendly ghosts" reading tilts the ink toward help.
- No 1990 object grounds it.
- It says what being caught means (you join them).

**Costs:**
- Hand-texture layers, which are harder to keep at ≥ 0.6 coverage.
- One figure layer.
- A torn-flap decal.

---

## Appendix C. Alternative: BLAZES, the wanderers' code

*Author: earlier people, alive, curated by the building.*

### C.1 Pitch
People fell in before you. They found out the paper glows when scratched, so they marked it for whoever came next. The building copies forward only the marks that are still true.

**Deck line:** *Someone marked the way for you. The building only keeps the marks that are still true.*

### C.2 Fiction
**Who made it, and when.**
- The WP03 paper has a phosphor ground coat under its vinyl print. Nobody in here knew why. It could be an off-spec mill run, but the game never explains it, and the wanderers didn't know either.
- Wanderers in the late 1980s and 1990 learned that scratching through the print exposes the ground, which glows in the dark.
- They built a four-sign code, like hobo signs chalked for the next traveller and painted trail blazes, and cut it with a stencil so that every hand makes the same shape.

**Why only in the dark.** It is glow-in-the-dark ground, and the print hides it in light.

**Why it needs light first.**
- It is ordinary glow material. The wanderers' rule: "lamps on, then read".
- A dark cluster is unreadable, and they knew it.

**Why it knows the way.**
- **Two authors.** The hands are human; the choice is the building's.
- Over years, people scratched thousands of marks in every direction. Each time the building rearranges itself and re-papers, it copies the marks that are still true of the new layout (the place copies everything, like Kane's repeating signs) and paints over the rest.
- Why does it show you only the ones that lead somewhere new? The code's first rule was "never mark the way back". The building also wants you moving. That second reason is the ambiguity.

**Chase.**
- Wanderers scratched RUN marks at the doors they escaped through.
- The building shows those only when the hunt runs. It is the building's choice again, and darkness is no part of it.

**Sags.** The place's power is unreliable. In those few seconds, everyone used to stop and read the walls.

**EAR.** The building surfaces old `QUIET` scratches where you were loud.

**Blind places.**
- Office drywall has no glow ground.
- The front rooms are freshly papered, and nobody has scratched them yet.

**First dark.** A lamp dies, and the scratches bloom.

**Legend (Q10).** "THE CODE": the four signs scratched beside the start door with a word under each. This is the hobo-sign tradition of explaining the signs.

**WP03 tie.** The stencils were cut from the décor's own bands, so the signs are made of the paper's geometry.

### C.3 Trust and the forger
**Trust.**
- The marks were made in good faith by people who knew things the building doesn't. M2's window variant says LOUD.
- Which marks you see is the building's choice.

**The forger: the keeper.**
- A wanderer who stopped wanting out. He settled in a pocket and wants visitors.
- **Why only FLOW:** his home is a dead end, so he never marks dead ends.
- **Why it shows under light:** he paints with self-luminous paint scraped from old radium-dial clocks, second-hand stock. It glows without charging and does not need the dark, so his marks shine in full light.
- **The close tell:** a small roof sign above his chevrons, his own addition to the code, meaning "stay".

### C.4 Link to the moving print

| Print event | Fiction |
|---|---|
| Unseen jumps | Re-papering: true marks copied forward, stale ones buried |
| Slips in a lamp's gasp | The paper settling. Copied marks never slip (LD R11) |
| Crawl | Re-papering in progress |
| Chase wave reprint | The building rushing the RUN marks forward |
| Relay wake reprint (removed in relay v2) | — |
| Subliminal drift | — |

### C.5 LD §12 slots

| Slot | Answer |
|---|---|
| What it is, who made it | Wanderers' scratches into a glow ground, copied forward by the building |
| Why NEW thresholds, never back | The code's first rule, plus the building's choice |
| Why it needs light; why dark clusters are blank | Ordinary glow ground: "lamps on, then read" |
| Why it reacts to the chase | The RUN marks, shown only when it runs |
| Help or herding | The marks help. The selection may herd |
| Content up close | Scratch texture, stencil edges, words and tallies |
| Glyph names | GO ON / DOOR / DEAD END / IT BROKE THIS / RUN |
| Sags | Unreliable power: everyone stops to read |
| First dark | The scratches bloom |
| EAR | Old QUIET marks surfaced |
| Forger | The keeper: radium paint, roof sign |
| Gilman | The "smooch": a rubbed trail along the wall (M6) |

### C.6 Ten marks

| # | Name | Up close | What it means here |
|---|---|---|---|
| M1 | GO ON | Stencil edges with scratch fill; `GO ON` scratched small | The way on |
| M2 | DOOR | Scratched `DOOR`. At a window: `DOOR · LOUD` | Through here. Glass is heard |
| M3 | DEAD END | Scratched `DEAD END` | A pocket. Always true |
| M4 | IT BROKE THIS | `IT BROKE THIS` and a tally of blows | The door will never shut |
| M5 | RUN | Two stencil passes; `RUN · SHUT IT` | Shut that door |
| M6 | THE SMOOCH | A long rubbed line at shoulder height where people steadied a hand on the wall (the wall-follower habit), with initials | People walked here |
| M7 | QUIET | An old `QUIET` scratch | You were loud here |
| M8 | NO NEWER DATE | Tallies get longer. Every date reads 1990 or earlier, however long the tally | Time doesn't pass in here |
| M9 | MESSAGES | Notes to each other: `WAIT AT THE TALL ROOM`, `11/90 STILL GOING` | People hoped. No gameplay meaning |
| M10 | THE KEEPER'S LINE | Fresh, even paint, no scratches, a little roof sign above | Someone wants visitors |

### C.7 Run arc

| Tier | Content | What the player realises |
|---|---|---|
| T0 | M1–M3, M6 | Someone marked this for me |
| T1 | + M4, the first chase (M5) | They knew about the monster |
| T2 | M8 | No date is newer than 1990 |
| T3 | M9 | They were people, and they're gone |
| T4+ | M10; GROUND reads `STAY` in the keeper's paint | Not every mark is left for my sake |

Optional Caught line: none.

### C.8 Writing rules
- **Human, terse, scratched caps.** No "help me", no names (initials only), no wiki explorer-group names or Backrooms wiki items.
- **Only the four code shapes carry meaning.** Everything else is commentary.
- **Dates ≤ 1990.**
- **The window variant may warn (LOUD).** It is the only human knowledge the ink adds. It is never a Relay position.

### C.9 Strengths, risks, costs
**Strengths:**
- Human warmth.
- The easiest to grasp.
- Real marking traditions behind it.
- The best legend object (THE CODE).

**Risks:**
- Two authors. The building's curation is the weakest reason among the four.
- Hand marks in crisp shapes need the stencil explanation.
- Close to the wiki's explorer-group culture and the found-note cliché.

**Costs:**
- Scratch and stencil type layers.
- A scratched-legend decal.

---

## Appendix D. Alternative: DRAG LINE, the hunt's laid course

*Author: the hunt; the Relay is its hound.*

### D.1 Pitch
The ink is a drag line: the course of a hunt, laid out ahead of the hounds. You are not escaping. You are running the course.

**Deck line:** *Somebody laid the course. You're the one running it.*

### D.2 Fiction
**Who laid it, and when.**
- The hunt. It is not the Relay's body: the Relay is the hound, the "relay" placed along the line (`research/hunter/03` §6).
- Whoever keeps the hunt is never seen. Call them the huntsman.
- The line is laid in ordinary phosphorescent luminous paint, always just ahead of you, while you are not looking.

**Why only in the dark.**
- A night course has to read when the lamps are out.
- The first-dark beat is the huntsman putting a lamp out: "moving off".

**Why it needs light first.** It is ordinary glow paint, which works only after the lamps have charged it.

**Why it knows the way.**
- A drag line never doubles back. A good run goes over fresh ground, and the hunt wants a long run.
- So it always points into compartments you haven't been in.

**Chase.**
- When the hound views you, the hunt redraws the line to the jumps, the doors you can put behind you, because a run that ends in seconds is a waste.
- The line doesn't hide you. Nothing does.

**Sags** are the hunt showing a stretch of the course.

**EAR.** Where it heard you, the line freshens: the scent is strong there.

**Blind places.**
- Office zones are outside the hunt's country. The hound still hunts there, but there is no line.
- The front rooms are the meet, before the hunt moves off.

**Legend (Q10).** A meet card pinned by the start door: `THE MEET · 03/90`, with ▶▶ LINE · ▮ ▮ JUMP · ═ EARTH. The card is printed and readable in light; its signs glow in the dark.

**WP03 tie.** The line is laid along the paper's bands, as a drag is laid along hedgerows.

### D.3 Trust and the forger
**Trust.**
- The line is always true about the course and never about your safety.
- Calm lines can run you straight into the hound. The hunt wants the hound to find you on the line, eventually.
- STOP marks the **earths**, the dens where the quarry goes to ground. The hunt marks them honestly because a run that goes to ground ends early.

**The forger: the hound itself.**
- The hunt plays fair. The hound doesn't: impatient for the kill, it lays a short false line into an earth.
- **Why only FLOW:** a relay repeats a signal. It can only copy the line, never the other signs.
- **Why it shows under light:** it is not paint. It is the hound's own light, whatever the Relay turns out to be.
- **The close tell:** a dragged mark is heavy where it starts and thins out. On a forged line the heavy end is at the pocket, because it was laid from the earth outward.

### D.4 Link to the moving print

| Print event | Fiction |
|---|---|
| Unseen jumps | The huntsman re-laying the country |
| Slips in a lamp's gasp | The huntsman's hand at work. The line never slips (LD R11) |
| Crawl | The line being laid |
| Chase wave reprint | The view: the field gallops out from the hound's cell |
| Relay wake reprint (removed in relay v2) | — |
| Subliminal drift | — |

### D.5 LD §12 slots

| Slot | Answer |
|---|---|
| What it is, who laid it | The hunt's drag line in luminous paint |
| Why NEW thresholds, never back | A line goes over fresh ground; the hunt wants a long run |
| Why it needs light; why dark clusters are blank | Ordinary glow paint |
| Why it reacts to the chase | The view: the line is redrawn to the jumps for a good run |
| Help or herding | Herding, openly. Running the course is still how you last |
| Content up close | Smear texture with a heavy start; course-card type on GROUND |
| Glyph names | THE LINE / THE JUMP / THE EARTH / THE BROKEN GATE / THE VIEW |
| Sags | The hunt showing the course |
| First dark | The huntsman puts a lamp out |
| EAR | The scent freshens |
| Forger | The hound: its own light, reversed smear |
| Gilman | The pattern moves because someone behind it lays the line |

### D.6 Ten marks

| # | Name | Up close | What it means here |
|---|---|---|---|
| M1 | THE LINE | A dragged smear inside each stroke, heavy at the start and thinning ahead | The course runs this way |
| M2 | THE JUMP | Smear brushed up the bars. At a window: a wider smear (the big jump) | Over here |
| M3 | THE EARTH | Two flat smears across | A den: you'd be dug out. Always true |
| M4 | THE BROKEN GATE | Smear crossed by a diagonal | The gate stays open now |
| M5 | THE VIEW | Two parallel smears, laid fast | It has seen you. The jump is ahead |
| M6 | COURSE CARD | Faint printed type: `MEET 03/90 · RUN 213 · 6 MIN` | Other runs happened |
| M7 | SCENT | Smear freshening round the source | It heard you here |
| M8 | THE TALLY | The card lists more runs. Most end `TO GROUND` | Runs end |
| M9 | THE FIELD | Hoof-and-boot scuffs along the GROUND band (the field's passage) | Others rode this. No gameplay meaning |
| M10 | THE HOUND'S LINE | The smear is heavy at the pocket end | A false line into an earth |

### D.7 Run arc

| Tier | Content | What the player realises |
|---|---|---|
| T0 | M1–M3, M6 | It's a course |
| T1 | + M4, the first chase (M5) | The hunt wants a long run |
| T2 | M8 | Runs end |
| T3 | M9 | I'm not the first quarry |
| T4+ | M10 | The hound cheats |

Optional Caught line: `RUN 214 · 6 MIN`, as a course-card entry using the existing stats.

### D.8 Writing rules
- **Hunting vocabulary appears only in the content up close**, never in the HUD.
- **No blood-sport gore.** The Relay is never drawn as a dog. "Hound" is its role, not its look.
- **The huntsman is never seen or named.** The line never hides you.

### D.9 Strengths, risks, costs
**Strengths:**
- The most paranoid reading.
- The best fit for the escalation coupling (following = being run).
- Uses the hunter research's own etymology.

**Risks:**
- It contradicts LD's thesis ("knows the building, not the monster") and the help half of help/lure.
- It fixes the Relay's *role* (hound) and adds an unseen huntsman.
- Hunting is a far frame from a 1990 office.
- It teaches "never follow".

**Costs:**
- Smear layers.
- A course-card prop.

---

## Appendix E. Side by side (how EGRESS was chosen)

### E.1 Key answers

| | A EGRESS | B SUB-PATTERN | C BLAZES | D DRAG LINE |
|---|---|---|---|---|
| Author | The building (machine) | The kept | Wanderers + the building | The hunt |
| Deck line | Fire-exit plan, no outside | People behind the pattern | Marks that are still true | You're running the course |
| Why only in the dark | Product spec / physics | Subdued by daylight | Glow ground under the print | A night course |
| Starved = blank because | Needs charging | They're hungry | Needs charging | Needs charging |
| Forger | The other occupant | The one at the surface | The keeper (radium paint) | The hound |
| Far tell / near tell | Glows in light / hand-drawn type | Glows in light / beckoning prints | Glows in light / roof sign | Glows in light / reversed smear |
| Close-up material | Sign type | Hands | Scratches + words | Smear + course card |
| Text on walls | Yes (caps) | None | Yes (scratched) | Little (card) |
| 1990 object behind it | Exit signs, egress marking | None | Hobo signs, glow paint | Luminous paint (hunting is a far frame) |
| Fixes the Relay? | No | What being caught means | No | Its role (hound) |
| Gilman | Going round | Literal | The smooch | Someone behind lays it |
| Biggest risk | Quieter dread | Ghosts read as friendly | Two authors | Breaks "help" |

### E.2 Scores (1–5)

| | Mechanics fit | 1990 grounding | Dread / ambiguity | Self-teaching | Leaves the Relay open | Escalation runway | **Total** |
|---|---|---|---|---|---|---|---|
| **A** | 5 | 5 | 4 | 5 | 5 | 4 | **28** |
| B | 3 | 2 | 5 | 4 | 3 | 5 | 22 |
| C | 2 | 4 | 3 | 5 | 5 | 3 | 22 |
| D | 4 | 2 | 5 | 3 | 2 | 4 | 20 |

Changes since revision 1:
- B's runway rises: with forgeries ON, its forger is its strongest beat.
- C's self-teaching rises: THE CODE legend.
- D rises one point for leaving the Relay's look open.

### E.3 What the player learns, and when

| Beat (LD §5) | A | B | C | D |
|---|---|---|---|---|
| First dark (5–45 s) | Where the light dies, the paper points | Someone wakes and points | Scratches bloom | The huntsman moves off |
| First door (≤ 60 s) | Green leads to new ground | Their way leads somewhere new | GO ON works | The course goes forward |
| First STOP (≤ 2 min) | NO EXIT means a pocket | The bars mean someone was cornered | DEAD END | An earth |
| First sag | The whole plan for 4 s | They crowd to look | Everyone stops to read | A stretch of course |
| First chase | A door you can shut buys time, not safety | They flee too | RUN marks | The view: jumps ahead |
| Run 2 | No light in, no light out; tenant space has no plan | No light, nothing to show | Lamps on, then read | Off the country |
| T2 | Every exit leads in | They're closer | No date after 1990 | Runs end |
| T3 | Others were here | They watch | They're gone | Not the first quarry |
| T4+ | Check the light: paper never outshines its lamp | She wants company | Someone wants visitors | The hound cheats |

The optional **GROUND figure layer** at T4+ (small creeping figures tiled per roll, visible only during a sag from ≥ 6 m) is Gilman's "great many women". It belongs most naturally to B, could be added to A as the occupants, and does not fit C or D.

---

### E.4 The recommendation Red accepted

**Red chose EGRESS on 2026-10-03, as recommended.** It is the only direction where every LD rule has its own in-fiction reason without a second author or a stretch. It is grounded in real 1990 objects, and it fixes nothing about the Relay while the hunter design is open.

**When another direction would have won** (kept for the record):
- **B**, if the deck needs the single scariest image and Red accepts that the game turns overtly supernatural. B's forger is the best of the four.
- **C**, if warmth and an easy pitch matter more than tight logic.
- **D**, only if Red wants the ink to be a lure first and help second, which changes LD's thesis.
