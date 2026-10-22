# Contract C2 · MAP-3 · The lamp border rule reads edges without generating chunks

| | |
|---|---|
| For | 关卡设计 (map chat). Map-owned file; the visual chat does not edit it. |
| Finding | MAP-3 (minor), `../20_findings.md` §2.5. Codex wrote the code (`8ef5b64`, V5 light lead). |
| File | `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs`: `BorderThemes` (line 1782) and a new helper `EdgeNoGenerate` |
| Base | main `0a9ee7f` + working tree, 2026-10-14. File sha1 `11586bbc4a6c9e3c7a4da98f7a9e3b833b166652`. |
| Patch | `C2_MAP3_lamp_border_pure.diff`. `git apply --check` passes on today's file, alone and after C1. |
| Depends on | nothing. **C3 (MAP-2) needs this one first**: it uses `EdgeNoGenerate`. |
| Replaces | `research/level_transitions/contracts/lightlead_lampmodeof_pure.diff` (2026-10-03, same idea). **Send only this one.** That older patch still applies with offsets, so two patches for one hunk would conflict. This one adds the refinement its own verifier asked for: border edges come from the pure `BorderEdge`, so the prediction is exact for every built lamp, even after the cache drops a neighbour. |

## What is wrong

- `LampModeOf` is documented as "Pure … it never generates a chunk" (line 3330), and your test checks it: "LampModeOf a far cell generates no chunk" (`FrontRoomsMapInteractionTests.cs:1634`).
- With `-lightleadBorders` or `-lightleadSoft`, the V5 mirror at line 3355 calls `BorderThemes` → `Cache.Edge` → `Cache.Get`, which generates the chunk (and its west and south neighbours) at the current tier.
- Off by default (`FrontRoomsTransitionLightLead.Borders` is false unless those flags are passed). Red keeps V5 ON, but not the border rule (`../RED_DECISIONS.md`).

## The change

- `BorderThemes` reads each edge with a new `EdgeNoGenerate(a, b)` instead of `Cache.Edge(a, b)`. It is used by both callers: `BuildFixture` (line 1766) and `LampModeOf` (line 3355).
- `EdgeNoGenerate`:
  - an edge between two chunks is a chunk-border edge, a pure function of the seed: `Cache.Generator.BorderEdge(a, east)`. `FrontRoomsMapValidator` already checks that chunks agree with it across neighbours (lines 91–99) and across shifts (219–220);
  - an inner edge is read from its own chunk with `Cache.TryGetGenerated`;
  - an inner edge of a chunk the cache has not generated reads as `Wall` (no crossing). Only a far-cell prediction meets this case. A chunk being built is always generated, so a build reads exactly what `Cache.Edge` would.

```diff
@@ -1785,11 +1785,29 @@ public sealed class FrontRoomsMapWorld : MonoBehaviour
         {
             var n = k == 0 ? new GridCoord(cell.x + 1, cell.y) : k == 1 ? new GridCoord(cell.x - 1, cell.y) : k == 2 ? new GridCoord(cell.x, cell.y + 1) : new GridCoord(cell.x, cell.y - 1);
             borderThemes[k] = Cache.ZoneOf(n).theme;
-            kinds[k] = InStartArea(n) || InStartArea(cell) ? EdgeKind.Wall : Cache.Edge(cell, n);
+            kinds[k] = InStartArea(n) || InStartArea(cell) ? EdgeKind.Wall : EdgeNoGenerate(cell, n);
         }
         return borderThemes;
     }
 
+    // The edge between two side-by-side cells, read without generating a chunk (codex audit MAP-3), so
+    // LampModeOf stays pure and a build reads no neighbour chunk. An edge on a chunk border is a pure
+    // function of the seed (FrontRoomsMapGenerator.BorderEdge; FrontRoomsMapValidator checks that chunks
+    // agree with it across neighbours and shifts). An inner edge comes from its own chunk; if the cache has
+    // not generated that chunk, it reads as Wall (no crossing). Only LampModeOf's prediction for a far cell
+    // meets that case: a chunk being built is always generated, so a build reads what Cache.Edge would.
+    EdgeKind EdgeNoGenerate(GridCoord a, GridCoord b)
+    {
+        if (b.x < a.x || b.y < a.y) { var t = a; a = b; b = t; }
+        var east = b.x > a.x;
+        var chunkA = MapGrid.ChunkOf(a);
+        if (chunkA != MapGrid.ChunkOf(b)) return Cache.Generator.BorderEdge(a, east);
+        if (!Cache.TryGetGenerated(chunkA, out var data)) return EdgeKind.Wall;
+        var o = data.Origin;
+        var index = MapGrid.LocalIndex(a.x - o.x, a.y - o.y);
+        return east ? data.east[index] : data.north[index];
+    }
+
```

Apply from the project root: `git apply Documentation/research/codex_audit/contracts/C2_MAP3_lamp_border_pure.diff`.

## Verified (2026-10-14, clone `W/proj_cxreport` = main `0a9ee7f` + tree, Unity 6000.3.10f1 batch, macOS 27.0.1)

Clone-only harness `FrontRoomsCxMapContracts` (copy: `harness/FrontRoomsCxMapContracts.cs.txt`), 10 seeds, 25 chunks each, the border rule forced on (`FrontRoomsTransitionLightLead.Borders = true`, what `-lightleadBorders` sets):

| Check | Main as is | Main + C1–C4 |
|---|---|---|
| `LampModeOf` on 64 far cells per seed: chunks generated | **800** | **0** |
| Built lamps whose predicted mode ≠ built mode | 0 of 15,970 | 0 of 15,970 |
| The same after the cache drops every generated-but-unbuilt chunk | 0, but 100 chunks regenerated | 0, 0 regenerated |
| Every built lamp's mode, main vs contract (seed, cell, mode) | — | identical, 15,970 of 15,970, border rule on and off |
| Lamps the border rule changes | 904 | 904 |
| `FrontRoomsMapInteractionTests` with the border rule on | **FAIL 151 / 1** ("LampModeOf a far cell (Steady) generates no chunk") | **PASS 152 / 0** |
| Same tests, default flags | PASS 152 / 0 | PASS 152 / 0 |

Roslyn compile (macOS and iOS defines), all four contracts together: 0 errors, 0 warnings.

## How to test

1. Your suites with the patch, default flags: same counts as today (`FrontRoomsMapInteractionTests` 152 / 0, `FrontRoomsFixtureTickTests` 28 / 0, `FrontRoomsMapVerification` 100 / 0).
2. Run `FrontRoomsMapInteractionTests.RunBatch` once with `-lightleadBorders` on the command line: the far-cell purity check must pass (today it fails).
3. Optional, the light-lead track's E1 test (`research/level_transitions/15_var_lightlead.md` §15.3): with the border rule on, `LampModeOf(cell)` equals the built fixture's mode for every built lamp.
