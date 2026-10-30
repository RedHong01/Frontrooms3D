// V3 HeldItem (D1.6, research/interactables/d16/10_design.md §7): the depth punch. Drawn by FrontRoomsHeldItem's pass
// as the override material of the HeldItem-layer renderers: no colour, depth = the far plane (reversed-Z aware), so the
// redraw that follows (ZTest LEqual) puts the held item over everything with its own self-occlusion.
Shader "Hidden/FrontRooms/HeldItemDepthPunch"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Name "DepthPunch"
            ZWrite On
            ZTest Always
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return o;
            }

            float Frag(Varyings input) : SV_Depth
            {
                return UNITY_RAW_FAR_CLIP_VALUE;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
