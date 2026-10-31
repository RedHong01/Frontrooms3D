# 53: Door break, asks for 声音设计 (2026-10-30)

From 游戏视觉 (door-break production, P6). These are written asks only; no sound file, FMOD bank or sound script is touched by the door_break package, and nothing here is a message to another session. Rule 24 holds on our side: the film harness and the video are silent, and beats are burned in as captions.

Design: `50_rethink.md` §3.10 (S1-S5), `54_dust_grit.md` (S6), log `52_production.md`. Red approved the break 2026-10-30 (`50` §6).

## What the visual gives you once `door_break` lands

Two static events on `FrontRoomsDoorBreakVisual`. Neither has a subscriber today. Render-only code never reads them back, so binding them cannot change gameplay.

| Hook | Signature | When |
|---|---|---|
| `DoorStageChanged` | `Action<FrontRoomsMapWorld.Door door, int stage, bool steel>` | On the swap frame of a NEW stage: 0 E, 1 M, 2 D1, 3 D2, 4 FINAL, 5 BROKEN. A kept stage that is struck again raises nothing. A restore raises nothing. |
| `DebrisContact` | `Action<Vector3 position, float speed, string surface, string piece>` | Each contact of the baked throw as its time passes. `surface` is `leaf` or `carpet`. Never on a restore. A phone drops the pieces it does not show. |

Other read-outs that already exist:
- `FrontRoomsMapHunter.BlowIndex` / `BlowCount`, as `FrontRoomsSoundDirector.OnDoorBlow` reads them today.
- `door.rig as FrontRoomsDoorRig` and `GetComponent<FrontRoomsDoorBreakVisual>()`, then `.Steel`.
- With the map contract's M1: `FrontRoomsMapHunter.DoorBlowStruck(door, index, count, from)`, one blow with its door.

The stage clock counts back from the last blow: `stage = max(0, 4 - (count - 1 - index))`. The shown stage never goes down, because damage is kept.

| Blows | Ladder | Where |
|---|---|---|
| 5 | E, M, D1, D2, FINAL | v1 T1; v2 T1, T2 |
| 4 | M, D1, D2, FINAL | v1 T2, T3; v2 T3, T4, T5 |
| 3 | D1, D2, FINAL | v1 T4, T5 |
| 6 (steel, M7) | E, E, M, D1, D2, FINAL | a locked steel door at v1 T1 (locked doors take 6 / 5 / 5 / 4 / 4 blows on T1-T5) |

## The asks

**S1. Layers on `Door/Blow`.** Add the parameters `Stage` (0 to 4) and `DoorType` (0 wood, 1 steel), keeping `Damage`.
- Wood: E boom; + creak (M); + split crack (D1); + splintering tear (D2); + tear-out with screws (FINAL).
- Steel: boom and ring; buckle; sheet tear; shriek.
- `DoorType` = 1 is `FrontRoomsDoorBreakVisual.Steel` (families `Kit_DoorLeafBreak_Steel_DA`, `_SteelLite_DA`).

**S2. A crack layer on the swap frame (±1 frame).** Bind `DoorStageChanged`. The picture changes on exactly that frame, with a hit-stop of 2 / 2 / 3 frames at D1 / D2 / FINAL.

**S3. `Mechanism/Door/DebrisImpact` (new): size class, speed, surface.** Bind `DebrisContact`.
- The contacts are baked, so their times are known in advance and exact: `Assets/Resources/Props/Models/Kit_DoorLeafBreak_{Veneer,Steel}_DA_throw{0..3}.json`, key `contacts`.
- Each throw holds 59 to 78 contacts, from `t` 0.10 to about 1.04 s after the FINAL blow: at most one `leaf` contact per piece, and the rest `carpet`. The `leaf` speeds run up to about 14.6 m/s (the leaf's own speed at that point). `carpet` speeds are the piece's landing speed. You will want to thin them.
- Size class comes from the piece's name: `deb_strip`, `deb_spl_*`, `deb_crm_*` (veneer); `deb_chip_*`, `deb_gyp_*` (steel).
- Surface today is `leaf` or `carpet` only. A hard floor shows on the door's record (`HardFloor`), if you want a VCT variant.

**S4. Break: a pivot-shear layer (metal and screws).** `OnDoorBroken` plays `StopLimit` at once, on the break. The leaf really stops 0.12 s later (`FrontRoomsShotTimings.DoorBreak.ThrowSeconds`): with the map contract's M4 a dressed leaf hits the casing at 95.4° there, and the apex is the wall hit. The pivot shear belongs on the break, and `StopLimit` belongs 0.12 s after it.
- Frame layers worth having at the same beat: the wood jamb splits at FINAL (`Kit_DoorFrameBreak_Wood_DA`); on steel the strike dents and the brush fin crushes at D2 and FINAL (`Kit_DoorFrameBreak_Steel_DA`).

**S5. `BrokenTried`.** Pose (a) is approved, so the hanging leaf grinds on its bottom pivot, together with the existing `BeatWobble`. Pose (b)'s creak is not needed.

**S6 (optional, from `54`).** A faint sift layer: grit keeps falling for about 3 s after the break. Only if it earns its place.

## What this asks of you, and what it blocks

Nothing here blocks the landing. Without S1-S5 the break plays today's `Door/Blow` and `StopLimit`, and the picture is complete.

The order we would suggest is S2 (the crack on the swap frame gives the most for the least), then S1, then S3.
