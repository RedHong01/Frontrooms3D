// FrontRoomsGlassSceneColor.cs -- GD3 step 5 (plan 10 sec 2.4, 4.3 "FrontRoomsGlassRefractionFeature", 4.8; build/04_stages.md
// sec 3): the scene behind cracked glass, for the crack mirrors.
//
// A crack face is an internal mirror: it shows the scene behind the glass in a new direction, never light of its own. So
// FrontRooms/Glass draws each fracture face from URP's opaque texture (CrackMirror). URP makes that texture only when asked,
// so this asks for it, per camera, only while a pane is held or cracked within Range of that camera: one colour copy after
// the skybox (the URP asset's 2x bilinear downsample), plus the depth texture the asset already makes. No renderer asset
// changes: the request is a no-op render pass enqueued from beginCameraRendering (URP's "inject a pass via scripting").
// The shader reads _FR_GlassSceneColor (1 = this camera has the textures). Without them (WebGL, reflection-probe captures,
// previews, overlay cameras) the cracks mirror the zone cube instead, dimmed by the lamps that light them.
// WebGL tier: never requested (#if UNITY_WEBGL; plan sec 4.8: no refraction on WebGL). Desktop is the reference.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public static class FrontRoomsGlassSceneColor
{
    /// <summary>Lab A/B switch (the fallback look); the game leaves it on.</summary>
    public static bool Enabled = true;
    /// <summary>A held or cracked pane farther than this from a camera (m) does not make that camera copy its colour.</summary>
    public static float Range = 30f;
    /// <summary>Cameras that got the request since start (lab statistics).</summary>
    public static int Requests { get; private set; }
    public static int Panes => panes.Count;

    static readonly List<Transform> panes = new List<Transform>();
    static readonly int SceneColorId = Shader.PropertyToID("_FR_GlassSceneColor");
    static RequestPass pass;
    static bool subscribed;

    /// <summary>Asks for the opaque and depth textures; records no work of its own.</summary>
    sealed class RequestPass : ScriptableRenderPass
    {
        public RequestPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) { }
    }

    /// <summary>A pane (its window root) needs the scene behind it: held, or cracked (S1, S2).</summary>
    public static void Add(Transform pane)
    {
#if !UNITY_WEBGL
        if (pane == null || panes.Contains(pane)) return;
        panes.Add(pane);
        if (subscribed) return;
        pass ??= new RequestPass();
        RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        subscribed = true;
#endif
    }

    /// <summary>The pane no longer needs it (released at S0, shattered, unloaded).</summary>
    public static void Remove(Transform pane)
    {
        if (!panes.Remove(pane) && pane != null) return;
        panes.RemoveAll(p => p == null);
        if (panes.Count > 0 || !subscribed) return;
        RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
        subscribed = false;
        Shader.SetGlobalFloat(SceneColorId, 0f);
    }

    static void OnBeginCamera(ScriptableRenderContext context, Camera cam)
    {
        var on = false;
        if (Enabled && (cam.cameraType == CameraType.Game || cam.cameraType == CameraType.SceneView)
            && cam.TryGetComponent(out UniversalAdditionalCameraData data) && data.renderType == CameraRenderType.Base
            && data.scriptableRenderer is UniversalRenderer renderer)
        {
            var eye = cam.transform.position;
            var r2 = Range * Range;
            for (var i = 0; i < panes.Count; i++)
                if (panes[i] != null && (panes[i].position - eye).sqrMagnitude <= r2) { on = true; break; }
            if (on)
            {
                renderer.EnqueuePass(pass);
                Requests++;
            }
        }
        Shader.SetGlobalFloat(SceneColorId, on ? 1f : 0f);
    }
}
