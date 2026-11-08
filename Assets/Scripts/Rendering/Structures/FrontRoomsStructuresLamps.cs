using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Scene 4's shared lamp parts (structures_assets 30_plan.md P6 + P7; line visual (game visual), 2026-11-06): the far lamp pools and the
/// lens drive of the lit kits (the laylight module, the light well). Inert until the K1 builder calls it; it creates no light.
///
/// Far pools (20_research.md C.3.1, S1). Past the light budget's 16-19 m a lamp keeps its lit lens but loses its light, so the
/// floor under a lit lens goes dark (measured: SP3's spine at 17-20 m, Y .115 against .23-.29). A pool is a floor quad on
/// FrontRooms/LampPool (Resources/Props/Lamps/Shaders/FrontRoomsLampPool.shader): an analytic patch the size of the lamp's inner
/// cone, no texture, in two parts in one pass: the floor's own shade (its albedo under the light it still has) lifted, so its
/// pattern stays, plus the lamp's own floor radiance added, so a floor nothing else lights still gets its pool (ZL_FarPool:
/// _Gain .25, _Add .10, sigma .40 of the quad, for Level 0's troffer on carpet: neighbouring pools overlap as the lamps' light does; a zone scales it through the share); transparent (GlassRT and the planar
/// water never see it), faded to nothing in fog, clipped to the room's rectangle so it never crosses a wall.
/// The caller drives each pool with SetPool(pool, colour, share): share = (1 - the light's budget level) x the lamp's state
/// (FrontRoomsStructuresLampModes), so light and pool cross-fade and a dead lamp leaves its floor dark. All pools share one
/// unit quad and the material; each carries its drive and clip in its own property block. Honest flaw: a pool lights nothing
/// standing in it (no shadow, no specular), which reads only on a prop in the far band.
///
/// Lit kits. Kit_Laylight_Module (+ _B, _C, _DeadRow) and Kit_LightWell_Round are Props/Models kits whose lens is the
/// Prop_LaylightDiffuser submesh; DriveLens writes _EmissionColor on every renderer of the instance (the T-bar and frame
/// materials have no emission, so only the diffuser takes it) = the zone lens's tint at LaylightLensPeak x the lamp's level:
/// a laylight flickers, dims, dies and blacks out with its lights, as every lamp of the game does (VISUAL_STANDARD 1.2).
/// ASCII only.
/// </summary>
public static class FrontRoomsStructuresLamps
{
    public const string PoolMaterialPath = "Props/Lamps/FarPool/Materials/ZL_FarPool";
    /// <summary>The laylight's lens as seen at full drive. SP4's stand-in glow drove 1.7 (SP2's 1.45): through the game's post that
    /// clips to one flat white, and the lamp rows, the panels' age and a dead row vanish (G1-b booth: laylight_lit17); 1.15 still sits on
    /// the tone curve's shoulder (rows faint, age gone); 0.9 keeps the laylight the brightest surface with its rows, its panels and a dead
    /// row readable. The caller may pass 1.7 for today's level.</summary>
    public const float LaylightLensPeak = 0.9f;
    /// <summary>The light well's diffuser (the oculus's lit disc, 2.4-2.65 in the stand-in; the kit's map peaks at 1).</summary>
    public const float LightWellLensPeak = 2.4f;
    /// <summary>Floor clearance of a pool quad (plus the shader's polygon offset).</summary>
    public const float PoolLift = .002f;

    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    static readonly int ClipId = Shader.PropertyToID("_Clip");
    static Material poolMaterial;
    static Mesh quad;
    static bool poolLoaded, warned;
    static MaterialPropertyBlock block;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        poolMaterial = null;
        quad = null;
        poolLoaded = warned = false;
        block = null;
    }

    /// <summary>The pool material is in the build (false: build no pools; the scene stays as today).</summary>
    public static bool PoolsAvailable
    {
        get
        {
            if (!poolLoaded)
            {
                poolLoaded = true;
                poolMaterial = Resources.Load<Material>(PoolMaterialPath);
                if (poolMaterial == null && !warned)
                {
                    warned = true;
                    Debug.LogWarning("[FrontRoomsStructuresLamps] Resources/" + PoolMaterialPath + " is missing: no far lamp pools.");
                }
            }
            return poolMaterial != null;
        }
    }

    static Mesh Quad()
    {
        if (quad != null) return quad;
        quad = new Mesh { name = "far lamp pool quad (shared)" };
        quad.SetVertices(new[] { new Vector3(-.5f, 0f, -.5f), new Vector3(.5f, 0f, -.5f), new Vector3(.5f, 0f, .5f), new Vector3(-.5f, 0f, .5f) });
        quad.SetUVs(0, new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) });
        quad.SetNormals(new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
        quad.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
        quad.bounds = new Bounds(Vector3.zero, new Vector3(1f, .01f, 1f));
        quad.UploadMeshData(true);
        return quad;
    }

    /// <summary>
    /// A far pool under a lamp: a floor quad centred on <paramref name="floorCentre"/> (parent space: the floor point under the
    /// lamp), <paramref name="radius"/> metres to its edge (the inner cone's footprint: about 3 m for a troffer at 2.9 m), clipped
    /// to <paramref name="room"/> (x = x0, y = z0, width, height in parent x / z; a zero-size rect means no clip). Off until
    /// SetPool. Null when the material is missing.
    /// </summary>
    public static MeshRenderer BuildPool(Transform parent, Vector3 floorCentre, float radius, Rect room, string name)
    {
        if (parent == null || radius <= .05f || !PoolsAvailable) return null;
        var go = new GameObject(string.IsNullOrEmpty(name) ? "far lamp pool" : name);
        var t = go.transform;
        t.SetParent(parent, false);
        t.localPosition = floorCentre + Vector3.up * PoolLift;
        t.localRotation = Quaternion.identity;
        t.localScale = new Vector3(radius * 2f, 1f, radius * 2f);
        go.AddComponent<MeshFilter>().sharedMesh = Quad();
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = poolMaterial;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.Off;
        r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        r.allowOcclusionWhenDynamic = true;
        var clip = new Vector4(0f, 0f, 1f, 1f);
        if (room.width > 0f && room.height > 0f)
        {
            var d = 2f * radius;
            clip = new Vector4(
                Mathf.Clamp01((room.xMin - (floorCentre.x - radius)) / d), Mathf.Clamp01((room.yMin - (floorCentre.z - radius)) / d),
                Mathf.Clamp01((room.xMax - (floorCentre.x - radius)) / d), Mathf.Clamp01((room.yMax - (floorCentre.z - radius)) / d));
        }
        if (block == null) block = new MaterialPropertyBlock();
        block.Clear();
        block.SetVector(ClipId, clip);
        block.SetColor(EmissionColorId, Color.black);
        r.SetPropertyBlock(block);
        r.enabled = false;
        return r;
    }

    /// <summary>Drives a pool: the light's colour (any brightness; normalised here) times <paramref name="share"/> (0..1: the part
    /// of the lamp's light the budget has taken away, times the lamp's own level). Off at zero.</summary>
    public static void SetPool(Renderer pool, Color lightColor, float share)
    {
        if (pool == null) return;
        var on = share > .001f;
        if (pool.enabled != on) pool.enabled = on;
        if (!on) return;
        var m = Mathf.Max(1e-4f, lightColor.maxColorComponent);
        var c = new Color(lightColor.r / m * share, lightColor.g / m * share, lightColor.b / m * share, 1f);
        if (block == null) block = new MaterialPropertyBlock();
        pool.GetPropertyBlock(block);
        block.SetColor(EmissionColorId, c);
        pool.SetPropertyBlock(block);
    }

    /// <summary>The drive a lit kit's lens takes: the zone lens's tint (any brightness) at <paramref name="peak"/> x
    /// <paramref name="level"/> (gamma as passed, as FrontRoomsLampKit.LensDrive).</summary>
    public static Color LensDrive(Color tint, float peak, float level)
    {
        var m = Mathf.Max(1e-4f, tint.maxColorComponent);
        var k = peak * Mathf.Max(0f, level);
        return new Color(tint.r / m * k, tint.g / m * k, tint.b / m * k, 1f);
    }

    /// <summary>Writes <paramref name="drive"/> as _EmissionColor on every renderer of a lit kit's instance (every LOD).</summary>
    public static void DriveLens(GameObject kit, Color drive)
    {
        if (kit == null) return;
        if (block == null) block = new MaterialPropertyBlock();
        var rs = kit.GetComponentsInChildren<Renderer>(true);
        for (var i = 0; i < rs.Length; i++)
        {
            var r = rs[i];
            if (r == null) continue;
            r.GetPropertyBlock(block);
            block.SetColor(EmissionColorId, drive);
            r.SetPropertyBlock(block);
        }
    }
}
