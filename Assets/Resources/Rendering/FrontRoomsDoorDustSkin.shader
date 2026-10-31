// FrontRooms/DoorDustSkin -- the floor's dust skin under a broken door (research/door_break/54_dust_grit.md 4.1, 4.5),
// production P1 2026-10-30. One quad per damaged door, 1.5 mm above the floor (Offset -1, -1), after the opaques. Its R8
// density (16 mm texels) is what the landed grit built: carpet absorbed grit under 1 mm into its loops (the skin only
// lightens the loop tops: a 0.5..1.0 pile mask from the floor's own albedo luminance, sampled in world metres at the
// carpet's tiling); a hard floor keeps a scatter (a grain-noise detail where the density is over 0.3). It persists.
// Colour = the fines' albedo (the core palette x 1.15), alpha = 0.35 x density x pile; lit by SH and the cluster lights.
Shader "FrontRooms/DoorDustSkin"
{
    Properties
    {
        _SkinTex ("Density (R8)", 2D) = "black" {}
        _PileTex ("Floor albedo (pile mask)", 2D) = "grey" {}
        _PileTile ("Floor tile (m)", Float) = 1
        _FinesColor ("Fines colour (linear)", Color) = (0.42, 0.29, 0.16, 1)
        _Hard ("Hard floor", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-60" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite Off
            ZTest LEqual
            Offset -1, -1
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _SkinTex_ST;
                float _PileTile;
                half4 _FinesColor;
                half _Hard;
            CBUFFER_END
            TEXTURE2D(_SkinTex); SAMPLER(sampler_SkinTex);
            TEXTURE2D(_PileTex); SAMPLER(sampler_PileTex);

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V2F { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; float fog : TEXCOORD2; };

            V2F Vert(Attributes v)
            {
                V2F o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.uv = v.uv;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            float Hash21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }

            half4 Frag(V2F i) : SV_Target
            {
                half d = SAMPLE_TEXTURE2D(_SkinTex, sampler_SkinTex, i.uv).r;
                if (d < 1.0h / 255.0h) discard;
                half3 floorAlb = SAMPLE_TEXTURE2D(_PileTex, sampler_PileTex, i.positionWS.xz / max(_PileTile, 1e-3)).rgb;
                half lum = dot(floorAlb, half3(0.2126h, 0.7152h, 0.0722h));
                half pile = _Hard > 0.5h ? 1.0h : lerp(0.5h, 1.0h, saturate((lum - 0.12h) / 0.3h));
                half grain = 1.0h;
                if (_Hard > 0.5h && d > 0.3h) grain = 0.6h + 0.8h * (half)Hash21(floor(i.positionWS.xz * 900.0));
                half a = saturate(0.35h * d * pile * grain);
                half3 n = half3(0, 1, 0);
                half3 light = SampleSH(n);
                Light mainLight = GetMainLight();
                light += mainLight.color * (mainLight.distanceAttenuation * saturate(dot(n, mainLight.direction)));
            #if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                uint pixelLightCount = GetAdditionalLightsCount();
            #if USE_CLUSTER_LIGHT_LOOP
                [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                    Light dl = GetAdditionalLight(lightIndex, i.positionWS);
                    light += dl.color * (dl.distanceAttenuation * saturate(dot(n, dl.direction)));
                }
            #endif
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light l = GetAdditionalLight(lightIndex, i.positionWS);
                    light += l.color * (l.distanceAttenuation * saturate(dot(n, l.direction)));
                LIGHT_LOOP_END
            #endif
                half3 col = MixFog(_FinesColor.rgb * light, i.fog);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
