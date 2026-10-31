// FrontRoomsDoorBreakVisual.cs -- the door break, production P1 (2026-10-30; prototype DB2 the same morning). Red approved the
// look 2026-10-30 ("1a，2 要，3 要，4 预算，5 要"): final pose (a), kept blows, baked throws (4 per family), the leak card,
// the walnut face. Design: Documentation/research/door_break/50_rethink.md 3.2-3.4, 3.7-3.9; dust 54_dust_grit.md; log
// 52_production.md.
//
// Render-only: the map owns the break (timings, collider, the throw); nothing here feeds back into gameplay. It sits on a
// dressed door's "Leaf rig" (FrontRoomsDoorRig) and is added on the first blow (FrontRoomsDoorBreakDriver) or on a restore,
// so intact doors carry nothing. It hangs on every leaf of the veneer family (Kit_DoorLeaf_Veneer_DA, _Veneer_Oak_DA,
// _Ward_DA: the "slot swap" gives the break the intact leaf's own face, hardware and plate surfaces; the Ward leaf's armor
// plates come as their own parts). Steel leaves wait for P3 (no visual: today's break).
//   Blow(i, n, from)  the stage clock counts back from the last blow: stage(i) = 4 - (n - 1 - i) (E, M, D1, D2, FINAL); the
//                     shown stage is max(kept, mapped), so damage is never undone (Red's Q2: kept blows). The first
//                     damaging stage swaps the intact leaf for the pre-fractured body + the cassette of this door's split
//                     motif (one of 3 by the edge hash), on this frame, under the blow: an elastic rock (6/9/12/16/16 mm at
//                     the latch end, out in 0.03 s, a 2/2/3-frame hit-stop at D1/D2/FINAL, back in 0.12 s), the dust and
//                     grit of the beat (FrontRoomsDoorBreakVfx), and a pulse on the leak card and the shaft.
//   FINAL             the torn cassette; the baked throw (one of 4 by the edge hash, played relative to this motif's rests)
//                     frees the latch strip with the faceplate and the rig's latchbolt, splinters and crumb clusters.
//   Broken(from)      after the map's BreakDoor: the breach light flares and fades (desktop); the leaf takes pose (a) (the
//                     top pivot tore: a 3 deg lean about the bottom pivot, about 110 mm at the head, + a 0.6 deg sag) as
//                     the map's bounce settles; the pivot tears dust out of the head and the leaf's swing pushes a gust.
//   settle            the debris and the resting crumbs and chips merge into one floor mesh (one draw per material), in
//                     slices of <= 1 ms; the record is stamped with the neighbourhood hash; the floor pack keeps the dust
//                     skin and the crumbs for an exact restore.
//   Restore           DressDoor (a rebuilt chunk, 48+ m away): body + the kept stage, or the torn leaf in pose (a), the
//                     debris at rest, the floor pack, the drooped levers; after a shift, a piece that would sit inside a
//                     new prop is dropped (the env check, GD3c's rule).
// Phones (P5, 2026-10-30; PhoneTier: FrontRoomsHandheld.Active with PhoneTierOnHandheld, or the lab's DebugForcePhoneTier):
// the LOD1 cassettes and frame inserts at every distance (their _L0 parts never show), the FINAL's debris capped at the 12
// most visible pieces (the strip always) and drawn as ONE combined dynamic mesh (one draw per material, its vertices moved
// on the CPU from the baked keys; the pieces keep their transforms for the latch, the merge and the env check), the phone
// dust counts and caps (FrontRoomsDoorBreakVfx.Tier.Phone), no breach light. Every phone branch tests Phone, so a desktop
// or Editor run takes exactly the code it took before P5 (W/door_break/P5: the desktop state and pixel digests match).
// WebGL does not dress doors (DressOnWebGL = false), so it never gets here.
// P3 (2026-10-30): the HOLLOW-METAL family (Kit_DoorLeafBreak_Steel_DA on Steel_DA and its PaintedMetal variant;
// Kit_DoorLeafBreak_SteelLite_DA on SteelLite_DA, sharing the Steel cassettes). Its sidecar says "material": "steel": the
// cassettes are the intact leaf cut and deformed (dish, bow, crushed lock edge, the parted seam showing kraft honeycomb);
// the face is Door_Enamel (its stage materials carry the crazing and primer chips); the face hardware (escutcheon, cylinder,
// plug, knob, number plates) rides the bow and the dish; the latch and the deadbolt ride the crushed edge and bend at FINAL;
// the leaf stays whole (no strip: the latch stays in the leaf). Red's Q3: steel takes 6 blows on T1 (E E M D1 D2 FINAL; map ask M7).
// Door vision (2026-10-30): the lited leaves' bases are the intact lited leaf minus the window (door_break_lite_bases.py): the
// lite stays through every stage; Kit_DoorLeafBreak_Ward_DA and _SteelRun_DA carry F5 and share their parents' cassettes.
// P4 (2026-10-30): the FRAME's strike zone (Kit_DoorFrameBreak_Wood_DA / _Steel_DA): from its first changed stage the
// intact frame's renderers are switched off and the frame's body + insert are shown: the wood jamb splits at FINAL (the
// strip and its casing pried toward the burst side, splinter teeth); the steel sleeve dents and its brush fin bends (D2,
// FINAL). The strike (the rig's part) tilts toward the burst side at D1/D2 and falls at the break (DetachStrike) as today.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(500)]
public sealed class FrontRoomsDoorBreakVisual : MonoBehaviour
{
    public const string FamilyName = "Kit_DoorLeafBreak_Veneer_DA";
    /// <summary>The break families, each a base kit whose sidecar lists the intact leaves it hangs on (hangsOn). P3 adds
    /// Kit_DoorLeafBreak_Steel_DA (Steel_DA, SteelLite_DA); a family whose kit is missing is skipped.</summary>
    public static readonly string[] FamilyNames = { FamilyName, "Kit_DoorLeafBreak_Steel_DA", "Kit_DoorLeafBreak_SteelLite_DA", "Kit_DoorLeafBreak_Ward_DA", "Kit_DoorLeafBreak_SteelRun_DA" };
    /// <summary>P4: the frames' strike-zone families (sidecar hangsOn = the intact frame kits).</summary>
    public static readonly string[] FrameFamilyNames = { "Kit_DoorFrameBreak_Wood_DA", "Kit_DoorFrameBreak_Steel_DA" };
    public enum FinalPose { A, B }

    /// <summary>P5: handheld players (FrontRoomsHandheld.Active: iOS / Android players, the Editor's handheld preview) take the
    /// phone tier. On by default: handheld doors are dressed (DressOnHandheld), so their breaks must not run desktop counts.</summary>
    public static bool PhoneTierOnHandheld = true;
    /// <summary>Lab only (never set by the game): run the phone tier in the Editor to film it (W/door_break/P5).</summary>
    public static bool DebugForcePhoneTier;
    /// <summary>The phone tier now (read once per door, at Init).</summary>
    public static bool PhoneTier => DebugForcePhoneTier || (PhoneTierOnHandheld && FrontRoomsHandheld.Active);
    /// <summary>P5 (50_rethink 3.9 "Live debris": phones <= 12, one combined dynamic mesh).</summary>
    public const int PhoneDebrisCap = 12;

    /// <summary>Sound ask S2 (research/door_break/53_for_sound.md): a stage change, raised on its swap frame: the door, the
    /// stage (0 E, 1 M, 2 D1, 3 D2, 4 FINAL, 5 BROKEN), and whether the leaf is steel. A kept stage struck again raises
    /// nothing, and a restore raises nothing. No subscriber today; render-only code never reads it back.</summary>
    public static event Action<FrontRoomsMapWorld.Door, int, bool> DoorStageChanged;
    /// <summary>Sound ask S3: one contact of the baked throw as its time passes: the world point, the speed in m/s, the
    /// surface ("leaf" or "carpet"), the piece's name. Fired only while a throw plays (never on a restore), and only for pieces
    /// that still show (a phone drops some). No subscriber today.</summary>
    public static event Action<Vector3, float, string, string> DebrisContact;

    /// <summary>Shown stage: -1 intact (S0), 0 E, 1 M, 2 D1, 3 D2, 4 FINAL (torn), 5 BROKEN.</summary>
    public int Stage { get; private set; } = -1;
    public static readonly string[] StageLabel = { "E", "M", "D1", "D2", "FINAL", "BROKEN" };
    static readonly string[] CasName = { "M", "D1", "D2", "TORN" };
    public static string LabelOf(int stage) => stage < 0 ? "S0" : StageLabel[Mathf.Min(stage, 5)];

    /// <summary>The pose the broken leaf takes: (a), Red's pick 2026-10-30. (b) stays for tools.</summary>
    public FinalPose Pose = FinalPose.A;
    /// <summary>+1: the leaf bursts toward the rig's +X (canonical); -1: the break root is mirrored.</summary>
    public float BurstSign { get; private set; } = 1f;
    public FrontRoomsDoorRig Rig { get; private set; }
    public bool Swapped { get; private set; }
    public bool IsBroken { get; private set; }
    public bool Restored { get; private set; }
    public int Motif { get; private set; }
    public int ThrowVariant { get; private set; }
    public bool Phone { get; private set; }
    public bool Ward { get; private set; }
    public string LeafKit { get; private set; } = "";
    public string FamilyKit => fam != null ? fam.name : "";
    /// <summary>P3: the family is the hollow-metal one (its sidecar's material).</summary>
    public bool Steel => fam != null && fam.material == "steel";
    public string FrameKit => frameFam != null ? frameFam.name : "";
    public string FrameStageShown { get; private set; } = "";
    public int FrameSwapFrame { get; private set; } = -1;
    /// <summary>Tools: Time.frameCount of each stage's swap (index = stage), -1 when it never swapped.</summary>
    public readonly int[] SwapFrame = { -1, -1, -1, -1, -1, -1 };
    /// <summary>Tools: the hit-stop frames actually held per stage.</summary>
    public readonly int[] HeldFrames = new int[6];
    public string LastNote { get; private set; } = "";
    public string IntactShadowNote { get; private set; } = "";
    public float FarLampLevel { get; private set; }
    public string ThrowMethod => throwData != null ? throwData.method : "none";
    public float ThrowSettle => throwData != null ? throwData.settle : -1f;
    public int BlowsTaken { get; private set; }
    // merge / restore stats (tools)
    public int MergeState { get; private set; }
    public float MergeWorstMs { get; private set; }
    public int MergeFrames { get; private set; }
    public int MergedPieces { get; private set; }
    public int MergedSolids { get; private set; }
    public int MergedTris { get; private set; }
    public int MergedDraws { get; private set; }
    public int DroppedByEnv { get; private set; }
    public string RestoreNote { get; private set; } = "";
    public FrontRoomsDoorBreakVfx Vfx => vfx;

    // ---- numbers (50_rethink 3.3, 3.8, 3.9)
    static readonly float[] RockMetres = { .006f, .009f, .012f, .016f, .016f };
    static readonly int[] HoldFrames = { 0, 0, 2, 2, 3 };
    static readonly float[] LeakBase = { 0f, 0f, .15f, .40f, 1f };
    const float RockOut = .03f, RockBack = .12f;
    const float LeakPulse = .12f, LeakHdr = 2.4f, ShaftGain = 3.0f;
    const float PoseALeanDeg = 3f, PoseASagDeg = .6f, PoseBTurnDeg = .8f, PoseBTwistDeg = 1.5f;
    const float BreachFlare = .3f, BreachFade = 1.0f;
    const double SliceMs = 1.0;
    static readonly Color LampColour = new Color(1f, .93f, .80f);

    // ---- the family (sidecar), the throws
    [Serializable] public sealed class Anchor { public string name; public float[] pos; }
    [Serializable] public sealed class StageInfo { public string stage; public float gapAtBore; public float stripDxLatch; public float[] splitY; public float bow, dish, dishR, crush, peel, peel2; }
    [Serializable] public sealed class DebrisRest { public string name; public float[] pos; public float[] size; public int tris; }
    [Serializable] public sealed class MotifInfo { public int index; public int seed; public string model; public string debris; public float[] ys; public float[] zs; public StageInfo[] stageInfo; public DebrisRest[] debrisRest; public float bowY, bowZ, dishY, dishZ, crushPk; public float[] peel; }
    [Serializable] public sealed class Palette { public string name; public float[] linear; }
    [Serializable] public sealed class Window { public float[] z; public float[] y; }
    [Serializable] public sealed class Family { public string name; public string material; public string[] hangsOn; public Anchor[] anchors; public MotifInfo[] motifs; public string[] throws; public Palette[] palettes; public Window window; }
    [Serializable] public sealed class FrameStage { public string name; public int from; }
    [Serializable] public sealed class SlotSwap { public string kit; public string from; public string to; }
    [Serializable] public sealed class FrameFamily { public string name; public string material; public string[] hangsOn; public FrameStage[] stages; public SlotSwap[] slotSwaps; }
    [Serializable] public sealed class ThrowCurve { public float deg; public float seconds; public float bounceSeconds; public float restDeg; public float pivotZ; }
    [Serializable] public sealed class ThrowPiece { public string name; public float[] rest; public float[] keys; }
    [Serializable] public sealed class ThrowContact { public float t; public string piece; public float speed; public string surface; }
    [Serializable] public sealed class ThrowData { public string method; public int hz; public float breakAt; public int frames; public float settle; public bool relative; public ThrowCurve @throw; public ThrowPiece[] pieces; public ThrowContact[] contacts; }

    static readonly Dictionary<string, Family> families = new Dictionary<string, Family>();
    static readonly Dictionary<string, FrameFamily> frameFamilies = new Dictionary<string, FrameFamily>();
    static readonly Dictionary<string, ThrowData> throwCache = new Dictionary<string, ThrowData>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { families.Clear(); frameFamilies.Clear(); throwCache.Clear(); DebugForcePhoneTier = false; DoorStageChanged = null; DebrisContact = null; }

    /// <summary>P4: the strike-zone family that hangs on this intact frame kit, or null (the frame stays as it is).</summary>
    public static FrameFamily FrameFamilyFor(string frameKit)
    {
        if (string.IsNullOrEmpty(frameKit)) return null;
        foreach (var n in FrameFamilyNames)
        {
            if (!frameFamilies.TryGetValue(n, out var f))
            {
                var t = Resources.Load<TextAsset>(FrontRoomsKitLibrary.ModelFolder + n);
                f = t != null ? JsonUtility.FromJson<FrameFamily>(t.text) : null;
                if (f != null && (f.hangsOn == null || f.stages == null || f.stages.Length == 0 || !FrontRoomsKitLibrary.Has(n))) f = null;
                if (f != null && string.IsNullOrEmpty(f.name)) f.name = n;
                frameFamilies[n] = f;
            }
            if (f != null && Array.IndexOf(f.hangsOn, frameKit) >= 0) return f;
        }
        return null;
    }

    /// <summary>A break family's sidecar (null when its kit is not in this build).</summary>
    public static Family LoadFamily(string name = FamilyName)
    {
        if (families.TryGetValue(name, out var f)) return f;
        var t = Resources.Load<TextAsset>(FrontRoomsKitLibrary.ModelFolder + name);
        f = t != null ? JsonUtility.FromJson<Family>(t.text) : null;
        if (f != null && (f.motifs == null || f.motifs.Length == 0 || f.hangsOn == null || !FrontRoomsKitLibrary.Has(name))) f = null;
        if (f != null && string.IsNullOrEmpty(f.name)) f.name = name;
        families[name] = f;
        return f;
    }

    /// <summary>The family that hangs on this intact leaf kit, or null (no break visual: today's break).</summary>
    public static Family FamilyFor(string leafKit)
    {
        if (string.IsNullOrEmpty(leafKit)) return null;
        foreach (var n in FamilyNames)
        {
            var f = LoadFamily(n);
            if (f != null && Array.IndexOf(f.hangsOn, leafKit) >= 0) return f;
        }
        return null;
    }

    public static ThrowData LoadThrow(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (throwCache.TryGetValue(name, out var d)) return d;
        var t = Resources.Load<TextAsset>(FrontRoomsKitLibrary.ModelFolder + name);
        d = t != null ? JsonUtility.FromJson<ThrowData>(t.text) : null;
        throwCache[name] = d;
        return d;
    }

    // ---- scene
    FrontRoomsMapWorld.Door door;
    FrontRoomsMapWorld map;
    Family fam;
    MotifInfo motif;
    Transform breakPose, kitRoot, debrisRoot, floorGo;
    GameObject intactLeaf, leafShadow;
    readonly Dictionary<string, GameObject> parts = new Dictionary<string, GameObject>();
    readonly List<Transform> movedChildren = new List<Transform>();
    LODGroup lodGroup;
    Material leakMat;
    Material faceMat, hardwareMat, plateMat;
    Light breach;
    FrontRoomsDoorBreakVfx vfx;
    float pivotZ;
    bool hardFloor;
    Vector3 fromPos;

    // ---- parts of the rig (index 0 = burst face, 1 = push face), rests in the Break pose's space
    readonly Transform[] rose = new Transform[2], lever = new Transform[2];
    readonly Vector3[] roseRestP = new Vector3[2], leverRestP = new Vector3[2];
    readonly Quaternion[] roseRestQ = new Quaternion[2], leverRestQ = new Quaternion[2];
    Transform latch, latchParent;
    Vector3 latchRestP;
    Quaternion latchRestQ;

    // ---- clocks
    float rockClock = -1f, rockMetres;
    int rockHoldFrames, rockHeldSoFar, rockStage;
    bool rockHolding;
    float leakPulse;
    float brokenClock = -1f, finalClock = -1f;
    float breachLevel;
    int envCheckIn = -1;

    // ---- throw
    ThrowData throwData;
    readonly List<Transform> debrisPieces = new List<Transform>();
    readonly List<ThrowPiece> debrisKeys = new List<ThrowPiece>();
    readonly List<Vector3> debrisRest = new List<Vector3>();
    GameObject debrisGo;

    // =========================================================================================== set-up

    /// <summary>The intact leaf kit a rig hangs (its row's), or null.</summary>
    public static string LeafKitOf(FrontRoomsDoorRig rig)
    {
        if (rig == null || rig.Acting != DoorActing.Double) return null;
        var row = FrontRoomsInteractableKit.DoorRowFor(rig.Member, rig.LockedModel);
        return row != null ? row.leaf : null;
    }

    /// <summary>The break visual of a dressed door (added on first use), or null for an undressed door, a leaf outside the
    /// family (steel: P3) or a missing kit.</summary>
    public static FrontRoomsDoorBreakVisual Ensure(FrontRoomsDoorRig rig)
    {
        if (rig == null || rig.Door == null) return null;
        var v = rig.GetComponent<FrontRoomsDoorBreakVisual>();
        if (v != null) return v;
        var leaf = LeafKitOf(rig);
        var fam = FamilyFor(leaf);
        if (fam == null) return null;
        v = rig.gameObject.AddComponent<FrontRoomsDoorBreakVisual>();
        v.Init(rig, fam, leaf);
        return v;
    }

    void Init(FrontRoomsDoorRig rig, Family family, string leaf)
    {
        fam = family;
        Rig = rig;
        door = rig.Door;
        map = rig.Map;
        LeafKit = leaf;
        pivotZ = rig.VisualPivotZ;
        Phone = PhoneTier;
        var hasRec = FrontRoomsDoorBreakRecords.TryGet(map, door.edge, out var rec);
        Motif = hasRec ? Mathf.Clamp(rec.motif, 0, fam.motifs.Length - 1) : (int)(FrontRoomsDoorBreakRecords.Hash(door.edge, 1) % (uint)fam.motifs.Length);
        var nThrows = fam.throws != null && fam.throws.Length > 0 ? fam.throws.Length : 1;
        ThrowVariant = hasRec ? Mathf.Clamp(rec.throwVariant, 0, nThrows - 1) : (int)(FrontRoomsDoorBreakRecords.Hash(door.edge, 2) % (uint)nThrows);
        motif = fam.motifs[Motif];
        throwData = LoadThrow(fam.throws != null && fam.throws.Length > 0 ? fam.throws[ThrowVariant] : fam.name + "_throw0");
        if (hasRec) { BurstSign = rec.burst < 0 ? -1f : 1f; BlowsTaken = (rec.flags >> 8) & 15; }
        var row = rig.Acting == DoorActing.Double ? FrontRoomsInteractableKit.DoorRowFor(rig.Member, rig.LockedModel) : null;
        frameKitName = row != null ? row.frame : null;
        frameFam = FrameFamilyFor(frameKitName);
        woodFrame = frameKitName != null && frameKitName.IndexOf("Wood", StringComparison.Ordinal) >= 0;
    }
    string frameKitName;
    FrameFamily frameFam;
    bool woodFrame = true;

    float StripDx(string stage)
    {
        if (motif == null || motif.stageInfo == null) return 0f;
        foreach (var s in motif.stageInfo) if (s != null && s.stage == stage) return s.stripDxLatch;
        return 0f;
    }

    // The "Break pose" node under the rig: every rig child moves under it, so the rock, the lean and the lockset move together.
    void EnsurePoseNode()
    {
        if (breakPose != null) return;
        var t = transform;
        breakPose = new GameObject("Break pose").transform;
        breakPose.SetParent(t, false);
        movedChildren.Clear();
        for (var i = t.childCount - 1; i >= 0; i--)
        {
            var c = t.GetChild(i);
            if (c == breakPose) continue;
            movedChildren.Add(c);
        }
        foreach (var c in movedChildren) c.SetParent(breakPose, false);
        intactLeaf = Find(FrontRoomsInteractableKit.LeafLabel);
        leafShadow = Find(FrontRoomsInteractableKit.LeafShadowLabel);
        latch = breakPose.Find("Lock latch");
        if (latch != null) { latchParent = latch.parent; latchRestP = latch.localPosition; latchRestQ = latch.localRotation; }
        ReadIntactSurfaces();
    }

    GameObject Find(string label)
    {
        var c = breakPose.Find(label);
        return c != null ? c.gameObject : null;
    }

    // The slot swap: the break wears the intact leaf's own surfaces (its face: the walnut Door_Veneer, or the Ward leaf's
    // laminate; its hardware: brass, or satin chrome on the chrome rows; the Ward leaf's stainless armor plates).
    void ReadIntactSurfaces()
    {
        faceMat = hardwareMat = plateMat = null;
        if (intactLeaf == null) return;
        foreach (var r in intactLeaf.GetComponentsInChildren<MeshRenderer>(true))
            foreach (var m in r.sharedMaterials)
            {
                if (m == null) continue;
                var n = m.name;
                if (n.StartsWith("Door_Veneer", StringComparison.Ordinal) || n.StartsWith("Prop_Wood", StringComparison.Ordinal)
                    || n.StartsWith("Door_Enamel", StringComparison.Ordinal) || n.StartsWith("Painted_Metal", StringComparison.Ordinal)) { if (faceMat == null) faceMat = m; }
                else if (n.Contains("Stainless") || n.Contains("Aluminium")) { if (plateMat == null) plateMat = m; }
                else if (n.Contains("Brass") || n.Contains("Chrome")) { if (hardwareMat == null) hardwareMat = m; }
            }
        if (faceMat == null) faceMat = FrontRoomsSurfaces.TryGet(Steel ? "Door_Enamel" : "Door_Veneer");
        // the Ward leaf is the wood leaf with stainless plates; a steel leaf's kick plates are its own (body) parts
        Ward = !Steel && plateMat != null;
    }

    static Material StageMaterial(Material face, int stage)
    {
        if (face == null) return null;
        var m = FrontRoomsSurfaces.TryGet(face.name + "_Damage_" + CasName[Mathf.Clamp(stage, 0, 3)]);
        return m != null ? m : face;
    }

    void ApplySurfaces(GameObject go, int stage)
    {
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
        {
            var mats = r.sharedMaterials;
            var changed = false;
            for (var i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                var n = mats[i].name;
                Material want = null;
                if (n == "Door_Veneer" || n == "Door_Enamel") want = stage >= 0 ? StageMaterial(faceMat, stage) : faceMat;
                else if ((n == "Prop_Brass" || n == "Prop_Chrome") && hardwareMat != null) want = hardwareMat;
                else if (n == "Prop_Aluminium" && plateMat != null) want = plateMat;
                if (want != null && want != mats[i]) { mats[i] = want; changed = true; }
            }
            if (changed) r.sharedMaterials = mats;
        }
    }

    static int CasStage(string part)
    {
        for (var k = 0; k < CasName.Length; k++)
            if (part.StartsWith("cas_" + CasName[k] + "_", StringComparison.Ordinal) || part.StartsWith("armor_" + CasName[k] + "_", StringComparison.Ordinal)) return k;
        return -1;
    }

    void Swap()
    {
        if (Swapped) return;
        EnsurePoseNode();
        kitRoot = new GameObject("Break kit").transform;
        kitRoot.SetParent(breakPose, false);
        kitRoot.localScale = new Vector3(BurstSign, 1f, 1f);
        var baseGo = FrontRoomsKitLibrary.Spawn(fam.name, kitRoot, Vector3.zero, Quaternion.identity, null, false, "Break base", false);
        var motifGo = FrontRoomsKitLibrary.Spawn(motif.model, kitRoot, Vector3.zero, Quaternion.identity, null, false, "Break motif " + Motif, false);
        if (baseGo == null || motifGo == null) { LastNote = "kit missing"; return; }
        parts.Clear();
        foreach (Transform c in baseGo.transform) parts[c.name] = c.gameObject;
        foreach (Transform c in motifGo.transform) parts[c.name] = c.gameObject;
        // shadow parity with the intact leaf: the same caster mode; its slab-sized shadow proxy stays on until D1 (the
        // first stage with through-gaps), so the swap frame changes only the window
        var intactR = intactLeaf != null ? intactLeaf.GetComponentInChildren<MeshRenderer>(true) : null;
        var castMode = intactR != null ? intactR.shadowCastingMode : ShadowCastingMode.On;
        var receive = intactR == null || intactR.receiveShadows;
        IntactShadowNote = intactR != null ? "intact leaf " + intactR.shadowCastingMode + " receive " + intactR.receiveShadows + ", proxy " + (leafShadow != null) : "no intact renderer";
        foreach (var kv in parts)
        {
            var r = kv.Value.GetComponent<MeshRenderer>();
            if (r != null) { r.shadowCastingMode = castMode; r.receiveShadows = receive; }
            ApplySurfaces(kv.Value, CasStage(kv.Key));
            kv.Value.SetActive(false);
        }
        // the leak cards: the far room's lamp colour, unlit HDR (the bloom carries it)
        leakMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Door break leak card" };
        foreach (var kv in parts)
            if (kv.Key.StartsWith("leak_", StringComparison.Ordinal))
            {
                var r = kv.Value.GetComponent<MeshRenderer>();
                if (r == null) continue;
                r.sharedMaterial = leakMat;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        BuildLods();
        // Door vision: the body carries the leaf's lite (door_break_lite_bases.py), so the window is there through every stage
        // and keeps its LIVE / DARK state; the wired glass never breaks (A42).
        var lite = intactLeaf != null ? intactLeaf.GetComponent<FrontRoomsDoorLite>() : null;
        if (lite != null) lite.AdoptBreakBody(baseGo);
        if (intactLeaf != null) intactLeaf.SetActive(false);
        Swapped = true;
    }

    bool LodAllowed(string k) => !(Phone && k.EndsWith("_L0", StringComparison.Ordinal));

    void BuildLods()
    {
        var intactGroup = intactLeaf != null ? intactLeaf.GetComponent<LODGroup>() : null;
        lodGroup = kitRoot.gameObject.AddComponent<LODGroup>();
        var l0 = new List<Renderer>();
        var l1 = new List<Renderer>();
        var l2 = new List<Renderer>();
        foreach (var kv in parts)
        {
            var r = kv.Value.GetComponent<MeshRenderer>();
            if (r == null) continue;
            var k = kv.Key;
            if (k.EndsWith("_L0", StringComparison.Ordinal)) { if (!Phone) l0.Add(r); }
            else if (k.EndsWith("_L1", StringComparison.Ordinal)) { l1.Add(r); if (Phone) l0.Add(r); }
            else if (k.EndsWith("_L2", StringComparison.Ordinal)) l2.Add(r);
            else { l0.Add(r); l1.Add(r); }      // the leak cards (near only)
        }
        float h0 = .25f, h1 = .08f, cull = .02f;
        if (intactGroup != null)
        {
            var lods = intactGroup.GetLODs();
            if (lods.Length > 0) h0 = lods[0].screenRelativeTransitionHeight;
            if (lods.Length > 1) h1 = lods[1].screenRelativeTransitionHeight;
            if (lods.Length > 0) cull = lods[lods.Length - 1].screenRelativeTransitionHeight;
            lodGroup.localReferencePoint = intactGroup.localReferencePoint;
            lodGroup.size = intactGroup.size;
        }
        else lodGroup.RecalculateBounds();
        if (h1 <= cull) h1 = Mathf.Max(cull * 1.5f, h0 * .33f);
        lodGroup.SetLODs(new[] { new LOD(h0, l0.ToArray()), new LOD(h1, l1.ToArray()), new LOD(cull, l2.ToArray()) });
        lodGroup.fadeMode = LODFadeMode.None;
    }

    void ShowCassette(int cas)
    {
        var st = CasName[Mathf.Clamp(cas, 0, 3)];
        foreach (var kv in parts)
        {
            var k = kv.Key;
            bool on;
            if (k.StartsWith("body_", StringComparison.Ordinal)) on = true;
            else if (k.StartsWith("cas_", StringComparison.Ordinal)) on = k.StartsWith("cas_" + st + "_", StringComparison.Ordinal);
            else if (k.StartsWith("leak_", StringComparison.Ordinal)) on = k == "leak_" + st;
            else if (k == "armor_win_L2" || k.StartsWith("armor_body_", StringComparison.Ordinal)) on = Ward;
            else if (k.StartsWith("armor_", StringComparison.Ordinal)) on = Ward && k.StartsWith("armor_" + st + "_", StringComparison.Ordinal);
            else on = false;
            if (on && !LodAllowed(k)) on = false;
            if (kv.Value.activeSelf != on) kv.Value.SetActive(on);
        }
    }

    void EnsureVfx()
    {
        if (vfx != null || Rig == null || Rig.Frame == null || motif == null) return;
        hardFloor = ProbeFloor(out _);
        var seed = FrontRoomsDoorBreakRecords.Hash(door.edge, 3) ^ (uint)(map != null ? map.Seed : 0);
        vfx = new FrontRoomsDoorBreakVfx(Rig.Frame, BurstSign, seed, Phone ? FrontRoomsDoorBreakVfx.Tier.Phone : FrontRoomsDoorBreakVfx.Tier.Desktop, hardFloor,
            faceMat, motif.ys, motif.zs, PaletteArray(), throwData, Steel, woodFrame);
        if (Steel)
        {
            vfx.SteelContact = new Vector3(-.022f, motif.dishY > 0f ? motif.dishY : 1.08f, motif.dishZ > 0f ? motif.dishZ : .745f);
            if (motif.peel != null && motif.peel.Length >= 2) vfx.SteelSeam = new Vector2(motif.peel[0], motif.peel[1]);
        }
    }

    // core, stile, the face (walnut veneer, or the Ward laminate), frame, steel, gypsum, settled dust, glue: 8 x 8 linear.
    // Steel (P3): kraft paper in the core slot, the enamel (with its primer entry) as the face. The frame slot is the frame's
    // own: the walnut casing, or the dark-bronze sleeve (P4).
    float[] PaletteArray()
    {
        var frameP = woodFrame ? "frame" : "steelframe";
        string[] order = Steel ? new[] { "kraft", "glue", "steel", frameP, "steel", "gypsum", "settled", "glue" }
            : new[] { "core", "stile", Ward ? "laminate" : "veneer", frameP, "steel", "gypsum", "settled", "glue" };
        var outp = new float[8 * 8 * 3];
        for (var k = 0; k < order.Length; k++)
        {
            float[] src = null;
            if (fam != null && fam.palettes != null) foreach (var p in fam.palettes) if (p != null && p.name == order[k]) src = p.linear;
            if (src == null && order[k] == "steelframe" && fam != null && fam.palettes != null) foreach (var p in fam.palettes) if (p != null && p.name == "frame") src = p.linear;
            for (var i = 0; i < 24; i++) outp[k * 24 + i] = src != null && i < src.Length ? src[i] : .3f;
        }
        return outp;
    }

    // The floor under the burst side: carpet (grit under 1 mm sinks into the loops) or a hard floor (it scatters).
    bool ProbeFloor(out Material floorMat)
    {
        floorMat = null;
        if (Rig == null || Rig.Frame == null) return false;
        var o = Rig.Frame.TransformPoint(new Vector3(BurstSign * .6f, .5f, .5f));
        if (!Physics.Raycast(o, Vector3.down, out var hit, 1.2f, ~0, QueryTriggerInteraction.Ignore)) return false;
        var r = hit.collider != null ? hit.collider.GetComponent<Renderer>() : null;
        floorMat = r != null ? r.sharedMaterial : null;
        var n = floorMat != null ? floorMat.name : "";
        return n.IndexOf("Carpet", StringComparison.OrdinalIgnoreCase) < 0 && (n.IndexOf("Floor", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("VCT", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("Tile", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    void WriteRecord()
    {
        if (map == null || door == null) return;
        FrontRoomsDoorBreakRecords.TryGet(map, door.edge, out var old);
        var flags = (ushort)(old.flags & (FrontRoomsDoorBreakRecord.Settled | FrontRoomsDoorBreakRecord.EnvSet));
        if (IsBroken) flags |= FrontRoomsDoorBreakRecord.Broken;
        if (Pose == FinalPose.B) flags |= FrontRoomsDoorBreakRecord.PoseB;
        if (hardFloor) flags |= FrontRoomsDoorBreakRecord.HardFloor;
        flags |= FrontRoomsDoorBreakRecord.Struck;
        flags |= (ushort)(Mathf.Clamp(BlowsTaken, 0, 15) << 8);
        FrontRoomsDoorBreakRecords.Put(map, new FrontRoomsDoorBreakRecord
        {
            edge = door.edge, stage = (byte)Mathf.Clamp(IsBroken ? 4 : Stage, 0, 4), burst = (sbyte)(BurstSign < 0f ? -1 : 1),
            motif = (byte)Motif, throwVariant = (byte)ThrowVariant, flags = flags, env = old.env,
        });
    }

    // =========================================================================================== the beats

    /// <summary>The stage blow i (0-based) of n plays: 4 - (n - 1 - i), floored at 0 (E 0, M 1, D1 2, D2 3, FINAL 4).</summary>
    public static int StageOf(int index, int count) => Mathf.Max(0, 4 - (count - 1 - index));

    /// <summary>The Relay's blow i of n from a world point (the map's TickBreak beat, via FrontRoomsDoorBreakDriver; tools
    /// call it on the same frame as JoltForBlow). Everything changes on this frame and nothing between blows.</summary>
    public void Blow(int index, int count, Vector3 from)
    {
        if (door == null || IsBroken || index < 0 || count <= 0) return;
        var mapped = StageOf(index, count);
        if (Stage < 0 && !Restored)
        {
            // The map has already turned the swing away from the Relay (SwingAwayFrom): the leaf bursts to -swing X.
            BurstSign = -door.swing;
        }
        fromPos = from;
        FarLampLevel = FarLamp(from);
        var prev = Stage;
        var stage = Mathf.Max(Stage, mapped);
        var newStage = stage > prev;
        EnsureVfx();
        if (stage >= 1 && !Swapped) { Swap(); BindFaceParts(); }
        if (newStage && stage >= 1 && Swapped)
        {
            ShowCassette(stage >= 4 ? 3 : stage - 1);
            if (stage >= 2 && leafShadow != null) leafShadow.SetActive(false);
            SwapFrame[stage] = Time.frameCount;
            if (stage == 4) Final();
        }
        else if (stage == 0 && SwapFrame[0] < 0) SwapFrame[0] = Time.frameCount;
        Stage = stage;
        if (newStage) DoorStageChanged?.Invoke(door, stage, Steel);
        vfx?.Blow(stage, BlowsTaken, newStage);
        BlowsTaken++;
        // the elastic rock (every blow), the hit-stop on the damaging stages from D1
        rockStage = Mathf.Clamp(mapped, 0, 4);
        rockMetres = RockMetres[rockStage];
        rockHoldFrames = HoldFrames[rockStage];
        rockHeldSoFar = 0;
        rockHolding = false;
        rockClock = 0f;
        leakPulse = 1f;
        if (stage >= 2 && breach == null) EnsureBreach();
        breachLevel = stage >= 3 ? .35f : stage >= 2 ? .18f : 0f;
        ApplyParts();
        UpdateFrame();
        WriteRecord();
        LastNote = "blow " + (index + 1) + "/" + count + " -> " + LabelOf(stage) + (newStage ? "" : " (kept)");
    }

    void Final()
    {
        finalClock = 0f;
        contactCursor = 0;
        SpawnDebris();
        for (var i = 0; i < debrisPieces.Count; i++)
        {
            PlayDebris(i, 0f);
            if (debrisKeys[i].name == "deb_strip" && latch != null) latch.SetParent(debrisPieces[i], true);
        }
        if (Phone) UpdateCombinedDebris();
    }

    void SpawnDebris()
    {
        if (debrisGo != null || throwData == null || throwData.pieces == null || Rig == null || Rig.Frame == null) return;
        if (debrisRoot == null)
        {
            debrisRoot = new GameObject("Break debris").transform;
            debrisRoot.SetParent(Rig.Frame, false);
            debrisRoot.localScale = new Vector3(BurstSign, 1f, 1f);
        }
        debrisGo = FrontRoomsKitLibrary.Spawn(motif.debris, debrisRoot, Vector3.zero, Quaternion.identity, null, false, "Break debris pieces", false);
        if (debrisGo == null) return;
        debrisPieces.Clear();
        debrisKeys.Clear();
        debrisRest.Clear();
        var byName = new Dictionary<string, Transform>();
        foreach (Transform c in debrisGo.transform) byName[c.name] = c;
        foreach (var p in throwData.pieces)
        {
            if (!byName.TryGetValue(p.name, out var t)) continue;
            debrisPieces.Add(t);
            debrisKeys.Add(p);
            debrisRest.Add(t.localPosition);
            var r = t.GetComponent<MeshRenderer>();
            if (r != null) { r.shadowCastingMode = ShadowCastingMode.On; ApplySurfaces(t.gameObject, -1); }
        }
        if (Phone) BuildCombinedDebris();
    }

    // =========================================================================================== P5: the phone's debris

    GameObject debrisCombinedGo;
    Mesh debrisCombined;
    readonly List<int> combinedPiece = new List<int>();       // debrisPieces index per kept piece
    readonly List<int> combinedStart = new List<int>();       // its first vertex in the combined mesh
    readonly List<int> combinedCount = new List<int>();       // its vertex count
    readonly List<Vector3> cSrcV = new List<Vector3>(), cSrcN = new List<Vector3>(), cV = new List<Vector3>(), cN = new List<Vector3>();
    readonly List<Vector4> cSrcT = new List<Vector4>(), cT = new List<Vector4>();
    /// <summary>Tools: the pieces the phone shows (all of them on desktop), the combined mesh's draws, its worst update.</summary>
    public int DebrisShown { get { var n = 0; foreach (var t in debrisPieces) if (t != null && t.gameObject.activeInHierarchy) n++; return n; } }
    public int DebrisCombinedDraws => debrisCombined != null && debrisCombinedGo != null && debrisCombinedGo.activeInHierarchy ? debrisCombined.subMeshCount : 0;
    public float CombinedWorstMs { get; private set; }

    // the piece's on-screen weight: the product of its two largest extents (the sidecar's rest size; the mesh bounds otherwise)
    float PieceArea(int i)
    {
        var name = debrisKeys[i].name;
        Vector3 size;
        DebrisRest rest = null;
        if (motif != null && motif.debrisRest != null) foreach (var d in motif.debrisRest) if (d != null && d.name == name) { rest = d; break; }
        if (rest != null && rest.size != null && rest.size.Length == 3) size = new Vector3(rest.size[0], rest.size[1], rest.size[2]);
        else
        {
            var mf = debrisPieces[i].GetComponent<MeshFilter>();
            size = mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds.size : Vector3.zero;
        }
        float a = Mathf.Abs(size.x), b = Mathf.Abs(size.y), c = Mathf.Abs(size.z);
        var lo = Mathf.Min(a, Mathf.Min(b, c));
        return a * b * c / Mathf.Max(lo, 1e-6f);
    }

    // Phones (50 3.9: <= 12 live, one combined dynamic mesh): the strip and the most visible pieces are kept, the rest never
    // show (they are switched off, so the merge and the env check skip them too); the kept pieces' renderers are off and one
    // mesh under the debris root draws them, one submesh per material, re-posed on the CPU while they fly.
    void BuildCombinedDebris()
    {
        if (debrisCombinedGo != null || debrisPieces.Count == 0) return;
        var order = new List<int>();
        for (var i = 0; i < debrisPieces.Count; i++) if (debrisPieces[i] != null) order.Add(i);
        var area = new float[debrisPieces.Count];
        foreach (var i in order) area[i] = PieceArea(i);
        order.Sort((x, y) =>
        {
            var sx = debrisKeys[x].name == "deb_strip" ? 1 : 0;
            var sy = debrisKeys[y].name == "deb_strip" ? 1 : 0;
            if (sx != sy) return sy - sx;
            var c = area[y].CompareTo(area[x]);
            return c != 0 ? c : string.CompareOrdinal(debrisKeys[x].name, debrisKeys[y].name);
        });
        combinedPiece.Clear(); combinedStart.Clear(); combinedCount.Clear();
        cSrcV.Clear(); cSrcN.Clear(); cSrcT.Clear();
        var uvs = new List<Vector2>();
        var mats = new List<Material>();
        var subs = new List<List<int>>();
        var pv = new List<Vector3>(); var pn = new List<Vector3>(); var pt = new List<Vector4>(); var pu = new List<Vector2>(); var pidx = new List<int>();
        for (var k = 0; k < order.Count; k++)
        {
            var i = order[k];
            var t = debrisPieces[i];
            if (k >= PhoneDebrisCap) { t.gameObject.SetActive(false); continue; }
            var mf = t.GetComponent<MeshFilter>();
            var mr = t.GetComponent<MeshRenderer>();
            var mesh = mf != null ? mf.sharedMesh : null;
            if (mesh == null || !mesh.isReadable || mr == null) continue;     // it keeps its own renderer
            pv.Clear(); pn.Clear(); pt.Clear(); pu.Clear();
            mesh.GetVertices(pv); mesh.GetNormals(pn); mesh.GetTangents(pt); mesh.GetUVs(0, pu);
            var b0 = cSrcV.Count;
            for (var v = 0; v < pv.Count; v++)
            {
                cSrcV.Add(pv[v]);
                cSrcN.Add(v < pn.Count ? pn[v] : Vector3.up);
                cSrcT.Add(v < pt.Count ? pt[v] : new Vector4(1f, 0f, 0f, 1f));
                uvs.Add(v < pu.Count ? pu[v] : Vector2.zero);
            }
            var pm = mr.sharedMaterials;
            for (var sub = 0; sub < mesh.subMeshCount; sub++)
            {
                var m = sub < pm.Length ? pm[sub] : null;
                var at = mats.IndexOf(m);
                if (at < 0) { at = mats.Count; mats.Add(m); subs.Add(new List<int>()); }
                pidx.Clear();
                mesh.GetIndices(pidx, sub);
                foreach (var x in pidx) subs[at].Add(b0 + x);
            }
            combinedPiece.Add(i); combinedStart.Add(b0); combinedCount.Add(pv.Count);
            mr.enabled = false;
        }
        if (combinedPiece.Count == 0) return;
        debrisCombined = new Mesh { name = "Door break debris (phone, combined)", indexFormat = cSrcV.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        debrisCombined.MarkDynamic();
        cV.Clear(); cN.Clear(); cT.Clear();
        cV.AddRange(cSrcV); cN.AddRange(cSrcN); cT.AddRange(cSrcT);
        debrisCombined.SetVertices(cV);
        debrisCombined.SetNormals(cN);
        debrisCombined.SetTangents(cT);
        debrisCombined.SetUVs(0, uvs);
        debrisCombined.subMeshCount = subs.Count;
        for (var k = 0; k < subs.Count; k++) debrisCombined.SetTriangles(subs[k], k, false);
        debrisCombinedGo = new GameObject("Break debris (phone, combined)");
        debrisCombinedGo.transform.SetParent(debrisRoot, false);
        debrisCombinedGo.AddComponent<MeshFilter>().sharedMesh = debrisCombined;
        var cr = debrisCombinedGo.AddComponent<MeshRenderer>();
        cr.sharedMaterials = mats.ToArray();
        cr.shadowCastingMode = ShadowCastingMode.On;
        cr.rayTracingMode = UnityEngine.Experimental.Rendering.RayTracingMode.Off;
    }

    // the kept pieces' vertices at their poses now (the combined mesh's frame is the debris root's: the mirror cancels, its
    // renderer applies it); a piece the env check dropped collapses to a point
    void UpdateCombinedDebris()
    {
        if (debrisCombined == null || debrisCombinedGo == null || !debrisCombinedGo.activeInHierarchy) return;
        var sw = Stopwatch.StartNew();
        var toLocal = debrisCombinedGo.transform.worldToLocalMatrix;
        for (var k = 0; k < combinedPiece.Count; k++)
        {
            var t = debrisPieces[combinedPiece[k]];
            int b0 = combinedStart[k], n = combinedCount[k];
            if (t == null || !t.gameObject.activeInHierarchy)
            {
                for (var v = b0; v < b0 + n; v++) { cV[v] = Vector3.zero; cN[v] = Vector3.up; }
                continue;
            }
            var m = toLocal * t.localToWorldMatrix;
            for (var v = b0; v < b0 + n; v++)
            {
                cV[v] = m.MultiplyPoint3x4(cSrcV[v]);
                cN[v] = m.MultiplyVector(cSrcN[v]);
                var st = cSrcT[v];
                var tw = m.MultiplyVector(new Vector3(st.x, st.y, st.z));
                cT[v] = new Vector4(tw.x, tw.y, tw.z, st.w);
            }
        }
        debrisCombined.SetVertices(cV);
        debrisCombined.SetNormals(cN);
        debrisCombined.SetTangents(cT);
        debrisCombined.RecalculateBounds();
        sw.Stop();
        CombinedWorstMs = Mathf.Max(CombinedWorstMs, (float)sw.Elapsed.TotalMilliseconds);
    }

    void DropCombinedDebris()
    {
        if (debrisCombinedGo != null) Destroy(debrisCombinedGo);
        if (debrisCombined != null) Destroy(debrisCombined);
        debrisCombinedGo = null;
        debrisCombined = null;
        combinedPiece.Clear(); combinedStart.Clear(); combinedCount.Clear();
        cSrcV.Clear(); cSrcN.Clear(); cSrcT.Clear(); cV.Clear(); cN.Clear(); cT.Clear();
    }

    /// <summary>The map's BreakDoor has thrown the leaf (DoorBrokenFrom).</summary>
    public void Broken(Vector3 from)
    {
        if (IsBroken) return;
        if (Stage < 0 && !Restored) BurstSign = -door.swing;
        EnsureVfx();
        if (!Swapped) { Swap(); BindFaceParts(); }
        if (Stage < 4 && Swapped)
        {
            ShowCassette(3);
            if (leafShadow != null) leafShadow.SetActive(false);
            Final();
        }
        IsBroken = true;
        Stage = 5;
        DoorStageChanged?.Invoke(door, 5, Steel);
        SwapFrame[5] = Time.frameCount;
        brokenClock = 0f;
        if (breach == null) EnsureBreach();
        vfx?.Break();
        ApplyParts();
        UpdateFrame();
        WriteRecord();
    }

    // =========================================================================================== restore (DressDoor)

    /// <summary>DressDoor: a door with a record (or broken by the map) comes back as it was. Never seen: a chunk is built
    /// 48+ m away.</summary>
    public static void RestoreOnDress(FrontRoomsDoorRig rig)
    {
        if (rig == null || rig.Door == null || rig.Map == null || !Application.isPlaying) return;
        var has = FrontRoomsDoorBreakRecords.TryGet(rig.Map, rig.Door.edge, out var rec);
        if (has && rec.Has(FrontRoomsDoorBreakRecord.Broken) && !rig.Door.broken)
        {
            // the map put the door back (tools, a reset): the record goes with it
            FrontRoomsDoorBreakRecords.Forget(rig.Map, rig.Door.edge);
            return;
        }
        if (!has && !rig.Door.broken) return;
        if (has && rec.stage == 0 && !rig.Door.broken) return;     // E leaves no mark
        var v = Ensure(rig);
        if (v == null) return;
        if (!has)
        {
            rec = new FrontRoomsDoorBreakRecord { edge = rig.Door.edge, stage = 4, burst = (sbyte)(rig.Door.swing > 0f ? -1 : 1), motif = (byte)v.Motif, throwVariant = (byte)v.ThrowVariant, flags = FrontRoomsDoorBreakRecord.Broken };
        }
        v.Restore(rec, rig.Door.broken);
    }

    void Restore(FrontRoomsDoorBreakRecord rec, bool broken)
    {
        var sw = Stopwatch.StartNew();
        Restored = true;
        BurstSign = rec.burst < 0 ? -1f : 1f;
        Pose = rec.Has(FrontRoomsDoorBreakRecord.PoseB) ? FinalPose.B : FinalPose.A;
        BlowsTaken = (rec.flags >> 8) & 15;
        FarLampLevel = FarLamp(door.position + (Rig.Frame != null ? Rig.Frame.TransformDirection(new Vector3(-BurstSign, 0f, 0f)) : Vector3.zero) * .8f);
        var stage = broken ? 4 : rec.stage;
        if (stage >= 1)
        {
            Swap();
            BindFaceParts();
            ShowCassette(stage >= 4 ? 3 : stage - 1);
            if (stage >= 2 && leafShadow != null) leafShadow.SetActive(false);
        }
        Stage = stage;
        if (broken)
        {
            IsBroken = true;
            Stage = 5;
            brokenClock = 99f;
            SpawnDebris();
            var end = 99f;
            for (var i = 0; i < debrisPieces.Count; i++)
            {
                PlayDebris(i, end);
                if (debrisKeys[i].name == "deb_strip" && latch != null) latch.SetParent(debrisPieces[i], false);
            }
            if (Phone) UpdateCombinedDebris();
            finalClock = -1f;
            // the floor pack: the dust skin and the settled crumbs and chips, then the merge (sliced) and the env check
            var pack = FrontRoomsDoorBreakRecords.Pack(map, door.edge);
            EnsureVfx();
            if (pack != null && vfx != null)
            {
                if (pack.skin != null) vfx.RestoreSkin(pack.skin);
                vfx.RestoreSolids(pack.solids);
            }
            // the env check first (a few frames: the chunk's props are built), then the merge of what is left
            restoreEnvSet = rec.Has(FrontRoomsDoorBreakRecord.EnvSet);
            restoreEnv = rec.env;
            envCheckIn = restoreEnvSet ? 3 : 0;
            mergeAfterEnv = true;
            RestoreNote = "broken, motif " + Motif + ", throw " + ThrowVariant + ", pack " + (pack != null ? (pack.solids.Count + " solids, skin " + (pack.skin != null)) : "none");
        }
        else RestoreNote = "stage " + LabelOf(stage) + " (kept), motif " + Motif;
        ApplyParts();
        UpdateFrame();
        sw.Stop();
        RestoreMs = (float)sw.Elapsed.TotalMilliseconds;
        LastNote = "restored: " + RestoreNote;
    }
    ushort restoreEnv;
    bool restoreEnvSet, mergeAfterEnv;
    public float RestoreMs { get; private set; }

    /// <summary>Tools: back to the intact leaf (destroys the kit, the debris and the VFX; the rig's parts go home).</summary>
    public void ResetVisual()
    {
        if (latch != null && latchParent != null) { latch.SetParent(latchParent, false); latch.localPosition = latchRestP; latch.localRotation = latchRestQ; }
        for (var i = 0; i < 2; i++)
        {
            if (rose[i] != null) { rose[i].localPosition = roseRestP[i]; rose[i].localRotation = roseRestQ[i]; }
            if (lever[i] != null) { lever[i].localPosition = leverRestP[i]; lever[i].localRotation = leverRestQ[i]; }
        }
        if (kitRoot != null) Destroy(kitRoot.gameObject);
        DropCombinedDebris();
        if (debrisRoot != null) Destroy(debrisRoot.gameObject);
        ResetFrame();
        if (breach != null) Destroy(breach.gameObject);
        if (leakMat != null) Destroy(leakMat);
        vfx?.Dispose();
        vfx = null;
        kitRoot = debrisRoot = floorGo = null;
        debrisGo = null;
        breach = null;
        parts.Clear();
        debrisPieces.Clear();
        debrisKeys.Clear();
        debrisRest.Clear();
        if (intactLeaf != null) intactLeaf.SetActive(true);
        if (intactLeaf != null) { var lite = intactLeaf.GetComponent<FrontRoomsDoorLite>(); if (lite != null) lite.AdoptBreakBody(null); }
        if (leafShadow != null) leafShadow.SetActive(true);
        if (breakPose != null) breakPose.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        Swapped = IsBroken = Restored = false;
        Stage = -1;
        BlowsTaken = 0;
        MergeState = 0;
        for (var i = 0; i < SwapFrame.Length; i++) { SwapFrame[i] = -1; HeldFrames[i] = 0; }
        rockClock = brokenClock = finalClock = -1f;
        leakPulse = breachLevel = 0f;
        if (map != null && door != null) FrontRoomsDoorBreakRecords.Forget(map, door.edge);
        LastNote = "reset";
    }

    /// <summary>Tools: the final pose now.</summary>
    public void SetPose(FinalPose pose) { Pose = pose; WriteRecord(); }

    // =========================================================================================== per frame

    void LateUpdate()
    {
        var dt = Time.deltaTime;
        if (breakPose != null)
        {
            // the rock: out in RockOut, the hit-stop (whole frames, counted), back over RockBack
            var rock = 0f;
            if (rockClock >= 0f)
            {
                if (rockHolding)
                {
                    rockHeldSoFar++;
                    HeldFrames[rockStage] = Mathf.Max(HeldFrames[rockStage], rockHeldSoFar);
                    if (rockHeldSoFar >= rockHoldFrames) rockHolding = false;
                    rock = 1f;
                }
                else
                {
                    var before = rockClock;
                    rockClock += dt;
                    if (before < RockOut && rockClock >= RockOut && rockHoldFrames > 0) { rockHolding = true; rockClock = RockOut; rock = 1f; }
                    else if (rockClock < RockOut) rock = rockClock / RockOut;
                    else if (rockClock < RockOut + RockBack) { var u = (rockClock - RockOut) / RockBack; rock = (1f - u) * (1f - u); }
                    else { rockClock = -1f; rock = 0f; }
                }
            }
            var rockDeg = BurstSign * Mathf.Asin(Mathf.Clamp(rockMetres * rock / Mathf.Max(.5f, FrontRoomsDoorRig.LatchEndZ - pivotZ), 0f, 1f)) * Mathf.Rad2Deg;
            var pivot = new Vector3(0f, 0f, pivotZ);
            var rockQ = Quaternion.AngleAxis(rockDeg, Vector3.up);
            var pos = pivot - rockQ * pivot;
            var rot = rockQ;
            if (IsBroken)
            {
                brokenClock += dt;
                // the pose eases in with the map's bounce (ThrowSeconds then 0.18 s)
                var u = Mathf.Clamp01((brokenClock - FrontRoomsShotTimings.DoorBreak.ThrowSeconds) / FrontRoomsMapWorld.DoorBreakBounceSeconds);
                u = 1f - (1f - u) * (1f - u);
                PoseOffset(u, out var pp, out var pq);
                // cancel the map's butt-hinge crooked tilt the rig copies (the rig's follow), keep its pivot correction
                // (map ask M3 makes the collider agree with pose (a))
                var rigT = transform;
                var aHinge = door.hinge != null ? Quaternion.Inverse(door.closed) * door.hinge.localRotation : Quaternion.identity;
                var p0 = Quaternion.Inverse(aHinge) * pivot - pivot;
                var invRot = Quaternion.Inverse(rigT.localRotation);
                var cancelP = invRot * (p0 - rigT.localPosition);
                var cancelQ = invRot;
                pos = cancelP + cancelQ * (pp + pq * pos);
                rot = cancelQ * pq * rot;
            }
            breakPose.SetLocalPositionAndRotation(pos, rot);
        }
        // leak cards: max(stage base, the blow pulse decaying over 0.12 s), scaled by the far lamp
        leakPulse = Mathf.Max(0f, leakPulse - dt / LeakPulse);
        var s = Mathf.Clamp(Stage, 0, 4);
        var leakK = Mathf.Max(LeakBase[s], leakPulse) * FarLampLevel;
        if (leakMat != null) leakMat.SetColor("_BaseColor", LampColour * (leakK * LeakHdr));
        // the breach light: low from D1, a flare at the break, then a 1 s fade (desktop only; one at a time)
        if (breach != null)
        {
            var level = breachLevel;
            if (IsBroken)
            {
                var b = brokenClock;
                level = b < .05f ? Mathf.Lerp(breachLevel, 1f, b / .05f) : b < BreachFlare ? 1f : Mathf.Max(0f, 1f - (b - BreachFlare) / BreachFade);
            }
            breach.intensity = level * 6f * Mathf.Max(.25f, FarLampLevel);
            breach.enabled = level > .001f;
        }
        // debris playback (the baked keys, interpolated, relative to this motif's rests)
        if (finalClock >= 0f)
        {
            finalClock += dt;
            for (var i = 0; i < debrisPieces.Count; i++) PlayDebris(i, finalClock);
            if (DebrisContact != null) FireContacts();
            var settle = throwData != null && throwData.settle > 0f ? throwData.settle : 1.6f;
            if (Phone && finalClock <= Mathf.Max(settle, throwData != null && throwData.hz > 0 && throwData.frames > 1 ? (throwData.frames - 1f) / throwData.hz : 0f) + Time.deltaTime) UpdateCombinedDebris();
            if (MergeState == 0 && finalClock > settle + .5f && (vfx == null || vfx.AllSolidsResting || finalClock > settle + 4f)) { MergeState = 1; mergeCursor = 0; }
        }
        if (vfx != null)
        {
            UpdateShaft(leakK);
            vfx.Tick(dt);
        }
        if (MergeState == 1) MergeSlice();
        if (envCheckIn >= 0 && --envCheckIn < 0)
        {
            if (restoreEnvSet) CheckNeighbourhood();
            if (mergeAfterEnv) { mergeAfterEnv = false; MergeState = 1; mergeCursor = 0; }
        }
    }

    // the slot / notch shaft (54 4.6): the opening of the current stage, the far lamp behind it
    void UpdateShaft(float leakK)
    {
        if (vfx == null) return;
        var on = kitRoot != null && !IsBroken && Stage >= 1 && leakK > .001f;
        if (!on) { vfx.SetShaft(false, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Color.black); return; }
        float yc, hu, zc, hv;
        if (Stage >= 4 && !Steel) { yc = 1.05f; hu = .33f; zc = .95f; hv = .045f; }
        else
        {
            var st = Stage == 1 ? 0 : Stage == 2 ? 1 : Stage == 3 ? 2 : 3;
            float y0 = .85f, y1 = 1.15f, gap = .0012f;
            if (motif != null && motif.stageInfo != null && st < motif.stageInfo.Length && motif.stageInfo[st].splitY != null && motif.stageInfo[st].splitY.Length == 2)
            { y0 = motif.stageInfo[st].splitY[0]; y1 = motif.stageInfo[st].splitY[1]; gap = Mathf.Max(.0006f, motif.stageInfo[st].gapAtBore); }
            yc = (y0 + y1) * .5f; hu = (y1 - y0) * .5f; zc = MotifZAt(Steel ? yc : 1.0f); hv = gap * .5f;
        }
        var c = kitRoot.TransformPoint(new Vector3(.022f, yc, zc));
        var u = kitRoot.TransformVector(new Vector3(0f, hu, 0f));
        var v = kitRoot.TransformVector(new Vector3(0f, 0f, hv));
        var n = kitRoot.TransformDirection(Vector3.right).normalized;
        var lamp = Rig.Frame.TransformPoint(new Vector3(-BurstSign * 1.5f, 2.6f, .5f));
        vfx.SetShaft(true, c, u, v, n, lamp, LampColour * (leakK * ShaftGain));
    }

    // S3: the bake's contact list (sorted by time) as the throw's clock passes it
    int contactCursor;
    void FireContacts()
    {
        var c = throwData != null ? throwData.contacts : null;
        if (c == null) return;
        while (contactCursor < c.Length && c[contactCursor].t <= finalClock)
        {
            var k = c[contactCursor++];
            for (var i = 0; i < debrisKeys.Count; i++)
            {
                if (debrisKeys[i].name != k.piece) continue;
                var t = debrisPieces[i];
                if (t != null && t.gameObject.activeInHierarchy) DebrisContact?.Invoke(t.position, k.speed, k.surface, k.piece);
                break;
            }
        }
    }

    float MotifZAt(float y)
    {
        if (motif == null || motif.ys == null || motif.ys.Length < 2) return .919f;
        var k = (y - motif.ys[0]) / (motif.ys[1] - motif.ys[0]);
        var i = Mathf.Clamp(Mathf.FloorToInt(k), 0, motif.ys.Length - 2);
        return Mathf.Lerp(motif.zs[i], motif.zs[i + 1], Mathf.Clamp01(k - i));
    }

    void PoseOffset(float u, out Vector3 p, out Quaternion q)
    {
        if (Pose == FinalPose.A)
        {
            // pose (a): the top pivot tore; lean out of plane about the bottom pivot (at the 80 deg rest the top moves ~110 mm
            // along the wall, off the hinge jamb into the opening, for either swing), and sag 0.6 deg in plane so the latch
            // end's foot rests on the carpet
            var b = new Vector3(0f, .015f, pivotZ);
            q = Quaternion.AngleAxis(BurstSign * PoseALeanDeg * u, Vector3.forward) * Quaternion.AngleAxis(PoseASagDeg * u, Vector3.right);
            p = b - q * b;
        }
        else
        {
            // pose (b): the bottom arm tore; the leaf hangs from the top pivot, latch corner down, twisted out of plane
            var t = new Vector3(0f, 2.095f, pivotZ);
            q = Quaternion.AngleAxis(BurstSign * PoseBTwistDeg * u, Vector3.forward) * Quaternion.AngleAxis(PoseBTurnDeg * u, Vector3.right);
            p = t - q * t;
        }
    }

    void PlayDebris(int i, float time)
    {
        var t = debrisPieces[i];
        var p = debrisKeys[i];
        if (t == null || p.keys == null || p.keys.Length < 7 || throwData == null) return;
        var n = p.keys.Length / 7;
        var f = Mathf.Clamp(time * throwData.hz, 0f, n - 1);
        var a0 = Mathf.Min(n - 2, Mathf.FloorToInt(f));
        if (a0 < 0) a0 = 0;
        var u = n > 1 ? f - a0 : 0f;
        var a = a0 * 7;
        var b = Mathf.Min(n - 1, a0 + 1) * 7;
        var k = p.keys;
        var pos = Vector3.Lerp(new Vector3(k[a], k[a + 1], k[a + 2]), new Vector3(k[b], k[b + 1], k[b + 2]), u);
        var rot = Quaternion.Slerp(new Quaternion(k[a + 3], k[a + 4], k[a + 5], k[a + 6]), new Quaternion(k[b + 3], k[b + 4], k[b + 5], k[b + 6]), u);
        // relative throws (P1): this motif's rest + the key's travel from the bake's rest
        if (throwData.relative && p.rest != null && p.rest.Length == 3) pos = debrisRest[i] + (pos - new Vector3(p.rest[0], p.rest[1], p.rest[2]));
        t.SetLocalPositionAndRotation(pos, rot);
    }

    // The lockset (the rig's parts, in the Break pose's space): the rose tips and shifts, the lever droops, the latchbolt
    // rides on the strip (50_rethink 3.3).
    void ApplyParts()
    {
        if (breakPose == null) return;
        ApplyStrike();
        if (Steel) { ApplySteelParts(); return; }
        var s = Stage;
        float tip = 0f, shiftZ = 0f, droop = 0f, pushIn = 0f, droopPush = 0f, crookPush = 0f;
        if (s >= 1) tip = 3f;
        if (s >= 2) { droop = 6f; pushIn = .003f; }
        if (s >= 3) { tip = 8f; shiftZ = -.008f; droop = 12f; crookPush = 8f; }
        if (s >= 4) { droop = 32f; droopPush = 26f; }
        PosePart(0, tip, shiftZ, 0f, droop);
        PosePart(1, crookPush, 0f, pushIn, droopPush);
        if (latch != null && s < 4 && latch.parent == latchParent)
        {
            var dx = s == 3 ? StripDx("D2") : s == 2 ? StripDx("D1") : 0f;
            latch.localPosition = latchRestP + new Vector3(BurstSign * dx, 0f, 0f);
        }
    }

    void PosePart(int face, float tipDeg, float shiftZ, float pushIn, float droopDeg)
    {
        var r = rose[face];
        if (r == null) return;
        var c = roseRestP[face];
        var outward = face == 0 ? BurstSign : -BurstSign;
        var tipQ = Quaternion.AngleAxis(tipDeg * outward, Vector3.forward);
        var shift = new Vector3(-outward * pushIn, -.003f * (tipDeg > 5f ? 1f : 0f), shiftZ);
        r.localPosition = c + shift;
        r.localRotation = tipQ * roseRestQ[face];
        var l = lever[face];
        if (l == null) return;
        var droopQ = Quaternion.AngleAxis(-droopDeg, Vector3.right);
        var q = tipQ * droopQ;
        l.localPosition = c + shift + q * (leverRestP[face] - c);
        l.localRotation = q * leverRestQ[face];
    }

    // =========================================================================================== P3: steel parts

    // The steel cassettes' fields (interact_door_leaf_steel_da_break.Motif, the same numbers from the sidecar): the bow toward
    // the burst side, the push face's dish, the lock edge's crush. Canonical door root (+X = the burst face).
    static float S3(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
    static float Bump(float y, float a, float b, float pk) => y <= a || y >= b ? 0f : y <= pk ? S3((y - a) / Mathf.Max(1e-6f, pk - a)) : S3((b - y) / Mathf.Max(1e-6f, b - pk));
    float WinZ0 => fam != null && fam.window != null && fam.window.z != null && fam.window.z.Length > 0 ? fam.window.z[0] : .60f;
    float WinY0 => fam != null && fam.window != null && fam.window.y != null && fam.window.y.Length > 1 ? fam.window.y[0] : .50f;
    float WinY1 => fam != null && fam.window != null && fam.window.y != null && fam.window.y.Length > 1 ? fam.window.y[1] : 1.22f;

    StageInfo SteelStage()
    {
        if (motif == null || motif.stageInfo == null || Stage < 1) return null;
        var k = Mathf.Clamp(Stage, 1, 4) - 1;
        return k < motif.stageInfo.Length ? motif.stageInfo[k] : null;
    }

    float SteelBow(StageInfo si, float y, float z)
    {
        if (si == null || si.bow <= 0f || z < WinZ0) return 0f;
        float bz = motif.bowZ > 0f ? motif.bowZ : .85f, by = motif.bowY > 0f ? motif.bowY : .97f;
        var hz = z < bz ? S3((z - WinZ0) / (bz - WinZ0)) : 1f - .45f * S3((z - bz) / (.995f - bz));
        return si.bow * Bump(y, WinY0, WinY1, by) * hz;
    }

    float SteelDish(StageInfo si, float y, float z)
    {
        if (si == null || si.dish <= 0f || si.dishR <= 0f) return 0f;
        var dz = (z - motif.dishZ) / si.dishR;
        var dy = (y - motif.dishY) / (1.15f * si.dishR);
        var r2 = dz * dz + dy * dy;
        return r2 < 1f ? si.dish * (1f - r2) * (1f - r2) : 0f;
    }

    float SteelCrush(StageInfo si, float y) => si == null ? 0f : si.crush * Bump(y, .72f, 1.20f, motif.crushPk > 0f ? motif.crushPk : .97f);

    static readonly string[] SteelFaceParts = { "Lock escutcheon", "Lock cylinder", "Lock plug", "Lock knob", "Door plate", "Door sign" };
    readonly List<Transform> steelParts = new List<Transform>();
    readonly List<Vector3> steelRest = new List<Vector3>();
    Transform deadbolt;
    Vector3 deadboltRestP;
    Quaternion deadboltRestQ;

    void BindSteelParts()
    {
        steelParts.Clear();
        steelRest.Clear();
        foreach (var n in SteelFaceParts)
            foreach (var f in new[] { " s", " p" })
            {
                var t = breakPose.Find(n + f);
                if (t == null) continue;
                steelParts.Add(t);
                steelRest.Add(t.localPosition);
            }
        deadbolt = breakPose.Find("Lock deadbolt");
        if (deadbolt != null) { deadboltRestP = deadbolt.localPosition; deadboltRestQ = deadbolt.localRotation; }
    }

    // The face hardware rides the skin (the bow on both faces, the dish on the push face); the latch and the deadbolt ride
    // the crushed lock edge and, at FINAL, bend where they tore through the strike (they stay in the leaf).
    void ApplySteelParts()
    {
        var si = SteelStage();
        for (var i = 0; i < steelParts.Count; i++)
        {
            var t = steelParts[i];
            if (t == null) continue;
            var p = steelRest[i];
            var cx = p.x * BurstSign;                         // canonical X: > 0 on the burst face
            var d = SteelBow(si, p.y, p.z) + (cx < 0f ? SteelDish(si, p.y, p.z) : 0f);
            t.localPosition = p + new Vector3(BurstSign * d, 0f, 0f);
        }
        var torn = Stage >= 4;
        if (latch != null && latch.parent == latchParent)
        {
            latch.localPosition = latchRestP + new Vector3(BurstSign * SteelBow(si, latchRestP.y, .995f), 0f, -SteelCrush(si, latchRestP.y));
            latch.localRotation = torn ? Quaternion.AngleAxis(22f, Vector3.right) * Quaternion.AngleAxis(BurstSign * 10f, Vector3.up) * latchRestQ : latchRestQ;
        }
        if (deadbolt != null)
        {
            deadbolt.localPosition = deadboltRestP + new Vector3(BurstSign * SteelBow(si, deadboltRestP.y, .995f), 0f, -SteelCrush(si, deadboltRestP.y));
            deadbolt.localRotation = torn ? Quaternion.AngleAxis(-14f, Vector3.right) * Quaternion.AngleAxis(BurstSign * 8f, Vector3.up) * deadboltRestQ : deadboltRestQ;
        }
    }

    // =========================================================================================== P4: the frame's strike zone

    GameObject frameGo;
    Transform frameInsert;
    readonly Dictionary<string, GameObject> frameParts = new Dictionary<string, GameObject>();
    readonly List<Renderer> frameIntact = new List<Renderer>();
    LODGroup frameLods;
    Transform strike;
    Vector3 strikeRestP;
    Quaternion strikeRestQ;
    bool strikeBound;

    // the strike's lips are dragged toward the burst side by the latch: a small turn and shift at D1 / D2 (the map's
    // DetachStrike drops it from wherever it is at the break)
    void ApplyStrike()
    {
        if (Rig == null || Rig.Frame == null) return;
        if (!strikeBound)
        {
            strike = Rig.Frame.Find("Lock strike");
            if (strike != null && !Rig.StrikeDetached) { strikeRestP = strike.localPosition; strikeRestQ = strike.localRotation; strikeBound = true; }
            else return;
        }
        if (strike == null || Rig.StrikeDetached) return;
        var s = Mathf.Clamp(Stage, -1, 4);
        float deg = s >= 3 ? 6f : s >= 2 ? 2.5f : 0f, dx = s >= 3 ? .003f : s >= 2 ? .0012f : 0f;
        strike.localPosition = strikeRestP + new Vector3(BurstSign * dx, 0f, 0f);
        strike.localRotation = Quaternion.AngleAxis(-BurstSign * deg, Vector3.up) * strikeRestQ;
    }

    /// <summary>The frame's insert for the shown stage: the latest of its stages whose "from" the door has reached (the
    /// swap happens on that blow's frame, under the blow).</summary>
    void UpdateFrame()
    {
        if (frameFam == null || Rig == null || Rig.Frame == null) return;
        var s = Mathf.Min(Stage, 4);
        string want = null;
        foreach (var st in frameFam.stages) if (st != null && s >= st.from) want = st.name;
        if (want == null || want == FrameStageShown) return;
        if (frameGo == null && !SpawnFrame()) return;
        foreach (var kv in frameParts)
        {
            var k = kv.Key;
            bool on;
            if (k.StartsWith("body_", StringComparison.Ordinal)) on = true;
            else if (k.StartsWith("ins_", StringComparison.Ordinal)) on = k.StartsWith("ins_" + want + "_", StringComparison.Ordinal);
            else on = false;
            if (on && !LodAllowed(k)) on = false;
            if (kv.Value.activeSelf != on) kv.Value.SetActive(on);
        }
        FrameStageShown = want;
        FrameSwapFrame = Time.frameCount;
    }

    bool SpawnFrame()
    {
        var root = new GameObject("Break frame");
        root.transform.SetParent(Rig.Frame, false);
        var go = FrontRoomsKitLibrary.Spawn(frameFam.name, root.transform, Vector3.zero, Quaternion.identity, null, false, "Break frame kit", false);
        if (go == null) { Destroy(root); frameFam = null; return false; }
        frameGo = root;
        // the insert (only) mirrors with the burst; the frames are symmetric in X and the body keeps the intact's texture
        frameInsert = new GameObject("Break frame insert").transform;
        frameInsert.SetParent(go.transform, false);
        frameInsert.localScale = new Vector3(BurstSign, 1f, 1f);
        frameParts.Clear();
        var kids = new List<Transform>();
        foreach (Transform c in go.transform) if (c != frameInsert) kids.Add(c);
        foreach (var c in kids)
        {
            frameParts[c.name] = c.gameObject;
            if (c.name.StartsWith("ins_", StringComparison.Ordinal)) c.SetParent(frameInsert, false);
        }
        // the intact frame's own renderers (its LOD set), their shadow mode, then off
        frameIntact.Clear();
        var lg = Rig.Frame.GetComponent<LODGroup>();
        if (lg != null) foreach (var l in lg.GetLODs()) foreach (var r in l.renderers) if (r != null && !frameIntact.Contains(r)) frameIntact.Add(r);
        if (frameIntact.Count == 0)
            foreach (var r in Rig.Frame.GetComponentsInChildren<MeshRenderer>(true))
                if (r.transform == Rig.Frame || r.transform.parent == Rig.Frame)
                    if (!r.name.StartsWith("Lock", StringComparison.Ordinal) && !r.name.StartsWith("Break", StringComparison.Ordinal)) frameIntact.Add(r);
        var cast = frameIntact.Count > 0 ? frameIntact[0].shadowCastingMode : ShadowCastingMode.On;
        var recv = frameIntact.Count == 0 || frameIntact[0].receiveShadows;
        foreach (var kv in frameParts)
        {
            var r = kv.Value.GetComponent<MeshRenderer>();
            if (r == null) continue;
            r.shadowCastingMode = cast;
            r.receiveShadows = recv;
            FrameSurfaces(r);
            kv.Value.SetActive(false);
        }
        // LODs: the intact frame's switch heights
        frameLods = root.AddComponent<LODGroup>();
        var l0 = new List<Renderer>(); var l1 = new List<Renderer>(); var l2 = new List<Renderer>();
        foreach (var kv in frameParts)
        {
            var r = kv.Value.GetComponent<MeshRenderer>();
            if (r == null) continue;
            if (kv.Key.EndsWith("_L0", StringComparison.Ordinal)) { if (!Phone) l0.Add(r); }
            else if (kv.Key.EndsWith("_L1", StringComparison.Ordinal)) { l1.Add(r); if (Phone) l0.Add(r); }
            else if (kv.Key.EndsWith("_L2", StringComparison.Ordinal)) l2.Add(r);
        }
        float h0 = .25f, h1 = .08f, cull = .02f;
        if (lg != null)
        {
            var lods = lg.GetLODs();
            if (lods.Length > 0) h0 = lods[0].screenRelativeTransitionHeight;
            if (lods.Length > 1) h1 = lods[1].screenRelativeTransitionHeight;
            if (lods.Length > 0) cull = lods[lods.Length - 1].screenRelativeTransitionHeight;
            frameLods.localReferencePoint = lg.localReferencePoint;
            frameLods.size = lg.size;
        }
        if (h1 <= cull) h1 = Mathf.Max(cull * 1.5f, h0 * .33f);
        frameLods.SetLODs(new[] { new LOD(h0, l0.ToArray()), new LOD(h1, l1.ToArray()), new LOD(cull, l2.ToArray()) });
        if (lg == null) frameLods.RecalculateBounds();
        frameLods.fadeMode = LODFadeMode.None;
        foreach (var r in frameIntact) r.enabled = false;
        return true;
    }

    // the frame's slot swaps (as DressDoor's spawn does for the intact frame): the wood frame's walnut, satin chrome, the
    // aluminium variant's sleeve
    void FrameSurfaces(Renderer r)
    {
        if (frameFam.slotSwaps == null) return;
        var mats = r.sharedMaterials;
        var changed = false;
        for (var i = 0; i < mats.Length; i++)
        {
            if (mats[i] == null) continue;
            foreach (var sw in frameFam.slotSwaps)
            {
                if (sw == null || mats[i].name != sw.from) continue;
                if (sw.kit != "*" && sw.kit != frameKitName) continue;
                var m = FrontRoomsSurfaces.TryGet(sw.to);
                if (m != null) { mats[i] = m; changed = true; }
                break;
            }
        }
        if (changed) r.sharedMaterials = mats;
    }

    void ResetFrame()
    {
        foreach (var r in frameIntact) if (r != null) r.enabled = true;
        frameIntact.Clear();
        if (frameGo != null) Destroy(frameGo);
        frameGo = null;
        frameInsert = null;
        frameLods = null;
        frameParts.Clear();
        FrameStageShown = "";
        FrameSwapFrame = -1;
        if (strike != null && strikeBound && Rig != null && !Rig.StrikeDetached) { strike.localPosition = strikeRestP; strike.localRotation = strikeRestQ; }
        strikeBound = false;
    }

    /// <summary>Tools: triangles and draws of the visible frame break parts at a LOD level (0, 1 or 2).</summary>
    public void CountFrame(int level, out int tris, out int draws)
    {
        tris = draws = 0;
        var lvl = Phone && level == 0 ? 1 : level;
        foreach (var kv in frameParts)
        {
            if (!kv.Value.activeInHierarchy || !kv.Key.EndsWith("_L" + lvl, StringComparison.Ordinal)) continue;
            Count(kv.Value, ref tris, ref draws);
        }
    }

    void BindFaceParts()
    {
        if (Steel) BindSteelParts();
        string burst = BurstSign > 0f ? " s" : " p", push = BurstSign > 0f ? " p" : " s";
        rose[0] = breakPose.Find("Lock rose" + burst);
        rose[1] = breakPose.Find("Lock rose" + push);
        lever[0] = breakPose.Find("Lock lever" + burst);
        lever[1] = breakPose.Find("Lock lever" + push);
        for (var i = 0; i < 2; i++)
        {
            if (rose[i] != null) { roseRestP[i] = rose[i].localPosition; roseRestQ[i] = rose[i].localRotation; }
            if (lever[i] != null) { leverRestP[i] = lever[i].localPosition; leverRestQ[i] = lever[i].localRotation; }
        }
    }

    float FarLamp(Vector3 from)
    {
        if (map == null) return 1f;
        var lvl = map.LampLevel(map.CellOf(from));
        return lvl < 0f ? 0f : Mathf.Clamp01(lvl);
    }

    void EnsureBreach()
    {
        if (Rig == null || Rig.Frame == null || Phone) return;
        var go = new GameObject("Break breach light");
        go.transform.SetParent(Rig.Frame, false);
        go.transform.localPosition = new Vector3(-BurstSign * 1.2f, 1.25f, .75f);
        go.transform.localRotation = Quaternion.LookRotation(new Vector3(BurstSign * 1.2f, -.15f, .17f).normalized, Vector3.up);
        breach = go.AddComponent<Light>();
        breach.type = LightType.Spot;
        breach.spotAngle = 75f;
        breach.innerSpotAngle = 40f;
        breach.range = 5f;
        breach.color = LampColour;
        breach.shadows = LightShadows.Soft;
        breach.intensity = 0f;
        breach.enabled = false;
    }

    // =========================================================================================== the floor merge (sliced)

    int mergeCursor;
    readonly Dictionary<Material, int> bucketOf = new Dictionary<Material, int>();
    readonly List<Material> bucketMat = new List<Material>();
    readonly List<List<Vector3>> bv = new List<List<Vector3>>();
    readonly List<List<Vector3>> bn = new List<List<Vector3>>();
    readonly List<List<Vector4>> bt = new List<List<Vector4>>();
    readonly List<List<Vector2>> bu = new List<List<Vector2>>();
    readonly List<List<int>> bi = new List<List<int>>();
    readonly List<Vector3> sv = new List<Vector3>();
    readonly List<Vector3> sn = new List<Vector3>();
    readonly List<Vector4> st = new List<Vector4>();
    readonly List<Vector2> su = new List<Vector2>();
    readonly List<int> si = new List<int>();
    readonly List<FrontRoomsDoorBreakVfx.Solid> takenSolids = new List<FrontRoomsDoorBreakVfx.Solid>();
    readonly List<int> pieceIndexCount = new List<int>();     // per bucket: the indices that are debris pieces (the far LOD)
    int[] remap = new int[0];
    Mesh mergeFull;
    readonly List<Material> mergeUsed = new List<Material>();

    int Bucket(Material m)
    {
        if (m == null) m = faceMat;
        if (bucketOf.TryGetValue(m, out var k)) return k;
        k = bucketMat.Count;
        bucketOf[m] = k;
        bucketMat.Add(m);
        bv.Add(new List<Vector3>()); bn.Add(new List<Vector3>()); bt.Add(new List<Vector4>()); bu.Add(new List<Vector2>()); bi.Add(new List<int>());
        return k;
    }

    // One step per piece, inside a 1 ms budget per frame (GD3c: a whole-frame merge cost 21.9 ms): the debris pieces at rest,
    // then the resting crumbs and chips, then the mesh. The merged floor is one renderer (one draw per material).
    void MergeSlice()
    {
        var sw = Stopwatch.StartNew();
        MergeFrames++;
        if (mergeCursor == 0)
        {
            bucketOf.Clear(); bucketMat.Clear(); bv.Clear(); bn.Clear(); bt.Clear(); bu.Clear(); bi.Clear(); takenSolids.Clear(); pieceIndexCount.Clear();
            MergedPieces = MergedSolids = 0;
        }
        var toFloor = debrisRoot != null ? debrisRoot.worldToLocalMatrix : Matrix4x4.identity;
        while (mergeCursor < debrisPieces.Count)
        {
            var t = debrisPieces[mergeCursor++];
            if (t == null || !t.gameObject.activeInHierarchy) continue;
            var mf = t.GetComponent<MeshFilter>();
            var mr = t.GetComponent<MeshRenderer>();
            var mesh = mf != null ? mf.sharedMesh : null;
            if (mesh == null || !mesh.isReadable) continue;
            var m2f = toFloor * t.localToWorldMatrix;
            sv.Clear(); sn.Clear(); st.Clear(); su.Clear();
            mesh.GetVertices(sv); mesh.GetNormals(sn); mesh.GetTangents(st); mesh.GetUVs(0, su);
            var mats = mr != null ? mr.sharedMaterials : null;
            for (var sub = 0; sub < mesh.subMeshCount; sub++)
            {
                var k = Bucket(mats != null && sub < mats.Length ? mats[sub] : null);
                var vb = bv[k].Count;
                si.Clear();
                mesh.GetIndices(si, sub);
                // the vertices this submesh uses, re-indexed (a pooled map, -1 = not yet taken)
                if (remap.Length < sv.Count) remap = new int[Mathf.NextPowerOfTwo(sv.Count)];
                for (var r = 0; r < sv.Count; r++) remap[r] = -1;
                foreach (var idx in si)
                {
                    var ni = remap[idx];
                    if (ni < 0)
                    {
                        ni = bv[k].Count;
                        remap[idx] = ni;
                        bv[k].Add(m2f.MultiplyPoint3x4(sv[idx]));
                        bn[k].Add(idx < sn.Count ? m2f.MultiplyVector(sn[idx]).normalized : Vector3.up);
                        var tg = idx < st.Count ? st[idx] : new Vector4(1, 0, 0, 1);
                        var tw = m2f.MultiplyVector(new Vector3(tg.x, tg.y, tg.z)).normalized;
                        bt[k].Add(new Vector4(tw.x, tw.y, tw.z, tg.w));
                        bu[k].Add(idx < su.Count ? su[idx] : Vector2.zero);
                    }
                    bi[k].Add(ni);
                }
            }
            MergedPieces++;
            if (sw.Elapsed.TotalMilliseconds > SliceMs) { MergeWorstMs = Mathf.Max(MergeWorstMs, (float)sw.Elapsed.TotalMilliseconds); return; }
        }
        var nPieces = debrisPieces.Count;
        if (mergeCursor == nPieces)
        {
            // pieces done: what they hold per bucket is the far LOD
            pieceIndexCount.Clear();
            for (var k = 0; k < bucketMat.Count; k++) pieceIndexCount.Add(bi[k].Count);
            if (vfx != null) for (var m = 0; m < FrontRoomsDoorBreakVfx.MatCount; m++) Bucket(vfx.SolidMaterial(m));
            while (pieceIndexCount.Count < bucketMat.Count) pieceIndexCount.Add(0);
            mergeCursor = nPieces + 1;
        }
        if (mergeCursor == nPieces + 1)
        {
            // the resting crumbs and chips, 160 per slice (the VFX root and the debris root are both the door root,
            // mirrored alike: the same local frame)
            if (vfx != null && vfx.SolidsResting > 0)
            {
                var pv = new List<Vector3>[FrontRoomsDoorBreakVfx.MatCount];
                var pn = new List<Vector3>[FrontRoomsDoorBreakVfx.MatCount];
                var pt = new List<Vector4>[FrontRoomsDoorBreakVfx.MatCount];
                var pu = new List<Vector2>[FrontRoomsDoorBreakVfx.MatCount];
                var pi = new List<int>[FrontRoomsDoorBreakVfx.MatCount];
                for (var m = 0; m < FrontRoomsDoorBreakVfx.MatCount; m++) { pv[m] = new List<Vector3>(); pn[m] = new List<Vector3>(); pt[m] = new List<Vector4>(); pu[m] = new List<Vector2>(); pi[m] = new List<int>(); }
                MergedSolids += vfx.TakeRestingSolids(Matrix4x4.identity, pv, pn, pt, pu, pi, takenSolids, 160);
                for (var m = 0; m < FrontRoomsDoorBreakVfx.MatCount; m++)
                {
                    var k = Bucket(vfx.SolidMaterial(m));
                    var b0 = bv[k].Count;
                    bv[k].AddRange(pv[m]); bn[k].AddRange(pn[m]); bt[k].AddRange(pt[m]); bu[k].AddRange(pu[m]);
                    foreach (var idx in pi[m]) bi[k].Add(b0 + idx);
                }
                MergeWorstMs = Mathf.Max(MergeWorstMs, (float)sw.Elapsed.TotalMilliseconds);
                return;
            }
            mergeCursor = nPieces + 2;
            MergeWorstMs = Mathf.Max(MergeWorstMs, (float)sw.Elapsed.TotalMilliseconds);
            return;
        }
        if (mergeCursor == nPieces + 2)
        {
            // the near mesh in its own slice
            mergeUsed.Clear();
            mergeFull = BuildFloorMesh("Door break floor", false, mergeUsed);
            mergeCursor = nPieces + 3;
            MergeWorstMs = Mathf.Max(MergeWorstMs, (float)sw.Elapsed.TotalMilliseconds);
            return;
        }
        // the far mesh and the floor object: LOD0 = the pieces + the crumbs and chips (up to the leaf's first switch,
        // 4 m), LOD1 = the pieces only (crumbs of 2-6 mm are under a pixel beyond it), culled with the leaf
        var usedMats = mergeUsed;
        var full = mergeFull;
        var far = BuildFloorMesh("Door break floor (far)", true, null);
        MergedTris = 0;
        for (var k = 0; k < full.subMeshCount; k++) MergedTris += (int)(full.GetIndexCount(k) / 3);
        MergedDraws = full.subMeshCount;
        if (debrisRoot != null)
        {
            var go = new GameObject("Break floor (merged)");
            go.transform.SetParent(debrisRoot, false);
            var near = new GameObject("near");
            near.transform.SetParent(go.transform, false);
            near.AddComponent<MeshFilter>().sharedMesh = full;
            var r0 = near.AddComponent<MeshRenderer>();
            r0.sharedMaterials = usedMats.ToArray();
            r0.shadowCastingMode = ShadowCastingMode.On;
            var farGo = new GameObject("far");
            farGo.transform.SetParent(go.transform, false);
            farGo.AddComponent<MeshFilter>().sharedMesh = far;
            var r1 = farGo.AddComponent<MeshRenderer>();
            r1.sharedMaterials = usedMats.ToArray();
            r1.shadowCastingMode = ShadowCastingMode.On;
            var lg = go.AddComponent<LODGroup>();
            float h0 = .25f, cull = .02f;
            if (lodGroup != null) { var lods = lodGroup.GetLODs(); if (lods.Length > 0) h0 = lods[0].screenRelativeTransitionHeight; if (lods.Length > 2) cull = lods[2].screenRelativeTransitionHeight; }
            lg.SetLODs(new[] { new LOD(h0, new Renderer[] { r0 }), new LOD(cull, new Renderer[] { r1 }) });
            lg.RecalculateBounds();
            lg.fadeMode = LODFadeMode.None;
            floorGo = go.transform;
        }
        // the pieces go (the latch rides on the strip: it stays, parented to the floor)
        foreach (var t in debrisPieces)
        {
            if (t == null) continue;
            if (latch != null && latch.parent == t) latch.SetParent(floorGo != null ? floorGo : debrisRoot, true);
            t.gameObject.SetActive(false);
        }
        if (Phone) DropCombinedDebris();
        finalClock = -1f;
        MergeState = 2;
        MergeWorstMs = Mathf.Max(MergeWorstMs, (float)sw.Elapsed.TotalMilliseconds);
        // stamp the record (settled, the neighbourhood) and keep the floor pack
        if (map != null && door != null)
        {
            FrontRoomsDoorBreakRecords.TryGet(map, door.edge, out var rec);
            if (rec.edge == door.edge || rec.edge == 0)
            {
                WriteRecord();
                FrontRoomsDoorBreakRecords.TryGet(map, door.edge, out rec);
                rec.flags |= FrontRoomsDoorBreakRecord.Settled;
                if (!Restored) { rec.env = Fold(EnvHash()); rec.flags |= FrontRoomsDoorBreakRecord.EnvSet; }
                FrontRoomsDoorBreakRecords.Put(map, rec);
            }
            var pack = FrontRoomsDoorBreakRecords.EnsurePack(map, door.edge);
            if (vfx != null && vfx.SkinUsed) pack.skin = vfx.SkinSnapshot();
            pack.solids.Clear();
            pack.solids.AddRange(takenSolids);
        }
    }

    // one floor mesh from the buckets: every index, or (far) only the debris pieces' share
    Mesh BuildFloorMesh(string name, bool piecesOnly, List<Material> usedMats)
    {
        var m = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
        var allV = new List<Vector3>(); var allN = new List<Vector3>(); var allT = new List<Vector4>(); var allU = new List<Vector2>();
        var subs = new List<List<int>>();
        for (var k = 0; k < bucketMat.Count; k++)
        {
            if (bi[k].Count == 0) continue;
            var count = piecesOnly ? (k < pieceIndexCount.Count ? pieceIndexCount[k] : 0) : bi[k].Count;
            var b0 = allV.Count;
            allV.AddRange(bv[k]); allN.AddRange(bn[k]); allT.AddRange(bt[k]); allU.AddRange(bu[k]);
            var ids = new List<int>(count);
            for (var x = 0; x < count; x++) ids.Add(b0 + bi[k][x]);
            subs.Add(ids);
            usedMats?.Add(bucketMat[k]);
        }
        m.SetVertices(allV); m.SetNormals(allN); m.SetTangents(allT); m.SetUVs(0, allU);
        m.subMeshCount = subs.Count;
        for (var k = 0; k < subs.Count; k++) m.SetTriangles(subs[k], k, false);
        m.RecalculateBounds();
        return m;
    }

    static ushort Fold(uint h) => (ushort)((h ^ (h >> 16)) & 0xffff);

    static readonly Collider[] envHits = new Collider[256];

    // The neighbourhood of the door when its debris settled: the map's colliders within 2.5 m (an order-free hash of their
    // quantised bounds; GD3c's EnvHash). The door's own colliders (its leaf, its rig) are not counted.
    uint EnvHash()
    {
        if (Rig == null || Rig.Frame == null) return 0;
        var root = Rig.Frame;
        var n = Physics.OverlapBoxNonAlloc(root.TransformPoint(new Vector3(0f, 1.2f, .5f)), new Vector3(2.5f, 1.2f, 2.5f), envHits, root.rotation, ~0, QueryTriggerInteraction.Ignore);
        uint h = 0;
        var count = 0;
        var mapT = map != null ? map.transform : null;
        for (var i = 0; i < n; i++)
        {
            var c = envHits[i];
            if (c == null || c is CharacterController || (c.attachedRigidbody != null && !c.attachedRigidbody.isKinematic)) continue;
            if (mapT != null && !c.transform.IsChildOf(mapT)) continue;
            if (door != null && door.leaf != null && c == door.leaf) continue;
            if (door != null && door.hinge != null && c.transform.IsChildOf(door.hinge)) continue;
            var b = c.bounds;
            var lc = b.center - root.position;
            unchecked
            {
                var k = (uint)(Mathf.RoundToInt(lc.x * 100f) * 73856093) ^ (uint)(Mathf.RoundToInt(lc.y * 100f) * 19349663) ^ (uint)(Mathf.RoundToInt(lc.z * 100f) * 83492791)
                    ^ (uint)(Mathf.RoundToInt(b.size.x * 100f) * 2654435761u) ^ (uint)(Mathf.RoundToInt(b.size.y * 100f) * 40503) ^ (uint)(Mathf.RoundToInt(b.size.z * 100f) * 2246822519u);
                h += k * 2654435761u + 0x9E3779B9u;
            }
            count++;
        }
        unchecked { return h ^ ((uint)count * 0x85EBCA6Bu); }
    }

    // After a shift the chunk may come back with new props: a debris piece or a merged crumb that would sit inside one is
    // dropped (the strike, the leaf and the skin stay). Runs a few frames after the restore (the chunk's props are built).
    void CheckNeighbourhood()
    {
        if (map == null || door == null) return;
        if (Fold(EnvHash()) == restoreEnv) { RestoreNote += ", neighbourhood unchanged"; return; }
        var mapT = map.transform;
        var dropped = 0;
        foreach (var t in debrisPieces)
        {
            if (t == null) continue;
            var r = t.GetComponent<Renderer>();
            if (r == null) continue;
            if (Phone && !t.gameObject.activeInHierarchy) continue;
            var b = Phone ? MeshWorldBounds(t) : r.bounds;
            var half = Vector3.Max(b.extents - new Vector3(.003f, .003f, .003f), new Vector3(.002f, .002f, .002f));
            var nh = Physics.OverlapBoxNonAlloc(b.center + Vector3.up * .004f, half, envHits, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            var blocked = false;
            for (var k = 0; k < nh && !blocked; k++)
            {
                var c = envHits[k];
                if (c == null || c is CharacterController || !c.transform.IsChildOf(mapT)) continue;
                if (c == door.leaf || (door.hinge != null && c.transform.IsChildOf(door.hinge))) continue;
                blocked = true;
            }
            if (blocked) { dropped++; t.gameObject.SetActive(false); }
        }
        DroppedByEnv = dropped;
        RestoreNote += ", neighbourhood changed: " + dropped + " pieces dropped";
        if (Phone) UpdateCombinedDebris();
    }

    // a piece's world bounds from its mesh (phones: its renderer is off, the combined mesh draws it)
    static Bounds MeshWorldBounds(Transform t)
    {
        var mf = t.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return new Bounds(t.position, Vector3.zero);
        var lb = mf.sharedMesh.bounds;
        var m = t.localToWorldMatrix;
        var b = new Bounds(m.MultiplyPoint3x4(lb.center), Vector3.zero);
        for (var k = 0; k < 8; k++)
            b.Encapsulate(m.MultiplyPoint3x4(lb.center + Vector3.Scale(lb.extents, new Vector3((k & 1) == 0 ? -1f : 1f, (k & 2) == 0 ? -1f : 1f, (k & 4) == 0 ? -1f : 1f))));
        return b;
    }

    // =========================================================================================== tools

    /// <summary>Tools: triangles and draws (renderer x submesh) of the visible break parts at a LOD level (0, 1 or 2).</summary>
    public void CountDrawn(int level, out int tris, out int draws)
    {
        tris = draws = 0;
        // phones: what LOD0 shows is the _L1 set (P5)
        var lvl = Phone && level == 0 ? 1 : level;
        foreach (var kv in parts)
        {
            if (!kv.Value.activeInHierarchy) continue;
            var k = kv.Key;
            var isLod = k.EndsWith("_L0", StringComparison.Ordinal) || k.EndsWith("_L1", StringComparison.Ordinal) || k.EndsWith("_L2", StringComparison.Ordinal);
            if (isLod && !k.EndsWith("_L" + lvl, StringComparison.Ordinal)) continue;
            if (!isLod && level == 2) continue;
            Count(kv.Value, ref tris, ref draws);
        }
        if (Phone && debrisCombinedGo != null) { if (debrisCombinedGo.activeInHierarchy) Count(debrisCombinedGo, ref tris, ref draws); }
        else foreach (var t in debrisPieces) if (t != null && t.gameObject.activeInHierarchy) Count(t.gameObject, ref tris, ref draws);
        if (floorGo != null)
        {
            var near = floorGo.Find(level == 0 ? "near" : "far");
            if (near != null) Count(near.gameObject, ref tris, ref draws);
        }
    }

    static void Count(GameObject go, ref int tris, ref int draws)
    {
        var f = go.GetComponent<MeshFilter>();
        if (f == null || f.sharedMesh == null) return;
        var m = f.sharedMesh;
        for (var s = 0; s < m.subMeshCount; s++) { tris += (int)(m.GetIndexCount(s) / 3); draws++; }
    }

    /// <summary>Tools: the debris pieces' lowest world Y relative to the door root (G5).</summary>
    public float DebrisLowest()
    {
        var low = float.MaxValue;
        if (Rig == null || Rig.Frame == null) return 0f;
        if (Phone && debrisCombinedGo != null)
        {
            var cr = debrisCombinedGo.GetComponent<Renderer>();
            return cr != null && debrisCombinedGo.activeInHierarchy ? cr.bounds.min.y - Rig.Frame.position.y : 0f;
        }
        foreach (var t in debrisPieces)
        {
            if (t == null || !t.gameObject.activeInHierarchy) continue;
            var r = t.GetComponent<Renderer>();
            if (r == null) continue;
            low = Mathf.Min(low, r.bounds.min.y - Rig.Frame.position.y);
        }
        return low == float.MaxValue ? 0f : low;
    }

    public int VfxLive => vfx != null ? vfx.Live : 0;
    public bool HardFloor => hardFloor;

    void OnDestroy()
    {
        if (leakMat != null) Destroy(leakMat);
        if (debrisCombined != null) Destroy(debrisCombined);
        vfx?.Dispose();
        vfx = null;
        if (map != null && door != null) FrontRoomsDoorBreakRecords.MarkReleased(map, door.edge);
    }
}
