using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Hunter B "Night Shift" rig checks (research/hunter/production_b/10_plan.md section 6, T5 and T6). Edit mode, headless,
/// no menu item and no InitializeOnLoad hook:
///   Unity -batchmode -projectPath <project> -buildTarget OSXUniversal -executeMethod FrontRoomsRelayRigTests.RunBatch -quit [-hbTestOut dir]
/// Default output: <project>/Verification/hunter_b/tests (tests.json, log.txt, lens_series.csv).
///   A1  the public API and every serialised field of FrontRoomsRelayRig as at base 64b9de9c (signatures by reflection)
///   A2  the production body binds: 3 skinned LODs on 36 bones, the Creature_* materials, the last LOD never culled,
///       the 16 primitives kept in the hierarchy and hidden (not destroyed)
///   A3  fallback: useProductionBody off -> no body, the primitives draw again
///   P1  pose envelope (BakeMesh of LOD0): low / std / tall x neck long / face +0.3: the ceiling pin (top within
///       0.05 m under the ceiling), PressesCeiling, the lens height bands
///   D1  door sequence: DoorSqueeze 0 -> 1 -> 0 while walking: door weight 1, phase approaching -> leaving, back to 0
///   C1  the catch at 0.7 m: the lens >= 0.30 m and every LOD0 vertex >= 0.15 m from the camera
///   L1  photosafety: a hostile 60 s state script: <= 3 transitions (>= 10 % of full) in any sliding 1 s window,
///       for the lens and for the light
///   L2  Reduce Flashing: every swing of 10 % or more lasts >= 0.5 s
///   L3  no radar: the same script with the camera at 2 m and at 30 m gives identical lens and light series
///   L4  steady levels: Listen 0.06 (light off), Walk 0.25 (light on, desktop), Run 1.0 (light on)
///   E1  CeilingBrush: <= 2/s, >= 0.5 s apart, only while pressed and moving (none in Tall, none standing)
///   E2  Squeeze without colliders (the fallback rule): at the start and at each +0.3 of DoorSqueeze
///   E3  Step: alternating feet, at the planted boot, cadence = ground speed / step length
///   G1  no allocation per Tick after the first frames
///   G2  rig tick cost (wall time; compare only relatively when the machine is loaded)
/// Under FRONTROOMS_RELAY_V2 (relay_rig_v2, production_b/20_build.md); without it one check, V0, that none of this compiled:
///   H1  the walk head: the hood on LookPoint, nothing below the neck moves for it
///   H2  the neck's limit: a target right round the body; the map's limit; the top speed
///   H3  no pop: every state change in one run, a continuous head and hood
///   H4  a doorway: the hood comes forward on the door keys' line
///   S1  the square-up: reached in 0.2 s, the hood up, the glove off the tile, the beat over 1.7 s, body turned to the player
///   S2  the square-up inside every ceiling, both necks; the 0.7 m catch's clearance from it
///   S3  the freeze: feet planted mid-stride, a turn stepped
///   S4  the "almost" stop: a share of the pose, the glove where it was, the head tracked, not latched
///   S5  the lens through holds: by state only, 2 m and 30 m alike, a hold changes nothing in it
///   S6  plain Listen does not turn the lens on a viewer it has not noticed (behind it, or through a lite: A42); a hold does
///   G3  no allocation with holds and a target every frame
/// </summary>
public static class FrontRoomsRelayRigTests
{
    const float Dt = 1f / 60f;
    static readonly CultureInfo IC = CultureInfo.InvariantCulture;
    static readonly StringBuilder Log = new StringBuilder();
    sealed class Result { public string name; public bool pass; public string detail; }
    static readonly List<Result> Results = new List<Result>();
    static string outDir;
    static readonly FieldInfo FaceField = typeof(FrontRoomsRelayRig).GetField("faceLift", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly FieldInfo UseField = typeof(FrontRoomsRelayRig).GetField("useProductionBody", BindingFlags.NonPublic | BindingFlags.Instance);

    public static void RunBatch()
    {
        var args = Environment.GetCommandLineArgs();
        outDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "hunter_b", "tests");
        for (var i = 0; i < args.Length - 1; i++) if (args[i] == "-hbTestOut") outDir = args[i + 1];
        Directory.CreateDirectory(outDir);
        Results.Clear();
        Log.Length = 0;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var reducedBefore = FrontRoomsSettings.ReduceFlashing;
        try
        {
            Run("A1 public API and serialised fields = base 64b9de9c", A1Api);
            Run("A2 the production body binds", A2Bind);
            Run("A3 fallback: useProductionBody off", A3Fallback);
            Run("P1 pose envelope: ceiling pin, PressesCeiling, lens heights", P1Envelope);
            Run("D1 door sequence from DoorSqueeze", D1Door);
            Run("C1 the catch at 0.7 m: camera clearance", C1Catch);
            Run("L1 photosafety: <= 3 transitions per sliding 1 s", () => L1Photosafe(false));
            Run("L2 Reduce Flashing: swings last >= 0.5 s", () => L1Photosafe(true));
            Run("L3 no radar: camera at 2 m vs 30 m", L3NoRadar);
            Run("L4 steady lens levels and the light", L4Levels);
            Run("E1 CeilingBrush rate and gating", E1Brush);
            Run("E2 Squeeze: start and every +0.3 (fallback rule)", E2Squeeze);
            Run("E3 Step: alternating, at the boot, cadence from ground speed", E3Step);
            Run("G1 no allocation per Tick", G1Alloc);
            Run("G2 rig tick cost", G2Cost);
#if FRONTROOMS_RELAY_V2
            Run("H1 walk head: hood on LookPoint, body on its heading", H1WalkHead);
            Run("H2 the neck's limit, a target all round", H2Limit);
            Run("H3 no pop across every state change", H3NoPop);
            Run("H4 a doorway: the hood forward on the keys' line", H4Door);
            Run("S1 square-up: pose, glove, beat, the body's turn", S1SquareUp);
            Run("S2 square-up inside every ceiling, the clearance", S2Envelope);
            Run("S3 the freeze: planted mid-stride, a turn stepped", S3Freeze);
            Run("S4 the almost stop", S4Almost);
            Run("S5 the lens through holds: state only", S5Lens);
            Run("S6 no reveal at a viewer it has not noticed", S6Reveal);
            Run("G3 no allocation with holds", G3Alloc);
#else
            Run("V0 define off: no hold API compiled", V0Off);
#endif
        }
        finally
        {
            SetReduced(reducedBefore);
            FrontRoomsRelayBody.ViewerOverride = null;
            FrontRoomsRelayBody.PhysicsInEditMode = false;
        }
        var json = new StringBuilder("{\n \"results\": [\n");
        for (var i = 0; i < Results.Count; i++)
            json.Append("  {\"name\": ").Append(Q(Results[i].name)).Append(", \"pass\": ").Append(Results[i].pass ? "true" : "false")
                .Append(", \"detail\": ").Append(Q(Results[i].detail)).Append(i < Results.Count - 1 ? "},\n" : "}\n");
        json.Append(" ],\n \"passed\": ").Append(Results.Count(r => r.pass)).Append(",\n \"failed\": ").Append(Results.Count(r => !r.pass)).Append("\n}\n");
        File.WriteAllText(Path.Combine(outDir, "tests.json"), json.ToString());
        File.WriteAllText(Path.Combine(outDir, "log.txt"), Log.ToString());
        Debug.Log("[RelayRigTests] DONE " + Results.Count(r => r.pass) + "/" + Results.Count + " passed -> " + outDir);
    }

    static void Run(string name, Func<string> test)
    {
        string detail;
        bool pass;
        try
        {
            detail = test();
            pass = !detail.StartsWith("FAIL", StringComparison.Ordinal);
        }
        catch (Exception e)
        {
            pass = false;
            detail = "FAIL exception " + e.GetType().Name + ": " + e.Message + " " + e.StackTrace;
        }
        Results.Add(new Result { name = name, pass = pass, detail = detail });
        Log.AppendLine((pass ? "PASS " : "FAIL ") + name + " :: " + detail);
        Debug.Log("[RelayRigTests] " + (pass ? "PASS " : "FAIL ") + name + " :: " + detail);
    }

    static string Q(string s) => "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ") + "\"";
    static string F(float v) => v.ToString("0.000", IC);

    static void SetReduced(bool on)
    {
        var m = typeof(FrontRoomsSettings).GetMethod("SetReduceFlashing", BindingFlags.Public | BindingFlags.Static);
        if (m != null) m.Invoke(null, new object[] { on });
    }

    static FrontRoomsRelayRig NewRig(string name = "Hunter")
    {
        var go = new GameObject(name);
        var rig = go.AddComponent<FrontRoomsRelayRig>();
        rig.Configure(FrontRoomsSurfaces.Lit("Relay / body", new Color32(0x2B, 0x29, 0x28, 255)), FrontRoomsSurfaces.Lit("Relay / blank head", new Color32(0xD8, 0xD4, 0xC8, 255)), FrontRoomsSurfaces.Lit("Relay / detail", new Color32(0xA9, 0x9E, 0x78, 255)));
        return rig;
    }

    static FrontRoomsRelayBody BodyOf(FrontRoomsRelayRig rig) => rig.GetComponentInChildren<FrontRoomsRelayBody>(true);

    static void Kill(FrontRoomsRelayRig rig) { if (rig != null) UnityEngine.Object.DestroyImmediate(rig.gameObject); }

    // ------------------------------------------------------------------ A
    static string A1Api()
    {
        var t = typeof(FrontRoomsRelayRig);
        var missing = new List<string>();
        void Need(bool ok, string what) { if (!ok) missing.Add(what); }
        Need(t.IsSealed && t.IsPublic && typeof(MonoBehaviour).IsAssignableFrom(t), "public sealed MonoBehaviour");
        Need(t.GetCustomAttributes(typeof(ExecuteAlways), false).Length == 1, "[ExecuteAlways]");
        var ms = typeof(FrontRoomsRelayRig.MotionState);
        Need(string.Join(",", Enum.GetNames(ms)) == "IdleListen,Walk,Run,BreakDoor,Stagger,Search", "MotionState members in order");
        var pub = BindingFlags.Public | BindingFlags.Instance;
        void Ev(string n, Type type) { var e = t.GetEvent(n, pub); Need(e != null && e.EventHandlerType == type, "event " + n); }
        Ev("Step", typeof(Action<int, Vector3>));
        Ev("CeilingBrush", typeof(Action<Vector3>));
        Ev("Squeeze", typeof(Action<Vector3, float>));
        void Prop(string n, Type type, bool set) { var p = t.GetProperty(n, pub); Need(p != null && p.PropertyType == type && p.CanRead && (!set || (p.SetMethod != null && p.SetMethod.IsPublic)), "property " + n); }
        Prop("RaisesSteps", typeof(bool), false);
        Prop("CeilingHeight", typeof(float), true);
        Prop("DoorSqueeze", typeof(float), true);
        Prop("PressesCeiling", typeof(bool), true);
        foreach (var n in new[] { "Body", "Head", "ArmLeft", "ArmRight", "LegLeft", "LegRight" }) Prop(n, typeof(Transform), false);
        Prop("State", ms, false);
        void Meth(string n, Type ret, params Type[] ps) { var m = t.GetMethod(n, pub, null, ps, null); Need(m != null && m.ReturnType == ret, "method " + n + "(" + string.Join(",", ps.Select(p => p.Name)) + ")"); }
        Meth("SetListenTarget", typeof(void), typeof(Vector3?));
        Meth("DoorBlow", typeof(void), typeof(int), typeof(int));
        Meth("Configure", typeof(void), typeof(Material), typeof(Material), typeof(Material));
        Meth("EnsureRig", typeof(void));
        Meth("TickAnimation", typeof(void), typeof(float), ms, typeof(bool), typeof(float));
        var priv = BindingFlags.NonPublic | BindingFlags.Instance;
        void Field(string n, Type type) { var f = t.GetField(n, priv); Need(f != null && f.FieldType == type && f.GetCustomAttributes(typeof(SerializeField), false).Length == 1, "serialised field " + n); }
        foreach (var n in new[] { "bodyMaterial", "headMaterial", "detailMaterial" }) Field(n, typeof(Material));
        foreach (var n in new[] { "rootBone", "pelvis", "spine", "chest", "neck", "head", "armL", "forearmL", "handL", "armR", "forearmR", "handR", "thighL", "shinL", "footL", "thighR", "shinR", "footR" }) Field(n, typeof(Transform));
        Field("previewState", ms);
        Field("previewSpeed", typeof(float));
        Field("bodyScale", typeof(float));
        var rig = NewRig();
        Need(Mathf.Approximately(rig.CeilingHeight, 2.9f), "CeilingHeight default 2.9");
        Need(rig.RaisesSteps, "RaisesSteps true");
        Kill(rig);
        return missing.Count == 0 ? "all 3 events, 11 properties, 5 methods, 24 serialised fields and the enum match the base" : "FAIL missing/changed: " + string.Join("; ", missing);
    }

    static string A2Bind()
    {
        var rig = NewRig();
        var body = BodyOf(rig);
        if (body == null || !body.Bound) { Kill(rig); return "FAIL no production body (Resources/Creatures/Relay_NightShift + _Poses.json)"; }
        var smr = body.Renderers;
        var issues = new List<string>();
        if (smr.Length != 3) issues.Add(smr.Length + " skinned renderers");
        foreach (var r in smr)
        {
            if (r.bones.Length != 36) issues.Add(r.name + " " + r.bones.Length + " bones");
            var mats = r.sharedMaterials;
            if (mats.Length != 3 || mats[0] == null || mats[0].name != "Creature_TwillSpruce" || mats[1] == null || mats[1].name != "Creature_LensOpal" || mats[2] == null || mats[2].name != "Creature_TapeSilver")
                issues.Add(r.name + " materials " + string.Join(",", mats.Select(m => m != null ? m.name : "null")));
        }
        var lod = body.GetComponentInChildren<LODGroup>(true);
        if (lod == null) issues.Add("no LODGroup");
        else
        {
            var lods = lod.GetLODs();
            if (lods.Length != 3 || lods[lods.Length - 1].screenRelativeTransitionHeight != 0f) issues.Add("LODs " + lods.Length + " last " + (lods.Length > 0 ? F(lods[lods.Length - 1].screenRelativeTransitionHeight) : "-"));
        }
        var prims = rig.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.GetComponentInParent<FrontRoomsRelayBody>() == null).ToArray();
        if (prims.Length != 16) issues.Add(prims.Length + " primitives (want 16 kept)");
        if (prims.Any(r => !r.forceRenderingOff)) issues.Add("a primitive still draws");
        var tris = smr.Where(r => r.name.EndsWith("_LOD0", StringComparison.Ordinal)).Select(r => r.sharedMesh.triangles.Length / 3).FirstOrDefault();
        if (tris > 12000) issues.Add("LOD0 " + tris + " tris > 12k");
        var detail = "3 LODs (LOD0 " + tris + " tris), 36 bones, materials ok, last LOD 0 (never culled), 16 primitives kept and hidden, body scale " + F(body.ModelScale);
        Kill(rig);
        return issues.Count == 0 ? detail : "FAIL " + string.Join("; ", issues);
    }

    static string A3Fallback()
    {
        var rig = NewRig();
        UseField.SetValue(rig, false);
        rig.EnsureRig();
        rig.TickAnimation(Dt, FrontRoomsRelayRig.MotionState.Walk, true, 1f);
        var body = BodyOf(rig);
        var prims = rig.GetComponentsInChildren<MeshRenderer>(true);
        var drawing = prims.Count(r => !r.forceRenderingOff);
        UseField.SetValue(rig, true);
        rig.EnsureRig();
        var back = BodyOf(rig) != null;
        Kill(rig);
        if (body != null) return "FAIL the body stayed with useProductionBody off";
        if (drawing != 16) return "FAIL " + drawing + " of 16 primitives draw";
        return "off: no body, 16 primitives draw; on again: body back " + back;
    }

    // ------------------------------------------------------------------ P / D / C
    static float BakeTop(FrontRoomsRelayBody body, out float minDistTo, Vector3 point)
    {
        var mesh = new Mesh();
        var top = float.MinValue;
        minDistTo = float.MaxValue;
        foreach (var r in body.Renderers)
        {
            if (!r.name.EndsWith("_LOD0", StringComparison.Ordinal)) continue;
            r.BakeMesh(mesh, true);
            var m = r.transform.localToWorldMatrix;
            foreach (var v in mesh.vertices)
            {
                var w = m.MultiplyPoint3x4(v);
                top = Mathf.Max(top, w.y);
                minDistTo = Mathf.Min(minDistTo, (w - point).magnitude);
            }
        }
        UnityEngine.Object.DestroyImmediate(mesh);
        return top;
    }

    static void Settle(FrontRoomsRelayRig rig, float h, float face, FrontRoomsRelayRig.MotionState s, float seconds = 3f)
    {
        FaceField.SetValue(rig, face);
        rig.CeilingHeight = h;
        rig.DoorSqueeze = 0f;
        var n = Mathf.RoundToInt(seconds / Dt);
        for (var i = 0; i < n; i++) rig.TickAnimation(Dt, s, false, 1f);
    }

    static string P1Envelope()
    {
        var rig = NewRig();
        var body = BodyOf(rig);
        if (body == null) { Kill(rig); return "FAIL no body"; }
        var lines = new List<string>();
        var fails = new List<string>();
        // lens height bands: plan section 1.3 (module values) minus the pin (<= 0.12 m)
        var cases = new[] { new[] { 2.4f, 0f, 1.20f, 1.55f }, new[] { 2.4f, 1f, 1.50f, 1.85f }, new[] { 2.9f, 0f, 1.62f, 1.90f }, new[] { 2.9f, 1f, 1.92f, 2.20f }, new[] { 5.4f, 0f, 2.75f, 3.05f } };
        foreach (var c in cases)
        {
            Settle(rig, c[0], c[1], FrontRoomsRelayRig.MotionState.Walk);
            var top = BakeTop(body, out _, Vector3.zero);
            var lens = body.LensWorld.y;
            var pinned = c[0] < 4f;
            var ok = true;
            if (pinned && (top > c[0] + .005f || top < c[0] - .08f)) { ok = false; fails.Add("h " + c[0] + " top " + F(top)); }
            if (pinned != rig.PressesCeiling) { ok = false; fails.Add("h " + c[0] + " PressesCeiling " + rig.PressesCeiling); }
            if (!pinned && (top < 3.10f || top > 3.32f)) { ok = false; fails.Add("tall top " + F(top)); }
            if (lens < c[2] || lens > c[3]) { ok = false; fails.Add("h " + c[0] + " face " + c[1] + " lens " + F(lens)); }
            lines.Add("h " + F(c[0]) + " face " + F(c[1]) + ": top " + F(top) + " (markers " + F(body.Top) + ", pin " + F(body.PinOffset) + "), presses " + rig.PressesCeiling + ", lens " + F(lens) + (ok ? "" : " X"));
        }
        Kill(rig);
        return (fails.Count == 0 ? "" : "FAIL " + string.Join("; ", fails) + " | ") + string.Join(" | ", lines);
    }

    static string D1Door()
    {
        var rig = NewRig();
        var body = BodyOf(rig);
        if (body == null) { Kill(rig); return "FAIL no body"; }
        Settle(rig, 2.9f, 0f, FrontRoomsRelayRig.MotionState.Walk, 1f);
        const float v = 2.6f;
        var s = -1.5f;
        var maxW = 0f;
        var sawApproach = false; var sawLeave = false; var orderOk = true;
        var lastS = -10f;
        while (s < 1.6f)
        {
            s += v * Dt;
            rig.transform.position += rig.transform.forward * v * Dt;
            rig.DoorSqueeze = Mathf.Clamp01(1f - Mathf.Abs(s) / .6f);
            rig.TickAnimation(Dt, FrontRoomsRelayRig.MotionState.Walk, true, 1f);
            maxW = Mathf.Max(maxW, body.DoorWeight);
            if (body.DoorWeight > .5f)
            {
                if (body.DoorPhaseS < 0f) { sawApproach = true; if (sawLeave) orderOk = false; }
                else sawLeave = true;
                if (body.DoorPhaseS < lastS - .05f) orderOk = false;
                lastS = body.DoorPhaseS;
            }
        }
        for (var i = 0; i < 60; i++) { rig.transform.position += rig.transform.forward * v * Dt; rig.DoorSqueeze = 0f; rig.TickAnimation(Dt, FrontRoomsRelayRig.MotionState.Walk, true, 1f); }
        var after = body.DoorWeight;
        Kill(rig);
        var detail = "max door weight " + F(maxW) + ", approaching then leaving " + (sawApproach && sawLeave && orderOk) + ", weight after " + F(after);
        return maxW > .95f && sawApproach && sawLeave && orderOk && after < .05f ? detail : "FAIL " + detail;
    }

    static string C1Catch()
    {
        var lines = new List<string>();
        var fails = new List<string>();
        foreach (var face in new[] { 0f, 1f })
            foreach (var dist in new[] { .7f, 1.3f })
            {
                var rig = NewRig();
                var body = BodyOf(rig);
                var cam = new GameObject("viewer").transform;
                FrontRoomsRelayBody.ViewerOverride = cam;
                rig.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                // the map's catch: the player's capsule centre `dist` m from the nav axis, eye 1.62 m
                cam.position = new Vector3(0f, 1.62f, dist);
                cam.rotation = Quaternion.LookRotation(-Vector3.forward);
                Settle(rig, 2.9f, face, FrontRoomsRelayRig.MotionState.Run, 2f);
                BakeTop(body, out var minDist, cam.position);
                var lensDist = Vector3.Distance(body.LensWorld, cam.position);
                var ok = lensDist >= .29f && minDist >= .145f;
                if (!ok) fails.Add("face " + face + " dist " + dist + ": lens " + F(lensDist) + " min vertex " + F(minDist));
                lines.Add("catch " + F(dist) + " face " + F(face) + ": lens " + F(lensDist) + " m, nearest vertex " + F(minDist) + " m, pull " + F(body.Clearance) + " m, lens y " + F(body.LensWorld.y));
                FrontRoomsRelayBody.ViewerOverride = null;
                UnityEngine.Object.DestroyImmediate(cam.gameObject);
                Kill(rig);
            }
        return (fails.Count == 0 ? "" : "FAIL " + string.Join("; ", fails) + " | ") + string.Join(" | ", lines);
    }

    // ------------------------------------------------------------------ L
    /// <summary>A hostile script: every state, rapid toggles, door blows, latched Search snaps.</summary>
    static FrontRoomsRelayRig.MotionState ScriptState(float t, out bool blow, out bool finalBlow)
    {
        blow = false; finalBlow = false;
        var A = FrontRoomsRelayRig.MotionState.IdleListen;
        if (t < 4f) return A;
        if (t < 8f) return FrontRoomsRelayRig.MotionState.Search;
        if (t < 12f) return FrontRoomsRelayRig.MotionState.Walk;
        if (t < 16f) return FrontRoomsRelayRig.MotionState.Run;
        if (t < 22f)
        {
            var k = Mathf.FloorToInt((t - 16f) / .9f);
            var phase = (t - 16f) - k * .9f;
            blow = phase < Dt * .99f;
            finalBlow = blow && k == 5;
            return FrontRoomsRelayRig.MotionState.BreakDoor;
        }
        if (t < 30f)
        {
            // rapid toggles: a state change every 0.15 s
            var k = Mathf.FloorToInt((t - 22f) / .15f);
            switch (k % 5) { case 0: return FrontRoomsRelayRig.MotionState.Run; case 1: return A; case 2: return FrontRoomsRelayRig.MotionState.Search; case 3: return FrontRoomsRelayRig.MotionState.Walk; default: return FrontRoomsRelayRig.MotionState.Stagger; }
        }
        if (t < 36f) return (Mathf.FloorToInt((t - 30f) / .4f) % 2) == 0 ? FrontRoomsRelayRig.MotionState.Run : A;
        if (t < 42f) return FrontRoomsRelayRig.MotionState.Stagger;
        if (t < 50f) return (Mathf.FloorToInt((t - 42f) / 1.1f) % 2) == 0 ? FrontRoomsRelayRig.MotionState.Search : FrontRoomsRelayRig.MotionState.Run;
        return FrontRoomsRelayRig.MotionState.Walk;
    }

    /// <summary>Drives a rig with the script; returns the lens and light series.</summary>
    static void Drive(FrontRoomsRelayRig rig, float seconds, List<float> lens, List<float> light, StringBuilder csv, string tag)
    {
        var body = BodyOf(rig);
        var n = Mathf.RoundToInt(seconds / Dt);
        for (var i = 0; i < n; i++)
        {
            var t = i * Dt;
            var s = ScriptState(t, out var blow, out var final);
            if (blow) rig.DoorBlow(final ? 5 : 0, 6);
            rig.SetListenTarget(new Vector3(Mathf.Sin(t * .7f) * 6f, 1.6f, 5f));
            rig.TickAnimation(Dt, s, s == FrontRoomsRelayRig.MotionState.Walk || s == FrontRoomsRelayRig.MotionState.Run, 1f);
            var li = body.LensLight != null && body.LensLight.enabled ? body.LensLight.intensity : 0f;
            lens.Add(body.LensLevel);
            light.Add(li);
            csv?.Append(tag).Append(',').Append(F(t)).Append(',').Append(s).Append(',').Append(body.LensLevel.ToString("0.0000", IC)).Append(',').Append(body.Lens.Envelope.ToString("0.0000", IC)).Append(',').Append(li.ToString("0.0000", IC)).Append('\n');
        }
    }

    /// <summary>Independent transition count: monotonic swings of >= threshold from the last turning point.</summary>
    static List<int> Transitions(List<float> y, float threshold, out List<float> durations)
    {
        var stamps = new List<int>();
        durations = new List<float>();
        var ext = y.Count > 0 ? y[0] : 0f;
        var extIndex = 0;
        var dir = 0;
        var counted = false;
        for (var i = 1; i < y.Count; i++)
        {
            var d = y[i] - y[i - 1];
            if (Mathf.Abs(d) < 1e-6f) continue;
            var nd = d > 0 ? 1 : -1;
            if (dir != 0 && nd != dir)
            {
                if (counted) durations.Add((i - 1 - extIndex) * Dt);
                ext = y[i - 1]; extIndex = i - 1; counted = false;
            }
            if (dir == 0) { ext = y[i - 1]; extIndex = i - 1; }
            dir = nd;
            if (!counted && Mathf.Abs(y[i] - ext) >= threshold) { counted = true; stamps.Add(i); }
        }
        if (counted) durations.Add((y.Count - 1 - extIndex) * Dt);
        return stamps;
    }

    static int MaxPerWindow(List<int> stamps)
    {
        var best = 0;
        var w = Mathf.RoundToInt(1f / Dt);
        for (var i = 0; i < stamps.Count; i++)
        {
            var c = 0;
            for (var j = i; j < stamps.Count && stamps[j] - stamps[i] < w; j++) c++;
            best = Mathf.Max(best, c);
        }
        return best;
    }

    static string L1Photosafe(bool reduced)
    {
        SetReduced(reduced);
        var rig = NewRig();
        var lens = new List<float>(); var light = new List<float>();
        var csv = new StringBuilder();
        var cam = new GameObject("viewer").transform;
        cam.position = new Vector3(0f, 1.62f, 6f);
        FrontRoomsRelayBody.ViewerOverride = cam;
        Drive(rig, 60f, lens, light, csv, reduced ? "reduced" : "normal");
        FrontRoomsRelayBody.ViewerOverride = null;
        UnityEngine.Object.DestroyImmediate(cam.gameObject);
        Kill(rig);
        SetReduced(false);
        var path = Path.Combine(outDir, "lens_series.csv");
        if (!reduced) File.WriteAllText(path, "mode,t,state,lens,envelope,light\n" + csv);
        else File.AppendAllText(path, csv.ToString());
        var tl = Transitions(lens, .1f, out var durL);
        var maxLight = light.Max();
        var lightNorm = light.Select(x => maxLight > 0f ? x / FrontRoomsRelayLens.FullLightIntensity : 0f).ToList();
        var tli = Transitions(lightNorm, .1f, out var durLi);
        var wl = MaxPerWindow(tl);
        var wli = MaxPerWindow(tli);
        var detail = "lens: " + tl.Count + " transitions in 60 s, max " + wl + " per 1 s window; light: " + tli.Count + ", max " + wli + " per window";
        if (reduced)
        {
            var shortest = durL.Count > 0 ? durL.Min() : 99f;
            var shortestLi = durLi.Count > 0 ? durLi.Min() : 99f;
            detail += "; shortest swing lens " + F(shortest) + " s, light " + F(shortestLi) + " s";
            return wl <= 3 && wli <= 3 && shortest >= .5f - Dt * 1.5f && shortestLi >= .5f - Dt * 1.5f ? detail : "FAIL " + detail;
        }
        return wl <= 3 && wli <= 3 ? detail : "FAIL " + detail;
    }

    static string L3NoRadar()
    {
        var series = new List<List<float>>();
        var lights = new List<List<float>>();
        foreach (var d in new[] { 2f, 30f })
        {
            var rig = NewRig();
            var cam = new GameObject("viewer").transform;
            cam.position = new Vector3(0f, 1.62f, d);
            cam.rotation = Quaternion.LookRotation(-Vector3.forward);
            FrontRoomsRelayBody.ViewerOverride = cam;
            var lens = new List<float>(); var light = new List<float>();
            Drive(rig, 40f, lens, light, null, null);
            series.Add(lens); lights.Add(light);
            FrontRoomsRelayBody.ViewerOverride = null;
            UnityEngine.Object.DestroyImmediate(cam.gameObject);
            Kill(rig);
        }
        var maxDiff = 0f; var maxDiffLight = 0f;
        for (var i = 0; i < series[0].Count; i++)
        {
            maxDiff = Mathf.Max(maxDiff, Mathf.Abs(series[0][i] - series[1][i]));
            maxDiffLight = Mathf.Max(maxDiffLight, Mathf.Abs(lights[0][i] - lights[1][i]));
        }
        var detail = series[0].Count + " frames: max |lens(2 m) - lens(30 m)| " + maxDiff.ToString("0.000000", IC) + ", light " + maxDiffLight.ToString("0.000000", IC);
        return maxDiff == 0f && maxDiffLight == 0f ? detail : "FAIL " + detail;
    }

    static string L4Levels()
    {
        var rig = NewRig();
        var body = BodyOf(rig);
        var lines = new List<string>();
        var fails = new List<string>();
        void Check(FrontRoomsRelayRig.MotionState s, float want, bool lightOn)
        {
            for (var i = 0; i < 240; i++) rig.TickAnimation(Dt, s, false, 1f);
            var on = body.LensLight != null && body.LensLight.enabled;
            lines.Add(s + " " + F(body.LensLevel) + " light " + on + (on ? " " + F(body.LensLight.intensity) + " r " + F(body.LensLight.range) : ""));
            if (Mathf.Abs(body.LensLevel - want) > .005f || on != lightOn) fails.Add(s.ToString());
        }
        Check(FrontRoomsRelayRig.MotionState.IdleListen, FrontRoomsRelayLens.StandbyLevel, false);
        Check(FrontRoomsRelayRig.MotionState.Search, FrontRoomsRelayLens.StandbyLevel, false);
        Check(FrontRoomsRelayRig.MotionState.Walk, FrontRoomsRelayLens.WalkLevel, true);
        Check(FrontRoomsRelayRig.MotionState.Run, FrontRoomsRelayLens.FullLevel, true);
        Check(FrontRoomsRelayRig.MotionState.IdleListen, FrontRoomsRelayLens.StandbyLevel, false);
        Kill(rig);
        return (fails.Count == 0 ? "" : "FAIL " + string.Join(",", fails) + " | ") + string.Join(" | ", lines);
    }

    // ------------------------------------------------------------------ E
    static string E1Brush()
    {
        var lines = new List<string>();
        var fails = new List<string>();
        foreach (var c in new[] { new[] { 2.9f, 1f }, new[] { 2.4f, 1f }, new[] { 5.4f, 1f }, new[] { 2.9f, 0f } })
        {
            var rig = NewRig();
            var times = new List<float>();
            var t = 0f;
            var pressedAtEvent = true;
            rig.CeilingBrush += p => { times.Add(t); if (!rig.PressesCeiling) pressedAtEvent = false; };
            Settle(rig, c[0], 0f, FrontRoomsRelayRig.MotionState.Walk, 1.5f);
            var moving = c[1] > 0f;
            for (var i = 0; i < 6 * 60; i++)
            {
                t += Dt;
                if (moving) rig.transform.position += rig.transform.forward * 2.6f * Dt;
                rig.TickAnimation(Dt, FrontRoomsRelayRig.MotionState.Walk, moving, 1f);
            }
            var minGap = 99f;
            for (var i = 1; i < times.Count; i++) minGap = Mathf.Min(minGap, times[i] - times[i - 1]);
            var rate = times.Count / 6f;
            lines.Add("h " + F(c[0]) + (moving ? " walking" : " standing") + ": " + times.Count + " events (" + F(rate) + "/s), min gap " + (times.Count > 1 ? F(minGap) : "-"));
            if (c[0] > 4f || !moving) { if (times.Count > 0) fails.Add("events with h " + c[0] + " moving " + moving); }
            else
            {
                if (times.Count == 0) fails.Add("no events at h " + c[0]);
                if (rate > 2.05f || (times.Count > 1 && minGap < .5f - 1e-3f)) fails.Add("rate/gap at h " + c[0]);
                if (!pressedAtEvent) fails.Add("event while not pressed");
            }
            Kill(rig);
        }
        return (fails.Count == 0 ? "" : "FAIL " + string.Join("; ", fails) + " | ") + string.Join(" | ", lines);
    }

    static string E2Squeeze()
    {
        var rig = NewRig();
        var levels = new List<float>();
        rig.Squeeze += (p, s) => levels.Add(s);
        Settle(rig, 2.9f, 0f, FrontRoomsRelayRig.MotionState.Walk, 1f);
        for (var i = 0; i <= 120; i++)
        {
            var d = i <= 60 ? i / 60f : (120 - i) / 60f;
            rig.DoorSqueeze = d;
            rig.transform.position += rig.transform.forward * 1.2f * Dt;
            rig.TickAnimation(Dt, FrontRoomsRelayRig.MotionState.Walk, true, 1f);
        }
        Kill(rig);
        var ok = levels.Count >= 3 && levels.Count <= 5;
        for (var i = 1; i < levels.Count; i++) if (levels[i] < levels[i - 1] + .3f - 1e-3f) ok = false;
        var detail = levels.Count + " events at DoorSqueeze " + string.Join(", ", levels.Select(F)) + " (in edit mode without colliders: the fallback rule)";
        return ok ? detail : "FAIL " + detail;
    }

    static string E3Step()
    {
        var rig = NewRig();
        var body = BodyOf(rig);
        var feet = new List<int>();
        var far = 0f;
        Transform footL = null, footR = null;
        foreach (var t in rig.GetComponentsInChildren<Transform>(true)) { if (t.name == "toe_L") footL = t; if (t.name == "toe_R") footR = t; }
        rig.Step += (f, p) =>
        {
            feet.Add(f);
            var toe = f == 0 ? footL : footR;
            if (toe != null) far = Mathf.Max(far, Vector3.Distance(Flat(toe.position), Flat(p)));
        };
        Settle(rig, 2.9f, 0f, FrontRoomsRelayRig.MotionState.Walk, 1f);
        const float v = 2.6f, seconds = 6f;
        for (var i = 0; i < seconds * 60; i++)
        {
            rig.transform.position += rig.transform.forward * v * Dt;
            rig.TickAnimation(Dt, FrontRoomsRelayRig.MotionState.Walk, true, 1f);
        }
        Kill(rig);
        var alternating = true;
        for (var i = 1; i < feet.Count; i++) if (feet[i] == feet[i - 1]) alternating = false;
        var cadence = feet.Count / seconds;
        var want = v / 1.14f;
        var detail = feet.Count + " steps in " + seconds + " s (" + F(cadence) + "/s, want ~" + F(want) + "), alternating " + alternating + ", max distance event -> toe " + F(far) + " m";
        return alternating && Mathf.Abs(cadence - want) / want < .25f && far < .35f ? detail : "FAIL " + detail;
    }

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

#if FRONTROOMS_RELAY_V2
    // ------------------------------------------------------------------ H, S: Relay v2's picture
    const FrontRoomsRelayRig.MotionState WalkS = FrontRoomsRelayRig.MotionState.Walk, RunS = FrontRoomsRelayRig.MotionState.Run, ListenS = FrontRoomsRelayRig.MotionState.IdleListen;

    /// <summary>The hunter's LookPoint for a yaw off the rig: 6 m out at eye height.</summary>
    static Vector3 Look(FrontRoomsRelayRig rig, float yaw) => rig.transform.position + Vector3.up * 1.6f + Quaternion.AngleAxis(yaw, Vector3.up) * rig.transform.forward * 6f;

    static float Sign3(Vector3 from, Vector3 to) => Vector3.SignedAngle(Flat(from), Flat(to), Vector3.up);

    static Vector3 ModelLens(FrontRoomsRelayRig rig, FrontRoomsRelayBody body) => rig.transform.InverseTransformPoint(body.LensWorld);

    /// <summary>The lens' pitch off level, up positive.</summary>
    static float LensPitch(FrontRoomsRelayBody body) { var d = body.LensDirWorld; return Mathf.Atan2(d.y, Flat(d).magnitude) * Mathf.Rad2Deg; }

    static string H1WalkHead()
    {
        var rig = NewRig(); var body = BodyOf(rig);
        var control = NewRig("Control"); var bodyC = BodyOf(control);
        Settle(rig, 2.9f, 0f, WalkS, .5f); Settle(control, 2.9f, 0f, WalkS, .5f);
        var heading = rig.transform.forward;
        var maxLag = 0f; var maxLens = 0f; var maxBelow = 0f;
        for (var i = 0; i < 5 * 60; i++)
        {
            var yaw = FrontRoomsMapHunter.WalkSweepDegrees * Mathf.Sin(i * Dt * 2f * Mathf.PI / 3.2f);
            rig.transform.position += heading * 2.6f * Dt;
            control.transform.position += heading * 2.6f * Dt;
            rig.SetListenTarget(Look(rig, yaw));
            control.SetListenTarget(Look(control, 0f));
            rig.TickAnimation(Dt, WalkS, true, 1f);
            control.TickAnimation(Dt, WalkS, true, 1f);
            if (i < 60) continue;
            maxLag = Mathf.Max(maxLag, Mathf.Abs(body.HeadYaw - yaw));
            maxLens = Mathf.Max(maxLens, Mathf.Abs(Sign3(rig.transform.forward, body.LensDirWorld) - yaw));
            foreach (var b in new[] { "pelvis", "chest", "shrug", "hand_L", "hand_R", "foot_L", "foot_R", "neck_02" })
                maxBelow = Mathf.Max(maxBelow, (body.BoneModel(b) - bodyC.BoneModel(b)).magnitude);
        }
        Kill(rig); Kill(control);
        var detail = "the map's sweep at 2.6 m/s: head lag max " + F(maxLag) + " deg, lens off the look max " + F(maxLens) + " deg (the chest's gait twist is in that), neck_02 and everything below moved " + F(maxBelow) + " m against a control";
        return maxLag <= 12f && maxLens <= 16f && maxBelow <= .002f ? detail : "FAIL " + detail;
    }

    static string H2Limit()
    {
        var rig = NewRig(); var body = BodyOf(rig);
        Settle(rig, 2.9f, 0f, WalkS, .5f);
        var heading = rig.transform.forward;
        var maxYaw = 0f; var maxLens = 0f; var maxStep = 0f; var last = body.HeadYaw;
        for (var i = 0; i < 8 * 60; i++)
        {
            rig.transform.position += heading * 2.6f * Dt;
            rig.SetListenTarget(Look(rig, i * Dt * 120f));
            rig.TickAnimation(Dt, WalkS, true, 1f);
            maxYaw = Mathf.Max(maxYaw, Mathf.Abs(body.HeadYaw));
            maxLens = Mathf.Max(maxLens, Mathf.Abs(Sign3(heading, body.LensDirWorld)));
            maxStep = Mathf.Max(maxStep, Mathf.Abs(body.HeadYaw - last));
            last = body.HeadYaw;
        }
        Kill(rig);
        var limit = FrontRoomsRelayBody.HeadLimitDegrees;
        var detail = "target right round twice: head max " + F(maxYaw) + " deg (limit " + F(limit) + ", map " + F(FrontRoomsMapHunter.HeadLimitDegrees) + "), lens max " + F(maxLens) + " deg, largest frame step " + F(maxStep) + " deg (top speed " + F(FrontRoomsRelayBody.HeadTopSpeed * Dt) + ")";
        return Mathf.Approximately(limit, FrontRoomsMapHunter.HeadLimitDegrees) && maxYaw <= limit + 1e-3f && maxLens <= limit + 4f && maxStep <= FrontRoomsRelayBody.HeadTopSpeed * Dt + .02f ? detail : "FAIL " + detail;
    }

    static string H3NoPop()
    {
        var rig = NewRig(); var body = BodyOf(rig);
        var cam = new GameObject("viewer").transform;
        FrontRoomsRelayBody.ViewerOverride = cam;
        Settle(rig, 2.9f, 0f, WalkS, .5f);
        // the game's order, the head continuous (LookYaw moves at 180 deg/s at most); seconds, state, hold, walking
        var steps = new[]
        {
            new object[] { 2.1f, WalkS, FrontRoomsRelayRig.HoldKind.None },
            new object[] { .7f, ListenS, FrontRoomsRelayRig.HoldKind.LockOn },
            new object[] { 1.8f, RunS, FrontRoomsRelayRig.HoldKind.None },
            new object[] { 1.7f, WalkS, FrontRoomsRelayRig.HoldKind.None },
            new object[] { .9f, ListenS, FrontRoomsRelayRig.HoldKind.Almost },
            new object[] { .4f, ListenS, FrontRoomsRelayRig.HoldKind.LockOn },
            new object[] { .35f, ListenS, FrontRoomsRelayRig.HoldKind.None },
            new object[] { 1.3f, WalkS, FrontRoomsRelayRig.HoldKind.None },
            new object[] { 1.7f, ListenS, FrontRoomsRelayRig.HoldKind.LockOn },
            new object[] { 1.2f, RunS, FrontRoomsRelayRig.HoldKind.None },
            new object[] { .6f, FrontRoomsRelayRig.MotionState.Stagger, FrontRoomsRelayRig.HoldKind.None },
            new object[] { 1.2f, WalkS, FrontRoomsRelayRig.HoldKind.None },
        };
        var look = 0f; var t = 0f;
        var lastYaw = body.HeadYaw; var lastLens = ModelLens(rig, body);
        var maxYaw = 0f; var maxLens = 0f; var lensAt = ""; var changes = 0;
        for (var k = 0; k < steps.Length; k++)
        {
            var seconds = (float)steps[k][0];
            var s = (FrontRoomsRelayRig.MotionState)steps[k][1];
            var hold = (FrontRoomsRelayRig.HoldKind)steps[k][2];
            changes++;
            var frames = Mathf.RoundToInt(seconds / Dt);
            for (var i = 0; i < frames; i++)
            {
                t += Dt;
                var moving = s == WalkS || s == RunS;
                if (moving) rig.transform.position += rig.transform.forward * (s == RunS ? 4.2f : 2.6f) * Dt;
                cam.position = rig.transform.position + rig.transform.forward * 8f + Vector3.up * 1.62f + rig.transform.right * 3f;
                var goal = s == WalkS ? 40f * Mathf.Sin(t * 2f * Mathf.PI / 3.2f) : hold != FrontRoomsRelayRig.HoldKind.None ? 25f : 0f;
                look = Mathf.MoveTowards(look, goal, 180f * Dt);
                rig.SetListenTarget(Look(rig, look));
                rig.HoldOverride = hold;
                rig.TickAnimation(Dt, s, moving, 1f);
                var plainListen = s == ListenS && hold == FrontRoomsRelayRig.HoldKind.None;   // the latched snaps are the design there
                var stagger = s == FrontRoomsRelayRig.MotionState.Stagger;
                var lens = ModelLens(rig, body);
                if (!plainListen && !stagger)
                {
                    maxYaw = Mathf.Max(maxYaw, Mathf.Abs(body.HeadYaw - lastYaw));
                    var d = (lens - lastLens).magnitude;
                    if (d > maxLens) { maxLens = d; lensAt = s + "/" + hold + " frame " + i; }
                }
                lastYaw = body.HeadYaw; lastLens = lens;
            }
        }
        rig.HoldOverride = null;
        FrontRoomsRelayBody.ViewerOverride = null;
        UnityEngine.Object.DestroyImmediate(cam.gameObject);
        Kill(rig);
        var detail = changes + " states in a row: largest head step " + F(maxYaw) + " deg per frame, largest lens step " + F(maxLens) + " m per frame (" + lensAt + ")";
        return maxYaw <= FrontRoomsRelayBody.HeadTopSpeed * Dt + .02f && maxLens <= .06f ? detail : "FAIL " + detail;
    }

    static string H4Door()
    {
        var rig = NewRig(); var body = BodyOf(rig);
        Settle(rig, 2.9f, 0f, WalkS, 1f);
        // a real doorway, 3.2 m ahead: in the game the rig's header probe starts the door weight up to 1.6 m out, the map's DoorSqueeze only from 0.6 m
        var wall = DoorWall(rig.transform.position + rig.transform.forward * 3.2f, rig.transform.rotation);
        FrontRoomsRelayBody.PhysicsInEditMode = true;
        const float v = 2.6f;
        var s = -3.2f; var inDoor = 0f;
        var before = 0f; var keysAt = 0f; var lensOff = 0f;
        while (s < 1.2f)
        {
            s += v * Dt;
            rig.transform.position += rig.transform.forward * v * Dt;
            rig.DoorSqueeze = Mathf.Clamp01(1f - Mathf.Abs(s) / .6f);
            rig.SetListenTarget(Look(rig, 40f));
            rig.TickAnimation(Dt, WalkS, true, 1f);
            if (keysAt == 0f && body.DoorWeight > 0f) keysAt = s;
            // the std hood leads the axis by about 0.8 m: from there it is in the doorway
            if (s >= -.85f && s <= .3f)
            {
                inDoor = Mathf.Max(inDoor, Mathf.Abs(body.HeadYaw));
                lensOff = Mathf.Max(lensOff, Mathf.Abs(Vector3.Dot(body.LensWorld - rig.transform.position, rig.transform.right)));
            }
            if (s < -1.7f + v * Dt * 2f || body.DoorWeight <= 0f) before = Mathf.Max(before, Mathf.Abs(body.HeadYaw));
        }
        FrontRoomsRelayBody.PhysicsInEditMode = false;
        UnityEngine.Object.DestroyImmediate(wall);
        Kill(rig);
        var detail = "a 40 deg look through a door (probe on, wall): door weight up at " + F(keysAt) + " m, head up to " + F(before) + " deg before the keys, at most " + F(inDoor) + " deg and " + F(lensOff) + " m off the axis with the hood in the doorway";
        return keysAt < -1f && inDoor <= 8f && before >= 20f ? detail : "FAIL " + detail;
    }

    /// <summary>A wall across the way at <paramref name="at"/>, 0.16 m thick, with a door 1 m wide and 2.2 m high on its centre: the map's header, for the rig's probe.</summary>
    static GameObject DoorWall(Vector3 at, Quaternion facing)
    {
        var wall = new GameObject("H4 wall");
        wall.transform.SetPositionAndRotation(at, facing);
        var left = wall.AddComponent<BoxCollider>(); left.center = new Vector3(-3f, 1.1f, 0f); left.size = new Vector3(5f, 2.2f, .16f);
        var right = wall.AddComponent<BoxCollider>(); right.center = new Vector3(3f, 1.1f, 0f); right.size = new Vector3(5f, 2.2f, .16f);
        var header = wall.AddComponent<BoxCollider>(); header.center = new Vector3(0f, 3.6f, 0f); header.size = new Vector3(11f, 2.8f, .16f);
        Physics.SyncTransforms();
        return wall;
    }

    static string S1SquareUp()
    {
        var lines = new List<string>();
        var fails = new List<string>();
        var ceiling = 2.9f;
        // what plain Listen looks like, for the difference
        var plain = NewRig();
        Settle(plain, ceiling, 0f, ListenS, 2f);
        var listenHand = BodyOf(plain).BoneModel("hand_R").y;
        var listenPitch = LensPitch(BodyOf(plain));
        Kill(plain);
        foreach (var holdSeconds in new[] { .35f, .7f, 1.7f })
        {
            var rig = NewRig(); var body = BodyOf(rig);
            var cam = new GameObject("viewer").transform;
            FrontRoomsRelayBody.ViewerOverride = cam;
            rig.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Settle(rig, ceiling, 0f, WalkS, .5f);
            for (var i = 0; i < 60; i++) { rig.transform.position += Vector3.forward * 2.6f * Dt; rig.SetListenTarget(Look(rig, 0f)); rig.TickAnimation(Dt, WalkS, true, 1f); }
            var walkPitch = LensPitch(body);
            // the player is 5 m off, 60 deg to the right of the heading: the game turns the transform to Heading at 360 deg/s, the head on LookPoint
            var player = rig.transform.position + Quaternion.AngleAxis(60f, Vector3.up) * Vector3.forward * 5f;
            cam.position = player + Vector3.up * 1.62f;
            var reached = -1f; var pitch03 = 0f;
            var frames = Mathf.RoundToInt(holdSeconds / Dt);
            for (var i = 0; i < frames; i++)
            {
                var to = Quaternion.LookRotation(Flat(player - rig.transform.position));
                rig.transform.rotation = Quaternion.RotateTowards(rig.transform.rotation, to, 360f * Dt);
                rig.SetListenTarget(rig.transform.position + Vector3.up * 1.6f + Flat(player - rig.transform.position).normalized * 6f);
                rig.HoldOverride = FrontRoomsRelayRig.HoldKind.LockOn;
                rig.TickAnimation(Dt, ListenS, false, 1f);
                if (reached < 0f && body.SquareWeight >= .999f) reached = (i + 1) * Dt;
                if (i * Dt <= .3f) pitch03 = LensPitch(body);
            }
            var pitch = LensPitch(body);
            var off = Mathf.Abs(Sign3(body.LensDirWorld, player - body.LensWorld));
            var hand = body.BoneModel("hand_R").y;
            var lineFails = new List<string>();
            if (reached < 0f && holdSeconds >= .2f + Dt || reached > .2f + 2f * Dt) lineFails.Add("weight 1 at " + F(reached));
            if (pitch - walkPitch < 12f) lineFails.Add("lift " + F(pitch - walkPitch));
            if (holdSeconds >= .7f && hand > 1.75f) lineFails.Add("hand " + F(hand));
            if (holdSeconds >= .7f && body.ListenWeight > 0f) lineFails.Add("listen " + F(body.ListenWeight));
            if (holdSeconds >= .7f && off > 6f) lineFails.Add("aim off " + F(off));
            if (holdSeconds >= 1.7f && pitch - pitch03 < 2f) lineFails.Add("beat " + F(pitch - pitch03));
            if (body.CatchWeight > 0f) lineFails.Add("catch weight in a 5 m hold");
            lines.Add("hold " + F(holdSeconds) + ": weight 1 at " + F(reached) + " s, lens pitch " + F(walkPitch) + " (walk) -> " + F(pitch) + ", aim off the player " + F(off) + " deg, hand_R " + F(hand) + " m (plain Listen " + F(listenHand) + ", pitch " + F(listenPitch) + ")");
            if (lineFails.Count > 0) fails.Add("hold " + F(holdSeconds) + " " + string.Join(", ", lineFails));
            rig.HoldOverride = null;
            FrontRoomsRelayBody.ViewerOverride = null;
            UnityEngine.Object.DestroyImmediate(cam.gameObject);
            Kill(rig);
        }
        return (fails.Count == 0 ? "" : "FAIL " + string.Join("; ", fails) + " | ") + string.Join(" | ", lines);
    }

    static void SettleHold(FrontRoomsRelayRig rig, float h, float face, float seconds)
    {
        FaceField.SetValue(rig, face);
        rig.CeilingHeight = h;
        rig.DoorSqueeze = 0f;
        rig.HoldOverride = FrontRoomsRelayRig.HoldKind.LockOn;
        var n = Mathf.RoundToInt(seconds / Dt);
        for (var i = 0; i < n; i++) { rig.SetListenTarget(Look(rig, 0f)); rig.TickAnimation(Dt, ListenS, false, 1f); }
    }

    static string S2Envelope()
    {
        var lines = new List<string>();
        var fails = new List<string>();
        foreach (var c in new[] { new[] { 2.4f, 0f }, new[] { 2.4f, 1f }, new[] { 2.9f, 0f }, new[] { 2.9f, 1f }, new[] { 5.4f, 0f } })
        {
            var rig = NewRig(); var body = BodyOf(rig);
            Settle(rig, c[0], c[1], WalkS, 3f);
            SettleHold(rig, c[0], c[1], 1.7f);
            var top = BakeTop(body, out _, Vector3.zero);
            var pinned = c[0] < 4f;
            if (pinned && top > c[0] + .005f) fails.Add("h " + F(c[0]) + " face " + F(c[1]) + " top " + F(top));
            if (!pinned && top > 3.45f) fails.Add("tall top " + F(top));
            lines.Add("h " + F(c[0]) + " face " + F(c[1]) + ": top " + F(top) + ", pin " + F(body.PinOffset) + ", lens " + F(body.LensWorld.y) + " m, pitch " + F(LensPitch(body)));
            rig.HoldOverride = null;
            Kill(rig);
        }
        // a touch lock-on: the camera inside the catch's reach, in the hold
        foreach (var dist in new[] { .7f, 1.3f })
        {
            var rig = NewRig(); var body = BodyOf(rig);
            var cam = new GameObject("viewer").transform;
            FrontRoomsRelayBody.ViewerOverride = cam;
            rig.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            cam.position = new Vector3(0f, 1.62f, dist);
            cam.rotation = Quaternion.LookRotation(-Vector3.forward);
            Settle(rig, 2.9f, 0f, WalkS, 1f);
            SettleHold(rig, 2.9f, 0f, 1.7f);
            BakeTop(body, out var minDist, cam.position);
            var lensDist = Vector3.Distance(body.LensWorld, cam.position);
            if (lensDist < .29f || minDist < .145f) fails.Add("hold at " + F(dist) + ": lens " + F(lensDist) + " vertex " + F(minDist));
            lines.Add("hold at " + F(dist) + " m: lens " + F(lensDist) + ", nearest vertex " + F(minDist) + ", pull " + F(body.Clearance));
            rig.HoldOverride = null;
            FrontRoomsRelayBody.ViewerOverride = null;
            UnityEngine.Object.DestroyImmediate(cam.gameObject);
            Kill(rig);
        }
        return (fails.Count == 0 ? "" : "FAIL " + string.Join("; ", fails) + " | ") + string.Join(" | ", lines);
    }

    static string S3Freeze()
    {
        var fails = new List<string>();
        var maxDrift = 0f; var maxJump = 0f; var bestStride = 0f;
        // several stop phases
        for (var run = 0; run < 5; run++)
        {
            var rig = NewRig(); var body = BodyOf(rig);
            Settle(rig, 2.9f, 0f, WalkS, .5f);
            var frames = 70 + run * 7;
            for (var i = 0; i < frames; i++) { rig.transform.position += rig.transform.forward * 2.6f * Dt; rig.SetListenTarget(Look(rig, 0f)); rig.TickAnimation(Dt, WalkS, true, 1f); }
            var lastL = rig.transform.TransformPoint(body.BoneModel("foot_L"));
            var lastR = rig.transform.TransformPoint(body.BoneModel("foot_R"));
            Vector3 atL = lastL, atR = lastR;
            for (var i = 0; i < 102; i++)
            {
                rig.HoldOverride = FrontRoomsRelayRig.HoldKind.LockOn;
                rig.SetListenTarget(Look(rig, 0f));
                rig.TickAnimation(Dt, ListenS, false, 1f);
                var l = rig.transform.TransformPoint(body.BoneModel("foot_L"));
                var r = rig.transform.TransformPoint(body.BoneModel("foot_R"));
                maxJump = Mathf.Max(maxJump, Mathf.Max((l - lastL).magnitude, (r - lastR).magnitude));
                if (i == 15) { atL = l; atR = r; }
                if (i > 15) maxDrift = Mathf.Max(maxDrift, Mathf.Max((l - atL).magnitude, (r - atR).magnitude));
                lastL = l; lastR = r;
            }
            bestStride = Mathf.Max(bestStride, Mathf.Abs(lastL.z - lastR.z));
            rig.HoldOverride = null;
            Kill(rig);
        }
        // a turn over planted feet, the game's 360 deg/s
        var turner = NewRig(); var turnerBody = BodyOf(turner);
        Settle(turner, 2.9f, 0f, WalkS, .5f);
        for (var i = 0; i < 80; i++) { turner.transform.position += turner.transform.forward * 2.6f * Dt; turner.SetListenTarget(Look(turner, 0f)); turner.TickAnimation(Dt, WalkS, true, 1f); }
        var target = turner.transform.rotation * Quaternion.Euler(0f, 150f, 0f);
        var worstReach = 0f;
        for (var i = 0; i < 102; i++)
        {
            turner.transform.rotation = Quaternion.RotateTowards(turner.transform.rotation, target, 360f * Dt);
            turner.HoldOverride = FrontRoomsRelayRig.HoldKind.LockOn;
            turner.SetListenTarget(Look(turner, 0f));
            turner.TickAnimation(Dt, ListenS, false, 1f);
            worstReach = Mathf.Max(worstReach, Flat(turnerBody.BoneModel("foot_L")).magnitude);
            worstReach = Mathf.Max(worstReach, Flat(turnerBody.BoneModel("foot_R")).magnitude);
        }
        var steps = turnerBody.PlantSteps;
        turner.HoldOverride = null;
        Kill(turner);
        if (maxDrift > .02f) fails.Add("drift " + F(maxDrift));
        if (maxJump > .09f) fails.Add("jump " + F(maxJump));
        if (steps < 2) fails.Add("steps " + steps);
        if (worstReach > 1.1f) fails.Add("reach " + F(worstReach));
        var detail = "5 stop phases: foot drift after 0.25 s max " + F(maxDrift) + " m, largest foot step in a frame " + F(maxJump) + " m, widest stride held " + F(bestStride) + " m; a 150 deg turn over planted feet: " + steps + " steps, foot at most " + F(worstReach) + " m off the axis";
        return fails.Count == 0 ? detail : "FAIL " + string.Join("; ", fails) + " | " + detail;
    }

    static string S4Almost()
    {
        var fails = new List<string>();
        var rig = NewRig(); var body = BodyOf(rig);
        Settle(rig, 2.9f, 0f, WalkS, .5f);
        for (var i = 0; i < 60; i++) { rig.transform.position += rig.transform.forward * 2.6f * Dt; rig.SetListenTarget(Look(rig, 0f)); rig.TickAnimation(Dt, WalkS, true, 1f); }
        var walkPitch = LensPitch(body);
        var maxListen = 0f;
        for (var i = 0; i < 60; i++)
        {
            rig.HoldOverride = FrontRoomsRelayRig.HoldKind.Almost;
            rig.SetListenTarget(Look(rig, 37.5f));
            rig.TickAnimation(Dt, ListenS, false, 1f);
            maxListen = Mathf.Max(maxListen, body.ListenWeight);
        }
        var yaw = body.HeadYaw; var weight = body.SquareWeight; var pitch = LensPitch(body);
        if (maxListen > 0f) fails.Add("the glove rose: listen " + F(maxListen));
        if (!Mathf.Approximately(weight, FrontRoomsRelayBody.AlmostShare)) fails.Add("weight " + F(weight));
        if (Mathf.Abs(yaw - 37.5f) > 1f) fails.Add("yaw " + F(yaw) + " (a 15 deg latch would stand at 30 or 45)");
        var line = "from a walk: weight " + F(weight) + ", listen " + F(maxListen) + ", head " + F(yaw) + " deg on a 37.5 deg look, lens pitch " + F(walkPitch) + " -> " + F(pitch);
        rig.HoldOverride = null;
        Kill(rig);
        // from Listen the glove stays up
        var rig2 = NewRig(); var body2 = BodyOf(rig2);
        Settle(rig2, 2.9f, 0f, ListenS, 1f);
        var minListen = 1f;
        for (var i = 0; i < 60; i++)
        {
            rig2.HoldOverride = FrontRoomsRelayRig.HoldKind.Almost;
            rig2.SetListenTarget(Look(rig2, 20f));
            rig2.TickAnimation(Dt, ListenS, false, 1f);
            minListen = Mathf.Min(minListen, body2.ListenWeight);
        }
        rig2.HoldOverride = null;
        Kill(rig2);
        if (minListen < 1f) fails.Add("the glove fell: listen " + F(minListen));
        line += "; from Listen the glove stays, listen " + F(minListen);
        return fails.Count == 0 ? line : "FAIL " + string.Join("; ", fails) + " | " + line;
    }

    static void LensScript(float camDistance, bool holds, List<float> lens, List<float> light)
    {
        var rig = NewRig(); var body = BodyOf(rig);
        var cam = new GameObject("viewer").transform;
        cam.position = new Vector3(0f, 1.62f, camDistance);
        cam.rotation = Quaternion.LookRotation(-Vector3.forward);
        FrontRoomsRelayBody.ViewerOverride = cam;
        for (var i = 0; i < 16 * 60; i++)
        {
            var t = i * Dt;
            var cycle = t % 5.3f;
            var s = cycle < 2f ? WalkS : cycle < 3.7f ? ListenS : cycle < 4.7f ? RunS : ListenS;
            var hold = !holds || s != ListenS ? FrontRoomsRelayRig.HoldKind.None : cycle < 3.7f ? FrontRoomsRelayRig.HoldKind.LockOn : FrontRoomsRelayRig.HoldKind.Almost;
            rig.HoldOverride = hold;
            rig.SetListenTarget(Look(rig, 20f * Mathf.Sin(t)));
            rig.TickAnimation(Dt, s, s == WalkS || s == RunS, 1f);
            lens.Add(body.LensLevel);
            light.Add(body.LensLight != null && body.LensLight.enabled ? body.LensLight.intensity : 0f);
        }
        rig.HoldOverride = null;
        FrontRoomsRelayBody.ViewerOverride = null;
        UnityEngine.Object.DestroyImmediate(cam.gameObject);
        Kill(rig);
    }

    static string S5Lens()
    {
        var near = new List<float>(); var nearLight = new List<float>();
        var far = new List<float>(); var farLight = new List<float>();
        var none = new List<float>(); var noneLight = new List<float>();
        LensScript(2f, true, near, nearLight);
        LensScript(30f, true, far, farLight);
        LensScript(30f, false, none, noneLight);
        var diffRadar = 0f; var diffHold = 0f;
        for (var i = 0; i < near.Count; i++)
        {
            diffRadar = Mathf.Max(diffRadar, Mathf.Max(Mathf.Abs(near[i] - far[i]), Mathf.Abs(nearLight[i] - farLight[i])));
            diffHold = Mathf.Max(diffHold, Mathf.Max(Mathf.Abs(far[i] - none[i]), Mathf.Abs(farLight[i] - noneLight[i])));
        }
        var perWindow = MaxPerWindow(Transitions(near, .1f, out _));
        // the end of a 1.7 s hold in a steady standby
        var rig = NewRig(); var body = BodyOf(rig);
        for (var i = 0; i < 120; i++) rig.TickAnimation(Dt, WalkS, false, 1f);
        rig.HoldOverride = FrontRoomsRelayRig.HoldKind.LockOn;
        for (var i = 0; i < 102; i++) { rig.SetListenTarget(Look(rig, 0f)); rig.TickAnimation(Dt, ListenS, false, 1f); }
        var level = body.LensLevel;
        rig.HoldOverride = null;
        Kill(rig);
        var detail = near.Count + " frames: max |2 m - 30 m| " + diffRadar.ToString("0.000000", IC) + ", max |holds - no holds| " + diffHold.ToString("0.000000", IC) + ", " + perWindow + " transitions at most per 1 s; a 1.7 s hold ends at " + F(level);
        return diffRadar == 0f && diffHold == 0f && perWindow <= 3 && Mathf.Abs(level - FrontRoomsRelayLens.StandbyLevel) < .005f ? detail : "FAIL " + detail;
    }

    static string S6Reveal()
    {
        float Turned(FrontRoomsRelayRig.HoldKind hold, FrontRoomsRelayRig.MotionState s, out float pull)
        {
            var rig = NewRig(); var body = BodyOf(rig);
            var cam = new GameObject("viewer").transform;
            FrontRoomsRelayBody.ViewerOverride = cam;
            rig.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            cam.position = new Vector3(100f, 1.62f, 0f);
            Settle(rig, 2.9f, 0f, s, .6f);
            rig.HoldOverride = hold;
            for (var i = 0; i < 60; i++) { rig.SetListenTarget(Look(rig, 0f)); rig.TickAnimation(Dt, s, false, 1f); }
            var before = body.LensDirWorld;
            // a viewer 1.0 m from the hood, to its left: behind a lite, or simply unnoticed
            cam.position = body.LensWorld + Vector3.left * 1f;
            for (var i = 0; i < 30; i++) { rig.SetListenTarget(Look(rig, 0f)); rig.TickAnimation(Dt, s, false, 1f); }
            var angle = Vector3.Angle(before, body.LensDirWorld);
            pull = body.CatchWeight;
            rig.HoldOverride = null;
            FrontRoomsRelayBody.ViewerOverride = null;
            UnityEngine.Object.DestroyImmediate(cam.gameObject);
            Kill(rig);
            return angle;
        }
        var plain = Turned(FrontRoomsRelayRig.HoldKind.None, ListenS, out var plainW);
        var held = Turned(FrontRoomsRelayRig.HoldKind.LockOn, ListenS, out var heldW);
        var detail = "viewer 1 m to the side: plain Listen turns the lens " + F(plain) + " deg (catch weight " + F(plainW) + ", the clearance stays), the lock-on's hold " + F(held) + " deg (weight " + F(heldW) + ")";
        return plain <= 16f && held >= 18f && plainW > .5f ? detail : "FAIL " + detail;
    }

    static string G3Alloc()
    {
        var rig = NewRig();
        var cam = new GameObject("viewer").transform;
        cam.position = new Vector3(0f, 1.62f, 4f);
        FrontRoomsRelayBody.ViewerOverride = cam;
        for (var i = 0; i < 120; i++) { rig.transform.position += rig.transform.forward * 2.6f * Dt; rig.TickAnimation(Dt, WalkS, true, 1f); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 600; i++)
        {
            var phase = (i / 60) % 4;
            var s = phase == 0 ? WalkS : phase == 1 ? ListenS : phase == 2 ? ListenS : RunS;
            rig.HoldOverride = phase == 1 ? FrontRoomsRelayRig.HoldKind.LockOn : phase == 2 ? FrontRoomsRelayRig.HoldKind.Almost : FrontRoomsRelayRig.HoldKind.None;
            rig.SetListenTarget(new Vector3(Mathf.Sin(i * .1f) * 6f, 1.6f, 6f));
            if (phase == 0 || phase == 3) rig.transform.position += rig.transform.forward * 2.6f * Dt;
            rig.TickAnimation(Dt, s, phase == 0 || phase == 3, 1f);
        }
        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        rig.HoldOverride = null;
        FrontRoomsRelayBody.ViewerOverride = null;
        UnityEngine.Object.DestroyImmediate(cam.gameObject);
        Kill(rig);
        var detail = bytes + " bytes over 600 ticks (walk head, hold, almost, run)";
        return bytes == 0 ? detail : "FAIL " + detail;
    }
#else
    static string V0Off()
    {
        var t = typeof(FrontRoomsRelayRig);
        var leaked = t.GetNestedType("HoldKind") != null || t.GetProperty("HoldNow") != null || typeof(FrontRoomsRelayBody).GetProperty("SquareWeight") != null;
        return leaked ? "FAIL the hold API compiled with the define off" : "no HoldKind, no HoldNow, no SquareWeight: main's text (unifdef -U proves it in the package)";
    }
#endif

    // ------------------------------------------------------------------ G
    static string G1Alloc()
    {
        var rig = NewRig();
        var cam = new GameObject("viewer").transform;
        cam.position = new Vector3(0f, 1.62f, 4f);
        FrontRoomsRelayBody.ViewerOverride = cam;
        for (var i = 0; i < 120; i++) { rig.transform.position += rig.transform.forward * 2.6f * Dt; rig.TickAnimation(Dt, FrontRoomsRelayRig.MotionState.Walk, true, 1f); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 600; i++)
        {
            var s = (i / 90) % 4 == 0 ? FrontRoomsRelayRig.MotionState.Walk : (i / 90) % 4 == 1 ? FrontRoomsRelayRig.MotionState.Run : (i / 90) % 4 == 2 ? FrontRoomsRelayRig.MotionState.Search : FrontRoomsRelayRig.MotionState.IdleListen;
            rig.CeilingHeight = (i / 150) % 2 == 0 ? 2.9f : 2.4f;
            rig.DoorSqueeze = Mathf.Clamp01(1f - Mathf.Abs((i % 200) - 100) / 40f);
            rig.transform.position += rig.transform.forward * 2.6f * Dt;
            rig.TickAnimation(Dt, s, true, 1f);
        }
        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        FrontRoomsRelayBody.ViewerOverride = null;
        UnityEngine.Object.DestroyImmediate(cam.gameObject);
        Kill(rig);
        var detail = bytes + " bytes over 600 ticks (state, ceiling and door changes)";
        return bytes == 0 ? detail : "FAIL " + detail;
    }

    static string G2Cost()
    {
        var rig = NewRig();
        var body = BodyOf(rig);
        var cam = new GameObject("viewer").transform;
        cam.position = new Vector3(0f, 1.62f, 4f);
        FrontRoomsRelayBody.ViewerOverride = cam;
        double sum = 0, worst = 0;
        var n = 0;
        var sw = new System.Diagnostics.Stopwatch();
        double rigSum = 0;
        for (var i = 0; i < 900; i++)
        {
            rig.transform.position += rig.transform.forward * 2.6f * Dt;
            sw.Restart();
            rig.TickAnimation(Dt, (i / 120) % 2 == 0 ? FrontRoomsRelayRig.MotionState.Walk : FrontRoomsRelayRig.MotionState.Run, true, 1f);
            sw.Stop();
            if (i < 120) continue;
            sum += body.LastTickMs; worst = Math.Max(worst, body.LastTickMs); n++;
            rigSum += sw.Elapsed.TotalMilliseconds;
        }
        FrontRoomsRelayBody.ViewerOverride = null;
        UnityEngine.Object.DestroyImmediate(cam.gameObject);
        Kill(rig);
        var mean = sum / n;
        return "body tick mean " + mean.ToString("0.000", IC) + " ms, worst " + worst.ToString("0.000", IC) + " ms, whole TickAnimation mean " + (rigSum / n).ToString("0.000", IC)
               + " ms over " + n + " ticks (edit mode, wall time; budget 0.25 ms; the report marks it UNDER LOAD at load > 32)";
    }
}
