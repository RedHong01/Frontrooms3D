using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An interactable office terminal on a desk CRT (game visual, 2026-10-30; research/computer_ui/41 + 20_design sections
/// 4.1, 9.2, 15.2). It owns the 80 x 25 grid, the panel machine and, while ACTIVE, one pooled text-mode picture, and puts
/// that picture on the monitor's own CRT tube (FrontRoomsScreenVideo.ShowTerminal; the ad, V4's still mix and the E power
/// switch step aside for a terminal). INERT until something calls <see cref="Attach"/>: no desk in main does yet (level
/// design picks the desks, L2), so nothing in play changes.
/// States (20_design section 4.1, budget section 9.2 with 41 section 3.1's raster):
///   TELL     not wanted, or over the budget: one shared tiny texture, the program's field and title band, no text;
///   ACTIVE   in view within r_A = min(8 m, 0.9 x the monitor's LOD cull distance): a live 1440 x 800 picture showing MAIN;
///   READING  the close-up is open (<see cref="BeginReading"/> .. <see cref="EndReading"/>): always live.
/// Live pictures come from a pool of <see cref="LiveBudget"/> (2 on desktop, 1 on handheld and WebGL), 5.9 MiB each; a
/// terminal keeps its picture until another one needs the slot. A picture renders only when its grid changed (a key, the
/// clock's minute, a face swap) or the GPU lost it: idle frames cost no draw and no allocation.
/// READ (TB2): a terminal carries a READ target built like the station TV's (ReadTarget; Q3 / A38: the E.1 eye and
/// "E . READ SCREEN" on readable terminals only), framed per class: desktop and WebGL as today's close-up (0.251 m, 70 %),
/// a phone full bleed (0.265 m, 95 % of the full height, no TEXT), an iPad at 70 %. FrontRoomsInspectTerminal joins the
/// close-up to the panel machine: it calls BeginReading / Apply / Press / Tap / EndReading and names the player's controls
/// with <see cref="SetControlNames"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class FrontRoomsTerminalScreen : MonoBehaviour
{
    public const float ActiveRadiusMax = 8f;
    /// <summary>A terminal keeps wanting its picture this far past r_A (no flicker at the edge).</summary>
    const float RadiusSlack = .5f;
    const int TellW = 90, TellH = 50;

    static readonly List<FrontRoomsTerminalScreen> all = new List<FrontRoomsTerminalScreen>();
    static readonly List<FrontRoomsTerminalRaster> freeRasters = new List<FrontRoomsTerminalRaster>();
    static readonly List<FrontRoomsTerminalScreen> ranked = new List<FrontRoomsTerminalScreen>();
    static readonly Plane[] planes = new Plane[6];
    static int rastersMade, tickFrame = -1, lastMinute = -1;
    static float clockWait;
    static Texture2D tellTexture;
    static bool faceHooked;
    static Camera fallbackCamera;

    /// <summary>A refused request (41 section 4.3) on this terminal: sound design's TerminalRefused (O-1: no tone in v1).</summary>
    public static event Action<FrontRoomsTerminalScreen, TerminalRefusal> TerminalRefused;
    /// <summary>An accepted key, tap or swipe: sound design's TerminalKey (41 section 4.5).</summary>
    public static event Action<FrontRoomsTerminalScreen> TerminalKey;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        all.Clear();
        foreach (var r in freeRasters) r?.Dispose();
        freeRasters.Clear();
        ranked.Clear();
        rastersMade = 0;
        tickFrame = -1;
        lastMinute = -1;
        clockWait = 0f;
        faceHooked = false;
        fallbackCamera = null;
        TerminalRefused = null;
        TerminalKey = null;
    }

    public static IReadOnlyList<FrontRoomsTerminalScreen> All => all;

    /// <summary>Live pictures at once: 2 on desktop, 1 on handheld and WebGL (20_design section 9.2, 41 section 3.1).</summary>
    public static int LiveBudget
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return 1;
#else
            return FrontRoomsHandheld.Active ? 1 : 2;
#endif
        }
    }

    /// <summary>Pictures created so far (never more than the budget) and their bytes.</summary>
    public static int PicturesMade => rastersMade;

    /// <summary>The TELL picture: the program's field with its title band, no text (shared, 90 x 50, about 24 KiB).</summary>
    public static Texture2D TellTexture
    {
        get
        {
            if (tellTexture != null) return tellTexture;
            var t = new Texture2D(TellW, TellH, TextureFormat.RGBA32, true, false)
            {
                name = "FrontRooms terminal TELL", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave,
            };
            var px = new Color32[TellW * TellH];
            var field = FrontRoomsTerminalPalette.Bg(FrontRoomsTerminalPalette.Field);
            var title = FrontRoomsTerminalPalette.Bg(FrontRoomsTerminalPalette.Title);
            var band = TellH / FrontRoomsTerminalGrid.Rows;   // one text row
            for (var y = 0; y < TellH; y++)
                for (var x = 0; x < TellW; x++)
                    px[y * TellW + x] = y >= TellH - band ? title : field;
            t.SetPixels32(px);
            t.Apply(true, true);
            tellTexture = t;
            return t;
        }
    }

    FrontRoomsScreenVideo screen;
    FrontRoomsTerminalGrid grid;
    FrontRoomsTerminal machine;
    FrontRoomsTerminalRaster raster;
    int renderedVersion = -1;
    bool reading, initialised, wanted;
    float distance, lodHeight = -1f, lodSize;
    Texture bound;

    // ---------------------------------------------------------------- the READ target (the EXAMINE terminal extension)

    /// <summary>The close-up a READ target is framed for: desktop and WebGL, a phone (full bleed), an iPad.</summary>
    public enum ReadClass { Desktop, Phone, Tablet }
    /// <summary>The lit text-mode picture on the glass (20_design 9.4; the tube's 276 x 207 mm underscan): the framed face.</summary>
    public static readonly Vector2 ReadSize = new Vector2(.276f, .207f);
    /// <summary>Desktop and WebGL: 0.251 m from the glass at the close-up's 61 degrees, the picture at 70 % of the height (20_design 2.4; today's framing).</summary>
    public const float DesktopReadDistance = .251f;
    /// <summary>
    /// Phone (Red 10-30, 交互设计 chat 11:00 and 游戏视觉 chat 11:1x; mock EC03 v3): 0.265 m from the glass, the picture at 95 % of
    /// the screen's full height, centred, the field of view narrowed to fit; no TEXT chip, no transcript panel.
    /// </summary>
    public const float PhoneReadDistance = .265f, PhoneFill = .95f;
    /// <summary>iPad: the picture at 70 % of the height (41 section 3.6), at the close-up's drawn field of view.</summary>
    public const float TabletFill = .70f;
    /// <summary>The close-up's field-of-view change on desktop (EXAMINE's flat default).</summary>
    const float FlatFovDelta = -15f;
    /// <summary>The aim box's depth behind the glass (the station TV's).</summary>
    const float ReadBoxDepth = .02f;

    FrontRoomsInspectable readTarget;
    InspectDef readDef;
    ReadClass readClass = (ReadClass)(-1);
    float readAspect = -1f, readBaseFov = -1f;
    Func<bool> readable;

    /// <summary>This terminal's READ target (null until its picture surface exists).</summary>
    public FrontRoomsInspectable ReadTarget => readTarget;
    /// <summary>The class its READ target is framed for now.</summary>
    public ReadClass ReadClassNow => readClass;
    /// <summary>The words row 25 was last named with (FrontRoomsInspectTerminal.Names; -1 = never).</summary>
    public int NamesApplied { get; set; } = -1;
    /// <summary>
    /// The eye and E . READ SCREEN show on it (Q3 / A38: readable terminals only; dark or still desk screens never): a terminal
    /// whose CRT is enabled, powered and showing the terminal's picture.
    /// </summary>
    public bool Readable => initialised && screen != null && screen.isActiveAndEnabled && screen.ShowsTerminal && screen.IsPoweredOn && !screen.IsOffForGood;

    /// <summary>The terminal whose READ target this is (null for any other target).</summary>
    public static FrontRoomsTerminalScreen Of(FrontRoomsInspectable target)
    {
        if (target == null) return null;
        for (var i = 0; i < all.Count; i++) if (all[i] != null && all[i].readTarget == target) return all[i];
        return null;
    }

    public int ContentKey { get; private set; }
    public FrontRoomsScreenVideo Screen => screen;
    public FrontRoomsTerminal Machine => machine;
    public FrontRoomsTerminalGrid Grid => grid;
    /// <summary>The live picture's raster (null in TELL).</summary>
    public FrontRoomsTerminalRaster Raster => raster;
    public bool Live => raster != null;
    public bool Reading => reading;
    /// <summary>What the tube shows now.</summary>
    public Texture Picture => raster != null ? raster.Picture : TellTexture;

    /// <summary>
    /// Make a desk CRT a terminal (level design, L2: which desks). The monitor needs its FrontRoomsScreenVideo first
    /// (FrontRoomsOfficeKit's AttachDesk). contentKey picks the content set (FrontRoomsTerminalContent.Load).
    /// </summary>
    public static FrontRoomsTerminalScreen Attach(GameObject monitor, int contentKey)
    {
        if (monitor == null) return null;
        var screen = monitor.GetComponent<FrontRoomsScreenVideo>();
        if (screen == null)
        {
            Debug.LogWarning("[FrontRoomsTerminalScreen] " + monitor.name + " has no FrontRoomsScreenVideo; not a terminal.");
            return null;
        }
        var t = monitor.GetComponent<FrontRoomsTerminalScreen>();
        if (t == null) t = monitor.AddComponent<FrontRoomsTerminalScreen>();
        t.Init(screen, contentKey);
        return t;
    }

    void Init(FrontRoomsScreenVideo s, int contentKey)
    {
        if (initialised && ContentKey == contentKey && screen == s) return;
        screen = s;
        ContentKey = contentKey;
        grid = new FrontRoomsTerminalGrid(FrontRoomsTerminalFace.Current);
        if (machine != null) Unhook();
        machine = new FrontRoomsTerminal(FrontRoomsTerminalContent.Load(contentKey), grid, contentKey);
        machine.Refused += OnRefused;
        machine.KeyAccepted += OnKey;
        var group = s.GetComponent<LODGroup>();
        if (group != null)
        {
            var lods = group.GetLODs();
            if (lods.Length > 0)
            {
                lodHeight = lods[lods.Length - 1].screenRelativeTransitionHeight;
                var ls = s.transform.lossyScale;
                lodSize = group.size * Mathf.Max(Mathf.Abs(ls.x), Mathf.Max(Mathf.Abs(ls.y), Mathf.Abs(ls.z)));
            }
        }
        initialised = true;
        renderedVersion = -1;
        NamesApplied = -1;
        Bind();
        RefreshRead();
    }

    /// <summary>
    /// The READ target, built like the playing station TV's (FrontRoomsScreenVideo.MakeInspectTarget): a flat "read" face on the
    /// picture surface, the terminal's own 276 x 207 mm, shipped, plastic, no zoom step (dist[1] = dist[0]; 20_design 9.4).
    /// Rebuilt per class (desktop / phone / iPad) when the class, the view's aspect or the player's FOV changes, never while
    /// reading (the open close-up keeps its solve).
    /// </summary>
    void RefreshRead()
    {
        if (!initialised || screen == null) return;
        if (readTarget == null && !MakeReadTarget()) return;
        if (reading) return;
        var cls = !FrontRoomsHandheld.Active ? ReadClass.Desktop : FrontRoomsHandheld.IsTablet ? ReadClass.Tablet : ReadClass.Phone;
        var cam = ViewCamera();
        var aspect = cam != null && cam.aspect > 0f ? cam.aspect : 16f / 9f;
        var baseFov = FrontRoomsInspect.RigBaseFov;
        if (cls == readClass && Mathf.Abs(aspect - readAspect) < 1e-3f && Mathf.Abs(baseFov - readBaseFov) < 1e-3f) return;
        readClass = cls;
        readAspect = aspect;
        readBaseFov = baseFov;
        var d = readDef;
        d.bleed = false;
        d.margin = 0f;
        d.fovDelta = 0f;
        d.transcript = FrontRoomsInspectCatalog.TerminalTranscript;
        var dist = DesktopReadDistance;
        if (cls == ReadClass.Phone)
        {
            // the FOV that puts the 207 mm picture at 95 % of the full height from 0.265 m, as drawn; handed over authored
            var drawn = 2f * Mathf.Atan(ReadSize.y / (PhoneFill * 2f * PhoneReadDistance)) * Mathf.Rad2Deg;
            d.fovDelta = AuthoredFov(drawn, aspect) - baseFov;
            d.margin = .01f;                 // composition B's margin, so the 95 % picture fits the full screen
            d.bleed = true;                  // the whole screen, not the safe area: centred, a thin dark margin top and bottom
            d.transcript = null;             // no TEXT chip, no transcript panel (Red 11:00)
            dist = PhoneReadDistance;
        }
        else if (cls == ReadClass.Tablet)
        {
            var drawn = FrontRoomsHandheld.AspectFov(baseFov + FlatFovDelta, aspect);
            dist = ReadSize.y / (TabletFill * 2f * Mathf.Tan(drawn * .5f * Mathf.Deg2Rad));
        }
        d.dist[0] = dist;
        d.dist[1] = dist;                    // the zoom step is never offered on a screen (no CLOSER hint, the wheel is MOVE)
    }

    static float AuthoredFov(float drawn, float aspect)
    {
        if (!FrontRoomsHandheld.Active || aspect <= 0f || aspect >= FrontRoomsHandheld.FovReferenceAspect) return drawn;
        return 2f * Mathf.Atan(Mathf.Tan(drawn * .5f * Mathf.Deg2Rad) * aspect / FrontRoomsHandheld.FovReferenceAspect) * Mathf.Rad2Deg;
    }

    bool MakeReadTarget()
    {
        var surface = screen.PictureSurface;
        if (surface == null) return false;
        var go = new GameObject("Terminal READ target");
        go.transform.SetParent(surface, false);
        // the surface's own scale never stretches the face
        var ls = surface.lossyScale;
        go.transform.localScale = new Vector3(ls.x != 0f ? 1f / ls.x : 1f, ls.y != 0f ? 1f / ls.y : 1f, ls.z != 0f ? 1f / ls.z : 1f);
        var w = ReadSize.x * .5f;
        var h = ReadSize.y * .5f;
        var info = new FrontRoomsKitLibrary.Info { boundsMin = new[] { -w, -h, -ReadBoxDepth }, boundsMax = new[] { w, h, 0f } };
        readDef = ReadDef();
        readTarget = go.AddComponent<FrontRoomsInspectable>();
        if (readable == null) readable = () => Readable;
        readTarget.Available = readable;
        readTarget.Configure(FrontRoomsInspectCatalog.TerminalKit, readDef, info);
        readTarget.EnsureRegistered();
        readClass = (ReadClass)(-1);
        return true;
    }

    /// <summary>
    /// The terminal's READ block (a fresh one per terminal; RefreshRead sets its distance per device). A43: explicitly a pause
    /// (InspectDef.PauseAlways): terminal pages are careful reading (系统设计, 2026-10-30), and its transcript is written only
    /// while it is read (and never on a phone), so the derived rule could not tell.
    /// </summary>
    public static InspectDef ReadDef() => new InspectDef
    {
        kind = "flat", verb = "read", ship = true, sound = "plastic", size = new[] { ReadSize.x, ReadSize.y },
        dist = new[] { DesktopReadDistance, DesktopReadDistance }, transcript = FrontRoomsInspectCatalog.TerminalTranscript,
        pause = InspectDef.PauseAlways,
    };

    void Unhook()
    {
        machine.Refused -= OnRefused;
        machine.KeyAccepted -= OnKey;
    }

    void OnRefused(TerminalRefusal kind) => TerminalRefused?.Invoke(this, kind);
    void OnKey() => TerminalKey?.Invoke(this);

    void OnEnable()
    {
        if (!all.Contains(this)) all.Add(this);
        if (!faceHooked)
        {
            faceHooked = true;
            FrontRoomsTerminalFace.Changed += OnFaceChanged;
        }
    }

    void OnDisable()
    {
        all.Remove(this);
        ReleaseRaster();
        if (all.Count == 0) DisposePool();
    }

    void OnDestroy()
    {
        ReleaseRaster();
        if (machine != null) Unhook();
        if (screen != null) screen.ClearTerminal();
        // the READ target goes with the terminal (an open close-up on it aborts through FrontRoomsInspect.Unregister)
        if (readTarget != null) Destroy(readTarget.gameObject);
        readTarget = null;
        if (all.Count == 0) DisposePool();
    }

    static void OnFaceChanged()
    {
        foreach (var t in all)
        {
            if (t.grid == null) continue;
            t.grid.Face = FrontRoomsTerminalFace.Current;
            t.machine.Refresh();
            t.renderedVersion = -1;
            t.Flush();
        }
    }

    // While a page is read the faceplate's reflections sit under the text; the glance keeps them (平面视觉, 2026-10-30).
    const float ReadingReflection = .08f, ReflectionFadeSeconds = .25f;
    float faceReflection = 1f;

    void StepFaceReflection(float dt)
    {
        var target = reading ? ReadingReflection : 1f;
        if (faceReflection == target || screen == null) return;
        faceReflection = Mathf.MoveTowards(faceReflection, target, (1f - ReadingReflection) * dt / ReflectionFadeSeconds);
        screen.SetFaceplateReflection(faceReflection);
    }

    // ---------------------------------------------------------------- reading (the EXAMINE terminal extension, next stage)

    /// <summary>The close-up opened on this terminal: it goes live at once and the machine opens on MAIN.</summary>
    public void BeginReading(float lockout = FrontRoomsTerminal.OpenLockout)
    {
        if (!initialised) return;
        reading = true;
        EnsureLive();
        machine.Open(lockout);
        Flush();
    }

    /// <summary>The close-up ended: back to MAIN (the glance).</summary>
    public void EndReading()
    {
        if (!initialised) return;
        reading = false;
        machine.Close();
        Flush();
    }

    public void Apply(in TerminalInput keys, float unscaledDt)
    {
        if (!initialised) return;
        machine.Apply(keys, unscaledDt);
        Flush();
    }

    public void Press(TerminalVerb verb)
    {
        if (!initialised) return;
        machine.Press(verb);
        Flush();
    }

    public void Tap(int col, int row)
    {
        if (!initialised) return;
        machine.Tap(col, row);
        Flush();
    }

    public bool WantsExit => machine != null && machine.WantsExit;
    public string Footer => machine != null ? machine.Footer : "";
    public string PanelTranscript => machine != null ? machine.PanelTranscript : "";

    /// <summary>Row 25 and HELP name the player's real controls (41 section 5.2): namer per action, soft keys on touch.</summary>
    public void SetControlNames(TerminalNamer namer, bool softKeys)
    {
        if (!initialised) return;
        machine.SoftKeys = softKeys;
        machine.Namer = namer;
        Flush();
    }

    // ---------------------------------------------------------------- live pictures

    void Flush()
    {
        if (raster == null) return;
        if (raster.Lost || grid.Version != renderedVersion)
        {
            raster.Render(grid, grid.Face);
            renderedVersion = grid.Version;
        }
        Bind();
    }

    void Bind()
    {
        if (screen == null) return;
        var p = Picture;
        if (p == bound && screen.ShowsTerminal) return;
        bound = p;
        screen.ShowTerminal(p);
    }

    void EnsureLive()
    {
        if (raster != null) return;
        var r = TakeRaster(this);
        if (r == null) return;
        raster = r;
        renderedVersion = -1;
        Flush();
    }

    void ReleaseRaster()
    {
        if (raster == null) return;
        freeRasters.Add(raster);
        raster = null;
        renderedVersion = -1;
        Bind();
    }

    static FrontRoomsTerminalRaster TakeRaster(FrontRoomsTerminalScreen forWhom)
    {
        if (freeRasters.Count > 0)
        {
            var r = freeRasters[freeRasters.Count - 1];
            freeRasters.RemoveAt(freeRasters.Count - 1);
            return r;
        }
        if (rastersMade < LiveBudget)
        {
            rastersMade++;
            return new FrontRoomsTerminalRaster();
        }
        // steal from the live terminal that wants it least: not reading, not wanted, farthest
        FrontRoomsTerminalScreen victim = null;
        foreach (var t in all)
        {
            if (t == forWhom || t.raster == null || t.reading) continue;
            if (victim == null || (victim.wanted && !t.wanted) || (victim.wanted == t.wanted && t.distance > victim.distance)) victim = t;
        }
        if (victim == null) return null;
        var stolen = victim.raster;
        victim.raster = null;
        victim.renderedVersion = -1;
        victim.Bind();
        return stolen;
    }

    static void DisposePool()
    {
        foreach (var r in freeRasters) r?.Dispose();
        freeRasters.Clear();
        rastersMade = 0;
    }

    /// <summary>r_A for this monitor and camera: min(8 m, 0.9 x the LOD cull distance at the current quality level).</summary>
    public float ActiveRadius(Camera cam)
    {
        if (lodHeight <= 0f || cam == null || cam.orthographic) return ActiveRadiusMax;
        var tan = Mathf.Tan(cam.fieldOfView * .5f * Mathf.Deg2Rad);
        var cull = lodSize * QualitySettings.lodBias / (2f * tan * lodHeight);
        return Mathf.Min(ActiveRadiusMax, .9f * cull);
    }

    bool Wants(Camera cam)
    {
        if (reading) return true;
        if (cam == null || screen == null || !screen.isActiveAndEnabled) return false;
        var centre = screen.PictureCentre;
        var eye = cam.transform.position;
        distance = Vector3.Distance(eye, centre);
        var radius = ActiveRadius(cam) + (raster != null ? RadiusSlack : 0f);
        if (distance > radius) return false;
        if (Vector3.Dot(screen.transform.forward, eye - centre) <= 0f) return false;   // its back or side
        return GeometryUtility.TestPlanesAABB(planes, new Bounds(centre, new Vector3(.35f, .35f, .35f)));
    }

    void LateUpdate()
    {
        if (all.Count > 0 && all[0] == this) TickAll();
    }

    /// <summary>Once per frame for every terminal: the clock's minute, the live set, renders of changed grids.</summary>
    static void TickAll()
    {
        if (Time.frameCount == tickFrame) return;
        tickFrame = Time.frameCount;
        clockWait -= Time.unscaledDeltaTime;
        if (clockWait <= 0f)
        {
            clockWait = 1f;
            var now = FrontRoomsTerminalContent.Now;
            var minute = now.Hour * 60 + now.Minute;
            if (minute != lastMinute)
            {
                lastMinute = minute;
                foreach (var t in all) if (t.machine != null) t.machine.Refresh();
            }
        }
        var cam = ViewCamera();
        if (cam != null) GeometryUtility.CalculateFrustumPlanes(cam, planes);
        ranked.Clear();
        foreach (var t in all)
        {
            if (!t.initialised) continue;
            t.wanted = t.Wants(cam);
            if (!t.wanted) continue;
            var i = 0;
            while (i < ranked.Count && Before(ranked[i], t)) i++;
            ranked.Insert(i, t);
        }
        var budget = LiveBudget;
        for (var i = 0; i < ranked.Count && i < budget; i++) ranked[i].EnsureLive();
        foreach (var t in all)
        {
            if (!t.initialised) continue;
            t.RefreshRead();
            t.StepFaceReflection(Time.unscaledDeltaTime);
            t.Flush();
        }
        // the EXAMINE terminal extension: an aborted close-up ends the reading; the phone's surround follows the picture
        FrontRoomsInspectTerminal.Watch(Time.unscaledDeltaTime);
    }

    /// <summary>
    /// The player's view camera (Land fix, 2026-10-30): the EXAMINE rig's camera once known, else main's view camera
    /// (FrontRoomsHeldItem.ViewCamera: enabled, no target texture, the highest depth), else Camera.main, else any camera.
    /// The game's first-person camera is not tagged MainCamera, so Camera.main is null in play, and a bare Find could
    /// return another camera (a capture or preview camera): the glance would then never go ACTIVE.
    /// </summary>
    static Camera ViewCamera()
    {
        var cam = FrontRoomsInspect.RigCamera;
        if (cam != null && cam.isActiveAndEnabled) return cam;
        cam = FrontRoomsHeldItem.ViewCamera;
        if (cam != null) return cam;
        cam = Camera.main;
        if (cam != null) return cam;
        if (fallbackCamera == null || !fallbackCamera.isActiveAndEnabled) fallbackCamera = FindAnyObjectByType<Camera>();
        return fallbackCamera;
    }

    static bool Before(FrontRoomsTerminalScreen a, FrontRoomsTerminalScreen b)
    {
        if (a.reading != b.reading) return a.reading;
        return a.distance <= b.distance;
    }
}
