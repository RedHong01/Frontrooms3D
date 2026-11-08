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
// The surfaces that meet a water (wall tile and course, risers, the weir lip, floors and stairs under it, columns in it)
// take the tile's instance for that water's height (PoolWet: FrontRoomsPoolTile.ForWater), so the wet band and the
// meniscus sit on the line; every other surface keeps the shared tile.
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

    /// <summary>The pool's fire stair door (the visual line's zone doors, Map stage 2026-11-06; bathroom_rooms 50: PL_FireStair's
    /// "S: fire door (the tower's)"): a door with one side in PL_FireStair and the other outside it. The kit dresses it as the
    /// Stair member (DoorMemberFor, M8): a hollow-metal stair door in the stairwell A language, not the pool's FRP door. False with
    /// no plan, so every map but the structures scene's pool route is unchanged.</summary>
    public bool IsPoolFireStairDoor(Door door)
    {
        var plan = PoolPlan;
        if (door == null || plan == null || !plan.Ready) return false;
        var ra = plan.RoomAt(door.a);
        var rb = plan.RoomAt(door.b);
        return (ra != null && ra.id == "PL_FireStair") != (rb != null && rb.id == "PL_FireStair");
    }

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

    // ---------- The wet band (pool_tile_real's follow-up: FrontRoomsPoolTile.ForWater) ----------

    // A height of the plan (map metres) as the world height the shader's _WaterY is in.
    float WorldWaterY(float y) => transform.TransformPoint(new Vector3(0f, y, 0f)).y;

    // The material for a surface that meets a water at waterY (map metres): the game's tile kind's instance for that height
    // (one per kind, tier and water height to the mm, shared by every renderer at that water), so FrontRooms/Surface draws
    // the wet band, the meniscus on the line and the soaked grout under it. NaN (no water), a dry material that is not that
    // kind's shared material (the LS fallback, paint, block) or a missing instance: the material as given. Above its band an
    // instance draws as the shared tile. Phones get the _Phone twin's instance (ForWater reads the tier); WebGL has no pool.
    // A water that rises or falls later moves its line with FrontRoomsPoolTile.MoveWater(WorldWaterY(from), WorldWaterY(to)).
    // FrontRoomsSurfaceDetail.Today: the shared tile everywhere (today's look; a change rebuilds the chunks, PollSurfaceDetail).
    Material PoolWet(string kind, Material dry, float waterY)
    {
        if (dry == null || float.IsNaN(waterY) || FrontRoomsSurfaceDetail.Today || FrontRoomsPoolTile.Get(kind) != dry) return dry;
        var wet = FrontRoomsPoolTile.ForWater(kind, WorldWaterY(waterY));
        if (wet == null || wet == dry) return dry;
        poolWetDry[wet] = dry;
        return wet;
    }

    // Each wet instance PoolWet handed out and the shared material it stands for; and, while a chunk builds, the blocks
    // (BlockOf) each one has surfaces in, noted where its geometry goes in (PoolWetUsed) for PoolEvenWetBounds.
    readonly Dictionary<Material, Material> poolWetDry = new Dictionary<Material, Material>();
    readonly Dictionary<Material, HashSet<int>> poolWetBlocks = new Dictionary<Material, HashSet<int>>();
    MeshBuilder poolWetChunk;   // the chunk build the notes belong to (its collision builder, poolCollision)

    void PoolWetUsed(int block, Material material)
    {
        if (material == null || !poolWetDry.ContainsKey(material)) return;
        if (poolWetChunk != poolCollision) { poolWetBlocks.Clear(); poolWetChunk = poolCollision; }
        if (!poolWetBlocks.TryGetValue(material, out var set)) poolWetBlocks[material] = set = new HashSet<int>();
        set.Add(block);
    }

    // A wet instance takes its surfaces out of the block's one renderer of the shared tile, and the tile box-projects its
    // reflection with the probe URP picks per renderer by the renderer's bounds (FrontRooms/Surface FRTileEnvSample:
    // unity_SpecCube0's box). Smaller renderers took another probe (PL_Turn's aqua course 20 % brighter, probe_wet_v2), so
    // every builder of that kind in that block gets the bounds the one renderer had: the union's two corners, as vertices no
    // triangle uses (ToMesh's RecalculateBounds keeps them). The probe, the reflections and the tile's look stay as they were;
    // culling and shadows see the bounds they saw before. BuildPoolChunk's end, before BuildInto makes the renderers.
    void PoolEvenWetBounds(BuilderFn get, MeshBuilder collision)
    {
        if (poolWetChunk != collision || poolWetBlocks.Count == 0) { poolWetBlocks.Clear(); poolWetChunk = null; return; }
        var dryByBlock = new Dictionary<int, List<Material>>();
        foreach (var used in poolWetBlocks)
        {
            var dry = poolWetDry[used.Key];
            foreach (var b in used.Value)
            {
                if (!dryByBlock.TryGetValue(b, out var list)) dryByBlock[b] = list = new List<Material>();
                if (!list.Contains(dry)) list.Add(dry);
            }
        }
        var group = new List<MeshBuilder>();
        foreach (var at in dryByBlock)
        foreach (var dry in at.Value)
        {
            group.Clear();
            var shared = get(at.Key, dry);   // the block holds an instance of the kind, so it is a block that builds
            if (shared.vertices.Count > 0) group.Add(shared);
            foreach (var used in poolWetBlocks)
                if (poolWetDry[used.Key] == dry && used.Value.Contains(at.Key))
                {
                    var wet = get(at.Key, used.Key);
                    if (wet.vertices.Count > 0) group.Add(wet);
                }
            if (group.Count < 2) continue;
            var min = group[0].vertices[0];
            var max = min;
            foreach (var m in group)
                foreach (var v in m.vertices) { min = Vector3.Min(min, v); max = Vector3.Max(max, v); }
            foreach (var m in group)
            {
                m.vertices.Add(min); m.normals.Add(Vector3.up); m.uvs.Add(Vector2.zero);
                m.vertices.Add(max); m.normals.Add(Vector3.up); m.uvs.Add(Vector2.zero);
            }
        }
        poolWetBlocks.Clear();
        poolWetChunk = null;
    }

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
        // The cell's water surface (map metres) when it holds one (Ankle, Wade), else NaN: the skin's wet band (PoolWet).
        public float water = float.NaN;
        public Material wall, upper;
        public float upperRepeat = 1f;
        // Surface bevels: the radius of this side's rounded corners (0: knife edges) and its segments.
        public float round;
        public int roundSegments = 2;
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
        s.water = s.data.HasWater ? s.data.waterY : float.NaN;
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

        // Corner reach exactly as BuildEdge's; surface bevels read each post's shape right after its CornerReach.
        var half = WallThickness * .5f;
        A.reachStart = A.reachEnd = B.reachStart = B.reachEnd = half;
        int endS = BevelEndNone, endE = BevelEndNone;
        float outS = 0f, outE = 0f;
        bool ownS = false, ownE = false;
        if (FrontRoomsTransitionKit.B0)
        {
            var startPoint = alongX ? new GridCoord(a.x, a.y + 1) : new GridCoord(a.x + 1, a.y);
            var startRole = alongX ? FrontRoomsTransitionKit.East : FrontRoomsTransitionKit.North;
            CornerReach(startPoint, startRole, out var sa, out var sb);
            if (bevelChunk) endS = BevelEndAt(startPoint, startRole, alongX, out outS, out ownS);
            var endPoint = new GridCoord(a.x + 1, a.y + 1);
            var endRole = alongX ? FrontRoomsTransitionKit.West : FrontRoomsTransitionKit.South;
            CornerReach(endPoint, endRole, out var ea, out var eb);
            if (bevelChunk) endE = BevelEndAt(endPoint, endRole, alongX, out outE, out ownE);
            A.reachStart *= sa; B.reachStart *= sb; A.reachEnd *= ea; B.reachEnd *= eb;
        }
        // Surface bevels: a pool side's corners take the tile's bullnose (its upper finish the same round, so a corner's
        // profile never changes at the tile's top), a map cell's paper its 3 mm round; none under FrontRoomsSurfaceDetail.Today.
        PoolSideRound(A);
        PoolSideRound(B);

        Cache.TryPoolRecord(a, b, out var record);

        // A skin box: collision f..t, y0..y1 as Solid writes it; render rf..rt, ry0..ry1 (the same extents: Solid itself).
        void Skin(PoolSide side, Material material, float f, float t, float y0, float y1, float repeat, float rf, float rt, float ry0, float ry1)
        {
            if (t - f < .02f || y1 - y0 < .005f || material == null) return;
            var center = start + along * ((f + t) * .5f) + Vector3.up * ((y0 + y1) * .5f) + across * (WallThickness * .25f * side.sign);
            var size = Abs(along * (t - f) + across * (WallThickness * .5f) + Vector3.up * (y1 - y0));
            if (rf == f && rt == t && ry0 == y0 && ry1 == y1) solid(side.block, material, center, size, repeat);
            else
            {
                if (rt - rf > 1e-4f && ry1 - ry0 > 1e-4f)
                    get(side.block, material).Box(start + along * ((rf + rt) * .5f) + Vector3.up * ((ry0 + ry1) * .5f) + across * (WallThickness * .25f * side.sign),
                        Abs(along * (rt - rf) + across * (WallThickness * .5f) + Vector3.up * (ry1 - ry0)), origin, repeat);
                poolCollision.Box(center, size, origin, 1f);
            }
            PoolWetUsed(side.block, material);
        }

        // One side's skin of a piece: the tile up to the tile's top, the upper finish over it, the aqua course. role (surface
        // bevels, with open): 1 left of an arch (its end is a jamb), 2 right of it, 3 over it (its bottom is the soffit).
        void SidePiece(PoolSide side, float from, float to, float bottom, float top, bool extendStart, bool extendEnd, int role = 0, PoolOpening open = null)
        {
            if (!side.exists) return;
            if (from > 0f || !mayExtendStart) extendStart = false;
            if (to < length || !mayExtendEnd) extendEnd = false;
            var lo = Mathf.Max(bottom, side.low);
            var hi = Mathf.Min(top, side.top);
            if (hi - lo < .02f) return;
            var f = from - (extendStart ? side.reachStart : 0f);
            var t = to + (extendEnd ? side.reachEnd : 0f);
            // Surface bevels: the render stops a round short of each rounded arris (a header widened and raised over its
            // soffit's rounds); collision keeps f..t, lo..hi.
            var rr = side.round;
            var freeS = rr > 0f && extendStart && endS == BevelEndFree;
            var freeE = rr > 0f && extendEnd && endE == BevelEndFree;
            var lS = rr > 0f && extendStart && endS == BevelEndL;
            var lE = rr > 0f && extendEnd && endE == BevelEndL;
            float rf = f, rt = t, rlo = lo;
            if (freeS || lS) rf += rr;
            if (freeE || lE) rt -= rr;
            if (rr > 0f && open != null)
            {
                if (role == 1) rt -= rr;
                if (role == 2) rf += rr;
                if (role == 3) { rf -= rr; rt += rr; rlo += rr; }
            }
            if (!side.pool)
            {
                Skin(side, side.wall, f, t, lo, hi, side.upperRepeat, rf, rt, rlo, hi);
                if (rr > 0f) PoolSideRounds(side, f, t, lo, hi, hi, side.wall, side.wall, side.upperRepeat, float.NaN, float.NaN, null, freeS, freeE, lS && ownS && outS == side.sign, lE && ownE && outE == side.sign, role, open);
                return;
            }
            var split = Mathf.Clamp(side.tileTopY, lo, hi);
            // At a water the tile takes its wet band; the upper finish too when it is the same tile and starts within 0.10 m
            // of the line (the band reaches 6 cm over it).
            var wetWall = PoolWet("FieldMesh", side.wall, side.water);
            var upperWet = side.upper == side.wall && !float.IsNaN(side.water) && split - side.water < .10f;
            var upperMat = upperWet ? wetWall : side.upper;
            Skin(side, wetWall, f, t, lo, split, 1f, rf, rt, Mathf.Min(Mathf.Max(lo, rlo), split), split);
            Skin(side, upperMat, f, t, split, hi, side.upperRepeat, rf, rt, Mathf.Min(Mathf.Max(split, rlo), hi), hi);
            // The aqua course, render only, 4 mm proud of the skin (rounded: its top, and its end where the skin's corner rounds).
            var skin = side.skin;
            float cb0 = float.NaN, cb1 = float.NaN;
            Material band = null;
            if (skin != null && skin.bandHigh > skin.bandLow)
            {
                var floor = side.FloorAt((from + to) * .5f);
                var b0 = (skin.bandOnFloor ? floor : side.baseY) + skin.bandLow;
                var b1 = (skin.bandOnFloor ? floor : side.baseY) + skin.bandHigh;
                b0 = Mathf.Max(b0, lo);
                b1 = Mathf.Min(b1, hi);
                // the waterline course (the level's, 2-32 cm over the floor) carries the line in ankle water
                band = PoolWet("CourseMesh", FrontRoomsPoolMaterials.Band, side.water);
                if (b1 - b0 > .005f && band != null)
                {
                    var c = start + along * ((rf + rt) * .5f) + Vector3.up * ((b0 + b1) * .5f) + across * (side.sign * (WallThickness * .5f + .002f));
                    var size = Abs(along * (rt - rf) + across * .004f + Vector3.up * (b1 - b0));
                    if (rr > 0f && BevelMaterial(band))
                    {
                        var outward = across * side.sign;
                        var edges = FrontRoomsBevel.Edge(Vector3.up, outward);
                        if (rf > f) edges |= FrontRoomsBevel.Edge(-along, outward);
                        if (rt < t) edges |= FrontRoomsBevel.Edge(along, outward);
                        BevelBoxInto(get(side.block, band), band, c, size, edges, BevelCourseRadius, BevelCourseSegments, origin, 1f);
                        cb0 = b0;
                        cb1 = b1;
                    }
                    else get(side.block, band).Box(c, size, origin, 1f);
                    PoolWetUsed(side.block, band);
                }
            }
            if (rr > 0f) PoolSideRounds(side, f, t, lo, hi, split, wetWall, upperMat, side.upperRepeat, cb0, cb1, band, freeS, freeE, lS && ownS && outS == side.sign, lE && ownE && outE == side.sign, role, open);
        }

        // Surface bevels: one side's rounds of a piece (its render boxes already stop short of them): at a free end this side's
        // corner and the end narrowed by the round; the outside corner of an L (drawn by its east-west arm); an arch's jamb
        // (to the springing or the soffit, mitred there) with its reveal narrowed, and the soffit with its flat. Each round
        // takes the skin's material at its height, and the course's colour where the course meets the corner.
        void PoolSideRounds(PoolSide side, float f, float t, float lo, float hi, float split, Material tileFinish, Material upperMat, float upperRepeat,
            float cb0, float cb1, Material band, bool freeS, bool freeE, bool lOwnS, bool lOwnE, int role, PoolOpening open)
        {
            var rr = side.round;
            var seg = side.roundSegments;
            var face = across * side.sign;
            var faceOff = across * (half * side.sign);
            var inset = across * ((half - rr) * side.sign);
            var up = Vector3.up;
            Material At(float y, out float repeat)
            {
                if (band != null && !float.IsNaN(cb0) && y > cb0 && y < cb1) { repeat = 1f; return band; }
                if (y < split) { repeat = side.pool ? 1f : upperRepeat; return tileFinish; }
                repeat = upperRepeat;
                return upperMat;
            }
            // a vertical round at along-position s from y0 to y1, split where its material changes; the top end cut by cutTop
            void Vertical(float s, float y0, float y1, Vector3 nB, Vector3 cutTop)
            {
                if (y1 - y0 < 1e-4f) return;
                var ys = new List<float> { y0, y1 };
                foreach (var y in new[] { cb0, cb1, split }) if (!float.IsNaN(y) && y > y0 + 1e-4f && y < y1 - 1e-4f) ys.Add(y);
                ys.Sort();
                var p = start + along * s + faceOff;
                for (var i = 0; i + 1 < ys.Count; i++)
                {
                    var m = At((ys[i] + ys[i + 1]) * .5f, out var repeat);
                    if (m == null) continue;
                    BevelStripInto(get(side.block, m), m, p + up * ys[i], p + up * ys[i + 1], face, nB, rr, seg, Vector3.zero, i + 2 == ys.Count ? cutTop : Vector3.zero, origin, repeat);
                }
            }
            // a flat across this side's half (from the round's inset to the wall's middle) at along-position s, y0..y1, split at the tile's top
            void Flat(float s, float y0, float y1, Vector3 normal)
            {
                var ys = new List<float> { y0, y1 };
                if (split > y0 + 1e-4f && split < y1 - 1e-4f) ys.Add(split);
                ys.Sort();
                var p = start + along * s;
                for (var i = 0; i + 1 < ys.Count; i++)
                {
                    var m = (ys[i] + ys[i + 1]) * .5f < split ? tileFinish : upperMat;
                    var repeat = (ys[i] + ys[i + 1]) * .5f < split && side.pool ? 1f : upperRepeat;
                    if (m != null) BevelQuadInto(get(side.block, m), p + inset + up * ys[i], p + inset + up * ys[i + 1], p + up * ys[i + 1], p + up * ys[i], normal, origin, repeat);
                }
            }
            // lattice ends
            if (freeS || lOwnS) Vertical(f, lo, hi, -along, Vector3.zero);
            if (freeS) Flat(f, lo, hi, -along);
            if (freeE || lOwnE) Vertical(t, lo, hi, along, Vector3.zero);
            if (freeE) Flat(t, lo, hi, along);
            if (open == null) return;
            if (role == 1 || role == 2)
            {
                // a jamb: from the floor to the opening's top (a rect soffit's mitre; a round arch's springing, where its sweep goes on)
                var s = role == 1 ? t : f;
                var nB = role == 1 ? along : -along;
                var top = Mathf.Min(open.top, hi);
                var cut = open.mitreTop ? (role == 1 ? FrontRoomsBevel.Mitre(up, along) : FrontRoomsBevel.Mitre(along, -up)) : Vector3.zero;
                if (top > lo)
                {
                    Vertical(s, lo, top, nB, cut);
                    Flat(s, lo, top, nB);
                }
            }
            else if (role == 3)
            {
                // the soffit, mitred into both jambs, and its flat
                var m = At(lo + 1e-3f, out var repeat);
                if (m == null) return;
                var y = lo;
                var k0 = start + along * open.l + up * y + faceOff;
                var k1 = start + along * open.r + up * y + faceOff;
                BevelStripInto(get(side.block, m), m, k0, k1, face, -up, rr, seg, FrontRoomsBevel.Mitre(up, along), FrontRoomsBevel.Mitre(along, -up), origin, repeat);
                var q0 = start + along * open.l + up * y;
                var q1 = start + along * open.r + up * y;
                BevelQuadInto(get(side.block, m), q0 + inset, q1 + inset, q1, q0, -up, origin, repeat);
            }
        }

        void Piece(float from, float to, float bottom, float top, bool extendStart, bool extendEnd, int role = 0, PoolOpening open = null)
        {
            SidePiece(A, from, to, bottom, top, extendStart, extendEnd, role, open);
            SidePiece(B, from, to, bottom, top, extendStart, extendEnd, role, open);
        }

        var minLow = Mathf.Min(A.exists ? A.low : 0f, B.exists ? B.low : 0f);
        var maxTop = Mathf.Max(A.exists ? A.top : 0f, B.exists ? B.top : 0f);

        if (kind == EdgeKind.Door || kind == EdgeKind.Window)
        {
            // Skirts under the map's door or window where a side's floor lies under 0; the map builds the rest at y 0.
            // (Surface bevels: a skirt keeps its corners, the wall above is the map's.)
            A.round = B.round = 0f;
            if (A.exists && A.pool && A.low < -.01f) SidePiece(A, 0f, length, A.low, 0f, true, true);
            if (B.exists && B.pool && B.low < -.01f) SidePiece(B, 0f, length, B.low, 0f, true, true);
            return false;
        }

        var tileMat = FrontRoomsPoolMaterials.Tile;
        if (kind == EdgeKind.Open)
        {
            if (A.exists && B.exists)
            {
                // surface bevels: a basin's edge rounds the high floor's top (PoolSlabRound), so its riser stops short of it
                var slabA = PoolSlabRound(A.cell, A.data, B.cell, alongX ? 3 : 1);
                var slabB = PoolSlabRound(B.cell, B.data, A.cell, alongX ? 2 : 0);
                PoolRisers(A, B, 0f, length, start, along, across, get, origin, slabA, slabB);
                PoolWeirAndCoping(A, B, start, along, across, get, origin);
            }
            return true;
        }

        if (kind == EdgeKind.Wall)
        {
            if (record.style == PoolEdgeStyle.ClearGlass)
            {
                // Jambs 0.2, a 0.2 curb, the head from 2.7 up; a 3 cm pane of the map's glass between (it blocks, as glass does).
                // Jambs and head are the plan's (FrontRoomsPoolPlan.GlassJamb, GlassHead: OpeningSpanAt keeps the valances off
                // this pane), so change them there.
                const float jamb = FrontRoomsPoolPlan.GlassJamb;
                var baseMax = Mathf.Max(A.exists ? A.baseY : 0f, B.exists ? B.baseY : 0f);
                var curb = Mathf.Max(A.exists ? A.high : 0f, B.exists ? B.high : 0f) + .2f;
                var head = baseMax + FrontRoomsPoolPlan.GlassHead;
                Piece(0f, jamb, minLow, maxTop, true, false);
                Piece(length - jamb, length, minLow, maxTop, false, true);
                Piece(jamb, length - jamb, minLow, curb, false, false);
                Piece(jamb, length - jamb, head, maxTop, false, false);
                if (head > curb + .1f && glass != null)
                    solid(blockIndex, glass, start + along * (length * .5f) + Vector3.up * ((curb + head) * .5f), Abs(along * (length - 2f * jamb) + across * ModuleUnits.GlassThickness + Vector3.up * (head - curb)), 1f);
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
        // The opening's top, worked out before the jambs so their rounds know it: a round head's springing, a rect's soffit.
        var roundHead = record.style == PoolEdgeStyle.RoundArch;
        var radius = width * .5f;
        float crown = 0f, openingTop;
        if (roundHead)
        {
            crown = Mathf.Min(record.crown > 0f || baseMin < -.01f ? record.crown : baseMin + 2.3f, ceilMin - PoolRules.CrownClearance);
            crown = Mathf.Max(crown, floorMax + radius + 1.2f);
            if (crown > ceilMin - .05f) crown = ceilMin - .05f;
            openingTop = crown - radius;
        }
        else
        {
            openingTop = baseMin + Mathf.Min(ModuleUnits.ArchTop, ceilMin - baseMin - ModuleUnits.ArchHeaderMin);
            if (openingTop < floorMax + 2.0f) openingTop = Mathf.Min(floorMax + 2.0f, ceilMin - .05f);
        }
        var opening = new PoolOpening { l = l, r = r, top = openingTop, mitreTop = !roundHead };
        Piece(0f, l, minLow, maxTop, true, false, 1, opening);
        Piece(r, length, minLow, maxTop, false, true, 2, opening);
        if (A.exists && B.exists) PoolRisers(A, B, l, r, start, along, across, get, origin);
        if (roundHead)
        {
            PoolRoundArch(A, B, c, radius, crown, maxTop, start, along, across, get, solid, origin, tileMat);
            if ((record.trim & 3) != 0) PoolArchTrim(A, B, c, radius, crown, record.trim, start, along, across, get, origin);
            return true;
        }
        Piece(l, r, openingTop, maxTop, false, false, 3, opening);
        return true;
    }

    // Surface bevels: the opening an arch's pieces round (along-positions of its jambs; its top: the soffit, or a round
    // head's springing, where the jamb's round goes on as the head's sweep instead of a mitre).
    sealed class PoolOpening
    {
        public float l, r, top;
        public bool mitreTop;
    }

    // Surface bevels: a side's round (radius and segments): the tile's bullnose on a pool side, 3 mm on a map cell's paper;
    // 0 when the chunk keeps box edges or the side's finish is a _FR_DETAIL surface (package B2).
    void PoolSideRound(PoolSide side)
    {
        if (side == null) return;
        side.round = 0f;
        side.roundSegments = 2;
        if (!bevelChunk || !side.exists || !BevelMaterial(side.wall) || !BevelMaterial(side.upper)) return;
        side.round = side.pool ? BevelTileRadius : BevelWallRadius;
        side.roundSegments = side.pool ? BevelTileSegments : BevelWallSegments;
    }

    /// <summary>
    /// Surface bevels: the round on a flat pool floor's top edge on side <paramref name="sideOfCell"/> (0 west, 1 east, 2
    /// south, 3 north) where an open edge drops along its whole length to a lower pool floor (the basin's edge, a step down);
    /// 0 where a coping (a dry deck over a Wade basin) or a weir's lip (two waters) covers that edge, at a stair (its own
    /// noses), a slope, the map's own cells, the start area or X.
    /// </summary>
    float PoolSlabRound(GridCoord cell, ModuleCell c, GridCoord nb, int sideOfCell)
    {
        if (!bevelChunk || Cache == null || Cache.Pool == null || !BevelMaterial(FrontRoomsPoolMaterials.Tile)) return 0f;
        if (c.connector == CellConnector.Void || c.connector == CellConnector.Stair) return 0f;
        if (Mathf.Abs(c.slopeX) > 1e-5f || Mathf.Abs(c.slopeZ) > 1e-5f) return 0f;
        if (!Cache.IsPoolCell(cell) || !Cache.IsPoolCell(nb) || InExitArea(nb) || InStartArea(nb) || InExitArea(cell) || InStartArea(cell)) return 0f;
        var n = Cache.Pool.CellOf(nb);
        if (n.connector == CellConnector.Void) return 0f;
        if (EffectiveEdge(cell, nb) != EdgeKind.Open) return 0f;
        var cs = MapGrid.CellSize;
        for (var k = 0; k <= 6; k++)
        {
            var t = k * cs / 6f;
            float u, v;
            switch (sideOfCell)
            {
                case 0: u = cs; v = t; break;   // the west neighbour's east side
                case 1: u = 0f; v = t; break;   // the east neighbour's west side
                case 2: u = t; v = cs; break;   // the south neighbour's north side
                default: u = t; v = 0f; break;  // the north neighbour's south side
            }
            if (c.floorY - n.StepAt(u, v) < .004f) return 0f;
        }
        if (c.HasWater != n.HasWater && (c.HasWater ? c.water : n.water) == CellWater.Wade) return 0f;
        if (c.HasWater && n.HasWater && Mathf.Abs(c.waterY - n.waterY) > .01f) return 0f;
        return BevelTileRadius;
    }

    // Risers where the two floors differ along [from, to] of the edge: faces toward the lower side on the edge line, with
    // UVs in metres (joints run on from the floor slabs), and collision boxes reaching into the higher side.
    // Surface bevels: slabA / slabB, the round on A's / B's floor top over this edge (PoolSlabRound): a riser under a rounded
    // floor stops that round short of the floor's top (the round is the floor's own, PoolFloor).
    void PoolRisers(PoolSide A, PoolSide B, float from, float to, Vector3 start, Vector3 along, Vector3 across, BuilderFn get, Vector3 origin, float slabA = 0f, float slabB = 0f)
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
            // The face looks toward the lower side; its block is that side's, and so is its water (the basin wall's line).
            var lowSide = runSign > 0f ? B : A;
            var normal = across * runSign;
            var faceTop = runHi - (runSign > 0f ? slabA : slabB);
            var p0 = start + along * runStart + Vector3.up * runLo;
            var p1 = start + along * runStart + Vector3.up * faceTop;
            var p2 = start + along * end + Vector3.up * faceTop;
            var p3 = start + along * end + Vector3.up * runLo;
            var face = PoolWet("FieldMesh", tile, lowSide.water);
            if (faceTop - runLo > 1e-3f) PoolQuad(get(lowSide.block, face), p0, p1, p2, p3, normal, origin, 1f);
            PoolWetUsed(lowSide.block, face);
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
            // The lip: 0.25 deep on the high side, its top a little under the high water (and wet with it).
            var lipTop = hw - .01f;
            var lipBottom = high.high - .01f;
            if (lipTop - lipBottom > .01f && tile != null)
            {
                var lip = PoolWet("FieldMesh", tile, hw);
                var lipCentre = start + along * (length * .5f) + Vector3.up * ((lipTop + lipBottom) * .5f) - into * .125f;
                var lipSize = Abs(along * length + across * .25f + Vector3.up * (lipTop - lipBottom));
                // surface bevels: both top arrises bullnosed (under the water)
                if (bevelChunk && BevelMaterial(lip))
                    BevelBoxInto(get(high.block, lip), lip, lipCentre, lipSize, FrontRoomsBevel.Edge(Vector3.up, into) | FrontRoomsBevel.Edge(Vector3.up, -into), BevelTileRadius, BevelTileSegments, origin, 1f);
                else get(high.block, lip).Box(lipCentre, lipSize, origin, 1f);
                PoolWetUsed(high.block, lip);
            }
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
        {
            var copeCentre = start + along * (length * .5f) + Vector3.up * (y + .002f) + away * .15f;
            var copeSize = Abs(along * length + across * .30f + Vector3.up * .004f);
            // surface bevels: the plate's nose over the basin a half round
            if (bevelChunk && BevelMaterial(tile))
                BevelBoxInto(get(dry.block, tile), tile, copeCentre, copeSize, FrontRoomsBevel.Edge(Vector3.up, -away) | FrontRoomsBevel.Edge(Vector3.down, -away), BevelCopingRadius, BevelCopingSegments, origin, 1f);
            else get(dry.block, tile).Box(copeCentre, copeSize, origin, 1f);
        }
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
        // Surface bevels: a side whose corners round (side.round) draws its crown piece a round higher and wider, its spandrels
        // down to the round's outer circle (radius + round, on the same radial lines), the intrados narrowed by the rounds with
        // smooth normals, and the round swept along the arc, on from the jambs' rounds at the springing. Collision as before.
        var bevelled = (A.exists && A.round > 0f) || (B.exists && B.round > 0f);
        // Over the opening, from the crown to the top, as a plain piece per side.
        foreach (var side in new[] { A, B })
        {
            if (!side.exists) continue;
            var hi = side.top;
            if (hi - crown < .02f) continue;
            var material = side.pool ? (crown >= side.tileTopY ? side.upper : side.wall) : side.wall;
            var center = start + along * c + Vector3.up * ((crown + hi) * .5f) + across * (WallThickness * .25f * side.sign);
            if (side.round <= 0f)
            {
                solid(side.block, material, center, Abs(along * (radius * 2f) + across * (WallThickness * .5f) + Vector3.up * (hi - crown)), side.pool ? 1f : side.upperRepeat);
                continue;
            }
            var rr = side.round;
            if (hi - crown - rr > 1e-4f)
                get(side.block, material).Box(start + along * c + Vector3.up * ((crown + rr + hi) * .5f) + across * (WallThickness * .25f * side.sign),
                    Abs(along * ((radius + rr) * 2f) + across * (WallThickness * .5f) + Vector3.up * (hi - crown - rr)), origin, side.pool ? 1f : side.upperRepeat);
            poolCollision.Box(center, Abs(along * (radius * 2f) + across * (WallThickness * .5f) + Vector3.up * (hi - crown)), origin, 1f);
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
                if (side.round <= 0f)
                {
                    var p0 = start + along * (c + u0) + Vector3.up * y0 + off;
                    var p1 = start + along * (c + u0) + Vector3.up * crown + off;
                    var p2 = start + along * (c + u1) + Vector3.up * crown + off;
                    var p3 = start + along * (c + u1) + Vector3.up * y1 + off;
                    PoolQuad(get(side.block, material), p0, p1, p2, p3, across * side.sign, origin, side.pool ? 1f : side.upperRepeat);
                    continue;
                }
                var grow = (radius + side.round) / radius;
                var raised = crown + side.round;
                var g0 = start + along * (c + u0 * grow) + Vector3.up * (spring + (y0 - spring) * grow) + off;
                var g3 = start + along * (c + u1 * grow) + Vector3.up * (spring + (y1 - spring) * grow) + off;
                var g1 = start + along * (c + u0 * grow) + Vector3.up * raised + off;
                var g2 = start + along * (c + u1 * grow) + Vector3.up * raised + off;
                PoolQuad(get(side.block, material), g0, g1, g2, g3, across * side.sign, origin, side.pool ? 1f : side.upperRepeat);
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
                if (!bevelled)
                {
                    var q0 = start + along * (c + u0) + Vector3.up * y0 + across * fromA;
                    var q1 = start + along * (c + u0) + Vector3.up * y0 + across * toB;
                    var q2 = start + along * (c + u1) + Vector3.up * y1 + across * toB;
                    var q3 = start + along * (c + u1) + Vector3.up * y1 + across * fromA;
                    PoolQuad(get(under.block, underMat), q0, q1, q2, q3, normal, origin, under.pool ? 1f : under.upperRepeat);
                }
                else
                {
                    // narrowed by each face's round, its normals smooth round the circle (UV0 still by the strip's own normal)
                    var a0 = fromA + (A.exists ? A.round : 0f);
                    var b0 = toB - (B.exists ? B.round : 0f);
                    var n0 = (-along * u0 - Vector3.up * (y0 - spring)).normalized;
                    var n1 = (-along * u1 - Vector3.up * (y1 - spring)).normalized;
                    var m = get(under.block, underMat);
                    var i0 = start + along * (c + u0) + Vector3.up * y0 + across * a0;
                    var i1 = start + along * (c + u0) + Vector3.up * y0 + across * b0;
                    var i2 = start + along * (c + u1) + Vector3.up * y1 + across * b0;
                    var i3 = start + along * (c + u1) + Vector3.up * y1 + across * a0;
                    // a plain-path finish (paint over the tile's top) keeps its flat strips (BevelFaceted: its frame follows the normal)
                    if (BevelFaceted(underMat)) PoolQuad(m, i0, i1, i2, i3, normal, origin, under.pool ? 1f : under.upperRepeat);
                    else
                    {
                        FrontRoomsBevel.QuadNormals(m.vertices, m.normals, m.uvs, m.triangles, i0, i1, i2, i3, n0, n0, n1, n1, normal, origin, under.pool ? 1f : under.upperRepeat);
                        m.planarSnap = true;
                    }
                }
            }
            if (poolCollision != null)
            {
                var yLow = Mathf.Min(y0, y1);
                if (crown - yLow > .01f)
                    poolCollision.Box(start + along * (c + um) + Vector3.up * ((yLow + crown) * .5f) + across * ((fromA + toB) * .5f),
                        Abs(along * (u1 - u0) + across * Mathf.Max(.02f, toB - fromA) + Vector3.up * (crown - yLow)), origin, 1f);
            }
        }
        if (!bevelled) return;
        // the rounds along the arc, per face; a run of steps in one finish at a time (the tile below its top, the upper over it)
        var P = new Vector3[segments + 1];
        var N = new Vector3[segments + 1];
        foreach (var side in new[] { A, B })
        {
            if (!side.exists || side.round <= 0f) continue;
            var off = across * (WallThickness * .5f * side.sign);
            for (var k = 0; k <= segments; k++)
            {
                var u = -radius + 2f * radius * k / segments;
                var y = spring + Mathf.Sqrt(Mathf.Max(0f, radius * radius - u * u));
                P[k] = start + along * (c + u) + Vector3.up * y + off;
                N[k] = (-along * u - Vector3.up * (y - spring)).normalized;
            }
            Material StepMaterial(int k) => side.pool ? ((P[k].y + P[k + 1].y) * .5f >= side.tileTopY ? side.upper : side.wall) : side.wall;
            var first = 0;
            for (var k = 1; k <= segments; k++)
            {
                if (k < segments && StepMaterial(k) == StepMaterial(first)) continue;
                var material = StepMaterial(first);
                if (material != null)
                {
                    var path = new List<Vector3>();
                    var normals = new List<Vector3>();
                    for (var i = first; i <= k; i++) { path.Add(P[i]); normals.Add(N[i]); }
                    var m = get(side.block, material);
                    var flat = BevelFaceted(material);
                    var segs = flat ? BevelFacets(side.round, side.roundSegments) : side.roundSegments;
                    if (FrontRoomsBevel.Sweep(m.vertices, m.normals, m.uvs, m.triangles, path, across * side.sign, normals, side.round, segs, origin, side.pool ? 1f : side.upperRepeat, flat) > 0)
                        m.planarSnap = true;
                }
                first = k;
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

    // Where a valance meant for p (metres along its wall of length run) stands with ValanceClear to the wall's openings
    // (along-wall spans at its foot): p itself, else the nearest place just past an opening, inside p's own 6 m and 0.3 m
    // off the corners; at 2.4 m, else 1.8, else 1.2. False: none fits. Private and as it was (the level-space line's
    // harness binds it by name); PoolValances is the public entry.
    const float ValanceClear = .1f, ValanceFoot = .78f, ValanceInset = .3f;

    static bool ValanceSpot(List<Vector2> openings, float p, float run, out float spot, out float length)
    {
        foreach (var l in new[] { 2.4f, 1.8f, 1.2f })
        {
            var half = l * .5f;
            float lo = Mathf.Max(p - 3f + half, half + .3f), hi = Mathf.Min(p + 3f - half, run - half - .3f);
            bool Free(float c)
            {
                if (c < lo - 1e-4f || c > hi + 1e-4f) return false;
                foreach (var o in openings)
                    if (c + half + ValanceClear > o.x && c - half - ValanceClear < o.y) return false;
                return true;
            }
            length = l;
            spot = p;
            if (Free(p)) return true;
            var best = float.MaxValue;
            foreach (var o in openings)
            {
                var left = o.x - ValanceClear - half;
                var right = o.y + ValanceClear + half;
                if (Free(left) && Mathf.Abs(left - p) < Mathf.Abs(best - p)) best = left;
                if (Free(right) && Mathf.Abs(right - p) < Mathf.Abs(best - p)) best = right;
            }
            if (best < float.MaxValue) { spot = best; return true; }
        }
        spot = p;
        length = 0f;
        return false;
    }

    /// <summary>One valance of a Valance-kit room, where the pool pass hangs it (PoolValances).</summary>
    public struct PoolValance
    {
        /// <summary>0 the room's south long wall (west when it is deeper than wide), 1 the north (east).</summary>
        public int wall;
        /// <summary>Its wall runs along x (the room is at least as wide as deep).</summary>
        public bool alongX;
        /// <summary>Metres along its wall from the room's west or south side: where it was meant (3, then every 6), where it
        /// hangs (its centre; the meant place when left out), and its length (2.4, 1.8 or 1.2; 0 when left out).</summary>
        public float meant, spot, length;
        /// <summary>Its centre in metres from the room's south-west corner, 0.3 m off its wall.</summary>
        public float x, z;
        /// <summary>False: no length fits clear of its wall's openings, so it is left out.</summary>
        public bool hung;
    }

    /// <summary>
    /// The valances of a room whose lamp kit is Valance (none for any other kit), added to <paramref name="into"/> in build
    /// order, where the pool pass hangs them (PoolRoomLamps): along the two longest walls one meant per 6 m from 3 m,
    /// ValanceClear off what its wall keeps clear at the valance's foot 0.78 m under the ceiling (FrontRoomsPoolPlan.OpeningSpanAt:
    /// arch and door openings, clear glass, the EXIT door), slid within its own 6 m, shortened to 1.8 or 1.2 m, else left out
    /// (listed, hung false). Static and plan-only: scene 4's sign rule and a check read the same placement on any seed's plan
    /// (the level-space line's ask, p_recheck4 11-05). One whose lamp is Off (PoolRoomLamps: its cell's, N's for one meant
    /// over N) is listed as hung; the pool pass does not build it. Returns how many hang.
    /// </summary>
    public static int PoolValances(FrontRoomsPoolPlan plan, PoolRoom room, List<PoolValance> into)
    {
        if (plan == null || room == null || room.module == null || into == null) return 0;
        if (PoolRules.SkinOf(room.module).lampKit != PoolLampKit.Valance) return 0;
        var cs = MapGrid.CellSize;
        float w = room.width * cs, d = room.depth * cs;
        var alongX = w >= d;
        var run = alongX ? w : d;
        var cells = alongX ? room.width : room.depth;
        var ceilingY = room.CeilingY;
        var foot = ceilingY - ValanceFoot;
        var openings = new List<Vector2>();
        var hung = 0;
        for (var wall = 0; wall < 2; wall++)
        {
            openings.Clear();
            for (var i = 0; i < cells; i++)
            {
                var inside = alongX ? new GridCoord(room.origin.x + i, wall == 0 ? room.origin.y : room.origin.y + room.depth - 1)
                    : new GridCoord(wall == 0 ? room.origin.x : room.origin.x + room.width - 1, room.origin.y + i);
                var outside = alongX ? new GridCoord(inside.x, inside.y + (wall == 0 ? -1 : 1)) : new GridCoord(inside.x + (wall == 0 ? -1 : 1), inside.y);
                if (plan.OpeningSpanAt(inside, outside, foot, out var f, out var t)) openings.Add(new Vector2(i * cs + f, i * cs + t));
            }
            for (var p = 3f; p < run; p += 6f)
            {
                var v = new PoolValance { wall = wall, alongX = alongX, meant = p };
                v.hung = ValanceSpot(openings, p, run, out v.spot, out v.length);
                v.x = alongX ? v.spot : (wall == 0 ? ValanceInset : w - ValanceInset);
                v.z = alongX ? (wall == 0 ? ValanceInset : d - ValanceInset) : v.spot;
                if (v.hung) hung++;
                into.Add(v);
            }
        }
        return hung;
    }

    // The lamps of one room by its kit (Valance, Rig with underwater niches, StairLights, Niche), and PC_Lockers' globes.
    void PoolRoomLamps(BuiltChunk chunk, MapChunk data, FrontRoomsPoolPlan plan, PoolRoom room, Vector3 roomOrigin, float baseY, float ceilingY)
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
                // Each where PoolValances hangs it, clear of what its wall keeps clear at its foot (FrontRoomsPoolPlan.OpeningSpanAt;
                // the level-space line's route P, 11-05: at the wall's middle it crossed an arch's head in PL_Chain, PL_Turn and
                // PL_Weird on every plan; p_recheck4: in front of PL_Court's clear glass, over the EXIT door's head trim): it
                // slides along its own 6 m, shortens to 1.8 or 1.2 m if it must, else that one is left out. Its height is from
                // room.CeilingY, the ceiling PoolValances takes its foot from. Its lamps are the lamp of the cell it hangs in,
                // except one meant over N (FrontRoomsPoolPlan.FrontCell, the EXIT chain's Steady cell): PL_Chain (EXIT)'s north
                // valance slides west off the EXIT door into the next cell, whose lamp is Auto, and no exit lamp hold reaches a
                // pool cell (reserved while the plan is held), so it keeps N's lamp, fixture and hash.
                var glow = FrontRoomsPoolMaterials.Glow("valance", new Color(1.1f, 1.18f, 1.2f));
                var valances = new List<PoolValance>();
                PoolValances(plan, room, valances);
                foreach (var v in valances)
                {
                    if (!v.hung) continue;
                    float lx = v.x, lz = v.z;
                    float mx = v.alongX ? v.meant : v.x, mz = v.alongX ? v.z : v.meant;
                    if (CellAtLocal(mx, mz) == plan.FrontCell) { lx = mx; lz = mz; }
                    var lamp = LampAtLocal(lx, lz);
                    if (lamp == ModuleLamp.Off) continue;
                    var local = roomOrigin + new Vector3(v.x, room.CeilingY - .72f, v.z);
                    var size = v.alongX ? new Vector3(v.length, .12f, .12f) : new Vector3(.12f, .12f, v.length);
                    var box = PoolLensBox(chunk, "STAND-IN valance " + room.label, local, Quaternion.identity, size, white);
                    var strip = PoolLensBox(chunk, "Valance glow " + room.label, local + Vector3.up * .065f, Quaternion.identity, new Vector3(size.x * .96f, .01f, size.z * .96f), glow);
                    var cell = CellAtLocal(lx, lz);
                    PoolFixture(chunk, cell, local + Vector3.up * .1f, Quaternion.Euler(-90f, 0f, 0f), LightType.Spot, 150f, 100f, 3.06f, 3.6f, new Color(.86f, .96f, 1f), lamp, strip ?? box, new Color(1.1f, 1.18f, 1.2f), tier, 1 + v.wall);
                    if (box != null)
                        PoolFixture(chunk, cell, local + Vector3.down * .1f, Quaternion.Euler(90f, 0f, 0f), LightType.Spot, 170f, 130f, 1.63f, 4.2f, new Color(.86f, .96f, 1f), lamp, box, Color.black, tier, 3 + v.wall);
                }
                break;
            }
            case PoolLampKit.Rig:
            {
                // P1-A (Red 11-04, "all as recommended" on the pool look; zone_lamps 20_design s.3.4): the indirect metal-halide
                // wall box, one a clear bay on both long walls at 3.4 m (PoolWallBoxBays: the spans between the room's beams and
                // soffit), its glass up and its light thrown 30 deg off the wall into the room (110 / 70), so the ceiling is the
                // light and no lamp shows in the water; its lit ceiling is the box's own patch, written the lamp's block
                // (Fixture.glow); a bounce comes down from that ceiling 1.2 m out. Each box's metal-halide white drifts a little
                // (MapHash). P2-A's wet niches on the basin walls (PoolBasinNiches). Either kit missing: the stand-ins below.
                if (FrontRoomsLampKit.Available(FrontRoomsLampKit.Family.WallBox))
                {
                    var alongX = w >= d;
                    var bays = new List<float>();
                    PoolWallBoxBays(room, alongX, bays);
                    var phone = FrontRoomsLampKit.PhoneTier;
                    var mountY = baseY + 3.4f;
                    var throwUp = ceilingY - mountY;
                    for (var side = 0; side < 2; side++)
                    for (var k = 0; k < bays.Count; k++)
                    {
                        var inset = WallThickness * .5f;
                        float x, z;
                        Vector3 outward;
                        if (alongX) { x = bays[k]; z = side == 0 ? inset : d - inset; outward = new Vector3(0f, 0f, side == 0 ? 1f : -1f); }
                        else { x = side == 0 ? inset : w - inset; z = bays[k]; outward = new Vector3(side == 0 ? 1f : -1f, 0f, 0f); }
                        var mount = roomOrigin + new Vector3(x, mountY, z);
                        var lens = FrontRoomsLampKit.BuildWallBox(chunk.root.transform, mount, Quaternion.LookRotation(outward, Vector3.up), "Wall box lens " + room.label, phone, throwUp, out var patch);
                        if (lens == null) continue;
                        var cell = CellAtLocal(x, z);
                        var lamp = LampAtLocal(x, z);
                        var mode = lamp == ModuleLamp.Off ? ModuleLamp.Steady : lamp;
                        // the box's metal-halide white drifts per lamp inside the drive (HidTick), its light out on a deep dip
                        var drift = MapHash.Unit(MapHash.Hash(Cache.Generator.Seed, cell.x * 4 + side, cell.y, 409)) * 2f - 1f;
                        var col = new Color(.9f, .97f, 1f);
                        var aim = Vector3.up * Mathf.Cos(30f * Mathf.Deg2Rad) + outward * Mathf.Sin(30f * Mathf.Deg2Rad);
                        var up = PoolFixture(chunk, cell, mount + outward * .16f + Vector3.up * .22f, Quaternion.LookRotation(aim, outward), LightType.Spot, 110f, 70f, 7f, 11f, col, mode, lens, col * 1.65f, tier, 1 + side);
                        if (up != null) { up.glow = patch; PoolHid(up, drift); }
                        PoolHid(PoolFixture(chunk, cell, mount + outward * 1.2f + Vector3.up * (throwUp - .05f), Quaternion.Euler(90f, 0f, 0f), LightType.Spot, 170f, 130f, 3f, 8f, col, mode, lens, col * 1.65f, tier, 3 + side), drift);
                    }
                    if (!PoolBasinNiches(chunk, room, roomOrigin, new Color(.55f, .92f, 1f), 2.4f, 10f, tier))
                        PoolNiches(chunk, room, roomOrigin, -.6f, 4, new Color(.55f, .92f, 1f), 2.4f, 10f, tier);
                    break;
                }
                // Stand-ins: metal halide brackets on the west and east walls at 3.4 m, three a side (up spot and a bounce), and
                // four underwater niches on each long basin wall at -0.6.
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
                // The dark zone (Q4: the grade follows them): P2-A's warm wet niches, one a 3 m cell along its long walls 0.7 m
                // under the water (PoolBasinNiches); the kit missing: two stand-ins a long wall at -2.0.
                if (!PoolBasinNiches(chunk, room, roomOrigin, new Color(1f, .82f, .62f), 2.4f, 10f, tier))
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

    // A pool wall box's fixture (P1-A) as a metal-halide lamp (LampTick's HidLevel): lit at its drift, its colours kept.
    static void PoolHid(Fixture f, float drift)
    {
        if (f == null) return;
        f.hid = true;
        f.hidLamp = FrontRoomsLampKit.HidLamp.Lit(drift, f.mode == 2);
        f.hidColor = f.light.color;
        f.hidEmission = f.emission;
        f.hidClock = f.clock;
    }

    /// <summary>
    /// P1-A's bays along a pool room's long walls (zone_lamps 20_design s.3.4), metres along the long axis into
    /// <paramref name="into"/>: the clear spans between the room's Kit_Beam and Kit_Soffit stand-ins (their footprints across the
    /// long axis; a beam that runs along it divides nothing), one box at the centre of each span 1.2 m long or more. A room
    /// with neither gets one a 6 m bay from 3 m (the valances' rule). PC_Hall: z 4.375, 9, 15, 21.125.
    /// </summary>
    static void PoolWallBoxBays(PoolRoom room, bool alongX, List<float> into)
    {
        into.Clear();
        var run = (alongX ? room.width : room.depth) * MapGrid.CellSize;
        var cuts = new List<Vector2>();
        var standIns = room.module != null && room.module.pool != null ? room.module.pool.standIns : null;
        if (standIns != null)
            foreach (var s in standIns)
            {
                if (s.kit != "Kit_Beam" && s.kit != "Kit_Soffit") continue;
                if (!s.Footprint(out var hx, out var hz, out _)) continue;
                var turned = Mathf.Abs(Mathf.DeltaAngle(s.yaw, 90f)) < 45f || Mathf.Abs(Mathf.DeltaAngle(s.yaw, 270f)) < 45f;
                var half = alongX ? (turned ? hz : hx) : (turned ? hx : hz);
                if (half > run * .4f) continue;
                var c = alongX ? s.x : s.z;
                cuts.Add(new Vector2(Mathf.Max(0f, c - half), Mathf.Min(run, c + half)));
            }
        if (cuts.Count == 0)
        {
            for (var a = 3f; a < run - .5f; a += 6f) into.Add(a);
            return;
        }
        cuts.Sort((p, q) => p.x.CompareTo(q.x));
        var from = 0f;
        foreach (var cut in cuts)
        {
            if (cut.x - from >= 1.2f) into.Add((from + cut.x) * .5f);
            from = Mathf.Max(from, cut.y);
        }
        if (run - from >= 1.2f) into.Add((from + run) * .5f);
    }

    /// <summary>
    /// P2-A (zone_lamps 20_design s.3.4): one 300 W wet niche a 3 m cell along the basin's long walls, the ring's centre 0.7 m
    /// under the water and facing into the basin, wherever the floor lies at least 0.25 m below it. The long walls are the
    /// sides, across the room's long axis, of Wade cells whose neighbour is not Wade (the deck, a beach) or lies outside the
    /// room (a flooded room's own walls). Each lamp is a map fixture (Steady) whose light FrontRoomsWaterSubmerged takes as a wet
    /// niche: its glow comes back out of the basin with the ripple's caustics and dims and dies with it. False when the kit is
    /// missing (the caller builds the stand-ins).
    /// </summary>
    bool PoolBasinNiches(BuiltChunk chunk, PoolRoom room, Vector3 roomOrigin, Color color, float intensity, float range, int tier)
    {
        if (!FrontRoomsLampKit.Available(FrontRoomsLampKit.Family.Niche)) return false;
        var pool = room.module != null ? room.module.pool : null;
        if (pool == null) return false;
        var cs = MapGrid.CellSize;
        var alongX = room.width >= room.depth;
        var phone = FrontRoomsLampKit.PhoneTier;
        var n = 0;
        for (var j = 0; j < room.depth; j++)
        for (var i = 0; i < room.width; i++)
        {
            var c = pool.CellAt(room.module, i, j);
            if (c.water != CellWater.Wade) continue;
            // a stair cell (an entry's steps) takes the 6.5 in niche of spa steps (ZL_NICHE_S, a 100 W lamp) 0.35 m under the
            // water, judged by its foot's floor; every other deep cell the 300 W one 0.7 m under
            var stair = c.stairRisers > 0;
            var ring = c.waterY - (stair ? .35f : .7f);
            var floor = stair ? c.floorY - c.stairRisers * c.stairRise : c.floorY;
            if (floor > ring - .25f) continue;
            for (var s = 0; s < 2; s++)
            {
                int ni = i, nj = j;
                if (alongX) nj += s == 0 ? -1 : 1;
                else ni += s == 0 ? -1 : 1;
                var inside = ni >= 0 && nj >= 0 && ni < room.width && nj < room.depth;
                if (inside && pool.CellAt(room.module, ni, nj).water == CellWater.Wade) continue;
                float x = (i + .5f) * cs, z = (j + .5f) * cs;
                var wallIn = inside ? 0f : WallThickness * .5f;
                Vector3 inward;
                if (alongX) { z += s == 0 ? -cs * .5f + wallIn : cs * .5f - wallIn; inward = new Vector3(0f, 0f, s == 0 ? 1f : -1f); }
                else { x += s == 0 ? -cs * .5f + wallIn : cs * .5f - wallIn; inward = new Vector3(s == 0 ? 1f : -1f, 0f, 0f); }
                var pos = roomOrigin + new Vector3(x, ring, z);
                var family = stair && FrontRoomsLampKit.Available(FrontRoomsLampKit.Family.NicheSmall) ? FrontRoomsLampKit.Family.NicheSmall : FrontRoomsLampKit.Family.Niche;
                var lens = FrontRoomsLampKit.Build(family, chunk.root.transform, pos, Quaternion.LookRotation(inward, Vector3.up), "Niche lens " + room.label, phone);
                if (lens == null) return n > 0;
                var cell = new GridCoord(room.origin.x + i, room.origin.y + j);
                var small = family == FrontRoomsLampKit.Family.NicheSmall;
                var f = PoolFixture(chunk, cell, pos + inward * (small ? .08f : .12f), Quaternion.LookRotation((inward + Vector3.down * .22f).normalized, Vector3.up), LightType.Spot, 110f, 40f, intensity * (small ? .4f : 1f), range * (small ? .6f : 1f), color, ModuleLamp.Steady, lens, color * 2.2f, tier, 16 + n);
                if (f != null && f.light != null) FrontRoomsWaterSubmerged.RegisterNiche(f.light, f.baseIntensity);
                n++;
            }
        }
        return true;
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

            // under a water the floor's (and a stair's) grout soaks and a beach shows its wet edge
            if (tile != null)
            {
                var floorTile = pc.HasWater ? PoolWet("FieldMesh", tile, pc.waterY) : tile;
                // surface bevels: a flat floor's top edges over a basin's edge round (PoolSlabRound); a stair's noses (PoolStair)
                var roundMask = 0;
                if (bevelChunk)
                {
                    if (PoolSlabRound(cell, pc, new GridCoord(cell.x - 1, cell.y), 0) > 0f) roundMask |= 1;
                    if (PoolSlabRound(cell, pc, new GridCoord(cell.x + 1, cell.y), 1) > 0f) roundMask |= 2;
                    if (PoolSlabRound(cell, pc, new GridCoord(cell.x, cell.y - 1), 2) > 0f) roundMask |= 4;
                    if (PoolSlabRound(cell, pc, new GridCoord(cell.x, cell.y + 1), 3) > 0f) roundMask |= 8;
                }
                PoolFloor(pc, x0, z0, block, get, solid, collision, origin, floorTile, roundMask, bevelChunk && BevelMaterial(floorTile));
                PoolWetUsed(block, floorTile);
            }

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
            try { PoolRoomLamps(chunk, data, plan, room, roomOrigin, baseY, ceilingY); }
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
            var water = float.NaN;   // the highest water of the four cells round it: the column stands in it
            foreach (var c in new[] { cell, new GridCoord(cell.x - 1, cell.y), new GridCoord(cell.x, cell.y - 1), new GridCoord(cell.x - 1, cell.y - 1) })
            {
                var pcc = plan.CellOf(c);
                var u = c.x == cell.x ? 0f : cs;
                var v = c.y == cell.y ? 0f : cs;
                low = Mathf.Min(low, pcc.StepAt(u, v));
                if (pcc.HasWater) water = float.IsNaN(water) ? pcc.waterY : Mathf.Max(water, pcc.waterY);
            }
            if (low == float.MaxValue) low = 0f;
            var width = (data.pillarStyle[i + j * (n + 1)] & MapChunk.ColumnLarge) != 0 ? ModuleUnits.ColumnLarge : ModuleUnits.ColumnSmall;
            var top = room.CeilingY;
            var rel = MapGrid.CeilingHeight(room.zone.height);
            if (tile != null)
            {
                var colBlock = blockOf(i, j, rel);
                var colTile = PoolWet("FieldMesh", tile, water);
                var colCentre = new Vector3(i * cs, (low - .02f + top) * .5f, j * cs);
                var colSize = new Vector3(width, top - low + .02f, width);
                // surface bevels: the pillar's four arrises bullnosed (render; collision as Solid writes it)
                if (bevelChunk && BevelMaterial(colTile))
                {
                    BevelBoxInto(get(colBlock, colTile), colTile, colCentre, colSize, BevelColumnEdges, BevelTileRadius, BevelTileSegments, origin, 1f);
                    collision.Box(colCentre, colSize, origin, 1f);
                }
                else solid(colBlock, colTile, colCentre, colSize, 1f);
                PoolWetUsed(colBlock, colTile);
            }
        }

        // The tile's renderers in the blocks a wet instance draws in keep the bounds (and so the probe) they had.
        PoolEvenWetBounds(get, collision);

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
    // Surface bevels: roundMask (1 west, 2 east, 4 south, 8 north) bullnoses a flat slab's top edges over a basin's edge (its
    // collision as Solid writes it); bevel rounds a stair's noses under a swept nosing.
    static void PoolFloor(ModuleCell c, float x0, float z0, int block, BuilderFn get, SolidFn solid, MeshBuilder collision, Vector3 origin, Material tile,
        int roundMask = 0, bool bevel = false)
    {
        var cs = MapGrid.CellSize;
        if (c.connector == CellConnector.Stair && c.stairRisers > 0 && c.stairRun > 1e-4f)
        {
            PoolStair(c, x0, z0, block, get, collision, origin, tile, bevel);
            return;
        }
        if (Mathf.Abs(c.slopeX) < 1e-5f && Mathf.Abs(c.slopeZ) < 1e-5f)
        {
            var centre = new Vector3(x0 + cs * .5f, c.floorY - ModuleUnits.FloorSlab * .5f, z0 + cs * .5f);
            var size = new Vector3(cs, ModuleUnits.FloorSlab, cs);
            if (roundMask != 0)
            {
                var edges = 0;
                if ((roundMask & 1) != 0) edges |= FrontRoomsBevel.Edge(Vector3.up, Vector3.left);
                if ((roundMask & 2) != 0) edges |= FrontRoomsBevel.Edge(Vector3.up, Vector3.right);
                if ((roundMask & 4) != 0) edges |= FrontRoomsBevel.Edge(Vector3.up, Vector3.back);
                if ((roundMask & 8) != 0) edges |= FrontRoomsBevel.Edge(Vector3.up, Vector3.forward);
                BevelBoxInto(get(block, tile), tile, centre, size, edges, BevelTileRadius, BevelTileSegments, origin, 1f);
                if (collision != null) collision.Box(centre, size, origin, 1f);
                return;
            }
            solid(block, tile, centre, size, 1f);
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
    // Surface bevels (bevel): each nose bullnosed (the box ending at it), the nosing swept round it (PoolNosing).
    static void PoolStair(ModuleCell c, float x0, float z0, int block, BuilderFn get, MeshBuilder collision, Vector3 origin, Material tile, bool bevel = false)
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
        // a nose rounds only where the step is deep and high enough for the bullnose and the nosing's 50 / 25 mm
        var rounds = bevel && c.stairRise > .03f && c.stairRun > .06f;
        void Tread(float s0, float s1, float top, bool collide, bool atNose)
        {
            if (s1 - s0 < .005f) return;
            var centre = E + S * ((s0 + s1) * .5f) + W * (cs * .5f) + Vector3.up * ((top + bottom) * .5f);
            var size = Abs(S * (s1 - s0) + W * cs + Vector3.up * (top - bottom));
            if (rounds && atNose) BevelBoxInto(get(block, tile), tile, centre, size, FrontRoomsBevel.Edge(Vector3.up, S), BevelTileRadius, BevelTileSegments, origin, 1f);
            else get(block, tile).Box(centre, size, origin, 1f);
            if (collide && collision != null)
                collision.Box(E + S * ((s0 + s1) * .5f) + W * (cs * .5f) + Vector3.up * (top - ModuleUnits.FloorSlab * .5f), Abs(S * (s1 - s0) + W * cs + Vector3.up * ModuleUnits.FloorSlab), origin, 1f);
        }
        // the entry flat ends at the first nose, tread k at nose k; the last tread runs on into the final flat
        Tread(0f, c.stairFrom, c.floorY, true, n > 0);
        for (var k = 1; k <= n; k++)
            Tread(c.stairFrom + (k - 1) * c.stairRun, c.stairFrom + k * c.stairRun, c.floorY - k * c.stairRise, false, k < n);
        var end = c.stairFrom + n * c.stairRun;
        Tread(end, cs, c.floorY - n * c.stairRise, true, false);
        // The nosing on every step: at the top of each riser (s = stairFrom + k * run), the higher tread's edge.
        if (nose != null)
            for (var k = 0; k < n; k++)
            {
                var s = c.stairFrom + k * c.stairRun;
                var y = c.floorY - k * c.stairRise;
                // a nose with no box ending at it (the flight starts at the cell's edge) keeps today's nosing
                if (rounds && (k > 0 || c.stairFrom >= .005f))
                {
                    PoolNosing(get(block, nose), E, S, W, s, y, cs, origin);
                    continue;
                }
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

    // Surface bevels: the dark nosing round a bullnosed nose at s (along S from E) and height y, across the flight's width:
    // 2 mm proud, 50 mm back on the tread, round the bullnose, 25 mm down the riser, as a swept profile; its two thin ends and
    // its sides closed. The profile's tread half (to the round's 45 degree point) carries the tread's normal and its riser half
    // the riser's (surface_bevels_fix: a normal between them faced neither lamp, FrontRoomsBevel), two ribbons sharing that point.
    static void PoolNosing(MeshBuilder m, Vector3 E, Vector3 S, Vector3 W, float s, float y, float width, Vector3 origin)
    {
        if (m == null) return;
        const float proud = .002f, onTread = .05f, downRiser = .025f;
        var r = BevelTileRadius;
        var seg = BevelTileSegments;
        var outerAt0 = new List<Vector3>();
        var outerAt1 = new List<Vector3>();
        var inner = new List<Vector3>();
        var outer = new List<Vector3>();
        Vector3 P(float ps, float py, float w) => E + S * ps + Vector3.up * py + W * w;
        void Add(float os, float oy, float iS, float iy)
        {
            outerAt0.Add(P(os, oy, 0f));
            outerAt1.Add(P(os, oy, width));
            outer.Add(P(os, oy, 0f));
            inner.Add(P(iS, iy, 0f));
        }
        List<Vector3> Same(Vector3 n, int count)
        {
            var list = new List<Vector3>(count);
            for (var i = 0; i < count; i++) list.Add(n);
            return list;
        }
        Add(s - onTread, y + proud, s - onTread, y);
        for (var i = 0; i <= seg; i++)
        {
            var a = Mathf.PI * .5f * (1f - (float)i / seg);   // from the top (90 degrees) to the riser (0)
            float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            Add(s - r + ca * (r + proud), y - r + sa * (r + proud), s - r + ca * r, y - r + sa * r);
        }
        Add(s + proud, y - downRiser, s, y - downRiser);
        var mid = 1 + seg / 2;
        var rest = outer.Count - mid;
        FrontRoomsBevel.Ribbon(m.vertices, m.normals, m.uvs, m.triangles, outerAt0.GetRange(0, mid + 1), outerAt1.GetRange(0, mid + 1), Same(Vector3.up, mid + 1), origin, 1f);
        FrontRoomsBevel.Ribbon(m.vertices, m.normals, m.uvs, m.triangles, outerAt0.GetRange(mid, rest), outerAt1.GetRange(mid, rest), Same(S, rest), origin, 1f);
        var last = outer.Count - 1;
        // its back edge on the tread and its foot on the riser
        FrontRoomsBevel.Quad(m.vertices, m.normals, m.uvs, m.triangles, inner[0], outer[0], outer[0] + W * width, inner[0] + W * width, -S, origin, 1f);
        FrontRoomsBevel.Quad(m.vertices, m.normals, m.uvs, m.triangles, inner[last], outer[last], outer[last] + W * width, inner[last] + W * width, Vector3.down, origin, 1f);
        // its two sides, across the flight's ends
        for (var i = 0; i < last; i++)
        {
            FrontRoomsBevel.Quad(m.vertices, m.normals, m.uvs, m.triangles, inner[i], outer[i], outer[i + 1], inner[i + 1], -W, origin, 1f);
            FrontRoomsBevel.Quad(m.vertices, m.normals, m.uvs, m.triangles, inner[i] + W * width, outer[i] + W * width, outer[i + 1] + W * width, inner[i + 1] + W * width, W, origin, 1f);
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
        for (var i = 0; i < skin.standIns.Length; i++)
        {
            // A kit an opening of the plan crosses stays out (FrontRoomsPoolPlan.KitLeftOut, with a plan note).
            if (plan.KitLeftOut(room, i)) continue;
            var s = skin.standIns[i];
            var local = roomOrigin + new Vector3(s.x, 0f, s.z);
            // The floor 5 cm behind the kit's point: a kit standing on a basin edge (the ladders at x 3.0) takes the deck
            // it stands on, not the basin floor the edge's far cell would give.
            var back = Quaternion.Euler(0f, s.yaw, 0f) * Vector3.back * .05f;
            local.y = s.fromBase ? skin.baseY + s.y : floorAt(local + back) + s.y;
            FrontRoomsPoolStandIns.Build(s.kit, holder.transform, local, s.yaw, s.length, s.collider ? collide : null, floorAt, chunk.meshes);
        }
    }
}
