// FrontRoomsGlassFoil.hlsl -- V7a (C13, foiled windows): the burglar-alarm foil ON the glass, drawn by FrontRooms/Glass in
// the pane frame (pane metres from the slab centre: the UV0 that the slab, the stage meshes, the shards, the teeth and the floor
// glass all carry for the grime), so the tape rides up with a pried sash and tears exactly where the glass tears, through every
// stage and the return after a walk away. Included by both passes of FrontRooms/Glass after their Hash11 / ValueNoise.
// Research: Documentation/research/glass/v7_foil/10_research.md.
//
// The tape (US 4,026,757, 1977): lead-tin, about 3/8 in (9.5 mm) wide and 0.002 in thick, laid a set margin inside the sight
// line (here 1 in), on a narrow coat of varnish and under a WIDER, brushed overcoat. Corners are turned with a 45 deg mitre fold:
// the tape folds on the diagonal of the corner square, so the outer corner triangle stays bare glass and the inner triangle is
// two layers with the crease along their shared edge. The two ends run up the ramps of a double foil block (US 4,084,879,
// 1978): under the block's footprint (_FoilGap*) there is no tape on the glass.
// The foil lies on the room face (_FoilShape.z = that face's sign along the pane's +Z). From the other side it is seen through
// 6 mm of glass: its underside, flat on the glass, reads as a slightly hazy mirror strip, placed where the refracted view ray
// meets the far face (no parallax shift in the floor-glass mode, whose axes are not the pane's).
// Per window (FrontRoomsGlassBreakable's MaterialPropertyBlock; the debris world copies it onto shards, teeth and floor glass).
// _FoilShape.z = 0 on every material and on every window the map did not foil: the callers skip all of this in a uniform branch.
#ifndef FRONTROOMS_GLASS_FOIL_INCLUDED
#define FRONTROOMS_GLASS_FOIL_INCLUDED

#define FR_FOIL_ETA 0.65789474

struct FRFoil
{
    half cover;    // the tape's coverage, antialiased (0-1)
    half front;    // 1: the eye sees the tape's own varnished face; 0: its underside, through the glass
    half halo;     // the varnish overcoat on bare glass beside the tape (0-1)
    half fold;     // the crease of a corner's mitre fold (0-1, a line)
    half flap;     // inside a fold's double layer (0-1)
    half edge;     // the varnish meniscus along the tape's cut edges (0-1)
    half contact;  // underside: 1 where the tape lies flat on the glass, lower where it bridges a wrinkle
    half2 tilt;    // normal tilt along (pane u, pane v): the crinkled tape and the brushed varnish
};

// Signed distance (m, negative on the tape) from p to one loop of tape whose OUTER edge is the rectangle r = (x0, y0, x1, y1),
// with the run's local frame: along (pane metres along the run), across (metres in from the outer edge), outward (unit, toward
// the outer edge), corner (toward the outer corner of the nearest corner, signs only), inCorner (1 inside a corner square) and
// crease (metres from the fold's crease line).
float FRFoilLoop(float2 p, float4 r, float w, out float along, out float across, out float2 outward, out float2 corner,
                 out float inCorner, out float crease)
{
    float dl = p.x - r.x;
    float dr = r.z - p.x;
    float db = p.y - r.y;
    float dt = r.w - p.y;
    float dx = min(dl, dr);
    float dy = min(db, dt);
    float dmin = min(dx, dy);
    float band = abs(dmin - 0.5 * w) - 0.5 * w;                 // the straight runs
    float mitre = (w - (dx + dy)) * 0.70710678;                 // the fold: the outer corner triangle is bare
    bool vertical = dx < dy;
    along = vertical ? p.y : p.x;
    across = vertical ? dx : dy;
    corner = float2(dl < dr ? -1.0 : 1.0, db < dt ? -1.0 : 1.0);
    outward = vertical ? float2(corner.x, 0.0) : float2(0.0, corner.y);
    inCorner = step(dx, w) * step(dy, w);
    crease = abs(dx + dy - w) * 0.70710678;
    return max(band, mitre);
}

// Signed distance to a box (centre xy, half size zw); a zero half size means no block (never inside).
float FRFoilBox(float2 p, float4 b)
{
    float2 q = abs(p - b.xy) - b.zw;
    float d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0);
    return b.z > 0.0 ? d : 1.0e4;
}

// m: pane metres of this fragment; px: one pixel in pane metres (from the caller, before any divergent branch);
// nGeo: the drawn face's normal (toward the eye); nPane: the pane's +Z (world); axisU / axisV: pane u / v (world).
FRFoil FRFoilAt(float2 m, float px, float3 nGeo, float3 nPane, float3 axisU, float3 axisV, float3 viewWS, float thickness, bool floorMode)
{
    FRFoil f = (FRFoil)0;
    f.contact = 1.0h;
    float faceSign = dot(nGeo, nPane) >= 0.0 ? 1.0 : -1.0;
    f.front = (half)step(0.0, faceSign * _FoilShape.z);
    float2 p = m;
    if (f.front < 0.5h && !floorMode)
    {
        // Seen from the back: the tape lies on the far face. Follow the refracted eye ray through the glass to it.
        float3 t = refract(-viewWS, nGeo, FR_FOIL_ETA);
        float dn = max(abs(dot(t, nGeo)), 0.25);
        p += float2(dot(t, axisU), dot(t, axisV)) * (thickness / dn);
    }
    float w = max(_FoilShape.x, 1e-4);
    float along, across, inCorner, crease;
    float2 outward, corner;
    float sd = FRFoilLoop(p, _FoilRectA, w, along, across, outward, corner, inCorner, crease);
    float seed = 0.0;
    [branch] if (_FoilRectB.z > _FoilRectB.x)
    {
        float alongB, acrossB, inCornerB, creaseB;
        float2 outwardB, cornerB;
        float sdB = FRFoilLoop(p, _FoilRectB, w, alongB, acrossB, outwardB, cornerB, inCornerB, creaseB);
        if (sdB < sd)
        {
            sd = sdB; along = alongB; across = acrossB; outward = outwardB; corner = cornerB; inCorner = inCornerB; crease = creaseB;
            seed = 17.3;
        }
    }
    // The blocks' footprints are bare glass: the two ends of the tape run up the blocks' ramps (the block mesh draws them).
    sd = max(sd, -FRFoilBox(p, _FoilGapA));
    sd = max(sd, -FRFoilBox(p, _FoilGapB));

    float aa = max(px, 1e-5);
    f.cover = (half)saturate(0.5 - sd / aa);
    // the varnish overcoat: wider than the tape, brushed on with a dauber, so its edge wanders (1.5-4 mm past the tape)
    float halo = max(_FoilShape.y, 0.0);
    float runKey = dot(outward, float2(3.1, 7.7)) + seed;
    [branch] if (sd < halo * 1.5 + 2.0 * aa)
    {
        float hw = halo * (0.55 + 0.9 * ValueNoise(float2(along * 38.0, runKey)));
        half film = (half)(saturate(0.5 - (sd - hw) / max(aa, 0.0006)) * (0.65 + 0.35 * ValueNoise(float2(along * 90.0, across * 300.0 + runKey))));
        f.halo = film * (1.0h - f.cover);
        // the tape's cut edges: varnish pools in a fine meniscus along them
        f.edge = (half)saturate(1.0 - (-sd) / (0.0006 + aa)) * f.cover;
        // crinkles: the tape is burnished down by hand, so it keeps fine wrinkles across the run, long gentle waves and
        // burnish streaks along it; the fine terms fade out where a pixel is wider than they are (no shimmer at distance)
        float fine = saturate(1.0 - aa / 0.0012);
        float flapKey = inCorner;   // the folded-over layer has its own wrinkles
        float wl = ValueNoise(float2(along * 70.0, across * 40.0 + runKey)) - 0.5;
        float wf = (ValueNoise(float2(along * 430.0, across * 160.0 + runKey * 1.7 + flapKey * 5.1)) - 0.5) * fine;
        float wb = (ValueNoise(float2(along * 21.0, across * 2600.0 + runKey * 2.3)) - 0.5) * fine;
        float tAlong = 0.07 * wf + 0.04 * wl;
        float tAcross = 0.11 * wl + 0.05 * wf + 0.03 * wb;
        // the varnish's brush marks on the bare glass beside the tape: a faint ripple across the strokes
        float brush = (ValueNoise(float2(along * 12.0, across * 900.0 + runKey)) - 0.5) * fine;
        tAcross += 0.05 * brush * (float)f.halo;
        float2 tilt = outward.x != 0.0 ? float2(-outward.x * tAcross, tAlong) : float2(tAlong, -outward.y * tAcross);
        // the mitre fold: the crease is the folded edge of the top layer (a rounded bend), which leans toward the bare corner
        f.flap = (half)(inCorner * f.cover);
        f.fold = (half)(inCorner * saturate(1.0 - crease / (0.00045 + aa)) * f.cover);
        tilt += corner * 0.70710678 * 0.45 * (float)f.fold;
        f.tilt = (half2)tilt;
        f.contact = (half)saturate(0.8 + 2.4 * wf);
    }
    return f;
}

#endif
