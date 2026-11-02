using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The game's languages (Red, 2026-10-30; Documentation/research/localization/README.md, D1–D5). One table per language in
/// Resources/Language/&lt;code&gt;.txt: tab-separated lines of the English string, its translation and a note for whoever
/// checks it. The English string is the key, so a missing row shows English, and while the game is in English nothing is
/// looked up at all: every string stays exactly what it was.
///
/// The world stays English (signs, printed sheets, the CRT, the logo). Only what the player reads to play goes through
/// T(). A language's own name never does: it is a constant here, in its own script, whatever language the game is in.
///
/// Bayon has Basic Latin only, so any other language swaps the whole face, never a glyph: a view calls Style(text) once
/// after setting a Text's font and size, and the Text follows the language from then on (LANGUAGE_MARKS.md, type by script).
/// </summary>
public static class FrontRoomsLanguage
{
    public sealed class Language
    {
        public readonly string Code;
        /// <summary>Its name in its own language and script: never translated, never in a table.</summary>
        public readonly string OwnName;
        /// <summary>The short name on its key tag (LANGUAGE_MARKS.md): as constant as OwnName.</summary>
        public readonly string Token;
        /// <summary>The table, fonts and width check are done. Only these reach a public build's list (D5).</summary>
        public readonly bool Finished;
        /// <summary>Resources path of the face that replaces Bayon, and its size against Bayon's; null keeps Bayon (English).</summary>
        public readonly string LabelFace;
        public readonly float LabelScale;
        /// <summary>Resources path of the face that replaces Source Serif 4; null keeps it.</summary>
        public readonly string TextFace;
        /// <summary>Resources path of the face that replaces IBM Plex Mono (the regular gothic). Set for CJK only, which also
        /// raises small text to its size floor (MetaFloor).</summary>
        public readonly string MetaFace;

        public Language(string code, string ownName, string token, bool finished, string labelFace = null, float labelScale = 1f, string textFace = null, string metaFace = null)
        {
            Code = code;
            OwnName = ownName;
            Token = token;
            Finished = finished;
            LabelFace = labelFace;
            LabelScale = labelScale;
            TextFace = textFace;
            MetaFace = metaFace;
        }
    }

    const string PreferenceKey = "FrontRooms.Language";
    // Oswald Bold at 0.85 × Bayon's size has Bayon's cap height (平面视觉, LANGUAGE_MARKS.md).
    const string Oswald = "Fonts/Oswald-Bold";
    const float OswaldScale = .85f;

    /// <summary>Every language, in the order of the list (README §7.1): English, then by share of Steam's players.</summary>
    public static readonly Language[] All =
    {
        new Language("en", "English", "EN", true),
        new Language("zh-Hans", "简体中文", "简", false, "Fonts/NotoSansSC-Bold", .9f, "Fonts/NotoSerifSC-Regular", "Fonts/NotoSansSC-Regular"),
        new Language("ru", "Русский", "РУ", false, Oswald, OswaldScale),
        new Language("es", "Español", "ES", false, Oswald, OswaldScale),
        new Language("pt-BR", "Português (Brasil)", "PT", false, Oswald, OswaldScale),
        new Language("de", "Deutsch", "DE", false, Oswald, OswaldScale),
        new Language("fr", "Français", "FR", false, Oswald, OswaldScale),
        new Language("ja", "日本語", "日", false, "Fonts/NotoSansJP-Bold", .9f, "Fonts/NotoSerifJP-Regular", "Fonts/NotoSansJP-Regular"),
        new Language("ko", "한국어", "한", false, "Fonts/NotoSansKR-Bold", .9f, "Fonts/NotoSerifKR-Regular", "Fonts/NotoSansKR-Regular"),
        new Language("zh-Hant", "繁體中文", "繁", false, "Fonts/NotoSansTC-Bold", .9f, "Fonts/NotoSerifTC-Regular", "Fonts/NotoSansTC-Regular"),
    };

    static Dictionary<string, string> table;
    static Font labelFont, textFont, metaFont;

    // A Text's English look, kept so it can follow any later change of language.
    struct Look
    {
        public Font Font;
        public int Size;
        public string English;
    }

    static readonly Dictionary<Text, Look> styled = new Dictionary<Text, Look>(256);
    static readonly List<Text> gone = new List<Text>();
    static int sweepAt = 512;
    static List<Language> listed;

    public static Language Current { get; private set; } = All[0];
    public static bool IsEnglish => table == null;

    /// <summary>Raised after the language changes; anything showing text sets it again.</summary>
    public static event Action Changed;

    /// <summary>Whether the list shows this language: every one of them, in a release build too (Red, 2026-10-31 17:17, L5:
    /// "B. 简体中文先不审，直接上线，以后再审 / C. 10 种语言都用 AI 初稿直接上线": all ten are listed and pickable as AI
    /// drafts, and Finished stays the review status the strip dims by, so 简体中文 still reads as unreviewed until Red signs
    /// it off). FromDevice still never lands a player in a half-done table. WebGL lists English alone (its own track).</summary>
    public static bool Listed(Language language)
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer) return language == All[0];
        return true;
    }

    /// <summary>The LANGUAGE page's rows, in the list's order.</summary>
    public static List<Language> ListedLanguages
    {
        get
        {
            if (listed != null) return listed;
            listed = new List<Language>(All.Length);
            foreach (var language in All) if (Listed(language)) listed.Add(language);
            return listed;
        }
    }

    /// <summary>The Settings row exists only when there is something to choose: a public build with English alone has none.</summary>
    public static bool HasChoice => ListedLanguages.Count > 1;

    /// <summary>The string in the current language, or the English one when its table has no row for it.</summary>
    public static string T(string english)
    {
        if (table == null || string.IsNullOrEmpty(english)) return english;
        return table.TryGetValue(english, out var translated) ? translated : english;
    }

    /// <summary>T() for a line built around a value: the table moves {0} to where its language wants it.</summary>
    public static string F(string english, string value) => T(english).Replace("{0}", value);

    static readonly Dictionary<string, Dictionary<string, string>> others = new Dictionary<string, Dictionary<string, string>>();

    /// <summary>T() into another language without switching to it: the LANGUAGE preview shows the game's words in the language
    /// under the pointer before it is picked (SETTINGS_MOTION.md). Each table is read once, on first use.</summary>
    public static string TIn(Language language, string english)
    {
        if (language == null || language == All[0] || string.IsNullOrEmpty(english)) return english;
        if (language == Current) return T(english);
        if (!others.TryGetValue(language.Code, out var rows))
        {
            rows = Parse(Resources.Load<TextAsset>("Language/" + language.Code)?.text);
            others[language.Code] = rows;
        }
        return rows.TryGetValue(english, out var translated) ? translated : english;
    }

    /// <summary>A language's face for Source Serif 4's role, or <paramref name="english"/> where it keeps that face.</summary>
    public static Font TextFaceOf(Language language, Font english) => FaceAt(language?.TextFace, english);

    /// <summary>A language's face for IBM Plex Mono's role, or <paramref name="english"/> where it keeps that face.</summary>
    public static Font MetaFaceOf(Language language, Font english) => FaceAt(language?.MetaFace, english);

    static Font FaceAt(string path, Font english)
    {
        if (path == null) return english;
        var face = Resources.Load<Font>(path);
        return face != null ? face : english;
    }

    /// <summary>Call once after setting a Text's font and size: it takes this language's face and follows any change. A fixed
    /// word passes its English string too, and then needs no refresh: the Text sets itself again on a change of language.</summary>
    public static void Style(Text text, string english = null)
    {
        if (text == null || styled.ContainsKey(text)) return;
        if (styled.Count >= sweepAt) Sweep();
        var look = new Look { Font = text.font, Size = text.fontSize, English = english };
        styled[text] = look;
        if (table != null) Dress(text, look);
    }

    static void Sweep()
    {
        gone.Clear();
        foreach (var pair in styled) if (pair.Key == null) gone.Add(pair.Key);
        foreach (var text in gone) styled.Remove(text);
        sweepAt = Mathf.Max(512, styled.Count * 2);
    }

    /// <summary>The smallest size dense CJK stays clear at (LANGUAGE_MARKS.md): measured on desktop, a judgement on phone and tablet.</summary>
    static int MetaFloor => FrontRoomsHandheld.Active ? (FrontRoomsHandheld.IsTablet ? 13 : 12) : 16;

    static void Dress(Text text, Look look)
    {
        var name = look.Font != null ? look.Font.name : string.Empty;
        var label = name.StartsWith("Bayon", StringComparison.Ordinal) && labelFont != null;
        var serif = name.StartsWith("SourceSerif", StringComparison.Ordinal) && textFont != null;
        var meta = name.StartsWith("IBMPlexMono", StringComparison.Ordinal) && Current.MetaFace != null;
        text.font = label ? labelFont : serif ? textFont : meta && metaFont != null ? metaFont : look.Font;
        text.fontSize = label ? Mathf.RoundToInt(look.Size * Current.LabelScale) : meta ? Mathf.Max(look.Size, MetaFloor) : look.Size;
        if (!string.IsNullOrEmpty(look.English)) text.text = T(look.English);
    }

    /// <summary>A label's width in this language's face (FrontRoomsSettingsMenu.TextWidth keeps its own Bayon table for English).</summary>
    public static float Measure(string text, float size, bool mono)
    {
        var scale = mono ? 1f : Current.LabelScale;
        var font = mono ? metaFont : labelFont;
        var px = Mathf.Max(1, Mathf.RoundToInt(size * scale));
        if (mono && Current.MetaFace != null) px = Mathf.Max(px, MetaFloor);
        if (font != null) font.RequestCharactersInTexture(text, px);
        var width = 0f;
        foreach (var c in text)
        {
            if (font != null && font.GetCharacterInfo(c, out var glyph, px)) width += glyph.advance;
            else width += px * (c >= '\u2E80' ? 1f : mono ? .6f : .5f);
        }
        return width;
    }

    /// <summary>A label padded to the detail column's value column (<paramref name="columns"/> characters of IBM Plex Mono at
    /// 13 px: the key map's rows, the desktop sheet's MetaLine). Plex Mono pads by count; a CJK language's gothic is proportional
    /// and raised to MetaFloor, so it pads by measured width to the same column at that size. Either way at least two spaces
    /// part the label from its value (a 15-letter German or Spanish label used to run into it).</summary>
    public static string MetaColumn(string text, int columns = 15)
    {
        text = text ?? string.Empty;
        if (Current.MetaFace == null || metaFont == null) return text.Length > columns - 2 ? text + "  " : text.PadRight(columns);
        var space = Mathf.Max(1f, Measure(" ", 13f, true));
        var target = columns * .6f * Mathf.Max(13, MetaFloor);
        return text + new string(' ', Mathf.Max(2, Mathf.CeilToInt((target - Measure(text, 13f, true)) / space)));
    }

    /// <summary>Called once at start, with the settings. Until the player picks, the game follows the device at each launch.</summary>
    public static void Load()
    {
        // A pick saved in a test build, where unfinished languages are listed, must not open a public build in a language it
        // lists no row to leave: it starts as a first launch does (2026-10-31).
        var saved = Find(PlayerPrefs.GetString(PreferenceKey, string.Empty));
        Apply(saved != null && Listed(saved) ? saved : FromDevice(Application.systemLanguage));
#if UNITY_EDITOR
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == "-frLanguage") Apply(Find(args[i + 1]) ?? Current);
#endif
    }

    /// <summary>The player's pick: saved, in force at once.</summary>
    public static void Pick(Language language)
    {
        if (language == null) return;
        PlayerPrefs.SetString(PreferenceKey, language.Code);
        PlayerPrefs.Save();
        if (language == Current) return;
        Apply(language);
        Changed?.Invoke();
    }

    public static Language Find(string code)
    {
        foreach (var language in All) if (language.Code == code) return language;
        return null;
    }

    /// <summary>The device's language if it is finished and listed (WebGL: English), else English: a player never lands in a
    /// half-done table.</summary>
    public static Language FromDevice(SystemLanguage device)
    {
        var language = Find(CodeOf(device));
        return language != null && language.Finished && Listed(language) ? language : All[0];
    }

    static string CodeOf(SystemLanguage device)
    {
        switch (device)
        {
            case SystemLanguage.Chinese:
            case SystemLanguage.ChineseSimplified: return "zh-Hans";
            case SystemLanguage.ChineseTraditional: return "zh-Hant";
            case SystemLanguage.Russian: return "ru";
            case SystemLanguage.Spanish: return "es";
            case SystemLanguage.Portuguese: return "pt-BR";
            case SystemLanguage.German: return "de";
            case SystemLanguage.French: return "fr";
            case SystemLanguage.Japanese: return "ja";
            case SystemLanguage.Korean: return "ko";
            default: return "en";
        }
    }

    static void Apply(Language language)
    {
        Current = language;
        table = language == All[0] ? null : Parse(Resources.Load<TextAsset>("Language/" + language.Code)?.text);
        labelFont = language.LabelFace != null ? Resources.Load<Font>(language.LabelFace) : null;
        textFont = language.TextFace != null ? Resources.Load<Font>(language.TextFace) : null;
        metaFont = language.MetaFace != null ? Resources.Load<Font>(language.MetaFace) : null;
        Sweep();
        foreach (var pair in styled) Dress(pair.Key, pair.Value);
    }

#if UNITY_EDITOR
    /// <summary>A language for a test: nothing is saved.</summary>
    public static void PreviewForTest(Language language) => Apply(language);
#endif

    /// <summary>A table's rows: English, tab, translation, then an optional note. Lines starting with # are notes; \n in a cell is a line break.</summary>
    public static Dictionary<string, string> Parse(string text)
    {
        var rows = new Dictionary<string, string>(512);
        if (string.IsNullOrEmpty(text)) return rows;
        foreach (var line in text.Split('\n'))
        {
            if (line.Length == 0 || line[0] == '#') continue;
            var first = line.IndexOf('\t');
            if (first <= 0) continue;
            var second = line.IndexOf('\t', first + 1);
            var translated = second < 0 ? line.Substring(first + 1) : line.Substring(first + 1, second - first - 1);
            if (translated.Length > 0) rows[Unescape(line.Substring(0, first))] = Unescape(translated);
        }
        return rows;
    }

    static string Unescape(string cell) => cell.Replace("\\n", "\n");
}
