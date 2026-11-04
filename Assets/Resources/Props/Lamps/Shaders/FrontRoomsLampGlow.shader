// FrontRooms zone lamps (line zone_lamps, research/zone_lamps; Red 2026-10-31 picked O-C for the Office): the lit-ceiling
// patch of an indirect fixture, first the Office's lensed-indirect pendant (FrontRoomsLampKit.Pendant, ZL_PEND "Glow").
// Additive, no texture: an analytic soft patch on the quad's uv, driven only by _EmissionColor, so the map writes it the
// lamp's own property block (MapWorld.TickFixtures / WriteEmission, beside the lens): it flickers, dims, dies, dips on the
// warning bursts and goes out in the blackout with its lamp. Faded to black in fog (additive). Transparent queue, no depth
// write: GlassRT does not trace it (a renderer whose first material is transparent), the lens beside it is traced as before.
// shape = exp(-d^2) (1 - hot) + hot exp(-6 d^2), d = (uv - centre) / sigma; edges faded per axis; optional dashes along v.
Shader "FrontRooms/LampGlow"
{
    Properties
    {
        [HDR] _EmissionColor ("Lamp drive (gamma as passed)", Color) = (1, 1, 1, 1)
        _Gain ("Peak added radiance per unit drive (linear)", Float) = 0.3
        _Shape ("centre u, centre v, sigma u, sigma v", Vector) = (0.5, 0.5, 0.3, 0.3)
        _Hot ("hot-spot share", Range(0, 1)) = 0.3
        _Edge ("fade u, fade v (0 off, 1 on), dash count, dash depth", Vector) = (1, 1, 0, 0)
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-10" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend One One
        Cull Back
        ZWrite Off
        ZTest LEqual
        Pass
        {
            Name "FrontRoomsLampGlow"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _EmissionColor;
                float _Gain;
                float4 _Shape;
                float _Hot;
                float4 _Edge;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half fog : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                float2 d = (i.uv - _Shape.xy) / max(_Shape.zw, 1e-3);
                float d2 = dot(d, d);
                float s = exp(-d2) * (1.0 - _Hot) + _Hot * exp(-6.0 * d2);
                float fu = lerp(1.0, smoothstep(0.0, 0.10, min(i.uv.x, 1.0 - i.uv.x)), _Edge.x);
                float fv = lerp(1.0, smoothstep(0.0, 0.10, min(i.uv.y, 1.0 - i.uv.y)), _Edge.y);
                float dash = lerp(1.0, 0.5 + 0.5 * cos(6.2831853 * i.uv.y * _Edge.z), _Edge.w);
                half3 c = _EmissionColor.rgb * (half)(_Gain * s * fu * fv * dash);
                c = MixFogColor(c, half3(0, 0, 0), i.fog);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
