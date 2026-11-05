// Scene 4 (the structures scene) only: the placards' world text (FrontRoomsStructuresSigns.DepthText; research/level_space/70
// section 1). Unity's GUI/Text Shader pass (the font atlas's alpha, the vertex colour times _Color) with the depth test written
// out (ZTest LEqual): GUI/Text Shader takes its ZTest from unity_GUIZTestMode, and the copy that set it on the material still
// drew every placard line through walls and through its own panel (review SP3-F1, SP4-F4, SP1-F12; sign_blades 11-04: a
// double-sided blade's far face showed mirrored through it). _Cull stays Off (as GUI/Text); no depth write, transparent queue.
// Referenced by name from scene 4's code only, so no build carries it.
Shader "FrontRooms/Structures/Placard Text"
{
    Properties
    {
        _MainTex ("Font Texture", 2D) = "white" {}
        _Color ("Text Color", Color) = (1,1,1,1)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Lighting Off Cull [_Cull] ZTest LEqual ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            uniform float4 _MainTex_ST;
            uniform fixed4 _Color;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = i.color;
                col.a *= tex2D(_MainTex, i.texcoord).a;
                return col;
            }
            ENDCG
        }
    }
}
