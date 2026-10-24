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
/// the Apple minimum of 44 pt. Layout follows the current window's safe area,
/// including a fold/unfold or side-by-side resize, without scaling touch targets.
///
/// The safe area it is given is the bleed safe area (FrontRoomsHandheld.DisplayShape):
/// iOS's, less a camera band iOS folded into it. A control that touches a camera or
/// a rounded corner gives way on its own (Red, 2026-10-24); each such control has a
/// ...Base position (before it gives way) so the view can animate the difference.
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
    public Rect SafeRect => Rect.MinMaxRect(SafeLeft, SafeBottom, Frame.x - SafeRight, Frame.y - SafeTop);

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
    /// <summary>Inside this the stick creeps (the slow gait); beyond it, it walks (Red, 2026-10-08).</summary>
    public float SlowRadius => 36f * Scale;
    /// <summary>The slow/walk edge moves this far either way, so a thumb resting on it never flickers between gaits.</summary>
    public float SlowHysteresis => 2f * Scale;
    /// <summary>The run line, 24 pt past the ring and drawn only ahead: crossing it runs, back inside the ring walks.</summary>
    public float RunLineRadius => 84f * Scale;
    /// <summary>How far the drawn thumb follows the finger ahead (or while running): just over the run line.</summary>
    public float RunThumbReach => 88f * Scale;
    /// <summary>A pull back past this (below the stick's centre) is the desktop S: it cancels a shot and ends a run.</summary>
    public float ShotBackDepth => .55f * StickRadius;

    /// <summary>Where the stick rests before it gives way to a camera or a rounded corner.</summary>
    public Vector2 StickRestBase
    {
        get
        {
            var inset = 28f + StickRadius;
            var x = LeftHanded ? Frame.x - SafeRight - inset : SafeLeft + inset;
            return KeepInsideSafe(new Vector2(x, SafeBottom + 31f + StickRadius), StickRadius);
        }
    }

    /// <summary>Where the stick rests (the ghost) and where a FIXED stick lives.</summary>
    public Vector2 StickRest => FrontRoomsHandheld.ClearCircle(StickRestBase, StickRadius);

    /// <summary>The optional RUN button (Settings › RUN: BUTTON), above the stick's rest, before it gives way.</summary>
    public Vector2 SprintButtonCenterBase => KeepInsideSafe(StickRestBase + Vector2.up * (104f * Scale), SprintButtonHitRadius);
    /// <summary>The optional RUN button (Settings › RUN: BUTTON): above the stick's rest.</summary>
    public Vector2 SprintButtonCenter => FrontRoomsHandheld.ClearCircle(SprintButtonCenterBase, SprintButtonHitRadius);
    public float SprintButtonRadius => 28f * Scale;
    public float SprintButtonHitRadius => Mathf.Max(22f, 40f * Scale);

    // ------------------------------------------------------------------ USE
    public float UseRadius => 36f * Scale;
    public float UseHitRadius => Mathf.Max(22f, 44f * Scale);
    public float HoldRingRadius => 46f * Scale;

    /// <summary>USE before it gives way to a camera or a rounded corner.</summary>
    public Vector2 UseCenterBase
    {
        get
        {
            var inset = 28f + 44f * Scale;
            var x = LeftHanded ? SafeLeft + inset : Frame.x - SafeRight - inset;
            return KeepInsideSafe(new Vector2(x, SafeBottom + 75f + 44f * Scale), Mathf.Max(UseHitRadius, HoldRingRadius));
        }
    }

    public Vector2 UseCenter => FrontRoomsHandheld.ClearCircle(UseCenterBase, Mathf.Max(UseHitRadius, HoldRingRadius));

    /// <summary>
    /// USE as BACK in a close-up (EXAMINE, contract T2): moved to the lower right, clear of the transcript panel (y 64–280 pt
    /// from the top) and the framed plan, its hit circle touching the safe area's bottom and outer edges. On the phone that is
    /// (768, 337) from the top-left, its Ø88 hit spanning y 293–381; on the iPad (×1.4) the disc stays 11 pt off the glass.
    /// </summary>
    public Vector2 InspectBackCenterBase
    {
        get
        {
            var u = UseCenterBase;
            return KeepInsideSafe(new Vector2(u.x + (LeftHanded ? -28f : 28f) * Scale, SafeBottom + 44f * Scale), UseHitRadius);
        }
    }

    public Vector2 InspectBackCenter => FrontRoomsHandheld.ClearCircle(InspectBackCenterBase, UseHitRadius);

    /// <summary>The TEXT chip in a close-up (EXAMINE T6, 平面视觉 G10): the slot pause had when G10 was signed (pause is hidden in
    /// a close-up), over the panel's right end: centre (778, 28) from the top-left on the phone. Its hit is 44 pt square.</summary>
    public Vector2 TextChipCenterBase => new Vector2(Frame.x - SafeRight - 34f, Frame.y - SafeTop - 28f);
    public static readonly Vector2 TextChipSize = new Vector2(48f, 26f);
    /// <summary>Where the chip draws: a block in the corner, it keeps the HUD's 16 pt from a rounded corner and 12 pt from a cutout.</summary>
    public Vector2 TextChipCenter => FrontRoomsHandheld.ClearCornerBlock(new Rect(TextChipCenterBase - TextChipSize * .5f, TextChipSize)).center;
    public float TextChipHitSize => 44f;

    /// <summary>
    /// The transcript panel in a close-up (EXAMINE T7, 平面视觉 G9): the right 36 % of the safe area, from 64 pt under the top (and
    /// at least 12 pt under the TEXT chip) down to 13 pt above BACK's hit circle; on the phone x 542–812, y 64–280 from the top.
    /// </summary>
    public Rect TranscriptPanel
    {
        get
        {
            var safeWidth = Frame.x - SafeLeft - SafeRight;
            var top = Mathf.Min(Frame.y - SafeTop - 64f, TextChipCenter.y - TextChipSize.y * .5f - 12f);
            return Rect.MinMaxRect(SafeLeft + safeWidth * .64f, InspectBackCenter.y + UseHitRadius + 13f, Frame.x - SafeRight, top);
        }
    }

    // ---------------------------------------------------------------- pause
    public float PauseSize => 32f;
    public float PauseHitSize => 44f;
    /// <summary>
    /// The pause control sits at the right bleed (Red, 2026-10-24): 28 pt from the
    /// window's right edge whatever the side insets, under the top safe inset. It
    /// moves only for a camera or a rounded corner (<see cref="PauseCenter"/>).
    /// </summary>
    public Vector2 PauseCenterBase => new Vector2(
        Frame.x - 28f,
        ClampCenter(Frame.y - SafeTop - 34f,
            SafeBottom + PauseHitSize * .5f,
            Frame.y - SafeTop - PauseHitSize * .5f));

    /// <summary>Where pause draws: drawn in along a rounded corner's diagonal until its hit circle keeps the HUD's 16 pt from the curve.</summary>
    public Vector2 PauseCenter => FrontRoomsHandheld.ClearCircle(PauseCenterBase, PauseHitSize * .5f, 12f, FrontRoomsHandheld.HudCornerGap);

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

    /// <summary>A floating stick's centre for a touch at <paramref name="finger"/>: the finger, kept far enough from the screen edges that the ring stays on screen, and clear of a camera or rounded corner.</summary>
    public Vector2 FloatingOrigin(Vector2 finger)
    {
        var r = StickRadius;
        return FrontRoomsHandheld.ClearCircle(KeepInsideSafe(finger, r), r);
    }

    Vector2 KeepInsideSafe(Vector2 point, float radius) => new Vector2(
        ClampCenter(point.x, SafeLeft + radius, Frame.x - SafeRight - radius),
        ClampCenter(point.y, SafeBottom + radius, Frame.y - SafeTop - radius));

    static float ClampCenter(float value, float min, float max) => min <= max ? Mathf.Clamp(value, min, max) : (min + max) * .5f;

    // ----------------------------------------------------------------- menus
    public Vector2 Center => Frame * .5f;

    /// <summary>The phone master's 750 pt column whenever the safe area holds it (the signed Figma 2528:5403); narrower
    /// windows keep 8 pt each side.</summary>
    public float MenuWidth => Mathf.Min(750f, Mathf.Max(1f, SafeRect.width >= 750f ? SafeRect.width : SafeRect.width - 16f));
    public bool CompactMenus => MenuWidth < 700f;
    public float MenuVerticalScale => Mathf.Min(1f, Mathf.Max(.01f, (SafeRect.height - 24f) / (CompactMenus ? 384f : 344f)));
    public Vector2 MenuCenter => new Vector2(Frame.x * .5f,
        ClampCenter(Center.y, SafeBottom + 173f * MenuVerticalScale, Frame.y - SafeTop - 171f * MenuVerticalScale));

    /// <summary>Authored menu spacing in current-window points. Only vertical spacing compresses on short windows;
    /// controls and text keep their point size. Compact views reflow wide content separately.</summary>
    public Vector2 FromMaster(float x, float yFromTop) => MenuCenter + new Vector2(x - 437f, (201f - yFromTop) * MenuVerticalScale);

    // A compact legend can wrap to five lines. Reserve its space above the actions;
    // short wide windows keep both 44 pt button rows above the bottom safe inset: QUIT TO TITLE
    // hangs max(48, 56 × scale) under the row, and its hit area reaches 22 pt below its centre.
    public Vector2 PauseActionsCenter
    {
        get
        {
            var point = FromMaster(437f, CompactMenus ? 326f : 290f);
            point.y = Mathf.Max(point.y, SafeBottom + 22f + Mathf.Max(48f, 56f * MenuVerticalScale));
            return point;
        }
    }

    public const float ChipHeight = 40f;
    public const float ChipHitHeight = 44f;

    // The settings sheet (Figma: a 742 × 369 card at 66, 10 on the phone master). Red, 2026-10-24: one black frosted
    // sheet whose list runs full bleed to the bottom edge, its last row scrolling clear of the home indicator
    // (SettingsBottomInset); the heading keeps 10 pt under the safe top, as on the master.
    public Vector2 SettingsCardSize => new Vector2(Mathf.Min(742f, Mathf.Max(1f, Frame.x - 16f)), Mathf.Max(1f, Frame.y - SafeTop - 10f));
    public Vector2 SettingsCardCenter => new Vector2(Frame.x * .5f, SettingsCardSize.y * .5f);
    /// <summary>How far above the bottom edge the list's last row can scroll: clear of the home indicator.</summary>
    public float SettingsBottomInset => SafeBottom + 8f;
    public bool SettingsSingleColumn => SettingsCardSize.x < 700f;
    public const float SettingsRowHeight = 44f;
    public const float SettingsColumnWidth = 337f;

    public Rect SettingsColumn(int column)
    {
        var card = SettingsCardSize;
        var c = SettingsCardCenter;
        var width = SettingsSingleColumn ? card.x - 48f : (card.x - 68f) * .5f;
        var left = c.x - card.x * .5f + 24f + (SettingsSingleColumn ? 0 : column) * (width + 20f);
        var top = c.y + card.y * .5f - 52f;
        var bottom = c.y - card.y * .5f;
        return new Rect(left, bottom, Mathf.Max(1f, width), Mathf.Max(1f, top - bottom));
    }

    public static bool Contains(Vector2 center, float radius, Vector2 p) => (p - center).sqrMagnitude <= radius * radius;

    public static bool Contains(Vector2 center, Vector2 size, Vector2 p) =>
        Mathf.Abs(p.x - center.x) <= size.x * .5f && Mathf.Abs(p.y - center.y) <= size.y * .5f;
}
