using System;
using FrontRooms.Map;
using UnityEngine;

/// <summary>
/// A key's look (10_spec §4.1, D1.6 10_design §4.3): the tag's shape and tint and the token typed on its insert. The
/// same identity drives the key's tag, the number plates on the door and (when it converts) the HUD's key icon.
/// - The token is the FLOOR's (Red 10-22 via the graphic-visual line, hud_build DECISIONS #28 / #32 / #33): "00" on LEVEL 0 (the
///   title's floor, FrontRooms3DGame floor 1), then "B1", "B2", ... below it; from B10 on the token is set at 0.8x on
///   the same baseline (the atlas cell carries that). The floor follows the game's own events (MapRunStarted = floor 1,
///   FloorChanged = the new floor), so a key built during NextFloor before the event is refreshed when it fires
///   (FloorTokenChanged).
/// - Shape and tint: Stub(zone), THE SAME hash as the HUD's FrontRoomsKeyHud.StubIdentity
///   (h = (uint)(x * 73856093 ^ y * 19349663) * 2654435761u; shape (h/100)%3, tint (h/300)%3), until the map's key
///   rule lands (contract M6, A13's floor key).
/// Atlas (Prop_KeyTagNo_A, 10 x 10 cells, row 0 at the top): cells 0-24 "00".."24" (key-rack and key-cabinet hook
/// labels use 01-24), cells 25-98 "B1".."B74", cell 99 blank paper (the held-type state, ShowKitType off).
/// - The desk fob (station_labels 2026-10-31; 30L S11, plan 70 P06, 平面视觉's lettering spec 10-31 01:2x): the station's
///   floor key (FrontRoomsMapWorld.FloorKeyZone) is Kit_KeyTag_Long_White at DeskFobScale with STAIR A typed on its
///   insert; the floor token stays on the HUD tag only. STAIR A is not one of the 100 cells: its band (Tools/lookdev/
///   gen_keytag_atlas.py) sits on the row 8 / row 9 boundary of column 9, in the cells' blank margins that no tag, plate
///   or label crop reaches, so the tag shows it through DeskFobST (a half-row offset, no scale). The HUD's stub
///   (FrontRoomsKeyHud.StubIdentity) answers the same Long / White for that zone, so the 3D key still equals the HUD glyph.
/// </summary>
[Serializable]
public struct FrontRoomsKeyIdentity : IEquatable<FrontRoomsKeyIdentity>
{
    public enum Shape { Rect, Round, Long }
    public enum Tint { Red, Blue, White }

    /// <summary>Atlas cells (Prop_KeyTagNo_A).</summary>
    public const int FirstBCell = 25, LastBCell = 98, BlankCell = 99;

    public Shape shape;
    public Tint tint;
    /// <summary>The floor whose token the tag shows: 1 = LEVEL 0 (the title's floor), 2 = LEVEL B1, ... (FrontRooms3DGame.FloorLabel's count).</summary>
    public int floor;
    /// <summary>The station's desk fob (DeskFob): its tag is drawn at DeskFobScale and prints STAIR A (DeskFobST), never the floor token.</summary>
    public bool deskFob;

    /// <summary>The desk fob's tag scale at rest (114 x 48 mm, 30L S11). In the hand and in a lock it is the tag's own size, so
    /// the KP-1 swap to the HUD glyph and the unlock shot keep their measured footprint (FrontRoomsKeyAssembly).</summary>
    public const float DeskFobScale = 1.5f;

    /// <summary>_BaseMap_ST that shows the STAIR A band on a tag insert shipped on cell 00: column 9, centred on the row 8 / 9 boundary.</summary>
    public static readonly Vector4 DeskFobST = new Vector4(1f, 1f, .9f, -.85f);

    /// <summary>What the desk fob prints (叙事 signed, research/narrative/80; for tools and transcripts).</summary>
    public const string DeskFobText = "STAIR A";

    /// <summary>The floor key is the desk fob (shipped on). Tools may turn it off: the floor key is then the stub hash's tag again.</summary>
    public const bool DeskFobShipped = true;
    public static bool DeskFobOn = DeskFobShipped;

    public FrontRoomsKeyIdentity(Shape shape, Tint tint, int floor)
    {
        this.shape = shape;
        this.tint = tint;
        this.floor = Mathf.Max(1, floor);
        deskFob = false;
    }

    /// <summary>The desk fob's identity on a floor: Long, White, STAIR A.</summary>
    public static FrontRoomsKeyIdentity DeskFob(int floor) => new FrontRoomsKeyIdentity(Shape.Long, Tint.White, floor) { deskFob = true };

    /// <summary>True for the floor key's zone while the desk fob is on (the 3D key and the HUD's stub both ask this).</summary>
    public static bool IsDeskFobZone(GridCoord zone) => DeskFobOn && zone == FrontRoomsMapWorld.FloorKeyZone;

    /// <summary>The stand-in identity of a zone's key on the current floor (shape / tint: the HUD's stub hash; the floor
    /// key: the desk fob).</summary>
    public static FrontRoomsKeyIdentity Stub(GridCoord zone)
    {
        if (IsDeskFobZone(zone)) return DeskFob(CurrentFloor);
        unchecked
        {
            var h = (uint)(zone.x * 73856093 ^ zone.y * 19349663) * 2654435761u;
            return new FrontRoomsKeyIdentity((Shape)((h / 100u) % 3u), (Tint)((h / 300u) % 3u), CurrentFloor);
        }
    }

    /// <summary>The tag kit: Kit_KeyTag_{Rect,Round,Long}{,_Blue,_White}.</summary>
    public string TagKit => "Kit_KeyTag_" + shape + (tint == Tint.Blue ? "_Blue" : tint == Tint.White ? "_White" : string.Empty);

    /// <summary>The token as the tag prints it: "00", "B1", ... (DECISIONS #33).</summary>
    public string Token => TokenOf(floor);

    /// <summary>Kept for tools: the printed token.</summary>
    public string Label => Token;

    /// <summary>The atlas cell of this identity's token.</summary>
    public int Cell => TokenCell(floor);

    public static string TokenOf(int floor) => floor <= 1 ? "00" : "B" + (floor - 1);

    /// <summary>The Prop_KeyTagNo_A cell of a floor's token (floors deeper than B74 show B74's cell).</summary>
    public static int TokenCell(int floor) => floor <= 1 ? 0 : Mathf.Clamp(FirstBCell - 1 + (floor - 1), FirstBCell, LastBCell);

    /// <summary>What an atlas cell prints ("" for the blank cell).</summary>
    public static string CellText(int cell) => cell < FirstBCell ? cell.ToString("00") : cell <= LastBCell ? "B" + (cell - FirstBCell + 1) : string.Empty;

    // ---------------------------------------------------------------- the run's floor

    /// <summary>The floor the game is on now (1 outside a run, in edit mode and in tools).</summary>
    public static int CurrentFloor { get; private set; } = 1;

    /// <summary>Raised when the floor (and so every tag and plate token) changes; the kit facade re-sets live tags and plates.</summary>
    public static event Action<int> FloorTokenChanged;

    /// <summary>Set the run's floor (the game's events do this; tools and tests may too).</summary>
    public static void SetFloor(int floor)
    {
        floor = Mathf.Max(1, floor);
        if (floor == CurrentFloor) return;
        CurrentFloor = floor;
        FloorTokenChanged?.Invoke(floor);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void HookGame()
    {
        CurrentFloor = 1;
        FrontRooms3DGame.MapRunStarted -= OnRunStarted;
        FrontRooms3DGame.MapRunStarted += OnRunStarted;
        FrontRooms3DGame.FloorChanged -= OnFloorChanged;
        FrontRooms3DGame.FloorChanged += OnFloorChanged;
    }

    static void OnRunStarted(FrontRoomsMapWorld map, FrontRoomsMapHunter hunter) => SetFloor(1);
    static void OnFloorChanged(FrontRoomsMapWorld map, FrontRoomsMapHunter hunter, int floor) => SetFloor(floor);

    public bool Equals(FrontRoomsKeyIdentity o) => shape == o.shape && tint == o.tint && floor == o.floor && deskFob == o.deskFob;
    public override bool Equals(object o) => o is FrontRoomsKeyIdentity k && Equals(k);
    public override int GetHashCode() => ((int)shape * 3 + (int)tint) * 1000 + floor + (deskFob ? 500000 : 0);
    public override string ToString() => shape + "/" + tint + "/" + Token + (deskFob ? "/" + DeskFobText : string.Empty);
}
