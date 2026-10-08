# 02 — Game research: walk up, inspect, close-up, pause

Status: **v1, 2026-10-07.**

**Red's ask (translated):**
- Every visual asset the player can really look at, such as the evacuation plan, should be walk-up-able and interactable, with a close-up shot.
- While the player looks at it up close, the game pauses.

**This file answers one question:** how shipped games do "walk up, interact, close-up, paused" for readable or inspectable objects. It ends with the patterns that fit FrontRooms (§6).

Companion files in this folder:
- `01_inventory.md`: which FrontRooms assets qualify.
- `03_code_survey.md`: the code path, the pause and the camera-rig limits. It is not repeated here.

**What was done (no downloads):**
- **On-disk media first.** The Exit 8, Dark Deception, Lethal Company and the Alien: Isolation iOS still (`Research/week0*`), plus our placard renders → frames `images/r_01`–`r_06`.
- **Official Steam trailers, measured.** Six trailers were viewed in the browser pane and stepped frame by frame for timings (M1–M6, ledger in `SOURCES.md` §2). Nothing was saved.
- **163 official Steam store screenshots** of 18 games were viewed as thumbnails to pick stills for `media_candidates.md`.
- **About 45 web sources** (§9).

**Tags**
- `[S#]`: the source page was opened and read (§9).
- `[search]`: a search-engine summary only; the page was not opened.
- `[M#]`: measured by me from an official trailer (SOURCES.md §2).
- `[D#]`: media on disk (frames, §8).
- `[PLAY]`: my memory of play or footage. **UNVERIFIED**; listed for checking in `media_candidates.md` §C.

---

## 0. The short version

1. **Red's request is a known pattern.**
   - Silent Hill 2 (2024) does it for a framed photo on a wall. The player walks up in third person; a **hard cut** goes to a square-on fixed close-up lit by the player's own flashlight, with tiny prompts in the corner. The hold was ≈ 3 s in the trailer; a hard cut goes back [M4].
   - Of everything measured, it is the closest match to "walk up to the evacuation plan and get a close-up shot".
2. **Games split into two families on time.**
   - **Live:** RE7's inventory [S1], Alien: Isolation's terminals, crafting and save animations [S22], Dead Space's RIG menus [S25], The Exit 8 [S36], P.T. [S33].
   - **Paused:** RE Village's inventory [S5]; Amnesia: The Bunker's notes and inventory on Normal [S15, M1].
   - **The Bunker makes it a setting.** Shell Shock mode turns the pause off [S15, M2].
3. **Speed: fast or nothing.** Measured entries are short:
   - Bunker journal: ≤ 0.2 s [M1].
   - Bunker inventory: ≤ 0.05 s [M2].
   - Gone Home note lift: ≈ 0.25 s plus a 0.15 s settle [M3].
   - SH2 (2024): a cut [M4].
   - Our built Unlock shot already travels 0.35–0.55 s with a cubic ease (`FrontRoomsShotTimings.Unlock`). So 0.40 s in and 0.25 s out sits at the slow end of the references, which suits a heavy, period camera.
4. **Framing: 5 families** (`r_08`):
   - read in place (walk or zoom);
   - a device held in the hand;
   - the object brought to the camera;
   - the camera goes to the object;
   - a 2D page or menu.

   For a **wall-mounted** print like the placard, only "camera goes to the object" keeps the real frame, lens, wallpaper, lamp light and the glow ink. Every 2D page in the set is a period look of its own (Bunker parchment, Signalis CRT, Dead Space hologram). FrontRooms' period object *is* the printed sheet.
5. **Why a pause is fine for FrontRooms.**
   - Pausing games make reading safe but cost something else: RE2's play clock keeps running in the map and inventory [S10]; The Bunker offers a no-pause mode [S15].
   - Live games make the reading the danger: Alien players open every file at the terminal and read them later from the menu [S22].
   - FrontRooms already has an Esc pause, so a paused close-up adds no new "time-out". It only adds **reading while frozen**. The fix is to gate the entry, not to make the reading dangerous:
     - no close-up while the Relay sees you, holds lock-on or chases;
     - the world resumes the moment the camera starts back;
     - nothing regenerates while frozen.
6. **Readability: no transcript by default.** The aids that fit 1990 are a closer camera, real light and one zoom step:
   - At 2 m the placard frame is about 160 px wide at 1080p (`r_06`).
   - Its title reaches a 10 px cap height only at about 1.0 m.
   - At the proposed 0.31 m close-up, the sheet gets 3.0 px/mm at 1080p (`03_code_survey.md` §11).
   - SH2 (2024) shows that a transcript is still expected as an **option**: enlargeable transcript text for notes and books [S12]. Route it to 平面视觉.
7. **Input in the references is one verb in and one verb out.**
   - P.T.'s whole game is walk + zoom (R3) [S33, S34].
   - Gone Home: click to pick up, click to drop [S29].
   - RE: examine and rotate in the inventory [S8, S9].
   - Rotation matters only for hand-sized objects (keys, tags). Wall prints get a small pan, not a rotate.
8. **The one Unity source in the set** (Gone Home) puts the pause in the input context. Each context declares "whether the simulation is paused, whether this context changes the player's mouse sensitivity or FOV" [S27]. That is the same shape as `03`'s proposal (`Phase.Inspect` + `timeScale 0` + an `Inspect` rig shot).

---

## 1. Method and limits

- **Steam trailer stepping.** I played each official HLS stream in the browser pane, seeked by script, drew frames onto a canvas, and looked at the pane. Accuracy: ±0.05 s where 0.05 s steps were used. Trailers are edited: a cut in a trailer can be an editor's cut.
  - **Trusted:** the Bunker demo (one continuous take of play), the Gone Home door note and the SH2 photo (continuous walk-up, close-up and return).
  - **Treated as trailer edits (not game timing):** the Signalis rows.
- **Fandom wikis and Steam threads are community sources.** They are used where a primary source was not found, and named as such.
- **Not reached:**
  - YouTube age-restricted videos (the RE Village Maiden demo) need a sign-in. I did not sign in.
  - TV Tropes and Giant Bomb returned 403.
  - No developer statement was found on why RE Village's inventory pauses when RE7's did not.

---

## 2. All games in one table

`Shot` = how the readable thing is framed. `World` = what time does while you read.

| Game (Steam release) | Shot | Entry → view | World while reading | Main readability aid | What stops it being a safe room |
|---|---|---|---|---|---|
| Resident Evil 7 (2017-01-23) | 3D item in the inventory screen; rotate to find hidden details [S2, S3] | UNVERIFIED | **Live**: the inventory "does not pause game-play" [S1] | Rotate the 3D item [S2] | The world never stops [S1, S4] |
| Resident Evil Village (2021-05-06) | 3D item; rotate (ring until the eye faces you) [S6] | UNVERIFIED | **Paused**: inventory "does pause gameplay itself" [S5] | Rotate; description text | Nothing found. A fan mod removes the pause [S4] |
| RE2 / RE3 / RE4 remakes (2019–2023) | 3D item, "examine from different angles" [S8]; RE4R rotates items Q/E [S9]; wall map boards feed the map screen [S11, PLAY] | UNVERIFIED | Action freezes [PLAY], but **the play clock runs** in map and inventory [S10] | The map marks unpicked items and locked doors [S11] | Time spent reading still counts against the run [S10] |
| Silent Hill 2 (2001) | 2D paper map with James's red pen marks [S13, S14]; memo pages [PLAY] | UNVERIFIED | Paused [PLAY] | Auto-annotated map legend [S13] | Must own the area's map first [S14] |
| Silent Hill 2 (2024-10-07) | **Camera cut to a fixed close-up of a wall photo** [M4]; puzzle close-ups [M4] | cut ≤ 0.1 s in; cut out, then ≈ 0.8 s camera re-settle [M4] | UNVERIFIED | The flashlight lights the object; enlargeable transcript [S12]; High Contrast Mode [S12] | UNVERIFIED |
| Amnesia: The Dark Descent (2010-09-08) | Note text on a dark page; filed in the Journal [S16, PLAY] | UNVERIFIED | Paused [PLAY] | Mementos summarise notes (M key) [S20] | A cut AMfP mode disabled pause entirely [S19] |
| Amnesia: The Bunker (2023-06-06) | Journal or inventory **panel on the right half; the hand and room stay on the left** [M1, M2] | ≤ 0.2 s (hand leads, then the panel cuts in) [M1]; ≤ 0.05 s [M2] | **Normal: paused** (hand frozen 14 s) [M1, S15]. **Shell Shock: live** [S15, M2] | Typed parchment page [M1] | Difficulty setting [S15]; holding the inventory key shows health without pausing [S17]; the timer counts in the inventory [S18] |
| Outlast (2013) / Outlast 2 (2017) | Blue "CONFIDENTIAL" folders → notes [S21]; O2: photograph a document with the camcorder, read it later [S21] | UNVERIFIED | UNVERIFIED | Camcorder-made notes [S21] | O2: reading is deferred, capture is the action [S21] |
| Alien: Isolation (2014-10-06) | Device in hand (tracker) [D4]; terminals [S22] | UNVERIFIED | **Live**: "Crafting, terminals, rewire stations and the animation part of save stations are all on active game time" [S22] | Tracker in hand [D4] | Everything at a device is dangerous [S22]; the 3 s save [S23]. iOS adds a **Radial Menu Pause** option [S24] |
| SOMA (2015-09-21) | Physical grab with weight lag [S44]; in-world terminals [PLAY] | n/a | Live [PLAY] | n/a | Grip on Safe Mode: tense "despite knowing I couldn't get killed" [S43] |
| Gone Home (2013-08-15) | **Object lifted to the camera, room visible behind** [M3, store #1/#3/#7] | lift ≈ 0.25 s + settle 0.15 s [M3] | No threat. The pause is a per-context flag [S27] | "Look Closer" examine and spin, zoom on small text (iOS) [S30] | n/a (no threat) |
| What Remains of Edith Finch (2017) | The readable opens a playable vignette; text drawn into the world [PLAY]; framed comic page [store #4] | n/a | n/a | Text in 3D space [PLAY] | n/a |
| Firewatch (2016) | **Paper map + compass held in both hands, live view** [S31, store #4] | UNVERIFIED | Live [store #4, PLAY] | The "you are here" dot can be switched off [S32] | No threat; the map must be held up to be read [S32] |
| Dead Space (2023) | RIG hologram menus; camera fixed while you navigate [S25]; text holograms in the world [M6] | n/a | **Live**: "in real time without pausing the action" [S25] | No HUD; diegetic UI [S25] | Live menus; pause kept as the only safe moment [S26] |
| Control (2019) | Full-screen document view [PLAY]; text, photos, video, audio collectibles [S40] | UNVERIFIED | Paused [PLAY] | Redaction bars as style [PLAY] | Reviewers call document pickups "reading breaks" in the sequel [S42] |
| Visage (2020-10-29) | Two items in the hands, five in the inventory [S39]; held items with prompts [store #9] | n/a | UNVERIFIED | Light sources [S39] | Darkness drains sanity [S39] |
| P.T. (2014) | **Read in place with a zoom**: "The only actions … are walking and zooming" [S33] | zoom (R3) [S34] | Live | Zoom; wall text [S34] | Zooming is itself a scare trigger: hold the zoom on the bathroom door "for a scary surprise" [S35] |
| SIGNALIS (2022-10-27) | CRT-styled inventory with INSPECT [store #2]; first-person close-ups of panels and drawers [M5, store #8/#9]; 3D documents on black [M5] | trailer edits [M5] | UNVERIFIED | White box on interactables [S38] | Clues are "fleeting" and must be noted by hand [S38] |
| The Exit 8 (2023-11-29) | **Read in place by walking up**: the rules sign "on the wall next to the player" [S36, D1]; posters are the anomaly test [S36, D2] | none | Live | Big, high-contrast signage [D1] | Reading *is* the game: no prompt, no pause [S36] |
| *Extra:* Lethal Company (on disk) | Camera locked to the terminal CRT; "Quit terminal: [ESC]" on screen [D3] | n/a | Live (co-op) [D3] | Green CRT text, full screen [D3] | Co-op: no pause possible |
| *Extra:* Dark Deception (our primary reference) | HUD map always on [D5] | n/a | Live [D5] | Room counter + map [D5] | Map never stops the chase [D5] |

---

## 3. Game by game

Each entry gives:
- **Shot**: how it is framed.
- **Time**: transition time and easing.
- **World**: whether the world pauses, and why.
- **Input**: enter, exit, rotate, zoom.
- **Aids**: readability aids.
- **Not a safe room**: what keeps it from becoming one.

### 3.1 Resident Evil 7 (Capcom, 2017)
- **Shot.** Examine happens on a 3D model inside the inventory screen. Items "can be examined in the inventory, which may reveal a hidden purpose or detail" [S2]. Pre-release coverage: you "rotate them to try and find something hidden, and also potentially open the item" [S3, search].
- **Time.** UNVERIFIED (no official footage checked).
- **World.** "Inventory is accessed in real-time and does not pause game-play" [S1].
  - Why: a modder's comment says RE7 "used the non-pausing inventory to create tension" [S4, search]. That is a player's view; no Capcom statement was found.
- **Input.** PC: Tab opens or closes the menu, F selects, Q/E switch menus [S1]. Rotate is with the mouse or right stick [PLAY].
- **Aids.** The Genome Codex watch is brought into view in the inventory to show health [S1]: even the status screen is a diegetic object.
- **Not a safe room.** The world never stops. Watching a VHS tape in a VCR becomes a playable flashback [S2]: a readable that turns into play, not a pause.

### 3.2 Resident Evil Village (Capcom, 2021)
- **Shot.** The same RE Engine examine. The Maiden demo's eye ring is rotated in the inventory until the eye faces the camera, then removed [S6, search].
- **Time.** UNVERIFIED. The one full demo upload found is age-restricted (`media_candidates.md` §C).
- **World.** "Opening the inventory does pause gameplay itself" [S5]. That is the reverse of RE7. A Nexus mod ("No pause on inventory") puts RE7's behaviour back [S4, search].
- **Input.** PlayStation: Cross = examine/pick up, Triangle = inventory, Touch pad = map/journal [S7].
- **Aids.** Rotation; item text.
- **Not a safe room.** Nothing specific found. The pause is simply accepted.

### 3.3 Resident Evil 2 / 3 / 4 remakes (Capcom, 2019 / 2020 / 2023)
- **Shot.** RE3R's own tutorial file: "You can examine an item from different angles by selecting it and using the Examine command. You might reveal something that helps you progress" [S8]. RE4R rotates items with Q/E (L1/R1) [S9].
  - RE2R's map screen "takes note of the items you don't pick up along the way as well as highlighting locked doors" [S11].
  - Area maps are taken from map boards in the world [PLAY].
- **Time.** UNVERIFIED.
- **World.** The action freezes in the map and inventory [PLAY]. But the Esc pause "yes [stops the timer], but map and inventory don't" [S10]. **Reading is safe from enemies but not free for the run's time.**
- **Input.** Tab or I = inventory, M = map on PC [S47, search]. Examine = a menu command [S8].
- **Not a safe room.** The clock cost [S10].

### 3.4 Silent Hill 2 (Konami, 2001) and Silent Hill 2 (Bloober Team, 2024)
- **2001 shot.** A 2D paper map with red pen marks James adds himself: a zig-zag for blocked, an arrow for accessible, a circle for the objective [S13]. The map button only works "if James has a map in his possession" [S14, search]. In the labyrinth he must draw his own [S13]. Memos are 2D pages [PLAY].
- **2001 world.** Paused [PLAY].
- **2024 shot, measured [M4].**
  - At 370.5 s James stands at a framed photo, over the shoulder, with the flashlight on it.
  - At 370.6 s a **hard cut** goes to a square-on fixed camera. The frame fills about 50 % of the width, warm flashlight hot-spot, two small prompts bottom-right.
  - Hold ≈ 2.8 s in the trailer.
  - At 373.4→373.5 s a hard cut goes back over the shoulder. The gameplay camera then settles for ≈ 0.8 s.
  - The same trailer has a top-down puzzle close-up of a floor drain with a cursor at 618–620 s.
- **2024 world.** UNVERIFIED (a still close-up cannot show whether time stops).
- **2024 aids.**
  - "Books, notes, and collectibles have adjustable transcript text that can be enlarged significantly" [S12].
  - Maps update themselves and mark locked doors and safe rooms [S12].
  - High Contrast Mode [S12].
  - "Players will still be checking their map or inventory while running around town" [S12].
- **Lesson.** The close-up uses **world light** (the flashlight), not a UI light, and a **cut**, not a dolly. A cut is also the accessibility fallback our rig needs at Camera motion Off (`03_code_survey.md` §0.5).

### 3.5 Amnesia: The Dark Descent (Frictional, 2010)
- **Shot.** Found notes are filed in the Journal, "a UI element where all found documents are stored" [S16]. Reading certain notes adds a memento (a hint), opened with M [S20, search]. The note view is a text page over a dark backdrop [PLAY].
- **World.** Paused [PLAY].
- **Interaction.** The rest of the game is physical: the mouse imitates door and lever motion (audit S8 via [S45]).
- **History.** A cut *A Machine for Pigs* mode had "pause functionality … disabled", with "safe areas" instead. It was possibly recycled into the Dark Descent's Hard Mode [S19, search snippet].

### 3.6 Amnesia: The Bunker (Frictional, 2023)
- **Shot, measured [M1]: a note pinned to a wall.**
  - 258.6 s: the hand rises toward it.
  - 258.7 s: the hand is in frame.
  - 258.8 s: the journal panel is fully open. No fade; the hand leads the cut.
  - The page is typed text on parchment, in a panel on the **right half**. The hand stays on the left over the dim room.
  - Read 258.8–273 s; closed with a cut by 274 s.
- **World, measured.** The hand pose is **identical from 262 to 273 s**, which is what a pause looks like [M1]. The wiki confirms that easier modes pause notes and the inventory [S15].
- **Shell Shock (v1.70, 2023-10-25)** [S15]:
  - "Checking inventory and reading notes do not pause the game as they did in easier difficulties … the player must check these things while enemies are not around."
  - Director Frederik Olson pitched it as "no time to rest" [S15].
  - Its trailer shows the inventory panel popping in within 0.05 s (58.10→58.15 s) under the card "NO PAUSING IN INVENTORY" [M2].
- **Not a safe room.**
  - The mode switch.
  - **Holding** the inventory key "check[s] your health state without actually pausing the game"; a tap opens the paused inventory [S17, search].
  - The achievement timer keeps counting in the inventory [S18, search].
- **Lesson.** One game can ship **both**: paused by default, live as an opt-in. Tap and hold can split "glance" (live) from "read" (paused).

### 3.7 Outlast (Red Barrels, 2013) and Outlast 2 (2017)
- **Shot.** Documents are blue folders stamped "CONFIDENTIAL" [S21]. They are read in the notes screen [PLAY].
- **Outlast 2.** Blake "takes photos of retrieved documents and stores them in his Camcorder for later viewing" [S21]. **Capturing is the in-world action; reading is deferred to a menu.**
- **World.** UNVERIFIED for both.
- **Lesson for us.** Deferred reading (a journal) removes the reason to stand at the object. FrontRooms should **not** copy the placard into a notes screen (§6, P9).

### 3.8 Alien: Isolation (Creative Assembly, 2014)
- **World.** On a 2015 Steam thread a player answered: "the only time the game is paused is when you are in the actual game menu. Maybe in the map screen too … Crafting, terminals, rewire stations and the animation part of save stations are all on active game time" [S22]. The asker's own tactic: "access all the files, then read them afterwards" [S22].
  - So a live terminal makes players **defer** reading. The danger moves the reading out of the world.
- **Shot.**
  - The motion tracker is raised in the hand in the live view [D4, store #3].
  - Terminals are framed in the world; on iOS they "become direct touchscreens" [S24].
  - The save takes a key card and 3 s, during which you can be killed (audit S1, S3 via [S23]).
- **Input (iOS).** A **Radial Menu Pause** option pauses the game while the weapon/equipment radial is open [S24]: an accessibility pause offered on mobile.
- **Lesson.** Live reading works when the readable *is* a device you operate. For a printed placard the device argument does not apply.

### 3.9 SOMA (Frictional, 2015)
- **Shot.** Objects are grabbed physically; the object lags the cursor, which reads as weight (PC Gamer via [S44]). Terminals and screens are used in the world [PLAY]. The omnitool is held in hand [store #1].
- **World.** Live [PLAY].
- **Lesson on pause and tension.** Thomas Grip on SOMA's monster-free Safe Mode: "I was surprised how tense it all felt despite knowing I couldn't get killed" [S43]. A paused, threat-free moment can still be tense if light and sound stay in character.

### 3.10 Gone Home (Fullbright, 2013)
- **Shot, measured [M3].** The note taped to the front door:
  - The player walks up (21.0–21.8 s).
  - The note is lifted off the door to the camera between 21.90 and 22.15 s (≈ 0.25 s, motion-blurred mid-way) and settles by ≈ 22.30 s.
  - It fills ≈ 45 % of the width. The door glass stays visible, slightly darker.
  - Store screenshots #1, #3 and #7 show the same grammar: a paper at the camera, the room unblurred behind.
- **World.** No threat. Gone Home's input system (Unity) gives every context a flag for "whether the simulation is paused" and for "whether this context changes the player's mouse sensitivity or FOV". The contexts shown are Default, Examine, Journal and Map [S27].
- **Input.**
  - "Left-click to interact … left-click again to drop a held object"; zoom and examine are taught by in-game text [S29, search].
  - iOS adds a **Look Closer** button: "examine an item you're holding and spin it around", and zoom in on small text [S30, search].
  - Interactivity "rests upon looking at objects and notes" [S28].
- **Lesson.** The pause and FOV belong to the input context. Gone Home's lift is ≈ 0.25 s with an ease-out.

### 3.11 What Remains of Edith Finch (Giant Sparrow, 2017)
- **Shot.** A readable (a letter, a comic) opens a playable story; text is drawn into the 3D world [PLAY]. Store #4 shows a story framed inside a comic page.
- **World.** n/a (no threat).
- **Lesson.** The readable can *be* the scene. Not needed for the placard; noted for narrative.

### 3.12 Firewatch (Campo Santo, 2016)
- **Shot.** "The game map is a physical item Henry manipulates"; Henry adds information to it [S31]. A reviewer: he "has to physically hold [it] in front of him to even see where he is"; the location dot can be turned off [S32, search]. Store #4: map and compass in both hands, sunlit, full world behind.
- **World.** Live [store #4; walking while holding it: PLAY].
- **Aids.** Campo Santo released printable maps and suggests turning off the "you are here" dot for that play [S32, search].
- **Lesson.** A handheld map in a live world is a strong 1990-compatible image. It fits a **carried** item (a key tag), not a wall print.

### 3.13 Dead Space (Motive, 2023)
- **Shot.** "A context menu appears as a hologram in front of you in real time without pausing the action … your camera fixed in position while you frantically navigate through the menus." There is no overlaid HUD [S25]. Text holograms in the world are read while the room's steam vents keep firing [M6].
- **World.** Live menus. Pause kept.
- **Why** (Joel MacMillan, realization director): "In horror games, you can pause and you have a moment of safety there … removing that as a feature would make horror games far more impactful … So I do like to have a pause button, personally" [S26].
- **Lesson.** Even the most hardline live-menu remake kept a pause, because players need one. Red's paused close-up follows the same logic: one deliberate safe moment, bought by walking to the object.

### 3.14 Control (Remedy, 2019)
- **Shot.** Collectibles are "text documents, photographs, videos, and audio files" [S40]. Reading opens a full-screen document view with redaction bars [PLAY].
- **World.** Paused [PLAY].
- **Reception.**
  - The documents read as research "that could conceivably be left out" rather than placed paperwork [S41, search].
  - The sequel was criticised for pickups that "regularly prompt reading breaks" [S42, search].
- **Lesson.** Volume turns reading into a chore. FrontRooms should keep close-ups **rare and short** (the placard is 1 per start door).

### 3.15 Visage (SadSquare, 2020)
- **Shot.** "Can store only five … items at a time and hold two more in his hands" [S39]. Store #9: an item held in hand with button prompts along the bottom.
- **World.** UNVERIFIED.
- **Systems.** Darkness drains sanity; light bulbs repair lights; candles are a light ghosts cannot touch [S39]. Critics found the two-handed juggling "laborious and cumbersome" [S39].
- **Lesson.** Light as the gate to reading fits our glow placard: it reads best when its lamp fails.

### 3.16 P.T. (Kojima Productions, 2014)
- **Shot.** "The only actions the player can use are walking and zooming" [S33].
  - R3 zooms on wall text ("Hello" above the phone) [S34, search].
  - Photo pieces are collected by looking and pressing R3 [S35, search].
  - At the bathroom door: "Peek inside and press R3 to zoom. Hold it long enough for a scary surprise" [S35, search].
- **World.** Live.
- **Lesson.** **The zoom is itself the bait.** P.T. uses the moment of looking as the scare's trigger. Our pause promise forbids that inside the close-up (§6, P7). It is still allowed *after* it.

### 3.17 SIGNALIS (rose-engine, 2022)
- **Shot.**
  - The inventory is a CRT-styled device with USE / COMBINE / INSPECT [store #2].
  - Puzzle objects get first-person close-ups (a power panel, an open drawer) [M5, store #8/#9].
  - Documents are 3D paper turning on black [M5].
- **Input.** "Press Tab to open the inventory screen at any time" [S37].
- **World.** UNVERIFIED. A tuned radio keeps playing after the inventory closes [S38, search].
- **Aids.** Interactables get a white box in range [S38, search].
- **Lesson.** A 1990-adjacent retro look can carry its UI as a device. FrontRooms has no device; the print is the UI.

### 3.18 The Exit 8 (KOTAKE CREATE, 2023)
- **Shot.** "The rules are displayed on a sign on the wall next to the player." The player only walks, and anomalies include "repeating poster designs" [S36].
  - Frames `r_01`/`r_02`: the rules sign is read by walking past it in ≈ 4.5 s.
  - Posters are judged at walking pace while a passer-by walks by [D1, D2].
- **World.** Live. No prompt, no zoom [S36: none described].
- **Lesson.** Reading in place works when the type is big and high-contrast and the text is short. The Exit 8's guide sign is legible from ≈ 1–2 m in `r_01`. Our placard title reaches 10 px at 1080p only at ≈ 1.0 m, and its plan and legend never get there without a close-up (`r_06`).

### 3.19 Extras on disk
- **Lethal Company terminal** [D3]: the camera is locked onto the CRT; the text fills the view; "Quit terminal : [ESC]" is printed on screen. Co-op means the world cannot pause. A camera-to-screen move with a single exit key.
- **Dark Deception** [D5]: the map is a permanent HUD card with a room counter. It never pauses: our primary reference keeps reading *inside* the chase. FrontRooms keeps the map in the world, so the placard is our "map card".

---

## 4. Cross-game findings, with numbers

### 4.1 Framing (five families, `r_08`)

| Family | Who | Keeps world light? | Fits FrontRooms for |
|---|---|---|---|
| Read in place (walk or zoom) | The Exit 8, P.T., Dark Deception | yes | **Titles and short signs** readable at ≥ 1 m |
| Device in hand | Firewatch, Alien tracker, Dead Space RIG, Visage, SOMA | yes | Not now (no device in our fiction) |
| Object brought to the camera | RE examine, Gone Home, Signalis documents | Gone Home: yes. RE and Signalis: no (dark backdrop) | **Hand-sized props**: the zone key and tag, M4 in `01_inventory.md` |
| Camera goes to the object | SH2 (2024), Alien terminals, Lethal Company, Signalis close-ups | yes | **Wall prints and fixtures**: placard, door sign, plate, clock |
| 2D page or menu | Amnesia, Bunker, Outlast, Control, SH2 2001, RE files | no | Not for 1990 FrontRooms (§6, P10) |

### 4.2 Timing
- Entry ranges from a hard cut (SH2 2024, Bunker) to a 0.25 s lift (Gone Home) [M1–M4]. Nothing measured exceeds 0.25 s for the object itself.
- SH2 2024 hides its cut-back with an ≈ 0.8 s camera re-settle [M4].
- The Bunker hides its cut with the hand animation (hand first, panel 0.1–0.2 s later) [M1].
- Our built shot language is slower and continuous: Unlock travel 0.35–0.55 s with a cubic ease in-out, return 0.40 s, a cancel return of 0.25 s (`FrontRoomsShotTimings.Unlock`).
- Changes of FOV should ride the same curve. Camera guidance lists rapid FOV shifts among common mistakes (audit S16). The shared rule is `MinFovChangeSeconds .4`.

### 4.3 Time and pause
- **Live:** 6 from sources (RE7, Alien, Dead Space, The Exit 8, P.T., Bunker Shell Shock), plus Lethal Company (co-op, so it cannot pause).
- **Paused:** 2 sourced or measured (RE Village, Bunker Normal), plus 5 from play memory.
- **Option:** 2 (Bunker modes, Alien iOS radial pause).
- **Reasons given in sources:**
  - live = tension (RE7 per a player [S4]; Bunker Shell Shock "no time to rest" [S15]);
  - pause = "a moment of safety", which a developer chose to keep [S26].
- **Cost when paused:** RE2's clock keeps counting [S10]; the Bunker's achievement timer counts in the inventory [S18].
- **Player behaviour when live:** files are deferred to a safe moment [S22].

### 4.4 Input
- **Enter:** the normal use button everywhere (E, Cross, F, a click, R3 for zoom).
- **Exit:** the same button, a back button, or Esc (Lethal Company prints "[ESC]") [D3].
- **Rotate:** only in item views (RE, Gone Home, Signalis).
- **Zoom:** one extra step (P.T. R3, Gone Home Look Closer).
- **Hold vs tap:** the Bunker splits hold = live glance, tap = paused read [S17]. Accessibility guidance asks for alternatives to holds (audit S20).

### 4.5 Readability aids seen
- Real light on the object (SH2 2024 flashlight, Gone Home room light).
- One zoom step (P.T., Gone Home).
- An optional, enlargeable transcript (SH2 2024).
- High contrast (SH2 2024).
- Self-annotating maps (SH2 2001 and 2024, RE2R).
- A dark backdrop (RE, Signalis).

No game in the set uses depth of field on a 2D page. The 3D views either dim (Gone Home) or black out (Signalis) the background.

---

## 5. What this means for the placard (numbers)

| Quantity | Value | From |
|---|---|---|
| Frame width on screen at 2 m, FOV 76, 1080p | ≈ 160 px | `r_06`; 466.8 mm / (2 × 2 m × 1.389) × 1920 |
| Title cap height | ≈ 14.5 mm on the sheet | measured on `q16_u1_A3_legend_0p6m.jpg` (37 px cap / 2.55 px per mm) |
| Distance where the title cap reaches 10 px at 1080p | ≈ 1.0 m (6.7 px at 1.5 m) | 1080 / (2 × tan 38° × d) |
| Close-up camera distance | 0.31 m at FOV 61 (76 − 15) | `03_code_survey.md` §11 |
| Screen density at the close-up | 3.0 px/mm at 1080p, 3.9 at 1440p | same |
| Legend label "AS PRINTED" | ≈ 4.3 mm cap → ≈ 13 px | measured on the same render × 3.0 |
| Smallest line ("WALLCOVERING SHOWN AT 1/25 SIZE") | ≈ 2.4 mm cap → ≈ 7 px: **below a comfortable read** | same; 平面视觉 to check (the code survey asks for ≈ 3 mm) |

So: the title is read in place (The Exit 8 family). The plan and legend need the close-up (SH2 2024 family). The smallest line needs either 平面视觉's resize or one zoom step to ≈ 0.20 m.

---

## 6. Patterns that fit FrontRooms

Constraints:
- a minimal HUD (one prompt, one threat signal, `UI_SYSTEM.md`);
- the 1990 tone (period objects, no modern UI chrome);
- Relay pursuit (staged warnings, lock-on hold, Chase; `RELAY_PURSUIT_REDESIGN.md` §8–9).

Owners are named per pattern. Map-chat items are contract requests, not edits.

**P1. Two reading distances.**
- Titles and short signs read in place at ≥ 1 m (The Exit 8).
- Plans, legends, plates and tags read in a close-up (SH2 2024).
- Every inspectable needs a title that reads at ≈ 1 m, so the player knows it is worth walking to.
- Owner: 平面视觉 (sizes), visual chat (check renders at 1.0 m and 2.0 m).

**P2. The camera goes to wall objects; hand objects come to the camera.**
- *Wall-mounted:* placard, door sign, number plate, clock. The camera dollies to a square-on pose at 0.31 m, FOV 76 → 61 on the same curve. The world, lamp and glow are what you see.
- *Hand-sized* (the zone key with its tag): it lifts to the camera like Gone Home's note, then flips once to show the tag number.
- No 2D card in either case.
- Owner: visual chat (close poses per kit, `03` §10.3); map chat (`ShotKind.Inspect`, contract).

**P3. Timing.**
- **In:** the Unlock travel rule, 0.35–0.55 s with a cubic ease in-out (≈ 0.40 s from a typical 1.2 m).
- **Out:** 0.25 s ease-out.
- **Camera motion Off:** a hard cut, which SH2 2024 shows reads as deliberate, not cheap.
- No hand clip, so no wait for an animation (the Bunker leads with a hand; we have none).
- Owner: map chat (rig), visual chat (curve review on a capture).

**P4. The pause, as Red asked, with three guards against a safe room.**
1. **Gate the entry.**
   - No inspect prompt while the Relay sees the player, during the lock-on hold, in Chase or in BreakDoor.
   - The prompt simply does not appear. There is no "can't read now" text, which keeps the HUD minimal.
   - Stages 1–2 (lamp dips, steps) still allow it. The director's pressure only counts perceived cues and is frozen during the pause anyway.
2. **Resume early.** World time restarts when the camera *starts* back, not when it lands. Input stays locked for those 0.25 s. At chase speed 4.2 m/s the Relay covers ≈ 1 m, so it is a small, honest cost (the save-station idea from Alien [S23], scaled down).
3. **Nothing pays out while frozen.** `timeScale 0` already freezes stamina, the tier clock, lamp ageing and the glow charge (`03` §3). The caught card's elapsed seconds should not count the pause, matching Esc.

- Owner: map chat (contract: `Phase.Inspect`, `timeScale 0`, prompt gate); Red (rule 2 and the clock rule).

**P5. An optional live mode** for players who want it (The Bunker's Shell Shock).
- A setting "Close-ups pause the game: On (default) / Off".
- Off keeps time running and lets `ShotCancel.Relay` end the close-up the moment the Relay sees you.
- Red decides. Default stays On, as asked.

**P6. Light is the readability aid, not UI.**
- No extra camera light for the placard. It would wash out the glow ink; the code survey reaches the same conclusion (`03` §11).
- The frozen lamp state is honest. A dead lamp means the glow legend reads, which is the narrative point (SH2 2024 lights its close-up only with the player's flashlight; Visage gates on light).
- Post: no depth of field on the sheet. A slight falloff on the wall beyond the frame; exposure unchanged; grain continues (UNVERIFIED under `timeScale 0`, `03` §3).
- Owner: visual chat (post volume, look-dev capture).

**P7. Keep the pause a promise.**
- Nothing scary happens *inside* the close-up. P.T. turns the zoom into bait [S35]; here that would break the meaning of the pause.
- Anything that changed must already have changed before the press. The Relay's staged warnings may resume right after.
- Owner: map chat, sound chat.

**P8. One verb in, one verb out.**
- **Desktop:**
  - E enters; E or S exits. Esc still opens the pause menu (it never cancels a shot, as today).
  - The mouse pans within a ±6° cone (like the glass shot's 5–6°).
  - Optional: scroll or right mouse for one zoom step to ≈ 0.20 m for the smallest print (P.T., Gone Home Look Closer).
  - Rotate only for the key and tag.
- **Touch:** USE enters, a tap or Back exits, drag pans, pinch makes the zoom step.
- Prompt words and the exit hint go through 平面视觉. The one prompt line reads like the door prompts.
- Owner: map chat (desktop), touch session (`Assets/Scripts/Input/*`), 平面视觉 (words).

**P9. No journal for prints.**
- The placard is not copied into a notes screen.
- Alien players deferred reading to a menu [S22], and Outlast 2 moves documents into the camcorder [S21]. Both drain the reason to stand in the room.
- `UI_SYSTEM.md` still lists a 2D-era "`Tab` opens notes / hold `E` reads" mapping. The 3D game has no read path today (`03` §0.1). This research argues against reviving a notes card for 3D.
- Owner: Red (decision), map chat.

**P10. An accessibility transcript, off by default.**
- SH2 2024 shows players expect an enlargeable transcript for notes [S12].
- Offer it as a setting.
- It appears *in* the close-up, in 平面视觉's type, never as the default.
- Owner: 平面视觉 (design), map chat (setting), visual chat (placement against the frame).

**P11. Keep close-ups rare.**
- Control's sequel was faulted for constant "reading breaks" [S42].
- One placard per start door (`placard/10_spec.md` D14), plus the `01_inventory.md` MUST list (5 rows), is the right scale.
- SHOULD props (vending machine, copier) get a close-up only if they carry text worth reading.
- Owner: Red, visual chat.

**P12. Sound** (sound chat decides; no new AudioSources).
- References differ, and none was sourced.
- Proposal to the sound chat: on `ShotStarted(Inspect)` stop gameplay events (Relay steps, door Foley), keep the room's fluorescent tone 10–12 dB down so the pause still sounds like the place (Grip's Safe Mode point [S43]), and give one paper or metal touch event on arrival.
- Hook: `FrontRoomsCameraRig.ShotStarted/ShotEnded` already exist.

---

## 7. Questions for Red

1. P4 rule 2: should the world restart when the camera *starts* back (a small risk), or only when it is home (fully safe)?
2. P5: do you want a "close-ups pause: Off" option for a harder mode, or never?
3. P9: no notes screen for prints. OK?
4. P2: for the key and tag, a lift to the camera (Gone Home) or a camera dip to the hand position (like the Unlock shot)?
5. P11: which SHOULD props deserve a close-up, if any?

---

## 8. Frames (`images/`, JPG q85, credits in `SOURCES.md` §1)

| Frame | Shows | Tag |
|---|---|---|
| `r_01_exit8_rules_walkup.jpg` | The Exit 8: the rules sign read while walking, 4 stills over 4.4 s | D1 |
| `r_02_exit8_posters.jpg` | The Exit 8: posters judged at walking pace with a passer-by | D2 |
| `r_03_lethal_terminal_live.jpg` | Lethal Company: the camera locked to the terminal, live world, "[ESC]" exit | D3 |
| `r_04_alien_tracker_realtime.jpg` | Alien: Isolation (iOS): tracker raised in real time; pause and journal are separate buttons | D4 |
| `r_05_darkdeception_live_map.jpg` | Dark Deception: the HUD map never pauses the chase | D5 |
| `r_06_frontrooms_placard_distance.jpg` | Our placard: ≈ 160 px at 2 m vs readable up close; lamp on vs lamp off (glow legend) | D6 |
| `r_07_measured_transitions.jpg` | Diagram: measured entry and exit times (M1–M4) against our Unlock shot and the proposal | support |
| `r_08_design_space.jpg` | Diagram: the five framing families × live / option / paused, with the FrontRooms proposal | support |

Wanted primary media (Steam stills and clip ranges with measured times, plus what is still missing) are in `media_candidates.md`.

---

## 9. Sources

Read 2026-10-07 unless marked. "browser" = read in the browser pane because the fetch tool was refused.

| # | Source | URL | Supports |
|---|---|---|---|
| S1 | Resident Evil Wiki, "Inventory (RE7)" (fan wiki, unsourced article), browser | https://residentevil.fandom.com/wiki/Inventory_(RE7) | Inventory "accessed in real-time and does not pause game-play"; Tab / F / Q-E; Genome Codex brought into view |
| S2 | Wikipedia, "Resident Evil 7: Biohazard" | https://en.wikipedia.org/wiki/Resident_Evil_7:_Biohazard | Examine reveals hidden purpose or detail; VHS tapes; first-person |
| S3 | Christian Today, RE7 pre-release (2016) [search] | https://www.christiantoday.com/trends/resident-evil-7-biohazard-release-date-news-gameplay-updates-new-trailers-feature-object-interaction-and-in-game-puzzles | Rotate or open items; no pause when examining |
| S4 | Nexus Mods, RE Village mod 661 "No pause on inventory" (page 403; comment via search) [search] | https://www.nexusmods.com/residentevilvillage/mods/661 | RE7's live inventory "created tension"; Village pauses |
| S5 | Resident Evil Wiki, "Inventory (Village)", browser | https://residentevil.fandom.com/wiki/Inventory_(Village) | "Opening the inventory does pause gameplay itself" |
| S6 | GameRevolution, Maiden demo eye key [search] | https://www.gamerevolution.com/?p=672359 | Rotate the ring until the eye faces the camera |
| S7 | Shacknews, RE Village controls | https://shacknews.com/article/124121/resident-evil-village-controls-and-keybindings | Cross examine; Triangle inventory; Touch pad map |
| S8 | Resident Evil Wiki, "Examining Items (RE3 remake)" (in-game file), browser | https://residentevil.fandom.com/wiki/Examining_Items_(RE3_remake) | "Examine an item from different angles … might reveal something" |
| S9 | Resident Evil Wiki, "Inventory (RE4 remake)", browser | https://residentevil.fandom.com/wiki/Inventory_(RE4_remake) | Rotate Q/E, L1/R1; Use / Examine / Combine |
| S10 | Steam Community, RE2 "Does pausing pause the game timer?" (2019-02-16) | https://steamcommunity.com/app/883710/discussions/0/3658515990050869854 | Pause stops the timer; "map and inventory don't" |
| S11 | Push Square, Resident Evil 2 review, browser | https://www.pushsquare.com/reviews/ps4/resident_evil_2 | Map notes unpicked items and locked doors; RE7's inventory and map design reused |
| S12 | Can I Play That?, *Silent Hill 2 Remake accessibility review* (Mike Matlock, 2024-11-28), browser | https://caniplaythat.com/2024/11/28/silent-hill-2-remake-accessibility-review/ | Enlargeable transcripts; auto map; High Contrast; "checking their map or inventory while running around town" |
| S13 | Silent Hill Wiki, "Map", browser | https://silenthill.fandom.com/wiki/Map | Red map marks; labyrinth self-drawn; Shattered Memories phone map in real time |
| S14 | *Silent Hill 2* PS2 US manual (PDF) [search] | https://www.silenthillmemories.net/sh2/versions/silent_hill_2_ps2_us_digital_manual.pdf | Map only if James has one; green pointer |
| S15 | Amnesia Wiki, "Shell Shock mode", browser | https://amnesia.fandom.com/wiki/Shell_Shock_mode | No pausing in Shell Shock; easier modes pause; v1.70 2023-10-25; "no time to rest" |
| S16 | Amnesia Wiki, "Journal (disambiguation)", browser | https://amnesia.fandom.com/wiki/Journal_(disambiguation) | The journal stores all documents; Bunker notes / photos / codes |
| S17 | Steam Community, Amnesia: The Bunker "Inventory does not open" [search] | https://steamcommunity.com/app/1944430/discussions/0/5264192561410039341/ | Holding shows health without pausing |
| S18 | Neoseeker, Amnesia: The Bunker achievement guide [search] | https://www.neoseeker.com/amnesia-the-bunker/Achievement_and_Trophy_Guide | The timer counts in the inventory |
| S19 | Amnesia Wiki, "Amnesia: A Machine for Pigs/Cut content" [search snippet] | https://amnesia.fandom.com/wiki/Amnesia:_A_Machine_for_Pigs/Cut_content | Cut mode with pause disabled and safe areas |
| S20 | Amnesia Wiki, "Mementos" [search] | https://amnesia.fandom.com/wiki/Mementos | Notes create mementos; M key |
| S21 | Outlast Wiki, "Documents", browser | https://outlast.fandom.com/wiki/Documents | Blue CONFIDENTIAL folders; Outlast 2 photographs documents for later |
| S22 | Steam Community, Alien: Isolation "Is the game 'paused' when you access terminals?" (2015-02-28) | https://steamcommunity.com/app/214490/discussions/0/617329150702516350 | Terminals, crafting, rewire, save animation on active time; deferred reading |
| S23 | `research/interaction_audit/04_aaa_references.md` S1 (Wikipedia Alien: Isolation) and S3 (Xbox Wire), read 2026-10-02 | see that file | 3 s key-card save with risk |
| S24 | `research/touch/01_shipped_games.md` (Feral iOS FAQ, App Store reviews), read 2026-10-03 | https://www.feralinteractive.com/en/support/ios/alienisolation/#faqs/ios_new_options | Radial Menu Pause option; terminals as touchscreens |
| S25 | Stuff, *Dead Space (2023) PC review* (Duncan Pike, 2023-01-27) | https://stuff.co.za/?p=159781 | Hologram menus in real time; camera fixed; no HUD |
| S26 | Inverse (Joseph Yaden, 2023-01-11); GamingBolt (same quotes) | https://inverse.com/gaming/dead-space-remake-interview-terrifying-change ; https://gamingbolt.com/dead-space-remake-almost-didnt-have-pause-functionality-developer-reveals | MacMillan on pause as safety; pause kept |
| S27 | Fullbright, "Gone Home's Input System" (Johnnemann, 2013-02-15), browser | https://fullbright.company/2013/02/15/gone-homes-input-system/ | Input contexts flag pause, sensitivity and FOV; Default / Examine / Journal / Map |
| S28 | Wikipedia, "Gone Home" | https://en.wikipedia.org/wiki/Gone_Home | Interactivity rests on looking at objects and notes |
| S29 | Steam Community, Gone Home "Game Controls for Keyboard" [search] | https://steamcommunity.com/app/232430/discussions/1/496880503077089410/ | Left-click pick up, left-click drop; zoom taught in game |
| S30 | App Unwrapper, *Gone Home iOS review* (2018-12-15) [search] | https://www.appunwrapper.com/2018/12/15/gone-home-ios-review/ | "Look Closer": examine, spin, zoom small text |
| S31 | Wikipedia, "Firewatch" | https://en.wikipedia.org/wiki/Firewatch | Map and compass; a physical item Henry manipulates |
| S32 | Firewatch reviews and Campo Santo printable maps [search] | https://www.dualshockers.com/campo-santo-releases-printable-firewatch-maps-for-cooperative-playthrough-or-your-wall/ ; https://mikepennisi.com/blog/2022/review-firewatch/ | Hold the map up to read it; dot can be off; printable maps |
| S33 | Wikipedia, "P.T. (video game)" | https://en.wikipedia.org/wiki/P.T._(video_game) | Only walking and zooming |
| S34 | Push Square, P.T. guide (2014-08) [search] | https://www.pushsquare.com/news/2014/08/guide_how_to_complete_the_spooky_pt_demo_on_ps4 | R3 zoom on wall writing |
| S35 | Prima Games, P.T. guide [search] | https://primagames.com/news/silent-hills-pt-complete-guide-and-walkthrough | R3 to collect photo pieces; hold zoom at the bathroom for a scare |
| S36 | Wikipedia, "The Exit 8" | https://en.wikipedia.org/wiki/The_Exit_8 | Rules sign on the wall; walking; poster anomalies |
| S37 | SIGNALIS Instruction Manual (wiki.gg) | https://signalis.wiki.gg/wiki/Instruction_Manual | Tab opens the inventory at any time; 6 stacks; storage box |
| S38 | SIGNALIS reviews (CogConnected, Gaming Nexus) [search] | https://cogconnected.com/review/signalis-review/ ; https://gamingnexus.com/Article/7735/Signalis | Radio stays on after the inventory closes; white box on interactables; fleeting clues |
| S39 | Wikipedia, "Visage (video game)" | https://en.wikipedia.org/wiki/Visage_(video_game) | 5 + 2 item limit; darkness and sanity; critics on juggling |
| S40 | Control Wiki, "Collectibles", browser | https://control.fandom.com/wiki/Collectibles | Text, photos, video, audio collectibles |
| S41 | PCWorld, *Control review* (Hayden Dingman, 2019-08-26) [search] | https://www.pcworld.com/article/397948/control-review-so-good-you-might-finally-stop-asking-for-alan-wake-2.html | Documents read as research, less distracting |
| S42 | Gameblog, *CONTROL Resonant* review [search] | https://www.gameblog.fr/jeu-video/ed/review/control-resonant-test-ps5-724280 | Frequent document "reading breaks" |
| S43 | Bloody Disgusting, Thomas Grip interview (Mike Wilson, 2017-12-20), browser | https://bloody-disgusting.com/news/3475635/interview-frictional-games-thomas-grip-talks-soma-new-xbox-one-port-exorcist/ | Safe Mode still tense |
| S44 | `interaction_audit/04_aaa_references.md` S27 (PC Gamer, SOMA doors) | see that file | Grab with weight lag |
| S45 | `interaction_audit/04_aaa_references.md` S8 (Wikipedia, Amnesia) | see that file | Mouse imitates doors and levers |
| S46 | Steam Store API `appdetails` (JSON) | https://store.steampowered.com/api/appdetails?appids=<id> | Release dates, screenshot and trailer lists, movie ids |
| S47 | Magic Game World, *Resident Evil 2 PC Keyboard Controls* [search] | https://www.magicgameworld.com/resident-evil-2-pc-keyboard-controls/ | Map M or Ctrl; inventory I or Tab |

Measured trailers M1–M6 and the on-disk media D1–D6 are in `SOURCES.md` §1–2. The audit's camera and accessibility sources (S16 Nesky, S19 and S20 Game Accessibility Guidelines) are cited as "audit S#" from `interaction_audit/04_aaa_references.md`.

---

## 10. Open items and UNVERIFIED

- **World-time behaviour needs a source** for: RE2/3/4 remakes (action freeze), SH2 2001 and 2024, Amnesia: The Dark Descent, Outlast, Control, Visage, SIGNALIS, Gone Home, SOMA. All are marked `[PLAY]` or UNVERIFIED above. `media_candidates.md` §C lists what to capture.
- **No transition times yet** for RE7, RE Village, Alien terminals, Control, Amnesia: The Dark Descent and Firewatch. The Village demo upload is age-restricted.
- **Trailer edits.** M5 (Signalis) is too edited to time. M4's 2.8 s hold is the trailer's, not a game rule.
- **Search summaries.** [search] rows were not opened: S3, S4, S6, S14, S17–S20, S29, S30, S32, S34, S35, S38, S41, S42, S47. Open them before quoting on a slide.
- **Hold-to-glance (S17)** is a single player post.
- **The ≈ 1.0 m title-read distance** is computed from one render's measured cap height. Confirm on a capture of the built placard.
- `UI_SYSTEM.md`'s "hold E reads / Tab opens notes" line looks stale against `03_code_survey.md` §0.1. Flagged for the map chat, not changed here.
