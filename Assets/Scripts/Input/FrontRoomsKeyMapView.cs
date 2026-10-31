using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Bind = FrontRoomsInput.Bind;
using BindCheck = FrontRoomsInput.BindCheck;
using Ease = FrontRoomsTouchMotion.Ease;
using Tween = FrontRoomsTouchMotion.Value;

/// <summary>
/// CONTROLS / KEYBOARD &amp; MOUSE, the desktop settings page's second level (Red, 2026-10-29: a mapping graph under
/// CONTROLS, in the settings type and grid, where players remap their keys; Figma 3688:7842, lane KEY MAP). A keyline
/// keyboard and mouse fill columns 1–8: keys in use are paper, free keys a 25 % keyline, the chosen action yellow
/// (focus, as everywhere in settings). Each action is a Bayon 20 label with an IBM Plex Mono 13 note and a 1 px leader
/// through the key gutters; a key moved off its default gets a leader routed around every keycap. The keys in use and
/// the bound keys come from FrontRoomsInput, so the map shows what the game listens to.
/// Motion (lane KEY MAP · MOTION, the recommended candidates): A1 the row's keycaps travel to their keys, B1 the focus
/// bar slides, C1 the key being changed empties, D1 the new key settles and its leader is drawn again. Reduce Motion:
/// short fades only.
/// </summary>
public sealed class FrontRoomsKeyMapView
{
    /// <summary>The map's size in canvas units at 1920 × 1080: columns 1–8, from the list's top.</summary>
    public const float Width = 1176f, Height = 716f;
    // Keys 35 on a 40 pitch (Red, 2026-10-30: a line between keys sits in the middle): every gutter and row gap is 5 units
    // and every key an odd width, so a 1-unit leader has 2 units on each side in a gutter and lands on a key's middle.
    const float U = 40f, KeySize = 35f, KX = 228f, KY = 176f, MX = 1016f, MY = 192f;
    static readonly float[] RowY = { KY, KY + 56f, KY + 96f, KY + 136f, KY + 176f, KY + 216f };
    const string LeftClick = "<Mouse>/leftButton", RightClick = "<Mouse>/rightButton", Wheel = "<Mouse>/scroll/y", MouseLook = "<Mouse>/delta";

    struct KeyDef
    {
        public string Label, Path;
        public float X, W;
        public int Row;
    }

    sealed class KeyView
    {
        public KeyDef Def;
        public Rect Home;
        public Image Fill;
        public readonly Image[] Edges = new Image[4];
        public Text Legend;
        public readonly Tween Chosen = new Tween();
        public readonly Tween Drain = new Tween();
        public readonly Tween Asked = new Tween();
        public readonly Tween Pop = new Tween(1f);
        public readonly Tween Travel = new Tween(1f);
        public Rect TravelFrom;
    }

    sealed class MousePart
    {
        public string Path;
        public RawImage Image;
        public Rect Rect;
        public bool Outline;
        public readonly Tween Chosen = new Tween();
        public readonly Tween Drain = new Tween();
    }

    sealed class Callout
    {
        public string Action, Note, Explain, FixedNote;
        public Bind[] Binds;
        public string[] Fixed;
        public string DefaultTarget;
        // Where the label and leader are drawn now (the slot it holds), and its own slot (the default key's geometry).
        public float LabelX, LabelY;
        public bool OnRight;
        public Vector2 From, To;
        public Callout Slot;
        public float OwnX, OwnY;
        public bool OwnOnRight;
        public Vector2 OwnFrom, OwnTo;
        public Text ActionText, NoteText;
        public Image Dot;
        public readonly List<Image> Lock = new List<Image>();
        public readonly List<Image> Segs = new List<Image>();
        public readonly List<Rect> SegRects = new List<Rect>();
        public float Length;
        public readonly Tween Grow = new Tween(1f);
        public readonly Tween Rise = new Tween();
        public Rect Hit;
        public string RoutedTo;
        public bool IsFixed => Binds.Length == 0;
    }

    public enum CaptureState { Idle, Listening, Asking }

    readonly Color paper, ink, accent;
    readonly Font bayon, mono;
    readonly RectTransform root;
    readonly CanvasGroup group;
    readonly List<KeyView> keys = new List<KeyView>();
    readonly List<MousePart> mouse = new List<MousePart>();
    readonly List<Callout> callouts = new List<Callout>();
    readonly Image bar;
    readonly Tween barX = new Tween(), barY = new Tween(), barH = new Tween(38f), fade = new Tween(1f);
    HashSet<string> bound = new HashSet<string>();
    bool armed;
    int step;
    Bind askOwner;
    string askPath, message;
    float messageUntil, listenStarted;

    public int Focus { get; private set; }
    public CaptureState State { get; private set; }
    public bool Capturing => State != CaptureState.Idle;
    public RectTransform Root => root;
    Callout Chosen => callouts[Focus];
    static bool Reduced => FrontRoomsHandheld.ReduceMotion;

    FrontRoomsKeyMapView(RectTransform parent, Font bayon, Font mono, Color paper, Color ink, Color accent)
    {
        this.bayon = bayon; this.mono = mono; this.paper = paper; this.ink = ink; this.accent = accent;
        root = Node(parent, "Key map (CONTROLS / KEYBOARD & MOUSE)");
        root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.sizeDelta = new Vector2(Width, Height);
        group = root.gameObject.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        BuildKeys();
        BuildMouse();
        BuildCallouts();
        BuildLegend();
        bar = Img(root, accent, 1f);
        Focus = 0;
        barX.Snap(callouts[0].Hit.x + 8f - 16f);
        barY.Snap(callouts[0].LabelY);
        root.gameObject.SetActive(false);
    }

    /// <summary>Builds the map under <paramref name="parent"/>, hidden; the settings page places, shows and feeds it.</summary>
    public static FrontRoomsKeyMapView Create(RectTransform parent, Font bayon, Font mono, Color paper, Color ink, Color accent) =>
        new FrontRoomsKeyMapView(parent, bayon, mono, paper, ink, accent);

    // ------------------------------------------------------------- layout data

    static IEnumerable<KeyDef> Layout()
    {
        KeyDef K(string label, string path, float x, float w, int row) => new KeyDef { Label = label, Path = "<Keyboard>/" + path, X = x, W = w, Row = row };
        // An Apple keyboard prints option and command where a PC prints Alt and the Windows key (OPTION is too wide for the cap).
        var alt = FrontRoomsInput.AppleKeys ? "OPT" : "ALT";
        var meta = FrontRoomsInput.AppleKeys ? "CMD" : "";
        yield return K("ESC", "escape", 0f, 1f, 0);
        for (var i = 0; i < 12; i++) yield return K("F" + (i + 1), "f" + (i + 1), 2f + i + (i >= 4 ? .5f : 0f) + (i >= 8 ? .5f : 0f), 1f, 0);
        var num = new[] { "`", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=" };
        var numPath = new[] { "backquote", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "minus", "equals" };
        for (var i = 0; i < num.Length; i++) yield return K(num[i], numPath[i], i, 1f, 1);
        yield return K("BACKSPACE", "backspace", 13f, 2f, 1);
        yield return K("TAB", "tab", 0f, 1.5f, 2);
        const string top = "QWERTYUIOP";
        for (var i = 0; i < top.Length; i++) yield return K(top[i].ToString(), char.ToLowerInvariant(top[i]).ToString(), 1.5f + i, 1f, 2);
        yield return K("[", "leftBracket", 11.5f, 1f, 2);
        yield return K("]", "rightBracket", 12.5f, 1f, 2);
        yield return K("\\", "backslash", 13.5f, 1.5f, 2);
        yield return K("CAPS", "capsLock", 0f, 1.75f, 3);
        const string home = "ASDFGHJKL";
        for (var i = 0; i < home.Length; i++) yield return K(home[i].ToString(), char.ToLowerInvariant(home[i]).ToString(), 1.75f + i, 1f, 3);
        yield return K(";", "semicolon", 10.75f, 1f, 3);
        yield return K("'", "quote", 11.75f, 1f, 3);
        yield return K("ENTER", "enter", 12.75f, 2.25f, 3);
        yield return K("SHIFT", "leftShift", 0f, 2.25f, 4);
        const string low = "ZXCVBNM";
        for (var i = 0; i < low.Length; i++) yield return K(low[i].ToString(), char.ToLowerInvariant(low[i]).ToString(), 2.25f + i, 1f, 4);
        yield return K(",", "comma", 9.25f, 1f, 4);
        yield return K(".", "period", 10.25f, 1f, 4);
        yield return K("/", "slash", 11.25f, 1f, 4);
        yield return K("SHIFT", "rightShift", 12.25f, 2.75f, 4);
        yield return K("CTRL", "leftCtrl", 0f, 1.25f, 5);
        yield return K(alt, "leftAlt", 1.25f, 1.25f, 5);
        yield return K(meta, "leftMeta", 2.5f, 1.25f, 5);
        yield return K("SPACE", "space", 3.75f, 6.25f, 5);
        yield return K(meta, "rightMeta", 10f, 1.25f, 5);
        yield return K(alt, "rightAlt", 11.25f, 1.25f, 5);
        yield return K("", "contextMenu", 12.5f, 1.25f, 5);
        yield return K("CTRL", "rightCtrl", 13.75f, 1.25f, 5);
        yield return K("↑", "upArrow", 16.5f, 1f, 4);
        yield return K("←", "leftArrow", 15.5f, 1f, 5);
        yield return K("↓", "downArrow", 16.5f, 1f, 5);
        yield return K("→", "rightArrow", 17.5f, 1f, 5);
    }

    static Rect KeyRect(KeyDef k) => new Rect(KX + k.X * U, RowY[k.Row], k.W * U - (U - KeySize), KeySize);

    // ------------------------------------------------------------- build

    void BuildKeys()
    {
        foreach (var k in Layout())
        {
            var v = new KeyView { Def = k, Home = KeyRect(k) };
            v.Fill = Img(root, paper, 0f);
            for (var e = 0; e < 4; e++) v.Edges[e] = Img(root, paper, .25f);
            if (k.Label.Length > 0) v.Legend = Txt(root, LegendFor(k), mono, 13, paper, TextAnchor.MiddleCenter);
            keys.Add(v);
            PlaceKey(v, v.Home, 1f);
        }
    }

    /// <summary>A letter key as this keyboard prints it (the map shows physical keys: an AZERTY board reads A on Q's key).</summary>
    static string LegendFor(KeyDef k)
    {
        if (k.Label.Length == 1 && char.IsLetter(k.Label[0])) return FrontRoomsInput.KeyName(k.Path);
        return k.Label;
    }

    void PlaceKey(KeyView v, Rect r, float scale)
    {
        if (!Mathf.Approximately(scale, 1f))
        {
            var c = r.center;
            r = new Rect(c.x - r.width * scale * .5f, c.y - r.height * scale * .5f, r.width * scale, r.height * scale);
        }
        Place(v.Fill.rectTransform, r.x, r.y, r.width, r.height);
        Place(v.Edges[0].rectTransform, r.x, r.y, r.width, 1f);
        Place(v.Edges[1].rectTransform, r.x, r.yMax - 1f, r.width, 1f);
        Place(v.Edges[2].rectTransform, r.x, r.y, 1f, r.height);
        Place(v.Edges[3].rectTransform, r.xMax - 1f, r.y, 1f, r.height);
        if (v.Legend != null) Place(v.Legend.rectTransform, r.x, r.y, r.width, r.height);
    }

    void BuildMouse()
    {
        // Two buttons over a body, the wheel between them (Figma K2): anti-aliased masks made once, tinted per state.
        mouse.Add(Part(LeftClick, MX, MY, 55f, 72f, new Vector4(55f, 0f, 0f, 0f), true));
        // Symmetric about the 1-unit split at MX + 55 (the LOOK and ZOOM STEP leaders' column): two 55-unit buttons, a
        // 111-unit body, the ring and the wheel odd-wide and centred on it; every part has a middle column.
        mouse.Add(Part(RightClick, MX + 56f, MY, 55f, 72f, new Vector4(0f, 55f, 0f, 0f), false));
        mouse.Add(Part(MouseLook, MX, MY + 73f, 111f, 103f, new Vector4(0f, 0f, 55.5f, 55.5f), true));
        var ring = Part("ring", MX + 47f, MY + 14f, 17f, 34f, new Vector4(8.5f, 8.5f, 8.5f, 8.5f), false);
        ring.Image.color = ink;
        mouse.Add(Part(Wheel, MX + 49f, MY + 16f, 13f, 30f, new Vector4(6.5f, 6.5f, 6.5f, 6.5f), false));
    }

    MousePart Part(string path, float x, float y, float w, float h, Vector4 radii, bool outline)
    {
        var img = new GameObject("mouse · " + path, typeof(RectTransform)).AddComponent<RawImage>();
        img.transform.SetParent(root, false);
        img.raycastTarget = false;
        img.texture = Mask(w, h, radii, outline);
        Place(img.rectTransform, x, y, w, h);
        return new MousePart { Path = path, Image = img, Rect = new Rect(x, y, w, h), Outline = outline };
    }

    void BuildCallouts()
    {
        var space = KeyRect(FindDef("<Keyboard>/space"));
        var down = KeyRect(FindDef("<Keyboard>/downArrow"));
        // A leader is one unit wide: its column is a key's middle column (odd widths: x + (w - 1) / 2), which over W, E and R
        // is also the middle of the gutters above them.
        var spx = Mathf.Floor(space.center.x);
        var dnx = Mathf.Floor(down.center.x);
        var ctrl = KeyRect(FindDef("<Keyboard>/leftCtrl"));
        var ctx = Mathf.Floor(ctrl.center.x);
        // Reading order: the keyboard top to bottom, then the mouse. Leaders run through the gutters (Figma K2).
        Add("MOVE", false, 337f, 104f, new Vector2(345f, 114f), new Vector2(345f, RowY[2]), "<Keyboard>/w",
            "W walks forward, S back, A and D to the sides. In a close-up the BACK key also steps out of it.", null,
            new[] { Bind.Forward, Bind.Left, Bind.Back, Bind.Right }); // reading order: the note says W A S D
        Add("USE", false, 377f, 40f, new Vector2(385f, 50f), new Vector2(385f, RowY[2]), "<Keyboard>/e",
            "Doors, keys and anything you can read. At a window, hold it to break the glass; BREAK GLASS on the CONTROLS page can make that a tap.", null,
            new[] { Bind.Use });
        // END RUN (Red, 2026-10-30): no question any more; the caught card keeps TRY AGAIN on the same key.
        Add("END RUN", true, 433f, 72f, new Vector2(425f, 82f), new Vector2(425f, RowY[2]), "<Keyboard>/r",
            "In the pause, ends the run and goes straight back to the title; nothing asks first. When the Relay has caught you, the same key is TRY AGAIN.", null,
            new[] { Bind.Restart });
        Add("SETTINGS", false, 196f, 183f, new Vector2(204f, 193f), new Vector2(KX, 193f), "<Keyboard>/escape",
            "Pauses the run and opens this page; Esc again goes back to the game. In a close-up it steps back; on a page like this one it goes back a level.",
            "ESC ALWAYS PAUSES AND GOES BACK", new Bind[0], "<Keyboard>/escape");
        // Red, 2026-10-30: Shift walks slowly (the phone's SLOW · UNHEARD) and takes SPRINT's old place; Ctrl runs, labelled
        // under the board like START, so the left column keeps its rhythm. SPRINT's locked key is Right Option on Apple keyboards.
        Add("SLOW", false, 196f, 359f, new Vector2(204f, 369f), new Vector2(KX, 369f), "<Keyboard>/leftShift",
            "Hold it to walk slowly. The Relay cannot hear you at this pace; a normal walk it hears from about 9 m.", null,
            new[] { Bind.Slow }, "<Keyboard>/rightShift");
        Add("SPRINT", true, ctx + 8f, 460f, new Vector2(ctx, ctrl.yMax), new Vector2(ctx, 470f), "<Keyboard>/leftCtrl",
            "Hold it to run, forward only. Running is loud: the Relay hears it from further away.", null,
            new[] { Bind.Sprint }, FrontRoomsInput.SprintLockPath);
        Add("START", true, spx + 8f, 460f, new Vector2(spx, space.yMax), new Vector2(spx, 470f), "<Keyboard>/space",
            "On the title screen, starts the run.", "THE TITLE ALWAYS STARTS WITH SPACE OR ENTER", new Bind[0], "<Keyboard>/space", "<Keyboard>/enter");
        Add("MOVE", true, dnx + 8f, 460f, new Vector2(dnx, down.yMax), new Vector2(dnx, 470f), "<Keyboard>/downArrow",
            "The arrow keys walk too, whatever W A S D become, so the walk can never be lost.", "THE ARROWS ALWAYS WALK", new Bind[0],
            "<Keyboard>/upArrow", "<Keyboard>/leftArrow", "<Keyboard>/downArrow", "<Keyboard>/rightArrow");
        // ZOOM meets the right button on its middle column (MX + 83), just inside its curved top.
        Add("ZOOM", false, MX + 75f, 40f, new Vector2(MX + 83f, 50f), new Vector2(MX + 83f, 200f), RightClick,
            "In a close-up, this fills the screen with what you are looking at, and again goes back.", null, new[] { Bind.Zoom });
        Add("ZOOM STEP", false, MX + 47f, 104f, new Vector2(MX + 55f, 114f), new Vector2(MX + 55f, MY + 16f), Wheel,
            "In a close-up, each turn of the wheel zooms one step.", "THE WHEEL ALWAYS STEPS THE ZOOM", new Bind[0], Wheel);
        Add("LOOK", true, MX + 63f, 400f, new Vector2(MX + 55f, MY + 176f), new Vector2(MX + 55f, 410f), MouseLook,
            "Turn your head. The game holds the mouse while you play; Esc lets it go.", "THE MOUSE ALWAYS LOOKS", new Bind[0], MouseLook);
    }

    void Add(string action, bool onRight, float x, float y, Vector2 from, Vector2 to, string defaultTarget, string explain, string fixedNote, Bind[] binds, params string[] fixedPaths)
    {
        var c = new Callout
        {
            Action = action, Explain = explain, FixedNote = fixedNote, Binds = binds, Fixed = fixedPaths, DefaultTarget = defaultTarget,
            LabelX = x, LabelY = y, OnRight = onRight, From = from, To = to, OwnX = x, OwnY = y, OwnOnRight = onRight, OwnFrom = from, OwnTo = to,
        };
        c.Slot = c;
        var anchor = onRight ? TextAnchor.UpperLeft : TextAnchor.UpperRight;
        c.ActionText = Txt(root, FrontRoomsLanguage.T(action), bayon, 20, paper, anchor);
        FrontRoomsLanguage.Style(c.ActionText, action);
        c.NoteText = Txt(root, "", mono, 13, new Color(paper.r, paper.g, paper.b, .75f), anchor);
        FrontRoomsLanguage.Style(c.NoteText);
        c.Dot = Img(root, paper, 0f);
        if (c.IsFixed)
        {
            // A padlock (research: fixed keys carry one): a 5 × 3 shackle in a keyline over a 7 × 5 body.
            foreach (var r in new[] { new Rect(1f, 0f, 5f, 1f), new Rect(1f, 0f, 1f, 4f), new Rect(5f, 0f, 1f, 4f), new Rect(0f, 4f, 7f, 5f) })
                c.Lock.Add(Img(root, paper, .75f));
        }
        callouts.Add(c);
        LayoutLabel(c);
    }

    void LayoutLabel(Callout c)
    {
        c.Note = NoteFor(c);
        c.NoteText.text = c.Note;
        var anchor = c.OnRight ? TextAnchor.UpperLeft : TextAnchor.UpperRight;
        c.ActionText.alignment = anchor;
        c.NoteText.alignment = anchor;
        var aw = FrontRoomsSettingsMenu.TextWidth(FrontRoomsLanguage.T(c.Action), 20f, false) + 2f;
        var nw = FrontRoomsSettingsMenu.TextWidth(c.Note, 13f, true) + 2f;
        var w = Mathf.Max(aw, nw);
        var rise = c.Rise.Current;
        var ax = c.OnRight ? c.LabelX : c.LabelX - aw;
        Place(c.ActionText.rectTransform, ax, c.LabelY + rise, aw, 22f);
        Place(c.NoteText.rectTransform, c.OnRight ? c.LabelX : c.LabelX - nw, c.LabelY + 22f + rise, nw, 18f);
        var left = c.OnRight ? c.LabelX : c.LabelX - w;
        c.Hit = new Rect(left - 8f, c.LabelY - 6f, w + 16f, 50f);
        // The changed dot (settings' rule: a changed row shows a dot before its control) and the padlock sit before the action word.
        var mark = c.OnRight ? ax + aw + 8f : ax - 12f;
        Place(c.Dot.rectTransform, mark, c.LabelY + 7f + rise, 6f, 6f);
        var shapes = new[] { new Rect(1f, 0f, 5f, 1f), new Rect(1f, 0f, 1f, 4f), new Rect(5f, 0f, 1f, 4f), new Rect(0f, 4f, 7f, 5f) };
        for (var i = 0; i < c.Lock.Count; i++)
            Place(c.Lock[i].rectTransform, (c.OnRight ? ax + aw + 8f : ax - 13f) + shapes[i].x, c.LabelY + 5f + rise + shapes[i].y, shapes[i].width, shapes[i].height);
    }

    string NoteFor(Callout c)
    {
        if (c == Chosen && State == CaptureState.Listening)
        {
            if (message != null && FrontRoomsTouchMotion.Now < messageUntil) return message;
            return c.Binds.Length > 1 ? FrontRoomsLanguage.T("PRESS A KEY") + " · " + FrontRoomsLanguage.T(Part(c.Binds[step])) + " " + (step + 1) + "/" + c.Binds.Length : FrontRoomsLanguage.T("PRESS A KEY");
        }
        if (c == Chosen && State == CaptureState.Asking) return FrontRoomsLanguage.F("SWAP WITH {0}?", FrontRoomsLanguage.T(ActionOf(askOwner)));
        if (c.Action == "MOVE" && !c.IsFixed)
        {
            var names = new List<string>();
            foreach (var b in c.Binds) names.Add(FrontRoomsInput.KeyName(FrontRoomsInput.PathOf(b)));
            return string.Join(" ", names.ToArray());
        }
        switch (c.Action)
        {
            case "USE": return FrontRoomsLanguage.T(FrontRoomsSettings.TapToBreak ? "TAP AT GLASS: BREAK IT" : "HOLD AT GLASS: BREAK IT");
            case "END RUN": return FrontRoomsLanguage.T("STRAIGHT TO THE TITLE");
            case "SETTINGS": return FrontRoomsLanguage.T("PAUSE · BACK");
            case "SPRINT": return FrontRoomsLanguage.T("HOLD · FORWARD ONLY");
            case "SLOW": return FrontRoomsLanguage.T("HOLD · UNHEARD");
            case "START": return FrontRoomsLanguage.T("SPACE OR ENTER · TITLE");
            case "MOVE": return FrontRoomsLanguage.T("ARROW KEYS");
            case "ZOOM": return FrontRoomsLanguage.T("CLOSE-UPS");
            case "ZOOM STEP": return FrontRoomsLanguage.T("WHEEL · CLOSE-UPS");
            case "LOOK": return FrontRoomsLanguage.T("THE MOUSE");
        }
        return "";
    }

    static string Part(Bind b) => b == Bind.Forward ? "FORWARD" : b == Bind.Back ? "BACK" : b == Bind.Left ? "LEFT" : b == Bind.Right ? "RIGHT" : b.ToString().ToUpperInvariant();

    /// <summary>The map's name for a target's action (the label players read).</summary>
    public static string ActionOf(Bind b)
    {
        switch (b)
        {
            case Bind.Sprint: return "SPRINT";
            case Bind.Slow: return "SLOW";
            case Bind.Use: return "USE";
            case Bind.Restart: return "END RUN";
            case Bind.Zoom: return "ZOOM";
            default: return "MOVE " + Part(b);
        }
    }

    void BuildLegend()
    {
        var x = KX;
        var labels = new[] { "IN USE", "FREE", "CHOSEN" };
        for (var kind = 0; kind < labels.Length; kind++)
        {
            if (kind == 1)
            {
                foreach (var e in new[] { new Rect(x, 562f, 12f, 1f), new Rect(x, 573f, 12f, 1f), new Rect(x, 562f, 1f, 12f), new Rect(x + 11f, 562f, 1f, 12f) })
                    Place(Img(root, paper, .25f).rectTransform, e.x, e.y, e.width, e.height);
            }
            else Place(Img(root, kind == 2 ? accent : paper, 1f).rectTransform, x, 562f, 12f, 12f);
            var t = Txt(root, FrontRoomsLanguage.T(labels[kind]), mono, 13, new Color(paper.r, paper.g, paper.b, .75f), TextAnchor.UpperLeft);
            FrontRoomsLanguage.Style(t, labels[kind]);
            var w = FrontRoomsSettingsMenu.TextWidth(FrontRoomsLanguage.T(labels[kind]), 13f, true) + 2f;
            Place(t.rectTransform, x + 20f, 560f, w, 18f);
            x += 20f + w + 32f;
        }
    }

    // ------------------------------------------------------------- state

    public void SetActive(bool on)
    {
        if (!on) CancelCapture();
        root.gameObject.SetActive(on);
        if (on) Refresh(true);
    }

    /// <summary>Places the map at (x, y) of its parent (top-left, y down) and scales it to fit <paramref name="available"/> width.</summary>
    public void PlaceAt(float x, float y, float available)
    {
        root.anchoredPosition = new Vector2(x, -y);
        var s = Mathf.Clamp01(available / Width);
        root.localScale = new Vector3(s, s, 1f);
    }

    /// <summary>Chooses the action named <paramref name="action"/> (the first one, for MOVE).</summary>
    public void Choose(string action)
    {
        for (var i = 0; i < callouts.Count; i++) if (callouts[i].Action == action) { SetFocus(i, true); return; }
    }

    void SetFocus(int i, bool snap)
    {
        Focus = i;
        var c = callouts[i];
        var left = c.Hit.x + 8f;
        if (snap || Reduced)
        {
            barX.Snap(left - 16f); barY.Snap(c.LabelY); barH.Snap(38f);
        }
        else
        {
            // B1: the focus bar slides to the chosen label, as the list's bar does.
            barX.To(left - 16f, .16f, Ease.OutCubic); barY.To(c.LabelY, .16f, Ease.OutCubic);
        }
        Refresh(false);
    }

    /// <summary>
    /// A1 (open): the CONTROLS row's keycaps travel from where they were drawn (map units, by control path) to their keys,
    /// the rest of the board fades in behind them, then each leader is drawn and its label rises 8 px. Reduce Motion: a fade.
    /// </summary>
    public void PlayOpen(Dictionary<string, Rect> from)
    {
        if (Reduced) { fade.Restart(0f, 1f, .12f, Ease.Linear); return; }
        fade.Snap(1f);
        foreach (var v in keys)
        {
            var key = FrontRoomsInput.Normalise(v.Def.Path);
            if (from != null && from.TryGetValue(key, out var r)) { v.TravelFrom = r; v.Travel.Restart(0f, 1f, .24f, Ease.OutCubic); }
            else v.Travel.Snap(1f);
        }
        for (var i = 0; i < callouts.Count; i++)
        {
            callouts[i].Grow.Restart(0f, 1f, .12f, Ease.OutCubic, .2f + i * .024f);
            callouts[i].Rise.Restart(8f, 0f, .2f, Ease.OutCubic, .22f + i * .024f);
        }
    }

    public void Refresh(bool reread = false)
    {
        bound = FrontRoomsInput.DesktopBoundPaths();
        var chosen = new HashSet<string>();
        foreach (var p in PathsOf(Chosen)) chosen.Add(FrontRoomsInput.Normalise(p));
        var target = State == CaptureState.Listening && Chosen.Binds.Length > 0 ? FrontRoomsInput.Normalise(FrontRoomsInput.PathOf(Chosen.Binds[step])) : null;
        foreach (var v in keys)
        {
            var path = FrontRoomsInput.Normalise(v.Def.Path);
            v.Chosen.To(chosen.Contains(path) ? 1f : 0f, Reduced ? 0f : .08f, Ease.OutCubic);
            // C1: the key being changed empties to a yellow keyline. Asking: the other action's key is outlined too.
            v.Drain.To(path == target ? 1f : 0f, Reduced ? 0f : .12f, Ease.OutCubic);
            v.Asked.To(State == CaptureState.Asking && path == FrontRoomsInput.Normalise(askPath) ? 1f : 0f, Reduced ? 0f : .12f, Ease.OutCubic);
        }
        foreach (var m in mouse)
        {
            if (m.Path == "ring") continue;
            var path = FrontRoomsInput.Normalise(m.Path);
            m.Chosen.To(chosen.Contains(path) ? 1f : 0f, Reduced ? 0f : .08f, Ease.OutCubic);
            m.Drain.To(path == target ? 1f : 0f, Reduced ? 0f : .12f, Ease.OutCubic);
        }
        var moved = AssignSlots();
        // Every label in its new place, and every leader about to move cleared, before any leader is routed: the router
        // walls in labels and leaders as they will be, never as they were (USE on F right after the E/R swap: END RUN's
        // old label and leader still sat in USE's slot, walled its start in, and USE fell back to an L through E and D).
        var pending = new List<Callout>();
        var grow = new List<bool>();
        foreach (var c in callouts)
        {
            c.Dot.color = new Color(paper.r, paper.g, paper.b, Changed(c) ? .8f : 0f);
            LayoutLabel(c);
            var routeTo = LeaderTarget(c);
            if (!reread && !moved && c.RoutedTo == routeTo) continue;
            pending.Add(c);
            grow.Add(!reread && c.RoutedTo != null && c.RoutedTo != routeTo);
            c.SegRects.Clear();
        }
        // Straight leaders (a key in its own slot) first, then the routed ones around them.
        for (var pass = 0; pass < 2; pass++)
            for (var i = 0; i < pending.Count; i++)
            {
                var routeTo = LeaderTarget(pending[i]);
                if ((routeTo == FrontRoomsInput.Normalise(pending[i].Slot.DefaultTarget)) == (pass == 0)) Route(pending[i], routeTo, grow[i]);
            }
        // The focus bar follows the chosen label when its slot moved (a swap).
        var bx = Chosen.Hit.x - 8f;
        if (Mathf.Abs(barX.Target - bx) > .5f || Mathf.Abs(barY.Target - Chosen.LabelY) > .5f)
        {
            if (Reduced) { barX.Snap(bx); barY.Snap(Chosen.LabelY); }
            else { barX.To(bx, .16f, Ease.OutCubic); barY.To(Chosen.LabelY, .16f, Ease.OutCubic); }
        }
        Animate();
    }

    /// <summary>
    /// Labels follow keys: each slot (a label place with its leader) belongs to a default key, so an action bound to
    /// another action's default key takes that slot (E and R swapped: USE and END RUN trade places and both leaders stay
    /// straight). An action on a key with no slot keeps its own slot, or takes one another action left. True when a slot changed.
    /// </summary>
    bool AssignSlots()
    {
        var used = new HashSet<Callout>();
        var next = new Dictionary<Callout, Callout>();
        foreach (var c in callouts)
        {
            if (c.IsFixed) { next[c] = c; used.Add(c); continue; }
            var t = LeaderTarget(c);
            foreach (var o in callouts)
            {
                if (o.IsFixed || used.Contains(o) || FrontRoomsInput.Normalise(o.DefaultTarget) != t) continue;
                next[c] = o; used.Add(o);
                break;
            }
        }
        foreach (var c in callouts)
        {
            if (next.ContainsKey(c)) continue;
            if (!used.Contains(c)) { next[c] = c; used.Add(c); continue; }
            foreach (var o in callouts)
            {
                if (o.IsFixed || used.Contains(o)) continue;
                next[c] = o; used.Add(o);
                break;
            }
            if (!next.ContainsKey(c)) next[c] = c;
        }
        var moved = false;
        foreach (var c in callouts)
        {
            var s = next[c];
            if (s != c.Slot) moved = true;
            c.Slot = s;
            c.LabelX = s.OwnX; c.LabelY = s.OwnY; c.OnRight = s.OwnOnRight; c.From = s.OwnFrom; c.To = s.OwnTo;
        }
        return moved;
    }

    static bool Changed(Callout c)
    {
        foreach (var b in c.Binds) if (!FrontRoomsInput.IsDefault(b)) return true;
        return false;
    }

    /// <summary>The control paths an action shows now: its bound keys, then its fixed ones.</summary>
    static List<string> PathsOf(Callout c)
    {
        var list = new List<string>();
        foreach (var b in c.Binds) list.Add(FrontRoomsInput.PathOf(b));
        list.AddRange(c.Fixed);
        return list;
    }

    static string LeaderTarget(Callout c) => FrontRoomsInput.Normalise(c.Binds.Length > 0 ? FrontRoomsInput.PathOf(c.Binds[0]) : c.Fixed.Length > 0 ? c.DefaultTarget : "");

    /// <summary>Per frame while the page is open: the tweens.</summary>
    public void Animate()
    {
        if (!root.gameObject.activeInHierarchy) return;
        group.alpha = fade.Current;
        var now = FrontRoomsTouchMotion.Now;
        foreach (var v in keys)
        {
            var path = FrontRoomsInput.Normalise(v.Def.Path);
            var inUse = bound.Contains(path) || v.Chosen.Current > .001f;
            var chosen = v.Chosen.Current;
            var drain = v.Drain.Current;
            var fillAlpha = (inUse ? 1f : 0f) * (1f - drain);
            var fill = Color.Lerp(paper, accent, chosen);
            v.Fill.color = new Color(fill.r, fill.g, fill.b, fillAlpha);
            var outline = Mathf.Max(drain, v.Asked.Current);
            var edge = outline > .001f ? new Color(accent.r, accent.g, accent.b, outline) : new Color(paper.r, paper.g, paper.b, inUse ? 0f : .25f);
            foreach (var e in v.Edges) e.color = edge;
            if (v.Legend != null) v.Legend.color = fillAlpha > .5f ? ink : new Color(paper.r, paper.g, paper.b, drain > .5f ? .6f : .35f);
            var r = v.Home;
            var t = v.Travel.Current;
            if (t < 1f) r = new Rect(Vector2.LerpUnclamped(v.TravelFrom.position, v.Home.position, t), Vector2.LerpUnclamped(v.TravelFrom.size, v.Home.size, t));
            PlaceKey(v, r, v.Pop.Current);
        }
        foreach (var m in mouse)
        {
            if (m.Path == "ring") continue;
            var path = FrontRoomsInput.Normalise(m.Path);
            var inUse = bound.Contains(path) || path == FrontRoomsInput.Normalise(MouseLook);
            var c = Color.Lerp(paper, accent, Mathf.Max(m.Chosen.Current, m.Drain.Current));
            var alpha = m.Outline ? (inUse ? .5f : .25f) : (inUse ? 1f : .25f) * (1f - .75f * m.Drain.Current);
            if (m.Chosen.Current > .5f) alpha = 1f;
            m.Image.color = new Color(c.r, c.g, c.b, alpha);
        }
        foreach (var c in callouts)
        {
            // The note pulses (1 Hz) while it waits for a key: C1, the word itself moves, nothing is added.
            var listening = c == Chosen && State != CaptureState.Idle;
            var pulse = listening && !Reduced ? .55f + .45f * (.5f + .5f * Mathf.Cos((now - listenStarted) * Mathf.PI * 2f)) : 1f;
            // A1: the label rises in as one (word, note, padlock, changed dot).
            var rise = c.Rise.Current;
            var shown = Mathf.Clamp01(1f - rise / 8f);
            c.NoteText.color = new Color(paper.r, paper.g, paper.b, (listening ? pulse : .75f) * shown);
            c.ActionText.color = new Color(paper.r, paper.g, paper.b, shown);
            foreach (var l in c.Lock) l.color = new Color(paper.r, paper.g, paper.b, .75f * shown);
            c.Dot.color = new Color(paper.r, paper.g, paper.b, Changed(c) ? .8f * shown : 0f);
            if (Mathf.Abs(rise) > .01f || listening) LayoutLabel(c);
            DrawLeader(c);
        }
        var hit = Chosen.Hit;
        Place(bar.rectTransform, barX.Current, barY.Current, 4f, barH.Current);
        bar.enabled = hit.width > 0f;
    }

    // ------------------------------------------------------------- capture (remapping)

    /// <summary>Enter on the chosen action: start listening for its new key; false (and a FIXED note) for a fixed one.</summary>
    public bool BeginCapture()
    {
        if (Chosen.IsFixed) { Flash(FrontRoomsLanguage.T("FIXED")); return false; }
        State = CaptureState.Listening;
        step = 0;
        armed = false;
        message = null;
        listenStarted = FrontRoomsTouchMotion.Now;
        FrontRoomsInput.SuspendGameplay = true;
        Refresh();
        return true;
    }

    public void CancelCapture()
    {
        if (State == CaptureState.Idle) return;
        State = CaptureState.Idle;
        FrontRoomsInput.SuspendGameplay = false;
        Refresh();
    }

    void Flash(string text)
    {
        message = text;
        messageUntil = FrontRoomsTouchMotion.Now + 1.4f;
        LayoutLabel(Chosen);
    }

    /// <summary>
    /// One frame of listening (the page calls it instead of its own keys): Esc cancels; a key or mouse button is checked
    /// and bound, or (taken by another action) asked about: Enter or a click swaps, Esc cancels. Listening starts only
    /// once the key or click that opened it is released (research: never capture the opening press).
    /// </summary>
    public void UpdateCapture()
    {
        if (State == CaptureState.Idle) return;
        var kb = Keyboard.current;
        var ms = Mouse.current;
        if (!armed)
        {
            var held = (kb != null && kb.anyKey.isPressed) || (ms != null && (ms.leftButton.isPressed || ms.rightButton.isPressed || ms.middleButton.isPressed));
            if (!held) armed = true;
            return;
        }
        if (kb != null && kb.escapeKey.wasPressedThisFrame) { CancelCapture(); return; }
        if (State == CaptureState.Asking)
        {
            var yes = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) || (ms != null && ms.leftButton.wasPressedThisFrame);
            if (yes) { Commit(true); }
            return;
        }
        var path = PressedPath(kb, ms);
        if (path == null) return;
        var b = Chosen.Binds[step];
        var check = FrontRoomsInput.CheckBind(b, path, out var owner, out var lockedName);
        switch (check)
        {
            case BindCheck.Reserved: Flash(FrontRoomsInput.KeyName(path) + " IS FIXED"); return;
            case BindCheck.Locked: Flash(FrontRoomsInput.KeyName(path) + " IS " + lockedName + "'S"); return;
            case BindCheck.Taken:
                askOwner = owner; askPath = path;
                State = CaptureState.Asking;
                listenStarted = FrontRoomsTouchMotion.Now;
                Refresh();
                return;
            case BindCheck.Same: Advance(); return;
            default: askPath = path; Commit(false); return;
        }
    }

    void Commit(bool swap)
    {
        var b = Chosen.Binds[step];
        if (swap) FrontRoomsInput.SwapInto(b, askPath); else FrontRoomsInput.Assign(b, askPath);
        // D1: the new key settles (a 0.92 → 1 pop) and its leader is drawn again; a swapped key does the same.
        Settle(askPath);
        if (swap) Settle(FrontRoomsInput.PathOf(askOwner));
        Advance();
    }

    void Settle(string path)
    {
        if (Reduced) return;
        var p = FrontRoomsInput.Normalise(path);
        foreach (var v in keys) if (FrontRoomsInput.Normalise(v.Def.Path) == p) v.Pop.Restart(.92f, 1f, .1f, Ease.OutBack);
    }

    void Advance()
    {
        step++;
        State = CaptureState.Listening;
        message = null;
        if (step >= Chosen.Binds.Length) { State = CaptureState.Idle; FrontRoomsInput.SuspendGameplay = false; step = 0; }
        else { armed = false; listenStarted = FrontRoomsTouchMotion.Now; }
        Refresh();
    }

    /// <summary>Backspace: the chosen action's keys back to their defaults (a key another action holds now is swapped back).</summary>
    public bool ResetChosen()
    {
        if (Chosen.IsFixed || !Changed(Chosen)) return false;
        foreach (var b in Chosen.Binds) { FrontRoomsInput.ResetBind(b); Settle(FrontRoomsInput.PathOf(b)); }
        Refresh();
        return true;
    }

    static string PressedPath(Keyboard kb, Mouse ms)
    {
        if (kb != null)
            foreach (var key in kb.allKeys)
                if (key != null && key.wasPressedThisFrame && key != kb.escapeKey) return "<Keyboard>/" + key.name;
        if (ms == null) return null;
        if (ms.leftButton.wasPressedThisFrame) return "<Mouse>/leftButton";
        if (ms.rightButton.wasPressedThisFrame) return "<Mouse>/rightButton";
        if (ms.middleButton.wasPressedThisFrame) return "<Mouse>/middleButton";
        if (ms.backButton.wasPressedThisFrame) return "<Mouse>/backButton";
        if (ms.forwardButton.wasPressedThisFrame) return "<Mouse>/forwardButton";
        return null;
    }

    // ------------------------------------------------------------- the detail column

    public string Title => FrontRoomsLanguage.T(Chosen.Action);

    public string Body
    {
        get
        {
            var c = Chosen;
            if (State == CaptureState.Asking)
                return FrontRoomsInput.KeyName(askPath) + " is " + ActionOf(askOwner) + "'s key. Swap them? " + ActionOf(c.Binds[step]) + " takes " +
                       FrontRoomsInput.KeyName(askPath) + " and " + ActionOf(askOwner) + " takes " + FrontRoomsInput.KeyName(FrontRoomsInput.PathOf(c.Binds[step])) + ".";
            if (State == CaptureState.Listening)
                return c.Binds.Length > 1
                    ? FrontRoomsLanguage.F(step == 0 ? "Press the key for {0}, then LEFT, BACK and RIGHT. Esc stops; the keys already set stay." : "Press the key for {0}. Esc stops; the keys already set stay.", FrontRoomsLanguage.T(Part(c.Binds[step])))
                    : FrontRoomsLanguage.F("Press the key or mouse button you want for {0}. Esc cancels; it keeps {1}.", FrontRoomsLanguage.T(c.Action)).Replace("{1}", FrontRoomsInput.KeyName(FrontRoomsInput.PathOf(c.Binds[0])));
            return FrontRoomsLanguage.T(c.Explain);
        }
    }

    /// <summary>A row of the detail column's monospaced table: the label through the table, the value's column kept.</summary>
    static string Row(string label) => FrontRoomsLanguage.T(label).PadRight(15);

    public string Meta
    {
        get
        {
            var c = Chosen;
            if (State == CaptureState.Asking) return "ENTER          " + FrontRoomsLanguage.T("SWAP") + "\nESC            " + FrontRoomsLanguage.T("CANCEL");
            if (State == CaptureState.Listening) return Row("NOW") + FrontRoomsInput.KeyName(FrontRoomsInput.PathOf(c.Binds[step]));
            if (c.IsFixed) return Row("FIXED") + FrontRoomsLanguage.T(c.FixedNote);
            var now = new List<string>();
            var defaults = new List<string>();
            foreach (var b in c.Binds) { now.Add(FrontRoomsInput.KeyName(FrontRoomsInput.PathOf(b))); defaults.Add(FrontRoomsInput.KeyName(FrontRoomsInput.DefaultPathOf(b))); }
            var meta = Row(c.Binds.Length > 1 ? "KEYS" : "KEY") + string.Join(" ", now.ToArray());
            if (c.Action == "SPRINT") meta += "\n" + Row("ALSO") + FrontRoomsLanguage.F("RIGHT {0} (LOCKED)", FrontRoomsInput.KeyName(FrontRoomsInput.SprintLockPath));
            if (c.Action == "SLOW") meta += "\n" + Row("ALSO") + FrontRoomsLanguage.T("RIGHT SHIFT (LOCKED)");
            if (c.Action == "MOVE") meta += "\n" + Row("ALSO") + FrontRoomsLanguage.T("THE ARROW KEYS (LOCKED)");
            if (c.Action == "USE") meta += "\n" + Row("IN A CLOSE-UP") + FrontRoomsLanguage.F("{0} OR ESC STEP BACK", FrontRoomsInput.KeyName(FrontRoomsInput.PathOf(Bind.Use)) + ", " + FrontRoomsInput.KeyName(FrontRoomsInput.StepBackPath));
            meta += Changed(c) ? "\n" + Row("DEFAULT") + string.Join(" ", defaults.ToArray()) + "\nBACKSPACE      " + FrontRoomsLanguage.T("BACK TO DEFAULT") : "\nENTER          " + FrontRoomsLanguage.T("CHANGE");
            return meta;
        }
    }

    // ------------------------------------------------------------- choosing

    /// <summary>Arrows, W A S D: the nearest action that way (reading the labels as points on the map).</summary>
    public void Navigate(Vector2 direction)
    {
        var here = Chosen.Hit.center;
        var best = -1;
        var bestCost = float.MaxValue;
        for (var i = 0; i < callouts.Count; i++)
        {
            if (i == Focus) continue;
            var d = callouts[i].Hit.center - here;
            var along = Vector2.Dot(d, direction);
            if (along <= 1f) continue;
            var across = Mathf.Abs(d.x * direction.y - d.y * direction.x);
            var cost = along + 2f * across;
            if (cost < bestCost) { bestCost = cost; best = i; }
        }
        if (best >= 0) SetFocus(best, false);
    }

    /// <summary>The action under a point of the map (its label, or one of its keys or buttons), or -1.</summary>
    public int ActionAt(Vector2 mapPoint)
    {
        for (var i = 0; i < callouts.Count; i++) if (callouts[i].Hit.Contains(mapPoint)) return i;
        string path = null;
        foreach (var v in keys) if (v.Home.Contains(mapPoint)) { path = v.Def.Path; break; }
        if (path == null) foreach (var m in mouse) if (m.Path != "ring" && m.Rect.Contains(mapPoint)) { path = m.Path; break; }
        if (path == null) return -1;
        var p = FrontRoomsInput.Normalise(path);
        for (var i = 0; i < callouts.Count; i++)
            foreach (var q in PathsOf(callouts[i]))
                if (FrontRoomsInput.Normalise(q) == p) return i;
        return -1;
    }

    public bool FocusAt(Vector2 mapPoint)
    {
        if (Capturing) return false;
        var i = ActionAt(mapPoint);
        if (i < 0 || i == Focus) return false;
        SetFocus(i, false);
        return true;
    }

    // ------------------------------------------------------------- leaders

    void Route(Callout c, string target, bool animate)
    {
        c.RoutedTo = target;
        c.SegRects.Clear();
        // A key with its own slot's default: the drawn straight leader of that slot.
        if (target == FrontRoomsInput.Normalise(c.Slot.DefaultTarget)) Seg(c, c.From, c.To);
        else
        {
            var path = FindRoute(c, target) ?? Orthogonal(new List<Vector2> { c.From, Centre(target) });
            for (var i = 1; i < path.Count; i++) Seg(c, path[i - 1], path[i]);
        }
        c.Length = 0f;
        foreach (var r in c.SegRects) c.Length += Mathf.Max(r.width, r.height);
        while (c.Segs.Count < c.SegRects.Count) c.Segs.Add(Img(root, paper, .6f));
        for (var i = c.SegRects.Count; i < c.Segs.Count; i++) c.Segs[i].enabled = false;
        if (animate && !Reduced) c.Grow.Restart(0f, 1f, .16f, Ease.OutCubic);
        DrawLeader(c);
    }

    static void Seg(Callout c, Vector2 a, Vector2 b) =>
        c.SegRects.Add(new Rect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(1f, Mathf.Abs(b.x - a.x)), Mathf.Max(1f, Mathf.Abs(b.y - a.y))));

    void DrawLeader(Callout c)
    {
        // Drawn from the label towards the key, as far as the grow tween has reached.
        var left = c.Length * c.Grow.Current;
        var chosen = c == Chosen;
        for (var i = 0; i < c.SegRects.Count; i++)
        {
            var r = c.SegRects[i];
            var len = Mathf.Max(r.width, r.height);
            var img = c.Segs[i];
            if (left <= 0f) { img.enabled = false; continue; }
            img.enabled = true;
            img.color = new Color(paper.r, paper.g, paper.b, chosen ? .9f : .6f);
            var show = Mathf.Min(len, left);
            left -= len;
            var horizontal = r.width > r.height;
            var startsAtMin = SegmentStartsAtMin(c, i);
            var x = r.x; var y = r.y; var w = r.width; var h = r.height;
            if (horizontal) { w = Mathf.Max(1f, show); if (!startsAtMin) x = r.xMax - w; }
            else { h = Mathf.Max(1f, show); if (!startsAtMin) y = r.yMax - h; }
            Place(img.rectTransform, x, y, w, h);
        }
    }

    // A segment grows from the end nearer the label: the previous segment's end (or the leader's start).
    static bool SegmentStartsAtMin(Callout c, int i)
    {
        var r = c.SegRects[i];
        var start = i == 0 ? c.From : PreviousEnd(c, i);
        return Mathf.Abs(start.x - r.xMin) + Mathf.Abs(start.y - r.yMin) <= Mathf.Abs(start.x - r.xMax) + Mathf.Abs(start.y - r.yMax);
    }

    static Vector2 PreviousEnd(Callout c, int i)
    {
        var p = c.SegRects[i - 1];
        var q = c.SegRects[i];
        // The shared corner of two orthogonal segments.
        var x = Mathf.Abs(p.xMin - q.xMin) < 1.5f || Mathf.Abs(p.xMin - q.xMax) < 1.5f ? p.xMin : p.xMax;
        var y = Mathf.Abs(p.yMin - q.yMin) < 1.5f || Mathf.Abs(p.yMin - q.yMax) < 1.5f ? p.yMin : p.yMax;
        return new Vector2(x, y);
    }

    Vector2 Centre(string path)
    {
        foreach (var v in keys) if (FrontRoomsInput.Normalise(v.Def.Path) == path) return v.Home.center;
        foreach (var m in mouse) if (FrontRoomsInput.Normalise(m.Path) == path) return m.Rect.center;
        return new Vector2(Width * .5f, Height * .5f);
    }

    Rect TargetRect(string path)
    {
        foreach (var v in keys) if (FrontRoomsInput.Normalise(v.Def.Path) == path) return v.Home;
        foreach (var m in mouse) if (FrontRoomsInput.Normalise(m.Path) == path) return m.Rect;
        return new Rect(Width * .5f, Height * .5f, 1f, 1f);
    }

    /// <summary>
    /// A leader for a key moved off its default: A* on a 2-unit grid from the leader's start to the key's edge, with every
    /// other keycap, the mouse, every label and every other leader as walls and a cost on each turn, so the line runs
    /// through the gutters and never crosses a key or another leader. Null when no way is found (the caller draws an L).
    /// </summary>
    List<Vector2> FindRoute(Callout c, string target)
    {
        const float Cell = 2f;
        var w = Mathf.CeilToInt(Width / Cell);
        var h = Mathf.CeilToInt(Height / Cell);
        var blocked = new bool[w * h];
        var goal = TargetRect(target);
        // A cell is a wall when its centre lies in the rect (plus pad). Keys with a 1-unit pad leave one cell in each 5-unit
        // gutter and row gap: its middle, so a routed line sits centred between keys like the straight ones.
        void Block(Rect r, float pad)
        {
            var x0 = Mathf.Max(0, Mathf.CeilToInt((r.xMin - pad - Cell * .5f) / Cell));
            var x1 = Mathf.Min(w - 1, Mathf.FloorToInt((r.xMax + pad - Cell * .5f) / Cell));
            var y0 = Mathf.Max(0, Mathf.CeilToInt((r.yMin - pad - Cell * .5f) / Cell));
            var y1 = Mathf.Min(h - 1, Mathf.FloorToInt((r.yMax + pad - Cell * .5f) / Cell));
            for (var y = y0; y <= y1; y++) for (var x = x0; x <= x1; x++) blocked[y * w + x] = true;
        }
        foreach (var v in keys) if (FrontRoomsInput.Normalise(v.Def.Path) != target) Block(v.Home, 1f);
        foreach (var m in mouse) if (m.Path != "ring" && FrontRoomsInput.Normalise(m.Path) != target) Block(m.Rect, 2f);
        foreach (var o in callouts) if (o != c) Block(o.Hit, 2f);
        // Other leaders are walls too, so two routed leaders never share a gutter and stay readable (a swap of E and R).
        foreach (var o in callouts) if (o != c) foreach (var r in o.SegRects) Block(r, 2f);
        var sx = Mathf.Clamp(Mathf.FloorToInt(c.From.x / Cell), 0, w - 1);
        var sy = Mathf.Clamp(Mathf.FloorToInt(c.From.y / Cell), 0, h - 1);
        // Goal cells: 1–4 units outside one of the target's edges, on its middle column or row (within a unit: the grid's
        // centres are odd, a 1.25-unit key's middle column even; the last stretch is moved onto it below).
        var colX = Mathf.Floor(goal.center.x);
        var rowY = Mathf.Floor(goal.center.y);
        bool IsGoal(int x, int y)
        {
            var px = x * Cell + Cell * .5f; var py = y * Cell + Cell * .5f;
            var dx = Mathf.Max(goal.xMin - px, 0f, px - goal.xMax);
            var dy = Mathf.Max(goal.yMin - py, 0f, py - goal.yMax);
            if (dx < .01f && dy >= 1f && dy <= 4f) return Mathf.Abs(px - colX) <= 1f;
            if (dy < .01f && dx >= 1f && dx <= 4f) return Mathf.Abs(py - rowY) <= 1f;
            return false;
        }
        var cost = new Dictionary<int, float>();
        var came = new Dictionary<int, int>();
        var open = new List<KeyValuePair<float, int>>();
        int State(int x, int y, int dir) => (y * w + x) * 4 + dir;
        void Push(float f, int s)
        {
            open.Add(new KeyValuePair<float, int>(f, s));
            var i = open.Count - 1;
            while (i > 0) { var p = (i - 1) / 2; if (open[p].Key <= open[i].Key) break; var t = open[p]; open[p] = open[i]; open[i] = t; i = p; }
        }
        KeyValuePair<float, int> Pop()
        {
            var top = open[0];
            open[0] = open[open.Count - 1];
            open.RemoveAt(open.Count - 1);
            var i = 0;
            while (true)
            {
                var l = i * 2 + 1; var r = l + 1; var m = i;
                if (l < open.Count && open[l].Key < open[m].Key) m = l;
                if (r < open.Count && open[r].Key < open[m].Key) m = r;
                if (m == i) break;
                var t = open[m]; open[m] = open[i]; open[i] = t; i = m;
            }
            return top;
        }
        float Heuristic(int x, int y)
        {
            var px = x * Cell + Cell * .5f; var py = y * Cell + Cell * .5f;
            return (Mathf.Max(goal.xMin - px, 0f, px - goal.xMax) + Mathf.Max(goal.yMin - py, 0f, py - goal.yMax)) / Cell;
        }
        var dirsX = new[] { 1, -1, 0, 0 };
        var dirsY = new[] { 0, 0, 1, -1 };
        for (var d = 0; d < 4; d++) { var s0 = State(sx, sy, d); cost[s0] = 0f; Push(Heuristic(sx, sy), s0); }
        var found = -1;
        var guard = 0;
        while (open.Count > 0 && guard++ < 400000)
        {
            var cur = Pop().Value;
            var cell = cur / 4; var dir = cur % 4;
            var cx = cell % w; var cy = cell / w;
            if (IsGoal(cx, cy) && (cx != sx || cy != sy)) { found = cur; break; }
            var g = cost[cur];
            for (var nd = 0; nd < 4; nd++)
            {
                var nx = cx + dirsX[nd]; var ny = cy + dirsY[nd];
                if (nx < 0 || ny < 0 || nx >= w || ny >= h || blocked[ny * w + nx]) continue;
                var ns = State(nx, ny, nd);
                var ng = g + 1f + (nd != dir ? 12f : 0f);
                if (cost.TryGetValue(ns, out var old) && old <= ng) continue;
                cost[ns] = ng;
                came[ns] = cur;
                Push(ng + Heuristic(nx, ny), ns);
            }
        }
        if (found < 0) return null;
        var cells = new List<Vector2>();
        for (var s = found; ; )
        {
            var cell = s / 4;
            cells.Add(new Vector2((cell % w) * Cell + Cell * .5f, (cell / w) * Cell + Cell * .5f));
            if (!came.TryGetValue(s, out var prev)) break;
            s = prev;
        }
        cells.Reverse();
        cells.Insert(0, c.From);
        // The last stretch reaches the key's edge straight on, on its middle column (or row).
        var end = cells[cells.Count - 1];
        var vertical = end.x >= goal.xMin && end.x <= goal.xMax;
        for (var i = cells.Count - 1; i >= 1; i--)
        {
            if (vertical ? Mathf.Abs(cells[i].x - end.x) > .01f : Mathf.Abs(cells[i].y - end.y) > .01f) break;
            cells[i] = vertical ? new Vector2(colX, cells[i].y) : new Vector2(cells[i].x, rowY);
        }
        end = cells[cells.Count - 1];
        cells.Add(vertical ? new Vector2(end.x, end.y < goal.yMin ? goal.yMin : goal.yMax) : new Vector2(end.x < goal.xMin ? goal.xMin : goal.xMax, end.y));
        // Keep the corners only.
        var corners = new List<Vector2> { cells[0] };
        for (var i = 1; i < cells.Count - 1; i++)
        {
            var a = corners[corners.Count - 1]; var b = cells[i]; var n = cells[i + 1];
            var straight = (Mathf.Abs(a.x - b.x) < .01f && Mathf.Abs(b.x - n.x) < .01f) || (Mathf.Abs(a.y - b.y) < .01f && Mathf.Abs(b.y - n.y) < .01f);
            if (!straight) corners.Add(b);
        }
        corners.Add(cells[cells.Count - 1]);
        // A run along the wide gap under the F-row sits on its middle row too (the narrow gaps allow nothing else).
        var gapTop = RowY[0] + KeySize;
        var gapMid = gapTop + Mathf.Floor((RowY[1] - gapTop) * .5f);
        for (var i = 1; i < corners.Count - 2; i++)
        {
            var a = corners[i]; var b = corners[i + 1];
            if (Mathf.Abs(a.y - b.y) > .01f || a.y <= gapTop || a.y >= RowY[1]) continue;
            corners[i] = new Vector2(a.x, gapMid);
            corners[i + 1] = new Vector2(b.x, gapMid);
        }
        return Orthogonal(corners);
    }

    /// <summary>Every step square: a step that moves both ways (the leader's start off the grid) becomes an L.</summary>
    static List<Vector2> Orthogonal(List<Vector2> points)
    {
        var o = new List<Vector2> { points[0] };
        for (var i = 1; i < points.Count; i++)
        {
            var a = o[o.Count - 1]; var b = points[i];
            if (Mathf.Abs(a.x - b.x) > 1.5f && Mathf.Abs(a.y - b.y) > 1.5f) o.Add(new Vector2(a.x, b.y));
            o.Add(b);
        }
        return o;
    }

    KeyDef FindDef(string path)
    {
        foreach (var v in keys) if (v.Def.Path == path) return v.Def;
        return default;
    }

    // ------------------------------------------------------------- drawing helpers

    /// <summary>
    /// A white mask of a rectangle with its own radius per corner (x top-left, y top-right, z bottom-right, w bottom-left),
    /// filled or as a 1 px inside keyline, drawn at 3× and anti-aliased from the signed distance.
    /// </summary>
    static Texture2D Mask(float w, float h, Vector4 radii, bool outline)
    {
        const int scale = 3;
        var tw = Mathf.CeilToInt(w * scale);
        var th = Mathf.CeilToInt(h * scale);
        var tex = new Texture2D(tw, th, TextureFormat.RGBA32, false) { name = "key map mask", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
        var pixels = new Color32[tw * th];
        // A corner as wide as its side (the mouse buttons' quarter circles) is an arc about a centre inside the box, so the
        // shape is the box cut by each corner's disc: the larger of the signed distances.
        var centres = new[] { new Vector2(radii.x, radii.x), new Vector2(w - radii.y, radii.y), new Vector2(w - radii.z, h - radii.z), new Vector2(radii.w, h - radii.w) };
        var r4 = new[] { radii.x, radii.y, radii.z, radii.w };
        for (var j = 0; j < th; j++)
        for (var i = 0; i < tw; i++)
        {
            // Shape units, y down from the top-left.
            var p = new Vector2((i + .5f) / scale, (th - j - .5f) / scale);
            var d = Mathf.Max(Mathf.Max(-p.x, p.x - w), Mathf.Max(-p.y, p.y - h));
            for (var k = 0; k < 4; k++)
            {
                if (r4[k] <= 0f) continue;
                var c = centres[k];
                var beyondX = k == 0 || k == 3 ? p.x < c.x : p.x > c.x;
                var beyondY = k < 2 ? p.y < c.y : p.y > c.y;
                if (beyondX && beyondY) d = Mathf.Max(d, (p - c).magnitude - r4[k]);
            }
            var a = Mathf.Clamp01(.5f - d * scale);
            if (outline) a -= Mathf.Clamp01(.5f - (d + 1f) * scale);
            pixels[j * tw + i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        return tex;
    }

    static RectTransform Node(Transform parent, string name)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }

    static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
    }

    static Image Img(Transform parent, Color c, float alpha)
    {
        var i = new GameObject("img", typeof(RectTransform)).AddComponent<Image>();
        i.transform.SetParent(parent, false);
        i.color = new Color(c.r, c.g, c.b, alpha);
        i.raycastTarget = false;
        return i;
    }

    static Text Txt(Transform parent, string value, Font font, int size, Color color, TextAnchor anchor)
    {
        var t = new GameObject("text", typeof(RectTransform)).AddComponent<Text>();
        t.transform.SetParent(parent, false);
        t.font = font; t.fontSize = size; t.color = color; t.alignment = anchor; t.text = value;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false; t.supportRichText = false;
        return t;
    }
}
