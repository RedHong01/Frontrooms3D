// D2 (b) body proxy test (Red 2026-10-22 12:36, Documentation/research/glass/rt/NOTE_1022_red_decisions.md), EditMode: no Play
// Mode, no scene, no PlayerPrefs write, no plugin call. Checks the reflection-only body proxy of FrontRoomsGlassRTSystem:
//   - the mesh (FrontRoomsGlassRTBodyProxyMesh): 2,540 vertices, 4,612 triangles, 4 submeshes, 1.75 m tall, feet on y = 0,
//     eye height inside the head, body-sized width and depth;
//   - the renderer setup: never drawn by a raster pass (forceRenderingOff, rendering layer mask 0), no raster shadow cast or
//     received, no probes, no motion vectors, no collider;
//   - the pose rules: yaw from the camera, the vertical scale and its clamp, the hide below a 0.20 m eye drop, the clamped
//     follow of the render camera's offset, the 2 mm / 0.2 deg / 0.2 % rewrite steps (the TLAS reuse of a still player);
//   - then the D1 (c) gate test (FrontRoomsGlassRTCoreGateTest.Run), which decides when RT, and so the proxy, is on.
// Batch: Unity -batchmode -projectPath <proj> -executeMethod FrontRoomsGlassRTBodyProxyTest.RunBatch [-frBodyOut <file>]
//        [-frRtIsolation] -logFile <f>   (exit 0 = all PASS, 1 = a FAIL; with -frRtIsolation and all PASS it then runs
//        FrontRoomsGlassRTIsolation.RunBatch, the WebGL / iOS / Windows / macOS player-script compile, which exits 0 itself)
#if UNITY_EDITOR_OSX
using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using S = FrontRoomsGlassRTSystem;

public static class FrontRoomsGlassRTBodyProxyTest
{
    public static void RunBatch()
    {
        var ok = false;
        var report = "";
        try { ok = Run(out report); }
        catch (Exception e) { report += "\nEXCEPTION " + e; }
        var gateOk = false;
        var gateReport = "";
        try { gateOk = FrontRoomsGlassRTCoreGateTest.Run(out gateReport); }
        catch (Exception e) { gateReport += "\nEXCEPTION " + e; }
        report += "\n\n" + gateReport;
        var all = ok && gateOk;
        var args = Environment.GetCommandLineArgs();
        string output = null;
        var isolation = false;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "-frBodyOut" && i + 1 < args.Length) output = args[i + 1];
            if (args[i] == "-frRtIsolation") isolation = true;
        }
        if (string.IsNullOrEmpty(output))
            output = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "rt_body", "body_test.txt");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, report.Replace("\r\n", "\n") + "\n" + (all ? "ALL PASS" : "FAIL") + " (body proxy " + (ok ? "PASS" : "FAIL") + ", D1 (c) gate " + (gateOk ? "PASS" : "FAIL") + ")\n");
        }
        catch (Exception e) { Debug.LogError("[GlassRTBodyProxyTest] could not write " + output + ": " + e.Message); }
        Debug.Log("[GlassRTBodyProxyTest] " + (all ? "ALL PASS" : "FAIL") + " -> " + output + "\n" + report);
        if (all && isolation) { FrontRoomsGlassRTIsolation.RunBatch(); return; }
        EditorApplication.Exit(all ? 0 : 1);
    }

    static int pass, fail;
    static StringBuilder log;

    static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    static void Check(string what, bool ok, string got)
    {
        if (ok) pass++; else fail++;
        log.AppendLine((ok ? "PASS  " : "FAIL  ") + what + " -> " + got);
    }

    static void Near(string what, float got, float want, float tol) => Check(what, Mathf.Abs(got - want) <= tol, F(got) + " (want " + F(want) + " +- " + F(tol) + ")");

    public static bool Run(out string report)
    {
        pass = fail = 0;
        log = new StringBuilder();
        log.AppendLine("D2 (b) body proxy test, " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));

        // 1. the mesh
        var mesh = FrontRoomsGlassRTBodyProxyMesh.Build();
        Check("mesh builds", mesh != null, mesh != null ? mesh.name : "null");
        if (mesh != null)
        {
            Check("vertices", mesh.vertexCount == FrontRoomsGlassRTBodyProxyMesh.Vertices && mesh.vertexCount == 2540, mesh.vertexCount.ToString());
            var tris = 0;
            for (var k = 0; k < mesh.subMeshCount; k++) tris += (int)mesh.GetIndexCount(k) / 3;
            Check("triangles", tris == FrontRoomsGlassRTBodyProxyMesh.Triangles && tris == 4612, tris.ToString());
            Check("submeshes suit / dark / skin / shirt", mesh.subMeshCount == 4, mesh.subMeshCount.ToString());
            var suit = (int)mesh.GetIndexCount(0) / 3;
            Check("the suit is the largest submesh (jacket, sleeves, trousers)", suit > tris / 3, suit + " of " + tris);
            var b = mesh.bounds;
            Near("feet on y = 0", b.min.y, 0f, .01f);
            Near("stature (PlayerHeight 1.75 m)", b.max.y, 1.75f, .03f);
            Check("eye height inside the head (1.50-1.75)", S.BodyModelEye > 1.5f && S.BodyModelEye < b.max.y, F(S.BodyModelEye));
            Check("body-sized width (0.45-0.70 m)", b.size.x >= .45f && b.size.x <= .70f, F(b.size.x));
            Check("body-sized depth (0.20-0.45 m)", b.size.z >= .20f && b.size.z <= .45f, F(b.size.z));
            Check("on the capsule axis (|centre x| < 5 cm)", Mathf.Abs(b.center.x) < .05f, F(b.center.x));
            var nOk = true;
            foreach (var n in mesh.normals) if (Mathf.Abs(n.sqrMagnitude - 1f) > .01f) { nOk = false; break; }
            Check("unit normals", nOk, nOk ? "all" : "a normal is not unit length");
            Object.DestroyImmediate(mesh);
        }

        // 2. the renderer: never drawn, no raster shadows, no collider
        var go = new GameObject("BodyProxyTest renderer");
        go.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            go.AddComponent<MeshFilter>();
            var r = go.AddComponent<MeshRenderer>();
            S.ConfigureBodyRenderer(r);
            Check("forceRenderingOff (culled from every camera, shadow and depth pass)", r.forceRenderingOff, r.forceRenderingOff.ToString());
            Check("rendering layer mask 0 (no DrawRenderers filter matches)", r.renderingLayerMask == 0u, r.renderingLayerMask.ToString());
            Check("casts no raster shadow", r.shadowCastingMode == ShadowCastingMode.Off, r.shadowCastingMode.ToString());
            Check("receives no raster shadow", !r.receiveShadows, r.receiveShadows.ToString());
            Check("no light / reflection probes", r.lightProbeUsage == LightProbeUsage.Off && r.reflectionProbeUsage == ReflectionProbeUsage.Off, r.lightProbeUsage + " / " + r.reflectionProbeUsage);
            Check("no motion vectors", r.motionVectorGenerationMode == MotionVectorGenerationMode.ForceNoMotion, r.motionVectorGenerationMode.ToString());
            Check("no collider (physics, the Relay's sight and the aim ray never meet it)", go.GetComponentsInChildren<Collider>(true).Length == 0, go.GetComponentsInChildren<Collider>(true).Length.ToString());
        }
        finally { Object.DestroyImmediate(go); }
        Check("reflection-only mask is 0x10 and not a shadow caster (0x20)", FrontRoomsGlassRTNative.MaskReflectionOnly == 0x10u && (FrontRoomsGlassRTNative.MaskReflectionOnly & FrontRoomsGlassRTNative.MaskCaster) == 0,
              "0x" + FrontRoomsGlassRTNative.MaskReflectionOnly.ToString("x"));

        // 3. the pose rules
        float yaw, sy;
        Check("standing, facing +Z: shown", S.BodyPose(1.62f, Vector3.forward, Vector3.up, out yaw, out sy), "yaw " + F(yaw) + " scale " + F(sy));
        Near("  yaw", yaw, 0f, .01f); Near("  scale", sy, 1f, .001f);
        S.BodyPose(1.62f, new Vector3(1f, -.3f, 0f), Vector3.up, out yaw, out sy);
        Near("facing +X, looking down 17 deg: yaw", yaw, 90f, .01f);
        S.BodyPose(1.62f, new Vector3(-.7071f, 0f, -.7071f), Vector3.up, out yaw, out sy);
        Near("facing -X-Z: yaw", Mathf.DeltaAngle(0f, yaw), -135f, .05f);
        Check("looking straight down: shown, yaw from the camera's up", S.BodyPose(1.62f, Vector3.down, new Vector3(0f, 0f, -1f), out yaw, out sy), "yaw " + F(yaw));
        Near("  yaw (camera up -Z = facing -Z)", Mathf.Abs(Mathf.DeltaAngle(0f, yaw)), 180f, .05f);
        Check("knee bend 0.10 m: shown", S.BodyPose(1.52f, Vector3.forward, Vector3.up, out yaw, out sy), "scale " + F(sy));
        Near("  scale = eye / 1.62", sy, 1.52f / 1.62f, .001f);
        // just inside the limit (1.62 - 1.42 is 0.2000000x in float, which is already past it)
        Check("drop 0.199 m (just inside the 0.20 m limit): shown", S.BodyPose(1.421f, Vector3.forward, Vector3.up, out yaw, out sy), "scale " + F(sy));
        Check("  squash at most 12.4 %", sy >= (1.62f - .20f) / 1.62f - 1e-4f, F(sy));
        Check("window-climb duck 0.55 m: out of the reflections", !S.BodyPose(1.62f - .55f, Vector3.forward, Vector3.up, out yaw, out sy), "hidden");
        Check("door-unlock dip 0.25 m: out of the reflections", !S.BodyPose(1.62f - .25f, Vector3.forward, Vector3.up, out yaw, out sy), "hidden");
        S.BodyPose(2.1f, Vector3.forward, Vector3.up, out yaw, out sy);
        Near("an eye far above (a jump): scale clamped to 1.15", sy, 1.15f, .001f);

        var fo = S.BodyFollow(new Vector3(.4f, 0f, 0f));
        Near("follow: 0.40 m sideways clamped to 0.25", fo.x, .25f, .001f);
        fo = S.BodyFollow(new Vector3(.3f, 0f, .4f));
        Near("follow: 0.50 m diagonal clamped to 0.25 (length)", new Vector2(fo.x, fo.z).magnitude, .25f, .001f);
        fo = S.BodyFollow(new Vector3(0f, -.4f, 0f));
        Near("follow: 0.40 m down clamped to -0.25", fo.y, -.25f, .001f);
        fo = S.BodyFollow(new Vector3(0f, .3f, 0f));
        Near("follow: 0.30 m up clamped to +0.10", fo.y, .10f, .001f);
        fo = S.BodyFollow(new Vector3(.08f, -.06f, .12f));
        Check("follow: a strike lunge (0.08, -0.06, 0.12) passes unchanged", (fo - new Vector3(.08f, -.06f, .12f)).sqrMagnitude < 1e-8f, fo.ToString("F3"));

        var p0 = new Vector3(3f, 0f, 5f);
        Check("rewrite: nothing written yet", S.BodyPoseMoved(false, p0, 10f, 1f, p0, 10f, 1f), "write");
        Check("rewrite: the same pose keeps the TLAS", !S.BodyPoseMoved(true, p0, 10f, 1f, p0, 10f, 1f), "keep");
        Check("rewrite: 1 mm keeps", !S.BodyPoseMoved(true, p0, 10f, 1f, p0 + new Vector3(.001f, 0f, 0f), 10f, 1f), "keep");
        Check("rewrite: 3 mm writes", S.BodyPoseMoved(true, p0, 10f, 1f, p0 + new Vector3(.003f, 0f, 0f), 10f, 1f), "write");
        Check("rewrite: 0.1 deg keeps", !S.BodyPoseMoved(true, p0, 10f, 1f, p0, 10.1f, 1f), "keep");
        Check("rewrite: 0.3 deg writes", S.BodyPoseMoved(true, p0, 10f, 1f, p0, 10.3f, 1f), "write");
        Check("rewrite: 359.9 -> 0.0 deg keeps (wraps)", !S.BodyPoseMoved(true, p0, 359.9f, 1f, p0, 0f, 1f), "keep");
        Check("rewrite: scale 0.1 % keeps", !S.BodyPoseMoved(true, p0, 10f, 1f, p0, 10f, 1.001f), "keep");
        Check("rewrite: scale 0.3 % writes", S.BodyPoseMoved(true, p0, 10f, 1f, p0, 10f, 1.003f), "write");
        Check("the game never switches the proxy off (harness A/B only)", S.BodyProxyOn, S.BodyProxyOn.ToString());

        log.AppendLine("body proxy: " + pass + " PASS, " + fail + " FAIL");
        report = log.ToString();
        return fail == 0;
    }
}
#endif
