# 60 — Door vision: the landing (set A, the PEEK, the break keeps the lite)

- **Line:** 游戏视觉, session local_a9a1dce2, account B, 2026-10-30. Build stage 17:3x to 18:04, network lost, finished 20:2x to 20:4x.
- **Status: READY, not applied.** Built and tested in `W/proj_doorvision` (W = `/Users/redwang/FrontRoomsVisualWork`). Nothing in main changed except this file.
- **Package:** `W/apply/door_vision.sh` (v2). 64 paths: 38 replaced, 26 new, 12 `.meta` kept. Bases are main as of 20:41 (Game b88052b7, MapHunter 63ab6122, MapWorld 6f741c2e).
- **Contracts:** `W/apply/door_vision_contract/` (`README.md`, `CONTRACT_map.diff`, `CONTRACT_interaction_peek.diff`, `contract_edit.py`).
- **Sheet:** `W/door_vision/land/door_vision_landing_sheet.jpg` (Shot format). Frames in `W/door_vision/land/out/`.

## 1. What was decided, and what is built

Red's lite set A, 10-30 17:3x, "A 按区域四种". Every lited double-acting door, the map's included (Red 10-30 12:29).

| Zone | Form | Doors | Peek |
|---|---|---|---|
| Lobby | **F4b** 10 x 48 in, centred on the leaf, glass 45 to 93 in | title pair, station entrance skin | none |
| Office | **F1** 10 x 30 in (DA-L1, unchanged) | Veneer_DA, Veneer_Oak_DA, Steel_DA, Steel_PaintedMetal_DA | yes |
| Run | **F5** 15 x 15 in, low, 6 screws | Ward_DA, SteelRun_DA (new) | yes, eye held at about 1.50 m |
| Exit | **F13** 4 x 25 in wired slot | the C22 steel EXIT door and its sealed twin (reverses 30_design §1.1) | none |

The peek follows C36, A43, A42 and 交互设计's notes:

- The verb is PEEK: "E  ·  PEEK", "A  ·  PEEK" on a pad, PEEK on the USE disc. LOOK THROUGH is gone.
- It is offered only within 1.0 m of the glass, on shut leaves. Beyond that the leaf is OPEN.
- The world runs live: timeScale 1, doors and hunter tick. No threat gate, no forced pull-back.
- It ends at once when its leaf moves. No TEXT, no panel, no PAGE, zoom off.
- The Relay's own sight stops at the leaf collider. The wired lite has no hold and no BREAK GLASS.

**The break keeps the lite.** `door_break_lite_bases.py` rebuilds the break bases as the intact lited leaf minus the cassette window (Z 0.60 to 0.995), so glass, bead and wear stay on the same vertices. Ward and SteelRun get their own bases and share their parents' cassettes. `FrontRoomsDoorLite.AdoptBreakBody` moves LIVE / DARK onto the body at the swap and back on a restore. The lite never reaches the window: its frame ends at Z 0.5495.

**One shadow mesh per form.** `HoledShadowBox` caches by rect, about 32 tris each, no extra caster. The previous stage's renders had this switched off; it is back on.

**`Kit_DoorLeafBreak_SteelLite_DA` stays as landed.** Nothing hangs on it today, because `Kit_DoorLeaf_SteelLite_DA` is not a dressed row.

## 2. Budgets per form

Triangles per LOD against main's unlited kit. Budget from 51: walnut +400, steel +650.

| Kit | Form | LOD0 | LOD1 | LOD2 | Added at LOD0 |
|---|---|---|---|---|---|
| Veneer_DA | F1 | 2,736 → 2,944 | 1,068 → 1,228 | 148 → 188 | **+208** |
| Veneer_Oak_DA | F1 | 2,742 → 2,950 | 1,068 → 1,228 | 148 → 188 | **+208** |
| Ward_DA | F5 | 4,136 → 4,720 | 1,156 → 1,316 | 172 → 212 | **+584** |
| Steel_DA | F1 | 4,968 → 5,536 | 1,580 → 1,724 | 172 → 212 | **+568** |
| SteelRun_DA (new) | F5 | from Steel_DA → 5,536 | 1,724 | 212 | **+568** |
| Kit_DoorLite_Exit (new) | F13 | 548 | 124 | 20 | 548 on the EXIT door |
| Title pair (C#) | F4b | one 12-tri pane per leaf, plus the bead in the leaf's own mesh | | | no extra opaque draw |

Ward_DA's +584 is over the walnut figure on paper; it is a laminate leaf with armor plates, and 51 lists it at this number after the cut from 8 screws to 6, which Red took.

Break bases, LOD0: Veneer 1,746 → 2,754; Ward 4,386 (new); Steel 5,262 → 5,466; SteelRun 5,466 (new).

Draws: each lited leaf adds one `Glass_Wired` slot per LOD renderer, no opaque draw (40_prototype G3, unchanged). The census on this run's map: 131 lites, 125 live, 6 dark, fade pool 4 free.

## 3. The one test round

| Test | Result | What it proves |
|---|---|---|
| Roslyn, main + payload, macOS / iOS | **COMPILE OK / OK** (Assembly-CSharp 174 files, Editor 75, 0 errors) | The package stands alone, without the contracts |
| Roslyn, main + payload + contracts, macOS / iOS | **COMPILE OK / OK** | All three land together |
| Same tree with `FRONTROOMS_RELAY_V2` defined, macOS | **COMPILE OK** | The four `.V2.cs` lines of the map contract compile (they sit behind the define) |
| FrontRoomsInspectTests | **150 / 0** | Every close-up's rules still hold with `InspectKind.Peek` appended |
| FrontRoomsInspectPlaytest | **166 / 0** | Real close-ups in play, A43 live and paused paths |
| Door peek checks (`FrontRoomsDoorVisionLand`, desktop) | **39 / 0** | See below |
| Door peek checks, phone | **3 / 0** | USE reads PEEK at 0.95 m, OPEN at 1.2 m, the peek opens live |
| StreamVerification | **5 / 0** | The title stream with lites on its doors |
| FrontRoomsDoorShotTests | **252 / 0** | The door shots on lited leaves |
| Exit run, seed 2554, station 4 | **PASS, ended by exits** | 44 checks ok, 0 not ok (27 exit, 15 station, 2 door), 0 errors, not caught, floor 4, p95 16.7 ms (Editor) |
| Break film, veneer (`-dbMode dry`) | **7 of 7 stages hold the lite** | S0, E, M, D1, D2, FINAL, BROKEN: glass drawn 3 of 3 LODs, LIVE; in the break body from M on |
| Break film, steel (`-dbMode smoke3`) | **7 of 7 stages, plus a kept D2** | The same on `Kit_DoorLeafBreak_Steel_DA`, and the steel restore |

Compile errors, shader errors and NullReferenceExceptions: 0 in all nine Unity logs.

The door peek checks, by name:

- **Offered:** at 1.000 m and 0.95 m (`E  ·  PEEK`), not at 1.2 m (`E  ·  OPEN DOOR`). Never on a moving leaf: 47 moving frames, 0 offered.
- **Live:** timeScale 1, the run clock +2.000 s over 2.0 s, the hunter's clock +2.000 s, doors tick.
- **A Relay on screen:** the prompt stays and the peek opens with it in view. It held 120 of 120 frames with no pull-back.
- **The peek ends:** frame 1 after the leaf moves, frame 1 after a strike.
- **The Relay's sight:** 0 of 120 frames see the player, while it stood on screen in all 120.
- **Run eye:** 1.497 m on Ward and SteelRun, each with its own holed shadow.
- **Break swap:** glass in the body, LIVE; DARK still drives it; no peek on a damaged door; the sight rule survives.
- **Not a window:** no hold on the leaf.
- **Exit:** slot 4 x 25, no peek, stair plates clear the frame by 20 mm, the sealed twin's dark slot lines up.

**Wire sharpness, re-measured with film grain and dither off, as 40_prototype asked:** peek **27.4 %** local contrast (target ≥ 10 %), 1 m **10.5 %**, repeat-render noise 0.00 %. The old 4.1 % / 9.5 % was the measure's fault, not the wire's.

Harness fixes during the round: three, all clone-only, none in the package.

- "The hunter ticks" read a Dormant Relay. On floor 1 the game does not tick an unreleased Relay at all, so the check now releases it far off for the two seconds.
- The lobby frames used the wrong height.
- The lobby frames used the wrong normal.

## 4. Package and contracts

- **Dry run on main:** `DRY RUN OK: every base matches; run with --apply to perform the actions above.`
- **Rehearsal on a scratch copy of main's Assets and Tools:** `MANIFEST OK: the target equals door_vision_expected.txt for all 64 touched paths; 12 kept .meta files unchanged.` A second run says ALREADY APPLIED.
- **Contract checks on scratch copies of main's files now:**
  - `CONTRACT_map`: patch --dry-run rc 0, git apply --check rc 0 (15 hunks, 5 files)
  - `CONTRACT_interaction_peek`: patch --dry-run rc 0, git apply --check rc 0 (7 hunks, 6 files)
  - Both applied together equal the clone's ten files byte for byte.
  - `contract_edit.py map` on a fresh copy of proj5's five files (V4b in progress): every anchor found, rc 0.

**What goes live with the package alone:** every map door shows its lite and offers PEEK within 1.0 m; the EXIT has its slot; the break keeps the lite.

| Missing | What the player gets |
|---|---|
| Map contract | The title pair stays DARK. A Relay in view hides the prompt, so C36 does not hold yet. A Relay behind glass does not count as seen, so it can step in behind a lite in view. |
| Interaction contract | The disc reads READ, the HUD shows the READ glyph, the camera stops 0.175 m off the glass. |
| M8 | No map door is a Run door. |

None of these breaks anything.

**The Relay-sight part of the old contract is dropped.** `SeesForTools` and `LitePassesForTools` duplicated nothing useful once V4b owns sight, and nothing here touches `Sees`. The door collider stays solid, so a lite is opaque to the Relay with or without V4b. What stays is the player's side (`VisibleToPlayer`), which V4b does not write.

**M8 is inert, plainly.** `DoorMemberFor` returns only Lobby or Office today, so **F5 appears in no real run until M8**. The package ships F5 ready and the hook `DoorMemberFromMap`. The contract adds `IsRunDoor(door) => false`, so landing it changes nothing either: 关卡设计 and 系统设计 still have to write which doors are Run doors. 关卡设计 lands it after V4b. The Run frames on the sheet force the row through `DoorMemberOverride`.

## 5. What Red will see

1. Walnut office doors with the 10 x 30 lite, the rooms behind them showing through wired glass.
2. The title pair with two tall dark lites; they turn clear when the door starts to open, once the map contract lands.
3. Within 1 m of the glass, E · PEEK. The camera goes to the glass and the game keeps running, with the Relay walking behind the mesh. The player decides when to back off.
4. The Relay breaks a lited door and the window stays in it through every stage.
5. The C22 EXIT door has a thin wired slot. Run doors with the square lite exist but appear nowhere yet (M8).

## 6. Not done

- **Not applied.** No LANDINGS or MERGED edits, by the brief.
- **Ward and SteelRun breaks are not filmed.** Their bases are generated, and the rule of the swap is the same code path as veneer and steel. No map door uses them before M8.
- **The exit's light is a clone capture of the maze side.** The stairwell's steady light through the slot from inside the stair is not on the sheet.
- **Title pair is shown DARK only.** The LIVE moment is in motion, as the door opens. No separate frame.
- **No Figma or VL entry** (batched daily, rule 23).
- **Focus loss during a peek: CLOSED** (交互设计, 2026-10-30): no code needed. The peek pauses and the resume returns into it through Game's A43 path. A Duo unfold in a peek leaves it, then opens settings, through their R6 path (checked in the layout tests, not on a peek).
- **M-L4 `CellReadyForLite` is not written.** A map lite uses the stand-in, LIVE once both chunks are built.
- **wallmat_l0 landed at 20:38, after the Unity round.** It touches no path in the package or contracts, and the final Roslyn pass includes its RenderSetup. The sheet shows the wallpaper as it was before it.
- **The clone's audio is not re-synced to 18:00.** Sound's landing touches no file in the package or contracts; Roslyn on main's scratch tree includes it, and the Unity round does not.

## 7. Asks for 声音设计 (in writing; rule 24, this line made no sound)

1. **The live close-up mix: confirm only.** Your code 2026-10-22.14 (LANDINGS 18:00) keys the mix on `FrontRoomsInspect.CurrentPauses`. The peek reports false, so it should stay in the live mix with no change. Please confirm on a peek by capture once the package lands.
2. **Glass-muffled hearing.** With the map contract, `PlayerSeesRelay` turns true through a lite, and `StepsAudible` (`FrontRoomsMapHunter.cs`) returns true on it. So a Relay seen through a shut door becomes audible as if nothing stood between. Please keep the door's occlusion on those steps. You can read `FrontRoomsInspect.PeekActive`, `FrontRoomsInspect.PeekLite`, and `FrontRoomsDoorLite.PassesPlayerSight`. If you need a plain flag (`PlayerSeesRelayThroughLite`), that is a row for 关卡设计; it is not in the contract.
3. **The peek's in and out.** The sidecar says `"sound": "glass"`, but `InspectSound` has no Glass, so it falls to Plastic today. Either add it (a soft touch on glass) or tell me to change the sidecar.
4. **The ear at the door.** While peeking, the far room a little less occluded. Your call.
5. **The break.** The wired lite does not break (A42). Any tick on the glass at FINAL is your call; no shatter.
