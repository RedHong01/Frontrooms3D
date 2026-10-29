using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// The facts the handheld (iOS / Android) layer reads about the device: whether
/// the touch layer is on at all, how many pixels make one point, the safe area,
/// tablet or phone, Reduce Motion and haptics.
///
/// <see cref="Active"/> is true only in iOS / Android players. Mac, Windows and
/// WebGL players compile it to false (a WebGL page opened on a phone stays on
/// the desktop path: mobile is its own track). In the Editor it is true only
/// when a touch harness forces a device profile, or when the interactive
/// preview is switched on outside batch mode and outside an autopilot run, so
/// the map chat's autopilot, Relay and capture runs always get the desktop HUD.
/// </summary>
public static class FrontRoomsHandheld
{
    /// <summary>A device the Editor can stand in for: its screen in pixels, points per pixel and safe insets in points.</summary>
    public readonly struct Profile
    {
        public readonly string Name;
        public readonly Vector2Int Pixels;
        public readonly float PointScale;
        public readonly float InsetLeft, InsetRight, InsetTop, InsetBottom;
        public readonly bool Tablet;
        /// <summary>Active occlusion regions iOS reports for this view, in points (the outer camera and its keep-out box once
        /// iOS settles; the inner FaceTime camera while it runs).</summary>
        public readonly Rect[] Regions;
        /// <summary>UIKit's interface orientation the profile stands for (1 portrait, 2 upside down, 3 and 4 landscape); 0 for none.</summary>
        public readonly int Orientation;
        /// <summary>Corner radii iOS reports for the view (x bottom-left, y bottom-right, z top-left, w top-right), in points.</summary>
        public readonly Vector4 Corners;

        public Profile(string name, int width, int height, float pointScale, float left, float right, float top, float bottom, bool tablet,
            Rect reservedRegion = default(Rect), int orientation = 0, Vector4 corners = default(Vector4))
        {
            Name = name;
            Pixels = new Vector2Int(width, height);
            PointScale = pointScale;
            InsetLeft = left;
            InsetRight = right;
            InsetTop = top;
            InsetBottom = bottom;
            Tablet = tablet;
            Regions = reservedRegion.width > 0f && reservedRegion.height > 0f ? new[] { reservedRegion } : System.Array.Empty<Rect>();
            Orientation = orientation;
            Corners = corners;
        }

        public Profile(string name, int width, int height, float pointScale, float left, float right, float top, float bottom, bool tablet,
            Rect[] regions, int orientation, Vector4 corners)
            : this(name, width, height, pointScale, left, right, top, bottom, tablet, default(Rect), orientation, corners)
        {
            Regions = regions ?? System.Array.Empty<Rect>();
        }

        public Vector2 Points => new Vector2(Pixels.x / PointScale, Pixels.y / PointScale);
    }

    /// <summary>iPhone 16 / 17 Pro landscape: 874 × 402 pt at @3x, the Figma phone master (insets L 62, R 62, B 21).</summary>
    public static readonly Profile IPhonePro = new Profile("iPhone 16 Pro", 2622, 1206, 3f, 62f, 62f, 0f, 21f, false);
    /// <summary>A 20:9 Android phone: 914 × 411 dp at 2.625 (punch-hole side and gesture bar insets).</summary>
    public static readonly Profile Android20x9 = new Profile("Android 20:9", 2400, 1080, 2.625f, 32f, 32f, 0f, 20f, false);
    /// <summary>iPad Air 11" landscape: 1180 × 820 pt at @2x (home indicator only).</summary>
    public static readonly Profile IPad11 = new Profile("iPad 11", 2360, 1640, 2f, 0f, 0f, 0f, 20f, true);
    // iPhone Duo, as the iOS 27.1 Simulator reports it (2026-10-24). The outer display is 678 x 466 pt in landscape at @3.
    // At launch iOS gives no region for its camera and insets that whole edge by 82 pt (B 82 bottom right, T 82 turned over,
    // home 34); once it settles it moves the band to the side (L 84), reports the camera (37 x 37) and its 84 x 82 keep-out
    // box as occlusion regions, and the corner radii. DisplayShape takes any such band back. The inner display is 951 x 669 pt
    // with 55 pt corners and a 34 pt home inset; its FaceTime camera is an occlusion region only while the camera runs.
    public static readonly Profile IPhoneDuoOuter = new Profile("iPhone Duo outer (camera bottom right)", 2034, 1398, 3f, 0f, 0f, 0f, 82f, false, default(Rect), 4);
    public static readonly Profile IPhoneDuoOuterFlipped = new Profile("iPhone Duo outer (camera top left)", 2034, 1398, 3f, 0f, 0f, 82f, 34f, false, default(Rect), 3);
    public static readonly Profile IPhoneDuoOuterFlippedSettled = new Profile("iPhone Duo outer (camera top left, settled)", 2034, 1398, 3f, 84f, 0f, 0f, 34f, false,
        new[] { new Rect(29.33f, 399.67f, 37f, 37f), new Rect(0f, 384f, 84f, 82f) }, 3, new Vector4(8f, 8f, 59f, 59f));
    public static readonly Profile IPhoneDuoInner = new Profile("iPhone Duo inner", 2853, 2007, 3f, 0f, 0f, 0f, 34f, false, default(Rect), 4, new Vector4(55f, 55f, 55f, 55f));
    public static readonly Profile IPhoneDuoInnerCameraOn = new Profile("iPhone Duo inner (camera on)", 2853, 2007, 3f, 0f, 0f, 0f, 34f, false, new Rect(677.3f, 611f, 58f, 37f), 4, new Vector4(55f, 55f, 55f, 55f));
    public static readonly Profile IPhoneDuoInnerPortrait = new Profile("iPhone Duo inner portrait", 2007, 2853, 3f, 0f, 0f, 0f, 34f, false, default(Rect), 2, new Vector4(55f, 55f, 55f, 55f));

    /// <summary>Whether the handheld layer (touch controls, handheld HUD) runs.</summary>
    public static bool Active
    {
        get
        {
#if UNITY_EDITOR
            if (forced) return true;
            return EditorPreviewRequested && !Application.isBatchMode && !EditorAutopilotRunning();
#elif UNITY_IOS || UNITY_ANDROID
            return true;
#else
            return false;
#endif
        }
    }

    /// <summary>Unity screen pixels per point (iOS) or per dp (Android), including iOS render-resolution scaling.</summary>
    public static float PointScale
    {
        get
        {
#if UNITY_EDITOR
            if (forced) return forcedProfile.PointScale * forcedPixelRatio;
            var preview = EditorPreviewProfile;
            return preview.PointScale * Mathf.Max(.1f, Screen.height / (float)preview.Pixels.y);
#elif UNITY_IOS
            ReadIosViewMetrics();
            // Unity's Screen dimensions can be below the panel's native pixels (resolution scaling).
            // Divide by the actual Unity view's points so drawn controls and touch positions agree.
            // A URP render scale only changes the world target, not Screen pixels or overlay UI.
            return iosViewPoints.y > 0f ? Mathf.Max(.01f, Screen.height / iosViewPoints.y) : iosDisplayScale;
#elif UNITY_ANDROID
            if (cachedPointScale <= 0f) cachedPointScale = AndroidDensity();
            return cachedPointScale;
#else
            return 1f;
#endif
        }
    }

    /// <summary>Current UIKit display scale, independent of Unity render resolution; changes on a display transition.</summary>
    public static float DisplayScale
    {
        get
        {
#if UNITY_IOS && !UNITY_EDITOR
            ReadIosViewMetrics();
            return iosDisplayScale;
#elif UNITY_EDITOR
            return forced ? forcedProfile.PointScale : EditorPreviewProfile.PointScale;
#else
            return PointScale;
#endif
        }
    }

    /// <summary>UIKit's interface orientation of the current window (1 portrait, 2 upside down, 3 landscape with the
    /// device's top to the left, 4 to the right); 0 when unknown.</summary>
    public static int InterfaceOrientation
    {
        get
        {
#if UNITY_EDITOR
            return forced ? forcedProfile.Orientation : EditorPreviewProfile.Orientation;
#elif UNITY_IOS
            ReadIosViewMetrics();
            return iosInterfaceOrientation;
#else
            return 0;
#endif
        }
    }

    /// <summary>The corner radii iOS reports for the view (x bottom-left, y bottom-right, z top-left, w top-right); zero
    /// when it reports none (the Duo's outer display).</summary>
    static Vector4 ReportedCorners
    {
        get
        {
#if UNITY_EDITOR
            return forced ? forcedProfile.Corners : EditorPreviewProfile.Corners;
#elif UNITY_IOS
            ReadIosViewMetrics();
            return iosCorners;
#else
            return Vector4.zero;
#endif
        }
    }

    /// <summary>The screen in pixels (the capture target when a harness renders off screen).</summary>
    public static Vector2 ScreenPixels
    {
        get
        {
#if UNITY_EDITOR
            if (forced) return forcedPixels;
#endif
            return new Vector2(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
        }
    }

    /// <summary>The safe area in pixels, origin bottom-left, as <see cref="Screen.safeArea"/> reports it.</summary>
    public static Rect SafeAreaPixels
    {
        get
        {
#if UNITY_EDITOR
            if (forced) return InsetRect(forcedProfile, forcedPixels, PointScale);
            if (Active) return InsetRect(EditorPreviewProfile, ScreenPixels, PointScale);
#endif
            var area = Screen.safeArea;
            if (area.width <= 0f || area.height <= 0f) area = new Rect(0f, 0f, Screen.width, Screen.height);
            return area;
        }
    }

    /// <summary>The screen in points.</summary>
    public static Vector2 ScreenPoints => ScreenPixels / Mathf.Max(.01f, PointScale);

    public const int DuoNone = 0, DuoOuter = 1, DuoInner = 2;

    /// <summary>Which iPhone Duo display the view fills, in either orientation: <see cref="DuoOuter"/>, <see cref="DuoInner"/>
    /// or <see cref="DuoNone"/> (another phone, or a window that fills neither).</summary>
    public static int DuoDisplay
    {
        get
        {
            var p = ScreenPoints;
            if (SizeIs(p, DuoOuterNative)) return DuoOuter;
            if (SizeIs(p, DuoInnerNative)) return DuoInner;
            return DuoNone;
        }
    }

    static bool SizeIs(Vector2 p, Vector2 native) =>
        (Mathf.Abs(p.x - native.x) < 1f && Mathf.Abs(p.y - native.y) < 1f) || (Mathf.Abs(p.x - native.y) < 1f && Mathf.Abs(p.y - native.x) < 1f);

    /// <summary>
    /// Active camera/hinge reservations in bottom-left point space. The
    /// native path is authoritative on iOS 27.1; Editor profiles carry only
    /// deliberately synthetic stress-test regions.
    /// </summary>
    public static Rect[] ReservedRegionsPoints
    {
        get
        {
#if UNITY_EDITOR
            if (forced && forcedProfile.Regions != null && forcedProfile.Regions.Length > 0) return (Rect[])forcedProfile.Regions.Clone();
            return System.Array.Empty<Rect>();
#elif UNITY_IOS && !UNITY_EDITOR
            ReadIosViewMetrics();
            var result = new System.Collections.Generic.List<Rect>();
            var count = FRGetReservedRegions(0, reservedRegions, MaxReservedRegions);
            for (var i = 0; i < count; i++)
                result.Add(new Rect(reservedRegions[i * 4], reservedRegions[i * 4 + 1], reservedRegions[i * 4 + 2], reservedRegions[i * 4 + 3]));
            return result.ToArray();
#else
            return System.Array.Empty<Rect>();
#endif
        }
    }

    /// <summary>Active fold divisions in bottom-left point space (iOS 27.1; none when the device is fully open or flat).</summary>
    public static Rect[] DivisionRegionsPoints
    {
        get
        {
#if UNITY_IOS && !UNITY_EDITOR
            ReadIosViewMetrics();
            var result = new System.Collections.Generic.List<Rect>();
            var count = FRGetReservedRegions(1, reservedRegions, MaxReservedRegions);
            for (var i = 0; i < count; i++)
                result.Add(new Rect(reservedRegions[i * 4], reservedRegions[i * 4 + 1], reservedRegions[i * 4 + 2], reservedRegions[i * 4 + 3]));
            return result.ToArray();
#else
            return System.Array.Empty<Rect>();
#endif
        }
    }

    // ---------------------------------------------------------------- display shape

    /// <summary>
    /// The current view's shape, in points with the origin at the bottom-left. Red (2026-10-24): only what touches a
    /// camera or a rounded corner moves, with a motion transition; everything else stays full bleed. iOS 27.1 reports the
    /// inner display's FaceTime camera as an occlusion region (active only while the camera runs) and the fold as a
    /// division region, but folds the outer display's front camera into the safe area: an 82 pt inset along that whole
    /// edge and no region (Apple's iPhone Duo tech talk: "The outer display camera is always present"). So while the
    /// window fills the outer display, the shape carries that camera itself and gives the band back to the content.
    /// </summary>
    public struct DisplayShape
    {
        /// <summary>The view in points.</summary>
        public Vector2 Frame;
        /// <summary>What content must clear: active occlusion regions (their frames already carry iOS's margins for
        /// interactive content) and a camera iOS folded into the safe area (the bounds of its circle).</summary>
        public Rect[] Cutouts;
        /// <summary>The screen's corner radii: x bottom-left, y bottom-right, z top-left, w top-right (0: square or unknown).</summary>
        public Vector4 Corners;
        /// <summary>The safe area less a folded camera's band: what full-bleed content respects.</summary>
        public Rect BleedSafe;
        /// <summary>A camera band was taken out of the safe area.</summary>
        public bool CameraBandRemoved;
    }

    // The iPhone Duo outer display in its native portrait frame (466 x 678 pt, top-left origin), measured on the iOS 27.1
    // Simulator (device type iPhone Duo, iPhone19,4): a 37 pt camera whose centre sits 47.83 pt from the native top and
    // right edges, and the corner radii of the device's capabilities (8 pt on the hinge side, native left; 59 pt on the
    // far side). iOS reports neither for this display.
    static readonly Vector2 DuoOuterNative = new Vector2(466f, 678f);
    /// <summary>The inner display's portrait frame (669 x 951 pt at @3, iOS 27.1 Simulator).</summary>
    static readonly Vector2 DuoInnerNative = new Vector2(669f, 951f);
    static readonly Vector2 DuoOuterCamera = new Vector2(466f - 47.83f, 47.83f);
    const float DuoOuterCameraRadius = 18.5f;
    /// <summary>Native corner radii: upper-left, upper-right, lower-left, lower-right.</summary>
    static readonly Vector4 DuoOuterCorners = new Vector4(8f, 59f, 8f, 59f);
    /// <summary>The home indicator's inset when a camera band covers the bottom edge: the last bottom inset iOS gave without
    /// one (learned), else the Duo's 34 pt (measured in both orientations).</summary>
    static float homeInset = 34f;

    static int shapeFrame = -1;
    static DisplayShape shape;

    /// <summary>The current view's shape (cutouts, corners, bleed safe area), read once a frame.</summary>
    public static DisplayShape Shape
    {
        get
        {
            if (shapeFrame == Time.frameCount && shape.Cutouts != null) return shape;
            shapeFrame = Time.frameCount;
            shape = ComputeShape();
            return shape;
        }
    }

    /// <summary>The bleed safe area in pixels, origin bottom-left: <see cref="SafeAreaPixels"/> less a folded camera's band.</summary>
    public static Rect BleedSafeAreaPixels
    {
        get
        {
            var s = Shape.BleedSafe;
            var scale = Mathf.Max(.01f, PointScale);
            return new Rect(s.x * scale, s.y * scale, s.width * scale, s.height * scale);
        }
    }

    static DisplayShape ComputeShape()
    {
        var frame = ScreenPoints;
        var scale = Mathf.Max(.01f, PointScale);
        var px = SafeAreaPixels;
        var safe = new Rect(px.x / scale, px.y / scale, px.width / scale, px.height / scale);
        var result = new DisplayShape { Frame = frame, Corners = ReportedCorners, BleedSafe = safe };
        var regions = ReservedRegionsPoints;
        // What content clears is the occluding element itself: of nested regions (the camera inside its keep-out box) the
        // innermost; the boxes only explain the safe-area band iOS builds from them.
        var cutouts = new System.Collections.Generic.List<Rect>();
        var explainers = new System.Collections.Generic.List<Rect>(regions);
        foreach (var r in regions)
        {
            var holdsAnother = false;
            foreach (var o in regions) if (o != r && r.Contains(o.min) && r.Contains(o.max)) holdsAnother = true;
            if (!holdsAnother) cutouts.Add(r);
        }
        var orientation = InterfaceOrientation;
        var landscape = orientation == 3 || orientation == 4;
        var fills = landscape
            ? Mathf.Abs(frame.x - DuoOuterNative.y) < 1f && Mathf.Abs(frame.y - DuoOuterNative.x) < 1f
            : Mathf.Abs(frame.x - DuoOuterNative.x) < 1f && Mathf.Abs(frame.y - DuoOuterNative.y) < 1f;
        if (orientation >= 1 && orientation <= 4 && fills)
        {
            // The outer display: iOS reports its corners and camera only once it settles; until then, its own description.
            if (result.Corners == Vector4.zero) result.Corners = MapCorners(DuoOuterCorners, orientation);
            var c = MapNative(DuoOuterCamera, orientation, frame);
            var rr = DuoOuterCameraRadius;
            var camera = new Rect(c.x - rr, c.y - rr, rr * 2f, rr * 2f);
            var known = false;
            foreach (var r in cutouts) if (r.Overlaps(camera)) known = true;
            if (!known) cutouts.Add(camera);
            explainers.Add(camera);
        }
        // On the Duo's own displays (Red's ask; other phones keep the insets their signed masters use), give back every edge
        // band a camera explains: the camera lies inside the band, and it is a local feature (well under half the edge).
        // The bottom keeps the home indicator's inset.
        var inner = landscape
            ? Mathf.Abs(frame.x - DuoInnerNative.y) < 1f && Mathf.Abs(frame.y - DuoInnerNative.x) < 1f
            : Mathf.Abs(frame.x - DuoInnerNative.x) < 1f && Mathf.Abs(frame.y - DuoInnerNative.y) < 1f;
        if (!fills && !inner) explainers.Clear();
        var left = safe.xMin;
        var right = frame.x - safe.xMax;
        var bottom = safe.yMin;
        var top = frame.y - safe.yMax;
        var bottomBand = false;
        foreach (var k in explainers)
        {
            if (left > .5f && k.xMax <= left + 1f && k.height < frame.y * .45f) { left = 0f; result.CameraBandRemoved = true; }
            if (right > .5f && k.xMin >= frame.x - right - 1f && k.height < frame.y * .45f) { right = 0f; result.CameraBandRemoved = true; }
            if (top > .5f && k.yMin >= frame.y - top - 1f && k.width < frame.x * .45f) { top = 0f; result.CameraBandRemoved = true; }
            if (bottom > .5f && k.yMax <= bottom + 1f && k.width < frame.x * .45f) bottomBand = true;
        }
        if (bottomBand) { bottom = Mathf.Min(bottom, homeInset); result.CameraBandRemoved = true; }
        else if (bottom > .5f && fills) homeInset = bottom;
        result.BleedSafe = Rect.MinMaxRect(left, bottom, frame.x - right, frame.y - top);
        result.Cutouts = cutouts.ToArray();
        return result;
    }

    /// <summary>A point of the outer display's native portrait frame (top-left origin) in the view's bottom-left points, for
    /// a UIKit interface orientation (3: landscape with the device's top to the left; 4: to the right).</summary>
    static Vector2 MapNative(Vector2 p, int orientation, Vector2 frame)
    {
        Vector2 topLeft;
        switch (orientation)
        {
            case 2: topLeft = new Vector2(DuoOuterNative.x - p.x, DuoOuterNative.y - p.y); break;
            case 3: topLeft = new Vector2(p.y, DuoOuterNative.x - p.x); break;
            case 4: topLeft = new Vector2(DuoOuterNative.y - p.y, p.x); break;
            default: topLeft = p; break;
        }
        return new Vector2(topLeft.x, frame.y - topLeft.y);
    }

    /// <summary>Native corner radii (upper-left, upper-right, lower-left, lower-right) as the view's x bottom-left,
    /// y bottom-right, z top-left, w top-right, for a UIKit interface orientation.</summary>
    static Vector4 MapCorners(Vector4 n, int orientation)
    {
        switch (orientation)
        {
            case 2: return new Vector4(n.y, n.x, n.w, n.z);
            case 3: return new Vector4(n.x, n.z, n.y, n.w);
            case 4: return new Vector4(n.w, n.y, n.z, n.x);
            default: return new Vector4(n.z, n.w, n.x, n.y);
        }
    }

    /// <summary>
    /// A round control's centre once its circle clears every cutout by <paramref name="cutoutGap"/> and every rounded
    /// corner by <paramref name="cornerGap"/>; unchanged when nothing touches it. A corner draws it in along the corner's
    /// diagonal, just far enough; a cutout pushes it out the shorter way that keeps the circle on screen.
    /// </summary>
    public static Vector2 ClearCircle(Vector2 centre, float radius, float cutoutGap = 12f, float cornerGap = 4f) =>
        ClearCircle(Shape, centre, radius, cutoutGap, cornerGap);

    public static Vector2 ClearCircle(DisplayShape s, Vector2 centre, float radius, float cutoutGap, float cornerGap)
    {
        var reach = radius + cornerGap;
        for (var i = 0; i < 4; i++)
        {
            // A corner no rounder than the circle only meets it on the straight edges.
            var round = s.Corners[i];
            if (round <= reach) continue;
            var left = i == 0 || i == 2;
            var bottom = i < 2;
            var k = new Vector2(left ? round : s.Frame.x - round, bottom ? round : s.Frame.y - round);
            var inZone = (left ? centre.x < k.x : centre.x > k.x) && (bottom ? centre.y < k.y : centre.y > k.y);
            var room = round - reach;
            if (inZone && (centre - k).sqrMagnitude > room * room) centre = k + (centre - k).normalized * room;
        }
        if (s.Cutouts == null) return centre;
        foreach (var cut in s.Cutouts)
        {
            var grow = radius + cutoutGap;
            var keep = new Rect(cut.xMin - grow, cut.yMin - grow, cut.width + grow * 2f, cut.height + grow * 2f);
            if (!keep.Contains(centre)) continue;
            var best = centre;
            var bestDistance = float.MaxValue;
            foreach (var candidate in new[] { new Vector2(keep.xMin, centre.y), new Vector2(keep.xMax, centre.y), new Vector2(centre.x, keep.yMin), new Vector2(centre.x, keep.yMax) })
            {
                if (candidate.x < radius || candidate.x > s.Frame.x - radius || candidate.y < radius || candidate.y > s.Frame.y - radius) continue;
                var distance = (candidate - centre).sqrMagnitude;
                if (distance < bestDistance) { best = candidate; bestDistance = distance; }
            }
            centre = best;
        }
        return centre;
    }

    /// <summary>
    /// How far the in-play HUD keeps from a rounded display corner (Red, 2026-10-24: the HUD sat on the Duo's curves).
    /// iOS 26's corner-adapted layout regions and Android's RoundedCorner sample both stop at the arc's 45° point,
    /// r · (1 − sin 45°), where a box's corner touches the curve; this keeps the same 16 pt from the curve that iOS keeps
    /// from a straight edge (research/interaction/duo/rounded_corners).
    /// </summary>
    public const float HudCornerGap = 16f;

    /// <summary>
    /// A HUD block anchored at a screen corner (the zone text, the threat readout) once it keeps <paramref name="cornerGap"/>
    /// from the rounded corner and <paramref name="cutoutGap"/> from every cutout. Unlike <see cref="ClearRect"/> it first
    /// moves away from the top or bottom edge, at most to the corner's diagonal inset r − (r − gap) / √2 and no further than
    /// clearing needs, then sideways as far as is still needed: a block whose top sits at the top margin moves a few points
    /// down and in, never across the whole corner. Cutouts still push it sideways. Unchanged when nothing touches it.
    /// </summary>
    public static Rect ClearCornerBlock(Rect rect, float cornerGap = HudCornerGap, float cutoutGap = 12f) =>
        ClearCornerBlock(Shape, rect, cornerGap, cutoutGap);

    public static Rect ClearCornerBlock(DisplayShape s, Rect rect, float cornerGap, float cutoutGap)
    {
        // Away from the edge first: the largest move any rounded corner asks for, per edge.
        var down = 0f;
        var up = 0f;
        for (var i = 0; i < 4; i++)
        {
            var round = s.Corners[i];
            if (round <= cornerGap) continue;
            var left = i == 0 || i == 2;
            var bottom = i < 2;
            var dx = left ? round - rect.xMin : rect.xMax - (s.Frame.x - round);
            var dy = bottom ? round - rect.yMin : rect.yMax - (s.Frame.y - round);
            if (dx <= 0f || dy <= 0f) continue;
            var room = round - cornerGap;
            if (dx * dx + dy * dy <= room * room) continue;
            var inset = round - room / Mathf.Sqrt(2f);
            var edgeGap = bottom ? rect.yMin : s.Frame.y - rect.yMax;
            var move = Mathf.Max(0f, inset - edgeGap);
            if (dx < room) move = Mathf.Min(move, dy - Mathf.Sqrt(room * room - dx * dx));
            if (bottom) up = Mathf.Max(up, move); else down = Mathf.Max(down, move);
        }
        rect.y += up - down;
        // Then sideways for what is left, and the cutouts, as ClearRect does.
        return ClearRect(s, rect, false, cutoutGap, cornerGap, 0f);
    }

    /// <summary>
    /// A block of content (a line of text, a chip, a settings row) once it clears every cutout by
    /// <paramref name="cutoutGap"/> and every rounded corner by <paramref name="cornerGap"/>. It gives way sideways:
    /// moved toward the screen's middle, or with <paramref name="narrow"/>, narrowed from the side that touches (a row
    /// whose label and value keep their own edges). Unchanged when nothing touches it.
    /// </summary>
    public static Rect ClearRect(Rect rect, bool narrow = false, float cutoutGap = 12f, float cornerGap = 6f, float minimumWidth = 44f) =>
        ClearRect(Shape, rect, narrow, cutoutGap, cornerGap, minimumWidth);

    public static Rect ClearRect(DisplayShape s, Rect rect, bool narrow, float cutoutGap, float cornerGap, float minimumWidth)
    {
        var fromLeft = 0f;
        var fromRight = 0f;
        for (var i = 0; i < 4; i++)
        {
            var round = s.Corners[i];
            if (round <= cornerGap) continue;
            var left = i == 0 || i == 2;
            var bottom = i < 2;
            // How far the rect's corner nearest this screen corner lies inside the corner's square, both ways.
            var dx = left ? round - rect.xMin : rect.xMax - (s.Frame.x - round);
            var dy = bottom ? round - rect.yMin : rect.yMax - (s.Frame.y - round);
            if (dx <= 0f || dy <= 0f) continue;
            var room = round - cornerGap;
            if (dx * dx + dy * dy <= room * room) continue;
            // Sideways to where the arc (less the gap) crosses that edge of the rect; the whole square when the edge is lower.
            var need = dy >= room ? dx : dx - Mathf.Sqrt(room * room - dy * dy);
            if (left) fromLeft = Mathf.Max(fromLeft, need); else fromRight = Mathf.Max(fromRight, need);
        }
        if (narrow) { rect.xMin += fromLeft; rect.xMax -= fromRight; }
        else rect.x += fromLeft - fromRight;
        if (s.Cutouts != null)
        {
            foreach (var cut in s.Cutouts)
            {
                var keep = new Rect(cut.xMin - cutoutGap, cut.yMin - cutoutGap, cut.width + cutoutGap * 2f, cut.height + cutoutGap * 2f);
                if (!keep.Overlaps(rect)) continue;
                if (keep.center.x >= rect.center.x)
                {
                    var over = rect.xMax - keep.xMin;
                    if (narrow) rect.xMax -= over; else rect.x -= over;
                }
                else
                {
                    var over = keep.xMax - rect.xMin;
                    if (narrow) rect.xMin += over; else rect.x += over;
                }
            }
        }
        if (narrow && rect.width < minimumWidth) rect.width = minimumWidth;
        return rect;
    }

    // ------------------------------------------------------------------ field of view

    /// <summary>The picture the camera's field of view is authored for (16:9).</summary>
    public const float FovReferenceAspect = 16f / 9f;
    /// <summary>The widest vertical field of view a handheld view draws: a portrait window would otherwise bend into a fisheye.</summary>
    public const float MaxHandheldFov = 100f;

    /// <summary>
    /// The vertical field of view a handheld view draws for one authored at 16:9 (Red, 2026-10-24: the unfolded Duo looked
    /// too close). Views squarer than 16:9 keep 16:9's horizontal field and see more above and below instead of less at
    /// the sides, up to <see cref="MaxHandheldFov"/>; wider views keep the authored value. Desktop always draws it as authored.
    /// </summary>
    public static float AspectFov(float fov, float aspect)
    {
        if (!Active || aspect <= 0f || aspect >= FovReferenceAspect || fov <= 0f || fov >= 179f) return fov;
        var widened = 2f * Mathf.Atan(Mathf.Tan(fov * .5f * Mathf.Deg2Rad) * FovReferenceAspect / aspect) * Mathf.Rad2Deg;
        return Mathf.Min(widened, Mathf.Max(fov, MaxHandheldFov));
    }

    /// <summary>Large-view grip, kept under its existing API name: current shortest side is at least 600 pt/dp.
    /// An unfolded phone can use it; a narrow tablet window cannot. Hardware identity never chooses layout.</summary>
    public static bool IsTablet
    {
        get
        {
#if UNITY_EDITOR || UNITY_IOS || UNITY_ANDROID
            var points = ScreenPoints;
            return Mathf.Min(points.x, points.y) >= 600f;
#else
            return false;
#endif
        }
    }

    /// <summary>The controls' starting size: 1.4× on a large view, 1× on a compact view.</summary>
    public static float DeviceControlsScale => IsTablet ? 1.4f : 1f;

    /// <summary>
    /// Motion should be reduced: the OS asks for it (iOS Reduce Motion, Android
    /// "remove animations"), or CAMERA MOTION is OFF. Transitions then become
    /// short opacity changes with no slide, scale or overshoot.
    /// </summary>
    public static bool ReduceMotion => OsReduceMotion || FrontRoomsSettings.CameraMotionPercent <= 0;

    /// <summary>
    /// False while a handheld's startup is still loading behind the held title room (Red, 2026-10-24: the room is the
    /// loading screen; TAP TO START and the legend wait until everything is in). The game sets it; desktop never clears it.
    /// </summary>
    public static bool StartupReady = true;
    /// <summary>When <see cref="StartupReady"/> turned true, on <see cref="FrontRoomsTouchMotion.Now"/>'s clock.</summary>
    public static float StartupReadyAt = -10f;

    /// <summary>The operating system's own reduce-motion switch.</summary>
    public static bool OsReduceMotion
    {
        get
        {
#if UNITY_EDITOR
            return EditorReduceMotion;
#elif UNITY_IOS
            return FRReduceMotion();
#elif UNITY_ANDROID
            return AndroidAnimationsOff();
#else
            return false;
#endif
        }
    }

    /// <summary>Whether the device can play haptics at all (no iPad can).</summary>
    public static bool HapticsSupported
    {
        get
        {
#if UNITY_EDITOR
            // Capability follows hardware, not the large-view layout an unfolded phone may use.
            return !(forced ? forcedProfile.Tablet : EditorPreviewProfile.Tablet);
#elif UNITY_IOS
            return !FRIsPad();
#elif UNITY_ANDROID
            return true;
#else
            return false;
#endif
        }
    }

    static Rect InsetRect(Profile profile, Vector2 pixels, float pointScale)
    {
        var left = profile.InsetLeft * pointScale;
        var right = profile.InsetRight * pointScale;
        var top = profile.InsetTop * pointScale;
        var bottom = profile.InsetBottom * pointScale;
        return new Rect(left, bottom, Mathf.Max(1f, pixels.x - left - right), Mathf.Max(1f, pixels.y - top - bottom));
    }

#if UNITY_IOS && !UNITY_EDITOR
    static int iosMetricsFrame = -1;
    static Vector2 iosViewPoints;
    static float iosDisplayScale = 1f;
    static int iosInterfaceOrientation;
    static Vector4 iosCorners;
    static readonly float[] iosCornerRadii = new float[4];
    const int MaxReservedRegions = 8;
    static readonly float[] reservedRegions = new float[MaxReservedRegions * 4];
    [DllImport("__Internal")] static extern int FRGetViewMetrics(out float width, out float height, out float displayScale);
    [DllImport("__Internal")] static extern int FRGetReservedRegions(int division, float[] rects, int maxRegions);
    [DllImport("__Internal")] static extern int FRGetInterfaceOrientation();
    [DllImport("__Internal")] static extern int FRGetCornerRadii(float[] radii);

    static void ReadIosViewMetrics()
    {
        // Share one coherent snapshot within a frame, never a launch-time UIScreen/nativeScale cache.
        if (iosMetricsFrame == Time.frameCount) return;
        iosMetricsFrame = Time.frameCount;
        if (FRGetViewMetrics(out var width, out var height, out var scale) != 0)
        {
            iosViewPoints = new Vector2(width, height);
            iosDisplayScale = Mathf.Max(.01f, scale);
        }
        else
        {
            iosViewPoints = Vector2.zero;
            iosDisplayScale = Mathf.Max(.01f, scale);
        }
        iosInterfaceOrientation = FRGetInterfaceOrientation();
        iosCorners = FRGetCornerRadii(iosCornerRadii) != 0
            ? new Vector4(iosCornerRadii[0], iosCornerRadii[1], iosCornerRadii[2], iosCornerRadii[3])
            : Vector4.zero;
    }
    // ints, not bools: a C bool return marshals unreliably.
    [DllImport("__Internal")] static extern int FRReduceMotionNative();
    [DllImport("__Internal")] static extern int FRIsPadNative();
    static bool FRReduceMotion() => FRReduceMotionNative() != 0;
    static bool FRIsPad() => FRIsPadNative() != 0;
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
    static float cachedPointScale;
    static float lastAnimationCheck = -10f;
    static bool animationsOff;

    static float AndroidDensity()
    {
        try
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var resources = activity.Call<AndroidJavaObject>("getResources"))
            using (var metrics = resources.Call<AndroidJavaObject>("getDisplayMetrics"))
            {
                var density = metrics.Get<float>("density");
                if (density > .5f) return density;
            }
        }
        catch (System.Exception) { }
        return Mathf.Max(1f, (Screen.dpi > 0f ? Screen.dpi : 160f) / 160f);
    }

    static bool AndroidAnimationsOff()
    {
        // Settings.Global.ANIMATOR_DURATION_SCALE == 0 is Android's "remove animations". Checked at most once a second.
        if (Time.unscaledTime - lastAnimationCheck < 1f) return animationsOff;
        lastAnimationCheck = Time.unscaledTime;
        try
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var resolver = activity.Call<AndroidJavaObject>("getContentResolver"))
            using (var global = new AndroidJavaClass("android.provider.Settings$Global"))
                animationsOff = global.CallStatic<float>("getFloat", resolver, "animator_duration_scale", 1f) <= 0f;
        }
        catch (System.Exception) { animationsOff = false; }
        return animationsOff;
    }
#endif

#if UNITY_EDITOR
    static bool forced;
    static Profile forcedProfile;
    static Vector2 forcedPixels;
    static float forcedPixelRatio = 1f;

    /// <summary>Set by the Editor menu (FrontRooms 3D / Mobile / Touch preview): the interactive Play Mode preview.</summary>
    public static bool EditorPreviewRequested;
    /// <summary>The device the interactive preview stands in for.</summary>
    public static Profile EditorPreviewProfile = IPhonePro;
    /// <summary>Stand-in for the OS Reduce Motion switch in the Editor.</summary>
    public static bool EditorReduceMotion;

    /// <summary>A harness takes the handheld path for its own run: the profile's screen, safe area and point scale, rendered at <paramref name="renderPixels"/> (the profile's pixels when null).</summary>
    public static void Force(Profile profile, Vector2Int? renderPixels = null)
    {
        forced = true;
        forcedProfile = profile;
        var pixels = renderPixels ?? profile.Pixels;
        forcedPixels = new Vector2(pixels.x, pixels.y);
        forcedPixelRatio = pixels.y / (float)profile.Pixels.y;
        shapeFrame = -1;
        homeInset = 34f;
    }

    public static void ClearForce()
    {
        forced = false;
        forcedPixelRatio = 1f;
        shapeFrame = -1;
    }

    public static bool Forced => forced;
    public static Profile ForcedProfile => forcedProfile;

    // The map chat's autopilot (FrontRoomsMainScenePlaytest) keeps the desktop path, even with the preview on.
    static bool EditorAutopilotRunning()
    {
        if (UnityEditor.SessionState.GetBool(FrontRooms3DGame.AutopilotKey, false)) return true;
        foreach (var arg in System.Environment.GetCommandLineArgs())
            if (arg.StartsWith("-autopilot", System.StringComparison.Ordinal)) return true;
        return false;
    }
#endif
}
