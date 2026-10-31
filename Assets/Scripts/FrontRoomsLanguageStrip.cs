using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Language = FrontRoomsLanguage.Language;

/// <summary>
/// The LANGUAGE control (Red, 2026-10-30: every language in Settings), the same on the desktop sheet and the touch sheet.
/// One row, ASSIST's first, with no page behind it. Since 2026-10-31 each language is a landmark souvenir keychain (Red, 平面视觉
/// chat: "每个国家代表性的那种旅游地标钥匙扣", no flags): a key heads the row with the current language's charm on it and its name
/// after the key's blade; every other charm hangs from one rail out to the right edge, in list order. A pick flies the charm onto
/// the key and the old one home. Spec: Documentation/research/desktop_ui/LANGUAGE_CHARMS.md (sizes, motion), README §7.
///
/// Being on the key is what current means; there is no fill or outline state. A keyboard or a pointer can go to another charm to
/// read its name under it without choosing it. Positions are in the strip's own units, top-left origin, y down: the sheet that
/// hosts it places Root and hands it input.
/// </summary>
public sealed class FrontRoomsLanguageStrip
{
    public struct Metrics
    {
        /// <summary>Row height; charm units to px or pt; the key's width; its bow hole from the strip's top-left.</summary>
        public float Row, Scale, KeyW, BowX, BowY;
        /// <summary>After the key's blade to the current name; between charm silhouettes, closing to MinGap when tight.</summary>
        public float NameGap, Gap;
        public int Name, HoverName;
        /// <summary>The row's A文 mark after the word LANGUAGE (BuildMark), drawn as the old tag.</summary>
        public float TagW, TagH, TagCentre;
        public int Token, TokenCjk;
    }

    public static readonly Metrics Desktop = new Metrics { Row = 80f, Scale = .6f, KeyW = 48f, BowX = 14f, BowY = 22f, NameGap = 16f, Gap = 10f, Name = 20, HoverName = 13, TagW = 40f, TagH = 28f, TagCentre = 16.9f, Token = 13, TokenCjk = 14 };
    public static readonly Metrics Phone = new Metrics { Row = 64f, Scale = .5f, KeyW = 40f, BowX = 12f, BowY = 19f, NameGap = 13.6f, Gap = 10f, Name = 17, HoverName = 11, TagW = 34f, TagH = 23.8f, TagCentre = 14.4f, Token = 11, TokenCjk = 12 };
    public static readonly Metrics Tablet = new Metrics { Row = 70f, Scale = .55f, KeyW = 44f, BowX = 13f, BowY = 20.5f, NameGap = 15.2f, Gap = 10f, Name = 19, HoverName = 12, TagW = 38f, TagH = 26.6f, TagCentre = 16.1f, Token = 12, TokenCjk = 13 };

    static readonly Color Paper = new Color(.957f, .945f, .91f, 1f);
    // The old tag swings about the top of its ring: (20, 2) in a 40 × 28 tag. Only the row's mark still uses it.
    static readonly Vector2 TagPivot = new Vector2(.5f, 1f - 2f / 28f);

    // A charm is drawn on a 64 × 72 unit canvas and hangs from its ring hole at (32, 6.2) (charms.json).
    const float CanvasW = 64f, CanvasH = 72f, RingX = 32f, RingY = 6.2f;
    // Each die-cut silhouette, x0 · x1 · bottom in units, so the gaps are even between charms, not canvases.
    static readonly Dictionary<string, Vector3> Silhouettes = new Dictionary<string, Vector3>
    {
        { "en", new Vector3(9.12f, 60.12f, 71.88f) }, { "zh-Hans", new Vector3(2.12f, 61.88f, 70.88f) },
        { "ru", new Vector3(1.12f, 62.88f, 71.88f) }, { "es", new Vector3(6.12f, 57.88f, 71.88f) },
        { "pt-BR", new Vector3(2.12f, 61.88f, 71.88f) }, { "de", new Vector3(1.12f, 62.88f, 71.88f) },
        { "fr", new Vector3(12.25f, 51.88f, 70.88f) }, { "ja", new Vector3(1.62f, 62.62f, 49.38f) },
        { "ko", new Vector3(2.12f, 61.88f, 71.88f) }, { "zh-Hant", new Vector3(20.5f, 43.5f, 70.88f) },
    };
    static readonly Vector3 NoSilhouette = new Vector3(12f, 52f, 60f);

    // The header key is the HUD's B key: its 240 × 108 alpha bbox at (186, 6) of a 432 × 288 image, bow hole at (24, 54) of it.
    const string KeyArt = "UI/KeyHud/KeyB_key@6x";
    static readonly Rect KeyUv = new Rect(186f / 432f, (288f - 114f) / 288f, 240f / 432f, 108f / 288f);
    const float KeyAspect = 108f / 240f, KeyBowU = 24f / 240f;

    const float FillSeconds = .12f, OpenSeconds = .16f, DimAlpha = .5f;
    const float SwingDegrees = 9f, SwingSeconds = 1.5f, HoverDegrees = 4f, HomeDegrees = 4f, KeyDegrees = 3f;
    const float LiftSeconds = .06f, FlySeconds = .36f, Lift = 4f, ArcToKey = 28f, ArcHome = 18f, FlightTilt = 12f;
    public const float MarkGap = 14f;
    const float MinGap = 2f, MinNameScale = .7f;

    sealed class Slot
    {
        public Language Language;
        public RectTransform Charm;
        public Image Art;
        public Text Token, Under;
        public float L, R, Hang, NameW;
        public float Rail, Shown, Home;
        public Vector2 Pos, From;
        public bool FlyingHome;
        public float SwingAt = -1f, SwingAmp;
    }

    readonly List<Slot> slots = new List<Slot>();
    readonly RectTransform key;
    readonly Text name;
    readonly Image rule, pressedWash;
    Metrics m;
    float laidW = -1f, gap, keyRight, nameScale = 1f;
    int highlight, flying = -1;
    float pickedAt = -10f, keyKickAt = -10f, nameAlpha = 1f;

    public RectTransform Root { get; }
    /// <summary>The charm a keyboard or a pointer is on: the current language when neither is.</summary>
    public int Highlight { get => highlight; set => highlight = Mathf.Clamp(value, 0, Mathf.Max(0, slots.Count - 1)); }
    /// <summary>The charm under a finger, or -1.</summary>
    public int Pressed { get; set; } = -1;
    public int Count => slots.Count;
    /// <summary>The width the row takes now: the key and the current name, then the rail out to the right edge.</summary>
    public float ContentWidth { get; private set; }
    /// <summary>The widest the row gets, whichever language is on the key (what Layout fits).</summary>
    public float WidestWidth { get; private set; }

    public static FrontRoomsLanguageStrip Create(Transform parent, Font serif) => new FrontRoomsLanguageStrip(parent, serif);

    FrontRoomsLanguageStrip(Transform parent, Font serif)
    {
        Root = Node("Language strip", parent);
        rule = Solid(Root, new Color(Paper.r, Paper.g, Paper.b, .08f));
        rule.enabled = false;
        pressedWash = Solid(Root, new Color(Paper.r, Paper.g, Paper.b, 0f));
        // The key first, so a charm's ring reads as threaded through its bow hole.
        key = Node("key", Root);
        var keyArt = new GameObject("key art", typeof(RectTransform)).AddComponent<RawImage>();
        keyArt.transform.SetParent(key, false);
        keyArt.texture = Resources.Load<Texture2D>(KeyArt);
        keyArt.uvRect = KeyUv;
        keyArt.raycastTarget = false;
        var keyRect = keyArt.rectTransform;
        keyRect.anchorMin = Vector2.zero; keyRect.anchorMax = Vector2.one; keyRect.offsetMin = keyRect.offsetMax = Vector2.zero;
        foreach (var language in FrontRoomsLanguage.ListedLanguages)
        {
            var s = new Slot { Language = language };
            s.Charm = Node(language.Code, Root);
            s.Charm.pivot = new Vector2(RingX / CanvasW, 1f - RingY / CanvasH);
            var art = Resources.Load<Sprite>("Language/LangCharm_" + language.Code);
            s.Art = Stretched(s.Charm, art);
            // A language drawn later than its charm set still shows: its token where the charm would hang.
            s.Token = Word(s.Charm, language.Token, NameFace(language, serif), 13, Paper, TextAnchor.MiddleCenter);
            s.Token.enabled = art == null;
            s.Art.enabled = art != null;
            s.Under = Word(Root, language.OwnName, NameFace(language, serif), 13, Paper, TextAnchor.UpperCenter);
            slots.Add(s);
        }
        foreach (var s in slots) s.Under.transform.SetAsLastSibling();
        name = Word(Root, string.Empty, serif, 20, Paper, TextAnchor.MiddleLeft);
        this.serif = serif;
    }

    readonly Font serif;

    public void SetActive(bool value)
    {
        if (value == Root.gameObject.activeSelf) return;
        Root.gameObject.SetActive(value);
        if (value) Snap();
    }

    /// <summary>Back to the current language.</summary>
    public void Reset() => Highlight = CurrentIndex;

    /// <summary>Everything where it belongs, with no motion: the strip as it appears.</summary>
    public void Snap()
    {
        Reset();
        Pressed = -1;
        flying = -1;
        pickedAt = keyKickAt = -10f;
        nameAlpha = 1f;
        key.localRotation = Quaternion.identity;
        foreach (var s in slots)
        {
            s.Rail = s == Current ? 0f : 1f;
            s.Shown = 0f;
            s.FlyingHome = false;
            s.SwingAt = -1f;
            s.Charm.localRotation = Quaternion.identity;
        }
        Animate();
        foreach (var s in slots) s.Pos = Target(s);
        Animate();
    }

    /// <summary>Fits the widest case, the longest name on the key with every other charm on the rail, into
    /// <paramref name="width"/>: the gaps close first, then the current name shrinks.</summary>
    public void Layout(Metrics metrics, float width)
    {
        if (metrics.Row == m.Row && metrics.Scale == m.Scale && width == laidW) return;
        m = metrics;
        laidW = width;
        var keyW = m.KeyW;
        var keyH = keyW * KeyAspect;
        key.pivot = new Vector2(KeyBowU, .5f);
        key.sizeDelta = new Vector2(keyW, keyH);
        key.anchoredPosition = new Vector2(m.BowX, -m.BowY);
        keyRight = m.BowX + keyW * (1f - KeyBowU);
        name.fontSize = m.Name;
        var all = 0f;
        foreach (var s in slots)
        {
            var box = Silhouettes.TryGetValue(s.Language.Code, out var b) ? b : NoSilhouette;
            s.L = (RingX - box.x) * m.Scale;
            s.R = (box.y - RingX) * m.Scale;
            s.Hang = (box.z - RingY) * m.Scale;
            s.Charm.sizeDelta = new Vector2(CanvasW * m.Scale, CanvasH * m.Scale);
            s.Token.fontSize = Cjk(s.Language.Token) ? m.TokenCjk : m.Token;
            Place(s.Token.rectTransform, 0f, RingY * m.Scale, CanvasW * m.Scale, (CanvasH - RingY) * m.Scale);
            s.Under.fontSize = m.HoverName;
            name.font = s.Under.font;
            name.text = s.Language.OwnName;
            s.NameW = Mathf.Ceil(name.preferredWidth);
            all += s.L + s.R;
        }
        // The worst case: the longest head (key, name) beside the widest rail, which leaves that one language out.
        var gaps = Mathf.Max(1, slots.Count - 2);
        var worst = 0f;
        foreach (var s in slots) worst = Mathf.Max(worst, keyRight + 2f * m.NameGap + s.NameW + all - s.L - s.R);
        gap = Mathf.Clamp((width - worst) / gaps, MinGap, m.Gap);
        nameScale = 1f;
        foreach (var s in slots)
        {
            var room = width - (keyRight + 2f * m.NameGap + all - s.L - s.R + gap * gaps);
            if (s.NameW > 0f) nameScale = Mathf.Min(nameScale, room / s.NameW);
        }
        nameScale = Mathf.Clamp(nameScale, MinNameScale, 1f);
        name.fontSize = Mathf.FloorToInt(m.Name * nameScale);
        WidestWidth = 0f;
        foreach (var s in slots)
        {
            name.font = s.Under.font;
            name.text = s.Language.OwnName;
            s.NameW = Mathf.Ceil(name.preferredWidth);
            WidestWidth = Mathf.Max(WidestWidth, keyRight + 2f * m.NameGap + s.NameW + all - s.L - s.R + gap * gaps);
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

    /// <summary>The charm at a distance along the strip, or -1: the key and its name are the current language; on the rail each
    /// charm's target runs to the middle of the gap beside it.</summary>
    public int At(float x)
    {
        if (x < 0f || x > laidW) return -1;
        var current = CurrentIndex;
        if (x < keyRight + m.NameGap + NameWidth(current) + m.NameGap * .5f) return current;
        for (var i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            if (i == current || s.Rail < .5f) continue;
            if (x >= s.Home - s.L - gap * .5f && x < s.Home + s.R + gap * .5f) return i;
        }
        return -1;
    }

    /// <summary>A pointer moving along the strip goes to the charm under it, which swings once as it is reached; off the strip,
    /// the current language.</summary>
    public void Hover(float x)
    {
        var at = At(x);
        if (at < 0) { Reset(); return; }
        if (at != Highlight) Kick(slots[at], HoverDegrees);
        Highlight = at;
    }

    /// <summary>← →: the next charm, which swings once; nothing changes until a pick.</summary>
    public void Step(int direction)
    {
        var to = Mathf.Clamp(Highlight + direction, 0, slots.Count - 1);
        if (to != Highlight) Kick(slots[to], HoverDegrees);
        Highlight = to;
    }

    public void PickHighlighted() => Pick(Highlight);

    /// <summary>The pick: saved and in force at once. The charm lifts, flies onto the key and swings; the old one flies home.</summary>
    public void Pick(int index)
    {
        if (index < 0 || index >= slots.Count) return;
        Highlight = index;
        var old = CurrentIndex;
        var reduced = FrontRoomsHandheld.ReduceMotion;
        if (index == old)
        {
            if (!reduced) Kick(slots[index], SwingDegrees);
            return;
        }
        if (!reduced)
        {
            // A pick in mid-flight: every charm still in the air sets off again from where it is.
            foreach (var f in slots) if (f.FlyingHome) f.From = f.Pos;
            var s = slots[index];
            s.From = s.Pos;
            s.SwingAt = -1f;
            s.Charm.localRotation = Quaternion.identity;
            s.Charm.SetAsLastSibling();
            flying = index;
            pickedAt = Time.unscaledTime;
            var o = slots[old];
            o.From = o.Pos;
            o.FlyingHome = true;
            o.SwingAt = -1f;
            nameAlpha = 0f;
        }
        FrontRoomsLanguage.Pick(slots[index].Language);
        if (reduced) Snap();
    }

    /// <summary>The middle of a charm along the strip (its ring hole), for a test's tap.</summary>
    public float TagCentreX(int index) => index == CurrentIndex ? m.BowX : slots[index].Home;

    /// <summary>Every frame while it shows: unscaled time, the game being paused behind the sheet.</summary>
    public void Animate()
    {
        if (slots.Count == 0) return;
        var reduced = FrontRoomsHandheld.ReduceMotion;
        var now = Time.unscaledTime;
        var current = CurrentIndex;
        var since = now - pickedAt;
        // The rail gives way once the charm has lifted: the picked one's gap closes, the old one's opens.
        var open = reduced ? 1f : since < LiftSeconds ? 0f : Time.unscaledDeltaTime / OpenSeconds;
        foreach (var s in slots) s.Rail = Mathf.MoveTowards(s.Rail, s == slots[current] ? 0f : 1f, open);

        var x = laidW + gap;
        var railLeft = laidW;
        for (var i = slots.Count - 1; i >= 0; i--)
        {
            var s = slots[i];
            var e = Mathf.SmoothStep(0f, 1f, s.Rail);
            s.Home = x - gap - s.R;
            if (e > 0f) railLeft = Mathf.Min(railLeft, x - e * gap - e * (s.R + s.L));
            x -= e * (gap + s.R + s.L);
        }
        ContentWidth = keyRight + 2f * m.NameGap + NameWidth(current) + (laidW - railLeft);

        if (flying >= 0 && since >= LiftSeconds + FlySeconds)
        {
            // Clip on: the key kicks, the charm takes today's pick swing.
            Kick(slots[flying], SwingDegrees, pickedAt + LiftSeconds + FlySeconds);
            keyKickAt = pickedAt + LiftSeconds + FlySeconds;
            flying = -1;
        }
        nameAlpha = flying >= 0 ? 0f : Mathf.MoveTowards(nameAlpha, 1f, reduced ? 1f : Time.unscaledDeltaTime / FillSeconds);

        for (var i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            var target = Target(s);
            var tilt = 0f;
            if (i == flying)
            {
                var start = s.From - new Vector2(0f, Lift);
                if (since < LiftSeconds) s.Pos = Vector2.Lerp(s.From, start, since / LiftSeconds);
                else
                {
                    var u = Mathf.Clamp01((since - LiftSeconds) / FlySeconds);
                    s.Pos = Arc(start, target, ArcToKey, EaseInOutCubic(u));
                    tilt = Tilt(start, target, u);
                }
            }
            else if (s.FlyingHome)
            {
                var u = Mathf.Clamp01(since / FlySeconds);
                s.Pos = Arc(s.From, target, ArcHome, EaseInOutCubic(u));
                tilt = Tilt(s.From, target, u);
                if (u >= 1f || reduced) { s.FlyingHome = false; s.Pos = target; Kick(s, HomeDegrees, now); }
            }
            else s.Pos = target;

            var swing = 0f;
            if (s.SwingAt >= 0f && !reduced)
            {
                var t = now - s.SwingAt;
                // The HUD key's own U·3 kick; clockwise is negative here.
                if (t > SwingSeconds) s.SwingAt = -1f;
                else if (t >= 0f) swing = -FrontRoomsKeyHudMotion.SwingKick(t, s.SwingAmp);
            }
            s.Charm.anchoredPosition = new Vector2(s.Pos.x, -s.Pos.y);
            s.Charm.localRotation = Quaternion.Euler(0f, 0f, reduced ? 0f : tilt + swing);

            // D5: test builds show what is not finished, dimmed. The current language stays whole.
            var isCurrent = i == current;
            var alpha = s.Language.Finished || isCurrent ? 1f : DimAlpha;
            s.Art.color = new Color(1f, 1f, 1f, alpha);
            s.Token.color = new Color(Paper.r, Paper.g, Paper.b, alpha);
            // A charm's name shows under it while a pointer or a keyboard is on it; the rail never moves for it.
            var named = i == Highlight && !isCurrent && i != flying && !s.FlyingHome;
            s.Shown = Mathf.MoveTowards(s.Shown, named ? 1f : 0f, reduced ? 1f : Time.unscaledDeltaTime / FillSeconds);
            s.Under.color = new Color(Paper.r, Paper.g, Paper.b, alpha * s.Shown);
            var under = Mathf.Min(m.BowY + s.Hang + 2f, m.Row - m.HoverName - 1f);
            Place(s.Under.rectTransform, s.Home - 60f, under, 120f, m.HoverName + 4f);
        }

        var keySince = now - keyKickAt;
        key.localRotation = Quaternion.Euler(0f, 0f, reduced || keySince > SwingSeconds ? 0f : -FrontRoomsKeyHudMotion.SwingKick(keySince, KeyDegrees));
        var shown = slots.Count > 0 ? slots[current] : null;
        name.font = shown != null ? shown.Under.font : serif;
        name.text = shown != null ? shown.Language.OwnName : string.Empty;
        name.fontSize = Mathf.FloorToInt(m.Name * nameScale);
        name.color = new Color(Paper.r, Paper.g, Paper.b, nameAlpha);
        Place(name.rectTransform, keyRight + m.NameGap, 0f, NameWidth(current) + 2f, m.Row);

        var wash = Pressed >= 0 && Pressed < slots.Count;
        pressedWash.color = new Color(Paper.r, Paper.g, Paper.b, wash ? .07f : 0f);
        if (wash)
        {
            if (Pressed == current) Place(pressedWash.rectTransform, 0f, 0f, keyRight + m.NameGap + NameWidth(current) + m.NameGap * .5f, m.Row);
            else
            {
                var p = slots[Pressed];
                Place(pressedWash.rectTransform, p.Home - p.L - gap * .5f, 0f, p.L + p.R + gap, m.Row);
            }
        }
    }

    int CurrentIndex
    {
        get
        {
            for (var i = 0; i < slots.Count; i++) if (slots[i].Language == FrontRoomsLanguage.Current) return i;
            return 0;
        }
    }

    Slot Current => slots.Count > 0 ? slots[CurrentIndex] : null;

    float NameWidth(int index) => index >= 0 && index < slots.Count ? slots[index].NameW : 0f;

    /// <summary>Where a charm rests: the key's bow hole, or its point on the rail.</summary>
    Vector2 Target(Slot s) => s == Current ? new Vector2(m.BowX, m.BowY) : new Vector2(s.Home, m.BowY);

    static void Kick(Slot s, float degrees, float at = -1f)
    {
        s.SwingAt = at >= 0f ? at : Time.unscaledTime;
        s.SwingAmp = degrees;
    }

    // A quadratic arc whose control point sits <height> above the chord's middle (y down, so above is minus).
    static Vector2 Arc(Vector2 from, Vector2 to, float height, float u)
    {
        var control = (from + to) * .5f - new Vector2(0f, height);
        var a = Vector2.Lerp(from, control, u);
        var b = Vector2.Lerp(control, to, u);
        return Vector2.Lerp(a, b, u);
    }

    // In flight a charm trails its ring: travelling left it leans counter-clockwise, travelling right clockwise.
    static float Tilt(Vector2 from, Vector2 to, float u) => FlightTilt * Mathf.Sin(Mathf.PI * u) * (to.x < from.x ? 1f : -1f);

    static float EaseInOutCubic(float u) => u < .5f ? 4f * u * u * u : 1f - Mathf.Pow(-2f * u + 2f, 3f) * .5f;

    // ------------------------------------------------------------- the row's mark

    /// <summary>After the word LANGUAGE, the outlined A文 that is the same in every language, so a player lost in one they cannot
    /// read still finds the row. <paramref name="labelEnd"/> is where the word ends.</summary>
    public static void BuildMark(RectTransform row, Metrics m, float labelEnd)
    {
        var mark = Node("mark", row);
        mark.pivot = TagPivot;
        var outline = Stretched(mark, Resources.Load<Sprite>("Language/LangTag_Outline"));
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

    // Names ship in every language (LANGUAGE_MARKS.md, type by script): each CJK language has a tiny subset of its own name,
    // always loaded. Without it the fallback face sets what it can and the system sets the rest.
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

    static Font NameFace(Language language, Font serif)
    {
        var subset = Resources.Load<Font>("Fonts/LanguageName-" + language.Code);
        return subset != null ? subset : serif;
    }

    static bool Cjk(string s) => s.Length > 0 && s[0] >= '⺀';

    // ------------------------------------------------------------- parts

    static Image Stretched(RectTransform parent, Sprite sprite)
    {
        var image = new GameObject(sprite != null ? sprite.name : "art", typeof(RectTransform)).AddComponent<Image>();
        image.transform.SetParent(parent, false);
        image.sprite = sprite;
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
