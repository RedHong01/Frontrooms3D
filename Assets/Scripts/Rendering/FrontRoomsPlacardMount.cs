using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The evacuation placard's mount on the terminal stream room (FrontRoomsPlacard.Prepare).
/// Waits for FrontRooms3DGame.MapRunStarted (fired in StartRunInPlace after EndStreamAt
/// and map.Begin; rooms are dressed one a frame from Update, so a keep-clear rect
/// registered here is in time), then once: resolves the spot (§5.2), asks the map to
/// keep its floor clear (map.KeepClearAtStart, CONTRACT Q16-1, in main since 10-08),
/// spawns the kits render-only with shadow casting OFF on every renderer
/// (KitLibrary would turn it on: the frame is over 0.3 m), and binds the glow.
/// research/placard/10_spec.md §4.6, §5.
/// </summary>
[AddComponentMenu("")]
public sealed class FrontRoomsPlacardMount : MonoBehaviour
{
    public const string ObjectName = "Placard mount";

    // WebGL (a separate track, §6): no lens renderer, 2 draws. Desktop and the Editor are the reference.
#if (UNITY_WEBGL && !UNITY_EDITOR) || FRONTROOMS_WEBGL_PREVIEW
    static readonly bool SpawnLens = false;
#else
    static readonly bool SpawnLens = true;
#endif

    Vector3 doorPoint;
    float facadeZ;
    bool armed, retired;

    public FrontRoomsPlacard.Spot Spot { get; private set; }
    public bool Placed { get; private set; }
    public GameObject Frame { get; private set; }
    public GameObject Lens { get; private set; }
    public FrontRoomsPlacardGlow Glow { get; private set; }

    public void Arm(Vector3 doorPointWorld, float facadeFaceZWorld)
    {
        doorPoint = doorPointWorld;
        facadeZ = facadeFaceZWorld;
        if (armed || retired) return;
        armed = true;
        FrontRooms3DGame.MapRunStarted += OnRunStarted;
    }

    /// <summary>
    /// FrontRoomsPlacard.Prepare replaced this mount: stop listening at once (its Destroy lands only at the end of
    /// the frame) and never place.
    /// </summary>
    public void Retire()
    {
        retired = true;
        if (armed) FrontRooms3DGame.MapRunStarted -= OnRunStarted;
        armed = false;
    }

    void OnRunStarted(FrontRoomsMapWorld map, FrontRoomsMapHunter relay)
    {
        FrontRooms3DGame.MapRunStarted -= OnRunStarted;
        armed = false;
        if (this == null || retired || map == null || Placed) return;
        Place(map, relay);
    }

    /// <summary>Resolve, keep clear, spawn and bind. Public for the lookdev and placement tests.</summary>
    public void Place(FrontRoomsMapWorld map, FrontRoomsMapHunter relay)
    {
        // A26 station-look-v1 (KIT 游戏视觉 2; plan 4.8; research/station/70_station_look_v1_plan.md §6.1): on a floor with
        // the station the placard hangs at the station's Placard anchor (north wall, x 5.5, 1.56 m, facing D). With no
        // station (stationModule empty) FloorHasStation is false and the code below runs exactly as before.
        if (map.FloorHasStation && map.Station != null && map.StationAnchor(ModuleMarkerKind.Placard, out var stationPos, out var stationRot, out var stationCell))
        {
            PlaceStation(map, relay, stationPos, stationRot, stationCell);
            return;
        }
        var spot = FrontRoomsPlacard.Resolve(map, doorPoint, facadeZ);
        Spot = spot;
        KeepClear(map, spot.keepClear);
        Frame = FrontRoomsKitLibrary.Spawn(FrontRoomsPlacard.KitName, transform, Vector3.zero, Quaternion.identity, null, false, "evacuation placard");
        if (Frame == null)
        {
            Debug.LogWarning("[Placard] " + FrontRoomsPlacard.KitName + " is missing; no placard this run");
            return;
        }
        Frame.transform.SetPositionAndRotation(spot.position, spot.rotation);
        var renderers = new List<Renderer>(Frame.GetComponentsInChildren<Renderer>(true));
        if (SpawnLens)
        {
            Lens = FrontRoomsKitLibrary.Spawn(FrontRoomsPlacard.LensKitName, transform, Vector3.zero, Quaternion.identity, null, false, "evacuation placard lens");
            if (Lens != null) Lens.transform.SetPositionAndRotation(spot.position, spot.rotation);
        }
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = true;
        }
        Glow = Frame.AddComponent<FrontRoomsPlacardGlow>();
        Glow.Bind(map, relay, spot.glowCell, renderers);
        Placed = true;
        Debug.Log("[Placard] mount " + FrontRoomsPlacard.Label(spot.mount) + " " + (spot.side < 0 ? "west" : "east")
                  + ", door cell " + spot.doorCell + " (west " + spot.west + ", east " + spot.east + "), glow cell " + spot.glowCell
                  + ", at " + spot.position.ToString("F4"));
    }

    // ---- station-look-v1 (A26): the station mount
    /// <summary>The station sheets (Tools/lookdev/pack_evac_plan.py from 平面视觉's sheetA_room and sheetA_room_phase0).</summary>
    public const string StationSheetRoom = "Surfaces/Textures/Prop_EvacPlan_A_Room", StationGlowRoom = "Surfaces/Textures/Prop_EvacPlan_E_Room";
    public const string StationSheetPhase0 = "Surfaces/Textures/Prop_EvacPlan_A_RoomPhase0", StationGlowPhase0 = "Surfaces/Textures/Prop_EvacPlan_E_RoomPhase0";

    /// <summary>
    /// The station mount (plan 70 §6.1): the frame at the anchor, 0.5 mm proud along its facing; no keep-clear (the
    /// station's props are authored and the map does not dress station cells); frame and lens re-parented under
    /// map.Station so they outlive the stream rooms; the glow gated by the anchor's cell (the Steady placard cell); the
    /// sheet sheetA_room while the stair lock is live (map.ExitLocked), else sheetA_room_phase0, with its EXAMINE transcript.
    /// </summary>
    void PlaceStation(FrontRoomsMapWorld map, FrontRoomsMapHunter relay, Vector3 pos, Quaternion rot, GridCoord cell)
    {
        Spot = new FrontRoomsPlacard.Spot
        {
            mount = FrontRoomsPlacard.Mount.Station,
            side = -1,
            position = pos + rot * new Vector3(0f, 0f, FrontRoomsPlacard.StationProud),
            rotation = rot,
            glowCell = cell,
            doorCell = map.StationLayout.door,
        };
        Frame = FrontRoomsKitLibrary.Spawn(FrontRoomsPlacard.KitName, transform, Vector3.zero, Quaternion.identity, null, false, "evacuation placard");
        if (Frame == null)
        {
            Debug.LogWarning("[Placard] " + FrontRoomsPlacard.KitName + " is missing; no placard this run");
            return;
        }
        Frame.transform.SetPositionAndRotation(Spot.position, Spot.rotation);
        var renderers = new List<Renderer>(Frame.GetComponentsInChildren<Renderer>(true));
        if (SpawnLens)
        {
            Lens = FrontRoomsKitLibrary.Spawn(FrontRoomsPlacard.LensKitName, transform, Vector3.zero, Quaternion.identity, null, false, "evacuation placard lens");
            if (Lens != null) Lens.transform.SetPositionAndRotation(Spot.position, Spot.rotation);
        }
        foreach (var r in Frame.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true; }
        if (Lens != null)
            foreach (var r in Lens.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true; }
        // Parented to the station set: the stream rooms (this mount's parent) are disposed after the start door shuts.
        Frame.transform.SetParent(map.Station.transform, true);
        if (Lens != null) Lens.transform.SetParent(map.Station.transform, true);
        Glow = Frame.AddComponent<FrontRoomsPlacardGlow>();
        Glow.Bind(map, relay, Spot.glowCell, renderers);
        var locked = map.ExitLocked;
        var sheet = Resources.Load<Texture2D>(locked ? StationSheetRoom : StationSheetPhase0);
        var glow = Resources.Load<Texture2D>(locked ? StationGlowRoom : StationGlowPhase0);
        if (Glow.Instance != null && sheet != null && glow != null)
        {
            Glow.Instance.SetTexture("_BaseMap", sheet);
            Glow.Instance.SetTexture("_EmissionMap", glow);
        }
        else Debug.LogWarning("[Placard] station sheet textures missing; the station placard keeps the default sheet");
        var inspectable = Frame.GetComponent<FrontRoomsInspectable>();
        if (inspectable != null && inspectable.Def != null)
        {
            var def = inspectable.Def.Clone();
            def.transcript = FrontRoomsInspectCatalog.StationPlacardTranscript(locked);
            def.regions = FrontRoomsInspectCatalog.StationPlacardRegions();
            inspectable.Configure(inspectable.Kit, def, FrontRoomsKitLibrary.GetInfo(FrontRoomsPlacard.KitName));
        }
        Placed = true;
        Debug.Log("[Placard] mount " + FrontRoomsPlacard.Label(Spot.mount) + ", station cell " + cell + ", sheet " + (locked ? "sheetA_room" : "sheetA_room_phase0")
                  + ", at " + Spot.position.ToString("F4"));
    }

    // CONTRACT Q16-1: map-owned FrontRoomsMapWorld.KeepClearAtStart(Rect), research/placard/40_contract_map.md.
    // It landed in main on 10-08 (MapWorld fb9164f7, test FrontRoomsMapInteractionTests.StartKeepClear), so the call
    // is direct; SetStartArea clears the list, and this runs after it (MapRunStarted).
    static void KeepClear(FrontRoomsMapWorld map, Rect worldXZ) => map.KeepClearAtStart(worldXZ);

    void OnDestroy() => Retire();
}
