using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FrontRooms.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// D1.6 door rig checks (research/interactables/d16/10_design.md 13 item 4), edit mode, on real map doors dressed by
/// FrontRoomsInteractableKit.DressDoor (DressInEditMode on for the run):
/// - the hierarchy: "Leaf rig" under the hinge, the cube's renderer off, its collider and transform untouched, no collider
///   and no forbidden name under the rig or the frame;
/// - the keyhole at X +-0.0315 (locked) / the rose spindle at +-0.0385 (free), Y 1.000, Z 0.920 on both faces (0.1 mm), the
///   keyway direction and up, the map's LockPose through the rig;
/// - plug, knob and lever signs (the plug's cuts turn toward the hinge on both faces; both levers' grips go down), bolt
///   travel (deadbolt 0.025, latch 0.019 mortise / 0.013 bored);
/// - Jolt direction against door.swing (the latch end moves toward -swing * rootX) and its return;
/// - the collider follow (crooked hang, rattle, blows move the kit leaf with no map change);
/// - the strike's detach; no GC per Tick; the per-door LOD0 cost (renderers, triangles) for T10.
/// Writes Verification/d16-door-rig-tests.json. Headless: -executeMethod FrontRoomsDoorRigTests.RunBatch -quit (throws on FAIL).
/// </summary>
public static class FrontRoomsDoorRigTests
{
    static int passed, failed;
    static List<string> lines;
    static StringBuilder cost;

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

    [MenuItem("FrontRooms/Rendering/Test door rigs (D1.6)")]
    static void Menu() => Execute(false);

    public static void RunBatch() => Execute(true);

    static void Execute(bool throwOnFail)
    {
        passed = failed = 0;
        lines = new List<string>();
        cost = new StringBuilder();
        var roots = new List<GameObject>();
        var profiles = new List<FrontRoomsLevelProfile>();
        var keepEdit = FrontRoomsInteractableKit.DressInEditMode;
        var keepLocked = FrontRoomsInteractableKit.DoorLockedOverride;
        try
        {
            if (Application.isBatchMode) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            FrontRoomsInteractableKit.DressInEditMode = true;
            foreach (var locked in new[] { true, false })
            {
                FrontRoomsInteractableKit.DoorLockedOverride = d => locked;
                var world = Build(roots, profiles, locked, locked ? 1 : 2);
                var door = FindDoor(world);
                if (door == null) { Check(false, (locked ? "LOCKED" : "FREE") + ": no shut door near the spawn"); continue; }
                RigChecks(world, door, locked);
            }
        }
        catch (Exception e)
        {
            Check(false, "harness threw " + e);
        }
        finally
        {
            FrontRoomsInteractableKit.DressInEditMode = keepEdit;
            FrontRoomsInteractableKit.DoorLockedOverride = keepLocked;
            foreach (var r in roots) if (r != null) UnityEngine.Object.DestroyImmediate(r);
            foreach (var p in profiles) if (p != null) UnityEngine.Object.DestroyImmediate(p);
        }
        var sb = new StringBuilder();
        sb.Append("{\n \"suite\": \"FrontRoomsDoorRigTests\",\n \"passed\": ").Append(passed).Append(",\n \"failed\": ").Append(failed).Append(",\n \"cost\": [").Append(cost).Append("],\n \"checks\": [\n");
        for (var i = 0; i < lines.Count; i++) sb.Append("  \"").Append(lines[i].Replace("\\", "/").Replace("\"", "'")).Append(i + 1 < lines.Count ? "\",\n" : "\"\n");
        sb.Append(" ]\n}\n");
        Directory.CreateDirectory("Verification");
        File.WriteAllText("Verification/d16-door-rig-tests.json", sb.ToString());
        foreach (var l in lines) Debug.Log("[D16 rig] " + l);
        Debug.Log("[D16 rig] RESULT " + passed + " passed, " + failed + " failed");
        if (throwOnFail && failed > 0) throw new Exception("FrontRoomsDoorRigTests: " + failed + " failed");
    }

    static FrontRoomsMapWorld Build(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles, bool keys, int slot)
    {
        var profile = UnityEngine.Object.Instantiate(FrontRoomsLevelProfiles.Resolve());
        profile.hideFlags = HideFlags.DontSave;
        profile.doorsNeedKeys = keys;
        profiles.Add(profile);
        var root = new GameObject("D16 RIG TEST " + slot) { hideFlags = HideFlags.DontSave };
        roots.Add(root);
        root.transform.position = new Vector3((-35f - 2f * slot) * ModuleUnits.WorldPeriod, 0f, -29f * ModuleUnits.WorldPeriod);
        var world = root.AddComponent<FrontRoomsMapWorld>();
        world.Profile = profile;
        world.BuildForCapture();
        return world;
    }

    static FrontRoomsMapWorld.Door FindDoor(FrontRoomsMapWorld world)
    {
        var mid = world.CellOf(world.SpawnWorldPosition);
        for (var r = 0; r <= 14; r++)
        for (var y = mid.y - r; y <= mid.y + r; y++)
        for (var x = mid.x - r; x <= mid.x + r; x++)
        {
            if (Mathf.Max(Mathf.Abs(x - mid.x), Mathf.Abs(y - mid.y)) != r) continue;
            var cell = new GridCoord(x, y);
            foreach (var step in new[] { new GridCoord(1, 0), new GridCoord(0, 1) })
            {
                var d = world.DoorBetween(cell, cell + step);
                if (d != null && !d.open && !d.broken && d.rig != null) return d;
            }
        }
        return null;
    }

    static Quaternion RootRot(FrontRoomsMapWorld.Door d) => (d.hinge.parent != null ? d.hinge.parent.rotation : Quaternion.identity) * d.closed;
    // The leaf's own frame: the hinge (the door root when shut, swung with the leaf otherwise).
    static Vector3 Local(FrontRoomsMapWorld.Door d, Vector3 world) => Quaternion.Inverse(d.hinge.rotation) * (world - d.hinge.position);
    static string V(Vector3 v) => "(" + v.x.ToString("0.00000") + ", " + v.y.ToString("0.00000") + ", " + v.z.ToString("0.00000") + ")";

    static void RigChecks(FrontRoomsMapWorld world, FrontRoomsMapWorld.Door door, bool locked)
    {
        var tag = locked ? "LOCKED " : "FREE ";
        var rig = door.rig as FrontRoomsDoorRig;
        Check(rig != null, tag + "door.rig is the kit's FrontRoomsDoorRig");
        if (rig == null) return;
        Check(rig.transform.parent == door.hinge && rig.name == FrontRoomsInteractableKit.LeafRigLabel, tag + "'Leaf rig' hangs under " + door.hinge.name);
        Check(rig.Acting == DoorActing.Double && Mathf.Approximately(rig.SSign, 1f), tag + "double-acting, S_sign +1 (no handing mirror)");
        Check(rig.LockedModel == locked, tag + "model " + (rig.LockedModel ? "LOCKED" : "FREE") + ", member " + rig.Member);
        var cube = door.leaf.GetComponent<MeshRenderer>();
        Check(cube != null && !cube.enabled, tag + "the cube leaf's renderer is off");
        var box = door.leaf as BoxCollider;
        Check(box != null && box.enabled && !box.isTrigger && door.leaf.name == "Door leaf", tag + "the 'Door leaf' collider is untouched");
        Check((door.leaf.transform.localScale - new Vector3(ModuleUnits.DoorLeafThickness, ModuleUnits.DoorHeight - ModuleUnits.DoorLeafGap, ModuleUnits.DoorWidth - ModuleUnits.DoorLeafGap)).magnitude < 1e-5f,
            tag + "collider size 0.05 x 2.08 x 0.98");
        Check(rig.GetComponentsInChildren<Collider>(true).Length == 0 && (rig.Frame == null || rig.Frame.GetComponentsInChildren<Collider>(true).Length == 0), tag + "no collider under the rig or the frame");
        var bad = 0;
        foreach (var t in rig.GetComponentsInChildren<Transform>(true)) if (BadName(t.name)) bad++;
        if (rig.Frame != null) foreach (var t in rig.Frame.GetComponentsInChildren<Transform>(true)) if (BadName(t.name)) bad++;
        Check(bad == 0, tag + "no child named 'Door hinge*', 'Light' or 'fluorescent light*' (" + bad + ")");
        SurfaceAndTraceChecks(door, rig, tag);
        Check(rig.Frame != null && rig.Frame.parent == door.hinge.parent && (rig.Frame.localPosition - door.hinge.localPosition).magnitude < 1e-6f, tag + "frame at the door root (the hinge's local pose)");

        // The rig's pose under the hinge at rest, and the collider's (diagnostics for the keyhole checks).
        lines.Add("info " + tag + "rig local " + rig.transform.localPosition.ToString("F6") + " euler " + rig.transform.localEulerAngles.ToString("F5")
            + " | collider local " + door.leaf.transform.localPosition.ToString("F6") + " euler " + door.leaf.transform.localEulerAngles.ToString("F5")
            + " | door angle " + door.angle.ToString("F5") + " swing " + door.swing);
        // Keyhole, both faces.
        var x = locked ? FrontRoomsDoorRig.KeyholeX : FrontRoomsDoorRig.RoseSpindleX;
        var ks = Local(door, rig.Keyhole(DoorFace.S));
        var kp = Local(door, rig.Keyhole(DoorFace.P));
        // World round trip at ~4.7 km from the origin (the test roots): float ulp there is 0.49 mm, so 1 mm here; the exact
        // check is in the rig's own space below (the parts the keyhole stands for, no large coordinates).
        Check((ks - new Vector3(x, 1f, .92f)).magnitude < 1e-3f, tag + "keyhole S " + V(ks) + " = (+" + x + ", 1.000, 0.920) within 1 mm (world, float ulp 0.49 mm at 4.7 km)");
        Check((kp - new Vector3(-x, 1f, .92f)).magnitude < 1e-3f, tag + "keyhole P " + V(kp) + " = (-" + x + ", 1.000, 0.920) within 1 mm (world, float ulp 0.49 mm at 4.7 km)");
        for (var side = 0; side < 2; side++)
        {
            var sgn = side == 0 ? 1f : -1f;
            var part = locked ? rig.PlugForTests(side) : rig.HandleForTests(side);
            // Locked: the plug sits on the escutcheon's keyhole = the seam's keyhole. Free: the lever on the rose spindle, 6.5 mm
            // inside the seam's fallback point (RoseSpindleX).
            var want = locked ? new Vector3(sgn * FrontRoomsDoorRig.KeyholeX, FrontRoomsDoorRig.KeyholeY, FrontRoomsDoorRig.KeyholeZ)
                : new Vector3(sgn * (FrontRoomsDoorRig.RoseSpindleX - .0065f), FrontRoomsDoorRig.KeyholeY, FrontRoomsDoorRig.KeyholeZ);
            var got = part != null ? rig.transform.InverseTransformPoint(part.position) : new Vector3(99f, 99f, 99f);
            if (part != null && part.parent == rig.transform) got = part.localPosition;
            Check((got - want).magnitude < 1e-5f, tag + (locked ? "plug " : "lever ") + (side == 0 ? "s" : "p") + " at " + V(got) + " in the rig = " + V(want) + " (0.01 mm)");
        }
        var right = door.hinge.rotation * Vector3.right;
        Check(Vector3.Dot(rig.KeyholeDir(DoorFace.S), -right) > .9999f && Vector3.Dot(rig.KeyholeDir(DoorFace.P), right) > .9999f, tag + "keyway into the lock: -rootX from S, +rootX from P");
        Check(Vector3.Dot(rig.KeyholeUp(DoorFace.S), Vector3.up) > .9999f, tag + "keyway up = +Y");
        var probeS = door.position + right * 1f;
        var lp = world.LockPose(door, probeS);
        Check((lp.position - rig.Keyhole(DoorFace.S)).magnitude < 1e-5f && FrontRoomsMapWorld.FaceOf(door, probeS) == DoorFace.S, tag + "the map's LockPose and FaceOf go through the rig (S = +X)");
        if (locked)
        {
            var esc = rig.HandleForTests(0) != null ? rig.HandleForTests(0).parent.Find("Lock escutcheon s") : null;
            Check(esc != null, tag + "escutcheon s present");
        }

        var towardHinge = -(RootRot(door) * Vector3.forward);
        if (locked)
        {
            for (var side = 0; side < 2; side++)
            {
                var face = side == 0 ? DoorFace.S : DoorFace.P;
                var plug = rig.PlugForTests(side);
                rig.SetPlug(face, 30f);
                Check(plug != null && Vector3.Dot(plug.up, towardHinge) > .45f, tag + "plug " + face + " +30 turns its cuts toward the hinge (dot " + (plug != null ? Vector3.Dot(plug.up, towardHinge).ToString("0.00") : "-") + ")");
                rig.SetPlug(face, 0f);
            }
            var r0 = rig.HandleForTests(0).localRotation;
            var r1 = rig.HandleForTests(1).localRotation;
            rig.SetKnob(DoorFace.S, 40f);
            Check(Quaternion.Angle(r0, rig.HandleForTests(0).localRotation) > 39f && Quaternion.Angle(r1, rig.HandleForTests(1).localRotation) > 39f, tag + "SetKnob(S, 40) turns both knobs 40 deg (through-spindle)");
            rig.SetLever(DoorFace.S, 35f);
            Check(Quaternion.Angle(r0, rig.HandleForTests(0).localRotation) > 39f, tag + "SetLever is a no-op on the locked model");
            rig.SetKnob(DoorFace.S, 0f);
            var db0 = rig.DeadboltForTests.localPosition;
            rig.SetDeadbolt(1f);
            Check(Mathf.Abs((db0 - rig.DeadboltForTests.localPosition).z - FrontRoomsDoorRig.DeadboltTravel) < 1e-5f, tag + "deadbolt draws 0.025 along -Z");
            rig.SetDeadbolt(0f);
        }
        else
        {
            var grip0 = new Vector3[2];
            for (var side = 0; side < 2; side++)
            {
                var lever = rig.HandleForTests(side);
                grip0[side] = lever.TransformPoint(new Vector3(.121f, 0f, .033f));
                Check(Vector3.Dot(grip0[side] - lever.position, towardHinge) > .08f, tag + "lever " + (side == 0 ? "s" : "p") + " points at the hinge");
            }
            rig.SetLever(DoorFace.P, 35f);
            for (var side = 0; side < 2; side++)
            {
                var g = rig.HandleForTests(side).TransformPoint(new Vector3(.121f, 0f, .033f));
                Check(grip0[side].y - g.y > .05f, tag + "SetLever(P, 35): lever " + (side == 0 ? "s" : "p") + " grip goes down " + ((grip0[side].y - g.y) * 1000f).ToString("0") + " mm");
            }
            rig.SetLever(DoorFace.P, 0f);
            Check(rig.DeadboltForTests == null && rig.PlugForTests(0) == null, tag + "no deadbolt and no plug on the free model");
        }
        var l0 = rig.LatchForTests.localPosition;
        rig.SetLatch(1f);
        var travel = locked ? FrontRoomsDoorRig.LatchTravelMortise : FrontRoomsDoorRig.LatchTravelBored;
        Check(Mathf.Abs((l0 - rig.LatchForTests.localPosition).z - travel) < 1e-5f, tag + "latch draws " + travel + " along -Z");
        rig.SetLatch(0f);

        // Jolt against the swing side.
        var keepSwing = door.swing;
        foreach (var s in new[] { 1f, -1f })
        {
            door.swing = s;
            FrontRoomsDoorRig.TickAllForTools(1f);
            var home = rig.transform.TransformPoint(new Vector3(0f, 1f, FrontRoomsDoorRig.LatchEndZ));
            rig.Jolt(FrontRoomsShotTimings.Unlock.LeafShiftMetres);
            FrontRoomsDoorRig.TickAllForTools(FrontRoomsShotTimings.Unlock.BoltJolt * .2f);
            var moved = Vector3.Dot(rig.transform.TransformPoint(new Vector3(0f, 1f, FrontRoomsDoorRig.LatchEndZ)) - home, right);
            Check(Mathf.Sign(moved) == -s && Mathf.Abs(Mathf.Abs(moved) - FrontRoomsShotTimings.Unlock.LeafShiftMetres) < .0003f,
                tag + "Jolt(1.5 mm) at swing " + s + ": latch end " + (moved * 1000f).ToString("0.00") + " mm along rootX (toward -swing)");
            FrontRoomsDoorRig.TickAllForTools(.5f);
            var back = (rig.transform.TransformPoint(new Vector3(0f, 1f, FrontRoomsDoorRig.LatchEndZ)) - home).magnitude;
            Check(back < 1e-5f, tag + "the jolt springs home (" + (back * 1000f).ToString("0.000") + " mm)");
        }
        door.swing = keepSwing;

        // The collider follow: a crooked / jolted collider moves the kit leaf the same way.
        var leafT = door.leaf.transform;
        var keepPos = leafT.localPosition;
        var keepRot = leafT.localRotation;
        leafT.localPosition = keepPos + new Vector3(.003f, -.002f, .001f);
        leafT.localRotation = Quaternion.Euler(-3f, .4f, 0f);
        FrontRoomsDoorRig.TickAllForTools(0f);
        var expectRot = leafT.localRotation;
        var expectPos = leafT.localPosition - expectRot * keepPos;
        Check((rig.transform.localPosition - expectPos).magnitude < 1e-6f && Quaternion.Angle(rig.transform.localRotation, expectRot) < .01f, tag + "the rig follows the collider (collider o home^-1)");
        leafT.localPosition = keepPos;
        leafT.localRotation = keepRot;
        FrontRoomsDoorRig.TickAllForTools(0f);
        Check(rig.transform.localPosition.magnitude < 1e-6f && Quaternion.Angle(rig.transform.localRotation, Quaternion.identity) < .01f, tag + "home again");

        // No GC per Tick, beats included.
        rig.BeatRattle();
        for (var i = 0; i < 20; i++) FrontRoomsDoorRig.TickAllForTools(1f / 60f);
        rig.BeatOpen();
        rig.BeatLatchClick();
        rig.BeatWobble();
        var before = AllocNow();
        for (var i = 0; i < 240; i++) FrontRoomsDoorRig.TickAllForTools(1f / 60f);
        var bytes = AllocNow() - before;
        Check(bytes == 0, tag + "240 ticks with beats allocate " + bytes + " bytes (" + CounterName + ")");

        // The strike comes off on a break and lands on the floor.
        rig.DetachStrike(door.position + right);
        for (var i = 0; i < 40; i++) FrontRoomsDoorRig.TickAllForTools(1f / 60f);
        var st = rig.StrikeForTests;
        Check(rig.StrikeDetached && st != null && st.position.y - door.hinge.position.y < .05f && Vector3.Dot(st.position - door.position, right) < 0f,
            tag + "strike detached, on the floor away from the breaker (y " + (st != null ? (st.position.y - door.hinge.position.y).ToString("0.000") : "-") + ")");

        // Cost at LOD0 (T10 estimate): renderers and triangles of LOD0 of every LODGroup under the frame and the rig.
        int renderers = 0, tris = 0, lodRenderers = 0;
        var groups = new List<LODGroup>(rig.GetComponentsInChildren<LODGroup>(true));
        if (rig.Frame != null) groups.AddRange(rig.Frame.GetComponentsInChildren<LODGroup>(true));
        var counted = new HashSet<Renderer>();
        foreach (var g in groups)
        {
            var lods = g.GetLODs();
            if (lods.Length == 0) continue;
            foreach (var r in lods[0].renderers) if (r != null && counted.Add(r)) { lodRenderers++; tris += Tris(r); }
            foreach (var lod in lods) foreach (var r in lod.renderers) if (r != null) counted.Add(r);
        }
        var all = new List<Renderer>(rig.GetComponentsInChildren<Renderer>(true));
        if (rig.Frame != null) all.AddRange(rig.Frame.GetComponentsInChildren<Renderer>(true));
        foreach (var r in all) if (!counted.Contains(r)) { renderers++; tris += Tris(r); }
        renderers += lodRenderers;
        if (cost.Length > 0) cost.Append(", ");
        cost.Append("{\"model\": \"").Append(locked ? "LOCKED" : "FREE").Append("\", \"member\": \"").Append(rig.Member).Append("\", \"lod0Renderers\": ").Append(renderers).Append(", \"lod0Tris\": ").Append(tris).Append(", \"lodGroups\": ").Append(groups.Count).Append("}");
        Check(renderers > 0 && tris > 0, tag + "LOD0 cost: " + renderers + " renderers, " + tris + " triangles, " + groups.Count + " LOD groups");

        FixOneChecks(world, door, rig, locked, tag);
    }

    // ---------------------------------------------------------------- fix 1 (verify round 1)

    static void FixOneChecks(FrontRoomsMapWorld world, FrontRoomsMapWorld.Door door, FrontRoomsDoorRig rig, bool locked, string tag)
    {
        // Satin hardware chrome (VL323): no part keeps the polished Prop_Chrome once Prop_ChromeSatin exists.
        var satinExists = FrontRoomsSurfaces.TryGet(FrontRoomsInteractableKit.HardwareChromeTo) != null;
        int polished = 0, satin = 0;
        var rends = new List<Renderer>(rig.GetComponentsInChildren<Renderer>(true));
        if (rig.Frame != null) rends.AddRange(rig.Frame.GetComponentsInChildren<Renderer>(true));
        foreach (var r in rends)
            foreach (var m in r.sharedMaterials)
            {
                if (m == null) continue;
                if (m.name == FrontRoomsInteractableKit.HardwareChromeFrom) polished++;
                if (m.name == FrontRoomsInteractableKit.HardwareChromeTo) satin++;
            }
        Check(satinExists && polished == 0, tag + "hardware chrome is satin: " + satin + " satin slots, " + polished + " polished (Prop_ChromeSatin " + (satinExists ? "present" : "MISSING") + ")");

        // The visual pivot (VL319): the leaf turns about its sidecar pivot_axis on the centre plane, 36 mm in from the map's
        // hinge point; shut, nothing moves; open, that axis stays put while the map's hinge turns about its own point.
        var pz = rig.VisualPivotZ;
        Check(pz > .02f && pz < .06f, tag + "visual pivot " + (pz * 1000f).ToString("0.0") + " mm into the opening (leaf sidecar pivot_axis)");
        var keepHinge = door.hinge.localRotation;
        FrontRoomsDoorRig.TickAllForTools(0f);
        var axisShut = rig.transform.TransformPoint(new Vector3(0f, 1f, pz));
        foreach (var deg in new[] { 45f, -95f })
        {
            door.hinge.localRotation = door.closed * Quaternion.Euler(0f, deg, 0f);
            FrontRoomsDoorRig.TickAllForTools(0f);
            var axisOpen = rig.transform.TransformPoint(new Vector3(0f, 1f, pz));
            Check((axisOpen - axisShut).magnitude < 1e-3f, tag + "open " + deg + " deg: the kit leaf turns about its pivot axis (" + ((axisOpen - axisShut).magnitude * 1000f).ToString("0.00") + " mm)");
        }
        door.hinge.localRotation = keepHinge;
        FrontRoomsDoorRig.TickAllForTools(0f);
        Check(rig.transform.localPosition.magnitude < 1e-6f, tag + "shut again: the rig is home (" + (rig.transform.localPosition.magnitude * 1000f).ToString("0.000") + " mm)");

        // The idle early-out: an unmoved, beat-free rig does not move on a tick.
        var before = rig.transform.localPosition;
        for (var i = 0; i < 5; i++) FrontRoomsDoorRig.TickAllForTools(1f / 60f);
        Check(rig.transform.localPosition == before, tag + "an idle rig stays put");

        // Printed type (DECISIONS #33; held until the graphic-visual line's OK): both plates carry the same floor token, and
        // follow the floor.
        var p0 = rig.PlateOf(0);
        var p1 = rig.PlateOf(1);
        if (p0 != null && p1 != null)
        {
            Check(rig.PlateIdOf(0).Token == rig.PlateIdOf(1).Token && rig.PlateIdOf(0).Token == "00", tag + "both plates print the floor token (" + rig.PlateIdOf(0).Token + " / " + rig.PlateIdOf(1).Token + ")");
            FrontRoomsKeyIdentity.SetFloor(4);
            Check(rig.PlateIdOf(0).Token == "B3" && rig.PlateIdOf(1).Token == "B3", tag + "floor 4: both plates follow to B3");
            FrontRoomsKeyIdentity.SetFloor(1);
        }
        else if (locked) Check(false, tag + "the LOCKED model has number plates on both faces");

        // The key over the knob (VL321): in the lock, the tag never passes through the knob, at the plug's 0 and 90 deg.
        if (locked)
        {
            var key = FrontRoomsInteractableKit.DressShotKey(null, FrontRoomsKeyIdentity.Stub(new GridCoord(0, 0)));
            if (key == null) Check(false, tag + "DressShotKey built a key");
            else
            {
                try
                {
                    var right = door.hinge.rotation * Vector3.right;
                    foreach (var face in new[] { DoorFace.S, DoorFace.P })
                    foreach (var turn in new[] { 0f, 90f })
                    {
                        var probe = door.position + right * (face == DoorFace.S ? 1f : -1f);
                        var lp = world.LockPose(door, probe);
                        key.PoseInLock(new Pose(lp.position, lp.rotation * Quaternion.AngleAxis(turn * (face == DoorFace.S ? 1f : -1f), Vector3.forward)));
                        var inside = TagInsideKnob(key, rig, face, out var worst);
                        Check(inside == 0, tag + "key in lock " + face + " plug " + turn + " deg: tag tilt " + key.TagTiltDegrees.ToString("0.0") + " deg, " + inside + " tag vertices inside the knob (deepest " + (worst * 1000f).ToString("0.0") + " mm)");
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(key.Root.gameObject); }
            }
        }

        // The lock-rule audit (verify r1 E3a): flip the rule, the door is dressed again with the other model; flip back.
        var keepOverride = FrontRoomsInteractableKit.DoorLockedOverride;
        try
        {
            FrontRoomsInteractableKit.DoorLockedOverride = d => d == door ? !locked : (bool?)null;
            var n = FrontRoomsInteractableKit.AuditLockModels();
            var now = door.rig as FrontRoomsDoorRig;
            Check(n >= 1 && now != null && now != rig && now.LockedModel == !locked, tag + "lock rule flipped: audit re-dressed " + n + ", model now " + (now != null && now.LockedModel ? "LOCKED" : "FREE"));
            FrontRoomsInteractableKit.DoorLockedOverride = d => d == door ? locked : (bool?)null;
            FrontRoomsInteractableKit.AuditLockModels();
            now = door.rig as FrontRoomsDoorRig;
            Check(now != null && now.LockedModel == locked && now.transform.parent == door.hinge, tag + "flipped back: model " + (now != null && now.LockedModel ? "LOCKED" : "FREE") + " again, under the hinge");
        }
        finally { FrontRoomsInteractableKit.DoorLockedOverride = keepOverride; }
    }

    // Tag mesh vertices (LOD0) inside the knob of a face: a cylinder of the knob's radius about the axis through the knob
    // pivot (KnobTopBelowKeyhole + radius under the keyhole), from the keyhole plane out to the knob's face.
    static int TagInsideKnob(FrontRoomsKeyAssembly key, FrontRoomsDoorRig rig, DoorFace face, out float worst)
    {
        worst = 0f;
        const float radius = .027f - .0005f;
        var kh = rig.Keyhole(face);
        var outDir = -rig.KeyholeDir(face);
        var up = rig.KeyholeUp(face);
        var centre = kh - up * (FrontRoomsKeyAssembly.KnobTopBelowKeyhole + .027f);
        var n = 0;
        foreach (var f in key.TagPart.GetComponentsInChildren<MeshFilter>(true))
        {
            if (f.sharedMesh == null || f.name.EndsWith("_LOD1") || f.name.EndsWith("_LOD2")) continue;
            var verts = f.sharedMesh.vertices;
            var m = f.transform.localToWorldMatrix;
            foreach (var v0 in verts)
            {
                var v = m.MultiplyPoint3x4(v0);
                var d = v - centre;
                var along = Vector3.Dot(d, outDir);
                if (along < 0f || along > FrontRoomsKeyAssembly.KnobFaceOut) continue;
                var radial = (d - outDir * along).magnitude;
                if (radial < radius) { n++; worst = Mathf.Max(worst, radius - radial); }
            }
        }
        return n;
    }

    static int Tris(Renderer r)
    {
        var f = r.GetComponent<MeshFilter>();
        if (f == null || f.sharedMesh == null) return 0;
        var m = f.sharedMesh;
        var n = 0;
        for (var i = 0; i < m.subMeshCount; i++) n += (int)m.GetIndexCount(i) / 3;
        return n;
    }

    static bool BadName(string n) => n.StartsWith("Door hinge") || n == "Light" || n.StartsWith("fluorescent light");

    // Fix 2 (verify r2 look 2, engineering E7): no mirror surface left on the dressed door (the leaf's kick / push plates and the
    // hardware are satin, Prop_ChromeSatin); and GlassRT's bounded kit-part walk (stops KitPartDepth hops up or at the chunk
    // root) tells every renderer under the rig and the frame exactly as the unbounded label walk does.
    static void SurfaceAndTraceChecks(FrontRoomsMapWorld.Door door, FrontRoomsDoorRig rig, string tag)
    {
        var mirror = 0; var satin = 0;
        var all = new List<Renderer>(rig.GetComponentsInChildren<Renderer>(true));
        if (rig.Frame != null) all.AddRange(rig.Frame.GetComponentsInChildren<Renderer>(true));
        foreach (var r in rig.GetComponentsInChildren<Renderer>(true))
            foreach (var m in r.sharedMaterials)
            {
                if (m == null) continue;
                if (m.name == "Prop_Aluminium" || m.name == "Prop_Chrome") mirror++;
                else if (m.name == "Prop_ChromeSatin" || m.name == "Prop_StainlessSatin") satin++;
            }
        var locked = rig.LockedModel;
        Check(mirror == 0 && (satin > 0 || !locked), tag + "leaf and hardware: no Prop_Aluminium / Prop_Chrome slot left (" + mirror + "), Prop_ChromeSatin / Prop_StainlessSatin on " + satin + " slots"
            + (locked ? "" : " (the L0 free door is brass)"));
        var type = Type.GetType("FrontRoomsGlassRTSystem, Assembly-CSharp");
        var method = type != null ? type.GetMethod("IsKitSmallPart", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static) : null;
        if (method == null) { Check(false, tag + "GlassRT IsKitSmallPart(Transform, Transform) found by reflection"); return; }
        var chunkRoot = door.hinge.parent;
        var small = 0; var miss = 0; var deepest = 0; var firstMiss = "";
        foreach (var r in all)
        {
            var labelled = false; var hops = 0;
            for (var t = r.transform; t != null && t != chunkRoot; t = t.parent, hops++)
            {
                var n = t.name;
                if (n.StartsWith("Lock ", StringComparison.Ordinal) || n.StartsWith("Door plate", StringComparison.Ordinal)
                    || n.StartsWith("Door sign", StringComparison.Ordinal) || n == "Key ring" || n == "Key tag") { labelled = true; deepest = Mathf.Max(deepest, hops); break; }
                if (n == FrontRoomsInteractableKit.LeafRigLabel || n == FrontRoomsInteractableKit.DoorFrameLabel || n.StartsWith("Door hinge", StringComparison.Ordinal)) break;
            }
            var got = (bool)method.Invoke(null, new object[] { r.transform, chunkRoot });
            if (labelled) small++;
            if (got != labelled) { miss++; if (firstMiss.Length == 0) firstMiss = "; first " + r.name; }
        }
        Check(miss == 0 && small > 0, tag + "GlassRT bounded kit-part walk = the label walk on " + all.Count + " renderers (" + small + " small parts, deepest label "
            + deepest + " hops up, limit 4; " + miss + " differ" + firstMiss + ")");
    }
}
