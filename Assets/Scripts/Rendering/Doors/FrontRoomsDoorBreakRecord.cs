// FrontRoomsDoorBreakRecord.cs -- the door break's persistence, production P1 (2026-10-30). Design: Documentation/research/
// door_break/50_rethink.md 3.9 "Persistence" (after GD3c's packed glass record) and 54_dust_grit.md 4.5.
//
// A knock is never undone: every damaged door keeps a 16-byte record in visual-side session memory, keyed by (map, floor
// seed, edge). DressDoor restores from it (a chunk rebuilt 48+ m away, a shift), and a door the Relay half broke and left
// shows its stage again when the Relay returns (kept damage = max(kept, mapped)).
//   edge long 8 | stage byte | burst sbyte | motif byte | throw byte | flags ushort | env ushort   = 16 B
// The design's last field was the break time (half); nothing reads it (the dust is gone 3 s after a break and a restore is
// never seen), so it carries the 16-bit fold of the neighbourhood hash taken at the settle instead, for the env check after a
// shift (GD3c's rule: a piece that would sit inside a new prop is dropped).
// The broken door's floor (the dust skin's density and the settled crumbs and chips) is a separate, capped side pack
// (about 12-18 KB a door, at most MaxPacks; the oldest released goes first): with it a restore is exact, without it the
// debris still comes back from the baked throw.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct FrontRoomsDoorBreakRecord
{
    public long edge;
    /// <summary>The stage shown: 0 E (no mark), 1 M, 2 D1, 3 D2, 4 FINAL / TORN.</summary>
    public byte stage;
    /// <summary>+1 = the leaf burst toward the rig's +X, -1 = toward -X (the break root's mirror).</summary>
    public sbyte burst;
    /// <summary>The split motif (0..2) and the baked throw (0..3), both from the edge hash.</summary>
    public byte motif;
    public byte throwVariant;
    public ushort flags;
    /// <summary>The 16-bit fold of the neighbourhood hash at the settle (flags EnvSet).</summary>
    public ushort env;

    public const int Size = 16;
    public const ushort Broken = 1, PoseB = 2, Settled = 4, EnvSet = 8, HardFloor = 16, Struck = 32;

    public bool Has(ushort f) => (flags & f) != 0;
    public override string ToString() => "edge " + edge + " stage " + stage + " burst " + burst + " motif " + motif + " throw " + throwVariant + " flags 0x" + flags.ToString("x") + " env 0x" + env.ToString("x4");
}

public static class FrontRoomsDoorBreakRecords
{
    /// <summary>Floor packs kept per session (the records themselves are never evicted: 16 B each).</summary>
    public static int MaxPacks = 48;

    public sealed class FloorPack
    {
        public byte[] skin;                                   // FrontRoomsDoorBreakVfx.SkinW x SkinH, R8
        public readonly List<FrontRoomsDoorBreakVfx.Solid> solids = new List<FrontRoomsDoorBreakVfx.Solid>();
        public bool live = true;
        public int releasedOrder;
        public int Bytes => (skin != null ? skin.Length : 0) + solids.Count * 12 + 48;
    }

    struct Key : IEquatable<Key>
    {
        public int map, floor;
        public long edge;
        public bool Equals(Key o) => map == o.map && floor == o.floor && edge == o.edge;
        public override bool Equals(object o) => o is Key k && Equals(k);
        public override int GetHashCode() => (map * 486187739 ^ floor * 16777619) ^ edge.GetHashCode();
    }

    static readonly Dictionary<Key, FrontRoomsDoorBreakRecord> store = new Dictionary<Key, FrontRoomsDoorBreakRecord>();
    static readonly Dictionary<Key, FloorPack> packs = new Dictionary<Key, FloorPack>();
    static int releaseCounter;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { store.Clear(); packs.Clear(); releaseCounter = 0; }

    static Key K(FrontRoomsMapWorld map, long edge) => new Key { map = map != null ? FrontRoomsObjectId.Of(map) : 0, floor = map != null ? map.Seed : 0, edge = edge };

    public static bool TryGet(FrontRoomsMapWorld map, long edge, out FrontRoomsDoorBreakRecord r) => store.TryGetValue(K(map, edge), out r);

    public static void Put(FrontRoomsMapWorld map, FrontRoomsDoorBreakRecord r)
    {
        if (map == null) return;
        store[K(map, r.edge)] = r;
    }

    public static FloorPack Pack(FrontRoomsMapWorld map, long edge) => packs.TryGetValue(K(map, edge), out var p) ? p : null;

    public static FloorPack EnsurePack(FrontRoomsMapWorld map, long edge)
    {
        var k = K(map, edge);
        if (!packs.TryGetValue(k, out var p)) { p = new FloorPack(); packs[k] = p; }
        p.live = true;
        Enforce();
        return p;
    }

    /// <summary>The door's chunk went (its visual is destroyed): its pack may be evicted, oldest released first.</summary>
    public static void MarkReleased(FrontRoomsMapWorld map, long edge)
    {
        if (packs.TryGetValue(K(map, edge), out var p)) { p.live = false; p.releasedOrder = ++releaseCounter; Enforce(); }
    }

    static void Enforce()
    {
        while (packs.Count > MaxPacks)
        {
            Key oldest = default;
            var best = int.MaxValue;
            foreach (var kv in packs) if (!kv.Value.live && kv.Value.releasedOrder < best) { best = kv.Value.releasedOrder; oldest = kv.Key; }
            if (best == int.MaxValue) break;
            packs.Remove(oldest);
            PacksEvicted++;
        }
    }

    /// <summary>Tools and a map reset of one door: forget its record and pack.</summary>
    public static void Forget(FrontRoomsMapWorld map, long edge)
    {
        var k = K(map, edge);
        store.Remove(k);
        packs.Remove(k);
    }

    /// <summary>A new run (MapRunStarted) or a new floor (FloorChanged): forget everything.</summary>
    public static void Clear() { store.Clear(); packs.Clear(); PacksEvicted = 0; }

    public static int Count => store.Count;
    public static int PackCount => packs.Count;
    public static int PacksEvicted { get; private set; }
    public static long Bytes
    {
        get
        {
            long b = store.Count * FrontRoomsDoorBreakRecord.Size;
            foreach (var p in packs.Values) b += p.Bytes;
            return b;
        }
    }

    /// <summary>The edge hash every door-break choice comes from (motif, throw, VFX seed): stable per edge and floor.</summary>
    public static uint Hash(long edge, int salt)
    {
        unchecked
        {
            var z = (ulong)edge * 0x9E3779B97F4A7C15UL ^ (ulong)(uint)salt * 0xC2B2AE3D27D4EB4FUL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return (uint)z;
        }
    }
}
