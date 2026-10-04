// Per-camera state of the desktop ray-traced glass reflections (design 10 §1.8). Added at runtime by
// FrontRoomsGlassRT.OptIn (FrontRoomsPostStack.ConfigureCamera) on macOS only; a camera without this component
// never traces, so the Scene view, previews, probe captures and the Autopilot shot cameras stay untouched.
// Each camera owns its own persistent targets, so a second camera never reallocates the first one's (R17, N10).
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[AddComponentMenu("")]
public sealed class FrontRoomsGlassRTCamera : MonoBehaviour
{
    internal int id = -1;
    internal RTHandle depth, normal, raw, aux, rawAA, outTex, ids;
    internal int width, height;
    internal bool hasAA, hasIds;
    internal int reallocations;
    internal long targetBytes;

    // temporal accumulation while the camera and the reflected scene are still
    internal Matrix4x4 lastViewProj;
    internal uint lastSignature;
    internal int accumulation;
    internal int frameIndex;

    // last frame (harness / stats)
    internal ulong lastSeq;
    internal RectInt lastRect;
    internal int lastVisibleReceivers;
    internal float lastWeight;

    internal static RTHandle Alloc(int w, int h, GraphicsFormat format, bool randomWrite, string name)
    {
        var handle = RTHandles.Alloc(w, h, colorFormat: format, filterMode: FilterMode.Point, wrapMode: TextureWrapMode.Clamp,
            enableRandomWrite: randomWrite, useMipMap: false, autoGenerateMips: false, name: name);
        if (handle != null && handle.rt != null && !handle.rt.IsCreated()) handle.rt.Create();
        return handle;
    }

    /// <summary>(Re)allocate the targets for this size. True when the native side must get new pointers.</summary>
    internal bool EnsureTargets(int w, int h, bool wantAA, bool wantIds)
    {
        if (w == width && h == height && depth != null && (hasAA || !wantAA) && (hasIds || !wantIds)) return false;
        ReleaseTargets();
        width = w; height = h;
        depth = Alloc(w, h, GraphicsFormat.R32_SFloat, false, "FRGlassRT GlassDepth");
        normal = Alloc(w, h, GraphicsFormat.R16G16B16A16_SFloat, false, "FRGlassRT GlassNormal");
        raw = Alloc(w, h, GraphicsFormat.R16G16B16A16_SFloat, true, "FRGlassRT raw");
        aux = Alloc(w, h, GraphicsFormat.R16G16B16A16_SFloat, true, "FRGlassRT aux");
        outTex = Alloc(w, h, GraphicsFormat.R16G16B16A16_SFloat, true, "_FR_GlassRTReflection");
        hasAA = wantAA;
        hasIds = wantIds;
        if (wantAA) rawAA = Alloc(w, h, GraphicsFormat.R16G16B16A16_SFloat, true, "FRGlassRT rawAA");
        if (wantIds) ids = Alloc(w, h, GraphicsFormat.R32G32B32A32_SFloat, true, "FRGlassRT ids");
        long px = (long)w * h;
        targetBytes = px * (4 + 8 * 4 + (wantAA ? 8 : 0) + (wantIds ? 16 : 0));
        reallocations++;
        accumulation = 0;
        return true;
    }

    internal void ReleaseTargets()
    {
        RTHandles.Release(depth); RTHandles.Release(normal); RTHandles.Release(raw); RTHandles.Release(aux);
        RTHandles.Release(outTex); RTHandles.Release(rawAA); RTHandles.Release(ids);
        depth = normal = raw = aux = outTex = rawAA = ids = null;
        width = height = 0;
        hasAA = hasIds = false;
    }

    void OnDestroy()
    {
        FrontRoomsGlassRTSystem.ReleaseCamera(this);
        ReleaseTargets();
    }
}
#endif
