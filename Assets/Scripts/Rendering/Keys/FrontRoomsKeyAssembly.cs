using System.Collections.Generic;
using UnityEngine;

/// <summary>How a key assembly is posed now (D1.6 10_design §6).</summary>
public enum KeyPose { Hung, Flat, OverEdge, Held, InLock }

/// <summary>
/// A dressed zone key (D1.6, 10_design §4.3 and §6): ring + key + tag under the map's unscaled "Key · zone {id}" root.
/// It implements the map's seam (FrontRoomsKitSeam: IFrontRoomsKeyRig) with exactly its members, and is render-only
/// except for the one look+E trigger, which sits ON THE ROOT GameObject (the map's TakeKeyByE matches
/// chunk.keys[i].go == trigger.gameObject). Parts never carry colliders.
/// - Rest poses: Hung (from a key host's hook, sidecar hungPose v2), Flat / OverEdge / Floor (flats down, ring and tag
///   beside it). Hung keys never spin and nothing glows.
/// - PoseHeld: the root goes to the pose (research/ui_key_icon/held_pose/held_arrangement.json: +Z to the lens, shifted
///   per tag shape by HeldShapeOffsets so the swap to the HUD glyph does not jump, fix 3), the
///   parts blend over KeyPickup.TravelToHeld from their rest local poses to the arrangement's, on the assembly's own
///   clock (the delta between PoseHeld calls, so a pause freezes it). Once the root is closer to the view camera than
///   1.5 x KeyPickup.HeldDistance (0.525 m) every part renderer goes to the HeldItem layer (30) until Release
///   (desktop + iOS; never on WebGL), so a wall never clips the held key.
/// - PoseInLock: the key's shoulder at the pose (+Z insertion, +Y cuts up), the ring hanging from the key's hole in the
///   plane of the key's flat and down, the tag hanging from the ring. Over a LOCKED door's knob (fix 1, verify r1 L3) the
///   tag cannot hang through it: it tilts out about its hole until it rests on the knob's top (or, if it is long enough to
///   reach past the knob's face, on its front edge), from the knob's numbers on the nearest dressed rig.
/// - Sway: theta(t) = degrees * exp(-decay t) * sin(2 pi hz t) on the tag about its hole (and on the ring about the
///   key's hole while in the lock), advanced in LateUpdate on unscaled-free Time.deltaTime.
/// No per-frame allocation.
/// </summary>
public sealed class FrontRoomsKeyAssembly : MonoBehaviour, IFrontRoomsKeyRig
{
    // held_arrangement.json (2026-10-14), identical for the three tag shapes: local poses in the held frame.
    static readonly Vector3 HeldKeyPos = new Vector3(-.025498f, .019907f, .00237f);
    static readonly Quaternion HeldKeyRot = new Quaternion(.108778f, -.679604f, -.110192f, .717052f);
    static readonly Vector3 HeldRingPos = new Vector3(.000461f, .02477f, -.004402f);
    static readonly Quaternion HeldRingRot = new Quaternion(-.10795f, .538633f, .394987f, .736347f);
    static readonly Vector3 HeldTagPos = new Vector3(.012725f, .008633f, -.0021f);
    static readonly Quaternion HeldTagRot = Quaternion.identity;

    /// <summary>
    /// S1 (fix 3, verify r2 spec 1): per tag shape (index = (int)FrontRoomsKeyIdentity.Shape: Rect, Round, Long), the whole
    /// held arrangement is shifted by this offset in the held frame (metres; +X = the viewer's left, +Y = up, +Z = toward the
    /// lens; Z stays 0, so the depth and the HeldItem rule are unchanged), so that at the KP-1 swap (KeyHandoff at
    /// KeyPickup.HoldUntil) the real key's pixel footprint has the same centroid as the HUD's B glyph where it starts (the
    /// glyph box centred on the hand point (0.65, 0.30) at the P4 start factor, DECISIONS #24). Measured in Play at the swap
    /// frame, desktop and phone (research/interactables/d16/data/fix3/handoff/). Tools may change the elements (captures).
    /// </summary>
    public static readonly Vector3[] HeldShapeOffsets =
    {
        new Vector3(.00190f, .00125f, 0f),   // Rect: the swap's centroid jump was (-4.1, -3.2) px at 1920 x 1080 (fix 3, x03)
        new Vector3(.00176f, .00173f, 0f),   // Round: (-3.8, -3.9) px
        new Vector3(.00266f, .00107f, 0f),   // Long: (-5.7, -2.8) px
    };

    /// <summary>The held offset of a tag shape (HeldShapeOffsets).</summary>
    public static Vector3 HeldShapeOffset(FrontRoomsKeyIdentity.Shape shape) => HeldShapeOffsets[(int)shape];

    // Kit anchors (sidecars): key shoulder = origin, ring_hole; ring hook_contact = origin, key_contact, tag_contact; tag hole = origin.
    internal static readonly Vector3 KeyRingHole = new Vector3(0f, .0005f, -.0265f);
    internal static readonly Vector3 RingKeyContact = new Vector3(-.004f, -.0215f, 0f), RingTagContact = new Vector3(.004f, -.0213f, 0f);
    // Half thicknesses for resting flat (key X 0.0011, ring Z 0.0009, tag Z 0.0021).
    const float KeyHalf = .0011f, RingHalf = .0009f, TagHalf = .0021f;

    /// <summary>The HeldItem switch distance factor (VISUAL_CHAT_TASKS D1.6 V3): 1.5 x KeyPickup.HeldDistance.</summary>
    public const float HeldItemDepthFactor = 1.5f;
    public static float HeldItemDepth => HeldItemDepthFactor * FrontRoomsShotTimings.KeyPickup.HeldDistance;

    static readonly List<FrontRoomsKeyAssembly> all = new List<FrontRoomsKeyAssembly>();
    /// <summary>Every live assembly (the facade re-sets the printed tokens on a floor change; tools).</summary>
    public static IReadOnlyList<FrontRoomsKeyAssembly> All => all;

    // The knob under a keyhole (the lock kits' sidecars: escutcheon keyhole (0, 0.032, 0.0095), knob_pivot (0, -0.0315,
    // 0.002); Kit_Lock_Knob radius 0.027, face 0.063 out from its spindle): its top 36.5 mm under the keyhole, its face
    // 55.5 mm out from the keyhole's plane.
    internal const float KnobTopBelowKeyhole = .0635f - .027f, KnobFaceOut = .063f + .002f - .0095f, KnobClear = .002f;

    Transform key, ring, tagT;
    float tagLength = .034f, tagHalfThick = TagHalf;
    /// <summary>The tag's tilt out over the knob in the last PoseInLock (degrees; 0 = hanging free).</summary>
    public float TagTiltDegrees { get; private set; }
    Renderer[] renderers = new Renderer[0];
    int[] restLayers = new int[0];
    Vector3 restKeyPos, restRingPos, restTagPos;
    Quaternion restKeyRot, restRingRot, restTagRot;
    Vector3 baseTagPos, baseRingPos;
    Quaternion baseTagRot, baseRingRot;
    float heldClock = -1f, lastHeldTime = -1f;
    bool onHeldLayer;
    float swayDeg, swayHz, swayDecay, swayClock = -1f;

    public FrontRoomsKeyIdentity Identity { get; private set; }
    public KeyPose Pose { get; private set; }
    public string TagKit => Identity.TagKit;
    public BoxCollider Trigger { get; private set; }
    public Bounds LocalBounds { get; private set; }
    public Transform Root => transform;
    public Transform KeyPart => key;
    public Transform RingPart => ring;
    public Transform TagPart => tagT;
    public bool OnHeldLayer => onHeldLayer;

    // ---------------------------------------------------------------- set-up (the facade)

    internal void Init(FrontRoomsKeyIdentity id, Transform keyPart, Transform ringPart, Transform tagPart)
    {
        Identity = id;
        key = keyPart;
        ring = ringPart;
        tagT = tagPart;
        renderers = GetComponentsInChildren<Renderer>(true);
        restLayers = new int[renderers.Length];
        for (var i = 0; i < renderers.Length; i++) restLayers[i] = renderers[i].gameObject.layer;
        // The tag's reach below its hole (tag-local -Y) and its half thickness, from its LOD0 mesh bounds.
        if (tagT != null)
        {
            foreach (var f in tagT.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh == null || f.name.EndsWith("_LOD1") || f.name.EndsWith("_LOD2")) continue;
                var m = tagT.worldToLocalMatrix * f.transform.localToWorldMatrix;
                var b = f.sharedMesh.bounds;
                for (var i = 0; i < 8; i++)
                {
                    var c = m.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                    tagLength = Mathf.Max(tagLength, -c.y);
                    tagHalfThick = Mathf.Max(tagHalfThick, Mathf.Abs(c.z));
                }
            }
        }
        if (!all.Contains(this)) all.Add(this);
    }

    /// <summary>The facade: the identity this key prints (a floor change re-sets the token).</summary>
    internal void SetIdentity(FrontRoomsKeyIdentity id) => Identity = id;

    /// <summary>Lay the parts at a rest pose (root-local), remembered as where a pickup's blend starts.</summary>
    internal void PoseRest(KeyPose pose, Quaternion hostFrame)
    {
        Pose = pose;
        if (pose == KeyPose.Hung) LayHung(hostFrame);
        else LayFlat(pose == KeyPose.OverEdge);
        StoreRest();
    }

    void StoreRest()
    {
        restKeyPos = key.localPosition; restKeyRot = key.localRotation;
        restRingPos = ring.localPosition; restRingRot = ring.localRotation;
        restTagPos = tagT.localPosition; restTagRot = tagT.localRotation;
        CaptureBase();
        LocalBounds = ComputeLocalBounds();
    }

    void CaptureBase()
    {
        baseTagPos = tagT.localPosition; baseTagRot = tagT.localRotation;
        baseRingPos = ring.localPosition; baseRingRot = ring.localRotation;
    }

    // Hung from a hook at the root (sidecar hungPose v2): ring yawed 120 deg and lifted, the key tip down with its flats to
    // the room and twisted -30 deg, the tag (on the wall side) facing the room twisted 150 deg. Host frame: +Z = room.
    void LayHung(Quaternion host)
    {
        var ringRot = host * Quaternion.Euler(0f, 120f, 0f);
        ring.localRotation = ringRot;
        ring.localPosition = new Vector3(0f, .0012f, 0f);
        var keyRot = host * Quaternion.Euler(0f, -30f, 0f) * Quaternion.LookRotation(Vector3.down, Vector3.left);
        key.localRotation = keyRot;
        key.localPosition = ring.localPosition + ringRot * RingKeyContact - keyRot * KeyRingHole;
        var tagRot = host * Quaternion.Euler(0f, 150f, 0f);
        tagT.localRotation = tagRot;
        tagT.localPosition = ring.localPosition + ringRot * RingTagContact;
    }

    // Lying on a surface at the root (y = 0 is the surface): the key's flats down, the ring flat through its hole, the tag
    // flat beside the ring (over the edge: hanging down the support's front face, the root's +Z).
    void LayFlat(bool overEdge)
    {
        var keyRot = Quaternion.LookRotation(Vector3.forward, Vector3.left);   // key X (flat normal) -> up
        key.localRotation = keyRot;
        key.localPosition = new Vector3(0f, KeyHalf, -.02f);
        var hole = key.localPosition + keyRot * KeyRingHole;
        var ringRot = Quaternion.LookRotation(Vector3.up, Vector3.back) * Quaternion.Euler(0f, 0f, 25f);
        ring.localRotation = ringRot;
        ring.localPosition = new Vector3(hole.x, RingHalf, hole.z) - ringRot * RingKeyContact;
        ring.localPosition = new Vector3(ring.localPosition.x, RingHalf, ring.localPosition.z);
        var contact = ring.localPosition + ringRot * RingTagContact;
        if (overEdge)
        {
            tagT.localRotation = Quaternion.identity;   // face +Z (the support's front), hanging along -Y
            tagT.localPosition = new Vector3(contact.x, 0f, Mathf.Max(contact.z, .002f));
        }
        else
        {
            // Beside the key (fix 1, verify r1 S14; 10_design §6): the tag lies out to the key's side (+X, 20 deg back from
            // square), never over its blade; face up.
            var along = new Vector3(Mathf.Cos(20f * Mathf.Deg2Rad), 0f, -Mathf.Sin(20f * Mathf.Deg2Rad));
            tagT.localRotation = Quaternion.LookRotation(Vector3.up, -along);
            tagT.localPosition = new Vector3(contact.x, TagHalf, contact.z);
        }
    }

    /// <summary>The root-local bounds of every part renderer (the trigger's box).</summary>
    Bounds ComputeLocalBounds()
    {
        var any = false;
        var b = new Bounds(Vector3.zero, Vector3.zero);
        var w2l = transform.worldToLocalMatrix;
        foreach (var r in renderers)
        {
            if (r == null) continue;
            var f = r.GetComponent<MeshFilter>();
            if (f == null || f.sharedMesh == null) continue;
            var mb = f.sharedMesh.bounds;
            var m = w2l * r.transform.localToWorldMatrix;
            for (var i = 0; i < 8; i++)
            {
                var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var p = m.MultiplyPoint3x4(c);
                if (!any) { b = new Bounds(p, Vector3.zero); any = true; }
                else b.Encapsulate(p);
            }
        }
        return b;
    }

    /// <summary>The look+E trigger on the root (KeyPickupNeedsE): the posed bounds + 2 cm, at least 0.16 x 0.12 x 0.08 m.</summary>
    internal BoxCollider AddTrigger()
    {
        if (Trigger != null) return Trigger;
        var c = gameObject.AddComponent<BoxCollider>();
        c.isTrigger = true;
        var b = LocalBounds;
        c.center = b.center;
        c.size = Vector3.Max(b.size + Vector3.one * .02f, new Vector3(.16f, .12f, .08f));
        Trigger = c;
        return c;
    }

    // ---------------------------------------------------------------- IFrontRoomsKeyRig

    public void PoseInLock(Pose shoulder)
    {
        Pose = KeyPose.InLock;
        heldClock = -1f;
        transform.SetPositionAndRotation(shoulder.position, shoulder.rotation);
        key.localPosition = Vector3.zero;
        key.localRotation = Quaternion.identity;
        // The ring hangs from the key's hole in the plane of the key's flat (its X) and down; the tag hangs from the ring,
        // its face back toward the viewer (against the insertion).
        var keyRight = shoulder.rotation * Vector3.right;
        var flatRight = Vector3.ProjectOnPlane(keyRight, Vector3.up);
        if (flatRight.sqrMagnitude < 1e-6f) flatRight = Vector3.right;
        var ringWorld = Quaternion.LookRotation(Vector3.Cross(flatRight.normalized, Vector3.up), Vector3.up);
        var inv = Quaternion.Inverse(shoulder.rotation);
        ring.localRotation = inv * ringWorld;
        ring.localPosition = KeyRingHole;
        var back = Vector3.ProjectOnPlane(-(shoulder.rotation * Vector3.forward), Vector3.up);
        if (back.sqrMagnitude < 1e-6f) back = -flatRight;
        back.Normalize();
        var hang = Quaternion.LookRotation(back, Vector3.up);
        tagT.localPosition = ring.localPosition + ring.localRotation * RingTagContact;
        var tilt = TiltOverKnob(shoulder, transform.TransformPoint(tagT.localPosition), back);
        TagTiltDegrees = tilt;
        var worldTag = tilt > 0f ? Quaternion.AngleAxis(tilt, Vector3.Cross(Vector3.down, back)) * hang : hang;
        tagT.localRotation = inv * worldTag;
        CaptureBase();
    }

    // How far the tag must tilt out (toward the viewer) about its hole so that it clears the knob of the nearest dressed
    // LOCKED rig: rest on the knob's top when the tilted tag's end stays over it, else on the knob face's top edge. 0 when
    // there is no knob under the key or the tag already hangs clear (above it, or out in front of its face).
    float TiltOverKnob(Pose shoulder, Vector3 hole, Vector3 outward)
    {
        // The lock the key is in (or at): the LOCKED rig whose keyhole is within 0.15 m of the shoulder (the standoff before
        // the insert is 0.06-0.10 m), on that face.
        var rig = FrontRoomsDoorRig.NearestKeyhole(shoulder.position, .15f, out var face);
        if (rig == null) return 0f;
        var keyhole = rig.Keyhole(face);
        var outDir = -rig.KeyholeDir(face);
        var outM = Vector3.Dot(hole - keyhole, outDir) - tagHalfThick;
        if (outM >= KnobFaceOut + KnobClear) return 0f;
        var dy = hole.y - (keyhole.y - KnobTopBelowKeyhole) - KnobClear - tagHalfThick;
        var len = tagLength;
        if (dy >= len) return 0f;
        if (dy <= 0f) return 85f;
        var onTop = Mathf.Acos(Mathf.Clamp(dy / len, -1f, 1f));
        var tipOut = Mathf.Max(0f, outM) + len * Mathf.Sin(onTop);
        var a = tipOut <= KnobFaceOut ? onTop : Mathf.Atan2(KnobFaceOut - Mathf.Max(0f, outM), dy);
        return Mathf.Clamp(a * Mathf.Rad2Deg, 0f, 85f);
    }

    public void PoseHeld(Pose lensFacing)
    {
        if (Pose != KeyPose.Held)
        {
            Pose = KeyPose.Held;
            heldClock = 0f;
            lastHeldTime = Now;
            if (Trigger != null) Trigger.enabled = false;
        }
        else
        {
            var now = Now;
            heldClock += Mathf.Max(0f, now - lastHeldTime);
            lastHeldTime = now;
        }
        transform.SetPositionAndRotation(lensFacing.position, lensFacing.rotation);
        var k = EaseOut(heldClock / FrontRoomsShotTimings.KeyPickup.TravelToHeld);
        var shift = HeldShapeOffset(Identity.shape);
        key.localPosition = Vector3.Lerp(restKeyPos, HeldKeyPos + shift, k);
        key.localRotation = Quaternion.Slerp(restKeyRot, HeldKeyRot, k);
        ring.localPosition = Vector3.Lerp(restRingPos, HeldRingPos + shift, k);
        ring.localRotation = Quaternion.Slerp(restRingRot, HeldRingRot, k);
        tagT.localPosition = Vector3.Lerp(restTagPos, HeldTagPos + shift, k);
        tagT.localRotation = Quaternion.Slerp(restTagRot, HeldTagRot, k);
        CaptureBase();
        if (!onHeldLayer && FrontRoomsHeldItem.Supported)
        {
            var cam = FrontRoomsHeldItem.ViewCamera;
            if (cam != null && Vector3.Dot(lensFacing.position - cam.transform.position, cam.transform.forward) < HeldItemDepth) SetHeldLayer(true);
        }
        ApplySway();
    }

    public void Sway(float degrees, float hz, float decay)
    {
        swayDeg = degrees;
        swayHz = hz;
        swayDecay = decay;
        swayClock = 0f;
    }

    public void Release()
    {
        swayClock = -1f;
        if (tagT != null) { tagT.localPosition = baseTagPos; tagT.localRotation = baseTagRot; }
        if (ring != null) { ring.localPosition = baseRingPos; ring.localRotation = baseRingRot; }
        SetHeldLayer(false);
    }

    /// <summary>Put every part renderer on the HeldItem layer (true) or back to the layers it had (false).</summary>
    public void SetHeldLayer(bool on)
    {
        if (on == onHeldLayer) return;
        onHeldLayer = on;
        for (var i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].gameObject.layer = on ? FrontRoomsHeldItem.Layer : restLayers[i];
        }
        if (on) FrontRoomsHeldItem.Register(this); else FrontRoomsHeldItem.Unregister(this);
    }

    void LateUpdate() => Step(Time.deltaTime);

    /// <summary>Tools and edit-mode tests: advance the sway by dt (LateUpdate does this in Play Mode).</summary>
    public void StepForTools(float dt) => Step(dt);

    void Step(float dt)
    {
        if (swayClock < 0f) return;
        swayClock += dt;
        ApplySway();
        if (swayClock * swayDecay > 6f) { swayClock = -1f; ApplySway(); }
    }

    void ApplySway()
    {
        if (tagT == null) return;
        var theta = swayClock >= 0f ? swayDeg * Mathf.Exp(-swayDecay * swayClock) * Mathf.Sin(2f * Mathf.PI * swayHz * swayClock) : 0f;
        if (Pose == KeyPose.InLock && TagTiltDegrees > 0f) theta *= .35f;   // resting on the knob: it slides, it does not swing
        // The tag's origin is its hole: turning about its own Z keeps the hole on the ring.
        tagT.localPosition = baseTagPos;
        tagT.localRotation = baseTagRot * Quaternion.AngleAxis(theta, Vector3.forward);
        if (Pose == KeyPose.InLock && ring != null)
        {
            // The ring swings about the key's hole (its origin is the hook contact, at the hole): half the tag's swing.
            ring.localRotation = baseRingRot * Quaternion.AngleAxis(theta * .5f, Vector3.forward);
            tagT.localPosition = ring.localPosition + ring.localRotation * RingTagContact;
        }
    }

    void OnDestroy()
    {
        all.Remove(this);
        if (onHeldLayer) FrontRoomsHeldItem.Unregister(this);
    }

    /// <summary>Edit-mode tests: the clock PoseHeld reads instead of Time.time (null = Time.time).</summary>
    public static System.Func<float> ClockForTests;
    static float Now => ClockForTests != null ? ClockForTests() : Time.time;

    static float EaseOut(float u)
    {
        u = Mathf.Clamp01(u);
        return 1f - (1f - u) * (1f - u);
    }

    // ---------------------------------------------------------------- tests
    public static Vector3 HeldLocalPositionForTests(int part) => part == 0 ? HeldKeyPos : part == 1 ? HeldRingPos : HeldTagPos;
    public static Quaternion HeldLocalRotationForTests(int part) => part == 0 ? HeldKeyRot : part == 1 ? HeldRingRot : HeldTagRot;
}
