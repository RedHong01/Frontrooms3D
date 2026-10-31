using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>States of a Run! B room (run_b_production/10_plan.md s.1.4).</summary>
public enum RunRigState : byte { Armed, Tripping, Tripped, Resetting }

/// <summary>Who drives the armed ceiling lamps: a host (its own lamps, the rig supplies MainsAt), or the rig itself
/// (its own wraparounds; the map module, CR-4). Since call 6 / C40 (2026-10-31) no host drives a Run! room: the title
/// stream's Run room stays on Ward, so HostLamps is kept compiled but has no caller in the game.</summary>
public enum RunRigHost : byte { HostLamps, OwnLamps }

/// <summary>One twin-head battery unit: its wall, its z and where its two heads aim (rig space).</summary>
[Serializable]
public struct RunUnitDef
{
    public float z;
    /// <summary>+1 on the right wall (x = +halfWidth), -1 on the left.</summary>
    public float side;
    /// <summary>When true, both aims follow the r8 rule (pitch 30 deg, across the axis at aimCross).</summary>
    public bool autoAim;
    public Vector3 aimMinus;
    public Vector3 aimPlus;
    /// <summary>The +Z head is the goal head (narrow beam on the goal, goalHeadGain).</summary>
    public bool goalPlus;
}

/// <summary>A per-state box probe (VL195 / VL236): centre and size in rig space.</summary>
[Serializable]
public struct RunProbeBox
{
    public Vector3 centre;
    public Vector3 size;
}

/// <summary>A wall face for the dress (rig space): from start along a unit direction for length, normal into the room;
/// gaps are (from, to) distances along the face that stay bare (doors, cabinets). On a short (end) face a gap is an
/// opening: gapTops[i] is its clear top (null or 0: 2.2 m, the map's arch top), the skin and shell close the wall above it.</summary>
[Serializable]
public struct RunWallFace
{
    public Vector3 start;
    public Vector3 along;
    public float length;
    public Vector3 normal;
    public Vector2[] gaps;
    public float[] gapTops;

    public float GapTop(int i) => gapTops != null && i < gapTops.Length && gapTops[i] > 0f ? gapTops[i] : 2.2f;
}

/// <summary>A wall-mounted dress item: kit, the wall point (rig space) and the wall's inward normal.</summary>
[Serializable]
public struct RunWallItem
{
    public string kit;
    public Vector3 point;
    public Vector3 normal;
}

/// <summary>A locked, non-opening RN-K side door prop (Kit_DoorFrame_Steel_DA + Kit_DoorLeaf_SteelRun_DA, a dead F5 lite, no
/// closer): its centre z, the wall side, and its face (staff, electrical, stencil).</summary>
[Serializable]
public struct RunSideDoor
{
    public float z;
    public float side;
    public string face;
}

/// <summary>A floor obstacle kit at a rig-space position and yaw.</summary>
[Serializable]
public struct RunObstacle
{
    public string kit;
    public Vector3 position;
    public float yaw;
}

/// <summary>
/// The geometry and values one Run! B room needs, in rig space: x = 0 on the room's axis, +Z from the entry
/// to the goal, y = 0 on the floor; z = 0 is the entrance wall's centre line (the map's cell line) and z = length the goal
/// wall's. A plain data class with one preset: Corridor1x9 (the map's Run_AlarmCorridor_1x9, FrontRoomsRunRooms). The
/// StreamRoom preset left with the RoomStream hunk (call 6 / C40, 2026-10-31). Values marked SP in the plan are starting
/// points, tuned against the VL338-VL341 gates in the combined test.
/// </summary>
[Serializable]
public sealed class RunRigLayout
{
    public string name = "Run";
    public float halfWidth = 1.42f;
    public float length = 27f;
    public float height = 2.9f;
    /// <summary>The map's wall (ModuleUnits.WallThickness): the end faces sit half of it inside z 0 and z length.</summary>
    public float wallThickness = .16f;
    public float WallHalf => wallThickness * .5f;

    [Header("Twin-head units")]
    public RunUnitDef[] units = new RunUnitDef[0];
    public float unitBoxCentreY = 2.45f;
    public float aimCross = .55f;
    public float floodPitch = 30f;
    public float floodOuter = 100f, floodInner = 96f, floodIntensity = 10f, floodRange = 11f, floodShadowStrength = .95f;
    public Color floodColor = new Color(1f, .84f, .64f);
    public float goalOuter = 26f, goalInner = 22f, goalRange = 13f, goalPitch = 13f;
    /// <summary>The goal head's gain over a flood head (call 4: one parameter). r8: 5.</summary>
    public float goalHeadGain = 5f;
    public float beamDensity = .05f, beamGain = 16f, beamLength = 2.5f, beamSlopeDegrees = 14f;
    public Color headLensEmission = new Color(1f, .84f, .64f) * 8f;
    public Color pilotEmission = new Color(2.6f, .20f, .10f);

    /// <summary>The goal door's opening centre on the goal wall's centre line, at the head's aim height (rig space).</summary>
    [Header("Goal (the map's door: RN-F + F5, never locked, C38 / M8; the rig builds no door)")]
    public Vector3 goalPoint = new Vector3(0f, 1.05f, 27f);
    /// <summary>The goal wall's normal into the corridor.</summary>
    public Vector3 goalNormal = Vector3.back;
    // C41 (Red 2026-10-31): no EXIT sign anywhere in a Run! room (the flag sign at z 13.5 and the wall sign over the goal
    // door are gone with their face lights). EXIT stays the floor's one way down; the fire gear keeps the red.

    /// <summary>One unshadowed spot past the goal wall, aimed away from the door at what the lite looks onto; exempt from the
    /// rig's cull; fades out as the far room's own lamp comes on (FarRoomLamp); untouched by the trip. Never in the WebGL tier
    /// (no lite there). SP values, tuned in the combined test (the lit square from 25 m and from 8 m).</summary>
    [Header("Goal window light (C42): the F5 lite reads as a lit square from the trip line")]
    public bool goalWindowLight = true;
    /// <summary>Past the goal wall's far face (m), height above the floor (under a Low zone's 2.4 m ceiling), pitch down.</summary>
    public float goalLightBehind = .45f, goalLightHeight = 2.2f, goalLightPitch = 32f;
    public float goalLightOuter = 120f, goalLightInner = 80f, goalLightRange = 8f, goalLightIntensity = 5f;
    /// <summary>The far zone's lamp colour (the binder sets it from the zone's theme; Level 0's by default).</summary>
    public Color goalLightColor = new Color(1f, .96f, .88f);
    /// <summary>Without a map hook: the far room's first lamp this far past the goal wall's centre line, and the map's lamp
    /// cull (lightRadius 16 m, faded over its last 3 m), so the light crossfades against the lamp's own fade-in.</summary>
    public float farLampDistance = 1.8f, farLampRadius = 16f, farLampFade = 3f;
    /// <summary>When the far room has no lamp (FarRoomLamp below 0): the light fades out between these distances (m, viewer
    /// to the goal point), so the lite shows the far room as it is once the player is at the door.</summary>
    public float goalLightNearStart = 6f, goalLightNearEnd = 2.5f;
    /// <summary>Off beyond this distance from the goal point (the lite is a few pixels there).</summary>
    public float goalLightMaxDistance = 40f;

    [Header("Own lamps (OwnLamps host only)")]
    public Vector3[] ownLamps = new Vector3[0];
    public int[] ownLampShadows = new int[0];
    public int stutterLamp = -1;
    public float stutterLevel = .78f;
    public float wrapIntensity = 5.5f, wrapRange = 10f, wrapOuter = 172f, wrapInner = 124f;
    public Color wrapColor = new Color(.90f, .95f, 1f);
    public Color wrapLensEmission = new Color(.90f, .95f, 1f) * 2.8f;
    public Color haloColor = new Color(.45f, .47f, .50f);

    [Header("Stage-1 / 2 warning on the rig's own wraps (CR-4; the map's burst envelope, FrontRoomsWarnLamps M3)")]
    public float warnDipSpacing = .55f, warnAttack = .05f, warnRelease = .30f, warnTail = .09f, warnHoldMin = .04f, warnHoldMax = .08f;
    public float warnFirstDepthMin = .55f, warnFirstDepthMax = .62f, warnLaterDepthMin = .65f, warnLaterDepthMax = .75f, warnJitter = .04f;
    public float warnReducedDepth = .7f, warnReducedSeconds = .6f;

    [Header("Timing (s)")]
    public float mainsDrop = .06f, headDelay = .35f, headStagger = .004f, headRise = .18f;
    public float afterglow = .03f, afterglowTau = .5f;
    public float reducedEnvelope = .5f, reducedHeadDelay = .25f;
    public float resetCellSeconds = .15f, resetCellLength = 3f, resetFlickOn = .08f, resetFlickPeriod = .34f, resetRise = .4f;

    [Header("Ambient (SH through property blocks, D9 bounce model)")]
    public float armedDown = .40f, armedSide = .25f, armedUp = .06f;
    public Color armedTint = new Color(.93f, .97f, 1f);
    public float trippedDown = .070f, trippedSide = .050f, trippedUp = .025f;
    public Color trippedTint = Color.white;
    public float unitDown = .20f, unitSide = .12f, unitUp = .07f;
    public Color unitTint = new Color(1f, .90f, .78f);
    /// <summary>Renderers whose bottom is above this height (soffit, main, tray, EMT) take the "lit from the floor"
    /// tripped set below instead of the four look-dev floor-bounce spots (not shipped). SP.</summary>
    public float highBand = 2.4f;
    public float highTrippedDown = .12f, highTrippedSide = .06f, highTrippedUp = .025f;

    [Header("Reflections, post, culling")]
    public RunProbeBox[] probes = new RunProbeBox[0];
    public float probeBlend = .15f;
    public int probeImportance = 2;
    public Vector3 postCentre = new Vector3(0f, 1.45f, 13.5f), postSize = new Vector3(3f, 2.9f, 27f);
    public float postBlend = 1f;
    public float cullDistance = 16f, cullDistanceWebGL = 10f;

    /// <summary>Look-dev only: the dress builds a shell of its own (0.1 m slabs with colliders, the floor's colliders).</summary>
    [Header("Dress (FrontRoomsRunDress)")]
    public bool buildShell;
    /// <summary>On the map (the binder): the map builds the structural shell (collision, both faces, its Run finish); the dress
    /// skins the corridor's inner faces (upper tone, soffit) 2 mm proud with its own renderers, so the rig's ambient, the
    /// lower tone and the VCT own every visible surface. The VCT lies 1 mm over the map's floor.</summary>
    public bool skinShell;
    public RunWallFace[] walls = new RunWallFace[0];
    public float floorX0 = -1.42f, floorX1 = 1.42f, floorZ0 = 0f, floorZ1 = 27f;
    public float laneHalf = .45f;
    public float[] accentRowsX = new float[0];
    public float accentWidth = .30f;
    public float waxBand = .15f;
    public float lowerToneTop = 1.2f, railY = .9f, coveHeight = .102f;
    public float mainX = .55f, mainY = 2.62f, trayX = -.60f, trayY = 2.70f;
    public float emtWallX = -1.42f;
    public float[] emtY = { 2.72f, 2.76f, 2.80f };
    public float runZ0 = 0f, runZ1 = 27f;
    public RunWallItem[] wallItems = new RunWallItem[0];
    public RunSideDoor[] sideDoors = new RunSideDoor[0];
    public RunObstacle[] obstacles = new RunObstacle[0];

    public Vector3 RightNormal => Vector3.left;
    public Vector3 LeftNormal => Vector3.right;

    static RunWallFace Face(Vector3 start, Vector3 along, float length, Vector3 normal, params Vector2[] gaps)
    {
        return new RunWallFace { start = start, along = along, length = length, normal = normal, gaps = gaps };
    }

    /// <summary>The map's Run! corridor (FrontRoomsRunRooms, CR-1): 1 x 9 cells, 27 m between the end walls' centre lines,
    /// faces at x +-1.42 and z 0.08 / 26.92, soffit 2.9; the entrance arch <paramref name="archWidth"/> wide (1.4, top 2.2)
    /// and the goal door <paramref name="doorWidth"/> wide (1.0, top 2.1), both centred (the r8 corridor, 11_run_dir_emergency_power.md s.2).</summary>
    public static RunRigLayout Corridor1x9(float archWidth = 1.4f, float doorWidth = 1f)
    {
        var w = 1.42f;
        const float wh = .08f;
        var ah = archWidth * .5f;
        var dh = doorWidth * .5f;
        var l = new RunRigLayout
        {
            name = "Corridor1x9",
            halfWidth = w, length = 27f, height = 2.9f,
            units = new[]
            {
                new RunUnitDef { z = 3f, side = 1f, autoAim = true },
                new RunUnitDef { z = 9f, side = -1f, autoAim = true },
                new RunUnitDef { z = 15f, side = 1f, autoAim = true },
                new RunUnitDef { z = 21f, side = -1f, autoAim = true, goalPlus = true },
            },
            goalPoint = new Vector3(0f, 1.05f, 27f),
            stutterLamp = 4,
            ownLampShadows = new[] { 0, 3, 6 },
            probes = new[]
            {
                new RunProbeBox { centre = new Vector3(0f, 1.45f, 4.5f), size = new Vector3(3.16f, 3.22f, 9.6f) },
                new RunProbeBox { centre = new Vector3(0f, 1.45f, 13.5f), size = new Vector3(3.16f, 3.22f, 9.6f) },
                new RunProbeBox { centre = new Vector3(0f, 1.45f, 22.46f), size = new Vector3(3.16f, 3.22f, 9.6f) },
            },
            postCentre = new Vector3(0f, 1.45f, 13.5f), postSize = new Vector3(3f, 2.9f, 27f),
            buildShell = true,
            floorX0 = -w, floorX1 = w, floorZ0 = 0f, floorZ1 = 27f,
            laneHalf = .45f, accentRowsX = new[] { -.6f, .6f },
            mainX = .55f, trayX = -.60f, emtWallX = -w, runZ0 = 0f, runZ1 = 27f,
        };
        l.ownLamps = new Vector3[9];
        for (var k = 0; k < 9; k++) l.ownLamps[k] = new Vector3(0f, 2.9f, 1.8f + 3f * k);
        // Long faces run the full 27 m (their first and last 0.08 m lie inside the end walls); the side-door gaps are the
        // leaf (+-0.5) plus 45 mm under the F4 casing (+-0.549), so the lower tone tucks under the casing and the rail stops clear.
        const float dg = .545f;
        var goal = Face(new Vector3(-w, 0f, 27f - wh), Vector3.right, w * 2f, Vector3.back, new Vector2(w - dh, w + dh));
        goal.gapTops = new[] { 2.1f };
        var entry = Face(new Vector3(-w, 0f, wh), Vector3.right, w * 2f, Vector3.forward, new Vector2(w - ah, w + ah));
        entry.gapTops = new[] { 2.2f };
        l.walls = new[]
        {
            Face(new Vector3(-w, 0f, 0f), Vector3.forward, 27f, Vector3.right, new Vector2(10.04f, 10.36f), new Vector2(17f - dg, 17f + dg)),
            Face(new Vector3(w, 0f, 0f), Vector3.forward, 27f, Vector3.left, new Vector2(5f - dg, 5f + dg), new Vector2(23f - dg, 23f + dg)),
            entry,
            goal,
        };
        l.wallItems = new[]
        {
            new RunWallItem { kit = "Kit_PullStation", point = new Vector3(w, 1.2f - .09f, 1f), normal = Vector3.left },
            new RunWallItem { kit = "Kit_FireBell", point = new Vector3(w, 2.3f, 1f), normal = Vector3.left },
            new RunWallItem { kit = "Kit_ExtinguisherCabinet", point = new Vector3(-w, 1.1f - .305f, 10.2f), normal = Vector3.right },
            // The NOTICE plate on the host side of the entrance wall (its outside face, z -0.08: F8, it was buried in the
            // wall), right of the arch seen from outside; the ALARM ZONE card goes on the left (FrontRoomsRunRigBinder).
            new RunWallItem { kit = "Kit_SafetyPlate_10x7", point = new Vector3(Mathf.Min(ah + .40f, w - .15f), 1.5f, -wh), normal = Vector3.back },
        };
        l.sideDoors = new[]
        {
            new RunSideDoor { z = 5f, side = 1f, face = "electrical" },
            new RunSideDoor { z = 17f, side = -1f, face = "staff" },
            new RunSideDoor { z = 23f, side = 1f, face = "stencil" },
        };
        l.obstacles = new[]
        {
            new RunObstacle { kit = "Kit_ChairDolly8", position = new Vector3(1.0f, 0f, 6.4f), yaw = 90f },
            new RunObstacle { kit = "Kit_JanitorCart", position = new Vector3(-.63f, 0f, 12.0f), yaw = 90f },
            new RunObstacle { kit = "Kit_HandTruck_Cartons", position = new Vector3(1.05f, 0f, 18.2f), yaw = -90f },
        };
        return l;
    }
}

/// <summary>
/// Run! direction B (EMERGENCY POWER), production rig (run_b_production/10_plan.md): the lights and states of one
/// Run room. It knows nothing about the map or the stream: a RunRigLayout says where things are, a host says who
/// owns the armed ceiling lamps.
///   Armed: wraparounds on (cell 5 stutters dim), red AC ON pilots lit, heads off, cool bounce ambient.
///   Trip (t 0): the mains drop (every wrap 1 -> 0 in 0.06 s, lens afterglow 0.03 decaying tau 0.5 s), pilots out,
///   0.35 s of black, then each head rises 0 -> 1 over 0.18 s (ease-in) from 0.35 + 0.004 z: hard warm pools.
///   No EXIT sign (C41, 2026-10-31). REDUCE FLASHING: no black gap, wraps fade over 0.5 s, heads rise over 0.5 s from 0.25 s.
///   Goal window light (C42): one unshadowed spot past the goal wall, outside the trip and the cull, crossfaded against
///   the far room's lamp (FarRoomLamp) so the goal door's F5 lite reads as a lit square from the trip line. Not on WebGL.
///   Warn (CR-4, armed): the map's stage-1 / 2 burst envelope on the rig's own wraps (Warn(stage)).
///   Reset (map only; the stream resets out of view): heads out in one change, wraps re-strike in a sweep from the
///   entrance, 0.15 s a cell, two flicks at least 0.34 s apart, pilots relight.
/// Evaluate(t) is deterministic (look-dev, tests); Tick(dt) allocates nothing. Events carry world positions for the
/// sound chat (CR-S3); the rig plays no audio (AUDIO_CONTRACT). WebGL tier: inside
/// (UNITY_WEBGL &amp;&amp; !UNITY_EDITOR) || FRONTROOMS_WEBGL_PREVIEW, one 80-degree spot per unit, no cookies, cones,
/// halos or box probes, only the nearest head shadowed.
/// </summary>
[DisallowMultipleComponent]
public sealed class FrontRoomsRunRig : MonoBehaviour
{
#if (UNITY_WEBGL && !UNITY_EDITOR) || FRONTROOMS_WEBGL_PREVIEW
    public static readonly bool WebGLTier = true;
#else
    public static readonly bool WebGLTier = false;
#endif

    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    static readonly int DensityId = Shader.PropertyToID("_Density");

    // ---------------------------------------------------------------- built parts
    sealed class Head
    {
        public Light light;
        public Renderer[] lens;      // every LOD renderer of the lamp kit
        public int[] lensIndex;
        public Renderer beam;
        public float z;
        public float baseIntensity;
        public bool goal;
        public bool hasLight;
        public Vector3 world;
        public int unit;
        public bool relayFired;
    }

    sealed class Wrap
    {
        public Light light;
        public Renderer[] lens;
        public int[] lensIndex;
        public Renderer halo;
        public float level = 1f;
        public float z;
    }

    struct Emissive
    {
        public Renderer renderer;
        public int index;
    }

    // Per ambient renderer: its SH set (0 low, 1 high, 2 unit) and whether it was CustomProvided by us.
    struct AmbientEntry
    {
        public Renderer renderer;
        public int materials;     // cached: Renderer.sharedMaterials allocates
        public byte kind;
        public bool host;
        public LightProbeUsage original;
    }

    public RunRigLayout Layout { get; private set; }
    public RunRigHost Host { get; private set; }
    public RunRigState State { get; private set; } = RunRigState.Armed;
    /// <summary>Seconds since the trip (Tripping / Tripped) or since the reset began (Resetting); -1 armed.</summary>
    public float Clock { get; private set; } = -1f;
    /// <summary>Host lamps: true when MainsAt changed this Tick (the host re-applies its lamp outputs).</summary>
    public bool MainsDirty { get; private set; }
    public int HeadCount => heads == null ? 0 : heads.Length;
    public int OwnLampCount => wraps == null ? 0 : wraps.Length;
    public int ProbeCount => (armedProbes == null ? 0 : armedProbes.Length) + (trippedProbes == null ? 0 : trippedProbes.Length);
    /// <summary>C42: the goal window light (null in the WebGL tier or when the layout has none).</summary>
    public Light GoalWindowLight => goalLight;
    /// <summary>
    /// C42: the far room's lamp, 0 (dark) .. 1 (fully lit, as the map's lamp tick applies it: level x its distance fade), or
    /// below 0 when the far room has no lamp. Set by the map hook (FrontRoomsRunRigBinder); null: the rig's estimate (a lamp at
    /// farLampDistance past the goal wall, culled by the map's 16 m / 3 m rule against the viewer). Called once per Tick; it
    /// must not allocate.
    /// </summary>
    public Func<float> FarRoomLamp { get; set; }
    /// <summary>The goal window light's weight last Tick (0 off .. 1 full; tests and tools).</summary>
    public float GoalLightWeight { get; private set; }

    /// <summary>Raised on every state change, with the room's centre.</summary>
    public event Action<RunRigState, Vector3> StateChanged;
    /// <summary>Raised at t 0 with the room's centre (the contactor drop-out).</summary>
    public event Action<Vector3> MainsDropped;
    /// <summary>Raised once per head as its transfer relay pulls in (head index, the head's position).</summary>
    public event Action<int, Vector3> TransferRelay;
    /// <summary>Raised when an animated reset begins.</summary>
    public event Action<Vector3> ResetStarted;

    Head[] heads;
    Wrap[] wraps;
    Emissive[] pilots;
    AmbientEntry[] ambient;
    ReflectionProbe[] armedProbes, trippedProbes;
    Light goalLight;
    Transform viewer;
    Light[] cullLights;
    float warn = 1f;
    // CR-4 burst (armed only): its clock (-1 none), dip count, per-dip depth and hold, the reduced sag, and per-wrap jitter.
    float burstClock = -1f, burstEnd;
    int burstDips, burstCount;
    bool burstReduced;
    readonly float[] dipDepth = new float[3], dipHold = new float[3];
    float[] wrapJitter;
    float stutterClock;
    float lastMains = -1f;
    bool hostAcquired;
    float reducedMix;
    MaterialPropertyBlock block;
    SphericalHarmonicsL2 armedSH, trippedSH, highTrippedSH, unitTrippedSH;
    readonly SphericalHarmonicsL2[] shScratch = new SphericalHarmonicsL2[1];
    float lastAmbientW = -1f, lastAmbientH = -1f;
    float maxHeadZ;
    Material beamMaterial, haloMaterial;
    static Mesh floodCone, goalCone, haloQuad;

    // ---------------------------------------------------------------- construction
    /// <summary>
    /// Build the rig under <paramref name="parent"/> (a child "Run B / rig"): units, heads, signs, own lamps (OwnLamps),
    /// probes and the Run_B post volume. <paramref name="hostRenderers"/> (may be null) are the host's shell renderers
    /// (walls, floor, ceiling) that take the rig's ambient while the host acquires them (AcquireHost).
    /// </summary>
    public static FrontRoomsRunRig Create(Transform parent, RunRigLayout layout, RunRigHost host, Renderer[] hostRenderers = null, Transform dressRoot = null)
    {
        var go = new GameObject("Run B / rig");
        go.transform.SetParent(parent, false);
        var rig = go.AddComponent<FrontRoomsRunRig>();
        rig.Build(layout, host, hostRenderers, dressRoot);
        return rig;
    }

    void Build(RunRigLayout layout, RunRigHost host, Renderer[] hostRenderers, Transform dressRoot)
    {
        Layout = layout ?? RunRigLayout.Corridor1x9();
        Host = host;
        block = new MaterialPropertyBlock();
        armedSH = BounceSH(Layout.armedUp, Layout.armedSide, Layout.armedDown, Layout.armedTint);
        trippedSH = BounceSH(Layout.trippedUp, Layout.trippedSide, Layout.trippedDown, Layout.trippedTint);
        highTrippedSH = BounceSH(Layout.highTrippedUp, Layout.highTrippedSide, Layout.highTrippedDown, Layout.trippedTint);
        unitTrippedSH = BounceSH(Layout.unitUp, Layout.unitSide, Layout.unitDown, Layout.unitTint);
        if (!WebGLTier)
        {
            var beamShader = Resources.Load<Shader>("Rendering/RunBeamCone");
            if (beamShader != null)
            {
                beamMaterial = new Material(beamShader) { name = "Run B / beam cone", renderQueue = 3000 };
                beamMaterial.SetColor("_Color", Layout.floodColor);
                beamMaterial.SetFloat("_Density", 0f);
                beamMaterial.SetFloat("_DiffuseCoefficient", 1f);
                beamMaterial.SetFloat("_Length", Layout.beamLength);
                beamMaterial.SetFloat("_SoftDepth", .35f);
                beamMaterial.SetFloat("_Gain", Layout.beamGain);
            }
            var haloShader = Resources.Load<Shader>("Rendering/RunSoffitHalo");
            if (haloShader != null)
            {
                haloMaterial = new Material(haloShader) { name = "Run B / soffit halo" };
                haloMaterial.SetColor("_Color", Layout.haloColor);
                haloMaterial.SetFloat("_Density", 1f);
                haloMaterial.SetFloat("_Aspect", 2.2f);
            }
        }
        var unitRenderers = new System.Collections.Generic.List<Renderer>();
        BuildUnits(unitRenderers);
        if (Host == RunRigHost.OwnLamps) BuildOwnLamps();
        if (!WebGLTier) BuildProbes();
        var post = FrontRoomsPostStack.EnsureZoneVolume(transform, "Run_B", new Bounds(Layout.postCentre, Layout.postSize), Layout.postBlend);
        if (post != null) post.priority = 2f;
        CollectAmbient(unitRenderers, hostRenderers, dressRoot);
        CollectCullLights();
        // After the cull list: the goal window light is never culled at 16 m (C42; F1's shape for the lite).
        if (!WebGLTier && Layout.goalWindowLight) BuildGoalLight();
        reducedMix = FrontRoomsSettings.ReduceFlashing ? 1f : 0f;
        State = RunRigState.Armed;
        Clock = -1f;
        Evaluate(-1f);
        ApplyAmbient(true);
    }

    static GameObject Kit(string asset, Transform parent, Vector3 position, Quaternion rotation, string label)
    {
        var go = FrontRoomsKitLibrary.Spawn(asset, parent, position, rotation, null, false, label, false);
        if (go == null)
        {
            go = new GameObject(label + " (missing " + asset + ")");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
        }
        return go;
    }

    static Vector3 Anchor(string asset, string anchor, Vector3 fallback)
    {
        var info = FrontRoomsKitLibrary.GetInfo(asset);
        return info != null && info.TryAnchor(anchor, out var p) ? p : fallback;
    }

    /// <summary>Every renderer of a kit instance and the index of the material whose name starts with <paramref name="slot"/>.</summary>
    static void SlotRenderers(GameObject root, string slot, out Renderer[] renderers, out int[] indices)
    {
        var all = root.GetComponentsInChildren<Renderer>(true);
        var count = 0;
        for (var i = 0; i < all.Length; i++) if (SlotIndex(all[i], slot) >= 0) count++;
        renderers = new Renderer[count];
        indices = new int[count];
        var k = 0;
        for (var i = 0; i < all.Length; i++)
        {
            var index = SlotIndex(all[i], slot);
            if (index < 0) continue;
            renderers[k] = all[i];
            indices[k] = index;
            k++;
        }
    }

    static int SlotIndex(Renderer renderer, string slot)
    {
        var materials = renderer.sharedMaterials;
        for (var i = 0; i < materials.Length; i++)
            if (materials[i] != null && materials[i].name.StartsWith(slot, StringComparison.Ordinal)) return i;
        return -1;
    }

    void BuildUnits(System.Collections.Generic.List<Renderer> unitRenderers)
    {
        var list = new System.Collections.Generic.List<Head>();
        var pilotList = new System.Collections.Generic.List<Emissive>();
        var cookie = WebGLTier ? null : Resources.Load<Texture2D>("Surfaces/Textures/Cookie_SealedBeam_S");
        var cookieCore = WebGLTier ? null : Resources.Load<Texture2D>("Surfaces/Textures/Cookie_SealedBeamCore_S");
        var mountL = Anchor("Kit_EmergencyTwinHead", "mount_l", new Vector3(.112f, .201f, .059f));
        var mountR = Anchor("Kit_EmergencyTwinHead", "mount_r", new Vector3(-.112f, .201f, .059f));
        var feed = Anchor("Kit_EmergencyTwinHead", "feed", new Vector3(0f, .1965f, .030f));
        var trunnion = Anchor("Kit_EmergencyTwinHead_Yoke", "trunnion", new Vector3(0f, .085f, 0f));
        var lensAnchor = Anchor("Kit_EmergencyTwinHead_Lamp", "lens", new Vector3(0f, 0f, .049f));
        const float cabinetHalfHeight = .0955f;
        for (var u = 0; u < Layout.units.Length; u++)
        {
            var def = Layout.units[u];
            var unitRoot = new GameObject("Run B / twin-head unit " + u + (def.side > 0 ? " R" : " L") + " z " + def.z.ToString("0.0")).transform;
            unitRoot.SetParent(transform, false);
            var yaw = def.side > 0 ? -90f : 90f;
            unitRoot.localPosition = new Vector3(def.side * Layout.halfWidth, Layout.unitBoxCentreY - cabinetHalfHeight, def.z);
            unitRoot.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var cabinet = Kit("Kit_EmergencyTwinHead", unitRoot, Vector3.zero, Quaternion.identity, "cabinet");
            SlotRenderers(cabinet, "Run_NeonPilot", out var pr, out var pi);
            for (var i = 0; i < pr.Length; i++) pilotList.Add(new Emissive { renderer = pr[i], index = pi[i] });
            // Conduit feed from the knockout up to the soffit (a 1 m EMT tile scaled to the run, 18 mm off the wall).
            var feedTop = Layout.height - (unitRoot.localPosition.y + feed.y);
            if (feedTop > .02f)
                FrontRoomsKitLibrary.Spawn("Kit_ConduitEMT", unitRoot, new Vector3(feed.x, feed.y, feed.z - .0115f), Quaternion.identity, new Vector3(1f, feedTop, 1f), false, "feed EMT", false);
            for (var m = 0; m < 2; m++)
            {
                var mount = m == 0 ? mountL : mountR;
                var yoke = Kit("Kit_EmergencyTwinHead_Yoke", unitRoot, mount, Quaternion.identity, "yoke " + m);
                var lamp = Kit("Kit_EmergencyTwinHead_Lamp", yoke.transform, trunnion, Quaternion.identity, "lamp head " + m);
                var pivot = transform.InverseTransformPoint(lamp.transform.position);
                var plus = pivot.z > def.z;
                var goal = plus && def.goalPlus;
                var target = AimTarget(def, pivot, plus, goal);
                var dir = transform.TransformDirection(target - pivot).normalized;
                var flat = Vector3.ProjectOnPlane(dir, transform.up);
                if (flat.sqrMagnitude > 1e-6f) yoke.transform.rotation = Quaternion.LookRotation(flat.normalized, transform.up);
                lamp.transform.rotation = Quaternion.LookRotation(dir, transform.up);
                var head = new Head { z = def.z, goal = goal, unit = u };
                SlotRenderers(lamp, "Run_SealedBeamLens", out head.lens, out head.lensIndex);
                head.world = lamp.transform.position;
                var makeLight = !WebGLTier || m == 0;
                if (makeLight)
                {
                    var lightGo = new GameObject(goal ? "head light (goal spot)" : "head light");
                    lightGo.transform.SetParent(lamp.transform, false);
                    lightGo.transform.localPosition = lensAnchor + new Vector3(0f, 0f, .017f);
                    var light = lightGo.AddComponent<Light>();
                    light.type = LightType.Spot;
                    light.color = Layout.floodColor;
                    light.shadows = LightShadows.Soft;
                    light.shadowStrength = Layout.floodShadowStrength;
                    light.shadowNearPlane = .05f;
                    light.renderMode = LightRenderMode.ForcePixel;
                    if (WebGLTier)
                    {
                        // One wide spot per unit, aimed between its two heads' targets, no cookie.
                        var other = AimTarget(def, pivot, !plus, !plus && def.goalPlus);
                        var mid = (target + other) * .5f;
                        lightGo.transform.rotation = Quaternion.LookRotation(transform.TransformDirection(mid - pivot).normalized, transform.up);
                        light.spotAngle = 80f;
                        light.innerSpotAngle = 60f;
                        light.range = Mathf.Max(Layout.floodRange, Layout.goalRange);
                        head.baseIntensity = Layout.floodIntensity;
                    }
                    else
                    {
                        light.spotAngle = goal ? Layout.goalOuter : Layout.floodOuter;
                        light.innerSpotAngle = goal ? Layout.goalInner : Layout.floodInner;
                        light.range = goal ? Layout.goalRange : Layout.floodRange;
                        light.cookie = goal ? cookieCore : cookie;
                        head.baseIntensity = Layout.floodIntensity * (goal ? Layout.goalHeadGain : 1f);
                    }
                    light.intensity = 0f;
                    light.enabled = false;
                    head.light = light;
                    head.hasLight = true;
                }
                if (!WebGLTier && beamMaterial != null)
                {
                    var beamGo = new GameObject("beam cone");
                    beamGo.transform.SetParent(lamp.transform, false);
                    beamGo.transform.localPosition = lensAnchor + new Vector3(0f, 0f, .001f);
                    beamGo.AddComponent<MeshFilter>().sharedMesh = goal ? GoalCone() : FloodCone();
                    var beam = beamGo.AddComponent<MeshRenderer>();
                    beam.sharedMaterial = beamMaterial;
                    beam.shadowCastingMode = ShadowCastingMode.Off;
                    beam.receiveShadows = false;
                    beam.lightProbeUsage = LightProbeUsage.Off;
                    beam.reflectionProbeUsage = ReflectionProbeUsage.Off;
                    beam.enabled = false;
                    head.beam = beam;
                }
                maxHeadZ = Mathf.Max(maxHeadZ, def.z);
                list.Add(head);
            }
            foreach (var r in unitRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (r is MeshRenderer && r.sharedMaterial == beamMaterial) continue;
                r.shadowCastingMode = ShadowCastingMode.Off;
                unitRenderers.Add(r);
            }
        }
        heads = list.ToArray();
        pilots = pilotList.ToArray();
    }

    Vector3 AimTarget(RunUnitDef def, Vector3 pivot, bool plus, bool goal)
    {
        if (goal)
        {
            if (!def.autoAim && def.aimPlus != Vector3.zero) return def.aimPlus;
            var run = new Vector2(Layout.goalPoint.x - pivot.x, Layout.goalPoint.z - pivot.z).magnitude;
            return new Vector3(Layout.goalPoint.x, pivot.y - Mathf.Tan(Layout.goalPitch * Mathf.Deg2Rad) * run, Layout.goalPoint.z);
        }
        if (!def.autoAim) return plus ? def.aimPlus : def.aimMinus;
        // r8: pitch 30 deg, aimed at the floor across the axis.
        var dh = pivot.y / Mathf.Tan(Layout.floodPitch * Mathf.Deg2Rad);
        var tx = -def.side * Layout.aimCross;
        var along = Mathf.Sqrt(Mathf.Max(.01f, dh * dh - (tx - pivot.x) * (tx - pivot.x)));
        return new Vector3(tx, 0f, pivot.z + (plus ? along : -along));
    }

    static Mesh FloodCone() => floodCone != null ? floodCone : (floodCone = ConeMesh(2.5f, .045f, Mathf.Tan(14f * Mathf.Deg2Rad), 32));
    static Mesh GoalCone() => goalCone != null ? goalCone : (goalCone = ConeMesh(2.5f, .045f, Mathf.Tan(14f * Mathf.Deg2Rad), 32));

    static Mesh ConeMesh(float length, float r0, float slope, int seg)
    {
        const int rings = 6;
        var verts = new Vector3[(rings + 1) * (seg + 1)];
        var normals = new Vector3[verts.Length];
        var uvs = new Vector2[verts.Length];
        var tris = new int[rings * seg * 6];
        var k = 0;
        for (var j = 0; j <= rings; j++)
        {
            var s = j / (float)rings;
            var zc = s * length;
            var r = r0 + slope * zc;
            for (var i = 0; i <= seg; i++)
            {
                var a = i / (float)seg * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                verts[k] = d * r + Vector3.forward * zc;
                normals[k] = (d - Vector3.forward * slope).normalized;
                uvs[k] = new Vector2(i / (float)seg, s);
                k++;
            }
        }
        var t = 0;
        for (var j = 0; j < rings; j++)
            for (var i = 0; i < seg; i++)
            {
                var a = j * (seg + 1) + i;
                var b = a + seg + 1;
                tris[t++] = a; tris[t++] = b; tris[t++] = a + 1;
                tris[t++] = a + 1; tris[t++] = b; tris[t++] = b + 1;
            }
        var mesh = new Mesh { name = "Run B beam cone" };
        mesh.vertices = verts;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh HaloQuad()
    {
        if (haloQuad != null) return haloQuad;
        // 2.6 m along the fixture (local Z) x 1.2 m across (local X), facing down; uv.x along the fixture.
        haloQuad = new Mesh { name = "Run B soffit halo" };
        haloQuad.vertices = new[] { new Vector3(-.6f, 0f, -1.3f), new Vector3(.6f, 0f, -1.3f), new Vector3(.6f, 0f, 1.3f), new Vector3(-.6f, 0f, 1.3f) };
        haloQuad.normals = new[] { Vector3.down, Vector3.down, Vector3.down, Vector3.down };
        haloQuad.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
        haloQuad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        haloQuad.RecalculateBounds();
        return haloQuad;
    }

    /// <summary>A soffit halo quad under <paramref name="parent"/> (the wrap's frame, long axis along local Z). Desktop only.</summary>
    public static Renderer BuildHalo(Transform parent, Vector3 localPosition, Material material)
    {
        if (WebGLTier || material == null) return null;
        var go = new GameObject("run b soffit halo");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.AddComponent<MeshFilter>().sharedMesh = HaloQuad();
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.Off;
        r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return r;
    }

    /// <summary>A soffit-halo material for host lamps (the stream's wraps), or null in the WebGL tier.</summary>
    public static Material CreateHaloMaterial(Color peak)
    {
        if (WebGLTier) return null;
        var shader = Resources.Load<Shader>("Rendering/RunSoffitHalo");
        if (shader == null) return null;
        var m = new Material(shader) { name = "Run B / soffit halo (host)" };
        m.SetColor("_Color", peak);
        m.SetFloat("_Density", 1f);
        m.SetFloat("_Aspect", 2.2f);
        return m;
    }

    /// <summary>C42: the goal window light, past the goal wall's far face, aimed away from the door and down at the floor and
    /// wall the F5 lite looks onto. Unshadowed (one Light), outside the cull list and the trip.</summary>
    void BuildGoalLight()
    {
        var n = Layout.goalNormal.sqrMagnitude > 0f ? Layout.goalNormal.normalized : Vector3.back;
        var away = -n;
        var p = new Vector3(Layout.goalPoint.x, Layout.goalLightHeight, Layout.goalPoint.z) + away * (Layout.WallHalf + Layout.goalLightBehind);
        var go = new GameObject("run b goal window light (C42)");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = p;
        var aim = Quaternion.AngleAxis(Layout.goalLightPitch, Vector3.Cross(Vector3.up, away).normalized) * away;
        go.transform.localRotation = Quaternion.LookRotation(aim, Vector3.up);
        var l = go.AddComponent<Light>();
        l.type = LightType.Spot;
        l.spotAngle = Layout.goalLightOuter;
        l.innerSpotAngle = Layout.goalLightInner;
        l.range = Layout.goalLightRange;
        l.color = Layout.goalLightColor;
        l.intensity = Layout.goalLightIntensity;
        l.shadows = LightShadows.None;
        l.renderMode = LightRenderMode.ForcePixel;
        goalLight = l;
        UpdateGoalLight();
    }

    /// <summary>C42: the goal window light's weight: off past the goal wall (the viewer is in the far room) and beyond
    /// goalLightMaxDistance; else 1 - the far room's lamp (FarRoomLamp, or the estimate), or a fade near the door when the far
    /// room has no lamp. Viewer null (edit mode, tests): full.</summary>
    void UpdateGoalLight()
    {
        if (goalLight == null) return;
        var w = 1f;
        if (viewer != null)
        {
            var n = Layout.goalNormal.sqrMagnitude > 0f ? Layout.goalNormal.normalized : Vector3.back;
            var local = transform.InverseTransformPoint(viewer.position);
            var toViewer = local - Layout.goalPoint;
            var flat = new Vector2(toViewer.x, toViewer.z).magnitude;
            if (Vector3.Dot(toViewer, n) < Layout.WallHalf || flat > Layout.goalLightMaxDistance) w = 0f;
            else
            {
                var far = FarRoomLamp != null ? FarRoomLamp() : EstimateFarLamp(local);
                if (far >= 0f) w = 1f - Mathf.Clamp01(far);
                else w = Mathf.Clamp01((flat - Layout.goalLightNearEnd) / Mathf.Max(.01f, Layout.goalLightNearStart - Layout.goalLightNearEnd));
            }
        }
        GoalLightWeight = w;
        var on = w > .001f;
        goalLight.intensity = Layout.goalLightIntensity * w;
        if (goalLight.enabled != on) goalLight.enabled = on;
    }

    /// <summary>The far room's lamp as the map's tick would light it, from the viewer's rig-space position: a lamp at
    /// farLampDistance past the goal wall's centre line, on within farLampRadius, faded over its last farLampFade (XZ).</summary>
    float EstimateFarLamp(Vector3 viewerLocal)
    {
        var n = Layout.goalNormal.sqrMagnitude > 0f ? Layout.goalNormal.normalized : Vector3.back;
        var lamp = Layout.goalPoint - n * Layout.farLampDistance;
        var d = new Vector2(viewerLocal.x - lamp.x, viewerLocal.z - lamp.z).magnitude;
        return Mathf.Clamp01((Layout.farLampRadius - d) / Mathf.Max(.01f, Layout.farLampFade));
    }

    void BuildOwnLamps()
    {
        wraps = new Wrap[Layout.ownLamps.Length];
        for (var k = 0; k < wraps.Length; k++)
        {
            var p = Layout.ownLamps[k];
            var frame = new GameObject("run b wraparound " + k).transform;
            frame.SetParent(transform, false);
            frame.localPosition = p;
            // The kit's long axis is X; turn it to run along the corridor (Z).
            var kit = Kit("Kit_WrapFixture_4ft", frame, Vector3.zero, Quaternion.Euler(0f, 90f, 0f), "wrap fixture");
            foreach (var r in kit.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
            var w = new Wrap { z = p.z, level = k == Layout.stutterLamp ? Layout.stutterLevel : 1f };
            SlotRenderers(kit, "Run_WrapLens", out w.lens, out w.lensIndex);
            var lightGo = new GameObject("run b wrap light " + k);
            lightGo.transform.SetParent(frame, false);
            lightGo.transform.localPosition = new Vector3(0f, -.092f, 0f);
            lightGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Spot;
            l.spotAngle = Layout.wrapOuter;
            l.innerSpotAngle = Layout.wrapInner;
            l.color = Layout.wrapColor;
            l.range = Layout.wrapRange;
            var shadowed = Array.IndexOf(Layout.ownLampShadows, k) >= 0;
            l.shadows = shadowed && (!WebGLTier || k == Layout.ownLampShadows[0]) ? LightShadows.Soft : LightShadows.None;
            l.shadowStrength = .92f;
            l.shadowNearPlane = .1f;
            w.light = l;
            w.halo = BuildHalo(frame, new Vector3(0f, -.003f, 0f), haloMaterial);   // 3 mm under the soffit: under the map skin (2 mm) too
            wraps[k] = w;
        }
        // CR-4: every wrap dips within +-warnJitter of the others (the map's per-lamp cell hash), from its index.
        wrapJitter = new float[wraps.Length];
        for (var k = 0; k < wraps.Length; k++)
        {
            var h = (uint)(k + 1) * 2654435761u;
            h ^= h >> 15;
            wrapJitter[k] = ((h & 0xffff) / 65535f * 2f - 1f) * Layout.warnJitter;
        }
    }

    /// <summary>Per-state box probes. Dropped 2026-10-31 without a measurement (Red: no testing until everything
    /// lands; T8 never showed a visible gain): the RunB_* cubes, the Surface.shader / RenderSetup pragmas and the URP
    /// flags are not shipped, so Resources.Load finds no cube and the rig builds no probe (ProbeCount 0). The code is
    /// kept for the combined test (run_b_production/60_production_66.md).</summary>
    void BuildProbes()
    {
        if (Layout.probes == null || Layout.probes.Length == 0) return;
        var na = 0; var nt = 0;
        var armed = new ReflectionProbe[Layout.probes.Length];
        var tripped = new ReflectionProbe[Layout.probes.Length];
        for (var i = 0; i < Layout.probes.Length; i++)
        {
            var a = Resources.Load<Texture>("Rendering/Reflections/RunB_" + Layout.name + "_armed_" + i);
            var t = Resources.Load<Texture>("Rendering/Reflections/RunB_" + Layout.name + "_tripped_" + i);
            if (a != null) armed[na++] = Probe("run b probe armed " + i, Layout.probes[i], a);
            if (t != null) tripped[nt++] = Probe("run b probe tripped " + i, Layout.probes[i], t);
        }
        Array.Resize(ref armed, na);
        Array.Resize(ref tripped, nt);
        armedProbes = armed;
        trippedProbes = tripped;
    }

    ReflectionProbe Probe(string label, RunProbeBox box, Texture cube)
    {
        var go = new GameObject(label);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = box.centre;
        var p = go.AddComponent<ReflectionProbe>();
        p.mode = ReflectionProbeMode.Custom;
        p.customBakedTexture = cube;
        // The box overhangs the shell by 0.16 m (VL195): a box on the shell weighs 0 on its own faces.
        p.size = box.size;
        p.center = Vector3.zero;
        p.boxProjection = true;
        p.blendDistance = Layout.probeBlend;
        p.importance = Layout.probeImportance;
        p.intensity = 1f;
        p.enabled = false;
        return p;
    }

    void CollectAmbient(System.Collections.Generic.List<Renderer> unitRenderers, Renderer[] hostRenderers, Transform dressRoot)
    {
        var list = new System.Collections.Generic.List<AmbientEntry>();
        var seen = new System.Collections.Generic.HashSet<Renderer>();
        void Add(Renderer r, byte kind, bool host)
        {
            if (r == null || !seen.Add(r)) return;
            var m = r.sharedMaterial;
            if (m != null && (m == beamMaterial || m == haloMaterial)) return;
            list.Add(new AmbientEntry { renderer = r, materials = r.sharedMaterials.Length, kind = kind, host = host, original = r.lightProbeUsage });
        }
        foreach (var r in unitRenderers) Add(r, 2, false);
        var high = Layout.highBand;
        foreach (var r in GetComponentsInChildren<Renderer>(true)) Add(r, Bottom(r) > high ? (byte)1 : (byte)0, false);
        if (dressRoot != null)
            foreach (var r in dressRoot.GetComponentsInChildren<Renderer>(true)) Add(r, Bottom(r) > high ? (byte)1 : (byte)0, false);
        if (hostRenderers != null)
            foreach (var r in hostRenderers) Add(r, Bottom(r) > high ? (byte)1 : (byte)0, true);
        ambient = list.ToArray();
        for (var i = 0; i < ambient.Length; i++)
            if (!ambient[i].host) ambient[i].renderer.lightProbeUsage = LightProbeUsage.CustomProvided;
        hostAcquired = false;
    }

    float Bottom(Renderer r) => transform.InverseTransformPoint(r.bounds.min).y;

    void CollectCullLights()
    {
        var all = GetComponentsInChildren<Light>(true);
        cullLights = all;
    }

    // ---------------------------------------------------------------- host coupling
    /// <summary>The host's shell takes the rig's ambient (the stream calls it while its room shows Run B).</summary>
    public void AcquireHost()
    {
        if (hostAcquired || ambient == null) return;
        hostAcquired = true;
        for (var i = 0; i < ambient.Length; i++)
            if (ambient[i].host && ambient[i].renderer != null) ambient[i].renderer.lightProbeUsage = LightProbeUsage.CustomProvided;
        ApplyAmbient(true);
    }

    /// <summary>Give the host's shell back: its light-probe usage restored, its property blocks cleared.</summary>
    public void ReleaseHost()
    {
        if (!hostAcquired || ambient == null) return;
        hostAcquired = false;
        for (var i = 0; i < ambient.Length; i++)
        {
            if (!ambient[i].host || ambient[i].renderer == null) continue;
            var r = ambient[i].renderer;
            r.lightProbeUsage = ambient[i].original;
            var n = ambient[i].materials;
            for (var k = 0; k < n; k++) r.SetPropertyBlock(null, k);
            r.SetPropertyBlock(null);
        }
    }

    /// <summary>The camera (or player) the rig culls its lights against.</summary>
    public void SetViewer(Transform t) => viewer = t;

    // ---------------------------------------------------------------- states
    public void Arm()
    {
        if (State == RunRigState.Armed) return;
        ResetImmediate();
    }

    /// <summary>Trip the room (once per instance: ignored unless Armed).</summary>
    public void Trip()
    {
        if (State != RunRigState.Armed) return;
        reducedMix = FrontRoomsSettings.ReduceFlashing ? 1f : 0f;
        Clock = 0f;
        burstClock = -1f;
        for (var i = 0; i < heads.Length; i++) heads[i].relayFired = false;
        SetState(RunRigState.Tripping);
        MainsDropped?.Invoke(transform.TransformPoint(Layout.postCentre));
        Evaluate(0f);
        ApplyAmbient(true);
    }

    /// <summary>The animated reset (map only, RoomReset): heads out, wraps re-strike in a sweep from the entrance.
    /// (Not named Reset: that is a MonoBehaviour editor message.)</summary>
    public void StartReset()
    {
        if (State == RunRigState.Armed || State == RunRigState.Resetting) return;
        reducedMix = FrontRoomsSettings.ReduceFlashing ? 1f : 0f;
        Clock = 0f;
        SetState(RunRigState.Resetting);
        ResetStarted?.Invoke(transform.TransformPoint(Layout.postCentre));
        EvaluateReset(0f);
        ApplyAmbient(true);
    }

    /// <summary>Back to Armed at once (the stream calls it while the slot is sealed, out of view).</summary>
    public void ResetImmediate()
    {
        var was = State;
        State = RunRigState.Armed;
        Clock = -1f;
        warn = 1f;
        burstClock = -1f;
        Evaluate(-1f);
        ApplyAmbient(true);
        if (was != RunRigState.Armed) StateChanged?.Invoke(State, transform.TransformPoint(Layout.postCentre));
    }

    /// <summary>Straight to Tripped with no events and no transition (a room rebuilt while its alarm stands: RunRoomBuilt
    /// after a drop, the map's RunRoomState.Tripped). Ignored unless Armed.</summary>
    public void TripImmediate()
    {
        if (State != RunRigState.Armed) return;
        reducedMix = FrontRoomsSettings.ReduceFlashing ? 1f : 0f;
        burstClock = -1f;
        for (var i = 0; i < heads.Length; i++) heads[i].relayFired = true;
        State = RunRigState.Tripped;
        Clock = TripEnd;
        Evaluate(Clock);
        ApplyAmbient(true);
    }

    /// <summary>Stage-1 warning on the rig's own lamps (CR-4): a multiplier on the armed wraps. Host lamps ignore it.</summary>
    public void ApplyWarn(float multiplier)
    {
        warn = Mathf.Clamp01(multiplier);
        if (State == RunRigState.Armed && Host == RunRigHost.OwnLamps) EvaluateWraps(-1f, 1f);
    }

    /// <summary>
    /// CR-4 (the map's RunRoomWarned): one warning burst on the armed wraps, the map's envelope (FrontRoomsWarnLamps M3):
    /// stage 1 two or three dips, stage 2 two, warnDipSpacing apart; the first to 0.55-0.62 of the level, the later ones to
    /// 0.65-0.75; attack 0.05 s, hold 0.04-0.08 s, release 0.30 s with an exponential tail; each wrap within +-40 ms.
    /// REDUCE FLASHING: one sag to 0.7 over 0.6 s and back. Deterministic (a hash of the burst count), allocation-free,
    /// ignored unless Armed with own lamps, and never on top of a burst still playing.
    /// </summary>
    public void Warn(int stage)
    {
        if (stage <= 0 || State != RunRigState.Armed || Host != RunRigHost.OwnLamps || wraps == null || burstClock >= 0f) return;
        burstCount++;
        burstReduced = FrontRoomsSettings.ReduceFlashing;
        burstDips = burstReduced ? 1 : stage == 1 ? (Hash(burstCount, 1) % 2 == 0 ? 2 : 3) : 2;
        for (var d = 0; d < burstDips; d++)
        {
            var u = (Hash(burstCount, 10 + d) & 0xffff) / 65535f;
            var v = (Hash(burstCount, 20 + d) & 0xffff) / 65535f;
            dipDepth[d] = d == 0 ? Mathf.Lerp(Layout.warnFirstDepthMin, Layout.warnFirstDepthMax, u) : Mathf.Lerp(Layout.warnLaterDepthMin, Layout.warnLaterDepthMax, u);
            dipHold[d] = Mathf.Lerp(Layout.warnHoldMin, Layout.warnHoldMax, v);
        }
        burstEnd = burstReduced ? Layout.warnReducedSeconds * 2f + Layout.warnJitter
            : (burstDips - 1) * Layout.warnDipSpacing + Layout.warnAttack + Layout.warnHoldMax + Layout.warnRelease + Layout.warnJitter * 2f + .05f;
        burstClock = 0f;
    }

    /// <summary>True while a CR-4 burst plays.</summary>
    public bool Bursting => burstClock >= 0f;

    static uint Hash(int a, int b)
    {
        var h = (uint)a * 2654435761u ^ (uint)b * 2246822519u;
        h ^= h >> 13; h *= 3266489917u; h ^= h >> 16;
        return h;
    }

    /// <summary>The burst's multiplier at burst time t for one wrap (1 outside a burst).</summary>
    float BurstLevel(float t)
    {
        if (burstClock < 0f || t < 0f) return 1f;
        if (burstReduced)
        {
            var half = Layout.warnReducedSeconds;
            var u = t < half ? t / half : Mathf.Clamp01(2f - t / half);
            return Mathf.Lerp(1f, Layout.warnReducedDepth, u);
        }
        var level = 1f;
        for (var d = 0; d < burstDips; d++)
        {
            var s = t - d * Layout.warnDipSpacing;
            if (s < 0f) continue;
            float g;
            if (s < Layout.warnAttack) g = s / Layout.warnAttack;
            else if (s < Layout.warnAttack + dipHold[d]) g = 1f;
            else
            {
                var r = s - Layout.warnAttack - dipHold[d];
                if (r >= Layout.warnRelease) continue;
                var tail = Mathf.Max(.01f, Layout.warnTail);
                g = 1f - (1f - Mathf.Exp(-r / tail)) / (1f - Mathf.Exp(-Layout.warnRelease / tail));
            }
            level = Mathf.Min(level, Mathf.Lerp(1f, dipDepth[d], Mathf.Clamp01(g)));
        }
        return level;
    }

    void SetState(RunRigState s)
    {
        if (State == s) return;
        State = s;
        StateChanged?.Invoke(s, transform.TransformPoint(Layout.postCentre));
    }

    /// <summary>Advance the rig. No allocation.</summary>
    public void Tick(float dt)
    {
        dt = Mathf.Clamp(dt, 0f, .1f);
        MainsDirty = false;
        switch (State)
        {
            case RunRigState.Tripping:
            {
                Clock += dt;
                FireRelays(Clock);
                Evaluate(Clock);
                ApplyAmbient(false);
                if (Clock >= TripEnd) SetState(RunRigState.Tripped);
                break;
            }
            case RunRigState.Resetting:
            {
                Clock += dt;
                EvaluateReset(Clock);
                ApplyAmbient(false);
                if (Clock >= ResetEnd) ResetImmediate();
                break;
            }
            case RunRigState.Armed:
            {
                var stutter = Host == RunRigHost.OwnLamps && Layout.stutterLamp >= 0 && wraps != null && Layout.stutterLamp < wraps.Length;
                if (stutter) stutterClock += dt;
                var bursting = burstClock >= 0f;
                if (bursting)
                {
                    burstClock += dt;
                    if (burstClock >= burstEnd) burstClock = -1f;
                }
                if (stutter || bursting)
                {
                    EvaluateWraps(-1f, 1f);
                    // the lenses dip with their light (the map's lens follows its lamp, _FR_LampDip): only the emissive
                    // renderers are rewritten (ApplyAmbient(false) skips the rest when the SH did not change)
                    if (emissionDirty) ApplyAmbient(false);
                }
                break;
            }
        }
        var m = MainsAt(0f);
        if (!Mathf.Approximately(m, lastMains) || State == RunRigState.Resetting) { MainsDirty = true; lastMains = m; }
        Cull();
        UpdateGoalLight();
    }

    void FireRelays(float t)
    {
        for (var i = 0; i < heads.Length; i++)
        {
            if (heads[i].relayFired) continue;
            if (t >= HeadStart(heads[i].z))
            {
                heads[i].relayFired = true;
                TransferRelay?.Invoke(i, heads[i].world);
            }
        }
    }

    void Cull()
    {
        if (viewer == null || cullLights == null) return;
        var limit = WebGLTier ? Layout.cullDistanceWebGL : Layout.cullDistance;
        var limit2 = limit * limit;
        var p = viewer.position;
        for (var i = 0; i < cullLights.Length; i++)
        {
            var l = cullLights[i];
            if (l == null) continue;
            var far = (l.transform.position - p).sqrMagnitude > limit2;
            if (far && l.enabled) l.enabled = false;
            else if (!far && !l.enabled && l.intensity > 0f) l.enabled = true;
        }
#if (UNITY_WEBGL && !UNITY_EDITOR) || FRONTROOMS_WEBGL_PREVIEW
        // Only the nearest head casts shadows.
        var best = -1; var bestD = float.MaxValue;
        for (var i = 0; i < heads.Length; i++)
        {
            if (!heads[i].hasLight) continue;
            var d = (heads[i].light.transform.position - p).sqrMagnitude;
            if (d < bestD) { bestD = d; best = i; }
        }
        for (var i = 0; i < heads.Length; i++)
            if (heads[i].hasLight) heads[i].light.shadows = i == best ? LightShadows.Soft : LightShadows.None;
#endif
    }

    // ---------------------------------------------------------------- timelines
    bool Reduced => reducedMix > .5f;
    public float HeadStart(float z) => Reduced ? Layout.reducedHeadDelay : Layout.headDelay + Layout.headStagger * z;
    float TripEnd => Reduced ? Layout.reducedHeadDelay + Layout.reducedEnvelope + .1f : HeadStart(maxHeadZ) + Layout.headRise + .1f;
    float ResetEnd => Layout.resetCellSeconds * Mathf.Ceil(Layout.length / Mathf.Max(.1f, Layout.resetCellLength)) + Layout.resetFlickPeriod * 2f + Layout.resetRise + .2f;

    /// <summary>Armed wrap (mains) level at trip time t (t &lt; 0 armed).</summary>
    public float WrapLevel(float t)
    {
        if (t < 0f) return 1f;
        if (Reduced) return Mathf.Clamp01(1f - t / Layout.reducedEnvelope);
        return Mathf.Clamp01(1f - t / Layout.mainsDrop);
    }

    /// <summary>A head's filament level at trip time t (0 before its relay, ease-in rise).</summary>
    public float HeadLevel(float t, float z)
    {
        if (t < 0f) return 0f;
        if (Reduced)
        {
            var r = Mathf.Clamp01((t - Layout.reducedHeadDelay) / Layout.reducedEnvelope);
            return r * r * (3f - 2f * r);
        }
        var f = Mathf.Clamp01((t - HeadStart(z)) / Layout.headRise);
        return f * f;
    }

    public float MeanHeadLevel(float t)
    {
        if (heads == null || heads.Length == 0) return t < 0f ? 0f : 1f;
        var sum = 0f;
        for (var i = 0; i < heads.Length; i++) sum += HeadLevel(t, heads[i].z);
        return sum / heads.Length;
    }

    /// <summary>Lens glow of a dead wrap: 0.03 phosphor afterglow decaying with tau 0.5 s after the drop.</summary>
    public float LensLevel(float t)
    {
        var w = WrapLevel(t);
        if (t < 0f) return 1f;
        var end = Reduced ? Layout.reducedEnvelope : Layout.mainsDrop;
        var glow = t <= end ? Layout.afterglow : Layout.afterglow * Mathf.Exp(-(t - end) / Mathf.Max(.01f, Layout.afterglowTau));
        return Mathf.Lerp(glow, 1f, w);
    }

    /// <summary>Re-strike level of a wrap at rig-space z, tr seconds into a reset (two flicks at least 0.34 s apart,
    /// then a rise from 0.35; REDUCE FLASHING: a 0.5 s rise, no flicks).</summary>
    public float RestrikeLevel(float tr, float z)
    {
        var cell = Mathf.Floor(Mathf.Max(0f, z) / Mathf.Max(.1f, Layout.resetCellLength));
        var s = tr - cell * Layout.resetCellSeconds;
        if (s < 0f) return 0f;
        if (Reduced) { var r = Mathf.Clamp01(s / Layout.reducedEnvelope); return r * r * (3f - 2f * r); }
        var p = Layout.resetFlickPeriod;
        if (s < p) return s < Layout.resetFlickOn ? .9f : 0f;
        if (s < 2f * p) return s - p < Layout.resetFlickOn ? .9f : 0f;
        var rise = Mathf.Clamp01((s - 2f * p) / Layout.resetRise);
        return Mathf.Lerp(.35f, 1f, rise * rise * (3f - 2f * rise));
    }

    /// <summary>The mains level a host lamp at rig-space z should multiply its output by.</summary>
    public float MainsAt(float z)
    {
        switch (State)
        {
            case RunRigState.Tripping:
            case RunRigState.Tripped: return WrapLevel(Mathf.Max(0f, Clock));
            case RunRigState.Resetting: return RestrikeLevel(Clock, z);
            default: return 1f;
        }
    }

    /// <summary>The lens emission multiplier for a host lamp (afterglow after the drop).</summary>
    public float LensAt(float z)
    {
        switch (State)
        {
            case RunRigState.Tripping:
            case RunRigState.Tripped: return LensLevel(Mathf.Max(0f, Clock));
            case RunRigState.Resetting: return Mathf.Max(.0f, RestrikeLevel(Clock, z));
            default: return 1f;
        }
    }

    /// <summary>Deterministic state at trip time t (t &lt; 0 = armed). Look-dev and tests call it directly.</summary>
    public void Evaluate(float t)
    {
        if (heads == null) return;
        var w = WrapLevel(t);
        EvaluateWraps(t, w);
        for (var i = 0; i < heads.Length; i++)
        {
            var h = heads[i];
            var f = HeadLevel(t, h.z);
            SetHead(h, f);
        }
        var pilot = t < 0f ? 1f : Reduced ? Mathf.Clamp01(1f - t / Layout.reducedEnvelope) : 0f;
        SetPilots(pilot);
        SetProbes(t < Layout.mainsDrop ? w : 0f, t < Layout.mainsDrop ? 0f : MeanHeadLevel(t));
    }

    void EvaluateReset(float tr)
    {
        var headsOut = Reduced ? Mathf.Clamp01(1f - tr / Layout.reducedEnvelope) : 0f;
        for (var i = 0; i < heads.Length; i++) SetHead(heads[i], headsOut);
        if (wraps != null)
            for (var k = 0; k < wraps.Length; k++)
                SetWrap(wraps[k], RestrikeLevel(tr, wraps[k].z));
        SetPilots(Reduced ? Mathf.Clamp01(tr / Layout.reducedEnvelope) : 1f);
        var mean = 0f;
        if (wraps != null && wraps.Length > 0) { for (var k = 0; k < wraps.Length; k++) mean += RestrikeLevel(tr, wraps[k].z); mean /= wraps.Length; }
        else mean = RestrikeLevel(tr, Layout.length * .5f);
        SetProbes(mean, headsOut);
    }

    void EvaluateWraps(float t, float w)
    {
        if (wraps == null) return;
        var lens = LensLevel(t);
        for (var k = 0; k < wraps.Length; k++)
        {
            var wr = wraps[k];
            var level = wr.level;
            if (k == Layout.stutterLamp && t < 0f) level = StutterLevel();
            var wm = t < 0f ? warn * BurstLevel(burstClock - (wrapJitter != null && k < wrapJitter.Length ? wrapJitter[k] : 0f) - Layout.warnJitter) : 1f;
            level *= w * wm;
            if (wr.light != null)
            {
                wr.light.intensity = Layout.wrapIntensity * level;
                wr.light.enabled = level > .0005f;
            }
            SetLens(wr.lens, wr.lensIndex, Layout.wrapLensEmission * (wr.level * wm * lens));
            if (wr.halo != null)
            {
                wr.halo.enabled = level > .0005f;
                block.Clear();
                block.SetFloat(DensityId, level);
                wr.halo.SetPropertyBlock(block);
            }
        }
    }

    void SetWrap(Wrap wr, float level)
    {
        if (wr.light != null) { wr.light.intensity = Layout.wrapIntensity * wr.level * level; wr.light.enabled = level > .0005f; }
        SetLens(wr.lens, wr.lensIndex, Layout.wrapLensEmission * (wr.level * level));
        if (wr.halo != null) { wr.halo.enabled = level > .0005f; block.Clear(); block.SetFloat(DensityId, wr.level * level); wr.halo.SetPropertyBlock(block); }
    }

    /// <summary>The stutter tube (cell 5): the stream's Unstable model (two incommensurate waves, Perlin drop-outs at
    /// most ~2 a second), scaled round 0.78; steady 0.78 under REDUCE FLASHING.</summary>
    float StutterLevel()
    {
        if (FrontRoomsSettings.ReduceFlashing) return Layout.stutterLevel;
        var t = stutterClock + 3.1f;
        var wave = .5f + .5f * Mathf.Sin(t * 3.7f) * (.68f + .32f * Mathf.Sin(t * 1.13f));
        var dropout = Mathf.PerlinNoise(t * .92f, 7.3f) > .77f ? .38f : 1f;
        return Layout.stutterLevel * (.86f + .14f * Mathf.Clamp01(wave)) * dropout;
    }

    void SetHead(Head h, float f)
    {
        if (h.light != null)
        {
            h.light.intensity = h.baseIntensity * f;
            h.light.enabled = f > .0005f && (viewer == null || (h.light.transform.position - viewer.position).sqrMagnitude <= Sq(WebGLTier ? Layout.cullDistanceWebGL : Layout.cullDistance));
        }
        SetLens(h.lens, h.lensIndex, Layout.headLensEmission * f);
        if (h.beam != null)
        {
            h.beam.enabled = f > .0005f;
            block.Clear();
            block.SetFloat(DensityId, Layout.beamDensity * f);
            h.beam.SetPropertyBlock(block);
        }
    }

    static float Sq(float x) => x * x;

    // Emission lives in the same per-material block as the ambient SH, so lens / pilot renderers are written
    // whole: ApplyAmbient(true) after each emission change re-writes them (they are few).
    readonly System.Collections.Generic.Dictionary<long, Color> emission = new System.Collections.Generic.Dictionary<long, Color>(64);

    static long Key(Renderer r, int index) => ((long)FrontRoomsObjectId.Of(r) << 8) | (uint)(index & 0xff);

    void SetLens(Renderer[] renderers, int[] indices, Color c)
    {
        if (renderers == null) return;
        for (var i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) emission[Key(renderers[i], indices[i])] = c;
        emissionDirty = true;
    }

    void SetPilots(float level)
    {
        if (pilots == null) return;
        var c = Layout.pilotEmission * level;
        for (var i = 0; i < pilots.Length; i++)
            if (pilots[i].renderer != null) emission[Key(pilots[i].renderer, pilots[i].index)] = c;
        emissionDirty = true;
    }

    bool emissionDirty;

    void SetProbes(float armedLevel, float trippedLevel)
    {
        if (armedProbes != null)
            for (var i = 0; i < armedProbes.Length; i++)
            {
                armedProbes[i].intensity = Mathf.Max(armedLevel, 1e-4f);
                armedProbes[i].enabled = armedLevel > 0f;
            }
        if (trippedProbes != null)
            for (var i = 0; i < trippedProbes.Length; i++)
            {
                trippedProbes[i].intensity = Mathf.Max(trippedLevel, 1e-4f);
                trippedProbes[i].enabled = trippedLevel > 0f && armedLevel <= 0f;
            }
    }

    // ---------------------------------------------------------------- ambient
    /// <summary>The corridor ambient (D9 bounce model): the wraps' share scales with the mains, the heads' share rises
    /// with the heads. Written only when it (or an emission) changed.</summary>
    void ApplyAmbient(bool force)
    {
        if (ambient == null) return;
        float w, hl;
        if (State == RunRigState.Resetting)
        {
            w = MainsAt(Layout.length * .5f);
            hl = Reduced ? Mathf.Clamp01(1f - Clock / Layout.reducedEnvelope) : 0f;
        }
        else
        {
            var t = State == RunRigState.Armed ? -1f : Clock;
            w = WrapLevel(t);
            hl = MeanHeadLevel(t);
        }
        var shChanged = force || !Mathf.Approximately(w, lastAmbientW) || !Mathf.Approximately(hl, lastAmbientH);
        if (!shChanged && !emissionDirty) return;
        lastAmbientW = w;
        lastAmbientH = hl;
        emissionDirty = false;
        for (var i = 0; i < ambient.Length; i++)
        {
            var e = ambient[i];
            if (e.renderer == null) continue;
            if (e.host && !hostAcquired) continue;
            if (!shChanged && !HasEmission(e.renderer, e.materials)) continue;
            var tripped = e.kind == 1 ? highTrippedSH : e.kind == 2 ? unitTrippedSH : trippedSH;
            shScratch[0] = armedSH * w + tripped * hl;
            var n = e.materials;
            for (var k = 0; k < n; k++)
            {
                block.Clear();
                block.CopySHCoefficientArraysFrom(shScratch);
                if (emission.TryGetValue(Key(e.renderer, k), out var c)) block.SetColor(EmissionId, c);
                e.renderer.SetPropertyBlock(block, k);
            }
        }
    }

    bool HasEmission(Renderer r, int n)
    {
        for (var k = 0; k < n; k++) if (emission.ContainsKey(Key(r, k))) return true;
        return false;
    }

    /// <summary>An SH whose SampleSH is up for an up-facing normal (floor), side for a wall, down for the ceiling:
    /// three basis lobes solved against Unity's own Evaluate, then tinted (r8, D9).</summary>
    public static SphericalHarmonicsL2 BounceSH(float up, float side, float down, Color tint)
    {
        var b0 = new SphericalHarmonicsL2(); b0.AddAmbientLight(Color.white);
        var b1 = new SphericalHarmonicsL2(); b1.AddDirectionalLight(Vector3.down, Color.white, 1f);
        var b2 = new SphericalHarmonicsL2();
        b2.AddDirectionalLight(Vector3.right, Color.white, .25f);
        b2.AddDirectionalLight(Vector3.left, Color.white, .25f);
        b2.AddDirectionalLight(Vector3.forward, Color.white, .25f);
        b2.AddDirectionalLight(Vector3.back, Color.white, .25f);
        var dirs = new[] { Vector3.up, Vector3.right, Vector3.down };
        var basis = new[] { b0, b1, b2 };
        var res = new Color[3];
        var m = new float[3, 3];
        for (var j = 0; j < 3; j++)
        {
            basis[j].Evaluate(dirs, res);
            for (var i = 0; i < 3; i++) m[i, j] = res[i].r;
        }
        var x = Solve3(m, up, side, down);
        var sh = b0 * x[0] + b1 * x[1] + b2 * x[2];
        for (var k = 0; k < 9; k++) { sh[0, k] *= tint.r; sh[1, k] *= tint.g; sh[2, k] *= tint.b; }
        return sh;
    }

    static float Det3(float[,] m)
    {
        return m[0, 0] * (m[1, 1] * m[2, 2] - m[1, 2] * m[2, 1]) - m[0, 1] * (m[1, 0] * m[2, 2] - m[1, 2] * m[2, 0]) + m[0, 2] * (m[1, 0] * m[2, 1] - m[1, 1] * m[2, 0]);
    }

    static float[] Solve3(float[,] a, float b0, float b1, float b2)
    {
        var d = Det3(a);
        var b = new[] { b0, b1, b2 };
        var x = new float[3];
        for (var c = 0; c < 3; c++)
        {
            var t = (float[,])a.Clone();
            for (var r = 0; r < 3; r++) t[r, c] = b[r];
            x[c] = Mathf.Abs(d) > 1e-9f ? Det3(t) / d : 0f;
        }
        return x;
    }

    // ---------------------------------------------------------------- reporting (look-dev, tests)
    /// <summary>Every light the rig owns and whether it is on now (counts for the budgets, s.3).</summary>
    public void CountLights(out int total, out int on, out int shadowedOn)
    {
        total = on = shadowedOn = 0;
        if (cullLights == null) return;
        for (var i = 0; i <= cullLights.Length; i++)
        {
            var l = i < cullLights.Length ? cullLights[i] : goalLight;
            if (l == null) continue;
            total++;
            if (!l.enabled || l.intensity <= 0f) continue;
            on++;
            if (l.shadows != LightShadows.None) shadowedOn++;
        }
    }

    /// <summary>Level of head i at trip time t (tests).</summary>
    public float HeadLevelOf(int i, float t) => heads == null || i < 0 || i >= heads.Length ? 0f : HeadLevel(t, heads[i].z);
    public bool HeadIsGoal(int i) => heads != null && i >= 0 && i < heads.Length && heads[i].goal;
    public Light HeadLight(int i) => heads == null || i < 0 || i >= heads.Length ? null : heads[i].light;
    public Light WrapLight(int k) => wraps == null || k < 0 || k >= wraps.Length ? null : wraps[k].light;
    public ReflectionProbe[] ArmedProbes => armedProbes;
    public ReflectionProbe[] TrippedProbes => trippedProbes;

    void OnDestroy()
    {
        ReleaseHost();
        if (beamMaterial != null) DestroyMaterial(beamMaterial);
        if (haloMaterial != null) DestroyMaterial(haloMaterial);
    }

    static void DestroyMaterial(Material m)
    {
        if (Application.isPlaying) Destroy(m); else DestroyImmediate(m);
    }
}
