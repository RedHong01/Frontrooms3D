using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>
/// What an office terminal says (narrative design writes it; the program's shape is 41 section 3.3). Loaded from data,
/// one JSON file per content set: Resources/Terminal/Content/terminal_&lt;contentKey&gt;.json, else the v1 set
/// Resources/Terminal/Content/interoffice.json. Replacing that file replaces every string; nothing is in code.
/// The v1 file holds the EXAM-COMPUTER mock's strings (W/exam_computer/pages.py, from existing lore) and 41's drafts.
/// Tokens, expanded when a panel is laid out (live clock and calendar, research/live_clocks via pages.py):
///   {CLOCK} h:MM AM (no seconds); {DATE_NOW} M/DD/YY and {DATE_BACK4} four months back; {MONTH_NOW} M/YY and
///   {MONTH_BACK4}; the next drill = the first Tuesday at least 8 days after the in-game date: {DRILL_WEEKDAY},
///   {DRILL_MONTH}, {DRILL_DAY}; {UNREAD}, {N}, {M}, {PROGRAM}. A ":U" suffix gives capitals ({DRILL_MONTH:U}).
/// The in-game date is today's month and day in 1990 (29 February reads 28).
/// </summary>
[Serializable]
public sealed class FrontRoomsTerminalContent
{
    public const string Folder = "Terminal/Content/";
    public const string DefaultSet = "interoffice";

    [Serializable] public sealed class Titles { public string main = "", doc = "", list = "", help = ""; }
    [Serializable] public sealed class MainPage
    {
        public string headline = "", second = "", secondNone = "";
        public string listItem = "", listMessage = "", helpItem = "", helpMessage = "";
    }
    [Serializable] public sealed class Field { public string label = "", value = ""; }
    [Serializable] public sealed class Memo
    {
        public string index = "", date = "", message = "";
        public Field[] head = new Field[0];
        public string[] body = new string[0];
        public string sign = "";
    }
    [Serializable] public sealed class Job { public string job = "", document = "", owner = "", pages = "", status = "", message = ""; }
    [Serializable] public sealed class ListPage { public string heading = ""; public string[] columns = new string[0]; public Job[] jobs = new Job[0]; }
    [Serializable] public sealed class HelpRow { public string action = "", text = ""; }
    [Serializable] public sealed class HelpPage { public string heading = ""; public HelpRow[] rows = new HelpRow[0]; }
    [Serializable] public sealed class Words
    {
        public string help = "Help", select = "Select", cancel = "Cancel", exit = "Exit", page = "Page";
        public string softSelect = "Enter", softCancel = "Cancel", softHelp = "Help";
        public string more = "More:", memoOf = "{N} of {M}";
    }
    [Serializable] public sealed class Refusals { public string notAuthorized = "", topOfData = "", bottomOfData = ""; }

    public string program = "";
    public Titles titles = new Titles();
    public MainPage main = new MainPage();
    public Memo[] memos = new Memo[0];
    public ListPage list = new ListPage();
    public HelpPage help = new HelpPage();
    public Words words = new Words();
    public Refusals refusals = new Refusals();

    /// <summary>Where this set came from (the resource name), for logs.</summary>
    [NonSerialized] public string Source = "";

    static readonly Dictionary<int, FrontRoomsTerminalContent> cache = new Dictionary<int, FrontRoomsTerminalContent>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        cache.Clear();
        ClockOverride = null;
    }

    /// <summary>The content set for a terminal's content key (the deal's key, 20_design section 3.4).</summary>
    public static FrontRoomsTerminalContent Load(int contentKey)
    {
        if (cache.TryGetValue(contentKey, out var hit) && hit != null) return hit;
        var name = "terminal_" + contentKey.ToString(CultureInfo.InvariantCulture);
        var asset = Resources.Load<TextAsset>(Folder + name);
        if (asset == null) { name = DefaultSet; asset = Resources.Load<TextAsset>(Folder + name); }
        var content = asset != null ? Parse(asset.text) : null;
        if (content == null)
        {
            Debug.LogWarning("[FrontRoomsTerminalContent] no content at Resources/" + Folder + name + ".json; the terminal shows an empty program.");
            content = new FrontRoomsTerminalContent();
        }
        content.Source = name;
        cache[contentKey] = content;
        return content;
    }

    public static FrontRoomsTerminalContent Parse(string json)
    {
        try
        {
            var c = JsonUtility.FromJson<FrontRoomsTerminalContent>(json);
            if (c == null) return null;
            if (c.titles == null) c.titles = new Titles();
            if (c.main == null) c.main = new MainPage();
            if (c.memos == null) c.memos = new Memo[0];
            if (c.list == null) c.list = new ListPage();
            if (c.list.jobs == null) c.list.jobs = new Job[0];
            if (c.list.columns == null) c.list.columns = new string[0];
            if (c.help == null) c.help = new HelpPage();
            if (c.help.rows == null) c.help.rows = new HelpRow[0];
            if (c.words == null) c.words = new Words();
            if (c.refusals == null) c.refusals = new Refusals();
            foreach (var m in c.memos)
            {
                if (m.head == null) m.head = new Field[0];
                if (m.body == null) m.body = new string[0];
            }
            return c;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[FrontRoomsTerminalContent] bad JSON: " + e.Message);
            return null;
        }
    }

    // ---------------------------------------------------------------- live clock and calendar

    /// <summary>Harness: a fixed wall clock (captures and tests). Null = the device's local time.</summary>
    public static DateTime? ClockOverride;
    public static DateTime Now => ClockOverride ?? DateTime.Now;

    static DateTime GameDate(DateTime real) => new DateTime(1990, real.Month, real.Month == 2 && real.Day == 29 ? 28 : real.Day);

    static DateTime MonthsBack(DateTime d, int n)
    {
        var m = d.Month - n;
        var y = d.Year;
        while (m < 1) { m += 12; y--; }
        return new DateTime(y, m, Math.Min(d.Day, DateTime.DaysInMonth(y, m)));
    }

    static DateTime NextDrill(DateTime d)
    {
        var t = d.AddDays(8);
        return t.AddDays(((int)DayOfWeek.Tuesday - (int)t.DayOfWeek + 7) % 7);
    }

    static string Short(DateTime d) => d.Month + "/" + d.Day.ToString("00", CultureInfo.InvariantCulture) + "/" + (d.Year % 100).ToString("00", CultureInfo.InvariantCulture);
    static string MonthYear(DateTime d) => d.Month + "/" + (d.Year % 100).ToString("00", CultureInfo.InvariantCulture);

    /// <summary>The header clock: h:MM AM (no leading zero, no seconds).</summary>
    public static string Clock(DateTime now)
    {
        var h = now.Hour % 12;
        if (h == 0) h = 12;
        return h + ":" + now.Minute.ToString("00", CultureInfo.InvariantCulture) + (now.Hour < 12 ? " AM" : " PM");
    }

    /// <summary>Expand the tokens in s (see the class summary). Unknown tokens are left as written.</summary>
    public string Expand(string s, int unread = 0, int n = 0, int m = 0)
    {
        if (string.IsNullOrEmpty(s) || s.IndexOf('{') < 0) return s ?? "";
        var now = Now;
        var game = GameDate(now);
        var sb = new StringBuilder(s.Length + 16);
        for (var i = 0; i < s.Length; i++)
        {
            var close = s[i] == '{' ? s.IndexOf('}', i + 1) : -1;
            if (close < 0) { sb.Append(s[i]); continue; }
            var token = s.Substring(i + 1, close - i - 1);
            var upper = token.EndsWith(":U", StringComparison.Ordinal);
            if (upper) token = token.Substring(0, token.Length - 2);
            string v;
            switch (token)
            {
                case "CLOCK": v = Clock(now); break;
                case "DATE_NOW": v = Short(game); break;
                case "DATE_BACK4": v = Short(MonthsBack(game, 4)); break;
                case "MONTH_NOW": v = MonthYear(game); break;
                case "MONTH_BACK4": v = MonthYear(MonthsBack(game, 4)); break;
                case "DRILL_WEEKDAY": v = NextDrill(game).ToString("dddd", CultureInfo.InvariantCulture); break;
                case "DRILL_MONTH": v = NextDrill(game).ToString("MMMM", CultureInfo.InvariantCulture); break;
                case "DRILL_DAY": v = NextDrill(game).Day.ToString(CultureInfo.InvariantCulture); break;
                case "UNREAD": v = unread.ToString(CultureInfo.InvariantCulture); break;
                case "N": v = n.ToString(CultureInfo.InvariantCulture); break;
                case "M": v = m.ToString(CultureInfo.InvariantCulture); break;
                case "PROGRAM": v = program ?? ""; break;
                default: v = null; break;
            }
            if (v == null) { sb.Append(s, i, close - i + 1); i = close; continue; }
            sb.Append(upper ? v.ToUpperInvariant() : v);
            i = close;
        }
        return sb.ToString();
    }
}
