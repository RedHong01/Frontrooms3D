using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// SP5 (key 5) of scene 4 (RULES 5b; research/level_space/70 section 7; 20 V6; 30 RN-P2; 31 E8; 40 sections 6.2, 11.4-11.5;
/// 63 RN-SPINE, RN-DOCK; zone lamps R-B): Run! B's service corridor as a ramp to the dock. 27 m: a level head cell, then a
/// 1:8 ramp 24 m long falling 3.0 m through two fire-door walls (a two-riser step down past each frame), to the goal door;
/// behind it a Tall loading dock (a platform 1.20 over a truck bay, a chain guard with a swing gate, two ways down, a
/// half-raised roll-up door over a ramp to the night street in sodium light). A riser shaft opens overhead at mid-ramp; a
/// chain-link cage and a propped-open janitor's closet stand at the head. T (alias L) trips the power: only the battery
/// heads remain. The corridor stays a corridor: no catwalk, no plant room, no spiral. Run! B's own rig is built for one
/// level corridor (FrontRoomsRunRig, the map's Corridor1x9), so its lamp sets are emulated here with the game's R-B strip
/// kit, twin heads and Run! B's numbers; the sign's greybox line and the G panel say so. Greybox where no kit exists.
/// </summary>
public sealed class FrontRoomsStructuresRun : FrontRoomsStructuresDistrict
{
    const float Clear = 2.9f, Dock = -3.0f, Bay = -4.2f, DockCeil = 1.2f;
    static readonly Vector3 Arrival = new Vector3(0f, 0f, 1.5f);
    static readonly Color Flood = new Color(1f, .84f, .64f);
    static readonly Color RunTint = new Color(.90f, .95f, 1f);
    static readonly Color Sodium = new Color(1f, .55f, .22f);

    FrontRoomsStructuresKit kit;
    Material lower, upper, floorMat, soffit, cove, galv, block, steel, red, concrete, waxMat, laneMat, accentMat;
    readonly List<Light> armed = new List<Light>(), heads = new List<Light>(), dockStrips = new List<Light>();
    bool dockLit = true;
    readonly List<Renderer> armedLens = new List<Renderer>(), headLens = new List<Renderer>(), pilots = new List<Renderer>(), redTubes = new List<Renderer>();
    // The game's Run! B numbers (the rig's own defaults): the red tube's bounce and burn (card zone_lamps 1, 11-04).
    readonly RunRigLayout rigNumbers = new RunRigLayout();
    Material pilotOn, pilotOff, lensOff, headOn;
    Transform doorLeaf;
    Collider doorCollider;
    bool tripped, doorOpen;
    MaterialPropertyBlock block2;

    public FrontRoomsStructuresRun(Vector3 site)
        : base(5, "SP5", "SP5 · Run!：一路下坡到卸货间", "Run! 的坡道走廊和卸货间。", site) { }

    // The floor and soffit of the spine (district metres) at z: the 70 section 7 table.
    static float FloorAt(float z)
    {
        if (z <= 3f) return 0f;
        if (z <= 7.8f) return -(z - 3f) / 8f;
        if (z <= 9.3f) return -.6f;
        if (z <= 9.6f) return -.75f;
        if (z <= 16.8f) return -.9f - (z - 9.6f) / 8f;
        if (z <= 18.3f) return -1.8f;
        if (z <= 18.6f) return -1.95f;
        if (z <= 25.8f) return -2.1f - (z - 18.6f) / 8f;
        return -3f;
    }

    // ---------------------------------------------------------------- build

    public override void Build(FrontRoomsStructures host)
    {
        kit = new FrontRoomsStructuresKit(host, this);
        lower = FrontRoomsPoolMaterials.Surface("RunB_BlockLower", new Color(.55f, .56f, .55f));
        upper = FrontRoomsPoolMaterials.Surface("RunB_BlockUpper", new Color(.78f, .78f, .74f));
        floorMat = FrontRoomsPoolMaterials.Surface("RunB_Floor", new Color(.62f, .61f, .58f));
        soffit = FrontRoomsPoolMaterials.Surface("RunB_Soffit", new Color(.7f, .7f, .68f));
        cove = FrontRoomsPoolMaterials.Surface("RunB_Cove", new Color(.3f, .3f, .3f));
        galv = FrontRoomsPoolMaterials.Surface("RunB_GalvSteel", new Color(.6f, .62f, .63f));
        red = FrontRoomsPoolMaterials.Surface("RunB_SprinklerRed", new Color(.6f, .1f, .08f));
        block = upper;
        steel = FrontRoomsPoolMaterials.Steel;
        concrete = FrontRoomsPoolMaterials.Lit("SP5 sealed concrete (greybox)", new Color(.52f, .52f, .50f), .25f);
        pilotOn = FrontRoomsPoolMaterials.Glow("SP5 head pilot red", new Color(2.6f, .2f, .1f));
        pilotOff = FrontRoomsPoolMaterials.Glow("SP5 head pilot off", new Color(.08f, .02f, .02f));
        headOn = FrontRoomsPoolMaterials.Glow("SP5 head lens on", Flood * 8f);
        lensOff = FrontRoomsPoolMaterials.Glow("SP5 lens off", new Color(.05f, .05f, .05f));

        kit.Safe("spine", BuildSpine);
        kit.Safe("fire-door walls", BuildFireWalls);
        kit.Safe("riser shaft", BuildShaft);
        kit.Safe("head: closet and cage", BuildHead);
        kit.Safe("goal door", BuildGoalDoor);
        kit.Safe("dock", BuildDock);
        kit.Safe("outside", BuildOutside);
        kit.Safe("lamps", BuildLamps);
        kit.Finish();
        kit.Safe("zone volume", () => FrontRoomsPostStack.EnsureZoneVolume(Root, "Run_B", new Bounds(new Vector3(1.5f, -.6f, 22f), new Vector3(12f, 8f, 46f))));
        kit.Safe("signs", BuildSigns);
        SetTripped(false);

        Box = new Bounds(Site + new Vector3(1.5f, 0f, 22f), new Vector3(16f, 16f, 50f));
        LowestFloor = Site.y + Bay;
        Far = 80f;
        Stops.Clear();
        kit.Stop("到达点：走廊头上", Arrival, new Vector3(0f, 1.62f, 27f));
        kit.Stop("坡道中段（竖井下）", new Vector3(0f, FloorAt(12.2f), 12.2f), new Vector3(0f, 3.5f, 13.5f));
        kit.Stop("回头看坡道", new Vector3(0f, FloorAt(24f), 24f), new Vector3(0f, .8f, 6f));
        kit.Stop("卸货平台", new Vector3(1.5f, Dock, 29f), new Vector3(4.5f, Bay + 1.2f, 39f));
        kit.Stop("卡车位", new Vector3(3.5f, Bay, 36.5f), new Vector3(4.5f, Bay + 1f, 44f));
        kit.Stop("储物笼前", new Vector3(.2f, 0f, .9f), new Vector3(3f, 1.2f, 1.6f));
    }

    // ---------------------------------------------------------------- the spine: head, three runs, landings, two STEPs

    void BuildSpine()
    {
        // Pieces: (z0, z1); the floor and the soffit follow FloorAt; past each fire wall the soffit steps down 0.30 behind the header.
        float[][] pieces =
        {
            new[] { 0f, 3f }, new[] { 3f, 7.8f }, new[] { 7.8f, 9.3f }, new[] { 9.6f, 16.8f }, new[] { 16.8f, 18.3f }, new[] { 18.6f, 25.8f }, new[] { 25.8f, 27f },
        };
        foreach (var p in pieces) Piece(p[0], p[1], FloorAt(p[0] + 1e-3f), FloorAt(p[1] - 1e-3f));
        // The two STEPs (B1): two risers of 0.15 in the lower cell's first fine module, past each frame.
        kit.Shell(Clear, -.9f);
        kit.StepRun(new Vector3(0f, 0f, 9.3f), Vector3.back, 2.84f, -.9f, -.6f, floorMat, cove);
        Walls(9.3f, 9.6f, -.9f, -.9f, 2.0f, 2.0f);
        Soffit(9f, 9.6f, 2.0f, 2.0f);
        kit.Shell(Clear, -2.1f);
        kit.StepRun(new Vector3(0f, 0f, 18.3f), Vector3.back, 2.84f, -2.1f, -1.8f, floorMat, cove);
        Walls(18.3f, 18.6f, -2.1f, -2.1f, .8f, .8f);
        Soffit(18f, 18.6f, .8f, .8f);
        // The services step down at each fire wall and start at its downhill face (D6; review SP5-F8).
        Services(9.08f, 9.6f, -.9f, -.9f);
        Services(18.08f, 18.6f, -2.1f, -2.1f);
    }

    // One piece of the spine from z0 to z1 (floor f0 -> f1, 1:8 or level): floor, Run! B's two-tone walls, soffit, services.
    void Piece(float z0, float z1, float f0, float f1)
    {
        kit.Shell(Mathf.Max(f0, f1) + Clear, Mathf.Min(f0, f1));
        if (Mathf.Abs(f0 - f1) < 1e-3f) kit.Box(-1.5f, f0 - .25f, z0, 1.5f, f0, z1, floorMat);
        else kit.Ramp(new Vector3(0f, f1, z1), Vector3.back, 3f, z1 - z0, f0 - f1, floorMat, .25f);
        FloorBands(z0, z1, f0, f1);
        // The soffit over landings A and B starts at the fire wall's downhill face; the head's own walls are built with the closet and cage.
        var s0 = f0 + Clear;
        var s1 = f1 + Clear;
        if (z0 > 3f - 1e-3f) Walls(z0, z1, f0, f1, s0, s1);
        else
        {
            WallsWest(z0, z1, f0, f1, s0, s1, true);
        }
        var sz1 = z1;
        if (Mathf.Abs(z1 - 9.3f) < 1e-3f || Mathf.Abs(z1 - 18.3f) < 1e-3f) sz1 = z1 - .3f;   // the soffit runs to the fire wall
        var se = s0 + (s1 - s0) * (sz1 - z0) / Mathf.Max(1e-3f, z1 - z0);
        if (z0 < 12f && sz1 > 15f)
        {
            // The riser shaft (z 12..15): the soffit opens over the whole cell, cut edges 2.9 over the floor.
            Soffit(z0, 12f, s0, FloorAt(12f) + Clear);
            Soffit(15f, sz1, FloorAt(15f) + Clear, se);
        }
        else Soffit(z0, sz1, s0, se);
        Services(z0, sz1, f0, f0 + (f1 - f0) * (sz1 - z0) / Mathf.Max(1e-3f, z1 - z0));
    }

    void Walls(float z0, float z1, float f0, float f1, float s0, float s1)
    {
        WallsWest(z0, z1, f0, f1, s0, s1, false);
        WallFace(1.42f, -1f, z0, z1, f0, f1, s0, s1);
        WallCollider(1.5f, z0, z1, Mathf.Min(f0, f1), Mathf.Max(s0, s1));
    }

    void WallsWest(float z0, float z1, float f0, float f1, float s0, float s1, bool headOnly)
    {
        if (headOnly) return;
        WallFace(-1.42f, 1f, z0, z1, f0, f1, s0, s1);
        WallCollider(-1.5f, z0, z1, Mathf.Min(f0, f1), Mathf.Max(s0, s1));
    }

    // A wall's inner face (x, facing +-x) from z0 to z1: the lower tone to 1.2 over the floor (the paint line runs with the
    // ramp), the upper tone to the soffit, a cove and a bumper rail at 0.9.
    void WallFace(float x, float facing, float z0, float z1, float f0, float f1, float s0, float s1)
    {
        var n = new Vector3(facing, 0f, 0f);
        Quad(new Vector3(x, f0, z0), new Vector3(x, f1, z1), new Vector3(x, f1 + 1.2f, z1), new Vector3(x, f0 + 1.2f, z0), n, lower);
        Quad(new Vector3(x, f0 + 1.2f, z0), new Vector3(x, f1 + 1.2f, z1), new Vector3(x, s1, z1), new Vector3(x, s0, z0), n, upper);
        Tilted(new Vector3(x + facing * .005f, f0 + .05f, z0), new Vector3(x + facing * .005f, f1 + .05f, z1), .01f, .102f, cove);
        Tilted(new Vector3(x + facing * .03f, f0 + .9f, z0), new Vector3(x + facing * .03f, f1 + .9f, z1), .05f, .12f, FrontRoomsPoolMaterials.Surface("RunB_BumperVinyl", new Color(.25f, .25f, .25f)));
    }

    void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 front, Material m)
    {
        // Metre UVs: u along z, v world y.
        kit.Quad(a, b, c, d, front, new Vector2(a.z, a.y), new Vector2(b.z, b.y), new Vector2(c.z, c.y), new Vector2(d.z, d.y), m);
    }

    void WallCollider(float x, float z0, float z1, float y0, float y1)
    {
        kit.Mesh.Collider(new Vector3(x, (y0 + y1) * .5f, (z0 + z1) * .5f), new Vector3(.16f, y1 - y0, z1 - z0));
    }

    // A box from a to b (centre line, district metres) with a w x h section, tilted with the line; no collider.
    void Tilted(Vector3 a, Vector3 b, float w, float h, Material m)
    {
        var d = b - a;
        var len = d.magnitude;
        if (len < 1e-3f || m == null) return;
        kit.Mesh.Box((a + b) * .5f, new Vector3(w, h, len), Quaternion.LookRotation(d / len, Vector3.up), m, false);
    }

    // Run! B's floor (the rig's layout numbers: wax band 0.15 along each wall, lane 0.45 each side of the axis, accent rows
    // 0.30 wide centred at x +-0.6), 2 mm over the field, render only, following the ramp (review SP5-F7: one flat grey).
    void FloorBands(float z0, float z1, float f0, float f1)
    {
        if (z1 - z0 < 1e-3f) return;
        if (waxMat == null)
        {
            waxMat = FrontRoomsPoolMaterials.Surface("RunB_FloorWax", new Color(.66f, .65f, .61f));
            laneMat = FrontRoomsPoolMaterials.Surface("RunB_FloorLane", new Color(.58f, .57f, .54f));
            accentMat = FrontRoomsPoolMaterials.Surface("RunB_FloorAccent", new Color(.45f, .44f, .42f));
        }
        Band(-1.345f, .15f, z0, z1, f0, f1, waxMat);
        Band(1.345f, .15f, z0, z1, f0, f1, waxMat);
        Band(0f, .9f, z0, z1, f0, f1, laneMat);
        Band(-.6f, .3f, z0, z1, f0, f1, accentMat);
        Band(.6f, .3f, z0, z1, f0, f1, accentMat);
    }

    void Band(float x, float w, float z0, float z1, float f0, float f1, Material m)
    {
        const float h = .01f, lift = .002f;
        Tilted(new Vector3(x, f0 + lift - h * .5f, z0), new Vector3(x, f1 + lift - h * .5f, z1), w, h, m);
    }

    void Soffit(float z0, float z1, float s0, float s1)
    {
        if (z1 - z0 < 1e-3f) return;
        Tilted(new Vector3(0f, s0 + .08f, z0), new Vector3(0f, s1 + .08f, z1), 3f, .16f, soffit);
    }

    // Run! B's services under the soffit, parallel to it: main 0.10 at 2.62 (x +0.55), tray 0.30 at 2.70 (x -0.60), EMT on the
    // west wall at 2.71-2.81, the duct 0.61 x 0.30 at x 0.69..1.30 with its underside 2.45 over the floor.
    void Services(float z0, float z1, float f0, float f1)
    {
        if (z1 - z0 < 1e-3f) return;
        // Non-casting, as the game's overhead dressing (FrontRoomsRunDress: every overhead renderer ShadowCastingMode.Off): the
        // shadowed third of the strips banded both upper walls with their shadows (review SP5-F7).
        var keep = kit.Mesh.CastShadows;
        kit.Mesh.CastShadows = false;
        Tilted(new Vector3(.55f, f0 + 2.62f, z0), new Vector3(.55f, f1 + 2.62f, z1), .1f, .1f, red);
        Tilted(new Vector3(-.6f, f0 + 2.70f, z0), new Vector3(-.6f, f1 + 2.70f, z1), .3f, .06f, galv);
        Tilted(new Vector3(-1.39f, f0 + 2.76f, z0), new Vector3(-1.39f, f1 + 2.76f, z1), .025f, .025f, galv);
        Tilted(new Vector3(.995f, f0 + 2.60f, z0), new Vector3(.995f, f1 + 2.60f, z1), .61f, .30f, galv);
        kit.Mesh.CastShadows = keep;
    }

    // ---------------------------------------------------------------- the fire-door walls (FD1 z 9, FD2 z 18)

    void BuildFireWalls()
    {
        var bronze = FrontRoomsPoolMaterials.Surface("Prop_SteelBrown", new Color(.16f, .12f, .09f));
        var leaf = FrontRoomsPoolMaterials.Surface("Door_Enamel", new Color(.55f, .52f, .45f));
        foreach (var z in new[] { 9f, 18f })
        {
            var f = FloorAt(z - .05f);
            var s = f + Clear;
            kit.Shell(s, f);
            // 0.16 block on the cell line, a 2.0 x 2.1 frame centred, dark-bronze casing; up to the uphill soffit.
            kit.WallX(z, -1.5f, 1.5f, f - .25f, s + .2f, block, new List<FrontRoomsStructuresKit.Opening> { new FrontRoomsStructuresKit.Opening(-1f, 1f, f, f + 2.1f) }, .16f);
            kit.Box(-1.06f, f, z - .1f, -1f, f + 2.16f, z + .1f, bronze, false);
            kit.Box(1f, f, z - .1f, 1.06f, f + 2.16f, z + .1f, bronze, false);
            kit.Box(-1.06f, f + 2.1f, z - .1f, 1.06f, f + 2.16f, z + .1f, bronze, false);
            // Run! B's lower tone on both faces beside the frame (the game lays it on every face, end walls included;
            // review SP5-F15, M1).
            foreach (var face in new[] { z - .08f, z + .08f })
            {
                var out1 = face < z ? -1f : 1f;
                LowerBand(face, out1, -1.42f, -1.06f, f);
                LowerBand(face, out1, 1.06f, 1.42f, f);
            }
            // The leaves held open at 90 degrees on magnets, on the uphill landing (1.12 m clear: no leaf over the step), in a
            // non-casting renderer (colliders kept): a 172-degree strip's shadow of them combed FD1's south face (review SP5-F12,
            // as SP7-space-F10).
            var keep = kit.Mesh.CastShadows;
            kit.Mesh.CastShadows = false;
            kit.Box(-1.0f, f, z - 1.02f, -.95f, f + 2.08f, z - .06f, leaf);
            kit.Box(.95f, f, z - 1.02f, 1.0f, f + 2.08f, z - .06f, leaf);
            kit.Mesh.CastShadows = keep;
        }
    }

    // Run! B's lower tone, 1.2 over the floor, on a wall face in the plane z = face (facing out1 = -1 or +1), x0..x1.
    void LowerBand(float face, float out1, float x0, float x1, float floor)
    {
        var za = face - out1 * .001f;
        var zb = face + out1 * .011f;
        kit.Box(x0, floor, Mathf.Min(za, zb), x1, floor + 1.2f, Mathf.Max(za, zb), lower, false);
    }

    // ---------------------------------------------------------------- the riser shaft (D7, z 12..15; view only)

    void BuildShaft()
    {
        var f = FloorAt(13.5f);
        var top = 6.6f;
        kit.Shell(top, FloorAt(15f));
        // Block shaft walls on the cell lines from the soffit's cut edges to a lid at +6.60.
        var cut0 = FloorAt(12f) + Clear;
        var cut1 = FloorAt(15f) + Clear;
        kit.WallX(12f, -1.58f, 1.58f, cut0, top, block);
        kit.WallX(15f, -1.58f, 1.58f, cut1, top, block);
        kit.WallZ(-1.5f, 12f, 15f, Mathf.Min(cut0, cut1), top, block);
        kit.WallZ(1.5f, 12f, 15f, Mathf.Min(cut0, cut1), top, block);
        kit.Lid(-1.5f, 12f, 1.5f, 15f, top, soffit);
        // A sprinkler riser off the main, a ladder tray riser, two clamped pipes, a caged ladder on the north wall (bottom
        // rung 3.5 m over the floor below it, out of reach).
        kit.Box(.5f, cut0 - .4f, 13.1f, .6f, top, 13.2f, red, false);
        kit.Box(-.9f, cut0 - .3f, 12.1f, -.6f, top, 12.16f, galv, false);
        kit.Box(-1.3f, cut0, 13.0f, -1.24f, top, 13.06f, galv, false);
        kit.Box(-1.3f, cut0, 13.6f, -1.24f, top, 13.66f, galv, false);
        var ladder0 = f + 3.5f;
        kit.Box(-.25f, ladder0, 14.78f, -.21f, top, 14.84f, steel, false);
        kit.Box(.21f, ladder0, 14.78f, .25f, top, 14.84f, steel, false);
        for (var y = ladder0; y < top - .2f; y += .3f) kit.Box(-.25f, y, 14.79f, .25f, y + .025f, 14.82f, steel, false);
        for (var y = ladder0 + 1.0f; y < top - .2f; y += .9f) kit.Box(-.4f, y, 14.2f, .4f, y + .03f, 14.24f, steel, false);
        kit.StandIn("ladder cage (greybox)", new Vector3(0f, (ladder0 + 1f + top) * .5f, 14.3f), new Vector3(.8f, top - ladder0 - 1f, .02f), FrontRoomsPoolMaterials.ChainLink(), 0f, false);
        // An R-B strip and a battery head at the +3.90 floor line and under the lid.
        // x5: at 5.6 m over the floor a scale-1 strip gave the ramp under the shaft its dimmest stretch, not a pool (review SP5-F14).
        Strip(new Vector3(1.38f, 3.9f + .3f, 13.5f), true, 5f, 10f, true);
        Head(new Vector3(-1.38f, 3.9f + .4f, 13.5f), Vector3.right, false);
        Head(new Vector3(0f, top - .15f, 13.5f), Vector3.down, false);
    }

    // ---------------------------------------------------------------- the head: the janitor's closet and the E8 cage

    void BuildHead()
    {
        kit.Shell(Clear, 0f);
        // The head's west wall with the closet's door (LMS 1.0 x 2.1 at z 1.4..2.4), the closet (Low 2.4) beyond it.
        kit.WallZ(-1.5f, 0f, 3f, 0f, Clear, upper, new List<FrontRoomsStructuresKit.Opening> { new FrontRoomsStructuresKit.Opening(1.4f, 2.4f, 0f, 2.1f) });
        kit.Prism(new Vector3(-1.415f, 0f, 0f), new Vector3(-1.415f, 0f, 1.4f), 0f, 1.2f, .012f, lower, false);
        kit.Prism(new Vector3(-1.415f, 0f, 2.4f), new Vector3(-1.415f, 0f, 3f), 0f, 1.2f, .012f, lower, false);
        // The entry wall on z 0 with Run! B's arch (x -0.7..0.7, head 2.2), the way in from the hub.
        kit.WallX(0f, -1.58f, 1.58f, 0f, Clear, upper, new List<FrontRoomsStructuresKit.Opening> { new FrontRoomsStructuresKit.Opening(-.7f, .7f, 0f, 2.2f) });
        kit.Slab(-1.5f, -3f, 1.5f, 0f, 0f, floorMat, .25f);   // the vestibule a whole cell deep (review SP5-F18)
        kit.WallZ(-1.5f, -3f, 0f, 0f, Clear, upper);
        kit.WallZ(1.5f, -3f, 0f, 0f, Clear, upper);
        kit.WallX(-3f, -1.58f, 1.58f, 0f, Clear, upper);
        kit.Lid(-1.5f, -3f, 1.5f, 0f, Clear, soffit);
        // The closet: x -4.5..-1.5, z 0..3, Low 2.4, its door propped open on a wedge, lit, enterable; the janitor's cart inside.
        kit.Shell(2.4f, 0f);
        kit.Slab(-4.5f, 0f, -1.5f, 3f, 0f, floorMat, .25f);
        kit.WallZ(-4.5f, -.08f, 3.08f, 0f, 2.4f, upper);
        kit.WallX(0f, -4.5f, -1.5f, 0f, 2.4f, upper);
        kit.WallX(3f, -4.5f, -1.5f, 0f, 2.4f, upper);
        kit.Lid(-4.5f, 0f, -1.5f, 3f, 2.4f, soffit);
        kit.Box(-2.45f, 0f, 2.38f, -1.58f, 2.08f, 2.42f, FrontRoomsPoolMaterials.Surface("Door_Enamel", new Color(.55f, .52f, .45f)));
        kit.StandIn("door wedge", new Vector3(-2.4f, .02f, 2.3f), new Vector3(.08f, .04f, .14f), FrontRoomsPoolMaterials.Lit("rubber wedge", new Color(.15f, .15f, .15f)), 0f, false);
        kit.Kit("Kit_JanitorCart", new Vector3(-3.6f, 0f, 1.2f), 90f, new Vector3(1.1f, 1.0f, .5f));
        kit.Lamp("closet", LightType.Spot, new Vector3(-3f, 2.35f, 1.5f), Vector3.down, 150f, 110f, 2.0f, 5f, RunTint, false, new Vector3(.18f, 1.22f, .08f), FrontRoomsPoolMaterials.Surface("Painted_Metal", new Color(.85f, .85f, .82f)), RunTint * 1.4f);
        // The E8 cage: chain-link from floor to soffit on x 1.5, posts at z 0, 1.5, 3.0, a padlocked mesh gate; shelving,
        // cartons and Run! B's chair dolly inside; a second Relay stand-in inside for 62 Q1 (sight crosses, bodies never).
        kit.Shell(Clear, 0f);
        kit.Slab(1.5f, 0f, 4.5f, 3f, 0f, floorMat, .25f);
        kit.WallZ(4.5f, -.08f, 3.08f, 0f, Clear, upper);
        kit.WallX(0f, 1.5f, 4.5f, 0f, Clear, upper);
        kit.WallX(3f, 1.5f, 4.5f, 0f, Clear, upper);
        kit.Lid(1.5f, 0f, 4.5f, 3f, Clear, soffit);
        kit.Guard(new Vector3(1.5f, 0f, 0f), new Vector3(1.5f, 0f, 3f), 0f, FrontRoomsStructuresKit.GuardStyle.Mesh, null, null, Clear - .02f);
        kit.Box(1.47f, 0f, .9f, 1.53f, 2.0f, .95f, steel, false);
        kit.Box(1.47f, 1.0f, .85f, 1.53f, 1.1f, .9f, FrontRoomsPoolMaterials.Surface("Prop_Brass", new Color(.7f, .55f, .25f)), false);
        kit.Kit("Kit_PlyCabinet", new Vector3(3.9f, 0f, 2.6f), 180f, new Vector3(1.2f, 1.8f, .45f));
        kit.Kit("Kit_Crate", new Vector3(3.6f, 0f, .6f), 10f, new Vector3(.6f, .5f, .5f));
        kit.Kit("Kit_Crate", new Vector3(4.0f, 0f, 1.2f), -6f, new Vector3(.6f, .5f, .5f));
        kit.Kit("Kit_ChairDolly8", new Vector3(2.6f, 0f, 2.2f), 30f, new Vector3(.8f, 1.4f, .8f));
        kit.Relay(new Vector3(2.6f, 0f, 1.0f), new Vector3(0f, 0f, 1.5f));
        // The pull station and bell on the west wall at z 0.6 (Run! B's, moved there by 70).
        kit.Kit("Kit_PullStation", new Vector3(-1.42f, 1.2f, .6f), 90f, new Vector3(.03f, .15f, .1f), null, false);
        kit.Kit("Kit_FireBell", new Vector3(-1.42f, 2.3f, .6f), 90f, new Vector3(.08f, .15f, .15f), null, false);
    }

    // ---------------------------------------------------------------- the goal door (z 27): RN-F, never locked, working (E)

    void BuildGoalDoor()
    {
        kit.Shell(Dock + Clear, Dock);
        kit.WallX(27f, -1.58f, 7.58f, Dock - .25f, DockCeil + .16f, block, new List<FrontRoomsStructuresKit.Opening> { new FrontRoomsStructuresKit.Opening(-.5f, .5f, Dock, Dock + 2.1f) });
        var frame = FrontRoomsPoolMaterials.Surface("Prop_SteelBrown", new Color(.16f, .12f, .09f));
        kit.Box(-.56f, Dock, 26.9f, -.5f, Dock + 2.16f, 27.1f, frame, false);
        kit.Box(.5f, Dock, 26.9f, .56f, Dock + 2.16f, 27.1f, frame, false);
        kit.Box(-.56f, Dock + 2.1f, 26.9f, .56f, Dock + 2.16f, 27.1f, frame, false);
        LowerBand(26.92f, -1f, -1.42f, -.56f, Dock);
        LowerBand(26.92f, -1f, .56f, 1.42f, Dock);
        LowerBand(27.08f, 1f, -1.42f, -.56f, Dock);
        LowerBand(27.08f, 1f, .56f, 7.42f, Dock);
        // The leaf: hinged at x -0.5, swinging into the dock; a 15 x 15 in lite (F5) lit from behind; a panic bar on the corridor face.
        var hinge = new GameObject("Goal door leaf (E opens)");
        hinge.transform.SetParent(Root, false);
        hinge.transform.localPosition = new Vector3(-.5f, Dock, 27f);
        doorLeaf = hinge.transform;
        var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slab.name = "leaf";
        slab.transform.SetParent(hinge.transform, false);
        slab.transform.localPosition = new Vector3(.5f, 1.05f, 0f);
        slab.transform.localScale = new Vector3(.98f, 2.08f, .045f);
        slab.GetComponent<Renderer>().sharedMaterial = FrontRoomsPoolMaterials.Surface("Door_Enamel", new Color(.55f, .52f, .45f));
        doorCollider = slab.GetComponent<Collider>();
        var lite = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lite.name = "lite";
        UnityEngine.Object.Destroy(lite.GetComponent<Collider>());
        lite.transform.SetParent(hinge.transform, false);
        lite.transform.localPosition = new Vector3(.5f, 1.384f, 0f);
        lite.transform.localScale = new Vector3(.381f, .381f, .05f);
        // Armed, only the lite can lead (the heads are dark): bright enough to bloom past the end wall (review SP5-F1).
        lite.GetComponent<Renderer>().sharedMaterial = FrontRoomsPoolMaterials.Glow("SP5 goal lite", new Color(3.2f, 2.95f, 2.5f));
        var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = "panic bar";
        UnityEngine.Object.Destroy(bar.GetComponent<Collider>());
        bar.transform.SetParent(hinge.transform, false);
        bar.transform.localPosition = new Vector3(.5f, 1.0f, -.06f);
        bar.transform.localScale = new Vector3(.8f, .05f, .05f);
        bar.GetComponent<Renderer>().sharedMaterial = steel;
        // No realtime light behind the door: the lite is emissive (as SP7's C42), and an unshadowed spot here lit run 3's floor
        // through the wall, brighter than the door it was meant to show (review SP5-F1, F4).
    }

    // ---------------------------------------------------------------- T_RN_DockRelease (x -1.5..7.5, z 27..39)

    void BuildDock()
    {
        // Platform -3.00 (z 27..33, 6.0 deep), bay -4.20 (z 33..39), Tall 5.4 over the bay (ceiling +1.20, RunB_Soffit).
        kit.Shell(DockCeil, Dock);
        kit.Box(-1.5f, Bay - .25f, 27f, 7.5f, Dock, 33f, floorMat);
        kit.Shell(DockCeil, Bay);
        kit.Slab(-1.5f, 33f, 7.5f, 39f, Bay, concrete, .25f);
        kit.WallZ(-1.5f, 27f, 39.08f, Bay, DockCeil + .16f, block);
        kit.WallZ(7.5f, 27f, 39.08f, Bay, DockCeil + .16f, block);
        kit.Lid(-1.5f, 27f, 7.5f, 39f, DockCeil, soffit);
        // The paint line 1.2 over each floor.
        kit.Prism(new Vector3(-1.415f, 0f, 27f), new Vector3(-1.415f, 0f, 33f), Dock, Dock + 1.2f, .012f, lower, false);
        kit.Prism(new Vector3(7.415f, 0f, 27f), new Vector3(7.415f, 0f, 33f), Dock, Dock + 1.2f, .012f, lower, false);
        kit.Prism(new Vector3(-1.415f, 0f, 33f), new Vector3(-1.415f, 0f, 39f), Bay, Bay + 1.2f, .012f, lower, false);
        kit.Prism(new Vector3(7.415f, 0f, 33f), new Vector3(7.415f, 0f, 39f), Bay, Bay + 1.2f, .012f, lower, false);
        // The dock face (LEDGE): concrete with a steel angle on its edge.
        kit.Box(-1.5f, Bay, 33f, 7.5f, Dock - .06f, 33.02f, concrete, false);
        kit.Box(-1.5f, Dock - .06f, 32.94f, 7.5f, Dock + .005f, 33.02f, galv, false);
        // The north wall with the 12 x 14 ft overhead door (3.66 x 4.27) centred on x 4.5, half up: its bar 1.2 over the bay.
        kit.WallX(39f, -1.58f, 7.58f, Bay, DockCeil + .16f, block, new List<FrontRoomsStructuresKit.Opening> { new FrontRoomsStructuresKit.Opening(2.67f, 6.33f, Bay, Bay + 4.27f) });
        LowerBand(38.92f, -1f, -1.42f, 2.67f, Bay);
        LowerBand(38.92f, -1f, 6.33f, 7.42f, Bay);
        kit.StandIn("overhead coiling door, half up (greybox)", new Vector3(4.5f, (Bay + 1.2f + Bay + 4.27f) * .5f, 39.0f), new Vector3(3.66f, 3.07f, .06f), galv);
        kit.StandIn("door coil hood (greybox)", new Vector3(4.5f, Bay + 4.5f, 38.8f), new Vector3(3.9f, .45f, .45f), galv, 0f, false);
        kit.StandIn("bollard", new Vector3(2.45f, Bay + .55f, 38.7f), new Vector3(.17f, 1.1f, .17f), FrontRoomsPoolMaterials.Surface("RunB_BucketYellow", new Color(.85f, .7f, .1f)));
        kit.StandIn("bollard", new Vector3(6.55f, Bay + .55f, 38.7f), new Vector3(.17f, 1.1f, .17f), FrontRoomsPoolMaterials.Surface("RunB_BucketYellow", new Color(.85f, .7f, .1f)));
        kit.StandIn("CCTV camera (greybox)", new Vector3(4.5f, Bay + 4.9f, 38.75f), new Vector3(.12f, .12f, .3f), FrontRoomsPoolMaterials.Lit("cctv beige", new Color(.7f, .68f, .6f)), 0f, false);
        // The leveler flush and stored on the door's axis, a bumper each side, the dock light on its arm at the west side.
        kit.StandIn("dock leveler (greybox)", new Vector3(4.5f, Dock - .02f, 31.78f), new Vector3(1.82f, .04f, 2.44f), galv, 0f, false);
        kit.StandIn("dock bumper (greybox)", new Vector3(3.3f, Dock - .45f, 33.1f), new Vector3(.25f, .5f, .2f), FrontRoomsPoolMaterials.Lit("rubber black", new Color(.08f, .08f, .08f)));
        kit.StandIn("dock bumper (greybox)", new Vector3(5.7f, Dock - .45f, 33.1f), new Vector3(.25f, .5f, .2f), FrontRoomsPoolMaterials.Lit("rubber black", new Color(.08f, .08f, .08f)));
        kit.StandIn("dock light arm (greybox)", new Vector3(3.0f, Dock + .9f, 32.8f), new Vector3(.08f, 1.8f, .08f), steel);
        // The chain guard (stanchions 1.07 with chain spans of 1.8 or less) from the west landing to the east flight, a 1.2
        // swing gate at the leveler (63 Q2's recommendation, asked on the sign).
        Chain(-.22f, 3.9f);
        Chain(5.1f, 6.22f);
        // The swing gate as a gate (review SP5-M3: a single knee bar read as a rail): two stiles, a top and a mid rail.
        var gateMat = FrontRoomsPoolMaterials.Surface("RunB_BucketYellow", new Color(.85f, .7f, .1f));
        kit.StandIn("swing gate stile (greybox)", new Vector3(3.93f, Dock + .535f, 32.9f), new Vector3(.05f, 1.07f, .04f), gateMat, 0f, false);
        kit.StandIn("swing gate stile (greybox)", new Vector3(5.07f, Dock + .535f, 32.9f), new Vector3(.05f, 1.07f, .04f), gateMat, 0f, false);
        kit.StandIn("swing gate top rail (greybox)", new Vector3(4.5f, Dock + 1.045f, 32.9f), new Vector3(1.2f, .05f, .04f), gateMat, 0f, false);
        kit.StandIn("swing gate mid rail (greybox)", new Vector3(4.5f, Dock + .55f, 32.9f), new Vector3(1.14f, .04f, .03f), gateMat, 0f, false);
        kit.Mesh.Collider(new Vector3(4.5f, Dock + .53f, 32.9f), new Vector3(1.2f, 1.07f, .06f));
        // B2 at the east end: 8 risers 0.15 / 0.30 from the platform down to the bay against the east wall, a rail on the open side.
        kit.Shell(DockCeil, Bay);
        kit.StepRun(new Vector3(6.82f, 0f, 33f), Vector3.back, 1.2f, Bay, Dock, floorMat, cove);
        RailSloped(new Vector3(6.18f, Dock, 33f), new Vector3(6.18f, Bay + .15f, 35.1f));
        kit.StandIn("bollard", new Vector3(5.9f, Bay + .55f, 35.7f), new Vector3(.17f, 1.1f, .17f), FrontRoomsPoolMaterials.Surface("RunB_BucketYellow", new Color(.85f, .7f, .1f)));
        // The second way: a 1.2 x 1.2 landing off the platform's west end, then 8 risers going east along the dock face.
        kit.Box(-1.42f, Bay, 33f, -.22f, Dock, 34.2f, floorMat);
        kit.StepRun(new Vector3(-.22f, 0f, 33.6f), Vector3.left, 1.2f, Bay, Dock, floorMat, cove);
        kit.Guard(new Vector3(-1.42f, 0f, 34.16f), new Vector3(-.22f, 0f, 34.16f), Dock, FrontRoomsStructuresKit.GuardStyle.Rail);
        RailSloped(new Vector3(-.22f, Dock, 34.24f), new Vector3(1.88f, Bay + .15f, 34.24f));
        // The bay's kit (greybox): a platform truck, a front-load dumpster on the west wall; the platform's loads on its south
        // and west walls (plan F): shelving, a pallet stack, the receiving desk, Run! B's hand truck.
        kit.StandIn("platform truck (greybox)", new Vector3(2.0f, Bay + .2f, 36.5f), new Vector3(.75f, .4f, 1.5f), galv);
        kit.StandIn("front-load dumpster (greybox)", new Vector3(-.6f, Bay + .65f, 37.2f), new Vector3(1.6f, 1.3f, 1.9f), FrontRoomsPoolMaterials.Lit("dumpster green", new Color(.15f, .3f, .2f), .3f));
        kit.Kit("Kit_PlyCabinet", new Vector3(6.6f, Dock, 27.5f), 0f, new Vector3(1.2f, 1.8f, .45f));
        kit.Kit("Kit_Pallet", new Vector3(-.6f, Dock, 29.0f), 90f, new Vector3(1.0f, .15f, 1.2f));
        kit.Kit("Kit_Crate", new Vector3(-.6f, Dock + .15f, 29.0f), 0f, new Vector3(.6f, .5f, .5f));
        kit.Kit("Kit_StationCounter_Run120", new Vector3(-.9f, Dock, 31.4f), 90f, new Vector3(.7f, 1.0f, 1.2f));
        kit.Kit("Kit_HandTruck_Cartons", new Vector3(5.6f, Dock, 28.0f), 200f, new Vector3(.6f, 1.3f, .6f));
    }

    void Chain(float x0, float x1)
    {
        var count = Mathf.Max(1, Mathf.CeilToInt((x1 - x0) / 1.8f));
        var chain = FrontRoomsPoolMaterials.Surface("Prop_ChainOxide", new Color(.4f, .35f, .3f));
        for (var k = 0; k <= count; k++)
        {
            var x = Mathf.Lerp(x0, x1, k / (float)count);
            kit.Box(x - .025f, Dock, 32.88f, x + .025f, Dock + 1.07f, 32.93f, FrontRoomsPoolMaterials.Surface("RunB_BucketYellow", new Color(.85f, .7f, .1f)), false);
            if (k == count) break;
            var xn = Mathf.Lerp(x0, x1, (k + 1) / (float)count);
            for (var s = 0; s < 8; s++)
            {
                var a = Mathf.Lerp(x, xn, s / 8f);
                var b = Mathf.Lerp(x, xn, (s + 1) / 8f);
                var sa = .12f * Mathf.Sin(Mathf.PI * s / 8f);
                var sb = .12f * Mathf.Sin(Mathf.PI * (s + 1) / 8f);
                Tilted(new Vector3(a, Dock + 1.0f - sa, 32.905f), new Vector3(b, Dock + 1.0f - sb, 32.905f), .02f, .024f, chain);
            }
        }
        kit.Mesh.Collider(new Vector3((x0 + x1) * .5f, Dock + .53f, 32.9f), new Vector3(x1 - x0, 1.07f, .06f));
    }

    // A rail on a slope (nosing-line points): posts every 1.2, top and mid rails, one collider.
    void RailSloped(Vector3 a, Vector3 b)
    {
        var d = b - a;
        var len = d.magnitude;
        if (len < .05f) return;
        var flat = new Vector3(d.x, 0f, d.z).normalized;
        var count = Mathf.Max(1, Mathf.CeilToInt(new Vector3(d.x, 0f, d.z).magnitude / 1.2f));
        var metal = FrontRoomsPoolMaterials.Surface("Painted_Metal", new Color(.42f, .41f, .38f));
        for (var k = 0; k <= count; k++)
        {
            var q = Vector3.Lerp(a, b, k / (float)count);
            kit.Prism(new Vector3(q.x, 0f, q.z) - flat * .02f, new Vector3(q.x, 0f, q.z) + flat * .02f, q.y, q.y + FrontRoomsStructuresKit.GuardHeight, .04f, metal, false);
        }
        var rot = Quaternion.LookRotation(d / len, Vector3.up);
        var mid = (a + b) * .5f;
        kit.Mesh.Box(mid + Vector3.up * (FrontRoomsStructuresKit.GuardHeight - .025f), new Vector3(.05f, .05f, len + .04f), rot, metal, false);
        kit.Mesh.Box(mid + Vector3.up * .53f, new Vector3(.03f, .03f, len), rot, metal, false);
        kit.Mesh.Collider(mid + Vector3.up * (FrontRoomsStructuresKit.GuardHeight * .5f), new Vector3(.05f, FrontRoomsStructuresKit.GuardHeight, len), rot);
    }

    // ---------------------------------------------------------------- outside: the ramp to the night street (greybox)

    void BuildOutside()
    {
        // x 1.5..7.5, z 39..45, rising north at 1:8 to -3.45 between block retaining walls; unroofed; the street out of sight.
        kit.Shell(4f, Bay);
        kit.Ramp(new Vector3(4.5f, Bay, 39f), Vector3.forward, 6f, 6f, .75f, concrete, .3f);
        kit.WallZ(1.5f, 39f, 45f, Bay - .3f, 1.2f, concrete, null, .3f);
        kit.WallZ(7.5f, 39f, 45f, Bay - .3f, 1.2f, concrete, null, .3f);
        kit.WallX(45f, 1.35f, 7.65f, -3.75f, 1.2f, concrete, null, .3f);
        // Two high-pressure sodium heads (2100 K) on the walls at z 43.5, 4.0 m over the ramp, aimed at it and the door; on in
        // both states: the slot and its orange spill on the bay floor are the brightest thing in the dock (63 Q3).
        // Both heads always cast their shadows while on (review SP5-F4): unshadowed, the west head washed the dock's east wall and
        // the bay floor through the goal wall; shadowed, the slot itself cuts the spill to the trapezoid on the bay floor.
        foreach (var x in new[] { 1.75f, 7.25f })
        {
            // 40 (was 14): shadowed, the slot under the half-raised door is the only way the light reaches the dock, so it must
            // clip to lead the x1.6-strip dock walls (L 208) (review SP5-F2, by eye).
            var head = kit.Lamp("high-pressure sodium", LightType.Spot, new Vector3(x, -3.64f + 4f, 43.5f), new Vector3(4.5f - x, -3.2f, -3.5f), 90f, 60f, 40f, 16f, Sodium, true,
                new Vector3(.4f, .25f, .35f), FrontRoomsPoolMaterials.Surface("RunB_GalvSteel", new Color(.6f, .62f, .63f)), Sodium * 6f);
            if (kit.Host != null && kit.Host.Look != null) kit.Host.Look.Shadowed(head);
        }
    }

    // ---------------------------------------------------------------- lamps: armed (R-B strips, the dock light) and tripped (heads)

    void BuildLamps()
    {
        // R-B strips on the soffit at z 1.8 + 3k, none at 13.8 under the shaft (the stutter tube moves to 10.8).
        for (var k = 0; k < 9; k++)
        {
            var z = 1.8f + 3f * k;
            if (Mathf.Abs(z - 13.8f) < .1f) continue;
            var s = SoffitAt(z);
            // Pitched with the soffit over its 1.22 m (atan 1/8 on a run, half that at a kink; review SP5-F17).
            var pitch = Mathf.Atan((SoffitAt(z - .61f) - SoffitAt(z + .61f)) / 1.22f) * Mathf.Rad2Deg;
            Strip(new Vector3(0f, s - .02f, z), false, Mathf.Abs(z - 10.8f) < .1f ? .78f : 1f, 10f, true, pitch);
        }
        // The dock: one R-B strip per cell at x1.6, and the dock light. The dock's strips light only while the walker is in the
        // dock or the goal door is open (review SP5-F3: every row reached landing C through the goal wall, unshadowed); the
        // north row casts its shadows, so the overhead door's wall stops lighting the outside ramp white (review SP5-F2).
        for (var i = 0; i < 3; i++)
            for (var j = 0; j < 4; j++)
            {
                var ds = Strip(new Vector3(-1.5f + 3f * i + 1.5f, DockCeil - .02f, 27f + 3f * j + 1.5f), false, 1.6f, 14f);
                dockStrips.Add(ds);
                if (j == 3 && kit.Host != null && kit.Host.Look != null) kit.Host.Look.Shadowed(ds);
            }
        var dl = kit.Lamp("dock light", LightType.Spot, new Vector3(3.1f, Dock + 1.75f, 32.7f), new Vector3(.4f, -.3f, 1f), 60f, 40f, 4f, 10f, new Color(1f, .95f, .85f), true,
            new Vector3(.3f, .3f, .25f), steel, new Color(1.8f, 1.7f, 1.5f));
        armed.Add(dl);
        // Tripped heads (battery, 2.45 over the floor): T-A twin heads at the ends (z 3.3 east; over the goal door aimed back),
        // T-B remote heads between (z 9 west on FD1's downhill face, z 15 east, z 21 west); the dock's on its south wall and at
        // each flight's foot. A head lights each B1 (R8). No EXIT sign anywhere in the room (C41).
        // Review SP5-F5: the east-wall heads hang under the duct at 2.20 with the cone kept under it (they sat 1 cm inside its
        // edge at 2.45 and lit it point-blank); the z 21 head aims back at FD2's B1; the dock's foot heads aim at their flights.
        // Review SP5-F1: the goal unit on the east wall 1.2 m out from the door, aimed at the leaf (as the rig's and SP7's goal
        // unit faces the door; over the door aimed back it lit the floor and left the leaf 64-80 deg off its axis).
        Head(new Vector3(1.38f, FloorAt(3.3f) + 2.20f, 3.3f), new Vector3(-1f, -.45f, 0f), true);
        Head(new Vector3(1.38f, -3f + 2.45f, 25.8f), new Vector3(-1.1f, -.2f, 1f), true);
        Head(new Vector3(-1.38f, FloorAt(9.2f) + 2.45f, 9.2f), Vector3.right, false);
        Head(new Vector3(1.38f, FloorAt(15f) + 2.20f, 15f), new Vector3(-1f, -.45f, 0f), false);
        Head(new Vector3(-1.38f, FloorAt(21f) + 2.45f, 21f), new Vector3(.6f, 0f, -1f), false);
        Head(new Vector3(1.5f, Dock + 2.45f, 27.15f), Vector3.forward, true);
        Head(new Vector3(4.5f, Dock + 2.45f, 27.15f), Vector3.forward, true);
        Head(new Vector3(7.35f, Bay + 2.45f, 35.6f), new Vector3(0f, -.6f, -1f), false);
        Head(new Vector3(2.2f, Bay + 2.45f, 34.3f), new Vector3(-1f, -.6f, -.2f), false);
    }

    static float SoffitAt(float z)
    {
        var f = FloorAt(z) + Clear;
        if (z >= 9f && z <= 9.6f) return 2.0f;
        if (z >= 18f && z <= 18.6f) return .8f;
        return f;
    }

    // An R-B strip (the game's strip kit: bare two tubes in a wire guard, its lens 2.6) with Run! B's wrap light under it.
    Light Strip(Vector3 at, bool alongZ, float scale, float range = 10f, bool view = false, float pitch = 0f)
    {
        var frame = new GameObject("R-B strip");
        frame.transform.SetParent(Root, false);
        frame.transform.localPosition = at;
        frame.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        MeshRenderer lens = null, redTube = null;
        try
        {
            // The game's strip; since 11-04 its middle tube is red-coated (the kit's second renderer), driven as the rig drives it.
            if (FrontRoomsLampKit.Available(FrontRoomsLampKit.Family.Strip))
                lens = FrontRoomsLampKit.Build(FrontRoomsLampKit.Family.Strip, frame.transform, Vector3.zero, Quaternion.identity, "SP5 strip tubes", false, out redTube);
        }
        catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("SP5 strip", "the R-B strip kit failed: " + e.Message); lens = null; redTube = null; }
        if (redTube != null) redTubes.Add(redTube);
        if (lens == null)
        {
            FrontRoomsPoolMaterials.LogOnce("SP5 strip missing", "the R-B strip kit is missing: a flat stand-in lens is used.");
            var flat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flat.name = "STAND-IN strip lens";
            UnityEngine.Object.Destroy(flat.GetComponent<Collider>());
            flat.transform.SetParent(frame.transform, false);
            flat.transform.localPosition = new Vector3(0f, -.05f, 0f);
            flat.transform.localScale = new Vector3(.12f, .04f, 1.22f);
            lens = flat.GetComponent<MeshRenderer>();
            lens.sharedMaterial = FrontRoomsPoolMaterials.Glow("SP5 strip lens", RunTint * 2.6f);
        }
        var lightGo = new GameObject("light");
        lightGo.transform.SetParent(frame.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, -.092f, 0f);
        lightGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var l = lightGo.AddComponent<Light>();
        l.type = LightType.Spot;
        l.spotAngle = 172f;
        l.innerSpotAngle = 124f;
        l.color = RunTint;
        l.intensity = 5.5f * scale;
        l.range = range;
        l.shadows = LightShadows.None;
        if (kit.Host != null && kit.Host.Look != null) kit.Host.Look.AddLamp(Key, l, lens, RunTint * 2.6f, view);
        armed.Add(l);
        armedLens.Add(lens);
        return l;
    }

    // A battery head (T-A twin head from the game's kit, or a T-B remote head): a flood in Run! B's warm colour, a red AC ON
    // pilot while armed; lit only when tripped.
    void Head(Vector3 at, Vector3 aim, bool twin)
    {
        GameObject body = null;
        if (twin) body = kit.Kit("Kit_EmergencyTwinHead", at - Vector3.up * .1f, Mathf.Atan2(aim.x, aim.z) * Mathf.Rad2Deg, new Vector3(.3f, .2f, .12f), FrontRoomsPoolMaterials.Lit("head beige", new Color(.85f, .82f, .74f)), false);
        else body = kit.StandIn("T-B remote head (greybox)", at, new Vector3(.18f, .12f, .12f), FrontRoomsPoolMaterials.Lit("head beige", new Color(.85f, .82f, .74f)), 0f, false);
        var lensGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        lensGo.name = "head lens";
        UnityEngine.Object.Destroy(lensGo.GetComponent<Collider>());
        lensGo.transform.SetParent(Root, false);
        var dir = (aim.normalized + Vector3.down * .55f).normalized;
        lensGo.transform.localPosition = at + dir * .08f;
        lensGo.transform.localRotation = Quaternion.LookRotation(-dir, Vector3.up);
        lensGo.transform.localScale = new Vector3(.1f, .1f, 1f);
        var lens = lensGo.GetComponent<MeshRenderer>();
        lens.sharedMaterial = lensOff;
        headLens.Add(lens);
        var pilot = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pilot.name = "AC ON pilot";
        UnityEngine.Object.Destroy(pilot.GetComponent<Collider>());
        pilot.transform.SetParent(Root, false);
        pilot.transform.localPosition = at + Vector3.down * .09f + aim.normalized * .07f;
        pilot.transform.localScale = new Vector3(.015f, .015f, .015f);
        var pr = pilot.GetComponent<MeshRenderer>();
        pr.sharedMaterial = pilotOn;
        pilots.Add(pr);
        var l = kit.Lamp(twin ? "T-A twin head" : "T-B remote head", LightType.Spot, at + dir * .1f, dir, 100f, 96f, 10f, 11f, Flood, true, Vector3.zero, null, Color.black);
        heads.Add(l);
    }

    // ---------------------------------------------------------------- signs (70 section 7, verbatim) and marks

    void BuildSigns()
    {
        // The arrival's sign spot is inside the corridor's east wall: on the wall, to the right, out of the main view.
        kit.Sign(new Vector3(1.36f, FloorAt(4f) + 1.5f, 4f), new Vector3(0f, FloorAt(4f) + 1.5f, 4f), "SP5 · Run!：一路下坡到卸货间",
            "游戏里的 Run!：设备走廊和它尽头的目标门。走廊 27 米：先平走一格，入口一边是铁丝网储物笼，一边是开着门的杂物间；后 24 米是 1:8 的坡道，一共下降 3 米；每 9 米一道防火门墙，过了门框再下两级小台阶（共 0.3 米）。坡道中段头顶开着一口设备竖井，里面的爬梯够不着。走廊尽头那扇门后面是 5.4 米高的卸货间：平台比卡车位高 1.2 米，边上拉着链条护栏；卷帘门只拉起一半，门下露出通往夜里街道的坡道，钠灯照得橘黄，是这里最亮的东西。按 T 停电，停电后只剩应急灯，再按一次恢复。\n"
            + "灰盒占位：铁丝网储物笼；竖井里的管线和爬梯；卸货间的混凝土地面和平台边；门外的坡道和钠灯；卷帘门、卸货调节板、链条护栏、风管、消防水带箱、打卡钟这些还没做的新道具。黑色人形是 Relay 的静态替身（2.05 米），只用来检查视线，不会动。\n"
            + "要你决定：1 走廊两侧的小房间能进还是只能看（63 Q1）？入口左边的杂物间开着门，可以走进去试。 2 卷帘门下面露出什么（63 Q3）？现在放的是钠灯下通往街道的坡道。 3 装卸平台边：真实的开口边，还是现在这种可拆的链条护栏加摆门（63 Q2）？ 4 储物笼里站着一个替身：铁丝网、龙骨架这类看得穿的隔断，Relay 应该能隔着看见你，还是像墙一样挡住（62 Q1）？ 5 竖井、储物笼、卸货间，各自放进游戏吗？（坡道什么时候出现、多长，你已经同意了：71 的决定 2，第 3 档起出现，1:8，最长 24 米，最多下降 3 米；它和别的新结构一样，你走过、说可以，才进游戏。）", FloorAt(4f));
        // The Relay stand-in on run 2 (Run! B's proxy spot), facing north.
        kit.Relay(new Vector3(-.8f, FloorAt(11.4f), 11.4f), new Vector3(-.8f, FloorAt(11.4f), 20f));
        kit.Mark(new Vector3(1.3f, FloorAt(6f) + 1.4f, 6f), Vector3.left, "坡道 1:8 · 24 米降 3.0");
        kit.Mark(new Vector3(1.3f, FloorAt(9.45f) + 1.9f, 9.45f), Vector3.left, "两级 0.15 · 门框 2.0 × 2.1");
        kit.Mark(new Vector3(0f, FloorAt(13.5f) + 3.2f, 13.5f), Vector3.back, "竖井到 +6.60 · 爬梯离地 3.5");
        kit.Mark(new Vector3(1.5f, Dock + 1.4f, 32.8f), Vector3.back, "平台 −3.00 · 卡车位 −4.20 · 高差 1.20");
    }

    // ---------------------------------------------------------------- the frame: trip key, the door

    public override FrontRoomsStructuresLook.Preset PresetAt(Vector3 world) => tripped ? FrontRoomsStructuresLook.Preset.RunBTripped : FrontRoomsStructuresLook.Preset.RunB;

    public override bool KeyPressed(FrontRoomsStructures host, Key key, out string message)
    {
        message = null;
        if (key != UnityEngine.InputSystem.Key.T && key != UnityEngine.InputSystem.Key.L) return false;
        SetTripped(!tripped);
        message = tripped ? "停电：只剩应急灯。" : "恢复供电。";
        return true;
    }

    void SetTripped(bool on)
    {
        tripped = on;
        var look = kit != null && kit.Host != null ? kit.Host.Look : null;
        if (block2 == null) block2 = new MaterialPropertyBlock();
        for (var k = 0; k < armed.Count; k++)
        {
            if (armed[k] != null && look != null) look.Scale(armed[k], on || (!dockLit && dockStrips.Contains(armed[k])) ? 0f : 1f);
            if (k < armedLens.Count && armedLens[k] != null)
            {
                armedLens[k].GetPropertyBlock(block2);
                block2.SetColor("_EmissionColor", on ? Color.black : RunTint * 2.6f);
                armedLens[k].SetPropertyBlock(block2);
            }
        }
        foreach (var l in heads) if (l != null && look != null) look.Scale(l, on ? 1f : 0f);
        // The red tubes (the rig, 11-04): armed their coating shows the lit white tubes (redTubeBounce); tripped they burn on the
        // battery circuit with the heads (redTubeLinear).
        var tube = on ? rigNumbers.redTubeLinear.gamma : rigNumbers.redTubeBounce;
        foreach (var r in redTubes)
        {
            if (r == null) continue;
            r.GetPropertyBlock(block2);
            block2.SetColor("_EmissionColor", tube);
            r.SetPropertyBlock(block2);
        }
        foreach (var r in headLens) if (r != null) r.sharedMaterial = on ? headOn : lensOff;
        foreach (var r in pilots) if (r != null) r.sharedMaterial = on ? pilotOff : pilotOn;
        if (look != null && look.Current != FrontRoomsStructuresLook.Preset.Game) look.Want(PresetAt(Site));
    }

    // The dock's strips: on while the walker is past the goal wall or the goal door stands open (review SP5-F3).
    public override void Tick(FrontRoomsStructures host, float dt, bool isCurrent)
    {
        if (!isCurrent || host == null || host.Walker == null || host.Look == null) return;
        var feet = host.Walker.transform.position - Site;
        var want = doorOpen || feet.z > 26.9f;
        if (want == dockLit) return;
        dockLit = want;
        if (tripped) return;
        foreach (var l in dockStrips) if (l != null) host.Look.Scale(l, want ? 1f : 0f);
    }

    public override string Describe(Collider c, out bool hold)
    {
        hold = false;
        if (c != null && c == doorCollider) return doorOpen ? "E  ·  关门" : "E  ·  开门（推杠）";
        return null;
    }

    public override void Use(Collider c)
    {
        if (c == null || c != doorCollider || doorLeaf == null) return;
        doorOpen = !doorOpen;
        doorLeaf.localRotation = Quaternion.Euler(0f, doorOpen ? -95f : 0f, 0f);
    }

    public override void Leave(FrontRoomsStructures host)
    {
        if (tripped) SetTripped(false);
    }

    public override void RelayBody(FrontRoomsStructures host, bool on)
    {
        if (kit != null) kit.SetRelay(on);
    }

    public override void ShowMetrics(FrontRoomsStructures host, bool on)
    {
        if (kit != null) kit.ShowMarks(on);
    }

    public override string Status(FrontRoomsStructures host) => tripped ? "停电（按 T 恢复）" : "供电正常（按 T 停电）";

    public override void Guide(FrontRoomsStructures host, StringBuilder sb)
    {
        sb.AppendLine(Title);
        sb.AppendLine("Run! B 的设备走廊做成一路下坡：头上平走一格，然后 24 米 1:8 的坡道穿过两道防火门墙，尽头是目标门和后面的卸货间。");
        sb.AppendLine();
        sb.AppendLine("按键：T（或 L）停电 / 恢复；E 开关目标门；[ ] 换站点（走廊头、竖井下、回头看坡道、卸货平台、卡车位、储物笼前）；R 回到当前站点；C Relay 体型；M 尺寸。");
        sb.AppendLine();
        sb.AppendLine("要你决定（在对话里按编号告诉总控）：");
        sb.AppendLine("SP5-1 走廊两侧的小房间能进还是只能看（63 Q1）？入口左边的杂物间开着门，可以走进去试。");
        sb.AppendLine("SP5-2 卷帘门下面露出什么（63 Q3）？现在放的是钠灯下通往街道的坡道。");
        sb.AppendLine("SP5-3 装卸平台边：真实的开口边，还是现在这种可拆的链条护栏加摆门（63 Q2）？");
        sb.AppendLine("SP5-4 储物笼里站着一个替身：铁丝网、龙骨架这类看得穿的隔断，Relay 应该能隔着看见你，还是像墙一样挡住（62 Q1）？");
        sb.AppendLine("SP5-5 竖井、储物笼、卸货间，各自放进游戏吗？（坡道什么时候出现、多长，你已经同意了：71 的决定 2，第 3 档起出现，1:8，最长 24 米，最多下降 3 米；它和别的新结构一样，你走过、说可以，才进游戏。）");
        sb.AppendLine();
        sb.AppendLine("灰盒占位：铁丝网储物笼；竖井里的管线和爬梯；卸货间的混凝土地面和平台边；门外的坡道和钠灯；卷帘门、卸货调节板、链条护栏、风管、消防水带箱、打卡钟这些还没做的新道具。黑色人形是 Relay 的静态替身（2.05 米），只用来检查视线，不会动。");
        sb.AppendLine();
        sb.AppendLine("技术记录：游戏的 Run! B 灯光装置只适合一条平的走廊，这里的灯（R-B 灯管、应急灯）按它的数值另搭，停电只切换灯组，没有跳闸动画和警报。");
    }
}
