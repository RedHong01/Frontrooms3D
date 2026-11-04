using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Run! direction B (EMERGENCY POWER), production dress (run_b_production/10_plan.md s.1.2-s.1.3, s.2.4): the
/// finishes and props of one Run room, from a RunRigLayout, under "Run B / dress". It knows nothing about the
/// map or the stream.
///   Floor: glossy waxed 12 in VCT (Red, 2026-10-03), world-projected 1.2 m (RunB_VCT), laid as ADJACENT slabs
///     (no overlay, no shader change): field, two accent rows on the lane edges (snapped to the world 0.30 m tile
///     grid), a burnished lane and a waxed 0.15 m perimeter. One texture set fits the 3 m corridor and the 11.5 m room.
///   Walls: a lower-tone slab 0 - 1.2 m (RunB_BlockLower, 2 mm proud), a cove base and, on the long walls, a vinyl
///     bumper rail at 0.9 m; the upper tone is the host's wall (or a shell slab when the layout builds the shell).
///   Overhead: the red sprinkler main with upright heads and band hangers every 3 m, the ladder tray on its
///     trapezes, three 3/4 in EMT runs with straps every 1.5 m and a handy box every 3 m.
///   Fire gear (red: sprinkler main, pull station, bell, extinguisher cabinet), plates, locked RN-K side-door props
///   (2026-10-31: Kit_DoorFrame_Steel_DA + Kit_DoorLeaf_SteelRun_DA, the map's RN-K row; F4 frame, A12 no closer, a dead
///   F5 lite, the locked trim on the corridor face, almond leaf, plate or NOT AN EXIT stencil over the lite), obstacles.
///   No EXIT sign anywhere (C41).
///   Shell: look-dev builds its own (buildShell); on the map the map builds the structure and the dress skins the corridor's
///   inner faces (skinShell: the upper tone and the soffit 2 mm proud, the arch's reveal on the room's half of the wall, the
///   VCT 1 mm over the map's floor), so the map's CR-3 finish (FrontRoomsSurfaces' RoomRule.Run set, which the title stream
///   shares and keeps as Ward's, C40) shows nowhere. The goal door's DA frame sleeves its own reveal.
/// Stage C (60_production_66.md): a wall item outside the corridor (the NOTICE plate on the host face, F8) hangs under
/// <c>parent</c>, not under the dress, so it takes the host's light and ambient like the ALARM ZONE cards; the overhead
/// services cast no shadow (every wrap is shadowed since F7: the tray and the main would band the walls, F9, as r8 never did).
/// Render-only except the obstacles (box colliders from their sidecars) and the look-dev shell.
/// Every object name avoids the room stream's material words (floor, carpet, ceiling, wall, seal, partition).
/// </summary>
public static class FrontRoomsRunDress
{
    const float Grid = .30f;     // the VCT tile (RunB_VCT: 1.2 m world tile = 4 x 4)

    static Material Mat(string name) => FrontRoomsSurfaces.Get(name);

    /// <summary>The side doors (the map's RN-K row, FrontRoomsInteractableKit.DoorRowFor(Run, true)).</summary>
    public const string SideDoorFrameKit = "Kit_DoorFrame_Steel_DA", SideDoorLeafKit = "Kit_DoorLeaf_SteelRun_DA";
    /// <summary>The locked trim (the map's RN-K row: escutcheon, cylinder shell, plug, knob).</summary>
    public const string EscutcheonKit = "Kit_Lock_Escutcheon", CylinderKit = "Kit_Lock_CylinderShell", PlugKit = "Kit_Lock_Plug", KnobKit = "Kit_Lock_Knob";
    /// <summary>The side door's plate or stencil: centred on the leaf, over the F5 lite (the lite fills 1.194-1.575 m).</summary>
    public const float SideDoorPlateY = 1.75f;

    public static Transform Build(Transform parent, RunRigLayout l)
    {
        var root = new GameObject("Run B / dress").transform;
        root.SetParent(parent, false);
        Vct(root, l);
        if (l.buildShell) Shell(root, l);
        else if (l.skinShell) Skin(root, l);
        Faces(root, l);
        var overhead = new GameObject("run b overhead services").transform;
        overhead.SetParent(root, false);
        Overhead(overhead, l);
        RemoteCircuit(overhead, l);
        foreach (var r in overhead.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
        for (var i = 0; i < l.wallItems.Length; i++)
        {
            var it = l.wallItems[i];
            var outside = it.point.z < -.001f || it.point.z > l.length + .001f;
            Kit(it.kit, outside ? parent : root, it.point, Quaternion.LookRotation(it.normal, Vector3.up), false, "run b " + it.kit);
        }
        SideDoors(root, l);
        for (var i = 0; i < l.obstacles.Length; i++)
        {
            var o = l.obstacles[i];
            var go = Kit(o.kit, root, o.position, Quaternion.Euler(0f, o.yaw, 0f), true, "run b obstacle " + o.kit);
            if (go != null)
                foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.On;
        }
        return root;
    }

    static GameObject Kit(string asset, Transform parent, Vector3 position, Quaternion rotation, bool colliders, string label, Vector3? scale = null)
    {
        return FrontRoomsKitLibrary.Spawn(asset, parent, position, rotation, scale, colliders, label, false);
    }

    static GameObject Slab(Transform parent, string name, Vector3 centre, Vector3 size, Quaternion rotation, Material material, bool collider = false, bool shadows = true)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = centre;
        go.transform.localRotation = rotation;
        go.AddComponent<MeshFilter>().sharedMesh = FrontRoomsFilmMesh.GetPlanarBox(size);
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        if (collider) go.AddComponent<BoxCollider>().size = size;
        return go;
    }

    // ------------------------------------------------------------------ floor
    enum Band : byte { Field, Accent, Lane }

    /// <summary>The nearest world tile seam (local x).</summary>
    static float Snap(float local, float originX)
    {
        var w = originX + local;
        return Mathf.Floor(w / Grid + .5f) * Grid - originX;
    }

    /// <summary>The centre of the world tile column under local x.</summary>
    static float SnapCentre(float local, float originX)
    {
        var w = originX + local;
        return (Mathf.Floor(w / Grid) + .5f) * Grid - originX;
    }

    static void Vct(Transform root, RunRigLayout l)
    {
        var field = Mat("RunB_Floor");
        var wax = Mat("RunB_FloorWax");
        var collider = l.buildShell;
        // Look-dev: the slabs are the floor (top at 0). On the map the map's floor is at 0: the VCT lies 1 mm over it.
        var lift = l.buildShell ? 0f : .001f;
        const float t = .02f;
        float x0 = l.floorX0, x1 = l.floorX1, z0 = l.floorZ0, z1 = l.floorZ1, wb = l.waxBand;
        var zl = z1 - z0;
        // Waxed perimeter (build-up along the walls), four slabs round the inner field.
        var y = lift - t * .5f;
        Slab(root, "run b vct wax left", new Vector3(x0 + wb * .5f, y, (z0 + z1) * .5f), new Vector3(wb, t, zl), Quaternion.identity, wax, collider);
        Slab(root, "run b vct wax right", new Vector3(x1 - wb * .5f, y, (z0 + z1) * .5f), new Vector3(wb, t, zl), Quaternion.identity, wax, collider);
        Slab(root, "run b vct wax entry", new Vector3((x0 + x1) * .5f, y, z0 + wb * .5f), new Vector3(x1 - x0 - 2f * wb, t, wb), Quaternion.identity, wax, collider);
        Slab(root, "run b vct wax goal", new Vector3((x0 + x1) * .5f, y, z1 - wb * .5f), new Vector3(x1 - x0 - 2f * wb, t, wb), Quaternion.identity, wax, collider);
        // Inner field: breakpoints across x, every edge on the world tile grid.
        var originX = root.position.x;
        var ix0 = x0 + wb; var ix1 = x1 - wb;
        var cuts = new System.Collections.Generic.List<float> { ix0, ix1 };
        var lane0 = Snap(-l.laneHalf, originX); var lane1 = Snap(l.laneHalf, originX);
        cuts.Add(lane0); cuts.Add(lane1);
        // Accent rows: whole tile columns (0.30 m) centred under the layout's x.
        var accents = new float[l.accentRowsX.Length];
        var half = Mathf.Max(1, Mathf.RoundToInt(l.accentWidth / Grid)) * Grid * .5f;
        for (var i = 0; i < l.accentRowsX.Length; i++)
        {
            accents[i] = SnapCentre(l.accentRowsX[i], originX);
            cuts.Add(accents[i] - half);
            cuts.Add(accents[i] + half);
        }
        cuts.Sort();
        var iz0 = z0 + wb; var iz1 = z1 - wb;
        float runStart = ix0; Band runBand = Band.Field; var first = true;
        for (var i = 0; i + 1 < cuts.Count; i++)
        {
            var a = Mathf.Clamp(cuts[i], ix0, ix1); var b = Mathf.Clamp(cuts[i + 1], ix0, ix1);
            if (b - a < .001f) continue;
            var mid = (a + b) * .5f;
            var band = mid > lane0 && mid < lane1 ? Band.Lane : Band.Field;
            for (var k = 0; k < accents.Length; k++)
                if (Mathf.Abs(mid - accents[k]) < half) band = Band.Accent;
            if (first) { runStart = a; runBand = band; first = false; continue; }
            if (band == runBand) continue;
            Strip(root, runStart, a, iz0, iz1, runBand, t, lift, collider);
            runStart = a; runBand = band;
        }
        if (!first) Strip(root, runStart, ix1, iz0, iz1, runBand, t, lift, collider);
    }

    static void Strip(Transform root, float a, float b, float z0, float z1, Band band, float t, float lift, bool collider)
    {
        var name = band == Band.Lane ? "run b vct lane" : band == Band.Accent ? "run b vct accent" : "run b vct field";
        var mat = Mat(band == Band.Lane ? "RunB_FloorLane" : band == Band.Accent ? "RunB_FloorAccent" : "RunB_Floor");
        Slab(root, name, new Vector3((a + b) * .5f, lift - t * .5f, (z0 + z1) * .5f), new Vector3(b - a, t, z1 - z0), Quaternion.identity, mat, collider);
    }

    // ------------------------------------------------------------------ shell (corridor look-dev / a map module without its own finish)
    static void Shell(Transform root, RunRigLayout l)
    {
        var upper = Mat("RunB_BlockUpper");
        var soffit = Mat("RunB_Soffit");
        var zl = l.floorZ1 - l.floorZ0;
        Slab(root, "run b soffit deck", new Vector3(0f, l.height + .05f, (l.floorZ0 + l.floorZ1) * .5f), new Vector3(l.halfWidth * 2f + .2f, .1f, zl), Quaternion.identity, soffit);
        for (var i = 0; i < l.walls.Length; i++)
        {
            var f = l.walls[i];
            var rot = Quaternion.LookRotation(f.normal, Vector3.up);
            var shortFace = Mathf.Abs(f.along.x) > .5f;
            // Long faces are continuous (their gaps are prop gaps); short faces open their gaps up to 2.2 m.
            var spans = shortFace ? Spans(f) : new[] { new Vector2(0f, f.length) };
            for (var s = 0; s < spans.Length; s++)
            {
                var a = spans[s].x; var b = spans[s].y;
                var c = f.start + f.along * ((a + b) * .5f) - f.normal * .05f;
                Slab(root, "run b block upper", new Vector3(c.x, l.height * .5f, c.z), new Vector3(b - a, l.height, .1f), rot, upper, true);
            }
            if (shortFace && f.gaps != null)
                for (var g = 0; g < f.gaps.Length; g++)
                {
                    var a = f.gaps[g].x; var b = f.gaps[g].y;
                    var top = f.GapTop(g);
                    var c = f.start + f.along * ((a + b) * .5f) - f.normal * .05f;
                    Slab(root, "run b block header", new Vector3(c.x, (l.height + top) * .5f, c.z), new Vector3(b - a, l.height - top, .1f), rot, upper);
                }
        }
    }

    // ------------------------------------------------------------------ skin (the map module: the map's shell, the dress's surfaces)
    /// <summary>The side door frame's head: the skin over a door starts here, under the F4 casing (opening top 2.098, casing to 2.149).</summary>
    public const float SideDoorSkinBottom = 2.12f;

    /// <summary>
    /// On the map: the corridor's inner faces in the dress's own renderers, so the rig's ambient (its SH blocks) and the
    /// trip reach every visible surface (the map's shell renderers are merged per chunk and keep the global ambient). The
    /// upper tone from lowerToneTop to the soffit (the lower tone and cove are Faces'): on the long faces it stops only at
    /// the side doors (a header from SideDoorSkinBottom, under the casing) and runs over the cabinet, whose semi-recessed
    /// box stands proud of it; on the end faces it stops at the openings and closes over each from its clear top. The
    /// soffit is one slab under the ceiling; the arch gets its reveal (Reveal). 2 mm, no collider, no shadow (the map's walls
    /// and ceiling cast). The map's own shell stands behind in its CR-3 Run finish and shows nowhere.
    /// </summary>
    static void Skin(Transform root, RunRigLayout l)
    {
        var upper = Mat("RunB_BlockUpper");
        var soffit = Mat("RunB_Soffit");
        const float t = .002f;
        var zl = l.floorZ1 - l.floorZ0;
        Slab(root, "run b soffit skin", new Vector3(0f, l.height - t * .5f, (l.floorZ0 + l.floorZ1) * .5f), new Vector3(l.halfWidth * 2f, t, zl), Quaternion.identity, soffit, false, false);
        var h = l.height - l.lowerToneTop;
        for (var i = 0; i < l.walls.Length; i++)
        {
            var f = l.walls[i];
            var rot = Quaternion.LookRotation(f.normal, Vector3.up);
            var shortFace = Mathf.Abs(f.along.x) > .5f;
            Vector2[] openings;
            float[] tops;
            if (shortFace)
            {
                openings = f.gaps ?? new Vector2[0];
                tops = new float[openings.Length];
                for (var g = 0; g < openings.Length; g++) tops[g] = f.GapTop(g);
            }
            else DoorGaps(l, f, out openings, out tops);
            var spans = Spans(new RunWallFace { start = f.start, along = f.along, length = f.length, normal = f.normal, gaps = openings });
            for (var s = 0; s < spans.Length; s++)
            {
                var a = spans[s].x; var b = spans[s].y;
                var c = f.start + f.along * ((a + b) * .5f) + f.normal * (t * .5f);
                Slab(root, "run b upper tone", new Vector3(c.x, l.lowerToneTop + h * .5f, c.z), new Vector3(b - a, h, t), rot, upper, false, false);
            }
            for (var g = 0; g < openings.Length; g++)
            {
                var a = openings[g].x; var b = openings[g].y;
                var bottom = Mathf.Max(tops[g], l.lowerToneTop);
                if (bottom >= l.height - .001f) continue;
                var c = f.start + f.along * ((a + b) * .5f) + f.normal * (t * .5f);
                Slab(root, "run b upper tone header", new Vector3(c.x, (l.height + bottom) * .5f, c.z), new Vector3(b - a, l.height - bottom, t), rot, upper, false, false);
            }
            // An end-face opening without a door (the arch; the goal door's DA frame sleeves its own reveal): its reveal.
            if (shortFace && !IsGoalFace(l, f))
                for (var g = 0; g < openings.Length; g++) Reveal(root, l, f, openings[g], tops[g]);
        }
    }

    /// <summary>The end face the goal door stands in (its plane within half a wall of the goal point).</summary>
    static bool IsGoalFace(RunRigLayout l, RunWallFace f) =>
        Mathf.Abs(Vector3.Dot(new Vector3(l.goalPoint.x, 0f, l.goalPoint.z) - f.start, f.normal)) <= l.WallHalf + .01f;

    /// <summary>
    /// The arch's reveal on the room's half of the wall (the map splits a wall's two finishes at its centre line, so this
    /// half carries the Run finish): both jambs in the lower tone to lowerToneTop with the cove, the upper tone above, and
    /// the head's underside in the upper tone, 2 mm proud, from the centre line to the room face. With it the map's Run
    /// finish shows nowhere (the title stream's RoomRule.Run set stays Ward's, C40). No collider, no shadow.
    /// </summary>
    static void Reveal(Transform root, RunRigLayout l, RunWallFace f, Vector2 opening, float top)
    {
        var lower = Mat("RunB_BlockLower");
        var upper = Mat("RunB_BlockUpper");
        var cove = Mat("RunB_Cove");
        const float t = .002f;
        var depth = l.WallHalf + t;                     // the centre line to 2 mm proud of the room face
        var mid = f.normal * (depth * .5f - l.WallHalf); // the reveal's centre along the normal, from the room face plane
        var ends = new[] { opening.x, opening.y };
        for (var e = 0; e < 2; e++)
        {
            var into = e == 0 ? f.along : -f.along;     // the jamb face looks into the opening
            var rot = Quaternion.LookRotation(into, Vector3.up);
            var p = f.start + f.along * ends[e] + into * (t * .5f) + mid;
            var lowTop = Mathf.Min(l.lowerToneTop, top);
            Slab(root, "run b arch reveal lower tone", new Vector3(p.x, lowTop * .5f, p.z), new Vector3(depth, lowTop, t), rot, lower, false, false);
            Slab(root, "run b arch reveal cove", new Vector3(p.x, l.coveHeight * .5f, p.z) + into * .0005f, new Vector3(depth, l.coveHeight, .003f), rot, cove, false, false);
            if (top > lowTop + .001f)
                Slab(root, "run b arch reveal upper tone", new Vector3(p.x, (lowTop + top) * .5f, p.z), new Vector3(depth, top - lowTop, t), rot, upper, false, false);
        }
        var hc = f.start + f.along * ((opening.x + opening.y) * .5f) + mid;
        Slab(root, "run b arch reveal head", new Vector3(hc.x, top - t * .5f, hc.z), new Vector3(opening.y - opening.x, t, depth), Quaternion.LookRotation(f.normal, Vector3.up), upper, false, false);
    }

    /// <summary>The side doors on a long face, as (from, to) along it (the leaf plus the casing's 45 mm, as Corridor1x9's
    /// gaps) with the skin's header bottom.</summary>
    static void DoorGaps(RunRigLayout l, RunWallFace f, out Vector2[] gaps, out float[] tops)
    {
        var list = new System.Collections.Generic.List<Vector2>();
        var faceSide = f.normal.x < 0f ? 1f : -1f;   // the right wall (x = +halfWidth) faces -X
        for (var i = 0; i < l.sideDoors.Length; i++)
        {
            var d = l.sideDoors[i];
            if (Mathf.Sign(d.side) != faceSide) continue;
            var along = Vector3.Dot(new Vector3(0f, 0f, d.z) - f.start, f.along);
            list.Add(new Vector2(along - SideDoorHalfGap, along + SideDoorHalfGap));
        }
        gaps = list.ToArray();
        tops = new float[gaps.Length];
        for (var i = 0; i < tops.Length; i++) tops[i] = SideDoorSkinBottom;
    }

    /// <summary>A side door's half gap in the wall finishes: the leaf's half width plus 45 mm under the F4 casing (0.549).</summary>
    public const float SideDoorHalfGap = .545f;

    static Vector2[] Spans(RunWallFace f)
    {
        var list = new System.Collections.Generic.List<Vector2>();
        var at = 0f;
        var gaps = f.gaps ?? new Vector2[0];
        var sorted = (Vector2[])gaps.Clone();
        System.Array.Sort(sorted, (p, q) => p.x.CompareTo(q.x));
        for (var i = 0; i < sorted.Length; i++)
        {
            if (sorted[i].x > at + .001f) list.Add(new Vector2(at, sorted[i].x));
            at = Mathf.Max(at, sorted[i].y);
        }
        if (f.length > at + .001f) list.Add(new Vector2(at, f.length));
        return list.ToArray();
    }

    // ------------------------------------------------------------------ wall finishes
    static void Faces(Transform root, RunRigLayout l)
    {
        var lower = Mat("RunB_BlockLower");
        var cove = Mat("RunB_Cove");
        for (var i = 0; i < l.walls.Length; i++)
        {
            var f = l.walls[i];
            var rot = Quaternion.LookRotation(f.normal, Vector3.up);
            var spans = Spans(f);
            var longFace = Mathf.Abs(f.along.z) > .5f;
            // On the map (skin) the lower tone and cove stop only at the side doors: they run on behind the semi-recessed
            // cabinet (its box stands 85 mm proud, its bottom at 0.795 m), so the map's own finish never shows under it.
            // The look-dev shell keeps every gap. The rail keeps every gap either way (it would cross the cabinet).
            var finish = spans;
            if (l.skinShell && !l.buildShell && longFace)
            {
                DoorGaps(l, f, out var doorGaps, out _);
                finish = Spans(new RunWallFace { start = f.start, along = f.along, length = f.length, normal = f.normal, gaps = doorGaps });
            }
            for (var s = 0; s < finish.Length; s++)
            {
                var a = finish[s].x; var b = finish[s].y;
                var c = f.start + f.along * ((a + b) * .5f);
                Slab(root, "run b lower tone", c + f.normal * .001f + Vector3.up * (l.lowerToneTop * .5f), new Vector3(b - a, l.lowerToneTop, .002f), rot, lower, false, false);
                Slab(root, "run b cove", c + f.normal * .0015f + Vector3.up * (l.coveHeight * .5f), new Vector3(b - a, l.coveHeight, .003f), rot, cove, false, false);
            }
            for (var s = 0; s < spans.Length; s++)
            {
                var a = spans[s].x; var b = spans[s].y;
                if (!longFace) continue;
                // Bumper rail tiles (1.2 m) wherever a whole tile fits in the span.
                var n = Mathf.FloorToInt((b - a - .02f) / 1.2f);
                var start = a + ((b - a) - n * 1.2f) * .5f;
                for (var k = 0; k < n; k++)
                {
                    var p = f.start + f.along * (start + .6f + 1.2f * k);
                    Kit("Kit_BumperRail_1200", root, new Vector3(p.x, l.railY, p.z), rot, false, "run b bumper rail");
                }
            }
        }
    }

    // ------------------------------------------------------------------ overhead services
    static void Overhead(Transform root, RunRigLayout l)
    {
        var galv = Mat("RunB_GalvSteel");
        var along = Quaternion.Euler(0f, 90f, 0f);   // kit +X (Blender) = Unity -X -> along +Z
        var z0 = l.runZ0; var z1 = l.runZ1;
        for (var z = z0; z < z1 - .01f; z += 3f)
        {
            Kit("Kit_SprinklerMain_3m", root, new Vector3(l.mainX, l.mainY, z), along, false, "run b sprinkler main");
            Kit("Kit_SprinklerHead_Upright", root, new Vector3(l.mainX, l.mainY + .05f, z + 1.5f), Quaternion.identity, false, "run b sprinkler head");
            Kit("Kit_PipeHanger_Band", root, new Vector3(l.mainX, l.mainY, z + .5f), along, false, "run b pipe hanger");
            Kit("Kit_CableTray_3m", root, new Vector3(l.trayX, l.trayY, z), along, false, "run b cable tray");
            // Tray trapeze: a strut under the tray on two threaded rods to the deck.
            var tz = z + .4f;
            Slab(root, "run b tray strut", new Vector3(l.trayX, l.trayY - .0105f, tz), new Vector3(.42f, .021f, .041f), Quaternion.identity, galv, false, false);
            for (var s = -1; s <= 1; s += 2)
            {
                var top = l.height + .02f; var bottom = l.trayY - .021f;
                Slab(root, "run b tray rod", new Vector3(l.trayX + s * .19f, (top + bottom) * .5f, tz), new Vector3(.0095f, top - bottom, .0095f), Quaternion.identity, galv, false, false);
            }
        }
        // EMT: three 3/4 in runs on one wall (1 m tiles), straps every 1.5 m, a handy box every 3 m.
        var n = l.emtWallX > 0f ? Vector3.left : Vector3.right;
        var run = Quaternion.LookRotation(n, Vector3.forward);
        var tiles = Mathf.FloorToInt(z1 - z0 + .001f);
        for (var e = 0; e < l.emtY.Length; e++)
        {
            for (var k = 0; k < tiles; k++)
                Kit("Kit_ConduitEMT", root, new Vector3(l.emtWallX, l.emtY[e], z0 + k), run, false, "run b emt");
            for (var zs = z0 + .75f; zs < z1; zs += 1.5f)
                Kit("Kit_ConduitStrap", root, new Vector3(l.emtWallX, l.emtY[e], zs), run, false, "run b emt strap");
        }
        if (l.emtY.Length > 0)
        {
            var low = l.emtY[0];
            for (var e = 1; e < l.emtY.Length; e++) low = Mathf.Min(low, l.emtY[e]);
            for (var zb = z0 + 1.5f; zb < z1; zb += 3f)
                Kit("Kit_OutletHandyBox", root, new Vector3(l.emtWallX, low - .113f, zb), Quaternion.LookRotation(n, Vector3.up), false, "run b junction box");
        }
    }

    // ------------------------------------------------------------------ the remote circuit (zone lamps, T-B)
    // The unit root sits UnitRootBelowCentre under the cabinet's centre (FrontRoomsRunRig.BuildUnits); the remote mount's
    // octagon box (ZL_RPLATE, built in the cabinet's frame) is centred RemoteBoxCentre above it, its side flats RemoteBoxFlat
    // off its centre (the 4 in box's 54 mm corner radius x cos 22.5 deg): where the run enters.
    const float UnitRootBelowCentre = .0955f, RemoteBoxCentre = .095f, RemoteBoxFlat = .0499f;
    // 3/4 in EMT as Kit_ConduitEMT: 17.9 mm across, its axis 11.5 mm off the wall; corners bent on a hand bender's 4.5 in
    // centreline radius; strapped within 0.3 m of each box and at most every 1.5 m between (the dress's own runs: 1.5 m).
    const float EmtRadius = .00895f, EmtOffset = .0115f, EmtBend = .114f, StrapEnd = .3f, StrapPitch = 1.5f;
    static Mesh bendMesh;

    /// <summary>
    /// T-B (zone lamps, Red 2026-10-31: T-A and T-B combined): the remote heads (RunUnitDef.remote) are fed from the entry
    /// unit (the self-contained unit nearest the entrance) by surface EMT at the remote boxes' centre height: along the entry
    /// unit's wall from its cabinet's far side into each box beyond it, and to the other wall from its cabinet's entrance
    /// side, round the corner, across the entrance wall above the arch and along the other wall into its boxes. Built only
    /// with the remote-mount kit in the build (without it every unit keeps its cabinet and its own feed). Corridor1x9: z 3 R
    /// feeds z 15 R (11.8 m) and z 9 L (2.9 m + 2.9 m across + 8.8 m); clear of the fire bell (z 1, top 2.38 m), the left
    /// wall's three runs and handy boxes (2.56 m up) and the side doors. Render-only, no shadows, as the overhead services.
    /// </summary>
    static void RemoteCircuit(Transform root, RunRigLayout l)
    {
        if (l.units == null || !FrontRoomsLampKit.Available(FrontRoomsLampKit.Family.RemotePlate)) return;
        var source = -1;
        for (var u = 0; u < l.units.Length; u++)
            if (!l.units[u].remote && (source < 0 || l.units[u].z < l.units[source].z)) source = u;
        if (source < 0) return;
        var s = l.units[source];
        var cabinet = FrontRoomsKitLibrary.GetInfo("Kit_EmergencyTwinHead");
        var cabinetHalf = cabinet != null ? cabinet.Max.x : .17275f;
        var y = l.unitBoxCentreY - UnitRootBelowCentre + RemoteBoxCentre;
        var sameWall = new System.Collections.Generic.List<float>();
        var otherWall = new System.Collections.Generic.List<float>();
        for (var u = 0; u < l.units.Length; u++)
        {
            var d = l.units[u];
            if (!d.remote) continue;
            if (d.side * s.side < 0f) otherWall.Add(d.z);
            else if (d.z > s.z) sameWall.Add(d.z);   // a remote between the entrance and the entry unit is not fed (no layout has one)
        }
        sameWall.Sort();
        otherWall.Sort();
        const string label = "run b remote emt";
        var from = s.z + cabinetHalf;
        for (var i = 0; i < sameWall.Count; i++)
        {
            WallRun(root, l, s.side, y, from, sameWall[i] - RemoteBoxFlat, label);
            from = sameWall[i] + RemoteBoxFlat;
        }
        if (otherWall.Count == 0) return;
        var corner = l.WallHalf + EmtOffset + EmtBend;   // where a long wall's run meets its bend
        var across = l.halfWidth - EmtOffset - EmtBend;  // where the entrance wall's run meets each bend
        WallRun(root, l, s.side, y, corner, s.z - cabinetHalf, label);
        Bend(root, l, s.side, y, label + " bend");
        Tiles(root, new Vector3(-across, y, l.WallHalf), Vector3.right, 2f * across, Quaternion.LookRotation(Vector3.forward, Vector3.right), label);
        Bend(root, l, -s.side, y, label + " bend");
        from = corner;
        for (var i = 0; i < otherWall.Count; i++)
        {
            WallRun(root, l, -s.side, y, from, otherWall[i] - RemoteBoxFlat, label);
            from = otherWall[i] + RemoteBoxFlat;
        }
    }

    /// <summary>A run on a long wall (x = side x halfWidth, its face) from z0 to z1 at height y.</summary>
    static void WallRun(Transform root, RunRigLayout l, float side, float y, float z0, float z1, string label)
    {
        if (z1 - z0 < .01f) return;
        var x = side > 0f ? l.halfWidth : -l.halfWidth;
        Tiles(root, new Vector3(x, y, z0), Vector3.forward, z1 - z0, Quaternion.LookRotation(side > 0f ? Vector3.left : Vector3.right, Vector3.forward), label);
    }

    /// <summary>EMT from a wall-face point along <paramref name="dir"/>: 1 m tiles and the rest scaled, then its straps.
    /// <paramref name="rotation"/> turns the kit's +Z out of the wall and its +Y along the run (as the dress's own runs).</summary>
    static void Tiles(Transform root, Vector3 start, Vector3 dir, float length, Quaternion rotation, string label)
    {
        var n = Mathf.FloorToInt(length + .0001f);
        for (var k = 0; k < n; k++) Kit("Kit_ConduitEMT", root, start + dir * k, rotation, false, label);
        var rest = length - n;
        if (rest > .002f) Kit("Kit_ConduitEMT", root, start + dir * n, rotation, false, label, new Vector3(1f, rest, 1f));
        if (length < 2f * StrapEnd)
        {
            Kit("Kit_ConduitStrap", root, start + dir * (length * .5f), rotation, false, label + " strap");
            return;
        }
        var spans = Mathf.Max(1, Mathf.CeilToInt((length - 2f * StrapEnd) / StrapPitch));
        for (var k = 0; k <= spans; k++)
            Kit("Kit_ConduitStrap", root, start + dir * (StrapEnd + (length - 2f * StrapEnd) * k / spans), rotation, false, label + " strap");
    }

    /// <summary>The 90 deg bend in the corner of the entrance wall and the long wall on <paramref name="side"/>: from the long
    /// wall's run (tangent at z = corner) to the entrance wall's (tangent at x = side x across).</summary>
    static void Bend(Transform root, RunRigLayout l, float side, float y, string label)
    {
        var go = new GameObject(label);
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(side * (l.halfWidth - EmtOffset - EmtBend), y, l.WallHalf + EmtOffset + EmtBend);
        // The mesh's quarter runs from +X to +Z round its centre: turned to run from the long wall (side x X) to -Z.
        go.transform.localRotation = Quaternion.Euler(0f, side > 0f ? 90f : 180f, 0f);
        go.AddComponent<MeshFilter>().sharedMesh = BendMesh();
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = Mat("Prop_Aluminium");   // Kit_ConduitEMT's slot
        r.shadowCastingMode = ShadowCastingMode.Off;
    }

    /// <summary>A quarter torus: centreline radius EmtBend from +X to +Z about the origin, tube radius EmtRadius; uv in metres.</summary>
    static Mesh BendMesh()
    {
        if (bendMesh != null) return bendMesh;
        const int along = 12, round = 12;
        var verts = new Vector3[(along + 1) * (round + 1)];
        var normals = new Vector3[verts.Length];
        var uvs = new Vector2[verts.Length];
        for (var i = 0; i <= along; i++)
        {
            var a = Mathf.PI * .5f * i / along;
            var radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            for (var j = 0; j <= round; j++)
            {
                var f = Mathf.PI * 2f * j / round;
                var n = radial * Mathf.Cos(f) + Vector3.up * Mathf.Sin(f);
                var k = i * (round + 1) + j;
                verts[k] = radial * EmtBend + n * EmtRadius;
                normals[k] = n;
                uvs[k] = new Vector2(EmtBend * a, EmtRadius * f);
            }
        }
        var tris = new int[along * round * 6];
        var t = 0;
        for (var i = 0; i < along; i++)
            for (var j = 0; j < round; j++)
            {
                var p = i * (round + 1) + j;
                var q = p + round + 1;
                // Front faces outward: Unity's front side is Cross(b - a, c - a); flip the quad when it points inward.
                var outward = Vector3.Dot(Vector3.Cross(verts[q] - verts[p], verts[q + 1] - verts[p]), normals[p] + normals[q + 1]) > 0f;
                if (outward) { tris[t++] = p; tris[t++] = q; tris[t++] = q + 1; tris[t++] = p; tris[t++] = q + 1; tris[t++] = p + 1; }
                else { tris[t++] = p; tris[t++] = q + 1; tris[t++] = q; tris[t++] = p; tris[t++] = p + 1; tris[t++] = q + 1; }
            }
        bendMesh = new Mesh { name = "run b remote emt bend" };
        bendMesh.vertices = verts;
        bendMesh.normals = normals;
        bendMesh.uv = uvs;
        bendMesh.triangles = tris;
        bendMesh.RecalculateTangents();
        bendMesh.RecalculateBounds();
        return bendMesh;
    }

    // ------------------------------------------------------------------ locked side doors (props)
    // The locked trim's offsets on the leaf face (FrontRoomsInteractableKit.Door.cs, DoorParts: EscKeyhole, EscKnobPivot,
    // FaceS), so a prop door carries the same hardware in the same place as a map RN-K door.
    static readonly Vector3 EscKeyhole = new Vector3(0f, .032f, .0095f), EscKnobPivot = new Vector3(0f, -.0315f, .002f);
    static readonly Quaternion FaceS = Quaternion.Euler(0f, 90f, 0f);

    /// <summary>
    /// The locked side doors (B: ELECTRICAL ROOM, STAFF ONLY, NOT AN EXIT), non-opening props in the long walls, on the map's
    /// RN-K row (door vision set A, 2026-10-30; A12, F4, 10_runb_state s.7):
    /// - frame Kit_DoorFrame_Steel_DA (F4, 50.8 mm face, its casings 11.1 mm proud of a 0.16 m wall) and leaf
    ///   Kit_DoorLeaf_SteelRun_DA (the F5 15 x 15 in lite), both on the hinge point as the map hangs them; no closer (A12);
    /// - a prop has no opening, so the frame is squeezed along the wall normal until its far casing sits 2.5 mm inside the
    ///   wall (it never shows on the neighbour's face): x 0.92 with the 0.16 m wall, the near casing 10.2 mm proud;
    /// - the leaf 2 mm proud of the face (the wall stands behind it), its F5 lite DEAD for good (FrontRoomsDoorLite.AttachDead:
    ///   dark wired glass, no peek: the kit is spawned without its EXAMINE block, so nothing offers a peek onto the wall);
    /// - the locked trim (escutcheon, cylinder, plug, knob) on the corridor face, the kits' satin chrome / stainless swaps,
    ///   the leaf in B's almond enamel; the plate or the NOT AN EXIT stencil centred over the lite.
    /// A prop never rattles and the Relay never breaks it; real RN-K edges would be map work.
    /// </summary>
    static void SideDoors(Transform root, RunRigLayout l)
    {
        if (l.sideDoors == null || l.sideDoors.Length == 0) return;
        var almond = Mat("Prop_SteelAlmond");
        var satin = FrontRoomsSurfaces.TryGet(FrontRoomsInteractableKit.HardwareChromeTo);
        var stainless = FrontRoomsSurfaces.TryGet(FrontRoomsInteractableKit.LeafPlateTo) ?? satin;
        var frameInfo = FrontRoomsKitLibrary.GetInfo(SideDoorFrameKit);
        var leafInfo = FrontRoomsKitLibrary.GetInfo(SideDoorLeafKit);
        var wallHalf = l.WallHalf;
        // The frame's half depth (both casings) and the squeeze that keeps its far casing inside the wall.
        var frameHalf = frameInfo != null ? Mathf.Max(-frameInfo.Min.x, frameInfo.Max.x) : .0911f;
        var squeeze = Mathf.Clamp((l.wallThickness - .0025f) / (wallHalf + frameHalf), .8f, 1f);
        var frameBack = wallHalf * squeeze;    // the near casing's back on the wall face
        var leafFace = leafInfo != null && leafInfo.TryAnchor("escutcheon_s", out var esc0) ? esc0.x : .022f;
        for (var i = 0; i < l.sideDoors.Length; i++)
        {
            var d = l.sideDoors[i];
            var right = d.side > 0f;
            var face = right ? l.halfWidth : -l.halfWidth;
            var n = right ? Vector3.left : Vector3.right;          // into the corridor
            // Local +X (the kits' S face) into the corridor, local +Z from the hinge to the latch along the wall.
            var rot = Quaternion.Euler(0f, right ? 180f : 0f, 0f);
            var hingeZ = right ? d.z + .5f : d.z - .5f;
            var frame = Kit(SideDoorFrameKit, root, new Vector3(face - n.x * frameBack, 0f, hingeZ), rot, false, "run b side door frame", new Vector3(squeeze, 1f, 1f));
            Swap(frame, FrontRoomsInteractableKit.HardwareChromeFrom, satin);
            var leaf = Kit(SideDoorLeafKit, root, new Vector3(face - n.x * (leafFace - .002f), 0f, hingeZ), rot, false, "run b side door leaf");
            if (leaf == null) continue;
            foreach (var r in leaf.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
            Swap(leaf, "Door_Enamel", almond);
            Swap(leaf, FrontRoomsInteractableKit.HardwareChromeFrom, satin);
            Swap(leaf, FrontRoomsInteractableKit.LeafPlateFrom, stainless);
            if (leafInfo != null && leafInfo.HasLite) FrontRoomsDoorLite.AttachDead(leaf, leaf.transform, leafInfo.LiteRect);
            // The locked trim, S face (the corridor's), as DoorParts.Build hangs it.
            var lt = leaf.transform;
            var esc = leafInfo != null && leafInfo.TryAnchor("escutcheon_s", out var e) ? e : new Vector3(.022f, .968f, .920f);
            var keyhole = esc + FaceS * EscKeyhole;
            Swap(Kit(EscutcheonKit, lt, esc, FaceS, false, "run b side door escutcheon"), FrontRoomsInteractableKit.HardwareChromeFrom, satin);
            Swap(Kit(CylinderKit, lt, keyhole, FaceS, false, "run b side door cylinder"), FrontRoomsInteractableKit.HardwareChromeFrom, satin);
            Swap(Kit(PlugKit, lt, keyhole, FaceS, false, "run b side door plug"), FrontRoomsInteractableKit.HardwareChromeFrom, satin);
            Swap(Kit(KnobKit, lt, esc + FaceS * EscKnobPivot, FaceS, false, "run b side door knob"), FrontRoomsInteractableKit.HardwareChromeFrom, satin);
            // The plate (10 x 7 in) or the NOT AN EXIT stencil (22 in), centred on the leaf over the F5 lite.
            var plate = d.face == "staff" ? "Kit_SafetyPlate_Staff" : d.face == "electrical" ? "Kit_SafetyPlate_Electrical" : "Kit_StencilNotExit";
            var centreZ = leafInfo != null ? (leafInfo.Min.z + leafInfo.Max.z) * .5f : .5f;
            Kit(plate, lt, new Vector3(leafFace + .0005f, SideDoorPlateY, centreZ), FaceS, false, "run b side door " + d.face);
        }
    }

    /// <summary>Every slot named <paramref name="from"/> on every renderer of a part (all LODs) takes <paramref name="to"/>.</summary>
    static void Swap(GameObject go, string from, Material to)
    {
        if (go == null || to == null) return;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            var changed = false;
            for (var m = 0; m < mats.Length; m++)
                if (mats[m] != null && mats[m].name.StartsWith(from, System.StringComparison.Ordinal)) { mats[m] = to; changed = true; }
            if (changed) r.sharedMaterials = mats;
        }
    }
}
