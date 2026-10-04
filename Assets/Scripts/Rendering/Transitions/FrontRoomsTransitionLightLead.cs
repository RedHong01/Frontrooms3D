using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;

/// <summary>
/// V5 Light lead (pre-render clone): the light changes first.
///  - Lamp look per theme (the visual chat's numbers; the map carries them in
///    ThemeMaterials): Level 0 warm (1.00, 0.96, 0.88) at 5.0, Office cool white
///    (0.90, 0.96, 1.00) at 6.0 (+9 %), Office lens Troffer_Lens_Cool.
///  - Border rule for Auto lamps (module lamps keep theirs): a Level 0 cell with
///    an Open or Arch edge into Office is dead (mode 3); a Level 0 cell whose
///    only crossings into Office are doors or windows is dim (mode 4); an Office
///    cell with any crossing into Level 0 is steady (mode 0); wall-only contact
///    keeps its roll. Soft (-lightleadSoft): every Level 0 border cell is dim.
/// 1990: cool-white F40 tubes were the office standard; warmer tubes and
/// incandescent were retail and residential.
/// </summary>
public static class FrontRoomsTransitionLightLead
{
    /// <summary>The variation is on. Off with -transitionsOff or -lightleadOff (inert check).</summary>
    public static bool On = !FrontRoomsTransitionKit.HasArg("-transitionsOff") && !FrontRoomsTransitionKit.HasArg("-lightleadOff");
    /// <summary>lightlead_soft: Level 0 border cells dim instead of dead.</summary>
    public static bool Soft = FrontRoomsTransitionKit.HasArg("-lightleadSoft");
    /// <summary>
    /// The provisional N1 pick keeps V5's colour/lens lead on by default. The
    /// stronger dead/dim border rule is opt-in for clone comparison or a
    /// deliberate lightlead run.
    /// </summary>
    public static bool Borders = Soft || FrontRoomsTransitionKit.HasArg("-lightleadBorders");

    public static readonly Color Level0Color = new Color(1f, .96f, .88f);
    public const float Level0Intensity = 5f;
    public static readonly Color OfficeColor = new Color(.90f, .96f, 1f);
    public const float OfficeIntensity = 6f;
    public const string OfficeLensName = "Troffer_Lens_Cool";

    /// <summary>The cool Office lens (Resources/Surfaces/Troffer_Lens_Cool), or null (the map keeps its own).</summary>
    public static Material OfficeLens => On ? FrontRoomsSurfaces.TryGet(OfficeLensName) : null;

    public const int Steady = 0, Dead = 3, Dim = 4;

    /// <summary>
    /// The border rule: the lamp mode for a cell of <paramref name="self"/>
    /// whose four neighbours have these themes and edge kinds, or
    /// <paramref name="rolled"/> when the rule leaves it alone.
    /// A neighbour that is not a crossing (wall, start area) passes kind Wall.
    /// </summary>
    public static int BorderMode(int rolled, ZoneTheme self, ZoneTheme[] themes, EdgeKind[] kinds)
    {
        if (!On) return rolled;
        bool frameless = false, framed = false;
        for (var k = 0; k < 4; k++)
        {
            if (themes[k] == self) continue;
            switch (kinds[k])
            {
                case EdgeKind.Open:
                case EdgeKind.Arch: frameless = true; break;
                case EdgeKind.Door:
                case EdgeKind.Window: framed = true; break;
            }
        }
        if (self == ZoneTheme.Level0)
        {
            if (frameless) return Soft ? Dim : Dead;
            if (framed) return Dim;
            return rolled;
        }
        return frameless || framed ? Steady : rolled;
    }

    // ---------- tools: what the rule did in the last build ----------

    /// <summary>Per built lamp: its cell, theme, the tier roll, and the mode after the rule.</summary>
    public static readonly List<(GridCoord cell, ZoneTheme theme, int rolled, int mode)> Applied = new List<(GridCoord, ZoneTheme, int, int)>();
    public static bool RecordForTools;

    public static void Record(GridCoord cell, ZoneTheme theme, int rolled, int mode)
    {
        if (RecordForTools) Applied.Add((cell, theme, rolled, mode));
    }
}
