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

    /// <summary>
    /// The profile for the running tier: the WebGL tier only in WebGL players (plan sec 4.8: desktop is never lowered for
    /// WebGL); Cinematic when asked for on desktop; else the type's High profile.
    /// </summary>
    public static FrontRoomsGlassProfileData ForTier(string glassType = FrontRoomsGlassProfileData.Annealed6, bool cinematic = false)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (glassType == FrontRoomsGlassProfileData.Annealed6)
            return Load(FrontRoomsGlassProfileData.Annealed6WebGL);
#endif
        if (cinematic && glassType == FrontRoomsGlassProfileData.Annealed6)
            return Load(FrontRoomsGlassProfileData.Annealed6Cinematic);
        return Load(glassType);
    }
}
