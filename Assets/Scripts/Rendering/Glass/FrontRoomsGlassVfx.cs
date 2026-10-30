// FrontRoomsGlassVfx.cs -- GD3 step 7 (plan 10 sec 2.3, 2.4 "Chips, powder, glints, dust", 2.5 rule 4, 4.6; build/06_debris.md):
// the small glass that is not a rigid body: chips knocked off the new cracks on the swap frames, the crushed core falling
// out, powder puffs at the crater, glitter on the sill and at the wall base, crumbs shed by every shard as it lets go.
//
// Why not Unity's Particle System: the project does not include the Particle System module (Packages/manifest.json has no
// com.unity.modules.particlesystem), and adding a module is a project-wide change. These particles are also better as
// they are: every particle is a tiny closed glass prism drawn with the same FrontRooms/Glass material as the stage meshes
// (pane faces + green mirror-like fracture sides, the crushed kind for powder), so the glints are the lamps' real specular
// on tumbling facets, not sprites, and nothing faces one camera (the lab renders four cameras a frame). Fixed 1/120 s
// substeps from a keyed random: the same break gives the same particles, frame for frame.
//
// Collisions are analytic in the window root's frame (the root sits on the floor at the opening centre): the floor (y 0),
// the wall slab (|z| < 0.08 outside the opening), the sill top (y 0.35 inside it) and, until the shatter, the pane itself.
// A particle that comes to rest stays (glitter); at the shatter's settle the debris world merges a window's resting
// particles into its floor-glass mesh.
//
// Counts per tier (plan sec 4.6 "Particles at the shatter": High 250, Cinematic 400, WebGL <= 60): Budget below. The WebGL
// numbers are used only through the profile's tier (FrontRoomsGlassTypeProfile.ForTier's #if UNITY_WEBGL gate).
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class FrontRoomsGlassVfx
{
    public enum Kind : byte { Chip = 0, Crumb = 1, Powder = 2, Glitter = 3 }

    /// <summary>Particle counts per event and tier.</summary>
    public struct Budget
    {
        public int s1Powder, s2Crush, s2Sill, puff, release, backward, teeth;
        public int Shatter => puff + release + backward + teeth;

        public static Budget For(string tier)
        {
            switch (tier)
            {
                case "Cinematic": return new Budget { s1Powder = 24, s2Crush = 18, s2Sill = 16, puff = 48, release = 240, backward = 80, teeth = 32 };   // 400
                case "WebGL": return new Budget { s1Powder = 6, s2Crush = 4, s2Sill = 4, puff = 10, release = 21, backward = 24, teeth = 5 };           // 60 (fix r1: 9 more for the player's side)
                default: return new Budget { s1Powder = 16, s2Crush = 12, s2Sill = 10, puff = 30, release = 150, backward = 50, teeth = 20 };         // 250
            }
        }
    }

    public const int MaxParticles = 2048;
    public const float Substep = 1f / 120f;
    const float Gravity = -9.81f;
    public const float WallHalf = .08f, Sill = .35f, OpeningHalf = .70f, OpeningTop = 2.0f;
    /// <summary>The sill top as the dressed frame builds it (the walnut through-stool's nose reaches Z +-0.121, the steel band
    /// +-0.105; X +-0.80): a particle crossing Sill there lands on it (art r1 #2: it used to fall through the nose).</summary>
    public const float SillNoseZ = .113f, SillNoseX = .80f;

    struct Particle
    {
        public Vector3 p, v, w;          // root-local position, velocity, angular velocity (rad/s)
        public Quaternion q;
        public float size, age;
        public byte shape;
        public Kind kind;
        public bool resting, onFloor, gone;
        public int group;
        public Vector2 paneUV;           // rest pane metres of the spawn point (UV0: grime keys)
    }

    public sealed class Group
    {
        public long edge;
        public Transform root;
        public FrontRoomsMapWorld map;
        public float keyA, keyB;
        public int farSign;              // +1: the glass flies toward the root's +Z
        public bool paneIntact = true;   // the pane is still there (S0-S2): particles cannot cross it
        public uint seed;
        internal GameObject go;
        internal Mesh mesh;
        internal MeshRenderer renderer;
        internal bool dirty;
        internal int alive;
        /// <summary>Live particles of this group (moving or resting, not yet merged).</summary>
        public int Alive => alive;
        // stats
        public int spawnedS1, spawnedS2, spawnedShatter, spawnedTeeth, restingFloor, restingSill, nearWallBaseBreaker, nearWallBaseFar;
    }

    readonly Particle[] parts = new Particle[MaxParticles];
    int count;
    readonly List<Group> groups = new List<Group>();
    public IReadOnlyList<Group> Groups => groups;
    double acc;
    public int Count => count;
    public int Dropped { get; private set; }
    public double LastTickMs { get; private set; }
    public static Func<double> ClockMs = () => System.Diagnostics.Stopwatch.GetTimestamp() * 1000.0 / System.Diagnostics.Stopwatch.Frequency;

    // ---- crumb shapes (unit size, centred; caps = pane faces, sides = fracture faces) -----------------------------------
    const int ShapeCount = 8;
    static Vector3[][] shapePos, shapeNrm;
    static byte[][] shapeKindOf;      // per vertex: 0 face, 1 fracture
    static int[][] shapeTri;

    static void EnsureShapes()
    {
        if (shapePos != null) return;
        shapePos = new Vector3[ShapeCount][];
        shapeNrm = new Vector3[ShapeCount][];
        shapeKindOf = new byte[ShapeCount][];
        shapeTri = new int[ShapeCount][];
        for (var s = 0; s < ShapeCount; s++)
        {
            var n = 3 + s % 3;
            var thick = s < 4 ? .22f : .5f;          // slivers, then chunky crumbs
            var ring = new Vector2[n];
            for (var i = 0; i < n; i++)
            {
                var a = (i + .35f * (float)(FrontRoomsGlassFractureMeshes.Rand01(1990u, 71, s, i) - .5)) / n * Mathf.PI * 2f;
                var r = .5f * (.7f + .3f * (float)FrontRoomsGlassFractureMeshes.Rand01(1990u, 72, s, i));
                ring[i] = new Vector2(Mathf.Cos(a) * r * (s < 4 ? 1.6f : 1f), Mathf.Sin(a) * r);
            }
            var pos = new List<Vector3>();
            var nrm = new List<Vector3>();
            var kind = new List<byte>();
            var tri = new List<int>();
            for (var cap = 0; cap < 2; cap++)
            {
                var z = cap == 0 ? thick * .5f : -thick * .5f;
                var b = pos.Count;
                for (var i = 0; i < n; i++) { pos.Add(new Vector3(ring[i].x, ring[i].y, z)); nrm.Add(new Vector3(0, 0, cap == 0 ? 1 : -1)); kind.Add(0); }
                for (var i = 1; i + 1 < n; i++)
                {
                    // Unity's front face is the one cross(b - a, c - a) points to: +z cap counter-clockwise in x/y
                    if (cap == 0) { tri.Add(b); tri.Add(b + i); tri.Add(b + i + 1); }
                    else { tri.Add(b); tri.Add(b + i + 1); tri.Add(b + i); }
                }
            }
            for (var i = 0; i < n; i++)
            {
                var a = ring[i];
                var c = ring[(i + 1) % n];
                var e = c - a;
                var o = new Vector3(e.y, -e.x, 0).normalized;
                var b = pos.Count;
                pos.Add(new Vector3(a.x, a.y, -thick * .5f)); pos.Add(new Vector3(c.x, c.y, -thick * .5f));
                pos.Add(new Vector3(c.x, c.y, thick * .5f)); pos.Add(new Vector3(a.x, a.y, thick * .5f));
                for (var k = 0; k < 4; k++) { nrm.Add(o); kind.Add(1); }
                // outward winding: cross(b1 - b0, b2 - b0) = outward normal o
                tri.Add(b); tri.Add(b + 1); tri.Add(b + 2);
                tri.Add(b); tri.Add(b + 2); tri.Add(b + 3);
            }
            shapePos[s] = pos.ToArray();
            shapeNrm[s] = nrm.ToArray();
            shapeKindOf[s] = kind.ToArray();
            shapeTri[s] = tri.ToArray();
        }
    }

    public static int ShapeTris(int s) { EnsureShapes(); return shapeTri[s].Length / 3; }
    public static int ShapeVerts(int s) { EnsureShapes(); return shapePos[s].Length; }

    // ---- groups ------------------------------------------------------------------------------------------------------------
    public Group GroupFor(FrontRoomsGlassBreakable glass)
    {
        var root = glass.transform;
        foreach (var g in groups) if (g.root == root) return g;
        var edge = glass.Window != null ? glass.Window.edge : 0L;
        FrontRoomsGlassFractureMeshes.PaneKeys(Matrix4x4.TRS(root.TransformPoint(new Vector3(0f, (float)FrontRoomsGlassFracturePattern.SlabCentreY, 0f)), root.rotation, Vector3.one), out var ka, out var kb);
        var grp = new Group
        {
            edge = edge, root = root, map = glass.Map, keyA = ka, keyB = kb, farSign = glass.Record.side > 0 ? 1 : -1,
            seed = (uint)glass.Record.seed, paneIntact = glass.Stage < 3,
        };
        groups.Add(grp);
        return grp;
    }

    public Group Find(Transform root)
    {
        foreach (var g in groups) if (g.root == root) return g;
        return null;
    }

    // ---- emitters (all positions root-local metres; the slab centre is (0, 1.175, 0)) ----------------------------------------
    static float R(uint seed, int salt, int a, int b) => (float)FrontRoomsGlassFractureMeshes.Rand01(seed, salt, a, b);

    int Add(Group g, Kind kind, Vector3 p, Vector3 v, float size, int key)
    {
        if (count >= MaxParticles) { Dropped++; return -1; }
        var gi = groups.IndexOf(g);
        if (gi < 0) { groups.Add(g); gi = groups.Count - 1; }   // a released group used again (its window emits once more)
        var s = g.seed;
        var i = count++;
        var axis = new Vector3(R(s, 81, key, 0) - .5f, R(s, 81, key, 1) - .5f, R(s, 81, key, 2) - .5f).normalized;
        parts[i] = new Particle
        {
            p = p, v = v, w = axis * Mathf.Lerp(6f, 30f, R(s, 82, key, 0)) * (kind == Kind.Powder ? .3f : 1f),
            q = Quaternion.Euler(R(s, 83, key, 0) * 360f, R(s, 83, key, 1) * 360f, R(s, 83, key, 2) * 360f),
            size = size, shape = (byte)(kind == Kind.Powder ? 4 + key % 4 : key % ShapeCount), kind = kind, group = gi,
            paneUV = new Vector2(p.x, p.y - (float)FrontRoomsGlassFracturePattern.SlabCentreY),
        };
        g.alive++;
        g.dirty = true;
        return i;
    }

    static float SizeOf(Kind k, float u)
    {
        switch (k)
        {
            case Kind.Chip: return Mathf.Lerp(.003f, .007f, u);
            case Kind.Crumb: return Mathf.Lerp(.002f, .005f, u);
            case Kind.Powder: return Mathf.Lerp(.0006f, .0015f, u);
            default: return Mathf.Lerp(.001f, .0025f, u);
        }
    }

    /// <summary>Crack1 / Crack2 chips on the new cracks (the generator's chips1 / chips2, slab coordinates), knocked out of the
    /// struck face toward the breaker; on the swap frame (plan sec 2.5 rule 4).</summary>
    public int EmitChips(Group g, List<GlassV2> slabPoints, int stage)
    {
        var n0 = count;
        var s = g.seed;
        var y0 = (float)FrontRoomsGlassFracturePattern.SlabCentreY;
        var face = -g.farSign * .0035f;                       // just off the struck face
        for (var i = 0; i < slabPoints.Count; i++)
        {
            var k = stage * 1000 + i;
            var p = new Vector3((float)slabPoints[i].x, (float)slabPoints[i].y + y0, face);
            var out_ = -g.farSign * Mathf.Lerp(.25f, .9f, R(s, 91, k, 0));
            var v = new Vector3((R(s, 91, k, 1) - .5f) * .5f, Mathf.Lerp(-.1f, .35f, R(s, 91, k, 2)), out_);
            Add(g, Kind.Chip, p, v, SizeOf(Kind.Chip, R(s, 92, k, 0)), k);
        }
        var n = count - n0;
        if (stage == 1) g.spawnedS1 += n; else g.spawnedS2 += n;
        return n;
    }

    /// <summary>A puff of crushed-glass powder at the crater (slab impact point), mostly toward the breaker.</summary>
    public int EmitPowder(Group g, GlassV2 impactSlab, double craterRadius, int n, int salt, float speed, int stage)
    {
        var n0 = count;
        var s = g.seed;
        var y0 = (float)FrontRoomsGlassFracturePattern.SlabCentreY;
        var rad = (float)Math.Max(.004, craterRadius);
        for (var i = 0; i < n; i++)
        {
            var k = salt * 1000 + i;
            var a = R(s, 101, k, 0) * Mathf.PI * 2f;
            var rr = rad * Mathf.Sqrt(R(s, 101, k, 1));
            var back = R(s, 101, k, 2) < .8f ? -1 : 1;      // 80 % toward the breaker
            var p = new Vector3((float)impactSlab.x + Mathf.Cos(a) * rr, (float)impactSlab.y + y0 + Mathf.Sin(a) * rr, back * g.farSign * .0035f);
            var dir = new Vector3(Mathf.Cos(a) * .6f, Mathf.Sin(a) * .6f + .15f, back * g.farSign * 1f).normalized;
            var v = dir * speed * Mathf.Lerp(.3f, 1f, R(s, 102, k, 0));
            Add(g, Kind.Powder, p, v, SizeOf(Kind.Powder, R(s, 103, k, 0)), k);
        }
        var m = count - n0;
        if (stage == 1) g.spawnedS1 += m; else if (stage == 2) g.spawnedS2 += m; else g.spawnedShatter += m;
        return m;
    }

    /// <summary>Crack2: the crushed core falls out of the hole (crumbs from the crush polygons) and glitter settles on the sill.</summary>
    public int EmitCrack2(Group g, FrontRoomsGlassFracture f, Budget b)
    {
        var n0 = count;
        var s = g.seed;
        var y0 = (float)FrontRoomsGlassFracturePattern.SlabCentreY;
        var crush = 0;
        for (var pi = 0; pi < f.pieces.Count && crush < b.s2Crush; pi++)
        {
            var pc = f.pieces[pi];
            if (pc.kind != GlassPieceKind.Crush) continue;
            var each = Math.Max(1, Math.Min(3, b.s2Crush / 4));
            for (var j = 0; j < each && crush < b.s2Crush; j++, crush++)
            {
                var k = 2000 + crush;
                var c = pc.centroid;
                var side = R(s, 111, k, 0) < .55f ? g.farSign : -g.farSign;
                var p = new Vector3((float)c.x + (R(s, 111, k, 1) - .5f) * .006f, (float)c.y + y0 + (R(s, 111, k, 2) - .5f) * .006f, side * .002f);
                var v = new Vector3((R(s, 112, k, 0) - .5f) * .2f, -.05f, side * Mathf.Lerp(.05f, .35f, R(s, 112, k, 1)));
                Add(g, Kind.Crumb, p, v, SizeOf(Kind.Crumb, R(s, 113, k, 0)), k);
            }
        }
        // glitter on the sill: tiny bits off the bottom edge, both faces
        for (var i = 0; i < b.s2Sill; i++)
        {
            var k = 2500 + i;
            var side = i % 2 == 0 ? -g.farSign : g.farSign;
            var p = new Vector3(Mathf.Lerp(-.62f, .62f, R(s, 114, k, 0)), Mathf.Lerp(.372f, .40f, R(s, 114, k, 1)), side * Mathf.Lerp(.004f, .012f, R(s, 114, k, 2)));
            var v = new Vector3((R(s, 115, k, 0) - .5f) * .05f, -.1f, side * Mathf.Lerp(0f, .06f, R(s, 115, k, 1)));
            Add(g, Kind.Glitter, p, v, SizeOf(Kind.Glitter, R(s, 116, k, 0)), k);
        }
        var n = count - n0;
        g.spawnedS2 += n;
        return n;
    }

    /// <summary>The shatter's backward glitter: fine bits off the pane that fall back toward the breaker and land at the wall base
    /// on the player's side (86 % of backward fragments land directly below the frame, [RCMP91]). art r1 (V10): fast enough
    /// toward the breaker that about half clear the stool's nose and reach the carpet at the wall base (before: almost all came to
    /// rest on the sill, none at the wall base).</summary>
    public int EmitBackwardGlitter(Group g, int n)
    {
        var n0 = count;
        var s = g.seed;
        for (var i = 0; i < n; i++)
        {
            var k = 3000 + i;
            var p = new Vector3(Mathf.Lerp(-.62f, .62f, R(s, 121, k, 0)), Mathf.Lerp(.45f, 1.6f, R(s, 121, k, 1)), -g.farSign * .004f);
            var v = new Vector3((R(s, 122, k, 0) - .5f) * .3f, Mathf.Lerp(-.05f, .35f, R(s, 122, k, 1)), -g.farSign * Mathf.Lerp(.18f, .65f, R(s, 122, k, 2)));
            Add(g, Kind.Glitter, p, v, SizeOf(Kind.Glitter, R(s, 123, k, 0)), k);
        }
        var m = count - n0;
        g.spawnedShatter += m;
        return m;
    }

    /// <summary>Crumbs a shard sheds as it lets go: from a point on its outline (root-local), with its velocity.</summary>
    public int EmitShed(Group g, Vector3 pLocal, Vector3 vLocal, int n, int key)
    {
        var n0 = count;
        var s = g.seed;
        for (var i = 0; i < n; i++)
        {
            var k = 4000 + key * 4 + i;
            var jitter = new Vector3(R(s, 131, k, 0) - .5f, R(s, 131, k, 1) - .5f, R(s, 131, k, 2) - .5f) * .6f;
            var v = vLocal * Mathf.Lerp(.5f, 1.1f, R(s, 132, k, 0)) + jitter;
            Add(g, Kind.Crumb, pLocal, v, SizeOf(Kind.Crumb, R(s, 133, k, 0)), k);
        }
        var m = count - n0;
        g.spawnedShatter += m;
        return m;
    }

    /// <summary>Crumbs off the tooth edges at the shatter, or when the bottom teeth snap (climb).</summary>
    public int EmitTeethCrumbs(Group g, List<Vector3> pointsLocal, int n, int salt)
    {
        var n0 = count;
        var s = g.seed;
        if (pointsLocal.Count == 0) return 0;
        for (var i = 0; i < n; i++)
        {
            var k = salt * 1000 + i;
            var p = pointsLocal[(int)(R(s, 141, k, 0) * pointsLocal.Count) % pointsLocal.Count];
            var side = R(s, 141, k, 1) < .5f ? 1 : -1;
            p.z += side * .004f;
            var v = new Vector3((R(s, 142, k, 0) - .5f) * .2f, Mathf.Lerp(-.2f, .1f, R(s, 142, k, 1)), side * Mathf.Lerp(.02f, .25f, R(s, 142, k, 2)));
            Add(g, R(s, 143, k, 0) < .5f ? Kind.Crumb : Kind.Glitter, p, v, SizeOf(Kind.Crumb, R(s, 143, k, 1)), k);
        }
        var m = count - n0;
        g.spawnedTeeth += m;
        return m;
    }

    // ---- simulation --------------------------------------------------------------------------------------------------------
    static float Drag(Kind k) => k == Kind.Powder ? 3.5f : k == Kind.Glitter ? 1.2f : .4f;

    /// <summary>Advance by dt (fixed 1/120 s substeps), then rebuild the meshes of groups that moved.</summary>
    public void Tick(float dt)
    {
        var t0 = ClockMs();
        // groups whose window went away (chunk dropped): their particles, mesh and object go too, and the group leaves the list
        // (eng r1 #4: a group whose particles had all been merged was kept for good, one object and one mesh per window touched)
        for (var gi = groups.Count - 1; gi >= 0; gi--)
            if (groups[gi].root == null) RemoveGroupAt(gi);
        acc += dt;
        var steps = 0;
        while (acc >= Substep - 1e-9 && steps < 8)
        {
            acc -= Substep;
            steps++;
            for (var i = 0; i < count; i++)
            {
                if (parts[i].resting || parts[i].gone) continue;
                Step(ref parts[i], groups[parts[i].group], Substep);
                groups[parts[i].group].dirty = true;
            }
        }
        if (steps >= 8) acc = 0;
        Compact();
        for (var gi = 0; gi < groups.Count; gi++)
            if (groups[gi].dirty) Rebuild(gi);
        LastTickMs = ClockMs() - t0;
    }

    static void Step(ref Particle q, Group g, float h)
    {
        var k = Drag(q.kind);
        q.age += h;
        q.v.y += Gravity * h * (q.kind == Kind.Powder ? .5f : 1f);   // powder: terminal speed about 1.4 m/s
        q.v *= 1f / (1f + k * h);
        var prev = q.p;
        q.p += q.v * h;
        var wlen = q.w.magnitude;
        if (wlen > 1e-4f) q.q = Quaternion.AngleAxis(wlen * h * Mathf.Rad2Deg, q.w / wlen) * q.q;
        q.w *= 1f / (1f + 2f * h);
        var half = q.size * .25f;
        var inOpening = Mathf.Abs(q.p.x) < OpeningHalf && q.p.y > Sill && q.p.y < OpeningTop;
        // the pane (S0-S2): a particle may not cross z = 0 inside the opening
        if (g.paneIntact && Mathf.Abs(q.p.x) < OpeningHalf && q.p.y > Sill && q.p.y < OpeningTop && Mathf.Sign(prev.z) != Mathf.Sign(q.p.z) && prev.z != 0f)
        {
            q.p.z = Mathf.Sign(prev.z) * .0031f;
            q.v.z = -q.v.z * .2f;
        }
        // the stool's nose past the wall faces: land on it
        if (Mathf.Abs(q.p.z) >= WallHalf && Mathf.Abs(q.p.z) < SillNoseZ + half && Mathf.Abs(q.p.x) < SillNoseX && prev.y >= Sill + half - 1e-4f && q.p.y < Sill + half)
        {
            q.p.y = Sill + half;
            Land(ref q, false);
            return;
        }
        // the wall slab outside the opening
        if (Mathf.Abs(q.p.z) < WallHalf + half && !inOpening)
        {
            var onSillTop = Mathf.Abs(q.p.x) < OpeningHalf && prev.y >= Sill + half - 1e-4f && q.p.y < Sill + half;
            if (onSillTop)
            {
                q.p.y = Sill + half;
                Land(ref q, false);
                return;
            }
            var s = Mathf.Abs(prev.z) >= WallHalf ? Mathf.Sign(prev.z) : (q.p.z >= 0 ? 1f : -1f);
            q.p.z = s * (WallHalf + half);
            q.v.z = -q.v.z * .25f;
            q.v.x *= .7f; q.v.y *= .7f;
        }
        // the floor
        if (q.p.y < half)
        {
            q.p.y = half;
            Land(ref q, true);
        }
    }

    static void Land(ref Particle q, bool floor)
    {
        if (Mathf.Abs(q.v.y) > .35f && q.kind != Kind.Powder)
        {
            q.v.y = -q.v.y * .18f;       // a small hop on carpet
            q.v.x *= .35f; q.v.z *= .35f;
            q.w *= .5f;
            return;
        }
        q.v = Vector3.zero;
        q.w = Vector3.zero;
        q.resting = true;
        q.onFloor = floor;
        // lie flat: the thin axis up, a keyed yaw
        var yaw = (q.q.eulerAngles.y);
        q.q = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(90f, 0f, 0f);
    }

    void Compact()
    {
        var w = 0;
        for (var i = 0; i < count; i++)
        {
            if (parts[i].gone) { var g = groups[parts[i].group]; g.alive--; g.dirty = true; continue; }
            if (w != i) parts[w] = parts[i];
            w++;
        }
        count = w;
    }

    // Clear a group and take it out of the list; particles of later groups keep pointing at their own group.
    void RemoveGroupAt(int gi)
    {
        ClearGroup(gi);
        groups.RemoveAt(gi);
        for (var i = 0; i < count; i++) if (parts[i].group > gi) parts[i].group--;
    }

    public int GroupCount => groups.Count;

    void ClearGroup(int gi)
    {
        for (var i = 0; i < count; i++) if (parts[i].group == gi) parts[i].gone = true;
        Compact();
        var g = groups[gi];
        g.alive = 0;
        if (g.go != null) UnityEngine.Object.Destroy(g.go);
        if (g.mesh != null) UnityEngine.Object.Destroy(g.mesh);
        g.go = null; g.mesh = null; g.renderer = null;
    }

    /// <summary>True while any particle of this group still moves.</summary>
    public bool Moving(Group g)
    {
        var gi = groups.IndexOf(g);
        for (var i = 0; i < count; i++) if (parts[i].group == gi && !parts[i].resting && !parts[i].gone) return true;
        return false;
    }

    /// <summary>The group's resting particles (root-local), removed from the live set: the floor-glass merge takes them. Particles
    /// still moving stay live.</summary>
    public void TakeResting(Group g, List<FrontRoomsGlassDebris.RestingCrumb> dst)
    {
        var gi = groups.IndexOf(g);
        g.restingFloor = g.restingSill = g.nearWallBaseBreaker = g.nearWallBaseFar = 0;
        for (var i = 0; i < count; i++)
        {
            if (parts[i].group != gi || !parts[i].resting || parts[i].gone) continue;
            var q = parts[i];
            dst.Add(new FrontRoomsGlassDebris.RestingCrumb { position = q.p, rotation = q.q, size = q.size, shape = q.shape, kind = (byte)q.kind, paneUV = q.paneUV });
            if (q.onFloor) g.restingFloor++; else g.restingSill++;
            if (q.onFloor && Mathf.Abs(q.p.z) - WallHalf < .30f) { if (Mathf.Sign(q.p.z) == -g.farSign) g.nearWallBaseBreaker++; else g.nearWallBaseFar++; }
            parts[i].gone = true;
        }
        Compact();
    }

    /// <summary>Counts for logs: (moving, resting on the floor, resting on a sill, resting within 0.3 m of the wall base on the
    /// breaker's side, the same on the far side).</summary>
    public (int moving, int floor, int sill, int wallBaseBreaker, int wallBaseFar) Census(Group g)
    {
        var gi = groups.IndexOf(g);
        int mv = 0, fl = 0, si = 0, wb = 0, wf = 0;
        for (var i = 0; i < count; i++)
        {
            if (parts[i].group != gi || parts[i].gone) continue;
            if (!parts[i].resting) { mv++; continue; }
            if (parts[i].onFloor) fl++; else si++;
            if (parts[i].onFloor && Mathf.Abs(parts[i].p.z) - WallHalf < .30f) { if (Mathf.Sign(parts[i].p.z) == -g.farSign) wb++; else wf++; }
        }
        return (mv, fl, si, wb, wf);
    }

    /// <summary>Lab: every particle of a group (root-local position, kind, resting, on floor) for logs.</summary>
    public void Dump(Group g, List<(Vector3 p, Kind kind, bool resting, bool floor, float size)> dst)
    {
        var gi = groups.IndexOf(g);
        for (var i = 0; i < count; i++)
            if (parts[i].group == gi && !parts[i].gone) dst.Add((parts[i].p, parts[i].kind, parts[i].resting, parts[i].onFloor, parts[i].size));
    }

    // ---- drawing -----------------------------------------------------------------------------------------------------------
    readonly List<FrontRoomsGlassFractureMeshes.Vertex> verts = new List<FrontRoomsGlassFractureMeshes.Vertex>(8192);
    readonly List<int> idx = new List<int>(16384);

    void Rebuild(int gi)
    {
        var g = groups[gi];
        g.dirty = false;
        if (g.root == null) return;
        verts.Clear();
        idx.Clear();
        for (var i = 0; i < count; i++)
            if (parts[i].group == gi && !parts[i].gone) AppendCrumb(verts, idx, parts[i].p, parts[i].q, parts[i].size, parts[i].shape, (byte)parts[i].kind, parts[i].paneUV, g.keyA, g.keyB, i);
        if (g.go == null && verts.Count == 0) return;
        if (g.go == null)
        {
            g.go = new GameObject("GD3 glass particles " + g.root.name);
            FrontRoomsGlassDebrisWorld.Adopt(g.go);
            g.mesh = new Mesh { name = "GD3 glass particles" };
            g.mesh.MarkDynamic();
            g.go.AddComponent<MeshFilter>().sharedMesh = g.mesh;
            g.renderer = g.go.AddComponent<MeshRenderer>();
            g.renderer.sharedMaterial = FrontRoomsGlassBreakable.FractureMaterial;
            g.renderer.shadowCastingMode = ShadowCastingMode.Off;
            g.renderer.receiveShadows = false;
            g.renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
        }
        g.go.transform.SetPositionAndRotation(g.root.position, g.root.rotation);
        Upload(g.mesh, verts, idx);
        g.renderer.enabled = verts.Count > 0;
    }

    /// <summary>One crumb as a closed prism (root-local or any frame): pane faces kind 0 (powder: crushed 2), sides kind 1.</summary>
    public static void AppendCrumb(List<FrontRoomsGlassFractureMeshes.Vertex> v, List<int> ix, Vector3 p, Quaternion q, float size, byte shape, byte kind, Vector2 paneUV, float keyA, float keyB, int salt)
    {
        EnsureShapes();
        var sp = shapePos[shape % ShapeCount];
        var sn = shapeNrm[shape % ShapeCount];
        var sk = shapeKindOf[shape % ShapeCount];
        var st = shapeTri[shape % ShapeCount];
        var b = v.Count;
        var g = (byte)(40 + (salt * 37) % 215);
        var paneCode = FrontRoomsGlassDebrisJobs.OctEncode24(q * Vector3.forward);   // floor mode: the crumb's own pane frame
        for (var k = 0; k < sp.Length; k++)
        {
            var faceKind = kind == (byte)Kind.Powder ? FrontRoomsGlassFractureMeshes.KindCrushed : sk[k] == 1 ? FrontRoomsGlassFractureMeshes.KindFracture : FrontRoomsGlassFractureMeshes.KindFace;
            v.Add(new FrontRoomsGlassFractureMeshes.Vertex
            {
                position = p + q * (sp[k] * size),
                normal = q * sn[k],
                color = new Color32(sk[k] == 1 ? (byte)255 : (byte)0, g, 128, 255),
                uv0 = paneUV,
                uv1 = new Vector4(keyA, keyB, paneCode, faceKind),
            });
        }
        for (var k = 0; k < st.Length; k++) ix.Add(b + st[k]);
    }

    static void Upload(Mesh mesh, List<FrontRoomsGlassFractureMeshes.Vertex> v, List<int> ix)
    {
        mesh.Clear();
        const MeshUpdateFlags flags = MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers;
        mesh.SetVertexBufferParams(v.Count, FrontRoomsGlassFractureMeshes.Layout);
        if (v.Count > 0) mesh.SetVertexBufferData(v, 0, 0, v.Count, 0, flags | MeshUpdateFlags.DontRecalculateBounds);
        mesh.SetIndexBufferParams(ix.Count, IndexFormat.UInt32);
        if (ix.Count > 0) mesh.SetIndexBufferData(ix, 0, 0, ix.Count, flags | MeshUpdateFlags.DontRecalculateBounds);
        mesh.subMeshCount = 1;
        mesh.SetSubMesh(0, new SubMeshDescriptor(0, ix.Count), flags);
        mesh.RecalculateBounds();
    }

    public void Clear()
    {
        for (var gi = 0; gi < groups.Count; gi++) ClearGroup(gi);
        groups.Clear();
        count = 0;
    }

    /// <summary>Forget a group's particles, renderer and mesh, and take it out of the list (its window's glass is merged, its chunk
    /// went, or the lab's take ended).</summary>
    public void Release(Group g)
    {
        var gi = groups.IndexOf(g);
        if (gi >= 0) RemoveGroupAt(gi);
    }
}
