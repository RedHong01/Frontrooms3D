// FrontRooms CRT tube: the physical layer of every in-game screen (SCREENS_VISUAL_SYSTEM.md section 3, layer 3; Red
// approved the tube in the graphic-visual advertising chat on 2026-10-22). The shader is the game-visual line's; the look
// spec is the advertising line's. Report: Documentation/research/screens/40_tube_layer.md. The name stays
// FrontRooms/ScreenVideo, so the desktop RT glass still traces the picture as emission only
// (FrontRoomsGlassRTSystem.Describe: _BaseMap x _BaseColor); a set that is off uses a material with _BaseColor black.
//
// One opaque, unlit pass on the curved picture surface FrontRoomsScreenVideo builds 2 mm in front of the kit's own face.
// The glass in front of it is NOT here: it is the glass system's own material (FrontRooms/Glass) on a second curved
// surface (desktop only), so RT on and the raster fallback match by construction.
//
// NO BOOST. Nothing here raises a level: no gain, the halation is energy-conserving (a field of level L stays L), the
// lines and the mask have a mean of 1, and the output never leaves 0..1 (the post Bloom threshold is 1.05, so screen
// whites never bloom). Even the collapsed beam of a power change stays at or below the steady picture's white.
//
// Per set (FrontRoomsScreenVideo.PushTubeGeometry, when the picture changes between the broadcast and a V4 still):
//   raster   _Raster.xy = half size of the lit raster in face units (face = the visible curved face, -1..1), .z barrel,
//            .w raster corner radius (face units). Kit_CRTTV: the broadcast is overscanned, 4.5 % top and bottom, ~4.7 %
//            at the sides, through the barrel, and the raster still covers the face corners. Kit_CRTMonitor (Red's pick,
//            2026-10-22 21:23): underscanned, the whole 4:3 frame lit at 276 x 207 mm in the 300 x 228 mm opening, the
//            border is the unlit phosphor body behind the faceplate (dark glass, never flat black); a V4 still lands on
//            the same area with no barrel.
//   lines    _Lines = 480 (broadcast, 480 visible lines). Band-limited: a single cosine fades in from 2 to 4 screen px per
//            line, its second harmonic only from 4 to 8 px, so no line pattern is ever drawn above the pixel grid's Nyquist
//            limit (no shimmer, no moire, at any distance or resolution). Mean 1: the lines never change the average level.
//            Dark lines are thinner than bright ones, never black.
//   mask     an aperture grille on the glass, in metres (_Face.xy = face half size, .z = grille pitch, .w = type: 0
//            aperture grille; 1 slot mask and 2 dot triad are kept for later kits). Mean 1 per channel, fades in from 3 to
//            6 px per period: at play distance it is far below a pixel, so only close-ups resolve it.
//   halation (Full, live picture) light scattered in the faceplate: lerp(picture, ring blur, _Halation). A uniform field
//            keeps its level; bright type gets a faint halo over a dark ground. 8 trilinear taps of mip 2.
//   vignette (Full, Reduced; live picture) brightness at the face corners falls by _Vignette.
//   power    _PowerH / _PowerV scale the lit raster (the collapse to a line and a dot), the beam concentrates its energy
//            in what is left (never above white), _Beam adds the white of the collapsed line, _Warm is the warm-up gain.
//            FrontRoomsScreenVideo drives them; nothing here reads a lamp or a warning.
//   still    _Still = 1 when the surface shows a still texture, not the shared ad RT (V4 desk frames): 1:1, none of the
//            tube's image effects (no barrel, lines, grille, halation, vignette or convergence error; FrontRoomsDeskStill:
//            "no tube effects"); _StillRect undoes V4's Fit scale inside the raster, so a 640 x 480 frame fills it exactly.
//   body     the phosphor screen behind the faceplate (sRGB 38, 45, 40), lit by the ambient probe only: the black level
//            of a lit room; a set that is off is this plus the faceplate (dark glass).
//   _TubeLevel 0 Full, 1 Reduced (no grille, no halation, half-depth lines, a dim beam), 2 Off (plain picture: no barrel,
//            lines, grille, halation or vignette; power is a 1.5 s fade). C26 wires a setting to it later.
// Uniform branches only: no new keywords beyond URP's fog and the depth-normals packing.
Shader "FrontRooms/ScreenVideo"
{
    Properties
    {
        [MainTexture] _BaseMap ("Video texture", 2D) = "black" {}
        [MainColor] _BaseColor ("Tint (black = a set that is off)", Color) = (1, 1, 1, 1)
        _TubeLevel ("Tube level: 0 Full, 1 Reduced, 2 Off", Float) = 0
        _Still ("A still frame, not the ad RT (1:1)", Float) = 0
        _StillRect ("1 / V4 Fit scale (still frames)", Vector) = (1, 1, 0, 0)
        _PowerH ("Lit raster width 0..1", Float) = 1
        _PowerV ("Lit raster height 0..1", Float) = 1
        _Beam ("Collapsed beam white", Float) = 0
        _Warm ("Picture warm-up 0..1", Float) = 1
        _Signal ("A video frame is decoded (0 = dark raster)", Float) = 1
        _Lines ("Scan lines over the raster height", Float) = 480
        _Raster ("Raster half size (face units) xy, barrel z, corner w", Vector) = (1.1591, 1.1538, 0.05, 0.06)
        _Face ("Face half size m xy, grille pitch m z, mask type w", Vector) = (0.146, 0.11, 0.00025, 0)
        _Halation ("Faceplate halation (energy-conserving)", Range(0, 0.4)) = 0.12
        _Vignette ("Corner falloff", Range(0, 0.5)) = 0.15
        _Body ("Phosphor body (sRGB swatch, ambient-lit)", Color) = (0.149, 0.176, 0.157, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _Body;
            float4 _StillRect, _Raster, _Face;
            float _TubeLevel, _Still, _PowerH, _PowerV, _Beam, _Warm, _Signal;
            float _Lines, _Halation, _Vignette;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "FrontRoomsScreenVideo"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/AmbientProbe.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogCoord : TEXCOORD2;
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
                o.positionCS = p.positionCS;
                o.uv = input.uv;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.fogCoord = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            // Signed distance to a rounded rectangle (negative inside).
            float RoundRect(float2 p, float2 halfSize, float r)
            {
                r = min(r, min(halfSize.x, halfSize.y));
                float2 q = abs(p) - halfSize + r;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            // Hexagonal lattice of spacing a (same-colour dots): peaks of 3 on the lattice points, mean 0.
            float HexDots(float2 p, float a)
            {
                float k = 7.2551975 / a;   // 4 pi / (a sqrt 3)
                return cos(k * p.y) + cos(k * dot(p, float2(0.8660254, -0.5))) + cos(k * dot(p, float2(-0.8660254, -0.5)));
            }

            // The phosphor mask on the glass, mean 1 per channel. pm = metres on the face.
            half3 Mask(float2 pm, float pitch, float type)
            {
                if (type > 1.5)
                {
                    // dot triad (delta), dot pitch = pitch: three hex lattices on the triangle's centroids
                    float3 s;
                    s.r = HexDots(pm, pitch);
                    s.g = HexDots(pm - pitch * float2(0.5, 0.2886751), pitch);
                    s.b = HexDots(pm - pitch * float2(1.0, 0.5773503), pitch);
                    return (half3)(1.0 + 0.8 * s / 3.0);
                }
                float tx = pm.x / pitch;
                half3 stripes = (half3)(1.0 + 0.6 * cos(tx * 6.28318531 - float3(0.0, 2.0943951, 4.1887902)));
                if (type > 0.5)
                {
                    // slot mask: the stripes broken by bridges, every other triad column offset half a slot
                    float col = floor(tx);
                    float ty = pm.y / (pitch * 1.25) + 0.5 * (col - 2.0 * floor(col * 0.5));
                    stripes *= (half)(1.0 + 0.3 * cos(ty * 6.28318531));
                }
                return stripes;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float full = 1.0 - step(0.5, _TubeLevel);      // 1 at Full
                float plain = step(1.5, _TubeLevel);           // 1 at Off
                float reduced = 1.0 - full - plain;            // 1 at Reduced
                float still = step(0.5, _Still);
                float live = 1.0 - still;                      // image effects only on the live picture

                // face units (-1..1 over the visible face) and the raster seen through the barrel
                float2 c0 = input.uv * 2.0 - 1.0;
                float r2 = dot(c0, c0);
                float2 c = c0 * (1.0 + _Raster.z * (1.0 - plain) * r2);

                // the lit raster: _Raster.xy in face units (over- or underscanned), scaled by the power collapse
                float2 ps = max(float2(_PowerH, _PowerV), float2(1e-4, 1e-4));
                float2 rh = _Raster.xy * ps;
                float2 vuv = (c / rh) * 0.5 + 0.5;             // 0..1 over the lit raster

                float aa = max(fwidth(c.x), fwidth(c.y)) + 1e-5;
                float d = RoundRect(c, rh, _Raster.w * (1.0 - plain));
                float lit = 1.0 - smoothstep(-aa, aa, d);

                // the texture: the live RT fills the 4:3 raster; a still frame keeps V4's Fit over the screenSize rectangle
                float2 tuv = lerp(vuv, 0.5 + (vuv - 0.5) * _StillRect.xy, still);
                tuv = tuv * _BaseMap_ST.xy + _BaseMap_ST.zw;

                // a little convergence error toward the edges (Full, live picture)
                float2 conv = c * (0.0012 * full * live);
                half3 col;
                col.r = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, tuv + conv).r;
                col.g = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, tuv).g;
                col.b = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, tuv - conv).b;

                // halation in the faceplate (Full, live): energy-conserving ring blur of mip 2 (8 trilinear taps)
                float hk = _Halation * full * live;
                if (hk > 0.0005)
                {
                    float2 rad = float2(0.0095, 0.0127);       // ~6 raster px at 640 x 480
                    half3 ring = 0;
                    ring += SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, tuv + rad * float2( 1.0,  0.0), 2.0).rgb;
                    ring += SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, tuv + rad * float2(-1.0,  0.0), 2.0).rgb;
                    ring += SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, tuv + rad * float2( 0.0,  1.0), 2.0).rgb;
                    ring += SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, tuv + rad * float2( 0.0, -1.0), 2.0).rgb;
                    ring += SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, tuv + rad * float2( 0.7071,  0.7071), 2.0).rgb;
                    ring += SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, tuv + rad * float2(-0.7071,  0.7071), 2.0).rgb;
                    ring += SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, tuv + rad * float2( 0.7071, -0.7071), 2.0).rgb;
                    ring += SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, tuv + rad * float2(-0.7071, -0.7071), 2.0).rgb;
                    col = lerp(col, ring * 0.125h, (half)hk);
                }

                // a collapsing raster stacks every line on top of the others: it shows the frame's average colour
                half3 avg = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, float2(0.5, 0.5), 9.0).rgb;
                col = lerp(avg, col, (half)saturate(ps.y * 6.0));
                float lum = saturate(dot((float3)col, float3(0.2126, 0.7152, 0.0722)));

                // scan lines, band-limited (see the header): 1 + a1 cos(2 pi t) + a2 cos(4 pi t), t = 0 on a line centre
                float ly = vuv.y * _Lines;
                float pxPerLine = 1.0 / max(fwidth(ly), 1e-4);
                float lineK = (full + 0.5 * reduced) * live;
                float amp1 = saturate((pxPerLine - 2.0) * 0.5) * lineK;
                float amp2 = saturate((pxPerLine - 4.0) * 0.25) * lineK;
                float sl = sqrt(lum);
                float t = (frac(ly) - 0.5) * 6.28318531;
                float beam = 1.0 + amp1 * lerp(0.50, 0.22, sl) * cos(t) + amp2 * lerp(0.16, 0.04, sl) * cos(2.0 * t);
                col *= (half)beam;

                // the phosphor mask on the glass (Full)
                float2 pm = c0 * _Face.xy;
                float period = _Face.z * (_Face.w > 1.5 ? 0.8660254 : 1.0);
                float pxPerPeriod = period / max(max(fwidth(pm.x), fwidth(pm.y)), 1e-7);
                float maskAmp = saturate((pxPerPeriod - 3.0) / 3.0) * full * live;
                if (maskAmp > 0.001) col *= lerp((half3)1.0h, Mask(pm, _Face.z, _Face.w), (half)maskAmp);

                // the beam concentrates its energy in a collapsed raster (capped), and the line burns white
                float conc = clamp(1.0 / (ps.x * ps.y), 1.0, 1.0 + 3.0 * full + 0.5 * reduced);
                float vig = 1.0 - _Vignette * (full + 0.6 * reduced) * live * pow(saturate(r2 * 0.5), 1.5);
                half3 pic = col * (half)(_Warm * _Signal * conc * vig);
                half3 lineWhite = (half3)(_Beam * _Warm * (full + 0.35 * reduced) * step(ps.y, 0.05));
                // never above white, not even the collapsed beam: mask and line peaks saturate, nothing glows or blooms
                pic = min(pic + lineWhite, (half3)1.0h);
                // a powered tube without a decoded frame yet: a faint raster glow (the warm-up look), never pure black
                half3 rasterGlow = (half3)(0.006 * (1.0 - _Signal) * _Warm * beam);
                half3 emission = (pic + rasterGlow) * _BaseColor.rgb * (half)lit;

                // the phosphor screen behind the faceplate, lit by the room's ambient probe
                half3 body = _Body.rgb * max((half3)SampleSH(normalize(input.normalWS)), (half3)0.0h);
                half3 outCol = body + emission;
                outCol = MixFog(outCol, input.fogCoord);
                return half4(outCol, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct DAttributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct DVaryings { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };

            DVaryings DepthVert(DAttributes input)
            {
                DVaryings o = (DVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return o;
            }

            half DepthFrag(DVaryings input) : SV_Target
            {
                return (half)input.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormalsOnly"
            Tags { "LightMode" = "DepthNormalsOnly" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex NVert
            #pragma fragment NFrag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing

            struct NAttributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct NVaryings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };

            NVaryings NVert(NAttributes input)
            {
                NVaryings o = (NVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            half4 NFrag(NVaryings input) : SV_Target
            {
            #if defined(_GBUFFER_NORMALS_OCT)
                float3 n = normalize(input.normalWS);
                float2 oct = PackNormalOctQuadEncode(n);
                half3 packed = (half3)PackFloat2To888(saturate(oct * 0.5 + 0.5));
                return half4(packed, 0.0h);
            #else
                return half4(NormalizeNormalPerPixel(input.normalWS), 0.0h);
            #endif
            }
            ENDHLSL
        }
    }
    Fallback Off
}
