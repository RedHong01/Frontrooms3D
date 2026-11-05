// The pool family's water: one body of water in a scene (research/bathroom_rooms/70_look_build.md, "Water surface").
// Game visual line.
using UnityEngine;

/// <summary>
/// One body of still water: a flat renderer at the water's height carrying the FrontRooms/Water material. While enabled
/// it puts its object on the Water layer (so no mirror camera sees it), registers with FrontRoomsWaterPlanar (the mirror
/// image or the probe, the lamp reflections, the refraction of what lies under it on desktop) and with FrontRoomsWaterRings (the
/// walls of its rectangle reflect rings). While any water is enabled in Play mode the planar manager ticks the ripple
/// and renders the mirror images once a frame, after every LateUpdate. In edit mode the water shows the probe tier and a
/// calm surface (nothing ticks).
/// It also registers with FrontRoomsWaterSubmerged: whatever lies under its water takes the water's colour (absorption by
/// the zone's scale, the trapping, the caustics of up to two compact lamps), and its room the wet niches' thrown light.
/// The structures scene (RULES 5b, scene 4) places one per pool room; the map places them once C2 gives it water classes.
/// One body per room keeps the reflections' box right (a room's walls, not the next room's); the bodies of one water
/// height share one mirror camera.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("FrontRooms/Water surface")]
public sealed class FrontRoomsWaterSurface : MonoBehaviour
{
    public enum Kind { Basin = 0, Ankle = 1 }

    [Tooltip("Basin: deep water. Ankle: a flooded floor (about 0.12 m). The Submerged stage's colour reads it.")]
    public Kind kind = Kind.Basin;
    [Tooltip("On desktop High and Ultra, what lies under the water is refracted from the camera's opaque texture (apparent depth and the ripple's wobble).")]
    public bool refraction = true;
    [Tooltip("The room above the water (m): reflections are projected on the box from the water up to this height.")]
    public float roomHeight = 2.9f;
    [Tooltip("Widens the room's box past the water's rectangle (m, x and z), for a room larger than its water.")]
    public Vector2 roomMargin = Vector2.zero;
    [Tooltip("Where the zone's reflection probe counts as captured, above the water (m), at the room's centre.")]
    public float probeHeight = 1.6f;

    [Header("Submerged (FrontRoomsWaterSubmerged)")]
    [Tooltip("Absorption scale (17 Q1): the club 1 (physical), the pool level 3-5, one or two rooms 9. Below 0: the zone's (FrontRoomsZoneLookProfile.waterAbsorbScale).")]
    public float absorbScale = -1f;
    [Tooltip("The basin's mean albedo (white tile 0.8): how much of the light the surface traps comes back up (17 4.2).")]
    [Range(0f, .95f)] public float basinAlbedo = .8f;
    [Tooltip("Up to two compact lamps over this water whose light carries the ripple's caustics onto its floor (17 4.4, 7 item 9; the general wash stays sourceless).")]
    public Light causticSourceA, causticSourceB;
    [Tooltip("Their radius (m): a small lamp draws crisp nets, a large one soft light.")]
    public float causticSourceRadius = .03f;
    [Tooltip("The wet niches' thrown light over this water (multiplies their glow). Below 0: the zone's (FrontRoomsZoneLookProfile.thrownLight).")]
    public float thrownGain = -1f;
    [Tooltip("The water's mean depth (m): the colour of the light it sends back into the room (FrontRoomsWaterSubmerged.BounceGain). Below 0: by kind (Basin 1.2, Ankle 0.12).")]
    public float depth = -1f;

    static int nextId = 1;
    int ringId;
    Renderer target;

    void OnEnable()
    {
        target = GetComponent<Renderer>();
        if (target == null) return;
        gameObject.layer = FrontRoomsWaterPlanar.WaterLayer;
        var b = target.bounds;
        var y = b.center.y;
        var box = new Bounds();
        box.SetMinMax(new Vector3(b.min.x - roomMargin.x, y, b.min.z - roomMargin.y), new Vector3(b.max.x + roomMargin.x, y + Mathf.Max(roomHeight, .1f), b.max.z + roomMargin.y));
        FrontRoomsWaterPlanar.Register(target, y, refraction, box, new Vector3(box.center.x, y + probeHeight, box.center.z));
        if (ringId == 0) ringId = nextId++;
        FrontRoomsWaterRings.RegisterWater(ringId, new Rect(b.min.x, b.min.z, b.size.x, b.size.z), y);
        FrontRoomsWaterSubmerged.Register(ringId, new Rect(b.min.x, b.min.z, b.size.x, b.size.z),
            new Rect(box.min.x, box.min.z, box.size.x, box.size.z), y, box.max.y, absorbScale, basinAlbedo, thrownGain);
        FrontRoomsWaterSubmerged.SetSources(ringId, causticSourceA, causticSourceB, causticSourceRadius);
        FrontRoomsWaterSubmerged.SetDepth(ringId, depth > 0f ? depth : (kind == Kind.Ankle ? .12f : 1.2f));
    }

    /// <summary>Pushes changed fields (scale, sources, albedo, depth) to the submerged look without a disable / enable.
    /// The body is registered again in place, so it keeps its place in FrontRoomsWaterSubmerged's list.</summary>
    public void RefreshSubmerged()
    {
        if (!isActiveAndEnabled) return;
        if (target != null) FrontRoomsWaterPlanar.Unregister(target);
        if (ringId != 0) FrontRoomsWaterRings.UnregisterWater(ringId);
        OnEnable();
    }

    void OnDisable()
    {
        if (target != null) FrontRoomsWaterPlanar.Unregister(target);
        if (ringId != 0) FrontRoomsWaterRings.UnregisterWater(ringId);
        if (ringId != 0) FrontRoomsWaterSubmerged.Unregister(ringId);
    }
}
