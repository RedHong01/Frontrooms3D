# 03 — Window landing: the contract for the map chat (r5)

Status: r5, 2026-10-04 08:40. For the map chat (关卡设计). Nothing here was written into `Frontrooms3D/Assets`. All code ran in the private clones `scratchpad/proj_win` (R1 + R2 + T) and `scratchpad/proj_win_base` (R1 + R2), both re-synced from main at 06:35 today.

This replaces r4 (2026-10-03 22:30), the `[WINDOW-KIT]` diff in `01` §2, and the "no change" note in `02` §7. The hunks are the same as r4; r5 re-tested them on today's main at a quiet machine load and adds the tests the map chat asked for.

**Do not use** `FrontRoomsMapWorld.window-kit.diff` or `apply_window_kit.py.txt` (12:32, yesterday). They are stale and fail on today's file.

---

## 0. Short answer

- **Base file:** the CURRENT real `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs`, re-read at 08:40 today. Main is `1385738`; the file is unchanged since `75cfdff` (19:41 yesterday): 3,182 lines, sha256 `4144008eb0ca487e…`.
- **Main already calls the window kit.** Codex added a direct call in `RaiseWindowBuilt` (commit `8ef5b64`, lines 1296–1307):
  ```csharp
  try { FrontRoomsInteractableKit.DressWindow(this, window, record); }
  catch (Exception e) { Debug.LogException(e); }
  ```
  You accepted this call site (codex audit, `NOTE_map_chat_attribution.md`). r5 keeps it.
- **We ask for 2 small required hunks and 1 optional one.** Every hunk is marked `[WINDOW-KIT]`.

| Hunk | Lines in today's file | What | Required? |
|---|---|---|---|
| **R1** | 1298–1301, and new lines after 1319 | The call goes through a guarded helper `DressWindowFrame`. One warning per session, not one error per window | yes |
| **R2** | 1277 | The pane gets the RT hint `FrontRoomsMetalGlassTarget` only while `FrontRoomsGlassBreakable` does not exist | yes (hygiene) |
| **T** | 1211–1220, 1288–1290, 1298–1301 | A window's jamb and head trims are built only when no kit frame was hung | **optional**, needs Red |

- **Apply:** `git apply FrontRoomsMapWorld.window-kit.r4.diff` (R1 + R2), then optionally `git apply FrontRoomsMapWorld.window-kit.r4-trims.diff` (T). Or `…r4-all.diff` for both. `git apply --check` passes on today's file for all three. The content-anchored script `apply_window_kit_r4.py.txt` (`--trims` for T) gives byte-identical results.
- **Tested today, on main + r5** (machine load in brackets; the autopilot ran at load 6–25, under the 32 limit):

| Run | Result |
|---|---|
| R1 + R2, kit on (`proj_win_base`, 411 windows, 10 seeds) | **PASS 20,412 / 0** |
| R1 + R2 + T, kit on (`proj_win`) | **PASS 20,475 / 0** |
| Kit forced to throw (3 seeds, every window and every second window), both clones | **PASS 307 / 0** each. 25 / 25 chunks built every time; failing windows keep trims and pane; 1 warning per build |
| Kit off (the look before the kit), both clones | **PASS 8,847 / 0** (R1 R2) and **8,913 / 0** (R1 R2 T) |
| Your suites with the kit, both clones | MapVerification 100 / 100 seeds · RelayNav 60 / 60 · Interaction **143** / 0 (your new tests included) · GlassShot 20 / 0 · LevelDesigner 138 / 0 · FixtureTick 28 / 0 · CameraRig 25 / 0 · Captions 9 / 0 |
| Play mode (the real game, seed 4242, run seed 516574485), R1 + R2 + T | **PASS 26 / 0**: prompt at 1.0 m and not at 1.6 m, crack, drop + rebuild in Play, break, drop + rebuild after the break, climb |
| Autopilot §8 (seeds 2554, 20388), R1 + R2 + T, kit on vs off | **PASS**: 59.0 / 58.9 fps, p99 17.3 / 19.8 ms with the kit; 59.0 / 58.6 fps, p99 17.0 / 19.0 ms without (§6.2) |

- **What changes on screen:** R1 and R2 change nothing you can see (main already shows the frames). T removes the map's trim boxes that sit hidden under the kit casing: 3 per window, 1,233 boxes in our 10 seeds. Doors keep theirs. §7.

---

## 1. What changed since r4

| Item | r4 (yesterday 22:30) | r5 (today) |
|---|---|---|
| Base | main `75cfdff` | main `1385738`. Your Relay fix (`956b786`) changed `FrontRoomsMapHunter.cs` and two tests, not MapWorld. The MapWorld sha is the same, so the hunks are unchanged |
| R1 + R2 + T full run | cut off: the frame capture crashed the shader compiler at load 950 | done at load 6–25, plus a harness fix (the capture texture is now kept across scene reloads) |
| Autopilot (§8) | not run | run, kit on and off, both seeds (§6.2) |
| Play mode | r2 only | r5 (R1 + R2 + T): 26 / 0 |
| Interaction suite | 136 tests | 143 tests (your new ones), 0 failed |

Why the old 12:32 diff is dead (kept for the record):

| Old issue | Now |
|---|---|
| Hunks 1, 5, 6 (own root and glass fields, the slab hung from the pane, the RT move) | Gone. Your `Window.root` is the root. The visible glass after the break belongs to GD3's `FrontRoomsGlassBreakable` on the root |
| Reflected `MethodInfo` bound by exact types; a mismatch fell back silently | Gone. The call is compile-time. A signature change is a compile error, never a silent fallback |
| The call ran inside `BuildInto`, under `Build`'s catch: one throw dropped the whole 24 m chunk | R1: the call runs after the chunk is registered, under its own try/catch. T: the call runs inside `BuildEdge`, but `DressWindowFrame` catches everything, so `Build`'s catch never sees it. Forced-throw test: 25 / 25 chunks every time |
| `DressWindow(Transform, ZoneTheme, bool)` had no edge, no member, no break state | `DressWindow(FrontRoomsMapWorld, Window, GlassBreakRecord)`: edge id, both cells (the member comes from `map.Cache`), pane, stage, seed, impact |

---

## 2. R1 — the guarded call, logged once

**Why.** Today a kit failure is logged with `Debug.LogException` for every window. A broken kit FBX would print one error per window per chunk (411 errors in our 10 test seeds). The Office kit and the pile log one warning. R1 does the same, and returns the result, so T can use it.

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
@@ -1315,8 +1319,28 @@   (end of RaiseWindowReleased, before GlassBreakableType at 1321)
         {
             try { handler(window); }
             catch (Exception e) { Debug.LogException(e); }
+        }
+    }
+
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
         }
     }
+    // [WINDOW-KIT END]
```

- The facade is all-or-nothing. If it throws, it destroys what it built and turns the pane renderer back on before it re-throws. So a failed window looks exactly as it did before the kit (tested: "throw-today" checks, every window).
- **Option B** (if you want MapWorld to compile without the visual file): bind the same method by reflection in `ResolveDressers`, next to `officeDress`. Bind by exact types `(FrontRoomsMapWorld, Window, GlassBreakRecord)` with return type `bool`. Log ONE warning when the type `FrontRoomsInteractableKit` exists but the method does not bind. We recommend the direct call, because a mismatch then cannot hide.

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

- **Why it is hygiene now, not a bug fix.** G14 as it is in main (`Scripts/Rendering/GlassRT/FrontRoomsGlassRTSystem.cs`) skips disabled renderers (line 1011) and finds glass by its material (`IsGlassMaterial`, line 463). So the visible 6 mm slab (`Glass_Window`) is traced without the hint, and a hint on a hidden pane is never registered.
- **What it fixes.** When GD3's breakable lands, the map hides every pane renderer (line 1288). Without R2, every pane would still carry a hint that points at nothing.
- **Hand-off to G14 and GD3** (we send nothing; the visual chat relays): the breakable's slab must use `Glass_Window` (or carry the hint). G10's window look-dev (`FrontRoomsGlassVerification.RunWindowLookdevBatch`) and G14's verify (`FrontRoomsGlassRTVerify.cs`) swap materials on, or mirror, `window.pane`'s renderer. With the kit live, that renderer is hidden. They must use the visible renderer: today the `Window glass` child of the pane; after GD3, the breakable's slab.

## 4. T (optional) — no map trims on framed windows

**Why.**
- The map's 3 trim boxes per window stay drawn under the kit casing. They cost overdraw and 24 vertices each in the merged trim mesh, and they cast shadows (T removes 273 shadow renderers in our 10 seeds).
- The jamb trim's inner face (X 0.700) sits 0.5 mm behind the kit lining (X 0.6995). That is a z-fight risk in the reveal.
- G4 had to shape the wood casing only to hide these boxes: the casing rule w ≥ 0.0215, the liner welded into the casing sweep, a closed 0.2 mm head mitre. Our art review found that these rules make the walnut member read as one flat block (`05` §3, A3). With T, G4 can model a real reveal and an open mitre.
- The later true steel profile (the 2-inch SDI face, `10_spec` §6.5 item 9) also needs the trims gone.

**How.** Dress first, then build the trims only if no frame was hung. Doors build their trims exactly as today. The call moves from `RaiseWindowBuilt` into `BuildEdge` (still through `DressWindowFrame`, still after your breakable hook, so a broken window and a window with the breakable get the frame too).

```diff
@@ -1209,6 +1209,10 @@   (BuildEdge)
         if (kind == EdgeKind.Arch) return;
 
         // Frame: two jambs and a head in the trim colour.
+        // [WINDOW-KIT BEGIN] (T) a window's trims wait until the visual chat's frame has been tried (window branch).
+        void FrameTrims()
+        {
+        // [WINDOW-KIT END]
         var trims = get(blockIndex, trim);
         ...                                   (lines 1213-1219 unchanged)
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
@@ -1300,9 +1312,7 @@   (RaiseWindowBuilt, on top of R1)
         var record = GlassBreakOf(window);
-        // [WINDOW-KIT BEGIN] the visual chat's frame on the root: render-only, broken windows too, outside Build's guard.
-        DressWindowFrame(window, record);
-        // [WINDOW-KIT END]
+        // [WINDOW-KIT] (T) the visual chat's frame was hung in BuildEdge, before the trims.
         if (WindowBuilt == null) return;
```

- **Your spec row with T.** `LEVEL_MODULE_SPEC.md` line 49 ("Window") says "Same trim, no sill trim". With T it would read: "The visual chat's frame (`FrontRoomsInteractableKit.DressWindow`) on the root; the map's jamb and head trims only when no frame was hung; no sill trim."
- **Order of landing:** T needs the importer LOD fix first (`04` §2 item 2). With main's importer, a re-imported frame is culled at 12 m (24 m on Ultra), well inside the 46 m sight distance. Without T a culled frame only pops back to the old trims; with T it would leave a bare cut in the wall. Measured after the fix: frames switch to LOD1 at 4.0 m and are never culled (`tf_logs/r5/proj_win_lod_probe.txt`).
- **T results** (`proj_win`):

| Check | Result |
|---|---|
| Trim boxes removed | **1,233 = 3 × 411 windows**, nothing else (`tf_logs/r5/compare_trims_cost.txt`). Every other shell mesh is identical, byte for byte |
| T with the kit off | identical to no T, in every section (rays, sight lines, overlaps, climbs, shell, shift) |
| Forced throw with T | every failing window keeps all 3 trims and its pane cube; 0 strays |
| Gameplay (411 windows) | E rays, Relay sight lines, opening overlaps, climb corridor and climbs: identical to kit off, ray for ray |

## 5. Two asks (no code)

- **S1 is approved** (your note 2026-10-03: the 16 mm stop band, the tooth band). Our docs now say so. Nothing to do.
- **S2 (new, small):** please allow render-only screw heads up to **1.5 mm proud of the stop face** inside the 16 mm stop band (|Z| ≤ 0.0235, X between the sight line 0.6835 and the lining 0.6995). They are smaller than the stop band you already allowed, and they carry no collider. Why: G4 cut an 8.8 × 1.6 mm groove into the W-OF loose stop only to keep the 30 screw domes behind the stop face. Real pressed-steel loose stops are a plain channel with the oval heads standing proud; the groove reads as decorative reeding (art review A11). Our harness already tests this band (`stop-band` check, 0 vertices outside it).

## 6. Tests you can run (and what they must say)

### 6.1 Your suites, unchanged, in a clone with R1 + R2 (and again with T)

Each writes its JSON under `Verification/`. Our runs are in `tf_logs/r5/suites_kit_R1R2/` and `suites_kit_R1R2T/`.

| Suite | Must read |
|---|---|
| `FrontRoomsMapVerification.RunBatch` | 100 passed, 0 failed |
| `FrontRoomsRelayNavTest.RunBatch` | 60 / 60 arrived, 0 ghosts, 0 exceptions |
| `FrontRoomsMapInteractionTests.RunBatch` | 143 / 0 (its own forced chunk failure still logs "Chunk (1, 0) failed to build") |
| `FrontRoomsGlassShotTests.RunBatch` | 20 / 0 |
| `FrontRoomsLevelDesignerTests.RunBatch` | 138 / 0 |
| `FrontRoomsFixtureTickTests`, `CameraRigTests`, `CaptionsTests` | 28 / 25 / 9, 0 failed |

### 6.2 The §8 acceptance (autopilot)

`FrontRoomsMainScenePlaytest.RunBatch -autopilotSeed 2554` and `20388`, in `proj_win` (R1 + R2 + T, importer fix, putty). "Off" = the same clone with the kit disabled (`-windowKitOff`, the look before the kit). Load is `uptime`'s 1-minute average at start and end.

| Run | Load | Avg fps | p99 ms | Spikes > 50 ms in the report | Chunk builds: median / p90 / max ms | Frames > 50 ms with map work |
|---|---|---|---|---|---|---|
| kit, 2554 | 6.2 / 8.4 | **59.0** | **17.3** | 1 (381 ms at 11.8 s, no map work) | 5.8 / 8.9 / 21.4 (70 builds) | 0 |
| off, 2554 | 8.4 / 7.9 | 59.0 | 17.0 | 1 (376 ms at 17.0 s) | 5.8 / 6.7 / 16.8 (80 builds) | 1 |
| kit, 20388 | 7.9 / 16.1 | **58.9** | **19.8** | 2 (388 ms at 9.8 s; 80 ms at 64.6 s), no map work | 6.1 / 6.9 / 19.6 (70 builds) | 0 |
| off, 20388 | 16.1 / 24.9 | 58.6 | 19.0 | 8 (worst 449 ms) | 6.2 / 7.1 / 76.4 (80 builds) | 2 |

- Bar: ≥ 55 fps and p99 ≤ 33 ms. **Both kit runs pass**, close to your baseline (59 fps, p99 ≈ 18 ms).
- The 380 ms spikes happen with and without the kit, in frames where the map did no work (editor shader compiles; your spec does not count them).
- The autopilot's path is not identical between runs (the zones and cells visited differ), so the kit/off columns compare two similar walks, not the same one.
- Frames spawn in the chunk-build frame, not in the room-dress queue. That costs **+0.2 to +0.3 ms per chunk** with 7–9 windows (bench, 7 calls each: 5.51 → 5.83, 6.70 → 6.88, 5.59 → 5.77 ms) and **0.017 ms per frame** (0.034 ms with the interim slab). No chunk build spiked, so we did not move the frames into the dress queue.
- Per-room LOD0 triangles with frames (10 seeds, a frame counted in both rooms it faces): worst Office room **85,556 / 120,000**, worst other room **47,126 / 60,000**. 0 rooms over.
- Raw: `autopilot_r5/` (report.json and the per-frame work log for each run), summary `autopilot_r5/summary.txt`.

### 6.3 Our window harness (clone only, never promoted)

`Assets/Editor/Audit/FrontRoomsWindowTF.cs` (copy: `FrontRoomsWindowTF.r5.cs.txt`). 411 windows in 10 seeds (366 W-L0, 45 W-OF; **45 on chunk borders, 366 inside a chunk**). Per window:
- the root's name and pose; the pane collider 1.4 × 1.65 × 0.03 on the wall line, mapped as architecture;
- exactly one enabled glass renderer under the root as G14 sees it, and it is the visible one;
- the frame member by room theme, face A toward the room, no collider, no light, the §5.2 envelope, the stop corners on both faces, nothing inside the clear opening;
- 54 E rays per window (game reach 2.4 m; prompt reach `FrontRoomsShotTimings.GlassBreak.Reach` = 1.2 m, read from code);
- 32 Relay sight lines per window (your `FrontRoomsMapHunter.Visible`), before and after the break;
- the opening clear after the break (default layer and all layers); the climb (18 starts per window; your `ClimbSeconds` 0.6, `ClimbLift` 0.35, `ClimbDuck` 0.55 read from code); minimum eye-to-frame clearance 0.244 m;
- determinism (drop + rebuild of every chunk; a second world: 25 / 25 identical in every seed);
- a **revisit shift** (`Cache.Shift` on every chunk with a window, as `Stream` does): broken windows that are still windows come back framed, with no pane and the opening clear (283 / 283 in 10 seeds: 45 on chunk borders, 238 inside); edges that became something else leave no root and no pane (128 / 128); new windows come back intact and framed (135 / 135);
- per-room LOD0 triangles against §8 (above).

`FrontRoomsWindowLandingPlay.RunBatch` (copy: `FrontRoomsWindowLandingPlay.r5.cs.txt`) covers Play mode, including a **drop + rebuild after a break while `Destroy` is deferred**: next frame exactly one root for the edge, its frame, no pane, 0 colliders in the opening.

**Level Designer:** not applicable by construction. `FrontRoomsModulePreview` builds one room at one height, so `ha == hb` everywhere and the map never makes a window edge there. Its 138 / 138 says nothing about windows.

**Not exercised:** a live `ApplyLive` change of `buildRadius` or `chunksPerFrame`. It only changes which chunks `Stream` builds and how many per frame; each one goes through the same `Build` path that the rebuild, shift and autopilot checks cover. At `chunksPerFrame` 3 the frames would add about 1 ms to that frame.

## 7. What changes on screen

| | Before the kit (kit off) | Main today (= R1 + R2) | R1 + R2 + T |
|---|---|---|---|
| Window frame | 3 flat trim boxes in the trim colour | walnut W-L0 / steel W-OF kit frame, stops on both faces | the same |
| Map trims | visible | still built, hidden under the casing | gone on framed windows; kept on a window whose frame failed |
| Glass | 30 mm milky pane cube | 6 mm `Glass_Window` slab; the pane cube's renderer is off, its collider stays | the same |
| A failed kit window | — | trims + pane cube, 1 warning per session | the same |
| Gameplay (E ray, Relay sight, climb, opening after the break) | — | identical to kit off, ray for ray | identical, ray for ray |

Frames for Red: `05_for_red.md`.

## 8. Files in this folder

| File | What |
|---|---|
| `FrontRoomsMapWorld.window-kit.r4.diff` | R1 + R2 (55 lines), `git apply` on today's file |
| `FrontRoomsMapWorld.window-kit.r4-trims.diff` | T on top of R1 + R2 (46 lines) |
| `FrontRoomsMapWorld.window-kit.r4-all.diff` | R1 + R2 + T in one (86 lines) |
| `apply_window_kit_r4.py.txt` | applies the same blocks by content anchors (`--trims` for T); refuses a file that already has `[WINDOW-KIT` blocks or any anchor found other than once |
| `tf_logs/r5/` | our logs, the suites' JSON, the comparison, the LOD probe, load per run |
| `autopilot_r5/` | the four autopilot runs |
