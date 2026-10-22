# Contract C3 · MAP-2 · Posts on chunk lines stop depending on the neighbour's interior

| | |
|---|---|
| For | 关卡设计 (map chat). Map-owned file; the visual chat does not edit it. The single B0 rule is also level-transitions' business (MAP-5): their pick will rework these hunks anyway. |
| Finding | MAP-2 (major), `../20_findings.md` §2.5. Codex's B0.1 (`8ef5b64`), copied from the V5 pre-render clone. |
| File | `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs`: `CornerReach` (line 1309), new `ChunkLineReach`, `PostPieceAt` (line 1319) |
| Base | main `0a9ee7f` + working tree, 2026-10-14, **with C2 applied**. Today's file sha1 `11586bbc4a6c9e3c7a4da98f7a9e3b833b166652`; after C1 + C2 `80b9aa7f…`; after C3 `b5df839c…`. |
| Patch | `C3_MAP2_chunk_line_posts.diff`. **Apply C2 first**: C3 calls `EdgeNoGenerate`. The text applies without C2, but then it does not compile. |
| Do not send | the earlier "+1 reach on every chunk-line post" patch in `../10_review_map-edits.md` §4. Both of this finding's verifiers rejected it: it brings back the corner z-fight Red reported at 13:09 (measured below: 1,315 posts against 0 today). |

## What is wrong

- The generator's rule (`FrontRoomsMap.cs:246–251`): everything two chunks share is a pure function of the seed, "so neighbours agree no matter which is built first. A chunk's revision only reshuffles its own interior walls."
- B0.1 decides how the four wall pieces at a post share the 0.16 m post square (`CornerReach` → `PostPieceAt` → `Cache.Edge` for all four pieces).
- At a post on a chunk line, one of the four pieces is the **other** chunk's interior wall. So a chunk's wall ends depend on its neighbour's interior.
- When the neighbour comes back shifted (re-entry after `shiftAfterSeconds` 30 s), only the neighbour is rebuilt. The chunk that stayed built keeps wall ends cut for the old interior: a notch (an empty quarter of the post, 8 × 8 cm, full height) or an overlap of two finishes (the z-fight B0 removes) at the junction.
- Side effect: every build also generated its neighbours' data early (`Cache.Edge` → `Cache.Get`). The verifiers judged the tier part of that harmless; C3 removes it anyway.

## The change

- **Inside a chunk** (all four pieces are the chunk's own edges) and **at a chunk corner** (all four are chunk-border edges, which are pure): B0.1 unchanged. `PostPieceAt` now reads edges with C2's `EdgeNoGenerate`, which gives the same values without generating anything.
- **On a chunk line** (start area left out), a new `ChunkLineReach`. The two pieces along the line are chunk-border edges (pure). The near cross piece (west of a vertical line, south of a horizontal one) belongs to the same chunk as the line pieces; the far cross piece belongs to the far chunk. So the far piece's reach reads only pure data, and the near pieces read pure data plus their own chunk's edges:
  - one finish round the post: today's reach (+1), nothing can z-fight;
  - no piece on the line: each cross piece runs to the cell line and covers its half;
  - both line pieces: the run owns the post, each piece its half; cross pieces stop at the post face;
  - one line piece L: the far cross piece stops at the post face and B0.1's own rule runs on L plus the near cross piece, **if** the room on the far side of the post is no taller than L and the near piece. Otherwise today's reach stays. That is the only case left that can z-fight.
- Start-area posts keep today's rule.

```diff
@@ -1309,6 +1309,19 @@ public sealed class FrontRoomsMapWorld : MonoBehaviour
     void CornerReach(GridCoord k, int role, out int reachA, out int reachB)
     {
         GridCoord sw = new GridCoord(k.x - 1, k.y - 1), se = new GridCoord(k.x, k.y - 1), nw = new GridCoord(k.x - 1, k.y), ne = k;
+        // MAP-2 (codex audit): a post on a chunk line (not a chunk corner) joins pieces of two chunks, and the
+        // far chunk's cross piece is that chunk's interior, which a revisit shift reshuffles while this chunk
+        // stays built. Such a post follows ChunkLineReach, which never reads the far chunk's interior, so both
+        // chunks agree after any shift. Inside a chunk all four pieces are its own edges, and at a chunk corner
+        // all four are chunk-border edges (pure), so the rule below stays; PostPieceAt reads them without
+        // generating a chunk.
+        var o = MapGrid.ChunkOrigin(MapGrid.ChunkOf(k));
+        bool onX = k.x == o.x, onY = k.y == o.y;
+        if (onX != onY && !InStartArea(sw) && !InStartArea(se) && !InStartArea(nw) && !InStartArea(ne))
+        {
+            ChunkLineReach(sw, se, nw, ne, onX, role, out reachA, out reachB);
+            return;
+        }
         postPieces[FrontRoomsTransitionKit.South] = PostPieceAt(sw, se, false);
         postPieces[FrontRoomsTransitionKit.North] = PostPieceAt(nw, ne, false);
         postPieces[FrontRoomsTransitionKit.West] = PostPieceAt(sw, nw, true);
@@ -1316,6 +1329,48 @@ public sealed class FrontRoomsMapWorld : MonoBehaviour
         FrontRoomsTransitionKit.CornerReach(postPieces, role, out reachA, out reachB);
     }
 
+    // MAP-2: the post rule on a chunk line (start area left out). The two pieces along the line are chunk-border
+    // edges, so their presence is pure; of the two cross pieces, the near one (west of a vertical line, south of a
+    // horizontal one) is built by the near chunk, like the line pieces, and the far one by the far chunk. So a far
+    // reach reads only pure data, and a near reach pure data plus the near chunk's own edges: both chunks agree
+    // after any shift, and no neighbour chunk is read or generated. Every quadrant of the post square is covered
+    // once where two or more pieces meet (no notch), by a piece at least as tall as the room it faces:
+    //  - one finish round the post: today's reach (nothing can z-fight);
+    //  - no piece on the line: each cross piece runs to the cell line and covers its own half;
+    //  - both line pieces: the run owns the post, each piece its half; the cross pieces stop at the post face;
+    //  - one line piece L: the far cross piece stops at the post face, and B0.1's own rule runs on L and the near
+    //    cross piece. That is safe only if the room on the far side of the post is no taller than L and the near
+    //    cross piece; where it is taller, today's reach stays (the only case left that can z-fight).
+    void ChunkLineReach(GridCoord sw, GridCoord se, GridCoord nw, GridCoord ne, bool onX, int role, out int reachA, out int reachB)
+    {
+        reachA = reachB = 1;
+        float Ceiling(GridCoord c) => MapGrid.CeilingHeight(Cache.ZoneOf(c).height);
+        long Finish(GridCoord c) => ((long)Theme(Cache.ZoneOf(c).theme).wall.GetInstanceID() << 4) | (long)HeightClass(Ceiling(c));
+        var f = Finish(sw);
+        if (Finish(se) == f && Finish(nw) == f && Finish(ne) == f) return;
+        int lo = onX ? FrontRoomsTransitionKit.South : FrontRoomsTransitionKit.West;
+        int hi = onX ? FrontRoomsTransitionKit.North : FrontRoomsTransitionKit.East;
+        int near = onX ? FrontRoomsTransitionKit.West : FrontRoomsTransitionKit.South;
+        int far = onX ? FrontRoomsTransitionKit.East : FrontRoomsTransitionKit.North;
+        var pLo = onX ? PostPieceAt(sw, se, false) : PostPieceAt(sw, nw, true);
+        var pHi = onX ? PostPieceAt(nw, ne, false) : PostPieceAt(se, ne, true);
+        var line = role == lo || role == hi;
+        if (!pLo.present && !pHi.present) { if (!line) reachA = reachB = 0; return; }
+        if (pLo.present && pHi.present) { reachA = reachB = line ? 0 : -1; return; }
+        var l = pLo.present ? lo : hi;
+        var risk = onX ? (l == lo ? ne : se) : (l == lo ? ne : nw);
+        var hL = onX ? (l == lo ? Mathf.Max(Ceiling(sw), Ceiling(se)) : Mathf.Max(Ceiling(nw), Ceiling(ne)))
+                     : (l == lo ? Mathf.Max(Ceiling(sw), Ceiling(nw)) : Mathf.Max(Ceiling(se), Ceiling(ne)));
+        var hNear = onX ? Mathf.Max(Ceiling(sw), Ceiling(nw)) : Mathf.Max(Ceiling(sw), Ceiling(se));
+        if (Ceiling(risk) > Mathf.Min(hL, hNear) + .001f) return;
+        if (role == far) { reachA = reachB = -1; return; }
+        postPieces[lo] = l == lo ? pLo : default;
+        postPieces[hi] = l == hi ? pHi : default;
+        postPieces[near] = onX ? PostPieceAt(sw, nw, true) : PostPieceAt(sw, se, false);
+        postPieces[far] = default;
+        FrontRoomsTransitionKit.CornerReach(postPieces, role, out reachA, out reachB);
+    }
+
     FrontRoomsTransitionKit.PostPiece PostPieceAt(GridCoord a, GridCoord b, bool northward)
     {
         var za = Cache.ZoneOf(a);
@@ -1324,7 +1379,7 @@ public sealed class FrontRoomsMapWorld : MonoBehaviour
         var hb = MapGrid.CeilingHeight(zb.height);
         var h = Mathf.Max(ha, hb);
         Material wa = Theme(za.theme).wall, wb = Theme(zb.theme).wall;
-        var kind = StartAreaEdge(Cache.Edge(a, b), a, b, northward, ref h, ref wa, ref wb);
+        var kind = StartAreaEdge(EdgeNoGenerate(a, b), a, b, northward, ref h, ref wa, ref wb);
         if (kind == EdgeKind.Open) return default;
         var startEdge = InStartArea(a) || InStartArea(b);
         var ca = HeightClass(startEdge ? h : ha);
```

Apply from the project root, after C2: `git apply Documentation/research/codex_audit/contracts/C3_MAP2_chunk_line_posts.diff`.

## Verified (2026-10-14, clone `W/proj_cxreport` = main `0a9ee7f` + tree; Unity 6000.3.10f1 batch)

Clone-only harness `FrontRoomsCxMapContracts` (copy: `harness/FrontRoomsCxMapContracts.cs.txt`). 20 seeds (the audit's signature seeds), 25 chunks each, root (4992, 0, 4992).

**T1, shift independence.** Build; shift and rebuild every "even" chunk (x + y even, the game's revisit shift); then compare every still-built "odd" chunk with a fresh rebuild of itself. The generator's rule says they are equal.

| Code | Still-built chunks ≠ their own rebuild (geometry) | Chunk data generated beyond the 500 built |
|---|---|---|
| main, B0 off (`-b0Off`, the pre-Codex rule) | 0 / 240 | 30 |
| **main as is (B0 on)** | **115 / 240**, all 20 seeds | **429** |
| main + C2 + C3 | **0 / 240** | 30 |

Lights: 0 / 240 differ in every case.

**T1b, the post square.** Every post whose four cells lie in built chunks (start area left out): 24,500 inside chunks, 5,600 on chunk lines (1,628 of them with mixed finishes), 320 at chunk corners. Each piece's presence, skin finishes and reach are read with the map's own `PostPieceAt` / `CornerReach`, as built: a piece of a shifted chunk as rebuilt, every other piece as first built. The square is split into 4 quadrants. **Notch** = two or more pieces meet and a quadrant is empty. **Overlap** = a quadrant covered by two different finishes (a z-fight). **High** = a quadrant whose tallest cover is lower than the room it faces. **Strip** = a covered quadrant whose open face shows another room's paper (a static 8 cm strip, no flicker). The other rules are simulated from the same data; "compiled" equals RD exactly.

| Rule (chunk-line posts, 5,600) | First build: notch / overlap / high / strip | After shifts: notch / overlap / high / strip | Chunk corners (320), overlap |
|---|---|---|---|
| main today (B0.1 everywhere) | 0 / 0 / 0 / 48 | **90 / 9** / 0 / 44 | 0 |
| "+1 on chunk lines" (the rejected patch = pre-B0) | 0 / **1,315** / 0 / 1,036 | 0 / 1,333 / 0 / 1,053 | 113 |
| the verifier's run rule (`20_findings.md` dropped notes) | 0 / 306 / 0 / 341 | 0 / 313 / 0 / 343 | 113 |
| **C3 (this patch)** | **0 / 24** / 0 / 202 | **0 / 24** / 0 / 196 | **0** |

Inside chunks every rule is the same (0 / 0 / 0 / 211, B0.1 unchanged). No rule has a "high" hole.

What this means for Red:
- C3 never leaves a notch and never depends on build order.
- It keeps 24 z-fight posts in 5,600 chunk-line posts (0.4 %, about 1 in 20 chunks): the posts where a taller room sits on the far side. Today's main has none in a fresh build, but 90 notches + 9 z-fights after this test's shifts.
- It adds 154 static paper strips (8 cm of the next room's paper at an outside corner where the cross arm is the other chunk's): no flicker.
- No pure per-piece rule can reach 0 / 0 / 0: the far chunk cannot know the near cross piece. The full fix is the verifiers' "post seam": fill chunk-line posts with quarter boxes, each in its own cell's finish and height, rebuilt by whichever chunk builds last (a world-level table keyed by post, as V5 §3.5 asks). That is the map chat's call; C3 is the safe step before it.

**T2/T3 and suites:** see C2 and `README.md` §2 (suites, signatures, Play run).

**Frames:** VL272 (`3150:7042`, FAIL main / PASS C3): seed 20261002, post (0, 4), Office. Main after the shift shows a full-height slot in the corner (158,417 px differ from the view after the stale chunk is rebuilt); with C2 + C3 the same shift leaves the corner whole (0 px differ). Images: `../images/map2_notch_main_after_shift.jpg`, `map2_notch_c3_after_shift.jpg`, `map2_notch_main_stale_rebuilt.jpg` (+ context `map2_notch_main_after_shift_wide.jpg`).

## How to test

1. Your suites with C2 + C3: `FrontRoomsMapVerification` 100 / 0, `FrontRoomsMapInteractionTests` 152 / 0, `FrontRoomsLevelDesignerTests`, `FrontRoomsRelayNavTest`, `FrontRoomsStreamVerification`: same counts as without.
2. The shift test above (the harness copy is in `harness/`; run `-executeMethod FrontRoomsCxMapContracts.RunBatch -cxLabel <name>` in a clone; it exits by itself): T1 must read 0 / 240 with B0 on.
3. In Play: walk away from a mixed chunk-line junction (an Office | Level 0 border on a chunk line) beyond `buildRadius`, wait more than 30 s, come back. With main as is the post can show an empty quarter; with C3 it cannot.
4. B0 off (`-transitionsOff` or `-b0Off`) must still build the old boxes exactly: `BuildEdge` calls `CornerReach` only when `FrontRoomsTransitionKit.B0` is true.
