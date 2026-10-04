using System;

/// <summary>
/// Semantic mobile interaction signals shared by the touch surface and the
/// game.  The bus carries game intent, rather than UI implementation details,
/// so it can later feed haptics, accessibility, telemetry, or tutorial UI.
/// </summary>
public static class FrontRoomsMobileInteractionEvents
{
    public enum Kind
    {
        UsePressed,
        UseReleased,
        SprintLatched,
        SprintReleased,
        GlassHoldStarted,
        GlassBeat,
        GlassShattered,
        Caught,
        MenuConfirmed
    }

    /// <summary>
    /// Raised after a semantic signal is emitted. The bool payload is used by
    /// sprint events; other kinds pass false. Progress is used by glass beats.
    /// </summary>
    public static event Action<Kind, bool, float> Raised;

    public static void UsePressed()
    {
        FrontRoomsMobileHaptics.UsePressed();
        Emit(Kind.UsePressed);
    }

    public static void UseReleased()
    {
        FrontRoomsMobileHaptics.UseReleased();
        Emit(Kind.UseReleased);
    }

    public static void SprintLatched(bool latched)
    {
        FrontRoomsMobileHaptics.Play(latched
            ? FrontRoomsMobileHaptics.Pulse.SprintLatched
            : FrontRoomsMobileHaptics.Pulse.SprintReleased);
        Emit(latched ? Kind.SprintLatched : Kind.SprintReleased, latched);
    }

    public static void GlassHoldStarted()
    {
        Emit(Kind.GlassHoldStarted);
    }

    public static void GlassBeat(float progress)
    {
        FrontRoomsMobileHaptics.GlassBeat();
        Emit(Kind.GlassBeat, false, progress);
    }

    public static void GlassShattered()
    {
        FrontRoomsMobileHaptics.GlassShattered();
        Emit(Kind.GlassShattered);
    }

    public static void Caught()
    {
        FrontRoomsMobileHaptics.Caught();
        Emit(Kind.Caught);
    }

    public static void MenuConfirmed()
    {
        FrontRoomsMobileHaptics.MenuConfirm();
        Emit(Kind.MenuConfirmed);
    }

    /// <summary>
    /// Clears subscribers owned by a scene when tests or a full reload need a
    /// deterministic bus. Normal gameplay does not need to call this.
    /// </summary>
    public static void ResetSubscribers()
    {
        Raised = null;
        FrontRoomsMobileHaptics.Reset();
    }

    static void Emit(Kind kind, bool state = false, float progress = 0f)
    {
        Raised?.Invoke(kind, state, progress);
    }
}
