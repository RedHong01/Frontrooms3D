// Run! B, card 4 (research/level_space/71_run_room.md s.7.2 D1, s.7.5 S1): the corridor's light under a shut Run door, on the
// approach side. One additive strip draped 0.4 mm over the Steel DA frame's saddle (its outer flat and bevel) and the floor in
// front of it (FrontRoomsRunDoorGap builds it). The profile along the strip is in the vertex alpha (the slot's line at the leaf,
// the flat, the brighter bevel, the floor's short tail, soft ends at the jambs); _Color is the added radiance at the slot, which
// FrontRoomsRunDoorGap reads every frame from the rig's own Lights at the corridor floor inside the door (linear, HDR), already
// faded by the leaf's opening. Lights cannot draw a 3 mm slot, as they cannot light the deck a lamp hangs from (RunSoffitHalo):
// zero lights, one draw, no texture. Every property is set by its owner (batch imports do not apply Property defaults).
Shader "FrontRooms/RunB/DoorGap"
{
    Properties
    {
        _Color ("Added radiance at the slot (linear)", Color) = (0, 0, 0, 1)
        _Density ("Level", Range(0, 4)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-10" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend One One
        Cull Back
        ZWrite Off
        ZTest LEqual
        Offset -1, -1

        Pass
        {
            Name "RunDoorGap"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Density;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; half profile : TEXCOORD0; half fog : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.profile = v.color.a;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half3 color = _Color.rgb * (i.profile * _Density);
                color = MixFogColor(color, half3(0, 0, 0), i.fog);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
