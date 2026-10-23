using System;

/// <summary>
/// Semantic handheld interaction signals shared by the touch layer and the
/// game. The bus carries game intent rather than UI details, so it feeds the
/// haptics now and can feed accessibility, telemetry or a tutorial later.
/// Haptics fire from these game events, never from audio callbacks (agreed
/// with 声音设计, AUDIO_CONTRACT).
/// </summary>
public static class FrontRoomsMobileInteractionEvents
{
    public enum Kind
    {
        UsePressed,
        UseReleased,
        RunStarted,
        RunEnded,
        GlassHoldStarted,
        GlassBeat,
        GlassShattered,
        Caught,
        MenuConfirmed,
        Winded,
        DoorLatched,
        DoorRattled,
        GlassCracked,
        LockOn,
        DoorBrokenTried,
        InspectOpened
    }

    /// <summary>
    /// Raised after a signal: the bool is whether the stick runs (other kinds
    /// pass false) and the float the glass progress or crack stage.
    /// </summary>
    public static event Action<Kind, bool, float> Raised;

    public static void UsePressed()
    {
        FrontRoomsMobileHaptics.UsePressed();
        Emit(Kind.UsePressed);
    }

    public static void UseReleased() => Emit(Kind.UseReleased);

    /// <summary>The thumb crossed the run line (or the RUN button went on): one firm click says "running" (Red, 2026-10-08).</summary>
    public static void RunChanged(bool running)
    {
        if (running) FrontRoomsMobileHaptics.RunStarted();
        Emit(running ? Kind.RunStarted : Kind.RunEnded, running);
    }

    public static void Winded()
    {
        FrontRoomsMobileHaptics.Winded();
        Emit(Kind.Winded);
    }

    public static void GlassHoldStarted() => Emit(Kind.GlassHoldStarted);

    public static void GlassBeat(float progress)
    {
        FrontRoomsMobileHaptics.GlassBeat(progress);
        Emit(Kind.GlassBeat, false, progress);
    }

    public static void GlassCracked(int stage)
    {
        FrontRoomsMobileHaptics.GlassCracked(stage);
        Emit(Kind.GlassCracked, false, stage);
    }

    public static void GlassShattered()
    {
        FrontRoomsMobileHaptics.GlassShattered();
        Emit(Kind.GlassShattered);
    }

    public static void DoorLatched()
    {
        FrontRoomsMobileHaptics.DoorLatched();
        Emit(Kind.DoorLatched);
    }

    public static void DoorRattled(float jolt1, float jolt2)
    {
        FrontRoomsMobileHaptics.DoorRattle(jolt1, jolt2);
        Emit(Kind.DoorRattled);
    }

    /// <summary>A try on a broken door (the map answered Broken): it won't shut, and the knock comes at <paramref name="knock"/> s.</summary>
    public static void DoorBrokenTried(float knock)
    {
        FrontRoomsMobileHaptics.DoorBrokenTried(knock);
        Emit(Kind.DoorBrokenTried);
    }

    /// <summary>A close-up opened (EXAMINE): felt like a door latch.</summary>
    public static void InspectOpened()
    {
        FrontRoomsMobileHaptics.InspectOpened();
        Emit(Kind.InspectOpened);
    }

    public static void LockOn()
    {
        FrontRoomsMobileHaptics.LockOn();
        Emit(Kind.LockOn);
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

    /// <summary>Clears scene-owned subscribers when tests or a full reload need a clean bus.</summary>
    public static void ResetSubscribers()
    {
        Raised = null;
        FrontRoomsMobileHaptics.Reset();
    }

    static void Emit(Kind kind, bool state = false, float value = 0f) => Raised?.Invoke(kind, state, value);
}
