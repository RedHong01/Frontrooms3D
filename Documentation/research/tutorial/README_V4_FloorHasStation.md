# V4 still desk screens · the `FloorHasStation` contract (for 关卡设计, with M4)

From 游戏视觉, 2026-10-22 (prebuild-integrations, V4 build stage). Agreed with 关卡设计 15:2x; wiring decision (a) by 总控 15:3x ("bound to: this floor has a station"). The build report is `PREBUILD_v4desk.md` in this folder.

## What the game does with it

Tutorial T3 (Red approved, 教程设计 chat 10-22): on a floor that has a station, the Office desk CRTs in the maze stop playing Ad_03 and each shows one still frame (平面视觉 · 广告's v2 art). The station TV stays the only moving screen there. After the key is taken the station TV goes off for good (V1's job), and the desks stay still for the rest of that floor. On a floor without a station, nothing changes: the desks play Ad_03 exactly as today. The title corridor TV (the title stream rooms) is never affected.

## What 关卡设计 adds with M4 (nothing else is needed from you)

One public member on `FrontRoomsMapWorld`:

```csharp
/// <summary>True when this floor's station is planned (M4). Set before Begin; NextFloor and the floor resets clear it.</summary>
[UnityEngine.Scripting.Preserve]
public bool FloorHasStation { get; set; }
```

- **Type and name:** a public instance `bool` named exactly `FloorHasStation`. A property is preferred; a public field of that name also works. Any setter visibility is fine (the reader only reads).
- **`[Preserve]` please:** the reader reads it by reflection only. The WebGL player strips at High (`managedStrippingLevel` WebGL 3), and a getter that no code calls can be stripped there; the attribute keeps it. iOS strips at Low.
- **When to set it:** before `map.Begin(...)` for the floor whose station is planned:
  - floor 1: in `StartRunInPlace`, before `map.Begin(playerRoot, false)` (so before `MapRunStarted`);
  - floors 2+: in `ChangeFloor`, after `map.NextFloor(...)` and before `map.Begin(playerRoot, false)` (so before `FloorChanged`).
- **When to clear it:** `NextFloor` sets it back to false, and so does any `SetExitArea` / `ClearExitArea`-style reset that forgets the floor's plan. A map with no station keeps it false.
- **Do not clear it when the key is taken.** The desks stay still for the whole floor.
- **No call into visual code, and no event.** Map code never references `FrontRoomsStationPresence` or any other visual class, never calls a setter there, and raises nothing for it.

## How the reader uses it (visual side, `Assets/Scripts/Rendering/FrontRoomsStationPresence.cs`)

- `public static bool FloorHasStation { get; }`, namespace-free, owned by 游戏视觉.
- It looks the member up once by reflection (a cached `PropertyInfo`, else a `FieldInfo`). While the member does not exist, it reads **false**, so every desk keeps the ad. That is today's main.
- It refreshes on two events of `FrontRooms3DGame`, both raised after `Begin` (Begin does not build, so no desk exists yet):
  - `MapRunStarted(map, relay)`: floor 1;
  - `FloorChanged(map, relay, floor)`: C22, in main since 15:39. The reader subscribes to it directly (`FloorChanged += ...`), so **please keep this event and its signature**; the package dry run refuses if it is missing. Until the 20:3x verify it bound the event by reflection, but no other code adds a `FloorChanged` handler, and the WebGL player's High stripping would drop an add accessor that only reflection reaches.
- It goes back to false on `MapRunEnded`.
- A value written later in the floor, after those events, is not seen until the next event. So set it before Begin, as above.
- Each desk switches only while nobody can see its screen. A desk built on a station floor starts still before it is ever drawn. When the answer changes while a desk is in view, that desk switches the first frame it is out of view.

## For V1 (the station TV)

The station TV must be attached with `FrontRoomsScreenVideo.Attach` (the ad path), never `AttachDesk`. Only the Office kit's desk CRTs use `AttachDesk`. If the station's own desk is dressed by `FrontRoomsOfficeKit`'s desk builder, its CRT goes still like any desk on that floor. That is the intent: the TV is the only moving screen.

## Verified (clone W/proj_v4desk, main aca6240b + working tree, Game fc76109d, MapWorld 83c19911)

With a clone-only stub of the member (`W/v4desk/tools/stub.py`, never packaged):
- set before Begin → still on floor 1;
- the game's own `ChangeFloor` → `FloorChanged` → the reader follows on floors 2 and 3.

Re-checked 2026-10-22 20:1x on main db790e30 + working tree (Game a2e212ef, MapWorld f742f592): `MapRunStarted(map, relay)`, `MapRunEnded()` and `FloorChanged(map, relay, floor)` are unchanged; `FloorHasStation` is still absent (the package dry run reports both); Roslyn compiles main + the package for macOS, iOS and the WebGL / iOS player defines with 0 errors.

Verify stage 2026-10-22 20:2x–20:3x, on main db790e30 + working tree (Game 938b0ee9, Mobile 4a7ccc9d, MapWorld f742f592): the three events are unchanged; with the direct `FloorChanged` subscription, Roslyn compiles main + the package with 0 errors (macOS `--all`, iOS, WebGL and iOS player defines); edit-mode tests 18 / 18 in the clone, including S1f (raising `FloorChanged` moves the reader to floor 3; re-hooking keeps one listener); dry run OK.

Numbers are in `PREBUILD_v4desk.md` §3 and §6.

## Checklist for M4 (关卡设计)

1. Add the member above, with `[Preserve]`.
2. Set it before `map.Begin(...)` in `StartRunInPlace` (floor 1) and in `ChangeFloor` (floors 2+), from the floor's station plan.
3. Clear it in `NextFloor` and in any reset that forgets the floor's plan; never at the key take.
4. Nothing else: no call into visual code, no event. The desks follow on the next `MapRunStarted` / `FloorChanged`. Keep `FrontRooms3DGame.FloorChanged(map, relay, floor)` as it is: the reader subscribes to it directly.
5. To see it work: on a floor whose station is planned, every Office desk CRT in the maze is still (some sets dark); the station TV still plays; the title corridor TV still plays.
