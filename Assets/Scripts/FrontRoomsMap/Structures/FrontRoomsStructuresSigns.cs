using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The structures scene's placards, pads and stand-ins (scene 4, RULES 5b; research/level_space/70 section 1; PLAN 2.5).
/// Placards are for Red, outside the fiction, set as the graphic line's district sign E (research/showcase_scenes/
/// 20_placard_type.md v2.4, 2026-11-04): a 1200 mm panel, its top edge 2.05 m over the floor under it, as tall as its
/// content plus 60 mm margins (at least 0.8 m), matte #161513; a code chip (IBM Plex Mono Medium, cap 40 mm, in a 1.2 mm
/// paper keyline) and the title (Noto Sans SC Bold, em 58 mm, broken only at a phrase); the body (Regular, em 33 mm, pitch
/// 44 mm); the greybox label (Bold, grey #96938B) with its list in light grey in a hanging column; the Red-decides label
/// (Bold, accent #F4DF3B, a 6 mm accent bar) with one numbered question a line (the number in accent, the question Bold in
/// paper, hanging); text unlit #F4F1E8. Two reading distances, eye to the face's centre: FAR 2.5 m, where each district's
/// arrival stands, for the code and title; NEAR standing 0.9 m in front, for the rest. The faces are the game's own
/// (Resources/Fonts). World text is TextMesh here (a stand-in for the game's world-space UI Text).
/// A district sign the arrival reads more than 35 degrees off its face also gets a blade (20_placard_type.md v2.5, E-blade;
/// Blade below): its code and title on a double-sided panel square to the wall, read face-on from the arrival.
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
    // (平面视觉, research/showcase_scenes/20_placard_type.md v2.4: chip and title at FAR 2.5 m, .55296 px/mm, 32 px a 58 mm
    // em; the rest at sqrt(2.5 x 0.92) between FAR and NEAR (standing 0.9 m in front, 0.92 m to the centre), .9113 px/mm,
    // 30 px a 33 mm em; v2.2 had NEAR 1.41 and 24 px).
    const int FarPx = 32, NearPx = 30;
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
    static bool rebuildHooked, textShaderTried;
    static Shader textShader;

    /// <summary>
    /// The font's material with the depth test on. A font's own material is GUI/Text Shader, whose ZTest comes from
    /// unity_GUIZTestMode, which no one sets in a 3D scene, so placard and mark text drew through walls and from behind its own
    /// panel (scene 4 review SP3-F1, SP4-F4, SP1-F12). A copy of the font's material with unity_GUIZTestMode set on it did not
    /// change that (11-04 sign_blades: ghost glyphs through the spine's wall right of SP3's V1 sign in review10 and review11, and
    /// a blade's far face mirrored through its panel), so each font gets one material on the structures-only text shader
    /// (Assets/Structures/FrontRoomsStructuresPlacardText.shader: GUI/Text's pass with ZTest LEqual written out; the text
    /// stands 7 mm in front of its panel); the old copy stays the fallback when that shader is missing. It follows the font's
    /// texture when a dynamic font rebuilds it.
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
        if (!textShaderTried)
        {
            textShaderTried = true;
            textShader = Shader.Find("FrontRooms/Structures/Placard Text");
            if (textShader == null) FrontRoomsPoolMaterials.LogOnce("placard text shader", "FrontRooms/Structures/Placard Text is missing: placard text keeps the font's material (it may draw through walls).");
        }
        if (!depthText.TryGetValue(font, out var m) || m == null)
        {
            if (textShader != null)
            {
                m = new Material(textShader) { name = font.material.name + " (depth-tested, scene 4)" };
                m.SetColor("_Color", Color.white);
                m.SetFloat("_Cull", (float)CullMode.Off);
            }
            else
            {
                m = new Material(font.material) { name = font.material.name + " (depth-tested, scene 4)" };
                m.SetFloat("unity_GUIZTestMode", (float)CompareFunction.LessEqual);
            }
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

    const string CloseBrackets = "）」』)";

    /// <summary>
    /// A title's lines (20_placard_type.md v2.4, E): a title that fits stays one line; a longer one breaks only at a phrase,
    /// before an opening bracket or after the full-width colon, never inside a bracket, whole phrases filling each line
    /// ("滑梯：瓷砖墙在下面托着" / "（你选的 A）"; Wrap had set "…（你选" / "的A）"). Every scene-4 title keeps the rule
    /// as written; for one that could not, two guards: a phrase wider than the line wraps as the body does, and a last line of
    /// one or two characters is logged once (change that title's words, in 70 and in its district).
    /// </summary>
    static string[] TitleLines(string name, float widthMetres, float em = TitleEm)
    {
        var chars = Mathf.Max(4, Mathf.FloorToInt(widthMetres / em));
        var lines = new List<string>();
        foreach (var raw in (name ?? "").Split('\n'))
        {
            var para = raw.Trim();
            if (para.Length == 0) continue;
            if (Ems(para) <= chars) { lines.Add(para); continue; }
            var first = lines.Count;
            var line = "";
            foreach (var phrase in Phrases(para))
            {
                if (line.Trim().Length > 0 && Ems((line + phrase).Trim()) <= chars) { line += phrase; continue; }
                if (line.Trim().Length > 0) lines.Add(line.Trim());
                line = phrase;
            }
            if (line.Trim().Length > 0) lines.Add(line.Trim());
            for (var i = lines.Count - 1; i >= first; i--)
            {
                if (Ems(lines[i]) <= chars) continue;
                var wrapped = Wrap(lines[i], chars).Split('\n');
                lines.RemoveAt(i);
                lines.InsertRange(i, wrapped);
            }
            if (lines.Count - first >= 2 && lines[lines.Count - 1].Replace(" ", "").Length <= 2)
                FrontRoomsPoolMaterials.LogOnce("title " + para, "placard title \"" + para + "\" ends on a line of one or two characters: change its words (20_placard_type.md v2.4).");
        }
        return lines.ToArray();
    }

    // A title paragraph's phrases: a break before an opening bracket and after the full-width colon, both outside brackets.
    static List<string> Phrases(string para)
    {
        var list = new List<string>();
        var sb = new StringBuilder();
        var depth = 0;
        foreach (var ch in para)
        {
            if (Openers.IndexOf(ch) >= 0)
            {
                if (depth == 0 && sb.ToString().Trim().Length > 0) { list.Add(sb.ToString()); sb.Length = 0; }
                depth++;
                sb.Append(ch);
                continue;
            }
            sb.Append(ch);
            if (CloseBrackets.IndexOf(ch) >= 0) depth = Mathf.Max(0, depth - 1);
            else if (ch == '\uFF1A' && depth == 0) { list.Add(sb.ToString()); sb.Length = 0; }
        }
        if (sb.Length > 0) list.Add(sb.ToString());
        return list;
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
        var titleLines = TitleLines(name, right - x);
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

    // ---------------------------------------------------------------- the district blade (E-blade)

    // 平面视觉's E-blade (research/showcase_scenes/20_placard_type.md v2.5, 2026-11-04 21:4x): a district sign that hangs flat on
    // a corridor wall is read 55-57 degrees off its face from the arrival, its characters squeezed to cos 57 = .54 of their
    // width, which no size cures; its code and title also go on a blade. A double-sided panel square to the wall, 1200 mm wide,
    // as tall as its row plus 60 mm margins, its inner edge 50 mm off the wall and its bottom edge 2.10 m over the floor under it
    // (heads clear it; the ceiling stays at least 0.1 m over its top); on each face the E sign's code chip (IBM Plex Mono Medium,
    // cap 42.5 mm, E's keyline and padding) and the title (Noto Sans SC Bold, em 62 mm, broken only at a phrase, two lines if it
    // needs them), both rastered at 34 px per em (2 f / (1000 x 2.5 m) = .553 px/mm: the arrival reads it from 2.5 m, eye to the
    // face's centre, looking up 11-15 degrees: 16.5 / 11.3 px). Nothing else: the flat sign stays for the body, read standing
    // 0.9 m in front. E's matte panel; two short standoffs reach the wall; no collider (it hangs over heads), no shadow.
    // Rule (v2.5): a district sign the arrival reads more than 35 degrees off its face gets a blade.
    const float BladeWidth = 1.2f, BladeBottom = 2.10f, BladeGap = .05f, BladeDepth = .03f;
    const float BladeTitleEm = .062f, BladeTitlePitch = .0775f, BladeChipCap = .0425f, BladeChipEm = .0609f, BladeChipGap = .016f;
    const int BladePx = 34;
    // The title's measure, 12 ems: "two lines at a phrase if it needs them" (v2.5). The arrival stands 0.77 m to the side of the
    // blade's centre, so a line runs away from the eye toward the wall; at 2.5 m one that ends more than 0.36 m past the panel's
    // centre falls under E's 16 px floor at its far end (review11, 11-04: 15.7 px at 0.54 m, 16.0 at 0.35 m; 17.5 at its start).
    const float BladeMeasure = .75f;
    // A TextMesh line's baseline lies one face ascender under its top (the fonts import their ascent from the face's ascender),
    // so a line's top stands this many ems over what the row centres: Plex Mono's cap (ascender 1.025 less half its cap .698)
    // and Noto Sans SC's ideographic em box (ascender 1.160 less the box's centre .380; the box runs -.120 to +.880).
    const float MonoCapTop = .676f, CjkBoxTop = .78f;
    // The text and the chip's keyline stand 7 and 6 mm off the panel's face, as on E.
    const float TextLift = .007f, KeylineLift = .006f;

    /// <summary>
    /// A district blade (E-blade) hung from a wall. <paramref name="wallFace"/>: a point on the wall's face (world; its x and z
    /// place the blade along the wall); <paramref name="into"/>: the wall's normal into the room; <paramref name="viewer"/>: the
    /// arrival's eye, which the front face looks back toward along the wall; <paramref name="floorY"/>: the floor under it
    /// (world y). Both faces carry the code chip and the title, each the right way round from its own side. Returns the blade's
    /// root, on the panel's top edge, centred (the front face is local -Z, the back +Z).
    /// </summary>
    public static GameObject Blade(Transform parent, Vector3 wallFace, Vector3 into, Vector3 viewer, string title, float floorY)
    {
        Fonts();
        into.y = 0f;
        into = into.sqrMagnitude > 1e-6f ? into.normalized : Vector3.right;
        SplitTitle(title, out var code, out var name);
        var left = -BladeWidth * .5f + Margin;
        var right = BladeWidth * .5f - Margin;
        var chipW = code.Length > 0 ? code.Length * .6f * BladeChipEm + 2f * (ChipPad + Keyline) : 0f;
        var chipH = code.Length > 0 ? BladeChipCap + 2f * (ChipPad + Keyline) : 0f;
        var titleX = code.Length > 0 ? left + chipW + BladeChipGap : left;
        var lines = TitleLines(name, Mathf.Min(right - titleX, BladeMeasure), BladeTitleEm);
        var block = lines.Length > 0 ? BladeTitleEm + (lines.Length - 1) * BladeTitlePitch : 0f;
        var row = Mathf.Max(chipH, block);
        var height = row + 2f * Margin;

        var along = Vector3.Cross(Vector3.up, into);
        var centre = wallFace + into * (BladeGap + BladeWidth * .5f);
        centre.y = floorY + BladeBottom + height * .5f;
        var toViewer = viewer - centre;
        toViewer.y = 0f;
        var front = Vector3.Dot(along, toViewer) >= 0f ? along : -along;
        var go = new GameObject("Blade · " + Clip(title));
        go.transform.SetParent(parent, true);
        go.transform.SetPositionAndRotation(new Vector3(centre.x, floorY + BladeBottom + height, centre.z), Quaternion.LookRotation(-front, Vector3.up));
        var root = go.transform;

        var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "Panel";
        Object.Destroy(panel.GetComponent<Collider>());
        panel.transform.SetParent(root, false);
        panel.transform.localPosition = new Vector3(0f, -height * .5f, 0f);
        panel.transform.localScale = new Vector3(BladeWidth, height, BladeDepth);
        var pr = panel.GetComponent<MeshRenderer>();
        pr.sharedMaterial = FrontRoomsPoolMaterials.Lit("placard panel E", PanelColor, .1f);
        pr.shadowCastingMode = ShadowCastingMode.Off;

        // Two standoffs from the panel's inner edge to the wall (2 mm into each), a quarter and three quarters down its height.
        var wallSide = Vector3.Dot(root.right, -into) > 0f ? 1f : -1f;
        for (var k = 0; k < 2; k++)
        {
            var stay = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stay.name = "Standoff";
            Object.Destroy(stay.GetComponent<Collider>());
            stay.transform.SetParent(root, false);
            stay.transform.localPosition = new Vector3(wallSide * (BladeWidth + BladeGap) * .5f, -height * (k == 0 ? .25f : .75f), 0f);
            stay.transform.localScale = new Vector3(BladeGap + .004f, .03f, .018f);
            var sr = stay.GetComponent<MeshRenderer>();
            sr.sharedMaterial = FrontRoomsPoolMaterials.Lit("placard post", new Color(.2f, .2f, .2f), .3f, .6f);
            sr.shadowCastingMode = ShadowCastingMode.Off;
        }

        // Each face in its own frame, the front as built and the back turned half round, so both read left to right: the chip,
        // then the title, the chip's cap and the title's em box centred on the first line's middle.
        var top = -Margin;
        var firstCentre = top - (row - block) * .5f - BladeTitleEm * .5f;
        var chipCentre = lines.Length > 0 ? firstCentre : top - row * .5f;
        for (var side = 0; side < 2; side++)
        {
            var face = new GameObject(side == 0 ? "Front" : "Back").transform;
            face.SetParent(root, false);
            face.localPosition = new Vector3(0f, 0f, (side == 0 ? -.5f : .5f) * BladeDepth);
            face.localRotation = side == 0 ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
            if (code.Length > 0)
            {
                Keyline4(face, new Vector3(left + chipW * .5f, chipCentre, -KeylineLift), chipW, chipH);
                Line(face, "Code", code, mono, BladeChipEm, BladePx, TextColor, new Vector3(left + ChipPad + Keyline, chipCentre + MonoCapTop * BladeChipEm, -TextLift));
            }
            for (var i = 0; i < lines.Length; i++)
                Line(face, "Title", lines[i], bold, BladeTitleEm, BladePx, TextColor, new Vector3(titleX, firstCentre - i * BladeTitlePitch + CjkBoxTop * BladeTitleEm, -TextLift));
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
