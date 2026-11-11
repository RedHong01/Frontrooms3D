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
/// NEW (contract K4, structures G4-b 2026-11-10, research/structures_assets/30_plan.md S5-02..S5-12): with the structure assets
/// on (kit.New) and their kits present, the services, the shaft, the cage, the closet, the fire-door frames, the goal door, the
/// dock, the outside ramp and the battery heads are the game's kits (structures_run_service P20, structures_dock P19, Run B's
/// kits); every collider stays today's, call for call (the fourth bollard S5-08 asks for adds its kit's); TODAY is unchanged.
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
    // NEW (K4): lenses this district drives itself (the dock light's PAR; the battery heads' sealed beams, by material index)
    readonly List<Renderer> ownLens = new List<Renderer>(), headKitLens = new List<Renderer>();
    readonly List<Color> ownLensOn = new List<Color>();
    readonly List<int> headKitLensIndex = new List<int>();
    Material sawcutMat;

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
        if (NewWith("Kit_HoseCabinet", "Kit_TimeClockStation")) kit.Safe("landing: hose cabinet, time clock", LandingNew);
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
        if (ServiceKits) ServicesNew();
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
        if (x > 0f && z0 > 25.8f - 1e-3f && z1 < 27f + 1e-3f && Mathf.Abs(f0 - f1) < 1e-4f && NewWith("Kit_HoseCabinet", "Kit_TimeClockStation"))
            WallFaceHoseHole(x, n, z0, z1, f0, s0);
        else
        {
            Quad(new Vector3(x, f0, z0), new Vector3(x, f1, z1), new Vector3(x, f1 + 1.2f, z1), new Vector3(x, f0 + 1.2f, z0), n, lower);
            Quad(new Vector3(x, f0 + 1.2f, z0), new Vector3(x, f1 + 1.2f, z1), new Vector3(x, s1, z1), new Vector3(x, s0, z0), n, upper);
        }
        Tilted(new Vector3(x + facing * .005f, f0 + .05f, z0), new Vector3(x + facing * .005f, f1 + .05f, z1), .01f, .102f, cove);
        // NEW (K4, S5-02): Run B's bumper rail tiles (1.2 m, whole tiles centred in the face), pitched with the ramp
        if (NewWith("Kit_BumperRail_1200")) BumperRails(x, facing, z0, z1, f0, f1);
        else Tilted(new Vector3(x + facing * .03f, f0 + .9f, z0), new Vector3(x + facing * .03f, f1 + .9f, z1), .05f, .12f, FrontRoomsPoolMaterials.Surface("RunB_BumperVinyl", new Color(.25f, .25f, .25f)));
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
        // NEW (K4, S5-03): the main, the tray and the duct are kits laid along the whole spine (ServicesNew); the EMT stays this box
        // in both (30_plan S5-03: its budget)
        var kits = ServiceKits;
        if (!kits) Tilted(new Vector3(.55f, f0 + 2.62f, z0), new Vector3(.55f, f1 + 2.62f, z1), .1f, .1f, red);
        if (!kits) Tilted(new Vector3(-.6f, f0 + 2.70f, z0), new Vector3(-.6f, f1 + 2.70f, z1), .3f, .06f, galv);
        Tilted(new Vector3(-1.39f, f0 + 2.76f, z0), new Vector3(-1.39f, f1 + 2.76f, z1), .025f, .025f, galv);
        if (!kits) Tilted(new Vector3(.995f, f0 + 2.60f, z0), new Vector3(.995f, f1 + 2.60f, z1), .61f, .30f, galv);
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
            // NEW (K4, S5-04): Run B's compartment frame (its block faces, lower tone, cove and dark-bronze casing over today's wall,
            // which keeps its collider), hinges uphill
            if (NewWith("Kit_RunCompartmentFrame"))
                KitAt("Kit_RunCompartmentFrame", new Vector3(0f, f, z), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            else
            {
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
            }
            // The leaves held open at 90 degrees on magnets, on the uphill landing (1.12 m clear: no leaf over the step), in a
            // non-casting renderer (colliders kept): a 172-degree strip's shadow of them combed FD1's south face (review SP5-F12,
            // as SP7-space-F10).
            var keep = kit.Mesh.CastShadows;
            kit.Mesh.CastShadows = false;
            if (NewWith("Kit_DoorLeaf_SteelRun_DA"))
            {
                // NEW (K4): Run B's double-acting steel leaves on the frame's hinges, open 90 degrees uphill, non-casting; today's two
                // leaf colliders, call for call
                kit.Mesh.Collider(new Vector3(-.975f, f + 1.04f, z - .54f), new Vector3(.05f, 2.08f, .96f));
                kit.Mesh.Collider(new Vector3(.975f, f + 1.04f, z - .54f), new Vector3(.05f, 2.08f, .96f));
                foreach (var lx in new[] { -.975f, .975f })
                    KitAt("Kit_DoorLeaf_SteelRun_DA", new Vector3(lx, f, z - .055f), Quaternion.Euler(0f, 180f, 0f), Vector3.one, false, false);
            }
            else
            {
                kit.Box(-1.0f, f, z - 1.02f, -.95f, f + 2.08f, z - .06f, leaf);
                kit.Box(.95f, f, z - 1.02f, 1.0f, f + 2.08f, z - .06f, leaf);
            }
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
        if (NewWith("Kit_CagedLadder", "Kit_RiserPipe_3m", "Kit_RiserPipe_3m_Galv2", "Kit_PipeClampWall", "Kit_RiserClamp", "Kit_CableTray_3m"))
            ShaftNew(f, top, cut0);
        else
        {
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
        }
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
        if (NewWith("Kit_DoorWedge")) KitAt("Kit_DoorWedge", new Vector3(-2.4f, 0f, 2.3f), Quaternion.identity, Vector3.one);
        else kit.StandIn("door wedge", new Vector3(-2.4f, .02f, 2.3f), new Vector3(.08f, .04f, .14f), FrontRoomsPoolMaterials.Lit("rubber wedge", new Color(.15f, .15f, .15f)), 0f, false);
        kit.Kit("Kit_JanitorCart", new Vector3(-3.6f, 0f, 1.2f), 90f, new Vector3(1.1f, 1.0f, .5f));
        if (NewWith("Kit_MopSink", "Kit_MopRack", "Kit_FloorMachine")) ClosetNew();
        if (kit.New && FrontRoomsLampKit.Available(FrontRoomsLampKit.Family.Globe)) ClosetLampNew();
        else kit.Lamp("closet", LightType.Spot, new Vector3(-3f, 2.35f, 1.5f), Vector3.down, 150f, 110f, 2.0f, 5f, RunTint, false, new Vector3(.18f, 1.22f, .08f), FrontRoomsPoolMaterials.Surface("Painted_Metal", new Color(.85f, .85f, .82f)), RunTint * 1.4f);
        // The E8 cage: chain-link from floor to soffit on x 1.5, posts at z 0, 1.5, 3.0, a padlocked mesh gate; shelving,
        // cartons and Run! B's chair dolly inside; a second Relay stand-in inside for 62 Q1 (sight crosses, bodies never).
        kit.Shell(Clear, 0f);
        kit.Slab(1.5f, 0f, 4.5f, 3f, 0f, floorMat, .25f);
        kit.WallZ(4.5f, -.08f, 3.08f, 0f, Clear, upper);
        kit.WallX(0f, 1.5f, 4.5f, 0f, Clear, upper);
        kit.WallX(3f, 1.5f, 4.5f, 0f, Clear, upper);
        kit.Lid(1.5f, 0f, 4.5f, 3f, Clear, soffit);
        if (NewWith("Kit_WireCagePartition_Door", "Kit_WireCagePartition_Panel1520", "Kit_WireCagePartition_Post")) CageNew();
        else
        {
            kit.Guard(new Vector3(1.5f, 0f, 0f), new Vector3(1.5f, 0f, 3f), 0f, FrontRoomsStructuresKit.GuardStyle.Mesh, null, null, Clear - .02f);
            kit.Box(1.47f, 0f, .9f, 1.53f, 2.0f, .95f, steel, false);
            kit.Box(1.47f, 1.0f, .85f, 1.53f, 1.1f, .9f, FrontRoomsPoolMaterials.Surface("Prop_Brass", new Color(.7f, .55f, .25f)), false);
        }
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
        // NEW (K4, S5-07): the game's steel frame in the 1.0 x 2.1 opening, the double-acting steel leaf and the crossbar on the hinge
        var doorKits = NewWith("Kit_DoorFrame_Steel", "Kit_DoorLeaf_SteelRun_DA", "Kit_ExitDevice_Crossbar");
        if (doorKits) KitAt("Kit_DoorFrame_Steel", new Vector3(-.5f, Dock, 27f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
        else
        {
            kit.Box(-.56f, Dock, 26.9f, -.5f, Dock + 2.16f, 27.1f, frame, false);
            kit.Box(.5f, Dock, 26.9f, .56f, Dock + 2.16f, 27.1f, frame, false);
            kit.Box(-.56f, Dock + 2.1f, 26.9f, .56f, Dock + 2.16f, 27.1f, frame, false);
        }
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
        if (doorKits)
        {
            // NEW: today's slab keeps its collider (E opens it) under the kit leaf; the lit lite moves into the leaf's 15 x 15 in
            // opening (its anchor lite_centre: 0.34 m from the hinge edge, 1.384 up) and stays the window back (71)
            slab.GetComponent<Renderer>().enabled = false;
            bar.GetComponent<Renderer>().enabled = false;
            FrontRoomsKitLibrary.Spawn("Kit_DoorLeaf_SteelRun_DA", hinge.transform, Vector3.zero, Quaternion.Euler(0f, 90f, 0f), null, false, "leaf (kit)", false);
            FrontRoomsKitLibrary.Spawn("Kit_ExitDevice_Crossbar", hinge.transform, new Vector3(.92f, 1.0f, -.025f), Quaternion.Euler(0f, 180f, 0f), null, false, "crossbar (kit)", false);
            lite.transform.localPosition = new Vector3(.34f, 1.3843f, 0f);
            lite.transform.localScale = new Vector3(.381f, .381f, .012f);
        }
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
        var dockKits = NewWith("Kit_OverheadCoilingDoor", "Kit_DockLeveler", "Kit_DockBumper", "Kit_Bollard", "Kit_DockLightPost",
            "Kit_DockEdgeGuard_Stanchion", "Kit_DockEdgeGuard_Chain_1400", "Kit_DockEdgeGuard_Chain_1100", "Kit_DockEdgeGuard_Gate");
        if (dockKits)
        {
            // NEW (K4, G4a.6): the curb angle stops where the leveler's lip hinges (x 3.59 / 5.41)
            kit.Box(-1.5f, Dock - .06f, 32.94f, 3.59f, Dock + .005f, 33.02f, galv, false);
            kit.Box(5.41f, Dock - .06f, 32.94f, 7.5f, Dock + .005f, 33.02f, galv, false);
        }
        else kit.Box(-1.5f, Dock - .06f, 32.94f, 7.5f, Dock + .005f, 33.02f, galv, false);
        // The north wall with the 12 x 14 ft overhead door (3.66 x 4.27) centred on x 4.5, half up: its bar 1.2 over the bay.
        kit.WallX(39f, -1.58f, 7.58f, Bay, DockCeil + .16f, block, new List<FrontRoomsStructuresKit.Opening> { new FrontRoomsStructuresKit.Opening(2.67f, 6.33f, Bay, Bay + 4.27f) });
        LowerBand(38.92f, -1f, -1.42f, 2.67f, Bay);
        LowerBand(38.92f, -1f, 6.33f, 7.42f, Bay);
        if (dockKits) DockNew();
        else
        {
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
        }
        kit.Mesh.Collider(new Vector3(4.5f, Dock + .53f, 32.9f), new Vector3(1.2f, 1.07f, .06f));
        // B2 at the east end: 8 risers 0.15 / 0.30 from the platform down to the bay against the east wall, a rail on the open side.
        kit.Shell(DockCeil, Bay);
        kit.StepRun(new Vector3(6.82f, 0f, 33f), Vector3.back, 1.2f, Bay, Dock, floorMat, cove);
        RailSloped(new Vector3(6.18f, Dock, 33f), new Vector3(6.18f, Bay + .15f, 35.1f));
        if (dockKits) Bollard(new Vector3(5.9f, Bay, 35.7f));
        else kit.StandIn("bollard", new Vector3(5.9f, Bay + .55f, 35.7f), new Vector3(.17f, 1.1f, .17f), FrontRoomsPoolMaterials.Surface("RunB_BucketYellow", new Color(.85f, .7f, .1f)));
        // The second way: a 1.2 x 1.2 landing off the platform's west end, then 8 risers going east along the dock face.
        kit.Box(-1.42f, Bay, 33f, -.22f, Dock, 34.2f, floorMat);
        kit.StepRun(new Vector3(-.22f, 0f, 33.6f), Vector3.left, 1.2f, Bay, Dock, floorMat, cove);
        kit.Guard(new Vector3(-1.42f, 0f, 34.16f), new Vector3(-.22f, 0f, 34.16f), Dock, FrontRoomsStructuresKit.GuardStyle.Rail);
        RailSloped(new Vector3(-.22f, Dock, 34.24f), new Vector3(1.88f, Bay + .15f, 34.24f));
        // The bay's kit (greybox): a platform truck, a front-load dumpster on the west wall; the platform's loads on its south
        // and west walls (plan F): shelving, a pallet stack, the receiving desk, Run! B's hand truck.
        if (NewWith("Kit_PlatformTruck", "Kit_DumpsterFrontLoad", "Kit_SteelShelving", "Kit_PalletLoad_Wrapped", "Kit_PalletLoad_Wrapped_High", "Kit_ReceivingDesk",
            "Kit_PalletJack", "Kit_WheelChock_Hung", "Kit_CCTVCamera"))
            DockPropsNew();
        else
        {
        kit.StandIn("platform truck (greybox)", new Vector3(2.0f, Bay + .2f, 36.5f), new Vector3(.75f, .4f, 1.5f), galv);
        kit.StandIn("front-load dumpster (greybox)", new Vector3(-.6f, Bay + .65f, 37.2f), new Vector3(1.6f, 1.3f, 1.9f), FrontRoomsPoolMaterials.Lit("dumpster green", new Color(.15f, .3f, .2f), .3f));
        kit.Kit("Kit_PlyCabinet", new Vector3(6.6f, Dock, 27.5f), 0f, new Vector3(1.2f, 1.8f, .45f));
        kit.Kit("Kit_Pallet", new Vector3(-.6f, Dock, 29.0f), 90f, new Vector3(1.0f, .15f, 1.2f));
        kit.Kit("Kit_Crate", new Vector3(-.6f, Dock + .15f, 29.0f), 0f, new Vector3(.6f, .5f, .5f));
        kit.Kit("Kit_StationCounter_Run120", new Vector3(-.9f, Dock, 31.4f), 90f, new Vector3(.7f, 1.0f, 1.2f));
        }
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
        // NEW (K4, S5-09): the retaining walls in Run!'s block (no new concrete family), with control joints, and the ramp's sawcuts
        var retaining = kit.New ? lower : concrete;
        kit.WallZ(1.5f, 39f, 45f, Bay - .3f, 1.2f, retaining, null, .3f);
        kit.WallZ(7.5f, 39f, 45f, Bay - .3f, 1.2f, retaining, null, .3f);
        kit.WallX(45f, 1.35f, 7.65f, -3.75f, 1.2f, retaining, null, .3f);
        if (kit.New) OutsideJointsNew();
        // Two high-pressure sodium heads (2100 K) on the walls at z 43.5, 4.0 m over the ramp, aimed at it and the door; on in
        // both states: the slot and its orange spill on the bay floor are the brightest thing in the dock (63 Q3).
        // Both heads always cast their shadows while on (review SP5-F4): unshadowed, the west head washed the dock's east wall and
        // the bay floor through the goal wall; shadowed, the slot itself cuts the spill to the trapezoid on the bay floor.
        foreach (var x in new[] { 1.75f, 7.25f })
        {
            // 240 (14, then 40): shadowed, the slot under the half-raised door is the only way the light reaches the dock, so it must
            // clip to lead the dock walls under their x1.0 strips (review SP5-F2; Red 11-04 S4 B). Measured in render9 (BT.709 luma,
            // the slot's opening against the four walls): at 40 the slot was 105 / 100 against the walls' 173 / 136 (platform /
            // truck bay); at 240 it is 192 / 188 (its bright part, p90, 204 / 202 over the walls' p90 195 / 168), still sodium amber.
            Light head;
            if (kit.New && LampFamily("WallPack", out var wallPack))
            {
                // NEW (K4, S5-09; G4a.3): the 1980s HPS wall pack on the retaining wall's face (x 1.65 / 7.35), its light at the lamp's
                // centre in the refractor (layout lamp_unity 0.15 out of the wall), today's aim, cone, 240 and shadow; held steady (ask 5
                // B: HpsTick's restrike and warm-up are filmed and flash-checked at G4 render before they drive it)
                var mount = new Vector3(x < 4.5f ? 1.65f : 7.35f, -3.64f + 4f, 43.5f);
                var face = Quaternion.Euler(0f, x < 4.5f ? 90f : -90f, 0f);
                var refractor = FrontRoomsLampKit.Build(wallPack, kit.PartRoot, mount, face, "SP5 HPS refractor", FrontRoomsLampKit.PhoneTier);
                if (refractor != null) SetEmission(refractor, Sodium * 2.4f);   // WallPackLensPeak 2.4 (structures_dock)
                head = kit.Lamp("high-pressure sodium", LightType.Spot, mount + face * new Vector3(0f, 0f, .15f), new Vector3(4.5f - x, -3.2f, -3.5f), 90f, 60f, 240f, 16f, Sodium, true,
                    Vector3.zero, null, Color.black);
            }
            else
            head = kit.Lamp("high-pressure sodium", LightType.Spot, new Vector3(x, -3.64f + 4f, 43.5f), new Vector3(4.5f - x, -3.2f, -3.5f), 90f, 60f, 240f, 16f, Sodium, true,
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
        // The dock: one R-B strip per cell at x1.0 (70 had x1.6; Red 11-04, S4 B, "每个都按推荐来做": the dock's strips step down so
        // the sodium slot under the overhead door is the brightest thing in the dock), and the dock light. The dock's strips light only while the walker is in the
        // dock or the goal door is open (review SP5-F3: every row reached landing C through the goal wall, unshadowed); the
        // north row casts its shadows, so the overhead door's wall stops lighting the outside ramp white (review SP5-F2).
        for (var i = 0; i < 3; i++)
            for (var j = 0; j < 4; j++)
            {
                var ds = Strip(new Vector3(-1.5f + 3f * i + 1.5f, DockCeil - .02f, 27f + 3f * j + 1.5f), false, 1f, 14f);
                dockStrips.Add(ds);
                if (j == 3 && kit.Host != null && kit.Host.Look != null) kit.Host.Look.Shadowed(ds);
            }
        Light dl;
        if (NewWith("Kit_DockLightPost") && LampFamily("DockArm", out var dockArm))
        {
            // NEW (K4, S5-08; G4a.3): the swing-arm dock light on its post's arm_mount (2.40 m up the +Z face); today's flood from
            // the PAR's face (layout light_unity, aim_unity), armed as before; its lens dims with it (SetTripped)
            var mount = new Vector3(3.0f, Dock + 2.4f, 32.8f + .0572f);
            var par = FrontRoomsLampKit.Build(dockArm, kit.PartRoot, mount, Quaternion.identity, "SP5 dock light PAR", FrontRoomsLampKit.PhoneTier);
            dl = kit.Lamp("dock light", LightType.Spot, mount + new Vector3(.653f, -.118f, .993f), new Vector3(0f, -.4226f, .9063f), 60f, 40f, 4f, 10f, new Color(1f, .95f, .85f), true,
                Vector3.zero, null, Color.black);
            if (par != null) { ownLens.Add(par); ownLensOn.Add(new Color(1f, .95f, .85f) * 2.6f); }   // DockArmLensPeak 2.6
        }
        else
            dl = kit.Lamp("dock light", LightType.Spot, new Vector3(3.1f, Dock + 1.75f, 32.7f), new Vector3(.4f, -.3f, 1f), 60f, 40f, 4f, 10f, new Color(1f, .95f, .85f), true,
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
        FrontRoomsMapWorld.ShippedShadowFilter(l);
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
        // NEW (K4, S5-11, "这里应该是TA和TB相结合"): the heads are Run B's yokes and sealed-beam lamps, on the twin-head cabinet (T-A)
        // or on the remote mount ZL_RPLATE (T-B), as FrontRoomsRunRig builds them; their own lenses light when tripped
        var kitHeads = NewWith("Kit_EmergencyTwinHead_Yoke", "Kit_EmergencyTwinHead_Lamp");
        if (twin) body = kit.Kit("Kit_EmergencyTwinHead", at - Vector3.up * .1f, Mathf.Atan2(aim.x, aim.z) * Mathf.Rad2Deg, new Vector3(.3f, .2f, .12f), FrontRoomsPoolMaterials.Lit("head beige", new Color(.85f, .82f, .74f)), false);
        else if (!kitHeads) body = kit.StandIn("T-B remote head (greybox)", at, new Vector3(.18f, .12f, .12f), FrontRoomsPoolMaterials.Lit("head beige", new Color(.85f, .82f, .74f)), 0f, false);
        if (kitHeads) HeadUnitNew(at, aim, twin);
        var dir = (aim.normalized + Vector3.down * .55f).normalized;
        if (!kitHeads)
        {
        var lensGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        lensGo.name = "head lens";
        UnityEngine.Object.Destroy(lensGo.GetComponent<Collider>());
        lensGo.transform.SetParent(Root, false);
        lensGo.transform.localPosition = at + dir * .08f;
        lensGo.transform.localRotation = Quaternion.LookRotation(-dir, Vector3.up);
        lensGo.transform.localScale = new Vector3(.1f, .1f, 1f);
        var lens = lensGo.GetComponent<MeshRenderer>();
        lens.sharedMaterial = lensOff;
        headLens.Add(lens);
        }
        if (!kitHeads || twin)
        {
        var pilot = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pilot.name = "AC ON pilot";
        UnityEngine.Object.Destroy(pilot.GetComponent<Collider>());
        pilot.transform.SetParent(Root, false);
        pilot.transform.localPosition = at + Vector3.down * .09f + aim.normalized * .07f;
        pilot.transform.localScale = new Vector3(.015f, .015f, .015f);
        var pr = pilot.GetComponent<MeshRenderer>();
        pr.sharedMaterial = pilotOn;
        pilots.Add(pr);
        }
        var l = kit.Lamp(twin ? "T-A twin head" : "T-B remote head", LightType.Spot, at + dir * .1f, dir, 100f, 96f, 10f, 11f, Flood, true, Vector3.zero, null, Color.black);
        heads.Add(l);
    }

    // ---------------------------------------------------------------- signs (70 section 7, verbatim) and marks

    void BuildSigns()
    {
        // The arrival's sign spot is inside the corridor's east wall: on the wall, to the right, out of the main view, its face's
        // centre 2.50 m from the arrival eye (20_placard_type.md v2.4, E's FAR; was z 4.0, 2.88 m), its south end at the cage's
        // last post (z 3.0, on x 1.5; the panel's back is at x 1.395). In two columns (v2.6, 平面视觉 11-04 22:5x): in one its last
        // two questions read 15.60 / 15.98 px standing 0.9 m in front (review11), under 16.48, and it cannot rise under its blade
        // (15 mm over its top) and the duct over that; the description and the greybox row left, the questions right,
        // the panel 1.383 m tall (was 1.457), every line 16.97 px or more at 0.9 m (signs_v26_tools/signs_columns.py). Its centre
        // stands 37 mm higher, so the spot moves 6 mm north to keep 2.500 m: z 3.568 (was 3.562).
        FrontRoomsStructuresSigns.Placard(Root, Root.TransformPoint(new Vector3(1.36f, FloorAt(3.568f) + 1.5f, 3.568f)),
            Root.TransformPoint(new Vector3(0f, FloorAt(3.568f) + 1.5f, 3.568f)), "SP5 · Run!：一路下坡到卸货间",
            "游戏里的 Run!：设备走廊和它尽头的目标门。走廊 27 米：先平走一格，入口一边是铁丝网储物笼，一边是开着门的杂物间；后 24 米是 1:8 的坡道，一共下降 3 米；每 9 米一道防火门墙，过了门框再下两级小台阶（共 0.3 米）。坡道中段头顶开着一口设备竖井，里面的爬梯够不着。走廊尽头那扇门后面是 5.4 米高的卸货间：平台比卡车位高 1.2 米，边上拉着链条护栏；卷帘门只拉起一半，门下露出通往夜里街道的坡道，钠灯照得橘黄，是这里最亮的东西。按 T 停电，停电后只剩应急灯，再按一次恢复。\n"
            + (kit != null && kit.New ? GreyNew + "\n" : "灰盒占位：铁丝网储物笼；竖井里的管线和爬梯；卸货间的混凝土地面和平台边；门外的坡道和钠灯；卷帘门、卸货调节板、链条护栏、风管、消防水带箱、打卡钟这些还没做的新道具。黑色人形是 Relay 的静态替身（2.05 米），只用来检查视线，不会动。\n")
            + "要你决定：1 走廊两侧的小房间能进还是只能看（63 Q1）？入口左边的杂物间开着门，可以走进去试。 2 卷帘门下面露出什么（63 Q3）？现在放的是钠灯下通往街道的坡道。 3 装卸平台边：真实的开口边，还是现在这种可拆的链条护栏加摆门（63 Q2）？ 4 储物笼里站着一个替身：铁丝网、龙骨架这类看得穿的隔断，Relay 应该能隔着看见你，还是像墙一样挡住（62 Q1）？ 5 竖井、储物笼、卸货间，各自放进游戏吗？（坡道什么时候出现、多长，你已经同意了：71 的决定 2，第 3 档起出现，1:8，最长 24 米，最多下降 3 米；它和别的新结构一样，你走过、说可以，才进游戏。）", Root.position.y + FloorAt(3.568f), 2);
        // Its blade (20_placard_type.md v2.5, E-blade): the arrival reads the flat sign 56 degrees off its face, so its code and
        // title also hang on a blade square to the east wall (face x 1.42), inner edge 50 mm off it, bottom 2.10 over the ramp
        // under it (FloorAt 3.8475 = -0.106), its title on one line (11 ems, inside the blade's 12-em measure), top 2.283: under
        // the duct (underside 2.45, 0.16 m clear) and the main (2.57), 0.38 m past the twin head (z 3.15..3.45), 15 mm over the
        // flat sign's top (the ramp falls 35 mm from the sign's centre to here); front face z 3.8325, 2.500 m from the arrival
        // eye, read 18 degrees to the side and 11 up. (Two lines would leave 0.09 m under the duct.)
        FrontRoomsStructuresSigns.Blade(Root, Root.TransformPoint(new Vector3(1.42f, 0f, 3.8475f)), Root.TransformDirection(Vector3.left),
            Root.TransformPoint(Arrival + Vector3.up * FrontRoomsStructuresWalker.Eye), "SP5 · Run!：一路下坡到卸货间", Root.position.y + FloorAt(3.8475f));
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
            // LC-6 (K1 review; K4): NEW leaves the strips' lenses to the Look (Scale drives each lens with its lamp and its mode);
            // TODAY writes them here as before
            if (!(kit != null && kit.New) && k < armedLens.Count && armedLens[k] != null)
            {
                armedLens[k].GetPropertyBlock(block2);
                block2.SetColor("_EmissionColor", on ? Color.black : RunTint * 2.6f);
                armedLens[k].SetPropertyBlock(block2);
            }
        }
        foreach (var l in heads) if (l != null && look != null) look.Scale(l, on ? 1f : 0f);
        for (var k = 0; k < ownLens.Count; k++) if (ownLens[k] != null) SetEmission(ownLens[k], on ? Color.black : ownLensOn[k]);
        for (var k = 0; k < headKitLens.Count; k++)
        {
            if (headKitLens[k] == null) continue;
            headKitLens[k].GetPropertyBlock(block2, headKitLensIndex[k]);
            block2.SetColor("_EmissionColor", on ? Flood * 8f : Color.black);
            headKitLens[k].SetPropertyBlock(block2, headKitLensIndex[k]);
        }
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
        sb.AppendLine(kit != null && kit.New ? GreyNew : "灰盒占位：铁丝网储物笼；竖井里的管线和爬梯；卸货间的混凝土地面和平台边；门外的坡道和钠灯；卷帘门、卸货调节板、链条护栏、风管、消防水带箱、打卡钟这些还没做的新道具。黑色人形是 Relay 的静态替身（2.05 米），只用来检查视线，不会动。");
        sb.AppendLine();
        sb.AppendLine("技术记录：游戏的 Run! B 灯光装置只适合一条平的走廊，这里的灯（R-B 灯管、应急灯）按它的数值另搭，停电只切换灯组，没有跳闸动画和警报。");
    }

    // ================================================================ NEW (contract K4, structures G4-b 2026-11-10)
    // research/structures_assets/30_plan.md S5-02..S5-12 and 60_build.md G4a.6 / G4-b. Each method runs only with the structure assets
    // on for this district (kit.New) and the kits it names present (NewWith); otherwise the district builds TODAY exactly. The kits
    // spawn render-only; the colliders are today's, call for call, except the fourth bollard (S5-08) and the closet's sink and floor
    // machine (63 Q1: the closet stays enterable), which take their kits' sidecar colliders (walk test at G4 render).

    // the sign's greybox line once the kits are in (rule 7: an item leaves the line when it lands)
    const string GreyNew = "灰盒占位：卷帘门上的门号和打卡卡片上的字（等平面视觉）；竖井的砌块控制缝。黑色人形是 Relay 的静态替身（2.05 米），只用来检查视线，不会动。";

    bool ServiceKits => NewWith("Kit_Duct_1500", "Kit_Duct_Slope8", "Kit_DuctHanger", "Kit_DuctWallCollar", "Kit_SprinklerMain_3m", "Kit_SprinklerMain_Slope8",
        "Kit_SprinklerHead_Upright", "Kit_PipeHanger_Band", "Kit_CableTray_3m", "Kit_CableTray_Slope8");

    bool NewWith(params string[] assets)
    {
        if (kit == null || !kit.New) return false;
        foreach (var a in assets) if (!FrontRoomsKitLibrary.Has(a)) return false;
        return true;
    }

    // One of the game's kits under the part root at a full rotation and scale, render-only unless asked; null (logged once) if missing.
    GameObject KitAt(string asset, Vector3 pos, Quaternion rot, Vector3 scale, bool colliders = false, bool shadows = true)
    {
        GameObject go = null;
        try { go = FrontRoomsKitLibrary.Spawn(asset, kit.PartRoot, pos, rot, scale, colliders, null, false); }
        catch (Exception e) { FrontRoomsPoolMaterials.LogOnce("SP5 kit " + asset, "SP5: kit " + asset + " failed: " + e.Message); }
        if (go == null) FrontRoomsPoolMaterials.LogOnce("SP5 kit missing " + asset, "SP5: kit " + asset + " is missing (NEW).");
        else if (!shadows) foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    // The dock's lamp families (WallPack, DockArm) come with structures_dock's FrontRoomsLampKit: parsed by name, so this file compiles
    // on a main without them and the lamps stay today's where they are missing.
    static bool LampFamily(string name, out FrontRoomsLampKit.Family family) => Enum.TryParse(name, out family) && FrontRoomsLampKit.Available(family);

    void SetEmission(Renderer r, Color c)
    {
        if (block2 == null) block2 = new MaterialPropertyBlock();
        r.GetPropertyBlock(block2);
        block2.SetColor("_EmissionColor", c);
        r.SetPropertyBlock(block2);
    }

    // ---------------------------------------------------------------- S5-03 / S5-12: the services along the spine
    struct ServiceRun
    {
        public float z0, z1, f0, f1;
        public ServiceRun(float a, float b, float c, float d) { z0 = a; z1 = b; f0 = c; f1 = d; }
    }

    // Three groups between the walls (entry 0.08, FD1 8.92 | 9.08, FD2 17.92 | 18.08, goal 26.92), each level, 1:8, level: the 24 x 12
    // in duct in 1.5 m sections with its gored 1:8 fittings at the kinks and the fire damper sleeves' angles at each wall, strap
    // hangers about every 2.4 m; the 4 in main in 3 m lengths with grooved slope couplings, band hangers and upright heads; the
    // ladder tray with hinged splice plates and a trapeze every 3 m. Non-casting, as the game's overhead dressing (FrontRoomsRunDress).
    void ServicesNew()
    {
        var groups = new[]
        {
            new[] { new ServiceRun(.08f, 3f, 0f, 0f), new ServiceRun(3f, 7.8f, 0f, -.6f), new ServiceRun(7.8f, 8.92f, -.6f, -.6f) },
            new[] { new ServiceRun(9.08f, 9.6f, -.9f, -.9f), new ServiceRun(9.6f, 16.8f, -.9f, -1.8f), new ServiceRun(16.8f, 17.92f, -1.8f, -1.8f) },
            new[] { new ServiceRun(18.08f, 18.6f, -2.1f, -2.1f), new ServiceRun(18.6f, 25.8f, -2.1f, -3f), new ServiceRun(25.8f, 26.92f, -3f, -3f) },
        };
        foreach (var g in groups)
        {
            for (var i = 0; i < g.Length; i++)
            {
                var r = g[i];
                var first = i == 0;
                var last = i == g.Length - 1;
                ServiceLine("Kit_Duct_1500", .995f, 2.60f, r, first ? 0f : .15f, last ? 0f : .15f, 1.5f, false);
                ServiceLine("Kit_SprinklerMain_3m", .55f, 2.62f, r, first ? 0f : .14f, last ? 0f : .14f, 3f, true);
                ServiceLine("Kit_CableTray_3m", -.6f, 2.70f, r, 0f, 0f, 3f, true);
                Hangers(r);
            }
            Kink(g[0].z1, g[0].f1, true);
            Kink(g[1].z1, g[1].f1, false);
            // the sleeves' retaining angles where the duct meets a wall: leaving it uphill (yaw 180) or downhill (yaw 0)
            KitAt("Kit_DuctWallCollar", new Vector3(.995f, g[0].f0 + 2.60f, g[0].z0), Quaternion.identity, Vector3.one, false, false);
            KitAt("Kit_DuctWallCollar", new Vector3(.995f, g[2].f1 + 2.60f, g[2].z1), Quaternion.Euler(0f, 180f, 0f), Vector3.one, false, false);
        }
    }

    // A service line along one run, trimmed by a fitting's half at each end, in n equal lengths of the nominal kit (scaled 0.75-1.25
    // along its axis); the duct runs along its kit's +Z, the main and the tray along their kits' -X (as FrontRoomsRunDress lays them).
    void ServiceLine(string asset, float x, float y, ServiceRun r, float trimStart, float trimEnd, float nominal, bool alongNegX)
    {
        var p0 = new Vector3(x, r.f0 + y, r.z0);
        var p1 = new Vector3(x, r.f1 + y, r.z1);
        var d = p1 - p0;
        var len = d.magnitude;
        if (len < .05f) return;
        var t = d / len;
        p0 += t * trimStart;
        len -= trimStart + trimEnd;
        if (len < .05f) return;
        var n = Mathf.Max(1, Mathf.RoundToInt(len / nominal));
        var seg = len / n;
        var rot = Quaternion.LookRotation(t, new Vector3(0f, t.z, -t.y));
        if (alongNegX) rot *= Quaternion.Euler(0f, 90f, 0f);
        var scale = alongNegX ? new Vector3(seg / nominal, 1f, 1f) : new Vector3(1f, 1f, seg / nominal);
        for (var k = 0; k < n; k++) KitAt(asset, p0 + t * (seg * k), rot, scale, false, false);
    }

    // Plumb hangers along a run: duct straps about every 2.4 m; the main's band hangers every 3 m with an upright head between; a
    // trapeze (a 1-5/8 in strut on two rods to the deck) under the tray every 3 m.
    void Hangers(ServiceRun r)
    {
        var dz = r.z1 - r.z0;
        if (dz < .3f) return;
        var nd = Mathf.Max(1, Mathf.RoundToInt(dz / 2.4f));
        for (var k = 0; k < nd; k++)
        {
            var z = r.z0 + dz * (k + .5f) / nd;
            KitAt("Kit_DuctHanger", new Vector3(.995f, Mathf.Lerp(r.f0, r.f1, (z - r.z0) / dz) + 2.60f, z), Quaternion.identity, Vector3.one, false, false);
        }
        var nm = Mathf.Max(1, Mathf.RoundToInt(dz / 3f));
        var keep = kit.Mesh.CastShadows;
        kit.Mesh.CastShadows = false;
        for (var k = 0; k < nm; k++)
        {
            var z = r.z0 + dz * (k + .25f) / nm;
            var f = Mathf.Lerp(r.f0, r.f1, (z - r.z0) / dz);
            KitAt("Kit_PipeHanger_Band", new Vector3(.55f, f + 2.62f, z), Quaternion.Euler(0f, 90f, 0f), Vector3.one, false, false);
            var zh = r.z0 + dz * (k + .75f) / nm;
            KitAt("Kit_SprinklerHead_Upright", new Vector3(.55f, Mathf.Lerp(r.f0, r.f1, (zh - r.z0) / dz) + 2.62f + .05f, zh), Quaternion.identity, Vector3.one, false, false);
            var tz = r.z0 + dz * (k + .5f) / nm;
            var ft = Mathf.Lerp(r.f0, r.f1, (tz - r.z0) / dz);
            kit.Mesh.Box(new Vector3(-.6f, ft + 2.70f - .0105f, tz), new Vector3(.42f, .021f, .041f), Quaternion.identity, galv, false);
            foreach (var sx in new[] { -.19f, .19f })
                kit.Mesh.Box(new Vector3(-.6f + sx, (ft + 2.70f - .021f + ft + Clear) * .5f, tz), new Vector3(.0095f, Clear - 2.70f + .021f, .0095f), Quaternion.identity, galv, false);
        }
        kit.Mesh.CastShadows = keep;
    }

    // The 1:8 fittings at a kink: the kits as built have level above and falling below (top); at a bottom kink the duct's and the
    // main's fittings turn 180 degrees about X (both are symmetric top to bottom) and the tray's splice plates do too, lifted by the
    // tray's 80 mm so they stay over its bottom line.
    void Kink(float z, float f, bool top)
    {
        var rot = top ? Quaternion.identity : Quaternion.Euler(180f, 0f, 0f);
        KitAt("Kit_Duct_Slope8", new Vector3(.995f, f + 2.60f, z), rot, Vector3.one, false, false);
        KitAt("Kit_SprinklerMain_Slope8", new Vector3(.55f, f + 2.62f, z), rot, Vector3.one, false, false);
        KitAt("Kit_CableTray_Slope8", new Vector3(-.6f, f + 2.70f + (top ? 0f : .08f), z), rot, Vector3.one, false, false);
    }

    // ---------------------------------------------------------------- S5-02: bumper rails
    void BumperRails(float x, float facing, float z0, float z1, float f0, float f1)
    {
        var a = new Vector3(x, f0 + .9f, z0);
        var d = new Vector3(x, f1 + .9f, z1) - a;
        var len = d.magnitude;
        var n = Mathf.FloorToInt((len - .02f) / 1.2f);
        if (n < 1) return;
        var t = d / len;
        var rot = Quaternion.LookRotation(new Vector3(facing, 0f, 0f), new Vector3(0f, t.z, -t.y));
        var start = (len - n * 1.2f) * .5f;
        for (var k = 0; k < n; k++) KitAt("Kit_BumperRail_1200", a + t * (start + .6f + 1.2f * k), rot, Vector3.one);
    }

    // ---------------------------------------------------------------- S5-12: the goal landing's hose cabinet and time clock
    // The semi-recessed Class II hose cabinet (tub 0.12 into the block) on the east wall, the time clock station on the west wall.
    void LandingNew()
    {
        KitAt("Kit_HoseCabinet", new Vector3(1.42f, Dock, 26.3f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
        KitAt("Kit_TimeClockStation", new Vector3(-1.42f, Dock, 26.25f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
    }

    // The east wall's face on the landing (level) with the hose cabinet's rough opening (z 25.85..26.75, 0.98..2.03 over the floor)
    // left open: each tone's quad split round it (the wall's collider is today's, from Walls).
    void WallFaceHoseHole(float x, Vector3 n, float z0, float z1, float f, float s)
    {
        const float H0 = 25.85f, H1 = 26.75f;
        var y0 = f + .98f;
        var y1 = f + 2.03f;
        foreach (var band in new[] { new Vector2(f, f + 1.2f), new Vector2(f + 1.2f, s) })
        {
            var m = band.x < f + .01f ? lower : upper;
            WallRect(x, n, z0, H0, band.x, band.y, m);
            WallRect(x, n, H1, z1, band.x, band.y, m);
            WallRect(x, n, H0, H1, band.x, Mathf.Min(band.y, y0), m);
            WallRect(x, n, H0, H1, Mathf.Max(band.x, y1), band.y, m);
        }
    }

    void WallRect(float x, Vector3 n, float z0, float z1, float y0, float y1, Material m)
    {
        if (z1 - z0 < 1e-3f || y1 - y0 < 1e-3f) return;
        Quad(new Vector3(x, y0, z0), new Vector3(x, y0, z1), new Vector3(x, y1, z1), new Vector3(x, y1, z0), n, m);
    }

    // ---------------------------------------------------------------- S5-05: the riser shaft (view only, no colliders, as today)
    // The caged ladder on the north wall (bottom rung 3.5 m over the ramp), the 4 in riser off the main to the lid in grooved 3 m
    // lengths, two W6 beams across the shaft carrying its riser clamp, the two 2 in pipes on strut clamps on the west wall, the ladder
    // tray riser up the south wall.
    void ShaftNew(float f, float top, float cut0)
    {
        KitAt("Kit_CagedLadder", new Vector3(0f, f + 3.5f, 14.92f), Quaternion.Euler(0f, 180f, 0f), Vector3.one, false, false);
        var y0 = FloorAt(13.15f) + 2.62f + .06f;
        Stack("Kit_RiserPipe_3m", new Vector3(.55f, y0, 13.15f), top - y0);
        var beam = FrontRoomsPoolMaterials.Surface("Painted_Metal", new Color(.42f, .41f, .38f));
        const float BeamTop = 4.0f;
        foreach (var bz in new[] { 13.01f, 13.29f })
        {
            kit.Box(-1.5f, BeamTop - .0055f, bz - .05f, 1.5f, BeamTop, bz + .05f, beam, false);
            kit.Box(-1.5f, BeamTop - .1445f, bz - .00215f, 1.5f, BeamTop - .0055f, bz + .00215f, beam, false);
            kit.Box(-1.5f, BeamTop - .15f, bz - .05f, 1.5f, BeamTop - .1445f, bz + .05f, beam, false);
        }
        KitAt("Kit_RiserClamp", new Vector3(.55f, BeamTop, 13.15f), Quaternion.Euler(0f, 90f, 0f), Vector3.one, false, false);
        foreach (var y in new[] { cut0 + .6f, cut0 + 2.6f, top - .5f })
            KitAt("Kit_PipeClampWall", new Vector3(-1.42f, y, 13.33f), Quaternion.Euler(0f, 90f, 0f), Vector3.one, false, false);
        foreach (var z in new[] { 13.03f, 13.63f })
            Stack("Kit_RiserPipe_3m_Galv2", new Vector3(-1.42f + .0715f, cut0, z), top - cut0);
        // Kit_CableTray_3m stood on end: its length (-X) up, its bottom on the south wall's face (z 12.08)
        var ty0 = cut0 - .3f;
        var len = top - ty0;
        var n = Mathf.Max(1, Mathf.CeilToInt(len / 3f - .01f));
        var seg = len / n;
        for (var k = 0; k < n; k++)
            KitAt("Kit_CableTray_3m", new Vector3(-.75f, ty0 + seg * k, 12.08f), Quaternion.LookRotation(Vector3.left, Vector3.forward), new Vector3(seg / 3f, 1f, 1f), false, false);
    }

    // a vertical run of 3 m kits (their length along +Y) from p up by len, in equal lengths
    void Stack(string asset, Vector3 p, float len)
    {
        var n = Mathf.Max(1, Mathf.CeilToInt(len / 3f - .01f));
        var seg = len / n;
        for (var k = 0; k < n; k++) KitAt(asset, p + Vector3.up * (seg * k), Quaternion.identity, new Vector3(1f, seg / 3f, 1f), false, false);
    }

    // ---------------------------------------------------------------- S5-06: the E8 cage and the janitor's closet
    // The cage front on x 1.5 as a 1980s welded-wire tenant partition: a 4 ft door hinged at the south wall (hasp and padlock at its
    // latch post), one 5 ft panel, three posts; the wire is two-faced, so both sides read lit (K1 review FR-1). Today's guard collider,
    // call for call (body only: sight still crosses the mesh).
    void CageNew()
    {
        kit.Mesh.Collider(new Vector3(1.5f, (Clear - .02f) * .5f, 1.5f), new Vector3(.05f, Clear - .02f, 3f));
        var rot = Quaternion.Euler(0f, -90f, 0f);   // the kits' front (+Z) to the corridor (-X), their +X along +Z
        foreach (var z in new[] { .105f, 1.375f, 2.895f }) KitAt("Kit_WireCagePartition_Post", new Vector3(1.5f, 0f, z), rot, Vector3.one);
        KitAt("Kit_WireCagePartition_Door", new Vector3(1.5f, 0f, .74f), rot, Vector3.one);
        KitAt("Kit_WireCagePartition_Panel1520", new Vector3(1.5f, 0f, 2.135f), rot, Vector3.one);
    }

    // 63 3.2 RN-JAN (Q1: the closet stays enterable, so it is furnished): the molded-stone floor basin in the north-west corner with
    // its service faucet, the mop and broom shelf over it, the floor machine stored by the south wall (their sidecar colliders).
    void ClosetNew()
    {
        var west = Quaternion.Euler(0f, 90f, 0f);   // on the closet's west wall (face x -4.42), front +X
        KitAt("Kit_MopSink", new Vector3(-4.42f, 0f, 2.61f), west, Vector3.one, true);
        KitAt("Kit_MopRack", new Vector3(-4.42f, 0f, 2.46f), west, Vector3.one);
        KitAt("Kit_FloorMachine", new Vector3(-4.14f, 0f, .40f), west, Vector3.one, true);
    }

    // S5-06: the closet lamp as ZL F7, the vapour-tight jelly jar on the lid (its own circuit: on in both states, as today), today's
    // light from the globe's centre without the fixture box
    void ClosetLampNew()
    {
        var globe = FrontRoomsLampKit.Build(FrontRoomsLampKit.Family.Globe, kit.PartRoot, new Vector3(-3f, 2.4f, 1.5f), Quaternion.identity, "SP5 closet globe", FrontRoomsLampKit.PhoneTier);
        if (globe != null) SetEmission(globe, RunTint * FrontRoomsLampKit.GlobeLensPeak);
        kit.Lamp("closet", LightType.Spot, new Vector3(-3f, 2.25f, 1.5f), Vector3.down, 150f, 110f, 2.0f, 5f, RunTint, false, Vector3.zero, null, Color.black);
    }

    // ---------------------------------------------------------------- S5-08: the dock (G4a.2 spots)
    void Bollard(Vector3 floor)
    {
        KitAt("Kit_Bollard", floor, Quaternion.identity, Vector3.one);
        kit.Mesh.Collider(floor + Vector3.up * .55f, new Vector3(.17f, 1.1f, .17f));
    }

    void DockNew()
    {
        // the 12 x 14 ft coiling door half up on the north wall's room face (hoist on the west jamb); today's curtain collider
        KitAt("Kit_OverheadCoilingDoor", new Vector3(4.5f, Bay, 38.92f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
        kit.Mesh.Collider(new Vector3(4.5f, (Bay + 1.2f + Bay + 4.27f) * .5f, 39.0f), new Vector3(3.66f, 3.07f, .06f));
        Bollard(new Vector3(2.45f, Bay, 38.7f));
        Bollard(new Vector3(6.55f, Bay, 38.7f));
        // the leveler flush and stored on the door's axis, its lip over the face; the bumpers each side (today's colliders); the
        // dock light's post (today's collider; the arm and its light in BuildLamps)
        KitAt("Kit_DockLeveler", new Vector3(4.5f, Dock, 33.02f), Quaternion.identity, Vector3.one);
        foreach (var x in new[] { 3.3f, 5.7f })
        {
            KitAt("Kit_DockBumper", new Vector3(x, Dock - .70f, 33.02f), Quaternion.identity, Vector3.one);
            kit.Mesh.Collider(new Vector3(x, Dock - .45f, 33.1f), new Vector3(.25f, .5f, .2f));
        }
        KitAt("Kit_DockLightPost", new Vector3(3.0f, Dock, 32.8f), Quaternion.identity, Vector3.one);
        kit.Mesh.Collider(new Vector3(3.0f, Dock + .9f, 32.8f), new Vector3(.08f, 1.8f, .08f));
        // the edge guard: stanchions on a 1.4 m pitch west and 1.1 east, the chains on snap hooks, the self-closing gate at the
        // leveler; today's two run colliders (Chain), call for call
        foreach (var x in new[] { -.30f, 1.10f, 2.50f, 3.90f, 5.10f, 6.20f })
            KitAt("Kit_DockEdgeGuard_Stanchion", new Vector3(x, Dock, 32.9f), Quaternion.identity, Vector3.one);
        foreach (var x in new[] { .40f, 1.80f, 3.20f })
            KitAt("Kit_DockEdgeGuard_Chain_1400", new Vector3(x, Dock + 1.0f, 32.9f), Quaternion.identity, Vector3.one);
        KitAt("Kit_DockEdgeGuard_Chain_1100", new Vector3(5.65f, Dock + 1.0f, 32.9f), Quaternion.identity, Vector3.one);
        KitAt("Kit_DockEdgeGuard_Gate", new Vector3(3.90f, Dock, 32.9f), Quaternion.identity, Vector3.one);
        kit.Mesh.Collider(new Vector3((-.22f + 3.9f) * .5f, Dock + .53f, 32.9f), new Vector3(3.9f + .22f, 1.07f, .06f));
        kit.Mesh.Collider(new Vector3((5.1f + 6.22f) * .5f, Dock + .53f, 32.9f), new Vector3(6.22f - 5.1f, 1.07f, .06f));
    }

    // The bay's and the platform's loads (S5-08, 63 RN-DOCK): today's truck and dumpster colliders kept; the platform's steel shelving,
    // wrapped loads, receiving desk and pallet jack with their sidecar colliders in today's pallet, crate, counter and cabinet's place
    // (walk test at G4 render); the CCTV camera off the new hood; chocks hung on the face; a fourth bollard at the west flight's foot;
    // the slab's sawcuts.
    void DockPropsNew()
    {
        KitAt("Kit_PlatformTruck", new Vector3(2.0f, Bay, 36.5f), Quaternion.identity, Vector3.one);
        kit.Mesh.Collider(new Vector3(2.0f, Bay + .2f, 36.5f), new Vector3(.75f, .4f, 1.5f));
        KitAt("Kit_DumpsterFrontLoad", new Vector3(-.62f, Bay, 37.2f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
        kit.Mesh.Collider(new Vector3(-.6f, Bay + .65f, 37.2f), new Vector3(1.6f, 1.3f, 1.9f));
        KitAt("Kit_SteelShelving", new Vector3(6.6f, Dock, 27.33f), Quaternion.identity, Vector3.one, true);
        KitAt("Kit_PalletLoad_Wrapped_High", new Vector3(-.75f, Dock, 29.0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one, true);
        KitAt("Kit_PalletLoad_Wrapped", new Vector3(-.75f, Dock, 30.35f), Quaternion.Euler(0f, 90f, 0f), Vector3.one, true);
        KitAt("Kit_ReceivingDesk", new Vector3(-1.12f, Dock, 31.4f), Quaternion.Euler(0f, 90f, 0f), Vector3.one, true);
        KitAt("Kit_PalletJack", new Vector3(2.6f, Dock, 27.9f), Quaternion.Euler(0f, 90f, 0f), Vector3.one, true);
        KitAt("Kit_CCTVCamera", new Vector3(7.05f, Bay + 3.35f, 38.92f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
        foreach (var x in new[] { 2.6f, 6.4f }) KitAt("Kit_WheelChock_Hung", new Vector3(x, Dock - .25f, 33.02f), Quaternion.identity, Vector3.one);
        KitAt("Kit_Bollard", new Vector3(2.35f, Bay, 34.55f), Quaternion.identity, Vector3.one, true);
        foreach (var x in new[] { 1.5f, 4.5f }) Sawcut(new Vector3(x, Bay, 33.05f), new Vector3(x, Bay, 38.9f));
        Sawcut(new Vector3(-1.45f, Bay, 36f), new Vector3(7.45f, Bay, 36f));
    }

    // a sawcut: a 5 mm dark line 2 mm proud of the slab, render only
    void Sawcut(Vector3 a, Vector3 b)
    {
        if (sawcutMat == null) sawcutMat = FrontRoomsPoolMaterials.Surface("Plenum_Dark", new Color(.06f, .06f, .05f));
        var keep = kit.Mesh.CastShadows;
        kit.Mesh.CastShadows = false;
        Tilted(a + Vector3.up * .001f, b + Vector3.up * .001f, .005f, .002f, sawcutMat);
        kit.Mesh.CastShadows = keep;
    }

    // S5-09: the retaining walls' control joints (one in each wall's middle) and the ramp's sawcuts (on its centre line and across)
    void OutsideJointsNew()
    {
        if (sawcutMat == null) sawcutMat = FrontRoomsPoolMaterials.Surface("Plenum_Dark", new Color(.06f, .06f, .05f));
        var ramp42 = Bay + .75f * .5f;
        kit.Box(1.645f, ramp42, 41.995f, 1.655f, 1.2f, 42.005f, sawcutMat, false);
        kit.Box(7.345f, ramp42, 41.995f, 7.355f, 1.2f, 42.005f, sawcutMat, false);
        kit.Box(4.495f, Bay + .75f, 44.845f, 4.505f, 1.2f, 44.855f, sawcutMat, false);
        Sawcut(new Vector3(4.5f, Bay, 39.05f), new Vector3(4.5f, Bay + .75f, 44.85f));
        Sawcut(new Vector3(1.65f, ramp42, 42f), new Vector3(7.35f, ramp42, 42f));
    }

    // ---------------------------------------------------------------- S5-11: the battery heads' units
    static readonly Vector3[] HeadMounts = { new Vector3(.112f, .201f, .059f), new Vector3(-.112f, .201f, .059f) };   // Kit_EmergencyTwinHead mount_l / _r

    // Two yokes and sealed-beam heads on the twin-head cabinet's mounts (T-A: the cabinet Head spawned) or on the remote mount
    // (T-B: ZL_RPLATE in the cabinet's frame, its trunnions at the head's point); both heads aim along today's flood; their lens
    // slots (Run_SealedBeamLens) light when tripped (SetTripped).
    void HeadUnitNew(Vector3 at, Vector3 aim, bool twin)
    {
        var yaw = Mathf.Atan2(aim.x, aim.z) * Mathf.Rad2Deg;
        var root = new GameObject(twin ? "T-A heads" : "T-B remote unit").transform;
        root.SetParent(kit.PartRoot, false);
        var flat = new Vector3(aim.x, 0f, aim.z);
        root.localPosition = twin ? at - Vector3.up * .1f : at + Vector3.down * .286f - (flat.sqrMagnitude > 1e-6f ? flat.normalized * .04f : Vector3.zero);
        root.localRotation = Quaternion.Euler(0f, yaw, 0f);
        if (!twin) FrontRoomsLampKit.BuildHousing(FrontRoomsLampKit.Family.RemotePlate, root, Vector3.zero, Quaternion.identity);
        var dir = kit.PartRoot.TransformDirection((aim.normalized + Vector3.down * .55f).normalized);
        var up = kit.PartRoot.up;
        foreach (var mount in HeadMounts)
        {
            var yoke = FrontRoomsKitLibrary.Spawn("Kit_EmergencyTwinHead_Yoke", root, mount, Quaternion.identity, null, false, "yoke", false);
            if (yoke == null) continue;
            var flatW = Vector3.ProjectOnPlane(dir, up);
            if (flatW.sqrMagnitude > 1e-6f) yoke.transform.rotation = Quaternion.LookRotation(flatW.normalized, up);
            var lamp = FrontRoomsKitLibrary.Spawn("Kit_EmergencyTwinHead_Lamp", yoke.transform, new Vector3(0f, .085f, 0f), Quaternion.identity, null, false, "lamp head", false);
            if (lamp != null)
            {
                lamp.transform.rotation = Quaternion.LookRotation(dir, up);
                foreach (var r in lamp.GetComponentsInChildren<Renderer>(true))
                {
                    var ms = r.sharedMaterials;
                    for (var i = 0; i < ms.Length; i++)
                        if (ms[i] != null && ms[i].name.StartsWith("Run_SealedBeamLens", StringComparison.Ordinal)) { headKitLens.Add(r); headKitLensIndex.Add(i); }
                }
            }
            foreach (var r in yoke.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
