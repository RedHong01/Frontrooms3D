// FrontRooms/DoorHaze -- the door break's haze (research/door_break/54_dust_grit.md 4.1, 4.6), production P1 2026-10-30.
// Thin wisps, never fog: <= 64 per door in ONE procedural draw from a 48-byte buffer (FrontRoomsDoorBreakVfx). A wisp is a
// camera-facing quad whose long axis follows its screen velocity (aspect 1:1 to 1:3 by emitter), textured from an 8-frame
// filament atlas whose outline is never radially symmetric; over its life it ERODES (the threshold rises through G) and
// breaks into filaments instead of fading as a disc. Its colour is the dust's own albedo (tan wood, grey-white gypsum) x the
// light it scatters FORWARD (HG g 0.6, back gain <= 6): up to 4 Forward+ lights WITH their shadow maps on desktop (haze in
// the leaf's shadow stays dark, haze in a lit gap glows), + the slot / notch shaft, + SH x 0.5. Alpha = 1 - exp(-tau), one
// wisp <= 0.12, clamped <= 0.30; soft against geometry over 0.15 m (_CameraDepthTexture), faded 0.6 -> 0.25 m from the
// camera. Premultiplied (One, OneMinusSrcAlpha), no sorting. Phones (_FR_HazeParams.x = 1): lit per vertex, no shadows.
// P5 (2026-10-30): a wisp whose four corners all lie nearer than 0.249 m from the camera (inside the fade's zero, where every
// fragment discards) is collapsed in the vertex stage: the image is unchanged, but a wisp drifting at the lens no longer
// costs a screenful of discarded fragments. Phones fade from _FR_HazeParams.w (0.80 m, full at 1.15 m) instead of 0.25 m, so their cull
// reaches further: the phone haze-fill cap (54 4.8).
Shader "FrontRooms/DoorHaze"
{
    Properties
    {
        _WispTex ("Wisp atlas (R density, G erosion order; 4 x 2 frames)", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5
            #pragma exclude_renderers gles gles3 glcore
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "FrontRoomsDoorDust.hlsl"

            struct Wisp { float3 pos; float size; float3 vel; uint packed; float4 misc; };
            StructuredBuffer<Wisp> _FR_Haze;
            TEXTURE2D(_WispTex); SAMPLER(sampler_WispTex);
            float4 _FR_HazeParams;       // x 1 = phone tier (vertex lit, no shadows), y alpha cap, z soft depth (m), w phone fade start (m)
            float4 _FR_HazeAlbedo[4];    // 0 wood split, 1 settled head dust, 2 gypsum, 3 core

            struct V2F
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                nointerpolation float4 misc : TEXCOORD2;    // x tau0, y erosion threshold, z fade-in, w unused
                nointerpolation half3 albedo : TEXCOORD3;
                half3 vertexLight : TEXCOORD4;               // phone tier only
                float eyeDepth : TEXCOORD5;
                float fog : TEXCOORD6;
            };

            // the lights x the HG phase: dust scatters forward, toward a camera that looks into the light. Two copies: the
            // fragment one samples the lights' shadow maps (comparison samplers are fragment-only on Metal), the vertex one
            // (phones) does not.
            half3 HazeLightsVertex(float3 positionWS, float3 V, float2 suv)
            {
                half3 acc = 0;
            #if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
                InputData inputData = (InputData)0;
                inputData.positionWS = positionWS;
                inputData.normalizedScreenSpaceUV = suv;
                uint pixelLightCount = GetAdditionalLightsCount();
                uint done = 0u;
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    if (done < 4u)
                    {
                        Light l = GetAdditionalLight(lightIndex, positionWS);
                        acc += l.color * (l.distanceAttenuation * FRDustHG(dot(-l.direction, V)));
                        done++;
                    }
                LIGHT_LOOP_END
            #endif
                Light mainLight = GetMainLight();
                acc += mainLight.color * (mainLight.distanceAttenuation * FRDustHG(dot(-mainLight.direction, V)));
                return acc;
            }

            half3 HazeLightsShadowed(float3 positionWS, float3 V, float2 suv)
            {
                half3 acc = 0;
            #if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
                InputData inputData = (InputData)0;
                inputData.positionWS = positionWS;
                inputData.normalizedScreenSpaceUV = suv;
                uint pixelLightCount = GetAdditionalLightsCount();
                uint done = 0u;
                half4 shadowMask = half4(1, 1, 1, 1);
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    if (done < 4u)
                    {
                        Light l = GetAdditionalLight(lightIndex, positionWS, shadowMask);
                        acc += l.color * (l.distanceAttenuation * l.shadowAttenuation * FRDustHG(dot(-l.direction, V)));
                        done++;
                    }
                LIGHT_LOOP_END
            #endif
                Light mainLight = GetMainLight();
                acc += mainLight.color * (mainLight.distanceAttenuation * FRDustHG(dot(-mainLight.direction, V)));
                return acc;
            }

            V2F Vert(uint vid : SV_VertexID)
            {
                V2F o = (V2F)0;
                uint wi = vid / 6u;
                uint c = vid - wi * 6u;
                Wisp w = _FR_Haze[wi];
                float2 corner = float2((c == 1u || c == 2u || c == 4u) ? 1.0 : 0.0, (c == 2u || c == 4u || c == 5u) ? 1.0 : 0.0);
                float3 p = w.pos;
                float3 toCam = normalize(_WorldSpaceCameraPos - p);
                float3 vel = w.vel - toCam * dot(w.vel, toCam);
                float3 worldUp = float3(0, 1, 0) - toCam * toCam.y;
                float3 up = dot(vel, vel) > 4e-4 ? normalize(vel) : (dot(worldUp, worldUp) > 1e-4 ? normalize(worldUp) : float3(1, 0, 0));
                float3 right = normalize(cross(toCam, up));
                float rot = (float)((w.packed >> 8) & 255u) / 127.0 - 1.0;
                float cr = cos(rot * 0.6), sr = sin(rot * 0.6);
                float3 r2 = right * cr + up * sr;
                float3 u2 = up * cr - right * sr;
                float aspect = max(w.misc.z, 1.0);
                float hx = w.size * 0.5 / sqrt(aspect);
                float hy = w.size * 0.5 * sqrt(aspect);
                float3 pos = p + r2 * ((corner.x * 2.0 - 1.0) * hx) + u2 * ((corner.y * 2.0 - 1.0) * hy);
                o.positionWS = pos;
                o.positionCS = TransformWorldToHClip(pos);
                uint frame = w.packed & 15u;
                o.uv = float2(((float)(frame & 3u) + corner.x) * 0.25, ((float)(frame >> 2) + corner.y) * 0.5);
                o.misc = float4(w.misc.x, w.misc.y, w.misc.w, 0);
                o.albedo = (half3)_FR_HazeAlbedo[(w.packed >> 4) & 3u].rgb;
                o.eyeDepth = -TransformWorldToView(pos).z;
                o.fog = ComputeFogFactor(o.positionCS.z);
                // P5: wholly inside the camera fade (camFade = 0 at <= 0.25 m; phones 0.80 m): collapse the quad (no fragments;
                // the same image)
                float farthest = 0.0;
                [unroll] for (uint k = 0u; k < 4u; k++)
                {
                    float2 sg = float2((k & 1u) != 0u ? 1.0 : -1.0, (k & 2u) != 0u ? 1.0 : -1.0);
                    farthest = max(farthest, -TransformWorldToView(p + r2 * (sg.x * hx) + u2 * (sg.y * hy)).z);
                }
                if (farthest < (_FR_HazeParams.x > 0.5 ? _FR_HazeParams.w : 0.25) - 0.001)
                {
                    o.positionCS = float4(2.0, 2.0, 2.0, 1.0);
                    return o;
                }
                if (_FR_HazeParams.x > 0.5)
                {
                    float3 V = toCam;
                    float4 scr = ComputeScreenPos(o.positionCS);
                    o.vertexLight = HazeLightsVertex(pos, V, scr.xy / max(scr.w, 1e-4)) + FRLeakShaft(pos, V, 1.0);
                }
                return o;
            }

            half4 Frag(V2F i) : SV_Target
            {
                half2 t = SAMPLE_TEXTURE2D(_WispTex, sampler_WispTex, i.uv).rg;
                // erosion: thin density and the gaps between filaments go first
                half dens = t.r * saturate((t.g - (half)i.misc.y) * 6.0h);
                float tau = i.misc.x * dens * i.misc.z;
                float2 suv = GetNormalizedScreenSpaceUV(i.positionCS);
                float sceneZ = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams);
                float soft = saturate((sceneZ - i.eyeDepth) / max(_FR_HazeParams.z, 1e-3));
                float camFade = _FR_HazeParams.x > 0.5 ? saturate((i.eyeDepth - _FR_HazeParams.w) / 0.35) : saturate((i.eyeDepth - 0.25) / 0.35);
                float a = min((1.0 - exp(-tau)) * soft * camFade, _FR_HazeParams.y);
                if (a < 1.0 / 512.0) discard;
                float3 V = normalize(_WorldSpaceCameraPos - i.positionWS);
                half3 light;
                if (_FR_HazeParams.x > 0.5) light = i.vertexLight;
                else light = HazeLightsShadowed(i.positionWS, V, suv) + FRLeakShaft(i.positionWS, V, 1.0);
                light += SampleSH(V) * 0.5h;
                half3 col = MixFog(i.albedo * light, i.fog);
                return half4(col * a, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
