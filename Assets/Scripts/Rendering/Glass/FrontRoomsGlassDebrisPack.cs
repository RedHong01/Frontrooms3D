// FrontRoomsGlassDebrisPack.cs -- GD3c (research/glass/destruction/gd3c/10_design.md sec 4): the compact record a shattered
// window leaves behind, one packed blob per edge in visual-side session memory. It survives the chunk's release (the floor
// glass and teeth meshes go with the chunk root) so a rebuilt window gets its teeth and its settled floor glass back without
// simulating anything again ("cracks do not heal", like the S1/S2 restore). The pattern itself is never stored: the map's
// GlassBreakRecord (seed, impact, rotation, side) regenerates it byte for byte, so a shard needs only its piece index and
// its settled pose.
//
// Layout (little-endian):
//   header 44 B   edge (long 8) | seed (int 4), impact x/y (half 2+2), rotation deg (half 2), side (sbyte), tier (byte) |
//                 floorY (float 4), break time (float 4), env hash (uint 4) | teeth snapped mask (ulong 8) |
//                 shard count (ushort 2), crumb-cluster count (byte), flags (byte)
//   per shard 16 B piece index (byte), flags (byte: bits 0-1 smallest-three index, 2 far side, 3 upside down, 4 on the sill,
//                 5 dropped out of the opening), centroid position int16 x 3 in 0.1 mm in the window root's frame (+-3.27 m),
//                 rotation smallest-three int16 x 3 (error about 0.003 deg), 2 B pad
//   per cluster 12 B (<= 8) centre half2 and extent half2 (root x, z), count ushort, seed ushort (14 bits) + level (2 bits:
//                 0 floor, 1 sill). Crumbs (2-9 mm) are scattered again from the seed: their exact places cannot be seen.
// Annealed6 at its 121-body maximum: 44 + 121 x 16 + 8 x 12 = 2,076 B. The reduced tier (<= 40 bodies): <= 780 B.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class FrontRoomsGlassDebrisPack
{
    public const int HeaderBytes = 44, ShardBytes = 16, ClusterBytes = 12, MaxClusters = 8, MaxShards = 255;
    /// <summary>Position quantum: 0.1 mm (int16 covers +-3.2767 m round the window root).</summary>
    public const float PositionScale = 10000f;
    const float RotationScale = 32767f / 0.70710678f;

    public const byte ShardFar = 1 << 2, ShardUpsideDown = 1 << 3, ShardOnSill = 1 << 4, ShardDropped = 1 << 5;
    public const byte TierDesktop = 0, TierReduced = 1;

    public struct Header
    {
        public long edge;
        public int seed;
        public Vector2 impact;
        public float rotation;
        public int side;
        public byte tier;
        public float floorY, breakTime;
        public uint envHash;
        public ulong teethSnapped;
        public byte flags;
    }

    public struct Shard
    {
        public byte piece, flags;
        /// <summary>The shard's rest centroid where it lies (window root frame).</summary>
        public Vector3 position;
        /// <summary>The shard object's rotation relative to the window root (its mesh is slab-framed at the stage-2 pose).</summary>
        public Quaternion rotation;
    }

    public struct Cluster
    {
        public Vector2 centre, extent;   // root x, z
        public ushort count, seed;
        public byte level;               // 0 floor, 1 sill
    }

    public static int SizeOf(int shards, int clusters) => HeaderBytes + shards * ShardBytes + clusters * ClusterBytes;

    // ---- pack --------------------------------------------------------------------------------------------------------------
    public static byte[] Pack(Header h, List<Shard> shards, List<Cluster> clusters)
    {
        var ns = shards != null ? Math.Min(shards.Count, MaxShards) : 0;
        var nc = clusters != null ? Math.Min(clusters.Count, MaxClusters) : 0;
        var b = new byte[SizeOf(ns, nc)];
        var o = 0;
        PutLong(b, ref o, h.edge);
        PutInt(b, ref o, h.seed);
        PutHalf(b, ref o, h.impact.x);
        PutHalf(b, ref o, h.impact.y);
        PutHalf(b, ref o, h.rotation);
        b[o++] = unchecked((byte)(sbyte)Mathf.Clamp(h.side, -1, 1));
        b[o++] = h.tier;
        PutFloat(b, ref o, h.floorY);
        PutFloat(b, ref o, h.breakTime);
        PutUInt(b, ref o, h.envHash);
        PutLong(b, ref o, unchecked((long)h.teethSnapped));
        PutUShort(b, ref o, (ushort)ns);
        b[o++] = (byte)nc;
        b[o++] = h.flags;
        for (var i = 0; i < ns; i++)
        {
            var s = shards[i];
            EncodeRotation(s.rotation, out var largest, out var r0, out var r1, out var r2);
            b[o++] = s.piece;
            b[o++] = (byte)((s.flags & ~3) | largest);
            PutShort(b, ref o, Q(s.position.x));
            PutShort(b, ref o, Q(s.position.y));
            PutShort(b, ref o, Q(s.position.z));
            PutShort(b, ref o, r0);
            PutShort(b, ref o, r1);
            PutShort(b, ref o, r2);
            o += 2;
        }
        for (var i = 0; i < nc; i++)
        {
            var c = clusters[i];
            PutHalf(b, ref o, c.centre.x);
            PutHalf(b, ref o, c.centre.y);
            PutHalf(b, ref o, c.extent.x);
            PutHalf(b, ref o, c.extent.y);
            PutUShort(b, ref o, c.count);
            PutUShort(b, ref o, (ushort)((c.seed & 0x3FFF) | ((c.level & 3) << 14)));
        }
        return b;
    }

    // ---- unpack ------------------------------------------------------------------------------------------------------------
    public static bool Unpack(byte[] b, out Header h, List<Shard> shards, List<Cluster> clusters)
    {
        h = default;
        shards?.Clear();
        clusters?.Clear();
        if (b == null || b.Length < HeaderBytes) return false;
        var o = 0;
        h.edge = GetLong(b, ref o);
        h.seed = GetInt(b, ref o);
        h.impact = new Vector2(GetHalf(b, ref o), GetHalf(b, ref o));
        h.rotation = GetHalf(b, ref o);
        h.side = unchecked((sbyte)b[o++]);
        h.tier = b[o++];
        h.floorY = GetFloat(b, ref o);
        h.breakTime = GetFloat(b, ref o);
        h.envHash = GetUInt(b, ref o);
        h.teethSnapped = unchecked((ulong)GetLong(b, ref o));
        var ns = GetUShort(b, ref o);
        var nc = b[o++];
        h.flags = b[o++];
        if (b.Length != SizeOf(ns, nc)) return false;
        for (var i = 0; i < ns; i++)
        {
            var s = new Shard { piece = b[o++] };
            var fl = b[o++];
            s.flags = (byte)(fl & ~3);
            s.position = new Vector3(GetShort(b, ref o) / PositionScale, GetShort(b, ref o) / PositionScale, GetShort(b, ref o) / PositionScale);
            var r0 = GetShort(b, ref o);
            var r1 = GetShort(b, ref o);
            var r2 = GetShort(b, ref o);
            s.rotation = DecodeRotation(fl & 3, r0, r1, r2);
            o += 2;
            shards?.Add(s);
        }
        for (var i = 0; i < nc; i++)
        {
            var c = new Cluster();
            c.centre = new Vector2(GetHalf(b, ref o), GetHalf(b, ref o));
            c.extent = new Vector2(GetHalf(b, ref o), GetHalf(b, ref o));
            c.count = GetUShort(b, ref o);
            var sl = GetUShort(b, ref o);
            c.seed = (ushort)(sl & 0x3FFF);
            c.level = (byte)(sl >> 14);
            clusters?.Add(c);
        }
        return true;
    }

    public static bool UnpackHeader(byte[] b, out Header h) => Unpack(b, out h, null, null);

    // ---- smallest three --------------------------------------------------------------------------------------------------
    public static void EncodeRotation(Quaternion q, out int largest, out short a, out short b, out short c)
    {
        q = Normalize(q);
        float x = q.x, y = q.y, z = q.z, w = q.w;
        float ax = Mathf.Abs(x), ay = Mathf.Abs(y), az = Mathf.Abs(z), aw = Mathf.Abs(w);
        largest = 0;
        var big = ax;
        if (ay > big) { big = ay; largest = 1; }
        if (az > big) { big = az; largest = 2; }
        if (aw > big) { largest = 3; }
        var sign = (largest == 0 ? x : largest == 1 ? y : largest == 2 ? z : w) < 0f ? -1f : 1f;
        float s0, s1, s2;
        if (largest == 0) { s0 = y; s1 = z; s2 = w; }
        else if (largest == 1) { s0 = x; s1 = z; s2 = w; }
        else if (largest == 2) { s0 = x; s1 = y; s2 = w; }
        else { s0 = x; s1 = y; s2 = z; }
        a = Q16(s0 * sign); b = Q16(s1 * sign); c = Q16(s2 * sign);
    }

    static short Q16(float v) => (short)Mathf.Clamp(Mathf.RoundToInt(v * RotationScale), -32767, 32767);

    public static Quaternion DecodeRotation(int largest, short a, short b, short c)
    {
        float s0 = a / RotationScale, s1 = b / RotationScale, s2 = c / RotationScale;
        var big = Mathf.Sqrt(Mathf.Max(0f, 1f - (s0 * s0 + s1 * s1 + s2 * s2)));
        Quaternion q;
        if (largest == 0) q = new Quaternion(big, s0, s1, s2);
        else if (largest == 1) q = new Quaternion(s0, big, s1, s2);
        else if (largest == 2) q = new Quaternion(s0, s1, big, s2);
        else q = new Quaternion(s0, s1, s2, big);
        return Normalize(q);
    }

    static Quaternion Normalize(Quaternion q)
    {
        var m = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        return m > 1e-8f ? new Quaternion(q.x / m, q.y / m, q.z / m, q.w / m) : Quaternion.identity;
    }

    static short Q(float metres) => (short)Mathf.Clamp(Mathf.RoundToInt(metres * PositionScale), -32767, 32767);

    // ---- bytes -------------------------------------------------------------------------------------------------------------
    static void PutLong(byte[] b, ref int o, long v) { for (var i = 0; i < 8; i++) b[o++] = (byte)(v >> (8 * i)); }
    static void PutInt(byte[] b, ref int o, int v) { for (var i = 0; i < 4; i++) b[o++] = (byte)(v >> (8 * i)); }
    static void PutUInt(byte[] b, ref int o, uint v) { for (var i = 0; i < 4; i++) b[o++] = (byte)(v >> (8 * i)); }
    static void PutShort(byte[] b, ref int o, short v) { b[o++] = (byte)v; b[o++] = (byte)(v >> 8); }
    static void PutUShort(byte[] b, ref int o, ushort v) { b[o++] = (byte)v; b[o++] = (byte)(v >> 8); }
    static void PutHalf(byte[] b, ref int o, float v) => PutUShort(b, ref o, Mathf.FloatToHalf(v));
    static void PutFloat(byte[] b, ref int o, float v) { var bytes = BitConverter.GetBytes(v); if (!BitConverter.IsLittleEndian) Array.Reverse(bytes); for (var i = 0; i < 4; i++) b[o++] = bytes[i]; }
    static long GetLong(byte[] b, ref int o) { long v = 0; for (var i = 0; i < 8; i++) v |= (long)b[o++] << (8 * i); return v; }
    static int GetInt(byte[] b, ref int o) { var v = 0; for (var i = 0; i < 4; i++) v |= b[o++] << (8 * i); return v; }
    static uint GetUInt(byte[] b, ref int o) { uint v = 0; for (var i = 0; i < 4; i++) v |= (uint)b[o++] << (8 * i); return v; }
    static short GetShort(byte[] b, ref int o) { var v = (short)(b[o] | (b[o + 1] << 8)); o += 2; return v; }
    static ushort GetUShort(byte[] b, ref int o) { var v = (ushort)(b[o] | (b[o + 1] << 8)); o += 2; return v; }
    static float GetHalf(byte[] b, ref int o) => Mathf.HalfToFloat(GetUShort(b, ref o));
    static float GetFloat(byte[] b, ref int o) { var t = new byte[4]; for (var i = 0; i < 4; i++) t[i] = b[o++]; if (!BitConverter.IsLittleEndian) Array.Reverse(t); return BitConverter.ToSingle(t, 0); }
}
