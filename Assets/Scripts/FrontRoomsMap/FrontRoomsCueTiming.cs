// Wallpaper cue timing (owner: graphic-visual chat; read-only for the driver).
// States: 0 = live print, 1..3 = turn 30/60/90 deg (3 = flow click 0), 4..8 = flow clicks 1..5.
public static class FrontRoomsCueTiming
{
    public const int TurnStates = 3;          // M4 ratchet: three 30-degree clicks
    public const int FlowLoop = 6;            // V1: 6 clicks = 750 mm = one roll, then state 3 again
    public const float CrossFade = .07f;      // seconds per click cross-fade (snap look)
    // Stage 1 (room set, FLOW route only): click times after WarnStageChanged(1)
    public static readonly float[] StageTurnAt = { .20f, .50f, .80f };
    // Chase wave (StateChanged(Chase) after the 0.7 s hold)
    public const float WaveSpeed = 8f;        // m/s, outward from the room set
    public const float WaveTurnStep = .12f;   // s between the three turn clicks as the wave arrives, and from the third turn click to the first flow click
    public const float FlowClick = .25f;      // s per flow click (4 clicks/s)
    // Retract (any exit from {Chase, BreakDoor}); turn-back (stage 0, or after a retract)
    public const float RetractWindow = 2f;    // s, outside-in
    public const float TurnBackStep = .15f;   // s between the three turn-back clicks
    // Reduce Motion / WebGL: turn only, no flow clicks (same states, same timing)
}
