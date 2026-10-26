# 40 · Wiring contract for HUD key B (for 关卡设计)

游戏视觉, 2026-10-22 21:5x; **re-based 2026-10-24 11:4x as v2.6** on main's FrontRooms3DGame.cs 2ebd70b9 (11:26). This is a contract, not a landing: these files belong to 关卡设计 (FrontRooms3DGame.cs, Scripts/FrontRoomsShots/*). 游戏视觉 has applied them only in its clone `W/proj_hudkey` to test them.

## What lands first, and what it does on its own

1. `W/apply/hud_style.sh` was landed at 15:52. It adds FrontRoomsHudStyle (DECISIONS #31).
2. `W/apply/hud_key_b.sh` is visual-owned. It adds 161 new paths:
   - FrontRoomsKeyHud and its motion, parts, vector graphic and text layout in `Assets/Scripts/Rendering/UI/`;
   - the `FrontRooms/UI/HudVector` shader and 74 part PNGs in `Assets/Resources/UI/KeyHud/`.

   It is stacked on hud_style. **On its own it changes nothing:** nothing calls `FrontRoomsKeyHud.Attach` yet.
3. **This contract** switches the game over to it. In play, the crosshair then uses the approved set:
   - the dot;
   - L3·D, D·3 and famOut on a locked door;
   - U·3 on the panel under the unlock shot (d), and F·1 for the padlock;
   - B·4 on a broken door;
   - E·1 on Read and Examine targets;
   - P4 when a key is picked up.

   The map's own prompt line steps aside only for the states the key HUD draws (`OwnsPrompt`). The legacy key panel stays off while the key HUD is attached.

## Files (bases = main as of 2026-10-24 11:26: Game.cs 2ebd70b9, DoorShot b2014396, MapWorld ca24a280)

v2.6 changes no hunk. Every v2.5 hunk applies unchanged to today's Game.cs (交互设计's handheld startup curtain, the desktop settings view, the phone HUD canvas at `FrontRoomsHandheld.PointScale` with the corner give-way). Only the line numbers moved. v2.5 is kept next to the masters as `*_1022h_v25.diff`.

| File | Base | What |
|---|---|---|
| `FrontRooms3DGame.HK1.diff` | Game.cs **2ebd70b9** | **HK1**, Game.cs only, 14 hunks. It does the following: <br> - attaches the key HUD (M-1); <br> - passes `AimKind` and the words into `FrontRoomsKeyHud.SetAim` (M-2, grammar H1). An EXAMINE target sets Examine for an object and Read for a flat face or a panel. <br> - hides the map's prompt line only while `OwnsPrompt` is true (M-2); <br> - makes the key HUD own the panel; <br> - sends the E events: `UseResult.Rattled` → `NoKey`, `KeyTurn` → `Unlocking`, `Broken` → `BrokenTry` (M-4); <br> - applies the takeover fade (M-3) and adds #14's one line in `ApplyGameplayHudAlpha`; <br> - adds `HudIdentity` = `StubIdentity(zone, floor − 1)` until D1.6 brings the zone rule. It compiles without KP-1. |
| `KP1_handoff.diff` | HK1 result + DoorShot **b2014396** | **KP-1**, the P4 hand-off. DoorShot raises `KeyHandoff` at `KeyPickup.HoldUntil` (2 hunks); Game.cs subscribes and calls `PickupFlight` (3 hunks). |
| `FrontRooms3DGame.contract.diff` | Game.cs 2ebd70b9 + DoorShot b2014396 | HK1 + KP-1 in one diff (18 hunks). |

**Checked again on 2026-10-24 11:4x (v2.6), as on 2026-10-22 21:4x (v2.5):**
- `patch -p1` applies `FrontRooms3DGame.contract.diff` cleanly to copies of main's two files. The result is byte-identical to the clone's tested Game.cs and DoorShot.cs.
- Roslyn `--all` gives COMPILE OK on macOS and iOS for each of these, with the hud_key_b payload included:
  - main + HK1 + KP-1;
  - main + HK1 only.
- MapWorld ca24a280 is not touched. Its `AimKind` (11 values) and `Kind()` are what the key HUD binds to (DECISIONS #20).

## Open items on the map's side (not blocking the visual landing)

- **O-25. The HUD comes back late after a close-up.** `FrontRoomsCameraRig` holds `HudFade` at 1 while the Inspect shot blends out. Measured in the clone, the crosshair is back 0.533 s after E-back. The grammar asks for 0.15 s after control returns. This is a rig change.
- **O-26. The EXAMINE gate uses a raw Relay check.** `RelayBlocksInspect()` checks the relay state, but DECISIONS #16 asks for `ThreatPerceivable`. The eye follows the prompt's own predicate, so the two always agree, but the predicate is yours to fix.
- **O-27. The first-key K hold is not in main** (#18): the game never calls `FrontRoomsInspect.OpenHeld`. When it lands:
  - during the hold, set `aimKind = Examine` with the words `E · EXAMINE KEY`;
  - call `PickupFlight(..., shotClock, takeOffClock)` when the hold ends.

  The API for this is ready and checked.
- **O-28. The Read / Examine split is a proposal.** It only changes the touch verb.

## For 交互设计 (the phone prompt glyph, X2)

After HK1, the game's `promptText` is empty whenever the key HUD owns the words (Locked, LockedHaveKey, Broken, Read, Examine). The key HUD draws its own two lines (phone strings `UNLOCK` / `BROKEN` / the READ words).

`UpdateMobilePromptGlyph` shows only when `promptText.text` is not empty, so for those kinds the mini USE glyph disappears.

**What it must do:**
- Keep the glyph for the map's own prompts: Open, Shut, TakeKey, Glass.
- For the kinds the key HUD owns, either:
  - leave the glyph hidden, because the USE button already carries the verb (UNLOCK, BROKEN, READ, EXAMINE); or
  - put the glyph in front of the key HUD's line 1. This needs a small getter on FrontRoomsKeyHud: line 1's rect, its width and its alpha. 游戏视觉 adds it in a follow-up package on request.

Which of the two is a type/layout call for 平面视觉. Until then, the safe default is hidden.
