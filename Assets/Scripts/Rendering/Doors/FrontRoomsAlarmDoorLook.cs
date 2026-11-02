using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;

/// <summary>
/// The C16 alarm door's look (Relay v2 V7b; game visual line, 2026-11-01). Research and numbers:
/// Documentation/research/door_hardware/v7b_alarm_door/10_research.md; kits: Tools/Blender/frontrooms_kit/assets/alarm_exit_*.py.
///
/// An armed alarm door is a double-acting map door (A12 / A19) dressed as a c. 1985-90 battery exit alarm, the map's P3
/// default: on BOTH faces an exit-alarm touch bar (the alarm housing at the latch end with the arming key cylinder, the
/// pilot LED and the mode button; the rail; the moving pad; the hinge-end cap) and the placard; one fire-red local horn
/// over the head, on the face of the zone that owns the door (FrontRoomsAlarmDoors.AlarmedZone; the S face for a tools
/// mark). The placard's face is a STAND-IN (PlacardFaceIsStandIn) until the information design line delivers its art.
///
/// Hook: FrontRoomsInteractableKit.DressDoor calls Attach when door.alarm is set (the map sets it on every build before the
/// kit dresses the door; always false with the level profile's alarmDoor off and no tools mark), so with the flag off this
/// class is never reached and nothing is built, ticked or read (inert by construction).
/// - FREE doors (the shipped rows L0-F, OF-F): the bar replaces the lever and rose on both faces (the bar is the handle;
///   the latch and the strike stay and still draw on the open beat). Bar datum 1.000 m.
/// - LOCKED doors (keys mode): the knob trim, cylinder, deadbolt and plates stay exactly as built (the unlock shot's keyhole
///   is the rig's constant), and the bar rides above them (datum 1.097 m: the rail clears the escutcheon top 1.070 and
///   stays under the F1 lite's bead at 1.124).
///
/// States, read from the map (never from the Relay, the player's distance or a tier, so nothing here is a radar):
/// - ARMED (map.IsAlarmArmed): the pilot LED takes the alarm family's standby blip (FrontRoomsAlarmTellLed: one dim
///   0.14 s blip every 10 s, three phases by place; Reduce Flashing: a steady dim glow).
/// - TRIPPED (map.IsAlarmTripped): the LED latches steady and brighter (period devices sound and show the alarm until a key
///   resets them: US4785286, US4631528). No strobe: battery exit alarms of the period had none (photosensitive rules met
///   by construction: nothing flashes faster than the 0.1 Hz standby blip).
/// - The pad's travel (Red's 10A, research/interaction/doors/20_design.md 4.6: "held 0.4 s while the bar visibly
///   travels"): from the map's hold through FrontRoomsInteractableKit.AlarmBarPressFromMap (the contract
///   W/apply/v7b_alarm_door_contract), signed by the face pushed; a short strain for a refused push; until the map line
///   lands it, a press beat at the trip (in 0.08 s, held 0.10 s) on the face the camera is on. Always a 0.15 s spring back.
/// - A break (the Relay): at the FINAL stage the leaf-mounted parts (bar, pad, placard) are taken down on both faces (the
///   torn latch strip carried the housing); a door rebuilt broken gets the horn only.
///
/// Tiers: desktop hero LOD0 within 2 m (horn and placard 3 m), LOD1, LOD2; handhelds drop LOD0 (FrontRoomsAlarmTell.
/// ReducedTier); WebGL never dresses map doors (FrontRoomsInteractableKit.DressOnWebGL). Render-only: no collider, no
/// light, no sound, no gameplay read-back. One component per alarm door on its "Leaf rig", stepped in LateUpdate (about
/// ten compares when idle); edit-mode tools step it with TickAllForTools.
/// </summary>
public sealed class FrontRoomsAlarmDoorLook : MonoBehaviour
{
    public const string BodyKit = "Kit_AlarmExit_Body", PadKit = "Kit_AlarmExit_Pad", HornKit = "Kit_AlarmExit_Horn", PlacardKit = "Kit_AlarmExit_Placard";
    public const string BarLabel = "Alarm bar", PadLabel = "Alarm bar pad", PlacardLabel = "Alarm placard", HornLabel = "Alarm horn";

    /// <summary>The placard's face (Resources/Print/AlarmDoor/alarm_door_placard_albedo.png) is a STAND-IN drawn with the
    /// signed words (ALARM WILL SOUND) in TeX Gyre Heros Bold; the information design line's face replaces it in place.</summary>
    public const bool PlacardFaceIsStandIn = true;

    // ---------------------------------------------------------------- mounts (door root D, metres; alarm_exit_common.py)
    /// <summary>The leaf faces (+-0.022) and the device datum: the housing's latch-side edge on the pad's centreline.</summary>
    public const float FaceX = .022f, DatumZ = .975f;
    /// <summary>Bar datum heights: free doors (where the lever was), locked doors (above the knob trim, under the lite).</summary>
    public const float FreeDatumY = 1.000f, LockedDatumY = 1.097f;
    /// <summary>The pad's travel into the rail at a full press (alarm_exit_common.PAD_TRAVEL).</summary>
    public const float PadTravel = .012f;
    /// <summary>The placard's centre on both faces: beside the F1 lite on the latch-side panel, at eye level.</summary>
    public const float PlacardY = 1.420f, PlacardZ = .770f;
    /// <summary>The horn's back-plate centre: over the opening's centre, its bottom 2.180 m (124 mm plate).</summary>
    public const float HornY = 2.242f, HornZ = .500f;

    // ---------------------------------------------------------------- motion (seconds)
    /// <summary>Red's 10A: the facing push is held this long while the bar travels (the map owns the hold).</summary>
    public const float HoldSeconds = .4f;
    /// <summary>How fast the shown press may rise (a held push ramps slower than this, so it is followed exactly).</summary>
    public const float RiseSeconds = .04f;
    /// <summary>The spring back to rest once nothing presses the pad.</summary>
    public const float ReleaseSeconds = .15f;
    /// <summary>The trip beat (no map hook, or the E open): in, held, then the spring back.</summary>
    public const float BeatIn = .08f, BeatHold = .10f;
    /// <summary>A trip whose pad was pressed this far in the last PeakWindow seconds plays no beat (the push showed it; at
    /// 15 fps the last frame before the open shows 0.83 of the hold).</summary>
    public const float BeatSkipPeak = .75f, PeakWindow = .3f;

    static readonly Quaternion FaceS = Quaternion.Euler(0f, 90f, 0f), FaceP = Quaternion.Euler(0f, -90f, 0f);
    static readonly List<FrontRoomsAlarmDoorLook> all = new List<FrontRoomsAlarmDoorLook>();
    static readonly List<Renderer> scratch = new List<Renderer>();
    static bool stageHooked;

    /// <summary>Every live alarm door look (tools, tests).</summary>
    public static IReadOnlyList<FrontRoomsAlarmDoorLook> All => all;

    /// <summary>Tools: force the handheld tier (LOD0 dropped) in the editor.</summary>
    public static bool ForceReducedForTools;

    /// <summary>Handhelds (and WebGL, which never dresses doors): the kits' LOD0 is dropped; nothing else changes.</summary>
    public static bool ReducedTier => ForceReducedForTools || FrontRoomsAlarmTell.ReducedTier;

    /// <summary>The device kits are in the build (else the door keeps its lever and gets no alarm look).</summary>
    public static bool KitsAvailable => FrontRoomsKitLibrary.Has(BodyKit) && FrontRoomsKitLibrary.Has(PadKit);

    public FrontRoomsMapWorld Map { get; private set; }
    public FrontRoomsMapWorld.Door Door { get; private set; }
    /// <summary>The bar rides above the knob trim (a LOCKED model).</summary>
    public bool LockedLayout { get; private set; }
    /// <summary>The face the horn hangs on: 0 = the door root's +X (S), 1 = -X.</summary>
    public int HornSide { get; private set; }
    /// <summary>The LED shows the tripped (latched) state.</summary>
    public bool ShowsTripped { get; private set; }
    /// <summary>The leaf-mounted parts were taken down (a break's FINAL, or a door rebuilt broken).</summary>
    public bool LeafPartsDown { get; private set; }

    // Parts. Index 0 = the root's +X face, 1 = -X.
    readonly GameObject[] bar = new GameObject[2];
    readonly Transform[] pad = new Transform[2];
    readonly Vector3[] padRest = new Vector3[2];
    readonly Vector3[] padIn = new Vector3[2];
    readonly GameObject[] placard = new GameObject[2];
    GameObject horn;
    readonly float[] shown = new float[2];
    readonly float[] applied = { -1f, -1f };
    readonly List<Renderer> leds = new List<Renderer>();
    Material ledAsset;
    int ledPhase;
    bool ledApplied;
    float peak, peakAge = 1e3f, sideAge = 1e3f;
    int lastSide;
    float beatClock = -1f, beatFrom;
    int beatSide;
    bool checkedRestore;

    // ---------------------------------------------------------------- dress

    /// <summary>
    /// Dress one alarm door (FrontRoomsInteractableKit.DressDoor, after the parts row is built and before the rig collects its
    /// LODs). Returns the look, or null when the device kits are missing. Render-only; throws only what the kit library
    /// throws (the caller's guard logs it and keeps the door).
    /// </summary>
    public static FrontRoomsAlarmDoorLook Attach(FrontRoomsMapWorld map, FrontRoomsMapWorld.Door door, FrontRoomsDoorRig rig, Transform frame, bool lockedModel)
    {
        if (map == null || door == null || rig == null || !KitsAvailable) return null;
        var look = rig.GetComponent<FrontRoomsAlarmDoorLook>();
        if (look == null) look = rig.gameObject.AddComponent<FrontRoomsAlarmDoorLook>();
        look.Build(map, door, rig.transform, frame, lockedModel);
        return look;
    }

    void Build(FrontRoomsMapWorld map, FrontRoomsMapWorld.Door door, Transform leaf, Transform frame, bool lockedModel)
    {
        Map = map;
        Door = door;
        LockedLayout = lockedModel;
        HookStages();
        if (!all.Contains(this)) all.Add(this);
        ledAsset = FrontRoomsSurfaces.TryGet(FrontRoomsAlarmTell.LedSlot);
        ledPhase = FrontRoomsAlarmTell.PhaseOf(door.position);
        var datumY = lockedModel ? LockedDatumY : FreeDatumY;
        var built = !door.broken;
        for (var side = 0; side < 2; side++)
        {
            var sgn = side == 0 ? 1f : -1f;
            var rot = side == 0 ? FaceS : FaceP;
            var mirror = side == 0 ? Vector3.one : new Vector3(-1f, 1f, 1f);
            var face = side == 0 ? " s" : " p";
            if (built)
            {
                var at = new Vector3(sgn * FaceX, datumY, DatumZ);
                bar[side] = Spawn(BodyKit, leaf, at, rot, mirror, BarLabel + face);
                var p = Spawn(PadKit, leaf, at, rot, mirror, PadLabel + face);
                if (p != null)
                {
                    pad[side] = p.transform;
                    padRest[side] = p.transform.localPosition;
                    // Into the leaf: part -Z, which the face rotation turns onto door -X (S) or +X (P).
                    padIn[side] = rot * Vector3.back;
                }
                if (FrontRoomsKitLibrary.Has(PlacardKit))
                    placard[side] = Spawn(PlacardKit, leaf, new Vector3(sgn * FaceX, PlacardY, PlacardZ), rot, Vector3.one, PlacardLabel + face);
            }
        }
        HornSide = HornSideOf(map, door);
        if (frame != null && FrontRoomsKitLibrary.Has(HornKit))
        {
            var hs = HornSide == 0 ? 1f : -1f;
            horn = Spawn(HornKit, frame, new Vector3(hs * ModuleUnits.WallHalf, HornY, HornZ), HornSide == 0 ? FaceS : FaceP, Vector3.one, HornLabel);
        }
        CollectLeds();
        LeafPartsDown = !built;
        ShowsTripped = map.IsAlarmTripped(door.edge);
        ledApplied = false;
        ApplyLed();
        for (var i = 0; i < 2; i++) { shown[i] = 0f; applied[i] = -1f; }
        ApplyPads();
    }

    static GameObject Spawn(string kit, Transform parent, Vector3 at, Quaternion rot, Vector3 scale, string label)
    {
        var go = FrontRoomsKitLibrary.Spawn(kit, parent, at, rot, scale, false, label, false);
        if (go == null) return null;
        if (ReducedTier) DropLod0(go);
        return go;
    }

    // The owner zone's face (the zone whose budget slot alarmed the door); a tools mark has no owner: the S face.
    static int HornSideOf(FrontRoomsMapWorld map, FrontRoomsMapWorld.Door door)
    {
        var planner = map.AlarmDoors;
        if (planner == null || map.Cache == null || !planner.AlarmedZone(door.edge, out var owner)) return 0;
        var probe = door.position + RootRight(door) * (MapGrid.CellSize * .5f);
        return map.ZoneOf(map.CellOf(probe)).id == owner ? 0 : 1;
    }

    static Vector3 RootRight(FrontRoomsMapWorld.Door door) =>
        (door.hinge != null && door.hinge.parent != null ? door.hinge.parent.rotation : Quaternion.identity) * door.closed * Vector3.right;

    void CollectLeds()
    {
        leds.Clear();
        if (ledAsset == null) return;
        for (var side = 0; side < 2; side++)
        {
            if (bar[side] == null) continue;
            bar[side].GetComponentsInChildren(true, scratch);
            foreach (var r in scratch)
            {
                var mats = r.sharedMaterials;
                for (var i = 0; i < mats.Length; i++)
                    if (FrontRoomsAlarmTellLed.IsLed(mats[i], ledAsset)) { leds.Add(r); break; }
            }
            scratch.Clear();
        }
    }

    // Reduced tier: LOD1's renderers take LOD0's place (down to LOD1's own switch); LOD0's renderers go off.
    static void DropLod0(GameObject go)
    {
        var group = go.GetComponent<LODGroup>();
        if (group == null) return;
        var lods = group.GetLODs();
        if (lods.Length < 2) return;
        var drop = lods[0].renderers;
        var keep = new LOD[lods.Length - 1];
        keep[0] = new LOD(lods[1].screenRelativeTransitionHeight, lods[1].renderers);
        for (var i = 2; i < lods.Length; i++) keep[i - 1] = lods[i];
        group.SetLODs(keep);
        if (drop != null)
            foreach (var r in drop)
                if (r != null) r.enabled = false;
    }

    // ---------------------------------------------------------------- break (FrontRoomsDoorBreakVisual's stage event)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        stageHooked = false;
        all.Clear();
    }

    static void HookStages()
    {
        if (stageHooked || !Application.isPlaying) return;
        stageHooked = true;
        FrontRoomsDoorBreakVisual.DoorStageChanged += OnStage;
    }

    static void OnStage(FrontRoomsMapWorld.Door door, int stage, bool steel)
    {
        if (door == null || stage < 4) return;
        for (var i = 0; i < all.Count; i++)
        {
            var l = all[i];
            if (l != null && l.Door == door) l.TakeDownLeafParts();
        }
    }

    /// <summary>The leaf-mounted parts come off on both faces (a break's FINAL tore the latch strip that carried the
    /// housing); the horn stays on the wall. Idempotent.</summary>
    public void TakeDownLeafParts()
    {
        if (LeafPartsDown) return;
        LeafPartsDown = true;
        for (var side = 0; side < 2; side++)
        {
            if (bar[side] != null) bar[side].SetActive(false);
            if (pad[side] != null) pad[side].gameObject.SetActive(false);
            if (placard[side] != null) placard[side].SetActive(false);
        }
        beatClock = -1f;
    }

    // ---------------------------------------------------------------- per frame

    void OnDestroy()
    {
        all.Remove(this);
    }

    void LateUpdate()
    {
        if (Map == null || Door == null) return;
        Tick(Map.DoorsPaused ? 0f : Time.deltaTime);
    }

    /// <summary>Edit mode (tools, captures): step every look as LateUpdate would.</summary>
    public static void TickAllForTools(float dt)
    {
        for (var i = all.Count - 1; i >= 0; i--)
        {
            if (all[i] == null) { all.RemoveAt(i); continue; }
            all[i].Tick(dt);
        }
    }

    void Tick(float dt)
    {
        if (Map == null || Door == null) return;
        if (!checkedRestore)
        {
            // A rebuilt chunk restores a broken door after DressDoor attached us (RestoreOnDress raises no stage).
            checkedRestore = true;
            var visual = GetComponent<FrontRoomsDoorBreakVisual>();
            if (visual != null && visual.Stage >= 4) TakeDownLeafParts();
        }
        if (!LeafPartsDown && Door.broken) TakeDownLeafParts();

        var tripped = Map.IsAlarmTripped(Door.edge);
        if (tripped != ShowsTripped)
        {
            ShowsTripped = tripped;
            ApplyLed();
            if (tripped) BeginTripBeat();
        }
        if (LeafPartsDown) return;

        // The map's hold (contract): + from the root's +X side, - from -X; 0 with no push or no hook.
        var hook = FrontRoomsInteractableKit.AlarmBarPressFromMap;
        var signed = hook != null ? hook(Map, Door) : 0f;
        var magnitude = Mathf.Clamp01(Mathf.Abs(signed));
        if (magnitude > 0f)
        {
            lastSide = signed > 0f ? 0 : 1;
            sideAge = 0f;
            if (magnitude >= peak || peakAge > PeakWindow) peak = magnitude;
            peakAge = 0f;
        }
        else
        {
            sideAge += dt;
            peakAge += dt;
            if (peakAge > PeakWindow) peak = 0f;
        }
        var beat = 0f;
        if (beatClock >= 0f)
        {
            beatClock += dt;
            beat = beatClock < BeatIn ? Mathf.Lerp(beatFrom, 1f, beatClock / BeatIn) : 1f;
            if (beatClock >= BeatIn + BeatHold) { beat = 0f; beatClock = -1f; }
        }
        for (var side = 0; side < 2; side++)
        {
            var target = side == lastSide && magnitude > 0f ? magnitude : 0f;
            if (side == beatSide && beat > target) target = beat;
            var s = shown[side];
            if (target > s) s = dt > 0f ? Mathf.Min(target, s + dt / RiseSeconds) : s;
            else if (target < s) s = Mathf.Max(target, s - dt / ReleaseSeconds);
            shown[side] = s;
        }
        ApplyPads();
    }

    void BeginTripBeat()
    {
        if (LeafPartsDown) return;
        // The push already carried the pad home (the map's hold): no beat, only the spring back.
        if (peak >= BeatSkipPeak && peakAge <= PeakWindow) return;
        beatSide = sideAge <= .5f ? lastSide : CameraSide();
        beatFrom = shown[beatSide];
        beatClock = 0f;
    }

    // The face the camera is on (the player who opened it): 0 = +X.
    int CameraSide()
    {
        var cam = Camera.main;
        if (cam == null) return 0;
        return Vector3.Dot(cam.transform.position - Door.position, RootRight(Door)) >= 0f ? 0 : 1;
    }

    void ApplyPads()
    {
        for (var side = 0; side < 2; side++)
        {
            var t = pad[side];
            if (t == null) continue;
            var s = shown[side];
            if (s == applied[side]) continue;
            applied[side] = s;
            // Smoothstep: the spring resists first, the pad runs, then seats at the bottom of its travel.
            var e = s * s * (3f - 2f * s);
            t.localPosition = padRest[side] + padIn[side] * (PadTravel * e);
        }
    }

    void ApplyLed()
    {
        if (ledAsset == null || leds.Count == 0) return;
        var want = ShowsTripped ? FrontRoomsAlarmTellLed.LatchMaterial(ledAsset) : FrontRoomsAlarmTellLed.PhaseMaterial(ledAsset, ledPhase);
        if (want == null) want = ledAsset;
        for (var k = 0; k < leds.Count; k++)
        {
            var r = leds[k];
            if (r == null) continue;
            var mats = r.sharedMaterials;
            var changed = false;
            for (var i = 0; i < mats.Length; i++)
                if (FrontRoomsAlarmTellLed.IsLed(mats[i], ledAsset) && mats[i] != want) { mats[i] = want; changed = true; }
            if (changed) r.sharedMaterials = mats;
        }
        ledApplied = true;
    }

    // ---------------------------------------------------------------- tools and tests

    /// <summary>Tests: the press each face shows now (0..1), the horn and the parts.</summary>
    public float ShownPressForTests(int side) => shown[side];
    public GameObject BarForTests(int side) => bar[side];
    public Transform PadForTests(int side) => pad[side];
    public GameObject PlacardForTests(int side) => placard[side];
    public GameObject HornForTests => horn;
    public bool LedAppliedForTests => ledApplied;
}
