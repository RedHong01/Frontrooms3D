# HUD key B wiring contract: what each hunk is (M-1 to M-9)

游戏视觉, 2026-10-29 13:5x, contract **v2.9** (fix round 2). These are 关卡设计's files. 游戏视觉 changed them only in its clone (`W/proj_hudkey`) to test them.

Bases: main ac63418d (Unity 6000.6.3f1) plus the working tree at 10:51:
- FrontRooms3DGame.cs **69687b6a** (13:51: 关卡设计's s25 HUD kit d958edfd of 11:39, the zone meta row in paper (83b148c2, 13:41), then s27's phone captions 220 pt; HEAD has 00d06f84). v2.8 was on 90154a8e and failed one hunk's context after s25 (`"RELAY  "` → `"IT  "`);
- FrontRoomsDoorShot.cs b2014396;
- FrontRoomsDoorShotTests.cs abc598da;
- FrontRoomsMapWorld.cs a8a83b0b, checked and not touched.

Every hunk carries its name in a code comment (`// HK M-n`), so you can grep for it.

| Name | Part | File | What it does |
|---|---|---|---|
| **M-1** | HK1 | FrontRooms3DGame.cs, BuildHud | **Attach.** `FrontRoomsKeyHud.Attach(hudCanvas, crosshair, keyPanel, mobileHud)`. From then on the key HUD draws the dot, the crosshair states (L3·D / D·3 / F·1, B·4, E·1) and the key panel (B, P4, U·3). |
| **M-2** | HK1 | FrontRooms3DGame.cs, UpdateAim / UpdateHud / SetPhase | **The per-frame seam.** <br> - `aimKind`: from the hit, `map.Kind`; from an EXAMINE target, Read or Examine; for a look+E key, TakeKey. <br> - `SetAim(aimKind, HudIdentity(zone), haveKey, words)`, called before the prompt line. <br> - The map's own dot and prompt step aside only while `OwnsDot` / `OwnsPrompt` are true. <br> - `SetHeldKey`. The map's own key panel stays off. <br> - A rest call out of play. <br> - `HudIdentity` = `StubIdentity(zone, floor − 1)` until D1.6 (DECISIONS #20). |
| **M-3** | HK1 | FrontRooms3DGame.cs, ApplyGameplayHudAlpha | **The alphas.** `SetHudAlpha(gameplayHudAlpha, rig.HudFade)`, plus DECISIONS #14's one line: the panel is exempt from the unlock shot's fade in mode (d). |
| **M-4** | HK1 | FrontRooms3DGame.cs, UseDoor | **The E events, for the door under the crosshair only.** Rattled → `NoKey`, KeyTurn → `Unlocking`, Broken → `BrokenTry`. |
| **M-5** | KP-1 | FrontRoomsDoorShot.cs + FrontRooms3DGame.cs | **The P4 hand-off.** <br> - DoorShot raises `KeyHandoff` at `KeyPickup.HoldUntil`, but **only when someone listens**. <br> - At that point the 3D key is hidden, not destroyed. The pocket beat (its sound) stays at 0.85 s. Presenting, Release and the destroy stay at 0.95 s. <br> - New: `HandedOff(zone)`. <br> - The game subscribes only when the key HUD is attached. `OnKeyHandoff` → `PickupFlight`. <br> - The key HUD shows the key from the hand-off (else its flight would end after 0.1 s). |
| **M-6** | HK1 | FrontRooms3DGame.cs, the autopilot's door scenario | **The autopilot's key-panel checks read the panel the player sees.** <br> - With the key HUD attached, these are `keyHud.PanelShown` and `keyHud.PanelArrival` (`AutoKeyPanelUp` / `AutoKeyPanelIn`). <br> - "Too early" now means "while the 3D key is still in view". <br> - Without the key HUD these read exactly what they read before. |
| **M-8** | HK1 | FrontRooms3DGame.cs, OnKeyTaken | **KP-1b, the instant pickup (DECISIONS #37, Red 10-29).** When a key is announced with nothing presenting it (shipped play, until D1.6's real key), the game calls `keyHud.PickupInstant(HudIdentity(zone), BaseFov)`. The B glyph then plays the full lab P4 by itself: 0.5 s held at the hand position (0.65, 0.30), then the flight into the key panel's slot (desktop bottom-left, phone top-left). With M-6 the autopilot's instant check accepts the key HUD's panel as up on the frame after contact, because it starts its P4 there. |
| **M-9** | HK1 | FrontRooms3DGame.cs, BuildHud | **One prompt keyline (G6, DECISIONS #38).** `AddKeyline(promptText, 2f)` → `AddKeyline(promptText, FrontRoomsHudStyle.PromptKeyline(mobileHud))`: 2 px on desktop as now; 1 pt on the phone (#23), where `2f` meant 6 px and the Outline copies read as a ghost (`../images/VL303b_g6_prompt_keyline.jpg`). The key HUD's line 1 reads the same function. Needs hud_key_b's FrontRoomsHudStyle v3. |
| **M-7** | KP-1 | Editor/FrontRoomsMap/FrontRoomsDoorShotTests.cs | **One new map test, `21 hand-off (KP-1)`, for the listener path.** It checks: <br> - one event at 0.75 s, with the viewport point and the depth; <br> - the key hidden from that point; <br> - the pocket beat at 0.85 s; <br> - gone at 0.95 s. <br> The old tests (no listener) are unchanged and still pass. |

HK1 = M-1 … M-4 + M-6 + M-8 + M-9. It is Game.cs only and compiles without KP-1. With HK1 alone, the shipped instant pickup already plays P4 (KP-1b).

KP-1 = M-5 + M-7: the presented key (D1.6's real key; in the editor, the placeholder). Land HK1 and KP-1 together. Without KP-1, a presented pickup's panel pops in at 0.95 s with no P4.

## Files

- `FrontRooms3DGame.HK1.diff`: HK1 on Game.cs 69687b6a (19 hunks).
- `KP1_handoff.diff`: KP-1 on top of HK1 (Game.cs 4 hunks, DoorShot 5, DoorShotTests 2).
- `FrontRooms3DGame.contract.diff`: both in one diff (Game.cs 21 hunks, DoorShot 5, DoorShotTests 2).
- `W/hudkey/contract/apply_contract.py`: the generator. It works on clones only. `--hk1-only` gives HK1 alone.
- `W/hudkey/contract/regen_contract.sh <tag> <version>`: re-makes the three diffs from main's current files and runs the dry runs below in one go (writes nothing in main). For a re-base, put the new Game.cs sha1 in the generator's BASES first; it refuses if an anchor no longer applies.
- v2.8 is kept as `*_1029_v28*` with `base_1029_v28/`; v2.7 as `*_1029_v27*`; v2.6 as `*_1024_v26*`.

## Checked (2026-10-29 13:4x–14:00, v2.9)

- `patch -p1 --dry-run` on fresh copies of main (Game 69687b6a, DoorShot b2014396, Tests abc598da): HK1 alone, combined, and KP-1 on HK1 are clean (no fuzz, no offset, no reject); the applied files equal the generator's output byte for byte (`W/hudkey/logs/r2_contract_dryrun.txt`).
- Roslyn `--all` (6000.6.3f1's compiler, `--ref W/proj_ref66`): COMPILE OK on macOS and iOS for main + payload, + HK1, + HK1 + KP-1, on main's files of 14:00 (Game 69687b6a, Mobile 6869ef97) (`W/tools/_rebase/hud_key_b_fix1029_r2/`).
- In the clone: `FrontRoomsInspectPlaytest.RunBatch` with HK1 **134/0** (the two new as-drawn checks pass, drawn by the key HUD); the key HUD's registry 72/72, wiring 24/24 desk, 24/24 phone, 65/65 for the E·1 scripts (O-25 0.150 s). M-5 … M-8 are unchanged, so v2.8's map-suite results below stand.

## Checked (2026-10-29, v2.8)

- `patch -p1 --dry-run` is clean (no fuzz, no offset) on copies of main's three files. Applying it gives the generator's output byte for byte.
- Roslyn `--all` gives COMPILE OK on macOS and iOS for each of these, with the hud_key_b payload and main's own 6000.6.3f1 `.rsp` references:
  - main + payload;
  - + HK1;
  - + HK1 + KP-1.
- In the clone on Unity 6000.6.3f1 (main ac63418d plus its working tree, the payload, the contract):
  - `FrontRoomsDoorShotTests.RunBatch`: 247 passed, 0 failed. This includes 1226 (the placeholder's pocket beat 0.85 s and gone 0.95 s), 1307 (the dressed key's Release and destroy at 0.95 s) and the new M-7.
  - `FrontRoomsInspectPlaytest.RunBatch` with HK1: 132/0.
  - The autopilot's door scenario, with v2.7 and with v2.8 (KP-1b): of its 34 door check rows, 33 are ok and 1 is a note (the raw ray meets the player's own controller from 44° down; the aim skips it). None fails. The overall FAIL verdict comes from the clone's FMOD BankLoadException, plus, on one seed, a Relay catch after the scenario that a no-contract control run also hit.
  - KP-1b in game, desktop and phone: `hud_build/20_build.md` §10 and `images/VL301c_kp1b_instant_pickup.jpg`.
