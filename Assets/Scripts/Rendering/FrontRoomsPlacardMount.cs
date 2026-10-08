using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The evacuation placard's mount on the terminal stream room (FrontRoomsPlacard.Prepare).
/// Waits for FrontRooms3DGame.MapRunStarted (fired in StartRunInPlace after EndStreamAt
/// and map.Begin; rooms are dressed one a frame from Update, so a keep-clear rect
/// registered here is in time), then once: resolves the spot (§5.2), asks the map to
/// keep its floor clear (map.KeepClearAtStart, CONTRACT Q16-1, called only if the map
/// has it), spawns the kits render-only with shadow casting OFF on every renderer
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

    static MethodInfo keepClearAtStart;
    static bool keepClearLooked;

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

    // CONTRACT Q16-1 (map-owned FrontRoomsMapWorld.KeepClearAtStart(Rect), research/placard/40_contract_map.md): used
    // when the map has it, else a no-op. Found by reflection only until the map lands it, so this file compiles against
    // today's map; the contract asks for [UnityEngine.Scripting.Preserve] on the method, because a method reached only by
    // reflection can be stripped (WebGL managed stripping High, iOS Low). Once it lands, replace this with a direct call.
    static void KeepClear(FrontRoomsMapWorld map, Rect worldXZ)
    {
        if (!keepClearLooked)
        {
            keepClearLooked = true;
            keepClearAtStart = typeof(FrontRoomsMapWorld).GetMethod("KeepClearAtStart", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Rect) }, null);
            if (keepClearAtStart == null)
                Debug.Log("[Placard] the map has no KeepClearAtStart (contract Q16-1): furniture may be dressed in front of a P2/P3 placard");
        }
        keepClearAtStart?.Invoke(map, new object[] { worldXZ });
    }

    void OnDestroy() => Retire();
}
