using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Language = FrontRoomsLanguage.Language;

/// <summary>
/// ASSIST / LANGUAGE (Red, 2026-10-30: every language in Settings; A · TAG): each language by its own name, with its key
/// tag, on the desktop sheet and the touch sheet alike. Spec: Documentation/research/localization/README.md §7 and
/// research/desktop_ui/LANGUAGE_MARKS.md (sizes, states, motion); Figma 3834:8767, lane A.
///
/// A rack of tiles, not a table (Red, 2026-10-30: the list wasted the phone's width and the desktop's): as many columns
/// as the longest name allows, up to four, then each tile takes its share of the height, and the tag and the name grow
/// with it. Every tag hangs by its ring from the rail above it and swings from there on a pick.
///
/// The yellow bar is focus and moves without changing anything; a pick is instant and the page stays open. The current
/// language is the filled tag, never a colour. Positions are in the page's own units, top-left origin, y down: the sheet
/// that hosts it places Root and hands it input.
/// </summary>
public sealed class FrontRoomsLanguagePage
{
    public struct Metrics
    {
        public float Row, TagW, TagH, TagCentre, TagGap, Inset, Gutter;
        public int Token, TokenCjk, Name;
    }

    public static readonly Metrics Desktop = new Metrics { Row = 48f, TagW = 40f, TagH = 28f, TagCentre = 16.9f, TagGap = 16f, Inset = 24f, Gutter = 24f, Token = 13, TokenCjk = 14, Name = 20 };
    public static readonly Metrics Phone = new Metrics { Row = 44f, TagW = 34f, TagH = 23.8f, TagCentre = 14.4f, TagGap = 13.6f, Inset = 16f, Gutter = 8f, Token = 11, TokenCjk = 12, Name = 17 };
    public static readonly Metrics Tablet = new Metrics { Row = 44f, TagW = 38f, TagH = 26.6f, TagCentre = 16.1f, TagGap = 15.2f, Inset = 16f, Gutter = 8f, Token = 12, TokenCjk = 13, Name = 19 };

    static readonly Color Paper = new Color(.957f, .945f, .91f, 1f);
    static readonly Color Ink = new Color32(0x14, 0x14, 0x14, 0xFF);
    static readonly Color Accent = new Color(.957f, .875f, .231f, 1f);
    // The tag hangs from, and swings about, the top of its ring: (20, 2) in a 40 × 28 tag.
    const float PivotUnits = 2f, TagUnits = 28f;
    static readonly Vector2 TagPivot = new Vector2(.5f, 1f - PivotUnits / TagUnits);
    const float FillSeconds = .12f, DimAlpha = .5f;
    const float SwingDegrees = 9f, SwingSeconds = 1.5f;
    const float NameRight = 24f, MarkGap = 14f;
    const int MaxColumns = 4;
    // The tag and the name fill this much of a tile's height, growing up to this far.
    const float TileFill = .6f, MaxGrowth = 1.75f;
    // The longest name keeps some of its rail, so it never runs into the next column.
    const float MaxNameShare = .85f;
    // Under the rail: the tag, this gap (in the tag's scale), the name's line.
    const float NameGap = 8f, NameLine = 1.25f;

    sealed class RowView
    {
        public Language Language;
        public RectTransform Node, Tag;
        public Image Outline, Fill, Rail;
        public Text Token, Name;
        public Rect Rect;
        public float Filled;
    }

    readonly List<RowView> rows = new List<RowView>();
    readonly Image focusBar, pressedWash;
    Metrics m;
    float laidW = -1f, laidH = -1f, contentH;
    int swinging = -1;
    float swungAt;

    public RectTransform Root { get; }
    /// <summary>The row with the yellow bar, or -1: keyboard, hover, or the last tapped row.</summary>
    public int Focus { get; set; } = -1;
    /// <summary>The row under a finger, or -1.</summary>
    public int Pressed { get; set; } = -1;
    public int Columns { get; private set; } = 1;
    public int PerColumn { get; private set; }
    public int Count => rows.Count;

    public static FrontRoomsLanguagePage Create(Transform parent, Font serif) => new FrontRoomsLanguagePage(parent, serif);

    FrontRoomsLanguagePage(Transform parent, Font serif)
    {
        Root = Node("Language page", parent);
        pressedWash = Solid(Root, new Color(Paper.r, Paper.g, Paper.b, 0f));
        foreach (var language in FrontRoomsLanguage.ListedLanguages) rows.Add(MakeRow(language, serif));
        focusBar = Solid(Root, Accent);
        focusBar.enabled = false;
    }

    RowView MakeRow(Language language, Font serif)
    {
        var r = new RowView { Language = language };
        r.Node = Node(language.Code, Root);
        r.Rail = Solid(r.Node, new Color(Paper.r, Paper.g, Paper.b, .16f));
        r.Tag = MakeTag(r.Node, out r.Outline, out r.Fill);
        r.Token = Word(r.Tag, language.Token, TokenFace(language), 13, Paper, TextAnchor.MiddleCenter);
        r.Name = Word(r.Node, language.OwnName, NameFace(language, serif), 20, Paper, TextAnchor.MiddleLeft);
        return r;
    }

    public void SetActive(bool value) => Root.gameObject.SetActive(value);

    /// <summary>The page as it opens: the current tag filled, nothing swinging, the bar on the current language for a
    /// keyboard or a pad and nowhere for a finger.</summary>
    public void Open(bool focusCurrent)
    {
        swinging = -1;
        Pressed = -1;
        Focus = -1;
        for (var i = 0; i < rows.Count; i++)
        {
            var current = rows[i].Language == FrontRoomsLanguage.Current;
            rows[i].Filled = current ? 1f : 0f;
            rows[i].Tag.localRotation = Quaternion.identity;
            if (current && focusCurrent) Focus = i;
        }
        Animate();
    }

    /// <summary>How the rack divides: as many columns as fit, up to four, then the fewest lines, then no empty last column.</summary>
    public static void Grid(int count, float width, float minimumColumn, out int columns, out int lines)
    {
        var fit = Mathf.Clamp(Mathf.FloorToInt(width / Mathf.Max(1f, minimumColumn)), 1, MaxColumns);
        lines = Mathf.CeilToInt(count / (float)fit);
        columns = Mathf.CeilToInt(count / (float)Mathf.Max(1, lines));
    }

    /// <summary>Every name on the page with no scrolling, the tiles sharing the whole width and height (README §7.2).</summary>
    public void Layout(Metrics metrics, float width, float height)
    {
        if (metrics.Row == m.Row && metrics.TagW == m.TagW && width == laidW && height == laidH) return;
        m = metrics;
        laidW = width;
        laidH = height;
        var longest = 1f;
        foreach (var r in rows)
        {
            r.Name.fontSize = m.Name;
            longest = Mathf.Max(longest, r.Name.preferredWidth);
        }
        Grid(rows.Count, width + m.Gutter, m.Inset + longest + m.TagGap + m.Gutter, out var columns, out var lines);
        Columns = columns;
        PerColumn = lines;
        var step = (width + m.Gutter) / columns;
        var rail = step - m.Inset - m.Gutter;
        var tile = height / lines;
        var hanging = m.TagH * (TagUnits - PivotUnits) / TagUnits;
        var grow = TileFill * tile / (hanging + NameGap + m.Name * NameLine);
        var k = Mathf.Clamp(Mathf.Floor(grow * 8f) / 8f, 1f, Mathf.Min(MaxGrowth, Mathf.Max(1f, MaxNameShare * rail / longest)));
        for (var i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            r.Rect = new Rect(i / lines * step, i % lines * tile, step - m.Gutter, tile);
            Place(r.Node, r.Rect.x, r.Rect.y, r.Rect.width, tile);
            Place(r.Rail.rectTransform, m.Inset, 0f, rail, 1f);
            var w = m.TagW * k;
            var h = m.TagH * k;
            r.Tag.sizeDelta = new Vector2(w, h);
            r.Tag.anchoredPosition = new Vector2(m.Inset + w * TagPivot.x, 0f);
            r.Token.fontSize = Mathf.RoundToInt((Cjk(r.Language.Token) ? m.TokenCjk : m.Token) * k);
            Place(r.Token.rectTransform, 0f, m.TagCentre * k - m.Row, w, m.Row * 2f);
            var line = m.Name * k * NameLine;
            r.Name.fontSize = Mathf.RoundToInt(m.Name * k);
            Place(r.Name.rectTransform, m.Inset, (hanging + NameGap) * k, rail, line);
            contentH = (hanging + NameGap) * k + line;
        }
        Root.sizeDelta = new Vector2(width, height);
    }

    public void PlaceAt(float x, float y) => Root.anchoredPosition = new Vector2(x, -y);

    /// <summary>The row at a point in page units, or -1.</summary>
    public int At(Vector2 p)
    {
        for (var i = 0; i < rows.Count; i++) if (rows[i].Rect.Contains(p)) return i;
        return -1;
    }

    /// <summary>Arrows: down the list and on through the next column, or across to the same line of the other column.
    /// Nothing changes until a pick.</summary>
    public void Navigate(int across, int down)
    {
        if (rows.Count == 0) return;
        if (Focus < 0) { Open(true); return; }
        if (down != 0) Focus = (Focus + down + rows.Count) % rows.Count;
        else if (across != 0 && Columns > 1)
        {
            var to = Focus + across * PerColumn;
            if (to >= 0 && to < rows.Count) Focus = to;
        }
    }

    public void PickFocused() => Pick(Focus);

    /// <summary>The pick: saved, in force at once, the page stays open. The new tag swings; any other stops at rest.</summary>
    public void Pick(int index)
    {
        if (index < 0 || index >= rows.Count) return;
        Focus = index;
        if (swinging >= 0) rows[swinging].Tag.localRotation = Quaternion.identity;
        swinging = index;
        swungAt = Time.unscaledTime;
        FrontRoomsLanguage.Pick(rows[index].Language);
    }

    /// <summary>Every frame while the page is open: unscaled time, the game being paused behind the sheet.</summary>
    public void Animate()
    {
        var reduced = FrontRoomsHandheld.ReduceMotion;
        var step = reduced ? 1f : Time.unscaledDeltaTime / FillSeconds;
        for (var i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            var current = r.Language == FrontRoomsLanguage.Current;
            r.Filled = Mathf.MoveTowards(r.Filled, current ? 1f : 0f, step);
            // D5: test builds show what is not finished, dimmed. The current language stays whole.
            var alpha = r.Language.Finished || current ? 1f : DimAlpha;
            var paper = new Color(Paper.r, Paper.g, Paper.b, alpha);
            r.Outline.color = paper;
            r.Fill.color = new Color(Paper.r, Paper.g, Paper.b, r.Filled);
            r.Token.color = Color.Lerp(paper, Ink, r.Filled);
            r.Name.color = paper;
        }
        if (swinging >= 0)
        {
            var t = Time.unscaledTime - swungAt;
            var done = reduced || t > SwingSeconds;
            // The HUD key's own U·3 kick; clockwise is negative here.
            rows[swinging].Tag.localRotation = Quaternion.Euler(0f, 0f, done ? 0f : -FrontRoomsKeyHudMotion.SwingKick(t, SwingDegrees));
            if (done) swinging = -1;
        }
        focusBar.enabled = Focus >= 0 && Focus < rows.Count;
        if (focusBar.enabled)
        {
            var f = rows[Focus].Rect;
            Place(focusBar.rectTransform, f.x, f.y, 4f, contentH);
        }
        var wash = Pressed >= 0 && Pressed < rows.Count;
        pressedWash.color = new Color(Paper.r, Paper.g, Paper.b, wash ? .07f : 0f);
        if (wash)
        {
            var p = rows[Pressed].Rect;
            Place(pressedWash.rectTransform, p.x, p.y, p.width, p.height);
        }
    }

    // ------------------------------------------------------------- the row in ASSIST

    /// <summary>The LANGUAGE row's control: after the word, the outlined A文 that is the same in every language, so a player
    /// lost in one they cannot read still finds it; at the right, the current tag, name and chevron. It fills
    /// <paramref name="row"/>; <paramref name="labelEnd"/> is where the word ends.</summary>
    public static void BuildRowControl(RectTransform row, Metrics m, float labelEnd, Font serif)
    {
        var mark = MakeTag(row, out var outline, out var unused);
        unused.enabled = false;
        outline.color = Paper;
        PlaceTag(mark, labelEnd + MarkGap, m.Row * .5f - m.TagCentre, m);
        var a = Word(mark, "A", TokenFont, m.Token, Paper, TextAnchor.MiddleCenter);
        var wen = Word(mark, "文", TokenCjkFont, m.Token, Paper, TextAnchor.MiddleCenter);
        var aW = m.Token * .6f;
        var left = (m.TagW - aW - m.Token) * .5f;
        Place(a.rectTransform, left, m.TagCentre - m.Row * .5f, aW, m.Row);
        Place(wen.rectTransform, left + aW, m.TagCentre - m.Row * .5f, m.Token, m.Row);

        var language = FrontRoomsLanguage.Current;
        var chevron = Word(row, "›", TokenFont, m.Token, new Color(Paper.r, Paper.g, Paper.b, .8f), TextAnchor.MiddleRight);
        PlaceRight(chevron.rectTransform, 2f, 0f, 28f, m.Row);
        var name = Word(row, language.OwnName, NameFace(language, serif), m.Name, Paper, TextAnchor.MiddleRight);
        var nameW = Mathf.Ceil(name.preferredWidth);
        PlaceRight(name.rectTransform, NameRight, 0f, nameW + 4f, m.Row);
        var tag = MakeTag(row, out outline, out var fill);
        outline.color = Paper;
        fill.color = Paper;
        var tagRight = NameRight + nameW + m.TagGap;
        tag.anchorMin = tag.anchorMax = new Vector2(1f, 1f);
        tag.sizeDelta = new Vector2(m.TagW, m.TagH);
        tag.anchoredPosition = new Vector2(-tagRight - m.TagW * (1f - tag.pivot.x), -(m.Row * .5f - m.TagCentre) - m.TagH * (1f - tag.pivot.y));
        var token = Word(tag, language.Token, TokenFace(language), Cjk(language.Token) ? m.TokenCjk : m.Token, Ink, TextAnchor.MiddleCenter);
        Place(token.rectTransform, 0f, m.TagCentre - m.Row * .5f, m.TagW, m.Row);
    }

    // ------------------------------------------------------------- faces

    // Tokens and names ship in every language (LANGUAGE_MARKS.md, type by script): the tiny subsets below are always loaded.
    // Until they exist the fallback face sets the characters it can and the system sets the rest.
    static Font tokenFont, tokenCjkFont;
    static Font TokenFont => tokenFont != null ? tokenFont : tokenFont = Resources.Load<Font>("Fonts/IBMPlexMono-Medium");
    static Font TokenCjkFont
    {
        get
        {
            if (tokenCjkFont == null) tokenCjkFont = Resources.Load<Font>("Fonts/LanguageTokens");
            return tokenCjkFont != null ? tokenCjkFont : TokenFont;
        }
    }

    static Font TokenFace(Language language) => Cjk(language.Token) ? TokenCjkFont : TokenFont;

    static Font NameFace(Language language, Font serif)
    {
        var subset = Resources.Load<Font>("Fonts/LanguageName-" + language.Code);
        return subset != null ? subset : serif;
    }

    static bool Cjk(string s) => s.Length > 0 && s[0] >= '⺀';

    // ------------------------------------------------------------- parts

    static RectTransform MakeTag(Transform parent, out Image outline, out Image fill)
    {
        var tag = Node("tag", parent);
        tag.pivot = TagPivot;
        fill = Stretched(tag, "LangTag_Fill");
        outline = Stretched(tag, "LangTag_Outline");
        return tag;
    }

    static Image Stretched(RectTransform parent, string sprite)
    {
        var image = new GameObject(sprite, typeof(RectTransform)).AddComponent<Image>();
        image.transform.SetParent(parent, false);
        image.sprite = Resources.Load<Sprite>("Language/" + sprite);
        image.raycastTarget = false;
        var rt = image.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        return image;
    }

    static void PlaceTag(RectTransform tag, float x, float y, Metrics m)
    {
        tag.sizeDelta = new Vector2(m.TagW, m.TagH);
        tag.anchoredPosition = new Vector2(x + m.TagW * tag.pivot.x, -y - m.TagH * (1f - tag.pivot.y));
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

    static void PlaceRight(RectTransform rt, float right, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-right, -y);
        rt.sizeDelta = new Vector2(w, h);
    }

    static Image Solid(Transform parent, Color color)
    {
        var image = new GameObject("img", typeof(RectTransform)).AddComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static Text Word(Transform parent, string value, Font font, int size, Color color, TextAnchor anchor)
    {
        var t = new GameObject("text", typeof(RectTransform)).AddComponent<Text>();
        t.transform.SetParent(parent, false);
        t.font = font; t.fontSize = size; t.color = color; t.alignment = anchor; t.text = value;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false; t.supportRichText = false;
        return t;
    }
}
