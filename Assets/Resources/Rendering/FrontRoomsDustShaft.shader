// Dust in lamp light (tech_art row 9, research/tech_art/20_ranked_changes.md 3.9; Red 2026-11-04 12:49 in the tech_art
// chat: the corridor's 60 additive beams go, and depth- and view-angle-faded shafts come back only where a room family
// wants dust). Built and driven by FrontRoomsDustShaft (Scripts/Rendering); nothing draws it until a room asks for dust.
//
// The renderer is a box below a lamp's lens (object space: y = 0 at the lens face, down to -length). Only its far faces
// draw (Cull Front, ZTest Always), so a camera inside the box still sees the shaft once; the fragment marches its own view
// ray through the box (a few jittered steps) and stops at the scene depth, so the dust ends where the floor, a desk or a
// wall begins (the RunBeamCone depth fade, done as a volume: no intersection line). Each step adds the light that reaches
// it from the lens: brightest just under the lens and gone well above the floor, spreading sideways from the lens's
// rectangle with a smooth tail, so the shaft has no flat side and nothing to show edge-on (the RunBeamCone view-angle
// fade, done as a volume); Henyey-Greenstein forward scattering (a shaft glows most when the eye looks toward its lamp);
// and drifting dust from a small tiling noise volume (FrontRoomsDustShaft makes it once), squashed into thin layers.
// One material for every shaft, no MaterialPropertyBlock: the lamp's colour (RGB565), level (8 bit) and a per-shaft seed
// (8 bit) ride in unity_RendererUserValue (MeshRenderer.SetShaderUserValue), so the shafts stay in the SRP Batcher. The
// box sizes ride in the mesh (TEXCOORD0/1), so lamps of any size share the material.
Shader "FrontRooms/DustShaft"
{
    Properties
    {
        _Density ("Density (added radiance per metre of fully lit dust at dust 1)", Float) = 0.1
        _Anisotropy ("Forward scattering g (Henyey-Greenstein)", Range(0, 0.9)) = 0.35
        _Steps ("March steps per pixel", Range(2, 16)) = 8
        _Falloff ("Depth below the lens where the light has halved (m)", Float) = 0.5
        _NoiseScale ("Dust noise tiles per metre", Float) = 0.6
        _NoiseAmount ("Dust contrast (0 = even haze)", Range(0, 1)) = 0.8
        _Drift ("Drift (m/s)", Float) = 0.04
        _NearFade ("Dust closer than this to the eye fades (m)", Float) = 0.35
        [NoScaleOffset] _DustNoise ("Dust noise (3D, made by FrontRoomsDustShaft)", 3D) = "" {}
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }

        Pass
        {
            Name "DustShaft"
            Tags { "LightMode" = "UniversalForward" }
            // Dust only adds light it scatters toward the eye.
            Blend One One
            Cull Front
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Density;
                float _Anisotropy;
                float _Steps;
                float _Falloff;
                float _NoiseScale;
                float _NoiseAmount;
                float _Drift;
                float _NearFade;
            CBUFFER_END

            TEXTURE3D(_DustNoise);
            SAMPLER(sampler_DustNoise);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 shape : TEXCOORD0;   // lens half x, lens half z, length, sideways spread (blur width gained per metre down)
                float4 shape2 : TEXCOORD1;  // box half x, box half z, blur width at the lens, unused
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                nointerpolation float4 shape : TEXCOORD1;
                nointerpolation float4 shape2 : TEXCOORD2;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.shape = v.shape;
                o.shape2 = v.shape2;
                return o;
            }

            // Interleaved gradient noise (Jimenez 2014): a per-pixel offset of the march, so a few steps leave no bands.
            float StepJitter(float2 pixel)
            {
                return frac(52.9829189 * frac(dot(pixel, float2(0.06711056, 0.00583715))));
            }

            // The lit dust at p (object space, metres below the lens = -p.y), seen along wo (toward the eye).
            float LitDust(float3 p, float4 shape, float4 shape2, float3 wo, float3 n1, float3 n2)
            {
                float h = -p.y;
                // Sideways: full under the lens's rectangle, then a smooth tail that widens with depth; nothing at the box.
                float w = shape2.z + shape.w * h;
                float2 d = max(abs(p.xz) - shape.xy, 0.0) / w;
                float2 l = 1.0 + d * d;
                float2 win = 1.0 - smoothstep(0.75 * shape2.xy, shape2.xy, abs(p.xz));
                float side = win.x * win.y / (l.x * l.x * l.y * l.y);
                // Down: light from the lens halves at _Falloff; the last part of the box fades out.
                float down = smoothstep(0.0, 0.06, h) * (1.0 - smoothstep(0.55 * shape.z, shape.z, h));
                float fall = 1.0 / (1.0 + (h * h) / (_Falloff * _Falloff));
                // Forward scattering from the nearest lens point, 1 at 90 degrees.
                float3 lensPoint = float3(clamp(p.x, -shape.x, shape.x), 0.0, clamp(p.z, -shape.y, shape.y));
                float3 wi = normalize(p - lensPoint + float3(0.0, -0.02, 0.0));
                float g = _Anisotropy;
                float g2 = g * g;
                float x = (1.0 + g2) / max(1.0 + g2 - 2.0 * g * dot(wi, wo), 0.001);
                float phase = x * sqrt(x);
                // Dust: two scales of the noise volume, squashed into thin layers, drifting; contrast lifts the wisps.
                float3 q = float3(p.x, p.y * 1.7, p.z) * _NoiseScale;
                float wv = 0.62 * SAMPLE_TEXTURE3D_LOD(_DustNoise, sampler_DustNoise, q + n1, 0).r
                         + 0.38 * SAMPLE_TEXTURE3D_LOD(_DustNoise, sampler_DustNoise, q * 2.7 + n2, 0).r;
                float dust = lerp(1.0, 2.0 * smoothstep(0.30, 0.80, wv), _NoiseAmount);
                return side * down * fall * phase * dust;
            }

            half4 frag(Varyings i) : SV_Target
            {
                uint u = unity_RendererUserValue;
                float level = (float)((u >> 8) & 255u) * (1.0 / 255.0);
                if (level <= 0.0) return half4(0.0, 0.0, 0.0, 0.0);
                float3 tint = float3((float)((u >> 27) & 31u) * (1.0 / 31.0), (float)((u >> 21) & 63u) * (1.0 / 63.0), (float)((u >> 16) & 31u) * (1.0 / 31.0));
                float seed = (float)(u & 255u);

                // The view ray, in world metres (t) and in the box's own space.
                float3 camWS = GetCameraPositionWS();
                float3 rdWS = normalize(i.positionWS - camWS);
                float3 camOS = TransformWorldToObject(camWS);
                float3 rdOSu = TransformWorldToObjectDir(rdWS, false);
                float k = max(length(rdOSu), 1e-5);
                float3 rdOS = rdOSu / k;
                float3 bmin = float3(-i.shape2.x, -i.shape.z, -i.shape2.y);
                float3 bmax = float3(i.shape2.x, 0.0, i.shape2.y);
                float3 rdSafe = float3(abs(rdOS.x) < 1e-6 ? 1e-6 : rdOS.x, abs(rdOS.y) < 1e-6 ? 1e-6 : rdOS.y, abs(rdOS.z) < 1e-6 ? 1e-6 : rdOS.z);
                float3 inv = 1.0 / rdSafe;
                float3 ta = (bmin - camOS) * inv;
                float3 tb = (bmax - camOS) * inv;
                float3 tlo = min(ta, tb);
                float3 thi = max(ta, tb);
                float tNear = max(max(tlo.x, tlo.y), tlo.z) / k;
                float tFar = min(min(thi.x, thi.y), thi.z) / k;

                // The scene along the same ray: the dust stops at the first surface (and at the near plane).
                float3 forward = -UNITY_MATRIX_V[2].xyz;
                float cosF = max(dot(rdWS, forward), 1e-4);
                float2 suv = GetNormalizedScreenSpaceUV(i.positionCS);
                float tScene = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams) / cosF;
                float a = max(tNear, _ProjectionParams.y / cosF);
                float b = min(tFar, tScene);
                if (b <= a) return half4(0.0, 0.0, 0.0, 0.0);

                int steps = (int)clamp(_Steps, 2.0, 16.0);
                float dt = (b - a) / steps;
                float t = a + StepJitter(i.positionCS.xy) * dt;
                float3 wo = -rdOS;
                float drift = _Drift * _NoiseScale * _Time.y;
                float3 n1 = frac(float3(0.137, 0.611, 0.291) * seed) + float3(0.60, 0.06, 0.25) * drift;
                float3 n2 = frac(float3(0.733, 0.197, 0.859) * seed) + float3(-0.31, 0.04, 0.47) * (drift * 1.6);
                float sum = 0.0;
                [loop] for (int s = 0; s < 16; s++)
                {
                    if (s >= steps) break;
                    float3 p = camOS + rdOS * (t * k);
                    sum += LitDust(p, i.shape, i.shape2, wo, n1, n2) * smoothstep(0.05, _NearFade, t);
                    t += dt;
                }
                float3 rgb = tint * (level * _Density * sum * dt);
                // Light the dust adds fades toward black in the scene fog (like the lamps it comes from).
                half fog = ComputeFogFactorZ0ToFar(max(0.5 * (a + b) * cosF - _ProjectionParams.y, 0.0));
                rgb = MixFogColor(half3(rgb), half3(0.0, 0.0, 0.0), fog);
                return half4(rgb, 0.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
