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
            // Creature cloth (Creature_TwillSpruce, Creature_TapeSilver only). Off: bit-identical.
            #pragma shader_feature_local_fragment _FR_SHEEN
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
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
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

            bool FROwnLight(uint lightIndex)
            {
            #if USE_CLUSTER_LIGHT_LOOP
                int perObjectLightIndex = lightIndex;
            #else
                int perObjectLightIndex = GetPerObjectLightIndex(lightIndex);
            #endif
            #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
                float oneOverRangeSq = _AdditionalLightsBuffer[perObjectLightIndex].attenuation.x;
            #else
                float oneOverRangeSq = _AdditionalLightsAttenuation[perObjectLightIndex].x;
            #endif
                return oneOverRangeSq <= 0.0 || oneOverRangeSq * (FR_OWN_LIGHT_RANGE * FR_OWN_LIGHT_RANGE) >= 1.0;
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
                    if (FROwnLight(lightIndex))
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

            void Frag(Varyings input, out half4 outColor : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out uint outRenderingLayers : SV_Target1
            #endif
            )
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 nGeo = normalize(input.normalWS);
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
            #else
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
                half4 mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uv);
                half3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);
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
                half3 sub = UnpackNormalScale(SAMPLE_TEXTURE2D(_SubstrateMap, sampler_BumpMap, wearUV / FR_SUBSTRATE_TILE), _SubstrateScale);
                nTS = BlendNormalRNM(normalize(nTS), sub);
                // Every lamp is overhead and most of the wall's light is soft, so a tilt alone barely shows past 2 m.
                // The same relief goes into the tone, as a texture artist bakes form: the upper side of a bump a little
                // lighter, the lower side a little darker. It multiplies, so a dead lamp still shows nothing of its own.
                albedo.rgb *= 1.0h + _SubstrateShade * sub.y;
            #endif
            #endif

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

            #if defined(_FR_DEBUG_VIEW)
                half4 color;
                if (_FR_DebugView > 0.5)
                    color = FRDebugLobePBR(inputData, s, _FR_DebugView < 1.5);   // one lobe, no fog
                else
                    color = half4(MixFog(UniversalFragmentPBR(inputData, s).rgb, inputData.fogCoord), 1);
            #elif defined(_FR_OWN_LIGHTS)
                half4 color = FROwnLightsPBR(inputData, s);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
            #else
                half4 color = UniversalFragmentPBR(inputData, s);
            #if defined(_FR_SHEEN)
                color.rgb += FRSheen(inputData, s, mask.a);
            #endif
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
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
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
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
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
