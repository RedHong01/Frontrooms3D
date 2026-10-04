using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime visual shell for <see cref="FrontRoomsTouchControls"/>.
/// Hit testing stays in the touch state machine; this view only supplies the
/// safe-area canvas and the editable visual targets from the mobile spec.
/// </summary>
[DefaultExecutionOrder(-450)]
[RequireComponent(typeof(FrontRoomsTouchControls))]
public sealed class FrontRoomsTouchControlsView : MonoBehaviour
{
    [SerializeField] bool showInEditor;
    [SerializeField, Range(.15f, .8f)] float opacity = .34f;

    FrontRoomsTouchControls controls;
    Canvas canvas;
    RectTransform safeRoot;
    Image moveBase;
    Image moveKnob;
    Image sprintSocket;
    Image useButton;
    Image useRing;
    Image pauseButton;
    RectTransform useHitArea;
    RectTransform pauseHitArea;
    Sprite circle;
    Rect lastSafe;
    Vector2Int lastSize;

    const float MoveDiameter = 120f;
    const float KnobDiameter = 52f;
    const float SprintDiameter = 40f;
    const float UseDiameter = 72f;
    const float SprintOffset = 92f;

    bool ShouldShow => Application.isMobilePlatform || (Application.isEditor && showInEditor);

    void Awake()
    {
        controls = GetComponent<FrontRoomsTouchControls>();
        BuildSurface();
    }

    void Update()
    {
        if (canvas == null) return;
        canvas.gameObject.SetActive(ShouldShow);
        if (!ShouldShow) return;
        UpdateSafeArea();
        UpdateState();
    }

    void BuildSurface()
    {
        var root = new GameObject("Mobile Touch UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        root.transform.SetParent(transform, false);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 490;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = .5f;

        var safe = new GameObject("Safe Area", typeof(RectTransform));
        safe.transform.SetParent(root.transform, false);
        safeRoot = safe.GetComponent<RectTransform>();
        circle = CreateCircleSprite();

        moveBase = Circle("Touch / Move base", MoveDiameter, new Color(1f, 1f, 1f, opacity));
        moveKnob = Circle("Touch / Move thumb", KnobDiameter, new Color(1f, 1f, 1f, opacity + .12f));
        sprintSocket = Circle("Touch / Sprint socket", SprintDiameter, new Color(.956f, .875f, .231f, opacity + .06f));
        useButton = Circle("Touch / USE", UseDiameter, new Color(.956f, .875f, .231f, opacity + .1f));
        useRing = Ring("Touch / USE hold ring", 92f, new Color(.956f, .875f, .231f, .85f));
        pauseButton = Circle("Touch / Pause", 32f, new Color(1f, 1f, 1f, opacity + .06f));

        // The visible glyphs follow the Figma sizes; their touch targets keep
        // the separate 88/44 pt accessibility hit boxes.
        useHitArea = HitArea("Touch / USE hit", 88f);
        pauseHitArea = HitArea("Touch / Pause hit", 44f);

        moveBase.gameObject.SetActive(false);
        moveKnob.gameObject.SetActive(false);
        sprintSocket.gameObject.SetActive(false);
        useRing.gameObject.SetActive(false);

        controls.SetUseHitArea(useHitArea);
        controls.SetPauseHitArea(pauseHitArea);
    }

    Image Circle(string name, float diameter, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(safeRoot, false);
        var image = go.GetComponent<Image>();
        image.sprite = circle;
        image.color = color;
        image.raycastTarget = false;
        image.rectTransform.sizeDelta = new Vector2(diameter, diameter);
        return image;
    }

    RectTransform HitArea(string name, float diameter)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(safeRoot, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(diameter, diameter);
        return rect;
    }

    Image Ring(string name, float diameter, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(safeRoot, false);
        var image = go.GetComponent<Image>();
        image.sprite = CreateRingSprite();
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Radial360;
        image.fillOrigin = 2;
        image.fillClockwise = true;
        image.fillAmount = 0f;
        image.color = color;
        image.raycastTarget = false;
        image.rectTransform.sizeDelta = new Vector2(diameter, diameter);
        return image;
    }

    Sprite CreateCircleSprite()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "FrontRooms Touch Circle" };
        texture.filterMode = FilterMode.Bilinear;
        var pixels = new Color[size * size];
        var center = (size - 1) * .5f;
        var radius = center - 1f;
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(radius + 1f - distance));
            }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(.5f, .5f), size);
    }

    Sprite CreateRingSprite()
    {
        const int size = 96;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "FrontRooms Touch Hold Ring" };
        texture.filterMode = FilterMode.Bilinear;
        var pixels = new Color[size * size];
        var center = (size - 1) * .5f;
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                var alpha = Mathf.Clamp01(1f - Mathf.Abs(d - .86f) / .055f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(.5f, .5f), size);
    }

    void UpdateSafeArea()
    {
        var area = Screen.safeArea;
        var size = new Vector2Int(Screen.width, Screen.height);
        if (area == lastSafe && size == lastSize) return;
        lastSafe = area;
        lastSize = size;
        var screen = new Vector2(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
        safeRoot.anchorMin = new Vector2(area.xMin / screen.x, area.yMin / screen.y);
        safeRoot.anchorMax = new Vector2(area.xMax / screen.x, area.yMax / screen.y);
        safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;

        SetScreenPosition(useButton.rectTransform, new Vector2(area.xMax - 134f, area.yMin + area.height * .35f));
        SetScreenPosition(useRing.rectTransform, new Vector2(area.xMax - 134f, area.yMin + area.height * .35f));
        SetScreenPosition(useHitArea, new Vector2(area.xMax - 134f, area.yMin + area.height * .35f));
        SetScreenPosition(pauseButton.rectTransform, new Vector2(area.xMax - 36f, area.yMax - 36f));
        SetScreenPosition(pauseHitArea, new Vector2(area.xMax - 36f, area.yMax - 36f));
    }

    void UpdateState()
    {
        var playing = controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Playing;
        useButton.gameObject.SetActive(playing && controls.CurrentUsePrompt.Visible);
        useRing.gameObject.SetActive(playing && controls.CurrentUsePrompt.Visible && controls.CurrentUsePrompt.Hold);
        useRing.fillAmount = Mathf.Clamp01(controls.CurrentUsePrompt.Progress);
        useHitArea.gameObject.SetActive(playing && controls.CurrentUsePrompt.Visible);
        pauseButton.gameObject.SetActive(playing || controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Paused || controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Settings);
        pauseHitArea.gameObject.SetActive(pauseButton.gameObject.activeSelf);

        var origin = controls.StickOriginScreenPosition;
        var thumb = controls.StickThumbScreenPosition;
        var hasStick = playing && origin.sqrMagnitude > 1f;
        moveBase.gameObject.SetActive(hasStick);
        moveKnob.gameObject.SetActive(hasStick);
        sprintSocket.gameObject.SetActive(hasStick || controls.SprintSocketLatched);
        if (hasStick)
        {
            SetScreenPosition(moveBase.rectTransform, origin);
            SetScreenPosition(moveKnob.rectTransform, thumb);
            SetScreenPosition(sprintSocket.rectTransform, origin + Vector2.up * SprintOffset);
        }
        useButton.color = new Color(.956f, .875f, .231f, opacity + (controls.UseHeld ? .22f : .1f));
        sprintSocket.color = new Color(.956f, .875f, .231f, opacity + (controls.SprintSocketLatched ? .22f : .06f));
    }

    void SetScreenPosition(RectTransform rect, Vector2 screenPosition)
    {
        if (rect == null || safeRoot == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(safeRoot, screenPosition, canvas.worldCamera, out var local);
        rect.anchoredPosition = local;
    }
}
