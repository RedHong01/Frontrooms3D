# 20 · Tutorial: requests to each line

Written 2026-10-22 by 教程设计. These implement `10_proposal.md`.

**APPROVED by Red, 2026-10-22,** in the 教程设计 chat (`local_c160c950-9b0e-4f15-ad77-95c2e4930c87`): "全部按推荐（T1=A，T2=E1，T3 静止屏，T5 去掉文字卡，T6 标题电视带声音）".
- Every row below is approved, except **V7** (E2 was not chosen).
- A message from me is not Red's approval (COMMON rule 15); each owner checks Red's words in this chat.
- **2026-10-29 evening (总控 A tracking, switch #8): the first room is LIVE in main.** Checked in LANDINGS, with the hashes spot-checked.
  - Switch-on 19:17: Level0 stationModule = Station_FrontDesk_4x4. It brings A26 + M2, M3, M4 (floor 1), M5, M6, M7, Red's Q1 = b and A35 (B floors: EXIT unlocked, no zone keys). MIT 373/373; autopilot exit + station, seed 2554, PASS.
  - Visual side: V1 station-look-v1 19:11, A13 exit chain 18:54, station entrance frame 20:14.
  - C-1 take spot 19:22. C33 Tall-hall arches 19:33 (R7 Tall 41.9 → 5.0 %).
  - crt_tube (title TV, T6 side) 19:59. D1.6 door/lock/key kit 20:07 + C-2 map contract 20:23 (real keys, incl. the station key on its hook).
  - Sound: S2/S3/S5 landed 17:24; S4 waits for V2.
  - Still open: V2 TV collapse, V6/G3 landing copies + floor numbers, the station paper atlas (alarm plate / register), L10 first arrows on floor 1, and stations on B floors (M4 B1+, not planned).
  - **M7 REPLACED (Red 10-30 11:38, 关卡设计 chat, verified there via AskUserQuestion):** "我不希望在玩家onboarding的时候就被追逐，最开始的阶段应该是玩家开始熟悉游戏的阶段". Floor 1 sends no hunter until 150 explored floor-1 cells OR 3 min after the start door shut, whichever comes first. The run's first real chase keeps the gentle arrival (C32) wherever it happens, B1 included. This replaces the Left (key + 2 cells out) / 150 s / A15 gate. **LANDED 2026-10-30 12:41 as A40** (MapHunter dc88b1df, Game e51939cc, RelayDirector 2c23228d; 系统设计 systems.json 2026-10-30c). Floor-1 copy: research/narrative/60_onboarding_floor1_copy.md (叙事). §10's "EXIT within N s of the take" metric now also needs "time to the first send".
  - **T6 title TV LANDED 21:15** (Kit_CRTTV on a bracket, title room 1 end wall, Attach TitleTV; RoomStream adb9f3fd).
  - Teacher-notes gap check: `Documentation/Takeover_2026-10-18/reviews/TEACHER_NOTES_GAP_20261029.md`. **§10 metrics need a new measure:** "station within 90 s" is trivial now; use "EXIT within N s of the take".
- **Amended 2026-10-22 15:3x (first room):** Red, 叙事设计 chat 15:24: "按推荐，第一个房间选方案 1" (option 1 of `research/station/NOTE_1022_first_room_tutorial.md`). Rows M2, M4, M7 and V1 changed (marked). Owners check Red's words in that chat (transcript 23146057).
- **2026-10-22 15:39: C22 LANDED in main** (关卡设计: EXIT + stairwell + floors, C23 step 1, 叙事 strings v1.1; Game 670219ab, MapWorld 10f71ffc, `FrontRoomsExitPlanner.cs`, `FrontRoomsStairwell.cs`; lean suite green incl. EXIT 300/300 and -autopilotExit 2554 over three floors). The EXIT is unlocked until M3. Phase 1 rows (M3–M8) are unblocked on the map side. V1's lens hook `_FR_LampDip` landed 15:40. The phone key-tap line is held: 6 broken-door touch checks fail with it (with 交互设计).
- **2026-10-22 16:3x, L11 (stairwell = safe zone; Red, 游戏视觉 chat 15:47):** taught by the world, not text (`10_proposal.md` §1 L11). Owner asks: 关卡设计: the Relay never enters a stairwell, and when it chases the player to the stair door it **stops at the door in sight or earshot and turns away** (no clipping, no vanishing), so a chased player learns the rule by surviving it once. 声音设计: the hunt sounds fall away once the steel door latches; no warning plays inside. 游戏视觉: steady stairwell light (look A); no dips through the door. No placard line is added.
- **2026-10-22 15:52–16:05, landed in main** (LANDINGS, checked by 教程设计 after switch #4):
  - **G1** (关卡设计 15:52): `MapRootFor` prefers a Standard root whose door cell's west edge is Open, so the placard's P1 west exists; the validator fails a seed without it.
  - **`FrontRoomsHudStyle`** (游戏视觉 15:52): X2's keyline gate.
  - **G1b + stairwell safe zone + L11** (关卡设计 16:05): floors 2+ use the west stair-door placard spot. The Relay walks to its stand at the stair door, stops 1.2 s facing it (`StoppedAtSafeDoor(feet)`), then walks 4 m off (`SafeDoorBeat`, max 8 s), never parking or vanishing meanwhile. The seal waits for that beat. Tests: interaction 279/279, 100 seeds, EXIT 300/300.
  - **L11 sound** (声音设计 17:06, CodeVersion 2026-10-22.4, play-mode check 12/12): in the sealed stairwell the player hears the hunter's real steps reach the EXIT door (muffled by it), one dragged step as it stops, then the steps going away. Hum out, Tension at its floor, no WarnBurst groan or caption. Matches `10_proposal.md` L11.
  - **游戏视觉 17:4x:**
    - the Bayon `·` fallback LANDED 17:44, so X2's last visual gate is cleared;
    - V2 + V3 (CRT beacon mode, light, VL245) are running as wf_baf2df8e-59f;
    - V1 follows 关卡设计's final first-room plan (FD-b; plan §4.9).
    - **平面视觉 18:0x:** the regenerated underprint is in the repo (Phase 0 md5 be0a97b2 / 2654943a; expected dark ratio about 1.5). Its quality call: the sheet **lands at 游戏视觉's repack**, even if the in-game number misses 1.30, which is then recorded as a temporary exception. No hold into the playtest window.
    - **The Phase 0 sheet swap was on HOLD (superseded by the line above):** it fails the dark check (A4 0.920 vs the 1.30 bar; v3 reads 1.430), and 平面视觉 is asked for paler glow ink. 教程设计's input: the live v3 sheet calls the start door EXIT on every run since C22, so if the regen takes more than about an hour, land Phase 0 with a recorded temporary dark-ratio exception and follow up.
  - **M1 LANDED 17:37** (关卡设计 + 交互设计 joint, LANDINGS): the CalmHint card is gone on every platform; desktop `ControlsLegend` "WASD  MOVE    MOUSE  LOOK    SHIFT  SPRINT    E  USE" (Game :186). Verified by 教程设计 in code.
  - **X2 LANDED 20:2x** (交互设计, LANDINGS): phone title/pause legend + stick teaching words (SLOW · UNHEARD; RUN preview), retiring after a counted creep once the Relay is out, or on the first crossing / after 4 s. Settings › STICK HINTS brings them back. All three of 教程设计's build notes are in. iPhone, Android 20:9 and iPad 153/153 each; VL415–VL416; rules in INTERACTION_DESIGN.md "X2 on phones".
  - **Not landed yet:** the Phase 0 sheet swap (lands at 游戏视觉's repack, above), M3–M8, V1–V5, S2–S5.
- **2026-10-22 20:41, V4 LANDED** (游戏视觉, v4_desk_still, 21 paths; Roslyn macOS + iOS 0 errors). Inert until 关卡设计's M4 adds **`FrontRoomsMapWorld.FloorHasStation`**; until then desk CRTs keep Ad_03 and the title TV is unaffected (the staging decided 10-22). Phase 0 placard repack restarted (wf_102c411b-5ed), first-room 61 rev 3 finish restarted (wf_e0585168-580).
- **2026-10-22 21:03, Phase 0 placard LANDED** (游戏视觉): the start-door sheet is now sheet A Phase 0 with G1, so v3's wrong rule (start door = EXIT) is gone. Blackout reading at the usual start spot (277, 6): 1.466 / 1.310 (bar 1.30). At the dimmer spot (85, 2) it landed as the agreed exception; 平面视觉 shipped a 5 % lighter band ink (~1.32 predicted), repacked as a texture swap. The partial-dark case at (85, 2) stays a recorded exception, as with v3. Line 4 stays `EXIT BY STAIR, / ONE FLOOR DOWN.` until M3 locks the EXIT.
- **2026-10-22 21:11, Red's first-room answers** (游戏视觉 chat, all as recommended; recorded at the top of `research/station/61_for_red_first_room.md` and in systems.json z20): (1) **N = 2, with the key condition kept** (M7); (2) **the key-take cluster first, then the Floor 1 first room + start group + M3/M6/M7 land together as one set**, after whole-set verification; (3) the station light layer: test first, turn it on for all platforms if the TV light leaks; (4) Floor 2+ stations: direction B on Level 0 floors, A on Office floors, walls 2 everywhere. §18 downloads were not approved.
- **Order:** Phase 0 rows first; Phase 1 rows after 关卡设计's C22 stairwell minimum. All after the Alpha. Each owner lands through its own claims in LANDINGS.md.

**Columns.**
- **Ph** = phase: 0 needs no new system; 1 is the Option A core; 2 is later.
- **Days** are ESTIMATE, for the owner to correct.
- Everything lands after the Alpha.

## 关卡设计 (map, game loop, Relay code)

| # | Ph | Request | Needs | Days |
|---|---|---|---|---|
| M1 | 0 | Remove the `CalmHint` card ("Shift to sprint…", 9 s). Show the controls once on the title and on the pause card (WASD · MOUSE · SHIFT · E). Touch hints are 交互设计's. **Any option** (T5) | 平面视觉 type | 0.25 |
| M2 | 0 | **(amended 10-22)** Placard mount **in the first room**, in the first sightline after control returns, `YOU ARE HERE` at the desk; 平面视觉 redraws the route and the mount (supersedes G1's P1 west of the start door). G2 unchanged: the start door never shows a padlock or an unlock prompt. The mount code is 游戏视觉's (placard `10_spec.md` §5.3) | 游戏视觉; 平面视觉 art; first-room geometry (M4) | 0.25 |

**M2 interim (decided 10-22 15:5x by 平面视觉, recorded by the tutorial tracker):** until A26's first room lands, Floor 1 shows **sheet A Phase 0** (`Tools/print/ink/art_from_graphic/placard/sheetA_phase0/`, print 9d52bba0) at today's start-door mount, **only in the same landing as G1** (MapRootFor keeps P1 west open, 100-seed check), so the YOU ARE HERE dot is true on every seed. Without G1, keep v3. Phase 0's line 4 `EXIT BY STAIR, / ONE FLOOR DOWN.` holds until M3 locks the EXIT. It retires on Floor 1 when the first room's M2 mount lands with the redrawn sheet. The B1 stairwell copies can go in now (C22 in main).

| M3 | 1 | **Locked EXIT:** the floor's EXIT door is locked; the floor key opens it with the existing unlock shot. **Floor key:** one per floor, at the station's KeySpot; it opens every locked door on its floor. Retires zone keys (C15 change, recorded by 系统设计). No locked interior doors on Floor 1 | C22 minimum; 系统设计 record | 1 |
| M4 | 1 | **Station placement (amended 10-22):** one station per floor; kept through revisit shifts. **Floor 1: the station is the first room**: the first map room behind the start door, module Station · Front Desk, a forced stamp at the start tested in `MapRootFor` (map cells, not a reserved start area), the Post Room walls by the per-cell skin override with 游戏视觉's surface set; the EXIT 3–6 cells from its doorway **in a straight sight line** (C24's 14–22-cell band does not apply on Floor 1). The stamp fixes: the placard mount wall (the same wall on every seed) and the EXIT's side seen from it (otherwise Floor 1 uses sheet C at run time, Phase 2); G2's two openings (not the main entrance), one on the EXIT sight line and one toward map cells that give C32's 22–30 m far-side arrival; ≥ 2 live lamps on the walk to the EXIT; no Arrive cell inside the room; fits one chunk. Floor 2+: 10–20 cells from the arrival, off the EXIT's shortest route, ≥ 6 cells from it. Never Tall; reachable without glass or keys (L5). Expose a "floor has a station" query (V4 gating). Geometry: `station/NOTE_1022_first_room_map.md` | 游戏视觉 modules (V1); L5 | 1 |
| M5 | 1 | **Beacon field:** a path field seeded at the station TV's cell (C24's machinery). Reach about 90 m; glass impassable; counts shut openings; rebuilt only on topology or opening changes. Public read: `BeaconPath(playerCell, out pathMetres, out shutOpenings, out portalPos)`, where `portalPos` is the first opening leaving the player's room set. Beyond the built map: the nearest built border doorway, as C24 Per A20 (Red, 10-22), the C24 calm route reads this station field until the key is taken, then the EXIT field | M4 | 0.75 |
| M6 | 1 | **The take:** `StationKeyTaken(stationCell)` event, which the TV (V2) and the sound (S3, S4) listen to. On v1, a Noise at the station with unlimited radius, so the Relay walks there (Hunt). On v2, a device call | M3, M4 | 0.25 |
| M7 | 1 | **Floor 1 send (E1, amended 10-22):** the Relay is not released until `StationKeyTaken` **and the player has left the first room** (the player's cell first leaves the first room's cells through any opening, the start door's 4 m shut never counts, and they are 2 cells out: N = 2 with the key condition kept, Red 21:11), with a fallback of 150 s (SP) after control returns or entering a third zone. It then arrives out of sight at **22–30 m of walking from the player**, on the far side from the player's route to the EXIT, reached through the first room's second opening (G2), never through the start side, never inside the room and never within 2 cells of the route (C32 as amended by Red's option 1). Tune so the first W1 burst lands on the walk to the EXIT or during the unlock. Fallback ladder: 18–34 m, then ≥ 14 cells, logged in M8. Its first leg is the Hunt to the station. Floor 2+ keeps ≥ 14 cells (42 m), v2's Arrive rule. **First-approach grace, Floor 1 only:** no chase before the player has had ≥ 1 W1 burst, unless within 6 m | M6; T2; 系统设计 records the C32 amendment | 0.5 |
| M8 | 1 | Autopilot report fields (amended 10-22): Space → take, take → leaving the first room (the send gate), leaving → EXIT, take → unlock, whether the EXIT was tried before the take, EXAMINE on the placard, first W1 burst before first chase, the send's ladder rung and N (`10_proposal.md` §10). The baseline bots (C05) log whether E1 and the Floor 1 grace are on, because both change v1's release and sight | — | 0.25 |

**M7 note (C32, answered):** Red, 2026-10-22 in 系统设计's chat: "C32 选 1：第 1 层第一次派怪物改到拿钥匙后 1 秒、在 22–30 m 处出现". Recorded in systems.json 2026-10-22v. M7 above is updated; the station's ≥ 2 Steady lamps are V1's.

**Status from 关卡设计 (10-22):** it checked Red's words in this chat and accepted M1–M8 as specified, with no number changes; all after the Alpha.
- **Phase 0 first:**
  - M1: CalmHint out, plus the T5 controls line in 平面视觉's type;
  - M2: P1 west in `MapRootFor`, and the start door never shows a padlock or an unlock prompt.
  - **Superseded in part by Red's option 1 (15:24, A26):** P1 west is now only the interim Phase 0 mount (G1, landing with the Phase 0 sheet); the final M2 mount is in the first room. M4 is the forced station stamp on Floor 1; M7 gates on leaving the first room; 游戏视觉 ships three station modules (adds Station · Front Desk). 关卡设计 took M2/M4/M7 at 15:4x (re-ack in its handoff).
- **Phase 1 sits on C22,** which is in its clone now: EXIT + stairwell + NextFloor, with the EXIT unlocked and a lock seam kept for M3. Then M3–M8 in order.
- **Module hand-over is settled:** 游戏视觉 ships the two station modules as new files. M4 places them; they never go into the random module pool.
- C24 and M5 share the field code and are built together.
- 关卡设计 reports when C22 lands and as each M row lands.

## 游戏视觉 (3D, kits, CRT and ScreenVideo, merges)

| # | Ph | Request | Needs | Days |
|---|---|---|---|---|
| V1 | 1 | **Station modules (amended 10-22):** **Station · Front Desk** for Floor 1's first room, the building's front desk at the main entrance, **with its own walls (not Level 0)** and a seam rule where it meets Level 0; main entrance doors locked (`narrative/40` §9). Floor 2+: Station · Waiting (Level 0) and Station · Desk (Office).<br>• Kits: `Kit_CRTTV`, `Kit_KeyHookBoard` with `Kit_KeyRing` and a tag, chairs, a desk lamp.<br>• One KeySpot on the rack.<br>• The TV side's lamps Dim or Off; **at least two Steady lamps on the entrance side** (changed 10-22, so a W1 burst shows in the station).<br>• The TV light along the doorway axis (§4.6); the key light is the subject at the take (§4.7) | Key model (D7) on a hook; first-room research `station/61_for_red_first_room.md` | 1.5 |

**V1 walls (Red, 游戏视觉 chat 15:44, "这个版本可以" on `first_room_s4_walls2_post_room.jpg`):** every station module on every floor (Front Desk, Waiting, Desk) uses **walls 2 · Post Room**: cool blue-grey paint #9AA3A6, a 6 in navy-charcoal rubber base, VCT floor, bronze door frames (叙事 R1: one look for the building's post). This replaces the Level 0 / Office themes listed in the row above. 游戏视觉's `research/station/61_*` is now a production spec, not a decision page.

| V2 | 1 | **Beacon mode for the station TV:**<br>• the ad plays with its light (V3);<br>• on `StationKeyTaken`, the picture collapses to a dot and fades (0.4 + 0.6 s), then stays off | M6 | 0.5 |
| V3 | 1 | **TV light:**<br>• colour from the video's mean colour, intensity from its luminance (0.1 s sampling, τ 0.12 s);<br>• range about 6 m; soft shadows on desktop, none on WebGL;<br>• ≤ 3 swings of 10 % or more per second; Reduce flashing τ 0.5 s | — | 0.5 |
| V4 | 1 | **Still desk screens:** on a floor whose key is untaken, Office CRTs show still, dim screens (`screens/30_screen_strings.md` §6) instead of the ad. No motion, no sound (T3) | 平面视觉 · 广告 art | 0.5 |
| V5 | 1 | **Title corridor TV with sound:** route its ad audio to 声音设计's event (T6). The surface keeps `audioOutputMode None` | S1 | 0.1 |
| V6 | 2 | Placard copies on each stairwell landing (same kit, new art per floor); the painted floor number on the landing wall | C22 stairwell look | 0.5 |
| V7 | — | **Not chosen** (T2 = E1). Kept for the record: (E2) a one-time black-and-white security monitor at the station showing the hunter cross a corridor | Hunter look final | 1.5–2 |

**V1 note (教程设计, 10-22):** the rack's warning plate (`10_proposal.md` §4.7) reads only in EXAMINE or within about 1 m. Its EXAMINE aim box must never take the aim from the key (aiming at the key always gives E · TAKE KEY). The plate stays after the take; the conduit to the annunciator (`station/30_look.md` §4.3) carries the wordless hint.

**Status from 游戏视觉 (10-22):** it checked Red's words in this chat and accepted V1–V6, plus the placard mount code for M2 (`FrontRoomsPlacardMount.cs`; P1 west for the interim Phase 0 sheet, then a fixed wall of the first room per `station/NOTE_1022_first_room_placard.md`; G1/G2 and `MapRootFor` stay 关卡设计's). V1 to re-estimate for Station · Front Desk (Post Room walls, surface set, light-layer fix).
- **Order:**
  1. V2 + V3, together with its pending CRT first-frame fix (VL245), as one workflow on `FrontRoomsScreenVideo.cs`. A public `Collapse()` stays inert until M6 calls it, so it can land before M6.
  2. V1 in a clone; the module asset hand-over into `Assets/Levels` goes through 关卡设计. The key on the rack waits for D1.6's KeyAssembly.
  3. The M2 mount code.
  4. V4 after 广告's still-screen art; V5 after S1; V6 after the stairwell look.
- **Days (its ESTIMATE):** V1 1 (+ the key pose); V2 0.5; V3 0.75 (shadowed light cost check + a WebGL tier); V4 0.5; V5 0.1; V6 0.5; M2 mount 0.25.
- Slots start as the machine frees up; 游戏视觉 reports each landing.

## 声音设计 (FMOD; not running: inbox note)

| # | Ph | Request | Needs | Days |
|---|---|---|---|---|
| S1 | 1 | **The WTCZ loop** for the beacon and the title TV: station ident, then the FRONT ROOMS jingle, matched to the 16 s ad. Start from the synthesised promo stems (`Tools/screens/promo_audio.py`). Strong bass and kick for the far bands; a mid-range hook that becomes clear near. **No bell, glock or chime timbres** (the lock-on chime stays the only tonal chime) | 广告 for content | 0.5 |
| S2 | 1 | **`Screen/Beacon` event:**<br>• 3D at `portalPos` (or the TV when in the room or in sight), with a 0.3–0.5 s crossfade when the doorway changes;<br>• parameters `PathMetres` and `ShutOpenings` drive gain, low-pass and wet per `10_proposal.md` §4.5 (−5 dB and cut-off × 0.6 per shut door);<br>• slew 0.5 s;<br>• the set's whine within 2 m | M5 | 0.75 |
| S3 | 1 | **Priority:** the beacon ducks −6 dB under the ballast groan, W2 steps and hum, and the lock-on chime; −12 dB in a chase; it pauses with Esc and EXAMINE | W2 bus | 0.25 |
| S4 | 1 | **Take beat:** the power-off zip at +0.4 s on `StationKeyTaken`. No CALLED tick: on a device call the device's own sound replaces it (RELAY §6) | M6 | 0.25 |
| S5 | 1 | **Captions:** `[TV, muffled]` with a direction tag from the doorway position (at most once per 20 s), `[TV]`, `[TV switches off]`. **Handheld only** (`FrontRoomsHandheld.Active`): far-band cut-off floor about 600 Hz and +4 dB (SP); desktop unchanged | 交互设计 check | 0.25 |

- **2026-10-22 16:2x, S1 content DONE (声音设计):** `~/FrontRoomsSoundWork/tutorial_s1/out/wtcz_loop.wav` (+ README, generator): 16 s seamless loop at 120 BPM cut to Ad_02/Ad_03 (hits on the 3/7/10/14 s cuts, ident 14–16 s), derived from 广告's synthesised promo music, no downloads; no bell/glock/chime, no 520 Hz alarm, no voice; far bands keep the beat (250 Hz line 5× median, 16 dB swing). Blocked: S2 `Screen/Beacon` on M5 BeaconPath; S4 on M6 StationKeyTaken; T6/V5 on 游戏视觉 naming the sounding screen. Open with 游戏视觉: `FrontRoomsScreenAdBank` pauses the shared video when no screen is visible, but the beacon is heard through walls, so the audio must be the clock (picture follows audio time or keeps playing unseen).

## 平面视觉 (the tutorial-placard session)

| # | Ph | Request | Needs | Days |
|---|---|---|---|---|
| G1 | 0 | **Your chosen option** from `placard/50_tutorial_placard.md` (Red's TP04 pick), with three Option A changes from `10_proposal.md` §5.2:<br>• the padlock sits on the EXIT door itself;<br>• one TV room per floor, and its key is the floor key;<br>• signals printed only when live (W1 now).<br>Until Option A lands, the key line reads without the stair lock. Strings are 叙事's | Red's TP04; 叙事 N1 | yours |
| G2 | 1 | Legend pictograms: STAIR (EXIT) and the attendant station (desk + TV + key). The HUD key panel string for one floor key (today it shows a zone number) | T1 = A | 0.5 |
| G3 | 2 | The stairwell copy (next sheet, its LEVEL) and the station plan (zone blocks, YOU ARE HERE at the station, STAIR marked; drawn at run time) | V6, M4 | 1–1.5 |

**Status from 平面视觉 (10-22):**
- **G1:** the v2 boards are done (Figma `3459:7280`, VL337) and already carry the Option A changes. Its options are now called "sheet A/B/C", so they are not confused with this doc's Option A.
- **Phase 0:** sheet TP06 `3471:7309` has no station and no padlock; line 4 reads `EXIT BY STAIR, / ONE FLOOR DOWN.` It becomes TP01 once Option A lands, C22 is in main (15:39); it ships together with the G1 mount as the interim Floor 1 sheet and retires when the first-room mount lands.
- **G2:** the pictograms exist. Proposal for the HUD key panel: the tag's number becomes the floor number, so it matches the placard's LEVEL. **Format picked by 平面视觉 (10-22, C28):** the tag shows the floor token in Courier Prime at the same size and place: `00` on Level 0, then `B1`…`B9`. From `B10` on, three characters at 0.8×, same baseline, centred; the tag does not grow. It matches the placard and the stair signs (LEVEL B1). Specs in `desktop_ui/FIX_CARD_VL217.md` §B and `placard/50_tutorial_placard.md` §11. The interim A-panel label stays `STAIR KEY`. 平面视觉 builds it after T1 = A.
- **G3:** sheet C's grammar, Phase 2.
- **10-22, first room (A26):** the Floor 1 sheet now draws the first room (spec `station/61` §3).
  - YOU ARE HERE is at the north-wall mount; the security desk (key) is at the counter; the start door reads NO REENTRY. The route runs east to STAIR A, which carries the EXIT and the padlock.
  - The four lines are the same; line 4 differs between the Phase 0 sheet and the full sheet.
  - Folders `sheetA_room_phase0/` and `sheetA_room/`; Figma TP07 `3587:7488`; VL411 PASS. 游戏视觉 packs them with A26 (M2). B1 stairwell copies keep the typical-floor sheet.
- **10-22, after C28:** sheet A (79b46369…) and the Phase 0 sheet (1e5d244b…) are regenerated with the warden fine-print line and sent to 游戏视觉 (`50_tutorial_placard.md` §13).
- **Sheet picked:** Red chose sheet A in 平面视觉's chat ("A 先上", 2026-10-22). Calls 2–4 are unanswered, so the sheet ships as drawn: no enemy drawn, no wall legend, DRAFT strings. **G1 is done for the stair copies (`sheetA_B1/`, `sheetA_phase0_B1/`). Reopened for Floor 1 (10-22, option 1):** `YOU ARE HERE` at the desk, `SECURITY DESK (KEY)` at the same spot, a short route to `STAIR A` · `EXIT`, the mount by the desk; `sheetA/` is not final until the first-room geometry exists. `sheetA_phase0/` ships as the interim Floor 1 sheet together with the G1 mount. The art is handed to 游戏视觉 in `Tools/print/ink/art_from_graphic/placard/`, each folder with `examine.json`:
  - `sheetA_phase0/`: swap in once the C22 stairwell and G1 land;
  - `sheetA/`: swap in once the stair lock and the station land.
  G2 waits on KeyHud B; G3 is Phase 2.
- **Later the same day** (specs in `placard/50_tutorial_placard.md` §11):
  - G2 is specified. The HUD tag's two-digit number is the floor (00, 03…) in KeyHud B's Courier Prime. Until KeyHud B, the interim label is `STAIR KEY` (DRAFT, no digits, because Bayon draws 0 and 1 as O and I).
  - The T5 controls line `WASD  MOVE    MOUSE  LOOK    SHIFT  SPRINT    E  USE` is in IBM Plex Mono 13 caps on paper, with a keyline and no dark card, centred under the title prompt. It was sent to 关卡设计 for M1.
  - TP04 line 4 follows Option A, with TP06 as Phase 0.

**平面视觉 · 广告** (not running; inbox note): the WTCZ loop's on-screen content stays Ad_02. The still desk screens (V4) are its art, from `screens/30_screen_strings.md` §6.
  - **2026-10-22 09:02, V4 art LANDED** (平面视觉 · 广告, running again since the account switch): `Tools/screens/out/still_desk/` (six screens, 640 × 480 + @2x, README, generator `still_desk.py` sha1 c6743734). Still, dim, neutral white; amber/green/VGA blue kept for their TM04 meanings. 游戏视觉 told; V4 is unblocked on the art side. 广告 sends 声音设计 an S1 content note (Ad_02's 16 s card timing, ident and jingle cue points).
  - **2026-10-22 16:0x, V4 staging (总控 as tutorial tracker, within Red's T3):** 游戏视觉 wires the three still frames + the seeded mix now, gated on **"this floor has a station"**. Before V1/M4 no floor has one, so desks keep Ad_03. Once V1/M4 land, station floors go still automatically (no second landing). After the take the desks stay still (§4.4). The title corridor TV is unaffected. M4 must expose a "floor has a station" query.
  - The call sign is now **WTCZ-TV 61** (叙事设计 `screens/30_screen_strings.md` v2.1, 10-22, under rule 11 "no real brands": WXRM-LP is a real licensed LPFM station, FCC facility 197239; 广告's FCC check in `AD_TYPE_SYSTEM.md` §7.1).

## 叙事设计 (not running: inbox note)

| # | Ph | Request | Days |
|---|---|---|---|
| N1 | 0 | Placard strings v2: the four instruction lines (L4, L5, L6, L1 + L2 in `10_proposal.md` §5.2), the legend labels, and the start door's label or none. Rules: never RUN or HIDE; nothing that implies darkness hides you; never "relay" | 0.25 |
| N2 | 1 | The key tag (placeholder `STAIR A`); the station's in-world name ("attendant station"?); whether the placard names the hunter's role (for example "floor warden"); the stairwell sheet numbering. **Done 10-22** (Red in 叙事's chat): `STAIR A`; `SECURITY DESK`; fine-print line `ON ALARM, A FLOOR WARDEN CHECKS EVERY ROOM.`; floors `LEVEL 0`, `B1`, `B2`, … (C28); CAUGHT adds `OCCUPANT ACCOUNTED FOR` (`narrative/40_floors_exit_tutorial_strings.md` v1.1) | 0.25 |

**N2 note (教程设计, 10-22):** the warning plate over the key rack is kept, on the close-reading layer only (EXAMINE or ≤ 1 m; not legible from the counter), and stays after the take. The wording must keep the alarm "at the panel", not in the room. See `10_proposal.md` §4.7.

## 系统设计 (records decisions; running)

| # | Request |
|---|---|
| D1 | When Red answers T1–T6, record them: verify Red's words in this chat and add the decision rows. If T1 = A, record the two system changes: C15 (zone keys → one floor key that opens the EXIT and any locked door on its floor; none on Floor 1) and "taking the station key sends the hunter to the station" |
| D2 | New systems for the table: Station + TV beacon (groups 6/7), Tutorial floor (group 7), Placard v4 (I1/N4). Edges: beacon ↔ N1 EXIT, beacon ↔ W8 (ducking), take ↔ B1/B2 |
| D3 | Note for C09 (screens join warnings): the beacon is a broadcast and is not a warning; it ducks under every warning, so C09's "one meaning per cue" holds |

**Status from 系统设计 (10-22):** D1–D3 are recorded in systems.json 2026-10-22e, after it checked Red's words in this chat:
- A13–A18: T1, the take, T2 + grace, T3, T5, T6;
- C15 and C22 carry "changed by A13" notes;
- new systems N6 "Station and TV beacon" and N7 "Tutorial floor";
- the C09 note.

T4 is not recorded, because the sheet is 平面视觉's TP04 pick. A20 (the Floor 2+ arrows) was answered by Red on 10-22 (`10_proposal.md` §7). **A26 (option 1)** is recorded (systems.json 2026-10-22z8). Rows it touches: I1 → "in the first room by the desk on Floor 1; on each stairwell landing"; A15 → add "on Floor 1, also not before the player has left the first room (A26)".

## 交互设计 (touch; running)

| # | Request |
|---|---|
| X1 | Phones: the station key is taken by walk-over until the tap path targets keys (C14). Check the beacon's handheld mix (S5) on a phone speaker, and whether the title should suggest headphones |
| X2 | The touch control hint that replaces `CalmHint` on phones (M1) |

**Status from 交互设计 (10-22):** X1 and X2 are taken, after the Alpha, behind A12 (door swing).
- **X1:** ~~phones keep walk-over until the tap path lands~~ **resolved 10-22 16:4x.** Phones take keys on purpose: look + USE (TAKE), a tap on the key, or within 22 pt of it. 关卡设计 set `KeyPickupNeedsE` true on every platform (Game fc76109d, MAP_GENERATION e022e8af), so walk-over is gone everywhere. The touch playtest's key steps pass. The beacon's phone mix check (S5) is still open.
- **X2 (10-22 16:4x):** built and in its test runs. It lands right after 关卡设计's joint M1 batch and 游戏视觉's Bayon `·` fallback.
- **X2 and T5 (R7, Red 2026-10-29 "全部按推荐", 总控's audit):** keep X2. Its words (SLOW · UNHEARD, the RUN preview, WINDED) sit on the controls, retire once learned and come back through Settings › STICK HINTS. They are not pop-ups or cards, so they are consistent with T5 (the text card removed; the controls on the title and pause). X2 landed 10-22 20:2x; no change.
- **The beacon's handheld mix** needs a real phone speaker, so a device build on Red's yes through App Store · 构建. A clone listening pass comes first, then a recommendation on suggesting headphones at the title.
- **X2:** the phone hint grows out of an existing touch element (the stick's legend, the USE disc), with no dark card. **Owners (corrected 10-22, COMMON rule 9):** control-label strings are 交互设计's; any line that tells the player what the hunter perceives (e.g. `SLOW · UNHEARD`) is signed by 叙事设计; type is 平面视觉's (answers in `placard/50_tutorial_placard.md` §11 "X2 type answers", approved by Red 10-22). The spec comes to the tutorial tracker (总控 on this account) before it is built.
  - **2026-10-22 10:3x, X2 spec reviewed by 总控 (tutorial tracker): sent back with changes, not approved to build.** Spec `/Users/redwang/FrontRoomsInteractionWork/x2/X2_PHONE_HINTS_SPEC.md`; review `<作业根>/Documentation/Takeover_2026-10-18/reviews/X2_review_20261022.md`. Blockers: RUN: BUTTON mode not covered; SLOW inside the slow ring sits under the thumb; type (Bayon 17) conflicts with 平面视觉's T5 handheld spec (`placard/50_tutorial_placard.md` §11) and goes to 平面视觉. Also: keep the phone-only noise rule (slow unheard, walk heard), deliberate-creep retire rule, land step 1 with M1.
  - **2026-10-22 11:3x, X2 v2/v2.1 re-reviewed: direction approved, one more revision (v3) before build; nothing needs Red.** Review `<作业根>/Documentation/Takeover_2026-10-18/reviews/X2_rereview_v2_20261022.md`. Open: the slot overlaps the RUN button in BUTTON mode; one-slot word handover (priority, latch, SLOW dwell); edge rule on the real label rect vs the key panel; a named release signal before M7; a playtest seam for the retire check; keyline legibility on lit ground; 关卡设计's ack of the joint M1 landing and the phone Flash-card hunk.
  - **2026-10-22 12:3x, X2 v3 re-reviewed: approved to build after v3.1** (pin the below-slot anchor so FIXED + BUTTON still shows and retires SLOW; higher-priority words cut the hold short; complete the placement rect: top edge, key panel mid-touch, iPad scale, sideways reach, canvas → points, caption rail; name check 12's route). 总控 checks the v3.1 diff only. Build waits for the Alpha cut-off, M1 and the spec's §8 gates (叙事's sign-off of `SLOW · UNHEARD`, 游戏视觉's KeylineAlpha + Bayon fallback). Review `<作业根>/Documentation/Takeover_2026-10-18/reviews/X2_rereview_v3_20261022.md`.
  - **2026-10-22 11:5x, X2 v3.1 light check by 教程设计 (diff against the v3 review only, no workflow): APPROVED TO BUILD.** Spec `X2_PHONE_HINTS_SPEC.md` v3.1 (sha1 ef2f0cf2). All four fixes hold (anchor + margins; interrupt; full placement rect; check 12 last + `HeldForTools`, EXAMINE step, check 3 ≥ 3 s). Three wording notes go in at build time, with no further review:
    1. **The placement test uses the settled liveScale 1.** At touch-down it is 0.88 (View :459), which would test a rectangle about 12 % short (R3-3 d, not carried into v3.1).
    2. **The iPad margins row** says "Bayon 19, cap 13.6", but §3.3 and today's code use Bayon 17 at every scale (View :321, never rescaled). Either keep 17 (cap 12.1, margin 3.9 pt), or name the tablet size as 平面视觉's and count the 1.4 pt keyline (margin about 2.0 pt). Positive either way.
    3. **Check 10** gets one Android 20:9 run beside the iPad run (R3-4).
    The build still waits for the Alpha cut-off, M1, and the §8 gates (`FrontRoomsHudStyle`, the Bayon → Plex Mono fallback, 平面视觉's sign-off of the Figma states). Recorded in `reviews/X2_rereview_v3_20261022.md` § "v3.1 light check".
  - Note for the record: L4 "walking is silent at T1" (`10_proposal.md` L4 row) is true on desktop only; on phones a walk is heard (9 m), only the slow gait is silent (`TOUCH_CONTROLS.md`).
