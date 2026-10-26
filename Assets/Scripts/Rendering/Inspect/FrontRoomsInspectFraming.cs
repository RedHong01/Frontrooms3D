using System;
using UnityEngine;

/// <summary>
/// EXAMINE framing (research/inspect/10_design.md §3.2, §3.4): where the camera goes for a flat or
/// panel close-up, and the one angular zoom step. Pure geometry: no Unity state is read, so the
/// EditMode tests run it with any aspect, FOV and eye. The world is asked only through the two
/// optional delegates in <see cref="Input"/> (null = an empty world), which the facade
/// (FrontRoomsInspect) fills with a spherecast and a CheckSphere.
///
/// Composition A puts the framed rect inside a viewport rect (the placard: Red's frame, the plan in
/// the left 63 %, the real wallpaper on the right): the camera looks square at the face and is
/// shifted so the rect lands there. Composition B centres the rect with a margin. A is used while
/// it fits within distMaxA; else B. Every fit projects the 4 corners of the rect through the
/// candidate camera, so any aspect, FOV or yaw offset works.
/// </summary>
public static class FrontRoomsInspectFraming
{
    public const float ReadArcmin = 18.6f, MaxTexelMagnification = 2f, MinFov = 30f;
    public const float DesktopArcmin = 1678f, PhoneArcmin = 762f, TabletArcmin = 1343f;
    /// <summary>The pose keeps this far from any collider face (§3.2 step 5).</summary>
    public const float MinSurface = .15f;
    /// <summary>The blocked-path fallback turn (§3.2 step 3).</summary>
    public const float FallbackTurnDeg = 20f;
    /// <summary>The zoom step is offered only if it is at least this much closer than the base framing (§3.4 step 4).</summary>
    public const float ZoomMinGain = .10f;
    /// <summary>Faces that look up or down more than this (|N·up|) frame with the face's own up (§3.2 step 4).</summary>
    public const float UpFaceDot = .7f;

    public struct Input
    {
        /// <summary>Face centre; rotation forward = the outward normal N, up = the text-up U.</summary>
        public Pose face;
        /// <summary>The framed rect in metres (width along the face's right, height along U).</summary>
        public Vector2 size;
        /// <summary>
        /// The framed object's depth about the face plane, metres along N: front ≥ 0 (toward the viewer, e.g. the placard
        /// frame's crown 8.95 mm proud of the sheet), back ≤ 0 (its wall side). Both 0 = a flat rect. The fit and the look
        /// limits test the 8 corners of this box, so a frame's side wall never leaves the screen (v1 tested 4).
        /// </summary>
        public float depthFront, depthBack;
        /// <summary>Composition B's margin per side, as a fraction of the size.</summary>
        public float margin;
        /// <summary>Composition A's viewport rect; zero width = no composition A.</summary>
        public Rect screen;
        /// <summary>Viewport rect of the safe area (phones); zero = the full screen.</summary>
        public Rect safeArea;
        /// <summary>Composition B's viewport rect (the free rect with a transcript; it cuts A's rect too); zero = the full screen.</summary>
        public Rect free;
        /// <summary>Vertical field of view of the shot (degrees: base + fovDelta) and the aspect (w / h).</summary>
        public float fov, aspect, nearPlane;
        /// <summary>The largest FOV during the travel (the player's base FOV) for the near-plane corner radius; 0 = fov.</summary>
        public float pathFov;
        /// <summary>The player's base eye (BaseEye).</summary>
        public Vector3 eye;
        public float minDist, maxDist, maxAngle, maxYawOff, distMaxA, minY, maxY;
        /// <summary>[base, zoom] metres; 0 = solve.</summary>
        public float[] dist;
        /// <summary>World: the clear distance a sphere of radius r travels from a to b (null = always clear).</summary>
        public Func<Vector3, Vector3, float, float> sweep;
        /// <summary>World: true when a sphere at p of radius r overlaps a collider (null = never).</summary>
        public Func<Vector3, float, bool> overlap;
    }

    public struct Result
    {
        public Pose pose;
        public float distance, angle;
        public bool ok, compositionB;
        /// <summary>Why it failed ("" when ok).</summary>
        public string why;
        /// <summary>Pan limits of the view centre in the face plane (metres, face right / up), pre-swept at the zoom distance.</summary>
        public Vector2 panMin, panMax;
        /// <summary>Look limits at base (degrees yaw / pitch).</summary>
        public Vector2 lookMin, lookMax;
        /// <summary>The view direction from the face (unit, out of the face toward the camera).</summary>
        public Vector3 direction;
        /// <summary>Where the camera's centre ray meets the face plane at base, in face (right, up) metres.</summary>
        public Vector2 centreOnFace;
        /// <summary>The path sphere radius used (near-plane corner radius + 1 cm).</summary>
        public float pathRadius;
    }

    public struct Zoom
    {
        public float distance, fov;
        public bool offered;
        /// <summary>Visible height of the face at the zoom step (metres).</summary>
        public float visibleHeight;
    }

    /// <summary>The near-plane corner radius: 0.113 m at 76° and 16:9, 0.133 m at 21:9 (§3.2).</summary>
    public static float NearCornerRadius(float nearPlane, float fovDeg, float aspect)
    {
        var hh = nearPlane * Mathf.Tan(fovDeg * .5f * Mathf.Deg2Rad);
        var hw = hh * aspect;
        return Mathf.Sqrt(hh * hh + hw * hw + nearPlane * nearPlane);
    }

    public static Vector3 FaceRight(Pose face) => face.rotation * Vector3.right;
    public static Vector3 FaceUp(Pose face) => face.rotation * Vector3.up;
    public static Vector3 FaceNormal(Pose face) => face.rotation * Vector3.forward;

    /// <summary>Viewport point of a world point for a camera pose (z = depth along the view axis).</summary>
    public static Vector3 Project(Pose camera, float fovDeg, float aspect, Vector3 world)
    {
        var local = Quaternion.Inverse(camera.rotation) * (world - camera.position);
        if (local.z <= 1e-5f) return new Vector3(float.NaN, float.NaN, local.z);
        var t = Mathf.Tan(fovDeg * .5f * Mathf.Deg2Rad);
        return new Vector3(.5f + .5f * local.x / (local.z * aspect * t), .5f + .5f * local.y / (local.z * t), local.z);
    }

    static Rect Full => new Rect(0f, 0f, 1f, 1f);
    static Rect OrFull(Rect r) => r.width > 0f && r.height > 0f ? r : Full;

    static Rect Intersect(Rect a, Rect b) =>
        Rect.MinMaxRect(Mathf.Max(a.xMin, b.xMin), Mathf.Max(a.yMin, b.yMin), Mathf.Min(a.xMax, b.xMax), Mathf.Min(a.yMax, b.yMax));

    /// <summary>The camera rotation that looks along -D, with world up (or the face's up for faces that look up or down).</summary>
    static Quaternion LookFor(Vector3 d, Vector3 faceUp)
    {
        var forward = -d;
        var up = Mathf.Abs(Vector3.Dot(d, Vector3.up)) > UpFaceDot ? faceUp : Vector3.up;
        if (Vector3.Cross(forward, up).sqrMagnitude < 1e-8f) up = faceUp;
        return Quaternion.LookRotation(forward, up);
    }

    /// <summary>The pose at distance <paramref name="d"/> along <paramref name="dir"/> that puts the face centre at the rect centre.</summary>
    static Pose PoseFor(Pose face, Vector3 dir, float d, Rect rect, float fov, float aspect)
    {
        var rotation = LookFor(dir, FaceUp(face));
        var t = Mathf.Tan(fov * .5f * Mathf.Deg2Rad);
        var lx = (rect.center.x - .5f) * 2f * d * aspect * t;
        var ly = (rect.center.y - .5f) * 2f * d * t;
        var position = face.position + dir * d - rotation * new Vector3(lx, ly, 0f);
        return new Pose(position, rotation);
    }

    /// <summary>All 8 corners of the framed box (the rect at the face plane, pushed to depthFront and depthBack along N) inside <paramref name="rect"/>.</summary>
    public static bool Fits(Pose face, Vector2 framed, Pose camera, float fov, float aspect, Rect rect, float depthFront = 0f, float depthBack = 0f)
    {
        var right = FaceRight(face) * (framed.x * .5f);
        var up = FaceUp(face) * (framed.y * .5f);
        var n = FaceNormal(face);
        var layers = depthFront != 0f || depthBack != 0f ? 2 : 1;
        for (var layer = 0; layer < layers; layer++)
        {
            var offset = layers == 1 ? Vector3.zero : n * (layer == 0 ? depthFront : depthBack);
            for (var i = 0; i < 4; i++)
            {
                var corner = face.position + offset + ((i & 1) == 0 ? right : -right) + ((i & 2) == 0 ? up : -up);
                var v = Project(camera, fov, aspect, corner);
                if (float.IsNaN(v.x) || v.x < rect.xMin - 1e-5f || v.x > rect.xMax + 1e-5f || v.y < rect.yMin - 1e-5f || v.y > rect.yMax + 1e-5f) return false;
            }
        }
        return true;
    }

    /// <summary>The smallest distance along <paramref name="dir"/> at which the framed box fits <paramref name="rect"/> (binary search; fits only grow with d).</summary>
    public static float FitDistance(Pose face, Vector3 dir, Vector2 framed, Rect rect, float fov, float aspect, float depthFront = 0f, float depthBack = 0f)
    {
        float lo = .02f, hi = 8f;
        if (!Fits(face, framed, PoseFor(face, dir, hi, rect, fov, aspect), fov, aspect, rect, depthFront, depthBack)) return float.PositiveInfinity;
        for (var i = 0; i < 40; i++)
        {
            var mid = (lo + hi) * .5f;
            if (Fits(face, framed, PoseFor(face, dir, mid, rect, fov, aspect), fov, aspect, rect, depthFront, depthBack)) hi = mid; else lo = mid;
        }
        return hi;
    }

    /// <summary>
    /// The ±BaseLookDeg look limits at base, shrunk so all 8 corners of the framed box stay inside the safe viewport at
    /// every limit, including the 4 diagonal extremes (the frame never leaves the screen while the player looks about).
    /// Composition A puts the frame 1 % from the left edge, so the look toward that side is about 1°, the other ±3°.
    /// </summary>
    public static void LookLimits(in Input input, Pose pose, Vector2 framed, out Vector2 lookMin, out Vector2 lookMax)
    {
        var limit = FrontRoomsShotTimings.Inspect.BaseLookDeg;
        var safe = OrFull(input.safeArea);
        var face = input.face;
        float fov = input.fov, aspect = input.aspect, front = input.depthFront, back = input.depthBack;
        bool On(float yaw, float pitch) => Fits(face, framed, new Pose(pose.position, pose.rotation * Quaternion.Euler(-pitch, yaw, 0f)), fov, aspect, safe, front, back);
        float Search(float sx, float sy)
        {
            if (!On(0f, 0f)) return 0f;
            if (On(sx * limit, sy * limit)) return limit;
            float lo = 0f, hi = limit;
            for (var i = 0; i < 24; i++)
            {
                var mid = (lo + hi) * .5f;
                if (On(sx * mid, sy * mid)) lo = mid; else hi = mid;
            }
            return lo;
        }
        lookMax = new Vector2(Search(1f, 0f), Search(0f, 1f));
        lookMin = new Vector2(-Search(-1f, 0f), -Search(0f, -1f));
        // The diagonal extremes: shrink both axes together until every corner is on screen.
        for (var k = 0; k < 24; k++)
        {
            if (On(lookMax.x, lookMax.y) && On(lookMax.x, lookMin.y) && On(lookMin.x, lookMax.y) && On(lookMin.x, lookMin.y)) break;
            lookMin *= .9f;
            lookMax *= .9f;
        }
    }

    /// <summary>Step 1: N turned toward the eye about world up by at most maxYawOff (+ an extra turn for the blocked-path fallback).</summary>
    static Vector3 BaseDirection(in Input input, float extraYaw)
    {
        var n = FaceNormal(input.face);
        var flatN = new Vector3(n.x, 0f, n.z);
        if (flatN.sqrMagnitude < 1e-6f) return n;   // a face that looks straight up or down: no yaw
        var toEye = input.eye - input.face.position;
        var flatEye = new Vector3(toEye.x, 0f, toEye.z);
        var yaw = flatEye.sqrMagnitude > 1e-8f ? Vector3.SignedAngle(flatN, flatEye, Vector3.up) : 0f;
        var turn = Mathf.Sign(yaw) * Mathf.Min(Mathf.Abs(yaw), input.maxYawOff) + extraYaw;
        return Quaternion.AngleAxis(turn, Vector3.up) * n;
    }

    /// <summary>Tilt <paramref name="dir"/> so the camera height stays in the body band (max maxAngle off the starting pitch).</summary>
    static Vector3 Tilt(in Input input, Vector3 dir, float d, Rect rect)
    {
        var pose = PoseFor(input.face, dir, d, rect, input.fov, input.aspect);
        var y = pose.position.y;
        if (y >= input.minY - 1e-4f && y <= input.maxY + 1e-4f) return dir;
        var target = Mathf.Clamp(y, input.minY, input.maxY);
        var flat = new Vector3(dir.x, 0f, dir.z);
        if (flat.sqrMagnitude < 1e-6f) return dir;
        flat.Normalize();
        var pitch0 = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
        // The rect shift's own height does not depend on the tilt for a vertical face: solve with it held.
        var shiftY = y - (input.face.position.y + dir.y * d);
        var s = Mathf.Clamp((target - shiftY - input.face.position.y) / Mathf.Max(d, 1e-3f), -1f, 1f);
        var pitch = Mathf.Asin(s) * Mathf.Rad2Deg;
        pitch = Mathf.Clamp(pitch, pitch0 - input.maxAngle, pitch0 + input.maxAngle);
        return (flat * Mathf.Cos(pitch * Mathf.Deg2Rad) + Vector3.up * Mathf.Sin(pitch * Mathf.Deg2Rad)).normalized;
    }

    /// <summary>The §3.2 solver: composition, direction, distance, tilt, rotation, path check, pan and look limits.</summary>
    public static Result Solve(in Input input)
    {
        var safe = OrFull(input.safeArea);
        var aRect = input.screen.width > 0f && input.screen.height > 0f ? Intersect(input.screen, safe) : default;
        var bRect = Intersect(OrFull(input.free), safe);
        // A free rect that cuts into A's own rect (a transcript panel wider than A leaves room for: on phones it starts at 64 %
        // of the safe area, left of A's 64 % of the screen) cuts A too. Without a panel the free rect is the safe area: no cut.
        if (aRect.width > 0f && (bRect.xMin > aRect.xMin || bRect.xMax < aRect.xMax || bRect.yMin > aRect.yMin || bRect.yMax < aRect.yMax))
            aRect = Intersect(aRect, bRect);
        var framedB = input.size * (1f + 2f * Mathf.Max(0f, input.margin));
        var baseDist = input.dist != null && input.dist.Length > 0 ? input.dist[0] : 0f;
        var pathRadius = NearCornerRadius(input.nearPlane > 0f ? input.nearPlane : .06f, Mathf.Max(input.fov, input.pathFov), input.aspect) + .01f;
        var why = "";

        // Candidate order: A then B; at each, the direction toward the eye, then ±20° (§3.2 step 3).
        for (var comp = 0; comp < 2; comp++)
        {
            var useA = comp == 0;
            if (useA && aRect.width <= 0f) continue;
            for (var k = 0; k < 3; k++)
            {
                var extra = k == 0 ? 0f : k == 1 ? FallbackTurnDeg : -FallbackTurnDeg;
                var rect = useA ? aRect : bRect;
                var framed = useA ? input.size : framedB;
                var dir = BaseDirection(input, extra);
                var d = 0f;
                for (var iter = 0; iter < 3; iter++)
                {
                    var fit = FitDistance(input.face, dir, framed, rect, input.fov, input.aspect, input.depthFront, input.depthBack);
                    d = useA ? Mathf.Max(baseDist, fit) : Mathf.Max(input.minDist, fit);
                    if (!useA && baseDist > 0f && aRect.width <= 0f) d = Mathf.Max(d, baseDist);
                    var tilted = Tilt(input, dir, d, rect);
                    if ((tilted - dir).sqrMagnitude < 1e-10f) break;
                    dir = tilted;
                }
                if (float.IsInfinity(d)) { why = "no fit"; continue; }
                if (useA && d > input.distMaxA + 1e-4f) { why = "A beyond distMaxA"; break; }   // A does not fit: composition B
                d = Mathf.Max(d, input.minDist);
                if (d > input.maxDist + 1e-4f) { why = "beyond maxDist"; continue; }
                var pose = PoseFor(input.face, dir, d, rect, input.fov, input.aspect);
                if (pose.position.y < input.minY - .005f || pose.position.y > input.maxY + .005f) { why = "outside the body band"; continue; }
                // Step 5: the path from the eye and the stand-off.
                if (input.sweep != null)
                {
                    var length = Vector3.Distance(input.eye, pose.position);
                    if (length > 1e-4f && input.sweep(input.eye, pose.position, pathRadius) < length - 1e-3f) { why = "path blocked"; continue; }
                }
                if (input.overlap != null && input.overlap(pose.position, MinSurface)) { why = "too close to a surface"; continue; }

                var result = new Result
                {
                    ok = true,
                    why = "",
                    pose = pose,
                    distance = d,
                    angle = Mathf.Asin(Mathf.Clamp((pose.rotation * Vector3.forward).y, -1f, 1f)) * Mathf.Rad2Deg,
                    compositionB = !useA,
                    direction = dir,
                    pathRadius = pathRadius,
                };
                LookLimits(input, pose, input.size, out result.lookMin, out result.lookMax);
                // Where the centre ray meets the face plane (face right / up coordinates).
                var fwd = pose.rotation * Vector3.forward;
                var n = FaceNormal(input.face);
                var denom = Vector3.Dot(fwd, n);
                if (Mathf.Abs(denom) > 1e-5f)
                {
                    var t = Vector3.Dot(input.face.position - pose.position, n) / denom;
                    var hit = pose.position + fwd * t - input.face.position;
                    result.centreOnFace = new Vector2(Vector3.Dot(hit, FaceRight(input.face)), Vector3.Dot(hit, FaceUp(input.face)));
                }
                // Pan limits: the framed rect plus the margin (the facade sweeps them against the world at the zoom distance).
                var half = input.size * .5f * (1f + Mathf.Max(0f, input.margin));
                result.panMin = -half;
                result.panMax = half;
                return result;
            }
        }
        return new Result { ok = false, why = why.Length > 0 ? why : "no pose", pathRadius = pathRadius };
    }

    /// <summary>
    /// The angular zoom step (§3.4): the visible height at which the smallest cap reads at 18.6′ on the
    /// screen class, or at the 2.0× texel magnification cap, whichever is larger. Dolly when that fits
    /// at minDist, else stay at minDist and narrow the FOV (floor 30°). Offered only ≥ 10 % closer.
    /// With no cap (no regions) the step is 0.70 × the base distance (the object kind's rule).
    /// </summary>
    public static Zoom ZoomFor(in Input input, float capMm, float screenArcmin, float viewportHeightPx, float texelsPerMetre, float baseDistance = 0f)
    {
        var v = input.fov;
        var t = Mathf.Tan(v * .5f * Mathf.Deg2Rad);
        var zoom = new Zoom { fov = v };
        if (capMm <= 0f)
        {
            zoom.distance = Mathf.Max(input.minDist, .7f * baseDistance);
            zoom.visibleHeight = 2f * zoom.distance * t;
        }
        else
        {
            var hAngle = capMm * .001f * screenArcmin / ReadArcmin;
            var hMag = texelsPerMetre > 0f ? viewportHeightPx / (MaxTexelMagnification * texelsPerMetre) : 0f;
            var h = Mathf.Max(hAngle, hMag);
            zoom.visibleHeight = h;
            if (h >= 2f * input.minDist * t) zoom.distance = h / (2f * t);
            else
            {
                zoom.distance = input.minDist;
                zoom.fov = Mathf.Max(MinFov, 2f * Mathf.Atan(h / (2f * input.minDist)) * Mathf.Rad2Deg);
            }
        }
        // Equivalent distance at the base FOV, for the 10 % rule.
        var equivalent = zoom.distance * Mathf.Tan(zoom.fov * .5f * Mathf.Deg2Rad) / t;
        zoom.offered = baseDistance <= 0f || equivalent <= baseDistance * (1f - ZoomMinGain);
        return zoom;
    }

    /// <summary>Screen class angle (arcmin) for the angular rule: desktop always uses the reference (§3.4).</summary>
    public static float ScreenArcmin(bool handheld, bool tablet) => !handheld ? DesktopArcmin : tablet ? TabletArcmin : PhoneArcmin;

    /// <summary>Pixels per millimetre on the face at distance d and vertical FOV v for a viewport height (§3.3).</summary>
    public static float PxPerMm(float d, float fovDeg, float viewportHeightPx) =>
        viewportHeightPx / (2f * d * Mathf.Tan(fovDeg * .5f * Mathf.Deg2Rad)) / 1000f;

    /// <summary>Arcminutes a cap of capMm subtends on a screen class at distance d and FOV v.</summary>
    public static float CapArcmin(float capMm, float d, float fovDeg, float screenArcmin) =>
        capMm * .001f / (2f * d * Mathf.Tan(fovDeg * .5f * Mathf.Deg2Rad)) * screenArcmin;
}
