using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;

/// <summary>
/// The ALARM ZONE tell (RELAY_PURSUIT_REDESIGN.md section 6 and row 10; game visual line, 2026-10-31): every trigger room
/// carries ONE signature at every entrance, the same for every trigger kind (Run! rooms first, then the others):
/// - a ceiling smoke detector on a red-banded base (Kit_AlarmDetector: ivory head, fire-red base and trim ring, a red
///   standby LED that blinks once every 10 s, as the period heads did), inside the room on the entrance's centreline,
///   far enough in to be seen through the door from 2 m outside (RELAY section 6: "the device is visible from 2 m outside
///   an entrance");
/// - an emissive ALARM ZONE card (Kit_AlarmZoneCard: a 1990 lit warning sign, red steel box, white legend on a red
///   acrylic face) on the outside wall beside the opening, centre 1.524 m (60 in), facing the approach.
/// Research and numbers: Documentation/research/room_visuals/50_alarm_zone_tell.md. The card's lettering is the
/// graphic-visual line's (51_alarm_zone_for_graphic.md); until it lands the face is a STAND-IN (CardFaceIsStandIn).
///
/// INERT: nothing in the game calls this yet. The map (level design line) calls Dress once per entrance of a trigger room
/// when it builds the room's chunk; the dress is a child of the entrance transform, so it goes with the chunk. The tell
/// does not depend on the Run! rig, the Relay or any map type beyond ModuleUnits' defaults.
///
/// Entrance frame: the transform sits on the FLOOR at the centre of the opening, on the wall's centre plane, with +Z
/// pointing INTO the trigger room and +Y up. Distances are metres in that frame.
///
/// Tiers (three-platform parity: the tell looks and behaves the same everywhere; only cost changes):
/// - desktop: hero LOD0 within 3 m, LOD1, LOD2; card LOD0/1/2;
/// - handhelds and WebGL (ReducedTier): the detector's LOD0 is dropped (LOD1 from the near plane, 1,210 tris instead of
///   8,590); the card, the LED's blink and every emission stay identical. WebGL dresses the tell even though it skips
///   most decor props: the tell is gameplay information, not decor.
/// No realtime light, no shadow caster, no collider, no per-instance MonoBehaviour. One shared LED clock
/// (FrontRoomsAlarmTellLed) is created by the first Dress in Play mode; it drives three shared LED materials (three
/// phases, so neighbouring detectors never blink together).
/// </summary>
public static class FrontRoomsAlarmTell
{
    public const string DetectorKit = "Kit_AlarmDetector", CardKit = "Kit_AlarmZoneCard";
    public const string DressName = "ALARM ZONE tell", CardName = "ALARM ZONE card", DetectorName = "ALARM ZONE detector";
    /// <summary>Kit slots (Resources/Surfaces/&lt;slot&gt;.mat).</summary>
    public const string LedSlot = "Alarm_LED", FaceSlot = "Alarm_CardFace", RedSlot = "Alarm_BandRed";

    /// <summary>The card face (Resources/Print/AlarmTell) is a STAND-IN until the graphic-visual line delivers its art.</summary>
    public const bool CardFaceIsStandIn = true;

    /// <summary>Card centre above the entrance floor: 60 in, the room-sign rule of ADAAG 1991 4.30.6.</summary>
    public const float CardCentreHeight = 1.524f;
    /// <summary>Card housing half width (286 mm box) and the clear gap from the door frame's outer edge to the card.</summary>
    public const float CardHalfWidth = .143f, CardGap = .10f;
    /// <summary>Door frame face beside the opening (F4 hollow-metal frame, 2 in).</summary>
    public const float FrameFace = .0508f;
    /// <summary>RELAY section 6: the device is seen from this far outside the entrance (m).</summary>
    public const float SightDistance = 2f;
    /// <summary>The sight rule aims at the red trim ring, this far under the ceiling (m).</summary>
    public const float BandDrop = .02f;
    /// <summary>Detector depth limits, from the wall's inner face (m): never closer than MinInside, never deeper than
    /// MaxInside, and BackClear short of the room's far wall when the room depth is known.</summary>
    public const float MinInside = 1f, MaxInside = 6f, BackClear = .6f;

    /// <summary>Tools: force the reduced tier (the phone frame) in the editor.</summary>
    public static bool ForceReducedForTools;

    /// <summary>Handhelds and WebGL: the detector's LOD0 is dropped; nothing else changes.</summary>
    public static bool ReducedTier
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return true;
#else
            return ForceReducedForTools || FrontRoomsHandheld.Active;
#endif
        }
    }

    // ---------------------------------------------------------------- the entrance

    /// <summary>
    /// Dress one entrance of a trigger room: the card outside, the detector inside. Returns the dress root (a child of
    /// <paramref name="entrance"/> named DressName), or null when neither kit could be spawned.
    /// </summary>
    /// <param name="entrance">Floor point at the opening's centre on the wall's centre plane; +Z into the room.</param>
    /// <param name="openingWidth">Clear opening width (m).</param>
    /// <param name="wallThickness">Wall thickness (m); the card sits on the outside face (z = -t/2).</param>
    /// <param name="ceilingHeight">The trigger room's ceiling above the entrance floor (m).</param>
    /// <param name="doorHeadHeight">Clear opening height (m): the sight line into the room passes under it (a door: ModuleUnits.DoorHeight 2.1; a doorless arch, such as a Run! room's south edge: ModuleUnits.ArchTop 2.2).</param>
    /// <param name="cardSide">+1: the card on the approacher's right (+X); -1: on the left.</param>
    /// <param name="roomDepth">Clear depth of the room behind the entrance (m, from the inner wall face); 0 = unknown.</param>
    /// <param name="detectorDepth">Overrides the sight rule: the detector's z in the entrance frame (m); negative = use
    /// DetectorDepth.</param>
    public static GameObject Dress(Transform entrance, float openingWidth = ModuleUnits.DoorWidth,
        float wallThickness = ModuleUnits.WallThickness, float ceilingHeight = ModuleUnits.StandardCeiling, float doorHeadHeight = ModuleUnits.DoorHeight,
        int cardSide = 1, float roomDepth = 0f, float detectorDepth = -1f)
    {
        if (entrance == null) return null;
        var root = new GameObject(DressName).transform;
        root.SetParent(entrance, false);
        var card = DressCard(root, CardPoint(openingWidth, wallThickness, cardSide), 180f);
        var z = detectorDepth >= 0f ? detectorDepth : DetectorDepth(wallThickness, ceilingHeight, doorHeadHeight, roomDepth);
        var detector = DressCeiling(root, new Vector3(0f, ceilingHeight, z), 180f);
        if (card == null && detector == null)
        {
            Discard(root.gameObject);
            return null;
        }
        return root.gameObject;
    }

    /// <summary>The card's wall point (housing centre on the outside wall face) in the entrance frame.</summary>
    public static Vector3 CardPoint(float openingWidth, float wallThickness, int cardSide)
    {
        var x = openingWidth * .5f + FrameFace + CardGap + CardHalfWidth;
        return new Vector3(cardSide < 0 ? -x : x, CardCentreHeight, -wallThickness * .5f);
    }

    /// <summary>
    /// The sight rule (RELAY section 6): the smallest z (entrance frame) at which a detector's red ring, BandDrop under the
    /// ceiling, is seen by an eye at ModuleUnits.PlayerEye standing SightDistance outside the wall, over the door head's
    /// inner edge; clamped to [MinInside, MaxInside] past the inner wall face and BackClear short of the far wall.
    /// Use SeenFromOutside to know whether the clamp broke the rule (then the card alone carries the tell outside).
    /// </summary>
    public static float DetectorDepth(float wallThickness, float ceilingHeight, float doorHeadHeight, float roomDepth = 0f)
    {
        var inner = wallThickness * .5f;
        var z = SightDepth(wallThickness, ceilingHeight, doorHeadHeight) + .1f;
        var lo = inner + MinInside;
        var hi = inner + MaxInside;
        if (roomDepth > 0f) hi = Mathf.Min(hi, inner + roomDepth - BackClear);
        if (hi < inner + .3f) hi = inner + .3f;
        return Mathf.Clamp(Mathf.Max(z, lo), inner + .3f, hi);
    }

    /// <summary>The unclamped sight-rule z (entrance frame): where the eye's line over the door head meets the ring.</summary>
    public static float SightDepth(float wallThickness, float ceilingHeight, float doorHeadHeight)
    {
        var eye = ModuleUnits.PlayerEye;
        var zEye = -(wallThickness * .5f + SightDistance);
        var zEdge = wallThickness * .5f;
        var rise = Mathf.Max(.01f, doorHeadHeight - eye);
        var slope = rise / (zEdge - zEye);
        var target = ceilingHeight - BandDrop;
        return zEye + (target - eye) / slope;
    }

    /// <summary>True when a detector at <paramref name="z"/> (entrance frame) is seen from SightDistance outside.</summary>
    public static bool SeenFromOutside(float z, float wallThickness, float ceilingHeight, float doorHeadHeight)
    {
        return z >= SightDepth(wallThickness, ceilingHeight, doorHeadHeight) - 1e-4f;
    }

    // ---------------------------------------------------------------- the parts

    /// <summary>The detector alone at a ceiling point (local to <paramref name="parent"/>; y = the ceiling plane), yaw in
    /// degrees (0: the LED faces the parent's +Z). For trigger rooms whose map places the device itself.</summary>
    public static GameObject DressCeiling(Transform parent, Vector3 localCeilingPoint, float yaw)
    {
        if (parent == null) return null;
        var go = FrontRoomsKitLibrary.Spawn(DetectorKit, parent, localCeilingPoint, yaw, null, false, DetectorName, false);
        if (go == null) return null;
        if (ReducedTier) DropLod0(go);
        var led = FrontRoomsSurfaces.TryGet(LedSlot);
        if (led != null) Swap(go, led, FrontRoomsAlarmTellLed.PhaseMaterial(led, PhaseOf(go.transform.position)));
        return go;
    }

    /// <summary>The card alone: <paramref name="localWallPoint"/> is the housing centre on the wall face (local to
    /// <paramref name="parent"/>), yaw in degrees (0: the face looks down the parent's +Z).</summary>
    public static GameObject DressCard(Transform parent, Vector3 localWallPoint, float yaw)
    {
        if (parent == null) return null;
        return FrontRoomsKitLibrary.Spawn(CardKit, parent, localWallPoint, yaw, null, false, CardName, false);
    }

    /// <summary>The dress Dress put on this entrance, or null.</summary>
    public static GameObject Find(Transform entrance)
    {
        var t = entrance != null ? entrance.Find(DressName) : null;
        return t != null ? t.gameObject : null;
    }

    /// <summary>Remove this entrance's dress (a rebuild that keeps the entrance transform).</summary>
    public static void Undress(Transform entrance)
    {
        var go = Find(entrance);
        if (go != null) Discard(go);
    }

    /// <summary>
    /// OPTIONAL, no caller: the period alarm latch. A real head's LED lights steady once it alarms and stays lit until the
    /// panel resets. The trigger's owner may call it when the room trips (on) and at RoomReset (off); never on a timer,
    /// never from the Relay's state. Without it the LED keeps its standby blink whatever happens.
    /// </summary>
    public static void SetAlarm(GameObject dress, bool on)
    {
        if (dress == null) return;
        var led = FrontRoomsSurfaces.TryGet(LedSlot);
        if (led == null) return;
        var renderers = dress.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var r in renderers)
        {
            var mats = r.sharedMaterials;
            var changed = false;
            for (var i = 0; i < mats.Length; i++)
            {
                if (!FrontRoomsAlarmTellLed.IsLed(mats[i], led)) continue;
                var want = on ? FrontRoomsAlarmTellLed.LatchMaterial(led) : FrontRoomsAlarmTellLed.PhaseMaterial(led, PhaseOf(r.transform.position));
                if (want == null) want = led;
                if (mats[i] != want) { mats[i] = want; changed = true; }
            }
            if (changed) r.sharedMaterials = mats;
        }
    }

    // ---------------------------------------------------------------- helpers

    /// <summary>Which of the three blink phases a detector at this world point takes (stable for a place).</summary>
    public static int PhaseOf(Vector3 world)
    {
        var n = Mathf.FloorToInt(world.x * 7.31f + world.z * 3.17f + world.y * 1.13f);
        var k = n % FrontRoomsAlarmTellLed.Phases;
        return k < 0 ? k + FrontRoomsAlarmTellLed.Phases : k;
    }

    static void Swap(GameObject go, Material from, Material to)
    {
        if (go == null || from == null || to == null || from == to) return;
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
        {
            var mats = r.sharedMaterials;
            var changed = false;
            for (var i = 0; i < mats.Length; i++)
                if (mats[i] == from) { mats[i] = to; changed = true; }
            if (changed) r.sharedMaterials = mats;
        }
    }

    static readonly List<Renderer> Scratch = new List<Renderer>();

    /// <summary>Reduced tier: LOD1's renderers take LOD0's place (down to LOD1's own switch); LOD0's renderers go off.</summary>
    static void DropLod0(GameObject go)
    {
        var group = go.GetComponent<LODGroup>();
        if (group == null) return;
        var lods = group.GetLODs();
        if (lods.Length < 2) return;
        Scratch.Clear();
        if (lods[0].renderers != null) Scratch.AddRange(lods[0].renderers);
        var keep = new LOD[lods.Length - 1];
        keep[0] = new LOD(lods[1].screenRelativeTransitionHeight, lods[1].renderers);
        for (var i = 2; i < lods.Length; i++) keep[i - 1] = lods[i];
        group.SetLODs(keep);
        foreach (var r in Scratch)
            if (r != null) r.enabled = false;
        Scratch.Clear();
    }

    static void Discard(GameObject go)
    {
        if (go == null) return;
        if (Application.isPlaying) Object.Destroy(go);
        else Object.DestroyImmediate(go);
    }
}
