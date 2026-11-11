using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene 4's pool lamps (structures_assets G2-a, 2026-11-07; line visual (game visual)): Red's image 1 (SP1's slide hall, SP2's
/// well over the pool, SP6's deep pool terrain) lit by image 2's lamps (Red, 2026-11-07 21:54, research/structures_assets/
/// 60_build.md G2-a): the zone lamps line's production kits in main, used as they are (FrontRoomsLampKit: P1-A WallBox, P2-A
/// Niche, ZL_NICHE_S NicheSmall; G1's ZL_FLUSH), placed where scene 4's stand-ins stood (the G2 light review, sections 2.1-2.3;
/// the zone lamps line's placement note of 21:58; the map's rig, FrontRoomsMapWorld.Pool.cs). Inert
/// until the K2 builder (FrontRoomsStructuresKit.PlaceLampKits) calls it after a pool district's shell has its colliders.
///
/// Wall boxes (P1-A): on the wall face behind the call's point (a ray back from 0.6 m out: SP1 and SP2 give the face, SP6 the
/// wall's centre line 0.15 m inside it), kit frame +Z out of the wall and +Y up; the up-light at face + out 0.16 + up 0.22 thrown
/// 30 degrees off the wall (110 / 70), or along the caller's aim (SP6's dome boxes); the lit-ceiling patch (the kit's glow)
/// fitted to the lid straight over the box and shown only where that lid is flat over the patch (2.4 x the throw along the wall,
/// 2.2 x out): under a vault, a dome, a well or a floating ceiling it is put out and the light shows the real lid.
/// Niches: by the water at the wall's foot (the floor 0.3 m out from the wall): deeper than 0.95 m P2-A (300 W), its ring 0.70 m
/// under the water; 0.60-0.95 m ZL_NICHE_S (100 W), its ring 0.35 m under (a stand-in that becomes one keeps the light scene 4
/// tuned, so image 1's water reads as it did; an extra one, on a stair's cheek or under a weir, x 0.4 light and x 0.6 range of
/// the district's niches); never on a wall
/// tilted more than 15 degrees (a beach, a tread), never with less than 0.25 m of water under the ring. The glass stands 1.2 mm
/// proud of the tile (the kit's own offset): the frame goes on the face the ray found. The light 0.12 m (small 0.08) out of
/// the ring, aimed into the water 12 degrees down (n + down 0.22) unless the scene aimed it steeper (SP2's basin, 30 degrees).
/// The bounce (the down-fill under a lit lid) follows its box (Bind: the nearest box within 7 m whose lid is the fill's lid,
/// with nothing between them); a fill with none gets a ZL_FLUSH on the lid over it.
/// The metal-halide drive (HidLevel) is the map's (FrontRoomsMapWorld.HidLevel, the zone lamps' HidTick): Dead a dark bay, Stutter and
/// Dim an aged lamp at 60 %, Failing cycling; while the arc is out or warming the quartz lamp holds 15 % in its warm white
/// ("darkness is never cover"). The niches are incandescent: Steady or Dead, and on a dip they dim with the line (V^3.4).
/// Every lens is named "Lens" under its lamp, as the stand-in's was, so the scene's reflection proxies find it. Nothing here
/// makes a light; the caller's lights are moved and retuned only. ASCII only.
/// </summary>
public static class FrontRoomsStructuresPoolLamps
{
    // ---------------------------------------------------------------- numbers (zone lamps note 21:58; zone_lamps 20_design 3.4, 6)

    /// <summary>The up-light's place over the box's mount (the map's rig): out of the wall, up, and its throw off the wall.</summary>
    public const float UpOut = .16f, UpUp = .22f, ThrowDegrees = 30f;
    /// <summary>A wall face is looked for this far in front of and behind the call's point (m).</summary>
    public const float WallSearch = .6f;
    /// <summary>The lid counts as flat over the patch when every ray meets it within this of the throw (m), or 3 % of it.</summary>
    public const float GlowFlatTolerance = .08f;
    /// <summary>A bounce follows a box this near (m, in plan) whose lid lies within BounceLidTolerance of its own.</summary>
    public const float BounceReach = 7f, BounceLidTolerance = .5f;
    /// <summary>The niche families by the water at the wall's foot (m), their rings under the water, the floor clear under a ring
    /// (the zone lamps line's rule: P2-A over 0.95 m, ZL_NICHE_S 0.60-0.95 m, 0.25 m clear). One deviation, ours: ZL_NICHE_S
    /// still goes in down to 0.55 m with 0.20 m clear under its ring (SP6's C9 cold plunge, 0.57 m: its 165 mm ring's rim stays
    /// 0.12 m over the floor), so a basin scene 4 lit keeps a lamp instead of going dark.</summary>
    public const float NicheDeep = .95f, NicheShallow = .55f, NicheRingDeep = .70f, NicheRingShallow = .35f, NicheFloorClear = .25f, SmallFloorClear = .20f;
    /// <summary>The steepest wall a niche goes on (degrees off vertical).</summary>
    public const float NicheMaxTilt = 15f;
    /// <summary>ZL_NICHE_S against P2-A: 100 W against 300 W (its light), and its throw (range).</summary>
    public const float SmallIntensity = .4f, SmallRange = .6f;
    /// <summary>The niche light out of its ring (m), and its aim down into the water (the map's n + down x 0.22).</summary>
    public const float NicheLightOut = .12f, SmallLightOut = .08f, NicheAimDown = .22f;
    /// <summary>The quartz restrike lamp's share of the box's light, an aged lamp's level, and an incandescent lamp's response to
    /// its line (output ~ V^3.4).</summary>
    public const float QuartzLevel = .15f, AgedLevel = .6f, IncandescentPower = 3.4f;
    /// <summary>The quartz lamp's colour (the map's HidQuartzTint).</summary>
    public static readonly Color QuartzTint = new Color(1f, .78f, .55f);
    /// <summary>P1-A's metal-halide white and its lens drive (the map's: the light's colour x 1.65); a niche's lens drive is its
    /// light's colour x 2.2 (the map's PoolBasinNiches).</summary>
    public static readonly Color BoxColour = new Color(.90f, .97f, 1f);
    public const float BoxLensGain = 1.65f, NicheLensGain = 2.2f;
    /// <summary>The scene's reflection-proxy layer (FrontRoomsStructuresLook.ProxyLayer): rays never stop on it.</summary>
    public const int ProxyLayer = 27;

    /// <summary>A pool's water as the builder made it (world): its rectangle and its height.</summary>
    public struct WaterRect
    {
        public Rect xz;
        public float y;

        public WaterRect(Rect xz, float y)
        {
            this.xz = xz;
            this.y = y;
        }
    }

    /// <summary>A placed wall box (world): where its kit stands, the throw to the lid over it, whether its patch is shown.</summary>
    public sealed class Box
    {
        public Light light;
        public Transform lamp;
        public Vector3 face, outward;
        /// <summary>From the mount to the lid straight over it (m); 0 when no lid was found within 30 m.</summary>
        public float throwUp;
        /// <summary>The highest lid met over the lit patch (world y); NegativeInfinity when none.</summary>
        public float lidTop = float.NegativeInfinity;
        public bool flat, onWall;
        public MeshRenderer lens, glow;
    }

    /// <summary>A placed niche (world): its family, the ring's centre on the wall face, the wall's normal, the water and floor.</summary>
    public sealed class Niche
    {
        public Light light;
        public FrontRoomsLampKit.Family family;
        public Vector3 ring, normal;
        public float water, floor;
        public MeshRenderer lens;
        public bool Small => family == FrontRoomsLampKit.Family.NicheSmall;
    }

    static int Mask => ~((1 << ProxyLayer) | (1 << FrontRoomsWaterPlanar.WaterLayer));

    /// <summary>The district whose geometry the rays see (the caller sets it for one placement pass, then null): the walker's
    /// capsule, a neighbour and anything outside the district never stop a ray. Null: every collider.</summary>
    public static Transform Scope;

    static readonly RaycastHit[] hits = new RaycastHit[32];

    static bool Ray(Vector3 origin, Vector3 dir, float max, out RaycastHit hit)
    {
        hit = default(RaycastHit);
        var n = Physics.RaycastNonAlloc(origin, dir, hits, max, Mask, QueryTriggerInteraction.Ignore);
        var best = float.MaxValue;
        var found = false;
        for (var i = 0; i < n; i++)
        {
            var h = hits[i];
            if (h.collider == null || (Scope != null && !h.collider.transform.IsChildOf(Scope))) continue;
            if (h.distance < best) { best = h.distance; hit = h; found = true; }
        }
        return found;
    }

    static bool Blocked(Vector3 from, Vector3 to)
    {
        var d = to - from;
        var len = d.magnitude;
        RaycastHit hit;
        return len > 1e-4f && Ray(from, d / len, len, out hit);
    }

    static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 1e-8f ? v.normalized : Vector3.forward;
    }

    static string M(float v) => v.ToString("0.00");

    // ---------------------------------------------------------------- the wall box (P1-A)

    /// <summary>
    /// P1-A for a wall box whose light the scene made (world: <paramref name="mount"/> the call's point, <paramref name="outward"/>
    /// out of the wall, <paramref name="aim"/> the light's own aim or zero for the 30-degree throw): the kit under
    /// <paramref name="lamp"/> on the wall face, its glow shown only under a flat lid, the light moved to the kit's lamp and aimed.
    /// Null (nothing built, the light untouched) when the kit does not build; note says what was done.
    /// </summary>
    public static Box PlaceWallBox(Transform lamp, Light light, Vector3 mount, Vector3 outward, Vector3 aim, bool phone, out string note)
    {
        note = "";
        if (lamp == null) { note = "no lamp object"; return null; }
        var o = Flat(outward);
        var up = Vector3.up;
        var box = new Box { light = light, lamp = lamp, outward = o, face = mount };
        RaycastHit hit;
        // the face: back from 0.6 m out along the call's normal (SP6's calls sit on the wall's centre line, 0.15 m inside it)
        if (Ray(mount + o * WallSearch, -o, WallSearch * 2f, out hit) && Vector3.Dot(Flat(hit.normal), o) > .9f && Mathf.Abs(hit.normal.y) < .26f)
        {
            box.face = hit.point;
            box.onWall = true;
        }
        else note = "no wall face within " + M(WallSearch) + " m: on the call's point; ";
        // the lid straight over the box, from 0.3 m out (the box's glass slopes out of the wall)
        box.throwUp = Ray(box.face + o * .3f + up * .3f, up, 30f, out hit) ? hit.distance + .3f : 0f;
        var d = box.throwUp;
        // the patch the kit lights (WallBoxGlowSpread 2.4): along the wall 2.4 d, out 0.1-2.2 d; flat when every ray meets the lid
        // within 8 cm (3 % on a high lid) of the throw
        var along = Vector3.Cross(up, o);
        var flat = d > .2f;
        if (d > 0f) box.lidTop = box.face.y + d;
        if (d > 0f)
            for (var i = 0; i < 5; i++)
                for (var j = 0; j < 4; j++)
                {
                    var p = box.face + along * ((i / 4f - .5f) * 2.4f * d) + o * ((.1f + j / 3f * 2.1f) * d) + up * .3f;
                    if (Ray(p, up, 30f, out hit))
                    {
                        if (Mathf.Abs(hit.point.y - box.face.y - d) > Mathf.Max(GlowFlatTolerance, .03f * d)) flat = false;
                        box.lidTop = Mathf.Max(box.lidTop, hit.point.y);
                    }
                    else flat = false;
                }
        box.flat = flat;
        var rot = Quaternion.LookRotation(o, up);
        box.lens = FrontRoomsLampKit.BuildWallBox(lamp, lamp.InverseTransformPoint(box.face), Quaternion.Inverse(lamp.rotation) * rot,
            FrontRoomsLampKit.LensName, phone, d > .2f ? d : 2f, out box.glow);
        if (box.lens == null) { note += "the wall box kit did not build"; return null; }
        if (box.glow != null && !flat) box.glow.gameObject.SetActive(false);
        if (light != null)
        {
            light.transform.position = box.face + o * UpOut + up * UpUp;
            var a = aim.sqrMagnitude > 1e-6f ? aim.normalized
                : (up * Mathf.Cos(ThrowDegrees * Mathf.Deg2Rad) + o * Mathf.Sin(ThrowDegrees * Mathf.Deg2Rad)).normalized;
            light.transform.rotation = Quaternion.LookRotation(a, o);
        }
        note += "throw " + M(d) + " m, lid " + (flat ? "flat: patch on" : "not flat: patch off");
        return box;
    }

    // ---------------------------------------------------------------- the niches (P2-A, ZL_NICHE_S)

    /// <summary>
    /// The wet niche for an underwater light the scene made (world: <paramref name="pos"/> its stand-in's point,
    /// <paramref name="into"/> into the water): the wall behind it, the water over it (<paramref name="waters"/>, the district's
    /// own rectangles) and the floor at the wall's foot pick P2-A or ZL_NICHE_S; the kit goes on the face, the light to the ring.
    /// <paramref name="forceSmall"/> (an extra niche: a stair's cheek, a weir's receiving basin): ZL_NICHE_S wherever the water is
    /// 0.60 m or more, its light x 0.4 and range x 0.6. Null (nothing built, the light untouched) where neither fits.
    /// </summary>
    public static Niche PlaceNiche(Transform lamp, Light light, Vector3 pos, Vector3 into, IList<WaterRect> waters, bool phone, bool forceSmall, out string note)
    {
        note = "";
        if (lamp == null) { note = "no lamp object"; return null; }
        var n = Flat(into);
        var up = Vector3.up;
        RaycastHit hit;
        if (!Ray(pos + n * WallSearch, -n, WallSearch + .6f, out hit)) { note = "no wall within 0.6 m behind it"; return null; }
        if (Mathf.Abs(hit.normal.y) > Mathf.Sin(NicheMaxTilt * Mathf.Deg2Rad)) { note = "its wall tilts more than " + NicheMaxTilt + " degrees"; return null; }
        var nh = Flat(hit.normal);
        if (Vector3.Dot(nh, n) < .7f) { note = "the wall behind it does not face the water"; return null; }
        var face = hit.point;
        // the water over it: the lowest surface above the stand-in over the point 0.3 m out from the wall
        var probe = face + nh * .3f;
        var water = float.NaN;
        if (waters != null)
            foreach (var w in waters)
            {
                var r = w.xz;
                if (probe.x < r.xMin - .05f || probe.x > r.xMax + .05f || probe.z < r.yMin - .05f || probe.z > r.yMax + .05f) continue;
                if (w.y < pos.y - .02f) continue;
                if (float.IsNaN(water) || w.y < water) water = w.y;
            }
        if (float.IsNaN(water)) { note = "under no water of its district"; return null; }
        var floor = Ray(new Vector3(probe.x, water - .02f, probe.z), Vector3.down, 8f, out hit) ? hit.point.y : water - 3f;
        var depth = water - floor;
        FrontRoomsLampKit.Family family;
        float ring;
        if (depth > NicheDeep && !forceSmall) { family = FrontRoomsLampKit.Family.Niche; ring = water - NicheRingDeep; }
        else if (depth >= NicheShallow) { family = FrontRoomsLampKit.Family.NicheSmall; ring = water - NicheRingShallow; }
        else { note = "water at the wall's foot " + M(depth) + " m (under " + M(NicheShallow) + "): no niche"; return null; }
        if (ring - floor < NicheFloorClear && family == FrontRoomsLampKit.Family.Niche) { family = FrontRoomsLampKit.Family.NicheSmall; ring = water - NicheRingShallow; }
        var clear = family == FrontRoomsLampKit.Family.NicheSmall ? SmallFloorClear : NicheFloorClear;
        if (ring - floor < clear) { note = "less than " + M(clear) + " m of water under the ring"; return null; }
        if (!FrontRoomsLampKit.Available(family)) { note = family + " kit missing"; return null; }
        // the face at the ring's own height (a wall that steps or slopes below the stand-in)
        var at = new Vector3(face.x, ring, face.z);
        if (Ray(at + nh * WallSearch, -nh, WallSearch + .3f, out hit) && Vector3.Dot(Flat(hit.normal), nh) > .9f
            && Mathf.Abs(hit.normal.y) <= Mathf.Sin(NicheMaxTilt * Mathf.Deg2Rad))
            at = hit.point;
        var rot = Quaternion.LookRotation(nh, up);
        var lens = FrontRoomsLampKit.Build(family, lamp, lamp.InverseTransformPoint(at), Quaternion.Inverse(lamp.rotation) * rot, FrontRoomsLampKit.LensName, phone);
        if (lens == null) { note = family + " kit did not build"; return null; }
        var result = new Niche { light = light, family = family, ring = at, normal = nh, water = water, floor = floor, lens = lens };
        if (light != null)
        {
            light.transform.position = at + nh * (result.Small ? SmallLightOut : NicheLightOut);
            var kitAim = (nh + Vector3.down * NicheAimDown).normalized;
            var now = light.transform.forward;
            light.transform.rotation = Quaternion.LookRotation(now.y < kitAim.y ? now : kitAim, up);
            // an extra niche (a stair's cheek, a weir) is a 100 W lamp against the district's niches; a stand-in that becomes a
            // ZL_NICHE_S keeps the light scene 4 tuned for it (Red 2026-11-07 21:54: image 1's water kept as it is)
            if (result.Small && forceSmall)
            {
                light.intensity *= SmallIntensity;
                light.range *= SmallRange;
            }
        }
        note = family + " ring " + M(water - at.y) + " m under the water, " + M(depth) + " m of water at the wall's foot";
        return result;
    }

    // ---------------------------------------------------------------- the bounce (a down-fill) and ZL_FLUSH

    /// <summary>True when a down-fill at <paramref name="fill"/> (world, just under its lid) is the bounce of
    /// <paramref name="box"/>: within BounceReach in plan of the box's lit patch (along the wall 1.2 x the throw either side, out to
    /// 2.2 x the throw: the bounce comes down from the patch), the same lid (within BounceLidTolerance of the box's), nothing
    /// between the fill and the box's patch or front. <paramref name="distance"/> the plan distance to the patch.</summary>
    public static bool Bind(Vector3 fill, Box box, out float distance)
    {
        distance = float.MaxValue;
        if (box == null) return false;
        var up = Vector3.up;
        var reach = Mathf.Max(box.throwUp, .5f);
        var along = Vector3.Cross(up, box.outward);
        var rel = fill - box.face;
        float a = Vector3.Dot(rel, along), b = Vector3.Dot(rel, box.outward);
        float ca = Mathf.Clamp(a, -1.2f * reach, 1.2f * reach), cb = Mathf.Clamp(b, 0f, 2.2f * reach);
        distance = Mathf.Sqrt((a - ca) * (a - ca) + (b - cb) * (b - cb));
        if (distance > BounceReach) return false;
        RaycastHit hit;
        if (!Ray(fill - up * .02f, up, 4f, out hit)) return false;
        var lid = hit.point.y;
        var boxLid = box.face.y + box.throwUp;
        if (Mathf.Abs(lid - box.lidTop) > BounceLidTolerance && Mathf.Abs(lid - boxLid) > BounceLidTolerance) return false;
        var from = fill - up * .3f;
        var front = box.face + box.outward * .5f + up * .3f;
        var patch = box.face + box.outward * (Mathf.Max(box.throwUp, .5f) * .6f) + up * Mathf.Max(box.throwUp - .3f, .3f);
        return !Blocked(from, front) || !Blocked(from, patch);
    }

    /// <summary>ZL_FLUSH (G1's F5 sealed flush lens) on the lid over a down-fill with no box of its own (world
    /// <paramref name="fill"/>, just under the lid), under <paramref name="lamp"/>; null when there is no lid within 1 m or the
    /// kit does not build. The light stays where it is.</summary>
    public static MeshRenderer PlaceFlush(Transform lamp, Vector3 fill, bool phone, out string note)
    {
        note = "";
        if (lamp == null) { note = "no lamp object"; return null; }
        RaycastHit hit;
        if (!Ray(fill - Vector3.up * .02f, Vector3.up, 1f, out hit)) { note = "no lid within 1 m over it"; return null; }
        if (!FrontRoomsLampKit.Available(FrontRoomsLampKit.Family.Flush)) { note = "the ZL_FLUSH kit is missing"; return null; }
        var lens = FrontRoomsLampKit.Build(FrontRoomsLampKit.Family.Flush, lamp, lamp.InverseTransformPoint(hit.point), Quaternion.Inverse(lamp.rotation),
            FrontRoomsLampKit.LensName, phone);
        note = lens != null ? "ZL_FLUSH on the lid " + M(hit.distance) + " m over the light" : "the ZL_FLUSH kit did not build";
        return lens;
    }

    // ---------------------------------------------------------------- the drive

    /// <summary>A lamp's colour drift, -1 (greener) .. 1 (pinker), from its place (stable from build to build).</summary>
    public static float Drift(Vector3 p)
    {
        unchecked
        {
            var h = (uint)(Mathf.RoundToInt(p.x * 10f) * 73856093 ^ Mathf.RoundToInt(p.y * 10f) * 19349663 ^ Mathf.RoundToInt(p.z * 10f) * 83492791);
            h ^= h >> 13;
            h *= 0x5bd1e995u;
            h ^= h >> 15;
            return (h & 0xffffu) / 32767.5f - 1f;
        }
    }

    /// <summary>
    /// A metal-halide box's level this frame (the map's HidLevel): its temperament (<paramref name="mode"/>, the game's), its
    /// line (<paramref name="mains"/>: 1, or a dip), HidTick's drop-out, restrike and warm-up; Stutter and Dim at 60 %, Dead 0;
    /// the quartz lamp's 15 % while the arc is out or warming. <paramref name="colour"/> multiplies the light's and the lens's
    /// colour (the warm-up's blue-green and the lamp's drift, or the quartz lamp's warm white).
    /// </summary>
    public static float HidLevel(ref FrontRoomsLampKit.HidLamp s, FrontRoomsStructuresLampModes.Mode mode, float mains, float dt, out Color colour)
    {
        colour = Color.white;
        if (mode == FrontRoomsStructuresLampModes.Mode.Dead) return 0f;
        s.failing = mode == FrontRoomsStructuresLampModes.Mode.Failing;
        float auxiliary;
        Color tint;
        var output = FrontRoomsLampKit.HidTick(ref s, mains, dt, out tint, out auxiliary);
        if (mode == FrontRoomsStructuresLampModes.Mode.Stutter || mode == FrontRoomsStructuresLampModes.Mode.Dim) output *= AgedLevel;
        var quartz = auxiliary * QuartzLevel;
        colour = quartz > output ? QuartzTint : tint;
        return Mathf.Max(output, quartz);
    }

    /// <summary>An incandescent niche's level: Dead 0 (the game's "use Dead"), else its line to the 3.4 (a dip dims it at once
    /// and it comes back with the line).</summary>
    public static float NicheLevel(FrontRoomsStructuresLampModes.Mode mode, float mains)
    {
        if (mode == FrontRoomsStructuresLampModes.Mode.Dead) return 0f;
        return Mathf.Pow(Mathf.Clamp01(mains), IncandescentPower);
    }

    /// <summary>An incandescent lamp takes only two of the game's temperaments: Dead, or Steady.</summary>
    public static FrontRoomsStructuresLampModes.Mode Incandescent(FrontRoomsStructuresLampModes.Mode rolled) =>
        rolled == FrontRoomsStructuresLampModes.Mode.Dead ? FrontRoomsStructuresLampModes.Mode.Dead : FrontRoomsStructuresLampModes.Mode.Steady;

    /// <summary>The lens drives: P1-A's (the box's white x 1.65), a niche's (its light's colour x 2.2), ZL_FLUSH's (its light's
    /// tint at FlushLensPeak).</summary>
    public static Color BoxLensDrive => BoxColour * BoxLensGain;
    public static Color NicheLensDrive(Color lightColour) => new Color(lightColour.r * NicheLensGain, lightColour.g * NicheLensGain, lightColour.b * NicheLensGain, 1f);
    public static Color FlushLensDrive(Color lightColour) => FrontRoomsStructuresLamps.LensDrive(lightColour, FrontRoomsLampKit.FlushLensPeak, 1f);
}
