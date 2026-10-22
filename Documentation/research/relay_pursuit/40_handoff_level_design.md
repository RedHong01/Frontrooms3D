# Pending for 关卡设计 (the map chat), from 系统设计

*Written 2026-10-14 because the map chat was offline. Read this on resume and delete each item once it lands. The rules live in `Documentation/RELAY_PURSUIT_REDESIGN.md`.*

## 1. Broken-door hooks: LANDED 2026-10-21 (关卡设计)

In main (`FrontRoomsMapWorld.cs` c0946262, `FrontRoomsShotTimings.cs` e7e027b4): `Use` on a broken door returns the appended `UseResult.Broken` and raises `DoorBrokenTried(door, player position)`, with no door event, no Relay noise and no state change; a try within `FrontRoomsShotTimings.BrokenDoor.Duration` (0.55 s) of the last returns None. `Describe` still gives a broken door no prompt (the strings are 平面视觉's); its kind is `AimKind.Broken` (§3). The four §10 tests are in `FrontRoomsMapInteractionTests.BrokenDoors` (12 checks, 163/163). Rules: `MAP_GENERATION.md` → Broken doors. Told: 交互设计 (USE needs the kind), 平面视觉 · 信息设计, 系统设计.

Red decided §16 Q9 on 2026-10-14: broken doors stay broken for the run, and v2's rehang is dropped. Never build a repair path, apart from the test tool `ResetDoorForTools`.

## 2. Place the door, lock and key kit in the map (Red, 2026-10-15: D1.5 go)

Red, quoting "the 57 formal models are built, but the map hasn't placed them yet": "把这个放进去" (put it in).

- **Kit assets:** in main since the interactables_kit merge. Nothing spawns them yet (`research/interactables/40_merged.md` §7).
- **Your part (D1.6):**
  - DressDoor: kit leaf and hardware as children of the map's door objects. Colliders stay the map's.
  - The hinge pivot fix A2 (`FrontRoomsMapWorld.cs` ~1441-1443).
  - Key hosts: `Kit_KeyHookBoard` in Level 0 rooms, `Kit_KeyCabinet` in Office, the single hook only where the sight line is ≤ 3 m. Hosted keys do not spin.
  - Bind the rigs through `FrontRoomsKitSeam.cs`.
- **游戏视觉's part:**
  - `FrontRoomsInteractableKit.Door.cs` / `.Key.cs` (the rigs);
  - the missing materials;
  - the head-dip numbers.
- 游戏视觉 has been asked to prepare your part as a contract diff in its clone, so you can review and merge it. Check `W/apply/` and its message before starting from scratch.
- **Locked doors stay off** (`doorsNeedKeys` 0). That is a separate call: about 1 door in 3 by default, and only once the key-reachability check exists.

## 3. Crosshair hooks: LANDED 2026-10-21 (关卡设计)

- **H1:** `FrontRoomsMapWorld.AimKind { None, Open, Shut, Locked, LockedHaveKey, Broken, Glass, GlassTap, TakeKey, Read, Examine }` (append only; `Pry` later), read with `Kind(collider, from, out keyZone, out haveKey)`, the name and signature KeyHud B's contract (M-6) already uses. `Describe` keeps the string. Not None exactly when `Describe` has a prompt, plus Broken. Read / Examine come from EXAMINE's aim registry. The game does not read the kind yet: KeyHud B's M-6 wiring does (map side, when 游戏视觉 lands FrontRoomsKeyHud; its `AimKind.Locked` test becomes Locked or LockedHaveKey).
- **H2:** `Rattle.Duration` 0.40 → 0.50 s.
