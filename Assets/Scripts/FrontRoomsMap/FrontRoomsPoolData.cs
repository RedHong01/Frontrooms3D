using System;
using System.Collections.Generic;

namespace FrontRooms.Map
{
    // s67_pool (R4a-1, the pool family greybox; contracts C1 to C12 of research/bathroom_rooms/50_modules_and_contracts.md),
    // carried into the structures scene (scene 4, RULES 5b; FrontRoomsMapWork/integration/structures/PLAN.md 2.7).
    // Plain data: the water class, floor height and connector of each cell of a pool module, the module's skin and lamp
    // kit, and its stand-ins. Every value below is only ever appended. Every other module's RoomModuleData.pool is null and
    // never read (RoomModuleData.IsPool is false for every theme but the three pool themes); read it through PoolRules.SkinOf.
    // All heights are metres from the map's y 0 plane unless a field says otherwise.

    /// <summary>C2: what a cell's floor is to walk on. Dry and Wet are dry walking (Wet: wet tile); Ankle is ankle-deep water; Wade is a basin.</summary>
    public enum CellWater : byte { Dry, Wet, Ankle, Wade }

    /// <summary>C9 and the terrain grammar (research/level_space/40 sec 6.1): what joins a cell's floor to its neighbours'. Append later: Spiral, Ramp, Bridge.</summary>
    public enum CellConnector : byte { None, Stair, Void }

    /// <summary>C5, C9, C10: what a pool module authors on an edge beyond its ModuleEdge. Append later: Guard, Coping.</summary>
    public enum PoolEdgeStyle : byte { None, RoundArch, ObsWindow, ClearGlass, Gate }

    /// <summary>C7: a pool room's ceiling. Grid and GridDeadMissing are the map's own Level 0 ceiling and troffers.</summary>
    public enum PoolCeiling : byte { Tile, Grid, GridDeadMissing, Plain, Rig }

    /// <summary>C7: a pool room's lamps. Troffer is the map's own fixture; the others are the pool family's own.</summary>
    public enum PoolLampKit : byte { None, Troffer, Flush, Valance, Strip, Rig, StairLights, Niche, Windows }

    /// <summary>What a pool room's walls are above its tile line (ModulePool.tileTop).</summary>
    public enum PoolUpper : byte { None, Paint, Block }

    /// <summary>
    /// One cell of a pool module (C2, C3, C4, C9). floorY is the floor at the cell's centre (a flat or sloped cell) or on
    /// its entry side (a Stair cell); slopeX and slopeZ are the rise per metre along +X and +Z. A Stair cell descends in
    /// stairDir (0 +Z, 1 +X, 2 -Z, 3 -X) from stairFrom metres past its entry side: stairRisers risers of stairRise, each
    /// stairRun deep, then flat at floorY - stairRisers * stairRise. waterY is the water surface (Ankle and Wade only).
    /// </summary>
    [Serializable]
    public struct ModuleCell
    {
        public CellWater water;
        public float floorY, slopeX, slopeZ, waterY;
        public bool beach;
        public CellConnector connector;
        public byte stairDir, stairRisers;
        public float stairFrom, stairRise, stairRun;

        /// <summary>True for an Ankle or Wade cell: it has a water surface at waterY.</summary>
        public bool HasWater => water == CellWater.Ankle || water == CellWater.Wade;

        /// <summary>The cell's descent along its stair, metres from the entry side, for a point (u, v) metres from the cell's south-west corner.</summary>
        public float StairDistance(float u, float v)
        {
            var c = MapGrid.CellSize;
            switch (stairDir & 3)
            {
                case 0: return v;
                case 1: return u;
                case 2: return c - v;
                default: return c - u;
            }
        }

        /// <summary>
        /// The walking floor at (u, v) metres from the cell's south-west corner (0..3): the slope plane, or for a Stair the
        /// line from the nosing of its first riser to the nosing of its last (what a ramp collider follows).
        /// </summary>
        public float FloorAt(float u, float v)
        {
            if (connector == CellConnector.Stair && stairRisers > 0)
            {
                var span = stairRisers * stairRun;
                var t = span > 1e-4f ? (StairDistance(u, v) - stairFrom) / span : 1f;
                t = t < 0f ? 0f : t > 1f ? 1f : t;
                return floorY - stairRise * stairRisers * t;
            }
            var h = MapGrid.CellSize * .5f;
            return floorY + slopeX * (u - h) + slopeZ * (v - h);
        }

        /// <summary>The tread a foot stands on at (u, v): the stepped floor of a Stair cell (FloorAt for any other cell).</summary>
        public float StepAt(float u, float v)
        {
            if (connector != CellConnector.Stair || stairRisers == 0) return FloorAt(u, v);
            var s = StairDistance(u, v) - stairFrom;
            if (s < 0f) return floorY;
            var k = stairRun > 1e-4f ? (int)Math.Floor(s / stairRun) + 1 : stairRisers;
            if (k > stairRisers) k = stairRisers;
            return floorY - stairRise * k;
        }

        /// <summary>The lowest floor anywhere in the cell.</summary>
        public float LowestFloor
        {
            get
            {
                var c = MapGrid.CellSize;
                if (connector == CellConnector.Stair) return floorY - stairRise * stairRisers;
                return Math.Min(Math.Min(FloorAt(0f, 0f), FloorAt(c, 0f)), Math.Min(FloorAt(0f, c), FloorAt(c, c)));
            }
        }

        /// <summary>The highest floor anywhere in the cell.</summary>
        public float HighestFloor
        {
            get
            {
                var c = MapGrid.CellSize;
                if (connector == CellConnector.Stair) return floorY;
                return Math.Max(Math.Max(FloorAt(0f, 0f), FloorAt(c, 0f)), Math.Max(FloorAt(0f, c), FloorAt(c, c)));
            }
        }

        /// <summary>This cell turned clockwise once (seen from above), as RoomModuleData.Rotated turns cells.</summary>
        public ModuleCell RotatedOnce()
        {
            var r = this;
            r.slopeX = slopeZ;
            r.slopeZ = -slopeX;
            r.stairDir = (byte)((stairDir + 1) & 3);
            return r;
        }
    }

    /// <summary>
    /// A greybox stand-in (s67_pool): a pool kit that does not exist yet, built from primitives by FrontRoomsPoolStandIns
    /// and named STAND-IN in the hierarchy so the rendering line's real kit replaces it by one table. (x, z) metres from the module's
    /// south-west corner; y above the floor at (x, z), or above the module's baseY with fromBase; yaw in degrees; a
    /// kit's long axis is its local X (yaw 90 runs it along Z, its front faces +X); length 0 = the kit's default.
    /// </summary>
    [Serializable]
    public struct ModuleStandIn
    {
        public string kit;
        public float x, z, y, yaw, length;
        public bool collider, fromBase;

        /// <summary>
        /// The kit's box as FrontRoomsPoolStandIns builds it (keep the two in step): half its size along its local X and Z
        /// round its point, and its lowest point under y, in metres. False for the kits flat on the floor (drains, depth
        /// marks, court lines) and the fire stair's rail and gate, which never stand in an opening. FrontRoomsPoolPlan reads
        /// it so that an arch it opens does not land behind a kit standing against that wall.
        /// </summary>
        public bool Footprint(out float halfX, out float halfZ, out float bottom)
        {
            var l = length;
            halfZ = .05f;
            bottom = 0f;
            switch (kit)
            {
                case "Kit_TowelDesk": halfX = (l > 0f ? l : 3f) * .5f + .05f; halfZ = .36f; return true;
                case "Kit_SignBlank": halfX = (l > 0f ? l : 1.2f) * .5f; halfZ = .03f; bottom = -.2f; return true;
                case "Kit_LockerBank": halfX = (l > 0f ? l : 3.4f) * .5f; halfZ = .22f; return true;
                case "Kit_LockerBench": halfX = (l > 0f ? l : 3.4f) * .5f; halfZ = .18f; return true;
                case "Kit_ShowerSet": halfX = .08f; halfZ = .3f; bottom = 1f; return true;
                case "Kit_GlassBlockHalfWall": halfX = (l > 0f ? l : 2.9f) * .5f + .01f; halfZ = .07f; return true;
                case "Kit_GlassBlockWall": halfX = (l > 0f ? l : 3.4f) * .5f; halfZ = .03f; return true;
                case "Kit_PoolLadder": halfX = .29f; halfZ = .39f; return true;
                case "Kit_GuardChair": halfX = .34f; halfZ = .33f; return true;
                case "Kit_RingBuoy": halfX = .39f; halfZ = .11f; bottom = -.39f; return true;
                case "Kit_RulesBoard": halfX = .6f; halfZ = .03f; bottom = -.45f; return true;
                case "Kit_PaceClock": halfX = .44f; halfZ = .05f; bottom = -.44f; return true;
                case "Kit_ObsWindow": halfX = .5f; halfZ = .04f; bottom = -.19f; return true;
                case "Kit_Soffit": halfX = (l > 0f ? l : 15f) * .5f; halfZ = 1.5f; return true;
                case "Kit_Beam": halfX = (l > 0f ? l : 15f) * .5f; halfZ = .25f; return true;
                case "Kit_PipeRun": halfX = (l > 0f ? l : 6f) * .5f; halfZ = .08f; bottom = -.07f; return true;
                case "Kit_Oculus": halfX = .78f; halfZ = .05f; bottom = -.78f; return true;
                // The tube leaves the wall behind it and bends down into the water ahead; its whole run counts as its depth.
                case "Kit_SlideTube": halfX = .5f; halfZ = 3.7f; bottom = -2.1f; return true;
                case "Kit_FloorDrain": case "Kit_MainDrainGrate": case "Kit_DepthMark": case "Kit_CourtLines":
                case "Kit_PipeRail": case "Kit_GateBars":
                    halfX = 0f;
                    return false;
                default: halfX = .25f; halfZ = .25f; return true;
            }
        }
    }

    /// <summary>
    /// What a pool module (theme PoolClub, PoolLevel or PoolDark) adds to RoomModuleData: per cell water, floors and
    /// connectors (cells, i + j * width; empty = the theme's default for every cell), the module's base plane and
    /// water line, ceiling, lamp kit, wall skin and stand-ins. Empty on every other module.
    /// </summary>
    [Serializable]
    public sealed class ModulePool
    {
        /// <summary>Per cell, i + j * width. Empty: every cell is the theme's default (PoolRules.DefaultCell).</summary>
        public ModuleCell[] cells = new ModuleCell[0];
        /// <summary>The module's base plane (0, or -2.52 in the dark zone): its ceiling is baseY + CeilingHeight(height).</summary>
        public float baseY;
        /// <summary>The module's water line for Wade cells the data does not give one (informational once generated).</summary>
        public float waterY;
        public PoolCeiling ceiling;
        public PoolLampKit lampKit;
        public PoolUpper upper;
        /// <summary>The tile line, metres above baseY: tile below it, the upper finish above. At or over the ceiling (99): tile to the ceiling; 0: no tile above the base plane; may be negative.</summary>
        public float tileTop = TileToCeiling;
        /// <summary>The aqua course: from bandLow to bandHigh (bandHigh 0: none), above the cell's floor with bandOnFloor (a waterline), else above baseY.</summary>
        public float bandLow, bandHigh;
        public bool bandOnFloor;
        /// <summary>At most this many of this module in one plan (0: no limit).</summary>
        public int maxPerRun;
        public ModuleStandIn[] standIns = new ModuleStandIn[0];

        /// <summary>tileTop's "tile to the ceiling".</summary>
        public const float TileToCeiling = 99f;

        public ModulePool Clone()
        {
            var c = (ModulePool)MemberwiseClone();
            c.cells = (ModuleCell[])cells?.Clone();
            c.standIns = (ModuleStandIn[])standIns?.Clone();
            return c;
        }

        /// <summary>Empty arrays stay empty; a non-empty cells array is sized to w * d by index (Resized keeps cells by (i, j)).</summary>
        public void Normalize(int w, int d)
        {
            if (cells == null) cells = new ModuleCell[0];
            if (cells.Length > 0 && cells.Length != w * d)
            {
                var n = new ModuleCell[Math.Max(0, w * d)];
                for (var k = 0; k < n.Length && k < cells.Length; k++) n[k] = cells[k];
                cells = n;
            }
            if (standIns == null) standIns = new ModuleStandIn[0];
        }

        /// <summary>After RoomModuleData.Resize: the cells by (i, j) from the module before (new cells take the theme's default).</summary>
        public void Resized(RoomModuleData old, RoomModuleData now)
        {
            var before = old != null ? old.pool : null;
            if (before == null || before.cells == null || before.cells.Length == 0 || before.cells.Length != old.width * old.depth)
            {
                if (cells != null && cells.Length > 0 && cells.Length != now.width * now.depth) cells = new ModuleCell[0];
                return;
            }
            var n = new ModuleCell[now.width * now.depth];
            for (var j = 0; j < now.depth; j++)
            for (var i = 0; i < now.width; i++)
                n[i + j * now.width] = i < old.width && j < old.depth ? before.cells[i + j * old.width] : PoolRules.DefaultCell(now.theme);
            cells = n;
            var keep = new List<ModuleStandIn>();
            foreach (var s in standIns ?? new ModuleStandIn[0])
                if (s.x >= 0f && s.z >= 0f && s.x <= now.WidthMetres && s.z <= now.DepthMetres) keep.Add(s);
            standIns = keep.ToArray();
        }

        /// <summary>This pool turned clockwise once, for a module oldWidth x oldDepth cells (widthMetres = oldWidth * 3) before the turn.</summary>
        public ModulePool RotatedOnce(int oldWidth, int oldDepth, float widthMetres)
        {
            var r = Clone();
            if (cells != null && cells.Length == oldWidth * oldDepth && cells.Length > 0)
            {
                // As RoomModuleData.RotatedOnce: old cell (i, j) -> new (j, oldWidth - 1 - i); the new width is oldDepth.
                r.cells = new ModuleCell[cells.Length];
                for (var j = 0; j < oldDepth; j++)
                for (var i = 0; i < oldWidth; i++)
                {
                    int ni = j, nj = oldWidth - 1 - i;
                    r.cells[ni + nj * oldDepth] = cells[i + j * oldWidth].RotatedOnce();
                }
            }
            if (standIns != null)
            {
                r.standIns = new ModuleStandIn[standIns.Length];
                for (var k = 0; k < standIns.Length; k++)
                {
                    var s = standIns[k];
                    var t = s;
                    t.x = s.z;
                    t.z = widthMetres - s.x;
                    t.yaw = (s.yaw + 90f) % 360f;
                    r.standIns[k] = t;
                }
            }
            return r;
        }

        /// <summary>Cell (i, j) of module m: its own, or the theme's default when the module has no cells.</summary>
        public ModuleCell CellAt(RoomModuleData m, int i, int j)
        {
            if (cells != null && m != null && cells.Length == m.width * m.depth && cells.Length > 0 && i >= 0 && j >= 0 && i < m.width && j < m.depth)
                return cells[i + j * m.width];
            return PoolRules.DefaultCell(m != null ? m.theme : ZoneTheme.PoolLevel);
        }

        /// <summary>The walking floor at (x, z) metres from module m's south-west corner.</summary>
        public float FloorAtMetres(RoomModuleData m, float x, float z)
        {
            var cs = MapGrid.CellSize;
            int i = (int)Math.Floor(x / cs), j = (int)Math.Floor(z / cs);
            var w = m != null ? m.width : 1;
            var d = m != null ? m.depth : 1;
            i = i < 0 ? 0 : i >= w ? w - 1 : i;
            j = j < 0 ? 0 : j >= d ? d - 1 : j;
            return CellAt(m, i, j).FloorAt(x - i * cs, z - j * cs);
        }

        /// <summary>
        /// The stamp's write (RoomModuleStamp.Apply, module m at chunk-local (x0, y0)): every cell of the footprint into
        /// chunk.pool (made on the first write, every other cell its zone theme's default), and every edge record whose
        /// two cells lie in this chunk into chunk.poolEdges (by MapGrid.EdgeId). Chunk borders stay the map's.
        /// </summary>
        public void WriteInto(FrontRoomsMapGenerator g, MapChunk chunk, RoomModuleData m, int x0, int y0)
        {
            if (g == null || chunk == null || m == null) return;
            const int n = MapGrid.ChunkCells;
            if (chunk.pool == null || chunk.pool.Length != MapGrid.CellsPerChunk)
            {
                chunk.pool = new ModuleCell[MapGrid.CellsPerChunk];
                for (var j = 0; j < n; j++)
                for (var i = 0; i < n; i++)
                    chunk.pool[MapGrid.LocalIndex(i, j)] = PoolRules.DefaultCell(g.ZoneOf(chunk.Cell(i, j)).theme);
            }
            for (var j = 0; j < m.depth; j++)
            for (var i = 0; i < m.width; i++)
            {
                int ci = x0 + i, cj = y0 + j;
                if (ci < 0 || cj < 0 || ci >= n || cj >= n) continue;
                chunk.pool[MapGrid.LocalIndex(ci, cj)] = CellAt(m, i, j);
            }
            if (chunk.poolEdges == null) chunk.poolEdges = new Dictionary<long, ModuleEdgeRecord>();
            if (m.edgeRecords == null) return;
            foreach (var r in m.edgeRecords)
            {
                if (!PoolRules.RecordCells(m, r, chunk.Origin.x + x0, chunk.Origin.y + y0, out var a, out var b)) continue;
                var la = new GridCoord(a.x - chunk.Origin.x, a.y - chunk.Origin.y);
                var lb = new GridCoord(b.x - chunk.Origin.x, b.y - chunk.Origin.y);
                if (la.x < 0 || la.y < 0 || la.x >= n || la.y >= n || lb.x < 0 || lb.y < 0 || lb.x >= n || lb.y >= n) continue;
                chunk.poolEdges[MapGrid.EdgeId(a, b)] = r;
            }
        }

        /// <summary>C12 (the module's part): cells, water classes, depths, arches, walk-outs and stand-in kits.</summary>
        public void Validate(RoomModuleData m, List<string> errors, List<string> warnings)
        {
            if (m == null) return;
            var count = m.width * m.depth;
            if (cells == null || (cells.Length != 0 && cells.Length != count))
            {
                errors.Add("Pool: " + (cells == null ? 0 : cells.Length) + " cell record(s) for a " + m.width + " x " + m.depth + " room (0 or " + count + ").");
                return;
            }
            var ceilingRel = MapGrid.CeilingHeight(m.height);
            for (var j = 0; j < m.depth; j++)
            for (var i = 0; i < m.width; i++)
            {
                var c = CellAt(m, i, j);
                var at = "Pool cell (" + i + ", " + j + ")";
                if ((byte)c.water > (byte)CellWater.Wade) errors.Add(at + ": unknown water class " + (byte)c.water + ".");
                if ((byte)c.connector > (byte)CellConnector.Void) errors.Add(at + ": unknown connector " + (byte)c.connector + ".");
                if (c.connector == CellConnector.Stair)
                {
                    if (c.stairDir > 3) errors.Add(at + ": stair direction " + c.stairDir + " (0 to 3).");
                    if (c.stairRisers == 0 || c.stairRise <= 0f || c.stairRun <= 0f) errors.Add(at + ": a stair needs risers, a rise and a run.");
                    else if (c.stairFrom < -1e-3f || c.stairFrom + c.stairRisers * c.stairRun > MapGrid.CellSize + 1e-3f)
                        errors.Add(at + ": the flight (" + (c.stairFrom + c.stairRisers * c.stairRun).ToString("0.00") + " m) leaves the cell.");
                    else if (c.stairRise > .2f || c.stairRun < .25f) warnings.Add(at + ": steps of " + c.stairRise.ToString("0.00") + " x " + c.stairRun.ToString("0.00") + " m are steeper than the family's 0.18 x 0.28.");
                }
                if (c.HasWater)
                {
                    var low = c.LowestFloor;
                    if (c.waterY <= c.HighestFloor + 1e-3f && c.connector != CellConnector.Stair) warnings.Add(at + ": " + c.water + " water at " + c.waterY.ToString("0.00") + " stands under its floor (" + c.HighestFloor.ToString("0.00") + ").");
                    if (c.waterY - low > PoolRules.MaxWadeDepth + 5e-3f) errors.Add(at + ": " + (c.waterY - low).ToString("0.00") + " m of water, deeper than D6's " + PoolRules.MaxWadeDepth.ToString("0.00") + " m.");
                    if (c.waterY > baseY + ceilingRel - 1.5f) errors.Add(at + ": the water line is within 1.5 m of the ceiling.");
                }
                if (c.water == CellWater.Wade && c.connector != CellConnector.Stair && c.waterY - c.HighestFloor < .3f) warnings.Add(at + ": Wade with " + (c.waterY - c.HighestFloor).ToString("0.00") + " m of water reads as Ankle.");
            }
            foreach (var r in m.edgeRecords ?? new ModuleEdgeRecord[0])
            {
                var at = "Pool edge " + r.side + " " + r.index;
                if ((byte)r.style > (byte)PoolEdgeStyle.Gate) errors.Add(at + ": unknown style " + (byte)r.style + ".");
                if (r.style == PoolEdgeStyle.RoundArch)
                {
                    if (r.archWidth < PoolRules.RoundArchMinWidth - 1e-3f || r.archWidth > PoolRules.RoundArchMaxWidth + 1e-3f)
                        errors.Add(at + ": a round-headed arch is 1.4 to 2.4 m wide (" + r.archWidth.ToString("0.00") + ").");
                    if (r.crown > ceilingRel - PoolRules.CrownClearance + 1e-3f)
                        errors.Add(at + ": crown " + r.crown.ToString("0.00") + " m is over the ceiling less 0.3 (" + (ceilingRel - PoolRules.CrownClearance).ToString("0.00") + ").");
                    if (r.crown - r.archWidth * .5f < 1.2f) warnings.Add(at + ": the springing line sits under 1.2 m.");
                }
                var edge = r.index >= 0 && r.index < (r.side == ModuleSide.South || r.side == ModuleSide.North ? m.width : m.depth) ? m.PerimeterAt(r.side, r.index) : ModuleEdge.Wall;
                if ((r.style == PoolEdgeStyle.RoundArch || r.style == PoolEdgeStyle.Gate) && edge != ModuleEdge.Arch) errors.Add(at + ": a " + r.style + " needs an Arch edge (it is " + edge + ").");
                if ((r.style == PoolEdgeStyle.ObsWindow || r.style == PoolEdgeStyle.ClearGlass) && edge != ModuleEdge.Wall) errors.Add(at + ": a " + r.style + " is a solid edge (it is " + edge + ").");
            }
            WalkOuts(m, errors);
            foreach (var s in standIns ?? new ModuleStandIn[0])
            {
                if (!PoolRules.IsKnownKit(s.kit)) errors.Add("Pool stand-in '" + s.kit + "' is not a known kit (FrontRoomsPoolStandIns).");
                if (s.x < -.01f || s.z < -.01f || s.x > m.WidthMetres + .01f || s.z > m.DepthMetres + .01f) warnings.Add("Pool stand-in " + s.kit + " at (" + s.x.ToString("0.00") + ", " + s.z.ToString("0.00") + ") stands outside the room.");
            }
        }

        // Every Wade region (4-connected Wade cells across open inner edges) needs a way out: a Stair cell, a step of at
        // most PoolRules.StepUp onto a cell that is not Wade, or an opening in the module's perimeter (the plan checks the route there).
        void WalkOuts(RoomModuleData m, List<string> errors)
        {
            var count = m.width * m.depth;
            var region = new int[count];
            for (var k = 0; k < count; k++) region[k] = -1;
            var regions = 0;
            var stack = new Stack<int>();
            for (var k0 = 0; k0 < count; k0++)
            {
                if (region[k0] >= 0 || CellAt(m, k0 % m.width, k0 / m.width).water != CellWater.Wade) continue;
                var id = regions++;
                var way = false;
                region[k0] = id;
                stack.Push(k0);
                while (stack.Count > 0)
                {
                    var k = stack.Pop();
                    int i = k % m.width, j = k / m.width;
                    var c = CellAt(m, i, j);
                    if (c.connector == CellConnector.Stair) way = true;
                    for (var d = 0; d < 4; d++)
                    {
                        int dx = d == 0 ? 1 : d == 1 ? -1 : 0, dy = d == 2 ? 1 : d == 3 ? -1 : 0;
                        int ni = i + dx, nj = j + dy;
                        var edge = m.EdgeAt(i, j, dx, dy);
                        if (ni < 0 || nj < 0 || ni >= m.width || nj >= m.depth)
                        {
                            if (edge != ModuleEdge.Wall) way = true;
                            continue;
                        }
                        if (edge == ModuleEdge.Wall) continue;
                        var o = CellAt(m, ni, nj);
                        var nk = ni + nj * m.width;
                        if (o.water == CellWater.Wade)
                        {
                            if (region[nk] < 0) { region[nk] = id; stack.Push(nk); }
                            continue;
                        }
                        if (PoolRules.EdgeFloor(o, -dx, -dy) - PoolRules.EdgeFloor(c, dx, dy) <= PoolRules.StepUp + 1e-3f) way = true;
                    }
                }
                if (!way) errors.Add("Pool: the basin round cell (" + k0 % m.width + ", " + k0 / m.width + ") has no way out (steps, a beach or a stair).");
            }
        }
    }

    /// <summary>The pool family's rules and defaults (C1, C2, C5, C12).</summary>
    public static class PoolRules
    {
        /// <summary>An Ankle cell: floor and water (pool_modules.json water_classes).</summary>
        public const float AnkleFloor = -.15f, AnkleWater = -.03f;
        /// <summary>The dark zone's base plane and water line (C9: one level down, 1.02 m deep).</summary>
        public const float DarkFloor = -2.52f, DarkWater = -1.5f;
        /// <summary>A basin cell of a dark room that was Wade in its source (1.30 m deep, inside D6).</summary>
        public const float DarkBasinFloor = -2.80f;
        /// <summary>D6: no water deeper than this.</summary>
        public const float MaxWadeDepth = 1.37f;
        /// <summary>The highest step a capsule walks up (CharacterController stepOffset 0.3).</summary>
        public const float StepUp = .3f;
        /// <summary>C5: a round-headed arch's width and its crown's clearance under the ceiling.</summary>
        public const float RoundArchMinWidth = 1.4f, RoundArchMaxWidth = 2.4f, CrownClearance = .3f;
        /// <summary>C9: the gate's opening on PL_FireStair's north edge.</summary>
        public const float GateWidth = 1.2f;

        /// <summary>The stand-in kits FrontRoomsPoolStandIns builds (the module generator and Validate accept only these).</summary>
        public static readonly string[] Kits =
        {
            "Kit_TowelDesk", "Kit_SignBlank", "Kit_LockerBank", "Kit_LockerBench", "Kit_ShowerSet", "Kit_GlassBlockHalfWall",
            "Kit_GlassBlockWall", "Kit_FloorDrain", "Kit_MainDrainGrate", "Kit_PoolLadder", "Kit_GuardChair", "Kit_RingBuoy",
            "Kit_RulesBoard", "Kit_PaceClock", "Kit_DepthMark", "Kit_ObsWindow", "Kit_Soffit", "Kit_Beam", "Kit_PipeRail",
            "Kit_GateBars", "Kit_PipeRun", "Kit_Oculus", "Kit_SlideTube", "Kit_CourtLines",
        };

        public static bool IsKnownKit(string kit)
        {
            if (string.IsNullOrEmpty(kit)) return false;
            foreach (var k in Kits) if (k == kit) return true;
            return false;
        }

        /// <summary>C1: the three pool themes (appended after Level0 and Office).</summary>
        public static bool IsPool(ZoneTheme theme) => theme >= ZoneTheme.PoolClub;

        /// <summary>A module's pool skin, null-safe: its own, else its theme's default (shared: Clone before writing).</summary>
        public static ModulePool SkinOf(RoomModuleData m) => m == null ? Default(ZoneTheme.PoolLevel) : m.pool ?? Default(m.theme);

        /// <summary>A cell of the theme's own kind: the club Dry at 0, the level Ankle, the dark zone Wade one level down.</summary>
        public static ModuleCell DefaultCell(ZoneTheme theme)
        {
            var c = new ModuleCell();
            if (theme == ZoneTheme.PoolLevel) { c.water = CellWater.Ankle; c.floorY = AnkleFloor; c.waterY = AnkleWater; }
            else if (theme == ZoneTheme.PoolDark) { c.water = CellWater.Wade; c.floorY = DarkFloor; c.waterY = DarkWater; }
            return c;
        }

        static ModulePool club, level, dark;

        /// <summary>The theme's default room skin (shared: never write to it, Clone first).</summary>
        public static ModulePool Default(ZoneTheme theme)
        {
            if (theme == ZoneTheme.PoolClub)
                return club ?? (club = new ModulePool { ceiling = PoolCeiling.Grid, lampKit = PoolLampKit.Troffer, upper = PoolUpper.Paint, tileTop = 0f });
            if (theme == ZoneTheme.PoolDark)
                return dark ?? (dark = new ModulePool { baseY = DarkFloor, waterY = DarkWater, ceiling = PoolCeiling.Tile, lampKit = PoolLampKit.Niche, upper = PoolUpper.None, tileTop = ModulePool.TileToCeiling });
            return level ?? (level = new ModulePool
            {
                waterY = AnkleWater, ceiling = PoolCeiling.Tile, lampKit = PoolLampKit.Flush, upper = PoolUpper.None, tileTop = ModulePool.TileToCeiling,
                bandLow = .02f, bandHigh = .32f, bandOnFloor = true,
            });
        }

        /// <summary>
        /// The dark zone's copy of a pool-level room (PL_Dark): theme PoolDark one level down (baseY -2.52), every cell
        /// Wade at -2.52 with water at -1.5 (cells that were Wade in the source at -2.80), every lamp Off, the Level 0
        /// ceiling grid kept with every lens dead (Grid becomes GridDeadMissing; anything else Tile), niche lamps,
        /// stand-ins and records kept, no aqua course.
        /// </summary>
        public static RoomModuleData DarkOf(RoomModuleData source)
        {
            if (source == null) return null;
            var d = source.Clone();
            d.Normalize();
            var src = source.pool ?? new ModulePool();
            d.theme = ZoneTheme.PoolDark;
            d.pool = src.Clone();
            d.pool.baseY = DarkFloor;
            d.pool.waterY = DarkWater;
            d.pool.cells = new ModuleCell[d.width * d.depth];
            for (var j = 0; j < d.depth; j++)
            for (var i = 0; i < d.width; i++)
            {
                var was = src.CellAt(source, i, j);
                var c = new ModuleCell { water = CellWater.Wade, floorY = was.water == CellWater.Wade ? DarkBasinFloor : DarkFloor, waterY = DarkWater };
                d.pool.cells[i + j * d.width] = c;
            }
            for (var k = 0; k < d.lamps.Length; k++) d.lamps[k] = ModuleLamp.Off;
            d.pool.ceiling = src.ceiling == PoolCeiling.Grid || src.ceiling == PoolCeiling.GridDeadMissing ? PoolCeiling.GridDeadMissing : PoolCeiling.Tile;
            d.pool.lampKit = PoolLampKit.Niche;
            d.pool.upper = PoolUpper.None;
            d.pool.tileTop = ModulePool.TileToCeiling;
            d.pool.bandLow = d.pool.bandHigh = 0f;
            d.pool.bandOnFloor = false;
            d.weight = 0f;
            return d;
        }

        /// <summary>
        /// The two map cells of module m's edge record r with the module's south-west cell at map cell (mx, my): a inside
        /// the module, b outside. False for an index off its side.
        /// </summary>
        public static bool RecordCells(RoomModuleData m, ModuleEdgeRecord r, int mx, int my, out GridCoord a, out GridCoord b)
        {
            a = b = default;
            if (m == null) return false;
            var along = r.side == ModuleSide.South || r.side == ModuleSide.North ? m.width : m.depth;
            if (r.index < 0 || r.index >= along) return false;
            switch (r.side)
            {
                case ModuleSide.South: a = new GridCoord(mx + r.index, my); b = new GridCoord(mx + r.index, my - 1); break;
                case ModuleSide.North: a = new GridCoord(mx + r.index, my + m.depth - 1); b = new GridCoord(mx + r.index, my + m.depth); break;
                case ModuleSide.West: a = new GridCoord(mx, my + r.index); b = new GridCoord(mx - 1, my + r.index); break;
                default: a = new GridCoord(mx + m.width - 1, my + r.index); b = new GridCoord(mx + m.width, my + r.index); break;
            }
            return true;
        }

        /// <summary>The floor of cell c in the middle of its edge toward (dx, dy) (the walking height where two cells meet).</summary>
        public static float EdgeFloor(ModuleCell c, int dx, int dy)
        {
            var cs = MapGrid.CellSize;
            var h = cs * .5f;
            float u = dx > 0 ? cs : dx < 0 ? 0f : h, v = dy > 0 ? cs : dy < 0 ? 0f : h;
            return c.FloorAt(u, v);
        }
    }

    /// <summary>MapWorld.WaterAt (C2): a cell's water for the player (P2) and the Relay (P3). Dry at 0 for any cell that is not a pool cell.</summary>
    public struct PoolWaterInfo
    {
        public CellWater water;
        public float floorY, waterY;
        public bool beach;
        /// <summary>True for a cell of a pool theme (the pool family is on and the cell is in it).</summary>
        public bool pool;
    }
}
