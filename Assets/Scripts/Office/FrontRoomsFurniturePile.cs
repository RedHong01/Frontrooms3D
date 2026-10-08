using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The film's furniture piles: intact, recognisable pieces copy-pasted into
/// one place. Nothing is broken; it is placed wrong. Pieces rest in legible,
/// quantised states (upright, on the back, on a side, exactly inverted, or
/// leaning 30-52 degrees on an edge against a neighbour), exact duplicates
/// interpenetrate, and the pile rises toward the ceiling as a landmark in an
/// empty hall.
///
/// Deterministic per seed (a chunk rebuilt later gets the same pile), no
/// runtime physics: an analytic solver over oriented boxes from the kit
/// sidecars. Called by the maze (halls of 4x4+ cells) by reflection and by
/// the look-dev hall. See Documentation/OFFICE_LEVEL_FURNITURE_RESEARCH.md
/// (distortion section) for the reasoning behind every rule.
/// </summary>
public static class FrontRoomsFurniturePile
{
    public enum Rest { Upright, Back, Front, Side, Inverted, EdgeLean }
    public enum Tableau { CentreSculpture, CopyPasteRow, CeilingStuck, OfficeCluster, ZeroPile }

    const float CeilingClearance = .06f;
    const float GlobalOverlapBudget = .22f;

    internal sealed class Piece
    {
        public string asset;
        public FrontRoomsKitLibrary.Info info;
        public string cls;
        public int mass;
        public Rest[] states;
        public float tolerance;
        public string palette;
        public bool topper;
        // Box numbers from the sidecar, worked out once with the same expressions
        // the solver used to re-evaluate on every test.
        public Vector3 centre, half;
        public float Volume, halfMagnitude;

        public void CacheBox()
        {
            centre = (info.Min + info.Max) * .5f;
            half = info.Size * .5f;
            Volume = Mathf.Max(1e-4f, info.Size.x * info.Size.y * info.Size.z);
            halfMagnitude = half.magnitude;
        }

        public bool Allows(Rest r) => Array.IndexOf(states, r) >= 0;
    }

    /// <summary>
    /// A piece where the solver put it. Everything derived from its position and
    /// rotation (box centre, inverse rotation, axes, corners, top, world extent)
    /// is computed on first use with the same expressions as before and kept
    /// until the position or rotation changes, so results are bit-identical.
    /// </summary>
    internal sealed class Placed
    {
        public Piece piece;
        public Rest rest;
        public int copyGroup = -1;   // pieces of one copy-paste run

        Vector3 pos;
        Quaternion rot;
        bool centreValid, rotValid, cornersValid, topValid;
        Vector3 centreCache, axisX, axisY, axisZ, extent;
        Quaternion inverse;
        Vector3[] corners;
        float top;

        /// <summary>Asset origin, pile-local.</summary>
        public Vector3 position
        {
            get => pos;
            set { pos = value; centreValid = cornersValid = topValid = false; }
        }

        public Quaternion rotation
        {
            get => rot;
            set { rot = value; rotValid = centreValid = cornersValid = topValid = false; }
        }

        public Vector3 obbCentre
        {
            get
            {
                if (!centreValid) { centreCache = pos + rot * piece.centre; centreValid = true; }
                return centreCache;
            }
        }

        public Vector3 obbHalf => piece.half;

        void Rot()
        {
            if (rotValid) return;
            inverse = Quaternion.Inverse(rot);
            axisX = rot * Vector3.right;
            axisY = rot * Vector3.up;
            axisZ = rot * Vector3.forward;
            var h = piece.half;
            // World half extent of the box (only for conservative early-outs).
            extent = new Vector3(
                Mathf.Abs(axisX.x) * h.x + Mathf.Abs(axisY.x) * h.y + Mathf.Abs(axisZ.x) * h.z,
                Mathf.Abs(axisX.y) * h.x + Mathf.Abs(axisY.y) * h.y + Mathf.Abs(axisZ.y) * h.z,
                Mathf.Abs(axisX.z) * h.x + Mathf.Abs(axisY.z) * h.y + Mathf.Abs(axisZ.z) * h.z);
            rotValid = true;
        }

        public Quaternion Inverse { get { Rot(); return inverse; } }
        public Vector3 AxisX { get { Rot(); return axisX; } }
        public Vector3 AxisY { get { Rot(); return axisY; } }
        public Vector3 AxisZ { get { Rot(); return axisZ; } }
        /// <summary>Half size of the world-axis box round the piece's box.</summary>
        public Vector3 Extent { get { Rot(); return extent; } }

        /// <summary>The eight box corners (x, y, z = -1/+1, z fastest). Read only.</summary>
        public Vector3[] Corners()
        {
            if (cornersValid) return corners;
            if (corners == null) corners = new Vector3[8];
            var c = obbCentre; var h = obbHalf; var i = 0;
            for (var x = -1; x <= 1; x += 2)
                for (var y = -1; y <= 1; y += 2)
                    for (var z = -1; z <= 1; z += 2)
                        corners[i++] = c + rot * Vector3.Scale(h, new Vector3(x, y, z));
            cornersValid = true;
            return corners;
        }

        /// <summary>Highest corner.</summary>
        public float Top
        {
            get
            {
                if (topValid) return top;
                var max = float.MinValue;
                foreach (var c in Corners()) max = Mathf.Max(max, c.y);
                top = max;
                topValid = true;
                return top;
            }
        }

        public bool Contains(Vector3 p, float pad = 0f)
        {
            var local = Inverse * (p - obbCentre);
            var h = obbHalf;
            return Mathf.Abs(local.x) <= h.x + pad && Mathf.Abs(local.y) <= h.y + pad && Mathf.Abs(local.z) <= h.z + pad;
        }
    }

    internal sealed class Plan
    {
        public Vector3 centre;
        public float radius, ceiling, hMax;
        public readonly List<Placed> placed = new List<Placed>();
        public float totalVolume, overlapVolume;
        public int nextCopyGroup;
    }

    sealed class Rng
    {
        uint state;
        public Rng(int seed) { state = (uint)seed * 747796405u + 2891336453u; if (state == 0) state = 1; }
        public float Next() { state ^= state << 13; state ^= state >> 17; state ^= state << 5; return (state & 0xFFFFFF) / 16777216f; }
        public float Range(float a, float b) => a + (b - a) * Next();
        public int Range(int a, int bExclusive) => a + Mathf.Min(bExclusive - a - 1, (int)(Next() * (bExclusive - a)));
        public bool Chance(float p) => Next() < p;
        public T Pick<T>(IList<T> list) => list[Range(0, list.Count)];
    }

    static List<Piece> library;
    static readonly float[] QuarterYaws = { 0f, 90f, 180f, 270f };
    static readonly float[] RowYawSteps = { 0f, 3f, 7f };
    static readonly float[] RunYawSteps = { 3f, 7f, 15f, 90f };
    static readonly Vector3[] RunAxes = { Vector3.right, Vector3.forward };

    // ===================================================================== API

    /// <summary>Reflection entry point used by FrontRoomsMapWorld (runs <see cref="BeginBuild"/> to completion).</summary>
    public static void Build(Transform parent, Vector3 localCenter, float radius, float ceilingHeight, int seed)
    {
        BuildPile(parent, localCenter, radius, ceilingHeight, seed, null);
    }

    /// <summary>Build and return the pile root (null if the kit has too few pieces).</summary>
    public static GameObject BuildPile(Transform parent, Vector3 localCenter, float radius, float ceilingHeight, int seed, Tableau? force)
    {
        var job = BeginBuildPile(parent, localCenter, radius, ceilingHeight, seed, force);
        job.Complete();
        return job.Root;
    }

    /// <summary>
    /// Time-sliced <see cref="Build"/>: the same pile, solved and spawned a
    /// little per <see cref="FrontRoomsDressJob.Step"/>. Nothing is created until
    /// the layout is solved: the pile root is made then (Build makes it at the
    /// same point, but within one call), a few steps after BeginBuild.
    /// So keep one job at a time per parent and add nothing else to
    /// <paramref name="parent"/> until this job is done (the map: do not begin
    /// the next room, nor run its PlaceProps, under the same chunk). Otherwise
    /// the pile root lands after those children instead of before them. A pile
    /// begun before an office under the same parent and stepped alternately
    /// gives "office dressing ; furniture pile" instead of the synchronous
    /// "furniture pile ; office dressing".
    /// </summary>
    public static FrontRoomsDressJob BeginBuild(Transform parent, Vector3 localCenter, float radius, float ceilingHeight, int seed)
    {
        return BeginBuildPile(parent, localCenter, radius, ceilingHeight, seed, null);
    }

    /// <summary>Time-sliced <see cref="BuildPile"/>; the job's Root is the pile root once it is finished.</summary>
    public static PileJob BeginBuildPile(Transform parent, Vector3 localCenter, float radius, float ceilingHeight, int seed, Tableau? force)
    {
        var job = new PileJob();
        var lib = Library();
        if (parent == null || lib.Count == 0 || radius < .6f || ceilingHeight < 1.8f)
        {
            job.Start(null, null);
            return job;
        }
        job.parent = parent;
        job.localCenter = localCenter;
        var build = new FrontRoomsDressBuild(null);
        build.Watch(parent);
        job.Start(Solve(job, lib, radius, ceilingHeight, seed, force).GetEnumerator(), build);
        return job;
    }

    /// <summary>A pile being solved and built (see <see cref="FrontRoomsDressJob"/>).</summary>
    public sealed class PileJob : FrontRoomsDressJob
    {
        internal Transform parent;
        internal Vector3 localCenter;
        internal Plan plan;
        internal Tableau tableau;

        /// <summary>The pile root once the job is finished (null when nothing was placed).</summary>
        public GameObject Root { get; private set; }

        protected override void OnPlanned()
        {
            if (plan == null || plan.placed.Count == 0) return;
            if (parent == null) { Cancel(); return; }
            var root = new GameObject("furniture pile (" + tableau + ")").transform;
            root.SetParent(parent, false);
            root.localPosition = localCenter;
            Root = root.gameObject;
            build.SetRoot(root);
            build.Watch(root);
            foreach (var p in plan.placed)
            {
                // Small pieces stay walk-through; everything else blocks.
                var blocks = p.piece.cls != "Small" && p.piece.info.Size.magnitude > .5f;
                build.Kit(-1, p.piece.asset, p.position, p.rotation, blocks, p.piece.asset + " / " + p.rest);
            }
            plan = null;
        }
    }

    /// <summary>The solver, one operator call per step.</summary>
    static IEnumerable<bool> Solve(PileJob job, List<Piece> lib, float radius, float ceilingHeight, int seed, Tableau? force)
    {
        var rng = new Rng(seed);
        var tableau = force ?? ChooseTableau(rng, lib, radius, ceilingHeight);
        // Cubicle panels and posts only belong to the pasted-workstation tableau.
        if (tableau != Tableau.OfficeCluster) lib = Filter(lib, p => !p.info.HasTag("office_cluster_only"));
        var plan = new Plan
        {
            centre = Vector3.zero,
            radius = radius,
            ceiling = ceilingHeight,
            hMax = Mathf.Min(ceilingHeight - CeilingClearance, HeightRatio(tableau, rng) * ceilingHeight),
        };
        job.plan = plan;
        job.tableau = tableau;
        IEnumerable<bool> steps = null;
        switch (tableau)
        {
            case Tableau.CentreSculpture: steps = CentreSculpture(plan, lib, rng); break;
            case Tableau.CopyPasteRow: steps = CopyPasteRow(plan, lib, rng); break;
            case Tableau.CeilingStuck: steps = CeilingStuck(plan, lib, rng); break;
            case Tableau.OfficeCluster: steps = OfficeCluster(plan, lib, rng); break;
            case Tableau.ZeroPile: steps = ZeroPile(plan, lib, rng); break;
        }
        if (steps != null)
            foreach (var s in steps) yield return s;
    }

    // ================================================================ library

    static List<Piece> Library()
    {
        if (library != null) return library;
        library = new List<Piece>();
        foreach (var name in FrontRoomsKitLibrary.AllNames())
        {
            var info = FrontRoomsKitLibrary.GetInfo(name);
            if (info == null || !info.IsPilePiece) continue;
            var states = new List<Rest>();
            if (info.pile.states != null)
                foreach (var s in info.pile.states)
                    if (Enum.TryParse(s, out Rest r)) states.Add(r);
            if (states.Count == 0) states.Add(Rest.Upright);
            var piece = new Piece
            {
                asset = name,
                info = info,
                cls = info.pile.cls,
                mass = info.pile.mass,
                states = states.ToArray(),
                tolerance = info.pile.tolerance > 0f ? info.pile.tolerance : .25f,
                palette = info.pile.palette,
                topper = info.pile.topper,
            };
            piece.CacheBox();
            library.Add(piece);
        }
        return library;
    }

    /// <summary>Forget the piece library (tools, after the kit is re-imported).</summary>
    internal static void ResetCachesForTools() => library = null;

    static List<Piece> Filter(List<Piece> lib, Func<Piece, bool> keep)
    {
        var list = new List<Piece>();
        foreach (var p in lib) if (keep(p)) list.Add(p);
        return list;
    }

    static Tableau ChooseTableau(Rng rng, List<Piece> lib, float radius, float ceiling)
    {
        var heavy = Filter(lib, p => p.mass >= 2).Count;
        var seats = Filter(lib, p => p.cls == "Seat").Count;
        var office = Filter(lib, p => p.palette == "office90s").Count;
        // Level 0 halls: mostly the film's centred sculpture. OfficeCluster
        // (pasted workstations) is only built when a caller forces it for an
        // Office zone; on its own it reads as a showroom, not a distortion.
        var roll = rng.Next();
        if (ceiling > 4.5f && roll < .10f) return Tableau.ZeroPile;
        if (heavy >= 2 && lib.Count >= 6 && roll < .72f) return Tableau.CentreSculpture;
        if (seats > 0 && roll < .88f) return Tableau.CopyPasteRow;
        if (ceiling < 3.2f && seats > 0) return Tableau.CeilingStuck;
        return heavy >= 1 ? Tableau.CentreSculpture : Tableau.CopyPasteRow;
    }

    static float HeightRatio(Tableau t, Rng rng)
    {
        switch (t)
        {
            case Tableau.CentreSculpture: return rng.Range(.82f, .95f);
            case Tableau.CeilingStuck: return 1f;
            case Tableau.CopyPasteRow: return rng.Range(.45f, .8f);
            case Tableau.OfficeCluster: return .72f;
            default: return .6f;
        }
    }

    // =============================================================== tableaux
    //
    // Each tableau is a sequence of operator calls; it yields after each one so
    // a PileJob can stop between them. Run to the end, the calls and their
    // random draws happen in exactly the same order as a straight call.

    static IEnumerable<bool> CentreSculpture(Plan plan, List<Piece> lib, Rng rng)
    {
        var bases = Filter(lib, p => p.mass >= 2);
        var mids = Filter(lib, p => p.cls != "Small" && p.cls != "Tall");
        var seatsTables = Filter(lib, p => p.cls == "Seat" || p.cls == "Table");
        var cases = Filter(lib, p => p.Allows(Rest.EdgeLean));
        var talls = Filter(lib, p => p.cls == "Tall");
        var toppers = Filter(lib, p => p.topper);
        var smalls = Filter(lib, p => p.cls == "Small");

        // Counts scale with the radius: the film's piles are a dense mass of
        // furniture, not a few objects (R 3.2 → ~4 bases, ~10 stacked pieces).
        var r = plan.radius;
        for (int i = 0, n = Mathf.Clamp(Mathf.RoundToInt(r * 1.3f), 2, 5); i < n; i++) { foreach (var s in TryBase(plan, bases.Count > 0 ? bases : mids, rng, Ignored)) yield return s; yield return true; }
        for (int i = 0, n = Mathf.Clamp(Mathf.RoundToInt(r * .8f), 1, 3); i < n; i++) { foreach (var s in TryEdgeLean(plan, cases, rng, Ignored)) yield return s; yield return true; }
        for (int i = 0, n = Mathf.Clamp(Mathf.RoundToInt(r * 3.2f), 4, 12); i < n; i++) { foreach (var s in TryStack(plan, mids, rng, null, rng.Chance(.45f), Ignored)) yield return s; yield return true; }
        for (int i = 0, n = rng.Range(2, 5); i < n; i++) { foreach (var s in TryStack(plan, seatsTables, rng, Rest.Inverted, rng.Chance(.3f), Ignored)) yield return s; yield return true; }
        if (seatsTables.Count > 0) { foreach (var s in TryCopyPasteRun(plan, seatsTables, rng, rng.Range(3, 6))) yield return s; yield return true; }
        for (int i = 0, n = Mathf.Clamp(Mathf.RoundToInt(r * 1.5f), 2, 5); i < n; i++) { foreach (var s in TryStack(plan, mids, rng, null, true, Ignored)) yield return s; yield return true; }
        if (talls.Count > 0) { foreach (var s in TrySpike(plan, talls, rng)) yield return s; yield return true; }
        if (toppers.Count > 0) { foreach (var s in TryStack(plan, toppers, rng, null, true, Ignored)) yield return s; yield return true; }
        if (smalls.Count > 0)
            for (int i = 0, n = rng.Range(4, 8); i < n; i++) { foreach (var s in TrySatellite(plan, smalls, rng)) yield return s; yield return true; }
    }

    static IEnumerable<bool> CopyPasteRow(Plan plan, List<Piece> lib, Rng rng)
    {
        var seats = Filter(lib, p => p.cls == "Seat" || p.cls == "Soft" || p.cls == "Table");
        if (seats.Count == 0) seats = Filter(lib, p => p.cls != "Small");
        if (seats.Count == 0) yield break;
        var piece = rng.Pick(seats);
        var yaw = rng.Pick(QuarterYaws);
        var count = Mathf.Clamp(Mathf.RoundToInt(plan.radius * 2f / Mathf.Max(.25f, piece.info.Size.x * .55f)), 5, 14);
        var step = piece.info.Size.x * rng.Range(.3f, .5f);
        var yawStep = rng.Pick(RowYawSteps);
        var start = -step * (count - 1) * .5f;
        var axis = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
        var group = plan.nextCopyGroup++;
        for (var i = 0; i < count; i++)
        {
            var rot = Quaternion.Euler(0f, yaw + yawStep * i, 0f);
            var pos = axis * (start + step * i);
            var placed = Make(piece, Rest.Upright, rot, pos, 0f);
            placed.copyGroup = group;
            TryCommit(plan, placed, ignoreOverlapWithGroup: true);
            yield return true;
        }
        // A second, inverted row pasted on top when it fits.
        if (piece.Allows(Rest.Inverted) && rng.Chance(.5f))
            for (var i = 0; i < count; i += 2)
            {
                var rot = Quaternion.Euler(0f, yaw + yawStep * i + 180f, 0f);
                var pos = axis * (start + step * i);
                var top = TopAt(plan, pos);
                var placed = Make(piece, Rest.Inverted, rot, pos, top);
                placed.copyGroup = group;
                TryCommit(plan, placed, ignoreOverlapWithGroup: true);
                yield return true;
            }
    }

    static IEnumerable<bool> CeilingStuck(Plan plan, List<Piece> lib, Rng rng)
    {
        // One light chair repeated floor to ceiling (the film's chair columns);
        // crates/tables only when no light seat exists.
        var stackable = Filter(lib, p => p.cls == "Seat" && p.mass == 0 && p.info.Height < plan.ceiling * .45f);
        if (stackable.Count == 0) stackable = Filter(lib, p => (p.cls == "Seat" || p.cls == "Crate" || p.cls == "Table") && p.info.Height < plan.ceiling * .45f);
        if (stackable.Count == 0)
        {
            foreach (var s in CentreSculpture(plan, lib, rng)) yield return s;
            yield break;
        }
        var piece = rng.Pick(stackable);
        var before = plan.placed.Count;
        var group = plan.nextCopyGroup++;
        var y = 0f;
        var yaw = rng.Range(0f, 360f);
        for (var i = 0; i < 12; i++)
        {
            var rest = i % 2 == 0 || !piece.Allows(Rest.Inverted) ? Rest.Upright : Rest.Inverted;
            var rot = Quaternion.Euler(0f, yaw + rng.Range(-9f, 9f), 0f);
            var pos = new Vector3(rng.Range(-.06f, .06f), 0f, rng.Range(-.06f, .06f));
            // The column touches the ceiling: the last piece may sink into it.
            var placed = Make(piece, rest, rot, pos, y);
            placed.copyGroup = group;
            var top = Top(placed);
            if (top > plan.ceiling + .12f) break;
            plan.hMax = plan.ceiling + .12f;
            if (!TryCommit(plan, placed, ignoreOverlapWithGroup: true, interlocked: i > 0)) break;
            // Interlock a little: the next copy sits 12% into this one.
            y = top - (top - y) * .12f;
            yield return true;
        }
        if (plan.placed.Count - before < 3)
        {
            // Too short to read as a column: make it a sculpture instead.
            plan.placed.RemoveRange(before, plan.placed.Count - before);
            plan.hMax = Mathf.Min(plan.ceiling - CeilingClearance, .9f * plan.ceiling);
            foreach (var s in CentreSculpture(plan, lib, rng)) yield return s;
            yield break;
        }
        var bases = Filter(lib, p => p.mass >= 2);
        if (bases.Count > 0)
            for (var i = 0; i < 3; i++) { foreach (var s in TryBase(plan, bases, rng, Ignored, .45f)) yield return s; yield return true; }
        var seats = Filter(lib, p => p.cls == "Seat");
        if (seats.Count > 0) { foreach (var s in TryCopyPasteRun(plan, seats, rng, rng.Range(2, 4))) yield return s; yield return true; }
    }

    static IEnumerable<bool> OfficeCluster(Plan plan, List<Piece> lib, Rng rng)
    {
        var office = Filter(lib, p => p.palette == "office90s" && p.cls != "Small");
        if (office.Count == 0)
        {
            foreach (var s in CentreSculpture(plan, lib, rng)) yield return s;
            yield break;
        }
        for (var i = 0; i < 2; i++) { foreach (var s in TryBase(plan, Filter(office, p => p.mass >= 1), rng, Ignored)) yield return s; yield return true; }
        for (var i = 0; i < 2; i++) { foreach (var s in TryEdgeLean(plan, Filter(office, p => p.Allows(Rest.EdgeLean)), rng, Ignored)) yield return s; yield return true; }
        for (var i = 0; i < 3; i++) { foreach (var s in TryStack(plan, office, rng, rng.Chance(.5f) ? Rest.Side : (Rest?)null, false, Ignored)) yield return s; yield return true; }
        foreach (var s in TryCopyPasteRun(plan, office, rng, rng.Range(2, 4))) yield return s;
        yield return true;
    }

    static IEnumerable<bool> ZeroPile(Plan plan, List<Piece> lib, Rng rng)
    {
        var seats = Filter(lib, p => p.cls == "Seat");
        if (seats.Count == 0) seats = Filter(lib, p => p.cls != "Small");
        if (seats.Count == 0) yield break;
        TryCommit(plan, Make(rng.Pick(seats), Rest.Upright, Quaternion.identity, Vector3.zero, 0f));
        yield return true;
    }

    // ============================================================= operators
    //
    // Each operator yields after every attempt (so a PileJob step is one
    // attempt, not up to 22) and reports whether it placed a piece through an
    // Outcome. Run to the end, every attempt and random draw happens in the
    // same order as before.

    sealed class Outcome { public bool placed; }

    static readonly Outcome Ignored = new Outcome();

    static IEnumerable<bool> TryBase(Plan plan, List<Piece> pool, Rng rng, Outcome result, float minR = 0f)
    {
        result.placed = false;
        if (pool.Count == 0) yield break;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var piece = rng.Pick(pool);
            var rest = piece.Allows(Rest.Back) && rng.Chance(.3f) ? Rest.Back : Rest.Upright;
            var r = Mathf.Lerp(minR, .55f, rng.Next()) * plan.radius;
            var a = rng.Range(0f, Mathf.PI * 2f);
            var yaw = rng.Pick(QuarterYaws) + rng.Range(-12f, 12f);
            var placed = Make(piece, rest, Quaternion.Euler(0f, yaw, 0f), new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r), 0f);
            if (TryCommit(plan, placed)) { result.placed = true; yield break; }
            yield return true;
        }
    }

    /// <summary>Stand a case next to a committed piece and tip it onto it about
    /// its bottom edge until first contact, then jam it a few degrees.</summary>
    static IEnumerable<bool> TryEdgeLean(Plan plan, List<Piece> pool, Rng rng, Outcome result)
    {
        result.placed = false;
        if (pool.Count == 0 || plan.placed.Count == 0) yield break;
        for (var attempt = 0; attempt < 12; attempt++)
        {
            var piece = rng.Pick(pool);
            var target = plan.placed[rng.Range(0, plan.placed.Count)];
            if (target.rest == Rest.EdgeLean) continue;
            var a = rng.Range(0f, Mathf.PI * 2f);
            var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            // Face the piece's front or back toward the target so it tips forward/back.
            var yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg + (rng.Chance(.5f) ? 0f : 180f);
            var upright = Quaternion.Euler(0f, yaw, 0f);
            var targetExtent = Extent(target, dir);
            var depthHalf = piece.half.z;
            var origin = target.obbCentre;
            origin.y = 0f;
            var pos = origin + dir * (targetExtent + depthHalf + .03f);
            var stand = Make(piece, Rest.Upright, upright, pos, 0f);
            // Pivot: the stand's bottom edge on the target's side.
            var pivot = stand.obbCentre - dir * depthHalf;
            pivot.y = 0f;
            var axis = Vector3.Cross(Vector3.up, -dir).normalized;
            float contact = -1f;
            for (var deg = 6f; deg <= 58f; deg += 2f)
            {
                var q = Quaternion.AngleAxis(deg, axis);
                var trial = new Placed { piece = piece, rest = Rest.EdgeLean, rotation = q * stand.rotation, position = pivot + q * (stand.position - pivot) };
                if (OverlapFraction(trial, target) > .004f) { contact = deg; break; }
            }
            if (contact < 0f) { yield return true; continue; }
            var angle = contact + rng.Range(2f, 6f);
            if (angle < 30f || angle > 52f) { yield return true; continue; }
            var rot = Quaternion.AngleAxis(angle, axis);
            var lean = new Placed { piece = piece, rest = Rest.EdgeLean, rotation = rot * stand.rotation, position = pivot + rot * (stand.position - pivot) };
            // Keep the hinge edge on the floor.
            Ground(lean, 0f);
            if (TryCommit(plan, lean)) { result.placed = true; yield break; }
            yield return true;
        }
    }

    /// <summary>Rest a piece on top of a committed piece (or the floor).</summary>
    static IEnumerable<bool> TryStack(Plan plan, List<Piece> pool, Rng rng, Rest? want, bool highest, Outcome result)
    {
        result.placed = false;
        if (pool.Count == 0) yield break;
        for (var attempt = 0; attempt < 22; attempt++)
        {
            var piece = rng.Pick(pool);
            var rest = want.HasValue && piece.Allows(want.Value) ? want.Value : PickRest(piece, rng);
            var yaw = rng.Pick(QuarterYaws) + rng.Range(-20f, 20f);
            Placed support = null;
            if (plan.placed.Count > 0)
            {
                support = highest ? Highest(plan) : plan.placed[rng.Range(0, plan.placed.Count)];
                if (support.rest == Rest.EdgeLean || (support.rest == Rest.Inverted && (support.piece.cls == "Seat" || support.piece.cls == "Table"))) support = null;
            }
            Vector3 pos;
            // (The height under pos used to be sampled here too and never used; MakeResting finds it.)
            if (support != null)
            {
                var c = support.obbCentre;
                var spread = Mathf.Min(Extent(support, Vector3.right), Extent(support, Vector3.forward)) * .3f;
                pos = new Vector3(c.x + rng.Range(-spread, spread), 0f, c.z + rng.Range(-spread, spread));
            }
            else
            {
                var r = rng.Range(0f, .55f) * plan.radius;
                var a = rng.Range(0f, Mathf.PI * 2f);
                pos = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            }
            var placed = MakeResting(plan, piece, rest, Quaternion.Euler(0f, yaw, 0f), pos);
            if (TryCommit(plan, placed)) { result.placed = true; yield break; }
            yield return true;
        }
    }

    /// <summary>Paste exact copies of one piece with a rigid offset; they interpenetrate.</summary>
    static IEnumerable<bool> TryCopyPasteRun(Plan plan, List<Piece> pool, Rng rng, int copies)
    {
        if (pool.Count == 0) yield break;
        Placed source = null;
        for (var i = plan.placed.Count - 1; i >= 0 && source == null; i--)
            if (pool.Contains(plan.placed[i].piece) && plan.placed[i].rest != Rest.EdgeLean) source = plan.placed[i];
        if (source == null)
        {
            var stacked = new Outcome();
            foreach (var s in TryStack(plan, pool, rng, null, false, stacked)) yield return s;
            if (!stacked.placed) yield break;
            source = plan.placed[plan.placed.Count - 1];
        }
        var group = source.copyGroup >= 0 ? source.copyGroup : (source.copyGroup = plan.nextCopyGroup++);
        var localAxis = rng.Pick(RunAxes);
        var size = Vector3.Scale(source.piece.info.Size, localAxis).magnitude;
        var step = size * rng.Range(.15f, .45f);
        var yawStep = rng.Pick(RunYawSteps);
        var prev = source;
        for (var i = 0; i < copies; i++)
        {
            var dir = prev.rotation * localAxis;
            dir.y = 0f;
            var pos = prev.position + dir.normalized * step;
            var rot = Quaternion.Euler(0f, yawStep, 0f) * prev.rotation;
            var copy = new Placed { piece = source.piece, rest = source.rest, rotation = rot, position = pos, copyGroup = group };
            // Copies on the floor stay on it; raised copies settle on whatever is below.
            Ground(copy, prev.position.y < .01f ? 0f : TopAt(plan, pos, copy));
            if (!TryCommit(plan, copy, ignoreOverlapWithGroup: true)) break;
            prev = copy;
            yield return true;
        }
    }

    static IEnumerable<bool> TrySpike(Plan plan, List<Piece> pool, Rng rng)
    {
        if (pool.Count == 0 || plan.placed.Count == 0) yield break;
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var piece = rng.Pick(pool);
            var support = Highest(plan);
            var c = support.obbCentre;
            var pos = new Vector3(c.x + rng.Range(-.15f, .15f), 0f, c.z + rng.Range(-.15f, .15f));
            var tilt = Quaternion.Euler(rng.Range(-8f, 8f), rng.Range(0f, 360f), rng.Range(-8f, 8f));
            var placed = MakeResting(plan, piece, Rest.Upright, tilt, pos);
            if (TryCommit(plan, placed)) yield break;
            yield return true;
        }
    }

    static IEnumerable<bool> TrySatellite(Plan plan, List<Piece> pool, Rng rng)
    {
        if (pool.Count == 0) yield break;
        for (var attempt = 0; attempt < 6; attempt++)
        {
            var piece = rng.Pick(pool);
            var r = rng.Range(.7f, .95f) * plan.radius;
            var a = rng.Range(0f, Mathf.PI * 2f);
            var rest = piece.Allows(Rest.Side) && rng.Chance(.3f) ? Rest.Side : Rest.Upright;
            var placed = Make(piece, rest, Quaternion.Euler(0f, rng.Range(0f, 360f), 0f), new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r), 0f);
            if (TryCommit(plan, placed)) yield break;
            yield return true;
        }
    }

    // ================================================================ geometry

    static Rest PickRest(Piece piece, Rng rng)
    {
        var options = new List<Rest>();
        foreach (var s in piece.states) if (s != Rest.EdgeLean) options.Add(s);
        if (options.Count == 0) return Rest.Upright;
        // Upright is the most common reading; the rest are deliberate.
        return rng.Chance(.4f) && options.Contains(Rest.Upright) ? Rest.Upright : rng.Pick(options);
    }

    static Quaternion[] restRotations;

    /// <summary>RestRotationOf, computed once per state (the same Quaternion.Euler results).</summary>
    static Quaternion RestRotation(Rest rest)
    {
        if (restRotations == null)
        {
            var values = (Rest[])Enum.GetValues(typeof(Rest));
            restRotations = new Quaternion[values.Length];
            foreach (var r in values) restRotations[(int)r] = RestRotationOf(r);
        }
        return restRotations[(int)rest];
    }

    static Quaternion RestRotationOf(Rest rest)
    {
        switch (rest)
        {
            case Rest.Back: return Quaternion.Euler(-90f, 0f, 0f);      // back face down, front up
            case Rest.Front: return Quaternion.Euler(90f, 0f, 0f);
            case Rest.Side: return Quaternion.Euler(0f, 0f, 90f);
            case Rest.Inverted: return Quaternion.Euler(180f, 0f, 0f);
            default: return Quaternion.identity;
        }
    }

    static Placed Make(Piece piece, Rest rest, Quaternion yaw, Vector3 position, float floorY)
    {
        var placed = new Placed { piece = piece, rest = rest, rotation = yaw * RestRotation(rest), position = position };
        Ground(placed, floorY);
        return placed;
    }

    /// <summary>
    /// Rest a piece on whatever is under its footprint: sample 3 x 3 points
    /// over the bottom of its box and drop it onto the highest surface found.
    /// </summary>
    static Placed MakeResting(Plan plan, Piece piece, Rest rest, Quaternion yaw, Vector3 position)
    {
        var placed = Make(piece, rest, yaw, position, 0f);
        var top = 0f;
        var samples = BottomSamples(placed);
        Gather(plan, samples, null);
        foreach (var s in samples) top = Mathf.Max(top, TopAtNear(s));
        Ground(placed, top);
        return placed;
    }

    static readonly Vector3[] Samples = new Vector3[9];

    /// <summary>Nine points spread over the lowest face of a piece's box (world xz, y = its bottom).</summary>
    static Vector3[] BottomSamples(Placed p)
    {
        var corners = p.Corners();
        var minY = float.MaxValue;
        foreach (var c in corners) minY = Mathf.Min(minY, c.y);
        // The four lowest corners span the resting face (good enough for quantised states).
        var lowCount = 0;
        foreach (var c in corners) if (c.y < minY + .05f) lowCount++;
        var all = lowCount < 3;
        float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
        foreach (var c in corners)
        {
            if (!all && !(c.y < minY + .05f)) continue;
            x0 = Mathf.Min(x0, c.x); x1 = Mathf.Max(x1, c.x); z0 = Mathf.Min(z0, c.z); z1 = Mathf.Max(z1, c.z);
        }
        var i = 0;
        for (var a = 0; a < 3; a++)
            for (var b = 0; b < 3; b++)
                Samples[i++] = new Vector3(Mathf.Lerp(x0, x1, .15f + .35f * a), minY, Mathf.Lerp(z0, z1, .15f + .35f * b));
        return Samples;
    }

    /// <summary>Fraction of a piece's bottom that touches the floor or a top surface (±3 cm).</summary>
    static float SupportFraction(Plan plan, Placed p)
    {
        var supported = 0;
        var samples = BottomSamples(p);
        var gathered = false;
        foreach (var s in samples)
        {
            if (s.y < .03f) { supported++; continue; }
            if (!gathered) { Gather(plan, samples, p); gathered = true; }
            if (Mathf.Abs(TopAtNear(s) - s.y) < .03f) supported++;
        }
        return supported / 9f;
    }

    /// <summary>Move vertically so the lowest corner sits at y.</summary>
    static void Ground(Placed p, float y)
    {
        var min = float.MaxValue;
        foreach (var c in p.Corners()) min = Mathf.Min(min, c.y);
        p.position += Vector3.up * (y - min);
    }

    static float Top(Placed p) => p.Top;

    static float Extent(Placed p, Vector3 dir)
    {
        var h = p.obbHalf;
        return Mathf.Abs(Vector3.Dot(p.AxisX, dir)) * h.x + Mathf.Abs(Vector3.Dot(p.AxisY, dir)) * h.y + Mathf.Abs(Vector3.Dot(p.AxisZ, dir)) * h.z;
    }

    static Placed Highest(Plan plan)
    {
        Placed best = plan.placed[0];
        var bestTop = Top(best);
        foreach (var p in plan.placed)
        {
            if (p.rest == Rest.EdgeLean) continue;
            var t = Top(p);
            if (t > bestTop) { best = p; bestTop = t; }
        }
        return best;
    }

    // Margins for the early-outs below. They only skip tests that cannot pass:
    // a point inside a box (padded by at most 2 cm along the box's own axes)
    // lies within the box's world-axis extent plus pad·√3 < 3.5 cm, and float
    // error on these metre-scale numbers is under 1e-5 m.
    const float TopAtMargin = .05f;
    const float OverlapMargin = .01f;

    /// <summary>
    /// Height of the highest committed surface under (x, z). An upright piece
    /// offers its authored supports (a desk top, a chair seat) when it has
    /// any; otherwise, and in every other rest state, the top of its box.
    /// </summary>
    static float TopAt(Plan plan, Vector3 xz, Placed ignore = null)
    {
        var y = 0f;
        foreach (var p in plan.placed)
        {
            if (p == ignore || p.rest == Rest.EdgeLean) continue;
            y = TopOf(p, xz, y);
        }
        return y;
    }

    // Pieces that can be under any of a set of sample points (same order as plan.placed).
    static readonly List<Placed> Near = new List<Placed>();

    /// <summary>
    /// Keep, in order, the pieces TopAt could use for any of the samples: a piece
    /// whose widened extent misses the samples' bounding rectangle fails TopAt's
    /// own early-out for every one of them (the extra 1 cm covers rounding).
    /// </summary>
    static void Gather(Plan plan, Vector3[] samples, Placed ignore)
    {
        float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
        foreach (var s in samples) { x0 = Mathf.Min(x0, s.x); x1 = Mathf.Max(x1, s.x); z0 = Mathf.Min(z0, s.z); z1 = Mathf.Max(z1, s.z); }
        const float m = TopAtMargin + .01f;
        Near.Clear();
        foreach (var p in plan.placed)
        {
            if (p == ignore || p.rest == Rest.EdgeLean) continue;
            var c = p.obbCentre;
            var e = p.Extent;
            if (c.x + e.x + m < x0 || c.x - e.x - m > x1 || c.z + e.z + m < z0 || c.z - e.z - m > z1) continue;
            Near.Add(p);
        }
    }

    /// <summary>TopAt over the gathered pieces (same pieces in the same order as TopAt would test).</summary>
    static float TopAtNear(Vector3 xz)
    {
        var y = 0f;
        foreach (var p in Near) y = TopOf(p, xz, y);
        return y;
    }

    /// <summary>One piece's contribution to TopAt: y raised to its surface under xz, if any.</summary>
    static float TopOf(Placed p, Vector3 xz, float y)
    {
        var c = p.obbCentre;
        var e = p.Extent;
        if (Mathf.Abs(xz.x - c.x) > e.x + TopAtMargin || Mathf.Abs(xz.z - c.z) > e.z + TopAtMargin) return y;
        var probe = new Vector3(xz.x, c.y, xz.z);
        if (!p.Contains(probe, .02f)) return y;
        var supports = p.piece.info.supports;
        if (p.rest == Rest.Upright && supports != null && supports.Length > 0)
        {
            var local = p.Inverse * (new Vector3(xz.x, 0f, xz.z) - new Vector3(p.position.x, 0f, p.position.z));
            foreach (var sup in supports)
            {
                if (sup?.centre == null || sup.size == null || sup.centre.Length < 3 || sup.size.Length < 2) continue;
                if (Mathf.Abs(local.x - sup.centre[0]) <= sup.size[0] * .5f + .02f && Mathf.Abs(local.z - sup.centre[2]) <= sup.size[1] * .5f + .02f)
                    y = Mathf.Max(y, p.position.y + sup.centre[1]);
            }
            return y;
        }
        return Mathf.Max(y, Top(p));
    }

    static readonly Vector3[] OverlapSamples = OverlapGrid();

    static Vector3[] OverlapGrid()
    {
        var grid = new Vector3[64];
        var n = 0;
        for (var i = 0; i < 4; i++)
            for (var j = 0; j < 4; j++)
                for (var k = 0; k < 4; k++)
                    grid[n++] = new Vector3((i + .5f) / 4f * 2f - 1f, (j + .5f) / 4f * 2f - 1f, (k + .5f) / 4f * 2f - 1f);
        return grid;
    }

    /// <summary>Fraction of a's box (4x4x4 samples) that lies inside b's box.</summary>
    static float OverlapFraction(Placed a, Placed b)
    {
        var ca = a.obbCentre;
        var cb = b.obbCentre;
        // Exact early-out: boxes whose world-axis extents are apart share no sample.
        var ea = a.Extent;
        var eb = b.Extent;
        if (Mathf.Abs(ca.x - cb.x) > ea.x + eb.x + OverlapMargin
            || Mathf.Abs(ca.y - cb.y) > ea.y + eb.y + OverlapMargin
            || Mathf.Abs(ca.z - cb.z) > ea.z + eb.z + OverlapMargin) return 0f;
        // Cheap reject on bounding spheres (as before).
        if ((ca - cb).magnitude > a.piece.halfMagnitude + b.piece.halfMagnitude) return 0f;
        var inside = 0;
        var h = a.obbHalf;
        var ra = a.rotation;
        foreach (var local in OverlapSamples)
            if (b.Contains(ca + ra * Vector3.Scale(h, local))) inside++;
        return inside / 64f;
    }

    static bool TryCommit(Plan plan, Placed p, bool ignoreOverlapWithGroup = false, bool interlocked = false)
    {
        // Nothing floats: a raised piece must rest on something under at
        // least half its footprint (a third for inverted seats, which sit on
        // their backrest edge and legs-up, as in the film's towers).
        // Interlocked column copies (CeilingStuck) sink into the one below.
        if (p.rest != Rest.EdgeLean && !interlocked)
        {
            var need = p.rest == Rest.Inverted && p.piece.cls == "Seat" ? .33f : .5f;
            if (SupportFraction(plan, p) < need) return false;
        }
        foreach (var c in p.Corners())
        {
            if (new Vector2(c.x, c.z).magnitude > plan.radius) return false;
            if (c.y > plan.hMax) return false;
            if (c.y < -.02f) return false;
        }
        var own = 0f;
        var added = 0f;
        foreach (var q in plan.placed)
        {
            var f = OverlapFraction(p, q);
            if (f <= 0f) continue;
            var sameGroup = p.copyGroup >= 0 && p.copyGroup == q.copyGroup;
            if (f > .8f) return false;                                  // never swallow a piece
            if (sameGroup && ignoreOverlapWithGroup) { if (f > .45f) return false; continue; }
            if (AlmostCoplanar(p, q)) return false;                     // z-fighting guard
            own += f;
            added += f * p.piece.Volume;
        }
        if (own > p.piece.tolerance) return false;
        var total = plan.totalVolume + p.piece.Volume;
        if (plan.overlapVolume + added > GlobalOverlapBudget * total) return false;
        plan.placed.Add(p);
        plan.totalVolume = total;
        plan.overlapVolume += added;
        return true;
    }

    static Vector3 Axis(Placed p, int i) => i == 0 ? p.AxisX : i == 1 ? p.AxisY : p.AxisZ;
    static float HalfAxis(Vector3 h, int i) => i == 0 ? h.x : i == 1 ? h.y : h.z;

    /// <summary>Parallel faces closer than 3 mm with overlapping projections flicker; reject them.</summary>
    static bool AlmostCoplanar(Placed a, Placed b)
    {
        var ha = a.obbHalf;
        var hb = b.obbHalf;
        for (var i = 0; i < 3; i++)
            for (var j = 0; j < 3; j++)
            {
                var n = Axis(a, i);
                var d = Vector3.Dot(n, Axis(b, j));
                if (Mathf.Abs(d) < .9994f) continue; // > 2 degrees apart
                var ca = Vector3.Dot(a.obbCentre, n);
                var cb = Vector3.Dot(b.obbCentre, n);
                for (var sa = -1; sa <= 1; sa += 2)
                    for (var sb = -1; sb <= 1; sb += 2)
                        if (Mathf.Abs((ca + sa * HalfAxis(ha, i)) - (cb + sb * HalfAxis(hb, j))) < .003f) return true;
            }
        return false;
    }
}
