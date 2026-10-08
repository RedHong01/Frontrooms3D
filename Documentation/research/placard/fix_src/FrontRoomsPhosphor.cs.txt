using FrontRooms.Map;
using UnityEngine;

/// <summary>
/// The ZnS:Cu phosphor ink's rules, shared by everything that glows like it:
/// today the evacuation placard's legend (FrontRoomsPlacardGlow), later the
/// walls' ink if Q2 resumes. Pure functions on logical lamp levels (0..1).
/// research/wallpaper_motion/20_level_design_phosphor.md R1 (gate), R2
/// (charge and decay), R15 (stutter never glows); research/placard/10_spec.md §4.3.
///
///   wall light   W  = min(1, L + Spill · Σ L_neighbours)
///   low-pass     Ls → L with τ LsTau (0.35 s; Reduce flashing 1.0 s)
///   charge       C  rises toward W with τ 2 s, decays as C₀ / (1 + C₀ t / 8 s)
///   gate         smoothstep(0.55, 0.15, Ls): 0 lit, 1 dark
///   glow         G  = C · gate (0 under a stutter lamp)
///   emission     _EmissionColor = TintLinear · PeakEmission · G (linear, SetVector)
/// </summary>
public static class FrontRoomsPhosphor
{
    /// <summary>
    /// Peak emission k: ONE absolute value for every surface printed in this ink (the placard's legend now, the
    /// walls' driver if Q2 resumes), never tuned per spot. A full charge (G 1) adds about 0.60 k (the real start
    /// door, P1) to 0.755 k (the look-dev room) of linear luminance over the glowing pixels, so k 0.158 adds about
    /// 0.095-0.12. Measured against the lit sheet that is 32 % at P1 in the game (lit paper 0.294) and 12 % in the
    /// look-dev room (0.738): the walls' "12 % of a lit wall" cap (LD §9) is relative to each spot, this is not.
    /// Legibility in a dark cell (glyph/paper, bar 1.30): P1 with 3 lit neighbours 1.72, power failure 2.60; P2 with
    /// 2 lit neighbours: placard 35_fix.md §3. Red picks k (placard 30_render.md §9: 0.158 recommended; 0.087 just
    /// meets the bar at P1; 0.059 holds the 12 % cap at P1 and fails the bar). A public static, not a const, only so
    /// the clone's look-dev can sweep it.
    /// </summary>
    public static float PeakEmission = .158f;

    public const float GateLit = .55f;       // Ls at or above: no glow (the lamp out-shines the ink)
    public const float GateDark = .15f;      // Ls at or below: the full charge shows
    public const float Spill = .2f;          // each lit neighbour's share of the wall light
    public const float RiseTau = 2f;         // charging, seconds
    public const float DecayT = 8f;          // hyperbolic decay constant, seconds
    public const float LsTauNormal = .35f;
    public const float LsTauCalm = 1f;       // FrontRoomsSettings.ReduceFlashing

    /// <summary>
    /// ZnS:Cu pale yellow-green, sRGB (0.78, 0.95, 0.58) (Tools/print/ink_tool.py, VISUAL_CHAT_TASKS Q2),
    /// as LINEAR RGB (Rec.709 luminance 0.779). Write it with Material.SetVector, never SetColor.
    /// </summary>
    public static readonly Vector3 TintLinear = new Vector3(.5705f, .8900f, .2957f);

    public static float LsTau => FrontRoomsSettings.ReduceFlashing ? LsTauCalm : LsTauNormal;

    /// <summary>smoothstep(0.55, 0.15, ls): 0 at ls ≥ 0.55, 1 at ls ≤ 0.15.</summary>
    public static float Gate(float ls)
    {
        var t = Mathf.Clamp01((ls - GateLit) / (GateDark - GateLit));
        return t * t * (3f - 2f * t);
    }

    /// <summary>One frame of charge: up toward the wall light with τ 2 s, else the hyperbolic decay (exact at any frame rate).</summary>
    public static float Step(float charge, float wallLight, float dt) =>
        wallLight > charge
            ? charge + (wallLight - charge) * (1f - Mathf.Exp(-dt / RiseTau))
            : charge / (1f + charge * dt / DecayT);

    /// <summary>The wall light from a cell's lamp and its four neighbours' (NoLamp and darker read as 0).</summary>
    public static float WallLight(float own, float east, float west, float north, float south) =>
        Mathf.Min(1f, Mathf.Max(0f, own) + Spill * (Mathf.Max(0f, east) + Mathf.Max(0f, west) + Mathf.Max(0f, north) + Mathf.Max(0f, south)));

    /// <summary>First-order low-pass toward <paramref name="target"/>.</summary>
    public static float LowPass(float current, float target, float dt, float tau) =>
        current + (target - current) * (1f - Mathf.Exp(-dt / Mathf.Max(1e-4f, tau)));

    /// <summary>The charge a cell starts with on the frame it is built (LD §4): a dead lamp sits at its spill equilibrium, an absent one starts empty.</summary>
    public static float Initial(ModuleLamp mode, float wallLight) => mode switch
    {
        ModuleLamp.Failing => .75f,
        ModuleLamp.Dim => .5f,
        ModuleLamp.Dead => Mathf.Clamp01(wallLight),
        ModuleLamp.Off => 0f,
        _ => 1f,
    };
}

/// <summary>
/// One patch of phosphor ink's state over time (the placard's legend; a wall
/// cell's ink later): the lamp low-pass Ls, the charge C, the calm read for
/// failing cells, a peak hold and the slewed glow. Pure (no Unity objects), so
/// the photosafety tests can drive it with synthetic lamp timelines. The numbers
/// are research/placard/10_spec.md §4.3 / §4.5, plus:
/// * The hold (placard 20_build.md): the spec's "the 2/s slew turns any burst
///   train into one swell" did not hold in simulation (a Warn train at 3 Hz gave
///   5 glow pulses at 3 Hz). The target G is held at its peak for HoldNormal
///   (0.6 s; calm 1.5 s) after it starts to fall, so a train, a dead lamp's blink
///   or a failing lamp's dropout reads as one swell; then the slew brings it down.
/// * The calm failing read (placard 35_fix.md §2). With Reduce flashing a Failing
///   cell shows, steadily, the strength the normal path reaches in its pulses:
///   the normal path's target C · gate(Ls at τ 0.35 s) feeds a peak follower
///   (instant attack, release τ 3 s), which is low-passed with τ 8 s, then held
///   and slewed at 0.5/s. The 2026-10-07 build gated the 3 s mean of the LAMP:
///   the gate is steep and the mean lamp (about 0.47) sits almost on its closed
///   end, so the legend vanished (G 0.087 in the game against 0.92 normal). A
///   3 s mean of the target was tried as well: 0.14, half the normal path's
///   time-average, too faint. The follower gives about 0.32 with 2 lit
///   neighbours (normal path: pulses to 0.78, mean 0.27), under 0.05 in any 2 s
///   window.
/// </summary>
public sealed class FrontRoomsPhosphorCell
{
    public const float SlewNormal = 2f, SlewCalm = .5f;    // G per second
    public const float HoldNormal = .6f, HoldCalm = 1.5f;  // seconds a peak of the target is held
    public const float CalmPeakRelease = 3f;               // s: the calm failing read's peak follower lets go with this τ
    public const float CalmFailingTau = 8f;                // s: low-pass on the follower (Reduce flashing, Failing)

    float holdLeft;

    public bool Primed { get; private set; }
    public float SmoothedLevel { get; private set; }       // Ls (τ 0.35 s; Reduce flashing 1.0 s)
    public float NormalLevel { get; private set; }         // Ls at the normal τ 0.35 s, always (feeds the calm failing read)
    public float Envelope { get; private set; }            // peak follower on the normal-path target C · gate(NormalLevel)
    public float CalmRead { get; private set; }              // Envelope low-passed 8 s: what a calm failing cell shows
    public float Charge { get; private set; }               // C
    public float Target { get; private set; }               // G before the hold and slew
    public float Held { get; private set; }                 // the held target the slew follows
    public float Shown { get; private set; }                // G after the slew

    public void Reset()
    {
        Primed = false;
        SmoothedLevel = NormalLevel = Envelope = CalmRead = Charge = Target = Held = Shown = holdLeft = 0f;
    }

    /// <summary>
    /// One frame. <paramref name="level"/> is the cell's own logical lamp level L,
    /// <paramref name="wallLight"/> W (FrontRoomsPhosphor.WallLight), <paramref name="mode"/>
    /// its temperament, <paramref name="calm"/> FrontRoomsSettings.ReduceFlashing. Returns the shown G.
    /// The first call primes the state (initial charge by temperament, Ls = L).
    /// </summary>
    public float Tick(float level, float wallLight, ModuleLamp mode, bool calm, float dt)
    {
        if (!Primed)
        {
            Charge = FrontRoomsPhosphor.Initial(mode, wallLight);
            SmoothedLevel = NormalLevel = level;
            Envelope = CalmRead = mode == ModuleLamp.Stutter ? 0f : Charge * FrontRoomsPhosphor.Gate(level);
            Primed = true;
        }
        if (dt <= 0f) return Shown;
        SmoothedLevel = FrontRoomsPhosphor.LowPass(SmoothedLevel, level, dt, calm ? FrontRoomsPhosphor.LsTauCalm : FrontRoomsPhosphor.LsTauNormal);
        NormalLevel = FrontRoomsPhosphor.LowPass(NormalLevel, level, dt, FrontRoomsPhosphor.LsTauNormal);
        Charge = FrontRoomsPhosphor.Step(Charge, wallLight, dt);
        // The calm failing read runs every frame, so it has already settled when Reduce flashing is switched on
        // or the lamp turns Failing (35_fix.md §2).
        var normalTarget = mode == ModuleLamp.Stutter ? 0f : Charge * FrontRoomsPhosphor.Gate(NormalLevel);
        Envelope = normalTarget > Envelope ? normalTarget : FrontRoomsPhosphor.LowPass(Envelope, normalTarget, dt, CalmPeakRelease);
        CalmRead = FrontRoomsPhosphor.LowPass(CalmRead, Envelope, dt, CalmFailingTau);
        if (mode == ModuleLamp.Stutter) Target = 0f;
        else if (calm && mode == ModuleLamp.Failing) Target = CalmRead;
        else Target = Charge * FrontRoomsPhosphor.Gate(SmoothedLevel);
        if (Target >= Held || mode == ModuleLamp.Stutter) { Held = Target; holdLeft = calm ? HoldCalm : HoldNormal; }
        else if ((holdLeft -= dt) <= 0f) Held = Target;
        Shown = Mathf.MoveTowards(Shown, Held, (calm ? SlewCalm : SlewNormal) * dt);
        return Shown;
    }
}
