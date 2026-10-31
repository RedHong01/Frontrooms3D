using System;
using System.Globalization;
using System.Linq;
using UnityEngine;

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
public static partial class FrontRoomsTransitionKit
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

    // ---------- Level transitions production (2026-10-22): Frame on every border, Renovation on 3 in 10 ----------
    // Red, 2026-10-21 ("all as recommended"): a Frame on every zone border (the Office owns every frame and base),
    // Renovation on about 3 in 10 crossings, the colour stays V5. The kits live in the partial files
    // FrontRoomsTransitionKit.{Chunk,Frame,Renovation,Mesh}.cs; the map reaches them through BeginChunk.

    /// <summary>
    /// The Frame and Renovation kits run (the level profile's <c>transitions</c> must be on too).
    /// Off with -transitionsOff (everything off, B0 included, as before) or -transitionKitsOff (the kits only; B0 stays).
    /// </summary>
    public static bool Kits = !HasArg("-transitionsOff") && !HasArg("-transitionKitsOff");

    /// <summary>Tools: Frame only, no crossing is renovated (-transitionFrameOnly).</summary>
    public static bool FrameOnly = HasArg("-transitionFrameOnly");

    /// <summary>Tools: the renovated share instead of the profile's (-renoShare F); negative keeps the profile's.</summary>
    public static float ShareOverride = FloatArg("-renoShare", -1f);

    /// <summary>VL367 fallback: a renovated open edge keeps the pick's bare line instead of the unfinished portal (-transitionBareOpen).</summary>
    public static bool BareRenovatedOpen = HasArg("-transitionBareOpen");

    /// <summary>
    /// The reduced tier: WebGL players (their own track), and iOS / Android players only while the phone tier is off
    /// (<see cref="PhoneTier"/>: the LOW preset, iOS Low Power Mode, or PhoneTierOnHandheld cleared). Coarser tears, no
    /// props, scraps, tape, islands or hanging strips, a straight carpet cut, the base in the trim mesh. The Editor,
    /// Mac and Windows never take it; -transitionReduced forces it in the Editor for measuring only.
    /// </summary>
    public static bool ReducedTier =
#if UNITY_WEBGL && !UNITY_EDITOR
        true;
#else
        HasArg("-transitionReduced");
#endif

    public static bool Reduced
    {
        get
        {
#if UNITY_EDITOR
            return ReducedTier;
#else
            // Mac / Windows players: FrontRoomsHandheld.Active is false, so this is ReducedTier as before; WebGL: true as before.
            return ReducedTier || (FrontRoomsHandheld.Active && !PhoneTier);
#endif
        }
    }

    // ---------- Phone tier (2026-10-31; Red 10-30 on production/20_build.md section 9: option A, the desktop at its highest, ----------
    // ---------- phones as sharp as the frame rate allows; verbatim in production/30_mobile_tier.md) ----------
    // Desktop keeps hero detail and the 3-in-10 share: nothing below runs there (every branch reads PhoneTier, which is false
    // on Mac, Windows and WebGL players and in the Editor outside the handheld preview). iPhone and iPad keep the desktop's
    // layout and share, every piece the desktop draws (props, hanging strips, islands, scraps, tape, the white lip, the carpet
    // fray) and the hero frame, with:
    //  - torn and cut paper, remnants and fray sampled every PhoneStepMm (desktop 4-4.5 mm, the reduced tier 12 mm); the fray
    //    keeps the desktop's random draws, so tiles, chalk and ceiling land where the desktop puts them;
    //  - props at LOD1 from the near plane (LOD0 dropped, as the door break and the ALARM ZONE tell do on phones);
    //  - the base in the block's Cove_Base trim mesh (one renderer fewer per chunk, as the reduced tier);
    //  - the finest kit renderers shown only near the eye (FrontRoomsTransitionNearDetail): the paper's lip and backs, floor
    //    scraps, tape, chalk and cable within 12 m (they cast no shadow, bar the paper); hanging strips, loose carpet tiles,
    //    binder bars and saddles, wall angle, corner guards and galvanised beads within 24 m;
    //  - the renovation textures at their authored size on iOS (2048, ASTC, as imported), not 1024 (FrontRoomsTransitionAssets).
    // The LOW preset and iOS Low Power Mode keep the reduced tier for the chunks built while they are on.

    /// <summary>Handheld players (FrontRoomsHandheld.Active: iOS / Android players, the Editor's handheld preview) take the phone tier.</summary>
    public static bool PhoneTierOnHandheld = true;
    /// <summary>Tools only (never set by the game): build the phone tier in the Editor (FrontRoomsTransitionAssets mode "phone").</summary>
    public static bool DebugForcePhoneTier;
    /// <summary>Lab switch: the phone tier's near-detail rings (FrontRoomsTransitionNearDetail). On in the game.</summary>
    public static bool PhoneNearDetail = true;
    /// <summary>The phone tier's finest sampling step for torn and cut paper, remnants and fray (mm).</summary>
    public const float PhoneStepMm = 8f;

    /// <summary>
    /// The phone tier for the chunk about to be built (read once per chunk). False on WebGL players, on Mac and Windows
    /// players and in the Editor (unless the handheld preview runs or the tools force it); false under the LOW preset or
    /// iOS Low Power Mode, which keep the reduced tier.
    /// </summary>
    public static bool PhoneTier
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return false;
#else
            if (DebugForcePhoneTier) return true;
            if (!PhoneTierOnHandheld || !FrontRoomsHandheld.Active) return false;
            // LOW means the LOW row as picked: a player who started from LOW and turned a detail up (the row reads CUSTOM) gets
            // the phone tier, the same rule as 设置菜单's RESOLUTION AUTO budget.
            return !(FrontRoomsQuality.LowPowerActive || (FrontRoomsQuality.CurrentPreset == FrontRoomsQuality.Preset.Low && !FrontRoomsQuality.IsCustom));
#endif
        }
    }

    /// <summary>The share of border crossings under renovation: the profile's value, or a tool override.</summary>
    public static float RenovationShare(float profileShare) =>
        FrameOnly ? 0f : ShareOverride >= 0f ? Mathf.Clamp01(ShareOverride) : Mathf.Clamp01(profileShare);

    static float FloatArg(string name, float fallback)
    {
        try
        {
            var a = Environment.GetCommandLineArgs();
            for (var i = 0; i + 1 < a.Length; i++)
                if (a[i] == name && float.TryParse(a[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) return v;
        }
        catch { }
        return fallback;
    }
}
