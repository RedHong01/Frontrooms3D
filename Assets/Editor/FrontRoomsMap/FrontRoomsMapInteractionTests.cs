using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FrontRooms.Map;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The map's interaction hooks and the module build rules, checked without
/// playing the game:
/// - the Relay's door blows: BlowIndex 0..4 at 0.5 / 1.0 / 1.5 / 2.0 / 2.4 s
///   into BreakDoor, the door giving at 2.5 s (the rig's DoorBlow contract);
/// - DoorSqueeze: 0 at a shut door, near 1 on the line of the open one it
///   walks through, 0 in the middle of a room;
/// - ListenPoint: the last noise heard;
/// - DoorUnlocked (doors need keys): a locked door stays shut, raises
///   DoorLocked and rattles (two leaf jolts toward its swing side, the hinge
///   still); with the key it raises DoorUnlocked once, with the zone and
///   the lock point on the opener's side, then swings after UnlockSwingDelay,
///   to its swing side; a second use while the key turns does nothing, and
///   later uses are plain; with no delay it swings at once; with keys off it
///   never raises;
/// - single-acting doors: each door's swing side is its edge's (the same after
///   a rebuild and a revisit shift, both sides occur); a push waits for the
///   lever (Open.SwingStart, a second E ignored), eases out to 95° with the 2°
///   overshoot and rests at Open.Duration on its swing side; PassageBetween is
///   ClosedDoor below 70° and Open from there; a shut lands in the frame,
///   latched; a pull raises DoorPulled first with a clear spot (out of the
///   sweep, latch side, in the keep-clear strip and the room), waits the pull
///   beat, and a leaf never ends inside a body standing in its way (the
///   player's or the Relay's): it stops on it and goes on once it is clear;
///   an open door is rebuilt open on its side;
/// - the Relay breaks from both sides: it stands out of the sweep on the swing
///   side, faces the door, IsBeingBroken hides the prompt and blocks E, each
///   blow jolts the leaf child, DoorBrokenFrom reports the side, and the leaf
///   is thrown past the stop to its swing side and rests at 80°, 3° crooked;
///   the hinge never turns more than 40° in a frame, even at 4 fps;
/// - a break the Relay leaves: a door opened under it is not broken, the mark
///   clears and it walks through; placed away mid-break, the mark clears;
/// - the game's pull step (PullStepSpeed for at most PullStepSeconds) gets the
///   player clear before the leaf moves; a player beside the hinge, past the
///   open leaf, is not moved and never touched;
/// - module props on a column are left out of the build;
/// - the Office kit filling a module keeps off its inner walls and inner doorways;
/// - a chunk whose build throws is undone, logged, not retried while in range,
///   and counts as done for ReadyAround / Settled;
/// - P4 tiers: the Relay's effective tuning per tier, blows 5 → 3, a break keeps
///   its length when the tier changes mid-break, zones → tier, chunks keep the
///   tier they were generated at (until a shift), module tier ranges, lamp odds;
/// - P4 markers: a module key spot takes the zone key; the Relay appears at a
///   module Relay entry behind the player and raises Arrived with its tag;
/// - P4 live tuning: ApplyLive moves the build radius; ReplaceModule rebuilds
///   only the module's chunk;
/// - lamp overrides v1, on the desktop and the WebGL tick path: idle lamps are
///   untouched (LampLevel == LampBaseLevel); an override's attack, hold and
///   release; overlapping overrides take the lowest multiplier; Reduce
///   flashing slows the attack; LampDipped and FixtureChanged; SetLampMode
///   kills and promotes live and survives a rebuild, a drop and a shift;
///   LampModeOf never generates a chunk;
/// - the glass break (the visual chat's contract): an unscaled window root
///   facing cell b with the pane under it; the impact kept 0.2 m inside the
///   glass and fixed once cracked; each crack once, never replayed; a release
///   drops to the stage reached; the break record survives a rebuild; the
///   shatter removes only the pane, with WindowShattered and GlassBroken.
/// Writes Verification/map-interaction-tests.json.
/// Headless: -executeMethod FrontRoomsMapInteractionTests.RunBatch -quit (throws on FAIL).
/// </summary>
public static class FrontRoomsMapInteractionTests
{
    const float Dt = 1f / 60f;

    [Serializable]
    sealed class Report
    {
        public string verdict;
        public int passed, failed;
        public List<string> checks = new List<string>();
    }

    static Report report;

    static void Check(bool ok, string what)
    {
        if (ok) report.passed++; else report.failed++;
        report.checks.Add((ok ? "ok   " : "FAIL ") + what);
    }

    [MenuItem("FrontRooms/Map/Test map interactions")]
    public static void Run() => Execute(false);

    public static void RunBatch() => Execute(true);

    static void Execute(bool throwOnFail)
    {
        report = new Report();
        var ambientMode = RenderSettings.ambientMode;
        var sky = RenderSettings.ambientSkyColor;
        var equator = RenderSettings.ambientEquatorColor;
        var ground = RenderSettings.ambientGroundColor;
        var reflection = RenderSettings.reflectionIntensity;
        var fog = RenderSettings.fog;
        var fogMode = RenderSettings.fogMode;
        var fogColor = RenderSettings.fogColor;
        var fogDensity = RenderSettings.fogDensity;
        var skybox = RenderSettings.skybox;
        var roots = new List<GameObject>();
        var profiles = new List<FrontRoomsLevelProfile>();
        try
        {
            Relay(roots);
            SingleActing(roots);
            RelayBreaks(roots);
            RelayBreakAborts(roots);
            Keys(roots, profiles, 1.1f);
            Keys(roots, profiles, 0f);
            Columns(roots, profiles);
            InnerWalls(roots, profiles);
            FillKeepsClear(roots, profiles);
            BuildGuard(roots);
            Tiers(roots, profiles);
            KeySpot(roots, profiles);
            RelayEntry(roots, profiles);
            Live(roots, profiles);
            Lamps(roots);
            Glass(roots);
        }
        catch (Exception e)
        {
            Check(false, "exception: " + e.GetType().Name + " " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            foreach (var r in roots)
            {
                if (r == null) continue;
                // Free the chunk meshes and the map's own materials (OnDestroy does not run for it in edit mode).
                var w = r.GetComponent<FrontRoomsMapWorld>();
                if (w != null) w.Release();
                UnityEngine.Object.DestroyImmediate(r);
            }
            foreach (var p in profiles) if (p != null) UnityEngine.Object.DestroyImmediate(p);
            RenderSettings.ambientMode = ambientMode;
            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = equator;
            RenderSettings.ambientGroundColor = ground;
            RenderSettings.reflectionIntensity = reflection;
            RenderSettings.fog = fog;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.skybox = skybox;
            DynamicGI.UpdateEnvironment();
        }

        var pass = report.failed == 0 && report.passed > 0;
        report.verdict = (pass ? "PASS" : "FAIL") + ": " + report.passed + " passed, " + report.failed + " failed";
        var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "map-interaction-tests.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        var text = "[MapInteractionTests] " + report.verdict + "\n" + string.Join("\n", report.checks);
        if (pass) Debug.Log(text); else Debug.LogError(text);
        if (throwOnFail && !pass) throw new Exception(report.verdict);
    }

    static readonly GridCoord[] Steps = { new GridCoord(1, 0), new GridCoord(-1, 0), new GridCoord(0, 1), new GridCoord(0, -1) };

    static FrontRoomsMapWorld World(List<GameObject> roots, FrontRoomsLevelProfile profile, float period, string name)
    {
        var root = new GameObject(name) { hideFlags = HideFlags.DontSave };
        roots.Add(root);
        // Far from the open scene, on whole world periods so surfaces stay on the lattice.
        root.transform.position = new Vector3(period * ModuleUnits.WorldPeriod, 0f, -27f * ModuleUnits.WorldPeriod);
        var world = root.AddComponent<FrontRoomsMapWorld>();
        world.Profile = profile;
        return world;
    }

    /// <summary>A closed door between built cells a (near) and b, with an open cell c behind a; optionally one the test picks (door, near cell).</summary>
    static bool FindDoor(FrontRoomsMapWorld world, out GridCoord a, out GridCoord b, out GridCoord c, Func<FrontRoomsMapWorld.Door, GridCoord, bool> where = null)
    {
        a = b = c = default;
        var mid = world.CellOf(world.SpawnWorldPosition);
        for (var y = mid.y - 12; y <= mid.y + 12; y++)
        for (var x = mid.x - 12; x <= mid.x + 12; x++)
        {
            var cell = new GridCoord(x, y);
            if (!world.IsBuilt(cell)) continue;
            foreach (var s in Steps)
            {
                var other = cell + s;
                if (!world.IsBuilt(other) || world.Cache.Edge(cell, other) != EdgeKind.Door) continue;
                var door = world.DoorBetween(cell, other);
                if (door == null || door.open || door.broken || (where != null && !where(door, cell))) continue;
                // Directly opposite the door, so the only short route from c to b is through a.
                var behind = new GridCoord(cell.x - s.x, cell.y - s.y);
                if (!world.IsBuilt(behind) || world.PassageBetween(cell, behind) != FrontRoomsMapWorld.Passage.Open) continue;
                a = cell; b = other; c = behind;
                return true;
            }
        }
        return false;
    }

    // ---------- The Relay: blows, squeeze, listen point ----------

    static void Relay(List<GameObject> roots)
    {
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), -27f, "MAP INTERACTION TEST / relay");
        world.BuildForCapture();
        if (!FindDoor(world, out var a, out var b, out var c)) { Check(false, "relay: no closed door with an open cell behind it near the spawn"); return; }
        var door = world.DoorBetween(a, b);

        var player = new GameObject("test player").transform;
        player.SetParent(world.transform, false);
        var body = player.gameObject.AddComponent<CapsuleCollider>();
        body.height = ModuleUnits.PlayerHeight;
        body.radius = ModuleUnits.PlayerRadius;
        body.center = Vector3.up * (ModuleUnits.PlayerHeight * .5f);
        // Blind: it hunts by ear, through the shut door.
        var tuning = new FrontRoomsHunterTuning { sightRange = 0f };
        var hunter = new FrontRoomsMapHunter(world, tuning, body, null, 5);
        Check(hunter.BlowCount == 5, "relay: BlowCount " + hunter.BlowCount + " at breakDoorSeconds " + tuning.breakDoorSeconds + " (expected 5)");

        var target = world.CellCenter(b);
        player.position = target + new Vector3(0f, 0f, 0f);
        Physics.SyncTransforms();
        hunter.DebugPlace(world.CellCenter(c));
        var middleSqueeze = hunter.DoorSqueeze;
        hunter.Noise(target, 1000f);
        Check(hunter.ListenPoint.HasValue && (hunter.ListenPoint.Value - target).sqrMagnitude < 1e-6f, "relay: ListenPoint is the noise it heard");

        var blows = new List<(int index, float at)>();
        var breakAt = -1f;
        var t = 0f;
        var breakStart = -1f;
        hunter.DoorBlow += _ => blows.Add((hunter.BlowIndex, t - breakStart));
        world.DoorBroken += _ => breakAt = t - breakStart;
        var squeezeAtShutDoor = 0f;
        var maxSqueeze = 0f;
        var crossed = false;
        for (; t < 40f; t += Dt)
        {
            hunter.Tick(Dt, target, target + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
            world.TickDoorsForTools(Dt);
            Physics.SyncTransforms();
            // StateTime is 0 on the tick it enters BreakDoor; later events are logged as StateTime.
            if (hunter.State == HunterState.BreakDoor && breakStart < 0f) breakStart = t - hunter.StateTime;
            if (hunter.State == HunterState.BreakDoor && !door.broken) squeezeAtShutDoor = Mathf.Max(squeezeAtShutDoor, hunter.DoorSqueeze);
            if (door.broken) maxSqueeze = Mathf.Max(maxSqueeze, hunter.DoorSqueeze);
            if (world.CellOf(hunter.Position) == b) crossed = true;
            if (crossed && hunter.State == HunterState.Search) break;
        }
        Check(breakStart >= 0f && door.broken, "relay: it broke into the door " + a + "→" + b);
        var expected = new[] { .5f, 1f, 1.5f, 2f, tuning.breakDoorSeconds - .1f };
        Check(blows.Count == 5, "relay: " + blows.Count + " blows (expected 5): " + string.Join(", ", blows.Select(x => x.index + "@" + x.at.ToString("0.000"))));
        for (var i = 0; i < Mathf.Min(5, blows.Count); i++)
            Check(blows[i].index == i && Mathf.Abs(blows[i].at - expected[i]) <= Dt * .6f, "relay: blow " + blows[i].index + " at " + blows[i].at.ToString("0.000") + " s (expected " + i + " at " + expected[i].ToString("0.00") + ")");
        Check(Mathf.Abs(breakAt - tuning.breakDoorSeconds) <= Dt * .6f && blows.Count > 0 && breakAt > blows[blows.Count - 1].at + .05f,
            "relay: the door gives at " + breakAt.ToString("0.000") + " s, after the last blow (expected " + tuning.breakDoorSeconds + ")");
        Check(squeezeAtShutDoor == 0f, "relay: DoorSqueeze 0 while the door is shut (" + squeezeAtShutDoor.ToString("0.00") + ")");
        Check(crossed && maxSqueeze >= .85f && maxSqueeze <= 1f, "relay: walking through the broken door, DoorSqueeze peaks at " + maxSqueeze.ToString("0.00") + " (expected ≥ 0.85)");
        Check(middleSqueeze == 0f, "relay: DoorSqueeze 0 in the middle of a cell (" + middleSqueeze.ToString("0.00") + ")");
    }

    // ---------- Keys: DoorUnlocked and the swing delay ----------

    static void Keys(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles, float delay)
    {
        var label = "keys (delay " + delay.ToString("0.0") + " s): ";
        var profile = UnityEngine.Object.Instantiate(FrontRoomsLevelProfiles.Resolve());
        profile.hideFlags = HideFlags.DontSave;
        profile.doorsNeedKeys = true;
        profiles.Add(profile);
        var world = World(roots, profile, delay > 0f ? -28f : -29f, "MAP INTERACTION TEST / keys");
        world.BuildForCapture();
        world.UnlockSwingDelay = delay;
        if (!FindDoor(world, out var a, out var b, out _)) { Check(false, label + "no closed door near the spawn"); return; }
        var door = world.DoorBetween(a, b);
        var player = world.Player;
        player.position = world.CellCenter(a) + Vector3.up * .05f;

        var unlocked = new List<(FrontRoomsMapWorld.Door door, GridCoord zone, Vector3 at)>();
        var locked = 0;
        var moved = 0;
        world.DoorUnlocked += (d, z, p) => unlocked.Add((d, z, p));
        world.DoorLocked += _ => locked++;
        world.DoorMoved += _ => moved++;

        world.Use(door.leaf);
        Check(locked == 1 && !door.open && unlocked.Count == 0, label + "without the key the door stays shut and raises DoorLocked (locked " + locked + ", open " + door.open + ")");
        // The rattle: two jolts of the leaf child toward the swing side (the stops hold the other way), at Rattle.Jolt1 and Jolt2; the hinge never moves.
        var home = door.leaf.transform.localPosition;
        var towardSwing = Vector3.right * -door.swing;
        var joltStarts = new List<float>();
        var maxShift = 0f;
        var hingeStill = true;
        var wasJolting = false;
        for (var k = 1; k <= 30; k++)
        {
            world.TickDoorsForTools(Dt);
            var offset = door.leaf.transform.localPosition - home;
            maxShift = Mathf.Max(maxShift, Vector3.Dot(offset, towardSwing));
            var jolting = offset.sqrMagnitude > 1e-12f;
            if (jolting && !wasJolting) joltStarts.Add(k * Dt);
            wasJolting = jolting;
            if (Quaternion.Angle(door.hinge.localRotation, door.closed) > 1e-3f || door.angle != 0f) hingeStill = false;
        }
        Check(joltStarts.Count == 2 && Mathf.Abs(joltStarts[0] - FrontRoomsShotTimings.Rattle.Jolt1) <= Dt * 1.1f && Mathf.Abs(joltStarts[1] - FrontRoomsShotTimings.Rattle.Jolt2) <= Dt * 1.1f
            && maxShift > FrontRoomsShotTimings.Rattle.JoltMetres * .5f && hingeStill && door.leaf.transform.localPosition == home,
            label + "the rattle jolts the leaf twice toward its swing side (at " + string.Join(", ", joltStarts.Select(x => x.ToString("0.000"))) + " s, up to "
            + (maxShift * 1000f).ToString("0.0") + " mm), the hinge still, and it settles home");

        // Give the key of the player's zone (as walking over it would).
        var zone = world.ZoneOf(a).id;
        var keys = (HashSet<GridCoord>)typeof(FrontRoomsMapWorld).GetField("keysHeld", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(world);
        keys.Add(zone);
        world.Use(door.leaf);
        Check(unlocked.Count == 1 && unlocked[0].door == door && unlocked[0].zone == zone, label + "with the key: DoorUnlocked once, for this door and zone " + zone);
        if (unlocked.Count > 0)
        {
            var at = unlocked[0].at;
            var rotation = door.hinge.parent.rotation * door.closed;
            var along = rotation * Vector3.forward;
            var across = rotation * Vector3.right;
            var offset = at - door.position;
            var playerSide = Mathf.Sign(Vector3.Dot(player.position - door.position, across));
            var face = Vector3.Dot(offset, across);
            Check(Mathf.Abs(offset.y - ModuleUnits.DoorHandleHeight) < .01f
                && Mathf.Abs(Vector3.Dot(offset, along) - (ModuleUnits.DoorWidth * .5f - ModuleUnits.DoorHandleInset)) < .01f
                && Mathf.Sign(face) == playerSide && Mathf.Abs(Mathf.Abs(face) - (ModuleUnits.DoorLeafThickness * .5f + ModuleUnits.DoorHandleProud)) < .01f,
                label + "lock point " + offset.ToString("F3") + " from the opening: 1.0 m up, 0.08 m in from the latch jamb, on the player's face");
        }
        if (delay > 0f)
        {
            Check(!door.open, label + "the door waits while the key turns");
            world.Use(door.leaf);
            Check(unlocked.Count == 1 && !door.open, label + "a second use while the key turns does nothing");
            for (var s = 0f; s < delay - .1f; s += Dt) world.TickDoorsForTools(Dt);
            Check(!door.open, label + "still shut just before the delay ends");
            for (var s = 0f; s < .2f; s += Dt) world.TickDoorsForTools(Dt);
            Check(door.open, label + "it swings once the delay is over");
        }
        else Check(door.open, label + "no delay: it swings at once");
        for (var s = 0f; s < 1f; s += Dt) world.TickDoorsForTools(Dt);
        Check(door.angle == ModuleUnits.DoorSwingDegrees && FrontRoomsMapWorld.OnSwingSide(door, door.leaf.transform.position),
            label + "the key opens it to its swing side (" + door.angle.ToString("0.0") + "°)");
        world.Use(door.leaf);
        for (var s = 0f; s < 1f; s += Dt) world.TickDoorsForTools(Dt);
        Check(!door.open, label + "shut again");
        world.Use(door.leaf);
        Check(door.open && unlocked.Count == 1, label + "opening it again is plain: no second DoorUnlocked, no wait");
        // From the other side, whose zone's key the player does not hold: a door a key has opened stays unlocked.
        for (var s = 0f; s < 1f; s += Dt) world.TickDoorsForTools(Dt);
        world.Use(door.leaf);
        for (var s = 0f; s < 1f; s += Dt) world.TickDoorsForTools(Dt);
        player.position = world.CellCenter(b) + Vector3.up * .05f;
        var otherZone = world.ZoneOf(b).id;
        var lockedBefore = locked;
        world.Use(door.leaf);
        Check(otherZone != zone && !keys.Contains(otherZone) && door.open && locked == lockedBefore && unlocked.Count == 1,
            label + "from the far side without its zone's key, the unlocked door opens (no DoorLocked, no second DoorUnlocked)");

        // Keys off: never raised.
        if (delay > 0f) return;
        var keysOff = UnityEngine.Object.Instantiate(FrontRoomsLevelProfiles.Resolve());
        keysOff.hideFlags = HideFlags.DontSave;
        keysOff.doorsNeedKeys = false;
        profiles.Add(keysOff);
        var plain = World(roots, keysOff, -30f, "MAP INTERACTION TEST / no keys");
        plain.BuildForCapture();
        var count = 0;
        plain.DoorUnlocked += (_, __, ___) => count++;
        if (FindDoor(plain, out var pa, out var pb, out _))
        {
            plain.Player.position = plain.CellCenter(pa) + Vector3.up * .05f;
            var d = plain.DoorBetween(pa, pb);
            plain.Use(d.leaf);
            Check(d.open && count == 0, "keys off: the door opens and DoorUnlocked is never raised");
        }
        else Check(false, "keys off: no closed door near the spawn");
    }

    // ---------- Single-acting doors: the fixed side, the lever, pulls, bodies, the passage ----------

    const float HingeStepLimit = FrontRoomsMapWorld.DoorMaxStepDegrees;

    /// <summary>A door's floor plan as the map defines it: x along the wall from the hinge axis to the latch, y out into its swing side.</summary>
    static Vector2 Plan(FrontRoomsMapWorld.Door door, Vector3 p)
    {
        var rotation = door.hinge.parent.rotation * door.closed;
        var offset = p - door.hinge.position;
        return new Vector2(Vector3.Dot(offset, rotation * Vector3.forward), Vector3.Dot(offset, rotation * Vector3.right * -door.swing));
    }

    /// <summary>A point in a door's floor plan, on the floor.</summary>
    static Vector3 FromPlan(FrontRoomsMapWorld.Door door, float along, float outward)
    {
        var rotation = door.hinge.parent.rotation * door.closed;
        var p = door.hinge.position + rotation * Vector3.forward * along + rotation * Vector3.right * (-door.swing * outward);
        p.y = door.hinge.position.y;
        return p;
    }

    /// <summary>Every built door within 16 cells of the spawn, by edge: its swing, and its near (west or south) cell.</summary>
    static Dictionary<long, (float swing, GridCoord a, GridCoord b)> Doors(FrontRoomsMapWorld world)
    {
        var doors = new Dictionary<long, (float, GridCoord, GridCoord)>();
        var mid = world.CellOf(world.SpawnWorldPosition);
        for (var y = mid.y - 16; y <= mid.y + 16; y++)
        for (var x = mid.x - 16; x <= mid.x + 16; x++)
        {
            var cell = new GridCoord(x, y);
            foreach (var s in new[] { new GridCoord(1, 0), new GridCoord(0, 1) })
            {
                var door = world.DoorBetween(cell, cell + s);
                if (door != null) doors[door.edge] = (door.swing, cell, cell + s);
            }
        }
        return doors;
    }

    static CapsuleCollider TestBody(FrontRoomsMapWorld world, string name, float radius)
    {
        var body = new GameObject(name).AddComponent<CapsuleCollider>();
        body.transform.SetParent(world.transform, true);
        body.radius = radius;
        body.height = ModuleUnits.PlayerHeight;
        body.center = Vector3.up * (ModuleUnits.PlayerHeight * .5f);
        return body;
    }

    static bool Overlaps(Collider a, Collider b) =>
        a.enabled && b.enabled && Physics.ComputePenetration(a, a.transform.position, a.transform.rotation, b, b.transform.position, b.transform.rotation, out _, out _);

    static void SingleActing(List<GameObject> roots)
    {
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), -41f, "MAP INTERACTION TEST / single-acting");
        world.BuildForCapture();
        var player = world.Player;

        // 1. The side belongs to the edge: the same after every chunk is rebuilt, and after a revisit shift.
        var before = Doors(world);
        var intoA = before.Values.Count(d => FrontRoomsMapWorld.OnSwingSide(world.DoorBetween(d.a, d.b), world.CellCenter(d.a)));
        Check(before.Count >= 6 && intoA > 0 && intoA < before.Count,
            "single-acting: " + before.Count + " doors, " + intoA + " swing into their west/south cell (both sides occur)");
        var chunks = before.Values.Select(d => MapGrid.ChunkOf(d.a)).Distinct().ToList();
        foreach (var c in chunks) world.RebuildChunk(c);
        var rebuilt = Doors(world);
        var sameAfterRebuild = before.Count(d => rebuilt.TryGetValue(d.Key, out var r) && r.swing == d.Value.swing);
        Check(sameAfterRebuild == before.Count && rebuilt.Count == before.Count,
            "single-acting: after rebuilding " + chunks.Count + " chunks, " + sameAfterRebuild + "/" + before.Count + " doors swing the same way");
        foreach (var c in chunks) { world.Cache.Shift(c); world.RebuildChunk(c); }
        var shifted = Doors(world);
        var common = before.Keys.Where(shifted.ContainsKey).ToList();
        var sameAfterShift = common.Count(k => shifted[k].swing == before[k].swing);
        Check(common.Count >= 3 && sameAfterShift == common.Count,
            "single-acting: after a revisit shift, " + sameAfterShift + "/" + common.Count + " doors still on their edges swing the same way");

        // A door to work: shut, both cells built.
        FrontRoomsMapWorld.Door door = null;
        foreach (var d in Doors(world).Values)
        {
            var candidate = world.DoorBetween(d.a, d.b);
            if (candidate != null && !candidate.open && !candidate.broken) { door = candidate; break; }
        }
        if (door == null) { Check(false, "single-acting: no shut door near the spawn"); return; }
        var swingCell = FrontRoomsMapWorld.OnSwingSide(door, world.CellCenter(door.a)) ? door.a : door.b;
        var moved = 0;
        var pulled = new List<(Vector3 clear, float angleThen, bool openThen)>();
        world.DoorMoved += _ => moved++;
        world.DoorPulled += (d, clear) => { if (d == door) pulled.Add((clear, d.angle, d.open)); };
        var leaf = door.leaf;
        var playerBody = TestBody(world, "test player body", ModuleUnits.PlayerRadius);

        // 2. A push from the stop side: the lever, then the leaf eases out to its swing side, away from the opener.
        player.position = FromPlan(door, .5f, -.9f);
        playerBody.transform.position = player.position;
        Physics.SyncTransforms();
        world.Use(leaf);
        Check(door.open && door.angle == 0f && moved == 0 && pulled.Count == 0,
            "single-acting: push from the stop side: open at once, the leaf waits for the lever (angle " + door.angle + ", DoorMoved " + moved + ", DoorPulled " + pulled.Count + ")");
        float t = 0f, startAt = -1f, maxAngle = 0f, maxStep = 0f, restAt = -1f;
        int movedAtStart = -1, passageWrong = 0, overlapFrames = 0;
        var secondUseIgnored = false;
        for (var k = 1; k <= 90; k++)
        {
            var was = door.angle;
            world.TickDoorsForTools(Dt);
            Physics.SyncTransforms();
            t = k * Dt;
            if (startAt < 0f && door.angle > 0f) { startAt = t; movedAtStart = moved; }
            if (k == 3)
            {
                world.Use(leaf);
                secondUseIgnored = door.open && door.angle == 0f;
            }
            maxAngle = Mathf.Max(maxAngle, door.angle);
            maxStep = Mathf.Max(maxStep, Mathf.Abs(door.angle - was));
            var passage = world.PassageBetween(door.a, door.b);
            if ((door.angle >= FrontRoomsMapWorld.DoorPassableDegrees) != (passage == FrontRoomsMapWorld.Passage.Open)) passageWrong++;
            if (Overlaps(leaf, playerBody)) overlapFrames++;
            if (restAt < 0f && door.angle == ModuleUnits.DoorSwingDegrees && Mathf.Abs(was - door.angle) < 1e-4f && t > .5f) restAt = t - Dt;
        }
        Check(secondUseIgnored, "single-acting: a second E while the lever is down does nothing");
        Check(Mathf.Abs(startAt - FrontRoomsShotTimings.Open.SwingStart) <= Dt * 1.1f && movedAtStart == 1,
            "single-acting: the leaf leaves the frame " + startAt.ToString("0.000") + " s after E (lever " + FrontRoomsShotTimings.Open.SwingStart + " s), with DoorMoved then (" + movedAtStart + ")");
        Check(Mathf.Abs(maxAngle - (ModuleUnits.DoorSwingDegrees + FrontRoomsShotTimings.Open.OvershootDeg)) < .3f && door.angle == ModuleUnits.DoorSwingDegrees
            && Mathf.Abs(restAt - FrontRoomsShotTimings.Open.Duration) <= Dt * 2.1f,
            "single-acting: it eases out onto the stop, overshoots to " + maxAngle.ToString("0.0") + "° and rests at " + door.angle.ToString("0.0") + "° at " + restAt.ToString("0.000") + " s (expected 97 / 95 / " + FrontRoomsShotTimings.Open.Duration + ")");
        Check(FrontRoomsMapWorld.OnSwingSide(door, leaf.transform.position) && !FrontRoomsMapWorld.OnSwingSide(door, player.position) && world.CellOf(leaf.transform.position) == swingCell,
            "single-acting: pushed open, the leaf rests in its swing-side cell " + swingCell + ", away from the opener");
        Check(passageWrong == 0, "single-acting: PassageBetween is ClosedDoor below " + FrontRoomsMapWorld.DoorPassableDegrees + "° and Open from there (" + passageWrong + " frames wrong)");
        Check(world.PassageBetween(door.a, door.b) == FrontRoomsMapWorld.Passage.Open && maxStep <= HingeStepLimit && overlapFrames == 0,
            "single-acting: open at rest; largest hinge step " + maxStep.ToString("0.0") + "°; the opener never touched");

        // 3. Shut: at once, a smooth close that lands in the frame (never through it), latched.
        var movedBefore = moved;
        world.Use(leaf);
        var minAngle = float.MaxValue;
        var shutAt = -1f;
        for (var k = 1; k <= 60; k++)
        {
            world.TickDoorsForTools(Dt);
            minAngle = Mathf.Min(minAngle, door.angle);
            if (shutAt < 0f && door.angle == 0f) shutAt = k * Dt;
        }
        Check(!door.open && moved == movedBefore + 1 && minAngle == 0f && Quaternion.Angle(door.hinge.localRotation, door.closed) < .01f
            && Mathf.Abs(shutAt - FrontRoomsShotTimings.Open.SwingSeconds) <= Dt * 1.1f,
            "single-acting: shut at once (DoorMoved), latched at " + shutAt.ToString("0.000") + " s, lowest angle " + minAngle.ToString("0.00") + "° (never through the frame)");

        // 4. A pull from the swing side, standing in the leaf's sweep: DoorPulled first, with a clear spot; the leaf waits the pull beat and stops on the body.
        player.position = FromPlan(door, .5f, .62f);
        playerBody.transform.position = player.position;
        Physics.SyncTransforms();
        movedBefore = moved;
        world.Use(leaf);
        Check(pulled.Count == 1 && pulled[0].angleThen == 0f && pulled[0].openThen && moved == movedBefore,
            "single-acting: pull: DoorPulled once, before the leaf moves (angle then " + (pulled.Count > 0 ? pulled[0].angleThen.ToString("0.0") : "-") + ")");
        if (pulled.Count == 0) return;
        var clear = pulled[0].clear;
        var plan = Plan(door, clear);
        var reach = ModuleUnits.DoorWidth - ModuleUnits.DoorLeafGap * .5f + ModuleUnits.DoorLeafThickness * .5f + ModuleUnits.PlayerRadius;
        Check(plan.magnitude >= reach + .03f && plan.x >= ModuleUnits.DoorWidth * .5f
            && plan.y >= ModuleUnits.WallHalf + ModuleUnits.PlayerRadius && plan.y <= ModuleUnits.WallHalf + ModuleUnits.DoorClearDepth - ModuleUnits.PlayerRadius
            && plan.x <= (MapGrid.CellSize + ModuleUnits.DoorWidth) * .5f - ModuleUnits.WallHalf - ModuleUnits.PlayerRadius
            && world.CellOf(clear) == swingCell,
            "single-acting: pull: the clear spot is " + plan.magnitude.ToString("0.00") + " m from the hinge (sweep + body " + reach.ToString("0.00") + "), " + plan.x.ToString("0.00")
            + " m along toward the latch, " + plan.y.ToString("0.00") + " m out, inside the keep-clear strip and the cell " + swingCell);
        startAt = -1f;
        overlapFrames = 0;
        var pullIgnored = false;
        maxAngle = 0f;
        for (var k = 1; k <= 120; k++)
        {
            world.TickDoorsForTools(Dt);
            Physics.SyncTransforms();
            if (k == 15)
            {
                world.Use(leaf);
                pullIgnored = door.open && door.angle == 0f;
            }
            if (startAt < 0f && door.angle > 0f) startAt = k * Dt;
            maxAngle = Mathf.Max(maxAngle, door.angle);
            if (Overlaps(leaf, playerBody)) overlapFrames++;
        }
        Check(pullIgnored, "single-acting: pull: a second E during the pull beat does nothing");
        Check(Mathf.Abs(startAt - (FrontRoomsShotTimings.Open.SwingStart + FrontRoomsMapWorld.DoorPullBeatSeconds)) <= Dt * 1.1f,
            "single-acting: pull: the leaf starts " + startAt.ToString("0.000") + " s after E (lever + pull beat " + (FrontRoomsShotTimings.Open.SwingStart + FrontRoomsMapWorld.DoorPullBeatSeconds).ToString("0.00") + ")");
        Check(overlapFrames == 0 && maxAngle > 1f && door.angle < FrontRoomsMapWorld.DoorPassableDegrees
            && world.PassageBetween(door.a, door.b) == FrontRoomsMapWorld.Passage.ClosedDoor,
            "single-acting: pull: a player who stays put stops the leaf on their body at " + door.angle.ToString("0.0") + "°, never inside it; the way stays ClosedDoor");
        // The player steps to the clear spot: the leaf goes on to its stop without touching them.
        player.position = clear;
        playerBody.transform.position = clear;
        Physics.SyncTransforms();
        overlapFrames = 0;
        for (var k = 1; k <= 90; k++)
        {
            world.TickDoorsForTools(Dt);
            Physics.SyncTransforms();
            if (Overlaps(leaf, playerBody)) overlapFrames++;
        }
        Check(overlapFrames == 0 && door.angle == ModuleUnits.DoorSwingDegrees && world.PassageBetween(door.a, door.b) == FrontRoomsMapWorld.Passage.Open
            && FrontRoomsMapWorld.OnSwingSide(door, leaf.transform.position),
            "single-acting: pull: from the clear spot the leaf swings on to its stop (" + door.angle.ToString("0.0") + "°) toward the player, past them");

        // 5. Shutting onto a body in the doorway: the leaf stops on it, short of the frame.
        player.position = FromPlan(door, .5f, 0f);
        playerBody.transform.position = player.position;
        Physics.SyncTransforms();
        world.Use(leaf);
        overlapFrames = 0;
        for (var k = 1; k <= 60; k++)
        {
            world.TickDoorsForTools(Dt);
            Physics.SyncTransforms();
            if (Overlaps(leaf, playerBody)) overlapFrames++;
        }
        Check(!door.open && overlapFrames == 0 && door.angle > 20f,
            "single-acting: shutting on a player in the doorway, the leaf stops on them at " + door.angle.ToString("0.0") + "°");
        // They step out (to the stop side): it shuts and latches.
        player.position = FromPlan(door, .5f, -1.5f);
        playerBody.transform.position = player.position;
        for (var k = 1; k <= 60; k++) world.TickDoorsForTools(Dt);
        Check(door.angle == 0f, "single-acting: once they step out, it shuts and latches");

        // 6. The Relay's body stops a leaf too (RelayBody, as the hunter sets it).
        var relayAt = FromPlan(door, .55f, .6f);
        var relayBody = TestBody(world, "test relay body", ModuleUnits.RelayRadius);
        relayBody.transform.position = relayAt;
        world.RelayBody = () => relayAt;
        player.position = FromPlan(door, .5f, -1.5f);
        world.Use(leaf);
        overlapFrames = 0;
        for (var k = 1; k <= 60; k++)
        {
            world.TickDoorsForTools(Dt);
            Physics.SyncTransforms();
            if (Overlaps(leaf, relayBody)) overlapFrames++;
        }
        var heldAt = door.angle;
        world.RelayBody = null;
        for (var k = 1; k <= 60; k++) world.TickDoorsForTools(Dt);
        Check(overlapFrames == 0 && heldAt > 1f && heldAt < 60f && door.angle == ModuleUnits.DoorSwingDegrees,
            "single-acting: a push stops on the Relay's body at " + heldAt.ToString("0.0") + "° and goes on once it is gone");

        // 8. The game's pull step: the player walks to the clear spot (PullStepSpeed, at most PullStepSeconds),
        //    from the frame after E, and the leaf never meets them on its way to the stop.
        ShutAndLatch(world, door, player, playerBody);
        var stepStart = FromPlan(door, .5f, .62f);
        player.position = stepStart;
        playerBody.transform.position = stepStart;
        Physics.SyncTransforms();
        pulled.Clear();
        world.Use(leaf);
        var stepOk = pulled.Count == 1;
        var stepTo = stepOk ? pulled[0].clear : stepStart;
        var stepped = 0f;
        overlapFrames = 0;
        var bodyStoppedLeaf = false;
        for (var k = 1; k <= 90; k++)
        {
            var was = door.angle;
            if (k >= 2 && stepped <= FrontRoomsMapWorld.PullStepSeconds)
            {
                var to = stepTo - player.position;
                to.y = 0f;
                if (to.magnitude >= 1e-3f)
                {
                    player.position += Vector3.ClampMagnitude(to, FrontRoomsMapWorld.PullStepSpeed * Dt);
                    playerBody.transform.position = player.position;
                    stepped += Dt;
                }
            }
            Physics.SyncTransforms();
            world.TickDoorsForTools(Dt);
            Physics.SyncTransforms();
            if (Overlaps(leaf, playerBody)) overlapFrames++;
            // Once moving, the leaf only goes on toward the stop (a body in the way would hold it).
            if (was > 1f && was < ModuleUnits.DoorSwingDegrees - 1f && door.angle <= was + 1e-4f) bodyStoppedLeaf = true;
        }
        Check(stepOk && overlapFrames == 0 && !bodyStoppedLeaf && door.angle == ModuleUnits.DoorSwingDegrees
            && Flat(player.position - stepTo) < 1e-3f && stepped <= FrontRoomsMapWorld.PullStepSeconds + Dt,
            "single-acting: pull step: the player walks " + Flat(stepTo - stepStart).ToString("0.00") + " m clear in " + stepped.ToString("0.00")
            + " s (≤ " + FrontRoomsMapWorld.PullStepSeconds + "), the leaf swings to " + door.angle.ToString("0.0") + "° without touching or stopping on them");

        // 9. Beside the hinge, past the open leaf: a pull from there moves no one and touches no one.
        ShutAndLatch(world, door, player, playerBody);
        var hingeSide = FromPlan(door, -.5f, .75f);
        player.position = hingeSide;
        playerBody.transform.position = hingeSide;
        Physics.SyncTransforms();
        pulled.Clear();
        world.Use(leaf);
        overlapFrames = 0;
        for (var k = 1; k <= 90; k++)
        {
            world.TickDoorsForTools(Dt);
            Physics.SyncTransforms();
            if (Overlaps(leaf, playerBody)) overlapFrames++;
        }
        Check(door.open && door.pull && pulled.Count == 0 && overlapFrames == 0 && door.angle == ModuleUnits.DoorSwingDegrees,
            "single-acting: from beside the hinge (out of the sweep) a pull raises no DoorPulled (" + pulled.Count + ") and the leaf opens to " + door.angle.ToString("0.0") + "° past them");

        // 7. An open door is rebuilt open, on its stop, on its own side.
        var edge = door.edge;
        world.RebuildChunk(MapGrid.ChunkOf(door.a));
        var again = world.DoorBetween(door.a, door.b);
        Check(again != null && again != door && again.edge == edge && again.open && again.angle == ModuleUnits.DoorSwingDegrees
            && FrontRoomsMapWorld.OnSwingSide(again, again.leaf.transform.position),
            "single-acting: an open door is rebuilt open at 95° on its swing side");
        UnityEngine.Object.DestroyImmediate(playerBody.gameObject);
        UnityEngine.Object.DestroyImmediate(relayBody.gameObject);
    }

    // Shut a door from its stop side and tick it into the frame.
    static void ShutAndLatch(FrontRoomsMapWorld world, FrontRoomsMapWorld.Door door, Transform player, Collider playerBody)
    {
        player.position = FromPlan(door, .5f, -1.5f);
        playerBody.transform.position = player.position;
        Physics.SyncTransforms();
        if (door.open) world.Use(door.leaf);
        for (var k = 1; k <= 90; k++) world.TickDoorsForTools(Dt);
    }

    // ---------- The Relay breaking a door from either side ----------

    static void RelayBreaks(List<GameObject> roots)
    {
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), -42f, "MAP INTERACTION TEST / relay breaks");
        world.BuildForCapture();
        var player = new GameObject("test player").transform;
        player.SetParent(world.transform, false);
        var body = player.gameObject.AddComponent<CapsuleCollider>();
        body.height = ModuleUnits.PlayerHeight;
        body.radius = ModuleUnits.PlayerRadius;
        body.center = Vector3.up * (ModuleUnits.PlayerHeight * .5f);
        var reachRelay = ModuleUnits.DoorWidth - ModuleUnits.DoorLeafGap * .5f + ModuleUnits.DoorLeafThickness * .5f + ModuleUnits.RelayRadius;
        foreach (var fromSwing in new[] { true, false })
        {
            var label = "relay breaks from the " + (fromSwing ? "swing" : "stop") + " side: ";
            if (!FindDoor(world, out var a, out var b, out var c, (d, near) => FrontRoomsMapWorld.OnSwingSide(d, world.CellCenter(near)) == fromSwing))
            {
                Check(false, label + "no closed door with an open cell behind it near the spawn");
                continue;
            }
            var door = world.DoorBetween(a, b);
            // The side its leaf must end on, read before the break: the near cell when the Relay comes from the swing side.
            var swingCell = fromSwing ? a : b;
            var tuning = new FrontRoomsHunterTuning { sightRange = 0f };
            var hunter = new FrontRoomsMapHunter(world, tuning, body, null, 5);
            var target = world.CellCenter(b);
            player.position = target;
            Physics.SyncTransforms();
            hunter.DebugPlace(world.CellCenter(c));
            hunter.Noise(target, 1000f);
            var brokenFrom = new List<(Vector3 at, bool swingSide)>();
            void OnBrokenFrom(FrontRoomsMapWorld.Door d, Vector3 at, bool swingSide) { if (d.edge == door.edge) brokenFrom.Add((at, swingSide)); }
            world.DoorBrokenFrom += OnBrokenFrom;
            float minReach = float.MaxValue, maxAngle = 0f, maxStep = 0f, maxJolt = 0f, minFacing = 1f;
            int markedFrames = 0, breakFrames = 0, promptFrames = 0, standOnSwingSide = 0;
            var useIgnored = true;
            Vector3 stance = default;
            var home = door.leaf.transform.localPosition;
            for (var t = 0f; t < 40f; t += Dt)
            {
                var was = door.angle;
                hunter.Tick(Dt, target, target + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
                world.TickDoorsForTools(Dt);
                Physics.SyncTransforms();
                maxAngle = Mathf.Max(maxAngle, door.angle);
                maxStep = Mathf.Max(maxStep, Mathf.Abs(door.angle - was));
                if (hunter.State == HunterState.BreakDoor && !door.broken)
                {
                    breakFrames++;
                    stance = hunter.Position;
                    if (world.IsBeingBroken(door)) markedFrames++;
                    if (world.Describe(door.leaf, out _) != null) promptFrames++;
                    if (FrontRoomsMapWorld.OnSwingSide(door, hunter.Position)) standOnSwingSide++;
                    minReach = Mathf.Min(minReach, Plan(door, hunter.Position).magnitude);
                    maxJolt = Mathf.Max(maxJolt, (door.leaf.transform.localPosition - home).magnitude);
                    var toDoor = door.position - hunter.Position;
                    toDoor.y = 0f;
                    minFacing = Mathf.Min(minFacing, Vector3.Dot(hunter.Heading, toDoor.normalized));
                    if (breakFrames == 30)
                    {
                        world.Use(door.leaf);
                        useIgnored = !door.open;
                    }
                }
                if (door.broken && hunter.State == HunterState.Search) break;
            }
            world.DoorBrokenFrom -= OnBrokenFrom;
            Check(door.broken && brokenFrom.Count == 1 && brokenFrom[0].swingSide == fromSwing && (brokenFrom[0].at - stance).sqrMagnitude < .01f,
                label + "DoorBrokenFrom once, from the " + (brokenFrom.Count > 0 ? (brokenFrom[0].swingSide ? "swing" : "stop") : "-") + " side, at where it stood");
            Check(breakFrames > 0 && standOnSwingSide == (fromSwing ? breakFrames : 0) && (!fromSwing || minReach >= reachRelay),
                label + "it stands on its own side for the whole break, " + minReach.ToString("0.00") + " m from the hinge" + (fromSwing ? " (out of the sweep: ≥ " + reachRelay.ToString("0.00") + ")" : ""));
            Check(markedFrames == breakFrames && promptFrames == 0 && useIgnored && !world.IsBeingBroken(door),
                label + "while it breaks the door: IsBeingBroken, no prompt, E does nothing; cleared once broken");
            Check(minFacing > .3f, label + "it faces the door while it breaks it (worst " + minFacing.ToString("0.00") + ")");
            Check(maxJolt > FrontRoomsShotTimings.DoorBreak.LeafJoltMinMetres * .5f,
                label + "each blow jolts the leaf child (largest " + (maxJolt * 1000f).ToString("0.0") + " mm)");
            var tilt = Quaternion.Angle(door.leaf.transform.localRotation, Quaternion.identity);
            Check(world.CellOf(door.leaf.transform.position) == swingCell && FrontRoomsMapWorld.OnSwingSide(door, door.leaf.transform.position)
                && Mathf.Abs(door.angle - FrontRoomsShotTimings.DoorBreak.BounceRestDeg) < .01f
                && maxAngle >= FrontRoomsMapWorld.DoorBreakThrowDegrees - 1f && maxStep <= HingeStepLimit && Mathf.Abs(tilt - FrontRoomsShotTimings.DoorBreak.CrookedDeg) < .05f,
                label + "the leaf is thrown to " + maxAngle.ToString("0.0") + "° into its swing-side cell " + swingCell + " (" + (fromSwing ? "toward" : "away from") + " the Relay) and rests at " + door.angle.ToString("0.0") + "°, " + tilt.ToString("0.0") + "° crooked; largest hinge step " + maxStep.ToString("0.0") + "°");
        }

        // A break seen at a very low frame rate: the hinge never turns more than DoorMaxStepDegrees in one frame.
        if (FindDoor(world, out var la, out var lb, out _))
        {
            var door = world.DoorBetween(la, lb);
            var maxStep = 0f;
            world.BreakDoor(door, world.CellCenter(la));
            for (var k = 0; k < 8; k++)
            {
                var was = door.angle;
                world.TickDoorsForTools(.25f);
                maxStep = Mathf.Max(maxStep, Mathf.Abs(door.angle - was));
            }
            Check(maxStep <= HingeStepLimit && Mathf.Abs(door.angle - FrontRoomsShotTimings.DoorBreak.BounceRestDeg) < .01f,
                "relay breaks: at 4 fps the hinge turns at most " + maxStep.ToString("0.0") + "° a frame and still rests at " + door.angle.ToString("0.0") + "°");
        }
        else Check(false, "relay breaks: no third closed door for the low frame rate break");
    }

    // ---------- The Relay leaving a break ----------

    static void RelayBreakAborts(List<GameObject> roots)
    {
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), -43f, "MAP INTERACTION TEST / break aborts");
        world.BuildForCapture();
        var player = new GameObject("test player").transform;
        player.SetParent(world.transform, false);
        var body = player.gameObject.AddComponent<CapsuleCollider>();
        body.height = ModuleUnits.PlayerHeight;
        body.radius = ModuleUnits.PlayerRadius;
        body.center = Vector3.up * (ModuleUnits.PlayerHeight * .5f);
        foreach (var placed in new[] { false, true })
        {
            var label = placed ? "break abort, placed away mid-break: " : "break abort, the door opened under it: ";
            if (!FindDoor(world, out var a, out var b, out var c))
            {
                Check(false, label + "no closed door with an open cell behind it near the spawn");
                continue;
            }
            var door = world.DoorBetween(a, b);
            var tuning = new FrontRoomsHunterTuning { sightRange = 0f };
            var hunter = new FrontRoomsMapHunter(world, tuning, body, null, 5);
            var target = world.CellCenter(b);
            player.position = target;
            Physics.SyncTransforms();
            hunter.DebugPlace(world.CellCenter(c));
            hunter.Noise(target, 1000f);
            var brokenEvents = 0;
            void OnBrokenFrom(FrontRoomsMapWorld.Door d, Vector3 at, bool swingSide) { if (d.edge == door.edge) brokenEvents++; }
            world.DoorBrokenFrom += OnBrokenFrom;
            try
            {
                var began = false;
                for (var t = 0f; t < 20f && !began; t += Dt)
                {
                    hunter.Tick(Dt, target, target + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
                    world.TickDoorsForTools(Dt);
                    Physics.SyncTransforms();
                    began = hunter.State == HunterState.BreakDoor;
                }
                if (!began || !world.IsBeingBroken(door))
                {
                    Check(false, label + "the Relay never began the break");
                    continue;
                }
                if (placed)
                {
                    hunter.DebugPlace(world.CellCenter(c));
                    Check(!world.IsBeingBroken(door) && world.Describe(door.leaf, out _) == "E  ·  OPEN DOOR" && hunter.State != HunterState.BreakDoor,
                        label + "the mark clears at once and the door takes E again");
                    continue;
                }
                // Already swinging when the break began: the leaf opens from the far side under the Relay's hands.
                world.MarkBeingBroken(door, false);
                var opened = world.TryOpenDoor(b, a);
                world.MarkBeingBroken(door, true);
                var crossed = false;
                var leftBreak = -1f;
                for (var t = 0f; t < 8f && !crossed; t += Dt)
                {
                    hunter.Tick(Dt, target, target + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
                    world.TickDoorsForTools(Dt);
                    Physics.SyncTransforms();
                    if (leftBreak < 0f && hunter.State != HunterState.BreakDoor) leftBreak = t;
                    crossed = world.CellOf(hunter.Position) == b;
                }
                Check(opened && !door.broken && hunter.DoorsBroken == 0 && brokenEvents == 0 && !world.IsBeingBroken(door)
                    && world.Describe(door.leaf, out _) == "E  ·  SHUT DOOR" && leftBreak >= 0f && leftBreak < tuning.breakDoorSeconds && crossed,
                    label + "not broken (" + hunter.DoorsBroken + " breaks, " + brokenEvents + " events), the mark clears, the Relay leaves the break after "
                    + leftBreak.ToString("0.00") + " s and walks through" + (crossed ? "" : " (it never crossed)"));
            }
            finally
            {
                world.DoorBrokenFrom -= OnBrokenFrom;
            }
        }
    }

    // ---------- Lamp overrides (interface v1) ----------

    static void Lamps(List<GameObject> roots)
    {
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), -44f, "MAP INTERACTION TEST / lamps");
        world.BuildForCapture();
        var nearOnly = FrontRoomsMapWorld.TickFixturesNearOnly;
        var flashing = FrontRoomsSettings.ReduceFlashing;
        var used = new HashSet<GridCoord>();
        try
        {
            if (FrontRoomsSettings.ReduceFlashing) FrontRoomsSettings.SetReduceFlashing(false);
            foreach (var webgl in new[] { false, true })
            {
                FrontRoomsMapWorld.TickFixturesNearOnly = webgl;
                LampChecks(world, used, webgl ? "lamps (WebGL path): " : "lamps: ");
            }
        }
        finally
        {
            FrontRoomsMapWorld.TickFixturesNearOnly = nearOnly;
            if (FrontRoomsSettings.ReduceFlashing != flashing) FrontRoomsSettings.SetReduceFlashing(flashing);
        }
    }

    static void LampTicks(FrontRoomsMapWorld world, float seconds)
    {
        for (var t = 0f; t < seconds - 1e-4f; t += Dt) world.TickFixturesForTools(Dt);
    }

    static float LampRatio(FrontRoomsMapWorld world, GridCoord c) => world.LampLevel(c) / Mathf.Max(world.LampBaseLevel(c), 1e-6f);

    static void LampChecks(FrontRoomsMapWorld world, HashSet<GridCoord> used, string label)
    {
        // Steady lamps near the spawn, a fresh one for each check.
        var mid = world.CellOf(world.SpawnWorldPosition);
        var steady = new List<GridCoord>();
        for (var y = mid.y - 10; y <= mid.y + 10; y++)
        for (var x = mid.x - 10; x <= mid.x + 10; x++)
        {
            var c = new GridCoord(x, y);
            if (used.Contains(c) || world.LampLevel(c) == FrontRoomsMapWorld.NoLamp || world.LampModeOf(c) != ModuleLamp.Steady) continue;
            steady.Add(c);
        }
        if (steady.Count < 6) { Check(false, label + "only " + steady.Count + " steady lamps near the spawn"); return; }
        foreach (var c in steady.Take(6)) used.Add(c);
        GridCoord a = steady[0], b = steady[1], cc = steady[2], d = steady[3], e = steady[4];

        // Idle: the override layer changes nothing.
        LampTicks(world, .5f);
        var idleWrong = 0;
        foreach (var c in steady) if (world.LampLevel(c) != world.LampBaseLevel(c)) idleWrong++;
        Check(world.LampOverrideCount == 0 && idleWrong == 0, label + "idle, every lamp's level is its own (" + idleWrong + " of " + steady.Count + " differ)");

        // An override's envelope: Dip 0.3 s attack, 0.5 s hold, 1.2 s release.
        int dipped = 0, changed = 0;
        var dippedAt = Vector3.zero;
        void OnDipped(GridCoord c, Vector3 p) { if (c == a) { dipped++; dippedAt = p; } }
        void OnChanged(GridCoord c, float level) { if (c == a) changed++; }
        world.LampDipped += OnDipped;
        world.FixtureChanged += OnChanged;
        world.SetLampOverride(a, FrontRoomsMapWorld.LampFx.Dip, .3f, .5f);
        LampTicks(world, .15f);
        var attack = LampRatio(world, a);
        LampTicks(world, .35f);
        var held = LampRatio(world, a);
        LampTicks(world, .3f + 1.2f + .05f);
        var after = LampRatio(world, a);
        world.LampDipped -= OnDipped;
        world.FixtureChanged -= OnChanged;
        var flat = world.CellCenter(a) - dippedAt;
        flat.y = 0f;
        Check(attack > .55f && attack < .75f && Mathf.Abs(held - .3f) < 1e-4f && after == 1f && world.LampLevel(a) == world.LampBaseLevel(a) && world.LampOverrideCount == 0,
            label + "a dip eases in (" + attack.ToString("0.00") + " at 0.15 s), holds at 0.30 (" + held.ToString("0.0000") + "), and is gone after its release (" + after.ToString("0.00") + ")");
        // Down through 0.8 and 0.55, and back up through both: four crossings, no chatter from the shimmer.
        Check(dipped == 1 && flat.magnitude < MapGrid.CellSize && changed == 4,
            label + "LampDipped once at the lamp; FixtureChanged once per line crossed, down and back (" + changed + " of 4)");

        // Stacking: the lowest multiplier, never the product; each releases on its own.
        var sag = world.SetLampOverride(b, FrontRoomsMapWorld.LampFx.Sag, .5f, float.PositiveInfinity);
        var dip = world.SetLampOverride(b, FrontRoomsMapWorld.LampFx.Dip, .3f, float.PositiveInfinity);
        LampTicks(world, 1f);
        var both = LampRatio(world, b);
        world.RemoveLampOverride(dip);
        LampTicks(world, 1.3f);
        var sagOnly = LampRatio(world, b);
        world.RemoveLampOverride(sag);
        LampTicks(world, 1.6f);
        Check(Mathf.Abs(both - .3f) < 1e-4f && Mathf.Abs(sagOnly - .5f) < 1e-4f && LampRatio(world, b) == 1f && world.LampOverrideCount == 0,
            label + "overlapping overrides take the lowest (" + both.ToString("0.000") + ", not 0.15); removing one leaves the other (" + sagOnly.ToString("0.000") + "); both gone, back to 1");

        // Reduce flashing: no attack faster than 0.5 s, even a warning's 0.08 s.
        FrontRoomsSettings.SetReduceFlashing(true);
        world.SetLampOverride(cc, FrontRoomsMapWorld.LampFx.Warn, .3f, .2f);
        FrontRoomsSettings.SetReduceFlashing(false);
        LampTicks(world, .1f);
        var calm = LampRatio(world, cc);
        LampTicks(world, 2f);
        Check(calm > .8f && world.LampOverrideCount == 0, label + "with Reduce flashing a warning dip eases in over 0.5 s (" + calm.ToString("0.00") + " at 0.1 s, not 0.3)");

        // A promotion starts a fresh cycle: a Steady lamp turned Dead does not blink on its old timer.
        world.SetLampMode(e, ModuleLamp.Dead);
        var blinks = 0;
        for (var t = 0f; t < 1.9f; t += Dt)
        {
            world.TickFixturesForTools(Dt);
            if (world.LampBaseLevel(e) > 0f) blinks++;
        }
        world.SetLampMode(e, ModuleLamp.Auto);
        Check(blinks == 0 && world.LampModeOf(e) == ModuleLamp.Steady, label + "promoted to Dead, the lamp stays dark for its first 2 s (" + blinks + " lit frames)");

        // The kill: a dark lens, kept through a rebuild, a drop and a shift; Auto brings the lamp back.
        var chunk = MapGrid.ChunkOf(d);
        world.SetLampMode(d, ModuleLamp.Off);
        LampTicks(world, .05f);
        var live = world.LampModeOf(d) == ModuleLamp.Off && world.LampBaseLevel(d) == 0f && world.LampLevel(d) == 0f;
        world.RebuildChunk(chunk);
        LampTicks(world, .05f);
        var rebuilt = world.LampLevel(d) == 0f;
        world.DropChunkForTools(chunk);
        var dropped = world.LampLevel(d) == FrontRoomsMapWorld.NoLamp;
        world.RebuildChunk(chunk);
        LampTicks(world, .05f);
        var back = world.LampLevel(d) == 0f;
        world.SetLampMode(d, ModuleLamp.Auto);
        LampTicks(world, .05f);
        var revived = world.LampModeOf(d) == ModuleLamp.Steady && world.LampBaseLevel(d) > .9f;
        Check(live && rebuilt && dropped && back && revived,
            label + "SetLampMode Off: dark at once (" + live + "), after a rebuild (" + rebuilt + "), not there while dropped (" + dropped + "), dark again when rebuilt (" + back + "); Auto brings it back steady (" + revived + ")");
        world.SetLampMode(d, ModuleLamp.Off);
        world.Cache.Shift(chunk);
        world.RebuildChunk(chunk);
        LampTicks(world, .05f);
        Check(world.LampModeOf(d) == ModuleLamp.Off && (world.LampLevel(d) == FrontRoomsMapWorld.NoLamp || world.LampLevel(d) == 0f),
            label + "a killed lamp stays dark after a revisit shift");
        world.SetLampMode(d, ModuleLamp.Auto);

        // Pure: asking about a far, never-built cell generates nothing.
        var generated = world.Cache.Built.Count();
        var far = new GridCoord(mid.x + 4000, mid.y - 4000);
        var farMode = world.LampModeOf(far);
        Check(world.Cache.Built.Count() == generated && world.LampLevel(far) == FrontRoomsMapWorld.NoLamp && farMode != ModuleLamp.Auto,
            label + "LampModeOf a far cell (" + farMode + ") generates no chunk (" + generated + " before and after)");
    }

    // ---------- The glass break (map side of the visual chat's contract) ----------

    static void Glass(List<GameObject> roots)
    {
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), -45f, "MAP INTERACTION TEST / glass");
        var built = new List<(FrontRoomsMapWorld.Window window, FrontRoomsMapWorld.GlassBreakRecord record)>();
        var released = new List<FrontRoomsMapWorld.Window>();
        world.WindowBuilt += (w, r) => built.Add((w, r));
        world.WindowReleased += w => released.Add(w);
        world.BuildForCapture();
        var window = built.Select(x => x.window).FirstOrDefault(w => w.pane != null && w.stage == 0 && world.IsBuilt(w.a) && world.IsBuilt(w.b));
        if (window == null) { Check(false, "glass: no intact window near the spawn"); return; }
        var root = window.root;
        var pane = window.pane.GetComponent<Collider>();
        var intoB = world.CellCenter(window.b) - world.CellCenter(window.a);
        intoB.y = 0f;
        Check(root.name == "Window " + window.a + "-" + window.b && (root.lossyScale - Vector3.one).magnitude < 1e-4f && window.pane.transform.parent == root
              && Vector3.Dot(root.forward, intoB.normalized) > .999f && (root.position - window.position).magnitude < 1e-4f
              && Mathf.Abs(window.pane.transform.position.y - root.position.y - 1.175f) < 1e-3f,
            "glass: an unscaled `Window {a}-{b}` root at the opening, +Z into b, the pane under it 1.175 m up");

        int started = 0, startedSeed = 0, shattered = 0, broken = 0;
        var cracks = new List<int>();
        var impulse = Vector3.zero;
        world.GlassHoldStarted += (w, at, seed) => { started++; startedSeed = seed; };
        world.GlassCracked += (w, stage) => cracks.Add(stage);
        world.WindowShattered += (w, at, push) => { shattered++; impulse = push; };
        world.GlassBroken += _ => broken++;
        var player = world.Player;
        player.position = world.CellCenter(window.a);

        // The box keeps its old world transform under the rotated root, for both wall directions.
        Physics.SyncTransforms();
        foreach (var vertical in new[] { false, true })
        {
            var w = built.Select(x => x.window).FirstOrDefault(x => x.pane != null && (x.a.x != x.b.x) == vertical);
            if (w == null) { Check(false, "glass: no " + (vertical ? "east" : "north") + " window built"); continue; }
            var size = w.pane.GetComponent<Collider>().bounds.size;
            var expected = vertical ? new Vector3(ModuleUnits.GlassThickness, 1.65f, 1.4f) : new Vector3(1.4f, 1.65f, ModuleUnits.GlassThickness);
            Check((size - expected).magnitude < 1e-3f && Mathf.Abs(w.pane.GetComponent<Collider>().bounds.center.y - 1.175f - w.root.position.y) < 1e-3f,
                "glass: " + (vertical ? "an east" : "a north") + " edge's box is " + size.ToString("F3") + " (expected " + expected.ToString("F3") + "), centred 1.175 m up");
        }

        // E-down near a corner: the impact is kept 0.2 m inside the exposed glass; struck from a.
        world.BeginGlassHold(pane, root.TransformPoint(new Vector3(.68f, .4f, 0f)), root.forward);
        world.TryGetGlassBreak(window.edge, out var record);
        Check(started == 1 && startedSeed == record.seed && Mathf.Abs(record.impact.x - .4835f) < 1e-3f && Mathf.Abs(record.impact.y - .5665f) < 1e-3f
              && record.side == 1 && record.impactUV.x > .8f && record.impactUV.y < .2f,
            "glass: E-down raises GlassHoldStarted with the seed; a corner hit is kept 0.2 m inside (" + record.impact.ToString("F3") + "), struck from a (side " + record.side + ")");
        var p = 0f;
        while (p < .5f) world.Hold(pane, Dt, out p);
        world.ReleaseHold(pane);
        var floor = window.hold;
        // A later strike elsewhere: the cracks' centre stays.
        world.BeginGlassHold(pane, root.TransformPoint(new Vector3(-.3f, 1.5f, 0f)), root.forward);
        world.TryGetGlassBreak(window.edge, out var resumed);
        while (p < .75f) world.Hold(pane, Dt, out p);
        Check(cracks.Count == 2 && cracks[0] == 1 && cracks[1] == 2 && Mathf.Abs(floor - FrontRoomsShotTimings.GlassBreak.Crack1) < 1e-4f && resumed.impact == record.impact,
            "glass: cracks 1 then 2, once each (" + string.Join(",", cracks) + "); a release drops to 0.35 (" + floor.ToString("0.00") + "); a resumed strike keeps the impact");

        // A rebuild: the window goes and comes back cracked, from 0.70.
        var chunk = MapGrid.ChunkOf(window.a);
        world.RebuildChunk(chunk);
        if (!released.Contains(window)) { chunk = MapGrid.ChunkOf(window.b); world.RebuildChunk(chunk); }
        var again = built.LastOrDefault(x => x.window.edge == window.edge);
        Check(released.Contains(window) && again.window != null && again.window != window && again.record.stage == 2 && again.window.stage == 2
              && Mathf.Abs(again.window.hold - FrontRoomsShotTimings.GlassBreak.Crack2) < 1e-4f && again.record.impact == record.impact,
            "glass: a rebuild raises WindowReleased, then WindowBuilt with the record (stage " + again.record.stage + "), resuming from 0.70");

        // A tap's float sum a rounding step short of a beat still cracks (the stage tolerance); a strike from b says side −1.
        var other = built.Select(x => x.window).FirstOrDefault(x => x.pane != null && x.stage == 0 && x != window && x.edge != window.edge && world.IsBuilt(x.a) && world.IsBuilt(x.b));
        if (other != null)
        {
            var otherPane = other.pane.GetComponent<Collider>();
            player.position = world.CellCenter(other.b);
            world.BeginGlassHold(otherPane, other.root.position + Vector3.up * 1.2f, -other.root.forward);
            var crackedBefore = cracks.Count;
            world.Hold(otherPane, FrontRoomsShotTimings.GlassBreak.Crack1 - 1e-6f, out _);
            world.TryGetGlassBreak(other.edge, out var otherRecord);
            Check(cracks.Count == crackedBefore + 1 && other.stage == 1 && otherRecord.side == -1,
                "glass: a hold one rounding step short of 0.35 still cracks once; struck from b, side −1 (" + otherRecord.side + ")");
            world.ReleaseHold(otherPane);
            cracks.RemoveRange(crackedBefore, cracks.Count - crackedBefore);
            player.position = world.CellCenter(window.a);
        }
        else Check(false, "glass: no second intact window for the tolerance and side checks");

        // A glass-kit handler that throws is logged and never breaks a build.
        Action<FrontRoomsMapWorld.Window, FrontRoomsMapWorld.GlassBreakRecord> throwing = (w, r) => throw new InvalidOperationException("test: a broken glass kit");
        world.WindowBuilt += throwing;
        var builtBefore = built.Count;
        world.RebuildChunk(chunk);
        world.WindowBuilt -= throwing;
        Check(world.IsBuilt(window.a) && world.IsBuilt(window.b) && built.Count > builtBefore,
            "glass: a WindowBuilt handler that throws does not stop the chunk building (" + (built.Count - builtBefore) + " windows announced)");
        // That rebuild replaced the window again: carry on with the newest one.
        again = built.LastOrDefault(x => x.window.edge == window.edge);

        // The shatter: only the pane goes; the root stays.
        var w2 = again.window;
        var pane2 = w2.pane.GetComponent<Collider>();
        var gave = false;
        for (var k = 0; k < 120 && !gave; k++) gave = world.Hold(pane2, Dt, out p);
        Check(gave && shattered == 1 && broken == 1 && Mathf.Abs(impulse.magnitude - FrontRoomsShotTimings.GlassBreak.ShatterImpulse) < 1e-3f && w2.pane == null && w2.root != null
              && w2.stage == 3 && world.PassageBetween(w2.a, w2.b) == FrontRoomsMapWorld.Passage.Open && cracks.Count == 2,
            "glass: the shatter raises WindowShattered (impulse " + impulse.magnitude.ToString("0.0") + " m/s) and GlassBroken once, removes only the pane, the way opens; no crack replayed");
        world.RebuildChunk(chunk);
        var after = built.LastOrDefault(x => x.window.edge == window.edge);
        Check(after.window != null && after.window != w2 && after.record.stage == 3 && after.window.pane == null && after.window.root != null,
            "glass: rebuilt broken, the window keeps its root and record (stage 3) with no pane");
    }

    // ---------- P4: tiers ----------

    static void Tiers(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles)
    {
        var rules = new FrontRoomsTierRules();
        var baseTuning = new FrontRoomsHunterTuning();
        var counts = new List<int>();
        for (var t = 1; t <= 5; t++)
        {
            var e = new FrontRoomsHunterTuning();
            e.CopyFrom(baseTuning);
            rules.At(t).ApplyTo(e);
            counts.Add(Mathf.Max(1, Mathf.RoundToInt(e.breakDoorSeconds / .5f)));
            if (t == 5)
                Check(Mathf.Abs(e.chaseSpeed - 4.2f * 1.29f) < .01f && Mathf.Abs(e.hearing - 1.4f * 1.6f) < .01f && Mathf.Abs(e.breakDoorSeconds - 1.3f) < .01f && Mathf.Abs(e.searchMaxSeconds - 24f) < .01f,
                    "tiers: tier 5 is chase " + e.chaseSpeed.ToString("0.00") + " m/s, hearing " + e.hearing.ToString("0.00") + ", break " + e.breakDoorSeconds.ToString("0.00") + " s, search " + e.searchMaxSeconds.ToString("0.0") + " s");
        }
        Check(string.Join(",", counts) == "5,4,4,3,3", "tiers: blows per door by tier " + string.Join(",", counts) + " (expected 5,4,4,3,3)");
        Check(baseTuning.chaseSpeed == 4.2f && baseTuning.breakDoorSeconds == 2.5f, "tiers: the base tuning is never changed");
        Check(rules.TierForZones(1) == 1 && rules.TierForZones(4) == 1 && rules.TierForZones(5) == 2 && rules.TierForZones(16) == 4 && rules.TierForZones(40) == 5,
            "tiers: zones 1-4 tier 1, 5 tier 2, 16 tier 4, 40 capped at 5");
        var t1 = rules.At(1);
        Check(t1.LampMode(.61f) == 0 && t1.LampMode(.63f) == 1 && t1.LampMode(.83f) == 2 && t1.LampMode(.93f) == 3 && t1.LampMode(.98f) == 4,
            "tiers: tier 1 lamp odds are the old 62 / 20 / 10 / 5 / 3");

        // A break keeps the length it started with when the tuning changes mid-break.
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), -34f, "MAP INTERACTION TEST / tier break");
        world.BuildForCapture();
        if (FindDoor(world, out var a, out var b, out var c))
        {
            var player = new GameObject("test player").transform;
            player.SetParent(world.transform, false);
            var body = player.gameObject.AddComponent<CapsuleCollider>();
            body.height = ModuleUnits.PlayerHeight;
            body.radius = ModuleUnits.PlayerRadius;
            body.center = Vector3.up * (ModuleUnits.PlayerHeight * .5f);
            var tuning = new FrontRoomsHunterTuning { sightRange = 0f };
            var hunter = new FrontRoomsMapHunter(world, tuning, body, null, 5);
            var target = world.CellCenter(b);
            player.position = target;
            Physics.SyncTransforms();
            hunter.DebugPlace(world.CellCenter(c));
            hunter.Noise(target, 1000f);
            var blows = new List<(int index, int count)>();
            hunter.DoorBlow += _ => blows.Add((hunter.BlowIndex, hunter.BlowCount));
            var door = world.DoorBetween(a, b);
            var changed = false;
            for (var t = 0f; t < 30f && !door.broken; t += Dt)
            {
                hunter.Tick(Dt, target, target + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
                world.TickDoorsForTools(Dt);
                Physics.SyncTransforms();
                // At 1.2 s into the break, the tier drops the break time to 1.3 s.
                if (!changed && hunter.State == HunterState.BreakDoor && hunter.StateTime >= 1.2f) { tuning.breakDoorSeconds = 1.3f; changed = true; }
            }
            Check(changed && door.broken && blows.Count == 5 && blows.All(x => x.count == 5) && blows[blows.Count - 1].index == 4,
                "tiers: a break that started at 2.5 s keeps 5 blows when the break time drops mid-break (" + string.Join(" ", blows.Select(x => x.index + "/" + x.count)) + ")");
            // Once the leaf has fallen and it walks on, the next break takes the new time.
            for (var t = 0f; t < 2f && hunter.State == HunterState.BreakDoor; t += Dt)
            {
                hunter.Tick(Dt, target, target + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
                world.TickDoorsForTools(Dt);
            }
            Check(hunter.State != HunterState.BreakDoor && hunter.BlowCount == 3, "tiers: after the break, BlowCount follows the new time (" + hunter.BlowCount + ")");
            // A real second break, on the same hunter behind another closed door, takes the new 1.3 s.
            if (FindDoor(world, out var a2, out var b2, out var c2))
            {
                var target2 = world.CellCenter(b2);
                var door2 = world.DoorBetween(a2, b2);
                player.position = target2;
                Physics.SyncTransforms();
                hunter.DebugPlace(world.CellCenter(c2));
                hunter.Noise(target2, 1000f);
                blows.Clear();
                float t2 = 0f, start2 = -1f, gave2 = -1f;
                world.DoorBroken += _ => { if (start2 >= 0f && gave2 < 0f) gave2 = t2 - start2; };
                for (; t2 < 30f && !door2.broken; t2 += Dt)
                {
                    hunter.Tick(Dt, target2, target2 + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
                    world.TickDoorsForTools(Dt);
                    Physics.SyncTransforms();
                    if (hunter.State == HunterState.BreakDoor && start2 < 0f) start2 = t2 - hunter.StateTime;
                }
                Check(door2.broken && blows.Count == 3 && blows.All(x => x.count == 3) && blows.Select(x => x.index).SequenceEqual(new[] { 0, 1, 2 }) && Mathf.Abs(gave2 - 1.3f) <= Dt * .6f,
                    "tiers: the next break takes the new time (" + string.Join(" ", blows.Select(x => x.index + "/" + x.count)) + ", gives at " + gave2.ToString("0.000") + " s; expected 0/3 1/3 2/3 at 1.3 s)");
            }
            else Check(false, "tiers: no second closed door near the spawn for the next break");
        }
        else Check(false, "tiers: no closed door near the spawn");

        // Generation: chunks keep the tier they were generated at; a shift takes the tier of its time.
        var cache = new FrontRoomsMapCache(FrontRoomsLevelProfiles.Resolve().Generation(4242));
        var first = cache.Get(new GridCoord(0, 0));
        cache.Tier = 3;
        var again = cache.Get(new GridCoord(0, 0));
        var fresh = cache.Get(new GridCoord(5, 0));
        cache.Shift(new GridCoord(0, 0));
        var shifted = cache.Get(new GridCoord(0, 0));
        Check(first.tier == 1 && ReferenceEquals(first, again) && fresh.tier == 3 && shifted.tier == 3,
            "tiers: a generated chunk keeps tier 1 after the run reaches tier 3; a new chunk and a shifted one take 3");
        // Module tier: a module for tiers 2+ never comes at tier 1, and does at tier 3.
        var m = Room(3, 3, ZoneTheme.Level0);
        m.minTier = 1; m.maxTier = 9; m.weight = 1f; m.allowRotate = true;
        var settings = FrontRoomsLevelProfiles.Resolve().Generation(777);
        settings.moduleChance = 1f;
        // The base tier is under test, not the profile's value.
        settings.moduleTier = 0;
        var lib = new List<RoomModuleData> { m };
        int Placed(int tier) => PlacedWith(lib, tier);
        int PlacedWith(List<RoomModuleData> library, int tier)
        {
            var cc = new FrontRoomsMapCache(settings, library) { Tier = tier };
            var n = 0;
            for (var y = -6; y < 6; y++)
            for (var x = -6; x < 6; x++)
            {
                var d = cc.Get(new GridCoord(x, y));
                for (var r = 0; r < d.rooms.Length; r++) if (d.ModuleOf(r) != null) n++;
            }
            return n;
        }
        int at1 = Placed(1), at3 = Placed(3);
        Check(at1 == 0 && at3 > 0, "tiers: a module with tiers 1-9 is placed " + at1 + " times at run tier 1 and " + at3 + " at tier 3 (module tier = base + tier - 1)");
        // The upper bound: a module for tiers 0-1 comes at run tiers 1 and 2, never at 3.
        var early = Room(3, 3, ZoneTheme.Level0);
        early.minTier = 0; early.maxTier = 1; early.weight = 1f; early.allowRotate = true;
        var libEarly = new List<RoomModuleData> { early };
        int e1 = PlacedWith(libEarly, 1), e2 = PlacedWith(libEarly, 2), e3 = PlacedWith(libEarly, 3);
        Check(e1 > 0 && e2 > 0 && e3 == 0, "tiers: a module with tiers 0-1 is placed " + e1 + "/" + e2 + "/" + e3 + " times at run tiers 1/2/3");
    }

    // ---------- P4: markers ----------

    static void KeySpot(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles)
    {
        // A 6 x 6 room over chunk (0, 0)'s cells 1..6 holds its zone's key cell (always local 1..6).
        var m = Room(6, 6, ZoneTheme.Level0);
        var probe = ModuleWorld(roots, profiles, m, 1, 1, -35f, "MAP INTERACTION TEST / key probe");
        var data = probe.Cache.Get(new GridCoord(0, 0));
        if (!data.hasKey) { Check(false, "key spot: chunk (0, 0) has no key in this profile (tall zone?)"); return; }
        var site = data.keySiteCell;
        var cs = MapGrid.CellSize;
        // The marker in the key's own cell, off its centre, on the floor, turned 30°.
        var mx = (site.x - 1 + .5f) * cs + .6f;
        var mz = (site.y - 1 + .5f) * cs - .4f;
        m.markers = new[] { new ModuleMarker { kind = ModuleMarkerKind.KeySpot, x = mx, z = mz, y = 0f, yaw = 30f } };
        var world = ModuleWorld(roots, profiles, m, 1, 1, -36f, "MAP INTERACTION TEST / key spot");
        var d2 = world.Cache.Get(new GridCoord(0, 0));
        Check(d2.keySpot && d2.keyCell == site && Mathf.Abs(d2.keyX - (cs + mx)) < .001f && Mathf.Abs(d2.keyZ - (cs + mz)) < .001f,
            "key spot: the module's key spot takes the zone key (cell " + d2.keyCell + ", at " + d2.keyX.ToString("0.00") + ", " + d2.keyZ.ToString("0.00") + ")");
        var key = world.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("Key · zone"));
        var expected = new Vector3(cs + mx, .06f, cs + mz);
        Check(key != null && (key.localPosition - expected).sqrMagnitude < 1e-4f && Mathf.Abs(Mathf.DeltaAngle(key.localEulerAngles.y, 30f)) < .5f,
            "key spot: the key lies at the marker, turned with it (" + (key != null ? key.localPosition.ToString("F2") : "none") + ")");
        // Without a key spot the key keeps its cell centre.
        var data0 = probe.Cache.Get(new GridCoord(0, 0));
        Check(!data0.keySpot && data0.keyCell == data0.keySiteCell, "key spot: without one the key stays at its site cell");
    }

    /// <summary>Walking distance in cells (open edges and doors, not glass) from a cell, up to maxDepth.</summary>
    static Dictionary<GridCoord, int> Walk(FrontRoomsMapWorld world, GridCoord start, int maxDepth)
    {
        var depth = new Dictionary<GridCoord, int> { [start] = 0 };
        var queue = new Queue<GridCoord>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            if (depth[cell] >= maxDepth) continue;
            foreach (var s in Steps)
            {
                var n = cell + s;
                if (depth.ContainsKey(n) || !world.IsBuilt(n)) continue;
                var p = world.PassageBetween(cell, n);
                if (p == FrontRoomsMapWorld.Passage.Wall || p == FrontRoomsMapWorld.Passage.Glass) continue;
                depth[n] = depth[cell] + 1;
                queue.Enqueue(n);
            }
        }
        return depth;
    }

    static void RelayEntry(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles)
    {
        var m = Room(3, 3, ZoneTheme.Level0);
        m.markers = new[] { new ModuleMarker { kind = ModuleMarkerKind.RelayEntry, x = 4.5f, z = 4.5f, tag = "doorway" } };
        var world = ModuleWorld(roots, profiles, m, 3, 3, -37f, "MAP INTERACTION TEST / relay entry");
        var list = new List<(Vector3 pos, string tag)>();
        world.RelayEntries(list);
        Check(list.Count == 1 && list[0].tag == "doorway", "relay entry: the map lists the module's entry (" + list.Count + ")");
        if (list.Count != 1) return;
        var entry = list[0].pos;
        var entryCell = world.CellOf(entry);
        // A player 10-14 cells of walking away that cannot see the entry, facing away from it.
        var depth = Walk(world, entryCell, 14);
        var eye = Vector3.up * ModuleUnits.PlayerEye;
        GridCoord? spot = null;
        foreach (var pair in depth.OrderBy(x => x.Value))
        {
            if (pair.Value < 10) continue;
            var feet = world.CellCenter(pair.Key);
            if (!Physics.Linecast(feet + eye, entry + Vector3.up * 1.6f) || !Physics.Linecast(feet + eye, entry + Vector3.up * 1.95f)) continue;
            spot = pair.Key;
            break;
        }
        if (!spot.HasValue) { Check(false, "relay entry: no cell 10-14 cells from the entry that cannot see it"); return; }
        var player = new GameObject("test player").transform;
        player.SetParent(world.transform, false);
        var body = player.gameObject.AddComponent<CapsuleCollider>();
        body.height = ModuleUnits.PlayerHeight;
        body.radius = ModuleUnits.PlayerRadius;
        body.center = Vector3.up * (ModuleUnits.PlayerHeight * .5f);
        var p = world.CellCenter(spot.Value);
        player.position = p;
        Physics.SyncTransforms();
        var forward = (p - entry);
        forward.y = 0f;
        forward.Normalize();
        var tuning = new FrontRoomsHunterTuning { releaseDelaySeconds = 0f };
        var hunter = new FrontRoomsMapHunter(world, tuning, body, null, 9);
        var arrivals = new List<(Vector3 pos, string tag)>();
        hunter.Arrived += (pos, tag) => arrivals.Add((pos, tag));
        hunter.Tick(Dt, p, p + eye, forward);
        Check(hunter.Released && arrivals.Count == 1 && arrivals[0].tag == "doorway" && Flat(arrivals[0].pos - entry) < .01f && Flat(hunter.Position - entry) < .01f,
            "relay entry: released at the module's entry behind the player, Arrived with tag 'doorway' (" + (arrivals.Count > 0 ? arrivals[0].tag + " at " + arrivals[0].pos.ToString("F2") : "no arrival") + ")");
        // A relay to an unmarked cell raises Arrived with no tag: stand next to the entry so it is out of the band.
        var near = world.CellCenter(entryCell);
        player.position = near;
        Physics.SyncTransforms();
        var hunter2 = new FrontRoomsMapHunter(world, tuning, body, null, 9);
        var tags = new List<string>();
        hunter2.Arrived += (pos, tag) => tags.Add(tag);
        hunter2.Tick(Dt, near, near + eye, Vector3.forward);
        Check(hunter2.Released && tags.Count == 1 && tags[0] == null, "relay entry: an arrival in an unmarked cell has no tag");
    }

    static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;

    /// <summary>The Office fill keeps off a module's Relay entry (0.8 m) and its floor key (1 m): both are put where the kit stood without them.</summary>
    static void FillKeepsClear(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles)
    {
        var cs = MapGrid.CellSize;
        var m = Room(6, 6, ZoneTheme.Office);   // 6 x 6 at (1, 1) holds the key's site cell (local 1..6)
        m.fill = ModuleFill.Office;
        var probe = ModuleWorld(roots, profiles, m, 1, 1, -39f, "MAP INTERACTION TEST / fill clear probe");
        var data = probe.Cache.Get(new GridCoord(0, 0));
        var origin = probe.transform.TransformPoint(new Vector3(cs, 0f, cs));
        // Non-vacuous: put the entry where the kit put furniture when nothing was kept clear.
        var hit = probe.GetComponentsInChildren<Transform>(true).Where(t => t.name == "office dressing")
            .SelectMany(t => t.GetComponentsInChildren<Collider>(true)).Select(c => c.bounds)
            .Where(b => b.min.y <= ModuleUnits.RelayHeight)
            .Select(b => new Vector2(b.center.x - origin.x, b.center.z - origin.z))
            .FirstOrDefault(p => p.x > 1.5f && p.y > 1.5f && p.x < m.WidthMetres - 1.5f && p.y < m.DepthMetres - 1.5f);
        if (hit == default) { Check(false, "fill clear: the Office kit put nothing inside the probe room"); return; }
        var markers = new List<ModuleMarker> { new ModuleMarker { kind = ModuleMarkerKind.RelayEntry, x = hit.x, z = hit.y, tag = "vent", host = "" } };
        var site = data.keySiteCell;
        if (data.hasKey) markers.Add(new ModuleMarker { kind = ModuleMarkerKind.KeySpot, x = (site.x - 1 + .5f) * cs, z = (site.y - 1 + .5f) * cs, y = 0f, host = "" });
        m.markers = markers.ToArray();
        var world = ModuleWorld(roots, profiles, m, 1, 1, -40f, "MAP INTERACTION TEST / fill clear");
        var d = world.Cache.Get(new GridCoord(0, 0));
        var keep = new List<(Rect r, string what)> { (new Rect(cs + hit.x - .4f, cs + hit.y - .4f, .8f, .8f), "Relay entry") };
        if (d.hasKey && d.keySpot) keep.Add((new Rect(d.keyX - .5f, d.keyZ - .5f, 1f, 1f), "floor key"));
        var bad = new List<string>(); var n = 0;
        foreach (var root in world.GetComponentsInChildren<Transform>(true).Where(t => t.name == "office dressing"))
        foreach (var c in root.GetComponentsInChildren<Collider>(true))
        {
            var b = c.bounds; if (b.min.y > ModuleUnits.RelayHeight) continue;
            var lo = world.transform.InverseTransformPoint(b.min); var hi = world.transform.InverseTransformPoint(b.max);
            n++;
            foreach (var (r, what) in keep)
                if (lo.x < r.xMax - .02f && r.xMin + .02f < hi.x && lo.z < r.yMax - .02f && r.yMin + .02f < hi.z) bad.Add(what + ": " + c.name);
        }
        Check(n > 0 && bad.Count == 0, "fill clear: the Office fill keeps off the Relay entry (0.8 m) and the floor key (1 m) (" + string.Join(", ", bad.Take(6)) + ")");
        Check(!data.hasKey || (d.keySpot && keep.Count == 2), "fill clear: the key spot took the zone key");
        // Optional: reuse RelayEntry's release procedure (factored out as a helper) on `world` and assert Arrived with tag "vent" at the entry.
    }

    // ---------- P4: live tuning ----------

    static void Live(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles)
    {
        var m = Room(3, 3, ZoneTheme.Level0);
        var world = ModuleWorld(roots, profiles, m, 3, 3, -38f, "MAP INTERACTION TEST / live");
        var live = UnityEngine.Object.Instantiate(FrontRoomsLevelProfiles.Resolve());
        live.hideFlags = HideFlags.DontSave;
        profiles.Add(live);
        live.buildRadius = 3;
        var applied = 0;
        world.LiveApplied += () => applied++;
        var before = world.SightDistance;
        world.ApplyLive(live);
        Check(applied == 1 && world.BuildRadius == 3 && world.SightDistance > before, "live: ApplyLive takes the build radius (sight " + before + " → " + world.SightDistance + " m)");
        // ReplaceModule rebuilds chunk (0, 0) and leaves the others as they were.
        var others = world.GetComponentsInChildren<Transform>(true).Where(t => t.parent == world.transform && t.name.StartsWith("Chunk ") && !t.name.StartsWith("Chunk (0, 0)")).Select(t => t.gameObject).ToList();
        var oldChunk = world.GetComponentsInChildren<Transform>(true).First(t => t.parent == world.transform && t.name.StartsWith("Chunk (0, 0)")).gameObject;
        var m2 = Room(3, 3, ZoneTheme.Level0);
        m2.innerEast[0] = ModuleEdge.Wall;
        world.ReplaceModule(new GridCoord(0, 0), m2, 3, 3);
        var newChunk = world.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.parent == world.transform && t.name.StartsWith("Chunk (0, 0)") && t.gameObject.activeSelf);
        var data = world.Cache.Get(new GridCoord(0, 0));
        Check(newChunk != null && newChunk.gameObject != oldChunk && others.All(o => o != null) && data.east[MapGrid.LocalIndex(3, 3)] == EdgeKind.Wall,
            "live: ReplaceModule rebuilt chunk (0, 0) with the new module (inner wall in) and kept the " + others.Count + " other chunks");
        // The rebuilt chunk's Relay entries are registered again, and a removed one is gone.
        var m3 = Room(3, 3, ZoneTheme.Level0);
        m3.markers = new[] { new ModuleMarker { kind = ModuleMarkerKind.RelayEntry, x = 4.5f, z = 4.5f, tag = "t" } };
        world.ReplaceModule(new GridCoord(0, 0), m3, 3, 3);
        var list = new List<(Vector3 pos, string tag)>();
        world.RelayEntries(list);
        var withEntry = list.Count == 1 && list[0].tag == "t";
        world.ReplaceModule(new GridCoord(0, 0), Room(3, 3, ZoneTheme.Level0), 3, 3);
        world.RelayEntries(list);
        Check(withEntry && list.Count == 0, "live: a rebuilt chunk registers its module's Relay entry again, and drops it when the module no longer has one");
    }

    // ---------- The guard round Build ----------

    static void BuildGuard(List<GameObject> roots)
    {
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), -33f, "MAP INTERACTION TEST / build guard");
        // The chunk east of the spawn chunk throws.
        var spawnChunk = MapGrid.ChunkOf(new GridCoord(0, 0));
        var failing = default(GridCoord);
        var calls = 0;
        world.FailBuildForTools = c =>
        {
            if (c != failing) return false;
            calls++;
            return true;
        };
        // The spawn is in the middle of chunk (0, 0) unless overridden.
        failing = new GridCoord(spawnChunk.x + 1, spawnChunk.y);
        world.BuildForCapture();
        var origin = MapGrid.ChunkOrigin(failing);
        var cell = new GridCoord(origin.x + 3, origin.y + 3);
        var leftovers = world.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("Chunk " + failing + " "));
        Check(calls == 1 && world.FailedChunkCount == 1, "build guard: the failing chunk " + failing + " was tried once and marked failed (calls " + calls + ", failed " + world.FailedChunkCount + ")");
        Check(leftovers == 0 && !world.IsBuilt(cell), "build guard: nothing of it is left in the scene or registered (leftovers " + leftovers + ")");
        Check(world.BuiltChunkCount > 0 && world.IsBuilt(world.CellOf(world.SpawnWorldPosition)), "build guard: the rest of the map is built (" + world.BuiltChunkCount + " chunks)");
        var stream = typeof(FrontRoomsMapWorld).GetMethod("Stream", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var center = MapGrid.ChunkOf(world.CellOf(world.SpawnWorldPosition));
        stream.Invoke(world, new object[] { center, int.MaxValue });
        stream.Invoke(world, new object[] { center, int.MaxValue });
        Check(calls == 1, "build guard: streaming again does not retry it while it is in range (calls " + calls + ")");
        Check(world.Settled && world.ReadyAround(1), "build guard: Settled and ReadyAround count the failed chunk as done");
        // Out of range and back: one more try.
        var far = new GridCoord(center.x + 10, center.y);
        stream.Invoke(world, new object[] { far, int.MaxValue });
        stream.Invoke(world, new object[] { center, int.MaxValue });
        Check(calls == 2 && world.FailedChunkCount == 1, "build guard: after leaving range it is tried once more (calls " + calls + ")");
    }

    // ---------- Module builds ----------

    /// <summary>A floor kit at least 1.2 m long (a desk) and a small kit to stand on it.</summary>
    static bool DeskKits(out string desk, out string item)
    {
        desk = item = null;
        foreach (var name in FrontRoomsKitLibrary.AllNames())
        {
            var info = FrontRoomsKitLibrary.GetInfo(name);
            var f = FrontRoomsMapWorld.KitFootprint(name);
            if (info == null || f == null) continue;
            float w = f[2] - f[0], d = f[3] - f[1];
            if (desk == null && info.placement == "Floor" && w >= 1.2f && w <= 2f && d <= 1f && f[4] < 1.2f) desk = name;
            if (item == null && info.placement == "DeskTop" && w <= .5f && d <= .5f) item = name;
        }
        return desk != null && item != null;
    }

    static string FloorKit()
    {
        foreach (var name in FrontRoomsKitLibrary.AllNames())
        {
            var info = FrontRoomsKitLibrary.GetInfo(name);
            var f = FrontRoomsMapWorld.KitFootprint(name);
            if (info == null || f == null || info.placement != "Floor") continue;
            if (f[2] - f[0] > .3f && f[2] - f[0] < 1.4f && f[3] - f[1] > .3f && f[3] - f[1] < 1.4f && f[4] < 1.6f) return name;
        }
        return null;
    }

    /// <summary>A world whose zones all take the module's height and theme, with the module in chunk (0, 0) at (x0, y0), as the Level Designer preview builds it.</summary>
    static FrontRoomsMapWorld ModuleWorld(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles, RoomModuleData m, int x0, int y0, float period, string name)
    {
        var profile = UnityEngine.Object.Instantiate(FrontRoomsLevelProfiles.Resolve());
        profile.hideFlags = HideFlags.DontSave;
        profiles.Add(profile);
        var g = profile.Generation(3);
        g.lowShare = m.height == ZoneHeight.Low ? 1f : 0f;
        g.standardShare = m.height == ZoneHeight.Standard ? 1f : 0f;
        g.tallShare = m.height == ZoneHeight.Tall ? 1f : 0f;
        g.officeShare = m.theme == ZoneTheme.Office ? 1f : 0f;
        g.moduleChance = 0f;
        profile.generation = g;
        profile.buildRadius = 1;
        var world = World(roots, profile, period, name);
        world.TagModuleProps = true;
        world.PlaceModule(m, new GridCoord(0, 0), x0, y0);
        world.BuildForCapture();
        return world;
    }

    static RoomModuleData Room(int w, int d, ZoneTheme theme)
    {
        var m = new RoomModuleData { width = w, depth = d, height = ZoneHeight.Standard, theme = theme, columns = ModuleColumns.None, fill = ModuleFill.None };
        m.Normalize();
        for (var i = 0; i < m.south.Length; i++) m.south[i] = ModuleEdge.Arch;
        for (var i = 0; i < m.north.Length; i++) m.north[i] = ModuleEdge.Arch;
        return m;
    }

    static void Columns(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles)
    {
        var kit = FloorKit();
        if (kit == null) { Check(false, "columns: no floor kit with a known footprint in the kit library"); return; }
        var m = Room(4, 4, ZoneTheme.Level0);
        m.columns = ModuleColumns.Custom;
        m.customColumns = new[] { new ModuleColumn { x = 2, y = 2, large = true } };
        // Prop 0 on the column (corner (2, 2) = 6 m, 6 m); prop 1 well clear of it and the strips.
        var props = new List<ModuleProp> { new ModuleProp { kit = kit, x = 6f, z = 6f }, new ModuleProp { kit = kit, x = 3f, z = 6f } };
        // A desk reaching into the column (2), an item on that desk clear of the column (3), and an item on the kept prop (4).
        var desks = DeskKits(out var desk, out var item);
        if (desks)
        {
            var df = FrontRoomsMapWorld.KitFootprint(desk);
            var half = (df[2] - df[0]) * .5f;
            props.Add(new ModuleProp { kit = desk, x = 6f + half - .1f, z = 6f });
            props.Add(new ModuleProp { kit = item, x = 6f + 2f * half - .35f, z = 6f, y = .74f, noCollider = true });
            props.Add(new ModuleProp { kit = item, x = 3f, z = 6f, y = .74f, noCollider = true });
        }
        m.props = props.ToArray();
        var errors = new List<string>();
        var warnings = new List<string>();
        m.Validate(errors, warnings, FrontRoomsMapWorld.KitFootprint);
        Check(warnings.Any(w => w.StartsWith("Prop 1") && w.Contains("a column stands")) && !warnings.Any(w => w.StartsWith("Prop 2") && w.Contains("column")),
            "columns: Validate warns about the prop on the column only (" + string.Join(" | ", warnings) + ")");
        var world = ModuleWorld(roots, profiles, m, 2, 2, -31f, "MAP INTERACTION TEST / columns");
        var built = world.GetComponentsInChildren<FrontRoomsModulePropTag>(true).Select(x => x.index).OrderBy(x => x).ToArray();
        Check(built.Contains(1) && !built.Contains(0), "columns: the prop on the column is left out of the build, the other stands (built " + string.Join(", ", built) + ", kit " + kit + ")");
        if (desks)
            Check(!built.Contains(2) && !built.Contains(3) && built.Contains(4),
                "columns: the desk reaching into the column goes, and the item on it with it; the item on the kept prop stays (built " + string.Join(", ", built) + ", " + desk + " / " + item + ")");
        else Check(false, "columns: no desk and desk-top kit pair in the kit library");
    }

    static void InnerWalls(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles)
    {
        // A 6 x 5 Office room with an inner wall line at x = 3 cells: wall on rows 0-1 and 3-4, an arch on row 2.
        var m = Room(6, 5, ZoneTheme.Office);
        m.fill = ModuleFill.Office;
        for (var j = 0; j < m.depth; j++) m.innerEast[2 + j * (m.width - 1)] = j == 2 ? ModuleEdge.Arch : ModuleEdge.Wall;
        var errors = new List<string>();
        var warnings = new List<string>();
        m.Validate(errors, warnings, FrontRoomsMapWorld.KitFootprint);
        Check(warnings.Any(w => w.Contains("furnishes the room as one open space")), "inner walls: Validate warns about Office fill with inner walls");
        var world = ModuleWorld(roots, profiles, m, 1, 1, -32f, "MAP INTERACTION TEST / inner walls");
        var dressing = world.GetComponentsInChildren<Transform>(true).Where(x => x.name == "office dressing").ToArray();
        if (dressing.Length == 0) { Check(false, "inner walls: the Office kit did not dress the module (is FrontRoomsOfficeKit in the project?)"); return; }
        // The room's inner strips in world space.
        var cs = MapGrid.CellSize;
        var origin = world.transform.TransformPoint(new Vector3(1 * cs, 0f, 1 * cs));
        var strips = m.InnerStrips();
        var inWall = new List<string>();
        var inDoorway = new List<string>();
        var colliders = 0;
        foreach (var root in dressing)
        foreach (var c in root.GetComponentsInChildren<Collider>(true))
        {
            // Only the dressing in this module's room.
            var b = c.bounds;
            var lx0 = b.min.x - origin.x; var lz0 = b.min.z - origin.z; var lx1 = b.max.x - origin.x; var lz1 = b.max.z - origin.z;
            if (lx1 < 0f || lz1 < 0f || lx0 > m.WidthMetres || lz0 > m.DepthMetres) continue;
            colliders++;
            if (b.min.y > ModuleUnits.RelayHeight) continue;
            foreach (var s in strips)
            {
                // A 2 cm tolerance for touching faces.
                if (!(lx0 < s[2] - .02f && s[0] + .02f < lx1 && lz0 < s[3] - .02f && s[1] + .02f < lz1)) continue;
                (s[4] > 0f ? inWall : inDoorway).Add(c.name + " < " + c.transform.parent?.name);
            }
        }
        Check(colliders > 0, "inner walls: the Office kit placed " + colliders + " colliders in the module");
        Check(inWall.Count == 0, "inner walls: nothing stands in an inner wall (" + string.Join(", ", inWall.Take(6)) + ")");
        Check(inDoorway.Count == 0, "inner walls: the inner doorway's clear floor stays clear (" + string.Join(", ", inDoorway.Take(6)) + ")");
    }
}
