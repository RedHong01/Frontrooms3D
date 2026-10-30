using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One settings system for Mac, Windows, iPhone, iPad and both iPhone Duo displays (Red, 2026-10-24: group the list,
/// and stop making every row tap-to-cycle). Tabs, rows, values, defaults and the explanation of each row live here;
/// the desktop sheet (FrontRoomsDesktopSettingsView) and the touch sheet (FrontRoomsTouchControlsView) draw them,
/// and the touch hit test uses the same geometry (Layout). Spec: Documentation/research/interaction/settings/IA.md.
///
/// A control per kind of row: SWITCH for on/off (the word always shown), SEGMENTS for a few named choices, a SLIDER
/// for anything ordered by amount (Red, 2026-10-24: numbers in segments were unreadable; a plain horizontal bar
/// instead), a METER for the measured frame rate and a text BUTTON for reset.
/// </summary>
public static class FrontRoomsSettingsMenu
{
    public enum Tab { Graphics, Comfort, Controls, Assist }
    /// <summary>Keys: a read-only row that names a key or a gesture (› GESTURES on phones: the old pause legend); a tap on a
    /// phone only explains it. Page: a row that opens a second-level page of its tab (the desktop's KEYBOARD &amp; MOUSE map).</summary>
    public enum Kind { Header, Switch, Segments, Slider, Meter, Action, Keys, Page }

    public enum Key
    {
        None, Quality, Frame, Resolution, RenderScale, Upscaling, FrameRate, AntiAliasing, AmbientOcclusion, Shadows, Bloom,
        Hdr, LowPower, ResetGraphics, CameraMotion, ReduceFlashing, BreakGlass, LookSpeed, InvertLook, GyroLook, Stick, Run,
        StickHints, ControlsSize, ControlsOpacity, LeftHanded, HapticsGameplay, HapticsControls, Captions, RelayReadout,
        KeyMove, KeyLook, KeySprint, KeyUse, GestureMove, GestureLook, GestureSlow, GestureUse, KeyMap,
    }

    public static readonly Tab[] Tabs = { Tab.Graphics, Tab.Comfort, Tab.Controls, Tab.Assist };
    public static string TabName(Tab t) => t == Tab.Graphics ? "GRAPHICS" : t == Tab.Comfort ? "COMFORT" : t == Tab.Controls ? "CONTROLS" : "ASSIST";

    public sealed class Row
    {
        public Key Key;
        public Kind Kind;
        public string Label;
        /// <summary>Segment labels, or a slider's value text for each stop.</summary>
        public string[] Options;
        /// <summary>A slider's stop values (the track spans the first to the last).</summary>
        public int[] Stops;
        public int Index;
        public int DefaultIndex;
        /// <summary>Relative frame cost from the probe (0 none shown, 1 low, 3 high).</summary>
        public int Cost;
        public bool Visual;
        /// <summary>Segment labels with digits are set in IBM Plex Mono (never Bayon, 平面视觉).</summary>
        public bool Mono;
        /// <summary>Half the segment padding, for the long rows that must fit a 418 pt list (handheld QUALITY, FRAME RATE).</summary>
        public bool Compact;
        public string Explain;
        public bool Changed => Kind != Kind.Header && Kind != Kind.Meter && Kind != Kind.Action && Kind != Kind.Keys && Index != DefaultIndex;
        public string ValueText => Options != null && Index >= 0 && Index < Options.Length ? Options[Index] : string.Empty;
        public string DefaultText => Options != null && DefaultIndex >= 0 && DefaultIndex < Options.Length ? Options[DefaultIndex] : string.Empty;
    }

    /// <summary>The game's HDR switch (the camera's, kept in FrontRooms3DGame).</summary>
    public static Func<bool> HdrGet;
    public static Action<bool> HdrSet;

    /// <summary>RESET GRAPHICS was pressed once; a second press within 3 s resets.</summary>
    public static float ResetArmedUntil = -1f;
    public static bool ResetArmed => Time.unscaledTime < ResetArmedUntil;

    public static event Action Changed;

    static bool Handheld => FrontRoomsHandheld.Active;
    static readonly Dictionary<Key, Row> pool = new Dictionary<Key, Row>();
    static readonly Dictionary<Tab, List<Row>> lists = new Dictionary<Tab, List<Row>>();

    static readonly string[] OffOn = { "OFF", "ON" };
    static readonly string[] QualityNames = { "LOW", "MEDIUM", "HIGH", "ULTRA", "CUSTOM" };
    // Handhelds name a preset by what it is for (Red, 2026-10-24, D2 · C): the same four presets underneath.
    static readonly string[] HandheldQualityNames = { "BATTERY", "BALANCED", "QUALITY", "ULTRA", "CUSTOM" };
    static readonly string[] LowHigh = { "LOW", "HIGH" };
    static readonly string[] OffLowHigh = { "OFF", "LOW", "HIGH" };
    static readonly string[] LowMedHigh = { "LOW", "MEDIUM", "HIGH" };
    static readonly int[] ScaleStops = { 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100 };
    static readonly int[] MotionStops = { 0, 50, 100 };
    static readonly int[] LookStops = { 60, 80, 100, 120, 150 };
    static readonly int[] SizeStops = { 80, 100, 120, 140 };
    static readonly int[] OpacityStops = { 20, 34, 50, 65 };
    static readonly string[] HandheldRateNames = { "30 FPS", "60 FPS" };
    static readonly string[] ProMotionRateNames = { "30 FPS", "40 FPS", "60 FPS", "120 FPS" };
    static readonly string[] DesktopRateNames = { "VSYNC", "30", "60", "120", "MAX" };

    static string[] Percents(int[] stops, bool offAtZero)
    {
        var s = new string[stops.Length];
        for (var i = 0; i < stops.Length; i++) s[i] = offAtZero && stops[i] == 0 ? "OFF" : stops[i] + "%";
        return s;
    }

    static Row Get(Key key, Kind kind, string label, string explain, string[] options = null, int[] stops = null, int cost = 0, bool visual = false, bool mono = false)
    {
        if (!pool.TryGetValue(key, out var r)) { r = new Row(); pool[key] = r; }
        r.Key = key; r.Kind = kind; r.Label = label; r.Explain = explain; r.Options = options; r.Stops = stops;
        r.Cost = cost; r.Visual = visual; r.Mono = mono; r.Compact = false;
        return r;
    }

    static readonly Dictionary<string, Row> headers = new Dictionary<string, Row>();

    static Row Header(Tab tab, string label)
    {
        var id = (int)tab + label;
        if (!headers.TryGetValue(id, out var r)) { r = new Row { Key = Key.None, Kind = Kind.Header, Label = label }; headers[id] = r; }
        return r;
    }

    static int IndexOf(int[] stops, int value)
    {
        var best = 0;
        for (var i = 1; i < stops.Length; i++) if (Mathf.Abs(stops[i] - value) < Mathf.Abs(stops[best] - value)) best = i;
        return best;
    }

    /// <summary>The rows of a tab on this device, with their current values (rebuilt in place; no allocation once warm).</summary>
    public static List<Row> Rows(Tab tab)
    {
        if (!lists.TryGetValue(tab, out var list)) { list = new List<Row>(24); lists[tab] = list; }
        list.Clear();
        switch (tab)
        {
            case Tab.Graphics: Graphics(list); break;
            case Tab.Comfort: Comfort(list); break;
            case Tab.Controls: Controls(list); break;
            default: Assist(list); break;
        }
        return list;
    }

    static void Graphics(List<Row> list)
    {
        var defaults = FrontRoomsQuality.DefaultPreset;
        list.Add(Header(Tab.Graphics, "PRESET"));
        var q = Get(Key.Quality, Kind.Segments, "QUALITY",
            Handheld ? "Sets every detail at once. BATTERY saves power, BALANCED holds the frame, QUALITY keeps detail, ULTRA draws it all. A change reads CUSTOM."
                     : "Sets every detail below at once. ULTRA is the game's full look; lower presets keep the frame steady on smaller computers. Change a detail and it reads CUSTOM.",
            Handheld ? HandheldQualityNames : QualityNames, cost: 0, visual: true);
        q.Compact = Handheld;
        q.Index = FrontRoomsQuality.IsCustom ? 4 : (int)FrontRoomsQuality.CurrentPreset;
        q.DefaultIndex = (int)defaults;
        list.Add(q);
        var f = Get(Key.Frame, Kind.Meter, "FRAME RATE NOW",
            "The frame rate the game is drawing right now, measured. Every dash lit means it holds its target.");
        list.Add(f);

        list.Add(Header(Tab.Graphics, "DETAIL"));
        var res = Get(Key.Resolution, Kind.Segments, "RESOLUTION",
            Handheld ? "AUTO draws every screen at about the same number of pixels: less on big displays such as the iPhone Duo's inner one. FIXED lets you set it."
                     : "AUTO draws at the window's full size. FIXED lets you draw at a share of it.",
            new[] { "AUTO", "FIXED" }, cost: 3, visual: true);
        res.Index = FrontRoomsQuality.Resolution;
        res.DefaultIndex = defaults == FrontRoomsQuality.Preset.Ultra && Handheld ? 1 : 0;
        list.Add(res);
        if (FrontRoomsQuality.Resolution == 1)
        {
            var rs = Get(Key.RenderScale, Kind.Slider, "RENDER SCALE",
                "Draws the picture at this share of the screen, then scales it up. Lower is smoother and softer; text and the HUD stay sharp.",
                Percents(ScaleStops, false), ScaleStops, cost: 3, visual: true);
            rs.Index = IndexOf(ScaleStops, FrontRoomsQuality.RenderScalePercent);
            rs.DefaultIndex = ScaleStops.Length - 1;
            list.Add(rs);
        }
        if (FrontRoomsQuality.EffectiveRenderScale < .999f)
        {
            var up = Get(Key.Upscaling, Kind.Segments, "UPSCALING",
                "How the smaller picture is scaled up to the screen. SHARP restores edges (FSR 1); SOFT is a plain, smooth scale.",
                new[] { "SHARP", "SOFT" }, visual: true);
            up.Index = FrontRoomsQuality.Upscaling; up.DefaultIndex = 0;
            list.Add(up);
        }
        var rateNames = !Handheld ? DesktopRateNames : FrontRoomsQuality.ProMotion ? ProMotionRateNames : HandheldRateNames;
        var fr = Get(Key.FrameRate, Kind.Segments, "FRAME RATE",
            !Handheld ? "VSYNC follows the display. A number caps the frame rate; MAX draws as fast as it can."
                : FrontRoomsQuality.ProMotion ? "Higher is smoother but costs battery and heat. 40 FPS is a steady middle step here; iOS drops 120 in Low Power Mode or when hot."
                : "30 FPS saves battery and heat; 60 FPS moves more smoothly.",
            rateNames, mono: true);
        fr.Compact = Handheld && FrontRoomsQuality.ProMotion;
        fr.Index = FrontRoomsQuality.FrameRate;
        fr.DefaultIndex = Handheld ? System.Array.IndexOf(FrontRoomsQuality.FrameRates, 60) : 0;
        list.Add(fr);
        var aa = Get(Key.AntiAliasing, Kind.Segments, "ANTI-ALIASING",
            "Smooths jagged edges. HIGH samples each pixel four times, LOW twice.", OffLowHigh, cost: 1, visual: true);
        aa.Index = FrontRoomsQuality.AntiAliasing; aa.DefaultIndex = defaults == FrontRoomsQuality.Preset.Ultra || defaults == FrontRoomsQuality.Preset.High ? 2 : 1;
        list.Add(aa);
        if (FrontRoomsQuality.AmbientOcclusionAvailable)
        {
            var ao = Get(Key.AmbientOcclusion, Kind.Segments, "AMBIENT OCCLUSION",
                "Soft shading where walls, floors and furniture meet. LOW draws it at half resolution.", LowHigh, cost: 1, visual: true);
            ao.Index = FrontRoomsQuality.AmbientOcclusion; ao.DefaultIndex = 1;
            list.Add(ao);
        }
        var sh = Get(Key.Shadows, Kind.Segments, "SHADOWS",
            "How sharp shadows are and how far they reach. LOW draws coarser shadows that end sooner.", LowMedHigh, cost: 1, visual: true);
        sh.Index = FrontRoomsQuality.Shadows; sh.DefaultIndex = 2;
        list.Add(sh);
        var bl = Get(Key.Bloom, Kind.Segments, "BLOOM",
            "The glow around the tubes and the lit screens. LOW uses a cheaper, slightly blockier glow.", LowHigh, cost: 1, visual: true);
        bl.Index = FrontRoomsQuality.Bloom; bl.DefaultIndex = 1;
        list.Add(bl);
        if (HdrGet != null)
        {
            var hdr = Get(Key.Hdr, Kind.Switch, "HDR RENDER",
                "Renders light with high dynamic range before the film look. OFF is plain SDR.", OffOn, cost: 1, visual: true);
            hdr.Index = HdrGet() ? 1 : 0; hdr.DefaultIndex = 1;
            list.Add(hdr);
        }
        if (FrontRoomsQuality.LowPowerSupported)
        {
            var lp = Get(Key.LowPower, Kind.Segments, "LOW POWER MODE",
                "FOLLOW: while iOS Low Power Mode is on, the game drops to 30 FPS and LOW. IGNORE keeps your settings.",
                new[] { "FOLLOW", "IGNORE" });
            lp.Index = FrontRoomsQuality.LowPower; lp.DefaultIndex = 0;
            list.Add(lp);
        }
        var reset = Get(Key.ResetGraphics, Kind.Action, "RESET GRAPHICS",
            "Puts every graphics row back to this device's default. Press twice to confirm.", new[] { "RESET" });
        reset.Options = new[] { ResetArmed ? "AGAIN TO RESET" : "RESET" };
        list.Add(reset);
    }

    static void Comfort(List<Row> list)
    {
        var cm = Get(Key.CameraMotion, Kind.Slider, "CAMERA MOTION",
            "How much close-ups, shakes and dips move the camera. OFF keeps the view still; everything still happens.",
            Percents(MotionStops, true), MotionStops);
        cm.Index = IndexOf(MotionStops, FrontRoomsSettings.CameraMotionPercent);
        cm.DefaultIndex = Handheld && FrontRoomsHandheld.OsReduceMotion ? 0 : MotionStops.Length - 1;
        list.Add(cm);
        var rf = Get(Key.ReduceFlashing, Kind.Switch, "REDUCE FLASHING",
            "Lamp strobes become soft dips and dead tubes stop blinking. It reduces flashing; some bright changes remain.", OffOn);
        rf.Index = FrontRoomsSettings.ReduceFlashing ? 1 : 0; rf.DefaultIndex = 0;
        list.Add(rf);
    }

    static Row KeyRow(Key key, string label, string keys, string explain)
    {
        var r = Get(key, Kind.Keys, label, explain, new[] { keys }, mono: true);
        r.Index = r.DefaultIndex = 0;
        return r;
    }

    /// <summary>The KEYBOARD &amp; MOUSE row's keycaps: the walk, SPRINT and USE as bound now, '|' between them.</summary>
    static string KeyPreview()
    {
        var binds = new[] { FrontRoomsInput.Bind.Forward, FrontRoomsInput.Bind.Left, FrontRoomsInput.Bind.Back, FrontRoomsInput.Bind.Right, FrontRoomsInput.Bind.Sprint, FrontRoomsInput.Bind.Use };
        var names = new string[binds.Length];
        for (var i = 0; i < binds.Length; i++) names[i] = FrontRoomsInput.KeyName(FrontRoomsInput.PathOf(binds[i]));
        return string.Join("|", names);
    }

    /// <summary>A row that opens a second-level page; <paramref name="preview"/> names the keys it shows, as keycaps ('|' between them).</summary>
    static Row PageRow(Key key, string label, string explain, string preview)
    {
        var r = Get(key, Kind.Page, label, explain, new[] { preview }, mono: true);
        r.Index = r.DefaultIndex = 0;
        return r;
    }

    static void Controls(List<Row> list)
    {
        if (!Handheld)
        {
            // The PAUSED card's legend became this tab (Red, 2026-10-24); the keys are said once, on the map, a second-level
            // page (Red, 2026-10-29: "mapping graph", CONTROLS' second level). HOLD E is the BREAK GLASS row.
            list.Add(Header(Tab.Controls, "KEYS"));
            list.Add(PageRow(Key.KeyMap, "KEYBOARD & MOUSE", "Every key and button the game uses, drawn on a keyboard and a mouse. Choose one to read what it does, or to change its key.", KeyPreview()));
            // The USE key as the player bound it (KEYBOARD & MOUSE).
            var use = FrontRoomsInput.UseKey;
            var bg = Get(Key.BreakGlass, Kind.Segments, "BREAK GLASS",
                "HOLD keeps " + use + " down to break a pane. TAP banks a little progress per tap, never faster than holding.", new[] { "HOLD " + use, "TAP " + use });
            bg.Index = FrontRoomsSettings.TapToBreak ? 1 : 0; bg.DefaultIndex = 0;
            list.Add(bg);
            return;
        }
        // The phone's PAUSED legend lives here now (Red, 2026-10-24: the pause is this sheet). PAST THE RING · RUN and
        // HOLD USE · BREAK GLASS are the RUN and BREAK GLASS rows below, so only the other four are named here.
        list.Add(Header(Tab.Controls, "GESTURES"));
        list.Add(KeyRow(Key.GestureMove, "MOVE", "STICK", "Push the stick to walk; past its ring you run."));
        list.Add(KeyRow(Key.GestureLook, "LOOK", "DRAG", "Drag on the look side of the screen to turn your head."));
        list.Add(KeyRow(Key.GestureSlow, "SLOW · UNHEARD", "INSIDE THE RING", "Keep the stick inside its ring: you walk slowly, and slow steps are not heard."));
        list.Add(KeyRow(Key.GestureUse, "USE · DOOR", "USE", "Tap USE, or the door itself, to open it."));
        list.Add(Header(Tab.Controls, "LOOK"));
        var ls = Get(Key.LookSpeed, Kind.Slider, "LOOK SPEED",
            "How far the view turns for each point you drag. 100% turns 0.20° per point.", Percents(LookStops, false), LookStops);
        ls.Index = IndexOf(LookStops, FrontRoomsSettings.TouchLookSpeedPercent); ls.DefaultIndex = 2;
        list.Add(ls);
        var il = Get(Key.InvertLook, Kind.Switch, "INVERT LOOK", "Dragging up looks down.", OffOn);
        il.Index = FrontRoomsSettings.TouchInvertLook ? 1 : 0; il.DefaultIndex = 0;
        list.Add(il);
        var gy = Get(Key.GyroLook, Kind.Segments, "GYRO LOOK",
            "Turn by tilting the phone: only while a finger is on the screen, or always.", new[] { "OFF", "WHILE TOUCHING", "ALWAYS" });
        gy.Index = FrontRoomsSettings.TouchGyroMode; gy.DefaultIndex = 0;
        list.Add(gy);
        list.Add(Header(Tab.Controls, "MOVE"));
        var st = Get(Key.Stick, Kind.Segments, "STICK",
            "FLOATING starts the stick where your thumb lands; FIXED keeps it in one place.", new[] { "FLOATING", "FIXED" });
        st.Index = FrontRoomsSettings.TouchFloatingStick ? 0 : 1; st.DefaultIndex = 0;
        list.Add(st);
        var run = Get(Key.Run, Kind.Segments, "RUN",
            "STICK: push past the ring to run. BUTTON: a RUN button above the stick.", new[] { "STICK", "BUTTON" });
        run.Index = FrontRoomsSettings.TouchSprintSocket ? 0 : 1; run.DefaultIndex = 0;
        list.Add(run);
        var bg2 = Get(Key.BreakGlass, Kind.Segments, "BREAK GLASS",
            "HOLD keeps USE down to break a pane. TAP banks a little progress per tap, never faster than holding.", new[] { "HOLD USE", "TAP USE" });
        bg2.Index = FrontRoomsSettings.TapToBreak ? 1 : 0; bg2.DefaultIndex = 0;
        list.Add(bg2);
        var hints = Get(Key.StickHints, Kind.Switch, "STICK HINTS",
            "Words on the stick that teach the walk and the run. ON shows them again.", OffOn);
        hints.Index = FrontRoomsTouchControls.StickHintsOn ? 1 : 0; hints.DefaultIndex = 1;
        list.Add(hints);
        list.Add(Header(Tab.Controls, "LAYOUT"));
        var size = Get(Key.ControlsSize, Kind.Slider, "CONTROLS SIZE",
            "The size of the stick, USE and the buttons.", Percents(SizeStops, false), SizeStops);
        size.Index = IndexOf(SizeStops, FrontRoomsSettings.TouchControlsScalePercent); size.DefaultIndex = 1;
        list.Add(size);
        var op = Get(Key.ControlsOpacity, Kind.Slider, "CONTROLS OPACITY",
            "How solid the empty controls look. A control that carries a word stays solid.", Percents(OpacityStops, false), OpacityStops);
        op.Index = IndexOf(OpacityStops, FrontRoomsSettings.TouchOpacityPercent); op.DefaultIndex = 1;
        list.Add(op);
        var lh = Get(Key.LeftHanded, Kind.Switch, "LEFT-HANDED", "Swaps the stick and the look side.", OffOn);
        lh.Index = FrontRoomsSettings.TouchLeftHanded ? 1 : 0; lh.DefaultIndex = 0;
        list.Add(lh);
        if (FrontRoomsHandheld.HapticsSupported)
        {
            list.Add(Header(Tab.Controls, "HAPTICS"));
            var hg = Get(Key.HapticsGameplay, Kind.Switch, "GAMEPLAY", "Vibration for doors, glass, being seen and being caught.", OffOn);
            hg.Index = FrontRoomsSettings.TouchHaptics ? 1 : 0; hg.DefaultIndex = 1;
            list.Add(hg);
            var hc = Get(Key.HapticsControls, Kind.Switch, "CONTROLS", "Vibration for the run click, USE and menu presses.", OffOn);
            hc.Index = FrontRoomsSettings.TouchHapticsControls ? 1 : 0; hc.DefaultIndex = 1;
            list.Add(hc);
        }
    }

    static void Assist(List<Row> list)
    {
        var c = Get(Key.Captions, Kind.Switch, "CAPTIONS", "Captions for its steps and the other sounds that matter, with where each one comes from.", OffOn);
        c.Index = FrontRoomsSettings.Captions ? 1 : 0; c.DefaultIndex = 0;
        list.Add(c);
        var rr = Get(Key.RelayReadout, Kind.Switch, "THREAT READOUT", "Shows what hunts you, its state, its distance and its tier, on the HUD. The tier rises as you explore. An assist; off by default.", OffOn);
        rr.Index = FrontRoomsSettings.RelayReadout ? 1 : 0; rr.DefaultIndex = 0;
        list.Add(rr);
    }

    // ------------------------------------------------------------- changes

    /// <summary>Sets a row to an option or stop (segments, sliders, switches); actions run.</summary>
    public static void Set(Row row, int index)
    {
        if (row == null || row.Kind == Kind.Header || row.Kind == Kind.Meter || row.Kind == Kind.Page) return;
        if (row.Kind == Kind.Action) { Activate(row); return; }
        var n = row.Options != null ? row.Options.Length : 2;
        index = Mathf.Clamp(index, 0, n - 1);
        var stop = row.Stops != null ? row.Stops[index] : index;
        switch (row.Key)
        {
            case Key.Quality: if (index < 4) FrontRoomsQuality.PickPreset((FrontRoomsQuality.Preset)index); break;
            case Key.Resolution: FrontRoomsQuality.SetResolution(index); break;
            case Key.RenderScale: FrontRoomsQuality.SetRenderScale(stop); break;
            case Key.Upscaling: FrontRoomsQuality.SetUpscaling(index); break;
            case Key.FrameRate: FrontRoomsQuality.SetFrameRate(index); break;
            case Key.AntiAliasing: FrontRoomsQuality.SetAntiAliasing(index); break;
            case Key.AmbientOcclusion: FrontRoomsQuality.SetAmbientOcclusion(index); break;
            case Key.Shadows: FrontRoomsQuality.SetShadows(index); break;
            case Key.Bloom: FrontRoomsQuality.SetBloom(index); break;
            case Key.Hdr: HdrSet?.Invoke(index == 1); break;
            case Key.LowPower: FrontRoomsQuality.SetLowPower(index); break;
            case Key.CameraMotion: FrontRoomsSettings.SetCameraMotion(stop); break;
            case Key.ReduceFlashing: FrontRoomsSettings.SetReduceFlashing(index == 1); break;
            case Key.BreakGlass: FrontRoomsSettings.SetTapToBreak(index == 1); break;
            case Key.LookSpeed: FrontRoomsSettings.SetTouchLookSpeed(stop); break;
            case Key.InvertLook: FrontRoomsSettings.SetTouchInvertLook(index == 1); break;
            case Key.GyroLook: FrontRoomsSettings.SetTouchGyro(index); break;
            case Key.Stick: FrontRoomsSettings.SetTouchFloatingStick(index == 0); break;
            case Key.Run: FrontRoomsSettings.SetTouchSprintSocket(index == 0); break;
            case Key.StickHints:
            {
                // ON clears both words, OFF retires both (X2 §3.5).
                var learned = index == 0;
                FrontRoomsTouchControls.SetTaught(true, learned);
                FrontRoomsTouchControls.SetTaught(false, learned);
                break;
            }
            case Key.ControlsSize: FrontRoomsSettings.SetTouchControlsScale(stop); break;
            case Key.ControlsOpacity: FrontRoomsSettings.SetTouchOpacity(stop); break;
            case Key.LeftHanded: FrontRoomsSettings.SetTouchLeftHanded(index == 1); break;
            case Key.HapticsGameplay: FrontRoomsSettings.SetTouchHaptics(index == 1); break;
            case Key.HapticsControls: FrontRoomsSettings.SetTouchHapticsControls(index == 1); break;
            case Key.Captions: FrontRoomsSettings.SetCaptions(index == 1); break;
            case Key.RelayReadout: FrontRoomsSettings.SetRelayReadout(index == 1); break;
        }
        Changed?.Invoke();
    }

    /// <summary>← → (or a step): one option or stop; switches flip. QUALITY skips CUSTOM.</summary>
    public static void Step(Row row, int direction)
    {
        if (row == null) return;
        switch (row.Kind)
        {
            case Kind.Switch: Set(row, row.Index == 1 ? 0 : 1); return;
            case Kind.Action: if (direction == 0) Activate(row); return;
            case Kind.Segments:
            case Kind.Slider:
            {
                var n = row.Options.Length - (row.Key == Key.Quality ? 1 : 0);
                var from = row.Key == Key.Quality && row.Index == 4 ? (int)FrontRoomsQuality.CurrentPreset : row.Index;
                var to = Mathf.Clamp(from + (direction >= 0 ? 1 : -1), 0, n - 1);
                if (to != row.Index) Set(row, to);
                return;
            }
        }
    }

    /// <summary>Enter, Space, a click or a tap on the row itself: switches flip, actions run.</summary>
    public static void Activate(Row row)
    {
        if (row == null) return;
        if (row.Kind == Kind.Switch) { Set(row, row.Index == 1 ? 0 : 1); return; }
        if (row.Kind != Kind.Action || row.Key != Key.ResetGraphics) return;
        if (ResetArmed)
        {
            ResetArmedUntil = -1f;
            FrontRoomsQuality.ResetToDefault();
            HdrSet?.Invoke(true);
        }
        else ResetArmedUntil = Time.unscaledTime + 3f;
        Changed?.Invoke();
    }

    /// <summary>Backspace / X: the row back to its default.</summary>
    public static void ResetRow(Row row)
    {
        if (row == null || !row.Changed) return;
        if (row.Key == Key.Quality) { FrontRoomsQuality.PickPreset(FrontRoomsQuality.DefaultPreset); Changed?.Invoke(); return; }
        Set(row, row.DefaultIndex);
    }

    // ------------------------------------------------------------- geometry

    /// <summary>Sizes of the controls, in the sheet's units (desktop px at 1920 × 1080; handheld pt).</summary>
    public struct Metrics
    {
        public float Label, Row, SegH, SegPad, MinCell, SwitchW, SwitchH, Knob, WordGap, Track, TrackGap, ValueW, ThumbW, ThumbH;
    }

    public static readonly Metrics Desktop = new Metrics { Label = 20, Row = 48, SegH = 32, SegPad = 16, MinCell = 48, SwitchW = 48, SwitchH = 24, Knob = 16, WordGap = 12, Track = 240, TrackGap = 16, ValueW = 72, ThumbW = 6, ThumbH = 24 };
    public static readonly Metrics Touch = new Metrics { Label = 17, Row = 44, SegH = 30, SegPad = 12, MinCell = 44, SwitchW = 40, SwitchH = 22, Knob = 14, WordGap = 10, Track = 170, TrackGap = 12, ValueW = 58, ThumbW = 6, ThumbH = 22 };

    // Bayon's advance widths (em), read from Assets/Resources/Fonts/Bayon-Regular.ttf: A–Z, then 0–9.
    static readonly float[] BayonUpper = { .427f, .464f, .453f, .472f, .420f, .421f, .450f, .475f, .210f, .435f, .469f, .397f, .537f, .464f, .457f, .457f, .466f, .482f, .436f, .400f, .460f, .433f, .629f, .404f, .401f, .414f };
    static readonly float[] BayonDigits = { .497f, .210f, .439f, .440f, .451f, .433f, .455f, .442f, .449f, .449f };

    /// <summary>A label's width: Bayon from its own advances, IBM Plex Mono at 0.6 em a character.</summary>
    public static float TextWidth(string s, float size, bool mono)
    {
        if (string.IsNullOrEmpty(s)) return 0f;
        if (mono) return s.Length * .6f * size;
        var w = 0f;
        foreach (var c in s)
        {
            if (c >= 'A' && c <= 'Z') w += BayonUpper[c - 'A'];
            else if (c >= '0' && c <= '9') w += BayonDigits[c - '0'];
            else if (c == ' ') w += .26f;
            else if (c == '-') w += .303f;
            else w += .45f;
        }
        return w * size;
    }

    /// <summary>A segment cell's width: its label plus padding, never under the minimum target.</summary>
    public static float CellWidth(Row row, int i, Metrics m) =>
        Mathf.Max(m.MinCell, Mathf.Ceil(TextWidth(row.Options[i], m.Label, row.Mono)) + 2f * (row.Compact ? Mathf.Round(m.SegPad * .5f) : m.SegPad));

    /// <summary>The control's width (it sits against the row's right edge).</summary>
    public static float ControlWidth(Row row, Metrics m)
    {
        switch (row.Kind)
        {
            case Kind.Switch: return m.SwitchW + m.WordGap + TextWidth("OFF", m.Label, false);
            case Kind.Segments:
            {
                var w = 0f;
                for (var i = 0; i < row.Options.Length; i++) w += CellWidth(row, i, m);
                return w;
            }
            case Kind.Slider: return m.Track + m.TrackGap + m.ValueW;
            case Kind.Action: return Mathf.Ceil(TextWidth(row.ValueText, m.Label, false)) + 2f * m.SegPad;
            default: return 0f;
        }
    }

    /// <summary>The segment under x (x measured from the row's right edge, leftwards positive), or -1.</summary>
    public static int SegmentAt(Row row, float fromRight, Metrics m)
    {
        if (row.Kind != Kind.Segments) return -1;
        var right = 0f;
        for (var i = row.Options.Length - 1; i >= 0; i--)
        {
            var w = CellWidth(row, i, m);
            if (fromRight >= right && fromRight < right + w) return i;
            right += w;
        }
        return -1;
    }

    /// <summary>The slider track's span, measured from the row's right edge (start is the far, low end).</summary>
    public static void TrackSpan(Metrics m, out float lowFromRight, out float highFromRight)
    {
        highFromRight = m.ValueW + m.TrackGap;
        lowFromRight = highFromRight + m.Track;
    }

    /// <summary>Where a stop sits along the track (0 at the low end, 1 at the high end).</summary>
    public static float StopPosition(Row row, int i)
    {
        if (row.Stops == null || row.Stops.Length < 2) return 0f;
        var lo = row.Stops[0];
        var hi = row.Stops[row.Stops.Length - 1];
        return Mathf.InverseLerp(lo, hi, row.Stops[Mathf.Clamp(i, 0, row.Stops.Length - 1)]);
    }

    /// <summary>The stop nearest to x on the track (x from the row's right edge), or -1 when x is off the track.</summary>
    public static int StopAt(Row row, float fromRight, Metrics m, float slop)
    {
        if (row.Kind != Kind.Slider) return -1;
        TrackSpan(m, out var low, out var high);
        if (fromRight > low + slop || fromRight < high - slop) return -1;
        var t = Mathf.Clamp01((low - fromRight) / Mathf.Max(1f, low - high));
        var best = 0;
        for (var i = 1; i < row.Stops.Length; i++) if (Mathf.Abs(StopPosition(row, i) - t) < Mathf.Abs(StopPosition(row, best) - t)) best = i;
        return best;
    }
}
