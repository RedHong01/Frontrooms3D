using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// One zone's look on top of the film look (research/bathroom_rooms/70_look_build.md, "Zone look"; 16 section 6 items 1-2,
/// 17 section 7 item 10, Red's Q4): the ambient bounce (a trilight in gamma colours, as RenderSettings takes them), the
/// haze (exp2 fog colour and density), an optional height fog over the zone's water (FrontRooms/Surface's _FR_HeightFog
/// term: URP 17 has no volumetric fog), and the zone's lamp preset. The zone's grade is its URP profile
/// Resources/Rendering/FrontRoomsPost_&lt;zone&gt; (FrontRoomsPostStack.EnsureZoneVolume), so white balance, exposure and
/// tints are tuned in that asset. Profiles live in Resources/Rendering/ZoneLook/FrontRoomsZoneLook_&lt;zone&gt;; a missing
/// asset falls back to Defaults(zone), so the code alone reproduces the look. ASCII only.
/// </summary>
[CreateAssetMenu(menuName = "FrontRooms/Zone look profile", fileName = "FrontRoomsZoneLook_Zone")]
public sealed class FrontRoomsZoneLookProfile : ScriptableObject
{
    [Tooltip("The zone this look belongs to: PoolClub, PoolHall, PoolLevel, PoolDark (the grade asset is FrontRoomsPost_<zone>).")]
    public string zone = "PoolHall";

    [Header("Ambient bounce (trilight, gamma, as the Lighting window)")]
    [Tooltip("Floors face it: the light from the ceiling.")] public Color ambientSky = new Color(.15f, .20f, .22f);
    [Tooltip("Walls face it.")] public Color ambientEquator = new Color(.17f, .23f, .24f);
    [Tooltip("Ceilings face it: the light from the floor and the water.")] public Color ambientGround = new Color(.21f, .28f, .28f);

    [Header("Haze (exponential squared, gamma colour)")]
    public Color fogColor = new Color(.11f, .16f, .17f);
    [Range(0f, .05f)] public float fogDensity = .010f;

    [Header("Ambient direction")]
    [Tooltip("Multiplier on the scene's directional fill (FrontRooms3D.unity's 'Soft ambient direction', the bounce of the room's lamps): 1 keeps it; the dark zone, whose overheads are all dead, keeps little of it.")]
    [Range(0f, 2f)] public float fillScale = 1f;

    [Header("Height fog over the water (FrontRooms/Surface)")]
    [Tooltip("A layer of humid haze sitting on the zone's water (17 section 4.5): density per metre at the water, falling off with height.")]
    public bool heightFog = true;
    [Range(0f, .3f)] public float heightFogDensity = .035f;
    [Tooltip("Metres over which the layer thins by e (0.7: about 12 % of the density at 1.5 m above the water).")]
    [Range(.1f, 4f)] public float heightFogScale = .7f;
    public Color heightFogColor = new Color(.36f, .45f, .47f);

    [Header("Lamp preset (the lamp kit path takes it per zone)")]
    [Tooltip("The overhead lamps' light colour (gamma, as Light.color).")]
    public Color lampLight = new Color(.90f, .97f, 1f);
    [Tooltip("Multiplier on the rig's own intensity.")]
    public float lampIntensity = 1f;
    [Tooltip("The overhead lens tint (normalised; the kit's own peak drive keeps the level).")]
    public Color lampLens = new Color(.90f, .97f, 1f);
    [Tooltip("The wet-niche lens as seen at its own face (warm incandescent, 2800 K, 0.7 m under water).")]
    public Color nicheLens = new Color(1f, .80f, .58f);
    [Tooltip("The wet-niche light at the lamp (2800 K). Since the Submerged stage the water absorbs along each ray's path in the shader (FrontRoomsWaterSubmerged), so this is the lens colour; before it, the colour after about 4 m of water stood in.")]
    public Color nicheLight = new Color(1f, .80f, .58f);
    public float nicheIntensity = 2.4f;
    [Header("Submerged (FrontRoomsWaterSubmerged)")]
    [Tooltip("Absorption scale of this zone's water (17 Q1, Red: 'wrong'): the club physical (1), the pool level 3-5 (one or two rooms 9 through the water's own absorbScale).")]
    public float waterAbsorbScale = 1f;
    [Tooltip("The wet niches' thrown light (their glow out of the basin onto ceilings and walls): 1 = the physical share (FrontRoomsWaterSubmerged.NicheGlow), 0 = none.")]
    public float thrownLight = 1f;

    [Header("Contact shade on the pool tile (FrontRooms/Surface's real look)")]
    [Tooltip("The share of the shared SSAO's darkening this zone's pool tile keeps (1 = the renderer's, as before). White porcelain bounces so much light that real contact shade is faint; the shared intensity 1.6 drew every base, parapet and riser corner as dark wire (level_space review SP6-F12). 0.6 is about intensity 0.7 under 1.6 (URP's SSAO: darkening ~ intensity^0.6). Only the tile's real look reads it (FrontRoomsPoolTile.Look not Today); the SSAO feature itself is untouched.")]
    [Range(0f, 1f)] public float contactShade = 1f;

    [System.NonSerialized] bool hasProbe;
    [System.NonSerialized] SphericalHarmonicsL2 probe;

    /// <summary>The ambient probe of this trilight (FrontRoomsZoneLook.TrilightProbe), cached until the asset changes.</summary>
    public SphericalHarmonicsL2 AmbientProbe
    {
        get
        {
            if (!hasProbe)
            {
                probe = FrontRoomsZoneLook.TrilightProbe(ambientSky, ambientEquator, ambientGround);
                hasProbe = true;
            }
            return probe;
        }
    }

    /// <summary>Forget the cached probe (the values changed).</summary>
    public void Invalidate() { hasProbe = false; }

    void OnValidate() { hasProbe = false; }
    void OnEnable() { hasProbe = false; }

    /// <summary>
    /// The built-in values per zone (the starting point was 16 section 5's clone values, judged on renders against 16's
    /// colour script and F1; the record of each value is in 70_look_build.md "Zone look"). Unknown zone: null.
    /// </summary>
    public static FrontRoomsZoneLookProfile Defaults(string zone)
    {
        var p = CreateInstance<FrontRoomsZoneLookProfile>();
        p.name = FrontRoomsZoneLook.ProfilePrefix + zone + " (defaults)";
        p.hideFlags = HideFlags.DontSave;
        p.zone = zone;
        if (!Fill(p, zone))
        {
            if (Application.isPlaying) Destroy(p); else DestroyImmediate(p);
            return null;
        }
        return p;
    }

    /// <summary>Write zone's built-in values into p. False for a zone without built-in values.</summary>
    public static bool Fill(FrontRoomsZoneLookProfile p, string zone)
    {
        if (p == null) return false;
        p.zone = zone;
        switch (zone)
        {
            case "PoolClub":
                // the club entry, lockers and the corridor to the hall (16 section 3): cool-white fluorescent F40 troffers,
                // neutral and a touch cool, no water; F1 turns it cool grey and plain
                p.ambientSky = new Color(.19f, .20f, .20f);
                p.ambientEquator = new Color(.23f, .245f, .245f);
                p.ambientGround = new Color(.32f, .335f, .33f);
                p.fogColor = new Color(.14f, .155f, .16f);
                p.fogDensity = .012f;
                p.fillScale = 1f;
                p.heightFog = false;
                p.heightFogDensity = 0f;
                p.heightFogScale = .7f;
                p.heightFogColor = new Color(.30f, .33f, .34f);
                p.lampLight = new Color(.93f, .97f, 1f);
                p.lampIntensity = 1f;
                p.lampLens = new Color(.93f, .97f, 1f);
                p.nicheLens = new Color(1f, .80f, .58f);
                p.nicheLight = new Color(1f, .80f, .58f);
                p.nicheIntensity = 2.4f;
                p.waterAbsorbScale = 1f;            // the club's water is physical (Q1)
                p.thrownLight = 0f;                 // no wet niches in the club's corridors
                p.contactShade = .7f;               // white tile wainscot: the corners' contact shade a third lighter
                return true;
            case "PoolHall":
                // the pool hall under indirect metal halide (P1-A): white tile, the ceiling lit, cool aqua-grey, a light humid
                // haze on the water; the wet niches warm at the lens, mint in the water
                // floors face the lit ceiling (P1-A), so the sky term is the brightest; cool grey, never saturated teal
                p.ambientSky = new Color(.30f, .33f, .34f);
                p.ambientEquator = new Color(.24f, .27f, .28f);
                p.ambientGround = new Color(.30f, .33f, .33f);
                p.fogColor = new Color(.14f, .17f, .18f);
                p.fogDensity = .009f;
                p.fillScale = 1f;
                p.heightFog = true;
                p.heightFogDensity = .015f;
                p.heightFogScale = .7f;
                p.heightFogColor = new Color(.42f, .48f, .50f);
                p.lampLight = new Color(.90f, .97f, 1f);
                p.lampIntensity = 2.2f;
                p.lampLens = new Color(.90f, .97f, 1f);
                // 2800 K at the lens and in the light: the Submerged stage absorbs along each ray's path in the water, which
                // turns it neutral after about 2 m and mint past 4 m (17 4.3), kept subtle; before it (zone look) the 4 m
                // value (.82, 1, .78) stood in for the path
                p.nicheLens = new Color(1f, .80f, .58f);
                p.nicheLight = new Color(1f, .80f, .58f);
                p.nicheIntensity = .8f;
                p.waterAbsorbScale = 1f;            // the club's basin: physical water (Q1)
                p.thrownLight = 1f;
                p.contactShade = .65f;              // the hall's porcelain: bases, parapets and the coping's foot
                return true;
            case "PoolLevel":
                // the turn and the pool level: daylight fluorescent with no source seen, high key, a faint aqua haze; the
                // ankle water everywhere carries its own thin haze layer
                p.ambientSky = new Color(.27f, .30f, .31f);
                p.ambientEquator = new Color(.28f, .31f, .32f);
                p.ambientGround = new Color(.38f, .41f, .41f);
                p.fogColor = new Color(.20f, .23f, .24f);
                p.fogDensity = .008f;
                p.fillScale = 1f;
                p.heightFog = true;
                p.heightFogDensity = .020f;
                p.heightFogScale = .6f;
                p.heightFogColor = new Color(.52f, .58f, .60f);
                p.lampLight = new Color(.90f, .97f, 1f);
                p.lampIntensity = 1.2f;
                p.lampLens = new Color(.88f, .96f, 1f);
                p.nicheLens = new Color(1f, .80f, .58f);
                p.nicheLight = new Color(1f, .80f, .58f);
                p.nicheIntensity = 2.4f;
                p.waterAbsorbScale = 4f;            // Q1 "wrong": the level's ankle water turquoise (x3-5)
                // its ankle water's moving light is the caustics of its hidden compact lamps (17 7 item 9); a basin of the
                // level with wet niches (scene 4's catch pools and court basins) throws their glow at the physical share
                // (17 4.3, FrontRoomsWaterSubmerged.NicheGlow; 0 until 11-04 hid it under the zone look, 70 12.10)
                p.thrownLight = 1f;
                p.contactShade = .6f;               // all porcelain, high key: the lightest contact shade
                return true;
            case "PoolDark":
                // Red's fixed dark zone (18 section 3; Q4 "follow"): every overhead dead, the wet niches only; the grade's white
                // balance follows the niche lamps (FrontRoomsPost_PoolDark, about 3200 K), so neutral greys here land deep
                // teal and the basin glows aqua: the ambient and haze are near-black neutrals before the grade
                // before the 3200 K balance these warm olives are what the niches' bounce gives; after it they land teal-black
                // (through URP's own white-balance matrix, for example (0.08, 0.088, 0.048) -> about (0.035, 0.095, 0.105))
                // the ground term (what ceilings and the undersides face) stood in for the water's own diffuse glow, the room's
                // main light at night (lifted 1.5x and the walls' term 1.15x on 11-03 22:4x, zl_v6L); since the Submerged
                // stage the glow is the wet niches' thrown light (FrontRoomsWaterSubmerged, per lamp, with the long ripple's
                // caustics), so both terms are back at the bounce alone
                p.ambientSky = new Color(.075f, .083f, .045f);
                p.ambientEquator = new Color(.08f, .088f, .048f);
                p.ambientGround = new Color(.095f, .105f, .058f);
                p.fogColor = new Color(.065f, .07f, .032f);
                p.fogDensity = .014f;
                // the scene's neutral fill would turn navy under the 3200 K balance and light the room evenly, which the dead
                // overheads cannot: a fifth of it stays (darkness is never cover: walls and the Relay still read)
                p.fillScale = .2f;
                p.heightFog = true;
                p.heightFogDensity = .03f;
                p.heightFogScale = .5f;
                p.heightFogColor = new Color(.13f, .155f, .10f);
                p.lampLight = new Color(.90f, .97f, 1f);
                p.lampIntensity = 0f;
                p.lampLens = new Color(.90f, .97f, 1f);
                // 2800 K at the lamp; the water (absorption x3 here, the level's "wrong" water reading deeper, 18 section 3)
                // turns it aqua along each path, and the grade balances to 3200 K ("the water is the light")
                p.nicheLens = new Color(1f, .80f, .58f);
                p.nicheLight = new Color(1f, .80f, .58f);
                p.nicheIntensity = 3.0f;
                p.waterAbsorbScale = 3f;
                // the niches' glow carries the room at night (16 section 3: "ceiling caustics carry the room"): 2.5x the
                // physical share estimated for the stand-in spots (sub_v3 S6 at 1x left the ceiling near black, mean 32)
                p.thrownLight = 2.5f;
                p.contactShade = .8f;               // the dark zone: the niches alone light it, so its corners keep more shade
                return true;
        }
        return false;
    }
}
