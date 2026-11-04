using System;
using System.Collections.Generic;

namespace FrontRooms.Map
{
    // Pool family (structures scene, RULES 5b; s67_pool DESIGN sections 0, 2): the generator and cache halves of a pool
    // plan. With no plan every field here is null and every hook in FrontRoomsMap.cs reads null first: the map is byte for
    // byte today's. A plan is set only through FrontRoomsMapCache.SetPool, and only a map whose profile has poolFamily on
    // asks for one (FrontRoomsMapWorld.SetPool): today that is the structures scene's own profile copy alone.

    public sealed partial class FrontRoomsMapGenerator
    {
        /// <summary>Synthetic zone ids of a pool plan start here (no site chunk is ever this far out).</summary>
        public const int PoolZoneBase = 1 << 24;

        // The plan's cells and their synthetic zones, its chunk-border edges, and the ids it put into `zones`.
        Dictionary<GridCoord, ZoneInfo> zoneOverride;
        IReadOnlyDictionary<long, EdgeKind> poolBorders;
        readonly List<GridCoord> poolZoneIds = new List<GridCoord>();

        /// <summary>A pool plan's zones are in force (the cache holds a plan).</summary>
        public bool PoolActive => zoneOverride != null;

        /// <summary>
        /// Force a pool plan's zones and border edges (FrontRoomsMapCache.SetPool only). Every synthetic zone is also put into
        /// the generator's own zones under its id, so Zone(id) returns it; the Tall-exit cache is cleared, as zones moved.
        /// </summary>
        internal void SetPoolZones(Dictionary<GridCoord, ZoneInfo> cells, IList<ZoneInfo> list, IReadOnlyDictionary<long, EdgeKind> borders)
        {
            ClearPoolZones();
            if (cells == null) return;
            zoneOverride = cells;
            poolBorders = borders;
            if (list != null)
                foreach (var z in list)
                {
                    zones[z.id] = z;
                    poolZoneIds.Add(z.id);
                }
            tallExits.Clear();
        }

        /// <summary>Forget a pool plan's zones (null again, so PoolActive is false and the hooks read null).</summary>
        internal void ClearPoolZones()
        {
            foreach (var id in poolZoneIds) zones.Remove(id);
            if (poolZoneIds.Count > 0 || zoneOverride != null) tallExits.Clear();
            poolZoneIds.Clear();
            zoneOverride = null;
            poolBorders = null;
        }
    }

    public sealed partial class FrontRoomsMapCache
    {
        /// <summary>The pool plan every Get applies last, or null (every map but the structures scene's pool route).</summary>
        public FrontRoomsPoolPlan Pool { get; private set; }

        /// <summary>A pool plan is in force.</summary>
        public bool HasPool => Pool != null;

        /// <summary>
        /// Hold a built plan (null or not built clears; the planner sets each candidate here before it accepts one, the
        /// world holds only a Ready plan): its zones and border edges go to the generator, and every chunk it touches is
        /// dropped so the next Get builds it with the plan. Begins with ClearPool, so setting the same plan twice is harmless.
        /// </summary>
        public void SetPool(FrontRoomsPoolPlan plan)
        {
            ClearPool();
            if (plan == null || !plan.Built) return;
            Pool = plan;
            Generator.SetPoolZones(plan.ZoneCells, plan.ZoneList, plan.Borders);
            foreach (var coord in plan.Touched) chunks.Remove(coord);
        }

        /// <summary>Forget the plan: the generator's zones go back to the seed's, and the chunks it touched are dropped.</summary>
        public void ClearPool()
        {
            var plan = Pool;
            if (plan == null) return;
            Pool = null;
            Generator.ClearPoolZones();
            foreach (var coord in plan.Touched) chunks.Remove(coord);
        }

        /// <summary>A cell of a pool theme (club, level or dark zone): false for every cell with no plan.</summary>
        public bool IsPoolCell(GridCoord cell) => Pool != null && PoolRules.IsPool(Generator.ZoneOf(cell).theme);

        /// <summary>The plan's record on the edge a-b (arch width, style, crown, trim), from either side.</summary>
        public bool TryPoolRecord(GridCoord a, GridCoord b, out ModuleEdgeRecord record)
        {
            record = default;
            return Pool != null && Pool.TryRecord(a, b, out record);
        }

        /// <summary>The plan's cell (water, floor, connector), or a dry cell at 0 with no plan or outside it.</summary>
        public ModuleCell PoolCellOf(GridCoord cell) => Pool != null ? Pool.CellOf(cell) : default;

        // ArchOverride's pool answer: an authored arch width on the edge, centred, as the Run! arch is.
        bool PoolArch(GridCoord a, GridCoord b, out float width, out float center)
        {
            width = center = 0f;
            if (!TryPoolRecord(a, b, out var r) || r.archWidth <= 0f) return false;
            width = r.archWidth;
            center = MapGrid.CellSize * .5f;
            return true;
        }
    }
}
