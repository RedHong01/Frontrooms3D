using UnityEngine;

/// <summary>How the screen moves to the panel the machine just laid out (41 section 4.2a).</summary>
public enum TerminalMotionKind
{
    /// <summary>At once, as a 1990 PC wrote its text buffer: the bar, a line of scroll, a message, the clock.</summary>
    Cut,
    /// <summary>A framed box, its inside cleared, grows in steps from where the panel comes from to the panel's frame; the panel is
    /// written into it after the last step (the character-mode "exploding window").</summary>
    Explode,
    /// <summary>The reverse: the screen underneath is back around a framed box that shrinks in steps, the closing panel still inside
    /// it, onto where the panel came from.</summary>
    Implode,
    /// <summary>A new page is written in from the top, two rows a step (a page turn inside DOC or HELP).</summary>
    Paint,
}

/// <summary>A rectangle of cells on the 80 x 25 grid, every edge inclusive (0-based, as the grid).</summary>
public struct TerminalBox
{
    public int left, top, right, bottom;

    public TerminalBox(int left, int top, int right, int bottom)
    {
        this.left = left;
        this.top = top;
        this.right = right;
        this.bottom = bottom;
    }

    /// <summary>No cells (a panel without a frame: MAIN).</summary>
    public static readonly TerminalBox None = new TerminalBox(0, 0, -1, -1);

    public int Width => right - left + 1;
    public int Height => bottom - top + 1;
    public bool IsEmpty => right < left || bottom < top;

    /// <summary>A small box at this one's centre (three rows, an eighth of its width, at least three columns): where a box that no
    /// line opened starts, as a TurboPower window exploded from its own middle.</summary>
    public TerminalBox Centre
    {
        get
        {
            var w = Mathf.Max(3, Width / 8);
            var l = left + (Width - w) / 2;
            var t = top + (Height - 3) / 2;
            return new TerminalBox(l, t, l + w - 1, t + 2);
        }
    }

    /// <summary>The box a share t of the way from a to b, each edge on a whole cell (linear: the period's loops stepped evenly).</summary>
    public static TerminalBox Lerp(TerminalBox a, TerminalBox b, float t) => new TerminalBox(
        Mathf.RoundToInt(Mathf.Lerp(a.left, b.left, t)), Mathf.RoundToInt(Mathf.Lerp(a.top, b.top, t)),
        Mathf.RoundToInt(Mathf.Lerp(a.right, b.right, t)), Mathf.RoundToInt(Mathf.Lerp(a.bottom, b.bottom, t)));

    /// <summary>The longest way any edge moves between a and b, in cells.</summary>
    public static int Travel(TerminalBox a, TerminalBox b) => Mathf.Max(
        Mathf.Max(Mathf.Abs(a.left - b.left), Mathf.Abs(a.right - b.right)), Mathf.Max(Mathf.Abs(a.top - b.top), Mathf.Abs(a.bottom - b.bottom)));

    public override string ToString() => left + ".." + right + " x " + top + ".." + bottom;
}

/// <summary>One screen change's motion: the panel machine notes it, the screen plays it.</summary>
public struct TerminalMotion
{
    public TerminalMotionKind kind;
    /// <summary>Explode and Implode: the first and the last box. Paint: the cells written are inside to (from is unused).</summary>
    public TerminalBox from, to;

    public static TerminalMotion Explode(TerminalBox from, TerminalBox to) => new TerminalMotion { kind = TerminalMotionKind.Explode, from = from, to = to };
    public static TerminalMotion Implode(TerminalBox from, TerminalBox to) => new TerminalMotion { kind = TerminalMotionKind.Implode, from = from, to = to };
    public static TerminalMotion Paint(TerminalBox rows) => new TerminalMotion { kind = TerminalMotionKind.Paint, to = rows };
}

/// <summary>
/// Plays a terminal's screen change on the grid the tube shows (动画和动态图形模型技术, 2026-11-02; Red: "我需要你调研一下那个时候的
/// 图形操作系统是否有motion transition，如果有，我需要你优化这个电脑交互的部分"; 41 section 4.2a, research in 10_research.md "Period
/// motion"). Only the period's own moves, made of cells as a 1990 character-mode program made them:
///   EXPLODE / IMPLODE  a framed box (CP437 single lines in FIELD's colours) steps between the line that opened a panel and the
///                      panel's frame, one step a tick (<see cref="StepSeconds"/>), linear, every edge on a whole cell: one step for
///                      each cell of the longest edge's travel, at most <see cref="MaxSteps"/> (the Finder's zoom drew 14). Exploding,
///                      its inside is cleared and the panel is written into it after the last step (TurboPower's ExplodeFrame);
///                      imploding, the screen underneath is back around it at once and the closing panel stays inside it, cut down
///                      step by step (ImplodeFrame restored the covered strips the same way).
///   PAINT              a new page is written in from the top, <see cref="PaintRowsPerStep"/> rows a tick.
/// No fades, no easing, nothing between cells, nothing blinks. Every move is over in 0.24 s at most, inside the machine's 0.34 s
/// window, so the next change never starts on top of it and what the player does still changes the screen at most three times a
/// second. Camera motion Off (the game's reduce-motion setting) and the CPU raster make every move a cut. Unscaled time
/// (FrontRoomsInspect.Dt, as the machine); no sound (TerminalKey is unchanged). Pure C#: the screen hands it the grids.
/// </summary>
public sealed class FrontRoomsTerminalMotion
{
    /// <summary>One step: a tick (the Finder zoom's Delay of one tick; TurboPower's ExplodeDelay was 15 ms, about one 70 Hz VGA refresh).</summary>
    public const float StepSeconds = 1f / 60f;
    /// <summary>The most steps an Explode or Implode takes (the Finder's ZoomRect drew 14); a longer travel moves an edge more than a cell a step.</summary>
    public const int MaxSteps = 14;
    /// <summary>Rows a Paint writes per step (DOC's 20 page rows in 10 steps, 0.17 s).</summary>
    public const int PaintRowsPerStep = 2;
    const float Slack = 1e-3f;

    readonly FrontRoomsTerminalGrid before;
    TerminalMotion motion;
    int steps;
    float age;
    bool running, fresh;

    public FrontRoomsTerminalMotion(FrontRoomsTerminalFace face) { before = new FrontRoomsTerminalGrid(face); }

    /// <summary>Moves play (Camera motion Off makes every one a cut, as it does for the game's shots).</summary>
    public static bool Enabled => FrontRoomsSettings.CameraMotionPercent > 0;

    public bool Running => running;
    public TerminalMotionKind Kind => running ? motion.kind : TerminalMotionKind.Cut;
    public float Age => age;
    /// <summary>The step on the tube now (1-based; 0 when nothing moves).</summary>
    public int Step => running ? StepAt(age) : 0;
    public int Steps => running ? steps : 0;

    /// <summary>Steps a move takes: a box move one per cell of its longest edge's travel (1 .. MaxSteps), a paint one per two rows.</summary>
    public static int StepsOf(in TerminalMotion m)
    {
        switch (m.kind)
        {
            case TerminalMotionKind.Explode:
            case TerminalMotionKind.Implode:
                return Mathf.Clamp(TerminalBox.Travel(m.from, m.to), 1, MaxSteps);
            case TerminalMotionKind.Paint:
                return Mathf.Max(1, (m.to.Height + PaintRowsPerStep - 1) / PaintRowsPerStep);
            default:
                return 0;
        }
    }

    /// <summary>How long a move lasts (s): its last step shows for one tick.</summary>
    public static float DurationOf(in TerminalMotion m) => StepsOf(m) * StepSeconds;

    /// <summary>A move starts from what the tube shows now (a step of another move included); a cut or an empty box stops any move.</summary>
    public void Begin(FrontRoomsTerminalGrid shown, in TerminalMotion m)
    {
        if (m.kind == TerminalMotionKind.Cut || m.to.IsEmpty || (m.kind != TerminalMotionKind.Paint && m.from.IsEmpty))
        {
            running = false;
            return;
        }
        before.Face = shown.Face;
        before.CopyFrom(shown);
        motion = m;
        steps = StepsOf(m);
        age = 0f;
        running = true;
        fresh = true;
    }

    public void Stop() => running = false;

    /// <summary>Time (unscaled). The frame a move begins in shows its first step, whatever that frame's length.</summary>
    public void Advance(float dt)
    {
        if (!running) return;
        if (fresh) { fresh = false; return; }
        age += Mathf.Max(0f, dt);
        if (age >= steps * StepSeconds - Slack) running = false;
    }

    /// <summary>The tube's grid for now: the target itself, or the current step of the move between the grid before it and the target.</summary>
    public void Compose(FrontRoomsTerminalGrid target, FrontRoomsTerminalGrid shown)
    {
        shown.Face = target.Face;
        if (!running)
        {
            shown.CopyFrom(target);
            return;
        }
        var k = StepAt(age);
        switch (motion.kind)
        {
            case TerminalMotionKind.Explode:
                // the old screen stays until the box is full size; the new panel is written into it after the last step
                shown.CopyFrom(before);
                DrawBox(shown, BoxAt(k), null);
                break;
            case TerminalMotionKind.Implode:
                // the screen underneath is back at once; the closing panel is cut down inside the box until the box has gone
                shown.CopyFrom(target);
                DrawBox(shown, BoxAt(k), before);
                break;
            case TerminalMotionKind.Paint:
                shown.CopyFrom(target);
                var rows = motion.to;
                var written = Mathf.Min(rows.Height, k * PaintRowsPerStep);
                for (var r = Mathf.Max(0, rows.top + written); r <= Mathf.Min(FrontRoomsTerminalGrid.Rows - 1, rows.bottom); r++)
                    for (var c = Mathf.Max(0, rows.left); c <= Mathf.Min(FrontRoomsTerminalGrid.Cols - 1, rows.right); c++)
                        shown.Set(c, r, before.Glyph(c, r), before.Attr(c, r));
                break;
        }
    }

    int StepAt(float t) => Mathf.Clamp(1 + Mathf.FloorToInt(t / StepSeconds + Slack), 1, Mathf.Max(1, steps));

    TerminalBox BoxAt(int k) => TerminalBox.Lerp(motion.from, motion.to, (float)k / steps);

    /// <summary>A box as the program draws its own (FrontRoomsTerminal.Box): single lines in FIELD's colours, its inside cleared to
    /// the field, or (inside != null) holding that grid's cells.</summary>
    static void DrawBox(FrontRoomsTerminalGrid g, TerminalBox b, FrontRoomsTerminalGrid inside)
    {
        b.left = Mathf.Max(0, b.left);
        b.top = Mathf.Max(0, b.top);
        b.right = Mathf.Min(FrontRoomsTerminalGrid.Cols - 1, b.right);
        b.bottom = Mathf.Min(FrontRoomsTerminalGrid.Rows - 1, b.bottom);
        if (b.Width < 2 || b.Height < 2) return;
        var a = FrontRoomsTerminalPalette.Field;
        if (inside == null) g.Fill(b.left + 1, b.top + 1, b.Width - 2, b.Height - 2, 0x20, a);
        else
            for (var r = b.top + 1; r < b.bottom; r++)
                for (var c = b.left + 1; c < b.right; c++)
                    g.Set(c, r, inside.Glyph(c, r), inside.Attr(c, r));
        g.Set(b.left, b.top, 0xDA, a); g.Fill(b.left + 1, b.top, b.Width - 2, 1, 0xC4, a); g.Set(b.right, b.top, 0xBF, a);
        for (var r = b.top + 1; r < b.bottom; r++) { g.Set(b.left, r, 0xB3, a); g.Set(b.right, r, 0xB3, a); }
        g.Set(b.left, b.bottom, 0xC0, a); g.Fill(b.left + 1, b.bottom, b.Width - 2, 1, 0xC4, a); g.Set(b.right, b.bottom, 0xD9, a);
    }
}
