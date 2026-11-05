using FrontRooms.Map;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// A13 (Red 2026-10-22; research/stairwell/10_directions.md 1.2), R1 since 2026-10-31 (Red: the lock had no structure
/// and the unlock no micro cut scene; research/unlock/10_research.md, 20_design.md): the floor's EXIT door shows its
/// floor-key lock in the world, on the maze side, built as the period's after-hours chain was:
/// <list type="bullet">
/// <item><description>LOCKED: a 3/8 in proof-coil chain threaded through a staple on a steel plate in the wall beside
/// the latch jamb, across the latch case, one turn round the crossbar, its two ends joined by a 2 in laminated padlock
/// that hangs with its back on the latch case (FrontRoomsExitChainKit, FrontRoomsExitPadlock).</description></item>
/// <item><description>UNLOCKED: the open padlock jammed against the staple, the chain hanging from it down the wall,
/// its tail on the floor, as the cut's fall comes to rest (FrontRoomsExitChainKit.UnlockedNodes, the same after the
/// cut, on every rebuild and floor, and with no cut).</description></item>
/// <item><description>Between them, on E - UNLOCK, the micro cut scene (FrontRoomsExitUnlockCut) on the map's unlock
/// shot's clock: the key goes into the padlock's keyway, turns, the body drops off the shackle at the commit, the
/// shackle swivels, the end link slips off, the chain falls off the bar and runs through the staple, the key comes
/// out.</description></item>
/// </list>
/// The EXIT sign is the map's and stays LIT in both (C23 LockLit); nothing of it is touched here. Owner: the game visual line.
///
/// Hook (no game change): one component per map, added at FrontRooms3DGame.MapRunStarted (before the first build;
/// Attach also dresses an EXIT already built). The map (FrontRoomsMapWorld): ExitDoorBuilt(door, hinge, leaf, frame) on
/// every build of the EXIT edge, dressed from scratch on the fresh "Leaf rig" and "EXIT frame N-X"; ExitLocked, read
/// at the build and every frame; ExitLockChanged(door, locked) on real changes. The unlock shot is read from the map's
/// KeyTurnOwner (the game's FrontRoomsDoorShot: Live, Clock, Committed, Door), never written.
///
/// LOCKED hangs on the leaf rig (it jolts with the leaf on a rattle); UNLOCKED, the staple and the cut's live chain on
/// the frame, in the door's shut frame (hinge.localPosition, door.closed). Both states are built with the door and
/// switched by SetActive. With no unlock shot on the door (tools, the walker, a lock lifted by SetExitLocked) the
/// change is one frame, as before; with one, the cut plays from the shot's start and UNLOCKED takes over at its end.
///
/// The map's LockPose seam (contract unlock_contract C1, 关卡设计's to land): Keyway(map, door, from, out pose) gives the
/// padlock's keyway for the EXIT, so the shot dips to the padlock instead of the latch case's face.
/// </summary>
public sealed class FrontRoomsExitChain : MonoBehaviour
{
    public const string LeafDressName = "A13 dress (EXIT leaf)", FrameDressName = "A13 dress (EXIT frame)";
    public const string LockedName = "A13 LOCKED (chain + padlock)", UnlockedName = "A13 UNLOCKED (chain hanging from the staple)";
    public const string AnchorName = "A13 chain staple", ChainName = "chain", PadlockName = "padlock", CutChainName = "A13 cut chain (live)";

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
        public GameObject locked, unlocked, unlockedChain;
        public FrontRoomsExitPadlock unlockedPadlock;
        public FrontRoomsExitUnlockCut cut;
        public bool shownLocked;
    }

    FrontRoomsMapWorld map;
    Dress current;

    /// <summary>EXIT builds dressed so far (tools).</summary>
    public int BuildsDressed { get; private set; }
    /// <summary>LOCKED/UNLOCKED changes on a dressed door after its build (tools: one per unlock, never more).</summary>
    public int LockSwitches { get; private set; }
    /// <summary>Micro cut scenes started / played to their end / eased back by a cancel before the release (tools).</summary>
    public int CutsStarted { get; private set; }
    public int CutsFinished { get; private set; }
    public int CutsCancelled { get; private set; }
    /// <summary>The dressed EXIT shows LOCKED now (false with none dressed, and while the cut plays).</summary>
    public bool ShowingLocked => current != null && current.locked != null && current.shownLocked && current.cut == null;
    /// <summary>The cut playing now, or null (tools, captures).</summary>
    public FrontRoomsExitUnlockCut Cut => current != null ? current.cut : null;
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
        if (current != null && current.cut != null) current.cut.Kill();
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
        // A cut on the old build goes with it (the chunk is gone).
        if (current != null && current.cut != null)
        {
            current.cut.Kill();
            current.cut = null;
        }

        // The push face: the side the map hung the crossbar on (the EXIT swings +1, so +X; mirrored for -1, as the map mirrors the crossbar).
        var bar = leaf.Find(FrontRoomsMapWorld.ExitCrossbarName);
        var push = bar != null ? (bar.localPosition.x >= 0f ? 1f : -1f) : (door.swing >= 0f ? 1f : -1f);
        var mirror = new Vector3(push, 1f, 1f);

        var d = new Dress { door = door, leaf = leaf };
        d.leafDress = Group(leaf, LeafDressName, Vector3.zero, Quaternion.identity, mirror);
        d.frameDress = Group(frame, FrameDressName, hinge.localPosition, door.closed, mirror);
        // LOCKED: the chain round the bar and through the staple, the padlock shut on its two end links
        d.locked = Group(d.leafDress, LockedName, Vector3.zero, Quaternion.identity, Vector3.one).gameObject;
        States(d.locked.transform, true);
        FrontRoomsExitPadlock.Build(d.locked.transform, PadlockName, FrontRoomsExitChainKit.LockedBody, false);
        // UNLOCKED: the chain hanging from the staple, the padlock open against it (the cut's padlock too)
        d.unlocked = Group(d.frameDress, UnlockedName, Vector3.zero, Quaternion.identity, Vector3.one).gameObject;
        d.unlockedChain = States(d.unlocked.transform, false);
        d.unlockedPadlock = FrontRoomsExitPadlock.Build(d.unlocked.transform, PadlockName, FrontRoomsExitChainKit.UnlockedBody, true);
        // the wall anchor, in both states
        Anchor(d.frameDress);

        // The state at the build (a lock set before Begin, kept through rebuilds and revisits), not counted as a switch.
        d.shownLocked = WantLocked(d);
        Show(d, d.shownLocked);
        current = d;
        BuildsDressed++;
    }

    void OnExitLockChanged(FrontRoomsMapWorld.Door door, bool locked) => Apply();

    void LateUpdate()
    {
        if (!Application.isPlaying) return;
        var d = current;
        if (d != null && Valid(d))
        {
            if (d.cut == null) TryStartCut(d);
            if (d.cut != null && !d.cut.Step(Time.deltaTime)) EndCut(d);
        }
        Apply();
    }

    bool Valid(Dress d) => d.leafDress != null && d.frameDress != null && d.locked != null && d.unlocked != null;

    // The game's unlock shot has started on this EXIT (before its commit): the cut takes the lock from here.
    void TryStartCut(Dress d)
    {
        if (map == null || !d.shownLocked || d.door == null || !map.IsExitDoor(d.door)) return;
        var shot = map.KeyTurnOwner as FrontRoomsDoorShot;
        if (shot == null || !shot.Live || shot.Committed || shot.Door == null || shot.Door.edge != d.door.edge) return;
        if (shot.Clock >= FrontRoomsExitUnlockCut.Release) return;
        d.cut = new FrontRoomsExitUnlockCut(map, d.door, d.frameDress, d.unlockedPadlock, shot.Clock) { LeafDress = d.leafDress };
        // the cut's own objects stand in for both states while it plays
        d.locked.SetActive(false);
        d.unlocked.SetActive(true);
        if (d.unlockedChain != null) d.unlockedChain.SetActive(false);
        CutsStarted++;
    }

    void EndCut(Dress d)
    {
        var cut = d.cut;
        d.cut = null;
        if (cut.Cancelling) CutsCancelled++;
        else CutsFinished++;
        var was = d.shownLocked;
        if (d.unlockedPadlock != null) d.unlockedPadlock.Pose(FrontRoomsExitChainKit.UnlockedPadlockAt, FrontRoomsExitChainKit.UnlockedPadlockRotation,
            FrontRoomsExitChainKit.PopTravel, FrontRoomsExitChainKit.SwivelDeg, 0f);
        if (d.unlockedChain != null) d.unlockedChain.SetActive(true);
        d.shownLocked = WantLocked(d);
        Show(d, d.shownLocked);
        if (was != d.shownLocked) LockSwitches++;
    }

    /// <summary>Read the map's lock now and show it (LateUpdate in play; edit-mode tools call it). No allocation. While the cut plays it has the lock.</summary>
    public void Apply()
    {
        var d = current;
        if (d == null) return;
        if (!Valid(d))
        {
            // the build went with its chunk or the floor
            if (d.cut != null) d.cut.Kill();
            current = null;
            return;
        }
        if (d.cut != null) return;
        var want = WantLocked(d);
        if (want == d.shownLocked) return;
        d.shownLocked = want;
        Show(d, want);
        LockSwitches++;
    }

    // Both in the same frame: never neither, never both.
    static void Show(Dress d, bool locked)
    {
        d.locked.SetActive(locked);
        d.unlocked.SetActive(!locked);
    }

    bool WantLocked(Dress d) => map != null && map.ExitLocked && map.IsExitDoor(d.door) && !d.door.broken && d.door.angle < 1f;

    // ---------------------------------------------------------------- the map's LockPose seam (contract C1)

    /// <summary>
    /// The EXIT's keyway for the map's LockPose (contract unlock_contract C1): the shut padlock's plug face on
    /// <paramref name="door"/> (world), +Z into the lock (up through its bottom), +Y the lock's face. False when the
    /// map has no A13 dresser, the door is not the dressed EXIT, or it is open or broken: the map keeps its spec keyhole.
    /// Reads only this component's own objects; no allocation.
    /// </summary>
    public static bool Keyway(FrontRoomsMapWorld map, FrontRoomsMapWorld.Door door, Vector3 from, out Pose keyway)
    {
        keyway = new Pose(from, Quaternion.identity);
        if (map == null || door == null || door.broken || door.angle >= 1f) return false;
        var d = map.GetComponent<FrontRoomsExitChain>();
        var c = d != null ? d.current : null;
        if (c == null || c.door == null || c.door.edge != door.edge || c.leafDress == null) return false;
        var k = FrontRoomsExitChainKit.LockedKeyway;
        var t = c.leafDress;
        var into = t.TransformDirection(k.rotation * Vector3.forward);
        var up = t.TransformDirection(k.rotation * Vector3.up);
        if (into.sqrMagnitude < 1e-8f || up.sqrMagnitude < 1e-8f) return false;
        keyway = new Pose(t.TransformPoint(k.position), Quaternion.LookRotation(into.normalized, up.normalized));
        return true;
    }

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

    // One state's chain: its three levels of detail under one LODGroup (never culled: the lock reads at 30 m).
    static GameObject States(Transform parent, bool locked)
    {
        var root = Group(parent, ChainName, Vector3.zero, Quaternion.identity, Vector3.one);
        var lods = new LOD[3];
        for (var i = 0; i < 3; i++)
        {
            var mesh = locked ? FrontRoomsExitChainKit.Locked(i) : FrontRoomsExitChainKit.Unlocked(i);
            var r = Part(root, "LOD" + i, mesh, FrontRoomsExitChainKit.Chain);
            lods[i] = new LOD(FrontRoomsExitChainKit.LodHeights[i], new Renderer[] { r });
        }
        var group = root.gameObject.AddComponent<LODGroup>();
        group.SetLODs(lods);
        group.RecalculateBounds();
        FrontRoomsLodFade.Apply(group);
        return root.gameObject;
    }

    // The wall anchor: Kit_ChainStaple at the plate's spot (a stand-in when the kit is missing).
    static void Anchor(Transform frameDress)
    {
        const string kit = FrontRoomsExitChainKit.StapleKit;
        var at = FrontRoomsExitChainKit.StapleAnchor;
        var rot = FrontRoomsExitChainKit.StapleRotation;
        GameObject go = null;
        if (FrontRoomsKitLibrary.Has(kit)) go = FrontRoomsKitLibrary.Spawn(kit, frameDress, at, rot, Vector3.one, false, AnchorName, false);
        if (go != null) return;
        var t = Group(frameDress, AnchorName, at, rot, Vector3.one);
        Part(t, "stand-in", FrontRoomsExitChainKit.StandIn(kit), FrontRoomsExitChainKit.Steel);
    }

    static MeshRenderer Part(Transform parent, string name, Mesh mesh, Material material)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = ShadowCastingMode.On;
        r.lightProbeUsage = LightProbeUsage.Off;
        return r;
    }
}
