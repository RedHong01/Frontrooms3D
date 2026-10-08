# 40 — Evacuation placard (Q16): placement contract for the map chat (关卡设计)

Status: **CONTRACT REQUEST Q16-1, 2026-10-08 (fix stage).** Exact and tested; not applied. The visual chat never edits map files.
- **What the map chat is asked to do:** add one method, `KeepClearAtStart(Rect)`, and its two clears and one dress hook (§5, about 20 lines). Nothing else.
- **What it gets:** furniture never lands in front of the placard. Today it can, in the P2 and P3 cases: about **1 run in 4** (P2 22 %, P3 2 % of 400 runs).
- Everything else (where the placard goes, spawning it, the glow) is visual code that reads only public map API. It ships in `W/apply/placard_q16.sh` (`41_promotion.md`).
- Sources: `10_spec.md` §5 (the rule), `30_render.md` §7 (the real door positions and shares), `35_fix.md` §5 (the new height).

## 1. Which start door

- **The terminal stream room's double door**, the door the player opens at the start of every run. It belongs to `FrontRoomsRoomStream` (visual-owned), not to the map: the map builds nothing on the start area's north side.
- `FrontRoomsRoomStream.EndStreamAt(terminal)` makes the facade; at its end it calls `FrontRoomsPlacard.Prepare(room root, door point, facade face z)`.
  - door point = the door's centre on the door line, at floor height: `room.TransformPoint(0, 0, RoomLength)`;
  - facade face z = the map-side ("far side") reveal plane: `RoomLength + DoorWallDepth / 2 + 0.002` local, i.e. **door line + 0.022 m**.
- In the title flow the door is at x = `FrontRooms3DGame.TitleCenterX` (256.5) of the title world; stream rooms are 12 m long, so the door stands at z 18, 30, 42, 54 … (the first run uses z 18).
- **The door opens north (+Z) onto cell D** = `map.CellOf(door point + 1.5 m north)` = the map's `startDoorCell`. `MapRootFor` makes D's row Standard-height Level 0, so D's side edges are only Open, Arch or Wall.

## 2. Where it hangs (the rule, deterministic from the seed)

Terms: **Dc** = the door point (D's centreline on the door line); **s** = side, −1 west (−X), +1 east (+X); **E(s)** = `map.Cache.Edge(D, D + s)`. West is tried first (the reader's right as they face the door from the map).

| Mount | When | Frame origin (world; the wall-face point at the frame centre) | Faces | Glow cell | Keep-clear rect (world XZ) | Share (400 runs) |
|---|---|---|---|---|---|---|
| **P1** facade beside the door | E(west) = Open, else E(east) = Open | x = Dc.x + s·**1.7334**, y = floor + **1.560**, z = door line + **0.0225** | +Z (into the map) | D + s | x ∈ [Dc.x + s·1.35, Dc.x + s·2.117], z ∈ [door line, + 0.90] | **76 %** |
| **P2** side wall of D | no Open side; E(west) = Wall, else E(east) = Wall | x = Dc.x + s·**1.4195**, y = floor + **1.560**, z = door line + **1.500** | −s·X (into D) | D | x ∈ [Dc.x + s·0.52, Dc.x + s·1.42], z ∈ [door line + 1.117, + 1.883] | **22 %** |
| **P3** facade past the arch pier | both sides Arch | x = Dc.x − **1.9134**, y = floor + **1.560**, z = door line + **0.0225** | +Z | D − x | x ∈ [Dc.x − 2.297, Dc.x − 1.53], z ∈ [door line, + 0.90] | **2 %** |

- **Height changed in the fix stage:** centre **1.560 m** (was 1.524). Frame 1.403–1.717 m, so it clears the wallpaper's guide row (cue band 1.184–1.391 m) by 12 mm and stays under 1.83 m.
- Frame: 466.8 × 313.8 mm, 13.5 mm deep, flat on the wall (no standoffs), 0.5 mm proud of the face it sits on.
- Clearances: P1 is 0.30 m from the jamb and 0.34 m from the open leaf (the leaves sweep only |x| ≤ 1.12). P2's frame spans z 1.267–1.733 from the door line, past the open leaf's tip (≈ 1.14). The frame bottom (1.403 m) is above the leaf handles (1.13–1.31 m).
- Seen from the doorway: the open west leaf hides P1 from the east half of the doorway, and the arch pier can hide P3 from D. Accepted: both read from their own cell, and the door shuts by itself 4 m out (`StartDoorShutDistance`).

## 3. How it is spawned (visual code; the map does nothing)

1. `Prepare` adds a child **`Placard mount`** (`FrontRoomsPlacardMount`) under the terminal room. It retires any earlier mount on that room at once (the fix stage's race fix).
2. The mount waits for `FrontRooms3DGame.MapRunStarted(map, relay)` (fired in `StartRunInPlace` after `EndStreamAt` and `map.Begin`). Rooms are dressed after that (one per frame, or time-sliced), so a keep-clear rect registered here is in time.
3. Once: `FrontRoomsPlacard.Resolve` (reads only `CellOf`, `Cache.Edge`), then `KeepClearAtStart(rect)` if the map has it (§5), then:
   - `FrontRoomsKitLibrary.Spawn("Kit_EvacPlacard", mount, …, colliders: false)` and the same for `Kit_EvacPlacardLens` (not on WebGL);
   - `shadowCastingMode = Off` on every placard renderer (render-only: no collider);
   - `FrontRoomsPlacardGlow` on the frame, bound to (map, relay, glow cell, renderers).
4. The glow reads the glow cell's **logical** `LampLevel` and its 4 neighbours, `LampModeOf` (once a second) and `IsBuilt`. It never writes map state and never reads light objects. A lamp held dark at the start (`StartLampsNorth`) still reads lit, so nothing glows behind the shut door.

## 4. Lifetime (under the stream room, not a chunk root)

- **The placard lives under the terminal stream room, not under a chunk root.** P1 and P3 hang on the stream's own facade, which no chunk owns; only P2 hangs on a map wall (D's chunk).
- It is created when the run starts and destroyed with the title world: `StopTitleCorridor` runs once `map.StartAreaBuilt` is false (every chunk round the start area has dropped), right before `map.ClearStartArea()`; or on a restart (`BuildTitleCorridor`). The keep-clear rects (§5) are cleared at that same moment.
- P2 edge cases, both far from the player:
  - D's chunk **drops** while the room still stands: the placard hangs unseen in the gap until the room goes.
  - D's chunk is **rebuilt** (a revision): the wall is rebuilt in the same place; the placard does not need to move.
- Why not a chunk root: a chunk rebuild would destroy the placard and need a re-spawn and a fresh glow state, and P1/P3 have no chunk. If the map chat would rather own it, see §8 option B.

## 5. CONTRACT REQUEST Q16-1 (exact; `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs`)

What it does: a list of world-XZ rects; `KeepClearAtStart` adds one; `SetStartArea` and `ClearStartArea` empty it; `Dress` turns each rect that overlaps the room being furnished into a chunk-local keep-clear rect. With no rect registered, dressing is bit-identical (the chunk determinism check is unaffected). `[Preserve]` keeps the method in stripped builds while the visual mount still finds it by reflection (WebGL strips at High, iOS at Low).

```diff
--- a/Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs
+++ b/Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs
@@ -390,6 +390,19 @@
     RectInt startArea;
     bool hasStartArea;
     GridCoord startDoorCell;
+    // World-XZ rects the start area keeps free of furnishing (visual Q16, research/placard/40_contract_map.md).
+    readonly List<Rect> startKeepClear = new List<Rect>();
+
+    /// <summary>
+    /// Keep furnishing (office kits, piles and module props) out of a world-XZ rect
+    /// while the start area stands: the evacuation placard's wall and the floor in
+    /// front of it. Cleared by SetStartArea and ClearStartArea. MapRunStarted is
+    /// early enough: rooms are dressed one a frame from Update. [Preserve]: the
+    /// visual mount reaches it by reflection until it calls it directly, and WebGL
+    /// (managed stripping High) / iOS (Low) may strip a method nothing calls.
+    /// </summary>
+    [UnityEngine.Scripting.Preserve]
+    public void KeepClearAtStart(Rect worldXZ) => startKeepClear.Add(worldXZ);
     readonly List<GridCoord> scratch = new List<GridCoord>();
     readonly List<Door> movingDoors = new List<Door>();
     Transform player;
@@ -613,6 +626,7 @@
     /// </summary>
     public void SetStartArea(RectInt cells, GridCoord doorCell)
     {
+        startKeepClear.Clear();
         startArea = cells;
         startDoorCell = doorCell;
         hasStartArea = cells.width > 0 && cells.height > 0;
@@ -623,7 +637,7 @@
     /// Only while StartAreaBuilt is false: a chunk built round the rooms keeps
     /// their hole and its walls until it is rebuilt.
     /// </summary>
-    public void ClearStartArea() => hasStartArea = false;
+    public void ClearStartArea() { hasStartArea = false; startKeepClear.Clear(); }
 
     public bool HasStartArea => hasStartArea;
 
@@ -1892,6 +1906,13 @@
                 doorway = new Rect((startDoorCell.x - o.x) * cs, (startDoorCell.y - o.y) * cs, cs, depth);
                 clear.Add(doorway.Value);
             }
+            foreach (var w in startKeepClear)
+            {
+                var a = chunk.root.transform.InverseTransformPoint(new Vector3(w.xMin, 0f, w.yMin));
+                var b = chunk.root.transform.InverseTransformPoint(new Vector3(w.xMax, 0f, w.yMax));
+                var local = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.z, b.z), Mathf.Max(a.x, b.x), Mathf.Max(a.z, b.z));
+                if (local.Overlaps(new Rect(room.x * cs, room.y * cs, room.w * cs, room.h * cs))) clear.Add(local);
+            }
         }
         else
         {
```

The same diff as a file: `fix_src/contract_q16_1.diff` (apply with `patch -p1` from the project root).

**Checked, 2026-10-08 01:2x:**

| Base | sha1 | Dry run | Compile (Roslyn, Unity's own options) |
|---|---|---|---|
| main HEAD 22bb75f (committed) | 9abacc6f… | applies | 0 errors (render stage also ran it in game: "KeepClearAtStart in this map: True", nothing dressed into the rects) |
| main working tree, 01:23 (uncommitted time-sliced dressing, 108 lines added) | 6b76a00c… | applies, with an offset | **0 errors**, together with the whole placard package |

- `clear` is chunk-local metres (the doorway rect uses `(cell − o) * cs`, and the chunk root sits at that origin), so `InverseTransformPoint` on the chunk root is exact.
- If your time-sliced dressing moves where `clear` is built, keep the `foreach` right after the doorway rect, inside `if (hasStartArea)`.

## 6. After Q16-1 lands (visual chat's follow-up)

`FrontRoomsPlacardMount.KeepClear` swaps its reflection lookup for a direct call, and the one-time log line goes away:

```csharp
static void KeepClear(FrontRoomsMapWorld map, Rect worldXZ) => map.KeepClearAtStart(worldXZ);
```

Until then the mount finds no method and logs once: `[Placard] the map has no KeepClearAtStart (contract Q16-1): furniture may be dressed in front of a P2/P3 placard`.

## 7. Tests the map chat runs after the patch

1. **Shares.** 100 seeds at each real door position (z 18, 30, 42, 54; `MapRootFor` as the game calls it): tally the §2 mount. Expect about P1 76 %, P2 22 %, P3 2 % (`30_render.md` §7; tables in `render_data/placement_doors/`).
2. **Furniture.** On those seeds, dress the rooms round the start door with the rects registered. Assert that no module prop, kit or pile footprint overlaps a keep-clear rect, and that nothing the map builds lies within 0.02 m of the frame box (0.4668 × 0.3138 × 0.0135 at the §2 origin; rotated for P2).
3. **Lifetime.** `ClearStartArea` and `SetStartArea` empty the list. A run with no rect registered dresses exactly as today (chunk signatures unchanged).
4. **Confirm in writing:**
   - outlets and module wall props never use the facade face, or the D-side face of a D side wall, between 1.38 and 1.74 m above the floor (the frame plus 0.02 m);
   - a receptacle at 0.31 m or a switch topping out at 1.28 m clears the frame's 1.403 m bottom.

## 8. Options the map chat may prefer (default: neither)

- **Option B, the map owns the rule** (narrative A.12 gives placement to 关卡设计): move `FrontRoomsPlacard.Resolve` into the map as `public PlacardSpot StartPlacardSpot()` with the §2 table. The visual mount then calls it; nothing else changes. If you also want the placard **under D's chunk root**, say so: the visual chat will add `FrontRoomsPlacardMount.SpawnUnder(Transform parent, …)`, and the map must call it again after every rebuild of D's chunk while the start area stands (P1/P3 would still hang on the stream facade).
- **Pin the glow cell's lamp to Dim** (`10_spec.md` §5.6), so the legend always reads faintly. It changes a gameplay lamp, so it needs Red's and your consent. Not requested.

## 9. What the map must not change for the placard

- The meaning of `LampLevel` (logical level, overrides included, −1 for no lamp), `LampModeOf`, `IsBuilt`, `Cache.Edge`, `CellOf` and the `MapRunStarted` event. The placard and, later, the walls' phosphor driver read them.
- `startDoorCell`'s definition (the cell the stream door opens onto).
