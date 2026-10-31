using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FrontRooms.Map;
using UnityEditor;
using UnityEngine;
using I = FrontRoomsShotTimings.Inspect;

/// <summary>
/// EXAMINE, checked without playing (research/inspect/10_design.md §7.1 file 7, AT-01d, AT-11):
/// - the framing solver's numbers of §3.3 at 16:9, 16:10, 1.54, 1.50, 1.44, 4:3, 21:9 and the phone, with
///   every frame corner on screen (0 px clipped); the 60° approach; the clock's look-up pose;
/// - the angular zoom table of §3.4 (desktop 1080p and 4K, phone, iPad) and the readable sizes in px and arcmin;
/// - the sidecar blocks (placard, key tag, door sign) and the defaults;
/// - Find: nearest box, reach, angle, ship / DevTargets, hysteresis; zero allocations; Abort resets the statics;
/// - the rig contract (C16-C18, AT-11): full pose at motion 0.5 and 1, ×1.5 times, a cut at Off, the sine peak,
///   rotation 0.15 s and position 0.40 s on time, CancelShot shortens both, ShotEnded once, EasedWeight = the
///   camera's pull, fovMin for Inspect only, the 16-hit buffer, Cubic shots unchanged;
/// - no clipping: 50 approach positions and angles per asset in a closed test room (walls, door pier, ceiling).
/// Writes Verification/inspect/inspect-tests.json. Headless: -executeMethod FrontRoomsInspectTests.RunBatch -quit.
/// Camera motion is set by reflection (no PlayerPrefs write: the Editor shares them with Red's project).
/// </summary>
public static class FrontRoomsInspectTests
{
    const float Dt = 1f / 60f;

    [Serializable]
    sealed class Report
    {
        public string verdict;
        public int passed, failed;
        public List<string> checks = new List<string>();
        public List<string> numbers = new List<string>();
    }

    static Report report;

    static void Check(bool ok, string what)
    {
        if (ok) report.passed++; else report.failed++;
        report.checks.Add((ok ? "ok   " : "FAIL ") + what);
    }

    static void Note(string line) => report.numbers.Add(line);
    static string F(float v, string fmt = "0.000") => v.ToString(fmt, System.Globalization.CultureInfo.InvariantCulture);

    [MenuItem("FrontRooms/Rendering/Inspect/Run EXAMINE tests")]
    public static void Run() => Execute(false);

    public static void RunBatch() => Execute(true);

    static void Execute(bool throwOnFail)
    {
        report = new Report();
        var root = new GameObject("INSPECT TEST") { hideFlags = HideFlags.DontSave };
        root.transform.position = new Vector3(-42f * ModuleUnits.WorldPeriod, 0f, -40f * ModuleUnits.WorldPeriod);
        FrontRoomsSettings.Load();
        var motion = FrontRoomsSettings.CameraMotionPercent;
        SetMotion(100);
        try
        {
            SolverTests();
            ZoomTests();
            SidecarTests();
            FindTests(root.transform);
            RigTests(root.transform);
            ClipTests(root.transform);
            FixTests();
            ReframeTests(root.transform);
            A43Tests(root.transform);
        }
        catch (Exception e)
        {
            Check(false, "exception: " + e.GetType().Name + " " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            FrontRoomsInspect.Abort();
            FrontRoomsHandheld.ClearForce();
            UnityEngine.Object.DestroyImmediate(root);
            SetMotion(motion);
        }
        var pass = report.failed == 0 && report.passed > 0;
        report.verdict = (pass ? "PASS" : "FAIL") + ": " + report.passed + " passed, " + report.failed + " failed";
        var dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "inspect");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "inspect-tests.json"), JsonUtility.ToJson(report, true));
        Debug.Log("[InspectTests] " + report.verdict + "\n" + string.Join("\n", report.checks));
        if (throwOnFail && !pass) throw new Exception("[InspectTests] " + report.verdict);
    }

    static void SetMotion(int percent)
    {
        var prop = typeof(FrontRoomsSettings).GetProperty("CameraMotionPercent", BindingFlags.Public | BindingFlags.Static);
        prop?.GetSetMethod(true)?.Invoke(null, new object[] { percent });
    }

    // ------------------------------------------------------------------ solver (§3.2, §3.3, AT-01d)
    static readonly Vector2 PlacardSize = new Vector2(.4668f, .3138f);

    static FrontRoomsInspectFraming.Input PlacardInput(float aspect, Vector3 eyeOffset, float fov = 76f)
    {
        // A face at 1.524 m looking +Z; the player 1.5 m in front at eye height 1.62.
        var face = new Pose(new Vector3(0f, 1.524f, 0f), Quaternion.LookRotation(Vector3.forward, Vector3.up));
        var eye = new Vector3(0f, ModuleUnits.PlayerEye, 1.5f) + eyeOffset;
        return new FrontRoomsInspectFraming.Input
        {
            face = face, size = PlacardSize, margin = .08f, screen = new Rect(.01f, 0f, .63f, 1f),
            fov = fov + I.FovFlat, aspect = aspect, nearPlane = .06f, eye = eye,
            minDist = .18f, maxDist = 1f, maxAngle = 25f, maxYawOff = 25f, distMaxA = .44f,
            minY = eye.y - I.MaxDrop, maxY = eye.y + I.MaxRise, dist = new[] { .36f, 0f },
            depthFront = PlacardDepthFront, depthBack = PlacardDepthBack,
        };
    }

    /// <summary>Main's v3 sheet: 2048 px over 279 mm (the smaller density; 9,481 px/m across).</summary>
    const float SheetTpm = 7330f;

    // The placard frame about its sheet face: crown 13.5 mm off the wall, sheet at 4.55 mm (evac_placard_common.py).
    const float PlacardDepthFront = .0135f - .00455f, PlacardDepthBack = -.00455f;

    static bool CornersOnScreen(in FrontRoomsInspectFraming.Input input, in FrontRoomsInspectFraming.Result r, out float worst, out Rect bounds)
    {
        var right = FrontRoomsInspectFraming.FaceRight(input.face) * (input.size.x * .5f);
        var up = FrontRoomsInspectFraming.FaceUp(input.face) * (input.size.y * .5f);
        var n = FrontRoomsInspectFraming.FaceNormal(input.face);
        worst = 1f;
        float x0 = 1f, x1 = 0f, y0 = 1f, y1 = 0f;
        var ok = true;
        for (var i = 0; i < 8; i++)   // the 8 corners of the framed box (the frame's side walls included)
        {
            var c = input.face.position + n * (i < 4 ? input.depthFront : input.depthBack) + ((i & 1) == 0 ? right : -right) + ((i & 2) == 0 ? up : -up);
            var v = FrontRoomsInspectFraming.Project(r.pose, input.fov, input.aspect, c);
            if (float.IsNaN(v.x) || v.x < -1e-4f || v.x > 1.0001f || v.y < -1e-4f || v.y > 1.0001f) ok = false;
            worst = Mathf.Min(worst, Mathf.Min(Mathf.Min(v.x, 1f - v.x), Mathf.Min(v.y, 1f - v.y)));
            x0 = Mathf.Min(x0, v.x); x1 = Mathf.Max(x1, v.x); y0 = Mathf.Min(y0, v.y); y1 = Mathf.Max(y1, v.y);
        }
        bounds = Rect.MinMaxRect(x0, y0, x1, y1);
        return ok;
    }

    static void SolverTests()
    {
        // name, aspect, composition B?, expected distance (0 = do not check)
        var cases = new (string name, float aspect, bool b, float d)[]
        {
            // Fix 2026-10-14: the fit holds the frame's 8 box corners (its crown is 8.95 mm proud of the sheet), so A sits
            // 2-4 % further back than v1's 4-corner numbers (0.360 / 0.393 / 0.408 / B 0.319 / B 0.345), and distMaxA
            // 0.44 keeps A down to aspect 1.50 as §3.2 intends.
            ("16:9", 16f / 9f, false, .368f),
            ("16:10", 1.6f, false, .407f),
            ("1.54 (14-inch MBP)", 1.54f, false, .423f),
            ("1.50", 1.5f, false, 0f),
            ("21:9", 21f / 9f, false, .36f),
            ("phone 2.17 (2622 x 1206)", 2622f / 1206f, false, .36f),
            ("iPad 11-inch 1.44", 1.44f, true, .328f),
            ("4:3", 4f / 3f, true, .354f),
        };
        foreach (var c in cases)
        {
            var input = PlacardInput(c.aspect, Vector3.zero);
            var r = FrontRoomsInspectFraming.Solve(input);
            var onScreen = CornersOnScreen(input, r, out var worst, out var b);
            Check(r.ok && r.compositionB == c.b, "solver " + c.name + ": composition " + (r.compositionB ? "B" : "A") + " (expect " + (c.b ? "B" : "A") + ")");
            if (c.d > 0f) Check(Mathf.Abs(r.distance - c.d) <= .005f, "solver " + c.name + ": d " + F(r.distance) + " m (expect " + F(c.d) + " ± 0.005)");
            Check(onScreen, "solver " + c.name + ": all 8 frame-box corners on screen (0 px clipped; nearest edge " + F(worst * 100f, "0.0") + " %)");
            Check(Mathf.Abs(r.pose.position.y - 1.524f) < .002f, "solver " + c.name + ": camera height " + F(r.pose.position.y) + " = face centre 1.524");
            Note("solve " + c.name + ": comp " + (r.compositionB ? "B" : "A") + ", d " + F(r.distance) + " m, frame x " + F(b.xMin * 100f, "0.0") + "–" + F(b.xMax * 100f, "0.0") + " %, y " + F(b.yMin * 100f, "0.0") + "–" + F(b.yMax * 100f, "0.0") + " %, fill " + F(b.width * 100f, "0.0") + " % w, " + F(b.height * 100f, "0.0") + " % h");
            if (c.name == "16:9")
            {
                Check(Mathf.Abs(b.width - .619f) <= .02f && Mathf.Abs(b.height - .74f) <= .02f, "16:9 frame fill " + F(b.width * 100f, "0.0") + " % w × " + F(b.height * 100f, "0.0") + " % h (expect 62 ± 2 × 74 ± 2)");
                Check(b.xMin >= .01f - 1e-3f && b.xMax <= .64f + 1e-3f, "16:9 composition A keeps the plan in the left 63 % (" + F(b.xMin, "0.000") + "–" + F(b.xMax, "0.000") + ")");
            }
        }
        // The 60° approach: the yaw off square-on is capped at 25°, the frame still fits.
        foreach (var yaw in new[] { -60f, -30f, 30f, 60f })
        {
            var off = Quaternion.AngleAxis(yaw, Vector3.up) * new Vector3(0f, 0f, 1.5f) - new Vector3(0f, 0f, 1.5f);
            var input = PlacardInput(16f / 9f, off);
            var r = FrontRoomsInspectFraming.Solve(input);
            var yawOff = Vector3.Angle(new Vector3(r.direction.x, 0f, r.direction.z), Vector3.forward);
            var swing = Vector3.Angle(Vector3.ProjectOnPlane(-(input.eye - input.face.position), Vector3.up), r.pose.rotation * Vector3.forward);
            Check(r.ok && yawOff <= 25.01f && CornersOnScreen(input, r, out _, out _), "approach " + yaw + "°: yaw off square-on " + F(yawOff, "0.0") + "° (≤ 25), frame on screen");
            Note("approach " + yaw + "°: d " + F(r.distance) + ", yaw off " + F(yawOff, "0.0") + "°, camera swing " + F(swing, "0.0") + "° (design ≤ 35)");
        }
        // The clock (T1): a face at 2.1 m; the camera stays at eye + 0.10 and looks up about 40°.
        {
            var face = new Pose(new Vector3(0f, 2.1f, 0f), Quaternion.LookRotation(Vector3.forward, Vector3.up));
            var eye = new Vector3(0f, ModuleUnits.PlayerEye, 1.2f);
            var input = new FrontRoomsInspectFraming.Input
            {
                face = face, size = new Vector2(.32f, .32f), margin = .08f, fov = 76f + I.FovFlat, aspect = 16f / 9f, nearPlane = .06f,
                eye = eye, minDist = .18f, maxDist = 1f, maxAngle = 40f, maxYawOff = 25f, distMaxA = .44f,
                minY = eye.y - I.MaxDrop, maxY = eye.y + I.MaxRise, dist = new[] { .59f, 0f },
            };
            var r = FrontRoomsInspectFraming.Solve(input);
            Check(r.ok && Mathf.Abs(r.angle - 40f) <= 1.2f && r.pose.position.y <= eye.y + .1f + .006f && Mathf.Abs(r.distance - .59f) <= .02f,
                "clock: pitch " + F(r.angle, "0.0") + "° up (40 ± 1), camera " + F(r.pose.position.y) + " m (≤ eye + 0.10), d " + F(r.distance));
        }
        // Near-plane corner radius (§3.2): 0.113 at 76° and 16:9, 0.133 at 21:9.
        Check(Mathf.Abs(FrontRoomsInspectFraming.NearCornerRadius(.06f, 76f, 16f / 9f) - .113f) < .001f
              && Mathf.Abs(FrontRoomsInspectFraming.NearCornerRadius(.06f, 76f, 21f / 9f) - .133f) < .001f, "near-plane corner radius 0.113 (16:9) / 0.133 (21:9)");
        // A blocked path falls back (±20°, then B) or refuses: never a pose through a wall.
        {
            var input = PlacardInput(16f / 9f, Vector3.zero);
            input.sweep = (a, b, r) => 0.2f;   // everything blocked after 0.2 m
            var r = FrontRoomsInspectFraming.Solve(input);
            Check(!r.ok, "a fully blocked path gives no pose (" + r.why + ")");
        }
    }

    // ------------------------------------------------------------------ zoom and readability (§3.4)
    static void ZoomTests()
    {
        var input = PlacardInput(16f / 9f, Vector3.zero);
        var z1080 = FrontRoomsInspectFraming.ZoomFor(input, 2.4f, FrontRoomsInspectFraming.DesktopArcmin, 1080f, SheetTpm, .36f);
        var z2160 = FrontRoomsInspectFraming.ZoomFor(input, 2.4f, FrontRoomsInspectFraming.DesktopArcmin, 2160f, SheetTpm, .36f);
        Check(z1080.offered && Mathf.Abs(z1080.distance - .184f) <= .002f && Mathf.Abs(z1080.fov - 61f) < .01f, "zoom desktop 1080p: dolly to " + F(z1080.distance) + " m at FOV " + F(z1080.fov, "0.0") + " (expect 0.184, 61)");
        Check(Mathf.Abs(z2160.distance - .184f) <= .002f, "zoom desktop 4K: " + F(z2160.distance) + " m (the angle binds, same pose)");
        var phone = PlacardInput(2622f / 1206f, Vector3.zero);
        var zp = FrontRoomsInspectFraming.ZoomFor(phone, 2.4f, FrontRoomsInspectFraming.PhoneArcmin, 1206f, SheetTpm, .36f);
        Check(Mathf.Abs(zp.distance - .18f) < .001f && Mathf.Abs(zp.fov - 30.5f) <= .3f, "zoom phone: " + F(zp.distance) + " m at FOV " + F(zp.fov, "0.0") + "° (expect 0.18, 30.5 on main's 7,330 px/m sheet; the angle binds)");
        var pad = PlacardInput(1.44f, Vector3.zero);
        var zi = FrontRoomsInspectFraming.ZoomFor(pad, 2.4f, FrontRoomsInspectFraming.TabletArcmin, 1640f, SheetTpm, .319f);
        Check(Mathf.Abs(zi.fov - 51.4f) <= .4f, "zoom iPad: FOV " + F(zi.fov, "0.0") + "° (expect 51.4)");
        // Texel magnification at the desktop zoom on main's v3 sheet (7,330 px/m down): 1080p 0.68×, 4K 1.36× (≤ 2.0).
        var mag1080 = 1080f / z1080.visibleHeight / SheetTpm;
        var mag2160 = 2160f / z2160.visibleHeight / SheetTpm;
        Check(Mathf.Abs(mag1080 - .68f) < .02f && Mathf.Abs(mag2160 - 1.36f) < .03f && mag2160 <= 2.0001f, "texel magnification at the zoom: 1080p " + F(mag1080, "0.00") + "×, 4K " + F(mag2160, "0.00") + "× (≤ 2.0)");
        // Readable sizes (§3.4 table): px at 1080p and 4K, arcmin on the desktop reference.
        var lines = new (string name, float cap)[]
        {
            ("EVACUATION PLAN", 15f), ("glow legend words", 5.2f), ("YOU ARE HERE / footer", 4.4f), ("EXIT (plan)", 4.2f),
            ("grid bubbles", 3.6f), ("N", 3.2f), ("MATCH LINE", 3f), ("SCALE", 2.8f), ("WALLCOVERING 1/25", 2.4f),
        };
        foreach (var l in lines)
        {
            var b1080 = l.cap * FrontRoomsInspectFraming.PxPerMm(.36f, 61f, 1080f);
            var b2160 = l.cap * FrontRoomsInspectFraming.PxPerMm(.36f, 61f, 2160f);
            var zoom1080 = l.cap * FrontRoomsInspectFraming.PxPerMm(z1080.distance, 61f, 1080f);
            var zoom2160 = l.cap * FrontRoomsInspectFraming.PxPerMm(z2160.distance, 61f, 2160f);
            var arcBase = FrontRoomsInspectFraming.CapArcmin(l.cap, .36f, 61f, FrontRoomsInspectFraming.DesktopArcmin);
            var arcZoom = FrontRoomsInspectFraming.CapArcmin(l.cap, z1080.distance, 61f, FrontRoomsInspectFraming.DesktopArcmin);
            var arcPhone = FrontRoomsInspectFraming.CapArcmin(l.cap, zp.distance, zp.fov, FrontRoomsInspectFraming.PhoneArcmin);
            Note(l.name + " " + F(l.cap, "0.0") + " mm: base " + F(b1080, "0.0") + " px (1080p) / " + F(b2160, "0.0") + " px (4K), " + F(arcBase, "0.0") + "′; zoom " + F(zoom1080, "0.0") + " / " + F(zoom2160, "0.0") + " px, " + F(arcZoom, "0.0") + "′; phone zoom " + F(arcPhone, "0.0") + "′");
            Check(arcZoom >= 18.6f - .05f && arcPhone >= 18.0f, l.name + ": readable at the zoom step on desktop (" + F(arcZoom, "0.0") + "′ ≥ 18.6) and phone (" + F(arcPhone, "0.0") + "′ ≥ 18)");
        }
        var title = 15f * FrontRoomsInspectFraming.PxPerMm(.36f, 61f, 1080f);
        Check(Mathf.Abs(title - 38.2f) < .5f, "title 15 mm at base = " + F(title, "0.0") + " px at 1080p (design 38)");
        var smallest = 2.4f * FrontRoomsInspectFraming.PxPerMm(z1080.distance, 61f, 1080f);
        Check(smallest >= 11.9f, "smallest line 2.4 mm at the zoom = " + F(smallest, "0.0") + " px at 1080p (≥ 12)");
        // Key tag digits (E2): 15 mm at 0.20 m, FOV 66 → 62 px.
        var tag = 15f * FrontRoomsInspectFraming.PxPerMm(.2f, 66f, 1080f);
        Check(tag >= 55f, "key tag digits 15 mm at 0.20 m, FOV 66 = " + F(tag, "0.0") + " px (≥ 55; 62 expected)");
        // Door sign (C22 alternative): 17 mm cap at 0.30 m reads without a zoom; the zoom is not offered.
        var signInput = PlacardInput(16f / 9f, Vector3.zero);
        var zs = FrontRoomsInspectFraming.ZoomFor(signInput, 17f, FrontRoomsInspectFraming.DesktopArcmin, 1080f, 2048f, .30f);
        var sign = 17f * FrontRoomsInspectFraming.PxPerMm(.3f, 61f, 1080f);
        Check(!zs.offered && sign >= 12f, "door sign 17 mm at 0.30 m = " + F(sign, "0.0") + " px; zoom step not offered");
    }

    // ------------------------------------------------------------------ sidecars (§3.8)
    static void SidecarTests()
    {
        FrontRoomsKitLibrary.ClearCache();
        var placard = FrontRoomsKitLibrary.GetInfo("Kit_EvacPlacard");
        var p = placard?.inspect;
        Check(p != null && p.IsSet && p.Kind == InspectKind.Flat && p.ship && Mathf.Approximately(p.BaseDist, .36f) && Mathf.Approximately(p.Reach, 1.6f)
              && p.ScreenRect.HasValue && Mathf.Approximately(p.TexelsPerMetre, SheetTpm) && Mathf.Approximately(p.SmallestCapMm, 2.8f) && p.RegionCount == 4,
              "Kit_EvacPlacard sidecar: flat, ship, d 0.36, reach 1.6, composition A rect, 7330 texel/m, 4 regions (tutorial sheet A examine.json: plan, legend, alarm, title block), smallest cap 2.8 mm");
        // The transcript ships with the packed sheet (sheet A or its Phase 0, 平面视觉's examine.json): the printed header
        // and title block, exactly one glow line (EXIT over the stair door) and the four numbered alarm lines.
        {
            var tLines = p != null ? FrontRoomsInspectCatalog.Transcript(p.transcript) : null;
            var tGlow = 0;
            var tNumbered = 0;
            var tPrinted = 0;
            if (tLines != null)
                foreach (var l in tLines)
                {
                    if (l.Length > 0 && l[0] == '\u0002') continue;   // a region head, not a printed line
                    tPrinted++;
                    if (l.Length > 0 && l[0] == '\u0001') tGlow++;
                    if (l.Length > 2 && l[0] >= '1' && l[0] <= '4' && l[1] == ' ') tNumbered++;
                }
            Check(tLines != null && tPrinted >= 15 && tGlow == 1 && tNumbered == 4
                  && tLines[0] == "EVACUATION PLAN \u00B7 LEVEL 0" && tLines[tLines.Length - 1] == "LEVEL 0 \u00B7 SHEET A-2 OF 4 \u00B7 PRINTED 11/89",
                "placard transcript '" + (p != null ? p.transcript : "-") + "': " + tPrinted + " printed lines, " + tGlow + " glow line, " + tNumbered + " numbered lines, sheet A header and title block");
        }
        RegionHeadTests(p);
        if (p != null && p.box != null && p.box.c != null)
            Check(Mathf.Abs(p.box.c[2] - .00675f) < .0005f && Mathf.Abs(p.box.s[0] - .4668f) < .0005f, "Kit_EvacPlacard aim box c.z " + F(p.box.c[2], "0.0000") + ", w " + F(p.box.s[0], "0.0000") + " (Unity part-local)");
        var tag = FrontRoomsKitLibrary.GetInfo("Kit_KeyTag_Rect")?.inspect;
        Check(tag != null && tag.IsSet && tag.Kind == InspectKind.Object && !tag.ship && Mathf.Approximately(tag.BaseDist, .2f) && Mathf.Approximately(tag.FovDelta, -10f),
              "Kit_KeyTag_Rect sidecar: object, ship false (D1.5), d 0.20, FOV -10");
        var sign = FrontRoomsKitLibrary.GetInfo("Kit_DoorSign")?.inspect;
        Check(sign != null && sign.IsSet && !sign.ship && Mathf.Approximately(sign.Reach, 1.2f), "Kit_DoorSign sidecar: flat, ship false (read in place in v1; R5-B test bed), reach 1.2");
        var none = FrontRoomsKitLibrary.GetInfo("Kit_KeyRing")?.inspect;
        Check(none == null || !none.IsSet, "a kit without a block is not inspectable (Kit_KeyRing)");
        var d = new InspectDef { kind = "flat" };
        Check(Mathf.Approximately(d.Reach, 1.6f) && Mathf.Approximately(d.ApproachAngle, 60f) && Mathf.Approximately(d.Margin, .08f) && Mathf.Approximately(d.MinDist, .18f)
              && Mathf.Approximately(d.FovDelta, -15f) && Mathf.Approximately(d.MaxAngleFor(1.5f), 25f) && Mathf.Approximately(d.MaxAngleFor(2.1f), 40f) && !d.ship && d.Verb == "read",
              "defaults: reach 1.6, angle 60, margin 0.08, minDist 0.18, FOV -15, maxAngle 25 (40 above 1.9 m), ship false, verb read");
        var o = new InspectDef { kind = "object" };
        Check(Mathf.Approximately(o.FovDelta, -10f) && o.Verb == "look", "object defaults: FOV -10, verb look");
        Check(FrontRoomsInspectCatalog.Fallback("Kit_WallClock") != null && !FrontRoomsInspectCatalog.Fallback("Kit_WallClock").ship, "test-bed fallback blocks carry ship false");
    }

    // ------------------------------------------------------------------ transcript region heads (G9, 2026-10-22)
    [Serializable]
    sealed class ExamineRegion { public string id; }

    [Serializable]
    sealed class ExamineFile { public ExamineRegion[] regions; public string[] transcript; }

    // The region words signed by narrative (40 section 4), per examine.json region id.
    static string RegionHead(string id)
    {
        switch (id)
        {
            case "plan": return "PLAN";
            case "legend": return "LEGEND";
            case "alarm": return "IN CASE OF ALARM";
            case "titleblock": return "TITLE BLOCK";
            default: return "?" + id;
        }
    }

    // The format agreed with interaction design (research/desktop_ui/EXAMINE_TEXT_G9_G10.md G9): '\u0002' + a region's name
    // heads its lines, one head per examine.json region in its order; every other line is examine.json's, byte for byte;
    // a head never glows. Tools/lookdev/inspect_transcript.py writes the catalog entry from the sheet's examine.json.
    static void RegionHeadTests(InspectDef p)
    {
        var tLines = p != null ? FrontRoomsInspectCatalog.Transcript(p.transcript) : null;
        var printed = new List<string>();
        var heads = new List<string>();
        var headAt = new List<int>();
        var badMarks = 0;
        if (tLines != null)
            for (var i = 0; i < tLines.Length; i++)
            {
                var l = tLines[i];
                var isHead = l.Length > 0 && l[0] == '\u0002';
                if (isHead) { heads.Add(l.Substring(1)); headAt.Add(i); } else printed.Add(l);
                var glowAt = l.IndexOf('\u0001');
                var headMarkAt = l.IndexOf('\u0002');
                if ((glowAt >= 0 && headMarkAt >= 0) || glowAt > 0 || headMarkAt > 0) badMarks++;
            }
        Check(tLines != null && badMarks == 0, "placard transcript: no line carries both markers (a head never glows); a marker only leads its line (" + badMarks + " bad)");

        // Which sheet folder the catalog came from: the examine.json whose transcript equals the printed lines.
        ExamineFile ex = null;
        string source = null;
        var sheets = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Tools", "print", "ink", "art_from_graphic", "placard");
        if (tLines != null && Directory.Exists(sheets))
        {
            var dirs = Directory.GetDirectories(sheets);
            Array.Sort(dirs, StringComparer.Ordinal);
            foreach (var dir in dirs)
            {
                var f = Path.Combine(dir, "examine.json");
                if (!File.Exists(f)) continue;
                ExamineFile e = null;
                try { e = JsonUtility.FromJson<ExamineFile>(File.ReadAllText(f)); } catch (Exception) { e = null; }
                if (e == null || e.transcript == null || e.transcript.Length != printed.Count) continue;
                var same = true;
                for (var i = 0; i < printed.Count && same; i++) same = string.Equals(e.transcript[i], printed[i], StringComparison.Ordinal);
                if (same) { ex = e; source = Path.GetFileName(dir); break; }
            }
        }
        Check(ex != null, "placard transcript: every line but the heads is examine.json's, byte for byte (" + (source != null ? source + "/examine.json" : "no sheet folder under Tools/print/ink/art_from_graphic/placard matches") + ", " + printed.Count + " lines)");

        var want = new List<string>();
        if (ex != null && ex.regions != null) foreach (var r in ex.regions) want.Add(RegionHead(r.id));
        Check(ex != null && heads.Count == 4 && want.Count == 4 && string.Join("|", heads.ToArray()) == string.Join("|", want.ToArray()),
            "placard transcript: 4 region heads in examine.json's order (" + string.Join(", ", heads.ToArray()) + "; examine.json " + string.Join(", ", want.ToArray()) + ")");

        // Each head opens its region where the sheet puts it: the title band (line 0) above every region, PLAN next, LEGEND at
        // the legend line, IN CASE OF ALARM at the alarm block, TITLE BLOCK at the last line; no region is empty.
        var placed = tLines != null && headAt.Count == 4 && tLines[0].StartsWith("EVACUATION PLAN", StringComparison.Ordinal) && headAt[0] == 1
                     && headAt[1] + 1 < tLines.Length && tLines[headAt[1] + 1].StartsWith("LEGEND", StringComparison.Ordinal)
                     && headAt[2] + 1 < tLines.Length && tLines[headAt[2] + 1] == "IN CASE OF ALARM"
                     && headAt[3] == tLines.Length - 2;
        var sizes = "";
        for (var k = 0; k < headAt.Count; k++)
        {
            var n = (k + 1 < headAt.Count ? headAt[k + 1] : (tLines != null ? tLines.Length : 0)) - headAt[k] - 1;
            if (n < 1) placed = false;
            sizes += (k > 0 ? ", " : "") + heads[k] + " " + n;
        }
        Check(placed, "placard transcript regions: title band " + (headAt.Count > 0 ? headAt[0] : 0) + " line, " + sizes);

        // The plain text (TranscriptText): never a raw marker, heads by name after a blank line, the glow line only while
        // legible, and a head whose lines are all hidden is dropped.
        var syn = new[] { "T", "\u0002A", "\u0001g", "\u0002B", "x" };
        var lit = FrontRoomsInspect.TranscriptPlain(tLines, false);
        var dark = FrontRoomsInspect.TranscriptPlain(tLines, true);
        Check(tLines != null && lit.IndexOf('\u0001') < 0 && lit.IndexOf('\u0002') < 0 && dark.IndexOf('\u0001') < 0 && dark.IndexOf('\u0002') < 0
              && dark.Contains("\n\nPLAN\n") && dark.Contains("\n\nTITLE BLOCK\n") && dark.Contains("EXIT (over the stair door") && !lit.Contains("EXIT (over the stair door")
              && FrontRoomsInspect.TranscriptPlain(syn, false) == "T\n\nB\nx" && FrontRoomsInspect.TranscriptPlain(syn, true) == "T\n\nA\ng\n\nB\nx"
              && FrontRoomsInspect.TranscriptPlain(null, true) == "",
            "plain transcript: no raw marker; heads by name after a blank line; the glow line only while legible; a head over hidden lines only is dropped (" + lit.Split('\n').Length + " / " + dark.Split('\n').Length + " text lines)");
        // A head said once (平面视觉 G9, 10-24 12:22): a region whose first shown line repeats its head shows the head only; a line that
        // only starts with the head's words stays; the title band stays first (catalog order).
        var rep = new[] { "T", "\u0002H", "H", "y", "\u0002G", "\u0001G", "G", "\u0002K", "K: z" };
        Check(FrontRoomsInspect.TranscriptPlain(rep, false) == "T\n\nH\ny\n\nG\n\nK\nK: z" && FrontRoomsInspect.TranscriptPlain(rep, true) == "T\n\nH\ny\n\nG\nG\n\nK\nK: z"
              && lit.StartsWith("EVACUATION PLAN · LEVEL 0\n\nPLAN\n") && lit.Contains("\n\nIN CASE OF ALARM\nON ALARM, A FLOOR WARDEN") && !lit.Contains("IN CASE OF ALARM\nIN CASE OF ALARM")
              && lit.Contains("\n\nLEGEND\nLEGEND: YOU ARE HERE"),
            "plain transcript: a repeated head is said once (IN CASE OF ALARM); LEGEND: ... stays; the title line comes first");
    }

    // ------------------------------------------------------------------ Find (§2.2), allocations, statics
    static FrontRoomsInspectable MakeTarget(Transform parent, string name, Vector3 at, bool ship, float reach = 0f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = at;
        var def = new InspectDef { kind = "flat", ship = ship, size = new[] { .4f, .3f }, reach = reach, box = new InspectDef.Box { c = new[] { 0f, 0f, .007f }, s = new[] { .4f, .3f, .014f } } };
        var t = go.AddComponent<FrontRoomsInspectable>();
        t.Configure(name, def, null);
        FrontRoomsInspect.Register(t);
        return t;
    }

    static void FindTests(Transform root)
    {
        FrontRoomsInspect.Abort();
        var o = root.position;
        var near = MakeTarget(root, "near", o + new Vector3(0f, 1.5f, 0f), true);
        var far = MakeTarget(root, "far", o + new Vector3(0f, 1.5f, -.5f), true);
        var dev = MakeTarget(root, "dev", o + new Vector3(2f, 1.5f, 0f), false);
        var eye = o + new Vector3(0f, 1.5f, 1.2f);
        var ray = new Ray(eye, Vector3.back);
        Check(FrontRoomsInspect.Find(ray, 5f, out var t) && t == near, "Find: the nearest box wins (" + (t != null ? t.name : "none") + ")");
        Check(!FrontRoomsInspect.Find(ray, 1.0f, out _), "Find: a box behind the physics hit + slack is ignored");
        var farRay = new Ray(o + new Vector3(0f, 1.5f, 1.75f), Vector3.back);
        FrontRoomsInspect.Abort();
        Check(!FrontRoomsInspect.Find(farRay, 5f, out _), "Find: out of reach at 1.75 m (reach 1.6)");
        Check(FrontRoomsInspect.Find(new Ray(o + new Vector3(0f, 1.5f, 1.55f), Vector3.back), 5f, out _) && FrontRoomsInspect.Find(new Ray(o + new Vector3(0f, 1.5f, 1.68f), Vector3.back), 5f, out _),
              "Find: hysteresis keeps the prompt to 1.7 m once shown");
        FrontRoomsInspect.Abort();
        // 70° off the face normal: refused (approach angle 60).
        var side = Quaternion.AngleAxis(70f, Vector3.up) * Vector3.forward * 1.2f;
        var sideEye = near.Face.position + side;
        Check(!FrontRoomsInspect.Find(new Ray(sideEye, (near.Face.position - sideEye).normalized), 5f, out _), "Find: refused at 70° off the face (approach 60°)");
        var devRay = new Ray(o + new Vector3(2f, 1.5f, 1.2f), Vector3.back);
        Check(!FrontRoomsInspect.Find(devRay, 5f, out _), "Find: ship false is skipped without DevTargets");
        FrontRoomsInspect.DevTargets = true;
        Check(FrontRoomsInspect.Find(devRay, 5f, out var d2) && d2 == dev, "Find: ship false is found with DevTargets");
        // Aim forgiveness: a 2 cm pad.
        Check(FrontRoomsInspect.Find(new Ray(o + new Vector3(.215f, 1.5f, 1.2f), Vector3.back), 5f, out _) && !FrontRoomsInspect.Find(new Ray(o + new Vector3(.23f, 1.5f, 1.2f), Vector3.back), 5f, out _),
              "Find: 2 cm aim pad (hit at 0.215, miss at 0.23 from the centre of a 0.40 m box)");
        // Zero allocation: Find and the pure solver.
        var input = PlacardInput(16f / 9f, Vector3.zero);
        FrontRoomsInspect.Find(ray, 5f, out _);
        FrontRoomsInspectFraming.Solve(input);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++) FrontRoomsInspect.Find(ray, 5f, out _);
        var findBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100; i++) FrontRoomsInspectFraming.Solve(input);
        var solveBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(findBytes == 0 && solveBytes == 0, "0 B allocated: 1000 × Find (" + findBytes + " B), 100 × Solve (" + solveBytes + " B)");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (var i = 0; i < 1000; i++) FrontRoomsInspect.Find(ray, 5f, out _);
        var findMs = sw.Elapsed.TotalMilliseconds / 1000.0;
        sw.Restart();
        for (var i = 0; i < 100; i++) FrontRoomsInspectFraming.Solve(input);
        var solveMs = sw.Elapsed.TotalMilliseconds / 100.0;
        Note("timing (edit mode, under the machine's load): Find " + (findMs * 1000).ToString("0.0") + " µs with 3 targets, Solve " + (solveMs * 1000).ToString("0.0") + " µs (no world)");
        // Statics.
        FrontRoomsInspect.DevTargets = true;
        FrontRoomsInspect.Abort();
        Check(!FrontRoomsInspect.DevTargets && !FrontRoomsInspect.Active && FrontRoomsInspect.Current == null, "Abort resets the statics (DevTargets, Active, Current)");
        UnityEngine.Object.DestroyImmediate(near.gameObject);
        UnityEngine.Object.DestroyImmediate(far.gameObject);
        UnityEngine.Object.DestroyImmediate(dev.gameObject);
    }

    // ------------------------------------------------------------------ the rig contract (C16-C18, AT-11)
    static (FrontRoomsCameraRig rig, Transform body, Camera camera) MakeRig(Transform root)
    {
        var body = new GameObject("body").transform;
        body.SetParent(root, false);
        var camera = new GameObject("camera").AddComponent<Camera>();
        camera.transform.SetParent(root, false);
        camera.fieldOfView = 76f;
        var rig = FrontRoomsCameraRig.Attach(body, camera, ModuleUnits.PlayerEye);
        rig.SetBase(0f);
        return (rig, body, camera);
    }

    static ShotSpec InspectSpec(Pose target, float blendIn = .44f)
    {
        return new ShotSpec
        {
            kind = ShotKind.Inspect, target = () => target, blendIn = blendIn, blendOut = I.BlendOut, rotBlendOut = I.RotBlendOut,
            fovDelta = I.FovFlat, lockMove = true, lockLook = true, cancel = ShotCancel.Relay, fullPose = true, ease = ShotEase.Sine,
        };
    }

    static float Run(FrontRoomsCameraRig rig, float seconds, Func<bool> until = null)
    {
        var t = 0f;
        while (t < seconds - 1e-5f)
        {
            rig.Tick(Dt);
            t += Dt;
            if (until != null && until()) break;
        }
        return t;
    }

    static void RigTests(Transform root)
    {
        var (rig, body, camera) = MakeRig(root);
        var eye = rig.BaseEye;
        var target = new Pose(eye.position + new Vector3(.3f, -.1f, 1.2f), Quaternion.Euler(0f, 30f, 0f));
        var ended = 0;
        Action<ShotKind, Vector3> onEnd = (k, f) => { if (k == ShotKind.Inspect) ended++; };
        FrontRoomsCameraRig.ShotEnded += onEnd;
        try
        {
            // Full pose at 100 and 50: the same pose at weight 1.
            SetMotion(100);
            var h = rig.BeginShot(InspectSpec(target));
            var t100 = Run(rig, 2f, () => h.Weight >= 1f);
            var p100 = camera.transform.position;
            Check((p100 - target.position).magnitude < 1e-4f && Mathf.Abs(camera.fieldOfView - 61f) < .01f, "motion 100: arrives at the full pose, FOV 61, in " + F(t100) + " s (0.44)");
            rig.CancelShot(h, 0f); rig.Tick(Dt);
            SetMotion(50);
            h = rig.BeginShot(InspectSpec(target));
            var t50 = Run(rig, 2f, () => h.Weight >= 1f);
            Check((camera.transform.position - target.position).magnitude < 1e-4f && Mathf.Abs(t50 - .66f) <= Dt + 1e-4f, "motion 50: same pose, in " + F(t50) + " s (0.44 × 1.5 = 0.66)");
            rig.CancelShot(h, 0f); rig.Tick(Dt);
            SetMotion(0);
            h = rig.BeginShot(InspectSpec(target));
            rig.Tick(Dt);
            Check((camera.transform.position - target.position).magnitude < 1e-4f, "motion Off: a cut in (one frame)");
            rig.EndShot(h); rig.Tick(Dt);
            Check(h.Ended && (camera.transform.position - rig.BaseEye.position).magnitude < 1e-4f, "motion Off: a cut out (one frame)");
            SetMotion(100);

            // Sine in: peak speed / average = π/2.
            ended = 0;
            h = rig.BeginShot(InspectSpec(target));
            var last = rig.BaseEye.position; var peak = 0f; var travel = (target.position - last).magnitude; var steps = 0;
            while (h.Weight < 1f && steps < 200) { rig.Tick(Dt); var p = camera.transform.position; peak = Mathf.Max(peak, (p - last).magnitude / Dt); last = p; steps++; }
            var avg = travel / .44f;
            Check(Mathf.Abs(peak / avg - Mathf.PI / 2f) < .06f, "sine in-out: peak " + F(peak, "0.00") + " m/s = " + F(peak / avg, "0.00") + " × the average (1.57)");
            Note("travel " + F(travel, "0.00") + " m in 0.44 s: peak " + F(peak, "0.00") + " m/s (design 4.3 m/s at 1.2 m)");
            Check(Mathf.Abs(h.EasedWeight - 1f) < 1e-5f && (camera.transform.position - target.position).magnitude < 1e-4f, "EasedWeight 1 = the camera on the target");
            // Out: rotation home in 0.15 s, position in 0.40 s.
            rig.EndShot(h);
            float rotHome = -1f, posHome = -1f, tt = 0f;
            var maxDeg = 0f; var lastRot = camera.transform.rotation;
            while (!h.Ended && tt < 1f)
            {
                rig.Tick(Dt); tt += Dt;
                maxDeg = Mathf.Max(maxDeg, Quaternion.Angle(lastRot, camera.transform.rotation) / Dt); lastRot = camera.transform.rotation;
                if (rotHome < 0f && Quaternion.Angle(camera.transform.rotation, rig.BaseEye.rotation) < .01f) rotHome = tt;
                if (posHome < 0f && (camera.transform.position - rig.BaseEye.position).magnitude < 1e-4f) posHome = tt;
                // EasedWeight equals the camera's pull toward the target.
                var pull = Vector3.Distance(rig.BaseEye.position, camera.transform.position) / travel;
                if (!h.Ended && Mathf.Abs(pull - h.EasedWeight) > 1e-3f) { Check(false, "EasedWeight " + F(h.EasedWeight) + " ≠ the camera pull " + F(pull)); break; }
            }
            Check(Mathf.Abs(rotHome - .15f) <= .02f && Mathf.Abs(posHome - .40f) <= .02f, "return: rotation home at " + F(rotHome) + " s (0.15 ± 0.02), position at " + F(posHome) + " s (0.40 ± 0.02)");
            Check(ended == 1, "ShotEnded(Inspect) fired once (" + ended + ")");
            Note("return peak rotation speed " + F(maxDeg, "0") + " °/s for a " + F(Quaternion.Angle(target.rotation, rig.BaseEye.rotation), "0.0") + "° swing (design ≤ 370)");
            // An early exit returns from the current weights, with no jump.
            h = rig.BeginShot(InspectSpec(target));
            Run(rig, .2f);
            var before = camera.transform.position;
            rig.EndShot(h); rig.Tick(Dt);
            Check((camera.transform.position - before).magnitude < .05f, "an early exit at 0.20 s returns from where the camera is (step " + F((camera.transform.position - before).magnitude) + " m)");
            Run(rig, 1f, () => h.Ended);
            // CancelShot shortens both (Relay: 0.15).
            h = rig.BeginShot(InspectSpec(target));
            Run(rig, 1f, () => h.Weight >= 1f);
            rig.EndShot(h);
            rig.CancelForRelay();
            var cancelT = Run(rig, 1f, () => h.Ended);
            Check(cancelT <= .15f + Dt + 1e-4f, "CancelForRelay during the return: home in " + F(cancelT) + " s (≤ 0.15)");
            // fovMin: Inspect only.
            var spec = InspectSpec(target); spec.fovDelta = -40f; spec.fovMin = 31.2f; spec.fov = () => 31.2f - 76f;
            h = rig.BeginShot(spec); Run(rig, 1f, () => h.Weight >= 1f);
            Check(Mathf.Abs(camera.fieldOfView - 31.2f) < .05f, "fovMin honoured for Inspect: FOV " + F(camera.fieldOfView, "0.0") + " (31.2)");
            rig.CancelShot(h, 0f); rig.Tick(Dt);
            var other = new ShotSpec { kind = ShotKind.Unlock, target = () => target, blendIn = .1f, blendOut = .1f, fovDelta = -40f, fovMin = 31.2f, fov = () => -44.8f };
            h = rig.BeginShot(other); Run(rig, .5f);
            Check(Mathf.Abs(camera.fieldOfView - 61f) < .05f, "fovMin ignored for other kinds: FOV " + F(camera.fieldOfView, "0.0") + " (61 = 76 - 15)");
            rig.CancelShot(h, 0f); rig.Tick(Dt);
            // Cubic shots behave as before: the same pull at the same weight.
            var cubic = new ShotSpec { kind = ShotKind.Open, target = () => target, blendIn = .5f, blendOut = .5f };
            h = rig.BeginShot(cubic); Run(rig, .25f);
            var w = h.Weight; var expect = w < .5f ? 4f * w * w * w : 1f - Mathf.Pow(-2f * w + 2f, 3f) * .5f;
            var pulled = Vector3.Distance(rig.BaseEye.position, camera.transform.position) / travel;
            Check(Mathf.Abs(pulled - expect) < 1e-3f, "a Cubic shot keeps the old ease (pull " + F(pulled) + " = Ease(" + F(w) + ") " + F(expect) + ")");
            rig.CancelShot(h, 0f); rig.Tick(Dt);
            // C17d: 16 hits.
            var hits = typeof(FrontRoomsCameraRig).GetField("hits", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(rig) as RaycastHit[];
            Check(hits != null && hits.Length == 16, "the rig's clamp buffer holds 16 hits (C17d)");
        }
        finally
        {
            FrontRoomsCameraRig.ShotEnded -= onEnd;
            SetMotion(100);
            UnityEngine.Object.DestroyImmediate(body.gameObject);   // the camera is parented under the body's eye by Attach
            if (camera != null) UnityEngine.Object.DestroyImmediate(camera.gameObject);
        }
    }

    // ------------------------------------------------------------------ no clipping (50 approaches per asset)
    /// <summary>
    /// A closed 3 m cell: the placard on the facade beside a door pier (0.30 m proud), a side wall 1.42 m off,
    /// floor and ceiling (2.74 m). 50 eye positions (0.5–1.6 m, -60..60°) per asset. Every ok pose must: keep its
    /// near-plane sphere clear of colliders, have a clear swept path from the eye (sampled along the eased travel
    /// with the near-corner radius), and keep all 4 frame corners on screen with nothing between the camera and them.
    /// </summary>
    static void ClipTests(Transform root)
    {
        var stage = new GameObject("clip stage").transform;
        stage.SetParent(root, false);
        var o = root.position + new Vector3(0f, 0f, 30f);
        void Box(string name, Vector3 centre, Vector3 size)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(stage, false);
            g.transform.position = o + centre;
            g.transform.localScale = size;
        }
        Box("facade", new Vector3(0f, 1.5f, -.05f), new Vector3(6f, 3f, .1f));
        Box("pier", new Vector3(-.55f, 1.5f, .15f), new Vector3(.2f, 3f, .3f));           // a door pier 0.30 m proud, 0.33 m left of the frame edge
        Box("side wall", new Vector3(1.47f, 1.5f, 1.5f), new Vector3(.1f, 3f, 3f));
        Box("floor", new Vector3(0f, -.05f, 1.5f), new Vector3(6f, .1f, 3f));
        Box("ceiling", new Vector3(0f, 2.79f, 1.5f), new Vector3(6f, .1f, 3f));
        Physics.SyncTransforms();
        var assets = new (string name, Vector2 size, float y, float dist, bool screenA)[]
        {
            ("placard", PlacardSize, 1.524f, .36f, true),
            ("door sign (R5-B)", new Vector2(.254f, .076f), 1.524f, .30f, false),
            ("clock (T1)", new Vector2(.32f, .32f), 2.1f, .59f, false),
        };
        Func<Vector3, Vector3, float, float> sweep = (a, b, r) =>
        {
            var d = b - a; var len = d.magnitude;
            if (len < 1e-5f) return 0f;
            if (Physics.CheckSphere(a, r, ~0, QueryTriggerInteraction.Ignore)) return 0f;   // as FrontRoomsInspect.SweepWorld: a wall at the eye blocks
            return Physics.SphereCast(a, r, d / len, out var hit, len, ~0, QueryTriggerInteraction.Ignore) ? hit.distance : len;
        };
        Func<Vector3, float, bool> overlap = (p, r) => Physics.CheckSphere(p, r, ~0, QueryTriggerInteraction.Ignore);
        var rng = new System.Random(4242);
        foreach (var a in assets)
        {
            var face = new Pose(o + new Vector3(0f, a.y, .0045f), Quaternion.LookRotation(Vector3.forward, Vector3.up));
            int solved = 0, refused = 0, clipped = 0, offscreen = 0, occluded = 0, cannotStand = 0;
            for (var i = 0; i < 50; i++)
            {
                var dist = .5f + (float)rng.NextDouble() * 1.1f;
                var ang = -60f + 120f * (float)rng.NextDouble();
                var flat = Quaternion.AngleAxis(ang, Vector3.up) * Vector3.forward * dist;
                var eye = new Vector3(face.position.x + flat.x, o.y + ModuleUnits.PlayerEye, face.position.z + flat.z);
                // The body (capsule r 0.30 m) must be able to stand there.
                var feet = new Vector3(eye.x, o.y, eye.z);
                if (Physics.CheckCapsule(feet + Vector3.up * .45f, feet + Vector3.up * 1.45f, ModuleUnits.PlayerRadius, ~0, QueryTriggerInteraction.Ignore)) { cannotStand++; continue; }
                var input = new FrontRoomsInspectFraming.Input
                {
                    face = face, size = a.size, margin = .08f, screen = a.screenA ? new Rect(.01f, 0f, .63f, 1f) : default,
                    fov = 61f, pathFov = 76f, aspect = 16f / 9f, nearPlane = .06f, eye = eye, minDist = .18f, maxDist = 1f,
                    maxAngle = a.y > 1.9f ? 40f : 25f, maxYawOff = 25f, distMaxA = .44f,
                    minY = eye.y - I.MaxDrop, maxY = eye.y + I.MaxRise, dist = new[] { a.dist, 0f }, sweep = sweep, overlap = overlap,
                };
                var r = FrontRoomsInspectFraming.Solve(input);
                if (!r.ok) { refused++; continue; }
                solved++;
                var rc = FrontRoomsInspectFraming.NearCornerRadius(.06f, 76f, 16f / 9f);
                var bad = Physics.CheckSphere(r.pose.position, rc, ~0, QueryTriggerInteraction.Ignore);
                // The eased travel: sample 20 points on the camera's path from the eye.
                for (var k = 1; k <= 20 && !bad; k++)
                {
                    var p = Vector3.Lerp(eye, r.pose.position, FrontRoomsCameraRig.SineIn(k / 20f));
                    if (Physics.CheckSphere(p, rc, ~0, QueryTriggerInteraction.Ignore)) bad = true;
                }
                if (bad) clipped++;
                if (!CornersOnScreen(input, r, out _, out _)) offscreen++;
                var right = face.rotation * Vector3.right * (a.size.x * .5f);
                var up = face.rotation * Vector3.up * (a.size.y * .5f);
                for (var c = 0; c < 4; c++)
                {
                    var corner = face.position + ((c & 1) == 0 ? right : -right) + ((c & 2) == 0 ? up : -up) + face.rotation * Vector3.forward * .002f;
                    if (Physics.Linecast(r.pose.position, corner, ~0, QueryTriggerInteraction.Ignore)) { occluded++; break; }
                }
            }
            Check(clipped == 0 && offscreen == 0 && occluded == 0 && solved > 0,
                "no clipping, " + a.name + ": 50 approaches → " + cannotStand + " where the body cannot stand, " + solved + " poses, " + refused + " refused (no prompt), " + clipped + " clipped, " + offscreen + " off screen, " + occluded + " occluded");
        }
        UnityEngine.Object.DestroyImmediate(stage.gameObject);
    }

    // ------------------------------------------------------------------ fix stage 2026-10-14 (verifier issues)
    static void FixTests()
    {
        // Adapt (V1-3 / V2 blocker): the own lamp dominates; lit = no lift; own lamp off = a lift of at least 1 EV.
        Check(FrontRoomsInspectLook.AdaptFor(1f) == 0f && FrontRoomsInspectLook.AdaptFor(.45f) == 0f, "adapt: lit cell (W >= 0.45) gets no lift");
        var w4 = 0f + FrontRoomsInspectLook.NeighbourShare * 4f;
        var w3 = 0f + FrontRoomsInspectLook.NeighbourShare * 3f;
        var ev4 = FrontRoomsInspectLook.AdaptFor(w4) * FrontRoomsInspectLook.AdaptMaxEv;
        var ev3 = FrontRoomsInspectLook.AdaptFor(w3) * FrontRoomsInspectLook.AdaptMaxEv;
        var ev0 = FrontRoomsInspectLook.AdaptFor(0f) * FrontRoomsInspectLook.AdaptMaxEv;
        Check(ev4 >= 1f && ev3 >= 1.5f && Mathf.Abs(ev0 - FrontRoomsInspectLook.AdaptMaxEv) < 1e-4f,
            "adapt: own lamp off lifts " + F(ev4, "0.00") + " EV with 4 lit neighbours (>= 1), " + F(ev3, "0.00") + " EV with 3 (>= 1.5, the P2 case), " + F(ev0, "0.00") + " EV in a dead area");
        Check(FrontRoomsInspectLook.AdaptFor(w4) >= .9f, "adapt: own lamp off gives Adapt " + F(FrontRoomsInspectLook.AdaptFor(w4), "0.00") + " (>= 0.9, verifier 2's test) even with 4 lit neighbours");
        var lit1 = FrontRoomsInspectLook.AdaptFor(1f + FrontRoomsInspectLook.NeighbourShare * 0f);
        Check(lit1 == 0f, "adapt: own lamp lit and every neighbour dead still gets no lift (the lit legend stays faint, spec A3)");
        Note("adapt lift EV: own off + 4 lit " + F(ev4, "0.00") + ", + 3 lit " + F(ev3, "0.00") + ", + 2 lit " + F(FrontRoomsInspectLook.AdaptFor(.1f) * FrontRoomsInspectLook.AdaptMaxEv, "0.00") + ", dead " + F(ev0, "0.00"));

        // The legend region (V2 framing): sheet mm -> face metres, viewer axes. Tutorial sheet A (examine.json): legend
        // [296, 46, 118, 158] mm, found by its id (the regions are plan, legend, alarm, title block).
        var def = FrontRoomsKitLibrary.GetInfo("Kit_EvacPlacard")?.inspect;
        var hasRegion = false;
        var rc = Vector2.zero;
        var rs = Vector2.zero;
        var legendIndex = -1;
        if (def != null && def.regions != null)
            for (var ri = 0; ri < def.regions.Length; ri++)
                if (def.regions[ri] != null && def.regions[ri].id == "legend") legendIndex = ri;
        if (def != null) hasRegion = def.TryRegion(legendIndex, PlacardSize, out rc, out rs);
        Check(hasRegion && Mathf.Abs(rc.x - .139f) < .001f && Mathf.Abs(rc.y - .0145f) < .001f && Mathf.Abs(rs.y - .158f) < .001f,
            "placard legend region: centre " + F(rc.x * 1000f, "0.0") + " mm right, " + F(rc.y * 1000f, "0.0") + " mm up of the face centre, " + F(rs.x * 1000f, "0") + " x " + F(rs.y * 1000f, "0") + " mm (sheet 432 x 279)");
        Check(def != null && Mathf.Abs(def.TexelsPerMetre - 7330f) < 1f, "placard texel density from main's v3 sheet: " + (def != null ? F(def.TexelsPerMetre, "0") : "-") + " px/m (7,330 down)");
        // The zoomed legend: the sidecar's smallest cap (sheet A 2.8 mm) reads at 18.6 arcmin (0.215 m), the whole region
        // inside the screen above the hint band.
        {
            var zin = PlacardInput(16f / 9f, Vector3.zero);
            var zCap = def != null && def.SmallestCapMm > 0f ? def.SmallestCapMm : 2.4f;
            var z = FrontRoomsInspectFraming.ZoomFor(zin, zCap, FrontRoomsInspectFraming.DesktopArcmin, 1080f, 7330f, .36f);
            var visH = 2f * z.distance * Mathf.Tan(z.fov * .5f * Mathf.Deg2Rad);
            var centreY = .5f + .5f * FrontRoomsInspect.HintBand;            // the region's centre on screen (viewport)
            var top = centreY + rs.y / visH * .5f;
            var bottom = centreY - rs.y / visH * .5f;
            var hintTopPx = 72f + 16f;                                        // glyph bottom on the 72 px margin + cap
            Check(top <= 1f && bottom * 1080f >= hintTopPx + 40f - .5f,
                "zoomed legend (" + F(z.distance) + " m for the " + F(zCap, "0.0") + " mm cap): spans viewport y " + F(bottom, "0.000") + "-" + F(top, "0.000") + "; bottom " + F(bottom * 1080f - hintTopPx, "0") + " px above the hint (>= 40)");
        }

        // 8-corner fit and look limits (V2 LOW): the frame box stays on screen at every look extreme.
        {
            var input = PlacardInput(16f / 9f, Vector3.zero);
            var r = FrontRoomsInspectFraming.Solve(input);
            var ok = r.ok;
            var worstEdge = 1f;
            foreach (var yaw in new[] { r.lookMin.x, 0f, r.lookMax.x })
                foreach (var pitch in new[] { r.lookMin.y, 0f, r.lookMax.y })
                {
                    var pose = new Pose(r.pose.position, r.pose.rotation * Quaternion.Euler(-pitch, yaw, 0f));
                    var rr = r; rr.pose = pose;
                    if (!CornersOnScreen(input, rr, out var wst, out _)) ok = false;
                    worstEdge = Mathf.Min(worstEdge, wst);
                }
            Check(ok, "look limits keep all 8 frame-box corners on screen at every extreme (yaw " + F(r.lookMin.x, "0.00") + " to " + F(r.lookMax.x, "0.00") + " deg, pitch " + F(r.lookMin.y, "0.00") + " to " + F(r.lookMax.y, "0.00") + "; nearest edge " + F(worstEdge * 1920f, "0") + " px)");
        }

        // The return's onset (V2 LOW comfort): no speed step at the exit; ends on time.
        {
            var k = I.OutOnset / I.BlendOut;
            var first = 1f - FrontRoomsCameraRig.SineOutOnset(1f - Dt / I.BlendOut, k);
            var oldFirst = 1f - FrontRoomsCameraRig.SineOut(1f - Dt / I.BlendOut);
            var peak = 0f;
            for (var t = 0f; t < I.BlendOut - 1e-5f; t += Dt)
                peak = Mathf.Max(peak, FrontRoomsCameraRig.SineOutOnset(1f - t / I.BlendOut, k) - FrontRoomsCameraRig.SineOutOnset(1f - (t + Dt) / I.BlendOut, k));
            var oldPeak = 1f - FrontRoomsCameraRig.SineOut(1f - Dt / I.BlendOut);
            Check(FrontRoomsCameraRig.SineOutOnset(1f, k) == 1f && FrontRoomsCameraRig.SineOutOnset(0f, k) == 0f && first < .5f * oldFirst && peak <= oldPeak * 1.1f,
                "return onset: first frame moves " + F(first * 100f, "0.0") + " % of the way (v1 " + F(oldFirst * 100f, "0.0") + " %), peak step " + F(peak * 100f, "0.0") + " % (v1 " + F(oldPeak * 100f, "0.0") + " %)");
        }

        // Prompts (V2 UI): verb + object, allocation-free.
        Check(FrontRoomsInspectCatalog.PromptFor("Kit_EvacPlacard", "read") == FrontRoomsInspectCatalog.PromptReadPlan
              && FrontRoomsInspectCatalog.PromptFor("Kit_KeyTag_Round_Blue", "look") == FrontRoomsInspectCatalog.PromptExamineKey
              && FrontRoomsInspectCatalog.PromptFor("Kit_Unknown", "look") == FrontRoomsInspectCatalog.PromptLook,
            "prompts: \"" + FrontRoomsInspectCatalog.PromptReadPlan + "\", \"" + FrontRoomsInspectCatalog.PromptExamineKey + "\" (every zone tag), bare verb otherwise");
        // Zoom gate (V1-2): the game wires zoom input in Awake (C7, 交互设计 10-24); with no game (EditMode) the gate stays shut.
        Check(!FrontRoomsInspect.ZoomInputWired, "zoom gate shut without the game (C7 opens it in FrontRooms3DGame.Awake): no zoom step, no CLOSER hint");
        // Direction B (Red 10-14): every zone tag carries the object block.
        var tags = 0;
        foreach (var shape in new[] { "Rect", "Round", "Long" })
            foreach (var colour in new[] { "", "_Blue", "_White" })
            {
                var t = FrontRoomsKitLibrary.GetInfo("Kit_KeyTag_" + shape + colour)?.inspect;
                if (t != null && t.IsSet && t.Kind == InspectKind.Object && !t.ship) tags++;
            }
        Check(tags == 9, "direction B: all 9 zone tags (rect, round, long x red, blue, white) carry the object block, ship false (" + tags + "/9)");
    }
    // ------------------------------------------------------------------ transcript reframe (10-24: SetTranscript during a close-up)
    static InspectDef ReframeDef(float[] screen, float[] dist) => new InspectDef
    {
        kind = "flat", verb = "read", ship = true, size = new[] { PlacardSize.x, PlacardSize.y }, margin = .08f, screen = screen, dist = dist,
        distMaxA = .44f, minDist = .18f, maxDist = 1f, maxAngle = 25f, maxYawOff = 25f, fovDelta = -15f, reach = 1.6f, approachAngle = 60f,
        box = new InspectDef.Box { c = new[] { 0f, 0f, .00675f }, s = new[] { PlacardSize.x, PlacardSize.y, .0135f } },
    };

    /// <summary>A flat target d metres in front of the eye, 0.1 m below it, its face toward the eye.</summary>
    static FrontRoomsInspectable MakeFacing(Transform parent, string name, Pose eye, float d, InspectDef def)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var fwd = eye.rotation * Vector3.forward;
        fwd.y = 0f;
        fwd.Normalize();
        go.transform.SetPositionAndRotation(eye.position + fwd * d + Vector3.down * .1f, Quaternion.LookRotation(-fwd, Vector3.up));
        var t = go.AddComponent<FrontRoomsInspectable>();
        t.Configure(name, def, null);
        FrontRoomsInspect.Register(t);
        Physics.SyncTransforms();
        return t;
    }

    /// <summary>The viewport bounds of a target's face rect through the render camera as it stands (its own FOV and aspect).</summary>
    static Rect FaceBounds(FrontRoomsInspectable t, Camera cam)
    {
        var f = t.Face;
        var s = t.Size;
        var r = FrontRoomsInspectFraming.FaceRight(f) * (s.x * .5f);
        var u = FrontRoomsInspectFraming.FaceUp(f) * (s.y * .5f);
        var pose = new Pose(cam.transform.position, cam.transform.rotation);
        float x0 = 9f, x1 = -9f, y0 = 9f, y1 = -9f;
        for (var i = 0; i < 4; i++)
        {
            var v = FrontRoomsInspectFraming.Project(pose, cam.fieldOfView, cam.aspect, f.position + ((i & 1) == 0 ? r : -r) + ((i & 2) == 0 ? u : -u));
            x0 = Mathf.Min(x0, v.x); x1 = Mathf.Max(x1, v.x); y0 = Mathf.Min(y0, v.y); y1 = Mathf.Max(y1, v.y);
        }
        return Rect.MinMaxRect(x0, y0, x1, y1);
    }

    static string Vp(Rect r) => "x " + F(r.xMin) + "–" + F(r.xMax) + ", y " + F(r.yMin) + "–" + F(r.yMax);

    /// <summary>One game frame of a close-up: FrontRoomsInspect.Tick, then the rig (as FrontRooms3DGame does in Phase.Inspect).</summary>
    static void InspectFrame(FrontRoomsCameraRig rig)
    {
        FrontRoomsInspect.Tick(Dt, Vector2.zero, 0f, false);
        rig.Tick(Dt);
    }

    static bool Arrive(FrontRoomsCameraRig rig, ShotHandle h)
    {
        for (var i = 0; i < 120 && h != null && h.Weight < 1f; i++) InspectFrame(rig);
        InspectFrame(rig);
        return h != null && h.Weight >= 1f;
    }

    /// <summary>Frames until the reframe ends: seconds taken, the largest per-frame camera move (m) and the largest change of
    /// that move between two frames (m, a vector: the speed step). Appends each frame's move to <paramref name="steps"/>.</summary>
    static float RunReframe(FrontRoomsCameraRig rig, Camera cam, out float maxStep, out float maxJerk, List<Vector3> steps = null, int max = 90)
    {
        var last = cam.transform.position;
        var lastStep = steps != null && steps.Count > 0 ? steps[steps.Count - 1] : (Vector3?)null;
        maxStep = maxJerk = 0f;
        var t = 0f;
        for (var i = 0; i < max && FrontRoomsInspect.Reframing; i++)
        {
            InspectFrame(rig);
            t += Dt;
            var p = cam.transform.position;
            var step = p - last;
            maxStep = Mathf.Max(maxStep, step.magnitude);
            if (lastStep.HasValue) maxJerk = Mathf.Max(maxJerk, (step - lastStep.Value).magnitude);
            steps?.Add(step);
            lastStep = step;
            last = p;
        }
        InspectFrame(rig);
        return t;
    }

    static float ReframeSeconds(float travel) => Mathf.Clamp(I.TravelBase + I.TravelPerMetre * travel, I.TravelMin, I.TravelMax);

    static void ReframeTests(Transform root)
    {
        FrontRoomsInspect.Abort();
        FrontRoomsHandheld.ClearForce();
        var stage = new GameObject("reframe stage").transform;
        stage.SetParent(root, false);
        stage.position = root.position + new Vector3(40f, 0f, 40f);
        var (rig, body, cam) = MakeRig(stage);
        body.position = stage.position;
        cam.aspect = 16f / 9f;
        var eye = rig.BaseEye;

        // ---- desktop, composition B (a flat target with no screen rect: the test bed's door sign kind)
        var b = MakeFacing(stage, "reframe B", eye, 1.2f, ReframeDef(null, new[] { .3f, 0f }));
        var h = FrontRoomsInspect.Open(b, rig);
        Check(h != null && Arrive(rig, h), "reframe B: the close-up opens and arrives (" + (h == null ? FrontRoomsInspect.LastSolveFor(b).why : "") + ")");
        if (h == null) return;
        var open = FrontRoomsInspect.LastSolve;
        var p0 = cam.transform.position;
        var r0 = cam.transform.rotation;
        var b0 = FaceBounds(b, cam);
        Check(!FrontRoomsInspect.Reframing && (p0 - open.pose.position).sqrMagnitude < 1e-10f && Mathf.Abs(b0.center.x - .5f) < .01f,
            "desktop, no toggle: the camera sits exactly on the solved pose (B, centred: " + Vp(b0) + "), nothing eases");
        FrontRoomsInspect.SetTranscript(true);
        var free = FrontRoomsInspect.FreeScreenRect;
        var to = FrontRoomsInspect.LastSolve;
        var travel = (to.pose.position - p0).magnitude;
        var want = ReframeSeconds(travel);
        var stepsIn = new List<Vector3>();
        var tIn = RunReframe(rig, cam, out var peakIn, out var jerkIn, stepsIn);
        var b1 = FaceBounds(b, cam);
        var avgIn = travel / want * Dt;
        Check(FrontRoomsInspect.ShowTranscript && free.xMax <= .64f + 1e-5f && b1.xMin >= free.xMin - 1e-3f && b1.xMax <= free.xMax + 1e-3f && Mathf.Abs(b1.center.x - free.center.x) < .01f,
            "TEXT on mid close-up: the subject moves into the free rect (x " + F(free.xMin) + "–" + F(free.xMax) + "): " + Vp(b0) + " → " + Vp(b1));
        Check(Mathf.Abs(tIn - want) <= Dt + 1e-4f && (cam.transform.position - to.pose.position).magnitude < 1e-5f,
            "…in " + F(tIn) + " s (the open's rule: 0.30 + 0.12 × " + F(travel) + " m, 0.40–0.55 = " + F(want) + " s), onto the new solved pose");
        var firstIn = stepsIn.Count > 0 ? stepsIn[0].magnitude : 0f;
        var lastIn = stepsIn.Count > 0 ? stepsIn[stepsIn.Count - 1].magnitude : 0f;
        Check(Mathf.Abs(peakIn / avgIn - Mathf.PI / 2f) < .08f && stepsIn.Count > 2 && firstIn < .2f * peakIn && lastIn < .2f * peakIn,
            "…on the open's curve (sine in-out): peak step " + F(peakIn * 1000f, "0.00") + " mm = " + F(peakIn / avgIn, "0.00") + " × the average (1.57); first " + F(firstIn * 1000f, "0.00") + " mm, last " + F(lastIn * 1000f, "0.00") + " mm");
        FrontRoomsInspect.SetTranscript(false);
        var tOut = RunReframe(rig, cam, out var peakOut, out _);
        var b2 = FaceBounds(b, cam);
        Check((cam.transform.position - p0).sqrMagnitude == 0f && cam.transform.rotation.Equals(r0) && FrontRoomsInspect.LastSolve.pose.position.Equals(open.pose.position) && Mathf.Abs(tOut - want) <= Dt + 1e-4f,
            "TEXT off: back to the open's own pose, bit for bit, in " + F(tOut) + " s (" + Vp(b2) + ")");
        // A double toggle mid-ease: no pose jump and no speed step (the running ease goes on under the new one).
        FrontRoomsInspect.SetTranscript(true);
        var lastP = cam.transform.position;
        var steps = new List<Vector3>();
        for (var i = 0; i < 12; i++) { InspectFrame(rig); steps.Add(cam.transform.position - lastP); lastP = cam.transform.position; }
        var midFrac = FrontRoomsCameraRig.SineIn(12f * Dt / want);
        var atToggle = steps[steps.Count - 1];
        FrontRoomsInspect.SetTranscript(false);
        var tBack = RunReframe(rig, cam, out _, out _, steps);
        var jerkAll = 0f;
        var stepMax = 0f;
        for (var i = 0; i < steps.Count; i++)
        {
            stepMax = Mathf.Max(stepMax, steps[i].magnitude);
            if (i > 0) jerkAll = Mathf.Max(jerkAll, (steps[i] - steps[i - 1]).magnitude);
        }
        var afterToggle = steps[12];
        Note("double toggle: the frame before the second toggle moved " + F(atToggle.magnitude * 1000f, "0.00") + " mm, the frame after " + F(afterToggle.magnitude * 1000f, "0.00") + " mm (same direction: " + F(Vector3.Dot(atToggle.normalized, afterToggle.normalized), "0.000") + "); a frozen start would drop it to ~0 at once");
        Check(stepMax <= peakIn * 1.02f && jerkAll <= jerkIn * 2f + 1e-6f && (cam.transform.position - p0).sqrMagnitude == 0f,
            "double toggle at 0.20 s (" + F(midFrac * 100f, "0") + " % of the way): largest frame step " + F(stepMax * 1000f, "0.00") + " mm (single toggle " + F(peakIn * 1000f, "0.00") + "), largest speed step " + F(jerkAll * 1000f, "0.000") + " mm/frame (single " + F(jerkIn * 1000f, "0.000") + "); back on the open's pose after " + F(tBack) + " s");
        Note("reframe B (desktop 16:9, d " + F(open.distance) + " → " + F(to.distance) + " m): travel " + F(travel * 1000f, "0") + " mm in " + F(want) + " s; frame " + Vp(b0) + " → " + Vp(b1));
        // Closing mid-ease holds the base pose and returns from where the camera is.
        FrontRoomsInspect.SetTranscript(true);
        for (var i = 0; i < 6; i++) InspectFrame(rig);
        var atClose = cam.transform.position;
        var home = rig.BaseEye.position;
        FrontRoomsInspect.Close(rig);
        // The base pose holds while the close-up returns: the camera runs the straight line from where it was to the eye.
        var offLine = 0f;
        for (var i = 0; i < 60 && !h.Ended; i++)
        {
            rig.Tick(Dt);
            var q = cam.transform.position - atClose;
            var line = (home - atClose).normalized;
            offLine = Mathf.Max(offLine, (q - line * Vector3.Dot(q, line)).magnitude);
        }
        // The stage stands ~2 km from the origin, where a float position steps by 0.24–0.49 mm: 1 mm is a few steps.
        Check(h.Ended && offLine < 1e-3f && (cam.transform.position - home).magnitude < 1e-4f,
            "a close mid-reframe returns from where the camera is, on the straight line home (largest step off it " + F(offLine * 1000f, "0.000") + " mm, float steps at the stage), and ends home");
        FrontRoomsInspect.Abort();
        UnityEngine.Object.DestroyImmediate(b.gameObject);

        // ---- desktop, the placard (composition A already keeps the left 64 %): the transcript changes nothing visible
        var placardDef = FrontRoomsKitLibrary.GetInfo("Kit_EvacPlacard")?.inspect;
        if (placardDef != null)
        {
            var pl = MakeFacing(stage, "reframe placard", eye, 1.5f, placardDef);
            h = FrontRoomsInspect.Open(pl, rig);
            Check(h != null && Arrive(rig, h), "desktop placard: opens (composition " + (FrontRoomsInspect.LastSolve.compositionB ? "B" : "A") + ")");
            if (h != null)
            {
                var pp = cam.transform.position;
                var pb = FaceBounds(pl, cam);
                FrontRoomsInspect.SetTranscript(true);
                RunReframe(rig, cam, out _, out _);
                var moved = (cam.transform.position - pp).magnitude;
                Check(!FrontRoomsInspect.LastSolve.compositionB && moved < 1e-4f && FaceBounds(pl, cam).xMax <= .64f + 1e-3f,
                    "desktop placard, TEXT on: composition A already sits in the free rect (" + Vp(pb) + "); the camera moves " + F(moved * 1000f, "0.000") + " mm");
                FrontRoomsInspect.SetTranscript(false);
                RunReframe(rig, cam, out _, out _);
            }
            FrontRoomsInspect.Abort();
            UnityEngine.Object.DestroyImmediate(pl.gameObject);
        }
        else Check(false, "the placard's inspect def loads (Kit_EvacPlacard)");

        // ---- the phone (iPhone 16 Pro, 874 x 402 pt, safe L 62 R 62 B 21): the free rect is the panel's own edge
        FrontRoomsHandheld.Force(FrontRoomsHandheld.IPhonePro);
        try
        {
            var frame = FrontRoomsHandheld.ScreenPoints;
            var layout = new FrontRoomsTouchLayout(frame, FrontRoomsHandheld.Shape.BleedSafe, FrontRoomsHandheld.DeviceControlsScale, false, FrontRoomsHandheld.IsTablet, 0f);
            var panel = layout.TranscriptPanel;
            FrontRoomsInspect.SetTranscript(true);   // outside a close-up: the flag only
            var pf = FrontRoomsInspect.FreeScreenRect;
            FrontRoomsInspect.SetTranscript(false);
            Check(Mathf.Abs(panel.xMin - 542f) < .5f && Mathf.Abs(panel.xMax - 812f) < .5f && Mathf.Abs(frame.y - panel.yMax - 64f) < .5f && Mathf.Abs(frame.y - panel.yMin - 280f) < .5f
                  && Mathf.Abs(pf.xMax * frame.x - panel.xMin) < .05f && Mathf.Abs(pf.xMin * frame.x - 62f) < .05f,
                "phone: the panel is x " + F(panel.xMin, "0.0") + "–" + F(panel.xMax, "0.0") + ", y " + F(frame.y - panel.yMax, "0.0") + "–" + F(frame.y - panel.yMin, "0.0") + " pt from the top; the free rect ends at x " + F(pf.xMax * frame.x, "0.00") + " pt (its left edge)");
            cam.aspect = 2622f / 1206f;
            // B on the phone: centred in the safe area, then left of the panel.
            var pb = MakeFacing(stage, "reframe B phone", eye, 1.2f, ReframeDef(null, new[] { .3f, 0f }));
            h = FrontRoomsInspect.Open(pb, rig);
            if (h != null && Arrive(rig, h))
            {
                var bb0 = FaceBounds(pb, cam);
                FrontRoomsInspect.SetTranscript(true);
                RunReframe(rig, cam, out _, out _);
                var bb1 = FaceBounds(pb, cam);
                Check(bb1.xMax * frame.x <= panel.xMin + .5f && bb1.xMin * frame.x >= 62f - .5f && (1f - bb1.yMax) * frame.y >= -.5f && (1f - bb1.yMin) * frame.y <= frame.y - 21f + .5f,
                    "phone B, TEXT on: the subject moves left of the panel: x " + F(bb0.xMin * frame.x, "0") + "–" + F(bb0.xMax * frame.x, "0") + " → " + F(bb1.xMin * frame.x, "0") + "–" + F(bb1.xMax * frame.x, "0") + " pt (panel from " + F(panel.xMin, "0") + ")");
                FrontRoomsInspect.SetTranscript(false);
                RunReframe(rig, cam, out _, out _);
                Check((FaceBounds(pb, cam).center - bb0.center).magnitude < 1e-4f, "phone B, TEXT off: centred again");
            }
            else Check(false, "phone B opens (" + FrontRoomsInspect.LastSolveFor(pb).why + ")");
            FrontRoomsInspect.Abort();
            UnityEngine.Object.DestroyImmediate(pb.gameObject);
            // The placard on the phone (composition A, the left 64 % of the screen) with a door pier 0.30 m proud at its left.
            if (placardDef != null)
            {
                var pl = MakeFacing(stage, "reframe placard phone", eye, 1.5f, placardDef);
                var plFace = pl.Face;
                var noPier = FrontRoomsInspect.CanOpen(pl, rig) ? FrontRoomsInspect.LastSolveFor(pl) : default;
                var pier = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pier.name = "door pier";
                pier.transform.SetParent(stage, false);
                var viewerLeft = -(eye.rotation * Vector3.right);
                pier.transform.position = plFace.position + viewerLeft * (PlacardSize.x * .5f + .33f + .1f) + (plFace.rotation * Vector3.forward) * .15f;
                pier.transform.rotation = plFace.rotation;
                pier.transform.localScale = new Vector3(.2f, 2.4f, .3f);
                Physics.SyncTransforms();
                FrontRoomsInspect.Abort();   // drops the solve cache: the pier counts
                h = FrontRoomsInspect.Open(pl, rig);
                if (h != null && Arrive(rig, h))
                {
                    var s0 = FrontRoomsInspect.LastSolve;
                    var off = (cam.transform.position - s0.pose.position).magnitude;
                    var a0 = FaceBounds(pl, cam);
                    Check(noPier.ok && !s0.compositionB && Mathf.Abs(s0.distance - noPier.distance) < 1e-4f && off < 1e-4f,
                        "phone placard by a door pier: composition A, d " + F(s0.distance) + " m (" + F(noPier.distance) + " without the pier); the camera " + F(off * 1000f, "0.000") + " mm off the solved pose (no clamp)");
                    FrontRoomsInspect.SetTranscript(true);
                    var tp = RunReframe(rig, cam, out _, out _);
                    var a1 = FaceBounds(pl, cam);
                    Check(a1.xMax * frame.x <= panel.xMin + .5f && a1.xMin * frame.x >= 62f - .5f && (1f - a1.yMax) * frame.y >= -.5f && (1f - a1.yMin) * frame.y <= frame.y - 21f + .5f,
                        "phone placard, TEXT on: x " + F(a0.xMin * frame.x, "0.0") + "–" + F(a0.xMax * frame.x, "0.0") + " → " + F(a1.xMin * frame.x, "0.0") + "–" + F(a1.xMax * frame.x, "0.0") + " pt, y " + F((1f - a1.yMax) * frame.y, "0.0") + "–" + F((1f - a1.yMin) * frame.y, "0.0") + " pt from the top: left of the panel (x 542–812, y 64–280), inside the safe area (" + F(tp) + " s)");
                    Note("phone placard (iPhone 16 Pro, aspect 2.174): composition A, d " + F(s0.distance) + " → " + F(FrontRoomsInspect.LastSolve.distance) + " m; sheet x " + F(a0.xMin * frame.x, "0.0") + "–" + F(a0.xMax * frame.x, "0.0") + " → " + F(a1.xMin * frame.x, "0.0") + "–" + F(a1.xMax * frame.x, "0.0") + " pt");
                }
                else Check(false, "phone placard opens (" + FrontRoomsInspect.LastSolveFor(pl).why + ")");
                FrontRoomsInspect.Abort();
                UnityEngine.Object.DestroyImmediate(pier);
                // Views squarer than 16:9 draw a wider FOV (FrontRoomsHandheld.AspectFov, 10-24): the solve frames for it.
                foreach (var profile in new[] { FrontRoomsHandheld.IPad11, FrontRoomsHandheld.IPhoneDuoInner })
                {
                    FrontRoomsHandheld.Force(profile);
                    cam.aspect = profile.Pixels.x / (float)profile.Pixels.y;
                    h = FrontRoomsInspect.Open(pl, rig);
                    if (h != null && Arrive(rig, h))
                    {
                        var s1 = FrontRoomsInspect.LastSolve;
                        var drawn = FrontRoomsHandheld.AspectFov(rig.BaseFov + placardDef.FovDelta, cam.aspect);
                        var pr = FaceBounds(pl, cam);
                        // The face centre lands where the solve put it: A's rect centre (x 0.325), B's free-rect centre (x 0.5).
                        var c = FrontRoomsInspectFraming.Project(new Pose(cam.transform.position, cam.transform.rotation), cam.fieldOfView, cam.aspect, pl.Face.position);
                        var wantX = s1.compositionB ? .5f : .325f;
                        Check(Mathf.Abs(cam.fieldOfView - drawn) < .01f && Mathf.Abs(c.x - wantX) < .005f && pr.xMin >= -1e-3f && pr.xMax <= 1.001f,
                            profile.Name + " (aspect " + F(cam.aspect, "0.00") + "): drawn FOV " + F(cam.fieldOfView, "0.0") + " (authored " + F(rig.BaseFov + placardDef.FovDelta, "0.0") + "), composition " + (s1.compositionB ? "B" : "A") + ", d " + F(s1.distance) + "; the face centre lands at x " + F(c.x) + " (solved " + F(wantX) + "), frame " + Vp(pr));
                    }
                    else Check(false, profile.Name + ": the placard opens (" + FrontRoomsInspect.LastSolveFor(pl).why + ")");
                    FrontRoomsInspect.Abort();
                }
                // The zoom step on the iPad (zoom input wired, as after C7): its FOV is solved as drawn, handed to the rig authored.
                {
                    var wired = FrontRoomsInspect.ZoomInputWired;
                    FrontRoomsInspect.ZoomInputWired = true;
                    FrontRoomsHandheld.Force(FrontRoomsHandheld.IPad11);
                    cam.aspect = FrontRoomsHandheld.IPad11.Pixels.x / (float)FrontRoomsHandheld.IPad11.Pixels.y;
                    h = FrontRoomsInspect.Open(pl, rig);
                    if (h != null && Arrive(rig, h) && FrontRoomsInspect.ZoomStep.offered)
                    {
                        var z = FrontRoomsInspect.ZoomStep;
                        FrontRoomsInspect.SetZoomTarget(true);
                        for (var i = 0; i < 90; i++) InspectFrame(rig);
                        var wantFov = 2f * Mathf.Atan(z.visibleHeight * .5f / z.distance) * Mathf.Rad2Deg;
                        var dist = Vector3.Dot(cam.transform.position - pl.Face.position, pl.Face.rotation * Vector3.forward);
                        Check(Mathf.Abs(cam.fieldOfView - wantFov) < .05f && Mathf.Abs(dist - z.distance) < .002f,
                            "iPad zoom step: drawn FOV " + F(cam.fieldOfView, "0.00") + " (wanted " + F(wantFov, "0.00") + " for " + F(z.visibleHeight * 1000f, "0") + " mm visible at " + F(z.distance) + " m; authored " + F(z.fov, "0.00") + "), camera " + F(dist) + " m off the face");
                    }
                    else Check(false, "iPad zoom: the placard opens with a zoom step (" + FrontRoomsInspect.LastSolveFor(pl).why + ", offered " + FrontRoomsInspect.ZoomStep.offered + ")");
                    FrontRoomsInspect.Abort();
                    FrontRoomsInspect.ZoomInputWired = wired;
                }
                UnityEngine.Object.DestroyImmediate(pl.gameObject);
            }
        }
        finally
        {
            FrontRoomsInspect.Abort();
            FrontRoomsHandheld.ClearForce();
            UnityEngine.Object.DestroyImmediate(stage.gameObject);
        }
    }

    // ------------------------------------------------------------------ A43: only careful reading pauses (2026-10-30)
    // research/inspect/NOTE_1030_a43_pause_sort.md: every close-up target sorted into READING (pauses) or LOOKING (live), the
    // read-outs the map's A43 contract reads (CurrentPauses, WantsEnd), and C36's per-target threat gate (noThreatGate).
    static void A43Tests(Transform root)
    {
        FrontRoomsInspect.Abort();
        FrontRoomsKitLibrary.ClearCache();
        // ---- the sort, target by target (the real blocks: sidecars, catalog fallbacks, the code-built targets)
        var placard = FrontRoomsKitLibrary.GetInfo("Kit_EvacPlacard")?.inspect;
        Check(placard != null && placard.Pauses, "A43 sort: the evacuation plan (Kit_EvacPlacard, read, transcript " + (placard != null ? placard.transcript : "-") + ") pauses");
        if (placard != null)
        {
            var station = placard.Clone();
            station.transcript = FrontRoomsInspectCatalog.StationPlacardTranscript(true);
            var stationPhase0 = placard.Clone();
            stationPhase0.transcript = FrontRoomsInspectCatalog.StationPlacardTranscript(false);
            Check(station.Pauses && stationPhase0.Pauses, "A43 sort: the station placard's two sheets (FrontRoomsPlacardMount: placard_station_room, _phase0) pause");
        }
        var sign = FrontRoomsKitLibrary.GetInfo("Kit_DoorSign")?.inspect;
        Check(sign != null && sign.Verb == "read" && string.IsNullOrEmpty(sign.transcript) && !sign.Pauses, "A43 sort: the door sign (Kit_DoorSign, read, no transcript: read at a glance) runs live");
        var tags = new[] { "Kit_KeyTag_Rect", "Kit_KeyTag_Rect_Blue", "Kit_KeyTag_Rect_White", "Kit_KeyTag_Round", "Kit_KeyTag_Round_Blue", "Kit_KeyTag_Round_White", "Kit_KeyTag_Long", "Kit_KeyTag_Long_Blue", "Kit_KeyTag_Long_White" };
        var tagsLive = 0;
        foreach (var k in tags)
        {
            var d = FrontRoomsKitLibrary.GetInfo(k)?.inspect;
            if (d != null && d.IsSet && d.Verb == "look" && !d.Pauses && d.ThreatGated) tagsLive++;
        }
        Check(tagsLive == tags.Length, "A43 sort: the keys (EXAMINE KEY, the 9 Kit_KeyTag_* blocks; OpenHeld's first key reads the same block) run live with the threat gate: " + tagsLive + " of " + tags.Length);
        var fallbacks = new[] { "Kit_WallClock", "Kit_VendingMachine", "Kit_Copier", "Kit_DeskPhone" };
        var fallbacksLive = 0;
        foreach (var k in fallbacks)
        {
            var d = FrontRoomsInspectCatalog.Fallback(k);
            if (d != null && d.Verb == "look" && !d.Pauses && d.ThreatGated) fallbacksLive++;
        }
        Check(fallbacksLive == fallbacks.Length, "A43 sort: the wall clock, vending machine, copier and desk phone (catalog fallbacks, look) run live with the threat gate: " + fallbacksLive + " of " + fallbacks.Length);
        var tv = FrontRoomsScreenVideo.StationTvDef();
        Check(tv.Verb == "read" && !tv.Pauses && tv.ThreatGated, "A43 sort: the station TV ad (R3, read, no transcript: watched, not read) runs live with the threat gate");
        var terminal = FrontRoomsTerminalScreen.ReadDef();
        var terminalPhone = FrontRoomsTerminalScreen.ReadDef();
        terminalPhone.transcript = null;   // a phone reads it with no transcript (RefreshRead)
        Check(terminal.Pauses && terminalPhone.Pauses && terminal.pause == InspectDef.PauseAlways, "A43 sort: an office terminal's pages pause (explicit \"pause\": desktop, and a phone with no transcript)");
        // The station paper (not registered by any kit yet): the transcripts as they stand today.
        var dar = new InspectDef { kind = "flat", transcript = "station_dar" };
        var plate = new InspectDef { kind = "flat", transcript = "station_key_plate" };
        var signOut = new InspectDef { kind = "flat", transcript = "station_sign_out" };
        var signOutPinned = new InspectDef { kind = "flat", transcript = "station_sign_out", pause = InspectDef.PauseAlways };
        Check(dar.Pauses && plate.Pauses && !signOut.Pauses && signOutPinned.Pauses,
            "A43 sort, station paper: the DAR (6 lines) and the key plate (2 lines) pause; the sign-out sheet and register carry a 1-line header today, so they register with pause = \"pause\" (NOTE)");
        // ---- the rule itself
        var heads = new[] { "\u0002PLAN", "ONE LINE", "\u0002LEGEND" };
        Check(InspectDef.PrintedLines(heads) == 1 && InspectDef.PrintedLines(null) == 0 && InspectDef.PrintedLines(FrontRoomsInspectCatalog.Transcript("placard_q16")) >= 15,
            "A43 rule: region heads are not printed lines (1 of 3), none = 0, the plan >= 15");
        var lookPinned = new InspectDef { kind = "object", verb = "look", pause = "PAUSE" };
        var readLive = placard != null ? placard.Clone() : new InspectDef { kind = "flat", transcript = "placard_q16" };
        readLive.pause = InspectDef.PauseNever;
        var flatNoVerb = new InspectDef { kind = "flat" };
        var panel = new InspectDef { kind = "panel", transcript = "placard_q16" };
        Check(lookPinned.Pauses && !readLive.Pauses && !flatNoVerb.Pauses && !panel.Pauses,
            "A43 rule: the override wins both ways (case-insensitive); a flat with no verb reads but has no transcript (live); a panel looks (live) whatever its transcript");
        Check(new InspectDef().ThreatGated && !new InspectDef { noThreatGate = true }.ThreatGated, "C36: the threat gate is on by default and off only with noThreatGate");
        // ---- the read-outs on a real close-up (edit mode: timeScale 1, the world 'runs')
        var stage = new GameObject("a43 stage").transform;
        stage.SetParent(root, false);
        stage.position = root.position + new Vector3(-40f, 0f, 40f);
        var (rig, body, cam) = MakeRig(stage);
        body.position = stage.position;
        cam.aspect = 16f / 9f;
        var eye = rig.BaseEye;
        try
        {
            Check(FrontRoomsInspect.CurrentPauses && FrontRoomsInspect.CurrentThreatGated && !FrontRoomsInspect.WorldRuns && !FrontRoomsInspect.WantsEnd && FrontRoomsInspect.PausesWorld(null),
                "A43 read-outs with no close-up: CurrentPauses (the safe answer), gated, the world does not 'run', no end asked; PausesWorld(null) true");
            var readDef = ReframeDef(null, new[] { .3f, 0f });
            readDef.transcript = "placard_q16";
            var reading = MakeFacing(stage, "a43 reading", eye, 1.2f, readDef);
            var h = FrontRoomsInspect.Open(reading, rig);
            Check(h != null && FrontRoomsInspect.PausesWorld(reading) && FrontRoomsInspect.CurrentPauses, "A43: a reading close-up (read + the plan's transcript) reports CurrentPauses");
            FrontRoomsInspect.Abort();
            UnityEngine.Object.DestroyImmediate(reading.gameObject);
            var lookDef = ReframeDef(null, new[] { .3f, 0f });
            lookDef.verb = "look";
            var looking = MakeFacing(stage, "a43 looking", eye, 1.2f, lookDef);
            h = FrontRoomsInspect.Open(looking, rig);
            var arrived = h != null && Arrive(rig, h);
            Check(arrived && !FrontRoomsInspect.PausesWorld(looking) && !FrontRoomsInspect.CurrentPauses && FrontRoomsInspect.WorldRuns && !FrontRoomsInspect.WantsEnd,
                "A43: a looking close-up opens and arrives at timeScale 1 (CurrentPauses false, WorldRuns, no end asked)");
            var ageBefore = FrontRoomsInspect.Age;
            var rotBefore = cam.transform.rotation;
            for (var i = 0; i < 30; i++) { FrontRoomsInspect.Tick(Dt, new Vector2(.5f, .2f), 0f, false); rig.Tick(Dt); }
            var turned = Quaternion.Angle(rotBefore, cam.transform.rotation);
            Check(FrontRoomsInspect.Age > ageBefore + .45f && turned > .05f, "A43: look and the inspect clock run in a live close-up (age +" + F(FrontRoomsInspect.Age - ageBefore, "0.00") + " s, the view turned " + F(turned, "0.00") + " deg)");
            rig.CancelForRelay();
            Check(h.Cancelled && FrontRoomsInspect.WantsEnd, "A43: the rig's CancelForRelay lets a gated close-up go and WantsEnd asks the game to end it");
            FrontRoomsInspect.Close(rig);
            Check(!FrontRoomsInspect.WantsEnd && FrontRoomsInspect.Closing, "A43: Close answers WantsEnd (the return runs, no end asked)");
            for (var i = 0; i < 60 && FrontRoomsInspect.Active; i++) InspectFrame(rig);
            FrontRoomsInspect.Abort();
            // RequestEnd (the door peek's own end when its leaf moves)
            h = FrontRoomsInspect.Open(looking, rig);
            Arrive(rig, h);
            FrontRoomsInspect.RequestEnd();
            var asked = FrontRoomsInspect.WantsEnd;
            FrontRoomsInspect.Abort();
            Check(asked && !FrontRoomsInspect.WantsEnd, "A43: RequestEnd raises WantsEnd; Abort clears it");
            // A shot taken away without Close (CancelShot at 0, as a Camera motion Off cancel ends it in one tick)
            h = FrontRoomsInspect.Open(looking, rig);
            Arrive(rig, h);
            rig.CancelShot(h, 0f);
            rig.Tick(Dt);
            Check(h.Ended && FrontRoomsInspect.WantsEnd, "A43: a shot ended from outside asks the game to end the close-up (WantsEnd)");
            FrontRoomsInspect.Abort();
            UnityEngine.Object.DestroyImmediate(looking.gameObject);
            // C36: no threat gate (the door peek): the rig never lets it go
            var peekDef = ReframeDef(null, new[] { .3f, 0f });
            peekDef.verb = "look";
            peekDef.noThreatGate = true;
            var peek = MakeFacing(stage, "a43 ungated", eye, 1.2f, peekDef);
            h = FrontRoomsInspect.Open(peek, rig);
            Arrive(rig, h);
            rig.CancelForRelay();
            for (var i = 0; i < 5; i++) InspectFrame(rig);
            Check(h != null && !h.Cancelled && !h.Ended && !FrontRoomsInspect.CurrentThreatGated && !FrontRoomsInspect.ThreatGated(peek) && !FrontRoomsInspect.WantsEnd && FrontRoomsInspect.Active,
                "C36: a close-up without the threat gate stays open through CancelForRelay (shot not cancelled, no end asked)");
            FrontRoomsInspect.Abort();
            UnityEngine.Object.DestroyImmediate(peek.gameObject);
        }
        finally
        {
            FrontRoomsInspect.Abort();
            UnityEngine.Object.DestroyImmediate(stage.gameObject);
        }
    }
}
