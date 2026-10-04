# 03 — Window landing: the contract for the map chat (r4)

Status: r4, 2026-10-03 22:30. For the map chat (关卡设计). Nothing here was written into `Frontrooms3D/Assets`. All code ran in the private clones `scratchpad/proj_win` and `scratchpad/proj_win_base`.

This replaces the `[WINDOW-KIT]` diff in `01` §2 and the "no change" note in `02` §7. Both are stale.

---

## 0. Short answer

- **Base file:** the CURRENT real `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs`, re-read at 22:30: main `75cfdff`, 3,182 lines, sha256 `4144008eb0ca487e…`.
- **Main already calls the window kit.** Codex added a direct call in `RaiseWindowBuilt` (commit `8ef5b64`, 19:10, lines 1296–1301):
  ```csharp
  try { FrontRoomsInteractableKit.DressWindow(this, window, record); }
  catch (Exception e) { Debug.LogException(e); }
  ```
  You told us (codex audit, `NOTE_map_chat_attribution.md`) that you accept this call site. r4 keeps it.
- **r4 asks for 2 small required changes and 1 optional one.** Every hunk is marked `[WINDOW-KIT]`.

| Hunk | Lines in today's file | What | Required? |
|---|---|---|---|
| **R1** | 1296–1301, and new lines before 1321 | The call goes through a guarded helper `DressWindowFrame`. One warning per session, not one error per window | yes |
| **R2** | 1277 | The pane gets the RT hint `FrontRoomsMetalGlassTarget` only while `FrontRoomsGlassBreakable` does not exist | yes (hygiene) |
| **T** | 1211–1221, 1288–1290, 1296–1301 | A window's jamb and head trims are built only when no kit frame was hung | **optional**, needs Red |

- **Tested in the clones, on main + r4:**

| Run | Result |
|---|---|
| R1 + R2, kit on (`proj_win_base`, 411 windows, 10 seeds) | **PASS 20,412 / 0** |
| R1 + R2, kit forced to throw (3 seeds, every window and every second window) | **PASS 307 / 0**. 25 / 25 chunks built every time; failing windows keep trims and pane; 1 warning per build |
| R1 + R2, kit off (the look before the kit) | **PASS 8,847 / 0** |
| R1 + R2 + T | see §4 (filled from `proj_win`) |
| Your suites with the kit (R1 + R2) | MapVerification 100 / 100 seeds · RelayNav 60 / 60 · Interaction 136 / 0 · GlassShot 20 / 0 · LevelDesigner 138 / 0 · FixtureTick 28 / 0 · CameraRig 25 / 0 · Captions 9 / 0 |

- **What changes on screen:** R1 and R2 change nothing you can see (main already shows the frames). T removes the map's trim boxes that sit hidden under the kit casing on framed windows (3 boxes per window). §5.

---

## 1. Why r3 is dead

r3 (`win_fix/FrontRoomsMapWorld.window-kit.r3-all.diff`) was built at 17:43 on your commit `df4cb03`. At 19:10 Codex landed a different call site in main. r3 now fails at its first anchor. r4 is rebuilt on main `75cfdff`:

| r3 / old issue | r4 |
|---|---|
| Hunks 1, 5, 6 of the 12:32 diff (own root and glass fields, pane slab, RT move) | Gone since r3. Your `Window.root` is the root. The visible glass after the break belongs to GD3's `FrontRoomsGlassBreakable` on the root |
| Reflected `MethodInfo` bound by exact types; a mismatch fell back silently | Gone. Codex's call is compile-time. A signature change is now a compile error, never a silent fallback |
| The call ran inside `BuildInto`, under `Build`'s catch: one throw dropped the whole 24 m chunk | The call runs in `RaiseWindowBuilt`, after the chunk is registered, under its own try/catch (R1). Forced-throw test: 25 / 25 chunks every time |
| `DressWindow(Transform, ZoneTheme, bool)` had no edge, no member, no break state | `DressWindow(FrontRoomsMapWorld, Window, GlassBreakRecord)`: edge id, both cells (so the member comes from `map.Cache`), pane, stage, seed, impact, rotation, side |
| Pane RT target lost when the breakable hides the pane | R2 (§3) |

---

## 2. R1 — the guarded call, logged once

**Why.** Today a kit failure is logged with `Debug.LogException` for every window. A broken kit FBX would print one error per window, every chunk (411 errors in our 10 test seeds). The Office kit and the pile log one warning. R1 does the same, and returns the result so T can use it.

```diff
@@ -1297,8 +1300,9 @@   (RaiseWindowBuilt)
     {
         window.announced = true;
         var record = GlassBreakOf(window);
-        try { FrontRoomsInteractableKit.DressWindow(this, window, record); }
-        catch (Exception e) { Debug.LogException(e); }
+        // [WINDOW-KIT BEGIN] the visual chat's frame on the root: render-only, broken windows too, outside Build's guard.
+        DressWindowFrame(window, record);
+        // [WINDOW-KIT END]
         if (WindowBuilt == null) return;
@@ -1315,8 +1319,28 @@   (after RaiseWindowReleased, before GlassBreakableType)
+    // [WINDOW-KIT BEGIN] The visual chat's window frame (Office/FrontRoomsInteractableKit.Window.cs). A throw is caught
+    // here, never by Build's guard (that would leave the whole chunk out: floor, walls, doors, key), and logged once;
+    // that window then keeps the map's trims and pane as today. Returns true when a frame was hung.
+    static bool windowFrameFailureLogged;
+
+    bool DressWindowFrame(Window window, GlassBreakRecord record)
+    {
+        try { return FrontRoomsInteractableKit.DressWindow(this, window, record); }
+        catch (Exception e)
+        {
+            if (!windowFrameFailureLogged)
+            {
+                windowFrameFailureLogged = true;
+                Debug.LogWarning("[FrontRoomsMap] Window frame " + window.a + "-" + window.b + " failed; it keeps the map's trims and pane (logged once): " + e);
+            }
+            return false;
+        }
+    }
+    // [WINDOW-KIT END]
```

- The facade is all-or-nothing. If it throws, it destroys what it built and turns the pane renderer back on before it re-throws. So a failed window looks exactly as it did before the kit.
- **Option B (if you want MapWorld to compile without the visual file):** bind the same method by reflection in `ResolveDressers`, next to `officeDress`. Bind by exact types `(FrontRoomsMapWorld, Window, GlassBreakRecord)` with return type `bool`, and log ONE warning when the type `FrontRoomsInteractableKit` exists but the method does not bind. The code is r3's R1/R2 blocks (`win_fix/FrontRoomsMapWorld.window-kit.r3-all.diff`, lines 6–9 and 89–99). We recommend the direct call, because a mismatch then cannot hide.

## 3. R2 — the RT hint follows the visible glass

```diff
@@ -1274,7 +1274,10 @@   (BuildEdge, window branch)
                 pane.GetComponent<Renderer>().sharedMaterial = glass;
-                pane.AddComponent<FrontRoomsMetalGlassTarget>();
+                // [WINDOW-KIT BEGIN] the RT hint marks the visible glass: the pane only while no FrontRoomsGlassBreakable
+                // exists (with it the pane renderer is hidden below, and the breakable's slab is the glass).
+                if (GlassBreakableType() == null) pane.AddComponent<FrontRoomsMetalGlassTarget>();
+                // [WINDOW-KIT END]
```

- **Why it is only hygiene now.** G14 as promoted (`GlassRT/FrontRoomsGlassRTSystem.cs`) skips disabled renderers (`Rescan`, line 1011) and finds glass by its material (`IsGlassMaterial`, line 463: shader `FrontRooms/Glass`, or a transparent named "glass"). So the visible slab (`Glass_Window`) is traced without the hint, and a hint on a hidden pane is never registered.
- **What it fixes.** When GD3's breakable lands, the map hides every pane renderer. Without R2 every pane would still carry a hint that points at nothing.
- **Hand-off (G14, GD3):** the breakable's slab must use `Glass_Window` (or carry the hint). G10's window look-dev (`FrontRoomsGlassVerification.RunWindowLookdevBatch`, lines 455–564) and G14's verify (`FrontRoomsGlassRTVerify.cs` lines 732, 874, 1534) swap materials on, or mirror, `window.pane`'s renderer. With the kit live, that renderer is hidden. They must use the visible renderer: today the `Window glass` child of the pane; after GD3, the breakable's slab.

## 4. T (optional) — no map trims on framed windows

**Why.** The map's 3 trim boxes per window stay drawn under the kit casing. They are hidden, but:
- they cost overdraw and 24 vertices each in the merged trim mesh;
- the jamb trim's inner face (X 0.700) sits 0.5 mm behind the kit lining (X 0.6995);
- G4 had to shape the wood casing to hide them: the casing rule w ≥ 0.0215, the liner welded into the casing sweep, a closed 0.2 mm head mitre. Our art review found that these rules make the walnut member read as one flat block (`05` §3). With T, G4 can model a real reveal and an open mitre.
- The later true steel profile (the 2-inch SDI face, `10_spec` §6.5 item 9) also needs the trims gone.

**How.** Dress first, then build the trims only if no frame was hung. Doors build their trims exactly as today. So the call moves from `RaiseWindowBuilt` into `BuildEdge` (still guarded by `DressWindowFrame`, still after your breakable hook).

```diff
@@ -1209,6 +1209,10 @@   (BuildEdge)
         // Frame: two jambs and a head in the trim colour.
+        // [WINDOW-KIT BEGIN] (T) a window's trims wait until the visual chat's frame has been tried (window branch).
+        void FrameTrims()
+        {
+        // [WINDOW-KIT END]
         var trims = get(blockIndex, trim);
         ...                                   (the 8 trim lines, unchanged)
         trims.Box(start + along * c + Vector3.up * (openingTop + frame * .5f), ...);
+        // [WINDOW-KIT BEGIN]
+        }
+        if (kind != EdgeKind.Window) FrameTrims();
+        // [WINDOW-KIT END]
@@ -1290,6 +1298,10 @@   (BuildEdge, after the breakable hook)
                 if (window.pane != null) window.pane.GetComponent<Renderer>().enabled = false;
             }
+            // [WINDOW-KIT BEGIN] (T) the visual chat's frame on the root (render-only, broken windows too, guarded);
+            // the map's own trims only when no frame was hung.
+            if (!DressWindowFrame(window, record)) FrameTrims();
+            // [WINDOW-KIT END]
             chunk.windows.Add(window);
@@ -1300,9 +1312,7 @@   (RaiseWindowBuilt)
         var record = GlassBreakOf(window);
-        // [WINDOW-KIT BEGIN] the visual chat's frame on the root: render-only, broken windows too, outside Build's guard.
-        DressWindowFrame(window, record);
-        // [WINDOW-KIT END]
+        // [WINDOW-KIT] (T) the visual chat's frame was hung in BuildEdge, before the trims.
         if (WindowBuilt == null) return;
```

- **Order of landing:** T needs the importer LOD fix first (`04` §2 item 2). Without it, a kit frame is culled past 12 m (24 m on Ultra), and with T that window would show a bare wall cut.
- **T results** (`proj_win`, main + R1 + R2 + T + facade r4 + importer fix): see the table below.

<!-- T-RESULTS -->

## 5. What changes on screen

| | Before the kit (kit off) | Main today = r4 R1 + R2 | r4 + T |
|---|---|---|---|
| Window frame | 3 flat trim boxes in the cove-base colour | walnut W-L0 / steel W-OF kit frame, stops on both faces | the same |
| Map trims | visible | still built, hidden under the casing | gone on framed windows; kept on a window whose frame failed |
| Glass | 30 mm milky pane cube | 6 mm `Glass_Window` slab; the pane cube is hidden, its collider stays | the same |
| A failed kit window | — | trims + pane cube, 1 warning per session | the same |
| Gameplay (E ray, Relay sight, climb, opening after the break) | — | identical to kit off, ray for ray (§6) | identical, ray for ray |

Frames: `05_for_red.md`.

## 6. Tests you can run (and what they must say)

Your own suites, unchanged, in a clone with r4 (each writes its JSON under `Verification/`):

| Suite | Must read |
|---|---|
| `FrontRoomsMapVerification.RunBatch` | 100 passed, 0 failed |
| `FrontRoomsRelayNavTest.RunBatch` | 60 / 60 arrived, 0 ghosts, 0 exceptions |
| `FrontRoomsMapInteractionTests.RunBatch` | 136 / 0 (its own forced chunk failure still logs "Chunk (1, 0) failed to build") |
| `FrontRoomsGlassShotTests.RunBatch` | 20 / 0 |
| `FrontRoomsLevelDesignerTests.RunBatch` | 138 / 0 |
| `FrontRoomsFixtureTickTests`, `CameraRigTests`, `CaptionsTests` | 28 / 25 / 9, 0 failed |
| `FrontRoomsMainScenePlaytest.RunBatch -autopilotSeed 2554` and `20388` | §8 acceptance: ≥ 55 fps average, p99 ≤ 33 ms (`04` §4 has our runs; load was far above 32) |

Our window harness (clone only, never promoted): `Assets/Editor/Audit/FrontRoomsWindowTF.cs` (copy here: `FrontRoomsWindowTF.r4.cs.txt`). Per window on 411 windows in 10 seeds (366 W-L0, 45 W-OF; 45 on chunk borders, 366 inside a chunk):
- the root's name and pose; the pane collider 1.4 × 1.65 × 0.03 on the wall line, mapped as architecture;
- exactly one enabled glass renderer under the root as G14 sees it, and it is the visible one;
- the frame member by room theme, face A toward the room, no collider, no light, the §5.2 envelope, the stop corners on both faces, nothing inside the clear opening;
- 54 E rays per window (game reach 2.4 m; prompt reach `FrontRoomsShotTimings.GlassBreak.Reach` 1.2 m, read from code);
- 32 Relay sight lines per window (your `FrontRoomsMapHunter.Visible`), before and after the break;
- the opening clear after the break (default layer and all layers), the climb (18 starts per window, your `ClimbSeconds` / `ClimbLift` / `ClimbDuck` read from code);
- determinism (drop + rebuild of every chunk; a second world);
- a **revisit shift** (`Cache.Shift` on every chunk with a window, as Stream does): broken windows that are still windows come back framed, without a pane, opening clear; edges that became something else leave no root and no pane; new windows come back intact and framed;
- per-room LOD0 triangles with frames against `LEVEL_MODULE_SPEC` §8 (120 k Office, 60 k other): 0 rooms over in all 10 seeds (worst Office room 85,556; worst other room with frames 47,126 when a frame is counted on both sides).

**Level Designer:** not applicable by construction. `FrontRoomsModulePreview` builds one room at one height, so `ha == hb` everywhere and the map never makes a window edge there. The 138 / 138 says nothing about windows.

**Not exercised here:** a live `ApplyLive` change of `buildRadius` or `chunksPerFrame`. It only changes which chunks `Stream` builds; each one goes through the same `Build` → `RaiseWindowBuilt` path that the revisit-shift and rebuild checks cover.
