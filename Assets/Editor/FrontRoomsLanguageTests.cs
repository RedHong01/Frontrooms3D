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

        Check(FrontRoomsLanguage.All.Length == 9, "nine languages (D1)");
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
        Debug.Log("[LanguageTests] " + (fail == 0 ? "PASS " : "FAIL ") + pass + " passed, " + fail + " failed");
        if (batch) EditorApplication.Exit(fail == 0 ? 0 : 1);
    }

    // The LANGUAGE row and page (README section 7): last tab, first row; a rack that needs no scrolling on any screen.
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
        Check(first.Key == FrontRoomsSettingsMenu.Key.Language && first.Kind == FrontRoomsSettingsMenu.Kind.Page, "LANGUAGE is ASSIST's first row");
        Check(first.ValueText == FrontRoomsLanguage.Current.OwnName, "the row names the current language");
        Check(!first.Changed, "the row is never a changed row");

        int columns, lines;
        FrontRoomsLanguagePage.Grid(9, 710f, 165.6f, out columns, out lines);
        Check(columns == 3 && lines == 3, "phone, 9 names: 3 by 3, got " + columns + " by " + lines);
        FrontRoomsLanguagePage.Grid(14, 710f, 165.6f, out columns, out lines);
        Check(columns == 4 && lines == 4, "phone, 14 names: 4 by 4, got " + columns + " by " + lines);
        FrontRoomsLanguagePage.Grid(14, 630f, 165.6f, out columns, out lines);
        Check(columns == 3 && lines == 5, "Duo outer, 14 names: 3 by 5, got " + columns + " by " + lines);
        FrontRoomsLanguagePage.Grid(9, 1800f, 240f, out columns, out lines);
        Check(columns == 3 && lines == 3, "desktop, 9 names: no empty column, got " + columns + " by " + lines);
        FrontRoomsLanguagePage.Grid(2, 1800f, 240f, out columns, out lines);
        Check(columns == 2 && lines == 1, "desktop, 2 names: one line, got " + columns + " by " + lines);

        var parent = new GameObject("LANGUAGE PAGE TEST", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var page = FrontRoomsLanguagePage.Create(parent.transform, Resources.Load<Font>("Fonts/SourceSerif4-Variable"));
            Check(page.Count == FrontRoomsLanguage.ListedLanguages.Count, "the page lists every listed language");
            page.Layout(FrontRoomsLanguagePage.Phone, 710f, 311f);
            Check(page.Columns == 3 && page.PerColumn == 3, "the phone's page is 3 by 3");
            Check(page.At(new Vector2(20f, 20f)) == 0, "the first tile is at the top left");
            Check(page.At(new Vector2(700f, 300f)) == page.Count - 1, "the last tile is at the bottom right");
            Check(page.At(new Vector2(-1f, 20f)) == -1, "outside the rack is no tile");
            page.Open(true);
            Check(page.Focus == 0, "the bar opens on the current language");
            page.Navigate(1, 0);
            Check(page.Focus == 3, "right goes to the next column's same line, got " + page.Focus);
            page.Navigate(-1, 0);
            page.Navigate(-1, 0);
            Check(page.Focus == 0, "left stops at the first column");
            page.Navigate(0, -1);
            Check(page.Focus == page.Count - 1, "up from the first name wraps to the last");
            Check(FrontRoomsLanguage.Current == FrontRoomsLanguage.All[0], "moving the bar picks nothing");
        }
        finally { UnityEngine.Object.DestroyImmediate(parent); }
        TouchSheet();
    }

    // The touch sheet's hooks: END RUN leaves, BACK takes RESUME's corner and goes back to ASSIST, and only then closes the sheet.
    static void TouchSheet()
    {
        var root = new GameObject("LANGUAGE TOUCH TEST") { hideFlags = HideFlags.HideAndDontSave };
        root.SetActive(false);
        FrontRoomsHandheld.Force(FrontRoomsHandheld.IPhonePro);
        try
        {
            var controls = root.AddComponent<FrontRoomsTouchControls>();
            var type = typeof(FrontRoomsTouchControls);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetMethod("RefreshLayout", flags).Invoke(controls, null);
            controls.SetMenuState(FrontRoomsTouchControls.MenuState.Settings);
            Vector2 center, size;
            Check(controls.TryGetChipBase(FrontRoomsTouchControls.Button.Restart, out center, out size), "END RUN is on the sheet");
            type.GetMethod("OpenLanguagePage", flags).Invoke(controls, null);
            Check(controls.LanguagePageOpen, "the row opens the page");
            Check(!controls.TryGetChipBase(FrontRoomsTouchControls.Button.Restart, out center, out size), "END RUN is not on the page");
            Check(controls.TryGetChipBase(FrontRoomsTouchControls.Button.SettingsClose, out center, out size) && size.x == 57f, "BACK takes the corner");
            var close = type.GetMethod("CloseSheet", flags);
            var down = type.GetField("frameSettingsDown", flags);
            close.Invoke(controls, null);
            Check(!controls.LanguagePageOpen && !(bool)down.GetValue(controls), "BACK goes back to ASSIST, not out of the sheet");
            close.Invoke(controls, null);
            Check((bool)down.GetValue(controls), "from ASSIST the chip closes the sheet");
            type.GetMethod("OpenLanguagePage", flags).Invoke(controls, null);
            controls.SetMenuState(FrontRoomsTouchControls.MenuState.Playing);
            Check(!controls.LanguagePageOpen, "leaving the sheet closes the page");
            Check(controls.Layout.SettingsPageOverhang == 0f, "a phone keeps the card's width");
            FrontRoomsHandheld.Force(FrontRoomsHandheld.IPhoneDuoInner);
            type.GetMethod("RefreshLayout", flags).Invoke(controls, null);
            var L = controls.Layout;
            Check(L.SettingsPageOverhang > 24f, "the Duo's inner display runs past the card");
            var cardLeft = L.SettingsCardCenter.x - L.SettingsCardSize.x * .5f;
            Check(cardLeft - L.SettingsPageOverhang >= L.SafeLeft - .01f && cardLeft + L.SettingsCardSize.x + L.SettingsPageOverhang <= L.Frame.x - L.SafeRight + .01f, "it stays inside the safe area");
        }
        finally
        {
            FrontRoomsHandheld.ClearForce();
            UnityEngine.Object.DestroyImmediate(root);
        }
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
