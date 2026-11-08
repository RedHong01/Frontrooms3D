using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Zone doors (游戏视觉, zone doors Build A, 2026-11-06; Documentation/research/double_doors/20_design.md Part A, 30_build.md).
/// Red, 2026-11-05, this line's chat: 19:17:06 "同时我需要游戏视觉显根据当年做调研，去翻新每一个区域的双开门模型，规格和结构上的要求和之前
/// 一样"; 19:35:21 "目前level 0的门不需要改变，我是需要你改变办公室，车站，后场，run！，泳池等的门"; 21:25:37 (D-1 = option 2)
/// "这里改成选项 2：整扇门都换成区域的门。* 玩家在 Level 0 房间里走向下一扇门时，会先看到办公室的门，算是一点预告。"; 21:15:15 "按默认做"
/// (D-3 station D's face, D-4 the Office leaf stays walnut G).
/// The same pair (the binding spec of the landed kit: 2.4 x 2.66 m, 1-3/4 in leaves on three DA spring hinges each, 3 mm gaps,
/// brush strips, HW-DA1, no latch) wears a ZONE LOOK, one look for the whole door, both faces alike:
///   Level0       today's walnut pair, byte for byte (every Level 0 room's door, the start door);
///   Office       O-1: the F4 dark-bronze pressed-steel frame (Kit_DoorFrame_SteelPair_DA), walnut G leaves with the Office F1
///                10 x 30 in lite centred in a walnut bead, satin chrome hinges, HW-DA1 in 630, the office wear layer;
///   NightPost    S-2 (the title's waiting room, a B-floor night post): the frame kit, today's walnut leaf and F4b lite, 630
///                hinges, stainless casing guards on the night post's face only;
///   BackOfHouse  B-2 (the title's LOW stock room; the D-2 map member's language): the frame kit, painted hollow-metal leaves in
///                a warm service grey with edge seams, the A2 lite (F1 pressed-steel kit) and the F12 sight-proof louvre over
///                the 10 in kick plate, 630 hinges, the service wear layer;
///   Run          C-1: the frame kit, almond hollow-metal leaves (the SteelRun finish) with edge seams, the F5 15 x 15 in
///                pressed-steel lite centred, 630 hinges, the cart wear layer;
///   Pool         Part B (zone doors Build B, 2026-11-06; 20_design.md Part B; D-5 kept by "按默认做": research, three options +
///                TODAY, the recommended one built): the title Pool and Dark pool pairs (the Club when it is built). PD-A, the
///                moulded FRP pair: white gel-coat leaves with moulded r 3 mm arrises and a faint parting seam (LeafMeshRound),
///                the A1 16 in porthole centred on each leaf in a moulded FRP ring on both faces (Kit_DoorLite_A1_FRP) with
///                1/4 in clear tempered glass (FrontRoomsDoorLite.Glazing.Tempered, Shape.Round), the one-piece moulded FRP frame
///                in the course aqua with a fluted 630 stainless saddle (Kit_DoorFrame_FRPPair_DA, Part A's envelope), 630
///                hinges, HW-DA1 as it is, the pool's level wear (DoorWear_Pool). PoolDoorFinish draws the sheet's other two
///                options on the parts that exist (PD-B clear-anodised aluminium, PD-C aqua epoxy steel): set before a title is
///                built, as Today. The title shows the pool's doors only with PoolDoorsLive (false: photosafety, see there).
/// Station D (D-3) is not a zone: the start door is a Level0 pair whose +Z face takes the station's own language
/// (SetStationFace: a 38 mm pressed-steel casing over a bronze lining cap, stainless casing guards, 630 barrels), on both
/// builders: the stream's terminal pair (at MapRunStarted, shut and held) and StationDress's static entrance.
/// How a look is worn, with no visible pop and no allocation in a recycle: the stream builds today's pair and, under the same
/// pivots, carriers and slabs, every zone part its programme needs, switched off (Prepare); a re-dress (Dress) switches parts,
/// swaps the slabs' shared meshes and materials and the hinges' material, and re-aims a lite whose form changes
/// (FrontRoomsDoorLite.AttachStream; it keeps Ready). Pivots and their names ("double door left / right hinge": 声音设计 reads
/// them), carriers, colliders, the sound components, the swing, PlaceLeaf / SetHingeSide and every reader of them stay. Every
/// new part and kit name avoids "floor", "carpet", "ceiling", "wall", "seal", "partition" and "threshold marker"
/// (FrontRoomsRoomStream.RefreshRoomMaterials paints those with the room's finish).
/// Today = true gives today's door everywhere (no zone look, no station face, today's trim stops); WebGL players always keep
/// today (rule 5: the WebGL track).
/// </summary>
public static partial class FrontRoomsDoubleDoorKit
{
    /// <summary>The look a pair wears (append-only: the title families map onto it in FrontRoomsRoomStream.TitleRooms).</summary>
    public enum Zone : byte { Level0, Office, NightPost, BackOfHouse, Run, Pool }

    /// <summary>The leaf's material for the sound line's contract (data only; W/zone_doors/contracts/sound.md, option (b)).</summary>
    public enum DoorSound : byte { Wood, Steel, Composite }

    /// <summary>The pool door's finish (D-5, append-only): Frp = PD-A, the moulded FRP pair (REC, built); Aluminium = PD-B and
    /// Epoxy = PD-C, drawn for the sheet on the parts that exist (20_design.md B4.4: Part A's steel frame kit and lite kit, the
    /// round leaf, the A1 ring kit, with their slots swapped).</summary>
    public enum PoolFinish : byte { Frp, Aluminium, Epoxy }

    /// <summary>The pool door's finish. Set before a title is built, as Today (a pair keeps the finish it was prepared with).</summary>
    public static PoolFinish PoolDoorFinish = PoolFinish.Frp;

    /// <summary>
    /// The pool's doors live in the title (Build B, 2026-11-06). False until the title's lamp strike is bounded for photosafety:
    /// the room past a door strikes its ballasts as the camera walks through (FrontRoomsRoomStream.ConfigureLightProfile: 0-5
    /// flickers of 0.13-0.34 s between output 0.92 and 0.035), and PD-A's white leaves, lit by those strikes at arm's length,
    /// take the Tour's walk through door 8 to 3.5 flashes in a second (Tools/lookdev/flash_check.py, limit 3; TODAY 2.5, PD-B
    /// and PD-C 3.0; research/double_doors/30_build.md section B7). While false the pool's doors wear Level0 and its band keeps
    /// today's stop: Build A's state, byte for byte. Set before a title is built (the door lab and the renders set it true).
    /// </summary>
    public static bool PoolDoorsLive;

#if UNITY_WEBGL && !UNITY_EDITOR
    /// <summary>WebGL players keep today's door (rule 5).</summary>
    public static bool Today { get { return true; } set { } }
#else
    /// <summary>True: today's walnut pair everywhere, byte for byte (no zone look, no station face, today's trim stops). Set
    /// before a title or a station is built, as FrontRoomsSurfaceDetail.Today and FrontRoomsRoomStream.Programme.</summary>
    public static bool Today { get; set; }
#endif

    /// <summary>The F4 frame kit's face edge and head top (Kit_DoorFrame_SteelPair_DA's sidecar casingOuter / casingTop).</summary>
    public const float ZoneCasingOuter = 1.2628f, ZoneCasingTop = 2.7228f;
    /// <summary>Where TitleVignettes' end trims stopped before the zone doors (they counted the 19 mm lining twice: 20 mm short
    /// of today's walnut casing, 1.250 / 2.710; kept as it is for Level 0, research §O3).</summary>
    public const float TodayTrimHalf = 1.27f, TodayTrimTop = 2.73f;
    /// <summary>The end trims' stop for a room whose doors wear the frame kit (today's under Today).</summary>
    public static float TrimStopHalf => Today ? TodayTrimHalf : ZoneCasingOuter;
    public static float TrimStopTop => Today ? TodayTrimTop : ZoneCasingTop;
    /// <summary>The same for one zone: today's for Level0 and for a zone whose face this build does not carry (ZoneBuilt). Every
    /// whole-door zone, the pool's three finishes included, shares Part A's envelope.</summary>
    public static float TrimStopHalfFor(Zone zone) => Today || zone == Zone.Level0 || !ZoneBuilt(zone) ? TodayTrimHalf : ZoneCasingOuter;
    public static float TrimStopTopFor(Zone zone) => Today || zone == Zone.Level0 || !ZoneBuilt(zone) ? TodayTrimTop : ZoneCasingTop;

    /// <summary>The zones whose look this build carries and shows (Pool since Build B, while PoolDoorsLive).</summary>
    public static bool ZoneBuilt(Zone zone)
    {
        switch (zone)
        {
            case Zone.Level0:
            case Zone.Office:
            case Zone.NightPost:
            case Zone.BackOfHouse:
            case Zone.Run:
                return true;
            case Zone.Pool:
                return PoolDoorsLive;
            default:
                return false;
        }
    }

    /// <summary>The leaf's material class, for 声音设计's contract (inert: nothing reads it until Red picks option (b)). The pool:
    /// Composite for PD-A's FRP leaf, Steel for PD-B / PD-C.</summary>
    public static DoorSound SoundOf(Zone zone)
    {
        switch (zone)
        {
            case Zone.BackOfHouse:
            case Zone.Run:
                return DoorSound.Steel;
            case Zone.Pool:
                return PoolDoorFinish == PoolFinish.Frp ? DoorSound.Composite : DoorSound.Steel;
            default:
                return DoorSound.Wood;
        }
    }

    // ------------------------------------------------------------------ the zone parts (kits: Tools/Blender/frontrooms_kit/assets/door_*.py)
    public const string ZoneFrameKit = "Kit_DoorFrame_SteelPair_DA", StationCasingKit = "Kit_DoorCasing_StationPair";
    public const string Guard38Kit = "Kit_DoorGuard_Casing38", Guard51Kit = "Kit_DoorGuard_Casing51";
    public const string LiteF1Kit = "Kit_DoorLite_F1_Steel", LiteF5Kit = "Kit_DoorLite_F5_Steel", LouvreKit = "Kit_DoorLouvre_F12";
    const string KitPaintSlot = "Painted_Metal";
    const string ChromeSatin = "Prop_ChromeSatin", Stainless630 = "Prop_Stainless630";
    const string BackOfHousePaint = "Door_BackOfHouse", RunPaint = "Prop_SteelAlmond";
    // the pool (Build B): PD-A's frame and ring kits, the gel coat, and PD-B / PD-C's swap materials (20_design.md B4.4, B8)
    public const string PoolFrameKit = "Kit_DoorFrame_FRPPair_DA", LiteA1Kit = "Kit_DoorLite_A1_FRP";
    const string PoolGelcoat = "Door_PoolGelcoat", PoolAluminium = "Door_PoolAluminium", PoolEpoxy = "Door_PoolEpoxy", SteelFrameSlot = "Prop_SteelBrown";
    /// <summary>The A1 porthole (door_vision 50 section 2.3, built from its numbers): 16 in clear (201 sq in), centred on each
    /// leaf at 55 in; the slab's cut-out 425.4 mm, the 1/4 in clear tempered pane 423.4 mm (1 mm short of the cut-out), 32 sides
    /// both; the ring kits (444.4 mm) cover the cut-out's edge on both faces.</summary>
    public const float PortholeClear = .2032f, PortholeCut = .2127f, PortholeGlass = .2117f, PortholeGlassThickness = .006f, PortholeCentreY = 1.397f;
    const int PortholeSides = 32;
    /// <summary>The casing's inner arris (the rise at 12 mm off the soffit plane, the face 7/16 in proud of the paper plane at
    /// |z| 0.022) and the guards' mid-height (E1's band 0.10-1.32 m): the guard kits' origin.</summary>
    const float RiseX = Width * .5f + .012f, FaceZ = .022f + .0111f, GuardMidY = (.10f + 1.32f) * .5f;

    // Door vision set A on the title pair (door_vision 50 / 60; Red 2026-10-30 "A 按区域四种"): the visible glass or louvre,
    // centred on each leaf, x0, x1 across the leaf from its centre, y0, y1 above the floor.
    static readonly Vector4 F1Floor = new Vector4(-.127f, .127f, 1.143f, 1.905f);       // 10 x 30 in, bottom 45 in
    static readonly Vector4 F5Floor = new Vector4(-.1905f, .1905f, 1.1938f, 1.5748f);   // 15 x 15 in, bottom 47 in
    static readonly Vector4 F12Floor = new Vector4(-.127f, .127f, .3302f, .7366f);      // the louvre, 10 x 16 in, 36 mm over the kick plate
    // the pool's A1 porthole: the clear circle's square (centre 55 in; clear bottom 47 in)
    static readonly Vector4 A1Floor = new Vector4(-PortholeClear, PortholeClear, PortholeCentreY - PortholeClear, PortholeCentreY + PortholeClear);

    /// <summary>A floor rect in the slab's own frame (the slab is centred on the leaf).</summary>
    static Vector4 SlabRect(Vector4 floor)
    {
        var cy = LeafBottom + LeafHeight * .5f;
        return new Vector4(floor.x, floor.y, floor.z - cy, floor.w - cy);
    }

    static Vector3 SlabCentre(Vector4 floor)
    {
        var cy = LeafBottom + LeafHeight * .5f;
        return new Vector3((floor.x + floor.y) * .5f, (floor.z + floor.w) * .5f - cy, 0f);
    }

    // ------------------------------------------------------------------ one built pair
    /// <summary>One built pair and everything a look changes on it. The caller keeps it (FrontRoomsRoomStream's RoomSlot.door,
    /// FrontRoomsStationDress).</summary>
    public sealed class Pair
    {
        public Faces Faces { get; private set; }
        public Zone Zone { get; internal set; }
        /// <summary>+1: the family room is on the door's +Z side (the door opens into it); -1: on its -Z side; 0: none.</summary>
        public int FamilySide { get; internal set; }
        public bool StationFace { get; internal set; }
        public Transform Frame { get; private set; }
        public Transform Pivot(int hand) => pivots[hand < 0 ? 0 : 1];
        public Transform Carrier(int hand) => carriers[hand < 0 ? 0 : 1];
        public Transform HingeBody(int hand) => hingeBodies[hand < 0 ? 0 : 1];
        public GameObject Slab(int hand) => slabs[hand < 0 ? 0 : 1];

        internal readonly Transform[] pivots = new Transform[2], carriers = new Transform[2], hingeBodies = new Transform[2];
        internal readonly GameObject[] slabs = new GameObject[2];
        internal Materials materials;
        // today's parts and what they wear
        internal GameObject todayFrame, todaySaddle;
        internal MeshRenderer hingeJamb;
        internal readonly MeshRenderer[] leafHinges = new MeshRenderer[2], bodyHinges = new MeshRenderer[2];
        internal readonly MeshFilter[] slabFilters = new MeshFilter[2];
        internal readonly MeshRenderer[] slabRenderers = new MeshRenderer[2];
        internal readonly Mesh[] todaySlab = new Mesh[2], todayGlass = new Mesh[2];
        internal Material todayLeaf, todayHardware;
        internal readonly GameObject[] glass = new GameObject[2];
        internal readonly MeshFilter[] glassFilters = new MeshFilter[2];
        internal readonly MeshRenderer[] glassRenderers = new MeshRenderer[2];
        internal readonly int[] liteForm = new int[2];   // 0 today's F4b, 1 F1, 2 F5, 3 the pool's A1 (tempered, round), 4 F1 tempered (PD-B)
        // the zone parts (built once, switched off; Prepare)
        internal int prepared, failed;
        internal GameObject zoneFrame;
        internal readonly GameObject[] liteF1 = new GameObject[2], liteF5 = new GameObject[2], louvre = new GameObject[2];
        // the pool's parts (Build B): its frame (PD-A's FRP kit, or for PD-B / PD-C a steel frame kit instance of its own with its slot
        // swapped), the A1 ring kits (PD-A, PD-C) or F1 lite kits (PD-B), and the finish they were built with
        internal GameObject poolFrame;
        internal readonly GameObject[] liteA1 = new GameObject[2], liteF1Pool = new GameObject[2];
        internal PoolFinish poolFinish;
        internal readonly GameObject[] guards51 = new GameObject[4];   // [face (0 -Z, 1 +Z) * 2 + jamb (0 -x, 1 +x)]
        // station D's face (SetStationFace)
        internal GameObject stationWalnut, stationCasing;
        internal readonly GameObject[] guards38 = new GameObject[2];   // +Z, [jamb (0 -x, 1 +x)]

        internal Pair(Transform frame, Faces faces, Materials m)
        {
            Frame = frame;
            Faces = faces;
            materials = m;
            Zone = Zone.Level0;
        }
    }

    /// <summary>
    /// Today's pair, gathered: BuildFrame, then BuildLeaf for hand -1 and +1, exactly as the callers built it before (the same
    /// objects, names, components and order). Callers still call PlaceLeaf / SetHingeSide themselves.
    /// </summary>
    public static Pair BuildPair(Transform pivotParent, Transform frame, Materials m, Faces faces, bool colliders)
    {
        BuildFrame(frame, m, faces);
        var pair = new Pair(frame, faces, m);
        for (var k = 0; k < 2; k++)
        {
            Transform carrier, hingeBody;
            GameObject slab;
            pair.pivots[k] = BuildLeaf(pivotParent, frame, k == 0 ? -1 : 1, m, faces, colliders, out carrier, out hingeBody, out slab);
            pair.carriers[k] = carrier;
            pair.hingeBodies[k] = hingeBody;
            pair.slabs[k] = slab;
        }
        Gather(pair);
        return pair;
    }

    /// <summary>A direct child by its exact name. Transform.Find reads '/' as a path separator, and two of today's part names hold
    /// one ("door hinges / jamb halves", "left|right door hinge barrels / swing side": names 声音设计 reads, never renamed), so Find
    /// returned null for both and the zone hardware never reached the barrels (Fix A, Review A finding 1).</summary>
    static Transform ChildNamed(Transform parent, string name)
    {
        if (parent == null) return null;
        for (var i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == name) return child;
        }
        return null;
    }

    static void Gather(Pair pair)
    {
        var frame = pair.Frame;
        var lining = ChildNamed(frame, "door frame lining and casing");
        var saddle = ChildNamed(frame, "door saddle");
        var jamb = ChildNamed(frame, "door hinges / jamb halves");
        pair.todayFrame = lining != null ? lining.gameObject : null;
        pair.todaySaddle = saddle != null ? saddle.gameObject : null;
        pair.hingeJamb = jamb != null ? jamb.GetComponent<MeshRenderer>() : null;
        pair.todayHardware = pair.materials.hardware;
        pair.todayLeaf = pair.materials.leaf;
        for (var k = 0; k < 2; k++)
        {
            var side = k == 0 ? "left" : "right";
            var leafHinge = ChildNamed(pair.carriers[k], side + " door hinge leaves");
            var bodyHinge = ChildNamed(pair.hingeBodies[k], side + " door hinge barrels / swing side");
            pair.leafHinges[k] = leafHinge != null ? leafHinge.GetComponent<MeshRenderer>() : null;
            pair.bodyHinges[k] = bodyHinge != null ? bodyHinge.GetComponent<MeshRenderer>() : null;
            var slab = pair.slabs[k];
            if (slab == null) continue;
            pair.slabFilters[k] = slab.GetComponent<MeshFilter>();
            pair.slabRenderers[k] = slab.GetComponent<MeshRenderer>();
            pair.todaySlab[k] = pair.slabFilters[k] != null ? pair.slabFilters[k].sharedMesh : null;
            if (pair.slabRenderers[k] != null && pair.slabRenderers[k].sharedMaterial != null) pair.todayLeaf = pair.slabRenderers[k].sharedMaterial;
            var g = ChildNamed(slab.transform, pair.Faces == Faces.Both ? "lite glass (lobby)" : "lite dark skin (lobby)");
            if (g == null) continue;
            pair.glass[k] = g.gameObject;
            pair.glassFilters[k] = g.GetComponent<MeshFilter>();
            pair.glassRenderers[k] = g.GetComponent<MeshRenderer>();
            pair.todayGlass[k] = pair.glassFilters[k] != null ? pair.glassFilters[k].sharedMesh : null;
        }
    }

    // ------------------------------------------------------------------ prepare: the zone parts, built once and switched off
    /// <summary>Builds the parts of <paramref name="zone"/> on a stream pair once, switched off (Faces.Both only). No-op for
    /// Level0, under Today, for a zone this build does not carry, or when done. A kit that cannot be loaded leaves the zone on
    /// today's door (logged once).</summary>
    public static void Prepare(Pair pair, Zone zone)
    {
        if (pair == null || pair.Faces != Faces.Both || Today || zone == Zone.Level0 || !ZoneBuilt(zone)) return;
        var bit = 1 << (int)zone;
        if ((pair.prepared & bit) != 0) return;
        pair.prepared |= bit;
        // the zone slabs have cut-outs: without the lites (an Editor tool's Lites = false) they would show through. The pool has a
        // frame of its own (EnsurePoolParts); the other whole-door zones share Part A's steel frame kit.
        var ok = Lites && (zone == Zone.Pool || EnsureZoneFrame(pair));
        if (ok)
            switch (zone)
            {
                case Zone.Office:
                    SlabMeshFor(pair, zone, 0);
                    GlassMeshFor(1);
                    break;
                case Zone.NightPost:
                    ok = EnsureGuards51(pair);
                    break;
                case Zone.BackOfHouse:
                {
                    var paint = FrontRoomsSurfaces.TryGet(BackOfHousePaint) ?? FrontRoomsSurfaces.TryGet(KitPaintSlot);
                    for (var k = 0; k < 2 && ok; k++)
                    {
                        ok &= SpawnLeafKit(pair, k, LiteF1Kit, F1Floor, "door lite kit F1", KitPaintSlot, paint, pair.liteF1);
                        ok &= SpawnLeafKit(pair, k, LouvreKit, F12Floor, "door louvre F12", KitPaintSlot, paint, pair.louvre);
                    }
                    SlabMeshFor(pair, zone, 0);
                    GlassMeshFor(1);
                    break;
                }
                case Zone.Run:
                {
                    var paint = FrontRoomsSurfaces.TryGet(RunPaint) ?? FrontRoomsSurfaces.TryGet(KitPaintSlot);
                    for (var k = 0; k < 2 && ok; k++)
                        ok &= SpawnLeafKit(pair, k, LiteF5Kit, F5Floor, "door lite kit F5", KitPaintSlot, paint, pair.liteF5);
                    SlabMeshFor(pair, zone, 0);
                    GlassMeshFor(2);
                    break;
                }
                case Zone.Pool:
                    ok = EnsurePoolParts(pair);
                    break;
            }
        if (!ok)
        {
            pair.failed |= bit;
            if (warnedZones.Add(zone)) Debug.LogWarning("[FrontRooms3D] zone doors: the " + zone + " look's parts could not be built (a kit or Lites missing); that door keeps today's look");
        }
    }

    static readonly HashSet<Zone> warnedZones = new HashSet<Zone>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetZoneStatics() => warnedZones.Clear();

    static bool EnsureZoneFrame(Pair pair)
    {
        if (pair.zoneFrame != null) return true;
        pair.zoneFrame = FrontRoomsKitLibrary.Spawn(ZoneFrameKit, pair.Frame, Vector3.zero, Quaternion.identity, null, false, "door frame steel (zone kit)", false);
        if (pair.zoneFrame == null) return false;
        pair.zoneFrame.SetActive(false);
        return true;
    }

    static readonly Vector3[] Guard51At =
    {
        new Vector3(-RiseX, GuardMidY, -FaceZ), new Vector3(RiseX, GuardMidY, -FaceZ),
        new Vector3(-RiseX, GuardMidY, FaceZ), new Vector3(RiseX, GuardMidY, FaceZ),
    };

    // The guard kit is authored on the +x jamb's +Z face; the others turn it 180 degrees about y and / or z (its ends are
    // symmetric about its mid-height), never a negative scale.
    static readonly Vector3[] Guard51Turn = { new Vector3(0f, 180f, 0f), new Vector3(180f, 0f, 0f), new Vector3(0f, 0f, 180f), Vector3.zero };

    static bool EnsureGuards51(Pair pair)
    {
        for (var i = 0; i < 4; i++)
        {
            if (pair.guards51[i] != null) continue;
            var label = "door casing guard " + (i < 2 ? "-Z" : "+Z") + ((i & 1) == 0 ? " -x" : " +x");
            var g = FrontRoomsKitLibrary.Spawn(Guard51Kit, pair.Frame, Guard51At[i], Quaternion.Euler(Guard51Turn[i]), null, false, label, false);
            if (g == null) return false;
            Guarded(g);
            g.SetActive(false);
            pair.guards51[i] = g;
        }
        return true;
    }

    // A 1.5 mm guard casts no shadow worth its pass (as HW-DA1's plates); phones start at LOD1.
    static void Guarded(GameObject g)
    {
        foreach (var r in g.GetComponentsInChildren<MeshRenderer>(true))
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ReducedTier(g);
    }

    // reduced: phones and WebGL start the kit at LOD1 (ReducedTier), as every Part A leaf kit does. Fix B: the pool's A1 ring kits
    // pass false, so a phone keeps the 64-segment LOD0 within its 2 m switch (the 32-gon's corners showed at the 0.35 m hero:
    // +1,792 tris a ring kit, only within 2 m of a pool door) and takes LOD1 from 2 m as the LODGroup decides.
    static bool SpawnLeafKit(Pair pair, int k, string kit, Vector4 floorRect, string label, string slot, Material paint, GameObject[] into, bool reduced = true)
    {
        if (into[k] != null) return true;
        var slab = pair.slabs[k];
        if (slab == null) return false;
        var side = k == 0 ? "left " : "right ";
        var go = FrontRoomsKitLibrary.Spawn(kit, slab.transform, SlabCentre(floorRect), Quaternion.identity, null, false, side + label, false);
        if (go == null) return false;
        // the kit's frame (and its painted-over screw heads) take the leaf's own paint (as the D1.6 rows swap kit slots at spawn)
        SwapSlot(go, slot, paint);
        if (reduced) ReducedTier(go);
        go.SetActive(false);
        into[k] = go;
        return true;
    }

    /// <summary>Every renderer of a spawned kit: the slot <paramref name="slot"/> takes <paramref name="m"/> (null: no change).</summary>
    static void SwapSlot(GameObject go, string slot, Material m)
    {
        if (go == null || m == null) return;
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
        {
            var mats = r.sharedMaterials;
            var changed = false;
            for (var i = 0; i < mats.Length; i++)
                if (mats[i] != null && mats[i].name == slot) { mats[i] = m; changed = true; }
            if (changed) r.sharedMaterials = mats;
        }
    }

    /// <summary>
    /// The pool's parts on a stream pair, built once, switched off, in the finish of the moment (PoolDoorFinish): PD-A (Frp) the
    /// moulded FRP frame kit under "double door frame" and an A1 ring kit under each slab at the porthole's centre; PD-B
    /// (Aluminium) Part A's steel frame kit and F1 lite kits, their slots in clear-anodised aluminium; PD-C (Epoxy) Part A's steel
    /// frame kit and PD-A's ring kits, their slots in the aqua epoxy. The round slab and the porthole pane (PD-A, PD-C) or the
    /// F1 hollow-metal slab (PD-B) are cached code meshes. Names avoid RefreshRoomMaterials' words.
    /// </summary>
    static bool EnsurePoolParts(Pair pair)
    {
        if (pair.poolFrame != null) return true;
        var finish = PoolDoorFinish;
        var swap = finish == PoolFinish.Aluminium ? FrontRoomsSurfaces.TryGet(PoolAluminium) : finish == PoolFinish.Epoxy ? FrontRoomsSurfaces.TryGet(PoolEpoxy) : null;
        GameObject frame;
        if (finish == PoolFinish.Frp)
            frame = FrontRoomsKitLibrary.Spawn(PoolFrameKit, pair.Frame, Vector3.zero, Quaternion.identity, null, false, "door frame frp (zone kit)", false);
        else
        {
            frame = FrontRoomsKitLibrary.Spawn(ZoneFrameKit, pair.Frame, Vector3.zero, Quaternion.identity, null, false,
                finish == PoolFinish.Aluminium ? "door frame steel (zone kit, pool aluminium)" : "door frame steel (zone kit, pool epoxy)", false);
            SwapSlot(frame, SteelFrameSlot, swap);
        }
        if (frame == null) return false;
        frame.SetActive(false);
        pair.poolFrame = frame;
        pair.poolFinish = finish;
        var ok = true;
        for (var k = 0; k < 2 && ok; k++)
        {
            if (finish == PoolFinish.Aluminium)
                ok &= SpawnLeafKit(pair, k, LiteF1Kit, F1Floor, "door lite kit F1 (pool aluminium)", KitPaintSlot, swap, pair.liteF1Pool);
            else
                ok &= SpawnLeafKit(pair, k, LiteA1Kit, A1Floor, finish == PoolFinish.Frp ? "door lite kit A1 frp" : "door lite kit A1 (pool epoxy)", PoolGelcoat, swap, pair.liteA1, false);
        }
        PoolSlabMesh(finish);
        GlassMeshFor(finish == PoolFinish.Aluminium ? 4 : 3);
        return ok;
    }
}

public static partial class FrontRoomsDoubleDoorKit
{
    // ------------------------------------------------------------------ dress: the pair wears a look
    /// <summary>
    /// The stream pair wears <paramref name="zone"/> on both faces (Faces.Both only). <paramref name="familySide"/>: +1 the family
    /// room is on the +Z side (the door opens into it), -1 on the -Z side (the family room's exit door); one-sided parts (the
    /// night post's guards) go on that face. Parts on and off, the slabs' shared meshes and materials, the hinges' material,
    /// a lite whose form changes re-aimed. Call only while the pair cannot be seen (a sealed slot, a shut and held terminal door).
    /// Under Today, for a zone this build does not carry, or when a zone's parts failed: Level0.
    /// </summary>
    public static void Dress(Pair pair, Zone zone, int familySide)
    {
        if (pair == null || pair.Faces != Faces.Both) return;
        zone = Wearable(pair, zone);
        if (zone != Zone.Level0) Prepare(pair, zone);
        zone = Wearable(pair, zone);
        if (zone == Zone.Level0) familySide = 0;
        if (zone == pair.Zone && familySide == pair.FamilySide) return;
        if (pair.StationFace) SetStationFace(pair, false);
        var whole = zone != Zone.Level0;
        var pool = zone == Zone.Pool;
        SetActive(pair.todayFrame, !whole);
        SetActive(pair.todaySaddle, !whole);
        SetActive(pair.zoneFrame, whole && !pool);
        SetActive(pair.poolFrame, pool);
        var hardware = HingeMaterial(pair, zone);
        if (pair.hingeJamb != null) pair.hingeJamb.sharedMaterial = hardware;
        for (var k = 0; k < 2; k++)
        {
            if (pair.leafHinges[k] != null) pair.leafHinges[k].sharedMaterial = hardware;
            if (pair.bodyHinges[k] != null) pair.bodyHinges[k].sharedMaterial = hardware;
            var mesh = SlabMeshFor(pair, zone, k);
            if (pair.slabFilters[k] != null && mesh != null) pair.slabFilters[k].sharedMesh = mesh;
            if (pair.slabRenderers[k] != null) pair.slabRenderers[k].sharedMaterial = LeafMaterial(pair, zone, k);
            SetActive(pair.liteF1[k], zone == Zone.BackOfHouse);
            SetActive(pair.louvre[k], zone == Zone.BackOfHouse);
            SetActive(pair.liteF5[k], zone == Zone.Run);
            SetActive(pair.liteA1[k], pool);
            SetActive(pair.liteF1Pool[k], pool);
            DressLite(pair, k, LiteFormOf(pair, zone));
        }
        for (var i = 0; i < 4; i++)
            SetActive(pair.guards51[i], zone == Zone.NightPost && (i < 2 ? familySide < 0 : familySide > 0));
        pair.Zone = zone;
        pair.FamilySide = familySide;
    }

    static Zone Wearable(Pair pair, Zone zone)
    {
        if (Today || !ZoneBuilt(zone)) return Zone.Level0;
        return (pair.failed & (1 << (int)zone)) != 0 ? Zone.Level0 : zone;
    }

    static void SetActive(GameObject go, bool on)
    {
        if (go != null && go.activeSelf != on) go.SetActive(on);
    }

    static int LiteFormOf(Pair pair, Zone zone)
    {
        switch (zone)
        {
            case Zone.Office:
            case Zone.BackOfHouse:
                return 1;
            case Zone.Run:
                return 2;
            case Zone.Pool:
                return pair.poolFinish == PoolFinish.Aluminium ? 4 : 3;
            default:
                return 0;
        }
    }

    static void DressLite(Pair pair, int k, int form)
    {
        var glass = pair.glass[k];
        if (glass == null || pair.glassFilters[k] == null || form == pair.liteForm[k]) return;
        pair.glassFilters[k].sharedMesh = form == 0 ? pair.todayGlass[k] : GlassMeshFor(form);
        var rect = form == 0 ? LiteVisible() : SlabRect(form == 1 || form == 4 ? F1Floor : form == 2 ? F5Floor : A1Floor);
        // the same component, its glass list refilled, Ready kept; DARK until the stream says the room behind is connected. The
        // pool's lites are clear tempered glass (Glass_Tempered / _TemperedDark), the porthole's sight test a circle.
        if (form >= 3)
            FrontRoomsDoorLite.AttachStream(glass, pair.glassRenderers[k], pair.slabs[k].transform, rect, false, FrontRoomsDoorLite.Glazing.Tempered,
                form == 3 ? FrontRoomsDoorLite.Shape.Round : FrontRoomsDoorLite.Shape.Rect);
        else
            FrontRoomsDoorLite.AttachStream(glass, pair.glassRenderers[k], pair.slabs[k].transform, rect, false);
        pair.liteForm[k] = form;
    }

    static Material HingeMaterial(Pair pair, Zone zone)
    {
        Material m = null;
        if (zone == Zone.Office) m = FrontRoomsSurfaces.TryGet(ChromeSatin);
        else if (zone != Zone.Level0) m = FrontRoomsSurfaces.TryGet(Stainless630);
        return m != null ? m : pair.todayHardware;
    }

    static Material LeafMaterial(Pair pair, Zone zone, int k)
    {
        var hand = k == 0 ? "_L" : "_R";
        Material m = null;
        switch (zone)
        {
            case Zone.Office:
                m = FrontRoomsSurfaces.TryGet("Door_Veneer_OfficeWear" + hand);
                break;
            case Zone.BackOfHouse:
                m = FrontRoomsSurfaces.TryGet(BackOfHousePaint + hand) ?? FrontRoomsSurfaces.TryGet(BackOfHousePaint);
                break;
            case Zone.Run:
                m = FrontRoomsSurfaces.TryGet("Door_RunAlmond" + hand) ?? FrontRoomsSurfaces.TryGet(RunPaint);
                break;
            case Zone.Pool:
                if (pair.poolFinish == PoolFinish.Aluminium) m = FrontRoomsSurfaces.TryGet(PoolAluminium);
                else if (pair.poolFinish == PoolFinish.Epoxy) m = FrontRoomsSurfaces.TryGet(PoolEpoxy);
                else m = FrontRoomsSurfaces.TryGet(PoolGelcoat + hand) ?? FrontRoomsSurfaces.TryGet(PoolGelcoat);
                break;
        }
        return m != null ? m : pair.todayLeaf;
    }

    static Mesh SlabMeshFor(Pair pair, Zone zone, int k)
    {
        switch (zone)
        {
            case Zone.Office:
                return LeafMeshLiteRect("leafLiteF1", SlabRect(F1Floor), "Stream door leaf, office lite F1");
            case Zone.BackOfHouse:
                return SteelSlab("leafSteelBackOfHouse", new[] { SlabRect(F12Floor), SlabRect(F1Floor) }, "Stream door leaf, hollow metal, F1 + F12 louvre");
            case Zone.Run:
                return SteelSlab("leafSteelRun", new[] { SlabRect(F5Floor) }, "Stream door leaf, hollow metal, F5");
            case Zone.Pool:
                return PoolSlabMesh(pair.poolFinish);
            default:
                return pair.todaySlab[k];
        }
    }

    /// <summary>The pool's slab: PD-A the moulded FRP leaf (r 3 mm arrises, the A1 cut-out, a faint 0.5 x 0.2 mm parting seam round
    /// all four edges); PD-C the same leaf with the hollow-metal leaves' 0.6 x 0.4 mm seam (the sheet's swap render); PD-B the
    /// hollow-metal slab with the F1 cut-out (B-2's, without the louvre).</summary>
    static Mesh PoolSlabMesh(PoolFinish finish)
    {
        switch (finish)
        {
            case PoolFinish.Aluminium:
                return SteelSlab("leafSteelF1", new[] { SlabRect(F1Floor) }, "Stream door leaf, hollow metal, F1 (pool aluminium)");
            case PoolFinish.Epoxy:
                return LeafMeshRound("leafRoundSteel", .0006f, .0004f, "Stream door leaf, moulded, A1 porthole, steel seam (pool epoxy)");
            default:
                return LeafMeshRound("leafRoundFrp", .0005f, .0002f, "Stream door leaf, moulded FRP, A1 porthole");
        }
    }

    static Mesh GlassMeshFor(int form)
    {
        var key = form == 1 ? "glassF1" : form == 2 ? "glassF5" : form == 3 ? "glassA1" : "glassF1Tempered";
        Mesh mesh;
        if (Cached(key, out mesh)) return mesh;
        var b = new MeshBuilder();
        if (form == 3)
        {
            // the porthole's 1/4 in clear tempered pane: a 32-sided disc 423.4 mm across, 6.0 mm thick, on the leaf's centre plane
            var c = SlabCentre(A1Floor);
            b.Cylinder(new Vector3(c.x, c.y, 0f), PortholeGlass, PortholeGlassThickness, 2, PortholeSides);
            return meshes[key] = b.ToMesh("Stream door lite glass A1 (porthole, tempered)");
        }
        var v = SlabRect(form == 2 ? F5Floor : F1Floor);
        var g = LitePocket - .001f;
        var t = form == 4 ? PortholeGlassThickness : LiteGlass;
        b.Box(new Vector3(v.x - g, v.z - g, -t * .5f), new Vector3(v.y + g, v.w + g, t * .5f), 0);
        return meshes[key] = b.ToMesh(form == 1 ? "Stream door lite glass F1" : form == 2 ? "Stream door lite glass F5" : "Stream door lite glass F1 (tempered)");
    }

    // ------------------------------------------------------------------ station D's face (D-3)
    /// <summary>
    /// D-3: the +Z face of a Level0 pair in the station's language (Both: the stream's terminal pair; Plus: StationDress's static
    /// entrance); false gives today's face back. The walnut lining and casing mesh is swapped for the same linings (and, on
    /// Both, today's -Z casing, vertex for vertex), Kit_DoorCasing_StationPair takes the +Z face over a bronze cap on the
    /// lining's end, Kit_DoorGuard_Casing38 guards both jambs (y 0.10-1.32), the +Z barrels turn 630 stainless. Kept: the
    /// leaves, F4b, the oak saddle, the -Z face, the BUSINESS HOURS plate 75 mm out, the rail and base stop 50 mm out (the band
    /// ends at today's 1.250), no header panel, render-only. Builds its parts on first use. No-op under Today.
    /// </summary>
    public static void SetStationFace(Pair pair, bool on)
    {
        if (pair == null || on == pair.StationFace || (on && Today)) return;
        if (on && pair.Faces == Faces.Both && pair.Zone != Zone.Level0) Dress(pair, Zone.Level0, 0);
        if (on && !EnsureStationParts(pair)) return;
        pair.StationFace = on;
        SetActive(pair.todayFrame, !on);
        SetActive(pair.stationWalnut, on);
        SetActive(pair.stationCasing, on);
        for (var i = 0; i < 2; i++) SetActive(pair.guards38[i], on);
        var barrels = on ? FrontRoomsSurfaces.TryGet(Stainless630) : null;
        if (barrels == null) barrels = pair.todayHardware;
        for (var k = 0; k < 2; k++)
            if (pair.bodyHinges[k] != null) pair.bodyHinges[k].sharedMaterial = barrels;
    }

    static bool EnsureStationParts(Pair pair)
    {
        if (pair.stationCasing != null) return true;
        var casing = FrontRoomsKitLibrary.Spawn(StationCasingKit, pair.Frame, Vector3.zero, Quaternion.identity, null, false, "door casing steel (station face)", false);
        if (casing == null)
        {
            if (warnedZones.Add(Zone.Level0)) Debug.LogWarning("[FrontRooms3D] zone doors: " + StationCasingKit + " missing; station D keeps today's face");
            return false;
        }
        casing.SetActive(false);
        for (var i = 0; i < 2; i++)
        {
            var at = new Vector3(i == 0 ? -RiseX : RiseX, GuardMidY, FaceZ);
            var g = FrontRoomsKitLibrary.Spawn(Guard38Kit, pair.Frame, at, i == 0 ? Quaternion.Euler(0f, 0f, 180f) : Quaternion.identity, null, false,
                "door casing guard +Z " + (i == 0 ? "-x" : "+x") + " (station face)", false);
            if (g == null) continue;
            Guarded(g);
            g.SetActive(false);
            pair.guards38[i] = g;
        }
        var both = pair.Faces == Faces.Both;
        pair.stationWalnut = Part(pair.Frame, both ? "door frame lining and casing -Z" : "door linings", both ? FrameMeshMinusZ() : LiningsMesh(), pair.materials.walnut);
        pair.stationWalnut.SetActive(false);
        pair.stationCasing = casing;
        return true;
    }

    /// <summary>Today's FrameMesh(true) less its +Z casing: the linings and the -Z casing, vertex for vertex.</summary>
    static Mesh FrameMeshMinusZ()
    {
        Mesh mesh;
        if (Cached("frameMinusZ", out mesh)) return mesh;
        var b = new MeshBuilder();
        Linings(b);
        var half = Width * .5f;
        var profile = CasingProfile();
        var inner = half + CasingReveal;
        var top = HeadBottom + CasingReveal;
        const float face = -1f;
        var z = face * WallDepth * .5f;
        var proud = new Vector3(0f, 0f, face);
        b.Sweep(new Vector3(-inner, 0f, z), Vector3.up, Vector3.left, proud, profile, 0f, 0f, top, 1f);
        b.Sweep(new Vector3(inner, 0f, z), Vector3.up, Vector3.right, proud, profile, 0f, 0f, top, 1f);
        b.Sweep(new Vector3(0f, top, z), Vector3.right, Vector3.up, proud, profile, -inner, -1f, inner, 1f);
        return meshes["frameMinusZ"] = b.ToMesh("Stream door frame, linings and -Z face (station face on +Z)");
    }

    /// <summary>Today's linings alone (the static entrance's frame under the station face).</summary>
    static Mesh LiningsMesh()
    {
        Mesh mesh;
        if (Cached("linings", out mesh)) return mesh;
        var b = new MeshBuilder();
        Linings(b);
        return meshes["linings"] = b.ToMesh("Stream door linings (station face on +Z)");
    }

    static void Linings(MeshBuilder b)
    {
        var half = Width * .5f;
        var t = LeafThickness * .5f;
        b.Box(new Vector3(-half - LiningThickness, 0f, -t), new Vector3(-half, HeadBottom, t), 1);
        b.Box(new Vector3(half, 0f, -t), new Vector3(half + LiningThickness, HeadBottom, t), 1);
        b.Box(new Vector3(-half - LiningThickness, HeadBottom, -t), new Vector3(half + LiningThickness, HeadBottom + LiningThickness, t), 0);
    }

    // ------------------------------------------------------------------ zone slabs
    /// <summary>LeafMeshLite(true) with another lite rect (the Office's F1 in the walnut bead): one mesh in the leaf's material.</summary>
    static Mesh LeafMeshLiteRect(string key, Vector4 v, string name)
    {
        Mesh mesh;
        if (Cached(key, out mesh)) return mesh;
        var b = new MeshBuilder();
        var size = new Vector3(LeafWidth, LeafHeight, LeafThickness);
        b.BevelBoxHoled(Vector3.zero, size, .0015f, new Vector2(v.x - LitePocket, v.z - LitePocket), new Vector2(v.y + LitePocket, v.w + LitePocket));
        foreach (var face in new[] { -1f, 1f })
        {
            var proud = new Vector3(0f, 0f, face);
            b.Sweep(new Vector3(v.x, 0f, 0f), Vector3.up, Vector3.left, proud, BeadProfile, v.z, -1f, v.w, 1f);
            b.Sweep(new Vector3(v.y, 0f, 0f), Vector3.up, Vector3.right, proud, BeadProfile, v.z, -1f, v.w, 1f);
            b.Sweep(new Vector3(0f, v.z, 0f), Vector3.right, Vector3.down, proud, BeadProfile, v.x, -1f, v.y, 1f);
            b.Sweep(new Vector3(0f, v.w, 0f), Vector3.right, Vector3.up, proud, BeadProfile, v.x, -1f, v.y, 1f);
        }
        return meshes[key] = b.ToMesh(name);
    }

    /// <summary>
    /// A hollow-metal leaf: the slab with 1.5 mm arrises, cut through for each visible rect plus the 9.5 mm pocket (the pressed
    /// kits' legs line the cut), and the visible edge seam of a 1990 commercial hollow-metal door down both stile edges (a
    /// 0.6 mm groove at mid-thickness, 0.4 mm deep, where the two face sheets meet). UVs as the walnut slab: metres from its
    /// min corner, front and back alike (the leaf wear's frame).
    /// </summary>
    static Mesh SteelSlab(string key, Vector4[] visible, string name)
    {
        Mesh mesh;
        if (Cached(key, out mesh)) return mesh;
        var holes = new Vector4[visible.Length];
        for (var i = 0; i < visible.Length; i++)
            holes[i] = new Vector4(visible[i].x - LitePocket, visible[i].y + LitePocket, visible[i].z - LitePocket, visible[i].w + LitePocket);
        var b = new SlabBuilder();
        b.Slab(new Vector3(LeafWidth, LeafHeight, LeafThickness), .0015f, holes, .0006f, .0004f);
        return meshes[key] = b.ToMesh(name);
    }

    /// <summary>The steel slab's geometry (the walnut slab's MeshBuilder rules: planar faces, metre UVs from the min corner, flat
    /// chamfers and corner triangles), with several stacked holes and grooved stile edges.</summary>
    sealed class SlabBuilder
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector3> n = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int> t = new List<int>();
        Vector3 o;

        int Vert(Vector3 p, Vector3 normal, Vector2 tex)
        {
            v.Add(p); n.Add(normal); uv.Add(tex);
            return v.Count - 1;
        }

        void Tri(int a, int b, int c, Vector3 facing)
        {
            if (Vector3.Dot(Vector3.Cross(v[b] - v[a], v[c] - v[a]), facing) < 0f) { var s = b; b = c; c = s; }
            t.Add(a); t.Add(b); t.Add(c);
        }

        static Vector2 Project(Vector3 p, Vector3 normal, int uAxis)
        {
            var drop = Mathf.Abs(normal.x) >= Mathf.Abs(normal.y) && Mathf.Abs(normal.x) >= Mathf.Abs(normal.z) ? 0 : (Mathf.Abs(normal.y) >= Mathf.Abs(normal.z) ? 1 : 2);
            if (uAxis == drop) uAxis = drop == 0 ? 2 : 0;
            var vAxis = 3 - drop - uAxis;
            return new Vector2(p[uAxis], p[vAxis]);
        }

        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, int uAxis)
        {
            var i0 = Vert(a, normal, Project(a - o, normal, uAxis));
            var i1 = Vert(b, normal, Project(b - o, normal, uAxis));
            var i2 = Vert(c, normal, Project(c - o, normal, uAxis));
            var i3 = Vert(d, normal, Project(d - o, normal, uAxis));
            Tri(i0, i1, i2, normal);
            Tri(i0, i2, i3, normal);
        }

        static Vector3 BevelPoint(Vector3 h, float bev, int face, int sx, int sy, int sz)
        {
            return new Vector3(sx * (face == 0 ? h.x : h.x - bev), sy * (face == 1 ? h.y : h.y - bev), sz * (face == 2 ? h.z : h.z - bev));
        }

        /// <summary>size: the slab, centred on 0; holes: (x0, x1, y0, y1), stacked in y, inside the faces; seamW / seamD: the stile
        /// edges' groove. Conforming (Fix A, Review A finding 3): every face, reveal, edge strip and chamfer is split wherever a
        /// neighbour has a vertex (x at every hole's sides, y at every hole's rails, z at the groove), so no edge ends on the
        /// middle of another (no T-junction): watertight to the pixel at any distance, nothing of the room behind shows through.</summary>
        public void Slab(Vector3 size, float bev, Vector4[] holes, float seamW, float seamD)
        {
            var h = size * .5f;
            o = -h;
            float fx0 = -(h.x - bev), fx1 = h.x - bev, fy0 = -(h.y - bev), fy1 = h.y - bev, fz0 = -(h.z - bev), fz1 = h.z - bev;
            float g0 = -seamW * .5f, g1 = seamW * .5f;
            var hx = new List<float>();
            var hy = new List<float>();
            foreach (var hole in holes) { hx.Add(hole.x); hx.Add(hole.y); hy.Add(hole.z); hy.Add(hole.w); }
            var xs = Breaks(fx0, fx1, hx);
            var ys = Breaks(fy0, fy1, hy);
            var zs = new[] { fz0, g0, g1, fz1 };
            // the two faces: every cell of the grid that is not a hole
            foreach (var s in new[] { -1f, 1f })
            {
                var z = s * h.z;
                var nz = new Vector3(0f, 0f, s);
                for (var i = 0; i + 1 < xs.Length; i++)
                    for (var j = 0; j + 1 < ys.Length; j++)
                    {
                        if (InHole(holes, (xs[i] + xs[i + 1]) * .5f, (ys[j] + ys[j + 1]) * .5f)) continue;
                        Quad(new Vector3(xs[i], ys[j], z), new Vector3(xs[i + 1], ys[j], z), new Vector3(xs[i + 1], ys[j + 1], z), new Vector3(xs[i], ys[j + 1], z), nz, 0);
                    }
            }
            // the holes' reveals, facing into each hole, split where the face grid's lines meet them
            foreach (var hole in holes)
            {
                float z0 = -h.z, z1 = h.z;
                for (var i = 0; i + 1 < xs.Length; i++)
                {
                    if (xs[i] < hole.x - 1e-6f || xs[i + 1] > hole.y + 1e-6f) continue;
                    float a = xs[i], b = xs[i + 1];
                    Quad(new Vector3(a, hole.z, z0), new Vector3(b, hole.z, z0), new Vector3(b, hole.z, z1), new Vector3(a, hole.z, z1), Vector3.up, 0);
                    Quad(new Vector3(a, hole.w, z0), new Vector3(b, hole.w, z0), new Vector3(b, hole.w, z1), new Vector3(a, hole.w, z1), Vector3.down, 0);
                }
                for (var j = 0; j + 1 < ys.Length; j++)
                {
                    if (ys[j] < hole.z - 1e-6f || ys[j + 1] > hole.w + 1e-6f) continue;
                    float a = ys[j], b = ys[j + 1];
                    Quad(new Vector3(hole.x, a, z0), new Vector3(hole.x, b, z0), new Vector3(hole.x, b, z1), new Vector3(hole.x, a, z1), Vector3.right, 1);
                    Quad(new Vector3(hole.y, a, z0), new Vector3(hole.y, b, z0), new Vector3(hole.y, b, z1), new Vector3(hole.y, a, z1), Vector3.left, 1);
                }
            }
            // top and bottom edges (the leaf's channels), split as the chamfers that meet them
            foreach (var s in new[] { -1f, 1f })
            {
                var y = s * h.y;
                for (var i = 0; i + 1 < xs.Length; i++)
                    for (var k = 0; k + 1 < zs.Length; k++)
                        Quad(new Vector3(xs[i], y, zs[k]), new Vector3(xs[i + 1], y, zs[k]), new Vector3(xs[i + 1], y, zs[k + 1]), new Vector3(xs[i], y, zs[k + 1]), new Vector3(0f, s, 0f), 0);
            }
            // the stile edges, with the seam groove at mid-thickness, split at the face grid's rows
            foreach (var s in new[] { -1f, 1f })
            {
                var x = s * h.x;
                var xi = s * (h.x - seamD);
                var nx = new Vector3(s, 0f, 0f);
                for (var j = 0; j + 1 < ys.Length; j++)
                {
                    float a = ys[j], b = ys[j + 1];
                    Quad(new Vector3(x, a, fz0), new Vector3(x, b, fz0), new Vector3(x, b, g0), new Vector3(x, a, g0), nx, 2);
                    Quad(new Vector3(x, a, g1), new Vector3(x, b, g1), new Vector3(x, b, fz1), new Vector3(x, a, fz1), nx, 2);
                    Quad(new Vector3(xi, a, g0), new Vector3(xi, b, g0), new Vector3(xi, b, g1), new Vector3(xi, a, g1), nx, 2);
                    Quad(new Vector3(x, a, g0), new Vector3(xi, a, g0), new Vector3(xi, b, g0), new Vector3(x, b, g0), Vector3.forward, 1);
                    Quad(new Vector3(x, a, g1), new Vector3(xi, a, g1), new Vector3(xi, b, g1), new Vector3(x, b, g1), Vector3.back, 1);
                }
                // the groove's ends (where it runs out into the top and bottom chamfers)
                Quad(new Vector3(xi, fy0, g0), new Vector3(x, fy0, g0), new Vector3(x, fy0, g1), new Vector3(xi, fy0, g1), Vector3.up, 0);
                Quad(new Vector3(xi, fy1, g0), new Vector3(x, fy1, g0), new Vector3(x, fy1, g1), new Vector3(xi, fy1, g1), Vector3.down, 0);
            }
            // chamfers between faces a and bb along the third axis c (as MeshBuilder.BevelBox), each split at that axis's
            // breakpoints: y for the stile chamfers, x for the top and bottom ones, z (the groove) for the corner ones
            for (var a = 0; a < 3; a++)
                for (var bb = a + 1; bb < 3; bb++)
                {
                    var c = 3 - a - bb;
                    var cut = c == 0 ? xs : c == 1 ? ys : zs;
                    for (var sa = -1; sa <= 1; sa += 2)
                        for (var sb = -1; sb <= 1; sb += 2)
                        {
                            var sg = new int[3];
                            sg[a] = sa; sg[bb] = sb;
                            sg[c] = 1;
                            var pa = BevelPoint(h, bev, a, sg[0], sg[1], sg[2]);
                            var pb = BevelPoint(h, bev, bb, sg[0], sg[1], sg[2]);
                            var normal = Vector3.zero; normal[a] = sa; normal[bb] = sb;
                            normal.Normalize();
                            for (var k = 0; k + 1 < cut.Length; k++)
                            {
                                Vector3 p0 = pa, p1 = pa, p2 = pb, p3 = pb;
                                p0[c] = cut[k]; p3[c] = cut[k];
                                p1[c] = cut[k + 1]; p2[c] = cut[k + 1];
                                Quad(p0, p1, p2, p3, normal, c == 1 ? 0 : c);
                            }
                        }
                }
            for (var sx = -1; sx <= 1; sx += 2)
                for (var sy = -1; sy <= 1; sy += 2)
                    for (var sz = -1; sz <= 1; sz += 2)
                    {
                        var normal = new Vector3(sx, sy, sz).normalized;
                        var pa = BevelPoint(h, bev, 0, sx, sy, sz);
                        var pb = BevelPoint(h, bev, 1, sx, sy, sz);
                        var pc = BevelPoint(h, bev, 2, sx, sy, sz);
                        var ia = Vert(pa, normal, Project(pa - o, normal, 0));
                        var ib = Vert(pb, normal, Project(pb - o, normal, 0));
                        var ic = Vert(pc, normal, Project(pc - o, normal, 0));
                        Tri(ia, ib, ic, normal);
                    }
        }

        /// <summary>lo, hi and every extra value strictly between them, sorted, without repeats.</summary>
        static float[] Breaks(float lo, float hi, List<float> extra)
        {
            var all = new List<float> { lo, hi };
            foreach (var e in extra)
                if (e > lo + 1e-6f && e < hi - 1e-6f) all.Add(e);
            all.Sort();
            var outList = new List<float>();
            foreach (var e in all)
                if (outList.Count == 0 || e - outList[outList.Count - 1] > 1e-6f) outList.Add(e);
            return outList.ToArray();
        }

        static bool InHole(Vector4[] holes, float x, float y)
        {
            foreach (var hole in holes)
                if (x > hole.x && x < hole.y && y > hole.z && y < hole.w) return true;
            return false;
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            try { mesh.RecalculateTangents(); } catch (System.Exception) { }
            return mesh;
        }
    }
}

public static partial class FrontRoomsDoubleDoorKit
{
    // ------------------------------------------------------------------ the pool's moulded leaf (Build B)
    /// <summary>
    /// PD-A's moulded FRP leaf (20_design.md B3.1): the 1.1955 x 2.639 x 0.0445 m slab with moulded r 3 mm arrises (3 segments,
    /// the eight corners spherical), the A1 porthole's cut-out (PortholeCut, 32 sides at the porthole's centre; its wall is hidden
    /// under the ring kits) and a faint parting seam at mid-thickness round all four edges (Pitt's trimmed peripheral seams,
    /// seamW wide, seamD deep). Planar faces with metre UVs from the slab's min corner, front and back alike, as the walnut and
    /// steel slabs (the leaf wear's frame). Watertight and conforming (no T-junction, Fix A's lesson): each face is a quad strip
    /// from the cut-out to a square of its radial projections, then four fans from the square's sides to the face's corners, so
    /// the face's edges keep only their two corners and the arris strips run whole along them; only the through-thickness corner
    /// strips split, at the seam.
    /// </summary>
    static Mesh LeafMeshRound(string key, float seamW, float seamD, string name)
    {
        Mesh mesh;
        if (Cached(key, out mesh)) return mesh;
        var c = SlabCentre(A1Floor);
        var b = new RoundSlabBuilder();
        b.Slab(new Vector3(LeafWidth, LeafHeight, LeafThickness), .003f, 3, new Vector2(c.x, c.y), PortholeCut, PortholeSides, .26f, seamW, seamD);
        return meshes[key] = b.ToMesh(name);
    }

    sealed class RoundSlabBuilder
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector3> n = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int> t = new List<int>();
        Vector3 o;

        int Vert(Vector3 p, Vector3 normal)
        {
            v.Add(p); n.Add(normal); uv.Add(Uv(p, normal));
            return v.Count - 1;
        }

        // metres from the slab's min corner, dropping the normal's dominant axis: z faces (x, y), x faces (z, y), y faces (x, z)
        Vector2 Uv(Vector3 p, Vector3 normal)
        {
            var q = p - o;
            float ax = Mathf.Abs(normal.x), ay = Mathf.Abs(normal.y), az = Mathf.Abs(normal.z);
            if (az >= ax && az >= ay) return new Vector2(q.x, q.y);
            if (ax >= ay) return new Vector2(q.z, q.y);
            return new Vector2(q.x, q.z);
        }

        void Tri(int a, int b, int c, Vector3 facing)
        {
            if (Vector3.Dot(Vector3.Cross(v[b] - v[a], v[c] - v[a]), facing) < 0f) { var s = b; b = c; c = s; }
            t.Add(a); t.Add(b); t.Add(c);
        }

        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        {
            var i0 = Vert(a, normal); var i1 = Vert(b, normal); var i2 = Vert(c, normal); var i3 = Vert(d, normal);
            Tri(i0, i1, i2, normal);
            Tri(i0, i2, i3, normal);
        }

        // a curved quad: a normal per corner, wound to face their mean
        void QuadN(Vector3 a, Vector3 na, Vector3 b, Vector3 nb, Vector3 c, Vector3 nc, Vector3 d, Vector3 nd)
        {
            var i0 = Vert(a, na); var i1 = Vert(b, nb); var i2 = Vert(c, nc); var i3 = Vert(d, nd);
            var f = (na + nb + nc + nd).normalized;
            Tri(i0, i1, i2, f);
            Tri(i0, i2, i3, f);
        }

        /// <summary>size: the slab, centred on 0; r, segs: the arrises; hc, hr, hs: the cut-out's centre (x, y), radius and sides
        /// (a multiple of 8: its vertices fall on the diagonals, so its radial projections meet the square's corners); sq: the
        /// square's half-size round the cut-out; seamW, seamD: the parting seam round the four edges.</summary>
        public void Slab(Vector3 size, float r, int segs, Vector2 hc, float hr, int hs, float sq, float seamW, float seamD)
        {
            var h = size * .5f;
            o = -h;
            float fx = h.x - r, fy = h.y - r, fz = h.z - r, g = seamW * .5f;
            // ---- the two faces
            var hole = new Vector2[hs];
            var ring = new Vector2[hs];
            for (var i = 0; i < hs; i++)
            {
                var a = i * Mathf.PI * 2f / hs;
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                hole[i] = hc + d * hr;
                ring[i] = hc + d * (sq / Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y)));
            }
            int iTR = hs / 8, iTL = 3 * hs / 8, iBL = 5 * hs / 8, iBR = 7 * hs / 8;
            var corner = new[] { new Vector2(fx, fy), new Vector2(-fx, fy), new Vector2(-fx, -fy), new Vector2(fx, -fy) };   // TR, TL, BL, BR
            foreach (var s in new[] { -1f, 1f })
            {
                var z = s * h.z;
                var nz = new Vector3(0f, 0f, s);
                for (var i = 0; i < hs; i++)
                {
                    var j = (i + 1) % hs;
                    Quad(P(hole[i], z), P(hole[j], z), P(ring[j], z), P(ring[i], z), nz);
                }
                // four fans: from a face corner over the face's edge to the next corner, then along the square's side back
                Fan(corner[0], corner[1], iTL, iTR, ring, hs, z, nz);   // top: TR, TL, ring TL .. TR
                Fan(corner[1], corner[2], iBL, iTL, ring, hs, z, nz);   // left: TL, BL, ring BL .. TL
                Fan(corner[2], corner[3], iBR, iBL, ring, hs, z, nz);   // bottom: BL, BR, ring BR .. BL
                Fan(corner[3], corner[0], iTR + hs, iBR, ring, hs, z, nz);   // right: BR, TR, ring TR .. BR (through 0)
            }
            // ---- the cut-out's wall, facing its axis
            for (var i = 0; i < hs; i++)
            {
                var j = (i + 1) % hs;
                var ni = -(Vector3)(hole[i] - hc).normalized;
                var nj = -(Vector3)(hole[j] - hc).normalized;
                QuadN(P(hole[i], -h.z), ni, P(hole[j], -h.z), nj, P(hole[j], h.z), nj, P(hole[i], h.z), ni);
            }
            // ---- the long arrises, whole along the faces' edges: along y (the stiles) and along x (top and bottom)
            for (var sx = -1; sx <= 1; sx += 2)
                for (var sz = -1; sz <= 1; sz += 2)
                    for (var k = 0; k < segs; k++)
                    {
                        Vector3 d0 = Dir(sx, 0, sz, k, segs), d1 = Dir(sx, 0, sz, k + 1, segs);
                        var c0 = new Vector3(sx * fx, -fy, sz * fz);
                        var c1 = new Vector3(sx * fx, fy, sz * fz);
                        QuadN(c0 + r * d0, d0, c0 + r * d1, d1, c1 + r * d1, d1, c1 + r * d0, d0);
                    }
            for (var sy = -1; sy <= 1; sy += 2)
                for (var sz = -1; sz <= 1; sz += 2)
                    for (var k = 0; k < segs; k++)
                    {
                        Vector3 d0 = Dir(0, sy, sz, k, segs), d1 = Dir(0, sy, sz, k + 1, segs);
                        var c0 = new Vector3(-fx, sy * fy, sz * fz);
                        var c1 = new Vector3(fx, sy * fy, sz * fz);
                        QuadN(c0 + r * d0, d0, c0 + r * d1, d1, c1 + r * d1, d1, c1 + r * d0, d0);
                    }
            // ---- the through-thickness corner arrises (x-y), in three spans: the seam's groove runs round them at r - seamD
            var zs = new[] { -fz, -g, g, fz };
            for (var sx = -1; sx <= 1; sx += 2)
                for (var sy = -1; sy <= 1; sy += 2)
                {
                    var c = new Vector3(sx * fx, sy * fy, 0f);
                    for (var k = 0; k < segs; k++)
                    {
                        Vector3 d0 = DirXY(sx, sy, k, segs), d1 = DirXY(sx, sy, k + 1, segs);
                        for (var span = 0; span < 3; span++)
                        {
                            var rr = span == 1 ? r - seamD : r;
                            var za = new Vector3(0f, 0f, zs[span]);
                            var zb = new Vector3(0f, 0f, zs[span + 1]);
                            QuadN(c + za + rr * d0, d0, c + za + rr * d1, d1, c + zb + rr * d1, d1, c + zb + rr * d0, d0);
                        }
                        // the groove's walls at z = -g and +g, facing into it
                        foreach (var sw in new[] { -1f, 1f })
                        {
                            var zw = new Vector3(0f, 0f, sw * g);
                            Quad(c + zw + (r - seamD) * d0, c + zw + (r - seamD) * d1, c + zw + r * d1, c + zw + r * d0, new Vector3(0f, 0f, -sw));
                        }
                    }
                }
            // ---- the edge bands: the stiles (x) and the top and bottom (y), each split at the seam
            for (var sx = -1; sx <= 1; sx += 2)
            {
                var nx = new Vector3(sx, 0f, 0f);
                float x = sx * h.x, xi = sx * (h.x - seamD);
                Quad(new Vector3(x, -fy, -fz), new Vector3(x, fy, -fz), new Vector3(x, fy, -g), new Vector3(x, -fy, -g), nx);
                Quad(new Vector3(x, -fy, g), new Vector3(x, fy, g), new Vector3(x, fy, fz), new Vector3(x, -fy, fz), nx);
                Quad(new Vector3(xi, -fy, -g), new Vector3(xi, fy, -g), new Vector3(xi, fy, g), new Vector3(xi, -fy, g), nx);
                Quad(new Vector3(xi, -fy, g), new Vector3(x, -fy, g), new Vector3(x, fy, g), new Vector3(xi, fy, g), Vector3.back);
                Quad(new Vector3(xi, -fy, -g), new Vector3(x, -fy, -g), new Vector3(x, fy, -g), new Vector3(xi, fy, -g), Vector3.forward);
            }
            for (var sy = -1; sy <= 1; sy += 2)
            {
                var ny = new Vector3(0f, sy, 0f);
                float y = sy * h.y, yi = sy * (h.y - seamD);
                Quad(new Vector3(-fx, y, -fz), new Vector3(fx, y, -fz), new Vector3(fx, y, -g), new Vector3(-fx, y, -g), ny);
                Quad(new Vector3(-fx, y, g), new Vector3(fx, y, g), new Vector3(fx, y, fz), new Vector3(-fx, y, fz), ny);
                Quad(new Vector3(-fx, yi, -g), new Vector3(fx, yi, -g), new Vector3(fx, yi, g), new Vector3(-fx, yi, g), ny);
                Quad(new Vector3(-fx, yi, g), new Vector3(-fx, y, g), new Vector3(fx, y, g), new Vector3(fx, yi, g), Vector3.back);
                Quad(new Vector3(-fx, yi, -g), new Vector3(-fx, y, -g), new Vector3(fx, y, -g), new Vector3(fx, yi, -g), Vector3.forward);
            }
            // ---- the eight corners: spherical patches, their edges on the three arrises' end arcs
            for (var sx = -1; sx <= 1; sx += 2)
                for (var sy = -1; sy <= 1; sy += 2)
                    for (var sz = -1; sz <= 1; sz += 2)
                        Octant(new Vector3(sx * fx, sy * fy, sz * fz), new Vector3(0f, 0f, sz), new Vector3(sx, 0f, 0f), new Vector3(0f, sy, 0f), r, segs);
        }

        static Vector3 P(Vector2 xy, float z) => new Vector3(xy.x, xy.y, z);

        // the arris direction at step k of segs between the face axis (k 0: x or y) and the slab's thickness (k segs: z)
        static Vector3 Dir(int sx, int sy, int sz, int k, int segs)
        {
            var a = k * Mathf.PI * .5f / segs;
            return new Vector3(sx * Mathf.Cos(a), sy * Mathf.Cos(a), sz * Mathf.Sin(a));
        }

        // the through-thickness corner arris: from x (k 0) to y (k segs)
        static Vector3 DirXY(int sx, int sy, int k, int segs)
        {
            var a = k * Mathf.PI * .5f / segs;
            return new Vector3(sx * Mathf.Cos(a), sy * Mathf.Sin(a), 0f);
        }

        // a fan from face corner c0 over the face's edge to c1, then along the square's side from ring index 'from' down to 'to'
        void Fan(Vector2 c0, Vector2 c1, int from, int to, Vector2[] ring, int hs, float z, Vector3 normal)
        {
            var apex = Vert(P(c0, z), normal);
            var prev = Vert(P(c1, z), normal);
            for (var i = from; i >= to; i--)
            {
                var cur = Vert(P(ring[((i % hs) + hs) % hs], z), normal);
                Tri(apex, prev, cur, normal);
                prev = cur;
            }
        }

        // a spherical corner patch: rows from a (the thickness axis) to the arc b..c, every boundary vertex on an arris's end arc
        void Octant(Vector3 centre, Vector3 a, Vector3 b, Vector3 c, float r, int segs)
        {
            var idx = new int[segs + 1][];
            for (var i = 0; i <= segs; i++)
            {
                idx[i] = new int[i + 1];
                var ti = i * Mathf.PI * .5f / segs;
                var pb = a * Mathf.Cos(ti) + b * Mathf.Sin(ti);
                var pc = a * Mathf.Cos(ti) + c * Mathf.Sin(ti);
                for (var j = 0; j <= i; j++)
                {
                    var d = i == 0 ? a : Vector3.Slerp(pb, pc, j / (float)i).normalized;
                    idx[i][j] = Vert(centre + r * d, d);
                }
            }
            for (var i = 0; i < segs; i++)
            {
                for (var j = 0; j <= i; j++)
                {
                    int p0 = idx[i][j], p1 = idx[i + 1][j], p2 = idx[i + 1][j + 1];
                    Tri(p0, p1, p2, n[p0] + n[p1] + n[p2]);
                }
                for (var j = 0; j < i; j++)
                {
                    int p0 = idx[i][j], p1 = idx[i + 1][j + 1], p2 = idx[i][j + 1];
                    Tri(p0, p1, p2, n[p0] + n[p1] + n[p2]);
                }
            }
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            try { mesh.RecalculateTangents(); } catch (System.Exception) { }
            return mesh;
        }
    }
}
