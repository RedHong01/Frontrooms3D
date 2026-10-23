using UnityEngine;

/// <summary>
/// The still desk screens (tutorial T3 → V4; Documentation/research/tutorial/PREBUILD_v4desk.md). On a floor with a
/// station (<see cref="FrontRoomsStationPresence.FloorHasStation"/>) the Office desk CRTs stop playing the ad and
/// each shows one still frame of 平面视觉 · 广告's v2 art (Tools/screens/out/still_desk/README.md), so the station TV
/// is the only screen that moves. <see cref="FrontRoomsScreenVideo"/> does the switching; this holds the art, the
/// seeded pick and the fit.
///
/// The README's rules, and where each one is kept:
/// - one still texture per monitor, never switched while it can be seen (FrontRoomsScreenVideo.UpdateDeskStill);
/// - shown 1:1: the frame's own pixels on the ad surface material (white tint, no emission, no boost), at the art's
///   aspect (4:3 on the 1.47:1 face: centred, the sides filled by the art's own border colour through Clamp);
/// - neutral white (the art's 150/156/160 on 9/10/11; nothing here tints it);
/// - seeded per desk: <see cref="Pick"/> hashes the map seed, the floor and the desk's place in the map, so a desk
///   shows the same frame every time it is rebuilt; desk_cursor most often, desk_cursor_low and desk_blank sometimes,
///   some sets off (dark glass);
/// - no tube effects.
/// Frames load from Resources on first use (nothing is loaded on a floor without a station). The @2x set is used
/// only where 1x would be magnified on screen; phones (FrontRoomsHandheld.Active) and WebGL always use 1x.
/// </summary>
public static class FrontRoomsDeskStill
{
    public enum Frame { Cursor = 0, CursorLow = 1, Blank = 2, Off = 3 }

    /// <summary>Resources folder of the six frames (640 × 480 and @2x 1280 × 960, the terminal's 40 × 15 grid of 16 × 32 cells).</summary>
    public const string Folder = "Rendering/StillDesk/";
    static readonly string[] Names = { "desk_cursor", "desk_cursor_low", "desk_blank" };

    /// <summary>Shares of the pick; the rest (0.15) are sets left off.</summary>
    public const float CursorShare = .55f, CursorLowShare = .17f, BlankShare = .13f;

    /// <summary>The art's rows at 1x.</summary>
    public const int ArtRows = 480;
    /// <summary>The closest the eye gets to a desk CRT's face: the screen sits ~0.33 m behind the desk's front edge, plus the 0.3 m capsule.</summary>
    public const float ClosestViewMetres = .6f;
    /// <summary>The game camera's field of view when none is at hand (FrontRooms3DGame: 76°).</summary>
    public const float DefaultFov = 76f;

    static readonly Texture2D[] Lo = new Texture2D[3], Hi = new Texture2D[3];
    static bool warnedMissing;
    static readonly Plane[] Planes = new Plane[6];
    static int planesFrame = -1;
    static Camera planesCamera;

    /// <summary>The frame of the desk at <paramref name="mapLocal"/> (metres, in the map's space) on this map and floor.</summary>
    public static Frame Pick(int mapSeed, int floor, Vector3 mapLocal)
    {
        var u = Unit(Hash(mapSeed, floor, Mathf.RoundToInt(mapLocal.x * 100f), Mathf.RoundToInt(mapLocal.y * 100f), Mathf.RoundToInt(mapLocal.z * 100f)));
        if (u < CursorShare) return Frame.Cursor;
        if (u < CursorShare + CursorLowShare) return Frame.CursorLow;
        if (u < CursorShare + CursorLowShare + BlankShare) return Frame.Blank;
        return Frame.Off;
    }

    static uint Hash(int a, int b, int c, int d, int e)
    {
        unchecked
        {
            var h = 2166136261u;
            h = Mix(h, a);
            h = Mix(h, b);
            h = Mix(h, c);
            h = Mix(h, d);
            h = Mix(h, e);
            h ^= h >> 16;
            h *= 0x85ebca6bu;
            h ^= h >> 13;
            h *= 0xc2b2ae35u;
            h ^= h >> 16;
            return h;
        }
    }

    static uint Mix(uint h, int v)
    {
        unchecked
        {
            var x = (uint)v;
            for (var i = 0; i < 4; i++)
            {
                h ^= x & 0xffu;
                h *= 16777619u;
                x >>= 8;
            }
            return h;
        }
    }

    static float Unit(uint h) => (h >> 8) * (1f / 16777216f);

    /// <summary>How many screen rows the art's height covers on a quad <paramref name="quadHeight"/> m tall seen square-on from <paramref name="distance"/> m.</summary>
    public static float ShownRows(float fovDegrees, int screenHeightPx, float quadHeight, float distance)
    {
        return screenHeightPx * quadHeight / (2f * Mathf.Max(.01f, distance) * Mathf.Tan(Mathf.Clamp(fovDegrees, 1f, 179f) * .5f * Mathf.Deg2Rad));
    }

    /// <summary>
    /// The @2x frames where 1x would be magnified: the art's 480 rows shown taller than 480 px at the closest view.
    /// At the game's 76° that needs a window over ~2,370 px tall. Phones and WebGL always take 1x (separate tracks: memory first).
    /// </summary>
    public static bool UseHiRes(float fovDegrees, int screenHeightPx, float quadHeight)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return false;
#else
        if (FrontRoomsHandheld.Active) return false;
        return ShownRows(fovDegrees, screenHeightPx, quadHeight, ClosestViewMetres) > ArtRows;
#endif
    }

    /// <summary>The camera the screens use now: the hi-res choice for this window.</summary>
    public static bool UseHiResNow(Camera camera, float quadHeight)
    {
        return UseHiRes(camera != null ? camera.fieldOfView : DefaultFov, Screen.height, quadHeight);
    }

    /// <summary>The texture of <paramref name="frame"/> (an Off set shows desk_cursor if the player turns it on); null if the art is missing.</summary>
    public static Texture2D Texture(Frame frame, bool hiRes)
    {
        var i = frame == Frame.Off ? (int)Frame.Cursor : (int)frame;
        var set = hiRes ? Hi : Lo;
        if (set[i] == null) set[i] = Resources.Load<Texture2D>(Folder + Names[i] + (hiRes ? "@2x" : ""));
        if (set[i] == null && hiRes) return Texture(frame, false);
        if (set[i] == null && !warnedMissing)
        {
            warnedMissing = true;
            Debug.LogWarning("[FrontRoomsDeskStill] Resources/" + Folder + Names[i] + " is missing; desk screens keep the ad.");
        }
        return set[i];
    }

    /// <summary>Loads the three frames this window will use, so the first desk of a station floor does not load them.</summary>
    public static void Prepare()
    {
        var hi = UseHiResNow(Camera.main, .19f);
        for (var i = 0; i < Names.Length; i++) Texture((Frame)i, hi);
    }

    /// <summary>
    /// _BaseMap scale (xy) and offset (zw) that show a w × h frame at its own aspect on a quad of <paramref name="quad"/>
    /// metres: centred, the spare width (or height) filled by the frame's edge colour (the texture clamps; the art's border
    /// is the field colour 9/10/11, so the fill is invisible).
    /// </summary>
    public static Vector4 Fit(Vector2 quad, int width, int height)
    {
        if (quad.x <= 0f || quad.y <= 0f || width <= 0 || height <= 0) return new Vector4(1f, 1f, 0f, 0f);
        var quadAspect = quad.x / quad.y;
        var artAspect = (float)width / height;
        if (quadAspect >= artAspect)
        {
            var s = quadAspect / artAspect;
            return new Vector4(s, 1f, (1f - s) * .5f, 0f);
        }
        var t = artAspect / quadAspect;
        return new Vector4(1f, t, 0f, (1f - t) * .5f);
    }

    /// <summary>Whether <paramref name="bounds"/> is inside <paramref name="camera"/>'s view frustum (planes cached per frame).</summary>
    public static bool InView(Camera camera, Bounds bounds)
    {
        if (camera == null) return false;
        if (planesFrame != Time.frameCount || planesCamera != camera)
        {
            GeometryUtility.CalculateFrustumPlanes(camera, Planes);
            planesFrame = Time.frameCount;
            planesCamera = camera;
        }
        return GeometryUtility.TestPlanesAABB(Planes, bounds);
    }
}
