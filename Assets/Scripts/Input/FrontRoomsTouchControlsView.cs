using System;
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
    RectTransform controlsRoot;
    Image moveBase;
    Image moveKnob;
    Image sprintSocket;
    Image useButton;
    Text useButtonLabel;
    Image useRing;
    Image pauseButton;
    Text pauseLabel;
    Image sprintButton;
    Text sprintButtonLabel;
    Image restartCancelButton;
    Image menuWash;
    Image settingsBackdrop;
    Image settingsCloseButton;
    Text settingsCloseLabel;
    Image[] menuButtons = new Image[5];
    Text[] menuButtonLabels = new Text[5];
    Image[] settingsRowButtons = new Image[9];
    Text[] settingsRowLabels = new Text[9];
    Text[] settingsRowValueLabels = new Text[9];
    RectTransform useHitArea;
    RectTransform pauseHitArea;
    RectTransform startHitArea;
    RectTransform restartHitArea;
    RectTransform settingsHitArea;
    RectTransform caughtRestartHitArea;
    RectTransform restartCancelHitArea;
    RectTransform sprintButtonHitArea;
    RectTransform[] settingsRowHitAreas = new RectTransform[9];
    Sprite circle;
    Sprite ring;
    Sprite solid;
    Rect lastSafe;
    Vector2Int lastSize;

    // Motion is deliberately state driven. The touch state machine remains
    // immediate for gameplay, while the visual shell eases toward the new
    // state on unscaled time so pause/settings transitions still finish while
    // Time.timeScale is zero.
    float menuWashAlpha;
    float menuWashTarget;
    float settingsBackdropAlpha;
    float settingsBackdropTarget;
    float useAlpha;
    float useTarget;
    float stickAlpha;
    float stickTarget;
    float socketAlpha;
    float socketTarget;
    float pauseAlpha;
    float pauseTarget;
    float menuMotion;
    float menuMotionTarget;
    float holdProgressVisual;
    float sprintPulse;
    Vector2 usePosition;
    Vector2 pausePosition;
    Vector2 moveOriginPosition;
    Vector2 moveThumbPosition;
    Vector2 socketPosition;
    FrontRoomsTouchControls.MenuState lastMenuState;
    bool motionInitialized;

    const float MoveDiameter = 120f;
    const float KnobDiameter = 52f;
    const float SprintDiameter = 40f;
    const float UseDiameter = 72f;
    const float SprintOffset = 92f;
    const float MenuButtonWidth = 108f;
    const float MenuButtonHeight = 40f;
    const float PhoneWidth = 874f;
    const float PhoneHeight = 402f;

    bool ShouldShow => Application.isMobilePlatform || (Application.isEditor && showInEditor);

    void Awake()
    {
        controls = GetComponent<FrontRoomsTouchControls>();
        BuildSurface();
        FrontRoomsSettings.Changed += ApplySavedSettings;
        ApplySavedSettings();
        lastMenuState = controls.CurrentMenuState;
    }

    void OnDestroy()
    {
        FrontRoomsSettings.Changed -= ApplySavedSettings;
    }

    void ApplySavedSettings()
    {
        if (controls != null)
            controls.ApplySavedSettings();
        opacity = Mathf.Clamp01(FrontRoomsSettings.TouchOpacityPercent / 100f);
        if (controlsRoot != null)
            controlsRoot.localScale = Vector3.one * (FrontRoomsSettings.TouchControlsScalePercent / 100f);
        // Left-handed changes move the right/left action cluster even when
        // the display itself did not resize.
        lastSafe = new Rect();
        lastSize = Vector2Int.zero;
        ApplyImageAlpha(moveBase, opacity);
        ApplyImageAlpha(moveKnob, opacity + .12f);
        ApplyImageAlpha(sprintSocket, opacity + .06f);
        ApplyImageAlpha(sprintButton, opacity + .1f);
        ApplyImageAlpha(useButton, opacity + .1f);
        ApplyImageAlpha(pauseButton, opacity + .06f);
        for (var i = 0; i < settingsRowButtons.Length; i++)
            ApplyImageAlpha(settingsRowButtons[i], .035f + opacity * .08f);
    }

    static void ApplyImageAlpha(Image image, float alpha)
    {
        if (image == null) return;
        var color = image.color;
        color.a = Mathf.Clamp01(alpha);
        image.color = color;
    }

    void Update()
    {
        if (canvas == null) return;
        canvas.gameObject.SetActive(ShouldShow);
        if (!ShouldShow) return;
        UpdateSafeArea();
        UpdateState();
        TickMotion();
    }

    void BuildSurface()
    {
        var root = new GameObject("Mobile Touch UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        root.transform.SetParent(transform, false);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 490;
        controls.SetUICanvas(canvas);
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        // Figma's mobile masters are authored at 874x402 pt. Keeping this as
        // the logical canvas means a 72 pt USE ring stays 72 pt on a Retina
        // iPhone instead of being downscaled by a desktop 1920x1080 reference.
        scaler.referenceResolution = new Vector2(PhoneWidth, PhoneHeight);
        scaler.matchWidthOrHeight = .5f;

        // Keep the Figma phone master as the logical full-frame coordinate
        // space. Figma's 874x402 artwork already places every visible element
        // inside the iPhone safe area (62pt side insets / 21pt bottom inset).
        // A safe-area-sized parent would apply those insets a second time and
        // push the menu wash, settings card, and chips out of alignment.
        var safe = new GameObject("Phone Frame", typeof(RectTransform));
        safe.transform.SetParent(root.transform, false);
        safeRoot = safe.GetComponent<RectTransform>();
        var controlsObject = new GameObject("Touch Controls", typeof(RectTransform));
        controlsObject.transform.SetParent(safeRoot, false);
        controlsRoot = controlsObject.GetComponent<RectTransform>();
        controlsRoot.anchorMin = Vector2.zero;
        controlsRoot.anchorMax = Vector2.one;
        controlsRoot.offsetMin = controlsRoot.offsetMax = Vector2.zero;
        controlsRoot.localScale = Vector3.one * (FrontRoomsSettings.TouchControlsScalePercent / 100f);
        circle = CreateCircleSprite();
        ring = CreateRingSprite();
        solid = CreateSolidSprite();

        menuWash = FullImage("Touch / menu wash", new Color(.929f, .922f, .878f, 0f), PhoneWidth, PhoneHeight);
        settingsBackdrop = FullImage("Touch / settings card", new Color(.055f, .055f, .051f, 0f), 742f, 369f);
        settingsBackdrop.rectTransform.anchorMin = settingsBackdrop.rectTransform.anchorMax = new Vector2(.5f, .5f);
        settingsBackdrop.rectTransform.anchoredPosition = new Vector2(0f, 6.5f);

        moveBase = Circle("Touch / Move base", MoveDiameter, new Color(1f, 1f, 1f, opacity));
        moveBase.sprite = ring;
        moveKnob = Circle("Touch / Move thumb", KnobDiameter, new Color(1f, 1f, 1f, opacity + .12f));
        sprintSocket = Circle("Touch / Sprint socket", SprintDiameter, new Color(.956f, .875f, .231f, opacity + .06f));
        sprintSocket.sprite = ring;
        sprintButton = Circle("Touch / Sprint button", 64f, new Color(.956f, .875f, .231f, opacity + .1f));
        sprintButton.sprite = ring;
        useButton = Circle("Touch / USE", UseDiameter, new Color(.956f, .875f, .231f, opacity + .1f));
        useButton.sprite = ring;
        useButtonLabel = ButtonLabel(useButton, "USE", 17);
        sprintButtonLabel = ButtonLabel(sprintButton, "SPRINT", 17);
        useRing = Ring("Touch / USE hold ring", 92f, new Color(.956f, .875f, .231f, .85f));
        pauseButton = Circle("Touch / Pause", 32f, new Color(1f, 1f, 1f, opacity + .06f));
        pauseButton.sprite = CreateRoundedSprite();
        pauseLabel = ButtonLabel(pauseButton, "II", 14);
        pauseLabel.color = new Color(.039f, .039f, .039f, .95f);

        // The visible glyphs follow the Figma sizes; their touch targets keep
        // the separate 88/44 pt accessibility hit boxes.
        useHitArea = ControlHitArea("Touch / USE hit", 88f);
        pauseHitArea = ControlHitArea("Touch / Pause hit", 44f);
        startHitArea = FullArea("Touch / Start hit");
        restartHitArea = MenuHitArea("Touch / Restart hit", new Vector2(95.5f, -89f), new Vector2(88f, 44f));
        settingsHitArea = MenuHitArea("Touch / Settings hit", new Vector2(-1.5f, -89f), new Vector2(92f, 44f));
        restartCancelHitArea = MenuHitArea("Touch / Restart cancel hit", new Vector2(95.5f, -89f), new Vector2(88f, 44f));
        sprintButtonHitArea = ControlHitArea("Touch / Sprint button hit", 88f);
        caughtRestartHitArea = MenuHitArea("Touch / Caught restart hit", new Vector2(0f, -89f), new Vector2(100f, 44f));
        BuildMenuButtons();
        restartCancelButton = MenuButton("Touch / CANCEL RESTART", "CANCEL", new Vector2(0f, -89f));
        settingsCloseButton = MenuChip("Touch / SETTINGS CLOSE", "CLOSE", new Vector2(312.5f, 163f), out settingsCloseLabel);
        BuildSettingsRows();

        moveBase.gameObject.SetActive(true);
        moveKnob.gameObject.SetActive(true);
        sprintSocket.gameObject.SetActive(true);
        useRing.gameObject.SetActive(true);

        controls.SetUseHitArea(useHitArea);
        controls.SetPauseHitArea(pauseHitArea);
        controls.SetStartHitArea(startHitArea);
        controls.SetRestartHitArea(restartHitArea);
        controls.SetSettingsHitArea(settingsHitArea);
        controls.SetRestartCancelHitArea(restartCancelHitArea);
        controls.SetSprintButtonHitArea(sprintButtonHitArea);
        // The caught card uses the same restart callback, but has its own
        // target so it can be positioned independently from the pause card.
        controls.SetCaughtRestartHitArea(caughtRestartHitArea);
        for (var i = 0; i < settingsRowHitAreas.Length; i++)
            controls.BindSettingsRow(i, settingsRowHitAreas[i]);
    }

    Image Circle(string name, float diameter, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(controlsRoot, false);
        var image = go.GetComponent<Image>();
        image.sprite = circle;
        image.color = color;
        image.raycastTarget = false;
        image.rectTransform.sizeDelta = new Vector2(diameter, diameter);
        return image;
    }

    Image FullImage(string name, Color color, float width, float height)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(safeRoot, false);
        var image = go.GetComponent<Image>();
        image.sprite = solid;
        image.color = color;
        image.raycastTarget = false;
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(.5f, .5f);
        image.rectTransform.pivot = new Vector2(.5f, .5f);
        image.rectTransform.anchoredPosition = Vector2.zero;
        image.rectTransform.sizeDelta = new Vector2(width, height);
        return image;
    }

    Text ButtonLabel(Image button, string value, int size)
    {
        var textObject = new GameObject(button.name + " label", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(button.transform, false);
        var text = textObject.GetComponent<Text>();
        text.font = Resources.Load<Font>("Fonts/Bayon-Regular") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = value;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = size;
        text.fontStyle = FontStyle.Normal;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = Color.white;
        text.raycastTarget = false;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        return text;
    }

    RectTransform HitArea(string name, float diameter)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(safeRoot, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(diameter, diameter);
        return rect;
    }

    RectTransform ControlHitArea(string name, float diameter)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(controlsRoot, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(diameter, diameter);
        return rect;
    }

    RectTransform FullArea(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(safeRoot, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    RectTransform MenuHitArea(string name, Vector2 position, Vector2 size)
    {
        var rect = HitArea(name, 1f);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    void BuildMenuButtons()
    {
        menuButtons[0] = MenuButton("Touch / RESUME", "RESUME", new Vector2(-96.5f, -89f));
        menuButtons[1] = MenuButton("Touch / SETTINGS", "SETTINGS", new Vector2(-1.5f, -89f));
        menuButtons[2] = MenuButton("Touch / RESTART", "RESTART", new Vector2(95.5f, -89f));
        menuButtons[3] = MenuButton("Touch / TRY AGAIN", "TRY AGAIN", new Vector2(0f, -89f));
        menuButtons[4] = MenuButton("Touch / TAP TO START", "TAP TO START", new Vector2(0f, -119f));
    }

    Image MenuButton(string name, string label, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(safeRoot, false);
        var image = go.GetComponent<Image>();
        var titlePrompt = name.IndexOf("TAP TO START", StringComparison.Ordinal) >= 0;
        // The title prompt is deliberately a text-only affordance. Its full
        // screen hit area remains in the touch state machine, while the old
        // yellow chip is removed to match the Figma title skeleton.
        image.color = titlePrompt
            ? new Color(1f, 1f, 1f, 0f)
            : new Color(.956f, .875f, .231f, 1f);
        image.raycastTarget = false;
        var rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(MenuButtonWidthFor(name), MenuButtonHeight);
        var textObject = new GameObject(name + " label", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(go.transform, false);
        var text = textObject.GetComponent<Text>();
        text.font = Resources.Load<Font>("Fonts/Bayon-Regular") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 17;
        text.fontStyle = FontStyle.Normal;
        text.color = titlePrompt
            ? new Color(.957f, .945f, .910f, .95f)
            : new Color(.039f, .039f, .039f, .95f);
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        var textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        menuButtonLabels[ArrayIndexForMenuButton(name)] = text;
        return image;
    }

    Image MenuChip(string name, string label, Vector2 position, out Text labelText)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(safeRoot, false);
        var image = go.GetComponent<Image>();
        image.color = new Color(.956f, .875f, .231f, 1f);
        image.raycastTarget = false;
        var rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(65f, MenuButtonHeight);
        var textObject = new GameObject(name + " label", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(go.transform, false);
        labelText = textObject.GetComponent<Text>();
        labelText.font = Resources.Load<Font>("Fonts/Bayon-Regular") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        labelText.text = label;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.fontSize = 17;
        labelText.fontStyle = FontStyle.Normal;
        labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
        labelText.verticalOverflow = VerticalWrapMode.Overflow;
        labelText.color = new Color(.039f, .039f, .039f, .95f);
        labelText.raycastTarget = false;
        labelText.rectTransform.anchorMin = Vector2.zero;
        labelText.rectTransform.anchorMax = Vector2.one;
        labelText.rectTransform.offsetMin = labelText.rectTransform.offsetMax = Vector2.zero;
        return image;
    }

    static float MenuButtonWidthFor(string name)
    {
        if (name.IndexOf("RESUME", StringComparison.Ordinal) >= 0) return 75f;
        if (name.IndexOf("SETTINGS", StringComparison.Ordinal) >= 0) return 83f;
        if (name.IndexOf("RESTART", StringComparison.Ordinal) >= 0) return 79f;
        if (name.IndexOf("TRY AGAIN", StringComparison.Ordinal) >= 0) return 91f;
        if (name.IndexOf("TAP TO START", StringComparison.Ordinal) >= 0) return 112f;
        return MenuButtonWidth;
    }

    int ArrayIndexForMenuButton(string name)
    {
        if (name.IndexOf("SETTINGS", StringComparison.Ordinal) >= 0) return 1;
        if (name.IndexOf("RESTART", StringComparison.Ordinal) >= 0) return 2;
        if (name.IndexOf("TRY AGAIN", StringComparison.Ordinal) >= 0) return 3;
        if (name.IndexOf("TAP TO START", StringComparison.Ordinal) >= 0) return 4;
        return 0;
    }

    void BuildSettingsRows()
    {
        const int rowCount = 9;
        var labels = new[] { "TOUCH SIZE", "TOUCH OPACITY", "HANDEDNESS", "LOOK SPEED", "INVERT LOOK", "STICK", "SPRINT", "HAPTICS", "GYRO LOOK" };
        for (var i = 0; i < rowCount; i++)
        {
            // The Figma settings card uses two 337 pt columns. Keep each
            // target at least 44 pt tall so a thumb can hit either the label
            // or value without requiring pixel precision.
            var column = i < 5 ? -178.5f : 178.5f;
            var rowIndex = i < 5 ? i : i - 5;
            var row = MenuHitArea("Touch / Settings row " + i, new Vector2(column, 99f - rowIndex * 44f), new Vector2(337f, 44f));
            settingsRowHitAreas[i] = row;
            settingsRowButtons[i] = row.gameObject.AddComponent<Image>();
            settingsRowButtons[i].color = new Color(.956f, .875f, .231f, .035f);
            settingsRowButtons[i].raycastTarget = false;
            var labelObject = new GameObject("Touch / Settings row " + i + " label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(row, false);
            var label = labelObject.GetComponent<Text>();
            label.font = Resources.Load<Font>("Fonts/Bayon-Regular") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.text = labels[i];
            label.fontSize = 17;
            label.fontStyle = FontStyle.Normal;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = new Color(.956f, .945f, .91f, 1f);
            label.raycastTarget = false;
            label.rectTransform.anchorMin = new Vector2(0f, 0f);
            label.rectTransform.anchorMax = new Vector2(.6f, 1f);
            label.rectTransform.offsetMin = new Vector2(22f, 0f);
            label.rectTransform.offsetMax = Vector2.zero;
            settingsRowLabels[i] = label;

            var valueObject = new GameObject("Touch / Settings row " + i + " value", typeof(RectTransform), typeof(Text));
            valueObject.transform.SetParent(row, false);
            var value = valueObject.GetComponent<Text>();
            value.font = Resources.Load<Font>("Fonts/IBMPlexMono-Regular") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            value.text = MobileSettingValue(i);
            value.fontSize = 17;
            value.fontStyle = FontStyle.Normal;
            value.alignment = TextAnchor.MiddleRight;
            value.color = new Color(.956f, .945f, .91f, 1f);
            value.raycastTarget = false;
            value.rectTransform.anchorMin = new Vector2(.4f, 0f);
            value.rectTransform.anchorMax = new Vector2(1f, 1f);
            value.rectTransform.offsetMin = Vector2.zero;
            value.rectTransform.offsetMax = new Vector2(-22f, 0f);
            settingsRowValueLabels[i] = value;
        }
    }

    Image Ring(string name, float diameter, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(controlsRoot, false);
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

    Sprite CreateSolidSprite()
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = "FrontRooms Touch Solid" };
        texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(.5f, .5f), 1f);
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

    Sprite CreateRoundedSprite()
    {
        const int size = 64;
        const float radius = 10f;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "FrontRooms Touch Rounded Keycap" };
        texture.filterMode = FilterMode.Bilinear;
        var pixels = new Color[size * size];
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = Mathf.Max(radius - x, 0f, x - (size - 1 - radius));
                var dy = Mathf.Max(radius - y, 0f, y - (size - 1 - radius));
                var distance = Mathf.Sqrt(dx * dx + dy * dy);
                pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(radius + .75f - distance));
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
        Canvas.ForceUpdateCanvases();
        area = Screen.safeArea;
        size = new Vector2Int(Screen.width, Screen.height);
        lastSafe = area;
        lastSize = size;
        // Children use the full 874x402 Figma frame. Safe-area pixels are
        // still used for the two dynamic action clusters below, but the
        // parent itself must not be inset a second time.
        safeRoot.anchorMin = Vector2.zero;
        safeRoot.anchorMax = Vector2.one;
        safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;

        // Keep the authored Figma centers first, then clamp the hit bounds to
        // the live safe rectangle. Using `safeRight - 72` directly changes the
        // visual grid on devices whose inset is 44 pt instead of Figma's 62 pt.
        // The CanvasScaler converts the logical points back to physical pixels.
        var pixelsPerPoint = Mathf.Max(.01f, canvas.scaleFactor);
        var safeMinX = area.xMin / pixelsPerPoint;
        var safeMaxX = area.xMax / pixelsPerPoint;
        var safeMinY = area.yMin / pixelsPerPoint;
        var safeMaxY = area.yMax / pixelsPerPoint;
        var actionTargetX = controls.LeftHanded ? 134f : 740f;
        var actionTargetY = 140f;
        var actionX = Mathf.Clamp(actionTargetX, safeMinX + 44f, safeMaxX - 44f) * pixelsPerPoint;
        var actionY = Mathf.Clamp(actionTargetY, safeMinY + 44f, safeMaxY - 44f) * pixelsPerPoint;
        var pauseX = Mathf.Clamp(784f, safeMinX + 22f, safeMaxX - 22f) * pixelsPerPoint;
        var pauseY = Mathf.Clamp(368f, safeMinY + 22f, safeMaxY - 22f) * pixelsPerPoint;
        SetScreenPosition(useButton.rectTransform, new Vector2(actionX, actionY));
        SetScreenPosition(useRing.rectTransform, new Vector2(actionX, actionY));
        SetScreenPosition(useHitArea, new Vector2(actionX, actionY));
        SetScreenPosition(sprintButton.rectTransform, new Vector2(actionX, actionY + SprintOffset * pixelsPerPoint));
        SetScreenPosition(sprintButtonHitArea, new Vector2(actionX, actionY + SprintOffset * pixelsPerPoint));
        SetScreenPosition(pauseButton.rectTransform, new Vector2(pauseX, pauseY));
        SetScreenPosition(pauseHitArea, new Vector2(pauseX, pauseY));

        // Keep the animated positions in screen space; safe-area changes and
        // left-handed swaps are eased in TickMotion rather than teleporting.
        // Refresh the logical target on rotation, safe-area changes, or a
        // handedness switch. TickMotion keeps the visual target stable for
        // the rest of the frame while the touch hit area remains immediate.
        usePosition = new Vector2(actionX, actionY);
        pausePosition = new Vector2(pauseX, pauseY);
        motionInitialized = true;
    }

    void UpdateState()
    {
        var state = controls.CurrentMenuState;
        var playing = state == FrontRoomsTouchControls.MenuState.Playing;
        if (state != lastMenuState)
        {
            menuMotion = 0f;
            lastMenuState = state;
        }

        useTarget = playing && controls.CurrentUsePrompt.Visible ? 1f : 0f;
        pauseTarget = playing || state == FrontRoomsTouchControls.MenuState.Paused || state == FrontRoomsTouchControls.MenuState.Settings ? 1f : 0f;
        var origin = controls.StickOriginScreenPosition;
        var thumb = controls.StickThumbScreenPosition;
        stickTarget = playing && origin.sqrMagnitude > 1f ? 1f : 0f;
        socketTarget = playing && controls.SprintSocketMode && (stickTarget > 0f || controls.SprintSocketLatched) ? 1f : 0f;
        menuWashTarget = state == FrontRoomsTouchControls.MenuState.Paused
            || state == FrontRoomsTouchControls.MenuState.Settings
            || state == FrontRoomsTouchControls.MenuState.Caught ? 1f : 0f;
        settingsBackdropTarget = state == FrontRoomsTouchControls.MenuState.Settings ? 1f : 0f;
        menuMotionTarget = state == FrontRoomsTouchControls.MenuState.Title
            || state == FrontRoomsTouchControls.MenuState.Paused
            || state == FrontRoomsTouchControls.MenuState.Settings
            || state == FrontRoomsTouchControls.MenuState.Caught ? 1f : 0f;

        // Input hit areas change immediately. The visual layer below remains
        // alive until its alpha reaches zero, so a transition cannot flash.
        useHitArea.gameObject.SetActive(useTarget > 0f);
        pauseHitArea.gameObject.SetActive(pauseTarget > 0f);
        sprintButtonHitArea.gameObject.SetActive(playing && !controls.SprintSocketMode);
        var title = state == FrontRoomsTouchControls.MenuState.Title;
        var paused = state == FrontRoomsTouchControls.MenuState.Paused;
        var settings = state == FrontRoomsTouchControls.MenuState.Settings;
        var caught = state == FrontRoomsTouchControls.MenuState.Caught;
        var restartConfirmation = paused && controls.RestartConfirmationOpen;
        startHitArea.gameObject.SetActive(title);
        restartHitArea.gameObject.SetActive(paused && !settings && !restartConfirmation);
        settingsHitArea.gameObject.SetActive((paused && !restartConfirmation) || settings);
        restartCancelHitArea.gameObject.SetActive(restartConfirmation);
        caughtRestartHitArea.gameObject.SetActive(caught);
        // The same state-machine binding is the SETTINGS chip while paused
        // and the CLOSE chip once the settings card is open.
        settingsHitArea.anchoredPosition = settings ? new Vector2(312.5f, 163f) : new Vector2(-1.5f, -89f);
        settingsCloseButton.gameObject.SetActive(settings || settingsBackdropAlpha > .01f);

        if (stickTarget > 0f)
        {
            moveOriginPosition = origin;
            moveThumbPosition = thumb;
            socketPosition = origin + Vector2.up * SprintOffset * Mathf.Max(.01f, canvas.scaleFactor);
        }

        var prompt = controls.CurrentUsePrompt;
        useButtonLabel.text = MobileUseLabel(prompt);
        useButtonLabel.color = prompt.Locked ? new Color(1f, 1f, 1f, .6f) : new Color(.956f, .945f, .91f, 1f);
        useButton.color = prompt.Locked
            ? new Color(.35f, .35f, .34f, 1f)
            : controls.UseHeld ? new Color(.956f, .875f, .231f, 1f) : new Color(.956f, .945f, .91f, 1f);
        sprintSocket.color = controls.SprintSocketLatched ? new Color(.956f, .875f, .231f, 1f) : new Color(.956f, .875f, .231f, .75f);
        sprintButton.color = controls.SprintSocketLatched ? new Color(.956f, .875f, .231f, 1f) : new Color(.956f, .875f, .231f, .75f);

        for (var i = 0; i < settingsRowHitAreas.Length; i++)
        {
            if (settingsRowLabels[i] != null)
                settingsRowLabels[i].text = MobileSettingName(i);
            if (settingsRowValueLabels[i] != null)
                settingsRowValueLabels[i].text = MobileSettingValue(i);
        }

        useButton.gameObject.SetActive(useTarget > 0f || useAlpha > .01f);
        useButtonLabel.gameObject.SetActive(useTarget > 0f || useAlpha > .01f);
        useRing.gameObject.SetActive(useTarget > 0f || useAlpha > .01f);
        moveBase.gameObject.SetActive(stickTarget > 0f || stickAlpha > .01f);
        moveKnob.gameObject.SetActive(stickTarget > 0f || stickAlpha > .01f);
        sprintSocket.gameObject.SetActive(socketTarget > 0f || socketAlpha > .01f);
        sprintButton.gameObject.SetActive(playing && !controls.SprintSocketMode);
        sprintButtonLabel.gameObject.SetActive(playing && !controls.SprintSocketMode);
        pauseButton.gameObject.SetActive(pauseTarget > 0f || pauseAlpha > .01f);
        for (var j = 0; j < menuButtons.Length; j++)
            menuButtons[j].gameObject.SetActive(MenuButtonTarget(j, state, restartConfirmation) || menuMotion > .01f);
        restartCancelButton.gameObject.SetActive(restartConfirmation || menuMotion > .01f);
    }

    bool MenuButtonTarget(int index, FrontRoomsTouchControls.MenuState state, bool restartConfirmation)
    {
        if (index >= 0 && index <= 2) return state == FrontRoomsTouchControls.MenuState.Paused && !restartConfirmation;
        if (index == 3) return state == FrontRoomsTouchControls.MenuState.Caught;
        if (index == 4) return state == FrontRoomsTouchControls.MenuState.Title;
        return false;
    }

    void TickMotion()
    {
        var dt = Mathf.Clamp(Time.unscaledDeltaTime, 0f, .05f);
        var reduced = FrontRoomsSettings.CameraMotionPercent <= 0;
        var menuDuration = reduced ? .12f : .24f;
        var controlDuration = reduced ? .10f : .18f;
        var pressDuration = reduced ? .08f : .12f;

        menuMotion = MoveTowards(menuMotion, menuMotionTarget, dt / menuDuration);
        menuWashAlpha = MoveTowards(menuWashAlpha, menuWashTarget, dt / menuDuration);
        settingsBackdropAlpha = MoveTowards(settingsBackdropAlpha, settingsBackdropTarget, dt / menuDuration);
        useAlpha = MoveTowards(useAlpha, useTarget, dt / controlDuration);
        stickAlpha = MoveTowards(stickAlpha, stickTarget, dt / controlDuration);
        socketAlpha = MoveTowards(socketAlpha, socketTarget, dt / controlDuration);
        pauseAlpha = MoveTowards(pauseAlpha, pauseTarget, dt / controlDuration);

        var prompt = controls.CurrentUsePrompt;
        holdProgressVisual = Mathf.MoveTowards(holdProgressVisual, prompt.Visible && prompt.Hold ? prompt.Progress : 0f, dt / .07f);
        sprintPulse = Mathf.MoveTowards(sprintPulse, controls.SprintSocketLatched && !FrontRoomsSettings.ReduceFlashing ? 1f : 0f, dt / .18f);

        if (motionInitialized)
        {
            // Safe-area anchors are refreshed by UpdateSafeArea; re-apply the
            // stored screen-space positions after the per-state transforms.
            SetScreenPosition(useButton.rectTransform, usePosition);
            SetScreenPosition(useRing.rectTransform, usePosition);
            SetScreenPosition(useHitArea, usePosition);
            SetScreenPosition(pauseButton.rectTransform, pausePosition);
            SetScreenPosition(pauseHitArea, pausePosition);
        }

        if (stickTarget > .01f)
        {
            SetScreenPosition(moveBase.rectTransform, moveOriginPosition);
            SetScreenPosition(moveKnob.rectTransform, moveThumbPosition);
            SetScreenPosition(sprintSocket.rectTransform, socketPosition);
        }

        var useScale = controls.UseHeld ? .92f : 1f;
        useButton.rectTransform.localScale = Vector3.one * Mathf.Lerp(.88f, useScale, useAlpha);
        useButtonLabel.rectTransform.localScale = Vector3.one * Mathf.Lerp(.88f, 1f, useAlpha);
        useRing.rectTransform.localScale = Vector3.one * Mathf.Lerp(.92f, 1f, useAlpha);
        moveBase.rectTransform.localScale = Vector3.one * Mathf.Lerp(.84f, 1f, stickAlpha);
        moveKnob.rectTransform.localScale = Vector3.one * Mathf.Lerp(.78f, 1f, stickAlpha);
        sprintSocket.rectTransform.localScale = Vector3.one * (1f + .08f * sprintPulse);
        sprintButton.rectTransform.localScale = Vector3.one * (controls.SprintSocketLatched ? .94f : 1f);
        pauseButton.rectTransform.localScale = Vector3.one * Mathf.Lerp(.92f, 1f, pauseAlpha);

        SetImageAlpha(menuWash, menuWashAlpha * .98f);
        SetImageAlpha(settingsBackdrop, settingsBackdropAlpha * .97f);
        SetImageAlpha(settingsCloseButton, settingsBackdropAlpha);
        SetTextAlpha(settingsCloseLabel, settingsBackdropAlpha * .95f);
        SetImageAlpha(useButton, (opacity + .10f) * useAlpha);
        SetTextAlpha(useButtonLabel, useAlpha);
        useRing.fillAmount = holdProgressVisual;
        SetImageAlpha(useRing, .85f * useAlpha);
        SetImageAlpha(moveBase, opacity * stickAlpha);
        SetImageAlpha(moveKnob, (opacity + .12f) * stickAlpha);
        SetImageAlpha(sprintSocket, (opacity + .06f) * socketAlpha);
        SetImageAlpha(sprintButton, (opacity + .10f) * (playingVisible ? 1f : 0f));
        SetTextAlpha(sprintButtonLabel, playingVisible ? 1f : 0f);
        SetImageAlpha(pauseButton, (opacity + .06f) * pauseAlpha);
        SetTextAlpha(pauseLabel, pauseAlpha);

        var state = controls.CurrentMenuState;
        var restartConfirmation = state == FrontRoomsTouchControls.MenuState.Paused && controls.RestartConfirmationOpen;
        for (var i = 0; i < menuButtons.Length; i++)
        {
            var target = MenuButtonTarget(i, state, restartConfirmation) ? menuMotion : 0f;
            var image = menuButtons[i];
            var offset = reduced ? 0f : (1f - menuMotion) * -18f;
            image.rectTransform.localScale = Vector3.one * Mathf.Lerp(.94f, 1f, menuMotion);
            image.rectTransform.anchoredPosition = MenuPosition(i) + Vector2.up * offset;
            SetImageAlpha(image, target);
            SetTextAlpha(menuButtonLabels[i], target * .95f);
        }
        var cancelTarget = restartConfirmation ? menuMotion : 0f;
        restartCancelButton.rectTransform.localScale = Vector3.one * Mathf.Lerp(.94f, 1f, menuMotion);
        SetImageAlpha(restartCancelButton, cancelTarget);
        SetTextAlpha(restartCancelButton.transform.GetComponentInChildren<Text>(), cancelTarget * .95f);

        for (var row = 0; row < settingsRowButtons.Length; row++)
        {
            var rowProgress = Mathf.Clamp01((settingsBackdropAlpha - row * .025f) / .75f);
            var rowImage = settingsRowButtons[row];
            rowImage.gameObject.SetActive(rowProgress > .001f || settingsBackdropAlpha > .001f);
            SetImageAlpha(rowImage, (.035f + opacity * .08f) * rowProgress);
            SetTextAlpha(settingsRowLabels[row], rowProgress);
            SetTextAlpha(settingsRowValueLabels[row], rowProgress);
        }

        if (useAlpha <= .001f) { useButton.gameObject.SetActive(false); useButtonLabel.gameObject.SetActive(false); useRing.gameObject.SetActive(false); }
        if (stickAlpha <= .001f) { moveBase.gameObject.SetActive(false); moveKnob.gameObject.SetActive(false); }
        if (socketAlpha <= .001f) sprintSocket.gameObject.SetActive(false);
        if (pauseAlpha <= .001f) pauseButton.gameObject.SetActive(false);
        if (menuMotion <= .001f)
            for (var i = 0; i < menuButtons.Length; i++) menuButtons[i].gameObject.SetActive(false);
    }

    bool playingVisible => controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Playing && !controls.SprintSocketMode;

    Vector2 MenuPosition(int index)
    {
        if (index == 0) return new Vector2(-96.5f, -89f);
        if (index == 1) return new Vector2(-1.5f, -89f);
        if (index == 2) return new Vector2(95.5f, -89f);
        if (index == 3) return new Vector2(0f, -89f);
        return new Vector2(0f, -119f);
    }

    static float MoveTowards(float current, float target, float amount)
    {
        return Mathf.MoveTowards(current, target, Mathf.Max(0f, amount));
    }

    static void SetImageAlpha(Image image, float alpha)
    {
        if (image == null) return;
        var c = image.color;
        c.a = Mathf.Clamp01(alpha);
        image.color = c;
    }

    static void SetTextAlpha(Text text, float alpha)
    {
        if (text == null) return;
        var c = text.color;
        c.a = Mathf.Clamp01(alpha);
        text.color = c;
    }

    string MobileSettingName(int index)
    {
        switch (index)
        {
            case 0: return "TOUCH SIZE";
            case 1: return "TOUCH OPACITY";
            case 2: return "HANDEDNESS";
            case 3: return "LOOK SPEED";
            case 4: return "INVERT LOOK";
            case 5: return "STICK";
            case 6: return "SPRINT";
            case 7: return "HAPTICS";
            case 8: return "GYRO LOOK";
            default: return string.Empty;
        }
    }

    string MobileSettingValue(int index)
    {
        switch (index)
        {
            case 0: return FrontRoomsSettings.TouchControlsScalePercent + "%";
            case 1: return FrontRoomsSettings.TouchOpacityPercent + "%";
            case 2: return FrontRoomsSettings.TouchLeftHanded ? "LEFT" : "RIGHT";
            case 3: return FrontRoomsSettings.TouchLookSpeedPercent + "%";
            case 4: return FrontRoomsSettings.TouchInvertLook ? "ON" : "OFF";
            case 5: return FrontRoomsSettings.TouchFloatingStick ? "FLOATING" : "FIXED";
            case 6: return FrontRoomsSettings.TouchSprintSocket ? "SOCKET" : "BUTTON";
            case 7: return FrontRoomsSettings.TouchHaptics ? "ON" : "OFF";
            case 8: return FrontRoomsSettings.TouchGyroMode == 0 ? "OFF" : FrontRoomsSettings.TouchGyroMode == 1 ? "WHILE TOUCHING" : "ALWAYS";
            default: return string.Empty;
        }
    }

    static string MobileUseLabel(FrontRoomsTouchControls.UsePrompt prompt)
    {
        if (!prompt.Visible) return string.Empty;
        if (prompt.Locked || prompt.Kind == FrontRoomsTouchControls.UseKind.Locked) return "LOCKED";
        if (prompt.Hold) return prompt.TapMode ? "TAP" : "HOLD";
        var label = prompt.Label ?? string.Empty;
        if (label.IndexOf("OPEN", StringComparison.OrdinalIgnoreCase) >= 0) return "OPEN";
        if (label.IndexOf("SHUT", StringComparison.OrdinalIgnoreCase) >= 0) return "SHUT";
        if (label.IndexOf("TAKE", StringComparison.OrdinalIgnoreCase) >= 0) return "TAKE";
        return "USE";
    }

    void SetScreenPosition(RectTransform rect, Vector2 screenPosition)
    {
        if (rect == null || safeRoot == null) return;
        var parent = rect.parent as RectTransform ?? safeRoot;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, canvas.worldCamera, out var local);
        rect.anchoredPosition = local;
    }
}
