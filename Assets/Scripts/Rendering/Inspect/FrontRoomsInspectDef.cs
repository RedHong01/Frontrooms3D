using System;
using UnityEngine;

/// <summary>EXAMINE: what kind of close-up a target gets (research/inspect/10_design.md §3.1).</summary>
public enum InspectKind { Flat, Panel, Object }

/// <summary>The Foley family of a target (the sound chat's InspectIn / InspectOut / KeyTurn events, §7.3 S3).</summary>
public enum InspectSound { Paper, Plastic, Metal, Keys }

/// <summary>
/// The sidecar "inspect" block (10_design.md §3.8), written by kitlib.py Kit.inspect() into
/// Resources/Props/Models/&lt;kit&gt;.json and read by FrontRoomsKitLibrary.Info.inspect.
/// Positions are Unity part-local (converted by the sidecar writer like colliders). JsonUtility
/// leaves absent numbers at 0, so every "0 = default" number is resolved by the getters below.
/// An empty kind means not inspectable: a kit opts in on purpose.
/// </summary>
[Serializable]
public class InspectDef
{
    [Serializable]
    public class Box { public float[] c; public float[] s; }

    [Serializable]
    public class Region
    {
        public string id;
        /// <summary>Sheet millimetres (x, y, w, h) from the sheet's top-left, as 平面视觉 exports them. All 0 = the whole face.</summary>
        public float[] rect;
        /// <summary>The smallest cap height on the region, mm (drives the angular zoom, §3.4).</summary>
        public float capMm;
    }

    public string kind;
    public string verb;
    public string face, faceDir, up;
    public float[] size;
    public float margin;
    public float[] screen;
    public float[] dist;
    public float distMaxA;
    public float minDist, maxDist;
    public float maxAngle, maxYawOff, fovDelta;
    public float reach, approachAngle;
    public float orbit;
    public Box box;
    public float aimPad;
    public float texelsPerMetre;
    /// <summary>The printed sheet (w, h, mm), centred on the face anchor: the frame of reference of the region rects. Absent = the framed size.</summary>
    public float[] sheet;
    public Region[] regions;
    public bool dof;
    /// <summary>
    /// Full bleed (the phone terminal, A39 / computer_ui 41, 2026-10-30): the framing ignores the safe area and the transcript's
    /// free rect, so the face can fill the screen's full height, centred. False (every sidecar, the default) = as before.
    /// </summary>
    public bool bleed;
    public bool ship;
    public string transcript;
    public string sound;
    /// <summary>
    /// A43 (Red 2026-10-30, 系统设计 chat): only careful reading pauses the world. Empty (every sidecar so far) = derived, see
    /// <see cref="Pauses"/>; <see cref="PauseAlways"/> or <see cref="PauseNever"/> overrides it (the office terminal is "pause").
    /// Sort and reasons: research/inspect/NOTE_1030_a43_pause_sort.md.
    /// </summary>
    public string pause;
    /// <summary>
    /// C36 (Red 2026-10-30): true = no threat gate. A target with the gate (false, the default: every target today) shows no
    /// prompt while a threat is perceivable (the game's CanShowInspect) and, in a live close-up, a perceivable threat ends it
    /// (FrontRoomsInspect.WantsEnd; its shot is also cancelled by the rig's CancelForRelay). True: the shot never cancels on the
    /// Relay and the close-up never ends on a threat; the player decides when to back off (the door peek, wired by the
    /// door-vision package).
    /// </summary>
    public bool noThreatGate;

    /// <summary>The <see cref="pause"/> overrides.</summary>
    public const string PauseAlways = "pause", PauseNever = "live";

    /// <summary>
    /// A43: this close-up pauses the world (the game's Phase.Inspect at timeScale 0, C8) instead of running live. The override
    /// wins; otherwise a "read" target pauses when its transcript has more than one printed line (the evacuation plans, the
    /// station sheets: careful reading), and everything else runs live: the "look" kits (wall clock, vending machine, copier,
    /// desk phone), the keys and the first key, the door peek, and a "read" target with no transcript or a one-line one (the
    /// station TV ad, a door sign: read at a glance; 交互设计's rule, 2026-10-30 13:59).
    /// </summary>
    public bool Pauses
    {
        get
        {
            if (string.Equals(pause, PauseAlways, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(pause, PauseNever, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.Equals(Verb, "read", StringComparison.OrdinalIgnoreCase)) return false;
            return PrintedLines(FrontRoomsInspectCatalog.Transcript(transcript)) > 1;
        }
    }

    /// <summary>C36: the threat gate applies (see <see cref="noThreatGate"/>).</summary>
    public bool ThreatGated => !noThreatGate;

    /// <summary>The printed lines of a catalog transcript: every line but the region heads ('\u0002'); 0 for none.</summary>
    public static int PrintedLines(string[] lines)
    {
        if (lines == null) return 0;
        var n = 0;
        foreach (var l in lines)
            if (l != null && !(l.Length > 0 && l[0] == '\u0002')) n++;
        return n;
    }

    // ---- defaults (§3.8 table)
    public const float DefaultMargin = .08f, DefaultDistMaxA = .44f, DefaultMinDist = .18f, DefaultMaxDist = 1.0f;
    public const float DefaultMaxAngle = 25f, DefaultMaxAngleHigh = 40f, HighFaceY = 1.9f, DefaultMaxYawOff = 25f;
    public const float DefaultFovFlat = -15f, DefaultFovObject = -10f, DefaultReach = 1.6f, DefaultApproach = 60f;
    public const float DefaultOrbit = 20f, DefaultAimPad = .02f, MinAimSize = .08f, DefaultTexelsPerMetre = 2048f;

    public bool IsSet => !string.IsNullOrEmpty(kind);

    public InspectKind Kind => string.Equals(kind, "object", StringComparison.OrdinalIgnoreCase) ? InspectKind.Object
        : string.Equals(kind, "panel", StringComparison.OrdinalIgnoreCase) ? InspectKind.Panel : InspectKind.Flat;

    public string Verb => !string.IsNullOrEmpty(verb) ? verb : Kind == InspectKind.Flat ? "read" : "look";
    public string FaceAnchor => !string.IsNullOrEmpty(face) ? face : "face";
    public string FaceDirAnchor => !string.IsNullOrEmpty(faceDir) ? faceDir : "face_dir";
    public float Margin => margin > 0f ? margin : DefaultMargin;
    public float DistMaxA => distMaxA > 0f ? distMaxA : DefaultDistMaxA;
    public float MinDist => minDist > 0f ? minDist : DefaultMinDist;
    public float MaxDist => maxDist > 0f ? maxDist : DefaultMaxDist;
    public float MaxAngleFor(float faceY) => maxAngle > 0f ? maxAngle : faceY > HighFaceY ? DefaultMaxAngleHigh : DefaultMaxAngle;
    public float MaxYawOff => maxYawOff > 0f ? maxYawOff : DefaultMaxYawOff;
    public float FovDelta => fovDelta != 0f ? fovDelta : Kind == InspectKind.Object ? DefaultFovObject : DefaultFovFlat;
    public float Reach => reach > 0f ? reach : DefaultReach;
    public float ApproachAngle => approachAngle > 0f ? approachAngle : DefaultApproach;
    public float Orbit => orbit > 0f ? orbit : DefaultOrbit;
    public float AimPad => aimPad > 0f ? aimPad : DefaultAimPad;
    public float TexelsPerMetre => texelsPerMetre > 0f ? texelsPerMetre : DefaultTexelsPerMetre;
    public float BaseDist => dist != null && dist.Length > 0 ? dist[0] : 0f;
    public float ZoomDist => dist != null && dist.Length > 1 ? dist[1] : 0f;

    /// <summary>Composition A's viewport rect (x, y, w, h); null = full screen (composition B only).</summary>
    public Rect? ScreenRect => screen != null && screen.Length >= 4 && screen[2] > 0f && screen[3] > 0f
        ? new Rect(screen[0], screen[1], screen[2], screen[3]) : (Rect?)null;

    /// <summary>The smallest cap over the regions (mm), 0 when none is listed.</summary>
    public float SmallestCapMm
    {
        get
        {
            var c = 0f;
            if (regions != null)
                foreach (var r in regions)
                    if (r != null && r.capMm > 0f) c = c <= 0f ? r.capMm : Mathf.Min(c, r.capMm);
            return c;
        }
    }

    /// <summary>
    /// A region's centre and size on the face, in metres, in VIEWER axes (x to the viewer's right, y up) from the face
    /// centre. The rect is sheet mm from the sheet's top-left (as 平面视觉 exports it); the sheet is centred on the face
    /// anchor. False when the region has no rect (the placeholder [0, 0, 0, 0]).
    /// </summary>
    public bool TryRegion(int index, Vector2 faceSize, out Vector2 centre, out Vector2 size)
    {
        centre = size = Vector2.zero;
        if (regions == null || index < 0 || index >= regions.Length) return false;
        var r = regions[index];
        if (r == null || r.rect == null || r.rect.Length < 4 || r.rect[2] <= 0f || r.rect[3] <= 0f) return false;
        var sw = sheet != null && sheet.Length >= 2 && sheet[0] > 0f ? sheet[0] : faceSize.x * 1000f;
        var sh = sheet != null && sheet.Length >= 2 && sheet[1] > 0f ? sheet[1] : faceSize.y * 1000f;
        centre = new Vector2(r.rect[0] + r.rect[2] * .5f - sw * .5f, sh * .5f - (r.rect[1] + r.rect[3] * .5f)) * .001f;
        size = new Vector2(r.rect[2], r.rect[3]) * .001f;
        return true;
    }

    public int RegionCount => regions != null ? regions.Length : 0;

    public InspectSound Sound => sound switch
    {
        "paper" => InspectSound.Paper,
        "metal" => InspectSound.Metal,
        "keys" => InspectSound.Keys,
        _ => InspectSound.Plastic,
    };

    public InspectDef Clone() => (InspectDef)MemberwiseClone();
}
