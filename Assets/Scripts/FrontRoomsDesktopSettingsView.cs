using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Tab = FrontRoomsSettingsMenu.Tab;
using Kind = FrontRoomsSettingsMenu.Kind;
using Row = FrontRoomsSettingsMenu.Row;
using PadButton = FrontRoomsInput.PadButton;

/// <summary>
/// The desktop settings sheet (Mac, Windows), drawn over the pause card. Figma: section 3688:7842, S1–S3 (1920 × 1080,
/// 12 columns, 72 px margin): SETTINGS, a tab row (Q / E), the list in columns 1–7 and pictures only in columns 9–12: on
/// GRAPHICS a live preview of the game at the current settings. A chosen row opens in place to twice its height and says
/// what its choice means in its own second line, and a map row unfolds its graph under it (SETTINGS_MOTION.md, Red 2026-11-01).
/// Clicking the preview (or SPACE)
/// leaves the sheet to show the game full screen; a click (or SPACE, ESC) brings it back (Red, 2026-10-24).
/// A pad works it too, with the touch sheet's buttons (2026-10-31: HandleKeys), and its words follow the device in use.
/// Rows and values come from FrontRoomsSettingsMenu; faces and sizes are the desktop masters' (Bayon 20, IBM Plex Mono
/// 13, Source Serif 24, at tracking 0).
/// </summary>
public sealed class FrontRoomsDesktopSettingsView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IPointerMoveHandler, IScrollHandler
{
    static readonly Color Paper = new Color(.957f, .945f, .91f, 1f);
    static readonly Color Ink = new Color(.055f, .055f, .051f, 1f);
    static readonly Color Accent = new Color(.957f, .875f, .231f, 1f);
    const float M = 72f, ListTop = 256f, DetailW = 576f, PreviewH = 324f;
    // A row opening in place (SETTINGS_MOTION.md; the constants are W/settings_motion/mock.py's): 1x -> 2x in 0.24 s, ease-out
    // quart, led by its bar; its second line from 0.09 s, each element in 0.18 s, 0.035 s after the one before; a closing row's
    // second line fades in 0.09 s, then the row folds in 0.18 s, ease-in-out cubic; the list follows in 0.32 s; the name grows
    // 1.4x. Reduced motion: heights jump and the second line fades in 0.12 s.
    const float OpenS = .24f, CloseFadeS = .09f, CloseS = .18f, RevealAt = .09f, RevealS = .18f, Stagger = .035f, ScrollS = .32f,
        TitleGrow = .4f, ReducedFadeS = .12f;
    // A map row unfolding: in 0.26 s + 1 s per 1500 units, at most 0.52 s, ease-out quart; folding, the graph fades in 0.12 s, then
    // the row folds in 0.22 s, ease-in-out cubic. The graph is 1.45x the 10-29 page at most (a 58 px key pitch), and fits the list.
    const float UnfoldMin = .26f, UnfoldMax = .52f, UnfoldPerUnit = 1f / 1500f, FoldFadeS = .12f, FoldS = .22f, MapMaxScale = 1.45f, MapMinScale = .6f, MapPad = 8f;
    // The second line, centres from the row's top: Source Serif 18 (two lines at a 1.24 em pitch), Plex Mono 12 notes, the
    // default's rule, the default. The cost column sits 462 px left of the row's end (columns 1-7 at 1920).
    const int SentenceSize = 18, NoteSize = 12, OpenLabelSize = 28;
    const float SentenceY = 68.6f, NoteY = 63.2f, RuleY = 71.7f, DefaultY = 83.5f, SentencePitch = .905f, CostFromRight = 462f;
    static FrontRoomsSettingsMenu.Metrics Mx => FrontRoomsSettingsMenu.Desktop;

    Font bayon, mono, serif;
    Camera cam;
    RectTransform root, listMask, listContent, detail, peekLayer, tabsRoot;
    CanvasGroup overlayGroup;
    Image sheet, preview2Frame, veil;
    RawImage preview, frost;
    RenderTexture previewRt, frostRt;
    readonly List<Image> keyEdges = new List<Image>();
    Text title, previewCaption, frameText, peekText, peekHint, hints, backText, restartText;
    Image back, restart;
    // CONTROLS' maps (Red, 2026-10-29), which unfold under their rows in the list: KEYBOARD & MOUSE, and its BACK.
    FrontRoomsKeyMapView keyMap;
    // The CONTROLLER map beside it (2026-10-31), made on its row's first open (null until then); page: the map open now,
    // or the last one open.
    FrontRoomsKeyMapView padMap, page;
    Text pageBackText;
    Image pageBack, pageDefaults;
    Text pageDefaultsText;
    bool pageOpen;
    // ASSIST's first row (Red, 2026-10-30). It outlives the rows, which are rebuilt when a pick changes their words.
    FrontRoomsLanguageStrip languageStrip;
    // LANGUAGE unfolded into its grid (SETTINGS_MOTION.md, E3), two lines of 60 under the row; the right column previews the
    // game's words in the language under the pointer.
    bool langOpen;
    FrontRoomsLanguagePreview languagePreview;
    const float LangCellH = 60f, LangGridH = 2f * LangCellH + 14f;
    float defaultsArmedUntil = -1f;
    // The corner's buttons with keys or a pad (2026-10-31): ↓ past the last row focuses RESUME, ← END RUN, ↑ the rows again.
    enum Corner { None, EndRun, Resume }
    Corner corner;
    // What the words were last drawn for: a pad in use, which pad, a PlayStation pad, a pad in, a pad's notice and its moment
    // (Update).
    bool padShown, playStationShown, connectedShown, noticeShown;
    float noticeAt = float.NegativeInfinity;
    UnityEngine.InputSystem.Gamepad padDevice;
    // The gap between two hints of a line.
    const string Gap = "     ";
    string PageHints
    {
        get
        {
            // A pad in use names its buttons (2026-10-31); the CONTROLLER page listens for a button.
            var pad = FrontRoomsInput.PadInUse;
            if (page.State == FrontRoomsKeyMapView.CaptureState.Listening)
                return page.IsPad ? FrontRoomsLanguage.T("PRESS A BUTTON") + Gap + FrontRoomsLanguage.F("{0}  CANCEL", pad ? FrontRoomsInput.PadName(PadButton.Menu) : "ESC")
                    : FrontRoomsLanguage.T("PRESS A KEY OR A MOUSE BUTTON     ESC  CANCEL");
            if (page.State == FrontRoomsKeyMapView.CaptureState.Asking)
                return pad ? FrontRoomsLanguage.F("{0}  SWAP", FrontRoomsInput.PadName(PadButton.South)) + Gap + FrontRoomsLanguage.F("{0}  CANCEL", FrontRoomsInput.PadName(PadButton.East))
                    : FrontRoomsLanguage.T("ENTER  SWAP     ESC  CANCEL");
            if (pad)
                return FrontRoomsLanguage.F("{0}  CHOOSE", "↑ ↓ ← →") + Gap + FrontRoomsLanguage.F("{0}  CHANGE", FrontRoomsInput.PadName(PadButton.South)) + Gap
                    + FrontRoomsLanguage.F("{0}  DEFAULT", FrontRoomsInput.PadName(PadButton.West)) + Gap + FrontRoomsLanguage.F("{0}  ALL DEFAULTS", FrontRoomsInput.PadName(PadButton.North))
                    + Gap + FrontRoomsLanguage.F("{0}  BACK", FrontRoomsInput.PadName(PadButton.East));
            return FrontRoomsLanguage.T("↑ ↓ ← →  CHOOSE     ENTER  CHANGE     BACKSPACE  DEFAULT     DELETE  ALL DEFAULTS     ESC  BACK");
        }
    }

    // The pause is this page on the desktop (Red, 2026-10-24): Esc resumes; R ends the run and goes to the title at once
    // (Red, 2026-10-30: no question before the title).
    const string HintKeys = "↑ ↓  CHOOSE     ← →  CHANGE     Q E  GROUP     BACKSPACE  DEFAULT     SPACE  PREVIEW";
    static string HintText()
    {
        // A pad in use (2026-10-31): its buttons, as the touch sheet takes them; END RUN is in the corner (↓ past the last row).
        if (FrontRoomsInput.PadInUse)
            return FrontRoomsLanguage.F("{0}  CHOOSE", "↑ ↓") + Gap + FrontRoomsLanguage.F("{0}  CHANGE", "← →") + Gap
                + FrontRoomsLanguage.F("{0}  GROUP", FrontRoomsInput.PadName(PadButton.LeftShoulder) + " " + FrontRoomsInput.PadName(PadButton.RightShoulder)) + Gap
                + FrontRoomsLanguage.F("{0}  DEFAULT", FrontRoomsInput.PadName(PadButton.West)) + Gap + FrontRoomsLanguage.F("{0}  PREVIEW", FrontRoomsInput.PadName(PadButton.North))
                + Gap + FrontRoomsLanguage.F("{0}  RESUME", FrontRoomsInput.PadName(PadButton.East));
        var r = FrontRoomsInput.KeyName(FrontRoomsInput.PathOf(FrontRoomsInput.Bind.Restart));
        return FrontRoomsLanguage.T(HintKeys) + "     " + FrontRoomsLanguage.F("{0}  END RUN", r) + "     " + FrontRoomsLanguage.T("ESC  RESUME");
    }
    readonly List<Image> frameDashes = new List<Image>(), peekDashes = new List<Image>();
    // The frame rate in the picture's bottom-right corner (Red, 2026-11-01), shown with the picture: its word, its reading.
    RectTransform frameOsd;
    Text frameLabel;
    float pictureH = -1f, frameReadW;
    // Page motion (Red, 2026-11-01): the list fades out as a map comes in and back as it leaves; the detail column, the corner
    // buttons and the hint line fade up with their new words; a map going back is animated until it has hidden itself.
    CanvasGroup listGroup, detailGroup;
    readonly List<CanvasGroup> cornerGroups = new List<CanvasGroup>();
    readonly FrontRoomsTouchMotion.Value listFade = new FrontRoomsTouchMotion.Value(1f), detailFade = new FrontRoomsTouchMotion.Value(1f),
        cornerFade = new FrontRoomsTouchMotion.Value(1f);
    FrontRoomsKeyMapView closingPage;
    // What the open map was last fitted to (FitMap).
    FrontRoomsKeyMapView mapFitPage;
    float mapFitView, mapFitW;
    Text peekFrameText;
    readonly List<Text> tabTexts = new List<Text>();
    readonly List<Image> tabRules = new List<Image>();
    readonly List<RowView> views = new List<RowView>();
    List<Row> rows;
    string signature;
    float scroll, contentH;
    int dragRow = -1;
    bool open;
    // The open row (SETTINGS_MOTION.md), -1 none, and its key, so a rebuild keeps it open. Armed: the next ↑ ↓ opens the focused
    // row where it is (a fresh sheet or tab); after that ↑ ↓ move and open. The row under the pointer only reads brighter.
    int openRow = -1, hoverRow = -1;
    FrontRoomsSettingsMenu.Key openKey;
    bool armed;
    // The scroll follows the open row in 0.32 s, a wheel moves it at once; the rows are laid out every frame until animateUntil.
    float scrollFrom, scrollTo, scrollAt = -100f, animateUntil;

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
        // A Page row's scheme word (ACTIVE, CONNECTED, 2026-11-01) and where its keycaps start.
        public Text Tag;
        public float TagLimit;
        // SETTINGS_MOTION.md: its own bar (no bar travels) and underlay; its name at the open size, drawn while the row is open or
        // moving (Label rests closed, so both rests are drawn at their own size); the second line: the sentence, a note under each
        // choice (the option or stop it belongs to; -1, a switch's current state), a rule under the default's note, the default.
        public Image Bar, Underlay, Rule;
        public Text LabelOpen, Sentence, Default;
        public readonly List<Text> Notes = new List<Text>();
        public readonly List<int> NoteOf = new List<int>();
        public int RuleSlot;
        public bool Opens, IsOpen;
        public float OpenAt = -100f, CloseAt = -100f, OpenFrom, CloseFrom, OpenH, RestH;
        // A map row: its › turning to ⌄, its keycaps (gone while it is unfolded), how far it is unfolded and how tall its graph is.
        public RectTransform Chevron;
        public CanvasGroup Caps;
        public bool Unfolds, IsUnfolded;
        public float UnfoldAt = -100f, FoldAt = -100f, UnfoldFrom, FoldFrom, FoldH;
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
        listGroup = listMask.gameObject.AddComponent<CanvasGroup>();
        listContent = Node(listMask, "Rows");
        Place(listContent, 0f, 0f, 1026f, 10f);
        detail = Node(root, "Detail");
        detail.anchorMin = detail.anchorMax = new Vector2(1f, 1f); detail.pivot = new Vector2(0f, 1f);
        detail.sizeDelta = new Vector2(DetailW, 760f);
        detailGroup = detail.gameObject.AddComponent<CanvasGroup>();
        // Pictures only (SETTINGS_MOTION.md): the GRAPHICS picture, and LANGUAGE's preview of the game's words.
        languagePreview = FrontRoomsLanguagePreview.Create(detail, serif, mono);
        languagePreview.SetActive(false);
        // The live preview (GRAPHICS): the game camera at the current settings; a click shows it full screen.
        preview2Frame = Img(detail, Paper, .35f);
        preview = new GameObject("Preview", typeof(RectTransform)).AddComponent<RawImage>();
        preview.transform.SetParent(detail, false);
        preview.raycastTarget = false;
        previewCaption = Txt(detail, "CLICK THE PICTURE OR PRESS SPACE TO SEE IT FULL SCREEN", mono, 13, new Color(Paper.r, Paper.g, Paper.b, .75f), TextAnchor.UpperLeft);
        // FRAME RATE NOW lives in the picture's bottom-right corner (Red, 2026-11-01: as a list row and again under the picture
        // it said the same thing twice, and the line under the picture met END RUN): the word and the reading over the dashes,
        // right-aligned, keylined like the HUD so it reads on the lit floor (PlacePicture).
        frameOsd = Node(detail, "Frame rate (in the picture)");
        frameLabel = Txt(frameOsd, "FRAME RATE NOW", mono, 13, new Color(Paper.r, Paper.g, Paper.b, .75f), TextAnchor.LowerRight);
        Keyline(frameLabel);
        frameText = Txt(frameOsd, "", mono, 13, Paper, TextAnchor.LowerRight);
        Keyline(frameText);
        for (var i = 0; i < 10; i++)
        {
            var d = Img(frameOsd, Paper, .2f);
            Keyline(d);
            frameDashes.Add(d);
        }
        frameOsd.gameObject.SetActive(false);
        hints = Txt(root, HintText(), mono, 13, new Color(Paper.r, Paper.g, Paper.b, .75f), TextAnchor.LowerLeft);
        hints.rectTransform.anchorMin = hints.rectTransform.anchorMax = Vector2.zero; hints.rectTransform.pivot = Vector2.zero;
        hints.rectTransform.anchoredPosition = new Vector2(M, M - 8f); hints.rectTransform.sizeDelta = new Vector2(1200f, 20f);
        // RESUME (the old BACK) and END RUN: keyline buttons, paper word, no fill (yellow means focus only on a settings page;
        // 平面视觉 fix 2). The desktop pause is this page (Red, 2026-10-24), so they carry the PAUSED card's two actions; END RUN
        // (the phone's word) goes to the title at once (Red, 2026-10-30).
        back = KeylineButton("RESUME", 0f, out backText);
        restart = KeylineButton("END RUN", back.rectTransform.sizeDelta.x + 12f, out restartText);
        cornerGroups.Add(hints.gameObject.AddComponent<CanvasGroup>());
        cornerGroups.Add(back.gameObject.AddComponent<CanvasGroup>());
        cornerGroups.Add(restart.gameObject.AddComponent<CanvasGroup>());
        // The map unfolds in the list, across columns 1–12; BACK takes the corner from RESUME and END RUN while it is open
        // (Figma 3688:7842, K2).
        keyMap = FrontRoomsKeyMapView.Create(listContent, bayon, mono, Paper, Ink, Accent);
        page = keyMap;
        pageBack = KeylineButton("BACK", 0f, out pageBackText);
        pageBack.gameObject.SetActive(false);
        // DEFAULTS asks first (DEFAULTS?): a second press within 3 s puts every key back.
        pageDefaults = KeylineButton("DEFAULTS?", pageBack.rectTransform.sizeDelta.x + 12f, out pageDefaultsText);
        pageDefaultsText.text = FrontRoomsLanguage.T("DEFAULTS");
        pageDefaults.gameObject.SetActive(false);
        cornerGroups.Add(pageBack.gameObject.AddComponent<CanvasGroup>());
        cornerGroups.Add(pageDefaults.gameObject.AddComponent<CanvasGroup>());
        languageStrip = FrontRoomsLanguageStrip.Create(listContent, serif);
        languageStrip.SetActive(false);
        FrontRoomsLanguage.Changed += OnLanguageChanged;

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
            StopPageMotion();
            keyMap.SetActive(false);
            if (padMap != null) padMap.SetActive(false);
            page = keyMap;
            corner = Corner.None;
            signature = null;
            ResetOpen();
            // CONTROLS puts the scheme in hand first as the sheet opens (Red, 2026-11-01), so it has the focus there.
            FrontRoomsSettingsMenu.LatchControlsOrder();
            Focus = FirstSelectable();
            Refresh();
        }
        else
        {
            // Both maps shut (a capture ends with them) and the END RUN key reads again (2026-10-31).
            keyMap.SetActive(false);
            if (padMap != null) padMap.SetActive(false);
            FrontRoomsInput.SuspendRestart = false;
            ReleasePreview();
        }
    }

    void SetPeek(bool value)
    {
        Peeking = value;
        if (overlayGroup != null) overlayGroup.alpha = value ? 0f : 1f;
        if (peekLayer != null) peekLayer.gameObject.SetActive(value);
        if (value) DrawPeekHint();
    }

    // The full-screen preview's way back for the device in use (2026-10-31): a pad's B (A and Y return too), else the click,
    // Space or Esc.
    void DrawPeekHint()
    {
        if (peekHint != null) peekHint.text = FrontRoomsInput.PadInUse ? FrontRoomsLanguage.F("{0}  BACK", FrontRoomsInput.PadName(PadButton.East)) : FrontRoomsLanguage.T("CLICK, SPACE OR ESC TO RETURN TO SETTINGS");
    }

    void SetTab(int index)
    {
        // The new list and its column fade up (Red, 2026-11-01); a map goes with the list it is in.
        var fromPage = pageOpen;
        if (pageOpen) { pageOpen = false; page.SetActive(false); }
        if (closingPage != null) { closingPage.SetActive(false); closingPage = null; }
        var reduced = FrontRoomsHandheld.ReduceMotion;
        if (reduced) { listFade.Snap(1f); detailFade.Snap(1f); cornerFade.Snap(1f); }
        else
        {
            var delay = fromPage ? .06f : 0f;
            listFade.Restart(0f, 1f, .16f, FrontRoomsTouchMotion.Ease.OutCubic, delay);
            detailFade.Restart(0f, 1f, .16f, FrontRoomsTouchMotion.Ease.OutCubic, delay);
            if (fromPage) cornerFade.Restart(0f, 1f, .2f, FrontRoomsTouchMotion.Ease.OutCubic, delay);
        }
        corner = Corner.None;
        var n = FrontRoomsSettingsMenu.Tabs.Length;
        CurrentTab = FrontRoomsSettingsMenu.Tabs[(index % n + n) % n];
        ResetOpen();
        signature = null;
        FrontRoomsSettingsMenu.LatchControlsOrder();
        rows = ListRows();
        Focus = FirstSelectable();
        Refresh();
    }

    int FirstSelectable()
    {
        rows = ListRows();
        for (var i = 0; i < rows.Count; i++) if (Selectable(rows[i])) return i;
        return 0;
    }

    static bool Selectable(Row r) => r.Kind != Kind.Header && r.Kind != Kind.Meter && r.Kind != Kind.Keys;

    /// <summary>The keys while the sheet is open (the game keeps Esc, H and T), and a pad's menu buttons (2026-10-31: the touch
    /// sheet's, "三端保持一致"): the d-pad or the left stick, A, B, X, Y, LB / RB.</summary>
    public void HandleKeys()
    {
        if (!open) return;
        var pad = FrontRoomsInput.ReadPadMenu();
        if (Peeking)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || pad.Confirm || pad.Back || pad.Defaults) SetPeek(false);
            return;
        }
        if (pageOpen) { HandlePageKeys(pad); return; }
        rows = ListRows();
        // B goes back as Esc does: the sheet's top level resumes the run. The press is swallowed for the rest of the frame, so
        // the game, which reads this frame after the sheet, does not take it for the pad's BACK out of a shot (2026-10-31).
        if (pad.Back) { if (!ConsumeEscape()) { FrontRoomsInput.ConsumeFrame(); FrontRoomsDesktopSettingsHost.RequestClose?.Invoke(); } return; }
        var tab = Input.GetKeyDown(KeyCode.Q) ? -1 : Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Tab) ? 1 : pad.Tab;
        if (tab != 0) { SetTab((int)CurrentTab + tab); return; }
        var move = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) ? -1 : Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) ? 1
            : pad.Dir.y > 0 ? -1 : pad.Dir.y < 0 ? 1 : 0;
        var step = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) ? -1 : Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) ? 1 : pad.Dir.x;
        var press = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || pad.Confirm;
        // The corner (RESUME, END RUN) has the focus: only ↑, ← → and a press reach it; X, Y and Space do nothing.
        if (corner != Corner.None) { HandleCorner(move, step, press); return; }
        // LANGUAGE unfolded: ← → and ↑ ↓ go through its grid, ↑ ↓ past its edge leave it, Enter picks (SETTINGS_MOTION.md, E3).
        if (langOpen && Focus >= 0 && Focus < rows.Count && rows[Focus].Kind == Kind.Language)
        {
            if (move != 0) { if (!languageStrip.StepRow(move)) { CloseLanguage(); MoveFocus(move); } return; }
            if (step != 0) { languageStrip.Step(step); return; }
            if (press || Input.GetKeyDown(KeyCode.Space)) { languageStrip.PickHighlighted(); return; }
        }
        if (move != 0) { MoveFocus(move); return; }
        if ((Input.GetKeyDown(KeyCode.Space) || pad.Defaults) && CurrentTab == Tab.Graphics) { SetPeek(true); return; }
        var row = Focus >= 0 && Focus < rows.Count ? rows[Focus] : null;
        if (row == null) return;
        var enter = press || Input.GetKeyDown(KeyCode.Space);
        if (row.Kind == Kind.Page && (enter || step > 0)) { OpenPage(); return; }
        if (row.Kind == Kind.Language)
        {
            // Folded: ← → go along the rail and change nothing; Enter, Space or A picks the charm chosen there, or on the current
            // one unfolds the grid.
            if (step != 0) languageStrip.Step(step);
            else if (enter && languageStrip.HighlightedLanguage != FrontRoomsLanguage.Current) languageStrip.PickHighlighted();
            else if (enter) OpenLanguage();
            return;
        }
        // A on a GRAPHICS row with a picture previews it, as the touch sheet's A does (Enter keeps doing nothing there).
        if (pad.Confirm && row.Visual && row.Kind != Kind.Switch) { SetPeek(true); return; }
        // A row ESC closed opens again before Enter works its control.
        if (enter && Opens(row) && openRow != Focus) { armed = false; SetOpenRow(Focus); Refresh(); return; }
        if (step != 0) { FrontRoomsSettingsMenu.Step(row, step); Refresh(); return; }
        if (enter) { FrontRoomsSettingsMenu.Activate(row); Refresh(); return; }
        if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete) || pad.Reset) { FrontRoomsSettingsMenu.ResetRow(row); Refresh(); }
    }

    // ↑ ↓ through the selectable rows without wrapping, opening each in place (SETTINGS_MOTION.md); on a fresh sheet or tab the
    // first opens the focused row where it is. ↓ past the last reaches the corner's RESUME, as a pad reaches the touch sheet's
    // chips (2026-10-31), and ↑ from there comes back.
    void MoveFocus(int move)
    {
        if (move == 0 || rows == null) return;
        CloseLanguage();
        if (armed && Focus >= 0 && Focus < views.Count && views[Focus] != null && views[Focus].Opens && openRow != Focus)
        {
            armed = false;
            SetOpenRow(Focus);
            Refresh();
            return;
        }
        armed = false;
        for (var j = Focus + move; j >= 0 && j < rows.Count; j += move)
            if (Selectable(rows[j])) { Focus = j; languageStrip.Reset(); SetOpenRow(j); Refresh(); return; }
        if (move > 0) { corner = Corner.Resume; languageStrip.Reset(); SetOpenRow(-1); Refresh(); }
    }

    // The corner's buttons with keys or a pad: ← END RUN, → RESUME, ↑ back to the rows; Enter or A presses.
    void HandleCorner(int move, int step, bool press)
    {
        if (corner == Corner.None) return;
        if (move < 0) { corner = Corner.None; SetOpenRow(Focus); Refresh(); return; }
        if (step != 0) { corner = step < 0 ? Corner.EndRun : Corner.Resume; Refresh(); return; }
        if (!press) return;
        // The press that resumes or ends the run is swallowed for the rest of the frame (2026-10-31): A is the pad's USE (and
        // Enter may be the player's), and the game reads this frame after the sheet, so A on RESUME would open the door ahead.
        FrontRoomsInput.ConsumeFrame();
        if (corner == Corner.Resume) FrontRoomsDesktopSettingsHost.RequestClose?.Invoke();
        else FrontRoomsDesktopSettingsHost.RequestRestart?.Invoke();
    }

    /// <summary>ESC: out of the preview, a second-level page or an open row first, then the game closes the sheet.</summary>
    public bool ConsumeEscape()
    {
        if (pageOpen && page.Capturing) { page.CancelCapture(); DrawPageDetail(); return true; }
        if (pageOpen) { ClosePage(); return true; }
        if (Peeking) { SetPeek(false); return true; }
        // An open row (or LANGUAGE's grid) closes first, then ESC goes back (SETTINGS_MOTION.md).
        if (langOpen) { CloseLanguage(); return true; }
        if (openRow < 0) return false;
        SetOpenRow(-1);
        Refresh();
        return true;
    }

    /// <summary>The unfolded map's keys: ← → or A D (a pad's d-pad or left stick) choose the next action left to right; ↑ ↓ or W S
    /// leave the graph for the rows above and below, which folds it; Q / E (LB / RB) change tab; Enter (A) listens, Backspace (X)
    /// resets the action, Delete (Y) DEFAULTS, B folds it.</summary>
    void HandlePageKeys(FrontRoomsInput.PadMenu pad)
    {
        // The map's y runs down the page, so up is -y: Vector2.down is the up key.
        var dir = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) ? Vector2.down
            : Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) ? Vector2.up
            : Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) ? Vector2.left
            : Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) ? Vector2.right
            : pad.Dir.x > 0 ? Vector2.right : pad.Dir.x < 0 ? Vector2.left : pad.Dir.y > 0 ? Vector2.down : pad.Dir.y < 0 ? Vector2.up : Vector2.zero;
        // Listening or asking: the view reads its own devices (Esc, MENU, B, A, Enter, a click). A direction held meanwhile (a
        // d-pad direction being bound) waits for its release, so it never moves the choice once the capture ends (2026-10-31).
        if (page.Capturing) { page.UpdateCapture(); FrontRoomsInput.HoldPadMenu(); DrawPageDetail(); return; }
        if (pad.Back) { ClosePage(); return; }
        if (dir.y != 0f) { var leave = dir.y < 0f ? -1 : 1; ClosePage(); MoveFocus(leave); return; }
        if (dir.x != 0f) { page.Step(dir.x > 0f ? 1 : -1); KeepChosenVisible(); DrawPageDetail(); return; }
        var tab = Input.GetKeyDown(KeyCode.Q) ? -1 : Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Tab) ? 1 : pad.Tab;
        if (tab != 0) { SetTab((int)CurrentTab + tab); return; }
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space) || pad.Confirm) { page.BeginCapture(); DrawPageDetail(); return; }
        if (Input.GetKeyDown(KeyCode.Backspace) || pad.Reset) { page.ResetChosen(); DrawPageDetail(); return; }
        if (Input.GetKeyDown(KeyCode.Delete) || pad.Defaults) PressDefaults();
    }

    /// <summary>DEFAULTS (Delete, a pad's Y): the first press asks, a second within 3 s puts the open page's device back
    /// (every key on KEYBOARD &amp; MOUSE, every button on CONTROLLER; 2026-10-31).</summary>
    void PressDefaults()
    {
        if (page.AllDefault) return;
        if (Time.unscaledTime < defaultsArmedUntil)
        {
            defaultsArmedUntil = -1f;
            page.ResetAll();
        }
        else defaultsArmedUntil = Time.unscaledTime + 3f;
        DrawPageDetail();
    }

    /// <summary>Unfolds the focused Page row: CONTROLS' KEYBOARD &amp; MOUSE map, or its CONTROLLER map (2026-10-31). Only one row is
    /// open: a row open in place closes as it unfolds.</summary>
    void OpenPage()
    {
        if (pageOpen || rows == null || Focus < 0 || Focus >= views.Count || views[Focus] == null || !views[Focus].Unfolds) return;
        SetOpenRow(-1);
        var padRow = rows != null && Focus >= 0 && Focus < rows.Count && rows[Focus].Key == FrontRoomsSettingsMenu.Key.PadMap;
        // The CONTROLLER map is drawn on its first open (2026-10-31): its masks cost every launch otherwise. It sits right
        // after the keyboard's map, where Build put that one.
        if (padRow && padMap == null)
        {
            padMap = FrontRoomsKeyMapView.CreatePad(listContent, bayon, mono, Paper, Ink, Accent);
            padMap.Root.SetSiblingIndex(keyMap.Root.GetSiblingIndex() + 1);
        }
        page = padRow ? padMap : keyMap;
        if (closingPage == page) closingPage = null;
        corner = Corner.None;
        armed = false;
        defaultsArmedUntil = -1f;
        pageOpen = true;
        page.SetActive(true);
        mapFitPage = null;
        FitMap();
        var now = FrontRoomsTouchMotion.Now;
        var v = views[Focus];
        v.UnfoldFrom = FoldAmount(v, now);
        v.IsUnfolded = true;
        v.UnfoldAt = now;
        animateUntil = now + 1f;
        FollowFocus();
        FadeUpCorner(FrontRoomsHandheld.ReduceMotion, .08f);
        Refresh();
        page.PlayOpen();
    }

    void ClosePage()
    {
        if (!pageOpen) return;
        pageOpen = false;
        // The whole graph fades out, then the row folds.
        page.PlayClose();
        closingPage = page;
        var now = FrontRoomsTouchMotion.Now;
        foreach (var v in views)
        {
            if (v == null || !v.Unfolds || !v.IsUnfolded) continue;
            v.FoldFrom = FoldAmount(v, now);
            v.IsUnfolded = false;
            v.FoldAt = now;
        }
        animateUntil = now + 1f;
        FollowFocus();
        FadeUpCorner(FrontRoomsHandheld.ReduceMotion, .1f);
        defaultsArmedUntil = -1f;
        Refresh();
    }

    /// <summary>Unfolds LANGUAGE into its grid under the row (E3); only one row is open.</summary>
    void OpenLanguage()
    {
        if (langOpen || rows == null || Focus < 0 || Focus >= views.Count || views[Focus] == null || rows[Focus].Kind != Kind.Language) return;
        SetOpenRow(-1);
        langOpen = true;
        armed = false;
        var now = FrontRoomsTouchMotion.Now;
        var v = views[Focus];
        v.FoldH = LangGridH;
        v.UnfoldFrom = FoldAmount(v, now);
        v.IsUnfolded = true;
        v.UnfoldAt = now;
        languageStrip.SetGrid(true);
        animateUntil = now + 1f;
        FollowFocus();
        Refresh();
    }

    // Folds LANGUAGE's grid: the cells fade, then the row folds and the rail comes back.
    void CloseLanguage()
    {
        if (!langOpen) return;
        langOpen = false;
        var now = FrontRoomsTouchMotion.Now;
        for (var i = 0; i < views.Count && i < rows.Count; i++)
        {
            var v = views[i];
            if (v == null || rows[i].Kind != Kind.Language || !v.IsUnfolded) continue;
            v.FoldFrom = FoldAmount(v, now);
            v.IsUnfolded = false;
            v.FoldAt = now;
        }
        languageStrip.SetGrid(false);
        animateUntil = now + 1f;
        FollowFocus();
        Refresh();
    }

    // The corner buttons and the hint line change their words at once; they fade up with the new ones.
    void FadeUpCorner(bool reduced, float delay)
    {
        if (reduced) { cornerFade.Snap(1f); return; }
        cornerFade.Restart(0f, 1f, .2f, FrontRoomsTouchMotion.Ease.OutCubic, delay);
    }

    // Everything where it rests, with no motion: the sheet opening.
    void StopPageMotion()
    {
        if (closingPage != null) closingPage.SetActive(false);
        closingPage = null;
        listFade.Snap(1f);
        detailFade.Snap(1f);
        cornerFade.Snap(1f);
    }

    // Per frame: the list, the detail column, the corner buttons and the hint line follow the motion; a map folding fades out.
    void AnimatePageMotion()
    {
        listGroup.alpha = listFade.Current;
        detailGroup.alpha = detailFade.Current;
        foreach (var g in cornerGroups) g.alpha = cornerFade.Current;
        if (closingPage != null)
        {
            closingPage.Animate();
            if (!closingPage.Closing) closingPage = null;
        }
    }

    // A pick changes the whole sheet at once (README §7.2). Fixed words already set themselves
    // (FrontRoomsLanguage.Style); the buttons follow their words' widths.
    void OnLanguageChanged()
    {
        FitKeyline(back, backText, 0f);
        FitKeyline(restart, restartText, back.rectTransform.sizeDelta.x + 12f);
        FitKeyline(pageBack, pageBackText, 0f);
        pageDefaultsText.text = FrontRoomsLanguage.T("DEFAULTS?");
        FitKeyline(pageDefaults, pageDefaultsText, pageBack.rectTransform.sizeDelta.x + 12f);
        pageDefaultsText.text = FrontRoomsLanguage.T("DEFAULTS");
        signature = null;
        pictureH = -1f;
        mapFitPage = null;
        Refresh();
    }

    // The tab's rows without FRAME RATE NOW: on the desktop its reading sits in the GRAPHICS picture (Red, 2026-11-01).
    List<Row> ListRows() => FrontRoomsSettingsMenu.Rows(CurrentTab).FindAll(r => r.Kind != Kind.Meter);

    /// <summary>A map is unfolded and has the keys (Esc folds it before it closes the sheet).</summary>
    public bool PageOpen => pageOpen;

    void ShowPage()
    {
        // While a map is open the END RUN key reads nothing (browsing it with R must not end the run; the END RUN button still does).
        FrontRoomsInput.SuspendRestart = pageOpen;
        back.gameObject.SetActive(!pageOpen);
        restart.gameObject.SetActive(!pageOpen);
        pageBack.gameObject.SetActive(pageOpen);
        pageDefaults.gameObject.SetActive(pageOpen);
        // A pad that came or went says so here for a moment, in place of the hints (2026-11-01).
        hints.text = FrontRoomsInput.NoticeLive ? FrontRoomsInput.ChangeLine() : pageOpen ? PageHints : HintText() + RowHint();
    }

    // What Enter does on a row that unfolds (a map, LANGUAGE), and how LANGUAGE's grid is worked once it has.
    string RowHint()
    {
        if (corner != Corner.None || rows == null || Focus < 0 || Focus >= rows.Count) return "";
        var row = rows[Focus];
        var pad = FrontRoomsInput.PadInUse;
        if (row.Kind == Kind.Language && langOpen)
            return Gap + (pad ? FrontRoomsLanguage.F("{0}  CHOOSE", "← →") + Gap + FrontRoomsLanguage.F("{0}  SET", FrontRoomsInput.PadName(PadButton.South)) : FrontRoomsLanguage.T("← →  CHOOSE     ENTER  SET"));
        if (row.Kind != Kind.Page && row.Kind != Kind.Language) return "";
        return Gap + (pad ? FrontRoomsLanguage.F("{0}  OPEN", FrontRoomsInput.PadName(PadButton.South)) : FrontRoomsLanguage.T("ENTER  OPEN"));
    }

    /// <summary>The open map's size: at most 1.45x (the spec's 58 px pitch), as large as the list's height under its row and columns
    /// 1–12 allow (again whenever the window or the words change).</summary>
    void FitMap()
    {
        var view = listMask.rect.height;
        var wide = root.rect.width - 2f * M;
        if (page == mapFitPage && Mathf.Approximately(view, mapFitView) && Mathf.Approximately(wide, mapFitW)) return;
        mapFitPage = page;
        mapFitView = view;
        mapFitW = wide;
        var k = Mathf.Clamp((view - Mx.Row - 2f * MapPad) / page.UnitsTall, MapMinScale, MapMaxScale);
        page.SetScale(k);
        var over = page.Bounds.width - wide;
        if (over > 0f) page.SetScale(Mathf.Max(MapMinScale, k * wide / (wide + over)));
        for (var i = 0; i < views.Count && i < rows.Count; i++)
            if (views[i] != null && views[i].Unfolds && IsRowOf(rows[i], page)) views[i].FoldH = Mathf.Ceil(page.Bounds.height + 2f * MapPad);
    }

    static bool IsRowOf(Row r, FrontRoomsKeyMapView map) => r.Key == (map.IsPad ? FrontRoomsSettingsMenu.Key.PadMap : FrontRoomsSettingsMenu.Key.KeyMap);

    // The hint line and DEFAULTS while a map has the keys; the words of the chosen action are on its own label.
    void DrawPageDetail()
    {
        hints.text = FrontRoomsInput.NoticeLive ? FrontRoomsInput.ChangeLine() : PageHints;
        var asking = Time.unscaledTime < defaultsArmedUntil;
        pageDefaultsText.text = FrontRoomsLanguage.T(asking ? "DEFAULTS?" : "DEFAULTS");
        pageDefaultsText.color = new Color(Paper.r, Paper.g, Paper.b, page.AllDefault ? .4f : 1f);
    }

    // The list follows the chosen action when the graph is taller than the list (0.32 s, as it follows a row).
    void KeepChosenVisible()
    {
        var y = -page.Root.anchoredPosition.y;
        var chosen = page.ChosenRect;
        var view = listMask.rect.height;
        var target = scrollTo;
        if (y + chosen.y - MapPad < target) target = y + chosen.y - MapPad;
        else if (y + chosen.yMax + MapPad > target + view) target = y + chosen.yMax + MapPad - view;
        target = Mathf.Clamp(target, 0f, Mathf.Max(0f, contentH - view));
        if (Mathf.Approximately(target, scrollTo)) return;
        scrollFrom = scroll;
        scrollTo = target;
        scrollAt = FrontRoomsTouchMotion.Now;
        animateUntil = Mathf.Max(animateUntil, scrollAt + ScrollS + .05f);
    }

    // ------------------------------------------------------------- rows that open in place (SETTINGS_MOTION.md)

    static float OutQuart(float t) { t = Mathf.Clamp01(t); var u = 1f - t; return 1f - u * u * u * u; }
    static float InOutCubic(float t) => FrontRoomsTouchMotion.Evaluate(FrontRoomsTouchMotion.Ease.InOutCubic, t);

    /// <summary>The rows that open in place. A map row and LANGUAGE unfold instead; FRAME RATE NOW lives in the picture.</summary>
    static bool Opens(Row r) => r.Kind == Kind.Segments || r.Kind == Kind.Switch || r.Kind == Kind.Slider || r.Kind == Kind.Action;

    // No row open, the list at its top, and the first ↑ ↓ opening the focused row where it is: a fresh sheet or tab.
    void ResetOpen()
    {
        openRow = hoverRow = -1;
        openKey = FrontRoomsSettingsMenu.Key.None;
        langOpen = false;
        languageStrip.SetGrid(false, true);
        armed = true;
        scroll = scrollFrom = scrollTo = 0f;
        scrollAt = -100f;
    }

    /// <summary>Opens row <paramref name="index"/> in place and closes the one open before it (only one row is open); -1, or
    /// a row that unfolds instead, closes it.</summary>
    void SetOpenRow(int index)
    {
        if (index < 0 || index >= views.Count || views[index] == null || !views[index].Opens) index = -1;
        var now = FrontRoomsTouchMotion.Now;
        for (var i = 0; i < views.Count; i++)
        {
            var v = views[i];
            if (v == null || !v.Opens || v.IsOpen == (i == index)) continue;
            var amount = OpenAmount(v, now);
            v.IsOpen = i == index;
            if (v.IsOpen) { v.OpenAt = now; v.OpenFrom = amount; }
            else { v.CloseAt = now; v.CloseFrom = amount; }
        }
        openRow = index;
        openKey = index >= 0 ? rows[index].Key : FrontRoomsSettingsMenu.Key.None;
        animateUntil = now + 1f;
        FollowFocus();
    }

    // How far a row is open, 0..1: it opens in 0.24 s, ease-out quart, from wherever it was; closing, it waits for its second
    // line to fade (0.09 s), then folds in 0.18 s, ease-in-out cubic. Reduced motion: it jumps.
    static float OpenAmount(RowView v, float now)
    {
        if (!v.Opens) return 0f;
        if (FrontRoomsHandheld.ReduceMotion) return v.IsOpen ? 1f : 0f;
        if (v.IsOpen) return Mathf.Lerp(v.OpenFrom, 1f, OutQuart((now - v.OpenAt) / OpenS));
        return v.CloseFrom * (1f - InOutCubic((now - v.CloseAt - CloseFadeS) / CloseS));
    }

    // A second-line element's alpha, 0..1. Slot 0 is the sentence, then the notes left to right, then the default: each fades in
    // 0.18 s from 0.09 s, 0.035 s after the one before, where it stays (nothing drops or pushes, Red 20:5x); a closing row's fade
    // out together in 0.09 s. Reduced motion: all fade in 0.12 s.
    static float Reveal(RowView v, int slot, float now)
    {
        if (FrontRoomsHandheld.ReduceMotion) return v.IsOpen ? Mathf.Clamp01((now - v.OpenAt) / ReducedFadeS) : 0f;
        var shown = OutQuart(((v.IsOpen ? now : v.CloseAt) - v.OpenAt - RevealAt - slot * Stagger) / RevealS);
        return v.IsOpen ? shown : shown * (1f - Mathf.Clamp01((now - v.CloseAt) / CloseFadeS));
    }

    // How far a map row is unfolded, 0..1: in 0.26 s + 1 s per 1500 units, at most 0.52 s, ease-out quart, from wherever it was;
    // folding, it waits for the graph to fade (0.12 s), then folds in 0.22 s, ease-in-out cubic. Reduced motion: it jumps.
    static float FoldAmount(RowView v, float now)
    {
        if (!v.Unfolds) return 0f;
        if (FrontRoomsHandheld.ReduceMotion) return v.IsUnfolded ? 1f : 0f;
        if (v.IsUnfolded) return Mathf.Lerp(v.UnfoldFrom, 1f, OutQuart((now - v.UnfoldAt) / Mathf.Min(UnfoldMax, UnfoldMin + v.FoldH * UnfoldPerUnit)));
        return v.FoldFrom * (1f - InOutCubic((now - v.FoldAt - FoldFadeS) / FoldS));
    }

    // The list follows to show the whole focused row, with the section header it heads (0.32 s, ease-in-out), measured on the
    // heights the rows are going to: it only ever moves as far as the open row needs.
    void FollowFocus()
    {
        if (rows == null || Focus < 0 || Focus >= views.Count || Focus >= rows.Count || views[Focus] == null) return;
        var y = 8f;
        var top = 0f;
        var height = 0f;
        for (var i = 0; i < views.Count && i < rows.Count; i++)
        {
            var v = views[i];
            var h = v == null ? 0f : v.Opens && v.IsOpen ? v.OpenH : v.Unfolds && v.IsUnfolded ? v.RestH + v.FoldH : v.RestH;
            if (i == Focus) { top = y; height = h; }
            y += h;
        }
        var view = listMask.rect.height;
        // The header goes with its row, unless a graph needs the room.
        if (Focus > 0 && rows[Focus - 1].Kind == Kind.Header && views[Focus - 1] != null && height + views[Focus - 1].RestH <= view)
        {
            top -= views[Focus - 1].RestH;
            height += views[Focus - 1].RestH;
        }
        var target = scrollTo;
        if (top < target) target = top;
        else if (top + height > target + view) target = Mathf.Min(top, top + height - view);
        target = Mathf.Clamp(target, 0f, Mathf.Max(0f, y + 8f - view));
        scrollFrom = scroll;
        scrollTo = target;
        scrollAt = FrontRoomsTouchMotion.Now;
        animateUntil = Mathf.Max(animateUntil, scrollAt + ScrollS + .05f);
    }

    // On every Refresh and each frame while anything moves: each row's height from how far it is open, every row below it, its
    // bar, its underlay, its name and its second line; then the list's scroll.
    void LayoutRows()
    {
        if (rows == null) return;
        var now = FrontRoomsTouchMotion.Now;
        var w = listContent.sizeDelta.x;
        var y = 8f;
        for (var i = 0; i < views.Count && i < rows.Count; i++)
        {
            var v = views[i];
            if (v == null) continue;
            var o = OpenAmount(v, now);
            var f = FoldAmount(v, now);
            v.H = v.Opens ? Mathf.Lerp(v.RestH, v.OpenH, o) : v.RestH + f * v.FoldH;
            v.Y = y;
            PlaceIn(v.Rect, 0f, y, w, v.H);
            y += v.H;
            if (rows[i].Kind == Kind.Header) continue;
            PlaceIn(v.Divider.rectTransform, 0f, v.H - 1f, w, 1f);
            // Its own bar, as tall as the row: it leads the open and folds with the row; a focused row at rest keeps it (while the
            // corner has the focus no row's bar shows and the focused button is yellow, 2026-10-31).
            var focus = i == Focus && corner == Corner.None;
            v.Bar.color = new Color(Accent.r, Accent.g, Accent.b, focus ? 1f : Mathf.Min(1f, Mathf.Max(o, f) * 4f));
            // A map row's bar stays at its name: its graph has its own bars.
            PlaceIn(v.Bar.rectTransform, 0f, 0f, 4f, v.Unfolds ? v.RestH : v.H);
            if (v.Opens) AnimateOpen(v, rows[i], o, now, w, i == Focus || i == hoverRow);
            if (v.Unfolds && rows[i].Kind == Kind.Page) AnimateFold(v, rows[i], f, i == Focus || i == hoverRow);
        }
        // LANGUAGE's strip follows its row while rows move.
        if (CurrentTab == Tab.Assist && FrontRoomsTouchMotion.Now < animateUntil) PlaceLanguageStrip();
        contentH = y + 8f;
        listContent.sizeDelta = new Vector2(w, contentH);
        var s = FrontRoomsHandheld.ReduceMotion ? scrollTo : Mathf.Lerp(scrollFrom, scrollTo, InOutCubic((now - scrollAt) / ScrollS));
        scroll = Mathf.Clamp(s, 0f, Mathf.Max(0f, contentH - listMask.rect.height));
        listContent.anchoredPosition = new Vector2(0f, scroll);
    }

    // An open (or moving) row: its paper underlay at 7 %; its name 1.4x from its left end on its own centre line (the resting
    // Label while closed, so both rests are drawn at their own size); its second line's alphas.
    void AnimateOpen(RowView v, Row r, float o, float now, float w, bool bright)
    {
        v.Underlay.color = new Color(Paper.r, Paper.g, Paper.b, .07f * Mathf.Min(1f, o * 1.5f));
        PlaceIn(v.Underlay.rectTransform, 0f, 0f, w, v.H);
        var moving = o > .0005f;
        v.Label.enabled = !moving;
        v.LabelOpen.enabled = moving;
        var k = v.Label.fontSize * (1f + TitleGrow * OutQuart(o)) / Mathf.Max(1, v.LabelOpen.fontSize);
        v.LabelOpen.rectTransform.localScale = new Vector3(k, k, 1f);
        v.LabelOpen.color = new Color(Paper.r, Paper.g, Paper.b, bright ? 1f : .9f);
        Fade(v.Sentence, .88f * Reveal(v, 0, now));
        for (var n = 0; n < v.Notes.Count; n++)
            Fade(v.Notes[n], (v.NoteOf[n] < 0 ? .8f : v.NoteOf[n] == r.Index ? .95f : .5f) * Reveal(v, 1 + n, now));
        if (v.Rule != null) v.Rule.color = new Color(Paper.r, Paper.g, Paper.b, .6f * Reveal(v, 1 + v.RuleSlot, now));
        if (v.Default != null) Fade(v.Default, .45f * Reveal(v, 1 + v.Notes.Count, now));
    }

    // A map row unfolding: its name grows as an open row's does, its › turns to ⌄, its keycaps go (the graph has them), and its
    // graph sits under it, centred across columns 1–12.
    void AnimateFold(RowView v, Row r, float f, bool bright)
    {
        var moving = f > .0005f;
        v.Label.enabled = !moving;
        v.LabelOpen.enabled = moving;
        var k = v.Label.fontSize * (1f + TitleGrow * f) / Mathf.Max(1, v.LabelOpen.fontSize);
        v.LabelOpen.rectTransform.localScale = new Vector3(k, k, 1f);
        v.LabelOpen.color = new Color(Paper.r, Paper.g, Paper.b, bright ? 1f : .9f);
        v.Chevron.localRotation = Quaternion.Euler(0f, 0f, -90f * f);
        v.Caps.alpha = 1f - Mathf.Clamp01(f * 2.5f);
        v.Caps.gameObject.SetActive(f < .999f);
        var x = 24f + Mathf.Ceil(v.Label.preferredWidth) * (1f + TitleGrow * f) + Mx.WordGap;
        PlaceIn(v.Tag.rectTransform, x, 0f, Mathf.Max(0f, listContent.sizeDelta.x - 40f - x), Mx.Row);
        // A long label in a long language keeps the keycaps clear: the tag steps aside until they have gone.
        v.Tag.enabled = v.Tag.text.Length > 0 && (f > .5f || x + v.Tag.preferredWidth <= v.TagLimit - 12f);
        var map = pageOpen && IsRowOf(r, page) ? page : closingPage != null && IsRowOf(r, closingPage) ? closingPage : null;
        if (map == null) return;
        var b = map.Bounds;
        map.PlaceAt(Mathf.Round(Mathf.Max(0f, (root.rect.width - 2f * M - b.width) * .5f) - b.x), Mathf.Round(v.Y + v.RestH + MapPad - b.y));
    }

    static void Fade(Text t, float alpha)
    {
        if (t != null) t.color = new Color(Paper.r, Paper.g, Paper.b, alpha);
    }

    /// <summary>A keyline button at the bottom right, <paramref name="rightInset"/> left of the margin, sized for <paramref name="widest"/>.</summary>
    Image KeylineButton(string widest, float rightInset, out Text word)
    {
        var b = Img(root, Paper, 0f);
        b.rectTransform.anchorMin = b.rectTransform.anchorMax = new Vector2(1f, 0f); b.rectTransform.pivot = new Vector2(1f, 0f);
        word = Txt(b.rectTransform, widest, bayon, 20, Paper, TextAnchor.MiddleCenter);
        FitKeyline(b, word, rightInset);
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

    void FitKeyline(Image b, Text word, float rightInset)
    {
        b.rectTransform.anchoredPosition = new Vector2(-M - rightInset, M - 16f);
        b.rectTransform.sizeDelta = new Vector2(Mathf.Max(64f, Mathf.Ceil(word.preferredWidth) + 26f), 38f);
    }

    void Update()
    {
        if (!open) return;
        var fps = FrontRoomsQuality.MeasuredFps;
        var target = Mathf.Max(1, FrontRoomsQuality.TargetFps);
        var text = Mathf.RoundToInt(fps) + " FPS  ·  " + (1000f / Mathf.Max(1f, fps)).ToString("0.0") + " MS  ·  " + FrontRoomsLanguage.T("TARGET") + " " + target;
        var lit = Mathf.Clamp(Mathf.RoundToInt(10f * fps / target), 0, 10);
        // Its numbers in fixed columns, so the corner holds still as they change.
        frameText.text = Mathf.RoundToInt(fps).ToString().PadLeft(3) + " FPS  ·  " + (1000f / Mathf.Max(1f, fps)).ToString("0.0").PadLeft(5) + " MS  ·  " + FrontRoomsLanguage.T("TARGET") + " " + target;
        peekFrameText.text = text;
        for (var i = 0; i < 10; i++)
        {
            frameDashes[i].color = new Color(Paper.r, Paper.g, Paper.b, i < lit ? 1f : .2f);
            peekDashes[i].color = frameDashes[i].color;
        }
        if (FrontRoomsSettingsMenu.ResetArmedUntil > 0f && !FrontRoomsSettingsMenu.ResetArmed) { FrontRoomsSettingsMenu.ResetArmedUntil = -1f; Refresh(); }
        languageStrip.Animate();
        if (langOpen && CurrentTab == Tab.Assist)
        {
            var under = languageStrip.HighlightedLanguage;
            languagePreview.Show(under != FrontRoomsLanguage.Current ? under : null);
            languagePreview.Animate();
        }
        // The words follow the device in use and the pad in hand (another pad, or a PlayStation pad for an Xbox one). A pad
        // coming or going redraws the scheme words and the hint line's notice, and the notice's end brings the hints back
        // (2026-11-01).
        var pad = FrontRoomsInput.PadInUse;
        var device = UnityEngine.InputSystem.Gamepad.current;
        var playStation = FrontRoomsInput.PlayStationPad;
        var connected = FrontRoomsInput.PadConnected;
        var notice = FrontRoomsInput.NoticeLive;
        var at = FrontRoomsInput.LastChangeAt;
        if (pad != padShown || device != padDevice || playStation != playStationShown || connected != connectedShown || notice != noticeShown || at != noticeAt)
        {
            padShown = pad; padDevice = device; playStationShown = playStation; connectedShown = connected; noticeShown = notice; noticeAt = at;
            if (Peeking) DrawPeekHint();
            Refresh();
        }
        AnimatePageMotion();
        // While a map is open every frame: a resized window keeps it on the grid.
        if (pageOpen) { LayoutFrame(); FitMap(); }
        if (FrontRoomsTouchMotion.Now < animateUntil || pageOpen) LayoutRows();
        PlacePicture();
        if (pageOpen)
        {
            page.Animate();
            if (defaultsArmedUntil > 0f && Time.unscaledTime >= defaultsArmedUntil) { defaultsArmedUntil = -1f; DrawPageDetail(); }
        }
        RenderPreview();
    }

    // ------------------------------------------------------------- draw

    public void Refresh()
    {
        if (!open) return;
        rows = ListRows();
        if (Focus >= rows.Count || Focus < 0 || !Selectable(rows[Focus])) Focus = FirstSelectable();
        LayoutFrame();
        DrawTabs();
        ShowPage();
        if (pageOpen) page.Refresh();
        var sig = CurrentTab.ToString();
        foreach (var r in rows) sig += "|" + r.Key + r.Kind + (r.Options != null ? string.Join(",", r.Options) : "") + r.Label;
        if (sig != signature) { signature = sig; RebuildRows(); }
        UpdateRows();
        DrawDetail();
        if (pageOpen) DrawPageDetail();
    }

    void LayoutFrame()
    {
        var w = root.rect.width;
        var listW = Mathf.Clamp(w - M - DetailW - M - 96f, 640f, 1026f);
        // CONTROLS has no picture: its list has the right column too, for a map, and runs to just over the hint line.
        var controls = CurrentTab == Tab.Controls;
        Place(listMask, M, ListTop, controls ? Mathf.Max(listW, w - 2f * M) + 24f : listW + 24f, Mathf.Max(200f, root.rect.height - ListTop - (controls ? 96f : 120f)));
        listContent.sizeDelta = new Vector2(listW, listContent.sizeDelta.y);
        detail.anchoredPosition = new Vector2(-M - DetailW, -ListTop);
        var read = FrontRoomsSettingsMenu.TextWidth("000 FPS  ·  000.0 MS  ·  " + FrontRoomsLanguage.T("TARGET") + " " + Mathf.Max(1, FrontRoomsQuality.TargetFps), 13f, true);
        if (read != frameReadW) { frameReadW = read; pictureH = -1f; }
        PlacePicture();
    }

    // The picture, its caption and its frame rate, kept clear of the corner's buttons whatever the window's shape (Red,
    // 2026-11-01: 版式错误, the column ran into END RUN): the column has until the buttons' top, and on a wide, short window the
    // picture gives way, keeping its proportions and its left edge.
    void PlacePicture()
    {
        // The picture heads the column: the words went into the rows (SETTINGS_MOTION.md).
        const float top = 0f, caption = 32f, clear = 4f, inset = 12f, dash = 16f, pitch = 19f;
        var buttonsTop = root.rect.height - ListTop - (M - 16f + 38f);
        var h = Mathf.Clamp(buttonsTop - clear - caption - top, 120f, PreviewH);
        if (Mathf.Approximately(h, pictureH)) return;
        pictureH = h;
        var w = Mathf.Round(h * DetailW / PreviewH);
        PlaceIn(preview2Frame.rectTransform, -1f, top - 1f, w + 2f, h + 2f);
        PlaceIn(preview.rectTransform, 0f, top, w, h);
        PlaceIn(previewCaption.rectTransform, 0f, top + h + 12f, DetailW, 20f);
        PlaceIn(frameOsd, 0f, top, w, h);
        var dashes = h - inset - 4f;
        for (var i = 0; i < frameDashes.Count; i++) PlaceIn(frameDashes[i].rectTransform, w - inset - (frameDashes.Count - i) * pitch + (pitch - dash), dashes, dash, 4f);
        var line = dashes - 6f - 18f;
        PlaceIn(frameText.rectTransform, 0f, line, w - inset, 18f);
        PlaceIn(frameLabel.rectTransform, 0f, line, w - inset - frameReadW - 16f, 18f);
        // A picture too small for the word keeps the reading.
        frameLabel.enabled = frameLabel.preferredWidth + 16f + frameReadW + 2f * inset <= w;
    }

    void DrawTabs()
    {
        var names = FrontRoomsSettingsMenu.Tabs;
        while (tabTexts.Count < names.Length + 2)
        {
            // Q and E (a pad's LB and RB while one is in use) are keycaps, the rest are words: set once, so a language's face is
            // not undone on every draw.
            var keycap = tabTexts.Count == 0 || tabTexts.Count == names.Length + 1;
            var t = Txt(tabsRoot, "", keycap ? mono : bayon, keycap ? 13 : 20, Paper, TextAnchor.UpperLeft);
            tabTexts.Add(t);
            tabRules.Add(Img(tabsRoot, Paper, 0f));
        }
        while (keyEdges.Count < 8) keyEdges.Add(Img(tabsRoot, Paper, .5f));
        var x = 0f;
        for (var i = 0; i < names.Length + 2; i++)
        {
            var key = i == 0 || i == names.Length + 1;
            var t = tabTexts[i];
            t.text = key ? (FrontRoomsInput.PadInUse ? FrontRoomsInput.PadName(i == 0 ? PadButton.LeftShoulder : PadButton.RightShoulder) : i == 0 ? "Q" : "E")
                : FrontRoomsSettingsMenu.TabName(names[i - 1]);
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
                v.H = v.RestH = 32f;
                PlaceIn(v.Rect, 0f, y, w, v.H);
                v.Label = Txt(v.Rect, r.Label, mono, 13, new Color(Paper.r, Paper.g, Paper.b, .75f), TextAnchor.MiddleLeft);
                PlaceIn(v.Label.rectTransform, 24f, 8f, 400f, 20f);
                views.Add(v);
                y += v.H;
                continue;
            }
            // LANGUAGE is taller: its charms hang below the key (LANGUAGE_CHARMS.md, 80 px).
            v.H = v.RestH = r.Kind == Kind.Language ? Mathf.Max(Mx.Row, FrontRoomsLanguageStrip.Desktop.Row) : Mx.Row;
            PlaceIn(v.Rect, 0f, y, w, v.H);
            v.Opens = Opens(r);
            v.Unfolds = r.Kind == Kind.Page || r.Kind == Kind.Language;
            // An open row's paper underlay, under everything in it.
            if (v.Opens) v.Underlay = Img(v.Rect, Paper, 0f);
            v.Label = Txt(v.Rect, r.Label, bayon, 20, Paper, TextAnchor.MiddleLeft);
            PlaceIn(v.Label.rectTransform, 24f, 0f, 480f, v.H);
            v.Divider = Img(v.Rect, Paper, .08f);
            PlaceIn(v.Divider.rectTransform, 0f, v.H - 1f, w, 1f);
            v.Changed = Img(v.Rect, Paper, 0f);
            BuildControl(v, r, w);
            if (v.Opens) BuildSecondLine(v, r, w);
            if (v.Unfolds) BuildOpenLabel(v, r);
            // Its own bar, over everything in it.
            v.Bar = Img(v.Rect, Accent, 0f);
            views.Add(v);
            y += v.H;
        }
        contentH = y + 8f;
        listContent.sizeDelta = new Vector2(w, contentH);
        PlaceCost(w);
        // A rebuild (a language, a pad's words) keeps the open row open, at rest.
        openRow = -1;
        for (var i = 0; i < rows.Count && i < views.Count; i++)
        {
            var v = views[i];
            if (openKey == FrontRoomsSettingsMenu.Key.None || rows[i].Key != openKey || v == null || !v.Opens) continue;
            v.IsOpen = true;
            openRow = i;
            break;
        }
        if (openRow < 0) openKey = FrontRoomsSettingsMenu.Key.None;
        // A rebuild (a pick, a language's words) keeps LANGUAGE unfolded, at rest.
        for (var i = 0; langOpen && i < rows.Count && i < views.Count; i++)
        {
            if (views[i] == null || rows[i].Kind != Kind.Language) continue;
            views[i].IsUnfolded = true;
            views[i].UnfoldFrom = 1f;
            views[i].FoldH = LangGridH;
        }
        // A rebuild (a key changed, a pad's words) keeps the open map unfolded, at rest.
        for (var i = 0; pageOpen && i < rows.Count && i < views.Count; i++)
        {
            if (views[i] == null || !views[i].Unfolds || !IsRowOf(rows[i], page)) continue;
            views[i].IsUnfolded = true;
            views[i].UnfoldFrom = 1f;
            views[i].FoldH = Mathf.Ceil(page.Bounds.height + 2f * MapPad);
        }
    }

    // The name at the open size, 1.4x, waiting beside the resting one.
    void BuildOpenLabel(RowView v, Row r)
    {
        v.LabelOpen = Txt(v.Rect, r.Label, bayon, OpenLabelSize, Paper, TextAnchor.MiddleLeft);
        var lt = v.LabelOpen.rectTransform;
        lt.anchorMin = lt.anchorMax = new Vector2(0f, 1f);
        lt.pivot = new Vector2(0f, .5f);
        lt.anchoredPosition = new Vector2(24f, -Mx.Row * .5f);
        lt.sizeDelta = new Vector2(640f, Mx.Row);
        v.LabelOpen.enabled = false;
    }

    // An open row's second line (SETTINGS_MOTION.md, "What the second line holds"), built hidden. Left: what the row does
    // (Source Serif 18 at 88 %, two lines at most; a longer sentence makes the row taller and is never cut). Under the control:
    // what each choice means (Plex Mono 12, the current one at 95 %, the rest at 50 %, a short rule under the default's). Right:
    // the default (45 %).
    void BuildSecondLine(RowView v, Row r, float w)
    {
        BuildOpenLabel(v, r);
        // What sits right of the sentence starts at the control, or at a note or the default reaching further left.
        var left = w - FrontRoomsSettingsMenu.ControlWidth(r, Mx);
        var notes = r.Notes;
        if (notes != null && r.Kind == Kind.Switch)
        {
            // One note, the current state's (UpdateRows), at the row's right end.
            AddNote(v, -1, w - 400f, 400f, TextAnchor.MiddleRight);
            foreach (var n in notes) left = Mathf.Min(left, w - FrontRoomsSettingsMenu.TextWidth(n, NoteSize, true));
        }
        else if (notes != null && (r.Kind == Kind.Segments || r.Kind == Kind.Slider))
        {
            FrontRoomsSettingsMenu.TrackSpan(Mx, out var low, out _);
            var count = r.Kind == Kind.Slider ? (r.Stops != null ? r.Stops.Length : 0) : r.Options.Length;
            var x = w - FrontRoomsSettingsMenu.ControlWidth(r, Mx);
            for (var i = 0; i < count; i++)
            {
                // Under its segment's middle, or under its stop on the track.
                var cell = r.Kind == Kind.Segments ? FrontRoomsSettingsMenu.CellWidth(r, i, Mx) : 0f;
                var c = r.Kind == Kind.Segments ? x + cell * .5f : w - low + Mathf.Round(Mx.Track * FrontRoomsSettingsMenu.StopPosition(r, i));
                x += cell;
                if (i >= notes.Length || string.IsNullOrEmpty(notes[i])) continue;
                var nw = FrontRoomsSettingsMenu.TextWidth(notes[i], NoteSize, true);
                AddNote(v, i, Mathf.Round(c) - 120f, 240f, TextAnchor.MiddleCenter).text = notes[i];
                left = Mathf.Min(left, c - nw * .5f);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                // A note wider than its choice goes back to the strings, never cut with "…" (SETTINGS_MOTION.md).
                if (r.Kind == Kind.Segments && nw > cell + 1f && warnedNotes.Add(notes[i]))
                    Debug.LogWarning("[settings] note wider than its choice (" + r.Label + "): " + notes[i]);
#endif
                if (i != r.DefaultIndex) continue;
                v.Rule = Img(v.Rect, Paper, 0f);
                PlaceIn(v.Rule.rectTransform, Mathf.Round(c) - 6f, RuleY, 12f, 1f);
                v.RuleSlot = v.Notes.Count - 1;
            }
        }
        var line = DefaultLineOf(r);
        if (line.Length > 0)
        {
            v.Default = Txt(v.Rect, "", mono, NoteSize, Paper, TextAnchor.MiddleRight);
            v.Default.text = line;
            v.Default.color = new Color(Paper.r, Paper.g, Paper.b, 0f);
            PlaceIn(v.Default.rectTransform, w - 600f, DefaultY - 10f, 600f, 20f);
            left = Mathf.Min(left, w - FrontRoomsSettingsMenu.TextWidth(line, NoteSize, true));
        }
        // The sentence, in the room left of all that, centred in the second line; a third line makes the row taller.
        v.Sentence = Txt(v.Rect, "", serif, SentenceSize, Paper, TextAnchor.MiddleLeft);
        v.Sentence.text = r.Explain ?? "";
        v.Sentence.horizontalOverflow = HorizontalWrapMode.Wrap;
        v.Sentence.lineSpacing = SentencePitch;
        v.Sentence.color = new Color(Paper.r, Paper.g, Paper.b, 0f);
        var width = Mathf.Max(180f, left - 28f - 24f);
        PlaceIn(v.Sentence.rectTransform, 24f, SentenceY - Mx.Row * .5f, width, Mx.Row);
        v.OpenH = 2f * Mx.Row;
        var block = v.Sentence.preferredHeight;
        if (block <= Mx.Row + .5f) return;
        v.OpenH = Mx.Row + Mathf.Ceil(block) + 14f;
        v.Sentence.alignment = TextAnchor.UpperLeft;
        PlaceIn(v.Sentence.rectTransform, 24f, Mx.Row + 4f, width, Mathf.Ceil(block) + 2f);
        if (v.Default != null) PlaceIn(v.Default.rectTransform, w - 600f, v.OpenH - 2f * Mx.Row + DefaultY - 10f, 600f, 20f);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    static readonly HashSet<string> warnedNotes = new HashSet<string>();
#endif

    // A note of the second line, hidden until the row opens.
    Text AddNote(RowView v, int of, float x, float width, TextAnchor anchor)
    {
        var t = Txt(v.Rect, "", mono, NoteSize, Paper, anchor);
        t.color = new Color(Paper.r, Paper.g, Paper.b, 0f);
        PlaceIn(t.rectTransform, x, NoteY - 10f, width, 20f);
        v.Notes.Add(t);
        v.NoteOf.Add(of);
        return t;
    }

    // The second line's right end, DEFAULT  HIGH; QUALITY names this computer, as the column did.
    static string DefaultLineOf(Row r) => r.Key == FrontRoomsSettingsMenu.Key.Quality && r.DefaultText.Length > 0
        ? FrontRoomsLanguage.F("DEFAULT  {0}", FrontRoomsLanguage.F("{0} ON THIS COMPUTER", r.DefaultText)) : r.DefaultLine;

    // FRAME COST as three bars of rising height in a column of the list (the mock's COST column; the right column keeps only
    // pictures), headed on the section header above the first costed row. Left out where the window is too narrow for it.
    void PlaceCost(float w)
    {
        var first = -1;
        var labelEnd = 0f;
        var controlLeft = w;
        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i].Kind == Kind.Header || rows[i].Cost <= 0) continue;
            if (first < 0) first = i;
            labelEnd = Mathf.Max(labelEnd, 24f + FrontRoomsSettingsMenu.TextWidth(rows[i].Label, Mx.Label, false) * (1f + TitleGrow));
            controlLeft = Mathf.Min(controlLeft, w - FrontRoomsSettingsMenu.ControlWidth(rows[i], Mx));
        }
        if (first < 0) return;
        var x = Mathf.Round(Mathf.Max(w - CostFromRight, labelEnd + 24f));
        // Clear of every control and of the changed dot before it.
        if (x + 13f + 12f > controlLeft - 18f) return;
        for (var i = first - 1; i >= 0; i--)
        {
            if (rows[i].Kind != Kind.Header) continue;
            var head = Txt(views[i].Rect, "FRAME COST", mono, 13, new Color(Paper.r, Paper.g, Paper.b, .75f), TextAnchor.MiddleLeft);
            PlaceIn(head.rectTransform, x - 4f, 8f, 240f, 20f);
            break;
        }
        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i].Kind == Kind.Header || rows[i].Cost <= 0) continue;
            for (var b = 0; b < 3; b++)
            {
                var tall = 3f + 2f * b;
                var bar = Img(views[i].Rect, Paper, b < rows[i].Cost ? 1f : .3f);
                PlaceIn(bar.rectTransform, x + 5f * b, Mx.Row * .5f + 4f - tall, 3f, tall);
            }
        }
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
                // The keys the graph shows as the kit's keycaps (Plex Mono 13 in a 1 px keyline), then ›, which turns to ⌄ as the
                // row unfolds: Enter unfolds it. CONTROLLER's are its pad buttons (2026-10-31), in FrontRoomsInput.PadBinds' order.
                var arrow = Txt(v.Rect, "›", mono, 13, new Color(Paper.r, Paper.g, Paper.b, .8f), TextAnchor.MiddleCenter);
                v.Chevron = arrow.rectTransform;
                PlaceIn(v.Chevron, w - 16f, 0f, 14f, Mx.Row);
                v.Chevron.pivot = new Vector2(.5f, .5f);
                v.Chevron.anchoredPosition = new Vector2(w - 9f, -mid);
                var caps = Node(v.Rect, "keycaps");
                PlaceIn(caps, 0f, 0f, w, Mx.Row);
                v.Caps = caps.gameObject.AddComponent<CanvasGroup>();
                var right = w - 2f - 8f - 12f;
                var tokens = r.Options[0].Split('|');
                for (var i = tokens.Length - 1; i >= 0; i--)
                {
                    var capW = FrontRoomsSettingsMenu.TextWidth(tokens[i], 13f, true) + 12f;
                    var x = right - capW;
                    var cap = Txt(caps, tokens[i], mono, 13, new Color(Paper.r, Paper.g, Paper.b, .8f), TextAnchor.MiddleCenter);
                    PlaceIn(cap.rectTransform, x, mid - 12f, capW, 24f);
                    foreach (var e in new[] { new Rect(x, mid - 12f, capW, 1f), new Rect(x, mid + 11f, capW, 1f), new Rect(x, mid - 12f, 1f, 24f), new Rect(x + capW - 1f, mid - 12f, 1f, 24f) })
                        PlaceIn(Img(caps, Paper, .5f).rectTransform, e.x, e.y, e.width, e.height);
                    right = x - 6f;
                }
                // The scheme's word after the label (UpdateRows), never past the first keycap.
                v.TagLimit = right + 6f;
                v.Tag = Txt(v.Rect, "", mono, 13, Paper, TextAnchor.MiddleLeft);
                break;
            }
            case Kind.Language:
                // The strip draws the row, its key at the head (PlaceLanguageStrip).
                break;
            case Kind.Keys:
            {
                // A key's name, read only, set like a value (IBM Plex Mono 20).
                v.Value = Txt(v.Rect, r.ValueText, mono, 20, Paper, TextAnchor.MiddleRight);
                PlaceIn(v.Value.rectTransform, w - 320f, 0f, 320f - 8f, Mx.Row);
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
        for (var i = 0; i < rows.Count && i < views.Count; i++)
        {
            var r = rows[i];
            var v = views[i];
            if (r.Kind == Kind.Header) continue;
            var focus = i == Focus || i == hoverRow;
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
                    v.Value.text = FrontRoomsLanguage.T(on ? "ON" : "OFF");
                    v.Value.color = new Color(Paper.r, Paper.g, Paper.b, on ? 1f : .7f);
                    var inset = (Mx.SwitchH - Mx.Knob) * .5f;
                    PlaceIn(v.Knob.rectTransform, on ? w - Mx.Knob - inset : w - Mx.SwitchW + inset, Mx.Row * .5f - Mx.Knob * .5f, Mx.Knob, Mx.Knob);
                    v.Knob.color = new Color(Paper.r, Paper.g, Paper.b, on ? 1f : .3f);
                    // The open row's note says what the current state does.
                    if (v.Notes.Count > 0) v.Notes[0].text = r.Notes != null && r.Index >= 0 && r.Index < r.Notes.Length ? r.Notes[r.Index] : "";
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
                    v.Cells[0].color = FrontRoomsSettingsMenu.Armed(r) ? Accent : new Color(0f, 0f, 0f, 0f);
                    v.CellTexts[0].color = FrontRoomsSettingsMenu.Armed(r) ? Ink : Paper;
                    break;
                case Kind.Page:
                {
                    // The scheme in use says so beside its name; CONTROLLER says CONNECTED while a pad is in and idle (Red,
                    // 2026-11-01: "在一旁应该有一个indicator"), by the pad's family, or that none is in. The KEYS header's face, Plex
                    // Mono 13: ACTIVE in paper, the rest at .75. Placed with the row's motion (AnimateFold).
                    var pad = r.Key == FrontRoomsSettingsMenu.Key.PadMap;
                    var active = FrontRoomsSettingsMenu.SchemeInUse == (pad ? FrontRoomsSettingsMenu.Scheme.Pad : FrontRoomsSettingsMenu.Scheme.Keyboard);
                    var state = active ? FrontRoomsLanguage.T("ACTIVE") : pad && FrontRoomsInput.PadConnected ? FrontRoomsLanguage.T("CONNECTED") : "";
                    if (pad && FrontRoomsInput.PadConnected) state += "  ·  " + FrontRoomsInput.FamilyName(FrontRoomsInput.Family);
                    else if (pad && !active) state = FrontRoomsLanguage.T(Application.platform == RuntimePlatform.WebGLPlayer ? "PRESS A BUTTON" : "CONNECT A CONTROLLER");
                    v.Tag.text = state;
                    v.Tag.color = new Color(Paper.r, Paper.g, Paper.b, active ? 1f : .75f);
                    break;
                }
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
            }
        }
        LayoutRows();
        PlaceLanguageStrip();
        PaintCorner(back, backText, corner == Corner.Resume);
        PaintCorner(restart, restartText, corner == Corner.EndRun);
    }

    // A corner button with the focus: an Accent fill with the word in ink, as focus is everywhere in settings; else the
    // keyline alone, its word in paper.
    static void PaintCorner(Image button, Text word, bool focused)
    {
        button.color = focused ? Accent : new Color(Paper.r, Paper.g, Paper.b, 0f);
        word.color = focused ? Ink : Paper;
    }

    // At least the English word's width, so the strip starts at one x in every language (Red, 2026-11-01).
    float LanguageLabelEnd(Row r) => 24f + FrontRoomsLanguageStrip.LabelWidth(FrontRoomsSettingsMenu.TextWidth(r.Label, Mx.Label, false), Mx.Label);

    /// <summary>The strip runs from past the row's mark to the row's right edge, where every control ends.</summary>
    void PlaceLanguageStrip()
    {
        var at = -1;
        for (var i = 0; i < rows.Count && i < views.Count; i++) if (rows[i].Kind == Kind.Language) at = i;
        if (at < 0) { languageStrip.SetActive(false); return; }
        var left = FrontRoomsLanguageStrip.StartAfter(FrontRoomsLanguageStrip.Desktop, LanguageLabelEnd(rows[at]));
        languageStrip.Layout(FrontRoomsLanguageStrip.Desktop, listContent.sizeDelta.x - left);
        languageStrip.PlaceAt(left, views[at].Y);
        // Its grid (E3): five cells across the row from where its name starts, under the row's head.
        var cells = listContent.sizeDelta.x - 24f;
        languageStrip.PlaceGrid(24f - left, views[at].RestH, cells / FrontRoomsLanguageStrip.GridColumns, LangCellH);
        languageStrip.Root.SetAsLastSibling();
        languageStrip.SetActive(true);
    }

    void DrawDetail()
    {
        // The right column keeps only pictures (SETTINGS_MOTION.md): a row says what it does in its own second line, a map row and
        // LANGUAGE unfold; LANGUAGE's grid has the game's words in the language under the pointer beside it.
        var pad = FrontRoomsInput.PadInUse;
        var language = CurrentTab == Tab.Assist && langOpen;
        languagePreview.SetActive(language);
        if (language) languagePreview.Layout(DetailW, Mathf.Max(120f, root.rect.height - ListTop - (M - 16f + 38f) - 12f));
        var graphics = CurrentTab == Tab.Graphics;
        preview.gameObject.SetActive(graphics);
        preview2Frame.gameObject.SetActive(graphics);
        previewCaption.gameObject.SetActive(graphics);
        frameOsd.gameObject.SetActive(graphics);
        // The picture's line names the device in use: a pad's Y opens it (2026-10-31).
        previewCaption.text = pad ? FrontRoomsLanguage.F("{0}  PREVIEW", FrontRoomsInput.PadName(PadButton.North)) : FrontRoomsLanguage.T("CLICK THE PICTURE OR PRESS SPACE TO SEE IT FULL SCREEN");
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
        FrontRoomsInput.SuspendRestart = false;
        FrontRoomsLanguage.Changed -= OnLanguageChanged;
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

    int RowAt(PointerEventData e, out float fromRight, out float down)
    {
        fromRight = 0f;
        down = 0f;
        if (!InRect(listMask, e)) return -1;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(listContent, e.position, e.pressEventCamera, out var p);
        // listContent's pivot is its top-left: x from the left, y down from the top.
        var x = p.x + listContent.pivot.x * listContent.rect.width;
        var y = -(p.y - (1f - listContent.pivot.y) * listContent.rect.height);
        for (var i = 0; i < views.Count; i++)
        {
            var v = views[i];
            if (y < v.Y || y >= v.Y + v.H) continue;
            fromRight = listContent.sizeDelta.x - x;
            down = y - v.Y;
            // Past the rows' right end, only an unfolded graph is under the pointer.
            if (fromRight < -4f && !(v.Unfolds && down >= v.RestH)) return -1;
            return Selectable(rows[i]) ? i : -1;
        }
        return -1;
    }

    /// <summary>A pointer along the LANGUAGE strip, from <see cref="RowAt"/>'s distance to the row's right edge.</summary>
    float StripPoint(float fromRight) => languageStrip.Root.sizeDelta.x - fromRight;

    /// <summary>A pointer on the map, in its own units (top-left origin, y down).</summary>
    Vector2 MapPoint(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(page.Root, e.position, e.pressEventCamera, out var p);
        return new Vector2(p.x, -p.y) / page.Scale;
    }

    public void OnPointerMove(PointerEventData e)
    {
        if (!open || Peeking || dragRow >= 0) return;
        if (pageOpen && page.FocusAt(MapPoint(e))) DrawPageDetail();
        var i = RowAt(e, out var fromRight, out var down);
        // Hover never opens a row and never moves a bar (a list must not move under a pointer, SETTINGS_MOTION.md): the row under
        // the pointer only reads brighter.
        if (i != hoverRow) { hoverRow = i; Refresh(); }
        if (i >= 0 && rows[i].Kind == Kind.Language && langOpen)
        {
            if (down >= views[i].RestH) languageStrip.HoverCell(new Vector2(StripPoint(fromRight), down));
            else languageStrip.Reset();
        }
        else if (i >= 0 && rows[i].Kind == Kind.Language) languageStrip.Hover(StripPoint(fromRight));
        else languageStrip.Reset();
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!open) return;
        if (Peeking) { SetPeek(false); return; }
        if (pageOpen && page.Capturing) return;
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
            var at = page.ActionAt(MapPoint(e));
            if (at >= 0)
            {
                if (at == page.Focus) page.BeginCapture();
                else page.FocusAt(MapPoint(e));
                DrawPageDetail();
                return;
            }
            // Its row's name folds it and the graph's blanks do nothing; another row folds it and takes the click below.
            var under = RowAt(e, out _, out var below);
            if (under == Focus) { if (below < Mx.Row) ClosePage(); return; }
            if (under < 0) return;
            ClosePage();
        }
        var row = RowAt(e, out var fromRight, out var down);
        if (row < 0) return;
        corner = Corner.None;
        armed = false;
        var r = rows[row];
        // A click opens its row (hover never does) and works the control under it: on the row's first line, or under a choice on
        // its second. A click on the open row's name closes it (SETTINGS_MOTION.md).
        var wasOpen = openRow == row;
        Focus = row;
        var used = false;
        switch (r.Kind)
        {
            case Kind.Page: SetOpenRow(-1); OpenPage(); return;
            case Kind.Language:
            {
                // Unfolded: a cell picks and the row's head folds it. Folded: a charm on the rail picks; the key, the name or the
                // rest of the row unfolds it.
                SetOpenRow(-1);
                if (langOpen)
                {
                    if (down >= views[row].RestH) languageStrip.Pick(languageStrip.AtCell(new Vector2(StripPoint(fromRight), down)));
                    else CloseLanguage();
                }
                else
                {
                    var at = languageStrip.At(StripPoint(fromRight));
                    if (at >= 0 && FrontRoomsLanguage.ListedLanguages[at] != FrontRoomsLanguage.Current) languageStrip.Pick(at);
                    else OpenLanguage();
                }
                Refresh();
                return;
            }
            case Kind.Switch:
            case Kind.Action:
                used = down < Mx.Row && fromRight <= FrontRoomsSettingsMenu.ControlWidth(r, Mx) + 4f;
                if (used) FrontRoomsSettingsMenu.Activate(r);
                break;
            case Kind.Segments:
            {
                var seg = FrontRoomsSettingsMenu.SegmentAt(r, fromRight, Mx);
                used = seg >= 0;
                if (used && seg != r.Index) FrontRoomsSettingsMenu.Set(r, seg);
                break;
            }
            case Kind.Slider:
            {
                var stop = FrontRoomsSettingsMenu.StopAt(r, fromRight, Mx, 12f);
                used = stop >= 0;
                if (used) { dragRow = row; if (stop != r.Index) FrontRoomsSettingsMenu.Set(r, stop); }
                break;
            }
        }
        if (!wasOpen) SetOpenRow(row);
        else if (!used && down < Mx.Row) SetOpenRow(-1);
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
        if (!open || Peeking) return;
        scroll = Mathf.Clamp(scroll - e.scrollDelta.y * 48f, 0f, Mathf.Max(0f, contentH - listMask.rect.height));
        scrollFrom = scrollTo = scroll;
        scrollAt = -100f;
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

    // The HUD's keyline (FrontRoomsHudStyle), for words and marks drawn over the live picture.
    static void Keyline(Graphic graphic)
    {
        var outline = graphic.gameObject.AddComponent<Outline>();
        var k = FrontRoomsHudStyle.KeylineColor;
        outline.effectColor = new Color(k.r, k.g, k.b, FrontRoomsHudStyle.OutlineTapAlpha);
        outline.effectDistance = new Vector2(FrontRoomsHudStyle.KeylineDistance, -FrontRoomsHudStyle.KeylineDistance);
    }

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
        t.font = font; t.fontSize = size; t.color = color; t.alignment = anchor; t.text = FrontRoomsLanguage.T(value);
        FrontRoomsLanguage.Style(t, value);
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
