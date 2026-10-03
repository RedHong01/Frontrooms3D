using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Scene light that does not come from a fixture: the ambient bounce and the
/// haze. Kept in one place so the title corridor (RoomStream), the maze
/// (MapWorld) and the shipped scene (FrontRoomsRenderSetup) all match.
/// RenderSettings colours are gamma-space.
/// </summary>
public static class FrontRoomsLook
{
    // The practicals are downward spots, so the ceiling faces down into the
    // ground colour: the warm bounce off a lit beige carpet. Walls take the
    // equator; floors face the sky colour, which is the dim ceiling.
    // Values are Red's, tuned in the Lighting window (FrontRooms3D.unity), so
    // calling ApplyAmbient at run start leaves the game's image unchanged.
    public static readonly Color AmbientSky = new Color(.20f, .19f, .15f);
    public static readonly Color AmbientEquator = new Color(.26f, .24f, .17f);
    public static readonly Color AmbientGround = new Color(.40f, .36f, .24f);
    public static readonly Color FogColor = new Color(.16f, .15f, .11f);
    public const float FogDensity = .014f;
    // The scene's slider value. Unity applies this slider in gamma: .3 is a decode of
    // GammaToLinear(.3) = 0.073 of the captured light. Once SetZoneReflection has run, the
    // zone's own intensity replaces it (FrontRoomsZoneReflection, stored in linear).
    public const float ReflectionIntensity = .3f;

    /// <summary>Which reflection cubemap the world uses (glass, VCT, metal): the player's zone,
    /// or DeadLamp when the lamp of the player's cell is dead or off.</summary>
    public enum ReflectionZone { Level0, Office, Tall, DeadLamp }

    /// <summary>
    /// Switch the default reflection to the zone's cubemap, crossfading over blendSeconds.
    /// Safe to call every frame with the same value. The map calls it at run start and
    /// on zone / dead-lamp changes. The first call (and any call outside Play mode or with
    /// blendSeconds 0) switches at once. Cubes: Resources/Rendering/Reflections/Refl_*
    /// (FrontRoomsZoneReflection; Documentation/VISUAL_CHAT_TASKS.md G6).
    /// </summary>
    public static void SetZoneReflection(ReflectionZone zone, float blendSeconds = .5f)
    {
        FrontRoomsZoneReflection.Set(zone, blendSeconds);
    }

    public static void ApplyAmbient()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        // Set ambientLight first: it aliases the sky colour.
        RenderSettings.ambientLight = AmbientSky;
        RenderSettings.ambientSkyColor = AmbientSky;
        RenderSettings.ambientEquatorColor = AmbientEquator;
        RenderSettings.ambientGroundColor = AmbientGround;
        RenderSettings.reflectionIntensity = ReflectionIntensity;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = FogColor;
        RenderSettings.fogDensity = FogDensity;
        // URP lights with the ambient probe (SH), which is only rebuilt from
        // these colours on a bake or here; without it the change is invisible.
        DynamicGI.UpdateEnvironment();
        // Keep the zone reflection (cube and intensity) if SetZoneReflection ran first.
        FrontRoomsZoneReflection.Reapply();
    }
}
