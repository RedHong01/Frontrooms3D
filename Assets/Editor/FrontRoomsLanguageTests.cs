using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The language tables and the list (Documentation/research/localization/README.md): parsing, English staying exactly
/// what it was, a missing row falling back to English, and a half-done language never reaching a player by itself.
/// Batch: -executeMethod FrontRoomsLanguageTests.RunBatch (exits 0 on pass).
/// </summary>
public static class FrontRoomsLanguageTests
{
    static int pass, fail;

    static void Check(bool ok, string what)
    {
        if (ok) { pass++; return; }
        fail++;
        Debug.LogError("[LanguageTests] FAIL " + what);
    }

    [MenuItem("FrontRooms/Tests/Language tables")]
    public static void Run() { Execute(false); }

    public static void RunBatch() { Execute(true); }

    static void Execute(bool batch)
    {
        pass = fail = 0;
        var rows = FrontRoomsLanguage.Parse("# a note\nOPEN\tA\tcontext\nTWO LINES\tB\\nC\nEMPTY\t\n\nNO TAB\n");
        Check(rows.Count == 2, "parse keeps two rows, got " + rows.Count);
        Check(rows.ContainsKey("OPEN") && rows["OPEN"] == "A", "the third cell is a note, not the translation");
        Check(rows.ContainsKey("TWO LINES") && rows["TWO LINES"] == "B\nC", "backslash n is a line break");
        Check(!rows.ContainsKey("EMPTY"), "an empty translation stays English");
        Check(FrontRoomsLanguage.Parse(null).Count == 0, "a missing table is an empty table");

        Check(FrontRoomsLanguage.All.Length == 10, "ten languages (D1, then Red's zh-Hant)");
        Check(FrontRoomsLanguage.All[0].Code == "en" && FrontRoomsLanguage.All[0].Finished, "English first and finished");
        var codes = new HashSet<string>();
        foreach (var language in FrontRoomsLanguage.All)
        {
            codes.Add(language.Code);
            Check(!string.IsNullOrEmpty(language.OwnName), "own name of " + language.Code);
            Check(language.Finished || FrontRoomsLanguage.FromDevice(CodeDevice(language.Code)) == FrontRoomsLanguage.All[0],
                "an unfinished language is never picked from the device: " + language.Code);
            if (language != FrontRoomsLanguage.All[0])
            {
                var asset = Resources.Load<TextAsset>("Language/" + language.Code);
                if (asset != null) Check(FrontRoomsLanguage.Parse(asset.text).Count > 0, "table has rows: " + language.Code);
            }
        }
        Check(codes.Count == FrontRoomsLanguage.All.Length, "codes are unique");
        Check(FrontRoomsLanguage.Find("xx") == null, "an unknown code is not a language");
        Check(FrontRoomsLanguage.FromDevice(SystemLanguage.Thai) == FrontRoomsLanguage.All[0], "a language outside the list means English");

        Check(FrontRoomsLanguage.IsEnglish, "the editor starts in English");
        Check(FrontRoomsLanguage.T("OPEN DOOR") == "OPEN DOOR", "English is untouched");
        Check(FrontRoomsLanguage.T(null) == null && FrontRoomsLanguage.T("") == "", "empty stays empty");
        Check(FrontRoomsLanguage.F("HOLD {0}", "E") == "HOLD E", "F fills {0}");

        Captions();
        Widths();
        Page();
        MetaFloor();
        Debug.Log("[LanguageTests] " + (fail == 0 ? "PASS " : "FAIL ") + pass + " passed, " + fail + " failed");
        if (batch) EditorApplication.Exit(fail == 0 ? 0 : 1);
    }

    // CJK's floor on small text (LANGUAGE_MARKS.md): Plex 13 reads 16 px on desktop, 11 reads 12 pt on a phone, 13 on a tablet;
    // a size already above it stays, a language without one is untouched, and back in English the Text is what it was.
    static void MetaFloor()
    {
        var go = new GameObject("LANGUAGE FLOOR TEST", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
        var plex = Resources.Load<Font>("Fonts/IBMPlexMono-Regular");
        var chinese = FrontRoomsLanguage.Find("zh-Hans");
        try
        {
            var small = go.AddComponent<UnityEngine.UI.Text>();
            small.font = plex;
            small.fontSize = 13;
            FrontRoomsLanguage.Style(small);
            var large = new GameObject("large", typeof(RectTransform)).AddComponent<UnityEngine.UI.Text>();
            large.transform.SetParent(go.transform, false);
            large.font = plex;
            large.fontSize = 20;
            FrontRoomsLanguage.Style(large);
            FrontRoomsLanguage.PreviewForTest(chinese);
            Check(small.fontSize == 16, "desktop floor 16, got " + small.fontSize);
            Check(large.fontSize == 20, "a larger size stays, got " + large.fontSize);
            FrontRoomsHandheld.Force(FrontRoomsHandheld.IPhonePro);
            FrontRoomsLanguage.PreviewForTest(chinese);
            Check(small.fontSize == 13, "phone floor 12 leaves 13, got " + small.fontSize);
            FrontRoomsHandheld.Force(FrontRoomsHandheld.IPad11);
            FrontRoomsLanguage.PreviewForTest(FrontRoomsLanguage.Find("ru"));
            Check(small.fontSize == 13 && small.font == plex, "Russian keeps Plex at its size");
            FrontRoomsLanguage.PreviewForTest(FrontRoomsLanguage.All[0]);
            Check(small.fontSize == 13 && small.font == plex && large.fontSize == 20, "English puts it back");
        }
        finally
        {
            FrontRoomsLanguage.PreviewForTest(FrontRoomsLanguage.All[0]);
            FrontRoomsHandheld.ClearForce();
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    // The LANGUAGE row (README section 7): last tab, first row, every language on one line.
    static void Page()
    {
        var tokens = new HashSet<string>();
        foreach (var language in FrontRoomsLanguage.All)
        {
            Check(!string.IsNullOrEmpty(language.Token), "token of " + language.Code);
            tokens.Add(language.Token);
            foreach (var other in FrontRoomsLanguage.All)
            {
                if (other == FrontRoomsLanguage.All[0]) continue;
                var table = Resources.Load<TextAsset>("Language/" + other.Code);
                if (table == null) continue;
                var rows = FrontRoomsLanguage.Parse(table.text);
                Check(!rows.ContainsKey(language.OwnName) && !rows.ContainsKey(language.Token), language.Code + " is never translated in " + other.Code);
            }
        }
        Check(tokens.Count == FrontRoomsLanguage.All.Length, "tokens are unique");
        Check(FrontRoomsLanguage.HasChoice, "the editor lists every language");
        var first = FrontRoomsSettingsMenu.Rows(FrontRoomsSettingsMenu.Tab.Assist)[0];
        Check(first.Key == FrontRoomsSettingsMenu.Key.Language && first.Kind == FrontRoomsSettingsMenu.Kind.Language, "LANGUAGE is ASSIST's first row");
        Check(first.ValueText == FrontRoomsLanguage.Current.OwnName, "the row names the current language");
        Check(!first.Changed, "the row is never a changed row");

        var parent = new GameObject("LANGUAGE STRIP TEST", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var strip = FrontRoomsLanguageStrip.Create(parent.transform, Resources.Load<Font>("Fonts/SourceSerif4-Variable"));
            Check(strip.Count == FrontRoomsLanguage.ListedLanguages.Count, "the strip lists every listed language");
            // Room on one line: the iPhone's card, the Duo's outer display, the desktop's row, with each name open in turn.
            var sizes = new[] { FrontRoomsLanguageStrip.Phone, FrontRoomsLanguageStrip.Phone, FrontRoomsLanguageStrip.Tablet, FrontRoomsLanguageStrip.Desktop };
            var room = new[] { 565f, 560f, 560f, 854f };
            for (var s = 0; s < sizes.Length; s++)
            {
                strip.Layout(sizes[s], room[s]);
                for (var i = 0; i < strip.Count; i++)
                {
                    strip.Snap();
                    strip.Highlight = i;
                    for (var f = 0; f < 200 && Mathf.Abs(strip.ContentWidth - room[s]) > room[s]; f++) strip.Animate();
                    OpenFully(strip, i);
                    Check(strip.ContentWidth <= room[s] + .5f, "name " + i + " fits " + room[s] + ": " + strip.ContentWidth);
                }
            }
            strip.Layout(FrontRoomsLanguageStrip.Phone, 565f);
            strip.Snap();
            Check(strip.Highlight == 0, "the current language is open");
            Check(strip.At(565f - strip.ContentWidth + 5f) == 0, "the first tag is at the start");
            Check(strip.At(564f) == strip.Count - 1, "the last tag is at the right edge");
            Check(strip.At(-1f) == -1, "outside the tags is no tag");
            strip.Step(1);
            Check(strip.Highlight == 1, "right opens the next tag");
            strip.Step(-1);
            strip.Step(-1);
            Check(strip.Highlight == 0, "left stops at the first tag");
            strip.Step(1);
            Check(FrontRoomsLanguage.Current == FrontRoomsLanguage.All[0], "moving picks nothing");
            strip.Reset();
            Check(strip.Highlight == 0, "leaving the row opens the current language again");
        }
        finally { UnityEngine.Object.DestroyImmediate(parent); }
    }

    // Every tag at rest with this one open: Animate steps by the frame, so it is walked there.
    static void OpenFully(FrontRoomsLanguageStrip strip, int index)
    {
        var type = typeof(FrontRoomsLanguageStrip);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var slots = (System.Collections.IList)type.GetField("slots", flags).GetValue(strip);
        for (var i = 0; i < slots.Count; i++)
        {
            var open = slots[i].GetType().GetField("Open");
            open.SetValue(slots[i], i == index ? 1f : 0f);
        }
        strip.Highlight = index;
        strip.Animate();
    }

    // A caption stays English in the list and is translated as it is drawn, with the direction inside the bracket.
    static void Captions()
    {
        var line = new FrontRoomsCaptions.Line { text = "[TV]", source = Vector3.left * 3f };
        var english = FrontRoomsCaptions.Format(line, Vector3.zero, Vector3.forward);
        Check(english == "[TV  \u00b7  LEFT]", "English caption unchanged: " + english);
        FrontRoomsLanguage.PreviewForTest(FrontRoomsLanguage.Find("de"));
        var german = FrontRoomsCaptions.Format(line, Vector3.zero, Vector3.forward);
        FrontRoomsLanguage.PreviewForTest(FrontRoomsLanguage.All[0]);
        Check(german == "[TV  \u00b7  LINKS]", "translated caption keeps its bracket: " + german);
        Check(line.text == "[TV]", "the stored line stays English");
    }

    // Settings rows on the phone's 418 pt list and the Duo outer display's 614 pt one: label, 22 pt gap, control. A row that
    // does not fit is listed, as the strings are still drafts; a language with no face yet is measured by estimate.
    static void Widths()
    {
        var wide = 0;
        FrontRoomsHandheld.Force(FrontRoomsHandheld.IPhoneDuoOuter);
        try
        {
            foreach (var language in FrontRoomsLanguage.All)
            {
                FrontRoomsLanguage.PreviewForTest(language);
                foreach (var tab in FrontRoomsSettingsMenu.Tabs)
                    foreach (var row in FrontRoomsSettingsMenu.Rows(tab))
                    {
                        if (row.Kind == FrontRoomsSettingsMenu.Kind.Header) continue;
                        var control = FrontRoomsSettingsMenu.ControlWidth(row, FrontRoomsSettingsMenu.Touch);
                        var need = FrontRoomsSettingsMenu.TextWidth(row.Label, FrontRoomsSettingsMenu.Touch.Label, false) + 22f + control;
                        if (need <= 418f) continue;
                        wide++;
                        Debug.Log("[LanguageWidth] " + language.Code + " " + row.Key + " needs " + Mathf.CeilToInt(need) + " pt (control " + Mathf.CeilToInt(control) + "), list 418" + (need > 614f ? ", outer 614" : ""));
                    }
                var tabs = 0f;
                foreach (var tab in FrontRoomsSettingsMenu.Tabs) tabs += FrontRoomsSettingsMenu.TextWidth(FrontRoomsSettingsMenu.TabName(tab), FrontRoomsSettingsMenu.Touch.Label, false) + 22f;
                Debug.Log("[LanguageWidth] " + language.Code + " tabs " + Mathf.CeilToInt(tabs) + " pt");
            }
        }
        finally
        {
            FrontRoomsLanguage.PreviewForTest(FrontRoomsLanguage.All[0]);
            FrontRoomsHandheld.ClearForce();
        }
        Debug.Log("[LanguageWidth] " + wide + " rows wider than the phone list");
    }

    static SystemLanguage CodeDevice(string code)
    {
        switch (code)
        {
            case "zh-Hans": return SystemLanguage.ChineseSimplified;
            case "zh-Hant": return SystemLanguage.ChineseTraditional;
            case "ru": return SystemLanguage.Russian;
            case "es": return SystemLanguage.Spanish;
            case "pt-BR": return SystemLanguage.Portuguese;
            case "de": return SystemLanguage.German;
            case "fr": return SystemLanguage.French;
            case "ja": return SystemLanguage.Japanese;
            case "ko": return SystemLanguage.Korean;
            default: return SystemLanguage.English;
        }
    }
}
