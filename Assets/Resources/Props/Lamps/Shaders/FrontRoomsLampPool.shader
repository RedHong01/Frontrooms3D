// FrontRooms far lamp pools (structures_assets 30_plan.md P7, line visual (game visual), 2026-11-06; FrontRoomsStructuresLamps.BuildPool):
// the floor patch a lamp keeps after the light budget has put its light out (20_research.md C.3.1). A variant of
// FrontRooms/LampGlow for floors: the same analytic patch on the quad's uv (no texture, driven only by _EmissionColor through the
// pool's own property block), applied in two parts in one pass (Blend One SrcAlpha: out = add + dst x (1 + lift)):
//   lift: the floor's own shade (its albedo under whatever light it still has) raised by _Gain x the patch, so the carpet or tile
//         and its pattern stay under the pool, brighter, as a lamp's light lifts a lit floor;
//   add:  the lamp's own floor radiance at the centre (_Add x the drive's colour x the patch), so a floor that nothing else lights
//         (a far bay past the budget with no view lamp) still gets its pool. The two are set so a band that keeps the zone's
//         ambient and the neighbours' tails (SP3's spine, measured .12 against .25 lit) and a dark far bay both land near their
//         lit level. The lift needs a float target (the game's HDR buffer; on an 8-bit target alpha clamps and only add remains).
// Clipped to the room's rectangle (_Clip, uv), faded round its rim and to nothing in fog. Transparent queue, no depth write, a
// polygon offset over its 2 mm lift: GlassRT and the planar water do not see it (they see today's darker far floor).
// shape = exp(-d^2) (1 - hot) + hot exp(-6 d^2), d = (uv - 0.5) / sigma; rim fade from _Shape.z to the quad's edge.
Shader "FrontRooms/LampPool"
{
    Properties
    {
        [HDR] _EmissionColor ("Pool drive (linear colour x share)", Color) = (0, 0, 0, 1)
        _Gain ("Lift at the centre per unit drive (x the floor's own shade)", Float) = 0.25
        _Add ("The lamp's own floor radiance at the centre per unit drive (linear)", Float) = 0.10
        _Shape ("sigma (uv), hot share, rim fade start (uv radius), unused", Vector) = (0.40, 0.20, 0.45, 0)
        _Clip ("Clip rectangle in uv (u0, v0, u1, v1)", Vector) = (0, 0, 1, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-20" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend One SrcAlpha
        Cull Back
        ZWrite Off
        ZTest LEqual
        Offset -1, -1
        Pass
        {
            Name "FrontRoomsLampPool"
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
                float _Add;
                float4 _Shape;
                float4 _Clip;
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
                float2 c = i.uv - 0.5;
                float2 d = c / max(_Shape.x, 1e-3);
                float d2 = dot(d, d);
                float s = exp(-d2) * (1.0 - _Shape.y) + _Shape.y * exp(-6.0 * d2);
                float rim = 1.0 - smoothstep(_Shape.z, 0.5, length(c));
                float inside = step(_Clip.x, i.uv.x) * step(i.uv.x, _Clip.z) * step(_Clip.y, i.uv.y) * step(i.uv.y, _Clip.w);
                // fog: the pool fades out with distance in fog (the lift to 1, the add to 0); MixFogColor of white over black is the
                // visibility in every fog variant (1 with fog off)
                half vis = MixFogColor(half3(1, 1, 1), half3(0, 0, 0), i.fog).r;
                float f = s * rim * inside * vis;
                half drive = max(_EmissionColor.r, max(_EmissionColor.g, _EmissionColor.b));
                half3 add = _EmissionColor.rgb * (half)(_Add * f);
                half lift = (half)(_Gain * f) * drive;
                return half4(add, 1.0h + lift);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
