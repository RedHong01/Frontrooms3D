// FrontRoomsGlassDebrisLite.cs -- GD3c (gd3c/10_design.md sec 3): the reduced tier of the shatter, for WebGL players
// (FrontRoomsGlassTypeProfile.ReducedTier: the existing ForTier gate; handheld only with ReducedTierOnHandheld, off until a
// device run). Desktop and the Editor never run this (the reference stays the PhysX debris scene, at the highest spec).
//   * No physics scene and no hull cooking: a scripted integrator in the window root's frame (gravity, spin, a bounce of
//     restitution 0.2 on the analytic floor on both sides and on the sill, the wall plane with its opening). A shard that
//     touches down tips toward lying flat, as glass on carpet does.
//   * The WebGL profile's <= 40 bodies are one SkinnedMeshRenderer, one bone per body: 1 draw (WebGL 2 skins on the CPU,
//     about 2k vertices).
//   * The same release waves, settle thresholds (cap 1.5 s), clear zone and compact record as desktop; then teeth and floor
//     glass merge into ONE mesh (<= 2.5k triangles, 1 draw) through the same merge job (bottom teeth last, so the climb snap
//     only shortens the draw range).
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public static class FrontRoomsGlassDebrisLite
{
    public static float Restitution = .2f, Friction = .5f, HardCap = 1.5f, FlattenRate = 10f, Gravity = 9.81f;

    internal sealed class Bodies
    {
        public int count;
        public GameObject go;
        public SkinnedMeshRenderer renderer;
        public Mesh mesh;
        public Transform[] bones;
        public Vector3[] p, v, w, c;
        public Quaternion[] q;
        public bool[] contact;
        public Vector3[][] pts;
        public double stepMsTotal;
        public int steps;
        public int Awake;
    }

    static readonly float SlabY = (float)FrontRoomsGlassFracturePattern.SlabCentreY;

    /// <summary>The per-shard logs the release plan writes into (desktop makes them with the shard objects).</summary>
    internal static void PrepareLogs(FrontRoomsGlassDebrisWorld.Run r)
    {
        r.logs.Clear(); r.quiet.Clear(); r.released.Clear(); r.frozen.Clear(); r.exiting.Clear(); r.lastImpactT.Clear();
        for (var s = 0; s < r.shardK.Count; s++)
        {
            var k = r.shardK[s];
            var pi = r.rangePiece[k];
            var pc = r.f.pieces[pi];
            var area = (float)pc.area;
            r.logs.Add(new FrontRoomsGlassDebrisWorld.ShardLog
            {
                k = k, piece = pi, wave = pc.releaseWave, area = area, mass = Mathf.Max(.002f, area * (float)r.f.slabT * 2500f),
                restRoot = new Vector3((float)pc.centroid.x, (float)pc.centroid.y + SlabY, 0f),
            });
            r.quiet.Add(0f); r.released.Add(false); r.frozen.Add(false); r.exiting.Add(false); r.lastImpactT.Add(-1);
        }
    }

    /// <summary>On the shatter frame: one skinned mesh of every body at the stage-2 pose (pixel-exact with the stage-2 mesh).</summary>
    internal static Bodies Build(FrontRoomsGlassDebrisWorld.Run r)
    {
        var n = r.shardK.Count;
        if (n == 0) return null;
        var b = new Bodies { count = n, p = new Vector3[n], v = new Vector3[n], w = new Vector3[n], c = new Vector3[n], q = new Quaternion[n], contact = new bool[n], pts = new Vector3[n][] };
        var verts = new List<FrontRoomsGlassFractureMeshes.Vertex>(4096);
        var sides = new List<int>(8192);
        var faces = new List<int>(8192);
        var bonesPerVertex = new List<byte>(4096);
        var weights = new List<BoneWeight1>(4096);
        var poly = new List<GlassV2>(32);
        for (var s = 0; s < n; s++)
        {
            var k = r.shardK[s];
            var v0 = r.shardV0[s];
            var vn = r.shardVn[s];
            var baseV = verts.Count;
            for (var i = 0; i < vn; i++)
            {
                var vx = r.srcV[v0 + i];
                vx.position += new Vector3(0f, SlabY, 0f);       // bind pose: the root frame
                verts.Add(vx);
                bonesPerVertex.Add(1);
                weights.Add(new BoneWeight1 { boneIndex = s, weight = 1f });
            }
            for (var i = 0; i < r.rangeSide[k].indexCount; i++) sides.Add(baseV + r.srcI[r.rangeSide[k].indexStart + i] - v0);
            for (var i = 0; i < r.rangeFace[k].indexCount; i++) faces.Add(baseV + r.srcI[r.rangeFace[k].indexStart + i] - v0);
            // the body: its rest centroid at the stage-2 pose, its hull points round it (both caps)
            var log = r.logs[s];
            var cs = FrontRoomsGlassDebrisWorld.StageCentre(r, log);
            b.c[s] = cs;
            b.p[s] = cs + new Vector3(0f, SlabY, 0f);
            b.q[s] = Quaternion.identity;
            r.f.GetPoly(log.piece, poly);
            var h = r.f.slabT * .5;
            var pts = new Vector3[poly.Count * 2];
            for (var cap = 0; cap < 2; cap++)
                for (var i = 0; i < poly.Count; i++)
                {
                    var rest = new FrontRoomsGlassFractureMeshes.Vector3d(poly[i].x, poly[i].y, cap == 0 ? h : -h);
                    var pp = r.f.pieces[log.piece].kind == GlassPieceKind.Shard ? FrontRoomsGlassFractureMeshes.StagePoint(r.f, log.piece, rest) : rest;
                    pts[cap * poly.Count + i] = new Vector3((float)pp.x, (float)pp.y, (float)pp.z) - cs;
                }
            b.pts[s] = pts;
        }
        sides.AddRange(faces);
        var mesh = new Mesh { name = "GD3c reduced shards (skinned)" };
        mesh.SetVertexBufferParams(verts.Count, FrontRoomsGlassFractureMeshes.Layout);
        mesh.SetVertexBufferData(verts, 0, 0, verts.Count);
        mesh.SetIndexBufferParams(sides.Count, IndexFormat.UInt16);
        var ix = new ushort[sides.Count];
        for (var i = 0; i < ix.Length; i++) ix[i] = (ushort)sides[i];
        mesh.SetIndexBufferData(ix, 0, 0, ix.Length);
        mesh.subMeshCount = 1;
        mesh.SetSubMesh(0, new SubMeshDescriptor(0, ix.Length));
        using (var bpv = new NativeArray<byte>(bonesPerVertex.ToArray(), Allocator.Temp))
        using (var bw = new NativeArray<BoneWeight1>(weights.ToArray(), Allocator.Temp))
            mesh.SetBoneWeights(bpv, bw);
        var bind = new Matrix4x4[n];
        var slab = Matrix4x4.Translate(new Vector3(0f, -SlabY, 0f));
        for (var s = 0; s < n; s++) bind[s] = slab;
        mesh.bindposes = bind;
        mesh.RecalculateBounds();
        b.mesh = mesh;
        b.go = new GameObject("GD3c reduced shards");
        b.go.transform.SetParent(r.root, false);
        b.bones = new Transform[n];
        for (var s = 0; s < n; s++)
        {
            var t = new GameObject("body " + s).transform;
            t.SetParent(b.go.transform, false);
            t.localPosition = new Vector3(0f, SlabY, 0f);
            t.localRotation = Quaternion.identity;
            b.bones[s] = t;
        }
        var smr = b.go.AddComponent<SkinnedMeshRenderer>();
        smr.sharedMesh = mesh;
        smr.bones = b.bones;
        smr.rootBone = b.go.transform;
        smr.quality = SkinQuality.Bone1;
        smr.updateWhenOffscreen = false;
        smr.localBounds = new Bounds(new Vector3(0f, 1.6f, 0f), new Vector3(6f, 3.4f, 6.6f));
        smr.sharedMaterial = FrontRoomsGlassBreakable.FractureMaterial;
        smr.shadowCastingMode = ShadowCastingMode.Off;
        smr.receiveShadows = false;
        smr.lightProbeUsage = LightProbeUsage.BlendProbes;
        b.go.AddComponent<FrontRoomsMetalGlassTarget>();
        b.renderer = smr;
        return b;
    }

    // the sill top as the dressed frame builds it (fix r1: the stool's nose reaches past the wall faces; a body used to drop
    // through it to the carpet)
    static float FloorAt(Vector3 p) => Mathf.Abs(p.x) < FrontRoomsGlassVfx.SillNoseX && Mathf.Abs(p.z) < FrontRoomsGlassVfx.SillNoseZ ? FrontRoomsGlassVfx.Sill : 0f;

    /// <summary>One 1/120 s substep of every body (root frame).</summary>
    internal static void Step(FrontRoomsGlassDebrisWorld world, FrontRoomsGlassDebrisWorld.Run r, float dt)
    {
        var b = r.lite;
        if (b == null) return;
        var t0 = FrontRoomsGlassDebrisWorld.ClockMs();
        var awake = 0;
        var capped = r.t >= FrontRoomsShotTimings.GlassBreak.SettleToStatic || r.t >= HardCap;
        var allFrozen = true;
        for (var s = 0; s < b.count; s++)
        {
            var log = r.logs[s];
            if (!r.released[s])
            {
                if (log.releaseT > r.t + 1e-9) { allFrozen = false; continue; }
                r.released[s] = true;
                b.v[s] = log.v0;
                b.w[s] = log.w0;
                world.EmitShedFor(r, s);
            }
            if (r.frozen[s]) continue;
            awake++;
            var v = b.v[s];
            var w = b.w[s];
            var prev = b.p[s];
            v.y -= Gravity * dt;
            v *= 1f - .02f * dt;
            w *= 1f - .15f * dt;
            var p = prev + v * dt;
            var wl = w.magnitude;
            var q = wl > 1e-6f ? Quaternion.AngleAxis(wl * dt * Mathf.Rad2Deg, w / wl) * b.q[s] : b.q[s];
            // the wall slab outside the opening: a body never passes through the wall
            var outside = Mathf.Abs(p.x) >= FrontRoomsGlassVfx.OpeningHalf || p.y < FrontRoomsGlassVfx.Sill || p.y > FrontRoomsGlassVfx.OpeningTop;
            if (outside && Mathf.Abs(p.z) < FrontRoomsGlassVfx.WallHalf && Mathf.Abs(prev.z) >= FrontRoomsGlassVfx.WallHalf - 1e-4f)
            {
                p.z = Mathf.Sign(prev.z) * FrontRoomsGlassVfx.WallHalf;
                v.z = -v.z * Restitution;
            }
            // the floor (and the sill inside the opening): the lowest hull point
            var floorH = FloorAt(p);
            var low = float.MaxValue;
            var pts = b.pts[s];
            for (var i = 0; i < pts.Length; i++) { var y = (q * pts[i]).y; if (y < low) low = y; }
            var bottom = p.y + low;
            var touching = false;
            if (bottom < floorH)
            {
                p.y += floorH - bottom;
                touching = true;
                if (v.y < 0f)
                {
                    var speed = -v.y;
                    v.y = -v.y * Restitution;
                    v.x *= 1f - Friction;
                    v.z *= 1f - Friction;
                    w *= .5f;
                    if (speed > .05f) FrontRoomsGlassDebrisWorld.RecordContact(r, s, r.root.TransformPoint(new Vector3(p.x, floorH, p.z)), floorH > .05f ? FrontRoomsGlassDebris.Surface.Furniture : FrontRoomsGlassDebris.Surface.Carpet, speed);   // fix r1: the sill is the stool (wood), as on desktop, not the carpet (V8 counted it as a landing at 0.023 s)
                }
                // glass on carpet tips toward lying flat
                if (v.magnitude < 1f)
                {
                    var n = q * Vector3.forward;
                    var target = Quaternion.FromToRotation(n, n.y >= 0f ? Vector3.up : Vector3.down) * q;
                    q = Quaternion.Slerp(q, target, Mathf.Min(1f, FlattenRate * dt));
                    w *= .9f;
                }
            }
            b.contact[s] = touching;
            b.p[s] = p;
            b.v[s] = v;
            b.w[s] = w;
            b.q[s] = q;
            var vm = v.magnitude;
            if (vm > log.maxSpeed) log.maxSpeed = vm;
            // V8 counts the room's floor only (fix r1, art #8: a bottom-row body starts millimetres above the sill, and its sill
            // contact at 0.026 s was logged as a floor landing)
            if (touching && log.firstContactT < 0) log.firstContactT = r.t;
            if (touching && floorH < .05f && log.firstFloorT < 0) log.firstFloorT = r.t;
            if (touching && vm < FrontRoomsGlassDebrisWorld.SettleSpeed && w.magnitude < FrontRoomsGlassDebrisWorld.SettleAngular) r.quiet[s] += dt; else r.quiet[s] = 0f;
            var freeze = r.quiet[s] >= FrontRoomsGlassDebrisWorld.SettleQuiet || (capped && vm < FrontRoomsGlassDebrisWorld.CapSpeed && w.magnitude < FrontRoomsGlassDebrisWorld.CapAngular) || r.t >= HardCap;
            if (freeze) { Freeze(r, s); if (capped && r.quiet[s] < FrontRoomsGlassDebrisWorld.SettleQuiet) log.frozenByCap = true; }
            else allFrozen = false;
        }
        b.Awake = awake;
        r.t += dt;
        r.substeps++;
        b.stepMsTotal += FrontRoomsGlassDebrisWorld.ClockMs() - t0;
        b.steps++;
        r.stepMs.Add(FrontRoomsGlassDebrisWorld.ClockMs() - t0);
        r.stepAwake.Add(awake);
        if (allFrozen && (!world.Vfx.Moving(r.group) || r.t >= HardCap))
        {
            r.settledAtT = r.t;
            r.settledByCap = capped;
            r.settleFrame = Time.frameCount;
            world.BeginMerge(r);
        }
    }

    static void Freeze(FrontRoomsGlassDebrisWorld.Run r, int s)
    {
        var b = r.lite;
        b.v[s] = Vector3.zero;
        b.w[s] = Vector3.zero;
        r.frozen[s] = true;
        r.logs[s].settledT = r.t;
    }

    internal static void FreezeAll(FrontRoomsGlassDebrisWorld world, FrontRoomsGlassDebrisWorld.Run r)
    {
        var b = r.lite;
        if (b == null) return;
        for (var s = 0; s < b.count; s++)
        {
            if (!r.released[s]) { r.released[s] = true; }
            if (!r.frozen[s]) { r.logs[s].frozenByCap = true; Freeze(r, s); }
        }
    }

    /// <summary>The bones follow the bodies (once a frame, after the substeps).</summary>
    internal static void Pose(FrontRoomsGlassDebrisWorld.Run r)
    {
        var b = r.lite;
        for (var s = 0; s < b.count; s++)
        {
            b.bones[s].localPosition = b.p[s] - b.q[s] * b.c[s];
            b.bones[s].localRotation = b.q[s];
        }
    }

    // F3 on the reduced tier: a body resting in the clear zone of the opening is laid flat on the floor on its side.
    internal static void ClearTheOpening(FrontRoomsGlassDebrisWorld.Run r)
    {
        var b = r.lite;
        if (b == null) return;
        var mn = FrontRoomsGlassDebrisWorld.ClearMin;
        var mx = FrontRoomsGlassDebrisWorld.ClearMax;
        // fix r1: the dressed frame's parts (stool, nosings, aprons, casings) as boxes: the integrator only knows the sill top, so a
        // body could come to rest in the stool's nose or an apron (6 merged vertices inside the frame in the fix run)
        var fb = r.frameBoxes;
        if (fb.Count == 0 && FrontRoomsGlassDebrisWorld.FrameProxies) FrontRoomsGlassDebrisWorld.FrameBoxes(r.root, fb);
        for (var s = 0; s < b.count; s++)
        {
            var inside = 0;
            var inFrame = false;
            foreach (var pt in b.pts[s])
            {
                var p = b.p[s] + b.q[s] * pt;
                if (p.x > mn.x && p.x < mx.x && p.y > mn.y && p.y < mx.y && p.z > mn.z && p.z < mx.z) inside++;
                if (!inFrame)
                    for (var k = 0; k + 1 < fb.Count; k += 2)
                        if (p.x > fb[k].x + .001f && p.x < fb[k + 1].x - .001f && p.y > fb[k].y + .001f && p.y < fb[k + 1].y - .001f && p.z > fb[k].z + .001f && p.z < fb[k + 1].z - .001f) { inFrame = true; break; }
            }
            if (inside == 0 && !inFrame) continue;
            var pc = r.f.pieces[r.logs[s].piece];
            var side = Mathf.Abs(b.p[s].z) > .002f ? Mathf.Sign(b.p[s].z) : r.farSign;
            var n = b.q[s] * Vector3.forward;
            var flat = Quaternion.FromToRotation(n, n.y >= 0f ? Vector3.up : Vector3.down) * b.q[s];
            b.q[s] = Quaternion.AngleAxis(360f * FrontRoomsGlassDebrisWorld.Rk(r.f.seed, 172, pc.id, 0), Vector3.up) * flat;
            // on the carpet past the stool's nose (0.121 m) on its side
            b.p[s] = new Vector3(Mathf.Clamp(b.p[s].x, -.55f, .55f), .004f, side * (.15f + .20f * FrontRoomsGlassDebrisWorld.Rk(r.f.seed, 171, pc.id, 0)));
            r.logs[s].dropped = true;
            r.droppedFromOpening++;
        }
        Pose(r);
    }

    /// <summary>The merge items of the bodies at their settled poses (no LOD1 on the reduced tier).</summary>
    internal static void Items(FrontRoomsGlassDebrisWorld.Run r, float flatCos)
    {
        var b = r.lite;
        if (b == null) return;
        for (var s = 0; s < b.count; s++)
            FrontRoomsGlassDebrisWorld.AddItem(r, r.shardK[s], Matrix4x4.TRS(b.p[s] - b.q[s] * b.c[s], b.q[s], Vector3.one), flatCos, false);
    }

    internal static void PoseOf(FrontRoomsGlassDebrisWorld.Run r, int s, out Vector3 pos, out Quaternion rot)
    {
        var b = r.lite;
        pos = b != null ? b.p[s] : Vector3.zero;
        rot = b != null ? b.q[s] : Quaternion.identity;
    }

    internal static void Release(FrontRoomsGlassDebrisWorld.Run r)
    {
        var b = r.lite;
        if (b == null) return;
        if (b.go != null) UnityEngine.Object.Destroy(b.go);
        if (b.mesh != null) UnityEngine.Object.Destroy(b.mesh);
        r.lite = null;
    }

    /// <summary>The climb on the reduced tier: the merged mesh draws its bottom teeth last; FloorGlass shortens the range.</summary>
    internal static void HideBottomTeeth(FrontRoomsGlassFloorGlass floor) { }
}
