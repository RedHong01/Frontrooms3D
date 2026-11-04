// The pool family's water surface (research/bathroom_rooms/70_look_build.md, "Water surface"; 17_water.md 2, 7 items 1
// and 6). Game visual line.
//
// Reflection only: the surface adds what it reflects and takes away, as alpha, the share it reflects (exact dielectric
// Fresnel for n = 1.333, 2 % looking straight down, 34-40 % at 10 m from a standing eye). It has no colour of its own:
// what lies under it (the floor, the basin's walls, their absorption: the Submerged stage) shows through by 1 - F.
//
// The ripple (FrontRoomsWaterRipples.hlsl) gives each pixel the slope it resolves and the slope covariance it cannot
// show; both go into every reflection term, so near water breaks the image into glints and far water smears it into
// streaks along the view (width / length = sin of the sight line's elevation, 17 2.2, W20).
//   planar (desktop High and Ultra, FrontRoomsWaterPlanar): the mirror camera's image, sampled where the reflected ray of
//          the resolved slope meets the room (its box), filtered by the unresolved covariance: the mip from the narrow
//          axis, taps along the long axis. The lamps are in that image (their lenses), so no lamp term is added there.
//   probe  (phones, WebGL, desktop Medium and Low, and every water height past the mirror budget): the renderer's
//          reflection probe (unity_SpecCube0: a local box probe on its own box, else the zone cube, RenderSettings' custom
//          reflection, on the room's box) filtered the same way in direction space; plus the lamps: every visible additional light
//          above the water as a small source, its reflection = Fresnel x radiance x the share of the pixel's slope
//          distribution (resolved mean, unresolved covariance, the source's own size) that points it at the eye.
//   refraction on desktop (_FR_WaterRefract): what lies under the rippled surface (floor, basin walls), refracted from the
//          camera's opaque texture (apparent depth and the wobble), checked against the depth so nothing above the water
//          leaks in; the probe tiers show it straight through.
// Fog: the planar image carries its own (its camera stands at the mirrored eye, so its distances are the whole reflected
// path); the probe and lamp terms get the zone fog and height fog of their path here.
Shader "FrontRooms/Water"
{
    Properties
    {
        _FR_WaterAlpha0 ("Calm roughness (GGX alpha the ripple model does not hold)", Range(0.002, 0.2)) = 0.012
        _FR_WaterBoxMin ("Room box min (world; w = 1 when set)", Vector) = (0, 0, 0, 0)
        _FR_WaterBoxMax ("Room box max (world)", Vector) = (0, 0, 0, 0)
        _FR_WaterProbePos ("Where the probe was captured (world; w = 1 when set)", Vector) = (0, 0, 0, 0)
        _FR_WaterLampSpec ("Lamp reflections from the lights (1 probe tiers; the planar sets 0)", Float) = 1
        _FR_WaterRefract ("Refraction from the opaque texture (desktop High and Ultra)", Float) = 0
        _FR_WaterLampRadius ("Lamp source radius (m)", Float) = 0.4
        _FR_WaterLampGain ("Lamp radiance per unit light intensity", Float) = 0.36
        [HideInInspector] _FR_PlanarTex ("Planar reflection", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Assets/Resources/Rendering/FrontRoomsWaterRipples.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float  _FR_WaterAlpha0;
                float4 _FR_WaterBoxMin;
                float4 _FR_WaterBoxMax;
                float4 _FR_WaterProbePos;
                float  _FR_WaterLampSpec;
                float  _FR_WaterRefract;
                float  _FR_WaterLampRadius;
                float  _FR_WaterLampGain;
            CBUFFER_END

            TEXTURE2D(_FR_PlanarTex);  SAMPLER(sampler_FR_PlanarTex);
            float4x4 _FR_PlanarVP;      // the mirror camera's view-projection of the frame it rendered (GL convention)
            float4   _FR_PlanarParams;  // x 1 = planar on, y highest mip, z texels wide, w texels high
            float4   _FR_HeightFog;     // the zone look's height fog (FrontRoomsZoneLook), as FrontRooms/Surface reads it
            float4   _FR_HeightFogColor;
            float    _FR_WaterDebug;    // tools: 0 off, 1 Fresnel, 2 planar coverage, 3 unresolved slope (deg), 4 reflection only, 5 planar uv, 6 F x reflection, 7 what shows through, 8 reflection with the resolved slope at 0
            float    _FR_WaterOpaque;   // 1 while this camera has the opaque texture (FrontRoomsWaterPlanar asked URP for it)

            #define FR_WATER_IOR 1.333

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                return o;
            }

            // Exact Fresnel reflectance of an air-to-water interface, unpolarised (n = 1.333): 0.0204 at normal incidence.
            float FRWaterFresnel(float c)
            {
                c = saturate(c);
                float g = sqrt(FR_WATER_IOR * FR_WATER_IOR - 1.0 + c * c);
                float a = (g - c) / (g + c);
                float b = (c * (g + c) - 1.0) / (c * (g - c) + 1.0);
                return saturate(0.5 * a * a * (1.0 + b * b));
            }

            float3 FRSlopeNormal(float2 s) { return normalize(float3(-s.x, 1.0, -s.y)); }

            // Where a ray from p along d leaves the room's box (metres); 6 m when no box is set.
            float FRBoxExit(float3 p, float3 d)
            {
                if (_FR_WaterBoxMin.w < 0.5) return 6.0;
                float3 sgn = step(0.0, d);                                          // 1 where d >= 0
                float3 dd = sgn * max(d, 1e-5) + (1.0 - sgn) * min(d, -1e-5);       // never 0
                float3 t = (lerp(_FR_WaterBoxMin.xyz, _FR_WaterBoxMax.xyz, sgn) - p) / dd;
                return clamp(min(t.x, min(t.y, t.z)), 0.05, 200.0);
            }

            float2 FRPlanarUV(float3 positionWS)
            {
                float4 c = mul(_FR_PlanarVP, float4(positionWS, 1.0));
                return c.xy / max(c.w, 1e-5) * 0.5 + 0.5;
            }

            // The unresolved slope covariance (world xz) as its two principal axes: e1 the long one; sd = their std devs.
            void FRCovAxes(float3 cov, out float2 e1, out float2 sd)
            {
                float h = 0.5 * (cov.x + cov.y);
                float disc = sqrt(max(0.25 * (cov.x - cov.y) * (cov.x - cov.y) + cov.z * cov.z, 0.0));
                float l1 = h + disc, l2 = max(h - disc, 0.0);
                float2 e = abs(cov.z) > 1e-12 ? float2(cov.z, l1 - cov.x) : (cov.x >= cov.y ? float2(1.0, 0.0) : float2(0.0, 1.0));
                e1 = e * rsqrt(max(dot(e, e), 1e-30));
                sd = sqrt(max(float2(l1, l2), 0.0));
            }

            // The planar image along the reflected rays of a Gaussian bundle of slopes: mean s, axes e1 / e2 with sd.
            // Each ray meets the room at the box distance of the mean ray; the mip covers the narrow axis, taps the long one.
            // A tap whose ray would point into the water is masked by the waves in front of it (Smith's masking): it drops out
            // and the rest are renormalised, so grazing water never pulls in what lies below the mirror camera's horizon.
            half4 FRPlanarReflection(float3 P, float3 V, float2 s, float2 e1, float2 sd)
            {
                float3 R = reflect(-V, FRSlopeNormal(s));
                float t = FRBoxExit(P, R);
                float2 uv = FRPlanarUV(P + R * t);
                float2 e2 = float2(-e1.y, e1.x);
                // one standard deviation of each axis, as rays and in texture space
                float3 Ra = reflect(-V, FRSlopeNormal(s + e1 * sd.x));
                float3 Rb = reflect(-V, FRSlopeNormal(s + e2 * sd.y));
                float2 ua = FRPlanarUV(P + Ra * t) - uv;
                float2 ub = FRPlanarUV(P + Rb * t) - uv;
                float2 texels = _FR_PlanarParams.zw;
                float la = length(ua * texels), lb = length(ub * texels);
                float3 dR = (la >= lb ? Ra : Rb) - R;
                float lMaj = max(la, lb), lMin = min(la, lb);
                // taps along the long axis over +-2 sd, spaced so the mip of the narrow axis closes the gaps
                int n = (int)clamp(ceil(2.0 * lMaj / max(lMin, 1.0)), 1.0, 12.0);
                float spacing = n > 1 ? 4.0 / (n - 1) : 0.0;
                float lod = clamp(log2(max(max(2.5 * lMin, spacing * lMaj), 1.0)), 0.0, _FR_PlanarParams.y);
                float3 acc = 0.0;
                float wIn = 0.0, wAll = 0.0;
                [loop] for (int i = 0; i < n; i++)
                {
                    float o = n > 1 ? -2.0 + spacing * i : 0.0;
                    float3 r = normalize(R + dR * o);
                    if (r.y <= 0.002) continue;
                    float w = exp(-0.5 * o * o);
                    float2 u = FRPlanarUV(P + r * t);
                    // outside the overscanned image the water falls back to the probe (coverage)
                    float inside = step(0.0, u.x) * step(u.x, 1.0) * step(0.0, u.y) * step(u.y, 1.0);
                    acc += SAMPLE_TEXTURE2D_LOD(_FR_PlanarTex, sampler_FR_PlanarTex, saturate(u), lod).rgb * (w * inside);
                    wIn += w * inside;
                    wAll += w;
                }
                float edge = saturate(min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y)) * 40.0);
                float cover = wAll > 0.0 ? wIn / wAll * edge : 0.0;
                return half4(acc / max(wIn, 1e-5), cover);
            }

            // The probe the water's renderer is given, as FrontRooms/Surface reads it: the project's URP asset keeps probe
            // blending off, so Forward+ hands every renderer its reflection probe as unity_SpecCube0 (per-object probes) and
            // never fills the cluster's probe atlas. A local probe with box projection (the structures scene's hall probes,
            // FrontRoomsStructuresLook.Probe) is projected on its own box from its own position, as URP does; without one the
            // renderer gets the default reflection, the zone cube (FrontRoomsZoneReflection), projected on this body's room
            // box from where that cube was captured. (Until the Package stage the cluster path read _GlossyEnvironmentCubeMap,
            // the same default reflection, so a local probe never reached the water.)
            half3 FRProbeSample(float3 P, float3 R, float t, float mip)
            {
                float3 dir = R;
                if (unity_SpecCube0_ProbePosition.w > 0.0)
                    dir = BoxProjectedCubemapDirection(R, P, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
                else if (_FR_WaterBoxMin.w > 0.5 && _FR_WaterProbePos.w > 0.5)
                    dir = normalize(P + R * t - _FR_WaterProbePos.xyz);
                half4 enc = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, dir, mip);
                return DecodeHDREnvironment(enc, unity_SpecCube0_HDR);
            }

            // The same bundle of reflected rays against the probe: the cube's GGX-filtered mip for the narrow axis (its
            // angular spread), taps along the long one (3 on the phone tier, up to 6 elsewhere).
            half3 FRProbeReflection(float3 P, float3 V, float2 s, float2 e1, float2 sd, float maxTaps, out float pathT)
            {
                float3 R = reflect(-V, FRSlopeNormal(s));
                float t = FRBoxExit(P, R);
                pathT = t;
                float2 e2 = float2(-e1.y, e1.x);
                float3 Ra = reflect(-V, FRSlopeNormal(s + e1 * sd.x));
                float3 Rb = reflect(-V, FRSlopeNormal(s + e2 * sd.y));
                float aa = acos(saturate(dot(R, Ra))), ab = acos(saturate(dot(R, Rb)));
                float3 Rmaj = aa >= ab ? Ra : Rb;
                float aMaj = max(aa, ab), aMin = min(aa, ab);
                // a lobe of angular sd a is about GGX alpha a / sqrt(2) on the reflected direction's half angle
                int n = (int)clamp(ceil(1.5 * aMaj / max(aMin, 0.01)), 1.0, maxTaps);
                float spacing = n > 1 ? 4.0 / (n - 1) : 0.0;
                float aFilt = max(aMin, spacing * aMaj * 0.4);
                float perceptual = sqrt(saturate(aFilt * 0.7071 + _FR_WaterAlpha0));
                float mip = PerceptualRoughnessToMipmapLevel(perceptual);
                float3 dR = Rmaj - R;
                half3 acc = 0.0;
                float wsum = 0.0;
                [loop] for (int i = 0; i < n; i++)
                {
                    float o = n > 1 ? -2.0 + spacing * i : 0.0;
                    float3 r = normalize(R + dR * o);
                    if (r.y <= 0.002 && n > 1) continue;            // masked by the waves in front (as the planar's taps)
                    float w = exp(-0.5 * o * o);
                    acc += FRProbeSample(P, r, t, mip) * w;
                    wsum += w;
                }
                return acc / max(wsum, 1e-5);
            }

            // The light's position (w 0 = directional), indexed as GetAdditionalLight indexes it.
            float4 FRLightPosition(uint loopIndex)
            {
            #if USE_CLUSTER_LIGHT_LOOP
                int idx = (int)loopIndex;
            #else
                int idx = GetPerObjectLightIndex(loopIndex);
            #endif
            #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
                return _AdditionalLightsBuffer[idx].position;
            #else
                return _AdditionalLightsPosition[idx];
            #endif
            }

            // Screen uv of a clip-space position, as the camera's opaque and depth textures are addressed (URP's
            // ComputeScreenPos, which is deprecated in 17).
            float2 FRClipToScreenUV(float4 c)
            {
                float2 ndc = c.xy / max(c.w, 1e-5);
                return float2(ndc.x, ndc.y * _ProjectionParams.x) * 0.5 + 0.5;
            }

            // One lamp's reflection: Fresnel x its radiance x the share of the pixel's slope distribution (Gaussian: the
            // resolved mean s, the unresolved covariance, the calm roughness and the lamp's own size) that points it at the
            // eye. Lamps below the water (wet niches) are skipped: from above, their light comes through, not off, the surface.
            half3 FRLampReflection(Light light, float3 lightPos, float3 P, float3 V, float2 s, float3 cov)
            {
                float3 toL = lightPos - P;
                float d2 = max(dot(toL, toL), 1e-4);
                float d = sqrt(d2);
                float3 L = toL / d;
                if (L.y <= 0.01 || light.distanceAttenuation <= 0.0) return 0.0;
                float3 h = normalize(L + V);
                if (h.y <= 0.05) return 0.0;
                float2 sh = -h.xz / h.y;                                   // the facet slope that sends L to the eye
                // the lamp as a downward lens of radius R: projected solid angle, and its area in slope space
                float omega = PI * _FR_WaterLampRadius * _FR_WaterLampRadius * L.y / d2;
                float hv = max(dot(h, V), 0.05);
                float area = omega / (4.0 * hv * h.y * h.y * h.y);
                float src = area / (2.0 * PI);                             // the source as a Gaussian of equal peak
                float iso = 0.5 * _FR_WaterAlpha0 * _FR_WaterAlpha0 + src;
                float a = cov.x + iso, b = cov.z, c = cov.y + iso;
                float det = max(a * c - b * b, 1e-14);
                float2 dd = sh - s;
                float q = (c * dd.x * dd.x - 2.0 * b * dd.x * dd.y + a * dd.y * dd.y) / det;
                float share = min(area * exp(-0.5 * q) / (2.0 * PI * sqrt(det)), 1.0);
                // the light's colour x intensity, its range window and cone, without the inverse square (a source seen in
                // a mirror keeps its radiance; the gain matches the reflection to the lens as the player sees it)
                half3 radiance = light.color * (light.distanceAttenuation * d2) * _FR_WaterLampGain;
                return radiance * (FRWaterFresnel(hv) * share);
            }

            half3 FRHeightFogPath(half3 color, float3 fromWS, float3 toWS, float extra)
            {
                UNITY_BRANCH
                if (_FR_HeightFog.x <= 0.0) return color;
                float len = length(toWS - fromWS) + extra;
                float k = _FR_HeightFog.z;
                float ya = max(fromWS.y - _FR_HeightFog.y, 0.0);
                float yb = max(toWS.y - _FR_HeightFog.y, 0.0);
                float t = k * (yb - ya);
                float f = abs(t) > 1e-3 ? (1.0 - exp(-t)) / t : 1.0 - 0.5 * t;
                float tau = _FR_HeightFog.x * len * exp(-k * ya) * f;
                return lerp(color, (half3)_FR_HeightFogColor.rgb, (half)saturate(1.0 - exp(-tau)));
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float3 P = i.positionWS;
                float3 toEye = _WorldSpaceCameraPos.xyz - P;
                float eyeDist = length(toEye);
                float3 V = toEye / max(eyeDist, 1e-4);

                FRRipple r = FRWaterRipples(P.xz);              // uniform control flow: it takes screen derivatives
                float2 s = r.slope;
                if (_FR_WaterDebug > 7.5) s = 0.0;               // tools: the unresolved lobe alone (streak geometry check)
                float3 n = FRSlopeNormal(s);
                // a slope steeper than the sight line would face the pixel away: keep the eye on the front of every facet
                // (n.V is proportional to V.y - s.V.xz; scale the slope so it stays above 0.02)
                float sv = dot(s, V.xz);
                if (sv > V.y - 0.02) { s *= saturate((V.y - 0.02) / max(sv, 1e-5)); n = FRSlopeNormal(s); }
                float nv = max(dot(n, V), 1e-3);
                float F = FRWaterFresnel(nv);
                float3 cov = r.cov;
                float2 e1, sd;
                FRCovAxes(cov + float3(0.5, 0.5, 0.0) * _FR_WaterAlpha0 * _FR_WaterAlpha0, e1, sd);

                float2 screenUV = GetNormalizedScreenSpaceUV(i.positionCS);
                half3 refl = 0.0;
                half3 lamps = 0.0;
                float probeWeight = 1.0;
                float pathT = 6.0;

                UNITY_BRANCH
                if (_FR_PlanarParams.x > 0.5)
                {
                    half4 pl = FRPlanarReflection(P, V, s, e1, sd);
                    refl = pl.rgb * pl.a;
                    probeWeight = 1.0 - pl.a;
                }
                float maxTaps = _FR_PlanarParams.x > 0.5 ? 3.0 : (_FR_RippleB.w > 0.5 ? 6.0 : 3.0);
                UNITY_BRANCH
                if (probeWeight > 0.001)
                {
                    half3 pr = FRProbeReflection(P, V, s, e1, sd, maxTaps, pathT);
                    float3 R = reflect(-V, n);
                    float3 H = P + R * pathT;
                    pr = MixFog(pr, ComputeFogFactorZ0ToFar(eyeDist + pathT));
                    pr = FRHeightFogPath(pr, _WorldSpaceCameraPos.xyz, H, eyeDist + pathT - length(H - _WorldSpaceCameraPos.xyz));
                    refl += pr * probeWeight;
                }

                UNITY_BRANCH
                if (_FR_WaterLampSpec > 0.5)
                {
                #if defined(_ADDITIONAL_LIGHTS)
                    InputData inputData = (InputData)0;
                    inputData.positionWS = P;
                    inputData.normalizedScreenSpaceUV = screenUV;
                    uint pixelLightCount = GetAdditionalLightsCount();
                #if USE_CLUSTER_LIGHT_LOOP
                    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                    {
                        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                        Light light = GetAdditionalLight(lightIndex, P);
                        float4 lp = FRLightPosition(lightIndex);
                        if (lp.w > 0.5) lamps += FRLampReflection(light, lp.xyz, P, V, s, cov);
                    }
                #endif
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light light = GetAdditionalLight(lightIndex, P);
                        float4 lp = FRLightPosition(lightIndex);
                        if (lp.w > 0.5) lamps += FRLampReflection(light, lp.xyz, P, V, s, cov);
                    LIGHT_LOOP_END
                #endif
                    lamps = MixFog(lamps, ComputeFogFactorZ0ToFar(eyeDist + pathT));
                }

                half3 color = refl * F + lamps;
                half alpha = F;

                UNITY_BRANCH
                if (_FR_WaterRefract > 0.5 && _FR_WaterOpaque > 0.5)
                {
                    // the floor through the rippled surface: the refracted ray meets the depth of the floor seen straight
                    // through (flat floors exact, walls close), and its image comes from the opaque texture
                    float raw = SampleSceneDepth(screenUV);
                    float3 behind = ComputeWorldSpacePosition(screenUV, raw, UNITY_MATRIX_I_VP);
                    float depth = max(P.y - behind.y, 0.0);
                    float3 T = refract(-V, n, 1.0 / FR_WATER_IOR);
                    float3 X = P + T * (depth / max(-T.y, 0.05));
                    float2 uvFull = FRClipToScreenUV(TransformWorldToHClip(X));
                    // looking steeply down near the frame's edge the refracted ray lands nearer than the straight one, off
                    // the screen: there the image keeps only the ripple's wobble (the rippled minus the flat refraction,
                    // moved onto the floor seen straight through), and the full refraction (apparent depth too) morphs in
                    // over the bottom and top 35 % of the frame (the shift runs along the view, so up and down the screen),
                    // never where its own target is off the screen
                    float3 T0 = refract(-V, float3(0.0, 1.0, 0.0), 1.0 / FR_WATER_IOR);
                    float3 X0 = P + T0 * (depth / max(-T0.y, 0.05));
                    float2 uvWob = FRClipToScreenUV(TransformWorldToHClip(behind + (X - X0)));
                    float edgeFull = min(saturate(min(min(uvFull.x, 1.0 - uvFull.x), min(uvFull.y, 1.0 - uvFull.y)) / 0.04),
                                         smoothstep(0.0, 0.35, min(screenUV.y, 1.0 - screenUV.y)));
                    float2 uvX = clamp(lerp(uvWob, uvFull, edgeFull), 0.002, 0.998);
                    float rawX = SampleSceneDepth(uvX);
                    float3 atX = ComputeWorldSpacePosition(uvX, rawX, UNITY_MATRIX_I_VP);
                    // whatever stands above the water at the refracted pixel is not under it: keep the straight sample
                    // (faded over 3 cm, so the coping's edge does not cut a hard line)
                    float ok = 1.0 - smoothstep(P.y, P.y + 0.03, atX.y);
                    float2 uvT = lerp(screenUV, uvX, ok);
                    half3 through = SampleSceneColor(uvT);
                    color += through * (1.0 - F);
                    alpha = 1.0;
                }

                UNITY_BRANCH
                if (_FR_WaterDebug > 0.5)
                {
                    if (_FR_WaterDebug < 1.5) return half4(F.xxx, 1.0);
                    if (_FR_WaterDebug < 2.5) return half4(1.0 - probeWeight, _FR_PlanarParams.x, 0.0, 1.0);
                    if (_FR_WaterDebug < 3.5) return half4(saturate(degrees(sd) / 4.0), 0.0, 1.0);
                    if (_FR_WaterDebug < 4.5) return half4(refl + lamps, 1.0);
                    if (_FR_WaterDebug > 5.5 && _FR_WaterDebug < 6.5) return half4(refl * F + lamps, 1.0);
                    if (_FR_WaterDebug > 6.5 && _FR_WaterDebug < 7.5) return half4(color - (refl * F + lamps), 1.0);
                    if (_FR_WaterDebug > 7.5) return half4(refl + lamps, 1.0);
                    float2 uv0 = FRPlanarUV(P);
                    return half4(frac(uv0 * 4.0), saturate(abs(_FR_PlanarVP[3][3]) + abs(_FR_PlanarVP[0][0])), 1.0);
                }
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
