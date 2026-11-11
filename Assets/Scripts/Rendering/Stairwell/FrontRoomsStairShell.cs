using System;
using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// The walkable stair (game visual line; Red 2026-10-22 13:00 "选 A、C22 合进去之后做真楼梯", 22:47 "这种楼梯间其实是安全区";
/// 2026-11-10 11:44 / 12:02 "现在就开"): the two-level switchback shell in X1 / X2 of the map contract
/// research/stairwell/60_walkable_stair_for_map.md v1.1 (sections 3 and 6), in look A (10_directions.md 1.1-1.3 and 2), built by the
/// stage STAIR author (80_stairwell_a_production.md section 6; W/apply/stairwell_walkable_contract/README.md for the map line).
///
/// Hook (inert until the map calls it): at load this file sets FrontRooms.Map.FrontRoomsStairShellHost.Create (the map line's factory,
/// the DoorMemberFromMap pattern). The map calls it only behind its profile flag walkableStair (default off); with the flag off, or with no
/// factory, the game keeps C22's one-cell vestibule (FrontRoomsStairwell) exactly as today, still dressed by FrontRoomsStairwellDresser.
///
/// Frame (the shell's Root): origin = X1's door-edge centre on the exit floor's floor plane (the factory's second argument), +Z = the door
/// normal into the stair (+z map north on every floor, 60 s1), +X = the EXIT's latch side (60 s6.2: the EXIT hinges on -x), Y up.
/// Exit landing y 0, turn landing -1.75, far landing (the shaft floor) -3.50; inner faces x +-1.33, z 0.17 (door wall) .. 5.83 (far
/// wall): the walls stand inset WallHalf + trim + 0.01 = 0.11 and 0.06 thick, C22's south-doorway numbers, so the landed dresser's EXIT
/// closer shoe lands on this exit frame as it lands on the vestibule's. Flight 1 (exit -> turn) runs down +z on x 0.21 .. 1.33, flight 2
/// (turn -> far) down -z on x -1.33 .. -0.21, the well 0.42 m (60 s6.1).
///
/// What it builds: the CMU shell in Stair_Block over the tan band to 1.1 m that follows the flights (Stair_Band, Stair_CutIn), the slab at
/// +3.30, the shaft floor; the kits Kit_StairFlight_SteelPan x2, Kit_StairLanding_SteelPan x2, Kit_StairGuard_Flight x2, Kit_StairRail_Wall
/// x2, Kit_StairGuard_Landing_1540 / _420, Kit_StandpipeHoseValve x2 on the red 4 in riser, Kit_StairBarrier_2660 (ADAAG 4.4.2); the
/// static shut EXIT leaf; the far door (95 deg into the stair, lever on its stair face); the stair ID plates and sign 7 at both door
/// landings (stairwell_a's, per floor); three Run! B wraparounds and the twin-heads over both doors; two box probes; and the walkable
/// colliders: one ramp per flight on the nosing line, a box per landing, thin guard boxes (60 s6.3).
///
/// The safe zone (Red 10-22): the shell's lights follow only SetLampLevel (the seal blackout, any power state) and the camera's distance
/// (off beyond LightWakeDistance); they are no map fixture, so FrontRoomsWarnLamps never selects them; every inside surface is a Stair_*
/// own-lights material (_FR_OWN_LIGHTS: map lamps over 6 m never reach in), and every stair spot's range is under 6 m. Sign 7 stays lit
/// on the emergency circuit. Movable in one frame (60 s6.7): no Static flags, realtime lights only, probes as children.
/// Tiers: desktop = two soft-shadowed spots per fixture and realtime probes rendered once; the reduced tier (phones, WebGL:
/// FrontRoomsStairwellDresser.ReducedTier) = one unshadowed spot per fixture, fixed probes, 1 px/mm plates (the plates' own rule).
/// </summary>
[DefaultExecutionOrder(1000)]
public sealed class FrontRoomsStairShell : MonoBehaviour, IFrontRoomsStairShell, IFrontRoomsStairShellVisible
{
    // ---------------------------------------------------------------- measures (metres, the shell frame)
    public const float Half = ModuleUnits.Cell * .5f;
    /// <summary>As C22's vestibule: half a wall, the 2 cm a map door frame stands proud, 1 cm clear.</summary>
    public const float Inset = ModuleUnits.WallHalf + ModuleUnits.TrimProud + .01f;
    public const float ShellThickness = .06f;
    /// <summary>The inner faces: x +-InnerX, z DoorWallZ (door wall) .. FarWallZ (far wall).</summary>
    public const float InnerX = Half - Inset - ShellThickness;
    public const float DoorWallZ = Inset + ShellThickness;
    public const float FarWallZ = 2f * ModuleUnits.Cell - Inset - ShellThickness;
    public const float FloorToFloorMetres = 3.5f, TurnY = -1.75f, FarY = -3.5f, SlabY = 3.3f, SlabThickness = .2f;
    public const float Rise = .175f, Going = .28f;
    public const int Treads = 9;
    /// <summary>The flights' nosing-0 edges (the exit landing's and the turn landing's flight edges).</summary>
    public const float ExitEdgeZ = 1.74f, TurnEdgeZ = 4.26f;
    public const float FlightX = .77f, FlightHalf = .56f, WellHalf = .21f;
    /// <summary>C22's depth gate: the seal plane 1.40 m in from the EXIT's line, past the leaf's swing and inside the 1.57 m landing (60 s4).</summary>
    public const float SealDepth = 1.4f;
    /// <summary>The exit opening: the vestibule's south doorway (the map leaf swings in through it); the far opening: a map door.</summary>
    public const float ExitOpeningWidth = ModuleUnits.DoorWidth + 2f * FrontRoomsStairwell.LeafRoom;
    public const float FarOpeningWidth = ModuleUnits.DoorWidth;
    public const float OpeningHeight = ModuleUnits.DoorHeight;
    /// <summary>The far-door wall's corridor face (half a wall past the line, where a map wall's face would be) and its height over the far
    /// landing for a Standard N-prime (2.9 m + the 0.16 m slab). The built top follows FarCeiling (FarWallTopFor), 关卡设计 11-10 ask (a).</summary>
    public const float CorridorFaceZ = -ModuleUnits.WallHalf;
    public const float FarWallTop = ModuleUnits.StandardCeiling + ModuleUnits.CeilingSlab;

    /// <summary>The thickening's top over the far landing for N-prime's ceiling <paramref name="ceiling"/>: that ceiling and its slab, never
    /// lower than 5 cm over the far opening's head trim.</summary>
    public static float FarWallTopFor(float ceiling) =>
        Mathf.Max(ceiling + ModuleUnits.CeilingSlab, OpeningHeight + ModuleUnits.TrimFace + .05f);

    /// <summary>The turn landing's wraparound (Run! B 4 ft) on the +x side wall, flight 1's outer wall, over the turn landing: its middle
    /// along the landing (z) and its height over the landing (the 10-22 wall mount's 2.55 m). Seen end-on from flight 1.</summary>
    public const float TurnLightZ = 5.0f, TurnLightHeight = 2.55f;
    /// <summary>The red 4 in riser on the +x wall (the valves' axis), 90 mm off the wall.</summary>
    public static readonly Vector2 RiserXZ = new Vector2(InnerX - .09f, .80f);
    public const float RiserRadius = .05715f;
    /// <summary>ADAAG 1991 4.4.2: where flight 1's underside drops below 80 in over the shaft floor.</summary>
    public const float BarrierZ = 3.72f;

    // ---------------------------------------------------------------- light (look A, 10_directions 1.3 / 2.4; safe zone)
    public static float SpotIntensity = 2.2f, SpotRange = 5.6f, ReducedIntensity = 2.6f, ReducedRange = 5.6f;
    public static readonly Color SpotColour = new Color(1f, .955f, .9f);
    public static readonly Color LensEmission = new Color(1f, .97f, .92f) * 2.4f;
    public static readonly Color PilotEmission = new Color(1f, .08f, .04f) * 1.4f;
    /// <summary>The shell's lights are on only while the camera is this near the shell (closed shaft: nothing of it is seen from farther).</summary>
    public static float LightWakeDistance = 14f;

    // ---------------------------------------------------------------- the kits (W/structures_assets/kits/frontrooms_kit/assets/structures_stair_*.py)
    public const string FlightKit = "Kit_StairFlight_SteelPan", LandingKit = "Kit_StairLanding_SteelPan", GuardKit = "Kit_StairGuard_Flight";
    public const string RailKit = "Kit_StairRail_Wall", LandingGuardKit = "Kit_StairGuard_Landing_1540", WellGuardKit = "Kit_StairGuard_Landing_420";
    public const string ValveKit = "Kit_StandpipeHoseValve", BarrierKit = "Kit_StairBarrier_2660";
    public const string SteelName = "Stair_SteelEnamel", RedName = "Stair_RedEnamel", BrassName = "Stair_ValveBrass";
    /// <summary>
    /// The block and the band of the walkable shell: Stair_Block / Stair_Band in mesh-UV metres of the shell's own frame and no
    /// world-space macro wear. The landed pair projects in world space, so the +3.50 m move would slide the CMU courses half a
    /// course (17.5 courses of 0.2 m) and shift the macro tone at the change; these move with the shell, so the change shows nothing.
    /// </summary>
    public const string BlockWalkName = "Stair_BlockWalk", BandWalkName = "Stair_BandWalk";
    public static Material BlockWalk => FrontRoomsStairKit.Get(BlockWalkName);
    public static Material BandWalk => FrontRoomsStairKit.Get(BandWalkName);
    /// <summary>The landed door kits (stairwell_doors): the static shut EXIT leaf and the far door use them when present.</summary>
    public const string ExitLeafKit = "Kit_DoorLeaf_StairWide", ExitFrameKit = "Kit_DoorFrame_StairSouth";
    public const string FarLeafKit = "Kit_DoorLeaf_StairNorth", FarFrameKit = "Kit_DoorFrame_StairNorth";

    /// <summary>The kits of the walkable stair are in the library (else plain stand-ins: a stepped flight, landing slabs, no guards' detail).</summary>
    public static bool KitsAvailable =>
        FrontRoomsKitLibrary.Has(FlightKit) && FrontRoomsKitLibrary.Has(LandingKit) && FrontRoomsKitLibrary.Has(GuardKit) && FrontRoomsKitLibrary.Has(RailKit);

    /// <summary>
    /// The stencil floor numeral is 平面视觉's art and waits (Red 2026-11-10 11:11 "目前先别做平面视觉相关的工作"): this is its clean hook.
    /// Set by their code when it lands; called with each door landing's anchor (hinge side of the door, the numeral's centre) and the floor
    /// label the map passed to SetFloor. Null today: nothing is painted.
    /// </summary>
    public static Action<FrontRoomsStairShell, Transform, string> StencilPainter { get; set; }

    // ---------------------------------------------------------------- registration (the map's factory)
    static readonly List<FrontRoomsStairShell> live = new List<FrontRoomsStairShell>();
    /// <summary>Shells alive now (the leak check reads it).</summary>
    public static int Alive => live.Count;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Register()
    {
        live.Clear();
        FrontRoomsStairShellHost.Create = Factory;
    }

    /// <summary>Tools: take the factory away (the game then keeps C22's vestibule), or put it back.</summary>
    public static void SetRegistered(bool on)
    {
        if (on) FrontRoomsStairShellHost.Create = Factory;
        else if (FrontRoomsStairShellHost.Create == Factory) FrontRoomsStairShellHost.Create = null;
    }

    static IFrontRoomsStairShell Factory(Transform parent, Vector3 x1DoorEdgeCentre, Vector3 doorNormal) => Create(parent, x1DoorEdgeCentre, doorNormal);

    /// <summary>
    /// Build a shell under <paramref name="parent"/> (the game's root, never the map: the shell outlives the floor it was made on) with its
    /// Root at <paramref name="x1DoorEdgeCentre"/> (X1's door-edge centre on the exit floor's floor plane) facing <paramref name="doorNormal"/>
    /// (into the stair; flattened to the ground plane). Everything stands at once: colliders on, renderers on, lights by distance.
    /// </summary>
    public static FrontRoomsStairShell Create(Transform parent, Vector3 x1DoorEdgeCentre, Vector3 doorNormal)
    {
        var go = new GameObject("STAIR shell (walkable)");
        if (parent != null) go.transform.SetParent(parent, false);
        var n = new Vector3(doorNormal.x, 0f, doorNormal.z);
        if (n.sqrMagnitude < 1e-6f) n = Vector3.forward;
        go.transform.SetPositionAndRotation(x1DoorEdgeCentre, Quaternion.LookRotation(n.normalized, Vector3.up));
        var s = go.AddComponent<FrontRoomsStairShell>();
        s.Build();
        return s;
    }

    // ---------------------------------------------------------------- state
    FrontRoomsStairDressOwner owner;
    Transform dress, latchAnchor, stencilExit, stencilFar;
    readonly List<Renderer> paper = new List<Renderer>();
    readonly List<Light> spots = new List<Light>();
    readonly List<float> spotBase = new List<float>();
    readonly List<Renderer> lenses = new List<Renderer>(), pilots = new List<Renderer>();
    readonly List<int> lensSlots = new List<int>(), pilotSlots = new List<int>();
    readonly List<ReflectionProbe> probes = new List<ReflectionProbe>();
    readonly List<FrontRoomsStairSigns.PlateSet> held = new List<FrontRoomsStairSigns.PlateSet>();
    readonly List<GameObject> plateParts = new List<GameObject>();
    MaterialPropertyBlock block;
    float lampLevel = 1f, appliedLevel = -1f;
    bool lightsAwake, appliedAwake, probesRendered, reduced, kits, doorKits;
    // IFrontRoomsStairShellVisible: renderers drawn (forceRenderingOff otherwise) and lights allowed; colliders never change
    bool visible = true, appliedVisible = true;
    readonly List<Renderer> allRenderers = new List<Renderer>();
    // the shell's boxes, merged per material (shell-frame planar metre UVs), and its colliders
    readonly Dictionary<Material, FrontRoomsStairKit.MB> batches = new Dictionary<Material, FrontRoomsStairKit.MB>();
    Transform colliderRoot;
    // the far-door wall's corridor thickening (N-prime's paper): one renderer whose mesh is rebuilt, and its three colliders resized,
    // whenever FarCeiling moves its top (BuildFarWall)
    MeshFilter farWallFilter;
    readonly BoxCollider[] farWallColliders = new BoxCollider[3];
    float farWallBuiltTop = float.NaN;
    // the far door's closer (stair face), solved again whenever the leaf moved
    Closer farCloser;
    int exitFloor = -1;
    string exitLabel = "", farLabel = "";
    float farCeiling = ModuleUnits.StandardCeiling;

    // the static shut EXIT leaf (shown after NextFloor: ShowExitLeafShut)
    Transform exitHinge;
    readonly List<Renderer> exitLeafRenderers = new List<Renderer>();
    Collider exitLeafCollider;
    bool exitLeafShown;
    Closer exitCloser;

    // the far door
    enum DoorPhase { Shut, Hold, Lever, Swing, Open, Shutting, Latched }
    DoorPhase phase;
    Transform farHinge, farLever;
    Collider farLeafCollider;
    Quaternion farClosed;
    float farAngle, clock, shutFrom, shutSeconds, holdGive, leverAngle, leverDownSign = -1f;
    static readonly Collider[] overlap = new Collider[8];

    public event Action FarDoorMoved;
    public event Action FarDoorLatched;

    // ---------------------------------------------------------------- IFrontRoomsStairShell
    public Transform Root => transform;
    /// <summary>The EXIT doorway's centre on its cell line, at the exit floor's floor plane.</summary>
    public Vector3 ExitDoorPoint => transform.position;
    /// <summary>The door normal into the stair: the seal plane stands at ExitDoorPoint + ExitDoorForward * SealDepth (SealPlanePoint).</summary>
    public Vector3 ExitDoorForward => transform.forward;
    public Vector3 SealPlanePoint => transform.TransformPoint(new Vector3(0f, 0f, SealDepth));
    /// <summary>The far doorway's centre on the cell line at the far landing (after the +3.50 m move: the new floor's floor plane).</summary>
    public Vector3 FarDoorPoint => transform.TransformPoint(new Vector3(0f, FarY, 0f));
    public Transform FarDoorHinge => farHinge;
    public float FloorToFloor => FloorToFloorMetres;
    public Transform LatchSoundAnchor => latchAnchor;

    /// <summary>
    /// The autopilot's walk, world space: exit landing (past the seal plane), flight 1 (its foot on the turn landing, past the well
    /// guard's end), turn, flight 2 (its head), the far landing (flight 2's foot), the far door (the latch-side stop outside the 0.98 m
    /// swing). Every point keeps a 0.3 m capsule clear of the guards, rails and the riser.
    /// </summary>
    public Vector3[] PathWaypoints
    {
        get
        {
            var w = new Vector3[LocalPath.Length];
            for (var i = 0; i < w.Length; i++) w[i] = transform.TransformPoint(LocalPath[i]);
            return w;
        }
    }

    static readonly Vector3[] LocalPath =
    {
        new Vector3(.70f, 0f, 1.50f),          // exit landing
        new Vector3(.77f, TurnY, 4.62f),       // flight 1 (its foot)
        new Vector3(0f, TurnY, 5.15f),         // turn
        new Vector3(-.77f, TurnY, 4.62f),      // flight 2 (its head)
        new Vector3(-.77f, FarY, 1.40f),       // far landing (flight 2's foot)
        new Vector3(-.85f, FarY, .62f),        // far door: the latch-side stop
    };

    /// <summary>The path the progress is measured on (the map's definition, IFrontRoomsStairShell): the EXIT door's line (0), the walk,
    /// the far door's line (1); 12.37 m of walk. The seal plane (1.40 m) reads 0.10, flight 2's foot 0.87; the game's arrival at 0.9 falls on
    /// the far landing 0.36 m past the foot of flight 2, 1.45 m from the far hinge (outside the 0.98 m leaf's sweep).</summary>
    static readonly Vector3[] ProgressPath =
    {
        new Vector3(0f, 0f, 0f),
        new Vector3(.70f, 0f, 1.50f),
        new Vector3(.77f, TurnY, 4.62f),
        new Vector3(0f, TurnY, 5.15f),
        new Vector3(-.77f, TurnY, 4.62f),
        new Vector3(-.77f, FarY, 1.40f),
        new Vector3(0f, FarY, 0f),
    };

    /// <summary>0 on the EXIT door's line, 1 on the far door's line, rising along the walk (a point off the path reads the nearest point
    /// of it); the levels are told apart by height (y weighs twice), so the exit landing and the far landing, one over the other, never mix.</summary>
    public float PathProgress(Vector3 feet)
    {
        var p = transform.InverseTransformPoint(feet);
        float total = 0f, best = float.MaxValue, at = 0f;
        for (var i = 0; i + 1 < ProgressPath.Length; i++)
        {
            var a = ProgressPath[i];
            var b = ProgressPath[i + 1];
            var ab = b - a;
            var len = ab.magnitude;
            var t = len > 1e-5f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / (len * len)) : 0f;
            var q = a + ab * t;
            var d = p - q;
            d.y *= 2f;
            var dist = d.sqrMagnitude;
            if (dist < best)
            {
                best = dist;
                at = total + len * t;
            }
            total += len;
        }
        return total > 0f ? Mathf.Clamp01(at / total) : 0f;
    }

    /// <summary>Feet on the far landing: the shaft floor between the door wall and flight 2's foot (z 0.17 .. 1.74), within 12 cm of its plane.</summary>
    public bool OnFarLanding(Vector3 feet)
    {
        var p = transform.InverseTransformPoint(feet);
        return Mathf.Abs(p.x) <= InnerX && p.z >= DoorWallZ - .05f && p.z <= ExitEdgeZ + .05f && p.y <= FarY + .12f && p.y >= FarY - .3f;
    }

    /// <summary>The shell's light at a point (EXAMINE's CellLightAt in X1 / X2): the lamp level (0 .. 1) inside the shell, -1 outside it
    /// (as FrontRoomsStairwellDresser.StairwellLampAt for C22's vestibule).</summary>
    public float LightAt(Vector3 worldPos)
    {
        var p = transform.InverseTransformPoint(worldPos);
        if (Mathf.Abs(p.x) > Half || p.z < CorridorFaceZ || p.z > 2f * ModuleUnits.Cell || p.y < FarY - .3f || p.y > SlabY + .1f) return -1f;
        return visible ? lampLevel : 0f;
    }

    /// <summary>For EXAMINE without a shell reference (FrontRoomsInspectLook.CellLightAt): the light of whichever live shell holds the
    /// point (0 .. 1), -1 when none does (then the map's cell light applies, as today).</summary>
    public static float LightAtAny(Vector3 worldPos)
    {
        var best = -1f;
        foreach (var s in live)
            if (s != null) best = Mathf.Max(best, s.LightAt(worldPos));
        return best;
    }

    /// <summary>The seal blackout and any power state (0 dark .. 1 normal): the only thing the shell's lights follow (the safe zone).</summary>
    public void SetLampLevel(float level)
    {
        lampLevel = Mathf.Clamp01(level);
        ApplyLights(false);
    }

    public float LampLevel => lampLevel;

    /// <summary>
    /// The numerals (60 s6.5): the map passes the run's floor and FloorLabel(floor). The first call (and any later call for another exit
    /// floor) names the floor whose EXIT this shell serves: its plates are baked for stair k = floor - 1 (LEVEL token(k) at the exit landing,
    /// token(k + 1) at the far landing), one per frame from the next LateUpdate. A call with floor = that floor + 1 (the change, after the
    /// move) only records the far label: the shell is still the stair between the two. Either label goes to StencilPainter when set.
    /// </summary>
    public void SetFloor(int floor, string label)
    {
        floor = Mathf.Max(1, floor);
        if (exitFloor > 0 && floor == exitFloor + 1)
        {
            farLabel = label ?? "";
            Paint();
            return;
        }
        exitFloor = floor;
        exitLabel = label ?? "";
        farLabel = "";
        foreach (var go in plateParts) if (go != null) Kill(go);
        plateParts.Clear();
        foreach (var set in held) FrontRoomsStairSigns.Release(set);
        held.Clear();
        pendingPlates.Clear();
        pendingPlates.Add(FrontRoomsStairSigns.Plate.IdSign);
        pendingPlates.Add(FrontRoomsStairSigns.Plate.IdSignFar);
        pendingPlates.Add(FrontRoomsStairSigns.Plate.ExitDoor);
        pendingPlates.Add(FrontRoomsStairSigns.Plate.FarDoor);
        pendingPlates.Add(FrontRoomsStairSigns.Plate.NotAnExit);
        Paint();
    }

    public int ExitFloor => exitFloor;
    public int StairK => Mathf.Max(0, exitFloor - 1);

    void Paint()
    {
        if (StencilPainter == null) return;
        if (stencilExit != null) StencilPainter(this, stencilExit, exitLabel);
        if (stencilFar != null) StencilPainter(this, stencilFar, string.IsNullOrEmpty(farLabel) ? FrontRoomsStairSigns.Token(StairK + 1) : farLabel);
    }

    /// <summary>The static shut EXIT leaf in the exit doorway, its stair face dressed (knuckles, closer, NO REENTRY), and its collider:
    /// shown from NextFloor on (the map's EXIT leaf goes away with the old floor), as C22's Seal.</summary>
    public void ShowExitLeafShut(bool shut)
    {
        exitLeafShown = shut;
        foreach (var r in exitLeafRenderers) if (r != null) r.enabled = shut;
        if (exitLeafCollider != null) exitLeafCollider.enabled = shut;
        if (shut && exitCloser != null) SolveCloser(exitCloser, true);
    }

    public bool ExitLeafShown => exitLeafShown;

    /// <summary>N-prime's paper on the far-door wall's corridor face (a Surface slot the map fills after the change), its grime band under
    /// the door cell's ceiling (farCeiling over the far landing, Standard 2.9 m unless the map says otherwise).</summary>
    public void SetFarWallPaper(Material paperMaterial)
    {
        if (paperMaterial != null)
            foreach (var r in paper) if (r != null) r.sharedMaterial = paperMaterial;
        BakeCeilings();
    }

    /// <summary>Tools and the map: the door cell's ceiling height over the far landing (MapGrid.CeilingHeight of N-prime's zone). The
    /// far-door wall's thickening follows it: its top goes to that ceiling and its slab (FarWallTopFor), rebuilt in place when it moves
    /// (关卡设计 11-10 ask (a): a SeedForStair fallback may give a non-Standard N-prime), and the paper's grime band bakes under it.</summary>
    public float FarCeiling
    {
        get => farCeiling;
        set
        {
            farCeiling = Mathf.Max(2f, value);
            if (owner != null && colliderRoot != null) BuildFarWall();
            BakeCeilings();
        }
    }

    /// <summary>After the game's +3.50 m move of Root (the map root never moves, 60 s3 (b)): re-bake _CeilingHeight from the shell's own world y.</summary>
    public void Rebased(Vector3 delta)
    {
        BakeCeilings();
    }

    static readonly int CeilingHeightId = Shader.PropertyToID("_CeilingHeight");
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    void BakeCeilings()
    {
        if (block == null) block = new MaterialPropertyBlock();
        var farFloorY = transform.TransformPoint(new Vector3(0f, FarY, 0f)).y;
        foreach (var r in paper)
        {
            if (r == null) continue;
            var m = r.sharedMaterial;
            if (m == null || !m.HasProperty(CeilingHeightId)) continue;
            r.GetPropertyBlock(block);
            block.SetFloat(CeilingHeightId, farFloorY + farCeiling);
            r.SetPropertyBlock(block);
        }
    }

    // ---------------------------------------------------------------- the far door (60 s6.6; 交互设计 11-10: auto-open, yields at the body)
    public bool FarDoorOpen => phase == DoorPhase.Lever || phase == DoorPhase.Swing || phase == DoorPhase.Open;
    public bool FarDoorIsLatched => phase == DoorPhase.Latched;
    public float FarDoorAngle => farAngle;

    /// <summary>Open the far door: the lever (Open.SwingStart; already down after a hold), then the leaf leaves the frame (FarDoorMoved)
    /// and eases out over Open.SwingSeconds onto 95 deg, into the stair over the far landing. It waits while a body stands in its sweep.</summary>
    public void OpenFarDoor()
    {
        if (phase == DoorPhase.Latched || phase == DoorPhase.Open || phase == DoorPhase.Swing || phase == DoorPhase.Lever) return;
        var fromHold = phase == DoorPhase.Hold;
        phase = fromHold ? DoorPhase.Swing : DoorPhase.Lever;
        clock = 0f;
        if (fromHold) FarDoorMoved?.Invoke();
    }

    /// <summary>The floor beyond is not ready: the lever goes down and the leaf gives 1.5 mm (stack pressure), silent, never locked (60 s4).</summary>
    public void HoldFarDoor()
    {
        if (phase != DoorPhase.Shut) return;
        phase = DoorPhase.Hold;
        clock = 0f;
    }

    /// <summary>Shut the far door for good: back into the frame on a smoothstep (FarDoorMoved), latched (FarDoorLatched). Nothing opens it again.</summary>
    public void CloseFarDoor()
    {
        if (phase == DoorPhase.Shutting || phase == DoorPhase.Latched) return;
        if (farAngle <= .01f)
        {
            farAngle = 0f;
            holdGive = 0f;
            PoseFar();
            Latch();
            return;
        }
        shutFrom = farAngle;
        shutSeconds = Mathf.Max(.12f, FrontRoomsShotTimings.Open.SwingSeconds * farAngle / FrontRoomsShotTimings.Open.SwingDeg);
        clock = 0f;
        phase = DoorPhase.Shutting;
        FarDoorMoved?.Invoke();
    }

    void Latch()
    {
        phase = DoorPhase.Latched;
        leverAngle = 0f;
        PoseLever();
        FarDoorLatched?.Invoke();
    }

    /// <summary>Tools in edit mode (Update does not run): one step of the far door, the lights and the plates.</summary>
    public void TickForTools(float dt)
    {
        TickDoor(dt);
        Tick();
    }

    void LateUpdate()
    {
        TickDoor(Time.deltaTime);
        Tick();
    }

    void TickDoor(float dt)
    {
        if (farHinge == null) return;
        const float holdDeg = .0877f;     // 1.5 mm at the latch edge of a 0.98 m leaf
        switch (phase)
        {
            case DoorPhase.Hold:
                clock += dt;
                leverAngle = Mathf.MoveTowards(leverAngle, FrontRoomsShotTimings.Open.LeverDownDeg, FrontRoomsShotTimings.Open.LeverDownDeg / FrontRoomsShotTimings.Open.LeverDown * dt);
                holdGive = Mathf.SmoothStep(0f, holdDeg, Mathf.Clamp01(clock / .15f));
                PoseLever();
                TryPose(holdGive);
                break;
            case DoorPhase.Lever:
                clock += dt;
                leverAngle = Mathf.MoveTowards(leverAngle, FrontRoomsShotTimings.Open.LeverDownDeg, FrontRoomsShotTimings.Open.LeverDownDeg / FrontRoomsShotTimings.Open.LeverDown * dt);
                PoseLever();
                if (clock >= FrontRoomsShotTimings.Open.SwingStart)
                {
                    clock = 0f;
                    phase = DoorPhase.Swing;
                    FarDoorMoved?.Invoke();
                }
                break;
            case DoorPhase.Swing:
            {
                var next = clock + dt;
                var t = Mathf.Clamp01(next / FrontRoomsShotTimings.Open.SwingSeconds);
                var a = Mathf.Max(holdGive, FrontRoomsShotTimings.Open.SwingDeg * (1f - (1f - t) * (1f - t)));
                if (TryPose(a))
                {
                    clock = next;
                    if (t >= 1f) phase = DoorPhase.Open;
                }
                if (clock > FrontRoomsShotTimings.Open.LeverBackStart)
                {
                    leverAngle = Mathf.MoveTowards(leverAngle, 0f, FrontRoomsShotTimings.Open.LeverDownDeg / (FrontRoomsShotTimings.Open.LeverBackEnd - FrontRoomsShotTimings.Open.LeverBackStart) * dt);
                    PoseLever();
                }
                break;
            }
            case DoorPhase.Shutting:
            {
                var next = clock + dt;
                var t = Mathf.Clamp01(next / shutSeconds);
                var a = shutFrom * (1f - t * t * (3f - 2f * t));
                if (TryPose(a))
                {
                    clock = next;
                    if (t >= 1f)
                    {
                        farAngle = 0f;
                        holdGive = 0f;
                        PoseFar();
                        Latch();
                    }
                }
                break;
            }
        }
    }

    /// <summary>Pose the leaf at <paramref name="angle"/> unless a body (a CharacterController) would be inside it there: then it waits
    /// (交互设计 11-10 rule 1: stop, wait, carry on once clear; never push the capsule or clip the camera).</summary>
    bool TryPose(float angle)
    {
        if (Mathf.Abs(angle - farAngle) < 1e-5f) return true;
        if (angle > farAngle + .2f && BodyInLeaf(angle)) return false;
        if (angle < farAngle - .2f && BodyInLeaf(angle)) return false;
        farAngle = angle;
        PoseFar();
        return true;
    }

    bool BodyInLeaf(float angle)
    {
        var rot = farHinge.parent.rotation * farClosed * Quaternion.Euler(0f, angle, 0f);
        var leafW = FarOpeningWidth - ModuleUnits.DoorLeafGap;
        var centre = farHinge.position + rot * new Vector3(0f, (OpeningHeight - ModuleUnits.DoorLeafGap) * .5f, leafW * .5f);
        var half = new Vector3(ModuleUnits.DoorLeafThickness * .5f + .02f, (OpeningHeight - ModuleUnits.DoorLeafGap) * .5f, leafW * .5f);
        var n = Physics.OverlapBoxNonAlloc(centre, half, overlap, rot, ~0, QueryTriggerInteraction.Ignore);
        for (var i = 0; i < n; i++)
            if (overlap[i] is CharacterController) return true;
        return false;
    }

    void PoseFar()
    {
        if (farHinge == null) return;
        // the hinge's +Z runs along the shut leaf toward -x, its +X is the stair: +angle swings the leaf into the stair
        farHinge.localRotation = farClosed * Quaternion.Euler(0f, farAngle, 0f);
        Physics.SyncTransforms();
        SolveCloser(farCloser, false);
    }

    void PoseLever()
    {
        if (farLever != null) farLever.localRotation = Quaternion.Euler(0f, 0f, leverDownSign * leverAngle);
    }

    // ---------------------------------------------------------------- lights, probes, plates
    readonly List<FrontRoomsStairSigns.Plate> pendingPlates = new List<FrontRoomsStairSigns.Plate>();

    void Tick()
    {
        // the plates: one bake a frame, off the change's frame
        if (pendingPlates.Count > 0)
        {
            var p = pendingPlates[0];
            pendingPlates.RemoveAt(0);
            BakePlate(p);
        }
        var cam = Camera.main;
        var awake = true;
        if (cam != null)
        {
            var c = transform.InverseTransformPoint(cam.transform.position);
            var dx = Mathf.Max(0f, Mathf.Abs(c.x) - Half);
            var dz = Mathf.Max(0f, Mathf.Max(CorridorFaceZ - c.z, c.z - 2f * ModuleUnits.Cell));
            var dy = Mathf.Max(0f, Mathf.Max(FarY - c.y, c.y - SlabY));
            awake = dx * dx + dy * dy + dz * dz <= LightWakeDistance * LightWakeDistance;
        }
        lightsAwake = awake;
        ApplyLights(false);
        // the probes: rendered once, lit and shown (as stairwell_a's), never while the change's blackout holds
        if (!probesRendered && visible && lightsAwake && lampLevel >= .999f && !reduced)
        {
            foreach (var pr in probes) if (pr != null && pr.mode == ReflectionProbeMode.Realtime) pr.RenderProbe();
            probesRendered = true;
        }
    }

    /// <summary>IFrontRoomsStairShellVisible (the game calls it as C22's SetVisible): renderers and lights on or off; colliders never.</summary>
    public void SetVisible(bool on)
    {
        visible = on;
        ApplyLights(false);
    }

    public bool Visible => visible;

    void ApplyLights(bool force)
    {
        if (!force && Mathf.Abs(appliedLevel - lampLevel) < 1e-4f && appliedAwake == lightsAwake && appliedVisible == visible) return;
        if (force || appliedVisible != visible)
            foreach (var r in allRenderers) if (r != null) r.forceRenderingOff = !visible;
        appliedLevel = lampLevel;
        appliedAwake = lightsAwake;
        appliedVisible = visible;
        if (block == null) block = new MaterialPropertyBlock();
        for (var i = 0; i < spots.Count; i++)
        {
            var l = spots[i];
            if (l == null) continue;
            l.intensity = spotBase[i] * lampLevel;
            l.enabled = visible && lightsAwake && lampLevel > .001f;
        }
        // every LOD's lens follows the level; the twin-heads' red pilot shows mains power: dark through the blackout (stairwell_a's rule)
        for (var i = 0; i < lenses.Count; i++) SetEmission(lenses[i], lensSlots[i], LensEmission * lampLevel);
        for (var i = 0; i < pilots.Count; i++) SetEmission(pilots[i], pilotSlots[i], PilotEmission * Mathf.Clamp01(lampLevel * 2f - 1f));
        foreach (var pr in probes) if (pr != null) pr.intensity = lampLevel;
    }

    void SetEmission(Renderer r, int slot, Color c)
    {
        if (r == null) return;
        r.GetPropertyBlock(block, slot);
        block.SetColor(EmissionId, c);
        r.SetPropertyBlock(block, slot);
    }

    void BakePlate(FrontRoomsStairSigns.Plate kind)
    {
        var set = FrontRoomsStairSigns.Acquire(kind, StairK);
        if (set == null) return;
        held.Add(set);
        const float idX = .88f, idY = 1.524f;
        var face = ModuleUnits.DoorLeafThickness * .5f;
        switch (kind)
        {
            case FrontRoomsStairSigns.Plate.IdSign:
                plateParts.Add(PlatePart(dress, set, new Vector3(idX, idY, DoorWallZ), Vector3.forward).gameObject);
                break;
            case FrontRoomsStairSigns.Plate.IdSignFar:
                plateParts.Add(PlatePart(dress, set, new Vector3(-idX, FarY + idY, DoorWallZ), Vector3.forward).gameObject);
                break;
            case FrontRoomsStairSigns.Plate.ExitDoor:
                if (exitHinge != null)
                {
                    var r = PlatePart(exitHinge, set, new Vector3(-face - .001f, idY, ExitOpeningWidth * .5f), Vector3.left);
                    r.enabled = exitLeafShown;
                    exitLeafRenderers.Add(r);
                    plateParts.Add(r.gameObject);
                }
                break;
            case FrontRoomsStairSigns.Plate.FarDoor:
                // the far leaf straddles its hinge axis on the wall's centre line (as C22's north leaf): +X is its stair face
                if (farHinge != null) plateParts.Add(PlatePart(farHinge, set, new Vector3(face + .001f, idY, FarLeafCentreZ), Vector3.right).gameObject);
                break;
            case FrontRoomsStairSigns.Plate.NotAnExit:
                if (farHinge != null) plateParts.Add(PlatePart(farHinge, set, new Vector3(-face - .001f, idY, FarLeafCentreZ), Vector3.left).gameObject);
                break;
        }
    }

    MeshRenderer PlatePart(Transform parent, FrontRoomsStairSigns.PlateSet set, Vector3 at, Vector3 normal)
    {
        var r = Part(parent, "plate " + set.plate + (set.text != null ? " (" + set.text + ")" : ""), FrontRoomsStairKit.PlateSlab(set.size), set.material,
            at, Quaternion.LookRotation(-normal, Vector3.up), ShadowCastingMode.Off);
        r.forceRenderingOff = !visible;
        allRenderers.Add(r);
        return r;
    }

    void OnDestroy()
    {
        live.Remove(this);
        foreach (var set in held) FrontRoomsStairSigns.Release(set);
        held.Clear();
    }

    // ---------------------------------------------------------------- building
    /// <summary>The far leaf's middle along its width (hinge z). Both door axes stand on the door wall's centre line (DoorAxisZ), as C22's.</summary>
    const float FarLeafCentreZ = (FarOpeningWidth - ModuleUnits.DoorLeafGap) * .5f;
    public const float DoorAxisZ = (Inset + DoorWallZ) * .5f;

    void Build()
    {
        live.Add(this);
        block = new MaterialPropertyBlock();
        reduced = FrontRoomsStairwellDresser.ReducedTier;
        kits = KitsAvailable;
        doorKits = FrontRoomsKitLibrary.Has(ExitLeafKit) && FrontRoomsKitLibrary.Has(FarLeafKit) && FrontRoomsKitLibrary.Has(ExitFrameKit) && FrontRoomsKitLibrary.Has(FarFrameKit);
        owner = gameObject.AddComponent<FrontRoomsStairDressOwner>();
        dress = transform;
        var t = transform;
        colliderRoot = NewChild(t, "colliders (walls, landings, guards)");
        BuildShell(t);
        BuildBand(t);
        if (kits) BuildKits(t); else BuildStandIns(t);
        BuildRiser(t);
        BuildExitDoor(t);
        BuildFarDoor(t);
        BuildSigns(t);
        BuildLights(t);
        BuildProbes(t);
        BuildColliders(t);
        FlushBatches(t);
        GetComponentsInChildren(true, allRenderers);
        latchAnchor = NewChild(t, "latch sound anchor (EXIT latch, stair side)");
        latchAnchor.localPosition = new Vector3(ExitOpeningWidth * .5f - .06f, ModuleUnits.DoorHandleHeight, DoorWallZ);
        // the stencil numerals' places (10_directions 1.4: block numerals on the block wall, hinge side of each stair door, 0.46 m)
        stencilExit = NewChild(t, "stencil numeral anchor (exit landing, hinge side)");
        stencilExit.localPosition = new Vector3(-(ExitOpeningWidth * .5f + ModuleUnits.TrimFace + .33f), 1.42f, DoorWallZ + .001f);
        stencilFar = NewChild(t, "stencil numeral anchor (far landing, hinge side)");
        stencilFar.localPosition = new Vector3(FarOpeningWidth * .5f + ModuleUnits.TrimFace + .33f, FarY + 1.42f, DoorWallZ + .001f);
        farClosed = farHinge != null ? farHinge.localRotation : Quaternion.identity;
        ShowExitLeafShut(false);
        BakeCeilings();
        ApplyLights(true);
    }

    /// <summary>The CMU shell: two side walls and the far wall the whole height, the door wall round its two openings, the far-door wall's
    /// corridor thickening (N-prime's paper), the slab at +3.30 and the shaft floor (under the walls, so both doorways have floor to their lines).</summary>
    void BuildShell(Transform t)
    {
        var block_ = BlockWalk;
        var yLo = FarY - SlabThickness;
        var h = SlabY - yLo;
        var yc = (SlabY + yLo) * .5f;
        var len = 2f * ModuleUnits.Cell - 2f * Inset;
        Box(t, "Wall +x (latch side)", block_, new Vector3(InnerX + ShellThickness * .5f, yc, ModuleUnits.Cell), new Vector3(ShellThickness, h, len), true);
        Box(t, "Wall -x (hinge side)", block_, new Vector3(-InnerX - ShellThickness * .5f, yc, ModuleUnits.Cell), new Vector3(ShellThickness, h, len), true);
        Box(t, "Wall far", block_, new Vector3(0f, yc, FarWallZ + ShellThickness * .5f), new Vector3(2f * InnerX, h, ShellThickness), true);
        // the door wall (z Inset .. DoorWallZ) in bands round the exit opening (y 0 .. 2.17) and the far opening (far landing .. +2.17)
        var zc = (Inset + DoorWallZ) * .5f;
        var head = OpeningHeight + ModuleUnits.TrimFace;
        var ex = ExitOpeningWidth * .5f + ModuleUnits.TrimFace;
        var fx = FarOpeningWidth * .5f + ModuleUnits.TrimFace;
        DoorWallBand(t, "far opening", FarY, FarY + head, fx, zc);
        DoorWallBand(t, "between", FarY + head, 0f, -1f, zc);
        DoorWallBand(t, "exit opening", 0f, head, ex, zc);
        DoorWallBand(t, "above", head, SlabY, -1f, zc);
        // the far-door wall's corridor thickening (BuildFarWall: its top follows FarCeiling)
        BuildFarWall();
        // the slab over the exit landing (60 s3: 3.30 m) and the shaft floor, the far landing
        Box(t, "Slab", FrontRoomsStairKit.Slab, new Vector3(0f, SlabY + SlabThickness * .5f, ModuleUnits.Cell), new Vector3(2f * InnerX, SlabThickness, len), true);
        Box(t, "Shaft floor (far landing)", FrontRoomsStairKit.Fill, new Vector3(0f, FarY - SlabThickness * .5f, (CorridorFaceZ + 2f * ModuleUnits.Cell) * .5f),
            new Vector3(2f * Half, SlabThickness, 2f * ModuleUnits.Cell - CorridorFaceZ), true);
        // the shaft floor's 4 in black vinyl cove along every wall foot (look A)
        var cove = FrontRoomsStairKit.CutInLine;
        Box(t, "Cove far wall", cove, new Vector3(0f, FarY + .05f, FarWallZ - .002f), new Vector3(2f * InnerX, .1f, .004f), false);
        Box(t, "Cove +x", cove, new Vector3(InnerX - .002f, FarY + .05f, (DoorWallZ + FarWallZ) * .5f), new Vector3(.004f, .1f, FarWallZ - DoorWallZ), false);
        Box(t, "Cove -x", cove, new Vector3(-InnerX + .002f, FarY + .05f, (DoorWallZ + FarWallZ) * .5f), new Vector3(.004f, .1f, FarWallZ - DoorWallZ), false);
        foreach (var sgn in new[] { -1f, 1f })
            Box(t, "Cove door wall", cove, new Vector3(sgn * (fx + (InnerX - fx) * .5f), FarY + .05f, DoorWallZ + .002f), new Vector3(InnerX - fx, .1f, .004f), false);
        // the sprinkler head at the top of the shaft (NFPA 13-1989: heads at the top of a noncombustible shaft; 01_research 1.2)
        var mb = new FrontRoomsStairKit.MB();
        mb.Cylinder(new Vector3(.9f, SlabY, 2.2f), new Vector3(.9f, SlabY - .12f, 2.2f), .013f, 10);
        mb.Cylinder(new Vector3(.9f, SlabY - .125f, 2.2f), new Vector3(.9f, SlabY - .131f, 2.2f), .028f, 14);
        MeshPart(t, "sprinkler head (top of the shaft)", mb, FrontRoomsStairKit.Get("Stair_ValveBrass"), ShadowCastingMode.Off);
    }

    void DoorWallBand(Transform t, string name, float y0, float y1, float halfHole, float zc)
    {
        if (y1 - y0 < .001f) return;
        var mat = BlockWalk;
        var d = DoorWallZ - Inset;
        if (halfHole < 0f)
        {
            Box(t, "Door wall " + name, mat, new Vector3(0f, (y0 + y1) * .5f, zc), new Vector3(2f * InnerX, y1 - y0, d), true);
            return;
        }
        var w = InnerX - halfHole;
        Box(t, "Door wall " + name + " -x", mat, new Vector3(-(halfHole + w * .5f), (y0 + y1) * .5f, zc), new Vector3(w, y1 - y0, d), true);
        Box(t, "Door wall " + name + " +x", mat, new Vector3(halfHole + w * .5f, (y0 + y1) * .5f, zc), new Vector3(w, y1 - y0, d), true);
    }

    /// <summary>
    /// The far-door wall's corridor thickening, from the shell's outer face to half a wall past the line, between X1's side lines: the far
    /// opening's two jambs and its head, from the far landing up to N-prime's ceiling and its slab (FarWallTopFor(FarCeiling); 3.06 m for a
    /// Standard 2.9 m N-prime). On the old floor it stands under the floor (nothing of the map is there); after the change it closes X1's
    /// south line, which the new map leaves open (its start area's door side), as C22's JoinNextFloor end wall does. Built with the shell and
    /// again whenever FarCeiling moves the top (the game sets it at the change from N-prime's zone): the same renderer (N-prime's paper, its
    /// property block and visibility kept) takes a new mesh, and the same three box colliders are resized. Solid: they are walls.
    /// </summary>
    void BuildFarWall()
    {
        var top = FarY + FarWallTopFor(farCeiling);
        if (farWallFilter != null && Mathf.Abs(top - farWallBuiltTop) < 1e-3f) return;
        farWallBuiltTop = top;
        var head = OpeningHeight + ModuleUnits.TrimFace;
        var fx = FarOpeningWidth * .5f + ModuleUnits.TrimFace;
        var span = Half - ModuleUnits.WallHalf;
        var depth = Inset - CorridorFaceZ;
        var pz = (Inset + CorridorFaceZ) * .5f;
        var side = span - fx;
        var centres = new[]
        {
            new Vector3(-(fx + side * .5f), (FarY + top) * .5f, pz),
            new Vector3(fx + side * .5f, (FarY + top) * .5f, pz),
            new Vector3(0f, (FarY + head + top) * .5f, pz),
        };
        var sizes = new[]
        {
            new Vector3(side, top - FarY, depth),
            new Vector3(side, top - FarY, depth),
            new Vector3(2f * fx, top - FarY - head, depth),
        };
        var mb = new FrontRoomsStairKit.MB();
        for (var i = 0; i < centres.Length; i++)
        {
            PlanarBox(mb, centres[i], sizes[i]);
            if (farWallColliders[i] == null)
                farWallColliders[i] = NewChild(colliderRoot, "far wall corridor face").gameObject.AddComponent<BoxCollider>();
            farWallColliders[i].center = centres[i];
            farWallColliders[i].size = sizes[i];
        }
        var mesh = mb.ToMesh("Stair walk / far wall corridor face");
        owner.meshes.Add(mesh);
        if (farWallFilter == null)
        {
            var r = Part(transform, "far wall corridor face (N-prime's paper)", mesh, BlockWalk, Vector3.zero, Quaternion.identity, ShadowCastingMode.On);
            r.forceRenderingOff = !visible;
            farWallFilter = r.GetComponent<MeshFilter>();
            paper.Add(r);
            return;
        }
        var old = farWallFilter.sharedMesh;
        farWallFilter.sharedMesh = mesh;
        if (old != null)
        {
            owner.meshes.Remove(old);
            Kill(old);
        }
    }

    /// <summary>
    /// Look A's band (BSS 120 Fig. 2.7.5; 10_directions 2.1): tan semi-gloss to 1.1 m over the walking line, following both flights and
    /// wrapping every landing, 1.5 mm proud of the block, its 12 mm dark cut-in line on top; stops at the doors' 70 mm frame faces.
    /// </summary>
    void BuildBand(Transform t)
    {
        var band = new FrontRoomsStairKit.MB();
        var cut = new FrontRoomsStairKit.MB();
        const float top = FrontRoomsStairKit.BandTop, under = .35f, o = .0015f;
        var xp = InnerX - o;
        // +x wall: exit landing, flight 1 down to the turn, turn landing; the far landing below
        SideBand(band, cut, xp, -1f, DoorWallZ, ExitEdgeZ, 0f, 0f, top, under);
        SideBand(band, cut, xp, -1f, ExitEdgeZ, TurnEdgeZ, 0f, TurnY, top, under);
        SideBand(band, cut, xp, -1f, TurnEdgeZ, FarWallZ, TurnY, TurnY, top, under);
        SideBand(band, cut, xp, -1f, DoorWallZ, BarrierZ, FarY, FarY, top, .06f);
        // -x wall: exit landing, turn landing, flight 2 down to the far landing, far landing
        SideBand(band, cut, -xp, 1f, DoorWallZ, ExitEdgeZ, 0f, 0f, top, under);
        SideBand(band, cut, -xp, 1f, TurnEdgeZ, FarWallZ, TurnY, TurnY, top, under);
        SideBand(band, cut, -xp, 1f, ExitEdgeZ, TurnEdgeZ, FarY, TurnY, top, under);
        SideBand(band, cut, -xp, 1f, DoorWallZ, ExitEdgeZ, FarY, FarY, top, .06f);
        // end walls: the door wall each side of both doors, the far wall at the turn landing
        var ex = ExitOpeningWidth * .5f + ModuleUnits.TrimFace;
        var fx = FarOpeningWidth * .5f + ModuleUnits.TrimFace;
        var zd = DoorWallZ + o;
        EndBand(band, cut, zd, 1f, -InnerX, -ex, 0f, top);
        EndBand(band, cut, zd, 1f, ex, InnerX, 0f, top);
        EndBand(band, cut, zd, 1f, -InnerX, -fx, FarY, top);
        EndBand(band, cut, zd, 1f, fx, InnerX, FarY, top);
        EndBand(band, cut, FarWallZ - o, -1f, InnerX, -InnerX, TurnY, top);
        var bandMesh = band.ToMesh("Stair walk / band");
        var cutMesh = cut.ToMesh("Stair walk / cut-in");
        owner.meshes.Add(bandMesh);
        owner.meshes.Add(cutMesh);
        Part(t, "block band (tan semi-gloss)", bandMesh, BandWalk, Vector3.zero, Quaternion.identity, ShadowCastingMode.Off);
        Part(t, "band cut-in line", cutMesh, FrontRoomsStairKit.CutInLine, Vector3.zero, Quaternion.identity, ShadowCastingMode.Off);
    }

    /// <summary>A band strip on a side wall (x fixed, facing nx) from za to zb over walking lines ya .. yb, and its cut-in line 1 mm further out.</summary>
    static void SideBand(FrontRoomsStairKit.MB band, FrontRoomsStairKit.MB cut, float x, float nx, float za, float zb, float ya, float yb, float top, float under)
    {
        var n = new Vector3(nx, 0f, 0f);
        Vector3 a = new Vector3(x, ya - under, za), b = new Vector3(x, yb - under, zb), c = new Vector3(x, yb + top, zb), d = new Vector3(x, ya + top, za);
        Quad(band, n, a, b, c, d);
        var xl = x + nx * .001f;
        Quad(cut, n, new Vector3(xl, ya + top - .008f, za), new Vector3(xl, yb + top - .008f, zb), new Vector3(xl, yb + top + .004f, zb), new Vector3(xl, ya + top + .004f, za));
    }

    static void EndBand(FrontRoomsStairKit.MB band, FrontRoomsStairKit.MB cut, float z, float nz, float xa, float xb, float y, float top)
    {
        var n = new Vector3(0f, 0f, nz);
        Quad(band, n, new Vector3(xa, y - .06f, z), new Vector3(xb, y - .06f, z), new Vector3(xb, y + top, z), new Vector3(xa, y + top, z));
        var zl = z + nz * .001f;
        Quad(cut, n, new Vector3(xa, y + top - .008f, zl), new Vector3(xb, y + top - .008f, zl), new Vector3(xb, y + top + .004f, zl), new Vector3(xa, y + top + .004f, zl));
    }

    /// <summary>A quad facing <paramref name="normal"/> (MB.Tri winds each triangle to it), a -> b along the wall (the tangent, +U), metre UVs.</summary>
    static void Quad(FrontRoomsStairKit.MB mb, Vector3 normal, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        Vector2 Uv(Vector3 p) => new Vector2(Mathf.Abs(normal.x) > .5f ? p.z : p.x, p.y);
        mb.Quad(a, b, c, d, normal, Uv(a), Uv(b), Uv(c), Uv(d));
    }

    /// <summary>The kits in the shell's layout (each kit's frame: research/stairwell/80 section 6; the modules' docstrings).</summary>
    void BuildKits(Transform t)
    {
        var flip = Quaternion.Euler(0f, 180f, 0f);
        var f1 = new Vector3(FlightX, 0f, ExitEdgeZ);
        var f2 = new Vector3(-FlightX, TurnY, TurnEdgeZ);
        Kit(t, FlightKit, f1, Quaternion.identity, "flight 1 (exit to turn)");
        Kit(t, FlightKit, f2, flip, "flight 2 (turn to far)");
        Kit(t, LandingKit, new Vector3(0f, 0f, ExitEdgeZ), Quaternion.identity, "exit landing");
        Kit(t, LandingKit, new Vector3(0f, TurnY, TurnEdgeZ), flip, "turn landing");
        Kit(t, GuardKit, f1, Quaternion.identity, "flight 1 guard (well side)");
        Kit(t, GuardKit, f2, flip, "flight 2 guard (well side)");
        Kit(t, RailKit, f1, Quaternion.identity, "flight 1 wall rail");
        Kit(t, RailKit, f2, flip, "flight 2 wall rail");
        // the exit landing's open edge (a 3.5 m drop on the flight-2 side, 10_directions 1.1) and the guard across the well at the turn
        Kit(t, LandingGuardKit, new Vector3(-(InnerX - (FlightX - FlightHalf)) * .5f, 0f, ExitEdgeZ - .035f), Quaternion.identity, "exit landing guard (42 in)");
        Kit(t, WellGuardKit, new Vector3(0f, TurnY, TurnEdgeZ + .035f), Quaternion.identity, "turn landing well guard (42 in)");
        // the hose stations at both floor-level landings (UBC 1991 3805(c)), outlets into the landing (-x)
        var face = Quaternion.Euler(0f, -90f, 0f);
        Kit(t, ValveKit, new Vector3(RiserXZ.x, 0f, RiserXZ.y), face, "hose station (exit landing)");
        Kit(t, ValveKit, new Vector3(RiserXZ.x, FarY, RiserXZ.y), face, "hose station (far landing)");
        Kit(t, BarrierKit, new Vector3(0f, FarY, BarrierZ), Quaternion.identity, "headroom barrier (ADAAG 4.4.2)");
    }

    GameObject Kit(Transform t, string kit, Vector3 at, Quaternion rot, string label)
    {
        var go = FrontRoomsKitLibrary.Spawn(kit, t, at, rot, null, false, label, false);
        if (go == null) Debug.LogWarning("[StairShell] kit " + kit + " is missing");
        return go;
    }

    /// <summary>No kits: plain stand-ins so the stair is never a hole (a stepped flight, landing slabs, a pipe rail on each guard line).</summary>
    void BuildStandIns(Transform t)
    {
        var steel = FrontRoomsStairKit.Get(SteelName);
        var fill = FrontRoomsStairKit.Fill;
        foreach (var f in new[] { 1, 2 })
        {
            var x = f == 1 ? FlightX : -FlightX;
            var dz = f == 1 ? 1f : -1f;
            var z0 = f == 1 ? ExitEdgeZ : TurnEdgeZ;
            var y0 = f == 1 ? 0f : TurnY;
            for (var k = 1; k <= Treads; k++)
            {
                var y = y0 - Rise * k;
                var zc = z0 + dz * Going * (k - .5f);
                Box(t, "stand-in tread " + f + "." + k, fill, new Vector3(x, y - .02f, zc), new Vector3(2f * FlightHalf - .06f, .04f, Going), false);
                Box(t, "stand-in riser " + f + "." + k, steel, new Vector3(x, y + Rise * .5f - .02f, z0 + dz * Going * (k - 1)), new Vector3(2f * FlightHalf - .06f, Rise, .006f), false);
            }
        }
        Box(t, "stand-in exit landing", fill, new Vector3(0f, -.025f, (DoorWallZ + ExitEdgeZ) * .5f), new Vector3(2f * InnerX, .05f, ExitEdgeZ - DoorWallZ), false);
        Box(t, "stand-in turn landing", fill, new Vector3(0f, TurnY - .025f, (TurnEdgeZ + FarWallZ) * .5f), new Vector3(2f * InnerX, .05f, FarWallZ - TurnEdgeZ), false);
    }

    // ---------------------------------------------------------------- the standpipe (NFPA 14-1990 Class I; UBC 1991 3805(c))
    /// <summary>The red 4 in riser on the +x wall from the shaft floor to the slab: the valve kits carry its stub at both door landings
    /// (0.60 .. 1.80 m over each), the runs between are built here, sleeved through the exit landing and the slab, strapped to the wall.</summary>
    void BuildRiser(Transform t)
    {
        var mb = new FrontRoomsStairKit.MB();
        var x = RiserXZ.x;
        var z = RiserXZ.y;
        const int sides = 24;
        var valves = kits && FrontRoomsKitLibrary.Has(ValveKit);
        var runs = valves
            ? new[] { new Vector2(FarY, FarY + .6f), new Vector2(FarY + 1.8f, .6f), new Vector2(1.8f, SlabY) }
            : new[] { new Vector2(FarY, SlabY) };
        foreach (var r in runs) mb.Tube(new List<Vector3> { new Vector3(x, r.x, z), new Vector3(x, r.y, z) }, RiserRadius, sides, false);
        // escutcheon sleeves: on the shaft floor, on the exit landing's fill, under its pan, under the slab
        foreach (var s in new[] { new Vector2(FarY, FarY + .008f), new Vector2(0f, .008f), new Vector2(-.205f, -.213f), new Vector2(SlabY, SlabY - .008f) })
            mb.Cylinder(new Vector3(x, s.x, z), new Vector3(x, s.y, z), RiserRadius + .016f, sides);
        // wall straps: a 4 mm pipe band and a 32 x 6 mm flat bar to the +x wall, once a storey between the valves
        foreach (var y in new[] { -1.0f, 2.5f })
        {
            var ring = new List<Vector3>();
            for (var i = 0; i < 24; i++)
            {
                var a = i * Mathf.PI * 2f / 24f;
                ring.Add(new Vector3(x + Mathf.Cos(a) * (RiserRadius + .004f), y, z + Mathf.Sin(a) * (RiserRadius + .004f)));
            }
            mb.Tube(ring, .004f, 6, true);
            var x0 = x + RiserRadius + .004f;
            mb.Box(new Vector3((x0 + InnerX) * .5f, y, z), new Vector3(InnerX - x0, .032f, .006f), Quaternion.identity);
        }
        MeshPart(t, "standpipe riser (4 in, NFPA 14-1990 Class I)", mb, FrontRoomsStairKit.Get(RedName), ShadowCastingMode.On);
    }

    // ---------------------------------------------------------------- the doors
    /// <summary>
    /// The static shut EXIT leaf in the exit doorway (C22's sealed south leaf, in the same place): its hinge frame is C22's south hinge
    /// (+X toward N, the push face), the 1.12 m leaf kit with its stair face in the own lights, knuckles and the closer on the stair (pull)
    /// side, NO REENTRY from SetFloor; hidden until ShowExitLeafShut(true). The doorway's frame stands from the start.
    /// </summary>
    void BuildExitDoor(Transform t)
    {
        exitHinge = NewChild(t, "Door hinge stairwell exit (static shut leaf)");
        exitHinge.localPosition = new Vector3(-ExitOpeningWidth * .5f, 0f, DoorAxisZ);
        exitHinge.localRotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
        var w = ExitOpeningWidth - ModuleUnits.DoorLeafGap;
        var h = OpeningHeight - ModuleUnits.DoorLeafGap;
        var leafDress = NewChild(exitHinge, "STAIR walk dress (static shut EXIT leaf)");
        if (doorKits) KitUnder(leafDress, ExitLeafKit, 1f, "stair leaf (kit)");
        else Part(leafDress, "Door leaf (stand-in)", LeafMesh(w, h), FrontRoomsStairKit.LeafInside, Vector3.zero, Quaternion.identity, ShadowCastingMode.On);
        Knuckles(leafDress, -1f);
        var lining = NewChild(exitHinge, "lining side");
        exitCloser = BuildCloser(leafDress, lining, doorKits ? FrontRoomsStairwellDresser.SealedShoeOutKit : FrontRoomsStairwellDresser.SealedShoeOut, 1f);
        SolveCloser(exitCloser, true);
        lining.SetParent(leafDress, true);
        leafDress.GetComponentsInChildren(true, exitLeafRenderers);
        var c = NewChild(exitHinge, "exit leaf collider").gameObject.AddComponent<BoxCollider>();
        c.center = new Vector3(0f, h * .5f, ExitOpeningWidth * .5f);
        c.size = new Vector3(ModuleUnits.DoorLeafThickness, h, w);
        exitLeafCollider = c;
        if (doorKits)
        {
            var at = NewChild(t, "stair frame exit (door space)");
            at.localPosition = exitHinge.localPosition;
            at.localRotation = exitHinge.localRotation;
            KitUnder(at, ExitFrameKit, 1f, "stair frame (kit) exit");
        }
        else ShellFrame(t, "exit", ExitOpeningWidth, 0f);
    }

    /// <summary>
    /// The far door (60 s6.6): in the far-door wall right under the EXIT, hinged on the +x jamb, the 0.98 m leaf swinging 95 deg into
    /// the stair over the far landing (pulled from the stair: the kit unmirrored, its pull side +X = the stair). Stair face: the lever
    /// (the hold pose moves it), the closer, REENTRY; floor face: lever trim and NOT AN EXIT (the plates from SetFloor). Its hinge is
    /// named "Door hinge stairwell ..." as C22's, so the sound layer hangs its swing and latch on it. The reveal through the corridor
    /// thickening is framed on the corridor side in the map's trim, as C22's north end wall.
    /// </summary>
    void BuildFarDoor(Transform t)
    {
        farHinge = NewChild(t, "Door hinge stairwell far (walkable)");
        farHinge.localPosition = new Vector3(FarOpeningWidth * .5f, FarY, DoorAxisZ);
        farHinge.localRotation = Quaternion.LookRotation(Vector3.left, Vector3.up);
        var frameSide = NewChild(t, "far door frame side");
        frameSide.localPosition = farHinge.localPosition;
        frameSide.localRotation = farHinge.localRotation;
        var w = FarOpeningWidth - ModuleUnits.DoorLeafGap;
        var h = OpeningHeight - ModuleUnits.DoorLeafGap;
        var face = ModuleUnits.DoorLeafThickness * .5f;
        var nd = NewChild(farHinge, "STAIR walk dress (far door)");
        if (doorKits) KitUnder(nd, FarLeafKit, -1f, "stair leaf (kit)");
        else Part(nd, "Door leaf (stand-in)", LeafMesh(w, h), FrontRoomsStairKit.Leaf, Vector3.zero, Quaternion.identity, ShadowCastingMode.On);
        Knuckles(nd, 1f);
        var rose = Spawn("Kit_Lock_Rose", nd, new Vector3(face, .968f, .92f), Quaternion.Euler(0f, 90f, 0f));
        if (rose != null) farLever = Spawn("Kit_Lock_Lever", rose, new Vector3(0f, 0f, .01f), Quaternion.identity);
        var roseOut = Spawn("Kit_Lock_Rose", nd, new Vector3(-face, .968f, .92f), Quaternion.Euler(0f, -90f, 180f));
        if (roseOut != null) Spawn("Kit_Lock_Lever", roseOut, new Vector3(0f, 0f, .01f), Quaternion.identity);
        farCloser = BuildCloser(nd, frameSide, doorKits ? FrontRoomsStairwellDresser.SealedShoeOutKit : FrontRoomsStairwellDresser.SealedShoeOut, -1f);
        SolveCloser(farCloser, true);
        var c = NewChild(farHinge, "far leaf collider").gameObject.AddComponent<BoxCollider>();
        c.center = new Vector3(0f, h * .5f, FarOpeningWidth * .5f);
        c.size = new Vector3(ModuleUnits.DoorLeafThickness, h, w);
        farLeafCollider = c;
        if (doorKits)
        {
            var at = NewChild(t, "stair frame far (door space)");
            at.localPosition = farHinge.localPosition;
            at.localRotation = farHinge.localRotation;
            KitUnder(at, FarFrameKit, -1f, "stair frame (kit) far");
        }
        else ShellFrame(t, "far", FarOpeningWidth, FarY);
        // the corridor side's frame through the thickening (flush on the stair side, 2 cm proud on the corridor side, the map's trim)
        var trim = FrontRoomsSurfaces.CoveBase;
        var half = FarOpeningWidth * .5f;
        var frame = ModuleUnits.TrimFace;
        var z0 = CorridorFaceZ - ModuleUnits.TrimProud;
        var depth = Inset - z0;
        var zc = (Inset + z0) * .5f;
        var headBottom = OpeningHeight + frame;
        Box(t, "far door corridor frame -x", trim, new Vector3(-(half + frame * .5f), FarY + headBottom * .5f, zc), new Vector3(frame, headBottom, depth), false);
        Box(t, "far door corridor frame +x", trim, new Vector3(half + frame * .5f, FarY + headBottom * .5f, zc), new Vector3(frame, headBottom, depth), false);
        Box(t, "far door corridor frame head", trim, new Vector3(0f, FarY + OpeningHeight + frame * .5f, zc), new Vector3(2f * half, frame, depth), false);
    }

    /// <summary>Without the door kits: C22's shell frame boxes (70 mm face, the wall's depth plus 2 cm proud into the stair).</summary>
    void ShellFrame(Transform t, string name, float width, float y)
    {
        var half = width * .5f;
        var frame = ModuleUnits.TrimFace;
        var depth = ShellThickness + ModuleUnits.TrimProud;
        var zc = Inset + depth * .5f;
        var headBottom = OpeningHeight + frame;
        var m = FrontRoomsStairKit.FrameInside;
        Box(t, "Frame " + name + " -x", m, new Vector3(-(half + frame * .5f), y + headBottom * .5f, zc), new Vector3(frame, headBottom, depth), false);
        Box(t, "Frame " + name + " +x", m, new Vector3(half + frame * .5f, y + headBottom * .5f, zc), new Vector3(frame, headBottom, depth), false);
        Box(t, "Frame " + name + " head", m, new Vector3(0f, y + OpeningHeight + frame * .5f, zc), new Vector3(2f * half, frame, depth), false);
    }

    /// <summary>A stand-in leaf in its hinge frame: centred on the axis, from the hinge (z 0) along +Z, the 2 cm gap at the top.</summary>
    Mesh LeafMesh(float w, float h)
    {
        var mb = new FrontRoomsStairKit.MB();
        mb.Box(new Vector3(0f, h * .5f, ModuleUnits.DoorLeafGap * .5f + w * .5f), new Vector3(ModuleUnits.DoorLeafThickness, h, w), Quaternion.identity);
        var m = mb.ToMesh("Stair walk / leaf " + w.ToString("0.000"));
        owner.meshes.Add(m);
        return m;
    }

    // Three 4 1/2 in ball-bearing butts on the hinge edge, knuckles on the pull side (stairwell_a's).
    static void Knuckles(Transform leafDress, float pullSign)
    {
        foreach (var y in new[] { .29f, 1.07f, 1.86f })
            Part(leafDress, "hinge knuckle", FrontRoomsStairKit.Knuckle, FrontRoomsStairKit.HingeSteel,
                new Vector3(pullSign * (ModuleUnits.DoorLeafThickness * .5f + .008f), y, -.002f), Quaternion.identity, ShadowCastingMode.On);
    }

    // ---------------------------------------------------------------- the closer (stairwell_a's regular arm; FrontRoomsStairwellDresser)
    sealed class Closer
    {
        public Transform kitRoot, arm, fore, shoe;
        public Vector3 spindle;
        public Quaternion lastRot;
        public Vector3 lastPos;
        public bool solved;
    }

    /// <summary>Kit_DoorCloser_* on the pull face (the kits are authored with +X on the pull side; push = +1 mirrors them, -1 not).</summary>
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

    /// <summary>The arm's elbow from the spindle (on the leaf) and the shoe (fixed): circles of 0.24 and 0.26 m, the root farther from the
    /// leaf; solved again whenever the leaf moved.</summary>
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
        var hgt = Mathf.Sqrt(Mathf.Max(0f, a * a - along * along));
        var dir = (p1 - p0) / dist;
        var mid = p0 + dir * Mathf.Min(along, a);
        var perp = new Vector2(-dir.y, dir.x);
        var e1 = mid + perp * hgt;
        var e2 = mid - perp * hgt;
        var e = e1.x >= e2.x ? e1 : e2;
        c.arm.localPosition = c.spindle;
        c.arm.localRotation = Quaternion.Euler(0f, Yaw(p0, e), 0f);
        c.fore.localPosition = new Vector3(e.x, c.spindle.y, e.y);
        c.fore.localRotation = Quaternion.Euler(0f, Yaw(e, p1), 0f);
    }

    static float Yaw(Vector2 from, Vector2 to) => Mathf.Atan2(-(to.y - from.y), to.x - from.x) * Mathf.Rad2Deg;

    // ---------------------------------------------------------------- signs (3D only: the landed plates; the stencil waits for 平面视觉)
    /// <summary>Sign 7 (AREA OF RESCUE ASSISTANCE, ADAAG 1991 4.3.11 / 4.30.7) over the stair ID sign at both door landings, a 2 in gap,
    /// lit steady on the emergency circuit; the ID plates themselves are baked per floor by SetFloor (BakePlate).</summary>
    void BuildSigns(Transform t)
    {
        const float idX = .88f, idY = 1.524f;
        var gap = .0508f + FrontRoomsStairKit.RescueBox.y * .5f;
        Rescue(t, "exit landing", new Vector3(idX, idY + FrontRoomsStairSigns.SizeOf(FrontRoomsStairSigns.Plate.IdSign).y * .5f + gap, DoorWallZ));
        Rescue(t, "far landing", new Vector3(-idX, FarY + idY + FrontRoomsStairSigns.SizeOf(FrontRoomsStairSigns.Plate.IdSignFar).y * .5f + gap, DoorWallZ));
    }

    void Rescue(Transform t, string where, Vector3 at)
    {
        var r = NewChild(t, "sign 7 AREA OF RESCUE ASSISTANCE (" + where + ")");
        r.localPosition = at;
        r.localRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        Part(r, "box (white enamel steel)", FrontRoomsStairKit.RescueBoxMesh, FrontRoomsStairKit.SignBox, Vector3.zero, Quaternion.identity, ShadowCastingMode.On);
        Part(r, "face (lit, emergency circuit)", FrontRoomsStairKit.RescueFaceMesh, FrontRoomsStairKit.RescueFaceMaterial, Vector3.zero, Quaternion.identity, ShadowCastingMode.Off);
    }

    // ---------------------------------------------------------------- light (look A; the safe zone: own lights, SetLampLevel only)
    /// <summary>
    /// Three Run! B 4 ft two-lamp wraparounds (10_directions 1.3 / 2.4): on the slab over the exit landing, on the +x side wall over the
    /// turn landing at 2.55 m, under the exit landing over the far landing; each with two soft-shadowed spots 0.6 m apart along the lamp
    /// (desktop: the contract's six), one unshadowed spot on the reduced tier. The turn landing's fixture hangs on flight 1's outer wall with
    /// its long axis along the landing and its lens toward the well, so walking down flight 1 shows its end, not its lens: on the far end
    /// wall (the first build) its whole lens face stood in the line of sight down flight 1 and the film of the change counted 4.0 flashes in
    /// the worst second (limit 3). The twin-head battery units at both door landings (heads dark, the red AC pilot lit), aimed at their
    /// door and their flight. No light here is a map fixture: FrontRoomsWarnLamps never selects them.
    /// </summary>
    void BuildLights(Transform t)
    {
        var down = Quaternion.Euler(90f, 0f, 0f);
        Wrap(t, "exit landing", new Vector3(0f, SlabY, DoorWallZ + .80f), Quaternion.identity, down);
        // on the +x wall: the mounting face on the wall (kit +Y -> shell +X), the lamp along the landing (kit +X -> shell +Z), lens toward
        // the well; the spots aim down and out over the landing and a little toward the far wall, which they wash (the wall you walk toward,
        // 10-22) so it does not stand dark against the lit side walls
        Wrap(t, "turn landing (+x wall, 2.55 m)", new Vector3(InnerX, TurnY + TurnLightHeight, TurnLightZ), Quaternion.LookRotation(Vector3.up, Vector3.right),
            Quaternion.LookRotation(new Vector3(-.45f, -1f, .1f), Vector3.up));
        Wrap(t, "far landing (under the exit landing)", new Vector3(0f, -.21f, DoorWallZ + .80f), Quaternion.identity, down);
        TwinHead(t, "exit landing", 0f, new[] { new Vector3(0f, 1.0f, DoorWallZ), new Vector3(FlightX, -.6f, ExitEdgeZ + .6f) });
        TwinHead(t, "far landing", FarY, new[] { new Vector3(0f, FarY + 1.0f, DoorWallZ), new Vector3(-FlightX, FarY + .4f, ExitEdgeZ + .3f) });
    }

    void Wrap(Transform t, string where, Vector3 at, Quaternion mount, Quaternion aim)
    {
        var wrap = Spawn("Kit_WrapFixture_4ft", t, at, mount);
        if (wrap != null)
        {
            wrap.name = "wraparound 4 ft (" + where + ")";
            foreach (var r in wrap.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                var mats = r.sharedMaterials;
                for (var i = 0; i < mats.Length; i++)
                    if (mats[i] != null && mats[i].name.StartsWith("Run_WrapLens")) { lenses.Add(r); lensSlots.Add(i); }
            }
        }
        else Debug.LogWarning("[StairShell] Kit_WrapFixture_4ft is missing: the " + where + " light has no housing");
        // the spots hang 9 cm off the fixture's mounting face, along its lens side, 0.6 m apart along the lamp (the kit's X)
        var off = mount * new Vector3(0f, -.09f, 0f);
        var xs = reduced ? new[] { 0f } : new[] { -.3f, .3f };
        foreach (var x in xs)
        {
            var along = mount * new Vector3(x, 0f, 0f);
            var end = xs.Length < 2 ? "" : Mathf.Abs(along.x) >= Mathf.Abs(along.z) ? (along.x < 0f ? " W" : " E") : (along.z < 0f ? " S" : " N");
            var go = new GameObject(FrontRoomsStairwellDresser.SpotNamePrefix + "walk " + where + end);
            go.transform.SetParent(t, false);
            go.transform.localPosition = at + off + along;
            go.transform.localRotation = aim;
            var l = go.AddComponent<Light>();
            l.type = LightType.Spot;
            l.spotAngle = 170f;
            l.innerSpotAngle = 120f;
            l.range = reduced ? ReducedRange : SpotRange;
            l.color = SpotColour;
            var baseI = reduced ? ReducedIntensity : SpotIntensity;
            l.intensity = baseI;
            l.shadows = reduced ? LightShadows.None : LightShadows.Soft;
            l.shadowStrength = .92f;
            l.shadowNearPlane = .05f;
            l.shadowBias = .02f;
            l.shadowNormalBias = .3f;
            var extra = go.AddComponent<UniversalAdditionalLightData>();
            extra.usePipelineSettings = false;
            spots.Add(l);
            spotBase.Add(baseI);
        }
    }

    /// <summary>The twin-head battery unit high on the -x wall of a door landing (Run! B production kits; stairwell_a's placement rule:
    /// the cabinet's back on the wall from its sidecar bounds, yoke mounts and trunnion from the sidecar anchors), heads aimed, EMT up.</summary>
    void TwinHead(Transform t, string where, float floorY, Vector3[] aims)
    {
        var cabInfo = FrontRoomsKitLibrary.GetInfo("Kit_EmergencyTwinHead");
        var cabBack = cabInfo != null ? cabInfo.Min.z : 0f;
        var cab = Spawn("Kit_EmergencyTwinHead", t, new Vector3(-InnerX - cabBack, floorY + 2.25f, DoorWallZ + .55f), Quaternion.Euler(0f, 90f, 0f));
        if (cab == null) return;
        cab.name = "twin-head unit (" + where + ")";
        foreach (var cr in cab.GetComponentsInChildren<Renderer>(true))
        {
            var mats = cr.sharedMaterials;
            for (var i = 0; i < mats.Length; i++)
                if (mats[i] != null && mats[i].name.StartsWith("Run_NeonPilot")) { pilots.Add(cr); pilotSlots.Add(i); }
        }
        var mounts = new[] { KitAnchor(cabInfo, "mount_l", new Vector3(.112f, .201f, .0593f)), KitAnchor(cabInfo, "mount_r", new Vector3(-.112f, .201f, .0593f)) };
        var trunnion = KitAnchor(FrontRoomsKitLibrary.GetInfo("Kit_EmergencyTwinHead_Yoke"), "trunnion", new Vector3(0f, .085f, 0f));
        for (var i = 0; i < 2; i++)
        {
            var yoke = Spawn("Kit_EmergencyTwinHead_Yoke", cab, mounts[i], Quaternion.identity);
            if (yoke == null) continue;
            var head = Spawn("Kit_EmergencyTwinHead_Lamp", yoke, trunnion, Quaternion.identity);
            if (head == null) continue;
            var dir = t.TransformPoint(aims[i]) - head.position;
            var dCab = cab.InverseTransformDirection(dir);
            yoke.localRotation = Quaternion.Euler(0f, Mathf.Atan2(dCab.x, dCab.z) * Mathf.Rad2Deg, 0f);
            var dYoke = yoke.InverseTransformDirection(dir);
            head.localRotation = Quaternion.Euler(Mathf.Atan2(-dYoke.y, dYoke.z) * Mathf.Rad2Deg, 0f, 0f);
        }
        // EMT from the unit up to the slab over it (the exit landing's) or to the exit landing's pan (the far landing's)
        var feed = t.InverseTransformPoint(cab.TransformPoint(KitAnchor(cabInfo, "feed", new Vector3(0f, .1965f, .03f))));
        var top = floorY < -1f ? -.205f : SlabY;
        Part(t, "unit feed EMT (" + where + ")", FrontRoomsStairKit.EmtRun(Mathf.Max(.05f, top - feed.y)), FrontRoomsStairKit.Conduit, feed, Quaternion.identity, ShadowCastingMode.On);
    }

    static Vector3 KitAnchor(FrontRoomsKitLibrary.Info info, string anchor, Vector3 fallback) =>
        info != null && info.TryAnchor(anchor, out var p) ? p : fallback;

    // ---------------------------------------------------------------- probes (60 s6.7: box probes as children of the shell)
    void BuildProbes(Transform t)
    {
        Probe(t, "upper (exit landing, flights, turn)", TurnY - .1f, SlabY);
        Probe(t, "lower (far landing)", FarY, TurnY + .5f);
    }

    void Probe(Transform t, string name, float y0, float y1)
    {
        var go = new GameObject("Stair walk reflection " + name);
        go.transform.SetParent(t, false);
        go.transform.localPosition = new Vector3(0f, (y0 + y1) * .5f, (DoorWallZ + FarWallZ) * .5f);
        var probe = go.AddComponent<ReflectionProbe>();
        probe.size = new Vector3(2f * InnerX, y1 - y0, FarWallZ - DoorWallZ);
        probe.center = Vector3.zero;
        probe.boxProjection = true;
        probe.importance = 10;
        probe.blendDistance = .25f;
        probe.resolution = 64;
        probe.hdr = true;
        probe.nearClipPlane = .05f;
        probe.farClipPlane = 7f;
        if (!reduced && QualitySettings.realtimeReflectionProbes)
        {
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
        }
        else
        {
            probe.mode = ReflectionProbeMode.Custom;
            probe.customBakedTexture = FixedCube();
        }
        probes.Add(probe);
    }

    static Cubemap fixedCube;

    /// <summary>The reduced tier's reflection: the shaft's mean radiance under its lamps (putty block, grey fill, the lamp's tint up).</summary>
    static Cubemap FixedCube()
    {
        if (fixedCube != null) return fixedCube;
        var c = new Cubemap(8, TextureFormat.RGBAHalf, false) { name = "Stair walk fixed reflection", hideFlags = HideFlags.DontSave };
        var wall = (Color)FrontRoomsStairKit.BlockPutty.linear * .30f;
        var floorC = (Color)FrontRoomsStairKit.TreadFill.linear * .24f;
        var lamp = new Color(1.4f, 1.36f, 1.3f);
        var px = new Color[64];
        foreach (var f in new[] { CubemapFace.PositiveX, CubemapFace.NegativeX, CubemapFace.PositiveZ, CubemapFace.NegativeZ, CubemapFace.PositiveY, CubemapFace.NegativeY })
        {
            var col = f == CubemapFace.NegativeY ? floorC : f == CubemapFace.PositiveY ? Color.Lerp(wall, lamp, .2f) : wall;
            for (var i = 0; i < px.Length; i++) px[i] = col;
            c.SetPixels(px, f);
        }
        c.Apply(false, true);
        fixedCube = c;
        return c;
    }

    // ---------------------------------------------------------------- the walkable colliders (60 s6.3)
    /// <summary>
    /// One smooth ramp per flight on the nosing line (the visible steps carry none), run one going past the last nosing so it meets the
    /// landing below at its own level; a box per landing (the exit landing reaches under the EXIT's line, so the doorway has floor); thin
    /// guard boxes 1.1 m over the walking line on every open edge; the headroom barrier, the hose stations and the riser. The shaft floor
    /// (the far landing) and the walls are boxes from BuildShell. CharacterController (stepOffset 0.30, skin 0.03) stays grounded down
    /// the 32 deg ramp at 3.2-5.3 m/s (60 s6.3).
    /// </summary>
    void BuildColliders(Transform t)
    {
        var f1 = new Vector3(FlightX, 0f, ExitEdgeZ);
        var f2 = new Vector3(-FlightX, TurnY, TurnEdgeZ);
        var flip = Quaternion.Euler(0f, 180f, 0f);
        var run = Going * (Treads + 1);
        var drop = Rise * (Treads + 1);
        var len = Mathf.Sqrt(run * run + drop * drop);
        var d = new Vector3(0f, -drop, run) / len;
        var n = new Vector3(0f, run, drop) / len;
        const float thick = .2f;
        var mid = new Vector3(0f, -drop * .5f, run * .5f) - n * (thick * .5f);
        Sloped("flight 1 ramp", f1 + mid, Quaternion.LookRotation(d, n), new Vector3(2f * FlightHalf, thick, len));
        Sloped("flight 2 ramp", f2 + flip * mid, flip * Quaternion.LookRotation(d, n), new Vector3(2f * FlightHalf, thick, len));
        BoxCol("exit landing", new Vector3(0f, -.125f, (CorridorFaceZ + ExitEdgeZ) * .5f), new Vector3(2f * InnerX, .25f, ExitEdgeZ - CorridorFaceZ));
        BoxCol("turn landing", new Vector3(0f, TurnY - .125f, (TurnEdgeZ + FarWallZ) * .5f), new Vector3(2f * InnerX, .25f, FarWallZ - TurnEdgeZ));
        // the flight guards on the well-side stringers, from post to post along the nosing line
        var gLen = Mathf.Sqrt(2.62f * 2.62f + (2.62f * Rise / Going) * (2.62f * Rise / Going));
        var g = new Vector3(0f, -Rise / Going, 1f).normalized;
        var gUp = Vector3.Cross(g, Vector3.right);
        if (gUp.y < 0f) gUp = -gUp;
        var gMid = new Vector3(-FlightHalf, -1.25f * Rise / Going + .55f, 1.25f);
        Sloped("flight 1 guard", f1 + gMid, Quaternion.LookRotation(g, gUp), new Vector3(.05f, 1.1f, gLen));
        Sloped("flight 2 guard", f2 + flip * gMid, flip * Quaternion.LookRotation(g, gUp), new Vector3(.05f, 1.1f, gLen));
        var landingGuard = InnerX - (FlightX - FlightHalf);
        BoxCol("exit landing guard", new Vector3(-InnerX + landingGuard * .5f, .55f, ExitEdgeZ - .035f), new Vector3(landingGuard, 1.1f, .05f));
        BoxCol("turn landing well guard", new Vector3(0f, TurnY + .55f, TurnEdgeZ + .035f), new Vector3(2f * WellHalf, 1.1f, .05f));
        BoxCol("headroom barrier", new Vector3(0f, FarY + .475f, BarrierZ), new Vector3(2f * InnerX, .95f, .06f));
        // the hose stations (valve, outlet and cap at 1.22 m) and the riser
        foreach (var y0 in new[] { 0f, FarY })
            BoxCol("hose station", new Vector3(RiserXZ.x - .13f, y0 + 1.2f, RiserXZ.y), new Vector3(.46f, 1.24f, .19f));
        BoxCol("standpipe riser", new Vector3(RiserXZ.x, (FarY + SlabY) * .5f, RiserXZ.y), new Vector3(.13f, SlabY - FarY, .13f));
    }

    void BoxCol(string name, Vector3 centre, Vector3 size)
    {
        var c = NewChild(colliderRoot, name).gameObject.AddComponent<BoxCollider>();
        c.center = centre;
        c.size = size;
    }

    void Sloped(string name, Vector3 centre, Quaternion rot, Vector3 size)
    {
        var go = NewChild(colliderRoot, name);
        go.localPosition = centre;
        go.localRotation = rot;
        go.gameObject.AddComponent<BoxCollider>().size = size;
    }

    // ---------------------------------------------------------------- helpers
    /// <summary>A box of the shell: merged into its material's mesh (planar metre UVs in the shell's frame, so world-projected
    /// materials see the same faces and the mesh-UV ones move with the shell), and a box collider when solid.</summary>
    void Box(Transform t, string name, Material material, Vector3 centre, Vector3 size, bool solid)
    {
        if (!batches.TryGetValue(material, out var mb))
        {
            mb = new FrontRoomsStairKit.MB();
            batches[material] = mb;
        }
        PlanarBox(mb, centre, size);
        if (solid) BoxCol(name, centre, size);
    }

    /// <summary>Six faces, each a quad whose U runs along the planar frame's tangent (FrontRoomsSurface PlanarFrame: t = up x n, or +x
    /// on horizontal faces; b = n x t) with metre UVs from the shell-frame position, so normal maps keep their frame.</summary>
    static void PlanarBox(FrontRoomsStairKit.MB mb, Vector3 c, Vector3 size)
    {
        var h = size * .5f;
        foreach (var n in new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back })
        {
            var tg = Mathf.Abs(n.y) > .5f ? Vector3.right : Vector3.Cross(Vector3.up, n).normalized;
            var bt = Vector3.Cross(n, tg);
            var fc = c + Vector3.Scale(n, h);
            var hu = Mathf.Abs(Vector3.Dot(h, tg));
            var hv = Mathf.Abs(Vector3.Dot(h, bt));
            Vector3 a = fc - tg * hu - bt * hv, b = fc + tg * hu - bt * hv, cc = fc + tg * hu + bt * hv, dd = fc - tg * hu + bt * hv;
            Vector2 Uv(Vector3 p) => new Vector2(Vector3.Dot(p, tg), Vector3.Dot(p, bt));
            mb.Quad(a, b, cc, dd, n, Uv(a), Uv(b), Uv(cc), Uv(dd));
        }
    }

    void FlushBatches(Transform t)
    {
        foreach (var kv in batches)
        {
            if (kv.Value.v.Count == 0) continue;
            MeshPart(t, "shell / " + kv.Key.name, kv.Value, kv.Key, ShadowCastingMode.On);
        }
        batches.Clear();
    }

    MeshRenderer MeshPart(Transform t, string name, FrontRoomsStairKit.MB mb, Material material, ShadowCastingMode shadows)
    {
        var mesh = mb.ToMesh("Stair walk / " + name);
        owner.meshes.Add(mesh);
        return Part(t, name, mesh, material, Vector3.zero, Quaternion.identity, shadows);
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

    static GameObject KitUnder(Transform parent, string kit, float push, string label)
    {
        var root = NewChild(parent, label + " root");
        root.localScale = new Vector3(-push, 1f, 1f);
        return FrontRoomsKitLibrary.Spawn(kit, root, Vector3.zero, Quaternion.identity, null, false, label, false);
    }

    static Transform Spawn(string kit, Transform parent, Vector3 position, Quaternion rotation)
    {
        var go = FrontRoomsKitLibrary.Spawn(kit, parent, position, rotation, null, false, kit, false);
        return go != null ? go.transform : null;
    }

    static void Kill(UnityEngine.Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
    }
}
