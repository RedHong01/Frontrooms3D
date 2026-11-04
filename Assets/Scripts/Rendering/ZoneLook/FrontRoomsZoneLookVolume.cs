using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// A box where a zone's look holds (FrontRoomsZoneLook; research/bathroom_rooms/70_look_build.md, "Zone look"): its ambient,
/// haze and height fog (Resources/Rendering/ZoneLook/FrontRoomsZoneLook_&lt;zone&gt;) and its grade
/// (Resources/Rendering/FrontRoomsPost_&lt;zone&gt;, a child URP volume made by FrontRoomsPostStack.EnsureZoneVolume over the same
/// box and blend). Both blend in over blendDistance as the camera walks in, by URP's own weight, so the grade and the
/// light never disagree. Scene-placeable (the structures scene, RULES 5b; the Level Designer) and placed by the map's chunk
/// build under the pool contract (FrontRoomsZoneLook.AddMapZone). Runs in edit mode too, so a scene shows its look in the
/// Scene and Game views; nothing it writes outlives a render (FrontRoomsZoneLook). ASCII only.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("FrontRooms/Zone look volume")]
public sealed class FrontRoomsZoneLookVolume : MonoBehaviour
{
    [Tooltip("PoolClub, PoolHall, PoolLevel or PoolDark: the look profile FrontRoomsZoneLook_<zone> and the grade FrontRoomsPost_<zone>.")]
    public string zone = "PoolHall";
    [Tooltip("The box, in this object's local space.")]
    public Vector3 center = new Vector3(0f, 1.5f, 0f);
    public Vector3 size = new Vector3(6f, 3f, 6f);
    [Tooltip("Metres outside the box over which the look fades in (URP's weight, 1 - (d / blend)^2).")]
    [Min(0f)] public float blendDistance = 2.5f;
    [Tooltip("Higher wins where volumes overlap (the Office grade is 1).")]
    public float priority = 1f;
    [Range(0f, 1f)] public float weight = 1f;
    [Tooltip("The zone's water surface, local y (the height fog sits on it). Off for a dry zone.")]
    public bool hasWater = true;
    public float waterLevel = -.12f;
    [Tooltip("Optional: a profile asset to use instead of the zone's (look-dev).")]
    public FrontRoomsZoneLookProfile profileOverride;

    Volume grade;

    public bool HasWater => hasWater;
    public float WaterLevelWorld => transform.TransformPoint(new Vector3(0f, waterLevel, 0f)).y;
    public FrontRoomsZoneLookProfile Profile => profileOverride != null ? profileOverride : FrontRoomsZoneLook.Profile(zone);
    /// <summary>The grade volume (null when the zone has no FrontRoomsPost_&lt;zone&gt;).</summary>
    public Volume Grade => grade;

    /// <summary>
    /// One look volume under parent (made or reused by name, like EnsureZoneVolume): the box in parent's local space; waterY
    /// local, NaN for a dry zone.
    /// </summary>
    public static FrontRoomsZoneLookVolume Ensure(Transform parent, string zone, Bounds localBounds, float blendDistance, float waterY)
    {
        if (parent == null || string.IsNullOrEmpty(zone)) return null;
        var name = "Zone look " + zone;
        var existing = parent.Find(name);
        var go = existing != null ? existing.gameObject : new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        FrontRoomsZoneLookVolume v;
        var fresh = !go.TryGetComponent(out v);
        if (fresh)
        {
            // configure before the first OnEnable registers it
            go.SetActive(false);
            v = go.AddComponent<FrontRoomsZoneLookVolume>();
        }
        v.zone = zone;
        v.center = localBounds.center;
        v.size = localBounds.size;
        v.blendDistance = blendDistance;
        v.hasWater = !float.IsNaN(waterY);
        v.waterLevel = v.hasWater ? waterY : 0f;
        if (fresh) go.SetActive(true);
        else v.Refresh();
        return v;
    }

    void OnEnable()
    {
        Refresh();
        FrontRoomsZoneLook.Register(this);
    }

    void OnDisable()
    {
        FrontRoomsZoneLook.Unregister(this);
        if (grade != null) grade.enabled = false;
    }

    void OnValidate()
    {
        size = new Vector3(Mathf.Max(0f, size.x), Mathf.Max(0f, size.y), Mathf.Max(0f, size.z));
        // no objects are made inside OnValidate: only an existing grade volume follows the fields
        if (grade != null) Match(grade);
        FrontRoomsZoneLook.MarkDirty();
    }

    /// <summary>Match the grade volume to the box, blend and priority (after editing the fields from code).</summary>
    public void Refresh()
    {
        // EnsureZoneVolume looks its child up by a name holding '/', which Transform.Find reads as a path, so a second call
        // would make a second volume: find our own first (one grade volume per look volume, whatever called Refresh)
        if (grade == null) grade = OwnGrade();
        if (grade == null) grade = FrontRoomsPostStack.EnsureZoneVolume(transform, zone, new Bounds(center, size), blendDistance);
        if (grade != null) Match(grade);
        FrontRoomsZoneLook.MarkDirty();
    }

    Volume OwnGrade()
    {
        var name = "Post / zone " + zone;
        for (var i = 0; i < transform.childCount; i++)
        {
            var c = transform.GetChild(i);
            Volume v;
            if (c.name == name && c.TryGetComponent(out v)) return v;
        }
        return null;
    }

    void Match(Volume v)
    {
        BoxCollider box;
        if (v.TryGetComponent(out box))
        {
            box.center = center;
            box.size = size;
        }
        v.blendDistance = blendDistance;
        v.priority = priority;
        v.weight = weight;
        v.enabled = isActiveAndEnabled;
    }

    /// <summary>
    /// URP's weight of this box at a world position: 1 inside, 1 - (d / blend)^2 within blendDistance of it, else 0; times
    /// weight. d is the distance to the box's closest point (as Collider.ClosestPoint gives URP).
    /// </summary>
    public float InfluenceAt(Vector3 world)
    {
        if (weight <= 0f) return 0f;
        var local = transform.InverseTransformPoint(world);
        var half = size * .5f;
        var c = new Vector3(Mathf.Clamp(local.x, center.x - half.x, center.x + half.x),
                            Mathf.Clamp(local.y, center.y - half.y, center.y + half.y),
                            Mathf.Clamp(local.z, center.z - half.z, center.z + half.z));
        var d2 = (transform.TransformPoint(c) - world).sqrMagnitude;
        if (d2 <= 0f) return Mathf.Clamp01(weight);
        var b2 = blendDistance * blendDistance;
        if (d2 > b2 || b2 <= 0f) return 0f;
        return Mathf.Clamp01(weight) * (1f - d2 / b2);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(.35f, .85f, .9f, .9f);
        Gizmos.DrawWireCube(center, size);
        Gizmos.color = new Color(.35f, .85f, .9f, .25f);
        Gizmos.DrawWireCube(center, size + Vector3.one * (2f * blendDistance));
        if (hasWater)
        {
            Gizmos.color = new Color(.2f, .6f, 1f, .6f);
            Gizmos.DrawWireCube(new Vector3(center.x, waterLevel, center.z), new Vector3(size.x, 0f, size.z));
        }
    }
}
