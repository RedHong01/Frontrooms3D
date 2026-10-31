using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Ray-traced glass (G14) registers MeshRenderers only, so the skinned Relay body would vanish from the glass
/// reflections (production_b/10_plan.md section 5, "Glass RT"). In Play on a Mac where G14 runs, this hidden
/// stand-in carries a baked copy of the body's LOD1 (at most 15 Hz) into the TLAS through the public
/// FrontRoomsGlassRT API: Dynamic | Relay | Deforming, the BLAS refit on every bake. It draws in no camera
/// (renderingLayerMask 0, which URP's passes filter on; no shadows) and it is not under the rig, so the map's own
/// Relay registration never sees it. Nothing in FrontRoomsGlassRTSystem changes; off-Mac it is never created.
/// </summary>
public sealed class FrontRoomsRelayGlassMirror : MonoBehaviour
{
    const float BakeInterval = 1f / 15f;

    FrontRoomsRelayBody body;
    SkinnedMeshRenderer source;
    Mesh mesh;
    MeshRenderer meshRenderer;
    int handle = -1;
    float timer;

    public int Handle => handle;

    public static FrontRoomsRelayGlassMirror Create(FrontRoomsRelayBody body)
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        if (body == null || !FrontRoomsGlassRT.Supported) return null;
        var src = body.MirrorSource();
        if (src == null || src.sharedMesh == null) return null;
        var go = new GameObject("RELAY BODY / glass mirror");
        go.layer = src.gameObject.layer;
        var m = go.AddComponent<FrontRoomsRelayGlassMirror>();
        m.body = body;
        m.source = src;
        m.mesh = new Mesh { name = "Relay glass mirror" };
        m.mesh.MarkDynamic();
        go.AddComponent<MeshFilter>().sharedMesh = m.mesh;
        m.meshRenderer = go.AddComponent<MeshRenderer>();
        m.meshRenderer.sharedMaterials = src.sharedMaterials;
        m.meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        m.meshRenderer.receiveShadows = false;
        m.meshRenderer.renderingLayerMask = 0u;
        m.meshRenderer.lightProbeUsage = LightProbeUsage.Off;
        m.meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        m.meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        return m;
#else
        return null;
#endif
    }

    void LateUpdate()
    {
        if (body == null || source == null) { Destroy(gameObject); return; }
        var on = FrontRoomsGlassRT.Supported && FrontRoomsGlassRT.Quality != FrontRoomsGlassRTQuality.Off && body.isActiveAndEnabled;
        if (!on)
        {
            if (handle >= 0) FrontRoomsGlassRT.SetEnabled(handle, false);
            return;
        }
        var t = source.transform;
        transform.SetPositionAndRotation(t.position, t.rotation);
        timer -= Time.deltaTime;
        if (handle >= 0 && timer > 0f) return;
        timer = BakeInterval;
        source.BakeMesh(mesh, true);
        if (handle < 0)
        {
            mesh.RecalculateBounds();
            handle = FrontRoomsGlassRT.Register(meshRenderer, FrontRoomsGlassRTFlags.Dynamic | FrontRoomsGlassRTFlags.Relay | FrontRoomsGlassRTFlags.Deforming);
            if (handle < 0) { enabled = false; return; }
        }
        else
        {
            FrontRoomsGlassRT.SetEnabled(handle, true);
            FrontRoomsGlassRT.RefitMesh(mesh);
        }
    }

    void OnDestroy()
    {
        if (handle >= 0) FrontRoomsGlassRT.Unregister(handle);
        handle = -1;
        if (mesh != null) Destroy(mesh);
    }
}
