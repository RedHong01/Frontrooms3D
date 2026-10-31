using UnityEngine;

/// <summary>
/// The wall cue's visual half (A + M4 + V1; Documentation/research/cue_driver/CUE_DRIVER_INTERFACE.md
/// sections 1, 2 and 5 items 11 and 14). It owns the cue array _FR_Cue and the switch _FR_CueReady that
/// FrontRooms/Surface's CueInk reads. The map's FrontRoomsWayCueDriver (the map line's file) writes the cells
/// (_FR_WayCells, _FR_WayOrigin) and the print hold (FrontRoomsPrintDriver.SetHold "cue") itself; it only
/// calls Bind and Unbind here. Nothing in this class loads anything on its own (no MonoBehaviour), so until a
/// driver calls Bind the array is never loaded and every wall shows the print exactly as before.
///
/// Call order (Documentation/research/cue_driver/CUEINK_CONTRACT_README.md):
///   1. The driver's Bind (FrontRooms3DGame.MapRunStarted): FrontRoomsCueInk.Bind(). The array loads in the
///      background; the shader shows the cue from the frame it is bound (Ready). Until then, and wherever
///      Bind returns false (WebGL, no print driver), the walls keep the plain print whatever the
///      cells say, so the driver can run its state machine everywhere (the beacon path reads the same field).
///   2. Each LateUpdate: the driver writes its cells and _FR_WayOrigin (w = 1), as it does today.
///   3. The driver's Unbind (MapRunEnded, OnDestroy): FrontRoomsCueInk.Unbind(). The array stays loaded for
///      the next run; Release() also unloads it.
/// Texel addressing is toroidal: texel (cellX &amp; 63, cellZ &amp; 63) of the map's grid cell, the origin being the
/// map root (cell (0, 0)'s corner), so 64 x 3 m = 192 m = ModuleUnits.WorldPeriod.
/// </summary>
public static class FrontRoomsCueInk
{
    /// <summary>Resources path of the cue array (FrontRoomsPrintArray.BuildCue).</summary>
    public const string DefaultCue = "Print/FR_Cue_HardEdge";
    /// <summary>_FR_WayCells is Cells x Cells texels, one per map cell.</summary>
    public const int Cells = 64;
    /// <summary>Map cell size in metres (MapGrid.CellSize).</summary>
    public const float CellSize = 3f;
    /// <summary>Cells x CellSize = 192 m = ModuleUnits.WorldPeriod: the toroidal period.</summary>
    public const float Period = Cells * CellSize;

    /// <summary>State 0: the live print exactly (the array is never sampled).</summary>
    public const int StateBaseline = 0;
    /// <summary>States 1..3: the M4 turn 30 / 60 / 90 degrees (3 = fitted = flow click 0).</summary>
    public const int StateTurned = 3;
    /// <summary>States 4..8: the V1 flow clicks 1..5 (click 6 is state 3 again).</summary>
    public const int StateMax = 8;
    /// <summary>Route directions in world axes (the map root is never rotated).</summary>
    public const int DirPlusX = 0, DirPlusZ = 1, DirMinusX = 2, DirMinusZ = 3;

    static readonly int CueId = Shader.PropertyToID("_FR_Cue");
    static readonly int ReadyId = Shader.PropertyToID("_FR_CueReady");

    static Texture2DArray cue;
    static ResourceRequest loading;
    static bool wanted, unloadWhenLoaded;

    /// <summary>Between a Bind that returned true and Unbind.</summary>
    public static bool Bound => wanted;

    /// <summary>The array is bound and _FR_CueReady is 1: the shader shows what the cells say.</summary>
    public static bool Ready { get; private set; }

    /// <summary>The cue array once it has loaded (null before).</summary>
    public static Texture2DArray Array => cue;

    /// <summary>
    /// Whether the cue can show here. WebGL: no (its own track: the shader compiles CueInk out and the WebGL
    /// import tab is a 32 px stub). Phones: yes since C34 (Red 10-29): the iOS and Android tabs are 1024 px per
    /// slice (21.3 MiB once bound) and the driver keeps them turn-only (FrontRoomsWayCueDriver.TurnOnly).
    /// Everywhere it needs the print driver: the cue shares _FR_Print's sampler, and the driver holds the
    /// ambient print on K00 while cells are turned (the slices are authored on K00).
    /// </summary>
    public static bool Supported
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return false;
#else
            return FrontRoomsPrintDriver.Instance != null;
#endif
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ResetForPlay()
    {
        // Enter Play Mode without a domain reload keeps statics: start every Play unbound. Loads nothing.
        wanted = unloadWhenLoaded = false;
        loading = null;
        Ready = false;
        Shader.SetGlobalFloat(ReadyId, 0f);
    }

    /// <summary>
    /// Starts the cue for a run: loads the array (in the background unless synchronous; tools and captures
    /// pass true) and binds it with _FR_CueReady = 1 once it is there. Returns false where the cue is not
    /// supported or the array is missing; the walls then stay on the plain print.
    /// </summary>
    public static bool Bind(bool synchronous = false)
    {
        wanted = Supported;
        unloadWhenLoaded = false;
        if (!wanted) { SetReady(false); return false; }
        if (cue == null && synchronous)
        {
            cue = Resources.Load<Texture2DArray>(DefaultCue);
            loading = null;
            if (cue == null) Debug.LogWarning("[CueInk] Resources/" + DefaultCue + " is missing; cue off");
        }
        if (cue != null) { SetReady(true); return true; }
        if (synchronous) { wanted = false; return false; }
        if (loading == null)
        {
            loading = Resources.LoadAsync<Texture2DArray>(DefaultCue);
            if (loading == null) { wanted = false; return false; }
            loading.completed += OnLoaded;
        }
        return true;
    }

    static void OnLoaded(AsyncOperation op)
    {
        if (op != loading) return;
        cue = loading.asset as Texture2DArray;
        loading = null;
        if (cue == null) { Debug.LogWarning("[CueInk] Resources/" + DefaultCue + " is missing; cue off"); wanted = false; }
        if (unloadWhenLoaded) { unloadWhenLoaded = false; Release(); return; }
        SetReady(wanted && cue != null);
    }

    /// <summary>Stops the cue: _FR_CueReady = 0 (every wall back on the plain print). The array stays loaded.</summary>
    public static void Unbind()
    {
        wanted = false;
        SetReady(false);
    }

    /// <summary>Unbind, then unloads the array (after the load, if one is still running).</summary>
    public static void Release()
    {
        Unbind();
        if (loading != null) { unloadWhenLoaded = true; return; }
        if (cue == null) return;
        Resources.UnloadAsset(cue);
        cue = null;
    }

    static void SetReady(bool on)
    {
        Ready = on;
        if (on) Shader.SetGlobalTexture(CueId, cue);
        Shader.SetGlobalFloat(ReadyId, on ? 1f : 0f);
    }

    /// <summary>Index into a Cells x Cells Color32 array (rows bottom-up) for a map grid cell, toroidally.</summary>
    public static int TexelIndex(int cellX, int cellZ) => (cellZ & (Cells - 1)) * Cells + (cellX & (Cells - 1));

    /// <summary>
    /// One texel of _FR_WayCells: R from-state, G to-state (0..8), B cross-fade from -> to (0..1), A route dir +
    /// 4 x message (flow = message 1 FLOW; message 0 keeps the plain print whatever the states say).
    /// </summary>
    public static Color32 Encode(int fromState, int toState, float crossFade, int dir, bool flow)
    {
        return new Color32((byte)Mathf.Clamp(fromState, 0, StateMax), (byte)Mathf.Clamp(toState, 0, StateMax),
            (byte)Mathf.RoundToInt(Mathf.Clamp01(crossFade) * 255f), (byte)((dir & 3) + (flow ? 4 : 0)));
    }
}
