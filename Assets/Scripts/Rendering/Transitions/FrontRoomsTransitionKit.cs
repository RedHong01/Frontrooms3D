using System;
using System.Linq;

/// <summary>
/// Level transitions (pre-render clone, variation "lightlead"): the
/// visual-owned stand-in for the transition kit. Pure functions only; the map
/// calls them from a few hooks marked "// TRANSITION lightlead".
///
/// B0.1 corner ownership. At a cell corner (the 0.16 x 0.16 m post square)
/// where pieces of different face finish meet, no two pieces may overlap in
/// the post square, so no two coplanar faces of different finish z-fight:
///  - a collinear pair (a straight run) owns the post and meets on the cell
///    line, each piece covering its own half;
///  - every perpendicular piece stops at the post face;
///  - at an L corner the piece whose exposed outside face is Office extends
///    over the post and the other stops at the post face;
///  - a free wall end, and every corner of one finish, stay as today.
/// A "face finish" is the face's material plus the ceiling class of the block
/// it is built in (B0.2 builds each face of a height-border wall in its own
/// side's block), so a grime band at a different height counts as a different
/// finish too.
/// </summary>
public static class FrontRoomsTransitionKit
{
    /// <summary>B0 (corner ownership + grime band per side) is on. Off with -transitionsOff or -b0Off (inert check).</summary>
    public static bool B0 = !HasArg("-transitionsOff") && !HasArg("-b0Off");

    public static bool HasArg(string name)
    {
        try { return Environment.GetCommandLineArgs().Contains(name); }
        catch { return false; }
    }

    /// <summary>One wall piece at a post. Side A is west (N-S pieces) or south (E-W pieces).</summary>
    public struct PostPiece
    {
        public bool present;
        public long finishA, finishB;
        public bool officeA, officeB;
    }

    /// <summary>Roles of the four pieces round a post.</summary>
    public const int South = 0, North = 1, West = 2, East = 3;

    /// <summary>
    /// How far each skin of the piece in <paramref name="role"/> runs past the
    /// corner point, in half wall thicknesses: +1 over the whole post (today's
    /// rule), 0 to the cell line, -1 stopping at the post face. Skin A is the
    /// west (N-S pieces) or south (E-W pieces) half.
    ///  - collinear pair: both skins 0 (the run covers the post, each piece its half);
    ///  - perpendicular piece: both skins -1;
    ///  - L corner, owner: outside skin +1 (it wraps the whole outside of the
    ///    post), inside skin -1; the other piece: both skins 0. The inside skins
    ///    then meet in the inside corner, and no end cap shows the inside paper
    ///    on the outside corner.
    /// </summary>
    public static void CornerReach(PostPiece[] p, int role, out int reachA, out int reachB)
    {
        reachA = reachB = 1;
        if (p == null || p.Length != 4 || !p[role].present) return;
        var reach = PieceReach(p, role, out var owner, out var outsideIsA);
        if (owner < 0) { reachA = reachB = reach; return; }
        if (role == owner)
        {
            reachA = outsideIsA ? 1 : -1;
            reachB = outsideIsA ? -1 : 1;
        }
        else reachA = reachB = 0;
    }

    // Whole-piece reach; for an L corner returns owner >= 0 and which skin of the owner faces outside.
    static int PieceReach(PostPiece[] p, int role, out int owner, out bool outsideIsA)
    {
        owner = -1;
        outsideIsA = false;
        // One finish everywhere: unchanged.
        long first = 0;
        var any = false;
        var mixed = false;
        for (var k = 0; k < 4 && !mixed; k++)
        {
            if (!p[k].present) continue;
            foreach (var f in new[] { p[k].finishA, p[k].finishB })
            {
                if (!any) { first = f; any = true; }
                else if (f != first) { mixed = true; break; }
            }
        }
        if (!mixed) return 1;

        var ns = p[South].present && p[North].present;
        var ew = p[West].present && p[East].present;
        var horizontal = role == West || role == East;
        if (ns || ew)
        {
            // The run owns the post (east-west when both pairs stand: a cross).
            var runIsEW = ew;
            return horizontal == runIsEW ? 0 : -1;
        }

        var count = (p[South].present ? 1 : 0) + (p[North].present ? 1 : 0) + (p[West].present ? 1 : 0) + (p[East].present ? 1 : 0);
        // A free wall end: unchanged (the end cap stays split; cut E is not this variation's).
        if (count < 2) return 1;

        // An L: one vertical (S or N) and one horizontal (W or E) piece.
        var v = p[South].present ? South : North;
        var h = p[West].present ? West : East;
        // The outside face of the vertical piece looks away from the horizontal one, and vice versa.
        var vOutsideA = h == East;
        var hOutsideA = v == North;
        var vOutsideOffice = vOutsideA ? p[v].officeA : p[v].officeB;
        var hOutsideOffice = hOutsideA ? p[h].officeA : p[h].officeB;
        if (vOutsideOffice && !hOutsideOffice) owner = v;
        else if (hOutsideOffice && !vOutsideOffice) owner = h;
        else owner = h; // tie (both or neither Office outside): the east-west piece owns the post
        outsideIsA = owner == v ? vOutsideA : hOutsideA;
        return role == owner ? 1 : -1;
    }
}
