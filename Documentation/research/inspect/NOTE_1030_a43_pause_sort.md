# A43: which close-ups pause (2026-10-30)

*From 游戏视觉 (account A, session local_5656cffd), package `inspect_a43`. Spec for code in `FrontRoomsInspectDef.cs` (`pause`, `noThreatGate`, `Pauses`) and `FrontRoomsInspect.cs` (`CurrentPauses`, `PausesWorld`, `WantsEnd`, `RequestEnd`, `WorldRuns`).*

## The rule

**Red, 2026-10-30 13:53, 系统设计 chat (verified):** "所有暂停的逻辑只存在于，玩家操作任何需要仔细阅读的情况下". Only careful reading pauses the game.

**C36 (13:52):** the door-window peek runs live, with no threat gate and no forced pull-back. It ships with the door-vision package. Here we only provide the per-target flag.

**How the code decides** (`InspectDef.Pauses`). 交互设计's rule (briefs/visual.md 13:59) is adopted as it stands, because the code agrees with it:

1. **An explicit `pause` wins.** `"pause"` always pauses, `"live"` never does. Today only the office terminal sets it, to `"pause"`.
2. **Otherwise the verb decides.** A `read` target pauses only when its transcript has **more than one printed line**. Region heads (`\u0002`) are not printed lines. The plans and the station sheets pause under this rule.
3. **Everything else runs live:**
   - every `look` target (the panel and object kinds default to `look`);
   - a `read` target with no transcript, or with a one-line transcript, because it is read at a glance.

So the prompt verb tells the player whether time stops: READ pauses, LOOK and EXAMINE KEY do not. The station TV is the one exception: its prompt is READ SCREEN, but it has no transcript, so it runs live.

**The threat gate** (`InspectDef.noThreatGate`, false by default) applies to every target today. Gated targets get two behaviours:

- **The prompt hides** while a threat is perceivable. This is the game's existing `CanShowInspect` check, unchanged.
- **A live close-up ends** at once on a threat:
  - `FrontRoomsInspect.WantsEnd` becomes true when `FrontRoomsMapHunter.ThreatPerceivable(render camera)` is true (the same reading as the prompt gate);
  - the rig's `CancelForRelay` also lets the shot go, on the hunter's `Threat.Any`.

The code shows nothing wrong with keeping the gate on live looking close-ups, so it stays on them. Only the peek sets `noThreatGate = true`:

- its shot is built with `ShotCancel.None`;
- `WantsEnd` never fires on a threat for it.

## The sort

| # | Target | Where it comes from | Kind / verb / transcript | A43 | Threat gate | Why |
|---|---|---|---|---|---|---|
| 1 | Evacuation plan, tutorial sheet A and Phase 0 | `Kit_EvacPlacard` sidecar (ship) | flat / read / `placard_q16`, 15 printed lines | **READING, pauses** | yes | The tutorial is read here: plan, legend, four alarm rules, title block. This is the careful reading A43 keeps. |
| 2 | Station placard: the room sheet and its phase-0 sheet | `FrontRoomsPlacardMount` (sidecar + `StationPlacardTranscript`) | flat / read / `placard_station_room` and `_phase0`, 16 lines | **READING, pauses** | yes | Same plan, plus the security-desk key line. |
| 3 | Office terminal pages (C19) | `FrontRoomsTerminalScreen.ReadDef()` (terminal_a) | flat / read / `terminal_panel` (none on a phone) / **explicit `pause`** | **READING, pauses** | yes (its own live-mode leave stays) | 系统设计: terminal READ stays a pause. The transcript is written only while the terminal is read, and never on a phone, so the derived rule cannot see it. Hence the explicit flag. |
| 4 | Memos | Read on the terminal (`Resources/Terminal/Content/interoffice.json`) | as #3 | **READING, pauses** | yes | A memo is a terminal page today. A paper memo kit, if one comes, gets `read` and its transcript, so it pauses by the rule. |
| 5 | Station paper: daily activity report (DAR) | Catalog transcript `station_dar`; **no kit registers it yet** | flat / read / 6 lines | **READING, pauses** | yes | A log to read line by line. |
| 6 | Station paper: key sign-out sheet, visitor register | `station_sign_out`, `station_register`; **not registered yet** | flat / read / **1 line today** (the column header) | **READING: register with `pause = "pause"`** | yes | A form whose entries are read. Until the atlas prints the rows, the transcript holds only the header, and the derived rule would make it live. The sidecar must carry `"pause"` (tested). |
| 7 | Station key plate | `station_key_plate`; not registered yet | flat / read / 2 lines | **READING, pauses** | yes | The engraved warning ("SILENT ALARM: REMOVAL SIGNALS PANEL"), as in 交互设计's table. |
| 8 | Station TV ad (R3) | `FrontRoomsScreenVideo.StationTvDef()` | flat / read / no transcript | **LOOKING, live** | yes | The ad is watched, not read carefully (交互设计's rule: no multi-line transcript). Its bank already plays through the close-up, and now the world does too. |
| 9 | Door sign (READ SIGN) | `Kit_DoorSign` sidecar (ship false, test bed) | flat / read / no transcript | **LOOKING, live** | yes | One label, read at a glance (cap 17 mm). |
| 10 | Keys (EXAMINE KEY, D1.6) | `Kit_KeyTag_*` sidecars, 9 of them (ship false) | object / look | **LOOKING, live** | yes | Turning a key over is looking. Its number is read at a glance. |
| 11 | First-key K hold (C21) | `FrontRoomsInspect.OpenHeld` with the key-tag block (fallback: object / look). **Not wired:** only the Editor calls it | object / look | **LOOKING, live** | yes | The same key, from the hand. Whoever wires C21 must hold DoorShot's key timeline and `PlaceKey` while `FrontRoomsInspect.HeldOpen` (contract README). |
| 12 | Wall clock | Catalog fallback `Kit_WallClock` (ship false) | flat / look | **LOOKING, live** | yes | You glance at a clock. |
| 13 | Vending machine | Catalog fallback `Kit_VendingMachine` (ship false) | panel / look | **LOOKING, live** | yes | Looking through the window. |
| 14 | Copier console | Catalog fallback `Kit_Copier` (ship false) | panel / look | **LOOKING, live** | yes | A console to look at. |
| 15 | Desk phone | Catalog fallback `Kit_DeskPhone` (ship false) | object / look | **LOOKING, live** | yes | An object turned in the hand. |
| 16 | Door peek (C36) | Door-vision package (`InspectKind.Peek`; clone `proj_doorvision`, **not in main**) | peek / look (PEEK) | **LOOKING, live** | **no: `noThreatGate = true`** | Red, C36: live, no threat gate, no pull-back. The Relay cannot see the player, but it can hear them. Wired by door vision; see below. |

**Counts:**

- 7 READING rows: #1 to #7. The plans and the terminal are live in main; #5 to #7 wait on a kit.
- 9 LOOKING rows: #8 to #16. Only #8 ships today; #9 to #15 are dev or test-bed targets; #16 is in a clone.

## What a live close-up does (with the map's contract applied)

**Everything that ran at timeScale 0 still runs the same way.** It all runs on the inspect clock (`FrontRoomsInspect.Dt`), which has always been unscaled:

- the camera rig shot (ticked once, with the world, in `UpdateMapPlay`);
- look, pan, zoom and turn;
- the hints;
- the eye (FrontRoomsKeyHud, unscaled time; Opened and Closed);
- the transcript;
- R3's close-up clock (`VideoTimeUpdateMode.UnscaledGameTime`, which at timeScale 1 runs at game speed).

**What is new:**

- **The eye's light is re-read every frame** while the world runs, so the eye adapts to a W1 lamp dip. A paused close-up still reads it once, at the open.
- **The CRTs keep playing** around a live close-up. C26's `WorldHeld` now holds playback only for a close-up that pauses. Focus and E stay held for any close-up (`InputHeld`).
- **A shot the rig takes away is handled like any return.** This covers `CancelForRelay`, which at Camera motion Off can end the shot within one tick:
  - the look lets go;
  - `Closed` fires;
  - `WantsEnd` asks the game to end its Phase.Inspect.

**Without the contract nothing changes in play.** The game still sets timeScale 0 for every close-up, so:

- `WorldRuns` is never true;
- `WorldHeld` equals its old value;
- no threat check runs.

## Not built here (owners)

**Door vision (us, later):**

- set the peek def's `noThreatGate = true`;
- end the peek when its leaf moves, with `FrontRoomsInspect.RequestEnd()`;
- skip the open gate's `ThreatPerceivable` for an ungated target (`FrontRoomsInspect.ThreatGated(t)`; a one-line change in `CanShowInspect`, map-owned);
- PEEK only within 1.0 m and only on shut leaves.

**交互设计, once the flag lands:**

- no TEXT in a live close-up (read `FrontRoomsInspect.WorldRuns` or `!CurrentPauses`);
- no back latch after leaving a live close-up;
- no pause button inside any close-up.

**声音设计:** `InspectPausedBuses` (Foley, mechanism, music, subjective) and the Relay bus at −6 dB must follow `FrontRoomsInspect.WorldRuns` at `Inspecting(true)`, not the close-up alone. In a live close-up the threat must stay audible.

**关卡设计:** the A43 contract: `W/apply/inspect_a43_contract/` (README, CONTRACT_game.diff).
