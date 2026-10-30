using System;
using UnityEngine;
using UnityEngine.UI;
using HM = FrontRoomsKeyHudMotion;

/// <summary>
/// The HUD key and the crosshair's states (the visual chat, Documentation/research/ui_key_icon/hud_build).
/// graphic design's design (lock_motion/README.md, hud_build/DECISIONS.md #1-#33) implemented as specified:
///  - the crosshair: the dot, and a small REGISTRY of states built from the dot (DECISIONS #16): the zone
///    padlock (L3.D in, D.3 DENY.tugShake on no key, the has-key swing and the F.1 drop, famOut to leave),
///    the broken door (B.4), the look-eye (E.1 "Opens from the dot", #17) for READ / EXAMINE targets; each
///    with its build, its reaction (a window that cannot retrigger) and its leave; both prompt lines (paper,
///    1 px ink keyline) while a state shows;
///  - priority and switching (#19, crosshair grammar sec. 4): Hidden (the HUD's alpha) > a latched reaction >
///    the aimed kind (Locked > Broken > Use > Hold > Read, the map's ray picks one) > Idle. A latched
///    reaction keeps the dot until its window ends; a state whose target goes away leaves, cut to 0.12 s
///    when another state (or a map prompt) waits; a re-aim mid-leave resumes from where it is;
///  - photosafety (grammar sec. 5): the eye blinks once per build, only after the aim left AND >= 1.0 s since the
///    last blink; Reduce Flashing = no blink; Reduce Motion = every state an alpha cross-fade of 0.15 s;
///  - the key panel (direction B: the key on its tag; the tag carries the FLOOR token 00, B1, B2 ..., #28/#32/#33; the
///    glyph has the same 1 px keyline as the padlock, #22), the P4 pickup flight from the held spot (the first
///    key of a run takes off when its EXAMINE hold ends, #18), and the U.3 unlock gesture on the panel key,
///    synced to the unlock shot's beats with the panel exempt from the shot's fade (mode (d), #13-#15).
/// The map decides everything about doors, keys and close-ups and tells this component what the aim is on and
/// when things happen (the seam: SetAim / SetHeldKey / SetHudAlpha every frame; NoKey / Unlocking / BrokenTry /
/// InspectOpened / InspectClosed / PickupFlight as events). Every curve is FrontRoomsKeyHudMotion (a port of
/// the lab). All motion runs on UNSCALED time, so it keeps moving under timeScale 0 (EXAMINE, pause). No
/// per-frame allocation (strings are built only when a line's words change). The keyline ink and alpha are the shared
/// FrontRoomsHudStyle (#31). The sound line hears the motions through the static SoundCue event (#34).
/// </summary>
public sealed class FrontRoomsKeyHud : MonoBehaviour
{
    public enum Shape { Rect, Round, Long }
    public enum Tint { Red, Blue, White }
    /// <summary>
    /// What the crosshair is on (from the map's AimKind, grammar H1; append only). Locked covers the map's Locked
    /// and LockedHaveKey (haveKey picks the lines). Read / Examine: EXAMINE's targets (the eye). Use (Open, Shut,
    /// TakeKey) and Hold (Glass, GlassTap) have no crosshair state: the dot stays and the map's prompt shows, but
    /// they count as a state waiting when another one leaves (grammar sec. 4).
    /// </summary>
    public enum Aim { None, Locked, Broken, Read, Examine, Use, Hold }

    [Serializable]
    public struct Identity : IEquatable<Identity>
    {
        public Shape shape;
        public Tint tint;
        /// <summary>The FLOOR the key opens out of (DECISIONS #28/#32/#33): 0 = LEVEL 0, shown "00"; n = Bn ("B1" .. "B99").</summary>
        public int number; // 0..99
        public Identity(Shape s, Tint t, int n) { shape = s; tint = t; number = Mathf.Clamp(n, 0, 99); }
        public bool Equals(Identity o) { return shape == o.shape && tint == o.tint && number == o.number; }
        public override bool Equals(object o) { return o is Identity && Equals((Identity)o); }
        public override int GetHashCode() { return ((int)shape * 3 + (int)tint) * 100 + number; }
        public override string ToString() { return shape + " " + tint + " " + FloorToken(number); }
    }

    /// <summary>The floor token on the key's tag and in the padlock (DECISIONS #32/#33): "00" for LEVEL 0, then "B1" ..
    /// "B9"; from "B10" on the three characters are set at 0.8x on the same baseline (the parts atlas does that).</summary>
    public static string FloorToken(int floor)
    {
        floor = Mathf.Clamp(floor, 0, 99);
        return floor == 0 ? "00" : TokenB[floor];
    }
    static readonly string[] TokenB = MakeTokens();
    static string[] MakeTokens() { var t = new string[100]; for (var i = 0; i < 100; i++) t[i] = i == 0 ? "00" : "B" + i; return t; }

    // ------------------------------------------------------------------ decisions kept as parameters
    public enum UnlockStartMode { A_OnKeyTurn, B_AfterShot, C_ExemptFromShotFade }
    public enum PanelGestureMode { B_AfterShot, D_ShotBeats }
    /// <summary>DECISIONS #13: when the crosshair padlock's has-key sequence plays relative to the unlock shot.
    /// The crosshair keeps (b): hidden during the shot, back open at the HUD's return, then F.1. (a)/(c) stay parameters.</summary>
    public static UnlockStartMode UnlockStart = UnlockStartMode.B_AfterShot;
    /// <summary>DECISIONS #13 (DECIDED (d), Red 2026-10-21) / #14 / #15: the panel key's U.3 runs on the shot's
    /// beats (KeyInsert, KeyTurn, BoltRetract) with the panel exempt from the shot's fade (PanelShotExempt).
    /// (b) (after the shot) stays a parameter.</summary>
    public static PanelGestureMode PanelMode = PanelGestureMode.D_ShotBeats;
    /// <summary>P4 start scale factors at 1080p: 3, 3 (DECISIONS #24: they stay 3x, 1.117 at 402 pt, until D1.6's real key
    /// model replaces the cube; then a held-key frame is measured into held_pose/measure.json). PENDING.</summary>
    public static float PickupFactorX = 3f, PickupFactorY = 3f;
    public enum EyeVariant { E1_OpensFromTheDot, E3_ReadingEye }
    /// <summary>DECISIONS #17/#30: the Read state is E.1. E.3 "Reading eye" (the lowered, heavy-lidded gaze) is a PARAMETER
    /// fallback, used only if playtesters misread E.1 (lab LOOK[2]).</summary>
    public static EyeVariant Eye = EyeVariant.E1_OpensFromTheDot;
    /// <summary>(b): the fall starts this long after the HUD returns (lab in-context code; O-19).</summary>
    public const float ReturnDropDelay = .10f;
    /// <summary>(d): a cancel before the commit eases the panel key back to rest over this long.</summary>
    public const float GestureCancelEase = .15f;
    /// <summary>The eye waits in its close-up state for InspectClosed. If the HUD is fully visible again for this
    /// long without it, the close-up is taken as over (a safety net; EXAMINE must call InspectClosed).</summary>
    public const float CloseUpSafetySeconds = 2f;

    /// <summary>The strings, final (DECISIONS #29, research/desktop_ui/FIX_CARD_VL217.md sec. B): one constant each. "." is
    /// IBM Plex Mono in line 1 (Bayon has none). The floor token is on the tag only, never in a line.</summary>
    public static class Strings
    {
        public const string Locked = "LOCKED";
        public const string NeedsKey = "NEEDS STAIR KEY";
        public const string Unlock = "E  \u00B7  UNLOCK";
        public const string HasKey = "STAIR KEY";
        /// <summary>Line 1 on a broken door; line 2 WontShut shows only during the 0.55 s won't-shut reaction (#29).</summary>
        public const string Broken = "BROKEN";
        public const string WontShut = "WON'T SHUT";
        public const string PanelKey = "STAIR KEY";
        public const string PanelMeta = "OPENS THE STAIR DOOR";
        /// <summary>The eye's line if the map passes no words (it should always pass its Describe words, EXAMINE G1:
        /// "E  .  READ PLAN", "E  .  READ SIGN", "E  .  EXAMINE KEY"; the line and the state are one predicate).</summary>
        public const string ReadFallback = "E  \u00B7  READ";
        public const string ExamineFallback = "E  \u00B7  EXAMINE";
    }

    // ------------------------------------------------------------------ the sound cues (DECISIONS #34, with the sound line)
    /// <summary>
    /// DECISIONS #34: raised on the HUD's own clock at the START of each motion, once per motion, for the sound line's
    /// 2D UI/* events (the offsets inside a motion are FMOD's): PickupFlight when the P4 flight leaves (KP-1b: after the
    /// glyph's 0.5 s hold), PickupLand when the key lands in the panel's slot, LockTug when the D.3 tug starts (E with no
    /// key), UnlockDrop when the open padlock starts its F.1 drop (never for a cancelled unlock), BrokenBuild when the
    /// B.4 icon starts to build, BrokenKnock when the won't-shut reaction starts (E on a broken door). Raised under Reduce
    /// Motion too, where the cross-fade that replaces the motion starts (the pickup: PickupFlight as the panel starts to
    /// fade in at its slot, PickupLand when it is in, 0.15 s later). Never while the pause card is up
    /// (FrontRooms3DGame.Paused) or a close-up is open (FrontRooms3DGame.InspectingNow): a motion that starts then raises
    /// nothing, then or later. A motion the HUD does not draw (no vector shader, an event a latched reaction swallows, a
    /// flight dropped before it left or landed) raises nothing. Invoked on the main thread inside LateUpdate; no allocation.
    /// </summary>
    public static event System.Action<FrontRoomsHudSoundCue> SoundCue;

    // ------------------------------------------------------------------ the editor clock and hooks (harness only)
#if UNITY_EDITOR
    /// <summary>Harness only: the clock (seconds) every phase reads instead of Time.unscaledTime.</summary>
    public static Func<float> EditorClock;
    /// <summary>Harness only: replaces the map's SetAim (the clone has no EXAMINE registry, so Read / Examine are injected here).</summary>
    public static bool EditorAimOverride;
    public static Aim EditorAim;
    public static Identity EditorAimId;
    public static bool EditorAimHaveKey;
    public static string EditorAimText;
    /// <summary>Harness only: -1 = the game's setting, 0 = off, 1 = on.</summary>
    public static int EditorReduceFlashing = -1, EditorReduceMotion = -1;
    /// <summary>Harness only: StubIdentity's answer, shape * 1000 + colour * 100 + number.</summary>
    public static Func<FrontRooms.Map.GridCoord, int> EditorIdentityOverride;
#endif
    static float Now
    {
        get
        {
#if UNITY_EDITOR
            if (EditorClock != null) return EditorClock();
#endif
            return Time.unscaledTime;
        }
    }
    /// <summary>Settings: Reduce Flashing (no blink), read when a state is entered.</summary>
    static bool ReduceFlashing
    {
        get
        {
#if UNITY_EDITOR
            if (EditorReduceFlashing >= 0) return EditorReduceFlashing > 0;
#endif
            return FrontRoomsSettings.ReduceFlashing;
        }
    }
    /// <summary>Reduce Motion (the OS switch on touch, or Camera motion 0 %): alpha cross-fades only, read when a state is entered.</summary>
    static bool ReduceMotion
    {
        get
        {
#if UNITY_EDITOR
            if (EditorReduceMotion >= 0) return EditorReduceMotion > 0;
#endif
            return FrontRoomsHandheld.ReduceMotion;
        }
    }

    // ------------------------------------------------------------------ colours (sRGB hex from the masters)
    static readonly Color Paper = Hex(0xF4F1E8), NumInk = Hex(0x1E1D1A), DotWhite = Hex(0xFFFFFF);   // the keyline ink: FrontRoomsHudStyle
    static readonly Color Yellow = Hex(0xF4DF3B);   // the panel's line 2 ink: FrontRoomsHudStyle.TextPaper (#39 (1); was the grey meta #BDBAB0)
    static readonly Color[] ZoneColour = { Hex(0x8C0F0D), Hex(0x0F2973), Hex(0xFFFFFF) };
    static Color Hex(int h) { return new Color(((h >> 16) & 255) / 255f, ((h >> 8) & 255) / 255f, (h & 255) / 255f, 1f); }
    static Vector4 Lin(Color c) { var l = c.linear; return new Vector4(l.r, l.g, l.b, 1f); }

    // ------------------------------------------------------------------ the seam
    /// <summary>Builds both roots (crosshair and panel) on the map's HUD. Returns null when it cannot.</summary>
    public static FrontRoomsKeyHud Attach(Canvas hudCanvas, RectTransform crosshair, RectTransform keyPanel, bool handheld)
    {
        if (hudCanvas == null || crosshair == null || keyPanel == null)
        {
            Debug.LogWarning("[FrontRoomsKeyHud] Attach: missing canvas, crosshair or key panel; the map's HUD stays as is.");
            return null;
        }
        var go = new GameObject("KeyHud", typeof(RectTransform));
        go.transform.SetParent(hudCanvas.transform, false);
        var hud = go.AddComponent<FrontRoomsKeyHud>();
        hud.Build(hudCanvas, crosshair, keyPanel, handheld);
        return hud;
    }

    /// <summary>Every frame (after the aim is known): what the crosshair is on, its zone identity, and whether
    /// the player holds that zone's key (only picks the prompt lines).</summary>
    public void SetAim(Aim aim, Identity id, bool haveKey) { SetAim(aim, id, haveKey, null); }

    /// <summary>Every frame, with the map's words for the target as shown (DisplayPrompt). Read / Examine need
    /// them (the eye's line: "E  .  READ PLAN", "E  .  EXAMINE KEY"); other kinds ignore them.</summary>
    public void SetAim(Aim aim, Identity id, bool haveKey, string prompt) { aimIn = aim; aimId = id; aimHaveKey = haveKey; aimText = prompt; }

    /// <summary>DECISIONS #20: every frame, bound to the map's own kind (FrontRoomsMapWorld.Kind, crosshair grammar H1).
    /// keyZone's identity, haveKey (LockedHaveKey) and, for Read / Examine, the words as shown.</summary>
    public void SetAim(FrontRoomsMapWorld.AimKind kind, Identity id, bool haveKey, string prompt) { SetAim(AimOf(kind), id, haveKey, prompt); }

    /// <summary>The map's AimKind as a crosshair state (grammar sec. 2 / sec. 4): Locked and LockedHaveKey = the padlock,
    /// Broken = B.4, Read / Examine = the eye, Open / Shut / TakeKey = Use and Glass / GlassTap = Hold (no state; the
    /// dot and the map's prompt, but they count as waiting), None.</summary>
    public static Aim AimOf(FrontRoomsMapWorld.AimKind k)
    {
        switch (k)
        {
            case FrontRoomsMapWorld.AimKind.Locked:
            case FrontRoomsMapWorld.AimKind.LockedHaveKey: return Aim.Locked;
            case FrontRoomsMapWorld.AimKind.Broken: return Aim.Broken;
            case FrontRoomsMapWorld.AimKind.Read: return Aim.Read;
            case FrontRoomsMapWorld.AimKind.Examine: return Aim.Examine;
            case FrontRoomsMapWorld.AimKind.Open:
            case FrontRoomsMapWorld.AimKind.Shut:
            case FrontRoomsMapWorld.AimKind.TakeKey: return Aim.Use;
            case FrontRoomsMapWorld.AimKind.Glass:
            case FrontRoomsMapWorld.AimKind.GlassTap: return Aim.Hold;
        }
        return Aim.None;
    }
    /// <summary>The eye's kinds: the game passes its words only for these (no string work for the others).</summary>
    public static bool WantsWords(FrontRoomsMapWorld.AimKind k) { return k == FrontRoomsMapWorld.AimKind.Read || k == FrontRoomsMapWorld.AimKind.Examine; }

    /// <summary>
    /// DECISIONS #20: a zone's key identity until the map's rule lands (D1.6 DressDoor / DressKey). A STUB, so the map is
    /// never touched for it: the game's HK1 HudIdentity calls this, and swaps in the map's identity when D1.6 brings it.
    /// Shape and colour: a hash of the zone (interactables/10_spec.md sec. 4.1). The token: the FLOOR (#28/#32), and the
    /// game has one floor until C22 lands, so the stub is floor 0 = LEVEL 0 = "00".
    /// </summary>
    public static Identity StubIdentity(FrontRooms.Map.GridCoord zone)
    {
#if UNITY_EDITOR
        if (EditorIdentityOverride != null)
        {
            var v = EditorIdentityOverride(zone);
            return new Identity((Shape)(v / 1000), (Tint)((v / 100) % 10), v % 100);
        }
#endif
        unchecked
        {
            var h = (uint)(zone.x * 73856093 ^ zone.y * 19349663) * 2654435761u;
            return new Identity((Shape)((h / 100u) % 3u), (Tint)((h / 300u) % 3u), 0);
        }
    }

    /// <summary>StubIdentity with the run's floor as the token (DECISIONS #28/#32/#33): floorIndex 0 = LEVEL 0 = "00",
    /// 1 = "B1", ... (C22's game floor 1 is LEVEL 0, so the game passes floor - 1). A harness identity override keeps its own.</summary>
    public static Identity StubIdentity(FrontRooms.Map.GridCoord zone, int floorIndex)
    {
        var id = StubIdentity(zone);
#if UNITY_EDITOR
        if (EditorIdentityOverride != null) return id;
#endif
        id.number = Mathf.Clamp(floorIndex, 0, 99);
        return id;
    }

    /// <summary>Every frame: show the key panel for this identity (the player holds this zone's key).</summary>
    public void SetHeldKey(bool show, Identity id) { heldShow = show; heldId = id; }

    /// <summary>Every frame (ApplyGameplayHudAlpha): the gameplay HUD alpha and the shot takeover fade (rig.HudFade).</summary>
    public void SetHudAlpha(float gameplay, float shotFade) { gameplayA = Mathf.Clamp01(gameplay); this.shotFade = Mathf.Clamp01(shotFade); }

    /// <summary>E on the aimed door, no key (Use answered Rattled).</summary>
    public void NoKey() { pendNoKey = true; }
    /// <summary>E on the aimed door with its key (Use answered KeyTurn: the frame IsHeldForKey starts).</summary>
    public void Unlocking() { pendUnlock = true; }
    /// <summary>E on the aimed broken door (Use answered Broken).</summary>
    public void BrokenTry() { pendBroken = true; }
    /// <summary>The close-up REALLY opened on the aimed READ / EXAMINE target (FrontRoomsInspect.Opened, which this
    /// component also hears by itself; calling both is harmless): the eye's
    /// 80 ms focus, ending inside the close-up's 0.15 s HUD fade. Never call it on E alone: a swallowed E (a gate,
    /// the cooldown, a shot running) has no reaction (grammar sec. 3).</summary>
    public void InspectOpened() { pendInspectOpen = true; }
    /// <summary>The close-up closes (FrontRoomsInspect.Closed, heard by itself too): the eye fades in at rest within
    /// 0.15 s, no blink, if the aim is still on the target (the HUD returns with the shot); otherwise the dot.</summary>
    public void InspectClosed() { pendInspectClose = true; }

    /// <summary>The pickup hand-off at KeyPickup.HoldUntil: the 3D key hides and the HUD key takes off from the held
    /// spot (viewport), drawn at the size the 3D key had (depth, vertical FOV); shotClock = the presentation's clock
    /// on this frame (compensates a late frame).</summary>
    public void PickupFlight(Identity id, Vector2 viewport, float heldDepth, float verticalFov, float shotClock)
    {
        PickupFlight(id, viewport, heldDepth, verticalFov, shotClock, FrontRoomsShotTimings.KeyPickup.HoldUntil);
    }

    /// <summary>DECISIONS #18: the hand-off at any time. The first key of a run is held for the EXAMINE K hold, so it
    /// takes off when that hold ends (move, look away or 2.0 s) or on HeldReturned plus the pocket beat:
    /// takeOffClock = the presentation clock the take-off was due at (HoldUntil for later keys), shotClock = the
    /// clock on this frame. The flight starts there; the 3D tag's held swing carries on from its own clock.</summary>
    public void PickupFlight(Identity id, Vector2 viewport, float heldDepth, float verticalFov, float shotClock, float takeOffClock)
    {
        pendFlight = true;
        flightFromHold = false;
        flightId = id;
        flightViewport = viewport;
        flightDepth = Mathf.Max(.05f, heldDepth);
        flightFov = Mathf.Clamp(verticalFov, 10f, 170f);
        flightLate = Mathf.Max(0f, shotClock - takeOffClock);
        flightHeldAt = Mathf.Max(0f, takeOffClock - FrontRoomsShotTimings.KeyPickup.Arrive);
    }

    /// <summary>
    /// KP-1b (DECISIONS #37, Red 2026-10-29): the INSTANT pickup (no presented key: shipped play until D1.6's real key).
    /// The B glyph plays the full lab P4 by itself: it appears at the hand position (KeyPickup.HeldViewportX / Y = 0.65 /
    /// 0.30, at the size a key held at KeyPickup.HeldDistance would have), is held there 0.5 s with the tag's swing from
    /// its arrival (the lab's t = 0 .. 0.5), then flies into the key panel's slot (desktop bottom-left, phone top-left).
    /// Call it on the frame the map announces the key (KeyTaken) when nothing presents it. Reduce Motion: no hold, no
    /// flight; the panel fades in at its slot (0.15 s), as for PickupFlight.
    /// </summary>
    public void PickupInstant(Identity id, float verticalFov)
    {
        pendFlight = true;
        flightFromHold = true;
        flightId = id;
        flightViewport = new Vector2(FrontRoomsShotTimings.KeyPickup.HeldViewportX, FrontRoomsShotTimings.KeyPickup.HeldViewportY);
        flightDepth = FrontRoomsShotTimings.KeyPickup.HeldDistance;
        flightFov = Mathf.Clamp(verticalFov, 10f, 170f);
        flightLate = 0f;
        flightHeldAt = HM.P4Hold;   // the held swing runs from the glyph's arrival: HeldSwing(t) on the lab's own clock
    }

    /// <summary>True while this component draws the prompt lines: the map writes "" to its own prompt then. It also
    /// counts a state the CURRENT aim will build (SetAim this frame, before the map's prompt line: contract v2.3), so on the
    /// frame a state starts the map does not show its words at full alpha for one frame (capture stage 2026-10-22, VL304).</summary>
    public bool OwnsPrompt => vectorOk && (active != null || lineA > 0f || StateFor(aimIn) != null);
    /// <summary>True once the vector shader loaded: the map hides its own crosshair dot then.</summary>
    public bool OwnsDot => vectorOk;
    /// <summary>(d): the panel is exempt from the shot's takeover fade, from Unlocking until the shot's fade has lifted.</summary>
    public bool PanelShotExempt => panelExempt;
    /// <summary>The crosshair state showing now (None = the dot).</summary>
    public Aim State => active == null ? Aim.None : active.Kind;
    /// <summary>The key panel shows (a held key at its slot, or the P4 flight): what the map's keyPanel.activeSelf
    /// meant before the key HUD (the map's autopilot reads this when the key HUD is attached, contract HK1).</summary>
    public bool PanelShown => ps != PS.Hidden;
    /// <summary>How far the key panel has come in, 0..1: 0 hidden; the P4 flight's share of its 0.72 s path (below 1
    /// until it lands); at the slot 1, or Reduce Motion's 0.15 s fade-in there (StartFlight).</summary>
    public float PanelArrival
    {
        get
        {
            if (ps == PS.Hidden) return 0f;
            if (ps == PS.Flight) return Landed(lastNow) ? 1f : Mathf.Min(.999f, HM.Clamp01((lastNow - tFlight) / HM.P4Flight));
            return HM.Clamp01((lastNow - panelFadeT) / HM.FadeSeconds);
        }
    }

    // ------------------------------------------------------------------ inputs
    Aim aimIn; Identity aimId; bool aimHaveKey; string aimText;
    Aim lastAim; Identity lastAimId; bool lastAimHaveKey; string lastAimText;   // the previous frame's aim (Take)
    bool heldShow; Identity heldId;
    float gameplayA, shotFade;
    bool pendNoKey, pendUnlock, pendBroken, pendFlight, pendInspectOpen, pendInspectClose;
    Identity flightId; Vector2 flightViewport; float flightDepth, flightFov, flightLate, flightHeldAt = HM.P4Hold;
    bool flightFromHold;   // KP-1b: the flight's lab clock starts at t = 0 (the 0.5 s hold drawn), not at the take-off (t = 0.5)
    float lastNow = -1f, dt;
    bool cuePaused;            // #34: the pause card is up (FrontRooms3DGame.Paused), so no cue
    bool cueFlight, cueLand;   // #34: the pickup's two cues, still due for the flight (or Reduce Motion fade-in) showing

    // ------------------------------------------------------------------ the crosshair: registry and this frame's picture
    XState active;
    XState[] registry;
    LockState lockS; BrokenState brokenS; EyeState eyeS;
    float lineA, lineX;
    HM.LockPose pose; bool lockVisible; float dotA = 1f;
    HM.DoorPose door; bool doorVisible; float doorA = 1f;
    HM.EyePose eye; bool eyeVisible;
    Aim linesKind = Aim.None; bool linesHaveKey; string linesText; int linesNumber = -1; bool linesDirty = true;
    float line2A = 1f, line2Shown = -1f;   // line 2's own alpha: Broken shows WON'T SHUT only during its reaction (#29)
    float linesScale = -1f;   // the canvas scale the lines were laid out at (the runs' x and widths are whole screen px at it)
    // #39 (2), the dwell (photosafety): dotOpen = a state is building or built (not leaving); tDot = when the dot last opened
    // or closed. For HM.MinDwellSeconds after each change the aim cannot turn it the other way; dwellHold = this frame holds
    // a state built whose aim has already left (its Step must not read the new aim's fields).
    bool dotOpen; float tDot = -99f; bool dwellHold;

    // ------------------------------------------------------------------ the panel
    enum PS { Hidden, Rest, Flight }
    enum UMode { A, B, C }
    PS ps = PS.Hidden;
    Identity panelId;
    float tFlight, tFlightStart; float flightS0x, flightS0y, flightX0, flightY0, flightCtrlX, flightCtrlY, flightX1, flightY1, flightS1;
    readonly float[] simTable = new float[3000]; int simN;
    float tGesture = -99f; bool gestureOn, gestureCancel, gestureQueued; float tCancel; HM.KeyGesture gestureAtCancel;
    bool panelExempt, exemptSawFade; float tPanelInsert = -99f; float panelFadeT = -99f;

    // ------------------------------------------------------------------ objects
    bool handheld, vectorOk;
    float ls, pS;                 // touch scale (crosshair), panel scale
    Canvas canvas;
    RectTransform canvasRect, keyPanelRef;
    Material mat;
    RectTransform crossRoot; CanvasGroup crossGroup; Canvas crossCanvas;
    FrontRoomsHudVectorGraphic vector;
    RectTransform linesRect; CanvasGroup linesGroup;
    Text l1a, l1dot, l1b, l2; FrontRoomsUITextLayout l1aL, l1dotL, l1bL, l2L;
    RectTransform panelRoot; CanvasGroup panelGroup;
    RectTransform content, rule, labelRect, metaRect; CanvasGroup labelGroup, metaGroup;
    // #41 (O-39) + O-42 (b): STAIR KEY's keyline, and its weight now (1 = the tap alpha), written only when it changes
    Outline labelOutline; float labelOutlineW = -1f;
    /// <summary>O-42 (b): the label's keyline comes in from this share of the word's alpha up (none below it). Under
    /// Bayon's translucent stems a keyline faded with the word read as a dark ghost on bright walls in the first P4 frames.</summary>
    public const float LabelKeylineFrom = .6f;
    Text labelKey, meta; FrontRoomsUITextLayout labelKeyL, metaL;
    RectTransform glyph, swing, keyFold, swingKL, keyFoldKL; RawImage ringTag, keyImg, numImg, ringTagKL, keyKL;
    Font bayon, plex;
    Texture2D lockAtlas; int lockAtlasShape = -1, lockAtlasDensity = -1;
    int panelRingDensity = -1, panelAtlasDensity = -1; Identity panelTexId; bool panelTexSet;
    readonly Vector4[] pts = new Vector4[64];
    static bool loggedShader, loggedCloseUp;

    static readonly int pA = Shader.PropertyToID("_HV_A"), pB = Shader.PropertyToID("_HV_B"), pLockT = Shader.PropertyToID("_HV_LockT"),
        pLockP = Shader.PropertyToID("_HV_LockP"), pBody = Shader.PropertyToID("_HV_Body"), pIns = Shader.PropertyToID("_HV_Ins"),
        pZone = Shader.PropertyToID("_HV_Zone"), pPaper = Shader.PropertyToID("_HV_Paper"), pInk = Shader.PropertyToID("_HV_Ink"),
        pNumInk = Shader.PropertyToID("_HV_NumInk"), pDotCol = Shader.PropertyToID("_HV_DotCol"), pDot = Shader.PropertyToID("_HV_Dot"),
        pDigitRect = Shader.PropertyToID("_HV_DigitRect"), pDigitUV = Shader.PropertyToID("_HV_DigitUV"), pDigit = Shader.PropertyToID("_HV_Digit"),
        pStroke = Shader.PropertyToID("_HV_Stroke"), pLeaf = Shader.PropertyToID("_HV_Leaf"), pLeafT = Shader.PropertyToID("_HV_LeafT"),
        pHinge = Shader.PropertyToID("_HV_Hinge"), pKnob = Shader.PropertyToID("_HV_Knob"), pPts = Shader.PropertyToID("_HV_Pts"),
        pC = Shader.PropertyToID("_HV_C"), pE = Shader.PropertyToID("_HV_E");

    // ------------------------------------------------------------------ build
    void Build(Canvas hudCanvas, RectTransform crosshair, RectTransform keyPanel, bool handheld)
    {
        this.handheld = handheld;
        canvas = hudCanvas.rootCanvas;
        canvasRect = (RectTransform)canvas.transform;
        keyPanelRef = keyPanel;
        ls = handheld ? .75f : 1f;
        pS = handheld ? .85f : 1f;
        // The registry (DECISIONS #16): a new crosshair state is one more entry here, in the grammar's rank order.
        lockS = new LockState(this);
        brokenS = new BrokenState(this);
        eyeS = new EyeState(this);
        registry = new XState[] { lockS, brokenS, eyeS };
        bayon = Resources.Load<Font>("Fonts/Bayon-Regular");
        plex = Resources.Load<Font>("Fonts/IBMPlexMono-Regular");
        var shader = Resources.Load<Shader>("UI/KeyHud/FrontRoomsHudVector");
        if (shader == null) shader = Shader.Find("FrontRooms/UI/HudVector");
        vectorOk = shader != null && shader.isSupported;
        if (!vectorOk && !loggedShader)
        {
            loggedShader = true;
            Debug.LogWarning("[FrontRoomsKeyHud] FrontRooms/UI/HudVector is missing or unsupported: the map keeps its own dot and prompt.");
        }
        if (vectorOk) { mat = new Material(shader) { name = "FrontRooms HUD vector (runtime)", hideFlags = HideFlags.DontSave }; WriteConstants(); }

        // ---- crosshair root: under the map's crosshair, its own canvas (no pixel snapping: the 0.6 px clack and
        // the 0.8 px recoil must not be rounded away) and its own group (the takeover fade is applied here).
        crossRoot = NewRect("KeyHud / Crosshair", crosshair, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        crossCanvas = crossRoot.gameObject.AddComponent<Canvas>();
        crossCanvas.overridePixelPerfect = true; crossCanvas.pixelPerfect = false;
        crossGroup = crossRoot.gameObject.AddComponent<CanvasGroup>();
        crossGroup.ignoreParentGroups = true; crossGroup.blocksRaycasts = false; crossGroup.interactable = false;
        var vgo = NewRect("Vector", crossRoot, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        vector = vgo.gameObject.AddComponent<FrontRoomsHudVectorGraphic>();
        vector.raycastTarget = false;
        vector.UnitScale = ls;
        if (vectorOk) vector.material = mat; else vector.enabled = false;
        linesRect = NewRect("Lines", crossRoot, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        linesGroup = linesRect.gameObject.AddComponent<CanvasGroup>();
        var f1 = handheld ? 17 : 20; var f2 = handheld ? 11 : 13;
        var y1 = handheld ? -38f : -51f; var y2 = handheld ? -58f : -74f;
        l1a = NewText("Line 1", linesRect, bayon, f1, Paper, y1, 0f, true, out l1aL);
        l1dot = NewText("Line 1 dot", linesRect, plex, 16, Paper, y1, 0f, false, out l1dotL);
        l1b = NewText("Line 1 b", linesRect, bayon, f1, Paper, y1, 0f, false, out l1bL);
        l2 = NewText("Line 2", linesRect, plex, f2, Paper, y2, .2f, true, out l2L);
        // G6 (DECISIONS #38): line 1 is the prompt, with the map's promptText keyline (2 px desktop, 1 pt phone);
        // line 2 keeps the 1 unit keyline
        var pk = FrontRoomsHudStyle.PromptKeyline(handheld);
        AddOutline(l1a, pk); AddOutline(l1dot, pk); AddOutline(l1b, pk); AddOutline(l2, FrontRoomsHudStyle.KeylineDistance);
        linesGroup.alpha = 0f;

        // ---- panel root: a sibling right after the map's key panel, full canvas, its own canvas and group
        panelRoot = NewRect("KeyHud / Panel", canvasRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        panelRoot.pivot = Vector2.zero;
        panelRoot.offsetMin = panelRoot.offsetMax = Vector2.zero;
        panelRoot.SetSiblingIndex(keyPanel.GetSiblingIndex() + 1);
        var pc = panelRoot.gameObject.AddComponent<Canvas>();
        pc.overridePixelPerfect = true; pc.pixelPerfect = false;
        panelGroup = panelRoot.gameObject.AddComponent<CanvasGroup>();
        panelGroup.ignoreParentGroups = true; panelGroup.blocksRaycasts = false; panelGroup.interactable = false;
        content = NewRect("Content", panelRoot, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        var ruleImg = NewRect("Rule", content, Vector2.zero, Vector2.zero, new Vector2(0f, 1f), new Vector2(4f, 48f) * pS).gameObject.AddComponent<Image>();
        ruleImg.color = Yellow; ruleImg.raycastTarget = false;
        rule = ruleImg.rectTransform;
        labelRect = NewRect("Label", content, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        labelGroup = labelRect.gameObject.AddComponent<CanvasGroup>();
        // "STAIR KEY" (#29): the floor token is on the tag, so the label carries no number (the Courier label number is gone)
        labelKey = NewText("Key label", labelRect, bayon, handheld ? 17 : 20, Paper, -22f * pS, 0f, false, out labelKeyL);
        labelKey.rectTransform.anchoredPosition = new Vector2(104f * pS, -22f * pS);
        // #41 (O-39): the label on the shared 1 unit keyline, as line 2; it comes in after the word (O-42 (b), LabelKeylineFrom)
        labelOutline = AddOutline(labelKey, FrontRoomsHudStyle.KeylineDistance);
        metaRect = NewRect("Meta", content, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        metaGroup = metaRect.gameObject.AddComponent<CanvasGroup>();
        // #39 (1), O-35 (a): line 2 in the HUD's text paper on the shared 1 unit keyline, as the crosshair's line 2; it is
        // quieter only by its face and size (Plex Mono 13 / 11), never by a dimmer ink (T5)
        meta = NewText("Key meta", metaRect, plex, handheld ? 11 : 13, FrontRoomsHudStyle.TextPaper, -41f * pS, .2f * pS, false, out metaL);
        meta.rectTransform.anchoredPosition = new Vector2(104f * pS, -41f * pS);
        AddOutline(meta, FrontRoomsHudStyle.KeylineDistance);
        labelKey.text = Strings.PanelKey;
        meta.text = Strings.PanelMeta;
        glyph = NewRect("Glyph", panelRoot, Vector2.zero, Vector2.zero, new Vector2(0f, 1f), new Vector2(72f, 48f));
        // DECISIONS #22: the glyph's 1 px keyline OUTSIDE the silhouette (key + ring + tag), the shared ink at the shared
        // alpha (#31). Two masks (the ring + tag's and the key's silhouettes grown by 1 unit), drawn BEFORE both fills so
        // they show only outside the union, never as a line where the key crosses the ring; their transforms copy the
        // swing and the fold every frame.
        swingKL = NewRect("Swing keyline", glyph, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(.5f, .5f), Vector2.zero);
        swingKL.anchoredPosition = new Vector2(35f, -10f);
        ringTagKL = NewRect("Ring + tag keyline", swingKL, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 1f), new Vector2(72f, 48f)).gameObject.AddComponent<RawImage>();
        ringTagKL.rectTransform.anchoredPosition = new Vector2(-35f, 10f); ringTagKL.raycastTarget = false; ringTagKL.color = FrontRoomsHudStyle.Keyline;
        keyFoldKL = NewRect("Key fold keyline", glyph, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(.5f, .5f), Vector2.zero);
        keyFoldKL.anchoredPosition = new Vector2(0f, -10f);
        keyKL = NewRect("Key keyline", keyFoldKL, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 1f), new Vector2(72f, 48f)).gameObject.AddComponent<RawImage>();
        keyKL.rectTransform.anchoredPosition = new Vector2(0f, 10f); keyKL.raycastTarget = false; keyKL.color = FrontRoomsHudStyle.Keyline;
        swing = NewRect("Swing", glyph, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(.5f, .5f), Vector2.zero);
        swing.anchoredPosition = new Vector2(35f, -10f);
        ringTag = NewRect("Ring + tag", swing, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 1f), new Vector2(72f, 48f)).gameObject.AddComponent<RawImage>();
        ringTag.rectTransform.anchoredPosition = new Vector2(-35f, 10f); ringTag.raycastTarget = false;
        numImg = NewRect("Number", swing, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero).gameObject.AddComponent<RawImage>();
        numImg.raycastTarget = false;
        keyFold = NewRect("Key fold", glyph, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(.5f, .5f), Vector2.zero);
        keyFold.anchoredPosition = new Vector2(0f, -10f);
        keyImg = NewRect("Key", keyFold, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 1f), new Vector2(72f, 48f)).gameObject.AddComponent<RawImage>();
        keyImg.rectTransform.anchoredPosition = new Vector2(0f, 10f); keyImg.raycastTarget = false;
        panelGroup.alpha = 0f;
        content.gameObject.SetActive(false);
        glyph.gameObject.SetActive(false);
    }

    static RectTransform NewRect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot; rt.sizeDelta = size; rt.anchoredPosition = Vector2.zero;
        go.layer = parent.gameObject.layer;
        return rt;
    }

    Text NewText(string name, Transform parent, Font font, int size, Color colour, float baseline, float tracking, bool centre, out FrontRoomsUITextLayout layout)
    {
        var rt = NewRect(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(800f, 60f));
        if (parent != linesRect) { rt.anchorMin = rt.anchorMax = Vector2.zero; }
        rt.anchoredPosition = new Vector2(0f, baseline);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = font; t.fontSize = size; t.color = colour; t.raycastTarget = false; t.supportRichText = false;
        t.alignment = TextAnchor.MiddleCenter; t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        layout = rt.gameObject.AddComponent<FrontRoomsUITextLayout>();
        layout.tracking = tracking; layout.centre = centre;
        return t;
    }

    Outline AddOutline(Text t, float distance)
    {
        var o = t.gameObject.AddComponent<Outline>();
        var c = FrontRoomsHudStyle.KeylineColor; c.a = FrontRoomsHudStyle.OutlineTapAlpha;
        o.effectColor = c;
        // canvas units, so the same value in px on desktop and in pt on phone (O-5 / #23; G6 #38 for the prompt line)
        o.effectDistance = new Vector2(distance, -distance);
        o.useGraphicAlpha = true;
        return o;
    }

    void WriteConstants()
    {
        WriteColours();
        mat.SetVectorArray(pPts, pts);
    }

    // The colours, the dot radii and the hinge. Written with every frame's uniforms too (capture stage 2026-10-22,
    // VL304): values written once at Build were found zero at render time in Play Mode (paper and white drawn black,
    // no dot), while the per-frame uniforms arrived. The shader also declares them in its Properties block with
    // these values as defaults, so a material whose undeclared properties are rebuilt still draws the right colours.
    // Six SetVector calls, no allocation.
    void WriteColours()
    {
        mat.SetVector(pPaper, Lin(Paper));
        mat.SetVector(pInk, Lin(FrontRoomsHudStyle.KeylineColor));
        mat.SetVector(pNumInk, Lin(NumInk));
        mat.SetVector(pDotCol, Lin(DotWhite));
        mat.SetVector(pDot, new Vector4(8f, 8f + HM.KL, 0f, 0f));
        mat.SetVector(pHinge, new Vector4(HM.HingeX, -HM.HingeY, 0f, 0f));
    }

    void OnEnable()
    {
        FrontRoomsDoorShot.Beat += OnBeat; FrontRoomsCameraRig.ShotEnded += OnShotEnded;
        FrontRoomsInspect.Opened += OnInspectOpened; FrontRoomsInspect.Closed += OnInspectClosed;
        FrontRooms3DGame.Paused += OnGamePaused;
    }
    void OnDisable()
    {
        FrontRoomsDoorShot.Beat -= OnBeat; FrontRoomsCameraRig.ShotEnded -= OnShotEnded;
        FrontRoomsInspect.Opened -= OnInspectOpened; FrontRoomsInspect.Closed -= OnInspectClosed;
        FrontRooms3DGame.Paused -= OnGamePaused;
    }

    // #34: the game's pause (Esc, the settings sheet, lost focus): true while the pause card is up, false on resume and
    // when a paused run is torn down. The HUD's motions keep running on unscaled time underneath; their cues do not.
    void OnGamePaused(bool on) { cuePaused = on; }

    /// <summary>#34: one cue, now (the start of its motion), unless the pause card is up or a close-up is open. A listener
    /// that throws is logged and never stops the HUD's frame.</summary>
    void Cue(FrontRoomsHudSoundCue cue)
    {
        if (cuePaused || FrontRooms3DGame.InspectingNow) return;
        var h = SoundCue;
        if (h == null) return;
        try { h(cue); }
        catch (Exception e) { Debug.LogException(e); }
    }

    // EXAMINE's own static events (no map line, like the shot's Beat): Opened = the close-up really opened (the
    // shot began; a swallowed E never raises it), Closed = the return started (the HUD comes back with the shot's end).
    void OnInspectOpened(FrontRoomsInspectable target) { pendInspectOpen = true; }
    void OnInspectClosed(FrontRoomsInspectable target) { pendInspectClose = true; }
    void OnDestroy() { if (mat != null) Destroy(mat); }

    // The unlock shot's beats (static event; no map line): (d) starts U.3 at KeyInsert; BoltRetract = committed.
    void OnBeat(ShotKind kind, DoorBeat beat, Vector3 at)
    {
        if (kind != ShotKind.Unlock || lockS == null || !lockS.unlocking) return;
        if (beat == DoorBeat.BoltRetract) lockS.boltSeen = true;
        if (beat == DoorBeat.KeyInsert && PanelMode == PanelGestureMode.D_ShotBeats && panelExempt) tPanelInsert = Now;
        // A cancel before the commit: the shot raises KeyWithdraw and Return with no BoltRetract (FrontRoomsDoorShot.Finish).
        // The panel key eases back on that beat, with the 3D key leaving the lock, so U.3's click (the rule's x1.6
        // pulse, the 'it worked' beat) never plays on an unlock that did not happen (fix round 1; O-23's default
        // eased back only at the HUD's return, 15 frames later, after the click). The padlock still comes back
        // closed at the HUD's return (unchanged).
        if ((beat == DoorBeat.KeyWithdraw || beat == DoorBeat.Return) && !lockS.boltSeen) { tPanelInsert = -99f; CancelGesture(Now); }
    }

    // The unlock shot ended (the camera rig's static event): in (d), a shot that ends before the bolt retracted was
    // cancelled, so the panel key eases back (DECISIONS #14).
    void OnShotEnded(ShotKind kind, Vector3 at)
    {
        if (kind != ShotKind.Unlock || lockS == null || !lockS.unlocking || lockS.boltSeen) return;
        var now = Now;
        CancelGesture(now);
        lockS.CancelUnlock(now);
    }

    // ------------------------------------------------------------------ per frame
    void LateUpdate()
    {
        var now = Now;
        dt = lastNow < 0f ? 0f : Mathf.Clamp(now - lastNow, 0f, .1f);
        lastNow = now;
#if UNITY_EDITOR
        if (EditorAimOverride) { aimIn = EditorAim; aimId = EditorAimId; aimHaveKey = EditorAimHaveKey; aimText = EditorAimText; }
#endif
        // Events are stamped on the first LateUpdate after the call (the same frame in the game, which calls them
        // during Update): their phase is at r = 0 on this frame.
        if (pendFlight) { pendFlight = false; StartFlight(now); }
        if (vectorOk) ApplyEvents(now);
        else pendUnlock = pendNoKey = pendBroken = pendInspectOpen = pendInspectClose = false;
        if (tPanelInsert > -98f) { StartGesture(tPanelInsert); tPanelInsert = -99f; }
        // (d): the exemption lasts until the shot's fade has risen and fallen back to 0, so the panel never dips
        if (panelExempt && shotFade > 1e-4f) exemptSawFade = true;
        if (panelExempt && exemptSawFade && shotFade <= 1e-4f) panelExempt = false;
        if (!panelExempt) exemptSawFade = false;

        if (vectorOk) StepCrosshair(now);
        StepPanel(now);
        lastAim = aimIn; lastAimId = aimId; lastAimHaveKey = aimHaveKey; lastAimText = aimText;
    }

    // ---------------------------------------------------------------- the crosshair registry (DECISIONS #16, #19)
    XState StateFor(Aim a)
    {
        for (var i = 0; i < registry.Length; i++) if (registry[i].Serves(a)) return registry[i];
        return null;
    }

    // E answered by the map (NoKey / Unlocking / BrokenTry) and the close-up's events, in the call order of the 10-14
    // build. Each belongs to one state; InspectClosed only ever returns the eye that is in its close-up state.
    void ApplyEvents(float now)
    {
        if (pendUnlock) { pendUnlock = false; if (Take(lockS, now, true)) lockS.Unlock(now); }
        if (pendNoKey) { pendNoKey = false; if (Take(lockS, now, false)) lockS.NoKey(now); }
        if (pendBroken) { pendBroken = false; if (Take(brokenS, now, false)) brokenS.Try(now); }
        if (pendInspectOpen) { pendInspectOpen = false; if (Take(eyeS, now, true)) eyeS.Opened(now); }
        if (pendInspectClose) { pendInspectClose = false; if (active == eyeS) eyeS.Closed(now, eyeS.Same(aimIn, aimId, aimText)); }
        // #39 (2): an event that opened the dot (a state taken or resumed for its reaction) opens it now, never held by the
        // dwell; the dwell then counts from here
        MarkDot(now);
    }

    // An event for state t (fix round 1, 2026-10-29: the 10-24 build applied an event only when t was ALREADY the
    // state showing, so it was lost on the first frame of an aim, while the previous state was still leaving (the
    // 0.12 s cut), or on a door of another zone). Now:
    //  - t already shows the event's target: the event applies to it (a leave in progress resumes first);
    //  - otherwise t takes the dot AT ONCE, on this frame, and builds with the reaction on top (rule 3), because a
    //    reaction (#19 rank 2) outranks another state's leave (rank 3: the aim);
    //  - a reaction already latched on another state keeps the dot (#19), so an in-place event (the D.3 tug, the
    //    won't-shut swing) shows nothing new then (the map's own 3D rattle / swing still plays); a takeover (the
    //    unlock, the close-up opening) always takes it, because its shot hides the HUD (rank 1), except from a
    //    padlock that is unlocking or an eye that is in its close-up (they own the HUD until their own end).
    // The aim the event was for: this frame's, or the last frame's when the game already passes None (Phase.Inspect
    // on the frame the close-up opens). With no aim for t, only the state already showing can take it (as before).
    bool Take(XState t, float now, bool takeover)
    {
        Aim a; Identity id; bool hk; string text;
        if (t.Serves(aimIn)) { a = aimIn; id = aimId; hk = aimHaveKey; text = aimText; }
        else if (t.Serves(lastAim)) { a = lastAim; id = lastAimId; hk = lastAimHaveKey; text = lastAimText; }
        else return active == t;
        if (active == t && t.Same(a, id, text)) { t.Resume(now); return true; }
        if (active != null)
        {
            var owns = (active == lockS && lockS.unlocking) || (active == eyeS && eyeS.Latched(now));
            if (owns || (!takeover && active.Latched(now))) return false;
        }
        var sa = aimIn; var sid = aimId; var shk = aimHaveKey; var st = aimText;
        aimIn = a; aimId = id; aimHaveKey = hk; aimText = text;
        active = t;
        t.Enter(now);
        aimIn = sa; aimId = sid; aimHaveKey = shk; aimText = st;
        return true;
    }

    // #39 (2): the dot's open / close changes. Open = a state building or built (or a reaction), closed = leaving or idle.
    // The 1e-4 s slack keeps a dwell of exactly MinDwellSeconds on its frame under a float32 clock.
    void MarkDot(float now)
    {
        var open = active != null && active.Open;
        if (open != dotOpen) { dotOpen = open; tDot = now; }
    }
    bool Dwelling(float now) { return now - tDot + 1e-4f < HM.MinDwellSeconds; }

    void StepCrosshair(float now)
    {
        lockVisible = doorVisible = eyeVisible = false; dotA = 1f; doorA = 1f; lineA = 0f; lineX = 0f; line2A = 1f;
        var want = StateFor(aimIn);
        dwellHold = false;
        if (active != null)
        {
            // aimed: the aim is still on this state's target. waiting: something else wants the dot (another state,
            // or a Use / Hold prompt), so a leave is cut to 0.12 s (grammar sec. 4).
            var aimed = want == active && active.Same(aimIn, aimId, aimText);
            var waiting = !aimed && aimIn != Aim.None;
            // #39 (2): inside the dwell the aim cannot turn the dot the other way. An aim that left less than
            // MinDwellSeconds after the open keeps the state built until then (its leave then starts as decided, cut if
            // something waits); an aim back on the target less than MinDwellSeconds after the close lets the leave carry
            // on (nothing else waits for it) until then, and the dot reopens only if the aim is still there
            if (Dwelling(now))
            {
                if (dotOpen && !aimed) { aimed = true; waiting = false; dwellHold = true; }
                else if (!dotOpen && aimed) aimed = waiting = false;
            }
            if (!active.Step(now, aimed, waiting))
            {
                active = null;
                lockVisible = doorVisible = eyeVisible = false; dotA = 1f; doorA = 1f; lineA = 0f; lineX = 0f;
            }
        }
        MarkDot(now);
        // Idle: a state builds on the frame the aim lands on its target (also the frame the previous one left), unless the
        // dot closed less than MinDwellSeconds ago (#39 (2)): then it builds when the dwell ends, if the aim is still there
        if (active == null && want != null && !Dwelling(now))
        {
            active = want;
            active.Enter(now);
            active.Step(now, true, false);
            MarkDot(now);
        }

        WriteUniforms();
        // a resolution change re-lays the three-run line out too (capture stage 2026-10-22: after a canvas-scale change
        // with the same words, "E . UNLOCK" / the eye's words kept the runs' x and widths from the old scale, off whole px)
        if (!Mathf.Approximately(canvas.scaleFactor, linesScale)) { linesScale = canvas.scaleFactor; linesDirty = true; RebuildTextMeshes(); }
        if (linesDirty) WriteLines();
        linesGroup.alpha = lineA;
        if (line2A != line2Shown) { line2Shown = line2A; var c2 = l2.color; c2.a = line2A; l2.color = c2; }   // only on a change (no per-frame mesh rebuild)
        linesRect.anchoredPosition = new Vector2(lineX * ls, 0f);
        // the crosshair's alpha: the takeover fade, except (c) during its unlock
        var exempt = active == lockS && lockS.ExemptFromShotFade;
        crossGroup.alpha = gameplayA * (exempt ? 1f : 1f - shotFade);
    }

    // A canvas-scale change (window resize, resolution, phone rotation) does not by itself regenerate a legacy Text whose words
    // stay the same: its glyph quads kept the old scale's size and texture (capture stage 2, 2026-10-22 pm: "E . READ PLAN",
    // first set before the capture resolution was applied, drew 1 % large and soft at 1080p and 1200p until its words changed).
    // So every HUD text is marked dirty here: the generator runs at the new pixelsPerUnit and FrontRoomsUITextLayout lays it
    // out again. Only on a scale change; no allocation.
    void RebuildTextMeshes()
    {
        l1a.SetVerticesDirty(); l1dot.SetVerticesDirty(); l1b.SetVerticesDirty(); l2.SetVerticesDirty();
        labelKey.SetVerticesDirty(); meta.SetVerticesDirty();
    }

    void SetLines(Aim kind, bool haveKey, string text, int number)
    {
        if (linesKind != kind || linesHaveKey != haveKey || linesNumber != number || !string.Equals(linesText, text, StringComparison.Ordinal)) linesDirty = true;
        linesKind = kind; linesHaveKey = haveKey; linesText = text; linesNumber = number;
    }

    /// <summary>
    /// One crosshair state (DECISIONS #16): built from the dot on aim, a reaction to its event inside a window that
    /// cannot retrigger, a leave back into the dot. Step draws this frame (FrontRoomsKeyHud's picture fields) and
    /// returns false once it has left. The shared leave clock runs at the nominal rate, faster while another state
    /// waits (cut to 0.12 s) and backwards when the same target is aimed again (resume, no rebuild).
    /// </summary>
    abstract class XState
    {
        protected readonly FrontRoomsKeyHud H;
        protected XState(FrontRoomsKeyHud h) { H = h; }
        /// <summary>The grammar's rank (sec. 4; lower wins). The map's ray already picks one kind per frame.</summary>
        public abstract int Rank { get; }
        public abstract Aim Kind { get; }
        public abstract string Phase { get; }
        public abstract bool Serves(Aim a);
        /// <summary>The aim is this state's target (kind and identity / words).</summary>
        public abstract bool Same(Aim a, Identity id, string text);
        /// <summary>The build starts on this frame (Reduce Motion / Flashing are read here, once per build).</summary>
        public abstract void Enter(float now);
        public abstract bool Step(float now, bool aimed, bool waiting);
        /// <summary>A reaction inside its window (#19 rank 2): the aim cannot take the dot from it.</summary>
        public abstract bool Latched(float now);
        /// <summary>An event for the target this state shows arrived while it was leaving: carry on from where it is,
        /// as a re-aim would on this frame, so the event's reaction can start (no rebuild, no blink).</summary>
        public virtual void Resume(float now) { }
        /// <summary>#39 (2): the dot is open (building, built or reacting), not leaving.</summary>
        public abstract bool Open { get; }

        float lcBase, lcT0, lcK = 1f;
        protected void LeaveStart(float now) { lcBase = 0f; lcT0 = now; lcK = 1f; }
        /// <summary>Seconds of leave at the nominal rate; k = the rate from now on.</summary>
        protected float LeaveClock(float now, float k)
        {
            if (k != lcK) { lcBase = Mathf.Max(0f, lcBase + (now - lcT0) * lcK); lcT0 = now; lcK = k; }
            return Mathf.Max(0f, lcBase + (now - lcT0) * lcK);
        }
        /// <summary>The leave's rate: backwards on a re-aim; cut so the rest of a leave of <paramref name="nominal"/>
        /// seconds takes at most 0.12 s while another state waits; else 1.</summary>
        protected static float LeaveRate(bool back, bool waiting, float nominal)
        {
            if (back) return -1f;
            return waiting && nominal > HM.LeaveCutSeconds ? nominal / HM.LeaveCutSeconds : 1f;
        }
    }

    // ---------------------------------------------------------------- Locked: L3.D, D.3, the unlock and F.1
    sealed class LockState : XState
    {
        public LockState(FrontRoomsKeyHud h) : base(h) { }
        public override int Rank => 3;
        public override Aim Kind => Aim.Locked;
        public override bool Serves(Aim a) { return a == Aim.Locked; }
        public override bool Same(Aim a, Identity i, string text) { return a == Aim.Locked && i.Equals(id); }
        public override string Phase => unlocking ? (umode == UMode.B && peakFade > 0f ? (tRet < 0f ? "unlock-hidden" : "unlock-return") : "unlock") : lockIn ? (u < 1f ? "in" : "hold") : "out";
        public override bool Open => unlocking || lockIn;

        public Identity id; public HM.Geo geo; bool geoSet; bool haveKey;
        bool fade, fresh; float fa;                   // Reduce Motion: the cross-fade, 0 = the dot, 1 = the padlock
        bool lockIn; public float u; float u0, tDir;  // the build (L3.D in, 0.5 s) and its reverse (famOut, 0.32 s)
        float textA, text0, tText;
        public float tDeny = -99f;
        public bool unlocking, boltSeen; UMode umode; public float tUnlock, tRet; float peakFade, unlockText0; int zeroFrames; bool noShot;
        bool dropCued;   // #34: this unlock's UnlockDrop was raised (once, as the drop or its Reduce Motion cross-fade starts)

        public bool ExemptFromShotFade => unlocking && umode == UMode.C;
        public string UModeName => umode.ToString();
        public bool LockInNow => lockIn;

        public override void Enter(float now)
        {
            var i = H.aimId;
            if (!geoSet || !id.Equals(i)) { geo = HM.GeoOf((HM.Shape)(int)i.shape); geoSet = true; }
            id = i; haveKey = H.aimHaveKey;
            H.SetLines(Aim.Locked, haveKey, null, -1);
            fade = ReduceMotion; fresh = true; fa = 0f;
            lockIn = true; u = u0 = 0f; tDir = now; textA = text0 = 0f; tText = now;
            tDeny = -99f; unlocking = false;
        }

        bool DenyOn(float now) { var r = now - tDeny; return r >= 0f && r < HM.DenySeconds; }
        // the famOut reversed from where it is (Step's re-aim branch, one frame early); Reduce Motion: the cross-fade turns back
        public override void Resume(float now)
        {
            if (unlocking || lockIn) return;
            lockIn = true;
            if (!fade) { u0 = u; tDir = now; text0 = textA; tText = now; }
        }
        public override bool Latched(float now) { return unlocking || DenyOn(now); }

        /// <summary>D.3: the 0.50 s window cannot retrigger (the map's Rattle.Duration is 0.50 s too, grammar H2). #34: the
        /// tug's cue as the window starts (Reduce Motion draws no tug, the cue still plays).</summary>
        public void NoKey(float now)
        {
            if (!unlocking && lockIn && now - tDeny >= HM.DenySeconds) { tDeny = now; H.Cue(FrontRoomsHudSoundCue.LockTug); }
        }

        public void Unlock(float now)
        {
            if (unlocking || !lockIn) return;
            unlocking = true; tUnlock = now;
            tRet = -1f; peakFade = 0f; zeroFrames = 0; boltSeen = false; noShot = false; dropCued = false;
            umode = UnlockStart == UnlockStartMode.B_AfterShot ? UMode.B : UnlockStart == UnlockStartMode.C_ExemptFromShotFade ? UMode.C : UMode.A;
            unlockText0 = textA;
            if (PanelMode == PanelGestureMode.D_ShotBeats && H.ps != PS.Hidden) H.panelExempt = true;
        }

        public override bool Step(float now, bool aimed, bool waiting)
        {
            // (a dwell hold, #39 (2), keeps this door's lines: the aim it would read has left)
            if (aimed && !H.dwellHold && H.aimHaveKey != haveKey) { haveKey = H.aimHaveKey; H.SetLines(Aim.Locked, haveKey, null, -1); }
            if (unlocking) return StepUnlock(now, aimed, waiting);
            // #19: the tug holds the dot until its 0.50 s window ends, then the leave starts
            var want = aimed || (lockIn && DenyOn(now));
            if (fade) return StepFade(now, want, waiting);
            if (lockIn && !want) { lockIn = false; u0 = u; text0 = textA; LeaveStart(now); }
            else if (!lockIn && want) { lockIn = true; u0 = u; tDir = now; text0 = textA; tText = now; }
            if (lockIn)
            {
                u = Mathf.Min(1f, u0 + (now - tDir) / HM.LockInSeconds);
                // rule 2 (re-aim mid-fold): min(clamp(u / 0.6), alpha at re-aim + t / 0.3); fresh: clamp(u / 0.6)
                textA = Mathf.Min(HM.Clamp01(u / .6f), text0 + (now - tText) / .3f);
            }
            else
            {
                // famOut from u0 (lab 983), cut to 0.12 s while another state waits
                var x = LeaveClock(now, LeaveRate(false, waiting, HM.LockOutSeconds * u0));
                u = Mathf.Max(0f, u0 - x / HM.LockOutSeconds);
                textA = text0 * (1f - HM.Clamp01(x / (.6f * HM.LockOutSeconds)));
                if (u <= 0f) return false;
            }
            H.pose = HM.LockIn(geo, u);
            var r = now - tDeny;
            // D.3 on top of the build (rule 3: the channels add)
            if (r >= 0f && r < HM.DenySeconds) { var buildY = H.pose.shY; HM.DenyTugShake(ref H.pose, r); H.pose.shY += buildY; }
            H.lockVisible = true; H.dotA = 0f; H.lineA = textA; H.lineX = H.pose.tx;
            return true;
        }

        // Reduce Motion: the closed padlock cross-fades with the dot (0.15 s; 0.12 s when another state waits). No tug.
        bool StepFade(float now, bool want, bool waiting)
        {
            if (!fresh) fa = Mathf.MoveTowards(fa, want ? 1f : 0f, H.dt * (want ? 1f : LeaveRate(false, waiting, HM.FadeSeconds)) / HM.FadeSeconds);
            fresh = false;
            lockIn = want; u = 1f;
            if (!want && fa <= 0f) return false;
            textA = fa;
            H.pose = HM.LockIn(geo, 1f); H.pose.op = fa;
            H.lockVisible = true; H.dotA = 1f - fa; H.lineA = fa; H.lineX = 0f;
            return true;
        }

        /// <summary>The unlock shot ended before the bolt retracted (a cancel: ShotEnded or the HUD's return with no
        /// BoltRetract): the door is still locked, so the padlock comes back CLOSED with the HUD, its lines fading in
        /// again; no swing, no fall (O-23, a the visual chat default: the lab has no cancel).</summary>
        public void CancelUnlock(float now)
        {
            if (!unlocking || boltSeen) return;
            unlocking = false;
            lockIn = true; u = u0 = 1f; tDir = now; text0 = 0f; textA = 0f; tText = now; fa = 1f; fresh = false; tDeny = -99f;
        }

        bool StepUnlock(float now, bool aimed, bool waiting)
        {
            var s = now - tUnlock;
            // The shot's takeover fade: its peak since the press, then the HUD's return (t_ret). No fade on the press
            // frame or the next two = no shot (BeginUnlock returned early): (a) for the crosshair and the panel.
            if (H.shotFade > peakFade) peakFade = H.shotFade;
            if (tRet < 0f && !noShot)
            {
                if (peakFade > 0f && H.shotFade < peakFade - 1e-4f)
                {
                    tRet = now;
                    if (!boltSeen) { H.CancelGesture(now); CancelUnlock(now); return Step(now, aimed, waiting); }
                    H.OnHudReturn(now);
                }
                else if (peakFade <= 0f && ++zeroFrames >= 3) { noShot = true; if (umode == UMode.B) umode = UMode.A; H.panelExempt = false; H.StartGesture(now); }
            }
            if (fade)
            {
                // Reduce Motion: no swing, no fall. Hidden under the shot, then the open padlock cross-fades into the dot.
                if (umode == UMode.B && peakFade > 0f && tRet < 0f) { H.dotA = 0f; H.lineA = Mathf.Min(unlockText0, 1f - HM.Clamp01(s / HM.FadeSeconds)); return true; }
                // #34: the cross-fade is the drop. In (b) its start is known only once the HUD is back from a committed
                // shot (a cancel went to CancelUnlock above) or no shot runs (3 frames without a fade: (a) from here on).
                if (!dropCued && (umode != UMode.B || tRet >= 0f)) { dropCued = true; H.Cue(FrontRoomsHudSoundCue.UnlockDrop); }
                var t0 = umode == UMode.B && peakFade > 0f ? tRet : tUnlock;
                var e = HM.Clamp01((now - t0) / HM.FadeSeconds);
                H.pose = HM.Closed(geo); HM.SwingOpen(ref H.pose, 99f); H.pose.op = 1f - e;
                H.lockVisible = true; H.dotA = e; H.lineA = Mathf.Min(unlockText0, 1f - e);
                if (e >= 1f) return EndUnlock();
                return true;
            }
            if (umode == UMode.B && peakFade > 0f)
            {
                if (tRet < 0f)
                {
                    // hidden under the shot (lab in-context 'b': P = null from the press; the prompt fades over 0.12 s)
                    H.dotA = 0f;
                    H.lineA = Mathf.Min(unlockText0, HM.OpenText(s));
                    return true;
                }
                // the HUD is back: the padlock is already open; the drop starts ReturnDropDelay later (O-19)
                var f = now - tRet - ReturnDropDelay;
                if (f >= 0f && !dropCued) { dropCued = true; H.Cue(FrontRoomsHudSoundCue.UnlockDrop); }   // #34: F.1 starts
                H.pose = HM.Closed(geo);
                HM.SwingOpen(ref H.pose, 99f);
                if (f > 0f) HM.FallDrop(ref H.pose, f); else H.pose.dot = 0f;
                H.lockVisible = true; H.dotA = H.pose.dot; H.lineA = 0f;
                if (f >= HM.FallSeconds) return EndUnlock();
                return true;
            }
            // (a) / (c), and (b) until a shot shows: from the press. The build finishes underneath (rule 3), the
            // shackle lifts and swings (swingOpen), a 60 ms beat, then F.1.
            var r = s;
            if (r < HM.OpenReact + HM.OpenBeat)
            {
                u = Mathf.Min(1f, u0 + (now - tDir) / HM.LockInSeconds);
                var buildText = Mathf.Min(HM.Clamp01(u / .6f), text0 + (now - tText) / .3f);
                H.pose = HM.LockIn(geo, u);
                var buildY = H.pose.shY;
                var d = now - tDeny;
                if (d >= 0f && d < HM.DenySeconds) { HM.DenyTugShake(ref H.pose, d); buildY += H.pose.shY; }
                HM.SwingOpen(ref H.pose, r);
                H.pose.shY += buildY;
                H.lockVisible = true; H.dotA = 0f;
                H.lineA = Mathf.Min(buildText, HM.OpenText(r));
                H.lineX = H.pose.tx;
                return true;
            }
            var f2 = r - HM.OpenReact - HM.OpenBeat;
            if (!dropCued) { dropCued = true; H.Cue(FrontRoomsHudSoundCue.UnlockDrop); }   // #34: F.1 starts after the swing and the beat
            H.pose = HM.OpenFall(geo, f2);
            H.lockVisible = true; H.dotA = H.pose.dot; H.lineA = 0f;
            if (f2 >= HM.FallSeconds) return EndUnlock();
            return true;
        }

        bool EndUnlock()
        {
            unlocking = false;
            lockIn = false; u = 0f; textA = 0f;
            H.panelExempt = false;
            H.lockVisible = false; H.dotA = 1f; H.lineA = 0f;
            return false;
        }
    }

    // The HUD came back from a committed unlock shot: (b) starts U.3 now ((d) started it on KeyInsert).
    void OnHudReturn(float now)
    {
        if (PanelMode == PanelGestureMode.B_AfterShot) StartGesture(now);
    }

    // ---------------------------------------------------------------- Broken: B.4
    sealed class BrokenState : XState
    {
        public BrokenState(FrontRoomsKeyHud h) : base(h) { }
        enum BP { In, Hold, Out }
        BP bph; float tB, tReact = -99f; HM.DoorPose bLeave; float bInX; bool fade, fresh; float fa;
        bool back;   // #39 (2): the leave runs back (re-aimed): the dot is opening again
        public override int Rank => 4;
        public override Aim Kind => Aim.Broken;
        public override string Phase => fade ? "fade" : bph == BP.In ? "in" : bph == BP.Hold ? (ReactOn(H.lastNow) ? "react" : "hold") : "out";
        public override bool Serves(Aim a) { return a == Aim.Broken; }
        public override bool Same(Aim a, Identity i, string text) { return a == Aim.Broken; }
        public override bool Open => bph != BP.Out || back;

        public override void Enter(float now)
        {
            bph = BP.In; tB = now; tReact = -99f; fade = ReduceMotion; fresh = true; fa = 0f; back = false;
            H.SetLines(Aim.Broken, false, null, -1);
            H.Cue(FrontRoomsHudSoundCue.BrokenBuild);   // #34: the build (or its Reduce Motion cross-fade) starts on this frame
        }
        bool ReactOn(float now) { var r = now - tReact; return r >= 0f && r < HM.DoorReactSeconds; }
        public override bool Latched(float now) { return ReactOn(now); }
        // mid-leave: back to where the leave started (the build or the hold), so the swing can play on it; the leave had
        // undone at most a few frames of the pose, and the swing starts on this frame
        public override void Resume(float now)
        {
            if (fade || bph != BP.Out) return;
            if (bInX < 1f) { bph = BP.In; tB = now - bInX * HM.DoorInSeconds; } else bph = BP.Hold;
        }
        /// <summary>E: the won't-shut swing, a 0.55 s window that cannot retrigger (the map's BrokenDoor.Duration is 0.55 s).
        /// #34: the knock's cue as the window starts.</summary>
        public void Try(float now)
        {
            if (bph != BP.Out && now - tReact >= HM.DoorReactSeconds) { tReact = now; H.Cue(FrontRoomsHudSoundCue.BrokenKnock); }
        }

        HM.DoorPose Built(float now, out float x)
        {
            HM.DoorPose d;
            if (bph == BP.In)
            {
                x = (now - tB) / HM.DoorInSeconds;
                d = HM.DoorIn(Mathf.Min(x, 1f));
                // O-30: a 1 ms slack keeps the frame at 0.800 s on the HOLD under a float32 clock (it reads 0.79999995 s
                // there); no 60 / 120 Hz frame moves (the frame before is at least 8.3 ms earlier)
                if (now - tB + 1e-3f >= HM.DoorInSeconds) { bph = BP.Hold; d = HM.DoorHold(); }
            }
            else { x = 1f; d = HM.DoorHold(); }
            var r = now - tReact;
            if (r >= 0f && r < HM.DoorReactSeconds) { var rd = HM.DoorReact(r); d.th = rd.th; d.dy = rd.dy; }
            return d;
        }

        public override bool Step(float now, bool aimed, bool waiting)
        {
            // #19: the won't-shut swing holds the dot until its window ends
            var want = aimed || (bph != BP.Out && ReactOn(now));
            if (fade)
            {
                if (!fresh) fa = Mathf.MoveTowards(fa, want ? 1f : 0f, H.dt * (want ? 1f : LeaveRate(false, waiting, HM.FadeSeconds)) / HM.FadeSeconds);
                fresh = false;
                bph = want ? BP.Hold : BP.Out;
                if (!want && fa <= 0f) return false;
                H.door = HM.DoorHold(); H.doorA = fa;
                H.doorVisible = true; H.dotA = 1f - fa; H.lineA = fa;
                H.line2A = ReactOn(now) ? 1f : 0f;
                return true;
            }
            HM.DoorPose d;
            if (bph != BP.Out)
            {
                float x;
                d = Built(now, out x);
                if (!want)
                {
                    // rule 7: leave from wherever it is (the build or the hold; never inside a reaction, #19)
                    bLeave = d; bInX = Mathf.Min(x, 1f); bph = BP.Out; LeaveStart(now); back = false;
                }
            }
            if (bph == BP.Out)
            {
                var t = LeaveClock(now, LeaveRate(want, waiting, HM.DoorOutSeconds));
                back = want;
                if (want && t <= 0f)
                {
                    // re-aimed mid-leave: it ran back to where it left; carry on from there (no rebuild)
                    if (bInX < 1f) { bph = BP.In; tB = now - bInX * HM.DoorInSeconds; } else bph = BP.Hold;
                    float x;
                    d = Built(now, out x);
                }
                else
                {
                    var x = t / HM.DoorOutSeconds;
                    var o = HM.DoorOut(Mathf.Min(x, 1f));
                    var undo = HM.Eio(HM.Clamp01(x / .5f));
                    d.m = Mathf.Min(bLeave.m, o.m);
                    d.fr = Mathf.Min(bLeave.fr, o.fr);
                    d.kn = Mathf.Min(bLeave.kn, o.kn);
                    d.th = bLeave.th * (1f - undo);
                    d.dy = bLeave.dy * (1f - undo);
                    d.text = bLeave.text * (1f - HM.Clamp01(t / (.6f * HM.DoorOutSeconds)));
                    if (!want && x >= 1f) return false;
                }
            }
            else d = Built(now, out _);
            H.door = d;
            H.doorVisible = true; H.dotA = 0f; H.lineA = d.text;
            H.line2A = ReactOn(now) ? 1f : 0f;   // #29: WON'T SHUT only during the 0.55 s reaction
            return true;
        }
    }

    // ---------------------------------------------------------------- Read / Examine: the look-eye E.1 (#17)
    sealed class EyeState : XState
    {
        public EyeState(FrontRoomsKeyHud h) : base(h) { }
        enum EP { In, Rest, React, Gone, Back, Settle, Out }
        EP ep; float tP; bool blink, blinkStarted, fade, fromRest, quit; float goneVisible;
        bool back;   // #39 (2): the leave runs back (re-aimed): the dot is opening again
        HM.EyePose S, last; float textS, lastText;
        Aim kind; string raw;
        bool e3;   // DECISIONS #30: the E.3 fallback (FrontRoomsKeyHud.Eye), read once per build
        /// <summary>When the last blink started (grammar sec. 5: a re-blink needs >= 1.0 s since then and the aim to have left).</summary>
        public float lastBlink = -99f;
        public override int Rank => 7;
        public override Aim Kind => kind;
        public override string Phase => ep.ToString().ToLowerInvariant() + (blink ? "" : "-noblink") + (fade ? "-fade" : "");
        public override bool Serves(Aim a) { return a == Aim.Read || a == Aim.Examine; }
        public override bool Same(Aim a, Identity i, string text) { return a == kind && string.Equals(text, raw, StringComparison.Ordinal); }
        public override bool Latched(float now) { return ep == EP.React || ep == EP.Gone; }
        public override bool Open => (ep != EP.Out || back) && !quit;

        public override void Enter(float now)
        {
            kind = H.aimIn; raw = H.aimText; e3 = Eye == EyeVariant.E3_ReadingEye;
            var fallback = kind == Aim.Examine ? Strings.ExamineFallback : Strings.ReadFallback;
            H.SetLines(kind, false, raw ?? (WithUseControl(fallback, H.handheld) ?? fallback), -1);
            fade = ReduceMotion;
            // one slow blink, on the build only; a fresh build means the aim had left, so it blinks if the last
            // blink started >= 1.0 s ago (an aim jittering at the gate's edge cannot strobe). Reduce Flashing: never.
            blink = !fade && !ReduceFlashing && now - lastBlink >= HM.EyeReblinkSeconds;
            blinkStarted = false; quit = false; goneVisible = 0f;
            ep = EP.In; tP = now; back = false;
            last = HM.DotEye; last.lidOp = 0f; lastText = 0f;
        }

        /// <summary>InspectOpened: the focus (80 ms), the eye and its line fading over 50-150 ms, then hidden.</summary>
        public void Opened(float now)
        {
            if (ep == EP.React || ep == EP.Gone) return;
            S = last; textS = lastText; ep = EP.React; tP = now;
        }
        /// <summary>InspectClosed: back at rest within 0.15 s, no blink, if the aim is still on the target.</summary>
        public void Closed(float now, bool aimedNow)
        {
            if (ep != EP.React && ep != EP.Gone) return;
            if (aimedNow) { ep = EP.Back; tP = now; } else quit = true;
        }

        public override bool Step(float now, bool aimed, bool waiting)
        {
            if (quit) return false;
            var st = HM.EyeRestOf(e3); var text = 1f; var draw = true; var dot = 0f;
            switch (ep)
            {
                case EP.In:
                {
                    var x = now - tP;
                    if (fade)
                    {
                        // Reduce Motion: the eye at rest cross-fades with the dot
                        var e = HM.Clamp01(x / HM.FadeSeconds);
                        st.op = e; text = e; dot = 1f - e;
                        if (x + 1e-5f >= HM.FadeSeconds) ep = EP.Rest;
                    }
                    else
                    {
                        if (blink && !blinkStarted && x + 1e-5f >= HM.EyeBlinkAt) { blinkStarted = true; lastBlink = tP + HM.EyeBlinkAt; }
                        st = HM.EyeIn(x, blink, e3); text = HM.EyeInText(x);
                        if (x + 1e-5f >= (blink ? HM.EyeInSeconds : HM.EyeNoBlinkIn)) ep = EP.Rest;
                    }
                    break;
                }
                case EP.Rest: break;
                case EP.React:
                {
                    var x = now - tP;
                    if (fade) { st = S; st.op = S.op * HM.EyeReactText(x); }
                    else st = HM.EyeReact(S, x, e3);
                    text = textS * HM.EyeReactText(x);
                    // the frame it ends is already the close-up (lab frameLook: t >= tGone draws nothing)
                    if (x + 1e-5f >= HM.EyeReactSeconds) { ep = EP.Gone; draw = false; text = 0f; }
                    break;
                }
                case EP.Gone:
                {
                    draw = false; text = 0f;
                    // safety net: the HUD is fully back for 2 s and no InspectClosed came
                    goneVisible = H.gameplayA * (1f - H.shotFade) >= .999f ? goneVisible + H.dt : 0f;
                    if (goneVisible >= CloseUpSafetySeconds)
                    {
                        if (!loggedCloseUp) { loggedCloseUp = true; Debug.LogWarning("[FrontRoomsKeyHud] the eye waited " + CloseUpSafetySeconds + " s for InspectClosed with the HUD back; returning (EXAMINE should call InspectClosed)."); }
                        if (aimed) { ep = EP.Back; tP = now; draw = true; st.op = 0f; } else return false;
                    }
                    break;
                }
                case EP.Back:
                {
                    var x = HM.Clamp01((now - tP) / HM.EyeBackSeconds);
                    st = HM.EyeBack(x, e3); text = HM.EyeBackText(x);
                    if ((now - tP) + 1e-5f >= HM.EyeBackSeconds) ep = EP.Rest;
                    break;
                }
                case EP.Settle:
                {
                    // back at rest from where a cut leave was (no rebuild, no blink): 0.30 s (0.15 s under Reduce Motion)
                    var len = fade ? HM.FadeSeconds : HM.EyeNoBlinkIn;
                    var e = HM.Eio(HM.Clamp01((now - tP) / len));
                    st = HM.LerpE(S, HM.EyeRestOf(e3), e); st.lidOp = HM.Lerp(S.lidOp, 1f, e); st.op = HM.Lerp(S.op, 1f, e);
                    text = HM.Lerp(textS, 1f, e);
                    if (fade) dot = 1f - st.op;
                    if ((now - tP) + 1e-5f >= len) ep = EP.Rest;
                    break;
                }
                case EP.Out:
                {
                    var nominal = fade ? HM.FadeSeconds : HM.EyeOutSeconds;
                    var k = LeaveRate(aimed, waiting, nominal);
                    back = k < 0f;
                    var x = LeaveClock(now, k);
                    if (aimed && x <= 0f)
                    {
                        // re-aimed mid-leave: it ran back to where it left; carry on from there, no rebuild, no blink
                        st = S; text = textS;
                        if (fromRest) ep = EP.Rest; else { ep = EP.Settle; tP = now; }
                        if (fade) dot = 1f - st.op;
                        break;
                    }
                    if (fade) { var e = 1f - HM.Clamp01(x / HM.FadeSeconds); st = S; st.op = S.op * e; text = textS * e; dot = 1f - st.op; }
                    else { st = HM.EyeOut(S, x); text = textS * HM.EyeOutText(x); }
                    if (k > 0f && x + 1e-5f >= nominal) return false;
                    break;
                }
            }
            // leaving: the aim is off this target and no reaction holds it (#19); a switch during the blink cuts it like any leave
            if (!aimed && (ep == EP.In || ep == EP.Rest || ep == EP.Back || ep == EP.Settle))
            {
                fromRest = ep == EP.Rest;
                S = st; textS = text; ep = EP.Out; LeaveStart(now); back = false;
            }
            last = st; lastText = text;
            H.eye = st;
            H.eyeVisible = draw && st.op > 0f;
            H.dotA = dot; H.lineA = draw ? text : 0f; H.lineX = 0f;
            return true;
        }
    }

    // ---------------------------------------------------------------- uniforms
    void WriteUniforms()
    {
        WriteColours();
        var px = PxPerUnit();
        var n = 0;
        var lockA = lockVisible ? Mathf.Clamp01(pose.op) : 0f;
        float eyeOp = 0f, eyeLid = 0f, eyePr = 0f, eyeW = 0f, eyeCy = 0f; var perLid = 0;
        if (lockVisible)
        {
            var geo = lockS.geo;
            n = HM.ShacklePoints(geo, pose.shK, -pose.shY, pts, 0);
            mat.SetVector(pLockT, new Vector4(pose.x, -pose.y, Mathf.Cos(-pose.rot * Mathf.Deg2Rad), Mathf.Sin(-pose.rot * Mathf.Deg2Rad)));
            mat.SetVector(pLockP, new Vector4(pose.rpx, -pose.rpy, pose.sc, 0f));
            mat.SetVector(pBody, new Vector4(pose.body.w, pose.body.h, pose.body.r, 0f));
            mat.SetVector(pIns, new Vector4(pose.ins.w, pose.ins.h, pose.ins.r, 0f));
            mat.SetVector(pZone, Lin(ZoneColour[(int)lockS.id.tint]));
            mat.SetVector(pStroke, new Vector4(HM.SW / 2f, HM.SW / 2f + HM.KL, 0f, 0f));
            WriteDigits(px);
        }
        else if (doorVisible)
        {
            n = HM.FramePoints(door.fr, pts, 0);
            var leaf = HM.LeafAt(door.m);
            mat.SetVector(pLeaf, new Vector4(leaf.w, leaf.h, leaf.r, -HM.LeafCy * door.m));
            var a = -door.th * Mathf.Deg2Rad;
            mat.SetVector(pLeafT, new Vector4(-door.dy, Mathf.Cos(a), Mathf.Sin(a), 0f));
            mat.SetVector(pKnob, new Vector4(HM.KnobX, -HM.KnobY, HM.KnobR * Mathf.Max(0f, door.kn), 0f));
            mat.SetVector(pStroke, new Vector4(1f, 1f + HM.KL, 0f, 0f));
        }
        else if (eyeVisible)
        {
            // eyeDraw (lab 998-1010): the lids from lidD, Unity y up; the pupil at (0, -pdy)
            float w, up, lo, pr;
            HM.EyeDraw(eye, out w, out up, out lo, out pr);
            perLid = HM.LidPoints(w, up, eye.c, pts, 0);
            HM.LidPoints(w, lo, eye.c, pts, perLid);
            eyeOp = Mathf.Clamp01(eye.op); eyeLid = Mathf.Clamp01(eye.lidOp); eyePr = pr; eyeW = w; eyeCy = -eye.pdy;
            mat.SetVectorArray(pPts, pts);
        }
        if (n > 0) mat.SetVectorArray(pPts, pts);
        mat.SetVector(pA, new Vector4(px, FrontRoomsHudStyle.KeylineAlpha, lockA, Mathf.Clamp01(dotA)));
        mat.SetVector(pB, new Vector4(doorVisible ? 1f : 0f, n, lockVisible ? -lockS.geo.top - 6f : 0f, 0f));
        mat.SetVector(pC, new Vector4(doorVisible ? Mathf.Clamp01(doorA) : 0f, eyeOp, eyeLid, eyePr));
        mat.SetVector(pE, new Vector4(eyeW, eyeCy, perLid, 0f));
    }

    float PxPerUnit() { return Mathf.Max(.01f, canvas.scaleFactor) * ls; }

    void WriteDigits(float px)
    {
        var shape = (int)lockS.id.shape;
        var density = DensityFor(px, FrontRoomsKeyHudParts.NumberDensities);
        if (shape != lockAtlasShape || density != lockAtlasDensity || lockAtlas == null)
        {
            lockAtlas = Resources.Load<Texture2D>(FrontRoomsKeyHudParts.NumberAtlasPath(shape, density));
            lockAtlasShape = shape; lockAtlasDensity = density;
            vector.NumberAtlas = lockAtlas;
        }
        if (lockAtlas == null) { mat.SetVector(pDigit, Vector4.zero); return; }
        Vector4 rect, uv;
        FrontRoomsKeyHudParts.Cell(shape, lockS.id.number, out rect, out uv);
        mat.SetVector(pDigitRect, rect);
        mat.SetVector(pDigitUV, uv);
        var reveal = pose.reveal >= 1f ? 1e4f : pose.reveal > 0f ? FrontRoomsKeyHudParts.Split(shape, lockS.id.number) : -1e4f;
        mat.SetVector(pDigit, new Vector4(reveal, 1f, 0f, 0f));
    }

    static int DensityFor(float px, int[] densities)
    {
        // the smallest atlas at least as dense as the screen (capture stage 2026-10-22: with >= 0.9 px, 1920 x 1200's
        // 1.054 px per unit took the 1x digits and upscaled them, soft and grey)
        for (var i = 0; i < densities.Length; i++) if (densities[i] >= px - 1e-3f) return densities[i];
        return densities[densities.Length - 1];
    }

    // ---------------------------------------------------------------- prompt lines
    /// <summary>
    /// A prompt line ("E  ·  UNLOCK") with the USE control the player has (交互设计, 2026-10-29): on the desktop the key USE
    /// is bound to; on a handheld with a pad, the pad's button (A / CROSS); with a keyboard, the key. Null on a touch
    /// handheld: the USE glyph carries the verb, and the line shows the verb alone.
    /// </summary>
    static string WithUseControl(string line, bool handheld)
    {
        if (!handheld) return FrontRoomsInput.WithKeyNames(line);
        switch (FrontRoomsTouchControls.CurrentMode)
        {
            case FrontRoomsTouchControls.InputMode.Gamepad:
                return line.StartsWith("E  ", StringComparison.Ordinal) ? FrontRoomsInput.PadName(FrontRoomsInput.PadButton.South) + line.Substring(1) : line;
            case FrontRoomsTouchControls.InputMode.Keyboard:
                return FrontRoomsInput.WithKeyNames(line);
            default:
                return null;
        }
    }

    void WriteLines()
    {
        linesDirty = false;
        string a, dot = "", b = "", two = "";
        if (linesKind == Aim.Broken) { a = Strings.Broken; two = Strings.WontShut; }
        else if (linesKind == Aim.Read || linesKind == Aim.Examine)
        {
            // the map's words, as shown (DisplayPrompt); "." is Plex Mono between two Bayon runs (lab 1067); no line 2
            var s = linesText ?? "";
            var i = s.IndexOf('\u00B7');
            if (i < 0) a = s; else { a = s.Substring(0, i); dot = "\u00B7"; b = s.Substring(i + 1); }
        }
        else if (linesKind == Aim.Locked)
        {
            if (!linesHaveKey) { a = Strings.Locked; two = Strings.NeedsKey; }
            else
            {
                two = Strings.HasKey;
                var unlock = WithUseControl(Strings.Unlock, handheld);
                var i = (unlock ?? Strings.Unlock).IndexOf('\u00B7');
                if (unlock == null || i < 0) { a = i < 0 ? (unlock ?? Strings.Unlock) : Strings.Unlock.Substring(i + 1).Trim(); }
                else { a = unlock.Substring(0, i); dot = "\u00B7"; b = unlock.Substring(i + 1); }
            }
        }
        else a = "";
        l1a.text = a; l1dot.text = dot; l1b.text = b; l2.text = two;
        var runs = dot.Length > 0;
        l1aL.centre = !runs;
        l1dot.enabled = runs; l1b.enabled = runs;
        if (runs)
        {
            // three runs on one baseline, centred as one (SVG text-anchor middle over E + . + VERB)
            var wa = Width(l1a, a, 0f); var wd = Width(l1dot, dot, 0f); var wb = Width(l1b, b, 0f);
            var x0 = -(wa + wd + wb) / 2f;
            var sc = Mathf.Max(.01f, canvas.scaleFactor);
            x0 = Mathf.Round(x0 * sc) / sc;
            SetX(l1a, x0); SetX(l1dot, x0 + wa); SetX(l1b, x0 + wa + wd);
        }
        else SetX(l1a, 0f);
        SetX(l2, 0f);
        // the baselines on whole screen pixels too (capture stage 2026-10-22: at 1920 x 1200, canvas scale 1.054, the
        // design baselines 51 / 74 units below the dot landed at 53.76 / 78.0 px, the glyphs off the pixel grid)
        var scy = Mathf.Max(.01f, canvas.scaleFactor);
        float b1 = handheld ? -38f : -51f, b2 = handheld ? -58f : -74f;
        b1 = Mathf.Round(b1 * scy) / scy; b2 = Mathf.Round(b2 * scy) / scy;   // canvas units (phone: points), as Build sets them
        SetY(l1a, b1); SetY(l1dot, b1); SetY(l1b, b1); SetY(l2, b2);
    }

    static void SetX(Text t, float x) { var p = t.rectTransform.anchoredPosition; p.x = x; t.rectTransform.anchoredPosition = p; }
    static void SetY(Text t, float y) { var p = t.rectTransform.anchoredPosition; p.y = y; t.rectTransform.anchoredPosition = p; }

    float Width(Text t, string s, float tracking)
    {
        var f = t.font; if (f == null || string.IsNullOrEmpty(s)) return 0f;
        var ppu = Mathf.Max(.01f, canvas.scaleFactor);
        var size = Mathf.RoundToInt(t.fontSize * ppu);
        f.RequestCharactersInTexture(s, size, t.fontStyle);
        var w = 0f;
        for (var i = 0; i < s.Length; i++) { CharacterInfo ci; if (f.GetCharacterInfo(s[i], out ci, size, t.fontStyle)) w += ci.advance / ppu + tracking; }
        return w;
    }

    // ---------------------------------------------------------------- key panel, P4, U.3
    void StartFlight(float now)
    {
        panelId = flightId;
        gestureOn = false; gestureCancel = false; gestureQueued = false; tGesture = -99f;
        cueFlight = cueLand = true;   // #34: StepPanel raises both, as the flight leaves and lands (or the fade-in starts and ends)
        if (ReduceMotion)
        {
            // Reduce Motion: no flight; the panel fades in at its slot (0.15 s)
            ps = PS.Rest; panelFadeT = now;
            return;
        }
        ps = PS.Flight;
        panelFadeT = -99f;
        // the flight's lab clock: t = 0.5 at take-off (0.5 s of hold before it), plus the frame's lateness; KP-1b draws
        // that hold too (t = 0 on this frame)
        tFlightStart = now - flightLate;
        tFlight = tFlightStart + (flightFromHold ? HM.P4Hold : 0f);
        var W = canvasRect.rect.width; var H = canvasRect.rect.height;
        var k = (H / 1080f) * (FrontRoomsShotTimings.KeyPickup.HeldDistance / flightDepth)
                * (Mathf.Tan(38f * Mathf.Deg2Rad) / Mathf.Tan(flightFov * .5f * Mathf.Deg2Rad));
        flightS0x = PickupFactorX * k; flightS0y = PickupFactorY * k;
        var hx = flightViewport.x * W; var hy = (1f - flightViewport.y) * H;      // lab: top-down canvas coordinates
        flightX0 = hx - 36f * flightS0x; flightY0 = hy - 24f * flightS0y;
        Vector2 slot; float s1;
        Slot(out slot, out s1);
        flightX1 = slot.x; flightY1 = H - slot.y; flightS1 = s1;
        if (handheld) { flightCtrlX = 300f; flightCtrlY = 330f; } else { flightCtrlX = 620f; flightCtrlY = H - 120f; }
        simN = HM.SimulateP4(flightX0, flightCtrlX, flightX1, flightS0x, flightS1, 6f, 3.6f, 1.7f, .12f, 18f, simTable);
    }

    // The slot: the glyph's top-left (canvas coordinates, y up) and its rest scale.
    void Slot(out Vector2 slot, out float scale)
    {
        var o = ContentOrigin();
        slot = o + new Vector2(18f * pS, 0f);
        scale = pS;
    }

    // The rule's top-left in canvas coordinates (y up), from the map's own key panel (safe area included).
    readonly Vector3[] corners = new Vector3[4];
    Vector2 ContentOrigin()
    {
        keyPanelRef.GetWorldCorners(corners);
        var bl = (Vector2)panelRoot.InverseTransformPoint(corners[0]);
        var tl = (Vector2)panelRoot.InverseTransformPoint(corners[1]);
        return handheld ? tl : bl + new Vector2(0f, 48f);
    }

    bool Landed(float now) { return ps == PS.Flight && HM.P4Hold + (now - tFlight) >= HM.P4Hold + HM.P4Flight; }

    void StartGesture(float at)
    {
        if (ReduceMotion) return;                       // Reduce Motion: no gesture (no swing, no fold)
        if (ps == PS.Flight && !Landed(at)) { gestureQueued = true; tGesture = at; return; }   // a flight wins: wait for the landing
        if (ps == PS.Hidden) return;
        tGesture = at; gestureOn = true; gestureCancel = false; gestureQueued = false;
    }

    void CancelGesture(float now)
    {
        gestureQueued = false;
        if (!gestureOn || gestureCancel) return;
        gestureCancel = true; tCancel = now; gestureAtCancel = HM.UnlockU3(now - tGesture);
    }

    void StepPanel(float now)
    {
        if (ps == PS.Flight && heldShow && !heldId.Equals(panelId)) ps = PS.Hidden;
        if (!heldShow && ps == PS.Rest) ps = PS.Hidden;
        // a flight whose key is no longer held here (the player left the zone, the key was used) is dropped 0.1 s after it
        // started (KP-1b: after its hold started, not 0.5 s later at the take-off)
        if (!heldShow && ps == PS.Flight && now - tFlightStart > .1f) ps = PS.Hidden;
        if (ps == PS.Hidden) cueFlight = cueLand = false;   // #34: a flight dropped before it left or landed raises nothing more
        if (heldShow && ps == PS.Hidden) { ps = PS.Rest; panelId = heldId; }
        var show = ps != PS.Hidden;
        if (content.gameObject.activeSelf != show) content.gameObject.SetActive(show);
        if (glyph.gameObject.activeSelf != show) glyph.gameObject.SetActive(show);
        var fadeIn = HM.Clamp01((now - panelFadeT) / HM.FadeSeconds);
        panelGroup.alpha = show ? gameplayA * (panelExempt ? 1f : 1f - shotFade) * fadeIn : 0f;
        if (!show) { gestureOn = gestureQueued = false; return; }
        if (!panelTexSet || !panelTexId.Equals(panelId)) { panelTexId = panelId; panelTexSet = true; panelRingDensity = -1; panelAtlasDensity = -1; }
        var H = canvasRect.rect.height;
        Vector2 slot; float s1;
        Slot(out slot, out s1);
        var o = ContentOrigin();
        content.anchoredPosition = o;
        float tlx, tly, sx, sy, theta = 0f, ruleY = 1f, ruleX = 1f, labA = 1f, metaA = 1f, keySy = 1f;
        var rest = false;
        if (ps == PS.Flight)
        {
            var t = HM.P4Hold + (now - tFlight);
            // #34: the flight leaves at tFlight (KP-1b: after the glyph's 0.5 s hold at the hand) and lands when Landed
            // turns true (t = P4Hold + P4Flight); a late take-off can raise both on one frame, in that order
            if (cueFlight && now >= tFlight) { cueFlight = false; Cue(FrontRoomsHudSoundCue.PickupFlight); }
            if (cueLand && !cueFlight && t >= HM.P4Hold + HM.P4Flight) { cueLand = false; Cue(FrontRoomsHudSoundCue.PickupLand); }
            var s = HM.P4S(t);
            tlx = HM.Bez(s, flightX0, flightCtrlX, flightX1);
            tly = H - HM.Bez(s, flightY0, flightCtrlY, flightY1);
            sy = HM.Lerp(flightS0y, flightS1, s);
            var aniso = HM.Lerp(flightS0x / flightS0y, 1f, HM.Eio(HM.Clamp01((t - HM.P4Hold) / (.3f * HM.P4Flight))));
            sx = sy * aniso;
            // the landing swing (the path's simulate table) plus the 3D tag's held swing on its own clock (#18: a late
            // take-off carries the swing on from the hold's start)
            theta = HM.SimAt(simTable, simN, t) + HM.HeldSwing(flightHeldAt + (now - tFlight));
            ruleY = HM.P4Rule(t); labA = HM.P4Label(t); metaA = HM.P4Meta(t);
            if (gestureQueued && t >= HM.P4Hold + HM.P4Flight) { gestureQueued = false; tGesture = now; gestureOn = true; gestureCancel = false; }
            if (t >= 6f) { ps = PS.Rest; theta = 0f; }
        }
        else
        {
            tlx = slot.x; tly = slot.y; sx = sy = s1; rest = true;
            // #34 under Reduce Motion (StartFlight: no flight): the fade-in at the slot starts (PickupFlight) and ends (PickupLand)
            if (cueFlight) { cueFlight = false; Cue(FrontRoomsHudSoundCue.PickupFlight); }
            if (cueLand && now - panelFadeT + 1e-5f >= HM.FadeSeconds) { cueLand = false; Cue(FrontRoomsHudSoundCue.PickupLand); }
        }
        if (gestureOn)
        {
            HM.KeyGesture q;
            if (gestureCancel)
            {
                var e = HM.Eio(HM.Clamp01((now - tCancel) / GestureCancelEase));
                q = gestureAtCancel;
                q.dx *= 1f - e; q.dy *= 1f - e; q.swing *= 1f - e; q.sy = HM.Lerp(q.sy, 1f, e); q.rule = HM.Lerp(q.rule, 1f, e);
                if (e >= 1f) gestureOn = false;
            }
            else
            {
                var r = now - tGesture;
                q = r >= 0f ? HM.UnlockU3(r) : HM.GestureRest();
                if (r >= HM.GestureSeconds) gestureOn = false;
            }
            tlx += q.dx * pS; tly -= q.dy * pS;
            theta += q.swing; keySy = q.sy; ruleX = q.rule;
            rest = rest && q.dx == 0f && q.dy == 0f;
        }
        // whole screen pixels at rest (the canvas has no pixel snapping)
        var scf = Mathf.Max(.01f, canvas.scaleFactor);
        if (rest) { tlx = Mathf.Round(tlx * scf) / scf; tly = Mathf.Round(tly * scf) / scf; }
        glyph.anchoredPosition = new Vector2(tlx, tly);
        glyph.localScale = new Vector3(sx, sy, 1f);
        swing.localEulerAngles = new Vector3(0f, 0f, -theta);
        keyFold.localScale = new Vector3(1f, keySy, 1f);
        swingKL.localEulerAngles = swing.localEulerAngles;   // the keylines follow the swing and the fold (#22)
        keyFoldKL.localScale = keyFold.localScale;
        rule.localScale = new Vector3(ruleX, ruleY, 1f);
        labelGroup.alpha = labA;
        var lw = Mathf.Clamp01((labA - LabelKeylineFrom) / (1f - LabelKeylineFrom));
        if (lw != labelOutlineW)
        {
            labelOutlineW = lw;
            var lc = labelOutline.effectColor; lc.a = FrontRoomsHudStyle.OutlineTapAlpha * lw; labelOutline.effectColor = lc;
        }
        labelRect.anchoredPosition = new Vector2(HM.P4LabelDx * (1f - labA) * pS, 0f);
        metaGroup.alpha = metaA;
        // textures at the density the glyph is drawn at
        var gpx = scf * Mathf.Max(sx, sy);
        var gd = DensityFor(gpx, FrontRoomsKeyHudParts.GlyphDensities);
        if (gd != panelRingDensity)
        {
            panelRingDensity = gd;
            ringTag.texture = Resources.Load<Texture2D>(FrontRoomsKeyHudParts.RingTagPath((int)panelId.shape, (int)panelId.tint, gd));
            keyImg.texture = Resources.Load<Texture2D>(FrontRoomsKeyHudParts.KeyPath(gd));
            ringTagKL.texture = Resources.Load<Texture2D>(FrontRoomsKeyHudParts.RingTagKeylinePath((int)panelId.shape, gd));
            keyKL.texture = Resources.Load<Texture2D>(FrontRoomsKeyHudParts.KeyKeylinePath(gd));
        }
        var nd = DensityFor(gpx, FrontRoomsKeyHudParts.NumberDensities);
        if (nd != panelAtlasDensity)
        {
            panelAtlasDensity = nd;
            // the panel atlas: the numbers rendered on the B glyph's own pixel grid at this density (exact at rest)
            numImg.texture = Resources.Load<Texture2D>(FrontRoomsKeyHudParts.TagNumberAtlasPath((int)panelId.shape, nd));
            Vector4 rect, uv;
            var sh = (int)panelId.shape;
            FrontRoomsKeyHudParts.TagCell(sh, nd, panelId.number, out rect, out uv);
            numImg.uvRect = new Rect(uv.x, uv.y, uv.z - uv.x, uv.w - uv.y);
            var cw = rect.z - rect.x; var ch = rect.w - rect.y;
            numImg.rectTransform.sizeDelta = new Vector2(cw, ch);
            numImg.rectTransform.pivot = new Vector2(-rect.x / cw, -rect.y / ch);
            var c = HM.GeoOf((HM.Shape)sh).c;
            numImg.rectTransform.anchoredPosition = new Vector2(c.x - 35f, -(c.y - 10f));
        }
    }

#if UNITY_EDITOR
    // ---------------------------------------------------------------- harness
    /// <summary>Harness: one line of every channel this frame (allocates; the harness only).</summary>
    public string DebugLine
    {
        get
        {
            var ic = System.Globalization.CultureInfo.InvariantCulture;
            Func<float, string> F = v => v.ToString("0.#####", ic);
            // cs / umode / lockIn first: the 10-14 capture harness and wiring.py read them
            var cs = active == null ? "Idle" : active == lockS ? (lockS.unlocking ? "Unlock" : "Lock") : active == brokenS ? "Broken" : "Read";
            return "cs=" + cs + "\tumode=" + lockS.UModeName + "\tlockIn=" + lockS.LockInNow
                + "\tstate=" + (active == null ? "Idle" : active.Kind.ToString()) + "\tphase=" + (active == null ? "-" : active.Phase)
                + "\taim=" + aimIn + "\tps=" + ps + "\tu=" + F(lockS.u)
                + "\tlockVis=" + lockVisible + "\tdoorVis=" + doorVisible + "\teyeVis=" + eyeVisible + "\tdot=" + F(dotA) + "\tline=" + F(lineA)
                + "\tx=" + F(pose.x) + "\ty=" + F(pose.y) + "\ttx=" + F(pose.tx) + "\trot=" + F(pose.rot) + "\top=" + F(pose.op)
                + "\tbodyW=" + F(pose.body.w) + "\tbodyH=" + F(pose.body.h) + "\tbodyR=" + F(pose.body.r)
                + "\tinsW=" + F(pose.ins.w) + "\tinsH=" + F(pose.ins.h) + "\tinsR=" + F(pose.ins.r)
                + "\tshY=" + F(pose.shY) + "\tshK=" + F(pose.shK) + "\treveal=" + F(pose.reveal)
                + "\tm=" + F(door.m) + "\tfr=" + F(door.fr) + "\tkn=" + F(door.kn) + "\tth=" + F(door.th) + "\tdy=" + F(door.dy) + "\tdoorA=" + F(doorA)
                + "\teyeW=" + F(eye.w) + "\teyeUp=" + F(eye.up) + "\teyeLo=" + F(eye.lo) + "\teyePr=" + F(eye.pr) + "\teyeC=" + F(eye.c)
                + "\teyePdy=" + F(eye.pdy) + "\teyeLid=" + F(eye.lidOp) + "\teyeOp=" + F(eye.op) + "\tlastBlink=" + F(eyeS.lastBlink)
                + "\tcrossA=" + F(crossGroup.alpha) + "\tpanelA=" + F(panelGroup.alpha) + "\texempt=" + panelExempt
                + "\ttUnlock=" + F(lockS.tUnlock) + "\ttRet=" + F(lockS.tRet) + "\tbolt=" + lockS.boltSeen + "\ttDeny=" + F(lockS.tDeny)
                + "\tglyphX=" + F(glyph.anchoredPosition.x) + "\tglyphY=" + F(glyph.anchoredPosition.y) + "\tglyphSx=" + F(glyph.localScale.x)
                + "\tglyphSy=" + F(glyph.localScale.y) + "\tswing=" + F(-Mathf.DeltaAngle(0f, swing.localEulerAngles.z))
                + "\tkeySy=" + F(keyFold.localScale.y) + "\truleX=" + F(rule.localScale.x) + "\truleY=" + F(rule.localScale.y)
                + "\tlabelA=" + F(labelGroup.alpha) + "\tmetaA=" + F(metaGroup.alpha) + "\ttGesture=" + F(tGesture) + "\tgesture=" + gestureOn
                + "\tl1=" + l1a.text + l1dot.text + l1b.text + "\tl2=" + l2.text + "\tl2a=" + F(line2A) + "\tpx=" + F(PxPerUnit())
                + "\ttoken=" + FloorToken(panelId.number) + "\teyeVar=" + Eye
                + "\topen=" + dotOpen + "\ttDot=" + F(tDot) + "\tdwellHold=" + dwellHold;
        }
    }
    /// <summary>Harness: the crosshair root (for the capture's crop) and the panel root.</summary>
    public RectTransform CrosshairRoot => crossRoot;
    public RectTransform PanelRoot => panelRoot;
    /// <summary>Harness: the eye's pose this frame (lab convention) and whether it is drawn.</summary>
    public HM.EyePose EyePoseNow => eye;
    public bool EyeVisibleNow => eyeVisible;
    public float LineAlphaNow => lineA;
    public float DotAlphaNow => dotA;
    /// <summary>Playtests: line 1 as drawn this frame (its runs joined; "" while the line is at alpha 0 or the crosshair
    /// group is hidden), so a check can tell the key HUD really draws the prompt it owns.</summary>
    public string Line1Drawn => lineA > 0f && crossGroup != null && crossGroup.alpha > 0f
        ? l1a.text + (l1dot.enabled ? l1dot.text + l1b.text : "") : "";
    /// <summary>Playtests: the keyline distance on line 1 / line 2 (G6, DECISIONS #38).</summary>
    public float Line1KeylineDistance => l1a.GetComponent<Outline>().effectDistance.x;
    public float Line2KeylineDistance => l2.GetComponent<Outline>().effectDistance.x;
#endif
}

/// <summary>
/// DECISIONS #34: the HUD's sound cues (FrontRoomsKeyHud.SoundCue), one per motion, raised as the motion starts. Bound by
/// the sound line to 2D UI/* events on the UI bus. APPEND-ONLY: new values go at the end; never reorder or renumber.
/// </summary>
public enum FrontRoomsHudSoundCue { PickupFlight, PickupLand, LockTug, UnlockDrop, BrokenBuild, BrokenKnock }
