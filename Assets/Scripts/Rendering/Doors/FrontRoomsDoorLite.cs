using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The vision lite of a door (door vision, research/door_vision/30_design.md sections 2.3, 3, 5, 60_landing.md; 游戏视觉; Red's
/// set A: lobby F4b, office F1, run F5, exit F13). Render-only: one per lited leaf (a D1.6 map leaf kit, a title stream door
/// leaf, the station entrance's dark skin, the C22 EXIT leaf and its sealed twin).
///
/// What shows through (30_design 3): one rule, two states, never a fake room.
/// - LIVE: the glass transmits (Glass_Wired); the real room behind is drawn as the game already draws it.
/// - DARK: transmission 0, an opaque dark wired pane that keeps its reflection (Glass_WiredDark; Valve's
///   func_areaportalwindow "rendered window"), while the far side is not ready, and for good on a dead door
///   (the station entrance, the shut terminal door).
/// LIVE and DARK cross-fade in FadeSeconds (sine in-out). A lite at rest uses one of the two shared materials (SRP
/// Batcher kept); a lite that is fading or being peeked through borrows one of the pooled fade materials for that time.
///
/// Readiness: a map lite is LIVE once the chunks on both sides of its door are built (its stand-in, <see cref="ChunksBuilt"/>).
/// CONTRACT M-L4 asks 关卡设计 for a per-cell CellReadyForLite (built, furnished, lamps evaluated), set through
/// <see cref="MapDoorReady"/>. A stream lite needs its Ready (CONTRACT T-L1): without one it stays DARK, a dark wired mirror.
/// <see cref="ForceNotReady"/> is the lab switch.
///
/// The player-side sight rule (30_design 5.2, CONTRACT M-L2): <see cref="PassesPlayerSight"/> says whether a ray that
/// meets a lited leaf's collider at a point passes through the glass (inside liteRect inset 10 mm). The Relay's own sight
/// never asks it (RelaySeesThroughLites = false): the leaf collider stays one solid box.
/// No per-frame allocation once warm; harness rules: ASCII, no tuples, FrontRoomsObjectId instead of GetInstanceID.
///
/// Glazing and shape (zone doors Build B, 2026-11-06, research/double_doors/20_design.md B3.5; append-only): a lite is Wired
/// (Glass_Wired / Glass_WiredDark, every lite before the pool) or Tempered (Glass_Tempered / Glass_TemperedDark: the same glass
/// without the wire keyword, the pool's clear 1/4 in panes), and Rect or Round (the A1 porthole: the sight test is the Rect's
/// inscribed circle, inset 10 mm). Each glazing has its own shared pair and its own fade pool. Today's lites keep Wired and Rect:
/// their materials, pool and sight test are as before.
/// </summary>
[AddComponentMenu("")]
[DisallowMultipleComponent]
public sealed class FrontRoomsDoorLite : MonoBehaviour
{
    public enum Kind { MapDoor, StreamDoor, StationSkin }

    /// <summary>The glass of a lite (append-only).</summary>
    public enum Glazing : byte { Wired, Tempered }

    /// <summary>The lite's outline for the sight test (append-only): Rect, or Round (the circle inscribed in the Rect).</summary>
    public enum Shape : byte { Rect, Round }

    public const string LiveMaterialName = "Glass_Wired", DarkMaterialName = "Glass_WiredDark";
    public const string TemperedLiveName = "Glass_Tempered", TemperedDarkName = "Glass_TemperedDark";
    /// <summary>LIVE to DARK and back (30_design 2.3).</summary>
    public const float FadeSeconds = .40f;
    /// <summary>The player-side sight passes inside the lite rect inset by this much (30_design 5.2).</summary>
    public const float SightInset = .010f;
    /// <summary>The peek's reflection scale (30_design 4.5: "cupped hands") and wire filter width.</summary>
    public const float PeekReflect = .15f, PeekWireSoftPx = 2.5f;
    /// <summary>A shut door: the leaf within this many degrees of closed (30_design 4.2 Available).</summary>
    public const float ShutDegrees = 2f;
    /// <summary>At most this many lites fade at once; the rest snap (40_prototype 5.1). A peek always gets its material.</summary>
    public const int FadePoolSize = 4;

    /// <summary>Lab switch (prototype S5): every map lite reads "not ready" and goes DARK.</summary>
    public static bool ForceNotReady;
    /// <summary>The map's per-door readiness (CONTRACT M-L4). Null = the stand-in, both chunks built.</summary>
    public static Func<FrontRoomsMapWorld.Door, bool> MapDoorReady;
    /// <summary>RelaySeesThroughLites (系统设计 S-L1): false. The Relay's sight never asks this class.</summary>
    public const bool RelaySeesThroughLites = false;

    static readonly List<FrontRoomsDoorLite> all = new List<FrontRoomsDoorLite>();
    static readonly Dictionary<int, FrontRoomsDoorLite> byCollider = new Dictionary<int, FrontRoomsDoorLite>();
    public static IReadOnlyList<FrontRoomsDoorLite> All => all;

    public Kind LiteKind { get; private set; }
    /// <summary>A dead door: DARK for good, no peek (the station entrance, the shut terminal door).</summary>
    public bool Dead { get; private set; }
    /// <summary>The stream's own readiness (the room behind connected); map doors use MapReady.</summary>
    public Func<bool> Ready;
    public FrontRoomsMapWorld.Door Door { get; private set; }
    /// <summary>The frame liteRect is measured in (the door root for a map leaf; the leaf itself for the stream).</summary>
    public Transform Root { get; private set; }
    /// <summary>(z0, z1, y0, y1) of the visible glass in Root's frame (map leaves: the kit sidecar's liteRect).</summary>
    public Vector4 Rect { get; private set; }
    public Collider LeafCollider { get; private set; }
    /// <summary>0 DARK .. 1 LIVE (eased on the fade).</summary>
    public float Live01 => live;
    public bool IsLive => !Dead && live >= 1f && target >= 1f;
    /// <summary>The peek's weight on this lite (0 none .. 1 the eye at the glass).</summary>
    public float PeekWeight { get; private set; }
    /// <summary>Seconds the last LIVE / DARK change took (the harness's G7 reading).</summary>
    public float LastFadeSeconds { get; private set; }
    /// <summary>The lite's glass (Wired unless a stream lite was attached Tempered, or a kit carries Glass_Tempered).</summary>
    public Glazing LiteGlazing { get; private set; }
    /// <summary>The lite's outline for the sight test.</summary>
    public Shape LiteShape { get; private set; }

    readonly List<Renderer> glassRenderers = new List<Renderer>();
    readonly List<int> glassSlots = new List<int>();
    float live, target, fadeClock = -1f;
    Material borrowed, applied;
    Glazing borrowedGlazing;
    Func<bool> peekAvailable;
    FrontRoomsMapWorld map;
    // The door break took over the glass: the leaf this sits on is switched off, the lite lives on in the break's body.
    bool adopted;

    static Material liveShared, darkShared;
    static readonly Stack<Material> pool = new Stack<Material>();
    static int poolMade;
    // the tempered glazing's shared pair and fade pool (Build B), made and loaded only when a tempered lite exists
    static Material temperedLive, temperedDark;
    static readonly Stack<Material> temperedPool = new Stack<Material>();
    static int temperedMade;
    static Runner runner;
    static readonly int LiteDarkId = Shader.PropertyToID("_LiteDark"), PeekReflectId = Shader.PropertyToID("_PeekReflect"), WireSoftId = Shader.PropertyToID("_WireSoftPx");

    // ------------------------------------------------------------------ set-up
    /// <summary>A D1.6 map leaf (DressDoor): the kit instance carrying the Glass_Wired submesh, its door and rig.</summary>
    public static FrontRoomsDoorLite AttachMap(GameObject leafKit, FrontRoomsMapWorld.Door door, Transform root, Vector4 rect, FrontRoomsMapWorld map)
    {
        if (leafKit == null) return null;
        var lite = leafKit.GetComponent<FrontRoomsDoorLite>();
        if (lite == null) lite = leafKit.AddComponent<FrontRoomsDoorLite>();
        lite.LiteKind = Kind.MapDoor;
        lite.Door = door;
        lite.map = map;
        lite.Root = root;
        lite.Rect = rect;
        lite.LeafCollider = door != null ? door.leaf : null;
        lite.CollectGlass(leafKit);
        if (lite.LeafCollider != null) byCollider[FrontRoomsObjectId.Of(lite.LeafCollider)] = lite;
        lite.InitState();
        return lite;
    }

    /// <summary>As above with the lite's outline for the sight test (the zone doors' Map stage, 2026-11-06: the pool member's A1
    /// porthole is Round; its glazing is the kit's Glass_Tempered, read by CollectGlass). The call above stays the Rect one.</summary>
    public static FrontRoomsDoorLite AttachMap(GameObject leafKit, FrontRoomsMapWorld.Door door, Transform root, Vector4 rect, FrontRoomsMapWorld map, Shape shape)
    {
        var lite = AttachMap(leafKit, door, root, rect, map);
        if (lite != null) lite.LiteShape = shape;
        return lite;
    }

    /// <summary>A title stream leaf or the station skin (FrontRoomsDoubleDoorKit). Stream lites start DARK. Wired glass, a Rect.</summary>
    public static FrontRoomsDoorLite AttachStream(GameObject owner, Renderer glass, Transform root, Vector4 rect, bool dead)
    {
        return AttachStream(owner, glass, root, rect, dead, Glazing.Wired, Shape.Rect);
    }

    /// <summary>As above with the lite's glazing and shape (the zone doors' pool: Tempered, Round). Re-callable on the same glass
    /// (a re-dress): the component is kept, its glass list refilled, Ready kept; a glazing change gives back a borrowed fade
    /// material to its own pool first.</summary>
    public static FrontRoomsDoorLite AttachStream(GameObject owner, Renderer glass, Transform root, Vector4 rect, bool dead, Glazing glazing, Shape shape)
    {
        if (owner == null) return null;
        var lite = owner.GetComponent<FrontRoomsDoorLite>();
        if (lite == null) lite = owner.AddComponent<FrontRoomsDoorLite>();
        if (lite.LiteGlazing != glazing)
        {
            lite.ReturnBorrowed();
            lite.LiteGlazing = glazing;
        }
        lite.LiteShape = shape;
        lite.LiteKind = dead ? Kind.StationSkin : Kind.StreamDoor;
        lite.Dead = dead;
        lite.Root = root;
        lite.Rect = rect;
        lite.glassRenderers.Clear();
        lite.glassSlots.Clear();
        if (glass != null) { lite.glassRenderers.Add(glass); lite.glassSlots.Add(0); }
        lite.InitState();
        return lite;
    }

    /// <summary>A dead lite in a kit (the sealed EXIT leaf): DARK for good, with no door behind it.</summary>
    public static FrontRoomsDoorLite AttachDead(GameObject kit, Transform root, Vector4 rect)
    {
        if (kit == null) return null;
        var lite = kit.GetComponent<FrontRoomsDoorLite>();
        if (lite == null) lite = kit.AddComponent<FrontRoomsDoorLite>();
        lite.LiteKind = Kind.StationSkin;
        lite.Dead = true;
        lite.Root = root;
        lite.Rect = rect;
        lite.CollectGlass(kit);
        lite.InitState();
        return lite;
    }

    /// <summary>
    /// The door break swapped the leaf for its pre-fractured body (<paramref name="breakBody"/>, whose body_L* carry the same
    /// glass): LIVE / DARK now drives those renderers, though this component's own object is off. Null gives the glass back
    /// to the intact leaf. The sight rule keeps working: it asks the door's collider and the rect, not the leaf.
    /// </summary>
    public void AdoptBreakBody(GameObject breakBody)
    {
        ReturnBorrowed();
        PeekWeight = 0f;
        adopted = breakBody != null;
        CollectGlass(breakBody != null ? breakBody : gameObject);
        applied = null;
        Apply();
    }

    void CollectGlass(GameObject leafKit)
    {
        glassRenderers.Clear();
        glassSlots.Clear();
        foreach (var r in leafKit.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            for (var i = 0; i < mats.Length; i++)
                if (mats[i] != null && (mats[i].name == LiveMaterialName || mats[i].name == DarkMaterialName))
                {
                    glassRenderers.Add(r);
                    glassSlots.Add(i);
                    // the glass casts no shadow (FrontRooms/Glass has no caster pass anyway); the frame's slot does
                }
                else if (mats[i] != null && (mats[i].name == TemperedLiveName || mats[i].name == TemperedDarkName))
                {
                    // a kit glazed with clear tempered glass (Build B: the map's pool leaf, when the Map stage builds it)
                    glassRenderers.Add(r);
                    glassSlots.Add(i);
                    if (LiteGlazing != Glazing.Tempered) { ReturnBorrowed(); LiteGlazing = Glazing.Tempered; }
                }
        }
    }

    void InitState()
    {
        EnsureShared();
        live = target = Dead ? 0f : (LiteKind == Kind.MapDoor ? (WantLive() ? 1f : 0f) : 0f);
        fadeClock = -1f;
        applied = null;
        Apply();
        if (!all.Contains(this)) all.Add(this);
        EnsureRunner();
    }

    /// <summary>The shut terminal door (CloseTerminalDoor, CONTRACT T-L1 SetLiteDead): DARK for good.</summary>
    public void SetDead()
    {
        Dead = true;
        target = 0f;
    }

    /// <summary>The peek on this lite: 0 none .. 1 the eye at the glass (reflection to 15 %, the wire soft).</summary>
    public void SetPeek(float w)
    {
        w = Mathf.Clamp01(w);
        if (Mathf.Abs(w - PeekWeight) < 1e-4f) return;
        PeekWeight = w;
        Apply();
    }

    /// <summary>The EXAMINE peek's Available (30_design 4.2): LIVE, the door shut, still and whole. Cached delegate.</summary>
    public Func<bool> PeekAvailableFunc => peekAvailable ?? (peekAvailable = PeekAvailable);

    public bool PeekAvailable()
    {
        if (Dead || !IsLive || !isActiveAndEnabled) return false;
        if (Door == null) return LiteKind == Kind.StreamDoor;
        return !Door.broken && !Door.open && Mathf.Abs(Door.angle) <= ShutDegrees && (map == null || !map.IsBeingBroken(Door));
    }

    void OnDisable() { if (!adopted) ReturnBorrowed(); }

    void OnEnable()
    {
        if (Root != null && !all.Contains(this)) all.Add(this);
    }

    void OnDestroy()
    {
        all.Remove(this);
        if (LeafCollider != null)
        {
            var id = FrontRoomsObjectId.Of(LeafCollider);
            if (byCollider.TryGetValue(id, out var l) && l == this) byCollider.Remove(id);
        }
        ReturnBorrowed();
    }

    // ------------------------------------------------------------------ sight (CONTRACT M-L2)
    /// <summary>
    /// The player-side sight rule: a ray that meets <paramref name="c"/> at <paramref name="hitPoint"/> passes through the
    /// glass when c is a lited leaf's collider, the point (in the door root) lies inside liteRect inset 10 mm and the door is
    /// not a dead door. An intact or cracked lite passes; the Relay's sight never asks.
    /// </summary>
    public static bool PassesPlayerSight(Collider c, Vector3 hitPoint)
    {
        if (c == null || byCollider.Count == 0) return false;
        if (!byCollider.TryGetValue(FrontRoomsObjectId.Of(c), out var lite) || lite == null || lite.Dead || lite.Root == null) return false;
        var p = lite.Root.InverseTransformPoint(hitPoint);
        var r = lite.Rect;
        if (lite.LiteShape == Shape.Round)
        {
            // a porthole: the circle inscribed in the rect, inset 10 mm
            var rad = Mathf.Min(r.y - r.x, r.w - r.z) * .5f - SightInset;
            float dz = p.z - (r.x + r.y) * .5f, dy = p.y - (r.z + r.w) * .5f;
            return rad > 0f && dz * dz + dy * dy <= rad * rad;
        }
        return p.z >= r.x + SightInset && p.z <= r.y - SightInset && p.y >= r.z + SightInset && p.y <= r.w - SightInset;
    }

    /// <summary>The lite whose leaf collider this is (or null).</summary>
    public static FrontRoomsDoorLite OfCollider(Collider c) =>
        c != null && byCollider.TryGetValue(FrontRoomsObjectId.Of(c), out var l) ? l : null;

    /// <summary>The glass plane's centre and its normal (+X of Root) in world space.</summary>
    public Pose GlassPose
    {
        get
        {
            var r = Rect;
            var c = Root != null ? Root.TransformPoint(new Vector3(0f, (r.z + r.w) * .5f, (r.x + r.y) * .5f)) : transform.position;
            var n = Root != null ? Root.TransformDirection(Vector3.right) : transform.right;
            return new Pose(c, Quaternion.LookRotation(n, Vector3.up));
        }
    }

    // ------------------------------------------------------------------ state
    bool WantLive()
    {
        if (Dead) return false;
        if (LiteKind == Kind.StreamDoor) return Ready != null && Ready();
        if (LiteKind == Kind.StationSkin) return false;
        if (ForceNotReady) return false;
        if (Door == null) return true;
        return MapDoorReady != null ? MapDoorReady(Door) : ChunksBuilt();
    }

    /// <summary>The stand-in for CONTRACT M-L4: the chunks on both sides of the door are built (not yet furnished or lit).</summary>
    bool ChunksBuilt() => map == null || (map.IsChunkBuiltAt(Door.a) && map.IsChunkBuiltAt(Door.b));

    void Step(float dt)
    {
        var want = WantLive() ? 1f : 0f;
        if (want != target)
        {
            target = want;
            fadeClock = 0f;
            // Fix 1 (test round, G8 memory): only a lite someone can see fades; one out of view (or never drawn yet: a lite
            // dressed with its chunk) snaps, so the pooled fade materials are bounded by the lites in view (v1 made 124 at
            // the run's first settle, every new chunk's lites fading at once).
            if (!AnyGlassVisible())
            {
                live = target;
                fadeClock = -1f;
                Apply();
                return;
            }
        }
        if (live != target)
        {
            live = Mathf.MoveTowards(live, target, dt / FadeSeconds);
            if (Mathf.Abs(live - target) < 1e-4f) live = target;   // 12 steps of 1/30 s land on 0.40 s, not a 13th for float error
            if (fadeClock >= 0f) fadeClock += dt;
            if (live == target && fadeClock >= 0f) { LastFadeSeconds = fadeClock; fadeClock = -1f; }
            Apply();
        }
    }

    bool AnyGlassVisible()
    {
        for (var i = 0; i < glassRenderers.Count; i++)
            if (glassRenderers[i] != null && glassRenderers[i].isVisible) return true;
        return false;
    }

    /// <summary>The DARK amount the glass shows now (sine in-out of the fade).</summary>
    public float DarkAmount => 1f - (.5f - .5f * Mathf.Cos(Mathf.PI * Mathf.Clamp01(live)));

    void Apply()
    {
        if (glassRenderers.Count == 0) return;
        Material want;
        var dark = DarkAmount;
        var tempered = LiteGlazing == Glazing.Tempered;
        if (tempered) EnsureTempered();
        var liveMat = tempered ? temperedLive : liveShared;
        var darkMat = tempered ? temperedDark : darkShared;
        if (PeekWeight <= 0f && dark <= 0f) want = liveMat;
        else if (PeekWeight <= 0f && dark >= 1f) want = darkMat;
        else
        {
            if (borrowed == null)
            {
                borrowed = Borrow(LiteGlazing, PeekWeight > 0f);
                borrowedGlazing = LiteGlazing;
            }
            if (borrowed == null)
            {
                // the pool is out: this fade snaps to its end (a snap in a crowd of fades reads as nothing)
                live = target;
                fadeClock = -1f;
                want = live >= 1f ? liveMat : darkMat;
            }
            else
            {
                borrowed.SetFloat(LiteDarkId, dark);
                borrowed.SetFloat(PeekReflectId, Mathf.Lerp(1f, PeekReflect, PeekWeight));
                borrowed.SetFloat(WireSoftId, Mathf.Lerp(1f, PeekWireSoftPx, PeekWeight));
                want = borrowed;
            }
        }
        if (want != borrowed) ReturnBorrowed();
        if (want == null || want == applied) return;
        applied = want;
        for (var i = 0; i < glassRenderers.Count; i++)
        {
            var r = glassRenderers[i];
            if (r == null) continue;
            var mats = r.sharedMaterials;
            var k = glassSlots[i];
            if (k < 0 || k >= mats.Length || mats[k] == want) continue;
            mats[k] = want;
            r.sharedMaterials = mats;
        }
    }

    static Material Borrow(Glazing glazing, bool must)
    {
        if (glazing == Glazing.Tempered)
        {
            // the tempered pool: its own FadePoolSize, made from its own live material (null without it: the fade snaps)
            if (temperedPool.Count > 0) return temperedPool.Pop();
            if ((temperedMade >= FadePoolSize && !must) || temperedLive == null) return null;
            temperedMade++;
            return new Material(temperedLive) { name = TemperedLiveName + " (fade " + temperedMade + ")", hideFlags = HideFlags.DontSave };
        }
        if (pool.Count > 0) return pool.Pop();
        if (poolMade >= FadePoolSize && !must) return null;
        poolMade++;
        return new Material(liveShared) { name = LiveMaterialName + " (fade " + poolMade + ")", hideFlags = HideFlags.DontSave };
    }

    void ReturnBorrowed()
    {
        if (borrowed == null) return;
        if (borrowedGlazing == Glazing.Tempered) temperedPool.Push(borrowed);
        else pool.Push(borrowed);
        borrowed = null;
    }

    static void EnsureShared()
    {
        if (liveShared == null) liveShared = FrontRoomsSurfaces.TryGet(LiveMaterialName);
        if (darkShared == null) darkShared = FrontRoomsSurfaces.TryGet(DarkMaterialName);
    }

    // loaded only when a tempered lite exists, so a build without one loads exactly what it loaded before
    static void EnsureTempered()
    {
        if (temperedLive == null) temperedLive = FrontRoomsSurfaces.TryGet(TemperedLiveName);
        if (temperedDark == null) temperedDark = FrontRoomsSurfaces.TryGet(TemperedDarkName);
    }

    /// <summary>The shared materials (DoubleDoorKit builds its glass with them).</summary>
    public static Material LiveShared { get { EnsureShared(); return liveShared; } }
    public static Material DarkShared { get { EnsureShared(); return darkShared; } }

    /// <summary>The shared LIVE material of a glazing.</summary>
    public static Material LiveSharedOf(Glazing glazing)
    {
        if (glazing == Glazing.Tempered) { EnsureTempered(); return temperedLive; }
        return LiveShared;
    }

    /// <summary>The shared DARK material of a glazing.</summary>
    public static Material DarkSharedOf(Glazing glazing)
    {
        if (glazing == Glazing.Tempered) { EnsureTempered(); return temperedDark; }
        return DarkShared;
    }

    /// <summary>Tools and the harness: step every lite now (the runner does this each LateUpdate).</summary>
    public static void TickAll(float dt)
    {
        for (var i = all.Count - 1; i >= 0; i--)
        {
            var l = all[i];
            if (l == null) { all.RemoveAt(i); continue; }
            if (!l.isActiveAndEnabled && !l.adopted) continue;
            l.Step(dt);
        }
    }

    /// <summary>Counts for the harness: lites, LIVE, DARK, fading, pooled materials made.</summary>
    public static string Census()
    {
        int n = 0, lv = 0, dk = 0, fd = 0;
        for (var i = 0; i < all.Count; i++)
        {
            var l = all[i];
            if (l == null) continue;
            n++;
            if (l.live >= 1f) lv++; else if (l.live <= 0f) dk++; else fd++;
        }
        var s = "lites " + n + " live " + lv + " dark " + dk + " fading " + fd + " pool " + poolMade + " (free " + pool.Count + ")";
        return temperedMade > 0 ? s + " tempered pool " + temperedMade + " (free " + temperedPool.Count + ")" : s;
    }

    // ------------------------------------------------------------------ holed leaf meshes (30_design 1.4)
    static readonly Dictionary<Vector4, Mesh> holedShadows = new Dictionary<Vector4, Mesh>();
    static readonly Dictionary<Vector4, Mesh> holedUnits = new Dictionary<Vector4, Mesh>();

    /// <summary>
    /// The D1.6 leaf's shadows-only proxy with the lite's hole (about 32 triangles, one shared mesh per form): the box of
    /// FrontRoomsInteractableKit's cube proxy (X +-0.05, Y 0.015..2.095, Z 0.02..0.98) with a through-hole over the
    /// visible glass, so a lamp in the room behind throws a lit rectangle through the lite. Door-root metres.
    /// </summary>
    public static Mesh HoledShadowBox(Vector4 rect)
    {
        if (holedShadows.TryGetValue(rect, out var cached) && cached != null) return cached;
        var m = HoledBox(new Vector3(.10f, 2.08f, .96f), new Vector3(0f, 1.055f, .5f), rect, "Leaf shadow (holed, " + Mathf.RoundToInt((rect.y - rect.x) / .0254f) + " x " + Mathf.RoundToInt((rect.w - rect.z) / .0254f) + ")");
        holedShadows[rect] = m;
        return m;
    }

    /// <summary>
    /// The Map stage (zone doors, 2026-11-06): the proxy of a leaf with a second opening on the same axis (the back-of-house
    /// member's A2 louvre under its F1 lite: light passes the louvre's blades). Both rects (z0, z1, y0, y1), one shared mesh per
    /// pair. A pair that overlaps or does not share its z span falls back to the lite's one hole.
    /// </summary>
    public static Mesh HoledShadowBox(Vector4 rect, Vector4 rect2)
    {
        if (Mathf.Abs(rect.x - rect2.x) > 1e-4f || Mathf.Abs(rect.y - rect2.y) > 1e-4f || (rect2.w > rect.z && rect.w > rect2.z)) return HoledShadowBox(rect);
        var key = new Vector4(rect.x + rect.z * 10f, rect.y + rect.w * 10f, rect2.z, rect2.w);
        if (holedShadows2.TryGetValue(key, out var cached) && cached != null) return cached;
        var lo = rect.z < rect2.z ? rect : rect2;
        var hi = rect.z < rect2.z ? rect2 : rect;
        var m = HoledStack(new Vector3(.10f, 2.08f, .96f), new Vector3(0f, 1.055f, .5f), rect.x, rect.y, lo.z, lo.w, hi.z, hi.w,
            "Leaf shadow (holed, lite + louvre)");
        holedShadows2[key] = m;
        return m;
    }

    /// <summary>
    /// The Map stage: the proxy of a leaf with the A1 porthole (a 32-gon through-hole inscribed in the rect, the clear 16 in), so
    /// a lamp behind the door throws a round patch of light through it, not a square. One shared mesh per rect.
    /// </summary>
    public static Mesh HoledShadowBoxRound(Vector4 rect)
    {
        if (holedRound.TryGetValue(rect, out var cached) && cached != null) return cached;
        var m = HoledRound(new Vector3(.10f, 2.08f, .96f), new Vector3(0f, 1.055f, .5f), rect, 32, "Leaf shadow (holed, porthole)");
        holedRound[rect] = m;
        return m;
    }

    static readonly Dictionary<Vector4, Mesh> holedShadows2 = new Dictionary<Vector4, Mesh>();
    static readonly Dictionary<Vector4, Mesh> holedRound = new Dictionary<Vector4, Mesh>();

    // A box (size, centre) with two through-holes along X sharing the z span hz0..hz1 (y spans a, then b above it).
    static Mesh HoledStack(Vector3 size, Vector3 centre, float hz0, float hz1, float ay0, float ay1, float by0, float by1, string name)
    {
        float x = size.x * .5f, y0 = centre.y - size.y * .5f, y1 = centre.y + size.y * .5f, z0 = centre.z - size.z * .5f, z1 = centre.z + size.z * .5f;
        var v = new List<Vector3>();
        var t = new List<int>();
        foreach (var s in new[] { -1f, 1f })
        {
            var n = new Vector3(s, 0f, 0f);
            var X = s * x;
            Quad(v, t, new Vector3(X, y0, z0), new Vector3(X, y0, z1), new Vector3(X, ay0, z1), new Vector3(X, ay0, z0), n);
            Quad(v, t, new Vector3(X, ay1, z0), new Vector3(X, ay1, z1), new Vector3(X, by0, z1), new Vector3(X, by0, z0), n);
            Quad(v, t, new Vector3(X, by1, z0), new Vector3(X, by1, z1), new Vector3(X, y1, z1), new Vector3(X, y1, z0), n);
            foreach (var band in new[] { new Vector2(ay0, ay1), new Vector2(by0, by1) })
            {
                Quad(v, t, new Vector3(X, band.x, z0), new Vector3(X, band.x, hz0), new Vector3(X, band.y, hz0), new Vector3(X, band.y, z0), n);
                Quad(v, t, new Vector3(X, band.x, hz1), new Vector3(X, band.x, z1), new Vector3(X, band.y, z1), new Vector3(X, band.y, hz1), n);
            }
        }
        Quad(v, t, new Vector3(-x, y0, z0), new Vector3(x, y0, z0), new Vector3(x, y0, z1), new Vector3(-x, y0, z1), Vector3.down);
        Quad(v, t, new Vector3(-x, y1, z0), new Vector3(x, y1, z0), new Vector3(x, y1, z1), new Vector3(-x, y1, z1), Vector3.up);
        Quad(v, t, new Vector3(-x, y0, z0), new Vector3(x, y0, z0), new Vector3(x, y1, z0), new Vector3(-x, y1, z0), Vector3.back);
        Quad(v, t, new Vector3(-x, y0, z1), new Vector3(x, y0, z1), new Vector3(x, y1, z1), new Vector3(-x, y1, z1), Vector3.forward);
        foreach (var band in new[] { new Vector2(ay0, ay1), new Vector2(by0, by1) })
        {
            Quad(v, t, new Vector3(-x, band.x, hz0), new Vector3(x, band.x, hz0), new Vector3(x, band.x, hz1), new Vector3(-x, band.x, hz1), Vector3.up);
            Quad(v, t, new Vector3(-x, band.y, hz0), new Vector3(x, band.y, hz0), new Vector3(x, band.y, hz1), new Vector3(-x, band.y, hz1), Vector3.down);
            Quad(v, t, new Vector3(-x, band.x, hz0), new Vector3(x, band.x, hz0), new Vector3(x, band.y, hz0), new Vector3(-x, band.y, hz0), Vector3.forward);
            Quad(v, t, new Vector3(-x, band.x, hz1), new Vector3(x, band.x, hz1), new Vector3(x, band.y, hz1), new Vector3(-x, band.y, hz1), Vector3.back);
        }
        return Finish(v, t, z0, z1, y0, y1, name);
    }

    // A box (size, centre) with a round through-hole along X inscribed in rect (z0, z1, y0, y1): each face is the ring between the
    // n-gon and the hole's bounding square (a quad per segment; n a multiple of 8 so the square's corners are ring vertices), then
    // the four quads round the square; the hole's wall faces in.
    static Mesh HoledRound(Vector3 size, Vector3 centre, Vector4 rect, int n, string name)
    {
        float x = size.x * .5f, y0 = centre.y - size.y * .5f, y1 = centre.y + size.y * .5f, z0 = centre.z - size.z * .5f, z1 = centre.z + size.z * .5f;
        float cz = (rect.x + rect.y) * .5f, cy = (rect.z + rect.w) * .5f, r = Mathf.Min(rect.y - rect.x, rect.w - rect.z) * .5f;
        float sz0 = cz - r, sz1 = cz + r, sy0 = cy - r, sy1 = cy + r;
        var v = new List<Vector3>();
        var t = new List<int>();
        foreach (var s in new[] { -1f, 1f })
        {
            var nrm = new Vector3(s, 0f, 0f);
            var X = s * x;
            Quad(v, t, new Vector3(X, y0, z0), new Vector3(X, y0, z1), new Vector3(X, sy0, z1), new Vector3(X, sy0, z0), nrm);
            Quad(v, t, new Vector3(X, sy1, z0), new Vector3(X, sy1, z1), new Vector3(X, y1, z1), new Vector3(X, y1, z0), nrm);
            Quad(v, t, new Vector3(X, sy0, z0), new Vector3(X, sy0, sz0), new Vector3(X, sy1, sz0), new Vector3(X, sy1, z0), nrm);
            Quad(v, t, new Vector3(X, sy0, sz1), new Vector3(X, sy0, z1), new Vector3(X, sy1, z1), new Vector3(X, sy1, sz1), nrm);
            for (var i = 0; i < n; i++)
            {
                float a0 = 2f * Mathf.PI * i / n, a1 = 2f * Mathf.PI * (i + 1) / n;
                var c0 = new Vector3(X, cy + r * Mathf.Sin(a0), cz + r * Mathf.Cos(a0));
                var c1 = new Vector3(X, cy + r * Mathf.Sin(a1), cz + r * Mathf.Cos(a1));
                Quad(v, t, c0, c1, SquarePoint(X, cz, cy, r, a1), SquarePoint(X, cz, cy, r, a0), nrm);
            }
        }
        Quad(v, t, new Vector3(-x, y0, z0), new Vector3(x, y0, z0), new Vector3(x, y0, z1), new Vector3(-x, y0, z1), Vector3.down);
        Quad(v, t, new Vector3(-x, y1, z0), new Vector3(x, y1, z0), new Vector3(x, y1, z1), new Vector3(-x, y1, z1), Vector3.up);
        Quad(v, t, new Vector3(-x, y0, z0), new Vector3(x, y0, z0), new Vector3(x, y1, z0), new Vector3(-x, y1, z0), Vector3.back);
        Quad(v, t, new Vector3(-x, y0, z1), new Vector3(x, y0, z1), new Vector3(x, y1, z1), new Vector3(-x, y1, z1), Vector3.forward);
        for (var i = 0; i < n; i++)
        {
            float a0 = 2f * Mathf.PI * i / n, a1 = 2f * Mathf.PI * (i + 1) / n, am = (a0 + a1) * .5f;
            Quad(v, t, new Vector3(-x, cy + r * Mathf.Sin(a0), cz + r * Mathf.Cos(a0)), new Vector3(x, cy + r * Mathf.Sin(a0), cz + r * Mathf.Cos(a0)),
                new Vector3(x, cy + r * Mathf.Sin(a1), cz + r * Mathf.Cos(a1)), new Vector3(-x, cy + r * Mathf.Sin(a1), cz + r * Mathf.Cos(a1)),
                new Vector3(0f, -Mathf.Sin(am), -Mathf.Cos(am)));
        }
        return Finish(v, t, z0, z1, y0, y1, name);
    }

    // The point where the ray from the hole's centre at angle a meets the hole's bounding square (half side r), in the face X.
    static Vector3 SquarePoint(float X, float cz, float cy, float r, float a)
    {
        float dz = Mathf.Cos(a), dy = Mathf.Sin(a);
        var k = r / Mathf.Max(Mathf.Abs(dz), Mathf.Abs(dy));
        return new Vector3(X, cy + dy * k, cz + dz * k);
    }

    static Mesh Finish(List<Vector3> v, List<int> t, float z0, float z1, float y0, float y1, string name)
    {
        var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
        mesh.SetVertices(v);
        mesh.SetTriangles(t, 0);
        var uv = new List<Vector2>(v.Count);
        for (var i = 0; i < v.Count; i++) uv.Add(new Vector2((v[i].z - z0) / (z1 - z0), (v[i].y - y0) / (y1 - y0)));
        mesh.SetUVs(0, uv);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>
    /// The cube leaf of a map door (a scaled unit cube: the C22 EXIT leaf, its sealed twin) with a through-hole, one shared mesh
    /// per hole. <paramref name="hole"/> is (z0, z1, y0, y1) in the cube's own unit space. Render-only: the door's collider
    /// is a box of its own and stays solid (the Relay's sight stops at it).
    /// </summary>
    public static Mesh HoledUnitLeaf(Vector4 hole)
    {
        if (holedUnits.TryGetValue(hole, out var cached) && cached != null) return cached;
        var m = HoledBox(Vector3.one, Vector3.zero, hole, "Door leaf (holed)");
        m.RecalculateTangents();
        holedUnits[hole] = m;
        return m;
    }

    // A box (size, centre) with a through-hole along X over (z0, z1, y0, y1).
    static Mesh HoledBox(Vector3 size, Vector3 centre, Vector4 rect, string name)
    {
        float x = size.x * .5f, y0 = centre.y - size.y * .5f, y1 = centre.y + size.y * .5f, z0 = centre.z - size.z * .5f, z1 = centre.z + size.z * .5f;
        float hz0 = rect.x, hz1 = rect.y, hy0 = rect.z, hy1 = rect.w;
        var v = new List<Vector3>();
        var t = new List<int>();
        foreach (var s in new[] { -1f, 1f })
        {
            var n = new Vector3(s, 0f, 0f);
            var X = s * x;
            Quad(v, t, new Vector3(X, y0, z0), new Vector3(X, y0, z1), new Vector3(X, hy0, z1), new Vector3(X, hy0, z0), n);
            Quad(v, t, new Vector3(X, hy1, z0), new Vector3(X, hy1, z1), new Vector3(X, y1, z1), new Vector3(X, y1, z0), n);
            Quad(v, t, new Vector3(X, hy0, z0), new Vector3(X, hy0, hz0), new Vector3(X, hy1, hz0), new Vector3(X, hy1, z0), n);
            Quad(v, t, new Vector3(X, hy0, hz1), new Vector3(X, hy0, z1), new Vector3(X, hy1, z1), new Vector3(X, hy1, hz1), n);
        }
        Quad(v, t, new Vector3(-x, y0, z0), new Vector3(x, y0, z0), new Vector3(x, y0, z1), new Vector3(-x, y0, z1), Vector3.down);
        Quad(v, t, new Vector3(-x, y1, z0), new Vector3(x, y1, z0), new Vector3(x, y1, z1), new Vector3(-x, y1, z1), Vector3.up);
        Quad(v, t, new Vector3(-x, y0, z0), new Vector3(x, y0, z0), new Vector3(x, y1, z0), new Vector3(-x, y1, z0), Vector3.back);
        Quad(v, t, new Vector3(-x, y0, z1), new Vector3(x, y0, z1), new Vector3(x, y1, z1), new Vector3(-x, y1, z1), Vector3.forward);
        // the hole's reveals face into the hole
        Quad(v, t, new Vector3(-x, hy0, hz0), new Vector3(x, hy0, hz0), new Vector3(x, hy0, hz1), new Vector3(-x, hy0, hz1), Vector3.up);
        Quad(v, t, new Vector3(-x, hy1, hz0), new Vector3(x, hy1, hz0), new Vector3(x, hy1, hz1), new Vector3(-x, hy1, hz1), Vector3.down);
        Quad(v, t, new Vector3(-x, hy0, hz0), new Vector3(x, hy0, hz0), new Vector3(x, hy1, hz0), new Vector3(-x, hy1, hz0), Vector3.forward);
        Quad(v, t, new Vector3(-x, hy0, hz1), new Vector3(x, hy0, hz1), new Vector3(x, hy1, hz1), new Vector3(-x, hy1, hz1), Vector3.back);
        var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
        mesh.SetVertices(v);
        mesh.SetTriangles(t, 0);
        var uv = new List<Vector2>(v.Count);
        for (var i = 0; i < v.Count; i++) uv.Add(new Vector2((v[i].z - z0) / (z1 - z0), (v[i].y - y0) / (y1 - y0)));
        mesh.SetUVs(0, uv);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // A quad wound to face 'facing' (Unity: clockwise from the front; cross(b - a, c - a) along the facing).
    static void Quad(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 facing)
    {
        var i = v.Count;
        v.Add(a); v.Add(b); v.Add(c); v.Add(d);
        var flip = Vector3.Dot(Vector3.Cross(b - a, c - a), facing) < 0f;
        if (!flip) { t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3); }
        else { t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i); t.Add(i + 3); t.Add(i + 2); }
    }

    // ------------------------------------------------------------------ runner
    static void EnsureRunner()
    {
        if (runner != null || !Application.isPlaying) return;
        var go = new GameObject("FrontRoomsDoorLite runner") { hideFlags = HideFlags.HideAndDontSave };
        DontDestroyOnLoad(go);
        runner = go.AddComponent<Runner>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        all.Clear();
        byCollider.Clear();
        pool.Clear();
        poolMade = 0;
        ForceNotReady = false;
        MapDoorReady = null;
        liveShared = darkShared = null;
        temperedPool.Clear();
        temperedMade = 0;
        temperedLive = temperedDark = null;
        holedShadows.Clear();
        holedUnits.Clear();
        holedShadows2.Clear();
        holedRound.Clear();
    }

    [AddComponentMenu("")]
    sealed class Runner : MonoBehaviour
    {
        void LateUpdate() => TickAll(Time.deltaTime);

        void OnDestroy()
        {
            if (runner == this) runner = null;
        }
    }
}
