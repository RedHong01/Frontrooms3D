// FrontRoomsGlassTypeProfile.cs -- GD3 step 3 (plan 10 sec 4.3): the generator numbers per glass type and tier as an asset.
// One asset per built-in profile lives in Resources/Rendering/GlassProfiles (made by the editor menu
// FrontRooms/Glass/Write Profile Assets). The numbers equal FrontRoomsGlassProfileData's built-ins, which equal
// crack_graph_ref.py PROFILES; FrontRoomsGlassFractureTests fails if an asset drifts (the goldens would go stale).
// Runtime code asks Load(name): the asset if present, else the built-in, so a missing asset never changes the pattern.
using UnityEngine;

[CreateAssetMenu(menuName = "FrontRooms/Glass Type Profile", fileName = "GlassTypeProfile")]
public sealed class FrontRoomsGlassTypeProfile : ScriptableObject
{
    public const string ResourceFolder = "Rendering/GlassProfiles";

    [Tooltip("Generator numbers (crack_graph_ref.py PROFILES). Edit only together with the reference and its goldens.")]
    public FrontRoomsGlassProfileData data = new FrontRoomsGlassProfileData();

    /// <summary>The profile by name: the Resources asset when there is one, else the built-in (never null for a built-in name).</summary>
    public static FrontRoomsGlassProfileData Load(string name)
    {
        var asset = Resources.Load<FrontRoomsGlassTypeProfile>(ResourceFolder + "/" + name);
        if (asset != null && asset.data != null)
            return asset.data;
        return FrontRoomsGlassProfileData.BuiltinShared(name);
    }

    /// <summary>Lab only (never set by the game): run the reduced tier in the Editor to film it.</summary>
    public static bool DebugForceReducedTier;

    /// <summary>
    /// GD3c fix r1 (eng #10, art #5): handheld (FrontRoomsHandheld.Active: iOS and Android players, the Editor's handheld preview)
    /// joins the reduced tier only once it has had a device run. Until then this stays false: handheld players keep exactly the
    /// crack patterns they have now (the desktop profile) and their broken glass takes the desktop debris path.
    /// </summary>
    public static bool ReducedTierOnHandheld = false;

    /// <summary>
    /// The reduced tier (gd3c sec 3): WebGL players (the existing gate), the lab switch, and handheld only with
    /// ReducedTierOnHandheld. Desktop players and the Editor's normal Play Mode never take it, so no desktop value is lowered
    /// (plan sec 4.8), and on every player ForTier returns what it returned before GD3c.
    /// </summary>
    public static bool ReducedTier
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return true;
#else
            return DebugForceReducedTier || (ReducedTierOnHandheld && FrontRoomsHandheld.Active);
#endif
        }
    }

    /// <summary>
    /// The profile for the running tier: the WebGL tier only on the reduced tier (WebGL players, the lab switch; plan sec 4.8:
    /// desktop is never lowered for WebGL); Cinematic when asked for on desktop; else the type's High profile.
    /// </summary>
    public static FrontRoomsGlassProfileData ForTier(string glassType = FrontRoomsGlassProfileData.Annealed6, bool cinematic = false)
    {
        if (ReducedTier && glassType == FrontRoomsGlassProfileData.Annealed6)
            return Load(FrontRoomsGlassProfileData.Annealed6WebGL);
        if (cinematic && glassType == FrontRoomsGlassProfileData.Annealed6)
            return Load(FrontRoomsGlassProfileData.Annealed6Cinematic);
        return Load(glassType);
    }
}
