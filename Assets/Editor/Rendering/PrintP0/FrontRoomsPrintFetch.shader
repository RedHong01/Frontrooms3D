// Test only (FrontRoomsPrintQ1bTest): reads one mip of a print texture or array slice
// texel for texel with Load (explicit mip, no sampler, no filtering, no bias), so compressed
// prints (BC5) can be read back and compared bit for bit. A sampler-based fetch is not used:
// the inline point sampler ignored the explicit LOD on Metal and returned mip 0 for every
// level (found 2026-10-04). FrontRoomsPrintQ1bTest self-checks this read against the exact
// R8G8 reference at every level. Never used by a material.
Shader "Hidden/FrontRooms/PrintFetch"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 4.5
            #include "UnityCG.cginc"

            Texture2D _FetchTex;
            Texture2DArray _FetchArr;
            float _FetchSlice, _FetchMip, _FetchUseArray;
            float4 _FetchSize;   // xy = width, height of the mip being read

            float4 frag(v2f_img i) : SV_Target
            {
                int2 size = int2(_FetchSize.xy);
                int2 p = clamp(int2(floor(i.uv * _FetchSize.xy)), int2(0, 0), size - 1);
                int mip = (int)(_FetchMip + 0.5);
                if (_FetchUseArray > 0.5)
                    return _FetchArr.Load(int4(p, (int)(_FetchSlice + 0.5), mip));
                return _FetchTex.Load(int3(p, mip));
            }
            ENDCG
        }
    }
}
