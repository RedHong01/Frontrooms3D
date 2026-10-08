# 01 — Inspect inventory: every asset worth a close look

Date: 2026-10-07. Stage: inventory, read-only (visual chat 游戏视觉).

**Red's request (translated):** every visual asset that can really be looked at, such as the evacuation plan, should be something the player can walk up to, interact with, and see in a close-up shot. While the player looks at it up close, the game pauses.

**Reference frame:** `research/placard/images/q16_u1_A3_legend_0p6m.jpg` (Q16 placard v2, in the clone).

**What was read:**
- all 113 kit sidecars in `Assets/Resources/Props/Models/*.json`;
- the code that places them: `FrontRoomsOfficeKit.cs`, `FrontRoomsFurniturePile.cs`, `FrontRoomsInteractableKit.Window.cs`, `FrontRoomsMapWorld.cs` (`Dress`, `PlaceProps`, `SpawnKey`, doors and windows), `FrontRoomsRoomStream.cs` (`BuildProfileProps`), `Media/FrontRoomsScreenVideo.cs`;
- the 4 modules in `Assets/Levels/Modules` and `Assets/Levels/FrontRoomsLevel0.asset`;
- the decal art, by looking at it: `Prop_Label`, `Prop_VendingFront`, `Prop_CopierPanel`, `Prop_ScreenCRT_E`, the placard v2 print and `FrontRooms_Ad_01.mp4`;
- the specs: `placard/10_spec.md` + `20_build.md`, `outlets/10_spec.md` + `20_code.md`, `interactables/10_spec.md`, `exit_sign/10_spec.md`, `wallpaper_motion/32_pattern_native_hints.md` + `30_narrative_phosphor.md`, `SCREEN_ADS.md`, `VISUAL_CHAT_TASKS.md`, `GRAPHIC_VISUAL_TASKS.md`.

Nothing was rendered, so this stage adds no row to the VERIFICATION LOG.

---

## 0. The short version

1. **25 rows are listed: 5 MUST, 7 SHOULD, 13 NO.** The NO rows group about 90 kits plus the code-built props. MUST covers the placard, the door sign, the door number plate, the zone key with its tag, and the scratch-throughs.
2. **No MUST asset is visible in today's game.**
   - The placard is built and verified only in the clone `proj_placard`.
   - The door sign, plate, key and tag FBX files are in main (Codex 8ef5b64), but nothing spawns them; they wait on Red's door/key call (D1.5).
   - Their text slots `Prop_SignEngraved` and `Prop_KeyTagNo` do not exist in main, so they would render blank.
   - The scratch-throughs are on hold (Q2, Red §8).
   - **So the inspect mechanic can be built and tested today only on SHOULD props. Its showcase targets land with Q16 and D1.5.**
3. **Live SHOULD targets today:** the Office CRT monitor (it already plays an ad video and toggles power on E), the vending machine front, the copier panel, the desk phone and the wall clock. The CRT TV is live too, but only inside furniture piles.
4. **There are no notices, memos, posters or office signs anywhere in the game.** The paper stack and the binders are the natural carriers if Red wants more MUST targets (narrative + 平面视觉).
5. **Conflicts the mechanic must solve (map chat):**
   - E already opens doors.
   - Hold E breaks glass.
   - The CRT reads the E key directly with its own raycast, bypassing the game's input, HUD prompt and touch layer.
   - The sign and plate sit on the door leaf, so the aim ray hits the door first.
   - Every MUST asset is render-only and has no collider.

## 1. Status legend

| Code | Meaning |
|---|---|
| **LIVE** | In main and seen in today's game flow (title Lobby rooms → map) |
| **MAIN-IDLE** | Files are in main, but no code places them, or only the unreachable stream profiles do (`BeginPlayableSequence` has no caller, so the stream's Shift/Office/Run/Exit rooms never show) |
| **PIPELINE** | Exists in a clone, a spec or a Blender module; not merged, or waiting on Red |

**What today's player sees:**
- the title stream: Lobby rooms only (`lobbyOnlyTitle = true`);
- then the map: Level 0 and Office themes, at Low / Standard / Tall heights;
- Office kit dressing in Office rooms (`officeShare` 0.3 of Standard zones);
- piles in 35 % of rooms of 4 × 4 cells or more (≥ 60 % in Tall halls);
- module props in 30 % of rooms that fit a module;
- kit window frames;
- cube door leaves;
- keys as glowing cubes that are picked up automatically. `doorsNeedKeys` is 0, so keys open nothing.

**Classes:**
- **MUST:** it carries information (story, hint, wayfinding).
- **SHOULD:** rich detail that rewards a look.
- **NO:** nothing to read, or inspecting it would cheapen the mechanic (it repeats on every desk, it reads fine from afar, or it already has its own shot).

## 2. MUST

**Texel bar** (`interaction_audit/10_audit_report.md` §6.4): ≥ 1,600 px/m at the framing distance, 2,048 to be safe.

### M1 — Evacuation placard (Q16): `Kit_EvacPlacard` + `Kit_EvacPlacardLens`
- **Status:** PIPELINE.
  - Built in `W/proj_placard` with art v2 (`placard/20_build.md`). Not in main.
  - The Blender modules are in main (`evac_placard*.py`).
- **Where, how often:**
  - 1 per run, on the map side of the start door, centre 1.524 m.
  - Mounts over 100 seeds: P1 facade 87 %, P2 side wall 9 %, P3 past the pier 4 %.
- **What a close look gives:** hint and wayfinding.
  - The legend teaches the wallpaper cue language (WAY ON / EXIT / NO EXIT), the key to the hint system.
  - It also carries story: a plan of the start room, with YOU ARE HERE as a deliberate error.
- **Readable content:**
  - EVACUATION PLAN.
  - A one-room plan: grid A–B / 1–2, EXIT, YOU ARE HERE and a red route, "MATCH LINE — SEE SHEET A-3", north arrow, SCALE 3/16" = 1'-0".
  - Legend: AS PRINTED, IN POWER FAILURE, WAY ON, EXIT, NO EXIT. These are WP03 swatches at 1/25, captioned "WALLCOVERING SHOWN AT 1/25 SIZE".
  - Footer: IN CASE OF FIRE: WALK, DO NOT RUN. / CLOSE DOORS BEHIND YOU.
  - Title block: LEVEL 0 | SHEET A-2 OF 4 | PRINTED 03/90.
- **Flat or 3D:** flat; read the face. It is screwed to the wall.
- **Close-up ready?** Yes.
  - 6,000 px/m. A1 at 0.3 m passes. LOD0 is 1,754 tris.
  - **FLAG:** dark-glow legibility is 1.23, under the 1.30 bar.
  - The glow is lamp-driven (`FrontRoomsPlacardGlow`), so the pause must freeze it as it is.

### M2 — Door sign: `Kit_DoorSign`
- **Status:** MAIN-IDLE.
  - FBX and sidecar are in main.
  - The `Prop_SignEngraved` slot is missing, so the plate falls back to black with no text.
- **Where, how often:**
  - Both faces of every locked door: L0-K, OF-K, RN-K (Run is P2). The sign centre is at 1.524 m on the leaf.
  - Map doors exist only where a Low zone meets a Standard zone. The locked share is about 1 in 3 (W5, Red's pick pending).
- **What a close look gives:** wayfinding. It names a back-of-house room and marks the door as locked.
- **Readable content:** atlas cells "EMPLOYEES ONLY" (Lobby, Office), "STAFF ONLY" (Run), "STORAGE", plus one spare cell. The face is TeX Gyre Heros Bold at a 17 mm cap.
- **Flat or 3D:** flat.
- **Close-up ready?** The mesh is (664 tris). The text is not: 4,031 px/m by design, but the slot is not in main.

### M3 — Door number plate: `Kit_DoorNumberPlate` (+ `_Blue`, `_White`)
- **Status:** MAIN-IDLE. The `Prop_KeyTagNo` slot is missing, so the plate shows blank paper.
- **Where, how often:** the same locked doors (and EX-K), both faces, at 1.000 m beside the cylinder.
- **What a close look gives:** a hint. The plate matches the zone key: the same 2-digit number and the same colour as the key's tag.
- **Readable content:** a 2-digit number, 00–99, typed in Courier Prime, on a red, blue or white plate.
- **Flat or 3D:** flat.
- **Close-up ready?** The mesh is (480 tris). The digits are about 1,800 px/m by design: above 1,600, under the 2,048 safe line. The slot is missing.

### M4 — Zone key with ring and tag
Kits: `Kit_Key_Zone` (+ `_Nickel`), `Kit_KeyRing`, and `Kit_KeyTag_Rect`/`_Round`/`_Long` × red/blue/white (9 identities).
- **Status:** MAIN-IDLE. Today the key is a scaled emissive cube (`MapWorld.SpawnKey`), picked up automatically, and it opens nothing.
- **Where, how often:**
  - 1 per zone that has a key. In the autopilot reference run that was about 11 zones in 75 s.
  - It hangs on a host or lies on a surface, at module `KeySpot` markers.
- **What a close look gives:** a hint. The tag number and colour tell which locked door it opens (M3), and the HUD would say "KEY 14".
- **Readable content:**
  - the tag insert: a 2-digit number (00–99) in Courier Prime, on a red, blue or white tag in one of 3 shapes;
  - the key: brass or nickel, a 6-cut bitting, no maker mark.
- **Flat or 3D:** 3D. Turn it over: the tag face, the key's bitting, the split ring.
- **Close-up ready?**
  - Hero LOD0 is built for 0.3 m: key 2,184 tris, tag 1,038–1,282.
  - The tag number is about 3,300 px/m by design, but the slot is missing.
  - The key-use head-dip shot is a separate shot.

### M5 — Scratch-throughs on the wallpaper (A.11 layer 9, "M9")
- **Status:** PIPELINE, on hold.
  - Q2 is on hold.
  - `32_pattern_native_hints.md` §6 proposes keeping them; Red's §8 Q4 is still open.
- **Where, how often:**
  - Tier ≥ 4 only.
  - About 1 in 8 GROUND cells, one hashed 0.75 m tile per wall face.
  - Each is a wall-anchored cluster with letters 25–35 mm tall.
- **What a close look gives:** story. Other occupants were here.
- **Readable content:** `IT ONLY GOES IN`, `SAME ROLL AGAIN`, `COUNTED 41 DOORS`, `R.M. 6/90`, `D.K. 11/90` (frozen in A.11).
- **Flat or 3D:** flat. It is a region of wall, not an object.
- **Close-up ready?** No.
  - It is a shader layer: `_FR_InkType` at 1024² per 0.75 m tile ≈ 1,365 px/m, under the bar.
  - It is not an object, so it has no hit target.

## 3. SHOULD

### S1 — Office CRT monitor: `Kit_CRTMonitor` + `FrontRoomsScreenVideo`
- **Status:** LIVE.
  - Every Office-kit monitor plays `FrontRooms_Ad_01.mp4`. E toggles power.
  - Monitors that land in piles get no video.
- **Where, how often:** Office rooms. 88 % of stations get desk gear, which includes a monitor. Monitors are also pile pieces (Screen class).
- **What a close look gives:** flavour, and the 1990 TV world.
- **Readable content:**
  - The ad loop (12 s): LEVITZ / LOWEST PRICES OF THE YEAR / SALE / 1990; FRONTROOMS FURNITURE / YOUR CHOICE 3 PIECE SET / $799 PICK-UP; CALL NOW / 1 800 FRONTROOMS / SALE ENDS SUNDAY; YOU WILL LOVE IT; header "CHANNEL 4 · LIVE".
  - An unused DOS emission texture: `C:\>DIR /W` … `Directory of C:\LEVEL4` … `FLOOR.DAT EXIT.BAT` … `C:\>EXIT.BAT` / `Bad command or file name`.
- **Flat or 3D:** flat (the screen).
- **Close-up ready?** About 2,300 px/m (640 px over 0.28 m), but the ad's small type is soft. **FLAG 1:** "LEVITZ" is a real 1990 retailer, so the brand must go. **FLAG 2:** "FRONTROOMS FURNITURE" is clipped at the frame. Both belong to 平面视觉 item 3 (revise before this becomes an inspect target).

### S2 — Vending machine: `Kit_VendingMachine`
- **Status:** LIVE.
- **Where, how often:**
  - An Office wall unit: 70 % of rooms over 60 m², otherwise 30 %.
  - Also a pile piece (Case).
- **What a close look gives:** flavour and period detail.
- **Readable content:**
  - 6 × 6 slots numbered 11–66, priced $0.65–$1.45.
  - Invented brands: CRUNCHOS, ZESTO, Mallo, SALTS, PRETZL, NUTBAR, POPPS, Fizz, Gummo, Choco, Tater.
  - PUSH.
- **Flat or 3D:** flat (the front).
- **Close-up ready?** About 1,650–1,800 px/m, which is borderline. **FLAG:** $1.45 is high for 1990. Era check by 平面视觉.

### S3 — Copier: `Kit_Copier`
- **Status:** LIVE.
- **Where, how often:**
  - Office rooms: 60 % of rooms over 50 m², otherwise 25 %.
  - The Office_Bullpen_4x4 module.
  - Also a pile piece.
- **What a close look gives:** flavour.
- **Readable content:** READY 001, a 0–9 keypad with * and #, RESET, STOP, DARKER, LIGHTER, START, paper-size cards.
- **Flat or 3D:** flat (the top panel).
- **Close-up ready?** About 2,130 px/m. 平面视觉 already did the era fix.

### S4 — Desk phone: `Kit_DeskPhone`
- **Status:** LIVE.
- **Where, how often:** 45 % of equipped Office stations; also a pile piece (Small).
- **What a close look gives:** period detail. The dial letters are pre-1990s (PRS, WXY; no Q or Z).
- **Readable content:** keys 1–0 with ABC … WXY and OPER, then HOLD, XFER, CONF, REDIAL, MSG.
- **Flat or 3D:** 3D (lift and turn; 0.20 m).
- **Close-up ready?** Not measured at 0.3 m. It was built in round 2 (10-02), before the hero-LOD0 order.

### S5 — Wall clock: `Kit_WallClock`
- **Status:** LIVE.
- **Where, how often:** only in the module L0_WaitingRoom_4x3, at 2.1 m. That module is one of 4, and modules go into 30 % of rooms that fit one.
- **What a close look gives:** flavour. Time has stopped.
- **Readable content:** the hands frozen at 10:08:37, bar markers, no numerals.
- **Flat or 3D:** flat. It hangs above eye height, so the shot looks up.
- **Close-up ready?** Yes. The markers are geometry (538 tris) and have no texture.

### S6 — Key hosts: `Kit_KeyHookBoard` (Lobby) and `Kit_KeyCabinet` (Office)
- **Status:** MAIN-IDLE. Waits on D1.5. The number slot is missing, so the strips show blank paper.
- **Where, how often:** at the zone key's `KeySpot` (`host` field), hooks at 1.40–1.55 m.
- **What a close look gives:** a hint. Which hook is empty, and the numbers.
- **Readable content:**
  - The board: 8 paper strips, 01–08.
  - The cabinet: 24 hook numbers and a blank ruled index card in the door, which could carry a zone list.
- **Flat or 3D:** flat.
- **Close-up ready?** The mesh is (4,392 and 6,804 tris). The text is not.

### S7 — CRT TV: `Kit_CRTTV`
- **Status:** LIVE, in piles only.
- **Where, how often:** piles (35 % of large rooms, ≥ 60 % of Tall halls), often turned or inverted.
- **What a close look gives:** flavour.
- **Readable content:** a rating plate on the back: MODEL CT-2104 / 120V~ 60Hz 95W / SER. 0045519. The screen is dead.
- **Flat or 3D:** 3D (walk round; it is too big to lift).
- **Close-up ready?** About 2,000 px/m (a Prop_Label cell).

## 4. NO

| Asset (kit) | Status | Why not |
|---|---|---|
| Keyboard, mouse, PC (`Kit_Keyboard`, `Kit_Mouse`, `Kit_PCDesktop`) | LIVE | On every desk; 101 generic key legends. Inspecting them would cheapen the mechanic |
| Binders, paper stack (`Kit_Binders`, `Kit_PaperStack`) | LIVE | Generic spines (Q3 REPORTS, MINUTES) and a tab strip (CLIENT FILES); the pages are blank. **They become MUST** if narrative writes a page or memo for one copy in N |
| Filing cabinet (`Kit_FilingCabinet`) | LIVE | Every copy has the same drawer cards (A–C, D–F, G–K, L–P) and ASSET 004193. **It becomes SHOULD** if the cards vary per copy |
| Maker's labels on bookcase, credenza, dressers and bar stool | LIVE (piles, Storage module) | MODEL 4417-B / MADE IN U.S.A. / QC 03/1979, identical on every copy. It reads by walking up; the furniture is too big to turn |
| Ply cabinet asset tag, crate stencils (FRAGILE, THIS SIDE UP, KEEP DRY) | LIVE | They read from afar, or they are the same everywhere |
| EXIT sign (`Kit_ExitSign`, `_Dead`, the code-built Run signs) | MAIN-IDLE / not reached | One word that reads at 20 m, hung 2.0–2.4 m up. The rebuild spec is in `exit_sign/10_spec.md`; `Run_ExitSign_Dead` is missing |
| Doors: leaves, frames, locks, closers, exit device (G1/G2 kits) | MAIN-IDLE (map uses cube leaves) | E opens them. The lock already has its own close-up, the key-use head-dip shot |
| Windows, blinds, interior window (`Kit_WindowFrame_*`, `Kit_MiniBlind_*`, `Kit_InteriorWindow`) | LIVE (frames) / MAIN-IDLE (blinds) | Nothing to read. Hold E breaks glass and has its own micro-cutscene |
| Outlets (N3; 20 kits, `outlet_*.py`) | PIPELINE (clone; C1 done) | The era lock bans all marks, so there is nothing to read. About 34 per chunk. Drawn by an instanced renderer with no GameObject and no collider |
| Wallpaper print and pattern cues (WP03, Q1b, A + M4 + V1) | LIVE print / PIPELINE cues | The environment, not an object. The cue reads in motion and pausing would freeze it. The placard (M1) is where its language is taught |
| Furniture: seats, sofas, tables, lamps, urn, pouf, hat stand, cases, crates, pallets, desk, panels, posts, water cooler, trash bin, doorway studs | LIVE | Nothing to read |
| Title-stream code-built props: outlet plates (3 per Lobby room); Run gurney, chairs, IV pole and EXIT signs; Exit threshold | LIVE (outlet boxes) / not reached | Placeholder boxes. The furnished stream profiles never show |
| Troffers and lamps, the Relay | LIVE | Never pause on a threat. Light fixtures are not objects to read |

## 5. Facts the next stages must respect

**Hit targets.**
- Every MUST asset is render-only with no collider: the placard, sign, plate, key, tag and hosts.
- Outlets have no GameObject at all.
- Scratch-throughs are a shader layer.
- So inspect needs its own hit volumes; the aim ray today only sees colliders.

**E is taken (map chat).**
- Doors use E. Glass uses hold E.
- The CRT reads `Keyboard.current.eKey` itself, with its own raycast (`FrontRoomsScreenVideo.cs:133-141`). It bypasses `FrontRooms3DGame` input, the HUD prompt (its "E TURN ON/OFF" string is never shown) and the touch layer.
- The sign (1.524 m) and plate (1.000 m) sit on the door leaf, so the door collider wins the ray.

**Pause.**
- `FrontRooms3DGame` has `Phase.Paused` and the `Paused` event (map chat).
- Things that move during a look:
  - the placard glow (lamp-driven): freeze it;
  - the CRT video: one decoder per ad bank, so pausing one screen pauses every screen of that ad;
  - the wallpaper cue clock.
- Open for Red and the map chat: may the player inspect during CHASE? A pause there would be an escape.

**Supply.**
- The placard is the showcase, but there is 1 per run, at the start door. If Red wants it seen more often, sheets A-1, A-3 and A-4 would be new copies (narrative + 平面视觉 + a map placement rule).
- There are no notices, memos, posters or office signage. The paper stack and binders can carry them.

**Owners.**
- Every string on an inspect target goes through 平面视觉 (faces per `FONTS_PERIOD_1990.md`).
- Prompts and any inspect UI are 平面视觉's and the map chat's.
- Touch input is the touch session's.
- Close-up framing and the close-up look are ours.

**References for the close-up look.** Figma PROP KIT 2324:852 holds the K46–K80 three-views (doors, locks, keys, tags, hosts, windows). Three-views for the placard and the outlets are still owed to 平面视觉.

**Small finding.** The docstring in `Tools/Blender/frontrooms_kit/assets/_storage_labels.py` still lists cells 4–5 as "1994"/"1995". The texture says 1989/1990, so only the comment is stale.

## 6. Counts

| Class | Families | LIVE | MAIN-IDLE | PIPELINE |
|---|---|---|---|---|
| MUST | 5 (M1–M5) | 0 | 3 (M2, M3, M4) | 2 (M1, M5) |
| SHOULD | 7 (S1–S7) | 6 (S1–S5; S7 in piles only) | 1 (S6) | 0 |
| NO | 13 rows (≈ 90 kits + code-built props) | most | doors, EXIT, blinds | outlets, cues |
