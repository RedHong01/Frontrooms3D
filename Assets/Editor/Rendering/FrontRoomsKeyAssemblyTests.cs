using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FrontRooms.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// D1.6 key assembly checks (research/interactables/d16/10_design.md 13 item 4), edit mode:
/// - FrontRoomsKeyIdentity.Stub = the HUD's StubIdentity hash (fixed expectations), the tag kit name;
/// - DressKey: ring + key + tag under the root, no part collider, the look+E trigger ON THE ROOT (TakeKeyByE matches the
///   GameObject), at least 0.16 x 0.12 x 0.08 m;
/// - PoseHeld: after KeyPickup.TravelToHeld the parts sit at held_arrangement.json's local poses (0.1 mm / 0.1 deg); the
///   trigger is off; the HeldItem layer (30) only once the root is nearer the view than 1.5 x HeldDistance (0.525 m), back
///   on Release; no GC per PoseHeld or sway step; PoseInLock puts the shoulder at the pose.
/// Writes Verification/d16-key-assembly-tests.json. Headless: -executeMethod FrontRoomsKeyAssemblyTests.RunBatch -quit.
/// </summary>
public static class FrontRoomsKeyAssemblyTests
{
    static int passed, failed;
    static List<string> lines;

    static void Check(bool ok, string what)
    {
        if (ok) passed++; else failed++;
        lines.Add((ok ? "ok   " : "FAIL ") + what);
    }

    // Fix 2 (verify r2 E2): Unity's Mono may answer 0 from GC.GetAllocatedBytesForCurrentThread whatever is allocated. Probe it
    // with a 4 KB array; when it reads 0, measure with the heap's used size (GC.GetTotalMemory(false), all threads: a GC
    // between the reads hides allocations, other threads add some) and say which counter the check used.
    static int counterState;   // 0 unknown, 1 per-thread works, 2 heap fallback
    static bool PerThreadCounter()
    {
        if (counterState == 0)
        {
            var b = GC.GetAllocatedBytesForCurrentThread();
            var a = new byte[4096];
            a[11] = 1;
            counterState = GC.GetAllocatedBytesForCurrentThread() - b >= 4096 && a[11] == 1 ? 1 : 2;
        }
        return counterState == 1;
    }
    static long AllocNow() => PerThreadCounter() ? GC.GetAllocatedBytesForCurrentThread() : GC.GetTotalMemory(false);
    static string CounterName => PerThreadCounter() ? "per-thread counter" : "heap used size (per-thread counter reads 0)";

    [MenuItem("FrontRooms/Rendering/Test key assembly (D1.6)")]
    static void Menu() => Execute(false);

    public static void RunBatch() => Execute(true);

    static void Execute(bool throwOnFail)
    {
        passed = failed = 0;
        lines = new List<string>();
        var keepEdit = FrontRoomsInteractableKit.DressInEditMode;
        var keepClock = FrontRoomsKeyAssembly.ClockForTests;
        var made = new List<GameObject>();
        var profiles = new List<FrontRoomsLevelProfile>();
        try
        {
            if (Application.isBatchMode) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            FrontRoomsInteractableKit.DressInEditMode = true;
            Identity();
            Assembly(made);
            TagRules(made);
            RealKeyHandoff(made, profiles);
        }
        catch (Exception e)
        {
            Check(false, "harness threw " + e);
        }
        finally
        {
            FrontRoomsInteractableKit.DressInEditMode = keepEdit;
            FrontRoomsKeyAssembly.ClockForTests = keepClock;
            foreach (var g in made) if (g != null) UnityEngine.Object.DestroyImmediate(g);
            foreach (var pr in profiles) if (pr != null) UnityEngine.Object.DestroyImmediate(pr);
        }
        var sb = new StringBuilder();
        sb.Append("{\n \"suite\": \"FrontRoomsKeyAssemblyTests\",\n \"passed\": ").Append(passed).Append(",\n \"failed\": ").Append(failed).Append(",\n \"checks\": [\n");
        for (var i = 0; i < lines.Count; i++) sb.Append("  \"").Append(lines[i].Replace("\\", "/").Replace("\"", "'")).Append(i + 1 < lines.Count ? "\",\n" : "\"\n");
        sb.Append(" ]\n}\n");
        Directory.CreateDirectory("Verification");
        File.WriteAllText("Verification/d16-key-assembly-tests.json", sb.ToString());
        foreach (var l in lines) Debug.Log("[D16 key] " + l);
        Debug.Log("[D16 key] RESULT " + passed + " passed, " + failed + " failed");
        if (throwOnFail && failed > 0) throw new Exception("FrontRoomsKeyAssemblyTests: " + failed + " failed");
    }

    static void Identity()
    {
        // Shape and tint from the HUD's stub hash, computed outside Unity: (x, y) -> shape, tint (the old typed number is gone:
        // the tag prints the floor token since fix 1, DECISIONS #33).
        int[,] cases = { { 0, 0, 0, 0 }, { 1, 2, 0, 2 }, { -3, 5, 0, 1 }, { 7, -4, 1, 1 }, { 12, 9, 1, 0 } };
        for (var i = 0; i < cases.GetLength(0); i++)
        {
            var id = FrontRoomsKeyIdentity.Stub(new GridCoord(cases[i, 0], cases[i, 1]));
            Check((int)id.shape == cases[i, 2] && (int)id.tint == cases[i, 3] && id.floor == FrontRoomsKeyIdentity.CurrentFloor,
                "stub (" + cases[i, 0] + ", " + cases[i, 1] + ") = " + id + " (the HUD's hash, the current floor)");
        }
        // Since HUD key B landed (2026-10-29): the 3D key and the HUD glyph must agree for every zone and floor. The game's
        // HK1 HudIdentity(zone) = FrontRoomsKeyHud.StubIdentity(zone, floor - 1).
        {
            var bad = 0; var n = 0; var first = "";
            for (var x = -24; x <= 24; x++)
                for (var y = -24; y <= 24; y++)
                    for (var fl = 1; fl <= 12; fl += 5)
                    {
                        var z = new GridCoord(x, y);
                        var hud = FrontRoomsKeyHud.StubIdentity(z, fl - 1);
                        var kit = FrontRoomsKeyIdentity.Stub(z);
                        kit.floor = fl;
                        n++;
                        if ((int)hud.shape != (int)kit.shape || (int)hud.tint != (int)kit.tint || FrontRoomsKeyHud.FloorToken(hud.number) != kit.Token)
                        {
                            bad++;
                            if (first.Length == 0) first = " first " + z + " floor " + fl + ": hud " + hud + " kit " + kit;
                        }
                    }
            Check(bad == 0, "the 3D key = the HUD glyph (FrontRoomsKeyHud.StubIdentity(zone, floor - 1)): " + (n - bad) + " of " + n + " zone x floor cases agree" + first);
        }
        Check(new FrontRoomsKeyIdentity(FrontRoomsKeyIdentity.Shape.Round, FrontRoomsKeyIdentity.Tint.Blue, 7).TagKit == "Kit_KeyTag_Round_Blue"
              && new FrontRoomsKeyIdentity(FrontRoomsKeyIdentity.Shape.Long, FrontRoomsKeyIdentity.Tint.Red, 7).TagKit == "Kit_KeyTag_Long", "tag kit names");
        // The floor token (DECISIONS #28 / #32 / #33): 00 on LEVEL 0, then B1, B2, ...; the atlas cells.
        Check(FrontRoomsKeyIdentity.TokenOf(1) == "00" && FrontRoomsKeyIdentity.TokenOf(2) == "B1" && FrontRoomsKeyIdentity.TokenOf(10) == "B9" && FrontRoomsKeyIdentity.TokenOf(11) == "B10",
            "tokens: floor 1 00, 2 B1, 10 B9, 11 B10");
        Check(FrontRoomsKeyIdentity.TokenCell(1) == 0 && FrontRoomsKeyIdentity.TokenCell(2) == FrontRoomsKeyIdentity.FirstBCell && FrontRoomsKeyIdentity.TokenCell(11) == FrontRoomsKeyIdentity.FirstBCell + 9
              && FrontRoomsKeyIdentity.CellText(FrontRoomsKeyIdentity.TokenCell(11)) == "B10" && FrontRoomsKeyIdentity.CellText(0) == "00" && FrontRoomsKeyIdentity.CellText(FrontRoomsKeyIdentity.BlankCell) == "",
            "atlas cells: 00 = 0, B1 = " + FrontRoomsKeyIdentity.FirstBCell + ", B10 = " + (FrontRoomsKeyIdentity.FirstBCell + 9) + ", blank = " + FrontRoomsKeyIdentity.BlankCell);
        var keepFloor = FrontRoomsKeyIdentity.CurrentFloor;
        var heard = -1;
        System.Action<int> ear = f => heard = f;
        FrontRoomsKeyIdentity.FloorTokenChanged += ear;
        try
        {
            FrontRoomsKeyIdentity.SetFloor(3);
            Check(heard == 3 && FrontRoomsKeyIdentity.Stub(new GridCoord(1, 2)).Token == "B2", "SetFloor(3): FloorTokenChanged fired, a new stub prints B2");
        }
        finally
        {
            FrontRoomsKeyIdentity.FloorTokenChanged -= ear;
            FrontRoomsKeyIdentity.SetFloor(keepFloor);
        }
        // The type hold: blank paper until the graphic-visual line OKs the proofs.
        var keepType = FrontRoomsInteractableKit.ShowKitType;
        var probe = FrontRoomsKeyIdentity.Stub(new GridCoord(1, 2));
        FrontRoomsInteractableKit.ShowKitType = false;
        var held = FrontRoomsInteractableKit.TypeCell(probe);
        FrontRoomsInteractableKit.ShowKitType = true;
        var shown = FrontRoomsInteractableKit.TypeCell(probe);
        FrontRoomsInteractableKit.ShowKitType = keepType;
        Check(held == FrontRoomsKeyIdentity.BlankCell && shown == probe.Cell, "type held: cell " + held + " (blank); shown: cell " + shown + " (" + probe.Token + ")");
    }

    static void Assembly(List<GameObject> made)
    {
        var camGo = new GameObject("D16 key test camera") { hideFlags = HideFlags.DontSave };
        made.Add(camGo);
        var cam = camGo.AddComponent<Camera>();
        cam.depth = 90f;
        cam.transform.SetPositionAndRotation(new Vector3(0f, 1.6f, 0f), Quaternion.identity);
        var root = new GameObject("Key \u00b7 zone (1, 2)") { hideFlags = HideFlags.DontSave };
        made.Add(root);
        root.transform.position = new Vector3(0f, .76f, 2f);
        var id = FrontRoomsKeyIdentity.Stub(new GridCoord(1, 2));
        var a = FrontRoomsInteractableKit.DressKey(root.transform, id, FrontRoomsInteractableKit.KeyPlacement.Surface, true);
        Check(a != null, "DressKey built an assembly (" + id + ")");
        if (a == null) return;
        Check(a.Root == root.transform && a.KeyPart != null && a.RingPart != null && a.TagPart != null && a.TagKit == id.TagKit, "ring, key and tag under the root; TagKit " + a.TagKit);
        var rootCollider = root.GetComponent<BoxCollider>();
        Check(rootCollider != null && rootCollider == a.Trigger && rootCollider.isTrigger, "the look+E trigger is on the root GameObject");
        Check(a.Trigger.size.x >= .16f - 1e-5f && a.Trigger.size.y >= .12f - 1e-5f && a.Trigger.size.z >= .08f - 1e-5f, "trigger size " + a.Trigger.size.ToString("F3"));
        var partColliders = 0;
        foreach (var c in root.GetComponentsInChildren<Collider>(true)) if (c.gameObject != root) partColliders++;
        Check(partColliders == 0, "no part carries a collider (" + partColliders + ")");
        Check(a.KeyPart.position.y >= root.transform.position.y && a.Pose == KeyPose.Flat, "lying flat on its surface");

        // Held: far first (no layer), then near (layer 30), then the arrangement after the travel.
        var now = 0f;
        FrontRoomsKeyAssembly.ClockForTests = () => now;
        var lens = cam.transform;
        Pose PoseAt(float depth)
        {
            var p = lens.position + lens.forward * depth + lens.right * .1f - lens.up * .08f;
            return new Pose(p, Quaternion.LookRotation(lens.position - p, lens.up));
        }
        a.PoseHeld(PoseAt(1.0f));
        Check(!a.OnHeldLayer && a.KeyPart.gameObject.layer != FrontRoomsHeldItem.Layer, "1.0 m from the lens: still the Default layer");
        Check(a.Trigger != null && !a.Trigger.enabled, "the trigger is off once held");
        now = .1f;
        a.PoseHeld(PoseAt(.5f));
        Check(a.OnHeldLayer == FrontRoomsHeldItem.Supported, "0.50 m (< 0.525): HeldItem layer " + a.OnHeldLayer);
        var allOn = true;
        foreach (var r in root.GetComponentsInChildren<Renderer>(true)) if (r.gameObject.layer != FrontRoomsHeldItem.Layer) allOn = false;
        Check(allOn, "every part renderer on layer 30");
        Check(FrontRoomsHeldItem.Count >= 1, "the HeldItem pass has a holder (" + FrontRoomsHeldItem.Count + ")");
        now = .1f + FrontRoomsShotTimings.KeyPickup.TravelToHeld + .01f;
        a.PoseHeld(PoseAt(FrontRoomsShotTimings.KeyPickup.HeldDistance));
        var parts = new[] { a.KeyPart, a.RingPart, a.TagPart };
        for (var i = 0; i < 3; i++)
        {
            var shift = FrontRoomsKeyAssembly.HeldShapeOffset(a.Identity.shape);
            var dp = (parts[i].localPosition - (FrontRoomsKeyAssembly.HeldLocalPositionForTests(i) + shift)).magnitude;
            var dr = Quaternion.Angle(parts[i].localRotation, FrontRoomsKeyAssembly.HeldLocalRotationForTests(i));
            Check(dp < 1e-4f && dr < .1f, "held part " + i + ": " + (dp * 1000f).ToString("0.000") + " mm, " + dr.ToString("0.000") + " deg from held_arrangement.json + the "
                + a.Identity.shape + " swap offset " + (shift * 1000f).ToString("F2") + " mm");
        }
        // No GC per held frame or per sway step.
        a.Sway(25f, 2.2f, 2.5f);
        for (var i = 0; i < 10; i++) { now += 1f / 60f; a.PoseHeld(PoseAt(.35f)); a.StepForTools(1f / 60f); }
        var before = AllocNow();
        for (var i = 0; i < 240; i++) { now += 1f / 60f; a.PoseHeld(PoseAt(.35f)); a.StepForTools(1f / 60f); }
        var bytes = AllocNow() - before;
        Check(bytes == 0, "240 held frames with sway allocate " + bytes + " bytes (" + CounterName + ")");
        a.Release();
        var allBack = true;
        foreach (var r in root.GetComponentsInChildren<Renderer>(true)) if (r.gameObject.layer == FrontRoomsHeldItem.Layer) allBack = false;
        Check(allBack && !a.OnHeldLayer, "Release: parts back on their layer");

        // In the lock: the shoulder at the pose.
        var lockPose = new Pose(new Vector3(.3f, 1f, 1f), Quaternion.LookRotation(Vector3.right, Vector3.up));
        a.PoseInLock(lockPose);
        Check((a.KeyPart.position - lockPose.position).magnitude < 1e-5f && Quaternion.Angle(a.KeyPart.rotation, lockPose.rotation) < .01f && a.Pose == KeyPose.InLock, "PoseInLock: the shoulder at the keyhole, +Z in, +Y up");
        var hole = a.KeyPart.TransformPoint(FrontRoomsKeyAssembly.KeyRingHole);
        Check((a.RingPart.position - hole).magnitude < 1e-5f, "the ring hangs from the key's hole");

        // A hung key on a host frame.
        var root2 = new GameObject("Key \u00b7 zone (2, 2)") { hideFlags = HideFlags.DontSave };
        made.Add(root2);
        var hung = FrontRoomsInteractableKit.DressKey(root2.transform, FrontRoomsKeyIdentity.Stub(new GridCoord(2, 2)), FrontRoomsInteractableKit.KeyPlacement.HostHung, false);
        Check(hung != null && hung.Trigger == null && root2.GetComponent<Collider>() == null, "hung key with no trigger when the map asks for none");
        if (hung != null) Check(Vector3.Dot(hung.KeyPart.forward, Vector3.down) > .95f, "hung: the key's tip points down");
    }

    // ------------------------------------------------------------------ fix 2 (verify round 2)

    static Vector4 TagST(FrontRoomsKeyAssembly a)
    {
        var block = new MaterialPropertyBlock();
        foreach (var r in a.TagPart.GetComponentsInChildren<MeshRenderer>(true))
        {
            var mats = r.sharedMaterials;
            for (var i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null || (mats[i].name != "Prop_KeyTagNo" && mats[i].name != "Prop_Paper")) continue;
                r.GetPropertyBlock(block, i);
                return block.GetVector("_BaseMap_ST");
            }
        }
        return new Vector4(-1f, -1f, -1f, -1f);
    }

    // Fix 3 (verify r2 spec S2): the key TAG's printed token is HELD until the graphic-visual line signs off the 3D tag type
    // (shipped: blank paper on every tag; plates, signs and hook labels held as before). With the type on (the proof state),
    // every tag shape takes its token as set, B10+ at 0.8x on the same baseline inside its cell (DECISIONS #33: never scaled
    // to fit, the round tag included). Look r2 #1: the key part is the satin brass. Fix 3 (S1): the swap offsets keep the depth.
    static void TagRules(List<GameObject> made)
    {
        Check(!FrontRoomsInteractableKit.ShowTagTypeShipped && !FrontRoomsInteractableKit.ShowKitTypeShipped,
            "shipped defaults: the tag token HELD (ShowTagType off until the graphic-visual line signs off), plates / signs / hook labels held (ShowKitType off); this run: tag "
            + FrontRoomsInteractableKit.ShowTagType + ", kit type " + FrontRoomsInteractableKit.ShowKitType);
        var offsetsOk = true; var offs = "";
        for (var i = 0; i < FrontRoomsKeyAssembly.HeldShapeOffsets.Length; i++)
        {
            var o = FrontRoomsKeyAssembly.HeldShapeOffsets[i];
            if (Mathf.Abs(o.z) > 1e-6f || o.magnitude > .02f) offsetsOk = false;
            offs += (i == 0 ? "" : ", ") + (FrontRoomsKeyIdentity.Shape)i + " " + (o * 1000f).ToString("F2");
        }
        Check(FrontRoomsKeyAssembly.HeldShapeOffsets.Length == 3 && offsetsOk, "swap offsets (mm, held frame): " + offs + "; Z 0 (the depth and the HeldItem rule unchanged), each under 20 mm");
        var keepTag = FrontRoomsInteractableKit.ShowTagType;
        foreach (var proof in new[] { false, true })
        {
        FrontRoomsInteractableKit.ShowTagType = proof || FrontRoomsInteractableKit.ShowTagTypeShipped;
        var shapes = new[] { FrontRoomsKeyIdentity.Shape.Rect, FrontRoomsKeyIdentity.Shape.Round, FrontRoomsKeyIdentity.Shape.Long };
        var floors = new[] { 1, 2, 11, 75 };
        var bad = 0; var n = 0; var first = "";
        for (var si = 0; si < shapes.Length; si++)
        for (var fi = 0; fi < floors.Length; fi++)
        {
            var root = new GameObject("Key \u00b7 tag rule " + si + "-" + fi) { hideFlags = HideFlags.DontSave };
            made.Add(root);
            root.transform.position = new Vector3(10f + si, 0f, 10f + fi);
            var id = new FrontRoomsKeyIdentity(shapes[si], (FrontRoomsKeyIdentity.Tint)0, floors[fi]);
            var a = FrontRoomsInteractableKit.DressKey(root.transform, id, FrontRoomsInteractableKit.KeyPlacement.Floor, false);
            n++;
            if (a == null) { bad++; continue; }
            var st = TagST(a);
            var cell = FrontRoomsInteractableKit.ShowTagType ? id.Cell : FrontRoomsKeyIdentity.BlankCell;
            var want = new Vector4(1f, 1f, (cell % 10) / 10f, -(cell / 10) / 10f);
            if ((st - want).magnitude > 1e-5f) { bad++; if (first.Length == 0) first = "; first miss " + id + " ST " + st.ToString("F4") + " want " + want.ToString("F4"); }
            if (si == 0 && fi == 0 && !proof)
            {
                var brass = 0; var oldBrass = 0;
                foreach (var r in a.KeyPart.GetComponentsInChildren<Renderer>(true))
                    foreach (var m in r.sharedMaterials) { if (m == null) continue; if (m.name == "Prop_BrassSatin") brass++; else if (m.name == "Prop_Brass") oldBrass++; }
                Check(brass > 0 && oldBrass == 0, "the key part wears Prop_BrassSatin (" + brass + " slots), no Prop_Brass left (" + oldBrass + ")");
            }
        }
        Check(bad == 0, (proof ? "tag tokens (proof, type on): " : "tag tokens (shipped, type held): ") + (n - bad) + " of " + n + " rect / round / long x 00 / B1 / B10 / B74 show "
            + (proof ? "their token's cell as set, every shape at crop x1 (#33, the round tag's B10+ included)" : "the blank cell " + FrontRoomsKeyIdentity.BlankCell) + first);
        }
        FrontRoomsInteractableKit.ShowTagType = keepTag;
    }

    // Spec r2 #1: the REAL key (DressKey) through the map's presenter, FrontRoomsDoorShot.BeginKeyPickup, with a KeyHandoff
    // listener as the game's key HUD has (HK1 / KP-1): one hand-off at KeyPickup.HoldUntil (0.75 s) at the held viewport point
    // (0.65, 0.30) and depth HeldDistance (0.35 m); on the HeldItem layer before it; the root inactive from that frame; off
    // the HeldItem holder list and destroyed once FinishKey has run (PocketEnd 0.95 s). Also the held key's size on a
    // 1920 x 1080 view at FOV 76 on the frame before the hand-off (DECISIONS #24 / O-16), to
    // Verification/d16-key-handoff.json.
    static void RealKeyHandoff(List<GameObject> made, List<FrontRoomsLevelProfile> profiles)
    {
        const float Dt = 1f / 60f;
        var profile = UnityEngine.Object.Instantiate(FrontRoomsLevelProfiles.Resolve());
        profile.hideFlags = HideFlags.DontSave;
        profiles.Add(profile);
        var mapRoot = new GameObject("D16 KEY HANDOFF TEST") { hideFlags = HideFlags.DontSave };
        made.Add(mapRoot);
        mapRoot.transform.position = new Vector3(-41f * ModuleUnits.WorldPeriod, 0f, -31f * ModuleUnits.WorldPeriod);
        var world = mapRoot.AddComponent<FrontRoomsMapWorld>();
        world.Profile = profile;
        world.BuildForCapture();
        var body = world.Player;
        var camera = new GameObject("D16 key handoff camera").AddComponent<Camera>();
        camera.transform.SetParent(mapRoot.transform, false);
        camera.fieldOfView = 76f;
        camera.aspect = 16f / 9f;
        var rig = FrontRoomsCameraRig.Attach(body, camera, ModuleUnits.PlayerEye);
        var shot = new FrontRoomsDoorShot(rig, world, camera, body);
        // A stand at the spawn facing the first of four yaws with 1.5 m clear ahead at eye height (so ClampToWorld keeps 0.35 m).
        var feet = world.SpawnWorldPosition;
        var yaw = 0f;
        for (var k = 0; k < 4; k++)
        {
            var dir = Quaternion.Euler(0f, k * 90f, 0f) * Vector3.forward;
            if (!Physics.Raycast(feet + Vector3.up * ModuleUnits.PlayerEye, dir, 1.5f, ~0, QueryTriggerInteraction.Ignore)
                && !Physics.Raycast(feet + Vector3.up * .3f, dir, 1.5f, ~0, QueryTriggerInteraction.Ignore)) { yaw = k * 90f; break; }
        }
        body.SetPositionAndRotation(feet, Quaternion.Euler(0f, yaw, 0f));
        rig.SetBase(0f);
        Physics.SyncTransforms();
        rig.Tick(Dt);
        var lens = camera.transform;
        var root = new GameObject("Key \u00b7 zone handoff test") { hideFlags = HideFlags.DontSave };
        made.Add(root);
        var fwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        root.transform.SetPositionAndRotation(new Vector3(feet.x, mapRoot.transform.position.y, feet.z) + fwd * .6f, Quaternion.LookRotation(fwd, Vector3.up));
        var zone = new GridCoord(-9998, -9998);
        var id = FrontRoomsKeyIdentity.Stub(new GridCoord(1, 2));
        var a = FrontRoomsInteractableKit.DressKey(root.transform, id, FrontRoomsInteractableKit.KeyPlacement.Floor, true);
        Check(a != null, "hand-off: DressKey built the real key on the floor 0.6 m ahead (" + id + ")");
        if (a == null) return;
        var clock = 0f;
        FrontRoomsKeyAssembly.ClockForTests = () => clock;
        var count0 = FrontRoomsHeldItem.Count;
        var calls = 0; var at = -1f; var clockAt = -1f; var depthAt = -1f; var viewport = Vector2.zero; var zoneAt = default(GridCoord);
        var activeAfter = false; var heldLayerBefore = false; var countHeld = 0;
        var w = 0f; var h = 0f; var cx = 0f; var cy = 0f; var keyW = 0f; var keyH = 0f; var depthKey = 0f;
        var key = root.transform;
        var assembly = a;
        Action<GridCoord, Vector2, float, float> listener = (z, v, d, c) => { calls++; at = clock; clockAt = c; depthAt = d; viewport = v; zoneAt = z; };
        var began = false;
        var goneAt = -1f;
        var countAfter = -1;
        FrontRoomsDoorShot.KeyHandoff += listener;
        try
        {
            began = shot.BeginKeyPickup(zone, key);
            while (clock < 1.05f - Dt * .5f)
            {
                clock += Dt;
                shot.Tick(Dt, false, false, -1f);
                rig.Tick(Dt);
                shot.PlaceKey();
                Physics.SyncTransforms();
                if (key != null && calls == 0 && clock >= .5f - 1e-4f && clock < .5f + Dt) { heldLayerBefore = assembly.OnHeldLayer; countHeld = FrontRoomsHeldItem.Count; }
                // The frame before the hand-off: the held key's extent on a 1920 x 1080 view (LOD0 meshes, every vertex).
                if (key != null && calls == 0 && clock >= FrontRoomsShotTimings.KeyPickup.HoldUntil - Dt - 1e-4f && clock < FrontRoomsShotTimings.KeyPickup.HoldUntil - 1e-4f)
                {
                    Extent(key, camera, null, out w, out h, out cx, out cy, out depthKey);
                    Extent(key, camera, assembly.KeyPart, out keyW, out keyH, out _, out _, out _);
                }
                if (key != null && calls > 0) activeAfter |= key.gameObject.activeSelf;
                if (goneAt < 0f && key == null && !shot.Presenting(zone)) { goneAt = clock; countAfter = FrontRoomsHeldItem.Count; }
            }
        }
        finally
        {
            FrontRoomsDoorShot.KeyHandoff -= listener;
            shot.Abort();
        }
        var kp = FrontRoomsShotTimings.KeyPickup.HoldUntil;
        Check(began, "hand-off: BeginKeyPickup takes the real key (its IFrontRoomsKeyRig is the assembly)");
        Check(calls == 1 && Mathf.Abs(at - kp) <= Dt * 1.01f && Mathf.Abs(clockAt - kp) <= Dt * 1.01f && zoneAt == zone
              && viewport == new Vector2(FrontRoomsShotTimings.KeyPickup.HeldViewportX, FrontRoomsShotTimings.KeyPickup.HeldViewportY)
              && Mathf.Abs(depthAt - FrontRoomsShotTimings.KeyPickup.HeldDistance) < 1e-3f,
            "hand-off (real key): " + calls + " KeyHandoff at " + at.ToString("0.000") + " s (0.75), clock " + clockAt.ToString("0.000") + ", viewport ("
            + viewport.x.ToString("0.00") + ", " + viewport.y.ToString("0.00") + "), depth " + depthAt.ToString("0.000") + " m (0.35)");
        Check(heldLayerBefore == FrontRoomsHeldItem.Supported && countHeld >= count0 + (FrontRoomsHeldItem.Supported ? 1 : 0),
            "hand-off: at 0.50 s the real key is on the HeldItem layer (" + heldLayerBefore + "), holders " + countHeld + " (before " + count0 + ")");
        Check(!activeAfter, "hand-off: the real key's root is inactive from the hand-off frame on");
        Check(Mathf.Abs(goneAt - FrontRoomsShotTimings.KeyPickup.PocketEnd) <= Dt * 1.01f && countAfter == count0 && !shot.Presenting(zone),
            "hand-off: FinishKey at " + goneAt.ToString("0.000") + " s (0.95): the root destroyed, HeldItem holders back to " + countAfter + " (before " + count0 + ")");
        // The swap measurement (DECISIONS #24 / O-16): px on 1920 x 1080 at FOV 76; the HUD's P4 starts the glyph's 72 x 48
        // lab box (top-left = hand - (36, 24) x factor, FrontRoomsKeyHud.StartFlight) at PickupFactorX / Y = 3.
        var ic = System.Globalization.CultureInfo.InvariantCulture;
        var json = "{\n \"what\": \"D1.6 real key at the KP-1 hand-off, frame before HoldUntil (" + (kp - Dt).ToString("0.000", ic) + " s), FrontRoomsKeyAssemblyTests.RealKeyHandoff\",\n"
            + " \"view\": {\"width\": 1920, \"height\": 1080, \"fov_v_deg\": 76},\n"
            + " \"identity\": \"" + id + "\",\n"
            + " \"held_viewport\": [" + FrontRoomsShotTimings.KeyPickup.HeldViewportX.ToString("0.000", ic) + ", " + FrontRoomsShotTimings.KeyPickup.HeldViewportY.ToString("0.000", ic) + "], \"held_depth_m\": " + depthAt.ToString("0.000", ic) + ",\n"
            + " \"assembly_px\": {\"w\": " + w.ToString("0.0", ic) + ", \"h\": " + h.ToString("0.0", ic) + ", \"centre_x\": " + cx.ToString("0.0", ic) + ", \"centre_y_from_top\": " + cy.ToString("0.0", ic) + ", \"nearest_vertex_depth_m\": " + depthKey.ToString("0.000", ic) + "},\n"
            + " \"key_part_px\": {\"w\": " + keyW.ToString("0.0", ic) + ", \"h\": " + keyH.ToString("0.0", ic) + "},\n"
            + " \"hud_glyph_lab_box\": [72, 48], \"hud_pickup_factor_now\": [" + FrontRoomsKeyHud.PickupFactorX.ToString("0.###", ic) + ", " + FrontRoomsKeyHud.PickupFactorY.ToString("0.###", ic) + "],\n"
            + " \"factor_matching_assembly\": [" + (w / 72f).ToString("0.000", ic) + ", " + (h / 48f).ToString("0.000", ic) + "]\n}\n";
        Directory.CreateDirectory("Verification");
        File.WriteAllText("Verification/d16-key-handoff.json", json);
        lines.Add("info hand-off size: assembly " + w.ToString("0.0") + " x " + h.ToString("0.0") + " px (key part " + keyW.ToString("0.0") + " x " + keyH.ToString("0.0")
            + ") at 1920 x 1080; factor vs the 72 x 48 glyph box " + (w / 72f).ToString("0.00") + " x " + (h / 48f).ToString("0.00") + " (HUD now 3 x 3)");
    }

    // Screen extent (1920 x 1080 px, y from the top) of the LOD0 meshes under a root (or one part), every vertex.
    static void Extent(Transform root, Camera cam, Transform only, out float w, out float h, out float cx, out float cy, out float nearest)
    {
        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        nearest = float.MaxValue;
        var lod1Up = new HashSet<Renderer>();
        foreach (var g in root.GetComponentsInChildren<LODGroup>(true))
        {
            var lods = g.GetLODs();
            for (var i = 1; i < lods.Length; i++) foreach (var r in lods[i].renderers) if (r != null) lod1Up.Add(r);
        }
        foreach (var mf in (only != null ? only : root).GetComponentsInChildren<MeshFilter>(true))
        {
            var r = mf.GetComponent<MeshRenderer>();
            if (r == null || !r.enabled || lod1Up.Contains(r) || mf.sharedMesh == null) continue;
            var m = mf.transform.localToWorldMatrix;
            foreach (var v in mf.sharedMesh.vertices)
            {
                var p = cam.WorldToViewportPoint(m.MultiplyPoint3x4(v));
                if (p.z <= 0f) continue;
                nearest = Mathf.Min(nearest, p.z);
                x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); y0 = Mathf.Min(y0, p.y); y1 = Mathf.Max(y1, p.y);
            }
        }
        if (x1 < x0) { w = h = cx = cy = 0f; nearest = 0f; return; }
        w = (x1 - x0) * 1920f; h = (y1 - y0) * 1080f; cx = (x0 + x1) * .5f * 1920f; cy = (1f - (y0 + y1) * .5f) * 1080f;
    }
}
