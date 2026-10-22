# 20 · Tutorial: requests to each line

Written 2026-10-22 by 教程设计. These implement `10_proposal.md`.

**APPROVED by Red, 2026-10-22,** in the 教程设计 chat (`local_c160c950-9b0e-4f15-ad77-95c2e4930c87`): "全部按推荐（T1=A，T2=E1，T3 静止屏，T5 去掉文字卡，T6 标题电视带声音）".
- Every row below is approved, except **V7** (E2 was not chosen).
- A message from me is not Red's approval (COMMON rule 15); each owner checks Red's words in this chat.
- **Order:** Phase 0 rows first; Phase 1 rows after 关卡设计's C22 stairwell minimum. All after the Alpha. Each owner lands through its own claims in LANDINGS.md.

**Columns.**
- **Ph** = phase: 0 needs no new system; 1 is the Option A core; 2 is later.
- **Days** are ESTIMATE, for the owner to correct.
- Everything lands after the Alpha.

## 关卡设计 (map, game loop, Relay code)

| # | Ph | Request | Needs | Days |
|---|---|---|---|---|
| M1 | 0 | Remove the `CalmHint` card ("Shift to sprint…", 9 s). Show the controls once on the title and on the pause card (WASD · MOUSE · SHIFT · E). Touch hints are 交互设计's. **Any option** (T5) | 平面视觉 type | 0.25 |
| M2 | 0 | Placard mount: 平面视觉's guarantee G1, P1 west on every seed, keeping D's west edge open in `MapRootFor` (`placard/50_tutorial_placard.md` §5). Also its G2 (the start door never shows a padlock or an unlock prompt). The mount code is 游戏视觉's (placard `10_spec.md` §5.3) | 游戏视觉; Red's TP04 pick | 0.25 |
| M3 | 1 | **Locked EXIT:** the floor's EXIT door is locked; the floor key opens it with the existing unlock shot. **Floor key:** one per floor, at the station's KeySpot; it opens every locked door on its floor. Retires zone keys (C15 change, recorded by 系统设计). No locked interior doors on Floor 1 | C22 minimum; 系统设计 record | 1 |
| M4 | 1 | **Station placement:** one station module per floor; kept through revisit shifts. Floor 1: 2–6 cells of walking past the EXIT, with the EXIT at 14–18 cells. Floor 2+: 10–20 cells from the arrival, off the EXIT's shortest route, ≥ 6 cells from it. Never Tall; reachable without glass or keys (L5) | 游戏视觉 modules (V1); L5 | 1 |
| M5 | 1 | **Beacon field:** a path field seeded at the station TV's cell (C24's machinery). Reach about 90 m; glass impassable; counts shut openings; rebuilt only on topology or opening changes. Public read: `BeaconPath(playerCell, out pathMetres, out shutOpenings, out portalPos)`, where `portalPos` is the first opening leaving the player's room set. Beyond the built map: the nearest built border doorway, as C24 Per A20 (Red, 10-22), the C24 calm route reads this station field until the key is taken, then the EXIT field | M4 | 0.75 |
| M6 | 1 | **The take:** `StationKeyTaken(stationCell)` event, which the TV (V2) and the sound (S3, S4) listen to. On v1, a Noise at the station with unlimited radius, so the Relay walks there (Hunt). On v2, a device call | M3, M4 | 0.25 |
| M7 | 1 | **Floor 1 send (E1):** the Relay is not released until `StationKeyTaken`, with a fallback of 150 s (SP) after the start door shuts or entering a third zone. Then it arrives at least 14 cells (42 m) of walking from the player (v2's Arrive rule, RELAY §5), away from the EXIT side if possible, and its first leg is the Hunt to the station. **First-approach grace, Floor 1 only:** no chase before the player has had ≥ 1 W1 burst, unless within 6 m | M6; T2 | 0.5 |
| M8 | 1 | Autopilot report fields: Space → station, take → EXIT, EXAMINE on the placard, first W1 burst before first chase (`10_proposal.md` §10). The baseline bots (C05) log whether E1 and the Floor 1 grace are on, because both change v1's release and sight | — | 0.25 |

**Status from 关卡设计 (10-22):** it checked Red's words in this chat and accepted M1–M8 as specified, with no number changes; all after the Alpha.
- **Phase 0 first:**
  - M1: CalmHint out, plus the T5 controls line in 平面视觉's type;
  - M2: P1 west in `MapRootFor`, and the start door never shows a padlock or an unlock prompt.
- **Phase 1 sits on C22,** which is in its clone now: EXIT + stairwell + NextFloor, with the EXIT unlocked and a lock seam kept for M3. Then M3–M8 in order.
- **Module hand-over is settled:** 游戏视觉 ships the two station modules as new files. M4 places them; they never go into the random module pool.
- C24 and M5 share the field code and are built together.
- 关卡设计 reports when C22 lands and as each M row lands.

## 游戏视觉 (3D, kits, CRT and ScreenVideo, merges)

| # | Ph | Request | Needs | Days |
|---|---|---|---|---|
| V1 | 1 | **Two station modules** (Level Designer): Station · Waiting (Level 0) and Station · Desk (Office).<br>• Kits: `Kit_CRTTV`, `Kit_KeyHookBoard` with `Kit_KeyRing` and a tag, chairs, a desk lamp.<br>• One KeySpot on the rack.<br>• The TV side's lamps Dim or Off; one lit lamp by the entrance | Key model (D7) on a hook | 1 |
| V2 | 1 | **Beacon mode for the station TV:**<br>• the ad plays with its light (V3);<br>• on `StationKeyTaken`, the picture collapses to a dot and fades (0.4 + 0.6 s), then stays off | M6 | 0.5 |
| V3 | 1 | **TV light:**<br>• colour from the video's mean colour, intensity from its luminance (0.1 s sampling, τ 0.12 s);<br>• range about 6 m; soft shadows on desktop, none on WebGL;<br>• ≤ 3 swings of 10 % or more per second; Reduce flashing τ 0.5 s | — | 0.5 |
| V4 | 1 | **Still desk screens:** on a floor whose key is untaken, Office CRTs show still, dim screens (`screens/30_screen_strings.md` §6) instead of the ad. No motion, no sound (T3) | 平面视觉 · 广告 art | 0.5 |
| V5 | 1 | **Title corridor TV with sound:** route its ad audio to 声音设计's event (T6). The surface keeps `audioOutputMode None` | S1 | 0.1 |
| V6 | 2 | Placard copies on each stairwell landing (same kit, new art per floor); the painted floor number on the landing wall | C22 stairwell look | 0.5 |
| V7 | — | **Not chosen** (T2 = E1). Kept for the record: (E2) a one-time black-and-white security monitor at the station showing the hunter cross a corridor | Hunter look final | 1.5–2 |

**Status from 游戏视觉 (10-22):** it checked Red's words in this chat and accepted V1–V6, plus the placard mount code for M2 (`FrontRoomsPlacardMount.cs`, P1 west; G1/G2 and `MapRootFor` stay 关卡设计's).
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

## 平面视觉 (the tutorial-placard session)

| # | Ph | Request | Needs | Days |
|---|---|---|---|---|
| G1 | 0 | **Your chosen option** from `placard/50_tutorial_placard.md` (Red's TP04 pick), with three Option A changes from `10_proposal.md` §5.2:<br>• the padlock sits on the EXIT door itself;<br>• one TV room per floor, and its key is the floor key;<br>• signals printed only when live (W1 now).<br>Until Option A lands, the key line reads without the stair lock. Strings are 叙事's | Red's TP04; 叙事 N1 | yours |
| G2 | 1 | Legend pictograms: STAIR (EXIT) and the attendant station (desk + TV + key). The HUD key panel string for one floor key (today it shows a zone number) | T1 = A | 0.5 |
| G3 | 2 | The stairwell copy (next sheet, its LEVEL) and the station plan (zone blocks, YOU ARE HERE at the station, STAIR marked; drawn at run time) | V6, M4 | 1–1.5 |

**Status from 平面视觉 (10-22):**
- **G1:** the v2 boards are done (Figma `3459:7280`, VL337) and already carry the Option A changes. Its options are now called "sheet A/B/C", so they are not confused with this doc's Option A.
- **Phase 0:** sheet TP06 `3471:7309` has no station and no padlock; line 4 reads `EXIT BY STAIR, / ONE FLOOR DOWN.` It becomes TP01 once Option A lands, and it still needs G1 (P1 west) and the C22 stairwell.
- **G2:** the pictograms exist. Proposal for the HUD key panel: the tag's number becomes the floor number (03 on Level 3), so it matches the placard's LEVEL. 平面视觉 builds it after T1 = A.
- **G3:** sheet C's grammar, Phase 2.
- **Sheet picked:** Red chose sheet A in 平面视觉's chat ("A 先上", 2026-10-22). Calls 2–4 are unanswered, so the sheet ships as drawn: no enemy drawn, no wall legend, DRAFT strings. **G1 is done.** The art is handed to 游戏视觉 in `Tools/print/ink/art_from_graphic/placard/`, each folder with `examine.json`:
  - `sheetA_phase0/`: swap in once the C22 stairwell and G1 land;
  - `sheetA/`: swap in once the stair lock and the station land.
  G2 waits on KeyHud B; G3 is Phase 2.
- **Later the same day** (specs in `placard/50_tutorial_placard.md` §11):
  - G2 is specified. The HUD tag's two-digit number is the floor (00, 03…) in KeyHud B's Courier Prime. Until KeyHud B, the interim label is `STAIR KEY` (DRAFT, no digits, because Bayon draws 0 and 1 as O and I).
  - The T5 controls line `WASD  MOVE    MOUSE  LOOK    SHIFT  SPRINT    E  USE` is in IBM Plex Mono 13 caps on paper, with a keyline and no dark card, centred under the title prompt. It was sent to 关卡设计 for M1.
  - TP04 line 4 follows Option A, with TP06 as Phase 0.

**平面视觉 · 广告** (not running; inbox note): the WTCZ loop's on-screen content stays Ad_02. The still desk screens (V4) are its art, from `screens/30_screen_strings.md` §6.
  - **2026-10-22 09:02, V4 art LANDED** (平面视觉 · 广告, running again since the account switch): `Tools/screens/out/still_desk/` (six screens, 640 × 480 + @2x, README, generator `still_desk.py` sha1 c6743734). Still, dim, neutral white; amber/green/VGA blue kept for their TM04 meanings. 游戏视觉 told; V4 is unblocked on the art side. 广告 sends 声音设计 an S1 content note (Ad_02's 16 s card timing, ident and jingle cue points).
  - The call sign is now **WTCZ-TV 61** (叙事设计 `screens/30_screen_strings.md` v2.1, 10-22, under rule 11 "no real brands": WXRM-LP is a real licensed LPFM station, FCC facility 197239; 广告's FCC check in `AD_TYPE_SYSTEM.md` §7.1).

## 叙事设计 (not running: inbox note)

| # | Ph | Request | Days |
|---|---|---|---|
| N1 | 0 | Placard strings v2: the four instruction lines (L4, L5, L6, L1 + L2 in `10_proposal.md` §5.2), the legend labels, and the start door's label or none. Rules: never RUN or HIDE; nothing that implies darkness hides you; never "relay" | 0.25 |
| N2 | 1 | The key tag (placeholder `STAIR A`); the station's in-world name ("attendant station"?); whether the placard names the hunter's role (for example "floor warden"); the stairwell sheet numbering | 0.25 |

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

T4 is not recorded, because the sheet is 平面视觉's TP04 pick. 系统设计 is asking Red the Floor 2+ arrow amendment to C24.

## 交互设计 (touch; running)

| # | Request |
|---|---|
| X1 | Phones: the station key is taken by walk-over until the tap path targets keys (C14). Check the beacon's handheld mix (S5) on a phone speaker, and whether the title should suggest headphones |
| X2 | The touch control hint that replaces `CalmHint` on phones (M1) |

**Status from 交互设计 (10-22):** X1 and X2 are taken, after the Alpha, behind A12 (door swing).
- **X1:** phones keep walk-over until the tap path lands (`KeyOnRay` + `AimKind.TakeKey` in `HandleMobileLookTap`), which is next in its queue.
- **The beacon's handheld mix** needs a real phone speaker, so a device build on Red's yes through App Store · 构建. A clone listening pass comes first, then a recommendation on suggesting headphones at the title.
- **X2:** the phone hint grows out of an existing touch element (the stick's legend, the USE disc), with no dark card. Type and strings go through 平面视觉. The spec comes to 教程设计 before it is built.
  - **2026-10-22 10:3x, X2 spec reviewed by 总控 (tutorial tracker): sent back with changes, not approved to build.** Spec `/Users/redwang/FrontRoomsInteractionWork/x2/X2_PHONE_HINTS_SPEC.md`; review `<作业根>/Documentation/Takeover_2026-10-18/reviews/X2_review_20261022.md`. Blockers: RUN: BUTTON mode not covered; SLOW inside the slow ring sits under the thumb; type (Bayon 17) conflicts with 平面视觉's T5 handheld spec (`placard/50_tutorial_placard.md` §11) and goes to 平面视觉. Also: keep the phone-only noise rule (slow unheard, walk heard), deliberate-creep retire rule, land step 1 with M1.
  - Note for the record: L4 "walking is silent at T1" (`10_proposal.md` L4 row) is true on desktop only; on phones a walk is heard (9 m), only the slow gait is silent (`TOUCH_CONTROLS.md`).
