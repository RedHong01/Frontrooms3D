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
    Image moveBase;
    Image moveKnob;
    Image sprintSocket;
    Image useButton;
    Text useButtonLabel;
    Image useRing;
    Image pauseButton;
    Image sprintButton;
    Text sprintButtonLabel;
    Image restartCancelButton;
    Image[] menuButtons = new Image[4];
    Text[] menuButtonLabels = new Text[4];
    Image[] settingsRowButtons = new Image[9];
    Text[] settingsRowLabels = new Text[9];
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
    Rect lastSafe;
    Vector2Int lastSize;

    const float MoveDiameter = 120f;
    const float KnobDiameter = 52f;
    const float SprintDiameter = 40f;
    const float UseDiameter = 72f;
    const float SprintOffset = 92f;
    const float MenuButtonWidth = 250f;
    const float MenuButtonHeight = 58f;

    bool ShouldShow => Application.isMobilePlatform || (Application.isEditor && showInEditor);

    void Awake()
    {
        controls = GetComponent<FrontRoomsTouchControls>();
        BuildSurface();
        FrontRoomsSettings.Changed += ApplySavedSettings;
        ApplySavedSettings();
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
        if (safeRoot != null)
            safeRoot.localScale = Vector3.one * (FrontRoomsSettings.TouchControlsScalePercent / 100f);
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
        sprintButton = Circle("Touch / Sprint button", 64f, new Color(.956f, .875f, .231f, opacity + .1f));
        useButton = Circle("Touch / USE", UseDiameter, new Color(.956f, .875f, .231f, opacity + .1f));
        useButtonLabel = ButtonLabel(useButton, "USE", 12);
        sprintButtonLabel = ButtonLabel(sprintButton, "SPRINT", 10);
        useRing = Ring("Touch / USE hold ring", 92f, new Color(.956f, .875f, .231f, .85f));
        pauseButton = Circle("Touch / Pause", 32f, new Color(1f, 1f, 1f, opacity + .06f));

        // The visible glyphs follow the Figma sizes; their touch targets keep
        // the separate 88/44 pt accessibility hit boxes.
        useHitArea = HitArea("Touch / USE hit", 88f);
        pauseHitArea = HitArea("Touch / Pause hit", 44f);
        startHitArea = FullArea("Touch / Start hit");
        restartHitArea = MenuHitArea("Touch / Restart hit", new Vector2(-146f, -230f), new Vector2(250f, 58f));
        settingsHitArea = MenuHitArea("Touch / Settings hit", new Vector2(146f, -230f), new Vector2(250f, 58f));
        restartCancelHitArea = MenuHitArea("Touch / Restart cancel hit", new Vector2(146f, -230f), new Vector2(250f, 58f));
        sprintButtonHitArea = HitArea("Touch / Sprint button hit", 88f);
        caughtRestartHitArea = MenuHitArea("Touch / Caught restart hit", new Vector2(0f, -230f), new Vector2(360f, 72f));
        BuildMenuButtons();
        restartCancelButton = MenuButton("Touch / CANCEL RESTART", "CANCEL", new Vector2(146f, -230f));
        BuildSettingsRows();

        moveBase.gameObject.SetActive(false);
        moveKnob.gameObject.SetActive(false);
        sprintSocket.gameObject.SetActive(false);
        useRing.gameObject.SetActive(false);

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
        go.transform.SetParent(safeRoot, false);
        var image = go.GetComponent<Image>();
        image.sprite = circle;
        image.color = color;
        image.raycastTarget = false;
        image.rectTransform.sizeDelta = new Vector2(diameter, diameter);
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
        text.fontStyle = FontStyle.Bold;
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
        menuButtons[0] = MenuButton("Touch / RESTART", "RESTART", new Vector2(-146f, -230f));
        menuButtons[1] = MenuButton("Touch / SETTINGS", "SETTINGS", new Vector2(146f, -230f));
        menuButtons[2] = MenuButton("Touch / TRY AGAIN", "TRY AGAIN", new Vector2(0f, -230f));
        menuButtons[3] = MenuButton("Touch / TAP TO START", "TAP TO START", new Vector2(0f, -330f));
    }

    Image MenuButton(string name, string label, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(safeRoot, false);
        var image = go.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, .07f);
        image.raycastTarget = false;
        var rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(MenuButtonWidth, MenuButtonHeight);
        var textObject = new GameObject(name + " label", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(go.transform, false);
        var text = textObject.GetComponent<Text>();
        text.font = Resources.Load<Font>("Fonts/Bayon-Regular") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 18;
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(1f, 1f, 1f, .9f);
        text.raycastTarget = false;
        var textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        menuButtonLabels[ArrayIndexForMenuButton(name)] = text;
        return image;
    }

    int ArrayIndexForMenuButton(string name)
    {
        if (name.IndexOf("SETTINGS", StringComparison.Ordinal) >= 0) return 1;
        if (name.IndexOf("TRY AGAIN", StringComparison.Ordinal) >= 0) return 2;
        if (name.IndexOf("TAP TO START", StringComparison.Ordinal) >= 0) return 3;
        return 0;
    }

    void BuildSettingsRows()
    {
        const int rowCount = 9;
        var labels = new[] { "TOUCH SIZE", "TOUCH OPACITY", "HANDEDNESS", "LOOK SPEED", "INVERT LOOK", "STICK", "SPRINT", "HAPTICS", "GYRO LOOK" };
        for (var i = 0; i < rowCount; i++)
        {
            // The settings card is 920x720 in the existing HUD. Keep each
            // target wider than the text line so a thumb can hit either the
            // label or value without requiring pixel precision.
            var row = MenuHitArea("Touch / Settings row " + i, new Vector2(0f, 190f - i * 48f), new Vector2(760f, 44f));
            settingsRowHitAreas[i] = row;
            settingsRowButtons[i] = row.gameObject.AddComponent<Image>();
            settingsRowButtons[i].color = new Color(.956f, .875f, .231f, .035f);
            settingsRowButtons[i].raycastTarget = false;
            var labelObject = new GameObject("Touch / Settings row " + i + " label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(row, false);
            var label = labelObject.GetComponent<Text>();
            label.font = Resources.Load<Font>("Fonts/IBMPlexMono-Regular") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.text = labels[i];
            label.fontSize = 15;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.white;
            label.raycastTarget = false;
            label.rectTransform.anchorMin = new Vector2(0f, 0f);
            label.rectTransform.anchorMax = new Vector2(.6f, 1f);
            label.rectTransform.offsetMin = new Vector2(22f, 0f);
            label.rectTransform.offsetMax = Vector2.zero;
            settingsRowLabels[i] = label;
        }
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

        var controlX = controls.LeftHanded ? area.xMin + 134f : area.xMax - 134f;
        var controlY = area.yMin + area.height * .35f;
        SetScreenPosition(useButton.rectTransform, new Vector2(controlX, controlY));
        SetScreenPosition(useRing.rectTransform, new Vector2(controlX, controlY));
        SetScreenPosition(useHitArea, new Vector2(controlX, controlY));
        SetScreenPosition(sprintButton.rectTransform, new Vector2(controlX, controlY + SprintOffset));
        SetScreenPosition(sprintButtonHitArea, new Vector2(controlX, controlY + SprintOffset));
        SetScreenPosition(pauseButton.rectTransform, new Vector2(area.xMax - 36f, area.yMax - 36f));
        SetScreenPosition(pauseHitArea, new Vector2(area.xMax - 36f, area.yMax - 36f));
    }

    void UpdateState()
    {
        var playing = controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Playing;
        useButton.gameObject.SetActive(playing && controls.CurrentUsePrompt.Visible);
        useButtonLabel.gameObject.SetActive(playing && controls.CurrentUsePrompt.Visible);
        useRing.gameObject.SetActive(playing && controls.CurrentUsePrompt.Visible && controls.CurrentUsePrompt.Hold);
        useRing.fillAmount = Mathf.Clamp01(controls.CurrentUsePrompt.Progress);
        useHitArea.gameObject.SetActive(playing && controls.CurrentUsePrompt.Visible);
        sprintButton.gameObject.SetActive(playing && !controls.SprintSocketMode);
        sprintButtonLabel.gameObject.SetActive(playing && !controls.SprintSocketMode);
        sprintButtonHitArea.gameObject.SetActive(playing && !controls.SprintSocketMode);
        pauseButton.gameObject.SetActive(playing || controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Paused || controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Settings);
        pauseHitArea.gameObject.SetActive(pauseButton.gameObject.activeSelf);

        var title = controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Title;
        var paused = controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Paused;
        var settings = controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Settings;
        var caught = controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Caught;
        var restartConfirmation = paused && controls.RestartConfirmationOpen;
        startHitArea.gameObject.SetActive(title);
        restartHitArea.gameObject.SetActive(paused && !settings);
        settingsHitArea.gameObject.SetActive(paused && !settings && !restartConfirmation);
        restartCancelHitArea.gameObject.SetActive(restartConfirmation);
        caughtRestartHitArea.gameObject.SetActive(caught);
        menuButtons[0].gameObject.SetActive(paused && !settings);
        menuButtons[1].gameObject.SetActive(paused && !settings && !restartConfirmation);
        menuButtons[2].gameObject.SetActive(caught);
        menuButtons[3].gameObject.SetActive(title);
        restartCancelButton.gameObject.SetActive(restartConfirmation);
        for (var i = 0; i < settingsRowHitAreas.Length; i++)
        {
            settingsRowHitAreas[i].gameObject.SetActive(settings);
            settingsRowButtons[i].gameObject.SetActive(settings);
            if (settingsRowLabels[i] != null)
                settingsRowLabels[i].text = MobileSettingLabel(i);
        }

        var origin = controls.StickOriginScreenPosition;
        var thumb = controls.StickThumbScreenPosition;
        var hasStick = playing && origin.sqrMagnitude > 1f;
        moveBase.gameObject.SetActive(hasStick);
        moveKnob.gameObject.SetActive(hasStick);
        sprintSocket.gameObject.SetActive(playing && controls.SprintSocketMode && (hasStick || controls.SprintSocketLatched));
        if (hasStick)
        {
            SetScreenPosition(moveBase.rectTransform, origin);
            SetScreenPosition(moveKnob.rectTransform, thumb);
            SetScreenPosition(sprintSocket.rectTransform, origin + Vector2.up * SprintOffset);
        }
        var prompt = controls.CurrentUsePrompt;
        useButtonLabel.text = MobileUseLabel(prompt);
        useButtonLabel.color = prompt.Locked ? new Color(1f, 1f, 1f, .6f) : Color.white;
        useButton.color = prompt.Locked
            ? new Color(.55f, .55f, .55f, opacity + .05f)
            : new Color(.956f, .875f, .231f, opacity + (controls.UseHeld ? .22f : .1f));
        sprintSocket.color = new Color(.956f, .875f, .231f, opacity + (controls.SprintSocketLatched ? .22f : .06f));
        sprintButton.color = new Color(.956f, .875f, .231f, opacity + (controls.SprintSocketLatched ? .22f : .1f));
    }

    string MobileSettingLabel(int index)
    {
        switch (index)
        {
            case 0: return "TOUCH SIZE   " + FrontRoomsSettings.TouchControlsScalePercent + "%";
            case 1: return "TOUCH OPACITY   " + FrontRoomsSettings.TouchOpacityPercent + "%";
            case 2: return "HANDEDNESS   " + (FrontRoomsSettings.TouchLeftHanded ? "LEFT" : "RIGHT");
            case 3: return "LOOK SPEED   " + FrontRoomsSettings.TouchLookSpeedPercent + "%";
            case 4: return "INVERT LOOK   " + (FrontRoomsSettings.TouchInvertLook ? "ON" : "OFF");
            case 5: return "STICK   " + (FrontRoomsSettings.TouchFloatingStick ? "FLOATING" : "FIXED");
            case 6: return "SPRINT   " + (FrontRoomsSettings.TouchSprintSocket ? "SOCKET" : "BUTTON");
            case 7: return "HAPTICS   " + (FrontRoomsSettings.TouchHaptics ? "ON" : "OFF");
            case 8: return "GYRO LOOK   " + (FrontRoomsSettings.TouchGyroMode == 0 ? "OFF" : FrontRoomsSettings.TouchGyroMode == 1 ? "WHILE TOUCHING" : "ALWAYS");
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
        RectTransformUtility.ScreenPointToLocalPointInRectangle(safeRoot, screenPosition, canvas.worldCamera, out var local);
        rect.anchoredPosition = local;
    }
}
