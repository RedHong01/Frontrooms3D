using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// A13 (Red 2026-10-22, research/stairwell/10_directions.md 1.2): the meshes and materials of the EXIT door's two lock
/// states, LOCKED and UNLOCKED, for FrontRoomsExitChain. Owner: the game visual line.
///
/// Every mesh is built once per session from the map's constants and cached here (one mesh per state and level of
/// detail, plus the eye bolt), so every build of the EXIT edge (first build, RebuildChunk, a chunk streaming back,
/// every floor after NextFloor) reuses the same meshes and makes no new mesh or material: nothing leaks and nothing
/// is built per frame. Each state mesh has two sub-meshes: 0 the chain (Prop_ChainOxide), 1 the padlock
/// (Prop_SteelLaminated); the eye bolt has one (Prop_SteelLaminated).
///
/// Frames (metres). LOCKED is authored in the EXIT leaf rig's frame, UNLOCKED and the eye bolt in the door's shut
/// frame on the EXIT frame object; both are the hinge's frame when the leaf is shut: origin the hinge-jamb edge on
/// the wall's centre line at the floor, +Y up, +Z along the opening (hinge 0, latch DoorWidth), +X the push face
/// (N's side) when the door swings +1 (the EXIT does: ExitSwing; FrontRoomsExitChain mirrors the groups for a door
/// built with -1, as the map mirrors the crossbar).
///
/// The geometry is the study's (research/stairwell/data/FrontRoomsStairLookdev.cs.txt, BuildLockStates, the frames
/// measured in 10_directions 5.4) moved onto the map's door: a 1/2 in proof-coil chain wrapped twice round the
/// crossbar 0.09-0.17 m in from its latch case, out to an eye bolt in the wall beside the latch jamb (0.14 m past the
/// opening, 1.0 m up), its two ends joined by a laminated-steel padlock under the bar; dropped, the same length of
/// chain lies coiled on the floor against the wall on the latch side with the padlock open beside it.
/// </summary>
public static class FrontRoomsExitChainKit
{
    // ---------------------------------------------------------------- the decided values (10_directions 1.2, 2.2)

    /// <summary>The chain: dark oxidised proof coil, #34302B (L* 20). The padlock and the eye bolt: laminated steel, #9A9C97.</summary>
    public const string ChainSurface = "Prop_ChainOxide", SteelSurface = "Prop_SteelLaminated";

    /// <summary>1/2 in proof-coil chain as in the study: wire 12.7 mm, link 64 x 44 mm outside, pitch 41 mm.</summary>
    public const float ChainWire = .0127f, ChainLength = .064f, ChainWidth = .044f, ChainPitch = .041f;

    /// <summary>
    /// The crossbar's bar line in the leaf rig: the map's latch case (MountExitCrossbar: the leaf's face, 1.0 m up,
    /// ExitCrossbarLatchInset in from the leaf's latch edge) and the kit's bar_centre anchor, 0.07 m out from the case's
    /// back (Kit_ExitDevice_Crossbar.json). The bar runs 0.8 m from the latch case toward the hinge (hinge_case).
    /// </summary>
    public const float BarOut = .07f, BarLength = .8f;

    /// <summary>The eye bolt: 0.14 m past the latch edge of the opening (the study's -0.64 with its 0.5 m half door), 1.0 m up, on N's wall face.</summary>
    public const float EyeAlong = .14f, EyeHeight = 1f;
    /// <summary>The eye: a ring of 22 mm centre-line radius in 9 mm wire, centred 38 mm off the wall, its plane square to the wall.</summary>
    public const float EyeRing = .022f, EyeWire = .0045f, EyeOut = .038f;

    /// <summary>
    /// The chain's wrap round the bar (32 mm, interact_exit_device.py BAR_R): two turns, from 0.19 to 0.09 m in from
    /// the latch case (clear of its collar at 0.046 m, 50 mm a turn, wider than a link), centre-line radius 35 mm with
    /// the links crossed at 45 degrees to the bar, so no wire enters the bar (1.6 mm clear) or nears the leaf.
    /// </summary>
    public const float WrapFrom = .19f, WrapTo = .09f, WrapRadius = .035f, BarRadius = .016f;

    /// <summary>The shut padlock's centre under the bar (the study's), and its shackle: legs 40 mm apart, 9.6 mm wire.</summary>
    public static readonly Vector3 PadlockAt = new Vector3(.135f, .765f, .98f);
    public const float ShackleHalf = .02f, ShackleWire = .0048f, ShackleTop = .023f;

    public static float LatchZ => ModuleUnits.DoorWidth - ModuleUnits.DoorLeafGap * .5f - FrontRoomsMapWorld.ExitCrossbarLatchInset;
    public static float LeafFace => ModuleUnits.DoorLeafThickness * .5f;
    public static Vector3 BarLatchEnd => new Vector3(LeafFace + BarOut, FrontRoomsMapWorld.ExitCrossbarHeight, LatchZ);
    public static Vector3 EyeAnchor => new Vector3(ModuleUnits.WallHalf, EyeHeight, ModuleUnits.DoorWidth + EyeAlong);
    public static Vector3 EyeCentre => EyeAnchor + new Vector3(EyeOut, 0f, 0f);

    /// <summary>Level-of-detail screen heights (LODGroup, relative): LOD0 for close looks (the unlock shot), LOD1, LOD2 (never culled: the lock reads at 30 m).</summary>
    public static readonly float[] LodHeights = { .12f, .04f, 0f };

    static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
    static Material chain, steel;

    // ---------------------------------------------------------------- materials

    /// <summary>Resources/Surfaces/Prop_ChainOxide (a plain stand-in of the same value when it is missing).</summary>
    public static Material Chain
    {
        get
        {
            if (chain == null) chain = FrontRoomsSurfaces.TryGet(ChainSurface) ?? FrontRoomsSurfaces.Lit("A13 chain (stand-in)", new Color(.204f, .188f, .169f), .35f, .55f);
            return chain;
        }
    }

    /// <summary>Resources/Surfaces/Prop_SteelLaminated (a plain stand-in of the same value when it is missing).</summary>
    public static Material Steel
    {
        get
        {
            if (steel == null) steel = FrontRoomsSurfaces.TryGet(SteelSurface) ?? FrontRoomsSurfaces.Lit("A13 laminated steel (stand-in)", new Color(.604f, .612f, .592f), .55f, .85f);
            return steel;
        }
    }

    /// <summary>Meshes made so far (tools: the cache must not grow with builds).</summary>
    public static int MeshCount => meshes.Count;

    // ---------------------------------------------------------------- the states

    /// <summary>LOCKED, in the leaf rig's frame: the chain round the bar to the eye bolt and back, the padlock shut under the bar.</summary>
    public static Mesh Locked(int lod) => Cached("A13 locked lod" + lod, mb =>
    {
        Links(mb, LockedPath(), lod, LockedPins(), true, false);
        // The padlock hangs from the two end links, its face toward N (+X): its body's +Z turned to +X.
        Padlock(mb, Matrix4x4.TRS(PadlockAt, Quaternion.Euler(0f, 90f, 0f), Vector3.one), false, lod);
    });

    /// <summary>UNLOCKED, in the door's shut frame: the same length of chain coiled on the floor against the wall on the latch side, the padlock open on its back.</summary>
    public static Mesh Unlocked(int lod) => Cached("A13 unlocked lod" + lod, mb =>
    {
        Links(mb, DroppedPath(PathLength(LockedPath())), lod, null, false, true);
        // the padlock open on its back beside the coil, its shackle toward the wall
        Padlock(mb, Matrix4x4.TRS(new Vector3(.32f, .013f, ModuleUnits.DoorWidth + .06f), Quaternion.Euler(0f, 90f, 0f) * Quaternion.Euler(-90f, 0f, 0f), Vector3.one), true, lod);
    });

    /// <summary>The eye bolt in N's wall face (both states), in the door's shut frame, its washer on the wall at EyeAnchor.</summary>
    public static Mesh EyeBolt() => Cached("A13 eye bolt", mb =>
    {
        mb.sub = 1;
        var o = EyeAnchor;
        // a zinc washer plate on the wall, the shank out of it and the eye (its plane square to the wall, along the chain's run)
        mb.Cylinder(o, o + new Vector3(.006f, 0f, 0f), .028f, 20);
        mb.Cylinder(o + new Vector3(.006f, 0f, 0f), o + new Vector3(EyeOut - EyeRing, 0f, 0f), .008f, 12);
        var ring = new List<Vector3>();
        for (var i = 0; i < 20; i++)
        {
            var a = i * Mathf.PI * 2f / 20f;
            ring.Add(EyeCentre + new Vector3(Mathf.Cos(a) * EyeRing, Mathf.Sin(a) * EyeRing, 0f));
        }
        mb.Tube(ring, EyeWire, 10, true);
    });

    // The shut shackle's bow centre in the leaf rig, and a point on the bow's outer radial at an angle from its apex (toward -Z for a > 0).
    static Vector3 BowCentre => PadlockAt + new Vector3(0f, ShackleTop + .006f, 0f);
    static Vector3 BowRadial(float degrees) => new Vector3(0f, Mathf.Cos(degrees * Mathf.Deg2Rad), -Mathf.Sin(degrees * Mathf.Deg2Rad));
    // An end link hooked over the shackle: its inner end round the shackle's wire, its long axis along the bow's radial.
    // (the link's inner end is ChainLength / 2 - ChainWire from its centre; 1 mm of play)
    static float HookReach => ShackleHalf + (ChainLength * .5f - ChainWire - ShackleWire) - .001f;

    /// <summary>
    /// The links that must sit exactly (the rest follow the path): the two end links hooked over the padlock's shackle
    /// at 60 degrees either side of its apex (their planes square to the shackle's wire), and the link threaded on the
    /// eye bolt's ring: the ring's bottom wire through the middle of its opening, its long axis along the chain's run
    /// (plane square to the ring), so its two ends stand either side of the ring for the links before and after it.
    /// </summary>
    public static List<Pin> LockedPins() => new List<Pin>
    {
        new Pin(BowCentre + BowRadial(60f) * HookReach, BowRadial(60f), Vector3.right),
        new Pin(EyeCentre + new Vector3(0f, -EyeRing, 0f), Vector3.forward, Vector3.up),
        new Pin(BowCentre + BowRadial(-60f) * HookReach, BowRadial(-60f), Vector3.right),
    };

    /// <summary>The chain's centre line when LOCKED (leaf rig frame): the study's path on the map's door, through the pinned links.</summary>
    public static List<Vector3> LockedPath()
    {
        var bar = BarLatchEnd;
        var pins = LockedPins();
        var a = pins[0];
        var eye = pins[1].centre;
        var b = pins[2];
        // Clearances (checked offline, W/a13_work/sim/chain_sim.py, and in the clone's A13 proofs): every wire 1 mm or more
        // off the crossbar's bar (32 mm) and its latch case (0.1025 x 0.18 x 0.095 m, door z 0.8525-0.959), the jamb trim,
        // the leaf, the eye's ring, the shackle, the padlock's body and every link but its two neighbours.
        var path = new List<Vector3>
        {
            // from the padlock up toward the hinge, in front of the bar, to the wrap's hinge-side end
            a.centre, a.centre + a.along * ChainPitch, new Vector3(.128f, .885f, .86f), new Vector3(.125f, .92f, .74f),
        };
        // two turns round the bar from WrapFrom back toward the latch case to WrapTo, starting under the bar
        for (var i = 0; i <= 48; i++)
        {
            var t = i / 48f;
            var ang = t * Mathf.PI * 4f;
            path.Add(new Vector3(bar.x + WrapRadius * Mathf.Sin(ang), bar.y - WrapRadius * Mathf.Cos(ang), bar.z - Mathf.Lerp(WrapFrom, WrapTo, t)));
        }
        // out and over the latch case's front, back in past the jamb trim to the eye, through it, and down to the padlock
        path.Add(new Vector3(.13f, .975f, .82f));
        path.Add(new Vector3(.152f, .985f, .86f));
        path.Add(new Vector3(.153f, .99f, .91f));
        path.Add(new Vector3(.15f, .99f, .96f));
        path.Add(new Vector3(.138f, .985f, 1.02f));
        path.Add(new Vector3(.13f, .98f, 1.065f));
        path.Add(eye + new Vector3(0f, 0f, -ChainPitch));
        path.Add(eye);
        path.Add(eye + new Vector3(0f, -.008f, ChainPitch));
        path.Add(new Vector3(.12f, .935f, 1.18f));
        path.Add(new Vector3(.122f, .9f, 1.16f));
        path.Add(new Vector3(.121f, .905f, 1.145f));
        path.Add(new Vector3(.127f, .875f, 1.105f));
        path.Add(b.centre + b.along * ChainPitch * 1.6f);
        path.Add(b.centre + b.along * ChainPitch);
        path.Add(b.centre);
        return path;
    }

    /// <summary>
    /// The chain's centre line when UNLOCKED (shut frame): the same length coiled flat on the floor against the wall on
    /// the latch side, wound outward from a 9 cm eye in 5 cm turns (no link lies in another), its end trailing back to
    /// the latch jamb where it fell from the bar; the links lie crossed at 45 degrees to the floor, centre line 17.5 mm up.
    /// </summary>
    public static List<Vector3> DroppedPath(float length)
    {
        var heap = new List<Vector3>();
        var c = new Vector3(.29f, FloorLinkHeight, ModuleUnits.DoorWidth + .30f);
        const float rin = .09f, pitch = .05f;
        var a = 0f;
        var acc = 0f;
        var prev = Vector3.zero;
        while (true)
        {
            var rad = rin + pitch * a / (2f * Mathf.PI);
            var p = c + new Vector3(Mathf.Cos(a) * rad, 0f, Mathf.Sin(a) * rad * .85f);
            if (heap.Count > 0) acc += (p - prev).magnitude;
            heap.Add(p);
            prev = p;
            if (acc >= length - .13f || heap.Count > 400) break;
            a += 2f * Mathf.PI / 48f;
        }
        heap.Add(prev + new Vector3(-.06f, 0f, -.10f));
        return heap;
    }

    /// <summary>A link lying on the floor crossed 45 degrees to it: its centre line's height.</summary>
    public const float FloorLinkHeight = .0175f;

    public static float PathLength(IList<Vector3> path)
    {
        var length = 0f;
        for (var i = 1; i < path.Count; i++) length += (path[i] - path[i - 1]).magnitude;
        return length;
    }

    /// <summary>Links along a path: one at each end and one every pitch or so between.</summary>
    public static int LinkCount(IList<Vector3> path) => Mathf.Max(2, Mathf.FloorToInt(PathLength(path) / ChainPitch) + 1);

    /// <summary>A link placed exactly: its centre, its long axis, and its width direction (its plane holds both).</summary>
    public struct Pin
    {
        public Vector3 centre, along, width;
        public Pin(Vector3 centre, Vector3 along, Vector3 width)
        {
            this.centre = centre;
            this.along = along.normalized;
            this.width = width.normalized;
        }
    }

    // ---------------------------------------------------------------- building

    static Mesh Cached(string key, System.Action<MB> build)
    {
        if (meshes.TryGetValue(key, out var m) && m != null) return m;
        var mb = new MB();
        build(mb);
        m = mb.ToMesh(key);
        meshes[key] = m;
        return m;
    }

    /// <summary>Each link's centre, long axis and width direction along a path (tools check clearances on these).</summary>
    public static void Frames(IList<Vector3> path, List<Pin> pins, bool wrap, bool floor, out Vector3[] centres, out Vector3[] tangents, out Vector3[] widths)
    {
        var cum = new List<float> { 0f };
        for (var i = 1; i < path.Count; i++) cum.Add(cum[i - 1] + (path[i] - path[i - 1]).magnitude);
        var total = cum[cum.Count - 1];
        var n = LinkCount(path);
        centres = new Vector3[n];
        tangents = new Vector3[n];
        widths = new Vector3[n];
        var u0 = Vector3.zero;
        var bar = BarLatchEnd;
        for (var k = 0; k < n; k++)
        {
            var d = k * total / (n - 1);
            var c = PointAt(path, cum, d);
            // the tangent from a chord a link long, so a link follows the curve it sits on
            var t = (PointAt(path, cum, Mathf.Min(total, d + ChainLength * .5f)) - PointAt(path, cum, Mathf.Max(0f, d - ChainLength * .5f))).normalized;
            if (t.sqrMagnitude < 1e-8f) t = Vector3.forward;
            // a plane carried from link to link (no flips), every other link turned a quarter round the tangent
            var reference = k == 0 ? (Mathf.Abs(Vector3.Dot(t, Vector3.up)) > .9f ? Vector3.forward : Vector3.up) : u0;
            var plane = Vector3.ProjectOnPlane(reference, t).normalized;
            if (plane.sqrMagnitude < 1e-8f) plane = Vector3.ProjectOnPlane(Vector3.right, t).normalized;
            u0 = plane;
            var u = (k % 2 == 0) ? plane : Vector3.Cross(t, plane).normalized;
            if (wrap)
            {
                // round the bar: 45 degrees off the radial, alternately
                var onAxis = new Vector3(bar.x, bar.y, c.z);
                var radial = Vector3.ProjectOnPlane(c - onAxis, t);
                var s = bar.z - c.z;
                if ((c - onAxis).magnitude < WrapRadius + .02f && s > WrapTo - .03f && s < WrapFrom + .03f && radial.sqrMagnitude > 1e-8f)
                    u = Quaternion.AngleAxis(k % 2 == 0 ? 45f : -45f, t) * radial.normalized;
            }
            // on the floor: crossed 45 degrees to it, alternately
            if (floor) u = Quaternion.AngleAxis(k % 2 == 0 ? 45f : -45f, t) * Vector3.ProjectOnPlane(Vector3.up, t).normalized;
            centres[k] = c;
            tangents[k] = t;
            widths[k] = u;
        }
        if (pins != null)
            foreach (var pin in pins)
            {
                var best = 0;
                for (var k = 1; k < n; k++)
                    if ((centres[k] - pin.centre).sqrMagnitude < (centres[best] - pin.centre).sqrMagnitude) best = k;
                centres[best] = pin.centre;
                tangents[best] = pin.along;
                widths[best] = pin.width;
            }
        for (var k = 0; k < n; k++) widths[k] = Vector3.ProjectOnPlane(widths[k], tangents[k]).normalized;
    }

    /// <summary>A link's wire centre line (m points round its stadium): for the clearance checks.</summary>
    public static void LinkCentreLine(Vector3 c, Vector3 t, Vector3 u, int m, List<Vector3> into)
    {
        var rc = (ChainWidth - ChainWire) * .5f;
        var hs = (ChainLength - ChainWidth) * .5f;
        for (var i = 0; i < m; i++)
        {
            var a = i * Mathf.PI * 2f / m;
            var x = Mathf.Cos(a);
            into.Add(c + t * (x * rc + Mathf.Sign(x) * hs) + u * (Mathf.Sin(a) * rc));
        }
    }

    /// <summary>
    /// Links along the path, the first and last at its ends, in alternating planes; each a stadium of wire: LOD0 12 x 6
    /// segments (144 triangles), LOD1 8 x 4 (64), LOD2 6 x 3 (36). Pinned links take their pin; with <paramref name="wrap"/>
    /// the links round the crossbar cross it at 45 degrees (alternately), as a chain pulled round a bar lies.
    /// </summary>
    static void Links(MB mb, IList<Vector3> path, int lod, List<Pin> pins, bool wrap, bool floor)
    {
        mb.sub = 0;
        var along = lod == 0 ? 12 : lod == 1 ? 8 : 6;
        var sides = lod == 0 ? 6 : lod == 1 ? 4 : 3;
        Vector3[] centres, tangents, widths;
        Frames(path, pins, wrap, floor, out centres, out tangents, out widths);
        var n = centres.Length;
        var rc = (ChainWidth - ChainWire) * .5f;
        var hs = (ChainLength - ChainWidth) * .5f;
        var loop = new List<Vector3>(along);
        // The wire's polygon keeps a round wire's mean width (circumradius r pi / (n sin(pi / n))): the coarser levels
        // keep the chain's silhouette, so it covers as many pixels at 12-30 m as the round wire does.
        var wire = ChainWire * .5f * Mathf.PI / (sides * Mathf.Sin(Mathf.PI / sides));
        for (var k = 0; k < n; k++)
        {
            var c = centres[k];
            var t = tangents[k];
            var u = widths[k];
            loop.Clear();
            for (var i = 0; i < along; i++)
            {
                var a = i * Mathf.PI * 2f / along;
                var x = Mathf.Cos(a);
                var y = Mathf.Sin(a);
                loop.Add(c + t * (x * rc + Mathf.Sign(x) * hs) + u * (y * rc));
            }
            mb.Tube(loop, wire, sides, true);
        }
    }

    static Vector3 PointAt(IList<Vector3> path, List<float> cum, float d)
    {
        var i = 0;
        while (i < path.Count - 2 && cum[i + 1] < d) i++;
        return Vector3.Lerp(path[i], path[i + 1], Mathf.InverseLerp(cum[i], cum[i + 1], d));
    }

    /// <summary>
    /// A laminated padlock 52 mm wide, 46 mm tall, 26 mm deep, its face along +Z of <paramref name="m"/>: seven plates
    /// with a groove between each (the period's laminated steel lock, as the HUD padlock 3113:6999), a keyway under
    /// it, and its shackle shut in the body or open (pulled up, the free leg swung out).
    /// </summary>
    static void Padlock(MB mb, Matrix4x4 m, bool open, int lod)
    {
        mb.sub = 1;
        const float w = .052f, h = .046f, d = .026f;
        for (var i = 0; i < 7; i++)
        {
            var y = -h * .5f + (i + .5f) * h / 7f;
            var inset = i % 2 == 0 ? 0f : .002f;
            mb.Box(m, new Vector3(0f, y, 0f), new Vector3(w - inset, h / 7f - .0006f, d - inset));
        }
        mb.Box(m, new Vector3(0f, -h * .5f - .0005f, 0f), new Vector3(.005f, .001f, .011f));
        const float r = ShackleWire, half = ShackleHalf, top = ShackleTop;
        var rise = open ? .028f : .006f;
        var arc = lod == 0 ? 10 : 6;
        var shackle = new List<Vector3> { new Vector3(-half, top - .004f, 0f), new Vector3(-half, top + rise, 0f) };
        for (var i = 1; i < arc; i++)
        {
            var a = Mathf.PI - i * Mathf.PI / arc;
            shackle.Add(new Vector3(Mathf.Cos(a) * half, top + rise + Mathf.Sin(a) * half, 0f));
        }
        shackle.Add(new Vector3(half, top + rise, 0f));
        shackle.Add(new Vector3(half, open ? top + rise - .006f : top - .004f, 0f));
        if (open)
        {
            // turned 120 degrees round the long leg, so the free leg stands out of the body
            var pivot = new Vector3(-half, 0f, 0f);
            var turn = Quaternion.AngleAxis(-120f, Vector3.up);
            for (var i = 2; i < shackle.Count; i++) shackle[i] = turn * (shackle[i] - pivot) + pivot;
        }
        for (var i = 0; i < shackle.Count; i++) shackle[i] = m.MultiplyPoint3x4(shackle[i]);
        mb.Tube(shackle, r, lod == 0 ? 10 : 6, false);
    }

    /// <summary>A small mesh builder with sub-meshes (sub 0 the chain, 1 the padlock and bolt).</summary>
    public sealed class MB
    {
        public readonly List<Vector3> v = new List<Vector3>();
        public readonly List<Vector3> n = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int>[] tri = { new List<int>(), new List<int>() };
        public int sub;

        void Tri(int a, int b, int c, Vector3 normal)
        {
            var t = tri[sub];
            if (Vector3.Dot(Vector3.Cross(v[b] - v[a], v[c] - v[a]), normal) >= 0f) { t.Add(a); t.Add(b); t.Add(c); }
            else { t.Add(a); t.Add(c); t.Add(b); }
        }

        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Vector2 size)
        {
            var i = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            for (var k = 0; k < 4; k++) n.Add(normal);
            uv.Add(Vector2.zero); uv.Add(new Vector2(size.x, 0f)); uv.Add(size); uv.Add(new Vector2(0f, size.y));
            Tri(i, i + 1, i + 2, normal);
            Tri(i, i + 2, i + 3, normal);
        }

        /// <summary>A box (centre, size) in the frame <paramref name="m"/> (rotation and translation only).</summary>
        public void Box(Matrix4x4 m, Vector3 c, Vector3 size)
        {
            var h = size * .5f;
            Vector3 P(float x, float y, float z) => m.MultiplyPoint3x4(c + new Vector3(x * h.x, y * h.y, z * h.z));
            Vector3 N(Vector3 dir) => m.MultiplyVector(dir).normalized;
            Quad(P(1, -1, 1), P(-1, -1, 1), P(-1, 1, 1), P(1, 1, 1), N(Vector3.forward), new Vector2(size.x, size.y));
            Quad(P(-1, -1, -1), P(1, -1, -1), P(1, 1, -1), P(-1, 1, -1), N(Vector3.back), new Vector2(size.x, size.y));
            Quad(P(1, -1, -1), P(1, -1, 1), P(1, 1, 1), P(1, 1, -1), N(Vector3.right), new Vector2(size.z, size.y));
            Quad(P(-1, -1, 1), P(-1, -1, -1), P(-1, 1, -1), P(-1, 1, 1), N(Vector3.left), new Vector2(size.z, size.y));
            Quad(P(-1, 1, -1), P(1, 1, -1), P(1, 1, 1), P(-1, 1, 1), N(Vector3.up), new Vector2(size.x, size.z));
            Quad(P(-1, -1, 1), P(1, -1, 1), P(1, -1, -1), P(-1, -1, -1), N(Vector3.down), new Vector2(size.x, size.z));
        }

        /// <summary>A tube of radius r along a polyline, closed (a ring) or open.</summary>
        public void Tube(IList<Vector3> path, float r, int sides, bool closed)
        {
            var n0 = path.Count;
            if (n0 < 2) return;
            var count = closed ? n0 + 1 : n0;
            var baseIndex = v.Count;
            var prevNormal = Vector3.zero;
            var length = 0f;
            for (var i = 0; i < count; i++)
            {
                var p = path[i % n0];
                Vector3 tangent;
                if (closed) tangent = (path[(i + 1) % n0] - path[(i - 1 + n0) % n0]).normalized;
                else tangent = (path[Mathf.Min(i + 1, n0 - 1)] - path[Mathf.Max(i - 1, 0)]).normalized;
                if (i > 0) length += (p - path[(i - 1) % n0]).magnitude;
                var nrm = i == 0 ? Vector3.Cross(tangent, Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) > .9f ? Vector3.forward : Vector3.up).normalized
                    : Vector3.ProjectOnPlane(prevNormal, tangent).normalized;
                if (nrm.sqrMagnitude < 1e-6f) nrm = Vector3.Cross(tangent, Vector3.right).normalized;
                prevNormal = nrm;
                var bin = Vector3.Cross(tangent, nrm);
                for (var s = 0; s <= sides; s++)
                {
                    var a = s * Mathf.PI * 2f / sides;
                    var dir = nrm * Mathf.Cos(a) + bin * Mathf.Sin(a);
                    v.Add(p + dir * r);
                    n.Add(dir);
                    uv.Add(new Vector2(length, s * Mathf.PI * 2f * r / sides));
                }
            }
            var ring = sides + 1;
            var t = tri[sub];
            for (var i = 0; i < count - 1; i++)
            for (var s = 0; s < sides; s++)
            {
                var a = baseIndex + i * ring + s;
                var b = a + ring;
                // outward-facing (Unity's front face: Cross(v1 - v0, v2 - v0) points out of the tube)
                t.Add(a); t.Add(a + 1); t.Add(b);
                t.Add(b); t.Add(a + 1); t.Add(b + 1);
            }
        }

        /// <summary>A capped cylinder from a to b.</summary>
        public void Cylinder(Vector3 a, Vector3 b, float r, int sides)
        {
            Tube(new List<Vector3> { a, b }, r, sides, false);
            var axis = (b - a).normalized;
            Disc(a, -axis, r, sides);
            Disc(b, axis, r, sides);
        }

        void Disc(Vector3 c, Vector3 normal, float r, int sides)
        {
            var u = Vector3.Cross(normal, Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > .9f ? Vector3.forward : Vector3.up).normalized;
            var w = Vector3.Cross(normal, u);
            var i0 = v.Count;
            v.Add(c); n.Add(normal); uv.Add(Vector2.zero);
            for (var s = 0; s <= sides; s++)
            {
                var a = s * Mathf.PI * 2f / sides;
                v.Add(c + (u * Mathf.Cos(a) + w * Mathf.Sin(a)) * r);
                n.Add(normal);
                uv.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            for (var s = 0; s < sides; s++) Tri(i0, i0 + 1 + s, i0 + 2 + s, normal);
        }

        public Mesh ToMesh(string name)
        {
            var m = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            if (v.Count > 65000) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            if (tri[0].Count == 0)
            {
                // one material only (the eye bolt)
                m.subMeshCount = 1;
                m.SetTriangles(tri[1], 0);
            }
            else
            {
                m.subMeshCount = 2;
                m.SetTriangles(tri[0], 0);
                m.SetTriangles(tri[1], 1);
            }
            m.RecalculateTangents();
            m.RecalculateBounds();
            return m;
        }
    }
}
