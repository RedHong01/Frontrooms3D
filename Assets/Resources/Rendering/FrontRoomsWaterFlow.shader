// The pool family's falling and running water (structures G2-b, package structures_water_flow; research/structures_assets/20_research.md
// A.6, 30_plan.md P13; W/structures_assets/g2_review_assets.md section 9). Game visual line.
//
// The still water is FrontRooms/Water (a level surface, reflection only, planar or probe). This is what moves: the sheets that fall
// over a weir lip (a nappe on its free-fall path), the film that clings to a tiled wall (SP2's cascade), the thin film that rides in
// the slide's trough, and the foam where water lands. Meshes come from FrontRoomsStructureFalls (district metres):
//   UV0  u metres across the flow (along the lip), v metres down the flow from where the water leaves (the lip, the slot, the tub)
//   color  R coverage (0 none .. 1 a whole sheet; the builder fades the sheet's ends and its foot), G foam (aerated water: the
//          impact line, the lip's spill), B mode (0 a free sheet falling from a lip: free-fall speed; 0.5 a steady film, the
//          slide's trough; 1 a film clinging to a tiled wall: steady, rivulets, the tile's joints trip it), A speed scale (x the
//          lip speed for the steady modes: the trough's film runs with the ride, about 5 m/s)
// What it draws (premultiplied alpha: the sheet's own light plus its reflection over what shows through, as FrontRooms/Glass):
//   flow     the pool water's own ripple loops (FrontRoomsWaterRipples: _FR_RippleLong and _FR_RippleFine, their current slices)
//            sampled ALONG the flow, stretched into streaks (a falling sheet's striations) and carried down at the water's speed
//            (v0 at the lip, then free fall: sqrt(v0^2 + 2 g v)); two phases cross-faded so the stretch never runs away (Valve's
//            flow maps, SIGGRAPH 2010, PC29: two fetches each)
//   light    the environment as FrontRooms/Surface reads it (the renderer's probe, box projected), Fresnel for n = 1.333; the lamps as
//            GGX glints of a rough sheet (alpha at least _SheetParams.w) whose peak is CAPPED (_GlintCap): LW0, photosafety (the
//            0d finding: 4 flashes in a second from glints on rippled water) comes before anything that makes glints brighter
//   body     aerated water scatters: white where it foams, a faint aqua where it is a clear sheet; lit by the ambient and the lamps
//   alpha    a clear sheet transmits most of what is behind it (alpha 0.10 face-on) and reflects more at grazing; foam is opaque
//            white; the film's rivulets come and go along the wall (a 1-D pattern across the flow, slowly drifting)
// Nothing else changes: no other material uses this shader. WebGL draws the same path (two texture-array fetches a phase).
Shader "FrontRooms/WaterFlow"
{
    Properties
    {
        _FlowColor ("Clear sheet colour", Color) = (0.62, 0.84, 0.85, 1)
        _FoamColor ("Foam colour", Color) = (0.93, 0.97, 0.98, 1)
        _FlowParams ("Flow: speed at the lip m/s, gravity m/s2, streak stretch, slope gain", Vector) = (0.66, 9.81, 6, 0.6)
        _SheetParams ("Sheet: alpha face-on, alpha grazing, foam alpha, min roughness (GGX alpha)", Vector) = (0.10, 0.55, 0.92, 0.12)
        _GlintCap ("Lamp glint cap (linear radiance)", Float) = 2.0
        _LampGain ("Lamp glint gain", Float) = 0.6
        _FilmParams ("Film: rivulet share, rivulet width m, joint pitch m, joint line strength", Vector) = (0.55, 0.035, 0.15, 0.6)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Resources/Rendering/FrontRoomsWaterRipples.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _FlowColor;
                half4  _FoamColor;
                float4 _FlowParams;
                float4 _SheetParams;
                float  _GlintCap;
                float  _LampGain;
                float4 _FilmParams;
            CBUFFER_END

            #define FR_FLOW_IOR 1.333

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                half4 color : TEXCOORD4;
                float fogCoord : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.tangentWS = float4(TransformObjectToWorldDir(v.tangentOS.xyz), v.tangentOS.w);
                o.uv = v.uv;
                o.color = v.color;
                o.fogCoord = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            // Exact Fresnel reflectance of an air-to-water interface, unpolarised (n = 1.333), as FrontRooms/Water.
            float FRFlowFresnel(float c)
            {
                c = saturate(c);
                float g = sqrt(FR_FLOW_IOR * FR_FLOW_IOR - 1.0 + c * c);
                float a = (g - c) / (g + c);
                float b = (c * (g + c) - 1.0) / (c * (g - c) + 1.0);
                return saturate(0.5 * a * a * (1.0 + b * b));
            }

            float FRFlowHash(float x) { return frac(sin(x * 127.1 + 311.7) * 43758.5453); }

            // 1-D value noise (the film's rivulets across the wall)
            float FRFlowNoise(float x)
            {
                float i = floor(x), f = frac(x);
                float u = f * f * (3.0 - 2.0 * f);
                return lerp(FRFlowHash(i), FRFlowHash(i + 1.0), u);
            }

            // The pool water's own ripple slopes carried along the flow: (u, v) metres on the sheet, the water's speed there and the
            // stretch; two phases so the streaks are carried and renewed. Returns the slope (across, along) in radians.
            float2 FRFlowSlope(float2 uv, float speed, float stretch, float t)
            {
                if (_FR_RippleA.w <= 0.0) return 0.0;
                float period = 1.2;
                float p0 = frac(t / period);
                float p1 = frac(t / period + 0.5);
                float w0 = 1.0 - abs(2.0 * p0 - 1.0);
                float2 base = float2(uv.x, uv.y / stretch);
                float2 uv0 = base - float2(0.0, speed * p0 * period / stretch) + float2(0.0, 0.0);
                float2 uv1 = base - float2(0.0, speed * p1 * period / stretch) + float2(0.43, 0.17);
                float2 gL0 = SAMPLE_TEXTURE2D_ARRAY(_FR_RippleLong, sampler_FR_RippleLong, uv0 * _FR_RippleGeo.w * 2.0, _FR_RippleL.x).rg * 2.0 - 1.0;
                float2 gL1 = SAMPLE_TEXTURE2D_ARRAY(_FR_RippleLong, sampler_FR_RippleLong, uv1 * _FR_RippleGeo.w * 2.0, _FR_RippleL.x).rg * 2.0 - 1.0;
                float2 gF0 = SAMPLE_TEXTURE2D_ARRAY(_FR_RippleFine, sampler_FR_RippleFine, uv0 * _FR_RippleGeo.z, _FR_RippleA.x).rg * 2.0 - 1.0;
                float2 gF1 = SAMPLE_TEXTURE2D_ARRAY(_FR_RippleFine, sampler_FR_RippleFine, uv1 * _FR_RippleGeo.z, _FR_RippleA.x).rg * 2.0 - 1.0;
                float2 g = lerp(gL1 * _FR_RippleEnc.y + gF1 * _FR_RippleEnc.x, gL0 * _FR_RippleEnc.y + gF0 * _FR_RippleEnc.x, w0);
                // across the flow the striations are fine and sharp, along it soft: the slope's along part follows the stretch
                return float2(g.x, g.y / sqrt(stretch));
            }

            // One lamp's glint on the moving sheet: a GGX lobe of roughness alpha (the sheet is never a mirror), Fresnel, and a cap
            // on the peak so a glint sliding down a fall stays small and steady (photosafety, LW0).
            half3 FRFlowGlint(Light light, float3 n, float3 V, float alpha)
            {
                half att = light.distanceAttenuation * light.shadowAttenuation;
                if (att <= 0.0h) return 0.0;
                float3 L = light.direction;
                float nl = saturate(dot(n, L));
                if (nl <= 0.0) return 0.0;
                float3 h = normalize(L + V);
                float nh = saturate(dot(n, h));
                float a2 = alpha * alpha;
                float d = nh * nh * (a2 - 1.0) + 1.0;
                float D = a2 / (PI * d * d + 1e-6);
                float F = FRFlowFresnel(saturate(dot(h, V)));
                float spec = min(D * F * nl * 0.25 / max(saturate(dot(n, V)), 0.2), _GlintCap);
                return light.color * (att * spec * _LampGain);
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 P = i.positionWS;
                float3 toEye = _WorldSpaceCameraPos.xyz - P;
                float3 V = normalize(toEye);
                // a thin sheet is seen from either side: its normal faces the eye (the winding's front face flips with a flipped
                // render target on Metal: job g2b_3 drew the cascade's film as a mirror of the pool behind the viewer)
                float3 nG = normalize(i.normalWS);
                if (dot(nG, V) < 0.0) nG = -nG;
                float3 tU = normalize(i.tangentWS.xyz);                 // across the flow (u)
                float3 tV = normalize(cross(nG, tU));                   // in the sheet, square to u (the flow's line; its sign is the UVs')
                half coverage = i.color.r, foam = i.color.g, mode = i.color.b, speedScale = max(i.color.a, 0.05h);
                half steady = mode > 0.25h ? 1.0h : 0.0h;          // the trough's film and the wall film run at their own speed
                half film = mode > 0.75h ? 1.0h : 0.0h;            // the wall film: rivulets and the joints
                float2 uv = i.uv;
                float t = _Time.y;

                // the water's speed here: v0 at the lip, then free fall along the sheet (a film on a wall or in the trough runs at
                // its own steady speed: the vertex's speed scale x v0)
                float v0 = _FlowParams.x * speedScale;
                float speed = lerp(sqrt(v0 * v0 + 2.0 * _FlowParams.y * max(uv.y, 0.0)), v0 * 2.0, steady);
                float stretch = lerp(_FlowParams.z, 3.5, steady);
                float2 s = FRFlowSlope(uv, speed, stretch, t) * _FlowParams.w;
                // the film on a tiled wall: every horizontal joint trips it into a fine bright line (a ridge across the flow)
                float jointLine = 0.0;
                if (film > 0.5 && _FilmParams.z > 0.0)
                {
                    float jy = frac(P.y / _FilmParams.z);
                    float dj = min(jy, 1.0 - jy) * _FilmParams.z;
                    jointLine = exp(-dj * dj / (0.004 * 0.004)) * _FilmParams.w;
                    s.y += jointLine * 0.25;
                }
                float3 n = normalize(nG - tU * s.x - tV * s.y);
                float nv = max(dot(n, V), 1e-3);
                float F = FRFlowFresnel(nv);

                // the film's rivulets: a 1-D pattern across the wall, drifting slowly, sharper where the film is thin
                float riv = 1.0;
                if (film > 0.5)
                {
                    float x = uv.x / max(_FilmParams.y, 0.005);
                    float wob = FRFlowNoise(uv.y * 1.3 + t * 0.15 + x * 0.07) * 0.8;
                    float r = FRFlowNoise(x + wob);
                    riv = smoothstep(1.0 - _FilmParams.x - 0.08, 1.0 - _FilmParams.x + 0.08, r);
                    riv = max(riv, jointLine * 0.5);
                }

                // foam: aerated water at the impact and the spill, broken by the flow's own pattern
                float foamTex = saturate(0.55 + 0.9 * (s.x * 7.0 + s.y * 5.0) + 0.35 * FRFlowNoise(uv.x * 37.0 + uv.y * 11.0 - t * speed * 3.0));
                half foamAmt = (half)saturate(foam * (0.65 + 0.7 * foamTex));

                // light: the environment (the renderer's probe, box projected) and the lamps as capped glints
                float alpha = max(_SheetParams.w, 0.06);
                float perceptual = sqrt(alpha);
                float3 R = reflect(-V, n);
                half3 env = GlossyEnvironmentReflection(R, P, (half)perceptual, 1.0h, GetNormalizedScreenSpaceUV(i.positionCS));
                half3 glints = 0.0;
                half3 diffuse = SampleSH(n);
                InputData inputData = (InputData)0;
                inputData.positionWS = P;
                inputData.normalWS = n;
                inputData.viewDirectionWS = V;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                half4 shadowMask = half4(1, 1, 1, 1);
                Light mainLight = GetMainLight();
                glints += FRFlowGlint(mainLight, n, V, alpha);
                diffuse += mainLight.color * (mainLight.distanceAttenuation * saturate(dot(n, mainLight.direction) * 0.5 + 0.5));
            #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
            #if USE_CLUSTER_LIGHT_LOOP
                [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                    Light light = GetAdditionalLight(lightIndex, P);
                    glints += FRFlowGlint(light, n, V, alpha);
                    diffuse += light.color * (light.distanceAttenuation * saturate(dot(n, light.direction) * 0.5 + 0.5));
                }
            #endif
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light = GetAdditionalLight(lightIndex, P);
                    glints += FRFlowGlint(light, n, V, alpha);
                    diffuse += light.color * (light.distanceAttenuation * saturate(dot(n, light.direction) * 0.5 + 0.5));
                LIGHT_LOOP_END
            #endif

                // alpha: a clear sheet face-on lets most through, more is reflected at grazing; foam is near opaque
                half aClear = (half)lerp(_SheetParams.x, _SheetParams.y, F);
                half a = lerp(aClear, (half)_SheetParams.z, foamAmt);
                a *= (half)saturate(coverage) * (half)riv;
                // the body: a clear sheet's faint aqua scatter, foam white, both lit by the room
                // (a clear sheet scatters little of its own: 6 % of the room's light; foam all of it)
                half3 body = lerp((half3)_FlowColor.rgb * 0.06h, (half3)_FoamColor.rgb, foamAmt) * diffuse;
                half3 spec = (env * (half)F + glints) * (1.0h - foamAmt * 0.6h) * (half)saturate(coverage) * (half)riv;
                half3 color = body * a + spec;
                // fog on premultiplied colour: the fog's own colour weighted by this surface's alpha (MixFog alone would lay the
                // fog colour over the whole quad, the uncovered parts too: a veil over the cascade, job g2b_2)
                color = MixFogColor(color, (half3)unity_FogColor.rgb * a, i.fogCoord);
                return half4(color, a);
            }
            ENDHLSL
        }
    }
}
