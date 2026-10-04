// Glass prepass for the desktop ray-traced glass reflections (G14; design 10 §1.3), used as an OVERRIDE shader for
// receivers whose material has no FRGlassRTPrepass pass of its own (FrontRooms/Glass has one: contract G-1).
// Drawn by FrontRoomsMetalGlassRTRendererFeature into two 1x targets before the transparents:
//   SV_Target0  R32F     linear eye depth of the front glass surface (0 = no glass)
//   SV_Target1  RGBA16F  world geometric normal (xyz), perceptual smoothness (w; the material's _Smoothness)
// A transient 1x D32 (ZWrite On, ZTest LEqual) keeps the front-most receiving layer, and the fragment is clipped when
// it lies behind the opaque scene (_FRGlassRTSceneDepth = _CameraDepthTexture), so the trace starts only from glass
// the player actually sees. On a closed thin box (the map's pane cube) only the broad face toward the camera is
// drawn (Cull Back) and the thin edge faces are clipped, so the 30 mm cube is never used as thickness (R20).
// Never compiled for WebGL players: FrontRoomsGlassRTWebGLStripper removes it.
Shader "Hidden/FrontRooms/GlassRTPrepass"
{
    Properties
    {
        _Smoothness ("Smoothness", Range(0, 1)) = 0.96
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
        Pass
        {
            Name "FRGlassRTPrepass"
            ZWrite On
            ZTest LEqual
            Cull Back
            Blend Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _Smoothness;
            CBUFFER_END

            TEXTURE2D_X_FLOAT(_FRGlassRTSceneDepth);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float  edge       : TEXCOORD1;
                float  eye        : TEXCOORD2;   // linear eye depth (view-space -z), the same quantity the glass hook compares
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.eye = -TransformWorldToView(p.positionWS).z;
                // thin axis of the scaled box = pane normal; faces along the other two axes are edge faces
                float4x4 m = GetObjectToWorldMatrix();
                float3 scale = float3(length(m._m00_m10_m20), length(m._m01_m11_m21), length(m._m02_m12_m22));
                float3 thin = scale.x <= scale.y && scale.x <= scale.z ? float3(1, 0, 0) : (scale.y <= scale.z ? float3(0, 1, 0) : float3(0, 0, 1));
                float3 an = abs(input.normalOS);
                float faceAxisIsThin = dot(step(max(an.x, max(an.y, an.z)) - 1e-3, an), thin);
                o.edge = 1.0 - saturate(faceAxisIsThin);
                return o;
            }

            void Frag(Varyings input, out float outDepth : SV_Target0, out half4 outNormal : SV_Target1)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                clip(0.5 - input.edge);
                float eye = input.eye;
                float sceneRaw = LOAD_TEXTURE2D_X(_FRGlassRTSceneDepth, uint2(input.positionCS.xy)).r;
                float sceneEye = LinearEyeDepth(sceneRaw, _ZBufferParams);
                clip(sceneEye + 0.01 + 0.002 * eye - eye);   // behind an opaque occluder: not visible, not traced
                outDepth = eye;
                outNormal = half4(normalize(input.normalWS), _Smoothness);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
