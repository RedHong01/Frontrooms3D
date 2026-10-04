// Compatibility shims for ChatGPT's prototype names (G14). Same file and script guid (6752b013…), so MapWorld's two
// existing lines keep compiling until the map chat removes them (design 10 §2.4 C1/C2). The real system is
// FrontRoomsGlassRT (Assets/Scripts/Rendering/GlassRT): it starts itself from FrontRoomsPostStack.ConfigureCamera,
// registers the map's renderers without any MapWorld call, and takes receivers from their material (_RTReceive = 1).
using System;
using UnityEngine;

/// <summary>
/// Hint only (kept for MapWorld's pane tagging, C2). A renderer carrying it is traced as glass; it RECEIVES ray-traced
/// reflections only when its material is FrontRooms/Glass with _RTReceive = 1 (Glass_Window). The destruction track
/// registers its visible slab / stage meshes through FrontRoomsGlassRT.Register instead.
/// </summary>
[DisallowMultipleComponent]
public sealed class FrontRoomsMetalGlassTarget : MonoBehaviour
{
}

/// <summary>Obsolete entry point of the prototype controller (MapWorld.Awake calls it for standalone play, C1).</summary>
public static class FrontRoomsMetalGlassRTController
{
    [Obsolete("The RT glass system starts itself from FrontRoomsPostStack.ConfigureCamera (FrontRoomsGlassRT.OptIn). Remove this call (G14 contract C1).")]
    public static void Ensure() => FrontRoomsGlassRT.EnsureStarted();
}
