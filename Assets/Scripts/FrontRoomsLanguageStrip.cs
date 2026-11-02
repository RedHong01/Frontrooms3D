using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Language = FrontRoomsLanguage.Language;

/// <summary>
/// The LANGUAGE control (Red, 2026-10-30: every language in Settings), the same on the desktop sheet and the touch sheet.
/// One row, ASSIST's first, with no page behind it. Since 2026-11-01 each language is a flag keychain, one die-cut tag for all
/// (Red, 交互设计 chat: "我希望你使用国旗钥匙扣方案而不是现在的物品钥匙扣方案"; art by 平面视觉): a key heads the row with the
/// current language's charm on it and its name after the key's blade; every other charm hangs from one rail out to the right
/// edge, in list order, with its language's token beside it (Red, same message: "语言名称应该显示在钥匙链旁边（就像是更早的方案"),
/// the A · TAG one-row's tokens. A row too narrow for the tokens beside the tags sets each under its tag instead, in the room
/// the short tags leave. A pick flies the charm onto the key and the old one home. Spec: Documentation/research/desktop_ui/
/// LANGUAGE_CHARMS.md (sizes, motion), README §7.
///
/// Nothing in the row moves with a pick (Red, 2026-11-01): every charm keeps its own place on the rail, the one on the key
/// leaving a faint print of itself there, and the strip starts at the same x whatever the row's label says in this language.
/// A charm's name under a pointer reads at the current name's size, one name at a time, in the open run left of the rail; on a
/// row too narrow for it, it takes the current name's place while the charm is under the pointer or finger.
///
/// Unfolded (SETTINGS_MOTION.md, E3; Red 20:4x: "这里应该保留之前钥匙的那套逻辑，不要full bleed bar"): the rail's charms fade out
/// and the ten stand in a grid under the row, five a line, each name beside its charm, the cells fading in left to right where
/// they stay; the key logic is kept: a pick flies the charm onto the key and the old one home to its cell, which keeps a print.
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
        /// <summary>After the key's blade to the current name; between charm silhouettes, closing to MinGap when tight; from a
        /// tag to its token.</summary>
        public float NameGap, Gap, SideGap;
        public int Name, HoverName;
    }

    public static readonly Metrics Desktop = new Metrics { Row = 80f, Scale = .6f, KeyW = 48f, BowX = 14f, BowY = 22f, NameGap = 16f, Gap = 10f, SideGap = 6f, Name = 20, HoverName = 13 };
    public static readonly Metrics Phone = new Metrics { Row = 64f, Scale = .5f, KeyW = 40f, BowX = 12f, BowY = 19f, NameGap = 13.6f, Gap = 10f, SideGap = 5f, Name = 17, HoverName = 11 };
    public static readonly Metrics Tablet = new Metrics { Row = 70f, Scale = .55f, KeyW = 44f, BowX = 13f, BowY = 20.5f, NameGap = 15.2f, Gap = 10f, SideGap = 5.5f, Name = 19, HoverName = 12 };

    static readonly Color Paper = new Color(.957f, .945f, .91f, 1f);

    // A charm is drawn on a 64 × 72 unit canvas and hangs from its ring hole at (32, 6.2) (charms.json).
    const float CanvasW = 64f, CanvasH = 72f, RingX = 32f, RingY = 6.2f;
    // Each die-cut silhouette, x0 · x1 · bottom in units, so the gaps are even between charms, not canvases. It is read from the
    // sprite's tight mesh (SilhouetteOf), so whatever art lands sets its own spacing; this table stands in for a missing mesh.
    // The flag tags share one die-cut (平面视觉, 2026-11-01: charms.json bboxUnits [1, 0.22, 63, 42.64]), body centred 26.9 down.
    static readonly Vector3 FlagTag = new Vector3(1f, 63f, 42.64f);
    const float FlagBodyY = 26.9f;
    static readonly Dictionary<string, Vector3> Silhouettes = new Dictionary<string, Vector3>
    {
        { "en", FlagTag }, { "zh-Hans", FlagTag }, { "ru", FlagTag }, { "es", FlagTag }, { "pt-BR", FlagTag },
        { "de", FlagTag }, { "fr", FlagTag }, { "ja", FlagTag }, { "ko", FlagTag }, { "zh-Hant", FlagTag },
    };
    static readonly Vector3 NoSilhouette = new Vector3(12f, 52f, 60f);

    // The header key is the HUD's B key: its 240 × 108 alpha bbox at (186, 6) of a 432 × 288 image, bow hole at (24, 54) of it.
    const string KeyArt = "UI/KeyHud/KeyB_key@6x";
    static readonly Rect KeyUv = new Rect(186f / 432f, (288f - 114f) / 288f, 240f / 432f, 108f / 288f);
    const float KeyAspect = 108f / 240f, KeyBowU = 24f / 240f;

    const float FillSeconds = .12f, OpenSeconds = .16f;
    const float SwingDegrees = 9f, SwingSeconds = 1.5f, HoverDegrees = 4f, HomeDegrees = 4f, KeyDegrees = 3f;
    const float LiftSeconds = .06f, FlySeconds = .36f, Lift = 4f, ArcToKey = 28f, ArcHome = 18f, FlightTilt = 12f;
    public const float MarkGap = 14f;
    const float MinGap = 2f, MinNameScale = .7f, PrintAlpha = .16f, RunNameAlpha = .8f;
    // The grid (E3): five a line; the rail out in 0.08 s; each cell in 0.18 s from 0.08 s, 0.035 s after the one before; folding,
    // the cells out in 0.06 s, the last first, 0.006 s apart; the rail back from 0.2 s in 0.12 s. A cell's charm sits 8 in and 6
    // down, its name 6 past the charm, on the line of the mock (W/settings_motion/mock.py, draw_language).
    public const int GridColumns = 5;
    const float RailOutS = .08f, CellInAt = .08f, CellInS = .18f, CellStagger = .035f, CellOutStep = .006f, CellOutS = .06f, RailBackAt = .2f, RailBackS = .12f;
    // The charm art is 256 × 288 and hangs at about 37 px on a 1080p desktop, where trilinear sampling blurs it (平面视觉, VL478):
    // half a level sharper.
    const float CharmMipBias = -.5f;
    // "LANGUAGE" in Bayon caps, in em (FrontRoomsSettingsMenu.BayonUpper): the widest of the row's label in all ten languages.
    const float LanguageWordEm = 3.495f;

    sealed class Slot
    {
        public Language Language;
        public RectTransform Charm;
        public Image Art;
        public Text Token, Under, Side;
        public RectTransform Spot;
        public Image Print;
        public float L, R, Hang, NameW, SideW;
        public Vector3 Box;
        public float Rail, Shown, Home, HomeY;
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
    // The tokens stand beside the tags when the row has room for them, else under them.
    bool beside = true;
    // Unfolded into the grid, since when; folded back, since when; where the cells are (strip units) and the names' size there.
    bool grid;
    float gridAt = -100f, railAt = -100f, gridLeft, gridTop, cellW = 1f, cellH = 1f;
    int gridName = 19;
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
            var art = Resources.Load<Sprite>("Language/LangCharm_" + language.Code);
            if (art != null && art.texture != null) art.texture.mipMapBias = CharmMipBias;
            s.Box = SilhouetteOf(art, language.Code);
            // Its place on the rail, drawn behind the charms: the print shows while the charm is on the key.
            s.Spot = Node(language.Code + " place", Root);
            s.Spot.pivot = new Vector2(RingX / CanvasW, 1f - RingY / CanvasH);
            s.Print = Stretched(s.Spot, art);
            s.Print.enabled = art != null;
            s.Charm = Node(language.Code, Root);
            s.Charm.pivot = new Vector2(RingX / CanvasW, 1f - RingY / CanvasH);
            s.Art = Stretched(s.Charm, art);
            // A language drawn later than its charm set still shows: its token where the charm would hang.
            s.Token = Word(s.Charm, language.Token, NameFace(language, serif), 13, Paper, TextAnchor.MiddleCenter);
            s.Token.enabled = art == null;
            s.Art.enabled = art != null;
            s.Under = Word(Root, language.OwnName, NameFace(language, serif), 13, Paper, TextAnchor.MiddleRight);
            // Its token beside its tag on the rail (EN, 简, РУ…), in its own face; it stays when the charm is on the key.
            s.Side = Word(Root, language.Token, NameFace(language, serif), 13, Paper, TextAnchor.MiddleLeft);
            s.Side.enabled = art != null;
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
            var box = s.Box;
            s.L = (RingX - box.x) * m.Scale;
            s.R = (box.y - RingX) * m.Scale;
            s.Hang = (box.z - RingY) * m.Scale;
            s.Charm.sizeDelta = new Vector2(CanvasW * m.Scale, CanvasH * m.Scale);
            s.Spot.sizeDelta = s.Charm.sizeDelta;
            s.Token.fontSize = m.HoverName;
            Place(s.Token.rectTransform, 0f, RingY * m.Scale, CanvasW * m.Scale, (CanvasH - RingY) * m.Scale);
            s.Side.fontSize = m.HoverName;
            s.SideW = s.Side.enabled ? Mathf.Ceil(s.Side.preferredWidth) : 0f;
            name.font = s.Under.font;
            name.text = s.Language.OwnName;
            s.NameW = Mathf.Ceil(name.preferredWidth);
            all += s.L + s.R;
        }
        // The worst case: the longest head (key, name) beside the whole rail, where every charm keeps its place. The tokens
        // stand beside their tags if that still fits with the gaps closed and the name at its smallest; else under them.
        var gaps = Mathf.Max(1, slots.Count - 1);
        var sides = 0f;
        foreach (var s in slots) sides += SideRun(s, true);
        var tightest = 0f;
        foreach (var s in slots) tightest = Mathf.Max(tightest, keyRight + 2f * m.NameGap + s.NameW * MinNameScale + all + sides + MinGap * gaps);
        beside = tightest <= width;
        if (beside) all += sides;
        var worst = 0f;
        foreach (var s in slots) worst = Mathf.Max(worst, keyRight + 2f * m.NameGap + s.NameW + all);
        gap = Mathf.Clamp((width - worst) / gaps, MinGap, m.Gap);
        nameScale = 1f;
        foreach (var s in slots)
        {
            var room = width - (keyRight + 2f * m.NameGap + all + gap * gaps);
            if (s.NameW > 0f) nameScale = Mathf.Min(nameScale, room / s.NameW);
        }
        nameScale = Mathf.Clamp(nameScale, MinNameScale, 1f);
        name.fontSize = Mathf.FloorToInt(m.Name * nameScale);
        WidestWidth = 0f;
        foreach (var s in slots)
        {
            s.Under.fontSize = name.fontSize;
            name.font = s.Under.font;
            name.text = s.Language.OwnName;
            s.NameW = Mathf.Ceil(name.preferredWidth);
            WidestWidth = Mathf.Max(WidestWidth, keyRight + 2f * m.NameGap + s.NameW + all + gap * gaps);
        }
        Root.sizeDelta = new Vector2(width, m.Row);
    }

    public void PlaceAt(float x, float y) => Root.anchoredPosition = new Vector2(x, -y);

    /// <summary>Unfolded into the grid (E3) or on the rail.</summary>
    public bool Grid => grid;

    /// <summary>Unfolds into the grid or folds back to the rail, in motion unless <paramref name="snap"/>.</summary>
    public void SetGrid(bool on, bool snap = false)
    {
        if (on != grid)
        {
            grid = on;
            if (on) gridAt = Time.unscaledTime; else railAt = Time.unscaledTime;
            if (!on) Reset();
        }
        if (snap) gridAt = railAt = -100f;
    }

    /// <summary>Where the grid's cells are, in the strip's units: the first cell's left and top, and a cell's size. The names take
    /// the current name's size less one, smaller where a cell is too narrow for the longest.</summary>
    public void PlaceGrid(float left, float top, float cellWidth, float cellHeight)
    {
        gridLeft = left;
        gridTop = top;
        cellW = Mathf.Max(1f, cellWidth);
        cellH = Mathf.Max(1f, cellHeight);
        var widest = 0f;
        foreach (var s in slots) widest = Mathf.Max(widest, s.NameW);
        var room = cellW + 8f - CellNameX - 6f;
        var size = name.fontSize - 1;
        if (widest > 0f && name.fontSize > 0) size = Mathf.Min(size, Mathf.FloorToInt(name.fontSize * room / widest));
        gridName = Mathf.Max(10, size);
    }

    /// <summary>The cell under a point of the strip (its units), or -1.</summary>
    public int AtCell(Vector2 point)
    {
        for (var i = 0; i < slots.Count; i++) if (CellRect(i).Contains(point)) return i;
        return -1;
    }

    /// <summary>A pointer over the grid goes to the charm under it, which swings once; off the cells, the current language.</summary>
    public void HoverCell(Vector2 point)
    {
        var at = AtCell(point);
        if (at < 0) { Reset(); return; }
        if (at != Highlight) Kick(slots[at], HoverDegrees);
        Highlight = at;
    }

    /// <summary>↑ ↓ in the grid: the cell a line up or down, which swings once; false past the grid's edge (the host leaves it).</summary>
    public bool StepRow(int direction)
    {
        var to = Highlight + direction * GridColumns;
        if (to < 0 || to >= slots.Count) return false;
        Kick(slots[to], HoverDegrees);
        Highlight = to;
        return true;
    }

    /// <summary>The language a pointer or a keyboard is on (the current one when neither is): what the preview shows.</summary>
    public Language HighlightedLanguage => slots.Count > 0 ? slots[Highlight].Language : null;

    Rect CellRect(int i) => new Rect(gridLeft + (i % GridColumns) * cellW - 8f, gridTop + (i / GridColumns) * cellH, cellW, cellH);
    float CellRingX => 8f + RingX * m.Scale;
    float CellRingY => 6f + RingY * m.Scale;
    float CellNameX => 8f + CanvasW * m.Scale + 6f;
    float CellNameY => 27f * m.Scale / .6f;

    // How much of a cell shows: fading in from its moment after the unfold, or out from its moment after the fold.
    float CellShown(int i, float now, bool reduced)
    {
        if (reduced) return grid ? 1f : 0f;
        if (grid) return EaseOut(Mathf.Clamp01((now - gridAt - CellInAt - i * CellStagger) / CellInS));
        return 1f - Mathf.Clamp01((now - railAt - (slots.Count - 1 - i) * CellOutStep) / CellOutS);
    }

    static float EaseOut(float u) { u = 1f - Mathf.Clamp01(u); return 1f - u * u * u * u; }

    /// <summary>The row's rule where the host's own stops short of the strip, in the strip's units.</summary>
    public void DrawRule(float x, float width)
    {
        rule.enabled = width > 0f;
        Place(rule.rectTransform, x, m.Row - 1f, width, 1f);
    }

    /// <summary>The charm at a distance along the strip, or -1: the key and its name are the current language; on the rail each
    /// charm's target runs to the middle of the gap beside it, so they meet, the print of the one on the key included.</summary>
    public int At(float x)
    {
        if (x < 0f || x > laidW) return -1;
        var current = CurrentIndex;
        if (x < keyRight + m.NameGap + NameWidth(current) + m.NameGap * .5f) return current;
        for (var i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            if (x >= s.Home - s.L - gap * .5f && x < s.Home + s.R + SideRun(s, beside) + gap * .5f) return i;
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
        // Rail: 1 hanging on the rail, 0 on the key; the print comes up once the charm has lifted.
        var open = reduced ? 1f : since < LiftSeconds ? 0f : Time.unscaledDeltaTime / OpenSeconds;
        foreach (var s in slots) s.Rail = Mathf.MoveTowards(s.Rail, s == slots[current] ? 0f : 1f, open);

        // Every charm's place is fixed, its own included while it is on the key, so a pick moves nothing else.
        var x = laidW + gap;
        for (var i = slots.Count - 1; i >= 0; i--)
        {
            var s = slots[i];
            var side = SideRun(s, beside);
            s.Home = x - gap - side - s.R;
            x -= gap + side + s.R + s.L;
        }
        var railLeft = x;
        ContentWidth = keyRight + 2f * m.NameGap + NameWidth(current) + (laidW - railLeft);
        // The name a pointer or a keyboard is on, and the open run between the current name and the rail.
        var run = railLeft - m.NameGap - (keyRight + 2f * m.NameGap + NameWidth(current));
        var named = Highlight != current && Highlight != flying && !slots[Highlight].FlyingHome ? Highlight : -1;
        // The grid (E3): the rail's charms fade out, then the cells fade in where they stay; folding, the cells go, the last first,
        // then the rail comes back. While the cells show, the charms rest in them.
        var inCells = grid ? reduced || now - gridAt >= RailOutS : !reduced && now - railAt < RailBackAt;
        var railShown = grid ? (reduced ? 0f : 1f - Mathf.Clamp01((now - gridAt) / RailOutS)) : reduced ? 1f : EaseOut((now - railAt - RailBackAt) / RailBackS);
        for (var i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            s.HomeY = m.BowY;
            if (!inCells) continue;
            var cell = CellRect(i);
            s.Home = cell.x + CellRingX;
            s.HomeY = cell.y + CellRingY;
        }

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

            // A charm's name shows while a pointer or a keyboard is on it, at the current name's size, right-aligned to the rail
            // (Red, 2026-11-01: under the charm it was too small, met the row's rule and overlapped the last name): a new name
            // takes over at once, and only the last one fades.
            s.Shown = i == named ? Mathf.MoveTowards(s.Shown, 1f, reduced ? 1f : Time.unscaledDeltaTime / FillSeconds)
                : named >= 0 ? 0f : Mathf.MoveTowards(s.Shown, 0f, reduced ? 1f : Time.unscaledDeltaTime / FillSeconds);
            var place = inCells ? CellShown(i, now, reduced) : railShown;
            if (inCells)
            {
                // Its name beside it: whole under the pointer, quieter while its charm is on the key.
                var cell = CellRect(i);
                s.Under.alignment = TextAnchor.MiddleLeft;
                s.Under.fontSize = gridName;
                s.Under.color = new Color(Paper.r, Paper.g, Paper.b, (i == named ? 1f : i == current ? .55f : .75f) * place);
                Place(s.Under.rectTransform, cell.x + CellNameX, cell.y + CellNameY - 13f, cell.width - CellNameX, 26f);
            }
            else
            {
                s.Under.alignment = TextAnchor.MiddleRight;
                s.Under.fontSize = Mathf.FloorToInt(m.Name * nameScale);
                s.Under.color = new Color(Paper.r, Paper.g, Paper.b, s.NameW <= run ? RunNameAlpha * s.Shown * place : 0f);
                Place(s.Under.rectTransform, railLeft - m.NameGap - s.NameW - 4f, 0f, s.NameW + 4f, m.Row);
            }

            // Every language ships, drafts included (Red 2026-10-31: "10 种语言都用 AI 初稿直接上线"), so none is dimmed; Finished
            // is the review status only.
            var charm = i == flying || s.FlyingHome || i == current ? 1f : place;
            s.Art.color = new Color(1f, 1f, 1f, charm);
            s.Token.color = new Color(Paper.r, Paper.g, Paper.b, charm);
            // The token beside its tag (or under it on a narrow row): read at the run's alpha, whole under a pointer, and as faint
            // as the print while its charm is on the key.
            var onRail = s.FlyingHome ? 1f : Mathf.SmoothStep(0f, 1f, s.Rail);
            var sideAlpha = Mathf.Lerp(PrintAlpha, i == named ? 1f : RunNameAlpha, onRail);
            s.Side.color = new Color(Paper.r, Paper.g, Paper.b, inCells ? 0f : sideAlpha * railShown);
            var lineH = m.HoverName * 1.6f;
            if (beside) Place(s.Side.rectTransform, s.Home + s.R + m.SideGap, m.BowY + (BodyCentre(s.Box) - RingY) * m.Scale - lineH * .5f, s.SideW + 2f, lineH);
            else Place(s.Side.rectTransform, s.Home - (s.SideW + 2f) * .5f, Mathf.Min(m.BowY + s.Hang + 2f, m.Row - 2f - lineH), s.SideW + 2f, lineH);
            s.Side.alignment = beside ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
            // The print of the charm on the key, where it hangs on the rail; a charm flying home lands on its own.
            s.Spot.anchoredPosition = new Vector2(s.Home, -s.HomeY);
            s.Print.color = new Color(1f, 1f, 1f, PrintAlpha * place * (s.FlyingHome ? 1f : 1f - Mathf.SmoothStep(0f, 1f, s.Rail)));
        }

        var keySince = now - keyKickAt;
        key.localRotation = Quaternion.Euler(0f, 0f, reduced || keySince > SwingSeconds ? 0f : -FrontRoomsKeyHudMotion.SwingKick(keySince, KeyDegrees));
        // A name too long for the run stands where the current one does while its charm is under the pointer or finger.
        var swap = !inCells && named >= 0 && slots[named].NameW > run ? slots[named] : null;
        var shown = swap ?? slots[current];
        name.font = shown.Under.font;
        name.text = shown.Language.OwnName;
        name.fontSize = Mathf.FloorToInt(m.Name * nameScale);
        name.color = new Color(Paper.r, Paper.g, Paper.b, swap != null ? RunNameAlpha : nameAlpha);
        Place(name.rectTransform, keyRight + m.NameGap, 0f, shown.NameW + 2f, m.Row);

        var wash = Pressed >= 0 && Pressed < slots.Count;
        pressedWash.color = new Color(Paper.r, Paper.g, Paper.b, wash ? .07f : 0f);
        if (wash)
        {
            if (Pressed == current) Place(pressedWash.rectTransform, 0f, 0f, keyRight + m.NameGap + NameWidth(current) + m.NameGap * .5f, m.Row);
            else
            {
                var p = slots[Pressed];
                Place(pressedWash.rectTransform, p.Home - p.L - gap * .5f, 0f, p.L + p.R + SideRun(p, beside) + gap, m.Row);
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

    /// <summary>A charm's die-cut from its sprite's tight mesh (the imports are Tight, pivot at the centre): x0, x1 and bottom in
    /// canvas units, y down. Without a mesh, the table's entry.</summary>
    static Vector3 SilhouetteOf(Sprite art, string code)
    {
        var vertices = art != null ? art.vertices : null;
        if (vertices == null || vertices.Length == 0 || art.rect.width <= 0f || art.rect.height <= 0f)
            return Silhouettes.TryGetValue(code, out var b) ? b : NoSilhouette;
        var ppu = art.pixelsPerUnit;
        var size = art.rect.size;
        var pivot = art.pivot;
        float x0 = float.MaxValue, x1 = float.MinValue, bottom = float.MinValue;
        foreach (var v in vertices)
        {
            var x = (v.x * ppu + pivot.x) * CanvasW / size.x;
            var y = (size.y - (v.y * ppu + pivot.y)) * CanvasH / size.y;
            x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x); bottom = Mathf.Max(bottom, y);
        }
        return new Vector3(x0, x1, bottom);
    }

    // The middle of a tag's body, in units: the flag tag's own (its ring tab sits above the body), else half way down below the ring.
    static float BodyCentre(Vector3 box) =>
        Mathf.Abs(box.x - FlagTag.x) < 1f && Mathf.Abs(box.y - FlagTag.y) < 1f && Mathf.Abs(box.z - FlagTag.z) < 1f ? FlagBodyY : (RingY + box.z) * .5f;

    /// <summary>What a token beside its tag adds to the tag's place on the rail (nothing when the tokens sit under the tags).</summary>
    float SideRun(Slot s, bool besideTags) => besideTags && s.SideW > 0f ? m.SideGap + s.SideW : 0f;

    /// <summary>Where a charm rests: the key's bow hole, or its point on the rail.</summary>
    Vector2 Target(Slot s) => s == Current ? new Vector2(m.BowX, m.BowY) : new Vector2(s.Home, s.HomeY);

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

    // ------------------------------------------------------------- the row's start

    // The key heads the row (平面视觉, 2026-10-31): there is no A文 mark after the word LANGUAGE any more.
    /// <summary>Where the strip starts in a row: a mark gap past the word, with the widest charm's ink starting there (a charm
    /// on the key hangs a little left of the bow hole).</summary>
    public static float StartAfter(Metrics m, float labelEnd)
    {
        var overhang = 0f;
        foreach (var box in Silhouettes.Values) overhang = Mathf.Max(overhang, (RingX - box.x) * m.Scale - m.BowX);
        return labelEnd + MarkGap + overhang;
    }

    /// <summary>The row label's width for <see cref="StartAfter"/>: at least the English word's, so the strip starts at one x in
    /// every language (Red, 2026-11-01).</summary>
    public static float LabelWidth(float measured, float labelSize) => Mathf.Max(measured, LanguageWordEm * labelSize);

    // ------------------------------------------------------------- faces

    // Names ship in every language (LANGUAGE_MARKS.md, type by script): each CJK language has a tiny subset of its own name,
    // always loaded. Without it the fallback face sets what it can and the system sets the rest.
    static Font NameFace(Language language, Font serif)
    {
        var subset = Resources.Load<Font>("Fonts/LanguageName-" + language.Code);
        return subset != null ? subset : serif;
    }

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
