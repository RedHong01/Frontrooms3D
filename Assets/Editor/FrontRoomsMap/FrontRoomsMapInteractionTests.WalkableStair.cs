using System;
using System.Collections.Generic;
using System.Linq;
using FrontRooms.Map;
using UnityEngine;

// ---------- The walkable stair (contract Documentation/research/stairwell/60_walkable_stair_for_map.md v1.1) ----------
// The map half of Red's 2026-10-22 13:00 decision (the real stair after C22), restarted 2026-11-10; MAP_GENERATION.md,
// "Walkable stair". The switch (FrontRoomsLevelProfile.walkableStair) is read by the game alone (StairRunBegin, with a shell
// factory set); these rows call the map's stair overloads as the game calls them, with StandInStairShell in place of the visual
// line's FrontRoomsStairShell: the frame and numbers of W/apply/stairwell_walkable_contract/README.md 4.2-4.3, no art, no
// animation. Rows: WalkableStairOff (the switch off is C22, byte for byte), WalkableStairValidator (RunStairExits on three
// seeds), WalkableStairOn (the planner's cells, the 1 x 2 exit area, the change, the next floor's 1 x 2 start area, the shell's
// frame on the map's lines, the far door no USE target, BS1 after a walkable stair).
public static partial class FrontRoomsMapInteractionTests
{
    const int StairSeed = 20261110;

    static void WalkableStair(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles)
    {
        var keepForced = FrontRoomsMapWorld.ForcedExitShadowsForTools;
        var keepFactory = FrontRoomsStairShellHost.Create;
        try
        {
            WalkableStairOff(roots, profiles);
            WalkableStairValidator();
            WalkableStairOn(roots, profiles);
        }
        finally
        {
            FrontRoomsMapWorld.ForcedExitShadowsForTools = keepForced;
            FrontRoomsStairShellHost.Create = keepFactory;
        }
    }

    // A scratch copy of the level profile with the switch as asked. Kits off, as in the exit area's rows (the transition kits keep
    // out of the EXIT's hold square anyway: TransitionExcluded). runRooms off where two builds of one floor are compared: the Run!
    // deck deals by floor and keeps a history, so a floor dealt again after other floors may differ for that reason alone.
    static FrontRoomsLevelProfile StairProfile(List<FrontRoomsLevelProfile> profiles, bool on, bool runRoomsOff)
    {
        var p = UnityEngine.Object.Instantiate(FrontRoomsLevelProfiles.Resolve());
        p.hideFlags = HideFlags.DontSave;
        p.transitions = false;
        p.walkableStair = on;
        if (runRoomsOff) p.runRooms = false;
        profiles.Add(p);
        return p;
    }

    static IFrontRoomsStairShell StandInFactory(Transform parent, Vector3 x1DoorEdgeCentre, Vector3 doorNormal) =>
        new StandInStairShell(parent, x1DoorEdgeCentre, doorNormal);

    /// <summary>
    /// The visual line's FrontRoomsStairShell as these rows need it: Root at X1's door-edge centre facing the door normal; the frame
    /// of README 4.2 (exit landing y 0, turn landing -1.75, far landing -3.50; inner faces x +-1.33, z 0.17 .. 5.83; the far leaf
    /// hinged at x +0.50 on the door axis z 0.14, 0.98 m wide, shutting toward -x); the walk's six waypoints and its progress path
    /// (4.3); one box for the far leaf (no map collider, no FrontRoomsInspectable, as the shell's own); the far door as a state that
    /// shuts and latches at once; every call counted.
    /// </summary>
    sealed class StandInStairShell : IFrontRoomsStairShell, IFrontRoomsStairShellVisible
    {
        public const float InnerX = 1.33f, DoorWallZ = .17f, FlightFootZ = 1.74f, ShellLength = 6f, TurnY = -1.75f, FarY = -3.5f, SlabY = 3.3f;
        public const float FarHingeX = .5f, DoorAxisZ = .14f, FarLeafWidth = .98f, FarLeafHeight = 2.1f;

        public enum FarDoorState { Shut, Held, Open, Latched }

        static readonly Vector3[] LocalPath =
        {
            new Vector3(.70f, 0f, 1.50f),
            new Vector3(.77f, TurnY, 4.62f),
            new Vector3(0f, TurnY, 5.15f),
            new Vector3(-.77f, TurnY, 4.62f),
            new Vector3(-.77f, FarY, 1.40f),
            new Vector3(-.85f, FarY, .62f),
        };

        static readonly Vector3[] ProgressPath =
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(.70f, 0f, 1.50f),
            new Vector3(.77f, TurnY, 4.62f),
            new Vector3(0f, TurnY, 5.15f),
            new Vector3(-.77f, TurnY, 4.62f),
            new Vector3(-.77f, FarY, 1.40f),
            new Vector3(0f, FarY, 0f),
        };

        readonly Transform root, farHinge, latchAnchor;
        public readonly BoxCollider FarLeaf;
        public FarDoorState Door;
        public int Opens, Holds, Closes, Moves, Latches, RebaseCalls, FloorCalls, PaperCalls, ShutCalls, VisibleCalls, LampCalls;
        public Vector3 LastDelta;
        public int LastFloor;
        public string LastLabel;
        public Material Paper;
        public bool ExitLeafShut, Shown = true;
        public float Lamp = 1f;
        float farCeiling = ModuleUnits.StandardCeiling;

        public StandInStairShell(Transform parent, Vector3 x1DoorEdgeCentre, Vector3 doorNormal)
        {
            var go = new GameObject("STAIR stand-in shell (MIT)") { hideFlags = HideFlags.DontSave };
            root = go.transform;
            if (parent != null) root.SetParent(parent, false);
            var normal = new Vector3(doorNormal.x, 0f, doorNormal.z);
            if (normal.sqrMagnitude < 1e-6f) normal = Vector3.forward;
            root.SetPositionAndRotation(x1DoorEdgeCentre, Quaternion.LookRotation(normal.normalized, Vector3.up));
            farHinge = new GameObject("Far hinge (stand-in)").transform;
            farHinge.SetParent(root, false);
            farHinge.localPosition = new Vector3(FarHingeX, FarY, DoorAxisZ);
            var leaf = new GameObject("Far leaf (stand-in)");
            leaf.transform.SetParent(farHinge, false);
            leaf.transform.localPosition = new Vector3(-FarLeafWidth * .5f, FarLeafHeight * .5f + .01f, 0f);
            FarLeaf = leaf.AddComponent<BoxCollider>();
            FarLeaf.size = new Vector3(FarLeafWidth, FarLeafHeight, .045f);
            latchAnchor = new GameObject("Latch anchor (stand-in)").transform;
            latchAnchor.SetParent(root, false);
            latchAnchor.localPosition = new Vector3(.5f, 1f, DoorWallZ);
        }

        public Vector3 ExitDoorPoint => root.position;
        public Vector3 ExitDoorForward => root.forward;
        public Vector3 FarDoorPoint => root.TransformPoint(new Vector3(0f, FarY, 0f));
        public Transform FarDoorHinge => farHinge;
        public float FloorToFloor => -FarY;

        public Vector3[] PathWaypoints
        {
            get
            {
                var w = new Vector3[LocalPath.Length];
                for (var i = 0; i < w.Length; i++) w[i] = root.TransformPoint(LocalPath[i]);
                return w;
            }
        }

        // As the shell's: the nearest point of the progress path, the levels told apart by height (y weighs twice).
        public float PathProgress(Vector3 feet)
        {
            var p = root.InverseTransformPoint(feet);
            float total = 0f, best = float.MaxValue, at = 0f;
            for (var i = 0; i + 1 < ProgressPath.Length; i++)
            {
                var a = ProgressPath[i];
                var ab = ProgressPath[i + 1] - a;
                var len = ab.magnitude;
                var t = len > 1e-5f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / (len * len)) : 0f;
                var d = p - (a + ab * t);
                d.y *= 2f;
                if (d.sqrMagnitude < best)
                {
                    best = d.sqrMagnitude;
                    at = total + len * t;
                }
                total += len;
            }
            return total > 0f ? Mathf.Clamp01(at / total) : 0f;
        }

        public bool OnFarLanding(Vector3 feet)
        {
            var p = root.InverseTransformPoint(feet);
            return Mathf.Abs(p.x) <= InnerX && p.z >= DoorWallZ - .05f && p.z <= FlightFootZ + .05f && p.y <= FarY + .12f && p.y >= FarY - .3f;
        }

        public void OpenFarDoor()
        {
            if (Door == FarDoorState.Open || Door == FarDoorState.Latched) return;
            Door = FarDoorState.Open;
            Opens++;
            Moves++;
            FarDoorMoved?.Invoke();
        }

        public void HoldFarDoor()
        {
            if (Door != FarDoorState.Shut) return;
            Door = FarDoorState.Held;
            Holds++;
        }

        public void CloseFarDoor()
        {
            if (Door == FarDoorState.Latched) return;
            Closes++;
            if (Door == FarDoorState.Open)
            {
                Moves++;
                FarDoorMoved?.Invoke();
            }
            Door = FarDoorState.Latched;
            Latches++;
            FarDoorLatched?.Invoke();
        }

        public event Action FarDoorMoved;
        public event Action FarDoorLatched;

        public void ShowExitLeafShut(bool shut)
        {
            ExitLeafShut = shut;
            ShutCalls++;
        }

        public void SetFarWallPaper(Material paper)
        {
            Paper = paper;
            PaperCalls++;
        }

        public float FarCeiling
        {
            get { return farCeiling; }
            set { farCeiling = Mathf.Max(2f, value); }
        }

        public void Rebased(Vector3 delta)
        {
            LastDelta = delta;
            RebaseCalls++;
        }

        public void SetFloor(int floor, string label)
        {
            LastFloor = floor;
            LastLabel = label;
            FloorCalls++;
        }

        public Transform LatchSoundAnchor => latchAnchor;
        public Transform Root => root;

        public void SetLampLevel(float level)
        {
            Lamp = Mathf.Clamp01(level);
            LampCalls++;
        }

        public float LightAt(Vector3 worldPos)
        {
            var p = root.InverseTransformPoint(worldPos);
            if (Mathf.Abs(p.x) > MapGrid.CellSize * .5f || p.z < -ModuleUnits.WallHalf || p.z > ShellLength || p.y < FarY - .3f || p.y > SlabY + .1f) return -1f;
            return Shown ? Lamp : 0f;
        }

        public void SetVisible(bool on)
        {
            Shown = on;
            VisibleCalls++;
        }
    }

    // ---------- The switch off: C22, byte for byte ----------

    /// <summary>
    /// The switch off is C22: the profile asset reads off; ChooseExit's plan is C22's (stair false, the next start area X alone
    /// with its door north of it, nothing of the new field in its JSON); a floor set with the old calls has the 1 x 1 exit area,
    /// C22's edges and no forced lamp shadow even with forced shadows allowed; the same floor built again after a walkable-stair
    /// floor (exit area 1 x 2, then a 1 x 2 start area) fingerprints exactly as before with nothing left over; a C22 start area
    /// keeps its open north edge and its walls.
    /// </summary>
    static void WalkableStairOff(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles)
    {
        const string label = "walkable stair, off: ";
        var asset = FrontRoomsLevelProfiles.Resolve();
        Check(asset != null && !asset.walkableStair,
            label + "the level profile " + (asset != null ? asset.name : "none") + " reads walkableStair " + (asset != null && asset.walkableStair)
            + " (a serialized bool, false by default and not written in the asset): every run plans C22's vestibule");

        var world = World(roots, StairProfile(profiles, false, true), 161f, "MAP INTERACTION TEST / walkable stair off");
        world.BuildForCapture();
        var eye = world.Player;
        var spawn = eye.position;
        var seed = world.Seed;
        var start = world.CellOf(spawn);
        var none = new CellRect(0, 0, 0, 0);
        var plan = FrontRoomsExitPlanner.ChooseExit(world.Cache, none, start, seed, FrontRoomsExitPlanner.Floor1BandMin, FrontRoomsExitPlanner.Floor1BandMax);
        if (!plan.found) { Check(false, label + "the planner found no EXIT from " + start + " (seed " + seed + ")"); return; }
        var x = plan.exitCell;
        var north = new GridCoord(x.x, x.y + 1);
        var json = JsonUtility.ToJson(plan);
        var planOk = !plan.stair && plan.NextDoorCell == north && plan.NextStartArea.x == x.x && plan.NextStartArea.y == x.y
            && plan.NextStartArea.w == 1 && plan.NextStartArea.h == 1 && !json.Contains("stair");

        // Forced shadows allowed (the desktop's): still none without a walkable-stair exit area.
        FrontRoomsMapWorld.ForcedExitShadowsForTools = 1;
        var before = WalkableStairC22Floor(world, eye, spawn, seed, x);
        var areaOk = world.HasExitArea && !world.ExitIsStair && world.ExitRect.width == 1 && world.ExitRect.height == 1 && world.InExitArea(x) && !world.InExitArea(north)
            && world.EffectiveEdge(world.ExitFrontCell, x) == EdgeKind.Door && world.EffectiveEdge(x, north) == EdgeKind.Wall;
        var forcedBefore = world.ForcedShadowLamps;
        Check(planOk && areaOk && forcedBefore == 0,
            label + "seed " + seed + ": ChooseExit's X " + x + " is C22's (stair " + plan.stair + ", next door " + plan.NextDoorCell + ", next start area 1 x 1, the JSON without the new field: "
            + !json.Contains("stair") + "); SetExitArea(X): exit area 1 x 1, N-X the EXIT door, X's north edge a wall; " + forcedBefore + " forced lamp shadows with them allowed");

        // A walkable-stair floor on the same map (exit area 1 x 2, then a 1 x 2 start area), then the C22 floor again.
        var stairPlan = FrontRoomsExitPlanner.ChooseStairExit(world.Cache, none, start, seed, FrontRoomsExitPlanner.Floor1BandMin, FrontRoomsExitPlanner.Floor1BandMax);
        var forcedStair = -1;
        var stairStart = false;
        if (stairPlan.found)
        {
            world.NextFloor(seed, 1, new RectInt(0, 0, 0, 0), default(GridCoord), false);
            world.SetExitArea(stairPlan.exitCell, true);
            eye.position = spawn;
            world.RunFloor = 1;
            world.Begin(eye, true);
            Physics.SyncTransforms();
            for (var k = 0; k < 40; k++) world.TickFixturesForTools(Dt);
            forcedStair = world.ForcedShadowLamps;
            world.NextFloor(unchecked(seed + 1), 2, new RectInt(stairPlan.exitCell.x, stairPlan.exitCell.y, 1, 2), stairPlan.frontCell, true);
            stairStart = world.StartIsStair;
        }
        var after = WalkableStairC22Floor(world, eye, spawn, seed, x);
        var same = 0;
        var differ = new List<string>();
        foreach (var pair in before)
        {
            if (after.TryGetValue(pair.Key, out var h) && h == pair.Value) same++;
            else if (differ.Count < 4) differ.Add(pair.Key);
        }
        var clean = !world.ExitIsStair && !world.StartIsStair && world.ForcedShadowLamps == 0;
        Check(after.Count == before.Count && same == before.Count && clean && (!stairPlan.found || (forcedStair > 0 && stairStart)),
            label + "the same C22 floor built again after a walkable-stair floor (" + (stairPlan.found ? "X1 " + stairPlan.exitCell + ", " + forcedStair + " forced shadows, then a 1 x 2 start area: "
            + stairStart : "no stair EXIT on this seed") + "): " + same + " of " + before.Count + " chunks the same (" + string.Join(", ", differ) + "), no stair flag or forced shadow left (" + clean + ")");

        // A C22 start area (floor 2 in X): its north edge open for the vestibule's north door, its other edges walls.
        world.NextFloor(unchecked(seed + 2), 2, new RectInt(x.x, x.y, 1, 1), north);
        var startOk = world.HasStartArea && !world.StartIsStair && world.InStartArea(x) && !world.InStartArea(north) && world.EffectiveEdge(x, north) == EdgeKind.Open
            && world.EffectiveEdge(new GridCoord(x.x, x.y - 1), x) == EdgeKind.Wall && world.EffectiveEdge(new GridCoord(x.x - 1, x.y), x) == EdgeKind.Wall
            && world.EffectiveEdge(x, new GridCoord(x.x + 1, x.y)) == EdgeKind.Wall;
        Check(startOk, label + "a C22 start area (NextFloor with the old signature, X " + x + ", door " + north + "): not the stair's, X's north edge open, its south, west and east edges walls");
    }

    // A floor-1 C22 EXIT at x set with the old calls, as the game sets it, built and ticked: its chunks' fingerprints.
    static Dictionary<string, ulong> WalkableStairC22Floor(FrontRoomsMapWorld world, Transform eye, Vector3 spawn, int seed, GridCoord x)
    {
        world.NextFloor(seed, 1, new RectInt(0, 0, 0, 0), default(GridCoord));
        world.SetExitArea(x);
        eye.position = spawn;
        world.RunFloor = 1;
        world.Begin(eye, true);
        Physics.SyncTransforms();
        for (var k = 0; k < 40; k++) world.TickFixturesForTools(Dt);
        return ChunkFingerprints(world);
    }

    // ---------- The validator's walk ----------

    /// <summary>FrontRoomsMapValidator.RunStairExits on three seeds x three floors (the 100-seed run is FrontRoomsMapVerification.RunWalkableStairBatch).</summary>
    static void WalkableStairValidator()
    {
        const string label = "walkable stair, validator: ";
        var profile = FrontRoomsLevelProfiles.Resolve();
        // Floor 1's start area where the game puts it (FrontRoomsMapVerification's TitleDoorWorldCell and TitleRows).
        var titleDoor = new GridCoord(Mathf.FloorToInt(FrontRooms3DGame.TitleCenterX / MapGrid.CellSize),
            Mathf.FloorToInt((FrontRoomsRoomStream.RoomLength * 1.5f + MapGrid.CellSize * .5f) / MapGrid.CellSize));
        var titleRows = Mathf.RoundToInt(FrontRoomsRoomStream.RoomLength * 2f / MapGrid.CellSize);
        var report = FrontRoomsMapValidator.RunStairExits(profile.generation, StairSeed, 3, 3, titleDoor, 2, titleRows, profile.ModuleData());
        var errors = "";
        foreach (var s in report.seeds)
            if (!s.passed) { errors = "seed " + s.seed + ": " + string.Join(" | ", s.errors.Take(3)); break; }
        Check(report.failed == 0 && report.passed == 3,
            label + "RunStairExits, 3 seeds x 3 floors (every EXIT 1 x 2 by ChooseStairExit, each later floor from the last X1-X2 by SeedForStair, checked by CheckStairExit): "
            + report.passed + "/3 " + errors);
    }

    // ---------- The switch on ----------

    /// <summary>
    /// The walkable stair on one map, as the game makes it (FrontRooms3DGame.WalkableStair.cs): ChooseStairExit's X1-X2 and its
    /// cell rules; SetExitArea with stair (the 1 x 2 exit area, its edges, X1-X2 no map: no slab, lamp, prop or passage; the exit
    /// lamp group over both cells; at most ForcedExitShadows forced shadows, none with them off); the Relay deaf to X1 and X2; the
    /// shell made from the factory at the game's anchor, its frame on the map's lines; the change (SeedForStair, NextFloor with
    /// stair, the map root kept, the shell moved up FloorToFloor): the start area X1-X2 with only N'-X1 open and nothing of the map
    /// on that line or inside, the start lamp groups by the far door's side, the far door on the new floor's line and floor and no
    /// USE target; BS1 on the floor after a walkable stair.
    /// </summary>
    static void WalkableStairOn(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles)
    {
        const string label = "walkable stair, on: ";
        var profile = StairProfile(profiles, true, false);
        var world = World(roots, profile, 165f, "MAP INTERACTION TEST / walkable stair");
        world.BuildForCapture();
        var eye = world.Player;
        var none = new CellRect(0, 0, 0, 0);
        var baseSeed = world.Seed;
        var seed = baseSeed;
        var spawn = eye.position;
        var start = world.CellOf(spawn);
        var plan = FrontRoomsExitPlanner.ChooseStairExit(world.Cache, none, start, seed, FrontRoomsExitPlanner.Floor1BandMin, FrontRoomsExitPlanner.Floor1BandMax);
        var tried = 1;
        for (var k = 1; k < 8 && !plan.found; k++)
        {
            seed = unchecked(baseSeed + k * 7919);
            world.NextFloor(seed, 1, new RectInt(0, 0, 0, 0), default(GridCoord));
            spawn = world.SpawnWorldPosition;
            start = world.CellOf(spawn);
            plan = FrontRoomsExitPlanner.ChooseStairExit(world.Cache, none, start, seed, FrontRoomsExitPlanner.Floor1BandMin, FrontRoomsExitPlanner.Floor1BandMax);
            tried++;
        }
        if (!plan.found) { Check(false, label + "ChooseStairExit found no EXIT from the spawn in " + tried + " seeds from " + baseSeed); return; }

        // 1. The planner's cells.
        var x1 = plan.exitCell;
        var n = plan.frontCell;
        var x2 = plan.TurnCell;
        var origin = MapGrid.ChunkOrigin(MapGrid.ChunkOf(x1));
        int li = x1.x - origin.x, lj = x1.y - origin.y;
        var cellsOk = plan.stair && x1 == new GridCoord(n.x, n.y + 1) && x2 == new GridCoord(n.x, n.y + 2) && plan.NextDoorCell == n
            && plan.NextStartArea.x == x1.x && plan.NextStartArea.y == x1.y && plan.NextStartArea.w == 1 && plan.NextStartArea.h == 2
            && FrontRoomsExitPlanner.StairCellsFit(world.Cache, none, x1) && FrontRoomsExitPlanner.StairKeepsChunkWhole(world.Cache, none, x1, n)
            && li >= 1 && li <= MapGrid.ChunkCells - 2 && lj >= 1 && lj <= MapGrid.ChunkCells - 3
            && MapGrid.ChunkOf(x2) == MapGrid.ChunkOf(x1) && MapGrid.ChunkOf(n) == MapGrid.ChunkOf(x1)
            && plan.distance >= plan.bandMin && plan.distance <= plan.bandMax && plan.deadEnd == FrontRoomsExitPlanner.IsStairDeadEnd(world.Cache, x1)
            && !FrontRoomsExitPlanner.KeyCellOf(world.Cache, x1) && !FrontRoomsExitPlanner.KeyCellOf(world.Cache, x2);
        Check(cellsOk, label + "ChooseStairExit (seed " + seed + ", " + tried + " tried): X1 " + x1 + " = N " + n + " + (0, 1), X2 " + x2 + "; the next floor's door N and start area X1-X2; "
            + "StairCellsFit, StairKeepsChunkWhole; X1 chunk-local (" + li + ", " + lj + ") in 1..6 x 1..5, so X2 and N share its chunk; " + plan.distance + " walking cells in "
            + plan.bandMin + ".." + plan.bandMax + ", dead end " + plan.deadEnd + "; no key in X1 or X2");

        // 2. The floor as the game makes it: the 1 x 2 exit area set before the build.
        FrontRoomsMapWorld.ForcedExitShadowsForTools = 1;
        world.NextFloor(seed, 1, new RectInt(0, 0, 0, 0), default(GridCoord));
        world.SetExitArea(x1, true);
        eye.position = spawn;
        world.Begin(eye, true);
        Physics.SyncTransforms();
        var w1 = new GridCoord(x1.x - 1, x1.y);
        var e1 = new GridCoord(x1.x + 1, x1.y);
        var w2 = new GridCoord(x2.x - 1, x2.y);
        var e2 = new GridCoord(x2.x + 1, x2.y);
        var n2 = new GridCoord(x2.x, x2.y + 1);
        var areaOk = world.HasExitArea && world.ExitIsStair && world.ExitCell == x1 && world.ExitTurnCell == x2 && world.ExitFrontCell == n
            && world.ExitRect.x == x1.x && world.ExitRect.y == x1.y && world.ExitRect.width == 1 && world.ExitRect.height == 2
            && world.InExitArea(x1) && world.InExitArea(x2) && !world.InExitArea(n) && !world.InExitArea(n2) && world.IsReserved(x2) && world.InStairwell(x2)
            && !world.IsBuilt(x1) && !world.IsBuilt(x2) && world.IsChunkBuiltAt(x2) && world.IsBuilt(n);
        var edgesOk = world.EffectiveEdge(n, x1) == EdgeKind.Door && world.EffectiveEdge(x1, x2) == EdgeKind.Open
            && world.EffectiveEdge(w1, x1) == EdgeKind.Wall && world.EffectiveEdge(x1, e1) == EdgeKind.Wall
            && world.EffectiveEdge(w2, x2) == EdgeKind.Wall && world.EffectiveEdge(x2, e2) == EdgeKind.Wall && world.EffectiveEdge(x2, n2) == EdgeKind.Wall;
        var walled = true;
        foreach (var c in new[] { x1, x2 })
            foreach (var s in Steps)
            {
                var other = c + s;
                if (world.PassageBetween(c, other) != FrontRoomsMapWorld.Passage.Wall || world.PassageBetween(other, c) != FrontRoomsMapWorld.Passage.Wall) walled = false;
            }
        Check(areaOk && edgesOk && walled,
            label + "SetExitArea(X1, stair): the exit area X1-X2 " + world.ExitRect + " (reserved, a stairwell, not built, its chunk built); N-X1 the EXIT door, X1-X2 open, "
            + "X1's west and east and X2's west, east and north edges walls; every passage into either cell a wall (" + walled + ")");

        // 3. Nothing of the map inside the shell's 2.66 x 5.66 m.
        var things = new List<string>();
        foreach (var c in world.GetComponentsInChildren<Collider>(true))
            if (!c.isTrigger && !world.IsArchitecture(c) && (InCell(world, x1, c.bounds.center, .05f) || InCell(world, x2, c.bounds.center, .05f))) things.Add(c.name);
        foreach (var l in world.GetComponentsInChildren<Light>(true))
            if (InCell(world, x1, l.transform.position, 0f) || InCell(world, x2, l.transform.position, 0f)) things.Add(l.name);
        var slabs = 0;
        foreach (var c in new[] { x1, x2 })
        {
            var inside = world.CellCenter(c);
            if (Physics.Raycast(inside + Vector3.up, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore)) slabs++;
            if (Physics.Raycast(inside + Vector3.up, Vector3.up, 10f, ~0, QueryTriggerInteraction.Ignore)) slabs++;
        }
        Check(things.Count == 0 && slabs == 0, label + "nothing of the map in X1 or X2: no floor or ceiling slab (" + slabs + " hits), no lamp, prop or furniture ("
            + string.Join(", ", things.Take(4)) + ")");

        // 4. The exit lamp group over both cells, and the desktop's forced shadows.
        var lamps = world.GetComponentsInChildren<Light>(true);
        world.ExitLamps = 1f;
        world.TickFixturesForTools(0f);
        var full = LampSnapshot(lamps);
        world.ExitLamps = 0f;
        world.TickFixturesForTools(0f);
        var dark = LampSnapshot(lamps);
        world.ExitLamps = 1f;
        world.TickFixturesForTools(0f);
        int group = 0, groupDark = 0, byX2 = 0, others = 0, othersSame = 0;
        foreach (var l in lamps)
        {
            if (InStairExitGroup(world, x1, l))
            {
                group++;
                if (dark[l] < 0f) groupDark++;
                if (!InExitGroup(world, x1, l)) byX2++;
            }
            else
            {
                others++;
                if (dark[l] == full[l]) othersSame++;
            }
        }
        for (var k = 0; k < 31; k++) world.TickFixturesForTools(Dt);
        var forcedOn = world.ForcedShadowLamps;
        var formula = world.CheckLampStatesForTools(out _, out _, null);
        FrontRoomsMapWorld.ForcedExitShadowsForTools = 0;
        for (var k = 0; k < 31; k++) world.TickFixturesForTools(Dt);
        var forcedOff = world.ForcedShadowLamps;
        FrontRoomsMapWorld.ForcedExitShadowsForTools = 1;
        for (var k = 0; k < 31; k++) world.TickFixturesForTools(Dt);
        Check(group > 0 && groupDark == group && othersSame == others && forcedOn >= 1 && forcedOn <= FrontRoomsMapWorld.ForcedExitShadows && formula == 0 && forcedOff == 0,
            label + "the exit lamp group is every lamp within its light's range of X1-X2 (" + group + ", " + byX2 + " of them by X2 alone): ExitLamps 0 holds all dark (" + groupDark
            + "), the other " + others + " never change (" + othersSame + "); forced soft shadows on " + forcedOn + " of them (at most " + FrontRoomsMapWorld.ForcedExitShadows + " within "
            + FrontRoomsMapWorld.ForcedExitShadowMetres + " m), the lamp formula agrees (" + formula + " mismatches), none with them off (" + forcedOff + ")");

        // 5. The Relay never hears X1 or X2 (the safe zone): a noise there lies past the EXIT door's line (N-X1) by more than a leaf.
        var playerRoot = new GameObject("MAP INTERACTION TEST / walkable stair player") { hideFlags = HideFlags.DontSave };
        roots.Add(playerRoot);
        var body = playerRoot.AddComponent<CapsuleCollider>();
        body.height = ModuleUnits.PlayerHeight;
        body.radius = ModuleUnits.PlayerRadius;
        body.center = Vector3.up * (ModuleUnits.PlayerHeight * .5f);
        playerRoot.transform.position = world.CellCenter(x1);
        var fromN = new FrontRoomsMapPathField(400f);
        fromN.Build(world, n);
        var far = n;
        var farDistance = 0f;
        for (var y = n.y - 6; y <= n.y + 6; y++)
        for (var xx = n.x - 6; xx <= n.x + 6; xx++)
        {
            var c = new GridCoord(xx, y);
            var d = fromN.DistanceTo(c);
            if (!world.IsBuilt(c) || float.IsPositiveInfinity(d) || d < 9f || d > 21f || d <= farDistance) continue;
            far = c;
            farDistance = d;
        }
        if (farDistance <= 0f) Check(false, label + "no built cell 9-21 m from N to listen from");
        else
        {
            var tuning = new FrontRoomsHunterTuning { sightRange = 0f };
#if FRONTROOMS_RELAY_V2
            var loud = 400f;
#else
            var loud = 400f / tuning.hearing;
#endif
            var earX2 = new FrontRoomsMapHunter(world, tuning, body, null, 41);
            earX2.DebugPlace(world.CellCenter(far));
            earX2.Noise(world.CellCenter(x2), loud);
            var earX1 = new FrontRoomsMapHunter(world, tuning, body, null, 42);
            earX1.DebugPlace(world.CellCenter(far));
            earX1.Noise(world.CellCenter(x1), loud);
            world.RelayBody = null;
            var heardX2 = earX2.ListenPoint.HasValue || earX2.State == HunterState.Hunt;
            var heardX1 = earX1.ListenPoint.HasValue || earX1.State == HunterState.Hunt;
            Check(!heardX2 && !heardX1, label + "safe zone: a noise in X2 or in X1 is never heard, even at 400 m (from " + far + ", " + farDistance.ToString("0.0")
                + " m from N: X2 " + heardX2 + ", X1 " + heardX1 + "); measured across N-X1, the EXIT door's own line");
        }

        // 6. The shell from the factory at the game's anchor (FrontRooms3DGame.CreateStairShell), and its frame on the map's lines.
        FrontRoomsStairShellHost.Create = StandInFactory;
        var game = new GameObject("MAP INTERACTION TEST / walkable stair game root") { hideFlags = HideFlags.DontSave };
        roots.Add(game);
        var cs = MapGrid.CellSize;
        var edge = world.transform.TransformPoint(new Vector3((n.x + .5f) * cs, 0f, (n.y + 1) * cs));
        var normal = world.CellCenter(x1) - world.CellCenter(n);
        normal.y = 0f;
        normal.Normalize();
        var shell = FrontRoomsStairShellHost.Available ? FrontRoomsStairShellHost.Create(game.transform, edge, normal) as StandInStairShell : null;
        if (shell == null) { Check(false, label + "no shell from FrontRoomsStairShellHost.Create with the stand-in factory set"); return; }
        var floorY = world.transform.position.y;
        var mouth = world.CrossingPoint(n, x1);
        var waypoints = shell.PathWaypoints;
        var inCells = true;
        var rising = true;
        var landing = true;
        for (var i = 0; i < waypoints.Length; i++)
        {
            var wp = waypoints[i];
            if (!(InCell(world, x1, wp, ModuleUnits.PlayerRadius) || InCell(world, x2, wp, ModuleUnits.PlayerRadius)) || wp.y > floorY + 1e-3f || wp.y < floorY - shell.FloorToFloor - 1e-3f) inCells = false;
            if (i > 0 && shell.PathProgress(wp) <= shell.PathProgress(waypoints[i - 1])) rising = false;
            if (shell.OnFarLanding(wp) != (i >= 4)) landing = false;
        }
        var sweep = Flat(waypoints[4] - shell.FarDoorHinge.position);
        var frameOk = shell.Root.parent == game.transform && !shell.Root.IsChildOf(world.transform)
            && Flat(shell.ExitDoorPoint - mouth) < 1e-3f && Mathf.Abs(shell.ExitDoorPoint.y - floorY) < 1e-4f
            && Vector3.Angle(shell.ExitDoorForward, world.transform.forward) < .01f
            && (shell.FarDoorPoint - (shell.ExitDoorPoint - Vector3.up * shell.FloorToFloor)).sqrMagnitude < 1e-8f
            && shell.PathProgress(shell.ExitDoorPoint) < .01f && shell.PathProgress(shell.FarDoorPoint) > .99f;
        Check(frameOk && inCells && rising && landing && sweep > StandInStairShell.FarLeafWidth + ModuleUnits.PlayerRadius,
            label + "the shell from FrontRoomsStairShellHost.Create at the game's anchor: under the game's root, its ExitDoorPoint on the EXIT door's line N-X1 at the map's floor, "
            + "facing the map's +z; FarDoorPoint " + shell.FloorToFloor + " m under it; the walk's " + waypoints.Length + " waypoints inside X1-X2 (a capsule clear of the cell lines: "
            + inCells + "), its progress rising (" + rising + "), the far landing's trigger on the last two (" + landing + "); the first of those " + sweep.ToString("0.00")
            + " m from the far hinge, outside the leaf's sweep and a capsule (" + (StandInStairShell.FarLeafWidth + ModuleUnits.PlayerRadius).ToString("0.00") + " m)");

        // 7. The change (variant B): the seed SeedForStair finds, NextFloor with stair (start area X1-X2, door N' = N), the map root
        // where it was, the shell (and the player) up FloorToFloor, then Rebased, SetFloor, FarCeiling and the far wall's paper.
        var rootBefore = world.transform.position;
        var area = FrontRoomsExitPlanner.StairArea(x1);
        var seedPlan = FrontRoomsExitPlanner.SeedForStair(seed, 2, x1, profile.generation, profile.ModuleData(), 1, out var seedCache);
        var seedOk = seedPlan.leadsOn && seedCache != null && FrontRoomsExitPlanner.StairRowLeadsOn(seedCache, n, area, true)
            && !FrontRoomsExitPlanner.StairKeyInArea(seedCache, area) && (!seedPlan.placardWest || FrontRoomsExitPlanner.FarDoorPlacardWest(seedCache, n));
        var seed2 = seedPlan.seed;
        world.NextFloor(seed2, 1, new RectInt(x1.x, x1.y, 1, 2), n, true);
        var delta = Vector3.up * shell.FloorToFloor;
        var pointsBefore = shell.PathWaypoints;
        shell.Root.position += delta;
        Physics.SyncTransforms();
        shell.Rebased(delta);
        shell.SetFloor(2, "LEVEL B1");
        shell.FarCeiling = MapGrid.CeilingHeight(world.ZoneOf(n).height);
        shell.SetFarWallPaper(world.WallPaperOf(n));
        eye.position = world.CellCenter(n) + Vector3.up * .05f;
        world.Begin(eye, true);
        Physics.SyncTransforms();
        var startOk = (world.transform.position - rootBefore).sqrMagnitude < 1e-8f && world.HasStartArea && world.StartIsStair && world.InStartArea(x1) && world.InStartArea(x2)
            && !world.InStartArea(n) && world.IsReserved(x1) && world.InStairwell(x2) && world.PassageBetween(n, x1) == FrontRoomsMapWorld.Passage.Wall;
        var startEdges = world.EffectiveEdge(n, x1) == EdgeKind.Open && world.EffectiveEdge(x1, x2) == EdgeKind.Open
            && world.EffectiveEdge(w1, x1) == EdgeKind.Wall && world.EffectiveEdge(x1, e1) == EdgeKind.Wall
            && world.EffectiveEdge(w2, x2) == EdgeKind.Wall && world.EffectiveEdge(x2, e2) == EdgeKind.Wall && world.EffectiveEdge(x2, n2) == EdgeKind.Wall;
        Check(seedOk && startOk && startEdges,
            label + "the change: SeedForStair's seed " + seed2 + " (try " + seedPlan.tries + ", N' standard " + seedPlan.standard + ", leads on south, no key in X1-X2, placard west "
            + seedPlan.placardWest + "); NextFloor(stair): the map root kept, the start area X1-X2 (reserved, a stairwell, no passage from N'), its only open edge N'-X1 "
            + "(the far door's), X1-X2 open, the other five edges walls");

        // 8. Nothing of the map on N'-X1 or inside X1-X2 (the visual line keeps its far-door wall's thickening only so): rays from
        // just inside N' across the line, at four heights and three offsets, meet no collider of the map before X2's north wall.
        var forward = world.transform.forward;
        var right = world.transform.right;
        var northFace = cs * 2f - ModuleUnits.WallHalf;
        int rays = 0, early = 0, northHits = 0;
        var earlyNames = new List<string>();
        foreach (var h in new[] { .3f, 1.2f, 2.0f })
            foreach (var off in new[] { -1f, 0f, 1f })
            {
                rays++;
                var rayFrom = mouth + right * off + Vector3.up * h - forward * .3f;
                var nearest = float.MaxValue;
                Collider nearestCollider = null;
                foreach (var hit in Physics.RaycastAll(rayFrom, forward, cs * 2f + 1f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider == null || !hit.collider.transform.IsChildOf(world.transform) || hit.distance >= nearest) continue;
                    nearest = hit.distance;
                    nearestCollider = hit.collider;
                }
                if (nearest < .3f + northFace - .1f)
                {
                    early++;
                    if (earlyNames.Count < 4) earlyNames.Add(nearestCollider.name + " at " + (nearest - .3f).ToString("0.00") + " m");
                }
                else if (nearest < .3f + northFace + .1f) northHits++;
            }
        Check(early == 0 && northHits == rays,
            label + "nothing of the map on N'-X1 or in X1-X2: of " + rays + " rays across the line (0.3 to 2.0 m up, the middle and 1 m to either side) none meets the map before X2's "
            + "north wall (" + string.Join(", ", earlyNames) + "), each meets that wall (" + northHits + ")");

        // 9. The start lamp groups by the far door's side: group 1 (lit with the door) the door row and south of it, group 2 behind.
        var lamps2 = world.GetComponentsInChildren<Light>(true);
        world.StartLampsNorth = 1f;
        world.StartLampsSouth = 1f;
        world.TickFixturesForTools(0f);
        var lit = LampSnapshot(lamps2);
        world.StartLampsNorth = 0f;
        world.TickFixturesForTools(0f);
        var doorSideHeld = LampSnapshot(lamps2);
        world.StartLampsNorth = 1f;
        world.StartLampsSouth = 0f;
        world.TickFixturesForTools(0f);
        var behindHeld = LampSnapshot(lamps2);
        world.StartLampsSouth = 1f;
        world.TickFixturesForTools(0f);
        int g1 = 0, g1Dark = 0, g1South = 0, g2 = 0, g2Dark = 0, crossed = 0;
        foreach (var l in lamps2)
        {
            var g = StairStartGroup(world, x1, n, l);
            if (g == 0)
            {
                if (doorSideHeld[l] != lit[l] || behindHeld[l] != lit[l]) crossed++;
                continue;
            }
            if (g == 1)
            {
                g1++;
                if (doorSideHeld[l] < 0f) g1Dark++;
                if (behindHeld[l] != lit[l]) crossed++;
                if (world.CellOf(l.transform.position).y < n.y) g1South++;
            }
            else
            {
                g2++;
                if (behindHeld[l] < 0f) g2Dark++;
                if (doorSideHeld[l] != lit[l]) crossed++;
            }
        }
        Check(g1 > 0 && g1Dark == g1 && g2Dark == g2 && crossed == 0,
            label + "the start lamp groups follow the far door's side: StartLampsNorth 0 holds the " + g1 + " lamps in range on N''s row and south of it dark (" + g1Dark + "; "
            + g1South + " south of N'), StartLampsSouth 0 the " + g2 + " behind it (" + g2Dark + "), no lamp answers the other group or none (" + crossed + ")");

        // 10. The shell after the move: the far door stands in the new floor's open N'-X1 line, on its floor; the walk moved with it.
        var moved = true;
        var pointsAfter = shell.PathWaypoints;
        for (var i = 0; i < pointsAfter.Length; i++)
            if ((pointsAfter[i] - pointsBefore[i] - delta).sqrMagnitude > 1e-8f) moved = false;
        var afterOk = Flat(shell.FarDoorPoint - mouth) < 1e-3f && Mathf.Abs(shell.FarDoorPoint.y - floorY) < 1e-4f
            && Mathf.Abs(shell.ExitDoorPoint.y - (floorY + shell.FloorToFloor)) < 1e-4f && shell.RebaseCalls == 1 && (shell.LastDelta - delta).sqrMagnitude < 1e-10f
            && moved && shell.OnFarLanding(shell.FarDoorPoint + forward * .8f) && !shell.OnFarLanding(shell.FarDoorPoint + forward * .8f + Vector3.up * shell.FloorToFloor)
            && Mathf.Abs(shell.FarCeiling - MapGrid.CeilingHeight(world.ZoneOf(n).height)) < 1e-4f && shell.Paper == world.WallPaperOf(n);
        Check(afterOk, label + "after the move (+" + shell.FloorToFloor + " m, Rebased once): FarDoorPoint on the new floor's N'-X1 line at its floor, the exit landing a floor up, "
            + "the waypoints moved by the same delta (" + moved + "), the far landing's trigger at the new floor only; FarCeiling " + shell.FarCeiling + " m (N''s zone), N''s paper");

        // 11. The far door is no USE or EXAMINE target (the interaction line, 2026-11-10, rule 3): the first thing aimed across the line from N' is the
        // shell's leaf, which the map does not know: no door, no kind, no prompt, no architecture, no close-up.
        var eyeAt = mouth - forward * .6f + Vector3.up * 1.5f;
        var aim = new Ray(eyeAt, forward);
        var aimed = Physics.Raycast(aim, out var aimHit, 2.4f, ~0, QueryTriggerInteraction.Ignore);
        var leafHit = aimed && aimHit.collider == shell.FarLeaf;
        var noUse = leafHit && world.DoorOf(aimHit.collider) == null && world.Kind(aimHit.collider, eyeAt, out _, out _) == FrontRoomsMapWorld.AimKind.None
            && world.Describe(aimHit.collider, out _) == null && !world.IsArchitecture(aimHit.collider) && !FrontRoomsInspect.Find(aim, 2.4f, out _);
        Check(noUse, label + "the far door is no USE target: aimed from N' (" + (aimed ? aimHit.collider.name + " at " + aimHit.distance.ToString("0.00") + " m" : "nothing")
            + "), the map has no door, kind or prompt for it and EXAMINE no close-up, so no prompt shows and the touch USE button stays unlit");

        // 12. BS1 on the floor after a walkable stair: the floor's next EXIT is the stair's too, and the station reads the arrival
        // door N' and keeps out of both stairs (or misses with its reason, as A35, the cache left raw).
        FrontRoomsExitPlanner.BandFor(2, out var lo2, out var hi2);
        var startArea2 = new CellRect(x1.x, x1.y, 1, 2);
        var stationCache = FrontRoomsExitPlanner.CacheFor(profile.generation, profile.ModuleData(), seed2, 1);
        var exit2 = FrontRoomsExitPlanner.ChooseStairExit(stationCache, startArea2, n, seed2, lo2, hi2);
        if (!exit2.found) { Check(false, label + "floor 2 (seed " + seed2 + "): ChooseStairExit found no EXIT from N' " + n); return; }
        var station = FrontRoomsFloorStation.Plan(stationCache, BsTemplate(ZoneTheme.Level0), BsTemplate(ZoneTheme.Office), startArea2, n, exit2);
        var exitArea2 = exit2.ExitArea;
        var stationOk = exit2.stair && !FrontRoomsExitPlanner.InArea(startArea2, exit2.exitCell) && !FrontRoomsExitPlanner.InArea(startArea2, exit2.TurnCell);
        var detail = "";
        if (station.Ready)
        {
            var s = station.layout.station;
            var gap = int.MaxValue;
            for (var j = s.y; j < s.y + s.h; j++)
            for (var i = s.x; i < s.x + s.w; i++)
            {
                var c = new GridCoord(i, j);
                if (FrontRoomsExitPlanner.InArea(startArea2, c) || FrontRoomsExitPlanner.InArea(exitArea2, c)) stationOk = false;
                var dx = Math.Max(Math.Max(exitArea2.x - i, i - (exitArea2.x + exitArea2.w - 1)), 0);
                var dy = Math.Max(Math.Max(exitArea2.y - j, j - (exitArea2.y + exitArea2.h - 1)), 0);
                gap = Math.Min(gap, Math.Max(dx, dy));
            }
            if (station.floor.walkCells < FrontRoomsFloorStation.WalkMin || station.floor.walkCells > FrontRoomsFloorStation.WalkMax || gap < FrontRoomsFloorStation.ExitGapCells) stationOk = false;
            detail = "a station " + station.floor.walkCells + " cells on foot from N' (" + FrontRoomsFloorStation.WalkMin + ".." + FrontRoomsFloorStation.WalkMax + "), " + gap
                + " cells from the next X1-X2 (at least " + FrontRoomsFloorStation.ExitGapCells + "), in neither stair";
        }
        else
        {
            if (station.error == null || stationCache.Carve != null) stationOk = false;
            detail = "no fit (" + station.error + "), the cache left raw";
        }
        Check(stationOk, label + "BS1 after a walkable stair (floor 2, seed " + seed2 + ", start area X1-X2, door N' " + n + "): the next EXIT X1 " + exit2.exitCell + " (stair " + exit2.stair
            + ") outside the start area; " + detail);
    }

    // The map's exit group for the walkable stair, worked out again from the lamp's cell and range (ExitLampGroup over X1-X2).
    static bool InStairExitGroup(FrontRoomsMapWorld world, GridCoord x1, Light light)
    {
        var cell = world.CellOf(light.transform.position);
        var cs = MapGrid.CellSize;
        var cx = (cell.x + .5f) * cs;
        var cz = (cell.y + .5f) * cs;
        var dx = Mathf.Max(x1.x * cs - cx, 0f, cx - (x1.x + 1) * cs);
        var dz = Mathf.Max(x1.y * cs - cz, 0f, cz - (x1.y + 2) * cs);
        return dx * dx + dz * dz < light.range * light.range;
    }

    // The walkable stair's start lamp group, worked out again (StartLampGroup with stair): 0 out of range of X1-X2, 1 on the door
    // row of N' or south of it (the far door's side), 2 behind it.
    static int StairStartGroup(FrontRoomsMapWorld world, GridCoord x1, GridCoord door, Light light)
    {
        if (!InStairExitGroup(world, x1, light)) return 0;
        return world.CellOf(light.transform.position).y <= door.y ? 1 : 2;
    }
}
