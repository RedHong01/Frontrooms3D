using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The structures scene's placards, pads and stand-ins (scene 4, RULES 5b; research/level_space/70 section 1; PLAN 2.5).
/// Placards are for Red, outside the fiction, set as the graphic line's district sign E (research/showcase_scenes/
/// 20_placard_type.md, 2026-11-03): a 1200 mm panel, its top edge 2.05 m over the floor under it, as tall as its content
/// plus 60 mm margins (at least 0.8 m), matte #161513; a code chip (IBM Plex Mono Medium, cap 40 mm, in a 1.2 mm paper
/// keyline) and the title (Noto Sans SC Bold, em 56 mm); the body (Regular, em 32 mm, pitch 43 mm); the greybox label (Bold,
/// grey #96938B) with its list in light grey in a hanging column; the Red-decides label (Bold, accent #F4DF3B, a 4 mm accent bar) with one
/// numbered question a line (the number in accent, the question Bold in paper, hanging); text unlit #F4F1E8. The faces
/// are the game's own (Resources/Fonts). World text is TextMesh here (a stand-in for the game's world-space UI Text).
/// Pads are 0.9 m floor plates the walker uses with E. The Relay stand-in is static (capsule r 0.3, 1.75 m + head 0.30,
/// top 2.05, (0.05, 0.05, 0.06)), with a collider, never animated. Every font or material lookup that misses logs once and
/// carries on with a plain stand-in.
/// </summary>
public static class FrontRoomsStructuresSigns
{
    public static readonly Color PanelColor = new Color(.086f, .082f, .075f);
    public static readonly Color TextColor = new Color(.957f, .945f, .91f);
    static readonly Color Grey = new Color(.588f, .576f, .545f);
    static readonly Color LightGrey = new Color(.74f, .73f, .70f);
    static readonly Color Accent = new Color(.957f, .875f, .231f);
    public const int WrapChars = 33;
    // TextMesh: a line of fontSize F at characterSize C is about F x C / 10 m tall (the em). One TextMesh a line, so the
    // pitch is exact. TextMesh rasters each glyph at fontSize px per em and the font atlas has no mipmaps, so each line takes
    // its own raster: rho = 2 x 691.2 / (1000 d) px per mm at the distance d it must read at, fontSize = round(em x rho)
    // (平面视觉, research/showcase_scenes/20_placard_type.md v2.2: chip and title at FAR 2.5 m, the rest at sqrt(2.5 x 1.41)).
    const int FarPx = 32, NearPx = 24;
    const float PanelWidth = 1.2f, PanelMinHeight = .8f, Margin = .06f, TopEdge = 2.05f;
    const float TitleEm = .058f, TitlePitch = .0725f, BodyEm = .033f, Pitch = .044f, ChipCap = .040f, ChipEm = .0573f, BarWidth = .006f;
    const float ChipPad = .009f, Keyline = .0012f, TitleGap = .06f, SectionGap = .03f, LabelColumn = .16f, NumberColumn = .036f;

    static Font bold, regular, mono;
    static bool fontsTried;

    static void Fonts()
    {
        if (fontsTried) return;
        fontsTried = true;
        bold = Resources.Load<Font>("Fonts/NotoSansSC-Bold");
        regular = Resources.Load<Font>("Fonts/NotoSansSC-Regular");
        mono = Resources.Load<Font>("Fonts/IBMPlexMono-Medium");
        if (bold == null || regular == null) FrontRoomsPoolMaterials.LogOnce("fonts", "NotoSansSC is missing from Resources/Fonts: the placards use the built-in font.");
        if (bold == null) bold = regular;
        if (regular == null) regular = bold;
        if (mono == null) mono = bold;
    }

    static readonly Dictionary<Font, Material> depthText = new Dictionary<Font, Material>();
    static bool rebuildHooked;

    /// <summary>
    /// The font's material with the depth test on. A font's own material is GUI/Text Shader, whose ZTest comes from
    /// unity_GUIZTestMode, which no one sets in a 3D scene, so placard and mark text drew through walls and from behind its own
    /// panel (scene 4 review SP3-F1, SP4-F4, SP1-F12). One runtime copy per font at LessEqual (the text stands 7 mm in front of
    /// its panel); it follows the font's texture when a dynamic font rebuilds it.
    /// </summary>
    public static Material DepthText(Font font)
    {
        if (font == null || font.material == null) return null;
        if (!rebuildHooked)
        {
            rebuildHooked = true;
            Font.textureRebuilt += f =>
            {
                if (f != null && f.material != null && depthText.TryGetValue(f, out var copy) && copy != null) copy.mainTexture = f.material.mainTexture;
            };
        }
        if (!depthText.TryGetValue(font, out var m) || m == null)
        {
            m = new Material(font.material) { name = font.material.name + " (depth-tested, scene 4)" };
            m.SetFloat("unity_GUIZTestMode", (float)CompareFunction.LessEqual);
            depthText[font] = m;
        }
        m.mainTexture = font.material.mainTexture;
        return m;
    }

    /// <summary>The body font (for the guide panel's IMGUI style); may be null.</summary>
    public static Font BodyFontAsset { get { Fonts(); return regular; } }
    public static Font TitleFontAsset { get { Fonts(); return bold; } }

    const string Closers = "，。、；：！？）」』)";
    const string Openers = "（「『(";
    const string Units = "米级层度格秒次倍";

    /// <summary>Wrap text at <paramref name="chars"/> ems (CJK one, Latin about half), keeping given line breaks. Words never
    /// split: a Latin or number run (with a unit right after a number) is one word, a closing mark stays with what precedes it
    /// and an opening mark with what follows; a last line of two ems or less takes the word before it along (review SP1-F17,
    /// SP2-F10, SP4 and SP5-F10: "Rela / y", "Run / !", "6 / 米", "A）" alone).</summary>
    public static string Wrap(string text, int chars = WrapChars)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var sb = new StringBuilder();
        foreach (var para in text.Split('\n'))
        {
            var lines = new List<List<string>>();
            var line = new List<string>();
            var width = 0f;
            foreach (var t in Words(para))
            {
                var w = Ems(t);
                if (t == " " && (line.Count == 0 || width + w > chars)) continue;
                if (width + w > chars && line.Count > 0)
                {
                    lines.Add(line);
                    line = new List<string>();
                    width = 0f;
                }
                line.Add(t);
                width += w;
            }
            lines.Add(line);
            if (lines.Count >= 2)
            {
                var last = lines[lines.Count - 1];
                var prev = lines[lines.Count - 2];
                while (prev.Count > 0 && prev[prev.Count - 1] == " ") prev.RemoveAt(prev.Count - 1);
                var lastW = 0f;
                foreach (var t in last) lastW += Ems(t);
                if (lastW <= 2.01f && prev.Count > 1 && lastW + Ems(prev[prev.Count - 1]) <= chars)
                {
                    last.Insert(0, prev[prev.Count - 1]);
                    prev.RemoveAt(prev.Count - 1);
                }
            }
            foreach (var l in lines) sb.Append(string.Concat(l).Trim()).Append('\n');
        }
        return sb.ToString().TrimEnd('\n');
    }

    static float Ems(string t)
    {
        var w = 0f;
        foreach (var ch in t) w += ch < 0x2E80 ? .55f : 1f;
        return w;
    }

    static bool WordChar(char c) => c < 0x2E80 && c != ' ' && Closers.IndexOf(c) < 0 && Openers.IndexOf(c) < 0;

    // The unbreakable pieces of a paragraph: openers + (a Latin or number run [+ a unit] | one character) + closers; spaces alone.
    static List<string> Words(string para)
    {
        var list = new List<string>();
        var i = 0;
        while (i < para.Length)
        {
            var start = i;
            if (para[i] == ' ') { list.Add(" "); i++; continue; }
            while (i < para.Length && Openers.IndexOf(para[i]) >= 0) i++;
            if (i < para.Length)
            {
                if (WordChar(para[i]))
                {
                    while (i < para.Length && WordChar(para[i])) i++;
                    if (i < para.Length && Units.IndexOf(para[i]) >= 0 && char.IsDigit(para[i - 1])) i++;
                }
                else if (para[i] != ' ') i++;
            }
            while (i < para.Length && Closers.IndexOf(para[i]) >= 0) i++;
            if (i == start) i++;
            list.Add(para.Substring(start, i - start));
        }
        return list;
    }

    static string[] Lines(string text, float widthMetres, float em)
    {
        var chars = Mathf.Max(4, Mathf.FloorToInt(widthMetres / em));
        var w = Wrap(text, chars);
        return string.IsNullOrEmpty(w) ? new string[0] : w.Split('\n');
    }

    /// <summary>
    /// A placard standing at <paramref name="centre"/> (world: its x and z; the panel hangs from 2.05 m over the floor),
    /// facing <paramref name="viewer"/> (level). Returns the placard's root.
    /// </summary>
    public static GameObject Placard(Transform parent, Vector3 centre, Vector3 viewer, string title, string body) =>
        Placard(parent, centre, viewer, title, body, centre.y - 1.5f);

    /// <summary>As above, with the floor under it (world y): the top edge sits 2.05 m over it, two posts reach down to it.</summary>
    public static GameObject Placard(Transform parent, Vector3 centre, Vector3 viewer, string title, string body, float floorY)
    {
        Fonts();
        var go = new GameObject("Placard · " + Clip(title));
        go.transform.SetParent(parent, true);
        var toSign = centre - viewer;
        toSign.y = 0f;
        if (toSign.sqrMagnitude < 1e-4f) toSign = Vector3.forward;
        // The root sits on the panel's top edge, centred; children hang below it (local -y), the front face is local -Z.
        go.transform.SetPositionAndRotation(new Vector3(centre.x, floorY + TopEdge, centre.z), Quaternion.LookRotation(toSign.normalized, Vector3.up));
        var root = go.transform;
        var left = -PanelWidth * .5f + Margin;
        var right = PanelWidth * .5f - Margin;
        var y = -Margin;

        // Title row: the code chip, then the title.
        SplitTitle(title, out var code, out var name);
        var x = left;
        var rowHeight = TitlePitch;
        if (code.Length > 0)
        {
            var textW = code.Length * .6f * ChipEm;
            var chipW = textW + 2f * (ChipPad + Keyline);
            var chipH = ChipCap + 2f * (ChipPad + Keyline);
            var cy = y - chipH * .5f - (TitlePitch - chipH) * .5f;
            Keyline4(root, new Vector3(x + chipW * .5f, cy, -.001f), chipW, chipH);
            Line(root, "Code", code, mono, ChipEm, FarPx, TextColor, new Vector3(x + ChipPad + Keyline, cy + ChipCap * .5f + ChipEm * .30f, -.002f));
            x += chipW + .016f;
            rowHeight = Mathf.Max(rowHeight, chipH);
        }
        var titleLines = Lines(name, right - x, TitleEm);
        for (var i = 0; i < titleLines.Length; i++)
            Line(root, "Title", titleLines[i], bold, TitleEm, FarPx, TextColor, new Vector3(x, y - i * TitlePitch, -.002f));
        y -= Mathf.Max(rowHeight, titleLines.Length * TitlePitch) + TitleGap;

        // The body: paragraphs, the greybox row, the questions row. A paragraph that carries the Red-decides label after its start
        // splits there (70's words unchanged), so its question row forms as on every placard (review SP3-F7, SP4-F13).
        var paragraphs = new List<string>();
        foreach (var raw in (body ?? "").Split('\n'))
        {
            var p = raw.Trim();
            var q = p.IndexOf("要你决定：", System.StringComparison.Ordinal);
            if (q > 0) { paragraphs.Add(p.Substring(0, q).Trim()); paragraphs.Add(p.Substring(q)); }
            else paragraphs.Add(p);
        }
        var first = true;
        foreach (var raw in paragraphs)
        {
            var para = raw.Trim();
            if (para.Length == 0) continue;
            if (!first) y -= SectionGap;
            first = false;
            if (para.StartsWith("灰盒占位：", System.StringComparison.Ordinal))
            {
                Line(root, "Label", "灰盒占位", bold, BodyEm, NearPx, Grey, new Vector3(left, y, -.002f));
                var list = Lines(para.Substring(5), right - left - LabelColumn, BodyEm);
                for (var i = 0; i < list.Length; i++) Line(root, "Greybox", list[i], regular, BodyEm, NearPx, LightGrey, new Vector3(left + LabelColumn, y - i * Pitch, -.002f));
                y -= Mathf.Max(1, list.Length) * Pitch;
                continue;
            }
            if (QuestionRow(para, out var label, out var rest))
            {
                var top = y;
                var qx = left + LabelColumn;
                if (label.Length <= 4) Line(root, "Label", label, bold, BodyEm, NearPx, Accent, new Vector3(left, y, -.002f));
                else
                {
                    var head = Lines(label, right - left, BodyEm);
                    for (var i = 0; i < head.Length; i++) Line(root, "Label", head[i], bold, BodyEm, NearPx, Accent, new Vector3(left, y - i * Pitch, -.002f));
                    y -= head.Length * Pitch;
                }
                var questions = Numbered(rest);
                foreach (var q in questions)
                {
                    var hasNumber = q.Key.Length > 0;
                    if (hasNumber) Line(root, "Number", q.Key, bold, BodyEm, NearPx, Accent, new Vector3(qx, y, -.002f));
                    var tx = hasNumber ? qx + NumberColumn : qx;
                    var ql = Lines(q.Value, right - tx, BodyEm);
                    for (var i = 0; i < ql.Length; i++) Line(root, "Question", ql[i], bold, BodyEm, NearPx, TextColor, new Vector3(tx, y - i * Pitch, -.002f));
                    y -= Mathf.Max(1, ql.Length) * Pitch;
                }
                // The accent bar beside the whole row.
                Quad(root, "Accent bar", new Vector3(left - .021f, (top + y) * .5f + .004f, -.001f), new Vector2(BarWidth, top - y - .008f), Accent);
                continue;
            }
            var lines = Lines(para, right - left, BodyEm);
            for (var i = 0; i < lines.Length; i++) Line(root, "Body", lines[i], regular, BodyEm, NearPx, TextColor, new Vector3(left, y - i * Pitch, -.002f));
            y -= lines.Length * Pitch;
        }

        var height = Mathf.Max(PanelMinHeight, -y + Margin);
        // The panel: a thin matte box behind the text.
        var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "Panel";
        panel.transform.SetParent(root, false);
        panel.transform.localPosition = new Vector3(0f, -height * .5f, .02f);
        panel.transform.localScale = new Vector3(PanelWidth, height, .03f);
        var pr = panel.GetComponent<MeshRenderer>();
        pr.sharedMaterial = FrontRoomsPoolMaterials.Lit("placard panel E", PanelColor, .1f);
        pr.shadowCastingMode = ShadowCastingMode.Off;
        // Two posts so it stands, not floats (render only; the panel's collider stays).
        var foot = TopEdge - height;
        if (foot > .2f && foot < 3f)
        {
            for (var k = -1; k <= 1; k += 2)
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = "Post";
                Object.Destroy(post.GetComponent<Collider>());
                post.transform.SetParent(root, false);
                post.transform.localPosition = new Vector3(k * .52f, -height - foot * .5f, .03f);
                post.transform.localScale = new Vector3(.04f, foot, .04f);
                post.GetComponent<MeshRenderer>().sharedMaterial = FrontRoomsPoolMaterials.Lit("placard post", new Color(.2f, .2f, .2f), .3f, .6f);
            }
        }
        return go;
    }

    static string Clip(string s) => string.IsNullOrEmpty(s) ? "" : s.Length > 24 ? s.Substring(0, 24) : s;

    // "SP1 <middle dot> name" -> code "SP1" and the name; a title without a short code before the " <middle dot> " has no chip.
    static void SplitTitle(string title, out string code, out string name)
    {
        code = "";
        name = title ?? "";
        var i = name.IndexOf(" · ", System.StringComparison.Ordinal);
        if (i <= 0 || i > 8) return;
        code = name.Substring(0, i).Trim();
        name = name.Substring(i + 3).Trim();
    }

    // A questions row: the Red-decides label, the walk-and-tell-us label or the still-open label, then a full-width colon and
    // the questions.
    static bool QuestionRow(string para, out string label, out string rest)
    {
        label = rest = "";
        var i = para.IndexOf('\uFF1A');
        if (i <= 0 || i > 60) return false;
        var head = para.Substring(0, i);
        if (!head.StartsWith("要你决定", System.StringComparison.Ordinal) && !head.StartsWith("走过后告诉我们", System.StringComparison.Ordinal)
            && !head.StartsWith("还没定的", System.StringComparison.Ordinal)) return false;
        label = head;
        rest = para.Substring(i + 1).Trim();
        return true;
    }

    // "1 a? 2 b? 3 c" -> (1, a), (2, b), (3, c), splitting only on the next number in turn (" 2 ", then " 3 "...), so a
    // number inside a question never splits it; a row that does not start with "1 " is one unnumbered question.
    static List<KeyValuePair<string, string>> Numbered(string rest)
    {
        var list = new List<KeyValuePair<string, string>>();
        if (!rest.StartsWith("1 ", System.StringComparison.Ordinal))
        {
            list.Add(new KeyValuePair<string, string>("", rest));
            return list;
        }
        var n = 1;
        var at = 2;
        while (true)
        {
            var next = rest.IndexOf(" " + (n + 1) + " ", at, System.StringComparison.Ordinal);
            var end = next >= 0 ? next : rest.Length;
            list.Add(new KeyValuePair<string, string>(n.ToString(), rest.Substring(at, end - at).Trim()));
            if (next < 0) break;
            n++;
            at = next + (" " + n + " ").Length;
        }
        return list;
    }

    static void Line(Transform parent, string name, string text, Font font, float em, int fontPx, Color colour, Vector3 local)
    {
        if (string.IsNullOrEmpty(text)) return;
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;
        go.transform.localRotation = Quaternion.identity;
        var tm = go.AddComponent<TextMesh>();
        if (font != null) tm.font = font;
        tm.fontSize = fontPx;
        tm.characterSize = em * 10f / fontPx;
        tm.anchor = TextAnchor.UpperLeft;
        tm.alignment = TextAlignment.Left;
        tm.richText = false;
        tm.color = colour;
        tm.text = text;
        var r = go.GetComponent<MeshRenderer>();
        if (r != null)
        {
            var depth = DepthText(font);
            if (depth != null) r.sharedMaterial = depth;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
    }

    // The chip's keyline: four thin unlit quads round a rectangle.
    static void Keyline4(Transform parent, Vector3 centre, float w, float h)
    {
        Quad(parent, "Chip keyline", centre + new Vector3(0f, h * .5f - Keyline * .5f, 0f), new Vector2(w, Keyline), TextColor);
        Quad(parent, "Chip keyline", centre - new Vector3(0f, h * .5f - Keyline * .5f, 0f), new Vector2(w, Keyline), TextColor);
        Quad(parent, "Chip keyline", centre + new Vector3(w * .5f - Keyline * .5f, 0f, 0f), new Vector2(Keyline, h), TextColor);
        Quad(parent, "Chip keyline", centre - new Vector3(w * .5f - Keyline * .5f, 0f, 0f), new Vector2(Keyline, h), TextColor);
    }

    static void Quad(Transform parent, string name, Vector3 local, Vector2 size, Color colour)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = name;
        Object.Destroy(q.GetComponent<Collider>());
        q.transform.SetParent(parent, false);
        q.transform.localPosition = local;
        // A Quad faces -Z (its visible side looks down -Z): the placard's front.
        q.transform.localRotation = Quaternion.identity;
        q.transform.localScale = new Vector3(size.x, size.y, 1f);
        var r = q.GetComponent<MeshRenderer>();
        r.sharedMaterial = FrontRoomsPoolMaterials.Glow("placard ink " + colour.r.ToString("0.000"), colour);
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    // ---------------------------------------------------------------- pads

    /// <summary>A pad: a 0.9 m floor plate the walker uses with E (standing on it or aiming at it). Its key is the jump's.</summary>
    public sealed class Pad
    {
        public int key;
        public string label;
        public Vector3 centre;
        public Collider collider;
        public GameObject root;
    }

    public static readonly List<Pad> Pads = new List<Pad>();

    /// <summary>A pad at a floor point (world), with a thin lit rim; registered for the walker.</summary>
    public static Pad MakePad(Transform parent, Vector3 floorCentre, int key, string label, Color accent)
    {
        var go = new GameObject("Pad · " + key);
        go.transform.SetParent(parent, true);
        go.transform.position = floorCentre;
        var plate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        plate.name = "Plate";
        plate.transform.SetParent(go.transform, false);
        plate.transform.localPosition = new Vector3(0f, .01f, 0f);
        plate.transform.localScale = new Vector3(.9f, .01f, .9f);
        plate.GetComponent<MeshRenderer>().sharedMaterial = FrontRoomsPoolMaterials.Lit("pad plate", new Color(.32f, .33f, .34f), .4f, .5f);
        // Cylinder's capsule collider would trip the walker: a flat box instead.
        Object.Destroy(plate.GetComponent<Collider>());
        var box = go.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, .01f, 0f);
        box.size = new Vector3(.9f, .02f, .9f);
        var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rim.name = "Rim";
        Object.Destroy(rim.GetComponent<Collider>());
        rim.transform.SetParent(go.transform, false);
        rim.transform.localPosition = new Vector3(0f, .012f, 0f);
        rim.transform.localScale = new Vector3(.96f, .004f, .96f);
        var rr = rim.GetComponent<MeshRenderer>();
        rr.sharedMaterial = FrontRoomsPoolMaterials.Glow("pad rim " + accent, accent);
        rr.shadowCastingMode = ShadowCastingMode.Off;
        var pad = new Pad { key = key, label = label, centre = floorCentre, collider = box, root = go };
        Pads.Add(pad);
        return pad;
    }

    /// <summary>The pad the walker stands on (within 0.45 m of a pad's centre, at most 0.6 m above it), or null.</summary>
    public static Pad PadUnder(Vector3 feet)
    {
        foreach (var p in Pads)
        {
            if (p == null || p.root == null || !p.root.activeInHierarchy) continue;
            var d = feet - p.centre;
            if (Mathf.Abs(d.y) > .6f) continue;
            d.y = 0f;
            if (d.sqrMagnitude <= .45f * .45f) return p;
        }
        return null;
    }

    public static Pad PadOf(Collider c)
    {
        if (c == null) return null;
        foreach (var p in Pads) if (p != null && p.collider == c) return p;
        return null;
    }

    // ---------------------------------------------------------------- the Relay stand-in

    /// <summary>The static Relay stand-in (70 section 1, F38): standing at a floor point, facing a point.</summary>
    public static GameObject RelayStandIn(Transform parent, Vector3 floor, Vector3 facing)
    {
        var go = new GameObject("STAND-IN Relay (static, 2.05 m)");
        go.transform.SetParent(parent, true);
        var dir = facing - floor;
        dir.y = 0f;
        go.transform.SetPositionAndRotation(floor, dir.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(dir.normalized) : Quaternion.identity);
        var mat = FrontRoomsPoolMaterials.Lit("relay stand-in", new Color(.05f, .05f, .06f), .2f);
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(go.transform, false);
        body.transform.localPosition = new Vector3(0f, .875f, 0f);
        body.transform.localScale = new Vector3(.6f, .875f, .6f);
        body.GetComponent<MeshRenderer>().sharedMaterial = mat;
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(go.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.90f, 0f);
        head.transform.localScale = new Vector3(.30f, .30f, .30f);
        head.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }
}
