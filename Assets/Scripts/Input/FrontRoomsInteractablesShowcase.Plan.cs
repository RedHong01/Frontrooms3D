using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Scene 3, menu 5 MUSEUM · PLAN (交互设计, 2026-11-03; scene3/rooms/90_plan.md §2): the museum of 10_research §3 (hub 0 and
// rooms 1-4) drawn as a plan from FrontRoomsShowcaseTable, every station with its code and placard. ENTER tries a station now,
// in the tour that has it: the tour loads, and once the player is through the start door the guide jumps to the nearest
// example and shows the station's placard. In the tours every list row, tag and jump toast carries the code, and the toast
// adds the placard's two Chinese lines (what it is, what Red decides). The built museum (menu 5, .Museum.cs) opens this page
// over the game with P; there a row it has built is tagged HERE and ENTER walks to it.
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
    // The placard's lines under the toast (Chinese, from the table).
    string toastWhat, toastDecide;
    // An OS font for the Chinese lines (the bundled Noto SC is a 747-character subset); destroyed with the guide.
    Font zhFont;
    bool zhFontOwned;
    GUIStyle zhLineStyle, zhSmallStyle, zhToastStyle, planCodeStyle;

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
        toastUntil = Time.unscaledTime + 8f;
    }

    /// <summary>The toast: its title and line, then the placard's Chinese lines when a station set them.</summary>
    void DrawToast(float tx, float ty, float tw, float s)
    {
        var a = Mathf.Clamp01((toastUntil - Time.unscaledTime) / .4f);
        Text(new Rect(tx, ty, tw, 40f * s), toastTitle ?? "", toastTitleStyle, Accent, a);
        var line = toastLine ?? "";
        var h = toastLineStyle.CalcHeight(new GUIContent(line), tw);
        Text(new Rect(tx, ty + 40f * s, tw, h), line, toastLineStyle, Paper, a);
        ty += 40f * s + h + 8f * s;
        if (!string.IsNullOrEmpty(toastWhat))
        {
            var wh = zhToastStyle.CalcHeight(new GUIContent(toastWhat), tw);
            Text(new Rect(tx, ty, tw, wh), toastWhat, zhToastStyle, Paper, a);
            ty += wh + 4f * s;
        }
        if (!string.IsNullOrEmpty(toastDecide))
        {
            var decide = Decide(toastDecide);
            var dh = zhToastStyle.CalcHeight(new GUIContent(decide), tw);
            Text(new Rect(tx, ty, tw, dh), decide, zhToastStyle, Accent, a);
        }
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

    /// <summary>The marked list row's "what Red decides" line, under its HowTo; returns the y below it.</summary>
    float DrawRowDecide(int k, float x, float y, float w, float s)
    {
        var row = FrontRoomsShowcaseTable.OfGuideKind(k);
        if (row == null || string.IsNullOrEmpty(row.decide)) return y;
        var line = Decide(row.decide);
        var h = zhSmallStyle.CalcHeight(new GUIContent(line), w);
        Text(new Rect(x, y - 2f * s, w, h), line, zhSmallStyle, Accent, .9f);
        return y + h + 6f * s;
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

    // ---------- the Chinese faces

    /// <summary>
    /// The styles of the Chinese lines and the plan's codes, rebuilt with the others when the scale changes (EnsureStyles).
    /// The font is made once from the OS: the first listed font that holds a character draws it (Font.CreateDynamicFontFromOSFont);
    /// Hiragino Sans GB holds every character of the table. Without one, the bundled subset.
    /// </summary>
    void EnsureZhStyles(float scale)
    {
        if (zhFont == null)
        {
            try
            {
                zhFont = Font.CreateDynamicFontFromOSFont(new[] { "Hiragino Sans GB", "PingFang SC", "Heiti SC", "Arial Unicode MS" }, 16);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Interactables showcase: no OS font for the Chinese lines (" + e.Message + "); the bundled subset draws them.");
                zhFont = null;
            }
            if (zhFont != null)
            {
                zhFont.hideFlags = HideFlags.HideAndDontSave;
                zhFontOwned = true;
            }
            else
            {
                zhFont = Resources.Load<Font>("Fonts/NotoSansSC-Regular");
                zhFontOwned = false;
            }
        }
        zhLineStyle = Style(zhFont, 16f * scale, TextAnchor.UpperLeft);
        zhSmallStyle = Style(zhFont, 14f * scale, TextAnchor.UpperLeft);
        zhToastStyle = Style(zhFont, 17f * scale, TextAnchor.UpperCenter);
        planCodeStyle = Style(mono, 12f * scale, TextAnchor.MiddleCenter);
    }

    void DestroyPlanFont()
    {
        if (zhFontOwned && zhFont != null) Destroy(zhFont);
        zhFont = null;
        zhFontOwned = false;
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
        var y = oy - 6f * s;
        var sign = RoomSign(planRoom);
        Text(new Rect(rx, y, rw, 40f * s), planRoom + "  " + (sign != null ? sign.name ?? "" : ""), titleStyle, Paper);
        y += 40f * s;
        if (sign != null)
        {
            y = Para(rx, y, rw, sign.holds, smallStyle, Paper, .85f, 4f * s);
            y = Para(rx, y, rw, Decide(sign.decide), zhLineStyle, Accent, 1f, 4f * s);
        }
        y += 10f * s;
        var all = FrontRoomsShowcaseTable.Stations;
        for (var i = 0; all != null && i < all.Length; i++)
        {
            var st = all[i];
            if (st == null || FrontRoomsShowcaseTable.RoomOf(st) != planRoom) continue;
            var marked = i == planIndex;
            if (marked) Fill(new Rect(rx - 14f * s, y + 6f * s, 4f * s, 14f * s), Accent);
            Text(new Rect(rx, y, 64f * s, 26f * s), st.code ?? "", nameStyle, marked ? Accent : Paper);
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

    /// <summary>Placard C (code and kind, try, touch, what it is, what Red decides, staging · status · rule) and card D (variants, the note).</summary>
    void DrawPlacard(FrontRoomsShowcaseTable.Station st, float x, float y, float w, float s)
    {
        Text(new Rect(x, y, w, 40f * s), LabelOf(st), titleStyle, Accent);
        y += 40f * s;
        var tryLine = st.tryKeys ?? (st.guideKind >= 0 && st.guideKind < KindCount ? HowTo[st.guideKind] : "");
        if (!string.IsNullOrEmpty(tryLine)) y = Para(x, y, w, "TRY  " + tryLine, lineStyle, Paper, .95f, 4f * s);
        if (!string.IsNullOrEmpty(st.tryTouch)) y = Para(x, y, w, "TOUCH  " + st.tryTouch, smallStyle, Paper, .8f, 8f * s);
        y = Para(x, y, w, st.what ?? "", zhLineStyle, Paper, 1f, 4f * s);
        y = Para(x, y, w, Decide(st.decide), zhLineStyle, Accent, 1f, 8f * s);
        var rule = st.rule == FrontRoomsShowcaseTable.Rule.Media ? "MEDIA BAY"
            : st.rule == FrontRoomsShowcaseTable.Rule.None ? "" : "RULE " + FrontRoomsShowcaseTable.RuleName(st.rule);
        var small = st.staging ?? "";
        if (!string.IsNullOrEmpty(st.status)) small += (small.Length > 0 ? "  ·  " : "") + st.status;
        if (rule.Length > 0) small += (small.Length > 0 ? "  ·  " : "") + rule;
        y = Para(x, y, w, small, smallStyle, Paper, .7f, 8f * s);
        if (!string.IsNullOrEmpty(st.variants)) y = Para(x, y, w, "VARIANTS  " + st.variants, smallStyle, Paper, .9f, 6f * s);
        Para(x, y, w, st.note ?? "", smallStyle, Paper, .9f, 0f);
    }
}
