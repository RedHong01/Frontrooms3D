// Surface detail (research/surface_detail/60_build.md section 2). Game visual line.
using UnityEngine;

/// <summary>
/// The switch between today's surfaces and the revised ones, and their tier. FrontRooms/Surface reads the global _FR_Detail on
/// every material that enables _FR_DETAIL (the Office's walls, carpet, ceiling, panel fabric, laminate, painted steel and the
/// vinyl cove base): x 1 = today's surfaces exactly (<see cref="Today"/>), y 1 = the reduced tier (<see cref="Reduced"/>:
/// phones and WebGL players skip the fine detail map and the micro-shadowing; the sheets, joints, lots, wear and sheen stay).
/// Unset, the global reads (0, 0): the revised surfaces at the full tier, so nothing has to run for the new look to show, in
/// the Editor, in Play or in a build. WebGL compiles the path out of the shader (its own track).
/// </summary>
public static class FrontRoomsSurfaceDetail
{
    static readonly int DetailId = Shader.PropertyToID("_FR_Detail");
    static bool today;
    static bool reduced;

    /// <summary>True: every _FR_DETAIL material draws exactly as before this package (compare against today).</summary>
    public static bool Today
    {
        get { return today; }
        set { today = value; Publish(); }
    }

    /// <summary>True: the reduced tier (set at start from the phone tier; tools may force it).</summary>
    public static bool Reduced
    {
        get { return reduced; }
        set { reduced = value; Publish(); }
    }

    /// <summary>Writes the global from <see cref="Today"/> and <see cref="Reduced"/>.</summary>
    public static void Publish()
    {
        Shader.SetGlobalVector(DetailId, new Vector4(today ? 1f : 0f, reduced ? 1f : 0f, 0f, 0f));
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        reduced = FrontRoomsTrofferKit.PhoneTier;
        Publish();
    }
}
