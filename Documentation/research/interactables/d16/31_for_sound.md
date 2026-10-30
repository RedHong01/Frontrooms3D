# D1.6 doors · locks · keys: note for 声音设计

**From:** 游戏视觉, package stage of x12-d16-fix3 (account A, 2026-10-29 20:0x). **For information. Nothing is asked of you.**

D1.6 adds no sound. There are no FMOD events, no banks, no `SoundIds`, and none of your files are touched. The kit is render only: double-acting doors on "Leaf rig" and the real key (ring + key + tag) instead of the cube.

What reaches play:
- The visual package `W/apply/d16.sh` is inert for doors and keys, because nothing calls the kit yet.
- The change becomes live when 关卡设计 lands map contract v9 (`data/contract_map_D16.diff`, README `W/apply/d16_contract/README.md`).

Below is what changes around your hooks at that point. All of it is measured on the clone (main 1831bded + tree with the first room on + the package + v9, 19:52–19:58).

## 1. Key pickup: `KeyTaken` moves from the press to 0.25 s

| | Today (cube key, instant path) | With contract v9 (dressed key, presented: P4 / KP-1) |
|---|---|---|
| E pressed | `KeyPickupStarted(zone, pos)` and `KeyTaken(zone)` in the same frame; the cube is gone | `KeyPickupStarted(zone, pos)` only. The real key starts to travel from where it rests to the hand |
| `KeyTaken` | the press frame | `FrontRoomsShotTimings.KeyPickup.Arrive` = **0.25 s** after the press, when the key reaches the hand (measured 0.267 s in the autopilot, at frame granularity) |
| `KeyHandoff` (the HUD takes over) | — | 0.75 s. The 3D key hides on that frame. The HUD's own cues (`PickupFlight`, `PickupLand`) run on the HUD clock as now |
| `DoorShot.Beat(KeyPickup, Pocket)` | — | 0.85 s (`PocketSound`). Your code leaves it unbound; nothing changes |

- Your `Foley/Player/KeyPickup` (2D, on `map.KeyTaken`) will play at about 0.25 s, as the key arrives in the hand, instead of at the press. This needs no change from you. If one of your checks expects it in the press frame, that check will see it 0.25 s later.
- **The station floor key** ("Key · floor"):
  - `StationKeyTaken` is still raised **at the press**, inside `TakeKey` and before the presenter. So S4 (the TV's power-off on the take) stays at the press.
  - station-look-v1's C-3 asked 关卡设计 to raise it at M3's `KeyTaken` instead (0.25 s after E). If they do that, S4 moves to 0.25 s with it. That is their call; v9 does not move it.
- With v9 the floor key **hangs on the switch plate's hook** in the station (1.42 m up, ring on the hook), and is lifted off it when taken.
  - `KeyPickupStarted` carries the key's position.
  - If you ever want a sound of the key leaving the hook, that is the hook to use. This is only a fact, not a request.

## 2. The unlock shot on a dressed door

- **Positions:** `FrontRoomsDoorShot.Beat` (KeyInsert / KeyTurn / BoltRetract / KeyWithdraw / KnobTurn / KnobReturn) still passes `lockPose.position`. On a dressed door, `map.LockPose` now reads the rig's keyhole (`IFrontRoomsDoorRig.Keyhole`). That is the same spec point the cube uses (1.000 m up, 0.920 m from the hinge, the leaf's face + 6.5 mm). On a shut leaf the two agree within 1 mm (`FrontRoomsDoorRigTests`, keyhole both faces).
- **Timing:** unchanged.
- **Picture:** the kit's plug turns and its bolt draws on those same beats. After `DoorUnlocked`, the deadbolt stays drawn for the rest of the run, from both faces.
- **Your `keyInLock` guard** (a cancel can raise KeyWithdraw before the key reached the keyway) still applies as written. The kit's key follows the same beats.

## 3. Door events: what the picture now does on them

The rig adds picture beats to map events you already have. It plays no sound and adds no event (`FrontRoomsDoorDressEvents`).

| Map event | Picture (kit) | You today |
|---|---|---|
| `DoorHandleTurned` | lever / knob down in 0.08 s, back over 0.10 s once the hinge passes 0.5° (at most 1.5 s held) | handle Foley |
| `DoorRattled` (the same try as `DoorLocked`) | knob down 0.05 s, held to 0.20 s, back by 0.40 s | `DoorLocked` one-shot |
| `DoorLatched` | latch click, 0.06 s | close compound (LatchStrike) |
| `DoorBrokenFrom` | the strike plate comes off and falls for 0.4 s | your `DoorBroken` crash covers it |
| `DoorBrokenTried` | the hanging leaf wobbles 0.6°, 2.4 Hz, 0.3 s | not subscribed. The map's own doc for this event mentions "a hanging-leaf Foley"; the picture now has the wobble if you want it |
| `DoorUnlocked` | deadbolt drawn for the run | (the unlock shot's beats) |

Two facts about the doors themselves:
- The doors are double-acting (A12 / A19) with **no closer and no motor**: the kit leaf never self-returns. This matches your RULE4 wooden-door Foley.
- The kit leaf's visual pivot sits 36 mm into the opening. Your sounds are placed from map positions and colliders, which do not move.

## 4. Not sound

None of these touch audio:
- the held-key fill light;
- the HeldItem layer;
- the LOD re-exports;
- the tag type (held until 平面视觉 signs off);
- the shot look (V4 / V5).
