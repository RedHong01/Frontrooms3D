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

    /// <summary>The PROMPT line under the crosshair on DESKTOP (Bayon 20, with its "."): 2 px at 1080p (G6, the
    /// graphic-visuals call of 2026-10-29, measured on a desktop frame: at Bayon 20 a 1 px ring lies over the 2 px stems,
    /// "DOOR" core vs ring 2.65:1 on lit wallpaper, 3.25:1 at 2 px; hud_build DECISIONS #38). Every other keyline stays
    /// KeylineDistance. Read it through PromptKeyline(handheld).</summary>
    public const float PromptKeylineDistance = 2f;

    /// <summary>The prompt line's keyline for a HUD: PromptKeylineDistance on desktop; on phone the 1 pt text keyline
    /// (O-5, DECISIONS #23), which is already 3 px at the phone's 3x (2 pt = 6 px: Unity's 4 diagonal Outline copies
    /// separate from Bayon 17's stems and read as a ghost of the line, capture 2026-10-29, DECISIONS #38). The map's
    /// promptText and FrontRoomsKeyHud's line 1 both read this, so the line looks the same whoever draws it.</summary>
    public static float PromptKeyline(bool handheld) => handheld ? KeylineDistance : PromptKeylineDistance;

    /// <summary>KeylineColor with KeylineAlpha.</summary>
    public static Color Keyline => new Color(KeylineColor.r, KeylineColor.g, KeylineColor.b, KeylineAlpha);

    /// <summary>The alpha for ONE tap of UnityEngine.UI.Outline (it draws 4 diagonal copies; at a ring pixel two of
    /// them overlap), so the doubled ring comes to KeylineAlpha: 1 - sqrt(1 - KeylineAlpha).</summary>
    public static float OutlineTapAlpha => 1f - Mathf.Sqrt(1f - KeylineAlpha);

    /// <summary>The HUD's text paper, #F4F1E8 (sRGB, opaque), on the shared keyline. A second line is made quieter only by
    /// its face and size, never by a dimmer ink (the T5 rule; hud_build DECISIONS #39 (1), the graphic-visuals call O-35 (a),
    /// 2026-10-29). The key panel's line 2 (OPENS THE STAIR DOOR, Plex Mono 13 desktop / 11 phone) reads it on phone and
    /// desktop; it was the grey meta #BDBAB0, 1.6-2.6:1 on lit Level 0.</summary>
    public static readonly Color TextPaper = new Color(244f / 255f, 241f / 255f, 232f / 255f, 1f);
}
