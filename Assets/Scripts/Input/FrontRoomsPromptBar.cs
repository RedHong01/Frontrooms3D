using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using PadButton = FrontRoomsInput.PadButton;

/// <summary>
/// The prompt bar (D, research/interaction/device_prompts/10_research.md §3). Red, 交互设计 2026-11-01 18:5x: "如果要是玩家已经使用
/// 手柄进行游戏的话，下面的这些iconography应该变成手柄套系，并且在按下按钮的时候应该对应inconography也会有对应的映射，这样玩家在进行菜单
/// 设置的时候就能一直有一个恒定用来选择/回退的按钮套系。对于键盘也是一样的".
/// One row of glyph + verb chips at the foot of a menu: confirm and back first, always, then the screen's own (D3). The glyphs
/// follow the device in hand (D1): a key is the desktop kit's keycap (Set·D: a 24 px square keyline, IBM Plex Mono 13); a pad
/// button is its own shape: a disc with A B X Y (a PlayStation pad's ✕ ○ □ △ drawn as strokes, a Nintendo pad's letters by its
/// places), pills for the shoulders, triggers, a d-pad with the arms in use filled, MENU as a pill with three lines (a Nintendo
/// pad's + in a disc). Monochrome paper on the sheet's ink: colour is never the cue.
/// A press fills its glyph, legend in ink, and drops it 1 px (D4; Reduce Motion keeps the fill, not the drop) while the button
/// is held and for at least 0.12 s; the screen acts on the same press as it always has, so nothing waits for the chip. A chip
/// whose button does nothing here is drawn at 40 % and keeps its place, so the pair never moves. A hold fills a ring round the
/// glyph (Hold). Geometry is in the parent's units at <c>scale</c> (1 on the desktop sheet's 1080 canvas).
/// </summary>
public sealed class FrontRoomsPromptBar
{
    /// <summary>What a chip's glyph stands for; the bar draws it for the device in hand.</summary>
    public enum Role
    {
        Confirm,   // ENTER · A (CROSS; a Nintendo pad's A, east)
        Back,      // ESC · B (CIRCLE; a Nintendo pad's B, south)
        Reset,     // BACKSPACE · X (SQUARE; a Nintendo pad's X, north)
        Extra,     // SPACE · Y (TRIANGLE; a Nintendo pad's Y, west)
        Defaults,  // DELETE · Y (TRIANGLE; a Nintendo pad's Y, west)
        Tabs,      // Q E · LB RB (L1 R1; L R)
        TabPrev,   // Q · LB (L1; L): a tab row's own keycap, no words
        TabNext,   // E · RB (R1; R)
        Choose,    // ↑ ↓ · the d-pad's up and down
        Change,    // ← → · the d-pad's left and right
        ChooseAll, // ↑ ↓ ← → · the whole d-pad
        Menu,      // ESC · MENU (OPTIONS; +)
        Restart,   // the END RUN key as bound · no chip on a pad
        Bound,     // a gameplay action's control as bound: Prompt.KeyBind on the keys, Prompt.PadBind on a pad (FrontRoomsInput)
        Text       // no glyph: the verb alone ("PRESS A BUTTON")
    }

    /// <summary>A chip: its role, its words as a table key with the glyph's place as {0} ("{0}  OPEN"; a Text chip's plain
    /// key), and whether its button does something here (false: drawn at 40 %).</summary>
    public struct Prompt
    {
        public Role Role;
        public string Verb;
        public bool Live;
        public FrontRoomsInput.Bind KeyBind, PadBind; // Role.Bound only

        public Prompt(Role role, string verb, bool live = true)
        {
            Role = role;
            Verb = verb;
            Live = live;
            KeyBind = PadBind = default(FrontRoomsInput.Bind);
        }

        /// <summary>A gameplay action's chip: its control as the player bound it on the keys (<paramref name="key"/>) or on a pad
        /// (<paramref name="pad"/>), e.g. Bound(Bind.Use, Bind.PadUse, "{0}  USE").</summary>
        public static Prompt Bound(FrontRoomsInput.Bind key, FrontRoomsInput.Bind pad, string verb, bool live = true) =>
            new Prompt(Role.Bound, verb, live) { KeyBind = key, PadBind = pad };

        public bool Same(Prompt o) => Role == o.Role && Verb == o.Verb && Live == o.Live && KeyBind == o.KeyBind && PadBind == o.PadBind;
    }

    // ---------------------------------------------------------------- the geometry (desktop px at scale 1; the mock's)

    const float Band = 24f;          // every glyph sits in one 24 px band (the kit's keycap)
    const float KeyPad = 8f;         // a keycap's word is 8 px from each side
    const float GlyphGap = 4f;       // between two glyphs of one chip (↑ ↓, Q E, LB RB)
    const float VerbGap = 8f;        // glyph to word
    const float ChipGap = 32f;       // chip to chip
    const float PillW = 38f, PillH = 18f, MenuW = 30f, TriggerW = 32f;
    const float DimAlpha = .4f;      // a chip whose button does nothing here
    const float EdgeAlpha = .5f, LegendAlpha = .8f, VerbAlpha = .75f, ArmAlpha = .45f;
    const float PressHold = .12f;    // a tap shows its press at least this long (D4)
    const int TextSize = 13;

    enum Shape { Keycap, Face, Pill, Trigger, Dpad, Menu, NintendoMenu }

    // A glyph to draw and what presses it: keys by Key, a pad button by place, or d-pad arms (with the left stick past half a tilt).
    sealed class GlyphSpec
    {
        public Shape Shape;
        public string Legend;         // a keycap's or a pill's word, a disc's letter; null for drawn marks
        public int Mark = -1;         // 0-3 ✕ ○ □ △ on a face disc; d-pad arms: 1 vertical, 2 horizontal, 3 all
        public Key[] Keys;
        public PadButton? Button;
        public string Path;           // the END RUN key as bound
        public ButtonControl Control; // Path's control, found once (again if its device went)
    }

    // One glyph on screen.
    sealed class GlyphView
    {
        public RectTransform Rect;
        public RawImage Fill, Edge, Mark;
        public Text Legend;
        public Image Ring;
        public GlyphSpec Spec;
        public float W, X;
        public float PressedAt = float.NegativeInfinity;
        public bool WasDown;
    }

    sealed class ChipView
    {
        public Prompt Prompt;
        public readonly List<GlyphView> Glyphs = new List<GlyphView>();
        public Text Before, After;
        public float X, W;
        public bool Shown;
    }

    readonly Font mono;
    readonly Color paper, ink;
    readonly float scale;
    readonly List<Prompt> prompts = new List<Prompt>();
    readonly List<ChipView> chips = new List<ChipView>();
    readonly List<GlyphView> glyphPool = new List<GlyphView>();
    readonly List<Text> textPool = new List<Text>();
    int glyphsUsed, textsUsed;
    bool dirty = true;
    // What the glyphs were drawn for: a pad in use, its name set (FrontRoomsInput.PadNames), the END RUN key, the language.
    bool drawnPad;
    int drawnNames = -1;
    string drawnRestart, drawnBound;
    FrontRoomsLanguage.Language drawnLanguage;

    /// <summary>The bar's node: its pivot is the left end of its centre line (the right end when <see cref="RightToLeft"/>).</summary>
    public RectTransform Root { get; }

    /// <summary>For the owner's fades.</summary>
    public CanvasGroup Group { get; }

    /// <summary>The laid-out width in the parent's units.</summary>
    public float Width { get; private set; }

    /// <summary>Extras past this width are left out, last first; confirm and back never are.</summary>
    public float MaxWidth
    {
        get => maxWidth;
        set { if (!Mathf.Approximately(value, maxWidth)) { maxWidth = value; dirty = true; } }
    }

    /// <summary>Anchored at its right end: the extras come first and confirm and back sit nearest the anchor, so the pair keeps its
    /// place whatever the extras are (left-anchored bars put the pair first for the same reason).</summary>
    public bool RightToLeft
    {
        get => rightToLeft;
        set { if (value != rightToLeft) { rightToLeft = value; dirty = true; } }
    }

    float maxWidth = float.PositiveInfinity;
    bool rightToLeft;

    /// <summary>On a paper card (ink glyphs, a caught card): glyphs and words at the card's own strength, not the dark sheet's
    /// quieter keylines (at .5 and .8 they read as a disabled control beside a full-ink chip).</summary>
    public bool Solid;

    /// <summary>The HUD's USE line over the lit world (游戏视觉's, device_prompts/20_contracts.md G8, with Keyline on): every
    /// part at full strength (edge, legend, drawn mark, the d-pad's arms, the words), since even Solid's .85 edge reads as
    /// disabled beside the HUD's opaque Bayon. Wins over Solid. A press looks as it always does, its fill full already; at
    /// rest the shape stays unfilled, as on every bar. A chip whose button does nothing here keeps its 40 %.</summary>
    public bool Hud;

    /// <summary>Over the lit world with no sheet behind (the full-screen preview): the HUD's keyline round every glyph and
    /// word (FrontRoomsHudStyle), so bright wallpaper never washes a chip out.</summary>
    public bool Keyline
    {
        get => keyline;
        set { if (value != keyline) { keyline = value; dirty = true; } }
    }

    bool keyline;
    float EdgeA => Hud ? 1f : Solid ? .85f : EdgeAlpha;
    float LegendA => Hud || Solid ? 1f : LegendAlpha;
    float VerbA => Hud || Solid ? 1f : VerbAlpha;
    float ArmA => Hud ? 1f : ArmAlpha;

    FrontRoomsPromptBar(RectTransform parent, Font mono, Color paper, Color ink, float scale)
    {
        this.mono = mono;
        this.paper = paper;
        this.ink = ink;
        this.scale = scale;
        Root = new GameObject("Prompt bar", typeof(RectTransform)).GetComponent<RectTransform>();
        Root.SetParent(parent, false);
        Root.anchorMin = Root.anchorMax = new Vector2(0f, 0f);
        Root.pivot = new Vector2(0f, .5f);
        Root.sizeDelta = new Vector2(0f, Band * scale);
        Group = Root.gameObject.AddComponent<CanvasGroup>();
        Group.blocksRaycasts = false;
        Group.interactable = false;
    }

    /// <summary>A bar under <paramref name="parent"/>, empty until <see cref="Set"/>; the owner anchors and places
    /// <see cref="Root"/> and calls <see cref="Tick"/> each frame it shows.</summary>
    public static FrontRoomsPromptBar Create(RectTransform parent, Font mono, Color paper, Color ink, float scale = 1f) =>
        new FrontRoomsPromptBar(parent, mono, paper, ink, scale);

    /// <summary>The chips, confirm and back first. Cheap when nothing changed: owners may call it every frame.</summary>
    public void Set(IList<Prompt> list)
    {
        var same = list.Count == prompts.Count;
        for (var i = 0; same && i < list.Count; i++) same = list[i].Same(prompts[i]);
        if (same) return;
        // Only a chip's Live changed: its alphas follow in Tick, the layout stands.
        var layoutSame = list.Count == prompts.Count;
        for (var i = 0; layoutSame && i < list.Count; i++) layoutSame = list[i].Role == prompts[i].Role && list[i].Verb == prompts[i].Verb;
        prompts.Clear();
        prompts.AddRange(list);
        if (layoutSame) { for (var i = 0; i < chips.Count && i < prompts.Count; i++) chips[i].Prompt = prompts[i]; return; }
        dirty = true;
    }

    /// <summary>The chips, confirm and back first.</summary>
    public void Set(params Prompt[] list) => Set((IList<Prompt>)list);

    /// <summary>A hold's progress round chip <paramref name="chip"/>'s first glyph, 0 (none) to 1 (the HOLD USE · BREAK GLASS
    /// ring, D4).</summary>
    public void Hold(int chip, float amount)
    {
        if (chip < 0 || chip >= chips.Count || chips[chip].Glyphs.Count == 0) return;
        var g = chips[chip].Glyphs[0];
        if (g.Ring == null)
        {
            if (amount <= 0f) return;
            g.Ring = NewRing(g);
        }
        g.Ring.fillAmount = Mathf.Clamp01(amount);
        g.Ring.enabled = amount > 0f;
    }

    /// <summary>Each frame while shown: the glyphs follow the device in hand, and each answers its button.</summary>
    public void Tick()
    {
        var pad = FrontRoomsInput.PadInUse;
        var names = FrontRoomsInput.PadNames;
        var restart = FrontRoomsInput.PathOf(FrontRoomsInput.Bind.Restart);
        var bound = BoundPaths(pad);
        if (pad != drawnPad || names != drawnNames || restart != drawnRestart || bound != drawnBound || FrontRoomsLanguage.Current != drawnLanguage) dirty = true;
        if (dirty) { drawnBound = bound; Build(pad, names, restart); }
        var now = Time.unscaledTime;
        var reduced = FrontRoomsHandheld.ReduceMotion;
        foreach (var chip in chips)
        {
            if (!chip.Shown) continue;
            var dim = chip.Prompt.Live ? 1f : DimAlpha;
            foreach (var g in chip.Glyphs)
            {
                var down = Down(g.Spec);
                if (down && !g.WasDown) g.PressedAt = now;
                g.WasDown = down;
                var pressed = down || now - g.PressedAt < PressHold;
                PaintGlyph(g, pressed, dim, reduced);
            }
            if (chip.Before != null) chip.Before.color = Alpha(paper, VerbA * dim);
            if (chip.After != null) chip.After.color = Alpha(paper, VerbA * dim);
        }
    }

    // The bound chips' paths for the device in hand, one string (null when there are none): a rebind redraws.
    string BoundPaths(bool pad)
    {
        string paths = null;
        foreach (var p in prompts)
            if (p.Role == Role.Bound) paths += FrontRoomsInput.PathOf(pad ? p.PadBind : p.KeyBind) + "|";
        return paths;
    }

    // ---------------------------------------------------------------- building

    void Build(bool pad, int names, string restart)
    {
        dirty = false;
        drawnPad = pad;
        drawnNames = names;
        drawnRestart = restart;
        drawnLanguage = FrontRoomsLanguage.Current;
        glyphsUsed = textsUsed = 0;
        chips.Clear();
        foreach (var p in prompts)
        {
            var chip = new ChipView { Prompt = p };
            var specs = p.Role == Role.Text ? null : Specs(p, pad, restart);
            // A chip with no glyph for this device (END RUN on a pad) is left out; a Text chip is its words alone.
            if (p.Role != Role.Text && (specs == null || specs.Count == 0)) { chip.Shown = false; chips.Add(chip); continue; }
            SplitVerb(p, out var before, out var after);
            if (!string.IsNullOrEmpty(before)) chip.Before = TakeText(before);
            if (specs != null) foreach (var s in specs) chip.Glyphs.Add(TakeGlyph(s));
            if (!string.IsNullOrEmpty(after)) chip.After = TakeText(after);
            chip.Shown = true;
            chips.Add(chip);
        }
        for (var i = glyphsUsed; i < glyphPool.Count; i++) glyphPool[i].Rect.gameObject.SetActive(false);
        for (var i = textsUsed; i < textPool.Count; i++) textPool[i].gameObject.SetActive(false);
        Layout();
    }

    // The words round the glyph: the table's "{0}  OPEN" puts the glyph first; a language may put it later.
    static void SplitVerb(Prompt p, out string before, out string after)
    {
        var words = FrontRoomsLanguage.T(p.Verb ?? string.Empty);
        if (p.Role == Role.Text) { before = words.Trim(); after = null; return; }
        var at = words.IndexOf("{0}", System.StringComparison.Ordinal);
        if (at < 0) { before = null; after = words.Trim(); return; }
        before = words.Substring(0, at).Trim();
        after = words.Substring(at + 3).Trim();
    }

    void Layout()
    {
        // Widths first, then drop extras from the end until the bar fits (never the first two).
        float ChipWidth(ChipView c)
        {
            var w = 0f;
            if (c.Before != null) w += c.Before.preferredWidth + (c.Glyphs.Count > 0 || c.After != null ? VerbGap * scale : 0f);
            for (var i = 0; i < c.Glyphs.Count; i++) w += c.Glyphs[i].W + (i > 0 ? GlyphGap * scale : 0f);
            if (c.After != null) w += (c.Glyphs.Count > 0 || c.Before != null ? VerbGap * scale : 0f) + c.After.preferredWidth;
            return w;
        }
        var total = 0f;
        var shown = 0;
        for (var i = 0; i < chips.Count; i++)
        {
            var c = chips[i];
            if (!c.Shown) continue;
            c.W = ChipWidth(c);
            var next = total + (shown > 0 ? ChipGap * scale : 0f) + c.W;
            if (i >= 2 && next > MaxWidth) { Hide(c); continue; }
            total = next;
            shown++;
        }
        Width = total;
        // Left to right, or the pair nearest a right-hand anchor.
        var order = new List<ChipView>();
        foreach (var c in chips) if (c.Shown) order.Add(c);
        if (RightToLeft && order.Count > 2)
        {
            var pair = order.GetRange(0, 2);
            order.RemoveRange(0, 2);
            order.AddRange(pair);
        }
        var x = RightToLeft ? -total : 0f;
        foreach (var c in order)
        {
            c.X = x;
            var cx = x;
            if (c.Before != null) { PlaceText(c.Before, cx); cx += c.Before.preferredWidth + VerbGap * scale; }
            for (var i = 0; i < c.Glyphs.Count; i++)
            {
                if (i > 0) cx += GlyphGap * scale;
                var g = c.Glyphs[i];
                g.X = cx;
                cx += g.W;
            }
            if (c.After != null) { if (c.Glyphs.Count > 0) cx += VerbGap * scale; PlaceText(c.After, cx); }
            x += c.W + ChipGap * scale;
        }
        Root.sizeDelta = new Vector2(total, Band * scale);
        Root.pivot = new Vector2(RightToLeft ? 1f : 0f, .5f);
        // Pivot moved: children are placed from the root's own left edge.
        var shift = RightToLeft ? total : 0f;
        foreach (var c in order)
        {
            if (c.Before != null) Nudge(c.Before.rectTransform, shift);
            if (c.After != null) Nudge(c.After.rectTransform, shift);
            foreach (var g in c.Glyphs) PlaceGlyph(g, g.X + shift);
        }
    }

    static void Nudge(RectTransform rt, float dx) => rt.anchoredPosition += new Vector2(dx, 0f);

    void Hide(ChipView c)
    {
        c.Shown = false;
        if (c.Before != null) c.Before.gameObject.SetActive(false);
        if (c.After != null) c.After.gameObject.SetActive(false);
        foreach (var g in c.Glyphs) g.Rect.gameObject.SetActive(false);
    }

    void PlaceText(Text t, float x)
    {
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, .5f);
        rt.pivot = new Vector2(0f, .5f);
        rt.anchoredPosition = new Vector2(x, 0f);
        rt.sizeDelta = new Vector2(t.preferredWidth + 2f, Band * scale);
    }

    void PlaceGlyph(GlyphView g, float x)
    {
        var rt = g.Rect;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, .5f);
        rt.pivot = new Vector2(0f, .5f);
        rt.anchoredPosition = new Vector2(x, 0f);
        rt.sizeDelta = new Vector2(g.W, Band * scale);
    }

    Text TakeText(string value)
    {
        Text t;
        if (textsUsed < textPool.Count) t = textPool[textsUsed];
        else
        {
            t = new GameObject("prompt word", typeof(RectTransform)).AddComponent<Text>();
            t.transform.SetParent(Root, false);
            t.font = mono;
            t.fontSize = Mathf.RoundToInt(TextSize * scale);
            t.alignment = TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = false;
            // The verbs take this language's face (a CJK language's gothic at its floor); key names in keycaps stay Plex Mono.
            FrontRoomsLanguage.Style(t);
            textPool.Add(t);
        }
        textsUsed++;
        t.gameObject.SetActive(true);
        t.text = value;
        t.color = Alpha(paper, VerbA);
        KeylineOf(t);
        return t;
    }

    GlyphView TakeGlyph(GlyphSpec s)
    {
        GlyphView g;
        if (glyphsUsed < glyphPool.Count) g = glyphPool[glyphsUsed];
        else
        {
            g = new GlyphView { Rect = new GameObject("prompt glyph", typeof(RectTransform)).GetComponent<RectTransform>() };
            g.Rect.SetParent(Root, false);
            g.Fill = Raw(g.Rect, "fill");
            g.Edge = Raw(g.Rect, "edge");
            g.Mark = Raw(g.Rect, "mark");
            g.Legend = new GameObject("legend", typeof(RectTransform)).AddComponent<Text>();
            g.Legend.transform.SetParent(g.Rect, false);
            g.Legend.font = mono;
            g.Legend.fontSize = Mathf.RoundToInt(TextSize * scale);
            g.Legend.alignment = TextAnchor.MiddleCenter;
            g.Legend.horizontalOverflow = HorizontalWrapMode.Overflow;
            g.Legend.verticalOverflow = VerticalWrapMode.Overflow;
            g.Legend.raycastTarget = false;
            g.Legend.supportRichText = false;
            glyphPool.Add(g);
        }
        glyphsUsed++;
        g.Rect.gameObject.SetActive(true);
        g.Spec = s;
        g.WasDown = Down(s);
        g.PressedAt = float.NegativeInfinity;
        if (g.Ring != null) { Object.Destroy(g.Ring.sprite); Object.Destroy(g.Ring.gameObject); g.Ring = null; }
        Dress(g);
        return g;
    }

    static RawImage Raw(RectTransform parent, string name)
    {
        var r = new GameObject(name, typeof(RectTransform)).AddComponent<RawImage>();
        r.transform.SetParent(parent, false);
        r.raycastTarget = false;
        Stretch(r.rectTransform);
        return r;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // A glyph's size and masks for its shape.
    void Dress(GlyphView g)
    {
        var s = g.Spec;
        g.Legend.text = s.Legend ?? string.Empty;
        g.Legend.enabled = s.Legend != null;
        Stretch(g.Legend.rectTransform);
        float w, h;
        switch (s.Shape)
        {
            case Shape.Keycap:
                w = Mathf.Max(Band, Mathf.Round(g.Legend.preferredWidth / scale + 2f * KeyPad));
                h = Band;
                break;
            case Shape.Pill: w = Mathf.Max(PillW, Mathf.Round(g.Legend.preferredWidth / scale + 2f * KeyPad + 2f)); h = PillH; break;
            case Shape.Menu: w = MenuW; h = PillH; break;
            case Shape.Trigger: w = TriggerW; h = Band; break;
            default: w = Band; h = Band; break;
        }
        g.W = w * scale;
        // The shape's masks fill a box of w x h centred in the band (a pill is thinner than the band).
        var key = s.Shape + " " + w + "x" + h;
        g.Fill.texture = ShapeMask(key, s.Shape, w, h, false);
        g.Edge.texture = ShapeMask(key, s.Shape, w, h, true);
        var inset = (Band - h) * .5f * scale;
        g.Fill.rectTransform.offsetMin = g.Edge.rectTransform.offsetMin = new Vector2(0f, inset);
        g.Fill.rectTransform.offsetMax = g.Edge.rectTransform.offsetMax = new Vector2(0f, -inset);
        // Marks: ✕ ○ □ △ on a disc, the d-pad's arms in use, MENU's three lines, a Nintendo pad's +.
        Texture2D mark = null;
        if (s.Shape == Shape.Face && s.Mark >= 0) mark = MarkMask("ps" + s.Mark, s.Mark);
        else if (s.Shape == Shape.Dpad) mark = MarkMask("arms" + s.Mark, 10 + s.Mark);
        else if (s.Shape == Shape.Menu) mark = MarkMask("menu", 20);
        else if (s.Shape == Shape.NintendoMenu) mark = MarkMask("plus", 21);
        else if (s.Shape == Shape.Keycap && s.Mark >= 30) mark = MarkMask("arrow" + s.Mark, s.Mark);
        g.Mark.texture = mark;
        g.Mark.enabled = mark != null;
        KeylineOf(g.Fill);
        KeylineOf(g.Edge);
        KeylineOf(g.Mark);
        KeylineOf(g.Legend);
        if (mark != null)
        {
            g.Mark.rectTransform.offsetMin = new Vector2(0f, inset);
            g.Mark.rectTransform.offsetMax = new Vector2(0f, -inset);
        }
    }

    void PaintGlyph(GlyphView g, bool pressed, float dim, bool reduced)
    {
        var drop = pressed && !reduced ? -1f * scale : 0f;
        var p = g.Rect.anchoredPosition;
        if (!Mathf.Approximately(p.y, drop)) g.Rect.anchoredPosition = new Vector2(p.x, drop);
        g.Fill.color = Alpha(paper, pressed ? dim : 0f);
        g.Edge.color = Alpha(paper, (pressed ? 1f : EdgeA) * dim);
        var legend = pressed ? Alpha(ink, 1f) : Alpha(paper, LegendA * dim);
        g.Legend.color = legend;
        if (g.Mark.enabled)
        {
            // The d-pad's arms in use are a fill of their own; pressed, the whole plus is paper and the arms vanish into it.
            var arms = g.Spec.Shape == Shape.Dpad;
            g.Mark.color = arms ? Alpha(paper, pressed ? 0f : ArmA * dim) : legend;
        }
        if (g.Ring != null) g.Ring.color = Alpha(paper, dim);
    }

    static Color Alpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

    // The HUD's keyline (FrontRoomsHudStyle: one Outline tap at OutlineTapAlpha, 4 diagonal copies) round a graphic, or none.
    void KeylineOf(Graphic g)
    {
        if (g == null) return;
        var o = g.GetComponent<Outline>();
        if (!keyline) { if (o != null) o.enabled = false; return; }
        if (o == null) o = g.gameObject.AddComponent<Outline>();
        o.enabled = true;
        var k = FrontRoomsHudStyle.KeylineColor;
        o.effectColor = new Color(k.r, k.g, k.b, FrontRoomsHudStyle.OutlineTapAlpha);
        o.effectDistance = new Vector2(FrontRoomsHudStyle.KeylineDistance * scale, -FrontRoomsHudStyle.KeylineDistance * scale);
        o.useGraphicAlpha = true;
    }

    // ---------------------------------------------------------------- the glyphs for a role and a device

    static readonly Key[] EnterKeys = { Key.Enter, Key.NumpadEnter };

    static List<GlyphSpec> Specs(Prompt p, bool pad, string restart)
    {
        var role = p.Role;
        var list = new List<GlyphSpec>(4);
        if (role == Role.Bound) { list.Add(BoundSpec(FrontRoomsInput.PathOf(pad ? p.PadBind : p.KeyBind))); return list; }
        if (!pad)
        {
            switch (role)
            {
                case Role.Confirm: list.Add(Keycap("ENTER", EnterKeys)); break;
                case Role.Back: case Role.Menu: list.Add(Keycap("ESC", Key.Escape)); break;
                case Role.Reset: list.Add(Keycap("BACKSPACE", Key.Backspace)); break;
                case Role.Extra: list.Add(Keycap("SPACE", Key.Space)); break;
                case Role.Defaults: list.Add(Keycap("DELETE", Key.Delete)); break;
                case Role.Tabs: list.Add(Keycap("Q", Key.Q)); list.Add(Keycap("E", Key.E)); break;
                case Role.TabPrev: list.Add(Keycap("Q", Key.Q)); break;
                case Role.TabNext: list.Add(Keycap("E", Key.E)); break;
                case Role.Choose: list.Add(Arrow(0, Key.UpArrow)); list.Add(Arrow(1, Key.DownArrow)); break;
                case Role.Change: list.Add(Arrow(2, Key.LeftArrow)); list.Add(Arrow(3, Key.RightArrow)); break;
                case Role.ChooseAll:
                    list.Add(Arrow(0, Key.UpArrow)); list.Add(Arrow(1, Key.DownArrow));
                    list.Add(Arrow(2, Key.LeftArrow)); list.Add(Arrow(3, Key.RightArrow));
                    break;
                case Role.Restart: list.Add(KeyGlyph(restart)); break;
            }
            return list;
        }
        switch (role)
        {
            case Role.Confirm: list.Add(Face(FrontRoomsInput.ConfirmButton)); break;
            case Role.Back: list.Add(Face(FrontRoomsInput.BackButton)); break;
            case Role.Reset: list.Add(Face(FrontRoomsInput.ResetButton)); break;
            case Role.Extra: case Role.Defaults: list.Add(Face(FrontRoomsInput.ExtraButton)); break;
            case Role.Tabs:
                list.Add(new GlyphSpec { Shape = Shape.Pill, Legend = FrontRoomsInput.PadName(PadButton.LeftShoulder), Button = PadButton.LeftShoulder });
                list.Add(new GlyphSpec { Shape = Shape.Pill, Legend = FrontRoomsInput.PadName(PadButton.RightShoulder), Button = PadButton.RightShoulder });
                break;
            case Role.TabPrev: list.Add(new GlyphSpec { Shape = Shape.Pill, Legend = FrontRoomsInput.PadName(PadButton.LeftShoulder), Button = PadButton.LeftShoulder }); break;
            case Role.TabNext: list.Add(new GlyphSpec { Shape = Shape.Pill, Legend = FrontRoomsInput.PadName(PadButton.RightShoulder), Button = PadButton.RightShoulder }); break;
            case Role.Choose: list.Add(new GlyphSpec { Shape = Shape.Dpad, Mark = 1 }); break;
            case Role.Change: list.Add(new GlyphSpec { Shape = Shape.Dpad, Mark = 2 }); break;
            case Role.ChooseAll: list.Add(new GlyphSpec { Shape = Shape.Dpad, Mark = 3 }); break;
            case Role.Menu:
                list.Add(new GlyphSpec { Shape = FrontRoomsInput.NintendoPad && !FrontRoomsInput.PlayStationPad ? Shape.NintendoMenu : Shape.Menu, Button = PadButton.Menu });
                break;
        }
        return list;
    }

    static GlyphSpec Keycap(string legend, params Key[] keys) => new GlyphSpec { Shape = Shape.Keycap, Legend = legend, Keys = keys };

    // An arrow key: the kit's keycap with its arrow drawn at the type's stroke (Plex Mono's ← → are 8 x 5 px of ink at 13 px,
    // captures 2026-11-03). 0 up, 1 down, 2 left, 3 right.
    static GlyphSpec Arrow(int dir, Key key) => new GlyphSpec { Shape = Shape.Keycap, Mark = 30 + dir, Keys = new[] { key } };

    // A bound key or mouse button: a keycap with its name, or its arrow drawn.
    static GlyphSpec KeyGlyph(string path)
    {
        var name = FrontRoomsInput.KeyName(path);
        var arrow = name != null && name.Length == 1 ? "↑↓←→".IndexOf(name[0]) : -1;
        return arrow >= 0 ? new GlyphSpec { Shape = Shape.Keycap, Mark = 30 + arrow, Path = path } : new GlyphSpec { Shape = Shape.Keycap, Legend = name, Path = path };
    }

    // A bound control's glyph: a key or a mouse button as a keycap with its name; a pad's face buttons, shoulders, triggers,
    // d-pad and MENU by their shapes; anything else on a pad (a stick, its press, VIEW) as a pill with its name.
    static GlyphSpec BoundSpec(string path)
    {
        var n = FrontRoomsInput.Normalise(path ?? string.Empty);
        const string pad = "<gamepad>/";
        if (!n.StartsWith(pad, System.StringComparison.Ordinal)) return KeyGlyph(path);
        switch (n.Substring(pad.Length))
        {
            case "buttonsouth": return Face(PadButton.South);
            case "buttoneast": return Face(PadButton.East);
            case "buttonwest": return Face(PadButton.West);
            case "buttonnorth": return Face(PadButton.North);
            case "leftshoulder": return new GlyphSpec { Shape = Shape.Pill, Legend = FrontRoomsInput.PadName(PadButton.LeftShoulder), Button = PadButton.LeftShoulder };
            case "rightshoulder": return new GlyphSpec { Shape = Shape.Pill, Legend = FrontRoomsInput.PadName(PadButton.RightShoulder), Button = PadButton.RightShoulder };
            case "lefttrigger": return new GlyphSpec { Shape = Shape.Trigger, Legend = FrontRoomsInput.PadName(PadButton.LeftTrigger), Button = PadButton.LeftTrigger };
            case "righttrigger": return new GlyphSpec { Shape = Shape.Trigger, Legend = FrontRoomsInput.PadName(PadButton.RightTrigger), Button = PadButton.RightTrigger };
            case "start": return new GlyphSpec { Shape = FrontRoomsInput.NintendoPad && !FrontRoomsInput.PlayStationPad ? Shape.NintendoMenu : Shape.Menu, Button = PadButton.Menu };
            case "dpad/up": case "dpad/down": case "dpad/left": case "dpad/right": return new GlyphSpec { Shape = Shape.Dpad, Mark = n.EndsWith("up") || n.EndsWith("down") ? 1 : 2 };
            default: return new GlyphSpec { Shape = Shape.Pill, Legend = FrontRoomsInput.KeyName(path), Path = path };
        }
    }

    // A face button: its letter, or on a PlayStation pad its shape drawn (✕ south, ○ east, □ west, △ north).
    static GlyphSpec Face(PadButton button)
    {
        var s = new GlyphSpec { Shape = Shape.Face, Button = button };
        if (FrontRoomsInput.PlayStationPad)
            s.Mark = button == PadButton.South ? 0 : button == PadButton.East ? 1 : button == PadButton.West ? 2 : 3;
        else s.Legend = FrontRoomsInput.PadName(button);
        return s;
    }

    // ---------------------------------------------------------------- presses

    static bool Down(GlyphSpec s)
    {
        if (s == null) return false;
        if (s.Keys != null)
        {
            var kb = Keyboard.current;
            if (kb == null) return false;
            foreach (var k in s.Keys) if (kb[k].isPressed) return true;
            return false;
        }
        if (s.Path != null)
        {
            if (s.Control == null || !s.Control.device.added) s.Control = InputSystem.FindControl(s.Path) as ButtonControl;
            return s.Control != null && s.Control.isPressed;
        }
        var pad = Gamepad.current;
        if (pad == null) return false;
        if (s.Shape == Shape.Dpad)
        {
            // The d-pad, or the left stick past half a tilt along the arms in use (the menus take both).
            var stick = pad.leftStick.ReadValue();
            var vertical = (s.Mark & 1) != 0 && (pad.dpad.up.isPressed || pad.dpad.down.isPressed || Mathf.Abs(stick.y) > .5f);
            var horizontal = (s.Mark & 2) != 0 && (pad.dpad.left.isPressed || pad.dpad.right.isPressed || Mathf.Abs(stick.x) > .5f);
            return vertical || horizontal;
        }
        if (s.Button == null) return false;
        var button = FrontRoomsInput.PadControl(pad, s.Button.Value);
        return button != null && button.isPressed;
    }

    // ---------------------------------------------------------------- masks (made once per run of the game, as the key map's)

    const int Super = 4;
    static readonly Dictionary<string, Texture2D> masks = new Dictionary<string, Texture2D>();

    // A shape's fill, or its 1 px inside keyline, w x h in px at scale 1, drawn at 4x and anti-aliased from the signed distance.
    static Texture2D ShapeMask(string key, Shape shape, float w, float h, bool edge)
    {
        var name = key + (edge ? " edge" : " fill");
        if (masks.TryGetValue(name, out var cached) && cached != null) return cached;
        var tex = Draw(name, w, h, p =>
        {
            var d = ShapeDistance(shape, p, w, h);
            var a = Mathf.Clamp01(.5f - d * Super);
            if (edge) a -= Mathf.Clamp01(.5f - (d + 1f) * Super);
            return a;
        });
        masks[name] = tex;
        return tex;
    }

    static float ShapeDistance(Shape shape, Vector2 p, float w, float h)
    {
        switch (shape)
        {
            case Shape.Face:
            case Shape.NintendoMenu:
                return (p - new Vector2(w * .5f, h * .5f)).magnitude - w * .5f;
            case Shape.Pill:
            case Shape.Menu:
                return RoundBox(p, w, h, h * .5f, h * .5f);
            case Shape.Trigger:
                return RoundBox(p, w, h, 10f, 3f);
            case Shape.Dpad:
            {
                // A plus of 8 px arms: the union of two boxes.
                var c = new Vector2(w * .5f, h * .5f);
                var q = p - c;
                var v = Box(q, new Vector2(4f, h * .5f));
                var hz = Box(q, new Vector2(w * .5f, 4f));
                return Mathf.Min(v, hz);
            }
            default:
                return Box(p - new Vector2(w * .5f, h * .5f), new Vector2(w * .5f, h * .5f));
        }
    }

    // The signed distance to a box of half size b about the origin.
    static float Box(Vector2 q, Vector2 b)
    {
        var dx = Mathf.Abs(q.x) - b.x;
        var dy = Mathf.Abs(q.y) - b.y;
        return new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dy, 0f)).magnitude + Mathf.Min(Mathf.Max(dx, dy), 0f);
    }

    // A w x h box (y down from the top-left) with top corners of radius top and bottom corners of radius bottom.
    static float RoundBox(Vector2 p, float w, float h, float top, float bottom)
    {
        var r = p.y < h * .5f ? top : bottom;
        var c = new Vector2(w * .5f, h * .5f);
        var q = p - c;
        var b = new Vector2(w * .5f - r, h * .5f - r);
        return Box(q, b) - r;
    }

    // A mark in a 24 px band: 0-3 ✕ ○ □ △ (10 px, 1.1 px strokes), 11-13 the d-pad's arms in use (1 vertical, 2 horizontal,
    // 3 all), 20 MENU's three lines (in the 30 x 18 pill), 21 a Nintendo pad's + (in the disc), 30-33 an arrow key's arrow.
    static Texture2D MarkMask(string name, int kind)
    {
        if (masks.TryGetValue(name, out var cached) && cached != null) return cached;
        var w = kind == 20 ? MenuW : Band;
        var h = kind == 20 ? PillH : Band;
        var c = new Vector2(w * .5f, h * .5f);
        const float stroke = .55f; // half of a 1.1 px stroke
        var tex = Draw(name, w, h, p =>
        {
            float d;
            var q = p - c;
            switch (kind)
            {
                case 0: d = Mathf.Min(Segment(q, new Vector2(-4.25f, -4.25f), new Vector2(4.25f, 4.25f)), Segment(q, new Vector2(4.25f, -4.25f), new Vector2(-4.25f, 4.25f))) - stroke; break;
                case 1: d = Mathf.Abs(q.magnitude - 4.75f) - stroke; break;
                case 2: d = Mathf.Abs(Box(q, new Vector2(4f, 4f))) - stroke; break;
                case 3:
                {
                    var top = new Vector2(0f, -4.75f);
                    var left = new Vector2(-5f, 3.75f);
                    var right = new Vector2(5f, 3.75f);
                    d = Mathf.Min(Segment(q, top, right), Mathf.Min(Segment(q, right, left), Segment(q, left, top))) - stroke;
                    break;
                }
                case 20:
                    d = Mathf.Min(Segment(q, new Vector2(-5f, -3f), new Vector2(5f, -3f)), Mathf.Min(Segment(q, new Vector2(-5f, 0f), new Vector2(5f, 0f)), Segment(q, new Vector2(-5f, 3f), new Vector2(5f, 3f)))) - .5f;
                    break;
                case 21:
                    d = Mathf.Min(Segment(q, new Vector2(-4.5f, 0f), new Vector2(4.5f, 0f)), Segment(q, new Vector2(0f, -4.5f), new Vector2(0f, 4.5f))) - stroke;
                    break;
                case 30:
                case 31:
                case 32:
                case 33:
                {
                    // An arrow 9 px long with a 3.5 px head each side (y is down: up is -y).
                    var dir = kind == 30 ? new Vector2(0f, -1f) : kind == 31 ? new Vector2(0f, 1f) : kind == 32 ? new Vector2(-1f, 0f) : new Vector2(1f, 0f);
                    var side = new Vector2(-dir.y, dir.x);
                    var tip = dir * 4.5f;
                    d = Mathf.Min(Segment(q, -dir * 4.5f, tip), Mathf.Min(Segment(q, tip, tip - dir * 3.5f + side * 3.5f), Segment(q, tip, tip - dir * 3.5f - side * 3.5f))) - stroke;
                    break;
                }
                default:
                {
                    // The arms in use, inside the plus's keyline: up and down 8 x 8 above and below the centre square, left
                    // and right beside it.
                    var arms = kind - 10;
                    d = float.PositiveInfinity;
                    if ((arms & 1) != 0) d = Mathf.Min(d, Mathf.Min(Box(q - new Vector2(0f, -8f), new Vector2(4f, 4f)), Box(q - new Vector2(0f, 8f), new Vector2(4f, 4f))));
                    if ((arms & 2) != 0) d = Mathf.Min(d, Mathf.Min(Box(q - new Vector2(-8f, 0f), new Vector2(4f, 4f)), Box(q - new Vector2(8f, 0f), new Vector2(4f, 4f))));
                    break;
                }
            }
            return Mathf.Clamp01(.5f - d * Super);
        });
        masks[name] = tex;
        return tex;
    }

    static float Segment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Vector2.Dot(ab, ab));
        return (p - (a + ab * t)).magnitude;
    }

    // A white mask w x h px at 4x; alpha from coverage(p), p in px with y down from the top-left.
    static Texture2D Draw(string name, float w, float h, System.Func<Vector2, float> coverage)
    {
        var tw = Mathf.CeilToInt(w * Super);
        var th = Mathf.CeilToInt(h * Super);
        var tex = new Texture2D(tw, th, TextureFormat.RGBA32, false) { name = "prompt " + name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
        var pixels = new Color32[tw * th];
        for (var j = 0; j < th; j++)
        for (var i = 0; i < tw; i++)
        {
            var p = new Vector2((i + .5f) / Super, (th - j - .5f) / Super);
            pixels[j * tw + i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(coverage(p)) * 255f));
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        return tex;
    }

    // The hold ring: 2 px, 3 px outside the glyph's shape, filled clockwise from the top.
    Image NewRing(GlyphView g)
    {
        var s = g.Spec;
        var w = g.W / scale + 6f;
        var h = (s.Shape == Shape.Pill || s.Shape == Shape.Menu ? PillH : Band) + 6f;
        var name = "ring " + s.Shape + " " + w + "x" + h;
        if (!masks.TryGetValue(name, out var tex) || tex == null)
        {
            var shape = s.Shape;
            tex = Draw(name, w, h, p =>
            {
                var d = ShapeDistance(shape, p, w, h);
                return Mathf.Clamp01(.5f - d * Super) - Mathf.Clamp01(.5f - (d + 2f) * Super);
            });
            masks[name] = tex;
        }
        var ring = new GameObject("hold ring", typeof(RectTransform)).AddComponent<Image>();
        ring.transform.SetParent(g.Rect, false);
        ring.raycastTarget = false;
        ring.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
        ring.type = Image.Type.Filled;
        ring.fillMethod = Image.FillMethod.Radial360;
        ring.fillOrigin = (int)Image.Origin360.Top;
        ring.fillClockwise = true;
        var rt = ring.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
        rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(w * scale, h * scale);
        return ring;
    }
}
