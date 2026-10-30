using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Stairwell A, production step 1 (game visual line; Red 2026-10-22 13:00 "look A", 15:5x "the stairwell is a safe
/// zone"): the look-A dressing of C22's one-cell stairwell X (FrontRoomsStairwell, the map line's) and of its EXIT
/// door's stair side (research/stairwell/10_directions.md 1.2-1.4, 2; NOTE_1022_red_safe_zone_READ_FIRST.md).
///
/// Hook (no game or map change): one dresser per map, added at FrontRooms3DGame.MapRunStarted (raised right after
/// map.Begin, which builds nothing, so the subscription stands before the first build); Attach also dresses an EXIT
/// already built (tools call it before Begin). ExitDoorBuilt(door, hinge, leaf, frame) comes on every build of the
/// EXIT edge (first build, RebuildChunk, a chunk streaming back, every floor after NextFloor) and there is no release
/// event, so each build is dressed from scratch on the fresh "Leaf rig" and "EXIT frame N-X" (never door.leaf, the
/// scaled cube); what hangs there goes with the chunk. The stairwell itself outlives its floor (the next floor starts
/// in it) and is dressed once, found by the EXIT's cell; its dress goes with it.
///
/// What it dresses:
/// - the EXIT door: the putty steel leaf and the hollow-metal frame finishes (A13's leaf / lock contrast needs the
///   pale leaf), three hinge knuckles, a 10 in kick plate and the STAIR A letter plate on the push face (maze side),
///   the regular-arm closer solved every frame from the swing and the NO REENTRY / NEAREST REENTRY ON LEVEL {to}
///   plate on the pull face (stair side). The crossbar, the EXIT sign and A13's chain and padlock are not this
///   file's: the map's and FrontRoomsExitChain's (Rendering/Exit). The sign's EmissionGain and no-shadow rule are
///   checked here (a warning if the map ever changes them);
/// - the stairwell: block walls in warm putty over the tan band to 1.1 m with its cut-in line, the painted slab,
///   the grey-enamel landing fill, hollow-metal frames, the stair ID signs (south wall LEVEL token(k), north wall
///   LEVEL token(k + 1)), sign 7 AREA OF RESCUE ASSISTANCE over the south ID sign (lit steady on the emergency
///   circuit: it stays on through the floor change's blackout), the far (north) door's REENTRY ON THIS FLOOR plate
///   and lever (NOT AN EXIT on its corridor face), the sealed south leaf's stair face (knuckles, closer, NO
///   REENTRY), a 4 ft two-lamp wraparound and the twin-head battery unit with its red AC pilot (mains: dark in the
///   blackout), and its own reflection probe;
/// - light (the safe zone): desktop = two soft-shadowed spots from the wraparound (the map's unshadowed point lamp
///   is switched off); phones and WebGL (the reduced tier) keep the map's lamp, no extra light, 1 px/mm
///   plates and a fixed reflection. Every stair light follows only the stairwell's LampLevel (the C22 blackout at
///   the floor change) and Visible (X's chunk built): never W1's dips, holds or WarnBurst (it is no map fixture, so
///   FrontRoomsWarnLamps never selects it), never CueInk (no print layer on any stair surface). The inside surfaces
///   are Stair_*Inside / planar Stair_* materials with _FR_OWN_LIGHTS, so the map's lamps (unshadowed 10-12 m spots
///   that light through the stair's walls and dip with W1) never reach the inside: this works with URP light layers
///   off (today) and replaces the rendering-layer separation the 10-22 draft planned.
/// </summary>
[DefaultExecutionOrder(1000)]
public sealed class FrontRoomsStairwellDresser : MonoBehaviour
{
    public const string LeafDressName = "STAIR A dress (EXIT leaf)", FrameDressName = "STAIR A dress (EXIT frame)";
    public const string ShellDressName = "STAIR A dress (stairwell)", SealedLeafDressName = "STAIR A dress (sealed leaf)";
    public const string FarDoorDressName = "STAIR A dress (far door)", SpotNamePrefix = "Stair A wraparound light ";

    /// <summary>Desktop spot intensity (each of two) and range (10_directions 2.4, tuned for the one-cell vestibule).</summary>
    public static float SpotIntensity = 2.2f, SpotRange = 4.2f;
    public static readonly Color SpotColour = new Color(1f, .955f, .9f);
    /// <summary>The wraparound lens, lit (linear; x LampLevel).</summary>
    public static readonly Color LensEmission = new Color(1f, .97f, .92f) * 2.4f;
    public static readonly Color PilotEmission = new Color(1f, .08f, .04f) * 1.4f;
    /// <summary>The closer's shoe: its back on the stairwell frame head's face, 0.19 m in from the wall line (the shoe kit's mount is 0.02 behind its origin).</summary>
    public const float ExitShoeOut = .21f, SealedShoeOut = .07f;

    /// <summary>Tools: force the reduced tier (the phone frame) in the editor.</summary>
    public static bool ForceReducedForTools;

    /// <summary>Phones and WebGL: no extra lights, 1 px/mm plates, a fixed reflection.</summary>
    public static bool ReducedTier
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return true;
#else
            return ForceReducedForTools || FrontRoomsHandheld.Active;
#endif
        }
    }

    // ---------------------------------------------------------------- bootstrap

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Boot()
    {
        FrontRooms3DGame.MapRunStarted -= OnMapRunStarted;
        FrontRooms3DGame.MapRunStarted += OnMapRunStarted;
        FrontRooms3DGame.FloorChanged -= OnFloorChanged;
        FrontRooms3DGame.FloorChanged += OnFloorChanged;
        registry.Clear();
    }

    static void OnMapRunStarted(FrontRoomsMapWorld map, FrontRoomsMapHunter relay)
    {
        var d = Attach(map);
        if (d != null) d.SetFloor(1);
    }

    static void OnFloorChanged(FrontRoomsMapWorld map, FrontRoomsMapHunter relay, int floor)
    {
        var d = map != null ? map.GetComponent<FrontRoomsStairwellDresser>() : null;
        if (d != null) d.SetFloor(floor);
    }

    /// <summary>The map's dresser (added on first call). Tools call it before Begin; the game through MapRunStarted.</summary>
    public static FrontRoomsStairwellDresser Attach(FrontRoomsMapWorld map)
    {
        if (map == null) return null;
        var d = map.GetComponent<FrontRoomsStairwellDresser>();
        if (d == null) d = map.gameObject.AddComponent<FrontRoomsStairwellDresser>();
        d.Bind(map);
        return d;
    }

    static readonly List<FrontRoomsStairwellDresser> registry = new List<FrontRoomsStairwellDresser>();

    /// <summary>The level of the stairwell light in this cell (0..1), or -1 when no dressed stairwell stands there (for EXAMINE's CellLightAt).</summary>
    public static float StairwellLampAt(FrontRoomsMapWorld map, GridCoord cell)
    {
        foreach (var d in registry)
        {
            if (d == null || d.map != map) continue;
            foreach (var s in d.shells)
                if (s.sw != null && s.sw.Cell == cell) return s.sw.Visible ? s.sw.LampLevel : 0f;
        }
        return -1f;
    }

    // ---------------------------------------------------------------- state

    FrontRoomsMapWorld map;
    int floor = 1;
    /// <summary>The stair the current floor's EXIT leads down: k = floor - 1 (LEVEL token(k) to token(k + 1)).</summary>
    public int StairK => Mathf.Max(0, floor - 1);
    public int Floor => floor;

    sealed class Closer
    {
        public Transform kitRoot, arm, fore, shoe;
        public Vector3 spindle;
        public Quaternion lastRot;
        public Vector3 lastPos;
        public bool solved;
    }

    sealed class ExitDress
    {
        public FrontRoomsMapWorld.Door door;
        public Transform leaf, frame, leafDress, frameDress;
        public Closer closer;
        public int k;
    }

    sealed class ShellDress
    {
        public FrontRoomsStairwell sw;
        public GameObject root, sealedLeaf;
        public Light mapLamp;
        public readonly List<Light> spots = new List<Light>();
        public Renderer lens, pilot;
        public int lensSlot = -1, pilotSlot = -1;
        public ReflectionProbe probe;
        public bool probeRendered;
        public float lastLevel = -1f;
        public int k;
        public Closer closer;
        public Transform housing, lensGo;
    }

    ExitDress exit;
    readonly List<ShellDress> shells = new List<ShellDress>();
    readonly List<FrontRoomsStairSigns.PlateSet> held = new List<FrontRoomsStairSigns.PlateSet>();
    readonly List<FrontRoomsStairSigns.Plate> pendingPlates = new List<FrontRoomsStairSigns.Plate>();
    int pendingK = -1;
    MaterialPropertyBlock block;

    public int ExitBuildsDressed { get; private set; }
    public int ShellsDressed { get; private set; }
    public FrontRoomsMapWorld Map => map;

    void Bind(FrontRoomsMapWorld m)
    {
        if (map == m) return;
        Unbind();
        map = m;
        map.ExitDoorBuilt += OnExitDoorBuilt;
        if (!registry.Contains(this)) registry.Add(this);
        CatchUp();
    }

    void Unbind()
    {
        if (map != null) map.ExitDoorBuilt -= OnExitDoorBuilt;
        map = null;
    }

    void OnDestroy()
    {
        Unbind();
        registry.Remove(this);
        foreach (var p in held) FrontRoomsStairSigns.Release(p);
        held.Clear();
    }

    /// <summary>
    /// The run's floor (1 = LEVEL 0). This floor's per-stair plates (the ID signs and the EXIT door's NO REENTRY) are
    /// baked one per frame from the next LateUpdate, off the floor change's frame; a dressing that needs one sooner
    /// bakes it then.
    /// </summary>
    public void SetFloor(int f)
    {
        floor = Mathf.Max(1, f);
        pendingK = StairK;
        pendingPlates.Clear();
        pendingPlates.Add(FrontRoomsStairSigns.Plate.ExitDoor);
        pendingPlates.Add(FrontRoomsStairSigns.Plate.IdSign);
        pendingPlates.Add(FrontRoomsStairSigns.Plate.IdSignFar);
        foreach (var p in held) FrontRoomsStairSigns.Release(p);
        held.Clear();
    }

    /// <summary>Tools: the floor and its plates at once (the harness drives NextFloor itself).</summary>
    public void SetFloorForTools(int f)
    {
        SetFloor(f);
        while (pendingPlates.Count > 0) BakeOnePending();
    }

    void BakeOnePending()
    {
        if (pendingPlates.Count == 0) return;
        var p = pendingPlates[0];
        pendingPlates.RemoveAt(0);
        var set = FrontRoomsStairSigns.Acquire(p, pendingK);
        if (set != null) held.Add(set);
    }

    void CatchUp()
    {
        if (map == null) return;
        var door = map.ExitDoor;
        if (door == null || door.hinge == null) return;
        var leaf = door.hinge.Find(FrontRoomsMapWorld.ExitLeafRigName);
        var frame = door.hinge.parent != null ? door.hinge.parent.Find("EXIT frame " + door.a + "-" + door.b) : null;
        if (leaf != null && frame != null) OnExitDoorBuilt(door, door.hinge, leaf, frame);
    }

    // ---------------------------------------------------------------- the EXIT door

    void OnExitDoorBuilt(FrontRoomsMapWorld.Door door, Transform hinge, Transform leaf, Transform frame)
    {
        if (door == null || hinge == null || leaf == null || frame == null) return;
        if (block == null) block = new MaterialPropertyBlock();
        // The stairwell first (it outlives the build); then the build itself, once.
        DressShellFor(map.ExitCell);
        if (leaf.Find(LeafDressName) != null) return;
        var k = StairK;
        var e = new ExitDress { door = door, leaf = leaf, frame = frame, k = k };

        // The push face: the side the map hung the crossbar on (the EXIT swings +1: +X, N's side), as A13 reads it.
        var bar = leaf.Find(FrontRoomsMapWorld.ExitCrossbarName);
        var push = bar != null ? (bar.localPosition.x >= 0f ? 1f : -1f) : (door.swing >= 0f ? 1f : -1f);
        var face = ModuleUnits.DoorLeafThickness * .5f;

        // Finishes: the map's cube leaf and its EXIT frame take the steel finishes (their renderers' material only).
        var leafRenderer = door.leaf != null ? door.leaf.GetComponent<Renderer>() : null;
        if (leafRenderer != null) leafRenderer.sharedMaterial = FrontRoomsStairKit.Leaf;
        foreach (var r in frame.GetComponentsInChildren<Renderer>(true))
        {
            if (r.transform != frame) continue;   // only the map's own frame mesh, never A13's or this dress's children
            r.sharedMaterial = FrontRoomsStairKit.Frame;
            // C23: the EXIT frame casts no shadow (the map's rule; kept if a rebuild ever forgets it).
            r.shadowCastingMode = ShadowCastingMode.Off;
        }
        var sign = map.ExitSign;
        if (sign != null)
        {
            if (Mathf.Abs(sign.EmissionGain - FrontRoomsMapWorld.ExitSignEmissionGain) > 1e-4f)
                Debug.LogWarning("[StairwellA] EXIT sign EmissionGain " + sign.EmissionGain + " (expected " + FrontRoomsMapWorld.ExitSignEmissionGain + ")");
            foreach (var r in sign.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
        }

        // On the leaf rig (swings, jolts and rattles with the leaf).
        var ld = NewChild(leaf, LeafDressName);
        var owner = ld.gameObject.AddComponent<FrontRoomsStairDressOwner>();
        e.leafDress = ld;
        Knuckles(ld, -push);
        // The pull face's skin (the safe zone): the map's cube leaf takes every light, so its stair face would dip with
        // W1 through the walls; a 0.8 mm skin in the stair's own-lights finish covers that face only.
        Skin(ld, leaf, door.leaf != null ? door.leaf.transform : null, -push, FrontRoomsStairKit.LeafInside);
        // Push face (the maze side): the kick plate and the STAIR A letter plate (SPEC #6).
        Part(ld, "kick plate", FrontRoomsStairKit.KickPlate(.93f), FrontRoomsStairKit.Kick,
            new Vector3(push * face, .012f, .5f), Quaternion.Euler(0f, 90f * push, 0f), ShadowCastingMode.Off);
        Plate(ld, owner, FrontRoomsStairSigns.Plate.LetterA, k, new Vector3(push * (face + PlateOut), 1.524f, .5f), new Vector3(push, 0f, 0f));
        // Pull face (the stair side): NO REENTRY / NEAREST REENTRY ON LEVEL {to} (SPEC #3).
        Plate(ld, owner, FrontRoomsStairSigns.Plate.ExitDoor, k, new Vector3(-push * (face + PlateOut), 1.524f, .5f), new Vector3(-push, 0f, 0f));

        // On the frame, in the door's shut frame (frame space: hinge.localPosition, door.closed).
        var fd = NewChild(frame, FrameDressName);
        fd.localPosition = hinge.localPosition;
        fd.localRotation = door.closed;
        e.frameDress = fd;
        // The closer: a regular arm on the stair side, its shoe on the stairwell frame head's face.
        e.closer = BuildCloser(ld, fd, ExitShoeOut, push);

        exit = e;
        ExitBuildsDressed++;
        SolveCloser(e.closer, true);
    }

    static Transform NewChild(Transform parent, string name)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }

    static MeshRenderer Part(Transform parent, string name, Mesh mesh, Material material, Vector3 position, Quaternion rotation, ShadowCastingMode shadows)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = rotation;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = shadows;
        r.lightProbeUsage = LightProbeUsage.Off;
        return r;
    }

    /// <summary>A plate on a face: its back on the surface at <paramref name="at"/>, its face along <paramref name="normal"/>.</summary>
    MeshRenderer Plate(Transform parent, FrontRoomsStairDressOwner owner, FrontRoomsStairSigns.Plate kind, int k, Vector3 at, Vector3 normal)
    {
        var set = FrontRoomsStairSigns.Acquire(kind, k);
        if (set == null) return null;
        owner.plates.Add(set);
        return Part(parent, "plate " + kind + (set.text != null ? " (" + set.text + ")" : ""), FrontRoomsStairKit.PlateSlab(set.size), set.material,
            at, Quaternion.LookRotation(-normal, Vector3.up), ShadowCastingMode.Off);
    }

    /// <summary>Plates on a skinned face stand this far off the leaf (the skin is SkinOut off it).</summary>
    const float PlateOut = .001f, SkinOut = .0008f;

    /// <summary>
    /// A skin over one face of a cube leaf (side +1: the leaf's +X face, -1: its -X face), parented to the leaf's rig so
    /// it swings and jolts with it: a quad SkinOut off the face, the leaf's size, in <paramref name="material"/>.
    /// </summary>
    static void Skin(Transform parent, Transform rig, Transform cube, float side, Material material)
    {
        if (parent == null || rig == null || cube == null) return;
        var size = cube.localScale;
        var centre = rig.InverseTransformPoint(cube.position);
        var x = side * (size.x * .5f + SkinOut);
        var mesh = FrontRoomsStairKit.SkinQuad(size.z, size.y, side);
        var r = Part(parent, "stair face skin", mesh, material, new Vector3(centre.x + x, centre.y, centre.z), Quaternion.identity, ShadowCastingMode.Off);
        r.receiveShadows = true;
    }

    // Three 4 1/2 in ball-bearing butt hinges on the hinge edge, knuckles on the pull side (the leaf swings into X).
    static void Knuckles(Transform leafDress, float pullSign)
    {
        foreach (var y in new[] { .29f, 1.07f, 1.86f })
            Part(leafDress, "hinge knuckle", FrontRoomsStairKit.Knuckle, FrontRoomsStairKit.HingeSteel,
                new Vector3(pullSign * (ModuleUnits.DoorLeafThickness * .5f + .008f), y, -.002f), Quaternion.identity, ShadowCastingMode.On);
    }

    // ---------------------------------------------------------------- the closer (regular arm, stair side)

    /// <summary>
    /// Kit_DoorCloser_* on the pull face. The kits are authored in a door root with +X on the pull side; the leaf's
    /// and the frame's +X here is the push face (push = +1), so the kits hang under roots mirrored in X (scale
    /// -push, 1, 1). Body at the leaf's closer mount (the map leaf's face is 0.025), shoe shoeOut in front of the
    /// wall's centre line.
    /// </summary>
    static Closer BuildCloser(Transform leafSide, Transform frameSide, float shoeOut, float push)
    {
        if (!FrontRoomsKitLibrary.Has("Kit_DoorCloser_Body")) return null;
        var c = new Closer();
        var kitLeaf = NewChild(leafSide, "closer (kit root, pull side)");
        kitLeaf.localScale = new Vector3(-push, 1f, 1f);
        var kitFrame = NewChild(frameSide, "closer shoe (kit root, pull side)");
        kitFrame.localScale = new Vector3(-push, 1f, 1f);
        var face = ModuleUnits.DoorLeafThickness * .5f;
        var rs = Quaternion.Euler(0f, 90f, 0f);
        var mount = new Vector3(face, 2.027f, .545f);
        Spawn("Kit_DoorCloser_Body", kitLeaf, mount, rs);
        c.spindle = mount + rs * new Vector3(.025f, .0405f, .04f);
        c.shoe = Spawn("Kit_DoorCloser_Shoe", kitFrame, new Vector3(shoeOut, 2.13f, .10f), rs);
        c.arm = Spawn("Kit_DoorCloser_Arm", kitLeaf, c.spindle, Quaternion.identity);
        c.fore = Spawn("Kit_DoorCloser_Forearm", kitLeaf, c.spindle, Quaternion.identity);
        c.kitRoot = kitLeaf;
        return c;
    }

    static Transform Spawn(string kit, Transform parent, Vector3 position, Quaternion rotation)
    {
        var go = FrontRoomsKitLibrary.Spawn(kit, parent, position, rotation, null, false, kit, false);
        return go != null ? go.transform : null;
    }

    /// <summary>The arm's elbow from the spindle (on the leaf) and the shoe (fixed): circle (spindle, 0.24) and circle
    /// (shoe, 0.26), the root farther from the leaf (kit +X); solved again whenever the leaf moved.</summary>
    static void SolveCloser(Closer c, bool force)
    {
        if (c == null || c.kitRoot == null || c.shoe == null || c.arm == null || c.fore == null) return;
        var rot = c.kitRoot.rotation;
        var pos = c.kitRoot.position;
        if (!force && c.solved && rot == c.lastRot && pos == c.lastPos) return;
        c.lastRot = rot;
        c.lastPos = pos;
        c.solved = true;
        const float a = .24f, b = .26f;
        var shoe = c.kitRoot.InverseTransformPoint(c.shoe.position);
        var p0 = new Vector2(c.spindle.x, c.spindle.z);
        var p1 = new Vector2(shoe.x, shoe.z);
        var dist = Mathf.Max(1e-4f, Vector2.Distance(p0, p1));
        var along = (a * a - b * b + dist * dist) / (2f * dist);
        var h = Mathf.Sqrt(Mathf.Max(0f, a * a - along * along));
        var dir = (p1 - p0) / dist;
        var mid = p0 + dir * Mathf.Min(along, a);
        var perp = new Vector2(-dir.y, dir.x);
        var e1 = mid + perp * h;
        var e2 = mid - perp * h;
        var e = e1.x >= e2.x ? e1 : e2;
        c.arm.localPosition = c.spindle;
        c.arm.localRotation = Quaternion.Euler(0f, Yaw(p0, e), 0f);
        c.fore.localPosition = new Vector3(e.x, c.spindle.y, e.y);
        c.fore.localRotation = Quaternion.Euler(0f, Yaw(e, p1), 0f);
    }

    static float Yaw(Vector2 from, Vector2 to) => Mathf.Atan2(-(to.y - from.y), to.x - from.x) * Mathf.Rad2Deg;

    // ---------------------------------------------------------------- the stairwell

    void DressShellFor(GridCoord cell)
    {
        foreach (var s in shells) if (s.sw != null && s.sw.Cell == cell && s.sw.Map == map) return;
        foreach (var sw in FindObjectsByType<FrontRoomsStairwell>(FindObjectsInactive.Exclude))
            if (sw != null && sw.Map == map && sw.Cell == cell && sw.transform.Find(ShellDressName) == null)
            {
                DressShell(sw, StairK);
                return;
            }
    }

    /// <summary>Tools: dress a stairwell for stair k at once (the harness builds its own).</summary>
    public void DressShellForTools(FrontRoomsStairwell sw, int k)
    {
        if (sw != null && sw.transform.Find(ShellDressName) == null) DressShell(sw, k);
    }

    void DressShell(FrontRoomsStairwell sw, int k)
    {
        if (block == null) block = new MaterialPropertyBlock();
        var t = sw.transform;
        var s = new ShellDress { sw = sw, k = k };
        // The shell's own boxes take look A (named as FrontRoomsStairwell.Build and Doorway make them). The north end
        // wall (JoinNextFloor) is the map's paper on the next floor's corridor and is left as it is.
        foreach (Transform c in t)
        {
            var r = c.GetComponent<Renderer>();
            var n = c.name;
            if (r != null)
            {
                if (n.StartsWith("Wall ")) r.sharedMaterial = FrontRoomsStairKit.Block;
                else if (n == "Floor") r.sharedMaterial = FrontRoomsStairKit.Fill;
                else if (n == "Ceiling") r.sharedMaterial = FrontRoomsStairKit.Slab;
                else if (n.StartsWith("Frame ")) r.sharedMaterial = FrontRoomsStairKit.FrameInside;
            }
            if (n == "Lamp housing") s.housing = c;
            if (n == "Lamp lens") s.lensGo = c;
        }
        // The south leaf (static, shown from the seal on) is seen only from inside; the north leaf is the next floor's
        // start door and is seen from its corridor too, so it takes every light.
        SetLeaf(t, "Door hinge stairwell south", FrontRoomsStairKit.LeafInside);
        SetLeaf(t, "Door hinge stairwell north", FrontRoomsStairKit.Leaf);
        s.mapLamp = sw.Lamp;

        var root = NewChild(t, ShellDressName);
        var owner = root.gameObject.AddComponent<FrontRoomsStairDressOwner>();
        s.root = root.gameObject;
        const float half = ModuleUnits.Cell * .5f;
        var inner = half - FrontRoomsStairwell.Inset - FrontRoomsStairwell.ShellThickness;
        var ceiling = FrontRoomsStairwell.CeilingHeight;

        // The band to 1.1 m and its cut-in line, painted over the block (overlays 1.5 / 2 mm off the inner faces).
        var southGap = FrontRoomsStairwell.SouthOpeningWidth * .5f + ModuleUnits.TrimFace;
        var northGap = FrontRoomsStairwell.NorthOpeningWidth * .5f + ModuleUnits.TrimFace;
        var band = new FrontRoomsStairKit.MB();
        var cut = new FrontRoomsStairKit.MB();
        BandWall(band, cut, new Vector3(-inner, 0f, 0f), Vector3.right, Vector3.forward, -inner, inner, 0f);
        BandWall(band, cut, new Vector3(inner, 0f, 0f), Vector3.left, Vector3.back, -inner, inner, 0f);
        BandWall(band, cut, new Vector3(0f, 0f, -inner), Vector3.forward, Vector3.left, -inner, inner, southGap);
        BandWall(band, cut, new Vector3(0f, 0f, inner), Vector3.back, Vector3.right, -inner, inner, northGap);
        var bandMesh = band.ToMesh("Stair A / band " + sw.Cell);
        var cutMesh = cut.ToMesh("Stair A / cut-in " + sw.Cell);
        owner.meshes.Add(bandMesh);
        owner.meshes.Add(cutMesh);
        Part(root, "block band (tan semi-gloss)", bandMesh, FrontRoomsStairKit.Band, Vector3.zero, Quaternion.identity, ShadowCastingMode.Off);
        Part(root, "band cut-in line", cutMesh, FrontRoomsStairKit.CutInLine, Vector3.zero, Quaternion.identity, ShadowCastingMode.Off);

        // The stair ID signs, latch side (east) of each door, centre 5 ft (UBC 1991 3306(p); SPEC #2).
        const float idX = .88f, idY = 1.524f;
        Plate(root, owner, FrontRoomsStairSigns.Plate.IdSign, k, new Vector3(idX, idY, -inner), Vector3.forward);
        Plate(root, owner, FrontRoomsStairSigns.Plate.IdSignFar, k, new Vector3(idX, idY, inner), Vector3.back);
        // Sign 7 (SPEC v1.3): centred over the south ID sign, a 2 in gap, box bottom 1.753 m; lit steady.
        var idTop = idY + FrontRoomsStairSigns.SizeOf(FrontRoomsStairSigns.Plate.IdSign).y * .5f;
        var rescue = NewChild(root, "sign 7 AREA OF RESCUE ASSISTANCE");
        rescue.localPosition = new Vector3(idX, idTop + .0508f + FrontRoomsStairKit.RescueBox.y * .5f, -inner);
        rescue.localRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        Part(rescue, "box (white enamel steel)", FrontRoomsStairKit.RescueBoxMesh, FrontRoomsStairKit.SignBox, Vector3.zero, Quaternion.identity, ShadowCastingMode.On);
        Part(rescue, "face (lit, emergency circuit)", FrontRoomsStairKit.RescueFaceMesh, FrontRoomsStairKit.RescueFaceMaterial, Vector3.zero, Quaternion.identity, ShadowCastingMode.Off);

        // The far (north) door: its hinge's +X is X's side. REENTRY ON THIS FLOOR and a lever on it; NOT AN EXIT outside.
        var north = t.Find("Door hinge stairwell north");
        if (north != null)
        {
            var nd = NewChild(north, FarDoorDressName);
            var face = ModuleUnits.DoorLeafThickness * .5f;
            // its stair face in the own-lights finish (the corridor face keeps the leaf's: it is seen from the next floor)
            var northLeaf = north.Find("Door leaf");
            Skin(nd, north, northLeaf, 1f, FrontRoomsStairKit.LeafInside);
            Plate(nd, owner, FrontRoomsStairSigns.Plate.FarDoor, k, new Vector3(face + PlateOut, 1.524f, .5f), Vector3.right);
            Plate(nd, owner, FrontRoomsStairSigns.Plate.NotAnExit, k, new Vector3(-face, 1.524f, .5f), Vector3.left);
            var rose = Spawn("Kit_Lock_Rose", nd, new Vector3(face, .968f, .92f), Quaternion.Euler(0f, 90f, 0f));
            if (rose != null) Spawn("Kit_Lock_Lever", rose, new Vector3(0f, 0f, .01f), Quaternion.identity);
        }

        // The sealed south leaf (shown from the seal on: the EXIT door seen from X on this floor and the next): its
        // stair face, as the map leaf's: knuckles, the closer (its shoe on the frame head's face), NO REENTRY. The
        // south hinge's +X faces N, so the stair side is -X (push = +1 as the map's EXIT).
        var south = t.Find("Door hinge stairwell south");
        if (south != null)
        {
            var sd = NewChild(south, SealedLeafDressName);
            Knuckles(sd, -1f);
            Plate(sd, owner, FrontRoomsStairSigns.Plate.ExitDoor, k, new Vector3(-ModuleUnits.DoorLeafThickness * .5f, 1.524f, .5f), Vector3.left);
            var lining = NewChild(south, "lining side");
            s.closer = BuildCloser(sd, lining, SealedShoeOut, 1f);
            SolveCloser(s.closer, true);
            s.sealedLeaf = sd.gameObject;
            lining.SetParent(sd, true);
        }

        // The 4 ft two-lamp wraparound on the slab, across X (it replaces the map's housing and lens).
        var wrap = Spawn("Kit_WrapFixture_4ft", root, new Vector3(0f, ceiling, 0f), Quaternion.identity);
        if (wrap != null)
        {
            foreach (var r in wrap.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                var mats = r.sharedMaterials;
                for (var i = 0; i < mats.Length; i++)
                    if (mats[i] != null && mats[i].name.StartsWith("Run_WrapLens")) { s.lens = r; s.lensSlot = i; }
            }
            if (s.housing != null) s.housing.gameObject.SetActive(false);
            if (s.lensGo != null) s.lensGo.gameObject.SetActive(false);
        }
        else Debug.LogWarning("[StairwellA] Kit_WrapFixture_4ft is missing: the map's lamp housing stays");

        // The twin-head battery unit, high on the hinge-side (west) wall by the EXIT, normal power: heads dark, the red
        // AC pilot lit (two 6 V sealed-beam heads, one on each door).
        var cab = Spawn("Kit_EmergencyTwinHead", root, new Vector3(-inner + .05f, 1.98f, -.95f), Quaternion.Euler(0f, 90f, 0f));
        if (cab != null)
        {
            foreach (var cr in cab.GetComponentsInChildren<Renderer>(true))
            {
                var mats = cr.sharedMaterials;
                for (var i = 0; i < mats.Length; i++)
                    if (mats[i] != null && mats[i].name.StartsWith("Run_NeonPilot")) { s.pilot = cr; s.pilotSlot = i; }
            }
            var aims = new[] { t.TransformPoint(new Vector3(0f, 1.0f, -inner)), t.TransformPoint(new Vector3(0f, .2f, inner)) };
            var mounts = new[] { new Vector3(.105f, .191f, .008f), new Vector3(-.105f, .191f, .008f) };
            for (var i = 0; i < 2; i++)
            {
                var yoke = Spawn("Kit_EmergencyTwinHead_Yoke", cab, mounts[i], Quaternion.identity);
                if (yoke == null) continue;
                var head = Spawn("Kit_EmergencyTwinHead_Lamp", yoke, new Vector3(0f, .085f, 0f), Quaternion.identity);
                if (head == null) continue;
                var dir = aims[i] - head.position;
                var dCab = cab.InverseTransformDirection(dir);
                yoke.localRotation = Quaternion.Euler(0f, Mathf.Atan2(dCab.x, dCab.z) * Mathf.Rad2Deg, 0f);
                var dYoke = yoke.InverseTransformDirection(dir);
                head.localRotation = Quaternion.Euler(Mathf.Atan2(-dYoke.y, dYoke.z) * Mathf.Rad2Deg, 0f, 0f);
            }
            // EMT from the unit to the slab
            var feed = root.InverseTransformPoint(cab.TransformPoint(new Vector3(0f, .182f, -.012f)));
            Part(root, "unit feed EMT", FrontRoomsStairKit.EmtRun(Mathf.Max(.05f, ceiling - feed.y)), FrontRoomsStairKit.Conduit, feed, Quaternion.identity, ShadowCastingMode.On);
        }

        // Light: desktop = two soft-shadowed spots under the wraparound; the reduced tier keeps the map's lamp.
        if (!ReducedTier)
        {
            foreach (var x in new[] { -.3f, .3f })
            {
                var go = new GameObject(SpotNamePrefix + (x < 0f ? "W" : "E"));
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3(x, ceiling - .09f, 0f);
                go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var l = go.AddComponent<Light>();
                l.type = LightType.Spot;
                l.spotAngle = 170f;
                l.innerSpotAngle = 120f;
                l.range = SpotRange;
                l.color = SpotColour;
                l.intensity = SpotIntensity;
                l.shadows = LightShadows.Soft;
                l.shadowStrength = .92f;
                l.shadowNearPlane = .05f;
                l.shadowBias = .02f;
                l.shadowNormalBias = .3f;
                var extra = go.AddComponent<UniversalAdditionalLightData>();
                extra.usePipelineSettings = false;
                s.spots.Add(l);
            }
            if (s.mapLamp != null) s.mapLamp.enabled = false;
        }

        // X's own reflection (not Level 0's): desktop a realtime box probe rendered once when lit and shown; the reduced
        // tier a fixed one (a small cube of the room's mean colour).
        var pgo = new GameObject("Stair A reflection");
        pgo.transform.SetParent(root, false);
        pgo.transform.localPosition = new Vector3(0f, ceiling * .5f, 0f);
        var probe = pgo.AddComponent<ReflectionProbe>();
        probe.size = new Vector3(2f * inner, ceiling, 2f * inner);
        probe.center = Vector3.zero;
        probe.boxProjection = true;
        probe.importance = 10;
        probe.blendDistance = 0f;
        probe.resolution = 64;
        probe.hdr = true;
        probe.nearClipPlane = .05f;
        probe.farClipPlane = 6f;
        if (!ReducedTier && QualitySettings.realtimeReflectionProbes)
        {
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
        }
        else
        {
            probe.mode = ReflectionProbeMode.Custom;
            probe.customBakedTexture = FixedCube();
            s.probeRendered = true;
        }
        s.probe = probe;

        shells.Add(s);
        ShellsDressed++;
        Tick(s);
    }

    static void SetLeaf(Transform stairwell, string hinge, Material material)
    {
        var h = stairwell.Find(hinge);
        var leaf = h != null ? h.Find("Door leaf") : null;
        var lr = leaf != null ? leaf.GetComponent<Renderer>() : null;
        if (lr != null) lr.sharedMaterial = material;
    }

    static Cubemap fixedCube;
    static Cubemap FixedCube()
    {
        if (fixedCube != null) return fixedCube;
        var c = new Cubemap(8, TextureFormat.RGBAHalf, false) { name = "Stair A fixed reflection", hideFlags = HideFlags.DontSave };
        // the room's mean radiance under its lamp: putty block at about the walk path's post level
        var wall = (Color)FrontRoomsStairKit.BlockPutty.linear * .32f;
        var floorC = (Color)FrontRoomsStairKit.TreadFill.linear * .28f;
        var lamp = new Color(1.4f, 1.36f, 1.3f);
        var px = new Color[64];
        foreach (var f in new[] { CubemapFace.PositiveX, CubemapFace.NegativeX, CubemapFace.PositiveZ, CubemapFace.NegativeZ, CubemapFace.PositiveY, CubemapFace.NegativeY })
        {
            var col = f == CubemapFace.NegativeY ? floorC : f == CubemapFace.PositiveY ? Color.Lerp(wall, lamp, .25f) : wall;
            for (var i = 0; i < px.Length; i++) px[i] = col;
            c.SetPixels(px, f);
        }
        c.Apply(false, true);
        fixedCube = c;
        return c;
    }

    // One wall's band from x0 to x1 along 'along' (0 at 'origin'), leaving out |x| < gap (a doorway and its frame).
    static void BandWall(FrontRoomsStairKit.MB band, FrontRoomsStairKit.MB cut, Vector3 origin, Vector3 normal, Vector3 along, float x0, float x1, float gap)
    {
        var spans = gap > 0f ? new[] { new Vector2(x0, -gap), new Vector2(gap, x1) } : new[] { new Vector2(x0, x1) };
        foreach (var s in spans)
        {
            if (s.y - s.x < .01f) continue;
            var o = origin + normal * .0015f;
            var oc = origin + normal * .002f;
            var y0 = FrontRoomsStairKit.BandTop - FrontRoomsStairKit.CutInHeight * .5f;
            var y1 = FrontRoomsStairKit.BandTop + FrontRoomsStairKit.CutInHeight * .5f;
            band.Quad(o + along * s.x, o + along * s.y, o + along * s.y + Vector3.up * FrontRoomsStairKit.BandTop, o + along * s.x + Vector3.up * FrontRoomsStairKit.BandTop, normal,
                new Vector2(s.x, 0f), new Vector2(s.y, 0f), new Vector2(s.y, FrontRoomsStairKit.BandTop), new Vector2(s.x, FrontRoomsStairKit.BandTop));
            cut.Quad(oc + along * s.x + Vector3.up * y0, oc + along * s.y + Vector3.up * y0, oc + along * s.y + Vector3.up * y1, oc + along * s.x + Vector3.up * y1, normal,
                new Vector2(s.x, y0), new Vector2(s.y, y0), new Vector2(s.y, y1), new Vector2(s.x, y1));
        }
    }

    // ---------------------------------------------------------------- every frame

    void LateUpdate()
    {
        if (!Application.isPlaying) return;
        if (pendingPlates.Count > 0) BakeOnePending();
        Tick();
    }

    /// <summary>Tools in edit mode, where LateUpdate does not run: one frame of the mirror (lights, closer).</summary>
    public void TickForTools() => Tick();

    void Tick()
    {
        for (var i = shells.Count - 1; i >= 0; i--)
        {
            var s = shells[i];
            if (s.sw == null || s.root == null) { shells.RemoveAt(i); continue; }
            Tick(s);
        }
        if (exit != null)
        {
            if (exit.leafDress == null) exit = null;
            else SolveCloser(exit.closer, false);
        }
    }

    void Tick(ShellDress s)
    {
        var visible = s.sw.Visible;
        var level = s.sw.LampLevel;
        if (s.root.activeSelf != visible) s.root.SetActive(visible);
        if (s.sealedLeaf != null)
        {
            var sealedShown = visible && s.sw.Sealed;
            if (s.sealedLeaf.activeSelf != sealedShown) s.sealedLeaf.SetActive(sealedShown);
        }
        // The map's housing and lens stay off under the wraparound; its lamp stays off where the spots replace it
        // (FrontRoomsStairwell turns it back on with every SetVisible / LampLevel: the stair's light level only).
        if (s.lens != null)
        {
            if (s.housing != null && s.housing.gameObject.activeSelf) s.housing.gameObject.SetActive(false);
            if (s.lensGo != null && s.lensGo.gameObject.activeSelf) s.lensGo.gameObject.SetActive(false);
        }
        if (s.spots.Count > 0 && s.mapLamp != null && s.mapLamp.enabled) s.mapLamp.enabled = false;
        if (Mathf.Abs(level - s.lastLevel) > 1e-4f)
        {
            s.lastLevel = level;
            foreach (var l in s.spots)
            {
                l.intensity = SpotIntensity * level;
                l.enabled = level > .001f;
            }
            if (block == null) block = new MaterialPropertyBlock();
            if (s.lens != null && s.lensSlot >= 0)
            {
                s.lens.GetPropertyBlock(block, s.lensSlot);
                block.SetColor("_EmissionColor", LensEmission * level);
                s.lens.SetPropertyBlock(block, s.lensSlot);
            }
            if (s.pilot != null && s.pilotSlot >= 0)
            {
                // the pilot shows mains power: dark in the blackout
                s.pilot.GetPropertyBlock(block, s.pilotSlot);
                block.SetColor("_EmissionColor", PilotEmission * Mathf.Clamp01(level * 2f - 1f));
                s.pilot.SetPropertyBlock(block, s.pilotSlot);
            }
            if (s.probe != null) s.probe.intensity = level;
        }
        if (!s.probeRendered && visible && level >= .999f && s.probe != null && s.probe.mode == ReflectionProbeMode.Realtime)
        {
            s.probe.RenderProbe();
            s.probeRendered = true;
        }
        SolveCloser(s.closer, false);
    }

    /// <summary>Tools: render every dressed stairwell's realtime probe now (edit-mode captures).</summary>
    public void RenderProbesForTools()
    {
        foreach (var s in shells)
            if (s.probe != null && s.probe.mode == ReflectionProbeMode.Realtime)
            {
                s.probe.RenderProbe();
                s.probeRendered = true;
            }
    }

    /// <summary>Tools: the dressed stairwells' roots and lights (budgets, the lamp trace).</summary>
    public void ShellsForTools(List<FrontRoomsStairwell> into, List<Light> lights)
    {
        foreach (var s in shells)
        {
            if (s.sw == null) continue;
            if (into != null) into.Add(s.sw);
            if (lights != null)
            {
                lights.AddRange(s.spots);
                if (s.spots.Count == 0 && s.mapLamp != null) lights.Add(s.mapLamp);
            }
        }
    }

    /// <summary>Tools: counts for the leak and duplicate checks.</summary>
    public void CountsForTools(Dictionary<string, int> into)
    {
        into["shells"] = shells.Count;
        into["shellsDressed"] = ShellsDressed;
        into["exitBuildsDressed"] = ExitBuildsDressed;
        into["heldPlates"] = held.Count;
        into["pendingPlates"] = pendingPlates.Count;
    }
}
