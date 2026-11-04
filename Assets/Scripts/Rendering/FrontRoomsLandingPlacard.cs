using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

/// <summary>
/// BS6 (research/tutorial/20_requests.md, 教程设计's row, "V6 now"; 10_proposal.md §5.4; 叙事 research/narrative/
/// 40_floors_exit_tutorial_strings.md §3): the copy of the evacuation plan on every stairwell landing. On each floor below
/// LEVEL 0 (the run's floor 2 = LEVEL B1, ...) the Q16 placard hangs on the MAP side of the stairwell's far (north) door,
/// at P1 west, exactly as Floor 1's title placard did beside the start door (FrontRoomsPlacard.Resolve, the same kit,
/// height, offsets, keep-clear rect and glow), so the sheet's YOU ARE HERE is true. FrontRoomsPlacard.Prepare stays the
/// title stream's (Floor 1); this is its per-floor counterpart.
///
/// Hook: FrontRooms3DGame.FloorChanged (raised after NextFloor, the floor's EXIT and station plans, SetExitLocked and Begin,
/// which builds nothing), subscribed directly (WebGL High stripping). One component per map; each floor change retires the
/// last floor's copy (its stairwell is two floors back) and hangs this floor's:
/// - only where 关卡设计's G1 rule for floors 2+ holds (FrontRoomsExitPlanner.StairDoorPlacardWest: the door cell's west
///   edge Open, a wall on the west cell's south edge to hang it on). Where it does not, the floor hangs no sheet (a P1 east
///   or P2 copy would draw YOU ARE HERE on the wrong side of the door);
/// - the sheet for THAT floor's own state (叙事 40 §3, A35): the full sheet (the security desk, the padlocked stair door)
///   when the floor has its own station and a locked EXIT (map.FloorHasStation and map.ExitLocked at the change), else the
///   Phase 0 sheet; with its LEVEL token, B10 and deeper LEVEL B10+ (FrontRoomsLandingPlacardSheets, generated with the
///   packed prints by Tools/lookdev/pack_landing_placards.py from 平面视觉's art, used as delivered);
/// - its EXAMINE transcript and regions from that sheet's examine.json (FrontRoomsInspectCatalog.SetTranscript);
/// - one floor's print at a time, made once at the hang (token patches, Red 2026-10-31 "(c)"): the variant's base print
///   (LEVEL B1's sheet) with this floor's token patches copied over it (FrontRoomsLandingPlacardSheets: the texels where
///   its LEVEL B&lt;n&gt; differs from B1). Desktop, iPhone and iPad copy the patches' compressed blocks into a GPU copy of the
///   base (Graphics.CopyTexture, the memory of one print, levels 0-5 at 4096 from the patches' own mips, which equal the
///   sheet's there); WebGL (no copy between compressed textures) blits into an sRGB RenderTexture of the base's size
///   (1024) and regenerates its mips. B1 hangs the base itself; a missing patch or no working path hangs the base (never
///   a blank). The base and the patches unload one frame after the composite is made; the composite is destroyed (its
///   RenderTexture released) when the copy goes. The full sheets' Locked glow is loaded at the hang and unloaded one frame
///   after the copy goes; Prop_EvacPlan_E (the Phase 0 glow, shared with the kit's own material) is never unloaded.
///   Nothing of this runs per frame.
/// The frame shows only while its wall's chunk and the west cell's are built (as the stairwell does, D8); it goes for good when the start
/// area is given back (the stairwell removed, its door no longer there) or when a rebuilt chunk no longer has the wall.
/// Desktop, iPhone and iPad hang the same copy (the lens kit too); WebGL keeps the title mount's rule: no lens.
/// World text follows the language (2026-11-03, FrontRoomsWorldText): the base prints, patches and glows are swapped bases,
/// so the composite is made from the language on screen, and made again when it changes (Applied); the copies are dropped
/// through FrontRoomsWorldText.Unload, which keeps a swapped base loaded. A language whose token sits elsewhere on the sheet
/// moves that token's patch (same size) through its own Prop_EvacPlan_A_Landing_rects (FrontRoomsWorldText.Text; English's
/// lists none): 简体中文's title band sets the token further left, so its B2-B9 Title0 patches sit at x 3456 (RectsFor).
/// </summary>
[AddComponentMenu("")]
[DisallowMultipleComponent]
public sealed class FrontRoomsLandingPlacard : MonoBehaviour
{
    public const string ObjectName = "Landing placard";

    // WebGL (a separate track, placard 10_spec §6): no lens renderer, as FrontRoomsPlacardMount.
#if (UNITY_WEBGL && !UNITY_EDITOR) || FRONTROOMS_WEBGL_PREVIEW
    static readonly bool SpawnLens = false;
#else
    static readonly bool SpawnLens = true;
#endif

    // WebGL (its own track): the print is composited in a RenderTexture. WebGL 2 cannot copy between compressed textures.
#if (UNITY_WEBGL && !UNITY_EDITOR) || FRONTROOMS_WEBGL_PREVIEW
    static readonly bool BlockCopy = false;
#else
    static readonly bool BlockCopy = true;
#endif

    /// <summary>The shared Phase 0 glow (main's Prop_EvacPlan_E, also the kit material's emission map): never unloaded here.</summary>
    const string SharedGlow = "Surfaces/Textures/Prop_EvacPlan_E";

    // ---------------------------------------------------------------- bootstrap

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Boot()
    {
        FrontRooms3DGame.FloorChanged -= OnFloorChanged;
        FrontRooms3DGame.FloorChanged += OnFloorChanged;
    }

    static void OnFloorChanged(FrontRoomsMapWorld map, FrontRoomsMapHunter relay, int floor)
    {
        if (map == null) return;
        var landing = map.GetComponent<FrontRoomsLandingPlacard>();
        if (landing == null) landing = map.gameObject.AddComponent<FrontRoomsLandingPlacard>();
        landing.Hang(map, relay, floor);
    }

    // ---------------------------------------------------------------- the language (FrontRoomsWorldText)

    /// <summary>A language's patch moves (Resources path, through FrontRoomsWorldText.Text): English's lists none.</summary>
    public const string RectsPath = "Surfaces/Textures/Prop_EvacPlan_A_Landing_rects";

    [System.Serializable] sealed class RectMove { public string name; public int[] rect; }
    [System.Serializable] sealed class RectMoves { public RectMove[] patches; }

    static string rectsCode;
    static readonly Dictionary<string, int[]> rectMoves = new Dictionary<string, int[]>();

    /// <summary>The sheet's patch rects in the language on screen: the table's, with the language's moves applied.</summary>
    public static int[] RectsFor(FrontRoomsLandingPlacardSheets.Sheet sheet)
    {
        if (sheet == null || sheet.rects == null) return null;
        var code = FrontRoomsWorldText.AppliedCode;
        if (rectsCode != code)
        {
            rectsCode = code;
            rectMoves.Clear();
            var text = FrontRoomsWorldText.Text(RectsPath);
            var moves = text != null ? JsonUtility.FromJson<RectMoves>(text.text) : null;
            if (text != null) Resources.UnloadAsset(text);
            if (moves != null && moves.patches != null)
                foreach (var m in moves.patches)
                    if (m != null && !string.IsNullOrEmpty(m.name) && m.rect != null && m.rect.Length == 4) rectMoves[m.name] = m.rect;
        }
        if (rectMoves.Count == 0 || sheet.patches == null) return sheet.rects;
        var rects = (int[])sheet.rects.Clone();
        for (var i = 0; i < sheet.patches.Length && 4 * i + 3 < rects.Length; i++)
        {
            if (!rectMoves.TryGetValue(System.IO.Path.GetFileName(sheet.patches[i]), out var r)) continue;
            // a move keeps the piece's size (the copy swaps into the same texture)
            if (r[2] != rects[4 * i + 2] || r[3] != rects[4 * i + 3]) continue;
            rects[4 * i] = r[0];
            rects[4 * i + 1] = r[1];
        }
        return rects;
    }

    void OnEnable() => FrontRoomsWorldText.Applied += OnWorldTextApplied;

    void OnDisable() => FrontRoomsWorldText.Applied -= OnWorldTextApplied;

    // The language changed: the base and the patches hold its pixels now, but the composite was made from the old ones. B1
    // hangs the base itself and follows by itself; a composite is made again, bound, then the old one goes.
    void OnWorldTextApplied()
    {
        if (root == null || Sheet == null || Glow == null || Glow.Instance == null || composite == null) return;
        var old = composite;
        composite = null;
        var print = LoadPrint(Sheet, "LEVEL " + Sheet.level + " (the language changed)");
        if (composite == null)
        {
            // no composite came out (a patch missing): keep the old one rather than hang B1's token
            composite = old;
            return;
        }
        sheetPrint = print;
        Glow.Instance.SetTexture("_BaseMap", sheetPrint);
        DestroyTexture(old);
    }

    /// <summary>The map's landing placard component, or null (tools and tests).</summary>
    public static FrontRoomsLandingPlacard Of(FrontRoomsMapWorld map) => map != null ? map.GetComponent<FrontRoomsLandingPlacard>() : null;

    // ---------------------------------------------------------------- state

    FrontRoomsMapWorld map;
    GameObject root;
    GridCoord stairCell, doorCell, westCell, wallCell;
    bool wasBuilt;
    // sheetPrint: the _BaseMap (the base itself, or composite); composite: made here, destroyed at the floor change.
    Texture sheetPrint, composite;
    Texture2D sheetGlow;
    readonly List<Texture2D> unloadNextFrame = new List<Texture2D>();
    int retiredFrame = -1;

    /// <summary>The floor of the run this copy was hung for (2 = LEVEL B1), 0 before the first change.</summary>
    public int Floor { get; private set; }
    /// <summary>The sheet hung (null: none on this floor).</summary>
    public FrontRoomsLandingPlacardSheets.Sheet Sheet { get; private set; }
    /// <summary>Why this floor hangs no copy (null when one hangs).</summary>
    public string NoSheetReason { get; private set; }
    public FrontRoomsPlacard.Spot Spot { get; private set; }
    public bool Placed => root != null && Frame != null;
    public GameObject Root => root;
    public GameObject Frame { get; private set; }
    public GameObject Lens { get; private set; }
    public FrontRoomsPlacardGlow Glow { get; private set; }
    /// <summary>This floor's _BaseMap: the base print, or the composite this component made (null: no copy).</summary>
    public Texture Print => sheetPrint;
    /// <summary>
    /// How the print was made: "base" (B1: the base is its sheet), "blocks" (the patches' compressed blocks in a GPU copy of the
    /// base), "blit" (an sRGB RenderTexture), "base (fallback: ...)" (a patch missing or no path ran); null when no copy hangs.
    /// </summary>
    public string PrintMode { get; private set; }

    /// <summary>
    /// Hang this floor's copy (FloorChanged; public for tools). Retires the last floor's first. Floors below 2 hang none
    /// (LEVEL 0 has the title / station placard).
    /// </summary>
    public void Hang(FrontRoomsMapWorld world, FrontRoomsMapHunter relay, int floor)
    {
        Retire(true);
        map = world;
        Floor = floor;
        Sheet = null;
        NoSheetReason = null;
        Spot = default;
        if (map == null || floor < 2) { NoSheetReason = "LEVEL 0"; return; }
        var label = "LEVEL B" + (floor - 1) + " (floor " + floor + ")";
        var stair = StartStairwell(map);
        if (stair == null || map.Cache == null)
        {
            NoSheetReason = "no start stairwell";
            Debug.LogWarning("[Placard] landing " + label + ": no start stairwell on the map; no copy this floor");
            return;
        }
        stairCell = stair.Cell;
        doorCell = stair.NorthDoorCell;
        westCell = new GridCoord(doorCell.x - 1, doorCell.y);
        // The wall the frame hangs on is the north edge of the cell south of W, so that cell's chunk builds it (each chunk
        // builds its cells' east and north edges); W's chunk builds W and the open D-W side.
        wallCell = new GridCoord(westCell.x, westCell.y - 1);
        if (!FrontRoomsExitPlanner.StairDoorPlacardWest(map.Cache, doorCell))
        {
            // G1 for floors 2+ did not hold on this seed (SeedFor prefers it; the validator counts placardWestMissing).
            NoSheetReason = "StairDoorPlacardWest false (west " + map.Cache.Edge(doorCell, westCell) + ", wall under it "
                            + map.Cache.Edge(westCell, wallCell) + ")";
            Debug.Log("[Placard] landing " + label + ": no sheet: " + NoSheetReason + ", door cell " + doorCell);
            return;
        }
        var full = map.FloorHasStation && map.ExitLocked;
        var sheet = FrontRoomsLandingPlacardSheets.For(floor - 1, full);
        // The far door's centre on its line, at floor level; the frame sits on the map wall's north face (centred on the line).
        var doorPoint = stair.NorthDoorPoint;
        var spot = FrontRoomsPlacard.Resolve(map, doorPoint, doorPoint.z + ModuleUnits.WallHalf);
        if (spot.mount != FrontRoomsPlacard.Mount.P1Facade || spot.side >= 0 || spot.doorCell != doorCell)
        {
            NoSheetReason = "the spot resolved to " + FrontRoomsPlacard.Label(spot.mount) + (spot.side < 0 ? " west" : " east") + " at door cell " + spot.doorCell;
            Debug.LogWarning("[Placard] landing " + label + ": " + NoSheetReason + ", not P1 west at " + doorCell + "; no copy this floor");
            return;
        }
        Spot = spot;
        map.KeepClearAtStart(spot.keepClear);
        root = new GameObject(ObjectName + " " + sheet.level);
        root.transform.SetParent(map.transform, false);
        Frame = FrontRoomsKitLibrary.Spawn(FrontRoomsPlacard.KitName, root.transform, Vector3.zero, Quaternion.identity, null, false, "evacuation placard (landing)");
        if (Frame == null)
        {
            NoSheetReason = FrontRoomsPlacard.KitName + " missing";
            Debug.LogWarning("[Placard] " + FrontRoomsPlacard.KitName + " is missing; no landing copy");
            Retire(false);
            return;
        }
        Frame.transform.SetPositionAndRotation(spot.position, spot.rotation);
        var renderers = new List<Renderer>(Frame.GetComponentsInChildren<Renderer>(true));
        if (SpawnLens)
        {
            Lens = FrontRoomsKitLibrary.Spawn(FrontRoomsPlacard.LensKitName, root.transform, Vector3.zero, Quaternion.identity, null, false, "evacuation placard lens (landing)");
            if (Lens != null) Lens.transform.SetPositionAndRotation(spot.position, spot.rotation);
        }
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = true;
        }
        Glow = Frame.AddComponent<FrontRoomsPlacardGlow>();
        Glow.Bind(map, relay, spot.glowCell, renderers);
        sheetPrint = LoadPrint(sheet, label);
        sheetGlow = Resources.Load<Texture2D>(sheet.glow);
        if (Glow.Instance != null && sheetPrint != null && sheetGlow != null)
        {
            Glow.Instance.SetTexture("_BaseMap", sheetPrint);
            Glow.Instance.SetTexture("_EmissionMap", sheetGlow);
        }
        else Debug.LogWarning("[Placard] landing " + label + ": sheet textures " + sheet.print + " / " + sheet.glow + " missing; the copy keeps the kit's default sheet");
        var inspectable = Frame.GetComponent<FrontRoomsInspectable>();
        if (inspectable != null && inspectable.Def != null)
        {
            FrontRoomsInspectCatalog.SetTranscript(sheet.transcript, sheet.lines);
            var def = inspectable.Def.Clone();
            def.transcript = sheet.transcript;
            def.regions = FrontRoomsLandingPlacardSheets.Regions(sheet);
            inspectable.Configure(inspectable.Kit, def, FrontRoomsKitLibrary.GetInfo(FrontRoomsPlacard.KitName));
        }
        Sheet = sheet;
        // Nothing of the floor is built yet (Begin built nothing): the copy shows with its wall (Update).
        wasBuilt = false;
        root.SetActive(false);
        Debug.Log("[Placard] landing " + label + ": P1 west, sheet " + sheet.folder + (full ? " (station, EXIT locked)" : " (no station of its own)")
                  + ", print " + PrintMode + ", door cell " + doorCell + ", glow cell " + spot.glowCell + ", at " + spot.position.ToString("F4"));
    }

    void Update()
    {
        if (unloadNextFrame.Count > 0 && Time.frameCount > retiredFrame)
        {
            // The retired copy's material instance went at the end of the frame it was retired in: nothing holds these any more.
            foreach (var t in unloadNextFrame)
                if (t != null && t != sheetPrint && t != sheetGlow) FrontRoomsWorldText.Unload(t);
            unloadNextFrame.Clear();
        }
        if (root == null || map == null) return;
        // The start area given back: the stairwell and its door are gone, so the plan's YOU ARE HERE is no longer true.
        if (!map.HasStartArea || !map.InStartArea(stairCell))
        {
            Retire(true);
            return;
        }
        // Shown only while both chunks stand: the wall's (the cell south of W) and W's. A revisit shift needs a drop first, so
        // every rebuild passes through "not built" and is checked here.
        var built = map.IsChunkBuiltAt(wallCell) && map.IsChunkBuiltAt(westCell);
        if (built && !wasBuilt && !WallStands())
        {
            // A revisit shift rebuilt a chunk without the wall (or with the door side walled): the copy goes.
            Debug.Log("[Placard] landing LEVEL B" + (Floor - 1) + ": the wall at " + westCell + " is gone; the copy is taken down");
            Retire(true);
            return;
        }
        wasBuilt = built;
        if (root.activeSelf != built) root.SetActive(built);
    }

    bool WallStands() =>
        map.Cache != null && map.Cache.Edge(doorCell, westCell) == EdgeKind.Open
        && map.Cache.Edge(westCell, wallCell) == EdgeKind.Wall;

    // The stairwell the map's floor started in (its start area is that one cell).
    static FrontRoomsStairwell StartStairwell(FrontRoomsMapWorld map)
    {
        foreach (var s in FindObjectsByType<FrontRoomsStairwell>())
            if (s != null && s.Map == map && map.InStartArea(s.Cell)) return s;
        return null;
    }

    /// <summary>Take the copy down (deactivated now, destroyed at the end of the frame); its textures unload next frame.</summary>
    void Retire(bool unload)
    {
        if (root != null)
        {
            root.SetActive(false);
            if (Application.isPlaying) Destroy(root);
            else DestroyImmediate(root);
        }
        root = null;
        Frame = null;
        Lens = null;
        Glow = null;
        if (unload)
        {
            retiredFrame = Time.frameCount;
            // The base hung as the print (B1, or a fallback): a Resources asset, unloaded next frame like the glow.
            if (sheetPrint != composite && sheetPrint is Texture2D hungBase) unloadNextFrame.Add(hungBase);
            if (sheetGlow != null && sheetGlow.name != System.IO.Path.GetFileName(SharedGlow)) unloadNextFrame.Add(sheetGlow);
        }
        // The composite is this component's own: destroyed now (its RenderTexture released); the copy no longer draws.
        DestroyTexture(composite);
        composite = null;
        sheetPrint = null;
        sheetGlow = null;
        PrintMode = null;
    }

    void OnDestroy()
    {
        // The map goes (a restart reloads the scene): the copy, a child of the map, goes with it, and the scene load unloads
        // what nothing holds. Nothing of the copy is touched here (the root may be mid-destroy); only the composite, which
        // this component made, is destroyed.
        root = null;
        Frame = null;
        Lens = null;
        Glow = null;
        DestroyTexture(composite);
        composite = null;
        sheetPrint = null;
        sheetGlow = null;
        PrintMode = null;
        unloadNextFrame.Clear();
    }

    // ---------------------------------------------------------------- the print: base + token patches, once per floor

    /// <summary>
    /// This floor's print (Hang): the variant's base for B1; else the base with the sheet's token patches composited over it
    /// (blocks on desktop and phones, a RenderTexture on WebGL or where blocks cannot be copied). The patches, and the base
    /// once composited, are queued to unload next frame. A missing patch or no working path: the base (never a blank).
    /// </summary>
    Texture LoadPrint(FrontRoomsLandingPlacardSheets.Sheet sheet, string label)
    {
        var basePrint = Resources.Load<Texture2D>(sheet.print);
        if (basePrint == null) return null;
        var count = sheet.patches != null ? sheet.patches.Length : 0;
        if (count == 0)
        {
            PrintMode = "base";
            return basePrint;
        }
        var patches = new Texture2D[count];
        string missing = null;
        for (var i = 0; i < count; i++)
        {
            patches[i] = Resources.Load<Texture2D>(sheet.patches[i]);
            if (patches[i] == null && missing == null) missing = sheet.patches[i];
        }
        Texture made = null;
        var how = "blocks";
        var rects = RectsFor(sheet);
        if (missing == null && rects != null && rects.Length == count * 4)
        {
            var name = basePrint.name + " " + sheet.level;
            if (BlockCopy) made = CompositeBlocks(basePrint, patches, rects, name);
            if (made == null)
            {
                how = "blit";
                made = CompositeBlit(basePrint, patches, rects, name);
            }
        }
        // The patches are in the composite now (or of no use): they go next frame.
        retiredFrame = Time.frameCount;
        foreach (var p in patches)
            if (p != null) unloadNextFrame.Add(p);
        if (made == null)
        {
            PrintMode = "base (fallback: " + (missing != null ? missing + " missing" : "no composite path") + ")";
            Debug.LogWarning("[Placard] landing " + label + ": " + PrintMode + "; the copy hangs " + sheet.print + " (LEVEL B1's token)");
            return basePrint;
        }
        // The composite holds every texel of the base: the base goes next frame too.
        unloadNextFrame.Add(basePrint);
        composite = made;
        PrintMode = how;
        return made;
    }

    /// <summary>The base's size as a power-of-two fraction of the packed print's (0 at 4096 x 2048), or -1.</summary>
    static int SizeShift(int w, int h)
    {
        for (var s = 0; s < 12; s++)
            if ((FrontRoomsLandingPlacardSheets.PrintWidth >> s) == w && (FrontRoomsLandingPlacardSheets.PrintHeight >> s) == h) return s;
        return -1;
    }

    /// <summary>
    /// Desktop, iPhone, iPad: a GPU-only texture in the base's own compressed format (BC7 / ASTC 4x4: the same memory as one
    /// print), the base copied in (every level), then each patch's blocks copied over its rect level by level while the rect
    /// stays on the block grid (levels 0-5 at 4096: the patch's own box mips equal the sheet's there, its rect being on the
    /// 128-texel grid); smaller levels keep the base's (the sheet is 64 texels wide there, no token legible). Null when the
    /// device cannot copy textures, a format or size does not match, or a rect is off the block grid at level 0.
    /// </summary>
    static Texture2D CompositeBlocks(Texture2D basePrint, Texture2D[] patches, int[] rects, string name)
    {
        if ((SystemInfo.copyTextureSupport & CopyTextureSupport.Basic) == 0) return null;
        var format = basePrint.graphicsFormat;
        int w = basePrint.width, h = basePrint.height, mips = basePrint.mipmapCount;
        int bw = (int)GraphicsFormatUtility.GetBlockWidth(format), bh = (int)GraphicsFormatUtility.GetBlockHeight(format);
        var shift = SizeShift(w, h);
        if (shift < 0 || basePrint.activeMipmapLimit != 0) return null;
        // Plan every copy before making anything: the patch level that is its rect at the base's size, and how many levels
        // stay on the block grid. Texel rows run from the bottom (ty).
        var first = new int[patches.Length];
        var levels = new int[patches.Length];
        for (var i = 0; i < patches.Length; i++)
        {
            var p = patches[i];
            if (p.graphicsFormat != format || p.activeMipmapLimit != 0) return null;
            int rx = rects[i * 4] >> shift, rw = rects[i * 4 + 2] >> shift, rh = rects[i * 4 + 3] >> shift;
            var ty = h - ((rects[i * 4 + 1] + rects[i * 4 + 3]) >> shift);
            var d = 0;
            while ((p.width >> d) > rw && d < p.mipmapCount - 1) d++;
            if ((p.width >> d) != rw || (p.height >> d) != rh) return null;
            var n = 0;
            while (n < mips && d + n < p.mipmapCount
                   && rx % (bw << n) == 0 && ty % (bh << n) == 0 && rw % (bw << n) == 0 && rh % (bh << n) == 0) n++;
            if (n == 0) return null;
            first[i] = d;
            levels[i] = n;
        }
        Texture2D dst;
        try
        {
            dst = new Texture2D(w, h, format, mips, TextureCreationFlags.DontInitializePixels)
            {
                name = name + " (composite)",
                filterMode = basePrint.filterMode,
                anisoLevel = basePrint.anisoLevel,
                wrapMode = basePrint.wrapMode,
                mipMapBias = basePrint.mipMapBias,
                hideFlags = HideFlags.DontSave,
            };
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Placard] landing print: no " + format + " texture (" + e.Message + "); the RenderTexture path");
            return null;
        }
        // On the GPU now, no CPU copy kept; every texel is written by the copies below.
        dst.Apply(false, true);
        Graphics.CopyTexture(basePrint, dst);
        for (var i = 0; i < patches.Length; i++)
        {
            int rx = rects[i * 4] >> shift, rw = rects[i * 4 + 2] >> shift, rh = rects[i * 4 + 3] >> shift;
            var ty = h - ((rects[i * 4 + 1] + rects[i * 4 + 3]) >> shift);
            for (var m = 0; m < levels[i]; m++)
                Graphics.CopyTexture(patches[i], 0, first[i] + m, 0, 0, rw >> m, rh >> m, dst, 0, m, rx >> m, ty >> m);
        }
        return dst;
    }

    static Material patchMaterial;

    /// <summary>
    /// WebGL, or where blocks cannot be copied: an sRGB RenderTexture of the base's size (1024 on WebGL), the base blitted in,
    /// each patch drawn over its rect (a quad, UI/Default: always included, white, nothing added; the patch's own mips give
    /// the base's scale), then the mips regenerated. Linear light throughout (sRGB decode on read, encode on write). Null when
    /// the shader or the RenderTexture is not there.
    /// </summary>
    static RenderTexture CompositeBlit(Texture2D basePrint, Texture2D[] patches, int[] rects, string name)
    {
        if (patchMaterial == null)
        {
            var shader = Shader.Find("UI/Default");
            if (shader == null) return null;
            patchMaterial = new Material(shader) { name = "FrontRooms landing placard patch", hideFlags = HideFlags.HideAndDontSave };
            patchMaterial.SetColor("_Color", Color.white);
            patchMaterial.SetVector("_TextureSampleAdd", Vector4.zero);
        }
        var desc = new RenderTextureDescriptor(basePrint.width, basePrint.height, RenderTextureFormat.ARGB32, 0)
        {
            sRGB = true, useMipMap = true, autoGenerateMips = false, msaaSamples = 1,
        };
        var rt = new RenderTexture(desc)
        {
            name = name + " (composite RT)",
            filterMode = basePrint.filterMode,
            anisoLevel = basePrint.anisoLevel,
            wrapMode = basePrint.wrapMode,
            mipMapBias = basePrint.mipMapBias,
            hideFlags = HideFlags.DontSave,
        };
        if (!rt.Create())
        {
            DestroyTexture(rt);
            return null;
        }
        Graphics.Blit(basePrint, rt);
        float pw = FrontRoomsLandingPlacardSheets.PrintWidth, ph = FrontRoomsLandingPlacardSheets.PrintHeight;
        var active = RenderTexture.active;
        RenderTexture.active = rt;
        GL.PushMatrix();
        GL.LoadOrtho();
        for (var i = 0; i < patches.Length; i++)
        {
            // The rect in the texture's UVs (v from the bottom), as Graphics.Blit maps a full quad.
            float x0 = rects[i * 4] / pw, x1 = (rects[i * 4] + rects[i * 4 + 2]) / pw;
            float y0 = 1f - (rects[i * 4 + 1] + rects[i * 4 + 3]) / ph, y1 = 1f - rects[i * 4 + 1] / ph;
            patchMaterial.mainTexture = patches[i];
            patchMaterial.SetPass(0);
            GL.Begin(GL.QUADS);
            GL.Color(Color.white);
            GL.TexCoord2(0f, 0f); GL.Vertex3(x0, y0, 0f);
            GL.TexCoord2(0f, 1f); GL.Vertex3(x0, y1, 0f);
            GL.TexCoord2(1f, 1f); GL.Vertex3(x1, y1, 0f);
            GL.TexCoord2(1f, 0f); GL.Vertex3(x1, y0, 0f);
            GL.End();
        }
        GL.PopMatrix();
        RenderTexture.active = active;
        patchMaterial.mainTexture = null;
        rt.GenerateMips();
        return rt;
    }

    static void DestroyTexture(Texture t)
    {
        if (t == null) return;
        if (t is RenderTexture rt) rt.Release();
        if (Application.isPlaying) Destroy(t);
        else DestroyImmediate(t);
    }
}
