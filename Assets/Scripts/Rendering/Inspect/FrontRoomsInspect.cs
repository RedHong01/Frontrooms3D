using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EXAMINE, the close-up inspect system (research/inspect/10_design.md; visual chat 游戏视觉).
/// The facade the game calls (map chat contracts C3-C15): a registry of FrontRoomsInspectable
/// targets, Find (the aim), CanOpen (a pose solves and its path is clear), Open / OpenHeld (begins
/// ShotKind.Inspect on the camera rig), Tick (look, pan, zoom, turn; Phase.Inspect only), Close (the
/// return) and Abort (teardown). A hidden runner's LateUpdate places the object kind, fades the hint
/// and drives FrontRoomsInspectLook while Active (including the 0.40 s return).
///
/// The world is paused by the game (Phase.Inspect + timeScale 0). Everything here runs on the inspect
/// clock (Dt): Time.captureDeltaTime under a capture harness, else unscaled time.
/// Allocation-free per frame once warm (no LINQ, no closures; delegates are cached).
/// </summary>
public static class FrontRoomsInspect
{
    /// <summary>Test-bed kits (ship: false) register only when true. Reset in Abort.</summary>
    public static bool DevTargets;
    /// <summary>
    /// The game passes a real zoom input (touch T3 + the map chat's C7 follow-up set this). Until then the zoom step is
    /// never offered and the CLOSER hint never shows (verifier 2026-10-08: v1 asked players to scroll while scroll did nothing).
    /// </summary>
    public static bool ZoomInputWired;
    /// <summary>The bottom of the screen kept clear of a zoomed region for the hint line (viewport fraction: the hint's
    /// glyph bottom on the 72 px margin, ~16 px of cap, 40 px clear, at 1080).</summary>
    public const float HintBand = .12f;
    /// <summary>The object kind fills this share of the frame (§3.3 E2: 50 % h), from its real render bounds.</summary>
    public const float ObjectFill = .5f;
    /// <summary>The framing's reference height for the texel cap (px): framing never changes with the resolution.</summary>
    public const float ReferenceHeightPx = 1080f;
    /// <summary>A box on a collider face wins up to 2 cm behind the physics hit.</summary>
    public const float SurfaceSlack = .02f;
    /// <summary>The inspect clock: captureDeltaTime under a harness, else unscaled.</summary>
    public static float Dt => Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime;

    public static event Action<FrontRoomsInspectable> Opened, Closed;
    /// <summary>The target went away mid close-up: the game must EndInspect + CancelShot(shot, 0).</summary>
    public static event Action AbortRequested;
    /// <summary>The held key is back at its held pose (the map resumes its key timeline).</summary>
    public static event Action HeldReturned;

    // ---------------------------------------------------------------- state
    static readonly List<FrontRoomsInspectable> targets = new List<FrontRoomsInspectable>(64);
    static FrontRoomsInspectable current, lastShown;
    static ShotHandle shot;
    static FrontRoomsCameraRig shotRig;
    static bool active, closing, held;
    static float age;
    static InspectDef def;
    static InspectKind kind;
    static FrontRoomsInspectFraming.Result solved;
    static FrontRoomsInspectFraming.Zoom zoomStep;
    static Pose face;
    static Vector3 faceRight, faceUp, viewDir;
    static Vector2 zoomCentre, panMin, panMax;
    static float baseFov, zoomDistance, fovDeltaShot, lightW;
    // damped controls
    static float zoom, zoomVel, zoomTarget;
    static Vector2 look, lookVel, lookTarget, pan, panVel, panTarget, turn, turnVel, turnTarget;
    // object kind
    static Transform display, displayParent;
    static Vector3 displayLocalPosition, displayLocalScale, centreOffsetLocal;
    static Quaternion displayLocalRotation, presentRotationLocal;
    static Pose displayStart;
    static Vector3 displayLift;
    /// <summary>The object kind first lifts off its mount along the face normal (at most this far) before it travels to the eye.</summary>
    public const float ObjectLift = .15f;
    static float objectDistance, objectZoomDistance;
    // hints
    static readonly int[] hintCounts = new int[3];
    static readonly int[] hintQueue = new int[3];
    static int hintQueueLength, hintQueueIndex;
    static float hintClock;
    static string hintText = "";
    static float hintAlpha;
    // world
    static FrontRoomsMapWorld map;
    static Transform ignoreBody;
    static readonly RaycastHit[] sweepHits = new RaycastHit[16];
    static readonly Collider[] overlapHits = new Collider[16];
    static readonly Func<Vector3, Vector3, float, float> SweepDelegate = SweepWorld;
    static readonly Func<Vector3, float, bool> OverlapDelegate = OverlapWorld;
    static readonly Func<Pose> PoseDelegate = CurrentPose;
    static readonly Func<float> FovDelegate = CurrentFovDelta;
    static Runner runner;
    // solve cache
    struct CacheEntry
    {
        public Vector3 eye;
        public Quaternion eyeRotation;
        public float aspect, fov;
        public Rect safe, free;
        public Vector3 facePosition;
        public Quaternion faceRotation;
        public FrontRoomsInspectFraming.Result result;
        public bool valid;
    }
    static readonly Dictionary<int, CacheEntry> cache = new Dictionary<int, CacheEntry>(64);
    static FrontRoomsCameraRig cameraRig;
    static Camera rigCamera;
    // transcript reframe (SetTranscript during a close-up): the base pose eases from where it is to the new solved.pose
    struct ReframeLeg
    {
        public Pose from, to;
        public float k, seconds;
    }
    static ReframeLeg leg, prevLeg;
    static bool showTranscript, reframing, prevLegLive;
    static string reframeWhy = "";

    // ---------------------------------------------------------------- read-outs
    public static bool Active => active;
    public static bool Closing => closing;
    public static float Age => age;
    public static FrontRoomsInspectable Current => current;
    public static string HintText => active && !closing ? hintText : "";
    public static float HintAlpha => active && !closing ? hintAlpha : 0f;
    /// <summary>
    /// The transcript panel shows: the desktop setting (C24; it lasts until Abort) or a phone's TEXT chip. Setting it is
    /// <see cref="SetTranscript"/>, so during a close-up the camera eases to the framing for the new free rect.
    /// </summary>
    public static bool ShowTranscript { get => showTranscript; set => SetTranscript(value); }
    /// <summary>A transcript reframe is easing the camera (SetTranscript during a close-up).</summary>
    public static bool Reframing => reframing;
    /// <summary>Why the last transcript toggle kept the framing ("" = it reframed, or none was asked).</summary>
    public static string ReframeWhy => reframeWhy;
    public static int Count => targets.Count;
    public static InspectKind CurrentKind => kind;
    public static FrontRoomsInspectFraming.Result LastSolve => solved;
    public static FrontRoomsInspectFraming.Zoom ZoomStep => zoomStep;
    public static float ZoomLevel => zoom;
    public static float ObjectDistance => Mathf.Lerp(objectDistance, objectZoomDistance, zoom);
    public static ShotHandle Shot => shot;
    public static Transform Display => display;

    /// <summary>The transcript of the current close-up as plain text (glow lines only while legible, §3.4; region heads
    /// by name, see TranscriptPlain). Empty when off or none.</summary>
    public static string TranscriptText
    {
        get
        {
            if (!active || !ShowTranscript || def == null) return "";
            // the ink gate opens below 0.15 (A4 / G = 1 reach 1.30 displayed)
            return TranscriptPlain(FrontRoomsInspectCatalog.Transcript(def.transcript), FrontRoomsInspectLook.OwnLight < .15f);
        }
    }

    /// <summary>
    /// A catalog transcript as plain text, one line per line, never a raw marker (format: FrontRoomsInspectCatalog.Transcripts).
    /// A glow line ('\u0001') shows only when glowLegible. A region head ('\u0002') shows as its name after a blank line, and
    /// only when a line of its region shows. Lines before the first head (the title band) come first. A region whose first
    /// shown line repeats its head word for word says it once, as the head (平面视觉 G9, 10-24). Empty for null / none.
    /// </summary>
    public static string TranscriptPlain(string[] lines, bool glowLegible)
    {
        if (lines == null || lines.Length == 0) return "";
        var sb = new System.Text.StringBuilder();
        string head = null;
        foreach (var line in lines)
        {
            var mark = line.Length > 0 ? line[0] : '\0';
            if (mark == '\u0002') { head = line.Substring(1); continue; }
            var glow = mark == '\u0001';
            if (glow && !glowLegible) continue;
            var text = glow ? line.Substring(1) : line;
            if (head != null)
            {
                if (sb.Length > 0) sb.Append("\n\n");
                sb.Append(head);
                var repeat = text == head;
                head = null;
                if (repeat) continue;
            }
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(text);
        }
        return sb.ToString();
    }

    /// <summary>The transcript panel's share of the safe area, at its right (平面视觉 G9; FrontRoomsTouchLayout.TranscriptPanel).</summary>
    public const float TranscriptPanelShare = .36f;

    /// <summary>
    /// The viewport rect left free for the transcript panel (the right 36 % is reserved; safe area on phones). On a handheld the
    /// free rect ends where the panel begins: the panel stands in the bleed safe area (FrontRoomsTouchLayout.TranscriptPanel), so
    /// on the iPhone 16 Pro the subject keeps left of x 542 of 874 pt, the panel's x 542–812.
    /// </summary>
    public static Rect FreeScreenRect
    {
        get
        {
            var safe = SafeViewport();
            if (!showTranscript) return safe;
            if (!FrontRoomsHandheld.Active) return new Rect(safe.x, safe.y, safe.width * .64f, safe.height);
            var px = FrontRoomsHandheld.ScreenPixels;
            var bleed = FrontRoomsHandheld.BleedSafeAreaPixels;
            var panelLeft = px.x > 0f && bleed.width > 0f ? (bleed.x + bleed.width * (1f - TranscriptPanelShare)) / px.x : safe.x + safe.width * (1f - TranscriptPanelShare);
            return Rect.MinMaxRect(safe.xMin, safe.yMin, Mathf.Clamp(panelLeft, safe.xMin, safe.xMax), safe.yMax);
        }
    }

    // ---------------------------------------------------------------- registry
    /// <summary>Called by FrontRoomsInspectable (and the EditMode tests).</summary>
    public static void Register(FrontRoomsInspectable t)
    {
        if (t == null || targets.Contains(t)) return;
        targets.Add(t);
    }

    public static void Unregister(FrontRoomsInspectable t)
    {
        if (t == null) return;
        targets.Remove(t);
        cache.Remove(t.GetInstanceID());
        if (lastShown == t) lastShown = null;
        if (active && current == t && !closing)
        {
            if (AbortRequested != null) AbortRequested.Invoke();
            else Abort();
        }
        else if (active && current == t) Finish();
    }

    /// <summary>Every registered target (tests and the Level Designer walker).</summary>
    public static int CopyTargets(List<FrontRoomsInspectable> into)
    {
        into.Clear();
        into.AddRange(targets);
        return into.Count;
    }

    // ---------------------------------------------------------------- aim
    /// <summary>Nearest available, in-reach, in-angle target whose padded world OBB the ray enters before maxDistance. No allocation.</summary>
    public static bool Find(in Ray ray, float maxDistance, out FrontRoomsInspectable target)
    {
        target = null;
        var best = float.PositiveInfinity;
        for (var i = targets.Count - 1; i >= 0; i--)
        {
            var t = targets[i];
            if (t == null) { targets.RemoveAt(i); continue; }
            if (!t.isActiveAndEnabled || t.Def == null) continue;
            if (!t.Def.ship && !DevTargets) continue;
            if (t.Available != null && !t.Available()) continue;
            if (!t.Raycast(ray, out var d) || d > maxDistance || d >= best) continue;
            var sticky = t == lastShown;
            if (d > t.Def.Reach + (sticky ? FrontRoomsShotTimings.Inspect.ReachHysteresis : 0f)) continue;
            if (t.Kind != InspectKind.Object)
            {
                var f = t.Face;
                var angle = Vector3.Angle(f.rotation * Vector3.forward, ray.origin - f.position);
                if (angle > t.Def.ApproachAngle + (sticky ? FrontRoomsShotTimings.Inspect.AngleHysteresis : 0f)) continue;
            }
            best = d;
            target = t;
        }
        lastShown = target;
        return target != null;
    }

    static Camera CameraOf(FrontRoomsCameraRig rig)
    {
        if (rig == null) return null;
        if (rig != cameraRig || rigCamera == null)
        {
            cameraRig = rig;
            rigCamera = rig.Eye != null ? rig.Eye.GetComponentInChildren<Camera>(true) : null;
            ignoreBody = rig.Eye != null ? rig.Eye.parent : null;
        }
        return rigCamera;
    }

    static Rect SafeViewport()
    {
        if (!FrontRoomsHandheld.Active) return new Rect(0f, 0f, 1f, 1f);
        var px = FrontRoomsHandheld.ScreenPixels;
        var s = FrontRoomsHandheld.SafeAreaPixels;
        if (px.x <= 0f || px.y <= 0f) return new Rect(0f, 0f, 1f, 1f);
        return new Rect(s.x / px.x, s.y / px.y, s.width / px.x, s.height / px.y);
    }

    /// <summary>The solver input for a target from the rig's base eye, camera and the face now.</summary>
    public static FrontRoomsInspectFraming.Input InputFor(FrontRoomsInspectable t, Pose eye, float baseFovDeg, float aspect, float nearPlane, bool withWorld)
    {
        var d = t.Def;
        var f = t.Face;
        return new FrontRoomsInspectFraming.Input
        {
            face = f,
            size = t.Size,
            depthFront = t.DepthFront,
            depthBack = t.DepthBack,
            margin = d.Margin,
            screen = d.ScreenRect ?? default,
            safeArea = SafeViewport(),
            free = FreeScreenRect,
            // The FOV the rig draws: a handheld view squarer than 16:9 widens it (FrontRoomsHandheld.AspectFov, 2026-10-24);
            // desktop and wider views draw it as authored.
            fov = FrontRoomsHandheld.AspectFov(baseFovDeg + d.FovDelta, aspect),
            pathFov = FrontRoomsHandheld.AspectFov(baseFovDeg, aspect),
            aspect = aspect,
            nearPlane = nearPlane,
            eye = eye.position,
            minDist = d.MinDist,
            maxDist = d.MaxDist,
            maxAngle = d.MaxAngleFor(f.position.y),
            maxYawOff = d.MaxYawOff,
            distMaxA = d.DistMaxA,
            minY = eye.position.y - FrontRoomsShotTimings.Inspect.MaxDrop,
            maxY = eye.position.y + FrontRoomsShotTimings.Inspect.MaxRise,
            dist = d.dist,
            sweep = withWorld ? SweepDelegate : null,
            overlap = withWorld ? OverlapDelegate : null,
        };
    }

    /// <summary>The last cached solve for a target (gizmos and tests).</summary>
    public static FrontRoomsInspectFraming.Result LastSolveFor(FrontRoomsInspectable t) =>
        t != null && cache.TryGetValue(t.GetInstanceID(), out var e) ? e.result : default;

    static FrontRoomsInspectFraming.Result SolveFor(FrontRoomsInspectable t, FrontRoomsCameraRig rig)
    {
        var cam = CameraOf(rig);
        var eye = rig.BaseEye;
        var aspect = cam != null ? cam.aspect : 16f / 9f;
        var near = cam != null ? cam.nearClipPlane : .06f;
        var f = t.Face;
        var safe = SafeViewport();
        var free = FreeScreenRect;
        var id = t.GetInstanceID();
        if (cache.TryGetValue(id, out var e) && e.valid
            && (e.eye - eye.position).sqrMagnitude < 1e-4f
            && Quaternion.Angle(e.eyeRotation, eye.rotation) < .5f
            && Mathf.Abs(e.aspect - aspect) < 1e-4f && Mathf.Abs(e.fov - rig.BaseFov) < 1e-3f
            && e.safe == safe && e.free == free
            && (e.facePosition - f.position).sqrMagnitude < 1e-8f && Quaternion.Angle(e.faceRotation, f.rotation) < .01f)
            return e.result;
        FrontRoomsInspectFraming.Result result;
        if (t.Kind == InspectKind.Object) result = SolveObject(t, eye);
        else
        {
            ignoreBody = rig.Eye != null ? rig.Eye.parent : null;
            currentSolveTarget = t;
            var input = InputFor(t, eye, rig.BaseFov, aspect, near, true);
            result = FrontRoomsInspectFraming.Solve(input);
            currentSolveTarget = null;
        }
        cache[id] = new CacheEntry { valid = true, eye = eye.position, eyeRotation = eye.rotation, aspect = aspect, fov = rig.BaseFov, safe = safe, free = free, facePosition = f.position, faceRotation = f.rotation, result = result };
        return result;
    }

    static FrontRoomsInspectable currentSolveTarget;

    /// <summary>§3.2 step 7: the object comes to its distance in front of the eye, clamped by a sphere sweep; minDist must be reachable.</summary>
    static FrontRoomsInspectFraming.Result SolveObject(FrontRoomsInspectable t, Pose eye)
    {
        var d = t.Def;
        var forward = eye.rotation * Vector3.forward;
        var want = d.BaseDist > 0f ? d.BaseDist : Mathf.Max(d.MinDist, t.BoundingRadius * 2f / .5f / (2f * Mathf.Tan((76f + d.FovDelta) * .5f * Mathf.Deg2Rad)));
        var radius = t.BoundingRadius + .02f;
        var reach = SweepWorld(eye.position, eye.position + forward * want, radius);
        var minDist = Mathf.Min(d.MinDist, .15f);
        if (reach < minDist) return new FrontRoomsInspectFraming.Result { ok = false, why = "no room in front of the eye" };
        var dist = Mathf.Min(want, reach);
        return new FrontRoomsInspectFraming.Result { ok = true, why = "", pose = eye, distance = dist, direction = -forward };
    }

    /// <summary>A pose solves for this eye, aspect, FOV and safe area, and its path is clear (cached).</summary>
    public static bool CanOpen(FrontRoomsInspectable target, FrontRoomsCameraRig rig)
    {
        // Not gated on Active: during the 0.40 s return the prompt may show; the game's transient gate swallows E (C4).
        if (target == null || rig == null || target.Def == null) return false;
        return SolveFor(target, rig).ok;
    }

    // ---------------------------------------------------------------- world queries (the solver's delegates)
    static bool Ignored(Collider c) => c == null || c.isTrigger || (ignoreBody != null && c.transform.IsChildOf(ignoreBody));

    /// <summary>The clear distance a sphere travels from a to b: colliders (not the body, not triggers) and the other targets' boxes.</summary>
    static float SweepWorld(Vector3 a, Vector3 b, float radius)
    {
        var delta = b - a;
        var length = delta.magnitude;
        if (length < 1e-5f) return 0f;
        var dir = delta / length;
        var reach = length;
        if (Physics.CheckSphere(a, radius, ~0, QueryTriggerInteraction.Ignore))
        {
            // Starting in contact with the body only is fine; a wall at the eye blocks.
            var n = Physics.OverlapSphereNonAlloc(a, radius, overlapHits, ~0, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < n; i++)
                if (!Ignored(overlapHits[i])) return 0f;
        }
        var count = Physics.SphereCastNonAlloc(a, radius, dir, sweepHits, length, ~0, QueryTriggerInteraction.Ignore);
        for (var i = 0; i < count; i++)
        {
            if (Ignored(sweepHits[i].collider)) continue;
            reach = Mathf.Min(reach, sweepHits[i].distance);
        }
        // Render-only targets have no colliders: their boxes block the path too (never the one being solved).
        var ray = new Ray(a, dir);
        for (var i = 0; i < targets.Count; i++)
        {
            var other = targets[i];
            if (other == null || other == currentSolveTarget || other == current || !other.isActiveAndEnabled) continue;
            if (other.Raycast(ray, out var d) && d < reach) reach = d;
        }
        return reach;
    }

    static bool OverlapWorld(Vector3 p, float radius)
    {
        var n = Physics.OverlapSphereNonAlloc(p, radius, overlapHits, ~0, QueryTriggerInteraction.Ignore);
        for (var i = 0; i < n; i++)
            if (!Ignored(overlapHits[i])) return true;
        return false;
    }

    // ---------------------------------------------------------------- open / close
    /// <summary>Begins ShotKind.Inspect (fullPose, ease Sine, rotBlendOut .15, lockMove, lockLook, cancel Relay). Null if it cannot.</summary>
    public static ShotHandle Open(FrontRoomsInspectable target, FrontRoomsCameraRig rig)
    {
        if (target == null || rig == null || active || target.Def == null) return null;
        var result = SolveFor(target, rig);
        if (!result.ok) return null;
        current = target;
        def = target.Def;
        kind = target.Kind;
        held = false;
        return Begin(rig, result, target.Face, target.Size, target.Kind == InspectKind.Object ? target.Display : null, target.BoxCentreWorld);
    }

    /// <summary>Key hand-off (C21): the held key's root moves to the camera; on close it returns to heldPose, then HeldReturned fires.</summary>
    public static ShotHandle OpenHeld(Transform heldDisplay, string kit, Pose heldPose, FrontRoomsCameraRig rig)
    {
        if (heldDisplay == null || rig == null || active) return null;
        var info = FrontRoomsKitLibrary.GetInfo(kit);
        var d = info != null && info.inspect != null && info.inspect.IsSet ? info.inspect : FrontRoomsInspectCatalog.Fallback(kit);
        if (d == null) d = new InspectDef { kind = "object", verb = "look", dist = new[] { .20f, .15f }, minDist = .15f, sound = "keys" };
        current = null;
        def = d;
        kind = InspectKind.Object;
        held = true;
        heldDisplay.SetPositionAndRotation(heldPose.position, heldPose.rotation);
        var eye = rig.BaseEye;
        var reach = SweepWorld(eye.position, eye.position + eye.rotation * Vector3.forward * Mathf.Max(.2f, d.BaseDist), .08f);
        var result = new FrontRoomsInspectFraming.Result { ok = reach >= .15f, pose = eye, distance = Mathf.Min(d.BaseDist > 0f ? d.BaseDist : .2f, reach) };
        if (!result.ok) { def = null; held = false; return null; }
        return Begin(rig, result, new Pose(heldDisplay.position, heldDisplay.rotation), Vector2.one * .1f, heldDisplay, RendererCentre(heldDisplay, heldDisplay.position));
    }

    static ShotHandle Begin(FrontRoomsCameraRig rig, FrontRoomsInspectFraming.Result result, Pose facePose, Vector2 size, Transform objectDisplay, Vector3 objectCentre)
    {
        var cam = CameraOf(rig);
        solved = result;
        face = facePose;
        faceRight = FrontRoomsInspectFraming.FaceRight(face);
        faceUp = FrontRoomsInspectFraming.FaceUp(face);
        viewDir = result.direction;
        fovDeltaShot = def.FovDelta;
        baseFov = rig.BaseFov + fovDeltaShot;
        shotRig = rig;
        age = 0f;
        closing = false;
        reframing = prevLegLive = false;
        reframeWhy = "";
        zoom = zoomVel = zoomTarget = 0f;
        look = lookVel = lookTarget = pan = panVel = panTarget = turn = turnVel = turnTarget = Vector2.zero;
        display = null;
        var eye = rig.BaseEye;
        var viewportHeight = cam != null ? cam.pixelHeight : 1080f;
        var screenArcmin = FrontRoomsInspectFraming.ScreenArcmin(FrontRoomsHandheld.Active, FrontRoomsHandheld.IsTablet);
        var input = current != null && kind != InspectKind.Object
            ? InputFor(current, eye, rig.BaseFov, cam != null ? cam.aspect : 16f / 9f, cam != null ? cam.nearClipPlane : .06f, false)
            : new FrontRoomsInspectFraming.Input { fov = baseFov, minDist = def.MinDist };
        if (kind == InspectKind.Object)
        {
            objectDistance = result.distance;
            var zd = def.ZoomDist > 0f ? def.ZoomDist : .7f * objectDistance;
            objectZoomDistance = Mathf.Max(Mathf.Min(def.MinDist, .15f), zd);
            BindDisplay(objectDisplay, eye, objectCentre);
            // Fill: the presented render bounds take ObjectFill of the frame (§3.3 E2), never past the texel cap at the
            // reference height, never wider than base + fovDelta (v1 left the key at 29 % h).
            fovDeltaShot = ObjectFovDelta(rig.BaseFov, cam != null ? cam.aspect : 16f / 9f);
            baseFov = rig.BaseFov + fovDeltaShot;
            zoomStep = new FrontRoomsInspectFraming.Zoom { distance = objectZoomDistance, fov = baseFov, offered = ZoomInputWired && objectZoomDistance <= objectDistance * (1f - FrontRoomsInspectFraming.ZoomMinGain) };
        }
        else
        {
            var cap = def.SmallestCapMm;
            zoomStep = FrontRoomsInspectFraming.ZoomFor(input, cap, screenArcmin, viewportHeight, def.TexelsPerMetre, result.distance);
            if (def.ZoomDist > 0f && cap <= 0f) { zoomStep.distance = def.ZoomDist; zoomStep.offered = def.ZoomDist <= result.distance * (1f - FrontRoomsInspectFraming.ZoomMinGain); }
            if (!ZoomInputWired) zoomStep.offered = false;   // no zoom input yet: no step, no CLOSER hint
            zoomDistance = zoomStep.distance;
            var t = Mathf.Tan(zoomStep.fov * .5f * Mathf.Deg2Rad);
            var aspect = cam != null ? cam.aspect : 16f / 9f;
            var viewH = 2f * zoomDistance * t;
            var viewW = viewH * aspect;
            // The step's FOV was solved as drawn; the rig takes it authored (it widens it again on a handheld squarer than 16:9).
            if (zoomStep.fov < input.fov - 1e-4f) zoomStep.fov = AuthoredFov(zoomStep.fov, aspect);
            else if (FrontRoomsHandheld.Active) zoomStep.fov = baseFov;
            if (RegionNearest(result.centreOnFace, size, out var regionCentre))
            {
                // The first zoom-in frames the readable region nearest the base view's centre (the placard's legend),
                // centred in the screen above the hint band, so its 2.4 mm line sits clear of the hint (v1 aimed at the
                // screen centre's point of the face: the legend landed at viewport x 0.76, cut top and bottom).
                zoomCentre = new Vector2(regionCentre.x, regionCentre.y - HintBand * .5f * viewH);
            }
            else
            {
                // No region: the point of the face nearest the screen centre, inset so the zoom view stays on the face.
                var insetX = Mathf.Max(0f, (size.x - viewW) * .5f);
                var insetY = Mathf.Max(0f, (size.y - viewH) * .5f);
                zoomCentre = new Vector2(Mathf.Clamp(result.centreOnFace.x, -insetX, insetX), Mathf.Clamp(result.centreOnFace.y, -insetY, insetY));
            }
            // Pan limits of the view centre (the framed rect + margin), swept at the zoom distance so the rig's clamp never fires.
            panMin = result.panMin - zoomCentre;
            panMax = result.panMax - zoomCentre;
            SweepPanLimits(result.pathRadius);
        }
        var travel = kind == InspectKind.Object ? Vector3.Distance(objectCentre, eye.position + eye.rotation * Vector3.forward * objectDistance)
            : Vector3.Distance(eye.position, result.pose.position);
        var blendIn = Mathf.Clamp(FrontRoomsShotTimings.Inspect.TravelBase + FrontRoomsShotTimings.Inspect.TravelPerMetre * travel,
            FrontRoomsShotTimings.Inspect.TravelMin, FrontRoomsShotTimings.Inspect.TravelMax);
        var spec = new ShotSpec
        {
            kind = ShotKind.Inspect,
            target = PoseDelegate,
            focus = face.position,
            blendIn = blendIn,
            blendOut = FrontRoomsShotTimings.Inspect.BlendOut,
            rotBlendOut = FrontRoomsShotTimings.Inspect.RotBlendOut,
            fovDelta = fovDeltaShot,
            fov = FovDelegate,
            lockMove = true,
            lockLook = true,
            cancel = ShotCancel.Relay,
            fullPose = true,
            ease = ShotEase.Sine,
            fovMin = Mathf.Min(zoomStep.fov < baseFov - 1e-3f ? zoomStep.fov : 999f, fovDeltaShot < -FrontRoomsShotTimings.MaxFovDeltaDeg ? baseFov : 999f),
        };
        if (spec.fovMin > 900f) spec.fovMin = 0f;
        // The light the eye adapts to: the cell the face looks into (the held key: where the player stands). The world
        // is frozen for the whole hold, so it is read once.
        var probe = held || current == null ? eye.position : face.position + (face.rotation * Vector3.forward) * FrontRoomsInspectLook.LightProbeAhead;
        lightW = FrontRoomsInspectLook.CellLightAt(map, probe);
        shot = rig.BeginShot(spec);
        active = true;
        QueueHints();
        EnsureRunner();
        FrontRoomsInspectLook.Begin(runner != null ? runner.transform : null, kind);
        Opened?.Invoke(current);
        return shot;
    }

    static void SweepPanLimits(float radius)
    {
        var zoomPose = ZoomPose(Vector2.zero);
        var p = zoomPose.position;
        float Clear(Vector3 dir, float wanted) => wanted <= 0f ? 0f : Mathf.Max(0f, SweepWorld(p, p + dir * wanted, radius) - .01f);
        panMax.x = Mathf.Min(panMax.x, Clear(faceRight, panMax.x));
        panMin.x = -Mathf.Min(-panMin.x, Clear(-faceRight, -panMin.x));
        panMax.y = Mathf.Min(panMax.y, Clear(faceUp, panMax.y));
        panMin.y = -Mathf.Min(-panMin.y, Clear(-faceUp, -panMin.y));
    }

    static Vector3 RendererCentre(Transform root, Vector3 fallback)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return fallback;
        var b = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b.center;
    }

    static void BindDisplay(Transform objectDisplay, Pose eye, Vector3 objectCentre)
    {
        display = objectDisplay;
        if (display == null) return;
        displayParent = display.parent;
        displayLocalPosition = display.localPosition;
        displayLocalRotation = display.localRotation;
        displayLocalScale = display.localScale;
        displayStart = new Pose(display.position, display.rotation);
        var centre = RendererCentre(display, objectCentre);
        // Off the wall first: half the way to the eye along the face normal, at most ObjectLift.
        var normal = face.rotation * Vector3.forward;
        displayLift = normal * Mathf.Min(ObjectLift, .5f * Mathf.Max(0f, Vector3.Dot(eye.position - centre, normal)));
        centreOffsetLocal = Quaternion.Inverse(display.rotation) * (centre - display.position);
        // Present the face to the camera, text-up = camera up; keep the object's own roll about the view axis otherwise.
        var faceNormalWorld = face.rotation * Vector3.forward;
        var faceUpWorld = face.rotation * Vector3.up;
        var toFace = Quaternion.Inverse(Quaternion.LookRotation(faceNormalWorld, faceUpWorld)) * display.rotation;   // display in face frame
        var camForward = eye.rotation * Vector3.forward;
        var camUp = eye.rotation * Vector3.up;
        presentRotationLocal = Quaternion.Inverse(eye.rotation) * (Quaternion.LookRotation(-camForward, camUp) * toFace);
    }

    /// <summary>Phase.Inspect only, before rig.Tick: look (degrees), zoom (steps; continuous for pinch), toggle.</summary>
    public static void Tick(float dt, Vector2 lookDegrees, float zoomSteps, bool zoomToggle)
    {
        if (!active || closing) return;
        age += dt;
        TickReframe(dt);
        if (zoomStep.offered)
        {
            if (zoomToggle) zoomTarget = zoomTarget > .5f ? 0f : 1f;
            else if (zoomSteps != 0f) zoomTarget = Mathf.Clamp01(zoomTarget + zoomSteps);
        }
        switch (kind)
        {
            case InspectKind.Flat:
                if (zoomTarget > .5f)
                {
                    var k = Mathf.Max(zoomDistance, .05f) * Mathf.Deg2Rad;
                    panTarget = new Vector2(Mathf.Clamp(panTarget.x + lookDegrees.x * k, panMin.x, panMax.x), Mathf.Clamp(panTarget.y + lookDegrees.y * k, panMin.y, panMax.y));
                    lookTarget = Vector2.zero;
                }
                else
                {
                    lookTarget = new Vector2(Mathf.Clamp(lookTarget.x + lookDegrees.x, solved.lookMin.x, solved.lookMax.x),
                                             Mathf.Clamp(lookTarget.y + lookDegrees.y, solved.lookMin.y, solved.lookMax.y));
                    panTarget = Vector2.zero;
                }
                break;
            case InspectKind.Panel:
                var orbit = def.Orbit;
                lookTarget = new Vector2(Mathf.Clamp(lookTarget.x + lookDegrees.x, -orbit, orbit), Mathf.Clamp(lookTarget.y + lookDegrees.y, -10f, 10f));
                break;
            case InspectKind.Object:
                turnTarget = new Vector2(turnTarget.x + lookDegrees.x, Mathf.Clamp(turnTarget.y - lookDegrees.y, -80f, 80f));
                break;
        }
        var zoomTime = zoomStep.fov < baseFov - 1e-3f ? Mathf.Max(FrontRoomsShotTimings.Inspect.ZoomFovSeconds, FrontRoomsSettings.ReduceFlashing ? .6f : 0f) : FrontRoomsShotTimings.Inspect.ZoomSeconds;
        zoom = Damp(zoom, zoomTarget, ref zoomVel, zoomTime, dt, 1e-5f);
        look = Damp(look, lookTarget, ref lookVel, kind == InspectKind.Panel ? FrontRoomsShotTimings.Inspect.PanSeconds : FrontRoomsShotTimings.Inspect.BaseLookSeconds, dt, 1e-3f);
        pan = Damp(pan, panTarget, ref panVel, FrontRoomsShotTimings.Inspect.PanSeconds, dt, 1e-5f);
        turn = Damp(turn, turnTarget, ref turnVel, FrontRoomsShotTimings.Inspect.TurnSeconds, dt, 1e-3f);
    }

    /// <summary>Tests and the harness: jump the zoom target (the game's v1 passes no zoom input until T3).</summary>
    public static void SetZoomTarget(bool zoomed) { if (active && !closing && zoomStep.offered) zoomTarget = zoomed ? 1f : 0f; }

    static float Damp(float x, float target, ref float v, float time, float dt, float eps)
    {
        if (dt <= 0f) return x;
        x = Mathf.SmoothDamp(x, target, ref v, Mathf.Max(1e-3f, time * .5f), float.PositiveInfinity, dt);
        if (Mathf.Abs(x - target) < eps) { x = target; v = 0f; }
        return x;
    }

    static Vector2 Damp(Vector2 x, Vector2 target, ref Vector2 v, float time, float dt, float eps)
    {
        var vx = v.x; var vy = v.y;
        x.x = Damp(x.x, target.x, ref vx, time, dt, eps);
        x.y = Damp(x.y, target.y, ref vy, time, dt, eps);
        v = new Vector2(vx, vy);
        return x;
    }

    static Pose ZoomPose(Vector2 panOffset)
    {
        var c = face.position + faceRight * (zoomCentre.x + panOffset.x) + faceUp * (zoomCentre.y + panOffset.y);
        return new Pose(c + viewDir * zoomDistance, solved.pose.rotation);
    }

    /// <summary>The shot's target this frame: the solved pose, zoom, pan and look (flat); orbit (panel); the eye (object).</summary>
    static Pose CurrentPose()
    {
        if (kind == InspectKind.Object)
            return shotRig != null ? shotRig.BaseEye : solved.pose;
        var bp = BasePose();
        if (kind == InspectKind.Panel)
        {
            var orbit = Quaternion.AngleAxis(look.x, Vector3.up) * Quaternion.AngleAxis(-look.y, bp.rotation * Vector3.right);
            var basePos = Vector3.Lerp(bp.position, ZoomPose(Vector2.zero).position, zoom);
            return new Pose(face.position + orbit * (basePos - face.position), orbit * bp.rotation);
        }
        var zp = ZoomPose(pan);
        var position = Vector3.Lerp(bp.position, zp.position, zoom);
        var rotation = bp.rotation * Quaternion.Euler(-look.y, look.x, 0f);
        return new Pose(position, rotation);
    }

    // ---------------------------------------------------------------- transcript reframe (T7 on phones, G9)
    /// <summary>
    /// Turns the transcript panel on or off: the one entry point (the ShowTranscript setter calls it; the phone's TEXT chip and
    /// the desktop setting both come here). Outside a close-up, or while one returns, it only sets the flag: the next CanOpen /
    /// Open solves for it. During a flat or panel close-up it solves the pose again for the new FreeScreenRect (with the
    /// panel: the subject left of it; without: back to the full safe area, the same pose the open had) and eases the camera
    /// there on the open's own curve (sine in-out) and time (0.30 s + 0.12 s a metre, 0.40–0.55 s; × 1.5 at reduced Camera
    /// motion, a cut at Off). A toggle while one eases blends from where the camera is and keeps its speed: the running
    /// ease goes on under the new one. Look, zoom and pan keep working throughout. When no pose fits the new rect the camera
    /// stays (ReframeWhy says why). The object kind and the held key do not move. Never changes how long ShowTranscript lasts.
    /// </summary>
    public static void SetTranscript(bool on)
    {
        if (showTranscript == on) return;
        showTranscript = on;
        if (!active || closing || held || current == null || kind == InspectKind.Object || shotRig == null) return;
        var result = SolveFor(current, shotRig);
        if (!result.ok) { reframeWhy = result.why ?? "no pose"; return; }
        reframeWhy = "";
        var from = BasePose();
        if (reframing)
        {
            // The running ease keeps going under the new one (its own start frozen where it is now): no speed step.
            prevLeg = new ReframeLeg { from = prevLegLive ? LegPose(prevLeg) : leg.from, to = solved.pose, k = leg.k, seconds = leg.seconds };
            prevLegLive = true;
        }
        else prevLegLive = false;
        var travel = Vector3.Distance(from.position, result.pose.position);
        var seconds = Mathf.Clamp(FrontRoomsShotTimings.Inspect.TravelBase + FrontRoomsShotTimings.Inspect.TravelPerMetre * travel,
            FrontRoomsShotTimings.Inspect.TravelMin, FrontRoomsShotTimings.Inspect.TravelMax);
        var motion = FrontRoomsSettings.CameraMotionScale;   // as the rig times a full-pose shot (C17)
        seconds = motion <= 0f ? 0f : motion < 1f ? seconds * FrontRoomsShotTimings.Inspect.HalfMotionTimeScale : seconds;
        leg = new ReframeLeg { from = from, to = result.pose, k = 0f, seconds = seconds };
        solved = result;   // the base pose and look limits; the zoom view (viewDir, zoomCentre, pan limits) stays the open's
        reframing = seconds > 0f;
        if (!reframing) prevLegLive = false;
    }

    /// <summary>The base pose of a flat or panel close-up: the solved pose, or on its way there after a transcript toggle.</summary>
    static Pose BasePose()
    {
        if (!reframing) return solved.pose;
        var from = prevLegLive ? LegPose(prevLeg) : leg.from;
        return BlendPose(from, solved.pose, FrontRoomsCameraRig.SineIn(leg.k));
    }

    static Pose LegPose(in ReframeLeg l) => BlendPose(l.from, l.to, FrontRoomsCameraRig.SineIn(l.k));

    static Pose BlendPose(Pose a, Pose b, float w) => new Pose(Vector3.Lerp(a.position, b.position, w), Quaternion.Slerp(a.rotation, b.rotation, w));

    /// <summary>Advances the reframe on the inspect clock (Tick, before rig.Tick). Holds while the close-up returns.</summary>
    static void TickReframe(float dt)
    {
        if (!reframing) return;
        if (prevLegLive) prevLeg.k = prevLeg.seconds > 0f ? Mathf.Min(1f, prevLeg.k + dt / prevLeg.seconds) : 1f;
        leg.k = leg.seconds > 0f ? Mathf.Min(1f, leg.k + dt / leg.seconds) : 1f;
        if (leg.k >= 1f) reframing = prevLegLive = false;
    }

    /// <summary>The authored FOV the rig draws as <paramref name="drawn"/> on this view: FrontRoomsHandheld.AspectFov undone
    /// (identity on desktop and on views 16:9 or wider).</summary>
    static float AuthoredFov(float drawn, float aspect)
    {
        if (!FrontRoomsHandheld.Active || aspect <= 0f || aspect >= FrontRoomsHandheld.FovReferenceAspect || drawn <= 0f || drawn >= 179f) return drawn;
        return 2f * Mathf.Atan(Mathf.Tan(drawn * .5f * Mathf.Deg2Rad) * aspect / FrontRoomsHandheld.FovReferenceAspect) * Mathf.Rad2Deg;
    }

    static float CurrentFovDelta() => def == null ? 0f : fovDeltaShot + zoom * (zoomStep.fov - baseFov);

    /// <summary>The flat region nearest a face point (face right / up metres), in face coordinates. False when none has a rect.</summary>
    static bool RegionNearest(Vector2 facePoint, Vector2 faceSize, out Vector2 centre)
    {
        centre = Vector2.zero;
        if (def == null || def.RegionCount == 0) return false;
        // Regions are written in viewer axes (x to the viewer's right); the face's right axis points to the viewer's left
        // for a face that looks at the viewer, so x flips by the sign between the two.
        var sx = Vector3.Dot(faceRight, solved.pose.rotation * Vector3.right) >= 0f ? 1f : -1f;
        var best = float.PositiveInfinity;
        var found = false;
        for (var i = 0; i < def.RegionCount; i++)
        {
            if (!def.TryRegion(i, faceSize, out var c, out _)) continue;
            c.x *= sx;
            var d = (c - facePoint).sqrMagnitude;
            if (d >= best) continue;
            best = d;
            centre = c;
            found = true;
        }
        return found;
    }

    /// <summary>The object kind's FOV delta: the presented render bounds fill ObjectFill of the frame at objectDistance (§3.3 E2).</summary>
    static float ObjectFovDelta(float baseFovDeg, float aspect)
    {
        var widest = baseFovDeg + def.FovDelta;
        if (display == null || objectDistance <= 0f) return def.FovDelta;
        var extent = PresentedExtent();
        if (extent.x <= 0f || extent.y <= 0f) return def.FovDelta;
        var visibleH = Mathf.Max(extent.y, extent.x / Mathf.Max(aspect, .1f)) / ObjectFill;
        var capH = ReferenceHeightPx / (FrontRoomsInspectFraming.MaxTexelMagnification * def.TexelsPerMetre);
        visibleH = Mathf.Max(visibleH, capH);
        // Solved as drawn (a handheld squarer than 16:9 draws a wider FOV than authored), handed to the rig authored.
        var fov = Mathf.Clamp(2f * Mathf.Atan(visibleH * .5f / objectDistance) * Mathf.Rad2Deg, FrontRoomsInspectFraming.MinFov, FrontRoomsHandheld.AspectFov(widest, aspect));
        return AuthoredFov(fov, aspect) - baseFovDeg;
    }

    /// <summary>The display's renderer bounds as presented to the camera: (width, height) in metres, camera axes.</summary>
    static Vector2 PresentedExtent()
    {
        var renderers = display.GetComponentsInChildren<Renderer>();
        var inv = Quaternion.Inverse(display.rotation);
        var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        foreach (var r in renderers)
        {
            if (!r.enabled || !(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;
            var b = r.localBounds;
            var t = r.transform;
            for (var i = 0; i < 8; i++)
            {
                var c = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                var v = presentRotationLocal * (inv * (t.TransformPoint(c) - display.position));
                min = Vector2.Min(min, v);
                max = Vector2.Max(max, v);
            }
        }
        return float.IsInfinity(min.x) ? Vector2.zero : max - min;
    }


    /// <summary>Starts the return (rig.EndShot). The adapt volume decays on its own tau.</summary>
    public static void Close(FrontRoomsCameraRig rig)
    {
        if (!active || closing) return;
        closing = true;
        FrontRoomsInspectLook.Release();
        (rig ?? shotRig)?.EndShot(shot);
        Closed?.Invoke(current);
    }

    /// <summary>The shot has ended: the object is home, the state clears.</summary>
    static void Finish()
    {
        RestoreDisplay();
        var wasHeld = held;
        active = closing = held = false;
        reframing = prevLegLive = false;
        current = null;
        shot = null;
        def = null;
        if (wasHeld) HeldReturned?.Invoke();
    }

    static void RestoreDisplay()
    {
        if (display == null) return;
        if (display.parent != displayParent) display.SetParent(displayParent, false);
        display.localPosition = displayLocalPosition;
        display.localRotation = displayLocalRotation;
        display.localScale = displayLocalScale;
        display = null;
    }

    /// <summary>Teardown now (restart, OnDestroy, caught): drops state, volumes to 0, statics reset.</summary>
    public static void Abort()
    {
        RestoreDisplay();
        active = closing = held = false;
        current = lastShown = null;
        shot = null;
        def = null;
        shotRig = null;
        age = 0f;
        hintText = "";
        hintAlpha = 0f;
        hintQueueLength = 0;
        DevTargets = false;
        showTranscript = false;   // the full reset: no reframe (C24 lifetime)
        reframing = prevLegLive = false;
        reframeWhy = "";
        cache.Clear();
        FrontRoomsInspectLook.Abort();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Abort();
        targets.Clear();
        map = null;
        for (var i = 0; i < hintCounts.Length; i++) hintCounts[i] = 0;
        FrontRooms3DGame.MapRunStarted -= OnMapRunStarted;
        FrontRooms3DGame.MapRunStarted += OnMapRunStarted;
        FrontRooms3DGame.MapRunEnded -= OnMapRunEnded;
        FrontRooms3DGame.MapRunEnded += OnMapRunEnded;
    }

    static void OnMapRunStarted(FrontRoomsMapWorld world, FrontRoomsMapHunter relay)
    {
        map = world;
        EnsureRunner();
        FrontRoomsInspectLook.Prewarm(runner != null ? runner.transform : null);   // desktop: one zero-CoC Bokeh frame (§3.7)
    }

    static void OnMapRunEnded() => map = null;

    // ---------------------------------------------------------------- hints (§5.1)
    static void QueueHints()
    {
        hintQueueLength = hintQueueIndex = 0;
        hintClock = 0f;
        hintText = "";
        hintAlpha = 0f;
        if (hintCounts[0] < FrontRoomsInspectCatalog.HintShowings) hintQueue[hintQueueLength++] = 0;
        if (ZoomInputWired && zoomStep.offered && kind != InspectKind.Object && hintCounts[1] < FrontRoomsInspectCatalog.HintShowings) hintQueue[hintQueueLength++] = 1;
        if (kind == InspectKind.Object && hintCounts[2] < FrontRoomsInspectCatalog.HintShowings) hintQueue[hintQueueLength++] = 2;
        if (hintQueueLength > 0) hintCounts[hintQueue[0]]++;
    }

    static string HintString(int id)
    {
        var touch = FrontRoomsHandheld.Active;
        switch (id)
        {
            case 0: return touch ? "" : FrontRoomsInspectCatalog.HintBack;   // touch: the BACK glyph on the button
            case 1: return touch ? FrontRoomsInspectCatalog.HintCloserTouch : FrontRoomsInspectCatalog.HintCloser;
            default: return touch ? FrontRoomsInspectCatalog.HintTurnTouch : FrontRoomsInspectCatalog.HintTurn;
        }
    }

    static void TickHint(float dt)
    {
        if (hintQueueIndex >= hintQueueLength) { hintText = ""; hintAlpha = 0f; return; }
        var fade = FrontRoomsSettings.ReduceFlashing ? FrontRoomsInspectCatalog.HintFadeCalm : FrontRoomsInspectCatalog.HintFadeIn;
        var length = fade + FrontRoomsInspectCatalog.HintHold + fade;
        hintClock += dt;
        hintText = HintString(hintQueue[hintQueueIndex]);
        hintAlpha = hintClock < fade ? hintClock / fade : hintClock < fade + FrontRoomsInspectCatalog.HintHold ? 1f : Mathf.Clamp01((length - hintClock) / fade);
        if (hintClock >= length)
        {
            hintClock = 0f;
            hintQueueIndex++;
            if (hintQueueIndex < hintQueueLength) hintCounts[hintQueue[hintQueueIndex]]++;
        }
    }

    // ---------------------------------------------------------------- runner
    static void EnsureRunner()
    {
        if (runner != null || !Application.isPlaying) return;
        // Like FrontRoomsZoneReflectionDriver: hidden, kept across R restarts (it holds no state of its own).
        var go = new GameObject("FrontRoomsInspect runner") { hideFlags = HideFlags.HideAndDontSave };
        UnityEngine.Object.DontDestroyOnLoad(go);
        runner = go.AddComponent<Runner>();
    }

    static void LateTick()
    {
        var dt = Dt;
        var weight = 0f;
        if (active)
        {
            if (shot == null || shot.Ended) Finish();
            else
            {
                weight = shot.EasedWeight;
                if (!closing) TickHint(dt);
                PlaceDisplay();
            }
        }
        FrontRoomsInspectLook.Tick(dt, weight, lightW, active ? ObjectDistance : 1f);
    }

    static void PlaceDisplay()
    {
        if (display == null || shotRig == null) return;
        var eye = shotRig.BaseEye;
        var dist = ObjectDistance;
        var centre = eye.position + eye.rotation * Vector3.forward * dist;
        var turnRotation = Quaternion.AngleAxis(turn.x, eye.rotation * Vector3.up) * Quaternion.AngleAxis(turn.y, eye.rotation * Vector3.right);
        var presentRotation = turnRotation * (eye.rotation * presentRotationLocal);
        var presentPosition = centre - presentRotation * centreOffsetLocal;
        var w = shot.EasedWeight;
        var r = shot.RotationWeight;
        display.SetPositionAndRotation(ObjectPath(displayStart.position, displayLift, presentPosition, w), Quaternion.Slerp(displayStart.rotation, presentRotation, r));
    }

    /// <summary>
    /// The object kind's travel: a quadratic Bézier from its mount, lifted off along the face normal (lift = normal × metres),
    /// to the presentation point, so it leaves the wall before it turns toward the eye (never grazing the wall it hangs on).
    /// </summary>
    public static Vector3 ObjectPath(Vector3 start, Vector3 lift, Vector3 end, float w)
    {
        w = Mathf.Clamp01(w);
        var u = 1f - w;
        return u * u * start + 2f * u * w * (start + lift) + w * w * end;
    }

    [AddComponentMenu("")]
    sealed class Runner : MonoBehaviour
    {
        void LateUpdate() => LateTick();

        void OnDestroy()
        {
            if (runner == this) runner = null;
        }
    }
}
