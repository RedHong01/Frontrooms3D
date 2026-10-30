using FrontRooms.Map;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// A13 (Red 2026-10-22; research/stairwell/10_directions.md 1.2): the floor's EXIT door shows its floor-key lock in
/// the world, on the maze side. One state change, no blink:
/// <list type="bullet">
/// <item><description>LOCKED: a dark 1/2 in proof-coil chain wrapped twice round the crossbar near its latch end,
/// through an eye bolt in the wall beside the latch jamb, its ends joined by a laminated-steel padlock under the bar
/// (the HUD padlock's family, 3113:6999).</description></item>
/// <item><description>UNLOCKED: the chain coiled on the floor against the wall on the latch side, the padlock open.</description></item>
/// </list>
/// The EXIT sign is the map's and stays LIT in both (C23 LockLit); nothing of it is touched here. Owner: the game visual line.
///
/// Hook (no game change): one component per map, added at FrontRooms3DGame.MapRunStarted, which the game raises
/// right after map.Begin(player, false); Begin builds nothing (chunks come from the map's Update), so the
/// subscription is in place before the first build, and Attach also dresses an EXIT already built (catch-up). Tools
/// call Attach before Begin. The map (the map line's C23 round 3 API, FrontRoomsMapWorld 5c57cfa5):
/// <list type="bullet">
/// <item><description>ExitDoorBuilt(door, hinge, leaf, frame) on every build of the EXIT edge (first build,
/// RebuildChunk, a chunk streaming back, every floor after NextFloor), raised once X's chunk is registered: each
/// build is dressed from scratch on the fresh "Leaf rig" (leaf, unscaled; never door.leaf, the scaled cube) and
/// "EXIT frame N-X" (frame). What hangs there goes with the chunk (there is no release event); the meshes and
/// materials are FrontRoomsExitChainKit's, shared, so nothing is made per build but a few objects.</description></item>
/// <item><description>ExitLocked, read in that handler (it covers a lock set before Begin, as PlanExit does), and
/// ExitLockChanged(door, locked) on real changes: E - UNLOCK's committed key turn raises (door, false) at 0.87 s on
/// the unlock shot's clock, then DoorUnlocked. ExitLocked goes back to false on SetExitArea / ClearExitArea /
/// NextFloor with no event, so the state is also read every frame (a bool, no allocation).</description></item>
/// </list>
/// LOCKED hangs on the leaf rig (it jolts with a rattle); UNLOCKED and the eye bolt on the frame, in the door's shut
/// frame (hinge.localPosition, door.closed). Both states are built with the door and switched by SetActive, so the
/// change is one frame and builds nothing. The chain is shown LOCKED only on a shut, whole EXIT that the map holds
/// locked; open, broken or no longer the EXIT, it lies on the floor.
/// </summary>
public sealed class FrontRoomsExitChain : MonoBehaviour
{
    public const string LeafDressName = "A13 dress (EXIT leaf)", FrameDressName = "A13 dress (EXIT frame)";
    public const string LockedName = "A13 LOCKED (chain + padlock)", UnlockedName = "A13 UNLOCKED (chain on the floor)", EyeBoltName = "A13 eye bolt";

    // ---------------------------------------------------------------- hook

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Boot()
    {
        FrontRooms3DGame.MapRunStarted -= OnMapRunStarted;
        FrontRooms3DGame.MapRunStarted += OnMapRunStarted;
    }

    static void OnMapRunStarted(FrontRoomsMapWorld map, FrontRoomsMapHunter relay) => Attach(map);

    /// <summary>The map's A13 dresser, added on the first call and bound to the map's EXIT events. Tools call it before Begin.</summary>
    public static FrontRoomsExitChain Attach(FrontRoomsMapWorld map)
    {
        if (map == null) return null;
        var d = map.GetComponent<FrontRoomsExitChain>();
        if (d == null) d = map.gameObject.AddComponent<FrontRoomsExitChain>();
        d.Bind(map);
        return d;
    }

    // ---------------------------------------------------------------- state

    sealed class Dress
    {
        public FrontRoomsMapWorld.Door door;
        public Transform leaf, leafDress, frameDress;
        public GameObject locked, unlocked;
        public bool shownLocked;
    }

    FrontRoomsMapWorld map;
    Dress current;

    /// <summary>EXIT builds dressed so far (tools).</summary>
    public int BuildsDressed { get; private set; }
    /// <summary>LOCKED/UNLOCKED changes on a dressed door after its build (tools: one per unlock, never more).</summary>
    public int LockSwitches { get; private set; }
    /// <summary>The dressed EXIT shows LOCKED now (false with none dressed).</summary>
    public bool ShowingLocked => current != null && current.locked != null && current.shownLocked;
    /// <summary>The EXIT leaf rig dressed now, or null.</summary>
    public Transform DressedLeaf => current != null && current.leafDress != null ? current.leaf : null;
    public FrontRoomsMapWorld Map => map;

    void Bind(FrontRoomsMapWorld m)
    {
        if (map == m) return;
        Unbind();
        map = m;
        map.ExitDoorBuilt += OnExitDoorBuilt;
        map.ExitLockChanged += OnExitLockChanged;
        CatchUp();
    }

    void Unbind()
    {
        if (map != null)
        {
            map.ExitDoorBuilt -= OnExitDoorBuilt;
            map.ExitLockChanged -= OnExitLockChanged;
        }
        map = null;
        current = null;
    }

    void OnDestroy() => Unbind();

    // An EXIT built before the subscription (a tool that attached late): dress it as its build would have been.
    void CatchUp()
    {
        var door = map != null ? map.ExitDoor : null;
        if (door == null || door.hinge == null) return;
        var leaf = door.hinge.Find(FrontRoomsMapWorld.ExitLeafRigName);
        var frame = door.hinge.parent != null ? door.hinge.parent.Find("EXIT frame " + door.a + "-" + door.b) : null;
        if (leaf != null && frame != null) OnExitDoorBuilt(door, door.hinge, leaf, frame);
    }

    // ---------------------------------------------------------------- the EXIT door's build

    void OnExitDoorBuilt(FrontRoomsMapWorld.Door door, Transform hinge, Transform leaf, Transform frame)
    {
        if (door == null || hinge == null || leaf == null || frame == null) return;
        // Once per build: a catch-up and the event never both dress the same rig.
        if (leaf.Find(LeafDressName) != null) return;

        // The push face: the side the map hung the crossbar on (the EXIT swings +1, so +X; mirrored for -1, as the map mirrors the crossbar).
        var bar = leaf.Find(FrontRoomsMapWorld.ExitCrossbarName);
        var push = bar != null ? (bar.localPosition.x >= 0f ? 1f : -1f) : (door.swing >= 0f ? 1f : -1f);
        var mirror = new Vector3(push, 1f, 1f);

        var d = new Dress { door = door, leaf = leaf };
        d.leafDress = Group(leaf, LeafDressName, Vector3.zero, Quaternion.identity, mirror);
        d.frameDress = Group(frame, FrameDressName, hinge.localPosition, door.closed, mirror);
        d.locked = States(d.leafDress, LockedName, true);
        d.unlocked = States(d.frameDress, UnlockedName, false);
        Part(d.frameDress, EyeBoltName, FrontRoomsExitChainKit.EyeBolt(), FrontRoomsExitChainKit.Steel, false);

        // The state at the build (a lock set before Begin, kept through rebuilds and revisits), not counted as a switch.
        d.shownLocked = WantLocked(d);
        d.locked.SetActive(d.shownLocked);
        d.unlocked.SetActive(!d.shownLocked);
        current = d;
        BuildsDressed++;
    }

    void OnExitLockChanged(FrontRoomsMapWorld.Door door, bool locked) => Apply();

    void LateUpdate()
    {
        if (Application.isPlaying) Apply();
    }

    /// <summary>Read the map's lock now and show it (LateUpdate in play; edit-mode tools call it). No allocation.</summary>
    public void Apply()
    {
        var d = current;
        if (d == null) return;
        if (d.leafDress == null || d.locked == null || d.unlocked == null)
        {
            // the build went with its chunk or the floor
            current = null;
            return;
        }
        var want = WantLocked(d);
        if (want == d.shownLocked) return;
        d.shownLocked = want;
        // Both in the same frame: never neither, never both.
        d.locked.SetActive(want);
        d.unlocked.SetActive(!want);
        LockSwitches++;
    }

    bool WantLocked(Dress d) => map != null && map.ExitLocked && map.IsExitDoor(d.door) && !d.door.broken && d.door.angle < 1f;

    // ---------------------------------------------------------------- objects

    static Transform Group(Transform parent, string name, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = position;
        t.localRotation = rotation;
        t.localScale = scale;
        return t;
    }

    // One state: its three levels of detail under one LODGroup (never culled: the lock reads at 30 m).
    static GameObject States(Transform parent, string name, bool locked)
    {
        var root = Group(parent, name, Vector3.zero, Quaternion.identity, Vector3.one);
        var lods = new LOD[3];
        for (var i = 0; i < 3; i++)
        {
            var mesh = locked ? FrontRoomsExitChainKit.Locked(i) : FrontRoomsExitChainKit.Unlocked(i);
            var r = Part(root, "LOD" + i, mesh, null, true);
            lods[i] = new LOD(FrontRoomsExitChainKit.LodHeights[i], new Renderer[] { r });
        }
        var group = root.gameObject.AddComponent<LODGroup>();
        group.fadeMode = LODFadeMode.None;
        group.SetLODs(lods);
        group.RecalculateBounds();
        return root.gameObject;
    }

    static readonly Material[] pair = new Material[2];

    static MeshRenderer Part(Transform parent, string name, Mesh mesh, Material single, bool twoMaterials)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        if (twoMaterials)
        {
            pair[0] = FrontRoomsExitChainKit.Chain;
            pair[1] = FrontRoomsExitChainKit.Steel;
            r.sharedMaterials = pair;
        }
        else r.sharedMaterial = single;
        r.shadowCastingMode = ShadowCastingMode.On;
        r.lightProbeUsage = LightProbeUsage.Off;
        return r;
    }
}
