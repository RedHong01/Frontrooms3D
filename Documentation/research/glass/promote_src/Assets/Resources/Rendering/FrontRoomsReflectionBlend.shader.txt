// Crossfade of two zone reflection cubemaps into one cube render texture, one
// face and one mip at a time (FrontRoomsZoneReflection drives it). Each mip of
// the sources is already convolved for roughness, so mip N of the result is
// lerp(A mip N, B mip N). Draw one full-face quad per face and mip; the face
// direction comes from the pixel position, no camera matrices are involved.
// WebGL 2: plain GLES3 fragment shader, one sampler per cube.
Shader "Hidden/FrontRooms/ReflectionBlend"
{
    Properties
    {
        _CubeA ("From", Cube) = "black" {}
        _CubeB ("To", Cube) = "black" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "PreviewType" = "Plane" }
        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            TEXTURECUBE(_CubeA); SAMPLER(sampler_CubeA);
            TEXTURECUBE(_CubeB); SAMPLER(sampler_CubeB);
            float4 _CubeA_HDR;
            float4 _CubeB_HDR;
            float _Blend;   // 0 = A, 1 = B
            float _Mip;     // source mip to read
            float _Face;    // CubemapFace 0..5
            float _Size;    // this mip's face size in pixels
            float _FlipY;   // 1 = pixel row 0 is the bottom of the face

            // Unity's cubemap face basis (D3D layout: u right, v down), as in
            // com.unity.render-pipelines.core ShaderLibrary/Sampling/Sampling.hlsl.
            float3 FaceDirection(float2 nvc, float face)
            {
                if (face < 0.5) return float3(1.0, -nvc.y, -nvc.x);
                if (face < 1.5) return float3(-1.0, -nvc.y, nvc.x);
                if (face < 2.5) return float3(nvc.x, 1.0, nvc.y);
                if (face < 3.5) return float3(nvc.x, -1.0, -nvc.y);
                if (face < 4.5) return float3(nvc.x, -nvc.y, 1.0);
                return float3(-nvc.x, -nvc.y, -1.0);
            }

            float3 DecodeHDR(float4 encoded, float4 decode)
            {
                float alpha = max(decode.w * (encoded.a - 1.0) + 1.0, 0.0);
                return (decode.x * PositivePow(alpha, decode.y)) * encoded.rgb;
            }

            struct Attributes { float3 positionOS : POSITION; };

            float4 Vert(Attributes v) : SV_POSITION
            {
                return float4(v.positionOS.xy, 0.5, 1.0);
            }

            float4 Frag(float4 positionCS : SV_POSITION) : SV_Target
            {
                float2 nvc = positionCS.xy / _Size * 2.0 - 1.0;
                nvc.y = _FlipY > 0.5 ? -nvc.y : nvc.y;
                float3 dir = normalize(FaceDirection(nvc, _Face));
                float3 a = DecodeHDR(SAMPLE_TEXTURECUBE_LOD(_CubeA, sampler_CubeA, dir, _Mip), _CubeA_HDR);
                float3 b = DecodeHDR(SAMPLE_TEXTURECUBE_LOD(_CubeB, sampler_CubeB, dir, _Mip), _CubeB_HDR);
                return float4(lerp(a, b, saturate(_Blend)), 1.0);
            }
            ENDHLSL
        }
    }
}
