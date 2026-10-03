// The beats of every interaction shot, in seconds (metres and degrees where named), shared
// so the picture (map chat), the post and props (visual chat) and the sound (sound chat) never
// duplicate a time as a literal. Values come from the visual chat's audit
// (Documentation/research/interaction_audit/10_audit_report.md §3, camera language B: the
// camera moves and the objects act, no hands) with Red's head-dip direction for the key shot.
// They are estimates to tune on a capture: tell the other chats when one moves.
// Gameplay never reads a shot pose (FrontRoomsCameraRig.BaseEye only). The Camera motion
// setting scales every dip, drop, roll, shake and FOV punch (Off: takeovers play in place).

public static class FrontRoomsShotTimings
{
    // ---- shared camera rules
    public const float MaxFovDeltaDeg = 15f;      // FOV changes are deltas from the player's FOV
    public const float MinFovChangeSeconds = .4f; // except punches <= 8 deg inside takeovers
    public const float WorldClampRadius = .1f;    // spherecast for every camera offset
    public const float HudFadeSeconds = .15f;     // HUD (except captions) fades during takeovers
    public const float DoorShotBufferedPush = .2f;// one E in the last 0.2 s of a door shot = "push open"

    // ---- 3.1 Key pickup (no input lock)
    public static class KeyPickup
    {
        public const float Duration = .95f;
        public const float AimAssistPitchDeg = 6f, AimAssistSeconds = .2f; // only while the mouse is idle
        public const float TravelToHeld = .25f;        // key lifts and travels to the held pose
        public const float HeldDistance = .35f;        // metres ahead, lower right (65% across, 70% down)
        public const float Arrive = .25f;              // KeyTaken fires here (moved from pickup)
        public const float HoldUntil = .75f;
        public const float PocketStart = .75f, PocketSound = .85f, PocketEnd = .95f;
    }

    // ---- 3.2 Unlock a locked door with the key: the HEAD DIP (input lock)
    public static class Unlock
    {
        public const float Duration = 1.55f;
        public const float Commit = .86f;              // DoorUnlocked fires HERE (moved from the key press)
        public const float MoveLockEnd = 1.45f, LookLockEnd = 1.30f, LookBlendBack = .2f;
        // Head dip: the eye drops toward the lock and pitches down, then pushes in.
        public const float DipEyeDrop = .25f;          // metres below the 1.62 eye (to ~1.37); scaled by the motion setting
        public const float DipDistanceFromLock = .45f; // metres along the door normal toward the player (~0.57 m to the lock)
        public const float DipPitchDeg = 38f;          // looking down at the cylinder (lock at 1.0 m)
        public const float FovDeltaDeg = -14f;         // 76 -> 62 at the default FOV, on the same curve
        public const float TravelBase = .30f, TravelPerMetre = .12f, TravelMin = .35f, TravelMax = .55f; // cubic ease-in-out
        public const float PostBlendIn = .40f;         // shot volume: mild far blur, vignette +0.1, exposure +0.15
        public const float HandheldDriftDeg = .1f;     // 0.30-0.55 s
        public const float KeyApproach = .30f, KeyAtKeyway = .55f; // the key enters from lower right to 3 cm in front
        public const float InsertStart = .55f, InsertEnd = .68f, InsertDepth = .025f, PinCatchAt = .6f, PinCatch = .03f;
        public const float TurnStart = .68f, TurnEnd = .90f, TurnDeg = 90f, TurnResistDeg = 10f, TurnRollDeg = .2f;
        public const float BoltJoltDeg = .3f, BoltJolt = .08f, LeafShiftMetres = .0015f; // on the "Door leaf" child
        public const float KeyOutStart = .90f, KeyOutEnd = 1.15f;
        public const float AjarStart = 1.00f, AjarEnd = 1.25f, AjarDeg = 10f, AjarOvershootDeg = 1f;
        public const float ReturnStart = 1.15f, ReturnEnd = 1.55f;
        public const float CancelAfter = .2f;          // S or a second E cancels after 0.2 s (never Esc)
        public const float CancelKeyOut = .15f, CancelCameraBack = .25f;
        public const float ChaseFreeSnap = .15f;       // after Commit: Chase/sight frees the player, camera back <= 0.15 s
        public const float CancelRelayDistance = 6f;   // Relay within 6 m with sight cancels before Commit
        public const float ShotLightIntensity = .6f;   // optional unshadowed light on the camera (costs 1 of 32 WebGL lights)
    }

    // ---- 3.3 Open a door normally (no lock)
    public static class Open
    {
        public const float Duration = .75f;
        public const float PushImpulseDeg = .3f, PushImpulseDecay = .15f;
        public const float LeverDownDeg = 35f, LeverDown = .08f, LeverBackStart = .12f, LeverBackEnd = .22f;
        public const float SwingStart = .10f, SwingSeconds = .55f, SwingDeg = 95f; // lever, then the leaf: ease-out (a pushed door), not smoothstep
        public const float OvershootDeg = 2f, OvershootStart = .63f, OvershootEnd = .75f;
        public const float ShutLatchImpulseDeg = .3f;  // "E · SHUT DOOR": same grammar, the impulse on the latch
    }

    // ---- 3.4 Locked-door rattle (no key, no lock)
    public static class Rattle
    {
        public const float Duration = .40f;
        public const float LeverDeg = 20f, LeverDown = .05f;
        public const float Jolt1 = .05f, Jolt2 = .16f, JoltSpring = .06f, JoltMetres = .002f, JoltDeg = .3f, CameraImpulseDeg = .25f;
        public const float LeverBackStart = .20f, LeverBackEnd = .40f;
        public const float LockedLabelSeconds = 1.5f;
    }

    // ---- 3.5 Hold to break glass: HELD. The visual chat is redesigning the window break as a
    // micro-cutscene with staged pre-fractured glass; its Glass class comes with that work. Until then
    // the hold stays 1.0 s with the sound beats at 0.35 / 0.70 / 1.0 (FrontRoomsMapWorld.Hold).

    // ---- 3.6 Climb through the broken window (move locked, as today)
    public static class Climb
    {
        public const float Duration = .6f;             // unchanged (a longer climb is a balance change for Red)
        public const float DuckEnd = .15f, DuckMetres = .30f, PitchDownDeg = 10f, RollDeg = 3f;
        public const float Plant = .15f, PlantJoltDeg = .5f;
        public const float OverStart = .15f, OverEnd = .45f;
        public const float LandStart = .45f, LandDipMetres = .06f, Step1 = .47f, Step2 = .58f;
    }

    // ---- 3.7 The Relay breaking a door (player side; no input lock; camera only within 8 m)
    public static class DoorBreak
    {
        public const float BlowInterval = .5f, FinalBlowLead = .1f; // the last blow lands 0.1 s before the door gives
        public const float CameraRange = 8f, BlowShakeMinDeg = .2f, BlowShakeMaxDeg = .6f, BlowShake = .15f;
        public const float LeafJoltMinMetres = .004f, LeafJoltMaxMetres = .008f, LeafJoltSpring = .12f;
        public const float Damage1At = .5f, Damage2At = .8f; // fractions of breakDoorSeconds
        public const float ThrowSeconds = .12f, BounceRestDeg = 80f, CrookedDeg = 3f, BreakShakeDeg = 1.0f, BreakShakeDecay = .3f;
        public const float RevealHold = .3f;           // the Relay holds in the doorway, back-lit
    }

    // ---- 3.8 Being caught (full input lock; Phase.Dying)
    public static class Caught
    {
        public const float Duration = 2.0f;
        public const float WhipEnd = .25f, WhipFovDeg = -8f, LungeMetres = .15f;
        public const float Impact = .25f;              // CaughtImpact fires here (silence + tinnitus)
        public const float KnockBackMetres = .15f, ImpactRollDeg = 8f, ImpactRoll = .2f, ImpactShakeDeg = 2.5f;
        public const float FallStart = .45f, FallEnd = 1.05f, FallHeight = .3f, FallRollDeg = 25f;
        public const float FadeStart = 1.0f, FadeEnd = 1.8f, CardFadeIn = .4f, RestartIgnoredUntil = .6f;
    }
}
