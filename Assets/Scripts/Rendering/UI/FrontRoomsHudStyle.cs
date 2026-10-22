using UnityEngine;

/// <summary>
/// The HUD's shared legibility style (the game-visuals line, hud_build DECISIONS #4 / #22 / #31): no dark backing card; paper
/// shapes and text get a 1 px ink keyline OUTSIDE the silhouette instead. One place for every HUD layer that draws a
/// keyline (FrontRoomsKeyHud, the touch layer's labels), so they can never drift apart.
/// The design value is #141414 at 85 % composited in sRGB (the browser, the lab, Figma). Unity blends UI in LINEAR
/// light (this project's colour space), where one alpha cannot equal an sRGB blend over every background:
/// KeylineAlpha is the linear-light alpha calibrated on the game's own backgrounds behind the crosshair (the HUD
/// capture stage, 2026-10-22: lit Level 0, the dark cell, the Office and the evacuation plan; 0.95 keeps the ring's
/// mean within 2.4/255 of the 85 % sRGB look on all four, the minimax).
/// </summary>
public static class FrontRoomsHudStyle
{
    /// <summary>The keyline ink, #141414 (sRGB, opaque; apply KeylineAlpha on top).</summary>
    public static readonly Color KeylineColor = new Color(20f / 255f, 20f / 255f, 20f / 255f, 1f);

    /// <summary>The keyline's alpha in Unity's linear-light blend (the design's 85 % sRGB, calibrated; see above).</summary>
    public const float KeylineAlpha = .95f;

    /// <summary>The keyline's width outside the silhouette, in the drawing's own units: 1 px at 1080p on desktop,
    /// 1 pt on phone (O-5). A drawing that is itself scaled by the touch HUD scale (FrontRoomsKeyHud's crosshair
    /// shapes, x 0.75) carries that scale on top, so its keyline is KeylineDistance x the touch scale.</summary>
    public const float KeylineDistance = 1f;

    /// <summary>KeylineColor with KeylineAlpha.</summary>
    public static Color Keyline => new Color(KeylineColor.r, KeylineColor.g, KeylineColor.b, KeylineAlpha);

    /// <summary>The alpha for ONE tap of UnityEngine.UI.Outline (it draws 4 diagonal copies; at a ring pixel two of
    /// them overlap), so the doubled ring comes to KeylineAlpha: 1 - sqrt(1 - KeylineAlpha).</summary>
    public static float OutlineTapAlpha => 1f - Mathf.Sqrt(1f - KeylineAlpha);
}
