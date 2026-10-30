# 21 · Touch contract for HUD key B (游戏视觉 → 交互设计)

**STATUS 2026-10-29 (交互设计, verified in main by 游戏视觉):** T-1 and T-2 LANDED: `UseKind.Unlock` and `UseKind.Examine` appended (`FrontRoomsTouchControls.cs:63`, labels in `FrontRoomsTouchControlsView.cs:1149`). EXAMINE when `InspectKind.Object`, else READ; `UseKindOf` maps `AimKind.Read` / `Examine`, and the verb comes from the kind, never from the prompt's words. T-3: the default is kept, no code. O-28: the READ / EXAMINE split stays. T-4 noted; `FrontRoomsHandheld.ReduceMotion` unchanged. 交互设计's tests: layout 896/0, touch EXAMINE 51/0. Still waiting: 关卡设计's HK1 + KP-1.

游戏视觉, package stage, **2026-10-29 14:2x**. These are 交互设计's files (`Assets/Scripts/Input/*`, the touch part of `FrontRooms3DGame.Mobile.cs`). 游戏视觉 does not edit them. This note says what the USE button and the phone prompt glyph should do once HUD key B (`W/apply/hud_key_b.sh`) and 关卡设计's wiring HK1 + KP-1 (`contract_map_HK1.md`) land. Nothing here blocks that landing: without these changes the phone still works, with the two verb gaps listed in T-1 and T-2.

Read on main's files of 14:2x: `FrontRooms3DGame.Mobile.cs` 6869ef97, `Input/FrontRoomsTouchControls.cs` 965092ec, `Input/FrontRoomsTouchControlsView.cs` 0d7900e8, `FrontRoomsMapWorld.cs` a8a83b0b.

## The rule (crosshair grammar §6, hook H4)

**The USE verb comes from the kind, never from English strings.** The kind is `FrontRoomsMapWorld.AimKind` (grammar H1, in main since MapWorld c0946262): `None, Open, Shut, Locked, LockedHaveKey, Broken, Glass, GlassTap, TakeKey, Read, Examine`. The prompt's words (`prompt`, `UsePrompt.Label`) are data to show, not something to search. Main already works this way (`UseKindOf(AimKind)` and the view's `UseVerb(prompt.Kind)`), so the items below only fill two gaps and fix what the key HUD changes.

| Kind (grammar H1) | USE verb (grammar §6, strings DECISIONS #29) | Main today | Change |
|---|---|---|---|
| `Read` | **READ** | READ (`inspectAimed != null` → `UseKind.Read`) | keep (T-2 picks it by kind) |
| `Examine` | **EXAMINE** | READ (no Examine verb yet) | **T-2** |
| `LockedHaveKey` | **UNLOCK** | OPEN ("until the A22 strings land") | **T-1** |
| `Broken` | **BROKEN** (a refusal; a press is the won't-shut try) | BROKEN | keep |
| `Locked` | LOCKED (a refusal; a press rattles) | LOCKED | keep |
| `Open` / `Shut` | OPEN / SHUT | same | keep |
| `TakeKey` | TAKE | same | keep |
| `Glass` / `GlassTap` | BREAK or TAP (the TapToBreak setting) | same | keep |
| in a close-up | BACK (EXAMINE T2) | same | keep |

## T-1 · UNLOCK on a locked door you hold the key for

- The strings are decided: phone `UNLOCK` (DECISIONS #29, Red's "全部按推荐" of 10-22). The key HUD's phone line 1 already reads `UNLOCK` there (`FrontRoomsKeyHud.Strings.Unlock` without its "E ·"), over `STAIR KEY`.
- Add `UseKind.Unlock`, appended at the END of the enum so the existing values keep their numbers. Give it the verb `"UNLOCK"` in `UseVerb`, and map `AimKind.LockedHaveKey → UseKind.Unlock` in `UseKindOf`.
- It is not a refusal: the press is the key turn (`UseResult.KeyTurn`), which starts the unlock shot. So it keeps the solid "go" look and is left out of `refused`.
- During the shot, USE fades with `rig.HudFade` as it does now (`SetTakeover`). Only the key PANEL is exempt from the shot's fade (DECISIONS #14, mode (d)); USE is not, so it never offers a verb a press won't do.

## T-2 · EXAMINE on an object close-up, READ on a face

- Pick Read or Examine with the same predicate the key HUD gets in HK1's M-2: `inspectAimed.Kind == InspectKind.Object ? Examine : Read` (`InspectKind { Flat, Panel, Object }`). An object close-up, such as the key, is EXAMINE. A plan, a screen or a sign is READ.
- After HK1, `FrontRooms3DGame` has a field `aimKind` (set in `UpdateAim` before the prompt line). Since Mobile.cs is the same partial class, you can key USE on that field directly instead of re-deriving it. Today's local `var aimKind` in `UpdateMobileTouchPrompt` shadows it, which compiles. Rename the local, or read the field, whichever you prefer.
- Add `UseKind.Examine`, appended at the end, with the verb `"EXAMINE"`. Its press is E's (opens the close-up), exactly as for READ.
- The first key of a run, during its EXAMINE K hold (#18, not in main yet, O-27), is `Examine` with `E · EXAMINE KEY` on the HUD, so USE says EXAMINE there too.
- O-28: the Read / Examine split changes only this verb. If you and 平面视觉 prefer one verb for both, say so and the key HUD needs no change: its eye is the same for both.

## T-3 · The mini USE glyph in front of the prompt (`UpdateMobilePromptGlyph`)

After HK1, the game's `promptText` is empty whenever the key HUD owns the line: Locked, LockedHaveKey, Broken, Read and Examine. The key HUD draws its own two lines in the same slot (on the phone, line 1's baseline is at 717 px on the 3× canvas). `UpdateMobilePromptGlyph` shows only when `promptText.text` is not empty, so for those kinds the glyph hides by itself.

- Keep the glyph as it is for the map's own prompts: Open, Shut, TakeKey and Glass (the hold ring glyph).
- Locked and Broken already hide it (refusals).
- For UNLOCK, READ and EXAMINE, the safe default is to **leave it hidden**: the USE button carries the verb. If 平面视觉 wants the glyph in front of the key HUD's line 1 there, 游戏视觉 adds a small getter for line 1's drawn rect in a follow-up, on request. That is 平面视觉's call.
- G6 (DECISIONS #38): HK1's M-9 changes `promptText`'s keyline from `2f` to `FrontRoomsHudStyle.PromptKeyline(mobileHud)`. That gives 2 px on desktop, as now, and the 1 pt of #23 on the phone, where `2f` meant 6 px and the Outline copies ghosted (`images/VL303b_g6_prompt_keyline.jpg`). The glyph copies `promptText.color`, so nothing changes for it.

## T-4 · What else the key HUD touches on a phone (no change needed from you)

- **The key panel's place:** the key HUD reads the map's `keyPanel` rect, safe area included, and puts its panel there (top-left on the phone). So `HudKeepOut` (`AddHudRect(keyPanel)`) still covers it, and the map's `keyPanel` object keeps its rect even though it stays inactive.
- **The P4 flight** (KP-1b on today's instant pickup, the walk-over or your key tap): the B glyph is held 0.5 s at the hand position (0.65, 0.30) at 1.117× (#24), then flies 0.72 s into the top-left slot. It never takes input (`blocksRaycasts = false`), so it cannot catch a touch on the way.
- **Reduce Motion:** the key HUD reads `FrontRoomsHandheld.ReduceMotion` (the OS setting or Camera motion 0 %). Every state then becomes an alpha cross-fade of 0.15 s or less, and the flight becomes a fade-in at the slot. Please keep that property's name and meaning.
- **Haptics:** none for the eye (grammar §6). Locked and Broken keep yours. The key HUD raises no haptic events.
- **Keyline:** `FrontRoomsHudStyle` v3 only appends `PromptKeylineDistance` and `PromptKeyline(bool handheld)`. `KeylineColor`, `KeylineAlpha` and `KeylineDistance`, which your X2 labels read, are unchanged.

## Tests to add on your side (suggested)

- USE says UNLOCK on a `LockedHaveKey` door, and a press starts the unlock shot.
- USE says EXAMINE on the key close-up (`InspectKind.Object`) and READ on the plan.
- No verb is ever chosen from `prompt` text: switch the prompt strings and the verbs do not change.
- With HK1: the mini glyph shows for OPEN DOOR / TAKE KEY / HOLD, and hides for LOCKED / UNLOCK / BROKEN / READ / EXAMINE (the default in T-3).
