// FrontRooms HUD key / crosshair vector shader (visual-owned, FrontRoomsHudVectorGraphic).
// One UI quad draws the crosshair dot, the zone padlock (L3.D, D.3, swing, F.1), the broken door (B.4) or
// the look-eye (E.1) from analytic distance fields, so the iris can lerp corner radii, the shackle keeps a
// constant 2.6 px stroke edge-on, the frame can draw to a partial length, the eye's pupil is clipped to the
// almond between its lids, and the padlock's and the eye's keylines are each ONE 85 % group.
// Coverage = clamp(0.5 - d * pxPerUnit): a box filter, no derivatives (same on Metal, D3D11, GLES3/WebGL2).
// Compositing: premultiplied, linear (the project's colour space), painter's order, times the vertex
// alpha (CanvasGroup). Units: 1080p design px, Unity y up, origin = the crosshair point (uv0).
Shader "FrontRooms/UI/HudVector"
{
    Properties
    {
        [PerRendererData] _MainTex ("Number atlas (alpha)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        // The constants FrontRoomsKeyHud writes (linear rgb of the sRGB masters; dot radii; the B.4 hinge, Unity y up),
        // declared so that a material rebuilt from its shader keeps them (capture stage 2026-10-22, VL304).
        [HideInInspector] _HV_Paper ("Paper F4F1E8 (linear)", Vector) = (0.9046612, 0.8796224, 0.8069523, 1)
        [HideInInspector] _HV_Ink ("Keyline 141414 (linear)", Vector) = (0.0069954, 0.0069954, 0.0069954, 1)
        [HideInInspector] _HV_NumInk ("Number ink 1E1D1A (linear)", Vector) = (0.0129830, 0.0122865, 0.0103298, 1)
        [HideInInspector] _HV_DotCol ("Dot FFFFFF (linear)", Vector) = (1, 1, 1, 1)
        [HideInInspector] _HV_Dot ("Dot radius, keyline radius", Vector) = (8, 9, 0, 0)
        [HideInInspector] _HV_Hinge ("B.4 hinge (Unity y up)", Vector) = (-5.5, 10.5, 0, 0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="False" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 texcoord : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex : SV_POSITION; float4 color : COLOR; float2 p : TEXCOORD0; float4 worldPosition : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };

            sampler2D _MainTex;
            float4 _Color;
            float4 _ClipRect;

            // x px per design unit, y keyline alpha, z lock alpha (0 = no lock), w dot alpha
            float4 _HV_A;
            // x door on (0/1), y point count, z (lock) shackle clip y, w unused
            float4 _HV_B;
            // lock transform: x, y offset (units), cos, sin of the Unity z-rotation
            float4 _HV_LockT;
            // lock pivot x, y, scale, unused
            float4 _HV_LockP;
            float4 _HV_Body;     // w, h, r
            float4 _HV_Ins;      // w, h, r
            float4 _HV_Zone;     // linear rgb
            float4 _HV_Paper;    // linear rgb
            float4 _HV_Ink;      // linear rgb (keyline / knob, #141414)
            float4 _HV_NumInk;   // linear rgb (#1E1D1A)
            float4 _HV_DotCol;   // linear rgb (#FFFFFF)
            float4 _HV_Dot;      // dot radius, keyline radius, unused, unused
            float4 _HV_DigitRect;// lock coords x0, y0, x1, y1 of the number cell
            float4 _HV_DigitUV;  // u0, v0, u1, v1
            float4 _HV_Digit;    // reveal x (draw where x < reveal), digits on (0/1)
            float4 _HV_Stroke;   // paper half width, keyline half width (shackle 1.3 / 2.3, frame 1 / 2)
            float4 _HV_Leaf;     // w, h, r, centre y (Unity)
            float4 _HV_LeafT;    // translate y (Unity), cos, sin of the Unity z-rotation round the hinge
            float4 _HV_Hinge;    // hinge x, y (Unity)
            float4 _HV_Knob;     // x, y, r
            float4 _HV_C;        // x door alpha, y eye alpha (op), z eye lid alpha (lidOp), w pupil radius
            float4 _HV_E;        // x eye half-width, y pupil centre y (Unity), z points per lid, w unused
            float4 _HV_Pts[64];  // polyline (shackle or frame) or the eye's two lids ([0, m) upper, [m, 2m) lower), xy

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.p = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            float sdRRect(float2 p, float2 hs, float r)
            {
                if (hs.x <= 0.0 || hs.y <= 0.0) return 1e4;
                r = clamp(r, 0.0, min(hs.x, hs.y));
                float2 q = abs(p) - hs + r;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            // Open polyline, round joins, butt caps at both ends. Returns the half-capsule pieces so two
            // stroke widths share one loop: x = min interior capsule distance, (y, z) first segment
            // (capsule, butt plane), (w, v) last segment (capsule, butt plane).
            void polyline(float2 p, int n, out float dMid, out float d0, out float pl0, out float d1, out float pl1)
            {
                dMid = 1e4; d0 = 1e4; pl0 = 1e4; d1 = 1e4; pl1 = 1e4;
                [loop] for (int i = 0; i < 63; i++)
                {
                    if (i >= n - 1) break;
                    float2 a = _HV_Pts[i].xy;
                    float2 b = _HV_Pts[i + 1].xy;
                    float2 ab = b - a;
                    float L2 = max(dot(ab, ab), 1e-12);
                    float t = saturate(dot(p - a, ab) / L2);
                    float d = length(p - a - ab * t);
                    float2 dir = ab * rsqrt(L2);
                    bool first = i == 0;
                    bool last = i == n - 2;
                    if (first) { d0 = d; pl0 = -dot(p - a, dir); }
                    if (last) { d1 = d; pl1 = dot(p - b, dir); }
                    if (!first && !last) dMid = min(dMid, d);
                }
            }
            float strokeSd(float dMid, float d0, float pl0, float d1, float pl1, float hw)
            {
                return min(dMid - hw, min(max(d0 - hw, pl0), max(d1 - hw, pl1)));
            }

            // Distance to an open polyline's centreline (round caps and joins), points [s, s + m).
            float polyRound(float2 p, int s, int m)
            {
                float d = 1e4;
                [loop] for (int i = 0; i < 63; i++)
                {
                    if (i >= m - 1) break;
                    float2 a = _HV_Pts[s + i].xy;
                    float2 b = _HV_Pts[s + i + 1].xy;
                    float2 ab = b - a;
                    float t = saturate(dot(p - a, ab) / max(dot(ab, ab), 1e-12));
                    d = min(d, length(p - a - ab * t));
                }
                return d;
            }
            // A lid's y at x (each lid is monotonic in x); 1e4 where the lid does not reach.
            float polyYAt(float x, int s, int m)
            {
                float y = 1e4;
                [loop] for (int i = 0; i < 63; i++)
                {
                    if (i >= m - 1) break;
                    float2 a = _HV_Pts[s + i].xy;
                    float2 b = _HV_Pts[s + i + 1].xy;
                    if (x >= a.x && x <= b.x) y = lerp(a.y, b.y, (x - a.x) / max(b.x - a.x, 1e-6));
                }
                return y;
            }

            float cov(float d, float px) { return saturate(0.5 - d * px); }
            void over(inout float4 acc, float3 c, float a) { acc.rgb = c * a + acc.rgb * (1.0 - a); acc.a = a + acc.a * (1.0 - a); }

            float4 frag(v2f i) : SV_Target
            {
                float px = _HV_A.x;
                float kla = _HV_A.y;
                float2 p = i.p;
                float4 acc = float4(0, 0, 0, 0);

                // ---- crosshair dot (lab kdot + dot: separate opacities, under the lock)
                float dotA = _HV_A.w;
                if (dotA > 0.0)
                {
                    float r = length(p);
                    over(acc, _HV_Ink.rgb, cov(r - _HV_Dot.y, px) * kla * dotA);
                    over(acc, _HV_DotCol.rgb, cov(r - _HV_Dot.x, px) * dotA);
                }

                int n = (int)(_HV_B.y + 0.5);

                // ---- padlock: one group, composited at the lock alpha
                float lockA = _HV_A.z;
                if (lockA > 0.0)
                {
                    float sc = max(_HV_LockP.z, 1e-4);
                    float2 v = p - _HV_LockT.xy - _HV_LockP.xy;
                    // inverse of the Unity z-rotation (cos, sin)
                    float2 q = float2(_HV_LockT.z * v.x + _HV_LockT.w * v.y, -_HV_LockT.w * v.x + _HV_LockT.z * v.y);
                    q = (q + _HV_LockP.xy) / sc;
                    float lpx = px * sc;
                    float4 g = float4(0, 0, 0, 0);
                    float2 bh = _HV_Body.xy * 0.5;
                    float dMid, d0, pl0, d1, pl1;
                    polyline(q, n, dMid, d0, pl0, d1, pl1);
                    float shClip = _HV_B.z - q.y;                  // shackle clip: nothing below it
                    float dShK = max(strokeSd(dMid, d0, pl0, d1, pl1, _HV_Stroke.y), shClip);
                    float dShP = max(strokeSd(dMid, d0, pl0, d1, pl1, _HV_Stroke.x), shClip);
                    float dRimK = sdRRect(q, bh + 2.0, _HV_Body.z + 2.0);
                    // 1 keyline (ink): shackle keyline UNION rim keyline, one 85 % layer
                    over(g, _HV_Ink.rgb, cov(min(dShK, dRimK), lpx) * kla);
                    // 2 shackle (paper)
                    over(g, _HV_Paper.rgb, cov(dShP, lpx));
                    // 3 rim (paper) = body + 2, r + 1
                    over(g, _HV_Paper.rgb, cov(sdRRect(q, bh + 1.0, _HV_Body.z + 1.0), lpx));
                    // 4 body (zone colour)
                    over(g, _HV_Zone.rgb, cov(sdRRect(q, bh, _HV_Body.z), lpx));
                    // 5 insert (paper)
                    over(g, _HV_Paper.rgb, cov(sdRRect(q, _HV_Ins.xy * 0.5, _HV_Ins.z), lpx));
                    // 6 number (ink), typed digit by digit (reveal x)
                    if (_HV_Digit.y > 0.5)
                    {
                        float2 r0 = _HV_DigitRect.xy, r1 = _HV_DigitRect.zw;
                        if (q.x >= r0.x && q.x <= r1.x && q.y >= r0.y && q.y <= r1.y)
                        {
                            float2 f = (q - r0) / (r1 - r0);
                            float2 uv = lerp(_HV_DigitUV.xy, _HV_DigitUV.zw, f);
                            float a = tex2Dlod(_MainTex, float4(uv, 0, 0)).a;
                            a *= saturate(0.5 - (q.x - _HV_Digit.x) * lpx);
                            over(g, _HV_NumInk.rgb, a);
                        }
                    }
                    acc = g * lockA + acc * (1.0 - g.a * lockA);
                }

                // ---- broken door B.4: frame keyline, frame, leaf keyline, leaf, knob (separate opacities), as one
                // group at the door alpha (1 except a Reduce Motion cross-fade)
                if (_HV_B.x > 0.5 && _HV_C.x > 0.0)
                {
                    float4 dg = float4(0, 0, 0, 0);
                    if (n >= 2)
                    {
                        float dMid, d0, pl0, d1, pl1;
                        polyline(p, n, dMid, d0, pl0, d1, pl1);
                        over(dg, _HV_Ink.rgb, cov(strokeSd(dMid, d0, pl0, d1, pl1, _HV_Stroke.y), px) * kla);
                        over(dg, _HV_Paper.rgb, cov(strokeSd(dMid, d0, pl0, d1, pl1, _HV_Stroke.x), px));
                    }
                    // the leaf group: translate (0, ty), then rotate round the hinge (Unity sense)
                    float2 v = p - float2(0.0, _HV_LeafT.x) - _HV_Hinge.xy;
                    float2 q = float2(_HV_LeafT.y * v.x + _HV_LeafT.z * v.y, -_HV_LeafT.z * v.x + _HV_LeafT.y * v.y) + _HV_Hinge.xy;
                    float2 lc = q - float2(0.0, _HV_Leaf.w);
                    float2 lh = _HV_Leaf.xy * 0.5;
                    // lab doorLeaf: keyline rx = min(r + KL, w/2 + KL, h/2 + KL); paper rx = min(r, w/2, h/2)
                    over(dg, _HV_Ink.rgb, cov(sdRRect(lc, lh + 1.0, _HV_Leaf.z + 1.0), px) * kla);
                    over(dg, _HV_Paper.rgb, cov(sdRRect(lc, lh, _HV_Leaf.z), px));
                    if (_HV_Knob.z > 0.0) over(dg, _HV_Ink.rgb, cov(length(q - _HV_Knob.xy) - _HV_Knob.z, px));
                    acc = dg * _HV_C.x + acc * (1.0 - dg.a * _HV_C.x);
                }

                // ---- the look-eye E.1 (lab eyeMake / eyeDraw): the keyline group (both lid keylines at the lid
                // alpha, then the pupil keyline clipped to the almond) at kl * op, the white pupil clipped to the
                // almond at op, then the two paper lids at lidOp * op. Lids: stroke 2 (keyline 2 + 2 KL), round caps.
                float eyeA = _HV_C.y;
                if (eyeA > 0.0)
                {
                    int m = (int)(_HV_E.z + 0.5);
                    float lidA = _HV_C.z;
                    float pr = _HV_C.w;
                    float dU = polyRound(p, 0, m);
                    float dL = polyRound(p, m, m);
                    float yU = polyYAt(p.x, 0, m);
                    float yL = polyYAt(p.x, m, m);
                    bool inside = yU < 1e3 && yL < 1e3 && p.y < yU && p.y > yL;   // Unity y up: the upper lid is above
                    float dAl = inside ? -min(dU, dL) : min(dU, dL);              // the almond's signed distance
                    float rP = length(p - float2(0.0, _HV_E.y));
                    float4 eg = float4(0, 0, 0, 0);
                    over(eg, _HV_Ink.rgb, cov(dU - 2.0, px) * lidA);
                    over(eg, _HV_Ink.rgb, cov(dL - 2.0, px) * lidA);
                    if (pr > 0.0) over(eg, _HV_Ink.rgb, cov(max(rP - (pr + 1.0), dAl), px));
                    float ga = kla * eyeA;
                    acc = eg * ga + acc * (1.0 - eg.a * ga);
                    if (pr > 0.0) over(acc, _HV_DotCol.rgb, cov(max(rP - pr, dAl), px) * eyeA);
                    over(acc, _HV_Paper.rgb, cov(dU - 1.0, px) * lidA * eyeA);
                    over(acc, _HV_Paper.rgb, cov(dL - 1.0, px) * lidA * eyeA);
                }

                acc *= i.color.a;
                #ifdef UNITY_UI_CLIP_RECT
                acc *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(acc.a - 0.001);
                #endif
                return acc;
            }
            ENDCG
        }
    }
}
