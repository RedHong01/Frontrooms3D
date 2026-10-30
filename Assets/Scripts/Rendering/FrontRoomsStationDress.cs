using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// A26 station-look-v1: 游戏视觉's dressing of Floor 1's first room, the station set the map builds
/// (research/station/70_station_look_v1_plan.md; 61_for_red_first_room.md; NOTE_1022_first_room_DECIDED.md).
/// Bound at FrontRooms3DGame.MapRunStarted / FloorChanged (subscribed directly: WebGL High stripping, V4R) when the map
/// holds a station (map.HasStation; with the profile's stationModule empty nothing happens and the game is today's).
/// Under map.Station it adds:
/// - the trim (FrontRoomsStationTrim): crash rail, base and border course on both phases' south faces (under the
///   south liner while the start area stands, under the static entrance after ClearStartArea), the E1/E2 frames,
///   guards, throats, reducers and ceiling bands; no header panel over D (plan check C1-5);
/// - the static entrance as the same door seen from inside: FrontRoomsDoubleDoorKit's stream door, shut, +Z face only,
///   on D's data wall (stream_door_frame follow-up); the map's stand-in leaves, jambs and head DEACTIVATED
///   (StationSet.SetVisible re-enables renderers);
/// - the BUSINESS HOURS plate (信息设计's art, 457.2 x 152.4 mm, adhesive) at map.Station.PlateAnchor in both phases;
/// - the set (P02) re-seated on its bracket's set_mount (20 deg front-down), its picture attached with
///   FrontRoomsScreenVideo.Attach(screen mount, null, Beacon) (the one moving screen; 声音设计's Beacon rule), the
///   picture added to the set's LODGroup (C2-5b), the set's light (FrontRoomsStationTvLight), and at the take
///   (map.StationKeyTaken) the light's fall and PowerOffForGood (C1-1);
/// - the rendering-layer masks (61 §7, plan §9 L1; inert until the URP asset's light layers are on): station renderers
///   (shell incl. ceiling, set, props, trim room side, plate, placard, the floor key) Station only; opening trims both;
///   the two entrance lamps both; every other station lamp Station only. Masks change by bit operations only (C2-3e).
/// Its renderers follow map.Station.Visible. Never edits a map file; never runs without a station.
/// </summary>
[AddComponentMenu("")]
public sealed class FrontRoomsStationDress : MonoBehaviour
{
    public const string ObjectName = "Station dress";
    public const uint StationBit = 1u << 1, DefaultBit = 1u;
    public const string TvKit = "Kit_CRTTV_Station", BracketKit = "Kit_TVWallBracket";
    public const string PlateMaterial = "Prop_PlateBusinessHours";
    /// <summary>The set's picture: inside the bezel, in front of the curved glass (Kit_CRTTV_Station, 0.41 x 0.31 m, 4:3).</summary>
    public static readonly Vector3 PictureCentre = new Vector3(0f, .284f, .226f);
    public static readonly Vector2 PictureSize = new Vector2(.41f, .31f);
    /// <summary>FrontRoomsScreenVideo's default surface (Kit_CRTMonitor's): anchor + 4 mm, size. The screen mount scales it onto the TV's picture.</summary>
    static readonly Vector3 DefaultSurface = new Vector3(0f, .248f, .194f + .004f);
    static readonly Vector2 DefaultSize = new Vector2(.28f, .19f);

    FrontRoomsMapWorld map;
    FrontRoomsStationSet set;
    readonly List<FrontRoomsStationTrim.Part> trim = new List<FrontRoomsStationTrim.Part>();
    readonly List<Renderer> own = new List<Renderer>();
    readonly List<Mesh> meshes = new List<Mesh>();
    int taggedSetRenderers, scannedChildren = -1;
    float scanTimer;
    bool visible = true;
    GameObject tv, bracket, screenMount;
    FrontRoomsScreenVideo screen;
    FrontRoomsStationTvLight tvLight;
    bool keyTagged;
    float keySearch;
    GridCoord entranceLampA, entranceLampB;

    public FrontRoomsStationTvLight TvLight => tvLight;
    public FrontRoomsScreenVideo Screen => screen;
    public GameObject Tv => tv;
    public GameObject Bracket => bracket;
    public bool TvReady => screen != null;
    public bool KeyTagged => keyTagged;
    public IReadOnlyList<FrontRoomsStationTrim.Part> Trim => trim;
    public IReadOnlyList<Renderer> OwnRenderers => own;

    // ------------------------------------------------------------------ hook
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Hook()
    {
        FrontRooms3DGame.MapRunStarted -= OnRunStarted;
        FrontRooms3DGame.MapRunStarted += OnRunStarted;
        FrontRooms3DGame.FloorChanged -= OnFloorChanged;
        FrontRooms3DGame.FloorChanged += OnFloorChanged;
    }

    static void OnRunStarted(FrontRoomsMapWorld map, FrontRoomsMapHunter relay) => Attach(map);
    static void OnFloorChanged(FrontRoomsMapWorld map, FrontRoomsMapHunter relay, int floor) => Attach(map);

    /// <summary>Dress the map's station once (null with no station). Public for the proof harness.</summary>
    public static FrontRoomsStationDress Attach(FrontRoomsMapWorld map)
    {
        if (map == null || !map.HasStation || map.Station == null) return null;
        var existing = map.Station.GetComponentInChildren<FrontRoomsStationDress>(true);
        if (existing != null) return existing;
        var go = new GameObject(ObjectName);
        go.transform.SetParent(map.Station.transform, false);
        var d = go.AddComponent<FrontRoomsStationDress>();
        d.Bind(map);
        return d;
    }

    // ------------------------------------------------------------------ bind
    void Bind(FrontRoomsMapWorld world)
    {
        map = world;
        set = map.Station;
        var room = FrontRoomsStationTrim.Room.From(map);
        // The trim: openings always; the room side once per phase, under the object that stands in that phase.
        FrontRoomsStationTrim.BuildOpenings(transform, room, trim, meshes);
        if (set.Liner != null)
            FrontRoomsStationTrim.BuildRoomSide(set.Liner.transform, room, room.south + FrontRoomsStationSet.FacadeFace + FrontRoomsStationSet.LinerProud, trim, meshes);
        if (set.Entrance != null)
            FrontRoomsStationTrim.BuildRoomSide(set.Entrance.transform, room, room.south + ModuleUnits.WallHalf, trim, meshes);
        foreach (var p in trim) own.Add(p.renderer);
        DressEntrance(room);
        MountPlates();
        var s = map.StationLayout.station;
        entranceLampA = new GridCoord(s.x + 1, s.y);
        entranceLampB = new GridCoord(s.x + 3, s.y);
        // Rendering layers: the shell built so far, and every later build.
        map.StationShellBuilt += OnShellBuilt;
        map.StationKeyTaken += OnKeyTaken;
        TagBuiltShell();
        foreach (var p in trim) SetMask(p.renderer, p.mask == FrontRoomsStationTrim.MaskClass.BothLayers);
        visible = !set.Visible;
        ApplyVisibility(set.Visible);
        Debug.Log("[StationDress] dressed the station at " + s.x + "," + s.y + ": " + trim.Count + " trim renderers, " + own.Count + " own renderers");
    }

    void OnDestroy()
    {
        if (map != null)
        {
            map.StationShellBuilt -= OnShellBuilt;
            map.StationKeyTaken -= OnKeyTaken;
        }
        foreach (var m in meshes)
            if (m != null) { if (Application.isPlaying) Destroy(m); else DestroyImmediate(m); }
        meshes.Clear();
    }

    // ------------------------------------------------------------------ the entrance and the plate
    static Material Hardware() => FrontRoomsSurfaces.Lit("Door / brushed steel", new Color(.64f, .63f, .60f), .62f, .9f);

    // The static entrance = the start door the player came through, shut, seen from the room (stream_door_frame
    // follow-up, 2026-10-29): FrontRoomsDoubleDoorKit builds it as FrontRoomsRoomStream does, +Z face only (Faces.Plus),
    // its casing on D's data wall face (the wall's inner face, where the stream's 0.04 m end wall had its station face),
    // so the lining, casing, saddle, hinges, leaves, pulls and kick plates sit where they stood before the clear; the
    // 3 mm gaps show a dark seal (the data wall is solid behind them). The map's stand-ins (leaves, jambs, head; render-
    // only, no colliders) are deactivated; nothing here has a collider, so the wall's collision is the map's.
    void DressEntrance(FrontRoomsStationTrim.Room room)
    {
        var entrance = set.Entrance;
        if (entrance == null) return;
        foreach (Transform child in entrance.transform)
            if (child.name.StartsWith("Static entrance leaf", System.StringComparison.Ordinal) || child.name.Contains("jamb") || child.name.Contains("head"))
                child.gameObject.SetActive(false);
        var door = new GameObject("Static entrance door (the stream door, shut)").transform;
        door.SetParent(entrance.transform, false);
        door.localPosition = new Vector3((room.doorX0 + room.doorX1) * .5f, 0f, room.south + ModuleUnits.WallHalf - FrontRoomsDoubleDoorKit.WallDepth * .5f);
        var hardware = Hardware();
        // The casing and linings: the walnut door frame stock turned onto U, as the stream door (door wood, Red 2026-10-30).
        var walnut = FrontRoomsSurfaces.TryGet("Door_FrameU") ?? FrontRoomsSurfaces.TryGet("Prop_WoodWalnut") ?? hardware;
        var kit = new FrontRoomsDoubleDoorKit.Materials
        {
            walnut = walnut,
            oak = FrontRoomsSurfaces.TryGet("Prop_WoodOak") ?? walnut,
            brush = FrontRoomsSurfaces.TryGet("Prop_PlasticBlack") ?? hardware,
            hardware = hardware,
            leaf = FrontRoomsSurfaces.DoorVeneer
        };
        var frame = new GameObject("double door frame").transform;
        frame.SetParent(door, false);
        FrontRoomsDoubleDoorKit.BuildFrame(frame, kit, FrontRoomsDoubleDoorKit.Faces.Plus);
        for (var hand = -1; hand <= 1; hand += 2)
        {
            Transform carrier, hingeBody;
            GameObject slab;
            var pivot = FrontRoomsDoubleDoorKit.BuildLeaf(door, frame, hand, kit, FrontRoomsDoubleDoorKit.Faces.Plus, false, out carrier, out hingeBody, out slab);
            FrontRoomsDoubleDoorKit.PlaceLeaf(frame, pivot, carrier, hingeBody, hand, 1);
        }
        foreach (var r in door.GetComponentsInChildren<Renderer>(true))
        {
            own.Add(r);
            SetMask(r, false);
        }
    }

    // The plate, 457.2 x 152.4 x 1.6 mm, 3.2 mm corner radius, at PlateAnchor in both phases (under the liner, then under
    // the static entrance): the same spot 75 mm from the opening, on the face that stands in that phase (C1-6: no shift).
    void MountPlates()
    {
        var mat = FrontRoomsSurfaces.TryGet(PlateMaterial);
        if (mat == null) { Debug.LogWarning("[StationDress] " + PlateMaterial + " missing: no BUSINESS HOURS plate"); return; }
        var mesh = PlateMesh();
        meshes.Add(mesh);
        set.PlateAnchor(out var pos, out var rot);
        var cleared = set.Entrance != null && set.Entrance.activeSelf;
        var step = ModuleUnits.WallHalf - (FrontRoomsStationSet.FacadeFace + FrontRoomsStationSet.LinerProud);
        var startPos = cleared ? pos - rot * Vector3.forward * step : pos;
        var clearPos = cleared ? pos : pos + rot * Vector3.forward * step;
        if (set.Liner != null) Plate(set.Liner.transform, "BUSINESS HOURS plate (start area)", startPos, rot, mesh, mat);
        if (set.Entrance != null) Plate(set.Entrance.transform, "BUSINESS HOURS plate (static entrance)", clearPos, rot, mesh, mat);
    }

    void Plate(Transform parent, string name, Vector3 worldPos, Quaternion worldRot, Mesh mesh, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(worldPos, worldRot);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        own.Add(r);
        SetMask(r, false);
    }

    /// <summary>The plate: a rounded rectangle 1.6 mm thick, its face toward +Z with 0..1 UVs reading left to right from the room.</summary>
    static Mesh PlateMesh()
    {
        float w = FrontRoomsStationSet.PlateWidth, h = FrontRoomsStationSet.PlateHeight, t = .0016f, r = .0032f;
        const float artW = .458f, artH = .153f;   // the art is 1832 x 612 px at 4 px/mm, centred on the plate
        var outline = new List<Vector2>();
        for (var c = 0; c < 4; c++)
        {
            var cx = (c == 0 || c == 3 ? 1 : -1) * (w * .5f - r);
            var cy = (c < 2 ? 1 : -1) * (h * .5f - r);
            for (var k = 0; k <= 6; k++)
            {
                var a = (c * 90f + k * 15f) * Mathf.Deg2Rad;
                outline.Add(new Vector2(cx + r * Mathf.Cos(a), cy + r * Mathf.Sin(a)));
            }
        }
        var v = new List<Vector3>();
        var uv = new List<Vector2>();
        var n = new List<Vector3>();
        var tri = new List<int>();
        Vector2 Uv(Vector2 p) => new Vector2(.5f - p.x / artW, .5f + p.y / artH);
        // Front (z = t) and back (z = 0) fans.
        var f0 = v.Count;
        v.Add(new Vector3(0f, 0f, t)); uv.Add(Uv(Vector2.zero)); n.Add(Vector3.forward);
        foreach (var p in outline) { v.Add(new Vector3(p.x, p.y, t)); uv.Add(Uv(p)); n.Add(Vector3.forward); }
        for (var i = 0; i < outline.Count; i++)
        {
            int a = f0 + 1 + i, b = f0 + 1 + (i + 1) % outline.Count;
            tri.Add(f0); tri.Add(a); tri.Add(b);
        }
        var b0 = v.Count;
        v.Add(new Vector3(0f, 0f, 0f)); uv.Add(Uv(Vector2.zero)); n.Add(Vector3.back);
        foreach (var p in outline) { v.Add(new Vector3(p.x, p.y, 0f)); uv.Add(Uv(p)); n.Add(Vector3.back); }
        for (var i = 0; i < outline.Count; i++)
        {
            int a = b0 + 1 + i, b = b0 + 1 + (i + 1) % outline.Count;
            tri.Add(b0); tri.Add(b); tri.Add(a);
        }
        // The edge band (the plate's black core shows on the edge: the albedo's border pixels).
        for (var i = 0; i < outline.Count; i++)
        {
            var p = outline[i]; var q = outline[(i + 1) % outline.Count];
            var e = new Vector2(q.y - p.y, p.x - q.x).normalized;
            var en = new Vector3(e.x, e.y, 0f);
            var k = v.Count;
            v.Add(new Vector3(p.x, p.y, 0f)); v.Add(new Vector3(q.x, q.y, 0f)); v.Add(new Vector3(q.x, q.y, t)); v.Add(new Vector3(p.x, p.y, t));
            for (var j = 0; j < 4; j++) { n.Add(en); uv.Add(new Vector2(.001f, .001f)); }
            tri.Add(k); tri.Add(k + 1); tri.Add(k + 2);
            tri.Add(k); tri.Add(k + 2); tri.Add(k + 3);
        }
        var m = new Mesh { name = "BUSINESS HOURS plate" };
        m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
        m.RecalculateBounds();
        m.RecalculateTangents();
        return m;
    }

    // ------------------------------------------------------------------ rendering layers (61 §7)
    /// <summary>Station only (bit 1 set, bit 0 cleared) or both layers (bit 1 set); never assigned, so G14's bits 29/30 stay.</summary>
    public static void SetMask(Renderer r, bool both)
    {
        if (r == null) return;
        r.renderingLayerMask |= StationBit;
        if (!both) r.renderingLayerMask &= ~DefaultBit;
    }

    void OnShellBuilt(GridCoord coord, IReadOnlyList<Renderer> renderers)
    {
        foreach (var r in renderers) SetMask(r, false);
        if (renderers.Count > 0) TagLamps(ChunkRoot(renderers[0].transform));
    }

    Transform ChunkRoot(Transform t)
    {
        while (t != null && t.parent != null && t.parent != map.transform) t = t.parent;
        return t;
    }

    // Chunks built before MapRunStarted (Begin builds round the start): find the station skin's renderers as the map does.
    void TagBuiltShell()
    {
        var wall = FrontRoomsSurfaces.TryGet(FrontRoomsMapWorld.StationWallSurface);
        var floor = FrontRoomsSurfaces.TryGet(FrontRoomsMapWorld.StationFloorSurface);
        var ceiling = FrontRoomsSurfaces.TryGet(FrontRoomsMapWorld.StationCeilingSurface);
        foreach (Transform chunk in map.transform)
        {
            if (chunk == set.transform) continue;
            var any = false;
            foreach (var r in chunk.GetComponentsInChildren<Renderer>(true))
            {
                var m = r.sharedMaterial;
                if (m == null || (m != wall && m != floor && m != ceiling)) continue;
                SetMask(r, false);
                any = true;
            }
            if (any) TagLamps(chunk);
        }
    }

    // The station cells' lamps (map fixtures "Fixture (x, y)/Light"): the two entrance lamps both layers, the others Station only.
    void TagLamps(Transform chunk)
    {
        if (chunk == null) return;
        foreach (Transform f in chunk)
        {
            if (!f.name.StartsWith("Fixture (", System.StringComparison.Ordinal)) continue;
            if (!TryCell(f.name, out var cell) || !map.InStation(cell)) continue;
            var lightT = f.Find("Light");
            var l = lightT != null ? lightT.GetComponent<Light>() : null;
            if (l == null) continue;
            var both = cell == entranceLampA || cell == entranceLampB;
            var m = (uint)l.renderingLayerMask | StationBit;
            if (!both) m &= ~DefaultBit;
            l.renderingLayerMask = (int)m;
        }
    }

    static bool TryCell(string name, out GridCoord cell)
    {
        cell = default;
        var a = name.IndexOf('(');
        var b = name.IndexOf(')');
        if (a < 0 || b < a) return false;
        var parts = name.Substring(a + 1, b - a - 1).Split(',');
        if (parts.Length != 2) return false;
        if (!int.TryParse(parts[0].Trim(), out var x) || !int.TryParse(parts[1].Trim(), out var y)) return false;
        cell = new GridCoord(x, y);
        return true;
    }

    // ------------------------------------------------------------------ per frame
    void Update()
    {
        if (map == null || set == null) return;
        // New set renderers (props spawned a few a frame): Station only.
        var rs = set.Renderers;
        for (; taggedSetRenderers < rs.Count; taggedSetRenderers++) SetMask(rs[taggedSetRenderers], false);
        // Anything else parented under the station (the placard re-parented by its mount, the picture, M3's key): Station only,
        // unless already classified (a renderer with the Station bit keeps its class, so the openings' both-layer trim stays).
        var count = set.transform.childCount;
        if (count != scannedChildren || (scanTimer -= Time.deltaTime) <= 0f)
        {
            scannedChildren = count;
            scanTimer = 1f;
            foreach (var r in set.GetComponentsInChildren<Renderer>(true))
                if ((r.renderingLayerMask & StationBit) == 0) SetMask(r, false);
        }
        if (!tvSearched && set.PropsDone) { tvSearched = true; SetUpTv(); }
        if (!keyTagged) FindKey();
        if (set.Visible != visible) ApplyVisibility(set.Visible);
    }

    void ApplyVisibility(bool show)
    {
        visible = show;
        foreach (var r in own)
            if (r != null) r.enabled = show;
        if (screenMount != null) screenMount.SetActive(show);
        if (tvLight != null) tvLight.gameObject.SetActive(show);
    }

    // The floor key + tag (M3's spawn, P06): the Station bit, so the set's light reaches it (C1-3). M3 is asked to parent it
    // under map.Station; until then it is found at the KeySpot.
    void FindKey()
    {
        keySearch -= Time.deltaTime;
        if (keySearch > 0f) return;
        keySearch = .5f;
        if (!map.StationAnchor(ModuleMarkerKind.KeySpot, out var spot, out _, out _)) return;
        var found = 0;
        foreach (var r in set.GetComponentsInChildren<Renderer>(true))
            if (IsKey(r.transform) && (r.bounds.center - spot).sqrMagnitude < .09f) { SetMask(r, false); found++; }
        foreach (var c in Physics.OverlapSphere(spot, .3f, ~0, QueryTriggerInteraction.Collide))
            foreach (var r in c.GetComponentsInChildren<Renderer>(true))
                if (IsKey(r.transform)) { SetMask(r, false); found++; }
        if (found > 0) keyTagged = true;
    }

    // M3's floor key (FrontRoomsMapWorld.FloorKeyName, NewKey's object: today the map's key cube, D1.6's model later) or the
    // D1.6 key kits (Kit_KeyRing, Kit_Key_*, Kit_KeyTag_*), never the key's host or the cabinet.
    static bool IsKey(Transform t)
    {
        for (; t != null; t = t.parent)
        {
            if (t.name == FrontRoomsMapWorld.FloorKeyName) return true;
            if (t.name.StartsWith("Kit_Key", System.StringComparison.Ordinal) && !t.name.StartsWith("Kit_KeySwitch", System.StringComparison.Ordinal)
                && !t.name.StartsWith("Kit_KeyCabinet", System.StringComparison.Ordinal)) return true;
        }
        return false;
    }

    // ------------------------------------------------------------------ the set
    void SetUpTv()
    {
        var props = set.transform.Find("Station props");
        if (props == null) return;
        foreach (Transform p in props)
        {
            if (p.name == TvKit && tv == null) tv = p.gameObject;
            else if (p.name == BracketKit && bracket == null) bracket = p.gameObject;
        }
        if (tv == null) { Debug.LogWarning("[StationDress] " + TvKit + " was not placed: no station TV"); return; }
        if (bracket != null) Reseat();
        // The picture: a scaled mount so ScreenVideo's default quad lands on Kit_CRTTV_Station's picture (GAP 4; no
        // ScreenVideo edit for the anchor), attached as the floor's playing station TV with 声音设计's Beacon role.
        screenMount = new GameObject("screen mount (station)");
        screenMount.transform.SetParent(tv.transform, false);
        var scale = new Vector3(PictureSize.x / DefaultSize.x, PictureSize.y / DefaultSize.y, 1f);
        screenMount.transform.localScale = scale;
        screenMount.transform.localPosition = PictureCentre - Vector3.Scale(scale, DefaultSurface);
        screen = FrontRoomsScreenVideo.Attach(screenMount, null, FrontRoomsScreenSoundRole.Beacon);
        AddPictureToLods();
        foreach (var r in screenMount.GetComponentsInChildren<Renderer>(true)) SetMask(r, false);
        tvLight = FrontRoomsStationTvLight.Create(tv.transform, new Vector3(0f, PictureCentre.y, .245f), map, screen);
        if (!visible) ApplyVisibility(false);
        Debug.Log("[StationDress] set re-seated on " + (bracket != null ? BracketKit : "(no bracket)") + ", attached (Beacon), light on; screen centre "
                  + tv.transform.TransformPoint(PictureCentre).ToString("F3"));
    }

    bool tvSearched;

    // P02 on P01's set_mount: the set's back-bottom edge on the hinge, pitched 20 deg front-down (plan 70 §8 step 1).
    void Reseat()
    {
        var bInfo = FrontRoomsKitLibrary.GetInfo(BracketKit);
        var tInfo = FrontRoomsKitLibrary.GetInfo(TvKit);
        if (bInfo == null || tInfo == null || !bInfo.TryAnchor("set_mount", out var mountLocal)) return;
        const float pitch = 20f;
        var rot = bracket.transform.rotation * Quaternion.Euler(pitch, 0f, 0f);
        var hinge = bracket.transform.TransformPoint(mountLocal);
        var backBottom = new Vector3(0f, tInfo.Min.y, tInfo.Min.z);
        tv.transform.SetPositionAndRotation(hinge - rot * backBottom, rot);
    }

    // C2-5b: ScreenVideo adds its quad to a LODGroup on the Attach target only; the mount has none, so the set's own group
    // gets the picture in every level, and the picture switches and culls with the set.
    void AddPictureToLods()
    {
        var group = tv.GetComponent<LODGroup>();
        if (group == null) return;
        var lods = group.GetLODs();
        if (lods.Length == 0) return;
        var quads = new List<Renderer>();
        foreach (var r in screenMount.GetComponentsInChildren<Renderer>(true)) quads.Add(r);
        for (var i = 0; i < lods.Length; i++)
        {
            var list = new List<Renderer>(lods[i].renderers ?? new Renderer[0]);
            foreach (var q in quads) if (!list.Contains(q)) list.Add(q);
            lods[i].renderers = list.ToArray();
        }
        group.SetLODs(lods);
    }

    // The take (50 §6): the light falls (its own handler) and the set goes off for good.
    void OnKeyTaken(Vector3 at)
    {
        if (screen != null) screen.PowerOffForGood();
    }
}
