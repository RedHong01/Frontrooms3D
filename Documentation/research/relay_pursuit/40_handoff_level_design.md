# Pending for 关卡设计 (the map chat), from 系统设计

*Written 2026-10-14 because the map chat was offline. Read this on resume and delete each item once it lands. The rules live in `Documentation/RELAY_PURSUIT_REDESIGN.md`.*

## 1. Broken-door hooks (Red, 2026-10-14)

Red: "a broken door cannot be shut, and the crosshair shows that it is broken." The rule is in §10, Doors → "Broken doors". Two hooks are needed in `FrontRoomsMapWorld.cs` (your file).

1. **`Describe`:** for a broken door (`door.broken`, ~:2453), return a broken prompt **kind** instead of null. The HUD keys its crosshair state on the kind, not on the string.
   - While `IsBeingBroken` or `IsHeldForKey`, it stays null, as today.
   - The strings ("BROKEN" / "WON'T SHUT") are 平面视觉's, settled with Red.
   - How the kind is carried (an out param, or an enum beside the string) is up to you.
2. **`Use`:** a broken door returns a new `UseResult.Broken`, appended to the enum (today it returns `None`, ~:2478).
   - Rate-limit it with a 550 ms guard: 平面视觉's "won't shut" reaction is 550 ms, so ignore repeat E within that window, as with `Rattle.Duration`.
   - **No Relay noise and Attention 0.**

Already true in main, so no change is needed:
- `brokenDoors` persists by edge across rebuilds and revisit shifts (:443, :1453-1456).
- The passage is Open (:1649).
- The path field gives 0 for a broken door (:949).
- A broken locked door already lets everyone through, because `Use` returns before the lock check.

Tests (§10):
- E on a broken door never changes its state and never makes a Relay noise.
- A broken door stays broken through a rebuild and a revisit shift.
- Its passage is Open, with 0 m in the path field.
- A broken locked door lets a keyless player through.

Touch: tell the touch chat that the USE button needs the same broken state.

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

## 3. Crosshair hooks (from `research/computer_ui/30_crosshair_grammar.md` §7, 2026-10-15)

- **H1:** `Describe` returns a kind enum, with the string as data: `None, Open, Shut, Locked, LockedHaveKey, Broken, Glass, GlassTap, TakeKey, Read, Examine` (later `Pry`). Section 1 of this file's broken kind is one value of it. The touch layer keys its USE verbs on the kind.
- **H2:** set the locked rattle guard (`Rattle.Duration`) from 0.40 s to 0.50 s. The HUD deny window is already 0.50 s, so one press then gives exactly one 3D rattle and one HUD tug.
