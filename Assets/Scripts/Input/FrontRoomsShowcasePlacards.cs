using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Scene 3's placards in the world (交互设计, 2026-11-04), built to 平面视觉's type spec
/// (Documentation/research/showcase_scenes/20_placard_type.md, VL500, and its mocks) at true size: the spec's millimetres in
/// metres. Placement and content follow 10_research.md §3.4. Each Build* takes its row of FrontRoomsShowcaseTable, a parent
/// (unit scale) and a WORLD pose, and returns the root; nothing places itself (the museum, .Museum.cs, calls these).
/// - C station placard (BuildStationPlacard): 600 x 340 mm on its own stand (or, withStand false, slab and bracket only for a
///   rail the caller builds), face top edge 1.0 m, tilted 45 degrees back so it looks up at the reader. Pose: the floor under
///   the face's centre, +Z toward the reader.
/// - D group card (BuildGroupCard): 520 x 236 mm on a front, centre 1.37 m. Pose: the card's centre on the wall's face, +Z
///   out of the wall (OnWall).
/// - Floor tag (BuildFloorTag): 260 x 130 mm on the floor in front of a variant. Pose: its centre on the floor, +Z up, +Y the
///   way its text reads up (from the reader toward the variant; OnFloor).
/// - B room sign (BuildRoomSign): 640 x 400 mm beside an entry, centre 1.37 m. Pose as D.
/// - A hub board (BuildHubBoard): 1400 x 700 mm on two legs, top 1.5 m, its plan drawn in 2 mm paper lines from the Plan the
///   caller passes. Pose as C (Standing).
/// - The room number over an entry (BuildEntryNumber): Plex Mono Medium, cap 300 mm, in the panel colour, on the wall, no
///   panel. Pose: the centre of the digits' cap box on the wall's face, +Z out of the wall.
/// Construction: the panel is matte LIT (URP Lit, albedo #161513, smoothness 0, one shared material made at runtime, a box
/// whose front face is the panel) so it sits in the room's light; the text is UNLIT world-space UGUI: one Canvas per placard
/// (no CanvasScaler: scale factor 1, reference pixels per unit 1), 1 canvas unit = 1 mm (scale .001), 1 mm in front of the
/// face (the text never z-fights the panel), no raycaster and raycastTarget off, in the game's UI faces (Resources/Fonts).
/// Every line is its own legacy Text set on its baseline by FrontRoomsUITextLayout, at the em the spec's cap, x-height or em
/// asks for with each face's measured metrics: its glyphs are rastered at its kind's density for the kind's reading
/// distance (the font atlas has no mipmaps, so at most about 2x minified where it is read) and the Text's own scale makes
/// the em exact. The lines come pre-broken from FrontRoomsShowcaseTable (gen_table.py breaks
/// them offline with the same metrics and these constants); nothing here breaks or measures a Chinese line. Code chips are a
/// 1.2 mm paper keyline, radius 2.5 mm, padding 5 mm (a 9-sliced sprite made once at runtime, mipmapped). Colours exactly
/// #161513, #F4F1E8, #96938B, #F4DF3B. No colliders. Runtime only: no asset or file is written, no editor API.
/// </summary>
public static class FrontRoomsShowcasePlacards
{
    // ---------- mounting (20_placard_type.md §2; metres)

    /// <summary>C: the face's top edge above the floor.</summary>
    public const float StationTop = 1f;
    /// <summary>C: the face leans back this far from vertical, up toward the reader's eye at the stop mark.</summary>
    public const float StationTilt = 45f;
    /// <summary>D: the card's centre above the floor.</summary>
    public const float GroupCentre = 1.37f;
    /// <summary>B: the sign's centre above the floor.</summary>
    public const float RoomSignCentre = 1.37f;
    /// <summary>A: the board's top edge above the floor.</summary>
    public const float BoardTop = 1.5f;

    // ---------- layout in mm, y down from the face's top edge, x from its left edge (placards.py's frame). tools/gen_table.py
    // and tools/fit_render.py read every "const float" below from this file, so the breaks, the fit check and the build agree.

    // The faces, measured from the files (tools/measure_fonts.py, fontTools: glyph bounds / unitsPerEm): Bayon cap = H yMax
    // 1425 / 2048; IBM Plex Mono Regular and Medium cap 698 / 1000, x-height 516 / 1000, every advance 600 / 1000.
    const float BayonCap = .6958008f, PlexCap = .698f, PlexX = .516f, PlexAdvance = .6f;
    /// <summary>The text canvas floats this far in front of the face (mm).</summary>
    const float TextLift = 1f;
    /// <summary>Code chips: keyline, corner radius, padding (mm).</summary>
    const float ChipLine = 1.2f, ChipRadius = 2.5f, ChipPad = 5f;

    // C · station placard (placards.py station()). The Try rows sit 17 mm apart, the mock's KEYS over TOUCH; once a Try block
    // wraps, every row after the first sits StationWrapPitch apart (1.15 em of the steps: a descender clears the next line's
    // ascenders by 3.5 mm, at 17 mm only 0.5). Two foot lines sit StationMetaPitch apart (1.18 em: 2.1 mm clear, at 9 mm 0.6).
    const float StationW = 600f, StationH = 340f, StationM = 30f, StationThick = 12f;
    const float StationCodeCap = 18f, StationKindCap = 26f, StationKindGap = 10f, StationKindRise = .5f;
    const float StationRow1 = 26f, StationRow1After = 30f;
    const float StationLabelX = 5.2f, StationStepX = 9f, StationStepsIndent = 62f, StationTryPitch = 17f, StationWrapPitch = 20f, StationTryAfter = 12f;
    const float StationZhEm = 18.5f, StationZhPitch = 24f, StationWhatAfter = 10f, StationDecideIndent = 92f;
    const float StationBarX = 12f, StationBarW = 4f, StationBarTop = 6f, StationBarBottom = 5f;
    const float StationMetaX = 4.6f, StationMetaPitch = 10.5f, StationMetaDrop = 6f, StationLiveReserve = 8f, StationLiveGap = 12f;
    const float StationBracket = 140f, StationBracketDepth = 30f, StationPost = 40f, StationFoot = 260f, StationFootThick = 8f;

    // D · group card (placards.py group()).
    const float GroupW = 520f, GroupH = 236f, GroupM = 30f, GroupThick = 6f;
    const float GroupTitleCap = 34f, GroupZhEm = 24f, GroupTitleToZh = 40f, GroupZhPitch = 31f, GroupZhToChips = 62f;
    const float GroupChipX = 11f, GroupChipGap = 8f, GroupChipSpacing = 26f, GroupRowPitch = 34f;

    // Floor tag (placards.py floortag()).
    const float TagW = 260f, TagH = 130f, TagM = 20f, TagThick = 3f;
    const float TagLetterCap = 62f, TagNameX = 16f, TagNameGap = 16f, TagBaseRaise = 18f, TagNamePitch = 30f;

    // B · room sign (placards.py roomsign(); the foot's x-height 13 is the mock's).
    const float SignW = 640f, SignH = 400f, SignM = 36f, SignThick = 6f;
    const float SignNumberCap = 120f, SignNameCap = 44f, SignZhEm = 36f, SignNameGap = 28f;
    const float SignNameOne = 58f, SignNameTwo = 44f, SignNamePitch = 52f, SignZhAfter = 54f;
    const float SignKindsX = 11f, SignKindsFirst = 202f, SignKindsPitch = 26f, SignFootX = 13f, SignArrowX = 20f;

    // A · hub board (no mock: laid out here to §2's sizes; plan on the right, legend on the left, controls along the foot).
    const float BoardW = 1400f, BoardH = 700f, BoardM = 60f, BoardThick = 20f;
    const float BoardTitleCap = 80f, BoardZhEm = 64f, BoardTitleToZh = 78f;
    const float BoardLegendFirst = 300f, BoardLegendPitch = 56f, BoardLegendLinePitch = 38f, BoardLegendCap = 30f, BoardLegendGap = 16f;
    const float BoardKeysX = 20f, BoardKeysPitch = 44f;
    const float BoardPlanLeft = 640f, BoardPlanTop = 190f, BoardPlanGap = 30f, BoardPlanLine = 2f, BoardRoomCap = 60f;
    const float BoardGateCells = .5f, BoardAlcoveInset = 4f;
    const float BoardLegX = 500f, BoardLeg = 50f, BoardFootW = 80f, BoardFootD = 420f, BoardFootThick = 12f;

    // The room number over an entry.
    const float NumberCap = 300f;

    // Reading distances (m; 20_placard_type.md §2 "Read from"): C the stop mark, D the lane, the floor tag standing behind
    // it, B across the lane, A the spawn; the number over an entry is read across the hub, 2 to 12 m.
    const float StationRead = 1.1f, GroupRead = 1.5f, TagRead = 1.8f, RoomSignRead = 3f, BoardRead = 3f, NumberRead = 12f;
    /// <summary>The game camera's focal length at 1080p (px): vertical fov 76 (FrontRooms3DGame.CreateCamera), 540 / tan 38.
    /// A face d metres away is drawn at Focal1080 / (1000 d) screen px per mm.</summary>
    const float Focal1080 = 691.2f;

    // Raster density per kind: the glyph raster, px per mm of the face. The legacy font atlas has no mipmaps (bilinear only),
    // so a line is rastered at about twice the 1080p screen's density where its kind is read, 2 x 691.2 / (1000 d): at most
    // about 2x minified there, so its strokes do not crawl as the head bobs; nearer than about half that distance it is
    // magnified a little (softer, never aliased; B and A at 1 m 1.5-1.8x). C 1.25 (1.1 m), D .9 (1.5 m), floor tag .75
    // (1.8 m), B and A .45 (3 m). The number over an entry keeps its 150 px raster (.35: 1x at 2 m, 6x at 12 m, where its
    // strokes are still 2.5 px wide: its edges may crawl, no stroke drops out). tools/fit_render.py prints every line's ratio.
    const float StationPpu = 1.25f, GroupPpu = .9f, TagPpu = .75f, RoomSignPpu = .45f, BoardPpu = .45f, NumberPpu = .35f;

    /// <summary>A line's glyph raster (px per em) stays within these; the Text's own scale makes up the em.</summary>
    const int MinRaster = 10, MaxFontSize = 300;
    const float Mm = .001f;
    const string DecideLabel = "要你决定";
    const string LiveName = "Live";

    static readonly Color32 Ink = new Color32(0x16, 0x15, 0x13, 0xFF);
    static readonly Color32 Paper = new Color32(0xF4, 0xF1, 0xE8, 0xFF);
    static readonly Color32 Grey = new Color32(0x96, 0x93, 0x8B, 0xFF);
    static readonly Color32 Accent = new Color32(0xF4, 0xDF, 0x3B, 0xFF);

    const string BayonPath = "Fonts/Bayon-Regular", PlexPath = "Fonts/IBMPlexMono-Regular", PlexMediumPath = "Fonts/IBMPlexMono-Medium";
    const string ScPath = "Fonts/NotoSansSC-Regular", ScBoldPath = "Fonts/NotoSansSC-Bold";

    // ---------- the hub board's plan (what the caller passes)

    /// <summary>
    /// The museum as board A draws it: cells on a grid, x to the right and y up = forward from the spawn (the museum's u and
    /// v, 80 §4). Rooms are outlined in 2 mm paper lines with a gap at every gate; each room shows its number (Plex Mono
    /// Medium, cap 60) and its alcove's picture (an empty frame until one is given); the legend names them from
    /// FrontRoomsShowcaseTable.Rooms.
    /// </summary>
    public sealed class Plan
    {
        public int cols, rows;
        public PlanRoom[] rooms;
        public PlanGate[] gates;
    }

    public sealed class PlanRoom
    {
        /// <summary>0 the hub, 1-4 the rooms (FrontRoomsShowcaseTable.Room.number).</summary>
        public int number;
        /// <summary>The room's cells.</summary>
        public RectInt cells;
        /// <summary>Its alcove's cells inside it; a zero size has none.</summary>
        public RectInt alcove;
        /// <summary>The alcove's picture, centre-cropped into its cells; null draws the empty frame.</summary>
        public Texture alcovePicture;
    }

    /// <summary>An opening (an arch) between two neighbouring cells, drawn as a gap in the outline.</summary>
    public struct PlanGate
    {
        public Vector2Int a, b;

        public PlanGate(Vector2Int a, Vector2Int b)
        {
            this.a = a;
            this.b = b;
        }
    }

    // ---------- poses

    /// <summary>C and A: stand on the floor at <paramref name="floorPoint"/> (under the face's centre), facing the reader.</summary>
    public static Pose Standing(Vector3 floorPoint, Vector3 towardReader) => new Pose(floorPoint, Quaternion.LookRotation(Flat(towardReader, Vector3.forward), Vector3.up));

    /// <summary>D, B and the entry number: centred <paramref name="height"/> above <paramref name="wallFloorPoint"/> (a point of the
    /// wall's face at floor level), facing out of the wall. GroupCentre and RoomSignCentre are the spec's heights.</summary>
    public static Pose OnWall(Vector3 wallFloorPoint, Vector3 outward, float height) =>
        new Pose(wallFloorPoint + Vector3.up * height, Quaternion.LookRotation(Flat(outward, Vector3.forward), Vector3.up));

    /// <summary>A floor tag: centred on the floor at <paramref name="floorPoint"/>, its text reading up toward the variant.</summary>
    public static Pose OnFloor(Vector3 floorPoint, Vector3 towardVariant) => new Pose(floorPoint, Quaternion.LookRotation(Vector3.up, Flat(towardVariant, Vector3.forward)));

    // ---------- C · station placard

    /// <summary>
    /// Placard C for one row: code chip and kind; Try (KEYS, TOUCH); what it is; 要你决定 with the question and the 4 mm accent
    /// bar; the foot (staging, status, rule set) and the live state, empty until SetLiveState. A slab 12 mm deep on a bracket,
    /// in the panel's material; with <paramref name="withStand"/> (the default) also a post and a floor plate, so it stands on
    /// its own; without, the bracket is left for a rail the caller builds (10_research §3.4: "on a rail at the front right
    /// corner of every bay"). Null for a null row.
    /// </summary>
    public static GameObject BuildStationPlacard(FrontRoomsShowcaseTable.Station st, Transform parent, Pose pose, bool withStand = true)
    {
        if (st == null) return null;
        var root = NewRoot("Placard C " + st.code, parent, pose);
        var t = root.transform;
        var tilt = StationTilt * Mathf.Deg2Rad;
        var up = new Vector3(0f, Mathf.Cos(tilt), -Mathf.Sin(tilt));
        var normal = new Vector3(0f, Mathf.Sin(tilt), Mathf.Cos(tilt));
        var h = StationH * Mm;
        var thick = StationThick * Mm;
        var centre = new Vector3(0f, StationTop - up.y * h * .5f, 0f);
        var faceRotation = Quaternion.LookRotation(-normal, up);
        Box(t, "Panel", centre - normal * (thick * .5f), faceRotation, new Vector3(StationW * Mm, h, thick), true);
        var depth = StationBracketDepth * Mm;
        var bracket = centre - normal * (thick + depth * .5f);
        Box(t, "Bracket", bracket, faceRotation, new Vector3(StationBracket * Mm, StationBracket * Mm, depth), true);
        if (withStand)
        {
            Box(t, "Post", new Vector3(0f, bracket.y * .5f, bracket.z), Quaternion.identity, new Vector3(StationPost * Mm, bracket.y, StationPost * Mm), true);
            Box(t, "Foot", new Vector3(0f, StationFootThick * Mm * .5f, bracket.z), Quaternion.identity, new Vector3(StationFoot * Mm, StationFootThick * Mm, StationFoot * Mm), true);
        }
        var c = NewCanvas(t, centre + normal * (TextLift * Mm), faceRotation, StationW, StationH);

        const float m = StationM, ppu = StationPpu;
        var y = m + StationRow1;
        var codeEm = PlexEmOfCap(StationCodeCap);
        var chip = Chip(c, m, y, st.code, PlexMedium, codeEm, StationCodeCap, ppu);
        Line(c, "Kind", st.kind, Bayon, StationKindCap / BayonCap, m + chip + StationKindGap, y + StationKindRise, Paper, ppu);
        y += StationRow1After;
        // KEYS over TOUCH at the mock's 17 mm; once either wraps, every Try row after the first at StationWrapPitch.
        var tryPitch = Count(st.keysLines) > 1 || Count(st.touchLines) > 1 ? StationWrapPitch : StationTryPitch;
        var tryRow = 0;
        y = TryRows(c, "KEYS", st.keysLines, y, tryPitch, ref tryRow);
        y = TryRows(c, "TOUCH", st.touchLines, y, tryPitch, ref tryRow);
        y += StationTryAfter;
        var what = st.whatLines;
        for (var i = 0; what != null && i < what.Length; i++)
        {
            y += StationZhPitch;
            Line(c, "What", what[i], Sc, StationZhEm, m, y, Paper, ppu);
        }
        y += StationWhatAfter;
        var decide = st.decideLines;
        if (decide != null && decide.Length > 0)
        {
            var y0 = y;
            for (var i = 0; i < decide.Length; i++)
            {
                y += StationZhPitch;
                if (i == 0) Line(c, "Decide label", DecideLabel, ScBold, StationZhEm, m, y, Accent, ppu);
                Line(c, "Decide", decide[i], ScBold, StationZhEm, m + StationDecideIndent, y, Paper, ppu);
            }
            Block(c, "Decide bar", m - StationBarX, y0 + StationBarTop, StationBarW, y + StationBarBottom - (y0 + StationBarTop), Accent);
        }
        // The foot, pinned to the bottom edge: the meta lines in grey, the live state right-aligned on the last one.
        var foot = StationH - m + StationMetaDrop;
        var meta = st.metaLines;
        var metaEm = PlexEmOfX(StationMetaX);
        for (var i = 0; meta != null && i < meta.Length; i++)
            Line(c, "Meta", meta[i], Plex, metaEm, m, foot - (meta.Length - 1 - i) * StationMetaPitch, Grey, ppu);
        Line(c, LiveName, "", PlexMedium, metaEm, StationW - m, foot, Accent, ppu, true);
        root.SetActive(true);
        return root;
    }

    /// <summary>The live state at the foot of placard C, right-aligned, as the exhibit's own script reports it ("CLOSED",
    /// "LOCKED"); null or empty clears it. Nothing for a root that is not a station placard, and nothing when the state is
    /// already shown (a caller polling it costs a lookup and a string compare, no text rebuild).</summary>
    public static void SetLiveState(GameObject stationPlacard, string state)
    {
        if (stationPlacard == null) return;
        var live = stationPlacard.transform.Find("Text/" + LiveName) as RectTransform;
        var text = live != null ? live.GetComponent<Text>() : null;
        if (text == null) return;
        var s = state ?? "";
        if (text.text == s) return;
        text.text = s;
        var w = Width(text.font, s, PlexEmOfX(StationMetaX), StationPpu);
        live.anchoredPosition = new Vector2(StationW - StationM - w, live.anchoredPosition.y);
    }

    // One Try block (KEYS or TOUCH), its label on its first row: the placard's first Try row StationTryPitch down from the
    // cursor (the mock's step), every later row `pitch` down.
    static float TryRows(RectTransform c, string label, string[] lines, float y, float pitch, ref int row)
    {
        for (var i = 0; lines != null && i < lines.Length; i++, row++)
        {
            y += row == 0 ? StationTryPitch : pitch;
            if (i == 0) Line(c, label, label, PlexMedium, PlexEmOfX(StationLabelX), StationM, y, Grey, StationPpu);
            Line(c, label + " steps", lines[i], Plex, PlexEmOfX(StationStepX), StationM + StationStepsIndent, y, Paper, StationPpu);
        }
        return y;
    }

    static int Count(string[] lines) => lines != null ? lines.Length : 0;

    // ---------- D · group card and the floor tags

    /// <summary>Card D for a row with variants (FrontRoomsShowcaseTable.Station.group): the property in Bayon, its Chinese,
    /// the variants as letter chips and names, in the table's rows. Null for a row with none.</summary>
    public static GameObject BuildGroupCard(FrontRoomsShowcaseTable.Station st, Transform parent, Pose pose)
    {
        var g = st != null ? st.group : null;
        if (g == null) return null;
        var root = NewRoot("Placard D " + st.code, parent, pose);
        var c = WallPanel(root.transform, GroupW, GroupH, GroupThick);
        const float m = GroupM, ppu = GroupPpu;
        var y = m + GroupTitleCap;
        Line(c, "Title", g.title, Bayon, GroupTitleCap / BayonCap, m, y, Paper, ppu);
        for (var i = 0; g.zh != null && i < g.zh.Length; i++)
        {
            y += i == 0 ? GroupTitleToZh : GroupZhPitch;
            Line(c, "Title zh", g.zh[i], Sc, GroupZhEm, m, y, Paper, ppu);
        }
        y += GroupZhToChips;
        var em = PlexEmOfX(GroupChipX);
        var cap = em * PlexCap;
        var count = g.letters != null ? g.letters.Length : 0;
        var rows = ChipRows(g.rowStarts, count);
        // Chip rows share columns: each column as wide as its widest variant (one row is the mock's run of chips).
        var columnX = new float[Mathf.Max(1, count)];
        var widest = new float[Mathf.Max(1, count)];
        for (var k = 0; k < count; k++)
        {
            var column = k - RowStart(rows, k);
            widest[column] = Mathf.Max(widest[column], Width(PlexMedium, g.letters[k], em, ppu) + 2f * ChipPad + GroupChipGap + Width(Plex, NameOf(g, k), em, ppu));
        }
        columnX[0] = m;
        for (var k = 1; k < count; k++) columnX[k] = columnX[k - 1] + widest[k - 1] + GroupChipSpacing;
        for (var r = 0; r < rows.Length; r++)
        {
            var end = r + 1 < rows.Length ? rows[r + 1] : count;
            for (var k = rows[r]; k < end && k < count; k++)
            {
                var x = columnX[k - rows[r]];
                var w = Chip(c, x, y, g.letters[k], PlexMedium, em, cap, ppu);
                Line(c, "Variant", NameOf(g, k), Plex, em, x + w + GroupChipGap, y, Paper, ppu);
            }
            y += GroupRowPitch;
        }
        root.SetActive(true);
        return root;
    }

    static int RowStart(int[] rows, int k)
    {
        var start = 0;
        for (var r = 0; r < rows.Length; r++) if (rows[r] <= k) start = rows[r];
        return start;
    }

    // The first variant of each chip row as the card uses it: 0, then the table's later starts that rise and stay under
    // `count` (anything else is dropped, so a malformed row list cannot index outside the variants).
    static int[] ChipRows(int[] given, int count)
    {
        var n = 1;
        var last = 0;
        for (var i = 0; given != null && i < given.Length; i++)
            if (given[i] > last && given[i] < count)
            {
                last = given[i];
                n++;
            }
        var rows = new int[n];
        n = 1;
        last = 0;
        for (var i = 0; given != null && i < given.Length; i++)
            if (given[i] > last && given[i] < count)
            {
                last = given[i];
                rows[n++] = last;
            }
        return rows;
    }

    static string NameOf(FrontRoomsShowcaseTable.Group g, int k) => g.names != null && k < g.names.Length ? g.names[k] ?? "" : "";

    /// <summary>The floor tag of variant <paramref name="variant"/> (0 = A) of a row with variants: its letter and name. Null
    /// when the row has no such variant.</summary>
    public static GameObject BuildFloorTag(FrontRoomsShowcaseTable.Station st, int variant, Transform parent, Pose pose)
    {
        var g = st != null ? st.group : null;
        if (g == null || g.letters == null || variant < 0 || variant >= g.letters.Length) return null;
        var root = NewRoot("Floor tag " + st.code + " " + g.letters[variant], parent, pose);
        var c = WallPanel(root.transform, TagW, TagH, TagThick);
        const float m = TagM, ppu = TagPpu;
        var baseline = TagH - m - TagBaseRaise;
        var letterEm = PlexEmOfCap(TagLetterCap);
        Line(c, "Letter", g.letters[variant], PlexMedium, letterEm, m, baseline, Paper, ppu);
        var x = m + Width(PlexMedium, g.letters[variant], letterEm, ppu) + TagNameGap;
        var lines = g.tagLines != null && variant < g.tagLines.Length ? g.tagLines[variant] : null;
        for (var i = 0; lines != null && i < lines.Length; i++)
            Line(c, "Name", lines[i], PlexMedium, PlexEmOfX(TagNameX), x, baseline - (lines.Length - 1 - i) * TagNamePitch, Paper, ppu);
        root.SetActive(true);
        return root;
    }

    // ---------- B · room sign and the room number

    /// <summary>Sign B for a room: number; name over its Chinese; the kinds it holds; stations and minutes, and the arrow to
    /// its first station. Null for a null room.</summary>
    public static GameObject BuildRoomSign(FrontRoomsShowcaseTable.Room room, Transform parent, Pose pose)
    {
        if (room == null) return null;
        var root = NewRoot("Placard B room " + room.number, parent, pose);
        var c = WallPanel(root.transform, SignW, SignH, SignThick);
        const float m = SignM, ppu = RoomSignPpu;
        var number = room.number.ToString();
        var numberEm = PlexEmOfCap(SignNumberCap);
        Line(c, "Number", number, PlexMedium, numberEm, m, m + SignNumberCap, Paper, ppu);
        var x = m + Width(PlexMedium, number, numberEm, ppu) + SignNameGap;
        var name = room.signNameLines;
        var y = m + (name != null && name.Length > 1 ? SignNameTwo : SignNameOne);
        for (var i = 0; name != null && i < name.Length; i++)
        {
            if (i > 0) y += SignNamePitch;
            Line(c, "Name", name[i], Bayon, SignNameCap / BayonCap, x, y, Paper, ppu);
        }
        if (!string.IsNullOrEmpty(room.nameZh)) Line(c, "Name zh", room.nameZh, ScBold, SignZhEm, x, y + SignZhAfter, Paper, ppu);
        var kinds = room.signKindsLines;
        for (var i = 0; kinds != null && i < kinds.Length; i++)
            Line(c, "Kinds", kinds[i], Plex, PlexEmOfX(SignKindsX), m, m + SignKindsFirst + i * SignKindsPitch, Paper, ppu);
        Line(c, "Foot", room.signFoot, PlexMedium, PlexEmOfX(SignFootX), m, SignH - m, Accent, ppu);
        if (!string.IsNullOrEmpty(room.signArrow))
        {
            var arrowEm = PlexEmOfX(SignArrowX);
            Line(c, "Arrow", room.signArrow, PlexMedium, arrowEm, SignW - m - Width(PlexMedium, room.signArrow, arrowEm, ppu), SignH - m, Accent, ppu);
        }
        root.SetActive(true);
        return root;
    }

    /// <summary>The room's number large over its entry, read across the hub: Plex Mono Medium, cap 300 mm, in the panel
    /// colour, 1 mm off the wall, no panel.</summary>
    public static GameObject BuildEntryNumber(int number, Transform parent, Pose pose)
    {
        var root = NewRoot("Room number " + number, parent, pose);
        var s = number.ToString();
        var em = PlexEmOfCap(NumberCap);
        var w = Width(PlexMedium, s, em, NumberPpu);
        var c = NewCanvas(root.transform, new Vector3(0f, 0f, TextLift * Mm), Quaternion.Euler(0f, 180f, 0f), w, NumberCap);
        Line(c, "Number", s, PlexMedium, em, 0f, NumberCap, Ink, NumberPpu);
        root.SetActive(true);
        return root;
    }

    // ---------- A · hub board

    /// <summary>
    /// Board A on two legs: the scene's name and its Chinese; the legend (each room's number and name); the plan of
    /// <paramref name="plan"/> on the right, scaled to fit above the controls; the controls along the foot (desktop lines, then
    /// touch lines). Lines from FrontRoomsShowcaseTable.BoardPlacard and Rooms. A null plan leaves the plan out and lists
    /// every room of the table.
    /// </summary>
    public static GameObject BuildHubBoard(Plan plan, Transform parent, Pose pose)
    {
        var root = NewRoot("Placard A hub board", parent, pose);
        var t = root.transform;
        var h = BoardH * Mm;
        var thick = BoardThick * Mm;
        var centre = new Vector3(0f, BoardTop - h * .5f, 0f);
        Box(t, "Panel", centre + new Vector3(0f, 0f, -thick * .5f), Quaternion.identity, new Vector3(BoardW * Mm, h, thick), true);
        var legZ = -(thick + BoardLeg * Mm * .5f);
        var legH = BoardTop - .1f;
        for (var side = -1; side <= 1; side += 2)
        {
            var x = side * BoardLegX * Mm;
            Box(t, "Leg", new Vector3(x, legH * .5f, legZ), Quaternion.identity, new Vector3(BoardLeg * Mm, legH, BoardLeg * Mm), true);
            Box(t, "Foot", new Vector3(x, BoardFootThick * Mm * .5f, legZ), Quaternion.identity, new Vector3(BoardFootW * Mm, BoardFootThick * Mm, BoardFootD * Mm), true);
        }
        var c = NewCanvas(t, centre + new Vector3(0f, 0f, TextLift * Mm), Quaternion.Euler(0f, 180f, 0f), BoardW, BoardH);
        const float m = BoardM, ppu = BoardPpu;
        var b = FrontRoomsShowcaseTable.BoardPlacard;
        var y = m + BoardTitleCap;
        if (b != null)
        {
            Line(c, "Title", b.name, Bayon, BoardTitleCap / BayonCap, m, y, Paper, ppu);
            if (!string.IsNullOrEmpty(b.zh)) Line(c, "Title zh", b.zh, ScBold, BoardZhEm, m, y + BoardTitleToZh, Paper, ppu);
        }

        // The controls, from the foot up: desktop lines, then touch lines.
        var keys = b != null && b.keysLines != null ? b.keysLines : new string[0];
        var touch = b != null && b.touchLines != null ? b.touchLines : new string[0];
        var count = keys.Length + touch.Length;
        var keysEm = PlexEmOfX(BoardKeysX);
        for (var i = 0; i < count; i++)
        {
            var line = i < keys.Length ? keys[i] : touch[i - keys.Length];
            Line(c, i < keys.Length ? "Keys" : "Touch", line, Plex, keysEm, m, BoardH - m - (count - 1 - i) * BoardKeysPitch, Paper, ppu);
        }
        var controlsTop = BoardH - m - (count - 1) * BoardKeysPitch - keysEm * PlexCap;
        if (count == 0) controlsTop = BoardH - m;

        // The legend: the rooms of the plan (all of the table's without one), in number order.
        var legendEm = PlexEmOfCap(BoardLegendCap);
        var ly = BoardLegendFirst;
        for (var n = 0; n <= 9; n++)
        {
            if (plan != null ? PlanRoomOf(plan, n) == null : RoomRow(n) == null) continue;
            var row = RoomRow(n);
            var label = n.ToString();
            Line(c, "Legend number", label, PlexMedium, legendEm, m, ly, Paper, ppu);
            var names = row != null ? row.boardNameLines : null;
            var nx = m + Width(PlexMedium, label, legendEm, ppu) + BoardLegendGap;
            var lines = names != null ? names.Length : 0;
            for (var i = 0; i < lines; i++) Line(c, "Legend name", names[i], Bayon, BoardLegendCap / BayonCap, nx, ly + i * BoardLegendLinePitch, Paper, ppu);
            ly += BoardLegendPitch + Mathf.Max(0, lines - 1) * BoardLegendLinePitch;
        }

        if (plan != null) DrawPlan(c, plan, BoardPlanLeft, BoardPlanTop, BoardW - m, controlsTop - BoardPlanGap);
        root.SetActive(true);
        return root;
    }

    static void DrawPlan(RectTransform c, Plan plan, float left, float top, float right, float bottom)
    {
        if (plan.cols <= 0 || plan.rows <= 0 || plan.rooms == null) return;
        var cell = Mathf.Min((right - left) / plan.cols, (bottom - top) / plan.rows);
        if (cell <= 0f) return;
        var ox = right - plan.cols * cell;
        var oy = top + plan.rows * cell;
        var numberEm = PlexEmOfCap(BoardRoomCap);
        foreach (var room in plan.rooms)
        {
            if (room == null) continue;
            var r = room.cells;
            Side(c, plan, r, true, r.yMin, ox, oy, cell);
            Side(c, plan, r, true, r.yMax, ox, oy, cell);
            Side(c, plan, r, false, r.xMin, ox, oy, cell);
            Side(c, plan, r, false, r.xMax, ox, oy, cell);

            // The number, centred in the room clear of its alcove's band.
            float x0 = r.xMin, x1 = r.xMax, y0 = r.yMin, y1 = r.yMax;
            var a = room.alcove;
            if (a.width > 0 && a.height > 0)
            {
                if (a.yMax == r.yMax && a.yMin > r.yMin) y1 = a.yMin;
                else if (a.yMin == r.yMin && a.yMax < r.yMax) y0 = a.yMax;
                else if (a.xMin == r.xMin && a.xMax < r.xMax) x0 = a.xMax;
                else if (a.xMax == r.xMax && a.xMin > r.xMin) x1 = a.xMin;
                Alcove(c, room, ox, oy, cell);
            }
            var label = room.number.ToString();
            var w = Width(PlexMedium, label, numberEm, BoardPpu);
            var cx = ox + (x0 + x1) * .5f * cell;
            var cy = oy - (y0 + y1) * .5f * cell;
            Line(c, "Plan number", label, PlexMedium, numberEm, cx - w * .5f, cy + BoardRoomCap * .5f, Paper, BoardPpu);
        }
    }

    // One side of a room's outline at grid line `at` (a row line when horizontal), in runs between its gates' gaps; the
    // runs stretch half a line past the room's corners, so the corners close.
    static void Side(RectTransform c, Plan plan, RectInt r, bool horizontal, int at, float ox, float oy, float cell)
    {
        var lw = BoardPlanLine;
        var gap = BoardGateCells;
        var from = horizontal ? r.xMin : r.yMin;
        var to = horizontal ? r.xMax : r.yMax;
        var start = (float)from;
        for (var k = from; k <= to; k++)
        {
            var gate = k < to && HasGate(plan, horizontal, at, k);
            if (k < to && !gate) continue;
            var end = k < to ? k + .5f - gap * .5f : to;
            if (end > start)
            {
                var extA = Mathf.Approximately(start, from) ? lw * .5f : 0f;
                var extB = k == to ? lw * .5f : 0f;
                if (horizontal)
                    Block(c, "Plan line", ox + start * cell - extA, oy - at * cell - lw * .5f, (end - start) * cell + extA + extB, lw, Paper);
                else
                    Block(c, "Plan line", ox + at * cell - lw * .5f, oy - end * cell - extB, lw, (end - start) * cell + extA + extB, Paper);
            }
            start = k + .5f + gap * .5f;
        }
    }

    // A gate across the unit edge at grid line `at`, cell `k` along it.
    static bool HasGate(Plan plan, bool horizontal, int at, int k)
    {
        if (plan.gates == null) return false;
        Vector2Int p, q;
        if (horizontal)
        {
            p = new Vector2Int(k, at - 1);
            q = new Vector2Int(k, at);
        }
        else
        {
            p = new Vector2Int(at - 1, k);
            q = new Vector2Int(at, k);
        }
        foreach (var g in plan.gates)
            if ((g.a == p && g.b == q) || (g.a == q && g.b == p)) return true;
        return false;
    }

    static void Alcove(RectTransform c, PlanRoom room, float ox, float oy, float cell)
    {
        var a = room.alcove;
        var inset = BoardAlcoveInset + BoardPlanLine * .5f;
        var x = ox + a.xMin * cell + inset;
        var y = oy - a.yMax * cell + inset;
        var w = a.width * cell - 2f * inset;
        var h = a.height * cell - 2f * inset;
        if (w <= 0f || h <= 0f) return;
        if (room.alcovePicture != null)
        {
            var go = new GameObject("Alcove picture", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            Place(rt, c, x, y, w, h);
            var image = go.AddComponent<RawImage>();
            image.texture = room.alcovePicture;
            image.raycastTarget = false;
            // Centre-crop the picture to the alcove's shape.
            var tw = Mathf.Max(1f, room.alcovePicture.width);
            var th = Mathf.Max(1f, room.alcovePicture.height);
            var want = w / h;
            var have = tw / th;
            image.uvRect = have > want ? new Rect((1f - want / have) * .5f, 0f, want / have, 1f) : new Rect(0f, (1f - have / want) * .5f, 1f, have / want);
            return;
        }
        var lw = BoardPlanLine;
        Block(c, "Alcove frame", x, y, w, lw, Paper);
        Block(c, "Alcove frame", x, y + h - lw, w, lw, Paper);
        Block(c, "Alcove frame", x, y + lw, lw, h - 2f * lw, Paper);
        Block(c, "Alcove frame", x + w - lw, y + lw, lw, h - 2f * lw, Paper);
    }

    static PlanRoom PlanRoomOf(Plan plan, int number)
    {
        if (plan.rooms == null) return null;
        foreach (var r in plan.rooms) if (r != null && r.number == number) return r;
        return null;
    }

    static FrontRoomsShowcaseTable.Room RoomRow(int number)
    {
        var rooms = FrontRoomsShowcaseTable.Rooms;
        for (var i = 0; rooms != null && i < rooms.Length; i++)
            if (rooms[i] != null && rooms[i].number == number) return rooms[i];
        return null;
    }

    // ---------- pieces

    static GameObject NewRoot(string name, Transform parent, Pose pose)
    {
        var go = new GameObject(name);
        // Built inactive and switched on at the end: no line generates its mesh before the placard is complete.
        go.SetActive(false);
        var t = go.transform;
        if (parent != null) t.SetParent(parent, false);
        // default(Pose) carries the zero quaternion, which no transform can take: treat it as no turn.
        var q = pose.rotation;
        if (q.x == 0f && q.y == 0f && q.z == 0f && q.w == 0f) q = Quaternion.identity;
        t.SetPositionAndRotation(pose.position, q);
        t.localScale = Vector3.one;
        return go;
    }

    // A wall (or floor) panel: the slab from the surface (z 0) to its face (z thick), the text canvas 1 mm in front.
    static RectTransform WallPanel(Transform root, float w, float h, float thick)
    {
        Box(root, "Panel", new Vector3(0f, 0f, thick * Mm * .5f), Quaternion.identity, new Vector3(w * Mm, h * Mm, thick * Mm), false);
        return NewCanvas(root, new Vector3(0f, 0f, (thick + TextLift) * Mm), Quaternion.Euler(0f, 180f, 0f), w, h);
    }

    // The text canvas: world space, 1 unit = 1 mm, its +Z into the face (the reader looks along it), w x h mm, pivot centred.
    // No CanvasScaler (it would run every frame and reset the canvas whenever a placard is hidden): scale factor 1, so a
    // Text's fontSize is its glyph raster in px, and 1 reference pixel per unit, so the chip ring's 16 px/mm sprite keeps
    // its 2.5 mm corners.
    static RectTransform NewCanvas(Transform parent, Vector3 localPosition, Quaternion localRotation, float w, float h)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.scaleFactor = 1f;
        canvas.referencePixelsPerUnit = 1f;
        rt.pivot = new Vector2(.5f, .5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.localPosition = localPosition;
        rt.localRotation = localRotation;
        rt.localScale = new Vector3(Mm, Mm, Mm);
        return rt;
    }

    /// <summary>One line: its left end at x, its baseline at y (mm from the canvas's top-left), at the given em, its glyphs
    /// rastered at <paramref name="density"/> px per mm (its kind's).</summary>
    static Text Line(RectTransform c, string name, string s, Font font, float em, float x, float baseline, Color32 colour, float density, bool keepEmpty = false)
    {
        if (font == null || (!keepEmpty && string.IsNullOrEmpty(s))) return null;
        // The canvas's scale factor is 1, so the generator rasters the glyphs at fontSize px and draws the em fontSize canvas
        // units tall; the Text's own scale brings that to the spec's em exactly.
        var size = Raster(em, density, Mono(font));
        var k = em / size;
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(c, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = Vector2.zero;
        rt.sizeDelta = new Vector2(size * 4f, size);
        rt.anchoredPosition = new Vector2(x, -baseline);
        rt.localScale = new Vector3(k, k, 1f);
        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyle.Normal;
        text.color = colour;
        text.alignment = TextAnchor.LowerLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = false;
        text.resizeTextForBestFit = false;
        text.alignByGeometry = false;
        text.lineSpacing = 1f;
        text.raycastTarget = false;
        text.text = s ?? "";
        // The baseline at local y 0 and the line's start at local x 0, from the glyphs' own metrics (the HUD's layout effect).
        var layout = go.AddComponent<FrontRoomsUITextLayout>();
        layout.centre = false;
        layout.tracking = 0f;
        return text;
    }

    /// <summary>A code chip: the keyline round the text, padding ChipPad; returns its width (mm).</summary>
    static float Chip(RectTransform c, float x, float baseline, string s, Font font, float em, float cap, float ppu)
    {
        if (string.IsNullOrEmpty(s)) return 0f;
        var w = Width(font, s, em, ppu) + 2f * ChipPad;
        var ring = Ring();
        var image = Block(c, "Chip", x, baseline - cap - ChipPad, w, cap + 2f * ChipPad, Paper);
        if (ring != null)
        {
            image.sprite = ring;
            image.type = Image.Type.Sliced;
            image.fillCenter = false;
            image.pixelsPerUnitMultiplier = 1f;
        }
        Line(c, "Code", s, font, em, x + ChipPad, baseline, Paper, ppu);
        return w;
    }

    /// <summary>A solid rect: top-left at (x, y) mm, y down.</summary>
    static Image Block(RectTransform c, string name, float x, float y, float w, float h, Color32 colour)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Place((RectTransform)go.transform, c, x, y, w, h);
        var image = go.AddComponent<Image>();
        image.color = colour;
        image.raycastTarget = false;
        return image;
    }

    static void Place(RectTransform rt, RectTransform c, float x, float y, float w, float h)
    {
        rt.SetParent(c, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(Mathf.Max(0f, w), Mathf.Max(0f, h));
    }

    static void Box(Transform parent, string name, Vector3 centre, Quaternion rotation, Vector3 size, bool shadows)
    {
        var material = PanelMaterial();
        if (material == null) return;
        var go = new GameObject(name);
        var t = go.transform;
        t.SetParent(parent, false);
        t.localPosition = centre;
        t.localRotation = rotation;
        t.localScale = size;
        go.AddComponent<MeshFilter>().sharedMesh = BoxMesh();
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        renderer.receiveShadows = true;
    }

    // ---------- metrics

    static float PlexEmOfCap(float cap) => cap / PlexCap;

    static float PlexEmOfX(float x) => x / PlexX;

    // The glyph raster (px per em, the Text's fontSize) for an em at a density (px per mm): of the valid rasters from
    // MinRaster up, the one nearest em x density but no more than 10 % over it (so a line is never much more minified than
    // its kind allows); ties go to the smaller. Valid: whole pixels and, for Plex Mono, a multiple of 5 px, so its 600-unit
    // advance is whole pixels too (the importer rounds every advance, shouldRoundAdvanceValue: 12.6 px drawn as 13 widens a
    // 44-character line by 3 %). tools/placard_metrics.py raster() is the same rule.
    static int Raster(float em, float density, bool mono)
    {
        var target = em * density;
        var step = mono ? 5 : 1;
        var best = MinRaster;
        for (var r = MinRaster + step; r <= MaxFontSize && r <= target * 1.1f + .001f; r += step)
            if (Mathf.Abs(r - target) < Mathf.Abs(best - target) - .001f) best = r;
        return best;
    }

    static bool Mono(Font font) => font != null && (font == plex || font == plexMedium);

    /// <summary>A line's advance width in mm as the Text draws it (the font's own advances at its raster).</summary>
    static float Width(Font font, string s, float em, float density)
    {
        if (string.IsNullOrEmpty(s)) return 0f;
        if (font != null)
        {
            var px = Raster(em, density, Mono(font));
            font.RequestCharactersInTexture(s, px, FontStyle.Normal);
            var sum = 0;
            var ok = true;
            for (var i = 0; i < s.Length && ok; i++)
            {
                CharacterInfo ci;
                ok = font.GetCharacterInfo(s[i], out ci, px, FontStyle.Normal);
                sum += ci.advance;
            }
            if (ok) return sum * em / px;
        }
        // Every width asked for is a Plex Mono line (codes, letters, numbers, names): a monospace advance.
        return s.Length * PlexAdvance * em;
    }

    static Vector3 Flat(Vector3 v, Vector3 fallback)
    {
        v.y = 0f;
        return v.sqrMagnitude > 1e-8f ? v.normalized : fallback;
    }

    // ---------- shared resources (made once, at runtime)

    static Font bayon, plex, plexMedium, sc, scBold;
    static Material panelMaterial;
    static Mesh boxMesh;
    static Sprite ring;
    static bool fontWarned, shaderWarned;

    static Font Bayon => Face(ref bayon, BayonPath);
    static Font Plex => Face(ref plex, PlexPath);
    static Font PlexMedium => Face(ref plexMedium, PlexMediumPath);
    static Font Sc => Face(ref sc, ScPath);
    static Font ScBold => Face(ref scBold, ScBoldPath);

    static Font Face(ref Font cache, string path)
    {
        if (cache != null) return cache;
        cache = Resources.Load<Font>(path);
        if (cache == null && !fontWarned)
        {
            fontWarned = true;
            Debug.LogWarning("FrontRoomsShowcasePlacards: Resources/" + path + " is missing; its lines are left out.");
        }
        return cache;
    }

    /// <summary>The panel: URP Lit, albedo #161513, smoothness 0, metallic 0 (matte), one material for every placard.</summary>
    static Material PanelMaterial()
    {
        if (panelMaterial != null) return panelMaterial;
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shader == null)
        {
            if (!shaderWarned)
            {
                shaderWarned = true;
                Debug.LogWarning("FrontRoomsShowcasePlacards: no URP Lit shader; the placards are built without panels.");
            }
            return null;
        }
        panelMaterial = new Material(shader) { name = "Showcase placard panel" };
        panelMaterial.SetColor("_BaseColor", Ink);
        panelMaterial.SetFloat("_Smoothness", 0f);
        panelMaterial.SetFloat("_Metallic", 0f);
        return panelMaterial;
    }

    /// <summary>A unit cube, its faces wound for Unity (clockwise seen from outside), normals out, a 0-1 UV square a face.</summary>
    static Mesh BoxMesh()
    {
        if (boxMesh != null) return boxMesh;
        var faces = new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        var vertices = new Vector3[24];
        var normals = new Vector3[24];
        var uv = new Vector2[24];
        var triangles = new int[36];
        for (var f = 0; f < 6; f++)
        {
            var n = faces[f];
            var up = n.y > .5f ? Vector3.forward : n.y < -.5f ? Vector3.back : Vector3.up;
            var right = Vector3.Cross(up, -n);
            var centre = n * .5f;
            var i = f * 4;
            vertices[i] = centre - right * .5f - up * .5f;
            vertices[i + 1] = centre - right * .5f + up * .5f;
            vertices[i + 2] = centre + right * .5f + up * .5f;
            vertices[i + 3] = centre + right * .5f - up * .5f;
            uv[i] = new Vector2(0f, 0f);
            uv[i + 1] = new Vector2(0f, 1f);
            uv[i + 2] = new Vector2(1f, 1f);
            uv[i + 3] = new Vector2(1f, 0f);
            for (var k = 0; k < 4; k++) normals[i + k] = n;
            var j = f * 6;
            triangles[j] = i;
            triangles[j + 1] = i + 1;
            triangles[j + 2] = i + 2;
            triangles[j + 3] = i;
            triangles[j + 4] = i + 2;
            triangles[j + 5] = i + 3;
        }
        boxMesh = new Mesh { name = "Showcase placard box" };
        boxMesh.vertices = vertices;
        boxMesh.normals = normals;
        boxMesh.uv = uv;
        boxMesh.triangles = triangles;
        boxMesh.RecalculateBounds();
        return boxMesh;
    }

    /// <summary>
    /// The chip keyline as a 9-sliced sprite: a white rounded ring, outer radius ChipRadius, stroke ChipLine, 16 texels a mm,
    /// 4 x 4 samples a texel, mipmapped and trilinear so it holds at the stop. Sprite pixelsPerUnit 16 with the canvas's
    /// referencePixelsPerUnit 1 makes its borders exactly the radius in mm.
    /// </summary>
    static Sprite Ring()
    {
        if (ring != null) return ring;
        const int texelsPerMm = 16;
        var radius = Mathf.RoundToInt(ChipRadius * texelsPerMm);
        var size = 2 * radius + 4;
        var stroke = ChipLine * texelsPerMm;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
        {
            name = "Showcase chip keyline",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Trilinear,
            anisoLevel = 4
        };
        var pixels = new Color32[size * size];
        var half = size * .5f;
        for (var py = 0; py < size; py++)
        for (var px = 0; px < size; px++)
        {
            var cover = 0;
            for (var sy = 0; sy < 4; sy++)
            for (var sx = 0; sx < 4; sx++)
            {
                var p = new Vector2(px + (sx + .5f) * .25f - half, py + (sy + .5f) * .25f - half);
                if (RoundRect(p, half, radius) <= 0f && RoundRect(p, half - stroke, radius - stroke) > 0f) cover++;
            }
            pixels[py * size + px] = new Color32(255, 255, 255, (byte)(cover * 255 / 16));
        }
        texture.SetPixels32(pixels);
        texture.Apply(true, true);
        ring = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(.5f, .5f), texelsPerMm, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        ring.name = "Showcase chip keyline";
        return ring;
    }

    // Signed distance from p to a square of half side `half` with corner radius `radius`, centred on the origin.
    static float RoundRect(Vector2 p, float half, float radius)
    {
        var qx = Mathf.Abs(p.x) - (half - radius);
        var qy = Mathf.Abs(p.y) - (half - radius);
        var outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
        return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
    }
}
