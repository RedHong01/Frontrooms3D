using System;
using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;

/// <summary>
/// W1, the Relay warning's lamp bursts (RELAY_PURSUIT_REDESIGN.md §9.2), bridged onto Relay v1 (C10): while the
/// hunter's WarnStage is 1 or 2, the lamps of the room the player stands in dip together in short bursts, with
/// quiet gaps between them. Stage 0 never touches a lamp; a chase (v1's lock-on) starts no new burst.
/// - The room: the carved room the player stands in, else a walk over Open edges (depth 2, at most 9 cells;
///   arches and doors are walls). Never the start area. Lamps within 2 cells of the Relay or in its room are
///   left out. Live lamps are the lit ones within lightRadius; with fewer than 2 the set widens inside the zone
///   over Open and Arch edges (depth 3, at most 12 cells); with none the burst is audio only (WarnBurst, 0 lamps).
/// - A burst: stage 1 two or three dips, stage 2 two, 0.55 s apart; each dip to 0.55-0.70 of the lamp's level,
///   attack 0.10 s, hold 0.06-0.15 s, release 0.22 s; every lamp within ±40 ms of the others (its cell hash).
///   Gaps: stage 1 3-6.5 s, stage 2 2.5-4 s; the first burst 0.3-1.0 s into stage 1; entering stage 2 bursts
///   at once if the last burst ended 2.5 s ago or more. Reduce flashing: one sag to 0.7 over 0.6 s and back.
/// - A burst's stuttering lamps (Stutter, Failing) are held Steady from its first dip, so no ambient flicker
///   shows through, and go back to the map's own mode in the first quiet moment after stage 0.
/// Built on the map's lamp overrides (LampFx.Warn); one instance per map, ticked by the game after the Relay.
/// </summary>
public sealed class FrontRoomsWarnLamps
{
    public const float DipSpacing = .55f, DipAttack = .10f, DipRelease = .22f, DipHoldMin = .06f, DipHoldMax = .15f;
    public const float DipDepthMin = .55f, DipDepthMax = .70f, Jitter = .04f;
    public const float FirstBurstMin = .3f, FirstBurstMax = 1f, Stage2Rearm = 2.5f;
    public const float Gap1Min = 3f, Gap1Max = 6.5f, Gap2Min = 2.5f, Gap2Max = 4f;
    public const float ReducedDepth = .7f, ReducedSeconds = .6f;
    const int RelayClearCells = 2, RoomDepth = 2, RoomCells = 9, WideDepth = 3, WideCells = 12;

    readonly FrontRoomsMapWorld world;
    uint rng;
    float clock;
    int stage;
    // The next burst's start (PositiveInfinity: none planned) and when the last one ended.
    float nextBurst = float.PositiveInfinity, lastBurstEnd = float.NegativeInfinity, burstEnd = float.NegativeInfinity;
    readonly List<Pending> pending = new List<Pending>();
    readonly List<int> handles = new List<int>();
    readonly HashSet<GridCoord> steadied = new HashSet<GridCoord>();
    readonly List<GridCoord> room = new List<GridCoord>(), live = new List<GridCoord>();
    GridCoord roomFor;
    bool roomValid;
    readonly Queue<Step> walk = new Queue<Step>();
    readonly HashSet<GridCoord> seen = new HashSet<GridCoord>();

    struct Step
    {
        public GridCoord cell;
        public int depth;
    }

    struct Pending
    {
        public float at, multiplier, hold, attack, release;
        public GridCoord cell;
    }

    /// <summary>A burst began: the room's centre and how many lamps dip (0 = audio only). The sound layer's ballast groan.</summary>
    public event Action<Vector3, int> WarnBurst;

    /// <summary>Tools and tests: bursts begun, and the room and live lamps of the last one.</summary>
    public int BurstCount { get; private set; }
    public IReadOnlyList<GridCoord> RoomSet => room;
    public IReadOnlyList<GridCoord> LiveLamps => live;
    /// <summary>True while a burst plays (its last dip has not finished).</summary>
    public bool Bursting => clock < burstEnd;

    public FrontRoomsWarnLamps(FrontRoomsMapWorld world, int seed)
    {
        this.world = world;
        rng = (uint)seed ^ 0x9E3779B9u;
        if (rng == 0) rng = 1;
    }

    /// <summary>
    /// One frame: the hunter's WarnStage (0-2), whether it has locked on (v1: Chase), the player's and the Relay's
    /// positions. Starts bursts by the rules above and plays their dips.
    /// </summary>
    public void Tick(float dt, int warnStage, bool lockedOn, Vector3 player, Vector3 relay)
    {
        if (world == null || world.Cache == null) return;
        clock += Mathf.Max(0f, dt);
        warnStage = Mathf.Clamp(warnStage, 0, 2);
        if (warnStage != stage) StageChanged(warnStage);
        PlayDue();
        if (stage == 0)
        {
            if (!Bursting && pending.Count == 0) RestoreModes();
            return;
        }
        if (lockedOn || clock < nextBurst || Bursting) return;
        BeginBurst(player, relay);
    }

    /// <summary>Tools and tests: work out the room and its live lamps for these positions, as a burst would (no dip).</summary>
    public void RoomForTools(Vector3 player, Vector3 relay)
    {
        var p = world.CellOf(player);
        RoomOf(p);
        roomFor = p;
        roomValid = true;
        LiveLampsOf(p, world.CellOf(relay));
    }

    /// <summary>Stop at once: no burst planned, every warning dip released, the held lamps back to their own modes.</summary>
    public void Reset()
    {
        stage = 0;
        nextBurst = float.PositiveInfinity;
        burstEnd = float.NegativeInfinity;
        pending.Clear();
        ReleaseAll();
        RestoreModes();
    }

    void StageChanged(int next)
    {
        var from = stage;
        stage = next;
        if (next == 0)
        {
            // Stage 0 never shows a warning: the burst stops, the dips playing release from where they are.
            nextBurst = float.PositiveInfinity;
            pending.Clear();
            ReleaseAll();
            burstEnd = Mathf.Min(burstEnd, clock + DipRelease);
            return;
        }
        if (from == 0) nextBurst = clock + Range(FirstBurstMin, FirstBurstMax);
        else if (next == 2 && from == 1 && !Bursting && clock - lastBurstEnd >= Stage2Rearm) nextBurst = clock;
    }

    void BeginBurst(Vector3 player, Vector3 relay)
    {
        var p = world.CellOf(player);
        if (!roomValid || p != roomFor) { RoomOf(p); roomFor = p; roomValid = true; }
        LiveLampsOf(p, world.CellOf(relay));
        var dips = stage == 1 ? (Next() % 2 == 0 ? 2 : 3) : 2;
        var reduced = FrontRoomsSettings.ReduceFlashing;
        var last = clock;
        if (reduced)
        {
            foreach (var c in live)
                pending.Add(new Pending { at = clock + Jit(c), cell = c, multiplier = ReducedDepth, hold = 0f, attack = ReducedSeconds, release = ReducedSeconds });
            last = clock + 2f * Jitter + 2f * ReducedSeconds;
        }
        else
        {
            for (var d = 0; d < dips; d++)
            {
                var start = clock + d * DipSpacing;
                var depth = Range(DipDepthMin, DipDepthMax);
                var hold = Range(DipHoldMin, DipHoldMax);
                foreach (var c in live)
                    pending.Add(new Pending { at = start + Jit(c), cell = c, multiplier = depth, hold = hold, attack = DipAttack, release = DipRelease });
                last = start + 2f * Jitter + DipAttack + hold + DipRelease;
            }
        }
        // Hold the burst's stuttering lamps steady from its first dip.
        foreach (var c in live)
        {
            var mode = world.LampModeOf(c);
            if ((mode == ModuleLamp.Stutter || mode == ModuleLamp.Failing) && steadied.Add(c)) world.SetLampMode(c, ModuleLamp.Steady);
        }
        burstEnd = last;
        lastBurstEnd = last;
        nextBurst = last + (stage == 1 ? Range(Gap1Min, Gap1Max) : Range(Gap2Min, Gap2Max));
        BurstCount++;
        WarnBurst?.Invoke(Centre(), live.Count);
        PlayDue();
    }

    void PlayDue()
    {
        for (var i = 0; i < pending.Count; i++)
        {
            var d = pending[i];
            if (d.at > clock) continue;
            handles.Add(world.SetLampOverride(d.cell, FrontRoomsMapWorld.LampFx.Warn, d.multiplier, d.hold,
                new FrontRoomsMapWorld.LampEnvelope(d.attack, d.release)));
            pending.RemoveAt(i--);
        }
        if (handles.Count > 64) handles.RemoveRange(0, handles.Count - 64);
    }

    void ReleaseAll()
    {
        foreach (var h in handles) world.RemoveLampOverride(h);
        handles.Clear();
    }

    void RestoreModes()
    {
        if (steadied.Count == 0) return;
        foreach (var c in steadied) world.SetLampMode(c, ModuleLamp.Auto);
        steadied.Clear();
    }

    // ---------- The room and its live lamps ----------

    // The carved room the cell stands in, else a walk over Open edges (depth 2, at most 9 cells). Never the start area.
    void RoomOf(GridCoord p)
    {
        room.Clear();
        if (!world.IsBuilt(p)) return;
        var chunk = MapGrid.ChunkOf(p);
        if (world.Cache.TryGetGenerated(chunk, out var data))
        {
            var o = MapGrid.ChunkOrigin(chunk);
            for (var r = data.rooms.Length - 1; r >= 0; r--)
            {
                var rect = data.rooms[r];
                if (!rect.Contains(p.x - o.x, p.y - o.y)) continue;
                for (var y = rect.y; y < rect.y + rect.h; y++)
                for (var x = rect.x; x < rect.x + rect.w; x++)
                {
                    var c = new GridCoord(o.x + x, o.y + y);
                    if (world.IsBuilt(c)) room.Add(c);
                }
                return;
            }
        }
        Walk(p, RoomDepth, RoomCells, false, room);
    }

    // The room's lit lamps within lightRadius, never near the Relay; widened inside the zone when fewer than 2.
    void LiveLampsOf(GridCoord player, GridCoord relay)
    {
        live.Clear();
        var relayRoom = RelayRoom(relay);
        Collect(room, player, relay, relayRoom);
        if (live.Count >= 2) return;
        var wide = new List<GridCoord>();
        Walk(player, WideDepth, WideCells, true, wide);
        live.Clear();
        Collect(wide, player, relay, relayRoom);
    }

    void Collect(List<GridCoord> cells, GridCoord player, GridCoord relay, CellRectInWorld relayRoom)
    {
        var radius = world.Profile != null ? world.Profile.lightRadius : 16f;
        var r2 = radius * radius;
        var centre = world.CellCenter(player);
        foreach (var c in cells)
        {
            if (Mathf.Max(Mathf.Abs(c.x - relay.x), Mathf.Abs(c.y - relay.y)) <= RelayClearCells || relayRoom.Contains(c)) continue;
            var level = world.LampLevel(c);
            if (level == FrontRoomsMapWorld.NoLamp) continue;
            var mode = world.LampModeOf(c);
            if (mode == ModuleLamp.Dead || mode == ModuleLamp.Off) continue;
            if ((world.CellCenter(c) - centre).sqrMagnitude > r2) continue;
            live.Add(c);
        }
    }

    readonly struct CellRectInWorld
    {
        readonly int x, y, w, h;
        public CellRectInWorld(int x, int y, int w, int h) { this.x = x; this.y = y; this.w = w; this.h = h; }
        public bool Contains(GridCoord c) => c.x >= x && c.x < x + w && c.y >= y && c.y < y + h;
    }

    CellRectInWorld RelayRoom(GridCoord relay)
    {
        var chunk = MapGrid.ChunkOf(relay);
        if (!world.Cache.TryGetGenerated(chunk, out var data)) return default;
        var o = MapGrid.ChunkOrigin(chunk);
        for (var r = data.rooms.Length - 1; r >= 0; r--)
        {
            var rect = data.rooms[r];
            if (rect.Contains(relay.x - o.x, relay.y - o.y)) return new CellRectInWorld(o.x + rect.x, o.y + rect.y, rect.w, rect.h);
        }
        return default;
    }

    // Breadth-first from p over built cells: Open edges only, or (wide) Open and Arch edges inside p's zone.
    void Walk(GridCoord p, int maxDepth, int maxCells, bool wide, List<GridCoord> into)
    {
        into.Clear();
        if (!world.IsBuilt(p)) return;
        walk.Clear();
        seen.Clear();
        walk.Enqueue(new Step { cell = p, depth = 0 });
        seen.Add(p);
        var zone = world.ZoneOf(p).id;
        while (walk.Count > 0 && into.Count < maxCells)
        {
            var step = walk.Dequeue();
            var cell = step.cell;
            var depth = step.depth;
            into.Add(cell);
            if (depth >= maxDepth) continue;
            for (var k = 0; k < 4; k++)
            {
                var n = k == 0 ? new GridCoord(cell.x + 1, cell.y) : k == 1 ? new GridCoord(cell.x - 1, cell.y)
                    : k == 2 ? new GridCoord(cell.x, cell.y + 1) : new GridCoord(cell.x, cell.y - 1);
                if (seen.Contains(n) || !world.IsBuilt(n)) continue;
                var edge = world.Cache.Edge(cell, n);
                if (!(edge == EdgeKind.Open || (wide && edge == EdgeKind.Arch))) continue;
                if (wide && world.ZoneOf(n).id != zone) continue;
                seen.Add(n);
                walk.Enqueue(new Step { cell = n, depth = depth + 1 });
            }
        }
    }

    Vector3 Centre()
    {
        var cells = live.Count > 0 ? live : room;
        if (cells.Count == 0) return Vector3.zero;
        var sum = Vector3.zero;
        foreach (var c in cells) sum += world.CellCenter(c);
        return sum / cells.Count;
    }

    // ---------- Randomness: the run's own stream, and a fixed jitter per lamp ----------

    uint Next()
    {
        rng ^= rng << 13;
        rng ^= rng >> 17;
        rng ^= rng << 5;
        return rng;
    }

    float Range(float a, float b) => a + (b - a) * ((Next() & 0xFFFFFF) / 16777216f);

    // 0..2 × Jitter, from the cell alone, so a lamp keeps its place in every dip (±40 ms round the dip's mean).
    static float Jit(GridCoord c)
    {
        unchecked
        {
            var h = (uint)(c.x * 73856093 ^ c.y * 19349663) * 2654435761u;
            return 2f * Jitter * ((h >> 8) / 16777216f);
        }
    }
}
