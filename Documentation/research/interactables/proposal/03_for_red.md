# For Red: the doors + windows proposal (phase 1)

2026-10-03. Nothing here is in the game. You confirm or change it; then we build.

## Where to look

- **Figma:** "Undergoing Game Projects", section **FRONTROOMS · DOORS + WINDOWS · PROPOSAL**: https://www.figma.com/design/0tCbAiVUlrPId3RWd9LRif?node-id=2497-3804. It now sits under TOUCH CONTROLS.
- **12 slides show, numbered 01–13.** Slide 02 (RE8) is hidden because it has no RE8 pictures yet (see Downloads).
- **Reading order:**
  - 01: today's faults (see-through slits, a one-line lock, a glowing key cube, a one-box window);
  - 03: the stop that closes every gap, in game;
  - 04–06: which door is which (the film's door, the 6/12/20 m test, the 8 doors);
  - 07–08: the lock and the key;
  - 09–10: the window and its glass pocket;
  - 11: what changes in the game;
  - **12–13: your calls.**

## Please confirm or change (slide 12)

| # | Call | Default | Or |
|---|---|---|---|
| 1 | Which doors lock | About 1 door in 3, fixed per seed | Only doors leading deeper, or only doors into Office |
| 2 | Which way doors open | Into the Standard room. So escaping into a Low room means pulling | A hash per door |
| 3 | Pulling a door | You step back 0.45 m (60° if blocked). Escaping from that side is slower | A push-only rule |
| 4 | The lock | Deadbolt + knob (the sliding bolt you asked for) | A storeroom knob with the key in the knob |
| 5 | Unlocked, then shut again | It still looks locked but opens on E | It never shuts fully; it rests ajar at 10° |
| 6 | Lobby glass | Clear: you see the hall and the Relay | Hammered glass in some Lobby windows hides them |
| 7 | Office blinds | Raised 1-inch blinds, one bent slat each | All slats straight, or no blinds |
| 8 | The LOD change | Approve it: every model gets LOD0, 1 and 2 | Small parts ship with no LODs (more draw calls) |

## Later, and already set (slide 13)

- **Call 9, peek window:** none by default. The option is a wired-glass slot in Run doors.
- **Call 10, EXIT letters:** red by default. The option is green.
- **7 small defaults:** Run plain glass; Exit breaks the same; a lever on free doors (the film's door has a knob); EMPLOYEES ONLY on both faces; item glint off; ray-traced glass on the visible pane; bronze frames would reflect white until the ray tracer reads colour. Say if any should change.

## How to read the pictures

- **Film and tape frames** (A24, Kane Pixels) are real IP material, from the downloads you approved on Oct 2.
- **In-game frames** are our own test captures. "Mock-up" means simple boxes at real size, not the models.
- **"WIP"** marks an early Blender render of a built model, in the measured game colours (dark bronze steel, walnut, almond). It shows the frame and leaf only. Locks, signs and plates come in phase 2.
- **Grey "PHASE 2" boxes** are renders still to come. **"AWAITING OK"** boxes need your download OK.
- **RE8:** we copy what its pictures show: a deep dark surround, a thick leaf, a threshold, and no light at the edges. How RE8 stops its gaps is not confirmed. Our real stops are your Option A.

## Downloads (one batch, needs your OK)

Nothing was downloaded.
- **First: C1 and C2.** C1 is one Steam screenshot (about 0.4 MB). C2 is three walkthrough stills (about 0.3 MB each; third-person camera). Together they unhide slide 02.
- **Then:** C8 (a 1950s–80s motel key), C10 (Sears 1993 blinds) and C11 (a 1982 glass-stop patent), about 0.3–0.5 MB each.
- The full list is in `media_candidates.md`.

## Needs other chats

- **Map chat:**
  - one OK for the 16 mm window stop band (slide 10 shows the plain fallback if they say no);
  - the Option A work: one swing side, the pull step-back, a lock flag, keys on hosts.
- **A name clash to fix before phase 2:** the key rack was named `Kit_KeyBoard`. On a Mac that is the same file as the computer keyboard `Kit_Keyboard`. The new name is `Kit_KeyRack`.

## What phase 2 adds

1. **Final renders in every grey and WIP slot:** 8 door sets, 7 lock parts, key sets, 4 windows, the blind and the EXIT sign.
2. **In-game tests in a private copy:**
   - gaps on the real meshes;
   - the swing;
   - the head-dip unlock with the real lock;
   - the 6/12/20 m test with the real models in Lobby, Office, Run and Exit.
3. **Equal-scale front views of every new model**, on 平面视觉's three-view section. Do you want them on this deck too? If so, we add a slide.
4. **Your approved pictures in their slots,** and slide 02 shown.
5. **Era-table rows:** the veneer office door and the storeroom door, 1960–2000.

After that, and only after your OK, the map chat and the visual chat build it into the game.
