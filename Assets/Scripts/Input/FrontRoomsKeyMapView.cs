using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Bind = FrontRoomsInput.Bind;
using BindCheck = FrontRoomsInput.BindCheck;
using Ease = FrontRoomsTouchMotion.Ease;
using Tween = FrontRoomsTouchMotion.Value;

/// <summary>
/// CONTROLS / KEYBOARD &amp; MOUSE (Red, 2026-10-29: a mapping graph under CONTROLS, in the settings type and grid, where
/// players remap their keys; since 2026-11-01 it unfolds under its row in the list, SETTINGS_MOTION.md). A keyline keyboard and
/// mouse across columns 1–12: keys in use are paper, free keys a 25 % keyline, the chosen action yellow (focus, as everywhere in
/// settings). Each action is a Bayon 22 label with an IBM Plex Mono 13 note and a 1 px leader through the key gutters; a key
/// moved off its default gets a leader routed around every keycap. The keys in use and the bound keys come from FrontRoomsInput,
/// so the map shows what the game listens to.
/// Geometry is in map units (a 40-unit key pitch, the 10-29 page's), drawn at a scale k in px (1.45 for the spec's 58 px pitch):
/// it lays itself out rather than scaling its root, so type stays sharp at its own size and every keyline stays 1 px.
/// Motion (SETTINGS_MOTION.md): the board fades in where it stays; a chosen action is covered by a yellow bar, full bleed, its
/// type in ink, as its key and leader cross-fade to yellow, and no bar travels; C1 the key being changed empties, D1 the new key
/// settles and its leader is drawn again. Reduce Motion: short fades only.
/// CONTROLS / CONTROLLER (2026-10-31) is the same view drawn on a pad: its board lives in FrontRoomsKeyMapView.Pad.cs.
/// </summary>
public sealed partial class FrontRoomsKeyMapView
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
        // The centre it is drawn about on touch: its cluster's (the d-pad's), so a cluster stays even at any scale (Snap).
        public Vector2 Axis;
        public Image Fill;
        public readonly Image[] Edges = new Image[4];
        public Text Legend;
        public readonly Tween Chosen = new Tween();
        public readonly Tween Drain = new Tween();
        public readonly Tween Asked = new Tween();
        public readonly Tween Pop = new Tween(1f);
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
        // The yellow bar under a chosen action, full bleed (Red, 2026-11-01), and how far it is in.
        public Image Under;
        public readonly Tween Bar = new Tween();
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
        public Rect Hit;
        // Its padlock, or where its changed dot shows, in map units (touch: Bounds holds it).
        public Rect Mark;
        public string RoutedTo;
        public bool IsFixed => Binds.Length == 0;
    }

    public enum CaptureState { Idle, Listening, Asking }

    readonly Color paper, ink, accent;
    readonly Font bayon, mono;
    // The CONTROLLER board (2026-10-31): a pad drawn instead of the keyboard and the mouse.
    readonly bool padBoard;
    readonly RectTransform root;
    readonly CanvasGroup group;
    readonly List<KeyView> keys = new List<KeyView>();
    readonly List<MousePart> mouse = new List<MousePart>();
    readonly List<Callout> callouts = new List<Callout>();
    readonly Tween fade = new Tween(1f);
    // The drawing's scale, px per map unit; the action's label and note sizes, which do not scale.
    float k = 1f;
    const int ActionSize = 22, NoteSize = 13;
    // The words' scale, 1 on the desktop; the touch sheet's map sets its labels at the touch sizes (17 / 11 pt), and their pads and
    // offsets with them.
    readonly float ts;
    // Touch: from a label's ink to its padlock's or changed dot's, at either side: a word space as the labels' own words stand
    // (Bayon's .26 em with the bearings round it: ZOOM to STEP is .317 em, 5.40 pt at 17 pt), 5.41 pt.
    const float MarkGap = 7f;
    bool showNotes = true;

    /// <summary>Each callout's note line (what the button does; PRESS A BUTTON while it listens), or the action word alone with
    /// the note read elsewhere (ChosenNote): the iPhone's map fits the list's height that way (平面视觉 VL498).</summary>
    public bool ShowNotes
    {
        get => showNotes;
        set
        {
            if (showNotes == value) return;
            showNotes = value;
            foreach (var c in callouts) LayoutLabel(c);
            Refresh(false);
        }
    }

    /// <summary>The chosen callout's note as its label shows it (a flash, PRESS A BUTTON while it listens).</summary>
    public string ChosenNote => Chosen.Note ?? string.Empty;
    // The reveal's clock and where it stands (UpdateReveal).
    float revealAt = -100f, keysIn = 1f, fillsIn = 1f, leadersIn = 1f, calloutsIn = 1f;
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
    /// <summary>Folding: still drawn, fading out, until it hides itself.</summary>
    public bool Closing { get; private set; }
    /// <summary>The drawing's scale, px per map unit.</summary>
    public float Scale => k;
    /// <summary>How tall the drawing is, labels included, in map units (what the host fits to the list's height).</summary>
    public float UnitsTall => padBoard ? 510f : 460f;
    Callout Chosen => callouts[Focus];
    static bool Reduced => FrontRoomsHandheld.ReduceMotion;

    FrontRoomsKeyMapView(RectTransform parent, Font bayon, Font mono, Color paper, Color ink, Color accent, bool pad, float textScale = 1f)
    {
        this.bayon = bayon; this.mono = mono; this.paper = paper; this.ink = ink; this.accent = accent;
        ts = textScale > 0f ? textScale : 1f;
        padBoard = pad;
        root = Node(parent, pad ? "Key map (CONTROLS / CONTROLLER)" : "Key map (CONTROLS / KEYBOARD & MOUSE)");
        root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.sizeDelta = new Vector2(Width, Height);
        group = root.gameObject.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        if (pad) { BuildPad(); BuildPadCallouts(); }
        else { BuildKeys(); BuildMouse(); BuildCallouts(); }
        Focus = 0;
        callouts[0].Bar.Snap(1f);
        root.gameObject.SetActive(false);
    }

    /// <summary>Builds the map under <paramref name="parent"/>, hidden; the settings page places, shows and feeds it.</summary>
    public static FrontRoomsKeyMapView Create(RectTransform parent, Font bayon, Font mono, Color paper, Color ink, Color accent, float textScale = 1f) =>
        new FrontRoomsKeyMapView(parent, bayon, mono, paper, ink, accent, false, textScale);

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
            var v = new KeyView { Def = k, Home = KeyRect(k), Axis = KeyRect(k).center };
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
        // Whole px, so every key's keyline is 1 px whatever the scale; on touch about its cluster's centre (Snap).
        var x0 = Mathf.Round(r.x * k);
        var y0 = Mathf.Round(r.y * k);
        var w = Mathf.Round(r.xMax * k) - x0;
        var h = Mathf.Round(r.yMax * k) - y0;
        if (ts < 1f) { var s = Snap(r, v.Axis); x0 = s.x; y0 = s.y; w = s.width; h = s.height; }
        Place(v.Fill.rectTransform, x0, y0, w, h);
        Place(v.Edges[0].rectTransform, x0, y0, w, 1f);
        Place(v.Edges[1].rectTransform, x0, y0 + h - 1f, w, 1f);
        Place(v.Edges[2].rectTransform, x0, y0, 1f, h);
        Place(v.Edges[3].rectTransform, x0 + w - 1f, y0, 1f, h);
        if (v.Legend != null) Place(v.Legend.rectTransform, x0, y0, w, h);
    }

    /// <summary>A rect of map units, in whole px at the drawing's scale.</summary>
    void PlaceUnits(RectTransform rt, Rect r)
    {
        var x0 = Mathf.Round(r.x * k);
        var y0 = Mathf.Round(r.y * k);
        Place(rt, x0, y0, Mathf.Round(r.xMax * k) - x0, Mathf.Round(r.yMax * k) - y0);
    }

    /// <summary>As PlaceUnits; on touch about <paramref name="axis"/> (Snap).</summary>
    void PlaceUnits(RectTransform rt, Rect r, Vector2 axis)
    {
        if (ts >= 1f) { PlaceUnits(rt, r); return; }
        var s = Snap(r, axis);
        Place(rt, s.x, s.y, s.width, s.height);
    }

    /// <summary>
    /// Touch: a rect of map units in whole px, its size rounded on its own and its centre's offset from <paramref name="axis"/> (its
    /// cluster's centre: the d-pad's, the face buttons', the pad's middle) rounded from an axis on a px's middle for an odd size and on
    /// a px's edge for an even one, so parts of one size are one size and a cluster stays even at any scale (11-04 r0: rounding each
    /// edge at k .35 drew the d-pad's gutters 2 and 1 px, the face buttons' 4 and 5, LB 38 px and RB 39, the left stick 19 × 20).
    /// </summary>
    Rect Snap(Rect r, Vector2 axis)
    {
        var w = Mathf.Max(1f, Mathf.Round(r.width * k));
        var h = Mathf.Max(1f, Mathf.Round(r.height * k));
        var x = About(axis.x, w) + Mathf.Round((r.center.x - axis.x) * k) - w * .5f;
        var y = About(axis.y, h) + Mathf.Round((r.center.y - axis.y) * k) - h * .5f;
        return new Rect(x, y, w, h);
    }

    float About(float axis, float size) => Mathf.RoundToInt(size) % 2 == 1 ? Mathf.Floor(axis * k) + .5f : Mathf.Round(axis * k);

    void BuildMouse()
    {
        // Two buttons over a body, the wheel between them (Figma K2): anti-aliased masks made once, tinted per state.
        // LEFT CLICK is filled like the right button (2026-10-31): the close-up's full-view tap since 10-30 21:38, ZOOM's locked key.
        mouse.Add(Part(LeftClick, MX, MY, 55f, 72f, new Vector4(55f, 0f, 0f, 0f), false));
        // Symmetric about the 1-unit split at MX + 55 (the LOOK and ZOOM STEP leaders' column): two 55-unit buttons, a
        // 111-unit body, the ring and the wheel odd-wide and centred on it; every part has a middle column.
        mouse.Add(Part(RightClick, MX + 56f, MY, 55f, 72f, new Vector4(0f, 55f, 0f, 0f), false));
        mouse.Add(Part(MouseLook, MX, MY + 73f, 111f, 103f, new Vector4(0f, 0f, 55.5f, 55.5f), true));
        var ring = Part("ring", MX + 47f, MY + 14f, 17f, 34f, new Vector4(8.5f, 8.5f, 8.5f, 8.5f), false);
        ring.Image.color = ink;
        // One of the mouse's parts, so a new scale places it with the rest (VL487 D-4: left out, it stayed at the 1x place over
        // F10-F11 and the wheel lost its ring); Animate tints it, the hit tests pass over it.
        mouse.Add(ring);
        mouse.Add(Part(Wheel, MX + 49f, MY + 16f, 13f, 30f, new Vector4(6.5f, 6.5f, 6.5f, 6.5f), false));
    }

    MousePart Part(string path, float x, float y, float w, float h, Vector4 radii, bool outline)
    {
        var img = new GameObject("mouse · " + path, typeof(RectTransform)).AddComponent<RawImage>();
        img.transform.SetParent(root, false);
        img.raycastTarget = false;
        img.texture = Mask(w, h, radii, outline);
        PlaceUnits(img.rectTransform, new Rect(x, y, w, h));
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
            "Keys, locks and anything you can read. At a window, hold it to break the glass; BREAK GLASS on the CONTROLS page can make that a tap.", null,
            new[] { Bind.Use });
        // END RUN (Red, 2026-10-30): no question any more; the caught card keeps TRY AGAIN on the same key.
        Add("END RUN", true, 433f, 72f, new Vector2(425f, 82f), new Vector2(425f, RowY[2]), "<Keyboard>/r",
            "In the pause, ends the run and goes straight back to the title; nothing asks first. Once you are caught, the same key is TRY AGAIN.", null,
            new[] { Bind.Restart });
        Add("SETTINGS", false, 196f, 183f, new Vector2(204f, 193f), new Vector2(KX, 193f), "<Keyboard>/escape",
            "Pauses the run and opens this page; Esc again goes back to the game. In a close-up it steps back; on a page like this one it goes back a level.",
            "ESC ALWAYS PAUSES AND GOES BACK", new Bind[0], "<Keyboard>/escape");
        // Red, 2026-10-30: Shift walks slowly (the phone's SLOW · UNHEARD) and takes SPRINT's old place; Ctrl runs, labelled
        // under the board like START, so the left column keeps its rhythm. SPRINT's locked key is Right Option on Apple keyboards.
        Add("SLOW", false, 196f, 359f, new Vector2(204f, 369f), new Vector2(KX, 369f), "<Keyboard>/leftShift",
            "Hold it to walk slowly. Slow steps are not heard; a normal walk is. A door still makes a sound.", null,
            new[] { Bind.Slow }, "<Keyboard>/rightShift");
        Add("SPRINT", true, ctx + 8f, 460f, new Vector2(ctx, ctrl.yMax), new Vector2(ctx, 470f), "<Keyboard>/leftCtrl",
            "Hold it to run, forward only. Running is loud: it carries further than a walk.", null,
            new[] { Bind.Sprint }, FrontRoomsInput.SprintLockPath);
        Add("START", true, spx + 8f, 460f, new Vector2(spx, space.yMax), new Vector2(spx, 470f), "<Keyboard>/space",
            "On the title screen, starts the run.", "THE TITLE ALWAYS STARTS WITH SPACE OR ENTER", new Bind[0], "<Keyboard>/space", "<Keyboard>/enter");
        Add("MOVE", true, dnx + 8f, 460f, new Vector2(dnx, down.yMax), new Vector2(dnx, 470f), "<Keyboard>/downArrow",
            "The arrow keys walk too, whatever W A S D become, so the walk can never be lost.", "THE ARROWS ALWAYS WALK", new Bind[0],
            "<Keyboard>/upArrow", "<Keyboard>/leftArrow", "<Keyboard>/downArrow", "<Keyboard>/rightArrow");
        // ZOOM meets the right button on its middle column (MX + 83), just inside its curved top. LEFT CLICK is its locked
        // second key (2026-10-31: the close-up's tap, read outside the bindings), lit with it as Right Shift is with SLOW.
        Add("ZOOM", false, MX + 75f, 40f, new Vector2(MX + 83f, 50f), new Vector2(MX + 83f, 200f), RightClick,
            "In a close-up, this fills the screen with what you are looking at, and again goes back.", null, new[] { Bind.Zoom }, LeftClick);
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
        // Under its words and over the keys and the leaders before it.
        c.Under = Img(root, accent, 0f);
        c.ActionText = Txt(root, FrontRoomsLanguage.T(action), bayon, Mathf.RoundToInt(ActionSize * ts), paper, anchor);
        FrontRoomsLanguage.Style(c.ActionText, action);
        c.NoteText = Txt(root, "", mono, Mathf.Max(11, Mathf.RoundToInt(NoteSize * ts)), new Color(paper.r, paper.g, paper.b, .75f), anchor);
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
        c.NoteText.enabled = showNotes;
        var anchor = c.OnRight ? TextAnchor.UpperLeft : TextAnchor.UpperRight;
        c.ActionText.alignment = anchor;
        c.NoteText.alignment = anchor;
        var aw = FrontRoomsSettingsMenu.TextWidth(FrontRoomsLanguage.T(c.Action), ActionSize * ts, false) + 2f;
        var nw = showNotes ? FrontRoomsSettingsMenu.TextWidth(c.Note, Mathf.Max(11, Mathf.RoundToInt(NoteSize * ts)), true) + 2f : 0f;
        var w = Mathf.Max(aw, nw);
        // The label's anchor scales with the drawing; its words stay their size.
        var x = Mathf.Round(c.LabelX * k);
        var y = Mathf.Round(c.LabelY * k);
        var ax = c.OnRight ? x : x - aw;
        Place(c.ActionText.rectTransform, ax, y, aw, 26f * ts);
        Place(c.NoteText.rectTransform, c.OnRight ? x : x - nw, y + 25f * ts, nw, 18f * ts);
        // The bar: 14 px past the words at each side, 6 over and 8 under them (the mock's pads, around Bayon's own leading); on
        // touch with the action word alone, even about its caps (BarTop).
        var left = c.OnRight ? x : x - w;
        var bar = new Rect(left - 14f * ts, BarTop(y), w + 28f * ts, BarHeight);
        Place(c.Under.rectTransform, bar.x, bar.y, bar.width, bar.height);
        c.Hit = new Rect(bar.x / k, bar.y / k, bar.width / k, bar.height / k);
        // The changed dot (settings' rule: a changed row shows a dot before its control) and the padlock sit before the action word.
        var dot = c.OnRight ? ax + aw + 8f * ts : ax - 12f * ts;
        var padlock = c.OnRight ? ax + aw + 8f * ts : ax - 13f * ts;
        // On touch both stand MarkGap from the word's ink at either side, measured on its glyphs as they draw (平面视觉, 11-04: set
        // from its advance, an iPad's padlock stood 3.95-5.70 pt from a left label's ink and 7.52-7.97 from a right one's).
        if (ts < 1f && InkSpan(c.ActionText, out var inkLeft, out var inkRight))
        {
            dot = c.OnRight ? ax + inkRight + MarkGap * ts : ax + inkLeft - MarkGap * ts - 6f;
            padlock = c.OnRight ? ax + inkRight + MarkGap * ts : ax + inkLeft - MarkGap * ts - 7f;
        }
        Place(c.Dot.rectTransform, dot, y + 8f * ts, 6f, 6f);
        var shapes = new[] { new Rect(1f, 0f, 5f, 1f), new Rect(1f, 0f, 1f, 4f), new Rect(5f, 0f, 1f, 4f), new Rect(0f, 4f, 7f, 5f) };
        for (var i = 0; i < c.Lock.Count; i++)
            Place(c.Lock[i].rectTransform, padlock + shapes[i].x, y + 6f * ts + shapes[i].y, shapes[i].width, shapes[i].height);
        c.Mark = c.Lock.Count > 0 ? new Rect(padlock / k, (y + 6f * ts) / k, 7f / k, 9f / k) : new Rect(dot / k, (y + 8f * ts) / k, 6f / k, 6f / k);
    }

    /// <summary>Touch: where a label's words have ink, in px from their rect's left edge: the outer edges of the glyphs' boxes as the
    /// text draws them (each glyph at the size it renders, on its whole-pixel advance; 11-04: an iPad drew ZOOM STEP 0.73 pt wider
    /// than its advances, the phone 0.94 narrower). False with nothing to measure.</summary>
    static bool InkSpan(Text t, out float left, out float right)
    {
        left = right = 0f;
        if (string.IsNullOrEmpty(t.text) || t.font == null) return false;
        // The text's own pixels per point (a dynamic font draws at its canvas's scale), the canvas looked up past a folded, hidden map.
        var scale = t.pixelsPerUnit;
        if (t.canvas == null && t.font.dynamic)
        {
            var canvas = t.GetComponentInParent<Canvas>(true);
            if (canvas != null) scale = canvas.scaleFactor;
        }
        var rect = t.rectTransform.rect;
        var settings = t.GetGenerationSettings(rect.size);
        settings.scaleFactor = scale;
        var gen = t.cachedTextGeneratorForLayout;
        if (!gen.Populate(t.text, settings)) return false;
        // Four vertices a glyph; a space's (or a line end's) box is empty and says nothing of the ink.
        var verts = gen.verts;
        var min = float.MaxValue;
        var max = float.MinValue;
        for (var i = 0; i + 3 < verts.Count; i += 4)
        {
            var from = Mathf.Min(Mathf.Min(verts[i].position.x, verts[i + 1].position.x), Mathf.Min(verts[i + 2].position.x, verts[i + 3].position.x));
            var to = Mathf.Max(Mathf.Max(verts[i].position.x, verts[i + 1].position.x), Mathf.Max(verts[i + 2].position.x, verts[i + 3].position.x));
            if (to - from < .01f) continue;
            min = Mathf.Min(min, from);
            max = Mathf.Max(max, to);
        }
        if (!(max > min)) return false;
        // The vertices are in px about the rect's pivot.
        left = min / scale - rect.xMin;
        right = max / scale - rect.xMin;
        return true;
    }

    string NoteFor(Callout c)
    {
        // A flash shows in every state (2026-10-31: one made while Idle, FIXED or CONNECT A CONTROLLER, used to be lost).
        if (c == Chosen && message != null && FrontRoomsTouchMotion.Now < messageUntil) return message;
        if (c == Chosen && State == CaptureState.Listening)
            return c.Binds.Length > 1 ? FrontRoomsLanguage.T("PRESS A KEY") + " · " + FrontRoomsLanguage.T(Part(c.Binds[step])) + " " + (step + 1) + "/" + c.Binds.Length : FrontRoomsLanguage.T(padBoard ? "PRESS A BUTTON" : "PRESS A KEY");
        if (c == Chosen && State == CaptureState.Asking) return FrontRoomsLanguage.F("SWAP WITH {0}?", FrontRoomsLanguage.T(ActionOf(askOwner)));
        if (padBoard) return PadNote(c);
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
            case "SLOW": return FrontRoomsLanguage.T(FrontRooms3DGame.BodyDoorsActive ? "HOLD · QUIET STEPS" : "HOLD · UNHEARD");
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
            // The pad's targets (CONTROLS / CONTROLLER, 2026-10-31).
            case Bind.PadSprint: return "SPRINT";
            case Bind.PadUse: return "USE";
            case Bind.PadBack: return "BACK";
            case Bind.PadZoom: return "ZOOM";
            default: return "MOVE " + Part(b);
        }
    }

    // ------------------------------------------------------------- state

    public void SetActive(bool on)
    {
        Closing = false;
        if (!on) CancelCapture();
        root.gameObject.SetActive(on);
        if (on) Refresh(true);
    }

    /// <summary>Places the map at (x, y) of its parent, in px (top-left, y down).</summary>
    public void PlaceAt(float x, float y) => root.anchoredPosition = new Vector2(x, -y);

    /// <summary>The drawing's scale, px per map unit (1.45 is the spec's 58 px pitch): lays everything out again.</summary>
    public void SetScale(float scale)
    {
        if (Mathf.Approximately(k, scale)) return;
        k = scale;
        root.sizeDelta = new Vector2(Width * k, Height * k);
        foreach (var m in mouse) PlaceUnits(m.Image.rectTransform, m.Rect);
        foreach (var part in padParts) if (part.Decoration) PlaceUnits(part.Edge.rectTransform, part.Home, part.Axis);
        foreach (var c in callouts) LayoutLabel(c);
        // Touch: where the smaller drawing makes labels meet they go down, and their leaders are routed again (Declutter); the
        // legends take the size their caps hold (FitLegends).
        if (ts < 1f) { FitLegends(); Refresh(false); return; }
        Animate();
    }

    /// <summary>What the drawing covers now, labels included, in px from its origin.</summary>
    public Rect Bounds
    {
        get
        {
            var xMin = float.MaxValue; var yMin = float.MaxValue; var xMax = float.MinValue; var yMax = float.MinValue;
            void Add(Rect r) { xMin = Mathf.Min(xMin, r.xMin); yMin = Mathf.Min(yMin, r.yMin); xMax = Mathf.Max(xMax, r.xMax); yMax = Mathf.Max(yMax, r.yMax); }
            foreach (var c in callouts) Add(c.Hit);
            // Touch: each label's padlock or changed dot too, which can stand past its bar (11-04: an OnRight padlock ended 2.36 pt
            // past it with the notes off, the phone's LOOK 0.92 pt past the map the sheet fitted).
            if (ts < 1f) foreach (var c in callouts) Add(c.Mark);
            foreach (var v in keys) Add(v.Home);
            foreach (var m in mouse) Add(m.Rect);
            foreach (var part in padParts) Add(part.Home);
            return new Rect(xMin * k, yMin * k, (xMax - xMin) * k, (yMax - yMin) * k);
        }
    }

    /// <summary>The chosen action's label, in px from the drawing's origin (the list follows it).</summary>
    public Rect ChosenRect => new Rect(Chosen.Hit.x * k, Chosen.Hit.y * k, Chosen.Hit.width * k, Chosen.Hit.height * k);

    /// <summary>Touch: the words the map sets, for the touch sheet's list edges (FrontRoomsTouchControlsView.TickEdgeWords, 平面视觉
    /// 11-04: a word an edge leaves less than half its caps of is hidden whole, its drawing clipped): each callout's action word and
    /// note line, each with its label's bar in <paramref name="bars"/>, then every legend, with none. In <paramref name="marks"/>
    /// what a word carries: the action word its changed dot and, on a fixed action, its padlock's four parts (11-04: they annotate
    /// the word, so they hide and show with it); a note and a legend carry none. A PlayStation pad's marks are drawn, as the parts
    /// are. In <paramref name="leaders"/> the word's callout's leader, every run, bend and stub of it, which goes with its bar
    /// (11-04, later: a leader is its callout's, not the drawing's; it points at a word, so it hides once no word of its callout
    /// shows); a legend has none. It is the callout's own list, so a run added when the leader is routed again (Declutter, a moved
    /// key) goes too. The map keeps these for its life, so its host lists them once and reads their caps itself.</summary>
    public void ListWords(List<Text> words, List<Image> bars, List<Image[]> marks, List<IReadOnlyList<Image>> leaders)
    {
        foreach (var c in callouts)
        {
            var carried = new Image[1 + c.Lock.Count];
            carried[0] = c.Dot;
            for (var i = 0; i < c.Lock.Count; i++) carried[1 + i] = c.Lock[i];
            words.Add(c.ActionText); bars.Add(c.Under); marks.Add(carried); leaders.Add(c.Segs);
            words.Add(c.NoteText); bars.Add(c.Under); marks.Add(null); leaders.Add(c.Segs);
        }
        foreach (var v in keys) if (v.Legend != null) { words.Add(v.Legend); bars.Add(null); marks.Add(null); leaders.Add(null); }
        foreach (var part in padParts) if (part.Legend != null) { words.Add(part.Legend); bars.Add(null); marks.Add(null); leaders.Add(null); }
    }

    /// <summary>Chooses the action named <paramref name="action"/> (the first one, for MOVE).</summary>
    public void Choose(string action)
    {
        for (var i = 0; i < callouts.Count; i++) if (callouts[i].Action == action) { SetFocus(i, true); return; }
    }

    void SetFocus(int i, bool snap)
    {
        // A flash belongs to the action it was made on: it never follows the focus.
        message = null;
        Focus = i;
        // Its bar fades in under it in 0.16 s; the one it leaves fades back in 0.12 s. No bar travels (Red, 2026-11-01).
        foreach (var c in callouts)
        {
            var chosen = c == callouts[i];
            if (snap || Reduced) c.Bar.Snap(chosen ? 1f : 0f);
            else c.Bar.To(chosen ? 1f : 0f, chosen ? .16f : .12f, Ease.OutCubic);
        }
        Refresh(false);
    }

    /// <summary>The unfold's reveal, fades only, all in place (SETTINGS_MOTION.md; Red 20:5x: no top-down push, nothing travels):
    /// the keys and the mouse fade in, then the keys in use fade to paper, then the leaders and the callouts.</summary>
    public void PlayOpen()
    {
        Closing = false;
        revealAt = FrontRoomsTouchMotion.Now;
        fade.Snap(1f);
        // This frame too: the first open would otherwise draw one frame at full strength.
        Animate();
    }

    /// <summary>The fold's first half: the whole graph fades out in 0.12 s and hides itself (the host keeps animating it until
    /// then, and folds the row after it).</summary>
    public void PlayClose()
    {
        CancelCapture();
        Closing = true;
        fade.Restart(fade.Current, 0f, .12f, Ease.OutCubic);
    }

    // Keys from 0.10 s for 0.28 s; the keys in use fill from 0.22 s; leaders from 0.30 s; callouts from 0.36 s; 0.26 s each. Reduce
    // Motion: all in a 0.12 s fade.
    void UpdateReveal()
    {
        var t = FrontRoomsTouchMotion.Now - revealAt;
        if (Reduced) { keysIn = fillsIn = leadersIn = calloutsIn = Mathf.Clamp01(t / .12f); return; }
        keysIn = RevealIn(t, .1f, .28f);
        fillsIn = RevealIn(t, .22f, .26f);
        leadersIn = RevealIn(t, .3f, .26f);
        calloutsIn = RevealIn(t, .36f, .26f);
    }

    static float RevealIn(float t, float at, float seconds)
    {
        var u = 1f - Mathf.Clamp01((t - at) / seconds);
        return 1f - u * u * u * u;
    }

    // A key's or a button's word: ink on its fill, else paper, fading in with the keys.
    Color WordOn(float fillAlpha, float drain)
    {
        var word = Color.Lerp(new Color(paper.r, paper.g, paper.b, drain > .5f ? .6f : .35f), new Color(ink.r, ink.g, ink.b, 1f), Mathf.Clamp01((fillAlpha - .3f) / .4f));
        word.a *= keysIn;
        return word;
    }

    public void Refresh(bool reread = false)
    {
        bound = padBoard ? FrontRoomsInput.PadBoundPaths() : FrontRoomsInput.DesktopBoundPaths();
        var chosen = new HashSet<string>();
        foreach (var p in PathsOf(Chosen)) chosen.Add(FrontRoomsInput.Normalise(p));
        var target = State == CaptureState.Listening && Chosen.Binds.Length > 0 ? FrontRoomsInput.Normalise(FrontRoomsInput.PathOf(Chosen.Binds[step])) : null;
        foreach (var v in keys)
        {
            var path = FrontRoomsInput.Normalise(v.Def.Path);
            v.Chosen.To(chosen.Contains(path) ? 1f : 0f, Reduced ? 0f : .16f, Ease.OutCubic);
            // C1: the key being changed empties to a yellow keyline. Asking: the other action's key is outlined too.
            v.Drain.To(path == target ? 1f : 0f, Reduced ? 0f : .12f, Ease.OutCubic);
            v.Asked.To(State == CaptureState.Asking && path == FrontRoomsInput.Normalise(askPath) ? 1f : 0f, Reduced ? 0f : .12f, Ease.OutCubic);
        }
        foreach (var m in mouse)
        {
            if (m.Path == "ring") continue;
            var path = FrontRoomsInput.Normalise(m.Path);
            m.Chosen.To(chosen.Contains(path) ? 1f : 0f, Reduced ? 0f : .16f, Ease.OutCubic);
            m.Drain.To(path == target ? 1f : 0f, Reduced ? 0f : .12f, Ease.OutCubic);
        }
        if (padBoard) RefreshPads(chosen, target);
        var moved = AssignSlots();
        if (ts < 1f && Declutter()) moved = true;
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
        // Straight leaders (a key in its own slot) first, then the routed ones around them; on touch the level ones before the
        // bent ones, the least bend first, so a bend keeps off every leader already drawn (LeaderTo).
        if (ts < 1f) ByBend(pending, grow);
        for (var pass = 0; pass < 2; pass++)
            for (var i = 0; i < pending.Count; i++)
            {
                var routeTo = LeaderTarget(pending[i]);
                if ((routeTo == FrontRoomsInput.Normalise(pending[i].Slot.DefaultTarget)) == (pass == 0)) Route(pending[i], routeTo, grow[i]);
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

    // A label's bar, 6 over and 8 under its words: two lines (the action, its note) or the action alone.
    float BarHeight => (showNotes ? 53f : 36f) * ts;

    // Touch: where the action word's caps' middle sits under its words' top, in em of its size (Bayon's caps run from .509 to
    // 1.215 em: 8.65 to 20.65 pt at the touch 17 pt, measured on the 11-04 frames). A leader meets its label there, and the bar
    // of the action word alone is even about its caps.
    const float CapMiddle = .862f;
    float CapMiddlePx => ActionSize * ts * CapMiddle;

    /// <summary>Touch: the leader's row under its label's words' top, in whole px: the 1 px line on the caps' middle.</summary>
    float LeaderEntry => Mathf.Round(CapMiddlePx - .5f);

    // Touch: where a control's edge is not square on its label's own row (a pill's round end), a label within this many px of the
    // control's middle row keeps a level leader on that row, which still meets its caps (12 pt tall); further, the leader bends
    // (LeaderTo).
    const float LeaderSlack = 3f;

    /// <summary>A label's bar top, its words' top at px <paramref name="y"/>: 4·ts over the words, or on touch with the action word
    /// alone (the phone, VL498) even about its caps, 7.9 pt over and under them (11-04 r0: 11.8 over and 4.4 under).</summary>
    float BarTop(float y) => ts < 1f && !showNotes ? y + CapMiddlePx - BarHeight * .5f : y - 4f * ts;

    /// <summary>
    /// Touch (ts < 1, 平面视觉 VL498 and 11-04): the words keep their size while the drawing shrinks, so labels can meet. From the
    /// slots' own places (AssignSlots), at the drawing's scale now: a label on a level leader first sits with its caps' middle on
    /// that leader's row (11-04 r0: on the phone the leaders met the labels 4-5 pt over their caps, SETTINGS' 6 pt). Then every
    /// label, top to bottom, goes down to at least the bar foot + 4·ts of each label above it whose bar shares some of its width
    /// (Stack: a side's column, and SETTINGS over the pad against ZOOM once the words sit on their leaders at a small scale); a
    /// label against a drawn part goes off it (OffParts) and the labels are stacked again: four stackings at most, and a stacking
    /// always last. The type stays its size. Each leader's label end (From) then sits on its words' caps' middle, and the leaders
    /// are routed after this, from there to their own parts (LeaderTo). Every label stays in Bounds, which the touch sheet fits to
    /// its list (the whole map, no scroll). True when a label is not at its slot's own place.
    /// </summary>
    bool Declutter()
    {
        foreach (var c in callouts)
            if (Level(c.Slot)) c.LabelY = (PartRow(c.Slot.DefaultTarget, c.From.y) - LeaderEntry) / Mathf.Max(.01f, k);
        for (var round = 0; ; round++)
        {
            Stack();
            if (round == 3 || !OffParts()) break;
        }
        var moved = false;
        foreach (var c in callouts)
        {
            c.From = new Vector2(c.From.x, (Mathf.Round(c.LabelY * k) + LeaderEntry) / Mathf.Max(.01f, k));
            if (!Mathf.Approximately(c.LabelY, c.Slot.OwnY) || c.From != c.Slot.OwnFrom) moved = true;
        }
        return moved;
    }

    // A slot whose own leader is level (a side's label to its control; not SETTINGS' from above).
    static bool Level(Callout slot) => Mathf.Abs(slot.OwnFrom.y - slot.OwnTo.y) < .01f;

    // Every label, top to bottom: at least the bar foot + 4·ts of each label above it whose bar shares some of its width, in whole
    // px, its leader's label end with it. True when one moved.
    bool Stack()
    {
        var moved = false;
        foreach (var c in callouts) LayoutLabel(c);
        var order = new List<Callout>(callouts);
        order.Sort((a, b) =>
        {
            var by = Mathf.Round(a.LabelY * k).CompareTo(Mathf.Round(b.LabelY * k));
            return by != 0 ? by : callouts.IndexOf(a).CompareTo(callouts.IndexOf(b));
        });
        for (var i = 0; i < order.Count; i++)
        {
            var c = order[i];
            var y = Mathf.Round(c.LabelY * k);
            var need = 0f;
            for (var j = 0; j < i; j++)
                if (Shares(c, order[j])) need = Mathf.Max(need, BarTop(Mathf.Round(order[j].LabelY * k)) + BarHeight + 4f * ts - BarTop(y));
            if (need <= .001f) continue;
            var to = (y + Mathf.Ceil(need - .001f)) / Mathf.Max(.01f, k);
            c.From = new Vector2(c.From.x, c.From.y + to - c.LabelY);
            c.LabelY = to;
            moved = true;
        }
        return moved;
    }

    // Two labels' bars share some width (as last laid out).
    static bool Shares(Callout a, Callout b) => a.Hit.xMin < b.Hit.xMax && b.Hit.xMin < a.Hit.xMax;

    /// <summary>
    /// Touch (平面视觉 VL498: on the phone SETTINGS' foot sat under RT's paper): a label whose bar meets a drawn part, or comes
    /// within 4·ts of it above or below, goes off it. Up when its bar's middle is above the part's, its foot 4·ts over the part, if
    /// it stays off every part there and 4·ts clear of every label whose bar shares its width; else down, its top 4·ts under the
    /// part. In whole px, its leader's label end with it. True when one moved.
    /// </summary>
    bool OffParts()
    {
        var moved = false;
        foreach (var c in callouts)
        {
            // Its bar's width as it will be drawn: a slot or a note may have changed since it was last laid out.
            LayoutLabel(c);
            var y = Mathf.Round(c.LabelY * k);
            for (var tries = 0; tries < 8; tries++)
            {
                if (!PartAt(c, y, out var part)) break;
                // Its words' top where the bar's top is 4·ts under the part, or its foot 4·ts over it.
                var ny = Mathf.Ceil(part.yMax + 4f * ts - BarTop(0f));
                var up = Mathf.Floor(part.yMin - 4f * ts - BarTop(0f) - BarHeight);
                if (BarTop(y) + BarHeight * .5f < part.center.y && !PartAt(c, up, out _) && ClearOfLabels(c, up)) ny = up;
                var to = ny / Mathf.Max(.01f, k);
                c.From = new Vector2(c.From.x, c.From.y + to - c.LabelY);
                c.LabelY = to;
                y = ny;
                moved = true;
            }
        }
        return moved;
    }

    // The label's bar, its words' top at px y, 4·ts clear of the bar of every other label that shares its width.
    bool ClearOfLabels(Callout c, float y)
    {
        var top = BarTop(y);
        foreach (var o in callouts)
        {
            if (o == c || !Shares(c, o)) continue;
            var other = BarTop(Mathf.Round(o.LabelY * k));
            if (other < top + BarHeight + 4f * ts - .01f && top < other + BarHeight + 4f * ts - .01f) return false;
        }
        return true;
    }

    // The first drawn part the label's bar meets, its words' top at px y, or comes within 4·ts of above or below: in whole px
    // as drawn, sharing some of the bar's width.
    bool PartAt(Callout c, float y, out Rect part)
    {
        var top = BarTop(y);
        var foot = top + BarHeight;
        var x0 = c.Hit.x * k;
        var x1 = c.Hit.xMax * k;
        foreach (var r in DrawnParts())
        {
            part = r;
            if (x1 > part.xMin && x0 < part.xMax && foot + 4f * ts > part.yMin + .01f && top - 4f * ts < part.yMax - .01f) return true;
        }
        part = default;
        return false;
    }

    // What a label and a leader stay off on touch, in whole px as drawn: the pad's controls and the keycaps (the d-pad's, the
    // keyboard's keys), the mouse's buttons and body; never a decoration (the pad's body and stick wells, a 25 % keyline under it
    // all) or the wheel's ring.
    IEnumerable<Rect> DrawnParts()
    {
        foreach (var v in keys) yield return Snap(v.Home, v.Axis);
        foreach (var m in mouse) if (m.Path != "ring") yield return Whole(m.Rect);
        foreach (var part in padParts) if (!part.Decoration) yield return Snap(part.Home, part.Axis);
    }

    // A rect of map units in whole px, each edge rounded (as PlaceUnits draws it).
    Rect Whole(Rect r) => Rect.MinMaxRect(Mathf.Round(r.x * k), Mathf.Round(r.y * k), Mathf.Round(r.xMax * k), Mathf.Round(r.yMax * k));

    /// <summary>Touch: a target's control in whole px as drawn (its keycap, mouse part or pad control), with the well round it for
    /// a stick (<paramref name="ring"/>: a bend keeps outside it) and its corners' <paramref name="radius"/> in px (a keycap's none,
    /// a mouse part's taken as round); false for none.</summary>
    bool DrawnAt(string path, out Rect drawn, out Rect ring, out float radius)
    {
        foreach (var v in keys) if (FrontRoomsInput.Normalise(v.Def.Path) == path) { drawn = ring = Snap(v.Home, v.Axis); radius = 0f; return true; }
        foreach (var m in mouse)
            if (m.Path != "ring" && FrontRoomsInput.Normalise(m.Path) == path) { drawn = ring = Whole(m.Rect); radius = Mathf.Min(drawn.width, drawn.height) * .5f; return true; }
        foreach (var part in padParts)
        {
            if (part.Decoration || !part.Has(path)) continue;
            drawn = ring = Snap(part.Home, part.Axis);
            radius = part.Radius * k;
            foreach (var d in padParts)
            {
                if (!d.Decoration || !d.Home.Contains(part.Home.center) || d.Home.width * d.Home.height >= 4f * part.Home.width * part.Home.height) continue;
                var well = Snap(d.Home, d.Axis);
                ring = Rect.MinMaxRect(Mathf.Min(ring.xMin, well.xMin), Mathf.Min(ring.yMin, well.yMin), Mathf.Max(ring.xMax, well.xMax), Mathf.Max(ring.yMax, well.yMax));
            }
            return true;
        }
        drawn = ring = default;
        radius = 0f;
        return false;
    }

    // Touch: the middle row (px) of a target's control as drawn, where its label's caps sit (Declutter); else the row of map units y.
    float PartRow(string target, float y) =>
        DrawnAt(FrontRoomsInput.Normalise(target), out var r, out _, out _) ? r.y + Mathf.Floor((r.height - 1f) * .5f) : Mathf.Round(y * k);

    /// <summary>Touch: how far inside its drawn rect (whole px, corners of <paramref name="radius"/> px) a control's edge stands on
    /// px row <paramref name="y"/>, at the row's middle: 0 where the edge runs straight, MaxValue on a row outside it.</summary>
    static float EdgeInset(Rect part, float radius, float y)
    {
        var t = Mathf.Min(y + .5f - part.yMin, part.yMax - (y + .5f));
        if (t <= 0f) return float.MaxValue;
        var r = Mathf.Min(radius, Mathf.Min(part.width, part.height) * .5f);
        if (t >= r) return 0f;
        var d = r - t;
        return r - Mathf.Sqrt(r * r - d * d);
    }

    /// <summary>
    /// Touch (ts < 1, 平面视觉 VL498 and 11-04: a legend fits its part at any scale, and one under a 5 pt cap height is not drawn):
    /// the legends keep one size across the board, as the desktop's Plex Mono 13 do, never more than a drawn one's cap holds: the
    /// largest whole size, at most 13, at which each legend shown fits its cap as drawn (LegendFit). A legend whose cap cannot hold a
    /// legible size drops out and binds no other (the shoulders' 23-unit pills go first: 8 pt at the phone's k ≈ .35, out below
    /// k ≈ .32). A PlayStation pad's ✕ ○ □ △ are its face buttons' legends: drawn at the same size (PlacePad; the ✕ stands .69 of
    /// its box, as the caps .698 em), in the same fit, dropping out the same way (11-04 fix1: drawn with the drawing, 13 units, at
    /// the phone's k ≈ .35 the ✕ stood 3.5 pt). Again at each scale, and whenever the pad's names change.
    /// </summary>
    void FitLegends()
    {
        var size = 13;
        foreach (var v in keys)
            if (v.Legend != null) { var fit = LegendFit(v.Legend.text, Snap(v.Home, v.Axis), 0f); if (Legible(fit)) size = Mathf.Min(size, fit); }
        foreach (var part in padParts)
        {
            if (part.Legend != null && LegendShown(part)) { var fit = LegendFit(part.Legend.text, Snap(part.Home, part.Axis), part.Radius); if (Legible(fit)) size = Mathf.Min(size, fit); }
            if (part.Glyph != null && legendsPlayStation) { var fit = GlyphFit(Snap(part.Home, part.Axis)); if (Legible(fit)) size = Mathf.Min(size, fit); }
        }
        legendSize = size;
        foreach (var v in keys)
        {
            if (v.Legend == null) continue;
            v.Legend.fontSize = size;
            v.Legend.enabled = Legible(LegendFit(v.Legend.text, Snap(v.Home, v.Axis), 0f));
        }
        foreach (var part in padParts)
        {
            if (part.Legend != null)
            {
                part.Legend.fontSize = size;
                part.Legend.enabled = LegendShown(part) && Legible(LegendFit(part.Legend.text, Snap(part.Home, part.Axis), part.Radius));
            }
            if (part.Glyph != null) part.Glyph.enabled = legendsPlayStation && Legible(GlyphFit(Snap(part.Home, part.Axis)));
        }
    }

    // Touch: the legends' one size (FitLegends), a PlayStation pad's marks' too (PlacePad).
    int legendSize = 13;

    // The least legend (平面视觉 11-04): a cap height under 5 pt (15 px at @3, 10 at @2), rounded down, is not drawn. Plex Mono's
    // caps are .698 em, so 8 pt is the least size drawn (7 pt's caps are 4.9 pt); a PlayStation mark's ✕ (9 of its 13 units) is
    // as tall to within .006 em, so the same size holds for the marks.
    const float LegendCaps = 5f, PlexCaps = .698f;
    static bool Legible(int size) => size * PlexCaps >= LegendCaps;

    // A PlayStation mark's farthest ink from its middle, in its box's size: the triangle's lower corners, with the half-unit stroke
    // and the anti-aliased edge (GlyphMask: 7.07 of 13 units).
    const float GlyphReach = 7.07f / 13f;

    /// <summary>The largest whole size, at most 13, at which a PlayStation mark drawn about the middle of the button at
    /// <paramref name="px"/> (whole px; half a px off it where their sizes' parities differ) keeps 1 px inside its edge (a keyline
    /// lies in that px); 0 when none does.</summary>
    static int GlyphFit(Rect px)
    {
        var room = Mathf.Min(px.width, px.height) * .5f - 1f;
        for (var size = 13; size > 0; size--)
        {
            var off = (Mathf.RoundToInt(px.width) - size) % 2 != 0 || (Mathf.RoundToInt(px.height) - size) % 2 != 0 ? .5f : 0f;
            if (size * GlyphReach + off <= room) return size;
        }
        return 0;
    }

    /// <summary>The largest whole size, at most 13, at which a Plex Mono legend's glyph box (0.6 em a character, its advance; 0.71 em
    /// tall, the caps' 0.698 and the round letters' and arrows' 0.012 overshoot) sits centred in the cap drawn at <paramref name="px"/>
    /// (whole px, with corners of <paramref name="radius"/> map units) 1 px inside its edge and clear of its rounded corners (a
    /// keyline lies in that px); 0 when none does. Rounded down: never a size that would not fit.</summary>
    int LegendFit(string text, Rect px, float radius)
    {
        var w = px.width - 2f;
        var h = px.height - 2f;
        var r = Mathf.Clamp(radius * k - 1f, 0f, Mathf.Max(0f, Mathf.Min(w, h) * .5f));
        for (var size = 13; size > 0; size--)
        {
            // Half the glyph box, and how far it reaches into a corner's square.
            var bw = text.Length * .6f * size * .5f;
            var bh = .71f * size * .5f;
            var ex = bw - (w * .5f - r);
            var ey = bh - (h * .5f - r);
            if (bw <= w * .5f && bh <= h * .5f && (ex <= 0f || ey <= 0f || ex * ex + ey * ey <= r * r)) return size;
        }
        return 0;
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
        if (Closing && fade.Done) { Closing = false; root.gameObject.SetActive(false); return; }
        group.alpha = fade.Current;
        UpdateReveal();
        var now = FrontRoomsTouchMotion.Now;
        foreach (var v in keys)
        {
            var path = FrontRoomsInput.Normalise(v.Def.Path);
            var inUse = bound.Contains(path) || v.Chosen.Current > .001f;
            var chosen = v.Chosen.Current;
            var drain = v.Drain.Current;
            // A key in use shows its keyline until its fill has come in.
            var fillAlpha = (inUse ? 1f : 0f) * (1f - drain) * fillsIn;
            var fill = Color.Lerp(paper, accent, chosen);
            v.Fill.color = new Color(fill.r, fill.g, fill.b, fillAlpha);
            var outline = Mathf.Max(drain, v.Asked.Current);
            var edge = outline > .001f ? new Color(accent.r, accent.g, accent.b, outline) : new Color(paper.r, paper.g, paper.b, (inUse ? 1f - fillsIn : 1f) * .25f * keysIn);
            foreach (var e in v.Edges) e.color = edge;
            if (v.Legend != null) v.Legend.color = WordOn(fillAlpha, drain);
            PlaceKey(v, v.Home, v.Pop.Current);
        }
        foreach (var m in mouse)
        {
            if (m.Path == "ring") { m.Image.color = new Color(ink.r, ink.g, ink.b, keysIn); continue; }
            var path = FrontRoomsInput.Normalise(m.Path);
            var inUse = bound.Contains(path) || path == FrontRoomsInput.Normalise(MouseLook);
            var c = Color.Lerp(paper, accent, Mathf.Max(m.Chosen.Current, m.Drain.Current));
            var alpha = m.Outline ? (inUse ? .5f : .25f) : (inUse ? Mathf.Lerp(.25f, 1f, fillsIn) : .25f) * (1f - .75f * m.Drain.Current);
            if (m.Chosen.Current > .5f) alpha = 1f;
            m.Image.color = new Color(c.r, c.g, c.b, alpha * keysIn);
        }
        if (padBoard) AnimatePads();
        foreach (var c in callouts)
        {
            // The note pulses (1 Hz) while it waits for a key: C1, the word itself moves, nothing is added.
            var listening = c == Chosen && State != CaptureState.Idle;
            var pulse = listening && !Reduced ? .55f + .45f * (.5f + .5f * Mathf.Cos((now - listenStarted) * Mathf.PI * 2f)) : 1f;
            // The label fades in as one (bar, word, note, padlock, changed dot); on its bar the type is ink. A fixed action reads
            // quieter, its padlock says why.
            var hot = c.Bar.Current;
            var type = Color.Lerp(paper, ink, hot);
            c.Under.color = new Color(accent.r, accent.g, accent.b, hot * calloutsIn);
            c.NoteText.color = new Color(type.r, type.g, type.b, (listening ? pulse : Mathf.Lerp(.8f, 1f, hot)) * calloutsIn);
            c.ActionText.color = new Color(type.r, type.g, type.b, (c.IsFixed ? Mathf.Lerp(.62f, 1f, hot) : 1f) * calloutsIn);
            foreach (var l in c.Lock) l.color = new Color(type.r, type.g, type.b, .75f * calloutsIn);
            c.Dot.color = new Color(type.r, type.g, type.b, Changed(c) ? .8f * calloutsIn : 0f);
            // A flash (FIXED, CONNECT A CONTROLLER, X IS FIXED) is drawn while it lasts, in any state, and cleared once.
            var flashing = c == Chosen && message != null;
            if (flashing && FrontRoomsTouchMotion.Now >= messageUntil) message = null;
            if (listening || flashing) LayoutLabel(c);
            DrawLeader(c);
        }
    }

    // ------------------------------------------------------------- capture (remapping)

    /// <summary>Enter on the chosen action: start listening for its new key; false (and a FIXED note) for a fixed one, and on
    /// the CONTROLLER page with no pad (CONNECT A CONTROLLER: only a pad's buttons are captured there).</summary>
    public bool BeginCapture()
    {
        if (Chosen.IsFixed) { Flash(FrontRoomsLanguage.T("FIXED")); return false; }
        if (padBoard && Gamepad.current == null) { Flash(FrontRoomsLanguage.T(BrowserPads ? "PRESS A BUTTON" : "CONNECT A CONTROLLER")); return false; }
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
        message = null;
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
    /// One frame of listening (the page calls it instead of its own keys): Esc cancels; a key or mouse button (on the
    /// CONTROLLER page a pad button) is checked and bound, or (taken by another action) asked about: Enter, a click or a
    /// pad's A swaps, Esc, a pad's MENU or B cancels. A pad's MENU (never bound) cancels on both pages, B too on the keyboard
    /// page, where no pad button binds; these cancel at once, also before listening starts. Listening starts only once the
    /// key, click or button that opened it is released (research: never capture the opening press; on the keyboard page only
    /// a pad's A counts). The pad unplugged while the CONTROLLER page listens or asks ends it.
    /// </summary>
    public void UpdateCapture()
    {
        if (State == CaptureState.Idle) return;
        var kb = Keyboard.current;
        var ms = Mouse.current;
        var gp = Gamepad.current;
        // The pad was unplugged while the pad page listened or asked: clicks are ignored while capturing and no pad button
        // can answer, so the capture ends and says why (2026-10-31).
        if (padBoard && gp == null) { CancelCapture(); Flash(FrontRoomsLanguage.T("CONNECT A CONTROLLER")); return; }
        // Esc or a pad's MENU (never bound) cancels; so does B while asking, and on the keyboard page, where no pad button binds.
        // Before the opening press is released too (2026-10-31): none of them can be that press (Esc and MENU go to the sheet's
        // ConsumeEscape, B closes an idle page), and a pad held down must never leave Esc unheard.
        var padBack = gp != null && FrontRoomsInput.PadControl(gp, FrontRoomsInput.BackButton).wasPressedThisFrame && (!padBoard || State == CaptureState.Asking);
        if ((kb != null && kb.escapeKey.wasPressedThisFrame) || (gp != null && gp.startButton.wasPressedThisFrame) || padBack) { CancelCapture(); return; }
        if (!armed)
        {
            // The pad page waits for every pad button; the keyboard page only for the pad's A that may have opened it, so a pad
            // resting on a trigger never keeps the keys from being heard (2026-10-31).
            var padDown = padBoard ? FrontRoomsInput.PadButtonHeld(gp) : gp != null && FrontRoomsInput.PadControl(gp, FrontRoomsInput.ConfirmButton).isPressed;
            var held = (kb != null && kb.anyKey.isPressed) || (ms != null && (ms.leftButton.isPressed || ms.rightButton.isPressed || ms.middleButton.isPressed)) || padDown;
            if (!held) armed = true;
            return;
        }
        if (State == CaptureState.Asking)
        {
            var yes = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                || (ms != null && ms.leftButton.wasPressedThisFrame) || (gp != null && FrontRoomsInput.PadControl(gp, FrontRoomsInput.ConfirmButton).wasPressedThisFrame);
            if (yes) { Commit(true); }
            return;
        }
        var path = padBoard ? FrontRoomsInput.PressedPadPath(gp) : FrontRoomsInput.PressedKeyPath(kb, ms);
        if (path == null) return;
        var b = Chosen.Binds[step];
        var check = FrontRoomsInput.CheckBind(b, path, out var owner, out var lockedName);
        switch (check)
        {
            case BindCheck.Reserved: Flash(FrontRoomsLanguage.F("{0} IS FIXED", FrontRoomsInput.KeyName(path))); return;
            case BindCheck.Locked: Flash(FrontRoomsLanguage.F("{0} IS {1}'S", FrontRoomsInput.KeyName(path)).Replace("{1}", FrontRoomsLanguage.T(lockedName))); return;
            case BindCheck.Taken:
            {
                // A swap would hand this action's key to the owner, and a locked key (ZOOM's LEFT CLICK, the pad SPRINT's L3)
                // may not go to it (2026-10-31): no question; the flash says whose key it is, and nothing changes.
                var mine = FrontRoomsInput.PathOf(b);
                if (FrontRoomsInput.CheckBind(owner, mine, out _, out var holder) == BindCheck.Locked)
                {
                    Flash(FrontRoomsLanguage.F("{0} IS {1}'S", FrontRoomsInput.KeyName(mine)).Replace("{1}", FrontRoomsLanguage.T(holder)));
                    return;
                }
                // The question replaces a flash still showing.
                message = null;
                askOwner = owner; askPath = path;
                State = CaptureState.Asking;
                listenStarted = FrontRoomsTouchMotion.Now;
                Refresh();
                return;
            }
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
        foreach (var part in padParts) if (!part.Decoration && part.Has(p)) part.Pop.Restart(.92f, 1f, .1f, Ease.OutBack);
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

    // ------------------------------------------------------------- the detail column

    public string Title => FrontRoomsLanguage.T(Chosen.Action);

    public string Body
    {
        get
        {
            if (padBoard) return PadBody();
            var c = Chosen;
            if (State == CaptureState.Asking)
                return FrontRoomsLanguage.F("{0} is {1}'s key. Swap them? {2} takes {0} and {1} takes {3}.", FrontRoomsInput.KeyName(askPath))
                    .Replace("{1}", FrontRoomsLanguage.T(ActionOf(askOwner))).Replace("{2}", FrontRoomsLanguage.T(ActionOf(c.Binds[step])))
                    .Replace("{3}", FrontRoomsInput.KeyName(FrontRoomsInput.PathOf(c.Binds[step])));
            if (State == CaptureState.Listening)
                return c.Binds.Length > 1
                    ? FrontRoomsLanguage.F(step == 0 ? "Press the key for {0}, then LEFT, BACK and RIGHT. Esc stops; the keys already set stay." : "Press the key for {0}. Esc stops; the keys already set stay.", FrontRoomsLanguage.T(Part(c.Binds[step])))
                    : FrontRoomsLanguage.F("Press the key or mouse button you want for {0}. Esc cancels; it keeps {1}.", FrontRoomsLanguage.T(c.Action)).Replace("{1}", FrontRoomsInput.KeyName(FrontRoomsInput.PathOf(c.Binds[0])));
            return FrontRoomsLanguage.T(c.Explain);
        }
    }

    /// <summary>A row of the detail column's monospaced table: the label through the table, the value's column kept (a CJK
    /// label by its measured width, 2026-10-31).</summary>
    static string Row(string label) => FrontRoomsLanguage.MetaColumn(FrontRoomsLanguage.T(label));

    // The detail column's key words for the device in use (a pad's buttons while one is in use, 2026-10-31).
    // The menus' own buttons (D6: a Nintendo pad confirms on its A, east, and goes back on its B, south).
    static string ConfirmName => FrontRoomsInput.PadInUse ? FrontRoomsInput.PadName(FrontRoomsInput.ConfirmButton) : "ENTER";
    static string CancelName => FrontRoomsInput.PadInUse ? FrontRoomsInput.PadName(FrontRoomsInput.BackButton) : "ESC";
    static string ResetName => FrontRoomsInput.PadInUse ? FrontRoomsInput.PadName(FrontRoomsInput.ResetButton) : "BACKSPACE";
    static string Col(string control) => FrontRoomsLanguage.MetaColumn(control);

    public string Meta
    {
        get
        {
            if (padBoard) return PadMeta();
            var c = Chosen;
            if (State == CaptureState.Asking) return Col(ConfirmName) + FrontRoomsLanguage.T("SWAP") + "\n" + Col(CancelName) + FrontRoomsLanguage.T("CANCEL");
            if (State == CaptureState.Listening) return Row("NOW") + FrontRoomsInput.KeyName(FrontRoomsInput.PathOf(c.Binds[step]));
            if (c.IsFixed) return Row("FIXED") + FrontRoomsLanguage.T(c.FixedNote);
            var now = new List<string>();
            var defaults = new List<string>();
            foreach (var b in c.Binds) { now.Add(FrontRoomsInput.KeyName(FrontRoomsInput.PathOf(b))); defaults.Add(FrontRoomsInput.KeyName(FrontRoomsInput.DefaultPathOf(b))); }
            var meta = Row(c.Binds.Length > 1 ? "KEYS" : "KEY") + string.Join(" ", now.ToArray());
            if (c.Action == "SPRINT") meta += "\n" + Row("ALSO") + FrontRoomsLanguage.F("RIGHT {0} (LOCKED)", FrontRoomsInput.KeyName(FrontRoomsInput.SprintLockPath));
            if (c.Action == "SLOW") meta += "\n" + Row("ALSO") + FrontRoomsLanguage.T("RIGHT SHIFT (LOCKED)");
            if (c.Action == "MOVE") meta += "\n" + Row("ALSO") + FrontRoomsLanguage.T("THE ARROW KEYS (LOCKED)");
            if (c.Action == "ZOOM") meta += "\n" + Row("ALSO") + FrontRoomsLanguage.F("{0} (LOCKED)", FrontRoomsInput.KeyName(LeftClick));
            if (c.Action == "USE") meta += "\n" + Row("IN A CLOSE-UP") + FrontRoomsLanguage.F("{0} OR ESC BACK", FrontRoomsInput.KeyName(FrontRoomsInput.PathOf(Bind.Use)) + ", " + FrontRoomsInput.KeyName(FrontRoomsInput.StepBackPath));
            meta += Changed(c) ? "\n" + Row("DEFAULT") + string.Join(" ", defaults.ToArray()) + "\n" + Col(ResetName) + FrontRoomsLanguage.T("BACK TO DEFAULT") : "\n" + Col(ConfirmName) + FrontRoomsLanguage.T("CHANGE");
            return meta;
        }
    }

    // ------------------------------------------------------------- choosing

    /// <summary>← →: the next action left to right, by the key or button each one names (Red: "从左往右"), stopping at the ends.</summary>
    public void Step(int direction)
    {
        var order = new List<int>();
        for (var i = 0; i < callouts.Count; i++) order.Add(i);
        order.Sort((a, b) =>
        {
            var by = TargetRect(LeaderTarget(callouts[a])).center.x.CompareTo(TargetRect(LeaderTarget(callouts[b])).center.x);
            return by != 0 ? by : a.CompareTo(b);
        });
        var at = order.IndexOf(Focus) + direction;
        if (at >= 0 && at < order.Count) SetFocus(order[at], false);
    }

    /// <summary>Arrows, W A S D: the nearest action that way (reading the labels as points on the map).</summary>
    public void Navigate(Vector2 direction)
    {
        var here = NavPoint(Chosen);
        var best = -1;
        var bestCost = float.MaxValue;
        for (var i = 0; i < callouts.Count; i++)
        {
            if (i == Focus) continue;
            var d = NavPoint(callouts[i]) - here;
            var along = Vector2.Dot(d, direction);
            if (along <= 1f) continue;
            var across = Mathf.Abs(d.x * direction.y - d.y * direction.x);
            var cost = along + 2f * across;
            if (cost < bestCost) { bestCost = cost; best = i; }
        }
        if (best >= 0) SetFocus(best, false);
    }

    // A label as a point for choosing: its centre; on the pad board the edge its column hangs from (left texts end at x 308,
    // right ones start at 868), so ← and → cross to the other column instead of to a narrower note in the same one (2026-10-31).
    Vector2 NavPoint(Callout c) => padBoard ? new Vector2(c.LabelX, c.Hit.center.y) : c.Hit.center;

    /// <summary>The action under a point of the map (its label, or one of its keys or buttons), or -1.</summary>
    public int ActionAt(Vector2 mapPoint)
    {
        for (var i = 0; i < callouts.Count; i++) if (callouts[i].Hit.Contains(mapPoint)) return i;
        string path = null;
        foreach (var v in keys) if (v.Home.Contains(mapPoint)) { path = v.Def.Path; break; }
        if (path == null) foreach (var m in mouse) if (m.Path != "ring" && m.Rect.Contains(mapPoint)) { path = m.Path; break; }
        // The pad board (2026-10-31): a part's paths in order, primary first: the left stick is MOVE before SPRINT's L3.
        if (path == null && padBoard)
            foreach (var part in padParts)
            {
                if (part.Decoration || !part.Home.Contains(mapPoint)) continue;
                foreach (var n in part.Paths)
                    for (var i = 0; i < callouts.Count; i++)
                        foreach (var q in PathsOf(callouts[i]))
                            if (FrontRoomsInput.Normalise(q) == n) return i;
                return -1;
            }
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
        // A key with its own slot's default: the drawn straight leader of that slot; on touch from wherever Declutter put its label
        // (LeaderTo).
        if (target == FrontRoomsInput.Normalise(c.Slot.DefaultTarget)) { if (ts < 1f) LeaderTo(c, target); else Seg(c, c.From, c.To); }
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

    /// <summary>
    /// Touch (平面视觉 11-04: every leader starts at its own part and ends at its own label, re-routed after Declutter's pushes, and
    /// no label or its bar crosses another label's leader; 11-04 r0: on the phone USE's and LOOK's leaders stayed on their buttons'
    /// rows, so A's ended by BACK and the right stick's ran into USE's bar): a slot's leader in whole px, from its label end (From,
    /// the words' caps middle) to its control's edge, or to its middle column for one from above (SETTINGS to MENU). Level on the
    /// label's own row where the control's edge stands square to it there (within a px of the drawn rect's: a keycap's whole
    /// height, a button's middle; 11-04 fix1: ZOOM STEP, moved 2 px down, kept its leader on the d-pad's middle row, over its
    /// caps' middle); else on the control's middle row, within LeaderSlack of the label's; else bent near the control: out of it
    /// along its row, past its well, down (or up) to the label's row and across into the label, at the first column from the
    /// control out where no run meets another control, another label's bar or another leader, nor the bend or the stub its own bar.
    /// With no such column, the A* route (FindRoute), as a moved key's.
    /// </summary>
    void LeaderTo(Callout c, string target)
    {
        if (!DrawnAt(target, out var part, out var ring, out var radius)) { Seg(c, c.From, c.To); return; }
        var q = new Vector2(Mathf.Round(c.From.x * k), Mathf.Round(c.From.y * k));
        if (Mathf.Abs(c.From.x - c.To.x) < .01f)
        {
            var col = part.x + Mathf.Floor((part.width - 1f) * .5f);
            var edge = q.y < part.y ? part.y : part.yMax;
            AddRun(c, new Rect(col, Mathf.Min(q.y, edge), 1f, Mathf.Max(1f, Mathf.Abs(q.y - edge))));
            return;
        }
        var right = q.x > part.center.x;
        var px = right ? part.xMax : part.x;
        var row = part.y + Mathf.Floor((part.height - 1f) * .5f);
        // On its own row the run reaches into the px the edge stands in, so it meets the control's ink.
        var inset = EdgeInset(part, radius, q.y);
        if (inset <= 1f && LevelRun(c, part, q, right ? px - Mathf.Round(inset) : px + Mathf.Round(inset), q.y)) return;
        if (Mathf.Abs(q.y - row) <= LeaderSlack && LevelRun(c, part, q, px, row)) return;
        var bar = new Rect(c.Hit.x * k, c.Hit.y * k, c.Hit.width * k, c.Hit.height * k);
        // The stub out of the control: as many px clear of it (and of a stick's well) before the bend, and the bend as many px
        // short of its own bar.
        var stub = Mathf.Max(3f, Mathf.Round(6f * ts));
        var first = right ? Mathf.Max(px, ring.xMax) + stub : Mathf.Min(px, ring.x) - 1f - stub;
        var last = right ? Mathf.Floor(bar.x) - 1f - stub : Mathf.Ceil(bar.xMax) + stub;
        for (var e = first; right ? e <= last : e >= last; e += right ? 1f : -1f)
        {
            var runs = Bent(q, px, row, e, right);
            if (!RunsClear(c, part, runs, true)) continue;
            foreach (var r in runs) AddRun(c, r);
            return;
        }
        var path = FindRoute(c, target) ?? Orthogonal(new List<Vector2> { c.From, Centre(target) });
        for (var i = 1; i < path.Count; i++) Seg(c, path[i - 1], path[i]);
    }

    // A level leader in whole px from the label end q to the control's edge at x, on row y: drawn when it runs clear (RunsClear).
    bool LevelRun(Callout c, Rect part, Vector2 q, float x, float y)
    {
        var run = new[] { new Rect(Mathf.Min(x, q.x), y, Mathf.Abs(q.x - x), 1f) };
        if (!RunsClear(c, part, run, false)) return false;
        AddRun(c, run[0]);
        return true;
    }

    /// <summary>An elbow in whole px from the label end <paramref name="q"/> to the control's edge <paramref name="px"/> on its
    /// <paramref name="row"/>, bent at column <paramref name="e"/>: the run from the label, the bend, the stub out of the control.
    /// Every px once (the line is drawn at .6: a px drawn twice would show as a dot), the corners included.</summary>
    static Rect[] Bent(Vector2 q, float px, float row, float e, bool right)
    {
        var down = q.y > row;
        if (right)
            return new[]
            {
                // The corner on the label's row is the run's (from e), the one on the control's row the bend's.
                new Rect(e, q.y, q.x - e, 1f),
                down ? new Rect(e, row, 1f, q.y - row) : new Rect(e, q.y + 1f, 1f, row - q.y),
                new Rect(px, row, e - px, 1f),
            };
        return new[]
        {
            // The corner on the label's row is the bend's, the one on the control's row the stub's (from e).
            new Rect(q.x, q.y, e - q.x, 1f),
            down ? new Rect(e, row + 1f, 1f, q.y - row) : new Rect(e, q.y, 1f, row - q.y),
            new Rect(e, row, px - e, 1f),
        };
    }

    // A run of whole px into the leader's segments, in map units as Seg's (DrawLeader draws it back at these px): a horizontal one
    // 1 px tall, a vertical one 1 px wide.
    void AddRun(Callout c, Rect px) =>
        c.SegRects.Add(px.width >= px.height ? new Rect(px.x / k, px.y / k, px.width / k, 1f) : new Rect(px.x / k, px.y / k, 1f, px.height / k));

    // True when no run (px) of a leader meets another control (a bend 2 px clear of it; its own control only where the last run
    // starts at its edge), another label's bar (1 px clear) or another leader (2 px clear), and, bent, its bend and stub keep out
    // of its own bar.
    bool RunsClear(Callout c, Rect own, Rect[] runs, bool bent)
    {
        for (var i = 0; i < runs.Length; i++)
        {
            var run = runs[i];
            var bend = bent && i == 1;
            foreach (var p in DrawnParts())
                if (!(i == runs.Length - 1 && p == own) && Meets(run, p, bend ? 2f : 0f)) return false;
            foreach (var o in callouts)
            {
                var bar = new Rect(o.Hit.x * k, o.Hit.y * k, o.Hit.width * k, o.Hit.height * k);
                if (o == c) { if (bent && i > 0 && Meets(run, bar, 1f)) return false; continue; }
                if (Meets(run, bar, 1f)) return false;
                foreach (var s in o.SegRects) if (Meets(run, Drawn(s), 2f)) return false;
            }
        }
        return true;
    }

    // Two px rects overlap, or come within clear px of each other.
    static bool Meets(Rect a, Rect b, float clear) =>
        a.xMin < b.xMax + clear && b.xMin - clear < a.xMax && a.yMin < b.yMax + clear && b.yMin - clear < a.yMax;

    // A leader segment in px as DrawLeader draws it at full length.
    Rect Drawn(Rect r)
    {
        var x = Mathf.Round(r.x * k);
        var y = Mathf.Round(r.y * k);
        return r.width > r.height ? new Rect(x, y, Mathf.Max(1f, Mathf.Round(r.xMax * k) - x), 1f) : new Rect(x, y, 1f, Mathf.Max(1f, Mathf.Round(r.yMax * k) - y));
    }

    // Touch: the pending leaders, level ones first, then by how far each label's row is from its control's (Refresh).
    void ByBend(List<Callout> pending, List<bool> grow)
    {
        var order = new List<int>();
        for (var i = 0; i < pending.Count; i++) order.Add(i);
        order.Sort((a, b) =>
        {
            var by = Bend(pending[a]).CompareTo(Bend(pending[b]));
            return by != 0 ? by : a.CompareTo(b);
        });
        var callout = new List<Callout>();
        var grows = new List<bool>();
        foreach (var i in order) { callout.Add(pending[i]); grows.Add(grow[i]); }
        pending.Clear(); pending.AddRange(callout);
        grow.Clear(); grow.AddRange(grows);
    }

    float Bend(Callout c)
    {
        if (!Level(c.Slot)) return 0f;
        var y = Mathf.Round(c.From.y * k);
        // Level on its label's own row (LeaderTo): no bend.
        if (DrawnAt(FrontRoomsInput.Normalise(c.Slot.DefaultTarget), out var part, out _, out var radius) && EdgeInset(part, radius, y) <= 1f) return 0f;
        var gap = Mathf.Abs(y - PartRow(c.Slot.DefaultTarget, c.Slot.OwnFrom.y));
        return gap <= LeaderSlack ? 0f : gap;
    }

    void DrawLeader(Callout c)
    {
        // Drawn from the label towards the key, as far as the grow tween has reached.
        var left = c.Length * c.Grow.Current;
        for (var i = 0; i < c.SegRects.Count; i++)
        {
            var r = c.SegRects[i];
            var len = Mathf.Max(r.width, r.height);
            var img = c.Segs[i];
            if (left <= 0f) { img.enabled = false; continue; }
            img.enabled = true;
            var hue = Color.Lerp(paper, accent, c.Bar.Current);
            img.color = new Color(hue.r, hue.g, hue.b, Mathf.Lerp(.6f, .9f, c.Bar.Current) * leadersIn);
            var show = Mathf.Min(len, left);
            left -= len;
            var horizontal = r.width > r.height;
            var startsAtMin = SegmentStartsAtMin(c, i);
            var x = r.x; var y = r.y; var w = r.width; var h = r.height;
            if (horizontal) { w = Mathf.Max(1f, show); if (!startsAtMin) x = r.xMax - w; }
            else { h = Mathf.Max(1f, show); if (!startsAtMin) y = r.yMax - h; }
            // Whole px, and always 1 px wide across its run.
            var px = Mathf.Round(x * k);
            var py = Mathf.Round(y * k);
            Place(img.rectTransform, px, py, horizontal ? Mathf.Max(1f, Mathf.Round((x + w) * k) - px) : 1f, horizontal ? 1f : Mathf.Max(1f, Mathf.Round((y + h) * k) - py));
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
        foreach (var part in padParts) if (!part.Decoration && part.Has(path)) return part.Home.center;
        return new Vector2(Width * .5f, Height * .5f);
    }

    Rect TargetRect(string path)
    {
        foreach (var v in keys) if (FrontRoomsInput.Normalise(v.Def.Path) == path) return v.Home;
        foreach (var m in mouse) if (FrontRoomsInput.Normalise(m.Path) == path) return m.Rect;
        foreach (var part in padParts) if (!part.Decoration && part.Has(path)) return part.Home;
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
        foreach (var part in padParts) if (!part.Decoration && !part.Has(target)) Block(part.Home, 2f);
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
        // A run along the wide gap under the F-row sits on its middle row too (the narrow gaps allow nothing else). Keyboard
        // only: on the pad board that band is the shoulders, and the run would be moved into LB or RB.
        var gapTop = RowY[0] + KeySize;
        var gapMid = gapTop + Mathf.Floor((RowY[1] - gapTop) * .5f);
        if (!padBoard)
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
            // .7 of a unit: about 1 px at the drawing's 1.45, the keys' keyline.
            if (outline) a -= Mathf.Clamp01(.5f - (d + .7f) * scale);
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
