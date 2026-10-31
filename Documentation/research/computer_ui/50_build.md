# Office terminals · build log (INTEROFFICE)

*游戏视觉, account A, session local_5656cffd. One section per build stage, newest last. Spec: `41_c19_period_interaction.md` (C19), `20_design.md` (amendments 2026-10-30), `30_crosshair_grammar.md` (Q3 / A38), `40_for_red.md` (EC03 v3). Clone only until the Land stage; main is read-only for the build. W = `/Users/redwang/FrontRoomsVisualWork`.*

---

## TB1 · Renderer + panel machine (2026-10-30, 12:2x–12:5x)

**Clone.** `W/proj_terminal` = `mkclone.sh proj_terminal` from main **3ea3d7bb** (0 dataless files skipped; `install_tools.py` ran inside mkclone), Library `cp -Rc` from `W/proj_examcrt` (Unity 6000.6.3f1). Silent clone (rule 24). Package list: `W/terminal/package/MANIFEST.txt`.

**Result.** Roslyn OK on macOS and iOS defines (0 errors; no warnings in the new files). One Unity run of the harness: **32 / 32 PASS** (`W/terminal/caps/run2/log.txt`; run1 found 3 issues, all fixed, see "Fixed in the round"). Image: `W/terminal/out/TB1_terminal_renderer.jpg` (Shot format: MAIN close-up, DOC close-up, glance at 1.5 m, the raw 1440 × 800 picture).

### What was built

| Piece | File (clone) | What it does |
|---|---|---|
| Face | `Scripts/Rendering/Screens/FrontRoomsTerminalFace.cs` + `Resources/Terminal/TerminalFace.png` | The C20 face as ONE swappable PNG atlas (128 × 320): the 8 × 16 face in CP437 order (y 0–255) and the two block alphabets for MAIN's big type (y 256–319). Unicode ↔ CP437 for ASCII and the 14 layout glyphs of 41 §6.1; the VGA 9th-column rule (0xC0–0xDF). `FrontRoomsTerminalFace.Use(atlas)` swaps at run time. |
| Stand-in face | `W/terminal/make_face.py` → the PNG | FR816 from `W/exam_computer/pages.py` unchanged (A–Z, 0–9, `. , : / - ' &`); added in its manner: the other 24 ASCII symbols, the 14 CP437 glyphs (geometric), block alphabets 5 × 7 and 4 × 5 (match 41 §3.3's MEMOS / 2 UNREAD). **Lowercase slots carry the capitals (stand-in only).** Cap stays 10 rows (O-5's 11 rows is 平面视觉's face). 108 / 108 slots drawn. |
| Grid | `FrontRoomsTerminalGrid.cs` | 80 × 25 (glyph, attribute), compare-before-write so `Version` moves only on a real change; `Put / PutRight / PutCentred / Fill / Paint / BlockText / RowText`; missing-glyph and clipped-string counters (AT-C7). `FrontRoomsTerminalPalette`: the VGA 16 colours and the 41 §3.4 roles FIELD 0x17, BRIGHT 0x1F, BAR 0x71, TITLE 0x30, SOFTKEY 0x71 (平面视觉 sets the values; one place). |
| Raster | `FrontRoomsTerminalRaster.cs` + `Resources/Rendering/FrontRoomsTerminalRaster.shader` | 720 × 400 at 2× = **1440 × 800**, whole pixels, 9 × 16 cells. **GPU path (default):** cells uploaded as an 80 × 25 RGBA8 texture, one offscreen procedural draw into an sRGB RenderTexture, mips generated on the GPU. **CPU twin** (`RasterCpu`): the same function in C#, used as the fallback (no graphics device / shader unsupported) and as the harness's reference. |
| Panel machine | `FrontRoomsTerminal.cs` | MAIN / DOC / LIST / HELP per 41 §3.3 and §4.1, the lightbar, paging (PgDn at a memo's end opens the next, PgUp at the top the previous one at its last screen), `More: - +`, the refusals (NotAuthorized, TopOfData, BottomOfData), unread marks and count, last-item memory per content key and run, HELP returns where it was opened, the leave paths, row 24 message line, row 25 from a naming callback (or CUA short-form soft keys on touch), touch taps (two-tap rule, soft keys with row 24 in their hit area), a transcript per panel. Timing: ≥ 0.34 s between screen changes (one press held, the rest dropped), repeat after 0.5 s then every 0.34 s, 0.20 s lockout after open, C25 latch for anything held at open. Pure C#; no device input, no sound. |
| Content | `FrontRoomsTerminalContent.cs` + `Resources/Terminal/Content/interoffice.json` | All strings from data (JsonUtility). `terminal_<contentKey>.json` if present, else `interoffice.json`. Memo, index and queue strings are pages.py's; titles, key words, HELP rows and refusals are 41's drafts. Live tokens: `{CLOCK}` (h:MM AM), `{DATE_NOW}`, `{DATE_BACK4}`, `{MONTH_NOW}`, `{MONTH_BACK4}`, `{DRILL_WEEKDAY}`, `{DRILL_MONTH}`, `{DRILL_DAY}`, `{UNREAD}`, `{N}`, `{M}`, `{PROGRAM}`, `:U` for capitals; in-game date = today's month and day in 1990, drill = first Tuesday ≥ 8 days later (pages.py's rules). Drafts with no source yet are listed in the file's `_about`. |
| Screen | `FrontRoomsTerminalScreen.cs` | The MonoBehaviour on a desk monitor: `Attach(monitor, contentKey)`; TELL / ACTIVE / READING (20_design §4.1): ACTIVE = in view (frustum, facing) within r_A = min(8 m, 0.9 × the monitor's LOD cull distance), +0.5 m hysteresis; READING always live. A pool of `LiveBudget` pictures (2 desktop, 1 handheld and WebGL); a terminal keeps its picture until another needs the slot. Renders only when the grid changed (key, clock minute, face swap) or the GPU lost the RT. TELL = one shared 90 × 50 texture (field + title band, no text). |
| Tube hook | `Scripts/Media/FrontRoomsScreenVideo.cs` (`[TERMINAL-BEGIN/END]` + 7 guarded lines; `W/terminal/package/FrontRoomsScreenVideo.terminal_hook.diff`) | `ShowTerminal(Texture)` / `ClearTerminal()` / `ShowsTerminal` / `PictureCentre`. While a terminal shows: its picture wins in `BindTexture`, the V4 still mix leaves it (`WantsStill`), the shared decoder never plays for it, E never toggles its power (its E is the READ close-up), the tube takes the live path (underscan 276 × 207 mm, barrel 0.02, its effects) with **400 scan lines** (VGA text mode) and never the no-frame glow. With no terminal every guard is false. |
| Harness (clone only) | `Editor/TerminalHarness/TerminalCapture.cs` | The checks below and the captures. ASCII, no tuples, no GetInstanceID. |

### API (for the next stages)

```csharp
// level design (L2: which desks) - after FrontRoomsScreenVideo.AttachDesk(monitor):
FrontRoomsTerminalScreen FrontRoomsTerminalScreen.Attach(GameObject monitor, int contentKey);
static void FrontRoomsTerminal.ResetRun();                 // a new run: unread marks + last-item memory

// the EXAMINE terminal extension (游戏视觉, next stage):
void BeginReading(float lockout = 0.20f);   void EndReading();
void Apply(in TerminalInput held, float unscaledDt);      // every close-up frame, FrontRoomsInspect.Dt
void Press(TerminalVerb v);                                // one-shots: wheel notch, soft key, swipe
void Tap(int col, int row);                                // touch -> cell on the 80 x 25 grid
bool WantsExit; string Footer; string PanelTranscript;
void SetControlNames(TerminalNamer namer, bool softKeys);  // row 25 + HELP name the real controls
FrontRoomsTerminal Machine; FrontRoomsTerminalGrid Grid; bool Live, Reading; Texture Picture;
static event Action<FrontRoomsTerminalScreen, TerminalRefusal> TerminalRefused;   // sound design (O-1: no tone)
static event Action<FrontRoomsTerminalScreen> TerminalKey;                         // sound design
static int LiveBudget;

public struct TerminalInput { int Move, Side, Page, Jump; bool Select, Cancel, Exit, Help; }   // HELD levels
public enum TerminalVerb { None, Up, Down, Prev, Next, PageUp, PageDown, First, Last, Select, Cancel, Exit, Help, Tap }
public enum TerminalAction { Move, Side, Select, Page, Cancel, Exit, Help }      // what a namer names
public delegate string TerminalNamer(TerminalAction a);   // null = leave the item out (Help on a Mac / pad)
public enum TerminalPanel { Main, Doc, List, Help }   public enum TerminalRefusal { NotAuthorized, TopOfData, BottomOfData }

// the tube (FrontRoomsScreenVideo): ShowTerminal(Texture) / ClearTerminal() / ShowsTerminal / PictureCentre
// the face: FrontRoomsTerminalFace.Current / Use(Texture2D) / Changed;  palette roles: FrontRoomsTerminalPalette.Field ...
```

### Budgets (measured on the Mac editor, Metal, run2; iPhone and WebGL not measured)

| Item | Value |
|---|---|
| World draws | **+0 per frame.** The picture rides the CRT's existing picture-surface draw; the faceplate is unchanged. |
| Offscreen work per screen change | 1 procedural draw (3 vertices, 1,152,000 px) + GPU mip generation (11 levels). Only on: a key that changed the screen (≤ 3 / s while reading), the clock's minute, becoming ACTIVE, a face swap. |
| Texture memory | **5.86 MiB per live picture** (6,143,892 B, RT with mips, GPU only) + 8 KB cells. Pool max: **11.7 MiB desktop (2), 5.86 MiB handheld / WebGL (1).** Shared: glyph texture R8 32 KiB, TELL 90 × 50 ≈ 24 KiB, the readable atlas asset 160 KiB (CPU + GPU copies; it can be unloaded after parsing, see notes). CPU fallback only: 16.1 MiB per picture. |
| CPU per idle frame | **1.5 µs** for one live terminal (frustum test, minute check; measured through reflection, so an upper bound), **0 B GC**, 0 renders. |
| CPU per screen change | layout 0.017 ms + render 0.08 ms (mean of 50; cells upload + command recording), 0 B GC per render. First render of a new panel 0.31 ms. **The very first render in the editor took 28 ms** (synchronous shader compile in the harness); see notes. |
| CPU twin (reference / fallback) | raster 1.0–1.2 ms; with CPU mips and upload 2.9–3.0 ms per change. |

### Checks (harness, run2: 32 / 32 PASS)

- Content and face load (memos 2, jobs 1, HELP rows 7; 111 drawn codes; alphabets 5 × 7 and 4 × 5).
- 0.34 s window: first press acts, one held, a third dropped, the held one applied at +0.35 s. Lockout 0.20 s. C25 latch (held 1.5 s at open: no move).
- AT-C2: hold DOWN 10 s on DOC: first repeat 0.517 s, then 0.35 s apart, max 3 changes in any 1 s; mash UP / DOWN at 20 Hz for 10 s on DOC and on MAIN: max 3 in any 1 s.
- Paging: memo 1 → PgDn → memo 2 → PgDn refuses BottomOfData (row 24 + event; stays on a dead key) → PgUp → memo 1 (message cleared) → PgUp refuses TopOfData. Long memo: `More: +`, one screen per PgDn, `More: - +`, End, `More: -`, PgDn opens the next memo at its top.
- LIST: Enter refuses NotAuthorized (row 24 + event), the message stays on a dead key, Cancel returns to MAIN on the queue item. HELP from DOC and back to DOC. Unread mark ■ clears, second line `1 UNREAD`. Reopen (and a rebuilt chunk with the same key) opens on MAIN with the bar on the last item opened.
- Leave paths: EXIT anywhere (immediately, even inside the window), CANCEL on MAIN leaves, CANCEL on DOC goes back.
- Row 25 for four label sets: PC `F1=Help  Enter=Select  F3=Exit` / `F1=Help  Esc=Cancel  F3=Exit  PgUp/PgDn=Page` / `F1=Help  Enter=Select  Esc=Cancel  F3=Exit` / `Esc=Cancel  F3=Exit` (41 §5.2 exactly); Mac `Return=Select  E=Exit`, `Esc=Cancel  E=Exit  ←/→=Page`; pad `A=Select  X=Exit`; touch soft keys `[Enter] [Help]`, `[Cancel] [Help]`.
- AT-C7: 4 panels × 4 label sets, 0 missing glyphs, 0 clipped strings.
- Touch: tap moves the bar, tap on the lit item opens it; a soft key hit on row 24 presses it.
- Raster: **GPU picture = CPU twin, 0 of 1,152,000 px differ** (MAIN and DOC), so orientation, the 9th column and the colours are exact.
- In the map (seed 4242, Office desk CRT on a station floor, V4 still before): inert before Attach (0 terminals before and after the run starts); Attach shows the terminal; **the 4 other screens unchanged**; ACTIVE at 1.5 m (live picture bound, 400 lines, signal 1, 1 render); idle frames render nothing; main's close-up opens on E; SELECT → DOC on the tube; after the close-up the glance is MAIN; removing the terminal gives the desk back (480 lines, its still frame).

**Fixed in the round (run1 → run2):** HELP's key column now fits the longest control name (touch `Tap it again` was clipped); two harness checks read the tube before it ticked (a batch run renders only on demand, and the tube ticks on `beginContextRendering`): they now render first.

### Notes for the next stage (EXAMINE terminal extension, input, framing, the eye)

1. **Wiring.** On the close-up opening on a terminal's READ target: `BeginReading()`; each close-up frame `Apply(held, FrontRoomsInspect.Dt)`; one-shots through `Press`; `WantsExit` → leave the close-up, then `EndReading()`. The Inspect branch asks "is this a terminal?" with `FrontRoomsScreenVideo.ShowsTerminal` or `GetComponent<FrontRoomsTerminalScreen>()` (CONTRACT 关卡设计 L3′).
2. **READ target.** Build it like the station TV's (`MakeInspectTarget`): flat, read, ship, 0.276 × 0.207 m on the picture surface, `dist[0]` = 0.251 desktop / 0.265 phone; the harness's target is the mock's and is not a product. Only a terminal (not TELL-only desks, not stills) gets one (Q3 / A38: the eye on readable terminals only).
3. **Reading preset (20_design §9.3).** In the close-up the faceplate's RT glass reflections land on the text (lamp highlights over `FROM:` in the DOC capture). The framed terminal needs the reading preset (cap glass reflection, mask, curvature) and the 4.5 : 1 contrast measure; not done here.
4. **Taps.** Map `FrontRoomsInspect.CurrentFaceViewportRect` → the lit raster (276 × 207 mm inside the 292 × 220 mm picture surface, barrel 0.02) → (col, row) = (floor(u × 80), floor((1 − v) × 25)); then `Tap(col, row)`. Soft keys exist only when `SetControlNames(..., softKeys: true)`.
5. **Names.** `SetControlNames` takes a `TerminalNamer`; build it next to `FrontRoomsKeyHud.WithUseControl` (USE key from `FrontRoomsInput.UseKey`, pad from `PadName`), re-call it on input-mode change. HELP rows use the same namer; a null name drops the row.
6. **First-render warm-up.** The first raster render paid 28 ms in the editor (synchronous shader compile). In a player the variant is precompiled, but the first pipeline-state use still costs; render once when the first terminal attaches (TELL time), not on the first walk-up. Not measured on device.
7. **Atlas memory.** The readable atlas asset can be `Resources.UnloadAsset`-ed after `FrontRoomsTerminalFace` has parsed it (−160 KiB GPU, −160 KiB CPU); left for the land pass.
8. **Batch-run gotcha.** Tube material state is pushed in `beginContextRendering`; any batch check of `_Lines`, `_Signal` or the bound texture must render the camera first. Likewise V4's "unseen" test sees nothing in batch (no game-view render), so a desk returns to its still at once there; in play it waits until unseen.
9. **Layout points for 平面视觉 (G1).** The second line `2 UNREAD` spans columns 2–79 (41's mock): on the tube it touches the raster edge under the barrel; check it in G1. Row 23 (Help item) and row 24 (message) are adjacent on MAIN, as in 41.
10. **WebGL / GLES3.** The raster shader is target 3.5 (texel loads); compiled and run here on Metal only. Prove it in the WebGL clone with O-4.

### Contract notes (other lines; nothing edited)

- **关卡设计:** `FrontRoomsTerminalScreen.Attach(monitor, contentKey)` after `AttachDesk` on the chosen desks (L2); `FrontRoomsTerminal.ResetRun()` at a run's start; the Inspect input branch (L3′) per note 1; `ThreatPerceivable(cam)` gates the READ prompt and the eye (L4, unchanged).
- **交互设计:** `TerminalInput` is held levels (the machine does edges, repeat and latches); wheel notches, soft keys and swipes go to `Press`; touch cells to `Tap`.
- **声音设计:** `TerminalKey` and `TerminalRefused(kind)` static events; no AudioSource here (O-1: no tone at first).
- **叙事设计:** replace `Resources/Terminal/Content/interoffice.json` whole; drafts without a source are listed in its `_about`; mixed case per 41 §6.4 when the C20 face has lowercase.
- **平面视觉:** replace `Resources/Terminal/TerminalFace.png` (layout in `FrontRoomsTerminalFace.cs` and `make_face.py`); colour roles in `FrontRoomsTerminalPalette`; O-5 (11-row cap) lives in the face.

---

## TB2 · The EXAMINE terminal extension (2026-10-30, 13:0x–13:2x)

**Clone.** Same `W/proj_terminal` (TB1's). Pre-TB2 copies of every file this stage edits: `W/terminal/base_tb2/`. Main's bases of the four edited game-visual files are unchanged since the clone (cmp, 13:2x). Main's `FrontRooms3DGame.cs` has moved since the clone (onboarding work); the contract diff is made against main's current file and applies to both.

**Result.** Roslyn OK on macOS and iOS defines (0 errors; the only new warning, an unused field, was removed). Harness `TerminalExamine` (clone only), one run per profile: **desktop 13 / 13 PASS** (`W/terminal/caps_tb2/desk1/log.txt`), **phone 14 / 14 PASS** (`…/phone3/log.txt`). Two phone runs before it found 2 problems, both fixed (see "Fixed in the round"). Stills (Shot format): `W/terminal/out/TB2_desktop_closeup.jpg`, `W/terminal/out/TB2_phone_closeup.jpg`. They go to the VL batch.

### What was built

| Piece | File (clone) | What it does |
|---|---|---|
| READ target | `Scripts/Rendering/Screens/FrontRoomsTerminalScreen.cs` | A terminal carries a READ target built like the station TV's (`MakeInspectTarget`): a flat `read` face on the picture surface, **0.276 × 0.207 m**, shipped, plastic, aim box 2 cm deep, kit `Screen_OfficeTerminal` → `E · READ SCREEN`. It is available only while the terminal is readable (CRT enabled, powered, showing the terminal), so **the E·1 eye shows on readable terminals only** (Q3 / A38); every other desk screen has no target. No zoom step (dist[1] = dist[0]). The def is set per class and refreshed when the class, the aspect or the player's FOV changes, never while reading. |
| Framing | same + `Rendering/Inspect/FrontRoomsInspect.cs`, `FrontRoomsInspectDef.cs` | **Desktop / WebGL: today's close-up**, 0.251 m, FOV 61°, picture 70 % of the height (measured 0.700), TEXT kept. **Phone (Red 11:00 / 11:1x):** 0.265 m, FOV narrowed to 44.7° (fovDelta −31.3, the rig's existing fovMin path), composition B with a 1 % margin on the **full screen** (new `InspectDef.bleed`: the solve ignores the safe area and the transcript's free rect for this target only), so the picture is **95 % of the full height, centred** (measured: face rect y 0.025–0.975, x 0.209–0.791; lit rows 30–1175 of 1206, margins 30 / 31 px), no transcript id (no TEXT chip, no panel). **iPad:** 70 % of the height at the drawn FOV, TEXT kept (computed, not run). |
| Phone surround | `Rendering/Inspect/FrontRoomsInspectTerminal.cs` | Two plain bands (sRGB 7 15 10, the dark glass as EC03 v3 measured it) fill the sides beyond 5 mm of dark glass, on a screen-space canvas at sortingOrder −50 (under every HUD canvas, so BACK stays on top). They follow `CurrentFaceViewportRect` each frame, fade in with the travel (0.15–0.45 s of the close-up) and out in 0.15 s. BACK is the only control (main's close-up). |
| The extension | `Rendering/Inspect/FrontRoomsInspectTerminal.cs` (new) | `Opened` → `BeginReading`, `Closed` (or an abort, seen once per frame) → `EndReading`. `Tick(dt, input)` from the game's Inspect branch reads the verb set as held levels and says when to leave. Row 25 and HELP name the player's control. The desktop / iPad transcript (`terminal_panel`) follows the panel. `MapRunStarted` → `FrontRoomsTerminal.ResetRun()`. |
| Read-outs | `FrontRoomsInspect.cs` | `RigBaseFov`, `RigCamera`, `CurrentFaceViewportRect` (the face projected through the rig camera now: taps, the surround). |
| Catalog | `FrontRoomsInspectCatalog.cs` | `TerminalKit` (same prompt string as the station TV), `TerminalTranscript`, `SetTranscript(id, lines)`. |
| Tube hook | `Media/FrontRoomsScreenVideo.cs` | `PictureSurface` (the READ target's parent), inside `[TERMINAL-BEGIN/END]`. |
| Stand-in (clone only) | `Assets/TerminalHarness/TerminalDeskStandIn.cs` | L2 stand-in, editor only: the nearest Office desk CRT within 12 m becomes the terminal, once per run; a harness can `Mark` a desk itself. Not a proposal for L2's rule. |
| Harness (clone only) | `Editor/TerminalHarness/TerminalExamine.cs`, `TerminalHarness/TerminalHarnessBoot.cs` | ASCII, no tuples, no GetInstanceID. The camera renders at the capture size throughout, so every solve sees the device's aspect. |

### Controls (41 §5.1 / §5.3, as built)

| Verb | Keyboard (read directly while a terminal is read) | Pad | Touch (hooks for 交互设计) |
|---|---|---|---|
| MOVE / SIDE | ↑ ↓ ← →, the bound W A S D through `FrontRoomsInput.Move` (**S = down**, O-3), the left stick; the **wheel**, one step per gesture (0.2 s re-arm) | d-pad, left stick | tap an item (`TapViewport`) |
| SELECT | Enter, numpad Enter, Space | A / CROSS | tap the lit item; `[Enter]` soft key |
| CANCEL | **Esc**, Backspace | B / CIRCLE | `[Cancel]` soft key; Android Back (`Back()`) |
| EXIT | F3, **the USE key** (whatever it is bound to) | X / SQUARE, MENU | BACK and the pull-back (the game's snapshot) |
| HELP | F1 | — | `[Help]` |
| PAGE | PgUp / PgDn (fn + ↑ ↓ on a Mac), Home / End = Jump | LB / RB | swipe (`Swipe(±1)`) |

- On a touch-only handheld the snapshot's MOVE (the hidden stick) is ignored, so a pull-back never moves the bar before it leaves.
- **Row 25 names** (41 §5.2), re-laid only when the words change: PC keyboard (`F1=Help  Enter=Select  F3=Exit`), Mac (`Return=Select  E=Exit` / `Esc=Cancel  E=Exit  ←/→=Page`, checked in the run), WebGL and a keyboard on a handheld (`Bksp`, the USE key, `←/→`), pad in Xbox or PlayStation words (`A`, `B`, `X`, `LB/RB` / `CROSS`, `CIRCLE`, `SQUARE`, `L1/R1`), touch soft keys (`[Enter] [Help]`, `[Cancel] [Help]`, checked in the run). Desktop switches to pad words at the first pad press (`FrontRoomsInput.ExternalActivity`), a handheld follows `FrontRoomsTouchControls.CurrentMode`.
- **ThreatPerceivable (L4):** the eye and the prompt hide through the game's own `CanShowInspect`, as for every EXAMINE target. The terminal also refuses at open if `relay.ThreatPerceivable(cam)` holds then, and leaves on it if the world runs during a close-up (D2 live mode). In v1 the close-up pauses the world, so nothing new can appear while reading.

### Checks (desktop 13 / 13, phone 14 / 14)

Inert before a desk is marked (0 terminals, 0 terminal READ targets) · the stand-in makes one terminal with one READ target, readable · the walk-up at 0.99 m: the game aims the READ target, kind `Read` (the eye) and `E · READ SCREEN` · E opens main's close-up and the terminal reads · framing (desktop 70 %; phone 95 %, centred, measured on the capture too) · phone: no TEXT chip, soft keys, the surround on · desktop: row 25 names the Mac's keys, the transcript holds the panel · no zoom step · SELECT → DOC (the desktop transcript follows) · S moves down and never leaves · EXIT leaves through the game's L3′ branch (contract applied in the clone) and the glance is MAIN again · no surround after the close-up.

**Fixed in the round (phone1 → phone3):** the surround canvas had `HideFlags.DontSave`, which `FindObjectsByType` skips, so the capture never composited it (in play it drew); it now has no hide flags (`DontDestroyOnLoad` keeps it out of saved scenes). Then a sliver of beige bezel showed between band and glass: the dark glass is about 5.4 mm at the sides, not the mock's 12 mm, so the bands now start 5 mm out.

### Contracts (nothing edited in other lines' files in main)

- **关卡设计:** `W/apply/terminal_contract/CONTRACT_map.diff` + `README.md`. L3′: in `Phase.Inspect`, if `FrontRoomsInspectTerminal.Owns`, `Tick(FrontRoomsInspect.Dt, input)` and `EndInspect(false)` when it says so (after CancelAfter), else C6 as before; C25b `moveLatch`. About 15 lines against main's current `FrontRooms3DGame.cs`. L2 (which desks) stays theirs: `FrontRoomsTerminalScreen.Attach(monitor, contentKey)` after `AttachDesk`. `ResetRun` needs nothing from them (the package hooks `MapRunStarted`). L4 unchanged.
- **交互设计:** `W/apply/terminal_contract/TOUCH_NOTES.md`: `PictureViewportRect`, `TapViewport`, `Swipe`, `Back`, what the phone close-up already does.

### Notes for the Land stage

1. **Land** the MANIFEST's LAND list (TB1 + TB2). The four EDIT files had unchanged bases in main at 13:2x; re-check with cmp first. **Never land** the contract (`FrontRooms3DGame.cs`) or anything under `Assets/TerminalHarness/` or `Assets/Editor/TerminalHarness/`.
2. **Safe without the other lines:** with no L2 `Attach`, nothing changes in play. With L2 but without L3′, a terminal opens main's ordinary close-up on MAIN (E, S and Esc leave as today; the terminal's keys are not read). With L3′, everything above.
3. **The one test round:** Roslyn macOS + iOS, then TB1's `TerminalCapture` and TB2's `TerminalExamine` (desk + phone) in a clone with the landed state plus the contract; all three are in `W/proj_terminal`. TB1's harness was adapted to TB2 after these runs and compiled, **not re-run** (rule 23): it now uses the terminal's own READ target, leaves through EXIT on the terminal keys, and turns the stand-in's automatic pick off (its inert checks). Its pre-TB2 copy is `W/terminal/base_tb2/TerminalCapture.tb1.cs`.
4. **Open, not in TB2:** the reading preset (20_design §9.3: the lamp highlights over `FROM:` in the desktop still) · touch routing (交互设计) · the inverse barrel for taps (≤ ½ row at the corners) · WebGL keys (O-4) · iPad framing and the pad / PC / WebGL names were not run (the namers are TB1's checked label sets) · a hint of the bezel's inner corners at the very bottom corners of the phone still · the desktop transcript panel (C24, 关卡设计).
5. **Gotcha for harnesses:** `FindObjectsByType` skips `HideFlags.DontSave` objects; a capture that converts overlay canvases misses them.

---

## Land · one test round, the package, the sheet (2026-10-30, 13:2x–13:5x)

**Result: READY, not applied.** `W/apply/terminal_a.sh` (apply convention v2) lands TB1 + TB2: **27 paths**, 23 new and 4 replaced (main's `.meta` kept). Bases = main now (HEAD 3ea3d7bb + working tree). DRY RUN OK on main; the rehearsal on a scratch copy gave MANIFEST OK, then ALREADY APPLIED. The one test round is green after one fix. Nothing changes in play until 关卡设计 calls `Attach` (L2); the station/exit run proves it.

**Re-sync.** `W/proj_terminal` was re-synced from main at 13:27 (rsync, no `--delete`: main deleted nothing the clone holds). Main had changed 10 game/map files (Relay v2, onboarding), `ProjectSettings.asset` and `Tools/relay_baseline_run.sh`. The 4 EDIT bases are unchanged in main (cmp), so the clone's edits were kept as they are. The contract was re-applied to main's current `FrontRooms3DGame.cs` (e51939cc → 6aee6289), then `apply_local_patches.py` (nothing to do) and `install_tools.py` (test bundle id `com.redwang.frontroomsss.test.terminal`). Pre-sync copies: `W/terminal/land/clone_pre_resync/`. Silent clone (rule 24).

### The test round

**State L** is exactly what lands: main + the payload, with no contract, no stand-in and no harness (parked in `W/terminal/land/parked/`, then restored). **State T** adds `CONTRACT_map.diff` and the clone-only stand-in. Scripts: `W/terminal/land/chain.zsh`, `chain_fix1.zsh`. Logs: `W/terminal/logs/land_*.log`, outputs in `W/terminal/land/out/` and `W/terminal/caps_land/`.

| Suite | State | Result |
|---|---|---|
| Roslyn, main + payload, macOS defines | — | COMPILE OK, 0 errors (Assembly-CSharp 167 files, Editor 75). No new warnings: the only payload-file warning, `FindFirstObjectByType` in ScreenVideo line 276, is already in main. |
| Roslyn, main + payload, iOS defines | — | COMPILE OK, 0 errors |
| Roslyn, the clone tree (contract + harness) | T | COMPILE OK, 0 errors |
| `FrontRoomsInspectTests` | L | **130 / 130 PASS** (16 s) |
| `FrontRoomsInspectPlaytest` (EXAMINE) | L | **134 / 134 PASS** (165 s) |
| Station/exit run `-autopilotExit -autopilotStation 4 -autopilotSeed 2554` | L | **PASS · ended by exits** on the second run, errors 0, EXIT 27 / 27, station 15 / 15, doors 2 / 2, 0 terminal log lines. The first run failed only on FMOD `BankLoadException`: the clone had no `FMOD/FrontRooms/Build/Desktop` banks. I copied them from the clone's own `StreamingAssets` banks (same sha1) and re-ran. |
| `TerminalCapture` (TB1 harness, adapted in TB2) | T | **32 / 32 PASS** after the fix. The first run had 26 passes and 2 failures (see "The one fix"). |
| `TerminalExamine` desktop (extended for this round) | T | **19 / 19 PASS** |
| `TerminalExamine` phone (iPhone Pro profile) | T | **20 / 20 PASS** |

- **Station run against the 12:16 run of the same command** (`W/doorhw_work/main-autopilot_base`): cells 62, zones 8, routes 4, keys 1, office rooms dressed 5, floor 4, chunks max 25 are all the same. FPS: average 57.3 against 57.6, p95 16.9 against 16.7 ms, p99 22.7 against 21.3 ms.
  - The worst frame was 745 ms, against 337 ms in the 12:16 run. It came 0.5 s into the run start, with 9.5 ms of map work in it. Every handoff spike is about 1.3× the 12:16 run's at load average ~30. No terminal code runs without `Attach`.
  - The station checks differ in content but not in count. Main's onboarding (10-30) moved the first send to floor 2.
- **The terminal play check** (`TerminalExamine`, extended for this round): the real map, the game's own aim and close-up, and the contract's L3′ branch.
  - The glance at 1.5 m shows the eye, `E · READ SCREEN`, and a live picture. E opens the close-up. Framing: desktop 70 %, row 25 names the Mac's keys, TEXT kept. Phone: 95 % (face rect y 0.025–0.975; lit rows 30–1175 of 1206), no TEXT chip, soft keys, the side bands.
  - The lightbar goes down to 1 and back up to 0. SELECT opens DOC, and S moves down without leaving. PAGE opens memo 2 of 2 at its top. CANCEL goes back to MAIN on the memo just read.
  - The bar moves to the print queue and SELECT opens LIST. SELECT on job 0412 is refused: `Not authorized: only the owner of a job can change it.` appears on row 24, and `TerminalRefused` is raised once.
  - EXIT leaves through the game's branch and the glance shows MAIN again. No surround remains after the close-up.
  - The harness is clone only (`Editor/TerminalHarness/TerminalExamine.cs`, pre-round copy `W/terminal/land/TerminalExamine.tb2.cs`).

### The one fix (rule 23: one fix, then re-run only the failed suite)

`TerminalCapture` failed "ACTIVE at 1.5 m": the tube kept the TELL texture and no live picture was made, and a NullReferenceException in the harness followed from that. The cause was in the package.

- The game's first-person camera is not tagged `MainCamera`, so `Camera.main` is null in play.
- `TickAll` then fell back to `FindAnyObjectByType<Camera>()`, which can return any camera. Here it was the capture's UI camera at y −10000, so the terminal was never in view.
- TB1 had passed by luck of object order.

**Fix:** `FrontRoomsTerminalScreen.ViewCamera()`. It tries the EXAMINE rig camera first, then main's `FrontRoomsHeldItem.ViewCamera` (enabled, no target texture, highest depth; already used by the key assembly and the shot look), then `Camera.main`, then any camera. `TickAll` and the READ target's aspect both use it. Pre-fix copy: `W/terminal/land/FrontRoomsTerminalScreen.prefix.cs`. After the fix, Roslyn passed (macOS, iOS, clone) and only `TerminalCapture` was re-run: 32 / 32 (ACTIVE at 1.5 m: live picture, renders 1). The `TerminalExamine` runs and the sheet are from before the fix; their glance was live through the old fallback.

### Budgets (Mac editor, Metal, the land round; iPhone and WebGL not measured)

| Item | Value |
|---|---|
| World draws | +0 per frame (the picture rides the CRT's picture-surface draw) |
| Texture memory | 5.86 MiB per live picture (6,143,892 B, RT with mips); pool max 11.7 MiB desktop (2), 5.86 MiB handheld / WebGL (1). GPU raster object 6,159,892 B. CPU fallback only: 16.1 MiB per picture |
| CPU per screen change | render 0.11 ms (mean of 50, 0 B GC), layout 0.027 ms (DOC). These are higher than TB1's 0.08 / 0.017 ms because the machine was under load. |
| CPU per idle frame | 2.1 µs for one live terminal (an upper bound, through reflection), 0 B GC, 0 renders |
| GPU = CPU twin | 0 of 1,152,000 px differ (MAIN and DOC) |
| Without `Attach` | 0 terminals, 0 READ targets, the 4 other screens unchanged; the station run as above |

### The package (`W/apply/terminal_a*`)

- `terminal_a.sh`: dry run by default, `--apply` writes. Sections 1–4 are as in `double_door_hardware.sh`. Section 5 reads the main API the payload uses:
  - `MapRunStarted` / `MapRunEnded`;
  - `FrontRoomsMapHunter.ThreatPerceivable(Camera)` (L4);
  - `FrontRoomsInput`: `FrameSnapshot`, `ReadFrame`, `BindingsChanged`, `SuspendGameplay`, `PathOf`, `UseKey`, `AppleKeys`, `PlayStationPad`, `ExternalActivity`;
  - `FrontRoomsHandheld`: `Active`, `IsTablet`, `AspectFov`, `FovReferenceAspect`;
  - `FrontRoomsTouchControls`: `InputMode`, `CurrentMode`;
  - `FrontRoomsHeldItem.ViewCamera`.
- Write order: 13 metas, then the PNG, the JSON, the shader, then the C# (new files before the 4 replaced ones).
- `terminal_a_bases.txt`: 4 sha1 + 23 ABSENT. `terminal_a_keep.txt`: the 4 kept metas. `terminal_a_expected.txt`: 27 paths. `terminal_a_payload/`, `terminal_a_base/` (the 4 exact bases), and `terminal_a.diff` (3,474 lines) complete the package. Builder: `W/terminal/land/build_package.py`.
- **Checks:**
  - DRY RUN OK on main (`W/terminal/land/dryrun_main.txt`).
  - Rehearsal in `W/tools/_test/terminal_a_1030/`: DRY RUN OK, then `--apply` MANIFEST OK, then ALREADY APPLIED. Half-applied and changed-`.meta` both REFUSED, and the target was restored afterwards.
  - `rebase_apply.sh terminal_a --no-install`: PASS, nothing to rebase.
- **Apply (Red or the 游戏视觉 session, not done here):** `bash /Users/redwang/FrontRoomsVisualWork/apply/terminal_a.sh --apply`. Then add a LANDINGS + MERGED.md row.

### Contracts (not landed; other lines)

- **关卡设计:** `W/apply/terminal_contract/CONTRACT_map.diff` with `README.md`. `patch -p1 --dry-run` against a copy of main's `FrontRooms3DGame.cs` (e51939cc, 13:4x) succeeds (rc 0). L2 is the `Attach` call only.
- **交互设计:** `W/apply/terminal_contract/TOUCH_NOTES.md`.

### Sheet

`W/terminal/out/LAND_terminal_a.jpg` (Shot format, 1920 × 1800, PASS). It shows:

- the glance at 1.5 m with the eye (crop 2×);
- the desktop close-up on MAIN and on a memo page;
- LIST with the refusal on row 24;
- the phone close-up at 95 %, on MAIN and on LIST refused.

It is queued for the daily VL batch with the TB1 / TB2 stills (`W/vl_pending/terminal_a.md`, no numbers reserved). Script: `W/terminal/land/make_sheet.py`.

### Open (unchanged from TB2 unless stated)

- The reading preset (20_design §9.3): RT lamp highlights over the text in every desktop close-up.
- Touch routing (交互设计).
- The inverse barrel for taps.
- WebGL keys and the GLES3 raster shader (O-4).
- The iPad and pad / PC / WebGL names were not run.
- The desktop transcript panel (C24).
- The first-render warm-up was not measured on a device.
- `2 UNREAD` touches the raster edge on the phone close-up (G1, 平面视觉).
- The clone is left in state T (contract + stand-in) with the FMOD build banks added. None of these is in the package.
