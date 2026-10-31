using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Language = FrontRoomsLanguage.Language;

/// <summary>
/// The LANGUAGE control (Red, 2026-10-30: every language in Settings; A · TAG), the same on the desktop sheet and the touch
/// sheet. One row, ASSIST's first, with no page behind it: the chosen language opens out to its key tag and its own name, and
/// every other one folds to its tag (Red: "只有被选择的那个是展开的，其他就是缩减版只有图标"). Spec:
/// Documentation/research/localization/README.md §7 and research/desktop_ui/LANGUAGE_MARKS.md (sizes, states, motion).
///
/// The filled tag is the current language, never a colour. A keyboard or a pointer can open another tag to read its name
/// without choosing it; a pick is instant. Positions are in the strip's own units, top-left origin, y down: the sheet that
/// hosts it places Root and hands it input.
/// </summary>
public sealed class FrontRoomsLanguageStrip
{
    public struct Metrics
    {
        public float Row, TagW, TagH, TagCentre, NameGap, Gap;
        public int Token, TokenCjk, Name;
    }

    public static readonly Metrics Desktop = new Metrics { Row = 48f, TagW = 40f, TagH = 28f, TagCentre = 16.9f, NameGap = 16f, Gap = 10f, Token = 13, TokenCjk = 14, Name = 20 };
    public static readonly Metrics Phone = new Metrics { Row = 44f, TagW = 34f, TagH = 23.8f, TagCentre = 14.4f, NameGap = 13.6f, Gap = 10f, Token = 11, TokenCjk = 12, Name = 17 };
    public static readonly Metrics Tablet = new Metrics { Row = 44f, TagW = 38f, TagH = 26.6f, TagCentre = 16.1f, NameGap = 15.2f, Gap = 10f, Token = 12, TokenCjk = 13, Name = 19 };

    static readonly Color Paper = new Color(.957f, .945f, .91f, 1f);
    static readonly Color Ink = new Color32(0x14, 0x14, 0x14, 0xFF);
    // The tag swings about the top of its ring: (20, 2) in a 40 × 28 tag.
    static readonly Vector2 TagPivot = new Vector2(.5f, 1f - 2f / 28f);
    const float FillSeconds = .12f, OpenSeconds = .16f, DimAlpha = .5f;
    const float SwingDegrees = 9f, SwingSeconds = 1.5f;
    public const float MarkGap = 14f;
    const float MinGap = 2f, MinNameScale = .7f;

    sealed class Slot
    {
        public Language Language;
        public RectTransform Tag, Clip;
        public Image Outline, Fill;
        public Text Token, Name;
        public float NameW, Filled, Open, X, W, After;
    }

    readonly List<Slot> slots = new List<Slot>();
    readonly Image rule, pressedWash;
    Metrics m;
    float laidW = -1f, gap;
    int swinging = -1;
    float swungAt;

    public RectTransform Root { get; }
    /// <summary>The tag opened out to its name: the current language, or where a keyboard or a pointer has gone.</summary>
    public int Highlight { get; set; }
    /// <summary>The tag under a finger, or -1.</summary>
    public int Pressed { get; set; } = -1;
    public int Count => slots.Count;
    /// <summary>The width the tags take now, against the right edge.</summary>
    public float ContentWidth { get; private set; }

    public static FrontRoomsLanguageStrip Create(Transform parent, Font serif) => new FrontRoomsLanguageStrip(parent, serif);

    FrontRoomsLanguageStrip(Transform parent, Font serif)
    {
        Root = Node("Language strip", parent);
        rule = Solid(Root, new Color(Paper.r, Paper.g, Paper.b, .08f));
        rule.enabled = false;
        pressedWash = Solid(Root, new Color(Paper.r, Paper.g, Paper.b, 0f));
        foreach (var language in FrontRoomsLanguage.ListedLanguages)
        {
            var s = new Slot { Language = language };
            s.Tag = MakeTag(Root, out s.Outline, out s.Fill);
            s.Token = Word(s.Tag, language.Token, TokenFace(language), 13, Paper, TextAnchor.MiddleCenter);
            s.Clip = Node(language.Code, Root);
            s.Clip.gameObject.AddComponent<RectMask2D>();
            s.Name = Word(s.Clip, language.OwnName, NameFace(language, serif), 20, Paper, TextAnchor.MiddleLeft);
            slots.Add(s);
        }
    }

    public void SetActive(bool value)
    {
        if (value == Root.gameObject.activeSelf) return;
        Root.gameObject.SetActive(value);
        if (value) Snap();
    }

    /// <summary>Back to the current language.</summary>
    public void Reset()
    {
        for (var i = 0; i < slots.Count; i++) if (slots[i].Language == FrontRoomsLanguage.Current) Highlight = i;
    }

    /// <summary>Everything where it belongs, with no motion: the strip as it appears.</summary>
    public void Snap()
    {
        Reset();
        swinging = -1;
        Pressed = -1;
        for (var i = 0; i < slots.Count; i++)
        {
            slots[i].Open = i == Highlight ? 1f : 0f;
            slots[i].Filled = slots[i].Language == FrontRoomsLanguage.Current ? 1f : 0f;
            slots[i].Tag.localRotation = Quaternion.identity;
        }
        Animate();
    }

    /// <summary>Fits the widest case, the longest name open, into <paramref name="width"/>: the gaps close first, then that name shrinks.</summary>
    public void Layout(Metrics metrics, float width)
    {
        if (metrics.Row == m.Row && metrics.TagW == m.TagW && width == laidW) return;
        m = metrics;
        laidW = width;
        var longest = 1f;
        foreach (var s in slots)
        {
            s.Name.fontSize = m.Name;
            s.NameW = Mathf.Ceil(s.Name.preferredWidth);
            longest = Mathf.Max(longest, s.NameW);
        }
        // The open name has a name gap on both sides, so it reads as its own tag's, not the next one's.
        var tags = slots.Count * m.TagW + 2f * m.NameGap;
        var between = Mathf.Max(1, slots.Count - 2);
        gap = Mathf.Clamp((width - tags - longest) / between, MinGap, m.Gap);
        var scale = Mathf.Clamp((width - tags - gap * between) / longest, MinNameScale, 1f);
        foreach (var s in slots)
        {
            s.Name.fontSize = Mathf.FloorToInt(m.Name * scale);
            s.NameW = Mathf.Ceil(s.Name.preferredWidth);
            s.Tag.sizeDelta = new Vector2(m.TagW, m.TagH);
            s.Token.fontSize = Cjk(s.Language.Token) ? m.TokenCjk : m.Token;
            Place(s.Token.rectTransform, 0f, m.TagCentre - m.Row * .5f, m.TagW, m.Row);
            Place(s.Name.rectTransform, 0f, 0f, s.NameW + 2f, m.Row);
        }
        Root.sizeDelta = new Vector2(width, m.Row);
    }

    public void PlaceAt(float x, float y) => Root.anchoredPosition = new Vector2(x, -y);

    /// <summary>The row's rule where the host's own stops short of the strip, in the strip's units.</summary>
    public void DrawRule(float x, float width)
    {
        rule.enabled = width > 0f;
        Place(rule.rectTransform, x, m.Row - 1f, width, 1f);
    }

    /// <summary>The tag at a distance along the strip, or -1. Each tag's target runs to the middle of the gap beside it.</summary>
    public int At(float x)
    {
        for (var i = 0; i < slots.Count; i++)
        {
            var before = i > 0 ? slots[i - 1].After : gap;
            var after = i < slots.Count - 1 ? slots[i].After : gap;
            if (x >= slots[i].X - before * .5f && x < slots[i].X + slots[i].W + after * .5f) return i;
        }
        return -1;
    }

    /// <summary>A pointer moving along the strip opens the tag under it; off the strip, the current language.</summary>
    public void Hover(float x)
    {
        var at = At(x);
        if (at >= 0) Highlight = at;
        else Reset();
    }

    /// <summary>← →: the next tag opens, and nothing changes until a pick.</summary>
    public void Step(int direction) => Highlight = Mathf.Clamp(Highlight + direction, 0, slots.Count - 1);

    public void PickHighlighted() => Pick(Highlight);

    /// <summary>The pick: saved, in force at once. The new tag swings; any other stops at rest.</summary>
    public void Pick(int index)
    {
        if (index < 0 || index >= slots.Count) return;
        Highlight = index;
        if (swinging >= 0) slots[swinging].Tag.localRotation = Quaternion.identity;
        swinging = index;
        swungAt = Time.unscaledTime;
        FrontRoomsLanguage.Pick(slots[index].Language);
    }

    /// <summary>The middle of a tag along the strip, for a test's tap.</summary>
    public float TagCentreX(int index) => slots[index].X + m.TagW * .5f;

    /// <summary>Every frame while it shows: unscaled time, the game being paused behind the sheet.</summary>
    public void Animate()
    {
        var reduced = FrontRoomsHandheld.ReduceMotion;
        var fill = reduced ? 1f : Time.unscaledDeltaTime / FillSeconds;
        var open = reduced ? 1f : Time.unscaledDeltaTime / OpenSeconds;
        ContentWidth = 0f;
        for (var i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            s.Open = Mathf.MoveTowards(s.Open, i == Highlight ? 1f : 0f, open);
            var opened = Mathf.SmoothStep(0f, 1f, s.Open);
            s.W = m.TagW + opened * (m.NameGap + s.NameW);
            ContentWidth += s.W;
        }
        // The gaps give way to the name that is open, so a short name leaves every tag its full target.
        gap = Mathf.Clamp((laidW - ContentWidth - m.NameGap) / Mathf.Max(1, slots.Count - 2), MinGap, m.Gap);
        for (var i = 0; i < slots.Count - 1; i++)
        {
            var s = slots[i];
            s.After = Mathf.Lerp(gap, Mathf.Max(gap, m.NameGap), Mathf.SmoothStep(0f, 1f, s.Open));
            ContentWidth += s.After;
        }
        var x = laidW - ContentWidth;
        for (var i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            var current = s.Language == FrontRoomsLanguage.Current;
            s.Filled = Mathf.MoveTowards(s.Filled, current ? 1f : 0f, fill);
            s.X = x;
            s.Tag.anchoredPosition = new Vector2(x + m.TagW * TagPivot.x, -(m.Row * .5f - m.TagCentre) - m.TagH * (1f - TagPivot.y));
            // D5: test builds show what is not finished, dimmed. The current language stays whole.
            var alpha = s.Language.Finished || current ? 1f : DimAlpha;
            var paper = new Color(Paper.r, Paper.g, Paper.b, alpha);
            s.Outline.color = paper;
            s.Fill.color = new Color(Paper.r, Paper.g, Paper.b, s.Filled);
            s.Token.color = Color.Lerp(paper, Ink, s.Filled);
            s.Name.color = paper;
            // The name is uncovered as the tag moves aside for it.
            Place(s.Clip, x + m.TagW + m.NameGap, 0f, Mathf.Max(0f, s.W - m.TagW - m.NameGap), m.Row);
            x += s.W + s.After;
        }
        if (swinging >= 0)
        {
            var t = Time.unscaledTime - swungAt;
            var done = reduced || t > SwingSeconds;
            // The HUD key's own U·3 kick; clockwise is negative here.
            slots[swinging].Tag.localRotation = Quaternion.Euler(0f, 0f, done ? 0f : -FrontRoomsKeyHudMotion.SwingKick(t, SwingDegrees));
            if (done) swinging = -1;
        }
        var wash = Pressed >= 0 && Pressed < slots.Count;
        pressedWash.color = new Color(Paper.r, Paper.g, Paper.b, wash ? .07f : 0f);
        if (wash) Place(pressedWash.rectTransform, slots[Pressed].X - gap * .5f, 0f, slots[Pressed].W + gap, m.Row);
    }

    // ------------------------------------------------------------- the row's mark

    /// <summary>After the word LANGUAGE, the outlined A文 that is the same in every language, so a player lost in one they cannot
    /// read still finds the row. <paramref name="labelEnd"/> is where the word ends.</summary>
    public static void BuildMark(RectTransform row, Metrics m, float labelEnd)
    {
        var mark = MakeTag(row, out var outline, out var unused);
        unused.enabled = false;
        outline.color = Paper;
        mark.sizeDelta = new Vector2(m.TagW, m.TagH);
        mark.anchoredPosition = new Vector2(labelEnd + MarkGap + m.TagW * TagPivot.x, -(m.Row * .5f - m.TagCentre) - m.TagH * (1f - TagPivot.y));
        var a = Word(mark, "A", TokenFont, m.Token, Paper, TextAnchor.MiddleCenter);
        var wen = Word(mark, "文", TokenCjkFont, m.Token, Paper, TextAnchor.MiddleCenter);
        var aW = m.Token * .6f;
        var left = (m.TagW - aW - m.Token) * .5f;
        Place(a.rectTransform, left, m.TagCentre - m.Row * .5f, aW, m.Row);
        Place(wen.rectTransform, left + aW, m.TagCentre - m.Row * .5f, m.Token, m.Row);
    }

    /// <summary>Where the strip starts in a row: past the word and its mark.</summary>
    public static float StartAfter(Metrics m, float labelEnd) => labelEnd + MarkGap + m.TagW + m.Gap * 2f;

    // ------------------------------------------------------------- faces

    // Tokens and names ship in every language (LANGUAGE_MARKS.md, type by script): each CJK language has two tiny subsets,
    // always loaded. Without them the fallback face sets what it can and the system sets the rest.
    static Font tokenFont, tokenCjkFont;
    static Font TokenFont => tokenFont != null ? tokenFont : tokenFont = Resources.Load<Font>("Fonts/IBMPlexMono-Medium");
    static Font TokenCjkFont
    {
        get
        {
            if (tokenCjkFont == null) tokenCjkFont = Resources.Load<Font>("Fonts/LanguageToken-zh-Hans");
            return tokenCjkFont != null ? tokenCjkFont : TokenFont;
        }
    }

    static Font TokenFace(Language language)
    {
        if (!Cjk(language.Token)) return TokenFont;
        var subset = Resources.Load<Font>("Fonts/LanguageToken-" + language.Code);
        return subset != null ? subset : TokenFont;
    }

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
