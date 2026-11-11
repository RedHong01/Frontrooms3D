// FrontRooms surface: URP physically based shading with the room materials
// projected in world metres. Walls, floors and ceilings take their UVs from the
// world position and the face normal, so a pattern runs continuously across
// every slab and keeps the same printed scale everywhere. Tile sizes are chosen
// to divide the room stream's 192 m origin rebase, so a rebase never shifts a
// pattern. Large-scale wear (discolouration, damp carpet, water streaks under
// the ceiling, dirt along the floor) is sampled at 8 m and 12.8 m in world
// space on top of the tile, which hides the tile repeat.
Shader "FrontRooms/Surface"
{
    Properties
    {
        [MainTexture] _BaseMap ("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        [Normal] _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Normal strength", Range(0, 2)) = 1
        _MaskMap ("Mask (R smoothness, G cavity)", 2D) = "white" {}
        _Smoothness ("Smoothness scale", Range(0, 3)) = 1
        _Metallic ("Metallic", Range(0, 1)) = 0
        _OcclusionStrength ("Cavity strength", Range(0, 1)) = 0.8
        _TileSize ("Tile size in metres (xy)", Vector) = (1, 1, 0, 0)
        [Toggle(_FR_MESH_UV)] _MeshUV ("Use mesh UVs (metres) instead of world projection", Float) = 0

        _MacroMap ("Macro wear (R tone, G dirt, B damp, A streaks)", 2D) = "gray" {}
        _MacroTone ("Macro discolouration", Range(0, 1)) = 0.35
        _MacroDirt ("Macro dirt", Range(0, 1)) = 0.25
        _StainColor ("Stain colour", Color) = (0.42, 0.31, 0.16, 1)
        _StainStrength ("Water stains", Range(0, 1)) = 0
        _WetStrength ("Damp patches (floors)", Range(0, 1)) = 0
        _FloorGrime ("Grime along the floor (walls)", Range(0, 1)) = 0
        _FloorGrimeHeight ("Floor grime height (m)", Float) = 0.35
        _CeilingGrime ("Streaks under the ceiling (walls)", Range(0, 1)) = 0
        _CeilingHeight ("Ceiling height (m)", Float) = 2.9

        [Toggle(_EMISSION)] _UseEmission ("Emission", Float) = 0
        _EmissionMap ("Emission", 2D) = "white" {}
        [HDR] _EmissionColor ("Emission colour", Color) = (0, 0, 0, 1)

        // EXIT sign diffusers (research/exit_sign/10_spec.md §7.2): with this on, emission =
        // dot(field.rgb, _LampField.rgb) * field.a * _EmissionColor (field: R lamp A, G lamp B,
        // B DC pair, A transmission; linear). FrontRoomsExitSign writes _LampField per renderer.
        [Toggle(_FR_LAMP_FIELD)] _UseLampField ("Lamp field (exit-sign diffusers)", Float) = 0
        _LampFieldMap ("Lamp field (R A, G B, B DC, A transmission; linear)", 2D) = "black" {}
        _LampField ("Lamp weights (A, B, DC, unused)", Vector) = (1, 1, 0, 0)

        // Stairwell A (the safe zone, research/stairwell): the stairwell's inside takes only its own
        // lights. With this on, additional punctual lights whose range is over FR_OWN_LIGHT_RANGE (6 m:
        // the map's 10-12 m lamps) are skipped; the main light, ambient and reflections are unchanged.
        [Toggle(_FR_OWN_LIGHTS)] _OwnLights ("Own lights only (skip lights reaching over 6 m)", Float) = 0

        // Door break (research/door_break/50_rethink.md 3.8, production P2): the _FR_DAMAGE onset mask, projected in the
        // leaf's OBJECT space (UV-free, so LOD0/1/2 agree; canonical +X = the burst face, the break's mirror is in the
        // object matrix). A texel turns when its onset (R) <= the material's stage; it carries the push-face dish (G
        // height + the normal map), crushed and lifted veneer (B: lighter, matte) and the fines' wipe (A). Only the door
        // break's stage materials (Door_Veneer_Damage_*, Prop_WoodLaminate_Damage_*) turn it on; off, every other
        // material compiles exactly as before.
        [Toggle(_FR_DAMAGE)] _UseDamage ("Door damage onset mask (door-break stage materials)", Float) = 0
        _FR_DamageMap ("Damage onset (R stage, G height, B fibre, A fines; linear)", 2D) = "black" {}
        [Normal] _FR_DamageNormal ("Damage normal (U along +Z, V along +Y)", 2D) = "bump" {}
        _FR_DamageStage ("Stage shown (0.25 M, 0.5 D1, 0.75 D2, 1 TORN)", Range(0, 1)) = 0
        _FR_DamageRect ("Mask rect in leaf metres (z0, y0, 1/dz, 1/dy)", Vector) = (0.595, 0.8, 2.5, 1.6666667)
        _FR_DamageFines ("Fines colour (linear)", Color) = (0.38, 0.25, 0.13, 1)

        // Zone doors (research/double_doors/20_design.md A6): the leaf-wear mask of a zone pair's leaf, read in mesh-UV metres
        // (the title pair's slab: metres from its min corner, both faces alike, as a double-acting leaf wears), mask uv =
        // (uv - rect.xy) * rect.zw. R grime (albedo darker), G dull, B burnish (smoother), A chip (albedo -> the chip colour).
        // Only the zone pairs' leaf materials (Door_*_L / _R) turn it on; off, every material compiles exactly as before.
        [Toggle(_FR_LEAFWEAR)] _UseLeafWear ("Leaf wear mask (zone door leaves)", Float) = 0
        _FR_LeafWearMap ("Leaf wear (R grime, G dull, B burnish, A chip; linear)", 2D) = "black" {}
        _FR_LeafWearRect ("Mask rect in mesh-UV metres (u0, v0, su, sv)", Vector) = (0, 0, 0.8364701, 0.3789314)
        _FR_LeafWearChip ("Chip colour (linear)", Color) = (0.27, 0.181, 0.117, 1)

        // Print layer (wallpaper "sandwich", research/wallpaper_motion): with it on, the
        // Albedo slot holds the PAPER modulation (linear, see FR_PAPER_*) and the colour
        // comes from the print: frame 0 below, or the global _FR_Print flipbook.
        [Toggle(_FR_PRINT)] _UsePrint ("Print layer (paper x print)", Float) = 0
        _PrintTex ("Print frame 0 (R density, G cream; linear)", 2D) = "black" {}
        _InkGround ("Ink ground", Color) = (0.8235, 0.7608, 0.4863, 1)
        _InkMid ("Ink mid", Color) = (0.6745, 0.6039, 0.3216, 1)
        _InkDeep ("Ink deep", Color) = (0.4627, 0.4157, 0.2039, 1)
        _InkCream ("Ink cream", Color) = (0.8902, 0.8353, 0.5804, 1)
        _PrintAmount ("Print amount", Range(0, 1)) = 1

        // Hung paper (research/wall_material, _FR_PRINT only): what makes the wall read as strips of paper
        // and not as a print on plaster. All static; the print and the cue never see any of it.
        _SheetParams ("Sheet: seam position in the print tile, pattern mismatch mm, lap mm, seam dirt", Vector) = (0.35, 4, 13, 0.5)
        _SeamParams ("Seam: band half-width mm, ridge height mm, hairline mm, open chance", Vector) = (10, 0.6, 0.8, 0.1)
        _StripVar ("Sheet tone: edgeshade, lot + wall tone, gloss, emboss", Vector) = (0.035, 0.04, 0.05, 0.3)
        _WearSheen ("Wear: dirt dull, damp dull, seam gloss, grime height jitter m", Vector) = (0.12, 0.12, 0.12, 0.03)
        [NoScaleOffset] _SubstrateMap ("Wall under the paper (normal, 6 x 3 m)", 2D) = "bump" {}
        _SubstrateScale ("Wall under the paper strength", Range(0, 3)) = 0.8
        _EmbossShade ("Emboss in the tone (lamps are overhead)", Range(0, 8)) = 2
        _SubstrateShade ("Wall relief in the tone (lamps are overhead)", Range(0, 8)) = 1.5

        // Required by URP's shadow/depth passes.
        _Cutoff ("Alpha cutoff", Range(0, 1)) = 0.5
        [HideInInspector] _Cull ("__cull", Float) = 2
        // Wall-match outlet plates set this per draw (FrontRoomsWallFixtureRenderer): the world-planar frame
        // follows the normal's dominant axis. 0 everywhere else, so no material changes.
        [HideInInspector] _FR_PlanarSnap ("Planar frame on the dominant axis", Float) = 0

        // Creature cloth (Hunter B "Night Shift", research/hunter/production_b/10_plan.md section 1.4): metal per texel
        // (mask B x _Metallic) and a fabric sheen at grazing view angles lit by the scene's own lights (mask A x _Sheen).
        // Only the Creature_* materials enable it; every other material is bit-identical.
        [Toggle(_FR_SHEEN)] _UseSheen ("Creature cloth (sheen + per-texel metal)", Float) = 0
        _Sheen ("Sheen strength", Range(0, 1)) = 0

        // Thin translucent laminate (structures G2-b: the slide's SL-A tube, unpigmented fibreglass 8 mm; research/structures_assets
        // 20_research.md A.7.2 and level_space 61 section 2.2, "light and shapes, not detail"). With this on, the light that reaches
        // the surface's BACK face (every lamp with its shadow, and the ambient) comes through the laminate, diffused and tinted, and a
        // lamp straight behind it along the view glows through; the surface stays opaque (no sorting; its depth is there for the
        // planar water and GlassRT, which treats it as a plain Surface: diffuse, no traced mirror). Only Prop_Gelcoat_SLA_* enable
        // it; off, every material is bit-identical. WebGL compiles it out.
        [Toggle(_FR_TRANSLUCENT)] _UseTranslucent ("Thin translucent laminate (back light through it)", Float) = 0
        _TransTint ("Translucent: transmitted colour", Color) = (0.55, 0.85, 0.82, 1)
        _TransParams ("Translucent: amount, wrap, forward glow, glow power", Vector) = (0.6, 0.35, 0.25, 4)

        // Pool tile (research/bathroom_rooms/70_look_build.md, "Tile"; maps from Tools/lookdev/pool_tile/gen_pool_tile.py). With
        // this on, the Albedo slot holds TILE DATA (linear: R tone, the albedo multiplier 0.75 + 0.5 R; G glaze thickness; B grout)
        // and the mask R smoothness, G cavity, B dirt potential, A chips. The colours are the material's: the glaze (_BaseColor)
        // breaking to the body (_EdgeColor) where it thins or a chip took it off, the grout, the dirt as far as _TileWear. Every
        // tile also takes its own shade and set (tilt) from a hash of its cell, so the map's repeat never shows. Only the
        // Prop_PoolTile_* materials enable it; every other material is bit-identical.
        [Toggle(_FR_TILE)] _UseTile ("Pool tile (data map, grout, per-tile shade and tilt)", Float) = 0
        _TileGrid ("Tile: tiles per map (u, v), tilt deg, shade jitter", Vector) = (8, 8, 0.18, 0.004)
        _GroutColor ("Grout colour", Color) = (0.66, 0.70, 0.67, 1)
        _EdgeColor ("Glaze edge (body) colour", Color) = (0.80, 0.81, 0.80, 1)
        _DirtColor ("Dirt colour", Color) = (0.36, 0.33, 0.27, 1)
        _TileWear ("Wear: grout dirt, crazing, chips (0 pristine, 1 club)", Range(0, 1)) = 0
        _TileShade ("Joint relief in the tone (walls; the lamps are overhead)", Range(0, 4)) = 0.6
        _TileLot ("Odd lots: share of off-shade tiles, their shift; z far specular AA ceiling (0 = the look's)", Vector) = (0, 0.035, 0, 0)
        // The tile's real look (research/bathroom_rooms/70_look_build.md section 13; GZ-C's glaze, research/level_space/
        // 73_pool_glaze.md, folded in): read only while the global _FR_TileLook.x is over 0.5 (FrontRoomsPoolTile.SetLook; 0 =
        // Today: the path above, bit-identical). Every tile draws its own shape (a dish, its corners, twist, tilt, a bowed edge,
        // the cushion edge, the struck grout, lippage against its neighbours), set-out (its offset in its joints, now and then a
        // turn), gloss and glaze waviness from a hash of its cell; the environment is box projected and comes in two lobes (the
        // glaze's sharp core in a soft halo); specular anti-aliasing filters the gloss by the normal's change per pixel. The
        // face map holds the waviness only (gen_pool_tile.py make_face). The dish also goes into the tone (section 13.15: the
        // sunk centre a little darker, the raised corners a little lighter, a narrow cavity at the joints; the global
        // _FR_TileTone, FrontRoomsPoolTile.DishTone). WebGL compiles it out.
        [NoScaleOffset] _TileFaceMap ("Tile real look: face map (glaze slopes RG, grout grain BA; linear)", 2D) = "gray" {}
        _TileForm ("Tile real look: shape scale, joint jitter mm, two-lobe glaze, gloss variety", Vector) = (0, 0, 0, 0)
        _TileForm2 ("Tile real look: peel variety, face smoothness, lippage mm, face source (1 cushion + face map)", Vector) = (0, 0.935, 0, 0)
        _TileEdge ("Tile real look: joint mm, cushion radius mm, face corner radius mm, grout depth mm", Vector) = (3, 1.8, 1.2, 0.7)

        // Wet band (any material of this shader; off while _WaterY is below -999, the default): under and just above a water
        // surface at _WaterY (world metres) porous parts (grout, unglazed faces) are darker, a water film evens the rest, a thin
        // bright meniscus sits at the line, and a club's pool shows its scale line. The water stages drive _WaterY.
        _WaterY ("Water height (world m; -1000 = no water)", Float) = -1000
        _WetBand ("Wet band: height above the water m, fade m, darkening, meniscus", Vector) = (0.03, 0.05, 1, 1)
        _WaterLine ("Waterline: scale line strength, its height m, band edge wobble m, unused", Vector) = (0, 0.012, 0.003, 0)

        // Surface detail (research/surface_detail/60_build.md section 2; tools Tools/lookdev/surface_detail): what a made and used
        // surface carries beyond its maps, by kind (_DetailKind.x: 1 painted gypsum board, 2 carpet tile, 3 lay-in ceiling, 4 panel
        // fabric, 5 laminate worksurface, 6 painted steel, 7 vinyl cove base): its sheets, joints and fasteners, lots and tiles laid
        // by hand, the paint's passes and touch-ups, the marks of chairs, shoes, cups and the vacuum, the fine relief of its finish,
        // sheen where cloth catches the light, micro-shadowing in its recesses under the lamps. Only the materials that enable it
        // change; the global _FR_Detail.x = 1 (FrontRoomsSurfaceDetail.Today) gives today's surfaces exactly. WebGL compiles it out.
        [Toggle(_FR_DETAIL)] _UseDetail ("Surface detail (kind terms; global switch FrontRoomsSurfaceDetail.Today)", Float) = 0
        _DetailKind ("Detail: kind, detail map span m, detail normal, micro-shadowing", Vector) = (0, 0.3, 0, 0)
        _DetailA ("Detail A (per kind, 60_build.md 2.3)", Vector) = (0, 0, 0, 0)
        _DetailB ("Detail B (per kind)", Vector) = (0, 0, 0, 0)
        _DetailC ("Detail C (per kind)", Vector) = (0, 0, 0, 0)
        [NoScaleOffset] _DetailMap ("Detail map (RG slopes, B gloss, A tone; linear)", 2D) = "gray" {}
        [NoScaleOffset] _WearMap ("Wear map (per kind; linear)", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "Queue" = "Geometry" }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _BumpScale;
            half _Smoothness;
            half _Metallic;
            half _OcclusionStrength;
            float4 _TileSize;
            half _MacroTone;
            half _MacroDirt;
            half4 _StainColor;
            half _StainStrength;
            half _WetStrength;
            half _FloorGrime;
            float _FloorGrimeHeight;
            half _CeilingGrime;
            float _CeilingHeight;
            half4 _EmissionColor;
            half _Cutoff;
            // Print layer: in every pass (outside any #if) so the layout never changes.
            float4 _PrintTex_ST;
            half4 _InkGround;
            half4 _InkMid;
            half4 _InkDeep;
            half4 _InkCream;
            half _PrintAmount;
            // Hung paper: in every pass, so the layout never changes.
            float4 _SheetParams;
            float4 _SeamParams;
            float4 _StripVar;
            float4 _WearSheen;
            half _SubstrateScale;
            half _SubstrateShade;
            half _EmbossShade;
            // Exit-sign lamp weights: in every pass, so the SRP batcher layout never changes.
            float4 _LampField;
            // Wall-match outlet plates (per-draw block only): world-planar frame on the normal's dominant axis.
            half _FR_PlanarSnap;
            // Door break onset mask (_FR_DAMAGE): in every pass, so the SRP batcher layout never changes.
            half _FR_DamageStage;
            float4 _FR_DamageRect;
            half4 _FR_DamageFines;
            // Creature cloth sheen: in every pass, so the SRP batcher layout never changes.
            half _Sheen;
            // Pool tile and the wet band: in every pass, so the SRP batcher layout never changes.
            float4 _TileGrid;
            half4 _GroutColor;
            half4 _EdgeColor;
            half4 _DirtColor;
            half _TileWear;
            half _TileShade;
            float4 _TileLot;
            // The tile's real look: in every pass, so the SRP batcher layout never changes.
            float4 _TileForm;
            float4 _TileForm2;
            float4 _TileEdge;
            float _WaterY;
            float4 _WetBand;
            float4 _WaterLine;
            // Surface detail: in every pass, so the SRP batcher layout never changes.
            float4 _DetailKind;
            float4 _DetailA;
            float4 _DetailB;
            float4 _DetailC;
            // Zone doors' leaf wear (_FR_LEAFWEAR): in every pass, so the SRP batcher layout never changes.
            float4 _FR_LeafWearRect;
            half4 _FR_LeafWearChip;
            // Thin translucent laminate (_FR_TRANSLUCENT): in every pass, so the SRP batcher layout never changes.
            half4 _TransTint;
            float4 _TransParams;
        CBUFFER_END

        // _BaseMap, _BumpMap and _EmissionMap come from URP's SurfaceInput,
        // which the shadow and depth passes below also rely on.
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
        TEXTURE2D(_MaskMap);      SAMPLER(sampler_MaskMap);
        TEXTURE2D(_MacroMap);     SAMPLER(sampler_MacroMap);
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            // Furniture materials are created at runtime after the room pool
            // is built. Keep both the world-projected and mesh-UV variants in
            // player builds so the runtime Office kit is not stripped to the
            // architectural projection path.
            #pragma multi_compile_local _ _FR_MESH_UV
            #pragma shader_feature_local_fragment _EMISSION
            // Wallpaper print layer (L0_Wallpaper, L0_Wallpaper_Shift, Exit_Wallpaper). Runtime
            // on/off goes through _FR_PrintClock.w, never through this keyword.
            #pragma shader_feature_local_fragment _FR_PRINT
            // Exit-sign lamp field (ExitSign_Diffuser only). Off: emission is the plain map, bit-identical.
            #pragma shader_feature_local_fragment _FR_LAMP_FIELD
            // Stairwell A's inside (Stair_* materials only): its own lights only. Off: UniversalFragmentPBR as before.
            #pragma shader_feature_local_fragment _FR_OWN_LIGHTS
            // Door break stage materials only (production P2): the onset mask in leaf object space. Off: bit-identical.
            #pragma shader_feature_local_fragment _FR_DAMAGE
            // Zone doors' leaf wear (Door_*_L / _R only; research/double_doors 20_design A6). Off: bit-identical.
            #pragma shader_feature_local_fragment _FR_LEAFWEAR
            // Creature cloth (Creature_TwillSpruce, Creature_TapeSilver only). Off: bit-identical.
            #pragma shader_feature_local_fragment _FR_SHEEN
            // Pool tile (Prop_PoolTile_* only). Off: bit-identical.
            #pragma shader_feature_local_fragment _FR_TILE
            // Surface detail (the Office's materials and what they share; 60_build.md section 2). Off: bit-identical.
            #pragma shader_feature_local_fragment _FR_DETAIL
            // Thin translucent laminate (Prop_Gelcoat_SLA_* only; structures G2-b). Off: bit-identical.
            #pragma shader_feature_local_fragment _FR_TRANSLUCENT
            // Look-dev debug views (specular-only, diffuse-only, wear masks, albedo). A global
            // shader_feature: no material enables it, so player builds strip it; the editor
            // compiles it on demand when a tool calls Shader.EnableKeyword("_FR_DEBUG_VIEW").
            #pragma shader_feature_fragment _ _FR_DEBUG_VIEW

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            // LOD cross-fade (tech_art row 7, FrontRoomsLodFade): Unity turns it on only for a renderer whose LODGroup is
            // fading, in this pass and in the shadow, depth, depth-normals and motion-vector passes below; every other draw
            // takes the variant without it, which is the code as it was.
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
        #endif
            // Submerged (research/bathroom_rooms/70_look_build.md, "Submerged"): under a registered body of water the colour
            // comes from the water (absorption, the trapping, the caustics); above it, in its room, the wet niches' thrown
            // light. With no body registered (_FR_SubCount 0: everywhere outside a pool zone) it returns at once.
            #include "Assets/Resources/Rendering/FrontRoomsWaterSubmerged.hlsl"

            // Zone look (research/bathroom_rooms/70_look_build.md, "Zone look"): a height fog over a pool zone's water, set per
            // render context by FrontRoomsZoneLook: x density per metre at the water, y the water's height (world), z 1 / scale
            // height (the layer thins by e over that many metres), w unused; colour linear. x is 0 everywhere else, and the
            // branch then returns the colour untouched, so every other zone renders exactly as before.
            float4 _FR_HeightFog;
            float4 _FR_HeightFogColor;

            half3 FRHeightFog(half3 color, float3 positionWS)
            {
                UNITY_BRANCH
                if (_FR_HeightFog.x <= 0.0)
                    return color;
                float3 cam = _WorldSpaceCameraPos.xyz;
                float len = length(positionWS - cam);
                float k = _FR_HeightFog.z;
                // density(y) = x * exp(-k * max(y - water, 0)), integrated analytically along the view ray
                float ya = max(cam.y - _FR_HeightFog.y, 0.0);
                float yb = max(positionWS.y - _FR_HeightFog.y, 0.0);
                float t = k * (yb - ya);
                float f = abs(t) > 1e-3 ? (1.0 - exp(-t)) / t : 1.0 - 0.5 * t;
                float tau = _FR_HeightFog.x * len * exp(-k * ya) * f;
                return lerp(color, (half3)_FR_HeightFogColor.rgb, (half)saturate(1.0 - exp(-tau)));
            }

            // Wet band (research/bathroom_rooms/70_look_build.md, "Tile"; see _WaterY): Lagarde's porous and film model. Porous
            // parts soak and darken under the water and in the band above it; a water film evens and glosses the band (above the
            // water only: under it the water surface does the reflecting); the meniscus, the 2.7 mm skin the water draws up a
            // wall, catches the light from above; a club's pool leaves a scale line. Off (the default) returns at once.
            // _FR_SubWaterline (FrontRoomsWaterSubmerged.Waterline; zero leaves every term as the material has it) strengthens
            // the line where shallow water would otherwise read as a clean wet floor: water wicked higher up what can soak,
            // more darkening, a brighter meniscus, and the tide mark: the tea-brown stain left at the top of a wet band on
            // paper, plaster and baseboard.
            // waterY: the material's _WaterY, or (Submerged) the height of the body of water the point lies in.
            void FRWetBand(float3 positionWS, float alongM, float3 nGeo, half porous, float waterY, inout half3 albedo, inout half smoothness, inout half3 nTS)
            {
                UNITY_BRANCH
                if (waterY < -999.0)
                    return;
                float above = positionWS.y - waterY;
                // the band's top wanders a little along the wall, where the splashes reached
                float wob = _WaterLine.z * (sin(alongM * 7.3) * 0.6 + sin(alongM * 17.9 + 1.7) * 0.4);
                float4 wl = _FR_SubWaterline;
                float top = _WetBand.x + wl.x;
                half wet = above <= 0.0 ? 1.0h : (half)(1.0 - smoothstep(top + wob, top + _WetBand.y + wob, above));
                half soak = wet * porous * (half)(_WetBand.z + wl.y);
                albedo *= 1.0h - 0.42h * soak;
                half film = above > 0.0 ? wet : 0.0h;
                smoothness = lerp(smoothness, max(smoothness, 0.90h), film * (0.35h + 0.65h * porous));
                nTS = lerp(nTS, half3(0.0h, 0.0h, 1.0h), film * porous * 0.6h);
                half wallF = 1.0h - saturate(abs((half)nGeo.y) * 2.0h);
                // the scale line: a thin rough grey deposit centred a little above the still level (TM 5-662 4-4, "scum ring")
                half sc = (half)(_WaterLine.x * saturate(1.0 - abs(above - _WaterLine.y * 0.35) / max(_WaterLine.y, 1e-4))) * wallF;
                albedo = lerp(albedo, half3(0.40h, 0.39h, 0.34h), sc * 0.45h);
                smoothness = lerp(smoothness, 0.40h, sc * 0.8h);
                // the meniscus, faded by its size on screen so it never shimmers
                float px = max(fwidth(positionWS.y), 1e-5);
                half men = (half)(_WetBand.w * saturate(1.0 - abs(above - 0.0014) / (0.0014 + px)) * saturate(0.0028 / px)) * wallF;
                men = min(men * (1.0h + (half)wl.z), max(men, 1.0h));
                smoothness = lerp(smoothness, 0.97h, men);
                nTS = normalize(lerp(nTS, half3(0.0h, 0.75h, 0.66h), men));
                // the tide mark: 6 mm at the band's dry edge, never thinner than a pixel, only on what soaks
                half tide = (half)(wl.w * saturate(1.0 - abs(above - (top + _WetBand.y + wob)) / max(0.006, px))) * porous * wallF;
                albedo = lerp(albedo, albedo * half3(0.80h, 0.68h, 0.52h), tide);
            }

        #if defined(_FR_TILE)
            // ---------------------------------------------------------------- pool tile
            // The maps are data (see Properties); the material holds the colours. Each tile takes a shade and a set from a hash
            // of its cell. The cell index wraps every 192 m (the room stream's rebase holds whole maps), so a rebase never
            // re-rolls a tile.
            uint FRTileHashU(uint2 c, uint salt)
            {
                uint x = c.x * 73856093u ^ c.y * 19349663u ^ salt * 83492791u;
                x ^= x >> 16; x *= 0x7feb352du; x ^= x >> 15; x *= 0x846ca68bu; x ^= x >> 16;
                return x;
            }

            half FRTileHash(uint2 c, uint salt)
            {
                return (half)((FRTileHashU(c, salt) & 0xffffu) / 65535.0);
            }

            // tile data -> albedo, mask (R smoothness after wear, G cavity, B dirt, A chips), tangent normal, and how porous the
            // texel is (grout, unglazed faces) for the wet band
            void FRTileSurface(float2 uv, float3 nGeo, out half4 albedo, out half4 mask, out half3 nTS, out half porous)
            {
                half4 d = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uv);
                nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);
                float2 cellF = floor(uv * _TileGrid.xy);
                float2 period = max(round(192.0 / max(_TileSize.xy, 1e-3) * _TileGrid.xy), 1.0);
                uint2 cell = (uint2)(cellF - period * floor(cellF / period));
                half grout = d.b;
                half glaze = 1.0h - grout;
                // shade: a small lot jitter on every tile, and now and then a tile from another delivery, bluer or creamier
                half j = (half)_TileGrid.w * (FRTileHash(cell, 1u) * 2.0h - 1.0h);
                half odd = FRTileHash(cell, 2u) < (half)_TileLot.x ? 1.0h : 0.0h;
                half3 oddShift = FRTileHash(cell, 3u) < 0.5h ? half3(-0.55h, 0.0h, 0.75h) : half3(0.45h, 0.2h, -0.65h);
                half3 lot = 1.0h + j + odd * (half)_TileLot.y * oddShift;
                // set: each tile sits at its own small angle, so a lamp's reflection breaks up tile by tile
                half tilt = (half)tan(radians(_TileGrid.z));
                nTS.xy += (half2(FRTileHash(cell, 4u), FRTileHash(cell, 5u)) * 2.0h - 1.0h) * tilt * glaze;
                // colour: the glaze breaking to the body where it thins and where a chip took it off; the grout; the dirt
                half chips = mask.a * _TileWear * glaze;
                half thick = d.g * (1.0h - chips);
                half m = 0.75h + 0.5h * d.r;
                half3 glazeCol = lerp(_EdgeColor.rgb, _BaseColor.rgb, thick) * lot;
                half3 col = lerp(glazeCol, _GroutColor.rgb, grout) * m;
                half dirt = saturate(mask.b * _TileWear);
                col = lerp(col, _DirtColor.rgb, dirt * 0.55h);
                // relief in the tone on walls: cushion edges facing up a little lighter, facing down darker (lamps are overhead)
                half wallF = 1.0h - saturate(abs((half)nGeo.y) * 2.0h);
                col *= 1.0h + _TileShade * nTS.y * wallF * glaze;
                albedo = half4(max(col, 0.0h), 1.0h);
                // a chip is matte bisque; dirt dulls; an old glaze a little
                mask.r = lerp(mask.r, 0.35h, chips) * (1.0h - 0.04h * _TileWear * glaze) - dirt * 0.18h;
                porous = max(grout, saturate((0.55h - mask.r) / 0.4h));
            }
        #endif

        #if defined(_FR_TILE) && !defined(SHADER_API_GLES3) && !defined(SHADER_API_WEBGPU)
            #define FR_TILE_REAL 1
        #else
            #define FR_TILE_REAL 0
        #endif

        #if FR_TILE_REAL
            // ---------------------------------------------------------------- pool tile, the real look (70 section 13)
            // Globals, written by FrontRoomsPoolTile.SetLook / Publish (0 everywhere = today's tile):
            float4 _FR_TileLook;    // x look (0 today, 1 GZ-C as given, 2-4 the dish), y dish depth mm, z dished share, w the Pools levers (0 / 1)
            float4 _FR_TileLook2;   // x tilt sd deg, y twist sd mm, z GZ-C bow lo mm, w GZ-C bow hi mm
            float4 _FR_TileGlaze;   // x core smoothness, y halo smoothness, z halo weight, w box projection (1 on)
            float4 _FR_TileAA;      // x AA screen variance, y AA threshold, z the face map's slope variance (rad^2), w its lod offset (phone 1)
            float4 _FR_TileTone;    // the dish in tone (13.15): x tone per mm of the dish's height at 0.35 m, y the joint's cavity at the cushion's foot, z the dish's falloff with distance (m), w the fade's end (m)
            // Written by FrontRoomsZoneLook (the zone's contact shade, by its weight): x the share of the shared SSAO's darkening
            // taken off the tile (0, unset: the renderer's).
            float4 _FR_ZoneSSAO;
            TEXTURE2D(_TileFaceMap);

            #define FR_TILE_FACE_SPAN 0.6       // the face map's span in metres (gen_pool_tile.py FACE_SPAN_MM)
            #define FR_TILE_FACE_K 14.285714    // its glaze slope code: 0.5 + slope * K (gen_pool_tile.py FACE_K)
            #define FR_TILE_GRAIN_K 0.8333333   // its grout grain code (GRAIN_K)

            uint2 FRTileWrap(float2 cellF, float2 period)
            {
                return (uint2)(cellF - period * floor(cellF / period));
            }

            // four numbers in [0, 1] (8 bits each) from one hash of the cell
            float4 FRTileH8(uint2 c, uint salt)
            {
                uint x = FRTileHashU(c, salt);
                return float4(x & 0xffu, (x >> 8) & 0xffu, (x >> 16) & 0xffu, x >> 24) / 255.0;
            }

            // A tile's set-out (salt 11): its offset in its joints (cells, uniform within +-jitC), now and then a turn (one tile
            // in six, up to 0.25 degrees: a wedge joint), and its height against its neighbours (lippage, unit sd).
            void FRTileSetOut(uint2 c, float2 jitC, float var, out float2 off, out float turn, out float lip)
            {
                float4 h = FRTileH8(c, 11u);
                off = (h.xy * 2.0 - 1.0) * jitC;
                turn = h.w < 0.16 ? (h.w / 0.16 * 2.0 - 1.0) * 0.0043633 * var : 0.0;
                lip = (h.z * 2.0 - 1.0) * 1.7320508;
            }

            // One axis of the hand-set joints. f the position in the cell (0..1), s the nearer joint (-1 left or below, +1 right
            // or above), dOwn this tile's offset here, dN the neighbour's across that joint (cells), jc the joint's width
            // (cells). The face moves with its tile; the joint between the two is stretched or squeezed to its new width (it
            // never closes: offsets stay under half a joint), so the maps are read continuously across it.
            float FRTileJointRemap(float f, float s, float dOwn, float dN, float jc)
            {
                float a = s < 0.0 ? -0.5 * jc + dN : 1.0 - 0.5 * jc + dOwn;
                float b = s < 0.0 ? 0.5 * jc + dOwn : 1.0 + 0.5 * jc + dN;
                float m0 = s < 0.0 ? -0.5 * jc : 1.0 - 0.5 * jc;
                bool inJ = s < 0.0 ? f < b : f > a;
                return inJ ? m0 + (f - a) / max(b - a, 1e-5) * jc : f - dOwn;
            }

            // The tile's shape over its face as slopes: dh/dx, dh/dy in mm per unit of x, y (-1..1 across the face; halfMM mm
            // per unit), from the shape hashes (salts 13-15). The dish looks (Red, 11-04 16:5x: a dish, the centre sunk and the
            // four corners up): on most tiles (a few near flat, a few domed), shallow or deep, a round bowl or a flat floor with
            // steep corners, the corners raised more, a twist (two corners up), one tile in three with one edge bowed, a tilt.
            // GZ-C as given (look 1, 73 section 7): its bow (domed U(lo, hi) on 80 %, dished at 0.65 x that on 20 %), saddle, tilt.
            float2 FRTileShapeSlope(uint2 c, float2 xy, float halfMM)
            {
                float4 a = FRTileH8(c, 13u);
                float4 b = FRTileH8(c, 14u);
                float4 t = FRTileH8(c, 15u);
                float x = xy.x, y = xy.y;
                // tilt: mm at the edge, sd _FR_TileLook2.x degrees (two uniforms: triangular, sd 1 after x 2.449)
                float tilt = tan(radians(_FR_TileLook2.x)) * halfMM * 2.4494897;
                float2 g = float2(t.x + t.y - 1.0, t.z + t.w - 1.0) * tilt;
                // twist (saddle): w x y, sd _FR_TileLook2.y mm at the corners
                float w = (b.x + b.y - 1.0) * 2.4494897 * _FR_TileLook2.y;
                g += w * float2(y, x);
                if (_FR_TileLook.x < 1.5)
                {
                    // GZ-C: h = bow (1 - r^2 / 75^2), r in mm from the centre
                    float bow = lerp(_FR_TileLook2.z, _FR_TileLook2.w, a.x);
                    bow = a.y < 0.2 ? -0.65 * bow : bow;
                    g += (-2.0 * bow * halfMM * halfMM / 5625.0) * float2(x, y);
                }
                else
                {
                    // the dish: h = D s^p + C x^2 y^2, s = (x^2 + y^2) / 2 (0 at the centre, 1 at the corners)
                    float D = _FR_TileLook.y * lerp(0.45, 1.55, a.x);
                    float share = _FR_TileLook.z;
                    D = a.y < share ? D : (a.y < share + (1.0 - share) * 0.6 ? 0.15 * D : -0.5 * D);
                    float p = lerp(1.0, 2.4, a.z * a.z);
                    float s = 0.5 * (x * x + y * y);
                    g += D * p * pow(max(s, 1e-4), p - 1.0) * float2(x, y);
                    float C = D * 0.4 * a.w;
                    g += 2.0 * C * float2(x * y * y, y * x * x);
                    if (b.z < 0.33)
                    {
                        // one edge bowed: its middle up or down, fading to the opposite edge
                        float E = (b.w * 2.0 - 1.0) * 0.45 * _FR_TileLook.y;
                        float e = floor(b.z * 12.0);
                        bool alongX = e < 1.5;
                        float sg = (e == 0.0 || e == 2.0) ? 1.0 : -1.0;
                        float q = alongX ? x : y;
                        float r = alongX ? y : x;
                        float k = 0.5 * (1.0 + sg * r);
                        float dq = -2.0 * q * E * k * k;
                        float dr = E * (1.0 - q * q) * k * sg;
                        g += alongX ? float2(dq, dr) : float2(dr, dq);
                    }
                }
                return g;
            }

            // ---------------------------------------------------------------- the dish in tone (70 section 13.15)
            // Red, 11-04 16:5x: the dish, the centre sunk and the four corners up. Half a millimetre of dish tilts the face by a
            // fraction of a degree: a lamp's reflection shows it, soft light does not (scene 4, 11-05 04:3x: the white face read
            // the same in every look). VISUAL_STANDARD 1.2: relief goes into the tone as well as the normal. So the same dish also
            // sets the tone: a little darker where it is sunk, a little lighter where it is raised (soft overhead light sees less
            // of a hollow: an occlusion of the sky, not a shadow), and the cushion and the grout's sides darken in a narrow band
            // at the joints, so each tile reads as a slightly pillowed, slightly sunk piece. Both from the tile's own shape (the
            // hashes and numbers of FRTileShapeSlope and the cushion), so tone and normal agree tile by tile; both fade to the
            // tile's mean tone with the view distance (no quilt of pillows on a far wall, on any screen) and are filtered by the
            // pixel's footprint (no dashes, no moire). Off (_FR_TileTone.xy 0: FrontRoomsPoolTile.DishTone 0, or Today) none of
            // this runs.

            // the joint's cavity at the joint's middle (the struck grout's most open point, 1 at the foot) and its tail onto the
            // face (in cushion radii past the foot)
            #define FR_TILE_TONE_GMID 0.6
            #define FR_TILE_TONE_TAIL 1.2

            // The tile's form as height over its face (mm, + toward the viewer) less its mean over the face: the dish, its raised
            // corners and a bowed edge (GZ-C: its bow), from the hashes and numbers of FRTileShapeSlope. Tilt and twist are left
            // out: a plane and a saddle hold no hollow.
            float FRTileShapeHeight(uint2 c, float2 xy, float halfMM)
            {
                float4 a = FRTileH8(c, 13u);
                float4 b = FRTileH8(c, 14u);
                float x = xy.x, y = xy.y;
                float r2 = x * x + y * y;
                if (_FR_TileLook.x < 1.5)
                {
                    // GZ-C: h = bow (1 - r^2 / 75^2), r = |xy| halfMM mm; r^2 averages 2/3 halfMM^2 over the face
                    float bow = lerp(_FR_TileLook2.z, _FR_TileLook2.w, a.x);
                    bow = a.y < 0.2 ? -0.65 * bow : bow;
                    return -bow * halfMM * halfMM / 5625.0 * (r2 - 0.6666667);
                }
                // the dish: D (s^p - its mean) + C (x^2 y^2 - 1/9), s = (x^2 + y^2) / 2; the mean of s^p over the face as a fit
                // (1/3 at p 1, 0.121 at p 2.4; within 0.0008)
                float D = _FR_TileLook.y * lerp(0.45, 1.55, a.x);
                float share = _FR_TileLook.z;
                D = a.y < share ? D : (a.y < share + (1.0 - share) * 0.6 ? 0.15 * D : -0.5 * D);
                float p = lerp(1.0, 2.4, a.z * a.z);
                float s = 0.5 * r2;
                float h = D * (pow(max(s, 1e-6), p) - 1.0 / (1.2907 + p * (0.8611 + 0.8550 * p)));
                h += D * 0.4 * a.w * (x * x * y * y - 0.1111111);
                if (b.z < 0.33)
                {
                    // one edge bowed: E (1 - q^2) k^2, its mean 2/9 E
                    float E = (b.w * 2.0 - 1.0) * 0.45 * _FR_TileLook.y;
                    float e = floor(b.z * 12.0);
                    bool alongX = e < 1.5;
                    float sg = (e == 0.0 || e == 2.0) ? 1.0 : -1.0;
                    float q = alongX ? x : y;
                    float k = 0.5 * (1.0 + sg * (alongX ? y : x));
                    h += E * ((1.0 - q * q) * k * k - 0.2222222);
                }
                return h;
            }

            // The joint's cavity across one edge, as its integral from the joint's middle (x = -halfJ) to x (mm into the face,
            // negative in the joint): over the struck grout it rises from FR_TILE_TONE_GMID at the middle to 1 at the cushion's
            // foot (the inside corner sees the least sky), stays 1 over the glaze's fillet to d0, then falls over the cushion
            // onto the face (exp, scale wT).
            float FRTileBandI(float x, float halfJ, float d0, float wT)
            {
                float u = clamp(x + halfJ, 0.0, halfJ);
                float I = FR_TILE_TONE_GMID * u + (1.0 - FR_TILE_TONE_GMID) * u * u * u / (3.0 * halfJ * halfJ);
                I += clamp(x, 0.0, d0);
                return I + wT * (1.0 - exp(-max(x - d0, 0.0) / wT));
            }

            // The cavity box-filtered over the pixel's footprint across the edge (L mm), exactly for this profile (the neighbour's
            // side of the joint is its mirror): as the joint shrinks on screen it keeps its darkening's integral, so it neither
            // breaks into dashes nor changes the wall's mean.
            float FRTileBand(float x, float L, float halfJ, float d0, float wT)
            {
                // a window reaching past the joint's middle takes the neighbour's side as this one's mirror
                float a = x - 0.5 * L;
                bool mirror = a < -halfJ;
                float Ia = FRTileBandI(mirror ? -2.0 * halfJ - a : a, halfJ, d0, wT);
                return (FRTileBandI(x + 0.5 * L, halfJ, d0, wT) - (mirror ? -Ia : Ia)) / L;
            }

            // The tone of a tile texel, a multiplier: 1 - A m + w (t - A (band - m)). t the dish (_FR_TileTone.x per mm of
            // FRTileShapeHeight at 0.35 m, falling with the distance; on the glaze, scaled by the kind's shape scale as the slope
            // is), A the cavity at the foot (_FR_TileTone.y; the cushion kinds only), m its mean over the tile, w the fade with
            // the view distance: past it a texel takes the tile's mean tone, so the wall's mean is the same near and far.
            float FRTileTone(uint2 c, float2 lmm, float2 hf, float2 pitchMM, float glaze, float2 dUVdx, float2 dUVdy, float hRad, float var, float3 positionWS)
            {
                // the fade with the view distance: whole to 0.35 x _FR_TileTone.w, gone at _FR_TileTone.w (m)
                float dist = length(GetCameraPositionWS() - positionWS);
                float wView = 1.0 - smoothstep(0.35 * _FR_TileTone.w, max(_FR_TileTone.w, 0.01), dist);
                // the cushion as FRTileSurfaceReal draws it: the tile's own radius, the grout's depth at the foot; the cavity's
                // integral across one edge (to the face's middle) times the cell's perimeter over its area is its mean
                float A = _TileForm2.w > 0.5 ? _FR_TileTone.y : 0.0;
                float r = _TileEdge.y * (1.0 + (lerp(0.85, 1.3, hRad) - 1.0) * var);
                float gF = _TileEdge.w;
                float d0 = r - sqrt(max(r * r - (r - gF) * (r - gF), 0.0));
                float wT = FR_TILE_TONE_TAIL * r;
                float halfJ = max(0.5 * _TileEdge.x, 1e-3);
                float across = halfJ * (FR_TILE_TONE_GMID + (1.0 - FR_TILE_TONE_GMID) / 3.0) + d0 + wT;
                float bandMean = across * 2.0 * (pitchMM.x + pitchMM.y) / (pitchMM.x * pitchMM.y);
                float tone = 1.0 - A * bandMean;
                // past the fade the texel is the tile's mean: nothing more to do
                UNITY_BRANCH
                if (wView > 0.0)
                {
                    // the pixel's footprint along u and v (mm), as the cushion's fade takes it
                    float2 pxMM = 1000.0 * float2(length(float2(dUVdx.x, dUVdy.x)) * _TileSize.x, length(float2(dUVdx.y, dUVdy.y)) * _TileSize.y);
                    float t = 0.0;
                    if (_TileForm.x > 0.0)
                    {
                        // the dish is one wave per tile: a gradient over a whole tile is what the eye sees least well up close
                        // (a tile at 0.35 m spans 24 degrees) and best at 5-15 m, so its amount is set at the hero's 0.35 m and
                        // falls with the distance over _FR_TileTone.z m (half at 0.35 + z); its only filter is the tile's size in
                        // pixels (gone below 3)
                        float tilePx = min(pitchMM.x, pitchMM.y) / max(max(pxMM.x, pxMM.y), 1e-3);
                        float wNear = _FR_TileTone.z / max(_FR_TileTone.z + max(dist - 0.35, 0.0), 1e-3);
                        t = _FR_TileTone.x * wNear * FRTileShapeHeight(c, lmm / hf, hf.x) * _TileForm.x * glaze * saturate((tilePx - 3.0) / 5.0);
                    }
                    float band = bandMean;
                    if (A > 0.0)
                    {
                        // the distance into the face (mm, negative in the joint; the face's rounded corners) and the footprint
                        // across the edge
                        float rc = min(_TileEdge.z, hf.x);
                        float2 q = abs(lmm) - (hf - rc);
                        float dIn = -(length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - rc);
                        float2 od = (q.x > 0.0 && q.y > 0.0) ? normalize(q) : (q.x > q.y ? float2(1.0, 0.0) : float2(0.0, 1.0));
                        band = FRTileBand(max(dIn, -halfJ), max(dot(abs(od), pxMM), 0.05), halfJ, d0, wT);
                    }
                    tone += wView * (t - A * (band - bandMean));
                }
                return tone;
            }

            // tile data -> albedo, mask, tangent normal and porosity (as FRTileSurface), the two-lobe weight of this texel and the
            // face map's slope variance its mip has averaged away (rad^2, for the roughness)
            void FRTileSurfaceReal(float2 uv, float3 nGeo, float3 positionWS, out half4 albedo, out half4 mask, out half3 nTS, out half porous, out half lobes, out float lostVar)
            {
                float var = _FR_TileLook.w;
                float2 grid = max(_TileGrid.xy, 1.0);
                float2 pitchM = max(_TileSize.xy, 1e-3) / grid;
                float2 pitchMM = pitchM * 1000.0;
                float2 jc = _TileEdge.x / pitchMM;
                float2 jitC = (_TileForm.y * var) / pitchMM;
                if (grid.y < 1.5) jitC.y = 0.0;
                float2 dUVdx = ddx(uv);
                float2 dUVdy = ddy(uv);
                float2 g = uv * grid;
                // the setter's lines drift a little over metres (periods 3.84 and 4.8 m divide 192 m)
                g += float2(sin(g.y * pitchM.y * 1.3089969), sin(g.x * pitchM.x * 1.6362461)) * jitC * 0.9;
                float2 cellF = floor(g);
                float2 f = g - cellF;
                float2 period = max(round(192.0 / pitchM), 1.0);
                float2 side = float2(f.x < 0.5 ? -1.0 : 1.0, f.y < 0.5 ? -1.0 : 1.0);
                uint2 c0 = FRTileWrap(cellF, period);
                uint2 cU = FRTileWrap(cellF + float2(side.x, 0.0), period);
                uint2 cV = FRTileWrap(cellF + float2(0.0, side.y), period);
                float2 o0, oU, oV;
                float t0, tU, tV, l0, lU, lV;
                FRTileSetOut(c0, jitC, var, o0, t0, l0);
                FRTileSetOut(cU, jitC, var, oU, tU, lU);
                FRTileSetOut(cV, jitC, var, oV, tV, lV);
                // a turn moves a tile's u offset along v and its v offset along u (small angles, in cells)
                float ry = pitchM.y / pitchM.x;
                float uR = FRTileJointRemap(f.x, side.x, o0.x - t0 * (f.y - 0.5) * ry, oU.x - tU * (f.y - 0.5) * ry, jc.x);
                float vR = FRTileJointRemap(f.y, side.y, o0.y + t0 * (f.x - 0.5) / ry, oV.y + tV * (f.x - 0.5) / ry, jc.y);
                float2 uvS = (cellF + float2(uR, vR)) / grid;
                half4 d = SAMPLE_TEXTURE2D_GRAD(_BaseMap, sampler_BaseMap, uvS, dUVdx, dUVdy);
                mask = SAMPLE_TEXTURE2D_GRAD(_MaskMap, sampler_MaskMap, uvS, dUVdx, dUVdy);
                half grout = d.b;
                half glaze = 1.0h - grout;
                float4 h16 = FRTileH8(c0, 16u);
                float4 h17 = FRTileH8(c0, 17u);
                // the face: mm from its centre, its half size, and -1..1 across it
                float2 lmm = (float2(uR, vR) - 0.5) * pitchMM;
                float2 hf = max(0.5 * pitchMM - 0.5 * _TileEdge.x, 1.0);
                float2 slope = 0.0;
                UNITY_BRANCH
                if (_TileForm.x > 0.0)
                    slope = FRTileShapeSlope(c0, lmm / hf, hf.x) / hf * (_TileForm.x * (float)glaze);
                lostVar = 0.0;
                half3 n;
                UNITY_BRANCH
                if (_TileForm2.w > 0.5)
                {
                    // the cushion edge and the struck grout (the set's geometry, _TileEdge, as the maps draw it): the face rounds
                    // over r mm into the joint, the glaze thickening at its foot; the grout g mm under the face there, dipping
                    // 0.85 g more at the joint's middle; each tile its own radius, and a proud edge (lippage) rolls further
                    float r = _TileEdge.y * (1.0 + (lerp(0.85, 1.3, h17.y) - 1.0) * var);
                    float rc = min(_TileEdge.z, hf.x);
                    float2 q = abs(lmm) - (hf - rc);
                    float dIn = -(length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - rc);
                    float2 outDir = (q.x > 0.0 && q.y > 0.0) ? normalize(q) : (q.x > q.y ? float2(1.0, 0.0) : float2(0.0, 1.0));
                    outDir *= float2(lmm.x < 0.0 ? -1.0 : 1.0, lmm.y < 0.0 ? -1.0 : 1.0);
                    float gF = _TileEdge.w;
                    float halfJ = max(0.5 * _TileEdge.x, 1e-3);
                    // the cushion and the joint are a few millimetres: where a pixel spans them (past about 1.5 m) their slopes
                    // average out, as the maps' mips average today's cushion; point-sampled they would alias into dark and white
                    // joints. Per axis, from the pixel's footprint along u and v (mm).
                    float2 pxMM = 1000.0 * float2(length(float2(dUVdx.x, dUVdy.x)) * _TileSize.x, length(float2(dUVdx.y, dUVdy.y)) * _TileSize.y);
                    float2 kRes = saturate((2.0 * r / max(pxMM, 1e-3) - 1.0) / 3.0);
                    float s;
                    if (dIn >= 0.0)
                    {
                        float rd = r - min(dIn, r);
                        float shoulder = rd / max(sqrt(max(r * r - rd * rd, 0.0)), 0.06);
                        float d0 = r - sqrt(max(r * r - (r - gF) * (r - gF), 0.0));
                        float lipStep = (abs(outDir.x) * (l0 - lU) + abs(outDir.y) * (l0 - lV)) * _TileForm2.z * var;
                        float roll = 2.0 * lipStep / r * saturate(1.0 - dIn / r);
                        s = (dIn < r ? max(shoulder + roll, 0.15 * shoulder) : 0.0) * smoothstep(d0 - 0.12, d0 + 0.22, dIn);
                    }
                    else
                        s = 1.7 * gF * saturate((dIn + halfJ) / halfJ) / halfJ;
                    slope -= outDir * s * kRes;
                    // the glaze's waviness through this tile's own window of the face map, its strength the tile's own
                    float2 kF = _TileSize.xy / FR_TILE_FACE_SPAN;
                    float2 uvF = uvS * kF + h17.zw;
                    half4 fm = SAMPLE_TEXTURE2D_GRAD(_TileFaceMap, sampler_BaseMap, uvF, dUVdx * kF, dUVdy * kF);
                    float peel = 1.0 + (h17.x * 2.0 - 1.0) * _TileForm2.x * var;
                    float faceW = (float)glaze * smoothstep(0.0, r, dIn);
                    slope += ((float2)fm.rg - 0.5) * (peel * faceW / FR_TILE_FACE_K);
                    slope += ((float2)fm.ba - 0.5) * ((float)grout / FR_TILE_GRAIN_K);
                    // the slope variance the face map's mip has averaged away (gen_pool_tile.py: lost by mip), for the roughness
                    float2 tx = dUVdx * kF * 1024.0;
                    float2 ty = dUVdy * kF * 1024.0;
                    float lod = 0.5 * log2(max(max(dot(tx, tx), dot(ty, ty)), 1e-8)) + _FR_TileAA.w;
                    lostVar = _FR_TileAA.z * peel * peel * faceW * smoothstep(-0.3, 3.8, lod);
                    n = (half3)normalize(float3(-slope, 1.0));
                }
                else
                {
                    // the set's own normal map (trims, ribs, the mosaic's and the deck's faces) with the shape on it
                    half3 nm = UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(_BumpMap, sampler_BumpMap, uvS, dUVdx, dUVdy), _BumpScale);
                    float2 sm = -(float2)nm.xy / max((float)nm.z, 0.2);
                    n = (half3)normalize(float3(-(sm + slope), 1.0));
                }
                // colour as FRTileSurface; with the Pools levers a tone step per tile on top of the lot jitter
                half j = (half)_TileGrid.w * (FRTileHash(c0, 1u) * 2.0h - 1.0h) + (half)((h16.w * 2.0 - 1.0) * 0.006 * var);
                half odd = FRTileHash(c0, 2u) < (half)_TileLot.x ? 1.0h : 0.0h;
                half3 oddShift = FRTileHash(c0, 3u) < 0.5h ? half3(-0.55h, 0.0h, 0.75h) : half3(0.45h, 0.2h, -0.65h);
                half3 lot = 1.0h + j + odd * (half)_TileLot.y * oddShift;
                half chips = mask.a * _TileWear * glaze;
                half thick = d.g * (1.0h - chips);
                half m = 0.75h + 0.5h * d.r;
                half3 glazeCol = lerp(_EdgeColor.rgb, _BaseColor.rgb, thick) * lot;
                half3 col = lerp(glazeCol, _GroutColor.rgb, grout) * m;
                half dirt = saturate(mask.b * _TileWear);
                col = lerp(col, _DirtColor.rgb, dirt * 0.55h);
                half wallF = 1.0h - saturate(abs((half)nGeo.y) * 2.0h);
                col *= 1.0h + _TileShade * n.y * wallF * glaze;
                // the dish in tone (section 13.15; off when FrontRoomsPoolTile.DishTone is 0: the arithmetic above, untouched)
                UNITY_BRANCH
                if (_FR_TileTone.x + _FR_TileTone.y > 0.0)
                    col *= (half)FRTileTone(c0, lmm, hf, pitchMM, (float)glaze, dUVdx, dUVdy, h17.y, var, positionWS);
                albedo = half4(max(col, 0.0h), 1.0h);
                // gloss per tile (the Pools levers): a little brighter or duller, one in seven a hazy glaze
                half dg = (half)(((h16.x * 2.0 - 1.0) * _TileForm.w - (h16.y < 0.14 ? lerp(0.04, 0.09, h16.z) : 0.0)) * var);
                mask.r = lerp(mask.r, 0.35h, chips) * (1.0h - 0.04h * _TileWear * glaze) - dirt * 0.18h + dg * glaze;
                porous = max(grout, saturate((0.55h - mask.r) / 0.4h));
                nTS = n;
                lobes = (half)_TileForm.z * glaze;
            }

            // Specular anti-aliasing (Tokuyoshi-Kaplanyan 2021, core RP's ProjectedSpaceGeometricNormalFiltering): addVar is the
            // normal's screen-space variance (x 2) plus what the face map's mips averaged away, clamped at the threshold. A material
            // may raise the threshold with _TileLot.z (0 everywhere but scene 4's far-AA copies: max(y, 0) = y, today's arithmetic):
            // a small round seen far away (a nosing's r 8 nose at 8-15 m) then spreads its highlight over its pixel instead of
            // aliasing into a dotted line (structures_assets G1 review FIX 5); up close its normal turns little per pixel.
            half FRTileAA(half smoothness, float addVar)
            {
                float r = PerceptualSmoothnessToRoughness(smoothness);
                float cap = max(_FR_TileAA.y, _TileLot.z);
                float r2 = saturate(r * r + min(addVar, cap * cap));
                return (half)RoughnessToPerceptualSmoothness(sqrt(r2));
            }

            // a lobe of the same BRDF at another smoothness (as InitializeBRDFDataDirect sets one; GZ-C's FRGlazeLobe)
            BRDFData FRTileLobe(BRDFData b, half smoothness)
            {
                b.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(smoothness);
                b.roughness = max(PerceptualRoughnessToRoughness(b.perceptualRoughness), HALF_MIN_SQRT);
                b.roughness2 = max(b.roughness * b.roughness, HALF_MIN);
                b.grazingTerm = saturate(smoothness + b.reflectivity);
                b.normalizationTerm = b.roughness * half(4.0) + half(2.0);
                b.roughness2MinusOne = b.roughness2 - half(1.0);
                return b;
            }

            // the probe in the reflected direction, box projected for the tile (no URP keyword: the probe's own flag decides)
            half3 FRTileEnvSample(half3 R, float3 positionWS, half perceptualRoughness, float2 uvSS)
            {
                half3 dir = _FR_TileGlaze.w > 0.5 ? BoxProjectedCubemapDirection(R, positionWS, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax) : R;
                return GlossyEnvironmentReflection(dir, positionWS, perceptualRoughness, 1.0h, uvSS);
            }

            // The environment (URP's GlobalIllumination, 17.6): on glaze two lobes of the same probe, a sharp core inside a soft
            // halo (hazy gloss, Barla 2018; GZ-C 73 5.1), each with its own environment BRDF so the energy stays at the glaze's F0;
            // unglazed tile one lobe. The cavity occludes the probe by direction (Lagarde 2014), not flatly.
            half3 FRTileGI(BRDFData brdf, half core, half halo, half haloW, half3 bakedGI, half occlusion, float3 positionWS, half3 normalWS, half3 viewDirectionWS, float2 uvSS)
            {
                half3 R = reflect(-viewDirectionWS, normalWS);
                half NoV = saturate(dot(normalWS, viewDirectionWS));
                half fresnelTerm = Pow4(1.0h - NoV);
                BRDFData bc = FRTileLobe(brdf, core);
                half3 spec = FRTileEnvSample(R, positionWS, bc.perceptualRoughness, uvSS) * EnvironmentBRDFSpecular(bc, fresnelTerm) * (1.0h - haloW);
                if (haloW > 0.001h)
                {
                    BRDFData bh = FRTileLobe(brdf, halo);
                    spec += FRTileEnvSample(R, positionWS, bh.perceptualRoughness, uvSS) * EnvironmentBRDFSpecular(bh, fresnelTerm) * haloW;
                }
                half specOcc = saturate(PositivePow(NoV + occlusion, exp2(-16.0h * bc.roughness - 1.0h)) - 1.0h + occlusion);
                return bakedGI * brdf.diffuse * occlusion + spec * specOcc;
            }

            // micro-shadowing from the cavity (Brinck-Maximov, The Technical Art of Uncharted 4), at half strength: the grout's
            // recess darkens under a grazing lamp and opens under a frontal one
            half FRTileMicroShadow(half NdotL, half ao)
            {
                half a = lerp(1.0h, ao, 0.5h);
                return saturate(NdotL + 2.0h * a * a - 1.0h);
            }

            // UniversalFragmentPBR (URP 17.6 Lighting.hlsl) for the real look: the lamps' lobe filtered (specular AA), the zone's
            // contact shade, the two-lobe box-projected environment (FRTileGI), micro-shadowing on every light.
            half4 FRTilePBR(InputData inputData, SurfaceData surfaceData, half lobes, float addVar)
            {
                bool specularHighlightsOff = false;
                half scale = saturate(surfaceData.smoothness / max((half)_TileForm2.y, 0.05h));
                half sAA = FRTileAA(surfaceData.smoothness, addVar);
                half core = lerp(sAA, FRTileAA(saturate(scale * (half)_FR_TileGlaze.x), addVar), lobes);
                half halo = FRTileAA(saturate(scale * (half)_FR_TileGlaze.y), addVar);
                half haloW = (half)_FR_TileGlaze.z * lobes;
                surfaceData.smoothness = sAA;
                BRDFData brdfData;
                InitializeBRDFData(surfaceData, brdfData);
            #if defined(DEBUG_DISPLAY)
                half4 debugColor;
                if (CanDebugOverrideOutputColor(inputData, surfaceData, brdfData, debugColor))
                    return debugColor;
            #endif
                BRDFData brdfDataClearCoat = CreateClearCoatBRDFData(surfaceData, brdfData);
                half4 shadowMask = CalculateShadowMask(inputData);
                AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
            #if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT)
                // the zone's contact shade: white porcelain bounces too much light for the shared SSAO's dark corners
                UNITY_BRANCH
                if (_FR_ZoneSSAO.x > 0.0)
                {
                    AmbientOcclusionFactor raw = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);
                    half keep = 1.0h - (half)saturate(_FR_ZoneSSAO.x);
                    aoFactor.indirectAmbientOcclusion = min(1.0h - keep * (1.0h - raw.indirectAmbientOcclusion), surfaceData.occlusion);
                    aoFactor.directAmbientOcclusion = 1.0h - keep * (1.0h - raw.directAmbientOcclusion);
                }
            #endif
                uint meshRenderingLayers = GetMeshRenderingLayer();
                Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
                MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);
                LightingData lightingData = CreateLightingData(inputData, surfaceData);
                lightingData.giColor = FRTileGI(brdfData, core, halo, haloW, inputData.bakedGI, aoFactor.indirectAmbientOcclusion,
                                                inputData.positionWS, inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV);
                half ao = surfaceData.occlusion;
            #ifdef _LIGHT_LAYERS
                if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
            #endif
                    lightingData.mainLightColor = LightingPhysicallyBased(brdfData, brdfDataClearCoat, mainLight,
                                                                          inputData.normalWS, inputData.viewDirectionWS,
                                                                          surfaceData.clearCoatMask, specularHighlightsOff)
                                                * FRTileMicroShadow(saturate(dot(inputData.normalWS, mainLight.direction)), ao);
            #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
            #if USE_CLUSTER_LIGHT_LOOP
                [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                    Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
            #ifdef _LIGHT_LAYERS
                    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            #endif
                        lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
                                                                                      inputData.normalWS, inputData.viewDirectionWS,
                                                                                      surfaceData.clearCoatMask, specularHighlightsOff)
                                                            * FRTileMicroShadow(saturate(dot(inputData.normalWS, light.direction)), ao);
                }
            #endif
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
            #ifdef _LIGHT_LAYERS
                    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            #endif
                        lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
                                                                                      inputData.normalWS, inputData.viewDirectionWS,
                                                                                      surfaceData.clearCoatMask, specularHighlightsOff)
                                                            * FRTileMicroShadow(saturate(dot(inputData.normalWS, light.direction)), ao);
                LIGHT_LOOP_END
            #endif
            #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                lightingData.vertexLightingColor += inputData.vertexLighting * brdfData.diffuse;
            #endif
            #if REAL_IS_HALF
                return min(CalculateFinalColor(lightingData, surfaceData.alpha), HALF_MAX);
            #else
                return CalculateFinalColor(lightingData, surfaceData.alpha);
            #endif
            }
        #endif

        #if !defined(SHADER_API_GLES3) && !defined(SHADER_API_WEBGPU)
            // structures_scale_terms (the game visual line's structures_assets P18; 30_plan rows S3-06, S4-08, ask 4): WebGL compiles it out
            #define FR_SCALE 1
        #else
            #define FR_SCALE 0
        #endif
        #if FR_SCALE
            // Global, written by FrontRoomsStructuresAssets in scene 4 only (unset = 0 everywhere else, so none of the scale terms
            // runs and the frame is today's, bit for bit): x Red's ask 4 (0 off, the default: the paper is Red's pick; 1 a lot step
            // per wall; 2 lot steps and a butt join each roll length on walls taller than one roll), y 1 = the lay-in T-bar drawn as
            // a footprint line in tall rooms (scene 4's NEW), z one roll's length in metres (6.4: 7 yd), w unused.
            float4 _FR_ScaleTerms;
        #endif

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            #if FR_SCALE
                // scene 4's per-wall value (FrontRoomsStructuresMesh UV1, NEW only: x the wall's lot 0..1, 0 = not a wall; y the
                // wall's foot, metres); every other mesh reads zeros
                float2 wallUV     : TEXCOORD1;
            #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 tangentWS  : TEXCOORD2;
                float2 uv         : TEXCOORD3;
                half4 fogAndVertexLight : TEXCOORD4;
            #if FR_SCALE
                float2 wall       : TEXCOORD6;
            #endif
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 5);
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = n.normalWS;
                o.tangentWS = float4(n.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                o.uv = input.uv;
            #if FR_SCALE
                o.wall = input.wallUV;
            #endif
                half fog = ComputeFogFactor(p.positionCS.z);
                o.fogAndVertexLight = half4(fog, VertexLighting(p.positionWS, n.normalWS));
                OUTPUT_SH(o.normalWS, o.vertexSH);
                return o;
            }

            // World-planar frame: floors/ceilings use X/Z, walls run along the
            // wall with V up. B = cross(N, T) keeps the normal map unmirrored.
            void PlanarFrame(float3 positionWS, float3 n, out float2 uvMetres, out float3 t, out float3 b)
            {
                t = abs(n.y) > 0.5 ? float3(1, 0, 0) : normalize(cross(float3(0, 1, 0), n));
                b = cross(n, t);
                uvMetres = float2(dot(positionWS, t), dot(positionWS, b));
            }

            // The world axis closest to n. A papered- or painted-over outlet plate (a few mm proud, its field domed by
            // 1-5 degrees, bevelled edges) projects the wall's paper through the wall's own frame with it: the frame is
            // dotted with the absolute world position, so any tilt of n would move the paper by metres (outlets 40_final.md).
            float3 SnapToAxis(float3 n)
            {
                float3 a = abs(n);
                if (a.y >= a.x && a.y >= a.z) return float3(0, n.y >= 0 ? 1 : -1, 0);
                if (a.x >= a.z) return float3(n.x >= 0 ? 1 : -1, 0, 0);
                return float3(0, 0, n.z >= 0 ? 1 : -1);
            }

        #if defined(_FR_DETAIL) && !defined(SHADER_API_GLES3) && !defined(SHADER_API_WEBGPU)
            #define FR_DETAIL 1
        #else
            #define FR_DETAIL 0
        #endif

        #if FR_DETAIL
            // ---------------------------------------------------------------- surface detail (60_build.md section 2)
            // Global, written by FrontRoomsSurfaceDetail (unset = 0, 0: the revised surfaces at the full tier): x 1 = today's surfaces
            // exactly (the switch); y 1 = the reduced tier (phones: no detail map fetch, no micro-shadowing).
            float4 _FR_Detail;
            TEXTURE2D(_DetailMap);  SAMPLER(sampler_DetailMap);
            TEXTURE2D(_WearMap);

            #define FR_DETAIL_K 4.0     // the detail map's slope code: 0.5 + slope * K (gen_office_detail.py SLOPE_K)

            struct FRDetailOut
            {
                half micro;         // micro-shadowing strength (0 off)
                half sheen;         // cloth sheen weight (0 off)
                half3 sheenColor;
            };

            uint FRDetailHashU(uint3 c, uint salt)
            {
                uint x = c.x * 73856093u ^ c.y * 19349663u ^ c.z * 83492791u ^ salt * 2654435761u;
                x ^= x >> 16; x *= 0x7feb352du; x ^= x >> 15; x *= 0x846ca68bu; x ^= x >> 16;
                return x;
            }

            // four numbers in [0, 1] (8 bits each) from one hash
            float4 FRDetailH8(uint3 c, uint salt)
            {
                uint x = FRDetailHashU(c, salt);
                return float4(x & 0xffu, (x >> 8) & 0xffu, (x >> 16) & 0xffu, x >> 24) / 255.0;
            }

            // an index that repeats every n (cells of a world grid wrap with the 192 m rebase)
            uint FRDetailWrap(float i, float n)
            {
                return (uint)(i - n * floor(i / n));
            }

            // smooth 1-D value noise (0..1)
            float FRDetailNoise1(float x, uint seed)
            {
                float i = floor(x), f = x - i;
                f = f * f * (3.0 - 2.0 * f);
                uint k = (uint)(int)i;
                float a = (FRDetailHashU(uint3(k, seed, 7u), 3u) & 0xffffu) / 65535.0;
                float b = (FRDetailHashU(uint3(k + 1u, seed, 7u), 3u) & 0xffffu) / 65535.0;
                return lerp(a, b, f);
            }

            // One id per flat wall: axis, sign and the plane's offset in 0.25 m classes, 192 m periodic (as the paper's FRWallId).
            uint FRDetailWallId(float3 p, float3 n)
            {
                float3 a = abs(n);
                uint axis = a.x >= a.z ? 0u : 1u;
                uint pos = (a.x >= a.z ? n.x : n.z) >= 0.0 ? 1u : 0u;
                int plane = (int)round(dot(p, n) * 4.0);
                plane = ((plane % 768) + 768) % 768;
                return (uint)plane * 4u + axis * 2u + pos;
            }

            // A line of width w (metres) centred at distance 0, faded to its area mean once it is under a pixel (px metres).
            float FRDetailLine(float d, float w, float px)
            {
                return saturate((0.5 * w + 0.5 * px - abs(d)) / px) * saturate(w / px);
            }

            // The mip for a map of texel size texel (metres) seen at px metres per pixel. The path takes no derivative at all: an
            // explicit ddx in the program changes how the hardware takes the implicit ones elsewhere on curved meshes, and the switch
            // must leave today's frame exact (60_build.md 2.4).
            float FRDetailLod(float px, float texel)
            {
                return max(log2(max(px, 1e-6) / texel), 0.0);
            }

            // the object's own lot (kit pieces: every desk, panel and pedestal a separate draw)
            float4 FRDetailObjectHash(uint salt)
            {
                float3 o = float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23);
                return FRDetailH8((uint3)(int3)floor(o * 4.0 + 8192.0), salt);
            }

            // ---- fine relief: the detail map (paint stipple, laminate suede), RNM over the map's normal, its gloss and tone; faded
            // where a texel is under a third of a pixel (the mips would flatten it anyway; this keeps its gloss from sparkling)
            void FRDetailFine(float2 m, float px, inout half3 albedo, inout half smoothness, inout half3 nTS)
            {
                float span = max(_DetailKind.y, 1e-3);
                half4 d = SAMPLE_TEXTURE2D_LOD(_DetailMap, sampler_DetailMap, m / span, FRDetailLod(px, span / 512.0));
                half fade = (half)saturate(1.5 - px / (span / 512.0) / 3.0);
                half2 sl = (d.rg - 0.5h) * (half)(1.0 / FR_DETAIL_K) * (half)_DetailKind.z * fade;
                nTS = BlendNormalRNM(normalize(nTS), normalize(half3(-sl, 1.0h)));
                smoothness += (d.b - 0.5h) * 0.4h * fade;
                albedo *= 1.0h + (d.a - 0.5h) * 0.4h * fade;
            }

            // ---- 1: painted gypsum board (Office_Wall). Boards 1.2 m wide hung on end from a joint somewhere on each wall; every joint
            // taped and feathered about 300 mm wide (the paint flashes over it, most often duller, and its crown bends the sheen);
            // screws on the studs every 0.4 m along and 0.3 m up, each spotted (a few popped); the paint rolled in passes about 0.6 m
            // wide with lap lines where wet met dry, touched up here and there from another can, cut in by brush under the ceiling;
            // chair backs burnish a band at 0.72-0.98 m; shoes, chair casters and the vacuum mark the foot (_WearMap: the strip 4.8 x
            // 0.6 m, v 0 at the floor). The lamps are overhead, so the knockdown's relief goes into the tone as well.
            // A: joint sheen, joint tone, fastener visibility, knockdown relief in the tone
            // B: pass sheen, touch-up share, touch-up sheen, cut-in band m      C: chair band, foot marks, unused, unused
            void FRDetailBoard(float3 p, float3 nGeo, float2 m, float px, half3 nMap, inout half3 albedo, inout half smoothness, inout half3 nTS)
            {
                uint wall = FRDetailWallId(p, nGeo);
                float4 hw = FRDetailH8(uint3(wall, 0u, 0u), 31u);
                float y = p.y;
                // the boards and their joints
                const float BW = 1.2;
                float u = m.x + hw.x * BW;
                float bi = floor(u / BW);
                float fu = u - bi * BW;
                float side = fu < 0.5 * BW ? -1.0 : 1.0;
                float dj = side < 0.0 ? fu : BW - fu;
                uint jid = FRDetailWrap(bi + (side > 0.0 ? 1.0 : 0.0), 160.0);
                float4 hj = FRDetailH8(uint3(wall, jid, 1u), 32u);
                float4 hj2 = FRDetailH8(uint3(wall, jid, 7u), 38u);
                float feather = 0.13 + 0.05 * hj.x;
                // the finish coat is heavier in places along the joint (two octaves); some joints hardly show at all
                float coat = 0.35 + 0.65 * (0.6 * FRDetailNoise1(y * 1.3 + hj.y * 37.0, jid * 7u + wall)
                                          + 0.4 * FRDetailNoise1(y * 4.1 + hj2.x * 19.0, jid * 7u + wall + 1u));
                coat *= 0.3 + 0.7 * hj2.y;
                float jb = (1.0 - smoothstep(0.35 * feather, feather, dj)) * coat;
                half flash = hj.z < 0.78 ? -(0.55h + 0.45h * (half)hj.w) : (0.5h + 0.5h * (half)hj.w);
                smoothness += (half)_DetailA.x * flash * (half)jb;
                albedo *= 1.0h + (half)(_DetailA.y * jb);
                // the crown (0.45-0.85 mm over the feather): h = c (1 - x^2)^2, its slope across the wall in the normal
                float crown = (0.00045 + 0.0004 * hj.y) * coat;
                float x = saturate(dj / feather);
                nTS.x += (half)(-side * 4.0 * crown * x * (1.0 - x * x) / feather);
                // the paper tape's edges, 26 mm each side of the joint: a faint ridge in the sheen at the hero distance
                smoothness -= (half)(0.025 * FRDetailLine(dj - 0.026, 0.002, px) * coat);

                // fasteners: the studs under the board (0.4 m apart; the board's edges sit on studs too), a screw every 0.3 m up
                uint bid = FRDetailWrap(bi, 160.0);
                float4 hb = FRDetailH8(uint3(wall, bid, 2u), 33u);
                float si = round(fu / 0.4);
                float y0 = 0.15 + 0.06 * hb.x;
                float ri = round((y - y0) / 0.3);
                // each screw where the hand put it: up to 40 mm off its row, a few mm off the stud; most spots never show
                float4 hs = FRDetailH8(uint3(wall, bid * 4u + (uint)si, FRDetailWrap(ri, 64.0) + 3u), 34u);
                float2 d2 = float2(fu - si * 0.4 - (hs.x - 0.5) * 0.012, y - (y0 + 0.3 * ri) - (hs.w - 0.5) * 0.08);
                float r = length(d2);
                float spotR = 0.016 + 0.01 * frac(hs.x * 7.0);
                float vis = (hs.y < 0.55 ? 0.0 : (hs.y < 0.9 ? 0.3 : 0.8)) * _DetailA.z;
                vis *= step(0.05, y0 + 0.3 * ri) * step(y0 + 0.3 * ri, _CeilingHeight - 0.05);
                float spot = (1.0 - smoothstep(spotR * 0.45, spotR + 0.5 * px, r)) * vis;
                smoothness -= (half)(0.03 * spot);
                albedo *= 1.0h + (half)(0.004 * spot);
                // a popped screw (one in thirty): a 12 mm dome 0.4 mm proud, its hairline ring; the lamps are overhead, so its
                // upper side is lighter in the tone too
                float pop = (hs.z > 0.967 ? 1.0 : 0.0) * _DetailA.z * step(0.05, y0 + 0.3 * ri);
                float pr = 0.006;
                float k = saturate(r / pr);
                float2 sl = (r > 1e-5 && r < pr) ? d2 / r * (0.0008 * k / pr) : float2(0.0, 0.0);
                nTS.xy += (half2)(sl * pop);
                albedo *= 1.0h + (half)(sl.y * pop * 0.6);
                albedo *= 1.0h - (half)(0.12 * FRDetailLine(r - pr, 0.0008, px) * pop);

                // the paint: rolled in passes, each a little different; lap lines where wet met dry (glossier: twice the film)
                float pw = 0.55 + 0.15 * hw.y;
                float uu = m.x + 0.07 * (FRDetailNoise1(y * 0.9 + hw.z * 13.0, wall * 3u + 11u) - 0.5) + hw.w * pw;
                float pIdx = floor(uu / pw);
                float fp = uu - pIdx * pw;
                float dl = min(fp, pw - fp);
                float4 hp = FRDetailH8(uint3(wall, FRDetailWrap(pIdx, 1024.0), 4u), 35u);
                float lap = (1.0 - smoothstep(0.012, 0.05, dl)) * hp.x * hp.x * 1.5;
                float runOut = smoothstep(0.25, 0.7, y) * (1.0 - smoothstep(_CeilingHeight - 0.5, _CeilingHeight - 0.1, y));
                smoothness += (half)(_DetailB.x * ((hp.y - 0.5) * 0.6 + lap * runOut));

                // touch-ups: a patch rolled over a repair or a mark, from another can (mostly glossier, now and then flatter;
                // a shade off), a soft rounded rectangle in its 1.2 x 0.9 m cell
                float2 tc = float2(m.x / 1.2, y / 0.9);
                float2 ci = floor(tc);
                float4 ht = FRDetailH8(uint3(wall, FRDetailWrap(ci.x, 1024.0), FRDetailWrap(ci.y, 16.0) + 8u), 36u);
                float4 ht2 = FRDetailH8(uint3(wall, FRDetailWrap(ci.x, 1024.0), FRDetailWrap(ci.y, 16.0) + 40u), 37u);
                float2 hsz = float2(0.06 + 0.18 * ht.y, 0.05 + 0.13 * ht.z);
                float2 cc = (ci + 0.25 + 0.5 * ht2.xy) * float2(1.2, 0.9);
                float2 q = abs(float2(m.x, y) - cc) - hsz + 0.03;
                float sdf = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - 0.03;
                float tf = 0.02 + 0.04 * ht2.z;
                float tu = (1.0 - smoothstep(-0.3 * tf, tf, sdf)) * (ht.x < _DetailB.y ? 1.0 : 0.0) * step(0.15, cc.y);
                smoothness += (half)(_DetailB.z * (ht2.w < 0.7 ? 1.0 : -0.7) * tu);
                albedo *= 1.0h + (half)((ht.w - 0.5) * 0.4 * _DetailB.z * tu);

                // cut in by brush under the ceiling: a band 50-90 mm deep, its own sheen, brush ridges along it
                float cutD = _DetailB.w * (0.75 + 0.5 * FRDetailNoise1(m.x * 2.3 + hw.y * 9.0, wall + 77u));
                float cut = smoothstep(_CeilingHeight - cutD - 0.01, _CeilingHeight - cutD + 0.01, y);
                smoothness += (half)(cut * (hw.z < 0.5 ? 0.025 : -0.02));
                float brush = FRDetailNoise1(y * 700.0 + FRDetailNoise1(m.x * 3.0, wall + 5u) * 9.0, wall + 9u) - 0.5;
                nTS.y += (half)(cut * brush * 0.03 * saturate(0.0025 / px - 0.3));

                // chair backs against the wall: burnished and greyed in patches at 0.72-0.98 m
                float chair = smoothstep(0.70, 0.78, y) * (1.0 - smoothstep(0.92, 1.0, y))
                            * smoothstep(0.55, 0.85, FRDetailNoise1(m.x * 1.3 + hw.x * 21.0, wall + 13u)) * _DetailC.x;
                smoothness += (half)(0.05 * chair);
                albedo *= 1.0h - (half)(0.03 * chair);

                // the foot: black rubber from heels, casters and the vacuum's bumper, grey abrasion
                UNITY_BRANCH
                if (y < 0.62)
                {
                    half4 w = SAMPLE_TEXTURE2D_LOD(_WearMap, sampler_DetailMap, float2(m.x / 4.8, y / 0.6), FRDetailLod(px, 4.8 / 2048.0));
                    half black = saturate(w.r * (half)_DetailC.y);
                    half grey = saturate(w.g * (half)_DetailC.y);
                    albedo = lerp(albedo, albedo * 0.18h + 0.02h, black * 0.85h);
                    smoothness += 0.06h * black - 0.06h * grey;
                    albedo *= 1.0h + 0.06h * grey;
                }

                // the knockdown's relief in the tone (its upper sides a little lighter), faded in with distance: at the hero distance
                // the lamps still draw it; past 2 m a tilt this small no longer reads, so the tone carries it
                albedo *= 1.0h + (half)(_DetailA.w * saturate(px / 0.0025)) * nMap.y;
            }

            // ---- 2: carpet tile (Office_Carpet: 0.6 m tiles laid quarter-turn; the map holds 2 x 2). Each tile its own dye lot,
            // now and then a newer replacement or a stain (the atlas's quadrants 0-1); the pile lies one way per tile, so the
            // checker lightens looking along the lay and darkens against it, as you move; seams open here and there; a corner lifts.
            // A: lot tone, lot hue, pile sheen, seam variation   B: newer share, newer lift, stain share, stain strength
            void FRDetailCarpet(float3 p, float2 m, float px, float3 t, float3 b, inout half3 albedo, inout half3 nTS)
            {
                const float TS = 0.6;
                float2 cf = floor(m / TS);
                uint2 c = uint2(FRDetailWrap(cf.x, 320.0), FRDetailWrap(cf.y, 320.0));
                float2 f = m / TS - cf;
                float4 h = FRDetailH8(uint3(c, 0u), 41u);
                half lot = (half)(_DetailA.x * (h.x * 2.0 - 1.0));
                half hue = (half)(_DetailA.y * (h.y * 2.0 - 1.0));
                albedo *= half3(1.0h + lot + hue, 1.0h + lot, 1.0h + lot - hue);
                half fresh = h.z < _DetailB.x ? 1.0h : 0.0h;
                albedo = lerp(albedo, albedo * (1.0h + (half)_DetailB.y) + 0.004h, fresh);
                // the lay: the map's quarter-turn (tiles with cx + cy even lie along v, odd along u), a few degrees off per tile
                float ang = ((c.x + c.y) % 2u == 0u ? 1.5707963 : 0.0) + (h.w - 0.5) * 0.35;
                float2 lay = float2(cos(ang), sin(ang));
                float3 V = GetWorldSpaceNormalizeViewDir(p);
                float2 v2 = float2(dot(V, t), dot(V, b));
                float vl = length(v2);
                half along = vl > 1e-4 ? (half)dot(-v2 / vl, lay) : 0.0h;
                albedo *= 1.0h + (half)_DetailA.z * along * (half)saturate(vl * 1.4) * (1.0h - 0.5h * fresh);
                // seams: each edge open or tight
                float2 e = min(f, 1.0 - f) * TS;
                bool ex = e.x < e.y;
                float edgeI = ex ? cf.x + (f.x > 0.5 ? 1.0 : 0.0) : cf.y + (f.y > 0.5 ? 1.0 : 0.0);
                float4 he = FRDetailH8(uint3(FRDetailWrap(edgeI, 320.0), FRDetailWrap(ex ? cf.y : cf.x, 320.0), ex ? 5u : 6u), 42u);
                float open = he.x * he.x;
                albedo *= 1.0h - (half)(FRDetailLine(min(e.x, e.y), 0.0008 + 0.0014 * open, px) * (0.2 + 0.5 * open) * _DetailA.w);
                // a corner lifted 2-3 mm (one tile in forty): it catches the light
                float4 hc = FRDetailH8(uint3(c, 2u), 43u);
                float2 corner = float2(hc.y < 0.5 ? 0.0 : 1.0, frac(hc.y * 2.0) < 0.5 ? 0.0 : 1.0);
                float lift = (hc.x < 0.025 ? 1.0 : 0.0) * (1.0 - smoothstep(0.02, 0.09, length(f - corner) * TS));
                nTS.xy += (half2)(normalize(corner - f + 1e-4) * 0.035 * lift);
                albedo *= 1.0h + (half)(0.03 * lift);
                // a stain from the atlas (quadrant 0 or 1), turned by quarter turns
                float4 hs = FRDetailH8(uint3(c, 3u), 44u);
                UNITY_BRANCH
                if (hs.x < _DetailB.z)
                {
                    float2 g = f - 0.5;
                    if (hs.y > 0.5) g = float2(-g.y, g.x);
                    if (frac(hs.y * 2.0) > 0.5) g = -g;
                    float2 auv = (g + 0.5) * 0.5 + float2(hs.z < 0.5 ? 0.0 : 0.5, 0.0);
                    half4 st = SAMPLE_TEXTURE2D_LOD(_WearMap, sampler_DetailMap, auv, FRDetailLod(px, TS / 512.0));
                    half body = st.r * (half)_DetailB.w;
                    albedo = lerp(albedo, albedo * half3(0.62h, 0.50h, 0.38h), body);
                    albedo *= 1.0h - 0.22h * st.g * (half)_DetailB.w;
                }
            }

            // ---- 3: lay-in ceiling (Office_Ceiling: 0.6 m mineral-fibre panels on a 24 mm T-bar grid; the map holds 2 x 2). Each
            // panel lies on the grid by itself: its own age; a sag (the middle drops 1-5 mm, more where it is damp); a small tilt;
            // one in fourteen replaced (whiter, cleaner); one in fifty pushed up at one edge (the plenum's dark shows along it); its
            // edges soiled by the air that leaks round them; now and then a water stain that stops short of its edges (the atlas's
            // quadrants 2-3). The grid's flange bows between hangers, so its sheen wanders along it.
            // A: age tone, replaced share, sag mm, tilt deg   B: edge soil, lifted share, stain share, stain strength
            // C: flange bow deg, replaced lift (0 = 0.035), unused, unused
            void FRDetailCeiling(float3 p, float2 m, float px, half damp, inout half3 albedo, inout half3 nTS)
            {
                const float TS = 0.6, HALF_TBAR = 0.0119;
                float2 cf = floor(m / TS);
                uint2 c = uint2(FRDetailWrap(cf.x, 320.0), FRDetailWrap(cf.y, 320.0));
                float2 g = m / TS - cf - 0.5;
                float2 e = (0.5 - abs(g)) * TS;
                float de = min(e.x, e.y);
                float tile = smoothstep(HALF_TBAR - 0.5 * px, HALF_TBAR + 0.5 * px, de);
                float4 h = FRDetailH8(uint3(c, 0u), 51u);
                float4 h2 = FRDetailH8(uint3(c, 1u), 52u);
                half fresh = h.y < _DetailA.y ? 1.0h : 0.0h;
                half age = (half)(h.x * 2.0 - 1.0) * (1.0h - fresh);
                half3 tone = half3(1.0h + (half)_DetailA.x * age, 1.0h + (half)_DetailA.x * age, 1.0h + (half)_DetailA.x * age * 1.7h);
                half newLift = (half)(_DetailC.y > 0.0 ? _DetailC.y : 0.035);
                tone = lerp(tone, half3(1.0h + newLift, 1.0h + newLift, 1.0h + 1.4h * newLift), fresh);
                albedo *= lerp(half3(1.0h, 1.0h, 1.0h), tone, (half)tile);
                // sag toward the room: h = s (1 - 4 gx^2)(1 - 4 gy^2); the normal tilts out from the middle; and a small tilt
                float sag = _DetailA.z * 0.001 * (0.2 + 0.45 * h.z + 0.6 * damp) * (1.0 - 0.6 * fresh);
                float ax = 1.0 - 4.0 * g.x * g.x, ay = 1.0 - 4.0 * g.y * g.y;
                float2 grad = sag * float2(-8.0 * g.x * ay, -8.0 * g.y * ax) / TS;
                float2 tl = (h2.xy * 2.0 - 1.0) * tan(radians(_DetailA.w));
                nTS.xy += (half2)((tl - grad) * tile);
                // pushed up at one edge: tilted up toward it, the plenum's dark along it
                float4 h3 = FRDetailH8(uint3(c, 2u), 53u);
                if (h3.x < _DetailB.y)
                {
                    float sideI = floor(h3.y * 4.0);
                    float2 dir = sideI < 1.0 ? float2(1.0, 0.0) : sideI < 2.0 ? float2(-1.0, 0.0) : sideI < 3.0 ? float2(0.0, 1.0) : float2(0.0, -1.0);
                    float lift = 0.004 + 0.006 * h3.z;
                    nTS.xy += (half2)(dir * (lift / TS) * tile);
                    float dEdge = (0.5 - dot(g, dir)) * TS - HALF_TBAR;
                    float gapW = lift * 0.7;
                    albedo *= 1.0h - (half)(0.8 * FRDetailLine(dEdge - 0.5 * gapW, gapW, px) * tile);
                }
                // the edges soiled by the air leaking round them
                float soil = (1.0 - smoothstep(HALF_TBAR, HALF_TBAR + 0.02 + 0.025 * h2.z, de)) * tile * (0.3 + 0.7 * h2.w) * _DetailB.x;
                albedo *= 1.0h - 0.12h * (half)soil;
                // a water stain, more often where it is damp
                float4 hs = FRDetailH8(uint3(c, 3u), 54u);
                UNITY_BRANCH
                if (hs.x < _DetailB.z * (0.5 + damp))
                {
                    float2 q = g;
                    if (hs.y > 0.5) q = float2(-q.y, q.x);
                    if (frac(hs.y * 2.0) > 0.5) q = -q;
                    float2 auv = (q + 0.5) * 0.5 + float2(hs.z < 0.5 ? 0.0 : 0.5, 0.5);
                    half4 st = SAMPLE_TEXTURE2D_LOD(_WearMap, sampler_DetailMap, auv, FRDetailLod(px, TS / 512.0));
                    half k = (half)(_DetailB.w * tile);
                    albedo = lerp(albedo, albedo * half3(0.93h, 0.86h, 0.70h), st.r * k);
                    albedo = lerp(albedo, albedo * half3(0.70h, 0.58h, 0.42h), st.g * k);
                }
                // the flange bows between the hangers: a tilt across the bar that wanders along it
                float onBar = 1.0 - tile;
                bool barAlongV = e.x < e.y;
                float wob = (FRDetailNoise1((barAlongV ? m.y : m.x) * 1.6, FRDetailWrap(barAlongV ? cf.x : cf.y, 320.0) + 61u) - 0.5) * 2.0;
                float wt = tan(radians(_DetailC.x)) * wob * onBar;
                nTS.xy += (half2)(barAlongV ? float2(wt, 0.0) : float2(0.0, wt));
            }

            // ---- 4: panel fabric (Prop_FabricCubicle, Office_Fabric): every panel its own lot (bought in different years, faded
            // differently); the bottom kicked and vacuumed darker; the cloth's sheen at grazing angles (FRDetailPBR, o.sheen).
            // A: sheen, unused, lot tone, lot hue   B: bottom soil, its height m, unused, unused
            void FRDetailFabric(float3 p, inout half3 albedo, inout FRDetailOut o)
            {
                float4 h = FRDetailObjectHash(71u);
                half lot = (half)(_DetailA.z * (h.x * 2.0 - 1.0));
                half hue = (half)(_DetailA.w * (h.y * 2.0 - 1.0));
                albedo *= half3(1.0h + lot - hue, 1.0h + lot, 1.0h + lot + hue);
                albedo *= 1.0h - (half)(_DetailB.x * (1.0 - smoothstep(0.04, max(_DetailB.y, 0.05), p.y)));
                half luma = dot(albedo, half3(0.2126h, 0.7152h, 0.0722h));
                o.sheen = (half)_DetailA.x;
                o.sheenColor = lerp(albedo, half3(luma, luma, luma), 0.4h) * 1.8h + 0.015h;
            }

            // ---- 5: laminate worksurface (Prop_LaminateBeige: HPL in a suede finish, ~10 GU at 60 deg): the suede in the detail map;
            // on the top (faces up) a desk's life from _WearMap (1 m, from its own place per desk): scratches where things slide,
            // burnished patches where forearms rest, a dust film where nothing moves, cup rings; each desk its own lot.
            // A: unused, scratches, burnish, dust   B: wear map span m, cup rings, lot tone, unused
            void FRDetailLaminate(float3 nGeo, float2 m, float px, inout half3 albedo, inout half smoothness)
            {
                float4 h = FRDetailObjectHash(81u);
                albedo *= 1.0h + (half)(_DetailB.z * (h.x * 2.0 - 1.0));
                half top = (half)saturate((nGeo.y - 0.6) / 0.3);
                UNITY_BRANCH
                if (top > 0.0h)
                {
                    float span = max(_DetailB.x, 0.1);
                    half4 w = SAMPLE_TEXTURE2D_LOD(_WearMap, sampler_DetailMap, (m + h.yz * span) / span, FRDetailLod(px, span / 1024.0));
                    half scr = w.r * (half)_DetailA.y * top;
                    smoothness -= 0.10h * scr;
                    albedo *= 1.0h + 0.05h * scr;
                    smoothness += (half)_DetailA.z * w.g * top;
                    half dust = w.b * (half)_DetailA.w * top;
                    smoothness -= dust;
                    albedo = lerp(albedo, half3(0.56h, 0.54h, 0.50h), dust * 0.5h);
                    half ring = w.a * (half)_DetailB.y * top;
                    smoothness -= 0.08h * ring;
                    albedo *= 1.0h - 0.07h * ring;
                }
            }

            // ---- 6: painted steel (Prop_SteelPutty: pedestals, panel trims and posts, filing cabinets): each piece its own lot; the
            // desk wear map (1 m, from its own place) for scratches into the paint (darker), hands' burnish and dust; kicks at the foot.
            // A: wear map span m, scratches, burnish, dust   B: lot tone, kick height m, kick strength, unused
            void FRDetailSteel(float3 p, float2 m, float px, inout half3 albedo, inout half smoothness)
            {
                float4 h = FRDetailObjectHash(82u);
                albedo *= 1.0h + (half)(_DetailB.x * (h.x * 2.0 - 1.0));
                float span = max(_DetailA.x, 0.1);
                half4 w = SAMPLE_TEXTURE2D_LOD(_WearMap, sampler_DetailMap, (m + h.yz * span) / span, FRDetailLod(px, span / 1024.0));
                half scr = w.r * (half)_DetailA.y;
                smoothness -= 0.12h * scr;
                albedo *= 1.0h - 0.10h * scr;
                smoothness += (half)_DetailA.z * w.g;
                half dust = w.b * (half)_DetailA.w;
                smoothness -= dust;
                albedo = lerp(albedo, half3(0.55h, 0.53h, 0.50h), dust * 0.35h);
                half kick = (half)((1.0 - smoothstep(0.03, max(_DetailB.y, 0.05), p.y)) * _DetailB.z);
                albedo *= 1.0h - 0.25h * kick * (half)(0.5 + 0.5 * FRDetailNoise1(m.x * 23.0 + h.w * 17.0, 83u));
                smoothness -= 0.1h * kick;
            }

            // ---- 7: vinyl cove base (Cove_Base: the map's 0.10 m box, 6 mm proud) drawn as what it is, 4 in x 1/8 in vinyl with a
            // 1/2 in toe: the toe curving out onto the floor at the bottom (it catches the overhead light; a contact line under it),
            // the thin top edge rounding back to the wall (a highlight line), 4 ft lengths butted (a hairline), the satin sheen
            // wandering with the adhesive under it and the strip's own wave, shoes' and the vacuum's marks (the wall's foot strip,
            // so they run on across the top), dust on its top.
            // A: unused, unused, unused, top m   B: marks, top dust, sheen variety, wave deg
            void FRDetailCove(float3 p, float3 nGeo, float2 m, float px, inout half3 albedo, inout half smoothness, inout half cavity, inout half3 nTS)
            {
                half front = (half)(1.0 - saturate(abs(nGeo.y) * 2.0));
                half topF = (half)saturate((nGeo.y - 0.5) * 2.0);
                float y = p.y;
                uint wall = FRDetailWallId(p, nGeo);
                float4 hw = FRDetailH8(uint3(wall, 0u, 0u), 91u);
                // the toe (bottom 12.7 mm) turns out onto the floor, up to 60 degrees; the top 2.5 mm rounds back to the wall
                const float TOE = 0.0127;
                float tz = saturate(1.0 - y / TOE);
                float topY = max(_DetailA.w, 0.02);
                float tt = saturate((y - (topY - 0.0025)) / 0.0025) * saturate(0.0025 / px);
                float ang = (1.05 * tz * tz + 1.2 * tt) * front;
                half ca = (half)cos(ang), sa = (half)sin(ang);
                nTS = half3(nTS.x, nTS.y * ca + nTS.z * sa, -nTS.y * sa + nTS.z * ca);
                float cl = 1.0 - smoothstep(0.0, 0.0025 + px, y);
                albedo *= 1.0h - 0.5h * (half)cl * front;
                cavity *= 1.0h - 0.4h * (half)cl * front;
                // 4 ft lengths butted: a hairline, one end a hair proud
                float u = m.x + hw.x * 1.2;
                float sIdx = floor(u / 1.2);
                float fu = u - sIdx * 1.2;
                float4 hs = FRDetailH8(uint3(wall, FRDetailWrap(sIdx, 160.0), 1u), 92u);
                albedo *= 1.0h - (half)(0.6 * FRDetailLine(min(fu, 1.2 - fu), 0.0004 + 0.0008 * hs.x, px)) * front;
                albedo *= 1.0h + (half)((hs.y - 0.5) * 0.04 * (1.0 - smoothstep(0.0, 0.05, fu))) * front;
                // the satin sheen wanders with the adhesive under it; the strip waves a little (seen in the sheen at grazing)
                smoothness += (half)(_DetailB.z * (FRDetailNoise1(u * 3.7 + hw.y * 11.0, wall + 3u) - 0.5) * 2.0) * front;
                nTS.x += (half)(tan(radians(_DetailB.w)) * (FRDetailNoise1(u * 5.3 + hw.z * 7.0, wall + 4u) - 0.5) * 2.0) * front;
                // marks: black rubber (darker, glossier), grey abrasion (the vinyl roughened: lighter, dull)
                half4 w = SAMPLE_TEXTURE2D_LOD(_WearMap, sampler_DetailMap, float2(m.x / 4.8, y / 0.6), FRDetailLod(px, 4.8 / 2048.0));
                half black = saturate(w.r * (half)_DetailB.x) * front;
                half grey = saturate(w.g * (half)_DetailB.x) * front;
                albedo = lerp(albedo, half3(0.022h, 0.020h, 0.018h), black * 0.8h);
                smoothness += 0.08h * black;
                albedo = lerp(albedo, albedo * 1.9h + 0.03h, grey * 0.7h);
                smoothness -= 0.15h * grey;
                // dust on the top
                albedo = lerp(albedo, half3(0.30h, 0.29h, 0.27h), (half)_DetailB.y * topF);
                smoothness -= 0.2h * topF;
            }

            // ---- 8: broadloom carpet (L0_Carpet: a level-loop pile in 12 ft rolls, glued down; the map holds 1 m of pile). The rolls
            // run along the floor's v (world -z), 192 / 52 m wide (12 ft 1 in: 52 rolls in the 192 m the title stream rebases by, so
            // nothing jumps at a rebase), cut into pieces 9.6 m long where the rooms needed them. Every piece is its own dye lot and
            // lies its own way (a few were turned end for end: the shading step at a seam, lighter on one side looking one way and
            // darker the other); the seams peak a little over the hot-melt tape; pieces that were never stretched tight buckle into
            // long soft ridges; old spills and water marks (_WearMap = Office_Stains_W: coffee in quadrants 0-1, a tide-ringed water
            // mark in quadrant 2) in some 1.6 m cells; where the floor is dirtiest (the macro field) the loops are crushed flat.
            // A: lot tone, lot hue, lay sheen, seams   B: ripple share, ripple height mm, stain share, stain strength
            // C: turned share, crush, lay wander rad, unused

            // smooth 2-D value noise (0..1) on the integer lattice of x
            float FRDetailNoise2(float2 x, uint seed)
            {
                float2 i = floor(x), f = x - i;
                f = f * f * (3.0 - 2.0 * f);
                uint2 k = (uint2)(int2)i;
                float a = (FRDetailHashU(uint3(k, seed), 5u) & 0xffffu) / 65535.0;
                float b = (FRDetailHashU(uint3(k + uint2(1u, 0u), seed), 5u) & 0xffffu) / 65535.0;
                float c = (FRDetailHashU(uint3(k + uint2(0u, 1u), seed), 5u) & 0xffffu) / 65535.0;
                float d = (FRDetailHashU(uint3(k + uint2(1u, 1u), seed), 5u) & 0xffffu) / 65535.0;
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            void FRDetailBroadloom(float3 p, float2 m, float px, float3 t, float3 b, half4 m1, half4 m2,
                                   inout half3 albedo, inout half smoothness, inout half cavity, inout half3 nTS)
            {
                const float RW = 192.0 / 52.0, PL = 9.6;
                // the roll and the piece of it this point lies on
                float rx = m.x + 1.37;
                float ri = floor(rx / RW);
                float fx = rx - ri * RW;
                uint roll = FRDetailWrap(ri, 52.0);
                float4 hr = FRDetailH8(uint3(roll, 0u, 0u), 101u);
                float ry = m.y + hr.x * PL;
                float pj = floor(ry / PL);
                float fy = ry - pj * PL;
                uint piece = FRDetailWrap(pj, 20.0);
                float4 hp = FRDetailH8(uint3(roll, piece, 1u), 102u);
                float4 hp2 = FRDetailH8(uint3(roll, piece, 2u), 103u);
                // the dye lot
                half lot = (half)(_DetailA.x * (hp.x * 2.0 - 1.0));
                half hue = (half)(_DetailA.y * (hp.y * 2.0 - 1.0));
                albedo *= half3(1.0h + lot + hue, 1.0h + lot, 1.0h + lot - 1.5h * hue);
                // the lay: along +v, or turned end for end, a few degrees off, and wandering inside the piece (walked, vacuumed and
                // crushed one way here and another there: soft patches a metre or two across); lighter looking along it, darker against
                float2 pq = float2(fx, fy);
                float wander = FRDetailNoise2(pq * 0.55 + hp.xy * 31.0, roll * 13u + piece) * 2.0 - 1.0;
                float ang = (hp.z < _DetailC.x ? -1.5707963 : 1.5707963) + (hp.w - 0.5) * 0.10 + wander * _DetailC.z;
                float2 lay = float2(cos(ang), sin(ang));
                float3 V = GetWorldSpaceNormalizeViewDir(p);
                float2 v2 = float2(dot(V, t), dot(V, b));
                float vl = length(v2);
                half along = vl > 1e-4 ? (half)dot(-v2 / vl, lay) : 0.0h;
                albedo *= 1.0h + (half)_DetailA.z * along * (half)saturate(vl * 1.4);
                // the seams: a hairline over the tape, both edges peaking about 1 mm in a soft ridge 2-3 cm wide
                float ds = min(fx, RW - fx);
                float de = min(fy, PL - fy);
                float sideS = fx < 0.5 * RW ? 1.0 : -1.0;
                float sideE = fy < 0.5 * PL ? 1.0 : -1.0;
                half gap = (half)max(FRDetailLine(ds, 0.0025, px), FRDetailLine(de, 0.0025, px));
                albedo *= 1.0h - (half)_DetailA.w * 0.45h * gap;
                cavity *= 1.0h - (half)_DetailA.w * 0.3h * gap;
                const float W0 = 0.022;
                float pk = 0.0012 * _DetailA.w * saturate(0.06 / px - 0.5);
                float gS = pk * (-2.0 * ds / (W0 * W0)) * exp(-(ds * ds) / (W0 * W0));
                float gE = pk * (-2.0 * de / (W0 * W0)) * exp(-(de * de) / (W0 * W0));
                nTS.xy -= (half2)float2(gS * sideS, gE * sideE);
                // buckles: up to two long soft ridges in a piece that let go, mostly across the roll, stopping short of the seams
                UNITY_BRANCH
                if (hp2.x < _DetailB.x)
                {
                    float2 q = float2(fx, fy);
                    float edge = smoothstep(0.0, 0.35, min(ds, de));
                    float2 grad = float2(0.0, 0.0);
                    float lift = 0.0;
                    for (uint k = 0u; k < 2u; k++)
                    {
                        float4 hk = FRDetailH8(uint3(roll, piece, 3u + k), 104u + k);
                        float on = (k == 0u || hp2.y < 0.45) ? 1.0 : 0.0;
                        float2 c = float2(RW * (0.15 + 0.7 * hk.x), PL * (0.12 + 0.76 * hk.y));
                        float a = (hk.z - 0.5) * 0.9;
                        float2 dir = float2(cos(a), sin(a));
                        float2 nrm = float2(-dir.y, dir.x);
                        float halfLen = 0.6 + 1.2 * hk.w;
                        float w = 0.08 + 0.12 * frac(hk.x * 7.31);
                        float hgt = _DetailB.y * 0.001 * (0.5 + 0.5 * frac(hk.y * 5.17)) * on;
                        float s = dot(q - c, dir);
                        float d = dot(q - c, nrm) + 0.10 * (FRDetailNoise1(s * 1.1 + hk.z * 17.0, roll * 31u + piece + k * 7u) - 0.5);
                        float env = (1.0 - smoothstep(halfLen - 0.5, halfLen, abs(s))) * edge;
                        float hh = hgt * env * exp(-(d * d) / (w * w));
                        grad += (-2.0 * d / (w * w)) * hh * nrm;
                        lift += hh;
                    }
                    nTS.xy -= (half2)(grad * saturate(0.3 / px - 0.5));
                    // the crest's loops open up a little (lighter), as stretched pile does
                    albedo *= 1.0h + (half)(3.5 * lift);
                }
                // spills and water marks: some 1.6 m cells (120 in 192 m) hold one, the atlas square 0.45-1.0 m, turned
                const float SC = 1.6;
                float2 sc = floor(m / SC);
                uint3 sid = uint3(FRDetailWrap(sc.x, 120.0), FRDetailWrap(sc.y, 120.0), 4u);
                float4 hs = FRDetailH8(sid, 106u);
                UNITY_BRANCH
                if (hs.x < _DetailB.z)
                {
                    float4 hs2 = FRDetailH8(sid, 107u);
                    float S = 0.45 + 0.55 * hs2.x;
                    float2 c = (sc + 0.5) * SC + (hs2.yz - 0.5) * (SC - S);
                    float2 g = (m - c) / S;
                    if (max(abs(g.x), abs(g.y)) < 0.5)
                    {
                        if (hs.y > 0.5) g = float2(-g.y, g.x);
                        if (frac(hs.y * 2.0) > 0.5) g = -g;
                        bool water = hs.w < 0.35;
                        float2 auv = (g + 0.5) * 0.5 + (water ? float2(0.0, 0.5) : float2(hs.z < 0.5 ? 0.0 : 0.5, 0.0));
                        half4 st = SAMPLE_TEXTURE2D_LOD(_WearMap, sampler_DetailMap, auv, FRDetailLod(px, S / 512.0));
                        half k = (half)_DetailB.w;
                        if (water)
                        {
                            albedo = lerp(albedo, albedo * half3(0.88h, 0.82h, 0.70h), st.r * k);
                            albedo = lerp(albedo, albedo * half3(0.64h, 0.54h, 0.42h), st.g * k);
                        }
                        else
                        {
                            albedo = lerp(albedo, albedo * half3(0.62h, 0.50h, 0.38h), st.r * k);
                            albedo *= 1.0h - 0.22h * st.g * k;
                        }
                        smoothness += 0.02h * st.g * k;
                    }
                }
                // crushed where the floor is dirtiest and where people walked most (patches inside the piece): the loops flattened (less
                // relief, less cavity), darker and a little glossier
                half walked = (half)smoothstep(0.58, 0.85, FRDetailNoise2(pq * 0.32 + hp.zw * 17.0, roll * 7u + piece + 3u));
                half cr = (half)_DetailC.y * saturate((half)smoothstep(0.55, 0.95, m1.g * 0.6 + m2.g * 0.4) + walked);
                albedo *= 1.0h - 0.07h * cr;
                nTS.xy *= 1.0h - 0.6h * cr;
                smoothness += 0.03h * cr;
                cavity = lerp(cavity, 1.0h, 0.4h * cr);
            }

            // ---- 9: painted concrete block (RunB_BlockUpper / Lower, Stair_Block, Stair_Band: 16 x 8 in units laid in running bond with
            // 3/8 in tooled joints; the map holds 0.4 x 0.4 m, two courses, the odd course's joints at 0.2 m). Every block was cast and
            // laid by itself and took the paint its own way: its face a hair proud or shy and tilted (it catches the lamps at its own
            // angle), bowed a little, denser or more open (the sheen and the tone a touch off its neighbours); the mortar takes the
            // paint flatter; the concave joints shade under the overhead lamps (the lower lip lit, the upper in shadow); touch-ups
            // rolled over a few blocks at a time and cut to the joints, from another can; pinholes in the face (the detail map);
            // shoes, carts and the mop at the foot (_WearMap: the wall's foot strip, Office_WallBase_W). The lamps are overhead, so
            // the faces' set and bow and the map's own relief go into the tone as well, faded in with distance.
            // A: block gloss variety, block tone variety, face tilt deg, face bow mm
            // B: joint gloss drop, joint shade, touch-up share, touch-up gloss      C: foot marks, relief in the tone, unused, unused
            void FRDetailBlock(float3 p, float3 nGeo, float2 m, float px, half3 nMap,
                               inout half3 albedo, inout half smoothness, inout half cavity, inout half3 nTS)
            {
                const float BL = 0.4, BH = 0.2, JW = 0.0095;
                uint wall = FRDetailWallId(p, nGeo);
                float y = m.y;
                float ci = floor(y / BH);
                float fy = y - ci * BH;
                float off = FRDetailWrap(ci, 2.0) == 1u ? 0.5 * BL : 0.0;
                float u = m.x - off;
                float bi = floor(u / BL);
                float fx = u - bi * BL;
                uint3 bid = uint3(wall, FRDetailWrap(ci, 1024.0), FRDetailWrap(bi, 480.0));
                float4 h = FRDetailH8(bid, 111u);
                float4 h2 = FRDetailH8(bid, 112u);
                // the joints, filtered by the pixel's footprint: vertical between the blocks, horizontal between the courses
                float dxj = min(fx, BL - fx);
                float dyj = min(fy, BH - fy);
                half jv = (half)FRDetailLine(dxj, JW, px);
                half jh = (half)FRDetailLine(dyj, JW, px);
                half joint = max(jv, jh);
                half face = 1.0h - joint;
                // the block's own take of the paint, its set and its bow (mostly proud, one in five shy)
                smoothness += (half)(_DetailA.x * (h.x * 2.0 - 1.0)) * face;
                albedo *= 1.0h + (half)(_DetailA.y * (h.y * 2.0 - 1.0)) * face;
                float2 tl = (h.zw * 2.0 - 1.0) * tan(radians(_DetailA.z));
                float bow = _DetailA.w * 0.001 * (0.35 + 0.65 * h2.x) * (h2.y < 0.8 ? 1.0 : -0.6);
                float gx = fx / BL - 0.5, gy = fy / BH - 0.5;
                float2 grad = bow * float2(-8.0 * gx * (1.0 - 4.0 * gy * gy) / BL, -8.0 * gy * (1.0 - 4.0 * gx * gx) / BH);
                float2 sl = (tl - grad) * face;
                nTS.xy += (half2)sl;
                // the mortar takes the paint flatter; a concave joint's upper half faces down (in the lamps' shadow), its lower half up
                smoothness -= (half)_DetailB.x * joint;
                albedo *= 1.0h - (half)_DetailB.y * jh * (fy < 0.5 * BH ? 1.0h : -0.4h);
                albedo *= 1.0h - (half)_DetailB.y * 0.3h * jv;
                cavity *= 1.0h - 0.15h * joint;
                // touch-ups: rolled over a patch of blocks and cut to the joints, from another can (patch cells of 5 blocks x 6 courses)
                float pcx = floor(bi / 5.0), pcy = floor(ci / 6.0);
                uint3 pid = uint3(wall, FRDetailWrap(pcx, 96.0), FRDetailWrap(pcy, 256.0) + 9u);
                float4 ht = FRDetailH8(pid, 113u);
                if (ht.x < _DetailB.z)
                {
                    float lx = bi - pcx * 5.0, ly = ci - pcy * 6.0;
                    float x0 = floor(ht.y * 3.0), x1 = x0 + 1.0 + floor(ht.z * 2.5);
                    float y0 = floor(ht.w * 3.0), y1 = y0 + 1.0 + floor(frac(ht.y * 7.0) * 3.0);
                    if (lx >= x0 && lx <= x1 && ly >= y0 && ly <= y1)
                    {
                        float4 ht2 = FRDetailH8(pid, 114u);
                        smoothness += (half)_DetailB.w * (ht2.x < 0.65 ? 1.0h : -0.8h);
                        albedo *= 1.0h + (half)((ht2.y - 0.5) * 0.05);
                    }
                }
                // the foot: black rubber from shoes, cart wheels and the mop bucket, grey scuffs (the foot strip, v 0 at the floor)
                UNITY_BRANCH
                if (y < 0.62 && _DetailC.x > 0.0)
                {
                    half4 w = SAMPLE_TEXTURE2D_LOD(_WearMap, sampler_DetailMap, float2(m.x / 4.8, y / 0.6), FRDetailLod(px, 4.8 / 2048.0));
                    half black = saturate(w.r * (half)_DetailC.x);
                    half grey = saturate(w.g * (half)_DetailC.x);
                    albedo = lerp(albedo, albedo * 0.18h + 0.02h, black * 0.85h);
                    smoothness += 0.06h * black - 0.08h * grey;
                    albedo *= 1.0h + 0.06h * grey;
                }
                // the relief in the tone (the lamps are overhead: what faces up is lighter), faded in with distance: the face's set and
                // bow, and half of the map's own relief (the joints' arrises, the face's texture)
                half fadeT = (half)saturate(px / 0.003);
                albedo *= 1.0h + (half)_DetailC.y * fadeT * ((half)sl.y + 0.5h * nMap.y);
            }

            // the hard floor's fine relief: the buffer's (or the trowel's) swirl in the detail map at _DetailKind.y metres (1024 px), at
            // _DetailC.z (negative: the arcs burnished glossier, as a power trowel leaves them). The slopes fade under a pixel; the
            // sheen does not: its mips average the scratches into the haze they make far off. (_DetailKind.z stays 0 on this kind,
            // so FRDetailSurface's generic fine pass never reads this map as well.)
            void FRDetailSwirl(float2 m, float px, inout half smoothness, inout half3 nTS)
            {
                float span = max(_DetailKind.y, 0.1);
                float texel = span / 1024.0;
                half4 d = SAMPLE_TEXTURE2D_LOD(_DetailMap, sampler_DetailMap, m / span, FRDetailLod(px, texel) + max(_DetailC.w, 0.0));
                half fade = (half)saturate(1.5 - px / texel / 3.0) * (half)(_DetailC.w > 0.5 ? 0.0 : 1.0);
                half k = (half)_DetailC.z;
                nTS.xy += (d.rg - 0.5h) * (half)(1.0 / FR_DETAIL_K) * k * fade;
                smoothness += (d.b - 0.5h) * 0.4h * k;
            }

            // ---- 10: hard floor under a wax or sealer coat (vinyl composition tile in 12 in squares: RunB_Floor / Wax / Lane / Accent,
            // Station_Floor, Station_FloorBorder; sealed concrete without tiles: Stair_Fill). The slab under it is not flat (a slow wave
            // over metres) and each tile sits on it at its own small angle, so a lamp's long reflection wavers and breaks at the
            // joints; each tile its own batch (tone) and its own take of the wax (gloss); the joints hold old wax and dirt (a dark
            // grid); the buffer's pad left arcs of fine scratches (FRDetailSwirl); heels, wheels and the mop bucket left black marks
            // and grey scuffs (_WearMap = Rooms_FloorMarks_W: four 0.6 m fields, one in some 0.6 m cells, turned); the wax is worn dull
            // where people walk (the lane) and built up yellow where the buffer never reached (the perimeter): per material.
            // _DetailKind: kind, swirl span m, 0 (no generic fine pass), micro-shadowing
            // A: tile gloss variety, tile tone variety, tile tilt deg, slab wave deg   B: joint dirt, marks share, marks strength, wear
            // C: tile size m (0 = no tiles), build-up, swirl strength (0 = no fetch; negative = burnished), swirl mip bias (broad arcs)
            void FRDetailHardFloor(float3 p, float2 m, float px, inout half3 albedo, inout half smoothness, inout half cavity, inout half3 nTS)
            {
                // the slab's slow wave: a slope field from four 1-D waves across each other, a metre or two long
                float wv = tan(radians(_DetailA.w));
                float2 wave = float2(FRDetailNoise1(dot(m, float2(0.8, 0.6)) * 0.55, 201u) + FRDetailNoise1(dot(m, float2(-0.28, 0.96)) * 0.9, 202u) - 1.0,
                                     FRDetailNoise1(dot(m, float2(0.6, -0.8)) * 0.6, 203u) + FRDetailNoise1(dot(m, float2(0.96, 0.28)) * 1.1, 204u) - 1.0);
                nTS.xy += (half2)(wave * wv);
                float TS = _DetailC.x;
                UNITY_BRANCH
                if (TS > 0.01)
                {
                    float2 cf = floor(m / TS);
                    float2 f = m / TS - cf;
                    uint2 c = uint2(FRDetailWrap(cf.x, 640.0), FRDetailWrap(cf.y, 640.0));
                    float4 h = FRDetailH8(uint3(c, 0u), 121u);
                    float4 h2 = FRDetailH8(uint3(c, 1u), 122u);
                    // the tile's batch and its take of the wax; its own set on the slab
                    smoothness += (half)(_DetailA.x * (h.x * 2.0 - 1.0));
                    albedo *= 1.0h + (half)(_DetailA.y * (h.y * 2.0 - 1.0));
                    nTS.xy += (half2)((h.zw * 2.0 - 1.0) * tan(radians(_DetailA.z)));
                    // the joints: old wax and dirt in them (a dark line 1-2 mm), the edges a hair duller
                    float2 e = min(f, 1.0 - f) * TS;
                    float de = min(e.x, e.y);
                    half jl = (half)FRDetailLine(de, 0.0011 + 0.0012 * h2.x, px);
                    albedo *= 1.0h - (half)_DetailB.x * 0.55h * jl;
                    smoothness -= (half)_DetailB.x * 0.25h * jl;
                    cavity *= 1.0h - 0.4h * jl;
                }
                // the buffer's swirl (the full tier only)
                UNITY_BRANCH
                if (abs(_DetailC.z) > 0.0 && _FR_Detail.y < 0.5)
                    FRDetailSwirl(m, px, smoothness, nTS);
                // worn dull where people walk (more in some stretches than others), built up yellow where the buffer never reached
                half wear = (half)_DetailB.w * (half)(0.7 + 0.6 * (FRDetailNoise1(m.x * 0.35 + m.y * 0.21, 205u) - 0.5));
                smoothness -= wear;
                half bu = (half)_DetailC.y;
                albedo *= lerp(half3(1.0h, 1.0h, 1.0h), half3(0.97h, 0.93h, 0.80h), bu);
                smoothness += 0.05h * bu;
                // heel and wheel marks, grey scuffs: one of the atlas's four 0.6 m fields in some 0.6 m cells, turned and mirrored
                const float MC = 0.6;
                float2 mc = floor(m / MC);
                float4 hm = FRDetailH8(uint3(FRDetailWrap(mc.x, 320.0), FRDetailWrap(mc.y, 320.0), 7u), 123u);
                UNITY_BRANCH
                if (hm.x < _DetailB.y)
                {
                    float2 g = m / MC - mc - 0.5;
                    if (hm.y > 0.5) g = float2(-g.y, g.x);
                    if (frac(hm.y * 2.0) > 0.5) g = -g;
                    if (hm.w > 0.5) g.x = -g.x;
                    float2 auv = (g + 0.5) * 0.5 + float2(hm.z < 0.5 ? 0.0 : 0.5, frac(hm.z * 2.0) < 0.5 ? 0.0 : 0.5);
                    half4 w = SAMPLE_TEXTURE2D_LOD(_WearMap, sampler_DetailMap, auv, FRDetailLod(px, MC / 512.0));
                    half black = saturate(w.r * (half)_DetailB.z);
                    half grey = saturate(w.g * (half)_DetailB.z);
                    albedo = lerp(albedo, albedo * 0.16h + 0.015h, black * 0.85h);
                    smoothness -= 0.10h * black + 0.18h * grey;
                    albedo *= 1.0h + 0.10h * grey;
                }
            }

            // One entry for every kind. m = the material's metres (world-planar or mesh), nMap = the map's own tangent normal (before
            // any of this). px, the metres one pixel spans here, comes from the camera (the frustum's height over the screen's, at
            // this distance, stretched by the view's slant), not from derivatives (FRDetailLod).
            void FRDetailSurface(float3 p, float3 nGeo, float2 m, float3 t, float3 b, half4 m1, half4 m2,
                                 inout half3 albedo, inout half smoothness, inout half cavity, inout half3 nTS, inout FRDetailOut o)
            {
                float3 toCam = _WorldSpaceCameraPos.xyz - p;
                float dist = max(length(toCam), 1e-4);
                float ndv = max(abs(dot(nGeo, toCam / dist)), 0.2);
                float px = max(dist * 2.0 / (max(abs(UNITY_MATRIX_P._m11), 1e-3) * max(_ScreenParams.y, 1.0)) / ndv, 1e-5);
                half3 nMap = nTS;
                float kind = _DetailKind.x;
                o.micro = _FR_Detail.y > 0.5 ? 0.0h : (half)_DetailKind.w;
                UNITY_BRANCH
                if (_DetailKind.z > 0.0 && _FR_Detail.y < 0.5)
                    FRDetailFine(m, px, albedo, smoothness, nTS);
                UNITY_BRANCH
                if (kind < 1.5)
                    FRDetailBoard(p, nGeo, m, px, nMap, albedo, smoothness, nTS);
                else if (kind < 2.5)
                    FRDetailCarpet(p, m, px, t, b, albedo, nTS);
                else if (kind < 3.5)
                    FRDetailCeiling(p, m, px, (half)smoothstep(0.35, 0.85, m1.b * 0.7 + m2.b * 0.5), albedo, nTS);
                else if (kind < 4.5)
                    FRDetailFabric(p, albedo, o);
                else if (kind < 5.5)
                    FRDetailLaminate(nGeo, m, px, albedo, smoothness);
                else if (kind < 6.5)
                    FRDetailSteel(p, m, px, albedo, smoothness);
                else if (kind < 7.5)
                    FRDetailCove(p, nGeo, m, px, albedo, smoothness, cavity, nTS);
                else if (kind < 8.5)
                    FRDetailBroadloom(p, m, px, t, b, m1, m2, albedo, smoothness, cavity, nTS);
                else if (kind < 9.5)
                    FRDetailBlock(p, nGeo, m, px, nMap, albedo, smoothness, cavity, nTS);
                else if (kind < 10.5)
                    FRDetailHardFloor(p, m, px, albedo, smoothness, cavity, nTS);
                // a kind this text does not know adds no term (fail-safe for a material that lands before its kind)
                albedo = max(albedo, 0.0h);
            }

            // micro-shadowing from the cavity (Brinck-Maximov, The Technical Art of Uncharted 4; CoD WWII's cone): a recess darkens
            // under a grazing lamp and opens under a frontal one; k 0 = off
            half FRDetailMicroShadow(half NdotL, half ao, half k)
            {
                half a = lerp(1.0h, ao, k);
                return saturate(NdotL + 2.0h * a * a - 1.0h);
            }

            // UniversalFragmentPBR (URP 17.6 Lighting.hlsl) for the detail kinds: micro-shadowing on every light, and the cloth's sheen
            // (a soft rim at grazing view, from every light and the ambient). These kinds are matte to satin (0.08-0.6): no specular
            // AA here (it would need the normal's derivatives, see FRDetailLod).
            half4 FRDetailPBR(InputData inputData, SurfaceData surfaceData, FRDetailOut o)
            {
                bool specularHighlightsOff = false;
                BRDFData brdfData;
                InitializeBRDFData(surfaceData, brdfData);
            #if defined(DEBUG_DISPLAY)
                half4 debugColor;
                if (CanDebugOverrideOutputColor(inputData, surfaceData, brdfData, debugColor))
                    return debugColor;
            #endif
                BRDFData brdfDataClearCoat = CreateClearCoatBRDFData(surfaceData, brdfData);
                half4 shadowMask = CalculateShadowMask(inputData);
                AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
                uint meshRenderingLayers = GetMeshRenderingLayer();
                Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
                MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);
                LightingData lightingData = CreateLightingData(inputData, surfaceData);
                lightingData.giColor = GlobalIllumination(brdfData, brdfDataClearCoat, surfaceData.clearCoatMask,
                                                          inputData.bakedGI, aoFactor.indirectAmbientOcclusion, inputData.positionWS,
                                                          inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV);
                half ao = surfaceData.occlusion;
                half nv = 1.0h - saturate(dot(inputData.normalWS, inputData.viewDirectionWS));
                half3 sheenK = o.sheenColor * (o.sheen * nv * nv * nv);
                half3 sheen = sheenK * inputData.bakedGI * aoFactor.indirectAmbientOcclusion * 0.6h;
                half NdotL = 0.0h;
            #ifdef _LIGHT_LAYERS
                if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
            #endif
                {
                    NdotL = saturate(dot(inputData.normalWS, mainLight.direction));
                    lightingData.mainLightColor = LightingPhysicallyBased(brdfData, brdfDataClearCoat, mainLight,
                                                                          inputData.normalWS, inputData.viewDirectionWS,
                                                                          surfaceData.clearCoatMask, specularHighlightsOff)
                                                * FRDetailMicroShadow(NdotL, ao, o.micro);
                    sheen += sheenK * mainLight.color * (mainLight.distanceAttenuation * mainLight.shadowAttenuation * NdotL);
                }
            #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
            #if USE_CLUSTER_LIGHT_LOOP
                [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                    Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
            #ifdef _LIGHT_LAYERS
                    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            #endif
                    {
                        NdotL = saturate(dot(inputData.normalWS, light.direction));
                        lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
                                                                                      inputData.normalWS, inputData.viewDirectionWS,
                                                                                      surfaceData.clearCoatMask, specularHighlightsOff)
                                                            * FRDetailMicroShadow(NdotL, ao, o.micro);
                        sheen += sheenK * light.color * (light.distanceAttenuation * light.shadowAttenuation * NdotL);
                    }
                }
            #endif
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
            #ifdef _LIGHT_LAYERS
                    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            #endif
                    {
                        NdotL = saturate(dot(inputData.normalWS, light.direction));
                        lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
                                                                                      inputData.normalWS, inputData.viewDirectionWS,
                                                                                      surfaceData.clearCoatMask, specularHighlightsOff)
                                                            * FRDetailMicroShadow(NdotL, ao, o.micro);
                        sheen += sheenK * light.color * (light.distanceAttenuation * light.shadowAttenuation * NdotL);
                    }
                LIGHT_LOOP_END
            #endif
            #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                lightingData.vertexLightingColor += inputData.vertexLighting * brdfData.diffuse;
            #endif
                lightingData.additionalLightsColor += sheen;
            #if REAL_IS_HALF
                return min(CalculateFinalColor(lightingData, surfaceData.alpha), HALF_MAX);
            #else
                return CalculateFinalColor(lightingData, surfaceData.alpha);
            #endif
            }

            // the lighting for this pixel: the detail kinds' while the switch is on, URP's own otherwise (today, bit for bit)
            half4 FRDetailLit(InputData inputData, SurfaceData surfaceData, FRDetailOut o)
            {
                UNITY_BRANCH
                if (_FR_Detail.x < 0.5)
                    return FRDetailPBR(inputData, surfaceData, o);
                return UniversalFragmentPBR(inputData, surfaceData);
            }
            #define FR_DETAIL_LIT(i, s, d) FRDetailLit(i, s, d)
        #else
            #define FR_DETAIL_LIT(i, s, d) UniversalFragmentPBR(i, s)
        #endif

        #if defined(_FR_PRINT)
            // ---------------------------------------------------------------- print layer
            // The wallpaper is a sandwich: a static PAPER (_BaseMap = modulation, _BumpMap,
            // _MaskMap) and a PRINT that may move. The print only ever reaches the albedo;
            // normal, smoothness and cavity come from the paper alone, and with
            // _Metallic = 0 URP's specular colour is the constant 0.04, so highlights
            // cannot see the print.
            //
            // Globals, written by the print driver (never in Properties):
            //   _FR_Print       Texture2DArray, one 0.75 x 1.125 m roll tile per slice,
            //                   R = ink density, G = cream, both the parameters of the
            //                   LINEAR-light ramp below (0 ground .5 mid 1 deep); art made
            //                   with the sRGB-value duotone goes through print_encode
            //                   (Tools/lookdev/gen_surfaces.py, print_encode_lut.json) first.
            //                   B reserved (phosphor ink, unused here), A unused
            //   _FR_PrintClock  x frame position [0, n), y n (whole slices), z per-roll phase
            //                   (frames), w live mix (0 or unset = the material's static frame 0)
            // WebGL2 guarantees 16 fragment sampler units and this variant binds 12 (+2 with
            // light cookies, +1 with reflection-probe blending). Any further print map (phosphor
            // mask, warp noise) must share a sampler or pack into _FR_Print B/A.
            TEXTURE2D_ARRAY(_FR_Print);   SAMPLER(sampler_FR_Print);
            float4 _FR_PrintClock;
            // Per material: frame 0 in the same encoding (sRGB off).
            TEXTURE2D(_PrintTex);         SAMPLER(sampler_PrintTex);

            // Wall cue A + M4 + V1 (research/cue_driver/CUE_DRIVER_INTERFACE.md §1, §5 items 11 and 14).
            // Globals (never in Properties):
            //   _FR_Cue        Texture2DArray of the encoded cue slices C01..C16 (C00 = K00 is never sampled,
            //                  so it is not in the array): slice = state - 1 + 8 * (face on the -u set).
            //                  Bound by FrontRoomsCueInk.Bind; shares sampler_FR_Print (no new sampler).
            //   _FR_WayCells   RGBA8 64 x 64, point, read with LOAD (no sampler). One texel per 3 m map cell,
            //                  addressed TOROIDALLY: texel = (cellX & 63, cellZ & 63) with
            //                  cell = floor((world xz - _FR_WayOrigin.xy) / _FR_WayOrigin.z), 64 x 3 m = 192 m.
            //                  R from-state, G to-state (0 live print, 1..3 turn 30/60/90, 4..8 flow clicks 1..5),
            //                  B cross-fade from -> to, A = dir (0 +x, 1 +z, 2 -x, 3 -z) + 4 * message (0 none, 1 FLOW).
            //   _FR_WayOrigin  xy world x/z of the map root (cell (0, 0)'s corner), z cell size (3 m), w enable.
            //   _FR_CueReady   1 while FrontRoomsCueInk has bound _FR_Cue (Bind), else 0.
            // The map's FrontRoomsWayCueDriver writes _FR_WayCells and _FR_WayOrigin; FrontRoomsCueInk writes
            // _FR_Cue and _FR_CueReady. The cue shows only where both are on: an unset global reads 0, so until
            // then every wall shows the print exactly as before, whatever the cells say. WebGL (GLES3, WebGPU)
            // compiles it out: its sampler budget and the array's memory there are the WebGL line's call.
        #if !defined(SHADER_API_GLES3) && !defined(SHADER_API_WEBGPU)
            #define FR_CUE_INK 1
            TEXTURE2D_ARRAY(_FR_Cue);
            TEXTURE2D(_FR_WayCells);
            float4 _FR_WayOrigin;
            float _FR_CueReady;
        #endif

            // Mip bias (render scale < 1, upscalers): URP (Core.hlsl) scales the gradients of
            // SAMPLE_TEXTURE2D_GRAD by _GlobalMipBias.y but leaves SAMPLE_TEXTURE2D_ARRAY_GRAD
            // alone, so the array fetches apply the same scale themselves. Both print fetches
            // use gradients: with 16x aniso, Metal's LOD bias (what SAMPLE_TEXTURE2D uses) and
            // the equivalent gradient scale pick different footprints (0.6 levels apart at
            // render scale 0.5), and the static frame must match the live slices exactly.
        #if defined(PLATFORM_SAMPLE_TEXTURE2D_GRAD)
            #define FR_PRINT_GRAD_SCALE _GlobalMipBias.y
        #else
            #define FR_PRINT_GRAD_SCALE 1.0
        #endif

            // Paper texel decode: (alpha, beta, gamma) = texel.rgb * SCALE + BIAS, and
            // albedo = print * alpha + tobacco * beta + gamma (linear light). Must match
            // PAPER_SCALE / PAPER_BIAS / TOBACCO in Tools/lookdev/gen_surfaces.py.
            #define FR_PAPER_SCALE half3(0.70h, 0.14h, 0.02h)
            #define FR_PAPER_BIAS  half3(0.46h, -0.001h, -0.004h)
            #define FR_TOBACCO_LIN half3(0.194618h, 0.104616h, 0.023153h)   // #7A5B2A
            #define FR_CREAM_MIX   0.55h

            float FRHash01(uint x)
            {
                x ^= x >> 16; x *= 0x7feb352du; x ^= x >> 15; x *= 0x846ca68bu; x ^= x >> 16;
                return (x & 0xffffu) / 65535.0;
            }

            // The print's UV for both print fetches, from the unwarped roll UV. The later
            // _FR_PrintWarp (albedo only) hooks in here and adds its offset to rollUV.
            float2 PrintUV(float2 rollUV)
            {
                return rollUV;
            }

            // Duotone ramp in linear light: ground -> mid -> deep.
            half3 InkRamp(half t)
            {
                return t < 0.5h ? lerp(_InkGround.rgb, _InkMid.rgb, t * 2.0h)
                                : lerp(_InkMid.rgb, _InkDeep.rgb, t * 2.0h - 1.0h);
            }

            // ---------------------------------------------------------------- hung paper
            // research/wall_material/10_research.md. A wall reads as paper because of its sheets: an edge you can see, a
            // tone step from sheet to sheet, paper that is not the same length of the roll on every sheet. All of it is
            // static, reads the material uv and the wall's frame, and touches only the paper's modulation, normal,
            // smoothness and cavity. A seam is where the paper changes, not where the print repeats, so it runs down the
            // plain field (_SheetParams.x) instead of hiding in the stripes at the tile edge. Each sheet's print sits a few
            // mm off (printShift, one constant per sheet added to every print fetch and the cue, so they stay together).
            // Sheets are lapped, not reverse-hung: the chevron is a matched pattern. Every id is periodic in 192 m
            // (sheet & 255, plane mod 768), so a rebase changes nothing. Edges only darken or dull: walls never glow.
            #define FR_LOT_STRIPS 6u
            #define FR_SUBSTRATE_TILE float2(6.0, 3.0)

            struct FRHung
            {
                float2 paperUV;    // uv + this sheet's own length of the roll (x fixed)
                float2 printShift; // this sheet's pattern mismatch, in uv
                float2 dx, dy;     // gradients of the continuous uv: sheet edges never pick a wrong mip
                float across;      // 0..1 across the sheet
                float seamM;       // metres to the nearest edge of the sheet
                float pxM;         // metres per pixel across the sheet
                float side;        // +1 on a sheet's left edge (it lies on top), -1 on its right edge
                float yM;          // metres up the wall
                float hStrip, hLot, hWall, hSeam;
                uint seamSeed;
            };

            // One id per flat wall: axis, sign and the plane's offset in 0.25 m classes, 192 m periodic.
            uint FRWallId(float3 positionWS, float3 nFace)
            {
                float3 a = abs(nFace);
                uint axis = a.x >= a.z ? 0u : 1u;
                uint pos = (a.x >= a.z ? nFace.x : nFace.z) >= 0.0 ? 1u : 0u;
                int plane = (int)round(dot(positionWS, nFace) * 4.0);
                plane = ((plane % 768) + 768) % 768;
                return (uint)plane * 4u + axis * 2u + pos;
            }

            FRHung HungPaper(float2 uv, float3 positionWS, float3 nFace)
            {
                FRHung h;
                uint wall = FRWallId(positionWS, nFace);
                float sx = uv.x - _SheetParams.x;
                uint sheet = (uint)(int)floor(sx) & 255u;
                uint seam = (uint)(int)round(sx) & 255u;
                float tileX = _TileSize.x / max(_BaseMap_ST.x, 1e-3);
                float tileY = _TileSize.y / max(_BaseMap_ST.y, 1e-3);
                h.hWall = FRHash01(wall * 2654435761u + 7u);
                h.hStrip = FRHash01(sheet * 257u + wall * 65537u + 1u);
                h.hLot = FRHash01((sheet / FR_LOT_STRIPS) * 1031u + wall * 65537u + 3u);
                h.seamSeed = seam * 1543u + wall * 65537u + 5u;
                h.hSeam = FRHash01(h.seamSeed);
                h.paperUV = uv + float2(0.0, FRHash01(sheet * 769u + wall * 65537u + 2u));
                float mm = _SheetParams.y * 0.001;
                h.printShift = float2((FRHash01(sheet * 577u + wall * 65537u + 4u) - 0.5) * 0.6 * mm / tileX,
                                      (FRHash01(sheet * 691u + wall * 65537u + 8u) - 0.5) * 2.0 * mm / tileY);
                h.dx = ddx(uv);
                h.dy = ddy(uv);
                h.across = frac(sx);
                h.seamM = min(h.across, 1.0 - h.across) * tileX;
                h.pxM = max(fwidth(uv.x) * tileX, 1e-5);
                h.side = h.across < 0.5 ? 1.0 : -1.0;
                h.yM = positionWS.y;
                return h;
            }

            float FRNoise1(float x, uint seed)
            {
                float i = floor(x), f = x - i;
                f = f * f * (3.0 - 2.0 * f);
                uint k = (uint)(int)i;
                return lerp(FRHash01(k * 2654435761u + seed), FRHash01((k + 1u) * 2654435761u + seed), f);
            }

            // Tone of the sheet: edgeshade (same direction on every strip: a matched print is not reverse-hung,
            // so each seam shows a small step), the shade lot and the wall's age.
            half HungTone(FRHung h)
            {
                float t = _StripVar.x * (h.across * 2.0 - 1.0)
                        + _StripVar.y * ((h.hLot - 0.5) * 2.0 + (h.hStrip - 0.5) + (h.hWall - 0.5));
                return (half)(1.0 + t);
            }

            // The seam, in metres, so it stays stable at any distance: millimetre terms fade to their area mean once they
            // are under a pixel, and the slope they lose goes to roughness. damp is 0..1.
            void HungSeam(FRHung h, float damp, inout half albedoScale, inout half smoothness, inout half cavity, inout half3 nTS)
            {
                float d = h.seamM, w = h.pxM, lap = _SheetParams.z * 0.001;
                UNITY_BRANCH
                if (d > max(_SeamParams.x * 0.002, lap + 0.008) + w * 2.0) return;
                float along = FRNoise1(h.yM * 3.1 + h.hSeam * 37.0, h.seamSeed) * 0.65
                            + FRNoise1(h.yM * 9.7 + h.hSeam * 11.0, h.seamSeed + 1u) * 0.35;
                float open = 0.0;
                if (h.hSeam > 1.0 - _SeamParams.w || damp > 0.55)
                    open = smoothstep(0.62, 0.74, FRNoise1(h.yM * 1.7 + h.hSeam * 53.0, h.seamSeed + 2u));
                // adhesive left along the joint (dull) and the dirt that gathers on it
                float bw = _SeamParams.x * 0.001;
                float band = smoothstep(bw + 0.5 * w, bw - 0.5 * w + 1e-5, d) * saturate(2.0 * bw / w);
                smoothness -= (half)(_WearSheen.z * band * (0.5 + along));
                albedoScale *= (half)(1.0 - 0.10 * _SheetParams.w * band * (0.4 + along));
                // the top sheet's edge rises to the joint, more where it is damp
                float sigma = max(bw * 0.55, 1e-4);
                float height = _SeamParams.y * 0.001 * (0.35 + 1.1 * along) * (1.0 + 1.4 * damp);
                float slope = 2.0 * d / (sigma * sigma) * height * exp(-(d * d) / (sigma * sigma));
                float keep = saturate(sigma / w);
                smoothness -= (half)(saturate(abs(slope) * 8.0) * (1.0 - keep) * 0.03);
                // the hairline, wider where the joint has opened
                float lw = _SeamParams.z * 0.001 + open * 0.0022;
                float line_ = saturate((lw * 0.5 + 0.5 * w - d) / w) * saturate(lw / w);
                albedoScale *= (half)(1.0 - line_ * (0.45 + 0.4 * open));
                cavity *= (half)(1.0 - 0.5 * line_);
                // the lap: a double layer on the top sheet, its shadow on the sheet below
                if (h.side > 0.0)
                {
                    float dbl = smoothstep(lap + 0.5 * w, lap - 0.5 * w + 1e-5, d);
                    albedoScale *= (half)(1.0 - 0.055 * dbl);
                    float rise = smoothstep(0.0, 0.0025, d) * (1.0 - smoothstep(lap - 0.002, lap + 0.002, d));
                    slope += 0.05 * (1.0 - rise) * dbl;
                }
                else
                {
                    float shadow = exp(-d / max(0.0028, 0.5 * w)) * saturate(0.0028 / w * 2.0);
                    albedoScale *= (half)(1.0 - 0.42 * shadow);
                    cavity *= (half)(1.0 - 0.5 * shadow);
                }
                nTS.x += (half)(h.side * slope * keep);
            }

        #if defined(FR_CUE_INK)
            // The cue on top of the live print, per map cell. State 0 never samples the array: it
            // returns the ink exactly as the static _PrintTex and the ambient flipbook gave it, so a cue
            // starts from and returns to whatever the paper shows, with no pop (§5 item 14).
            // n is the face normal of the print's planar frame: the cell is taken half a metre into the
            // room the face looks into, so a wall on a cell edge belongs to the room it faces; the face
            // set comes from route . t, t = cross(up, n) = +u of PlanarFrame; faces across the route
            // (|route . t| < 0.5), floors and ceilings keep the plain print.
            // Cost while a cell carries a message: one LOAD and one array fetch (two during a cross-fade).
            void CueInk(float2 puv, float2 dx, float2 dy, float3 positionWS, float3 n, inout half2 ink)
            {
                if (_FR_WayOrigin.w < 0.5 || _FR_CueReady < 0.5 || abs(n.y) > 0.5) return;
                float2 p = (positionWS.xz + n.xz * 0.5 - _FR_WayOrigin.xy) / _FR_WayOrigin.z;
                uint2 texel = (uint2)(int2)floor(p) & 63u;                       // toroidal: 64 cells = 192 m
                half4 w = LOAD_TEXTURE2D(_FR_WayCells, texel);
                uint code = (uint)round(w.a * 255.0);
                if ((code >> 2) == 0u) return;                                    // no message here
                uint dir = code & 3u;
                float2 d = dir == 0u ? float2(1, 0) : dir == 1u ? float2(0, 1) : dir == 2u ? float2(-1, 0) : float2(0, -1);
                float faceSign = dot(d, float2(n.z, -n.x));                       // route . t
                if (abs(faceSign) < 0.5) return;                                  // across the route: plain print
                float set = faceSign < 0.0 ? 7.0 : -1.0;                          // slice = state - 1 (+8 on -u faces)
                float from = round(w.r * 255.0), to = round(w.g * 255.0);
                half k = (half)w.b;
                dx *= FR_PRINT_GRAD_SCALE; dy *= FR_PRINT_GRAD_SCALE;
                half2 c0 = from < 0.5 ? ink : SAMPLE_TEXTURE2D_ARRAY_GRAD(_FR_Cue, sampler_FR_Print, puv, from + set, dx, dy).rg;
                UNITY_BRANCH
                if (k > 0.002h && to != from)
                {
                    half2 c1 = to < 0.5 ? ink : SAMPLE_TEXTURE2D_ARRAY_GRAD(_FR_Cue, sampler_FR_Print, puv, to + set, dx, dy).rg;
                    c0 = lerp(c0, c1, k);
                }
                ink = c0;
            }
        #endif

            // Ink (R density, G cream) at this roll position. uv is the material UV (metres /
            // _TileSize); rollUV applies _PrintTex_ST once and is the print's own roll space:
            // the roll strips (floor(rollUV.x)) and the fetches both use it. It is continuous
            // across roll seams and the derivatives come from it, so roll lines never pick
            // the wrong mip. positionWS and nFace (the planar frame's normal) are for the cue only.
            half2 PrintInk(float2 uv, float3 positionWS, float3 nFace, float2 sheetShift)
            {
                float2 rollUV = uv * _PrintTex_ST.xy + _PrintTex_ST.zw;
                float2 puv = PrintUV(rollUV);
                float2 dx = ddx(puv), dy = ddy(puv);
                float2 dx0 = dx, dy0 = dy;                                       // continuous across the sheet edges
                puv += sheetShift;
                half2 ink = SAMPLE_TEXTURE2D_GRAD(_PrintTex, sampler_PrintTex, puv, dx, dy).rg;   // URP scales dx, dy by _GlobalMipBias.y
                UNITY_BRANCH
                if (_FR_PrintClock.w > 0.0)
                {
                    dx *= FR_PRINT_GRAD_SCALE; dy *= FR_PRINT_GRAD_SCALE;                           // the same scale for the array
                    float n = max(floor(_FR_PrintClock.y), 1.0);          // whole slices only
                    uint strip = (uint)(int)floor(rollUV.x) & 3u;         // & 3: every 3 m module looks the same
                    float f = _FR_PrintClock.x + FRHash01(strip) * _FR_PrintClock.z;
                    f -= n * floor(f / n);
                    float i0 = min(floor(f), n - 1.0);
                    float i1 = (i0 + 1.0 >= n) ? 0.0 : i0 + 1.0;
                    half k = (half)saturate(f - i0);
                    half2 a = SAMPLE_TEXTURE2D_ARRAY_GRAD(_FR_Print, sampler_FR_Print, puv, i0, dx, dy).rg;
                    half2 b = SAMPLE_TEXTURE2D_ARRAY_GRAD(_FR_Print, sampler_FR_Print, puv, i1, dx, dy).rg;
                    ink = lerp(ink, lerp(a, b, k), (half)saturate(_FR_PrintClock.w));
                }
            #if defined(FR_CUE_INK)
                CueInk(puv, dx0, dy0, positionWS, nFace, ink);
            #endif
                return ink;
            }

            half3 PrintAlbedo(float2 uv, float3 positionWS, float3 nFace, FRHung hung)
            {
                half3 paper = SAMPLE_TEXTURE2D_GRAD(_BaseMap, sampler_BaseMap, hung.paperUV, hung.dx, hung.dy).rgb * FR_PAPER_SCALE + FR_PAPER_BIAS;
                half2 ink = PrintInk(uv, positionWS, nFace, hung.printShift) * _PrintAmount;
                half3 inkColour = lerp(InkRamp(ink.r), _InkCream.rgb, ink.g * FR_CREAM_MIX);
                return (inkColour * paper.x + FR_TOBACCO_LIN * paper.y + paper.z) * HungTone(hung);
            }
        #endif

        #if defined(_FR_PRINT) && !defined(SHADER_API_GLES3) && !defined(SHADER_API_WEBGPU)
            // The wall under the paper: shares sampler_BumpMap (no new sampler). WebGL compiles it out: this variant
            // is at its texture-unit budget there, and that tier's look is the WebGL line's call.
            #define FR_SUBSTRATE 1
            TEXTURE2D(_SubstrateMap);
        #endif

        #if defined(_FR_LAMP_FIELD)
            // Its own sampler: with the keyword on, _EmissionMap is not sampled, so the
            // compiler strips it and sampler_EmissionMap no longer exists (HLSLcc error on
            // Metal). The field map's importer state is clamped, trilinear.
            TEXTURE2D(_LampFieldMap); SAMPLER(sampler_LampFieldMap);
        #endif

        #if defined(_FR_DEBUG_VIEW)
            // 1 specular only, 2 diffuse only, 3 wear masks (stain, floor grime, ceiling
            // streaks), 4 wear masks (tone, dirt, damp), 5 albedo. 0 = normal shading.
            float _FR_DebugView;

            // UniversalFragmentPBR (URP 17.3 Lighting.hlsl) with one lobe removed AFTER
            // InitializeBRDFData, so the BRDF still sees the real albedo (print included).
            half4 FRDebugLobePBR(InputData inputData, SurfaceData surfaceData, bool specularOnly)
            {
                bool specularHighlightsOff = false;
                BRDFData brdfData;
                InitializeBRDFData(surfaceData, brdfData);
                if (specularOnly) brdfData.diffuse = 0;
                else { brdfData.specular = 0; brdfData.grazingTerm = 0; }
                surfaceData.emission = 0;

                BRDFData brdfDataClearCoat = CreateClearCoatBRDFData(surfaceData, brdfData);
                half4 shadowMask = CalculateShadowMask(inputData);
                AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
                uint meshRenderingLayers = GetMeshRenderingLayer();
                Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
                MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);
                LightingData lightingData = CreateLightingData(inputData, surfaceData);
                lightingData.giColor = GlobalIllumination(brdfData, brdfDataClearCoat, surfaceData.clearCoatMask,
                                                          inputData.bakedGI, aoFactor.indirectAmbientOcclusion, inputData.positionWS,
                                                          inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV);
            #ifdef _LIGHT_LAYERS
                if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
            #endif
                    lightingData.mainLightColor = LightingPhysicallyBased(brdfData, brdfDataClearCoat, mainLight,
                                                                          inputData.normalWS, inputData.viewDirectionWS,
                                                                          surfaceData.clearCoatMask, specularHighlightsOff);
            #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
            #if USE_CLUSTER_LIGHT_LOOP
                [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                    Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
            #ifdef _LIGHT_LAYERS
                    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            #endif
                        lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
                                                                                      inputData.normalWS, inputData.viewDirectionWS,
                                                                                      surfaceData.clearCoatMask, specularHighlightsOff);
                }
            #endif
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
            #ifdef _LIGHT_LAYERS
                    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            #endif
                        lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
                                                                                      inputData.normalWS, inputData.viewDirectionWS,
                                                                                      surfaceData.clearCoatMask, specularHighlightsOff);
                LIGHT_LOOP_END
            #endif
            #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                lightingData.vertexLightingColor += inputData.vertexLighting * brdfData.diffuse;
            #endif
                return CalculateFinalColor(lightingData, surfaceData.alpha);
            }
        #endif

        #if defined(_FR_LEAFWEAR)
            // Zone doors' leaf wear (20_design A6): one fetch in mesh-UV metres, zone door leaves only.
            TEXTURE2D(_FR_LeafWearMap);   SAMPLER(sampler_FR_LeafWearMap);
        #endif

        #if defined(_FR_DAMAGE)
            // Door break (50_rethink 3.8): two fetches (onset + normal, one sampler) and a few ALU, on damaged doors only.
            TEXTURE2D(_FR_DamageMap);     SAMPLER(sampler_FR_DamageMap);
            TEXTURE2D(_FR_DamageNormal);

            float FRDamageHash(float2 p)
            {
                p = frac(p * float2(0.1031, 0.1030));
                p += dot(p, p.yx + 33.33);
                return frac((p.x + p.y) * p.x);
            }

            // value noise (Frostbite 2's detail noise: a ragged, growing edge instead of the texel steps)
            half FRDamageNoise(float2 x)
            {
                float2 i = floor(x), f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                float a = FRDamageHash(i), b = FRDamageHash(i + float2(1, 0)), c = FRDamageHash(i + float2(0, 1)), d = FRDamageHash(i + 1.0);
                return (half)lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
        #endif

        #if defined(_FR_OWN_LIGHTS)
            // Stairwell A (Red 2026-10-22: the stairwell is a safe zone; it must read steady while the maze's lamps
            // dip with W1's warnings). The map's lamps are unshadowed 10-12 m spots that light straight through the
            // stairwell's walls, and URP light layers are off (FrontRooms_URP.asset), so URP writes no light layer
            // mask to test. A punctual additional light whose range is over FR_OWN_LIGHT_RANGE is skipped instead:
            // URP 17 stores 1 / range^2 in its attenuation.x on every platform (directional lights store 0 and are
            // kept). The stairwell's own lights reach 4.2 m (the desktop spots) or 1.5 m (its point lamp on phones).
            #define FR_OWN_LIGHT_RANGE 6.0

            // Run! rooms (run_b_production/60_production_66.md, stage C: step 10 + C39). The map's lamps light through a
            // Run! corridor's walls and the rig's own lights reach 10-14 m, so the range test cannot tell them apart. A
            // Run! renderer's property block carries its room as a box (FrontRoomsRunRig.GateVectors: the room-local
            // frame, z 0 on the entrance wall's centre line, the sides on the long walls' centre lines) with its two
            // openings, and _FR_RunGateB.w = 1 switches FROwnLight to it: a light reaches a point when both lie on the
            // same side of the box, or when the segment between them first leaves the box through the arch (z 0) or the
            // goal opening (z = length; only its lite while the leaf is shut). Every other renderer (the stairwell's
            // Stair_* materials) never sets these, reads 0 and keeps the range test exactly as before.
            //   A: the room's +X and +Z axes in world XZ (x, z, x, z)    B: the origin (world, on the floor); w 1 = on
            //   C: half width to the wall centre lines, length, ceiling  D: the arch (centre x, half width, top)
            //   E: the goal opening now (centre x, half width, bottom, top)
            float4 _FR_RunGateA, _FR_RunGateB, _FR_RunGateC, _FR_RunGateD, _FR_RunGateE;
            #define FR_RUN_FLOOR (-0.05)

            float3 FRRunLocal(float3 p)
            {
                float2 d = p.xz - _FR_RunGateB.xz;
                return float3(dot(d, _FR_RunGateA.xy), p.y - _FR_RunGateB.y, dot(d, _FR_RunGateA.zw));
            }

            bool FRRunInside(float3 q)
            {
                return abs(q.x) < _FR_RunGateC.x && q.z > 0.0 && q.z < _FR_RunGateC.y && q.y > FR_RUN_FLOOR && q.y < _FR_RunGateC.z;
            }

            // a inside the box, b outside (room-local): does the segment a -> b first leave the box through an opening?
            bool FRRunPortal(float3 a, float3 b)
            {
                float3 d = b - a;
                float tx = abs(b.x) >= _FR_RunGateC.x ? (sign(b.x) * _FR_RunGateC.x - a.x) / d.x : 2.0;
                float ty = b.y >= _FR_RunGateC.z ? (_FR_RunGateC.z - a.y) / d.y : (b.y <= FR_RUN_FLOOR ? (FR_RUN_FLOOR - a.y) / d.y : 2.0);
                float tz = b.z >= _FR_RunGateC.y ? (_FR_RunGateC.y - a.z) / d.z : (b.z <= 0.0 ? -a.z / d.z : 2.0);
                float3 c = a + d * tz;
                float4 o = b.z <= 0.0 ? float4(_FR_RunGateD.x, _FR_RunGateD.y, FR_RUN_FLOOR, _FR_RunGateD.z) : _FR_RunGateE;
                // an end wall must come first (not a side wall, the floor or the ceiling), and in its opening
                return tz < tx && tz < ty && abs(c.x - o.x) < o.y && c.y > o.z && c.y < o.w;
            }

            bool FROwnLight(uint lightIndex, float3 positionWS)
            {
            #if USE_CLUSTER_LIGHT_LOOP
                int perObjectLightIndex = lightIndex;
            #else
                int perObjectLightIndex = GetPerObjectLightIndex(lightIndex);
            #endif
                bool own;
                if (_FR_RunGateB.w > 0.5)
                {
            #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
                    float4 lightPositionWS = _AdditionalLightsBuffer[perObjectLightIndex].position;
            #else
                    float4 lightPositionWS = _AdditionalLightsPosition[perObjectLightIndex];
            #endif
                    float3 p = FRRunLocal(positionWS);
                    float3 q = FRRunLocal(lightPositionWS.xyz);
                    bool pIn = FRRunInside(p);
                    bool qIn = FRRunInside(q);
                    // directional lights (w 0) always; a punctual light on the same side, or through an opening
                    own = lightPositionWS.w < 0.5 || pIn == qIn || (pIn ? FRRunPortal(p, q) : FRRunPortal(q, p));
                }
                else
                {
            #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
                    float oneOverRangeSq = _AdditionalLightsBuffer[perObjectLightIndex].attenuation.x;
            #else
                    float oneOverRangeSq = _AdditionalLightsAttenuation[perObjectLightIndex].x;
            #endif
                    own = oneOverRangeSq <= 0.0 || oneOverRangeSq * (FR_OWN_LIGHT_RANGE * FR_OWN_LIGHT_RANGE) >= 1.0;
                }
                return own;
            }

            // UniversalFragmentPBR (URP 17.3 Lighting.hlsl) with FROwnLight on the additional lights.
            half4 FROwnLightsPBR(InputData inputData, SurfaceData surfaceData)
            {
                bool specularHighlightsOff = false;
                BRDFData brdfData;
                InitializeBRDFData(surfaceData, brdfData);
            #if defined(DEBUG_DISPLAY)
                half4 debugColor;
                if (CanDebugOverrideOutputColor(inputData, surfaceData, brdfData, debugColor))
                    return debugColor;
            #endif
                BRDFData brdfDataClearCoat = CreateClearCoatBRDFData(surfaceData, brdfData);
                half4 shadowMask = CalculateShadowMask(inputData);
                AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
                uint meshRenderingLayers = GetMeshRenderingLayer();
                Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
                MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);
                LightingData lightingData = CreateLightingData(inputData, surfaceData);
                lightingData.giColor = GlobalIllumination(brdfData, brdfDataClearCoat, surfaceData.clearCoatMask,
                                                          inputData.bakedGI, aoFactor.indirectAmbientOcclusion, inputData.positionWS,
                                                          inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV);
            #ifdef _LIGHT_LAYERS
                if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
            #endif
                    lightingData.mainLightColor = LightingPhysicallyBased(brdfData, brdfDataClearCoat, mainLight,
                                                                          inputData.normalWS, inputData.viewDirectionWS,
                                                                          surfaceData.clearCoatMask, specularHighlightsOff);
            #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
            #if USE_CLUSTER_LIGHT_LOOP
                [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                    Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
            #ifdef _LIGHT_LAYERS
                    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            #endif
                        lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
                                                                                      inputData.normalWS, inputData.viewDirectionWS,
                                                                                      surfaceData.clearCoatMask, specularHighlightsOff);
                }
            #endif
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    if (FROwnLight(lightIndex, inputData.positionWS))
                    {
                        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
            #ifdef _LIGHT_LAYERS
                        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            #endif
                            lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
                                                                                          inputData.normalWS, inputData.viewDirectionWS,
                                                                                          surfaceData.clearCoatMask, specularHighlightsOff);
                    }
                LIGHT_LOOP_END
            #endif
            #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                lightingData.vertexLightingColor += inputData.vertexLighting * brdfData.diffuse;
            #endif
            #if REAL_IS_HALF
                return min(CalculateFinalColor(lightingData, surfaceData.alpha), HALF_MAX);
            #else
                return CalculateFinalColor(lightingData, surfaceData.alpha);
            #endif
            }
        #endif

        #if FR_DETAIL && defined(_FR_OWN_LIGHTS)
            // FROwnLightsPBR with the detail kinds' micro-shadowing on every light it keeps (surface_detail stage Rooms: the Run! rooms'
            // gated twins, FrontRoomsRunGate, and the stairwell's Stair_* materials). Today (_FR_Detail.x 1): FROwnLightsPBR itself.
            half4 FRDetailOwnPBR(InputData inputData, SurfaceData surfaceData, FRDetailOut o)
            {
                bool specularHighlightsOff = false;
                BRDFData brdfData;
                InitializeBRDFData(surfaceData, brdfData);
            #if defined(DEBUG_DISPLAY)
                half4 debugColor;
                if (CanDebugOverrideOutputColor(inputData, surfaceData, brdfData, debugColor))
                    return debugColor;
            #endif
                BRDFData brdfDataClearCoat = CreateClearCoatBRDFData(surfaceData, brdfData);
                half4 shadowMask = CalculateShadowMask(inputData);
                AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
                uint meshRenderingLayers = GetMeshRenderingLayer();
                Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
                MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);
                LightingData lightingData = CreateLightingData(inputData, surfaceData);
                lightingData.giColor = GlobalIllumination(brdfData, brdfDataClearCoat, surfaceData.clearCoatMask,
                                                          inputData.bakedGI, aoFactor.indirectAmbientOcclusion, inputData.positionWS,
                                                          inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV);
                half ao = surfaceData.occlusion;
            #ifdef _LIGHT_LAYERS
                if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
            #endif
                    lightingData.mainLightColor = LightingPhysicallyBased(brdfData, brdfDataClearCoat, mainLight,
                                                                          inputData.normalWS, inputData.viewDirectionWS,
                                                                          surfaceData.clearCoatMask, specularHighlightsOff)
                                                * FRDetailMicroShadow(saturate(dot(inputData.normalWS, mainLight.direction)), ao, o.micro);
            #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
            #if USE_CLUSTER_LIGHT_LOOP
                [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                    Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
            #ifdef _LIGHT_LAYERS
                    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            #endif
                        lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
                                                                                      inputData.normalWS, inputData.viewDirectionWS,
                                                                                      surfaceData.clearCoatMask, specularHighlightsOff)
                                                            * FRDetailMicroShadow(saturate(dot(inputData.normalWS, light.direction)), ao, o.micro);
                }
            #endif
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    if (FROwnLight(lightIndex, inputData.positionWS))
                    {
                        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
            #ifdef _LIGHT_LAYERS
                        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            #endif
                            lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
                                                                                          inputData.normalWS, inputData.viewDirectionWS,
                                                                                          surfaceData.clearCoatMask, specularHighlightsOff)
                                                                * FRDetailMicroShadow(saturate(dot(inputData.normalWS, light.direction)), ao, o.micro);
                    }
                LIGHT_LOOP_END
            #endif
            #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                lightingData.vertexLightingColor += inputData.vertexLighting * brdfData.diffuse;
            #endif
            #if REAL_IS_HALF
                return min(CalculateFinalColor(lightingData, surfaceData.alpha), HALF_MAX);
            #else
                return CalculateFinalColor(lightingData, surfaceData.alpha);
            #endif
            }

            half4 FRDetailOwnLit(InputData inputData, SurfaceData surfaceData, FRDetailOut o)
            {
                UNITY_BRANCH
                if (_FR_Detail.x < 0.5)
                    return FRDetailOwnPBR(inputData, surfaceData, o);
                return FROwnLightsPBR(inputData, surfaceData);
            }
            #define FR_DETAIL_OWN_LIT(i, s, d) FRDetailOwnLit(i, s, d)
        #else
            #define FR_DETAIL_OWN_LIT(i, s, d) FROwnLightsPBR(i, s)
        #endif

        #if defined(_FR_SHEEN)
            // The light this point receives (a white, rough, non-metal copy through URP's own loop: every light, its
            // shadow and the ambient), brightened at grazing view angles: twill catching light along its silhouette.
            half3 FRSheen(InputData inputData, SurfaceData surfaceData, half weight)
            {
                if (_Sheen <= 0.0h || weight <= 0.0h) return half3(0, 0, 0);
                SurfaceData white = surfaceData;
                white.albedo = half3(1, 1, 1);
                white.metallic = 0;
                white.specular = half3(0, 0, 0);
                white.smoothness = 0;
                white.emission = half3(0, 0, 0);
                half3 received = UniversalFragmentPBR(inputData, white).rgb;
                half nv = saturate(dot(inputData.normalWS, inputData.viewDirectionWS));
                half grazing = (1.0h - nv) * (1.0h - nv);
                grazing *= grazing;
                return received * (_Sheen * weight * grazing) * half3(0.86h, 0.90h, 0.88h);
            }
        #endif

        #if defined(_FR_TRANSLUCENT) && !defined(SHADER_API_GLES3) && !defined(SHADER_API_WEBGPU)
            // Thin translucent laminate (structures G2-b, SL-A): one light's share through the sheet. nBack is the BACK face's
            // normal; the light reaching it is diffused by the glass mat (a wrapped lobe: the sheet scatters light round its own
            // thickness) and a little goes on forward along the view (a lamp straight behind the sheet glows through it).
            half3 FRTransOne(Light light, half3 nBack, half3 v, half wrap, half fwd, half fwdPow)
            {
                half att = light.distanceAttenuation * light.shadowAttenuation;
                half d = saturate((dot(nBack, light.direction) + wrap) / (1.0h + wrap));
                half f = fwd * pow(saturate(dot(-v, light.direction)), fwdPow);
                return light.color * (att * (d + f));
            }

            // What comes through: every lamp (with its shadow) and the ambient on the back face, tinted by the laminate
            // (_TransTint) and scaled by its transmittance (_TransParams.x). Added to the front's own lighting in Frag; above the
            // water only (the tube is never under it).
            half3 FRTranslucent(InputData inputData, half occlusion)
            {
                half amount = (half)_TransParams.x;
                if (amount <= 0.0h) return half3(0, 0, 0);
                half3 nb = -inputData.normalWS;
                half3 v = inputData.viewDirectionWS;
                half wrap = (half)_TransParams.y;
                half fwd = (half)_TransParams.z;
                half fwdPow = max((half)_TransParams.w, 1.0h);
                half4 shadowMask = CalculateShadowMask(inputData);
                AmbientOcclusionFactor aoFactor;
                aoFactor.indirectAmbientOcclusion = 1.0h;
                aoFactor.directAmbientOcclusion = 1.0h;
                uint meshRenderingLayers = GetMeshRenderingLayer();
                half3 sum = SampleSH(nb) * occlusion;
                Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
            #ifdef _LIGHT_LAYERS
                if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
            #endif
                    sum += FRTransOne(mainLight, nb, v, wrap, fwd, fwdPow);
            #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
            #if USE_CLUSTER_LIGHT_LOOP
                [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                    Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
            #ifdef _LIGHT_LAYERS
                    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            #endif
                        sum += FRTransOne(light, nb, v, wrap, fwd, fwdPow);
                }
            #endif
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
            #ifdef _LIGHT_LAYERS
                    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            #endif
                        sum += FRTransOne(light, nb, v, wrap, fwd, fwdPow);
                LIGHT_LOOP_END
            #endif
                return sum * (half3)_TransTint.rgb * amount;
            }
        #endif

        #if FR_SCALE
            // ---------------------------------------------------------------- structures_scale_terms (P18)
            // The banal kit at the wrong scale (20_research.md C.3.3 S3 and C.3.4 S4): scene 4's walls are 14.6 m tall (V4), 39 m long
            // (SP3's spine) or 11.9 m (SP4's stack), where one roll of Red's C+2 paper repeats as a texture would. Each term has a real
            // cause and runs only where _FR_ScaleTerms turns it on.

            // A line of width w (m) centred at d = 0, faded to its area mean once it is under a pixel (px m): K7's filter.
            float FRScaleLine(float d, float w, float px)
            {
                return saturate((0.5 * w + 0.5 * px - abs(d)) / px) * saturate(w / px);
            }

            // The metres one pixel spans at p (the frustum's height over the screen's at this distance, stretched by the view's
            // slant), as FRDetailSurface takes it: from the camera, never from derivatives.
            float FRScalePx(float3 p, float3 nGeo)
            {
                float3 toCam = _WorldSpaceCameraPos.xyz - p;
                float dist = max(length(toCam), 1e-4);
                float ndv = max(abs(dot(nGeo, toCam / dist)), 0.2);
                return max(dist * 2.0 / (max(abs(UNITY_MATRIX_P._m11), 1e-3) * max(_ScreenParams.y, 1.0)) / ndv, 1e-5);
            }

            // A wall's lot (its UV1.x, a hash of the wall's line) as a tone step 1.5-2.5 % lighter or darker with a small hue shift:
            // every production run has its own shade and installers keep one lot to a wall (F16-F18), so the step falls at corners.
            half3 FRScaleLot(float lot)
            {
                float h = frac(lot * 7.31 + 0.137);
                float s = (lot < 0.5 ? -1.0 : 1.0) * (0.015 + 0.010 * h);
                float hue = (frac(lot * 13.7 + 0.71) - 0.5) * 0.012;
                return half3(1.0 + s + hue, 1.0 + s, 1.0 + s - hue);
            }

            // Ask 4 C: over the wall's foot (UV1.y) each roll length starts the next roll, hung a few mm off the pattern (the match
            // slip: up 2-6 mm, along +-1.5 mm, by roll). Returns the slip (m: along the wall, up) and the roll's index (0 = the first).
            float2 FRScaleSlip(float2 wall, float y, out float rollK)
            {
                float roll = max(_FR_ScaleTerms.z, 1.0);
                rollK = max(floor((y - wall.y) / roll), 0.0);
                if (rollK < 1.0) return float2(0.0, 0.0);
                float a = frac(sin(wall.x * 91.7 + rollK * 12.9898) * 43758.5453);
                float b = frac(sin(wall.x * 47.3 + rollK * 78.233) * 24634.6345);
                return float2((a - 0.5) * 0.003, 0.002 + 0.004 * b);
            }

            // The butt join's line: dust in the 1.5 mm joint (darker) and the upper sheet's edge lifted over it (a lighter line just
            // above), filtered by the footprint; 0 below the first roll's top.
            half FRScaleSeam(float2 wall, float y, float px, out half lift)
            {
                float roll = max(_FR_ScaleTerms.z, 1.0);
                float r = (y - wall.y) / roll;
                float k = round(r);
                lift = 0.0h;
                if (k < 1.0) return 0.0h;
                float d = (r - k) * roll;
                lift = (half)FRScaleLine(d - 0.0018, 0.0015, px);
                return (half)FRScaleLine(d, 0.0015, px);
            }

        #if FR_DETAIL
            // The lay-in ceiling seen from far below (kind 3; C.3.3 S3, K7): past about 1 cm a pixel the map's drawn T-bar (24 mm, at
            // every 0.6 m of the map's 1.2 m tile) aliases on the mips into moire; there the grid is drawn again as a line filtered by
            // the footprint over the panel's own colour (the map at the panel's centre at a quarter-panel texel, where no bar is), the
            // bar's colour from the map on the bar. Only on a lid more than about 4 m over the eye (V4's 14.6 m lid, the slot hall's
            // 10.7, a well's lid), so every 2.9 m room draws as today. Both lay-in maps are 2048 px a tile (the LOD assumes it).
            void FRScaleTBar(float3 p, float3 nGeo, float2 m, inout half4 albedo, inout half4 mask, inout half3 nTS)
            {
                float px = FRScalePx(p, nGeo);
                float w = smoothstep(3.5, 4.5, p.y - _WorldSpaceCameraPos.y) * smoothstep(0.008, 0.016, px);
                UNITY_BRANCH
                if (w <= 0.0) return;
                const float TS = 0.6, BAR = 0.0238;
                float2 cf = floor(m / TS);
                float2 g = m / TS - cf;
                float2 e = min(g, 1.0 - g) * TS;
                float cov = max(FRScaleLine(e.x, BAR, px), FRScaleLine(e.y, BAR, px));
                float texel0 = _TileSize.x / max(_BaseMap_ST.x, 1e-3) / 2048.0;
                float2 toUV = _BaseMap_ST.xy / max(_TileSize.xy, 1e-3);
                float2 uvPanel = (cf + 0.5) * TS * toUV + _BaseMap_ST.zw;
                float2 mb = e.x < e.y ? float2(round(m.x / TS) * TS, m.y) : float2(m.x, round(m.y / TS) * TS);
                float2 uvBar = mb * toUV + _BaseMap_ST.zw;
                float lodPanel = log2(max(TS * 0.25 / texel0, 1.0));
                float lodBar = log2(max(BAR * 0.25 / texel0, 1.0));
                half4 aP = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, uvPanel, lodPanel);
                half4 aB = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, uvBar, lodBar);
                half4 mP = SAMPLE_TEXTURE2D_LOD(_MaskMap, sampler_MaskMap, uvPanel, lodPanel);
                half4 mB = SAMPLE_TEXTURE2D_LOD(_MaskMap, sampler_MaskMap, uvBar, lodBar);
                half c = (half)cov;
                half k = (half)w;
                albedo = lerp(albedo, lerp(aP, aB, c) * _BaseColor, k);
                mask = lerp(mask, lerp(mP, mB, c), k);
                nTS = normalize(lerp(nTS, half3(0.0h, 0.0h, 1.0h), k));
            }
        #endif
        #endif

            void Frag(Varyings input, out half4 outColor : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out uint outRenderingLayers : SV_Target1
            #endif
            )
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 nGeo = normalize(input.normalWS);
                // Submerged: the body of water this point lies under or above (body -1 everywhere outside a pool zone). The
                // footprint is taken here, in uniform control flow, for the caustics' mip.
                float subPx = max(length(ddx(input.positionWS)), length(ddy(input.positionWS)));
                FRSub sub = FRSubFind(input.positionWS, subPx);
                float2 metres;
                float3 t, b;
            #if defined(_FR_MESH_UV)
                metres = input.uv;
                t = normalize(input.tangentWS.xyz);
                b = input.tangentWS.w * cross(nGeo, t);
            #else
                PlanarFrame(input.positionWS, nGeo, metres, t, b);
                if (_FR_PlanarSnap > 0.5)
                {
                    // UVs from the snapped frame; the TBN (t, b, nGeo) keeps the real normal's frame for lighting.
                    float3 st, sb;
                    PlanarFrame(input.positionWS, SnapToAxis(nGeo), metres, st, sb);
                }
            #endif
                float2 uv = metres / max(_TileSize.xy, 1e-3) * _BaseMap_ST.xy + _BaseMap_ST.zw;
            #if FR_SCALE && defined(_FR_PRINT)
                // structures_scale_terms (ask 4 C): above each butt join the next roll, a few mm off the pattern
                float rollK = 0.0;
                UNITY_BRANCH
                if (_FR_ScaleTerms.x > 1.5 && input.wall.x > 0.0)
                    uv += FRScaleSlip(input.wall, input.positionWS.y, rollK) * _BaseMap_ST.xy / max(_TileSize.xy, 1e-3);
            #endif

            #if defined(_FR_PRINT)
                // Paper x print. Everything below (wear, stains, grime) sits over the ink. The cue
                // takes the face from the same frame as the UVs (snapped on wall-match outlet plates).
                float3 nFace = _FR_PlanarSnap > 0.5 ? SnapToAxis(nGeo) : nGeo;
                FRHung hung = HungPaper(uv, input.positionWS, nFace);
                half4 albedo = half4(PrintAlbedo(uv, input.positionWS, nFace, hung), 1.0h) * _BaseColor;
                half4 mask = SAMPLE_TEXTURE2D_GRAD(_MaskMap, sampler_MaskMap, hung.paperUV, hung.dx, hung.dy);
                half3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(_BumpMap, sampler_BumpMap, hung.paperUV, hung.dx, hung.dy), _BumpScale);
                nTS.xy *= 1.0h + (half)(_StripVar.w * (hung.hStrip * 2.0 - 1.0));
                // The emboss has to read past 2 m, where the wall's soft light no longer picks out a tilt this small, so its
                // relief also goes into the tone: the top of each thread or grain lighter, the underside darker.
                albedo.rgb *= 1.0h + _EmbossShade * nTS.y;
            #elif defined(_FR_TILE)
                // Pool tile: data maps, the material's colours, per-tile shade and set (FRTileSurface); the real look while
                // _FR_TileLook.x is on (FRTileSurfaceReal: FrontRoomsPoolTile.SetLook; off = today, bit-identical).
                half4 albedo, mask;
                half3 nTS;
                half tilePorous;
            #if FR_TILE_REAL
                half tileLobes = 0.0h;
                float tileLostVar = 0.0;
                UNITY_BRANCH
                if (_FR_TileLook.x > 0.5)
                    FRTileSurfaceReal(uv, nGeo, input.positionWS, albedo, mask, nTS, tilePorous, tileLobes, tileLostVar);
                else
            #endif
                    FRTileSurface(uv, nGeo, albedo, mask, nTS, tilePorous);
            #else
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
                half4 mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uv);
                half3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);
            #if FR_SCALE && FR_DETAIL
                // structures_scale_terms (K7): the lay-in ceiling's T-bar as a footprint line in tall rooms (scene 4's NEW only)
                UNITY_BRANCH
                if (_FR_ScaleTerms.y > 0.5 && _DetailKind.x > 2.5 && _DetailKind.x < 3.5 && nGeo.y < -0.5)
                    FRScaleTBar(input.positionWS, nGeo, metres, albedo, mask, nTS);
            #endif
            #endif

            #if FR_SCALE
                // structures_scale_terms (Red's ask 4; off unless Red's key turns it on): the wall's lot on the paper and on painted
                // board (kind 1); on the paper above one roll, the butt join's line and the next roll's own shade
                UNITY_BRANCH
                if (_FR_ScaleTerms.x > 0.5 && input.wall.x > 0.0)
                {
                #if defined(_FR_PRINT)
                    albedo.rgb *= FRScaleLot(input.wall.x);
                    UNITY_BRANCH
                    if (_FR_ScaleTerms.x > 1.5 && rollK > 0.5)
                    {
                        half lift;
                        half gap = FRScaleSeam(input.wall, input.positionWS.y, FRScalePx(input.positionWS, nGeo), lift);
                        albedo.rgb *= (1.0h - 0.45h * gap) * (1.0h + 0.08h * lift) * (half)(1.0 + 0.016 * (frac(rollK * 0.618 + input.wall.x) - 0.5));
                    }
                #elif FR_DETAIL
                    if (_DetailKind.x > 0.5 && _DetailKind.x < 1.5) albedo.rgb *= FRScaleLot(input.wall.x);
                #endif
                }
            #endif

            #if defined(_FR_LEAFWEAR) && defined(_FR_MESH_UV)
                // Zone doors' leaf wear (20_design A6): the leaf's own handling marks, in its mesh-UV metres (the left and the
                // right leaf's materials mirror u, so u 1 is always the meeting edge). Taken in uniform control flow.
                half4 leafWear = SAMPLE_TEXTURE2D(_FR_LeafWearMap, sampler_FR_LeafWearMap, (metres - _FR_LeafWearRect.xy) * _FR_LeafWearRect.zw);
                albedo.rgb *= 1.0h - 0.35h * leafWear.r;
                albedo.rgb = lerp(albedo.rgb, _FR_LeafWearChip.rgb, leafWear.a);
            #endif

                // Large-scale wear in world space (two scales, the second turned
                // 90 degrees). Both periods divide 192 m.
                float2 wearUV;
                float3 wt, wb;
                PlanarFrame(input.positionWS, _FR_PlanarSnap > 0.5 ? SnapToAxis(nGeo) : nGeo, wearUV, wt, wb);
                half4 m1 = SAMPLE_TEXTURE2D(_MacroMap, sampler_MacroMap, wearUV / 8.0);
                half4 m2 = SAMPLE_TEXTURE2D(_MacroMap, sampler_MacroMap, float2(wearUV.y, -wearUV.x) / 12.8);
                half tone = lerp(1.0h, 0.84h + 0.30h * m1.r * m2.r, _MacroTone);
                half dirt = 1.0h - _MacroDirt * smoothstep(0.55h, 0.95h, m1.g * 0.6h + m2.g * 0.4h);
                albedo.rgb *= tone * dirt;

                // Water stains: soft tide-mark rings where the broad wear field crosses a band.
                // Only the top of the broad field stains, so tide marks are
                // occasional patches, not a ceiling-wide wash.
                half ring = smoothstep(0.70h, 0.73h, m2.r) * (1.0h - smoothstep(0.73h, 0.80h, m2.r));
                half pool = smoothstep(0.74h, 0.88h, m2.r);
                albedo.rgb = lerp(albedo.rgb, albedo.rgb * _StainColor.rgb * 1.6h, saturate((ring * 0.9h + pool * 0.35h) * _StainStrength));

                half smoothness = mask.r * _Smoothness;
                half cavity = lerp(1.0h, mask.g, _OcclusionStrength);
            #if defined(_FR_LEAFWEAR) && defined(_FR_MESH_UV)
                smoothness = smoothness * (1.0h - 0.5h * leafWear.g) * (1.0h - 0.3h * leafWear.a) + 0.20h * leafWear.b;
            #endif

            #if FR_DETAIL
                // Surface detail (FRDetailSurface); off (FrontRoomsSurfaceDetail.Today: _FR_Detail.x 1) every term keeps today's value.
                FRDetailOut detail = (FRDetailOut)0;
                UNITY_BRANCH
                if (_FR_Detail.x < 0.5)
                    FRDetailSurface(input.positionWS, nGeo, metres, t, b, m1, m2, albedo.rgb, smoothness, cavity, nTS, detail);
            #endif

                // Damp carpet: darker, flatter, glossier.
                half wet = smoothstep(0.35h, 0.85h, m1.b * 0.7h + m2.b * 0.5h) * _WetStrength;
                albedo.rgb *= 1.0h - 0.32h * wet;
                smoothness = lerp(smoothness, 0.62h, wet);
                nTS = normalize(lerp(nTS, half3(0, 0, 1), wet * 0.7h));

                // Walls: dirt picked up along the floor, streaks from the ceiling grid.
                half wall = 1.0h - saturate(abs(nGeo.y) * 2.0h);
                half y = (half)input.positionWS.y;
            #if defined(_FR_PRINT)
                y += (half)((hung.hStrip - 0.5) * 2.0 * _WearSheen.w);       // each sheet took the floor's dirt its own way
            #endif
                half floorBand = 1.0h - smoothstep(0.0h, (half)_FloorGrimeHeight, y);
                half ceilBand = smoothstep((half)_CeilingHeight - 1.6h, (half)_CeilingHeight, y);
                half streak = m1.a * ceilBand;
                albedo.rgb *= 1.0h - wall * (_FloorGrime * 0.35h * floorBand * (0.6h + 0.4h * m1.g));
                albedo.rgb = lerp(albedo.rgb, albedo.rgb * _StainColor.rgb * 1.5h, wall * saturate(_CeilingGrime * (streak * 0.8h + ceilBand * 0.15h)));

            #if defined(_FR_PRINT)
                // Paper: the strip's own gloss, the seam, the wall under the paper, and wear that changes the sheen
                // (the print stays albedo-only; none of this reads it).
                half damp = smoothstep(0.45h, 0.80h, m1.b * 0.7h + m2.b * 0.5h);
                half dull = saturate((1.0h - dirt) * 3.0h + floorBand * (0.6h + 0.4h * m1.g) * _FloorGrime + streak * _CeilingGrime + ring * _StainStrength * 3.0h);
                smoothness += (half)(_StripVar.z * (hung.hStrip * 2.0 - 1.0));
                smoothness -= (half)_WearSheen.x * dull + (half)_WearSheen.y * damp * (0.5h + 0.5h * m2.g);
                half seamScale = 1.0h;
                HungSeam(hung, damp, seamScale, smoothness, cavity, nTS);
                albedo.rgb *= seamScale;
            #if defined(FR_SUBSTRATE)
                half3 subN = UnpackNormalScale(SAMPLE_TEXTURE2D(_SubstrateMap, sampler_BumpMap, wearUV / FR_SUBSTRATE_TILE), _SubstrateScale);
                nTS = BlendNormalRNM(normalize(nTS), subN);
                // Every lamp is overhead and most of the wall's light is soft, so a tilt alone barely shows past 2 m.
                // The same relief goes into the tone, as a texture artist bakes form: the upper side of a bump a little
                // lighter, the lower side a little darker. It multiplies, so a dead lamp still shows nothing of its own.
                albedo.rgb *= 1.0h + _SubstrateShade * subN.y;
            #endif
            #endif

                // Wet band at _WaterY (FRWetBand): off unless a pool material or a water stage sets the water's height.
            #if defined(_FR_TILE)
                half wetPorous = tilePorous;
            #else
                half wetPorous = saturate((0.55h - smoothness) / 0.4h);
            #endif
                // (soft room faces, structures G2-a: only inside the room's own box, never on its ramp outside it; 1 with the hard test)
                float wetY = (_WaterY < -999.0 && sub.body >= 0 && sub.roomW > 0.5) ? sub.waterY : _WaterY;
                FRWetBand(input.positionWS, metres.x, nGeo, wetPorous, wetY, albedo.rgb, smoothness, nTS);

            #if defined(_FR_DAMAGE)
                // Door break onset mask, in the leaf's object space (canonical: +X = the burst face). Left half of the
                // map = the burst face, right half = the push face; each covers Z z0..z0+dz x Y y0..y0+dy.
                half4 dmg = 0;
                half dmgM = 0;
                float2 dmgUV = 0;
                {
                    float3 dPos = TransformWorldToObject(input.positionWS);
                    float3 dNrm = TransformWorldToObjectDir(nGeo);
                    float2 r = float2((dPos.z - _FR_DamageRect.x) * _FR_DamageRect.z, (dPos.y - _FR_DamageRect.y) * _FR_DamageRect.w);
                    if (abs(dNrm.x) > 0.85 && r.x > 0.0 && r.y > 0.0 && r.x < 1.0 && r.y < 1.0)
                    {
                        dmgUV = float2((r.x + (dNrm.x > 0.0 ? 0.0 : 1.0)) * 0.5, r.y);
                        dmg = SAMPLE_TEXTURE2D(_FR_DamageMap, sampler_FR_DamageMap, dmgUV);
                        half noise = FRDamageNoise(dPos.zy * 900.0);
                        dmgM = dmg.r > 0.002h ? saturate((_FR_DamageStage - dmg.r) * 12.0h + 1.0h + (noise - 0.5h) * 0.6h) : 0.0h;
                    }
                }
                // crushed and lifted veneer reads lighter and matte (never dark ink, 21_C F2); the fines wipe toward their colour
                albedo.rgb = lerp(albedo.rgb, albedo.rgb * 1.28h + 0.025h, dmg.b * dmgM);
                albedo.rgb = lerp(albedo.rgb, _FR_DamageFines.rgb, dmg.a * dmgM * 0.55h);
                smoothness *= 1.0h - 0.6h * dmg.b * dmgM;
                cavity *= 1.0h - 0.5h * saturate((0.5h - dmg.g) * 2.0h) * dmgM;
            #endif

            #if defined(_FR_DEBUG_VIEW)
                // Wear masks and albedo, unlit (the same expressions as above).
                if (_FR_DebugView > 2.5)
                {
                    half3 dbg = albedo.rgb;
                    if (_FR_DebugView < 3.5)
                        dbg = half3(saturate((ring * 0.9h + pool * 0.35h) * _StainStrength),
                                    wall * (_FloorGrime * 0.35h * floorBand * (0.6h + 0.4h * m1.g)),
                                    wall * saturate(_CeilingGrime * (streak * 0.8h + ceilBand * 0.15h)));
                    else if (_FR_DebugView < 4.5)
                        dbg = half3(tone, dirt, wet);
                    outColor = half4(dbg, 1);
                #ifdef _WRITE_RENDERING_LAYERS
                    outRenderingLayers = EncodeMeshRenderingLayer();
                #endif
                    return;
                }
            #endif

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                half3x3 tbn = half3x3(t, b, nGeo);
                inputData.normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(nTS, tbn));
            #if defined(_FR_DAMAGE)
                if (dmgM > 0.0h)
                {
                    // the dish and the crush: the map's frame is U along the leaf's +Z, V along +Y on either face; the
                    // depth reads deeper stage by stage (M 2, D1 4, D2 6 mm of the map's 6 mm)
                    half3 dn = UnpackNormal(SAMPLE_TEXTURE2D(_FR_DamageNormal, sampler_FR_DamageMap, dmgUV));
                    float3 tW = TransformObjectToWorldDir(float3(0, 0, 1));
                    float3 bW = TransformObjectToWorldDir(float3(0, 1, 0));
                    half k = dmgM * saturate(_FR_DamageStage * 1.34h);
                    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS + (tW * dn.x + bW * dn.y) * k);
                }
            #endif
                inputData.tangentToWorld = tbn;
            #if FR_TILE_REAL
                // the real look's specular AA: the mapped normal's change per pixel (taken here, in uniform control flow)
                float3 tileNdx = ddx(inputData.normalWS);
                float3 tileNdy = ddy(inputData.normalWS);
                float tileAddVar = 2.0 * _FR_TileAA.x * (dot(tileNdx, tileNdx) + dot(tileNdy, tileNdy)) + tileLostVar;
            #endif
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
            #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
            #else
                inputData.shadowCoord = float4(0, 0, 0, 0);
            #endif
                inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogAndVertexLight.x);
                inputData.vertexLighting = input.fogAndVertexLight.yzw;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData s = (SurfaceData)0;
                s.albedo = albedo.rgb;
                s.metallic = _Metallic;
                s.specular = half3(0, 0, 0);
                s.smoothness = saturate(smoothness);
                s.normalTS = nTS;
                s.occlusion = cavity;
                s.alpha = 1;
            #if defined(_FR_SHEEN)
                s.metallic = _Metallic * mask.b;
            #endif
            #if defined(_EMISSION)
            #if defined(_FR_LAMP_FIELD)
                half4 lampField = SAMPLE_TEXTURE2D(_LampFieldMap, sampler_LampFieldMap, uv);
                s.emission = dot(lampField.rgb, (half3)_LampField.rgb) * lampField.a * _EmissionColor.rgb;
            #else
                s.emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb * _EmissionColor.rgb;
            #endif
            #endif

            #if defined(LOD_FADE_CROSSFADE)
                // This level's share of the LOD dither (unity_LODFade, the URP asset's Blue Noise), after every texture read
                // of the surface (their derivatives need the whole quad) and before the lighting, where URP's Lit takes it
                // (LitForwardPass.hlsl).
                LODFadeCrossFade(input.positionCS);
            #endif

            #if defined(_FR_DEBUG_VIEW)
                half4 color;
                if (_FR_DebugView > 0.5)
                    color = FRDebugLobePBR(inputData, s, _FR_DebugView < 1.5);   // one lobe, no fog
                else
                    color = half4(FRHeightFog(MixFog(UniversalFragmentPBR(inputData, s).rgb, inputData.fogCoord), input.positionWS), 1);
            #elif defined(_FR_OWN_LIGHTS)
                half4 color = FR_DETAIL_OWN_LIT(inputData, s, detail);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.rgb = FRHeightFog(color.rgb, input.positionWS);
            #else
                half4 color;
                // Submerged: under the water the light comes through it (FRSubmergedPBR); above it, in a pool room, the wet
                // niches' thrown light adds. Both off (sub.body -1) outside a pool zone.
                UNITY_BRANCH
                if (sub.under)
                    color = half4(FRSubmergedPBR(inputData, s, sub), 1.0h);
            #if FR_TILE_REAL
                else if (_FR_TileLook.x > 0.5)
                    color = FRTilePBR(inputData, s, tileLobes, tileAddVar);
            #endif
                else
                    color = FR_DETAIL_LIT(inputData, s, detail);
            #if defined(_FR_SHEEN)
                if (!sub.under)
                    color.rgb += FRSheen(inputData, s, mask.a);
            #endif
            #if defined(_FR_TRANSLUCENT) && !defined(SHADER_API_GLES3) && !defined(SHADER_API_WEBGPU)
                if (!sub.under)
                    color.rgb += FRTranslucent(inputData, s.occlusion);
            #endif
                UNITY_BRANCH
                if (sub.room)
                    color.rgb += s.albedo * (0.96h * s.occlusion) * FRSubThrown(input.positionWS, inputData.normalWS, sub);
                // the room lit by its water (FRSubWaterLight: the lamps the water mirrors, the light it sends back in its
                // own colour); off unless FrontRoomsWaterSubmerged's statics turn it on
                UNITY_BRANCH
                if (sub.room && _FR_SubWL.w > 0.5)
                {
                    // occluded like the ambient it replaces (URP takes the lesser of the material's and SSAO's indirect
                    // occlusion): otherwise a corner SSAO has darkened loses the full unoccluded ambient and goes black
                    half wlOcc = s.occlusion;
                #if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT)
                    wlOcc = min(wlOcc, GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV).indirectAmbientOcclusion);
                #endif
                    color.rgb = max(color.rgb + s.albedo * (0.96h * wlOcc) * FRSubWaterLight(inputData, sub), 0.0h);
                }
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.rgb = FRHeightFog(color.rgb, input.positionWS);
            #endif
                color.a = 1;
                outColor = color;
            #ifdef _WRITE_RENDERING_LAYERS
                outRenderingLayers = EncodeMeshRenderingLayer();
            #endif
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowPassVertex
            #pragma fragment FRShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"

            // A fading caster draws no dither into a shadow map: its two levels hand over at mid-fade instead (unity_LODFade.x
            // is x for one level and -x for the other, so exactly one of them casts at any moment), so nothing in a shadow can
            // crawl as the cascades step or the shadow atlas re-packs. Without the keyword this is URP's ShadowPassFragment.
            half4 FRShadowPassFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
            #if defined(LOD_FADE_CROSSFADE)
                clip(unity_LODFade.x - (unity_LODFade.x >= 0.0 ? 0.5 : -0.5));
            #endif
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }

        // Object motion vectors, as URP's Lit (ObjectMotionVectors.hlsl, LOD cross-fade like the passes above; nothing here
        // clips, so no alpha test). Drawn only for a camera that asks for motion vectors (STP, TAA, motion blur); the desktop
        // asks for none (MSAA, no TAA, no motion blur), so there it never draws. It is the handheld STP's prerequisite (tech_art
        // row 13b): FrontRoomsQuality turns STP on only while this shader has a MotionVectors pass.
        Pass
        {
            Name "MotionVectors"
            Tags { "LightMode" = "MotionVectors" }
            ColorMask RG
            Cull [_Cull]

            HLSLPROGRAM
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ObjectMotionVectors.hlsl"
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
