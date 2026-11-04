// FrontRooms/RunGrit -- the Run! T2 re-stack's mortar grit (71_run_room.md s.5.3, s.5.8; card 2, the T2 body, 2026-11-04). The
// door break's grain look (FrontRooms/DoorGrit, research/door_break/54_dust_grit.md 4.1: a camera-facing quad per grain streaked
// along its screen motion over a 1/120 s exposure, at least 1 px wide with the opacity carrying its true size, one of 8 grain
// silhouettes, lit per vertex on a tumbling normal), but ANALYTIC: FrontRoomsRunGrit writes each grain once when its course
// lands (start point, start time, start velocity, colour and size class: 32 bytes) and this vertex stage moves it: gravity
// with linear drag to its size's settling speed (0.7 m/s at 0.1 mm, 8 m/s at 2 mm), resting on the floor once it gets there
// (the contact found by Newton steps), fading out by 2.6 s. No per-frame C# simulation. ONE procedural draw
// (Graphics.RenderPrimitives, 6 vertices a grain, no vertex buffer). Not on WebGL (no structured buffer in the vertex stage on
// GLES3; WebGL never builds the re-stack).
Shader "FrontRooms/RunGrit"
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

            struct Grain { float3 p0; float t0; float3 v0; uint packed; };
            StructuredBuffer<Grain> _FR_RunGrit;
            TEXTURE2D(_GrainTex); SAMPLER(sampler_GrainTex);
            float4 _FR_GritParams;   // x exposure (s), y streak cap (px), z 1 = MSAA off (coverage keep), w the floor's world y
            float4 _FR_GritScale;    // x drawn-size gain within 1.2 m, y from 3 m on, z clock (s, the tumble), w count
            float _FR_RunGritClock;  // the recede's clock (s since the trip): the grains' time base

            #define RUNGRIT_LIFE 2.6
            #define RUNGRIT_G 9.81

            struct V2F
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;                      // x across 0..1, y along 0..1
                half4 color : TEXCOORD1;                    // lit colour, a = coverage
                nointerpolation float4 misc : TEXCOORD2;    // x frame, y keep hash 0..1, z streak length / width, w fog
            };

            // the grain's place and velocity at age t: v(t) = vt + (v0 - vt) e^-kt, p(t) = p0 + vt t + (v0 - vt)(1 - e^-kt) / k
            void Fall(Grain g, float k, float vts, float t, out float3 p, out float3 v)
            {
                float3 vt = float3(0.0, -vts, 0.0);
                float e = exp(-k * t);
                p = g.p0 + vt * t + (g.v0 - vt) * ((1.0 - e) / k);
                v = vt + (g.v0 - vt) * e;
            }

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
                Grain g = _FR_RunGrit[gi];
                float age = _FR_RunGritClock - g.t0;
                // size class 0..255 -> 0.1..2 mm (log), settling speed and drag from the size
                float cls = (float)(g.packed >> 24) / 255.0;
                float diam = 0.0001 * exp(cls * 2.9957);
                float vts = min(0.7 * pow(diam / 0.0001, 0.83), 9.0);
                float k = RUNGRIT_G / vts;
                float3 p, v;
                Fall(g, k, vts, max(age, 0.0), p, v);
                float floorY = _FR_GritParams.w + diam * 0.5;
                if (p.y < floorY)
                {
                    // the floor contact: Newton steps on y(t) = floor, then the grain rests where it touched
                    float t = max(age, 0.0);
                    [unroll] for (int it = 0; it < 4; it++)
                    {
                        float3 pp, vv;
                        Fall(g, k, vts, t, pp, vv);
                        t -= (pp.y - floorY) / min(vv.y, -0.05);
                        t = max(t, 0.0);
                    }
                    float3 pv;
                    Fall(g, k, vts, t, p, pv);
                    p.y = floorY;
                    v = 0;
                }
                float fade = 1.0 - smoothstep(RUNGRIT_LIFE - 0.9, RUNGRIT_LIFE, age);
                float2 corner = float2((c == 1u || c == 2u || c == 4u) ? 1.0 : 0.0, (c == 2u || c == 4u || c == 5u) ? 1.0 : 0.0);
                float4 c0 = TransformWorldToHClip(p);
                float4 c1 = TransformWorldToHClip(p - v * _FR_GritParams.x);
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
                float gain = lerp(_FR_GritScale.x, _FR_GritScale.y, saturate((w0 - 1.2) / 1.8));
                float pxPerM = halfScreen.y * abs(UNITY_MATRIX_P._m11) / w0;
                float dpx = diam * gain * pxPerM;
                float wpx = max(dpx, 1.0);
                float cov = saturate(dpx) * fade;
                float L = len + wpx;
                float2 mid = s0 - ds * 0.5;
                float2 sp = mid + dir * ((corner.y - 0.5) * L) + nrm * ((corner.x - 0.5) * wpx);
                o.positionCS = float4(sp / halfScreen * w0, c0.z, c0.w);
                if (age < 0.0 || age > RUNGRIT_LIFE || cov <= 0.0) o.positionCS = float4(0, 0, -1, 1);    // not yet, or gone
                o.uv = corner;

                uint seed = FRDustHash(gi * 2654435761u + 977u);
                half3 albedo = SRGBToLinear(half3(g.packed & 255u, (g.packed >> 8) & 255u, (g.packed >> 16) & 255u) / 255.0);
                float tt = _FR_GritScale.z * (2.0 + (float)(seed & 7u)) + (seed >> 8 & 255u) * 0.0245;
                float b = (seed >> 16 & 255u) * 2.39996;
                float3 n = normalize(float3(sin(tt) * cos(b), cos(tt), sin(tt) * sin(b)));
                float3 V = normalize(_WorldSpaceCameraPos - p);
                if (dot(n, V) < 0.0) n = -n;
                half3 light = SampleSH(n);
                Light mainLight = GetMainLight();
                light += mainLight.color * (mainLight.distanceAttenuation * FRDustWrap(dot(n, mainLight.direction)));
                float4 scr = ComputeScreenPos(c0);
                light += GritLights(p, n, scr.xy / max(scr.w, 1e-4));
                o.color = half4(albedo * light, cov);
                o.misc = float4((float)(seed & 7u), (FRDustHash(gi * 7919u + 17u) & 0xffffu) / 65535.0, L / wpx, ComputeFogFactor(c0.z));
                return o;
            }

            half4 Frag(V2F i) : SV_Target
            {
                float aspect = max(i.misc.z, 1.0);
                float along = i.uv.y * aspect;
                float sv = along < 0.5 ? along : (aspect - along < 0.5 ? 1.0 - (aspect - along) : 0.5);
                float2 auv = float2((i.misc.x + i.uv.x) / 8.0, sv);
                half cov = SAMPLE_TEXTURE2D(_GrainTex, sampler_GrainTex, auv).r;
                half a = i.color.a * cov;
                if (_FR_GritParams.z > 0.5)
                {
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
