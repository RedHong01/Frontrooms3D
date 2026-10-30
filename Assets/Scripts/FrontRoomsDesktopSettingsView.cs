using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Tab = FrontRoomsSettingsMenu.Tab;
using Kind = FrontRoomsSettingsMenu.Kind;
using Row = FrontRoomsSettingsMenu.Row;

/// <summary>
/// The desktop settings sheet (Mac, Windows), drawn over the pause card. Figma: section 3688:7842, S1–S3 (1920 × 1080,
/// 12 columns, 72 px margin): SETTINGS, a tab row (Q / E), the list in columns 1–7, the focused row's explanation in
/// columns 9–12, and on GRAPHICS a live preview of the game at the current settings. Clicking the preview (or SPACE)
/// leaves the sheet to show the game full screen; a click (or SPACE, ESC) brings it back (Red, 2026-10-24).
/// Rows and values come from FrontRoomsSettingsMenu; faces and sizes are the desktop masters' (Bayon 20, IBM Plex Mono
/// 13, Source Serif 24, at tracking 0).
/// </summary>
public sealed class FrontRoomsDesktopSettingsView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IPointerMoveHandler, IScrollHandler
{
    static readonly Color Paper = new Color(.957f, .945f, .91f, 1f);
    static readonly Color Ink = new Color(.055f, .055f, .051f, 1f);
    static readonly Color Accent = new Color(.957f, .875f, .231f, 1f);
    const float M = 72f, ListTop = 256f, DetailW = 576f, PreviewH = 324f;
    static FrontRoomsSettingsMenu.Metrics Mx => FrontRoomsSettingsMenu.Desktop;

    Font bayon, mono, serif;
    Camera cam;
    RectTransform root, listMask, listContent, detail, peekLayer, tabsRoot;
    CanvasGroup overlayGroup;
    Image sheet, focusBar, preview2Frame, veil;
    RawImage preview, frost;
    RenderTexture previewRt, frostRt;
    readonly List<Image> keyEdges = new List<Image>();
    Text title, detailTitle, detailBody, detailMeta, previewCaption, frameText, peekText, peekHint, hints, backText, restartText;
    Image back, restart;
    // CONTROLS' second level (Red, 2026-10-29): the KEYBOARD & MOUSE map, its breadcrumb and BACK.
    FrontRoomsKeyMapView keyMap;
    Text crumb, pageBackText;
    Image pageBack, pageDefaults;
    Text pageDefaultsText;
    bool pageOpen;
    float defaultsArmedUntil = -1f;
    // The KEYBOARD & MOUSE row's keycaps as drawn (list units, by control path): A1 sends them to their keys.
    readonly Dictionary<string, Rect> pageCaps = new Dictionary<string, Rect>();
    // In the order of FrontRoomsSettingsMenu.KeyPreview's keycaps: W A S D, SPRINT (Ctrl), SLOW (Shift), USE.
    static readonly FrontRoomsInput.Bind[] PreviewBinds = { FrontRoomsInput.Bind.Forward, FrontRoomsInput.Bind.Left, FrontRoomsInput.Bind.Back, FrontRoomsInput.Bind.Right, FrontRoomsInput.Bind.Sprint, FrontRoomsInput.Bind.Slow, FrontRoomsInput.Bind.Use };
    string PageHints
    {
        get
        {
            if (keyMap.State == FrontRoomsKeyMapView.CaptureState.Listening) return "PRESS A KEY OR A MOUSE BUTTON     ESC  CANCEL";
            if (keyMap.State == FrontRoomsKeyMapView.CaptureState.Asking) return "ENTER  SWAP     ESC  CANCEL";
            return "↑ ↓ ← →  CHOOSE     ENTER  CHANGE     BACKSPACE  DEFAULT     DELETE  ALL DEFAULTS     ESC  BACK";
        }
    }

    // The pause is this page on the desktop (Red, 2026-10-24): Esc resumes; R ends the run and goes to the title at once
    // (Red, 2026-10-30: no question before the title).
    const string HintKeys = "↑ ↓  CHOOSE     ← →  CHANGE     Q E  GROUP     BACKSPACE  DEFAULT     SPACE  PREVIEW";
    static string HintText()
    {
        var r = FrontRoomsInput.KeyName(FrontRoomsInput.PathOf(FrontRoomsInput.Bind.Restart));
        return HintKeys + "     " + r + "  END RUN" + "     ESC  RESUME";
    }
    readonly List<Image> frameDashes = new List<Image>(), peekDashes = new List<Image>();
    Text peekFrameText;
    readonly List<Text> tabTexts = new List<Text>();
    readonly List<Image> tabRules = new List<Image>();
    readonly List<RowView> views = new List<RowView>();
    List<Row> rows;
    string signature;
    float scroll, contentH;
    int dragRow = -1;
    bool open;

    public Tab CurrentTab { get; private set; } = Tab.Graphics;
    public int Focus { get; private set; } = 1;
    public bool Peeking { get; private set; }
    public bool Open => open;

    sealed class RowView
    {
        public RectTransform Rect;
        public Text Label, Value;
        public Image Changed, Divider;
        public readonly List<Image> Cells = new List<Image>();
        public readonly List<Text> CellTexts = new List<Text>();
        public readonly List<Image> Bits = new List<Image>();
        public Image Knob, Fill, Thumb;
        public float Y, H;
    }

    // ------------------------------------------------------------- build

    public static FrontRoomsDesktopSettingsView Create(Transform overlay, Transform canvasRoot, Font bayon, Font mono, Font serif, Camera cam)
    {
        var go = new GameObject("Settings sheet (desktop)", typeof(RectTransform));
        go.transform.SetParent(overlay, false);
        var v = go.AddComponent<FrontRoomsDesktopSettingsView>();
        v.bayon = bayon; v.mono = mono; v.serif = serif; v.cam = cam;
        // (Not ??: a missing component is Unity's fake null.)
        v.overlayGroup = overlay.GetComponent<CanvasGroup>();
        if (v.overlayGroup == null) v.overlayGroup = overlay.gameObject.AddComponent<CanvasGroup>();
        v.Build(canvasRoot);
        go.SetActive(false);
        return v;
    }

    void Build(Transform canvasRoot)
    {
        root = (RectTransform)transform;
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
        // The handheld sheet's look (Red, 2026-10-24: one black frosted sheet): the paused frame, frosted, under black at
        // 86 %. The root Image only takes the pointer. (A translucent sheet over the light pause wash reads grey: UI blends
        // in linear light.)
        sheet = gameObject.AddComponent<Image>();
        sheet.color = new Color(0f, 0f, 0f, 0f);
        frost = new GameObject("Paused frame (frosted)", typeof(RectTransform)).AddComponent<RawImage>();
        frost.transform.SetParent(root, false);
        frost.raycastTarget = false;
        frost.color = Color.white;
        frost.rectTransform.anchorMin = Vector2.zero; frost.rectTransform.anchorMax = Vector2.one; frost.rectTransform.offsetMin = frost.rectTransform.offsetMax = Vector2.zero;
        veil = Img(root, Ink, .86f);
        veil.rectTransform.anchorMin = Vector2.zero; veil.rectTransform.anchorMax = Vector2.one; veil.rectTransform.offsetMin = veil.rectTransform.offsetMax = Vector2.zero;
        title = Txt(root, "SETTINGS", bayon, 88, Paper, TextAnchor.UpperLeft);
        Place(title.rectTransform, M, 56f, 900f, 96f);
        tabsRoot = Node(root, "Tabs");
        Place(tabsRoot, M, 184f, 1200f, 40f);
        var rule = Img(root, Paper, .16f);
        rule.rectTransform.anchorMin = new Vector2(0f, 1f); rule.rectTransform.anchorMax = new Vector2(1f, 1f);
        rule.rectTransform.pivot = new Vector2(0f, 1f);
        rule.rectTransform.offsetMin = new Vector2(M, -233f); rule.rectTransform.offsetMax = new Vector2(-M, -232f);
        listMask = Node(root, "List");
        listMask.gameObject.AddComponent<RectMask2D>();
        listContent = Node(listMask, "Rows");
        Place(listContent, 0f, 0f, 1026f, 10f);
        focusBar = Img(listContent, Accent, 1f);
        detail = Node(root, "Detail");
        detail.anchorMin = detail.anchorMax = new Vector2(1f, 1f); detail.pivot = new Vector2(0f, 1f);
        detail.sizeDelta = new Vector2(DetailW, 760f);
        detailTitle = Txt(detail, "", bayon, 20, Paper, TextAnchor.UpperLeft);
        PlaceIn(detailTitle.rectTransform, 0f, 8f, DetailW, 24f);
        detailBody = Txt(detail, "", serif, 24, new Color(Paper.r, Paper.g, Paper.b, .9f), TextAnchor.UpperLeft);
        detailBody.lineSpacing = 1.05f;
        detailBody.horizontalOverflow = HorizontalWrapMode.Wrap;
        PlaceIn(detailBody.rectTransform, 0f, 48f, DetailW, 190f);
        detailMeta = Txt(detail, "", mono, 13, new Color(Paper.r, Paper.g, Paper.b, .75f), TextAnchor.UpperLeft);
        detailMeta.lineSpacing = 1.3f;
        PlaceIn(detailMeta.rectTransform, 0f, 250f, DetailW, 60f);
        // The live preview (GRAPHICS): the game camera at the current settings; a click shows it full screen.
        preview2Frame = Img(detail, Paper, .35f);
        PlaceIn(preview2Frame.rectTransform, -1f, 323f, DetailW + 2f, PreviewH + 2f);
        preview = new GameObject("Preview", typeof(RectTransform)).AddComponent<RawImage>();
        preview.transform.SetParent(detail, false);
        preview.raycastTarget = false;
        PlaceIn(preview.rectTransform, 0f, 324f, DetailW, PreviewH);
        previewCaption = Txt(detail, "CLICK THE PICTURE OR PRESS SPACE TO SEE IT FULL SCREEN", mono, 13, new Color(Paper.r, Paper.g, Paper.b, .75f), TextAnchor.UpperLeft);
        PlaceIn(previewCaption.rectTransform, 0f, 660f, DetailW, 20f);
        frameText = Txt(detail, "", mono, 13, Paper, TextAnchor.UpperLeft);
        PlaceIn(frameText.rectTransform, 220f, 692f, 360f, 20f);
        var fl = Txt(detail, "FRAME RATE NOW", mono, 13, new Color(Paper.r, Paper.g, Paper.b, .75f), TextAnchor.UpperLeft);
        PlaceIn(fl.rectTransform, 0f, 692f, 200f, 20f);
        frameText.transform.SetAsLastSibling();
        for (var i = 0; i < 10; i++)
        {
            var d = Img(detail, Paper, .2f);
            PlaceIn(d.rectTransform, 220f + i * 19f, 712f, 16f, 4f);
            frameDashes.Add(d);
        }
        hints = Txt(root, HintText(), mono, 13, new Color(Paper.r, Paper.g, Paper.b, .75f), TextAnchor.LowerLeft);
        hints.rectTransform.anchorMin = hints.rectTransform.anchorMax = Vector2.zero; hints.rectTransform.pivot = Vector2.zero;
        hints.rectTransform.anchoredPosition = new Vector2(M, M - 8f); hints.rectTransform.sizeDelta = new Vector2(1200f, 20f);
        // RESUME (the old BACK) and END RUN: keyline buttons, paper word, no fill (yellow means focus only on a settings page;
        // 平面视觉 fix 2). The desktop pause is this page (Red, 2026-10-24), so they carry the PAUSED card's two actions; END RUN
        // (the phone's word) goes to the title at once (Red, 2026-10-30).
        back = KeylineButton("RESUME", 0f, out backText);
        restart = KeylineButton("END RUN", back.rectTransform.sizeDelta.x + 12f, out restartText);
        // The map page: in columns 1–8 from the list's top, its breadcrumb where a section header sits; BACK takes the
        // corner from RESUME and END RUN while it is open (Figma 3688:7842, K2).
        crumb = Txt(root, "CONTROLS / KEYBOARD & MOUSE", mono, 13, new Color(Paper.r, Paper.g, Paper.b, .75f), TextAnchor.MiddleLeft);
        Place(crumb.rectTransform, M + 24f, ListTop + 4f, 600f, 20f);
        crumb.gameObject.SetActive(false);
        keyMap = FrontRoomsKeyMapView.Create(root, bayon, mono, Paper, Ink, Accent);
        pageBack = KeylineButton("BACK", 0f, out pageBackText);
        pageBack.gameObject.SetActive(false);
        // DEFAULTS asks first (DEFAULTS?): a second press within 3 s puts every key back.
        pageDefaults = KeylineButton("DEFAULTS?", pageBack.rectTransform.sizeDelta.x + 12f, out pageDefaultsText);
        pageDefaultsText.text = "DEFAULTS";
        pageDefaults.gameObject.SetActive(false);

        // PEEK: the game full screen, only a quiet line at the margin (no backing card), like the HUD.
        peekLayer = Node(canvasRoot, "Settings preview (full screen)");
        peekLayer.anchorMin = Vector2.zero; peekLayer.anchorMax = Vector2.one; peekLayer.offsetMin = peekLayer.offsetMax = Vector2.zero;
        peekText = Txt(peekLayer, "PREVIEW", bayon, 20, Paper, TextAnchor.UpperLeft);
        Place(peekText.rectTransform, M, M, 600f, 24f);
        peekFrameText = Txt(peekLayer, "", mono, 13, Paper, TextAnchor.UpperLeft);
        Place(peekFrameText.rectTransform, M + 220f, M + 36f, 400f, 20f);
        var pl = Txt(peekLayer, "FRAME RATE NOW", mono, 13, new Color(Paper.r, Paper.g, Paper.b, .75f), TextAnchor.UpperLeft);
        Place(pl.rectTransform, M, M + 36f, 200f, 20f);
        for (var i = 0; i < 10; i++)
        {
            var d = Img(peekLayer, Paper, .2f);
            Place(d.rectTransform, M + 220f + i * 19f, M + 56f, 16f, 4f);
            peekDashes.Add(d);
        }
        peekHint = Txt(peekLayer, "CLICK, SPACE OR ESC TO RETURN TO SETTINGS", mono, 13, new Color(Paper.r, Paper.g, Paper.b, .7f), TextAnchor.LowerLeft);
        peekHint.rectTransform.anchorMin = peekHint.rectTransform.anchorMax = Vector2.zero; peekHint.rectTransform.pivot = Vector2.zero;
        peekHint.rectTransform.anchoredPosition = new Vector2(M, M - 8f); peekHint.rectTransform.sizeDelta = new Vector2(900f, 20f);
        peekLayer.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------- state

    public void SetOpen(bool value)
    {
        open = value;
        if (!open) SetPeek(false);
        gameObject.SetActive(open);
        if (open) transform.SetAsLastSibling();
        if (open) CaptureFrost(); else ReleaseFrost();
        if (open)
        {
            pageOpen = false;
            keyMap.SetActive(false);
            signature = null;
            scroll = 0f;
            Focus = FirstSelectable();
            Refresh();
        }
        else ReleasePreview();
    }

    void SetPeek(bool value)
    {
        Peeking = value;
        if (overlayGroup != null) overlayGroup.alpha = value ? 0f : 1f;
        if (peekLayer != null) peekLayer.gameObject.SetActive(value);
    }

    void SetTab(int index)
    {
        if (pageOpen) { pageOpen = false; keyMap.SetActive(false); }
        var n = FrontRoomsSettingsMenu.Tabs.Length;
        CurrentTab = FrontRoomsSettingsMenu.Tabs[(index % n + n) % n];
        scroll = 0f;
        signature = null;
        rows = FrontRoomsSettingsMenu.Rows(CurrentTab);
        Focus = FirstSelectable();
        Refresh();
    }

    int FirstSelectable()
    {
        rows = FrontRoomsSettingsMenu.Rows(CurrentTab);
        for (var i = 0; i < rows.Count; i++) if (Selectable(rows[i])) return i;
        return 0;
    }

    static bool Selectable(Row r) => r.Kind != Kind.Header && r.Kind != Kind.Meter && r.Kind != Kind.Keys;

    /// <summary>The keys while the sheet is open (the game keeps Esc, H and T).</summary>
    public void HandleKeys()
    {
        if (!open) return;
        if (Peeking)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) SetPeek(false);
            return;
        }
        if (pageOpen) { HandlePageKeys(); return; }
        rows = FrontRoomsSettingsMenu.Rows(CurrentTab);
        var move = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) ? -1 : Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) ? 1 : 0;
        if (move != 0)
        {
            for (var i = 1; i <= rows.Count; i++)
            {
                var j = ((Focus + move * i) % rows.Count + rows.Count) % rows.Count;
                if (Selectable(rows[j])) { Focus = j; break; }
            }
            KeepFocusVisible();
            Refresh();
            return;
        }
        if (Input.GetKeyDown(KeyCode.Q)) { SetTab((int)CurrentTab - 1); return; }
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Tab)) { SetTab((int)CurrentTab + 1); return; }
        if (Input.GetKeyDown(KeyCode.Space) && CurrentTab == Tab.Graphics) { SetPeek(true); return; }
        var row = Focus >= 0 && Focus < rows.Count ? rows[Focus] : null;
        if (row == null) return;
        var step = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) ? -1 : Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) ? 1 : 0;
        var enter = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
        if (row.Kind == Kind.Page && (enter || step > 0)) { OpenPage(); return; }
        if (step != 0) { FrontRoomsSettingsMenu.Step(row, step); Refresh(); return; }
        if (enter) { FrontRoomsSettingsMenu.Activate(row); Refresh(); return; }
        if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete)) { FrontRoomsSettingsMenu.ResetRow(row); Refresh(); }
    }

    /// <summary>ESC: out of the preview or a second-level page first, then the game closes the sheet.</summary>
    public bool ConsumeEscape()
    {
        if (pageOpen && keyMap.Capturing) { keyMap.CancelCapture(); DrawPageDetail(); return true; }
        if (pageOpen) { ClosePage(); return true; }
        if (!Peeking) return false;
        SetPeek(false);
        return true;
    }

    /// <summary>The map page's keys: arrows or W A S D choose the nearest action that way; Q / E change tab (and leave it).</summary>
    void HandlePageKeys()
    {
        // The map's y runs down the page, so up is -y.
        var dir = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) ? Vector2.down
            : Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) ? Vector2.up
            : Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) ? Vector2.left
            : Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) ? Vector2.right : Vector2.zero;
        if (keyMap.Capturing) { keyMap.UpdateCapture(); DrawPageDetail(); return; }
        if (dir != Vector2.zero) { keyMap.Navigate(dir); DrawPageDetail(); return; }
        if (Input.GetKeyDown(KeyCode.Q)) { SetTab((int)CurrentTab - 1); return; }
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Tab)) { SetTab((int)CurrentTab + 1); return; }
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)) { keyMap.BeginCapture(); DrawPageDetail(); return; }
        if (Input.GetKeyDown(KeyCode.Backspace)) { keyMap.ResetChosen(); DrawPageDetail(); return; }
        if (Input.GetKeyDown(KeyCode.Delete)) PressDefaults();
    }

    /// <summary>DEFAULTS (or Delete): the first press asks, a second within 3 s puts every key back.</summary>
    void PressDefaults()
    {
        if (FrontRoomsInput.AllDefault) return;
        if (Time.unscaledTime < defaultsArmedUntil)
        {
            defaultsArmedUntil = -1f;
            FrontRoomsInput.ResetAllBinds();
            keyMap.Refresh();
        }
        else defaultsArmedUntil = Time.unscaledTime + 3f;
        DrawPageDetail();
    }

    /// <summary>Opens the focused Page row's second level (CONTROLS' KEYBOARD &amp; MOUSE map).</summary>
    void OpenPage()
    {
        if (pageOpen) return;
        pageOpen = true;
        keyMap.SetActive(true);
        Refresh();
        // A1: the row's keycaps travel to their keys (the map shares the list's origin; it may be scaled).
        var s = Mathf.Max(.01f, keyMap.Root.localScale.x);
        var from = new Dictionary<string, Rect>();
        foreach (var kv in pageCaps) from[kv.Key] = new Rect(kv.Value.x / s, (kv.Value.y - scroll) / s, kv.Value.width / s, kv.Value.height / s);
        keyMap.PlayOpen(from);
    }

    void ClosePage()
    {
        if (!pageOpen) return;
        pageOpen = false;
        keyMap.SetActive(false);
        Refresh();
    }

    /// <summary>A second-level page is open (Esc goes back to its tab before it closes the sheet).</summary>
    public bool PageOpen => pageOpen;

    void ShowPage()
    {
        listMask.gameObject.SetActive(!pageOpen);
        crumb.gameObject.SetActive(pageOpen);
        back.gameObject.SetActive(!pageOpen);
        restart.gameObject.SetActive(!pageOpen);
        pageBack.gameObject.SetActive(pageOpen);
        pageDefaults.gameObject.SetActive(pageOpen);
        hints.text = pageOpen ? PageHints : HintText();
        if (pageOpen) PlaceMap();
    }

    /// <summary>Columns 1–8: from the margin to a gutter before the right column; the map scales down on narrower windows
    /// (every frame while it is open, so a resized window keeps it on the grid).</summary>
    void PlaceMap() => keyMap.PlaceAt(M, ListTop, Mathf.Max(0f, root.rect.width - M - DetailW - 24f - M));

    void DrawPageDetail()
    {
        detailTitle.text = keyMap.Title;
        detailBody.text = keyMap.Body;
        detailMeta.text = keyMap.Meta;
        hints.text = PageHints;
        var asking = Time.unscaledTime < defaultsArmedUntil;
        pageDefaultsText.text = asking ? "DEFAULTS?" : "DEFAULTS";
        pageDefaultsText.color = new Color(Paper.r, Paper.g, Paper.b, FrontRoomsInput.AllDefault ? .4f : 1f);
        preview.gameObject.SetActive(false);
        preview2Frame.gameObject.SetActive(false);
        previewCaption.gameObject.SetActive(false);
        ReleasePreview();
    }

    void KeepFocusVisible()
    {
        if (Focus < 0 || Focus >= views.Count || views[Focus] == null) return;
        var v = views[Focus];
        var h = listMask.rect.height;
        if (v.Y - scroll < 0f) scroll = v.Y;
        else if (v.Y + v.H - scroll > h) scroll = v.Y + v.H - h;
        scroll = Mathf.Clamp(scroll, 0f, Mathf.Max(0f, contentH - h));
    }

    /// <summary>A keyline button at the bottom right, <paramref name="rightInset"/> left of the margin, sized for <paramref name="widest"/>.</summary>
    Image KeylineButton(string widest, float rightInset, out Text word)
    {
        var b = Img(root, Paper, 0f);
        b.rectTransform.anchorMin = b.rectTransform.anchorMax = new Vector2(1f, 0f); b.rectTransform.pivot = new Vector2(1f, 0f);
        word = Txt(b.rectTransform, widest, bayon, 20, Paper, TextAnchor.MiddleCenter);
        b.rectTransform.anchoredPosition = new Vector2(-M - rightInset, M - 16f);
        b.rectTransform.sizeDelta = new Vector2(Mathf.Max(64f, Mathf.Ceil(word.preferredWidth) + 26f), 38f);
        word.rectTransform.anchorMin = Vector2.zero; word.rectTransform.anchorMax = Vector2.one; word.rectTransform.offsetMin = word.rectTransform.offsetMax = Vector2.zero;
        foreach (var edge in new[] { new Vector4(0f, 1f, 1f, 1f), new Vector4(0f, 0f, 1f, 0f), new Vector4(0f, 0f, 0f, 1f), new Vector4(1f, 0f, 1f, 1f) })
        {
            var e = Img(b.rectTransform, Paper, .5f);
            var ert = e.rectTransform;
            ert.anchorMin = new Vector2(edge.x, edge.y); ert.anchorMax = new Vector2(edge.z, edge.w);
            ert.sizeDelta = new Vector2(edge.x == edge.z ? 1f : 0f, edge.y == edge.w ? 1f : 0f);
            ert.anchoredPosition = new Vector2(edge.x == edge.z ? (edge.x == 0f ? .5f : -.5f) : 0f, edge.y == edge.w ? (edge.y == 0f ? .5f : -.5f) : 0f);
        }
        return b;
    }

    void Update()
    {
        if (!open) return;
        var fps = FrontRoomsQuality.MeasuredFps;
        var target = Mathf.Max(1, FrontRoomsQuality.TargetFps);
        var text = Mathf.RoundToInt(fps) + " FPS  ·  " + (1000f / Mathf.Max(1f, fps)).ToString("0.0") + " MS  ·  TARGET " + target;
        var lit = Mathf.Clamp(Mathf.RoundToInt(10f * fps / target), 0, 10);
        frameText.text = text;
        peekFrameText.text = text;
        for (var i = 0; i < 10; i++)
        {
            frameDashes[i].color = new Color(Paper.r, Paper.g, Paper.b, i < lit ? 1f : .2f);
            peekDashes[i].color = frameDashes[i].color;
        }
        if (rows != null) foreach (var v in views) if (v != null && v.Value != null && v.Bits.Count > 0 && rows.Count > views.IndexOf(v) && rows[views.IndexOf(v)].Kind == Kind.Meter) UpdateMeter(v);
        if (FrontRoomsSettingsMenu.ResetArmedUntil > 0f && !FrontRoomsSettingsMenu.ResetArmed) { FrontRoomsSettingsMenu.ResetArmedUntil = -1f; Refresh(); }
        if (pageOpen)
        {
            PlaceMap();
            keyMap.Animate();
            if (defaultsArmedUntil > 0f && Time.unscaledTime >= defaultsArmedUntil) { defaultsArmedUntil = -1f; DrawPageDetail(); }
        }
        RenderPreview();
    }

    // ------------------------------------------------------------- draw

    public void Refresh()
    {
        if (!open) return;
        rows = FrontRoomsSettingsMenu.Rows(CurrentTab);
        if (Focus >= rows.Count || Focus < 0 || !Selectable(rows[Focus])) Focus = FirstSelectable();
        LayoutFrame();
        DrawTabs();
        ShowPage();
        if (pageOpen) { keyMap.Refresh(); DrawPageDetail(); return; }
        var sig = CurrentTab.ToString();
        foreach (var r in rows) sig += "|" + r.Key + r.Kind + (r.Options != null ? string.Join(",", r.Options) : "") + r.Label;
        if (sig != signature) { signature = sig; RebuildRows(); }
        UpdateRows();
        DrawDetail();
    }

    void LayoutFrame()
    {
        var w = root.rect.width;
        var listW = Mathf.Clamp(w - M - DetailW - M - 96f, 640f, 1026f);
        Place(listMask, M, ListTop, listW + 24f, Mathf.Max(200f, root.rect.height - ListTop - 120f));
        listContent.sizeDelta = new Vector2(listW, listContent.sizeDelta.y);
        detail.anchoredPosition = new Vector2(-M - DetailW, -ListTop);
    }

    void DrawTabs()
    {
        var names = FrontRoomsSettingsMenu.Tabs;
        while (tabTexts.Count < names.Length + 2)
        {
            var t = Txt(tabsRoot, "", bayon, 20, Paper, TextAnchor.UpperLeft);
            tabTexts.Add(t);
            tabRules.Add(Img(tabsRoot, Paper, 0f));
        }
        while (keyEdges.Count < 8) keyEdges.Add(Img(tabsRoot, Paper, .5f));
        var x = 0f;
        for (var i = 0; i < names.Length + 2; i++)
        {
            var key = i == 0 || i == names.Length + 1;
            var t = tabTexts[i];
            t.font = key ? mono : bayon;
            t.fontSize = key ? 13 : 20;
            t.text = key ? (i == 0 ? "Q" : "E") : FrontRoomsSettingsMenu.TabName(names[i - 1]);
            var selected = !key && names[i - 1] == CurrentTab;
            t.color = new Color(Paper.r, Paper.g, Paper.b, key ? .8f : selected ? 1f : .6f);
            var tw = key ? 24f : FrontRoomsSettingsMenu.TextWidth(t.text, 20f, false) + 2f;
            // A keycap: Plex Mono 13 centred in a 24 px keyline box (the desktop kit's Set·D / Keycap).
            t.alignment = key ? TextAnchor.MiddleCenter : TextAnchor.UpperLeft;
            PlaceIn(t.rectTransform, x, key ? -2f : 0f, key ? 24f : tw + 4f, 24f);
            if (key)
            {
                var k = i == 0 ? 0 : 4;
                PlaceIn(keyEdges[k].rectTransform, x, -2f, 24f, 1f);
                PlaceIn(keyEdges[k + 1].rectTransform, x, 21f, 24f, 1f);
                PlaceIn(keyEdges[k + 2].rectTransform, x, -2f, 1f, 24f);
                PlaceIn(keyEdges[k + 3].rectTransform, x + 23f, -2f, 1f, 24f);
            }
            var u = tabRules[i];
            PlaceIn(u.rectTransform, x, 28f, tw, 2f);
            u.color = key ? Color.clear : new Color(Paper.r, Paper.g, Paper.b, selected ? 1f : 0f);
            x += tw + (key ? 24f : 40f);
        }
    }

    void RebuildRows()
    {
        foreach (var v in views) if (v != null) Destroy(v.Rect.gameObject);
        views.Clear();
        var y = 8f;
        var w = listContent.sizeDelta.x;
        foreach (var r in rows)
        {
            var v = new RowView { Y = y };
            v.Rect = Node(listContent, "row · " + r.Label);
            if (r.Kind == Kind.Header)
            {
                v.H = 32f;
                PlaceIn(v.Rect, 0f, y, w, v.H);
                v.Label = Txt(v.Rect, r.Label, mono, 13, new Color(Paper.r, Paper.g, Paper.b, .75f), TextAnchor.MiddleLeft);
                PlaceIn(v.Label.rectTransform, 24f, 8f, 400f, 20f);
                views.Add(v);
                y += v.H;
                continue;
            }
            v.H = Mx.Row;
            PlaceIn(v.Rect, 0f, y, w, v.H);
            v.Label = Txt(v.Rect, r.Label, bayon, 20, Paper, TextAnchor.MiddleLeft);
            PlaceIn(v.Label.rectTransform, 24f, 0f, 480f, v.H);
            v.Divider = Img(v.Rect, Paper, .08f);
            PlaceIn(v.Divider.rectTransform, 0f, v.H - 1f, w, 1f);
            v.Changed = Img(v.Rect, Paper, 0f);
            BuildControl(v, r, w);
            views.Add(v);
            y += v.H;
        }
        contentH = y + 8f;
        listContent.sizeDelta = new Vector2(w, contentH);
    }

    void BuildControl(RowView v, Row r, float w)
    {
        var cw = FrontRoomsSettingsMenu.ControlWidth(r, Mx);
        var x0 = w - cw;
        var mid = Mx.Row * .5f;
        switch (r.Kind)
        {
            case Kind.Switch:
            {
                v.Value = Txt(v.Rect, "", bayon, 20, Paper, TextAnchor.MiddleRight);
                PlaceIn(v.Value.rectTransform, x0 - 40f, 0f, cw - Mx.SwitchW - Mx.WordGap + 40f, Mx.Row);
                Keyline(v, w - Mx.SwitchW, mid - Mx.SwitchH * .5f, Mx.SwitchW, Mx.SwitchH, .5f);
                v.Knob = Img(v.Rect, Paper, 1f);
                break;
            }
            case Kind.Segments:
            case Kind.Action:
            {
                var x = x0;
                var n = r.Kind == Kind.Action ? 1 : r.Options.Length;
                for (var i = 0; i < n; i++)
                {
                    var cell = r.Kind == Kind.Action ? cw : FrontRoomsSettingsMenu.CellWidth(r, i, Mx);
                    var fill = Img(v.Rect, Paper, 0f);
                    PlaceIn(fill.rectTransform, x, mid - Mx.SegH * .5f, cell, Mx.SegH);
                    v.Cells.Add(fill);
                    var t = Txt(v.Rect, r.Kind == Kind.Action ? r.ValueText : r.Options[i], r.Mono ? mono : bayon, 20, Paper, TextAnchor.MiddleCenter);
                    PlaceIn(t.rectTransform, x, mid - Mx.SegH * .5f, cell, Mx.SegH);
                    v.CellTexts.Add(t);
                    if (i > 0) { var d = Img(v.Rect, Paper, .5f); PlaceIn(d.rectTransform, x, mid - Mx.SegH * .5f, 1f, Mx.SegH); v.Bits.Add(d); }
                    x += cell;
                }
                Keyline(v, x0, mid - Mx.SegH * .5f, cw, Mx.SegH, .5f);
                break;
            }
            case Kind.Slider:
            {
                FrontRoomsSettingsMenu.TrackSpan(Mx, out var low, out var high);
                var tx = w - low;
                var track = Img(v.Rect, Paper, .35f);
                PlaceIn(track.rectTransform, tx, mid, Mx.Track, 1f);
                v.Bits.Add(track);
                for (var i = 0; i < r.Stops.Length; i++)
                {
                    var tick = Img(v.Rect, Paper, .35f);
                    var tall = i == r.DefaultIndex ? 12f : 6f;
                    PlaceIn(tick.rectTransform, tx + Mathf.Round(Mx.Track * FrontRoomsSettingsMenu.StopPosition(r, i)), mid - tall * .5f, 1f, tall);
                    v.Bits.Add(tick);
                }
                v.Fill = Img(v.Rect, Paper, 1f);
                v.Thumb = Img(v.Rect, Paper, 1f);
                v.Value = Txt(v.Rect, "", mono, 20, Paper, TextAnchor.MiddleRight);
                PlaceIn(v.Value.rectTransform, w - Mx.ValueW - 8f, 0f, Mx.ValueW + 8f, Mx.Row);
                break;
            }
            case Kind.Page:
            {
                // The keys the page shows as the kit's keycaps (Plex Mono 13 in a 1 px keyline), then ›: Enter opens it.
                var arrow = Txt(v.Rect, "›", mono, 13, new Color(Paper.r, Paper.g, Paper.b, .8f), TextAnchor.MiddleRight);
                PlaceIn(arrow.rectTransform, w - 30f, 0f, 28f, Mx.Row);
                var right = w - 2f - 8f - 12f;
                var tokens = r.Options[0].Split('|');
                pageCaps.Clear();
                for (var i = tokens.Length - 1; i >= 0; i--)
                {
                    var capW = FrontRoomsSettingsMenu.TextWidth(tokens[i], 13f, true) + 12f;
                    var x = right - capW;
                    var cap = Txt(v.Rect, tokens[i], mono, 13, new Color(Paper.r, Paper.g, Paper.b, .8f), TextAnchor.MiddleCenter);
                    PlaceIn(cap.rectTransform, x, mid - 12f, capW, 24f);
                    Keyline(v, x, mid - 12f, capW, 24f, .5f);
                    if (i < PreviewBinds.Length) pageCaps[FrontRoomsInput.Normalise(FrontRoomsInput.PathOf(PreviewBinds[i]))] = new Rect(x, v.Y + mid - 12f, capW, 24f);
                    right = x - 6f;
                }
                break;
            }
            case Kind.Keys:
            {
                // A key's name, read only, set like a value (IBM Plex Mono 20).
                v.Value = Txt(v.Rect, r.ValueText, mono, 20, Paper, TextAnchor.MiddleRight);
                PlaceIn(v.Value.rectTransform, w - 320f, 0f, 320f - 8f, Mx.Row);
                break;
            }
            case Kind.Meter:
            {
                v.Value = Txt(v.Rect, "", mono, 13, Paper, TextAnchor.MiddleRight);
                PlaceIn(v.Value.rectTransform, w - 420f, 0f, 420f - 10f * 19f - 16f, Mx.Row);
                for (var i = 0; i < 10; i++)
                {
                    var d = Img(v.Rect, Paper, .2f);
                    PlaceIn(d.rectTransform, w - 10f * 19f + i * 19f + 3f, mid - 2f, 16f, 4f);
                    v.Bits.Add(d);
                }
                break;
            }
        }
    }

    void Keyline(RowView v, float x, float y, float w, float h, float alpha)
    {
        foreach (var r in new[] { new Rect(x, y, w, 1f), new Rect(x, y + h - 1f, w, 1f), new Rect(x, y, 1f, h), new Rect(x + w - 1f, y, 1f, h) })
        {
            var i = Img(v.Rect, Paper, alpha);
            PlaceIn(i.rectTransform, r.x, r.y, r.width, r.height);
        }
    }

    void UpdateRows()
    {
        listContent.anchoredPosition = new Vector2(0f, scroll);
        for (var i = 0; i < rows.Count && i < views.Count; i++)
        {
            var r = rows[i];
            var v = views[i];
            if (r.Kind == Kind.Header) continue;
            var focus = i == Focus;
            v.Label.color = new Color(Paper.r, Paper.g, Paper.b, focus ? 1f : .9f);
            var w = listContent.sizeDelta.x;
            var cw = FrontRoomsSettingsMenu.ControlWidth(r, Mx);
            // A changed row says so by shape (a dot before its control), not by colour (R3 rule 11).
            v.Changed.color = new Color(Paper.r, Paper.g, Paper.b, r.Changed ? .8f : 0f);
            PlaceIn(v.Changed.rectTransform, w - cw - 18f, Mx.Row * .5f - 3f, 6f, 6f);
            switch (r.Kind)
            {
                case Kind.Switch:
                {
                    var on = r.Index == 1;
                    v.Value.text = on ? "ON" : "OFF";
                    v.Value.color = new Color(Paper.r, Paper.g, Paper.b, on ? 1f : .7f);
                    var inset = (Mx.SwitchH - Mx.Knob) * .5f;
                    PlaceIn(v.Knob.rectTransform, on ? w - Mx.Knob - inset : w - Mx.SwitchW + inset, Mx.Row * .5f - Mx.Knob * .5f, Mx.Knob, Mx.Knob);
                    v.Knob.color = new Color(Paper.r, Paper.g, Paper.b, on ? 1f : .3f);
                    break;
                }
                case Kind.Segments:
                    for (var c = 0; c < v.Cells.Count; c++)
                    {
                        var sel = c == r.Index;
                        v.Cells[c].color = new Color(Paper.r, Paper.g, Paper.b, sel ? 1f : 0f);
                        v.CellTexts[c].color = sel ? Ink : new Color(Paper.r, Paper.g, Paper.b, .75f);
                    }
                    break;
                case Kind.Action:
                    v.CellTexts[0].text = r.ValueText;
                    v.Cells[0].color = FrontRoomsSettingsMenu.ResetArmed ? Accent : new Color(0f, 0f, 0f, 0f);
                    v.CellTexts[0].color = FrontRoomsSettingsMenu.ResetArmed ? Ink : Paper;
                    break;
                case Kind.Slider:
                {
                    FrontRoomsSettingsMenu.TrackSpan(Mx, out var low, out _);
                    var tx = w - low;
                    var px = Mathf.Round(Mx.Track * FrontRoomsSettingsMenu.StopPosition(r, r.Index));
                    PlaceIn(v.Fill.rectTransform, tx, Mx.Row * .5f - 1f, Mathf.Max(0f, px), 3f);
                    PlaceIn(v.Thumb.rectTransform, tx + Mathf.Clamp(px - Mx.ThumbW * .5f, 0f, Mx.Track - Mx.ThumbW), Mx.Row * .5f - Mx.ThumbH * .5f, Mx.ThumbW, Mx.ThumbH);
                    v.Value.text = r.ValueText;
                    break;
                }
                case Kind.Meter: UpdateMeter(v); break;
            }
        }
        var f = Focus >= 0 && Focus < views.Count ? views[Focus] : null;
        focusBar.enabled = f != null;
        if (f != null) { PlaceIn(focusBar.rectTransform, 0f, f.Y, 4f, f.H); focusBar.transform.SetAsLastSibling(); }
    }

    void UpdateMeter(RowView v)
    {
        var fps = FrontRoomsQuality.MeasuredFps;
        var target = Mathf.Max(1, FrontRoomsQuality.TargetFps);
        v.Value.text = Mathf.RoundToInt(fps) + " FPS  ·  " + (1000f / Mathf.Max(1f, fps)).ToString("0.0") + " MS";
        var lit = Mathf.Clamp(Mathf.RoundToInt(10f * fps / target), 0, 10);
        for (var i = 0; i < v.Bits.Count; i++) v.Bits[i].color = new Color(Paper.r, Paper.g, Paper.b, i < lit ? 1f : .2f);
    }

    void DrawDetail()
    {
        var row = Focus >= 0 && Focus < rows.Count ? rows[Focus] : null;
        detailTitle.text = row != null ? row.Label : "";
        detailBody.text = row != null ? row.Explain ?? "" : "";
        var meta = "";
        if (row != null && row.Kind == Kind.Page) meta = "ENTER  OPEN";
        else if (row != null && row.Kind != Kind.Action && row.Kind != Kind.Meter)
        {
            meta = "DEFAULT        " + (row.Key == FrontRoomsSettingsMenu.Key.Quality ? row.DefaultText + " ON THIS COMPUTER" : row.DefaultText);
            if (row.Cost > 0) meta += "\nFRAME COST     " + (row.Cost >= 3 ? "HIGH: CHANGE THIS FIRST" : "LOW");
            if (row.Changed) meta += "\nBACKSPACE      BACK TO DEFAULT";
        }
        detailMeta.text = meta;
        var graphics = CurrentTab == Tab.Graphics;
        preview.gameObject.SetActive(graphics);
        preview2Frame.gameObject.SetActive(graphics);
        previewCaption.gameObject.SetActive(graphics);
        if (!graphics) ReleasePreview();
    }

    void RenderPreview()
    {
        if (Peeking || CurrentTab != Tab.Graphics || cam == null || !cam.isActiveAndEnabled) return;
        var canvas = root.GetComponentInParent<Canvas>();
        var scale = canvas != null ? canvas.scaleFactor : 1f;
        var w = Mathf.Clamp(Mathf.RoundToInt(DetailW * scale), 64, 1280);
        var h = Mathf.Clamp(Mathf.RoundToInt(w * PreviewH / DetailW), 36, 720);
        var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
        var msaa = urp != null ? Mathf.Max(1, urp.msaaSampleCount) : 1;
        if (previewRt == null || previewRt.width != w || previewRt.height != h || previewRt.antiAliasing != msaa)
        {
            ReleasePreview();
            previewRt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { name = "Settings preview", antiAliasing = msaa };
            previewRt.Create();
            preview.texture = previewRt;
        }
        // The game camera alone (no HUD), at the current settings: render scale, MSAA, shadows, AO, bloom, HDR.
        var previous = cam.targetTexture;
        var aspect = cam.aspect;
        try
        {
            cam.targetTexture = previewRt;
            cam.aspect = w / (float)h;
            cam.Render();
        }
        finally
        {
            cam.targetTexture = previous;
            cam.aspect = aspect;
            cam.ResetAspect();
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

    /// <summary>The paused frame, frosted like the handheld sheet's: the game camera at half size, halved to 1/16 and back
    /// up to 1/4 with bilinear steps. Once per opening.</summary>
    void CaptureFrost()
    {
        ReleaseFrost();
        if (cam == null || !cam.isActiveAndEnabled) { frost.enabled = false; veil.color = new Color(Ink.r, Ink.g, Ink.b, 1f); return; }
        var w = Mathf.Max(64, Screen.width / 2);
        var h = Mathf.Max(32, Screen.height / 2);
        var src = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var previous = cam.targetTexture;
        try { cam.targetTexture = src; cam.Render(); }
        finally { cam.targetTexture = previous; }
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
        frostRt = src;
        frost.texture = frostRt;
        frost.enabled = true;
        veil.color = new Color(Ink.r, Ink.g, Ink.b, .86f);
    }

    void ReleaseFrost()
    {
        if (frost != null) frost.texture = null;
        if (frostRt == null) return;
        RenderTexture.ReleaseTemporary(frostRt);
        frostRt = null;
    }

    void OnDestroy()
    {
        ReleasePreview();
        ReleaseFrost();
        if (peekLayer != null) Destroy(peekLayer.gameObject);
    }

    // ------------------------------------------------------------- pointer

    Vector2 Local(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, e.position, e.pressEventCamera, out var p);
        // Top-left origin, y down, in canvas units.
        return new Vector2(p.x - root.rect.xMin, root.rect.yMax - p.y);
    }

    bool InRect(RectTransform rt, PointerEventData e)
    {
        return rt != null && rt.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rt, e.position, e.pressEventCamera);
    }

    int RowAt(PointerEventData e, out float fromRight)
    {
        fromRight = 0f;
        if (!InRect(listMask, e)) return -1;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(listContent, e.position, e.pressEventCamera, out var p);
        // listContent's pivot is its top-left: x from the left, y down from the top.
        var x = p.x + listContent.pivot.x * listContent.rect.width;
        var y = -(p.y - (1f - listContent.pivot.y) * listContent.rect.height);
        for (var i = 0; i < views.Count; i++)
        {
            var v = views[i];
            if (y >= v.Y && y < v.Y + v.H) { fromRight = listContent.sizeDelta.x - x; return Selectable(rows[i]) ? i : -1; }
        }
        return -1;
    }

    /// <summary>A pointer on the map, in its own units (top-left origin, y down).</summary>
    Vector2 MapPoint(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(keyMap.Root, e.position, e.pressEventCamera, out var p);
        return new Vector2(p.x, -p.y);
    }

    public void OnPointerMove(PointerEventData e)
    {
        if (!open || Peeking || dragRow >= 0) return;
        if (pageOpen) { if (keyMap.FocusAt(MapPoint(e))) DrawPageDetail(); return; }
        var i = RowAt(e, out _);
        if (i >= 0 && i != Focus) { Focus = i; Refresh(); }
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!open) return;
        if (Peeking) { SetPeek(false); return; }
        if (pageOpen && keyMap.Capturing) return;
        if (pageOpen && InRect(pageBack.rectTransform, e)) { ClosePage(); return; }
        if (pageOpen && InRect(pageDefaults.rectTransform, e)) { PressDefaults(); return; }
        if (InRect(back.rectTransform, e)) { FrontRoomsDesktopSettingsHost.RequestClose?.Invoke(); return; }
        if (InRect(restart.rectTransform, e)) { FrontRoomsDesktopSettingsHost.RequestRestart?.Invoke(); return; }
        if (CurrentTab == Tab.Graphics && InRect(preview.rectTransform, e)) { SetPeek(true); return; }
        for (var i = 0; i < FrontRoomsSettingsMenu.Tabs.Length; i++)
            if (InRect(tabTexts[i + 1].rectTransform, e) || InRect(tabRules[i + 1].rectTransform, e)) { SetTab(i); return; }
        if (InRect(tabTexts[0].rectTransform, e)) { SetTab((int)CurrentTab - 1); return; }
        if (InRect(tabTexts[tabTexts.Count - 1].rectTransform, e)) { SetTab((int)CurrentTab + 1); return; }
        if (pageOpen)
        {
            var at = keyMap.ActionAt(MapPoint(e));
            if (at >= 0 && at == keyMap.Focus) keyMap.BeginCapture();
            else keyMap.FocusAt(MapPoint(e));
            DrawPageDetail();
            return;
        }
        var row = RowAt(e, out var fromRight);
        if (row < 0) return;
        Focus = row;
        var r = rows[row];
        switch (r.Kind)
        {
            case Kind.Page: OpenPage(); return;
            case Kind.Switch: FrontRoomsSettingsMenu.Activate(r); break;
            case Kind.Action: FrontRoomsSettingsMenu.Activate(r); break;
            case Kind.Segments:
            {
                var seg = FrontRoomsSettingsMenu.SegmentAt(r, fromRight, Mx);
                if (seg >= 0 && seg != r.Index) FrontRoomsSettingsMenu.Set(r, seg);
                break;
            }
            case Kind.Slider:
            {
                var stop = FrontRoomsSettingsMenu.StopAt(r, fromRight, Mx, 12f);
                if (stop >= 0) { dragRow = row; if (stop != r.Index) FrontRoomsSettingsMenu.Set(r, stop); }
                break;
            }
        }
        Refresh();
    }

    public void OnDrag(PointerEventData e)
    {
        if (dragRow < 0 || rows == null || dragRow >= rows.Count) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(listContent, e.position, e.pressEventCamera, out var p);
        var x = p.x + listContent.pivot.x * listContent.rect.width;
        var r = rows[dragRow];
        var stop = FrontRoomsSettingsMenu.StopAt(r, listContent.sizeDelta.x - x, Mx, 10000f);
        if (stop >= 0 && stop != r.Index) { FrontRoomsSettingsMenu.Set(r, stop); Refresh(); }
    }

    public void OnPointerUp(PointerEventData e) => dragRow = -1;

    public void OnScroll(PointerEventData e)
    {
        if (!open || Peeking || pageOpen) return;
        scroll = Mathf.Clamp(scroll - e.scrollDelta.y * 48f, 0f, Mathf.Max(0f, contentH - listMask.rect.height));
        listContent.anchoredPosition = new Vector2(0f, scroll);
    }

    // ------------------------------------------------------------- helpers

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

    static void PlaceIn(RectTransform rt, float x, float y, float w, float h) => Place(rt, x, y, w, h);

    Image Img(Transform parent, Color c, float alpha)
    {
        var i = new GameObject("img", typeof(RectTransform)).AddComponent<Image>();
        i.transform.SetParent(parent, false);
        i.color = new Color(c.r, c.g, c.b, alpha);
        i.raycastTarget = false;
        return i;
    }

    Text Txt(Transform parent, string value, Font font, int size, Color color, TextAnchor anchor)
    {
        var t = new GameObject("text", typeof(RectTransform)).AddComponent<Text>();
        t.transform.SetParent(parent, false);
        t.font = font; t.fontSize = size; t.color = color; t.alignment = anchor; t.text = value;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false; t.supportRichText = false;
        return t;
    }
}

/// <summary>What the desktop sheet asks of the game (closing goes through the game's own pause logic).</summary>
public static class FrontRoomsDesktopSettingsHost
{
    /// <summary>RESUME or Esc: the game resumes the run (the desktop pause is this page).</summary>
    public static System.Action RequestClose;
    /// <summary>END RUN (or R): the game ends the run and goes straight to the title (Red, 2026-10-30: no question).</summary>
    public static System.Action RequestRestart;
    /// <summary>The old restart question's state. The sheet no longer reads it (END RUN asks nothing); kept so the game's
    /// hook still compiles.</summary>
    public static System.Func<bool> RestartAsking;
}
