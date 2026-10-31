using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Level transitions (production, 2026-10-22): the swept-profile mesh writer shared by the Frame and
/// Renovation kits. Render-only geometry in chunk-local metres. From the 10-08 frame kit
/// (Documentation/research/level_transitions/code/pick), unchanged except the non-finite guard.
/// </summary>
public static partial class FrontRoomsTransitionKit
{
    public static bool Finite(Vector3 v) => !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));

    // ---------- geometry ----------

    public sealed class TransitionMesh
    {
        public readonly List<Vector3> vertices = new List<Vector3>();
        public readonly List<Vector3> normals = new List<Vector3>();
        public readonly List<Vector2> uvs = new List<Vector2>();
        public readonly List<int> triangles = new List<int>();
        public int TriangleCount => triangles.Count / 3;
        /// <summary>Triangles dropped for a non-finite corner since the process started (tools report it; 0 expected).</summary>
        public static int NonFinite;

        public int Add(Vector3 p, Vector3 n, Vector2 uv)
        {
            vertices.Add(p);
            normals.Add(n);
            uvs.Add(uv);
            return vertices.Count - 1;
        }

        /// <summary>A triangle wound to face the side its vertex normals point to (clockwise seen from the front, as Unity draws).</summary>
        public void Tri(int a, int b, int c)
        {
            // Every mesh writer drops a triangle with a non-finite corner (the renovation run's guard).
            if (!Finite(vertices[a]) || !Finite(vertices[b]) || !Finite(vertices[c])) { NonFinite++; return; }
            var va = vertices[a];
            var face = Vector3.Cross(vertices[b] - va, vertices[c] - va);
            if (face.sqrMagnitude < 1e-16f) return;
            var n = normals[a] + normals[b] + normals[c];
            if (Vector3.Dot(face, n) < 0f) { var t = b; b = c; c = t; }
            triangles.Add(a); triangles.Add(b); triangles.Add(c);
        }

        public void Quad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal)
        {
            // UVs in metres on the quad's own plane (mesh-UV materials).
            var t = (p1 - p0).normalized;
            var bt = Vector3.Cross(normal, t);
            Vector2 U(Vector3 p) => new Vector2(Vector3.Dot(p, t), Vector3.Dot(p, bt));
            var i0 = Add(p0, normal, U(p0));
            var i1 = Add(p1, normal, U(p1));
            var i2 = Add(p2, normal, U(p2));
            var i3 = Add(p3, normal, U(p3));
            Tri(i0, i1, i2);
            Tri(i0, i2, i3);
        }

        /// <summary>tangents: only mesh-UV materials (_FR_MESH_UV) read them; world-projected ones build their own frame.</summary>
        public Mesh ToMesh(string name, bool tangents = true)
        {
            var mesh = new Mesh { name = name };
            if (vertices.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            if (tangents) mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    /// <summary>
    /// A 2D section in (s, w): s runs along the sweep's outward vector, w along
    /// its fixed axis A. Strips are smooth inside and hard between each other.
    /// </summary>
    public sealed class Profile
    {
        public readonly List<List<Vector2>> points = new List<List<Vector2>>();
        public readonly List<List<Vector2>> normals = new List<List<Vector2>>();
        /// <summary>Closed outline for end caps (optional).</summary>
        public readonly List<Vector2> outline = new List<Vector2>();

        public void Line(Vector2 a, Vector2 b, Vector2 n)
        {
            points.Add(new List<Vector2> { a, b });
            normals.Add(new List<Vector2> { n.normalized, n.normalized });
        }

        /// <summary>Arc round <paramref name="c"/>, radius r, whose outward normal turns from n0 to n1 (shortest way).</summary>
        public void Arc(Vector2 c, float r, Vector2 n0, Vector2 n1, int segments)
        {
            var a0 = Mathf.Atan2(n0.y, n0.x);
            var a1 = Mathf.Atan2(n1.y, n1.x);
            var d = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            var p = new List<Vector2>();
            var n = new List<Vector2>();
            for (var k = 0; k <= segments; k++)
            {
                var a = a0 + d * k / segments;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                p.Add(c + dir * r);
                n.Add(dir);
            }
            points.Add(p);
            normals.Add(n);
        }

        /// <summary>
        /// A polyline with filleted corners. The outside (air) lies to the left of
        /// travel, so each flat's normal is its direction turned 90 degrees counter-clockwise.
        /// radii[k] is the fillet at point k (0 = sharp).
        /// </summary>
        public static Profile Rounded(IList<Vector2> poly, IList<float> radii, bool closed, int arcSegments = 3)
        {
            var pr = new Profile();
            var n = poly.Count;
            Vector2 Ccw(Vector2 d) => new Vector2(-d.y, d.x);
            // Tangent points per corner.
            var tin = new Vector2[n];
            var tout = new Vector2[n];
            var hasArc = new bool[n];
            for (var k = 0; k < n; k++)
            {
                tin[k] = tout[k] = poly[k];
                var interior = closed || (k > 0 && k < n - 1);
                if (!interior || radii[k] <= 0f) continue;
                var prev = poly[(k - 1 + n) % n];
                var next = poly[(k + 1) % n];
                var d1 = (poly[k] - prev).normalized;
                var d2 = (next - poly[k]).normalized;
                var turn = Mathf.Acos(Mathf.Clamp(Vector2.Dot(d1, d2), -1f, 1f));
                if (turn < 1e-3f) continue;
                var t = radii[k] * Mathf.Tan(turn * .5f);
                t = Mathf.Min(t, (poly[k] - prev).magnitude * .5f, (next - poly[k]).magnitude * .5f);
                tin[k] = poly[k] - d1 * t;
                tout[k] = poly[k] + d2 * t;
                hasArc[k] = true;
            }
            var segs = closed ? n : n - 1;
            for (var k = 0; k < segs; k++)
            {
                var k1 = (k + 1) % n;
                var a = tout[k];
                var b = tin[k1];
                var d = (poly[k1] - poly[k]).normalized;
                if ((b - a).sqrMagnitude > 1e-12f) pr.Line(a, b, Ccw(d));
                if (hasArc[k1])
                {
                    var next = poly[(k1 + 1) % n];
                    var d2 = (next - poly[k1]).normalized;
                    var n0 = Ccw(d);
                    var n1 = Ccw(d2);
                    // Fillet centre: on the inside of the turn, radius from both tangent points.
                    var cross = d.x * d2.y - d.y * d2.x;
                    // Left turn (cross > 0) turns toward the outside: a concave fillet whose centre is on the air side.
                    var centre = cross > 0f ? tin[k1] + n0 * Fillet(tin[k1], tout[k1], d, d2) : tin[k1] - n0 * Fillet(tin[k1], tout[k1], d, d2);
                    var arc = new List<Vector2>();
                    var arcN = new List<Vector2>();
                    var r0 = tin[k1] - centre;
                    var r1 = tout[k1] - centre;
                    var a0 = Mathf.Atan2(r0.y, r0.x);
                    var da = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, Mathf.Atan2(r1.y, r1.x) * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                    var rad = r0.magnitude;
                    var na0 = Mathf.Atan2(n0.y, n0.x);
                    var dn = Mathf.DeltaAngle(na0 * Mathf.Rad2Deg, Mathf.Atan2(n1.y, n1.x) * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                    for (var s = 0; s <= arcSegments; s++)
                    {
                        var f = (float)s / arcSegments;
                        var aa = a0 + da * f;
                        arc.Add(centre + new Vector2(Mathf.Cos(aa), Mathf.Sin(aa)) * rad);
                        var nn = na0 + dn * f;
                        arcN.Add(new Vector2(Mathf.Cos(nn), Mathf.Sin(nn)));
                    }
                    pr.points.Add(arc);
                    pr.normals.Add(arcN);
                }
            }
            foreach (var p in poly) pr.outline.Add(p);
            return pr;
        }

        static float Fillet(Vector2 tin, Vector2 tout, Vector2 d1, Vector2 d2)
        {
            // Distance from a tangent point to the centre (= radius): chord / (2 sin(turn/2)).
            var turn = Mathf.Acos(Mathf.Clamp(Vector2.Dot(d1, d2), -1f, 1f));
            var chord = (tout - tin).magnitude;
            return chord / Mathf.Max(1e-6f, 2f * Mathf.Sin(turn * .5f));
        }

        /// <summary>The cap outline: every strip point in order, de-duplicated.</summary>
        public List<Vector2> CapOutline()
        {
            if (outline.Count > 0 && points.Count == 0) return outline;
            var o = new List<Vector2>();
            foreach (var strip in points)
                foreach (var p in strip)
                    if (o.Count == 0 || (o[o.Count - 1] - p).sqrMagnitude > 1e-12f) o.Add(p);
            foreach (var p in extraCap) o.Add(p);
            if (o.Count > 1 && (o[0] - o[o.Count - 1]).sqrMagnitude < 1e-12f) o.RemoveAt(o.Count - 1);
            return o;
        }

        /// <summary>Points appended to the cap outline after the strips (e.g. the hidden wall corner).</summary>
        public readonly List<Vector2> extraCap = new List<Vector2>();
    }

    /// <summary>
    /// Sweep a profile along a polyline that lies in the plane normal to A.
    /// Joints are mitred. o0 is the outward vector of the first segment; the
    /// others turn with the path. k0 / k1 slide the open ends' section by k*s
    /// along the path (1 = an outside mitre, for runs that wrap a corner).
    /// </summary>
    public static int Sweep(TransitionMesh m, IList<Vector3> path, bool closed, Vector3 A, Vector3 o0, Profile prof,
        float k0 = 0f, float k1 = 0f, bool cap0 = false, bool cap1 = false)
    {
        var before = m.TriangleCount;
        var np = path.Count;
        var nSeg = closed ? np : np - 1;
        if (nSeg < 1) return 0;
        A = A.normalized;
        var d = new Vector3[nSeg];
        var o = new Vector3[nSeg];
        var len = new float[nSeg];
        for (var k = 0; k < nSeg; k++)
        {
            var v = path[(k + 1) % np] - path[k];
            len[k] = v.magnitude;
            d[k] = v / Mathf.Max(1e-6f, len[k]);
        }
        var side = Mathf.Sign(Vector3.Dot(Vector3.Cross(A, d[0]), o0));
        if (side == 0f) side = 1f;
        for (var k = 0; k < nSeg; k++) o[k] = (Vector3.Cross(A, d[k]) * side).normalized;

        Vector3 Offset(int seg, bool atStart, float s)
        {
            var other = atStart ? seg - 1 : seg + 1;
            if (closed) other = (other + nSeg) % nSeg;
            if (other >= 0 && other < nSeg)
            {
                var mv = (o[seg] + o[other]).normalized;
                var dot = Vector3.Dot(mv, o[seg]);
                return mv * (s / Mathf.Max(.2f, dot));
            }
            var kk = atStart ? k0 : k1;
            var outDir = atStart ? -d[seg] : d[seg];
            return o[seg] * s + outDir * (kk * s);
        }

        var uAcc = 0f;
        for (var k = 0; k < nSeg; k++)
        {
            var p0 = path[k];
            var p1 = path[(k + 1) % np];
            for (var si = 0; si < prof.points.Count; si++)
            {
                var pts = prof.points[si];
                var nrm = prof.normals[si];
                var vAcc = 0f;
                var prev0 = -1;
                var prev1 = -1;
                for (var j = 0; j < pts.Count; j++)
                {
                    if (j > 0) vAcc += (pts[j] - pts[j - 1]).magnitude;
                    var s = pts[j].x;
                    var w = pts[j].y;
                    var n = (o[k] * nrm[j].x + A * nrm[j].y).normalized;
                    var v0 = p0 + Offset(k, true, s) + A * w;
                    var v1 = p1 + Offset(k, false, s) + A * w;
                    var i0 = m.Add(v0, n, new Vector2(uAcc + Vector3.Dot(v0 - p0, d[k]), vAcc));
                    var i1 = m.Add(v1, n, new Vector2(uAcc + Vector3.Dot(v1 - p0, d[k]), vAcc));
                    if (prev0 >= 0)
                    {
                        m.Tri(prev0, prev1, i1);
                        m.Tri(prev0, i1, i0);
                    }
                    prev0 = i0;
                    prev1 = i1;
                }
            }
            uAcc += len[k];
        }
        if (!closed)
        {
            if (cap0) Cap(m, prof, path[0], o[0], A, -d[0], 0, k0, d[0]);
            if (cap1) Cap(m, prof, path[np - 1], o[nSeg - 1], A, d[nSeg - 1], 0, k1, d[nSeg - 1]);
        }
        return m.TriangleCount - before;
    }

    static void Cap(TransitionMesh m, Profile prof, Vector3 at, Vector3 o, Vector3 A, Vector3 normal, int unused, float k, Vector3 d)
    {
        var outline = prof.CapOutline();
        if (outline.Count < 3) return;
        var idx = new int[outline.Count];
        for (var i = 0; i < outline.Count; i++)
        {
            var p = at + o * outline[i].x + A * outline[i].y + normal * (k * outline[i].x);
            idx[i] = m.Add(p, normal, new Vector2(Vector3.Dot(p, o), Vector3.Dot(p, A)));
        }
        foreach (var t in EarClip(outline))
            m.Tri(idx[t.x], idx[t.y], idx[t.z]);
    }

    /// <summary>Ear clipping for a simple polygon (either winding).</summary>
    public static List<Vector3Int> EarClip(IList<Vector2> poly)
    {
        var result = new List<Vector3Int>();
        var n = poly.Count;
        var idx = new List<int>();
        for (var i = 0; i < n; i++) idx.Add(i);
        var area = 0f;
        for (var i = 0; i < n; i++)
        {
            var a = poly[i];
            var b = poly[(i + 1) % n];
            area += a.x * b.y - b.x * a.y;
        }
        var ccw = area > 0f;
        float Cross(Vector2 a, Vector2 b, Vector2 c) => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        bool Inside(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            var c1 = Cross(a, b, p);
            var c2 = Cross(b, c, p);
            var c3 = Cross(c, a, p);
            return ccw ? (c1 > 1e-12f && c2 > 1e-12f && c3 > 1e-12f) : (c1 < -1e-12f && c2 < -1e-12f && c3 < -1e-12f);
        }
        var guard = 0;
        while (idx.Count > 3 && guard++ < 10000)
        {
            var clipped = false;
            for (var i = 0; i < idx.Count; i++)
            {
                var ia = idx[(i - 1 + idx.Count) % idx.Count];
                var ib = idx[i];
                var ic = idx[(i + 1) % idx.Count];
                var cr = Cross(poly[ia], poly[ib], poly[ic]);
                if (ccw ? cr <= 1e-14f : cr >= -1e-14f) continue;
                var ok = true;
                foreach (var j in idx)
                {
                    if (j == ia || j == ib || j == ic) continue;
                    if (Inside(poly[j], poly[ia], poly[ib], poly[ic])) { ok = false; break; }
                }
                if (!ok) continue;
                result.Add(new Vector3Int(ia, ib, ic));
                idx.RemoveAt(i);
                clipped = true;
                break;
            }
            if (!clipped) break;
        }
        if (idx.Count == 3) result.Add(new Vector3Int(idx[0], idx[1], idx[2]));
        return result;
    }
}
