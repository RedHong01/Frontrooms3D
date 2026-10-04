// FrontRooms glass: URP physically based lighting for clear float glass
// (Documentation/research/interaction_audit/10_audit_report.md §4.2).
//
// Transparent, "Alpha + Preserve Specular": the ROP blend is premultiplied
// (One, OneMinusSrcAlpha) and only the diffuse part is multiplied by alpha, so
// lamp highlights and the environment reflection stay at full strength while the
// pane transmits about 90 % face-on. ZWrite off, no shadow caster (the pane
// throws no shadow), Cull Back on a closed thin box (one face drawn per side).
//
//   reflectance = _PaneF0 (.08): a pane has TWO surfaces (n 1.52: 0.082 face-on, 0.109 at 50°,
//                 0.156 at 60°, 0.385 at 75°), specular setup, so lamp highlights and the
//                 environment both use it; URP's Pow4 Fresnel rises it toward grazing
//   alpha       = absorption + reflectance: window .11 + .89 * Fresnel^5 (+ .25 dust), so the
//                 view behind is dimmed by what the pane reflects (T .89 face-on)
//   base        = linear (.02, .025, .022) -> dust colour where dusty
//   smoothness  = .96 -> .62 under smudges (smudges blur the reflection)
//   normal      = flat + .02 long-wave roll + .05 smudge normal
//
// Grime (keyword _FR_GLASS_GRIME, windows only) is laid out on the pane in
// metres, so any pane size works: dust in the bottom 10-15 cm, in the corners
// and along the glazing stop; smears at 0.9-1.5 m above _FloorY; a few prints
// near the side edges. The pane frame comes from the object: a unit-cube mesh
// (Unity's cube primitive, as the map builds its panes) scaled by its transform,
// or set _PaneSize to the mesh's object-space size if it is modelled in metres.
// The thinnest axis is the pane normal; with _AutoEdge the four thin edge faces
// draw as green float-glass edge.
//
// Crack hooks for the hold-to-break shot (§3.5): _Crack (0-1 reach of a radial
// crack from _ImpactUV, rings appear at .30/.55/.80), _CrackSeed, _Palm (a palm
// smudge at _ImpactUV). All default to 0 and are skipped by a uniform branch.
// This procedural crack is the placeholder until the baked fracture masks (G8).
//
// WebGL 2: no compute, every texture has its own sampler (GLES couples them),
// 2 material variants (grime on/off) on top of URP's lighting keywords.
Shader "FrontRooms/Glass"
{
    Properties
    {
        [MainColor] _BaseColor ("Base colour (sRGB swatch of linear .02/.025/.022)", Color) = (0.152, 0.172, 0.160, 1)
        _AlphaFace ("Alpha face-on", Range(0, 1)) = 0.08
        _AlphaFresnel ("Alpha added at grazing (x Fresnel^5)", Range(0, 1)) = 0.55
        _Smoothness ("Smoothness, clean", Range(0, 1)) = 0.96
        _SmudgeSmoothness ("Smoothness under smudges", Range(0, 1)) = 0.62
        _Metallic ("Metallic (unused: specular setup)", Range(0, 1)) = 0
        _PaneF0 ("Pane reflectance face-on, both surfaces", Range(0, 0.2)) = 0.08
        _RollStrength ("Long-wave roll (normal tilt)", Range(0, 0.1)) = 0.02
        _RollPeriod ("Roll period (m)", Float) = 0.37
        _ReflectionMin ("Reflection at least (linear; 0 = the global intensity)", Range(0, 1)) = 0
        _DustFilm ("Even dust film (props without grime maps)", Range(0, 1)) = 0

        [Toggle(_FR_GLASS_GRIME)] _Grime ("Grime maps (window panes)", Float) = 0
        [NoScaleOffset] _GrimeMap ("Grime (R smear, G prints, B specks, A mottling)", 2D) = "black" {}
        [NoScaleOffset][Normal] _SmearNormal ("Smear normal", 2D) = "bump" {}
        _SmudgeNormal ("Smudge normal strength", Range(0, 0.2)) = 0.05
        _DustColor ("Dust colour (sRGB swatch of linear .42/.40/.34)", Color) = (0.680, 0.665, 0.618, 1)
        _DustAlpha ("Alpha added by dust", Range(0, 1)) = 0.25
        _Dust ("Dust amount", Range(0, 1)) = 1
        _Smear ("Smear amount", Range(0, 1)) = 1
        _Prints ("Fingerprint amount", Range(0, 1)) = 1
        _FloorY ("Floor height, world y (smear band)", Float) = 0
        _PaneSize ("Mesh size in object units (xy); 0 = unit cube", Vector) = (0, 0, 0, 0)
        _Scatter ("Grime scatters room light (both sides)", Range(0, 4)) = 1
        [ToggleUI] _GrimeDebug ("Debug: show masks (R dust, G smudge, B crack)", Float) = 0

        [ToggleUI] _AutoEdge ("Thin edge faces use the edge colour", Float) = 0
        _EdgeColor ("Edge colour (sRGB swatch of linear .28/.42/.34)", Color) = (0.566, 0.680, 0.618, 1)
        _EdgeAlpha ("Edge alpha", Range(0, 1)) = 0.92
        _EdgeSmoothness ("Edge smoothness", Range(0, 1)) = 0.6

        _Crack ("Crack reach (0-1)", Range(0, 1)) = 0
        _ImpactUV ("Impact point, pane uv (0-1)", Vector) = (0.5, 0.5, 0, 0)
        _CrackSeed ("Crack seed", Float) = 0
        _Palm ("Palm smudge (0-1)", Range(0, 1)) = 0

        // [G14-HOOK-BEGIN] receiver flag for the global RT / planar reflection input (window panes only)
        [ToggleUI] _RTReceive ("Takes RT/planar reflection (_FR_GlassRTReflection)", Float) = 0
        // [G14-HOOK-END]

        [HideInInspector] _Cull ("__cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "IgnoreProjector" = "True" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma shader_feature_local_fragment _FR_GLASS_GRIME
            // [G14-HOOK-BEGIN] RT / planar reflection input. A global keyword, so with it off (the default)
            // the compiled code is exactly the code without the hook (a uniform branch alone changed the
            // last bit of ~1e-7 in up to 95k pixels: logs/g14_hook_proof_*). The RT pass enables it only for
            // the camera it feeds; FrontRoomsGlassRTStripper removes the variant from WebGL builds.
            #pragma multi_compile_fragment _ _FR_GLASS_RT
            // [G14-HOOK-END]

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            // As URP Lit: follow the URP asset's probe blending / box projection (G7). URP strips the
            // variants while no URP asset in the build turns them on, so they cost nothing until then.
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #pragma multi_compile_instancing

            // Always transparent with Preserve Specular, specular workflow: plain defines, no variants.
            #define _SURFACE_TYPE_TRANSPARENT 1
            #define _ALPHAPREMULTIPLY_ON 1
            #define _SPECULAR_SETUP 1

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _AlphaFace;
                half _AlphaFresnel;
                half _Smoothness;
                half _SmudgeSmoothness;
                half _Metallic;
                half _PaneF0;
                half _RollStrength;
                float _RollPeriod;
                half _ReflectionMin;
                half _DustFilm;
                half _Scatter;
                half _GrimeDebug;
                half _SmudgeNormal;
                half4 _DustColor;
                half _DustAlpha;
                half _Dust;
                half _Smear;
                half _Prints;
                float _FloorY;
                float4 _PaneSize;
                half _AutoEdge;
                half4 _EdgeColor;
                half _EdgeAlpha;
                half _EdgeSmoothness;
                half _Crack;
                float4 _ImpactUV;
                float _CrackSeed;
                half _Palm;
                // [G14-HOOK-BEGIN]
                half _RTReceive;
                // [G14-HOOK-END]
            CBUFFER_END

            TEXTURE2D(_GrimeMap);    SAMPLER(sampler_GrimeMap);
            TEXTURE2D(_SmearNormal); SAMPLER(sampler_SmearNormal);
            // [G14-HOOK-BEGIN] Globals written by the RT / planar track (VISUAL_CHAT_TASKS G14), never per material.
            #if defined(_FR_GLASS_RT)
            //   _FR_GlassRTReflection  screen-space, linear HDR. RGB = reflected radiance with NO Fresnel and NO
            //                          strength applied (this shader applies its own). A = 0 none; 0 < A <= 1
            //                          coverage; A > 1: 1 + linear eye depth (m) of the glass surface the texel
            //                          belongs to, coverage 1 (a pane at another depth keeps its own reflection).
            //   _FR_GlassRTWeight      0 = today's output, unchanged.
            TEXTURE2D(_FR_GlassRTReflection); SAMPLER(sampler_FR_GlassRTReflection);
            float _FR_GlassRTWeight;
            // [G14 G-2] smudge fade range (perceptual roughness) set by the RT pass: (0.15, 0.45) without the RT
            // smudge blur, (0.45, 0.70) once the RT resolve blurs smudges itself (P1).
            float2 _FR_GlassRTFade;
            // [G14 G-3] roll-wave scale while RT is on (0.075: annealed glass has no roll wave; true parallax shows it).
            float _FR_GlassRTRollScale;
            #endif
            // [G14-HOOK-END]
            // Steady linear zone reflection intensity (FrontRoomsZoneReflection); 0 = not published.
            float _FR_ZoneReflNominal;

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 pane       : TEXCOORD2;   // xy metres from the pane centre (u along, v up), zw half size (m)
                float3 axisU      : TEXCOORD3;   // world direction of pane u
                float3 axisV      : TEXCOORD4;   // world direction of pane v
                float4 misc       : TEXCOORD5;   // x edge face (0/1), yz per-pane random, w unused
                half4 fogAndVertexLight : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(input.normalOS);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = n.normalWS;

                // Pane frame from the object: the thinnest scaled axis is the pane normal,
                // v is world-up unless the pane lies flat, u is the remaining axis.
                float4x4 m = GetObjectToWorldMatrix();
                float3 axisX = float3(m._m00, m._m10, m._m20);
                float3 axisY = float3(m._m01, m._m11, m._m21);
                float3 axisZ = float3(m._m02, m._m12, m._m22);
                float3 objSize = _PaneSize.x > 0.0 ? float3(_PaneSize.xy, max(_PaneSize.z, 1e-4)) : float3(1.0, 1.0, 1.0);
                float3 scale = float3(length(axisX), length(axisY), length(axisZ));
                float3 extent = scale * objSize;
                float3 thin = extent.x <= extent.y && extent.x <= extent.z ? float3(1, 0, 0)
                            : (extent.y <= extent.z ? float3(0, 1, 0) : float3(0, 0, 1));
                float3 vMask = thin.y > 0.5 ? float3(0, 0, 1) : float3(0, 1, 0);
                float3 uMask = 1.0 - thin - vMask;
                float3 pos = input.positionOS.xyz;
                o.pane = float4(dot(pos, uMask) * dot(scale, uMask), dot(pos, vMask) * dot(scale, vMask),
                                0.5 * dot(extent, uMask), 0.5 * dot(extent, vMask));
                o.axisU = SafeNormalize(mul((float3x3)m, uMask));
                o.axisV = SafeNormalize(mul((float3x3)m, vMask));
                float3 an = abs(input.normalOS);
                float faceAxisIsThin = dot(step(max(an.x, max(an.y, an.z)) - 1e-3, an), thin);
                float3 centre = float3(m._m03, m._m13, m._m23);
                o.misc = float4(1.0 - saturate(faceAxisIsThin),
                                Hash11(dot(floor(centre * 10.0), float3(1.0, 57.0, 113.0))),
                                Hash11(dot(floor(centre * 10.0), float3(7.0, 3.0, 29.0)) + 0.37), 0);

                half fog = ComputeFogFactor(p.positionCS.z);
                o.fogAndVertexLight = half4(fog, VertexLighting(p.positionWS, n.normalWS));
                return o;
            }

            // Extra environment reflection that tops the glass up to _ReflectionMin (linear). Divides by the
            // steady zone intensity, not the current decode, so glass dips with the world in the WebGL dip.
            half ReflectionTopUp()
            {
                if (!(_ReflectionMin > 0.001 && _GlossyEnvironmentCubeMap_HDR.w < 0.5)) return 0.0h;
                half nominal = _FR_ZoneReflNominal > 0.0 ? (half)_FR_ZoneReflNominal : (half)_GlossyEnvironmentCubeMap_HDR.x;
                return max(_ReflectionMin / max(nominal, 1e-3h) - 1.0h, 0.0h);
            }

            // Radial crack from the impact point (metres in pane space). ALU only.
            void CrackMask(float2 m, float2 halfSize, float pixelM, out half crack, out half crush, out half2 tilt)
            {
                float2 d = m - (_ImpactUV.xy - 0.5) * 2.0 * halfSize;
                float r = length(d) + 1e-5;
                float ang = atan2(d.y, d.x);
                float seed = _CrackSeed * 17.13 + 3.1;
                float rays = 9.0 + floor(Hash11(seed) * 6.0);
                // The ray angle wanders with distance so the cracks are not straight spokes.
                float a = ang * (rays / TWO_PI) + Hash11(seed + 1.7) + 0.05 * sin(r * 9.0 + seed) + 0.02 * sin(r * 31.0 + seed * 2.3);
                float rayId = floor(a + 0.5);
                float rayW = rayId - rays * floor(rayId / rays);
                float lineDist = abs(a - rayId) * TWO_PI * r / rays;
                float reach = _Crack * 1.25 * lerp(0.35, 1.0, Hash11(rayW + seed * 7.7));
                // Anti-alias with the pixel footprint in metres (no derivatives inside the branch).
                float aa = max(pixelM, 1e-4);
                half radial = saturate(1.0 - (lineDist - 0.0008) / aa) * step(r, reach);
                half rings = 0;
                [unroll] for (int i = 0; i < 3; i++)
                {
                    float ri = (0.05 + 0.08 * i + 0.03 * i * i) * (1.0 + 0.15 * sin(ang * (3.0 + i) + seed + i));
                    float seg = step(0.45, Hash11(rayW * 3.1 + i * 11.0 + seed));
                    rings += saturate(1.0 - (abs(r - ri) - 0.0004) / aa) * seg * step(0.30 + 0.25 * i, _Crack);
                }
                crack = saturate(radial + rings);
                crush = (1.0 - smoothstep(0.004, 0.02 + 0.015 * _Crack, r)) * step(0.01, _Crack);
                // Each wedge between two rays sits slightly out of plane: the reflection breaks into facets.
                float wedge = floor(a);
                wedge -= rays * floor(wedge / rays);
                float2 h = float2(Hash11(wedge + seed), Hash11(wedge * 1.37 + seed + 5.0)) - 0.5;
                tilt = (half2)(h * 0.06 * smoothstep(0.0, 0.3, _Crack) * step(r, _Crack * 1.25));
            }

            // A flat hand pressed on the glass at the impact point (heel, four fingers, thumb).
            half PalmMask(float2 m, float2 halfSize)
            {
                float2 p = m - (_ImpactUV.xy - 0.5) * 2.0 * halfSize;
                float palm = 1.0 - smoothstep(0.7, 1.0, length((p - float2(0.0, -0.03)) / float2(0.045, 0.055)));
                float fingers = 0.0;
                [unroll] for (int i = 0; i < 4; i++)
                {
                    float2 q = p - float2((i - 1.5) * 0.024, 0.075 + 0.012 * (1.5 - abs(i - 1.5)));
                    fingers = max(fingers, 1.0 - smoothstep(0.6, 1.0, length(q / float2(0.009, 0.035))));
                }
                float thumb = 1.0 - smoothstep(0.6, 1.0, length((p - float2(-0.06, -0.005)) / float2(0.012, 0.03)));
                return (half)saturate(max(max(palm, fingers), thumb)) * _Palm;
            }

            void Frag(Varyings input, out half4 outColor : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out uint outRenderingLayers : SV_Target1
            #endif
            )
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 nGeo = normalize(input.normalWS);
                float3 viewWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half fresnel = PositivePow(1.0h - (half)saturate(dot(nGeo, viewWS)), 5.0h);
                float2 m = input.pane.xy;
                float2 halfSize = input.pane.zw;

                half3 base = _BaseColor.rgb;
                half smooth = _Smoothness;
                half dust = 0, smudge = 0;
                half2 tilt = 0;

                // Float glass is never perfectly flat: a long, shallow roll along the pane.
                float k = TWO_PI / max(_RollPeriod, 0.05);
                float ph = input.misc.y * TWO_PI;
                half roll = _RollStrength;
            #if defined(_FR_GLASS_RT)
                // [G14 G-3] with a traced (true-parallax) reflection the roll is scaled down; same factor in FRGlassRTPrepass
                if (_FR_GlassRTWeight > 0.0 && _RTReceive > 0.5) roll *= (half)_FR_GlassRTRollScale;
            #endif
                tilt.x += roll * cos(m.x * k + ph);
                tilt.y += 0.5h * roll * cos(dot(m, float2(0.31, 0.95)) * k * 0.37 + ph * 1.7);

            #if defined(_FR_GLASS_GRIME)
                {
                    float2 wuv = m + input.misc.yz * 7.31;   // per-pane offset: neighbours do not share a pattern
                    // The two faces of a pane carry different grime (each side was wiped and touched separately).
                    wuv += sign(dot(nGeo, cross(input.axisU, input.axisV))) * 0.53;
                    half4 gA = SAMPLE_TEXTURE2D(_GrimeMap, sampler_GrimeMap, wuv / 0.9);
                    half4 gB = SAMPLE_TEXTURE2D(_GrimeMap, sampler_GrimeMap, wuv.yx / 0.32 + 0.37);
                    half patch = SAMPLE_TEXTURE2D(_GrimeMap, sampler_GrimeMap, wuv / 2.3 + 0.61).a;
                    float2 e = halfSize - abs(m);                 // metres to the side edge / to the top or bottom edge
                    float above = m.y + halfSize.y;              // metres above the bottom edge
                    float hFloor = input.positionWS.y - _FloorY;
                    half bottom = 1.0h - smoothstep(0.0, 0.14, above);
                    half corner = (1.0h - smoothstep(0.0, 0.12, e.x)) * (1.0h - smoothstep(0.0, 0.12, e.y));
                    half rim = 1.0h - smoothstep(0.0, 0.06, min(e.x, e.y));
                    // Fine specks only up close (full at 0.8 m, gone by 2 m): further out they read as
                    // marks on the floor behind the pane, and the thresholded mip shimmers in motion.
                    half speckNear = (half)saturate(1.0 - (length(input.positionWS - GetCameraPositionWS()) - 0.8) / 1.2);
                    dust = saturate((0.14h + 0.85h * bottom + 0.3h * corner + 0.12h * rim) * (0.3h + 0.7h * gA.a)
                                    + 0.5h * gB.b * (0.3h + bottom) * speckNear) * _Dust;
                    half band = smoothstep(0.80, 0.95, hFloor) * (1.0h - smoothstep(1.45, 1.65, hFloor));
                    half smear = band * smoothstep(0.45h, 0.75h, patch) * smoothstep(0.35h, 0.70h, gA.r) * _Smear;
                    half side = 1.0h - smoothstep(0.05, 0.22, e.x);
                    half pband = smoothstep(0.85, 1.0, hFloor) * (1.0h - smoothstep(1.55, 1.75, hFloor));
                    half prints = side * pband * smoothstep(0.55h, 0.80h, gB.g) * _Prints;
                    smudge = saturate(smear + prints);
                    half3 sn = UnpackNormal(SAMPLE_TEXTURE2D(_SmearNormal, sampler_SmearNormal, wuv / 0.9));
                    tilt += sn.xy * (_SmudgeNormal * 2.0h) * smudge;
                }
            #endif
                dust = saturate(dust + _DustFilm);

                half alphaExtra = 0;
                [branch] if (_Palm > 0.001)
                {
                    half palm = PalmMask(m, halfSize);
                    smudge = max(smudge, palm * 0.9h);
                    alphaExtra += 0.04h * palm;
                }
                half crack = 0, crush = 0;
                [branch] if (_Crack > 0.001)
                {
                    // Metres covered by one pixel at this depth, widened on oblique views.
                    float pixelM = length(input.positionWS - GetCameraPositionWS()) * 2.0 / max(_ScreenParams.y * abs(UNITY_MATRIX_P._m11), 1e-3);
                    pixelM /= max((float)saturate(dot(nGeo, viewWS)), 0.25);
                    half2 crackTilt;
                    CrackMask(m, halfSize, pixelM, crack, crush, crackTilt);
                    tilt += crackTilt;
                }

                smooth = lerp(smooth, _SmudgeSmoothness, smudge);
                smooth = lerp(smooth, 0.45h, dust * 0.6h);
                smooth = lerp(smooth, 0.35h, saturate(crack * 0.7h + crush));
                base = lerp(base, _DustColor.rgb, saturate(dust * 1.2h + smudge * 0.08h));
                base = lerp(base, half3(0.70h, 0.73h, 0.71h), saturate(crack * 0.9h + crush));
                half alpha = _AlphaFace + _AlphaFresnel * fresnel + _DustAlpha * dust + 0.05h * smudge + alphaExtra
                           + 0.55h * crack + 0.6h * crush;

                // The four thin edge faces of the pane: green float-glass edge, near opaque.
                half edge = (half)input.misc.x * step(0.5h, _AutoEdge);
                base = lerp(base, _EdgeColor.rgb, edge);
                alpha = lerp(alpha, _EdgeAlpha, edge);
                smooth = lerp(smooth, _EdgeSmoothness, edge);
                tilt *= 1.0h - edge;
                alpha = saturate(alpha);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalize(nGeo + tilt.x * input.axisU + tilt.y * input.axisV);
                inputData.viewDirectionWS = viewWS;
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
                s.albedo = base;
                s.metallic = 0;
                s.specular = (half3)_PaneF0;
                s.smoothness = saturate(smooth);
                s.normalTS = half3(0, 0, 1);
                s.occlusion = 1;
                s.alpha = alpha;
                // Dust, smudges and cracks scatter the room light that reaches them from both
                // sides of the pane (a lit haze). Without this, physically clear glass is
                // invisible in these dim, evenly lit rooms (audit 05, frames 08/09/11).
                half scatter = (dust + 0.6h * smudge + 3.0h * crack + 2.0h * crush) * (1.0h - edge) * _Scatter;
                s.emission = scatter * _DustColor.rgb * 0.5h * (SampleSH(nGeo) + SampleSH(-nGeo));

                half4 color = UniversalFragmentPBR(inputData, s);

                // Reflection floor on glass: the world's default reflection is capped at 0.5 linear
                // (FrontRoomsZoneReflection.MaxLinear: no parallax yet, frozen wallpaper print). Glass
                // tops its environment term up to _ReflectionMin (1.0 = the full captured light, which
                // is physical: the cubes were captured under the game's own lamps). The ratio uses the
                // steady zone intensity (_FR_ZoneReflNominal), so glass still dips with the world.
                // Skips RGBM-encoded cubes (decode w != 0).
                half extra = ReflectionTopUp();
                [branch] if (extra > 0.0h)
                {
                    BRDFData brdf;
                    half brdfAlpha = alpha;
                    InitializeBRDFData(s.albedo, s.metallic, s.specular, s.smoothness, brdfAlpha, brdf);
                    half3 reflectVector = reflect(-viewWS, inputData.normalWS);
                    half fresnelTerm = Pow4(1.0h - saturate(dot(inputData.normalWS, viewWS)));
                    half3 env = GlossyEnvironmentReflection(reflectVector, inputData.positionWS, brdf.perceptualRoughness, 1.0h, inputData.normalizedScreenSpaceUV);
                    color.rgb += EnvironmentBRDFSpecular(brdf, fresnelTerm) * env * extra;
                }
                // [G14-HOOK-BEGIN] RT / planar reflection: where weight x coverage > 0, swap URP's environment
                // term (and the _ReflectionMin top-up above) for the supplied radiance. It is still weighted by
                // this shader's own EnvironmentBRDFSpecular (Fresnel) and fades out under smudges, dust, cracks
                // and on the edge faces, so grime keeps the convolved cube. It runs before fog and post, so the
                // RT result is fogged, tonemapped, graded and bloomed with the scene.
            #if defined(_FR_GLASS_RT)
                [branch] if (_FR_GlassRTWeight > 0.0 && _RTReceive > 0.5)
                {
                    half4 rt = (half4)SAMPLE_TEXTURE2D_LOD(_FR_GlassRTReflection, sampler_FR_GlassRTReflection, inputData.normalizedScreenSpaceUV, 0);
                    half cover = saturate(rt.a);
                    if (rt.a > 1.0h)
                    {
                        // Depth-tagged texel: only the glass surface it was traced for takes it.
                        float tagDepth = (float)rt.a - 1.0;
                        float myDepth = -TransformWorldToView(input.positionWS).z;
                        cover = abs(tagDepth - myDepth) <= 0.02 + 0.01 * myDepth ? 1.0h : 0.0h;
                    }
                    half w = saturate((half)_FR_GlassRTWeight * cover) * (1.0h - edge) * (1.0h - saturate(crack + crush));
                    [branch] if (w > 0.0h)
                    {
                        BRDFData b;
                        half aRT = alpha;
                        InitializeBRDFData(s.albedo, s.metallic, s.specular, s.smoothness, aRT, b);
                        half3 rv = reflect(-viewWS, inputData.normalWS);
                        half ft = Pow4(1.0h - saturate(dot(inputData.normalWS, viewWS)));
                        half3 envRT = GlossyEnvironmentReflection(rv, inputData.positionWS, b.perceptualRoughness, 1.0h, inputData.normalizedScreenSpaceUV);
                        half envScale = 1.0h + extra;
                        // [G14 G-2] fade range from the RT pass (falls back to 0.15-0.45 if unset)
                        float2 fade = _FR_GlassRTFade.y > _FR_GlassRTFade.x ? _FR_GlassRTFade : float2(0.15, 0.45);
                        w *= 1.0h - smoothstep((half)fade.x, (half)fade.y, b.perceptualRoughness);
                        color.rgb += EnvironmentBRDFSpecular(b, ft) * w * (rt.rgb - envRT * envScale);
                    }
                }
            #endif
                // [G14-HOOK-END]
                // Premultiplied output: fog toward fogColour * alpha, not the full fog colour.
                color.rgb = MixFogColor(color.rgb, (half3)(unity_FogColor.rgb * alpha), (half)inputData.fogCoord);
                outColor = half4(color.rgb, alpha);
                if (_GrimeDebug > 0.5) outColor = half4(dust, smudge, saturate(crack + crush), 1.0h);
            #ifdef _WRITE_RENDERING_LAYERS
                outRenderingLayers = EncodeMeshRenderingLayer();
            #endif
            }
            ENDHLSL
        }

        // [G14 G-1] FRGlassRTPrepass: drawn ONLY by the RT renderer feature (desktop macOS, rendering-layer bit 30 on
        // registered receivers) into GlassDepth (R32F linear eye depth) + GlassNormal (RGBA16F final world normal with
        // roll x _FR_GlassRTRollScale, smudge, palm and crack facets; perceptual smoothness). ZWrite into the feature's
        // own 1x depth keeps the front-most receiver; glass behind the opaque scene (_CameraDepthTexture) and the thin
        // edge faces are clipped. Same grime / roll / crack code as ForwardLit (keep the two in sync).
        // Stripped from WebGL builds (FrontRoomsGlassRTWebGLStripper); no new keywords.
        Pass
        {
            Name "FRGlassRTPrepass"
            Tags { "LightMode" = "FRGlassRTPrepass" }
            ZWrite On
            ZTest LEqual
            Cull [_Cull]
            Blend Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment FragPrepass
            #pragma shader_feature_local_fragment _FR_GLASS_GRIME
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _AlphaFace;
                half _AlphaFresnel;
                half _Smoothness;
                half _SmudgeSmoothness;
                half _Metallic;
                half _PaneF0;
                half _RollStrength;
                float _RollPeriod;
                half _ReflectionMin;
                half _DustFilm;
                half _Scatter;
                half _GrimeDebug;
                half _SmudgeNormal;
                half4 _DustColor;
                half _DustAlpha;
                half _Dust;
                half _Smear;
                half _Prints;
                float _FloorY;
                float4 _PaneSize;
                half _AutoEdge;
                half4 _EdgeColor;
                half _EdgeAlpha;
                half _EdgeSmoothness;
                half _Crack;
                float4 _ImpactUV;
                float _CrackSeed;
                half _Palm;
                half _RTReceive;
            CBUFFER_END

            TEXTURE2D(_GrimeMap);    SAMPLER(sampler_GrimeMap);
            TEXTURE2D(_SmearNormal); SAMPLER(sampler_SmearNormal);
            TEXTURE2D_X_FLOAT(_FRGlassRTSceneDepth);
            float _FR_GlassRTRollScale;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 pane       : TEXCOORD2;
                float3 axisU      : TEXCOORD3;
                float3 axisV      : TEXCOORD4;
                float4 misc       : TEXCOORD5;   // x edge face, yz per-pane random, w linear eye depth
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(input.normalOS);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = n.normalWS;
                float4x4 m = GetObjectToWorldMatrix();
                float3 axisX = float3(m._m00, m._m10, m._m20);
                float3 axisY = float3(m._m01, m._m11, m._m21);
                float3 axisZ = float3(m._m02, m._m12, m._m22);
                float3 objSize = _PaneSize.x > 0.0 ? float3(_PaneSize.xy, max(_PaneSize.z, 1e-4)) : float3(1.0, 1.0, 1.0);
                float3 scale = float3(length(axisX), length(axisY), length(axisZ));
                float3 extent = scale * objSize;
                float3 thin = extent.x <= extent.y && extent.x <= extent.z ? float3(1, 0, 0)
                            : (extent.y <= extent.z ? float3(0, 1, 0) : float3(0, 0, 1));
                float3 vMask = thin.y > 0.5 ? float3(0, 0, 1) : float3(0, 1, 0);
                float3 uMask = 1.0 - thin - vMask;
                float3 pos = input.positionOS.xyz;
                o.pane = float4(dot(pos, uMask) * dot(scale, uMask), dot(pos, vMask) * dot(scale, vMask),
                                0.5 * dot(extent, uMask), 0.5 * dot(extent, vMask));
                o.axisU = SafeNormalize(mul((float3x3)m, uMask));
                o.axisV = SafeNormalize(mul((float3x3)m, vMask));
                float3 an = abs(input.normalOS);
                float faceAxisIsThin = dot(step(max(an.x, max(an.y, an.z)) - 1e-3, an), thin);
                float3 centre = float3(m._m03, m._m13, m._m23);
                o.misc = float4(1.0 - saturate(faceAxisIsThin),
                                Hash11(dot(floor(centre * 10.0), float3(1.0, 57.0, 113.0))),
                                Hash11(dot(floor(centre * 10.0), float3(7.0, 3.0, 29.0)) + 0.37),
                                -TransformWorldToView(p.positionWS).z);
                return o;
            }

            void CrackMask(float2 m, float2 halfSize, float pixelM, out half crack, out half crush, out half2 tilt)
            {
                float2 d = m - (_ImpactUV.xy - 0.5) * 2.0 * halfSize;
                float r = length(d) + 1e-5;
                float ang = atan2(d.y, d.x);
                float seed = _CrackSeed * 17.13 + 3.1;
                float rays = 9.0 + floor(Hash11(seed) * 6.0);
                float a = ang * (rays / TWO_PI) + Hash11(seed + 1.7) + 0.05 * sin(r * 9.0 + seed) + 0.02 * sin(r * 31.0 + seed * 2.3);
                float rayId = floor(a + 0.5);
                float rayW = rayId - rays * floor(rayId / rays);
                float lineDist = abs(a - rayId) * TWO_PI * r / rays;
                float reach = _Crack * 1.25 * lerp(0.35, 1.0, Hash11(rayW + seed * 7.7));
                float aa = max(pixelM, 1e-4);
                half radial = saturate(1.0 - (lineDist - 0.0008) / aa) * step(r, reach);
                half rings = 0;
                [unroll] for (int i = 0; i < 3; i++)
                {
                    float ri = (0.05 + 0.08 * i + 0.03 * i * i) * (1.0 + 0.15 * sin(ang * (3.0 + i) + seed + i));
                    float seg = step(0.45, Hash11(rayW * 3.1 + i * 11.0 + seed));
                    rings += saturate(1.0 - (abs(r - ri) - 0.0004) / aa) * seg * step(0.30 + 0.25 * i, _Crack);
                }
                crack = saturate(radial + rings);
                crush = (1.0 - smoothstep(0.004, 0.02 + 0.015 * _Crack, r)) * step(0.01, _Crack);
                float wedge = floor(a);
                wedge -= rays * floor(wedge / rays);
                float2 h = float2(Hash11(wedge + seed), Hash11(wedge * 1.37 + seed + 5.0)) - 0.5;
                tilt = (half2)(h * 0.06 * smoothstep(0.0, 0.3, _Crack) * step(r, _Crack * 1.25));
            }

            half PalmMask(float2 m, float2 halfSize)
            {
                float2 p = m - (_ImpactUV.xy - 0.5) * 2.0 * halfSize;
                float palm = 1.0 - smoothstep(0.7, 1.0, length((p - float2(0.0, -0.03)) / float2(0.045, 0.055)));
                float fingers = 0.0;
                [unroll] for (int i = 0; i < 4; i++)
                {
                    float2 q = p - float2((i - 1.5) * 0.024, 0.075 + 0.012 * (1.5 - abs(i - 1.5)));
                    fingers = max(fingers, 1.0 - smoothstep(0.6, 1.0, length(q / float2(0.009, 0.035))));
                }
                float thumb = 1.0 - smoothstep(0.6, 1.0, length((p - float2(-0.06, -0.005)) / float2(0.012, 0.03)));
                return (half)saturate(max(max(palm, fingers), thumb)) * _Palm;
            }

            void FragPrepass(Varyings input, out float outDepth : SV_Target0, out half4 outNormal : SV_Target1)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                clip(_RTReceive - 0.5);
                half edge = (half)input.misc.x * step(0.5h, _AutoEdge);
                clip(0.5 - input.misc.x);   // thin edge faces never trace (the 30 mm cube is not a thickness)
                float eye = input.misc.w;
                float sceneRaw = LOAD_TEXTURE2D_X(_FRGlassRTSceneDepth, uint2(input.positionCS.xy)).r;
                clip(LinearEyeDepth(sceneRaw, _ZBufferParams) + 0.001 + 0.0005 * eye - eye);   // 1 mm + 0.05 %: the window stops sit 3 mm in front of the glass

                float3 nGeo = normalize(input.normalWS);
                float2 m = input.pane.xy;
                float2 halfSize = input.pane.zw;
                half smooth = _Smoothness;
                half dust = 0, smudge = 0;
                half2 tilt = 0;
                float k = TWO_PI / max(_RollPeriod, 0.05);
                float ph = input.misc.y * TWO_PI;
                half roll = _RollStrength * (half)_FR_GlassRTRollScale;
                tilt.x += roll * cos(m.x * k + ph);
                tilt.y += 0.5h * roll * cos(dot(m, float2(0.31, 0.95)) * k * 0.37 + ph * 1.7);
            #if defined(_FR_GLASS_GRIME)
                {
                    float2 wuv = m + input.misc.yz * 7.31;
                    wuv += sign(dot(nGeo, cross(input.axisU, input.axisV))) * 0.53;
                    half4 gA = SAMPLE_TEXTURE2D(_GrimeMap, sampler_GrimeMap, wuv / 0.9);
                    half4 gB = SAMPLE_TEXTURE2D(_GrimeMap, sampler_GrimeMap, wuv.yx / 0.32 + 0.37);
                    half patch = SAMPLE_TEXTURE2D(_GrimeMap, sampler_GrimeMap, wuv / 2.3 + 0.61).a;
                    float2 e = halfSize - abs(m);
                    float above = m.y + halfSize.y;
                    float hFloor = input.positionWS.y - _FloorY;
                    half bottom = 1.0h - smoothstep(0.0, 0.14, above);
                    half corner = (1.0h - smoothstep(0.0, 0.12, e.x)) * (1.0h - smoothstep(0.0, 0.12, e.y));
                    half rim = 1.0h - smoothstep(0.0, 0.06, min(e.x, e.y));
                    half speckNear = (half)saturate(1.0 - (length(input.positionWS - GetCameraPositionWS()) - 0.8) / 1.2);
                    dust = saturate((0.14h + 0.85h * bottom + 0.3h * corner + 0.12h * rim) * (0.3h + 0.7h * gA.a)
                                    + 0.5h * gB.b * (0.3h + bottom) * speckNear) * _Dust;
                    half band = smoothstep(0.80, 0.95, hFloor) * (1.0h - smoothstep(1.45, 1.65, hFloor));
                    half smear = band * smoothstep(0.45h, 0.75h, patch) * smoothstep(0.35h, 0.70h, gA.r) * _Smear;
                    half side = 1.0h - smoothstep(0.05, 0.22, e.x);
                    half pband = smoothstep(0.85, 1.0, hFloor) * (1.0h - smoothstep(1.55, 1.75, hFloor));
                    half prints = side * pband * smoothstep(0.55h, 0.80h, gB.g) * _Prints;
                    smudge = saturate(smear + prints);
                    half3 sn = UnpackNormal(SAMPLE_TEXTURE2D(_SmearNormal, sampler_SmearNormal, wuv / 0.9));
                    tilt += sn.xy * (_SmudgeNormal * 2.0h) * smudge;
                }
            #endif
                dust = saturate(dust + _DustFilm);
                [branch] if (_Palm > 0.001) smudge = max(smudge, PalmMask(m, halfSize) * 0.9h);
                half crack = 0, crush = 0;
                [branch] if (_Crack > 0.001)
                {
                    float3 viewWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                    float pixelM = length(input.positionWS - GetCameraPositionWS()) * 2.0 / max(_ScreenParams.y * abs(UNITY_MATRIX_P._m11), 1e-3);
                    pixelM /= max((float)saturate(dot(nGeo, viewWS)), 0.25);
                    half2 crackTilt;
                    CrackMask(m, halfSize, pixelM, crack, crush, crackTilt);
                    tilt += crackTilt;
                }
                smooth = lerp(smooth, _SmudgeSmoothness, smudge);
                smooth = lerp(smooth, 0.45h, dust * 0.6h);
                smooth = lerp(smooth, 0.35h, saturate(crack * 0.7h + crush));
                tilt *= 1.0h - edge;
                outDepth = eye;
                outNormal = half4(normalize(nGeo + tilt.x * input.axisU + tilt.y * input.axisV), saturate(smooth));
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
