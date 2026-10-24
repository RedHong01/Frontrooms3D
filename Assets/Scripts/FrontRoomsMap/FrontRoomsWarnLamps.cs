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
///   left out. Live lamps are the lit ones within lightRadius (Stutter and Failing ones only while M1 holds them);
///   with fewer than 2 the set widens inside the zone over Open and Arch edges (depth 3, at most 12 cells); with
///   none the burst is audio only (WarnBurst, 0 lamps).
/// - A burst (M3, the lamp check's envelope, research/lamp_warn/10_lamp_look_check.md §4): stage 1 two or three
///   dips, stage 2 two, 0.55 s apart; the first dip of a burst to 0.55-0.62 of the lamp's level, the later ones
///   to 0.65-0.75 (the first hit is the deepest); attack 0.05 s, hold 0.04-0.08 s, release 0.30 s with an
///   exponential tail (0.09 s: fast at first, then easing out), so a dip is at most 0.43 s; every lamp within
///   ±40 ms of the others (its cell hash). Gaps: stage 1 3-6.5 s, stage 2 2.5-4 s; the first burst 0.3-1.0 s into
///   stage 1; entering stage 2 bursts at once if the last burst ended 2.5 s ago or more. Reduce flashing: one sag
///   to 0.7 over 0.6 s and back (linear).
/// - M1, hold and rejoin without a pop: from stage 1 the room's Stutter and Failing lamps are held, crossfaded in
///   the map's lamp tick (FrontRoomsMapWorld.SetLampHold; 0.3 s, Reduce flashing 0.5 s, from the level they show):
///   Failing to Dim (0.42), Stutter to Steady. Held, they are live like the others and dip with them; a held lamp
///   joins once it has been fully held HoldSettle, and a burst due before that waits for it (the whole burst,
///   never one lamp), so its crossfade never shares a second with two dips. The hold follows the player's room;
///   a lamp left behind, and every one at stage 0, lets go 2-6 s later (each its own draw, never on the stage-0
///   frame) over 1.2 s, back to its own mode, which plays its events again only from a quiet moment. Their mode
///   (LampModeOf) never changes. Reset lets go at once (over 1.2 s); NextFloor drops every hold.
/// - M2 (§9.5): from stage 1 until stage 0 the room's Dead lamps strike no blink (FrontRoomsMapWorld.SetAmbientHold
///   on the room the player stands in, moved with them; released at stage 0 and on Reset). Dead lamps need nothing
///   more for M1: dark, they have no level to crossfade or dip, and their strike timer runs on, so letting go at
///   stage 0 shows no strike on that frame.
/// Built on the map's lamp overrides (LampFx.Warn); one instance per map, ticked by the game after the Relay.
/// </summary>
public sealed class FrontRoomsWarnLamps
{
    public const float DipSpacing = .55f, DipAttack = .05f, DipRelease = .30f, DipHoldMin = .04f, DipHoldMax = .08f;
    public const float DipDepthMin = .55f, DipDepthMax = .75f, Jitter = .04f;
    /// <summary>M3: the first dip of a burst goes deepest, the later ones less; the release's exponential tail.</summary>
    public const float FirstDepthMin = .55f, FirstDepthMax = .62f, LaterDepthMin = .65f, LaterDepthMax = .75f, DipTail = .09f;
    /// <summary>M1: the hold's crossfade in (Reduce flashing: longer), the settle before a held lamp joins a burst, the let-go's delay and crossfade back.</summary>
    public const float HoldIn = .3f, HoldInReduced = .5f, HoldSettle = .6f, RestoreMin = 2f, RestoreMax = 6f, RestoreSeconds = 1.2f;
    public const float FirstBurstMin = .3f, FirstBurstMax = 1f, Stage2Rearm = 2.5f;
    public const float Gap1Min = 3f, Gap1Max = 6.5f, Gap2Min = 2.5f, Gap2Max = 4f;
    public const float ReducedDepth = .7f, ReducedSeconds = .6f;
    const int RelayClearCells = 2, WideDepth = 3, WideCells = 12;

    readonly FrontRoomsMapWorld world;
    uint rng;
    float clock;
    int stage;
    // The next burst's start (PositiveInfinity: none planned) and when the last one ended.
    float nextBurst = float.PositiveInfinity, lastBurstEnd = float.NegativeInfinity, burstEnd = float.NegativeInfinity;
    readonly List<Pending> pending = new List<Pending>();
    readonly List<int> handles = new List<int>();
    readonly List<GridCoord> room = new List<GridCoord>(), live = new List<GridCoord>();
    GridCoord roomFor;
    bool roomValid;
    // M2: the room whose Dead lamps are held dark (worked out apart from the burst's room, so a burst is unchanged).
    readonly List<GridCoord> holdRoom = new List<GridCoord>(), held = new List<GridCoord>();
    GridCoord holdFor;
    bool holdValid;
    // M1: the room's Stutter and Failing lamps this warning holds, with the clock at which each may join a burst
    // (the keys are the held set); the next room's while it is worked out; RoomForTools' own; the let-go draws.
    readonly List<GridCoord> flickering = new List<GridCoord>(), flickeringNext = new List<GridCoord>();
    readonly Dictionary<GridCoord, float> joinAt = new Dictionary<GridCoord, float>(), joinTools = new Dictionary<GridCoord, float>();
    uint restoreRng;
    readonly Queue<Step> walk = new Queue<Step>();
    readonly HashSet<GridCoord> seen = new HashSet<GridCoord>();

    struct Step
    {
        public GridCoord cell;
        public int depth;
    }

    struct Pending
    {
        public float at, multiplier, hold, attack, release, tail;
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
        // M1's let-go draws come from a stream of their own, so the bursts' draws are the same as without it.
        restoreRng = (uint)seed ^ 0x85EBCA6Bu;
        if (restoreRng == 0) restoreRng = 1;
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
        if (stage == 0) return;
        HoldRoom(world.CellOf(player));
        if (lockedOn || clock < nextBurst || Bursting) return;
        BeginBurst(player, relay);
    }

    /// <summary>Tools and tests: work out the room and its live lamps for these positions, as a burst would (no dip).</summary>
    public void RoomForTools(Vector3 player, Vector3 relay)
    {
        var p = world.CellOf(player);
        RoomOf(p, room);
        roomFor = p;
        roomValid = true;
        // As a burst in a warning would: the room's Stutter and Failing lamps held (M1).
        joinTools.Clear();
        if (HoldFlickeringForTools)
        {
            FlickeringOf(room, flickeringNext);
            foreach (var c in flickeringNext) joinTools[c] = float.NegativeInfinity;
        }
        LiveLampsOf(p, world.CellOf(relay), joinTools);
    }

    /// <summary>Tools and tests (M2): the cells whose Dead strikes this warning holds dark now (empty at stage 0).</summary>
    public IReadOnlyList<GridCoord> HeldCells => held;

    /// <summary>Tools and tests (M1): the Stutter and Failing lamps this warning holds now (empty at stage 0; a let-go then runs on the map).</summary>
    public IReadOnlyList<GridCoord> HeldFlickering => flickering;

    /// <summary>Tools and tests (M1): false keeps this warning from holding Stutter and Failing lamps (main before M1: they are left out and flicker through).</summary>
    public bool HoldFlickeringForTools { get; set; } = true;

    /// <summary>Stop at once: no burst planned, every warning dip released, no Dead lamp held, every held lamp let go now (over RestoreSeconds).</summary>
    public void Reset()
    {
        // C22 (plan v2 D12): the game resets at the seal, while the old floor's lamps still stand (what this
        // releases is theirs), before NextFloor reseeds the map in place. The room is remembered by cell, so
        // it is worked out again: a burst in the same cell of the next floor must not dip the old floor's room.
        roomValid = false;
        stage = 0;
        nextBurst = float.PositiveInfinity;
        burstEnd = float.NegativeInfinity;
        pending.Clear();
        ReleaseAll();
        ReleaseHolds();
        ReleaseFlickering(false);
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
            ReleaseHolds();
            ReleaseFlickering(true);
            burstEnd = Mathf.Min(burstEnd, clock + DipRelease);
            return;
        }
        if (from == 0) nextBurst = clock + Range(FirstBurstMin, FirstBurstMax);
        else if (next == 2 && from == 1 && !Bursting && clock - lastBurstEnd >= Stage2Rearm) nextBurst = clock;
    }

    void BeginBurst(Vector3 player, Vector3 relay)
    {
        var p = world.CellOf(player);
        if (!roomValid || p != roomFor) { RoomOf(p, room); roomFor = p; roomValid = true; }
        LiveLampsOf(p, world.CellOf(relay), joinAt);
        // M1: a held lamp joins once settled; a burst due before that waits, all of it (never one lamp).
        var ready = clock;
        foreach (var c in live)
            if (joinAt.TryGetValue(c, out var at) && at > ready) ready = at;
        if (ready > clock)
        {
            nextBurst = ready;
            return;
        }
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
                var depth = d == 0 ? Range(FirstDepthMin, FirstDepthMax) : Range(LaterDepthMin, LaterDepthMax);
                var hold = Range(DipHoldMin, DipHoldMax);
                foreach (var c in live)
                    pending.Add(new Pending { at = start + Jit(c), cell = c, multiplier = depth, hold = hold, attack = DipAttack, release = DipRelease, tail = DipTail });
                last = start + 2f * Jitter + DipAttack + hold + DipRelease;
            }
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
                new FrontRoomsMapWorld.LampEnvelope(d.attack, d.release, d.tail)));
            pending.RemoveAt(i--);
        }
        // A Tall room's burst set (M1: up to about 30 lamps, 3 dips) must stay releasable at stage 0.
        if (handles.Count > 256) handles.RemoveRange(0, handles.Count - 256);
    }

    void ReleaseAll()
    {
        foreach (var h in handles) world.RemoveLampOverride(h);
        handles.Clear();
    }

    // M2: hold the Dead lamps of the room the player stands in (worked out again when they change cell); a cell
    // left behind is let go.
    void HoldRoom(GridCoord p)
    {
        if (holdValid && p == holdFor) return;
        holdFor = p;
        holdValid = true;
        RoomOf(p, holdRoom);
        foreach (var c in held)
            if (!holdRoom.Contains(c)) world.SetAmbientHold(c, false);
        world.SetAmbientHold(holdRoom, true);
        held.Clear();
        held.AddRange(holdRoom);
        HoldFlickering(holdRoom);
    }

    void ReleaseHolds()
    {
        world.SetAmbientHold(held, false);
        held.Clear();
        holdValid = false;
    }

    // M1: hold the room's Stutter and Failing lamps (Failing at Dim, Stutter at Steady, crossfaded from where they
    // are) and let go of those left behind (2-6 s later). A lamp may join a burst once fully held for HoldSettle.
    void HoldFlickering(List<GridCoord> cells)
    {
        if (!HoldFlickeringForTools) return;
        FlickeringOf(cells, flickeringNext);
        foreach (var c in flickering)
        {
            if (flickeringNext.Contains(c)) continue;
            world.ReleaseLampHold(c, Restore(), RestoreSeconds);
            joinAt.Remove(c);
        }
        var seconds = FrontRoomsSettings.ReduceFlashing ? HoldInReduced : HoldIn;
        foreach (var c in flickeringNext)
        {
            if (joinAt.ContainsKey(c)) continue;
            var weight = world.LampHoldWeight(c);
            world.SetLampHold(c, world.LampModeOf(c) == ModuleLamp.Failing ? ModuleLamp.Dim : ModuleLamp.Steady, seconds);
            joinAt[c] = weight >= 1f ? clock : clock + (1f - weight) * seconds + HoldSettle;
        }
        flickering.Clear();
        flickering.AddRange(flickeringNext);
    }

    // M1: let go of every held lamp: at stage 0 each 2-6 s later (its own draw); on Reset at once. Over RestoreSeconds.
    void ReleaseFlickering(bool late)
    {
        foreach (var c in flickering) world.ReleaseLampHold(c, late ? Restore() : 0f, RestoreSeconds);
        flickering.Clear();
        joinAt.Clear();
    }

    // The built Stutter and Failing lamps among these cells.
    void FlickeringOf(List<GridCoord> cells, List<GridCoord> into)
    {
        into.Clear();
        foreach (var c in cells)
        {
            if (world.LampLevel(c) == FrontRoomsMapWorld.NoLamp) continue;
            var mode = world.LampModeOf(c);
            if (mode == ModuleLamp.Stutter || mode == ModuleLamp.Failing) into.Add(c);
        }
    }

    // ---------- The room and its live lamps ----------

    // The carved room the cell stands in, else a walk over Open edges (depth 2, at most 9 cells). Never the start area.
    // The one shared room set (RELAY 9.2), which the wallpaper cue's turns read too: FrontRoomsWayfinding.RoomSet.
    void RoomOf(GridCoord p, List<GridCoord> into) => FrontRoomsWayfinding.RoomSet(world, p, into);

    // The room's lit lamps within lightRadius, never near the Relay; widened inside the zone when fewer than 2.
    // Stutter and Failing lamps are live only while held (M1: the keys of `holds`).
    void LiveLampsOf(GridCoord player, GridCoord relay, Dictionary<GridCoord, float> holds)
    {
        live.Clear();
        var relayRoom = RelayRoom(relay);
        Collect(room, player, relay, relayRoom, holds);
        if (live.Count >= 2) return;
        var wide = new List<GridCoord>();
        Walk(player, WideDepth, WideCells, true, wide);
        live.Clear();
        Collect(wide, player, relay, relayRoom, holds);
    }

    void Collect(List<GridCoord> cells, GridCoord player, GridCoord relay, CellRectInWorld relayRoom, Dictionary<GridCoord, float> holds)
    {
        var radius = world.Profile != null ? world.Profile.lightRadius : 16f;
        var r2 = radius * radius;
        var centre = world.CellCenter(player);
        foreach (var c in cells)
        {
            if (Mathf.Max(Mathf.Abs(c.x - relay.x), Mathf.Abs(c.y - relay.y)) <= RelayClearCells || relayRoom.Contains(c)) continue;
            var level = world.LampLevel(c);
            if (level == FrontRoomsMapWorld.NoLamp) continue;
            // Dark lamps have nothing to dip; stuttering ones take part only while held (M1), else they would flicker through.
            var mode = world.LampModeOf(c);
            if (mode == ModuleLamp.Dead || mode == ModuleLamp.Off) continue;
            if ((mode == ModuleLamp.Stutter || mode == ModuleLamp.Failing) && !holds.ContainsKey(c)) continue;
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

    // M1: a let-go's delay, RestoreMin..RestoreMax, from its own stream.
    float Restore()
    {
        restoreRng ^= restoreRng << 13;
        restoreRng ^= restoreRng >> 17;
        restoreRng ^= restoreRng << 5;
        return RestoreMin + (RestoreMax - RestoreMin) * ((restoreRng & 0xFFFFFF) / 16777216f);
    }

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
