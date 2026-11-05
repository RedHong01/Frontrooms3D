using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

// Scene 3, menu 5 MUSEUM · PLAN (交互设计, 2026-11-03; scene3/rooms/90_plan.md §2): the museum of 10_research §3 (hub 0 and
// rooms 1-4) drawn as a plan from FrontRoomsShowcaseTable, every station with its code and placard. ENTER tries a station now,
// in the tour that has it: the tour loads, and once the player is through the start door the guide jumps to the nearest
// example and shows the station's placard. In the tours every list row, tag and jump toast carries the code, and the toast
// adds the placard's two Chinese lines (what it is, what Red decides). The built museum (menu 5, .Museum.cs) opens this page
// over the game with P; there a row it has built is tagged HERE and ENTER walks to it.
// Every card on screen (the toast with a placard, the plan's room sign and placard, ROOM TYPES' card, a list row's 要你决定)
// is set in 平面视觉's placard type (research/showcase_scenes/20_placard_type.md, VL500; 交互设计 inc8a, 2026-11-04): its
// faces, colours and line order at 1 px a millimetre at 1080p, with no panel behind (Red 10-14, "我不希望有背后的这个黑色底"):
// the HUD's 1 px ink keyline carries every line, chip and bar.
public sealed partial class FrontRoomsInteractablesShowcase
{
    /// <summary>ENTER's jump waits this long outside the start rooms for an example, then says where else to find one.</summary>
    const float PendingSeconds = 180f;
    const string DecidePrefix = "要你决定：";

    // The plan page: the room shown on the right (0 the hub, 1-4) and the selected row (an index into the table's Stations).
    int planRoom;
    int planIndex = -1;
    // ENTER's jump into a tour: the kind to jump to (-1 none), the time the player first stood outside the start rooms, the row.
    int pendingKind = -1;
    float pendingOutAt = -1f;
    FrontRoomsShowcaseTable.Station pendingStation;
    // The placard's lines under the toast (Chinese, from the table), and its station (the code chip); Toast clears them.
    string toastWhat, toastDecide;
    FrontRoomsShowcaseTable.Station toastStation;
    // The game's own faces from Resources/Fonts (no OS font: 20 §1, "the iPad and iPhone review builds must draw the same face").
    Font zhFont, zhBold, monoMedium;
    bool faceWarned;
    GUIStyle zhLineStyle, zhSmallStyle, planCodeStyle;
    // The placard type (EnsureZhStyles): chip code, kind and its figures, Try steps, variant letters, labels, meta, live line,
    // what it is, 要你决定; a list row's 要你决定 and code; the room sign's number and foot; a toast title's code and figures.
    GUIStyle cardCodeStyle, cardKindStyle, cardFigureStyle, cardStepStyle, cardLetterStyle, cardLabelStyle, cardMetaStyle, cardLiveStyle;
    GUIStyle cardZhStyle, cardZhBoldStyle, rowZhBoldStyle, rowCodeStyle, signNumberStyle, signFootStyle, signArrowStyle, toastFigureStyle;

    // ---------- codes and placards (the tours)

    /// <summary>A kind's name with its museum code in front ("1.2  DOOR LITE"); the name alone when the table has no row.</summary>
    static string Label(int k)
    {
        if (k < 0 || k >= KindCount) return "";
        var row = FrontRoomsShowcaseTable.OfGuideKind(k);
        return row != null ? row.code + "  " + Names[k] : Names[k];
    }

    static string LabelOf(FrontRoomsShowcaseTable.Station st) => st == null ? "" : st.code + "  " + st.kind;

    static string Decide(string decide) => string.IsNullOrEmpty(decide) ? "" : DecidePrefix + decide;

    /// <summary>A station's two placard lines under the toast; the toast then stays 8 s.</summary>
    void ShowPlacard(FrontRoomsShowcaseTable.Station st)
    {
        if (st == null) return;
        toastWhat = st.what;
        toastDecide = st.decide;
        toastStation = st;
        toastUntil = Time.unscaledTime + 8f;
    }

    /// <summary>
    /// The toast: its title and line, centred. With a placard's lines (ShowPlacard, or the museum's room question) it is placard C
    /// on screen instead (DrawCard): the code chip and the kind, the toast's line in the Try slot, what it is, 要你决定; flush
    /// left in a block centred where the toast stands. Without them the title's leading code ("1.4", "R11 B") and its figures
    /// are set in Plex Mono Medium at the Bayon title's size (20 §1: Bayon's 1 reads as I).
    /// </summary>
    void DrawToast(float tx, float ty, float tw, float s)
    {
        var a = Mathf.Clamp01((toastUntil - Time.unscaledTime) / .4f);
        if (!string.IsNullOrEmpty(toastWhat) || !string.IsNullOrEmpty(toastDecide))
        {
            string code, kind;
            ToastTitle(out code, out kind);
            var y = ty + 12f * s;
            DrawCard(tx, y, tw, a, false, code, kind, null, toastLine, null, null, toastWhat, toastDecide, null, null, null, null);
            var x = tx + (tw - Mathf.Min(tw, cardWidth)) * .5f;
            DrawCard(x, y, tw, a, true, code, kind, null, toastLine, null, null, toastWhat, toastDecide, null, null, null, null);
            return;
        }
        string head, rest;
        SplitCode(toastTitle ?? "", out head, out rest);
        var baseline = ty + BayonAscent * toastTitleStyle.fontSize;
        var headW = string.IsNullOrEmpty(head) ? 0f : Width(head, toastFigureStyle) + Advance("  ", toastTitleStyle);
        var hx = tx + (tw - headW - KindSpan(0f, 0f, rest, toastTitleStyle, toastFigureStyle, Accent, a, false)) * .5f;
        if (headW > 0f) Span(hx, baseline, head, toastFigureStyle, PlexAscent, Accent, a);
        KindSpan(hx + headW, baseline, rest, toastTitleStyle, toastFigureStyle, Accent, a, true);
        var line = toastLine ?? "";
        var h = toastLineStyle.CalcHeight(new GUIContent(line), tw);
        Text(new Rect(tx, ty + 40f * s, tw, h), line, toastLineStyle, Paper, a);
    }

    /// <summary>The toast's title as placard C's first line: the station's code in the chip when the title starts with it
    /// ("1.1A  BODY DOOR  2 / 5": 1.1A, then BODY DOOR  2 / 5 in Bayon); otherwise the whole title in Bayon.</summary>
    void ToastTitle(out string code, out string kind)
    {
        var title = toastTitle ?? "";
        var c = toastStation != null ? toastStation.code : null;
        if (!string.IsNullOrEmpty(c) && title.Length > c.Length + 2 && string.CompareOrdinal(title, 0, c, 0, c.Length) == 0
            && title[c.Length] == ' ' && title[c.Length + 1] == ' ')
        {
            code = c;
            kind = title.Substring(c.Length + 2);
            return;
        }
        code = null;
        kind = title;
    }

    /// <summary>The list's line while ENTER's jump waits (under the meta line); returns the y below it.</summary>
    float DrawWaiting(float x, float y, float w, float s)
    {
        if (pendingKind < 0) return y;
        var line = "WAITING  ·  " + (pendingStation != null ? LabelOf(pendingStation) : Label(pendingKind))
            + (pendingOutAt < 0f ? "  ·  walk through the start door" : "  ·  walk on: the jump comes once one is near");
        var h = smallStyle.CalcHeight(new GUIContent(line), w);
        Text(new Rect(x, y, w, h), line, smallStyle, Accent);
        return y + h + 6f * s;
    }

    /// <summary>The marked list row's "what Red decides" line, under its HowTo (RowDecide); returns the y below it.</summary>
    float DrawRowDecide(int k, float x, float y, float w, float s)
    {
        var row = FrontRoomsShowcaseTable.OfGuideKind(k);
        if (row == null || string.IsNullOrEmpty(row.decide)) return y;
        return RowDecide(x, y - 2f * s, w, row.decide, .9f, 8f * s);
    }

    // ---------- ENTER's jump (the pending station)

    /// <summary>
    /// Every frame of a tour: once the player has stood 1 s outside the start rooms (they belong to the title's stream, as
    /// Jump says), not paused and not in a close-up, jump to the nearest example of the station's kind and show its placard.
    /// None after PendingSeconds outside: the kind's not-here line. Any manual jump, F2 or the run's end cancels it.
    /// </summary>
    void TryPending()
    {
        if (pendingKind < 0 || pendingKind >= KindCount || map == null || player == null || paused || FrontRooms3DGame.InspectingNow) return;
        if (map.InStartArea(map.CellOf(player.position))) return;
        var now = Time.unscaledTime;
        if (pendingOutAt < 0f)
        {
            pendingOutAt = now;
            return;
        }
        if (now - pendingOutAt < 1f) return;
        var k = pendingKind;
        if (found[k].Count > 0)
        {
            // Not in a busy frame (a shot, a climb, a pull, the seal): the jump waits for a calm one rather than being dropped.
            if (GateRefusal() != null) return;
            var st = pendingStation;
            ClearPending();
            // A refused jump keeps Jump's own reason on screen.
            if (Jump((Kind)k, 0) && st != null)
            {
                toastTitle = LabelOf(st);
                if (st.note != null) toastLine = st.note;
                ShowPlacard(st);
            }
            return;
        }
        if (now - pendingOutAt < PendingSeconds) return;
        Toast(Label(k), NotHereLine((Kind)k));
        ClearPending();
    }

    void ClearPending()
    {
        pendingKind = -1;
        pendingStation = null;
        pendingOutAt = -1f;
    }

    // ---------- the plan page's keys

    FrontRoomsShowcaseTable.Station SelectedStation
    {
        get
        {
            var all = FrontRoomsShowcaseTable.Stations;
            return all != null && planIndex >= 0 && planIndex < all.Length ? all[planIndex] : null;
        }
    }

    /// <summary>[ ] a room (0 the hub, 1-4), 1-7 a station (again on the same one: its next variant), 0 the hub, ENTER try it.</summary>
    void UpdatePlan(Keyboard keys)
    {
        if (keys == null) return;
        if (keys.leftBracketKey.wasPressedThisFrame) SelectRoom((planRoom + 4) % 5);
        else if (keys.rightBracketKey.wasPressedThisFrame) SelectRoom((planRoom + 1) % 5);
        else if (keys.digit0Key.wasPressedThisFrame || keys.numpad0Key.wasPressedThisFrame) SelectRoom(0);
        else if (keys.enterKey.wasPressedThisFrame || keys.numpadEnterKey.wasPressedThisFrame) TryStation(SelectedStation);
        else
        {
            var slot = SlotDown(keys);
            if (slot > 0) SelectSlot(slot);
        }
    }

    static int SlotDown(Keyboard keys)
    {
        if (keys.digit1Key.wasPressedThisFrame || keys.numpad1Key.wasPressedThisFrame) return 1;
        if (keys.digit2Key.wasPressedThisFrame || keys.numpad2Key.wasPressedThisFrame) return 2;
        if (keys.digit3Key.wasPressedThisFrame || keys.numpad3Key.wasPressedThisFrame) return 3;
        if (keys.digit4Key.wasPressedThisFrame || keys.numpad4Key.wasPressedThisFrame) return 4;
        if (keys.digit5Key.wasPressedThisFrame || keys.numpad5Key.wasPressedThisFrame) return 5;
        if (keys.digit6Key.wasPressedThisFrame || keys.numpad6Key.wasPressedThisFrame) return 6;
        if (keys.digit7Key.wasPressedThisFrame || keys.numpad7Key.wasPressedThisFrame) return 7;
        return 0;
    }

    /// <summary>A room on the right, its first row selected.</summary>
    void SelectRoom(int room)
    {
        planRoom = room;
        planIndex = -1;
        var all = FrontRoomsShowcaseTable.Stations;
        for (var i = 0; all != null && i < all.Length; i++)
            if (FrontRoomsShowcaseTable.RoomOf(all[i]) == room)
            {
                planIndex = i;
                return;
            }
    }

    /// <summary>The room's first row at a slot; pressed again on that slot, the slot's next row (1.1A -> 1.1B -> 1.1A).</summary>
    void SelectSlot(int slot)
    {
        var all = FrontRoomsShowcaseTable.Stations;
        if (all == null) return;
        var current = SelectedStation;
        if (current != null && FrontRoomsShowcaseTable.RoomOf(current) == planRoom && FrontRoomsShowcaseTable.SlotOf(current) == slot)
        {
            for (var n = 1; n < all.Length; n++)
            {
                var i = (planIndex + n) % all.Length;
                if (FrontRoomsShowcaseTable.RoomOf(all[i]) == planRoom && FrontRoomsShowcaseTable.SlotOf(all[i]) == slot)
                {
                    planIndex = i;
                    return;
                }
            }
            return;
        }
        var first = FrontRoomsShowcaseTable.At(planRoom, slot);
        if (first != null) planIndex = Array.IndexOf(all, first);
    }

    /// <summary>ENTER: the media bay, or the station's tour with its jump waiting for the start door; a row in no tour says why.</summary>
    void TryStation(FrontRoomsShowcaseTable.Station st)
    {
        if (st == null) return;
        if (st.tour == 1)
        {
            Choose(Tour.MediaBay);
            return;
        }
        var next = st.tour == 2 ? Tour.Shipped : st.tour == 3 ? Tour.Devices : st.tour == 4 ? Tour.KeysFloor : Tour.Menu;
        if (next == Tour.Menu)
        {
            Toast(LabelOf(st), st.note ?? "Not built yet.");
            return;
        }
        pendingStation = st;
        pendingKind = st.jumpKind >= 0 && st.jumpKind < KindCount ? st.jumpKind : -1;
        pendingOutAt = -1f;
        // Choose keeps the guide through the load (DontDestroyOnLoad), so the pending jump goes along.
        Choose(next);
    }

    // ---------- the faces

    /// <summary>
    /// The styles of the Chinese lines, the plan's codes and the placard type, rebuilt with the others when the scale changes
    /// (EnsureStyles, which has loaded Bayon and Plex Mono). Every face is the game's own in Resources/Fonts, never the OS's:
    /// Noto Sans SC Regular and Bold, IBM Plex Mono Medium. Their subset holds every character of the showcase's lines (872
    /// characters, 2026-11-04); a character it lacks falls back in the editor through the importer's font name (Noto Sans SC,
    /// installed on this Mac), then the OS's own list.
    /// </summary>
    void EnsureZhStyles(float scale)
    {
        if (zhFont == null) zhFont = Resources.Load<Font>("Fonts/NotoSansSC-Regular");
        if (zhBold == null) zhBold = Resources.Load<Font>("Fonts/NotoSansSC-Bold");
        if (monoMedium == null) monoMedium = Resources.Load<Font>("Fonts/IBMPlexMono-Medium");
        if ((zhFont == null || zhBold == null || monoMedium == null) && !faceWarned)
        {
            faceWarned = true;
            Debug.LogWarning("Interactables showcase: Resources/Fonts has no NotoSansSC-Regular, NotoSansSC-Bold or IBMPlexMono-Medium; a stand-in face draws those lines.");
        }
        var bold = zhBold != null ? zhBold : zhFont;
        var medium = monoMedium != null ? monoMedium : mono;
        zhLineStyle = Style(zhFont, 16f * scale, TextAnchor.UpperLeft);
        // 14 before: the CJK floor is 16 px at 1080p (research/desktop_ui/LANGUAGE_MARKS.md); .Museum.cs's room list reads it too.
        zhSmallStyle = Style(zhFont, RowZhSize * scale, TextAnchor.UpperLeft);
        planCodeStyle = Style(mono, 12f * scale, TextAnchor.MiddleCenter);
        cardCodeStyle = SpanStyle(medium, CodeSize * scale);
        cardKindStyle = SpanStyle(bayon, KindSize * scale);
        cardFigureStyle = SpanStyle(medium, KindSize * scale);
        cardStepStyle = SpanStyle(mono, StepSize * scale);
        cardLetterStyle = SpanStyle(medium, StepSize * scale);
        cardLabelStyle = SpanStyle(medium, MetaSize * scale);
        cardMetaStyle = SpanStyle(mono, MetaSize * scale);
        cardLiveStyle = SpanStyle(medium, MetaSize * scale);
        cardZhStyle = SpanStyle(zhFont, ZhSize * scale);
        cardZhBoldStyle = SpanStyle(bold, ZhSize * scale);
        rowZhBoldStyle = SpanStyle(bold, RowZhSize * scale);
        rowCodeStyle = SpanStyle(medium, RowCodeSize * scale);
        signNumberStyle = SpanStyle(medium, SignNumberSize * scale);
        signFootStyle = SpanStyle(medium, SignFootSize * scale);
        signArrowStyle = SpanStyle(medium, SignArrowSize * scale);
        // The toast title's Bayon size (toastTitleStyle, EnsureStyles: set just before this).
        toastFigureStyle = SpanStyle(medium, toastTitleStyle != null ? toastTitleStyle.fontSize : 30f * scale);
        if (zhBold == null) cardZhBoldStyle.fontStyle = rowZhBoldStyle.fontStyle = FontStyle.Bold;
        // Lines were broken for the old sizes.
        wraps.Clear();
    }

    // A line of one face: never wrapped by IMGUI (the lines are broken here, by the spec's rules).
    static GUIStyle SpanStyle(Font font, float size)
    {
        var style = Style(font, size, TextAnchor.UpperLeft);
        style.wordWrap = false;
        return style;
    }

    // The faces are shared Resources assets (nothing is made from the OS any more): only the references go.
    void DestroyPlanFont()
    {
        zhFont = zhBold = monoMedium = null;
        wraps.Clear();
    }

    // ---------- the plan page

    static Color PaperAt(float alpha) => new Color(Paper.r, Paper.g, Paper.b, alpha);

    static string BoardLine(int i)
    {
        var board = FrontRoomsShowcaseTable.Board;
        return board != null && i >= 0 && i < board.Length ? board[i] ?? "" : "";
    }

    static FrontRoomsShowcaseTable.Room RoomSign(int number)
    {
        var rooms = FrontRoomsShowcaseTable.Rooms;
        for (var i = 0; rooms != null && i < rooms.Length; i++)
            if (rooms[i] != null && rooms[i].number == number) return rooms[i];
        return null;
    }

    /// <summary>A museum cell (u 0-15 west to east, v 0-7 south to north) on screen, up = north = the spawn's forward.</summary>
    static Rect CellRect(float ox, float oy, float c, int u, int v) => new Rect(ox + u * c, oy + (7 - v) * c, c, c);

    /// <summary>A room's own cell (i from the visitor's left, j from the hub side) on screen (80 §4).</summary>
    static Rect LocalRect(int room, int i, int j, float ox, float oy, float c)
    {
        int u, v;
        if (room == 1)
        {
            u = 3 - j;
            v = i;
        }
        else if (room == 2 || room == 3)
        {
            u = (room == 2 ? 4 : 8) + i;
            v = 4 + j;
        }
        else
        {
            u = 12 + j;
            v = 3 - i;
        }
        return CellRect(ox, oy, c, u, v);
    }

    static Rect Union(Rect a, Rect b) => Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));

    // One pixel in from each side, so the ink behind draws the grid.
    static Rect Inset(Rect r) => new Rect(r.x + 1f, r.y + 1f, r.width - 2f, r.height - 2f);

    /// <summary>A station's place: .1-.3 up the right wall, .4 the alcove at the far side, .5-.7 down the left wall; on the hub,
    /// 0.1 the overview strip and 0.2 the locker frame on its south row, left of the spawn.</summary>
    static Rect SlotRect(int room, int slot, float ox, float oy, float c)
    {
        if (room == 0)
            return slot == 1 ? Union(CellRect(ox, oy, c, 4, 0), CellRect(ox, oy, c, 5, 0)) : CellRect(ox, oy, c, 6, 0);
        if (slot <= 3) return LocalRect(room, 3, slot - 1, ox, oy, c);
        if (slot == 4) return Union(LocalRect(room, 1, 3, ox, oy, c), LocalRect(room, 2, 3, ox, oy, c));
        return LocalRect(room, 0, 7 - slot, ox, oy, c);
    }

    /// <summary>The row a slot shows: the selected one when it stands there (a variant), else the slot's first.</summary>
    static FrontRoomsShowcaseTable.Station SlotRow(int room, int slot, FrontRoomsShowcaseTable.Station selected)
    {
        if (selected != null && FrontRoomsShowcaseTable.RoomOf(selected) == room && FrontRoomsShowcaseTable.SlotOf(selected) == slot) return selected;
        return FrontRoomsShowcaseTable.At(room, slot);
    }

    static void Centred(Rect r, string s, GUIStyle style, Color color, float alpha)
    {
        var size = style.CalcSize(new GUIContent(s));
        Text(new Rect(r.center.x - size.x * .5f, r.center.y - size.y * .5f, size.x, size.y), s, style, color, alpha);
    }

    /// <summary>A wrapped paragraph; returns the y below it plus the gap. Nothing for an empty string.</summary>
    static float Para(float x, float y, float w, string text, GUIStyle style, Color color, float alpha, float gap)
    {
        if (string.IsNullOrEmpty(text)) return y;
        var h = style.CalcHeight(new GUIContent(text), w);
        Text(new Rect(x, y, w, h), text, style, color, alpha);
        return y + h + gap;
    }

    string TagOf(FrontRoomsShowcaseTable.Station st)
    {
        if (st == null) return "";
        if (MuseumBuiltHere(st)) return "HERE · ENTER";
        if (st.tour == 1) return "MEDIA BAY · ENTER";
        if (st.tour >= 2 && st.tour <= 4) return "TOUR " + st.tour + " · ENTER";
        return st.guideKind >= 0 || st.rule == FrontRoomsShowcaseTable.Rule.None ? "NOT IN THE GAME YET" : "NOT BUILT";
    }

    /// <summary>
    /// Menu 5: the museum as a plan on the left (hub 0 and rooms 1-4 as 80 §4 lays them out, up = forward from the spawn;
    /// every station with its code, E and X on the hub walls), the selected room's sign, rows and the selected station's
    /// placard on the right, the board's lines round them. Repaint only, as all of OnGUI.
    /// </summary>
    void DrawPlan(float margin, float s)
    {
        // The bay rig keeps moving its camera behind the page (FrontRoomsScreenAdsDemoRig): the backdrop keeps the page readable.
        Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(Ink.r, Ink.g, Ink.b, .72f));
        var fullW = Mathf.Max(200f * s, Screen.width - 2f * margin);
        Text(new Rect(margin, margin, fullW, 40f * s), BoardLine(0), titleStyle, Paper);
        Text(new Rect(margin, margin + 40f * s, fullW, 24f * s), BoardLine(1), lineStyle, Paper, .85f);

        var c = Mathf.Floor(Mathf.Min(44f * s, Mathf.Min((Screen.width * .48f - margin) / 16f, (Screen.height - 2f * margin - 150f * s) / 8f)));
        c = Mathf.Max(8f, c);
        var ox = margin;
        var oy = margin + 92f * s;
        var selected = SelectedStation;
        var accent = new Color(Accent.r, Accent.g, Accent.b, .55f);

        // Hub 0 (u 4-11, v 0-3): its floor, the overview strip and the locker frame, the spawn, its number.
        for (var u = 4; u < 12; u++)
        for (var v = 0; v < 4; v++)
            Fill(Inset(CellRect(ox, oy, c, u, v)), PaperAt(.10f));
        for (var slot = 1; slot <= 2; slot++)
        {
            var r = SlotRect(0, slot, ox, oy, c);
            var row = SlotRow(0, slot, selected);
            Fill(Inset(r), row != null && row == selected ? accent : PaperAt(planRoom == 0 ? .24f : .16f));
            if (row != null) Text(r, row.code ?? "", planCodeStyle, Paper);
        }
        Text(Union(CellRect(ox, oy, c, 7, 0), CellRect(ox, oy, c, 8, 0)), "^ SPAWN", planCodeStyle, Paper, .85f);
        Centred(Union(CellRect(ox, oy, c, 7, 2), CellRect(ox, oy, c, 8, 1)), "0", menuNameStyle, Paper, .5f);

        // Rooms 1-4: lane, closed far corners, six bays and the alcove, the number in the lane.
        for (var room = 1; room <= 4; room++)
        {
            for (var i = 1; i <= 2; i++)
            for (var j = 0; j <= 2; j++)
                Fill(Inset(LocalRect(room, i, j, ox, oy, c)), PaperAt(.06f));
            Fill(Inset(LocalRect(room, 0, 3, ox, oy, c)), Ink);
            Fill(Inset(LocalRect(room, 3, 3, ox, oy, c)), Ink);
            for (var slot = 1; slot <= 7; slot++)
            {
                var r = SlotRect(room, slot, ox, oy, c);
                var row = SlotRow(room, slot, selected);
                var fill = row != null && row == selected ? accent : PaperAt(slot == 4 ? .22f : room == planRoom ? .24f : .16f);
                Fill(Inset(r), fill);
                if (row != null) Text(r, row.code ?? "", planCodeStyle, Paper);
            }
            Centred(Union(LocalRect(room, 1, 0, ox, oy, c), LocalRect(room, 2, 2, ox, oy, c)), room.ToString(), menuNameStyle, Paper, .5f);
        }

        // E (in, right of the middle facing the room) and X (out) on the hub walls (80 §4).
        DrawGate(3, 2, 4, 2, "E", ox, oy, c, s);
        DrawGate(3, 1, 4, 1, "X", ox, oy, c, s);
        DrawGate(6, 3, 6, 4, "E", ox, oy, c, s);
        DrawGate(5, 3, 5, 4, "X", ox, oy, c, s);
        DrawGate(10, 3, 10, 4, "E", ox, oy, c, s);
        DrawGate(9, 3, 9, 4, "X", ox, oy, c, s);
        DrawGate(11, 1, 12, 1, "E", ox, oy, c, s);
        DrawGate(11, 2, 12, 2, "X", ox, oy, c, s);

        // The right column: room sign B, the room's rows, placard C and card D of the selected row.
        var rx = ox + 16f * c + 40f * s;
        var rw = Mathf.Max(160f * s, Screen.width - margin - rx);
        var y = DrawRoomSign(planRoom, RoomSign(planRoom), rx, oy - 6f * s, rw) + 10f * s;
        var all = FrontRoomsShowcaseTable.Stations;
        for (var i = 0; all != null && i < all.Length; i++)
        {
            var st = all[i];
            if (st == null || FrontRoomsShowcaseTable.RoomOf(st) != planRoom) continue;
            var marked = i == planIndex;
            if (marked) Fill(new Rect(rx - 14f * s, y + 6f * s, 4f * s, 14f * s), Accent);
            // The code in Plex Mono Medium (20 §1: codes and figures; Bayon's 1 reads as I), on the Bayon name's baseline.
            Span(rx, y + BayonAscent * nameStyle.fontSize, st.code ?? "", rowCodeStyle, PlexAscent, marked ? Accent : Paper, 1f);
            Text(new Rect(rx + 64f * s, y, Mathf.Max(40f * s, rw - 264f * s), 26f * s), st.kind ?? "", nameStyle, Paper, marked ? 1f : .8f);
            Text(new Rect(rx + rw - 200f * s, y + 5f * s, 200f * s, 22f * s), TagOf(st), statusStyle, Paper, st.tour > 0 ? .9f : .5f);
            y += 28f * s;
        }
        y += 16f * s;
        if (selected != null && FrontRoomsShowcaseTable.RoomOf(selected) == planRoom) DrawPlacard(selected, rx, y, rw, s);

        // Under the plan: the toast (ENTER on a row in no tour), the rule sets and the keys at the bottom left.
        var fw = 16f * c;
        if (Time.unscaledTime < toastUntil && toastTitle != null) DrawToast(ox, oy + 8f * c + 20f * s, fw, s);
        var rules = BoardLine(3);
        var keys = BoardLine(2);
        var rulesH = smallStyle.CalcHeight(new GUIContent(rules), fw);
        var keysH = lineStyle.CalcHeight(new GUIContent(keys), fw);
        var fy = Mathf.Max(oy + 8f * c + 16f * s, Screen.height - margin - rulesH - keysH - 6f * s);
        Text(new Rect(ox, fy, fw, rulesH), rules, smallStyle, Paper, .7f);
        Text(new Rect(ox, fy + rulesH + 6f * s, fw, keysH), keys, lineStyle, Paper, .9f);
    }

    /// <summary>An opening on the hub wall between two museum cells: a short accent bar on the wall, its letter in the hub's cell.</summary>
    void DrawGate(int u1, int v1, int u2, int v2, string label, float ox, float oy, float c, float s)
    {
        var bar = 4f * s;
        Rect r;
        if (v1 == v2)
        {
            var x = ox + Mathf.Max(u1, u2) * c;
            r = new Rect(x - bar * .5f, oy + (7 - v1) * c + c * .2f, bar, c * .6f);
        }
        else
        {
            var y = oy + (8 - Mathf.Max(v1, v2)) * c;
            r = new Rect(ox + u1 * c + c * .2f, y - bar * .5f, c * .6f, bar);
        }
        Fill(r, Accent);
        var inHub = u1 >= 4 && u1 <= 11 && v1 >= 0 && v1 <= 3;
        Text(inHub ? CellRect(ox, oy, c, u1, v1) : CellRect(ox, oy, c, u2, v2), label, planCodeStyle, Accent);
    }

    /// <summary>Placard C (the code chip and kind, KEYS, TOUCH, what it is, 要你决定, the meta: staging · status · rule) and card D
    /// (the variants as letter chips, the note), in the placard type (DrawCard).</summary>
    void DrawPlacard(FrontRoomsShowcaseTable.Station st, float x, float y, float w, float s)
    {
        var tryLine = st.tryKeys ?? (st.guideKind >= 0 && st.guideKind < KindCount ? HowTo[st.guideKind] : "");
        var rule = st.rule == FrontRoomsShowcaseTable.Rule.Media ? "MEDIA BAY"
            : st.rule == FrontRoomsShowcaseTable.Rule.None ? "" : "RULE " + FrontRoomsShowcaseTable.RuleName(st.rule);
        var small = st.staging ?? "";
        if (!string.IsNullOrEmpty(st.status)) small += (small.Length > 0 ? "  ·  " : "") + st.status;
        if (rule.Length > 0) small += (small.Length > 0 ? "  ·  " : "") + rule;
        DrawCard(x, y + 12f * s, w, 1f, true, st.code, st.kind, TryLabel, tryLine, TouchLabel, st.tryTouch, st.what, st.decide, small, null,
            st.variants, st.note);
    }

    // ---------- the placard type on screen (平面视觉 research/showcase_scenes/20_placard_type.md, VL500; inc8a)

    // Placard C at 1 px a spec millimetre at 1080p (x styleScale, as every showcase style): the code chip in Plex Mono Medium,
    // cap 18 mm (26 px); the kind in Bayon, cap 26 mm (37 px); the Try steps in Plex Mono, x-height 9 mm (17 px); what it is in
    // Noto Sans SC, em 18.5 mm, pitch 24 mm (18 px); 要你决定 and the question in Noto Sans SC Bold. The labels (x-height 5.2 mm)
    // and the meta (4.6 mm) would be 10 and 9 px on screen: they take the HUD's 13 px meta (FrontRoomsPromptBar's TextSize). CJK
    // is never under 16 px at 1080p (research/desktop_ui/LANGUAGE_MARKS.md): a list row's 要你决定 stands on that floor. Room
    // sign B's number keeps the spec's 120 : 44 to the name (cap 70 px), and its foot the same scale (x-height 13 and 20 mm: 15
    // and 23 px). The spec's foot is pinned on a panel; here it follows the kinds.
    const float CodeSize = 26f, KindSize = 37f, StepSize = 17f, MetaSize = 13f, ZhSize = 18f, RowZhSize = 16f;
    const float RowCodeSize = 21f, SignNumberSize = 101f, SignFootSize = 15f, SignArrowSize = 23f, SignFootLead = 36f;
    const float StepPitch = 21f, ZhPitch = 24f, MetaPitch = 17f, RowZhPitch = 21f;
    // A face's baseline under the top of its line box (Unity's FaceAscender, the importers' ascentCalculationMode 1: hhea
    // ascender / units per em), its descent, and the cap heights (ink of H), measured from the files in Resources/Fonts.
    const float BayonAscent = 1.2207f, PlexAscent = 1.025f, NotoAscent = 1.16f, NotoDescent = .288f, BayonCap = .696f, PlexCap = .698f;
    // Placard C's measure: its 600 mm panel less two 30 mm margins, "same size at every station" (20 §2 C), so every card
    // breaks its lines where the mock does, whatever room its caller has (the toast 820, the plan 1032, ROOM TYPES 600).
    // Room sign B on the plan stands on the same measure.
    const float CardMeasure = 540f;
    // The label column of 20's mock (the steps start 62 mm in) and the labels of 20 §2 C line 2 (KEYS and TOUCH).
    const float LabelColumn = 62f;
    const string DecideLabel = "要你决定", TryLabel = "KEYS", TouchLabel = "TOUCH", VariantsLabel = "VARIANTS";
    static readonly string[] VariantSeparator = { "  ·  " };

    // The blocks of a card, for the step between them (Lead).
    const int CardTitle = 0, CardTry = 1, CardWhat = 2, CardDecide = 3, CardMeta = 4, CardLive = 5, CardChips = 6, CardNote = 7;

    // Broken lines by text, style and width: the table's lines are broken once per scale (cleared with the styles).
    const int WrapLatin = 0, WrapBody = 1, WrapQuestion = 2;
    readonly Dictionary<WrapKey, string[]> wraps = new Dictionary<WrapKey, string[]>();
    static readonly GUIContent measured = new GUIContent();
    // The card being laid out: its widest line (DrawToast centres the block by it).
    float cardWidth;

    struct WrapKey : IEquatable<WrapKey>
    {
        public string text;
        public GUIStyle style;
        public int width, mode;

        public bool Equals(WrapKey o) =>
            width == o.width && mode == o.mode && ReferenceEquals(style, o.style) && string.Equals(text, o.text, StringComparison.Ordinal);

        public override bool Equals(object o) => o is WrapKey k && Equals(k);

        public override int GetHashCode()
        {
            unchecked
            {
                var h = text != null ? StringComparer.Ordinal.GetHashCode(text) : 0;
                h = h * 31 + width;
                h = h * 31 + mode;
                return h * 31 + (style != null ? RuntimeHelpers.GetHashCode(style) : 0);
            }
        }
    }

    /// <summary>
    /// Placard C on screen (20 §2 C), flush left from x and y (the kind's cap top), on C's measure (w, at most CardMeasure), in its
    /// line order: the code chip and the kind; the Try rows, a grey Plex Mono Medium label (KEYS and TOUCH, 20 §2 C line 2) beside
    /// the steps in Plex Mono; what it is (Noto Sans SC); 要你决定 (Bold, accent) and the question (Bold, paper) with the accent bar;
    /// the meta in grey and the live line in accent at the foot; then card D's variants as letter chips (20 §2 D) and the note. A
    /// null or empty line is left out. With draw false it only measures (cardWidth, the widest line). Returns the y under the
    /// last line.
    /// </summary>
    float DrawCard(float x, float y, float w, float alpha, bool draw, string code, string kind, string tryLabel, string tryLine,
        string touchLabel, string touchLine, string what, string decide, string meta, string live, string variants, string note)
    {
        var s = styleScale;
        w = Mathf.Min(w, CardMeasure * s);
        cardWidth = 0f;
        var baseline = y + BayonCap * cardKindStyle.fontSize;
        var cx = x;
        if (!string.IsNullOrEmpty(code)) cx += Chip(x, baseline, code, cardCodeStyle, 5f * s, alpha, draw) + 10f * s;
        cx += KindSpan(cx, baseline, kind, cardKindStyle, cardFigureStyle, Paper, alpha, draw);
        Widest(cx - x);
        var hasVariants = !string.IsNullOrEmpty(variants);
        // The steps' column from KEYS and TOUCH alone, so it stands 62 mm in on every card (the mock); VARIANTS, wider, has its own.
        var labels = Mathf.Max(LabelWidth(tryLine, tryLabel), LabelWidth(touchLine, touchLabel));
        var column = labels > 0f ? Mathf.Max(LabelColumn * s, labels + 14f * s) : 0f;
        var prev = CardTitle;
        if (!string.IsNullOrEmpty(tryLine))
        {
            baseline = CardRows(x, baseline + Lead(prev, CardTry) * s, w, tryLabel, tryLine, column, alpha, draw);
            prev = CardTry;
        }
        if (!string.IsNullOrEmpty(touchLine))
        {
            baseline = CardRows(x, baseline + Lead(prev, CardTry) * s, w, touchLabel, touchLine, column, alpha, draw);
            prev = CardTry;
        }
        if (!string.IsNullOrEmpty(what))
        {
            baseline = CardZh(x, baseline + Lead(prev, CardWhat) * s, w, what, alpha, draw);
            prev = CardWhat;
        }
        if (!string.IsNullOrEmpty(decide))
        {
            baseline = DecideLines(x, baseline + Lead(prev, CardDecide) * s, w, decide, cardZhBoldStyle, ZhPitch * s, alpha, true, draw);
            prev = CardDecide;
        }
        if (!string.IsNullOrEmpty(meta))
        {
            baseline = CardSmall(x, baseline + Lead(prev, CardMeta) * s, w, meta, cardMetaStyle, Meta, alpha, draw);
            prev = CardMeta;
        }
        if (!string.IsNullOrEmpty(live))
        {
            baseline = CardSmall(x, baseline + Lead(prev, CardLive) * s, w, live, cardLiveStyle, Accent, alpha, draw);
            prev = CardLive;
        }
        if (hasVariants)
        {
            var chipColumn = Mathf.Max(LabelColumn * s, Width(VariantsLabel, cardLabelStyle) + 14f * s);
            baseline = CardVariants(x, baseline + Lead(prev, CardChips) * s, w, variants, chipColumn, alpha, draw);
            prev = CardChips;
        }
        if (!string.IsNullOrEmpty(note)) baseline = CardSmall(x, baseline + Lead(prev, CardNote) * s, w, note, cardMetaStyle, Paper, alpha, draw);
        return baseline + 8f * s;
    }

    // The baseline-to-baseline step into a block, from 20's mock at 1 px a mm: the title to the first block 47, Try to Try 17
    // (21 here: a wrapped step needs the room for its keyline), Try to what 36, what to 要你决定 34; the foot 30 under the rest.
    static float Lead(int prev, int next)
    {
        if (prev == CardTitle) return 47f;
        switch (next)
        {
            case CardTry: return StepPitch;
            case CardWhat: return 36f;
            case CardDecide: return prev == CardWhat ? 34f : 36f;
            case CardMeta: return 30f;
            case CardLive: return prev == CardMeta ? MetaPitch : 30f;
            case CardChips: return 34f;
            default: return 26f;
        }
    }

    float LabelWidth(string line, string label) =>
        string.IsNullOrEmpty(line) || string.IsNullOrEmpty(label) ? 0f : Width(label, cardLabelStyle);

    void Widest(float width)
    {
        if (width > cardWidth) cardWidth = width;
    }

    /// <summary>A Try row: its label in the label column on the first baseline, the steps broken at spaces beside it. Returns the
    /// last baseline.</summary>
    float CardRows(float x, float baseline, float w, string label, string text, float column, float alpha, bool draw)
    {
        var s = styleScale;
        if (draw && !string.IsNullOrEmpty(label)) Span(x, baseline, label, cardLabelStyle, PlexAscent, Meta, alpha);
        var lines = Wrapped(text, cardStepStyle, Mathf.Max(40f * s, w - column), WrapLatin);
        for (var i = 0; i < lines.Length; i++)
            Widest(column + Span(x + column, baseline + i * StepPitch * s, lines[i], cardStepStyle, PlexAscent, Paper, alpha, draw));
        return baseline + Mathf.Max(0, lines.Length - 1) * StepPitch * s;
    }

    /// <summary>What it is: Noto Sans SC Regular, paper, broken by the spec's Chinese rule. Returns the last baseline.</summary>
    float CardZh(float x, float baseline, float w, string text, float alpha, bool draw)
    {
        var s = styleScale;
        var lines = Wrapped(text, cardZhStyle, w, WrapBody);
        for (var i = 0; i < lines.Length; i++)
            Widest(Span(x, baseline + i * ZhPitch * s, lines[i], cardZhStyle, NotoAscent, Paper, alpha, draw));
        return baseline + Mathf.Max(0, lines.Length - 1) * ZhPitch * s;
    }

    /// <summary>The meta, the live line or the note: one Plex Mono face and colour, broken at spaces. Returns the last baseline.</summary>
    float CardSmall(float x, float baseline, float w, string text, GUIStyle style, Color color, float alpha, bool draw)
    {
        var s = styleScale;
        var lines = Wrapped(text, style, w, WrapLatin);
        for (var i = 0; i < lines.Length; i++)
            Widest(Span(x, baseline + i * MetaPitch * s, lines[i], style, PlexAscent, color, alpha, draw));
        return baseline + Mathf.Max(0, lines.Length - 1) * MetaPitch * s;
    }

    /// <summary>
    /// 要你决定 in Noto Sans SC Bold, accent, and the question in Bold, paper, hanging one em after it (20 §2 C line 4; a question
    /// breaks after a phrase mark); with bar, the accent bar from the first line's em top to under the last. Returns the last
    /// baseline.
    /// </summary>
    float DecideLines(float x, float baseline, float w, string question, GUIStyle style, float pitch, float alpha, bool bar, bool draw)
    {
        var s = styleScale;
        var qx = x + Width(DecideLabel, style) + style.fontSize;
        var lines = Wrapped(question, style, Mathf.Max(40f * s, x + w - qx), WrapQuestion);
        Span(x, baseline, DecideLabel, style, NotoAscent, Accent, alpha, draw);
        for (var i = 0; i < lines.Length; i++)
            Widest(qx - x + Span(qx, baseline + i * pitch, lines[i], style, NotoAscent, Paper, alpha, draw));
        var last = baseline + Mathf.Max(0, lines.Length - 1) * pitch;
        if (bar && draw) DecideBar(x, baseline - style.fontSize, last + 5f * s, alpha);
        return last;
    }

    /// <summary>A list row's 要你决定 (the marked row): label and question in Noto Sans SC Bold at the CJK floor, hanging, no bar
    /// (the row's own marker stands beside it). y is the top of its line box, as Para's; returns the y below it plus gap.</summary>
    float RowDecide(float x, float y, float w, string question, float alpha, float gap)
    {
        if (string.IsNullOrEmpty(question)) return y;
        var size = rowZhBoldStyle.fontSize;
        var last = DecideLines(x, y + NotoAscent * size, w, question, rowZhBoldStyle, RowZhPitch * styleScale, alpha, false, true);
        return last + NotoDescent * size + gap;
    }

    /// <summary>Card D's variants (20 §2 D): "A LOBBY  ·  B OFFICE" as letter chips (Plex Mono Medium) and names (Plex Mono) after
    /// the grey VARIANTS label, wrapping in the steps' column; a list in any other form is one Plex Mono row. Returns the last
    /// baseline.</summary>
    float CardVariants(float x, float baseline, float w, string variants, float column, float alpha, bool draw)
    {
        var s = styleScale;
        if (draw) Span(x, baseline, VariantsLabel, cardLabelStyle, PlexAscent, Meta, alpha);
        var parts = variants.Split(VariantSeparator, StringSplitOptions.RemoveEmptyEntries);
        var chips = parts.Length > 0;
        foreach (var p in parts)
            if (p.Length < 3 || p[1] != ' ' || p[0] < 'A' || p[0] > 'Z') chips = false;
        if (!chips) return CardRows(x, baseline, w, null, variants, column, alpha, draw);
        var cx = x + column;
        foreach (var p in parts)
        {
            var letter = p.Substring(0, 1);
            var name = p.Substring(2);
            var cw = Chip(cx, baseline, letter, cardLetterStyle, 4f * s, alpha, false);
            var nw = Width(name, cardStepStyle);
            if (cx > x + column && cx + cw + 8f * s + nw > x + w)
            {
                cx = x + column;
                baseline += 30f * s;
            }
            Chip(cx, baseline, letter, cardLetterStyle, 4f * s, alpha, draw);
            Span(cx + cw + 8f * s, baseline, name, cardStepStyle, PlexAscent, Paper, alpha, draw);
            cx += cw + 8f * s + nw;
            Widest(cx - x);
            cx += 22f * s;
        }
        return baseline;
    }

    /// <summary>
    /// Room sign B on screen (20 §2 B) from x and y (the number's cap top), on the card's measure: the number in Plex Mono Medium,
    /// cap 120 : 44 to the name; the name in Bayon at the card's kind size beside the number's top; the kinds the room holds (the
    /// table's holds in capitals joined by " · ", two lines at most, Plex Mono, paper) beside the number from its baseline on, in
    /// the place B gives the Chinese name (the table carries none); B's foot in Plex Mono Medium, accent: the room's stations and
    /// ABOUT 5 MIN (10_research §3: about 5 minutes a room), and the arrow to .1 at the measure's right, as the room's inside
    /// world sign shows it ("← 1.1", room 3's "→ 3.1"; .Museum.cs MuseumSignArrow). The hub has board A, not B: no foot. Then
    /// the room's 要你决定 with its bar (the table's question; B itself has none). Returns the y below.
    /// </summary>
    float DrawRoomSign(int number, FrontRoomsShowcaseTable.Room sign, float x, float y, float w)
    {
        var s = styleScale;
        w = Mathf.Min(w, CardMeasure * s);
        var numberBase = y + PlexCap * signNumberStyle.fontSize;
        var nx = x + Span(x, numberBase, number.ToString(), signNumberStyle, PlexAscent, Paper, 1f) + 16f * s;
        var last = numberBase;
        if (sign == null) return last + 10f * s;
        KindSpan(nx, y + 8f * s + BayonCap * cardKindStyle.fontSize, sign.name, cardKindStyle, cardFigureStyle, Paper, 1f, true);
        var kinds = Wrapped(SignKinds(sign.holds), cardMetaStyle, Mathf.Max(40f * s, x + w - nx), WrapLatin);
        for (var i = 0; i < kinds.Length; i++) Span(nx, numberBase + i * MetaPitch * s, kinds[i], cardMetaStyle, PlexAscent, Paper, 1f);
        last += Mathf.Max(0, kinds.Length - 1) * MetaPitch * s;
        if (number > 0)
        {
            last += SignFootLead * s;
            var footW = Span(x, last, SignStations(number) + " STATIONS · ABOUT 5 MIN", signFootStyle, PlexAscent, Accent, 1f);
            var arrow = MuseumSignArrow(number);
            Span(Mathf.Max(x + footW + 24f * s, x + w - Width(arrow, signArrowStyle)), last, arrow, signArrowStyle, PlexAscent, Accent, 1f);
        }
        if (!string.IsNullOrEmpty(sign.decide)) last = DecideLines(x, last + 40f * s, w, sign.decide, cardZhBoldStyle, ZhPitch * s, 1f, true, true);
        return last + 10f * s;
    }

    // B's kinds from the table's holds: in capitals, its ", " and "  ·  " joins set as " · " (20 §2 B's mock).
    static string SignKinds(string holds) =>
        string.IsNullOrEmpty(holds) ? "" : holds.ToUpperInvariant().Replace("  ·  ", " · ").Replace(", ", " · ");

    // B's station count: the room's slots in the table (a slot's variants, 1.1A and 1.1B, are one station).
    static int SignStations(int room)
    {
        var all = FrontRoomsShowcaseTable.Stations;
        var slots = 0;
        for (var i = 0; all != null && i < all.Length; i++)
        {
            var slot = FrontRoomsShowcaseTable.SlotOf(all[i]);
            if (FrontRoomsShowcaseTable.RoomOf(all[i]) == room && slot > 0 && slot < 31) slots |= 1 << slot;
        }
        var n = 0;
        for (; slots != 0; slots &= slots - 1) n++;
        return n;
    }

    /// <summary>
    /// A code chip (20 §1): the code in Plex Mono Medium inside a paper keyline 1.2 mm wide, radius 2.5 mm, padding pad (5 mm on C)
    /// — the prompt chips' keycap grammar, so a code reads as "say this" — with the HUD's ink ring 1 px outside and inside the paper
    /// line where a panel would be. Returns its width.
    /// </summary>
    float Chip(float x, float baseline, string code, GUIStyle style, float pad, float alpha, bool draw)
    {
        var s = styleScale;
        var cap = PlexCap * style.fontSize;
        var w = Mathf.Round(Width(code, style) + 2f * pad);
        if (!draw) return w;
        var r = new Rect(Mathf.Round(x), Mathf.Round(baseline - cap - pad), w, Mathf.Round(cap + 2f * pad));
        var line = Mathf.Max(1f, Mathf.Round(1.2f * s));
        var radius = 2.5f * s;
        Ring(new Rect(r.x - 1f, r.y - 1f, r.width + 2f, r.height + 2f), line + 2f, radius + 1f, new Color(Ink.r, Ink.g, Ink.b, FrontRoomsHudStyle.KeylineAlpha * alpha));
        Ring(r, line, radius, new Color(Paper.r, Paper.g, Paper.b, alpha));
        Span(r.x + pad, baseline, code, style, PlexAscent, Paper, alpha);
        return w;
    }

    // A rounded keyline: IMGUI's bordered DrawTexture (only the border is drawn).
    static void Ring(Rect r, float width, float radius, Color c) => GUI.DrawTexture(r, white, ScaleMode.StretchToFill, true, 0f, c, width, radius);

    // The 要你决定 bar (20 §2 C line 4): 4 mm of accent left of the column, with the HUD's ink ring. It stands where the lists'
    // marker bar stands (x - 14, 4 wide; the spec's mock has it 2 mm nearer), so the bars of one column line up on the plan.
    void DecideBar(float x, float top, float bottom, float alpha)
    {
        var s = styleScale;
        var r = new Rect(Mathf.Round(x - 14f * s), Mathf.Round(top), Mathf.Max(1f, Mathf.Round(4f * s)), Mathf.Round(bottom - top));
        Fill(new Rect(r.x - 1f, r.y - 1f, r.width + 2f, r.height + 2f), new Color(Ink.r, Ink.g, Ink.b, FrontRoomsHudStyle.KeylineAlpha * alpha));
        Fill(r, new Color(Accent.r, Accent.g, Accent.b, alpha));
    }

    /// <summary>A kind or a room's name in Bayon with its figures in Plex Mono Medium at the same size (20 §1: Bayon has no
    /// figures, its 1 reads as I; the caps match, .696 and .698 em). Returns the width.</summary>
    static float KindSpan(float x, float baseline, string text, GUIStyle bayonStyle, GUIStyle figureStyle, Color color, float alpha, bool draw)
    {
        if (string.IsNullOrEmpty(text)) return 0f;
        var cx = x;
        var start = 0;
        while (start < text.Length)
        {
            var figure = IsFigure(text[start]);
            var end = start + 1;
            while (end < text.Length && IsFigure(text[end]) == figure) end++;
            var part = text.Substring(start, end - start);
            cx += figure ? Span(cx, baseline, part, figureStyle, PlexAscent, color, alpha, draw) : Span(cx, baseline, part, bayonStyle, BayonAscent, color, alpha, draw);
            start = end;
        }
        return cx - x;
    }

    static bool IsFigure(char c) => c >= '0' && c <= '9';

    /// <summary>One line of one face with its baseline at <paramref name="baseline"/>, the HUD's keyline round it (Text). Returns
    /// its advance; with draw false it only measures.</summary>
    static float Span(float x, float baseline, string text, GUIStyle style, float ascent, Color color, float alpha, bool draw = true)
    {
        if (string.IsNullOrEmpty(text) || style == null) return 0f;
        var w = Advance(text, style);
        if (draw) Text(new Rect(Mathf.Round(x), Mathf.Round(baseline - ascent * style.fontSize), w + 4f, style.fontSize * 2f), text, style, color, alpha);
        return w;
    }

    static float Width(string text, GUIStyle style)
    {
        if (string.IsNullOrEmpty(text) || style == null) return 0f;
        measured.text = text;
        return style.CalcSize(measured).x;
    }

    // A span's advance with its trailing spaces (a figure follows them on the line).
    static float Advance(string text, GUIStyle style) =>
        text[text.Length - 1] == ' ' ? Width(text + ".", style) - Width(".", style) : Width(text, style);

    string[] Wrapped(string text, GUIStyle style, float width, int mode)
    {
        if (string.IsNullOrEmpty(text) || style == null) return Array.Empty<string>();
        var key = new WrapKey { text = text, style = style, width = Mathf.RoundToInt(width), mode = mode };
        string[] lines;
        if (wraps.TryGetValue(key, out lines)) return lines;
        // The live lines change with every metre walked.
        if (wraps.Count >= 512) wraps.Clear();
        lines = mode == WrapLatin ? WrapWords(text, style, width) : WrapZh(text, style, width, mode == WrapQuestion);
        wraps[key] = lines;
        return lines;
    }

    /// <summary>Latin at spaces (the steps, the meta, the note): a line takes words while they fit; a word wider than the line
    /// stands alone on it; a separator dot never starts a line (it hangs on the line before); a word of three characters or
    /// fewer never stands alone on the last line.</summary>
    static string[] WrapWords(string text, GUIStyle style, float width)
    {
        var lines = new List<string>();
        var line = "";
        var words = text.Split(' ');
        for (var i = 0; i < words.Length; i++)
        {
            var word = words[i];
            var open = line.Trim().Length == 0;
            if (open && word == "·" && lines.Count > 0)
            {
                lines[lines.Count - 1] += " ·";
                line = "";
                continue;
            }
            var next = line.Length == 0 ? word : line + " " + word;
            if (open || Width(next.TrimEnd(' '), style) <= width)
            {
                line = next;
                continue;
            }
            if (word == "·")
            {
                lines.Add(line.TrimEnd(' ') + " ·");
                line = "";
                continue;
            }
            lines.Add(line.TrimEnd(' '));
            line = word;
        }
        if (line.Trim().Length > 0) lines.Add(line.TrimEnd(' '));
        // No short word alone on the last line ("4.", "on."): the word before it comes down with it, when both fit.
        var n = lines.Count;
        if (n >= 2 && lines[n - 1].Length <= 3)
        {
            var prev = lines[n - 2];
            var cut = prev.LastIndexOf(' ');
            var moved = cut > 0 ? prev.Substring(cut + 1) : "";
            if (moved.Length > 0 && moved != "·" && prev.Substring(0, cut).Trim().Length > 0 && Width(moved + " " + lines[n - 1], style) <= width)
            {
                lines[n - 2] = prev.Substring(0, cut).TrimEnd(' ');
                lines[n - 1] = moved + " " + lines[n - 1];
            }
        }
        return lines.ToArray();
    }

    // The spec's Chinese breaks (20 §1 Setting; W/showcase_placards/placards.py wrap_cjk and wrap_cjk_n): a line may end after
    // these phrase marks (a question's also after ？ and ！); a closing mark never starts a line, an opening one never ends it.
    const string PhraseMarks = "。；，：、", QuestionMarks = "。；，：、？！";
    const string NoStartMarks = "，。；：、？！）」』】》”’,.;:!?)%", NoEndMarks = "（「『【《“‘(";

    /// <summary>
    /// Chinese lines by the spec's rule. Two lines (wrap_cjk): after the last phrase mark (a question's also ？ and ！) that leaves
    /// both halves inside, else balanced by length. More: the body fills each line; a question breaks after its last phrase mark
    /// that fills 60 % of the line (wrap_cjk_n). Always: a Latin word or number is never cut, a full-width mark never starts a
    /// line, no lone last character.
    /// </summary>
    static string[] WrapZh(string text, GUIStyle style, float width, bool question)
    {
        var lines = new List<string>();
        var s = text.Trim();
        for (var guard = 0; s.Length > 0 && guard < 64; guard++)
        {
            var full = Width(s, style);
            if (full <= width)
            {
                lines.Add(s);
                break;
            }
            // The longest head that fits, one character at least.
            int lo = 1, hi = s.Length - 1;
            while (lo < hi)
            {
                var mid = (lo + hi + 1) / 2;
                if (Width(s.Substring(0, mid), style) <= width) lo = mid;
                else hi = mid - 1;
            }
            var cut = lo;
            if (full <= 2f * width)
            {
                var best = -1;
                // A question may also break after its ？ or ！ (wrap_cjk_n's marks for questions).
                var marks = question ? QuestionMarks : PhraseMarks;
                for (var i = 1; i <= lo; i++)
                    if (marks.IndexOf(s[i - 1]) >= 0 && Width(s.Substring(i).TrimStart(' '), style) <= width) best = i;
                cut = best > 0 ? best : Mathf.Min((s.Length + 1) / 2, lo);
            }
            else if (question)
            {
                for (var i = lo; i > 0; i--)
                    if (QuestionMarks.IndexOf(s[i - 1]) >= 0 && Width(s.Substring(0, i), style) >= .6f * width)
                    {
                        cut = i;
                        break;
                    }
            }
            cut = SafeCut(s, cut);
            if (cut < s.Length && CoreCount(s, cut) == 1)
            {
                // No lone last character: the last character of this line goes down with it.
                var k = cut - 1;
                while (k > 0 && (s[k] == ' ' || NoStartMarks.IndexOf(s[k]) >= 0)) k--;
                if (k > 0) cut = SafeCut(s, k);
            }
            lines.Add(s.Substring(0, cut).TrimEnd(' '));
            s = s.Substring(cut).TrimStart(' ');
        }
        return lines.ToArray();
    }

    // Up to U+1FFF: Latin, figures, Greek, Cyrillic and the Latin-1 marks (· included); never CJK or the full-width marks.
    static bool IsLatinChar(char c) => c > ' ' && c < '\u2000';

    // A break moved off a Latin word or number (to the word's start), off an opening mark, and past closing marks (they hang).
    static int SafeCut(string s, int cut)
    {
        if (cut > 0 && cut < s.Length && IsLatinChar(s[cut - 1]) && IsLatinChar(s[cut]))
        {
            var j = cut;
            while (j > 0 && IsLatinChar(s[j - 1])) j--;
            if (j > 0) cut = j;
        }
        while (cut > 1 && NoEndMarks.IndexOf(s[cut - 1]) >= 0) cut--;
        while (cut < s.Length && NoStartMarks.IndexOf(s[cut]) >= 0) cut++;
        return cut;
    }

    // The characters from i on that are neither marks nor spaces.
    static int CoreCount(string s, int from)
    {
        var n = 0;
        for (var i = from; i < s.Length; i++)
            if (s[i] != ' ' && NoStartMarks.IndexOf(s[i]) < 0) n++;
        return n;
    }

    /// <summary>"R2 B  PLAIN ROOM" as its code ("R2 B", for the chip) and the rest, at the first double space; none: no code.</summary>
    static void SplitLabel(string label, out string code, out string rest)
    {
        var i = string.IsNullOrEmpty(label) ? -1 : label.IndexOf("  ", StringComparison.Ordinal);
        code = i > 0 ? label.Substring(0, i) : null;
        rest = i > 0 ? label.Substring(i + 2) : label ?? "";
    }

    /// <summary>SplitLabel when the head is a code ("1.4  ALCOVE · DOORS", "R11 B  INSIDE"); else no code and the whole label
    /// ("MUSEUM  ·  0  HUB", "R  RESET").</summary>
    static void SplitCode(string label, out string code, out string rest)
    {
        SplitLabel(label, out code, out rest);
        if (code == null || IsCode(code)) return;
        code = null;
        rest = label ?? "";
    }

    /// <summary>A station's code ("0.1", "1.1A") or a ROOM TYPES stop's ("R7", "R11 B").</summary>
    static bool IsCode(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        var room = s[0] == 'R';
        var i = room ? 1 : 0;
        var start = i;
        while (i < s.Length && IsFigure(s[i])) i++;
        if (i == start) return false;
        if (room) return i == s.Length || (i == s.Length - 2 && s[i] == ' ' && IsCapital(s[i + 1]));
        if (i >= s.Length - 1 || s[i] != '.') return false;
        start = ++i;
        while (i < s.Length && IsFigure(s[i])) i++;
        return i > start && (i == s.Length || (i == s.Length - 1 && IsCapital(s[i])));
    }

    static bool IsCapital(char c) => c >= 'A' && c <= 'Z';

    /// <summary>A list row's code and name (the tours' list, ROOM TYPES' list) as the plan's rows set them: the code in Plex Mono
    /// Medium on the Bayon name's baseline (20 §1; Bayon's 1 reads as I), the name from the code column on, its figures in Plex
    /// Mono Medium too ("FLOOR 1", "B1"); no code, the name at x. y is the row's top; one line, as the names are.</summary>
    void RowLabel(float x, float y, string code, string name, float column, Color color, float alpha)
    {
        var baseline = y + BayonAscent * nameStyle.fontSize;
        if (string.IsNullOrEmpty(code)) column = 0f;
        else Span(x, baseline, code, rowCodeStyle, PlexAscent, color, alpha);
        KindSpan(x + column, baseline, name, nameStyle, rowCodeStyle, color, alpha, true);
    }

    /// <summary>A list row's key (1-9, 0) in Plex Mono Medium on the row's Bayon baseline, as its code.</summary>
    void RowKey(float x, float y, string key, Color color, float alpha) =>
        Span(x, y + BayonAscent * nameStyle.fontSize, key, rowCodeStyle, PlexAscent, color, alpha);
}
