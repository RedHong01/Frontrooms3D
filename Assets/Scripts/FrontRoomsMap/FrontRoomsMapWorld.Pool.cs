using System;
using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.Rendering;

// Pool family (structures scene, scene 4, RULES 5b; s67_pool DESIGN sections 4-6, structures PLAN 2.7): the map's pool
// pass. Every entry here is reached only through a hook that tests Cache.HasPool (or a pool theme) first, and a cache
// holds a plan only when the structures scene asks for one: with no plan the map builds byte for byte as before.
// Pool cells are reserved (IsReserved), so the map's own floor, ceiling, lamp, key, column and furniture code leaves them
// alone; this pass builds their floors (flat, sloped, stepped with a dark nosing and a ramp collider), ceilings, wall
// skins (tile, upper finish, the aqua course), risers, weirs and copings, rect and round-headed arches with trims,
// observation windows, clear glass, water and greybox caustics, the family's lamps (as map fixtures, so the light budget,
// flicker and Release work), columns, missing ceiling tiles and the STAND-IN kits. Doors and windows stay BuildEdge's.
// Numbers are DESIGN section 6's starting values; the look is judged by walking it.
public sealed partial class FrontRoomsMapWorld
{
    // The chunk being built's collision builder (BuildInto sets it right after making it).
    MeshBuilder poolCollision;
    ThemeMaterials poolClubTheme, poolLevelTheme, poolDarkTheme;
    Material poolClubLens, poolLevelLens, poolDarkLens;
    static bool poolRefusedLogged;

    // ---------- Public entries (DESIGN 5) ----------

    /// <summary>The pool plan the cache holds, or null (every map but the structures scene's pool route).</summary>
    public FrontRoomsPoolPlan PoolPlan => Cache != null ? Cache.Pool : null;

    /// <summary>A Ready pool plan is in force.</summary>
    public bool HasPoolPlan => Cache != null && Cache.Pool != null && Cache.Pool.Ready;

    /// <summary>A cell of a pool theme (club, level, dark zone); false for every cell with no plan.</summary>
    public bool IsPoolCell(GridCoord cell) => Cache != null && Cache.IsPoolCell(cell);

    /// <summary>C2: a cell's water for the player (P2) and the Relay (P3): Dry at 0 for any cell with no plan or outside it.</summary>
    public PoolWaterInfo WaterAt(GridCoord cell)
    {
        var info = new PoolWaterInfo();
        if (Cache == null || Cache.Pool == null) return info;
        var c = Cache.Pool.CellOf(cell);
        var h = MapGrid.CellSize * .5f;
        info.water = c.water;
        info.floorY = c.FloorAt(h, h);
        info.waterY = c.HasWater ? c.waterY : 0f;
        info.beach = c.beach;
        info.pool = Cache.IsPoolCell(cell);
        return info;
    }

    /// <summary>The walking floor under a world point (world y): the plan's floor in a pool cell, the map's y 0 plane elsewhere.</summary>
    public float FloorYAt(Vector3 world)
    {
        var local = transform.InverseTransformPoint(world);
        var y = Cache != null && Cache.Pool != null ? Cache.Pool.FloorAt(local.x, local.z) : 0f;
        return transform.TransformPoint(new Vector3(local.x, y, local.z)).y;
    }

    /// <summary>
    /// Hold a Ready plan: the cache takes it, every built chunk it touches is built again, and path caches go stale.
    /// Refused (one log line, and the cache lets go of any plan) unless this map's profile has poolFamily on: only a map
    /// built on the structures scene's profile copy holds a plan.
    /// </summary>
    public bool SetPool(FrontRoomsPoolPlan plan)
    {
        if (Cache == null) return false;
        if (plan == null || !plan.Ready) { ClearPool(); return false; }
        if (!Profile.poolFamily)
        {
            if (!poolRefusedLogged) Debug.LogWarning("[FrontRoomsMap] POOL · the map's profile has poolFamily off: no pool plan is held.");
            poolRefusedLogged = true;
            ClearPool();
            return false;
        }
        Cache.SetPool(plan);
        RebuildTouched(plan.Touched);
        TouchPassageRevision();
        return true;
    }

    /// <summary>Let go of the pool plan: the chunks it touched build as the map's again.</summary>
    public void ClearPool()
    {
        if (Cache == null || Cache.Pool == null) return;
        var touched = new List<GridCoord>(Cache.Pool.Touched);
        Cache.ClearPool();
        RebuildTouched(touched);
        TouchPassageRevision();
    }

    void RebuildTouched(IEnumerable<GridCoord> coords)
    {
        var list = new List<GridCoord>();
        foreach (var c in coords) if (built.ContainsKey(c)) list.Add(c);
        foreach (var c in list) RebuildChunk(c);
    }

    // ---------- Hooks' helpers ----------

    // Theme (hook): the pool themes' materials. Walls tile (the club paint), floors tile, ceilings tile (the club the map's
    // Level 0 ceiling); the lens a map-owned copy of Level 0's in the pool hue. Re-made when BuildMaterials killed them.
    ThemeMaterials PoolTheme(ZoneTheme theme)
    {
        if (level0 == null) return office;
        if (theme == ZoneTheme.PoolClub)
        {
            if (poolClubTheme == null || poolClubLens == null)
            {
                poolClubLens = PoolLens("Map / pool club lens", new Color(.93f, .97f, 1f));
                poolClubTheme = new ThemeMaterials { wall = PoolPaint, floor = FrontRoomsPoolMaterials.Tile ?? level0.floor, ceiling = level0.ceiling, lens = poolClubLens, lampIntensity = 5f, lampColor = new Color(.93f, .97f, 1f) };
            }
            return poolClubTheme;
        }
        if (theme == ZoneTheme.PoolDark)
        {
            if (poolDarkTheme == null || poolDarkLens == null)
            {
                poolDarkLens = PoolLens("Map / pool dark lens", new Color(1f, .86f, .7f));
                poolDarkTheme = new ThemeMaterials { wall = FrontRoomsPoolMaterials.Tile ?? level0.wall, floor = FrontRoomsPoolMaterials.Tile ?? level0.floor, ceiling = FrontRoomsPoolMaterials.Tile ?? level0.ceiling, lens = poolDarkLens, lampIntensity = 2.4f, lampColor = new Color(1f, .86f, .7f) };
            }
            return poolDarkTheme;
        }
        if (poolLevelTheme == null || poolLevelLens == null)
        {
            poolLevelLens = PoolLens("Map / pool level lens", new Color(.86f, .96f, 1f));
            poolLevelTheme = new ThemeMaterials { wall = FrontRoomsPoolMaterials.Tile ?? level0.wall, floor = FrontRoomsPoolMaterials.Tile ?? level0.floor, ceiling = FrontRoomsPoolMaterials.Tile ?? level0.ceiling, lens = poolLevelLens, lampIntensity = 6f, lampColor = new Color(.86f, .96f, 1f) };
        }
        return poolLevelTheme;
    }

    // A map-owned copy of Level 0's lens with its emission in a pool hue at the same peak.
    Material PoolLens(string name, Color hue)
    {
        var source = level0 != null ? level0.lens : null;
        if (source == null) return null;
        var lens = Own(new Material(source) { name = name });
        if (lens.HasProperty("_EmissionColor"))
        {
            var e = source.GetColor("_EmissionColor");
            var peak = Mathf.Max(e.r, Mathf.Max(e.g, e.b));
            lens.SetColor("_EmissionColor", hue * peak);
        }
        return lens;
    }

    static Material PoolPaint => FrontRoomsPoolMaterials.Surface("Office_Wall_Fresh", new Color(.90f, .91f, .89f));
    static Material PoolBlock => FrontRoomsPoolMaterials.Lit("block", new Color(.66f, .67f, .66f), .15f);

    // A plan chunk (the cache's Apply gave it pool cells): no wall fixtures there.
    static bool PoolChunkHasCells(MapChunk data) => data != null && data.pool != null;

    // A room rect with any pool cell (Furnish leaves it alone).
    bool PoolRoomAny(MapChunk data, CellRect room)
    {
        for (var j = room.y; j < room.y + room.h; j++)
        for (var i = room.x; i < room.x + room.w; i++)
            if (Cache.IsPoolCell(data.Cell(i, j))) return true;
        return false;
    }

    // ---------- Quads ----------

    // A quad p0..p3 (clockwise seen from the front) into a builder with metre UVs as MeshBuilder.Box makes them (world
    // planar on the normal's main axis), turned round if its winding faces away from the normal asked.
    static void PoolQuad(MeshBuilder m, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal, Vector3 origin, float repeat)
    {
        if (m == null) return;
        if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), normal) < 0f) { var t = p1; p1 = p3; p3 = t; }
        var i = m.vertices.Count;
        PoolCorner(m, p0, normal, origin, repeat);
        PoolCorner(m, p1, normal, origin, repeat);
        PoolCorner(m, p2, normal, origin, repeat);
        PoolCorner(m, p3, normal, origin, repeat);
        m.triangles.Add(i); m.triangles.Add(i + 1); m.triangles.Add(i + 2);
        m.triangles.Add(i); m.triangles.Add(i + 2); m.triangles.Add(i + 3);
    }

    static void PoolCorner(MeshBuilder m, Vector3 p, Vector3 normal, Vector3 origin, float repeat)
    {
        m.vertices.Add(p);
        m.normals.Add(normal);
        var w = p + origin;
        var ax = Mathf.Abs(normal.x);
        var ay = Mathf.Abs(normal.y);
        var az = Mathf.Abs(normal.z);
        var uv = ax >= ay && ax >= az ? new Vector2(w.z, w.y) : ay >= az ? new Vector2(w.x, w.z) : new Vector2(w.x, w.y);
        m.uvs.Add(uv / (repeat > 0f ? repeat : 1f));
    }

    // ---------- The two sides of an edge ----------

    sealed class PoolSide
    {
        public bool exists, pool;
        public GridCoord cell;
        public ModuleCell data;
        public PoolRoom room;
        public ModulePool skin;
        public float low, high, top, ceiling, baseY, tileTopY;
        public Material wall, upper;
        public float upperRepeat = 1f;
        public int block;
        public float sign;
        public float reachStart, reachEnd;
        // Where the edge runs on this cell: an east edge of a (u 3), a west edge of b (u 0), a north edge of a (v 3), a south edge of b (v 0).
        public bool alongX, isA;

        public float FloorAt(float t)
        {
            if (!pool) return 0f;
            var cs = MapGrid.CellSize;
            float u, v;
            if (alongX) { u = t; v = isA ? cs : 0f; }
            else { v = t; u = isA ? cs : 0f; }
            return data.StepAt(u, v);
        }
    }

    PoolSide MakeSide(FrontRoomsPoolPlan plan, GridCoord cell, bool isA, bool alongX, Material normalWall, int block)
    {
        var s = new PoolSide { cell = cell, isA = isA, alongX = alongX, sign = isA ? -1f : 1f, block = block };
        s.pool = Cache.IsPoolCell(cell);
        if (!s.pool)
        {
            // A cell of the map's own (the approach, the ring): its paper from 0 to its own ceiling.
            s.exists = true;
            s.wall = normalWall;
            s.upper = normalWall;
            s.upperRepeat = WallpaperRepeat;
            s.low = -.02f;
            s.high = 0f;
            s.ceiling = MapGrid.CeilingHeight(Cache.ZoneOf(cell).height);
            s.top = s.ceiling;
            s.tileTopY = -99f;
            return s;
        }
        s.data = plan.CellOf(cell);
        if (s.data.connector == CellConnector.Void) return s;
        s.exists = true;
        s.room = plan.RoomAt(cell);
        s.skin = PoolRules.SkinOf(s.room != null ? s.room.module : null);
        var zone = Cache.ZoneOf(cell);
        s.baseY = s.skin.baseY;
        s.ceiling = s.baseY + MapGrid.CeilingHeight(zone.height);
        s.top = s.ceiling;
        var lo = float.MaxValue;
        var hi = float.MinValue;
        for (var k = 0; k <= 6; k++)
        {
            var f = s.FloorAt(k * .5f);
            lo = Mathf.Min(lo, f);
            hi = Mathf.Max(hi, f);
        }
        s.low = lo - .02f;
        s.high = hi;
        var tile = FrontRoomsPoolMaterials.Tile;
        var tileTop = s.skin.tileTop;
        s.tileTopY = tileTop >= ModulePool.TileToCeiling - 1f ? 99f : s.baseY + tileTop;
        s.wall = tile != null ? tile : normalWall;
        s.upper = s.skin.upper == PoolUpper.Paint ? PoolPaint : s.skin.upper == PoolUpper.Block ? PoolBlock : s.wall;
        s.upperRepeat = s.skin.upper == PoolUpper.Paint ? WallpaperRepeat : 1f;
        if (s.upper == null) s.upper = s.wall;
        return s;
    }

    // ---------- BuildPoolEdge (the BuildEdge hook) ----------

    /// <summary>
    /// The pool pass's part of one edge (BuildEdge's first statement, with a pool plan). True when it built the edge; false
    /// leaves it to BuildEdge: X and the start area, edges with no pool cell, doors and windows (after their skirts), and a
    /// Void cell beside a cell of the map's own (the map's plain wall).
    /// </summary>
    bool BuildPoolEdge(BuiltChunk chunk, EdgeKind kind, GridCoord a, GridCoord b, Vector3 start, Vector3 along, float height,
        Material wallA, Material wallB, int blockIndex, int sideA, int sideB, BuilderFn get, SolidFn solid, Vector3 origin,
        bool mayExtendStart, bool mayExtendEnd)
    {
        var plan = Cache.Pool;
        if (plan == null || chunk == null) return false;
        if (InExitArea(a) || InExitArea(b) || InStartArea(a) || InStartArea(b)) return false;
        var pa = Cache.IsPoolCell(a);
        var pb = Cache.IsPoolCell(b);
        if (!pa && !pb) return false;
        var alongX = along.x > .5f;
        var A = MakeSide(plan, a, true, alongX, wallA, sideA);
        var B = MakeSide(plan, b, false, alongX, wallB, sideB);
        if (!A.exists && !B.exists) return true;
        if ((!A.exists && !B.pool) || (!B.exists && !A.pool)) return false;
        var length = MapGrid.CellSize;
        var across = new Vector3(along.z, 0f, along.x);

        // Corner reach exactly as BuildEdge's.
        var half = WallThickness * .5f;
        A.reachStart = A.reachEnd = B.reachStart = B.reachEnd = half;
        if (FrontRoomsTransitionKit.B0)
        {
            CornerReach(alongX ? new GridCoord(a.x, a.y + 1) : new GridCoord(a.x + 1, a.y), alongX ? FrontRoomsTransitionKit.East : FrontRoomsTransitionKit.North, out var sa, out var sb);
            CornerReach(new GridCoord(a.x + 1, a.y + 1), alongX ? FrontRoomsTransitionKit.West : FrontRoomsTransitionKit.South, out var ea, out var eb);
            A.reachStart *= sa; B.reachStart *= sb; A.reachEnd *= ea; B.reachEnd *= eb;
        }

        Cache.TryPoolRecord(a, b, out var record);

        void Skin(PoolSide side, Material material, float f, float t, float y0, float y1, float repeat)
        {
            if (t - f < .02f || y1 - y0 < .005f || material == null) return;
            var center = start + along * ((f + t) * .5f) + Vector3.up * ((y0 + y1) * .5f) + across * (WallThickness * .25f * side.sign);
            solid(side.block, material, center, Abs(along * (t - f) + across * (WallThickness * .5f) + Vector3.up * (y1 - y0)), repeat);
        }

        void SidePiece(PoolSide side, float from, float to, float bottom, float top, bool extendStart, bool extendEnd)
        {
            if (!side.exists) return;
            if (from > 0f || !mayExtendStart) extendStart = false;
            if (to < length || !mayExtendEnd) extendEnd = false;
            var lo = Mathf.Max(bottom, side.low);
            var hi = Mathf.Min(top, side.top);
            if (hi - lo < .02f) return;
            var f = from - (extendStart ? side.reachStart : 0f);
            var t = to + (extendEnd ? side.reachEnd : 0f);
            if (!side.pool) { Skin(side, side.wall, f, t, lo, hi, side.upperRepeat); return; }
            var split = Mathf.Clamp(side.tileTopY, lo, hi);
            Skin(side, side.wall, f, t, lo, split, 1f);
            Skin(side, side.upper, f, t, split, hi, side.upperRepeat);
            // The aqua course, render only, 4 mm proud of the skin.
            var skin = side.skin;
            if (skin != null && skin.bandHigh > skin.bandLow)
            {
                var floor = side.FloorAt((from + to) * .5f);
                var b0 = (skin.bandOnFloor ? floor : side.baseY) + skin.bandLow;
                var b1 = (skin.bandOnFloor ? floor : side.baseY) + skin.bandHigh;
                b0 = Mathf.Max(b0, lo);
                b1 = Mathf.Min(b1, hi);
                var band = FrontRoomsPoolMaterials.Band;
                if (b1 - b0 > .005f && band != null)
                {
                    var c = start + along * ((f + t) * .5f) + Vector3.up * ((b0 + b1) * .5f) + across * (side.sign * (WallThickness * .5f + .002f));
                    get(side.block, band).Box(c, Abs(along * (t - f) + across * .004f + Vector3.up * (b1 - b0)), origin, 1f);
                }
            }
        }

        void Piece(float from, float to, float bottom, float top, bool extendStart, bool extendEnd)
        {
            SidePiece(A, from, to, bottom, top, extendStart, extendEnd);
            SidePiece(B, from, to, bottom, top, extendStart, extendEnd);
        }

        var minLow = Mathf.Min(A.exists ? A.low : 0f, B.exists ? B.low : 0f);
        var maxTop = Mathf.Max(A.exists ? A.top : 0f, B.exists ? B.top : 0f);

        if (kind == EdgeKind.Door || kind == EdgeKind.Window)
        {
            // Skirts under the map's door or window where a side's floor lies under 0; the map builds the rest at y 0.
            if (A.exists && A.pool && A.low < -.01f) SidePiece(A, 0f, length, A.low, 0f, true, true);
            if (B.exists && B.pool && B.low < -.01f) SidePiece(B, 0f, length, B.low, 0f, true, true);
            return false;
        }

        var tileMat = FrontRoomsPoolMaterials.Tile;
        if (kind == EdgeKind.Open)
        {
            if (A.exists && B.exists)
            {
                PoolRisers(A, B, 0f, length, start, along, across, get, origin);
                PoolWeirAndCoping(A, B, start, along, across, get, origin);
            }
            return true;
        }

        if (kind == EdgeKind.Wall)
        {
            if (record.style == PoolEdgeStyle.ClearGlass)
            {
                // Jambs 0.2, a 0.2 curb, the head from 2.7 up; a 3 cm pane of the map's glass between (it blocks, as glass does).
                var baseMax = Mathf.Max(A.exists ? A.baseY : 0f, B.exists ? B.baseY : 0f);
                var curb = Mathf.Max(A.exists ? A.high : 0f, B.exists ? B.high : 0f) + .2f;
                var head = baseMax + 2.7f;
                Piece(0f, .2f, minLow, maxTop, true, false);
                Piece(length - .2f, length, minLow, maxTop, false, true);
                Piece(.2f, length - .2f, minLow, curb, false, false);
                Piece(.2f, length - .2f, head, maxTop, false, false);
                if (head > curb + .1f && glass != null)
                    solid(blockIndex, glass, start + along * (length * .5f) + Vector3.up * ((curb + head) * .5f), Abs(along * (length - .4f) + across * ModuleUnits.GlassThickness + Vector3.up * (head - curb)), 1f);
                return true;
            }
            Piece(0f, length, minLow, maxTop, true, true);
            if (record.style == PoolEdgeStyle.ObsWindow) PoolObsWindow(chunk, plan, A, B, record, start, along, across, get, origin);
            return true;
        }

        // Arch: rect, round-headed (with trims) or the gate (a rect opening; the bars are the stand-in's).
        ArchOpening(a, alongX, out var width, out var c);
        var ceilMin = Mathf.Min(A.exists ? A.ceiling : maxTop, B.exists ? B.ceiling : maxTop);
        var baseMin = Mathf.Min(A.exists ? A.baseY : 0f, B.exists ? B.baseY : 0f);
        var floorMax = Mathf.Max(A.exists ? A.high : 0f, B.exists ? B.high : 0f);
        var l = c - width * .5f;
        var r = c + width * .5f;
        Piece(0f, l, minLow, maxTop, true, false);
        Piece(r, length, minLow, maxTop, false, true);
        if (A.exists && B.exists) PoolRisers(A, B, l, r, start, along, across, get, origin);
        if (record.style == PoolEdgeStyle.RoundArch)
        {
            var radius = width * .5f;
            var crown = Mathf.Min(record.crown > 0f || baseMin < -.01f ? record.crown : baseMin + 2.3f, ceilMin - PoolRules.CrownClearance);
            crown = Mathf.Max(crown, floorMax + radius + 1.2f);
            if (crown > ceilMin - .05f) crown = ceilMin - .05f;
            PoolRoundArch(A, B, c, radius, crown, maxTop, start, along, across, get, solid, origin, tileMat);
            if ((record.trim & 3) != 0) PoolArchTrim(A, B, c, radius, crown, record.trim, start, along, across, get, origin);
            return true;
        }
        var openingTop = baseMin + Mathf.Min(ModuleUnits.ArchTop, ceilMin - baseMin - ModuleUnits.ArchHeaderMin);
        if (openingTop < floorMax + 2.0f) openingTop = Mathf.Min(floorMax + 2.0f, ceilMin - .05f);
        Piece(l, r, openingTop, maxTop, false, false);
        return true;
    }

    // Risers where the two floors differ along [from, to] of the edge: faces toward the lower side on the edge line, with
    // UVs in metres (joints run on from the floor slabs), and collision boxes reaching into the higher side.
    void PoolRisers(PoolSide A, PoolSide B, float from, float to, Vector3 start, Vector3 along, Vector3 across, BuilderFn get, Vector3 origin)
    {
        var tile = FrontRoomsPoolMaterials.Tile;
        if (tile == null) return;
        var ts = new List<float>();
        for (var t = from; t < to - 1e-4f; t += .1f) ts.Add(t);
        AddStairBreaks(A, from, to, ts);
        AddStairBreaks(B, from, to, ts);
        ts.Add(to);
        ts.Sort();
        float runStart = 0f, runLo = 0f, runHi = 0f, runSign = 0f;
        var open = false;
        void Flush(float end)
        {
            if (!open) return;
            open = false;
            if (end - runStart < .005f || runHi - runLo < .004f) return;
            // The face looks toward the lower side; its block is that side's.
            var lowSide = runSign > 0f ? B : A;
            var normal = across * runSign;
            var p0 = start + along * runStart + Vector3.up * runLo;
            var p1 = start + along * runStart + Vector3.up * runHi;
            var p2 = start + along * end + Vector3.up * runHi;
            var p3 = start + along * end + Vector3.up * runLo;
            PoolQuad(get(lowSide.block, tile), p0, p1, p2, p3, normal, origin, 1f);
            if (poolCollision != null)
            {
                var centre = start + along * ((runStart + end) * .5f) + Vector3.up * ((runLo + runHi) * .5f) - across * (runSign * WallThickness * .5f);
                poolCollision.Box(centre, Abs(along * (end - runStart) + across * WallThickness + Vector3.up * (runHi - runLo)), origin, 1f);
            }
        }
        for (var k = 0; k + 1 < ts.Count; k++)
        {
            float t0 = ts[k], t1 = ts[k + 1];
            if (t1 - t0 < 1e-4f) continue;
            var tm = (t0 + t1) * .5f;
            var fa = A.FloorAt(tm);
            var fb = B.FloorAt(tm);
            var lo = Mathf.Min(fa, fb);
            var hi = Mathf.Max(fa, fb);
            var sign = Mathf.Abs(fa - fb) < .004f ? 0f : fa > fb ? 1f : -1f;
            if (open && (sign != runSign || Mathf.Abs(lo - runLo) > 1e-4f || Mathf.Abs(hi - runHi) > 1e-4f)) Flush(t0);
            if (sign == 0f) continue;
            if (!open) { open = true; runStart = t0; runLo = lo; runHi = hi; runSign = sign; }
        }
        Flush(to);
    }

    // A stair cell whose flight runs along the edge adds its tread boundaries, so the cheek steps exactly with the treads.
    static void AddStairBreaks(PoolSide side, float from, float to, List<float> ts)
    {
        if (!side.exists || !side.pool) return;
        var c = side.data;
        if (c.connector != CellConnector.Stair || c.stairRisers == 0 || c.stairRun <= 1e-4f) return;
        var dir = c.stairDir & 3;
        // Along X the edge's t is u (dir 1 or 3 run along it); along Z it is v (dir 0 or 2).
        var runsAlong = side.alongX ? (dir == 1 || dir == 3) : (dir == 0 || dir == 2);
        if (!runsAlong) return;
        var cs = MapGrid.CellSize;
        for (var k = 0; k <= c.stairRisers; k++)
        {
            var s = c.stairFrom + k * c.stairRun;
            var t = dir == 0 || dir == 1 ? s : cs - s;
            if (t > from + 1e-4f && t < to - 1e-4f) ts.Add(t);
        }
    }

    // A weir where two waters meet at different lines (a white lip on the high side and a falling sheet), or a coping and
    // gutter where a dry deck meets a basin. Render only.
    void PoolWeirAndCoping(PoolSide A, PoolSide B, Vector3 start, Vector3 along, Vector3 across, BuilderFn get, Vector3 origin)
    {
        if (!A.pool || !B.pool) return;
        var length = MapGrid.CellSize;
        var ca = A.data;
        var cb = B.data;
        var tile = FrontRoomsPoolMaterials.Tile;
        if (ca.HasWater && cb.HasWater && Mathf.Abs(ca.waterY - cb.waterY) > .01f)
        {
            var highIsA = ca.waterY > cb.waterY;
            var high = highIsA ? A : B;
            var lowSide = highIsA ? B : A;
            var hw = Mathf.Max(ca.waterY, cb.waterY);
            var lw = Mathf.Min(ca.waterY, cb.waterY);
            var into = across * (highIsA ? 1f : -1f);
            // The lip: 0.25 deep on the high side, its top a little under the high water.
            var lipTop = hw - .01f;
            var lipBottom = high.high - .01f;
            if (lipTop - lipBottom > .01f && tile != null)
                get(high.block, tile).Box(start + along * (length * .5f) + Vector3.up * ((lipTop + lipBottom) * .5f) - into * .125f, Abs(along * length + across * .25f + Vector3.up * (lipTop - lipBottom)), origin, 1f);
            // The sheet: 2 cm into the low side, from the high water to the low.
            var water = FrontRoomsPoolMaterials.WaterOf(PoolWaterKind(lowSide));
            if (water != null)
            {
                var p0 = start + into * .02f + Vector3.up * lw;
                var p1 = start + into * .02f + Vector3.up * hw;
                var p2 = start + along * length + into * .02f + Vector3.up * hw;
                var p3 = start + along * length + into * .02f + Vector3.up * lw;
                PoolQuad(get(lowSide.block, water), p0, p1, p2, p3, into, origin, 1f);
            }
            return;
        }
        var aDry = !ca.HasWater;
        var bDry = !cb.HasWater;
        if (aDry == bDry) return;
        var basin = aDry ? cb : ca;
        if (basin.water != CellWater.Wade) return;
        var dry = aDry ? A : B;
        var away = across * (aDry ? -1f : 1f);
        var y = dry.high;
        if (tile != null)
            get(dry.block, tile).Box(start + along * (length * .5f) + Vector3.up * (y + .002f) + away * .15f, Abs(along * length + across * .30f + Vector3.up * .004f), origin, 1f);
        var gutter = FrontRoomsPoolMaterials.Lit("gutter", new Color(.10f, .12f, .13f), .2f);
        if (gutter != null)
            get(dry.block, gutter).Box(start + along * (length * .5f) + Vector3.up * (y + .003f) + away * .34f, Abs(along * length + across * .08f + Vector3.up * .002f), origin, 1f);
    }

    static FrontRoomsPoolMaterials.Water PoolWaterKind(PoolSide side)
    {
        if (side == null || side.room == null || side.room.module == null) return FrontRoomsPoolMaterials.Water.Level;
        var theme = side.room.module.theme;
        return theme == ZoneTheme.PoolClub ? FrontRoomsPoolMaterials.Water.Club : theme == ZoneTheme.PoolDark ? FrontRoomsPoolMaterials.Water.Dark : FrontRoomsPoolMaterials.Water.Level;
    }

    // A round-headed arch above the rect opening: the wall from the crown up, and on each existing face the spandrels
    // between the circle and the crown in 24 strips, with the intrados across the wall; collision by strip.
    void PoolRoundArch(PoolSide A, PoolSide B, float c, float radius, float crown, float top, Vector3 start, Vector3 along, Vector3 across,
        BuilderFn get, SolidFn solid, Vector3 origin, Material tile)
    {
        var spring = crown - radius;
        // Over the opening, from the crown to the top, as a plain piece per side.
        foreach (var side in new[] { A, B })
        {
            if (!side.exists) continue;
            var hi = side.top;
            if (hi - crown < .02f) continue;
            var material = side.pool ? (crown >= side.tileTopY ? side.upper : side.wall) : side.wall;
            var center = start + along * c + Vector3.up * ((crown + hi) * .5f) + across * (WallThickness * .25f * side.sign);
            solid(side.block, material, center, Abs(along * (radius * 2f) + across * (WallThickness * .5f) + Vector3.up * (hi - crown)), side.pool ? 1f : side.upperRepeat);
        }
        const int segments = 24;
        for (var k = 0; k < segments; k++)
        {
            var u0 = -radius + 2f * radius * k / segments;
            var u1 = -radius + 2f * radius * (k + 1) / segments;
            var y0 = spring + Mathf.Sqrt(Mathf.Max(0f, radius * radius - u0 * u0));
            var y1 = spring + Mathf.Sqrt(Mathf.Max(0f, radius * radius - u1 * u1));
            foreach (var side in new[] { A, B })
            {
                if (!side.exists) continue;
                var material = side.pool ? (y0 >= side.tileTopY ? side.upper : side.wall) : side.wall;
                if (material == null) continue;
                var off = across * (WallThickness * .5f * side.sign);
                var p0 = start + along * (c + u0) + Vector3.up * y0 + off;
                var p1 = start + along * (c + u0) + Vector3.up * crown + off;
                var p2 = start + along * (c + u1) + Vector3.up * crown + off;
                var p3 = start + along * (c + u1) + Vector3.up * y1 + off;
                PoolQuad(get(side.block, material), p0, p1, p2, p3, across * side.sign, origin, side.pool ? 1f : side.upperRepeat);
            }
            // The intrados across the wall (only the halves that exist), facing the circle's centre.
            var fromA = A.exists ? -WallThickness * .5f : 0f;
            var toB = B.exists ? WallThickness * .5f : 0f;
            var um = (u0 + u1) * .5f;
            var ym = (y0 + y1) * .5f;
            var normal = (-along * um - Vector3.up * (ym - spring)).normalized;
            var under = (A.exists ? A : B);
            var underMat = under.pool ? (ym >= under.tileTopY ? under.upper : under.wall) : under.wall;
            if (underMat != null && toB - fromA > .01f)
            {
                var q0 = start + along * (c + u0) + Vector3.up * y0 + across * fromA;
                var q1 = start + along * (c + u0) + Vector3.up * y0 + across * toB;
                var q2 = start + along * (c + u1) + Vector3.up * y1 + across * toB;
                var q3 = start + along * (c + u1) + Vector3.up * y1 + across * fromA;
                PoolQuad(get(under.block, underMat), q0, q1, q2, q3, normal, origin, under.pool ? 1f : under.upperRepeat);
            }
            if (poolCollision != null)
            {
                var yLow = Mathf.Min(y0, y1);
                if (crown - yLow > .01f)
                    poolCollision.Box(start + along * (c + um) + Vector3.up * ((yLow + crown) * .5f) + across * ((fromA + toB) * .5f),
                        Abs(along * (u1 - u0) + across * Mathf.Max(.02f, toB - fromA) + Vector3.up * (crown - yLow)), origin, 1f);
            }
        }
    }

    // M5's trims, render only: a keystone (rose) at the crown and pilasters (teal) with capitals beside the opening, both faces.
    void PoolArchTrim(PoolSide A, PoolSide B, float c, float radius, float crown, byte trim, Vector3 start, Vector3 along, Vector3 across, BuilderFn get, Vector3 origin)
    {
        var rose = FrontRoomsPoolMaterials.Lit("rose", new Color(.74f, .48f, .54f), .45f);
        var teal = FrontRoomsPoolMaterials.Lit("teal", new Color(.16f, .44f, .46f), .45f);
        foreach (var side in new[] { A, B })
        {
            if (!side.exists) continue;
            var face = across * (side.sign * (WallThickness * .5f));
            if ((trim & 1) != 0 && rose != null)
                get(side.block, rose).Box(start + along * c + Vector3.up * (crown - .05f) + face + across * (side.sign * .04f), Abs(along * .5f + across * .08f + Vector3.up * .58f), origin, 1f);
            if ((trim & 2) != 0 && teal != null)
            {
                var floor = side.high;
                foreach (var x in new[] { c - radius - .3f, c + radius + .3f })
                {
                    get(side.block, teal).Box(start + along * x + Vector3.up * (floor + .9f) + face + across * (side.sign * .05f), Abs(along * .5f + across * .1f + Vector3.up * 1.8f), origin, 1f);
                    get(side.block, teal).Box(start + along * x + Vector3.up * (floor + 1.9f) + face + across * (side.sign * .07f), Abs(along * .64f + across * .14f + Vector3.up * .2f), origin, 1f);
                }
            }
        }
    }

    // M4's observation window, render only on the face of the room that authors it: square (even index) frame 1.0 x 0.7 at
    // 1.15-1.85 with a glow panel, round (odd) frame r 0.38 with a glow disc at 1.5; each panel a Windows lamp.
    void PoolObsWindow(BuiltChunk chunk, FrontRoomsPoolPlan plan, PoolSide A, PoolSide B, ModuleEdgeRecord record, Vector3 start, Vector3 along, Vector3 across, BuilderFn get, Vector3 origin)
    {
        var side = A.exists && A.pool && A.room != null ? A : B.exists && B.pool && B.room != null ? B : null;
        if (side == null) return;
        var face = across * (side.sign * (WallThickness * .5f));
        var outward = across * side.sign;
        var floor = side.high;
        var centre = start + along * (MapGrid.CellSize * .5f) + face;
        var steel = FrontRoomsPoolMaterials.Steel;
        var glow = FrontRoomsPoolMaterials.Glow("window", new Color(.30f, 1.35f, 1.45f));
        var round = (record.index & 1) == 1;
        var cy = floor + 1.5f;
        if (!round)
        {
            if (steel != null)
            {
                get(side.block, steel).Box(centre + Vector3.up * (floor + 1.15f + .04f) + outward * .015f, Abs(along * 1.0f + across * .03f + Vector3.up * .08f), origin, 1f);
                get(side.block, steel).Box(centre + Vector3.up * (floor + 1.85f - .04f) + outward * .015f, Abs(along * 1.0f + across * .03f + Vector3.up * .08f), origin, 1f);
                get(side.block, steel).Box(centre - along * .46f + Vector3.up * cy + outward * .015f, Abs(along * .08f + across * .03f + Vector3.up * .7f), origin, 1f);
                get(side.block, steel).Box(centre + along * .46f + Vector3.up * cy + outward * .015f, Abs(along * .08f + across * .03f + Vector3.up * .7f), origin, 1f);
            }
        }
        else if (steel != null)
        {
            for (var k = 0; k < 12; k++)
            {
                var ang = k * Mathf.PI * 2f / 12f;
                var p = centre + along * (Mathf.Cos(ang) * .34f) + Vector3.up * (cy + Mathf.Sin(ang) * .34f) + outward * .015f;
                get(side.block, steel).Box(p, Abs(along * .19f + across * .03f + Vector3.up * .19f), origin, 1f);
            }
        }
        // The glow panel: its own renderer, the window lamp's lens.
        var go = new GameObject("STAND-IN obs window " + side.cell);
        go.transform.SetParent(chunk.root.transform, false);
        go.transform.localPosition = centre + Vector3.up * cy + outward * .02f;
        go.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up);
        var mesh = round ? PoolDiscMesh(.30f, 20) : PoolRectMesh(.84f, .56f);
        chunk.meshes.Add(mesh);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var panel = go.AddComponent<MeshRenderer>();
        panel.sharedMaterial = glow;
        panel.shadowCastingMode = ShadowCastingMode.Off;
        panel.receiveShadows = false;
        PoolFixture(chunk, side.cell, go.transform.localPosition + outward * .1f, Quaternion.LookRotation(outward, Vector3.up), LightType.Spot, 130f, 70f, 1.8f, 4.5f,
            new Color(.35f, .85f, .9f), ModuleLamp.Steady, panel, new Color(.30f, 1.35f, 1.45f), 1, 7);
    }

    // A flat disc facing +Z (local), radius r, n segments; and a rect w x h facing +Z.
    static Mesh PoolDiscMesh(float r, int n)
    {
        var v = new List<Vector3> { Vector3.zero };
        var nrm = new List<Vector3> { Vector3.forward };
        var uv = new List<Vector2> { new Vector2(.5f, .5f) };
        var t = new List<int>();
        for (var k = 0; k <= n; k++)
        {
            var a = k * Mathf.PI * 2f / n;
            v.Add(new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f));
            nrm.Add(Vector3.forward);
            uv.Add(new Vector2(.5f + Mathf.Cos(a) * .5f, .5f + Mathf.Sin(a) * .5f));
            // Clockwise seen from +Z (the front): centre, ring k-1, ring k.
            if (k > 0) { t.Add(0); t.Add(k); t.Add(k + 1); }
        }
        var mesh = new Mesh { name = "Pool disc" };
        mesh.SetVertices(v);
        mesh.SetNormals(nrm);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(t, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh PoolRectMesh(float w, float h)
    {
        var mesh = new Mesh { name = "Pool panel" };
        mesh.SetVertices(new List<Vector3> { new Vector3(-w * .5f, -h * .5f, 0f), new Vector3(-w * .5f, h * .5f, 0f), new Vector3(w * .5f, h * .5f, 0f), new Vector3(w * .5f, -h * .5f, 0f) });
        mesh.SetNormals(new List<Vector3> { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward });
        mesh.SetUVs(0, new List<Vector2> { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) });
        // Seen from +Z (the panel's front, LookRotation's forward): clockwise from the viewer.
        mesh.SetTriangles(new List<int> { 0, 2, 1, 0, 3, 2 }, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    // ---------- Lamps ----------

    // One of the pool family's lamps as a map fixture (BuildFixture's set-up), so the light budget, the temperaments,
    // overrides, holds and Release treat it as any lamp. The panel is a renderer the lamp's level drives.
    Fixture PoolFixture(BuiltChunk chunk, GridCoord cell, Vector3 localPosition, Quaternion localRotation, LightType type, float spot, float inner,
        float intensity, float range, Color color, ModuleLamp lamp, Renderer panel, Color emission, int tier, int salt)
    {
        if (lamp == ModuleLamp.Off || chunk == null || panel == null) return null;
        var seed = Cache.Generator.Seed;
        var f = new Fixture { rng = MapHash.Hash(seed, cell.x * 8 + salt, cell.y, 211) | 1u, cell = cell };
        var go = new GameObject("Light " + cell);
        go.transform.SetParent(chunk.root.transform, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        var light = go.AddComponent<Light>();
        ShippedShadowFilter(light);
        light.type = type;
        if (type == LightType.Spot)
        {
            light.spotAngle = spot;
            light.innerSpotAngle = inner;
        }
        light.color = color;
        light.range = range;
        light.shadows = LightShadows.None;
        light.shadowStrength = .92f;
        light.shadowNearPlane = .1f;
        f.light = light;
        f.panel = panel;
        f.emission = emission;
        f.baseIntensity = intensity;
        f.offset = light.transform.position - chunk.root.transform.position;
        f.startGroup = StartLampGroup(cell, range);
        f.exitGroup = ExitLampGroup(cell, range);
        f.castsShadow = salt == 0 && MapHash.Unit(MapHash.Hash(seed, cell.x, cell.y, 223)) < .34f;
        var roll = Rand(ref f.rng);
        f.mode = (tierRules ?? new FrontRoomsTierRules()).At(tier).LampMode(roll);
        if (lamp != ModuleLamp.Auto) f.mode = (int)lamp - 1;
        if (lampModes.TryGetValue(cell, out var set)) f.mode = (int)set - 1;
        f.ambientHold = ambientHolds.Contains(cell);
        f.hold = lampHolds.TryGetValue(cell, out var lookHold) ? lookHold : null;
        f.phase = Rand(ref f.rng) * 50f;
        f.nextEvent = 2f + Rand(ref f.rng) * 14f;
        chunk.fixtures.Add(f);
        if (!fixtureByCell.ContainsKey(cell)) fixtureByCell[cell] = f;
        return f;
    }

    // A lens or glow box (its own renderer, shadows off) for a pool lamp.
    MeshRenderer PoolLensBox(BuiltChunk chunk, string name, Vector3 localPosition, Quaternion localRotation, Vector3 size, Material material)
    {
        if (material == null) return null;
        var go = new GameObject(name);
        go.transform.SetParent(chunk.root.transform, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        go.AddComponent<MeshFilter>().sharedMesh = FrontRoomsFilmMesh.GetPlanarBox(size);
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        return r;
    }

    static Color LensEmission(Material m) => m != null && m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.white;

    // The lamps of one cell by its room's kit (Troffer through the map's own BuildFixture; Flush and Strip per cell).
    void PoolCellLamps(BuiltChunk chunk, MapChunk data, GridCoord cell, int index, Vector3 cellCenter, float baseY, float ceilingY, PoolRoom room, ModulePool skin, ThemeMaterials theme)
    {
        var lamp = data.lamp[index];
        if (lamp == ModuleLamp.Off || skin == null) return;
        var cs = MapGrid.CellSize;
        var lens = theme != null ? theme.lens : null;
        switch (skin.lampKit)
        {
            case PoolLampKit.Troffer:
                BuildFixture(chunk, cell, new Vector3(cellCenter.x, baseY, cellCenter.z), ceilingY - baseY, theme, lamp, data.tier);
                break;
            case PoolLampKit.Flush:
            {
                var h = MapHash.Hash(Cache.Generator.Seed, cell.x, cell.y, 241);
                var off = new Vector3(((h & 255) / 255f - .5f) * .6f, 0f, (((h >> 8) & 255) / 255f - .5f) * .6f);
                // On the 0.6 m ceiling grid.
                off.x = Mathf.Round(off.x / .6f) * .6f;
                off.z = Mathf.Round(off.z / .6f) * .6f;
                var pos = new Vector3(cellCenter.x + off.x, ceilingY - ModuleUnits.TrofferLens * .5f - .004f, cellCenter.z + off.z);
                var panel = PoolLensBox(chunk, "Lens " + cell, pos, Quaternion.identity, new Vector3(.6f, ModuleUnits.TrofferLens, .6f), lens);
                PoolFixture(chunk, cell, pos + Vector3.down * ModuleUnits.LampDrop, Quaternion.Euler(90f, 0f, 0f), LightType.Spot, 140f, 80f, 6.1f, 7.5f,
                    new Color(.86f, .96f, 1f), lamp, panel, LensEmission(lens), data.tier, 0);
                break;
            }
            case PoolLampKit.Strip:
            {
                // Two lenses per cell over the aisles (the room's long axis).
                var alongZ = room == null || room.depth >= room.width;
                for (var k = 0; k < 2; k++)
                {
                    var o = (k == 0 ? -.75f : .75f);
                    var pos = new Vector3(cellCenter.x + (alongZ ? o : 0f), ceilingY - .02f, cellCenter.z + (alongZ ? 0f : o));
                    var size = alongZ ? new Vector3(.16f, .03f, 1.22f) : new Vector3(1.22f, .03f, .16f);
                    var panel = PoolLensBox(chunk, "Lens " + cell + " " + k, pos, Quaternion.identity, size, lens);
                    PoolFixture(chunk, cell, pos + Vector3.down * ModuleUnits.LampDrop, Quaternion.Euler(90f, 0f, 0f), LightType.Spot, 150f, 100f, 2.0f, 5f,
                        new Color(.93f, .97f, 1f), lamp, panel, LensEmission(lens), data.tier, k);
                }
                break;
            }
        }
        _ = cs;
    }

    // The lamps of one room by its kit (Valance, Rig with underwater niches, StairLights, Niche), and PC_Lockers' globes.
    void PoolRoomLamps(BuiltChunk chunk, MapChunk data, PoolRoom room, Vector3 roomOrigin, float baseY, float ceilingY)
    {
        var skin = PoolRules.SkinOf(room.module);
        var cs = MapGrid.CellSize;
        float w = room.width * cs, d = room.depth * cs;
        var white = FrontRoomsPoolMaterials.Lit("valance", new Color(.93f, .94f, .93f), .35f);
        var tier = data.tier;
        GridCoord CellAtLocal(float x, float z) => new GridCoord(room.origin.x + Mathf.Clamp((int)(x / cs), 0, room.width - 1), room.origin.y + Mathf.Clamp((int)(z / cs), 0, room.depth - 1));
        ModuleLamp LampAtLocal(float x, float z)
        {
            var c = CellAtLocal(x, z);
            var o = data.Origin;
            int li = c.x - o.x, lj = c.y - o.y;
            return li >= 0 && lj >= 0 && li < MapGrid.ChunkCells && lj < MapGrid.ChunkCells ? data.lamp[MapGrid.LocalIndex(li, lj)] : ModuleLamp.Auto;
        }
        switch (skin.lampKit)
        {
            case PoolLampKit.Valance:
            {
                // Along the two longest walls, one per 6 m: a white box 0.66-0.78 under the ceiling, its top glowing; an up spot and a bounce.
                var alongX = w >= d;
                var run = alongX ? w : d;
                var glow = FrontRoomsPoolMaterials.Glow("valance", new Color(1.1f, 1.18f, 1.2f));
                for (var wall = 0; wall < 2; wall++)
                for (var p = 3f; p < run; p += 6f)
                {
                    var inset = .3f;
                    var x = alongX ? p : (wall == 0 ? inset : w - inset);
                    var z = alongX ? (wall == 0 ? inset : d - inset) : p;
                    var lamp = LampAtLocal(x, z);
                    if (lamp == ModuleLamp.Off) continue;
                    var local = roomOrigin + new Vector3(x, ceilingY - .72f, z);
                    var size = alongX ? new Vector3(2.4f, .12f, .12f) : new Vector3(.12f, .12f, 2.4f);
                    var box = PoolLensBox(chunk, "STAND-IN valance " + room.label, local, Quaternion.identity, size, white);
                    var strip = PoolLensBox(chunk, "Valance glow " + room.label, local + Vector3.up * .065f, Quaternion.identity, new Vector3(size.x * .96f, .01f, size.z * .96f), glow);
                    var cell = CellAtLocal(x, z);
                    PoolFixture(chunk, cell, local + Vector3.up * .1f, Quaternion.Euler(-90f, 0f, 0f), LightType.Spot, 150f, 100f, 3.06f, 3.6f, new Color(.86f, .96f, 1f), lamp, strip ?? box, new Color(1.1f, 1.18f, 1.2f), tier, 1 + wall);
                    if (box != null)
                        PoolFixture(chunk, cell, local + Vector3.down * .1f, Quaternion.Euler(90f, 0f, 0f), LightType.Spot, 170f, 130f, 1.63f, 4.2f, new Color(.86f, .96f, 1f), lamp, box, Color.black, tier, 3 + wall);
                }
                break;
            }
            case PoolLampKit.Rig:
            {
                // Metal halide brackets on the west and east walls at 3.4 m, three a side (up spot and a bounce), and four
                // underwater niches on each long basin wall at -0.6.
                var steel = FrontRoomsPoolMaterials.Steel;
                var halide = FrontRoomsPoolMaterials.Glow("halide", new Color(1.6f, 1.7f, 1.75f));
                for (var side = 0; side < 2; side++)
                for (var k = 0; k < 3; k++)
                {
                    var x = side == 0 ? .35f : w - .35f;
                    var z = d * (k + 1) / 4f;
                    var local = roomOrigin + new Vector3(x, baseY + 3.4f, z);
                    var lamp = LampAtLocal(x, z);
                    PoolLensBox(chunk, "STAND-IN halide bracket " + room.label, local, Quaternion.identity, new Vector3(.4f, .18f, .5f), steel);
                    var head = PoolLensBox(chunk, "Halide " + room.label, local + Vector3.up * .1f, Quaternion.identity, new Vector3(.32f, .02f, .42f), halide);
                    var cell = CellAtLocal(x, z);
                    var mode = lamp == ModuleLamp.Off ? ModuleLamp.Steady : lamp;
                    PoolFixture(chunk, cell, local + Vector3.up * .2f, Quaternion.Euler(-90f, 0f, 0f), LightType.Spot, 150f, 100f, 6f, 9f, new Color(.9f, .97f, 1f), mode, head, new Color(1.6f, 1.7f, 1.75f), tier, 1 + side);
                    PoolFixture(chunk, cell, local + Vector3.down * .15f, Quaternion.Euler(90f, 0f, 0f), LightType.Spot, 170f, 130f, 3f, 8f, new Color(.9f, .97f, 1f), mode, head, new Color(1.6f, 1.7f, 1.75f), tier, 3 + side);
                }
                PoolNiches(chunk, room, roomOrigin, -.6f, 4, new Color(.55f, .92f, 1f), 2.4f, 10f, tier);
                break;
            }
            case PoolLampKit.StairLights:
            {
                var warm = new Color(1f, .86f, .7f);
                var glow = FrontRoomsPoolMaterials.Glow("stair light", new Color(1.5f, 1.25f, .95f));
                var p0 = roomOrigin + new Vector3(Mathf.Min(2.8f, w - .15f), baseY + 2.4f, .8f);
                var p1 = roomOrigin + new Vector3(.2f, baseY - .3f, Mathf.Min(6.24f, d - .2f));
                var g0 = PoolLensBox(chunk, "STAND-IN stair light", p0, Quaternion.identity, new Vector3(.12f, .22f, .3f), glow);
                var g1 = PoolLensBox(chunk, "STAND-IN stair light", p1, Quaternion.identity, new Vector3(.12f, .22f, .3f), glow);
                PoolFixture(chunk, CellAtLocal(p0.x - roomOrigin.x, p0.z - roomOrigin.z), p0 + Vector3.left * .1f, Quaternion.identity, LightType.Point, 0f, 0f, 1.3f, 5f, warm, ModuleLamp.Steady, g0, new Color(1.5f, 1.25f, .95f), tier, 1);
                PoolFixture(chunk, CellAtLocal(p1.x - roomOrigin.x, p1.z - roomOrigin.z), p1 + Vector3.right * .1f, Quaternion.identity, LightType.Point, 0f, 0f, .7f, 4f, warm, ModuleLamp.Steady, g1, new Color(1.5f, 1.25f, .95f), tier, 2);
                break;
            }
            case PoolLampKit.Niche:
                // The dark zone: two warm niches per long wall, under the water at -2.0 (Q4: the grade follows them).
                PoolNiches(chunk, room, roomOrigin, -2.0f - baseY, 2, new Color(1f, .82f, .62f), 2.4f, 10f, tier);
                break;
        }
        // PC_Lockers' two globes in the north-east cell.
        if (room.id == "PC_Lockers")
        {
            var globe = FrontRoomsPoolMaterials.Glow("globe", new Color(1.6f, 1.38f, 1.1f));
            for (var k = 0; k < 2; k++)
            {
                var pos = roomOrigin + new Vector3(w - 1.5f + (k == 0 ? -.6f : .6f), ceilingY - .35f, d - 1.5f);
                var g = PoolLensBox(chunk, "STAND-IN globe", pos, Quaternion.identity, new Vector3(.3f, .3f, .3f), globe);
                PoolFixture(chunk, CellAtLocal(pos.x - roomOrigin.x, pos.z - roomOrigin.z), pos, Quaternion.identity, LightType.Point, 0f, 0f, 1.0f, 3.6f, new Color(1f, .86f, .7f), ModuleLamp.Auto, g, new Color(1.6f, 1.38f, 1.1f), tier, 5 + k);
            }
        }
    }

    // Wall niches looking across the room (the long walls), height relative to the room's base plane.
    void PoolNiches(BuiltChunk chunk, PoolRoom room, Vector3 roomOrigin, float yRel, int perWall, Color color, float intensity, float range, int tier)
    {
        var cs = MapGrid.CellSize;
        float w = room.width * cs, d = room.depth * cs;
        var baseY = room.BaseY;
        var alongX = w >= d;
        var run = alongX ? w : d;
        var glow = FrontRoomsPoolMaterials.Glow("niche " + color.r.ToString("0.00"), color * 2.2f);
        for (var wall = 0; wall < 2; wall++)
        for (var k = 0; k < perWall; k++)
        {
            var p = run * (k + 1) / (perWall + 1);
            var inset = WallThickness * .5f + .01f;
            var x = alongX ? p : (wall == 0 ? inset : w - inset);
            var z = alongX ? (wall == 0 ? inset : d - inset) : p;
            var inward = alongX ? new Vector3(0f, 0f, wall == 0 ? 1f : -1f) : new Vector3(wall == 0 ? 1f : -1f, 0f, 0f);
            var pos = roomOrigin + new Vector3(x, baseY + yRel, z);
            var go = new GameObject("STAND-IN niche lamp " + room.label);
            go.transform.SetParent(chunk.root.transform, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.LookRotation(inward, Vector3.up);
            var mesh = PoolDiscMesh(.12f, 16);
            chunk.meshes.Add(mesh);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = glow;
            r.shadowCastingMode = ShadowCastingMode.Off;
            var cell = new GridCoord(room.origin.x + Mathf.Clamp((int)(x / cs), 0, room.width - 1), room.origin.y + Mathf.Clamp((int)(z / cs), 0, room.depth - 1));
            PoolFixture(chunk, cell, pos + inward * .05f, Quaternion.LookRotation(inward, Vector3.up), LightType.Spot, 110f, 40f, intensity, range, color, ModuleLamp.Steady, r, color * 2.2f, tier, 4 + wall * 2 + k);
        }
    }

    // ---------- BuildPoolChunk (the BuildInto hook) ----------

    /// <summary>
    /// The pool cells of one chunk (BuildInto, before the blocks are flushed): floors, ceilings, water and caustics, lamps,
    /// columns, missing tiles and stand-ins. Void cells build nothing; X and the start area are the stairwell's.
    /// </summary>
    void BuildPoolChunk(BuiltChunk chunk, MapChunk data, Func<int, int, float, int> blockOf, BuilderFn get, SolidFn solid, MeshBuilder collision, Vector3 origin)
    {
        var plan = Cache.Pool;
        if (plan == null || data == null || chunk == null || chunk.root == null) return;
        const int n = MapGrid.ChunkCells;
        var cs = MapGrid.CellSize;
        var tile = FrontRoomsPoolMaterials.Tile;
        var waters = new Dictionary<Material, MeshBuilder>();
        var caustics = new Dictionary<Material, MeshBuilder>();
        for (var j = 0; j < n; j++)
        for (var i = 0; i < n; i++)
        {
            var cell = data.Cell(i, j);
            if (!Cache.IsPoolCell(cell) || InExitArea(cell) || InStartArea(cell)) continue;
            var pc = plan.CellOf(cell);
            if (pc.connector == CellConnector.Void) continue;
            var index = MapGrid.LocalIndex(i, j);
            var room = plan.RoomAt(cell);
            var skin = PoolRules.SkinOf(room != null ? room.module : null);
            var zone = Cache.ZoneOf(cell);
            var theme = Theme(zone.theme);
            var rel = MapGrid.CeilingHeight(zone.height);
            var baseY = skin.baseY;
            var ceilingY = baseY + rel;
            var block = blockOf(i, j, rel);
            var x0 = i * cs;
            var z0 = j * cs;
            var cellCenter = new Vector3(x0 + cs * .5f, 0f, z0 + cs * .5f);

            if (tile != null) PoolFloor(pc, x0, z0, block, get, solid, collision, origin, tile);

            Material ceiling;
            var ceilingRepeat = 1f;
            switch (skin.ceiling)
            {
                case PoolCeiling.Grid:
                case PoolCeiling.GridDeadMissing: ceiling = level0 != null ? level0.ceiling : tile; ceilingRepeat = CeilingRepeat; break;
                case PoolCeiling.Plain: ceiling = PoolBlock; break;
                case PoolCeiling.Rig: ceiling = PoolPaint; ceilingRepeat = WallpaperRepeat; break;
                default: ceiling = tile; break;
            }
            if (ceiling != null) solid(block, ceiling, cellCenter + Vector3.up * (ceilingY + ModuleUnits.CeilingSlab * .5f), new Vector3(cs, ModuleUnits.CeilingSlab, cs), ceilingRepeat);

            if (pc.HasWater)
            {
                var water = FrontRoomsPoolMaterials.WaterOf(zone.theme == ZoneTheme.PoolClub ? FrontRoomsPoolMaterials.Water.Club : zone.theme == ZoneTheme.PoolDark ? FrontRoomsPoolMaterials.Water.Dark : FrontRoomsPoolMaterials.Water.Level);
                if (water != null)
                {
                    if (!waters.TryGetValue(water, out var wb)) waters[water] = wb = new MeshBuilder();
                    PoolQuad(wb, new Vector3(x0, pc.waterY, z0), new Vector3(x0, pc.waterY, z0 + cs), new Vector3(x0 + cs, pc.waterY, z0 + cs), new Vector3(x0 + cs, pc.waterY, z0), Vector3.up, origin, 1f);
                }
                var caustic = FrontRoomsPoolMaterials.Caustic(zone.theme == ZoneTheme.PoolDark);
                if (caustic != null)
                {
                    if (!caustics.TryGetValue(caustic, out var cb)) caustics[caustic] = cb = new MeshBuilder();
                    var y = ceilingY - .008f;
                    PoolQuad(cb, new Vector3(x0, y, z0), new Vector3(x0 + cs, y, z0), new Vector3(x0 + cs, y, z0 + cs), new Vector3(x0, y, z0 + cs), Vector3.down, origin, 1f);
                }
            }

            PoolCellLamps(chunk, data, cell, index, cellCenter, baseY, ceilingY, room, skin, theme);
        }

        // Rooms of this chunk: their own lamps, missing ceiling tiles and stand-ins.
        foreach (var room in plan.Rooms)
        {
            if (room.module == null || MapGrid.ChunkOf(room.origin) != data.coord) continue;
            var o = data.Origin;
            var roomOrigin = new Vector3((room.origin.x - o.x) * cs, 0f, (room.origin.y - o.y) * cs);
            var skin = PoolRules.SkinOf(room.module);
            var baseY = skin.baseY;
            var ceilingY = room.CeilingY;
            try { PoolRoomLamps(chunk, data, room, roomOrigin, baseY, ceilingY); }
            catch (Exception e) { Debug.LogWarning("[FrontRoomsMap] POOL · lamps of " + room.label + " failed: " + e.Message); }
            if (skin.ceiling == PoolCeiling.GridDeadMissing) PoolMissingTiles(room, roomOrigin, ceilingY, blockOf, get, origin, data);
            if (skin.standIns != null && skin.standIns.Length > 0)
            {
                try { PoolStandIns(chunk, plan, room, roomOrigin, skin, collision, origin, data); }
                catch (Exception e) { Debug.LogWarning("[FrontRoomsMap] POOL · stand-ins of " + room.label + " failed: " + e.Message); }
            }
        }

        // Columns: the room's own (PlaceColumns wrote its Custom list into data.pillar), tile, floor to ceiling.
        for (var j = 1; j < n; j++)
        for (var i = 1; i < n; i++)
        {
            if (!data.pillar[i + j * (n + 1)]) continue;
            var cell = data.Cell(i, j);
            if (!Cache.IsPoolCell(cell)) continue;
            var room = plan.RoomAt(cell);
            if (room == null) continue;
            var low = float.MaxValue;
            foreach (var c in new[] { cell, new GridCoord(cell.x - 1, cell.y), new GridCoord(cell.x, cell.y - 1), new GridCoord(cell.x - 1, cell.y - 1) })
            {
                var pcc = plan.CellOf(c);
                var u = c.x == cell.x ? 0f : cs;
                var v = c.y == cell.y ? 0f : cs;
                low = Mathf.Min(low, pcc.StepAt(u, v));
            }
            if (low == float.MaxValue) low = 0f;
            var width = (data.pillarStyle[i + j * (n + 1)] & MapChunk.ColumnLarge) != 0 ? ModuleUnits.ColumnLarge : ModuleUnits.ColumnSmall;
            var top = room.CeilingY;
            var rel = MapGrid.CeilingHeight(room.zone.height);
            if (tile != null)
                solid(blockOf(i, j, rel), tile, new Vector3(i * cs, (low - .02f + top) * .5f, j * cs), new Vector3(width, top - low + .02f, width), 1f);
        }

        // Water and caustics: one renderer per material, no shadows.
        if (waters.Count > 0 || caustics.Count > 0)
        {
            var holder = new GameObject("Pool water");
            holder.transform.SetParent(chunk.root.transform, false);
            foreach (var pair in waters) PoolRenderer(chunk, holder.transform, pair.Key, pair.Value, "water");
            foreach (var pair in caustics) PoolRenderer(chunk, holder.transform, pair.Key, pair.Value, "caustic (greybox)");
        }
    }

    void PoolRenderer(BuiltChunk chunk, Transform parent, Material material, MeshBuilder builder, string name)
    {
        if (material == null || builder == null || builder.vertices.Count == 0) return;
        var go = new GameObject(name + " " + material.name);
        go.transform.SetParent(parent, false);
        var mesh = builder.ToMesh(chunk.root.name + " " + name);
        chunk.meshes.Add(mesh);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    // One pool cell's floor: a flat slab, a sloped quad, or a stair (tread boxes, a dark nosing on every step, a ramp collider).
    static void PoolFloor(ModuleCell c, float x0, float z0, int block, BuilderFn get, SolidFn solid, MeshBuilder collision, Vector3 origin, Material tile)
    {
        var cs = MapGrid.CellSize;
        if (c.connector == CellConnector.Stair && c.stairRisers > 0 && c.stairRun > 1e-4f)
        {
            PoolStair(c, x0, z0, block, get, collision, origin, tile);
            return;
        }
        if (Mathf.Abs(c.slopeX) < 1e-5f && Mathf.Abs(c.slopeZ) < 1e-5f)
        {
            solid(block, tile, new Vector3(x0 + cs * .5f, c.floorY - ModuleUnits.FloorSlab * .5f, z0 + cs * .5f), new Vector3(cs, ModuleUnits.FloorSlab, cs), 1f);
            return;
        }
        var p0 = new Vector3(x0, c.FloorAt(0f, 0f), z0);
        var p1 = new Vector3(x0, c.FloorAt(0f, cs), z0 + cs);
        var p2 = new Vector3(x0 + cs, c.FloorAt(cs, cs), z0 + cs);
        var p3 = new Vector3(x0 + cs, c.FloorAt(cs, 0f), z0);
        var normal = Vector3.Cross(p1 - p0, p3 - p0).normalized;
        if (normal.y < 0f) normal = -normal;
        PoolQuad(get(block, tile), p0, p1, p2, p3, normal, origin, 1f);
        PoolQuad(collision, p0, p1, p2, p3, normal, origin, 1f);
    }

    // A stair cell: the entry flat, a box per tread from a common bottom (so each riser is the higher box's face), the
    // final flat; the nosing 50 mm on the tread and 25 mm down the riser, 2 mm proud; collision: the flats and one ramp.
    static void PoolStair(ModuleCell c, float x0, float z0, int block, BuilderFn get, MeshBuilder collision, Vector3 origin, Material tile)
    {
        var cs = MapGrid.CellSize;
        int n = c.stairRisers;
        var dir = c.stairDir & 3;
        // The descent axis S and the entry corner E (s = 0, w = 0); W runs across the flight.
        Vector3 S, W, E;
        switch (dir)
        {
            case 0: S = Vector3.forward; W = Vector3.right; E = new Vector3(x0, 0f, z0); break;
            case 1: S = Vector3.right; W = Vector3.forward; E = new Vector3(x0, 0f, z0); break;
            case 2: S = Vector3.back; W = Vector3.right; E = new Vector3(x0, 0f, z0 + cs); break;
            default: S = Vector3.left; W = Vector3.forward; E = new Vector3(x0 + cs, 0f, z0); break;
        }
        var bottom = c.floorY - c.stairRise * n - ModuleUnits.FloorSlab;
        var nose = FrontRoomsPoolMaterials.Nosing;
        void Tread(float s0, float s1, float top, bool collide)
        {
            if (s1 - s0 < .005f) return;
            var centre = E + S * ((s0 + s1) * .5f) + W * (cs * .5f) + Vector3.up * ((top + bottom) * .5f);
            var size = Abs(S * (s1 - s0) + W * cs + Vector3.up * (top - bottom));
            get(block, tile).Box(centre, size, origin, 1f);
            if (collide && collision != null)
                collision.Box(E + S * ((s0 + s1) * .5f) + W * (cs * .5f) + Vector3.up * (top - ModuleUnits.FloorSlab * .5f), Abs(S * (s1 - s0) + W * cs + Vector3.up * ModuleUnits.FloorSlab), origin, 1f);
        }
        Tread(0f, c.stairFrom, c.floorY, true);
        for (var k = 1; k <= n; k++)
            Tread(c.stairFrom + (k - 1) * c.stairRun, c.stairFrom + k * c.stairRun, c.floorY - k * c.stairRise, false);
        var end = c.stairFrom + n * c.stairRun;
        Tread(end, cs, c.floorY - n * c.stairRise, true);
        // The nosing on every step: at the top of each riser (s = stairFrom + k * run), the higher tread's edge.
        if (nose != null)
            for (var k = 0; k < n; k++)
            {
                var s = c.stairFrom + k * c.stairRun;
                var y = c.floorY - k * c.stairRise;
                get(block, nose).Box(E + S * (s - .025f) + W * (cs * .5f) + Vector3.up * (y + .001f), Abs(S * .05f + W * cs + Vector3.up * .002f), origin, 1f);
                get(block, nose).Box(E + S * (s + .001f) + W * (cs * .5f) + Vector3.up * (y - .0115f), Abs(S * .002f + W * cs + Vector3.up * .027f), origin, 1f);
            }
        // One ramp quad over the flight, collision only (the capsule glides it; DESIGN 6).
        if (collision != null)
        {
            var a0 = E + S * c.stairFrom + Vector3.up * c.floorY;
            var a1 = E + S * c.stairFrom + W * cs + Vector3.up * c.floorY;
            var b1 = E + S * end + W * cs + Vector3.up * (c.floorY - n * c.stairRise);
            var b0 = E + S * end + Vector3.up * (c.floorY - n * c.stairRise);
            var normal = Vector3.Cross(a1 - a0, b0 - a0).normalized;
            if (normal.y < 0f) normal = -normal;
            PoolQuad(collision, a0, a1, b1, b0, normal, origin, 1f);
        }
    }

    // GridDeadMissing: five of the room's cells by hash show a dark plenum where a 0.6 m tile is gone, off the troffer's rect.
    void PoolMissingTiles(PoolRoom room, Vector3 roomOrigin, float ceilingY, Func<int, int, float, int> blockOf, BuilderFn get, Vector3 origin, MapChunk data)
    {
        var plenum = FrontRoomsPoolMaterials.Surface("Plenum_Dark", new Color(.05f, .05f, .05f));
        if (plenum == null) return;
        var cs = MapGrid.CellSize;
        var count = room.width * room.depth;
        var o = data.Origin;
        var picked = new HashSet<int>();
        for (var k = 0; k < 40 && picked.Count < Mathf.Min(5, count); k++)
        {
            var h = MapHash.Hash(Cache.Generator.Seed, room.origin.x * 31 + k, room.origin.y, 241);
            var c = (int)(h % (uint)count);
            if (!picked.Add(c)) continue;
            int ci = c % room.width, cj = c / room.width;
            // The tile: one of the 0.6 m grid squares off the troffer's X [1.2, 1.8] x Z [1.2, 2.4].
            int[] xs = { 0, 1, 3, 4 };
            int[] zs = { 0, 1, 4 };
            var tx = xs[(int)((h >> 8) % 4)];
            var tz = zs[(int)((h >> 12) % 3)];
            var x = roomOrigin.x + ci * cs + tx * .6f + .3f;
            var z = roomOrigin.z + cj * cs + tz * .6f + .3f;
            var y = ceilingY - .003f;
            int li = room.origin.x + ci - o.x, lj = room.origin.y + cj - o.y;
            var block = blockOf(Mathf.Clamp(li, 0, MapGrid.ChunkCells - 1), Mathf.Clamp(lj, 0, MapGrid.ChunkCells - 1), MapGrid.CeilingHeight(room.zone.height));
            PoolQuad(get(block, plenum), new Vector3(x - .3f, y, z - .3f), new Vector3(x + .3f, y, z - .3f), new Vector3(x + .3f, y, z + .3f), new Vector3(x - .3f, y, z + .3f), Vector3.down, origin, 1f);
        }
    }

    // The room's STAND-IN kits (FrontRoomsPoolStandIns), on the floor at their point or above the base plane; collider
    // boxes go into the chunk's one collision mesh.
    void PoolStandIns(BuiltChunk chunk, FrontRoomsPoolPlan plan, PoolRoom room, Vector3 roomOrigin, ModulePool skin, MeshBuilder collision, Vector3 origin, MapChunk data)
    {
        var holder = new GameObject("STAND-IN kits " + room.label);
        holder.transform.SetParent(chunk.root.transform, false);
        var cs = MapGrid.CellSize;
        var chunkMetres = new Vector3(data.Origin.x * cs, 0f, data.Origin.y * cs);
        Func<Vector3, float> floorAt = p => plan.FloorAt(p.x + chunkMetres.x, p.z + chunkMetres.z);
        Action<Vector3, Vector3> collide = (c, s) => { if (collision != null) collision.Box(c, s, origin, 1f); };
        foreach (var s in skin.standIns)
        {
            var local = roomOrigin + new Vector3(s.x, 0f, s.z);
            // The floor 5 cm behind the kit's point: a kit standing on a basin edge (the ladders at x 3.0) takes the deck
            // it stands on, not the basin floor the edge's far cell would give.
            var back = Quaternion.Euler(0f, s.yaw, 0f) * Vector3.back * .05f;
            local.y = s.fromBase ? skin.baseY + s.y : floorAt(local + back) + s.y;
            FrontRoomsPoolStandIns.Build(s.kit, holder.transform, local, s.yaw, s.length, s.collider ? collide : null, floorAt, chunk.meshes);
        }
    }
}
