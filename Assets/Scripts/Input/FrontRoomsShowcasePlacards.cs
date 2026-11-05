using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Scene 3's placards in the world (交互设计, 2026-11-04), built to 平面视觉's type spec v2.3
/// (Documentation/research/showcase_scenes/20_placard_type.md §1-§2, VL504, VL507, VL508, its mocks and placards_v2.py) at true
/// size: the spec's millimetres in metres. Placement and content follow 10_research.md §3.4. Each Build* takes its row of
/// FrontRoomsShowcaseTable, a parent (unit scale) and a WORLD pose, and returns the root; nothing places itself (the museum,
/// .Museum.cs, calls these with the public mounting and reading constants below).
/// - C station placard (BuildStationPlacard): 800 x 480 mm on its own post (withStand false: slab and bracket only, for a rail
///   the caller builds), face top edge 1.15 m, tilted 45 degrees back so it looks up at the reader standing 0.75 m from it.
///   Pose: the floor under the face's centre, +Z toward the reader (Standing). Footprint about 800 x 350 mm.
/// - D group card (BuildGroupCard): 640 x 290 mm on a front, centre 1.37 m. Pose: the card's centre on the wall's face, +Z out
///   of the wall (OnWall).
/// - Floor tag (BuildFloorTag): 380 x 150 mm on the floor in front of a variant. Pose: its centre on the floor, +Z up, +Y the
///   way its text reads up (from the reader toward the variant; OnFloor).
/// - B room sign (BuildRoomSign): 720 x 470 mm beside an entry, centre 1.37 m. Pose as D.
/// - A hub board (BuildHubBoard): 1400 x 700 mm on two legs, top 1.5 m; the plan of the Plan the caller passes on 35 mm cells
///   (numbers only), the legend right of it (number, name, Chinese), the controls on two lines. Pose as C (Standing).
/// - The room number over an entry (BuildEntryNumber): Plex Mono Medium, cap 300 mm, in the panel colour, on the wall, no
///   panel (unlit, F9). Pose: the centre of the digits' cap box on the wall's face, +Z out of the wall.
/// Construction: the panel is matte LIT (URP Lit, albedo #161513, smoothness 0, one shared material made at runtime, a box
/// whose front face is the panel) so it sits in the room's light; no placard part casts or receives a shadow (10 §3.5); the
/// text is UNLIT world-space UGUI: one Canvas per placard (no CanvasScaler: scale factor 1, reference pixels per unit 1),
/// 1 canvas unit = 1 mm (scale .001), 1 mm in front of the face, no raycaster and raycastTarget off, in the game's UI faces
/// (Resources/Fonts). Every line is its own legacy Text set on its baseline by FrontRoomsUITextLayout, at the em the spec's
/// cap, x-height or em asks for with each face's measured metrics: its glyphs are rastered at the density of its job, for
/// the distance that line has to read at (the font atlas has no mipmaps), and the Text's own scale makes the em exact; a
/// Bayon line's " · " is Bayon's own period, raised (BayonLine). The lines come pre-broken from FrontRoomsShowcaseTable
/// (tools/gen_table.py breaks them offline with the same metrics and these constants); nothing here breaks or measures a
/// Chinese line. Every Chinese line is set flush left, so the full-width marks that end one simply hang into the right
/// margin (20 §1 Setting, 标点悬挂: the generator measures a line without its trailing stop marks, never brackets or quotes;
/// 2.7's 。 and 4.1's ；).
/// Code chips are a 1.4 mm paper keyline, radius 3 mm, padding 6 mm (a 9-sliced sprite made once at runtime, mipmapped).
/// Colours exactly #161513, #F4F1E8, #96938B, #F4DF3B. No colliders. Runtime only: no asset or file is written, no editor API.
/// </summary>
public static class FrontRoomsShowcasePlacards
{
    // ---------- mounting and reading (20_placard_type.md v2 §2; metres). Reading distances run from the eye (1.62 m) to the
    // face's centre: FAR where a kind's identity must read (number, name, title), NEAR where the player steps up to read the
    // rest. The museum places every placard by these; tools/fit_render.py checks every line at them.

    /// <summary>C: the face's top edge above the floor (on its own post, F8).</summary>
    public const float StationTop = 1.15f;
    /// <summary>C: the face leans back this far from vertical, up toward the reader's eye.</summary>
    public const float StationTilt = 45f;
    /// <summary>C: the face's width and height (landscape, the same at every station).</summary>
    public const float StationWidth = .8f, StationHeight = .48f;
    /// <summary>C: NEAR, eye to the face's centre, where the whole placard is read (the foot from 0.8 m).</summary>
    public const float StationNear = .99f;
    /// <summary>C: the reader's eye this far (horizontal) from the vertical through the face's centre.</summary>
    public const float StationStandOff = .75f;
    /// <summary>C: the bracket's centre this far behind the face, along the face's normal; the post and its floor plate stand
    /// under it, StationBracketBack x cos(StationTilt) behind the face's centre in plan.</summary>
    public const float StationBracketBack = (StationThick + StationBracketDepth * .5f) * Mm;
    /// <summary>D: the card's centre above the floor.</summary>
    public const float GroupCentre = 1.37f;
    /// <summary>D: the card's width and height.</summary>
    public const float GroupWidth = .64f, GroupHeight = .29f;
    /// <summary>D: NEAR, eye to the card's centre.</summary>
    public const float GroupNear = 1.18f;
    /// <summary>D: the reader's eye this far (horizontal) from the card.</summary>
    public const float GroupStandOff = 1.15f;
    /// <summary>Floor tag: its width and height (height along the reading direction).</summary>
    public const float TagWidth = .38f, TagHeight = .15f;
    /// <summary>Floor tag: eye to the tag's centre, the reader standing TagStandBehind behind it (the floor's slope counted).</summary>
    public const float TagRead = 1.76f;
    /// <summary>Floor tag: the reader's eye this far (horizontal) behind the tag's centre.</summary>
    public const float TagStandBehind = .7f;
    /// <summary>B: the sign's centre above the floor.</summary>
    public const float RoomSignCentre = 1.37f;
    /// <summary>B: the sign's width and height.</summary>
    public const float RoomSignWidth = .72f, RoomSignHeight = .47f;
    /// <summary>B: FAR across the lane (number, name, Chinese) and NEAR at the entry (the rest), eye to the sign's centre.</summary>
    public const float RoomSignFar = 3f, RoomSignNear = 1.2f;
    /// <summary>A: the board's top edge above the floor (rooms 2 and 3 stay in view over it).</summary>
    public const float BoardTop = 1.5f;
    /// <summary>A: the board's width and height.</summary>
    public const float BoardWidth = 1.4f, BoardHeight = .7f;
    /// <summary>A: FAR from the spawn (title, Chinese, the plan's numbers) and NEAR (legend, controls), eye to the board's centre.</summary>
    public const float BoardFar = 3.04f, BoardNear = 1.57f;

    // ---------- layout in mm, y down from the face's top edge, x from its left edge (placards_v2.py's frame; every number
    // below is the spec's or that file's). tools/gen_table.py and tools/fit_render.py read every "const float" and "const int"
    // of this class from this file (expressions included), so the breaks, the fit check and the build agree.

    // The faces, measured from the files (tools/measure_fonts.py, fontTools: glyph bounds / unitsPerEm): Bayon cap = H yMax
    // 1425 / 2048; IBM Plex Mono Regular and Medium cap 698 / 1000, x-height 516 / 1000, every advance 600 / 1000. A Chinese
    // line's baseline sits .88 em under the line above's (placards_v2.py's rhythm).
    const float BayonCap = .6958008f, PlexCap = .698f, PlexX = .516f, PlexAdvance = .6f, ZhAscent = .88f;
    // Bayon has no U+00B7 (20 §1 Setting, v2.1; 平面视觉's interpunct.py, VL507): in a Bayon line " · " is Bayon's own period,
    // its ink 0 to 248 / 2048 em ('.' yMax), its baseline raised BayonDotLift x cap (.413) so that ink sits on the cap middle,
    // a Bayon word space each side (BayonLine). Not the import fallback's Plex Mono dot, low and round in a 48 mm gap at C's
    // cap 30.
    const float BayonPeriodInk = .1210938f, BayonDotLift = .5f - .5f * BayonPeriodInk / BayonCap;
    const string Interpunct = " · ";
    /// <summary>The text canvas floats this far in front of the face (mm).</summary>
    const float TextLift = 1f;
    /// <summary>Code chips (§1): keyline, corner radius, padding (mm).</summary>
    const float ChipLine = 1.4f, ChipRadius = 3f, ChipPad = 6f;

    // C · station placard (placards_v2.py station(); the sizes of 20 v2.3). Row 1: the code chip (its code centred on the
    // kind's cap) and the kind 14 mm on. Try: KEYS / TOUCH in the margin, the steps from x 140 (measure 620), every row 28 mm.
    // What it is, then 要你决定 with the question hanging 116 mm in (the label's 4 em, 98 mm at em 24.5, + 18), the 5 mm
    // accent bar 16 mm left of the margin from 8 mm under the block's top to 7 mm under its last baseline. The foot pinned to
    // the bottom: its last baseline 4 mm under the bottom margin, the live state right-aligned on it, the lines above at
    // StationMetaPitch, 21 mm = 1.03 em at x-height 10.5 (the spec's: v2's 17 sat under the em and 2.2's 'g' met the '('
    // below; Plex's descender .212 em and its brackets' .76 em take 19.8 mm), so every glyph pair of the face stays 1 mm apart.
    const float StationW = 800f, StationH = 480f, StationM = 40f, StationThick = 12f;
    const float StationCodeCap = 20f, StationKindCap = 30f, StationKindGap = 14f, StationHeadAfter = 16f;
    const float StationLabelCap = 17f, StationStepX = 12.5f, StationStepsLeft = 140f, StationTryPitch = 28f, StationTryAfter = 12f;
    const float StationZhEm = 24.5f, StationZhPitch = 33f, StationWhatAfter = 10f, StationDecideIndent = 116f;
    const float StationBarLeft = 16f, StationBarW = 5f, StationBarTop = 8f, StationBarBottom = 7f;
    // The foot's lines leave StationLiveReserve characters of the live state (Plex Mono Medium at the foot's x-height) and
    // StationLiveGap free at their right end (gen_table.py breaks them to that measure): the museum's longest word is 10
    // ("GAME LIGHT", "PLAIN DOOR").
    const float StationMetaX = 10.5f, StationMetaPitch = 21f, StationMetaDrop = 4f, StationLiveReserve = 10f, StationLiveGap = 20f;
    // The stand (F8: its own post): a bracket behind the face's centre, a 50 mm post, a 360 mm floor plate.
    const float StationBracket = 160f, StationBracketDepth = 40f, StationPost = 50f, StationFoot = 360f, StationFootThick = 10f;
    /// <summary>C's foot is read from 0.8 m (m; 20 §2).</summary>
    const float StationFootRead = .8f;

    // D · group card (placards_v2.py group()): the property, its Chinese 18 mm + .88 em under it, then each chip row 26 mm +
    // the chip's cap + 12 mm under the line above; a row is a run: chip, 9 mm, name, 28 mm, the next chip.
    const float GroupW = 640f, GroupH = 290f, GroupM = 34f, GroupThick = 6f;
    const float GroupTitleCap = 36f, GroupZhEm = 28.5f, GroupZhGap = 18f, GroupZhPitch = 36f;
    const float GroupChipX = 15f, GroupChipGap = 9f, GroupChipSpacing = 28f, GroupRowGap = 26f, GroupRowAfter = 12f;

    // Floor tag (placards_v2.py floortag()): the letter on a baseline 8 mm over the bottom margin, the name 18 mm right of it,
    // its lines bottom-aligned to the letter, cap + 12 mm apart.
    const float TagW = 380f, TagH = 150f, TagM = 22f, TagThick = 3f;
    const float TagLetterCap = 62f, TagNameX = 23.5f, TagNameGap = 18f, TagBaseRaise = 8f, TagNameLead = 12f;

    // B · room sign (placards_v2.py roomsign()): the number top left; the name 30 mm right of it, cap + 14 mm a line; its
    // Chinese 18 mm + .88 em under the name; the kinds from the margin, 22 mm then 32 mm a line; foot and arrow on the bottom
    // margin.
    const float SignW = 720f, SignH = 470f, SignM = 40f, SignThick = 6f;
    const float SignNumberCap = 120f, SignNameCap = 50f, SignNameGap = 30f, SignNameLead = 14f, SignZhEm = 72f, SignZhGap = 18f;
    const float SignKindsX = 15.5f, SignKindsGap = 22f, SignKindsPitch = 32f, SignFootX = 16f, SignArrowX = 20f;

    // A · hub board (placards_v2.py board()): title, its Chinese 22 mm + .88 em under it; the plan 31 mm under that, 2 mm paper
    // lines on 35 mm cells (16 x 8, the museum), each room's number only, centred; the legend 50 mm right of the plan, a row
    // every 56 mm from 8 mm under the plan's top: number, the name 40 mm on, its Chinese 22 mm after the name, 2 mm lower;
    // the controls on the bottom margin, 50 mm a line.
    const float BoardW = 1400f, BoardH = 700f, BoardM = 50f, BoardThick = 20f;
    const float BoardTitleCap = 80f, BoardZhEm = 72.5f, BoardZhGap = 22f, BoardPlanGap = 31f;
    const float BoardPlanCell = 35f, BoardPlanLine = 2f, BoardRoomCap = 60f, BoardAlcoveInset = 4f;
    const int BoardPlanCols = 16, BoardPlanRows = 8;
    const float BoardLegendGap = 50f, BoardLegendTop = 8f, BoardLegendPitch = 56f, BoardLegendLinePitch = 40f;
    const float BoardLegendCap = 30f, BoardLegendNameX = 40f, BoardLegendZhEm = 42f, BoardLegendZhGap = 22f, BoardLegendZhDrop = 2f;
    const float BoardKeysX = 22.5f, BoardKeysPitch = 50f;
    const float BoardLegX = 500f, BoardLeg = 50f, BoardFootW = 80f, BoardFootD = 420f, BoardFootThick = 12f;

    // The room number over an entry (F9: unlit is fine for now).
    const float NumberCap = 300f;
    /// <summary>The number over an entry is read across the hub, 2 to 12 m; its raster is set for the hub's middle (m).</summary>
    const float NumberRead = 12f, NumberMid = 6f;

    /// <summary>The game camera's focal length at 1080p (px): vertical fov 76 (FrontRooms3DGame.CreateCamera), 540 / tan 38.
    /// A face d metres away is drawn at Focal1080 / (1000 d) screen px per mm.</summary>
    const float Focal1080 = 691.2f;

    // Raster density per LINE (px per mm of the face; F2's machinery: canvas scale factor 1, the Text's own scale sets the em).
    // 20_placard_type.md v2.2 §1 Production: "the legacy font atlas has no mipmaps (bilinear only), so each line is rastered at
    // twice the 1080p screen's density where that line has to read, ρ = 2 f / (1000 d), f = 691.2 px: at most 2x minified
    // there (no texel skipped), and 1:1 on a 2160p screen". Every Line call passes the constant of its job:
    // - C, D and the floor tag are read at one distance each, d = StationNear, GroupNear, TagRead: 1.396, 1.172, .785 px/mm.
    // - B and A are read at two, split by job. The lines that identify the sign from afar (B's number, name and Chinese; A's
    //   title, Chinese and plan numbers) take d = FAR: RoomSignFarPpu .46080, BoardFarPpu .45474, 2x minified at FAR; at NEAR
    //   B's are 1.25x magnified (soft but whole), A's 1.03x minified. The lines read stepping up (B's kinds, foot and arrow;
    //   A's legend and controls) take the geometric mean, d = sqrt(FAR x NEAR): RoomSignNearPpu .72859 (1.897 m), BoardNearPpu
    //   .63277 (2.185 m; written out, as a const takes no square root: change them with RoomSignFar/Near, BoardFar/Near),
    //   1.26x / 1.44x minified at NEAR; at FAR they are under the size floors, texture only.
    // - The number over an entry, read across the hub at 2-12 m, takes d = the hub's middle, NumberMid 6 m (20 v2.3 §2):
    //   .2304, 4x minified at 12 m, 1.5x magnified at 2 m (the .35 before drew it 6.1x minified at 12 m).
    // The spec's E (scene 4's district sign) is not built here.
    // tools/fit_render.py checks every line at its own d and renders the placards as seen at 1080p (fit/seen_*.png).
    const float StationPpu = 2f * Focal1080 / (1000f * StationNear), GroupPpu = 2f * Focal1080 / (1000f * GroupNear);
    const float TagPpu = 2f * Focal1080 / (1000f * TagRead), NumberPpu = 2f * Focal1080 / (1000f * NumberMid);
    const float RoomSignFarPpu = 2f * Focal1080 / (1000f * RoomSignFar), RoomSignNearPpu = .72859f;
    const float BoardFarPpu = 2f * Focal1080 / (1000f * BoardFar), BoardNearPpu = .63277f;

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
    /// v, 80 §4). Rooms are outlined in 2 mm paper lines; each room shows its number only (Plex Mono Medium, cap 60) and its
    /// alcove's picture when one is given (F6: the names stand in the legend right of the plan, from
    /// FrontRoomsShowcaseTable.Rooms). The gates are taken and not drawn: spec v2's plan is outlines and numbers (VL504), and
    /// at 35 mm cells a room's E and X arches, side by side, cut its hub wall into a dashed line.
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
        /// <summary>The alcove's picture, centre-cropped into its cells; null draws nothing there (the plan holds numbers only).</summary>
        public Texture alcovePicture;
    }

    /// <summary>An opening (an arch) between two neighbouring cells (kept for the caller; v2's plan does not draw it).</summary>
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
    /// Placard C for one row: the code chip and kind; Try (KEYS, TOUCH); what it is; 要你决定 with the question and the 5 mm
    /// accent bar; the foot (staging and status, F4: no rule set) and the live state, empty until SetLiveState. A slab 12 mm
    /// deep on a bracket, in the panel's material; with <paramref name="withStand"/> (the default, F8) also a post and a floor
    /// plate, so it stands on its own; without, the bracket is left for a rail the caller builds at the same height. Null for
    /// a null row.
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
        Box(t, "Panel", centre - normal * (thick * .5f), faceRotation, new Vector3(StationW * Mm, h, thick));
        var bracket = centre - normal * StationBracketBack;
        Box(t, "Bracket", bracket, faceRotation, new Vector3(StationBracket * Mm, StationBracket * Mm, StationBracketDepth * Mm));
        if (withStand)
        {
            Box(t, "Post", new Vector3(0f, bracket.y * .5f, bracket.z), Quaternion.identity, new Vector3(StationPost * Mm, bracket.y, StationPost * Mm));
            Box(t, "Foot", new Vector3(0f, StationFootThick * Mm * .5f, bracket.z), Quaternion.identity, new Vector3(StationFoot * Mm, StationFootThick * Mm, StationFoot * Mm));
        }
        var c = NewCanvas(t, centre + normal * (TextLift * Mm), faceRotation, StationW, StationH);

        const float m = StationM, ppu = StationPpu;
        var y = m + StationKindCap;
        var chip = Chip(c, m, y - (StationKindCap - StationCodeCap) * .5f, st.code, PlexMedium, PlexEmOfCap(StationCodeCap), StationCodeCap, ppu);
        BayonLine(c, "Kind", st.kind, StationKindCap, m + chip + StationKindGap, y, Paper, ppu);
        y += StationHeadAfter;
        y = TryRows(c, "KEYS", st.keysLines, y);
        y = TryRows(c, "TOUCH", st.touchLines, y);
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
            Block(c, "Decide bar", m - StationBarLeft, y0 + StationBarTop, StationBarW, y + StationBarBottom - (y0 + StationBarTop), Accent);
        }
        // The foot, pinned to the bottom edge: the staging and status in grey, the live state right-aligned on the last line.
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
    /// "GAME LIGHT"; up to 10 characters fit beside the foot: StationLiveReserve); null or empty clears it. Nothing for a root
    /// that is not a station placard, and nothing when the state is already shown (a caller polling it costs a lookup and a
    /// string compare, no text rebuild).</summary>
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

    // One Try block (KEYS or TOUCH): every row StationTryPitch under the one above, its label (Plex Mono Medium, cap 17, grey)
    // in the margin on its first row, the steps (Plex Mono, x-height 12.5) from StationStepsLeft.
    static float TryRows(RectTransform c, string label, string[] lines, float y)
    {
        for (var i = 0; lines != null && i < lines.Length; i++)
        {
            y += StationTryPitch;
            if (i == 0) Line(c, label, label, PlexMedium, PlexEmOfCap(StationLabelCap), StationM, y, Grey, StationPpu);
            Line(c, label + " steps", lines[i], Plex, PlexEmOfX(StationStepX), StationStepsLeft, y, Paper, StationPpu);
        }
        return y;
    }

    // ---------- D · group card and the floor tags

    /// <summary>Card D for a row with variants (FrontRoomsShowcaseTable.Station.group): the property in Bayon, its Chinese,
    /// the variants as letter chips and names, a run of them per row of the table's rowStarts. Null for a row with none.</summary>
    public static GameObject BuildGroupCard(FrontRoomsShowcaseTable.Station st, Transform parent, Pose pose)
    {
        var g = st != null ? st.group : null;
        if (g == null) return null;
        var root = NewRoot("Placard D " + st.code, parent, pose);
        var c = WallPanel(root.transform, GroupW, GroupH, GroupThick);
        const float m = GroupM, ppu = GroupPpu;
        var y = m + GroupTitleCap;
        BayonLine(c, "Title", g.title, GroupTitleCap, m, y, Paper, ppu);
        for (var i = 0; g.zh != null && i < g.zh.Length; i++)
        {
            y += i == 0 ? GroupZhGap + ZhAscent * GroupZhEm : GroupZhPitch;
            Line(c, "Title zh", g.zh[i], Sc, GroupZhEm, m, y, Paper, ppu);
        }
        var em = PlexEmOfX(GroupChipX);
        var cap = em * PlexCap;
        var count = g.letters != null ? g.letters.Length : 0;
        var rows = ChipRows(g.rowStarts, count);
        for (var r = 0; r < rows.Length && count > 0; r++)
        {
            y += GroupRowGap + cap + GroupRowAfter;
            var end = r + 1 < rows.Length ? rows[r + 1] : count;
            var x = m;
            for (var k = rows[r]; k < end; k++)
            {
                var w = Chip(c, x, y, g.letters[k], PlexMedium, em, cap, ppu);
                var name = NameOf(g, k);
                Line(c, "Variant", name, Plex, em, x + w + GroupChipGap, y, Paper, ppu);
                x += w + GroupChipGap + Width(Plex, name, em, ppu) + GroupChipSpacing;
            }
        }
        root.SetActive(true);
        return root;
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
        var nameEm = PlexEmOfX(TagNameX);
        var pitch = nameEm * PlexCap + TagNameLead;
        for (var i = 0; lines != null && i < lines.Length; i++)
            Line(c, "Name", lines[i], PlexMedium, nameEm, x, baseline - (lines.Length - 1 - i) * pitch, Paper, ppu);
        root.SetActive(true);
        return root;
    }

    // ---------- B · room sign and the room number

    /// <summary>Sign B for a room: number; name (one or two lines) over its Chinese, read across the lane; the kinds it
    /// holds; stations and minutes, and the arrow to its first station, read at the entry. Null for a null room.</summary>
    public static GameObject BuildRoomSign(FrontRoomsShowcaseTable.Room room, Transform parent, Pose pose)
    {
        if (room == null) return null;
        var root = NewRoot("Placard B room " + room.number, parent, pose);
        var c = WallPanel(root.transform, SignW, SignH, SignThick);
        // Number, name and Chinese identify the sign across the lane (FAR's density); kinds, foot and arrow are read at the entry.
        const float m = SignM, far = RoomSignFarPpu, near = RoomSignNearPpu;
        var number = room.number.ToString();
        var numberEm = PlexEmOfCap(SignNumberCap);
        Line(c, "Number", number, PlexMedium, numberEm, m, m + SignNumberCap, Paper, far);
        var x = m + Width(PlexMedium, number, numberEm, far) + SignNameGap;
        var y = m + SignNameCap;
        var name = room.signNameLines;
        for (var i = 0; name != null && i < name.Length; i++)
        {
            if (i > 0) y += SignNameCap + SignNameLead;
            BayonLine(c, "Name", name[i], SignNameCap, x, y, Paper, far);
        }
        if (!string.IsNullOrEmpty(room.nameZh))
        {
            y += SignZhGap + ZhAscent * SignZhEm;
            Line(c, "Name zh", room.nameZh, ScBold, SignZhEm, x, y, Paper, far);
        }
        y += SignKindsGap;
        var kinds = room.signKindsLines;
        var kindsEm = PlexEmOfX(SignKindsX);
        for (var i = 0; kinds != null && i < kinds.Length; i++)
        {
            y += SignKindsPitch;
            Line(c, "Kinds", kinds[i], Plex, kindsEm, m, y, Paper, near);
        }
        Line(c, "Foot", room.signFoot, PlexMedium, PlexEmOfX(SignFootX), m, SignH - m, Accent, near);
        if (!string.IsNullOrEmpty(room.signArrow))
        {
            var arrowEm = PlexEmOfX(SignArrowX);
            Line(c, "Arrow", room.signArrow, PlexMedium, arrowEm, SignW - m - Width(PlexMedium, room.signArrow, arrowEm, near), SignH - m, Accent, near);
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
    /// Board A on two legs: the scene's name over its Chinese; the plan of <paramref name="plan"/> under them on 35 mm cells
    /// (each room's number only; smaller cells when a plan has more than 16 x 8); the legend right of it, a row a room (number,
    /// name, Chinese in grey); the controls on the bottom (desktop lines, then touch lines). Lines from
    /// FrontRoomsShowcaseTable.BoardPlacard and Rooms. A null plan leaves the plan out and lists every room of the table.
    /// </summary>
    public static GameObject BuildHubBoard(Plan plan, Transform parent, Pose pose)
    {
        var root = NewRoot("Placard A hub board", parent, pose);
        var t = root.transform;
        var h = BoardH * Mm;
        var thick = BoardThick * Mm;
        var centre = new Vector3(0f, BoardTop - h * .5f, 0f);
        Box(t, "Panel", centre + new Vector3(0f, 0f, -thick * .5f), Quaternion.identity, new Vector3(BoardW * Mm, h, thick));
        var legZ = -(thick + BoardLeg * Mm * .5f);
        var legH = BoardTop - .1f;
        for (var side = -1; side <= 1; side += 2)
        {
            var x = side * BoardLegX * Mm;
            Box(t, "Leg", new Vector3(x, legH * .5f, legZ), Quaternion.identity, new Vector3(BoardLeg * Mm, legH, BoardLeg * Mm));
            Box(t, "Foot", new Vector3(x, BoardFootThick * Mm * .5f, legZ), Quaternion.identity, new Vector3(BoardFootW * Mm, BoardFootThick * Mm, BoardFootD * Mm));
        }
        var c = NewCanvas(t, centre + new Vector3(0f, 0f, TextLift * Mm), Quaternion.Euler(0f, 180f, 0f), BoardW, BoardH);
        // Title, Chinese and the plan's numbers read from the spawn (FAR's density); the legend and the controls up close.
        const float m = BoardM, far = BoardFarPpu, near = BoardNearPpu;
        var b = FrontRoomsShowcaseTable.BoardPlacard;
        var y = m + BoardTitleCap;
        if (b != null)
        {
            BayonLine(c, "Title", b.name, BoardTitleCap, m, y, Paper, far);
            if (!string.IsNullOrEmpty(b.zh))
            {
                y += BoardZhGap + ZhAscent * BoardZhEm;
                Line(c, "Title zh", b.zh, ScBold, BoardZhEm, m, y, Paper, far);
            }
        }
        var planTop = y + BoardPlanGap;
        if (plan != null) DrawPlan(c, plan, m, planTop);

        // The legend right of the plan: the rooms of the plan (all of the table's without one), in number order.
        var lx = m + BoardPlanCols * BoardPlanCell + BoardLegendGap;
        var numberEm = PlexEmOfCap(BoardLegendCap);
        var ly = planTop + BoardLegendTop;
        for (var n = 0; n <= 9; n++)
        {
            if (plan != null ? PlanRoomOf(plan, n) == null : RoomRow(n) == null) continue;
            var row = RoomRow(n);
            ly += BoardLegendPitch;
            Line(c, "Legend number", n.ToString(), PlexMedium, numberEm, lx, ly, Paper, near);
            var names = row != null ? row.boardNameLines : null;
            var lines = names != null ? names.Length : 0;
            var end = lx + BoardLegendNameX;
            for (var i = 0; i < lines; i++)
            {
                if (i > 0) ly += BoardLegendLinePitch;
                end = lx + BoardLegendNameX + BayonLine(c, "Legend name", names[i], BoardLegendCap, lx + BoardLegendNameX, ly, Paper, near);
            }
            if (row != null && !string.IsNullOrEmpty(row.nameZh))
                Line(c, "Legend zh", row.nameZh, Sc, BoardLegendZhEm, end + BoardLegendZhGap, ly + BoardLegendZhDrop, Grey, near);
        }

        // The controls on the bottom margin: desktop lines, then touch lines.
        var keys = b != null && b.keysLines != null ? b.keysLines : new string[0];
        var touch = b != null && b.touchLines != null ? b.touchLines : new string[0];
        var count = keys.Length + touch.Length;
        var keysEm = PlexEmOfX(BoardKeysX);
        for (var i = 0; i < count; i++)
        {
            var line = i < keys.Length ? keys[i] : touch[i - keys.Length];
            Line(c, i < keys.Length ? "Keys" : "Touch", line, Plex, keysEm, m, BoardH - m - (count - 1 - i) * BoardKeysPitch, Paper, near);
        }
        root.SetActive(true);
        return root;
    }

    // The plan from its top-left corner (left, top): 35 mm cells, fewer millimetres a cell only when the plan is larger than
    // the board's 16 x 8; y up = forward, so row 0 is the bottom.
    static void DrawPlan(RectTransform c, Plan plan, float left, float top)
    {
        if (plan.cols <= 0 || plan.rows <= 0 || plan.rooms == null) return;
        var cell = Mathf.Min(BoardPlanCell, Mathf.Min(BoardPlanCols * BoardPlanCell / plan.cols, BoardPlanRows * BoardPlanCell / plan.rows));
        var ox = left;
        var oy = top + plan.rows * cell;
        var numberEm = PlexEmOfCap(BoardRoomCap);
        var lw = BoardPlanLine;
        foreach (var room in plan.rooms)
        {
            if (room == null) continue;
            var r = room.cells;
            // The outline: each side centred on its grid line, running half a line past the corners so they close; a wall two
            // rooms share is drawn twice in the same place.
            float x0 = ox + r.xMin * cell, x1 = ox + r.xMax * cell, y0 = oy - r.yMax * cell, y1 = oy - r.yMin * cell;
            Block(c, "Plan line", x0 - lw * .5f, y0 - lw * .5f, x1 - x0 + lw, lw, Paper);
            Block(c, "Plan line", x0 - lw * .5f, y1 - lw * .5f, x1 - x0 + lw, lw, Paper);
            Block(c, "Plan line", x0 - lw * .5f, y0 + lw * .5f, lw, y1 - y0 - lw, Paper);
            Block(c, "Plan line", x1 - lw * .5f, y0 + lw * .5f, lw, y1 - y0 - lw, Paper);
            if (room.alcove.width > 0 && room.alcove.height > 0 && room.alcovePicture != null) Alcove(c, room, ox, oy, cell);
            // The number, its cap box centred in the room.
            var label = room.number.ToString();
            var w = Width(PlexMedium, label, numberEm, BoardFarPpu);
            var cx = ox + (r.xMin + r.xMax) * .5f * cell;
            var cy = oy - (r.yMin + r.yMax) * .5f * cell;
            Line(c, "Plan number", label, PlexMedium, numberEm, cx - w * .5f, cy + BoardRoomCap * .5f, Paper, BoardFarPpu);
        }
    }

    // The alcove's picture, centre-cropped into its cells, inset from the outline.
    static void Alcove(RectTransform c, PlanRoom room, float ox, float oy, float cell)
    {
        var a = room.alcove;
        var inset = BoardAlcoveInset + BoardPlanLine * .5f;
        var x = ox + a.xMin * cell + inset;
        var y = oy - a.yMax * cell + inset;
        var w = a.width * cell - 2f * inset;
        var h = a.height * cell - 2f * inset;
        if (w <= 0f || h <= 0f || room.alcovePicture == null) return;
        var go = new GameObject("Alcove picture", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        Place(rt, c, x, y, w, h);
        var image = go.AddComponent<RawImage>();
        image.texture = room.alcovePicture;
        image.raycastTarget = false;
        var tw = Mathf.Max(1f, room.alcovePicture.width);
        var th = Mathf.Max(1f, room.alcovePicture.height);
        var want = w / h;
        var have = tw / th;
        image.uvRect = have > want ? new Rect((1f - want / have) * .5f, 0f, want / have, 1f) : new Rect(0f, (1f - have / want) * .5f, 1f, have / want);
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
        Box(root, "Panel", new Vector3(0f, 0f, thick * Mm * .5f), Quaternion.identity, new Vector3(w * Mm, h * Mm, thick * Mm));
        return NewCanvas(root, new Vector3(0f, 0f, (thick + TextLift) * Mm), Quaternion.Euler(0f, 180f, 0f), w, h);
    }

    // The text canvas: world space, 1 unit = 1 mm, its +Z into the face (the reader looks along it), w x h mm, pivot centred.
    // No CanvasScaler (it would run every frame and reset the canvas whenever a placard is hidden): scale factor 1, so a
    // Text's fontSize is its glyph raster in px, and 1 reference pixel per unit, so the chip ring's 16 px/mm sprite keeps
    // its 3 mm corners.
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
    /// rastered at <paramref name="density"/> px per mm (its job's: the *Ppu constants). Bayon lines go through BayonLine.</summary>
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

    /// <summary>
    /// A Bayon line, its cap in mm, from x on its baseline at <paramref name="density"/>; returns its advance (mm). Every Bayon
    /// line is set here. Bayon has no U+00B7: each " · " is Bayon's own period between two Bayon word spaces, its baseline
    /// BayonDotLift x cap up so its ink sits on the cap middle (20 §1 Setting). The runs and each period are Texts of their own,
    /// left-aligned from their pen origin (FrontRoomsUITextLayout, no tracking), so the line's advance is the sum of theirs.
    /// </summary>
    static float BayonLine(RectTransform c, string name, string s, float cap, float x, float baseline, Color32 colour, float density)
    {
        if (string.IsNullOrEmpty(s)) return 0f;
        var font = Bayon;
        var em = cap / BayonCap;
        var start = x;
        var from = 0;
        var at = s.IndexOf(Interpunct, System.StringComparison.Ordinal);
        if (at >= 0)
        {
            var space = Width(font, " ", em, density);
            for (; at >= 0; at = s.IndexOf(Interpunct, from, System.StringComparison.Ordinal))
            {
                var run = s.Substring(from, at - from);
                Line(c, name, run, font, em, x, baseline, colour, density);
                x += Width(font, run, em, density) + space;
                Line(c, name + " dot", ".", font, em, x, baseline - BayonDotLift * cap, colour, density);
                x += Width(font, ".", em, density) + space;
                from = at + Interpunct.Length;
            }
        }
        var last = from > 0 ? s.Substring(from) : s;
        Line(c, name, last, font, em, x, baseline, colour, density);
        return x + Width(font, last, em, density) - start;
    }

    /// <summary>A code chip (§1): the keyline round the text, padding ChipPad; the text's baseline at `baseline`, the keyline
    /// ChipPad over its cap and under its baseline; returns the chip's width (mm).</summary>
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

    // A part of a placard in the panel's material. No shadow either way (10 §3.5, "no shadow on a placard"): nothing receives
    // one (the renderer's flag, and the material's switch, which is the one URP reads), and nothing casts one, because C
    // stands at a bay's front beside its exhibit and the floor tags and A at the spawn, where a cast shadow would fall on an
    // exhibit or another placard; the slabs on walls and floor are 3-6 mm deep (a hairline shadow, acne-prone) and every
    // caster costs a shadow pass per light on the phones.
    static void Box(Transform parent, string name, Vector3 centre, Quaternion rotation, Vector3 size)
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
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
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
        // A face that could not answer: every width asked for without one is a Plex Mono line (codes, letters, numbers,
        // names) or a Bayon legend name, measured as a monospace advance.
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

    /// <summary>The panel: URP Lit, albedo #161513, smoothness 0, metallic 0 (matte), receiving no shadow, one material for
    /// every placard.</summary>
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
        // URP ignores MeshRenderer.receiveShadows (its SupportedRenderingFeatures turn the flag off); Lit and Simple Lit
        // receive none with _ReceiveShadows 0 and the _RECEIVE_SHADOWS_OFF keyword. That keyword is a shader feature: a player
        // carries its variant only when a built material uses it, else the panel falls back to the plain variant (a shadow on
        // #161513 is about 1 % of a white wall's luminance).
        panelMaterial.SetFloat("_ReceiveShadows", 0f);
        panelMaterial.EnableKeyword("_RECEIVE_SHADOWS_OFF");
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
