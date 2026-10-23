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
        _Smoothness ("Smoothness scale", Range(0, 2)) = 1
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

        // Required by URP's shadow/depth passes.
        _Cutoff ("Alpha cutoff", Range(0, 1)) = 0.5
        [HideInInspector] _Cull ("__cull", Float) = 2
        // Wall-match outlet plates set this per draw (FrontRoomsWallFixtureRenderer): the world-planar frame
        // follows the normal's dominant axis. 0 everywhere else, so no material changes.
        [HideInInspector] _FR_PlanarSnap ("Planar frame on the dominant axis", Float) = 0
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
            // Exit-sign lamp weights: in every pass, so the SRP batcher layout never changes.
            float4 _LampField;
            // Wall-match outlet plates (per-draw block only): world-planar frame on the normal's dominant axis.
            half _FR_PlanarSnap;
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
            half2 PrintInk(float2 uv, float3 positionWS, float3 nFace)
            {
                float2 rollUV = uv * _PrintTex_ST.xy + _PrintTex_ST.zw;
                float2 puv = PrintUV(rollUV);
                float2 dx = ddx(puv), dy = ddy(puv);
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
                CueInk(puv, ddx(puv), ddy(puv), positionWS, nFace, ink);
            #endif
                return ink;
            }

            half3 PrintAlbedo(float2 uv, float3 positionWS, float3 nFace)
            {
                half3 paper = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb * FR_PAPER_SCALE + FR_PAPER_BIAS;
                half2 ink = PrintInk(uv, positionWS, nFace) * _PrintAmount;
                half3 inkColour = lerp(InkRamp(ink.r), _InkCream.rgb, ink.g * FR_CREAM_MIX);
                return inkColour * paper.x + FR_TOBACCO_LIN * paper.y + paper.z;
            }
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
                half4 albedo = half4(PrintAlbedo(uv, input.positionWS, _FR_PlanarSnap > 0.5 ? SnapToAxis(nGeo) : nGeo), 1.0h) * _BaseColor;
            #else
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
            #endif
                half4 mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uv);
                half3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);

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
                half floorBand = 1.0h - smoothstep(0.0h, (half)_FloorGrimeHeight, y);
                half ceilBand = smoothstep((half)_CeilingHeight - 1.6h, (half)_CeilingHeight, y);
                half streak = m1.a * ceilBand;
                albedo.rgb *= 1.0h - wall * (_FloorGrime * 0.35h * floorBand * (0.6h + 0.4h * m1.g));
                albedo.rgb = lerp(albedo.rgb, albedo.rgb * _StainColor.rgb * 1.5h, wall * saturate(_CeilingGrime * (streak * 0.8h + ceilBand * 0.15h)));

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
            #else
                half4 color = UniversalFragmentPBR(inputData, s);
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
