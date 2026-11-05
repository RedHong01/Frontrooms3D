using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// SP1 (key 1) of scene 4 (RULES 5b; research/level_space/70 section 3, 40 section 11.6; Red 10-31 17:15 and 11-02 pick A, the wall
/// under it): the slide on its wall, inside a pool skeleton. A 7 x 7 cell tiled hall (deck +-0, tiled lid 8.7, floor above
/// +9.0) where the flume rides on an aqueduct of tiled walls: a 0.6 m body on the flume's line, a pier on every cell
/// corner, a round arch in every bay by the crown rule (walk arch where c >= 3.6, sight arch 2.2-3.6, plinth below), a
/// coping 0.9 x 0.30 on a 0.15 corbel stepping where the riding line comes down to it, a tiled saddle under the trough at
/// every pier, a kerb where the line runs under 0.60. The round tower (2 x 2 cells, 3.6 m, a spiral of 48 risers of 0.1875
/// in four turns, helical slot windows) rises through the lid to the start room (+9.0) and its tub (+9.3); a B3 core
/// outside the east wall is the second way up and the Relay's. The riding line: 62 courses (9.3 m) over about 79 m in
/// cells of 3.0 and 45-degree bend bays (r 3.0), mixed styles changing only at joints (SL-A milky aqua tube, SL-B ivory with
/// a teal band, SL-C ivory with caps), out through the west wall, across a room you never reach (seen through a round arch),
/// round in the service void, back through the north wall and into a 0.9 m catch pool; a terraced water court with weirs
/// under a floating office ceiling (pool M1, M7). E in the tub rides it (about 15 s, no stop, look within 70 degrees);
/// R puts you back in the tub; L (alias N) night, F the flume's water. The Relay never rides and never climbs the tower.
/// Greybox where no kit exists, listed on the sign.
/// </summary>
public sealed class FrontRoomsStructuresSlide : FrontRoomsStructuresDistrict
{
    const float Course = FrontRoomsStructuresKit.Course;
    const float RideEye = .75f;
    const float TubX0 = -5.7f, TubX1 = -4.5f, TubZ0 = 2.1f, TubZ1 = 3.9f, TubSeat = 9.3f;
    const float Lid = 8.7f, Upper = 9.0f, UpperLid = 12.0f;
    static readonly Vector3 Arrival = new Vector3(-9f, 0f, 13.5f);
    static readonly Vector3 ArrivalLook = new Vector3(6f, 1.62f, 9f);
    static readonly Vector3 TowerCentre = new Vector3(-7.5f, 0f, 3f);
    static readonly Vector3 RideEnd = new Vector3(1.5f, -.95f, 15f);

    // A piece of the riding line: a cell (line) or a 45-degree bend bay (arc r 3.0), its drop in courses and its style.
    sealed class Piece
    {
        public bool arc;
        public Vector3 a, b, centre;
        public float r, a0, a1, length, r0, r1, floor;
        public int courses, style, zone, i0, i1;
        public float Grade => length > 1e-3f ? courses * Course / length : 0f;
    }

    // Styles: -1 the tub (no flume), 0 SL-A, 1 SL-B, 2 SL-C. Zones: 0 hall, 1 crossing room, 2 service void.
    const int Tub = -1, SLA = 0, SLB = 1, SLC = 2;

    readonly List<Piece> pieces = new List<Piece>();
    readonly List<Vector3> P = new List<Vector3>(), T = new List<Vector3>(), R = new List<Vector3>(), U = new List<Vector3>();
    readonly List<float> S = new List<float>();
    readonly List<int> pieceOf = new List<int>();
    float level;

    FrontRoomsStructuresKit kit;
    GameObject wetPart;
    Material tile, nosing, block, slA, ivory, teal, capMat, slCInside, flangeA, flangeC, film;
    readonly List<Light> hallLamps = new List<Light>(), nightLamps = new List<Light>();
    readonly List<Renderer> redLamps = new List<Renderer>(), greenLamps = new List<Renderer>();
    Material redOn, redOff, greenOn, greenOff;
    bool riding, night, wet, enteredStart, clicked;
    float rideS, rideV, splash, redUntil = float.MaxValue, clickAt = -1f;
    Vector3 splashFrom;
    FrontRoomsStructures hostRef;

    public FrontRoomsStructuresSlide(Vector3 site)
        : base(1, "SP1", "SP1 · 滑梯：瓷砖墙在下面托着（你选的 A）", "泳池骨架里的滑梯 A：双层高的瓷砖大厅、塔和起滑间，可以滑一次。", site) { }

    // ---------------------------------------------------------------- build

    public override void Build(FrontRoomsStructures host)
    {
        hostRef = host;
        kit = new FrontRoomsStructuresKit(host, this);
        tile = FrontRoomsPoolMaterials.Tile;
        nosing = FrontRoomsPoolMaterials.Nosing;
        block = FrontRoomsPoolMaterials.Surface("Stair_Block", new Color(.62f, .62f, .6f));
        slA = FrontRoomsPoolMaterials.Transparent("slide SL-A gelcoat", new Color(.42f, .82f, .84f, .55f), .85f);
        // 61's #F2EDDB family, nudged warmer (review SP1-F9: after the pool grade the gelcoat read R/G .91 against the tile's .89;
        // 61 l.210 wants it to read as its own warmer material).
        ivory = FrontRoomsPoolMaterials.Lit("slide gelcoat ivory", new Color(.96f, .92f, .82f), .72f);
        teal = FrontRoomsPoolMaterials.Lit("slide band teal", new Color(0f, .50f, .52f), .7f);
        capMat = FrontRoomsPoolMaterials.Lit("slide SL-C cap", new Color(.15f, .24f, .26f), .55f);
        // SL-C is dark inside (61: caps #263D42, the dark marks the blind spots): the channel's riding surface under the cap
        // takes the cap's colour with a wet gelcoat's sheen, so a closed section reads as a tube, not a lit plane (review SP1-F4).
        slCInside = FrontRoomsPoolMaterials.Lit("slide SL-C inside", new Color(.15f, .24f, .26f), .85f);
        flangeA = FrontRoomsPoolMaterials.Transparent("slide SL-A flange", new Color(.38f, .74f, .76f, .85f), .8f);
        flangeC = FrontRoomsPoolMaterials.Lit("slide flange ivory", new Color(.88f, .86f, .79f), .6f);
        film = FrontRoomsPoolMaterials.WaterOf(FrontRoomsPoolMaterials.Water.Level);   // every pool in scene 4 is level water (70 section 2)

        BuildPath();
        kit.Shell(Lid, 0f);
        kit.Safe("hall", BuildHall);
        kit.Safe("upper floor", BuildUpper);
        kit.Safe("tower", BuildTower);
        kit.Safe("core", BuildCore);
        kit.Safe("crossing room", BuildCrossing);
        kit.Safe("service void", BuildVoid);
        kit.Safe("spine", BuildSpine);
        kit.Safe("catch pool", BuildPool);
        kit.Safe("court", BuildCourt);
        kit.Safe("office ceiling", BuildFloatingCeiling);
        kit.Safe("tub", BuildTub);
        kit.Safe("flume", BuildFlume);
        kit.Safe("lamps", BuildLamps);
        kit.Finish();
        kit.Safe("flume water", BuildWater);
        kit.Safe("signs", BuildSigns);

        Box = new Bounds(Site + new Vector3(-3f, 5.6f, 15f), new Vector3(37f, 17.2f, 34f));
        LowestFloor = Site.y - .95f;
        Far = 80f;
        Stops.Clear();
        kit.Stop("到达点：西边的平台", Arrival, ArrivalLook);
        kit.Stop("起滑池（按 E 滑下去）", new Vector3((TubX0 + TubX1) * .5f, TubSeat, 3f), new Vector3(4.5f, 7.6f, 3f));
        kit.Stop("落水池边", new Vector3(3f, 0f, 10.4f), new Vector3(-2.5f, .8f, 15.5f));
        kit.Stop("台地顶上", new Vector3(9.2f, 2.4f, 20.2f), new Vector3(-1f, 1.4f, 13f));
        kit.Stop("塔底", new Vector3(-4.6f, 0f, 6.4f), new Vector3(-7.5f, 2f, 3f));

        try { host.Look.Probe(Root, new Bounds(Site + new Vector3(0f, Lid * .5f, 10.5f), new Vector3(21f, Lid, 21f)), 1024); }
        catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("SP1 probe", "SP1 probe: " + e.Message); }
        kit.Safe("pool zone look", BuildZoneLook);
        Debug.Log(FrontRoomsStructures.Log + "SP1 riding line: " + pieces.Count + " pieces, " + S[S.Count - 1].ToString("0.0") + " m, from +" + TubSeat + " to +0.00 (" + Mathf.RoundToInt((TubSeat - level) / Course) + " courses).");
    }

    // 游戏视觉's pool zone look (FrontRoomsStructuresLook.PoolRoom, as the map's AddPoolLooks): one PoolLevel volume per room
    // (70 section 2: every pool here is level water; the map gives every pool-level cell PoolLevel). The height fog sits on a
    // room's water only where all of it shares one height: the hall's catch pool (-0.05) and the court's basins (+0.50 to
    // +2.30) do not, so the hall has none; the crossing room's ankle sheet (-0.03) does. The tub and the flume are no pool water.
    void BuildZoneLook()
    {
        var look = kit.Host.Look;
        const string zone = "PoolLevel";
        look.PoolRoom(this, "hall", zone, -10.5f, 0f, 10.5f, 21f, 0f, Lid);
        look.PoolRoom(this, "start room", zone, -10.5f, 0f, 4.5f, 6f, Upper, UpperLid);
        look.PoolRoom(this, "upper corridor", zone, 4.5f, 0f, 10.5f, 3f, Upper, UpperLid);
        look.PoolRoom(this, "tower", zone, TowerCentre.x - 1.8f, TowerCentre.z - 1.8f, TowerCentre.x + 1.8f, TowerCentre.z + 1.8f, 0f, UpperLid);
        look.PoolRoom(this, "core B3", zone, 10.5f, 0f, 13.5f, 9f, 0f, UpperLid);
        look.PoolRoom(this, "crossing room", zone, -19.5f, 0f, -10.5f, 21f, -.15f, 6.6f, -.03f);
        look.PoolRoom(this, "service void", zone, -19.5f, 21f, -4.5f, 30f, 0f, 4.5f);
        look.PoolRoomNote(this, "夜景（L）只换灯：开着泳池外观时，环境光和调色跟着区域外观走。");
    }

    // ---------------------------------------------------------------- the riding line (70 section 3's table)

    void BuildPath()
    {
        pieces.Clear();
        level = TubSeat;
        Straight(TubX0 + .6f, 3f, TubX1, 3f, 0, Tub, Upper, 0);
        // 1 the slot, 2 cells, SL-B, 5c + 5c.
        Straight(-4.5f, 3f, -1.5f, 3f, 5, SLB, 0f, 0);
        Straight(-1.5f, 3f, 1.5f, 3f, 5, SLB, 0f, 0);
        // 2 a cell, SL-B, 3c.
        Straight(1.5f, 3f, 4.5f, 3f, 3, SLB, 0f, 0);
        // 3 the 180-degree bend round (4.5, 6), 4 bays of 2c, SL-A.
        Bend(4.5f, 6f, -90f, 90f, 4, 2, SLA, 0f, 0);
        // 4 five cells along z = 9, 2c each, SL-A, the last SL-C (WALL PASS, hall west wall).
        Straight(4.5f, 9f, 1.5f, 9f, 2, SLA, 0f, 0);
        Straight(1.5f, 9f, -1.5f, 9f, 2, SLA, 0f, 0);
        Straight(-1.5f, 9f, -4.5f, 9f, 2, SLA, 0f, 0);
        Straight(-4.5f, 9f, -7.5f, 9f, 2, SLA, 0f, 0);
        Straight(-7.5f, 9f, -10.5f, 9f, 2, SLC, 0f, 0);
        // 5 a cell in the crossing room, SL-C, 2c.
        Straight(-10.5f, 9f, -13.5f, 9f, 2, SLC, -.15f, 1);
        // 6 a 90-degree bend round (-13.5, 12), 2 bays of 2c, SL-A.
        Bend(-13.5f, 12f, -90f, -180f, 2, 2, SLA, -.15f, 1);
        // 7 three cells along x = -16.5, SL-A, the last SL-C (WALL PASS into the void).
        Straight(-16.5f, 12f, -16.5f, 15f, 2, SLA, -.15f, 1);
        Straight(-16.5f, 15f, -16.5f, 18f, 2, SLA, -.15f, 1);
        Straight(-16.5f, 18f, -16.5f, 21f, 2, SLC, -.15f, 1);
        // 8 in the void, all SL-C: cell, bend, cell, bend, a 1c cell (WALL PASS, hall north wall).
        Straight(-16.5f, 21f, -16.5f, 24f, 2, SLC, 0f, 2);
        Bend(-13.5f, 24f, 180f, 90f, 2, 2, SLC, 0f, 2);
        Straight(-13.5f, 27f, -10.5f, 27f, 2, SLC, 0f, 2);
        Bend(-10.5f, 24f, 90f, 0f, 2, 2, SLC, 0f, 2);
        Straight(-7.5f, 24f, -7.5f, 21f, 1, SLC, 0f, 2);
        // 9 the inside cell of the north WALL PASS, SL-C, 2c.
        Straight(-7.5f, 21f, -7.5f, 18f, 2, SLC, 0f, 0);
        // 10 a 90-degree bend round (-4.5, 18), 2 bays of 2c, SL-B.
        Bend(-4.5f, 18f, 180f, 270f, 2, 2, SLB, 0f, 0);
        // 11 the last cell, level, SL-B: the lip at (-1.5, 15), 50 mm over the water.
        Straight(-4.5f, 15f, -1.5f, 15f, 0, SLB, 0f, 0);
        Frames();
    }

    void Straight(float x0, float z0, float x1, float z1, int courses, int style, float floor, int zone)
    {
        var p = new Piece { arc = false, a = new Vector3(x0, 0f, z0), b = new Vector3(x1, 0f, z1), courses = courses, style = style, floor = floor, zone = zone };
        p.length = Vector3.Distance(p.a, p.b);
        Add(p);
    }

    void Bend(float cx, float cz, float from, float to, int bays, int coursesPerBay, int style, float floor, int zone)
    {
        var step = (to - from) / bays;
        for (var k = 0; k < bays; k++)
        {
            var p = new Piece { arc = true, centre = new Vector3(cx, 0f, cz), r = 3f, a0 = from + step * k, a1 = from + step * (k + 1), courses = coursesPerBay, style = style, floor = floor, zone = zone };
            p.a = Plan(p, 0f);
            p.b = Plan(p, 1f);
            p.length = p.r * Mathf.Abs(p.a1 - p.a0) * Mathf.Deg2Rad;
            Add(p);
        }
    }

    static Vector3 Plan(Piece p, float f)
    {
        if (!p.arc) return Vector3.Lerp(p.a, p.b, f);
        var ang = Mathf.Lerp(p.a0, p.a1, f) * Mathf.Deg2Rad;
        return p.centre + new Vector3(Mathf.Cos(ang) * p.r, 0f, Mathf.Sin(ang) * p.r);
    }

    void Add(Piece p)
    {
        p.r0 = level;
        level -= p.courses * Course;
        p.r1 = level;
        var n = Mathf.Max(2, Mathf.CeilToInt(p.length / .1f));
        p.i0 = P.Count == 0 ? 0 : P.Count - 1;
        for (var k = P.Count == 0 ? 0 : 1; k <= n; k++)
        {
            var f = k / (float)n;
            var q = Plan(p, f);
            q.y = Mathf.Lerp(p.r0, p.r1, f);
            P.Add(q);
            pieceOf.Add(pieces.Count);
        }
        // The joint sample belongs to the piece that ends there; the next piece starts on it.
        p.i1 = P.Count - 1;
        pieces.Add(p);
    }

    void Frames()
    {
        T.Clear(); R.Clear(); U.Clear(); S.Clear();
        var acc = 0f;
        for (var i = 0; i < P.Count; i++)
        {
            var a = P[Mathf.Max(0, i - 1)];
            var b = P[Mathf.Min(P.Count - 1, i + 1)];
            var t = (b - a).normalized;
            var right = new Vector3(t.z, 0f, -t.x).normalized;
            var up = Vector3.Cross(t, right).normalized;
            T.Add(t);
            R.Add(right);
            U.Add(up);
            if (i > 0) acc += Vector3.Distance(P[i - 1], P[i]);
            S.Add(acc);
        }
    }

    // ---------------------------------------------------------------- the hall, the floor above, the tower, the core

    void BuildHall()
    {
        // Deck +-0 round the catch pool (x -1.5..7.5, z 12..18).
        kit.Shell(Lid, 0f);
        kit.Slab(-10.5f, 0f, 10.5f, 12f, 0f, tile, .3f);
        kit.Slab(-10.5f, 12f, -1.5f, 18f, 0f, tile, .3f);
        kit.Slab(7.5f, 12f, 10.5f, 18f, 0f, tile, .3f);
        kit.Slab(-10.5f, 18f, 10.5f, 21f, 0f, tile, .3f);
        const float t = .3f;
        // West wall (x -10.5): the crossing room's round arch over a tiled half-wall (z 12.3..14.7, sill 1.07, springing 3.6,
        // crown 4.8) and the WALL PASS of row 4 (1.8 x 1.8 from 0.25 under the riding line, 4.40-6.20).
        kit.WallZ(-10.5f, -t * .5f, 21f + t * .5f, -.15f, Upper, tile, new List<FrontRoomsStructuresKit.Opening>
        {
            new FrontRoomsStructuresKit.Opening(8.1f, 9.9f, 4.40f, 6.20f),
            new FrontRoomsStructuresKit.Opening(12.3f, 14.7f, 1.07f, 3.6f, true),
        }, t);
        // North wall (z 21): the WALL PASS of row 8 (0.65-2.45).
        kit.WallX(21f, -10.5f, 10.5f, 0f, Upper, tile, new List<FrontRoomsStructuresKit.Opening> { new FrontRoomsStructuresKit.Opening(-8.4f, -6.6f, .65f, 2.45f) }, t);
        // South wall (z 0), up through the floor above to its lid.
        kit.WallX(0f, -10.5f - t * .5f, 13.5f + t * .5f, 0f, UpperLid + .16f, tile, null, t);
        // East wall (x 10.5): the core's foot by a round arch (z 0.6..2.4, springing 2.1) and its +9.0 door; above the
        // lid only where the corridor runs (z 0..3).
        kit.WallZ(10.5f, 0f, 3f, 0f, UpperLid + .16f, tile, new List<FrontRoomsStructuresKit.Opening>
        {
            new FrontRoomsStructuresKit.Opening(.6f, 2.4f, 0f, 2.1f, true),
            new FrontRoomsStructuresKit.Opening(.6f, 2.4f, Upper, Upper + 2.2f),
        }, t);
        kit.WallZ(10.5f, 3f, 21f + t * .5f, 0f, Upper, tile, null, t);
        // The lid (8.7 soffit, 0.3 slab, +9.0 floor above) round the tower's shaft and the slot (x -4.5..1.5, z 1.8..4.2).
        kit.SlabRoundHole(TowerCentre, 3f, 1.8f, Upper, .3f, tile);
        kit.Slab(-4.5f, 0f, 10.5f, 1.8f, Upper, tile, .3f);
        kit.Slab(-4.5f, 4.2f, 10.5f, 6f, Upper, tile, .3f);
        kit.Slab(1.5f, 1.8f, 10.5f, 4.2f, Upper, tile, .3f);
        kit.Slab(-10.5f, 6f, 10.5f, 21f, Upper, tile, .3f);
        // The bottom guard's tiled stand on the south deck by the steps out of the pool (clear of the +0.6 terrace's step run).
        kit.Box(new Vector3(4.6f, 0f, 9.9f), new Vector3(5.4f, .9f, 10.7f), tile);
        // The Relay stand-in on the deck, seen from the arrival through the walk arch of the z 9 arcade.
        kit.Relay(new Vector3(0f, 0f, 6f), Arrival);
    }

    void BuildUpper()
    {
        kit.Shell(UpperLid, Upper);
        // Start room x -10.5..4.5, z 0..6 (+9.0, 3.0 clear); corridor x 4.5..10.5, z 0..3.
        kit.WallZ(-10.5f, 0f, 6f + .08f, Upper, UpperLid + .16f, tile, null, .3f);
        kit.WallX(6f, -10.5f, 4.5f + .08f, Upper, UpperLid + .16f, tile);
        kit.WallZ(4.5f, 0f, 6f, Upper, UpperLid + .16f, tile, new List<FrontRoomsStructuresKit.Opening> { new FrontRoomsStructuresKit.Opening(1.0f, 2.0f, Upper, Upper + 2.1f) });
        kit.WallX(3f, 4.5f, 10.5f, Upper, UpperLid + .16f, tile);
        kit.Lid(-10.5f, 0f, 4.5f, 6f, UpperLid, tile);
        kit.Lid(4.5f, 0f, 10.5f, 3f, UpperLid, tile);
        // The slot's tiled half-walls (1.07, 0.2 thick) on its long sides and its east end; the tub closes its west end.
        kit.Prism(new Vector3(-4.5f, 0f, 1.7f), new Vector3(1.5f, 0f, 1.7f), Upper, Upper + 1.07f, .2f, tile);
        kit.Prism(new Vector3(-4.5f, 0f, 4.3f), new Vector3(1.5f, 0f, 4.3f), Upper, Upper + 1.07f, .2f, tile);
        kit.Prism(new Vector3(1.6f, 0f, 1.6f), new Vector3(1.6f, 0f, 4.4f), Upper, Upper + 1.07f, .2f, tile);
        // The top guard's stool, the CRT on a tiled shelf (a still of the catch pool from above), a blank rules board.
        kit.StandIn("top guard's stool", new Vector3(-5.0f, Upper + .33f, 5.0f), new Vector3(.38f, .66f, .38f), FrontRoomsPoolMaterials.Steel);
        // Shelf, CRT and board stand on the north wall's face (z 5.92), the CRT seated on its 0.42 m shelf (review SP1, missed).
        kit.Box(new Vector3(-3.1f, Upper + .95f, 5.50f), new Vector3(-1.9f, Upper + 1.03f, 5.92f), tile);
        kit.StandIn("CRT monitor (a still of the catch pool, no live camera)", new Vector3(-2.5f, Upper + 1.22f, 5.71f), new Vector3(.42f, .38f, .40f), FrontRoomsPoolMaterials.Lit("crt beige", new Color(.72f, .70f, .63f), .3f), 0f, false);
        var screen = FrontRoomsSurfaces.TryGet("Prop_ScreenCRT_On");
        kit.StandIn("CRT screen", new Vector3(-2.5f, Upper + 1.23f, 5.505f), new Vector3(.32f, .26f, .01f), screen != null ? screen : FrontRoomsPoolMaterials.Glow("crt still", new Color(.25f, .55f, .58f)), 0f, false);
        kit.StandIn("rules board (blank)", new Vector3(.2f, Upper + 1.5f, 5.905f), new Vector3(.9f, 1.2f, .03f), FrontRoomsPoolMaterials.Lit("rules board", new Color(.93f, .93f, .9f), .3f), 0f, false);
    }

    void BuildTower()
    {
        kit.Shell(Upper, 0f);
        var c = TowerCentre;
        // The shaft: 3.6 outside, wall 0.3, deck to +9.0; a round-arched door 1.2 wide (springing 2.1) on its north-east side,
        // helical slot windows 0.47 x 1.2 over the stair so a climber is glimpsed (COMMON rule 12).
        var holes = new List<FrontRoomsStructuresKit.RingHole> { new FrontRoomsStructuresKit.RingHole(45f - 19.1f, 45f + 19.1f, 0f, 2.1f, true) };
        const float first = 70f;
        for (var turn = 0; turn < 4; turn++)
            foreach (var deg in new[] { 90f, 360f })
            {
                var k = (deg - first) / 30f + 12f * turn;
                var tread = (k + 1f) * .1875f;
                var bottom = tread + .9f;
                if (bottom + 1.2f > Upper - .2f) continue;
                var at = deg >= 360f ? 0f : deg;
                holes.Add(new FrontRoomsStructuresKit.RingHole(at - 7.5f, at + 7.5f, bottom, bottom + 1.2f));
            }
        // Angles below 0 for the window at 0 degrees: the ring runs -180..180 so a hole round 0 stays whole.
        kit.Ring(c, 1.5f, 1.8f, 0f, Upper, -180f, 180f, tile, true, holes, 1.5f);
        // The newel and the spiral: 48 risers of 0.1875 in four turns of 12, first riser just past the door.
        kit.Ring(c, 0f, .25f, 0f, Upper + .9f, 0f, 360f, tile, true, null, 30f);
        kit.Spiral(c, .25f, 1.5f, first, 30f, 48, .1875f, 0f, tile, nosing, .1875f);
        // The landing at +9.0 (one tread wide, so the stair a turn below keeps 2.1 m) and a rail across the shaft beside it;
        // the cap's opening (35..100) lets out. Its collision is a walking surface exactly its own size at +9.0: the sector's
        // tread boxes, wider than the sector near the newel, stopped the player's 1.75 m capsule climbing on the inner side
        // about 7 m up (walk test 11-04).
        kit.Sector(c, .25f, 1.5f, first + 47f * 30f - 1440f, first + 47f * 30f - 1440f + 35f, Upper, .15f, tile, false);
        kit.WalkSurface(c, .25f, 1.5f, first + 47f * 30f - 1440f, first + 47f * 30f - 1440f + 35f, Upper);
        var railA = TowerCentre + new Vector3(Mathf.Cos(75f * Mathf.Deg2Rad), 0f, Mathf.Sin(75f * Mathf.Deg2Rad)) * .25f;
        var railB = TowerCentre + new Vector3(Mathf.Cos(75f * Mathf.Deg2Rad), 0f, Mathf.Sin(75f * Mathf.Deg2Rad)) * 1.5f;
        kit.Guard(railA, railB, Upper, FrontRoomsStructuresKit.GuardStyle.TileWall);
        // The cap: a tiled half-wall ring 1.07 high at +9.0, open on its north-east quadrant onto the start room.
        kit.Shell(UpperLid, Upper);
        kit.Ring(c, 1.5f, 1.8f, Upper, Upper + 1.07f, -180f, 180f, tile, true, new List<FrontRoomsStructuresKit.RingHole> { new FrontRoomsStructuresKit.RingHole(35f, 100f, Upper - .1f, Upper + 1.2f) });
        // The Relay never climbs it (70 section 3): closed in the Relay-body test.
        kit.RelayBlocker("tower door", c + new Vector3(.7f, 0f, .7f), c + new Vector3(2.2f, 2.8f, 2.2f));
        // And its head: the cap ring's opening onto the start room and the +9.0 landing behind it (RingHole 35..100 deg).
        kit.RelayBlocker("tower cap", c + new Vector3(-.5f, Upper - .3f, .3f), c + new Vector3(2.0f, Upper + 2.4f, 2.1f));
    }

    void BuildCore()
    {
        // B3 outside the hall's east wall (x 10.5..13.5, z 0..9): per storey two flights of 15 risers of 0.15 (4.2 m going),
        // landings at both ends; the deck and +9.0 landings at its south end. The walk back after a ride, and the Relay's way.
        kit.Shell(UpperLid, 0f);
        kit.Slab(10.5f, 0f, 13.5f, 9f, 0f, tile, .3f);
        kit.WallZ(13.5f, -.15f, 9.15f, 0f, UpperLid + .16f, tile, null, .3f);
        kit.WallX(9f, 10.5f, 13.5f, 0f, UpperLid + .16f, tile, null, .3f);
        kit.CoreB3(10.66f, 13.34f, .15f, 8.85f, 2.77f, new[] { 0f, 4.5f, Upper }, tile, nosing, tile, tile, true);
        kit.Lid(10.5f, 0f, 13.5f, 9f, UpperLid, tile);
        // Mid landings are closed by the scissor wall; the top landing's open edge over flight 1 has a rail (CoreB3 topOpen).
    }

    void BuildCrossing()
    {
        // The room you never reach (x -19.5..-10.5, z 0..21): floor -0.15 under ankle water 0.12 (surface -0.03), tiled lid
        // 6.6, two lamps, no door on the player's side; the flume crosses it on its own spine.
        kit.Shell(6.6f, -.15f);
        kit.Slab(-19.5f, 0f, -10.5f, 21f, -.15f, tile, .25f);
        kit.Water(-19.5f, 0f, -10.5f, 21f, -.03f);
        kit.WallZ(-19.5f, -.15f, 21.15f, -.15f, 6.76f, tile, null, .3f);
        kit.WallX(0f, -19.5f, -10.5f, -.15f, 6.76f, tile, null, .3f);
        kit.WallX(21f, -19.5f, -10.5f, -.15f, 6.76f, tile, new List<FrontRoomsStructuresKit.Opening> { new FrontRoomsStructuresKit.Opening(-17.4f, -15.6f, 2.60f, 4.40f) }, .3f);
        kit.Lid(-19.5f, 0f, -10.5f, 21f, 6.6f, tile);
    }

    void BuildVoid()
    {
        // The service void north of both rooms (x -19.5..-4.5, z 21..30): clear 4.5, block walls, one dim lamp.
        kit.Shell(4.5f, 0f);
        kit.Slab(-19.5f, 21f, -4.5f, 30f, 0f, block, .3f);
        kit.WallX(30f, -19.5f, -4.5f, 0f, 4.66f, block, null, .3f);
        kit.WallZ(-19.5f, 21f, 30.15f, 0f, 4.66f, block, null, .3f);
        kit.WallZ(-4.5f, 21f, 30.15f, 0f, 4.66f, block, null, .3f);
        kit.Lid(-19.5f, 21f, -4.5f, 30f, 4.5f, block);
        kit.Lamp("service void", LightType.Point, new Vector3(-12f, 4.2f, 25.5f), Vector3.down, 0f, 0f, .7f, 6f, new Color(.9f, .95f, 1f), false,
            new Vector3(.25f, .25f, .1f), FrontRoomsPoolMaterials.Steel, new Color(.9f, .95f, 1f));
    }

    // ---------------------------------------------------------------- support A: the wall under the flume

    void BuildSpine()
    {
        for (var k = 0; k < pieces.Count; k++)
        {
            var p = pieces[k];
            if (p.style == Tub) continue;
            kit.Shell(p.zone == 1 ? 6.6f : p.zone == 2 ? 4.5f : Lid, p.floor);
            Bay(p);
            if (k > 0 && pieces[k - 1].style != Tub) Joint(pieces[k - 1], p);
            Saddle(p.a, p.b - p.a, p.r0, p.floor, p.Grade);
            if (k == pieces.Count - 1) Saddle(p.b, p.b - p.a, p.r1, p.floor, p.Grade);
        }
    }

    // One bay: the body with its opening by the crown rule, the corbel and coping in two levels, or a kerb under 0.60.
    void Bay(Piece p)
    {
        var a = p.a;
        var b = p.b;
        var L = Vector3.Distance(a, b);
        var t = (b - a) / L;
        var floor = p.floor;
        var up0 = p.r0 - Course;
        var up1 = p.r1 - Course;
        if (p.r0 - floor < .6f + 1e-3f)
        {
            // Rows 10-11: a solid tiled kerb (top = riding - 0.15), no coping or corbel, in two steps a bay.
            var h0 = Mathf.Lerp(p.r0, p.r1, .25f) - Course;
            var h1 = Mathf.Lerp(p.r0, p.r1, .75f) - Course;
            if (h0 > floor + .02f) kit.Prism(a, a + t * (L * .5f), floor, h0, .6f, tile);
            if (h1 > floor + .02f) kit.Prism(a + t * (L * .5f), b, floor, h1, .6f, tile);
            LowCollider(p);
            return;
        }
        var c = p.r1 - .6f - floor;
        var walk = c >= 3.6f;
        var sight = !walk && c >= 2.2f;
        var thick = walk || sight ? .6f : .9f;
        var bodyLow = floor + c;
        var ops = new List<FrontRoomsStructuresKit.Opening>();
        if (walk)
        {
            var s = Mathf.Min(4.5f, Mathf.Floor(.6f * c / Course + 1e-3f) * Course);
            ops.Add(new FrontRoomsStructuresKit.Opening(.3f, L - .3f, floor, floor + s, true));
        }
        else if (sight)
        {
            var crown = floor + c - .3f;
            ops.Add(new FrontRoomsStructuresKit.Opening(L * .5f - .6f, L * .5f + .6f, floor + 1.07f, crown - .6f, true));
        }
        if (bodyLow > floor + .02f) kit.WallRun(a, b, floor, bodyLow, thick, ops, tile, true);
        var xStep = p.courses > 0 ? Mathf.Min(L, L / p.courses) : L;
        if (p.courses > 0 && xStep < L - 1e-3f)
        {
            // Upper level from the upper pier to where the riding line comes down to it; lower level beyond.
            if (up0 - .45f > floor + .02f) kit.Prism(a, a + t * xStep, Mathf.Max(floor, bodyLow), up0 - .45f, thick, tile);
            Coping(a, a + t * xStep, up0);
            Coping(a + t * xStep, b, up1);
        }
        else Coping(a, b, p.courses > 0 ? up0 : up1);
        if (p.r1 - floor < 2.4f && p.zone == 0) LowCollider(p);
    }

    // The pier where the riding line turns (a bend bay meets a cell or another bay): round, so the chords' outer wedges
    // close, at the body's, the corbel's and the coping's widths (40 11.6: a pier on every cell corner).
    void Joint(Piece prev, Piece p)
    {
        var d0 = prev.b - prev.a;
        var d1 = p.b - p.a;
        d0.y = d1.y = 0f;
        if (d0.sqrMagnitude < 1e-6f || d1.sqrMagnitude < 1e-6f || Vector3.Angle(d0, d1) < 1f) return;
        var at = p.a;
        var floor = Mathf.Max(prev.floor, p.floor);
        var riding = p.r0;
        if (riding - floor < .6f + 1e-3f)
        {
            if (riding - Course - .02f > floor + .02f) kit.Ring(at, 0f, .3f, floor, riding - Course - .02f, -180f, 180f, tile, true, null, 15f);
            return;
        }
        var thick = Mathf.Max(prev.r1 - .6f - prev.floor < 2.2f ? .9f : .6f, p.r1 - .6f - p.floor < 2.2f ? .9f : .6f);
        if (riding - .6f > floor + .02f) kit.Ring(at, 0f, thick * .5f, floor, riding - .6f, -180f, 180f, tile, true, null, 15f);
        kit.Ring(at, 0f, .375f, riding - .6f, riding - .45f, -180f, 180f, tile, true, null, 15f);
        kit.Ring(at, 0f, .45f, riding - .45f, riding - .15f, -180f, 180f, tile, true, null, 15f);
    }

    // A corbel course (0.15, 0.75 wide) and a coping (0.30, 0.9 wide) whose top is at <paramref name="top"/>.
    void Coping(Vector3 a, Vector3 b, float top)
    {
        kit.Prism(a, b, top - .45f, top - .30f, .75f, tile);
        kit.Prism(a, b, top - .30f, top, .9f, tile);
    }

    // The tiled saddle at a pier: 0.7 long, one course raked with the riding line (the trough's bottom), 5 mm under it, so it
    // never stands up through the trough downstream (review SP1-F1); the collider keeps to the lower end.
    void Saddle(Vector3 at, Vector3 dir, float riding, float floor, float grade)
    {
        if (riding - Course < floor) return;
        dir.y = 0f;
        dir.Normalize();
        var a = at - dir * .35f;
        var b = at + dir * .35f;
        a.y = riding + grade * .35f;
        b.y = riding - grade * .35f;
        kit.RakeWall(a, b, -.005f, Mathf.Max(floor, a.y - Course), Mathf.Max(floor, b.y - Course), .7f, tile, false);
        if (b.y - .005f > Mathf.Max(floor, riding - Course) + .01f) kit.Prism(a, b, Mathf.Max(floor, riding - Course), b.y - .005f, .7f, null, true);
    }

    // The flume where it runs low over the deck: solid, so nobody walks into the trough.
    void LowCollider(Piece p)
    {
        var a = p.a;
        var b = p.b;
        var top = Mathf.Max(p.r0, p.r1) + .72f;
        kit.Prism(a, b, p.floor, top, 1.1f, null, true);
    }

    // ---------------------------------------------------------------- the catch pool and the court

    void BuildPool()
    {
        kit.Shell(Lid, -.95f);
        // 3 x 2 cells on the flume's line, water 0.9 deep, its surface 50 mm under the deck, floor -0.95; 9 m beyond the lip.
        kit.Basin(-1.5f, 12f, 7.5f, 18f, -.95f, -.3f, tile);
        kit.Water(-1.5f, 12f, 7.5f, 18f, -.05f);
        kit.Shell(Lid, 0f);
        kit.Coping(new Vector3(-1.5f, 0f, 12f), new Vector3(4.5f, 0f, 12f), 0f, tile);
        kit.Coping(new Vector3(-1.5f, 0f, 18f), new Vector3(7.5f, 0f, 18f), 0f, tile);
        kit.Coping(new Vector3(-1.5f, 0f, 12f), new Vector3(-1.5f, 0f, 14.4f), 0f, tile);
        kit.Coping(new Vector3(-1.5f, 0f, 15.6f), new Vector3(-1.5f, 0f, 18f), 0f, tile);
        kit.Coping(new Vector3(7.5f, 0f, 12f), new Vector3(7.5f, 0f, 18f), 0f, tile);
        // Tiled steps out on the south side near the east end: 6 risers of 0.158, a cheek, no handrail, a dark nosing on each.
        kit.Shell(Lid, -.95f);
        kit.Flight(new Vector3(6f, -.95f, 13.5f), Vector3.back, 3f, 6, .158f, .3f, tile, nosing, true);
        kit.Prism(new Vector3(4.5f, 0f, 12f), new Vector3(4.5f, 0f, 13.5f), -.95f, 0f, .15f, tile);
        kit.Shell(Lid, 0f);
    }

    void BuildCourt()
    {
        // The terraced water court (F2, r5 S2): an L round the pool's north and east sides, four terraces 0.60 apart rising to
        // the north-east corner. North arm basins 0.30 deep, water 0.10 under each terrace, falling over a tiled weir (C5,
        // 0.60) into the next; the last (+0.50) across the pool's north edge into the pool. The east arm dry.
        kit.Shell(Lid, .6f);
        // T1 +0.6 (x -4.5..1.5): its basin only over the pool's edge (x -1.5..1.5). Every basin is 0.28 deep (70 section 3: 0.30;
        // they were built 0.40, over the player's 0.30 step: walk in from the terrace and nobody climbs out; walk test 11-04).
        kit.Box(-4.5f, 0f, 19.5f, 1.5f, .6f, 21f, tile);
        kit.Box(-4.5f, 0f, 18f, -1.5f, .6f, 19.5f, tile);
        kit.Box(-1.5f, 0f, 18f, 1.5f, .32f, 19.5f, tile);
        kit.Box(-1.5f, 0f, 18f, 1.5f, .5f, 18.15f, tile);
        kit.Water(-1.5f, 18.15f, 1.5f, 19.5f, .5f);
        Sheet(new Vector3(-1.4f, 0f, 17.985f), new Vector3(1.4f, 0f, 17.985f), .5f, -.05f);
        // T2 +1.2 (x 1.5..3.9), T3 +1.8 (x 3.9..7.5), T4 +2.4 (x 7.5..10.5, z 16.2..21, the corner).
        Terrace(1.5f, 3.9f, 1.2f, true);
        Terrace(3.9f, 7.5f, 1.8f, true);
        kit.Shell(Lid, 2.4f);
        kit.Box(7.5f, 0f, 19.5f, 10.5f, 2.4f, 21f, tile);
        kit.Box(7.5f, 0f, 16.2f, 10.5f, 2.4f, 18f, tile);
        kit.Box(7.65f, 0f, 18f, 10.5f, 2.12f, 19.5f, tile);
        kit.Box(7.5f, 0f, 18f, 7.65f, 2.3f, 19.5f, tile);
        kit.Water(7.65f, 18f, 10.5f, 19.5f, 2.3f);
        Sheet(new Vector3(7.49f, 0f, 18.05f), new Vector3(7.49f, 0f, 19.45f), 2.3f, 1.7f);
        // The east arm, dry, stepping down south to the deck at z 9.
        kit.Shell(Lid, .6f);
        kit.Box(7.5f, 0f, 13.8f, 10.5f, 1.8f, 16.2f, tile);
        kit.Box(7.5f, 0f, 12f, 10.5f, 1.2f, 13.8f, tile);
        kit.Box(7.5f, 0f, 9f, 10.5f, .6f, 12f, tile);
        // Step runs (4 risers of 0.15) at every terrace edge: two ways onto each terrace (R6).
        var f = kit;
        f.StepRun(new Vector3(1.5f, 0f, 20.25f), Vector3.right, 1.5f, .6f, 1.2f, tile, nosing);
        f.StepRun(new Vector3(3.9f, 0f, 20.25f), Vector3.right, 1.5f, 1.2f, 1.8f, tile, nosing);
        f.StepRun(new Vector3(7.5f, 0f, 20.25f), Vector3.right, 1.5f, 1.8f, 2.4f, tile, nosing);
        f.StepRun(new Vector3(9.75f, 0f, 16.2f), Vector3.forward, 1.5f, 1.8f, 2.4f, tile, nosing);
        f.StepRun(new Vector3(9.75f, 0f, 13.8f), Vector3.forward, 1.5f, 1.2f, 1.8f, tile, nosing);
        f.StepRun(new Vector3(9.75f, 0f, 12f), Vector3.forward, 1.5f, .6f, 1.2f, tile, nosing);
        f.StepRun(new Vector3(9f, 0f, 9f), Vector3.forward, 3f, 0f, .6f, tile, nosing);
        f.StepRun(new Vector3(7.5f, 0f, 10.75f), Vector3.right, 1.5f, 0f, .6f, tile, nosing);
        // The deck pocket inside the last bend, reached only from T1: a dead end by the lip.
        f.StepRun(new Vector3(-3f, 0f, 18f), Vector3.forward, 1.5f, 0f, .6f, tile, nosing);
        f.StepRun(new Vector3(-4.5f, 0f, 19.5f), Vector3.right, 1.5f, 0f, .6f, tile, nosing);
        // Guards 1.07 where a terrace drops more than 0.76 to the deck or the pool's water (tiled half-walls).
        var g = FrontRoomsStructuresKit.GuardStyle.TileWall;
        f.Guard(new Vector3(1.5f, 0f, 18.1f), new Vector3(7.5f, 0f, 18.1f), 0f, FrontRoomsStructuresKit.GuardStyle.None);
        f.Guard(new Vector3(1.5f, 0f, 18.08f), new Vector3(3.9f, 0f, 18.08f), 1.2f, FrontRoomsStructuresKit.GuardStyle.Glass);
        f.Guard(new Vector3(3.9f, 0f, 18.08f), new Vector3(7.5f, 0f, 18.08f), 1.8f, FrontRoomsStructuresKit.GuardStyle.Glass);
        f.Guard(new Vector3(7.6f, 0f, 12f), new Vector3(7.6f, 0f, 13.8f), 1.2f, g);
        f.Guard(new Vector3(7.6f, 0f, 13.8f), new Vector3(7.6f, 0f, 16.2f), 1.8f, g);
        f.Guard(new Vector3(7.6f, 0f, 16.2f), new Vector3(7.6f, 0f, 18f), 2.4f, g);
        kit.Shell(Lid, 0f);
    }

    // A north-arm terrace from x0 to x1 at top h: walkway z 19.5..21, a basin z 18..19.5 (floor h - 0.4, water h - 0.1),
    // its weir wall at x0 (crest = its water) falling into the next basin west, its south rim at h.
    void Terrace(float x0, float x1, float h, bool basin)
    {
        kit.Shell(Lid, h);
        kit.Box(x0, 0f, 19.5f, x1, h, 21f, tile);
        if (!basin) { kit.Box(x0, 0f, 18f, x1, h, 19.5f, tile); return; }
        kit.Box(x0 + .15f, 0f, 18.15f, x1, h - .28f, 19.5f, tile);
        kit.Box(x0, 0f, 18.15f, x0 + .15f, h - .1f, 19.5f, tile);
        kit.Box(x0, 0f, 18f, x1, h, 18.15f, tile);
        kit.Water(x0 + .15f, 18.15f, x1, 19.5f, h - .1f);
        Sheet(new Vector3(x0 - .01f, 0f, 18.2f), new Vector3(x0 - .01f, 0f, 19.45f), h - .1f, h - .7f);
    }

    // A falling water sheet (greybox: a transparent quad) from top down to bottom along a..b.
    void Sheet(Vector3 a, Vector3 b, float top, float bottom)
    {
        var m = FrontRoomsPoolMaterials.Transparent("water sheet", new Color(.55f, .85f, .86f, .32f), .95f);
        var keep = kit.Mesh.CastShadows;
        kit.Mesh.CastShadows = false;
        var n = Vector3.Cross(Vector3.up, b - a).normalized;
        kit.QuadPlanar(a + Vector3.up * bottom, b + Vector3.up * bottom, b + Vector3.up * top, a + Vector3.up * top, n, m);
        kit.QuadPlanar(a + Vector3.up * bottom, b + Vector3.up * bottom, b + Vector3.up * top, a + Vector3.up * top, -n, m);
        kit.Mesh.CastShadows = keep;
    }

    void BuildFloatingCeiling()
    {
        // Pool M1: a lay-in grid (L0_Ceiling, 0.6 grid, one troffer per cell) at +5.4 over the court only, hung on wires from
        // the tile lid; a plenum box 0.8 deep above it, black inside, so three missing tiles and two dead troffers open into
        // black; beyond its edges the lit tile lid shows. The one surface in the hall that is not tile.
        var grid = FrontRoomsPoolMaterials.Surface("L0_Ceiling", new Color(.86f, .83f, .70f));
        var plenum = FrontRoomsPoolMaterials.Surface("Plenum_Dark", new Color(.03f, .03f, .03f));
        const float y = 5.4f, top = 6.2f;
        kit.Shell(y, 0f);
        var cells = new List<Vector2>();
        for (var x = -4.5f; x < 10.4f; x += 3f) cells.Add(new Vector2(x, 18f));
        cells.Add(new Vector2(7.5f, 9f));
        cells.Add(new Vector2(7.5f, 12f));
        cells.Add(new Vector2(7.5f, 15f));
        var missing = new[] { new Vector2(0f, 19.2f), new Vector2(8.4f, 13.2f), new Vector2(5.4f, 18.6f) };
        for (var k = 0; k < cells.Count; k++)
        {
            var c = cells[k];
            Vector2? hole = null;
            foreach (var m in missing)
                if (m.x >= c.x && m.x < c.x + 3f && m.y >= c.y && m.y < c.y + 3f) hole = m;
            if (hole.HasValue)
            {
                var h = hole.Value;
                kit.Lid(c.x, c.y, h.x, c.y + 3f, y, grid, .04f);
                kit.Lid(h.x + .6f, c.y, c.x + 3f, c.y + 3f, y, grid, .04f);
                kit.Lid(h.x, c.y, h.x + .6f, h.y, y, grid, .04f);
                kit.Lid(h.x, h.y + .6f, h.x + .6f, c.y + 3f, y, grid, .04f);
            }
            else kit.Lid(c.x, c.y, c.x + 3f, c.y + 3f, y, grid, .04f);
            var dead = k == 2 || k == 7;
            var l = kit.Troffer(new Vector3(c.x + 1.5f, y, c.y + 1.5f + FrontRooms.Map.ModuleUnits.TrofferOffsetZ), true, false, 1f, 8f, dead);
            if (l != null) hallLamps.Add(l);
        }
        // The plenum box: sides on the L's open edges, a black top, wires up to the lid. Black inside only: each side carries
        // a 0.05 tile skin on its hall face, so from the deck the box reads as part of the tiled hall with the grid hung in it,
        // not a black beam (review SP1-F6; the grid stays the one surface in the hall that is not tile, 70 section 3).
        kit.Box(new Vector3(-4.55f, y, 18f), new Vector3(-4.5f, top, 21f), plenum, false);
        kit.Box(new Vector3(-4.5f, y, 17.95f), new Vector3(7.5f, top, 18f), plenum, false);
        kit.Box(new Vector3(7.45f, y, 9f), new Vector3(7.5f, top, 17.95f), plenum, false);
        kit.Box(new Vector3(7.5f, y, 8.95f), new Vector3(10.5f, top, 9f), plenum, false);
        kit.Box(new Vector3(-4.6f, y, 17.9f), new Vector3(-4.55f, top, 21f), tile, false);
        kit.Box(new Vector3(-4.55f, y, 17.9f), new Vector3(7.45f, top, 17.95f), tile, false);
        kit.Box(new Vector3(7.4f, y, 8.9f), new Vector3(7.45f, top, 17.95f), tile, false);
        kit.Box(new Vector3(7.45f, y, 8.9f), new Vector3(10.5f, top, 8.95f), tile, false);
        // The grid's own floor bounce: its troffers aim down and the wall boxes up, so the lay-in's underside took only the
        // pool trilight's cyan ground term and read dark olive; two weak warm up-fills over the terraces give it Level 0's
        // lit cream back (review SP1, missed).
        foreach (var f in new[] { new Vector3(-3f, 3.0f, 19.5f), new Vector3(3f, 3.4f, 19.5f), new Vector3(9f, 4.0f, 13.5f) })
            hallLamps.Add(kit.Lamp("grid bounce (up-fill)", LightType.Spot, f, Vector3.up, 150f, 110f, 2.2f, 5f, new Color(1f, .93f, .80f), true, Vector3.zero, null, Color.black));
        kit.Box(new Vector3(-4.5f, top, 18f), new Vector3(10.5f, top + .04f, 21f), plenum, false);
        kit.Box(new Vector3(7.5f, top, 9f), new Vector3(10.5f, top + .04f, 18f), plenum, false);
        foreach (var w in new[] { new Vector2(-3f, 19.5f), new Vector2(3f, 19.5f), new Vector2(9f, 19.5f), new Vector2(9f, 13.5f) })
            kit.Box(new Vector3(w.x - .005f, top + .04f, w.y - .005f), new Vector3(w.x + .005f, Lid, w.y + .005f), FrontRoomsPoolMaterials.Steel, false);
    }

    // ---------------------------------------------------------------- the tub and the start light

    void BuildTub()
    {
        kit.Shell(UpperLid, Upper);
        // A tiled basin 1.2 along the flume x 1.8 across, riding surface +9.3 (0.3 seat), its back on the cap ring.
        kit.Box(TubX0, Upper, TubZ0, TubX1, TubSeat, TubZ1, tile);
        kit.Box(TubX0, Upper, TubZ1, TubX1 - .3f, Upper + .15f, TubZ1 + .3f, tile);
        // Tiled cheeks with the stainless crossbar 1.0 over the riding surface; entered from the north past a chain.
        kit.Box(TubX0, Upper, TubZ0 - .2f, TubX1, TubSeat + 1.15f, TubZ0, tile);
        kit.Box(TubX1 - .3f, Upper, TubZ1, TubX1, TubSeat + 1.15f, TubZ1 + .2f, tile);
        kit.Box(new Vector3(TubX1 - .18f, TubSeat + .97f, TubZ0 - .1f), new Vector3(TubX1 - .12f, TubSeat + 1.03f, TubZ1 + .1f), FrontRoomsPoolMaterials.Steel, false);
        // The chain across the entry (visual only, no collider), from the cap ring's face (x -6.0 at z 4.0) to the north cheek.
        for (var k = 0; k < 12; k++)
        {
            var x = -6.0f + .05f + k * .1f;
            var sag = .08f * Mathf.Sin(Mathf.PI * k / 11f);
            kit.Box(new Vector3(x, TubSeat + .55f - sag, TubZ1 + .09f), new Vector3(x + .07f, TubSeat + .58f - sag, TubZ1 + .11f), FrontRoomsPoolMaterials.Steel, false);
        }
        // Nobody walks off the tub into the slot: the ride starts with E.
        kit.Mesh.Collider(new Vector3(TubX1 + .05f, TubSeat + .9f, (TubZ0 + TubZ1) * .5f), new Vector3(.1f, 1.8f, TubZ1 - TubZ0));
        kit.RelayBlocker("tub", new Vector3(TubX0 - .1f, Upper, TubZ0), new Vector3(TubX1, Upper + 2.4f, TubZ1 + .6f));
        // The red / green start light: a lamp head on the north cheek's east end (61: on a post facing the gate), two hooded
        // lenses a colour, one face west toward the tub and the cap ring's opening (who comes in past the chain), one east down
        // the slot toward the corridor door (review SP1-F11; the old plates faced south into the tub, edge-on from both ways).
        redOn = FrontRoomsPoolMaterials.Glow("start light red on", new Color(1.8f, .14f, .08f));
        redOff = FrontRoomsPoolMaterials.Glow("start light red off", new Color(.10f, .02f, .02f));
        greenOn = FrontRoomsPoolMaterials.Glow("start light green on", new Color(.10f, 1.6f, .35f));
        greenOff = FrontRoomsPoolMaterials.Glow("start light green off", new Color(.01f, .08f, .03f));
        var headMat = FrontRoomsPoolMaterials.Lit("start light head", new Color(.16f, .17f, .18f), .45f, .3f);
        var hx = TubX1 - .15f;
        var hz = TubZ1 + .1f;
        var h0 = TubSeat + 1.15f;
        kit.Box(new Vector3(hx - .03f, h0, hz - .03f), new Vector3(hx + .03f, h0 + .08f, hz + .03f), headMat, false);
        kit.Box(new Vector3(hx - .08f, h0 + .08f, hz - .1f), new Vector3(hx + .08f, h0 + .44f, hz + .1f), headMat, false);
        for (var side = -1; side <= 1; side += 2)
        {
            var fx = hx + side * .08f;
            for (var k = 0; k < 2; k++)
            {
                var cy = h0 + .17f + .17f * (1 - k);
                kit.Box(new Vector3(Mathf.Min(fx, fx + side * .07f), cy + .065f, hz - .075f), new Vector3(Mathf.Max(fx, fx + side * .07f), cy + .075f, hz + .075f), headMat, false);
                var lens = kit.StandIn(k == 0 ? "start light red" : "start light green", new Vector3(fx + side * .004f, cy, hz), new Vector3(.008f, .12f, .12f),
                    k == 0 ? redOn : greenOff, 0f, false).GetComponent<Renderer>();
                if (k == 0) redLamps.Add(lens); else greenLamps.Add(lens);
            }
        }
    }

    // ---------------------------------------------------------------- the flume

    internal static List<Vector2> TubeProfile(float r, int n)
    {
        var p = new List<Vector2>();
        for (var k = 0; k < n; k++)
        {
            var a = (-90f + k * 360f / n) * Mathf.Deg2Rad;
            p.Add(new Vector2(r * Mathf.Cos(a), r + r * Mathf.Sin(a)));
        }
        return p;
    }

    // The open flume: 0.9 inside, a round bottom, walls to 0.65, a 70 mm lip flange along both top edges.
    internal static List<Vector2> ChannelProfile(float grow)
    {
        float r = .45f + grow, top = .65f + grow, lip = .07f;
        var p = new List<Vector2> { new Vector2(-r - lip, top), new Vector2(-r, top) };
        for (var k = 0; k <= 16; k++)
        {
            var a = (180f + k * 180f / 16f) * Mathf.Deg2Rad;
            p.Add(new Vector2(r * Mathf.Cos(a), .45f + r * Mathf.Sin(a)));
        }
        p.Add(new Vector2(r, top));
        p.Add(new Vector2(r + lip, top));
        return p;
    }

    internal static List<Vector2> CapProfile()
    {
        var p = new List<Vector2>();
        for (var k = 0; k <= 16; k++)
        {
            var a = (180f - k * 180f / 16f) * Mathf.Deg2Rad;
            p.Add(new Vector2(.52f * Mathf.Cos(a), .65f + .52f * Mathf.Sin(a)));
        }
        return p;
    }

    void BuildFlume()
    {
        var tube = TubeProfile(.6f, 48);   // 48 sides: the 24-gon's 15 deg facets showed on the mouths (review SP1-F21)
        var channel = ChannelProfile(0f);
        var cap = CapProfile();
        var bandL = new List<Vector2> { new Vector2(-.47f, .38f), new Vector2(-.47f, .65f) };
        var bandR = new List<Vector2> { new Vector2(.47f, .65f), new Vector2(.47f, .38f) };
        var ringA = TubeProfile(.65f, 48);
        var ringC = ChannelProfile(.04f);
        kit.Shell(Lid, 0f);
        kit.Mesh.CastShadows = true;
        for (var k = 0; k < pieces.Count; k++)
        {
            var p = pieces[k];
            if (p.style == Tub) continue;
            switch (p.style)
            {
                case SLA:
                    kit.Sweep(P, R, U, p.i0, p.i1, tube, true, true, slA, S[p.i0]);
                    break;
                case SLB:
                    kit.Sweep(P, R, U, p.i0, p.i1, channel, false, true, ivory, S[p.i0]);
                    kit.Sweep(P, R, U, p.i0, p.i1, bandL, false, true, teal, S[p.i0]);
                    kit.Sweep(P, R, U, p.i0, p.i1, bandR, false, true, teal, S[p.i0]);
                    break;
                default:
                    // The channel's back faces are its riding surface (its front normals point out of the hull).
                    kit.Sweep(P, R, U, p.i0, p.i1, channel, false, true, ivory, S[p.i0], slCInside);
                    kit.Sweep(P, R, U, p.i0, p.i1, cap, false, true, capMat, S[p.i0]);
                    break;
            }
            // A flange ring at every joint (the style changes only here; Red 17:15 "样式混用").
            Ring(p.i0, p.style == SLA ? ringA : ringC, p.style == SLA, p.style == SLA ? flangeA : flangeC);
            if (k == pieces.Count - 1) Ring(p.i1, ringC, false, flangeC);
        }
        kit.Mesh.CastShadows = true;
    }

    void Ring(int i, List<Vector2> prof, bool closed, Material m)
    {
        var pts = new List<Vector3> { P[i] - T[i] * .035f, P[i] + T[i] * .035f };
        var rs = new List<Vector3> { R[i], R[i] };
        var us = new List<Vector3> { U[i], U[i] };
        kit.Sweep(pts, rs, us, 0, 1, prof, closed, true, m, 0f);
    }

    // F: a water film in the trough and the spill at the tub (61 P7), default dry: its own part, switched as a whole.
    void BuildWater()
    {
        var part = kit.BeginPart("SP1 flume water (F)");
        kit.Mesh.CastShadows = false;
        var filmProfile = new List<Vector2> { new Vector2(-.26f, .085f), new Vector2(-.13f, .03f), new Vector2(0f, .015f), new Vector2(.13f, .03f), new Vector2(.26f, .085f) };
        for (var k = 0; k < pieces.Count; k++)
        {
            var p = pieces[k];
            if (p.style == Tub) continue;
            kit.Sweep(P, R, U, p.i0, p.i1, filmProfile, false, true, film, S[p.i0]);
        }
        // 61: water 0.42 over the tub floor (+9.0), so 0.12 over the seat, spilling into the slot (review SP1-F14; the basin
        // itself is still the solid seat block). The film leaves at the lip into the pool with a short fall and a white patch
        // where it lands (review SP1-F7: nothing left the lip).
        kit.Water(TubX0 + .02f, TubZ0 + .02f, TubX1, TubZ1 - .02f, Upper + .42f, FrontRoomsPoolMaterials.Water.Level);
        Sheet(new Vector3(TubX1 + .01f, 0f, TubZ0 + .3f), new Vector3(TubX1 + .01f, 0f, TubZ1 - .3f), Upper + .42f, TubSeat + .05f);
        if (P.Count > 1)
        {
            var e = P[P.Count - 1];
            var er = R[R.Count - 1];
            var et = T[T.Count - 1];
            er.y = et.y = 0f;
            er.Normalize();
            et.Normalize();
            var foot = new Vector3(e.x, 0f, e.z);
            Sheet(foot - er * .28f, foot + er * .28f, e.y + .06f, -.05f);
            var foam = FrontRoomsPoolMaterials.Transparent("lip outflow foam", new Color(.86f, .96f, .97f, .42f), .6f);
            var keep = kit.Mesh.CastShadows;
            kit.Mesh.CastShadows = false;
            var a0 = foot - er * .45f;
            var a1 = foot + er * .45f;
            kit.QuadPlanar(a0 + Vector3.up * -.045f, a1 + Vector3.up * -.045f, a1 + et * 1.1f + Vector3.up * -.045f, a0 + et * 1.1f + Vector3.up * -.045f, Vector3.up, foam);
            kit.Mesh.CastShadows = keep;
        }
        kit.EndPart();
        wetPart = part != null ? part.gameObject : null;
        if (wetPart != null) wetPart.SetActive(false);
    }

    // ---------------------------------------------------------------- lamps

    void BuildLamps()
    {
        // Metal-halide wall boxes throwing up (the lid the brightest surface), a soft down-fill grid at 6 m, three
        // underwater lamps in the catch pool; the hall's lamps stay on in SP1 (view lamps). Intensities by eye.
        // Range 15 (was 11): in the 8.7 m hall the boxes at 5.4 / 7.2 reached the lid's middle at the window's tail (review SP1-F8).
        hallLamps.Add(kit.WallBox(new Vector3(-1.5f, 5.4f, .15f), Vector3.forward, 10f, 15f));
        hallLamps.Add(kit.WallBox(new Vector3(4.5f, 5.4f, .15f), Vector3.forward, 10f, 15f));
        hallLamps.Add(kit.WallBox(new Vector3(-7.5f, 5.4f, 20.85f), Vector3.back, 10f, 15f));
        hallLamps.Add(kit.WallBox(new Vector3(1.5f, 7.2f, 20.85f), Vector3.back, 10f, 15f));
        hallLamps.Add(kit.WallBox(new Vector3(-10.35f, 5.4f, 4.5f), Vector3.right, 10f, 15f));
        hallLamps.Add(kit.WallBox(new Vector3(-10.35f, 5.4f, 18f), Vector3.right, 10f, 15f));
        hallLamps.Add(kit.WallBox(new Vector3(10.35f, 5.4f, 6f), Vector3.left, 10f, 15f));
        hallLamps.Add(kit.WallBox(new Vector3(10.35f, 7.2f, 19.5f), Vector3.left, 10f, 15f));
        foreach (var d in new[] { new Vector2(-1.5f, 4.5f), new Vector2(4.5f, 4.5f), new Vector2(-7.5f, 10.5f), new Vector2(-1.5f, 10.5f), new Vector2(4.5f, 10.5f), new Vector2(-7.5f, 16.5f), new Vector2(-1.5f, 16.5f), new Vector2(4.5f, 16.5f) })
            hallLamps.Add(kit.DownFill(new Vector3(d.x, Lid - .07f, d.y), 4f, 14f));   // 2.4 / 12 under an 8.7 lid (review SP1-F8)
        kit.Underwater(new Vector3(7.28f, -.5f, 15f), Vector3.left);
        kit.Underwater(new Vector3(2.5f, -.5f, 12.22f), Vector3.forward);
        kit.Underwater(new Vector3(3.5f, -.5f, 17.78f), Vector3.back);
        // The crossing room: two lamps (slide M3).
        kit.WallBox(new Vector3(-19.35f, 4.0f, 6f), Vector3.right, 4f, 9f);
        kit.WallBox(new Vector3(-19.35f, 4.0f, 15f), Vector3.right, 4f, 9f);
        // The start room, the corridor, the core's landings, the tower (R8: light on every joint).
        kit.DownFill(new Vector3(-2.5f, UpperLid - .07f, 5.2f), 1.6f, 6f, false);
        kit.DownFill(new Vector3(3f, UpperLid - .07f, 1.0f), 1.6f, 6f, false);
        kit.DownFill(new Vector3(7.5f, UpperLid - .07f, 1.5f), 1.4f, 6f, false);
        foreach (var y in new[] { 4.15f, 8.65f, UpperLid - .07f })
            kit.DownFill(new Vector3(12f, y, 1.4f), 1.4f, 5f, false);
        foreach (var y in new[] { 6.4f, UpperLid - .07f })
            kit.DownFill(new Vector3(12f, y, 8.0f), 1.2f, 5f, false);
        foreach (var y in new[] { 2.4f, 5.0f, 7.6f })
            kit.Lamp("tower", LightType.Point, TowerCentre + new Vector3(.6f, y, -.6f), Vector3.down, 0f, 0f, 1.1f, 4f, new Color(.92f, .98f, 1f), false, Vector3.zero, null, Color.black);
        // Night lines: one point lamp on the axis of every SL-A section (61 section 2.2), off by day.
        foreach (var p in pieces)
        {
            if (p.style != SLA) continue;
            var mid = (p.i0 + p.i1) / 2;
            // Aqua (.30, 1, .95), 0.8: the tube's specular takes the lamp's colour, so the line stays aqua at night instead of
            // clipping to white (review SP1-F10: G/R 1.04 at (.6, 1, 1) x 1).
            var l = kit.Lamp("SL-A night line", LightType.Point, P[mid] + U[mid] * .6f, Vector3.down, 0f, 0f, .8f, 3f, new Color(.30f, 1f, .95f), true, Vector3.zero, null, Color.black);
            nightLamps.Add(l);
            host().Look.Scale(l, 0f);
        }
    }

    FrontRoomsStructures host() => hostRef;

    // ---------------------------------------------------------------- signs (70 section 3, verbatim)

    void BuildSigns()
    {
        var dir = (new Vector3(ArrivalLook.x, 0f, ArrivalLook.z) - Arrival).normalized;
        var right = new Vector3(dir.z, 0f, -dir.x);
        // 50 degrees right of the main view's axis, the face's centre 2.50 m from the arrival eye (20_placard_type.md v2.4, E's
        // FAR; was 2 ahead + 2.1 right, 2.91 m): in the arrival frame it takes +36 to +54 degrees (was +35 to +54), a little
        // less of the arcade's near end, its right third beyond the frame; 2 m north of the z 9 aqueduct, clear of the tower.
        var at = Arrival + dir * 1.5996f + right * 1.9063f;
        kit.Sign(new Vector3(at.x, 1.5f, at.z), Arrival + Vector3.up * 1.5f, "SP1 · 滑梯：瓷砖墙在下面托着（你选的 A）",
            "泳池层。双层高的瓷砖大厅，滑道骑在一道瓷砖拱墙上，塔从天花板穿到楼上的起滑间（楼面 +9.0 米，起滑面 +9.3 米）。滑道从西墙出去，穿过一间你永远走不到的房间，在墙后转一圈，再从北墙回来，落进 0.9 米深的落水池，全长约 79 米。东北角是一层层往下溢水的台地，头顶悬着一块办公室吊顶。站进起滑间的池子按 E 滑一次（约 15 秒，中途不能停），滑完按 R 回到池子；N 切换夜景，F 切换滑道里有没有水。东墙外的楼梯是走回起滑间的路，也是 Relay 的路。\n"
            + "灰盒占位：滑道（没有真实的玻璃钢和螺栓）、拱墙细部、起滑灯、监视器、规则牌、水和焦散；瓷砖用的是 150 毫米瓷白砖贴图（只在这个场景里）。黑色人形是 Relay 的静态替身（2.05 米），只用来检查视线，不会动。\n"
            + "要你决定：1 这套滑梯放进泳池层吗？ 2 滑道里有没有水流（61 P7，按 F 对比）？ 3 落水池 0.9 米还是 1.07 米（63 Q8）？ 4 空无一人时起滑灯变绿，每局一次，配 61 的声音提示，可以吗（63 Q7）？ 5 办公室吊顶（M1，你已选）放在台地上方，这个位置对吗？ 6 滑道是象牙色（比瓷砖暖一点），可以吗？", 0f);
        // Metric marks (M): the catch pool's depth (0.9 built; 1.07 asked), the floors, the slot, the tower.
        kit.Mark(new Vector3(-1.2f, .35f, 12.6f), Vector3.back, "落水池水深 0.90 米（63 Q8：或 1.07）");
        kit.Mark(new Vector3(-1.2f, .02f, 12.6f), Vector3.back, "1.07 米水深的水面在 +0.12");
        kit.Mark(new Vector3(-4.0f, Upper + 1.6f, 4.6f), Vector3.back, "起滑面 +9.30 · 楼面 +9.00");
        kit.Mark(new Vector3(-4.6f, 2.6f, 6.4f), Vector3.forward, "塔 Ø3.6 · 48 级 × 0.1875");
        kit.Mark(new Vector3(0f, 6.2f, 9f), Vector3.back, "拱：c 4.65 · 起拱 2.70（Relay 可过）");
        kit.Mark(new Vector3(9f, 3.2f, 20.4f), Vector3.back, "台地 +0.6 / +1.2 / +1.8 / +2.4");
    }

    // ---------------------------------------------------------------- the frame: the ride, the start light, keys

    public override string FeetPrompt(FrontRoomsStructures host, Vector3 feet)
    {
        if (riding || !InTub(feet)) return null;
        return "E  ·  按 E 滑下去（约 15 秒，中途不能停）";
    }

    bool InTub(Vector3 feet)
    {
        var l = feet - Site;
        return l.x > TubX0 - .05f && l.x < TubX1 + .05f && l.z > TubZ0 - .05f && l.z < TubZ1 + .05f && l.y > TubSeat - .2f && l.y < TubSeat + .6f;
    }

    public override void UseAtFeet(FrontRoomsStructures host, Vector3 feet)
    {
        if (riding || !InTub(feet) || host.Walker == null) return;
        if (host.Walker.RelayBody) { host.Flash("Relay 不能滑滑梯。"); return; }
        riding = true;
        rideS = 0f;
        rideV = 0f;
        splash = -1f;
        redUntil = float.MaxValue;
        host.Walker.BeginRide(Heading(0));
        host.Flash("滑下去……（中途不能停）");
    }

    float Heading(int i)
    {
        var t = T[Mathf.Clamp(i, 0, T.Count - 1)];
        return Mathf.Atan2(t.x, t.z) * Mathf.Rad2Deg;
    }

    static float SpeedFor(float grade)
    {
        // 70 section 3: 25 % -> 7 m/s, 15 % -> 6.3, a 12.7 % bend bay -> 5.8, 10 % -> 5.3, 5 % -> 4, level -> 3.
        float[] g = { 0f, .05f, .10f, .127f, .15f, .25f };
        float[] v = { 3f, 4f, 5.3f, 5.8f, 6.3f, 7f };
        if (grade <= 0f) return v[0];
        if (grade >= g[g.Length - 1]) return v[v.Length - 1];
        for (var k = 0; k + 1 < g.Length; k++)
            if (grade <= g[k + 1]) return Mathf.Lerp(v[k], v[k + 1], (grade - g[k]) / (g[k + 1] - g[k]));
        return v[v.Length - 1];
    }

    int SampleAt(float s)
    {
        int lo = 0, hi = S.Count - 1;
        while (hi - lo > 1)
        {
            var mid = (lo + hi) / 2;
            if (S[mid] <= s) lo = mid; else hi = mid;
        }
        return lo;
    }

    public override void Tick(FrontRoomsStructures host, float dt, bool isCurrent)
    {
        if (!isCurrent || host.Walker == null) return;
        var walker = host.Walker;
        if (riding) Ride(host, walker, dt);
        // The start light: red during a ride and 4 s after; the first visit to the start room clicks it from red to green
        // with nobody at the tub, once (63 Q7; silent, rule 24).
        var feet = walker.transform.position - Site;
        // Only in the room itself, not on the tower's last risers inside the cap ring (review SP1-F11).
        if (!enteredStart && feet.y > Upper - .3f && feet.y < UpperLid && feet.x > -10.5f && feet.x < 4.5f && feet.z > 0f && feet.z < 6f
            && new Vector2(feet.x - TowerCentre.x, feet.z - TowerCentre.z).magnitude > 1.9f)
        {
            enteredStart = true;
            clickAt = Time.time + 1.2f;
        }
        bool red;
        if (!clicked)
        {
            red = true;
            if (clickAt > 0f && Time.time >= clickAt) { clicked = true; redUntil = 0f; red = false; }
        }
        else red = riding || Time.time < redUntil;
        foreach (var r in redLamps) if (r != null) r.sharedMaterial = red ? redOn : redOff;
        foreach (var g in greenLamps) if (g != null) g.sharedMaterial = red ? greenOff : greenOn;
    }

    void Ride(FrontRoomsStructures host, FrontRoomsStructuresWalker walker, float dt)
    {
        var total = S[S.Count - 1];
        if (splash < 0f)
        {
            var i = SampleAt(rideS);
            var p = pieces[Mathf.Clamp(pieceOf[Mathf.Clamp(i, 0, pieceOf.Count - 1)], 0, pieces.Count - 1)];
            var target = p.style == Tub ? 1.5f : SpeedFor(p.Grade);
            rideV += (target - rideV) * (1f - Mathf.Exp(-dt / 1f));
            rideS += rideV * dt;
            if (rideS >= total)
            {
                splash = 0f;
                splashFrom = P[P.Count - 1] + Vector3.up * (RideEye - FrontRoomsStructuresWalker.Eye);
            }
            else
            {
                var j = SampleAt(rideS);
                var f = S[j + 1] > S[j] ? (rideS - S[j]) / (S[j + 1] - S[j]) : 0f;
                var at = Vector3.Lerp(P[j], P[j + 1], f);
                walker.RideTo(Site + at + Vector3.up * (RideEye - FrontRoomsStructuresWalker.Eye), Heading(j));
                return;
            }
        }
        // Into the catch pool: 3 m past the lip, standing in the water, facing east.
        splash += dt / .6f;
        var e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(splash));
        walker.RideTo(Site + Vector3.Lerp(splashFrom, RideEnd, e), 90f);
        if (splash < 1f) return;
        riding = false;
        redUntil = Time.time + 4f;
        walker.EndRide(Site + RideEnd, 90f);
        StopIndex = 1;
        host.Flash("滑完了。R：回到起滑池。东南角的台阶出水，东墙外的楼梯回到楼上。");
    }

    public override void Arrived(FrontRoomsStructures host, int stop)
    {
        // A jump, R or [ ] during a ride ends it where the walker lands.
        if (riding && host.Walker != null)
        {
            riding = false;
            redUntil = Time.time + 4f;
            var s = Stops[Mathf.Clamp(stop, 0, Stops.Count - 1)];
            host.Walker.EndRide(s.position, s.yaw);
        }
    }

    public override void Leave(FrontRoomsStructures host)
    {
        if (riding && host.Walker != null)
        {
            riding = false;
            host.Walker.EndRide(host.Walker.transform.position, host.Walker.Yaw);
        }
        if (night) SetNight(host, false);
    }

    public override FrontRoomsStructuresLook.Preset PresetAt(Vector3 world) => night ? FrontRoomsStructuresLook.Preset.PoolNight : FrontRoomsStructuresLook.Preset.Pool;

    public override bool KeyPressed(FrontRoomsStructures host, Key key, out string message)
    {
        message = null;
        if (key == UnityEngine.InputSystem.Key.L || key == UnityEngine.InputSystem.Key.N)
        {
            SetNight(host, !night);
            message = night ? "夜景：大厅的灯降到 20%，SL-A 段亮成一条光线。" : "白天。";
            return true;
        }
        if (key == UnityEngine.InputSystem.Key.F)
        {
            wet = !wet;
            if (wetPart != null) wetPart.SetActive(wet);
            message = wet ? "滑道里有水流（61 P7）。" : "滑道是干的。";
            return true;
        }
        return false;
    }

    void SetNight(FrontRoomsStructures host, bool on)
    {
        night = on;
        foreach (var l in hallLamps) if (l != null) host.Look.Scale(l, on ? .2f : 1f);
        foreach (var l in nightLamps) if (l != null) host.Look.Scale(l, on ? 1f : 0f);
        // Aqua that stays aqua (G/R >= 1.6 in the frame): x4 tonemapped and bloomed to white (review SP1-F10); x1.4 under the
        // pool grade's +.6 still clipped to G/R 1.2, so x0.8.
        if (slA != null && slA.HasProperty("_EmissionColor")) slA.SetColor("_EmissionColor", on ? new Color(.02f, .55f, .55f) * .8f : Color.black);
        host.Look.Want(PresetAt(Site));
    }

    public override void RelayBody(FrontRoomsStructures host, bool on)
    {
        if (kit != null) kit.SetRelay(on);
    }

    public override void ShowMetrics(FrontRoomsStructures host, bool on)
    {
        if (kit != null) kit.ShowMarks(on);
    }

    public override string Status(FrontRoomsStructures host) =>
        (riding ? "正在滑" : "按 E 在起滑池里滑") + " · " + (night ? "夜景" : "白天") + " · 滑道" + (wet ? "有水" : "干");

    public override void Guide(FrontRoomsStructures host, StringBuilder sb)
    {
        sb.AppendLine(Title);
        sb.AppendLine("泳池层的滑梯（你选的 A：瓷砖墙在下面托着）。7 × 7 格的瓷砖大厅，楼上 +9.0 是起滑间；滑道从起滑池出发，沿拱墙绕一个 180 度弯，穿过西墙外一间走不到的房间，在北墙后的设备空间里转一圈，从北墙回来，落进落水池。");
        sb.AppendLine();
        sb.AppendLine("按键：E（站在起滑池里）滑一次，约 15 秒，中途不能停，鼠标可以左右看 70 度；R 回到起滑池；[ ] 换站点（到达点、起滑池、落水池边、台地顶上、塔底）；L 或 N 夜景；F 滑道里的水；C Relay 体型（塔和起滑池对它关闭）；M 尺寸。");
        sb.AppendLine("上楼的两条路：塔里的螺旋楼梯（玩家的近路），或者东墙外的楼梯（每层两段 15 级，Relay 的路）。");
        sb.AppendLine();
        sb.AppendLine("要你决定（在对话里按编号告诉总控）：");
        sb.AppendLine("SP1-1 这套滑梯放进泳池层吗？");
        sb.AppendLine("SP1-2 滑道里有没有水流（61 P7，按 F 对比）？");
        sb.AppendLine("SP1-3 落水池 0.9 米还是 1.07 米（63 Q8，按 M 看标记）？");
        sb.AppendLine("SP1-4 空无一人时起滑灯变绿，每局一次，配 61 的声音提示，可以吗（63 Q7）？");
        sb.AppendLine("SP1-5 办公室吊顶（M1，你已选）放在台地上方，这个位置对吗？");
        sb.AppendLine("SP1-6 滑道是象牙色（比瓷砖暖一点），可以吗？");
        sb.AppendLine();
        sb.AppendLine("灰盒占位：滑道（没有真实的玻璃钢和螺栓）、拱墙细部、起滑灯、监视器、规则牌、水和焦散；瓷砖用的是 150 毫米瓷白砖贴图（只在这个场景里）。黑色人形是 Relay 的静态替身（2.05 米），只用来检查视线，不会动。");
        if (pieces.Count > 0)
        {
            sb.AppendLine();
            sb.Append("技术记录：滑道 ").Append(pieces.Count - 1).Append(" 段，长 ").Append(S[S.Count - 1].ToString("0.0")).Append(" 米，+9.30 到 +0.00；拱：c ≥ 3.6 走拱，2.2-3.6 观景拱，以下实心。").AppendLine();
        }
    }

    public override void Release()
    {
        base.Release();
    }
}
