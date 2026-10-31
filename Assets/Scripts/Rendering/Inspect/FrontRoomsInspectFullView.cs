using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The full view's mask (Red 2026-10-30: "如果再点一次，用户可以直接看到full bleed的只有显示内容的版本，而不是带有四周环境"): four plain
/// bands around the content, so a second tap on a close-up shows what is displayed and none of the room around it. The
/// camera does the framing (FrontRoomsInspect.ToggleFullView); the bands follow the content's rect on screen every frame, on a
/// screen-space canvas under every HUD canvas, so BACK stays on top. They fade with the camera's travel. Nothing is built
/// until the first full view.
/// </summary>
public static class FrontRoomsInspectFullView
{
    const float FadeSeconds = .25f;
    static readonly Color32 BandColour = new Color32(10, 10, 10, 255);

    static Canvas canvas;
    static readonly RectTransform[] bands = new RectTransform[4];
    static readonly Image[] images = new Image[4];
    static float alpha;

    /// <summary>The mask shows (the harness reads it).</summary>
    public static bool Showing => canvas != null && canvas.enabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        canvas = null;
        alpha = 0f;
    }

    /// <summary>Once per frame, after the cameras moved (FrontRoomsInspect.LateTick).</summary>
    public static void Tick(float dt)
    {
        var want = FrontRoomsInspect.FullView;
        alpha = Mathf.MoveTowards(alpha, want ? 1f : 0f, dt / FadeSeconds);
        var r = alpha > 0f ? FrontRoomsInspect.ContentViewportRect : default;
        if (alpha <= 0f || r.width <= 0f)
        {
            if (canvas != null && canvas.enabled) canvas.enabled = false;
            return;
        }
        Ensure();
        Place(0, Vector2.zero, new Vector2(Mathf.Clamp01(r.xMin), 1f));
        Place(1, new Vector2(Mathf.Clamp01(r.xMax), 0f), Vector2.one);
        Place(2, new Vector2(Mathf.Clamp01(r.xMin), 0f), new Vector2(Mathf.Clamp01(r.xMax), Mathf.Clamp01(r.yMin)));
        Place(3, new Vector2(Mathf.Clamp01(r.xMin), Mathf.Clamp01(r.yMax)), new Vector2(Mathf.Clamp01(r.xMax), 1f));
        Color c = BandColour;
        c.a = alpha;
        foreach (var image in images) image.color = c;
        if (!canvas.enabled) canvas.enabled = true;
    }

    static void Place(int i, Vector2 min, Vector2 max)
    {
        bands[i].anchorMin = min;
        bands[i].anchorMax = max;
    }

    static void Ensure()
    {
        if (canvas != null) return;
        var go = new GameObject("FrontRooms close-up full view");
        Object.DontDestroyOnLoad(go);
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = FrontRoomsInspectTerminal.SurroundSortingOrder;
        for (var i = 0; i < 4; i++)
        {
            var band = new GameObject("band", typeof(RectTransform), typeof(Image));
            band.transform.SetParent(go.transform, false);
            bands[i] = (RectTransform)band.transform;
            bands[i].offsetMin = bands[i].offsetMax = Vector2.zero;
            images[i] = band.GetComponent<Image>();
            images[i].raycastTarget = false;
        }
    }
}
