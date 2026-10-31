// FrontRooms/DoorGrit -- the door break's grit: 0.1-2 mm grains (research/door_break/54_dust_grit.md 4.1), production P1
// 2026-10-30. ONE procedural draw (Graphics.RenderPrimitives, 6 vertices per grain, no vertex buffer) from the 32-byte grains
// FrontRoomsDoorBreakVfx writes once a frame in world space. Each grain is a camera-facing quad stretched along its SCREEN
// motion over a 1/120 s exposure (capped at 24 px), at least 1 px wide, its opacity carrying its true covered size (Humus'
// phone-wire AA); the cross profile is one of 8 irregular grain silhouettes. Lit per vertex (one grain = one colour): SH,
// the main light and up to 8 Forward+ cluster lights with a 0.3 wrap on a TUMBLING normal (the twinkle), plus the slot or
// notch shaft x the HG phase. MSAA >= 2: alpha-to-coverage (order-free, writes depth). MSAA 1: a grain is drawn opaque when
// hash(grain) < its coverage, else dropped (decided per grain from _FR_GritParams.z, no keyword).
// Not on WebGL (no structured buffer in the vertex stage on GLES3; WebGL does not dress doors).
Shader "FrontRooms/DoorGrit"
{
    Properties
    {
        _GrainTex ("Grain atlas (R coverage, G height; 8 grains of 16 px)", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite On
            ZTest LEqual
            AlphaToMask On

            HLSLPROGRAM
            #pragma target 4.5
            #pragma exclude_renderers gles gles3 glcore
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "FrontRoomsDoorDust.hlsl"

            struct Grain { float3 pos; float diam; float3 vel; uint packed; };
            StructuredBuffer<Grain> _FR_Grit;
            TEXTURE2D(_GrainTex); SAMPLER(sampler_GrainTex);
            float4 _FR_GritParams;   // x exposure (s), y streak cap (px), z 1 = MSAA off (coverage keep)
            float4 _FR_GritScale;    // x drawn-size gain within 1.2 m, y from 3 m on, z time (s), w count

            struct V2F
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;                      // x across 0..1, y along 0..1
                half4 color : TEXCOORD1;                    // lit colour, a = coverage
                nointerpolation float4 misc : TEXCOORD2;    // x frame, y keep hash 0..1, z streak length / width, w fog
            };

            half3 GritLights(float3 positionWS, float3 n, float2 suv)
            {
                half3 acc = 0;
            #if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
                InputData inputData = (InputData)0;
                inputData.positionWS = positionWS;
                inputData.normalizedScreenSpaceUV = suv;
                uint pixelLightCount = GetAdditionalLightsCount();
                uint done = 0u;
            #if USE_CLUSTER_LIGHT_LOOP
                [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                    Light dl = GetAdditionalLight(lightIndex, positionWS);
                    acc += dl.color * (dl.distanceAttenuation * FRDustWrap(dot(n, dl.direction)));
                }
            #endif
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    if (done < 8u)
                    {
                        Light l = GetAdditionalLight(lightIndex, positionWS);
                        acc += l.color * (l.distanceAttenuation * FRDustWrap(dot(n, l.direction)));
                        done++;
                    }
                LIGHT_LOOP_END
            #endif
                return acc;
            }

            V2F Vert(uint vid : SV_VertexID)
            {
                V2F o = (V2F)0;
                uint gi = vid / 6u;
                uint c = vid - gi * 6u;
                Grain g = _FR_Grit[gi];
                float2 corner = float2((c == 1u || c == 2u || c == 4u) ? 1.0 : 0.0, (c == 2u || c == 4u || c == 5u) ? 1.0 : 0.0);
                float3 p = g.pos;
                float4 c0 = TransformWorldToHClip(p);
                float4 c1 = TransformWorldToHClip(p - g.vel * _FR_GritParams.x);
                float2 halfScreen = _ScreenParams.xy * 0.5;
                float w0 = max(c0.w, 1e-4);
                float2 s0 = c0.xy / w0 * halfScreen;
                float2 s1 = c1.w > 1e-4 ? c1.xy / c1.w * halfScreen : s0;
                float2 ds = s0 - s1;
                float len = length(ds);
                float cap = _FR_GritParams.y;
                if (len > cap) { ds *= cap / len; len = cap; }
                float2 dir = len > 1e-3 ? ds / len : float2(0.0, 1.0);
                float2 nrm = float2(-dir.y, dir.x);
                // the drawn size: physical within 1.2 m, x the far gain from 3 m (Red's open choice, 54 4.12 a)
                float gain = lerp(_FR_GritScale.x, _FR_GritScale.y, saturate((w0 - 1.2) / 1.8));
                float pxPerM = halfScreen.y * abs(UNITY_MATRIX_P._m11) / w0;
                float dpx = g.diam * gain * pxPerM;
                float wpx = max(dpx, 1.0);
                float cov = saturate(dpx);
                float L = len + wpx;
                float2 mid = s0 - ds * 0.5;
                float2 sp = mid + dir * ((corner.y - 0.5) * L) + nrm * ((corner.x - 0.5) * wpx);
                o.positionCS = float4(sp / halfScreen * w0, c0.z, c0.w);
                if (g.diam <= 0.0) o.positionCS = float4(0, 0, -1, 1);    // a faded-out grain: degenerate
                o.uv = corner;

                uint seed = g.packed >> 24;
                half3 albedo = SRGBToLinear(half3(g.packed & 255u, (g.packed >> 8) & 255u, (g.packed >> 16) & 255u) / 255.0);
                float t = _FR_GritScale.z * (2.0 + (float)(seed & 7u)) + seed * 0.0245;
                float b = seed * 2.39996;
                float3 n = normalize(float3(sin(t) * cos(b), cos(t), sin(t) * sin(b)));
                float3 V = normalize(_WorldSpaceCameraPos - p);
                if (dot(n, V) < 0.0) n = -n;
                half3 light = SampleSH(n);
                Light mainLight = GetMainLight();
                light += mainLight.color * (mainLight.distanceAttenuation * FRDustWrap(dot(n, mainLight.direction)));
                float4 scr = ComputeScreenPos(c0);
                light += GritLights(p, n, scr.xy / max(scr.w, 1e-4));
                light += FRLeakShaft(p, V, 1.0);
                o.color = half4(albedo * light, cov);
                o.misc = float4((float)(seed & 7u), (FRDustHash(gi * 7919u + 17u) & 0xffffu) / 65535.0, L / wpx, ComputeFogFactor(c0.z));
                return o;
            }

            half4 Frag(V2F i) : SV_Target
            {
                // the grain's silhouette across the streak, soft-ended along it (the ends are the grain itself)
                float aspect = max(i.misc.z, 1.0);
                float along = i.uv.y * aspect;
                float sv = along < 0.5 ? along : (aspect - along < 0.5 ? 1.0 - (aspect - along) : 0.5);
                float2 auv = float2((i.misc.x + i.uv.x) / 8.0, sv);
                half cov = SAMPLE_TEXTURE2D(_GrainTex, sampler_GrainTex, auv).r;
                half a = i.color.a * cov;
                if (_FR_GritParams.z > 0.5)
                {
                    // MSAA off: coverage-weighted keep, one decision per grain
                    if (i.misc.y >= i.color.a || cov < 0.5h) discard;
                    a = 1.0h;
                }
                if (a < 1.0h / 255.0h) discard;
                half3 col = MixFog(i.color.rgb, i.misc.w);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
