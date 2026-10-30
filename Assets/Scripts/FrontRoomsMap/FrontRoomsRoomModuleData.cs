using System;
using System.Collections.Generic;

namespace FrontRooms.Map
{
    /// <summary>
    /// What a room module puts on one of its edges. Doors and windows are not
    /// here: the map decides those where the module meets a zone of another
    /// height (Red's decision 1, doors and windows only on zone borders).
    /// </summary>
    public enum ModuleEdge : byte { Wall, Open, Arch }

    /// <summary>Columns inside the module: the map's 6 m grid rule, none, or the module's own list.</summary>
    public enum ModuleColumns : byte { Auto, None, Custom }

    /// <summary>How the module's room is filled beyond its own props.</summary>
    public enum ModuleFill : byte
    {
        /// <summary>Only the module's props.</summary>
        None,
        /// <summary>As a generated room: Office rooms get the Office kit, Level 0 halls of 4 x 4 or more may get a pile.</summary>
        Auto,
        /// <summary>The Office kit fills round the module's props and columns.</summary>
        Office,
        /// <summary>A furniture pile in the room, whatever the theme.</summary>
        Pile,
    }

    /// <summary>A lamp's temperament, per cell. Auto rolls it from the seed, as everywhere else.</summary>
    public enum ModuleLamp : byte { Auto, Steady, Stutter, Failing, Dead, Dim, Off }

    /// <summary>
    /// One prop: a kit asset at a point in metres from the module's south-west
    /// corner (a cell line), y metres above the floor (wall pieces: a clock,
    /// a window), turned by yaw degrees (0 = front faces +Z).
    /// </summary>
    [Serializable]
    public struct ModuleProp
    {
        public string kit;
        public float x, z, yaw;
        public float y;
        public bool noCollider;
    }

    /// <summary>A column on an inner cell corner of the module, (1..width-1, 1..depth-1).</summary>
    [Serializable]
    public struct ModuleColumn
    {
        public int x, y;
        public bool large;
    }

    /// <summary>What a gameplay marker marks. Values are only ever appended.</summary>
    public enum ModuleMarkerKind : byte
    {
        /// <summary>Where the zone key lies when it falls in this room (the first one is used).</summary>
        KeySpot,
        /// <summary>Where the Relay may appear when it is released or relays (on the floor).</summary>
        RelayEntry,
        /// <summary>A station (A26): the screen the room is read by (M5's beacon, the wayfinding seed).</summary>
        StationBeacon,
        /// <summary>A station (A26): where the evacuation placard hangs (its centre, facing the room).</summary>
        Placard,
        /// <summary>A station (A26): the start door's centre on the south wall; it makes the module a station (IsStation).</summary>
        StationEntrance,
    }

    /// <summary>
    /// A gameplay marker at (x, z) metres from the module's south-west cell
    /// lines, turned with the module like a prop. Height y above the floor
    /// (a key on a desk); yaw in degrees, 0 = +Z.
    /// </summary>
    [Serializable]
    public struct ModuleMarker
    {
        public ModuleMarkerKind kind;
        public float x, z, y, yaw;
        /// <summary>Relay entry: what it is ("vent", "doorway", or anything), passed to the sound layer with the arrival. Empty for none.</summary>
        public string tag;
        /// <summary>Key spot: the kit asset the key hangs on or lies on (the visual chat's key hosts). Empty: the default.</summary>
        public string host;
    }

    /// <summary>
    /// A room authored by a level designer: its footprint in 3 m cells, ceiling
    /// height, theme, what every edge is, columns, props, lamps and where the
    /// generator may use it. Plain data (FrontRoomsRoomModule wraps it as an
    /// asset) so the stamp and its checks run without Unity.
    /// Cell (i, j): i along +X (width), j along +Z (depth).
    /// </summary>
    [Serializable]
    public sealed class RoomModuleData
    {
        public const int MaxCells = MapGrid.ChunkCells;

        public int width = 3, depth = 3;
        public ZoneHeight height = ZoneHeight.Standard;
        public ZoneTheme theme = ZoneTheme.Level0;

        // Perimeter, one entry per cell edge: south and north run along X
        // (width entries, west to east), west and east along Z (depth entries,
        // south to north).
        public ModuleEdge[] south = new ModuleEdge[0], north = new ModuleEdge[0], west = new ModuleEdge[0], east = new ModuleEdge[0];
        // Inside: between cell (i, j) and (i+1, j) at i + j*(width-1); between
        // (i, j) and (i, j+1) at i + j*width. Open unless the designer
        // partitions the room.
        public ModuleEdge[] innerEast = new ModuleEdge[0], innerNorth = new ModuleEdge[0];

        public ModuleColumns columns = ModuleColumns.Auto;
        public ModuleColumn[] customColumns = new ModuleColumn[0];
        public ModuleFill fill = ModuleFill.Auto;
        public ModuleProp[] props = new ModuleProp[0];
        // Gameplay markers (P4): key spot, Relay entries.
        public ModuleMarker[] markers = new ModuleMarker[0];
        // Per cell, i + j*width.
        public ModuleLamp[] lamps = new ModuleLamp[0];

        // Where the generator may use it (P3).
        public float weight = 1f;
        public bool allowRotate = true;
        public int minTier = 0, maxTier = 4;

        public float WidthMetres => width * MapGrid.CellSize;
        public float DepthMetres => depth * MapGrid.CellSize;

        /// <summary>
        /// A station module (A26, the floor-1 start group's room): it carries a StationEntrance marker. Validate then
        /// also asks the station contract (ValidateStation). A station is never in a profile's random pool; the profile
        /// holds it apart (FrontRoomsLevelProfile.stationModule) and FrontRoomsStartGroup builds it into floor 1.
        /// </summary>
        public bool IsStation
        {
            get
            {
                if (markers != null)
                    foreach (var mk in markers)
                        if (mk.kind == ModuleMarkerKind.StationEntrance) return true;
                return false;
            }
        }

        /// <summary>
        /// Change the footprint, keeping every edge, lamp, prop and column
        /// that still fits where it was (cells keep their coordinates).
        /// </summary>
        public void Resize(int newWidth, int newDepth)
        {
            Normalize();
            newWidth = Clamp(newWidth, 1, MaxCells);
            newDepth = Clamp(newDepth, 1, MaxCells);
            if (newWidth == width && newDepth == depth) return;
            var old = Clone();
            width = newWidth;
            depth = newDepth;
            south = north = west = east = innerEast = innerNorth = null;
            lamps = null;
            Normalize();
            for (var j = 0; j < depth; j++)
            for (var i = 0; i < width; i++)
            {
                if (i < old.width && j < old.depth) lamps[i + j * width] = old.LampAt(i, j);
                // Perimeter edges stay on their side; inner edges keep their cells.
                if (j == 0 && i < old.width) south[i] = old.south[i];
                if (j == depth - 1 && i < old.width) north[i] = old.north[i];
                if (i == 0 && j < old.depth) west[j] = old.west[j];
                if (i == width - 1 && j < old.depth) east[j] = old.east[j];
                if (i < width - 1 && i < old.width - 1 && j < old.depth) innerEast[i + j * (width - 1)] = old.innerEast[i + j * (old.width - 1)];
                if (j < depth - 1 && j < old.depth - 1 && i < old.width) innerNorth[i + j * width] = old.innerNorth[i + j * old.width];
            }
            var keep = new List<ModuleColumn>();
            foreach (var c in customColumns) if (c.x > 0 && c.y > 0 && c.x < width && c.y < depth) keep.Add(c);
            customColumns = keep.ToArray();
        }

        /// <summary>Clamp the footprint and size every array to it (after loading, or when arrays are missing).</summary>
        public void Normalize()
        {
            width = Clamp(width, 1, MaxCells);
            depth = Clamp(depth, 1, MaxCells);
            if (theme == ZoneTheme.Office) height = ZoneHeight.Standard;
            south = Resize(south, width, ModuleEdge.Wall);
            north = Resize(north, width, ModuleEdge.Wall);
            west = Resize(west, depth, ModuleEdge.Wall);
            east = Resize(east, depth, ModuleEdge.Wall);
            innerEast = Resize(innerEast, Math.Max(0, width - 1) * depth, ModuleEdge.Open);
            innerNorth = Resize(innerNorth, width * Math.Max(0, depth - 1), ModuleEdge.Open);
            lamps = Resize(lamps, width * depth, ModuleLamp.Auto);
            customColumns = customColumns ?? new ModuleColumn[0];
            props = props ?? new ModuleProp[0];
            markers = markers ?? new ModuleMarker[0];
            // NaN and negative weights mean never; a huge one is capped so totals stay finite.
            weight = weight > 0f ? Math.Min(weight, 1000f) : 0f;
            minTier = Clamp(minTier, 0, 9);
            maxTier = Clamp(maxTier, minTier, 9);
        }

        static T[] Resize<T>(T[] a, int n, T fill)
        {
            if (a != null && a.Length == n) return a;
            var result = new T[n];
            for (var k = 0; k < n; k++) result[k] = a != null && k < a.Length ? a[k] : fill;
            return result;
        }

        static int Clamp(int v, int lo, int hi) => v < lo ? lo : v > hi ? hi : v;

        public RoomModuleData Clone()
        {
            var c = (RoomModuleData)MemberwiseClone();
            c.south = (ModuleEdge[])south?.Clone();
            c.north = (ModuleEdge[])north?.Clone();
            c.west = (ModuleEdge[])west?.Clone();
            c.east = (ModuleEdge[])east?.Clone();
            c.innerEast = (ModuleEdge[])innerEast?.Clone();
            c.innerNorth = (ModuleEdge[])innerNorth?.Clone();
            c.customColumns = (ModuleColumn[])customColumns?.Clone();
            c.props = (ModuleProp[])props?.Clone();
            c.markers = (ModuleMarker[])markers?.Clone();
            c.lamps = (ModuleLamp[])lamps?.Clone();
            return c;
        }

        // ---------- Edges by cell ----------

        /// <summary>The edge between module cell (i, j) and its neighbour in direction (dx, dy); outside the footprint it is a perimeter edge.</summary>
        public ModuleEdge EdgeAt(int i, int j, int dx, int dy)
        {
            if (dx == 1) return i == width - 1 ? east[j] : innerEast[i + j * (width - 1)];
            if (dx == -1) return i == 0 ? west[j] : innerEast[i - 1 + j * (width - 1)];
            if (dy == 1) return j == depth - 1 ? north[i] : innerNorth[i + j * width];
            return j == 0 ? south[i] : innerNorth[i + (j - 1) * width];
        }

        public void SetEdge(int i, int j, int dx, int dy, ModuleEdge kind)
        {
            if (dx == 1) { if (i == width - 1) east[j] = kind; else innerEast[i + j * (width - 1)] = kind; }
            else if (dx == -1) { if (i == 0) west[j] = kind; else innerEast[i - 1 + j * (width - 1)] = kind; }
            else if (dy == 1) { if (j == depth - 1) north[i] = kind; else innerNorth[i + j * width] = kind; }
            else { if (j == 0) south[i] = kind; else innerNorth[i + (j - 1) * width] = kind; }
        }

        public ModuleLamp LampAt(int i, int j) => lamps[i + j * width];

        // ---------- Rotation ----------

        /// <summary>
        /// This module turned clockwise (seen from above) by quarter turns.
        /// Cell (i, j) of a quarter turn goes to (j, width-1-i); props and
        /// columns turn with it and props' yaw grows by 90 per turn.
        /// </summary>
        public RoomModuleData Rotated(int quarterTurns)
        {
            var turns = ((quarterTurns % 4) + 4) % 4;
            var r = Clone();
            for (var t = 0; t < turns; t++) r = r.RotatedOnce();
            return r;
        }

        RoomModuleData RotatedOnce()
        {
            var r = Clone();
            r.width = depth;
            r.depth = width;
            r.south = new ModuleEdge[0]; r.north = new ModuleEdge[0]; r.west = new ModuleEdge[0]; r.east = new ModuleEdge[0];
            r.innerEast = new ModuleEdge[0]; r.innerNorth = new ModuleEdge[0]; r.lamps = new ModuleLamp[0];
            r.Normalize();
            // Clockwise about +Y: +X -> -Z, +Z -> +X. Old cell (i, j) -> new (j, width-1-i).
            for (var j = 0; j < depth; j++)
            for (var i = 0; i < width; i++)
            {
                int ni = j, nj = width - 1 - i;
                r.lamps[ni + nj * r.width] = LampAt(i, j);
                // Old +X edge becomes new -Z edge; old +Z becomes new +X.
                r.SetEdge(ni, nj, 0, -1, EdgeAt(i, j, 1, 0));
                r.SetEdge(ni, nj, 1, 0, EdgeAt(i, j, 0, 1));
                r.SetEdge(ni, nj, 0, 1, EdgeAt(i, j, -1, 0));
                r.SetEdge(ni, nj, -1, 0, EdgeAt(i, j, 0, -1));
            }
            var w = WidthMetres;
            for (var k = 0; k < r.props.Length; k++)
            {
                var p = r.props[k];
                r.props[k] = new ModuleProp { kit = p.kit, x = p.z, z = w - p.x, y = p.y, yaw = (p.yaw + 90f) % 360f, noCollider = p.noCollider };
            }
            for (var k = 0; k < r.customColumns.Length; k++)
            {
                var c = r.customColumns[k];
                r.customColumns[k] = new ModuleColumn { x = c.y, y = width - c.x, large = c.large };
            }
            for (var k = 0; k < r.markers.Length; k++)
            {
                var mk = r.markers[k];
                r.markers[k] = new ModuleMarker { kind = mk.kind, x = mk.z, z = w - mk.x, y = mk.y, yaw = (mk.yaw + 90f) % 360f, tag = mk.tag, host = mk.host };
            }
            return r;
        }

        // ---------- Checks ----------

        /// <summary>
        /// Problems a designer must fix (errors) or should know (warnings).
        /// <paramref name="footprint"/> gives a kit asset's footprint as
        /// (min x, min z, max x, max z, height) about its pivot, or null if unknown,
        /// optionally followed by its screen's centre (x, y, z about the pivot; a
        /// station's TV, ValidateStation's R3 rules).
        /// </summary>
        public void Validate(List<string> errors, List<string> warnings, Func<string, float[]> footprint = null)
        {
            Normalize();
            var ceiling = MapGrid.CeilingHeight(height);

            // At least one way in, and every cell reachable from the others.
            var openings = 0;
            foreach (var e in south) if (e != ModuleEdge.Wall) openings++;
            foreach (var e in north) if (e != ModuleEdge.Wall) openings++;
            foreach (var e in west) if (e != ModuleEdge.Wall) openings++;
            foreach (var e in east) if (e != ModuleEdge.Wall) openings++;
            if (openings == 0) errors.Add("No way in: every perimeter edge is a wall.");
            var seen = new bool[width * depth];
            var stack = new Stack<int>();
            stack.Push(0);
            seen[0] = true;
            var reached = 1;
            while (stack.Count > 0)
            {
                var k = stack.Pop();
                int i = k % width, j = k / width;
                foreach (var d in Steps)
                {
                    int ni = i + d[0], nj = j + d[1];
                    if (ni < 0 || nj < 0 || ni >= width || nj >= depth) continue;
                    if (EdgeAt(i, j, d[0], d[1]) == ModuleEdge.Wall || seen[ni + nj * width]) continue;
                    seen[ni + nj * width] = true;
                    reached++;
                    stack.Push(ni + nj * width);
                }
            }
            if (reached < width * depth) errors.Add("Inner walls cut off " + (width * depth - reached) + " cell(s) from the rest of the room.");

            if (columns == ModuleColumns.Custom)
            {
                foreach (var c in customColumns)
                    if (c.x <= 0 || c.y <= 0 || c.x >= width || c.y >= depth) errors.Add("Column at corner (" + c.x + ", " + c.y + ") is not inside the room.");
                if (customColumns.Length > 0 && (width < 3 || depth < 3)) warnings.Add("Columns in a room under 3 x 3 cells: the map's own rule never puts them there.");
            }

            if (width >= MaxCells - 1 || depth >= MaxCells - 1)
                warnings.Add("A side of 7 or 8 cells meets the chunk border: the map decides the edges there and may open one of your walls to keep the maze connected. Keep props off those walls.");
            if (fill == ModuleFill.Office && theme != ZoneTheme.Office) warnings.Add("Office fill in a Level 0 room.");
            var dresses = fill == ModuleFill.Office || fill == ModuleFill.Pile || (fill == ModuleFill.Auto && (theme == ZoneTheme.Office || (width >= 4 && depth >= 4)));
            if (dresses && InnerStrips().Count > 0)
                warnings.Add("Fill " + fill + " furnishes the room as one open space: the Office kit or a pile may stand in an inner wall or block an inner doorway. Use Fill None with inner walls.");
            if (fill == ModuleFill.Pile && (width < 4 || depth < 4)) warnings.Add("A pile needs a hall of 4 x 4 cells to look right.");

            // Columns run floor to ceiling: the corners where one stands, or may stand in a turn the generator can use.
            var columnAt = new List<(int i, int j, float size, string what)>();
            var autoSize = theme == ZoneTheme.Office || height == ZoneHeight.Tall ? ModuleUnits.ColumnLarge : ModuleUnits.ColumnSmall;
            for (var j = 1; j < depth; j++)
            for (var i = 1; i < width; i++)
            {
                if (columns == ModuleColumns.Custom)
                {
                    foreach (var c in customColumns)
                        if (c.x == i && c.y == j) { columnAt.Add((i, j, c.large ? ModuleUnits.ColumnLarge : ModuleUnits.ColumnSmall, "a column stands")); break; }
                    continue;
                }
                for (var t = 0; t < (allowRotate ? 4 : 1); t++)
                    if (AutoColumnAt(i, j, t)) { columnAt.Add((i, j, autoSize, t == 0 ? "an Auto column may stand" : "an Auto column may stand when the room is turned " + t * 90 + "°")); break; }
            }

            // Props: inside the clear floor, under the ceiling, off the openings and columns.
            var clearMin = ModuleUnits.WallHalf;
            for (var k = 0; k < props.Length; k++)
            {
                var p = props[k];
                var label = "Prop " + (k + 1) + " (" + (string.IsNullOrEmpty(p.kit) ? "no kit" : p.kit) + ")";
                if (string.IsNullOrEmpty(p.kit)) { errors.Add(label + ": choose a kit asset."); continue; }
                var f = footprint?.Invoke(p.kit);
                if (f == null) { warnings.Add(label + ": no footprint known (asset missing?)."); continue; }
                Bounds(p, f, out var x0, out var z0, out var x1, out var z1);
                if (x0 < clearMin - .01f || z0 < clearMin - .01f || x1 > WidthMetres - clearMin + .01f || z1 > DepthMetres - clearMin + .01f)
                    errors.Add(label + ": stands in or beyond a wall.");
                if (p.y + f[4] > ceiling - ModuleUnits.CeilingClearance) errors.Add(label + ": reaches " + (p.y + f[4]).ToString("0.00") + " m, the ceiling is " + ceiling.ToString("0.0") + " m.");
                foreach (var c in columnAt)
                {
                    float cx = c.i * MapGrid.CellSize, cz = c.j * MapGrid.CellSize, r = c.size * .5f;
                    if (x0 < cx + r && cx - r < x1 && z0 < cz + r && cz - r < z1) { warnings.Add(label + ": stands where " + c.what + " (corner " + c.i + ", " + c.j + ")."); break; }
                }
                if (p.y > ModuleUnits.RelayHeight) continue; // wall pieces up high leave the floor free
                // The map keeps these strips clear when it builds (FrontRoomsMapWorld.KeepClear): a prop with a collider there is left out.
                foreach (var strip in EntryStrips())
                {
                    if (!(x0 < strip[2] && strip[0] < x1 && z0 < strip[3] && strip[1] < z1)) continue;
                    if (p.noCollider) warnings.Add(label + ": stands in the floor kept clear inside an opening (" + ModuleUnits.EntryClearDepth.ToString("0.0") + " m).");
                    else errors.Add(label + ": blocks the floor inside an opening (keep " + ModuleUnits.EntryClearDepth.ToString("0.0") + " m clear); the map leaves it out of the build. Move it, or tick No collider for clutter.");
                    break;
                }
                foreach (var strip in InnerStrips())
                {
                    if (!(x0 < strip[2] && strip[0] < x1 && z0 < strip[3] && strip[1] < z1)) continue;
                    if (strip[4] > 0f) errors.Add(label + ": stands in an inner wall.");
                    else warnings.Add(label + ": blocks the floor at an inner doorway.");
                    break;
                }
            }

            // Can the player get from every opening to every other, and into every cell, past the props and inner walls?
            WalkGrid walked = null;
            if (errors.Count == 0 && openings > 0)
            {
                if (!Walk(footprint, out var problem, out walked)) errors.Add(problem);
                else if (walked.seen == null) walked = null;
            }
            ValidateMarkers(errors, warnings, footprint, columnAt, ceiling, walked);
            if (IsStation) ValidateStation(errors, footprint);
        }

        // ---------- The station contract (A26, notes/floor1_start_group_plan.md 4.9) ----------

        /// <summary>A station's footprint: 4 cells wide, 4 or 3 deep (12 x 12 or 12 x 9 m).</summary>
        public const int StationWidth = 4, StationMinDepth = 3, StationMaxDepth = 4;
        /// <summary>The start door's column (0-based from the west) = E1's index in the north wall; E2's row in the west wall.</summary>
        public const int StationEntranceColumn = 2, StationE2Row = 1;
        /// <summary>
        /// The sight line from the stream room to the EXIT sign (B8): nothing over StationAxisClear within StationAxisHalf
        /// of the door's axis, and no screen in the axis column; a screen hung up high keeps its bottom at StationScreenBottom
        /// or more, over the Relay's 2.05 m walking height.
        /// </summary>
        public const float StationAxisClear = 1.5f, StationAxisHalf = .6f, StationScreenBottom = 2.15f;

        /// <summary>A screen prop (the kit's CRT TV): the axis column and bracket rules ask it.</summary>
        static bool IsScreen(string kit) => kit != null && kit.IndexOf("CRTTV", StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>
        /// R3 (the visual line's station TV contract, 2026-10-29, apply/r3_station_tv_contract section 2): the playing TV's
        /// close-up solves only with its screen centre at most StationScreenTop over the floor (measured: 2.70 m solves,
        /// 2.75 m does not; this keeps 5 cm under it). The close-up's camera is pushed in from the stand spot, 1.0-1.3 m in
        /// front of the screen, and the push-in is swept against colliders: nothing with a collider, and not the map's riser
        /// block, may stand within StationPushInHalf of the screen's axis from the screen out to StationPushInDepth.
        /// </summary>
        public const float StationScreenTop = 2.65f, StationPushInDepth = 1.3f, StationPushInHalf = .5f;
        /// <summary>M5: the TV hangs on a cell edge (the wayfinding's station cell points at it) when its StationBeacon marker is square to that wall, within this many degrees, and this close to its cell line (metres).</summary>
        public const float StationBeaconSquareDeg = 10f, StationBeaconWallReach = 1f;

        /// <summary>
        /// M5: a station's TV as the wayfinding's seed. The module cell (i, j) of the StationBeacon marker, and the edge of
        /// that cell the TV hangs on (0 +x, 1 +z, 2 -x, 3 -z, FrontRoomsWayfinding's dirs): the wall behind the marker, which
        /// faces the room along its yaw, when the yaw is square to that wall (within StationBeaconSquareDeg) and the marker
        /// lies within StationBeaconWallReach of its cell line; else -1 (the TV stands in the room, in view from its cell).
        /// False with no StationBeacon marker. The map adds its own test that the edge is a wall (FrontRoomsMapWorld
        /// StationBeaconEdge).
        /// </summary>
        public bool StationBeaconCell(out int i, out int j, out int edge)
        {
            i = j = 0;
            edge = -1;
            if (markers == null) return false;
            foreach (var mk in markers)
            {
                if (mk.kind != ModuleMarkerKind.StationBeacon) continue;
                var cs = MapGrid.CellSize;
                i = Clamp((int)Math.Floor(mk.x / cs), 0, width - 1);
                j = Clamp((int)Math.Floor(mk.z / cs), 0, depth - 1);
                // The wall is behind the screen: the marker's facing turned round.
                var rad = mk.yaw * (Math.PI / 180.0);
                double bx = -Math.Sin(rad), bz = -Math.Cos(rad);
                var square = Math.Cos(StationBeaconSquareDeg * Math.PI / 180.0);
                for (var k = 0; k < 4; k++)
                {
                    int dx = k == 0 ? 1 : k == 2 ? -1 : 0, dz = k == 1 ? 1 : k == 3 ? -1 : 0;
                    if (bx * dx + bz * dz < square) continue;
                    // The cell line on that side, and the marker's distance to it.
                    var gap = k == 0 ? (i + 1) * cs - mk.x : k == 1 ? (j + 1) * cs - mk.z : k == 2 ? mk.x - i * cs : mk.z - j * cs;
                    if (gap <= StationBeaconWallReach) edge = k;
                    break;
                }
                return true;
            }
            return false;
        }

        // A screen prop's screen (R3): its centre in module metres (x, z) and over the floor (y), and its facing (nx, nz: the
        // kit's front faces +Z, turned by yaw). The centre is the kit's screen anchor (the footprint's entries 5-7) or, when
        // the footprint has none, the middle of the front face of its box.
        static void ScreenOf(ModuleProp p, float[] f, out float x, out float y, out float z, out float nx, out float nz)
        {
            float ax, ay, az;
            if (f.Length >= 8) { ax = f[5]; ay = f[6]; az = f[7]; }
            else { ax = (f[0] + f[2]) * .5f; ay = f[4] * .5f; az = f[3]; }
            var rad = p.yaw * (Math.PI / 180.0);
            float c = (float)Math.Cos(rad), s = (float)Math.Sin(rad);
            // Unity yaw, as Bounds: x' = x cos + z sin, z' = -x sin + z cos.
            x = p.x + ax * c + az * s;
            z = p.z - ax * s + az * c;
            y = p.y + ay;
            nx = s;
            nz = c;
        }

        // R3's push-in: the strip StationPushInHalf either side of the screen's axis, from the screen (ox, oz) out along its
        // facing (nx, nz, unit) to StationPushInDepth, overlaps the rect x0..x1, z0..z1 (module metres). Separating axes: the
        // rect's two and the strip's two; 1 cm of grace, so a box that only touches the strip passes.
        static bool InPushIn(float ox, float oz, float nx, float nz, float x0, float z0, float x1, float z1)
        {
            const float Grace = .01f;
            float tx = nz, tz = -nx;
            float aMin = float.MaxValue, aMax = float.MinValue, cMin = float.MaxValue, cMax = float.MinValue;
            for (var k = 0; k < 4; k++)
            {
                float px = (k & 1) == 0 ? x0 : x1, pz = (k & 2) == 0 ? z0 : z1;
                float along = (px - ox) * nx + (pz - oz) * nz, across = (px - ox) * tx + (pz - oz) * tz;
                aMin = Math.Min(aMin, along); aMax = Math.Max(aMax, along);
                cMin = Math.Min(cMin, across); cMax = Math.Max(cMax, across);
            }
            if (aMax <= Grace || aMin >= StationPushInDepth - Grace || cMax <= -StationPushInHalf + Grace || cMin >= StationPushInHalf - Grace) return false;
            float sxMin = float.MaxValue, sxMax = float.MinValue, szMin = float.MaxValue, szMax = float.MinValue;
            for (var k = 0; k < 4; k++)
            {
                float along = (k & 1) == 0 ? 0f : StationPushInDepth, across = (k & 2) == 0 ? -StationPushInHalf : StationPushInHalf;
                float px = ox + nx * along + tx * across, pz = oz + nz * along + tz * across;
                sxMin = Math.Min(sxMin, px); sxMax = Math.Max(sxMax, px);
                szMin = Math.Min(szMin, pz); szMax = Math.Max(szMax, pz);
            }
            return !(sxMax <= x0 + Grace || sxMin >= x1 - Grace || szMax <= z0 + Grace || szMin >= z1 - Grace);
        }

        // R3: the push-in in front of a screen (what) at (ox, oy, oz), facing (nx, nz), against the riser block's rect and every
        // prop with a collider but the screen itself (self; -1 for the beacon marker, which leaves out every screen prop: the
        // TV is the screen). A prop hung with its bottom at the screen's centre or higher is over the push-in, which rises from
        // the eye toward the screen.
        void PushInClear(List<string> errors, string label, string what, float ox, float oy, float oz, float nx, float nz, int self,
            Func<string, float[]> footprint, float rx0, float rz0, float rx1, float rz1)
        {
            var rule = " (keep " + StationPushInHalf.ToString("0.00") + " m either side of its axis clear from the screen out to " + StationPushInDepth.ToString("0.00")
                + " m: the close-up's camera is pushed in from the stand spot, 1.0-1.3 m out, and swept against colliders; R3).";
            if (InPushIn(ox, oz, nx, nz, rx0, rz0, rx1, rz1))
                errors.Add(label + "the riser block (the map builds it at x " + rx0.ToString("0.00") + "-" + rx1.ToString("0.00") + ", z " + rz0.ToString("0.00") + "-" + rz1.ToString("0.00")
                    + " m) stands in the push-in in front of " + what + rule);
            for (var q = 0; q < props.Length; q++)
            {
                if (q == self) continue;
                var p = props[q];
                if (p.noCollider || string.IsNullOrEmpty(p.kit) || (self < 0 && IsScreen(p.kit)) || p.y >= oy) continue;
                var f = footprint?.Invoke(p.kit);
                if (f == null) continue;
                Bounds(p, f, out var x0, out var z0, out var x1, out var z1);
                if (InPushIn(ox, oz, nx, nz, x0, z0, x1, z1)) errors.Add(label + "Prop " + (q + 1) + " (" + p.kit + ") stands in the push-in in front of " + what + rule);
            }
        }

        /// <summary>
        /// The station contract (4.9), on top of every module rule: 4 x 4 or 4 x 3, Standard Level 0, no columns, no fill,
        /// weight 0, never turned; the south and east walls whole; exactly one arch north, at the entrance column (E1), and
        /// one west, at row 1 (E2); the start door's west inner edge Open (G1) and the entrance column's inner edges Open
        /// (the straight walk D to E1); no Auto lamp and at least 2 Steady on the south row; one key spot, one station
        /// beacon, one placard, one station entrance (at the entrance column) and no Relay entry; nothing over 1.50 m
        /// within 0.6 m of the door's axis, no screen in the axis column, a hung screen's bottom at 2.15 m or more.
        /// R3 (the playing TV): a screen's centre (its kit's screen anchor) and the beacon marker at most 2.65 m up, and the
        /// push-in in front of each (a screen prop's screen; the beacon marker's point along its turn) clear of props with
        /// colliders and of the riser block's fixed north-east footprint (FrontRoomsStationSet RiserWidth x RiserDepth).
        /// Props over 2.85 m are the general ceiling rule's. The budget (100k LOD0 triangles, 120 renderers) and the riser
        /// block itself (map-built shell, 9) are checked on the built set, not here.
        /// </summary>
        void ValidateStation(List<string> errors, Func<string, float[]> footprint)
        {
            const string label = "Station: ";
            if (height != ZoneHeight.Standard || theme != ZoneTheme.Level0) errors.Add(label + height + " " + theme + "; a station is Standard height, Level 0 (its skin is its own).");
            if (columns != ModuleColumns.None) errors.Add(label + "columns " + columns + "; a station has None.");
            if (fill != ModuleFill.None) errors.Add(label + "fill " + fill + "; a station has None (only its own props).");
            if (weight != 0f) errors.Add(label + "weight " + weight + "; a station is never a random pick (weight 0).");
            if (allowRotate) errors.Add(label + "allow rotate is on; a station is never turned.");
            if (width != StationWidth || depth < StationMinDepth || depth > StationMaxDepth)
            {
                errors.Add(label + width + " x " + depth + " cells; a station is " + StationWidth + " x 4 or " + StationWidth + " x 3.");
                return;
            }

            for (var i = 0; i < width; i++)
                if (south[i] != ModuleEdge.Wall) { errors.Add(label + "the south wall has an opening at column " + (i + 1) + "; it is whole (the start door is the title's)."); break; }
            for (var j = 0; j < depth; j++)
                if (east[j] != ModuleEdge.Wall) { errors.Add(label + "the east wall has an opening at row " + (j + 1) + "; it is whole."); break; }
            for (var i = 0; i < width; i++)
            {
                var e1 = i == StationEntranceColumn;
                if (e1 ? north[i] != ModuleEdge.Arch : north[i] != ModuleEdge.Wall)
                    errors.Add(label + "north column " + (i + 1) + " is " + north[i] + "; E1 is the one arch, at column " + (StationEntranceColumn + 1) + " (the start door's axis).");
            }
            for (var j = 0; j < depth; j++)
            {
                var e2 = j == StationE2Row;
                if (e2 ? west[j] != ModuleEdge.Arch : west[j] != ModuleEdge.Wall)
                    errors.Add(label + "west row " + (j + 1) + " is " + west[j] + "; E2 is the one arch, at row " + (StationE2Row + 1) + ".");
            }
            if (innerEast[StationEntranceColumn - 1] != ModuleEdge.Open)
                errors.Add(label + "the inner edge west of the start door is " + innerEast[StationEntranceColumn - 1] + "; it is Open (G1).");
            for (var j = 0; j < depth - 1; j++)
                if (innerNorth[StationEntranceColumn + j * width] != ModuleEdge.Open)
                    errors.Add(label + "the entrance column's inner edge north of row " + (j + 1) + " is " + innerNorth[StationEntranceColumn + j * width] + "; the walk from the door to E1 is Open.");

            var steady = 0;
            for (var k = 0; k < lamps.Length; k++)
            {
                if (lamps[k] == ModuleLamp.Auto) { errors.Add(label + "the lamp of cell (" + (k % width + 1) + ", " + (k / width + 1) + ") is Auto; a station's lamps are all set."); break; }
                if (k < width && lamps[k] == ModuleLamp.Steady) steady++;
            }
            if (steady < 2) errors.Add(label + steady + " Steady lamp(s) on the south row; at least 2 light the entrance.");

            int keys = 0, beacons = 0, placards = 0, entrances = 0, relays = 0;
            foreach (var mk in markers)
            {
                switch (mk.kind)
                {
                    case ModuleMarkerKind.KeySpot: keys++; break;
                    case ModuleMarkerKind.RelayEntry: relays++; break;
                    case ModuleMarkerKind.StationBeacon: beacons++; break;
                    case ModuleMarkerKind.Placard: placards++; break;
                    case ModuleMarkerKind.StationEntrance:
                        entrances++;
                        if ((int)Math.Floor(mk.x / MapGrid.CellSize) != StationEntranceColumn || mk.z > MapGrid.CellSize)
                            errors.Add(label + "the station entrance at (" + mk.x.ToString("0.00") + ", " + mk.z.ToString("0.00") + ") is not in the south row's column " + (StationEntranceColumn + 1) + ".");
                        break;
                }
            }
            if (keys != 1 || beacons != 1 || placards != 1 || entrances != 1 || relays != 0)
                errors.Add(label + "markers: " + keys + " key spot, " + beacons + " station beacon, " + placards + " placard, " + entrances + " station entrance, " + relays
                    + " Relay entry; a station has one of each and no Relay entry.");

            var axis = (StationEntranceColumn + .5f) * MapGrid.CellSize;
            for (var k = 0; k < props.Length; k++)
            {
                var p = props[k];
                var f = string.IsNullOrEmpty(p.kit) ? null : footprint?.Invoke(p.kit);
                if (f == null) continue;
                Bounds(p, f, out var x0, out _, out var x1, out _);
                var name = "Prop " + (k + 1) + " (" + p.kit + ")";
                if (x0 < axis + StationAxisHalf && x1 > axis - StationAxisHalf && p.y + f[4] > StationAxisClear)
                    errors.Add(label + name + " reaches " + (p.y + f[4]).ToString("0.00") + " m within " + StationAxisHalf.ToString("0.0") + " m of the door's axis; nothing there over "
                        + StationAxisClear.ToString("0.00") + " m (the EXIT sign is seen over it).");
                if (!IsScreen(p.kit)) continue;
                if (x0 < (StationEntranceColumn + 1) * MapGrid.CellSize && x1 > StationEntranceColumn * MapGrid.CellSize)
                    errors.Add(label + name + " reaches into the axis column (x " + (StationEntranceColumn * MapGrid.CellSize).ToString("0") + "-" + ((StationEntranceColumn + 1) * MapGrid.CellSize).ToString("0") + " m).");
                if (p.y > .01f && p.y < StationScreenBottom)
                    errors.Add(label + name + " hangs with its bottom at " + p.y.ToString("0.00") + " m; at least " + StationScreenBottom.ToString("0.00") + " m (over the Relay's walking height).");
            }

            // R3: the playing TV. Each screen prop from its kit's screen, and the beacon marker (the TV the room is read by,
            // M5) from its point and turn, so a module whose TV is dressed later is checked too. The riser block stands in the
            // north-east cell, RiserWidth in from the east wall line and RiserDepth in from the north (FrontRoomsStationSet).
            float rx0 = WidthMetres - FrontRoomsStationSet.RiserWidth, rz0 = DepthMetres - FrontRoomsStationSet.RiserDepth, rx1 = WidthMetres, rz1 = DepthMetres;
            for (var k = 0; k < props.Length; k++)
            {
                var p = props[k];
                if (!IsScreen(p.kit)) continue;
                var f = footprint?.Invoke(p.kit);
                if (f == null) continue;
                ScreenOf(p, f, out var sx, out var sy, out var sz, out var nx, out var nz);
                var name = "Prop " + (k + 1) + " (" + p.kit + ")";
                if (sy > StationScreenTop)
                    errors.Add(label + name + " has its screen centre " + sy.ToString("0.00") + " m up; the TV's close-up solves only up to " + StationScreenTop.ToString("0.00")
                        + " m (R3: 2.70 m solves, 2.75 m does not).");
                PushInClear(errors, label, name + "'s screen", sx, sy, sz, nx, nz, k, footprint, rx0, rz0, rx1, rz1);
            }
            foreach (var mk in markers)
            {
                if (mk.kind != ModuleMarkerKind.StationBeacon) continue;
                if (mk.y > StationScreenTop)
                    errors.Add(label + "the station beacon is " + mk.y.ToString("0.00") + " m up; it marks the TV, whose screen centre is at most " + StationScreenTop.ToString("0.00") + " m up (R3).");
                var rad = mk.yaw * (Math.PI / 180.0);
                PushInClear(errors, label, "the station beacon", mk.x, mk.y, mk.z, (float)Math.Sin(rad), (float)Math.Cos(rad), -1, footprint, rx0, rz0, rx1, rz1);
            }
        }

        /// <summary>
        /// The markers' checks. Errors: outside the room or in a wall, an inner
        /// wall, a column or (Relay entry) a prop; a key inside a prop rather than
        /// on it; a key above the ceiling; a marker nobody can reach from the
        /// openings. Warnings: a second key spot (only the first is used), a
        /// Relay entry with a height (it stands on the floor), a marker where an
        /// Auto column may stand, a key spot above 1.8 m (over the eye line).
        /// </summary>
        void ValidateMarkers(List<string> errors, List<string> warnings, Func<string, float[]> footprint, List<(int i, int j, float size, string what)> columnAt, float ceiling, WalkGrid walked)
        {
            var keys = 0;
            for (var k = 0; k < markers.Length; k++)
            {
                var mk = markers[k];
                if (mk.kind > ModuleMarkerKind.RelayEntry)
                {
                    // A station anchor (A26): a point the station set reads, on the floor, up high or on a wall's face, so
                    // only inside the footprint and under the ceiling (ValidateStation counts them).
                    var at = "Marker " + (k + 1) + " (" + mk.kind + ")";
                    if (mk.x < -.001f || mk.z < -.001f || mk.x > WidthMetres + .001f || mk.z > DepthMetres + .001f) errors.Add(at + ": lies outside the room.");
                    else if (mk.y < 0f || mk.y > ceiling) errors.Add(at + ": is " + mk.y.ToString("0.00") + " m up, outside the floor-to-ceiling " + ceiling.ToString("0.0") + " m.");
                    continue;
                }
                var relay = mk.kind == ModuleMarkerKind.RelayEntry;
                var label = "Marker " + (k + 1) + " (" + (relay ? "Relay entry" : "key spot") + ")";
                // The Relay's body must fit there; a key only has to be off the wall.
                var margin = relay ? ModuleUnits.RelayRadius : .05f;
                var lo = ModuleUnits.WallHalf + margin;
                if (mk.x < lo - .001f || mk.z < lo - .001f || mk.x > WidthMetres - lo + .001f || mk.z > DepthMetres - lo + .001f)
                {
                    errors.Add(label + ": stands in or beyond a wall (keep " + lo.ToString("0.00") + " m from the cell lines).");
                    continue;
                }
                foreach (var w in InnerStrips())
                    if (w[4] > 0f && mk.x > w[0] - margin && mk.x < w[2] + margin && mk.z > w[1] - margin && mk.z < w[3] + margin)
                    { errors.Add(label + ": stands in an inner wall."); break; }
                foreach (var c in columnAt)
                {
                    float cx = c.i * MapGrid.CellSize, cz = c.j * MapGrid.CellSize, r = c.size * .5f + margin;
                    if (Math.Abs(mk.x - cx) >= r || Math.Abs(mk.z - cz) >= r) continue;
                    if (columns == ModuleColumns.Custom) errors.Add(label + ": stands in a column (corner " + c.i + ", " + c.j + ").");
                    else warnings.Add(label + ": stands where " + c.what + " (corner " + c.i + ", " + c.j + ").");
                    break;
                }
                for (var q = 0; q < props.Length; q++)
                {
                    var pr = props[q];
                    var f = string.IsNullOrEmpty(pr.kit) ? null : footprint?.Invoke(pr.kit);
                    if (f == null) continue;
                    Bounds(pr, f, out var x0, out var z0, out var x1, out var z1);
                    if (relay)
                    {
                        if (pr.noCollider || pr.y > ModuleUnits.RelayHeight) continue;
                        if (mk.x > x0 - margin && mk.x < x1 + margin && mk.z > z0 - margin && mk.z < z1 + margin)
                        { errors.Add(label + ": stands in prop " + (q + 1) + " (" + pr.kit + "): the Relay's 0.3 m body must fit."); break; }
                    }
                    else if (mk.x > x0 && mk.x < x1 && mk.z > z0 && mk.z < z1 && mk.y < pr.y + f[4] - .02f && mk.y + .02f > pr.y)
                    { errors.Add(label + ": is inside prop " + (q + 1) + " (" + pr.kit + "); put it on top (height " + (pr.y + f[4]).ToString("0.00") + " m)."); break; }
                }
                if (relay)
                {
                    if (Math.Abs(mk.y) > .001f) warnings.Add(label + ": a Relay entry stands on the floor; its height is ignored.");
                    if (walked != null && !walked.NearestFreeReached(mk.x, mk.z, walked.step * 1.5f))
                        errors.Add(label + ": the Relay could not walk out of it (props or inner walls shut it in).");
                }
                else
                {
                    if (++keys == 2) warnings.Add(label + ": only the first key spot is used.");
                    if (mk.y > ceiling - .1f) errors.Add(label + ": is " + mk.y.ToString("0.00") + " m up, at or above the " + ceiling.ToString("0.0") + " m ceiling.");
                    else if (mk.y > 1.8f) warnings.Add(label + ": is " + mk.y.ToString("0.00") + " m up, above the player's eye line (they may not see it).");
                    // A key is taken from up to 0.9 m away (FrontRoomsMapWorld), at any height.
                    if (walked != null && !walked.ReachedNear(mk.x, mk.z, .85f))
                        errors.Add(label + ": nobody can get within 0.9 m of it to take the key.");
                }
            }
        }

        /// <summary>
        /// True if the map's column rule (FrontRoomsMapGenerator.PlaceColumns) may put an
        /// Auto column on inner corner (i, j) with the room placed <paramref name="turns"/>
        /// quarter turns clockwise. The generator lands an Auto-column module on the world
        /// 6 m grid phase of its turned footprint centred in a chunk, as the preview shows it,
        /// so an odd side moves the columns to the other corners in odd turns. Never in Low
        /// rooms, rooms under <paramref name="minCells"/> a side, or where inner walls or doorways meet.
        /// </summary>
        public bool AutoColumnAt(int i, int j, int turns, int minCells = 3)
        {
            if (columns != ModuleColumns.Auto || height == ZoneHeight.Low || width < minCells || depth < minCells) return false;
            if (i <= 0 || j <= 0 || i >= width || j >= depth) return false;
            if (EdgeAt(i - 1, j, 1, 0) != ModuleEdge.Open || EdgeAt(i - 1, j - 1, 1, 0) != ModuleEdge.Open
                || EdgeAt(i - 1, j - 1, 0, 1) != ModuleEdge.Open || EdgeAt(i, j - 1, 0, 1) != ModuleEdge.Open) return false;
            int w = width, d = depth, ci = i, cj = j;
            for (var t = 0; t < ((turns % 4) + 4) % 4; t++)
            {
                // Clockwise: corner (x, y) of a w x d room goes to (y, w - x) of the d x w room.
                (ci, cj) = (cj, w - ci);
                (w, d) = (d, w);
            }
            return ((MaxCells - w) / 2 + ci) % 2 == 0 && ((MaxCells - d) / 2 + cj) % 2 == 0;
        }

        /// <summary>
        /// Inner edges as (x0, z0, x1, z1, isWall) in module metres: an inner
        /// wall's 0.16 m band, or the floor kept clear both sides of an inner
        /// doorway (arch). Open inner edges are just floor.
        /// </summary>
        public List<float[]> InnerStrips()
        {
            var cs = MapGrid.CellSize;
            var h = ModuleUnits.WallHalf;
            var d = ModuleUnits.EntryClearDepth + h;
            var strips = new List<float[]>();
            for (var j = 0; j < depth; j++)
            for (var i = 0; i < width; i++)
            {
                if (i < width - 1 && innerEast[i + j * (width - 1)] != ModuleEdge.Open)
                {
                    var wall = innerEast[i + j * (width - 1)] == ModuleEdge.Wall;
                    var r = wall ? h : d;
                    var x = (i + 1) * cs;
                    strips.Add(new[] { x - r, j * cs, x + r, (j + 1) * cs, wall ? 1f : 0f });
                }
                if (j < depth - 1 && innerNorth[i + j * width] != ModuleEdge.Open)
                {
                    var wall = innerNorth[i + j * width] == ModuleEdge.Wall;
                    var r = wall ? h : d;
                    var z = (j + 1) * cs;
                    strips.Add(new[] { i * cs, z - r, (i + 1) * cs, z + r, wall ? 1f : 0f });
                }
            }
            return strips;
        }

        /// <summary>
        /// Floods the floor on a 0.25 m grid with the player's 0.3 m body,
        /// from the first opening, past walls (perimeter and inner) and props
        /// on the floor. False, with the reason, if an opening or a cell is
        /// cut off. Inner doorways count as open along their whole edge (where
        /// the map puts the gap varies), so this is a lower bound on trouble.
        /// </summary>
        public bool Walkable(Func<string, float[]> footprint, out string problem) => Walk(footprint, out problem, out _);

        /// <summary>The walk test's grid: 0.25 m nodes, blocked for the 0.3 m body, and those reached from the first opening.</summary>
        sealed class WalkGrid
        {
            public bool[] blocked, seen;
            public int nx, nz;
            public float step;

            /// <summary>
            /// Whether the free node nearest (x, z), within <paramref name="radius"/>, was reached. A point
            /// the clearance checks passed can sit up to about a step and a half from its nearest free node
            /// at a wall, prop or corner margin.
            /// </summary>
            public bool NearestFreeReached(float x, float z, float radius)
            {
                if (seen == null) return false;
                var best = -1;
                var bestD = radius * radius;
                for (var k = 0; k < blocked.Length; k++)
                {
                    if (blocked[k]) continue;
                    float dx = (k % nx + .5f) * step - x, dz = (k / nx + .5f) * step - z;
                    var d2 = dx * dx + dz * dz;
                    if (d2 <= bestD) { bestD = d2; best = k; }
                }
                return best >= 0 && seen[best];
            }

            /// <summary>A reached node within <paramref name="radius"/> of (x, z).</summary>
            public bool ReachedNear(float x, float z, float radius)
            {
                if (seen == null) return false;
                for (var k = 0; k < seen.Length; k++)
                {
                    if (!seen[k]) continue;
                    float nx0 = (k % nx + .5f) * step - x, nz0 = (k / nx + .5f) * step - z;
                    if (nx0 * nx0 + nz0 * nz0 <= radius * radius) return true;
                }
                return false;
            }
        }

        bool Walk(Func<string, float[]> footprint, out string problem, out WalkGrid grid)
        {
            problem = null;
            grid = null;
            const float step = .25f;
            var cs = MapGrid.CellSize;
            var body = ModuleUnits.PlayerRadius;
            int nx = (int)Math.Round(WidthMetres / step), nz = (int)Math.Round(DepthMetres / step);
            var blocked = new bool[nx * nz];
            grid = new WalkGrid { blocked = blocked, nx = nx, nz = nz, step = step };
            var rects = new List<float[]>();
            foreach (var p in props)
            {
                if (p.noCollider || p.y > ModuleUnits.RelayHeight || string.IsNullOrEmpty(p.kit)) continue;
                var f = footprint?.Invoke(p.kit);
                if (f == null) continue;
                Bounds(p, f, out var x0, out var z0, out var x1, out var z1);
                rects.Add(new[] { x0, z0, x1, z1 });
            }
            foreach (var w in InnerStrips()) if (w[4] > 0f) rects.Add(new[] { w[0], w[1], w[2], w[3] });
            for (var k = 0; k < blocked.Length; k++)
            {
                float x = (k % nx + .5f) * step, z = (k / nx + .5f) * step;
                // Perimeter walls: everywhere but the openings.
                var nearWall = x < ModuleUnits.WallHalf + body || z < ModuleUnits.WallHalf + body || x > WidthMetres - ModuleUnits.WallHalf - body || z > DepthMetres - ModuleUnits.WallHalf - body;
                if (nearWall && !InOpening(x, z, body)) { blocked[k] = true; continue; }
                foreach (var r in rects)
                    if (x > r[0] - body && x < r[2] + body && z > r[1] - body && z < r[3] + body) { blocked[k] = true; break; }
            }

            // One seed per perimeter opening: a free node inside the floor kept
            // clear behind it, nearest the opening's middle. None: the opening is blocked.
            var seeds = new List<int>();
            var names = new List<string>();
            void Seed(float[] strip, string name)
            {
                float cx = (strip[0] + strip[2]) * .5f, cz = (strip[1] + strip[3]) * .5f;
                var best = -1;
                var bestD = float.MaxValue;
                for (var k = 0; k < blocked.Length; k++)
                {
                    if (blocked[k]) continue;
                    float x = (k % nx + .5f) * step, z = (k / nx + .5f) * step;
                    if (x < strip[0] || x > strip[2] || z < strip[1] || z > strip[3]) continue;
                    var d2 = (x - cx) * (x - cx) + (z - cz) * (z - cz);
                    if (d2 < bestD) { bestD = d2; best = k; }
                }
                seeds.Add(best);
                names.Add(name);
            }
            var depthClear = ModuleUnits.EntryClearDepth + ModuleUnits.WallHalf;
            for (var i = 0; i < width; i++)
            {
                if (south[i] != ModuleEdge.Wall) Seed(new[] { i * cs, 0f, (i + 1) * cs, depthClear }, "the south opening of column " + (i + 1));
                if (north[i] != ModuleEdge.Wall) Seed(new[] { i * cs, DepthMetres - depthClear, (i + 1) * cs, DepthMetres }, "the north opening of column " + (i + 1));
            }
            for (var j = 0; j < depth; j++)
            {
                if (west[j] != ModuleEdge.Wall) Seed(new[] { 0f, j * cs, depthClear, (j + 1) * cs }, "the west opening of row " + (j + 1));
                if (east[j] != ModuleEdge.Wall) Seed(new[] { WidthMetres - depthClear, j * cs, WidthMetres, (j + 1) * cs }, "the east opening of row " + (j + 1));
            }
            for (var s = 0; s < seeds.Count; s++)
                if (seeds[s] < 0) { problem = "Props block " + names[s] + "."; return false; }
            if (seeds.Count == 0) return true;

            var seen = new bool[blocked.Length];
            grid.seen = seen;
            var queue = new Queue<int>();
            queue.Enqueue(seeds[0]);
            seen[seeds[0]] = true;
            while (queue.Count > 0)
            {
                var k = queue.Dequeue();
                int x = k % nx, z = k / nx;
                if (x > 0) Visit(k - 1);
                if (x < nx - 1) Visit(k + 1);
                if (z > 0) Visit(k - nx);
                if (z < nz - 1) Visit(k + nx);
            }
            void Visit(int k)
            {
                if (seen[k] || blocked[k]) return;
                seen[k] = true;
                queue.Enqueue(k);
            }
            for (var s = 1; s < seeds.Count; s++)
                if (!seen[seeds[s]]) { problem = "Props or inner walls cut " + names[s] + " off from " + names[0] + "."; return false; }
            var per = (int)Math.Round(cs / step);
            for (var j = 0; j < depth; j++)
            for (var i = 0; i < width; i++)
            {
                var any = false;
                for (var b = 0; b < per && !any; b++)
                for (var a = 0; a < per && !any; a++)
                    any = seen[i * per + a + (j * per + b) * nx];
                if (!any) { problem = "Props or inner walls leave cell (" + (i + 1) + ", " + (j + 1) + ") unreachable."; return false; }
            }
            return true;
        }

        bool InOpening(float x, float z, float body)
        {
            var cs = MapGrid.CellSize;
            var i = Math.Min(width - 1, Math.Max(0, (int)(x / cs)));
            var j = Math.Min(depth - 1, Math.Max(0, (int)(z / cs)));
            var lo = ModuleUnits.WallHalf + body;
            if (z < lo && south[i] != ModuleEdge.Wall) return true;
            if (z > DepthMetres - lo && north[i] != ModuleEdge.Wall) return true;
            if (x < lo && west[j] != ModuleEdge.Wall) return true;
            if (x > WidthMetres - lo && east[j] != ModuleEdge.Wall) return true;
            return false;
        }

        static readonly int[][] Steps = { new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 } };

        /// <summary>The axis-aligned box a prop covers, in module metres.</summary>
        public static void Bounds(ModuleProp p, float[] f, out float x0, out float z0, out float x1, out float z1)
        {
            var rad = p.yaw * (Math.PI / 180.0);
            float c = (float)Math.Cos(rad), s = (float)Math.Sin(rad);
            x0 = z0 = float.MaxValue;
            x1 = z1 = float.MinValue;
            foreach (var cx in new[] { f[0], f[2] })
            foreach (var cz in new[] { f[1], f[3] })
            {
                // Unity yaw: x' = x cos + z sin, z' = -x sin + z cos.
                var wx = p.x + cx * c + cz * s;
                var wz = p.z - cx * s + cz * c;
                if (wx < x0) x0 = wx; if (wx > x1) x1 = wx;
                if (wz < z0) z0 = wz; if (wz > z1) z1 = wz;
            }
        }

        /// <summary>Floor kept clear inside every opening of the perimeter, as (x0, z0, x1, z1) in module metres.</summary>
        public List<float[]> EntryStrips()
        {
            var cs = MapGrid.CellSize;
            var d = ModuleUnits.EntryClearDepth + ModuleUnits.WallHalf;
            var strips = new List<float[]>();
            for (var i = 0; i < width; i++)
            {
                if (south[i] != ModuleEdge.Wall) strips.Add(new[] { i * cs, 0f, (i + 1) * cs, d });
                if (north[i] != ModuleEdge.Wall) strips.Add(new[] { i * cs, DepthMetres - d, (i + 1) * cs, DepthMetres });
            }
            for (var j = 0; j < depth; j++)
            {
                if (west[j] != ModuleEdge.Wall) strips.Add(new[] { 0f, j * cs, d, (j + 1) * cs });
                if (east[j] != ModuleEdge.Wall) strips.Add(new[] { WidthMetres - d, j * cs, WidthMetres, (j + 1) * cs });
            }
            return strips;
        }

        public static EdgeKind ToEdge(ModuleEdge e) => e == ModuleEdge.Open ? EdgeKind.Open : e == ModuleEdge.Arch ? EdgeKind.Arch : EdgeKind.Wall;
    }
}
