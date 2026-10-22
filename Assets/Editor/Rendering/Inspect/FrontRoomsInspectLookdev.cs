using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// EXAMINE look-dev (research/inspect/10_design.md §7.1 file 8): every kit whose sidecar carries an "inspect" block,
/// framed at the game's numbers, outside the game. A 3 m Level 0 wall (L0_Wallpaper, floor, ceiling) lit by one
/// ceiling point lamp; the kit on the wall (flat and panel at its own height or 1.524 m; object kinds hang at 1.40 m);
/// an eye 1.5 m out at 1.62 m; the solver's base pose and the zoom step; FOV 76 + fovDelta; the game's post
/// (FrontRoomsPostStack) and 4x MSAA. Renders 1920x1080 and 3840x2160 and writes, per frame, px/mm at 1080p and 4K
/// and the contrast of the face (p90 / p5 of linear luminance inside the framed rect).
/// Out: Verification/inspect/lookdev/ (PNG + lookdev.txt). Headless: -executeMethod FrontRoomsInspectLookdev.RunBatch -quit.
/// The contrast probe (ContrastRatio) is the same p90/p5 method §3.4 uses on Red's render.
/// </summary>
public static class FrontRoomsInspectLookdev
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    [MenuItem("FrontRooms/Rendering/Inspect/Look-dev every inspect kit")]
    public static void Run() => Execute(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "inspect", "lookdev"));

    public static void RunBatch() => Run();

    /// <summary>p90 / p5 of linear luminance (Rec. 709) of the sRGB pixels inside a rect (bottom-left origin, pixels).</summary>
    public static float ContrastRatio(Texture2D tex, RectInt box)
    {
        var px = tex.GetPixels(box.x, box.y, box.width, box.height);
        var lum = new float[px.Length];
        for (var i = 0; i < px.Length; i++)
        {
            var c = px[i].linear;
            lum[i] = .2126f * c.r + .7152f * c.g + .0722f * c.b;
        }
        Array.Sort(lum);
        var p5 = lum[Mathf.Clamp((int)(lum.Length * .05f), 0, lum.Length - 1)];
        var p90 = lum[Mathf.Clamp((int)(lum.Length * .90f), 0, lum.Length - 1)];
        return p90 / Mathf.Max(1e-5f, p5);
    }

    static void Execute(string outDir)
    {
        Directory.CreateDirectory(outDir);
        var log = new StringBuilder();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        FrontRoomsKitLibrary.ClearCache();
        var stage = new GameObject("INSPECT LOOKDEV") { hideFlags = HideFlags.DontSave };
        try
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(stage.transform, false);
            wall.transform.position = new Vector3(0f, 1.37f, -.05f);
            wall.transform.localScale = new Vector3(3f, 2.74f, .1f);
            var wallMat = FrontRoomsSurfaces.Room(RoomRule.Lobby, FrontRoomsSurfaces.Slot.Wall);
            if (wallMat != null) wall.GetComponent<Renderer>().sharedMaterial = wallMat;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(stage.transform, false);
            floor.transform.position = new Vector3(0f, -.05f, 1.5f);
            floor.transform.localScale = new Vector3(3f, .1f, 3f);
            var floorMat = FrontRoomsSurfaces.Room(RoomRule.Lobby, FrontRoomsSurfaces.Slot.Floor);
            if (floorMat != null) floor.GetComponent<Renderer>().sharedMaterial = floorMat;
            var lamp = new GameObject("lamp").AddComponent<Light>();
            lamp.transform.SetParent(stage.transform, false);
            lamp.transform.position = new Vector3(0f, 2.6f, 1.2f);
            lamp.type = LightType.Point;
            lamp.range = 6f;
            lamp.intensity = 2.2f;
            lamp.color = new Color(1f, .95f, .82f);
            lamp.shadows = LightShadows.Soft;
            var camGo = new GameObject("lookdev camera");
            camGo.transform.SetParent(stage.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = .06f;
            cam.farClipPlane = 40f;
            FrontRoomsPostStack.ConfigureCamera(cam);
            FrontRoomsPostStack.Ensure(stage.transform);
            var rt1080 = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            var rt4k = new RenderTexture(3840, 2160, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            foreach (var kit in FrontRoomsKitLibrary.AllNames())
            {
                var info = FrontRoomsKitLibrary.GetInfo(kit);
                var def = info?.inspect;
                if (def == null || !def.IsSet) continue;
                var go = FrontRoomsKitLibrary.Spawn(kit, stage.transform, Vector3.zero, Quaternion.identity, null, false, kit, false);
                if (go == null) continue;
                var y = def.Kind == InspectKind.Object ? 1.40f : 1.524f;
                go.transform.position = new Vector3(0f, y, def.Kind == InspectKind.Object ? .01f : 0f);
                var target = go.AddComponent<FrontRoomsInspectable>();
                target.Configure(kit, def, info);
                var face = target.Face;
                var eye = new Vector3(0f, 1.62f, 1.5f);
                var fov = 76f + def.FovDelta;
                if (def.Kind == InspectKind.Object)
                {
                    // The object kind: the object comes to the eye at its distance (rendered here by moving the camera the other way).
                    var d = def.BaseDist > 0f ? def.BaseDist : .3f;
                    var pose = new Pose(target.BoxCentreWorld + Vector3.forward * d, Quaternion.LookRotation(Vector3.back, Vector3.up));
                    Shoot(cam, rt1080, rt4k, pose, fov, outDir, kit + "_base", log, target, def, d);
                }
                else
                {
                    var input = FrontRoomsInspect.InputFor(target, new Pose(eye, Quaternion.LookRotation(face.position - eye)), 76f, 16f / 9f, .06f, false);
                    var r = FrontRoomsInspectFraming.Solve(input);
                    if (!r.ok) { log.AppendLine(kit + ": no pose (" + r.why + ")"); UnityEngine.Object.DestroyImmediate(go); continue; }
                    Shoot(cam, rt1080, rt4k, r.pose, fov, outDir, kit + "_base", log, target, def, r.distance);
                    var z = FrontRoomsInspectFraming.ZoomFor(input, def.SmallestCapMm, FrontRoomsInspectFraming.DesktopArcmin, 1080f, def.TexelsPerMetre, r.distance);
                    if (z.offered)
                    {
                        var zp = new Pose(face.position + r.direction * z.distance, r.pose.rotation);
                        Shoot(cam, rt1080, rt4k, zp, z.fov, outDir, kit + "_zoom", log, target, def, z.distance);
                    }
                }
                UnityEngine.Object.DestroyImmediate(go);
            }
            rt1080.Release(); rt4k.Release();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(stage);
            File.WriteAllText(Path.Combine(outDir, "lookdev.txt"), log.ToString());
            Debug.Log("[InspectLookdev]\n" + log);
        }
    }

    static void Shoot(Camera cam, RenderTexture rt1080, RenderTexture rt4k, Pose pose, float fov, string outDir, string name, StringBuilder log, FrontRoomsInspectable t, InspectDef def, float d)
    {
        cam.transform.SetPositionAndRotation(pose.position, pose.rotation);
        cam.fieldOfView = fov;
        foreach (var (rt, tag) in new[] { (rt1080, ""), (rt4k, "_4k") })
        {
            cam.targetTexture = rt;
            cam.Render();
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            cam.targetTexture = null;
            File.WriteAllBytes(Path.Combine(outDir, name + tag + ".png"), tex.EncodeToPNG());
            // Contrast inside the framed rect (flat and panel), from its projected corners.
            var contrast = 0f;
            if (def.Kind != InspectKind.Object)
            {
                var f = t.Face; var s = t.Size;
                float x0 = 1, x1 = 0, y0 = 1, y1 = 0;
                foreach (var sx in new[] { -.5f, .5f })
                    foreach (var sy in new[] { -.5f, .5f })
                    {
                        var v = FrontRoomsInspectFraming.Project(pose, fov, 16f / 9f, f.position + f.rotation * new Vector3(sx * s.x * .9f, sy * s.y * .9f, 0f));
                        x0 = Mathf.Min(x0, v.x); x1 = Mathf.Max(x1, v.x); y0 = Mathf.Min(y0, v.y); y1 = Mathf.Max(y1, v.y);
                    }
                var box = new RectInt(Mathf.Clamp((int)(x0 * rt.width), 0, rt.width - 2), Mathf.Clamp((int)(y0 * rt.height), 0, rt.height - 2), 1, 1);
                box.width = Mathf.Clamp((int)((x1 - x0) * rt.width), 1, rt.width - box.x);
                box.height = Mathf.Clamp((int)((y1 - y0) * rt.height), 1, rt.height - box.y);
                contrast = ContrastRatio(tex, box);
            }
            UnityEngine.Object.DestroyImmediate(tex);
            var pxmm = FrontRoomsInspectFraming.PxPerMm(d, fov, rt.height);
            log.AppendLine(name + tag + ": d " + d.ToString("0.000", Inv) + " m, FOV " + fov.ToString("0.0", Inv) + ", " + pxmm.ToString("0.00", Inv) + " px/mm, smallest cap "
                + def.SmallestCapMm.ToString("0.0", Inv) + " mm = " + (pxmm * def.SmallestCapMm).ToString("0.0", Inv) + " px"
                + (def.Kind != InspectKind.Object ? ", face p90/p5 " + contrast.ToString("0.00", Inv) : ""));
        }
    }
}
