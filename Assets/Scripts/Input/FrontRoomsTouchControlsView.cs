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
        public RectTransform Root, Thumb, Socket, Halo, Label;
        public Image BaseDisc, BaseRing, SocketRing, SocketArc, HaloRing, ThumbDisc;
        public Text LabelText;
        public CanvasGroup Group;
    }

    StickView ghost, live;
    readonly MotionValue ghostAlpha = new MotionValue(1f);
    readonly MotionValue liveAlpha = new MotionValue();
    readonly MotionValue liveScale = new MotionValue(1f);
    readonly MotionValue thumbReturn = new MotionValue(1f);
    readonly MotionValue haloGrow = new MotionValue();
    readonly MotionValue latchPop = new MotionValue();
    readonly MotionValue sprintTint = new MotionValue();
    readonly MotionValue labelAlpha = new MotionValue();
    readonly MotionValue windTint = new MotionValue();
    bool wasTouched, wasLatched, wasWinded;
    Vector2 releasedThumb, lastOrigin;

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

    Image wash;
    readonly MotionValue washAlpha = new MotionValue();
    Item pauseTitle, pauseMeta, legendA, legendB, confirmText, titlePrompt, caughtTitle, caughtRule, caughtLine, settingsCard;
    Text pauseMetaText, confirmTextText, caughtLineText;
    Text legendATextMove, legendATextDrag, legendATextSprint, legendBTextUse, legendBTextHold;
    Image legendSprintGlyph;
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
        s.Socket = Node("socket", s.Root, Vector2.zero, false);
        s.SocketRing = Img("socket ring", s.Socket, Vector2.zero, Paper);
        s.SocketArc = Img("socket arming arc", s.Socket, Vector2.zero, Accent);
        s.SocketArc.type = Image.Type.Filled;
        s.SocketArc.fillMethod = Image.FillMethod.Radial360;
        s.SocketArc.fillOrigin = (int)Image.Origin360.Top;
        s.SocketArc.fillClockwise = true;
        s.SocketArc.fillAmount = 0f;
        s.Halo = Node("halo", s.Root, Vector2.zero, false);
        s.HaloRing = s.Halo.gameObject.AddComponent<Image>();
        s.HaloRing.color = Accent;
        s.HaloRing.raycastTarget = false;
        s.Thumb = Node("thumb", s.Root, Vector2.zero, false);
        s.ThumbDisc = s.Thumb.gameObject.AddComponent<Image>();
        s.ThumbDisc.color = Paper;
        s.ThumbDisc.raycastTarget = false;
        s.LabelText = Txt("state label", s.Root, string.Empty, bayon, 17, Accent, TextAnchor.MiddleCenter, new Vector2(120f, 20f));
        s.Label = s.LabelText.rectTransform;
        return s;
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
        sprintToggleText = Txt("label", sprintToggleRoot, "SPRINT", bayon, 17, Paper, TextAnchor.MiddleCenter, new Vector2(60f, 22f));
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
            var sd = L.SocketRadius * 2f;
            s.Socket.anchoredPosition = new Vector2(0f, L.SocketOffset);
            s.SocketRing.sprite = FrontRoomsTouchSprites.Ring(sd, 2f);
            s.SocketRing.rectTransform.sizeDelta = new Vector2(sd, sd);
            // The arming arc fills outside the thumb (Ø52 covers the Ø40 socket), where the latched halo sits:
            // the halo is the arc completed, the same rule as USE's hold ring.
            var hd = 66f * k;
            s.SocketArc.sprite = FrontRoomsTouchSprites.Ring(hd, 3.5f);
            s.SocketArc.rectTransform.sizeDelta = new Vector2(hd, hd);
            s.HaloRing.sprite = FrontRoomsTouchSprites.Ring(hd, 3.5f);
            s.Halo.sizeDelta = new Vector2(hd, hd);
            s.Halo.anchoredPosition = new Vector2(0f, L.SocketOffset);
            var td = L.ThumbRadius * 2f;
            s.ThumbDisc.sprite = FrontRoomsTouchSprites.Disc(td);
            s.Thumb.sizeDelta = new Vector2(td, td);
            s.Label.anchoredPosition = new Vector2(0f, L.SocketOffset + 33f * k + 14f);
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
        wash.sprite = FrontRoomsTouchSprites.Solid();
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

        // SPRINT toggle (only when Settings › SPRINT is BUTTON).
        var showToggle = !controls.SprintSocketMode;
        sprintToggleRoot.gameObject.SetActive(showToggle);
        if (showToggle)
        {
            sprintToggleRoot.anchoredPosition = L.SprintButtonCenter;
            toggleOn.To(controls.SprintLatched ? 1f : 0f, Dur(.12f), Ease.OutCubic);
            var toggleWinded = controls.Winded;
            sprintToggleDisc.color = WithAlpha(Color.Lerp(Media, Accent, toggleOn), Mathf.Min(1f, Mathf.Lerp(.45f * opacity, 1f, toggleOn)));
            sprintToggleRing.color = WithAlpha(toggleWinded ? Muted : Color.Lerp(Paper, Accent, toggleOn), Mathf.Min(1f, (toggleWinded ? .4f : .8f) * opacity + toggleOn));
            sprintToggleText.color = toggleWinded ? Muted : Color.Lerp(Paper, Ink, toggleOn);
            var pressed = controls.PressedButton == FrontRoomsTouchControls.Button.SprintToggle;
            sprintToggleRoot.localScale = Vector3.one * (Reduced || !pressed ? 1f : .92f);
        }
    }

    void TickStick(FrontRoomsTouchLayout L, float opacity)
    {
        var touched = controls.StickTouched;
        var latched = controls.SprintLatched;
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

        if (latched != wasLatched)
        {
            if (latched)
            {
                haloGrow.Snap(1f);
                sprintTint.To(1f, Dur(.12f), Ease.OutCubic);
                labelAlpha.To(1f, Dur(.14f), Ease.OutCubic);
                if (!Reduced) latchPop.Restart(1f, 0f, .22f, Ease.OutCubic);
            }
            else
            {
                haloGrow.To(0f, Dur(.15f), Ease.InCubic);
                sprintTint.To(0f, Dur(.15f), Ease.InCubic);
                if (!winded) labelAlpha.To(0f, Dur(.1f), Ease.InCubic);
            }
            wasLatched = latched;
        }

        if (winded != wasWinded)
        {
            windTint.To(winded ? 1f : 0f, Dur(.2f), Ease.OutCubic);
            labelAlpha.To(winded || latched ? 1f : 0f, Dur(.14f), Ease.OutCubic);
            wasWinded = winded;
        }

        // The resting ghost (floating stick) or the fixed stick at rest.
        ghost.Root.anchoredPosition = L.StickRest;
        ghost.Thumb.anchoredPosition = Vector2.zero;
        ghost.Group.alpha = ghostAlpha;
        ApplyStickStyle(ghost, 0f, 0f, 0f, windTint, opacity, 0f);
        ghost.LabelText.text = string.Empty;
        ghost.SocketArc.fillAmount = 0f;
        ghost.Halo.gameObject.SetActive(false);

        // The live stick under the thumb.
        if (touched) lastOrigin = controls.StickOrigin;
        live.Root.anchoredPosition = lastOrigin;
        live.Root.localScale = Vector3.one * liveScale;
        live.Group.alpha = liveAlpha;
        var thumb = touched ? controls.StickThumb - controls.StickOrigin : Vector2.LerpUnclamped(releasedThumb, Vector2.zero, thumbReturn);
        live.Thumb.anchoredPosition = thumb;
        var arm = controls.SocketArm;
        live.SocketArc.fillAmount = latched ? 1f : arm;
        // Latched: the completed arc stays as the halo; letting go fades it instead of snapping it off.
        live.Halo.gameObject.SetActive(haloGrow > .001f);
        live.Halo.localScale = Vector3.one * (1f + .15f * latchPop);
        live.HaloRing.color = WithAlpha(Accent, Mathf.Clamp01(haloGrow));
        live.Socket.localScale = Vector3.one * (1f + .15f * latchPop);
        live.LabelText.text = winded ? "WINDED" : "SPRINT";
        live.LabelText.color = WithAlpha(winded ? Muted : Accent, labelAlpha);
        live.Label.anchoredPosition = new Vector2(0f, L.SocketOffset + 33f * L.Scale + 14f - (Reduced ? 0f : 6f * (1f - labelAlpha)));
        ApplyStickStyle(live, 1f, sprintTint, arm, windTint, opacity, 1f);
    }

    /// <summary>The Figma stick states: rest (ghost), walk, sprint (yellow thumb in the socket), winded (muted).</summary>
    static void ApplyStickStyle(StickView s, float activeness, float sprint, float arm, float winded, float opacity, float liveLayer)
    {
        var baseFill = Mathf.Lerp(.18f, .28f, activeness);
        var ring = Mathf.Lerp(.35f, .6f, activeness);
        var socket = Mathf.Lerp(.25f, .45f, activeness);
        var thumb = Mathf.Lerp(.30f, .9f, activeness);
        s.BaseDisc.color = WithAlpha(Media, Mathf.Min(1f, baseFill * opacity));
        s.BaseRing.color = WithAlpha(Paper, Mathf.Min(1f, ring * opacity));
        var socketColor = Color.Lerp(Color.Lerp(Paper, Accent, arm * .6f), Muted, winded);
        s.SocketRing.color = WithAlpha(socketColor, Mathf.Min(1f, Mathf.Lerp(socket, .35f, winded) * opacity));
        s.SocketArc.color = WithAlpha(Accent, Mathf.Min(1f, liveLayer * (1f - winded)));
        var thumbColor = Color.Lerp(Color.Lerp(Paper, Accent, sprint), Muted, winded);
        s.ThumbDisc.color = WithAlpha(thumbColor, Mathf.Min(1f, Mathf.Lerp(thumb, 1f, sprint) * Mathf.Lerp(1f, .85f, winded) * (sprint > .5f ? 1f : opacity)));
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
        useRoot.anchoredPosition = L.UseCenter + new Vector2(Reduced ? 0f : Shake(lockedShake.Current), 0f);
        useGroup.alpha = useAlpha;

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
        var locked = prompt.Kind == FrontRoomsTouchControls.UseKind.Locked || prompt.Locked;
        var press = usePress.Current;
        var textColor = locked ? Muted : Color.Lerp(Paper, Ink, press);
        var current = useLabelOnA ? useTextA : useTextB;
        var previous = useLabelOnA ? useTextB : useTextA;
        current.color = WithAlpha(textColor, labelSwap);
        previous.color = WithAlpha(textColor, 1f - labelSwap);
        current.rectTransform.anchoredPosition = new Vector2(0f, Reduced ? 0f : -4f * (1f - labelSwap));
        previous.rectTransform.anchoredPosition = new Vector2(0f, Reduced ? 0f : 4f * labelSwap);

        useDisc.color = WithAlpha(Media, Mathf.Min(1f, .55f * opacity + .2f));
        usePressed.color = WithAlpha(Accent, press * (locked ? .35f : 1f));
        useRing.color = WithAlpha(Paper, locked ? .3f : Mathf.Min(1f, .9f * opacity + .3f));

        // The hold ring sits outside the thumb, so the progress stays visible under a pressing thumb.
        var hold = visible && prompt.Hold;
        holdTrackAlpha.To(hold ? 1f : 0f, Dur(.12f), Ease.OutCubic);
        holdShown = Mathf.MoveTowards(holdShown, hold ? prompt.Progress : 0f, Time.unscaledDeltaTime / .06f);
        holdTrack.color = WithAlpha(Paper, .3f * holdTrackAlpha);
        holdArc.color = WithAlpha(Accent, holdTrackAlpha);
        holdArc.fillAmount = holdShown;
    }

    static float Shake(float t) => t >= 1f ? 0f : Mathf.Sin(t * Mathf.PI * 6f) * 4f * (1f - t);

    static string UseVerb(FrontRoomsTouchControls.UsePrompt prompt)
    {
        switch (prompt.Kind)
        {
            case FrontRoomsTouchControls.UseKind.Open: return "OPEN";
            case FrontRoomsTouchControls.UseKind.Shut: return "SHUT";
            case FrontRoomsTouchControls.UseKind.Take: return "TAKE";
            case FrontRoomsTouchControls.UseKind.Locked: return "LOCKED";
            case FrontRoomsTouchControls.UseKind.Hold: return prompt.TapMode ? "TAP" : "BREAK";
            case FrontRoomsTouchControls.UseKind.Tap: return "TAP";
            default: return string.Empty;
        }
    }

    // ================================================================= menus

    void BuildMenus()
    {
        wash = Img("Wash", menuLayer, Vector2.zero, WithAlpha(WashColor, 0f), true);
        var wrt = wash.rectTransform;
        wrt.anchorMin = Vector2.zero;
        wrt.anchorMax = Vector2.one;
        wrt.offsetMin = wrt.offsetMax = Vector2.zero;

        // Pause card (Figma TS 6 · PAUSE).
        pauseTitle = MakeItem("PAUSED", Txt("PAUSED", menuLayer, "PAUSED", bayon, 48, Ink, TextAnchor.MiddleCenter, new Vector2(600f, 48f), true).rectTransform);
        pauseMetaText = Txt("Pause meta", menuLayer, string.Empty, plex, 11, WithAlpha(Ink, .6f), TextAnchor.MiddleCenter, new Vector2(700f, 16f), true);
        pauseMeta = MakeItem("meta", pauseMetaText.rectTransform);
        legendA = MakeItem("legend 1", Node("Legend 1", menuLayer, new Vector2(700f, 24f)));
        legendB = MakeItem("legend 2", Node("Legend 2", menuLayer, new Vector2(700f, 24f)));
        BuildLegendRows();
        confirmTextText = Txt("Restart question", menuLayer, "RESTART THIS RUN?", bayon, 17, Ink, TextAnchor.MiddleCenter, new Vector2(400f, 22f), true);
        confirmText = MakeItem("confirm", confirmTextText.rectTransform);

        foreach (var b in new[] { FrontRoomsTouchControls.Button.Resume, FrontRoomsTouchControls.Button.Settings, FrontRoomsTouchControls.Button.Restart,
                     FrontRoomsTouchControls.Button.RestartCancel, FrontRoomsTouchControls.Button.RestartConfirm, FrontRoomsTouchControls.Button.TryAgain,
                     FrontRoomsTouchControls.Button.SettingsClose })
            BuildChip(b);

        // Caught card (Figma TS 8 · CAUGHT).
        caughtTitle = MakeItem("CAUGHT", Txt("CAUGHT", menuLayer, "CAUGHT", bayon, 48, Ink, TextAnchor.MiddleCenter, new Vector2(600f, 48f), true).rectTransform);
        var labels = new[] { "SURVIVED", "ZONES CROSSED", "TIER REACHED", "DOORS IT BROKE" };
        for (var i = 0; i < 4; i++)
        {
            caughtNumberTexts[i] = Txt("stat " + i, menuLayer, "0", serif, 26, Ink, TextAnchor.MiddleCenter, new Vector2(180f, 30f), true);
            caughtNumbers[i] = MakeItem("stat", caughtNumberTexts[i].rectTransform);
            caughtLabels[i] = MakeItem("stat label", Txt("stat label " + i, menuLayer, labels[i], bayon, 17, WithAlpha(Ink, .55f), TextAnchor.MiddleCenter, new Vector2(180f, 20f), true).rectTransform);
        }
        var rule = Img("Caught rule", menuLayer, new Vector2(718f, 1f), WithAlpha(Ink, .2f), true);
        rule.sprite = FrontRoomsTouchSprites.Solid();
        caughtRule = MakeItem("rule", rule.rectTransform);
        caughtLineText = Txt("Caught line", menuLayer, string.Empty, serif, 20, Ink, TextAnchor.MiddleCenter, new Vector2(700f, 24f), true);
        caughtLine = MakeItem("line", caughtLineText.rectTransform);

        // Title: a text line under the wordmark, no container (Red's review).
        titlePrompt = MakeItem("TAP TO START", Txt("TAP TO START", menuLayer, "TAP TO START", bayon, 17, WithAlpha(Paper, .95f), TextAnchor.MiddleCenter, new Vector2(300f, 22f), true).rectTransform);

        BuildSettingsCard();
    }

    Item MakeItem(string name, RectTransform rt)
    {
        var item = new Item { Rect = rt, Group = rt.gameObject.AddComponent<CanvasGroup>() };
        item.Group.alpha = 0f;
        item.Group.blocksRaycasts = false;
        return item;
    }

    void BuildLegendRows()
    {
        legendATextMove = LegendEntry(legendA.Rect, FrontRoomsTouchSprites.Glyph.Move, "MOVE", out _);
        legendATextDrag = LegendEntry(legendA.Rect, FrontRoomsTouchSprites.Glyph.Drag, "DRAG  ·  LOOK", out _);
        legendATextSprint = LegendEntry(legendA.Rect, FrontRoomsTouchSprites.Glyph.Socket, "SOCKET  ·  SPRINT", out legendSprintGlyph);
        legendBTextUse = LegendEntry(legendB.Rect, FrontRoomsTouchSprites.Glyph.Use, "USE  ·  DOOR", out _);
        legendBTextHold = LegendEntry(legendB.Rect, FrontRoomsTouchSprites.Glyph.Hold, "HOLD USE  ·  BREAK GLASS", out _);
    }

    Text LegendEntry(RectTransform row, FrontRoomsTouchSprites.Glyph glyph, string label, out Image glyphImage)
    {
        glyphImage = Img("glyph " + glyph, row, new Vector2(22f, 22f), Ink);
        glyphImage.name = "glyph:" + glyph;
        var t = Txt("legend " + label, row, label, bayon, 17, Ink, TextAnchor.MiddleLeft, new Vector2(240f, 22f));
        return t;
    }

    void LayoutLegendRow(RectTransform row, params Text[] entries)
    {
        // Glyph + 6 pt + label, entries 26 pt apart, the row centred (Figma pause legend).
        var widths = new float[entries.Length];
        var total = 0f;
        for (var i = 0; i < entries.Length; i++)
        {
            widths[i] = 22f + 6f + entries[i].preferredWidth;
            total += widths[i] + (i > 0 ? 26f : 0f);
        }
        var x = -total * .5f;
        for (var i = 0; i < entries.Length; i++)
        {
            var glyph = entries[i].transform.parent.Find("glyph:" + GlyphFor(entries[i]));
            if (glyph != null) ((RectTransform)glyph).anchoredPosition = new Vector2(x + 11f, 0f);
            var labelWidth = entries[i].preferredWidth;
            entries[i].rectTransform.sizeDelta = new Vector2(labelWidth + 2f, 22f);
            entries[i].rectTransform.anchoredPosition = new Vector2(x + 28f + labelWidth * .5f, 1f);
            x += widths[i] + 26f;
        }
    }

    string GlyphFor(Text entry)
    {
        if (entry == legendATextMove) return FrontRoomsTouchSprites.Glyph.Move.ToString();
        if (entry == legendATextDrag) return FrontRoomsTouchSprites.Glyph.Drag.ToString();
        if (entry == legendATextSprint) return FrontRoomsTouchSprites.Glyph.Socket.ToString();
        if (entry == legendBTextUse) return FrontRoomsTouchSprites.Glyph.Use.ToString();
        return FrontRoomsTouchSprites.Glyph.Hold.ToString();
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
        fill.color = Accent;
        fill.raycastTarget = false;
        fill.sprite = FrontRoomsTouchSprites.Solid();
        Txt("label", rt, label, bayon, 17, Ink, TextAnchor.MiddleCenter, new Vector2(120f, 22f)).rectTransform.anchoredPosition = new Vector2(0f, 1f);
        var item = MakeItem(label, rt);
        chips[button] = item;
        chipFills[button] = fill;
    }

    static string ChipLabel(FrontRoomsTouchControls.Button b)
    {
        switch (b)
        {
            case FrontRoomsTouchControls.Button.Resume: return "RESUME";
            case FrontRoomsTouchControls.Button.Settings: return "SETTINGS";
            case FrontRoomsTouchControls.Button.Restart: return "RESTART";
            case FrontRoomsTouchControls.Button.RestartCancel: return "CANCEL";
            case FrontRoomsTouchControls.Button.RestartConfirm: return "RESTART";
            case FrontRoomsTouchControls.Button.TryAgain: return "TRY AGAIN";
            case FrontRoomsTouchControls.Button.SettingsClose: return "CLOSE";
            default: return string.Empty;
        }
    }

    void BuildSettingsCard()
    {
        cardRoot = Node("Settings card", menuLayer, new Vector2(742f, 369f));
        var card = cardRoot.gameObject.AddComponent<Image>();
        card.color = WithAlpha(CardColor, .97f);
        card.sprite = FrontRoomsTouchSprites.Solid();
        card.raycastTarget = false;
        settingsCard = MakeItem("card", cardRoot);
        cardHeading = Txt("SETTINGS", cardRoot, "SETTINGS", bayon, 17, Paper, TextAnchor.MiddleLeft, new Vector2(200f, 22f));
        cardViewportLeft = Node("Left column", cardRoot, Vector2.zero, false);
        cardViewportRight = Node("Touch column", cardRoot, Vector2.zero, false);
        cardViewportRight.gameObject.AddComponent<RectMask2D>();
        scrollFade = Img("scroll fade", cardRoot, new Vector2(337f, 56f), WithAlpha(CardColor, .97f));
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
        var now = Time.unscaledTime;

        // The wash: the desktop pause wash, over the room.
        var washed = state == FrontRoomsTouchControls.MenuState.Paused || state == FrontRoomsTouchControls.MenuState.Settings || state == FrontRoomsTouchControls.MenuState.Caught;
        if (entered)
        {
            var caught = state == FrontRoomsTouchControls.MenuState.Caught;
            washAlpha.To(washed ? .98f : 0f, Dur(washed ? (caught ? .42f : .24f) : .16f), washed ? Ease.OutCubic : Ease.InCubic);
        }
        wash.color = WithAlpha(WashColor, washAlpha);

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
            ShowChip(FrontRoomsTouchControls.Button.RestartCancel, paused && confirm, .05f);
            ShowChip(FrontRoomsTouchControls.Button.RestartConfirm, paused && confirm, .08f);

            var settings = state == FrontRoomsTouchControls.MenuState.Settings;
            if (settings && entered) settingsOpenedAt = now;
            Show(settingsCard, settings, 0f, 0f, settings ? .97f : 1f);
            ShowChip(FrontRoomsTouchControls.Button.SettingsClose, settings, .08f);

            var caughtState = state == FrontRoomsTouchControls.MenuState.Caught;
            if (caughtState && entered) StartCaught();
            Show(caughtTitle, caughtState, .08f, -12f);
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
            legendATextSprint.text = controls.SprintSocketMode ? "SOCKET  ·  SPRINT" : "SPRINT BUTTON";
            legendSprintGlyph.sprite = FrontRoomsTouchSprites.GlyphSprite(controls.SprintSocketMode ? FrontRoomsTouchSprites.Glyph.Socket : FrontRoomsTouchSprites.Glyph.SprintButton);
            legendBTextHold.text = FrontRoomsSettings.TapToBreak ? "TAP USE  ·  BREAK GLASS" : "HOLD USE  ·  BREAK GLASS";
            LayoutLegendRow(legendA.Rect, legendATextMove, legendATextDrag, legendATextSprint);
            LayoutLegendRow(legendB.Rect, legendBTextUse, legendBTextHold);
        }
        foreach (var pair in chips)
        {
            if (controls.TryGetChip(pair.Key, out var center, out var size)) pair.Value.Base = center;
            if (size.x > 0f) pair.Value.Rect.sizeDelta = size;
            var pressed = controls.PressedButton == pair.Key;
            pair.Value.Scale.To(pressed ? .94f : 1f, Dur(pressed ? .07f : .16f), pressed ? Ease.OutCubic : Ease.OutBackSoft);
            chipFills[pair.Key].color = pressed ? Color.Lerp(Accent, Ink, .08f) : Accent;
        }
        Place(caughtTitle, L.FromMaster(437f, 64f));
        for (var i = 0; i < 4; i++)
        {
            var x = 62f + 750f * (i + .5f) / 4f;
            Place(caughtNumbers[i], L.FromMaster(x, 125f));
            Place(caughtLabels[i], L.FromMaster(x, 154f));
        }
        Place(caughtRule, L.FromMaster(437f, 186f));
        Place(caughtLine, L.FromMaster(437f, 225f));
        Place(titlePrompt, L.FromMaster(437f, 320f));
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
        Apply(caughtTitle); Apply(caughtRule); Apply(caughtLine); Apply(titlePrompt);
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
        caughtStart = Time.unscaledTime;
        var stats = controls.Host != null ? controls.Host.CaughtStats : default;
        caughtTargets[0] = stats.Seconds;
        caughtTargets[1] = stats.Zones;
        caughtTargets[2] = stats.Tier;
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
        if (titlePrompt.Shown && titlePrompt.Alpha.Done && !Reduced && !FrontRoomsSettings.ReduceFlashing)
        {
            // A slow breath (2.4 s) so the line reads as waiting, never blinking.
            var breath = .775f + .225f * Mathf.Cos((now - titleShownAt - .6f) * Mathf.PI * 2f / 2.4f);
            titlePrompt.Group.alpha = breath;
        }
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
        scrollFade.color = WithAlpha(CardColor, moreBelow ? .97f : 0f);
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
