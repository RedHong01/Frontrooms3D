namespace FrontRooms.Map
{
    /// <summary>
    /// The span of a generated doorless doorway (an Arch edge), from the seed and the edge alone: the hash
    /// FrontRoomsMapWorld.ArchOpening (FrontRoomsMapWorld.cs:2411-2417) builds with, moved here as plain arithmetic
    /// (no UnityEngine, no Mathf) so the data side can ask it before anything is built (A26 step 0: the floor-1 start
    /// group's sight-line ray test, notes/floor1_start_group_plan.md 5.1). The world does not call it yet (step 1 switches
    /// ArchOpening to it); until then the two copies must agree bit for bit, which the feasibility batch checks.
    /// </summary>
    public static class MapArch
    {
        /// <summary>The span hash's salt (MapHash uses 11-73; the map 89, 97, 211, 223, 307).</summary>
        public const int Salt = 97;

        /// <summary>
        /// Width and centre (metres from the edge's start) of the doorway on an edge. <paramref name="a"/> is the
        /// west or south cell; <paramref name="alongX"/> is true for its north edge (the edge runs along X), false for
        /// its east edge. The edge starts at the cell corner with the smaller coordinate (CrossingPoint, MW:2447-2460).
        /// </summary>
        public static void Opening(int seed, GridCoord a, bool alongX, out float width, out float center)
        {
            var edgeHash = MapHash.Hash(seed, a.x * 2 + (alongX ? 1 : 0), a.y, Salt);
            width = ModuleUnits.ArchMinWidth + ModuleUnits.ArchWidthSpread * MapHash.Unit(edgeHash);
            var margin = ModuleUnits.ArchCornerMargin + width * .5f;
            var far = MapGrid.CellSize - margin;
            var t = MapHash.Unit(MapHash.Hash((int)edgeHash, 3, 7, 11));
            // Mathf.Lerp(margin, far, t) is margin + (far - margin) * Clamp01(t); MapHash.Unit is in [0, 1), so no clamp.
            center = margin + (far - margin) * t;
        }
    }
}
