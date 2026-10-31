using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Level transitions, phone tier (FrontRoomsTransitionKit.PhoneTier, 2026-10-31): a chunk's finest kit renderers show only
/// near the eye. ChunkBuild.Finish adds one to the chunk's "Transitions" root on the phone tier only; desktop, WebGL and
/// the Editor outside the handheld preview never get one, so their kits draw exactly as before.
///  - fine ring (FineShow, hidden past FineHide): the paper's white lip, curls and backs, floor scraps, tape, the chalk
///    line, the cable: millimetre detail, under a pixel on a phone beyond it;
///  - mid ring (MidShow / MidHide): hanging strips, loose carpet tiles, the frame's binder bars and saddles, wall angle,
///    corner guards and galvanised beads.
/// The distance is from the view camera to the nearest point of the renderer's mesh bounds (one renderer per chunk and
/// material, so a renderer shows while any of its pieces is in range; the map has unit scale). Checked every 4th frame,
/// staggered by chunk, and once when attached in Play Mode; only the renderers' enabled flags change (no collider, no map
/// state, no other renderer). No camera: everything shows, as built. Edit mode (tools): everything shows unless a tool
/// calls <see cref="EvaluateAll"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class FrontRoomsTransitionNearDetail : MonoBehaviour
{
    public const float FineShow = 12f, FineHide = 15f, MidShow = 24f, MidHide = 28f;
    const int Every = 4;

    Renderer[] renderers;
    Transform[] transforms;
    Bounds[] local;
    float[] showSq, hideSq;
    bool[] shown;
    int phase;
    static int nextPhase;

    /// <summary>Renderers these rings gate (tests, captures).</summary>
    public int Count => renderers != null ? renderers.Length : 0;

    /// <summary>How many of them show now (tests, captures).</summary>
    public int Shown
    {
        get
        {
            var n = 0;
            if (shown != null) foreach (var s in shown) if (s) n++;
            return n;
        }
    }

    /// <summary>Gate the given renderers of one chunk's kit root (fine and mid rings). Null when there is nothing to gate.</summary>
    public static FrontRoomsTransitionNearDetail Attach(GameObject kitRoot, List<Renderer> fine, List<Renderer> mid)
    {
        if (kitRoot == null) return null;
        var n = (fine != null ? fine.Count : 0) + (mid != null ? mid.Count : 0);
        if (n == 0) return null;
        var g = kitRoot.AddComponent<FrontRoomsTransitionNearDetail>();
        g.renderers = new Renderer[n];
        g.transforms = new Transform[n];
        g.local = new Bounds[n];
        g.showSq = new float[n];
        g.hideSq = new float[n];
        g.shown = new bool[n];
        g.phase = nextPhase++ & (Every - 1);
        var i = 0;
        if (fine != null) foreach (var r in fine) g.Add(i++, r, FineShow, FineHide);
        if (mid != null) foreach (var r in mid) g.Add(i++, r, MidShow, MidHide);
        if (Application.isPlaying && Eye(out var eye)) g.Apply(eye, true);
        return g;
    }

    void Add(int i, Renderer r, float show, float hide)
    {
        renderers[i] = r;
        transforms[i] = r != null ? r.transform : null;
        var mf = r != null ? r.GetComponent<MeshFilter>() : null;
        local[i] = mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.zero);
        showSq[i] = show * show;
        hideSq[i] = hide * hide;
        shown[i] = r == null || r.enabled;
    }

    void Update()
    {
        if (renderers == null || ((Time.frameCount + phase) & (Every - 1)) != 0) return;
        if (Eye(out var eye)) Apply(eye, false);
        else ShowAll();
    }

    /// <summary>Show or hide each renderer for an eye in world space; fresh = no hysteresis (a new chunk, a tool).</summary>
    public void Apply(Vector3 eye, bool fresh)
    {
        if (renderers == null) return;
        for (var i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            var t = transforms[i];
            if (r == null || t == null) continue;
            var d = local[i].SqrDistance(t.InverseTransformPoint(eye));
            var want = fresh || !shown[i] ? d <= showSq[i] : d <= hideSq[i];
            if (want == shown[i] && r.enabled == want) continue;
            shown[i] = want;
            r.enabled = want;
        }
    }

    /// <summary>Every gated renderer on (no camera).</summary>
    public void ShowAll()
    {
        if (renderers == null) return;
        for (var i = 0; i < renderers.Length; i++)
        {
            if (shown[i]) continue;
            shown[i] = true;
            if (renderers[i] != null) renderers[i].enabled = true;
        }
    }

    /// <summary>Tools (Stats, tests): apply every ring set under root for one eye, as the phone would draw from there.</summary>
    public static int EvaluateAll(GameObject root, Vector3 eye)
    {
        var n = 0;
        if (root == null) return 0;
        foreach (var g in root.GetComponentsInChildren<FrontRoomsTransitionNearDetail>(true))
        {
            g.Apply(eye, true);
            n++;
        }
        return n;
    }

    /// <summary>The player's eye: the game's view camera (enabled, no target texture, the highest depth), else Camera.main.</summary>
    static bool Eye(out Vector3 eye)
    {
        var cam = FrontRoomsHeldItem.ViewCamera;
        if (cam == null) cam = Camera.main;
        eye = cam != null ? cam.transform.position : Vector3.zero;
        return cam != null;
    }
}
