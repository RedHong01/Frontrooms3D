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
// GD3 staged breakage (Documentation/research/glass/destruction/10_glass_destruction_plan.md §4.2,
// build/04_stages.md). The cracks are GEOMETRY now (FrontRoomsGlassBreakable's stage meshes); the old
// procedural CrackMask lines, wedge tilt and crack glow are gone (_Crack / _CrackSeed stay declared, unused,
// so old materials and scripts still load). Hooks, all uniform branches (no keyword, no new variant):
//   _PaneFromUV  stage meshes carry their pane frame: UV0 = pane metres from the slab centre, UV1 = (two
//                per-pane hash keys, band, kind), colour R = fracture-edge mask, G = per-segment random,
//                B = depth across the 6 mm. Axes = object X / Y, so grime and smudges stay put through every
//                swap and on moving pieces. Kind: 0 pane face, 1 fracture face (the crack plane: a glass-to-air
//                interface met past the critical angle, i.e. a total-reflection mirror of the FAR side; dark
//                where the far side is dark, never self-lit), 2 crushed glass (crater), 3 outer edge (green).
//   _Crush, _CrushRadius  the whitish crushed bruise round the impact on pane faces (S1 slab, S2 rim).
//   _BowCentre (xy impact, zw half size of the held glass, pane metres), _BowAmp (m toward +Z): the pane
//                bowing under the strikes, an analytic normal tilt only (no vertex moves).
//   _Palm        a palm smudge at _ImpactUV (smoothness/normal only: it no longer scatters light).
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

        [HideInInspector] _Crack ("Legacy: procedural crack reach (unused since GD3)", Range(0, 1)) = 0
        _ImpactUV ("Impact point, pane uv (0-1)", Vector) = (0.5, 0.5, 0, 0)
        [HideInInspector] _CrackSeed ("Legacy: procedural crack seed (unused since GD3)", Float) = 0
        _Palm ("Palm smudge (0-1)", Range(0, 1)) = 0
        [ToggleUI] _PaneFromUV ("GD3: pane frame from the mesh (UV0 metres, UV1 keys/kind, colour R edge)", Float) = 0
        _Crush ("GD3: crushed bruise round the impact (0-1)", Range(0, 1)) = 0
        _CrushRadius ("GD3: crushed bruise radius (m)", Float) = 0.0127
        _BowCentre ("GD3: bow centre xy, held half size zw (pane metres)", Vector) = (0, 0, 0.6835, 0.8085)
        _BowAmp ("GD3: bow, peak displacement toward +Z (m, signed)", Float) = 0
        _FractureReflect ("GD3: fracture-face reflectance (total reflection, x green tint)", Range(0, 1)) = 0.9
        _FractureSmoothness ("GD3: fracture-face smoothness", Range(0, 1)) = 0.9
        _FractureMirror ("GD3: crack mirror transmission (Fresnel in and out, 2 x 6 mm of glass)", Range(0, 1)) = 0.88
        _FractureTint ("GD3: crack mirror green (0 clear .. 1 the edge colour)", Range(0, 1)) = 0.45
        _FractureLampRef ("GD3: fallback only: lamp luminance at the crack that counts as fully lit", Float) = 1.5

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
                half _PaneFromUV;
                half _Crush;
                float _CrushRadius;
                float4 _BowCentre;
                float _BowAmp;
                half _FractureReflect;
                half _FractureSmoothness;
                half _FractureMirror;
                half _FractureTint;
                half _FractureLampRef;
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
            // GD3: 1 while FrontRoomsGlassSceneColor has requested URP's opaque + depth textures for the camera being drawn
            // (desktop, a cracked or held pane nearby), else 0 (WebGL, probes, previews): then the crack faces mirror the zone
            // cube, dimmed by the lamps that actually light the crack.
            float _FR_GlassSceneColor;

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv0        : TEXCOORD0;   // GD3 (_PaneFromUV): pane metres from the slab centre
                float4 uv1        : TEXCOORD1;   // GD3: per-pane hash keys (xy), band (z), kind (w)
                half4 color       : COLOR;       // GD3: R fracture-edge mask, G segment random, B depth 0-1
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
                float4 misc       : TEXCOORD5;   // x edge face (0/1), yz per-pane random, w GD3 kind (0 on the cube path)
                half4 fogAndVertexLight : TEXCOORD6;
                float4 gd3        : TEXCOORD7;   // GD3: x segment random, y depth 0-1, z band
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
                [branch] if (_PaneFromUV > 0.5)
                {
                    // GD3 stage meshes: the pane frame travels with the mesh (same values on every stage and piece).
                    o.pane = float4(input.uv0, 0.5 * _PaneSize.xy);
                    o.axisU = SafeNormalize(axisX);
                    o.axisV = SafeNormalize(axisY);
                    o.misc = float4(step(2.5, input.uv1.w), Hash11(input.uv1.x), Hash11(input.uv1.y + 0.37), input.uv1.w);
                    o.gd3 = float4(input.color.g, input.color.b, input.uv1.z, 0);
                }

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

            // ---- GD3 (destruction plan §4.2): bow, crushed bruise, fracture-face ripples. ALU only, uniform branches.
            // Normalised deflection of a simply supported span with the load at a (0-1) and its slope: sin(pi g), g mapping
            // [0, a] -> [0, .5] and [a, 1] -> [.5, 1]; zero outside the span (the stops hold the glass).
            float2 BowAxis(float t, float a)
            {
                a = clamp(a, 0.05, 0.95);
                float tc = saturate(t);
                float g = tc < a ? 0.5 * tc / a : 0.5 + 0.5 * (tc - a) / (1.0 - a);
                float dg = tc < a ? 0.5 / a : 0.5 / (1.0 - a);
                float inside = step(0.0, t) * step(t, 1.0);
                return float2(sin(PI * g), PI * cos(PI * g) * dg) * inside;
            }

            // Slope (dw/du, dw/dv) of the bow at pane point m (metres from the pane centre); _BowCentre.xy = the load,
            // .zw = half size of the held glass; _BowAmp = peak displacement toward the pane's +Z (signed metres).
            float2 BowSlope(float2 m)
            {
                float2 h = max(_BowCentre.zw, 1e-3);
                float2 t = (m + h) / (2.0 * h);
                float2 a = (_BowCentre.xy + h) / (2.0 * h);
                float2 fx = BowAxis(t.x, a.x);
                float2 fy = BowAxis(t.y, a.y);
                return _BowAmp * float2(fx.y * fy.x / (2.0 * h.x), fx.x * fy.y / (2.0 * h.y));
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash11(dot(i, float2(1.0, 57.0)));
                float b = Hash11(dot(i + float2(1.0, 0.0), float2(1.0, 57.0)));
                float c = Hash11(dot(i + float2(0.0, 1.0), float2(1.0, 57.0)));
                float d = Hash11(dot(i + float2(1.0, 1.0), float2(1.0, 57.0)));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // The crushed bruise round the impact on pane faces: granular, irregular edge, about 1.3 x the crater radius.
            half CrushMask(float2 m, float2 halfSize)
            {
                float2 d = m - (_ImpactUV.xy - 0.5) * 2.0 * halfSize;
                float r = length(d);
                float R = max(_CrushRadius, 1e-4);
                float grain = ValueNoise(m * 1100.0);
                float edge = ValueNoise(float2(atan2(d.y, d.x) * 3.0, 7.3));
                // reaches just past the crater (S1) and stays as a crushed white rim round the hole once the core falls out (S2);
                // never past 14.5 mm from the impact (plan sec 2.3: the crushed spot is at most 1.5 cm)
                half halo = (half)(1.0 - smoothstep(0.55 * R, min((1.06 + 0.14 * edge) * R, 0.0145), r));
                return (half)_Crush * halo * (half)saturate(0.25 + 0.95 * grain * grain);
            }

            // Hackle and rib marks on a fracture face: a small tilt of the face normal across the 6 mm (depth 0-1) and along
            // the crack (pane metres), so a crack's mirror image breaks up and its brightness changes along it (V3).
            float FractureRipple(float2 m, float depth, float seg)
            {
                // centimetre-scale runs along the crack (not millimetre stitches), one slow rib across the depth
                float along = ValueNoise(m * 90.0 + seg * 31.0) - 0.5;
                float fine = ValueNoise(m * 260.0 + seg * 17.0) - 0.5;
                return 0.06 * sin(TWO_PI * (depth * 0.9 + seg)) + 0.30 * along + 0.06 * fine;
            }

            // GD3: a fracture face is an internal mirror. The eye ray refracts in through the face it meets, reflects off the
            // crack plane (total reflection: the ray meets it past the 41.8 deg critical angle) and leaves through a face, so
            // the crack shows the scene in that NEW direction: brighter or darker than the glass beside it depending on what it
            // finds there, never lit by itself. Desktop: that scene is read from URP's opaque texture where the new ray reaches
            // the depth behind the glass (one step, the depth texture). a = 1 when the read is usable; 0 when the ray stays
            // trapped, turns back to the eye's side, or leaves the screen (the caller falls back to the zone cube).
            half4 CrackMirror(float3 positionWS, float3 viewWS, float3 nCrack, float3 nPane, float4 positionCS, float2 screenUV)
            {
                float3 nFace = dot(nPane, viewWS) >= 0.0 ? nPane : -nPane;    // the face the eye looks at (outward)
                float3 t = refract(-viewWS, nFace, 1.0 / 1.52);
                float3 t2 = reflect(t, nCrack);
                if (dot(t2, nFace) > 0.0) return half4(0, 0, 0, 0);           // heads back out through the eye's face
                float3 o = refract(t2, nFace, 1.52);                          // out through the far face (outward = -nFace)
                if (dot(o, o) < 1e-4) return half4(0, 0, 0, 0);               // trapped by total reflection at the far face
                float fragEye = -TransformWorldToView(positionWS).z;
                float behind = clamp(LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams) - fragEye, 0.05, 60.0);
                float3 camFwd = -UNITY_MATRIX_V[2].xyz;
                float3 q = positionWS + o * (behind / max(dot(o, camFwd), 0.2));
                float4 csP = TransformWorldToHClip(positionWS);
                float4 csQ = TransformWorldToHClip(q);
                if (csQ.w <= 1e-4) return half4(0, 0, 0, 0);
                float2 d = csQ.xy / csQ.w - csP.xy / csP.w;                   // NDC step from this pixel to the mirrored point
            #if UNITY_UV_STARTS_AT_TOP
                d.y = -d.y;
            #endif
                float2 uv = GetNormalizedScreenSpaceUV(positionCS.xy + d * 0.5 * GetScaledScreenParams().xy);
                if (any(uv < 0.0) || any(uv > 1.0)) return half4(0, 0, 0, 0);
                return half4(SampleSceneColor(uv), 1.0h);
            }

            // GD3 fallback (no scene texture: WebGL, probe captures): how strongly the lamps light this crack, 0-1, from the
            // additional lights' attenuated luminance (_FractureLampRef = full). A dead-lamp room gives 0: nothing glows.
            half CrackLampLight(InputData inputData)
            {
                half sum = 0.0h;
            #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
                #if USE_CLUSTER_LIGHT_LOOP
                [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    Light dl = GetAdditionalLight(lightIndex, inputData.positionWS);
                    sum += Luminance(dl.color) * dl.distanceAttenuation;
                }
                #endif
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light l = GetAdditionalLight(lightIndex, inputData.positionWS);
                    sum += Luminance(l.color) * l.distanceAttenuation;
                LIGHT_LOOP_END
            #endif
                Light mainLight = GetMainLight();
                sum += Luminance(mainLight.color) * mainLight.distanceAttenuation;
                return saturate(sum / max(_FractureLampRef, 1e-3h));
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
            // [G14-HOOK-BEGIN] RT-only roll attenuation; only traced coverage changes the normal.
            #if defined(_FR_GLASS_RT)
                // [G14 G-3] A stale/empty/depth-mismatched RT texel must leave today's pane unchanged.
                // Sample the same screen-space coverage used by the reflection resolve so P2/P3/P5 do not
                // attenuate the roll outside the receiver's traced pixels.
                half rtRollCoverage = 0.0h;
                if (_FR_GlassRTWeight > 0.0 && _RTReceive > 0.5)
                {
                    half4 rtRoll = (half4)SAMPLE_TEXTURE2D_LOD(_FR_GlassRTReflection, sampler_FR_GlassRTReflection,
                                                                GetNormalizedScreenSpaceUV(input.positionCS), 0);
                    rtRollCoverage = saturate(rtRoll.a);
                    if (rtRoll.a > 1.0h)
                    {
                        float tagDepth = (float)rtRoll.a - 1.0;
                        float myDepth = -TransformWorldToView(input.positionWS).z;
                        rtRollCoverage = abs(tagDepth - myDepth) <= 0.02 + 0.01 * myDepth ? 1.0h : 0.0h;
                    }
                }
                roll *= lerp(1.0h, (half)_FR_GlassRTRollScale, rtRollCoverage);
            #endif
            // [G14-HOOK-END]
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
                half grimeSmudge = smudge;   // GD3: grime scatters room light; a palm print is grease (smoothness, normal) and does not
                [branch] if (_Palm > 0.001)
                {
                    half palm = PalmMask(m, halfSize);
                    smudge = max(smudge, palm * 0.9h);
                    alphaExtra += 0.04h * palm;
                }
                // GD3: crack = the stage meshes' fracture faces and crushed glass (kinds 1-2); crush = the bruise on pane faces.
                // (The G14 hook below reads these two names: traced reflections stay off cracks and crushed glass.)
                half crack = 0, crush = 0, fracture = 0, crushed = 0;
                float3 nShade = nGeo;
                half3 spec = (half3)_PaneF0;
                [branch] if (_PaneFromUV > 0.5)
                {
                    float kind = input.misc.w;
                    fracture = (half)(step(0.5, kind) * step(kind, 1.5));
                    crushed = (half)(step(1.5, kind) * step(kind, 2.5));
                    crack = saturate(fracture + crushed);
                    half faces = saturate(1.0h - crack - (half)input.misc.x);
                    [branch] if (_Crush > 0.001) crush = CrushMask(m, halfSize) * faces;
                    float3 nPane = cross(input.axisU, input.axisV);
                    [branch] if (abs(_BowAmp) > 1e-7) tilt -= (half2)(BowSlope(m) * dot(nGeo, nPane)) * faces;
                    [branch] if (fracture > 0.5)
                    {
                        // The crack plane, seen from inside the glass: total reflection (cos <= 1/1.52 past the 41 deg critical
                        // angle), so it mirrors the FAR side along reflect(-v, n); slightly green from the path through the glass.
                        nShade = normalize(nGeo + FractureRipple(m, input.gd3.y, input.gd3.x) * nPane);
                        half3 tint = lerp(1.0h, _EdgeColor.rgb / max(max(_EdgeColor.r, _EdgeColor.g), max(_EdgeColor.b, 1e-3h)), 0.75h);
                        spec = _FractureReflect * (0.82h + 0.18h * (half)input.gd3.x) * tint;
                    }
                    [branch] if (crushed > 0.5)
                    {
                        // Crushed glass is a powder of sub-millimetre facets: shade it on the pane normal of the viewer's side with
                        // a granular normal, so the crater's triangles never read as a star of facets (V5).
                        float2 g = float2(ValueNoise(m * 1800.0), ValueNoise(m * 1800.0 + 23.7)) - 0.5;
                        float3 nSide = dot(nGeo, nPane) >= 0.0 ? nPane : -nPane;
                        nShade = normalize(nSide + 1.4 * (g.x * input.axisU + g.y * input.axisV));
                    }
                }

                smooth = lerp(smooth, _SmudgeSmoothness, smudge);
                smooth = lerp(smooth, 0.45h, dust * 0.6h);
                smooth = lerp(smooth, 0.35h, saturate(crack * 0.7h + crush));
                base = lerp(base, _DustColor.rgb, saturate(dust * 1.2h + smudge * 0.08h));
                base = lerp(base, half3(0.70h, 0.73h, 0.71h), saturate(crack * 0.9h + crush));
                half alpha = _AlphaFace + _AlphaFresnel * fresnel + _DustAlpha * dust + 0.05h * smudge + alphaExtra
                           + 0.55h * crack + 0.6h * crush;
                [branch] if (crack > 0.0h)
                {
                    // GD3 fracture face: a mirror (no diffuse); crushed glass: white powder (diffuse, rough), irregular cover.
                    half powder = (half)(0.55 + 0.45 * ValueNoise(m * 1400.0));
                    base = lerp(base, half3(0.66h, 0.69h, 0.67h), crushed);
                    base *= 1.0h - fracture;
                    smooth = lerp(smooth, (half)_FractureSmoothness * (0.9h + 0.1h * (half)input.gd3.x), fracture);
                    smooth = lerp(smooth, 0.55h, crushed);
                    alpha = lerp(alpha, 0.92h, fracture);
                    alpha = lerp(alpha, 0.60h * powder, crushed);
                    spec = lerp(spec, (half3)0.04h, crushed);
                    tilt *= 1.0h - crack;
                }

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
                inputData.normalWS = normalize(nShade + tilt.x * input.axisU + tilt.y * input.axisV);
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
                s.specular = spec;
                s.smoothness = saturate(smooth);
                s.normalTS = half3(0, 0, 1);
                s.occlusion = 1.0h - fracture;   // GD3: a crack face takes no cube light; its mirror image is added below
                s.alpha = alpha;
                // Dust, smudges and cracks scatter the room light that reaches them from both
                // sides of the pane (a lit haze). Without this, physically clear glass is
                // invisible in these dim, evenly lit rooms (audit 05, frames 08/09/11).
                // GD3: cracks, crushed glass and palm prints add no light of their own (nothing glows in a dead-lamp room).
                half scatter = (dust + 0.6h * grimeSmudge) * (1.0h - edge) * (1.0h - crack) * _Scatter;
                s.emission = scatter * _DustColor.rgb * 0.5h * (SampleSH(nGeo) + SampleSH(-nGeo));

                half4 color = UniversalFragmentPBR(inputData, s);

                // Reflection floor on glass: the world's default reflection is capped at 0.5 linear
                // (FrontRoomsZoneReflection.MaxLinear: no parallax yet, frozen wallpaper print). Glass
                // tops its environment term up to _ReflectionMin (1.0 = the full captured light, which
                // is physical: the cubes were captured under the game's own lamps). The ratio uses the
                // steady zone intensity (_FR_ZoneReflNominal), so glass still dips with the world.
                // Skips RGBM-encoded cubes (decode w != 0).
                half extra = ReflectionTopUp();
                half extraAll = extra;
                extra *= 1.0h - fracture;
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
                // GD3: the crack face's own light = the scene it mirrors (CrackMirror), through two passes of the glass (slightly
                // green). Fallback without the scene texture: the zone cube along the mirror direction, times the lamps that light
                // this crack (dead lamps: dark). Direct lamp glints come from UniversalFragmentPBR above (the crack's specular).
                [branch] if (fracture > 0.5)
                {
                    half3 tintM = lerp(1.0h, _EdgeColor.rgb / max(max(_EdgeColor.r, _EdgeColor.g), max(_EdgeColor.b, 1e-3h)), _FractureTint);
                    half4 mir = half4(0, 0, 0, 0);
                    [branch] if (_FR_GlassSceneColor > 0.5)
                        mir = CrackMirror(input.positionWS, viewWS, inputData.normalWS, cross(input.axisU, input.axisV), input.positionCS, inputData.normalizedScreenSpaceUV);
                    half3 seen = mir.rgb;
                    [branch] if (mir.a < 0.5)
                    {
                        half rough = PerceptualSmoothnessToPerceptualRoughness(s.smoothness);
                        seen = GlossyEnvironmentReflection(reflect(-viewWS, inputData.normalWS), inputData.positionWS, rough, 1.0h, inputData.normalizedScreenSpaceUV)
                             * (1.0h + extraAll) * CrackLampLight(inputData);
                    }
                    color.rgb += seen * _FractureMirror * tintM;
                }
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

        // [G14-HOOK-BEGIN] prepass is stripped from the keyword-off baseline used by the proof harness.
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
                half _PaneFromUV;
                half _Crush;
                float _CrushRadius;
                float4 _BowCentre;
                float _BowAmp;
                half _FractureReflect;
                half _FractureSmoothness;
                half _FractureMirror;
                half _FractureTint;
                half _FractureLampRef;
            CBUFFER_END

            TEXTURE2D(_GrimeMap);    SAMPLER(sampler_GrimeMap);
            TEXTURE2D(_SmearNormal); SAMPLER(sampler_SmearNormal);
            TEXTURE2D_X_FLOAT(_FRGlassRTSceneDepth);
            float _FR_GlassRTRollScale;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv0        : TEXCOORD0;   // GD3 (_PaneFromUV): pane metres from the slab centre
                float4 uv1        : TEXCOORD1;   // GD3: per-pane hash keys (xy), band (z), kind (w)
                half4 color       : COLOR;       // GD3: R fracture-edge mask, G segment random, B depth 0-1
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
                float4 gd3        : TEXCOORD6;   // GD3: x segment random, y depth 0-1, z band, w kind
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
                [branch] if (_PaneFromUV > 0.5)
                {
                    // GD3 stage meshes (same rule as ForwardLit). Fracture faces, crushed glass and outer edges never trace.
                    o.pane = float4(input.uv0, 0.5 * _PaneSize.xy);
                    o.axisU = SafeNormalize(axisX);
                    o.axisV = SafeNormalize(axisY);
                    o.misc.xyz = float3(step(0.5, input.uv1.w), Hash11(input.uv1.x), Hash11(input.uv1.y + 0.37));
                    o.gd3 = float4(input.color.g, input.color.b, input.uv1.z, input.uv1.w);
                }
                return o;
            }

            // ---- GD3 (destruction plan §4.2): bow, crushed bruise, fracture-face ripples. ALU only, uniform branches.
            // Normalised deflection of a simply supported span with the load at a (0-1) and its slope: sin(pi g), g mapping
            // [0, a] -> [0, .5] and [a, 1] -> [.5, 1]; zero outside the span (the stops hold the glass).
            float2 BowAxis(float t, float a)
            {
                a = clamp(a, 0.05, 0.95);
                float tc = saturate(t);
                float g = tc < a ? 0.5 * tc / a : 0.5 + 0.5 * (tc - a) / (1.0 - a);
                float dg = tc < a ? 0.5 / a : 0.5 / (1.0 - a);
                float inside = step(0.0, t) * step(t, 1.0);
                return float2(sin(PI * g), PI * cos(PI * g) * dg) * inside;
            }

            // Slope (dw/du, dw/dv) of the bow at pane point m (metres from the pane centre); _BowCentre.xy = the load,
            // .zw = half size of the held glass; _BowAmp = peak displacement toward the pane's +Z (signed metres).
            float2 BowSlope(float2 m)
            {
                float2 h = max(_BowCentre.zw, 1e-3);
                float2 t = (m + h) / (2.0 * h);
                float2 a = (_BowCentre.xy + h) / (2.0 * h);
                float2 fx = BowAxis(t.x, a.x);
                float2 fy = BowAxis(t.y, a.y);
                return _BowAmp * float2(fx.y * fy.x / (2.0 * h.x), fx.x * fy.y / (2.0 * h.y));
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash11(dot(i, float2(1.0, 57.0)));
                float b = Hash11(dot(i + float2(1.0, 0.0), float2(1.0, 57.0)));
                float c = Hash11(dot(i + float2(0.0, 1.0), float2(1.0, 57.0)));
                float d = Hash11(dot(i + float2(1.0, 1.0), float2(1.0, 57.0)));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // The crushed bruise round the impact on pane faces: granular, irregular edge, about 1.3 x the crater radius.
            half CrushMask(float2 m, float2 halfSize)
            {
                float2 d = m - (_ImpactUV.xy - 0.5) * 2.0 * halfSize;
                float r = length(d);
                float R = max(_CrushRadius, 1e-4);
                float grain = ValueNoise(m * 1100.0);
                float edge = ValueNoise(float2(atan2(d.y, d.x) * 3.0, 7.3));
                // reaches just past the crater (S1) and stays as a crushed white rim round the hole once the core falls out (S2);
                // never past 14.5 mm from the impact (plan sec 2.3: the crushed spot is at most 1.5 cm)
                half halo = (half)(1.0 - smoothstep(0.55 * R, min((1.06 + 0.14 * edge) * R, 0.0145), r));
                return (half)_Crush * halo * (half)saturate(0.25 + 0.95 * grain * grain);
            }

            // Hackle and rib marks on a fracture face: a small tilt of the face normal across the 6 mm (depth 0-1) and along
            // the crack (pane metres), so a crack's mirror image breaks up and its brightness changes along it (V3).
            float FractureRipple(float2 m, float depth, float seg)
            {
                // centimetre-scale runs along the crack (not millimetre stitches), one slow rib across the depth
                float along = ValueNoise(m * 90.0 + seg * 31.0) - 0.5;
                float fine = ValueNoise(m * 260.0 + seg * 17.0) - 0.5;
                return 0.06 * sin(TWO_PI * (depth * 0.9 + seg)) + 0.30 * along + 0.06 * fine;
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
                // GD3 (same as ForwardLit): only pane faces reach here (fracture faces, crushed glass and outer edges are clipped
                // above); the bruise and the analytic bow go into the traced normal.
                half crack = 0, crush = 0;
                [branch] if (_PaneFromUV > 0.5)
                {
                    [branch] if (_Crush > 0.001) crush = CrushMask(m, halfSize);
                    [branch] if (abs(_BowAmp) > 1e-7) tilt -= (half2)(BowSlope(m) * dot(nGeo, cross(input.axisU, input.axisV)));
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
        // [G14-HOOK-END]
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
