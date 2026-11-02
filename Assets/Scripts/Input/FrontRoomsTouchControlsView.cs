using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ease = FrontRoomsTouchMotion.Ease;
using MotionValue = FrontRoomsTouchMotion.Value;

/// <summary>
/// The touch layer's picture and motion (Documentation/TOUCH_CONTROLS.md §5, §10;
/// Figma TK touch kit masters 2528:5495 and TS screen masters 2530:5061).
///
/// Drawn from the desktop HUD kit's parts: the stick and USE's hold ring are the
/// hold ring (paper 30 % track, 3.5 pt yellow arc), USE and pause are the keycap
/// (2 pt paper outline), a press turns yellow like the key chip, and the menus
/// are the desktop's pause wash, dark settings card and yellow chips.
///
/// The canvas is point-true: one unit is one point (iOS) or dp (Android) on
/// every device, so a control keeps its physical size on a phone or a tablet.
/// Positions come from <see cref="FrontRoomsTouchLayout"/>, the same layout the
/// state machine hit-tests, and every change of state is a timed transition on
/// unscaled time (menus keep moving while the game is paused). With Reduce
/// Motion (iOS / Android, or CAMERA MOTION off) transitions are short fades.
/// </summary>
[DefaultExecutionOrder(-450)]
[RequireComponent(typeof(FrontRoomsTouchControls))]
public sealed class FrontRoomsTouchControlsView : MonoBehaviour
{
    // The desktop HUD palette (UI_SYSTEM.md, Figma HUD kit).
    static readonly Color Paper = Hex(0xF4F1E8);
    static readonly Color Muted = Hex(0xBDBAB0);
    static readonly Color Accent = Hex(0xF4DF3B);
    static readonly Color Ink = Hex(0x0A0A0A);
    static readonly Color Media = Hex(0x141414);
    static readonly Color CardColor = Hex(0x0E0E0D);
    static readonly Color WashColor = new Color(.93f, .92f, .88f, 1f);
    /// <summary>Settings is one black frosted sheet (Red, 2026-10-24): the veil turns black at this alpha, so the frozen
    /// room shows through faintly while paper text keeps ≥ 4.5 : 1 even over a blurred ceiling light (linear blend).</summary>
    const float SettingsVeil = .86f;
    readonly MotionValue settingsDark = new MotionValue();
    Image settingsCardImage;
    FrontRoomsLanguageStrip languageStrip;
    readonly Dictionary<FrontRoomsTouchControls.Button, float> chipWords = new Dictionary<FrontRoomsTouchControls.Button, float>();


    FrontRoomsTouchControls controls;
    Canvas canvas;
    CanvasScaler scaler;
    RectTransform root;
    CanvasGroup controlsGroup;
    RectTransform menuLayer;
    Font bayon, plex, serif;
    float builtDensity = -1f;
    float builtScale = -1f;

    // ------------------------------------------------------------ the stick
    sealed class StickView
    {
        public RectTransform Root, Thumb, RunLine, Label;
        public Image BaseDisc, BaseRing, SlowRingDashed, SlowRingLit, RunLineDashed, RunLineLit, ThumbDisc;
        public Text LabelText;
        public Outline LabelKeyline;
        public CanvasGroup Group;
    }

    StickView ghost, live;
    readonly MotionValue ghostAlpha = new MotionValue(1f);
    readonly MotionValue liveAlpha = new MotionValue();
    readonly MotionValue liveScale = new MotionValue(1f);
    readonly MotionValue thumbReturn = new MotionValue(1f);
    readonly MotionValue runPop = new MotionValue();
    readonly MotionValue runTint = new MotionValue();
    readonly MotionValue slowTint = new MotionValue();
    readonly MotionValue labelAlpha = new MotionValue();
    readonly MotionValue windTint = new MotionValue();
    bool wasTouched, wasRunning, wasWinded;
    Vector2 releasedThumb, lastOrigin;

    // The stick's slot words (X2 spec v3.3 §3.3–§3.4): one Text, one fade. The enum order is the priority.
    enum SlotWord { None, Slow, Preview, Run, Winded }
    enum SlotPlace { Hidden, Above, Below }
    readonly SlotPlace[] slotPlaces = new SlotPlace[5];
    readonly List<Rect> slotKeepOut = new List<Rect>();
    SlotWord slotShown;
    bool slotLeaving;
    Vector2 slotFrozen;
    // Whether this touch may teach a word: decided at touch-down, so a word that retires leaves from the next touch.
    bool slotTeachSlow, slotTeachRun, slotSlowDwelled;
    float slotSlowFor, slotSlowOutFor, slotCreepFor;
    const float SlotIn = .14f, SlotOut = .10f;

    // ------------------------------------------------------------------ USE
    RectTransform useRoot, useLabelA, useLabelB;
    CanvasGroup useGroup;
    Image useDisc, useRing, usePressed, holdTrack, holdArc;
    Text useTextA, useTextB;
    readonly MotionValue useAlpha = new MotionValue();
    readonly MotionValue useScale = new MotionValue(1f);
    readonly MotionValue usePress = new MotionValue();
    readonly MotionValue holdTrackAlpha = new MotionValue();
    readonly MotionValue labelSwap = new MotionValue(1f);
    readonly MotionValue lockedShake = new MotionValue(1f);
    readonly MotionValue brokenSwing = new MotionValue(1f);
    float brokenSwingSeen = float.NegativeInfinity;
    readonly MotionValue useTakeover = new MotionValue();
    string useLabelShown = string.Empty;
    bool useLabelOnA = true;
    bool wasUseHeld;
    float holdShown;

    // ---------------------------------------------------- pause, sprint toggle
    RectTransform pauseRoot, sprintToggleRoot;
    // In a close-up only USE (as BACK) stays: the stick, pause and RUN button ease out (EXAMINE, contract T1).
    CanvasGroup pauseGroup, sprintToggleGroup;
    readonly MotionValue outsideInspect = new MotionValue();
    Image pauseKeycapFill, pauseKeycapLine, sprintToggleDisc, sprintToggleRing;
    Text sprintToggleText;
    readonly MotionValue pausePress = new MotionValue();
    // How far each control has given way to a camera or a rounded corner, eased (GiveWay).
    readonly MotionValue pauseGiveX = new MotionValue(), pauseGiveY = new MotionValue();
    readonly MotionValue useGiveX = new MotionValue(), useGiveY = new MotionValue();
    readonly MotionValue sprintGiveX = new MotionValue(), sprintGiveY = new MotionValue();
    readonly MotionValue restGiveX = new MotionValue(), restGiveY = new MotionValue();
    readonly MotionValue textChipGiveX = new MotionValue(), textChipGiveY = new MotionValue();
    readonly MotionValue headingGive = new MotionValue();
    readonly MotionValue toggleOn = new MotionValue();
    readonly MotionValue controlsAlpha = new MotionValue();

    // ------------------------------------------------------ EXAMINE text (T6, T7)
    RectTransform textChipRoot, panelRoot, panelViewport, panelContent;
    Image textChipFill, panelSurface, panelFadeTop, panelFadeBottom;
    Text textChipLabel;
    Outline textChipKeyline;
    CanvasGroup textChipGroup, panelGroup;
    readonly MotionValue textChipAlpha = new MotionValue();
    readonly MotionValue panelAlpha = new MotionValue();
    string panelKey;
    float panelContentHeight;
    /// <summary>The placard's phosphor ink (平面视觉 G9): a glow line's colour and its 2 pt rule.</summary>
    static readonly Color Phosphor = Hex(0xD6DEBD);
    /// <summary>Glow lines show only below the ink gate, as FrontRoomsInspect.TranscriptText shows them.</summary>
    const float GlowGate = .15f;
    /// <summary>The panel's type: G9 on the phone (heads Plex Mono 11/14, lines Source Serif 4 17/20); on the iPad the touch ramp's
    /// iPad row for the same roles (meta 12/15, caption 19/22; TOUCH_CONTROLS §4).</summary>
    static int HeadSize => FrontRoomsHandheld.IsTablet ? 12 : 11;
    static float HeadPitch => FrontRoomsHandheld.IsTablet ? 15f : 14f;
    static int LineSize => FrontRoomsHandheld.IsTablet ? 19 : 17;
    static float LinePitch => FrontRoomsHandheld.IsTablet ? 22f : 20f;
    /// <summary>Natural lines in em: Source Serif 4 1.371 (hhea 1036 + 335 of 1000; 23.31 pt at 17), IBM Plex Mono 1.3 (1025 + 275).</summary>
    const float SerifLineEm = 1.371f, PlexLineEm = 1.3f;

    // ---------------------------------------------------------------- menus
    sealed class Item
    {
        public RectTransform Rect;
        public CanvasGroup Group;
        public Vector2 Base;
        public readonly MotionValue Alpha = new MotionValue();
        public readonly MotionValue Rise = new MotionValue();
        public readonly MotionValue Scale = new MotionValue(1f);
        /// <summary>How far it has given way to a camera or a rounded corner (chips).</summary>
        public readonly MotionValue GiveX = new MotionValue(), GiveY = new MotionValue();
        public bool Shown;
    }

    // The frost behind the pause, settings and caught cards (Red, 2026-10-07): the paused frame itself, blurred,
    // under a light paper veil and the wallpaper's own paper and print, tone on tone. The cards sit on top.
    CanvasGroup frostGroup;
    RawImage frostFrame, frostPaper;
    Image frostVeil;
    RenderTexture frostTexture;
    /// <summary>Paper over the blurred frame, blended linear like all UI here: the room shows, the ink still reads (≥ 6 : 1).</summary>
    const float FrostVeil = .30f;
    /// <summary>Bayon's cap height in em (平面视觉, measured; Figma's glyph bounds agree at 12.17 pt for 17 pt).</summary>
    const float BayonCapHeightEm = .714f;
    /// <summary>IBM Plex Mono's cap height in em (698 / 1000 units).</summary>
    const float PlexCapHeightEm = .698f;
    /// <summary>One wallpaper roll tile (0.75 × 1.125 m) is drawn this many points wide.</summary>
    const float FrostTileWidth = 280f;
    readonly MotionValue washAlpha = new MotionValue();
    Item pauseTitle, pauseMeta, legendA, legendB, confirmText, titlePrompt, caughtTitle, caughtSubtitle, caughtRule, caughtLine, settingsCard;
    Text titlePromptText;
    Text pauseMetaText, confirmTextText, caughtLineText;

    // One legend, word for word on the pause card (ink on the frost) and the title (paper with the keyline): X2 §3.2.
    sealed class Legend
    {
        public RectTransform RowA, RowB;
        public Text Move, Drag, Slow, Sprint, Use, Hold;
        public Image SprintGlyph;
        public int RowsA = 1, RowsB = 1;
        public readonly Dictionary<Text, Image> Glyphs = new Dictionary<Text, Image>();
    }

    Legend pauseLegend, titleLegend;
    readonly Dictionary<FrontRoomsTouchControls.Button, Item> chips = new Dictionary<FrontRoomsTouchControls.Button, Item>();
    readonly Dictionary<FrontRoomsTouchControls.Button, Image> chipFills = new Dictionary<FrontRoomsTouchControls.Button, Image>();
    readonly Dictionary<FrontRoomsTouchControls.Button, Text> chipLabels = new Dictionary<FrontRoomsTouchControls.Button, Text>();
    readonly Item[] caughtNumbers = new Item[4];
    readonly Item[] caughtLabels = new Item[4];
    readonly Text[] caughtNumberTexts = new Text[4];
    readonly int[] caughtTargets = new int[4];
    float caughtStart;
    float titleShownAt = -1f;
    FrontRoomsTouchControls.MenuState lastState = (FrontRoomsTouchControls.MenuState)(-1);
    bool lastConfirm;

    // ---------------------------------------------------------- settings card
    sealed class RowView
    {
        public RectTransform Rect;
        public CanvasGroup Group;
        public Text Label, ValueA, ValueB, ChevronLeft, ChevronRight;
        public Image Divider, Pressed;
        public string ShownValue;
        public bool OnA = true;
        public readonly MotionValue Swap = new MotionValue(1f);
        public readonly MotionValue Press = new MotionValue();
        public readonly MotionValue RuleAlpha = new MotionValue();
        public readonly MotionValue Alpha = new MotionValue();
        public readonly MotionValue Rise = new MotionValue();
        /// <summary>How far the row has narrowed from each side for a camera or a rounded corner, where it is scrolled to.</summary>
        public readonly MotionValue GiveLeft = new MotionValue(), GiveRight = new MotionValue();
        public int Id = int.MinValue;
        public bool Header;
        // The row's control (FrontRoomsSettingsMenu: switch, segments, slider, meter, button), rebuilt when its shape changes.
        public RectTransform Control;
        public string ControlSig;
        public readonly List<Image> Cells = new List<Image>();
        public readonly List<Text> CellTexts = new List<Text>();
        public readonly List<Image> Bits = new List<Image>();
        public Image Knob, Fill, Thumb, Changed;
        public Text Word;
        // SETTINGS_MOTION.md: its own bar (no bar travels) and underlay; its name at the open size, drawn while it is open or
        // moving (Label rests closed); the second line under the first: the sentence, a note under each choice (NoteOf: the
        // option or stop it belongs to; -1, a switch's current state), a short rule under the default's, the default; on a sheet
        // without the picture a picture row's TAP HERE line; FRAME RATE NOW's live line against its target. Built when its
        // words or its width change.
        public Image Bar, Underlay, NoteRule, Target;
        public Text LabelOpen, Sentence, Default, Tap, TargetWord;
        public readonly List<Text> Notes = new List<Text>();
        public readonly List<int> NoteOf = new List<int>();
        public readonly List<Image> Trace = new List<Image>();
        public RectTransform Line;
        public int RuleSlot;
        public FrontRoomsSettingsMenu.Row LineRow;
        public string LineExplain;
        public string[] LineNotes;
        public float LineWidth;
        public int LineTarget;
        public bool LineTap;
        public string LineSig;
    }

    RectTransform cardRoot, cardViewportLeft, cardViewportRight, scrollBar;
    Image scrollBarImage;
    Text cardHeading;
    readonly List<RowView> rows = new List<RowView>();
    readonly MotionValue scrollBarAlpha = new MotionValue();
    // Tabs beside SETTINGS, the explanation column, the GRAPHICS preview and the full-screen preview (Red, 2026-10-24).
    readonly Text[] tabTexts = new Text[4];
    readonly Image[] tabRules = new Image[4];
    RectTransform detailRoot;
    Text previewCaption, previewFrameWord, previewFrameLine, sentenceProbe;
    readonly Image[] previewDashes = new Image[10];
    RawImage preview;
    Image previewKeyline;
    RenderTexture previewRt;
    CanvasGroup menuGroup;
    RectTransform peekLayer;
    Text peekTitle, peekLine;
    readonly Image[] peekDashes = new Image[10];
    readonly MotionValue peekAlpha = new MotionValue();
    bool previewWanted;
    float lastScroll;
    float settingsOpenedAt;
    // An open row's second line (SETTINGS_MOTION.md, touch metrics in pt): the name 17 -> 24 Bayon, the sentence Source Serif 14 at
    // a 1.24 em pitch, the notes and the default Plex Mono 11; note, rule and default centres under the first line, as the desktop's.
    const int OpenLabelSize = 24, SentenceSize = 14, NoteSize = 11;
    const float SentencePitch = .905f, NoteDY = 14f, RuleDY = 21.5f, DefaultDY = 32.5f, SentenceMin = 160f, TapH = 18f;
    readonly Dictionary<FrontRoomsSettingsMenu.Row, Vector4> openHeights = new Dictionary<FrontRoomsSettingsMenu.Row, Vector4>();
    readonly Dictionary<FrontRoomsSettingsMenu.Row, string> openHeightTexts = new Dictionary<FrontRoomsSettingsMenu.Row, string>();
    // FRAME RATE NOW's live line: the mean frame time of every 0.1 s, the last 48 (4.8 s), while the sheet is open.
    readonly float[] frameTimes = new float[48];
    int frameTimeHead, frameTimeCount, frameTimeFrames;
    float frameTimeSum, frameTimeAt;

    bool Reduced => FrontRoomsHandheld.ReduceMotion;
    float Dur(float seconds) => Reduced ? Mathf.Min(seconds, .12f) * .9f : seconds;

    /// <summary>
    /// Red (2026-10-24): what has to give way to a camera or a rounded corner moves there with a motion transition, and
    /// back once it no longer has to (after a rotation, a fold, or a settings row scrolling past the camera). A new
    /// target eases in over 0.28 s (OutCubic); it snaps while the element is off screen and under Reduce Motion.
    /// </summary>
    void GiveWay(MotionValue value, float target, bool snap)
    {
        if (snap || Reduced) { if (!Mathf.Approximately(value.Target, target) || !value.Done) value.Snap(target); return; }
        value.To(target, .28f, Ease.OutCubic);
    }

    void Awake()
    {
        controls = GetComponent<FrontRoomsTouchControls>();
        bayon = LoadFont("Fonts/Bayon-Regular");
        plex = LoadFont("Fonts/IBMPlexMono-Regular");
        serif = LoadFont("Fonts/SourceSerif4-Variable");
        BuildCanvas();
        FrontRoomsSettings.Changed += OnSettingsChanged;
        FrontRoomsLanguage.Changed += OnLanguageChanged;
    }

    void OnDestroy()
    {
        FrontRoomsSettings.Changed -= OnSettingsChanged;
        FrontRoomsLanguage.Changed -= OnLanguageChanged;
        ReleaseFrost();
        ReleasePreview();
    }

    void OnLanguageChanged() => chipWords.Clear();

    // Measured once per language: the hit test asks for it every frame.
    float ChipWordWidth(FrontRoomsTouchControls.Button button)
    {
        if (chipWords.TryGetValue(button, out var width)) return width;
        if (!chipLabels.TryGetValue(button, out var word)) return 0f;
        width = Mathf.Ceil(word.preferredWidth);
        chipWords[button] = width;
        return width;
    }

    void OnSettingsChanged()
    {
        controls.ApplySavedSettings();
        builtScale = -1f;
    }

    static Font LoadFont(string path) => Resources.Load<Font>(path) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    void LateUpdate()
    {
        var active = FrontRoomsHandheld.Active;
        if (canvas.gameObject.activeSelf != active) canvas.gameObject.SetActive(active);
        if (!active) return;
        var density = FrontRoomsHandheld.PointScale;
        if (!Mathf.Approximately(scaler.scaleFactor, density)) scaler.scaleFactor = density;
        var L = controls.Layout;
        if (!Mathf.Approximately(builtDensity, density) || !Mathf.Approximately(builtScale, L.Scale))
        {
            ApplyGeometry(L);
            builtDensity = density;
            builtScale = L.Scale;
        }
        TickControls(L);
        TickExamineText(L);
        TickMenus(L);
        TickPeek(L);
        RenderPreview();
    }

    // =================================================================== build

    void BuildCanvas()
    {
        var go = new GameObject("Handheld touch UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(transform, false);
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 490;
        canvas.pixelPerfect = false;
        scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = Mathf.Max(.01f, FrontRoomsHandheld.PointScale);
        scaler.referencePixelsPerUnit = 100f;
        root = go.GetComponent<RectTransform>();

        var controlsLayer = Layer("Controls");
        controlsGroup = controlsLayer.gameObject.AddComponent<CanvasGroup>();
        controlsGroup.alpha = 0f;
        controlsGroup.blocksRaycasts = false;
        menuLayer = Layer("Menus");
        menuGroup = menuLayer.gameObject.AddComponent<CanvasGroup>();

        ghost = BuildStick(controlsLayer, "Stick / rest");
        live = BuildStick(controlsLayer, "Stick / live");
        BuildUse(controlsLayer);
        BuildPause(controlsLayer);
        BuildSprintToggle(controlsLayer);
        BuildExamineText(controlsLayer);
        BuildMenus();
        BuildPeek();
        canvas.gameObject.SetActive(FrontRoomsHandheld.Active);
    }

    RectTransform Layer(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(root, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    /// <summary>An element in a full-frame layer: its anchoredPosition is its centre in points from the bottom-left.</summary>
    static RectTransform Node(string name, Transform parent, Vector2 size, bool frameAnchored = true)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = frameAnchored ? Vector2.zero : new Vector2(.5f, .5f);
        rt.pivot = new Vector2(.5f, .5f);
        rt.sizeDelta = size;
        return rt;
    }

    static Image Img(string name, Transform parent, Vector2 size, Color color, bool frameAnchored = false)
    {
        var rt = Node(name, parent, size, frameAnchored);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>An arc sprite (its texture holds only the arc) placed with its ring's centre on the parent's centre.</summary>
    static void PlaceArc(Image image, Sprite sprite)
    {
        image.sprite = sprite;
        var rt = image.rectTransform;
        rt.sizeDelta = FrontRoomsTouchSprites.PointSize(sprite);
        rt.pivot = FrontRoomsTouchSprites.PivotOf(sprite);
        rt.anchoredPosition = Vector2.zero;
    }

    Text Txt(string name, Transform parent, string value, Font font, int size, Color color, TextAnchor anchor, Vector2 box, bool frameAnchored = false)
    {
        var rt = Node(name, parent, box, frameAnchored);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.fontStyle = FontStyle.Normal;
        t.text = FrontRoomsLanguage.T(value);
        t.color = color;
        t.alignment = anchor;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.supportRichText = false;
        t.raycastTarget = false;
        // Its language's face and size; a word fixed here follows a language change by itself.
        FrontRoomsLanguage.Style(t, string.IsNullOrEmpty(value) ? null : value);
        return t;
    }

    StickView BuildStick(Transform parent, string name)
    {
        var s = new StickView();
        s.Root = Node(name, parent, Vector2.zero);
        s.Group = s.Root.gameObject.AddComponent<CanvasGroup>();
        s.BaseDisc = Img("base", s.Root, Vector2.zero, Media);
        s.BaseRing = Img("base ring", s.Root, Vector2.zero, Paper);
        // The slow ring (36 pt): dashed while walking, solid while creeping (Red, 2026-10-08; Figma Touch / Stick).
        s.SlowRingDashed = Img("slow ring", s.Root, Vector2.zero, Paper);
        s.SlowRingLit = Img("slow ring lit", s.Root, Vector2.zero, Paper);
        // The run line, 24 pt past the ring and only ahead: dashed paper that firms up as the thumb nears it, yellow
        // once it runs, the same rule as USE's hold ring.
        s.RunLine = Node("run line", s.Root, Vector2.zero, false);
        s.RunLineDashed = Img("run line dashed", s.RunLine, Vector2.zero, Paper);
        s.RunLineLit = Img("run line lit", s.RunLine, Vector2.zero, Accent);
        s.Thumb = Node("thumb", s.Root, Vector2.zero, false);
        s.ThumbDisc = s.Thumb.gameObject.AddComponent<Image>();
        s.ThumbDisc.color = Paper;
        s.ThumbDisc.raycastTarget = false;
        // The slot word has a keyline, no chip (平面视觉 for X2, 2026-10-22; Red's no-dark-card rule): Bayon 17 with a
        // 1 pt #141414 outline from FrontRoomsHudStyle, scaled with the stick.
        s.LabelText = Txt("state label", s.Root, string.Empty, bayon, 17, Accent, TextAnchor.MiddleCenter, new Vector2(160f, 20f));
        s.LabelKeyline = Keyline(s.LabelText, 1f);
        s.Label = s.LabelText.rectTransform;
        return s;
    }

    /// <summary>The HUD's keyline under a word or glyph (FrontRoomsHudStyle): Unity's Outline at one tap's alpha, fading with the graphic.</summary>
    static Outline Keyline(Graphic graphic, float distance)
    {
        var outline = graphic.gameObject.AddComponent<Outline>();
        var ink = FrontRoomsHudStyle.KeylineColor;
        outline.effectColor = new Color(ink.r, ink.g, ink.b, FrontRoomsHudStyle.OutlineTapAlpha);
        outline.effectDistance = new Vector2(distance, -distance);
        outline.useGraphicAlpha = true;
        return outline;
    }

    /// <summary>The TEXT chip and the transcript panel of a close-up (EXAMINE T6/T7; 平面视觉 G9/G10).</summary>
    void BuildExamineText(Transform parent)
    {
        textChipRoot = Node("TEXT chip", parent, new Vector2(48f, 26f));
        textChipGroup = textChipRoot.gameObject.AddComponent<CanvasGroup>();
        textChipGroup.alpha = 0f;
        textChipFill = Img("chip", textChipRoot, new Vector2(48f, 26f), WithAlpha(Paper, 0f));
        textChipFill.sprite = FrontRoomsTouchSprites.Solid();
        textChipLabel = Txt("label", textChipRoot, "TEXT", bayon, 17, Paper, TextAnchor.MiddleCenter, new Vector2(80f, 22f));
        textChipKeyline = Keyline(textChipLabel, FrontRoomsHudStyle.KeylineDistance);

        panelRoot = Node("Transcript panel", parent, new Vector2(270f, 216f));
        panelGroup = panelRoot.gameObject.AddComponent<CanvasGroup>();
        panelGroup.alpha = 0f;
        // A reading surface, like settings (G9): #0E0E0D at 97 %, no rule.
        panelSurface = Img("surface", panelRoot, new Vector2(270f, 216f), WithAlpha(CardColor, .97f));
        panelSurface.sprite = FrontRoomsTouchSprites.Solid();
        panelViewport = Node("viewport", panelRoot, new Vector2(270f, 216f), false);
        panelViewport.gameObject.AddComponent<RectMask2D>();
        panelContent = Node("content", panelViewport, new Vector2(270f, 0f), false);
        panelContent.anchorMin = panelContent.anchorMax = panelContent.pivot = new Vector2(.5f, 1f);
        // A 12 pt fade where the text is cut, no scroll bar (G9).
        panelFadeTop = Img("fade top", panelRoot, new Vector2(270f, 12f), WithAlpha(CardColor, 0f));
        panelFadeTop.sprite = FrontRoomsTouchSprites.FadeUp();
        panelFadeTop.rectTransform.localEulerAngles = new Vector3(0f, 0f, 180f);
        panelFadeBottom = Img("fade bottom", panelRoot, new Vector2(270f, 12f), WithAlpha(CardColor, 0f));
        panelFadeBottom.sprite = FrontRoomsTouchSprites.FadeUp();
    }

    /// <summary>
    /// The TEXT chip shows in a close-up whose target has a transcript; while it is on, the panel shows the transcript: region
    /// heads (lines starting U+0002, 游戏视觉's format) in IBM Plex Mono 11/14 caps, lines in Source Serif 4 17/20, glow lines
    /// (U+0001) in phosphor ink with a 2 pt rule, only below the ink gate. A drag on the panel scrolls it.
    /// </summary>
    void TickExamineText(FrontRoomsTouchLayout L)
    {
        var inspect = controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Inspect && FrontRoomsInspect.Active && !FrontRoomsInspect.Closing;
        var target = inspect ? FrontRoomsInspect.Current : null;
        var id = target != null && target.Def != null ? target.Def.transcript : null;
        var lines = string.IsNullOrEmpty(id) ? null : FrontRoomsInspectCatalog.Transcript(id);
        // A43: TEXT means reading, so it comes only with a pause: a live close-up never offers it.
        var hasText = lines != null && lines.Length > 0 && FrontRoomsInspect.CurrentPauses && !FrontRoomsInspect.FullView;
        var on = hasText && FrontRoomsInspect.ShowTranscript;
        controls.TextChipShown = hasText;
        controls.TranscriptShown = on;

        // The chip (G10): off, the paper word with the HUD keyline and no fill; on, a paper chip with the word in ink.
        // With a pad or a keyboard the chip steps aside: Y (TRIANGLE) or T is TEXT.
        var chipShown = hasText && !controls.ExternalInput;
        textChipAlpha.To(chipShown ? 1f : 0f, Dur(chipShown ? .18f : .12f), chipShown ? Ease.OutCubic : Ease.InCubic);
        textChipGroup.alpha = textChipAlpha;
        var caps = Caps(textChipLabel);
        // It gives way to a rounded corner or a cutout as the HUD does: eased, snapped while hidden.
        var textChipGive = L.TextChipCenter - L.TextChipCenterBase;
        GiveWay(textChipGiveX, textChipGive.x, textChipAlpha.Current < .01f);
        GiveWay(textChipGiveY, textChipGive.y, textChipAlpha.Current < .01f);
        textChipRoot.anchoredPosition = L.TextChipCenterBase + new Vector2(textChipGiveX, textChipGiveY);
        textChipRoot.localScale = Vector3.one * (controls.PressedButton == FrontRoomsTouchControls.Button.TextChip ? .94f : 1f);
        textChipFill.rectTransform.anchoredPosition = new Vector2(caps.x, caps.y);
        textChipFill.rectTransform.sizeDelta = new Vector2(caps.z + 20f, caps.w + 14f);
        textChipFill.color = WithAlpha(Paper, on ? 1f : 0f);
        textChipLabel.color = on ? Ink : Paper;
        textChipKeyline.enabled = !on;

        // The panel (G9): the right 36 % of the safe area, a reading surface.
        panelAlpha.To(on ? 1f : 0f, Dur(on ? .18f : .12f), on ? Ease.OutCubic : Ease.InCubic);
        panelGroup.alpha = panelAlpha;
        var rect = L.TranscriptPanel;
        panelRoot.anchoredPosition = rect.center;
        panelRoot.sizeDelta = rect.size;
        panelSurface.rectTransform.sizeDelta = rect.size;
        panelViewport.sizeDelta = rect.size;
        panelContent.sizeDelta = new Vector2(rect.width, panelContentHeight);
        var glow = FrontRoomsInspectLook.OwnLight < GlowGate;
        if (on)
        {
            var key = id + "|" + glow + "|" + Mathf.RoundToInt(rect.width) + "|" + lines.Length;
            if (key != panelKey)
            {
                BuildTranscript(lines, glow, rect.width);
                panelKey = key;
            }
        }
        controls.TranscriptScrollMax = Mathf.Max(0f, panelContentHeight - rect.height);
        var scroll = Mathf.Clamp(controls.TranscriptScroll, 0f, controls.TranscriptScrollMax);
        panelContent.anchoredPosition = new Vector2(0f, scroll);
        panelFadeTop.rectTransform.anchoredPosition = new Vector2(0f, rect.height * .5f - 6f);
        panelFadeTop.rectTransform.sizeDelta = new Vector2(rect.width, 12f);
        panelFadeBottom.rectTransform.anchoredPosition = new Vector2(0f, -rect.height * .5f + 6f);
        panelFadeBottom.rectTransform.sizeDelta = new Vector2(rect.width, 12f);
        panelFadeTop.color = WithAlpha(CardColor, scroll > .5f ? .97f : 0f);
        panelFadeBottom.color = WithAlpha(CardColor, scroll < controls.TranscriptScrollMax - .5f ? .97f : 0f);
    }

    /// <summary>Lays the transcript out top-down as on the board (EX01): 16 pt padding, 12 pt between regions, 4 pt between lines (G9).
    /// Each text sits on its design line box (17/20, 11/14 on the phone). Unity's preferred height gives a first line its natural height
    /// and hangs it one ascent below the top, so the box is sized from the line count and raised by half the difference, as Figma
    /// centres a line.</summary>
    void BuildTranscript(string[] lines, bool glow, float width)
    {
        for (var i = panelContent.childCount - 1; i >= 0; i--) Destroy(panelContent.GetChild(i).gameObject);
        const float pad = 16f, rule = 2f, ruleGap = 6f;
        var inner = width - pad * 2f;
        var y = pad;
        var first = true;
        string lastHead = null;
        foreach (var raw in lines)
        {
            if (string.IsNullOrEmpty(raw)) continue;
            var head = raw[0] == '\u0002';
            var isGlow = raw[0] == '\u0001';
            if (isGlow && !glow) continue;
            var text = head || isGlow ? raw.Substring(1) : raw;
            // A region whose first printed line is its own head (IN CASE OF ALARM on the evacuation plan) says it once, as the head.
            if (!head && !isGlow && lastHead != null && string.Equals(text.Trim(), lastHead, System.StringComparison.OrdinalIgnoreCase)) { lastHead = null; continue; }
            lastHead = head ? text.Trim() : null;
            if (head && !first) y += 12f - 4f;
            var indent = isGlow ? rule + ruleGap : 0f;
            var size = head ? HeadSize : LineSize;
            var pitch = head ? HeadPitch : LinePitch;
            var natural = size * (head ? PlexLineEm : SerifLineEm);
            var t = Txt(head ? "head" : isGlow ? "glow line" : "line", panelContent, head ? text.ToUpperInvariant() : text, head ? plex : serif, size,
                isGlow ? Phosphor : Paper, TextAnchor.UpperLeft, new Vector2(inner - indent, 0f));
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.lineSpacing = pitch / natural;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(inner - indent, 0f);
            _ = t.preferredHeight; // lays the text out at this width, so the layout generator holds its line count
            var h = Mathf.Max(1, t.cachedTextGeneratorForLayout.lineCount) * pitch;
            rt.sizeDelta = new Vector2(inner - indent, h);
            rt.anchoredPosition = new Vector2(pad + indent, -y + (natural - pitch) * .5f);
            if (isGlow)
            {
                var bar = Img("glow rule", panelContent, new Vector2(rule, h), Phosphor);
                bar.sprite = FrontRoomsTouchSprites.Solid();
                var br = bar.rectTransform;
                br.anchorMin = br.anchorMax = br.pivot = new Vector2(0f, 1f);
                br.anchoredPosition = new Vector2(pad, -y);
            }
            y += h + 4f;
            first = false;
        }
        panelContentHeight = y - 4f + pad;
    }

    void BuildUse(Transform parent)
    {
        useRoot = Node("USE", parent, Vector2.zero);
        useGroup = useRoot.gameObject.AddComponent<CanvasGroup>();
        holdTrack = Img("hold track", useRoot, Vector2.zero, Paper);
        holdArc = Img("hold progress", useRoot, Vector2.zero, Accent);
        holdArc.type = Image.Type.Filled;
        holdArc.fillMethod = Image.FillMethod.Radial360;
        holdArc.fillOrigin = (int)Image.Origin360.Top;
        holdArc.fillClockwise = true;
        useDisc = Img("button", useRoot, Vector2.zero, Media);
        usePressed = Img("pressed fill", useRoot, Vector2.zero, Accent);
        useRing = Img("keycap ring", useRoot, Vector2.zero, Paper);
        useTextA = Txt("verb A", useRoot, string.Empty, bayon, 17, Paper, TextAnchor.MiddleCenter, new Vector2(80f, 22f));
        useTextB = Txt("verb B", useRoot, string.Empty, bayon, 17, Paper, TextAnchor.MiddleCenter, new Vector2(80f, 22f));
        useLabelA = useTextA.rectTransform;
        useLabelB = useTextB.rectTransform;
        useGroup.alpha = 0f;
    }

    void BuildPause(Transform parent)
    {
        pauseRoot = Node("Pause", parent, new Vector2(32f, 32f));
        pauseGroup = pauseRoot.gameObject.AddComponent<CanvasGroup>();
        outsideInspect.Snap(1f);
        pauseKeycapFill = Img("keycap fill", pauseRoot, new Vector2(32f, 32f), Media);
        pauseKeycapFill.type = Image.Type.Sliced;
        pauseKeycapLine = Img("keycap line", pauseRoot, new Vector2(32f, 32f), Paper);
        pauseKeycapLine.type = Image.Type.Sliced;
        var bar1 = Img("bar", pauseRoot, new Vector2(3f, 12f), Paper);
        bar1.rectTransform.anchoredPosition = new Vector2(-4f, 0f);
        var bar2 = Img("bar", pauseRoot, new Vector2(3f, 12f), Paper);
        bar2.rectTransform.anchoredPosition = new Vector2(4f, 0f);
        bar1.sprite = bar2.sprite = FrontRoomsTouchSprites.Solid();
    }

    void BuildSprintToggle(Transform parent)
    {
        sprintToggleRoot = Node("Sprint toggle", parent, Vector2.zero);
        sprintToggleGroup = sprintToggleRoot.gameObject.AddComponent<CanvasGroup>();
        sprintToggleDisc = Img("button", sprintToggleRoot, Vector2.zero, Media);
        sprintToggleRing = Img("ring", sprintToggleRoot, Vector2.zero, Paper);
        sprintToggleText = Txt("label", sprintToggleRoot, "RUN", bayon, 17, Paper, TextAnchor.MiddleCenter, new Vector2(60f, 22f));
    }

    void ApplyGeometry(FrontRoomsTouchLayout L)
    {
        var k = L.Scale;
        foreach (var s in new[] { ghost, live })
        {
            var d = L.StickRadius * 2f;
            s.BaseDisc.sprite = FrontRoomsTouchSprites.Disc(d);
            s.BaseDisc.rectTransform.sizeDelta = new Vector2(d, d);
            s.BaseRing.sprite = FrontRoomsTouchSprites.Ring(d, 2f);
            s.BaseRing.rectTransform.sizeDelta = new Vector2(d, d);
            var sr = L.SlowRadius * 2f;
            PlaceArc(s.SlowRingDashed, FrontRoomsTouchSprites.Arc(sr, 1.5f, 180f, 4f, 4f));
            s.SlowRingLit.sprite = FrontRoomsTouchSprites.Ring(sr, 1.5f);
            s.SlowRingLit.rectTransform.sizeDelta = new Vector2(sr, sr);
            var rl = L.RunLineRadius * 2f;
            PlaceArc(s.RunLineDashed, FrontRoomsTouchSprites.Arc(rl, 2f, FrontRoomsTouchControls.RunHalfAngle, 6f, 6f));
            PlaceArc(s.RunLineLit, FrontRoomsTouchSprites.Arc(rl, 3.5f, FrontRoomsTouchControls.RunHalfAngle, 0f, 0f));
            var td = L.ThumbRadius * 2f;
            s.ThumbDisc.sprite = FrontRoomsTouchSprites.Disc(td);
            s.Thumb.sizeDelta = new Vector2(td, td);
            var kl = FrontRoomsHudStyle.KeylineDistance * k;
            s.LabelKeyline.effectDistance = new Vector2(kl, -kl);
        }
        var ud = L.UseRadius * 2f;
        useDisc.sprite = usePressed.sprite = FrontRoomsTouchSprites.Disc(ud);
        useDisc.rectTransform.sizeDelta = usePressed.rectTransform.sizeDelta = new Vector2(ud, ud);
        useRing.sprite = FrontRoomsTouchSprites.Ring(ud, 2f);
        useRing.rectTransform.sizeDelta = new Vector2(ud, ud);
        var hr = (L.HoldRingRadius + 1.75f) * 2f;
        holdTrack.sprite = FrontRoomsTouchSprites.Ring(hr, 3f);
        holdArc.sprite = FrontRoomsTouchSprites.Ring(hr, 3.5f);
        holdTrack.rectTransform.sizeDelta = holdArc.rectTransform.sizeDelta = new Vector2(hr, hr);
        pauseKeycapFill.sprite = FrontRoomsTouchSprites.RoundRect(4f, 0f);
        pauseKeycapLine.sprite = FrontRoomsTouchSprites.RoundRect(4f, 2f);
        var sb = L.SprintButtonRadius * 2f;
        sprintToggleDisc.sprite = FrontRoomsTouchSprites.Disc(sb);
        sprintToggleRing.sprite = FrontRoomsTouchSprites.Ring(sb, 2f);
        sprintToggleDisc.rectTransform.sizeDelta = sprintToggleRing.rectTransform.sizeDelta = new Vector2(sb, sb);
        frostVeil.sprite = FrontRoomsTouchSprites.Solid();
        foreach (var fill in chipFills.Values) fill.sprite = FrontRoomsTouchSprites.Solid();
        ApplyMenuSprites();
    }

    // ============================================================= gameplay

    void TickControls(FrontRoomsTouchLayout L)
    {
        var playing = controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Playing;
        var inspect = controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Inspect;
        // A pad or a keyboard in use hides the stick, USE, RUN and pause (Red, 2026-10-29); a touch brings them back.
        var shown = (playing || inspect) && !controls.ExternalInput;
        controlsAlpha.To(shown ? 1f : 0f, Dur(shown ? .18f : .12f), shown ? Ease.OutCubic : Ease.InCubic);
        controlsGroup.alpha = controlsAlpha;
        outsideInspect.To(inspect ? 0f : 1f, Dur(inspect ? .12f : .18f), inspect ? Ease.InCubic : Ease.OutCubic);
        pauseGroup.alpha = sprintToggleGroup.alpha = outsideInspect;
        var opacity = Mathf.Clamp(FrontRoomsSettings.TouchOpacityPercent / 34f, .5f, 1.9f);

        TickStick(L, opacity);
        TickUse(L, opacity);

        // Pause keycap: at the right bleed, giving way to a camera or a rounded corner with a motion transition.
        var pauseGive = L.PauseCenter - L.PauseCenterBase;
        var controlsOff = controlsGroup.alpha < .01f;
        GiveWay(pauseGiveX, pauseGive.x, controlsOff);
        GiveWay(pauseGiveY, pauseGive.y, controlsOff);
        pauseRoot.anchoredPosition = L.PauseCenterBase + new Vector2(pauseGiveX, pauseGiveY);
        var pausePressed = controls.PressedButton == FrontRoomsTouchControls.Button.Pause;
        pausePress.To(pausePressed ? 1f : 0f, Dur(pausePressed ? .07f : .16f), pausePressed ? Ease.OutCubic : Ease.OutBackSoft);
        pauseRoot.localScale = Vector3.one * (Reduced ? 1f : Mathf.LerpUnclamped(1f, .9f, pausePress));
        pauseKeycapFill.color = WithAlpha(Color.Lerp(Media, Paper, pausePress * .35f), Mathf.Min(1f, (.35f + .35f * pausePress) * opacity));
        pauseKeycapLine.color = WithAlpha(Paper, Mathf.Min(1f, .9f * opacity));

        // RUN button (only when Settings › RUN is BUTTON).
        var showToggle = !controls.RunStickMode;
        sprintToggleRoot.gameObject.SetActive(showToggle);
        if (showToggle)
        {
            var sprintGive = L.SprintButtonCenter - L.SprintButtonCenterBase;
            GiveWay(sprintGiveX, sprintGive.x, controlsOff);
            GiveWay(sprintGiveY, sprintGive.y, controlsOff);
            sprintToggleRoot.anchoredPosition = L.SprintButtonCenterBase + new Vector2(sprintGiveX, sprintGiveY);
            toggleOn.To(controls.Running ? 1f : 0f, Dur(.12f), Ease.OutCubic);
            var toggleWinded = controls.Winded;
            // It carries a word, so its disc is solid whatever CONTROLS OPACITY says.
            sprintToggleDisc.color = Color.Lerp(CardColor, Accent, toggleOn);
            sprintToggleRing.color = WithAlpha(toggleWinded ? Muted : Color.Lerp(Paper, Accent, toggleOn), Mathf.Min(1f, (toggleWinded ? .4f : .8f) * opacity + toggleOn));
            sprintToggleText.color = toggleWinded ? Muted : Color.Lerp(Paper, Ink, toggleOn);
            var pressed = controls.PressedButton == FrontRoomsTouchControls.Button.SprintToggle;
            sprintToggleRoot.localScale = Vector3.one * (Reduced || !pressed ? 1f : .92f);
        }
    }

    void TickStick(FrontRoomsTouchLayout L, float opacity)
    {
        var touched = controls.StickTouched;
        var running = controls.Running;
        var winded = controls.Winded;

        if (touched != wasTouched)
        {
            if (touched)
            {
                // The stick appears under the thumb; the resting ghost steps aside.
                liveAlpha.To(1f, Dur(.09f), Ease.OutCubic);
                if (!Reduced) liveScale.Restart(.88f, 1f, .14f, Ease.OutBackSoft);
                ghostAlpha.To(0f, Dur(.09f), Ease.OutCubic);
                thumbReturn.Snap(1f);
                DecideSlot(L);
            }
            else
            {
                // The thumb springs home while the stick fades; the ghost comes back to rest.
                releasedThumb = live.Thumb.anchoredPosition;
                thumbReturn.Restart(0f, 1f, Dur(.16f), Reduced ? Ease.OutCubic : Ease.OutBackSoft);
                liveAlpha.To(0f, Dur(.18f), Ease.InCubic);
                ghostAlpha.To(1f, Dur(.22f), Ease.OutCubic, Reduced ? 0f : .06f);
            }
            wasTouched = touched;
        }

        if (running != wasRunning)
        {
            if (running)
            {
                runTint.To(1f, Dur(.12f), Ease.OutCubic);
                // The click you feel, seen once: the thumb and the line pop as the thumb crosses.
                if (!Reduced) runPop.Restart(1f, 0f, .22f, Ease.OutCubic);
                // The first real stick crossing retires the RUN preview (a RUN-button run never does).
                if (touched && controls.RunStickMode && !FrontRoomsTouchControls.TaughtRun) FrontRoomsTouchControls.SetTaught(false, true);
            }
            else runTint.To(0f, Dur(.15f), Ease.InCubic);
            wasRunning = running;
        }

        if (winded != wasWinded)
        {
            windTint.To(winded ? 1f : 0f, Dur(.2f), Ease.OutCubic);
            wasWinded = winded;
        }

        var slow = touched && controls.CurrentGait == FrontRoomsGait.Slow;
        slowTint.To(slow ? 1f : 0f, Dur(slow ? .1f : .14f), Ease.OutCubic);

        // The resting ghost (floating stick) or the fixed stick at rest, giving way like the other controls.
        var restGive = L.StickRest - L.StickRestBase;
        GiveWay(restGiveX, restGive.x, controlsGroup.alpha < .01f);
        GiveWay(restGiveY, restGive.y, controlsGroup.alpha < .01f);
        ghost.Root.anchoredPosition = L.StickRestBase + new Vector2(restGiveX, restGiveY);
        ghost.Thumb.anchoredPosition = Vector2.zero;
        ghost.Group.alpha = ghostAlpha * outsideInspect;
        ApplyStickStyle(ghost, 0f, 0f, 0f, 0f, windTint, opacity, false);
        ghost.LabelText.text = string.Empty;

        // The live stick under the thumb.
        if (touched) lastOrigin = controls.StickOrigin;
        live.Root.anchoredPosition = lastOrigin;
        live.Root.localScale = Vector3.one * liveScale;
        live.Group.alpha = liveAlpha * outsideInspect;
        var thumb = touched ? controls.StickThumb - controls.StickOrigin : Vector2.LerpUnclamped(releasedThumb, Vector2.zero, thumbReturn);
        live.Thumb.anchoredPosition = thumb;
        live.Thumb.localScale = Vector3.one * (1f + .15f * runPop);
        live.RunLine.localScale = Vector3.one * (1f + .04f * runPop);
        TickSlot(L, touched, running, winded, thumb);
        ApplyStickStyle(live, 1f, runTint, controls.RunReach, slowTint, windTint, opacity, controls.RunStickMode);
    }

    // ------------------------------------------------------ the slot words (X2)

    static string SlotText(SlotWord word) => FrontRoomsLanguage.T(SlotEnglish(word));

    static string SlotEnglish(SlotWord word)
    {
        switch (word)
        {
            case SlotWord.Winded: return "WINDED";
            case SlotWord.Run:
            case SlotWord.Preview: return "RUN";
            case SlotWord.Slow: return FrontRooms3DGame.BodyDoorsActive ? "SLOW · QUIET STEPS" : "SLOW · UNHEARD";
            default: return string.Empty;
        }
    }

    /// <summary>
    /// Where each word may draw for this touch (X2 §3.3), decided once at touch-down: above the thumb when the
    /// word's rectangle, swept over every thumb position it can show at (settled scale 1), clears the screen's edges
    /// and insets, the HUD's top band and captions, pause and, in BUTTON mode, the RUN button; else hung under the
    /// ring; else hidden for this touch. The x is never clamped.
    /// </summary>
    void DecideSlot(FrontRoomsTouchLayout L)
    {
        slotTeachSlow = !FrontRoomsTouchControls.TaughtSlow;
        slotTeachRun = controls.RunStickMode && !FrontRoomsTouchControls.TaughtRun;
        slotKeepOut.Clear();
        controls.Host?.HudKeepOut(slotKeepOut);
        var pause = L.PauseCenter;
        var hit = L.PauseHitSize;
        slotKeepOut.Add(new Rect(pause.x - hit * .5f, pause.y - hit * .5f, hit, hit));
        // A word never draws over a camera (12 pt clear, like everything that gives way to one).
        foreach (var cut in FrontRoomsHandheld.Shape.Cutouts) slotKeepOut.Add(new Rect(cut.xMin - 12f, cut.yMin - 12f, cut.width + 24f, cut.height + 24f));
        var origin = controls.StickOrigin;
        var s = L.Scale;
        var ahead = FrontRoomsTouchControls.RunHalfAngle * Mathf.Deg2Rad;
        for (var w = SlotWord.Slow; w <= SlotWord.Winded; w++)
        {
            if ((w == SlotWord.Run || w == SlotWord.Preview) && !controls.RunStickMode) { slotPlaces[(int)w] = SlotPlace.Hidden; continue; }
            // The thumb positions this word shows at, from the stick's centre.
            Rect reach;
            if (w == SlotWord.Slow) reach = Rect.MinMaxRect(-38f * s, -38f * s, 38f * s, 38f * s);
            else if (w == SlotWord.Preview) reach = Rect.MinMaxRect(-88f * s * Mathf.Sin(ahead), 60f * s * Mathf.Cos(ahead), 88f * s * Mathf.Sin(ahead), 88f * s);
            else if (w == SlotWord.Run) reach = Rect.MinMaxRect(-88f * s, 0f, 88f * s, 88f * s);
            else reach = Rect.MinMaxRect(-60f * s, -60f * s, 60f * s, 60f * s);
            var half = SlotHalfSize(w, L);
            var lift = 33f * s + 14f;
            var above = Rect.MinMaxRect(origin.x + reach.xMin - half.x, origin.y + lift + reach.yMin - half.y,
                origin.x + reach.xMax + half.x, origin.y + lift + reach.yMax + half.y);
            // Under the ring: the cap top at origin − 60·Scale − 14, the keyline round it.
            var top = origin.y - 60f * s - 14f + (half.y - 17f * BayonCapHeightEm * .5f);
            var below = Rect.MinMaxRect(origin.x - half.x, top - 2f * half.y, origin.x + half.x, top);
            slotPlaces[(int)w] = SlotClear(above, L) ? SlotPlace.Above : SlotClear(below, L) ? SlotPlace.Below : SlotPlace.Hidden;
        }
    }

    /// <summary>Half the word's drawn size: Bayon 17's width and cap height, plus the keyline (× L.Scale) all round.</summary>
    Vector2 SlotHalfSize(SlotWord word, FrontRoomsTouchLayout L)
    {
        var t = live.LabelText;
        var width = t.cachedTextGeneratorForLayout.GetPreferredWidth(SlotText(word), t.GetGenerationSettings(Vector2.zero)) / Mathf.Max(.01f, t.pixelsPerUnit);
        var keyline = FrontRoomsHudStyle.KeylineDistance * L.Scale;
        return new Vector2(width * .5f + keyline, 17f * BayonCapHeightEm * .5f + keyline);
    }

    bool SlotClear(Rect r, FrontRoomsTouchLayout L)
    {
        var left = Mathf.Max(L.SafeLeft, L.EdgeGuard);
        var right = L.Frame.x - Mathf.Max(L.SafeRight, L.EdgeGuard);
        if (r.xMin < left || r.xMax > right || r.yMin < L.SafeBottom || r.yMax > L.Frame.y - L.SafeTop) return false;
        foreach (var k in slotKeepOut) if (k.Overlaps(r)) return false;
        if (!controls.RunStickMode)
        {
            // The RUN button's disc, at its hit radius.
            var c = L.SprintButtonCenter;
            var nearest = new Vector2(Mathf.Clamp(c.x, r.xMin, r.xMax), Mathf.Clamp(c.y, r.yMin, r.yMax));
            if ((nearest - c).sqrMagnitude < L.SprintButtonHitRadius * L.SprintButtonHitRadius) return false;
        }
        return true;
    }

    /// <summary>
    /// The slot's word this frame (X2 §3.3): WINDED, then the running RUN (STICK only), then the RUN preview and SLOW ·
    /// UNHEARD while they still teach. A higher word cuts in at once and fades in over 0.14 s; anything else waits for
    /// the current word to fade out (0.10 s) where it stands. The preview turns into the running RUN in place.
    /// </summary>
    void TickSlot(FrontRoomsTouchLayout L, bool touched, bool running, bool winded, Vector2 thumb)
    {
        var dt = Time.deltaTime;
        // SLOW · UNHEARD's dwell: in after 0.25 s of continuous Slow, out after 0.20 s without (walk starts pass Slow).
        if (touched && controls.CurrentGait == FrontRoomsGait.Slow)
        {
            slotSlowOutFor = 0f;
            slotSlowFor += dt;
            if (slotSlowFor >= .25f) slotSlowDwelled = true;
        }
        else
        {
            slotSlowFor = 0f;
            slotSlowOutFor += dt;
            if (slotSlowOutFor >= .20f) slotSlowDwelled = false;
        }

        var want = SlotWord.None;
        if (touched)
        {
            if (winded) want = SlotWord.Winded;
            else if (running && controls.RunStickMode) want = SlotWord.Run;
            else if (slotTeachRun && !running && controls.RunReach > 0f) want = SlotWord.Preview;
            else if (slotTeachSlow && slotSlowDwelled) want = SlotWord.Slow;
            if (slotPlaces[(int)want] == SlotPlace.Hidden) want = SlotWord.None;
        }

        if (want != slotShown)
        {
            if (slotShown == SlotWord.Preview && want == SlotWord.Run && !slotLeaving) slotShown = SlotWord.Run;
            else if (want > slotShown)
            {
                slotShown = want;
                slotLeaving = false;
                labelAlpha.Snap(0f);
                labelAlpha.To(1f, Dur(SlotIn), Ease.OutCubic);
            }
            else
            {
                if (!slotLeaving)
                {
                    slotLeaving = true;
                    slotFrozen = live.Label.anchoredPosition;
                    labelAlpha.To(0f, Dur(SlotOut), Ease.InCubic);
                }
                if (labelAlpha.Current <= .001f)
                {
                    slotLeaving = false;
                    slotShown = want;
                    if (want != SlotWord.None) { labelAlpha.Snap(0f); labelAlpha.To(1f, Dur(SlotIn), Ease.OutCubic); }
                }
            }
        }
        else if (slotLeaving && want != SlotWord.None)
        {
            slotLeaving = false;
            labelAlpha.To(1f, Dur(SlotIn), Ease.OutCubic);
        }

        live.LabelText.text = SlotText(slotShown);
        // WINDED and the words that teach in paper; only the running RUN is yellow.
        live.LabelText.color = WithAlpha(slotShown == SlotWord.Run ? Accent : Paper, labelAlpha);
        Vector2 at;
        if (slotLeaving) at = slotFrozen;
        else if (slotPlaces[(int)slotShown] == SlotPlace.Below)
        {
            // Hung under the ring, out of the thumb's way; it does not follow the thumb.
            var caps = Caps(live.LabelText);
            at = new Vector2(0f, -60f * L.Scale - 14f - caps.w * .5f - caps.y);
        }
        else at = thumb + new Vector2(0f, 33f * L.Scale + 14f - (Reduced ? 0f : 6f * (1f - labelAlpha)));
        live.Label.anchoredPosition = at;

        // Retiring (X2 §3.4): the flag is set when met; the word leaves from the next touch.
        var host = controls.Host;
        var slowShowing = slotShown == SlotWord.Slow && !slotLeaving && labelAlpha.Current > .5f;
        if (slowShowing && host != null && host.RelayEverReleased && host.CreepCounts) slotCreepFor += dt;
        else slotCreepFor = 0f;
        if (slotCreepFor >= 1.5f && !FrontRoomsTouchControls.TaughtSlow) FrontRoomsTouchControls.SetTaught(true, true);
        if (slotShown == SlotWord.Preview && !slotLeaving) FrontRoomsTouchControls.PreviewShownFor += dt;
        if (FrontRoomsTouchControls.PreviewShownFor >= 4f && !FrontRoomsTouchControls.TaughtRun) FrontRoomsTouchControls.SetTaught(false, true);
    }

    /// <summary>
    /// The Figma stick states (Touch / Stick): rest (the ghost), slow (the inner ring lit), walk, run (yellow thumb
    /// past the yellow line) and winded (muted). The rings and lines are text-free ghosts, so CONTROLS OPACITY
    /// thins them; the running thumb and line are the state itself and stay solid.
    /// </summary>
    static void ApplyStickStyle(StickView s, float activeness, float run, float reach, float slow, float winded, float opacity, bool runLine)
    {
        var baseFill = Mathf.Lerp(.18f, .28f, activeness);
        var ring = Mathf.Lerp(.35f, .6f, activeness);
        var thumb = Mathf.Lerp(.30f, .9f, activeness);
        s.BaseDisc.color = WithAlpha(Media, Mathf.Min(1f, baseFill * opacity));
        s.BaseRing.color = WithAlpha(Paper, Mathf.Min(1f, ring * opacity));
        s.SlowRingDashed.color = WithAlpha(Paper, Mathf.Min(1f, .25f * activeness * (1f - slow) * (1f - run) * opacity));
        s.SlowRingLit.color = WithAlpha(Paper, Mathf.Min(1f, .6f * activeness * slow * opacity));
        var line = runLine ? activeness : 0f;
        var lineColor = Color.Lerp(Paper, Muted, winded);
        s.RunLineDashed.color = WithAlpha(lineColor, Mathf.Min(1f, Mathf.Lerp(.35f, .75f, reach * (1f - winded)) * line * (1f - run) * opacity));
        s.RunLineLit.color = WithAlpha(Accent, line * run);
        var thumbColor = Color.Lerp(Color.Lerp(Paper, Accent, run), Muted, winded);
        s.ThumbDisc.color = WithAlpha(thumbColor, Mathf.Min(1f, Mathf.Lerp(thumb, 1f, run) * Mathf.Lerp(1f, .85f, winded) * (run > .5f ? 1f : opacity)));
    }

    void TickUse(FrontRoomsTouchLayout L, float opacity)
    {
        var prompt = controls.CurrentUsePrompt;
        var state = controls.CurrentMenuState;
        var visible = prompt.Visible && (state == FrontRoomsTouchControls.MenuState.Playing || state == FrontRoomsTouchControls.MenuState.Inspect);
        if (visible && useAlpha.Target < .5f)
        {
            useAlpha.To(1f, Dur(.16f), Ease.OutCubic);
            if (!Reduced) useScale.Restart(.86f, 1f, .2f, Ease.OutBack);
        }
        else if (!visible && useAlpha.Target > .5f)
        {
            useAlpha.To(0f, Dur(.12f), Ease.InCubic);
            if (!Reduced) useScale.To(.94f, .12f, Ease.InCubic);
        }
        // A broken door's answer (the map's Broken, for the door USE names): USE swings once round its top point, as
        // the leaf hangs from its hinge, the bottom inward. LOCKED shakes sideways instead (a rattle). Reduce Motion:
        // no swing; the press's ring flash and the knock say it.
        if (controls.BrokenTriedAt != brokenSwingSeen)
        {
            brokenSwingSeen = controls.BrokenTriedAt;
            if (!Reduced) brokenSwing.Restart(0f, 1f, FrontRoomsShotTimings.BrokenDoor.Duration, Ease.Linear);
        }
        var swing = Reduced ? 0f : (controls.LeftHanded ? 1f : -1f) * BrokenSwing(brokenSwing.Current);
        var hang = new Vector2(Mathf.Sin(swing * Mathf.Deg2Rad), 1f - Mathf.Cos(swing * Mathf.Deg2Rad)) * L.UseRadius;
        var useGive = controls.UseCenterNow - controls.UseCenterNowBase;
        GiveWay(useGiveX, useGive.x, controlsGroup.alpha < .01f);
        GiveWay(useGiveY, useGive.y, controlsGroup.alpha < .01f);
        useRoot.anchoredPosition = controls.UseCenterNowBase + new Vector2(useGiveX, useGiveY) + new Vector2(Reduced ? 0f : Shake(lockedShake.Current), 0f) + hang;
        useRoot.localRotation = Quaternion.Euler(0f, 0f, swing);
        // A camera takeover fades USE with the desktop HUD (关卡设计, 2026-10-08), except under a holding thumb:
        // the glass hold is a takeover too, and its ring is the hold's feedback. Visual only; presses still go to the rig.
        useTakeover.To(controls.UseHeld ? 0f : controls.Takeover, Dur(.12f), Ease.OutCubic);
        useGroup.alpha = useAlpha * (1f - useTakeover);

        var held = controls.UseHeld;
        if (held != wasUseHeld)
        {
            usePress.To(held ? 1f : 0f, Dur(held ? .07f : .14f), Ease.OutCubic);
            if (!Reduced) useScale.Restart(useScale.Current, held ? .92f : 1f, held ? .07f : .18f, held ? Ease.OutCubic : Ease.OutBack);
            if (held && prompt.Kind == FrontRoomsTouchControls.UseKind.Locked && !Reduced) lockedShake.Restart(0f, 1f, .24f, Ease.Linear);
            wasUseHeld = held;
        }
        useRoot.localScale = Vector3.one * (Reduced ? 1f : useScale.Current);

        var label = UseVerb(prompt);
        if (visible && label != useLabelShown)
        {
            // The verb changes by crossfade (OPEN ↔ SHUT) instead of a jump.
            if (string.IsNullOrEmpty(useLabelShown) || useAlpha.Current < .05f)
            {
                (useLabelOnA ? useTextA : useTextB).text = label;
                labelSwap.Snap(1f);
            }
            else
            {
                useLabelOnA = !useLabelOnA;
                (useLabelOnA ? useTextA : useTextB).text = label;
                labelSwap.Restart(0f, 1f, Dur(.12f), Ease.OutCubic);
            }
            useLabelShown = label;
        }
        // LOCKED and BROKEN are refusals: the verb in muted ink, and a press says no instead of "go".
        var refused = prompt.Kind == FrontRoomsTouchControls.UseKind.Locked || prompt.Kind == FrontRoomsTouchControls.UseKind.Broken || prompt.Locked;
        var press = usePress.Current;
        var textColor = refused ? Muted : Color.Lerp(Paper, Ink, press);
        var current = useLabelOnA ? useTextA : useTextB;
        var previous = useLabelOnA ? useTextB : useTextA;
        current.color = WithAlpha(textColor, labelSwap);
        previous.color = WithAlpha(textColor, 1f - labelSwap);
        current.rectTransform.anchoredPosition = new Vector2(0f, Reduced ? 0f : -4f * (1f - labelSwap));
        previous.rectTransform.anchoredPosition = new Vector2(0f, Reduced ? 0f : 4f * labelSwap);

        // A control that carries a word is solid (平面视觉, 2026-10-07): the verb reads on any room, and
        // CONTROLS OPACITY only thins the text-free ghosts (its ring here, the stick, the keycap).
        useDisc.color = CardColor;
        // A refused press never fills (no yellow "go", and no paper tint either: it greyed the solid disc and took
        // LOCKED to 2.6 : 1 in run 15). The text-free ring flashes to full paper instead; the shake and the rattle
        // say no to LOCKED, the swing and the knock to BROKEN.
        usePressed.color = refused ? WithAlpha(Accent, 0f) : WithAlpha(Accent, press);
        useRing.color = WithAlpha(Paper, refused ? Mathf.Lerp(.3f, 1f, press) : Mathf.Min(1f, .9f * opacity + .3f));

        // The hold ring sits outside the thumb, so the progress stays visible under a pressing thumb.
        var hold = visible && prompt.Hold;
        holdTrackAlpha.To(hold ? 1f : 0f, Dur(.12f), Ease.OutCubic);
        holdShown = Mathf.MoveTowards(holdShown, hold ? prompt.Progress : 0f, FrontRoomsTouchMotion.Delta / .06f);
        holdTrack.color = WithAlpha(Paper, .3f * holdTrackAlpha);
        holdArc.color = WithAlpha(Accent, holdTrackAlpha);
        holdArc.fillAmount = holdShown;
    }

    readonly Dictionary<string, Vector4> capsCache = new Dictionary<string, Vector4>();

    /// <summary>
    /// The caps of a Text's word (pt), for the ink chip: x, y = where their middle sits from the centre of the
    /// line box, z = their width, w = their height. Laid out once per word and density from the glyph quads, so
    /// the chip hugs the letters rather than Bayon's line metrics or side bearings.
    /// </summary>
    Vector4 Caps(Text t)
    {
        if (string.IsNullOrEmpty(t.text)) return Vector4.zero;
        var key = t.text + "@" + t.pixelsPerUnit + "@" + (t.font != null ? t.font.name : string.Empty) + t.fontSize;
        if (capsCache.TryGetValue(key, out var caps)) return caps;
        var generator = new TextGenerator();
        generator.Populate(t.text, t.GetGenerationSettings(t.rectTransform.rect.size));
        float low = float.MaxValue, high = float.MinValue, left = float.MaxValue, right = float.MinValue;
        foreach (var v in generator.verts)
        {
            low = Mathf.Min(low, v.position.y);
            high = Mathf.Max(high, v.position.y);
            left = Mathf.Min(left, v.position.x);
            right = Mathf.Max(right, v.position.x);
        }
        var ppu = Mathf.Max(.01f, t.pixelsPerUnit);
        // The quads (and the rasterised glyph bounds) carry the same padding on every side: their middle is the
        // caps' middle; the height is Bayon's cap height, 0.714 em (平面视觉's figure; runs 11-12 drew 21.3 pt
        // chips from raster bounds); that padding, read off the height, comes off the width too (run 14 measured
        // the advance width 0.7-1.5 pt wide).
        if (generator.vertexCount == 0) return Vector4.zero;
        var capHeight = t.fontSize * BayonCapHeightEm;
        var padding = Mathf.Max(0f, ((high - low) / ppu - capHeight) * .5f);
        caps = new Vector4((left + right) * .5f / ppu, (low + high) * .5f / ppu, (right - left) / ppu - 2f * padding, capHeight);
        capsCache[key] = caps;
        return caps;
    }

    static float Shake(float t) => t >= 1f ? 0f : Mathf.Sin(t * Mathf.PI * 6f) * 4f * (1f - t);

    const float BrokenSwingDegrees = 4f;

    /// <summary>
    /// B·4's won't-shut on the crosshair (Red, 10-14), on the disc, t 0..1 over BrokenDoor.Duration: out to
    /// BrokenSwingDegrees in BrokenKnockSeconds, then it meets the frame and falls back, a 2.6 Hz swing with
    /// damping 0.3 that is all but still by the end.
    /// </summary>
    static float BrokenSwing(float t)
    {
        if (t >= 1f) return 0f;
        var s = t * FrontRoomsShotTimings.BrokenDoor.Duration;
        var knock = FrontRoomsTouchControls.BrokenKnockSeconds;
        if (s < knock)
        {
            var u = 1f - s / knock;
            return BrokenSwingDegrees * (1f - u * u * u);
        }
        var w = 2f * Mathf.PI * 2.6f;
        var after = s - knock;
        return BrokenSwingDegrees * Mathf.Exp(-.3f * w * after) * Mathf.Cos(w * Mathf.Sqrt(1f - .3f * .3f) * after);
    }

    static string UseVerb(FrontRoomsTouchControls.UsePrompt prompt) => FrontRoomsLanguage.T(UseEnglish(prompt));

    static string UseEnglish(FrontRoomsTouchControls.UsePrompt prompt)
    {
        switch (prompt.Kind)
        {
            case FrontRoomsTouchControls.UseKind.Open: return "OPEN";
            case FrontRoomsTouchControls.UseKind.Shut: return "SHUT";
            case FrontRoomsTouchControls.UseKind.Take: return "TAKE";
            case FrontRoomsTouchControls.UseKind.Locked: return "LOCKED";
            case FrontRoomsTouchControls.UseKind.Broken: return "BROKEN";
            case FrontRoomsTouchControls.UseKind.Read: return "READ";
            case FrontRoomsTouchControls.UseKind.Examine: return "EXAMINE";
            case FrontRoomsTouchControls.UseKind.Watch: return "WATCH";
            case FrontRoomsTouchControls.UseKind.Peek: return FrontRoomsInspectCatalog.PromptPeekTouch;
            case FrontRoomsTouchControls.UseKind.Unlock: return "UNLOCK";
            case FrontRoomsTouchControls.UseKind.Back: return "BACK";
            case FrontRoomsTouchControls.UseKind.Hold: return prompt.TapMode ? "TAP" : "BREAK";
            case FrontRoomsTouchControls.UseKind.Tap: return "TAP";
            default: return string.Empty;
        }
    }

    // ================================================================= menus

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    void BuildFrost()
    {
        var frost = Node("Frost", menuLayer, Vector2.zero);
        Stretch(frost);
        frostGroup = frost.gameObject.AddComponent<CanvasGroup>();
        frostGroup.alpha = 0f;
        frostGroup.blocksRaycasts = false;
        frostGroup.interactable = false;
        frostFrame = Node("Paused frame (blurred)", frost, Vector2.zero).gameObject.AddComponent<RawImage>();
        Stretch(frostFrame.rectTransform);
        frostFrame.raycastTarget = false;
        frostFrame.enabled = false;
        frostVeil = Img("Paper veil", frost, Vector2.zero, WithAlpha(WashColor, FrostVeil));
        Stretch(frostVeil.rectTransform);
        frostPaper = Node("Wallpaper paper + print", frost, Vector2.zero).gameObject.AddComponent<RawImage>();
        Stretch(frostPaper.rectTransform);
        frostPaper.raycastTarget = false;
        var paper = Resources.Load<Texture2D>("UI/Touch/TouchFrostPaper");
        if (paper != null && paper.wrapMode != TextureWrapMode.Repeat) paper.wrapMode = TextureWrapMode.Repeat;
        frostPaper.texture = paper;
        frostPaper.enabled = paper != null;
    }

    static bool Washed(FrontRoomsTouchControls.MenuState s) =>
        s == FrontRoomsTouchControls.MenuState.Paused || s == FrontRoomsTouchControls.MenuState.Settings || s == FrontRoomsTouchControls.MenuState.Caught;

    /// <summary>
    /// Freezes the frame under the frost: the game camera alone (no HUD), at half resolution, halved down to
    /// 1/16 of the screen and back up to 1/4 with bilinear steps (frosted, not just out of focus). Once per
    /// pause, so it costs one extra camera render on the press.
    /// </summary>
    void CaptureFrost()
    {
        ReleaseFrost();
        var cam = controls.Host != null ? controls.Host.BackdropCamera : null;
        if (cam == null || !cam.isActiveAndEnabled) return;
        var px = FrontRoomsHandheld.ScreenPixels;
        var w = Mathf.Max(64, Mathf.RoundToInt(px.x * .5f));
        var h = Mathf.Max(32, Mathf.RoundToInt(px.y * .5f));
        var src = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var previous = cam.targetTexture;
        try
        {
            cam.targetTexture = src;
            cam.Render();
        }
        finally
        {
            cam.targetTexture = previous;
        }
        for (var i = 0; i < 5; i++)
        {
            if (i < 3) { w = Mathf.Max(1, w / 2); h = Mathf.Max(1, h / 2); }
            else { w *= 2; h *= 2; }
            var next = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            next.filterMode = FilterMode.Bilinear;
            Graphics.Blit(src, next);
            RenderTexture.ReleaseTemporary(src);
            src = next;
        }
        frostTexture = src;
        frostFrame.texture = frostTexture;
    }

    void ReleaseFrost()
    {
        if (frostFrame != null) frostFrame.texture = null;
        if (frostTexture == null) return;
        RenderTexture.ReleaseTemporary(frostTexture);
        frostTexture = null;
    }

    void BuildMenus()
    {
        BuildFrost();

        // Pause card (Figma TS 6 · PAUSE).
        pauseTitle = MakeItem("PAUSED", Txt("PAUSED", menuLayer, "PAUSED", bayon, 48, Ink, TextAnchor.MiddleCenter, new Vector2(600f, 48f), true).rectTransform);
        pauseMetaText = Txt("Pause meta", menuLayer, string.Empty, plex, 11, Ink, TextAnchor.MiddleCenter, new Vector2(700f, 16f), true);
        pauseMeta = MakeItem("meta", pauseMetaText.rectTransform);
        legendA = MakeItem("legend 1", Node("Legend 1", menuLayer, new Vector2(700f, 24f)));
        legendB = MakeItem("legend 2", Node("Legend 2", menuLayer, new Vector2(700f, 24f)));
        pauseLegend = BuildLegend(legendA.Rect, legendB.Rect, Ink, false);
        confirmTextText = Txt("Restart question", menuLayer, "END THIS RUN?", bayon, 17, Ink, TextAnchor.MiddleCenter, new Vector2(400f, 22f), true);
        confirmText = MakeItem("confirm", confirmTextText.rectTransform);

        foreach (var b in new[] { FrontRoomsTouchControls.Button.Resume, FrontRoomsTouchControls.Button.Settings, FrontRoomsTouchControls.Button.Restart, FrontRoomsTouchControls.Button.ReturnTitle,
                     FrontRoomsTouchControls.Button.RestartCancel, FrontRoomsTouchControls.Button.RestartConfirm, FrontRoomsTouchControls.Button.TryAgain,
                     FrontRoomsTouchControls.Button.SettingsClose })
            BuildChip(b);

        // Caught card (Figma TS 8 · CAUGHT).
        caughtTitle = MakeItem("CAUGHT", Txt("CAUGHT", menuLayer, "CAUGHT", bayon, 48, Ink, TextAnchor.MiddleCenter, new Vector2(600f, 48f), true).rectTransform);
        // The subtitle under CAUGHT (叙事设计 40 §6; 平面视觉 FIX_CARD_VL217 A7: Bayon 17 ink, 600 × 22, centred).
        caughtSubtitle = MakeItem("OCCUPANT ACCOUNTED FOR", Txt("Caught subtitle", menuLayer, "OCCUPANT ACCOUNTED FOR", bayon, 17, Ink, TextAnchor.MiddleCenter, new Vector2(600f, 22f), true).rectTransform);
        // C04 took TIER off CAUGHT; the third number is the keys taken (叙事设计 40 §6).
        var labels = new[] { "SURVIVED", "ZONES CROSSED", "KEYS TAKEN", "DOORS IT BROKE" };
        for (var i = 0; i < 4; i++)
        {
            caughtNumberTexts[i] = Txt("stat " + i, menuLayer, "0", serif, 26, Ink, TextAnchor.MiddleCenter, new Vector2(180f, 30f), true);
            caughtNumbers[i] = MakeItem("stat", caughtNumberTexts[i].rectTransform);
            caughtLabels[i] = MakeItem("stat label", Txt("stat label " + i, menuLayer, labels[i], bayon, 17, Ink, TextAnchor.MiddleCenter, new Vector2(180f, 20f), true).rectTransform);
        }
        var rule = Img("Caught rule", menuLayer, new Vector2(718f, 1f), WithAlpha(Ink, .2f), true);
        rule.sprite = FrontRoomsTouchSprites.Solid();
        caughtRule = MakeItem("rule", rule.rectTransform);
        caughtLineText = Txt("Caught line", menuLayer, string.Empty, serif, 20, Ink, TextAnchor.MiddleCenter, new Vector2(700f, 24f), true);
        caughtLine = MakeItem("line", caughtLineText.rectTransform);

        // Title lockup (X2, 平面视觉's board 3564:7362, option c): TAP TO START in paper with the keyline, no chip, and
        // the legend under it, word for word the pause card's, also paper with the keyline. Static once in (no alpha
        // ramp on words); it goes with the title at the first input.
        var promptRoot = Node("Title prompt", menuLayer, new Vector2(300f, 22f));
        titlePromptText = Txt("TAP TO START", promptRoot, "TAP TO START", bayon, 17, Paper, TextAnchor.MiddleCenter, new Vector2(300f, 22f));
        Keyline(titlePromptText, FrontRoomsHudStyle.KeylineDistance);
        titleLegend = BuildLegend(Node("Title legend 1", promptRoot, new Vector2(700f, 24f), false), Node("Title legend 2", promptRoot, new Vector2(700f, 24f), false), Paper, true);
        titlePrompt = MakeItem("TAP TO START", promptRoot);

        BuildSettingsCard();
        // BACK sits on the card: draw it above the card (it was built first and the opaque card hid it).
        chips[FrontRoomsTouchControls.Button.SettingsClose].Rect.SetAsLastSibling();
    }

    Item MakeItem(string name, RectTransform rt)
    {
        var item = new Item { Rect = rt, Group = rt.gameObject.AddComponent<CanvasGroup>() };
        item.Group.alpha = 0f;
        item.Group.blocksRaycasts = false;
        return item;
    }

    /// <summary>
    /// The legend (X2 §3.2): row A MOVE / DRAG · LOOK / SLOW · UNHEARD / PAST THE RING · RUN (or RUN BUTTON), row B
    /// USE · DOOR / HOLD USE · BREAK GLASS. With body doors (DL2) SLOW · QUIET STEPS, and row B leads with the door lesson,
    /// WALK INTO DOORS  ·  STEP BACK TO SHUT, on the stick's glyph. IBM Plex Mono 11 caps (iPad 12) beside 22 pt glyphs, ink on the pause frost,
    /// paper with the keyline on the title (平面视觉).
    /// </summary>
    Legend BuildLegend(RectTransform rowA, RectTransform rowB, Color color, bool keyline)
    {
        var l = new Legend { RowA = rowA, RowB = rowB };
        l.Move = LegendEntry(l, rowA, FrontRoomsTouchSprites.Glyph.Move, "MOVE", color, keyline, out _);
        l.Drag = LegendEntry(l, rowA, FrontRoomsTouchSprites.Glyph.Drag, "DRAG · LOOK", color, keyline, out _);
        l.Slow = LegendEntry(l, rowA, FrontRoomsTouchSprites.Glyph.Slow, FrontRooms3DGame.BodyDoorsActive ? "SLOW · QUIET STEPS" : "SLOW · UNHEARD", color, keyline, out _);
        l.Sprint = LegendEntry(l, rowA, FrontRoomsTouchSprites.Glyph.Run, "PAST THE RING · RUN", color, keyline, out l.SprintGlyph);
        l.Use = LegendEntry(l, rowB, FrontRoomsTouchSprites.Glyph.Use, "USE · DOOR", color, keyline, out _);
        l.Hold = LegendEntry(l, rowB, FrontRoomsTouchSprites.Glyph.Hold, "HOLD USE · BREAK GLASS", color, keyline, out _);
        return l;
    }

    int LegendSize => FrontRoomsHandheld.IsTablet ? 12 : 11;

    Text LegendEntry(Legend l, RectTransform row, FrontRoomsTouchSprites.Glyph glyph, string label, Color color, bool keyline, out Image glyphImage)
    {
        glyphImage = Img("glyph " + glyph, row, new Vector2(22f, 22f), color);
        glyphImage.name = "glyph:" + glyph;
        var t = Txt("legend " + label, row, label, plex, LegendSize, color, TextAnchor.MiddleLeft, new Vector2(300f, 16f));
        if (keyline)
        {
            Keyline(glyphImage, FrontRoomsHudStyle.KeylineDistance);
            Keyline(t, FrontRoomsHudStyle.KeylineDistance);
        }
        l.Glyphs[t] = glyphImage;
        return t;
    }

    /// <summary>The run entry follows the RUN setting; the hold entry follows TAP mode. With a pad or a keyboard in use the six
    /// entries name its controls as bound (2026-11-01: the T5 pairs, glyphs hidden; a keyboard keeps DRAG · LOOK and its glyph,
    /// as the finger still looks). Then both rows are laid out. The title and the pause card call it every frame, so a device
    /// change shows at once.</summary>
    void UpdateLegend(Legend l, float availableWidth)
    {
        // DL2: set every time the card shows. Built once at start-up, when BodyDoorsActive is still false, these went stale.
        var doors = FrontRooms3DGame.DoorLessonShown;
        var touch = controls.Mode == FrontRoomsTouchControls.InputMode.Touch;
        if (touch)
        {
            l.Move.text = FrontRoomsLanguage.T("MOVE");
            l.Drag.text = FrontRoomsLanguage.T("DRAG · LOOK");
            l.Sprint.text = FrontRoomsLanguage.T(controls.RunStickMode ? "PAST THE RING · RUN" : "RUN BUTTON");
            l.Hold.text = FrontRoomsLanguage.T(FrontRoomsSettings.TapToBreak ? "TAP USE · BREAK GLASS" : "HOLD USE · BREAK GLASS");
            l.Slow.text = FrontRoomsLanguage.T(doors ? "SLOW · QUIET STEPS" : "SLOW · UNHEARD");
            l.Use.text = FrontRoomsLanguage.T(doors ? FrontRooms3DGame.DoorLessonLine : "USE · DOOR");
        }
        else
        {
            var pad = controls.Mode == FrontRoomsTouchControls.InputMode.Gamepad;
            l.Move.text = FrontRoomsInput.LegendPair(FrontRoomsInput.LegendPart.Move, pad);
            l.Drag.text = pad ? FrontRoomsInput.LegendPair(FrontRoomsInput.LegendPart.Look, true) : FrontRoomsLanguage.T("DRAG · LOOK");
            l.Slow.text = FrontRoomsInput.LegendPair(FrontRoomsInput.LegendPart.Slow, pad);
            l.Sprint.text = FrontRoomsInput.LegendPair(FrontRoomsInput.LegendPart.Sprint, pad);
            l.Use.text = doors ? FrontRoomsLanguage.T(FrontRooms3DGame.DoorLessonLine) : FrontRoomsInput.LegendPair(FrontRoomsInput.LegendPart.Use, pad);
            var use = pad ? FrontRoomsInput.ControlName(FrontRoomsInput.Bind.PadUse) : FrontRoomsInput.UseKey;
            l.Hold.text = FrontRoomsLanguage.F(FrontRoomsSettings.TapToBreak ? "TAP {0}" : "HOLD {0}", use) + "  " + FrontRoomsLanguage.T("BREAK GLASS");
        }
        // Touch shows every glyph; a pad none; a keyboard only DRAG · LOOK's.
        foreach (var pair in l.Glyphs) pair.Value.enabled = touch || (pair.Key == l.Drag && controls.Mode == FrontRoomsTouchControls.InputMode.Keyboard);
        l.SprintGlyph.sprite = FrontRoomsTouchSprites.GlyphSprite(controls.RunStickMode ? FrontRoomsTouchSprites.Glyph.Run : FrontRoomsTouchSprites.Glyph.SprintButton);
        if (l.Glyphs.TryGetValue(l.Use, out var useGlyph))
            useGlyph.sprite = FrontRoomsTouchSprites.GlyphSprite(doors ? FrontRoomsTouchSprites.Glyph.Move : FrontRoomsTouchSprites.Glyph.Use);
        l.RowsA = LayoutLegendRow(l, availableWidth, l.Move, l.Drag, l.Slow, l.Sprint);
        l.RowsB = LayoutLegendRow(l, availableWidth, l.Use, l.Hold);
    }

    // A shown glyph's slot before its label (a hidden one leaves none, 2026-11-01).
    static float GlyphSlot(Legend l, Text entry) => l.Glyphs.TryGetValue(entry, out var glyph) && glyph.enabled ? 28f : 0f;

    int LayoutLegendRow(Legend l, float availableWidth, params Text[] entries)
    {
        // Wrap whole glyph/label pairs; controls keep their point size when the view narrows.
        var widths = new float[entries.Length];
        var lines = new List<List<int>> { new List<int>() };
        var lineWidth = 0f;
        for (var i = 0; i < entries.Length; i++)
        {
            widths[i] = GlyphSlot(l, entries[i]) + entries[i].preferredWidth;
            if (lines[lines.Count - 1].Count > 0 && lineWidth + 26f + widths[i] > availableWidth)
            {
                lines.Add(new List<int>());
                lineWidth = 0f;
            }
            if (lineWidth > 0f) lineWidth += 26f;
            lines[lines.Count - 1].Add(i);
            lineWidth += widths[i];
        }
        for (var row = 0; row < lines.Count; row++)
        {
            var line = lines[row];
            var total = 26f * (line.Count - 1);
            foreach (var i in line) total += widths[i];
            var x = -total * .5f;
            var y = ((lines.Count - 1) * .5f - row) * 28f;
            foreach (var i in line)
            {
                var entry = entries[i];
                var slot = GlyphSlot(l, entry);
                var labelWidth = widths[i] - slot;
                if (l.Glyphs.TryGetValue(entry, out var glyph)) glyph.rectTransform.anchoredPosition = new Vector2(x + 11f, y);
                entry.rectTransform.sizeDelta = new Vector2(labelWidth + 2f, 16f);
                entry.rectTransform.anchoredPosition = new Vector2(x + slot + labelWidth * .5f, y - Caps(entry).y);
                x += widths[i] + 26f;
            }
        }
        return lines.Count;
    }

    void ApplyMenuSprites()
    {
        foreach (var img in menuLayer.GetComponentsInChildren<Image>(true))
        {
            if (img.name.StartsWith("glyph:"))
            {
                var name = img.name.Substring(6);
                if (System.Enum.TryParse<FrontRoomsTouchSprites.Glyph>(name, out var g)) img.sprite = FrontRoomsTouchSprites.GlyphSprite(g);
            }
        }
        if (scrollBarImage != null) scrollBarImage.sprite = FrontRoomsTouchSprites.Solid();
        foreach (var r in rows)
        {
            if (r.Divider != null) r.Divider.sprite = FrontRoomsTouchSprites.Solid();
            if (r.Pressed != null) r.Pressed.sprite = FrontRoomsTouchSprites.Solid();
            if (r.Bar != null) r.Bar.sprite = FrontRoomsTouchSprites.Solid();
            if (r.Underlay != null) r.Underlay.sprite = FrontRoomsTouchSprites.Solid();
            if (r.NoteRule != null) r.NoteRule.sprite = FrontRoomsTouchSprites.Solid();
            if (r.Target != null) r.Target.sprite = FrontRoomsTouchSprites.Solid();
            foreach (var t in r.Trace) t.sprite = FrontRoomsTouchSprites.Solid();
        }
    }

    void BuildChip(FrontRoomsTouchControls.Button button)
    {
        var label = ChipLabel(button);
        var rt = Node("Chip " + label, menuLayer, new Vector2(80f, FrontRoomsTouchLayout.ChipHeight));
        var fill = rt.gameObject.AddComponent<Image>();
        var dark = DarkChip(button);
        var keyline = KeylineChip(button);
        fill.color = dark ? CardColor : keyline ? WithAlpha(Paper, 0f) : Accent;
        fill.raycastTarget = false;
        fill.sprite = FrontRoomsTouchSprites.Solid();
        if (keyline)
        {
            // A 1 pt keyline round the chip, stretched with it (平面视觉 SETTINGS_KIT_REVIEW_1024 fix 2).
            foreach (var edge in new[] { new Vector4(0f, 1f, 1f, 1f), new Vector4(0f, 0f, 1f, 0f), new Vector4(0f, 0f, 0f, 1f), new Vector4(1f, 0f, 1f, 1f) })
            {
                var e = Img("keyline", rt, Vector2.zero, WithAlpha(Paper, .5f));
                e.sprite = FrontRoomsTouchSprites.Solid();
                var ert = e.rectTransform;
                ert.anchorMin = new Vector2(edge.x, edge.y); ert.anchorMax = new Vector2(edge.z, edge.w);
                ert.sizeDelta = new Vector2(edge.x == edge.z ? 1f : 0f, edge.y == edge.w ? 1f : 0f);
                ert.anchoredPosition = new Vector2(edge.x == edge.z ? (edge.x == 0f ? .5f : -.5f) : 0f, edge.y == edge.w ? (edge.y == 0f ? .5f : -.5f) : 0f);
            }
        }
        var word = Txt("label", rt, label, bayon, 17, dark || keyline ? Paper : Ink, TextAnchor.MiddleCenter, new Vector2(140f, 22f));
        word.rectTransform.anchoredPosition = new Vector2(0f, 1f);
        var item = MakeItem(label, rt);
        chips[button] = item;
        chipFills[button] = fill;
        chipLabels[button] = word;
    }

    /// <summary>The title's prompt in the player's own terms: TAP, the pad's south button (A or CROSS), or SPACE.</summary>
    string TitlePromptWords() =>
        controls.Mode == FrontRoomsTouchControls.InputMode.Gamepad ? FrontRoomsLanguage.F("PRESS {0} TO START", FrontRoomsInput.PadName(FrontRoomsInput.PadButton.South))
        : FrontRoomsLanguage.T(controls.Mode == FrontRoomsTouchControls.InputMode.Keyboard ? "PRESS SPACE TO START" : "TAP TO START");

    /// <summary>An exit, not a go: QUIT TO TITLE sits on the solid ink chip with paper caps, so it never weighs as much as RESUME (平面视觉, 2026-10-08).</summary>
    static bool DarkChip(FrontRoomsTouchControls.Button b) => b == FrontRoomsTouchControls.Button.ReturnTitle;

    /// <summary>Settings BACK is a keyline chip, paper word, no fill: on a settings page yellow means focus only, since
    /// changes apply at once and there is no single primary action (平面视觉, SETTINGS_KIT_REVIEW_1024 fix 2).</summary>
    static bool KeylineChip(FrontRoomsTouchControls.Button b) => b == FrontRoomsTouchControls.Button.SettingsClose || b == FrontRoomsTouchControls.Button.Restart;

    static string ChipLabel(FrontRoomsTouchControls.Button b)
    {
        switch (b)
        {
            case FrontRoomsTouchControls.Button.Resume: return "RESUME";
            case FrontRoomsTouchControls.Button.Settings: return "SETTINGS";
            case FrontRoomsTouchControls.Button.Restart: return "END RUN";
            case FrontRoomsTouchControls.Button.ReturnTitle: return "QUIT TO TITLE";
            case FrontRoomsTouchControls.Button.RestartCancel: return "CANCEL";
            case FrontRoomsTouchControls.Button.RestartConfirm: return "RESTART";
            case FrontRoomsTouchControls.Button.TryAgain: return "TRY AGAIN";
            case FrontRoomsTouchControls.Button.SettingsClose: return "RESUME";
            default: return string.Empty;
        }
    }

    void BuildSettingsCard()
    {
        cardRoot = Node("Settings card", menuLayer, new Vector2(742f, 369f));
        var card = cardRoot.gameObject.AddComponent<Image>();
        settingsCardImage = card;
        // Opaque on touch: 3 % of a textured frost showing through would read as dirt, and the scroll fade must match it.
        // Black glass: keep the game/frost layer visible underneath while
        // preserving enough contrast for the settings text.
        card.color = new Color(CardColor.r, CardColor.g, CardColor.b, .88f);
        card.sprite = FrontRoomsTouchSprites.Solid();
        card.raycastTarget = false;
        settingsCard = MakeItem("card", cardRoot);
        cardHeading = Txt("SETTINGS", cardRoot, "SETTINGS", bayon, 17, Paper, TextAnchor.MiddleLeft, new Vector2(200f, 22f));
        cardViewportLeft = Node("Left column", cardRoot, Vector2.zero, false);
        cardViewportRight = Node("Touch column", cardRoot, Vector2.zero, false);
        cardViewportRight.gameObject.AddComponent<RectMask2D>();
        scrollBarImage = Img("scroll bar", cardRoot, new Vector2(2f, 40f), WithAlpha(Muted, 0f));
        scrollBar = scrollBarImage.rectTransform;
        // The tabs, beside SETTINGS (Figma 3688:7842): Bayon 17, the open one in full paper over a 2 pt rule.
        for (var i = 0; i < tabTexts.Length; i++)
        {
            tabTexts[i] = Txt("tab " + FrontRoomsSettingsMenu.TabEnglish(FrontRoomsSettingsMenu.Tabs[i]), cardRoot,
                FrontRoomsSettingsMenu.TabEnglish(FrontRoomsSettingsMenu.Tabs[i]), bayon, 17, Paper, TextAnchor.MiddleCenter, new Vector2(80f, 22f));
            tabRules[i] = Img("tab rule", cardRoot, new Vector2(40f, 2f), WithAlpha(Paper, 0f));
            tabRules[i].sprite = FrontRoomsTouchSprites.Solid();
        }
        // The picture column (a tablet's sheet): on GRAPHICS a live picture of the game at the current settings; a tap on it
        // shows the game full screen. What a row does is in the row (SETTINGS_MOTION.md).
        // ASSIST's first row (Red, 2026-10-30): on the card, not in the list, whose mask stops at the explanation column.
        languageStrip = FrontRoomsLanguageStrip.Create(cardRoot, serif);
        languageStrip.SetActive(false);
        controls.LanguageStrip = languageStrip;
        controls.ChipWordWidth = ChipWordWidth;
        detailRoot = Node("Explanation", cardRoot, Vector2.zero, false);
        previewKeyline = Img("preview keyline", detailRoot, Vector2.zero, WithAlpha(Paper, .35f));
        previewKeyline.sprite = FrontRoomsTouchSprites.Solid();
        preview = Node("preview", detailRoot, Vector2.zero, false).gameObject.AddComponent<RawImage>();
        preview.raycastTarget = false;
        previewCaption = Txt("preview caption", detailRoot, "TAP THE PICTURE TO SEE IT FULL SCREEN", plex, 11, WithAlpha(Paper, .75f), TextAnchor.UpperLeft, new Vector2(256f, 14f));
        // FRAME RATE NOW in the picture's bottom-right corner (Red, 2026-11-01), keylined like the HUD; no card behind it.
        previewFrameWord = Txt("preview frame word", detailRoot, "FRAME RATE NOW", plex, 11, WithAlpha(Paper, .75f), TextAnchor.LowerRight, new Vector2(256f, 14f));
        Keyline(previewFrameWord, FrontRoomsHudStyle.KeylineDistance);
        previewFrameLine = Txt("preview frame line", detailRoot, string.Empty, plex, 11, Paper, TextAnchor.LowerRight, new Vector2(256f, 14f));
        Keyline(previewFrameLine, FrontRoomsHudStyle.KeylineDistance);
        for (var i = 0; i < previewDashes.Length; i++)
        {
            previewDashes[i] = Img("preview frame dash", detailRoot, new Vector2(14f, 4f), WithAlpha(Paper, .2f));
            previewDashes[i].sprite = FrontRoomsTouchSprites.Solid();
            Keyline(previewDashes[i], FrontRoomsHudStyle.KeylineDistance);
        }
        // An open row's sentence is measured here for the list (its open height), never drawn.
        sentenceProbe = Txt("sentence probe", cardRoot, string.Empty, serif, SentenceSize, WithAlpha(Paper, 0f), TextAnchor.UpperLeft, new Vector2(300f, 40f));
        sentenceProbe.horizontalOverflow = HorizontalWrapMode.Wrap;
        sentenceProbe.lineSpacing = SentencePitch;
        sentenceProbe.enabled = false;
        controls.SettingsOpenHeight = OpenHeight;
    }

    /// <summary>The full-screen preview: the game at the new settings, a quiet line at the HUD's corner, no backing card.</summary>
    void BuildPeek()
    {
        peekLayer = Layer("Settings preview");
        peekTitle = Txt("preview title", peekLayer, "PREVIEW  ·  TAP ANYWHERE TO RETURN", bayon, 17, Paper, TextAnchor.UpperLeft, new Vector2(320f, 22f), true);
        peekLine = Txt("preview frame", peekLayer, string.Empty, plex, 11, Paper, TextAnchor.UpperLeft, new Vector2(320f, 14f), true);
        for (var i = 0; i < peekDashes.Length; i++)
        {
            peekDashes[i] = Img("frame dash", peekLayer, new Vector2(14f, 4f), WithAlpha(Paper, .2f), true);
            peekDashes[i].sprite = FrontRoomsTouchSprites.Solid();
        }
        peekLayer.gameObject.SetActive(false);
    }

    void TickPeek(FrontRoomsTouchLayout L)
    {
        var on = controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Settings && controls.SettingsPeek;
        if (!Mathf.Approximately(peekAlpha.Target, on ? 1f : 0f)) peekAlpha.To(on ? 1f : 0f, Dur(on ? .22f : .16f), on ? Ease.OutCubic : Ease.InCubic);
        menuGroup.alpha = 1f - peekAlpha.Current;
        var show = peekAlpha.Current > .001f;
        if (peekLayer.gameObject.activeSelf != show) peekLayer.gameObject.SetActive(show);
        if (!show) return;
        // The line keeps the HUD's place: 16 pt from the edges, and 16 pt from a rounded corner's curve.
        var frame = L.Frame;
        var block = FrontRoomsHandheld.ClearCornerBlock(new Rect(16f, frame.y - 16f - 46f, 320f, 46f));
        var fps = FrontRoomsQuality.MeasuredFps;
        var target = Mathf.Max(1, FrontRoomsQuality.TargetFps);
        peekTitle.rectTransform.anchoredPosition = new Vector2(block.x + 160f, block.yMax - 11f);
        peekLine.text = FrontRoomsLanguage.T("FRAME RATE NOW") + "   " + Mathf.RoundToInt(fps) + " FPS  ·  " + (1000f / Mathf.Max(1f, fps)).ToString("0.0") + " MS";
        peekLine.rectTransform.anchoredPosition = new Vector2(block.x + 160f, block.yMax - 32f);
        var lit = Mathf.Clamp(Mathf.RoundToInt(10f * fps / target), 0, 10);
        for (var i = 0; i < peekDashes.Length; i++)
        {
            peekDashes[i].rectTransform.anchoredPosition = new Vector2(block.x + 7f + i * 17f, block.yMax - 44f);
            peekDashes[i].color = WithAlpha(Paper, (i < lit ? 1f : .2f) * peekAlpha.Current);
        }
        peekTitle.color = WithAlpha(Paper, peekAlpha.Current);
        peekLine.color = WithAlpha(Paper, .8f * peekAlpha.Current);
    }

    /// <summary>The GRAPHICS preview: the game camera alone (no HUD) into a texture, at the current settings.</summary>
    void RenderPreview()
    {
        var cam = controls.Host != null ? controls.Host.BackdropCamera : null;
        if (!previewWanted || cam == null || !cam.isActiveAndEnabled) { ReleasePreview(); return; }
        var size = preview.rectTransform.sizeDelta * Mathf.Max(1f, FrontRoomsHandheld.PointScale);
        var w = Mathf.Clamp(Mathf.RoundToInt(size.x), 32, 960);
        var h = Mathf.Clamp(Mathf.RoundToInt(size.y), 18, 720);
        var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
        var msaa = urp != null ? Mathf.Max(1, urp.msaaSampleCount) : 1;
        if (previewRt == null || previewRt.width != w || previewRt.height != h || previewRt.antiAliasing != msaa)
        {
            ReleasePreview();
            previewRt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { name = "Settings preview", antiAliasing = msaa };
            previewRt.Create();
            preview.texture = previewRt;
        }
        var previous = cam.targetTexture;
        try
        {
            cam.targetTexture = previewRt;
            cam.Render();
        }
        finally
        {
            cam.targetTexture = previous;
        }
    }

    void ReleasePreview()
    {
        if (preview != null) preview.texture = null;
        if (previewRt == null) return;
        previewRt.Release();
        Destroy(previewRt);
        previewRt = null;
    }

    RowView MakeRow(bool header)
    {
        var r = new RowView { Header = header };
        r.Rect = Node(header ? "Section" : "Row", cardRoot, new Vector2(FrontRoomsTouchLayout.SettingsColumnWidth, header ? 18f : FrontRoomsTouchLayout.SettingsRowHeight), false);
        r.Group = r.Rect.gameObject.AddComponent<CanvasGroup>();
        if (header)
        {
            r.Label = Txt("label", r.Rect, string.Empty, plex, 11, Muted, TextAnchor.MiddleLeft, new Vector2(FrontRoomsTouchLayout.SettingsColumnWidth, 16f));
            return r;
        }
        r.Pressed = Img("pressed", r.Rect, new Vector2(FrontRoomsTouchLayout.SettingsColumnWidth + 16f, FrontRoomsTouchLayout.SettingsRowHeight), WithAlpha(Paper, 0f));
        r.Pressed.sprite = FrontRoomsTouchSprites.Solid();
        // An open row's paper underlay (7 %), from its bar to its right end.
        r.Underlay = Img("underlay", r.Rect, Vector2.zero, WithAlpha(Paper, 0f));
        r.Underlay.sprite = FrontRoomsTouchSprites.Solid();
        r.Divider = Img("divider", r.Rect, new Vector2(FrontRoomsTouchLayout.SettingsColumnWidth, 1f), WithAlpha(Paper, .08f));
        r.Divider.sprite = FrontRoomsTouchSprites.Solid();
        r.Divider.rectTransform.anchoredPosition = new Vector2(0f, -FrontRoomsTouchLayout.SettingsRowHeight * .5f + .5f);
        r.Label = Txt("label", r.Rect, string.Empty, bayon, 17, Paper, TextAnchor.MiddleLeft, new Vector2(FrontRoomsTouchLayout.SettingsColumnWidth, 22f));
        r.ValueA = Txt("value A", r.Rect, string.Empty, bayon, 17, Paper, TextAnchor.MiddleRight, new Vector2(FrontRoomsTouchLayout.SettingsColumnWidth, 22f));
        r.ValueB = Txt("value B", r.Rect, string.Empty, bayon, 17, Paper, TextAnchor.MiddleRight, new Vector2(FrontRoomsTouchLayout.SettingsColumnWidth, 22f));
        r.ChevronLeft = Txt("chevron left", r.Rect, "‹", bayon, 17, Muted, TextAnchor.MiddleCenter, new Vector2(12f, 22f));
        r.ChevronRight = Txt("chevron right", r.Rect, "›", bayon, 17, Muted, TextAnchor.MiddleCenter, new Vector2(12f, 22f));
        // The second line hangs from the row's top, so the row grows under it and nothing in it moves.
        r.Line = Node("second line", r.Rect, Vector2.zero, false);
        r.Line.anchorMin = new Vector2(0f, 1f);
        r.Line.anchorMax = new Vector2(1f, 1f);
        r.Line.pivot = new Vector2(.5f, 1f);
        r.Line.sizeDelta = Vector2.zero;
        r.Line.anchoredPosition = Vector2.zero;
        r.Line.gameObject.SetActive(false);
        // The name at the open size, waiting beside the resting one: it grows from its left end on the first line's centre line.
        r.LabelOpen = Txt("label open", r.Rect, string.Empty, bayon, OpenLabelSize, Paper, TextAnchor.MiddleLeft, new Vector2(FrontRoomsTouchLayout.SettingsColumnWidth, 30f));
        var lt = r.LabelOpen.rectTransform;
        lt.anchorMin = lt.anchorMax = new Vector2(0f, 1f);
        lt.pivot = new Vector2(0f, .5f);
        r.LabelOpen.enabled = false;
        // Its own 4 pt bar at its left, over everything in it: it leads the open and folds with the row.
        r.Bar = Img("bar", r.Rect, new Vector2(4f, FrontRoomsTouchLayout.SettingsRowHeight), WithAlpha(Accent, 0f));
        r.Bar.sprite = FrontRoomsTouchSprites.Solid();
        return r;
    }

    void TickMenus(FrontRoomsTouchLayout L)
    {
        var state = controls.CurrentMenuState;
        var confirm = controls.RestartConfirmationOpen;
        var entered = state != lastState;
        var confirmChanged = confirm != lastConfirm;
        var now = FrontRoomsTouchMotion.Now;

        // The wash: the desktop pause wash, over the room.
        var washed = state == FrontRoomsTouchControls.MenuState.Paused || state == FrontRoomsTouchControls.MenuState.Settings || state == FrontRoomsTouchControls.MenuState.Caught;
        if (entered)
        {
            var caught = state == FrontRoomsTouchControls.MenuState.Caught;
            // From play into a card: freeze this frame under the frost (pause ↔ settings keeps the one it has).
            if (washed && !Washed(lastState)) CaptureFrost();
            washAlpha.To(washed ? 1f : 0f, Dur(washed ? (caught ? .42f : .24f) : .16f), washed ? Ease.OutCubic : Ease.InCubic);
        }
        // The frost fades in over the live frame it froze, so it reads as the room frosting over.
        frostGroup.alpha = washAlpha;
        frostFrame.enabled = frostTexture != null;
        // No frame to freeze (no camera): the opaque paper wash it replaces. Settings turns the frost into black glass:
        // the paper veil and its texture crossfade to black over the frozen room, and the card dissolves into it.
        var settingsNow = state == FrontRoomsTouchControls.MenuState.Settings && !confirm;
        if (entered || confirmChanged) settingsDark.To(settingsNow ? 1f : 0f, Dur(.24f), settingsNow ? Ease.OutCubic : Ease.InCubic);
        frostVeil.color = Color.Lerp(WithAlpha(WashColor, frostTexture != null ? FrostVeil : .98f), WithAlpha(CardColor, SettingsVeil), settingsDark);
        frostPaper.color = WithAlpha(Color.white, 1f - settingsDark);
        if (settingsCardImage != null) settingsCardImage.color = WithAlpha(CardColor, .88f * (1f - settingsDark));
        var frame = L.Frame;
        frostPaper.uvRect = new Rect(0f, 0f, frame.x / FrostTileWidth, frame.y / (FrostTileWidth * 1.5f));
        if (!washed && washAlpha.Current <= .001f && frostTexture != null) ReleaseFrost();

        var paused = state == FrontRoomsTouchControls.MenuState.Paused;
        if (entered || confirmChanged)
        {
            // Pause: title, meta, legend, chips rise into place, staggered (alpha only under Reduce Motion).
            // The sheet is the pause (Red, 2026-10-24); END RUN's question brings the old card's title, run line and chips back
            // over the frost: CANCEL · RESTART, QUIT TO TITLE under them.
            var settings = state == FrontRoomsTouchControls.MenuState.Settings;
            var asking = (paused || settings) && confirm;
            Show(pauseTitle, paused || asking, .04f, 18f);
            Show(pauseMeta, paused || asking, .07f, 12f);
            Show(legendA, paused && !confirm, .10f, 10f);
            Show(legendB, paused && !confirm, .13f, 10f);
            Show(confirmText, asking, .04f, 8f);
            ShowChip(FrontRoomsTouchControls.Button.Resume, paused && !confirm, .16f);
            ShowChip(FrontRoomsTouchControls.Button.Settings, paused && !confirm, .19f);
            ShowChip(FrontRoomsTouchControls.Button.Restart, (paused || settings) && !confirm, settings ? .1f : .22f);
            // QUIT TO TITLE sits under the row so it cannot be confused with RESTART.
            ShowChip(FrontRoomsTouchControls.Button.ReturnTitle, (paused && !confirm) || asking, asking ? .11f : .25f);
            ShowChip(FrontRoomsTouchControls.Button.RestartCancel, asking, .05f);
            ShowChip(FrontRoomsTouchControls.Button.RestartConfirm, asking, .08f);

            if (settings && entered) settingsOpenedAt = now;
            Show(settingsCard, settings && !confirm, 0f, 0f, settings ? .97f : 1f);
            ShowChip(FrontRoomsTouchControls.Button.SettingsClose, settings && !confirm, .08f);

            var caughtState = state == FrontRoomsTouchControls.MenuState.Caught;
            if (caughtState && entered) StartCaught();
            Show(caughtTitle, caughtState, .08f, -12f);
            // 80 ms after the title, with the same drop.
            Show(caughtSubtitle, caughtState, .16f, -12f);
            for (var i = 0; i < 4; i++)
            {
                Show(caughtNumbers[i], caughtState, .18f + i * .04f, 8f);
                Show(caughtLabels[i], caughtState, .2f + i * .04f, 8f);
            }
            Show(caughtRule, caughtState, .3f, 0f);
            Show(caughtLine, caughtState, .34f, 6f);
            // TRY AGAIN waits: a thumb still pressed from the chase must not restart by accident.
            ShowChip(FrontRoomsTouchControls.Button.TryAgain, caughtState, .6f);

            if (state == FrontRoomsTouchControls.MenuState.Title && entered) titleShownAt = now + 3f;
            if (state != FrontRoomsTouchControls.MenuState.Title) Show(titlePrompt, false, 0f, 0f);
            lastState = state;
            lastConfirm = confirm;
        }

        // Positions (Figma screen masters, centred on the frame).
        Place(pauseTitle, L.FromMaster(437f, 74f));
        Place(pauseMeta, L.FromMaster(437f, 114f));
        Place(legendA, L.FromMaster(437f, 161f));
        Place(legendB, L.FromMaster(437f, 197f));
        Place(confirmText, L.FromMaster(437f, 180f));
        if (paused)
        {
            pauseMetaText.text = controls.Host != null ? controls.Host.PauseMeta : string.Empty;
            UpdateLegend(pauseLegend, L.MenuWidth);
            if (L.CompactMenus)
            {
                Place(legendA, L.FromMaster(437f, 151f + (pauseLegend.RowsA - 1) * 14f));
                Place(legendB, L.FromMaster(437f, 151f + pauseLegend.RowsA * 28f + 8f + (pauseLegend.RowsB - 1) * 14f));
            }
        }
        foreach (var pair in chips)
        {
            if (controls.TryGetChipBase(pair.Key, out var center, out var size) && controls.TryGetChip(pair.Key, out var cleared, out _))
            {
                pair.Value.Base = center;
                var hidden = pair.Value.Alpha.Current < .01f && !pair.Value.Shown;
                GiveWay(pair.Value.GiveX, cleared.x - center.x, hidden);
                GiveWay(pair.Value.GiveY, cleared.y - center.y, hidden);
            }
            if (size.x > 0f) pair.Value.Rect.sizeDelta = size;
            var pressed = controls.PressedButton == pair.Key;
            pair.Value.Scale.To(pressed ? .94f : 1f, Dur(pressed ? .07f : .16f), pressed ? Ease.OutCubic : Ease.OutBackSoft);
            // A pad's or a keyboard's focus is yellow with the word in ink, as on the desktop sheet.
            var focused = controls.FocusChip == pair.Key;
            chipFills[pair.Key].color = focused ? Accent
                : DarkChip(pair.Key) ? (pressed ? Color.Lerp(CardColor, Paper, .12f) : CardColor)
                : KeylineChip(pair.Key) ? WithAlpha(Paper, pressed ? .12f : 0f)
                : (pressed ? Color.Lerp(Accent, Ink, .08f) : Accent);
            if (chipLabels.TryGetValue(pair.Key, out var label)) label.color = focused ? Ink : DarkChip(pair.Key) || KeylineChip(pair.Key) ? Paper : Ink;
        }
        // Four columns on the authored phone master; two readable rows in a compact window.
        Place(caughtTitle, L.FromMaster(437f, 64f));
        Place(caughtSubtitle, L.FromMaster(437f, 97f));
        var statColumns = L.CompactMenus ? 2 : 4;
        var statWidth = L.MenuWidth / statColumns;
        for (var i = 0; i < 4; i++)
        {
            var x = 437f - L.MenuWidth * .5f + statWidth * (i % statColumns + .5f);
            var row = i / statColumns;
            Place(caughtNumbers[i], L.FromMaster(x, 135f + row * 60f));
            Place(caughtLabels[i], L.FromMaster(x, 164f + row * 60f));
            caughtNumbers[i].Rect.sizeDelta = new Vector2(statWidth - 8f, 30f);
            caughtLabels[i].Rect.sizeDelta = new Vector2(statWidth - 8f, 20f);
            FitText(caughtLabels[i].Rect.GetComponent<Text>(), 17, 14);
        }
        Place(caughtRule, L.FromMaster(437f, L.CompactMenus ? 256f : 196f));
        Place(caughtLine, L.FromMaster(437f, L.CompactMenus ? 288f : 235f));
        caughtRule.Rect.sizeDelta = new Vector2(Mathf.Min(718f, L.MenuWidth - 16f), 1f);
        caughtLine.Rect.sizeDelta = new Vector2(Mathf.Min(700f, L.MenuWidth - 16f), L.CompactMenus ? 48f : 24f);
        caughtLineText.horizontalOverflow = L.CompactMenus ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
        foreach (var item in new[] { pauseTitle, pauseMeta, confirmText, caughtTitle, caughtSubtitle })
        {
            item.Rect.sizeDelta = new Vector2(Mathf.Min(item == pauseMeta ? 700f : 600f, L.MenuWidth), item.Rect.sizeDelta.y);
            FitText(item.Rect.GetComponent<Text>(), item == pauseTitle || item == caughtTitle ? 48 : item == pauseMeta ? 11 : 17,
                item == pauseTitle || item == caughtTitle ? 36 : item == pauseMeta ? 10 : 14);
        }
        // Up 14 pt from 320 (board option c), so row B clears the iPhone's home inset.
        Place(titlePrompt, L.FromMaster(437f, 306f));
        titlePromptText.text = TitlePromptWords();
        if (state == FrontRoomsTouchControls.MenuState.Caught) TickCaught(now);

        // Settings card.
        cardRoot.sizeDelta = L.SettingsCardSize;
        settingsCard.Base = L.SettingsCardCenter;
        TickSettings(L, now);

        TickItems();
        TickTitle(now, state, L);
    }

    void Show(Item item, bool on, float delay, float rise, float fromScale = 1f)
    {
        if (on == item.Shown) return;
        item.Shown = on;
        if (on)
        {
            item.Alpha.Restart(item.Alpha.Current, 1f, Dur(.26f), Ease.OutCubic, Reduced ? 0f : delay);
            if (!Reduced && rise != 0f) item.Rise.Restart(rise, 0f, .26f, Ease.OutCubic, delay); else item.Rise.Snap(0f);
            if (!Reduced && fromScale != 1f) item.Scale.Restart(fromScale, 1f, .22f, Ease.OutCubic, delay); else item.Scale.Snap(1f);
        }
        else
        {
            item.Alpha.Restart(item.Alpha.Current, 0f, Dur(.14f), Ease.InCubic);
            item.Rise.Snap(0f);
        }
    }

    void ShowChip(FrontRoomsTouchControls.Button button, bool on, float delay)
    {
        if (!chips.TryGetValue(button, out var item)) return;
        if (on && !item.Shown && !Reduced) item.Scale.Restart(.96f, 1f, .2f, Ease.OutBackSoft, delay);
        Show(item, on, delay, 12f);
    }

    static void Place(Item item, Vector2 position) => item.Base = position;

    void TickItems()
    {
        Apply(pauseTitle); Apply(pauseMeta); Apply(legendA); Apply(legendB); Apply(confirmText);
        Apply(caughtTitle); Apply(caughtSubtitle); Apply(caughtRule); Apply(caughtLine); Apply(titlePrompt);
        for (var i = 0; i < 4; i++) { Apply(caughtNumbers[i]); Apply(caughtLabels[i]); }
        foreach (var chip in chips.Values) Apply(chip);
        Apply(settingsCard);
    }

    static void Apply(Item item)
    {
        var a = item.Alpha.Current;
        item.Group.alpha = a;
        var active = a > .001f || item.Shown;
        if (item.Rect.gameObject.activeSelf != active) item.Rect.gameObject.SetActive(active);
        item.Rect.anchoredPosition = item.Base + new Vector2(item.GiveX.Current, item.GiveY.Current) + Vector2.down * item.Rise.Current;
        item.Rect.localScale = Vector3.one * item.Scale.Current;
    }

    void StartCaught()
    {
        caughtStart = FrontRoomsTouchMotion.Now;
        var stats = controls.Host != null ? controls.Host.CaughtStats : default;
        caughtTargets[0] = stats.Seconds;
        caughtTargets[1] = stats.Zones;
        caughtTargets[2] = stats.Keys;
        caughtTargets[3] = stats.DoorsBroken;
        caughtLineText.text = string.IsNullOrEmpty(stats.Where) ? string.Empty : FrontRoomsLanguage.F("Caught in {0}.", stats.Where);
    }

    void TickCaught(float now)
    {
        // The numbers count up as the card settles (instantly under Reduce Motion).
        var t = Reduced ? 1f : FrontRoomsTouchMotion.Evaluate(Ease.OutCubic, (now - caughtStart - .2f) / .6f);
        for (var i = 0; i < 4; i++)
        {
            var v = Mathf.RoundToInt(caughtTargets[i] * t);
            caughtNumberTexts[i].text = i == 0 ? FrontRoomsLanguage.F("{0} s", v.ToString()) : v.ToString();
        }
    }

    void TickTitle(float now, FrontRoomsTouchControls.MenuState state, FrontRoomsTouchLayout L)
    {
        if (state != FrontRoomsTouchControls.MenuState.Title) return;
        // A cold start shows the logo, then the room; the prompt and legend come 1 s after everything is loaded (Red,
        // 2026-10-24: "if the rest isn't loaded, don't show TAP TO START and the small text").
        var loaded = FrontRoomsHandheld.StartupReady && now >= FrontRoomsHandheld.StartupReadyAt + 1f;
        if (titleShownAt > 0f && now >= titleShownAt && loaded && !titlePrompt.Shown)
        {
            titlePrompt.Shown = true;
            titlePrompt.Alpha.Restart(0f, 1f, Reduced ? .2f : .6f, Ease.InOutSine);
        }
        // Static once in: the corridor already moves. If a "waiting" signal ever returns, it is a hard on/off of a
        // solid element (a block cursor, 0.6 s / 0.6 s, off under REDUCE FLASHING), never an alpha ramp on the words.
        // TAP TO START's caps are centred on the lockup's point; row A's cap top sits 16 pt under its baseline and
        // row B follows at the glyph box + 8 pt.
        titlePromptText.rectTransform.anchoredPosition = new Vector2(0f, -Caps(titlePromptText).y);
        UpdateLegend(titleLegend, L.MenuWidth);
        var rowA = -(17f * BayonCapHeightEm * .5f + 16f + LegendSize * PlexCapHeightEm * .5f);
        titleLegend.RowA.anchoredPosition = new Vector2(0f, rowA - (titleLegend.RowsA - 1) * 14f);
        titleLegend.RowB.anchoredPosition = new Vector2(0f, rowA - titleLegend.RowsA * 28f - 2f - (titleLegend.RowsB - 1) * 14f);
        // The expanded legend must clear the home indicator after a live resize.
        var bottom = titleLegend.RowB.anchoredPosition.y - (titleLegend.RowsB - 1) * 14f - 11f;
        var basePoint = titlePrompt.Base;
        basePoint.y = Mathf.Max(basePoint.y, L.SafeBottom + 8f - bottom);
        titlePrompt.Base = basePoint;
        titlePrompt.Rect.anchoredPosition = basePoint + Vector2.down * titlePrompt.Rise.Current;
    }

    // ---------------------------------------------------------------- settings

    void TickSettings(FrontRoomsTouchLayout L, float now)
    {
        var card = L.SettingsCardSize;
        var cardCenter = L.SettingsCardCenter;
        // Card-local coordinates (the card's centre is 0, 0).
        Vector2 Local(Vector2 frame) => frame - cardCenter;
        var settingsOpen = controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Settings;

        // SETTINGS and the tabs give way together, sideways, to a camera or a rounded corner (Red, 2026-10-24: the outer
        // camera at the top left pushes them right, with a motion transition), keeping the HUD's 16 pt from a curve
        // (rounded-corner option 1, research/interaction/duo/rounded_corners).
        var headingW = Mathf.Max(1f, cardHeading.preferredWidth);
        var stripLeft = cardCenter.x - card.x * .5f + 24f;
        var midY = cardCenter.y + card.y * .5f - 27f;
        var widths = tabWidths;
        var tabsWidth = 0f;
        for (var i = 0; i < tabTexts.Length; i++)
        {
            widths[i] = Mathf.Ceil(tabTexts[i].preferredWidth);
            tabsWidth += widths[i];
        }
        // A long language can't share the line with the chips: the gaps close, then the heading goes (the tabs say where you are).
        var chipsLeft = controls.TryGetChipBase(FrontRoomsTouchControls.Button.Restart, out var endRun, out var endSize) ? endRun.x - endSize.x * .5f : stripLeft + card.x;
        var room = chipsLeft - 12f - stripLeft;
        var gaps = tabTexts.Length - 1;
        var tabGap = Mathf.Clamp((room - headingW - 28f - tabsWidth) / gaps, 10f, 18f);
        var headed = headingW + 28f + tabsWidth + tabGap * gaps <= room;
        if (!headed) tabGap = Mathf.Clamp((room - tabsWidth) / gaps, 6f, 18f);
        cardHeading.enabled = headed;
        var total = (headed ? headingW + 28f : 0f) + tabsWidth + tabGap * gaps;
        var stripBase = new Rect(stripLeft, midY - 11f, total, 22f);
        var stripClear = FrontRoomsHandheld.ClearRect(stripBase, false, 12f, FrontRoomsHandheld.HudCornerGap);
        GiveWay(headingGive, stripClear.x - stripBase.x, settingsCard.Alpha.Current < .01f);
        cardHeading.rectTransform.anchoredPosition = new Vector2(-card.x * .5f + 24f + 100f + headingGive, card.y * .5f - 27f);
        var x = stripLeft + (headed ? headingW + 28f : 0f) + headingGive;
        for (var i = 0; i < tabTexts.Length; i++)
        {
            var selected = FrontRoomsSettingsMenu.Tabs[i] == controls.SettingsTab;
            tabTexts[i].rectTransform.sizeDelta = new Vector2(widths[i] + 4f, 22f);
            tabTexts[i].rectTransform.anchoredPosition = Local(new Vector2(x + widths[i] * .5f, midY));
            tabTexts[i].color = WithAlpha(Paper, selected ? 1f : .6f);
            tabRules[i].rectTransform.sizeDelta = new Vector2(widths[i], 2f);
            tabRules[i].rectTransform.anchoredPosition = Local(new Vector2(x + widths[i] * .5f, midY - 15f));
            tabRules[i].color = WithAlpha(Paper, selected ? 1f : 0f);
            controls.SettingsTabRects[i] = settingsOpen ? new Rect(x, midY - 11f, widths[i], 22f) : default;
            x += widths[i] + tabGap;
        }

        var slots = controls.SettingsSlots;
        var viewport = controls.SettingsScrollViewport;
        // The list's mask reaches 16 pt left of it for an open row's bar and underlay, and 8 pt right for a pressed row.
        var mask = Rect.MinMaxRect(viewport.xMin - 16f, viewport.yMin, viewport.xMax + 8f, viewport.yMax);
        cardViewportRight.anchoredPosition = Local(mask.center);
        cardViewportRight.sizeDelta = mask.size;
        cardViewportLeft.sizeDelta = Vector2.zero;

        previewWanted = false;
        if (!settingsOpen)
        {
            controls.SettingsPreviewRect = default;
            // Closing: the rows fade with the card; they are cleared once it is gone.
            if (settingsCard.Alpha.Current <= .001f && rows.Count > 0)
            {
                foreach (var dead in rows) if (dead != null) Destroy(dead.Rect.gameObject);
                rows.Clear();
            }
            return;
        }
        var m = FrontRoomsSettingsMenu.Touch;
        var motionNow = FrontRoomsTouchMotion.Now;
        SampleFrameTime();
        while (rows.Count < slots.Count) rows.Add(null);
        for (var i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            var r = rows[i];
            if (r == null || r.Header != s.Header)
            {
                if (r != null) Destroy(r.Rect.gameObject);
                r = MakeRow(s.Header);
                rows[i] = r;
            }
            if (r.Rect.parent != cardViewportRight) r.Rect.SetParent(cardViewportRight, false);
            var pos = s.Rect.center - mask.center + new Vector2(0f, controls.SettingsScroll);

            // Each row gives way where it is now, scrolled (Red, 2026-10-24): it narrows from the side that meets a camera
            // or a rounded corner, with a motion transition, and widens back once it has passed. A new row starts there.
            var onScreen = s.Rect;
            onScreen.y += controls.SettingsScroll;
            var clear = FrontRoomsHandheld.ClearRect(onScreen, true);
            var fresh = r.Id != s.Id;
            GiveWay(r.GiveLeft, clear.xMin - onScreen.xMin, fresh);
            GiveWay(r.GiveRight, onScreen.xMax - clear.xMax, fresh);
            pos.x += (r.GiveLeft.Current - r.GiveRight.Current) * .5f;

            if (r.Id != s.Id)
            {
                r.Id = s.Id;
                r.ShownValue = null;
                // Rows rise in, 24 ms apart, when the card opens or the tab changes.
                var delay = Mathf.Max(0f, (fresh && now - settingsOpenedAt < .5f ? settingsOpenedAt + .06f : now) + i * .024f - now);
                r.Alpha.Restart(0f, 1f, Dur(.2f), Ease.OutCubic, Reduced ? 0f : delay);
                if (!Reduced) r.Rise.Restart(8f, 0f, .2f, Ease.OutCubic, delay); else r.Rise.Snap(0f);
            }
            r.Rect.anchoredPosition = pos + Vector2.down * r.Rise.Current;
            r.Group.alpha = r.Alpha.Current;
            var width = Mathf.Max(44f, s.Rect.width - r.GiveLeft.Current - r.GiveRight.Current);
            var h = s.Rect.height;
            r.Rect.sizeDelta = new Vector2(width, h);
            if (s.Header)
            {
                r.Label.text = s.Label;
                r.Label.rectTransform.anchoredPosition = new Vector2(0f, -4f);
                r.Label.rectTransform.sizeDelta = new Vector2(width, 16f);
                continue;
            }
            var row = s.Row;
            // The first line holds still at the row's top while the row opens under it (SETTINGS_MOTION.md).
            var firstY = (h - s.Rest) * .5f;
            r.Label.text = row != null && row.Kind == FrontRoomsSettingsMenu.Kind.Language && L.SettingsSingleColumn ? string.Empty : s.Label;
            r.Pressed.rectTransform.sizeDelta = new Vector2(width + 16f, h);
            r.Divider.rectTransform.sizeDelta = new Vector2(width, 1f);
            r.Divider.rectTransform.anchoredPosition = new Vector2(0f, -h * .5f + .5f);
            r.ValueA.text = string.Empty;
            r.ValueB.text = string.Empty;
            r.ChevronLeft.gameObject.SetActive(false);
            r.ChevronRight.gameObject.SetActive(false);

            // The control against the row's right edge; the label owns the rest, so a narrow view never overprints it.
            var controlW = row != null ? FrontRoomsSettingsMenu.ControlWidth(row, m) : 0f;
            EnsureControl(r, row);
            if (row != null) LayoutControl(r, row, width, m);
            if (r.Control != null) r.Control.anchoredPosition = new Vector2(0f, firstY);
            var labelWidth = Mathf.Max(1f, width - controlW - 22f);
            r.Label.rectTransform.sizeDelta = new Vector2(labelWidth, 22f);
            r.Label.rectTransform.anchoredPosition = new Vector2((labelWidth - width) * .5f, firstY);
            FitText(r.Label, 17, 13);
            r.Label.color = Paper;

            var pressedNow = controls.PressedSettingsRow == s.Id;
            r.Press.To(pressedNow ? 1f : 0f, Dur(pressedNow ? .06f : .2f), Ease.OutCubic);
            r.Pressed.color = WithAlpha(Paper, .07f * r.Press);
            // Its own bar, as tall as the row: it leads the open and folds with it, and a row in a pad's or a keyboard's focus keeps
            // it at rest (yellow = focus and choice, no bar travels). Under it all, a paper underlay at 7 % while it is open.
            var focus = controls.ExternalInput && controls.FocusChip == FrontRoomsTouchControls.Button.None && controls.ActiveSettingsRow == s.Id;
            r.RuleAlpha.To(focus ? 1f : 0f, Dur(.12f), Ease.OutCubic);
            r.Bar.color = WithAlpha(Accent, Mathf.Max(r.RuleAlpha.Current, Mathf.Min(1f, s.Open * 4f)));
            r.Bar.rectTransform.sizeDelta = new Vector2(4f, h);
            r.Bar.rectTransform.anchoredPosition = new Vector2(-width * .5f - 14f, 0f);
            r.Underlay.color = WithAlpha(Paper, .07f * Mathf.Min(1f, s.Open * 1.5f));
            r.Underlay.rectTransform.sizeDelta = new Vector2(width + 16f, h);
            r.Underlay.rectTransform.anchoredPosition = new Vector2(-8f, 0f);
            if (FrontRoomsTouchControls.Opens(row) && (s.Open > 0f || s.IsOpen)) AnimateOpen(r, s, row, width, labelWidth, motionNow);
            else RestClosed(r);
        }
        for (var i = slots.Count; i < rows.Count; i++)
        {
            if (rows[i] != null) Destroy(rows[i].Rect.gameObject);
        }
        if (rows.Count > slots.Count) rows.RemoveRange(slots.Count, rows.Count - slots.Count);

        TickLanguageStrip(L, card, cardCenter);
        TickDetailColumn(L, Local);

        // The list's scroll: a thin bar while it moves. The rows run on under the bottom edge (Red, 2026-10-24: no fade
        // there, full bleed), so a row cut by the edge is what says the list goes on.
        var max = controls.SettingsScrollMax;
        var scroll = controls.SettingsScroll;
        if (Mathf.Abs(scroll - lastScroll) > .1f) { scrollBarAlpha.Restart(scrollBarAlpha.Current, 1f, .08f, Ease.OutCubic); }
        else if (scrollBarAlpha.Done && scrollBarAlpha.Target > .5f) scrollBarAlpha.To(0f, .4f, Ease.InCubic, .6f);
        lastScroll = scroll;
        if (max > .5f)
        {
            var shown = viewport.height - L.SettingsBottomInset;
            var content = shown + max;
            var barH = Mathf.Max(24f, shown * shown / content);
            var t = scroll / max;
            var travel = viewport.height - L.SettingsBottomInset;
            var y = viewport.yMax - barH * .5f - t * (travel - barH);
            scrollBar.sizeDelta = new Vector2(2f, barH);
            // The bar steps aside for a camera or a corner too (it only shows while the list moves, so it does not ease).
            var bar = FrontRoomsHandheld.ClearRect(new Rect(viewport.xMax + 9f, y - barH * .5f, 2f, barH));
            scrollBar.anchoredPosition = Local(bar.center);
            scrollBarImage.color = WithAlpha(Muted, .6f * scrollBarAlpha);
        }
        else scrollBarImage.color = WithAlpha(Muted, 0f);
    }

    readonly float[] tabWidths = new float[4];

    static FrontRoomsLanguageStrip.Metrics LanguageMetrics => FrontRoomsTouchLayout.LanguageMetrics;

    /// <summary>Where the row's word ends. A single-column sheet gives the word's room to the strip, so the charms keep their
    /// spec gaps: the key at the strip's head says what the row is, as it does for a player who can't read the word. Touch
    /// picks by slide and lift (FrontRoomsTouchControls.Move), so charms under 44 pt apart stay safe (2026-10-31).</summary>
    float LanguageLabelEnd(string label) =>
        controls.Layout.SettingsSingleColumn ? -FrontRoomsLanguageStrip.MarkGap
            : FrontRoomsLanguageStrip.LabelWidth(FrontRoomsSettingsMenu.TextWidth(label, FrontRoomsSettingsMenu.Touch.Label, false), FrontRoomsSettingsMenu.Touch.Label);

    /// <summary>The LANGUAGE strip over its row: from past the row's mark to the card's right edge, across the explanation
    /// column, which steps down for it (TickDetailColumn).</summary>
    void TickLanguageStrip(FrontRoomsTouchLayout L, Vector2 card, Vector2 cardCenter)
    {
        controls.LanguageStripRect = default;
        var showing = false;
        var row = default(FrontRoomsTouchControls.SettingsSlot);
        foreach (var s in controls.SettingsSlots)
            if (s.Row != null && s.Row.Kind == FrontRoomsSettingsMenu.Kind.Language) { row = s; showing = true; }
        if (!showing || controls.SettingsPeek) { languageStrip.SetActive(false); return; }
        var m = LanguageMetrics;
        var list = L.SettingsListRect;
        var cardLeft = cardCenter.x - card.x * .5f;
        var left = row.Rect.x + FrontRoomsLanguageStrip.StartAfter(m, LanguageLabelEnd(row.Label));
        var top = row.Rect.yMax + controls.SettingsScroll;
        var rect = new Rect(left, top - m.Row, list.xMax - left, m.Row);
        rect = FrontRoomsHandheld.ClearRect(rect, true);
        controls.LanguageStripRect = rect;
        languageStrip.Layout(m, rect.width);
        languageStrip.PlaceAt(rect.x - cardLeft, cardCenter.y + card.y * .5f - rect.yMax);
        languageStrip.SetActive(true);
        languageStrip.DrawRule(list.xMax - rect.x, rect.xMax - list.xMax);
        languageStrip.Pressed = controls.PressedButton == FrontRoomsTouchControls.Button.LanguageRow ? controls.PressedSettingsRow : -1;
        if (controls.ActiveSettingsRow != row.Id) languageStrip.Reset();
        languageStrip.Animate();
    }

    /// <summary>The picture column (a tablet's sheet; SETTINGS_MOTION.md: the right column keeps only pictures): on GRAPHICS the
    /// live game at the current settings, the frame rate in its corner, a tap on it the game full screen. Other tabs leave it empty.</summary>
    void TickDetailColumn(FrontRoomsTouchLayout L, System.Func<Vector2, Vector2> local)
    {
        var rect = L.SettingsDetailRect;
        var graphics = controls.SettingsTab == FrontRoomsSettingsMenu.Tab.Graphics;
        detailRoot.gameObject.SetActive(rect.width > 1f && graphics);
        controls.SettingsPreviewRect = default;
        if (rect.width <= 1f || !graphics || controls.SettingsPeek) return;
        var w = rect.width;
        var top = rect.yMax - 8f;
        var frame = L.Frame;
        var ph = Mathf.Round(w * frame.y / Mathf.Max(1f, frame.x));
        // The column steps aside as a whole for a camera or a corner (16 pt from a curve).
        var block = new Rect(rect.x, top - ph - 36f, w, ph + 36f);
        var left = rect.x + FrontRoomsHandheld.ClearRect(block, false, 12f, FrontRoomsHandheld.HudCornerGap).x - block.x;
        var pr = new Rect(left, top - ph, w, ph);
        TickPreviewFrame(pr, local);
        preview.rectTransform.sizeDelta = pr.size;
        preview.rectTransform.anchoredPosition = local(pr.center);
        previewKeyline.rectTransform.sizeDelta = pr.size + Vector2.one * 2f;
        previewKeyline.rectTransform.anchoredPosition = local(pr.center);
        controls.SettingsPreviewRect = pr;
        previewWanted = settingsCard.Alpha.Current > .5f;
        previewCaption.rectTransform.sizeDelta = new Vector2(w, 14f);
        previewCaption.rectTransform.anchoredPosition = local(new Vector2(left + w * .5f, top - ph - 6f - 7f));
    }

    /// <summary>The frame rate in the picture's bottom-right corner, 8 pt in: the word and the reading over the dashes.</summary>
    void TickPreviewFrame(Rect picture, System.Func<Vector2, Vector2> local)
    {
        const float inset = 8f, dash = 14f, pitch = 17f, gap = 12f;
        var fps = FrontRoomsQuality.MeasuredFps;
        var lit = Mathf.Clamp(Mathf.RoundToInt(10f * fps / Mathf.Max(1, FrontRoomsQuality.TargetFps)), 0, 10);
        var right = picture.xMax - inset;
        var dashY = picture.y + inset + 2f;
        for (var i = 0; i < previewDashes.Length; i++)
        {
            previewDashes[i].rectTransform.anchoredPosition = local(new Vector2(right - (previewDashes.Length - i) * pitch + (pitch - dash) + dash * .5f, dashY));
            previewDashes[i].color = WithAlpha(Paper, i < lit ? 1f : .2f);
        }
        // Its numbers in fixed columns, so the corner holds still as they change.
        previewFrameLine.text = Mathf.RoundToInt(fps).ToString().PadLeft(3) + " FPS  ·  " + (1000f / Mathf.Max(1f, fps)).ToString("0.0").PadLeft(5) + " MS";
        var lineW = FrontRoomsSettingsMenu.TextWidth("000 FPS  ·  000.0 MS", 11f, true);
        var lineY = dashY + 4f + 9f;
        previewFrameLine.rectTransform.sizeDelta = new Vector2(picture.width - 2f * inset, 14f);
        previewFrameLine.rectTransform.anchoredPosition = local(new Vector2(right - (picture.width - 2f * inset) * .5f, lineY));
        // A picture too small for the word keeps the reading.
        var wordW = picture.width - 2f * inset - lineW - gap;
        previewFrameWord.enabled = previewFrameWord.preferredWidth <= wordW;
        previewFrameWord.rectTransform.sizeDelta = new Vector2(Mathf.Max(1f, wordW), 14f);
        previewFrameWord.rectTransform.anchoredPosition = local(new Vector2(picture.xMin + inset + wordW * .5f, lineY));
    }

    // ------------------------------------------------------------ a row opens in place (SETTINGS_MOTION.md, E4 touch)

    // An open row's second line, below its first line (touch pt): the sentence's block centred 19 below it in a 2x row (from 4
    // below it when taller), the notes at NoteDY, the default's rule at RuleDY, the default (and a picture row's TAP HERE line
    // beside it) as far above the row's bottom as DefaultDY is below the first line's bottom in a 2x row; FRAME RATE NOW's trace
    // between 8 below the first line and 12 above the 2x row's bottom.
    const float SentenceDY = 19f, TraceTop = 8f, TraceBottom = 12f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    static readonly HashSet<string> warnedNotes = new HashSet<string>();
#endif

    /// <summary>A picture row on a sheet without the picture (the phone): its second line shows the game full screen.</summary>
    bool TapLine(FrontRoomsSettingsMenu.Row row) => row != null && row.Visual && !controls.Layout.SettingsPictures;

    static string TapWords => FrontRoomsLanguage.T("TAP HERE TO SEE IT FULL SCREEN");

    // The second line's right end, DEFAULT  BALANCED; QUALITY names this device, as the explanation column did.
    static string DefaultLineOf(FrontRoomsSettingsMenu.Row row) => row.Key == FrontRoomsSettingsMenu.Key.Quality && row.DefaultText.Length > 0
        ? FrontRoomsLanguage.F("DEFAULT  {0}", row.DefaultText + " " + FrontRoomsLanguage.T("ON THIS DEVICE")) : row.DefaultLine;

    // The centre of the note under option or stop i, from the row's left end.
    static float NoteCentre(FrontRoomsSettingsMenu.Row row, int i, float w, FrontRoomsSettingsMenu.Metrics m)
    {
        if (row.Kind == FrontRoomsSettingsMenu.Kind.Slider)
        {
            FrontRoomsSettingsMenu.TrackSpan(m, out var low, out _);
            return w - low + Mathf.Round(m.Track * FrontRoomsSettingsMenu.StopPosition(row, i));
        }
        if (row.Options == null || i >= row.Options.Length) return w;
        var x = w - FrontRoomsSettingsMenu.ControlWidth(row, m);
        for (var k = 0; k < i; k++) x += FrontRoomsSettingsMenu.CellWidth(row, k, m);
        return x + FrontRoomsSettingsMenu.CellWidth(row, i, m) * .5f;
    }

    /// <summary>The room left of a row's control, its notes and its default, where its sentence wraps: 28 pt clear of them, at
    /// least SentenceMin. FRAME RATE NOW's sentence takes the left half; its trace is the right.</summary>
    float SentenceRoom(FrontRoomsSettingsMenu.Row row, float w)
    {
        var m = FrontRoomsSettingsMenu.Touch;
        if (row.Kind == FrontRoomsSettingsMenu.Kind.Meter) return Mathf.Max(SentenceMin, Mathf.Round(w * .48f));
        var left = w - FrontRoomsSettingsMenu.ControlWidth(row, m);
        if (row.Kind == FrontRoomsSettingsMenu.Kind.Keys) left = w - FrontRoomsSettingsMenu.TextWidth(row.ValueText, m.Label, true);
        var notes = row.Notes;
        if (notes != null && row.Kind == FrontRoomsSettingsMenu.Kind.Switch)
            foreach (var n in notes) left = Mathf.Min(left, w - FrontRoomsSettingsMenu.TextWidth(n, NoteSize, true));
        else if (notes != null && (row.Kind == FrontRoomsSettingsMenu.Kind.Segments || row.Kind == FrontRoomsSettingsMenu.Kind.Slider))
            for (var i = 0; i < notes.Length; i++)
                if (!string.IsNullOrEmpty(notes[i])) left = Mathf.Min(left, NoteCentre(row, i, w, m) - FrontRoomsSettingsMenu.TextWidth(notes[i], NoteSize, true) * .5f);
        var line = DefaultLineOf(row);
        if (line.Length > 0) left = Mathf.Min(left, w - FrontRoomsSettingsMenu.TextWidth(line, NoteSize, true));
        return Mathf.Max(SentenceMin, Mathf.Floor(left - 28f));
    }

    // The sentence's height at a width (Source Serif 14, never cut), measured on the probe once per words and width.
    float SentenceBlock(FrontRoomsSettingsMenu.Row row, float room)
    {
        var words = row.Explain ?? string.Empty;
        if (words.Length == 0 || sentenceProbe == null) return 0f;
        var key = words + "\n" + room.ToString("0");
        if (openHeightTexts.TryGetValue(row, out var seen) && seen == key && openHeights.TryGetValue(row, out var known)) return known.x;
        if (openHeights.Count > 256) { openHeights.Clear(); openHeightTexts.Clear(); }
        sentenceProbe.text = words;
        sentenceProbe.rectTransform.sizeDelta = new Vector2(room, 40f);
        var block = Mathf.Ceil(sentenceProbe.preferredHeight);
        openHeightTexts[row] = key;
        openHeights[row] = new Vector4(block, room, 0f, 0f);
        return block;
    }

    // The second line measured at a row width: the sentence's room and height, whether a picture row's TAP HERE line shares the
    // default's line or, too long for it, takes its own under the sentence; returns the second line's height.
    float SecondLineHeight(FrontRoomsSettingsMenu.Row row, float w, float rest, out float room, out float block, out bool stacked)
    {
        room = SentenceRoom(row, w);
        block = SentenceBlock(row, room);
        stacked = false;
        if (!TapLine(row)) return block <= rest + .5f ? rest : block + 13f;
        stacked = FrontRoomsSettingsMenu.TextWidth(TapWords, NoteSize, true) + 24f + FrontRoomsSettingsMenu.TextWidth(DefaultLineOf(row), NoteSize, true) > w;
        return Mathf.Max(rest, Mathf.Ceil(4f + block + 4f + (stacked ? TapH + 4f : 0f) + 7f + (rest - DefaultDY)));
    }

    /// <summary>FrontRoomsTouchControls.SettingsOpenHeight: a row's open height at a list width (twice its first line, or taller
    /// for a long sentence or a TAP HERE line).</summary>
    float OpenHeight(FrontRoomsSettingsMenu.Row row, float listWidth)
    {
        var rest = FrontRoomsTouchLayout.SettingsRowHeightOf(row);
        if (row == null || sentenceProbe == null) return 2f * rest;
        return rest + SecondLineHeight(row, listWidth, rest, out _, out _, out _);
    }

    // What the second line was built for: the row, its words, its choices, the width and the TAP HERE line.
    string LineSignature(FrontRoomsSettingsMenu.Row row, float w) =>
        row.Key + "|" + row.Kind + "|" + row.Label + "|" + row.Explain + "|" + (row.Notes != null ? string.Join("|", row.Notes) : "-") + "|"
        + (row.Options != null ? string.Join("|", row.Options) : "-") + "|" + row.DefaultIndex + "|" + row.Compact + "|" + Mathf.Round(w) + "|" + TapLine(row);

    /// <summary>The open row's second line (SETTINGS_MOTION.md, "What the second line holds"), built hidden under its first line
    /// when the row, its words or its width change; AnimateOpen fades it. Left: what the row does (Source Serif 14 at 88 %, two
    /// lines in the 2x row; a longer sentence makes the row taller, never cut). Under the control: what each choice means (Plex
    /// Mono 11, the current one at 95 %, the rest at 50 %, a 12 pt rule under the default's; a switch says what its current state
    /// does). Right: the default (45 %). A picture row on the phone: TAP HERE TO SEE IT FULL SCREEN beside the default, or under
    /// the sentence when both do not fit. FRAME RATE NOW on the phone: the last 4.8 s of frame times against its target.</summary>
    void BuildSecondLine(RowView v, FrontRoomsSettingsMenu.Row row, float w, float rest)
    {
        foreach (Transform child in v.Line) Destroy(child.gameObject);
        v.Notes.Clear();
        v.NoteOf.Clear();
        v.Trace.Clear();
        v.Sentence = v.Default = v.Tap = v.TargetWord = null;
        v.NoteRule = v.Target = null;
        v.RuleSlot = -1;
        v.LineRow = row;
        v.LineExplain = row.Explain;
        v.LineNotes = row.Notes;
        v.LineWidth = w;
        v.LineTap = TapLine(row);
        v.LineTarget = -1;
        v.LineSig = LineSignature(row, w);
        var m = FrontRoomsSettingsMenu.Touch;
        var h2 = SecondLineHeight(row, w, rest, out var room, out var block, out var stacked);
        var half = w * .5f;
        // The sentence: centred in a 2x row; from the second line's top when it is taller or a TAP HERE line follows it.
        v.Sentence = Txt("sentence", v.Line, string.Empty, serif, SentenceSize, WithAlpha(Paper, 0f), TextAnchor.MiddleLeft, new Vector2(room, rest));
        v.Sentence.text = row.Explain ?? string.Empty;
        v.Sentence.horizontalOverflow = HorizontalWrapMode.Wrap;
        v.Sentence.lineSpacing = SentencePitch;
        if (block <= rest + .5f && !v.LineTap) v.Sentence.rectTransform.anchoredPosition = new Vector2(-half + room * .5f, -(rest + SentenceDY));
        else
        {
            v.Sentence.alignment = TextAnchor.UpperLeft;
            v.Sentence.rectTransform.sizeDelta = new Vector2(room, block + 2f);
            v.Sentence.rectTransform.anchoredPosition = new Vector2(-half + room * .5f, -(rest + 4f + (block + 2f) * .5f));
        }
        var notes = row.Notes;
        if (notes != null && row.Kind == FrontRoomsSettingsMenu.Kind.Switch) AddNote(v, -1, half - 200f, 400f, TextAnchor.MiddleRight, rest);
        else if (notes != null && (row.Kind == FrontRoomsSettingsMenu.Kind.Segments || row.Kind == FrontRoomsSettingsMenu.Kind.Slider))
        {
            var count = row.Kind == FrontRoomsSettingsMenu.Kind.Slider ? (row.Stops != null ? row.Stops.Length : 0) : (row.Options != null ? row.Options.Length : 0);
            for (var i = 0; i < count && i < notes.Length; i++)
            {
                if (string.IsNullOrEmpty(notes[i])) continue;
                var c = Mathf.Round(NoteCentre(row, i, w, m)) - half;
                AddNote(v, i, c, 160f, TextAnchor.MiddleCenter, rest).text = notes[i];
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                // A note wider than its choice goes back to the strings, never cut with "…" (SETTINGS_MOTION.md).
                if (row.Kind == FrontRoomsSettingsMenu.Kind.Segments && FrontRoomsSettingsMenu.TextWidth(notes[i], NoteSize, true) > FrontRoomsSettingsMenu.CellWidth(row, i, m) + 1f && warnedNotes.Add(notes[i]))
                    Debug.LogWarning("[settings] touch note wider than its choice (" + row.Label + "): " + notes[i]);
#endif
                if (i != row.DefaultIndex) continue;
                v.NoteRule = Img("default rule", v.Line, new Vector2(12f, 1f), WithAlpha(Paper, 0f));
                v.NoteRule.sprite = FrontRoomsTouchSprites.Solid();
                v.NoteRule.rectTransform.anchoredPosition = new Vector2(c, -(rest + RuleDY));
                v.RuleSlot = v.Notes.Count - 1;
            }
        }
        // The default and a TAP HERE line keep their distance from the row's bottom, as a taller row grows.
        var bottomY = -(rest + h2 - (rest - DefaultDY));
        var line = DefaultLineOf(row);
        if (line.Length > 0)
        {
            v.Default = Txt("default", v.Line, string.Empty, plex, NoteSize, WithAlpha(Paper, 0f), TextAnchor.MiddleRight, new Vector2(600f, 14f));
            v.Default.text = line;
            v.Default.rectTransform.anchoredPosition = new Vector2(half - 300f, bottomY);
        }
        if (v.LineTap)
        {
            v.Tap = Txt("tap here", v.Line, "TAP HERE TO SEE IT FULL SCREEN", plex, NoteSize, WithAlpha(Paper, 0f), TextAnchor.MiddleLeft, new Vector2(w, 14f));
            v.Tap.rectTransform.anchoredPosition = new Vector2(0f, stacked ? -(rest + 4f + block + 4f + TapH * .5f) : bottomY);
        }
        if (row.Kind == FrontRoomsSettingsMenu.Kind.Meter) BuildTrace(v, w);
    }

    // A note of the second line, its anchor's centre at x, hidden until the row opens.
    Text AddNote(RowView v, int of, float x, float width, TextAnchor anchor, float rest)
    {
        var t = Txt("note", v.Line, string.Empty, plex, NoteSize, WithAlpha(Paper, 0f), anchor, new Vector2(width, 14f));
        t.rectTransform.anchoredPosition = new Vector2(x, -(rest + NoteDY));
        v.Notes.Add(t);
        v.NoteOf.Add(of);
        return t;
    }

    // FRAME RATE NOW's live line (SETTINGS_MOTION.md, METER): 47 strokes between the last 48 frame times across the right 44 % of
    // the row, the target as a yellow rule with its milliseconds beside it.
    void BuildTrace(RowView v, float w)
    {
        v.Target = Img("target", v.Line, new Vector2(Mathf.Round(w * .44f), 1f), WithAlpha(Accent, 0f));
        v.Target.sprite = FrontRoomsTouchSprites.Solid();
        v.TargetWord = Txt("target ms", v.Line, string.Empty, plex, NoteSize, WithAlpha(Paper, 0f), TextAnchor.MiddleRight, new Vector2(60f, 14f));
        for (var i = 0; i < frameTimes.Length - 1; i++)
        {
            var stroke = Img("trace", v.Line, new Vector2(1f, 1.5f), WithAlpha(Paper, 0f));
            stroke.sprite = FrontRoomsTouchSprites.Solid();
            stroke.rectTransform.pivot = new Vector2(0f, .5f);
            v.Trace.Add(stroke);
        }
    }

    // The trace, each frame while the row shows: the newest frame time at the right end; a sheet opened a moment ago draws what it
    // has. The scale runs to 40 ms, or to twice the target's frame time at a low target.
    void TickTrace(RowView v, float w, float rest, float alpha)
    {
        var half = w * .5f;
        var gx1 = half;
        var gx0 = half - Mathf.Round(w * .44f);
        var top = -(rest + TraceTop);
        var bottom = -(rest + rest - TraceBottom);
        var fps = Mathf.Max(1, FrontRoomsQuality.TargetFps);
        var target = 1000f / fps;
        var scale = Mathf.Max(40f, 2f * target);
        var ty = Mathf.Round(Mathf.Lerp(bottom, top, target / scale)) + .5f;
        v.Target.rectTransform.anchoredPosition = new Vector2((gx0 + gx1) * .5f, ty);
        v.Target.color = WithAlpha(Accent, .9f * alpha);
        if (v.LineTarget != fps) { v.LineTarget = fps; v.TargetWord.text = target.ToString("0.0"); }
        v.TargetWord.rectTransform.anchoredPosition = new Vector2(gx0 - 8f - 30f, ty);
        v.TargetWord.color = WithAlpha(Paper, .6f * alpha);
        var n = frameTimes.Length;
        var step = (gx1 - gx0) / (n - 1);
        for (var i = 0; i < v.Trace.Count; i++)
        {
            var stroke = v.Trace[i];
            var a = Sample(n - 1 - i);
            var b = Sample(n - 2 - i);
            if (a < 0f || b < 0f || alpha <= .001f) { stroke.enabled = false; continue; }
            stroke.enabled = true;
            var p = new Vector2(gx0 + i * step, Mathf.Lerp(bottom, top, Mathf.Clamp01(a / scale)));
            var q = new Vector2(gx0 + (i + 1) * step, Mathf.Lerp(bottom, top, Mathf.Clamp01(b / scale)));
            var d = q - p;
            stroke.rectTransform.anchoredPosition = p;
            stroke.rectTransform.sizeDelta = new Vector2(d.magnitude + .5f, 1.5f);
            stroke.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            stroke.color = WithAlpha(Paper, alpha);
        }
    }

    // The frame time <paramref name="back"/> samples before the newest, in ms; -1 before the sheet has it.
    float Sample(int back)
    {
        if (back < 0 || back >= frameTimeCount) return -1f;
        var i = frameTimeHead - 1 - back;
        if (i < 0) i += frameTimes.Length;
        return frameTimes[i];
    }

    // FRAME RATE NOW's samples: the mean frame time of every 0.1 s while the sheet is open, the last 48 (4.8 s); a gap (the sheet
    // was shut) starts again.
    void SampleFrameTime()
    {
        var now = Time.unscaledTime;
        if (now - frameTimeAt > 1f)
        {
            frameTimeAt = now;
            frameTimeCount = frameTimeFrames = 0;
            frameTimeSum = 0f;
            return;
        }
        frameTimeSum += Time.unscaledDeltaTime;
        frameTimeFrames++;
        if (now - frameTimeAt < .1f) return;
        frameTimeAt = now;
        frameTimes[frameTimeHead] = 1000f * frameTimeSum / Mathf.Max(1, frameTimeFrames);
        frameTimeHead = (frameTimeHead + 1) % frameTimes.Length;
        frameTimeCount = Mathf.Min(frameTimeCount + 1, frameTimes.Length);
        frameTimeSum = 0f;
        frameTimeFrames = 0;
    }

    // A second-line element's alpha, 0..1 (FrontRoomsDesktopSettingsView.Reveal): slot 0 is the sentence, then the notes left to
    // right, then the default; each fades in 0.18 s from 0.09 s, 0.035 s after the one before, where it stays; a closing row's
    // fade out together in 0.09 s. Reduced motion: all fade in 0.12 s.
    static float Reveal(FrontRoomsTouchControls.SettingsSlot s, int slot, float now)
    {
        if (FrontRoomsHandheld.ReduceMotion) return s.IsOpen ? Mathf.Clamp01((now - s.OpenAt) / FrontRoomsTouchControls.ReducedFadeS) : 0f;
        var shown = FrontRoomsTouchControls.OutQuart(((s.IsOpen ? now : s.CloseAt) - s.OpenAt - FrontRoomsTouchControls.RevealAt - slot * FrontRoomsTouchControls.Stagger) / FrontRoomsTouchControls.RevealS);
        return s.IsOpen ? shown : shown * (1f - Mathf.Clamp01((now - s.CloseAt) / FrontRoomsTouchControls.CloseFadeS));
    }

    static void FadePaper(Text t, float alpha)
    {
        if (t != null) t.color = WithAlpha(Paper, alpha);
    }

    /// <summary>An open (or moving) row: its name 1.4x from its left end on the first line's centre line (the resting Label while
    /// closed, so both rests are drawn at their own size; never into the control), its second line's alphas, FRAME RATE NOW's
    /// trace. The bar and the underlay are TickSettings'.</summary>
    void AnimateOpen(RowView r, FrontRoomsTouchControls.SettingsSlot s, FrontRoomsSettingsMenu.Row row, float width, float labelWidth, float now)
    {
        if (r.Line == null) return;
        if (r.LineSig != LineSignature(row, width)) BuildSecondLine(r, row, width, s.Rest);
        if (!r.Line.gameObject.activeSelf) r.Line.gameObject.SetActive(true);
        var moving = s.Open > .0005f;
        r.Label.enabled = !moving;
        r.LabelOpen.enabled = moving;
        if (moving)
        {
            if (r.LabelOpen.text != s.Label) r.LabelOpen.text = s.Label;
            var gen = r.Label.cachedTextGenerator;
            var restSize = r.Label.resizeTextForBestFit && gen != null && gen.fontSizeUsedForBestFit > 0 ? gen.fontSizeUsedForBestFit : r.Label.fontSize;
            var openSize = (float)Mathf.Max(1, r.LabelOpen.fontSize);
            var k = restSize * (1f + FrontRoomsTouchControls.TitleGrow * FrontRoomsTouchControls.OutQuart(s.Open)) / openSize;
            var full = r.LabelOpen.preferredWidth;
            if (full > 1f) k = Mathf.Min(k, Mathf.Max(restSize / openSize, labelWidth / full));
            var lt = r.LabelOpen.rectTransform;
            lt.sizeDelta = new Vector2(Mathf.Max(labelWidth, full + 4f), 30f);
            lt.anchoredPosition = new Vector2(0f, -s.Rest * .5f);
            lt.localScale = new Vector3(k, k, 1f);
            r.LabelOpen.color = Paper;
        }
        FadePaper(r.Sentence, .88f * Reveal(s, 0, now));
        // A switch's note says what its current state does.
        if (row.Kind == FrontRoomsSettingsMenu.Kind.Switch && r.Notes.Count > 0)
        {
            var said = row.Notes != null && row.Index >= 0 && row.Index < row.Notes.Length ? row.Notes[row.Index] : string.Empty;
            if (r.Notes[0].text != said) r.Notes[0].text = said;
        }
        for (var n = 0; n < r.Notes.Count; n++)
            FadePaper(r.Notes[n], (r.NoteOf[n] < 0 ? .8f : r.NoteOf[n] == row.Index ? .95f : .5f) * Reveal(s, 1 + n, now));
        if (r.NoteRule != null) r.NoteRule.color = WithAlpha(Paper, .6f * Reveal(s, 1 + r.RuleSlot, now));
        var last = Reveal(s, 1 + r.Notes.Count, now);
        FadePaper(r.Default, .45f * last);
        FadePaper(r.Tap, .75f * last);
        if (row.Kind == FrontRoomsSettingsMenu.Kind.Meter && r.Target != null) TickTrace(r, width, s.Rest, Reveal(s, 1, now));
    }

    // A closed row: its resting name, no second line.
    static void RestClosed(RowView r)
    {
        if (r.Label != null) r.Label.enabled = true;
        if (r.LabelOpen != null) r.LabelOpen.enabled = false;
        if (r.Line != null && r.Line.gameObject.activeSelf) r.Line.gameObject.SetActive(false);
    }


    /// <summary>Builds the row's control once per shape (kind, options and stops); LayoutControl places it every frame.</summary>
    void EnsureControl(RowView v, FrontRoomsSettingsMenu.Row row)
    {
        var sig = row == null ? string.Empty : row.Kind + ":" + (row.Options != null ? string.Join("|", row.Options) : string.Empty) + ":" + (row.Stops != null ? row.Stops.Length : 0) + ":" + row.DefaultIndex + ":" + controls.Layout.SettingsSingleColumn;
        if (sig == v.ControlSig) return;
        v.ControlSig = sig;
        if (v.Control != null) Destroy(v.Control.gameObject);
        v.Cells.Clear(); v.CellTexts.Clear(); v.Bits.Clear();
        v.Knob = v.Fill = v.Thumb = v.Changed = null;
        v.Word = null;
        if (row == null) return;
        v.Control = Node("control", v.Rect, Vector2.zero, false);
        Image Part(string name) { var i = Img(name, v.Control, Vector2.zero, WithAlpha(Paper, 1f)); i.sprite = FrontRoomsTouchSprites.Solid(); return i; }
        v.Changed = Part("changed");
        switch (row.Kind)
        {
            case FrontRoomsSettingsMenu.Kind.Switch:
                v.Word = Txt("word", v.Control, string.Empty, bayon, 17, Paper, TextAnchor.MiddleRight, new Vector2(40f, 22f));
                for (var i = 0; i < 4; i++) v.Bits.Add(Part("keyline"));
                v.Knob = Part("knob");
                break;
            case FrontRoomsSettingsMenu.Kind.Segments:
            case FrontRoomsSettingsMenu.Kind.Action:
            {
                var n = row.Kind == FrontRoomsSettingsMenu.Kind.Action ? 1 : row.Options.Length;
                for (var i = 0; i < n; i++)
                {
                    v.Cells.Add(Part("cell"));
                    v.CellTexts.Add(Txt("cell label", v.Control, string.Empty, row.Mono ? plex : bayon, 17, Paper, TextAnchor.MiddleCenter, new Vector2(40f, 22f)));
                }
                for (var i = 0; i < 4 + n - 1; i++) v.Bits.Add(Part("keyline"));
                break;
            }
            case FrontRoomsSettingsMenu.Kind.Slider:
                for (var i = 0; i < 1 + row.Stops.Length; i++) v.Bits.Add(Part(i == 0 ? "track" : "tick"));
                v.Fill = Part("fill");
                v.Thumb = Part("thumb");
                v.Word = Txt("value", v.Control, string.Empty, plex, 17, Paper, TextAnchor.MiddleRight, new Vector2(58f, 22f));
                break;
            case FrontRoomsSettingsMenu.Kind.Keys:
                // A gesture's name, read only (CONTROLS › GESTURES, the old pause legend), set like a value.
                v.Word = Txt("gesture", v.Control, string.Empty, plex, 17, Paper, TextAnchor.MiddleRight, new Vector2(200f, 22f));
                break;
            case FrontRoomsSettingsMenu.Kind.Language:
                // Nothing in the row itself: the strip is on the card (TickLanguageStrip), headed by its key (平面视觉, 2026-10-31).
                break;
            case FrontRoomsSettingsMenu.Kind.Meter:
                v.Word = Txt("frame rate", v.Control, string.Empty, plex, 11, Paper, TextAnchor.MiddleRight, new Vector2(170f, 14f));
                for (var i = 0; i < 10; i++) v.Bits.Add(Part("dash"));
                break;
        }
    }

    static void Put(Graphic g, float x, float y, float w, float h)
    {
        g.rectTransform.sizeDelta = new Vector2(w, h);
        g.rectTransform.anchoredPosition = new Vector2(x + w * .5f, y);
    }

    /// <summary>Places the control against the row's right edge (row-local, centre origin) and shows the row's value.</summary>
    void LayoutControl(RowView v, FrontRoomsSettingsMenu.Row row, float width, FrontRoomsSettingsMenu.Metrics m)
    {
        if (v.Control == null) return;
        v.Control.sizeDelta = row.Kind == FrontRoomsSettingsMenu.Kind.Language ? new Vector2(width, FrontRoomsTouchLayout.SettingsRowHeightOf(row)) : Vector2.zero;
        v.Control.anchoredPosition = Vector2.zero;
        var right = width * .5f;
        var cw = FrontRoomsSettingsMenu.ControlWidth(row, m);
        var x0 = right - cw;
        // A changed row says so by shape (a dot before its control), not by colour alone (R3 rule 11).
        Put(v.Changed, x0 - 12f, 0f, 5f, 5f);
        v.Changed.color = WithAlpha(Paper, row.Changed ? .8f : 0f);
        switch (row.Kind)
        {
            case FrontRoomsSettingsMenu.Kind.Keys:
                v.Word.text = row.ValueText;
                v.Word.rectTransform.sizeDelta = new Vector2(200f, 22f);
                v.Word.rectTransform.anchoredPosition = new Vector2(right - 100f, 0f);
                break;
            case FrontRoomsSettingsMenu.Kind.Switch:
            {
                var on = row.Index == 1;
                v.Word.text = FrontRoomsLanguage.T(on ? "ON" : "OFF");
                v.Word.color = WithAlpha(Paper, on ? 1f : .7f);
                v.Word.rectTransform.sizeDelta = new Vector2(40f, 22f);
                v.Word.rectTransform.anchoredPosition = new Vector2(right - m.SwitchW - m.WordGap - 20f, 0f);
                var bx = right - m.SwitchW;
                var a = on ? 1f : .5f;
                Put(v.Bits[0], bx, m.SwitchH * .5f - .5f, m.SwitchW, 1f);
                Put(v.Bits[1], bx, -m.SwitchH * .5f + .5f, m.SwitchW, 1f);
                Put(v.Bits[2], bx, 0f, 1f, m.SwitchH);
                Put(v.Bits[3], right - 1f, 0f, 1f, m.SwitchH);
                for (var i = 0; i < 4; i++) v.Bits[i].color = WithAlpha(Paper, a);
                var inset = (m.SwitchH - m.Knob) * .5f;
                Put(v.Knob, on ? right - m.Knob - inset : bx + inset, 0f, m.Knob, m.Knob);
                v.Knob.color = WithAlpha(Paper, on ? 1f : .3f);
                break;
            }
            case FrontRoomsSettingsMenu.Kind.Segments:
            case FrontRoomsSettingsMenu.Kind.Action:
            {
                var action = row.Kind == FrontRoomsSettingsMenu.Kind.Action;
                // RESET GRAPHICS, a list's DEFAULTS asking, a bind row listening or asking (2026-11-01): the Accent cell, ink words.
                var armed = action && FrontRoomsSettingsMenu.Armed(row);
                var x = x0;
                for (var i = 0; i < v.Cells.Count; i++)
                {
                    var cell = action ? cw : FrontRoomsSettingsMenu.CellWidth(row, i, m);
                    var sel = !action && i == row.Index;
                    Put(v.Cells[i], x, 0f, cell, m.SegH);
                    v.Cells[i].color = armed ? Accent : WithAlpha(Paper, sel ? 1f : 0f);
                    var t = v.CellTexts[i];
                    t.text = action ? row.ValueText : row.Options[i];
                    t.color = sel || armed ? Ink : WithAlpha(Paper, action ? 1f : .75f);
                    t.rectTransform.sizeDelta = new Vector2(cell - 4f, 22f);
                    t.rectTransform.anchoredPosition = new Vector2(x + cell * .5f, 0f);
                    FitText(t, 17, 11);
                    if (i > 0) { Put(v.Bits[4 + i - 1], x, 0f, 1f, m.SegH); v.Bits[4 + i - 1].color = WithAlpha(Paper, .5f); }
                    x += cell;
                }
                Put(v.Bits[0], x0, m.SegH * .5f - .5f, cw, 1f);
                Put(v.Bits[1], x0, -m.SegH * .5f + .5f, cw, 1f);
                Put(v.Bits[2], x0, 0f, 1f, m.SegH);
                Put(v.Bits[3], right - 1f, 0f, 1f, m.SegH);
                for (var i = 0; i < 4; i++) v.Bits[i].color = WithAlpha(Paper, .5f);
                break;
            }
            case FrontRoomsSettingsMenu.Kind.Slider:
            {
                FrontRoomsSettingsMenu.TrackSpan(m, out var low, out _);
                var tx = right - low;
                Put(v.Bits[0], tx, 0f, m.Track, 1f);
                v.Bits[0].color = WithAlpha(Paper, .35f);
                for (var i = 0; i < row.Stops.Length; i++)
                {
                    // The default's tick stands taller (R3 rule 4).
                    var tall = i == row.DefaultIndex ? 12f : 6f;
                    Put(v.Bits[1 + i], tx + Mathf.Round(m.Track * FrontRoomsSettingsMenu.StopPosition(row, i)) - .5f, 0f, 1f, tall);
                    v.Bits[1 + i].color = WithAlpha(Paper, .35f);
                }
                var px = Mathf.Round(m.Track * FrontRoomsSettingsMenu.StopPosition(row, row.Index));
                Put(v.Fill, tx, 0f, Mathf.Max(0f, px), 3f);
                Put(v.Thumb, tx + Mathf.Clamp(px - m.ThumbW * .5f, 0f, m.Track - m.ThumbW), 0f, m.ThumbW, m.ThumbH);
                v.Word.text = row.ValueText;
                v.Word.rectTransform.sizeDelta = new Vector2(m.ValueW, 22f);
                v.Word.rectTransform.anchoredPosition = new Vector2(right - m.ValueW * .5f, 0f);
                break;
            }
            case FrontRoomsSettingsMenu.Kind.Meter:
            {
                var fps = FrontRoomsQuality.MeasuredFps;
                var target = Mathf.Max(1, FrontRoomsQuality.TargetFps);
                var lit = Mathf.Clamp(Mathf.RoundToInt(10f * fps / target), 0, 10);
                for (var i = 0; i < 10; i++)
                {
                    Put(v.Bits[i], right - 10f * 17f + i * 17f + 3f, 0f, 14f, 4f);
                    v.Bits[i].color = WithAlpha(Paper, i < lit ? 1f : .2f);
                }
                v.Word.text = Mathf.RoundToInt(fps) + " FPS  ·  " + (1000f / Mathf.Max(1f, fps)).ToString("0.0") + " MS";
                v.Word.rectTransform.sizeDelta = new Vector2(170f, 14f);
                v.Word.rectTransform.anchoredPosition = new Vector2(right - 10f * 17f - 12f - 85f, 0f);
                break;
            }
        }
    }

    static void FitText(Text text, int maximum, int minimum)
    {
        if (text == null) return;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = minimum;
        text.resizeTextMaxSize = maximum;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
    }

    static bool HasDigit(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        foreach (var c in s) if (c >= '0' && c <= '9') return true;
        return false;
    }

    static Color WithAlpha(Color c, float a)
    {
        c.a = Mathf.Clamp01(a);
        return c;
    }

    static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
}
