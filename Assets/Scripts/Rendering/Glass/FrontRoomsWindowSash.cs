// FrontRoomsWindowSash.cs -- V6 (C12, the quiet sash pry): the sash look of the map's plain windows while the run's sashPry
// flag is on (FrontRoomsMapWorld.SashPry; 0 in FrontRoomsLevel0.asset, so today nothing here is ever made).
// Research: Documentation/research/glass/v6_sash/10_research.md. Owned by the visual chat (game visual).
//
// The window is a box-head double-hung (a "pocket window": Lunken, US 1,326,677, 1919): two sashes meet on the map's latch box
// (the meeting rails, Y 1.160-1.1955 in the window root frame); a die-cast cam sash lock sits on the room sash's meeting rail and
// its keeper on the hall sash's rail. The map owns the latch box (Window.latch, the aim target), the pry's progress and the pried
// state; this component only draws them:
//   standing   both sashes closed round the GD3 glass (FrontRoomsGlassBreakable's slab and crack stages are untouched);
//   prying     SashPryProgress: the room sash creeps up (4 mm), the keeper's screws back out of the hall rail and it tips (7 deg);
//              the strain is banked like the map's hold (a rebuilt window starts from Window.sashHold);
//   pried      SashPried: the keeper tears off and drops onto the hall side's sill; both sashes run up into the head pocket in
//              0.70 s carrying the glass and any cracks (FrontRoomsGlassBreakable.SashLift), then stop drawing (SashStowed).
//              The opening is then clear from sill to head, as the map's passage, the player's climb and the Relay's step have it;
//   shattered  WindowShattered: the two meeting rails (lock and keeper on them) break out with the glass and drop onto the sill;
//              stiles, head rail and bottom rail stay in the frame (nothing crosses the climb);
//   rebuilt    DressWindow again (a chunk rebuild or shift): IsSashPried -> stowed, the keeper at rest on the sill; no pane ->
//              the rails at rest on the sill; otherwise closed at the banked strain.
// Render-only: no colliders (the map's latch box and pane are the only bodies), no lights, no sound (the map's events are the
// sound line's; RailsDropped gives the sound line the one moment the map's events do not carry). Every part sits inside the stop line and within |Z| <= 0.0205 of the glass, so the rising sashes pass behind the
// head stops (their faces are at 0.022) into the head. Tiers: desktop = hero meshes (the lock holds at 0.3 m: 48-segment turnings,
// slotted countersunk screws, threaded keeper screws, eased arrises) within about 3 m and the phone meshes past that (a LODGroup on
// the lock and on the keeper, no shadow), culled at about 20 m; handhelds (FrontRoomsHandheld.Active) and WebGL = the phone meshes
// (14 segments, no slots or threads) and no hardware shadows. Meshes are built once per tier and shared by every window.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

[DisallowMultipleComponent]
public sealed class FrontRoomsWindowSash : MonoBehaviour
{
    /// <summary>A shatter broke the two meeting rails out (sashPry on): the world point between the rails, then the seconds until
    /// the first and the last rail lands on the sill (one 0.1 s bounce follows each). For 声音设计 (10-31): GlassBroken carries only a
    /// position, and the latch is gone by then.</summary>
    public static event Action<FrontRoomsWindowSash, Vector3, float, float> RailsDropped;

    public const string Label = "Window sash (kit)";

    // ---- section: the window root frame turned like the kit frame (X along the wall, Y up from the floor, +Z = the room's face)
    const float StopX = FrontRoomsInteractableKit.WindowSection.StopX;          // 0.6835, the stop line
    const float StopSill = FrontRoomsInteractableKit.WindowSection.StopSill;    // 0.3665
    const float StopHead = FrontRoomsInteractableKit.WindowSection.StopHead;    // 1.9835
    /// <summary>The lining's sill face: where torn hardware and broken rails come to rest.</summary>
    public const float SillTop = FrontRoomsInteractableKit.WindowSection.LiningSill;
    public const float StileWidth = .045f, RailX = StopX - StileWidth;
    public const float BottomRailTop = .4365f, TopRailBottom = 1.9385f;
    /// <summary>The meeting rails (the map's latch box is centred on 1.175, 0.14 tall, so it holds the rails and the lock).</summary>
    public const float MeetBottom = 1.160f, MeetTop = 1.1955f;
    const float MeetHeight = MeetTop - MeetBottom;
    /// <summary>The sashes' depth: the room sash Z -0.0105..0.0205, the hall sash -0.0205..0.0105; the meeting rails split at 0.</summary>
    public const float ZFace = .0205f, ZStep = .0105f, ZSplit = .0005f;
    const float Arris = .0025f;

    // ---- motion
    /// <summary>How far the sashes run up: the glass's bottom edge (0.354) passes the head lining (1.9995) with 10 mm to spare.</summary>
    public const float Travel = 1.66f;
    public const float RiseSeconds = .70f, RiseDelay = .06f;
    public const float CreepMax = .004f, KeeperTiltMax = 7f, KeeperLiftMax = .0025f;
    /// <summary>Where torn and broken parts land, across the sill from the glass (both linings are 0.16 deep).</summary>
    public const float RestZ = .052f;
    const float Gravity = 9.81f;

    // ---- tiers (as FrontRoomsDoorBreakVisual.PhoneTier)
    /// <summary>Tools: build the phone meshes on desktop too.</summary>
    public static bool DebugForcePhoneTier;
    public static bool PhoneTierOnHandheld = true;
    public static bool PhoneTier
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return true;
#else
            return DebugForcePhoneTier || (PhoneTierOnHandheld && FrontRoomsHandheld.Active);
#endif
        }
    }

    public enum Phase { Standing, Rising, Stowed, Broken }
    /// <summary>What the window shows now.</summary>
    public Phase Now { get; private set; }
    /// <summary>The pry's strain as drawn (0-1, banked like the map's hold).</summary>
    public float Strain => shown;
    /// <summary>Metres the sashes stand raised (0 closed, Travel stowed).</summary>
    public float Lift => lift;
    public Transform Keeper => keeper;
    public Transform LockBody => lockBody;

    FrontRoomsMapWorld map;
    FrontRoomsMapWorld.Window window;
    FrontRoomsGlassBreakable glass;
    Transform pivot, stack, lowerSash, upperSash, lowerRail, upperRail, lockBody, keeper;
    Vector3 keeperHome;
    bool bound, wood, keeperOnRail;
    float strain, shown, prySince = 10f, riseClock, lift, seed;

    sealed class Drop
    {
        public Transform t;
        public Vector3 p0, p1;
        public Quaternion q0, q1;
        public float vy, clock, seconds;
    }
    readonly List<Drop> drops = new List<Drop>(2);

    // =========================================================================================== attach (the kit's DressWindow)
    /// <summary>
    /// Called by FrontRoomsInteractableKit.DressWindow for every window built while map.SashPry is on, after the frame, with the
    /// frame's turn (face A to the room). Builds the look for the window's state now (standing, pried, or broken).
    /// </summary>
    public static FrontRoomsWindowSash Attach(FrontRoomsMapWorld map, FrontRoomsMapWorld.Window window, FrontRoomsMapWorld.GlassBreakRecord record, bool woodFrame, Quaternion turn)
    {
        if (map == null || window == null || window.root == null) return null;
        var sash = window.root.GetComponent<FrontRoomsWindowSash>();
        if (sash == null) sash = window.root.gameObject.AddComponent<FrontRoomsWindowSash>();
        sash.Build(map, window, record, woodFrame, turn);
        return sash;
    }

    void Build(FrontRoomsMapWorld world, FrontRoomsMapWorld.Window w, FrontRoomsMapWorld.GlassBreakRecord record, bool woodFrame, Quaternion turn)
    {
        Unbind();
        if (pivot != null) Kill(pivot.gameObject);
        drops.Clear();
        map = world;
        window = w;
        wood = woodFrame;
        glass = GetComponent<FrontRoomsGlassBreakable>();
        seed = Hash01(w.edge, 7);
        var parts = Parts.For(PhoneTier);
        var phone = parts.phone;
        var member = MemberMaterial(wood);
        var metal = HardwareMaterial(wood);
        var ink = SlotInk;

        pivot = new GameObject(Label).transform;
        pivot.SetParent(transform, false);
        pivot.localRotation = turn;
        stack = Node("sash stack", pivot, Vector3.zero);
        lowerSash = Draw("room sash", stack, Vector3.zero, parts.lowerBody, new[] { member, metal }, ShadowCastingMode.On);
        upperSash = Draw("hall sash", stack, Vector3.zero, parts.upperBody, new[] { member }, ShadowCastingMode.On);
        lowerRail = Draw("room meeting rail", lowerSash, new Vector3(0f, MeetBottom, 0f), parts.lowerRail, new[] { member }, ShadowCastingMode.On);
        upperRail = Draw("hall meeting rail", upperSash, new Vector3(0f, MeetBottom, 0f), parts.upperRail, new[] { member }, ShadowCastingMode.On);
        var hw = phone ? ShadowCastingMode.Off : ShadowCastingMode.On;
        lockBody = Draw("cam sash lock", lowerRail, new Vector3(0f, MeetHeight, 0f), parts.lockBody, new[] { metal, ink }, hw);
        keeperHome = new Vector3(0f, MeetHeight, 0f);
        keeper = Draw("sash lock keeper", upperRail, keeperHome, parts.keeper, new[] { metal, ink }, hw);
        keeperOnRail = true;
        if (!phone)
        {
            // Desktop: the hero hardware within reach (it holds at 0.3 m), the phone meshes past it, nothing at the far end.
            var lite = Parts.For(true);
            HardwareLods(lockBody, lite.lockBody, new[] { metal, ink });
            HardwareLods(keeper, lite.keeper, new[] { metal, ink });
        }

        if (world.IsSashPried(w.edge))
        {
            // Pried before this build: the sashes (and the glass) are up in the head pocket; the torn keeper lies on the sill.
            lift = Travel;
            stack.localPosition = new Vector3(0f, Travel, 0f);
            stack.gameObject.SetActive(false);
            if (glass != null) { glass.SashLift = Travel; glass.SashStowed = true; }
            TearKeeper(false);
            Now = Phase.Stowed;
            shown = strain = 1f;
        }
        else if (w.pane == null)
        {
            // Broken before this build (no pane, not pried): the meeting rails lie on the sill, the rest stays in the frame.
            DropRails(false);
            Now = Phase.Broken;
        }
        else
        {
            Now = Phase.Standing;
            shown = strain = Mathf.Clamp01(w.sashHold / FrontRoomsMapWorld.SashPryHoldSeconds);
            PoseStrain(shown, 0f);
            Bind();
        }
        enabled = false;
    }

    // =========================================================================================== map events
    void Bind()
    {
        if (bound || map == null) return;
        bound = true;
        map.SashPryProgress += OnPryProgress;
        map.SashPryReleased += OnPryReleased;
        map.SashPried += OnPried;
        map.WindowShattered += OnShattered;
    }

    void Unbind()
    {
        if (!bound || map == null) return;
        bound = false;
        map.SashPryProgress -= OnPryProgress;
        map.SashPryReleased -= OnPryReleased;
        map.SashPried -= OnPried;
        map.WindowShattered -= OnShattered;
    }

    void OnDestroy() => Unbind();

    /// <summary>The event is this sash's. Edit mode never calls OnDestroy (DestroyImmediate on a component without
    /// ExecuteAlways), so a sash destroyed there is still on the map's events: the first one after that drops its
    /// handlers instead of reading the dead transform. In play OnDestroy has already unbound it.</summary>
    bool Mine(FrontRoomsMapWorld.Window w)
    {
        if (this == null) { Unbind(); return false; }
        return w != null && w.root == transform && Now == Phase.Standing;
    }

    void OnPryProgress(FrontRoomsMapWorld.Window w, float progress)
    {
        if (!Mine(w)) return;
        strain = Mathf.Max(strain, Mathf.Clamp01(progress));
        prySince = 0f;
        enabled = true;
    }

    void OnPryReleased(FrontRoomsMapWorld.Window w)
    {
        if (!Mine(w)) return;
        prySince = 10f;
        enabled = true;
    }

    void OnPried(FrontRoomsMapWorld.Window w)
    {
        if (!Mine(w)) return;
        Unbind();
        strain = shown = 1f;
        PoseStrain(1f, 0f);
        TearKeeper(true);
        Now = Phase.Rising;
        riseClock = 0f;
        enabled = true;
    }

    void OnShattered(FrontRoomsMapWorld.Window w, Vector3 impactWorld, Vector3 impulse)
    {
        if (!Mine(w)) return;
        Unbind();
        var at = lowerRail != null && upperRail != null ? (lowerRail.position + upperRail.position) * .5f
            : lowerRail != null ? lowerRail.position : upperRail != null ? upperRail.position : transform.position;
        var before = drops.Count;
        DropRails(true);
        float first = float.MaxValue, last = 0f;
        for (var i = before; i < drops.Count; i++) { first = Mathf.Min(first, drops[i].seconds); last = Mathf.Max(last, drops[i].seconds); }
        if (drops.Count > before) RailsDropped?.Invoke(this, at, first, last);
        Now = Phase.Broken;
        enabled = true;
    }

    // =========================================================================================== per frame (only while something moves)
    void Update()
    {
        var dt = Mathf.Min(Time.deltaTime, .05f);
        var busy = false;
        if (Now == Phase.Standing)
        {
            prySince += dt;
            shown = Mathf.MoveTowards(shown, strain, dt * 1.6f);
            var prying = prySince < .15f;
            // While E is held the frame works under the bar: a small rattle on the strain (0.4 mm, 0.4 deg at full strain).
            var jitter = prying ? (Mathf.PerlinNoise(seed * 31f, Time.time * 21f) - .5f) * 2f : 0f;
            PoseStrain(shown, jitter);
            busy = prying || !Mathf.Approximately(shown, strain);
        }
        else if (Now == Phase.Rising)
        {
            riseClock += dt;
            var u = Mathf.Clamp01((riseClock - RiseDelay) / RiseSeconds);
            // Counterweighted and shoved: fast off the sill, easing into the pocket (cubic ease-out).
            var e = 1f - (1f - u) * (1f - u) * (1f - u);
            lift = Travel * e;
            stack.localPosition = new Vector3(0f, lift, 0f);
            if (glass != null) glass.SashLift = lift;
            if (u >= 1f)
            {
                stack.gameObject.SetActive(false);
                if (glass != null) glass.SashStowed = true;
                Now = Phase.Stowed;
            }
            else busy = true;
        }
        for (var i = drops.Count - 1; i >= 0; i--)
        {
            if (TickDrop(drops[i], dt)) busy = true;
            else drops.RemoveAt(i);
        }
        if (!busy) enabled = false;
    }

    void PoseStrain(float s, float jitter)
    {
        var k = Smooth(0f, 1f, s);
        if (lowerSash != null) lowerSash.localPosition = new Vector3(0f, CreepMax * k + .0004f * jitter * k, 0f);
        if (keeper == null || !keeperOnRail) return;
        // The cam pulls the keeper's lip up as the room sash creeps: the two screws back out and it tips about its far edge.
        var tip = Smooth(.35f, 1f, s);
        var tilt = KeeperTiltMax * tip + .4f * jitter * tip;
        var rot = Quaternion.AngleAxis(-tilt, Vector3.right);
        var hinge = new Vector3(0f, 0f, -.0195f);
        keeper.localRotation = rot;
        keeper.localPosition = keeperHome + hinge - rot * hinge + Vector3.up * (KeeperLiftMax * Smooth(.15f, 1f, s));
    }

    // =========================================================================================== tear, rise, break
    void TearKeeper(bool animate)
    {
        if (keeper == null || !keeperOnRail) return;
        keeperOnRail = false;
        keeper.SetParent(pivot, true);
        var yaw = Mathf.Lerp(-40f, 40f, Hash01(window.edge, 11));
        var q1 = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(180f, Vector3.right);
        var p1 = new Vector3(Mathf.Lerp(-.06f, .06f, Hash01(window.edge, 13)), 0f, -RestZ);
        p1.y = SillTop - LowestUnder(keeper, q1);
        if (!animate) { keeper.localPosition = p1; keeper.localRotation = q1; return; }
        // Torn off the hall rail with a flick toward the hall: up 0.3 m/s, then down onto the sill.
        StartDrop(keeper, p1, q1, .3f);
    }

    void DropRails(bool animate)
    {
        DropRail(lowerRail, RestZ, 17, animate);
        DropRail(upperRail, -RestZ, 19, animate);
    }

    void DropRail(Transform rail, float restZ, int salt, bool animate)
    {
        if (rail == null) return;
        rail.SetParent(pivot, true);
        var yaw = Mathf.Lerp(-.8f, .8f, Hash01(window.edge, salt));
        var roll = Mathf.Lerp(-5f, 5f, Hash01(window.edge, salt + 1));
        var q1 = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(roll, Vector3.right);
        var filter = rail.GetComponent<MeshFilter>();
        var centre = filter != null && filter.sharedMesh != null ? filter.sharedMesh.bounds.center : Vector3.zero;
        var p1 = new Vector3(0f, 0f, restZ - (q1 * centre).z);
        p1.y = SillTop - LowestUnder(rail, q1);
        if (!animate) { rail.localPosition = p1; rail.localRotation = q1; return; }
        StartDrop(rail, p1, q1, 0f);
    }

    void StartDrop(Transform t, Vector3 p1, Quaternion q1, float vy)
    {
        var h = Mathf.Max(0f, t.localPosition.y - p1.y);
        var seconds = (vy + Mathf.Sqrt(vy * vy + 2f * Gravity * h)) / Gravity;
        drops.Add(new Drop { t = t, p0 = t.localPosition, q0 = t.localRotation, p1 = p1, q1 = q1, vy = vy, clock = 0f, seconds = Mathf.Max(.05f, seconds) });
        enabled = true;
    }

    // A ballistic fall from where it was to its rest on the sill, turning on the way, with one small bounce (8% of the fall, 0.1 s).
    static bool TickDrop(Drop d, float dt)
    {
        if (d.t == null) return false;
        d.clock += dt;
        const float BounceSeconds = .1f;
        if (d.clock < d.seconds)
        {
            var t = d.clock;
            var u = t / d.seconds;
            var p = Vector3.Lerp(d.p0, d.p1, u);
            p.y = d.p0.y + d.vy * t - .5f * Gravity * t * t;
            d.t.localPosition = p;
            d.t.localRotation = Quaternion.Slerp(d.q0, d.q1, u * u * (3f - 2f * u));
            return true;
        }
        var b = (d.clock - d.seconds) / BounceSeconds;
        var bounce = b < 1f ? Mathf.Sin(b * Mathf.PI) * Mathf.Min(.008f, .08f * (d.p0.y - d.p1.y)) : 0f;
        d.t.localPosition = d.p1 + Vector3.up * bounce;
        d.t.localRotation = d.q1;
        return b < 1f;
    }

    // The lowest point of a part and its children (local, unrotated meshes) under rotation q about the part's own origin.
    static float LowestUnder(Transform part, Quaternion q)
    {
        var low = float.MaxValue;
        foreach (var f in part.GetComponentsInChildren<MeshFilter>(true))
        {
            if (f.sharedMesh == null) continue;
            var b = f.sharedMesh.bounds;
            var offset = f.transform == part ? Vector3.zero : part.InverseTransformPoint(f.transform.position);
            for (var i = 0; i < 8; i++)
            {
                var c = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                low = Mathf.Min(low, (q * (c + offset)).y);
            }
        }
        return low == float.MaxValue ? 0f : low;
    }

    // =========================================================================================== objects
    static Transform Node(string name, Transform parent, Vector3 at)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = at;
        return t;
    }

    static Transform Draw(string name, Transform parent, Vector3 at, Mesh mesh, Material[] materials, ShadowCastingMode shadows)
    {
        var t = Node(name, parent, at);
        t.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = t.gameObject.AddComponent<MeshRenderer>();
        r.sharedMaterials = materials;
        r.shadowCastingMode = shadows;
        r.receiveShadows = true;
        r.lightProbeUsage = LightProbeUsage.BlendProbes;
        return t;
    }

    /// <summary>The hardware's switch heights (screen share of a 66 mm lock: hero within about 3 m, the phone mesh to about 20 m).</summary>
    public const float HeroLodHeight = .014f, LiteLodHeight = .002f;

    // A LODGroup on a hardware node: LOD0 its own hero renderer, LOD1 a child with the phone mesh (no shadow), culled beyond.
    static void HardwareLods(Transform node, Mesh lite, Material[] materials)
    {
        if (node == null || lite == null) return;
        var hero = node.GetComponent<MeshRenderer>();
        var far = Draw("LOD1", node, Vector3.zero, lite, materials, ShadowCastingMode.Off).GetComponent<MeshRenderer>();
        var group = node.gameObject.AddComponent<LODGroup>();
        group.SetLODs(new[] { new LOD(HeroLodHeight, new Renderer[] { hero }), new LOD(LiteLodHeight, new Renderer[] { far }) });
        group.RecalculateBounds();
        FrontRoomsLodFade.Apply(group);
    }

    static void Kill(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
    }

    static float Smooth(float a, float b, float x)
    {
        var t = Mathf.Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }

    static float Hash01(long edge, int salt)
    {
        unchecked
        {
            var h = (ulong)edge * 0x9E3779B97F4A7C15UL + (ulong)salt * 0xBF58476D1CE4E5B9UL;
            h ^= h >> 31; h *= 0x94D049BB133111EBUL; h ^= h >> 29;
            return (h >> 40) / (float)(1UL << 24);
        }
    }

    // =========================================================================================== materials
    static readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();

    static Material Surface(string name, Color fallback, float smoothness, float metallic)
    {
        if (materialCache.TryGetValue(name, out var m) && m != null) return m;
        m = FrontRoomsSurfaces.TryGet(name);
        if (m == null) m = FrontRoomsSurfaces.Lit("Window sash / " + name, fallback, smoothness, metallic);
        materialCache[name] = m;
        return m;
    }

    /// <summary>The sash members: the frame's own wood (W-L0 walnut) or pressed steel (W-OF dark bronze).</summary>
    static Material MemberMaterial(bool woodFrame) => woodFrame
        ? Surface("Prop_WoodWalnut", new Color(.20f, .12f, .07f), .45f, 0f)
        : Surface("Prop_SteelBrown", new Color(.16f, .12f, .09f), .45f, .6f);

    /// <summary>The lock, keeper, screws and lifts: cast brass on the walnut sash, satin bronze-brass on the steel one.</summary>
    static Material HardwareMaterial(bool woodFrame) => woodFrame
        ? Surface("Prop_Brass", new Color(.62f, .48f, .24f), .6f, 1f)
        : Surface("Prop_BrassSatin", new Color(.55f, .45f, .28f), .45f, 1f);

    /// <summary>The screw slots' shadowed floor.</summary>
    static Material SlotInk => Surface("Window sash / slot", new Color(.06f, .05f, .04f), .15f, 0f);

    // =========================================================================================== meshes (once per tier)
    sealed class Parts
    {
        public bool phone;
        public Mesh lowerBody, upperBody, lowerRail, upperRail, lockBody, keeper;
        static readonly Parts[] cache = new Parts[2];

        public static Parts For(bool phone)
        {
            var i = phone ? 1 : 0;
            if (cache[i] != null && cache[i].lockBody != null) return cache[i];
            var p = new Parts { phone = phone };
            p.lowerBody = BuildLowerBody(phone);
            p.upperBody = BuildUpperBody();
            p.lowerRail = BuildRail(true);
            p.upperRail = BuildRail(false);
            p.lockBody = BuildLock(phone);
            p.keeper = BuildKeeper(phone);
            cache[i] = p;
            return p;
        }
    }

    // The room (lower) sash: two stiles, the deep bottom rail and two flush brass lifts on its room face. Its meeting rail is its own part.
    static Mesh BuildLowerBody(bool phone)
    {
        var m = new MeshKit(2);
        foreach (var s in new[] { -1f, 1f })
            m.ChamferBox(0, new Vector3(s < 0 ? -StopX : RailX, StopSill, -ZStep), new Vector3(s < 0 ? -RailX : StopX, MeetTop, ZFace), Arris, 1);
        m.ChamferBox(0, new Vector3(-RailX, StopSill, -ZStep), new Vector3(RailX, BottomRailTop, ZFace), Arris, 0);
        // Flush sash lifts (a box-head sash is lifted by its bottom rail): 44 x 26 mm plates, 0.6 mm proud, a 0.8 mm finger bar;
        // flush so they pass behind the head stops (face at Z 0.022) when the sashes run up.
        foreach (var x in new[] { -.40f, .40f })
        {
            m.M = Matrix4x4.TRS(new Vector3(x, .401f, ZFace), Quaternion.AngleAxis(90f, Vector3.right), Vector3.one);
            m.Slab(1, RoundedRect(0f, 0f, .022f, .013f, .004f, phone ? 2 : 5), 0f, .0006f, phone ? 0f : .0003f, true, false, 0);
            m.ChamferBox(1, new Vector3(-.014f, .0006f, -.0035f), new Vector3(.014f, .0014f, .0010f), phone ? 0f : .0003f, 0);
            m.M = Matrix4x4.identity;
        }
        return m.ToMesh("Window sash / room sash");
    }

    // The hall (upper) sash: two stiles and the head rail.
    static Mesh BuildUpperBody()
    {
        var m = new MeshKit(1);
        foreach (var s in new[] { -1f, 1f })
            m.ChamferBox(0, new Vector3(s < 0 ? -StopX : RailX, MeetBottom, -ZFace), new Vector3(s < 0 ? -RailX : StopX, StopHead, ZStep), Arris, 1);
        m.ChamferBox(0, new Vector3(-RailX, TopRailBottom, -ZFace), new Vector3(RailX, StopHead, ZStep), Arris, 0);
        return m.ToMesh("Window sash / hall sash");
    }

    // A meeting rail in its own space (origin at the rail's foot on the meeting line): the room half or the hall half.
    static Mesh BuildRail(bool room)
    {
        var m = new MeshKit(1);
        var z0 = room ? ZSplit : -ZFace;
        var z1 = room ? ZFace : -ZSplit;
        m.ChamferBox(0, new Vector3(-RailX, 0f, z0), new Vector3(RailX, MeetHeight, z1), Arris, 0);
        return m.ToMesh(room ? "Window sash / room meeting rail" : "Window sash / hall meeting rail");
    }

    // ---- the cam sash lock (die-cast, 2-1/4 in screw centres), origin at its foot on the meeting line; the room half (+Z)
    const float LockZ = .01075f, HousingX = -.004f, HousingZ = .0090f, BaseTop = .0028f;

    static Mesh BuildLock(bool phone)
    {
        var m = new MeshKit(2);
        var seg = phone ? 14 : 48;
        // base plate 66 x 18.5 mm, 2.8 mm, eased top edge
        m.Slab(0, RoundedRect(0f, LockZ, .033f, .00925f, .0065f, phone ? 3 : 8), 0f, BaseTop, phone ? 0f : .0007f, true, false, 0);
        // two countersunk oval-head screws, slotted at their own angles
        Screw(m, new Vector3(-.0285f, BaseTop, LockZ), .0031f, 37f, phone, false);
        Screw(m, new Vector3(.0285f, BaseTop, LockZ), .0031f, 112f, phone, false);
        // the cam: an eccentric disc under the housing whose lobe reaches across the meeting line into the keeper's lip
        m.Slab(0, Circle(HousingX, HousingZ - .0035f, .0095f, seg), BaseTop + .0001f, BaseTop + .0029f, 0f, true, true, 0);
        // the housing: a turned drum with a filleted, slightly domed top
        m.Lathe(0, new Vector3(HousingX, BaseTop, HousingZ), new[]
        {
            new Vector2(.0082f, 0f), new Vector2(.0082f, .0058f), new Vector2(.0080f, .0065f), new Vector2(.0075f, .0071f),
            new Vector2(.0067f, .0076f), new Vector2(.0056f, .0081f), new Vector2(.0035f, .0086f), new Vector2(0f, .0088f),
        }, seg);
        // the thumb lever: a tapered arm along the rail, its thumb end raised 7 deg, on a domed hub
        var leverY = BaseTop + .0084f;
        var hull = Hull(Circle(0f, 0f, .0042f, seg / 2), Circle(.040f, 0f, .0056f, seg / 2));
        m.M = Matrix4x4.TRS(new Vector3(HousingX, leverY, HousingZ), Quaternion.AngleAxis(7f, Vector3.forward), Vector3.one);
        m.Slab(0, hull, 0f, .0030f, phone ? 0f : .0006f, true, true, 0);
        m.Lathe(0, new Vector3(0f, .0030f, 0f), new[] { new Vector2(.0047f, 0f), new Vector2(.0046f, .0008f), new Vector2(.0038f, .0018f), new Vector2(.0020f, .0024f), new Vector2(0f, .0026f) }, phone ? 10 : 32);
        m.M = Matrix4x4.identity;
        return m.ToMesh("Window sash / cam sash lock");
    }

    // ---- the keeper: plate, riser and hooked lip on the hall rail, two screws whose shanks show as they pull out
    const float KeeperZ = -.0104f;

    static Mesh BuildKeeper(bool phone)
    {
        var m = new MeshKit(2);
        var c = phone ? 0f : .0005f;
        m.Slab(0, RoundedRect(0f, KeeperZ, .026f, .0086f, .005f, phone ? 3 : 8), 0f, .0026f, phone ? 0f : .0006f, true, true, 0);
        m.ChamferBox(0, new Vector3(-.013f, .0026f, -.0185f), new Vector3(.013f, .0078f, -.0150f), c, 0);
        m.ChamferBox(0, new Vector3(-.013f, .0058f, -.0185f), new Vector3(.013f, .0078f, -.0025f), c, 0);
        Screw(m, new Vector3(-.0195f, .0026f, KeeperZ), .0029f, 64f, phone, true);
        Screw(m, new Vector3(.0195f, .0026f, KeeperZ), .0029f, 151f, phone, true);
        return m.ToMesh("Window sash / keeper");
    }

    // An oval countersunk head (spherical cap) on a surface at `at`; hero: the slot as a dark strip on the dome; with a shank:
    // 16 mm of #6 wood screw under the plate (threads on the hero mesh), hidden in the rail until the keeper lifts.
    static void Screw(MeshKit m, Vector3 at, float r, float slotDeg, bool phone, bool shank)
    {
        const float rim = .00015f, dome = .0009f;
        var rho = (r * r + dome * dome) / (2f * dome);
        float H(float x) => rim + Mathf.Sqrt(Mathf.Max(0f, rho * rho - x * x)) - (rho - dome);
        var steps = phone ? 3 : 6;
        var prof = new List<Vector2> { new Vector2(r, 0f), new Vector2(r, rim) };
        for (var i = 1; i <= steps; i++) { var x = r * (1f - (float)i / steps); prof.Add(new Vector2(x, H(x))); }
        m.Lathe(0, at, prof.ToArray(), phone ? 8 : 20);
        if (!phone)
        {
            var dir = Quaternion.AngleAxis(slotDeg, Vector3.up) * Vector3.right;
            var side = Vector3.Cross(Vector3.up, dir) * .00035f;
            const int k = 8;
            for (var i = 0; i < k; i++)
            {
                var s0 = Mathf.Lerp(-.86f * r, .86f * r, (float)i / k);
                var s1 = Mathf.Lerp(-.86f * r, .86f * r, (float)(i + 1) / k);
                var a = at + dir * s0 + Vector3.up * (H(Mathf.Abs(s0)) + .00004f);
                var b = at + dir * s1 + Vector3.up * (H(Mathf.Abs(s1)) + .00004f);
                m.Quad(1, a - side, b - side, b + side, a + side, Vector3.up, 0);
            }
        }
        if (!shank) return;
        var sp = new List<Vector2> { new Vector2(0f, -.0165f), new Vector2(.0009f, -.0150f) };
        if (phone) sp.Add(new Vector2(.0016f, -.0130f));
        else for (var y = -.0138f; y < -.0012f; y += .0014f) { sp.Add(new Vector2(.0019f, y)); sp.Add(new Vector2(.0014f, y + .0007f)); }
        sp.Add(new Vector2(.0016f, 0f));
        // The shank's profile runs downward-in to upward-out: its lathe normals must face out, so it is laid from the tip up.
        m.Lathe(0, at - Vector3.up * .0026f, sp.ToArray(), phone ? 6 : 14);
    }

    // =========================================================================================== outlines
    static Vector2[] RoundedRect(float cx, float cz, float hx, float hz, float r, int perCorner)
    {
        r = Mathf.Min(r, Mathf.Min(hx, hz));
        var pts = new List<Vector2>();
        var centres = new[] { new Vector2(hx - r, hz - r), new Vector2(-hx + r, hz - r), new Vector2(-hx + r, -hz + r), new Vector2(hx - r, -hz + r) };
        for (var c = 0; c < 4; c++)
            for (var i = 0; i <= perCorner; i++)
            {
                var a = (c * 90f + 90f * i / Mathf.Max(1, perCorner)) * Mathf.Deg2Rad;
                pts.Add(new Vector2(cx + centres[c].x + r * Mathf.Cos(a), cz + centres[c].y + r * Mathf.Sin(a)));
            }
        return pts.ToArray();
    }

    static Vector2[] Circle(float cx, float cz, float r, int n)
    {
        var pts = new Vector2[n];
        for (var i = 0; i < n; i++) { var a = 2f * Mathf.PI * i / n; pts[i] = new Vector2(cx + r * Mathf.Cos(a), cz + r * Mathf.Sin(a)); }
        return pts;
    }

    // Andrew's monotone chain: the convex hull of two point sets, counter-clockwise.
    static Vector2[] Hull(Vector2[] a, Vector2[] b)
    {
        var pts = new List<Vector2>(a.Length + b.Length);
        pts.AddRange(a);
        pts.AddRange(b);
        pts.Sort((p, q) => p.x != q.x ? p.x.CompareTo(q.x) : p.y.CompareTo(q.y));
        var h = new List<Vector2>();
        for (var pass = 0; pass < 2; pass++)
        {
            var start = h.Count;
            for (var k = 0; k < pts.Count; k++)
            {
                var p = pass == 0 ? pts[k] : pts[pts.Count - 1 - k];
                while (h.Count >= start + 2 && Cross(h[h.Count - 2], h[h.Count - 1], p) <= 0f) h.RemoveAt(h.Count - 1);
                h.Add(p);
            }
            h.RemoveAt(h.Count - 1);
        }
        return h.ToArray();
    }

    static float Cross(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);

    // =========================================================================================== mesh builder
    /// <summary>A small mesh builder: flat or smooth faces, every point through M (a rotation and translation), UVs in metres
    /// (u along the member, for FrontRooms/Surface's mesh UVs), winding from the intended outward side (Unity: clockwise front).</summary>
    sealed class MeshKit
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector3> n = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int>[] tris;
        public Matrix4x4 M = Matrix4x4.identity;

        public MeshKit(int submeshes)
        {
            tris = new List<int>[submeshes];
            for (var i = 0; i < submeshes; i++) tris[i] = new List<int>();
        }

        int Add(Vector3 p, Vector3 normal, Vector2 t)
        {
            v.Add(M.MultiplyPoint3x4(p));
            n.Add(M.MultiplyVector(normal).normalized);
            uv.Add(t);
            return v.Count - 1;
        }

        static Vector2 Planar(Vector3 p, Vector3 normal, int uAxis)
        {
            var ax = Mathf.Abs(normal.x); var ay = Mathf.Abs(normal.y); var az = Mathf.Abs(normal.z);
            var drop = ax >= ay && ax >= az ? 0 : ay >= az ? 1 : 2;
            var a1 = drop == 0 ? 1 : 0;
            var a2 = drop == 2 ? 1 : 2;
            if (uAxis == a2) return new Vector2(p[a2], p[a1]);
            return new Vector2(p[a1], p[a2]);
        }

        /// <summary>One flat triangle, wound to face `outward`.</summary>
        public void Tri(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 outward, int uAxis)
        {
            var normal = Vector3.Cross(b - a, c - a);
            if (normal.sqrMagnitude < 1e-20f) return;
            var flip = Vector3.Dot(normal, outward) < 0f;
            normal = normal.normalized * (flip ? -1f : 1f);
            var i0 = Add(a, normal, Planar(a, normal, uAxis));
            var i1 = Add(b, normal, Planar(b, normal, uAxis));
            var i2 = Add(c, normal, Planar(c, normal, uAxis));
            var t = tris[sub];
            if (!flip) { t.Add(i0); t.Add(i1); t.Add(i2); } else { t.Add(i0); t.Add(i2); t.Add(i1); }
        }

        /// <summary>A flat planar quad a-b-c-d (in order round its edge), wound to face `outward`.</summary>
        public void Quad(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward, int uAxis)
        {
            Tri(sub, a, b, c, outward, uAxis);
            Tri(sub, a, c, d, outward, uAxis);
        }

        /// <summary>A smooth triangle (per-vertex normals); the winding follows the normals' sum.</summary>
        public void TriN(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 na, Vector3 nb, Vector3 nc, Vector2 ta, Vector2 tb, Vector2 tc)
        {
            var normal = Vector3.Cross(b - a, c - a);
            if (normal.sqrMagnitude < 1e-20f) return;
            var flip = Vector3.Dot(normal, na + nb + nc) < 0f;
            var i0 = Add(a, na, ta);
            var i1 = Add(b, nb, tb);
            var i2 = Add(c, nc, tc);
            var t = tris[sub];
            if (!flip) { t.Add(i0); t.Add(i1); t.Add(i2); } else { t.Add(i0); t.Add(i2); t.Add(i1); }
        }

        /// <summary>A box with every arris chamfered by c (flat chamfers and corner facets: crisp highlights); c = 0 is a plain box.</summary>
        public void ChamferBox(int sub, Vector3 lo, Vector3 hi, float c, int uAxis)
        {
            var size = hi - lo;
            c = Mathf.Min(c, .45f * Mathf.Min(size.x, Mathf.Min(size.y, size.z)));
            // The point of corner (sx, sy, sz) that lies on its face across axis f: the other two coordinates pulled in by c.
            Vector3 F(int sx, int sy, int sz, int f)
            {
                var p = new Vector3(sx == 1 ? hi.x : lo.x, sy == 1 ? hi.y : lo.y, sz == 1 ? hi.z : lo.z);
                if (f != 0) p.x += sx == 1 ? -c : c;
                if (f != 1) p.y += sy == 1 ? -c : c;
                if (f != 2) p.z += sz == 1 ? -c : c;
                return p;
            }
            int S(int[] s, int axis) => s[axis];
            // six faces
            for (var f = 0; f < 3; f++)
                for (var side = 0; side < 2; side++)
                {
                    var a1 = f == 0 ? 1 : 0;
                    var a2 = f == 2 ? 1 : 2;
                    var pts = new Vector3[4];
                    var k = 0;
                    foreach (var pair in new[] { new[] { 0, 0 }, new[] { 1, 0 }, new[] { 1, 1 }, new[] { 0, 1 } })
                    {
                        var s = new int[3];
                        s[f] = side; s[a1] = pair[0]; s[a2] = pair[1];
                        pts[k++] = F(S(s, 0), S(s, 1), S(s, 2), f);
                    }
                    var outward = Vector3.zero;
                    outward[f] = side == 1 ? 1f : -1f;
                    Quad(sub, pts[0], pts[1], pts[2], pts[3], outward, uAxis);
                }
            if (c <= 1e-6f) return;
            // twelve edge chamfers: the edge runs along e, between the faces across a and b
            for (var e = 0; e < 3; e++)
            {
                var a = e == 0 ? 1 : 0;
                var b = e == 2 ? 1 : 2;
                for (var sa = 0; sa < 2; sa++)
                    for (var sb = 0; sb < 2; sb++)
                    {
                        var lo3 = new int[3]; lo3[e] = 0; lo3[a] = sa; lo3[b] = sb;
                        var hi3 = new int[3]; hi3[e] = 1; hi3[a] = sa; hi3[b] = sb;
                        var outward = Vector3.zero;
                        outward[a] = sa == 1 ? 1f : -1f;
                        outward[b] = sb == 1 ? 1f : -1f;
                        Quad(sub, F(lo3[0], lo3[1], lo3[2], a), F(hi3[0], hi3[1], hi3[2], a), F(hi3[0], hi3[1], hi3[2], b), F(lo3[0], lo3[1], lo3[2], b), outward, uAxis);
                    }
            }
            // eight corner facets
            for (var i = 0; i < 8; i++)
            {
                int sx = i & 1, sy = (i >> 1) & 1, sz = (i >> 2) & 1;
                var outward = new Vector3(sx == 1 ? 1f : -1f, sy == 1 ? 1f : -1f, sz == 1 ? 1f : -1f);
                Tri(sub, F(sx, sy, sz, 0), F(sx, sy, sz, 1), F(sx, sy, sz, 2), outward, uAxis);
            }
        }

        /// <summary>
        /// A surface of revolution round the local Y axis through `c`: the profile (radius, height) runs up and in (its outward side is
        /// to the right of the direction of travel, so a drum's wall faces out and a dome's top faces up). Smooth round the axis;
        /// along the profile, joints under 40 deg are smoothed and sharper ones kept crisp.
        /// </summary>
        public void Lathe(int sub, Vector3 c, Vector2[] prof, int seg)
        {
            var count = prof.Length - 1;
            if (count < 1 || seg < 3) return;
            var ns = new Vector2[count];
            for (var i = 0; i < count; i++)
            {
                var d = prof[i + 1] - prof[i];
                ns[i] = d.sqrMagnitude > 1e-14f ? new Vector2(d.y, -d.x).normalized : Vector2.up;
            }
            bool SmoothJoin(int i, int j) => Vector2.Dot(ns[i], ns[j]) > .766f;
            var len = 0f;
            for (var i = 0; i < count; i++)
            {
                var p0 = prof[i];
                var p1 = prof[i + 1];
                var segLen = Vector2.Distance(p0, p1);
                if (segLen < 1e-7f) continue;
                var n0 = i > 0 && SmoothJoin(i - 1, i) ? (ns[i - 1] + ns[i]).normalized : ns[i];
                var n1 = i + 1 < count && SmoothJoin(i, i + 1) ? (ns[i] + ns[i + 1]).normalized : ns[i];
                for (var j = 0; j < seg; j++)
                {
                    var a0 = 2f * Mathf.PI * j / seg;
                    var a1 = 2f * Mathf.PI * (j + 1) / seg;
                    var r0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                    var r1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                    var A = c + r0 * p0.x + Vector3.up * p0.y;
                    var B = c + r1 * p0.x + Vector3.up * p0.y;
                    var C = c + r1 * p1.x + Vector3.up * p1.y;
                    var D = c + r0 * p1.x + Vector3.up * p1.y;
                    var nA = r0 * n0.x + Vector3.up * n0.y;
                    var nB = r1 * n0.x + Vector3.up * n0.y;
                    var nC = r1 * n1.x + Vector3.up * n1.y;
                    var nD = r0 * n1.x + Vector3.up * n1.y;
                    var u0 = (float)j / seg * 2f * Mathf.PI * Mathf.Max(p0.x, p1.x);
                    var u1 = (float)(j + 1) / seg * 2f * Mathf.PI * Mathf.Max(p0.x, p1.x);
                    var tA = new Vector2(u0, len); var tB = new Vector2(u1, len);
                    var tC = new Vector2(u1, len + segLen); var tD = new Vector2(u0, len + segLen);
                    TriN(sub, A, B, C, nA, nB, nC, tA, tB, tC);
                    TriN(sub, A, C, D, nA, nC, nD, tA, tC, tD);
                }
                len += segLen;
            }
        }

        /// <summary>
        /// A plate: a closed outline in (x, z) (convex, or star-shaped round its centroid) between heights y0 and y1, with a 45 deg
        /// chamfer of `bevel` round its top edge, smooth or flat walls, a top cap and optionally a bottom cap.
        /// </summary>
        public void Slab(int sub, Vector2[] outline, float y0, float y1, float bevel, bool smoothWalls, bool bottom, int uAxis)
        {
            var k = outline.Length;
            if (k < 3) return;
            var area = 0f;
            var centroid = Vector2.zero;
            for (var i = 0; i < k; i++)
            {
                var p = outline[i]; var q = outline[(i + 1) % k];
                area += p.x * q.y - q.x * p.y;
                centroid += p;
            }
            centroid /= k;
            var ccw = area > 0f;
            var en = new Vector2[k];   // outward normal of edge i (i -> i+1)
            for (var i = 0; i < k; i++)
            {
                var d = outline[(i + 1) % k] - outline[i];
                var o = ccw ? new Vector2(d.y, -d.x) : new Vector2(-d.y, d.x);
                en[i] = o.sqrMagnitude > 1e-16f ? o.normalized : Vector2.zero;
            }
            var vn = new Vector2[k];   // vertex normals (miter direction)
            var inset = new Vector2[k];
            bevel = Mathf.Max(0f, Mathf.Min(bevel, (y1 - y0) * .8f));
            for (var i = 0; i < k; i++)
            {
                var a = en[(i - 1 + k) % k]; var b = en[i];
                var m = (a + b).sqrMagnitude > 1e-12f ? (a + b).normalized : b;
                vn[i] = m;
                var cosHalf = Mathf.Max(.5f, Vector2.Dot(m, b));
                inset[i] = outline[i] - m * (bevel / cosHalf);
            }
            var top = y1 - bevel;
            var run = 0f;
            for (var i = 0; i < k; i++)
            {
                var j = (i + 1) % k;
                var p = outline[i]; var q = outline[j];
                var edge = Vector2.Distance(p, q);
                var npi = smoothWalls ? vn[i] : en[i];
                var npj = smoothWalls ? vn[j] : en[i];
                var A = new Vector3(p.x, y0, p.y); var B = new Vector3(q.x, y0, q.y);
                var C = new Vector3(q.x, top, q.y); var D = new Vector3(p.x, top, p.y);
                var nA = new Vector3(npi.x, 0f, npi.y); var nB = new Vector3(npj.x, 0f, npj.y);
                var tA = new Vector2(run, y0); var tB = new Vector2(run + edge, y0);
                var tC = new Vector2(run + edge, top); var tD = new Vector2(run, top);
                TriN(sub, A, B, C, nA, nB, nB, tA, tB, tC);
                TriN(sub, A, C, D, nA, nB, nA, tA, tC, tD);
                if (bevel > 0f)
                {
                    var E = new Vector3(inset[j].x, y1, inset[j].y); var G = new Vector3(inset[i].x, y1, inset[i].y);
                    var bi = (nA + Vector3.up).normalized; var bj = (nB + Vector3.up).normalized;
                    TriN(sub, D, C, E, bi, bj, bj, tD, tC, new Vector2(run + edge, y1));
                    TriN(sub, D, E, G, bi, bj, bi, tD, new Vector2(run + edge, y1), new Vector2(run, y1));
                }
                run += edge;
            }
            var ring = bevel > 0f ? inset : outline;
            var ctop = new Vector3(centroid.x, y1, centroid.y);
            for (var i = 0; i < k; i++)
            {
                var j = (i + 1) % k;
                var a = new Vector3(ring[i].x, y1, ring[i].y); var b = new Vector3(ring[j].x, y1, ring[j].y);
                Tri(sub, ctop, a, b, Vector3.up, uAxis);
                if (bottom)
                {
                    var cb = new Vector3(centroid.x, y0, centroid.y);
                    Tri(sub, cb, new Vector3(outline[i].x, y0, outline[i].y), new Vector3(outline[j].x, y0, outline[j].y), Vector3.down, uAxis);
                }
            }
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.subMeshCount = tris.Length;
            for (var i = 0; i < tris.Length; i++) mesh.SetTriangles(tris[i], i);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
