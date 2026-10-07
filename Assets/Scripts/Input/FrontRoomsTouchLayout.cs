using UnityEngine;

/// <summary>
/// Where every touch control sits, in points with the origin at the bottom-left
/// of the screen. The touch state machine hit-tests against it and the view draws
/// from it, so a control is always pressed where it is drawn.
///
/// Gameplay controls hang off the safe area (the Figma phone master at
/// 874 × 402 pt with insets L 62 / R 62 / B 21 puts the stick's rest at
/// (150, 112) bottom-up, USE at (740, 140) and pause at (784, 368)). Menus sit on
/// the frame centre, as in the Figma screen masters. Sizes scale with the
/// controls scale (CONTROLS SIZE × 1.4 on tablets); hit areas never shrink below
/// the Apple minimums (44 pt for play controls, 28 pt for menu rows).
/// </summary>
public readonly struct FrontRoomsTouchLayout
{
    public readonly Vector2 Frame;
    public readonly float SafeLeft, SafeRight, SafeBottom, SafeTop;
    public readonly float Scale;
    public readonly bool LeftHanded;
    public readonly bool Tablet;
    /// <summary>Android's back gesture lives on both long edges: a move touch never starts this close to them.</summary>
    public readonly float EdgeGuard;

    public FrontRoomsTouchLayout(Vector2 framePoints, Rect safePoints, float scale, bool leftHanded, bool tablet, float edgeGuard)
    {
        Frame = framePoints;
        SafeLeft = Mathf.Max(0f, safePoints.xMin);
        SafeBottom = Mathf.Max(0f, safePoints.yMin);
        SafeRight = Mathf.Max(0f, framePoints.x - safePoints.xMax);
        SafeTop = Mathf.Max(0f, framePoints.y - safePoints.yMax);
        Scale = Mathf.Clamp(scale, .6f, 2.2f);
        LeftHanded = leftHanded;
        Tablet = tablet;
        EdgeGuard = edgeGuard;
    }

    // ------------------------------------------------------------- the stick
    public float StickRadius => 60f * Scale;
    public float ThumbRadius => 26f * Scale;
    public float StickDeadZone => .12f * StickRadius;
    /// <summary>The sprint socket's centre above the stick's centre.</summary>
    public float SocketOffset => 92f * Scale;
    public float SocketRadius => 20f * Scale;
    /// <summary>How close the thumb must come to the socket's centre to arm it (a little wider than the ring, so it is easy to find).</summary>
    public float SocketCatchRadius => 28f * Scale;
    /// <summary>A pull back past this (below the stick's centre) is the desktop S: it cancels a shot and ends a sprint.</summary>
    public float ShotBackDepth => .55f * StickRadius;

    /// <summary>Where the stick rests (the ghost) and where a FIXED stick lives.</summary>
    public Vector2 StickRest
    {
        get
        {
            var inset = 28f + StickRadius;
            var x = LeftHanded ? Frame.x - SafeRight - inset : SafeLeft + inset;
            return new Vector2(x, SafeBottom + 31f + StickRadius);
        }
    }

    /// <summary>The optional SPRINT toggle (Settings › SPRINT: BUTTON): above the stick's rest.</summary>
    public Vector2 SprintButtonCenter => StickRest + Vector2.up * (104f * Scale);
    public float SprintButtonRadius => 28f * Scale;
    public float SprintButtonHitRadius => Mathf.Max(22f, 40f * Scale);

    // ------------------------------------------------------------------ USE
    public float UseRadius => 36f * Scale;
    public float UseHitRadius => Mathf.Max(22f, 44f * Scale);
    public float HoldRingRadius => 46f * Scale;

    public Vector2 UseCenter
    {
        get
        {
            var inset = 28f + 44f * Scale;
            var x = LeftHanded ? SafeLeft + inset : Frame.x - SafeRight - inset;
            return new Vector2(x, SafeBottom + 75f + 44f * Scale);
        }
    }

    // ---------------------------------------------------------------- pause
    public float PauseSize => 32f;
    public float PauseHitSize => 44f;
    public Vector2 PauseCenter => new Vector2(Frame.x - SafeRight - 28f, Frame.y - SafeTop - 34f);

    /// <summary>The read-only top band (zone, key, pause): touches starting above it never move or look.</summary>
    public float TopBandBottom => Frame.y - SafeTop - 64f;

    // ----------------------------------------------------------------- zones
    public bool InMoveZone(Vector2 p)
    {
        if (p.y >= TopBandBottom || p.y < SafeBottom) return false;
        if (p.x < EdgeGuard || p.x > Frame.x - EdgeGuard) return false;
        var mid = Frame.x * .5f;
        return LeftHanded ? p.x >= mid : p.x < mid;
    }

    public bool InLookZone(Vector2 p)
    {
        if (p.y >= TopBandBottom) return false;
        var mid = Frame.x * .5f;
        return LeftHanded ? p.x < mid : p.x >= mid;
    }

    /// <summary>A floating stick's centre for a touch at <paramref name="finger"/>: the finger, kept far enough from the screen edges that the ring stays on screen.</summary>
    public Vector2 FloatingOrigin(Vector2 finger)
    {
        var r = StickRadius;
        return new Vector2(Mathf.Clamp(finger.x, r * .75f, Frame.x - r * .75f), Mathf.Clamp(finger.y, Mathf.Max(SafeBottom, r * .6f), Frame.y - r));
    }

    // ----------------------------------------------------------------- menus
    public Vector2 Center => Frame * .5f;

    /// <summary>A Figma screen-master position (874 × 402, origin top-left) on this frame, centred.</summary>
    public Vector2 FromMaster(float x, float yFromTop) => Center + new Vector2(x - 437f, 201f - yFromTop);

    public const float ChipHeight = 40f;
    public const float ChipHitHeight = 44f;

    // The settings card (Figma: 742 × 369 at 66, 10 on the phone master).
    public Vector2 SettingsCardSize => new Vector2(Mathf.Min(742f, Frame.x - 32f), Mathf.Min(369f, Frame.y - 24f));
    public Vector2 SettingsCardCenter => FromMaster(437f, 194.5f);
    public const float SettingsRowHeight = 44f;
    public const float SettingsColumnWidth = 337f;

    public Rect SettingsColumn(int column)
    {
        var card = SettingsCardSize;
        var c = SettingsCardCenter;
        var left = c.x - card.x * .5f + 24f + column * (SettingsColumnWidth + 20f);
        var top = c.y + card.y * .5f - 52f;
        var bottom = c.y - card.y * .5f + 8f;
        return new Rect(left, bottom, SettingsColumnWidth, top - bottom);
    }

    public static bool Contains(Vector2 center, float radius, Vector2 p) => (p - center).sqrMagnitude <= radius * radius;

    public static bool Contains(Vector2 center, Vector2 size, Vector2 p) =>
        Mathf.Abs(p.x - center.x) <= size.x * .5f && Mathf.Abs(p.y - center.y) <= size.y * .5f;
}
