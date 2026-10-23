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
    Image pauseKeycapFill, pauseKeycapLine, sprintToggleDisc, sprintToggleRing;
    Text sprintToggleText;
    readonly MotionValue pausePress = new MotionValue();
    readonly MotionValue toggleOn = new MotionValue();
    readonly MotionValue controlsAlpha = new MotionValue();

    // ---------------------------------------------------------------- menus
    sealed class Item
    {
        public RectTransform Rect;
        public CanvasGroup Group;
        public Vector2 Base;
        public readonly MotionValue Alpha = new MotionValue();
        public readonly MotionValue Rise = new MotionValue();
        public readonly MotionValue Scale = new MotionValue(1f);
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
        public readonly Dictionary<Text, Image> Glyphs = new Dictionary<Text, Image>();
    }

    Legend pauseLegend, titleLegend;
    readonly Dictionary<FrontRoomsTouchControls.Button, Item> chips = new Dictionary<FrontRoomsTouchControls.Button, Item>();
    readonly Dictionary<FrontRoomsTouchControls.Button, Image> chipFills = new Dictionary<FrontRoomsTouchControls.Button, Image>();
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
        public Image Divider, Pressed, Rule;
        public string ShownValue;
        public bool OnA = true;
        public readonly MotionValue Swap = new MotionValue(1f);
        public readonly MotionValue Press = new MotionValue();
        public readonly MotionValue RuleAlpha = new MotionValue();
        public readonly MotionValue Alpha = new MotionValue();
        public readonly MotionValue Rise = new MotionValue();
        public int Id = int.MinValue;
        public bool Header;
    }

    RectTransform cardRoot, cardViewportLeft, cardViewportRight, scrollBar;
    Image scrollFade, scrollBarImage;
    Text cardHeading;
    readonly List<RowView> rows = new List<RowView>();
    readonly MotionValue scrollBarAlpha = new MotionValue();
    float lastScroll;
    float settingsOpenedAt;

    bool Reduced => FrontRoomsHandheld.ReduceMotion;
    float Dur(float seconds) => Reduced ? Mathf.Min(seconds, .12f) * .9f : seconds;

    void Awake()
    {
        controls = GetComponent<FrontRoomsTouchControls>();
        bayon = LoadFont("Fonts/Bayon-Regular");
        plex = LoadFont("Fonts/IBMPlexMono-Regular");
        serif = LoadFont("Fonts/SourceSerif4-Variable");
        BuildCanvas();
        FrontRoomsSettings.Changed += OnSettingsChanged;
    }

    void OnDestroy()
    {
        FrontRoomsSettings.Changed -= OnSettingsChanged;
        ReleaseFrost();
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
        TickMenus(L);
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

        ghost = BuildStick(controlsLayer, "Stick / rest");
        live = BuildStick(controlsLayer, "Stick / live");
        BuildUse(controlsLayer);
        BuildPause(controlsLayer);
        BuildSprintToggle(controlsLayer);
        BuildMenus();
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
        t.text = value;
        t.color = color;
        t.alignment = anchor;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.supportRichText = false;
        t.raycastTarget = false;
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
        controlsAlpha.To(playing ? 1f : 0f, Dur(playing ? .18f : .12f), playing ? Ease.OutCubic : Ease.InCubic);
        controlsGroup.alpha = controlsAlpha;
        var opacity = Mathf.Clamp(FrontRoomsSettings.TouchOpacityPercent / 34f, .5f, 1.9f);

        TickStick(L, opacity);
        TickUse(L, opacity);

        // Pause keycap (top right).
        pauseRoot.anchoredPosition = L.PauseCenter;
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
            sprintToggleRoot.anchoredPosition = L.SprintButtonCenter;
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

        // The resting ghost (floating stick) or the fixed stick at rest.
        ghost.Root.anchoredPosition = L.StickRest;
        ghost.Thumb.anchoredPosition = Vector2.zero;
        ghost.Group.alpha = ghostAlpha;
        ApplyStickStyle(ghost, 0f, 0f, 0f, 0f, windTint, opacity, false);
        ghost.LabelText.text = string.Empty;

        // The live stick under the thumb.
        if (touched) lastOrigin = controls.StickOrigin;
        live.Root.anchoredPosition = lastOrigin;
        live.Root.localScale = Vector3.one * liveScale;
        live.Group.alpha = liveAlpha;
        var thumb = touched ? controls.StickThumb - controls.StickOrigin : Vector2.LerpUnclamped(releasedThumb, Vector2.zero, thumbReturn);
        live.Thumb.anchoredPosition = thumb;
        live.Thumb.localScale = Vector3.one * (1f + .15f * runPop);
        live.RunLine.localScale = Vector3.one * (1f + .04f * runPop);
        TickSlot(L, touched, running, winded, thumb);
        ApplyStickStyle(live, 1f, runTint, controls.RunReach, slowTint, windTint, opacity, controls.RunStickMode);
    }

    // ------------------------------------------------------ the slot words (X2)

    static string SlotText(SlotWord word)
    {
        switch (word)
        {
            case SlotWord.Winded: return "WINDED";
            case SlotWord.Run:
            case SlotWord.Preview: return "RUN";
            case SlotWord.Slow: return "SLOW · UNHEARD";
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
        var visible = prompt.Visible && controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Playing;
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
        useRoot.anchoredPosition = L.UseCenter + new Vector2(Reduced ? 0f : Shake(lockedShake.Current), 0f) + hang;
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

    static string UseVerb(FrontRoomsTouchControls.UsePrompt prompt)
    {
        switch (prompt.Kind)
        {
            case FrontRoomsTouchControls.UseKind.Open: return "OPEN";
            case FrontRoomsTouchControls.UseKind.Shut: return "SHUT";
            case FrontRoomsTouchControls.UseKind.Take: return "TAKE";
            case FrontRoomsTouchControls.UseKind.Locked: return "LOCKED";
            case FrontRoomsTouchControls.UseKind.Broken: return "BROKEN";
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
        confirmTextText = Txt("Restart question", menuLayer, "RESTART THIS RUN?", bayon, 17, Ink, TextAnchor.MiddleCenter, new Vector2(400f, 22f), true);
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
    /// USE · DOOR / HOLD USE · BREAK GLASS. IBM Plex Mono 11 caps (iPad 12) beside 22 pt glyphs, ink on the pause frost,
    /// paper with the keyline on the title (平面视觉).
    /// </summary>
    Legend BuildLegend(RectTransform rowA, RectTransform rowB, Color color, bool keyline)
    {
        var l = new Legend { RowA = rowA, RowB = rowB };
        l.Move = LegendEntry(l, rowA, FrontRoomsTouchSprites.Glyph.Move, "MOVE", color, keyline, out _);
        l.Drag = LegendEntry(l, rowA, FrontRoomsTouchSprites.Glyph.Drag, "DRAG · LOOK", color, keyline, out _);
        l.Slow = LegendEntry(l, rowA, FrontRoomsTouchSprites.Glyph.Slow, "SLOW · UNHEARD", color, keyline, out _);
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

    /// <summary>The run entry follows the RUN setting; the hold entry follows TAP mode. Then both rows are laid out.</summary>
    void UpdateLegend(Legend l)
    {
        l.Sprint.text = controls.RunStickMode ? "PAST THE RING · RUN" : "RUN BUTTON";
        l.SprintGlyph.sprite = FrontRoomsTouchSprites.GlyphSprite(controls.RunStickMode ? FrontRoomsTouchSprites.Glyph.Run : FrontRoomsTouchSprites.Glyph.SprintButton);
        l.Hold.text = FrontRoomsSettings.TapToBreak ? "TAP USE · BREAK GLASS" : "HOLD USE · BREAK GLASS";
        LayoutLegendRow(l, l.Move, l.Drag, l.Slow, l.Sprint);
        LayoutLegendRow(l, l.Use, l.Hold);
    }

    void LayoutLegendRow(Legend l, params Text[] entries)
    {
        // Glyph + 6 pt + label, entries 26 pt apart, the row centred; the caps' middle on the glyphs' middle.
        var total = 0f;
        for (var i = 0; i < entries.Length; i++) total += 22f + 6f + entries[i].preferredWidth + (i > 0 ? 26f : 0f);
        var x = -total * .5f;
        foreach (var entry in entries)
        {
            var labelWidth = entry.preferredWidth;
            if (l.Glyphs.TryGetValue(entry, out var glyph)) glyph.rectTransform.anchoredPosition = new Vector2(x + 11f, 0f);
            entry.rectTransform.sizeDelta = new Vector2(labelWidth + 2f, 16f);
            entry.rectTransform.anchoredPosition = new Vector2(x + 28f + labelWidth * .5f, -Caps(entry).y);
            x += 22f + 6f + labelWidth + 26f;
        }
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
        if (scrollFade != null) scrollFade.sprite = FrontRoomsTouchSprites.FadeUp();
        if (scrollBarImage != null) scrollBarImage.sprite = FrontRoomsTouchSprites.Solid();
        foreach (var r in rows)
        {
            if (r.Divider != null) r.Divider.sprite = FrontRoomsTouchSprites.Solid();
            if (r.Pressed != null) r.Pressed.sprite = FrontRoomsTouchSprites.Solid();
            if (r.Rule != null) r.Rule.sprite = FrontRoomsTouchSprites.Solid();
        }
    }

    void BuildChip(FrontRoomsTouchControls.Button button)
    {
        var label = ChipLabel(button);
        var rt = Node("Chip " + label, menuLayer, new Vector2(80f, FrontRoomsTouchLayout.ChipHeight));
        var fill = rt.gameObject.AddComponent<Image>();
        var dark = DarkChip(button);
        fill.color = dark ? CardColor : Accent;
        fill.raycastTarget = false;
        fill.sprite = FrontRoomsTouchSprites.Solid();
        Txt("label", rt, label, bayon, 17, dark ? Paper : Ink, TextAnchor.MiddleCenter, new Vector2(140f, 22f)).rectTransform.anchoredPosition = new Vector2(0f, 1f);
        var item = MakeItem(label, rt);
        chips[button] = item;
        chipFills[button] = fill;
    }

    /// <summary>An exit, not a go: QUIT TO TITLE sits on the solid ink chip with paper caps, so it never weighs as much as RESUME (平面视觉, 2026-10-08).</summary>
    static bool DarkChip(FrontRoomsTouchControls.Button b) => b == FrontRoomsTouchControls.Button.ReturnTitle;

    static string ChipLabel(FrontRoomsTouchControls.Button b)
    {
        switch (b)
        {
            case FrontRoomsTouchControls.Button.Resume: return "RESUME";
            case FrontRoomsTouchControls.Button.Settings: return "SETTINGS";
            case FrontRoomsTouchControls.Button.Restart: return "RESTART";
            case FrontRoomsTouchControls.Button.ReturnTitle: return "QUIT TO TITLE";
            case FrontRoomsTouchControls.Button.RestartCancel: return "CANCEL";
            case FrontRoomsTouchControls.Button.RestartConfirm: return "RESTART";
            case FrontRoomsTouchControls.Button.TryAgain: return "TRY AGAIN";
            case FrontRoomsTouchControls.Button.SettingsClose: return "BACK";
            default: return string.Empty;
        }
    }

    void BuildSettingsCard()
    {
        cardRoot = Node("Settings card", menuLayer, new Vector2(742f, 369f));
        var card = cardRoot.gameObject.AddComponent<Image>();
        // Opaque on touch: 3 % of a textured frost showing through would read as dirt, and the scroll fade must match it.
        card.color = CardColor;
        card.sprite = FrontRoomsTouchSprites.Solid();
        card.raycastTarget = false;
        settingsCard = MakeItem("card", cardRoot);
        cardHeading = Txt("SETTINGS", cardRoot, "SETTINGS", bayon, 17, Paper, TextAnchor.MiddleLeft, new Vector2(200f, 22f));
        cardViewportLeft = Node("Left column", cardRoot, Vector2.zero, false);
        cardViewportRight = Node("Touch column", cardRoot, Vector2.zero, false);
        cardViewportRight.gameObject.AddComponent<RectMask2D>();
        scrollFade = Img("scroll fade", cardRoot, new Vector2(337f, 56f), WithAlpha(CardColor, 0f));
        scrollBarImage = Img("scroll bar", cardRoot, new Vector2(2f, 40f), WithAlpha(Muted, 0f));
        scrollBar = scrollBarImage.rectTransform;
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
        r.Divider = Img("divider", r.Rect, new Vector2(FrontRoomsTouchLayout.SettingsColumnWidth, 1f), WithAlpha(Paper, .08f));
        r.Divider.sprite = FrontRoomsTouchSprites.Solid();
        r.Divider.rectTransform.anchoredPosition = new Vector2(0f, -FrontRoomsTouchLayout.SettingsRowHeight * .5f + .5f);
        r.Rule = Img("accent rule", r.Rect, new Vector2(3f, 28f), WithAlpha(Accent, 0f));
        r.Rule.sprite = FrontRoomsTouchSprites.Solid();
        r.Rule.rectTransform.anchoredPosition = new Vector2(-FrontRoomsTouchLayout.SettingsColumnWidth * .5f - 14.5f, 0f);
        r.Label = Txt("label", r.Rect, string.Empty, bayon, 17, Paper, TextAnchor.MiddleLeft, new Vector2(FrontRoomsTouchLayout.SettingsColumnWidth, 22f));
        r.ValueA = Txt("value A", r.Rect, string.Empty, bayon, 17, Paper, TextAnchor.MiddleRight, new Vector2(FrontRoomsTouchLayout.SettingsColumnWidth, 22f));
        r.ValueB = Txt("value B", r.Rect, string.Empty, bayon, 17, Paper, TextAnchor.MiddleRight, new Vector2(FrontRoomsTouchLayout.SettingsColumnWidth, 22f));
        r.ChevronLeft = Txt("chevron left", r.Rect, "‹", bayon, 17, Muted, TextAnchor.MiddleCenter, new Vector2(12f, 22f));
        r.ChevronRight = Txt("chevron right", r.Rect, "›", bayon, 17, Muted, TextAnchor.MiddleCenter, new Vector2(12f, 22f));
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
        // No frame to freeze (no camera): the opaque paper wash it replaces.
        frostVeil.color = WithAlpha(WashColor, frostTexture != null ? FrostVeil : .98f);
        var frame = L.Frame;
        frostPaper.uvRect = new Rect(0f, 0f, frame.x / FrostTileWidth, frame.y / (FrostTileWidth * 1.5f));
        if (!washed && washAlpha.Current <= .001f && frostTexture != null) ReleaseFrost();

        var paused = state == FrontRoomsTouchControls.MenuState.Paused;
        if (entered || confirmChanged)
        {
            // Pause: title, meta, legend, chips rise into place, staggered (alpha only under Reduce Motion).
            Show(pauseTitle, paused, .04f, 18f);
            Show(pauseMeta, paused, .07f, 12f);
            Show(legendA, paused && !confirm, .10f, 10f);
            Show(legendB, paused && !confirm, .13f, 10f);
            Show(confirmText, paused && confirm, .04f, 8f);
            ShowChip(FrontRoomsTouchControls.Button.Resume, paused && !confirm, .16f);
            ShowChip(FrontRoomsTouchControls.Button.Settings, paused && !confirm, .19f);
            ShowChip(FrontRoomsTouchControls.Button.Restart, paused && !confirm, .22f);
            // Keep the existing RESUME / SETTINGS / RESTART row stable; the explicit
            // QUIT TO TITLE action sits beneath it so it cannot be confused with RESTART.
            ShowChip(FrontRoomsTouchControls.Button.ReturnTitle, paused && !confirm, .25f);
            ShowChip(FrontRoomsTouchControls.Button.RestartCancel, paused && confirm, .05f);
            ShowChip(FrontRoomsTouchControls.Button.RestartConfirm, paused && confirm, .08f);

            var settings = state == FrontRoomsTouchControls.MenuState.Settings;
            if (settings && entered) settingsOpenedAt = now;
            Show(settingsCard, settings, 0f, 0f, settings ? .97f : 1f);
            ShowChip(FrontRoomsTouchControls.Button.SettingsClose, settings, .08f);

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
            UpdateLegend(pauseLegend);
        }
        foreach (var pair in chips)
        {
            if (controls.TryGetChip(pair.Key, out var center, out var size)) pair.Value.Base = center;
            if (size.x > 0f) pair.Value.Rect.sizeDelta = size;
            var pressed = controls.PressedButton == pair.Key;
            pair.Value.Scale.To(pressed ? .94f : 1f, Dur(pressed ? .07f : .16f), pressed ? Ease.OutCubic : Ease.OutBackSoft);
            chipFills[pair.Key].color = DarkChip(pair.Key) ? (pressed ? Color.Lerp(CardColor, Paper, .12f) : CardColor)
                : (pressed ? Color.Lerp(Accent, Ink, .08f) : Accent);
        }
        // The subtitle sits at y 97 and pushes the block below it down 10 pt (平面视觉, FIX_CARD_VL217 A7).
        Place(caughtTitle, L.FromMaster(437f, 64f));
        Place(caughtSubtitle, L.FromMaster(437f, 97f));
        for (var i = 0; i < 4; i++)
        {
            var x = 62f + 750f * (i + .5f) / 4f;
            Place(caughtNumbers[i], L.FromMaster(x, 135f));
            Place(caughtLabels[i], L.FromMaster(x, 164f));
        }
        Place(caughtRule, L.FromMaster(437f, 196f));
        Place(caughtLine, L.FromMaster(437f, 235f));
        // Up 14 pt from 320 (board option c), so row B clears the iPhone's home inset.
        Place(titlePrompt, L.FromMaster(437f, 306f));
        if (state == FrontRoomsTouchControls.MenuState.Caught) TickCaught(now);

        // Settings card.
        cardRoot.sizeDelta = L.SettingsCardSize;
        settingsCard.Base = L.SettingsCardCenter;
        TickSettings(L, now);

        TickItems();
        TickTitle(now, state);
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
        item.Rect.anchoredPosition = item.Base + Vector2.down * item.Rise.Current;
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
        caughtLineText.text = string.IsNullOrEmpty(stats.Where) ? string.Empty : "Caught in " + stats.Where + ".";
    }

    void TickCaught(float now)
    {
        // The numbers count up as the card settles (instantly under Reduce Motion).
        var t = Reduced ? 1f : FrontRoomsTouchMotion.Evaluate(Ease.OutCubic, (now - caughtStart - .2f) / .6f);
        for (var i = 0; i < 4; i++)
        {
            var v = Mathf.RoundToInt(caughtTargets[i] * t);
            caughtNumberTexts[i].text = i == 0 ? v + " s" : v.ToString();
        }
    }

    void TickTitle(float now, FrontRoomsTouchControls.MenuState state)
    {
        if (state != FrontRoomsTouchControls.MenuState.Title) return;
        if (titleShownAt > 0f && now >= titleShownAt && !titlePrompt.Shown)
        {
            titlePrompt.Shown = true;
            titlePrompt.Alpha.Restart(0f, 1f, Reduced ? .2f : .6f, Ease.InOutSine);
        }
        // Static once in: the corridor already moves. If a "waiting" signal ever returns, it is a hard on/off of a
        // solid element (a block cursor, 0.6 s / 0.6 s, off under REDUCE FLASHING), never an alpha ramp on the words.
        // TAP TO START's caps are centred on the lockup's point; row A's cap top sits 16 pt under its baseline and
        // row B follows at the glyph box + 8 pt.
        titlePromptText.rectTransform.anchoredPosition = new Vector2(0f, -Caps(titlePromptText).y);
        UpdateLegend(titleLegend);
        var rowA = -(17f * BayonCapHeightEm * .5f + 16f + LegendSize * PlexCapHeightEm * .5f);
        titleLegend.RowA.anchoredPosition = new Vector2(0f, rowA);
        titleLegend.RowB.anchoredPosition = new Vector2(0f, rowA - (22f + 8f));
    }

    // ---------------------------------------------------------------- settings

    void TickSettings(FrontRoomsTouchLayout L, float now)
    {
        var card = L.SettingsCardSize;
        var cardCenter = L.SettingsCardCenter;
        // Card-local coordinates (the card's centre is 0, 0).
        Vector2 Local(Vector2 frame) => frame - cardCenter;
        cardHeading.rectTransform.anchoredPosition = new Vector2(-card.x * .5f + 24f + 100f, card.y * .5f - 27f);

        var slots = controls.SettingsSlots;
        var left = L.SettingsColumn(0);
        var right = L.SettingsColumn(1);
        var viewport = controls.SettingsScrollViewport;
        cardViewportRight.anchoredPosition = Local(viewport.center);
        cardViewportRight.sizeDelta = viewport.size;
        cardViewportLeft.anchoredPosition = Local(left.center);
        cardViewportLeft.sizeDelta = left.size;

        var settingsOpen = controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Settings;
        if (!settingsOpen)
        {
            // Closing: the rows fade with the card; they are cleared once it is gone.
            if (settingsCard.Alpha.Current <= .001f && rows.Count > 0)
            {
                foreach (var dead in rows) if (dead != null) Destroy(dead.Rect.gameObject);
                rows.Clear();
            }
            scrollFade.color = WithAlpha(CardColor, 0f);
            return;
        }
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
            var parent = s.Column == 1 && !s.Header ? cardViewportRight : cardRoot;
            if (r.Rect.parent != parent) r.Rect.SetParent(parent, false);
            var pos = s.Rect.center;
            if (parent == cardViewportRight) pos = pos - viewport.center + new Vector2(0f, controls.SettingsScroll);
            else pos = Local(pos);

            if (r.Id != s.Id)
            {
                r.Id = s.Id;
                r.ShownValue = null;
                // Rows rise in, 24 ms apart, when the card opens.
                var delay = Mathf.Max(0f, settingsOpenedAt + .06f + i * .024f - now);
                r.Alpha.Restart(0f, 1f, Dur(.2f), Ease.OutCubic, Reduced ? 0f : delay);
                if (!Reduced) r.Rise.Restart(8f, 0f, .2f, Ease.OutCubic, delay); else r.Rise.Snap(0f);
            }
            r.Rect.anchoredPosition = pos + Vector2.down * r.Rise.Current;
            r.Group.alpha = r.Alpha.Current;
            r.Label.text = s.Label;
            r.Label.rectTransform.anchoredPosition = Vector2.zero;
            if (s.Header) continue;

            // The value: crossfades and slides when it changes; numerals in IBM Plex Mono (never Bayon, 平面视觉).
            var value = s.Value ?? string.Empty;
            if (r.ShownValue == null)
            {
                (r.OnA ? r.ValueA : r.ValueB).text = value;
                (r.OnA ? r.ValueB : r.ValueA).text = string.Empty;
                r.Swap.Snap(1f);
            }
            else if (value != r.ShownValue)
            {
                r.OnA = !r.OnA;
                (r.OnA ? r.ValueA : r.ValueB).text = value;
                r.Swap.Restart(0f, 1f, Dur(.14f), Ease.OutCubic);
            }
            r.ShownValue = value;
            var cur = r.OnA ? r.ValueA : r.ValueB;
            var prev = r.OnA ? r.ValueB : r.ValueA;
            foreach (var v in new[] { cur, prev })
            {
                var numeric = HasDigit(v.text);
                v.font = numeric ? plex : bayon;
                v.fontSize = 17;
            }
            var valueRight = s.Stepped ? -22f : 0f;
            cur.rectTransform.anchoredPosition = new Vector2(valueRight, Reduced ? 0f : -6f * (1f - r.Swap));
            prev.rectTransform.anchoredPosition = new Vector2(valueRight, Reduced ? 0f : 6f * r.Swap);
            // ‹ value › on stepped rows: ‹ steps down, › steps up (the state machine's tap zones).
            r.ChevronLeft.gameObject.SetActive(s.Stepped);
            r.ChevronRight.gameObject.SetActive(s.Stepped);
            if (s.Stepped)
            {
                var half = FrontRoomsTouchLayout.SettingsColumnWidth * .5f;
                r.ChevronRight.rectTransform.anchoredPosition = new Vector2(half - 6f, 0f);
                r.ChevronLeft.rectTransform.anchoredPosition = new Vector2(half + valueRight - cur.preferredWidth - 14f, 0f);
            }

            var pressedNow = controls.PressedSettingsRow == s.Id;
            r.Press.To(pressedNow ? 1f : 0f, Dur(pressedNow ? .06f : .2f), Ease.OutCubic);
            r.Pressed.color = WithAlpha(Paper, .07f * r.Press);
            var activeRow = controls.ActiveSettingsRow == s.Id;
            r.RuleAlpha.To(activeRow ? 1f : 0f, Dur(.12f), Ease.OutCubic);
            r.Rule.color = WithAlpha(Accent, r.RuleAlpha);
            // The row last changed turns yellow, like the desktop's selected row.
            var tint = Color.Lerp(Paper, Accent, r.RuleAlpha.Current);
            r.Label.color = tint;
            cur.color = WithAlpha(tint, .9f * r.Swap + .1f * r.RuleAlpha.Current);
            prev.color = WithAlpha(tint, .9f * (1f - r.Swap));
        }
        for (var i = slots.Count; i < rows.Count; i++)
        {
            if (rows[i] != null) Destroy(rows[i].Rect.gameObject);
        }
        if (rows.Count > slots.Count) rows.RemoveRange(slots.Count, rows.Count - slots.Count);

        // The touch column's scroll: a fade where rows continue, and a thin bar while it moves.
        var max = controls.SettingsScrollMax;
        var scroll = controls.SettingsScroll;
        var moreBelow = max > .5f && scroll < max - .5f;
        scrollFade.rectTransform.anchoredPosition = Local(new Vector2(viewport.center.x, viewport.yMin + 28f));
        scrollFade.rectTransform.sizeDelta = new Vector2(viewport.width, 56f);
        // Only once the card has arrived: two translucent layers of one colour would stack darker while it fades in.
        scrollFade.color = WithAlpha(CardColor, moreBelow ? Mathf.InverseLerp(.9f, 1f, settingsCard.Alpha.Current) : 0f);
        if (Mathf.Abs(scroll - lastScroll) > .1f) { scrollBarAlpha.Restart(scrollBarAlpha.Current, 1f, .08f, Ease.OutCubic); }
        else if (scrollBarAlpha.Done && scrollBarAlpha.Target > .5f) scrollBarAlpha.To(0f, .4f, Ease.InCubic, .6f);
        lastScroll = scroll;
        if (max > .5f)
        {
            var content = viewport.height + max;
            var barH = Mathf.Max(24f, viewport.height * viewport.height / content);
            var t = scroll / max;
            var y = viewport.yMax - barH * .5f - t * (viewport.height - barH);
            scrollBar.sizeDelta = new Vector2(2f, barH);
            scrollBar.anchoredPosition = Local(new Vector2(viewport.xMax + 10f, y));
            scrollBarImage.color = WithAlpha(Muted, .6f * scrollBarAlpha);
        }
        else scrollBarImage.color = WithAlpha(Muted, 0f);
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
