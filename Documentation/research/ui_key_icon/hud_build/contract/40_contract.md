# 40 · Wiring contract for HUD key B (for 关卡设计)

游戏视觉. **v2.9, 2026-10-29 13:5x** (fix round 2).

It is re-based on main ac63418d (Unity 6000.6.3f1) plus the working tree:
- FrontRooms3DGame.cs **69687b6a** (13:51: your s25 HUD kit d958edfd of 11:39, the zone meta row in paper 83b148c2 at 13:41, then s27's phone captions 220 pt). HEAD has 00d06f84.
- FrontRoomsDoorShot.cs **b2014396**;
- FrontRoomsDoorShotTests.cs **abc598da**;
- MapWorld **a8a83b0b**, checked and not touched.

v2.9 adds **M-9** (one line, DECISIONS #38) to v2.8. v2.8 no longer applied after your s25 kit: M-2's UpdateHud hunk had `"RELAY  "` in its context, which is now `"IT  "`.

This is a contract, not a landing. These files belong to 关卡设计. 游戏视觉 has applied them only in its clone `W/proj_hudkey`, to test them. **Every hunk is named M-1 … M-9 in `README.md`** (next to this file), and each one carries its name in a code comment.

**v2.9 already includes your s27 (13:51).** If Game.cs moves again before you land this, re-generate with `W/hudkey/contract/regen_contract.sh <tag> v2.9` after putting the new sha1 in `apply_contract.py`'s BASES: it re-makes the three diffs from main's files and runs the dry runs below (游戏视觉 runs it; it writes nothing in main).

## What lands first, and what it does on its own

1. `W/apply/hud_style.sh` landed 2026-10-22 15:52. It adds FrontRoomsHudStyle (DECISIONS #31).
2. `W/apply/hud_key_b.sh` is visual-owned. It adds 161 new paths:
   - FrontRoomsKeyHud and its motion, parts, vector graphic and text layout, in `Assets/Scripts/Rendering/UI/`;
   - the `FrontRooms/UI/HudVector` shader and 74 part PNGs, in `Assets/Resources/UI/KeyHud/`.

   It also replaces two visual-owned files:
   - `Assets/Editor/Rendering/Inspect/FrontRoomsInspectPlaytest.cs`. Its prompt checks read the game's `prompt` string, so they pass with and without HUD B. Two more checks read the line as drawn: the key HUD's line 1 when it owns the line, else `promptText`.
   - `Assets/Scripts/Rendering/UI/FrontRoomsHudStyle.cs` (hud_style v3). It **appends** `PromptKeylineDistance = 2` and `PromptKeyline(bool handheld)` (G6, DECISIONS #38). Nothing that exists changes, so your `AddKeyline` and the touch layer compile and draw as before.

   The package is stacked on hud_style. **On its own it changes nothing:** nothing calls `FrontRoomsKeyHud.Attach` yet.
3. **This contract** switches the game over. In play, the crosshair then uses the approved set:
   - the dot;
   - L3·D, D·3 and famOut on a locked door;
   - U·3 on the panel under the unlock shot (d), and F·1 for the padlock;
   - B·4 on a broken door;
   - E·1 on Read and Examine targets;
   - P4 when a key is picked up: for the instant pickup of shipped play by KP-1b (M-8), for a presented key by KP-1 (M-5).

## Files

| File | Base | What |
|---|---|---|
| `FrontRooms3DGame.HK1.diff` | Game.cs 69687b6a | **HK1** = M-1 … M-4 + M-6 + M-8 (KP-1b) + M-9. Game.cs only, 19 hunks. It compiles without KP-1. |
| `KP1_handoff.diff` | HK1 result + DoorShot b2014396 + DoorShotTests abc598da | **KP-1** = M-5 + M-7. Game.cs 4 hunks, DoorShot 5, DoorShotTests 2. |
| `FrontRooms3DGame.contract.diff` | Game.cs 69687b6a + DoorShot b2014396 + DoorShotTests abc598da | Both in one diff: Game.cs 21 hunks, DoorShot 5, DoorShotTests 2. |

## What changed in v2.9 (fix round 2, DECISIONS #38)

- **The re-base onto 69687b6a** (13:51). No hunk changed. Only the diff's context moved: M-2's UpdateHud hunk shows `"IT  "` where v2.8 showed `"RELAY  "`.
- **M-9, one line in BuildHud:** `AddKeyline(promptText, 2f)` becomes `AddKeyline(promptText, FrontRoomsHudStyle.PromptKeyline(mobileHud))`.
  - **Desktop:** 2 px, exactly as now (G6).
  - **Phone:** 1 pt, which is #23's phone keyline. Your `2f` is in canvas units, and on the phone canvas (ConstantPixelSize × 3) that is 6 px. There, Unity's four diagonal Outline copies come apart from Bayon 17's stems and read as a ghost of the line: see `../images/VL303b_g6_prompt_keyline.jpg`, the "2 pt (rejected)" rows. G6 was measured on a desktop frame and says "2 px", and the 1 pt phone keyline is already 3 px.
  - The key HUD's line 1 reads the same function, so LOCKED / E · UNLOCK / BROKEN / E · READ PLAN (key HUD) and OPEN DOOR / TAKE KEY (yours) get the same keyline on both platforms.
  - If 平面视觉 wants 2 pt on the phone after all (O-35), it is one constant in FrontRoomsHudStyle, not your file.

## What changed in v2.8 (DECISIONS #37, Red 2026-10-29 10:55)

- **M-8, KP-1b.** `OnKeyTaken` calls `keyHud.PickupInstant(HudIdentity(zone), BaseFov)` when the key is announced with nothing presenting it. That is shipped play's instant pickup, until D1.6's real key. The B glyph then:
  - appears at the hand position (0.65, 0.30) at the held size;
  - is held there 0.5 s with the tag's swing;
  - flies into the key panel's slot: desktop bottom-left, phone top-left (#35).
- **M-6** now accepts the key HUD's panel as up on the frame after an instant contact (its P4 starts there).
- **The re-base:** every v2.7 hunk applies unchanged to a6b6b6c6 and to 90154a8e (11:21).

## What changed in v2.7 (engineering verify, 2026-10-29)

1. **KP-1 no longer breaks your tests (M-5).**
   - DoorShot hands off only when a `KeyHandoff` listener exists. With no listener (your EditMode tests, or a game without the key HUD), the key drops into the pocket as before.
   - With a listener, the 3D key is **hidden** at `HoldUntil`, not destroyed. So these keep their clock:
     - the pocket beat (its sound) at `PocketSound` 0.85 s;
     - `Presenting`, `Release` and the destroy at `PocketEnd` 0.95 s.
   - This keeps your checks at `FrontRoomsDoorShotTests.cs:1226` / `:1307` and the autopilot's pocket and gone checks valid.
   - New API: `FrontRoomsDoorShot.HandedOff(zone)`.
   - The game subscribes only when the key HUD is attached.
   - The key HUD panel shows from the hand-off. Without that, its P4 flight would have ended after 0.1 s, because `Presenting` now lasts to 0.95 s.
2. **HK1 no longer breaks the autopilot (M-6).**
   - The door scenario's key-panel checks read `keyHud.PanelShown` / `keyHud.PanelArrival` when the key HUD is attached. The map's `keyPanel` stays inactive then.
   - "Too early" now means "while the 3D key is still in view".
   - Without the key HUD these checks are what they were.
3. **A new test for the listener path (M-7):** `21 hand-off (KP-1)` in FrontRoomsDoorShotTests.

v2.7 is kept in `W/hudkey/contract/` as `*_1029_v27*` with `base_1029_v27/`, and v2.6 as `*_1024_v26*` with `base_1024_v26/`.

## Checked on 2026-10-29 (v2.9, 13:4x–14:00)

- `patch -p1 --dry-run` on fresh copies of main's files (Game 69687b6a, DoorShot b2014396, Tests abc598da): HK1 alone, the combined diff, and KP-1 on top of HK1 all apply with no fuzz, no offset and no reject. The applied result is byte-identical to the generator's output (`W/hudkey/logs/r2_contract_dryrun.txt`).
- Roslyn `--all`, Unity 6000.6.3f1's own compiler, reference `W/proj_ref66`: COMPILE OK on macOS and iOS for main + payload, + HK1, + HK1 + KP-1, on main's files of 14:00 (Game 69687b6a, Mobile 6869ef97) (`W/tools/_rebase/hud_key_b_fix1029_r2/`).
- In `W/proj_hudkey` (main's files of 13:40 copied in, Game 83b148c2 with contract v2.9; s27 only moves the phone captions; Unity 6000.6.3f1): `FrontRoomsInspectPlaytest.RunBatch` with HK1: **134 passed, 0 failed**, including the two new as-drawn checks ("drawn by the key HUD"). The checks below are v2.8's: M-5 / M-6 / M-7 / M-8 are unchanged in v2.9.

## Checked on 2026-10-29 (v2.8)

- `patch -p1 --dry-run` applies `FrontRooms3DGame.contract.diff` cleanly (no fuzz, no offset) to copies of main's three files. The result is byte-identical to the generator's output.
- Roslyn `--all` gives COMPILE OK on macOS and iOS, against main's own 6000.6.3f1 `.rsp` references, with the hud_key_b payload included. Three configurations:
  - main + payload;
  - main + payload + HK1;
  - main + payload + HK1 + KP-1.
- In `W/proj_hudkey`, run on Unity 6000.6.3f1 (re-synced 10:31 to main ac63418d, then main's 9 files changed since copied in at 11:2x):
  - `FrontRoomsDoorShotTests.RunBatch`: **247 passed, 0 failed**, including the new M-7 checks.
  - `FrontRoomsInspectPlaytest.RunBatch`: **132/0**, with HK1 attached.
  - The autopilot's door scenario (`-autopilotDoors`): of its 34 door check rows, **33 are ok and 1 is a note** ("a raw ray meets the player's own controller from 44° down (the aim skips it)"); none fails. That holds for KP-1 alone (v2.7) and with KP-1b (v2.8), and includes M-6. The runs' overall verdict is FAIL in the clone, for two reasons:
    - the FMOD BankLoadException, the known clone issue;
    - one seed was also caught by the Relay after the scenario. A control run with **no** contract and the same seed was caught too, so that is not HUD B.
  - The key HUD's captures: see `20_build.md` §10.

## When you land it

- Land HK1 and KP-1 together. HK1 alone already gives the shipped instant pickup its P4 (KP-1b). Without KP-1, a presented pickup's key panel pops in at 0.95 s with no P4.
- When D1.6's real key lands, its presented pickup goes through KP-1, and M-8 stays for any key without a model.

## Open items on the map's side (not blocking the visual landing)

- **New, the prompt's baseline (legibility verify of 10-29):** your `promptText` sits **1 px higher** than the key HUD's line 1 on desktop (glyph rows 576–589 against 577–590; the key HUD's baseline 591 matches Figma). After HK1 the line hops 1 px when the aim moves between a door you prompt (OPEN DOOR, TAKE KEY, glass) and one the key HUD prompts (LOCKED, E · UNLOCK, BROKEN, E · READ PLAN). Fix on your side: move `promptText` down 1 px on desktop (its anchored y −44 → −45, or align its baseline to 591). Frames: `W/proj_hudkey/Verification/hudkey/cap1029_kp1b_desk/1920x1080/lit/pickup_instant/F_0000.png` against `…/plan/eye_closeup/F_0060.png`.
- **New, the locked door in today's game:** `FrontRoomsLevel0.asset:50` has `doorsNeedKeys: 0`, and `exitLocked` is false on every floor until A13 (`FrontRoomsMapWorld.cs:592, 1262`). So Locked and Unlock do not show in play yet; every Locked capture uses a wood leaf the harness locks. When A13 lands, the real locked door is the steel EXIT leaf: 游戏视觉 then captures the padlock and the prompt on it.

- **O-25 is fixed by your 10:51 rig change (main's working tree, CameraRig bfbdb69b).** Measured in the clone at 11:05: the crosshair, with the eye at rest, is back **0.150 s** after E-back, where the 10-24 build measured 0.533 s. Commit it with the rest.
- **O-26. The EXAMINE gate uses a raw Relay check.** `RelayBlocksInspect()` checks the relay state, but DECISIONS #16 asks for `ThreatPerceivable`.
- **O-27. The first-key K hold is not in main** (#18). When it lands:
  - during the hold, set `aimKind = Examine` with the words `E · EXAMINE KEY`;
  - call `PickupFlight(..., shotClock, takeOffClock)` when the hold ends.
- **O-28. The Read / Examine split is a proposal.** It only changes the touch verb.

## For 交互设计 (the phone prompt glyph, X2)

After HK1, the game's `promptText` is empty whenever the key HUD owns the words: Locked, LockedHaveKey, Broken, Read and Examine. The key HUD draws its own two lines.

`UpdateMobilePromptGlyph` shows only when `promptText.text` is not empty, so for those kinds the mini USE glyph disappears.

What it should do:
- Keep the glyph for the map's own prompts: Open, Shut, TakeKey and Glass.
- For the kinds the key HUD owns, either:
  - leave the glyph hidden (the safe default), because the USE button already carries the verb; or
  - put the glyph in front of the key HUD's line 1, using a small getter. 游戏视觉 adds it in a follow-up on request.

Which of the two is 平面视觉's call.
