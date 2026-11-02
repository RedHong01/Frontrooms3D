using UnityEngine;
using UnityEngine.UI;
using Language = FrontRoomsLanguage.Language;

/// <summary>
/// The LANGUAGE row's picture (SETTINGS_MOTION.md, LANGUAGE preview; Red, 2026-11-01: "这里我需要游戏内实际的的"): the game's own
/// words in the language under the pointer, before it is picked, as the GRAPHICS picture shows the game at new settings. On top
/// is the Level 0 evacuation plan the first room hangs, its real print (Prop_EvacPlan_A_Room, English in every language, as on
/// the wall); under it the plan's TEXT panel as the close-up draws it (平面视觉 G9: #0E0E0D at 97 %, IBM Plex Mono heads, Source
/// Serif 4 lines; each language's own faces), its words in that language. The C20 terminal is not shown: its words are not in
/// the tables yet, so it would read the same in every language.
///
/// A host places Root in its picture column, calls Layout with the column's size, Show(language) as the pointer or the keyboard
/// moves along the strip, and Animate every frame while it shows. Positions are top-left origin, y down.
/// </summary>
public sealed class FrontRoomsLanguagePreview
{
    const string PlanArt = "Surfaces/Textures/Prop_EvacPlan_A_Room";
    const float PlanAspect = 2048f / 4096f;
    // The close-up's panel is 36 % of a 1920 px screen (FrontRoomsInspect.TranscriptPanelShare); the preview draws it at the
    // column's width, so its type keeps G9's proportions: Plex Mono 13/16 heads, Source Serif 4 20/23 lines.
    const float PanelPx = 1920f * .36f;
    const float HeadSize = 13f, HeadPitch = 16f, LineSize = 20f, LinePitch = 23f, Pad = 16f, RegionGap = 8f, Spacing = 4f;
    // A line's own height at a pitch of 1 (the touch panel's measures), so a pitch sets the leading exactly.
    const float SerifLineEm = 1.371f, PlexLineEm = 1.3f;
    const float FadeSeconds = .18f, Gap = 12f;
    static readonly Color Surface = new Color32(0x0E, 0x0E, 0x0D, 0xF7);
    static readonly Color Paper = new Color32(0xF4, 0xF1, 0xE8, 0xFF);

    sealed class Page
    {
        public RectTransform Root;
        public CanvasGroup Group;
        public Language Language;
        public float Alpha;
    }

    readonly Page[] pages = new Page[2];
    readonly RawImage plan;
    readonly RectTransform panel;
    readonly Font serif, plex;
    int front;
    float width = -1f, height = -1f;

    public RectTransform Root { get; }
    /// <summary>The language the preview shows now (fading in, or in).</summary>
    public Language Showing => pages[front].Language;

    public static FrontRoomsLanguagePreview Create(Transform parent, Font serif = null, Font plex = null) => new FrontRoomsLanguagePreview(parent, serif, plex);

    FrontRoomsLanguagePreview(Transform parent, Font serif, Font plex)
    {
        this.serif = serif != null ? serif : Resources.Load<Font>("Fonts/SourceSerif4-Variable");
        this.plex = plex != null ? plex : Resources.Load<Font>("Fonts/IBMPlexMono-Regular");
        Root = Node("Language preview", parent);
        plan = new GameObject("plan", typeof(RectTransform)).AddComponent<RawImage>();
        plan.transform.SetParent(Root, false);
        plan.texture = Resources.Load<Texture2D>(PlanArt);
        plan.raycastTarget = false;
        panel = Node("TEXT panel", Root);
        panel.gameObject.AddComponent<RectMask2D>();
        var surface = new GameObject("surface", typeof(RectTransform)).AddComponent<Image>();
        surface.transform.SetParent(panel, false);
        surface.color = Surface;
        surface.raycastTarget = false;
        var sr = surface.rectTransform;
        sr.anchorMin = Vector2.zero; sr.anchorMax = Vector2.one; sr.offsetMin = sr.offsetMax = Vector2.zero;
        for (var i = 0; i < pages.Length; i++)
        {
            var page = new Page { Root = Node("page", panel) };
            page.Group = page.Root.gameObject.AddComponent<CanvasGroup>();
            page.Group.alpha = 0f;
            page.Group.blocksRaycasts = false;
            pages[i] = page;
        }
    }

    public void SetActive(bool value)
    {
        if (value == Root.gameObject.activeSelf) return;
        Root.gameObject.SetActive(value);
    }

    /// <summary>Fits the column: the plan at the column's width, the panel under it down to the column's foot.</summary>
    public void Layout(float columnWidth, float columnHeight)
    {
        if (columnWidth == width && columnHeight == height) return;
        width = columnWidth;
        height = columnHeight;
        Root.sizeDelta = new Vector2(width, height);
        var planH = width * PlanAspect;
        Place(plan.rectTransform, 0f, 0f, width, planH);
        Place(panel, 0f, planH + Gap, width, Mathf.Max(0f, height - planH - Gap));
        foreach (var page in pages) if (page.Language != null) Build(page, page.Language);
    }

    /// <summary>The language under the pointer, or null for the current one. A new language cross-fades in over the last.</summary>
    public void Show(Language language)
    {
        if (language == null) language = FrontRoomsLanguage.Current;
        if (pages[front].Language == language) return;
        front = 1 - front;
        Build(pages[front], language);
        pages[front].Root.SetAsLastSibling();
        if (FrontRoomsHandheld.ReduceMotion)
        {
            pages[front].Alpha = 1f;
            pages[1 - front].Alpha = 0f;
        }
    }

    /// <summary>Every frame while it shows: unscaled time, the game being paused behind the sheet.</summary>
    public void Animate()
    {
        var step = FrontRoomsHandheld.ReduceMotion ? 1f : Time.unscaledDeltaTime / FadeSeconds;
        for (var i = 0; i < pages.Length; i++)
        {
            var page = pages[i];
            page.Alpha = Mathf.MoveTowards(page.Alpha, i == front && page.Language != null ? 1f : 0f, step);
            // The new words ease out as they arrive; the old ones simply go.
            page.Group.alpha = i == front ? 1f - (1f - page.Alpha) * (1f - page.Alpha) : page.Alpha;
        }
    }

    // The panel as FrontRoomsTouchControlsView.BuildTranscript sets it, at G9's desktop sizes and in the given language: heads
    // in capitals, a region whose first line repeats its head says it once, glow lines left out (the plan read in ordinary light).
    void Build(Page page, Language language)
    {
        page.Language = language;
        for (var i = page.Root.childCount - 1; i >= 0; i--) Object.Destroy(page.Root.GetChild(i).gameObject);
        var panelW = panel.sizeDelta.x;
        var panelH = panel.sizeDelta.y;
        Place(page.Root, 0f, 0f, panelW, panelH);
        if (panelW <= 0f) return;
        var lines = FrontRoomsInspectCatalog.Transcript(FrontRoomsInspectCatalog.StationPlacardTranscript(true));
        if (lines == null) return;
        var s = panelW / PanelPx;
        var lineFace = FrontRoomsLanguage.TextFaceOf(language, serif);
        var headFace = FrontRoomsLanguage.MetaFaceOf(language, plex);
        var pad = Pad * s;
        var inner = panelW - pad * 2f;
        var y = pad;
        var first = true;
        string lastHead = null;
        foreach (var raw in lines)
        {
            if (string.IsNullOrEmpty(raw) || y > panelH) continue;
            var head = raw[0] == '\u0002';
            if (raw[0] == '\u0001') continue;
            var english = head ? raw.Substring(1) : raw;
            if (!head && lastHead != null && string.Equals(english.Trim(), lastHead, System.StringComparison.OrdinalIgnoreCase)) { lastHead = null; continue; }
            lastHead = head ? english.Trim() : null;
            if (head && !first) y += RegionGap * s;
            var words = FrontRoomsLanguage.TIn(language, english);
            var pitch = (head ? HeadPitch : LinePitch) * s;
            var size = Mathf.Max(1, Mathf.RoundToInt((head ? HeadSize : LineSize) * s));
            var natural = size * (head ? PlexLineEm : SerifLineEm);
            var t = new GameObject(head ? "head" : "line", typeof(RectTransform)).AddComponent<Text>();
            t.transform.SetParent(page.Root, false);
            t.font = head ? headFace : lineFace;
            t.fontSize = size;
            t.color = Paper;
            t.alignment = TextAnchor.UpperLeft;
            t.text = head ? words.ToUpperInvariant() : words;
            t.supportRichText = false;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = pitch / natural;
            var rt = t.rectTransform;
            Place(rt, pad, 0f, inner, 0f);
            _ = t.preferredHeight; // lays the text out at this width, so the layout generator holds its line count
            var h = Mathf.Max(1, t.cachedTextGeneratorForLayout.lineCount) * pitch;
            Place(rt, pad, y - (natural - pitch) * .5f, inner, h);
            y += h + Spacing * s;
            first = false;
        }
    }

    static RectTransform Node(string name, Transform parent)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        return rt;
    }

    static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
    }
}
