# 10 · FrontRooms tutorial proposal (v1)

Written 2026-10-22 by 教程设计.

**Status: APPROVED by Red, 2026-10-22,** in the 教程设计 chat (`local_c160c950-9b0e-4f15-ad77-95c2e4930c87`): "全部按推荐（T1=A，T2=E1，T3 静止屏，T5 去掉文字卡，T6 标题电视带声音）".
- T4 (the placard sheet) is Red's pick through 平面视觉's TP04 calls, not this doc.
- The C24 amendment for Floor 2+ arrows (§7) is 系统设计's question to Red.
- Everything lands after the Alpha (handed in at class on 10-22).
- The answers are in §12.

**Inputs.**
- Red's request (10-22, after the class): no text pop-ups; Idea A is the TV beacon; Idea B is the evacuation plan as the tutorial; plus a survey of AAA horror tutorials.
- Research: `01_research.md`, with rules R1–R10 in its §7. Sources: `SOURCES.md`.
- Decided system rules, from systems.json 2026-10-22c:
  - C14: look + E keys;
  - C15: about 1 door in 3 locked after L5;
  - C18: first-key close-up;
  - C22: one EXIT per floor, a stairwell one floor down, a run ends only when caught;
  - C23: a LIT EXIT sign over the EXIT door;
  - C24: the route points at the EXIT, which sits 14–22 cells of walking from the start door.
- Hard rules (COMMON §4):
  - cues are never a radar;
  - no cue points at glass;
  - darkness is never cover;
  - no Relay state on the HUD;
  - photosafety;
  - era lock 1990;
  - the audio contract.

**Tags.**
- **SP**: a starting value to tune in playtest.
- **ESTIMATE**: my guess, for the owner to confirm.
- **LIVE / BUILT / DESIGN**: as in systems.json.

---

## 0. The short version

- **Recommendation: Option A, "the TV keeps the stair key".**
  - Every floor's EXIT is a stair door, and it is **locked**.
  - Its key hangs in the **one room on the floor where a TV is playing**: an attendant's station.
  - The TV's broadcast is heard through the building: muffled and bassy far away, clearer through each doorway, clear in the room.
  - Taking the key switches the TV off. That dying set is the device's own sound (RELAY §6): something has been sent to that room.
  - The player walks back to the EXIT, unlocks it, and goes down a floor.
- **The evacuation plan by the start door is the rule sheet:**
  - one plan, four short imperatives and a floor number;
  - true in play from the first second;
  - repeated on every stairwell landing.
- **Floor 1 is the tutorial version of the same floor:**
  - the station sits just past the EXIT, so the player sees the locked stair door before they find the key;
  - the hunter is not sent until the key is taken, so the first minute is safe;
  - its first approach is announced by the room's lamps (W1, already live).
- **Nothing pops up.**
  - Verbs keep their crosshair prompts (E · TAKE KEY, E · UNLOCK).
  - The 9 s "Shift to sprint…" card goes.
  - Direction never comes from text.

---

## 1. What the tutorial has to teach (the curriculum)

| # | Lesson | Why it matters | Taught by | Live today? |
|---|---|---|---|---|
| L1 | **There is one way on: the EXIT stair, one per floor** | 8 of 9 testers could not name a goal | Placard plan + the LIT EXIT sign (C23) + the route (C24) | EXIT: DESIGN (N1, 关卡设计 building) |
| L2 | **The stair door is locked; its key is where the TV plays** | Gives keys a purpose; gives the floor a centre | Placard line 4 + the beacon + lock-before-key on Floor 1 | DESIGN (this doc) |
| L3 | **Look + E does things** (take, open, read) | Every verb | Crosshair prompts (C14, EXAMINE) | LIVE |
| L4 | **Walk. Running is heard** | Sprint steps are a 26 m noise; walking is silent at T1 (**desktop only**: on phones a walk is heard, 9 m, and only the slow gait is silent; `TOUCH_CONTROLS.md`; phone lesson = X2's `SLOW · UNHEARD` legend entry, Q9 desktop parity queued for Red by 交互设计) | Placard line 1 | Noise LIVE; lesson today = text card |
| L5 | **Shut doors slow it; it breaks them when hunting** | Doors are the main defence | Placard line 2; hearing it break a door | LIVE (v1) |
| L6 | **Your lamps dipping together means it is near** | The first warning | Placard line 3; Floor 1's first approach | W1 LIVE (10-22) |
| L7 | **If it sees you, it runs you down; being caught ends the run** | The rule of the game | The first chase, after the warnings | LIVE |
| L8 | **Glass breaks with a held E and is loud** | A loud shortcut | Systemic (glass is not on the critical path) | LIVE |
| L9 | **The building goes down, floor after floor** | The run's shape (C22) | Stairwell + the floor number on each landing | DESIGN |
| L10 | (later) **In the dark, the wallpaper points the way** | The ink cue (W6) | Placard glow legend, first-dark beat | CLONE_ONLY |

L1, L2, L6 and L7 are the core. L10 waits for W6, and the placard must not promise it before then (§5.1).

---

## 2. Three options

| | **A · The TV keeps the stair key (recommended)** | **B · A TV in every key zone** | **C · Tutorial floor only** |
|---|---|---|---|
| The goal | The EXIT stair, **locked**; its key at the floor's one playing TV | The EXIT stair, unlocked; zone keys open locked doors (C15 as decided) | The EXIT stair, unlocked; zone keys (C15) |
| TVs playing | One per floor, until its key is taken | The TV of the zone you stand in, until its key is taken | Floor 1's only |
| Keys | One **floor key**: opens the EXIT and any locked door on that floor. Replaces zone keys (a C15 change for 系统设计 to record) | Zone keys as decided | Zone keys; Floor 1's key opens Floor 1's EXIT |
| The placard says | "Stair door locked. Key at the attendant's station (TV)." | "Locked doors open with this zone's key. The key is by the zone's TV." | Floor 1 only |
| Strengths | One rule, true on every floor. Lock before key. Every key matters. The TV room is a real level space, not idle space (Red's aim) | Keeps every decided rule | Cheapest |
| Weaknesses | Changes C15. One more step per floor | Zones are about one chunk (8 × 8 cells), so a playing TV every ~24 m: beacons overlap and become routine. With 1 door in 3 locked, many keys are never needed | Teaches a rule that stops being true on Floor 2. Later TV rooms become idle space again |
| Cost, ESTIMATE | Map 2–3 d, visual 2 d, sound 1.5–2 d, graphic 1 d, narrative 0.5 d | Map 3–4 d, visual 2 d, sound 2 d, graphic 1 d | Map 1.5 d, visual 1.5 d, sound 1.5 d, graphic 1 d |

**Why A.**
- It turns Red's Idea A into the game's goal loop, not a one-off:
  - hear the TV, take the key;
  - the building reacts (it is sent to that room);
  - get to the stair, go down.
- It follows the research:
  - R1 goal first;
  - R2 rehearse once;
  - R5 lock before key;
  - R6 one sign, repeated;
  - R7 loud state change.
- Option B keeps today's rules but makes beacons constant, and Outer Wilds shows that several signals at once confuse players (`01` §3.4).
- Option C breaks R6: a lesson has to stay true.

---

## 3. Floor 1, step by step (Option A)

Times are for a first-time player who walks. Owner and status are at the end of each row.

| # | When | What the player sees and hears | What it teaches | Who builds it |
|---|---|---|---|---|
| 0 | Title loop | The title corridor's TV plays the WTCZ ad **with sound**: the FRONT ROOMS jingle, clear and close | "This tune comes from a TV." Outer Wilds' fix: teach the source once (`01` T3) | Sound (event), visual (title TV) · DESIGN |
| 1 | Space | Control in place, as today. The start door opens when the maze is ready. **Through it, faint and bassy, the same jingle plays somewhere ahead** | "The TV is out there" | Sound + map (beacon data) · DESIGN |
| 2 | 4 m in | The start door swings shut behind them and latches (today). The sound makes them turn: **the placard** hangs where their view lands, lit (mount P1 west, §5.3). E on it opens EXAMINE, and the game pauses | L1, L2, L4–L6 in four lines; the floor number | Graphic (art), narrative (strings); the prop and EXAMINE are LIVE |
| 3 | 0:10–0:40 | Following the sound: at each junction it comes from **the doorway** on the way to it, and it gets less muffled with each room. A cold, moving light spills from one doorway ahead | Sound and light point the same way (R10) | Map (portal data), sound, visual (TV light) |
| 4 | On the way | The route passes **the EXIT**: a lit EXIT sign over a steel door (C23). If they try it: the locked rattle and the padlock on the crosshair (LIVE grammar) | Lock before key (R5): "I need something for this door" | Map (placement rule) · DESIGN |
| 5 | 0:40–1:10 | **The station**, 2–6 cells past the EXIT: an attendant's desk or a waiting area. Chairs face a TV on a stand, still playing; the room's own lamps are dim. On the key rack beside it, one key, lit by the screen | "Something is going on here" (Red's words); the key is the thing | Visual (module look), map (module + KeySpot) |
| 6 | Take | E · TAKE KEY. First key of the run: the hand-off close-up (C18) shows the tag: `STAIR A` (placeholder; narrative) | Why this key: it is the stair's | LIVE close-up; tag string DESIGN |
| 7 | +0.4 s | **The TV switches off**: the picture collapses to a dot, the power-off zip, the jingle stops. The room drops to its dim lamps. No panel tick: on a device call the device's own sound replaces it (RELAY §6), and here the dying set is that sound | A loud change of state (R7): "I set something off" | Visual (collapse), sound (zip) |
| 8 | +3 s | The hunter is sent **to the station**, arriving out of sight at least 14 cells (42 m) of walking from the player (§6) | — (unseen) | Map / Relay code |
| 9 | 1:10–2:00 | The player heads back to the EXIT, 2–6 cells away. Within seconds their room's lamps **dip together** (W1 stage 1, LIVE) | L6: "lamps dipping = it is near"; placard line 3 pays off | LIVE |
| 10 | At the EXIT | E · UNLOCK: the head-dip unlock shot (LIVE), then the door opens on a stairwell. It shuts and locks behind them (C22) | L2 closes | Map (C22 minimum) |
| 11 | Stairwell | Safe. The floor number is painted on the landing wall (NYC LL5 floor numerals, `stairwell/01` S4) and **a copy of the plan, the next sheet**, hangs by the far door | L9: progress number (Exit 8 pattern); a re-read | Visual + graphic |
| 12 | Floor 2 | Same rules: hear the TV, take the key, reach the stair. The station is farther and off the EXIT route; the hunter is already out | Rehearsed → tested (R2) | Map |

**If the player lingers or wanders.**
- **They dawdle in the station after the take.** It arrives there. Lamps dip first (W1), then steps (W2, once 声音设计 delivers), then the chase. The first catch always follows a warning.
- **They never find the TV.** The hunter is released anyway 150 s (SP) after the start door shuts, or when they enter a third zone, whichever comes first. The game does not wait forever.
- **They reach the station before the EXIT.** That is fine: lesson 2 pays off at the door instead. The placard already said the stair is locked.
- **They break glass.** Glass is never on the critical path (C22 placement rule: the EXIT is reachable without glass), and L5 also checks that the station is reachable without glass and without keys.

---

## 4. Idea A: the TV beacon

### 4.1 Fiction
- **The station is the floor's attendant post.** A 1990 office floor's keys hang at the guard or reception desk. UBC 1991 and NYC LL5 make stair doors that lock and are released from a control station period-true (`stairwell/01` §0).
- The attendant is gone, but their set is still on: WTCZ-TV 61, `STAY TUNED.`, airing the Front Rooms Furniture ad (`screens/30_screen_strings.md` §1–3). (The ad video in the game today, Ad_02, still shows WXRM until Red OKs Ad_03 in 平面视觉 · 广告's chat.)
- **Taking the stair key from the rack is an alarm condition**, in EGRESS terms: a device trips, and the panel sends its responder to that device, never to you (`relay_pursuit/30_narrative.md` §1). The TV dies because the panel drops the station's non-emergency circuit.
- **No new fiction is invented beyond the station.** Station, set, ad and panel already exist in the canon.

### 4.2 The room (Level Designer modules)
- **Two modules** (ESTIMATE 2–3 cells a side, Standard height):
  - **Station · Waiting** (Level 0 theme): two short rows of chairs facing a TV on a rolling stand; a reception counter; the key rack on the wall behind it.
  - **Station · Desk** (Office theme): a guard desk with the TV on its end; a desk lamp; the key rack beside it.
- **Kits that exist:** `Kit_CRTTV` (screen anchor), `Kit_KeyHookBoard` ("key rack", A09), `Kit_KeyRing`, `Kit_KeyTag_*`, `Kit_TableLampPleated`, the chair and desk kits.
- **One KeySpot marker** on the rack (host: the rack); everything else is ordinary props.
- **Lamps.** The TV side's lamps are Dim or Off in the module (Level Designer lamp modes), so the TV's light is the brightest thing in the room (L4D's single focal light, Rogers' squint test). At least one lamp by the entrance stays lit, so W1 can still burst in the room.
- **Doors.** Modules cannot author doors (the map makes them). Not needed: the sound path and the light spill work through arches and open edges.

### 4.3 Placement (map side; rules for 关卡设计)
- **One station per floor.** It keeps its room through revisit shifts, as keys do.
- **Floor 1:** 2–6 cells of walking **past the EXIT door**, so the shortest route from the start door to the station passes within sight of the lit EXIT. This is Unexplored's path-A/path-B cycle in miniature (`01` T10).
  - On Floor 1 the EXIT takes the near end of C24's band (14–18 cells), so the station is at most about 24 cells (72 m) of walking from the start door. That is inside the beacon field's 90 m, so it is faintly audible from the first second.
- **Floor 2 on:** 10–20 cells of walking from the stairwell arrival, off the shortest route to the EXIT (a branch), so the floor makes you explore.
- **Never in a Tall zone.** Reachable without glass and without keys: L5's key check, with the station as "the key".
- **Not within 6 cells of the EXIT on Floor 2+,** so the two landmarks do not compete (R10).

### 4.4 Only one TV plays
- Red's rule: until the key is taken, the station's room is the only room with a TV playing (sound and light).
- **Desk monitors** (I6, today all playing the ad silently): they show a **still, dim office screen** instead. Since 10-22 (平面视觉 · 广告's v2, after 信息设计's review against `computer_ui/20_design.md` §3.5 / AT-T19) that is a **dark field with one still block cursor, or a blanked field, with no text**: `desk_cursor`, `desk_cursor_low`, `desk_blank` in `Tools/screens/out/still_desk/` (README there). The old text frames (`C:\>_`, `PLAN SHEET A-3 OF 4`, `SYSTEM IDLE / OCCUPANT LOAD 1`) are superseded: body-size text faked a READ terminal. Optional glance frames stay empty until 叙事设计 signs era-checked words. Nothing moves on them, and they make no sound.
- **The title corridor TV** keeps the ad and gains sound (step 0). It is gone once the start rooms are destroyed.
- **After the key:** the station TV is off for good. Desk monitors stay still. A new floor brings a new station TV.
- **So T3 retires I6 in the maze.** The desk monitors never play the ad there again (Red approved I6 on 10-14, A05). The ad stays on the title corridor TV and on each floor's station TV. The alternative is desk monitors that resume the silent ad after the take; I do not recommend it, because moving pictures across the floor right after the take would read as "another TV".

### 4.5 The sound

**Content.**
- One WTCZ loop matched to the 16 s ad: the station ident, then the FRONT ROOMS jingle.
- Neither ad file has an audio track (P9), so the loop is new.
- 平面视觉 · 广告 already made an original, synthesised jingle (120 BPM, cuts on the beat), an ident and CRT set sounds for the promo, with nothing downloaded (`Tools/screens/promo_audio.py`). That is the starting material.
- Mix it so the tune survives a low-pass: a strong bass line and kick for far away; a mid-range hook (whistle, organ or voice) that turns clear near.
- **No bell, glockenspiel or chime timbres**, because the lock-on chime must stay the only tonal chime in the game (RELAY §9.4).

**Distance from the path, not the straight line.**
- **A beacon field seeded at the TV's cell**, the same machinery as C24's field seeded at the EXIT door. It reaches about 90 m (SP), so the TV is audible from the start door on Floor 1 (§4.3).
  - The player-rooted 45 m field the Relay's hearing uses (`FrontRoomsMapPathField`) is too short for this.
  - The field changes only with the station, built topology or an opening state, so it is not rebuilt per frame.
- **From the player's cell it gives:**
  - path distance P;
  - the number of shut doors on the path (hearing's rule: 9 m per shut opening);
  - the next cell toward the TV.
- **It treats glass as impassable,** as C24's route does, so the sound never seems to come through a window.
- **Beyond the built map:** use the nearest built border doorway that lowers the distance, as C24 does.

| Band | P | Gain vs in-room | Low-pass | Reverb | What you hear |
|---|---|---|---|---|---|
| In the room, or in sight ≤ 6 m | ≤ 6 m | 0 dB | none; the set's 15.7 kHz whine faintly within 2 m | dry | words, tune, the set's hum |
| Next room | 6–15 m | −6 to −12 dB | 6 → 2.5 kHz | some | the tune; words blurred |
| Two rooms away | 15–30 m | −12 to −20 dB | 2.5 kHz → 800 Hz | more | rhythm and bass; no words |
| Far | 30–60 m | −20 to −28 dB | 800 → 350 Hz | mostly wet | a thump through the walls |
| Very far | 60–90 m | −28 to −32 dB | 250 Hz | wet | barely there: "a TV somewhere" |
| Beyond the field | > 90 m | silent | — | — | — |

All values are SP. 声音设计 sets the absolute level against the room tone; the far bands must stay audible over the HumBed.

**Per shut door on the path:** −5 dB and the cut-off × 0.6 (SP). A wall loses about 6 dB per doubling of frequency (mass law, `01` T17), so a door is a low-pass, not just a volume step. Parameters slew over 0.5 s, so **opening a door audibly "opens" the sound**: a state change the player feels.

**Where the sound comes from.**
- In the room or in sight: the TV itself.
- Otherwise: **the doorway the sound leaks through**. That is the first opening on the path from the player's room toward the TV: follow the beacon field's next-cell links from the player's cell until the path leaves the player's room set. When that doorway changes, the position crossfades over 0.3–0.5 s.
- This is how TLOU, Rainbow Six Siege and Wwise portals let a sound lead the player through rooms (`01` T2, T14, T15).

**Mix priority.**
- The beacon ducks −6 dB under every Relay cue: the W1 ballast groan, W2 steps and hum, the lock-on chime.
- It ducks −12 dB during a chase.
- It pauses with the game (Esc, EXAMINE).
- **A warning is never masked by the beacon.**

**Accessibility.**
- Captions (an assist, off by default):
  - `[TV, muffled]` with AHEAD / LEFT / RIGHT / BEHIND from the doorway position, at most once per 20 s;
  - `[TV]` in the room;
  - `[TV switches off]`.
- The "Visual sound cues" assist (P3) may show the beacon like a stage-2 cue.

**Phones.** Phone speakers lose the bass that carries the far bands. Behind `FrontRoomsHandheld.Active` only, raise the far bands' cut-off floor to about 600 Hz and their gain by about 4 dB (SP). Desktop is unchanged (COMMON rule 5).

**It is not a noise source.**
- The Relay never hears the TV.
- The TV does not mask the player's steps.
- The beacon's only cost is the key take (§6). That keeps one meaning per cue (C09's reasoning).

### 4.6 The light
- One light at the screen. Its colour is the video's mean colour and its intensity follows mean luminance, sampled every 0.1 s and low-passed (τ 0.12 s, SP). Range about 6 m.
- Soft shadows of the chairs on the desktop tier (highest spec, COMMON rule 6); shadows off on WebGL.
- **Photosafety.** No more than 3 luminance swings of 10 % or more per second (WCAG 2.3.1, as RELAY §9.5). With Reduce flashing, τ 0.5 s.
- The spill through the doorway is the beacon's short-range visual. In a lit corridor it reads as a moving cold glow in one doorway.

### 4.7 The take
1. **E · TAKE KEY** (desktop look + E, C14; phones walk over until the tap path lands).
2. **First key of the run:** the EXAMINE hand-off (C18) shows the tag.
3. **+0.4 s:** the picture collapses to a bright dot that fades (0.6 s); the power-off zip; silence. The room light drops to its dim lamps.
4. **No panel tick.** RELAY §6: a device call plays the device's own sound instead of the CALLED tick. Here the device is the station, and its sound is the dying set (step 3). (Changed 10-22 after 系统设计's review.)
5. **The HUD key panel** shows the key (HUD B family, already designed).
6. **The hunter is sent to the station** (§6).
- **Same on every floor**, so the rule stays true: taking the station key always calls it to the station.

### 4.8 Rules check (beacon)

| Rule | How the beacon keeps it |
|---|---|
| Never a radar | It marks a fixed room and never reads the Relay |
| No cue points at glass | Its path field treats glass as impassable; its doorway position is never a window |
| Darkness is never cover | The station's dim lamps are décor; nothing implies hiding |
| No Relay state on the HUD | None |
| Photosafety | §4.6 limits; Reduce flashing |
| Era lock 1990, no brands | CRT set, WTCZ (fictional call sign; it replaced WXRM on 10-22 because WXRM-LP is a real licensed LPFM station, FCC facility 197239; `screens/30_screen_strings.md` v2.1, rule 11), Front Rooms Furniture, synthesised jingle |
| Audio contract | FMOD events only; `FrontRoomsScreenVideo` keeps `audioOutputMode None` |
| The only tonal chime | No chime timbres in the jingle |
| Artlist | Not used (P8 is synthesised) |

---

## 5. Idea B: the evacuation plan as the rule sheet

**Owner of the sheet's design: 平面视觉** (the tutorial-placard session).
- It made three options on 10-22, with layouts, type, legibility and guarantees: `research/placard/50_tutorial_placard.md`, Figma `3459:7280`, VL332.
- **Red picks the sheet there** (its four calls, TP04). This section does not ask Red again. **Picked 2026-10-22: sheet A** (Red in 平面视觉's chat: "A 先上"), shipping as drawn; Phase 0 art is `sheetA_phase0/`, Option A art is `sheetA/`.
- It says what the tutorial needs from the sheet and how that fits 平面视觉's T1–T10. Where we differ, §5.2 says so.

### 5.1 The current placard (art v3) against today's game

| Part | What it tells the player | What the game does | Verdict |
|---|---|---|---|
| `EVACUATION PLAN` | A real plan | — | Keep |
| `YOU ARE HERE` inside the start room | You are in that room | The reader stands **outside** it, on the map side. The start rooms are destroyed once the door shuts | Misleading. Also against NYC LL5 (1973), which asks the diagram to show **where it is posted** (placard `10_spec.md` S13). **Move the dot to the posting spot and turn the plan to the reader** (平面视觉's rule 2, ISO 23601) |
| The start door labelled `EXIT`, with the dashed red route to it | The way out is the door you came through | That door shut and locked behind you. After C22 the EXIT is a stair 14–22 cells away | **Wrong after C22.** The start door becomes `NO RE-ENTRY` (平面视觉's rule 1); the route leads to the stair, labelled EXIT |
| Match line, `SEE SHEET A-3` | The rest is not on this sheet | True: the floor is generated, and the walls' stamp is A-3 | Keep: it honestly explains a partial plan |
| Column grid, north arrow, scale | Period detail | — | Keep |
| Glow legend `IN POWER FAILURE`: `WAY ON` / `EXIT` / `NO EXIT` | The wallpaper points the way in the dark | The wall arrows (W6) are CLONE_ONLY, not in main (N4) | A promise the game cannot keep yet. **Keep it only in a build that ships W6;** until then print it without the glow, or drop it (D17 kill switch) |
| Footer `WALK, DO NOT RUN.` / `CLOSE DOORS BEHIND YOU.` | Walk; shut doors | Sprint steps are a 26 m noise and walking is silent at T1. A shut door adds 9 m to its hearing path and stops a wander. On v1, shutting a door is itself a 14 m noise | True enough, and v2's lesson. Keep, merged into the instructions |
| Title block `LEVEL 0` · `SHEET A-2 OF 4` · `PRINTED 03/90` | Which floor | Floors exist after C22 | **Make LEVEL the floor number**; each stairwell copy carries its own |
| (missing) the key, the station, the lamps | — | W1 is live; keys are about to matter | **Add** |

### 5.2 What the plan must teach (contract for 平面视觉 and 叙事)

**How 平面视觉's T1–T10 (`50` §1) fit this proposal.**
- They cover L1–L7 and L10 already.
- Under Option A, three of their rows tighten:
  - **`50`'s T3 / G4 / A1:** the locked door on the route **is the EXIT door itself**, and **one floor key** opens it, not a zone key. Their A1 ("the EXIT sits behind at least one locked door whose key hangs in a TV room") always holds.
  - **`50`'s T4 / A2:** **one TV room per floor**, not one per zone.
  - **`50`'s T6 / G5:** print a signal only when it is live. Today that is the lamps (W1) only. Steps (W2) and the chime (W3) join when 声音设计 delivers them.
- **One string to settle with 叙事:** the DRAFT `CHIME: IT HAS SEEN YOU. RUN.` breaks the existing writing rule "never RUN or HIDE" (`screens/30_screen_strings.md` §5) and sits next to `WALK, DO NOT RUN`. W3 is not live, so the row is cut for now anyway (G5).

**The minimum the tutorial needs from any option:** at most four short imperatives plus a progress number (The Exit 8 pattern, `01` §5). Then:

1. **Plan.**
   - `YOU ARE HERE` at the posting spot, beside the start door.
   - The start door unlabelled, or marked as the way in.
   - A dashed route leaving across the match line.
   - At the far end, the **stair** symbol labelled EXIT.
2. **Legend.**
   - STAIR (EXIT).
   - The attendant station (a desk-and-TV pictogram with a key).
   - YOU ARE HERE.
   - Plus the glow rows only with W6 (§5.1).
3. **Instructions, four lines.** Exact wording is 叙事's; these are placeholders:
   1. `WALK. DO NOT RUN.` (L4)
   2. `CLOSE DOORS BEHIND YOU.` (L5)
   3. `IF THE LIGHTS DIM, LEAVE THE AREA.` (L6: tells you something is near, not where)
   4. `STAIR DOORS ARE LOCKED. KEY AT ATTENDANT STATION.` (L1 + L2)
4. **Progress number.** `LEVEL` = the floor, also painted in the stairwell.

**Writing rules** (from RELAY §9.4 and `screens/30` §5):
- never `RUN` or `HIDE`;
- nothing that implies darkness hides you;
- never the word "relay";
- no live threat information.

**Open for 叙事 and Red:** whether a line names the hunter's role (for example "floor warden", the person who checks every room in a fire drill). My recommendation is to name the role at most, never the body. The hunter's look is still open (`research/hunter`).

### 5.3 Where and how it is read
- **Mount: 平面视觉's G1, P1 west on every seed** (the reader's right of the start door, facing into the map). A turned plan needs one fixed mount for the dot to be true.
  - It also fits the attention beat: the door's shut at 4 m is a sound behind the player, and turning toward it puts the door facade, and P1 beside it, in view.
  - My earlier note to 平面视觉 suggested P2; G1 supersedes it.
- **It is lit:** the start group's lamps are forced on once the door shuts (`20_level_design_phosphor.md` §4).
- **EXAMINE** (E · READ, the look-eye) frames it and pauses the game (A07). Reading is safe here because the hunter is not out yet on Floor 1. On later floors the stairwell copy is inside the safe stairwell.

### 5.4 Repeats
- **A copy hangs on each stairwell landing:** the next floor's sheet, with its LEVEL number. One prop, one art swap (D17). Every floor starts with the rules.
- **Later (Phase 2), a station plan** beside the key rack: `YOU ARE HERE` at the station and a block plan of the floor's zones with the STAIR marked.
  - Zones and borders survive revisit shifts (MAP_GENERATION "Revisits shift"), so a zone-level plan stays true.
  - It answers "where now?" right after the key: Silent Hill 2's and RE2's room maps (`01` §5).
  - Cost: a texture drawn at run time.

### 5.5 Phase 0 can ship first
The prop takes new art of the same size with no mesh or code change (D17). So the plan's fixes can land before any of §4:
- the dot;
- the EXIT;
- the floor number;
- the four lines;
- the glow legend held back.

Until the EXIT is locked, line 4 must read without the key (for example `EXIT BY STAIR`), and the key line joins once Option A lands.

---

## 6. The first encounter with the hunter

| | **E1 · Sent by the key (recommended)** | E2 · Seen on a monitor first (later) | E3 · Today's timing |
|---|---|---|---|
| When it comes | Floor 1: not sent until the station key is taken (fallback 150 s, or a third zone). Floors 2+: out as today; every key take sends it to the station | As E1, plus a one-time black-and-white security monitor at the station that shows it crossing a corridor for a few seconds before the take | Released 3 s after the start door shuts, 9–15 cells away |
| What the player learns first | The rules (placard), the goal (TV, key, stair), then **lamps dip → it is near** | Its look and gait, safely | Often nothing: round-two runs ended at about 165 s |
| Research | Alien: Isolation holds the hunter back and unlocks its behaviours in stages; RE2's Mr. X comes after a build; hear it before you see it (`01` §2) | Evil Within watches the butcher first; RE7's tapes (`01` T26, T38) | Outlast's early scare (but Outlast has a goal first) |
| Cost, ESTIMATE | Map 0.5–1 d on v1 (the key take is a large noise at the station; Floor 1 release gate) | +1.5–2 d (render-texture camera, monitor look); the hunter's look must be final; one-time only, or it becomes a radar | 0 |

**E1 in detail (Floor 1).**
- **The send.** 3 s after the take, it arrives out of sight **at least 14 cells (42 m) of walking from the player**, the same hard rule as v2's Arrive (RELAY §5), preferring the side away from the EXIT, at a Relay entry if a module has one. The lamps then start while it walks in, not at the moment it arrives. (Was 10–12 cells from the station; 系统设计 pointed out that is 30–36 m from a player still in the station.) Its first leg walks to the station: a Hunt, at hunt speed, never a chase.
  - On v1 this is the existing Noise → Hunt with an unlimited radius at the station.
  - On v2 it is a device call (B2 director, L2 devices).
- **The first approach is announced.** Under E1 the player is 2–6 cells from the EXIT, walking away from the station while it walks in. W1 bursts at W ≤ 30 m are very likely before any sighting.
- **First-approach grace (Floor 1 only; v1 bridge).** Its first chase cannot start until the player has had at least one W1 burst, unless they are within 6 m.
  - It is invisible, and the same idea as v2's G3 gate (warnings first).
  - Alien: Isolation's early, limited behaviours are the precedent (`01` T8).
  - Off on Floor 2+.
- **If they linger** they meet the full ladder: lamps, then steps (W2, when delivered), then sight, then chase. The first catch is earned.

**No cutscene.** The camera stays with the player (`01` §6). The hunter is introduced by its warnings and then its body, in play.

---

## 7. What changes after the key

| | Before the take | After the take |
|---|---|---|
| Station TV | Plays, sound and light; the only moving screen on the floor | Off for good: collapse, zip, silence |
| Hunter | Floor 1: not sent. Floor 2+: out | Sent to the station; staged warnings as it nears |
| EXIT | Locked (rattle, padlock) | E · UNLOCK opens it (head-dip shot, LIVE) |
| HUD | — | Key panel shows the key |
| Route (C24 / W6, when built) | Points at the station (reads the beacon's station field; A20) | Points at the EXIT |
| Desk monitors | Still screens | Still screens |
| Floor end | — | Through the EXIT, the door locks behind; stairwell; next floor |

On Floor 1 the beacon and the route agree, because the station is past the EXIT.

**Floor 2+: settled by A20 (Red, 2026-10-22, in 系统设计's chat: "选 1：拿钥匙前箭头指向值班台，拿到后指向 EXIT").**
- The W6 wall arrows turn at stages 1–2 whenever the hunter nears (RELAY §9.6), and on Floor 2+ it is out before the take.
- So before the take, the arrows would point at the locked EXIT while the beacon pulls toward a station ≥ 6 cells off that route. That breaks R10.
- **The fix (A20, amends C24; systems.json 2026-10-22g, RELAY §9.6):** before the take, the calm route reads the beacon's station field (M5); after the take, the EXIT field. So the arrows and the beacon never pull two ways.
- W6 is not in main yet, so nothing conflicts today.

---

## 8. Later floors and escalation

- **The loop is the same on every floor:** placard, beacon, key, call, stair. What escalates is the hunter (tiers, as decided) and the distance (stations farther from Floor 2).
- **From Floor 2, locked interior doors** (C15's about 1 in 3) can return, opened by the floor key. Before the key they shape the route; after it the floor opens up. **Floor 1 has none.**
- **The beacon never lies.**
  - EGRESS forges FLOW marks from tier 4, but the beacon is the tutorial's anchor and keeps its meaning.
  - Recommendation: no fake TVs, no TV that plays after its key is gone.
- Tiers make the hunter better, never the hints fewer (RELAY §9.6).

---

## 9. What goes

- **The 9 s text card** "Shift to sprint. About 5 seconds, and it hears every step." (`CalmHint`).
  - Placard line 1 carries the lesson (the run part on every platform; on phones the silent slow gait is taught by X2's legend entry, since a phone walk is heard).
  - The controls move to the title screen and the pause card, as one small legend: WASD · MOUSE · SHIFT · E (current wording: `20_requests.md` M1 status, `placard/50_tutorial_placard.md` §11).
  - The touch hints are 交互设计's call.
- **The placard's start-door EXIT** (§5.1).
- **Any `Flash()` text that gives direction.** Verb feedback such as "GLASS BROKEN / WALK INTO THE FRAME" is a verb hint, not direction, so it can stay.

---

## 10. How we will know it works (next playtest)

| Measure | Target | Today |
|---|---|---|
| Can name the goal after Floor 1, unprompted | ≥ 7 of 9 | 1 of 9 |
| Reach the station within 90 s of the start door | ≥ 70 % | — |
| Open the placard (EXAMINE) on Floor 1 | ≥ 50 % | — |
| Clear Floor 1 on the first run | ≥ 60 % | — |
| First catch comes after at least one W1 burst | ≥ 90 % | — |
| Can say what dipping lamps mean after the run | ≥ 5 of 9 | — |
| Ever confuse the TV with the hunter's sounds | 0 | — |

关卡设计's autopilot report can log the timed ones (Space → station, take → EXIT, EXAMINE opened).

---

## 11. Phases (all after the Alpha)

| Phase | What | Needs |
|---|---|---|
| 0 | The new placard sheet (平面视觉's option, Red's TP04 pick; §5.5); drop the `CalmHint` card; controls on title and pause | 平面视觉, 叙事, 游戏视觉 (pack), 关卡设计 (HUD, G1/G2); no new systems |
| 1 | Option A core: locked EXIT + floor key, station modules and placement, beacon sound and light, the take beat, Floor 1 send (E1), still desk screens, title TV sound | After C22's stairwell minimum (关卡设计, in progress) |
| 2 | Stairwell placard copies and floor numbers; station plan; C23 chevron signs; W6 arrows; W2 steps; CCTV glimpse (E2) if wanted | Each on its own line |

---

## 12. Decisions for Red (answered 2026-10-22: all as recommended)

| # | Question | Recommendation |
|---|---|---|
| T1 | Which option: A (the TV keeps the stair key), B (a TV per key zone) or C (Floor 1 only)? | **A** |
| T2 | First encounter: E1 (sent by the key, Floor 1 calm until then) or E3 (today's 3 s release)? | **E1** |
| T3 | Desk monitors in the maze show still office screens (this **retires the live I6 ads in the maze**; the ad stays on the title TV and the station TVs), go dark, or keep playing the ad silently? | **Still screens** |
| T4 | The placard: **not asked here.** Red picks the sheet through 平面视觉's four calls (TP04 in `placard/50_tutorial_placard.md`): option A for the next build, then C; no drawn enemy; the v3 wall legend off until W6; DRAFT strings until 叙事. This proposal fits their A and C (§5.2) | — |
| T5 | Remove the 9 s "Shift to sprint" card and put the controls on the title and pause screens? | **Yes** |
| T6 | The title corridor TV plays the ad with sound, so players learn the tune before the maze? | **Yes** |

**Answered.** Red: "全部按推荐（T1=A，T2=E1，T3 静止屏，T5 去掉文字卡，T6 标题电视带声音）" (2026-10-22, this chat). So T1 = A, T2 = E1, T3 = still screens (I6 retired in the maze), T5 = remove the card, T6 = title TV with sound.

**Because T1 = A,** two system rules change, and 系统设计 records them; Red is not asked again.
- C15: zone keys become one floor key, which opens the EXIT and any locked door on its floor; Floor 1 has no locked interior doors.
- New: "taking the station key sends the hunter to the station".

## 13. Risks

- **The beacon is missed.** Players might not notice a quiet, far sound.
  - Stack the cues: sound + light spill + the route passing the EXIT (Floor 1).
  - The title TV teaches the tune.
  - Measure it (§10); if missed, raise the far band.
- **It gets confused with the hunter.** Keep the families apart: broadcast music and voice vs steps, hum and the one chime. The beacon ducks under every warning. Measure it.
- **Phones.** Speaker bass loss; mitigated in §4.5.
- **The key take feels like a punishment.** It is the floor's one decision point, and it is fair: warnings first (E1), and the EXIT is close on Floor 1.
- **Scope.** Phase 1 is about 7–9 line-days across five lines (ESTIMATE). Phase 0 is cheap and fixes the placard's wrong EXIT before players see C22.
