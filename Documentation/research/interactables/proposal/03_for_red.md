# For Red: the doors + windows proposal (phase 1)

**DECIDED 2026-10-21 (Red in the 系统设计 chat, "系统设计这边也全部按推荐（包括更正）"; systems.json 2026-10-21f, A09 = SD04): every call at its default.** Calls 2–8 as below (2 swing into the Standard room; 3 pull step-back 0.45 m; 4 deadbolt + knob; 5 unlocked still looks locked but opens on E; 6 clear Lobby glass; 7 raised 1-inch blinds with one bent slat; 8 LOD0/1/2 for every model). Call 1 (which doors lock) = C15: about 1 in 3 per seed, after the L5 key-reach check. A = the Run window frame stays dark-bronze steel. B = display name **"key rack"** (file stays `Kit_KeyHookBoard`; 平面视觉 relabels K73). Window contract T = yes (drop the map trims under framed windows). C14 = look + E (desktop first). D1.6 may start in a clone now; it lands after the Alpha items (W1 lamps → exit minimum → small door fixes).

2026-10-03. Nothing here is in the game. You confirm or change it; then we build.

## Update 2026-10-14: the kit is built; two calls are still yours

The 57 models are in the project now (not yet placed by the map). The deck shows them: slide 14 "The kit, as built" is new, every grey PHASE 2 box and every WIP tile has its final render, and the chips name the kit files. Slide 11 keeps its Oct 3 text, with a note that points to slide 14. The section moved: it is now at **x 25257, y 13920**, and 13 slides show, numbered 01–14.

Two things on the slides were left as they are, because they are your calls:

| # | Call | What the slides show now | Or |
|---|---|---|---|
| A | **Run window frame** (slide 09, DW08) | The Run window (W-RN) uses the dark-bronze steel frame `Kit_WindowFrame_Steel`, the same as the Run doors (fix pass, critic M2). The white enamel frame `Kit_WindowFrame_Steel_Enamel` is shown as "Run option" | White enamel for Run. The model is built; it has no map member yet |
| B | **Key board name** (slide 08, DW07) | The file is `Kit_KeyHookBoard`. The slide's chip shows that name, and its numeral says "Key rack · 0.30 m". 平面视觉's sheet K73 says "Key board" | Pick "key board" or "key rack" for the slides and the sheet (codex audit F6). The file name can stay |

The `Kit_KeyBoard` / `Kit_Keyboard` clash under "Needs other chats" below is fixed: the file is `Kit_KeyHookBoard`. Only the display name (B) is open.

## Where to look

- **Figma:** "Undergoing Game Projects", section **FRONTROOMS · DOORS + WINDOWS · PROPOSAL (rebuilt 2026-10-07)**: https://www.figma.com/design/0tCbAiVUlrPId3RWd9LRif?node-id=2748-6099. It sits at **x 25257, y 13920**: under ROOM VISUALS, above LEVEL TRANSITIONS, between PROP KIT on the left and TOUCH CONTROLS (x 33937) on the right. (It was built at x 51297, y 2000 on Oct 7 and moved since.) The first section (2497:3804) was deleted from the file. This one rebuilds it with the same slides, text and calls; `02_figma.md` §R lists the few differences.
- **12 slides show, numbered 01–13** (Oct 3; since Oct 14: 13 show, 01–14, see the update above). Slide 02 (RE8) is hidden because it has no RE8 pictures yet (see Downloads).
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
