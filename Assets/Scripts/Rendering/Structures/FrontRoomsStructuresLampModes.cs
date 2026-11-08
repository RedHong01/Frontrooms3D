using UnityEngine;

/// <summary>
/// The game's lamp modes for scene 4's own lamps (structures_assets 30_plan.md P7, ask 5; line visual (game visual), 2026-11-06). Red's ask 5
/// is open: "B: the game's tier-1 modes" is built as the default, "A: all steady" (today) on a key the K1 builder binds
/// (GameModes). Each lamp rolls its temperament once from its own seed at the game's tier-1 odds (FrontRoomsTier's defaults:
/// 62 % steady, 20 stutter, 10 failing, 5 dead, 3 dim, with the RELAY 12 caps), then Advance gives its level every frame with
/// the game's photosafe shapes (FrontRoomsMapWorld's constants, read from there): a stutter is an episode of 2 or 3 dips, one
/// per 0.55 s slot, to 0.5-0.6 over 60-80 ms ramps, at least 3.7 s apart; a failing lamp's wave never settles and drops out
/// to 5 % for at least 0.34 s, at least 1.1 s apart; a dead tube strikes once in a long while (never two within 4 s); a dim
/// one hums at 42 %. A per-lamp limiter holds back any change that would make a 4th flash within a second (WCAG 2.3.1), and
/// Reduce Flashing (FrontRoomsSettings) turns every dip into one slow sag, as in the game. The clocks and the RNG advance
/// exactly as before whichever way, so a lamp is deterministic from its seed.
/// Pool lamps stay steady until the 0d glint finding is fixed (steadyOnly; research/structures_assets 20_research.md C.3.2):
/// the pool districts' fluorescents and every lamp over water roll Steady.
/// The levels are the caller's to apply: the K1 builder multiplies its light's intensity, its lens drive (and a far pool's,
/// FrontRoomsStructuresLamps), never through Look.Scale (that re-captures the district's probes on every change: a
/// flickering lamp must not); the probes are captured with every lamp at its steady level. This file creates no light and
/// keeps no scene state: it is inert until a caller uses it. ASCII only.
/// </summary>
public static class FrontRoomsStructuresLampModes
{
    /// <summary>The game's Auto lamp temperaments (FrontRoomsMapWorld fixture modes 0..4).</summary>
    public enum Mode : byte { Steady, Stutter, Failing, Dead, Dim }

    /// <summary>Ask 5: true = B, the game's tier-1 modes (the recommendation, built as the default while Red decides); false =
    /// A, every lamp steady (today's scene 4). The K1 builder binds a key to SetGameModes.</summary>
    public static bool GameModes { get; private set; } = true;

    /// <summary>Bumps whenever GameModes flips, so a builder can re-apply its lamps once.</summary>
    public static int Version { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        GameModes = true;
        Version = 0;
    }

    public static void SetGameModes(bool on)
    {
        if (GameModes == on) return;
        GameModes = on;
        Version++;
    }

    /// <summary>One lamp's state: its mode and the timers and RNG of the game's fixture, plus the flash limiter's track.</summary>
    public sealed class State
    {
        public Mode mode;
        public float clock, phase, level = 1f;
        public uint rng;
        public float nextEvent, eventEnd, episodeStart;
        public byte episodeDips;
        public uint episodeSeed;
        public float gain = 1f;
        public bool dropping;
        public float dropStart = float.NegativeInfinity, dropEnd = float.NegativeInfinity;
        public float lastStrike = float.NegativeInfinity;
        public bool strikeShown;
        // the flash limiter (FrontRoomsMapWorld.Limit): turning points and the times of the last changes
        public bool begun, counted;
        public int dir, count, head;
        public float pivot, ext, lo, hi, last;
        public readonly float[] times = new float[FrontRoomsMapWorld.FlashMaxTransitions];
    }

    static FrontRoomsTier tier1;

    /// <summary>The game's tier-1 odds (FrontRoomsTier's defaults, capped as the game caps them).</summary>
    public static FrontRoomsTier Tier1 => tier1 ?? (tier1 = new FrontRoomsTier());

    /// <summary>
    /// Rolls a lamp's temperament from its seed (any stable int: the district key and the lamp's index, or its position hashed)
    /// at the game's tier-1 odds; <paramref name="steadyOnly"/> gives Steady (pool districts until 0d, view-only show lamps).
    /// The phase and first event are drawn as the map draws them, so two lamps never pulse together.
    /// </summary>
    public static State Roll(int seed, bool steadyOnly)
    {
        var s = new State { rng = Mix((uint)seed ^ 0x51ed27u) | 1u };
        var roll = Rand(ref s.rng);
        s.mode = steadyOnly ? Mode.Steady : (Mode)Mathf.Clamp(Tier1.LampMode(roll), 0, 4);
        s.phase = Rand(ref s.rng) * 50f;
        s.nextEvent = 2f + Rand(ref s.rng) * 14f;
        return s;
    }

    /// <summary>
    /// Advances a lamp by <paramref name="dt"/> seconds and returns its level (0..1 of the lamp's full output), the game's
    /// formula for its mode through the flash limiter. With GameModes off every lamp reads 1 (today); its clock still runs.
    /// </summary>
    public static float Advance(State s, float dt)
    {
        if (s == null) return 1f;
        s.clock += Mathf.Max(0f, dt);
        var v = Level(s);
        s.level = GameModes ? v : 1f;
        return s.level;
    }

    /// <summary>True while the lamp is in one of its events (a stutter episode, a dropout, a strike): the caller may tick such
    /// lamps every frame and the rest at the budget's 10 Hz.</summary>
    public static bool Busy(State s) =>
        s != null && (s.mode == Mode.Stutter ? s.episodeDips > 0 : s.mode == Mode.Failing ? true : s.mode == Mode.Dead && s.clock < s.eventEnd);

    // ---------------------------------------------------------------- the game's shapes (FrontRoomsMapWorld.Level and its parts)

    static float Level(State f)
    {
        var t = f.clock + f.phase;
        var calm = FrontRoomsSettings.ReduceFlashing;
        switch (f.mode)
        {
            case Mode.Stutter: return Limit(f, Stutter(f, t, calm));
            case Mode.Failing: return Limit(f, Failing(f, t, calm));
            case Mode.Dead: return Limit(f, DeadBlink(f, calm));
            case Mode.Dim: return .42f + .03f * Mathf.Sin(t * 90f);
            default: return .98f + .02f * Mathf.Sin(t * 110f);
        }
    }

    static float Stutter(State f, float t, bool calm)
    {
        if (f.clock >= f.nextEvent && f.eventEnd <= f.clock)
        {
            f.eventEnd = f.clock + .15f + Rand(ref f.rng) * .7f;
            f.nextEvent = f.eventEnd + 5f + Rand(ref f.rng) * 15f;
            f.episodeStart = f.clock;
            f.episodeDips = (byte)(f.eventEnd - f.clock < .5f ? 2 : 3);
            f.episodeSeed = f.rng;
        }
        if (f.episodeDips == 0) return .97f + .03f * Mathf.Sin(t * 120f);
        f.gain = StutterGain(f, f.clock - f.episodeStart, calm);
        return (.97f + .03f * Mathf.Sin(t * 120f)) * f.gain;
    }

    static float StutterGain(State f, float u, bool calm)
    {
        var n = f.episodeDips;
        float down, hold, up, depth;
        if (calm)
        {
            StutterDip(f, n - 1, out down, out hold, out up, out depth);
            var until = (n - 1) * FrontRoomsMapWorld.StutterSlot + down + hold + up;
            var env = FrontRoomsMapWorld.ReducedFlashingEnvelopeSeconds;
            if (u >= Mathf.Max(until, env) + env) { f.episodeDips = 0; return 1f; }
            return ReducedSag(u, until);
        }
        var k = Mathf.Min((int)(u / FrontRoomsMapWorld.StutterSlot), n - 1);
        var x = u - k * FrontRoomsMapWorld.StutterSlot;
        StutterDip(f, k, out down, out hold, out up, out depth);
        if (x >= down + hold + up)
        {
            if (k == n - 1) f.episodeDips = 0;
            return 1f;
        }
        if (x < down) return Mathf.Lerp(1f, depth, x / down);
        if (x < down + hold) return depth;
        return Mathf.Lerp(depth, 1f, (x - down - hold) / up);
    }

    static void StutterDip(State f, int k, out float down, out float hold, out float up, out float depth)
    {
        var seed = (int)f.episodeSeed;
        down = Mathf.Lerp(FrontRoomsMapWorld.StutterRampMin, FrontRoomsMapWorld.StutterRampMax, Unit(Hash(seed, 0, 0, 239)));
        hold = Mathf.Lerp(FrontRoomsMapWorld.StutterHoldMin, FrontRoomsMapWorld.StutterHoldMax, Unit(Hash(seed, 0, 1, 239)));
        up = Mathf.Lerp(FrontRoomsMapWorld.StutterRampMin, FrontRoomsMapWorld.StutterRampMax, Unit(Hash(seed, 0, 2, 239)));
        depth = FrontRoomsMapWorld.StutterFloor + FrontRoomsMapWorld.StutterFloorSpread * Unit(Hash(seed, k, 3, 239));
    }

    static float Failing(State f, float t, bool calm)
    {
        var wave = .5f + .5f * Mathf.Sin(t * 3.1f) * (.7f + .3f * Mathf.Sin(t * 1.27f));
        var want = Mathf.PerlinNoise(t * 1.4f, f.phase) > .72f;
        if (f.dropping)
        {
            if (!want && f.clock - f.dropStart >= FrontRoomsMapWorld.FailingHoldMin) { f.dropping = false; f.dropEnd = f.clock; }
        }
        else if (want && f.clock - f.dropEnd >= FrontRoomsMapWorld.FailingGap) { f.dropping = true; f.dropStart = f.clock; }
        var env = FrontRoomsMapWorld.ReducedFlashingEnvelopeSeconds;
        if (!calm) f.gain = f.dropping ? .05f : 1f;
        else if (f.dropping) f.gain = ReducedSag(f.clock - f.dropStart, float.PositiveInfinity);
        else if (float.IsNegativeInfinity(f.dropStart)) f.gain = 1f;
        else
        {
            var until = f.dropEnd - f.dropStart;
            var u = f.clock - f.dropStart;
            f.gain = u < Mathf.Max(until, env) + env ? ReducedSag(u, until) : 1f;
        }
        return (.25f + .45f * wave) * f.gain;
    }

    static float DeadBlink(State f, bool calm)
    {
        if (f.clock >= f.nextEvent && f.eventEnd <= f.clock)
        {
            f.eventEnd = f.clock + .06f + Rand(ref f.rng) * .12f;
            f.nextEvent = f.eventEnd + 8f + Rand(ref f.rng) * 20f;
            f.strikeShown = f.clock - f.lastStrike >= FrontRoomsMapWorld.DeadStrikeGap;
            if (f.strikeShown) f.lastStrike = f.clock;
        }
        return f.clock < f.eventEnd && !calm && f.strikeShown ? .8f : 0f;
    }

    static float ReducedSag(float u, float until)
    {
        var r = FrontRoomsMapWorld.ReducedFlashingEnvelopeSeconds;
        var down = Smooth01(u / r);
        var back = Smooth01((u - Mathf.Max(until, r)) / r);
        return 1f - (1f - FrontRoomsMapWorld.ReducedAmbientDepth) * down * (1f - back);
    }

    static float Smooth01(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    // The flash limiter (FrontRoomsMapWorld.Limit, section 9.5): a swing of FlashStep or more between turning points whose darker
    // side is under FlashDark is one change; a level that would make the 7th change (a 4th flash) within FlashWindow is held.
    static float Limit(State z, float v)
    {
        const float Step = FrontRoomsMapWorld.FlashStep, Dark = FrontRoomsMapWorld.FlashDark, Window = FrontRoomsMapWorld.FlashWindow;
        const int Max = FrontRoomsMapWorld.FlashMaxTransitions;
        if (!z.begun)
        {
            z.begun = true;
            z.dir = 0;
            z.pivot = z.ext = z.lo = z.hi = z.last = v;
            z.counted = false;
            return v;
        }
        var dir = z.dir;
        float pivot = z.pivot, ext = z.ext, lo = z.lo, hi = z.hi;
        var counted = z.counted;
        if (dir == 0)
        {
            lo = Mathf.Min(lo, v);
            hi = Mathf.Max(hi, v);
            if (v - lo >= Step) { dir = 1; pivot = lo; ext = v; counted = false; }
            else if (hi - v >= Step) { dir = -1; pivot = hi; ext = v; counted = false; }
        }
        else if (dir > 0)
        {
            if (v > ext) ext = v;
            else if (ext - v >= Step) { dir = -1; pivot = ext; ext = v; counted = false; }
        }
        else
        {
            if (v < ext) ext = v;
            else if (v - ext >= Step) { dir = 1; pivot = ext; ext = v; counted = false; }
        }
        var change = dir != 0 && !counted && (dir > 0 ? ext - pivot >= Step && pivot < Dark : pivot - ext >= Step && ext < Dark);
        if (change && z.count == Max && z.times[z.head] >= z.clock - Window - .001f) return z.last;
        z.dir = dir;
        z.pivot = pivot;
        z.ext = ext;
        z.lo = lo;
        z.hi = hi;
        z.counted = counted || change;
        if (change)
        {
            if (z.count < Max) z.times[(z.head + z.count++) % Max] = z.clock;
            else
            {
                z.times[z.head] = z.clock;
                z.head = (z.head + 1) % Max;
            }
        }
        z.last = v;
        return v;
    }

    // ---------------------------------------------------------------- the map's RNG and hash (same functions, local copies)

    static float Rand(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return (state & 0xffffff) / 16777216f;
    }

    static uint Mix(uint h)
    {
        unchecked
        {
            h ^= h >> 16; h *= 0x7feb352du;
            h ^= h >> 15; h *= 0x846ca68bu;
            h ^= h >> 16;
            return h;
        }
    }

    static uint Hash(int seed, int a, int b, int salt)
    {
        unchecked
        {
            var h = Mix((uint)seed ^ 0x9e3779b9u);
            h = Mix(h ^ (uint)a * 0x85ebca6bu);
            h = Mix(h ^ (uint)b * 0xc2b2ae35u);
            h = Mix(h ^ (uint)salt * 0x27d4eb2fu);
            return Mix(h);
        }
    }

    static float Unit(uint h) => (h & 0xffffff) / 16777216f;
}
