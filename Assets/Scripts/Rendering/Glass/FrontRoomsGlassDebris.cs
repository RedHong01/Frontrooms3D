// FrontRoomsGlassDebris.cs -- GD3 steps 6-7 and GD3c (plan 10 sec 2.3 S3-S4, 4.3, 4.5; build/06_debris.md; gd3c/10_design.md
// sec 4, 6): the public face of the broken glass for the other chats, and the compact records that bring a shattered window's
// teeth and floor glass back after its chunk was released. Pure static API; the work is done by FrontRoomsGlassDebrisWorld
// (the shards), FrontRoomsGlassFloorGlass (teeth and floor glass on the window root) and FrontRoomsGlassVfx (chips, crumbs).
//
// Sound chat (plan sec 4.5), visual -> sound (FYI only, no new beat):
//   ShardImpact(position, mass kg, speed m/s)   a shard hit something for real (a contact in the debris physics scene),
//                                               throttled (>= 0.3 m/s, one per shard per 50 ms, <= 16 per frame).
//                                               ShardImpactDetailed carries the surface (carpet, hard floor, wall, furniture,
//                                               glass) and whether it is the shard's first landing, for the muffled-carpet rule.
//   TeethSnap(position)                         the bottom teeth snapped off (the climb plant, plan sec 2.3 "Later").
//   IsGlassAt(position, radius)                 settled glass on the floor within radius of a foot (glass footsteps).
//
// Red 2026-10-22 ("broken glass stays on the floor; only when the player is far away may it be cleared to save memory"):
//   * Floor glass is never cleared on a timer and never while its chunk is built. "Too far" = the map releases the window's
//     chunk (WindowReleased): 48-96 m away, always beyond the camera's 46 m far plane. The meshes go with the chunk root.
//   * What stays is one compact record per edge (FrontRoomsGlassDebrisPack, about 2 KB). When the chunk is built again the
//     teeth and the floor glass come back from it (sliced, no physics). At most MaxRecords records: over that, the oldest
//     RELEASED record goes first (that window comes back with its teeth only: Red's literal rule).
//   * KeepFloorRecords = false is Red's literal rule everywhere (a released window comes back with teeth only).
using System;
using System.Collections.Generic;
using UnityEngine;

public static class FrontRoomsGlassDebris
{
    public enum Surface : byte { Carpet = 0, Hard = 1, Wall = 2, Furniture = 3, Glass = 4 }

    public struct Impact
    {
        public Vector3 position;
        public float mass, speed;
        public Surface surface;
        /// <summary>The shard's first contact with the floor (the landing tinkle; later contacts are slides and bounces).</summary>
        public bool firstLanding;
        public long edge;
    }

    /// <summary>A shard hit something: position, mass (kg), contact speed (m/s). Raised during the debris step (LateUpdate).</summary>
    public static event Action<Vector3, float, float> ShardImpact;
    /// <summary>The same contact with its surface (the sound chat muffles carpet).</summary>
    public static event Action<Impact> ShardImpactDetailed;
    /// <summary>The bottom teeth of a broken window snapped off (the climb). Position = their centre.</summary>
    public static event Action<Vector3> TeethSnap;

    /// <summary>Throttle of ShardImpact: minimum contact speed, minimum gap per shard, maximum per frame.</summary>
    public static float ImpactMinSpeed = .3f, ImpactMinGap = .05f;
    public static int ImpactMaxPerFrame = 16;
    /// <summary>Counters for logs and the lab (raised / dropped by the per-frame cap).</summary>
    public static int ImpactsRaised { get; private set; }
    public static int ImpactsDropped { get; private set; }
    static int impactFrame = -1, impactsThisFrame;

    internal static void RaiseImpact(Impact i)
    {
        if (Time.frameCount != impactFrame) { impactFrame = Time.frameCount; impactsThisFrame = 0; }
        if (impactsThisFrame >= ImpactMaxPerFrame) { ImpactsDropped++; return; }
        impactsThisFrame++;
        ImpactsRaised++;
        try { ShardImpact?.Invoke(i.position, i.mass, i.speed); } catch (Exception e) { Debug.LogException(e); }
        try { ShardImpactDetailed?.Invoke(i); } catch (Exception e) { Debug.LogException(e); }
    }

    internal static void RaiseTeethSnap(Vector3 at)
    {
        try { TeethSnap?.Invoke(at); } catch (Exception e) { Debug.LogException(e); }
    }

    /// <summary>One crumb resting on the floor or a sill (root-local).</summary>
    [Serializable]
    public struct RestingCrumb
    {
        public Vector3 position;
        public Quaternion rotation;
        public float size;
        public byte shape, kind;
        public Vector2 paneUV;
    }

    // ---- the compact records (gd3c sec 4, 6.3) ---------------------------------------------------------------------------
    /// <summary>Red's literal rule when false: a released window comes back with its teeth only (no floor record is kept).</summary>
    public static bool KeepFloorRecords = true;
    /// <summary>Records kept per session (about 1.1 MB at the desktop maximum); over it the oldest released one goes first.</summary>
    public static int MaxRecords = 512;

    public sealed class Entry
    {
        /// <summary>The map (FrontRoomsMapWorld) the edge belongs to; any Unity object works as the key (tests).</summary>
        public UnityEngine.Object map;
        public long edge;
        public byte[] data;
        /// <summary>The window's chunk is built now (its floor glass is drawn).</summary>
        public bool live;
        /// <summary>Order of the last release (0 = never released); the oldest released goes first over MaxRecords.</summary>
        public int releasedOrder;
        public int brokenOrder;
        public int Bytes => (data != null ? data.Length : 0) + 48;
    }

    struct Key : IEquatable<Key>
    {
        public int map;
        public long edge;
        public bool Equals(Key o) => map == o.map && edge == o.edge;
        public override bool Equals(object o) => o is Key k && Equals(k);
        public override int GetHashCode() => map * 486187739 ^ edge.GetHashCode();
    }

    static readonly Dictionary<Key, Entry> store = new Dictionary<Key, Entry>();
    static readonly List<Key> dead = new List<Key>();
    static int releaseCounter, brokenCounter;
    /// <summary>Records dropped by the MaxRecords budget this session (lab).</summary>
    public static int RecordsEvicted { get; private set; }

    static Key K(UnityEngine.Object map, long edge) => new Key { map = FrontRoomsObjectId.Of(map), edge = edge };

    public static Entry Find(UnityEngine.Object map, long edge)
    {
        if (map == null) return null;
        return store.TryGetValue(K(map, edge), out var e) ? e : null;
    }

    /// <summary>The debris world stores a settled window's record (tests may call it with any Unity object as the map).</summary>
    public static Entry Put(UnityEngine.Object map, long edge, byte[] data)
    {
        if (map == null || !KeepFloorRecords) return null;
        Prune();
        var k = K(map, edge);
        if (!store.TryGetValue(k, out var e)) { e = new Entry { map = map, edge = edge, brokenOrder = ++brokenCounter }; store[k] = e; }
        e.data = data;
        e.live = true;
        Enforce();
        return e;
    }

    public static void Remove(UnityEngine.Object map, long edge)
    {
        if (map != null) store.Remove(K(map, edge));
    }

    /// <summary>The window's chunk was released: its record may now be evicted (oldest released first).</summary>
    public static void MarkReleased(UnityEngine.Object map, long edge)
    {
        var e = Find(map, edge);
        if (e == null) return;
        e.live = false;
        e.releasedOrder = ++releaseCounter;
        Enforce();
    }

    public static void MarkLive(UnityEngine.Object map, long edge)
    {
        var e = Find(map, edge);
        if (e != null) e.live = true;
    }

    /// <summary>Over MaxRecords: drop the oldest released records (a live window's record is never dropped).</summary>
    static void Enforce()
    {
        while (store.Count > MaxRecords)
        {
            Key oldest = default;
            var best = int.MaxValue;
            foreach (var kv in store)
                if (!kv.Value.live && kv.Value.releasedOrder > 0 && kv.Value.releasedOrder < best) { best = kv.Value.releasedOrder; oldest = kv.Key; }
            if (best == int.MaxValue) break;
            store.Remove(oldest);
            RecordsEvicted++;
        }
    }

    public static int RecordCount { get { Prune(); return store.Count; } }

    /// <summary>Session memory of the records (bytes: blobs plus a fixed per-entry overhead).</summary>
    public static long RecordBytes
    {
        get
        {
            Prune();
            long b = 0;
            foreach (var e in store.Values) b += e.Bytes;
            return b;
        }
    }

    static void Prune()
    {
        dead.Clear();
        foreach (var kv in store) if (kv.Value.map == null) dead.Add(kv.Key);
        foreach (var k in dead) store.Remove(k);
    }

    /// <summary>Lab: forget every record (a new run).</summary>
    public static void ClearRecords() { store.Clear(); RecordsEvicted = 0; }

    // ---- glass footsteps -------------------------------------------------------------------------------------------------
    public const float CellSize = .05f;
    /// <summary>Glass this close above the floor counts for IsGlassAt (lying flat; a shard on a desk does not crunch).</summary>
    public const float FloorBand = .08f;
    internal static readonly List<FrontRoomsGlassFloorGlass> liveFloors = new List<FrontRoomsGlassFloorGlass>();

    public static long CellKey(float x, float z) => ((long)Mathf.FloorToInt(x / CellSize) << 32) ^ (uint)Mathf.FloorToInt(z / CellSize);

    /// <summary>
    /// True when settled glass (shards or crumbs, lying within FloorBand of the floor) is within <paramref name="radius"/> of
    /// <paramref name="position"/> (world; only x and z are compared, and the floor must be within 0.5 m below or above).
    /// Glass footsteps (sound chat). Cheap: a hash lookup per 5 cm cell in the circle, only for floor glass that is drawn.
    /// </summary>
    public static bool IsGlassAt(Vector3 position, float radius = .15f)
    {
        if (liveFloors.Count == 0) return false;
        var n = Mathf.CeilToInt(radius / CellSize);
        for (var i = liveFloors.Count - 1; i >= 0; i--)
        {
            var f = liveFloors[i];
            if (f == null) { liveFloors.RemoveAt(i); continue; }
            if (f.Cells.Count == 0) continue;
            var rp = f.transform.position;
            if (Mathf.Abs(position.y - rp.y) > .5f) continue;
            if ((new Vector2(position.x - rp.x, position.z - rp.z)).sqrMagnitude > 25f) continue;
            for (var dz = -n; dz <= n; dz++)
            for (var dx = -n; dx <= n; dx++)
            {
                if ((dx * dx + dz * dz) * CellSize * CellSize > (radius + CellSize) * (radius + CellSize)) continue;
                if (f.Cells.Contains(CellKey(position.x + dx * CellSize, position.z + dz * CellSize))) return true;
            }
        }
        return false;
    }

    /// <summary>
    /// The climb plant (plan sec 2.3 "Later"): the bottom teeth of the broken window at <paramref name="windowRoot"/> snap off,
    /// fall as crumbs, and TeethSnap is raised once at their centre. Returns the number of teeth snapped (0 when there are
    /// none, or they are gone already). The snapped mask goes into the window's record, so they stay gone after a rebuild.
    /// </summary>
    public static int SnapBottomTeeth(Transform windowRoot)
    {
        if (windowRoot == null) return 0;
        var floor = windowRoot.GetComponent<FrontRoomsGlassFloorGlass>();
        return floor != null ? floor.SnapBottomTeeth() : 0;
    }

    /// <summary>Plan step 9: snap the bottom teeth when the player climbs through (FrontRooms3DGame.PlayerClimbed).</summary>
    public static bool SnapTeethOnClimb = true;
}
