using System;
using UnityEngine;

/// <summary>
/// One EXAMINE target (research/inspect/10_design.md §7.1 file 2): a kit instance whose sidecar
/// carries an "inspect" block. FrontRoomsKitLibrary.Spawn attaches it in Play Mode only. It
/// registers with FrontRoomsInspect in OnEnable and leaves in OnDisable (a chunk unload or a Level
/// Designer rebuild while it is the close-up's target raises FrontRoomsInspect.AbortRequested).
/// The aim box and the face are turned into world space at query time, never cached: the placard
/// mount spawns at the origin and only then moves the frame.
/// No collider and no layer: the game asks the registry with its aim ray after the physics ray.
/// </summary>
[AddComponentMenu("")]
[DisallowMultipleComponent]
public sealed class FrontRoomsInspectable : MonoBehaviour
{
    static int serial;

    /// <summary>"Kit_EvacPlacard#3".</summary>
    public string Id { get; private set; }
    public string Kit { get; private set; }
    public InspectDef Def { get; private set; }
    public InspectKind Kind => Def != null ? Def.Kind : InspectKind.Flat;
    public InspectSound Sound => Def != null ? Def.Sound : InspectSound.Plastic;
    /// <summary>Per-target condition (null = always).</summary>
    public Func<bool> Available;
    /// <summary>Object kind: what moves to the camera (default: this transform).</summary>
    public Transform Display;

    Vector3 faceLocal, faceDirLocal, upLocal, boxCentre, boxSize;
    Vector2 sizeLocal;
    float depthFrontLocal, depthBackLocal;
    bool hasUp;

    /// <summary>The prompt line, in the existing slot: verb + object (strings from 平面视觉: FrontRoomsInspectCatalog).</summary>
    public string Prompt => prompt ?? FrontRoomsInspectCatalog.PromptRead;
    string prompt;

    /// <summary>The aim box's extent along the face normal, from the face plane (metres): front ≥ 0, back ≤ 0 (the framing's 8-corner fit).</summary>
    public float DepthFront => depthFrontLocal * Mathf.Abs(transform.lossyScale.z);
    public float DepthBack => depthBackLocal * Mathf.Abs(transform.lossyScale.z);

    /// <summary>The face in world space: position = its centre, forward = outward normal, up = text-up.</summary>
    public Pose Face
    {
        get
        {
            var t = transform;
            var position = t.TransformPoint(faceLocal);
            var normal = t.TransformDirection(faceDirLocal - faceLocal);
            if (normal.sqrMagnitude < 1e-10f) normal = t.forward;
            normal.Normalize();
            var up = hasUp ? t.TransformDirection(upLocal - faceLocal) : t.up;
            up = Vector3.ProjectOnPlane(up, normal);
            if (up.sqrMagnitude < 1e-8f) up = Vector3.ProjectOnPlane(Vector3.up, normal);
            if (up.sqrMagnitude < 1e-8f) up = Vector3.ProjectOnPlane(Vector3.forward, normal);
            return new Pose(position, Quaternion.LookRotation(normal, up.normalized));
        }
    }

    /// <summary>The framed rect in world metres (scale applied).</summary>
    public Vector2 Size
    {
        get
        {
            var s = transform.lossyScale;
            return new Vector2(sizeLocal.x * Mathf.Abs(s.x), sizeLocal.y * Mathf.Abs(s.y));
        }
    }

    /// <summary>The largest half-diagonal of the aim box in world metres (the object kind's bounding radius).</summary>
    public float BoundingRadius => Vector3.Scale(boxSize, Abs(transform.lossyScale)).magnitude * .5f;

    /// <summary>The aim box's centre in world space.</summary>
    public Vector3 BoxCentreWorld => transform.TransformPoint(boxCentre);

    static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    /// <summary>Attach (or refresh) the component on a spawned kit instance. Null when the block is not set.</summary>
    public static FrontRoomsInspectable Attach(GameObject instance, string kit, InspectDef def)
    {
        if (instance == null || def == null || !def.IsSet) return null;
        var target = instance.GetComponent<FrontRoomsInspectable>();
        if (target == null) target = instance.AddComponent<FrontRoomsInspectable>();
        target.Configure(kit, def, FrontRoomsKitLibrary.GetInfo(kit));
        target.EnsureRegistered();   // OnEnable ran inside AddComponent, before the data was bound
        return target;
    }

    /// <summary>Bind the data (anchors, size and box from the block, else from the kit's bounds, §3.8).</summary>
    public void Configure(string kit, InspectDef def, FrontRoomsKitLibrary.Info info)
    {
        Kit = kit;
        Def = def;
        prompt = def.Kind == InspectKind.Peek ? FrontRoomsInspectCatalog.PromptPeek : FrontRoomsInspectCatalog.PromptFor(kit, def.Verb);
        if (string.IsNullOrEmpty(Id)) Id = kit + "#" + (++serial);
        var min = info != null ? info.Min : new Vector3(-.1f, -.1f, 0f);
        var max = info != null ? info.Max : new Vector3(.1f, .1f, .01f);
        var frontCentre = new Vector3((min.x + max.x) * .5f, (min.y + max.y) * .5f, max.z);
        faceLocal = frontCentre;
        faceDirLocal = frontCentre + Vector3.forward * .1f;
        if (info != null && info.TryAnchor(def.FaceAnchor, out var f))
        {
            faceLocal = f;
            faceDirLocal = info.TryAnchor(def.FaceDirAnchor, out var fd) ? fd : f + Vector3.forward * .1f;
        }
        hasUp = false;
        if (!string.IsNullOrEmpty(def.up) && info != null && info.TryAnchor(def.up, out var u)) { upLocal = u; hasUp = true; }
        sizeLocal = def.size != null && def.size.Length >= 2 ? new Vector2(def.size[0], def.size[1]) : new Vector2(max.x - min.x, max.y - min.y);
        if (def.box != null && def.box.c != null && def.box.c.Length >= 3 && def.box.s != null && def.box.s.Length >= 3)
        {
            boxCentre = new Vector3(def.box.c[0], def.box.c[1], def.box.c[2]);
            boxSize = new Vector3(def.box.s[0], def.box.s[1], def.box.s[2]);
        }
        else
        {
            boxCentre = (min + max) * .5f;
            boxSize = max - min;
        }
        // The box about the face plane along the face normal (a placard frame: 4.55 mm behind the sheet to 8.95 mm proud of it).
        var n = faceDirLocal - faceLocal;
        n = n.sqrMagnitude > 1e-10f ? n.normalized : Vector3.forward;
        var halfAlong = .5f * (Mathf.Abs(n.x) * boxSize.x + Mathf.Abs(n.y) * boxSize.y + Mathf.Abs(n.z) * boxSize.z);
        var centreAlong = Vector3.Dot(boxCentre - faceLocal, n);
        depthFrontLocal = Mathf.Max(0f, centreAlong + halfAlong);
        depthBackLocal = Mathf.Min(0f, centreAlong - halfAlong);
        if (Display == null) Display = transform;
    }

    void OnEnable()
    {
        if (Def != null) FrontRoomsInspect.Register(this);
    }

    void OnDisable() => FrontRoomsInspect.Unregister(this);

    void OnDestroy() => FrontRoomsInspect.Unregister(this);

    /// <summary>Re-register after Configure on an already enabled instance.</summary>
    internal void EnsureRegistered()
    {
        if (isActiveAndEnabled && Def != null) FrontRoomsInspect.Register(this);
    }

    /// <summary>
    /// The ray against the padded world OBB of the aim box (aimPad per side, each side at least
    /// MinAimSize overall). Allocation-free. Distance = where the ray enters the box (0 inside).
    /// </summary>
    public bool Raycast(in Ray ray, out float distance)
    {
        distance = 0f;
        var t = transform;
        var rotation = t.rotation;
        var inv = Quaternion.Inverse(rotation);
        var centre = t.TransformPoint(boxCentre);
        var pad = Def != null ? Def.AimPad : InspectDef.DefaultAimPad;
        var half = Vector3.Scale(boxSize, Abs(t.lossyScale)) * .5f + new Vector3(pad, pad, pad);
        var minHalf = InspectDef.MinAimSize * .5f;
        half = new Vector3(Mathf.Max(half.x, minHalf), Mathf.Max(half.y, minHalf), Mathf.Max(half.z, minHalf));
        var o = inv * (ray.origin - centre);
        var d = inv * ray.direction;
        var tMin = 0f;
        var tMax = float.PositiveInfinity;
        for (var axis = 0; axis < 3; axis++)
        {
            var oa = o[axis];
            var da = d[axis];
            var h = half[axis];
            if (Mathf.Abs(da) < 1e-8f)
            {
                if (oa < -h || oa > h) return false;
                continue;
            }
            var inv1 = 1f / da;
            var t1 = (-h - oa) * inv1;
            var t2 = (h - oa) * inv1;
            if (t1 > t2) { var swap = t1; t1 = t2; t2 = swap; }
            if (t1 > tMin) tMin = t1;
            if (t2 < tMax) tMax = t2;
            if (tMin > tMax) return false;
        }
        distance = tMin;
        return true;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (Def == null) return;
        var face = Face;
        var size = Size;
        Gizmos.color = new Color(.96f, .87f, .23f, 1f);
        Gizmos.matrix = Matrix4x4.TRS(face.position, face.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, size.y, .002f));
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.DrawLine(face.position, face.position + face.rotation * Vector3.forward * .3f);
        var last = FrontRoomsInspect.LastSolveFor(this);
        if (last.ok)
        {
            Gizmos.color = new Color(.2f, .8f, 1f, 1f);
            Gizmos.DrawWireSphere(last.pose.position, .02f);
            Gizmos.DrawLine(last.pose.position, last.pose.position + last.pose.rotation * Vector3.forward * .15f);
        }
    }
#endif
}
