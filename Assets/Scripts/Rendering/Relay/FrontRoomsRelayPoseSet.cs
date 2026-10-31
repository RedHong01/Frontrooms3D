using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The pose keys of the production Relay body ("Night Shift", research/hunter/production_b/10_plan.md section 3.1):
/// Resources/Creatures/Relay_NightShift_Poses.json, written by
/// Tools/Blender/frontrooms_kit/creatures/relay_night_shift.py.
///
/// The file is in Blender armature space: per bone the bind head and bind rotation, and per key the
/// armature-space delta rotation and the offset from the parent's head (in the parent's bind frame), so that
///     rot[b] = delta[b] * bindRot[b],   pos[b] = pos[parent] + delta[parent] * off[b].
/// <see cref="Convert"/> turns every key into the imported body's own model space with an axis map measured
/// from the bind heads (a signed permutation times a uniform scale), so the FBX importer's axis conversion and
/// bone rolls never matter. Parsed once per TextAsset.
/// </summary>
public sealed class FrontRoomsRelayPoseSet
{
    [Serializable] public sealed class BoneData { public string name; public string parent; public float[] bind; public float[] bindRot; public float length; }
    [Serializable] public sealed class PoseData { public string name; public int faceLift; public float[] rot; public float[] off; public float[] lens; public float top; }
    [Serializable] public sealed class LocatorData { public string name; public string bone; public float[] pos; public float[] dir; }
    [Serializable] public sealed class NeckData { public float n1, n2, n3, hidden, stretchMax = 1.25f; }
    [Serializable] public sealed class GaitData { public float stepStd = 1.14f, stepStdRun = 1.45f, stepLow = .9f, stepLowRun = 1.1f, stepTall = 1.3f, stepTallRun = 1.6f, swingLift = .14f; }
    [Serializable] public sealed class DoorData { public float sIn = -.62f, sMid, sOut = .6f; }
    [Serializable] public sealed class FileData { public int version; public string name; public BoneData[] bones; public PoseData[] poses; public LocatorData[] locators; public NeckData neck; public GaitData gait; public DoorData door; }

    /// <summary>One key in model space: per bone the delta rotation and the offset (see the class summary).</summary>
    public sealed class Key
    {
        public string name;
        public int faceLift;
        public Quaternion[] delta;
        public Vector3[] offset;
        public float top;
    }

    public readonly FileData Data;
    public readonly string[] BoneNames;
    public readonly int[] Parent;
    readonly Dictionary<string, int> index = new Dictionary<string, int>();

    static readonly Dictionary<int, FrontRoomsRelayPoseSet> Cache = new Dictionary<int, FrontRoomsRelayPoseSet>();

    FrontRoomsRelayPoseSet(FileData data)
    {
        Data = data;
        var n = data.bones.Length;
        BoneNames = new string[n];
        Parent = new int[n];
        for (var i = 0; i < n; i++)
        {
            BoneNames[i] = data.bones[i].name;
            index[BoneNames[i]] = i;
        }
        for (var i = 0; i < n; i++)
            Parent[i] = string.IsNullOrEmpty(data.bones[i].parent) ? -1 : IndexOf(data.bones[i].parent);
    }

    public int BoneCount => BoneNames.Length;

    /// <summary>Parsed once per TextAsset; null if the file is missing or malformed.</summary>
    public static FrontRoomsRelayPoseSet Load(TextAsset json)
    {
        if (json == null) return null;
        var id = FrontRoomsObjectId.Of(json);
        if (Cache.TryGetValue(id, out var cached) && cached != null) return cached;
        FileData data;
        try { data = JsonUtility.FromJson<FileData>(json.text); }
        catch (Exception e)
        {
            Debug.LogWarning("[FrontRoomsRelayBody] " + json.name + " could not be parsed: " + e.Message);
            return null;
        }
        if (data == null || data.bones == null || data.poses == null || data.bones.Length == 0)
        {
            Debug.LogWarning("[FrontRoomsRelayBody] " + json.name + " holds no bones or poses.");
            return null;
        }
        var n = data.bones.Length;
        foreach (var p in data.poses)
            if (p.rot == null || p.off == null || p.rot.Length != n * 4 || p.off.Length != n * 3)
            {
                Debug.LogWarning("[FrontRoomsRelayBody] " + json.name + ": key " + p.name + " does not match the " + n + " bones.");
                return null;
            }
        if (data.neck == null) data.neck = new NeckData();
        if (data.gait == null) data.gait = new GaitData();
        if (data.door == null) data.door = new DoorData();
        var set = new FrontRoomsRelayPoseSet(data);
        Cache[id] = set;
        return set;
    }

    public int IndexOf(string bone) => bone != null && index.TryGetValue(bone, out var i) ? i : -1;

    public static Vector3 V(float[] a, int at = 0) => a != null && a.Length >= at + 3 ? new Vector3(a[at], a[at + 1], a[at + 2]) : Vector3.zero;
    public static Quaternion Q(float[] a, int at = 0) => a != null && a.Length >= at + 4 ? new Quaternion(a[at], a[at + 1], a[at + 2], a[at + 3]) : Quaternion.identity;

    /// <summary>
    /// The axis map from Blender armature space to the body's model space, measured from the bind heads:
    /// model = scale * axes * blender + origin. axes is snapped to a signed permutation; false when the
    /// measured map is not one (residual above 5 mm), and the caller keeps the legacy rig.
    /// </summary>
    public bool MeasureAxes(Vector3[] modelBindHeads, out Matrix4x4 axes, out float scale, out Vector3 origin, out float residual)
    {
        var n = BoneCount;
        axes = Matrix4x4.identity; scale = 1f; origin = Vector3.zero; residual = float.MaxValue;
        if (modelBindHeads == null || modelBindHeads.Length != n || n < 4) return false;
        Vector3 cb = Vector3.zero, cu = Vector3.zero;
        for (var i = 0; i < n; i++) { cb += V(Data.bones[i].bind); cu += modelBindHeads[i]; }
        cb /= n; cu /= n;
        // H = sum (u - cu)(b - cb)^T, G = sum (b - cb)(b - cb)^T, A = H G^-1
        var H = new double[3, 3];
        var G = new double[3, 3];
        for (var i = 0; i < n; i++)
        {
            var b = V(Data.bones[i].bind) - cb;
            var u = modelBindHeads[i] - cu;
            for (var r = 0; r < 3; r++)
                for (var c = 0; c < 3; c++)
                {
                    H[r, c] += u[r] * b[c];
                    G[r, c] += b[r] * b[c];
                }
        }
        if (!Invert3(G, out var Gi)) return false;
        var A = new double[3, 3];
        for (var r = 0; r < 3; r++)
            for (var c = 0; c < 3; c++)
                for (var k = 0; k < 3; k++) A[r, c] += H[r, k] * Gi[k, c];
        var det = A[0, 0] * (A[1, 1] * A[2, 2] - A[1, 2] * A[2, 1]) - A[0, 1] * (A[1, 0] * A[2, 2] - A[1, 2] * A[2, 0]) + A[0, 2] * (A[1, 0] * A[2, 1] - A[1, 1] * A[2, 0]);
        if (Math.Abs(det) < 1e-9) return false;
        scale = (float)Math.Pow(Math.Abs(det), 1.0 / 3.0);
        axes = Matrix4x4.zero;
        axes[3, 3] = 1f;
        var used = new bool[3];
        for (var r = 0; r < 3; r++)
        {
            var best = 0;
            for (var c = 1; c < 3; c++) if (Math.Abs(A[r, c]) > Math.Abs(A[r, best])) best = c;
            if (used[best]) return false;
            used[best] = true;
            axes[r, best] = A[r, best] >= 0 ? 1f : -1f;
        }
        for (var c = 0; c < 3; c++) if (axes[c, 0] == 0f && axes[c, 1] == 0f && axes[c, 2] == 0f) return false;
        axes[0, 3] = axes[1, 3] = axes[2, 3] = 0f;
        origin = cu - scale * axes.MultiplyVector(cb);
        residual = 0f;
        for (var i = 0; i < n; i++)
        {
            var m = scale * axes.MultiplyVector(V(Data.bones[i].bind)) + origin;
            residual = Mathf.Max(residual, (m - modelBindHeads[i]).magnitude);
        }
        return residual <= .005f;
    }

    static bool Invert3(double[,] m, out double[,] inv)
    {
        inv = new double[3, 3];
        var a = m[0, 0]; var b = m[0, 1]; var c = m[0, 2];
        var d = m[1, 0]; var e = m[1, 1]; var f = m[1, 2];
        var g = m[2, 0]; var h = m[2, 1]; var k = m[2, 2];
        var A = e * k - f * h; var B = -(d * k - f * g); var C = d * h - e * g;
        var det = a * A + b * B + c * C;
        if (Math.Abs(det) < 1e-12) return false;
        var s = 1.0 / det;
        inv[0, 0] = A * s; inv[0, 1] = -(b * k - c * h) * s; inv[0, 2] = (b * f - c * e) * s;
        inv[1, 0] = B * s; inv[1, 1] = (a * k - c * g) * s; inv[1, 2] = -(a * f - c * d) * s;
        inv[2, 0] = C * s; inv[2, 1] = -(a * h - b * g) * s; inv[2, 2] = (a * e - b * d) * s;
        return true;
    }

    /// <summary>A Blender rotation conjugated into model space by the signed permutation.</summary>
    public static Quaternion ConvertRotation(Quaternion blender, Matrix4x4 axes)
    {
        var m = axes * Matrix4x4.Rotate(blender) * axes.transpose;
        var forward = new Vector3(m.m02, m.m12, m.m22);
        var up = new Vector3(m.m01, m.m11, m.m21);
        return Quaternion.LookRotation(forward, up);
    }

    /// <summary>Every key in model space (positions scaled; the root offset also gets the origin).</summary>
    public List<Key> Convert(Matrix4x4 axes, float scale, Vector3 origin)
    {
        var n = BoneCount;
        var keys = new List<Key>(Data.poses.Length);
        foreach (var p in Data.poses)
        {
            var k = new Key { name = p.name, faceLift = p.faceLift, delta = new Quaternion[n], offset = new Vector3[n], top = p.top * scale };
            for (var b = 0; b < n; b++)
            {
                k.delta[b] = ConvertRotation(Q(p.rot, b * 4), axes);
                var o = scale * axes.MultiplyVector(V(p.off, b * 3));
                k.offset[b] = Parent[b] < 0 ? o + origin : o;
            }
            keys.Add(k);
        }
        return keys;
    }
}
