using UnityEngine;

/// <summary>
/// LOD cross-fade for the kits' LODGroups (tech_art 20_ranked_changes row 7, research/tech_art/60_visual_build.md "Row 7"):
/// props, troffer and lamp housings, door plates, window hardware, the exit chain, the door break kit and the Run! re-stack
/// dissolve from one level of detail into the next instead of popping. The URP assets already have LOD cross-fade on (Blue
/// Noise dither); FrontRooms/Surface and FrontRooms/Glass carry LOD_FADE_CROSSFADE in every pass that draws them.
/// Animated: a switch still happens at the authored distance (times QualitySettings.lodBias) and then dissolves over
/// LODGroup.crossFadeAnimationDuration (Unity's 0.5 s), so a still frame always shows one level and nothing dithers while the
/// player stands at a switch distance. Each level also carries a transition width for the distance-based fade that the GPU
/// Resident Drawer falls back to (tech_art row 6): the dissolve then spans the last DistanceBand of that level's distance.
/// Called by the kit importer (Editor/Rendering/FrontRoomsKitImporter.cs), the prefabs carry the same values, and every
/// runtime builder that makes a LODGroup of its own calls it after SetLODs.
/// </summary>
public static class FrontRoomsLodFade
{
    /// <summary>Share of a level's switch distance over which a distance-based fade dissolves (the GPU Resident Drawer's
    /// fallback only; the animated fade ignores it).</summary>
    public const float DistanceBand = .1f;

    /// <summary>Cross-fade on, animated, with each level's transition width from <see cref="Width"/>. Call after SetLODs.</summary>
    public static void Apply(LODGroup group)
    {
        if (group == null) return;
        var lods = group.GetLODs();
        if (lods.Length > 0)
        {
            SetWidths(lods);
            group.SetLODs(lods);
        }
        Enable(group);
    }

    /// <summary>Cross-fade on and animated, the levels left as they are (for a group whose levels are set again later).</summary>
    public static void Enable(LODGroup group)
    {
        if (group == null) return;
        group.fadeMode = LODFadeMode.CrossFade;
        group.animateCrossFading = true;
    }

    /// <summary>Every level's fadeTransitionWidth, from its own switch height and the one above it.</summary>
    public static void SetWidths(LOD[] lods)
    {
        if (lods == null) return;
        var upper = 1f;
        for (var i = 0; i < lods.Length; i++)
        {
            var h = lods[i].screenRelativeTransitionHeight;
            lods[i].fadeTransitionWidth = Width(h, upper);
            upper = h;
        }
    }

    /// <summary>
    /// The transition width (a share of the level's own height range, upper - height) whose fade starts at
    /// (1 - DistanceBand) of the switch distance: the screen height grows as 1 / distance, so there it is
    /// height / (1 - DistanceBand). Zero for a level that never culls (height 0) or has no range.
    /// </summary>
    public static float Width(float height, float upper)
    {
        var range = upper - height;
        if (height <= 0f || range <= 1e-6f) return 0f;
        return Mathf.Clamp01(height * (1f / (1f - DistanceBand) - 1f) / range);
    }
}
