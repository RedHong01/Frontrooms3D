using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>The four panels of OFFICE MEMO (41 section 3.3).</summary>
public enum TerminalPanel { Main, Doc, List, Help }

/// <summary>The actions row 25 and HELP name (41 section 5.2). The input stage names each one for the player's device.</summary>
public enum TerminalAction { Move, Side, Select, Page, Cancel, Exit, Help }

/// <summary>The v1 refusals (41 section 4.3). Each raises TerminalRefused once; sound design decides what it sounds like (O-1).</summary>
public enum TerminalRefusal { NotAuthorized, TopOfData, BottomOfData }

/// <summary>One step of the verb set MOVE / SELECT / CANCEL / EXIT / HELP / PAGE (41 section 5.1), as the panels read it.</summary>
public enum TerminalVerb { None, Up, Down, Prev, Next, PageUp, PageDown, First, Last, Select, Cancel, Exit, Help, Tap }

/// <summary>
/// The verbs as HELD LEVELS for one frame (the input stage fills it from keys, pad and wheel; nothing here reads a
/// device). The panel machine finds the edges, repeats held movement (0.5 s, then every 0.34 s) and latches anything held
/// when the close-up opens. One-shot inputs (a wheel notch, a soft key, a swipe) go to Press; a touch goes to Tap.
/// </summary>
public struct TerminalInput
{
    /// <summary>-1 up, +1 down (arrows, the MOVE keys W / S, d-pad, stick).</summary>
    public int Move;
    /// <summary>-1 / +1 left / right (arrows, A / D): previous / next item on MAIN and LIST, screen on DOC and HELP.</summary>
    public int Side;
    /// <summary>-1 PgUp, +1 PgDn (LB / RB).</summary>
    public int Page;
    /// <summary>-1 Home, +1 End.</summary>
    public int Jump;
    public bool Select, Cancel, Exit, Help;
}

/// <summary>The name of the player's control for an action, as their device prints it (null = no such control: the item is left out).</summary>
public delegate string TerminalNamer(TerminalAction action);

/// <summary>
/// OFFICE MEMO's panel machine (game visual, 2026-10-30; spec research/computer_ui/41_c19_period_interaction.md sections
/// 3-5): MAIN / DOC / LIST / HELP, the lightbar, paging (PgDn at the end of a memo opens the next one), refusals, the
/// photosafe timing (at most one screen change per 0.34 s; a press inside the window is held and applied when it ends,
/// further presses are dropped; held movement repeats after 0.5 s, then every 0.34 s; nothing acts for 0.20 s after the
/// close-up opens; keys held at open are latched until released). It lays every panel out on the 80 x 25 grid; the
/// screen component renders the grid. Pure C#: no device input, no sound (events only), unscaled time from the caller.
/// Strings come from <see cref="FrontRoomsTerminalContent"/>; colours from <see cref="FrontRoomsTerminalPalette"/>.
/// </summary>
public sealed class FrontRoomsTerminal
{
    public const float MinInterval = .34f, RepeatDelay = .5f, RepeatInterval = .34f, OpenLockout = .20f;
    /// <summary>Rows, 0-based (the spec's rows 1, 3, 4-22, 24, 25). Row 23 (0-based 22) is a spacer on every panel, as row 2
    /// is under the title: nothing in the body touches the message line (A2, 2026-10-30: MAIN's last item sat right above it).</summary>
    public const int TitleRow = 0, HeadingRow = 2, BodyTop = 3, BodyBottom = 21, MessageRow = 23, FooterRow = 24;
    public const int BodyRows = BodyBottom - BodyTop + 1;
    const int BarLeft = 10, BarWidth = 60, MarkCol = 11, ItemCol = 13, ItemRight = 67, ItemsTop = 16;
    const int TextCol = 2, ValueCol = 9, RuleWidth = 76, MessageWidth = 77;
    // LIST: the box (top rule, header, rule, jobs, bottom rule) ends on BodyBottom at most
    const int BoxLeft = 2, BoxRight = 77, TableTop = 4, MaxJobs = BodyBottom - TableTop - 4;
    static readonly int[] ListCols = { 4, 10, 44, 54, 62 };
    static readonly int[] ListWidths = { 5, 33, 9, 7, 15 };
    const int HelpRight = 59, HelpKeyCol = 4, HelpKeyWidth = 11, HelpTextCol = 16, HelpView = BodyBottom - TableTop - 2;

    static readonly TerminalAction[][] KeyRow =
    {
        new[] { TerminalAction.Help, TerminalAction.Select, TerminalAction.Exit },
        new[] { TerminalAction.Help, TerminalAction.Cancel, TerminalAction.Exit, TerminalAction.Page },
        new[] { TerminalAction.Help, TerminalAction.Select, TerminalAction.Cancel, TerminalAction.Exit },
        new[] { TerminalAction.Cancel, TerminalAction.Exit },
    };
    static readonly TerminalAction[][] SoftRow =
    {
        new[] { TerminalAction.Select, TerminalAction.Help },
        new[] { TerminalAction.Cancel, TerminalAction.Help },
        new[] { TerminalAction.Select, TerminalAction.Cancel, TerminalAction.Help },
        new[] { TerminalAction.Cancel },
    };

    struct Channel { public int level; public bool latched; public float next; }
    struct SoftKey { public int from, to; public TerminalVerb verb; }
    enum DocKind { Rule, Field, Text, Blank }
    struct DocLine { public DocKind kind; public string label, text; }
    sealed class RunMemory { public bool[] read; public int lastItem; }

    // UI state that outlives a close-up and a chunk: per content key, per run (41 section 4.4; 20_design section 4.1)
    static readonly Dictionary<int, RunMemory> runs = new Dictionary<int, RunMemory>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => runs.Clear();

    /// <summary>A new run: every unread mark and last-item memory resets (the map line calls it with the run's start).</summary>
    public static void ResetRun() => runs.Clear();

    readonly FrontRoomsTerminalContent content;
    readonly FrontRoomsTerminalGrid grid, scratch;
    readonly RunMemory memory;
    readonly List<string> transcript = new List<string>();
    readonly List<DocLine> doc = new List<DocLine>();
    readonly List<SoftKey> softKeys = new List<SoftKey>();
    readonly List<int> helpRows = new List<int>();
    readonly List<string> wrapped = new List<string>();

    TerminalPanel panel;
    int bar, listBar, memo, scroll, helpScroll;
    TerminalPanel helpFrom;
    bool hasRefusal;
    TerminalRefusal refusal;
    bool open, wantsExit, baseline;
    float now, lastChange = -100f, lockUntil;
    bool hasPending;
    TerminalVerb pending;
    int pendingCol, pendingRow;
    Channel move, side, page;
    int jumpLevel;
    bool selectHeld, cancelHeld, exitHeld, helpHeld;
    TerminalNamer namer;
    bool softKeyMode;
    string transcriptText;
    int layoutExtra;

    /// <summary>Raised for each refused request (41 section 4.3).</summary>
    public event Action<TerminalRefusal> Refused;
    /// <summary>Raised for each accepted press that changed the screen or left (sound design's TerminalKey, 41 section 4.5).</summary>
    public event Action KeyAccepted;
    /// <summary>Raised when the panel changes (silent in v1).</summary>
    public event Action<TerminalPanel> PanelChanged;

    public FrontRoomsTerminal(FrontRoomsTerminalContent content, FrontRoomsTerminalGrid grid, int contentKey)
    {
        this.content = content ?? new FrontRoomsTerminalContent();
        this.grid = grid;
        scratch = new FrontRoomsTerminalGrid(grid.Face);
        var n = this.content.memos.Length;
        if (!runs.TryGetValue(contentKey, out memory) || memory == null || memory.read == null || memory.read.Length != n)
        {
            memory = new RunMemory { read = new bool[n], lastItem = 0 };
            runs[contentKey] = memory;
        }
        bar = ClampItem(memory.lastItem);
        Layout();
    }

    // ---------------------------------------------------------------- state (read-only)

    public TerminalPanel Panel => panel;
    /// <summary>The highlighted MAIN item (memos first, then the print queue, then Help).</summary>
    public int Bar => bar;
    public int ListBar => listBar;
    public int Memo => memo;
    public int Scroll => scroll;
    public int MaxScroll => Mathf.Max(0, doc.Count - BodyRows);
    public int HelpScroll => helpScroll;
    public int ItemCount => content.memos.Length + 2;
    /// <summary>MAIN: the row of item i (0-based). The items sit on ItemsTop, two rows apart (one touch hit = two rows), and move
    /// up as a block when the last one would pass BodyBottom; the second line moves up with them (LayoutMain).</summary>
    public int ItemRow(int i) => ItemsTopRow + i * ItemSpacing;
    int ItemsTopRow => Mathf.Max(BodyTop, Mathf.Min(ItemsTop, BodyBottom - (ItemCount - 1) * ItemSpacing));
    public int Unread { get { var u = 0; foreach (var r in memory.read) if (!r) u++; return u; } }
    public bool IsOpen => open;
    /// <summary>The player asked to leave (EXIT, or CANCEL on MAIN); the close-up reads it and calls <see cref="Close"/>.</summary>
    public bool WantsExit => wantsExit;
    public bool HasRefusal => hasRefusal;
    public TerminalRefusal Refusal => refusal;
    public bool HasPending => hasPending;
    /// <summary>The machine's own unscaled clock (s) and the time of the last screen change.</summary>
    public float ClockSeconds => now;
    public float LastChange => lastChange;
    public FrontRoomsTerminalContent Content => content;
    /// <summary>Glyphs missing from the face and strings clipped in the last layout (41 AT-C7: both 0).</summary>
    public int LayoutMissing { get; private set; }
    public int LayoutClipped { get; private set; }
    /// <summary>Row 25 as text (the footer for the current input mode).</summary>
    public string Footer => grid.RowText(FooterRow);
    /// <summary>The current panel's lines in reading order (the desktop TEXT transcript, C24).</summary>
    public string PanelTranscript => transcriptText ?? (transcriptText = string.Join("\n", transcript));

    /// <summary>Names the player's controls in row 25 and HELP (null = the period keys of a PC keyboard). Re-lays the panel.</summary>
    public TerminalNamer Namer { get => namer; set { namer = value; Layout(); } }

    /// <summary>Touch: row 25 is CUA's short form, boxed soft keys (41 section 5.2). Re-lays the panel.</summary>
    public bool SoftKeys { get => softKeyMode; set { if (softKeyMode == value) return; softKeyMode = value; Layout(); } }

    /// <summary>The default names: a Windows / Linux PC keyboard (41 section 5.2, first row).</summary>
    public static string PeriodKeys(TerminalAction a)
    {
        switch (a)
        {
            case TerminalAction.Move: return "\u2191 \u2193";
            case TerminalAction.Side: return "\u2190 \u2192";
            case TerminalAction.Select: return "Enter";
            case TerminalAction.Page: return "PgUp/PgDn";
            case TerminalAction.Cancel: return "Esc";
            case TerminalAction.Exit: return "F3";
            case TerminalAction.Help: return "F1";
            default: return null;
        }
    }

    string Name(TerminalAction a) => namer != null ? namer(a) : PeriodKeys(a);

    // ---------------------------------------------------------------- open / close

    /// <summary>The close-up opens: MAIN, the bar on the last item opened this run (41 section 4.4); nothing acts for lockout seconds.</summary>
    public void Open(float lockout = OpenLockout)
    {
        open = true;
        wantsExit = false;
        hasPending = false;
        hasRefusal = false;
        SetPanel(TerminalPanel.Main);
        bar = ClampItem(memory.lastItem);
        scroll = 0;
        lockUntil = now + Mathf.Max(0f, lockout);
        baseline = true;
        Layout();
    }

    /// <summary>The close-up ended: back to MAIN, which the glance shows.</summary>
    public void Close()
    {
        open = false;
        wantsExit = false;
        hasPending = false;
        hasRefusal = false;
        SetPanel(TerminalPanel.Main);
        bar = ClampItem(memory.lastItem);
        Layout();
    }

    // ---------------------------------------------------------------- input

    /// <summary>One frame of held levels (see <see cref="TerminalInput"/>), on unscaled time.</summary>
    public void Apply(in TerminalInput k, float unscaledDt)
    {
        now += Mathf.Max(0f, unscaledDt);
        if (!open) return;
        if (baseline)
        {
            // C25: whatever is held as the close-up opens never acts; movement stays latched until it is released
            baseline = false;
            move = new Channel { level = Math.Sign(k.Move), latched = k.Move != 0 };
            side = new Channel { level = Math.Sign(k.Side), latched = k.Side != 0 };
            page = new Channel { level = Math.Sign(k.Page), latched = k.Page != 0 };
            jumpLevel = Math.Sign(k.Jump);
            selectHeld = k.Select; cancelHeld = k.Cancel; exitHeld = k.Exit; helpHeld = k.Help;
            ApplyPending();
            return;
        }
        if (k.Exit && !exitHeld) Fire(TerminalVerb.Exit, -1, -1);
        if (k.Cancel && !cancelHeld) Fire(TerminalVerb.Cancel, -1, -1);
        if (k.Select && !selectHeld) Fire(TerminalVerb.Select, -1, -1);
        if (k.Help && !helpHeld) Fire(TerminalVerb.Help, -1, -1);
        exitHeld = k.Exit; cancelHeld = k.Cancel; selectHeld = k.Select; helpHeld = k.Help;
        Step(ref move, Math.Sign(k.Move), TerminalVerb.Up, TerminalVerb.Down);
        Step(ref side, Math.Sign(k.Side), TerminalVerb.Prev, TerminalVerb.Next);
        Step(ref page, Math.Sign(k.Page), TerminalVerb.PageUp, TerminalVerb.PageDown);
        var j = Math.Sign(k.Jump);
        if (j != jumpLevel)
        {
            jumpLevel = j;
            if (j != 0) Fire(j < 0 ? TerminalVerb.First : TerminalVerb.Last, -1, -1);
        }
        ApplyPending();
    }

    /// <summary>Time only (a frame with no input): a held press is applied once its window ends.</summary>
    public void Tick(float unscaledDt)
    {
        now += Mathf.Max(0f, unscaledDt);
        if (open) ApplyPending();
    }

    /// <summary>A one-shot verb: a wheel notch, a soft key, a swipe. Goes through the same 0.34 s window.</summary>
    public void Press(TerminalVerb v)
    {
        if (open && v != TerminalVerb.None && v != TerminalVerb.Tap) Fire(v, -1, -1);
    }

    /// <summary>
    /// A touch on cell (col, row) of the 80 x 25 grid (41 section 5.4): a soft key in row 25 (its hit area covers row 24
    /// too) presses it; on MAIN and LIST a tap on an item moves the bar there and a tap on the lit item opens it (the
    /// two-tap rule). Anything else does nothing.
    /// </summary>
    public void Tap(int col, int row)
    {
        if (open) Fire(TerminalVerb.Tap, col, row);
    }

    void Step(ref Channel ch, int level, TerminalVerb neg, TerminalVerb pos)
    {
        if (level != ch.level)
        {
            ch.level = level;
            ch.latched = false;
            if (level != 0)
            {
                Fire(level < 0 ? neg : pos, -1, -1);
                ch.next = now + RepeatDelay;
            }
        }
        else if (level != 0 && !ch.latched && now >= ch.next)
        {
            Fire(level < 0 ? neg : pos, -1, -1);
            ch.next = now + RepeatInterval;
        }
    }

    void Fire(TerminalVerb v, int col, int row)
    {
        if (now < lockUntil || wantsExit) return;
        if (v == TerminalVerb.Exit || (v == TerminalVerb.Cancel && panel == TerminalPanel.Main && !hasPending))
        {
            // leaving is never held back: it is not a screen change
            wantsExit = true;
            KeyAccepted?.Invoke();
            return;
        }
        if (hasPending) return;                       // one press is held; further presses are dropped
        if (now - lastChange >= MinInterval) Do(v, col, row);
        else { hasPending = true; pending = v; pendingCol = col; pendingRow = row; }
    }

    void ApplyPending()
    {
        if (!hasPending || now - lastChange < MinInterval) return;
        hasPending = false;
        if (pending == TerminalVerb.Cancel && panel == TerminalPanel.Main) { wantsExit = true; KeyAccepted?.Invoke(); return; }
        Do(pending, pendingCol, pendingRow);
    }

    void Do(TerminalVerb v, int col, int row)
    {
        var before = grid.Version;
        if (v == TerminalVerb.Tap) ActTap(col, row); else Act(v);
        if (grid.Version != before)
        {
            lastChange = now;
            KeyAccepted?.Invoke();
        }
    }

    // ---------------------------------------------------------------- the panels' keys (41 section 4.1)

    void Act(TerminalVerb v)
    {
        var changed = false;
        var refused = false;
        var kind = TerminalRefusal.NotAuthorized;
        switch (panel)
        {
            case TerminalPanel.Main:
                switch (v)
                {
                    case TerminalVerb.Up: case TerminalVerb.Prev: changed = SetBar(bar - 1); break;
                    case TerminalVerb.Down: case TerminalVerb.Next: changed = SetBar(bar + 1); break;
                    case TerminalVerb.PageUp: case TerminalVerb.First: changed = SetBar(0); break;
                    case TerminalVerb.PageDown: case TerminalVerb.Last: changed = SetBar(ItemCount - 1); break;
                    case TerminalVerb.Select: OpenItem(bar); changed = true; break;
                    case TerminalVerb.Help: OpenHelp(); changed = true; break;
                }
                break;
            case TerminalPanel.Doc:
                switch (v)
                {
                    case TerminalVerb.Up: changed = SetScroll(scroll - 1); break;
                    case TerminalVerb.Down: changed = SetScroll(scroll + 1); break;
                    case TerminalVerb.Prev:
                    case TerminalVerb.PageUp:
                        if (scroll > 0) changed = SetScroll(scroll - BodyRows);
                        else if (memo > 0) { OpenMemo(memo - 1, true); changed = true; }
                        else { refused = true; kind = TerminalRefusal.TopOfData; }
                        break;
                    case TerminalVerb.Next:
                    case TerminalVerb.PageDown:
                        if (scroll < MaxScroll) changed = SetScroll(scroll + BodyRows);
                        else if (memo < content.memos.Length - 1) { OpenMemo(memo + 1, false); changed = true; }
                        else { refused = true; kind = TerminalRefusal.BottomOfData; }
                        break;
                    case TerminalVerb.First: changed = SetScroll(0); break;
                    case TerminalVerb.Last: changed = SetScroll(MaxScroll); break;
                    case TerminalVerb.Cancel: ToMain(); changed = true; break;
                    case TerminalVerb.Help: OpenHelp(); changed = true; break;
                }
                break;
            case TerminalPanel.List:
                var jobs = Mathf.Min(content.list.jobs.Length, MaxJobs);
                switch (v)
                {
                    case TerminalVerb.Up: case TerminalVerb.Prev: changed = SetListBar(listBar - 1); break;
                    case TerminalVerb.Down: case TerminalVerb.Next: changed = SetListBar(listBar + 1); break;
                    case TerminalVerb.PageUp: case TerminalVerb.First: changed = SetListBar(0); break;
                    case TerminalVerb.PageDown: case TerminalVerb.Last: changed = SetListBar(jobs - 1); break;
                    case TerminalVerb.Select: if (jobs > 0) { refused = true; kind = TerminalRefusal.NotAuthorized; } break;
                    case TerminalVerb.Cancel: ToMain(); changed = true; break;
                    case TerminalVerb.Help: OpenHelp(); changed = true; break;
                }
                break;
            case TerminalPanel.Help:
                switch (v)
                {
                    case TerminalVerb.Up: changed = SetHelpScroll(helpScroll - 1); break;
                    case TerminalVerb.Down: changed = SetHelpScroll(helpScroll + 1); break;
                    case TerminalVerb.Prev: case TerminalVerb.PageUp: changed = SetHelpScroll(helpScroll - HelpView); break;
                    case TerminalVerb.Next: case TerminalVerb.PageDown: changed = SetHelpScroll(helpScroll + HelpView); break;
                    case TerminalVerb.First: changed = SetHelpScroll(0); break;
                    case TerminalVerb.Last: changed = SetHelpScroll(int.MaxValue / 2); break;
                    case TerminalVerb.Cancel: SetPanel(helpFrom); changed = true; break;
                }
                break;
        }
        if (changed) hasRefusal = false;              // the message line describes the item again
        if (refused) { hasRefusal = true; refusal = kind; }
        if (changed || refused) Layout();
        if (refused) Refused?.Invoke(kind);
    }

    void ActTap(int col, int row)
    {
        if (softKeyMode && (row == FooterRow || row == MessageRow))
        {
            foreach (var k in softKeys)
                if (col >= k.from && col <= k.to)
                {
                    if (k.verb == TerminalVerb.Cancel && panel == TerminalPanel.Main) { wantsExit = true; KeyAccepted?.Invoke(); return; }
                    Act(k.verb);
                    return;
                }
            return;
        }
        if (panel == TerminalPanel.Main)
        {
            var spacing = ItemSpacing;
            for (var i = 0; i < ItemCount; i++)
            {
                var r0 = ItemRow(i);
                if (row != r0 && !(spacing == 2 && row == r0 + 1)) continue;
                if (col < BarLeft || col >= BarLeft + BarWidth) return;
                if (i == bar) Act(TerminalVerb.Select);
                else if (SetBar(i)) { hasRefusal = false; Layout(); }
                return;
            }
        }
        else if (panel == TerminalPanel.List)
        {
            var j = row - (TableTop + 3);
            if (j < 0 || j >= Mathf.Min(content.list.jobs.Length, MaxJobs) || col <= BoxLeft || col >= BoxRight) return;
            if (j == listBar) Act(TerminalVerb.Select);
            else if (SetListBar(j)) { hasRefusal = false; Layout(); }
        }
    }

    int ItemSpacing => ItemCount <= 4 ? 2 : 1;
    int ClampItem(int i) => Mathf.Clamp(i, 0, ItemCount - 1);

    bool SetBar(int i)
    {
        i = ClampItem(i);
        if (i == bar) return false;
        bar = i;
        return true;
    }

    bool SetListBar(int i)
    {
        var jobs = Mathf.Min(content.list.jobs.Length, MaxJobs);
        i = Mathf.Clamp(i, 0, Mathf.Max(0, jobs - 1));
        if (i == listBar) return false;
        listBar = i;
        return true;
    }

    bool SetScroll(int s)
    {
        s = Mathf.Clamp(s, 0, MaxScroll);
        if (s == scroll) return false;
        scroll = s;
        return true;
    }

    bool SetHelpScroll(int s)
    {
        s = Mathf.Clamp(s, 0, Mathf.Max(0, helpRows.Count - HelpView));
        if (s == helpScroll) return false;
        helpScroll = s;
        return true;
    }

    void SetPanel(TerminalPanel p)
    {
        if (p == panel) return;
        panel = p;
        PanelChanged?.Invoke(p);
    }

    void OpenItem(int i)
    {
        memory.lastItem = i;
        var memos = content.memos.Length;
        if (i < memos) OpenMemo(i, false);
        else if (i == memos) { listBar = 0; SetPanel(TerminalPanel.List); }
        else OpenHelp();
    }

    void OpenMemo(int m, bool atEnd)
    {
        memo = m;
        memory.read[m] = true;
        memory.lastItem = m;
        SetPanel(TerminalPanel.Doc);
        BuildDoc();
        scroll = atEnd ? MaxScroll : 0;
    }

    void OpenHelp()
    {
        if (panel == TerminalPanel.Help) return;
        helpFrom = panel;
        helpScroll = 0;
        SetPanel(TerminalPanel.Help);
    }

    void ToMain()
    {
        SetPanel(TerminalPanel.Main);
        bar = ClampItem(memory.lastItem);
    }

    // ---------------------------------------------------------------- layout

    /// <summary>Lay the current panel out again (the clock's minute, a face swap, a new namer). Only real changes reach the grid.</summary>
    public void Refresh() => Layout();

    void Layout()
    {
        var g = scratch;
        g.Face = grid.Face;
        g.ResetChecks();
        layoutExtra = 0;
        g.Clear(FrontRoomsTerminalPalette.Field);
        transcript.Clear();
        transcriptText = null;
        TitleLine(g);
        switch (panel)
        {
            case TerminalPanel.Main: LayoutMain(g); break;
            case TerminalPanel.Doc: LayoutDoc(g); break;
            case TerminalPanel.List: LayoutList(g); break;
            case TerminalPanel.Help: LayoutHelp(g); break;
        }
        var message = hasRefusal ? RefusalText(refusal) : PanelMessage();
        if (!string.IsNullOrEmpty(message))
        {
            g.Put(TextCol, MessageRow, message, FrontRoomsTerminalPalette.Bright, MessageWidth);
            T(message);
        }
        FooterLine(g);
        LayoutMissing = g.Missing;
        LayoutClipped = g.Clipped + layoutExtra;
        grid.CopyFrom(g);
    }

    void T(string s)
    {
        if (!string.IsNullOrEmpty(s)) transcript.Add(s.Trim());
    }

    string X(string s, int n = 0, int m = 0) => content.Expand(s, Unread, n, m);

    string Title()
    {
        switch (panel)
        {
            case TerminalPanel.Doc: return content.titles.doc;
            case TerminalPanel.List: return content.titles.list;
            case TerminalPanel.Help: return content.titles.help;
            default: return content.titles.main;
        }
    }

    void TitleLine(FrontRoomsTerminalGrid g)
    {
        var a = FrontRoomsTerminalPalette.Title;
        g.Fill(0, TitleRow, FrontRoomsTerminalGrid.Cols, 1, ' ', a);
        var program = X(content.program);
        var title = X(Title());
        var clock = FrontRoomsTerminalContent.Clock(FrontRoomsTerminalContent.Now);
        g.Put(1, TitleRow, program, a);
        g.PutCentred(TitleRow, title, a);
        g.PutRight(78, TitleRow, clock, a);
        T(program + "  " + title + "  " + clock);
    }

    void LayoutMain(FrontRoomsTerminalGrid g)
    {
        var face = g.Face;
        var headline = X(content.main.headline);
        var unread = Unread;
        var second = unread > 0 ? X(content.main.second) : X(content.main.secondNone);
        if (face != null)
        {
            // the second line keeps one blank row above the first item; it gives up its gap under the headline first
            var headBottom = HeadingRow + Mathf.Max(face.Headline.Height, 1);
            var secondTop = Mathf.Max(headBottom, Mathf.Min(headBottom + 1, ItemsTopRow - 1 - Mathf.Max(face.Second.Height, 1)));
            g.BlockText(headline, face.Headline, HeadingRow, FrontRoomsTerminalPalette.Bright);
            g.BlockText(second, face.Second, secondTop, FrontRoomsTerminalPalette.Field);
        }
        T(headline);
        T(second);
        for (var i = 0; i < ItemCount; i++)
        {
            var row = ItemRow(i);
            if (row > BodyBottom) { layoutExtra++; break; }
            string label, right = "";
            var mark = false;
            if (i < content.memos.Length)
            {
                var m = content.memos[i];
                label = X(m.index);
                right = X(m.date);
                mark = !memory.read[i];
            }
            else if (i == content.memos.Length) label = X(content.main.listItem);
            else label = X(content.main.helpItem);
            var a = i == bar ? FrontRoomsTerminalPalette.Bar : FrontRoomsTerminalPalette.Field;
            if (i == bar) g.Fill(BarLeft, row, BarWidth, 1, ' ', a);
            if (mark) g.Set(MarkCol, row, 0xFE, a);
            var room = (string.IsNullOrEmpty(right) ? ItemRight + 1 : ItemRight - right.Length) - ItemCol;
            g.Put(ItemCol, row, label, a, Mathf.Max(0, room));
            if (!string.IsNullOrEmpty(right)) g.PutRight(ItemRight, row, right, a);
            T(string.IsNullOrEmpty(right) ? label : label + "  " + right);
        }
    }

    void BuildDoc()
    {
        doc.Clear();
        if (content.memos.Length == 0) return;
        var m = content.memos[Mathf.Clamp(memo, 0, content.memos.Length - 1)];
        doc.Add(new DocLine { kind = DocKind.Rule });
        foreach (var f in m.head) doc.Add(new DocLine { kind = DocKind.Field, label = X(f.label), text = X(f.value) });
        doc.Add(new DocLine { kind = DocKind.Rule });
        doc.Add(new DocLine { kind = DocKind.Blank });
        foreach (var para in m.body)
        {
            if (string.IsNullOrEmpty(para)) { doc.Add(new DocLine { kind = DocKind.Blank }); continue; }
            wrapped.Clear();
            Wrap(X(para), RuleWidth, wrapped);
            foreach (var w in wrapped) doc.Add(new DocLine { kind = DocKind.Text, text = w });
        }
        if (!string.IsNullOrEmpty(m.sign))
        {
            doc.Add(new DocLine { kind = DocKind.Blank });
            doc.Add(new DocLine { kind = DocKind.Text, text = X(m.sign) });
        }
    }

    static void Wrap(string s, int width, List<string> into)
    {
        var start = 0;
        while (start < s.Length)
        {
            if (s.Length - start <= width) { into.Add(s.Substring(start)); break; }
            var cut = s.LastIndexOf(' ', start + width, width + 1);
            if (cut <= start) cut = start + width;
            into.Add(s.Substring(start, cut - start).TrimEnd());
            start = cut;
            while (start < s.Length && s[start] == ' ') start++;
        }
    }

    void LayoutDoc(FrontRoomsTerminalGrid g)
    {
        BuildDoc();
        scroll = Mathf.Clamp(scroll, 0, MaxScroll);
        var heading = X(content.words.memoOf, memo + 1, content.memos.Length);
        g.Put(TextCol, HeadingRow, heading, FrontRoomsTerminalPalette.Bright);
        T(heading);
        var more = MoreText(scroll > 0, scroll < MaxScroll);
        if (more != null) g.PutRight(78, HeadingRow, more, FrontRoomsTerminalPalette.Bright);
        for (var r = 0; r < BodyRows; r++)
        {
            var k = scroll + r;
            if (k >= doc.Count) break;
            var line = doc[k];
            var row = BodyTop + r;
            switch (line.kind)
            {
                case DocKind.Rule: g.Fill(TextCol, row, RuleWidth, 1, 0xC4, FrontRoomsTerminalPalette.Field); break;
                case DocKind.Field:
                    g.Put(TextCol, row, line.label, FrontRoomsTerminalPalette.Bright, ValueCol - TextCol - 1);
                    g.Put(ValueCol, row, line.text, FrontRoomsTerminalPalette.Field, BoxRight + 1 - ValueCol);
                    T(line.label + " " + line.text);
                    break;
                case DocKind.Text:
                    g.Put(TextCol, row, line.text, FrontRoomsTerminalPalette.Field, RuleWidth);
                    T(line.text);
                    break;
            }
        }
    }

    string MoreText(bool up, bool down)
    {
        if (!up && !down) return null;
        var w = content.words.more;
        return up && down ? w + " - +" : up ? w + " -" : w + " +";
    }

    void Box(FrontRoomsTerminalGrid g, int left, int right, int top, int bottom)
    {
        var a = FrontRoomsTerminalPalette.Field;
        g.Set(left, top, 0xDA, a); g.Fill(left + 1, top, right - left - 1, 1, 0xC4, a); g.Set(right, top, 0xBF, a);
        for (var r = top + 1; r < bottom; r++) { g.Set(left, r, 0xB3, a); g.Set(right, r, 0xB3, a); }
        g.Set(left, bottom, 0xC0, a); g.Fill(left + 1, bottom, right - left - 1, 1, 0xC4, a); g.Set(right, bottom, 0xD9, a);
    }

    void LayoutList(FrontRoomsTerminalGrid g)
    {
        var list = content.list;
        var heading = X(list.heading);
        if (!string.IsNullOrEmpty(heading)) { g.Put(TextCol, HeadingRow, heading, FrontRoomsTerminalPalette.Bright, RuleWidth); T(heading); }
        var jobs = Mathf.Min(list.jobs.Length, MaxJobs);
        if (list.jobs.Length > MaxJobs) layoutExtra++;
        var bottom = TableTop + 3 + jobs + 1;
        Box(g, BoxLeft, BoxRight, TableTop, bottom);
        var sep = TableTop + 2;
        g.Set(BoxLeft, sep, 0xC3, FrontRoomsTerminalPalette.Field);
        g.Fill(BoxLeft + 1, sep, BoxRight - BoxLeft - 1, 1, 0xC4, FrontRoomsTerminalPalette.Field);
        g.Set(BoxRight, sep, 0xB4, FrontRoomsTerminalPalette.Field);
        var head = new StringBuilder();
        for (var c = 0; c < ListCols.Length && c < list.columns.Length; c++)
        {
            var s = X(list.columns[c]);
            g.Put(ListCols[c], TableTop + 1, s, FrontRoomsTerminalPalette.Bright, ListWidths[c]);
            if (head.Length > 0) head.Append("  ");
            head.Append(s);
        }
        T(head.ToString());
        for (var j = 0; j < jobs; j++)
        {
            var job = list.jobs[j];
            var row = TableTop + 3 + j;
            var a = j == listBar ? FrontRoomsTerminalPalette.Bar : FrontRoomsTerminalPalette.Field;
            if (j == listBar) g.Fill(BoxLeft + 1, row, BoxRight - BoxLeft - 1, 1, ' ', a);
            var cells = new[] { job.job, job.document, job.owner, job.pages, job.status };
            var line = new StringBuilder();
            for (var c = 0; c < cells.Length; c++)
            {
                var s = X(cells[c]);
                g.Put(ListCols[c], row, s, a, ListWidths[c]);
                if (line.Length > 0) line.Append("  ");
                line.Append(s);
            }
            T(line.ToString());
        }
    }

    void LayoutHelp(FrontRoomsTerminalGrid g)
    {
        var heading = X(content.help.heading);
        g.Put(TextCol, HeadingRow, heading, FrontRoomsTerminalPalette.Bright, RuleWidth);
        T(heading);
        helpRows.Clear();
        for (var i = 0; i < content.help.rows.Length; i++)
        {
            TerminalAction action;
            if (!Enum.TryParse(content.help.rows[i].action, true, out action)) { layoutExtra++; continue; }
            if (!string.IsNullOrEmpty(Name(action))) helpRows.Add(i);
        }
        helpScroll = Mathf.Clamp(helpScroll, 0, Mathf.Max(0, helpRows.Count - HelpView));
        var shown = Mathf.Min(HelpView, helpRows.Count - helpScroll);
        var more = MoreText(helpScroll > 0, helpScroll + shown < helpRows.Count);
        if (more != null) g.PutRight(78, HeadingRow, more, FrontRoomsTerminalPalette.Bright);
        // the key column fits the longest name of this label set (touch: "Tap it again"), the box the longest line
        var keyWidth = HelpKeyWidth;
        var textWidth = 0;
        for (var k = 0; k < shown; k++)
        {
            var row = content.help.rows[helpRows[helpScroll + k]];
            TerminalAction action;
            Enum.TryParse(row.action, true, out action);
            keyWidth = Mathf.Max(keyWidth, Name(action).Length);
            textWidth = Mathf.Max(textWidth, X(row.text).Length);
        }
        var textCol = Mathf.Max(HelpTextCol, HelpKeyCol + keyWidth + 1);
        var right = Mathf.Clamp(textCol + textWidth + 1, HelpRight, BoxRight);
        Box(g, BoxLeft, right, TableTop, TableTop + 1 + shown + 1);
        for (var k = 0; k < shown; k++)
        {
            var row = content.help.rows[helpRows[helpScroll + k]];
            TerminalAction action;
            Enum.TryParse(row.action, true, out action);
            var key = Name(action);
            var text = X(row.text);
            g.Put(HelpKeyCol, TableTop + 1 + k, key, FrontRoomsTerminalPalette.Bright, textCol - HelpKeyCol - 1);
            g.Put(textCol, TableTop + 1 + k, text, FrontRoomsTerminalPalette.Field, right - textCol);
            T(key + "  " + text);
        }
    }

    string PanelMessage()
    {
        switch (panel)
        {
            case TerminalPanel.Main:
                if (bar < content.memos.Length) return X(content.memos[bar].message);
                return bar == content.memos.Length ? X(content.main.listMessage) : X(content.main.helpMessage);
            case TerminalPanel.List:
                return listBar < content.list.jobs.Length ? X(content.list.jobs[listBar].message) : "";
            default:
                return "";
        }
    }

    string RefusalText(TerminalRefusal r)
    {
        switch (r)
        {
            case TerminalRefusal.TopOfData: return X(content.refusals.topOfData);
            case TerminalRefusal.BottomOfData: return X(content.refusals.bottomOfData);
            default: return X(content.refusals.notAuthorized);
        }
    }

    string Word(TerminalAction a)
    {
        var w = content.words;
        switch (a)
        {
            case TerminalAction.Help: return softKeyMode ? w.softHelp : w.help;
            case TerminalAction.Select: return softKeyMode ? w.softSelect : w.select;
            case TerminalAction.Cancel: return softKeyMode ? w.softCancel : w.cancel;
            case TerminalAction.Exit: return w.exit;
            case TerminalAction.Page: return w.page;
            default: return "";
        }
    }

    static TerminalVerb VerbOf(TerminalAction a)
    {
        switch (a)
        {
            case TerminalAction.Select: return TerminalVerb.Select;
            case TerminalAction.Cancel: return TerminalVerb.Cancel;
            case TerminalAction.Exit: return TerminalVerb.Exit;
            case TerminalAction.Help: return TerminalVerb.Help;
            case TerminalAction.Page: return TerminalVerb.PageDown;
            default: return TerminalVerb.None;
        }
    }

    void FooterLine(FrontRoomsTerminalGrid g)
    {
        softKeys.Clear();
        var col = 1;
        var actions = softKeyMode ? SoftRow[(int)panel] : KeyRow[(int)panel];
        foreach (var action in actions)
        {
            if (softKeyMode)
            {
                var label = " " + Word(action) + " ";
                if (col + label.Length > 79) { layoutExtra++; break; }
                g.Put(col, FooterRow, label, FrontRoomsTerminalPalette.SoftKey);
                softKeys.Add(new SoftKey { from = col, to = col + label.Length - 1, verb = VerbOf(action) });
                col += label.Length + 2;
            }
            else
            {
                var name = Name(action);
                if (string.IsNullOrEmpty(name)) continue;
                var word = "=" + Word(action);
                if (col + name.Length + word.Length > 79) { layoutExtra++; break; }
                g.Put(col, FooterRow, name, FrontRoomsTerminalPalette.Bright);
                g.Put(col + name.Length, FooterRow, word, FrontRoomsTerminalPalette.Field);
                col += name.Length + word.Length + 2;
            }
        }
    }
}
