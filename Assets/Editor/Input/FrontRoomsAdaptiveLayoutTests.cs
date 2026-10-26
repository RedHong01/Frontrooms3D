using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Geometry/input regression checks without opening a scene or entering Play Mode.
/// Batch: -executeMethod FrontRoomsAdaptiveLayoutTests.RunBatch -quit.
/// The Duo profiles carry what the iOS 27.1 Simulator reports (2026-10-24); the synthetic ones exercise resizability.
/// Writes Verification/adaptive-layout-tests.json; native fold geometry and visual QA
/// still require the Xcode Duo simulator and hardware.
/// </summary>
public static class FrontRoomsAdaptiveLayoutTests
{
    const float Epsilon = .05f;
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [Serializable]
    sealed class Report
    {
        public string verdict;
        public string scope = "Synthetic point-space geometry and input-state regression; no native simulator, rendering, or hardware validation.";
        public int passed, failed;
        public List<string> checks = new List<string>();
    }

    sealed class HostStub : IFrontRoomsTouchHost
    {
        public int GameSettingCount => 6;
        public string GameSettingSection(int index) => index < 3 ? "COMFORT" : "INPUT";
        public string GameSettingLabel(int index) => "GAME SETTING " + index;
        public string GameSettingValue(int index) => "100%";
        public bool GameSettingStepped(int index) => true;
        public void StepGameSetting(int index, int direction) { }
        public string PauseMeta => "LAYOUT TEST";
        public FrontRoomsTouchCaught CaughtStats => default;
        public Camera BackdropCamera => null;
        public bool RelayEverReleased => false;
        public bool CreepCounts => false;
        public void HudKeepOut(List<Rect> into) { }
    }

    static Report report;

    [MenuItem("FrontRooms 3D/Mobile/Test adaptive layout")]
    public static void Run() => Execute(false);

    public static void RunBatch() => Execute(true);

    static void Execute(bool throwOnFail)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || FrontRoomsHandheld.Forced)
            throw new InvalidOperationException("Run adaptive layout checks outside Play Mode and outside an active forced-device harness.");

        report = new Report();
        var root = new GameObject("ADAPTIVE LAYOUT TEST") { hideFlags = HideFlags.HideAndDontSave };
        root.SetActive(false); // Keep EnhancedTouch, gyro, and the game lifecycle untouched.
        try
        {
            var controls = root.AddComponent<FrontRoomsTouchControls>();
            controls.Host = new HostStub();
            var profiles = new[]
            {
                FrontRoomsHandheld.IPhonePro,
                FrontRoomsHandheld.IPhoneDuoOuter,
                FrontRoomsHandheld.IPhoneDuoOuterFlipped,
                FrontRoomsHandheld.IPhoneDuoOuterFlippedSettled,
                FrontRoomsHandheld.IPhoneDuoInner,
                FrontRoomsHandheld.IPhoneDuoInnerCameraOn,
                FrontRoomsHandheld.IPhoneDuoInnerPortrait,
                Profile("Duo portrait synthetic", 466, 678, 12, 32, 24, 21),
                Profile("Narrow split synthetic", 320, 640, 12, 16, 8, 21),
                Profile("Asymmetric synthetic", 678, 466, 110, 12, 8, 21),
                Profile("Short split synthetic", 678, 320, 80, 8, 8, 21)
            };
            foreach (var profile in profiles) CheckProfile(controls, profile);
            CheckDisplayShape();
            CheckAspectFov();
            FrontRoomsHandheld.Force(FrontRoomsHandheld.IPhonePro);
            CheckBaseline();
            CheckResizeContinuity(controls);
        }
        catch (Exception e)
        {
            Check(false, "Unhandled exception: " + e);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            FrontRoomsHandheld.ClearForce();
        }

        var passed = report.failed == 0 && report.passed > 0;
        report.verdict = (passed ? "PASS" : "FAIL") + ": " + report.passed + " passed, " + report.failed + " failed";
        var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "adaptive-layout-tests.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        if (passed) Debug.Log("[AdaptiveLayoutTests] " + report.verdict + " — " + path);
        else Debug.LogError("[AdaptiveLayoutTests] " + report.verdict + "\n" + string.Join("\n", report.checks.FindAll(s => s.StartsWith("FAIL ", StringComparison.Ordinal))));
        if (!passed && throwOnFail) throw new Exception(report.verdict);
    }

    static FrontRoomsHandheld.Profile Profile(string name, int width, int height, float left, float right, float top, float bottom) =>
        new FrontRoomsHandheld.Profile(name, width * 3, height * 3, 3f, left, right, top, bottom, false);

    static void CheckProfile(FrontRoomsTouchControls controls, FrontRoomsHandheld.Profile profile)
    {
        FrontRoomsHandheld.Force(profile);
        Invoke(controls, "RefreshLayout");
        var frame = FrontRoomsHandheld.ScreenPoints;
        // The layout's safe area is the bleed one: iOS's, less a camera band it folded in (the Duo's outer display).
        var shape = FrontRoomsHandheld.Shape;
        var safe = shape.BleedSafe;
        foreach (var handed in new[] { false, true })
        foreach (var controlScale in new[] { .8f, 1f, 1.4f })
        {
            var l = new FrontRoomsTouchLayout(frame, safe, controlScale * FrontRoomsHandheld.DeviceControlsScale,
                handed, FrontRoomsHandheld.IsTablet, 0f);
            var name = profile.Name + (handed ? " left" : " right") + " scale=" + controlScale;
            Check(l.UseHitRadius * 2f >= 44f && l.SprintButtonHitRadius * 2f >= 44f && l.PauseHitSize >= 44f,
                name + ": gameplay hit targets stay at least 44 pt");
            Check(Inside(safe, Box(l.UseCenter, Vector2.one * l.UseHitRadius * 2f)) &&
                Inside(safe, Box(l.InspectBackCenter, Vector2.one * l.UseHitRadius * 2f)) &&
                Inside(safe, Box(l.SprintButtonCenter, Vector2.one * l.SprintButtonHitRadius * 2f)),
                name + ": USE, inspect BACK and sprint hit targets stay in safe area");
            var pauseBounds = new Rect(0f, safe.yMin, frame.x, safe.height);
            Check(Inside(pauseBounds, Box(l.PauseCenter, Vector2.one * l.PauseHitSize)) &&
                Mathf.Abs(l.PauseCenterBase.x - (frame.x - 28f)) < Epsilon && Vector2.Distance(l.PauseCenter, l.PauseCenterBase) < 24f,
                name + ": pause target retains 44 pt at right screen bleed with vertical safe bounds, moving only for a camera or corner");
            Check(CornerGap(shape, l.PauseCenter, l.PauseHitSize * .5f) >= FrontRoomsHandheld.HudCornerGap - .01f,
                name + ": pause keeps the HUD's 16 pt from a rounded corner (gap " + CornerGap(shape, l.PauseCenter, l.PauseHitSize * .5f).ToString("0.0") + ")");
            Check(CircleClear(shape, l.PauseCenter, l.PauseHitSize * .5f) && CircleClear(shape, l.UseCenter, Mathf.Max(l.UseHitRadius, l.HoldRingRadius)) &&
                CircleClear(shape, l.InspectBackCenter, l.UseHitRadius) && CircleClear(shape, l.SprintButtonCenter, l.SprintButtonHitRadius) &&
                CircleClear(shape, l.StickRest, l.StickRadius),
                name + ": pause, USE, inspect BACK, RUN and the stick's rest clear every camera (12 pt) and rounded corner");
            Check(Untouched(shape, l.UseCenterBase, Mathf.Max(l.UseHitRadius, l.HoldRingRadius)) == (Vector2.Distance(l.UseCenter, l.UseCenterBase) < Epsilon) &&
                Untouched(shape, l.StickRestBase, l.StickRadius) == (Vector2.Distance(l.StickRest, l.StickRestBase) < Epsilon),
                name + ": a control that meets nothing stays where it is (full bleed)");
            Check(Inside(safe, Box(l.StickRest, Vector2.one * l.StickRadius * 2f)) &&
                Inside(safe, Box(l.FloatingOrigin(new Vector2(-100f, -100f)), Vector2.one * l.StickRadius * 2f)) &&
                Inside(safe, Box(l.FloatingOrigin(frame + Vector2.one * 100f), Vector2.one * l.StickRadius * 2f)),
                name + ": fixed/floating stick rings remain visible at edge touches");
        }

        var layout = controls.Layout;
        Check(Mathf.Abs(layout.MenuCenter.x - frame.x * .5f) < Epsilon,
            profile.Name + ": pause and menu group stay centered on the full frame despite asymmetric insets");
        var card = Box(layout.SettingsCardCenter, layout.SettingsCardSize);
        var fullFrame = new Rect(0f, 0f, frame.x, frame.y);
        Check(Inside(fullFrame, card) && Mathf.Abs(card.yMin) < Epsilon && card.yMax <= safe.yMax - 10f + Epsilon,
            profile.Name + ": the settings sheet runs full bleed to the bottom edge, its heading 10 pt under the safe top");
        Check(layout.SettingsBottomInset >= safe.yMin + 8f - Epsilon, profile.Name + ": the list's last row scrolls clear of the home indicator");
        var first = layout.SettingsColumn(0);
        var second = layout.SettingsColumn(1);
        Check(Inside(card, first) && Inside(card, second) && first.width >= 44f && first.height >= 44f,
            profile.Name + ": settings columns inside card with usable extent");
        Check(layout.SettingsSingleColumn ? Approximately(first, second) : first.xMax <= second.xMin,
            profile.Name + ": settings columns reflow or remain disjoint");
        Set(controls, "menuState", FrontRoomsTouchControls.MenuState.Settings);
        Invoke(controls, "UpdateSettingsSlots");
        Check(Inside(card, controls.SettingsScrollViewport) && Mathf.Abs(controls.SettingsScrollViewport.yMin) < Epsilon,
            profile.Name + ": settings scroll viewport inside the sheet, down to the bottom edge");
        CheckSettingsReachability(controls, profile.Name);
        CheckSettingsHeader(controls, profile.Name, shape);
        CheckChips(controls, profile.Name, safe);
    }

    /// <summary>The sheet's header (Red, 2026-10-24): RESUME gives way to a rounded corner sideways only, so it stays on one
    /// line with END RUN, which moves with it and keeps 12 pt beside it unless a camera between them pushes it further.</summary>
    static void CheckSettingsHeader(FrontRoomsTouchControls controls, string name, FrontRoomsHandheld.DisplayShape shape)
    {
        Set(controls, "menuState", FrontRoomsTouchControls.MenuState.Settings);
        controls.SetRestartConfirmation(false);
        if (!controls.TryGetChipBase(FrontRoomsTouchControls.Button.SettingsClose, out var resumeBase, out _) ||
            !controls.TryGetChipBase(FrontRoomsTouchControls.Button.Restart, out var endRunBase, out _) ||
            !controls.TryGetChip(FrontRoomsTouchControls.Button.SettingsClose, out var resume, out var resumeSize) ||
            !controls.TryGetChip(FrontRoomsTouchControls.Button.Restart, out var endRun, out var endRunSize))
        {
            Check(false, name + ": the settings header shows RESUME and END RUN");
            return;
        }
        var gap = RectCornerGap(shape, Box(resume, resumeSize));
        var between = resume.x - resumeSize.x * .5f - (endRun.x + endRunSize.x * .5f);
        var followed = Mathf.Abs(endRun.x - (endRunBase.x + resume.x - resumeBase.x)) < Epsilon;
        Check(Mathf.Abs(resume.y - resumeBase.y) < Epsilon && Mathf.Abs(endRun.y - resume.y) < Epsilon && (followed ? Mathf.Abs(between - 12f) < Epsilon : between > 12f) &&
            gap >= FrontRoomsHandheld.HudCornerGap - .01f,
            name + ": RESUME stays on the header line with END RUN (in " + (resumeBase.x - resume.x).ToString("0.0") + " pt), " +
            (followed ? "12 pt apart" : "END RUN past a camera, " + between.ToString("0") + " pt apart") + ", 16 pt from a rounded corner (gap " + gap.ToString("0.0") + ")");
    }

    static void CheckSettingsReachability(FrontRoomsTouchControls controls, string name)
    {
        // The tabbed sheet (2026-10-24): every tab's list scrolls in one viewport; each row keeps a 44 pt target and can be
        // scrolled into view and hit.
        var viewport = controls.SettingsScrollViewport;
        var allReachable = true;
        var allSized = true;
        var emptyTab = false;
        var controlsRows = 0;
        foreach (var tab in FrontRoomsSettingsMenu.Tabs)
        {
            SetProperty(controls, "SettingsTab", tab);
            Invoke(controls, "UpdateSettingsSlots");
            var rows = 0;
            foreach (var slot in controls.SettingsSlots)
            {
                if (slot.Header || slot.Detail) continue;
                rows++;
                allSized &= slot.Rect.height >= 44f && slot.Rect.width >= 44f;
                var rect = slot.Rect;
                var scroll = Mathf.Clamp(viewport.yMax - rect.yMax, 0f, controls.SettingsScrollMax);
                SetProperty(controls, "SettingsScroll", scroll);
                rect.y += scroll;
                allReachable &= Inside(viewport, rect) && rect.yMin >= controls.Layout.SettingsBottomInset - Epsilon;
                var args = new object[] { rect.center, -1, 0 };
                var hit = (bool)Invoke(controls, "SettingsRowAt", args);
                allReachable &= hit && (int)args[1] == slot.Id;
            }
            emptyTab |= rows == 0;
            if (tab == FrontRoomsSettingsMenu.Tab.Controls) controlsRows = rows;
        }
        Check(allSized, name + ": every settings row retains a 44 pt target");
        Check(!emptyTab && controlsRows >= 9, name + ": every tab has rows and CONTROLS keeps the touch rows (" + controlsRows + ")");
        Check(allReachable, name + ": each settings row is fully visible and hit-testable at an achievable scroll position");
        SetProperty(controls, "SettingsTab", FrontRoomsSettingsMenu.Tab.Graphics);
        SetProperty(controls, "SettingsScroll", 0f);
    }

    static void CheckChips(FrontRoomsTouchControls controls, string name, Rect safe)
    {
        foreach (var state in new[] { FrontRoomsTouchControls.MenuState.Paused, FrontRoomsTouchControls.MenuState.Settings, FrontRoomsTouchControls.MenuState.Caught })
        foreach (var confirmation in new[] { false, true })
        {
            Set(controls, "menuState", state);
            controls.SetRestartConfirmation(confirmation);
            var rects = new List<Rect>();
            foreach (FrontRoomsTouchControls.Button button in Enum.GetValues(typeof(FrontRoomsTouchControls.Button)))
            {
                if (!controls.TryGetChip(button, out var center, out var size)) continue;
                var hitSize = new Vector2(size.x + 12f, Mathf.Max(FrontRoomsTouchLayout.ChipHitHeight, size.y + 4f));
                var hit = Box(center, hitSize);
                var menuBounds = new Rect(0f, safe.yMin, safe.xMax + safe.xMin, safe.height);
                Check(hitSize.x >= 44f && hitSize.y >= 44f && Inside(menuBounds, hit),
                    name + ": " + state + "/" + button + " target >=44 pt and inside safe area");
                var overlap = false;
                foreach (var other in rects) overlap |= hit.Overlaps(other);
                Check(!overlap, name + ": " + state + "/" + button + " does not overlap another menu hit target");
                rects.Add(hit);
            }
        }
    }

    /// <summary>The display shape on the Duo, as iOS 27.1 reports it, and how content gives way to it.</summary>
    static void CheckDisplayShape()
    {
        FrontRoomsHandheld.Force(FrontRoomsHandheld.IPhoneDuoOuter);
        var s = FrontRoomsHandheld.Shape;
        Check(s.CameraBandRemoved && Mathf.Abs(s.BleedSafe.yMin - 34f) < Epsilon && Mathf.Abs(s.BleedSafe.yMax - 466f) < Epsilon &&
            s.Corners == new Vector4(59f, 59f, 8f, 8f) && s.Cutouts.Length == 1 && Mathf.Abs(s.Cutouts[0].center.x - 630.17f) < .05f && Mathf.Abs(s.Cutouts[0].center.y - 47.83f) < .05f,
            "Duo outer, camera bottom right: the 82 pt band goes back to the content (bleed bottom = the home inset, 34), the camera stays a 37 pt cutout, big corners at the bottom");
        var row = new Rect(32f, 30f, 614f, 44f);
        var narrowed = FrontRoomsHandheld.ClearRect(row, true);
        Check(Mathf.Abs(narrowed.xMin - row.xMin) < Epsilon && Mathf.Abs(narrowed.xMax - (s.Cutouts[0].xMin - 12f)) < Epsilon,
            "Duo outer: a settings row at the camera narrows from its right to end 12 pt before it; its left edge stays");
        var high = new Rect(32f, 300f, 614f, 44f);
        Check(FrontRoomsHandheld.ClearRect(high, true) == high, "Duo outer: a row away from the camera keeps its full width");

        FrontRoomsHandheld.Force(FrontRoomsHandheld.IPhoneDuoOuterFlipped);
        s = FrontRoomsHandheld.Shape;
        Check(s.CameraBandRemoved && Mathf.Abs(s.BleedSafe.yMax - 466f) < Epsilon && Mathf.Abs(s.BleedSafe.yMin - 34f) < Epsilon &&
            s.Corners == new Vector4(8f, 8f, 59f, 59f) && s.Cutouts.Length == 1 && Mathf.Abs(s.Cutouts[0].center.x - 47.83f) < .05f && Mathf.Abs(s.Cutouts[0].center.y - 418.17f) < .05f,
            "Duo outer, camera top left: the band at the top goes back, big corners on top");
        var heading = new Rect(32f, 417f, 70f, 22f);
        var pushed = FrontRoomsHandheld.ClearRect(heading);
        Check(Mathf.Abs(pushed.xMin - (s.Cutouts[0].xMax + 12f)) < Epsilon && Mathf.Abs(pushed.width - heading.width) < Epsilon && Mathf.Abs(pushed.y - heading.y) < Epsilon,
            "Duo outer flipped: SETTINGS at the top left is pushed right, 12 pt past the camera, keeping its size");
        var pause = new Vector2(650f, 432f);
        var drawn = FrontRoomsHandheld.ClearCircle(pause, 22f);
        Check(Vector2.Distance(drawn, pause) > 1f && Vector2.Distance(drawn, new Vector2(619f, 407f)) <= 59f - 26f + Epsilon,
            "Duo outer flipped: pause draws in along the 59 pt corner's diagonal, just enough");

        // Settled (iOS 27.1, 05:5x): the band on the left (84), the camera and its 84 x 82 keep-out box reported as regions.
        FrontRoomsHandheld.Force(FrontRoomsHandheld.IPhoneDuoOuterFlippedSettled);
        s = FrontRoomsHandheld.Shape;
        Check(s.CameraBandRemoved && Mathf.Abs(s.BleedSafe.xMin) < Epsilon && Mathf.Abs(s.BleedSafe.yMin - 34f) < Epsilon && s.Cutouts.Length == 1 &&
            Mathf.Abs(s.Cutouts[0].width - 37f) < .05f && Mathf.Abs(s.Cutouts[0].center.x - 47.83f) < .05f,
            "Duo outer settled: the 84 pt side band goes back; content clears the camera itself (37 pt), not its 84 x 82 keep-out box");
        pushed = FrontRoomsHandheld.ClearRect(heading);
        Check(Mathf.Abs(pushed.xMin - (s.Cutouts[0].xMax + 12f)) < Epsilon, "Duo outer settled: SETTINGS still lands 12 pt past the camera");

        FrontRoomsHandheld.Force(FrontRoomsHandheld.IPhoneDuoInner);
        s = FrontRoomsHandheld.Shape;
        Check(!s.CameraBandRemoved && s.Cutouts.Length == 0 && s.Corners == new Vector4(55f, 55f, 55f, 55f),
            "Duo inner: no cutout while the FaceTime camera is off, 55 pt corners");
        // The HUD's corner rule (Red, 2026-10-24: the HUD sat on the curves): 16 pt from the curve, down and in.
        var zone = new Rect(26.835f, 669f - 16f - 40f, 360f, 40f);
        var cleared = FrontRoomsHandheld.ClearCornerBlock(zone);
        var cornerGap = 55f - Vector2.Distance(new Vector2(cleared.xMin, cleared.yMax), new Vector2(55f, 669f - 55f));
        Check(Mathf.Abs(cornerGap - FrontRoomsHandheld.HudCornerGap) < .5f && zone.yMax - cleared.yMax > 10f && zone.yMax - cleared.yMax < 12f &&
            cleared.xMin - zone.xMin < 2f && Mathf.Abs(cleared.width - zone.width) < Epsilon,
            "Duo inner: the zone text moves " + (zone.yMax - cleared.yMax).ToString("0.0") + " pt down and " + (cleared.xMin - zone.xMin).ToString("0.0") +
            " pt in to keep 16 pt from the 55 pt corner (gap " + cornerGap.ToString("0.0") + "), not across the corner");
        var mid = new Rect(300f, 669f - 16f - 40f, 200f, 40f);
        Check(FrontRoomsHandheld.ClearCornerBlock(mid) == mid, "Duo inner: a top-band block away from the corners stays put");
        FrontRoomsHandheld.Force(FrontRoomsHandheld.IPhoneDuoOuterFlippedSettled);
        var outerZone = new Rect(16f, 466f - 16f - 40f, 360f, 40f);
        var outerCleared = FrontRoomsHandheld.ClearCornerBlock(outerZone);
        Check(outerCleared.xMin >= FrontRoomsHandheld.Shape.Cutouts[0].xMax + 12f - Epsilon && outerZone.yMax - outerCleared.yMax > 10f,
            "Duo outer settled: the zone clears the 59 pt corner (" + (outerZone.yMax - outerCleared.yMax).ToString("0.0") + " pt down) and still lands 12 pt past the camera");
        FrontRoomsHandheld.Force(FrontRoomsHandheld.IPhonePro);
        Check(FrontRoomsHandheld.ClearCornerBlock(outerZone) == outerZone, "Square corners (harness iPhone): a corner block never moves");
        FrontRoomsHandheld.Force(FrontRoomsHandheld.IPhoneDuoInnerCameraOn);
        Check(FrontRoomsHandheld.Shape.Cutouts.Length == 1, "Duo inner: the FaceTime camera's occlusion region is a cutout while it runs");
        FrontRoomsHandheld.Force(FrontRoomsHandheld.IPhonePro);
        s = FrontRoomsHandheld.Shape;
        Check(s.Cutouts.Length == 0 && s.Corners == Vector4.zero && s.BleedSafe == new Rect(62f, 21f, 750f, 381f),
            "iPhone 16 Pro: no cutout, the safe area as iOS gives it");
    }

    /// <summary>Squarer handheld views keep 16:9's horizontal field of view (Red, 2026-10-24); desktop draws it as authored.</summary>
    static void CheckAspectFov()
    {
        FrontRoomsHandheld.Force(FrontRoomsHandheld.IPhoneDuoInner);
        var inner = FrontRoomsHandheld.AspectFov(76f, 951f / 669f);
        var hRef = 2f * Mathf.Atan(Mathf.Tan(38f * Mathf.Deg2Rad) * 16f / 9f);
        var hInner = 2f * Mathf.Atan(Mathf.Tan(inner * .5f * Mathf.Deg2Rad) * 951f / 669f);
        Check(Mathf.Abs(hInner - hRef) < 1e-3f && inner > 76f, "Duo inner: 76 at 16:9 draws " + inner.ToString("0.0") + ", the same horizontal field");
        Check(Mathf.Abs(FrontRoomsHandheld.AspectFov(76f, 874f / 402f) - 76f) < Epsilon, "iPhone landscape (wider than 16:9): the FOV as authored");
        Check(Mathf.Abs(FrontRoomsHandheld.AspectFov(76f, 669f / 951f) - FrontRoomsHandheld.MaxHandheldFov) < Epsilon, "Duo inner portrait: capped at " + FrontRoomsHandheld.MaxHandheldFov);
        FrontRoomsHandheld.ClearForce();
        Check(Mathf.Abs(FrontRoomsHandheld.AspectFov(76f, 4f / 3f) - 76f) < Epsilon, "Desktop (not handheld): the FOV as authored at any aspect");
    }

    /// <summary>A circle (a control's hit area or ring) keeps 11.5 pt from every cutout and stays inside every rounded corner.</summary>
    static bool CircleClear(FrontRoomsHandheld.DisplayShape s, Vector2 c, float radius)
    {
        foreach (var cut in s.Cutouts)
        {
            var nearest = new Vector2(Mathf.Clamp(c.x, cut.xMin, cut.xMax), Mathf.Clamp(c.y, cut.yMin, cut.yMax));
            if (Vector2.Distance(nearest, c) < radius + 11.5f) return false;
        }
        for (var i = 0; i < 4; i++)
        {
            var round = s.Corners[i];
            if (round <= radius) continue;
            var left = i == 0 || i == 2;
            var bottom = i < 2;
            var k = new Vector2(left ? round : s.Frame.x - round, bottom ? round : s.Frame.y - round);
            var inZone = (left ? c.x < k.x : c.x > k.x) && (bottom ? c.y < k.y : c.y > k.y);
            if (inZone && Vector2.Distance(c, k) > round - radius + Epsilon) return false;
        }
        return true;
    }

    /// <summary>How far a circle stays inside the nearest rounded corner's curve (a large number when no corner reaches it).</summary>
    static float CornerGap(FrontRoomsHandheld.DisplayShape s, Vector2 c, float radius)
    {
        var gap = 999f;
        for (var i = 0; i < 4; i++)
        {
            var round = s.Corners[i];
            if (round <= radius + FrontRoomsHandheld.HudCornerGap) continue;
            var left = i == 0 || i == 2;
            var bottom = i < 2;
            var k = new Vector2(left ? round : s.Frame.x - round, bottom ? round : s.Frame.y - round);
            var inZone = (left ? c.x < k.x : c.x > k.x) && (bottom ? c.y < k.y : c.y > k.y);
            if (inZone) gap = Mathf.Min(gap, round - radius - Vector2.Distance(c, k));
        }
        return gap;
    }

    /// <summary>How far a rect's corner stays inside the nearest rounded corner's curve (a large number when none reaches it).</summary>
    static float RectCornerGap(FrontRoomsHandheld.DisplayShape s, Rect r)
    {
        var gap = 999f;
        for (var i = 0; i < 4; i++)
        {
            var round = s.Corners[i];
            if (round <= FrontRoomsHandheld.HudCornerGap) continue;
            var left = i == 0 || i == 2;
            var bottom = i < 2;
            var k = new Vector2(left ? round : s.Frame.x - round, bottom ? round : s.Frame.y - round);
            var p = new Vector2(left ? r.xMin : r.xMax, bottom ? r.yMin : r.yMax);
            var inZone = (left ? p.x < k.x : p.x > k.x) && (bottom ? p.y < k.y : p.y > k.y);
            if (inZone) gap = Mathf.Min(gap, round - Vector2.Distance(p, k));
        }
        return gap;
    }

    /// <summary>Whether a circle at its authored place already meets no cutout and no corner.</summary>
    static bool Untouched(FrontRoomsHandheld.DisplayShape s, Vector2 c, float radius) =>
        FrontRoomsHandheld.ClearCircle(s, c, radius, 12f, 4f) == c;

    static void CheckBaseline()
    {
        var l = new FrontRoomsTouchLayout(new Vector2(874, 402), new Rect(62, 21, 750, 381), 1f, false, false, 0f);
        Check(Vector2.Distance(l.StickRest, new Vector2(150, 112)) < Epsilon &&
            Vector2.Distance(l.UseCenter, new Vector2(740, 140)) < Epsilon &&
            Vector2.Distance(l.PauseCenter, new Vector2(846, 368)) < Epsilon,
            "Standard iPhone: stick/USE positions preserved and pause follows user-requested right bleed");
        Check(!l.SettingsSingleColumn && Mathf.Abs(l.SettingsColumn(0).width - 337f) < Epsilon,
            "Standard iPhone: existing two-column settings width preserved");
    }

    static void CheckResizeContinuity(FrontRoomsTouchControls controls)
    {
        FrontRoomsHandheld.Force(FrontRoomsHandheld.IPhonePro);
        Invoke(controls, "RefreshLayout");
        Set(controls, "menuState", FrontRoomsTouchControls.MenuState.Paused);
        controls.SetRestartConfirmation(true);
        var prompt = new FrontRoomsTouchControls.UsePrompt { Visible = true, Interactable = true, Kind = FrontRoomsTouchControls.UseKind.Hold, Hold = true, Label = "HOLD", Progress = .4f };
        controls.SetUsePrompt(prompt);
        SetProperty(controls, "SettingsScroll", 32f);
        Set(controls, "moveId", 1);
        Set(controls, "lookId", 2);
        Set(controls, "useId", 3);
        Set(controls, "pinchId", 4);
        Set(controls, "lastTapTime", 12f);
        Set(controls, "scrollVelocity", 90f);
        Set(controls, "frameMove", Vector2.one);
        Set(controls, "frameLook", Vector2.one);
        Set(controls, "frameUseDown", true);
        Set(controls, "frameZoom", 1f);
        Set(controls, "frameZoomToggle", true);
        // A stable frame must not discard an ongoing gesture.
        Invoke(controls, "RefreshLayout");
        Check(Get<int>(controls, "moveId") == 1 && Get<int>(controls, "useId") == 3,
            "Stable metrics preserve ongoing touches");
        FrontRoomsHandheld.Force(FrontRoomsHandheld.IPhoneDuoInner);
        Invoke(controls, "RefreshLayout");
        Check(Get<int>(controls, "moveId") == -1 && Get<int>(controls, "lookId") == -1 &&
            Get<int>(controls, "useId") == -1 && Get<int>(controls, "pinchId") == -1 && !controls.Running,
            "Display resize releases move, look, hold and pinch ownership");
        Check(Get<Vector2>(controls, "frameMove") == Vector2.zero && Get<Vector2>(controls, "frameLook") == Vector2.zero &&
            !Get<bool>(controls, "frameUseDown") && Get<float>(controls, "frameZoom") == 0f && !Get<bool>(controls, "frameZoomToggle") &&
            Get<float>(controls, "lastTapTime") < 0f && Get<float>(controls, "scrollVelocity") == 0f,
            "Display resize clears accumulated input, double tap and inertia");
        Check(controls.CurrentMenuState == FrontRoomsTouchControls.MenuState.Paused && controls.RestartConfirmationOpen &&
            controls.CurrentUsePrompt == prompt && Mathf.Abs(controls.SettingsScroll - 32f) < Epsilon,
            "Display resize preserves menu, restart confirmation, prompt and settings position");

        // Insets may change without a window size change (system overlay / camera side).
        Set(controls, "moveId", 5);
        var p = FrontRoomsHandheld.IPhoneDuoInner;
        FrontRoomsHandheld.Force(new FrontRoomsHandheld.Profile("Safe-area-only transition", p.Pixels.x, p.Pixels.y,
            p.PointScale, 24f, 4f, 8f, p.InsetBottom, p.Tablet));
        Invoke(controls, "RefreshLayout");
        Check(Get<int>(controls, "moveId") == -1 && Mathf.Abs(controls.Layout.SafeLeft - 24f) < Epsilon,
            "Safe-area-only transition refreshes layout and clears stale touch coordinates");
    }

    static void Check(bool value, string text)
    {
        if (value) report.passed++; else report.failed++;
        report.checks.Add((value ? "ok   " : "FAIL ") + text);
    }

    static Rect Box(Vector2 center, Vector2 size) => new Rect(center - size * .5f, size);
    static bool Inside(Rect outer, Rect inner) => inner.width > 0f && inner.height > 0f &&
        inner.xMin >= outer.xMin - Epsilon && inner.xMax <= outer.xMax + Epsilon &&
        inner.yMin >= outer.yMin - Epsilon && inner.yMax <= outer.yMax + Epsilon;
    static bool Approximately(Rect a, Rect b) => Vector2.Distance(a.position, b.position) < Epsilon && Vector2.Distance(a.size, b.size) < Epsilon;
    static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);
    static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
    static void SetProperty(object target, string name, object value) => target.GetType().GetProperty(name).GetSetMethod(true).Invoke(target, new[] { value });
    static object Invoke(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);
}
