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
/// - keys (doors need keys; door_shots_plan.md §5.3): LockedByRule is the
///   level's rule for every door; a locked door's Use answers Rattled, raises
///   DoorLocked and DoorRattled once each (the lock point on the trier's face)
///   and rattles (two leaf jolts toward its swing side, the hinge still); a
///   try within Rattle.Duration does nothing, a later one rattles again; with
///   the key and a key-turn owner Use answers KeyTurn and holds the door (no
///   prompt, Use and TryOpenDoor leave it, no DoorUnlocked); CommitUnlock
///   raises DoorUnlocked once with the zone and the keyhole, and the leaf
///   shifts and settles; ReleaseKeyTurn opens it to its swing side, with a
///   drawn latch at once and with no DoorHandleTurned; later opens are plain,
///   and from the far side it stays unlocked; with no owner the key unlocks
///   and opens at once; CancelKeyTurn leaves a door locked; TryOpenDoor with
///   the key unlocks at once; DoorOpenBegan carries the source; DoorsPaused
///   freezes a swing; with keys off DoorUnlocked is never raised;
/// - broken doors (RELAY §10, Red 2026-10-14) and the crosshair kinds (H1):
///   Kind agrees with Describe on every door (Locked without the key, at the
///   zone the player stands in; LockedHaveKey with it; Shut once open, Open
///   once unlocked and shut) and on a pane (Glass, or GlassTap); E on a broken
///   door answers Broken with DoorBrokenTried, nothing within
///   BrokenDoor.Duration, raises no door event (no Relay noise) and never moves
///   it; it has kind Broken and no prompt, is an Open passage at 0 m, lets a
///   keyless player through a locked door, and stays broken through a rebuild
///   and a revisit shift;
/// - double-acting doors (A12, Red 2026-10-22): doors opened from alternating
///   sides each rest beyond their opener (both sides occur) and are rebuilt
///   open on that side after a rebuild and a revisit shift; a push from either
///   face waits for the lever (Open.SwingStart, no pull beat, a second E
///   ignored), eases out to 95° with the 2° overshoot and rests at
///   Open.Duration on the far face, never touching the opener; PassageBetween
///   is ClosedDoor below 70° and Open from there; a shut from the far side
///   lands in the frame, latched, with no DoorPulled; from the side the leaf
///   last went to, even inside its old sweep or beside the hinge, it turns
///   over and opens away (no DoorPulled); a shut from the leaf's side inside
///   its sweep raises DoorPulled with a clear spot (out of the sweep, latch
///   side, in the keep-clear strip and the cell) and the game's step gets the
///   player clear while it shuts; E on a leaf resting on its shutter turns it
///   back open, away from them; E from behind a leaf mid-shut pushes it on
///   through the frame to the far side without latching (rebuilt on that
///   side); a leaf never ends inside a body standing in its way (the
///   player's or the Relay's): it stops on it and goes on once it is clear;
/// - the Relay breaks a door whichever side its leaf last went to: it
///   shoulders it in front of the opening, faces the door, IsBeingBroken hides
///   the prompt and blocks E, each blow jolts the leaf child, DoorBrokenFrom
///   reports the side away from the leaf, and the leaf is thrown past its rest
///   into the far cell and rests at 80°, 3° crooked; the hinge never turns
///   more than 40° in a frame, even at 4 fps; a leaf held partly open on the
///   Relay's side (resting on a body that stays put) keeps its side: the Relay
///   breaks it from beside the latch, out of the leaf's sweep for every break
///   frame, DoorBrokenFrom reports the leaf's (swing) side, and the leaf is
///   thrown toward the Relay and rests at 80° in its cell (the rip);
/// - a break the Relay leaves: a door opened under it (A12: its leaf swings
///   toward the Relay and may rest on it) is not broken, the mark clears, it
///   steps clear of the sweep and walks through; placed away mid-break, the
///   mark clears;
/// - a wander or a search never breaks a door: one the player shuts across
///   the leg stays shut and unbroken (no DoorBlow), and the Relay never turns
///   to Hunt or BreakDoor; with a way round it walks round to the leg's end,
///   with none the leg is over at the door;
/// - losing sight, the Relay follows the player through a door only when it
///   saw them go through it in its last second of sight; one they went
///   through unseen it never learns of, and one it saw over a second before
///   it lost them it lets go: either way it goes where it last saw them;
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
///   keeps its progress (GD3c, in the break record too); the break record and
///   the kept progress survive a rebuild; the
///   shatter removes only the pane, with WindowShattered and GlassBroken.
/// - W1, the warning's lamp bursts (RELAY §9.2, the C10 bridge onto v1): stage 0
///   never dips a lamp; stage 1's first burst comes 0.3-1.0 s in, 2-3 dips 0.55 s
///   apart with every live lamp within 80 ms, to 0.55-0.70 of its level, gaps of
///   at least 3 s, at most 2 starts a second per lamp; entering stage 2 bursts at
///   once (2 dips); a chase starts no burst; back at stage 0 every dip is gone and
///   the held lamps have their own modes; never a lamp within 2 cells of the
///   Relay; Reduce flashing: one sag per lamp. M2 (RELAY 9.5): from stage 1
///   until stage 0 the room's Dead lamps strike no blink while one in another
///   room does; the hold moves with the player and Reset lets it go.
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
            DoubleActing(roots);
            RelayBreaks(roots);
            RelayBreakAborts(roots);
            RelayKeepsShutDoors(roots);
            RelayLosesTrack(roots);
            Keys(roots, profiles);
            BrokenDoors(roots, profiles);
            Columns(roots, profiles);
            InnerWalls(roots, profiles);
            FillKeepsClear(roots, profiles);
            StartKeepClear(roots, profiles);
            BuildGuard(roots);
            Tiers(roots, profiles);
            KeySpot(roots, profiles);
            RelayEntry(roots, profiles);
            Live(roots, profiles);
            Lamps(roots);
            Glass(roots);
            WarnStages(roots);
            WarnLamps(roots);
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

    // ---------- Keys: the rattle, the key turn's hold, the commit and the release ----------

    static void Keys(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles)
    {
        const string label = "keys: ";
        var profile = UnityEngine.Object.Instantiate(FrontRoomsLevelProfiles.Resolve());
        profile.hideFlags = HideFlags.DontSave;
        profile.doorsNeedKeys = true;
        profiles.Add(profile);
        var world = World(roots, profile, -28f, "MAP INTERACTION TEST / keys");
        world.BuildForCapture();
        var all = Doors(world).Values.Select(d => world.DoorBetween(d.a, d.b)).Where(d => d != null).ToList();
        Check(all.Count > 0 && all.All(world.LockedByRule), label + "LockedByRule is the level's rule (doors need keys) for all " + all.Count + " doors");
        var used = new HashSet<long>();
        if (!FindDoor(world, out var a, out var b, out _)) { Check(false, label + "no closed door near the spawn"); return; }
        var door = world.DoorBetween(a, b);
        used.Add(door.edge);
        // Work from the stop side (a push), so the release's drawn latch opens at once.
        if (FrontRoomsMapWorld.OnSwingSide(door, world.CellCenter(a))) (a, b) = (b, a);
        var player = world.Player;
        player.position = world.CellCenter(a) + Vector3.up * .05f;

        var unlocked = new List<(FrontRoomsMapWorld.Door door, GridCoord zone, Vector3 at)>();
        var rattled = new List<(FrontRoomsMapWorld.Door door, Vector3 at)>();
        var began = new List<(FrontRoomsMapWorld.Door door, FrontRoomsMapWorld.DoorOpenSource source, bool latchDrawn)>();
        int locked = 0, moved = 0, handles = 0;
        var lockedFrame = -1;
        var frame = 0;
        world.DoorUnlocked += (d, z, p) => unlocked.Add((d, z, p));
        world.DoorLocked += _ => { locked++; lockedFrame = frame; };
        world.DoorRattled += (d, p) => { rattled.Add((d, p)); if (lockedFrame != frame) lockedFrame = -2; };
        world.DoorMoved += _ => moved++;
        world.DoorHandleTurned += (d, p) => handles++;
        world.DoorOpenBegan += (d, src, latch) => began.Add((d, src, latch));
        void Tick(float seconds) { for (var s = 0f; s < seconds - 1e-4f; s += Dt) { frame++; world.TickDoorsForTools(Dt); } }

        // 1. Locked: Rattled, DoorLocked then DoorRattled in the same frame, the lock point on the trier's face.
        var result = world.Use(door.leaf);
        Check(result == FrontRoomsMapWorld.UseResult.Rattled && locked == 1 && rattled.Count == 1 && rattled[0].door == door && lockedFrame == frame && !door.open && unlocked.Count == 0,
            label + "without the key Use answers Rattled (" + result + "), the door stays shut, DoorLocked and DoorRattled once each in the same frame (locked " + locked + ", rattled " + rattled.Count + ")");
        if (rattled.Count > 0)
        {
            var rotation = door.hinge.parent.rotation * door.closed;
            var offset = rattled[0].at - door.position;
            var across = rotation * Vector3.right;
            var face = Vector3.Dot(offset, across);
            Check(Mathf.Abs(offset.y - ModuleUnits.DoorHandleHeight) < .01f
                && Mathf.Abs(Vector3.Dot(offset, rotation * Vector3.forward) - (ModuleUnits.DoorWidth * .5f - ModuleUnits.DoorHandleInset)) < .01f
                && Mathf.Sign(face) == Mathf.Sign(Vector3.Dot(player.position - door.position, across))
                && Mathf.Abs(Mathf.Abs(face) - (ModuleUnits.DoorLeafThickness * .5f + ModuleUnits.DoorHandleProud)) < .01f,
                label + "DoorRattled's lock point " + offset.ToString("F3") + " from the opening: 1.0 m up, 0.08 m in from the latch jamb, on the trier's face");
        }
        // The rattle: two jolts of the leaf child toward the swing side (the stops hold the other way), at Rattle.Jolt1 and Jolt2; the hinge never moves.
        var home = door.leaf.transform.localPosition;
        var towardSwing = Vector3.right * -door.swing;
        var joltStarts = new List<float>();
        var maxShift = 0f;
        var hingeStill = true;
        var wasJolting = false;
        for (var k = 1; k <= 30; k++)
        {
            frame++;
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
        // The debounce: a try 0.2 s after a rattle does nothing; one just past Rattle.Duration rattles again.
        // The jolt loop above ran 0.5 s, which is Rattle.Duration itself: step past it first, so this try rattles by margin, not by rounding.
        Tick(.05f);
        var opening = world.Use(door.leaf);
        var lockedBefore = locked;
        var rattledBefore = rattled.Count;
        Tick(.2f);
        var early = world.Use(door.leaf);
        var earlyQuiet = locked == lockedBefore && rattled.Count == rattledBefore;
        Tick(FrontRoomsShotTimings.Rattle.Duration - .2f + .05f);
        var late = world.Use(door.leaf);
        Check(opening == FrontRoomsMapWorld.UseResult.Rattled && early == FrontRoomsMapWorld.UseResult.None && earlyQuiet && late == FrontRoomsMapWorld.UseResult.Rattled && locked == lockedBefore + 1 && rattled.Count == rattledBefore + 1,
            label + "a try 0.2 s after a rattle answers " + early + " and raises nothing; at " + (FrontRoomsShotTimings.Rattle.Duration + .05f).ToString("0.00") + " s it rattles again (" + late + ")");
        Tick(FrontRoomsShotTimings.Rattle.Duration + .1f);

        // 2. With the key and a key-turn owner: the door is held for the shot, nothing raised.
        var zone = world.ZoneOf(a).id;
        world.GiveKeyForTools(zone);
        world.KeyTurnOwner = new object();
        result = world.Use(door.leaf);
        var held = world.IsHeldForKey(door);
        var prompt = world.Describe(door.leaf, out _);
        var again = world.Use(door.leaf);
        var tools = world.TryOpenDoor(a, b);
        Check(result == FrontRoomsMapWorld.UseResult.KeyTurn && unlocked.Count == 0 && held && prompt == null && again == FrontRoomsMapWorld.UseResult.None && !tools && !door.open,
            label + "with the key and an owner Use answers KeyTurn (" + result + "): held, no prompt, a second Use (" + again + ") and TryOpenDoor (" + tools + ") leave it, no DoorUnlocked");

        // 3. The commit: DoorUnlocked once with the zone and the keyhole; the leaf shifts as the bolt draws and settles home.
        world.CommitUnlock(door);
        var shiftedMax = 0f;
        for (var k = 0; k < 12; k++)
        {
            frame++;
            world.TickDoorsForTools(Dt);
            shiftedMax = Mathf.Max(shiftedMax, (door.leaf.transform.localPosition - home).magnitude);
        }
        Check(unlocked.Count == 1 && unlocked[0].door == door && unlocked[0].zone == zone && world.IsUnlockedForTools(door) && world.IsHeldForKey(door) && !door.open
              && shiftedMax > FrontRoomsShotTimings.Unlock.LeafShiftMetres * .5f && door.leaf.transform.localPosition == home,
            label + "CommitUnlock: DoorUnlocked once for this door and zone " + zone + ", unlocked and still held, the leaf shifts " + (shiftedMax * 1000f).ToString("0.0") + " mm and settles home");
        if (unlocked.Count > 0)
        {
            var at = unlocked[0].at;
            var rotation = door.hinge.parent.rotation * door.closed;
            var across = rotation * Vector3.right;
            var offset = at - door.position;
            var face = Vector3.Dot(offset, across);
            var expected = world.LockPose(door, player.position);
            Check(Mathf.Abs(offset.y - ModuleUnits.DoorHandleHeight) < .01f
                && Mathf.Abs(Vector3.Dot(offset, rotation * Vector3.forward) - (ModuleUnits.DoorWidth * .5f - ModuleUnits.DoorHandleInset)) < .01f
                && Mathf.Sign(face) == Mathf.Sign(Vector3.Dot(player.position - door.position, across))
                && Mathf.Abs(Mathf.Abs(face) - .0315f) < .001f && (expected.position - at).magnitude < 1e-4f
                && Vector3.Dot(expected.rotation * Vector3.forward, door.position - player.position) > 0f,
                label + "DoorUnlocked's point is the keyhole " + offset.ToString("F4") + ": 1.0 m up, 0.92 m from the hinge, 0.0315 m off the leaf's centre plane on the opener's face (LockPose, +Z into the lock)");
        }

        // 4. The release: a drawn latch opens at once (no lever wait, no DoorHandleTurned), to the swing side.
        var handlesBefore = handles;
        var movedBefore = moved;
        world.ReleaseKeyTurn(door, true, player.position, true);
        var beganNow = began.Count > 0 ? began[began.Count - 1] : default;
        var movedAtOnce = moved == movedBefore + 1;
        frame++;
        world.TickDoorsForTools(Dt);
        var leafMoving = door.angle > 0f;
        Tick(1f);
        Check(!world.IsHeldForKey(door) && handles == handlesBefore && beganNow.door == door && beganNow.source == FrontRoomsMapWorld.DoorOpenSource.Unlock && beganNow.latchDrawn
              && movedAtOnce && leafMoving && door.angle == ModuleUnits.DoorSwingDegrees && FrontRoomsMapWorld.OnSwingSide(door, door.leaf.transform.position),
            label + "ReleaseKeyTurn(open, latch drawn): no DoorHandleTurned, DoorOpenBegan(Unlock, drawn), DoorMoved at once, the leaf moving the next frame, open to its swing side (" + door.angle.ToString("0.0") + "°)");
        world.Use(door.leaf);
        Tick(1f);
        Check(!door.open, label + "shut again");
        result = world.Use(door.leaf);
        beganNow = began[began.Count - 1];
        Check(result == FrontRoomsMapWorld.UseResult.Opened && door.open && unlocked.Count == 1 && beganNow.source == FrontRoomsMapWorld.DoorOpenSource.Use && !beganNow.latchDrawn,
            label + "opening it again is plain: Opened, no second DoorUnlocked, DoorOpenBegan(Use, not drawn)");
        // From the other side, whose zone's key the player does not hold: a door a key has opened stays unlocked.
        Tick(1f);
        world.Use(door.leaf);
        Tick(1f);
        player.position = world.CellCenter(b) + Vector3.up * .05f;
        var otherZone = world.ZoneOf(b).id;
        lockedBefore = locked;
        result = world.Use(door.leaf);
        Check(otherZone != zone && !world.HasKeyFor(otherZone) && result == FrontRoomsMapWorld.UseResult.Opened && door.open && locked == lockedBefore && unlocked.Count == 1,
            label + "from the far side without its zone's key, the unlocked door opens (no DoorLocked, no second DoorUnlocked)");
        Tick(1f);

        // 5. No owner (the walker, tools): the key unlocks and opens at once.
        world.KeyTurnOwner = null;
        if (FindDoor(world, out var a2, out var b2, out _, (d, cell) => !used.Contains(d.edge)))
        {
            var door2 = world.DoorBetween(a2, b2);
            used.Add(door2.edge);
            player.position = world.CellCenter(a2) + Vector3.up * .05f;
            var zone2 = world.ZoneOf(a2).id;
            world.GiveKeyForTools(zone2);
            var count = unlocked.Count;
            result = world.Use(door2.leaf);
            Check(result == FrontRoomsMapWorld.UseResult.Opened && unlocked.Count == count + 1 && unlocked[unlocked.Count - 1].door == door2 && unlocked[unlocked.Count - 1].zone == zone2
                  && door2.open && !world.IsHeldForKey(door2) && world.IsUnlockedForTools(door2),
                label + "with no key-turn owner the key unlocks (DoorUnlocked at once) and opens it (" + result + "), no hold");
            Tick(1f);
        }
        else Check(false, label + "no second closed door for the no-owner case");

        // 6. A cancelled key turn leaves the door locked: from the side whose key is not held, it still rattles.
        world.KeyTurnOwner = new object();
        if (FindDoor(world, out var a3, out var b3, out _, (d, cell) => !used.Contains(d.edge) && !world.HasKeyFor(world.ZoneOf(OtherCell(d, cell)).id)))
        {
            var door3 = world.DoorBetween(a3, b3);
            used.Add(door3.edge);
            player.position = world.CellCenter(a3) + Vector3.up * .05f;
            world.GiveKeyForTools(world.ZoneOf(a3).id);
            var count = unlocked.Count;
            result = world.Use(door3.leaf);
            world.CancelKeyTurn(door3);
            player.position = world.CellCenter(b3) + Vector3.up * .05f;
            Tick(.5f);
            var after = world.Use(door3.leaf);
            Check(result == FrontRoomsMapWorld.UseResult.KeyTurn && !world.IsHeldForKey(door3) && !world.IsUnlockedForTools(door3) && unlocked.Count == count && after == FrontRoomsMapWorld.UseResult.Rattled && !door3.open,
                label + "CancelKeyTurn: the hold goes, the door is still locked, and without the key it rattles (" + after + ")");
            Tick(.5f);
        }
        else Check(false, label + "no third closed door for the cancel case");

        // 7. The tools path: TryOpenDoor never turns a key; with the key it unlocks and opens at once, without it nothing.
        if (FindDoor(world, out var a4, out var b4, out _, (d, cell) => !used.Contains(d.edge) && !world.HasKeyFor(world.ZoneOf(cell).id)))
        {
            var door4 = world.DoorBetween(a4, b4);
            used.Add(door4.edge);
            player.position = world.CellCenter(a4) + Vector3.up * .05f;
            lockedBefore = locked;
            var refused = !world.TryOpenDoor(a4, b4) && !door4.open && locked == lockedBefore;
            world.GiveKeyForTools(world.ZoneOf(a4).id);
            var count = unlocked.Count;
            var opened = world.TryOpenDoor(a4, b4);
            Check(refused && opened && unlocked.Count == count + 1 && door4.open && !world.IsHeldForKey(door4),
                label + "TryOpenDoor: refused without the key (nothing raised); with it DoorUnlocked at once and the door opens, no hold, even with an owner set");
        }
        else Check(false, label + "no fourth closed door for the tools case");
        world.KeyTurnOwner = null;

        // Keys off: never raised, never locked by the rule; DoorOpenBegan's sources; DoorsPaused freezes a swing.
        var keysOff = UnityEngine.Object.Instantiate(FrontRoomsLevelProfiles.Resolve());
        keysOff.hideFlags = HideFlags.DontSave;
        keysOff.doorsNeedKeys = false;
        profiles.Add(keysOff);
        var plain = World(roots, keysOff, -30f, "MAP INTERACTION TEST / no keys");
        plain.BuildForCapture();
        var count0 = 0;
        var plainBegan = new List<(FrontRoomsMapWorld.DoorOpenSource source, bool latchDrawn)>();
        int plainMoved = 0, plainLatched = 0;
        plain.DoorUnlocked += (_, __, ___) => count0++;
        plain.DoorOpenBegan += (d, src, latch) => plainBegan.Add((src, latch));
        plain.DoorMoved += _ => plainMoved++;
        plain.DoorLatched += (_, __) => plainLatched++;
        var plainDoors = Doors(plain).Values.Select(d => plain.DoorBetween(d.a, d.b)).Where(d => d != null).ToList();
        Check(plainDoors.Count > 0 && plainDoors.All(d => !plain.LockedByRule(d)), "keys off: LockedByRule is false for all " + plainDoors.Count + " doors");
        if (FindDoor(plain, out var pa, out var pb, out _))
        {
            plain.Player.position = plain.CellCenter(pa) + Vector3.up * .05f;
            var d = plain.DoorBetween(pa, pb);
            var r = plain.Use(d.leaf);
            Check(r == FrontRoomsMapWorld.UseResult.Opened && d.open && count0 == 0 && plainBegan.Count == 1 && plainBegan[0].source == FrontRoomsMapWorld.DoorOpenSource.Use && !plainBegan[0].latchDrawn,
                "keys off: Use opens the door (" + r + "), DoorOpenBegan(Use, not drawn), DoorUnlocked never raised");
            // A pause mid-swing: the leaf and its events wait, then go on.
            for (var s = 0f; s < .3f; s += Dt) plain.TickDoorsForTools(Dt);
            var angle = d.angle;
            var movedThen = plainMoved;
            var latchedThen = plainLatched;
            plain.DoorsPaused = true;
            for (var s = 0f; s < 1f; s += Dt) plain.TickDoorsForTools(Dt);
            var frozen = d.angle == angle && plainMoved == movedThen && plainLatched == latchedThen;
            plain.DoorsPaused = false;
            for (var s = 0f; s < 1f; s += Dt) plain.TickDoorsForTools(Dt);
            Check(angle > 0f && angle < ModuleUnits.DoorSwingDegrees && frozen && d.angle == ModuleUnits.DoorSwingDegrees,
                "DoorsPaused: a swing at " + angle.ToString("0.0") + "° holds still for 1 s with no DoorMoved or DoorLatched, then finishes at 95° once resumed");
        }
        else Check(false, "keys off: no closed door near the spawn");
        if (FindDoor(plain, out var ta, out var tb, out _))
        {
            plain.Player.position = plain.CellCenter(ta) + Vector3.up * .05f;
            plainBegan.Clear();
            var opened = plain.TryOpenDoor(ta, tb);
            Check(opened && plainBegan.Count == 1 && plainBegan[0].source == FrontRoomsMapWorld.DoorOpenSource.Tools && !plainBegan[0].latchDrawn && count0 == 0,
                "keys off: TryOpenDoor opens with DoorOpenBegan(Tools, not drawn), no DoorUnlocked");
        }
        else Check(false, "keys off: no second closed door near the spawn");
    }

    // The door's cell on the other side from cell.
    // ---------- Broken doors (RELAY §10) and the crosshair kinds (H1) ----------

    static void BrokenDoors(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles)
    {
        const string label = "broken doors: ";
        var profile = UnityEngine.Object.Instantiate(FrontRoomsLevelProfiles.Resolve());
        profile.hideFlags = HideFlags.DontSave;
        profile.doorsNeedKeys = true;
        profiles.Add(profile);
        var world = World(roots, profile, -46f, "MAP INTERACTION TEST / broken doors");
        world.BuildForCapture();
        // A door on a chunk line: its edge is the seed's alone (BorderEdge), so it is still a door after the revisit shift below.
        if (!FindDoor(world, out var a, out var b, out _, (d, cell) => MapGrid.ChunkOf(d.a) != MapGrid.ChunkOf(d.b))) { Check(false, label + "no closed door on a chunk line near the spawn"); return; }
        var door = world.DoorBetween(a, b);
        var player = world.Player;
        player.position = world.CellCenter(a) + Vector3.up * .05f;
        var here = world.ZoneOf(a).id;
        void Tick(float seconds) { for (var s = 0f; s < seconds - 1e-4f; s += Dt) world.TickDoorsForTools(Dt); }
        bool KindIs(FrontRoomsMapWorld.Door d, FrontRoomsMapWorld.AimKind want, string words)
        {
            var kind = world.Kind(d.leaf, player.position, out var zone, out var have);
            var text = world.Describe(d.leaf, out var hold);
            var locked = want == FrontRoomsMapWorld.AimKind.Locked || want == FrontRoomsMapWorld.AimKind.LockedHaveKey;
            return kind == want && !hold && (words == null ? text == null : text != null && text.Contains(words))
                && have == (want == FrontRoomsMapWorld.AimKind.LockedHaveKey) && (!locked || zone == here);
        }

        // H1 before any break: every shut door is Locked (no key), its key the zone the player stands in.
        var all = Doors(world).Values.Select(d => world.DoorBetween(d.a, d.b)).Where(d => d != null && !d.open).ToList();
        var lockedKinds = all.Count(d => KindIs(d, FrontRoomsMapWorld.AimKind.Locked, "LOCKED"));
        Check(all.Count > 0 && lockedKinds == all.Count, label + "Kind is Locked with Describe's LOCKED prompt on " + lockedKinds + "/" + all.Count + " shut doors (no key; keyZone = the player's zone)");
        var window = world.NearestIntactWindowForTools(player.position);
        if (window == null) Check(false, label + "no intact window near the spawn");
        else
        {
            var pane = window.pane.GetComponent<Collider>();
            var paneKind = world.Kind(pane, player.position, out _, out _);
            var paneText = world.Describe(pane, out var paneHold);
            var wantPane = FrontRoomsSettings.TapToBreak ? FrontRoomsMapWorld.AimKind.GlassTap : FrontRoomsMapWorld.AimKind.Glass;
            Check(paneKind == wantPane && paneHold && paneText != null, label + "a pane is " + paneKind + " (Tap to break " + FrontRoomsSettings.TapToBreak + ") with the hold prompt");
        }
        Check(world.Kind(null, player.position, out _, out _) == FrontRoomsMapWorld.AimKind.None, label + "Kind(null) is None");

        // Every door event, and DoorBrokenTried on this door.
        int moved = 0, lockedEvents = 0, rattled = 0, began = 0, handles = 0, unlocked = 0, pulled = 0, latched = 0, tried = 0;
        world.DoorMoved += _ => moved++;
        world.DoorLocked += _ => lockedEvents++;
        world.DoorRattled += (d, p) => rattled++;
        world.DoorOpenBegan += (d, src, latch) => began++;
        world.DoorHandleTurned += (d, p) => handles++;
        world.DoorUnlocked += (d, z, p) => unlocked++;
        world.DoorPulled += (d, p) => pulled++;
        world.DoorLatched += (d, p) => latched++;
        world.DoorBrokenTried += (d, p) => { if (d.edge == door.edge) tried++; };
        int DoorEvents() => moved + lockedEvents + rattled + began + handles + unlocked + pulled + latched;

        // The Relay breaks it from the far side; let the throw and the bounce settle.
        world.BreakDoor(door, world.CellCenter(b));
        Tick(4f);
        moved = lockedEvents = rattled = began = handles = unlocked = pulled = latched = 0;
        var angle = door.angle;
        var leafPose = door.leaf.transform.localRotation;

        // E: Broken, then nothing within BrokenDoor.Duration, then Broken again; never a door event or a move.
        var r1 = world.Use(door.leaf);
        var r2 = world.Use(door.leaf);
        Tick(.3f);
        var r3 = world.Use(door.leaf);
        Tick(.3f);
        var r4 = world.Use(door.leaf);
        Tick(1f);
        Check(r1 == FrontRoomsMapWorld.UseResult.Broken && r2 == FrontRoomsMapWorld.UseResult.None && r3 == FrontRoomsMapWorld.UseResult.None && r4 == FrontRoomsMapWorld.UseResult.Broken && tried == 2,
            label + "E answers " + r1 + ", within BrokenDoor.Duration " + r2 + " and " + r3 + " (0.3 s), after it " + r4 + "; DoorBrokenTried " + tried + "x");
        Check(DoorEvents() == 0 && door.broken && door.open && Mathf.Abs(door.angle - angle) < .01f && Quaternion.Angle(door.leaf.transform.localRotation, leafPose) < .01f,
            label + "E never moves it and raises no door event (so no Relay noise): " + DoorEvents() + " events, still broken and open at " + door.angle.ToString("0.0") + " deg");
        Check(KindIs(door, FrontRoomsMapWorld.AimKind.Broken, null), label + "kind Broken, no prompt (the strings are the graphic chat's)");
        Check(world.PassageBetween(a, b) == FrontRoomsMapWorld.Passage.Open && world.OpeningCostBetween(a, b) == 0f,
            label + "an Open passage (" + world.PassageBetween(a, b) + "), " + world.OpeningCostBetween(a, b) + " m in the path field");
        Check(world.LockedByRule(door) && !world.HasKeyFor(here) && !world.HasKeyFor(world.ZoneOf(b).id) && world.PassageBetween(a, b) == FrontRoomsMapWorld.Passage.Open,
            label + "a broken locked door lets a keyless player through");

        // Kept by edge: a rebuild, then a revisit shift, give a new Door that is still broken and still answers Broken.
        var chunks = new HashSet<GridCoord> { MapGrid.ChunkOf(a), MapGrid.ChunkOf(b) };
        foreach (var c in chunks) world.RebuildChunk(c);
        var rebuilt = world.DoorBetween(a, b);
        var rebuiltOk = rebuilt != null && rebuilt != door && rebuilt.broken && rebuilt.open && KindIs(rebuilt, FrontRoomsMapWorld.AimKind.Broken, null)
            && world.Use(rebuilt.leaf) == FrontRoomsMapWorld.UseResult.Broken;
        foreach (var c in chunks) { world.Cache.Shift(c); world.RebuildChunk(c); }
        var shifted = world.DoorBetween(a, b);
        var shiftedOk = shifted != null && shifted != rebuilt && shifted.broken && shifted.open && KindIs(shifted, FrontRoomsMapWorld.AimKind.Broken, null)
            && world.Use(shifted.leaf) == FrontRoomsMapWorld.UseResult.Broken && world.PassageBetween(a, b) == FrontRoomsMapWorld.Passage.Open;
        Check(rebuiltOk && shiftedOk, label + "still broken, Open and answering Broken after a rebuild (" + rebuiltOk + ") and a revisit shift (" + shiftedOk + ")");

        // H1 with the key: LockedHaveKey; one opened is Shut; shut again it is Open (unlocked for good).
        world.GiveKeyForTools(here);
        var shut = Doors(world).Values.Select(d => world.DoorBetween(d.a, d.b)).Where(d => d != null && !d.open && !d.broken).ToList();
        var haveKinds = shut.Count(d => KindIs(d, FrontRoomsMapWorld.AimKind.LockedHaveKey, "OPEN DOOR"));
        Check(shut.Count > 0 && haveKinds == shut.Count, label + "with the key, Kind is LockedHaveKey with the OPEN prompt on " + haveKinds + "/" + shut.Count + " shut doors");
        if (shut.Count > 0)
        {
            var other = shut[0];
            var opened = world.Use(other.leaf);
            var shutKind = KindIs(other, FrontRoomsMapWorld.AimKind.Shut, "SHUT DOOR");
            Tick(4f);
            var closed = world.Use(other.leaf);
            Tick(4f);
            Check(opened == FrontRoomsMapWorld.UseResult.Opened && shutKind && closed == FrontRoomsMapWorld.UseResult.Shut && !other.open && KindIs(other, FrontRoomsMapWorld.AimKind.Open, "OPEN DOOR"),
                label + "opened with the key it is Shut (" + opened + ", " + shutKind + "); shut again (" + closed + ") it is Open, unlocked for good");
        }
    }

    static GridCoord OtherCell(FrontRoomsMapWorld.Door door, GridCoord cell) => door.a == cell ? door.b : door.a;

    // ---------- Double-acting doors (A12, Red 2026-10-22): away from the opener, the side kept, pulls only on a shut ----------

    const float HingeStepLimit = FrontRoomsMapWorld.DoorMaxStepDegrees;

    /// <summary>A door's floor plan as the map defines it: x along the wall from the hinge axis to the latch, y out into its swing side now.</summary>
    static Vector2 Plan(FrontRoomsMapWorld.Door door, Vector3 p)
    {
        var rotation = door.hinge.parent.rotation * door.closed;
        var offset = p - door.hinge.position;
        return new Vector2(Vector3.Dot(offset, rotation * Vector3.forward), Vector3.Dot(offset, rotation * Vector3.right * -door.swing));
    }

    /// <summary>A point in a door's floor plan (on its swing side now), on the floor.</summary>
    static Vector3 FromPlan(FrontRoomsMapWorld.Door door, float along, float outward)
    {
        var rotation = door.hinge.parent.rotation * door.closed;
        var p = door.hinge.position + rotation * Vector3.forward * along + rotation * Vector3.right * (-door.swing * outward);
        p.y = door.hinge.position.y;
        return p;
    }

    /// <summary>A point on one face of a door, on the floor: sign +1 on the door root's +X face (DoorFace.S), -1 on its -X face (P).</summary>
    static Vector3 OnFace(FrontRoomsMapWorld.Door door, float along, float outward, float sign)
    {
        var rotation = door.hinge.parent.rotation * door.closed;
        var p = door.hinge.position + rotation * Vector3.forward * along + rotation * Vector3.right * (sign * outward);
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

    // The player and their test body at a point.
    static void Put(Transform player, Collider body, Vector3 at)
    {
        player.position = at;
        body.transform.position = at;
        Physics.SyncTransforms();
    }

    static void DoubleActing(List<GameObject> roots)
    {
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), -41f, "MAP INTERACTION TEST / double-acting");
        world.BuildForCapture();
        var player = world.Player;

        // 1. Every open goes to the far side of its opener, from either side, and an open door keeps that side
        //    through a rebuild and a revisit shift.
        var opened = new Dictionary<long, (GridCoord from, GridCoord into)>();
        foreach (var d in Doors(world).Values)
        {
            var candidate = world.DoorBetween(d.a, d.b);
            if (candidate == null || candidate.open || candidate.broken || !world.IsBuilt(d.a) || !world.IsBuilt(d.b)) continue;
            // The opener's side alternates, whatever side the leaf last went to.
            var from = opened.Count % 2 == 0 ? d.a : d.b;
            var into = from == d.a ? d.b : d.a;
            player.position = world.CellCenter(from);
            if (world.TryOpenDoor(from, into)) opened[candidate.edge] = (from, into);
            if (opened.Count >= 16) break;
        }
        for (var k = 0; k < 90; k++) world.TickDoorsForTools(Dt);
        int FarSide(Func<long, bool> keep)
        {
            var n = 0;
            foreach (var o in opened)
            {
                if (!keep(o.Key)) continue;
                var door = world.DoorBetween(o.Value.from, o.Value.into);
                if (door != null && door.open && door.angle == ModuleUnits.DoorSwingDegrees && world.CellOf(door.leaf.transform.position) == o.Value.into) n++;
            }
            return n;
        }
        var intoNear = opened.Values.Count(o => o.into.x < o.from.x || o.into.y < o.from.y);
        var away = FarSide(_ => true);
        Check(opened.Count >= 6 && away == opened.Count && intoNear > 0 && intoNear < opened.Count,
            "double-acting: " + away + "/" + opened.Count + " doors opened from alternating sides rest in the cell beyond their opener (" + intoNear + " into the west/south cell: both sides occur)");
        var chunks = opened.Values.Select(o => MapGrid.ChunkOf(o.from)).Concat(opened.Values.Select(o => MapGrid.ChunkOf(o.into))).Distinct().ToList();
        foreach (var c in chunks) world.RebuildChunk(c);
        var afterRebuild = FarSide(_ => true);
        Check(afterRebuild == opened.Count,
            "double-acting: after rebuilding " + chunks.Count + " chunks, " + afterRebuild + "/" + opened.Count + " open doors are rebuilt open on the side they went to");
        foreach (var c in chunks) { world.Cache.Shift(c); world.RebuildChunk(c); }
        bool Kept(long edge) => opened.TryGetValue(edge, out var o) && world.DoorBetween(o.from, o.into) is FrontRoomsMapWorld.Door again && again.edge == edge;
        var common = opened.Keys.Count(Kept);
        var afterShift = FarSide(Kept);
        Check(common >= 2 && afterShift == common,
            "double-acting: after a revisit shift, " + afterShift + "/" + common + " open doors still on their edges are on the side they went to");

        // A door to work: shut, both cells built.
        FrontRoomsMapWorld.Door door = null;
        foreach (var d in Doors(world).Values)
        {
            var candidate = world.DoorBetween(d.a, d.b);
            if (candidate != null && !candidate.open && !candidate.broken && world.IsBuilt(d.a) && world.IsBuilt(d.b)) { door = candidate; break; }
        }
        if (door == null) { Check(false, "double-acting: no shut door near the spawn"); return; }
        var moved = 0;
        var latchedEvents = 0;
        var pulled = new List<(Vector3 clear, float angleThen, bool openThen)>();
        world.DoorMoved += _ => moved++;
        world.DoorLatched += (d, _) => { if (d == door) latchedEvents++; };
        world.DoorPulled += (d, clear) => { if (d == door) pulled.Add((clear, d.angle, d.open)); };
        var leaf = door.leaf;
        var playerBody = TestBody(world, "test player body", ModuleUnits.PlayerRadius);

        // 2. A push from either face: the lever, then the leaf eases out to the far side, away from the opener.
        foreach (var sign in new[] { 1f, -1f })
        {
            var face = sign > 0f ? "S" : "P";
            ShutAndLatch(world, door, player, playerBody);
            Put(player, playerBody, OnFace(door, .5f, .9f, sign));
            var movedBefore = moved;
            pulled.Clear();
            world.Use(leaf);
            Check(door.open && door.angle == 0f && moved == movedBefore && pulled.Count == 0,
                "double-acting: push from face " + face + ": open at once, the leaf waits for the lever, no DoorPulled (angle " + door.angle + ", DoorPulled " + pulled.Count + ")");
            float t = 0f, startAt = -1f, maxAngle = 0f, maxStep = 0f, restAt = -1f;
            int movedAtStart = -1, passageWrong = 0, overlapFrames = 0;
            var secondUseIgnored = false;
            for (var k = 1; k <= 90; k++)
            {
                var was = door.angle;
                world.TickDoorsForTools(Dt);
                Physics.SyncTransforms();
                t = k * Dt;
                if (startAt < 0f && door.angle > 0f) { startAt = t; movedAtStart = moved - movedBefore; }
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
            Check(secondUseIgnored, "double-acting: face " + face + ": a second E while the lever is down does nothing");
            Check(Mathf.Abs(startAt - FrontRoomsShotTimings.Open.SwingStart) <= Dt * 1.1f && movedAtStart == 1,
                "double-acting: face " + face + ": the leaf leaves the frame " + startAt.ToString("0.000") + " s after E (lever " + FrontRoomsShotTimings.Open.SwingStart + " s, no pull beat), with DoorMoved then (" + movedAtStart + ")");
            Check(Mathf.Abs(maxAngle - (ModuleUnits.DoorSwingDegrees + FrontRoomsShotTimings.Open.OvershootDeg)) < .3f && door.angle == ModuleUnits.DoorSwingDegrees
                && Mathf.Abs(restAt - FrontRoomsShotTimings.Open.Duration) <= Dt * 2.1f,
                "double-acting: face " + face + ": it eases out, overshoots to " + maxAngle.ToString("0.0") + "° and rests at " + door.angle.ToString("0.0") + "° at " + restAt.ToString("0.000") + " s (expected 97 / 95 / " + FrontRoomsShotTimings.Open.Duration + ")");
            Check(FrontRoomsMapWorld.FaceOf(door, leaf.transform.position) != FrontRoomsMapWorld.FaceOf(door, player.position) && FrontRoomsMapWorld.OnSwingSide(door, leaf.transform.position)
                && !FrontRoomsMapWorld.OnSwingSide(door, player.position) && world.CellOf(leaf.transform.position) != world.CellOf(player.position),
                "double-acting: pushed from face " + face + ", the leaf rests on the far face, in the cell beyond the opener");
            Check(passageWrong == 0 && world.PassageBetween(door.a, door.b) == FrontRoomsMapWorld.Passage.Open && maxStep <= HingeStepLimit && overlapFrames == 0,
                "double-acting: face " + face + ": PassageBetween ClosedDoor below " + FrontRoomsMapWorld.DoorPassableDegrees + "° and Open from there (" + passageWrong + " frames wrong); largest hinge step " + maxStep.ToString("0.0") + "°; the opener never touched");
        }

        // 3. Shut from the far side: at once, a smooth close that lands in the frame, latched, never toward the shutter.
        {
            var movedBefore = moved;
            var latchedBefore = latchedEvents;
            pulled.Clear();
            world.Use(leaf);
            var minAngle = float.MaxValue;
            var shutAt = -1f;
            for (var k = 1; k <= 60; k++)
            {
                world.TickDoorsForTools(Dt);
                minAngle = Mathf.Min(minAngle, door.angle);
                if (shutAt < 0f && door.angle == 0f) shutAt = k * Dt;
            }
            Check(!door.open && moved == movedBefore + 1 && minAngle == 0f && Quaternion.Angle(door.hinge.localRotation, door.closed) < .01f && pulled.Count == 0
                && latchedEvents == latchedBefore + 1 && Mathf.Abs(shutAt - FrontRoomsShotTimings.Open.SwingSeconds) <= Dt * 1.1f,
                "double-acting: shut from the far side at once (DoorMoved, no DoorPulled), latched at " + shutAt.ToString("0.000") + " s (DoorLatched), lowest angle " + minAngle.ToString("0.00") + "°");
        }

        // 4. From the side the leaf last went to, standing in its old sweep (the single-acting pull): it turns away instead.
        {
            var at = FromPlan(door, .5f, .62f);
            Put(player, playerBody, at);
            var sideBefore = door.swing;
            var movedBefore = moved;
            pulled.Clear();
            world.Use(leaf);
            var turned = door.swing == -sideBefore && !FrontRoomsMapWorld.OnSwingSide(door, at);
            float startAt = -1f;
            var overlapFrames = 0;
            for (var k = 1; k <= 90; k++)
            {
                world.TickDoorsForTools(Dt);
                Physics.SyncTransforms();
                if (startAt < 0f && door.angle > 0f) startAt = k * Dt;
                if (Overlaps(leaf, playerBody)) overlapFrames++;
            }
            Check(turned && pulled.Count == 0 && overlapFrames == 0 && door.angle == ModuleUnits.DoorSwingDegrees && moved == movedBefore + 1
                && Mathf.Abs(startAt - FrontRoomsShotTimings.Open.SwingStart) <= Dt * 1.1f && world.CellOf(leaf.transform.position) != world.CellOf(at),
                "double-acting: from the side it last opened to (0.62 m out, in its old sweep) the leaf turns over and opens away at " + startAt.ToString("0.000") + " s: no DoorPulled, no pull beat, never touches the opener");
        }

        // 5. Shutting onto a body in the doorway: the leaf stops on it, short of the frame.
        Put(player, playerBody, FromPlan(door, .5f, 0f));
        world.Use(leaf);
        var stopFrames = 0;
        for (var k = 1; k <= 60; k++)
        {
            world.TickDoorsForTools(Dt);
            Physics.SyncTransforms();
            if (Overlaps(leaf, playerBody)) stopFrames++;
        }
        Check(!door.open && stopFrames == 0 && door.angle > 20f,
            "double-acting: shutting on a player in the doorway, the leaf stops on them at " + door.angle.ToString("0.0") + "°");
        // They step out (to the other side): it shuts and latches.
        Put(player, playerBody, FromPlan(door, .5f, -1.5f));
        for (var k = 1; k <= 60; k++) world.TickDoorsForTools(Dt);
        Check(door.angle == 0f, "double-acting: once they step out, it shuts and latches");

        // 6. The Relay's body stops a leaf too (RelayBody, as the hunter sets it): a push from the far side meets it.
        var relayAt = FromPlan(door, .55f, .6f);
        var relayBody = TestBody(world, "test relay body", ModuleUnits.RelayRadius);
        relayBody.transform.position = relayAt;
        world.RelayBody = () => relayAt;
        Put(player, playerBody, FromPlan(door, .5f, -1.5f));
        world.Use(leaf);
        var relayOverlap = 0;
        for (var k = 1; k <= 60; k++)
        {
            world.TickDoorsForTools(Dt);
            Physics.SyncTransforms();
            if (Overlaps(leaf, relayBody)) relayOverlap++;
        }
        var heldAt = door.angle;
        world.RelayBody = null;
        relayBody.transform.position += Vector3.down * 10f;
        for (var k = 1; k <= 60; k++) world.TickDoorsForTools(Dt);
        Check(relayOverlap == 0 && heldAt > 1f && heldAt < 60f && door.angle == ModuleUnits.DoorSwingDegrees,
            "double-acting: a push stops on the Relay's body beyond the door at " + heldAt.ToString("0.0") + "° and goes on once it is gone");

        // 7. Shutting from the leaf's side, standing in its sweep: DoorPulled with a clear spot, and the game's step
        //    (PullStepSpeed for at most PullStepSeconds; Red's A12 answer keeps it) gets them clear while it shuts.
        {
            var stepStart = FromPlan(door, .5f, .62f);
            Put(player, playerBody, stepStart);
            pulled.Clear();
            world.Use(leaf);
            var stepOk = pulled.Count == 1 && pulled[0].angleThen == ModuleUnits.DoorSwingDegrees && !pulled[0].openThen;
            var clear = stepOk ? pulled[0].clear : stepStart;
            var plan = Plan(door, clear);
            var reach = ModuleUnits.DoorWidth - ModuleUnits.DoorLeafGap * .5f + ModuleUnits.DoorLeafThickness * .5f + ModuleUnits.PlayerRadius;
            Check(stepOk && plan.magnitude >= reach + .03f && plan.x >= ModuleUnits.DoorWidth * .5f
                && plan.y >= ModuleUnits.WallHalf + ModuleUnits.PlayerRadius && plan.y <= ModuleUnits.WallHalf + ModuleUnits.DoorClearDepth - ModuleUnits.PlayerRadius
                && plan.x <= (MapGrid.CellSize + ModuleUnits.DoorWidth) * .5f - ModuleUnits.WallHalf - ModuleUnits.PlayerRadius
                && world.CellOf(clear) == world.CellOf(stepStart),
                "double-acting: shut from the leaf's side in its sweep: DoorPulled once, the clear spot " + plan.magnitude.ToString("0.00") + " m from the hinge (sweep + body " + reach.ToString("0.00") + "), "
                + plan.x.ToString("0.00") + " m toward the latch, " + plan.y.ToString("0.00") + " m out, in the keep-clear strip and the same cell");
            var stepped = 0f;
            var overlapFrames = 0;
            var bodyStoppedLeaf = false;
            for (var k = 1; k <= 90; k++)
            {
                var was = door.angle;
                if (k >= 2 && stepped <= FrontRoomsMapWorld.PullStepSeconds)
                {
                    var to = clear - player.position;
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
                // Once moving, the leaf only goes on toward the frame (a body in the way would hold it).
                if (was > 1f && was < ModuleUnits.DoorSwingDegrees - 1f && door.angle >= was - 1e-4f) bodyStoppedLeaf = true;
            }
            Check(stepOk && overlapFrames == 0 && !bodyStoppedLeaf && door.angle == 0f && Flat(player.position - clear) < 1e-3f && stepped <= FrontRoomsMapWorld.PullStepSeconds + Dt,
                "double-acting: the step: the player walks " + Flat(clear - stepStart).ToString("0.00") + " m clear in " + stepped.ToString("0.00")
                + " s (≤ " + FrontRoomsMapWorld.PullStepSeconds + "), the leaf shuts to " + door.angle.ToString("0.0") + "° without touching or stopping on them");
        }

        // 8a. E on a leaf resting on its shutter (a chase: no step): the leaf turns back open, away from them.
        {
            Put(player, playerBody, FromPlan(door, .5f, -.9f));
            world.Use(leaf);
            for (var k = 1; k <= 90; k++) world.TickDoorsForTools(Dt);
            var inSweep = FromPlan(door, .5f, .62f);
            Put(player, playerBody, inSweep);
            world.Use(leaf);
            for (var k = 1; k <= 60; k++) { world.TickDoorsForTools(Dt); Physics.SyncTransforms(); }
            var restingAt = door.angle;
            var side = door.swing;
            pulled.Clear();
            world.Use(leaf);
            var overlapFrames = 0;
            var minAngle = restingAt;
            for (var k = 1; k <= 60; k++)
            {
                world.TickDoorsForTools(Dt);
                Physics.SyncTransforms();
                minAngle = Mathf.Min(minAngle, door.angle);
                if (Overlaps(leaf, playerBody)) overlapFrames++;
            }
            Check(restingAt > 1f && restingAt < ModuleUnits.DoorSwingDegrees && door.open && door.swing == side && minAngle >= restingAt - 1e-3f && door.angle == ModuleUnits.DoorSwingDegrees
                && overlapFrames == 0 && pulled.Count == 0,
                "double-acting: E on a leaf resting on its shutter at " + restingAt.ToString("0.0") + "°: it turns back open to 95° on its side, away from them, never toward them");
        }

        // 8b. Push-through: the shutter stands behind the leaf (between it and the hinge-side wall); the leaf shuts away
        //     from them, and E again mid-shut carries it on through the frame to the far side, without latching.
        {
            var behind = FromPlan(door, -.45f, .55f);
            Put(player, playerBody, behind);
            pulled.Clear();
            world.Use(leaf);
            var noStep = pulled.Count == 0;
            for (var k = 1; k <= 60 && door.angle > 45f; k++) { world.TickDoorsForTools(Dt); Physics.SyncTransforms(); }
            var midAngle = door.angle;
            var side = door.swing;
            var movedBefore = moved;
            var latchedBefore = latchedEvents;
            world.Use(leaf);
            var movedAtUse = moved - movedBefore;
            int zeroFrames = 0, overlapFrames = 0;
            var maxStep = 0f;
            var crossedAt = -1f;
            for (var k = 1; k <= 90; k++)
            {
                var was = door.angle;
                world.TickDoorsForTools(Dt);
                Physics.SyncTransforms();
                if (door.angle == 0f) zeroFrames++;
                if (crossedAt < 0f && door.swing == -side) crossedAt = k * Dt;
                maxStep = Mathf.Max(maxStep, Mathf.Abs(door.angle - was));
                if (Overlaps(leaf, playerBody)) overlapFrames++;
            }
            var expectCross = midAngle / (2f * FrontRoomsShotTimings.Open.SwingDeg / FrontRoomsShotTimings.Open.SwingSeconds);
            Check(noStep && midAngle > 20f && midAngle < 60f && movedAtUse == 1 && latchedEvents == latchedBefore && zeroFrames <= 1 && door.swing == -side
                && Mathf.Abs(crossedAt - expectCross) <= Dt * 2.1f && door.open && door.angle == ModuleUnits.DoorSwingDegrees && !FrontRoomsMapWorld.OnSwingSide(door, behind)
                && world.CellOf(leaf.transform.position) != world.CellOf(behind) && overlapFrames == 0 && maxStep <= HingeStepLimit,
                "double-acting: push-through from behind the leaf at " + midAngle.ToString("0.0") + "°: DoorMoved once at E, through the frame at " + crossedAt.ToString("0.000")
                + " s (" + expectCross.ToString("0.000") + " expected, " + zeroFrames + " frame at 0°, no DoorLatched), open to 95° on the far side, never touching them");
            var edge = door.edge;
            var leafCell = world.CellOf(leaf.transform.position);
            var sideNow = door.swing;
            world.RebuildChunk(MapGrid.ChunkOf(door.a));
            var again = world.DoorBetween(door.a, door.b);
            Check(again != null && again != door && again.edge == edge && again.open && again.angle == ModuleUnits.DoorSwingDegrees && again.swing == sideNow
                && world.CellOf(again.leaf.transform.position) == leafCell,
                "double-acting: rebuilt after a push-through, the door is open at 95° on the side it went to");
            door = again;
            leaf = door.leaf;
        }

        // 9. Beside the hinge, on the side the leaf last went to: it turns over and opens away; no one is moved or touched.
        {
            ShutAndLatch(world, door, player, playerBody);
            var hingeSide = FromPlan(door, -.5f, .75f);
            Put(player, playerBody, hingeSide);
            pulled.Clear();
            world.Use(leaf);
            var overlapFrames = 0;
            for (var k = 1; k <= 90; k++)
            {
                world.TickDoorsForTools(Dt);
                Physics.SyncTransforms();
                if (Overlaps(leaf, playerBody)) overlapFrames++;
            }
            Check(door.open && !door.pull && pulled.Count == 0 && overlapFrames == 0 && door.angle == ModuleUnits.DoorSwingDegrees && !FrontRoomsMapWorld.OnSwingSide(door, hingeSide),
                "double-acting: from beside the hinge the leaf opens to the far side (" + door.angle.ToString("0.0") + "°): no DoorPulled, never touched");
        }
        UnityEngine.Object.DestroyImmediate(playerBody.gameObject);
        UnityEngine.Object.DestroyImmediate(relayBody.gameObject);
    }

    // Shut a door from the side away from its leaf and tick it into the frame.
    static void ShutAndLatch(FrontRoomsMapWorld world, FrontRoomsMapWorld.Door door, Transform player, Collider playerBody)
    {
        player.position = FromPlan(door, .5f, -1.5f);
        playerBody.transform.position = player.position;
        Physics.SyncTransforms();
        if (door.open) world.Use(door.leaf);
        for (var k = 1; k <= 90; k++) world.TickDoorsForTools(Dt);
    }

    // ---------- The Relay breaking a door from either side (A12: the leaf always bursts away from it) ----------

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
        foreach (var ledToward in new[] { true, false })
        {
            var label = "relay breaks a door last open " + (ledToward ? "toward" : "away from") + " it: ";
            if (!FindDoor(world, out var a, out var b, out var c, (d, near) => FrontRoomsMapWorld.OnSwingSide(d, world.CellCenter(near)) == ledToward))
            {
                Check(false, label + "no closed door with an open cell behind it near the spawn");
                continue;
            }
            var door = world.DoorBetween(a, b);
            // A12: whichever side the leaf last went to, a shut leaf bursts away from the Relay, into the far cell b.
            var swingCell = b;
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
            Check(door.broken && brokenFrom.Count == 1 && !brokenFrom[0].swingSide && (brokenFrom[0].at - stance).sqrMagnitude < .01f,
                label + "DoorBrokenFrom once, from the side away from the leaf (" + (brokenFrom.Count > 0 ? (brokenFrom[0].swingSide ? "toward" : "away") : "-") + "), at where it stood");
            Check(breakFrames > 0 && standOnSwingSide == 0,
                label + "it shoulders the door in front of the opening for the whole break, never on the leaf's side (" + standOnSwingSide + " frames; " + minReach.ToString("0.00") + " m from the hinge, sweep " + reachRelay.ToString("0.00") + ")");
            Check(markedFrames == breakFrames && promptFrames == 0 && useIgnored && !world.IsBeingBroken(door),
                label + "while it breaks the door: IsBeingBroken, no prompt, E does nothing; cleared once broken");
            Check(minFacing > .3f, label + "it faces the door while it breaks it (worst " + minFacing.ToString("0.00") + ")");
            Check(maxJolt > FrontRoomsShotTimings.DoorBreak.LeafJoltMinMetres * .5f,
                label + "each blow jolts the leaf child (largest " + (maxJolt * 1000f).ToString("0.0") + " mm)");
            var tilt = Quaternion.Angle(door.leaf.transform.localRotation, Quaternion.identity);
            Check(world.CellOf(door.leaf.transform.position) == swingCell && FrontRoomsMapWorld.OnSwingSide(door, door.leaf.transform.position)
                && Mathf.Abs(door.angle - FrontRoomsShotTimings.DoorBreak.BounceRestDeg) < .01f
                && maxAngle >= FrontRoomsMapWorld.DoorBreakThrowDegrees - 1f && maxStep <= HingeStepLimit && Mathf.Abs(tilt - FrontRoomsShotTimings.DoorBreak.CrookedDeg) < .05f,
                label + "the leaf is thrown to " + maxAngle.ToString("0.0") + "° into the far cell " + swingCell + " (away from the Relay) and rests at " + door.angle.ToString("0.0") + "°, " + tilt.ToString("0.0") + "° crooked; largest hinge step " + maxStep.ToString("0.0") + "°");
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

        // A12 contract rule 7, the kept rip: a leaf caught partly open on the Relay's side keeps its side. The Relay stands beside
        // the latch, out of the leaf's sweep (BreakStance: ClearOfSwing), and the leaf is thrown toward it (DoorBrokenFrom from the
        // swing side). The leaf is held by the map's player, a body that stays put: on the Relay's own body a leaf would go on
        // shutting once it stepped away, and latch.
        RelayRipsHeldLeaf(world, player, body, reachRelay);
    }

    static void RelayRipsHeldLeaf(FrontRoomsMapWorld world, Transform player, Collider body, float reachRelay)
    {
        const string label = "relay breaks a leaf held partly open on its side (the rip): ";
        if (!FindDoor(world, out var a, out var b, out var c))
        {
            Check(false, label + "no fourth closed door with an open cell behind it near the spawn");
            return;
        }
        var door = world.DoorBetween(a, b);
        // The Relay first, standing in c: from here on a leaf stops on its body (RelayBody), not on an earlier case's Relay.
        var tuning = new FrontRoomsHunterTuning { sightRange = 0f };
        var hunter = new FrontRoomsMapHunter(world, tuning, body, null, 5);
        hunter.DebugPlace(world.CellCenter(c));
        var target = world.CellCenter(b);
        player.position = target;
        // The map's player (BuildForCapture's eye: LimitByBodies stops every leaf on it) waits in b, behind the opening leaf.
        var holder = world.Player;
        var holderWas = holder.position;
        holder.position = world.CellCenter(b);
        Physics.SyncTransforms();
        // Opened from the far cell b, the leaf swings away from there: into a, the side the Relay will come from.
        var opened = world.TryOpenDoor(b, a);
        for (var k = 0; k < 90; k++) world.TickDoorsForTools(Dt);
        // Then it steps into the leaf's return sweep in the mouth of the opening (0.60 m along, 0.40 m out, its body just off the
        // trim's face) and shuts it from there: the leaf stops on it about 64 degrees out of the frame (bearing 33.7 + contact
        // half-angle 30.4), short of DoorPassableDegrees (70), so the door is still no way through. That spot is about 0.65 m
        // from the Relay's stance beside the latch (ClearOfSwing: about 1.10 m along, 0.81 m out), so the two bodies (0.3 m
        // each) do not overlap. Nearer the hinge the leaf would rest at 70 degrees or more (open: no rip); further along it
        // would stand within two bodies of the Relay. Nothing answers DoorPulled here (no game), so it stays put.
        var holderAt = FromPlan(door, .6f, .4f);
        holder.position = holderAt;
        Physics.SyncTransforms();
        var shut = world.Use(door.leaf);
        for (var k = 0; k < 90; k++) world.TickDoorsForTools(Dt);
        Physics.SyncTransforms();
        var heldAt = door.angle;
        var leafInA = world.CellOf(door.leaf.transform.position) == a;
        Check(opened && shut == FrontRoomsMapWorld.UseResult.Shut && !door.open && !door.broken && !FrontRoomsMapWorld.IsLatched(door) && heldAt >= 30f && heldAt < FrontRoomsMapWorld.DoorPassableDegrees
            && FrontRoomsMapWorld.OnSwingSide(door, world.CellCenter(a)) && leafInA && world.PassageBetween(a, b) == FrontRoomsMapWorld.Passage.ClosedDoor,
            label + "setup: opened from " + b + " and shut onto a body standing in its sweep, the leaf rests at " + heldAt.ToString("0.0") + "° in " + a
            + " (" + (leafInA ? "the Relay's side" : "the far side") + "), shut but not latched, no way through");

        hunter.Noise(target, 1000f);
        var brokenEvents = 0;
        var brokenSwingSide = false;
        var brokenAngle = -1f;
        var brokenAt = Vector3.zero;
        void OnBrokenFrom(FrontRoomsMapWorld.Door d, Vector3 at, bool swingSide)
        {
            if (d.edge != door.edge) return;
            brokenEvents++;
            brokenSwingSide = swingSide;
            brokenAngle = d.angle;
            brokenAt = at;
        }
        world.DoorBrokenFrom += OnBrokenFrom;
        float minReach = float.MaxValue, maxAngle = 0f, maxStep = 0f, startAngle = -1f;
        // Where it stands in the door's plan over the break (along from the hinge, out into the swing side, from the hinge),
        // and how squarely it faces the lock on its own face.
        float minAlong = float.MaxValue, maxOut = float.MinValue, maxReach = 0f, minLockFacing = 1f;
        int breakFrames = 0, standOnSwingSide = 0;
        Vector3 stance = default;
        try
        {
            for (var t = 0f; t < 40f; t += Dt)
            {
                var was = door.angle;
                hunter.Tick(Dt, target, target + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
                // The leaf as the break begins (a latched leaf would have been turned away by now; this one is out of the frame).
                if (hunter.State == HunterState.BreakDoor && breakFrames == 0) startAngle = door.angle;
                world.TickDoorsForTools(Dt);
                Physics.SyncTransforms();
                maxAngle = Mathf.Max(maxAngle, door.angle);
                maxStep = Mathf.Max(maxStep, Mathf.Abs(door.angle - was));
                // Every frame of the break, the throw and the fall included (it stands still until it walks on).
                if (hunter.State == HunterState.BreakDoor)
                {
                    breakFrames++;
                    if (!door.broken) stance = hunter.Position;
                    if (FrontRoomsMapWorld.OnSwingSide(door, hunter.Position)) standOnSwingSide++;
                    var plan = Plan(door, hunter.Position);
                    minReach = Mathf.Min(minReach, plan.magnitude);
                    minAlong = Mathf.Min(minAlong, plan.x);
                    maxOut = Mathf.Max(maxOut, plan.y);
                    maxReach = Mathf.Max(maxReach, plan.magnitude);
                    var toLock = FrontRoomsMapWorld.LockPoint(door, hunter.Position) - hunter.Position;
                    toLock.y = 0f;
                    minLockFacing = Mathf.Min(minLockFacing, Vector3.Dot(hunter.Heading, toLock.normalized));
                }
                if (door.broken && hunter.State == HunterState.Search) break;
            }
        }
        finally
        {
            world.DoorBrokenFrom -= OnBrokenFrom;
            holder.position = holderWas;
            Physics.SyncTransforms();
        }
        Check(breakFrames > 0 && startAngle >= 30f && startAngle < FrontRoomsMapWorld.DoorPassableDegrees,
            label + "it breaks the door (no wait at the stance: the door is shut, not open) with the leaf still out of the frame at " + startAngle.ToString("0.0") + "° (not latched)");
        Check(breakFrames > 0 && standOnSwingSide == breakFrames && minReach >= reachRelay,
            label + "it stands beside the latch on the leaf's side for all " + breakFrames + " break frames (" + standOnSwingSide + " on that side), never in the sweep: "
            + minReach.ToString("0.000") + " m from the hinge at the closest (sweep " + reachRelay.ToString("0.000") + ")");
        // Beside the latch, not just out of the sweep, and facing the lock it rips the leaf by. ClearOfSwing's stance (body
        // 0.35 m) is at least 1.365 m from the hinge and at most 0.83 m out, so at least 1.08 m along: past the latch jamb
        // (DoorWidth), even 4 cm short of it (MoveDirect). Its body stays inside the 1.2 m keep-clear strip, within a hand of
        // the sweep. It faces the lock from where it stands (BreakStance), so the worst facing is about 1; the face stance in
        // front of the opening (0.5 m along) faces the door at about 0.7 to the lock, which these bounds tell apart.
        var stripOut = ModuleUnits.WallHalf + ModuleUnits.DoorClearDepth - ModuleUnits.RelayRadius;
        Check(breakFrames > 0 && minAlong >= ModuleUnits.DoorWidth && maxOut <= stripOut && maxReach <= reachRelay + .15f,
            label + "it stands beside the latch: " + minAlong.ToString("0.000") + " m along at the least (past the latch jamb at " + ModuleUnits.DoorWidth.ToString("0.00")
            + "), " + maxOut.ToString("0.000") + " m out at the most (its body in the keep-clear strip: " + stripOut.ToString("0.00") + "), "
            + maxReach.ToString("0.000") + " m from the hinge at the farthest (sweep + 0.15: " + (reachRelay + .15f).ToString("0.000") + ")");
        Check(breakFrames > 0 && minLockFacing > .9f,
            label + "it faces the lock on its face for every break frame (worst " + minLockFacing.ToString("0.00") + ", at least 0.9)");
        var holderGap = new Vector2(holderAt.x - stance.x, holderAt.z - stance.z).magnitude;
        Check(breakFrames > 0 && holderGap >= ModuleUnits.PlayerRadius + ModuleUnits.RelayRadius,
            label + "the body holding the leaf stands clear of the Relay: " + holderGap.ToString("0.000") + " m apart (two bodies "
            + (ModuleUnits.PlayerRadius + ModuleUnits.RelayRadius).ToString("0.00") + ")");
        Check(door.broken && brokenEvents == 1 && brokenSwingSide && (brokenAt - stance).sqrMagnitude < .01f && brokenAngle >= 30f && brokenAngle < FrontRoomsMapWorld.DoorPassableDegrees,
            label + "DoorBrokenFrom " + brokenEvents + " time(s), from the " + (brokenEvents > 0 ? (brokenSwingSide ? "leaf's (swing)" : "far") : "-") + " side, at where it stood; the leaf ripped from "
            + brokenAngle.ToString("0.0") + "°");
        var tilt = Quaternion.Angle(door.leaf.transform.localRotation, Quaternion.identity);
        var restCell = world.CellOf(door.leaf.transform.position);
        Check(door.broken && restCell == a && FrontRoomsMapWorld.OnSwingSide(door, door.leaf.transform.position) && FrontRoomsMapWorld.OnSwingSide(door, stance)
            && Mathf.Abs(door.angle - FrontRoomsShotTimings.DoorBreak.BounceRestDeg) < .01f && maxAngle >= FrontRoomsMapWorld.DoorBreakThrowDegrees - 1f
            && Mathf.Abs(tilt - FrontRoomsShotTimings.DoorBreak.CrookedDeg) < .05f,
            label + "the leaf keeps its side: thrown to " + maxAngle.ToString("0.0") + "° into " + restCell + " (" + (restCell == a ? "the Relay's cell" : "not the Relay's cell " + a) + "), it rests there at " + door.angle.ToString("0.0") + "°, "
            + tilt.ToString("0.0") + "° crooked");
        Check(maxStep <= HingeStepLimit, label + "the hinge never turns more than " + HingeStepLimit.ToString("0") + "° in a frame (largest " + maxStep.ToString("0.0") + "°)");
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

    // ---------- The Relay never breaks a door on a wander or a search ----------

    static void RelayKeepsShutDoors(List<GameObject> roots)
    {
        // Out of the leaf's sweep, so the shutting leaf never stops on its body.
        var reachRelay = ModuleUnits.DoorWidth - ModuleUnits.DoorLeafGap * .5f + ModuleUnits.DoorLeafThickness * .5f + ModuleUnits.RelayRadius;
        // A world each, so a door broken on one leg (were the rule to fail) cannot spoil the next. With no way round
        // the shut door the leg is over; with one (open passages only, as a wander or a search walks) it walks round.
        foreach (var (leg, round, period) in new[] { (HunterState.Wander, false, -46f), (HunterState.Search, false, -48f), (HunterState.Wander, true, -49f) })
        {
            var label = "relay keeps a door shut across its " + (leg == HunterState.Wander ? "wander" : "search") + (round ? " (a way round)" : " (no way round)") + ": ";
            var world = World(roots, FrontRoomsLevelProfiles.Resolve(), period, "MAP INTERACTION TEST / keeps shut doors on a " + leg + (round ? ", a way round" : ""));
            world.BuildForCapture();
            var player = new GameObject("test player").transform;
            player.SetParent(world.transform, false);
            var body = player.gameObject.AddComponent<CapsuleCollider>();
            body.height = ModuleUnits.PlayerHeight;
            body.radius = ModuleUnits.PlayerRadius;
            body.center = Vector3.up * (ModuleUnits.PlayerHeight * .5f);
            // No way round: its near cell outside the carved rooms, so a search there looks round through the open door (in a
            // room it keeps to the room), and nothing joins the two sides within the planner's 64 cells but the door.
            // A way round: the map shuts its rooms with doors, so open passages alone seldom join a door's two sides. Other
            // doors that do, within 10 cells, are opened first, so the walk round keeps to open passages as a wander does.
            GridCoord a = default, b = default, c = default;
            var found = false;
            if (round)
            {
                var tried = new HashSet<long>();
                while (!found && FindDoor(world, out a, out b, out c, (d, near) => !tried.Contains(d.edge) && RouteRound(world, near, d.a == near ? d.b : d.a, null)))
                {
                    tried.Add(world.DoorBetween(a, b).edge);
                    var route = new List<(GridCoord, GridCoord)>();
                    RouteRound(world, a, b, route);
                    foreach (var (p, q) in route) world.TryOpenDoor(p, q);
                    for (var k = 0; k < 90; k++) world.TickDoorsForTools(Dt);
                    // A locked door on the route stays shut: the next door.
                    found = OpenWalk(world, a, b, 10) > 0;
                }
            }
            else found = FindDoor(world, out a, out b, out c, (d, near) => !InRoom(world, near) && OpenWalk(world, near, d.a == near ? d.b : d.a, 64) < 0);
            if (!found)
            {
                Check(false, label + "no closed door " + (round ? "with a way round it within 10 cells" : "outside a room with no way round it") + " and an open cell behind it near the spawn");
                continue;
            }
            var door = world.DoorBetween(a, b);
            FrontRoomsMapHunter hunter = null;
            var shutting = false;
            var seed = 0;
            GridCoord from = a, to = b;
            // Where a leg goes is the rng's pick: try seeds (odd, so each is its own rng) until one leaves its start cell
            // through the open door. A wander starts on either side in turn: one side may be a pocket with nowhere 6 cells off.
            for (var i = 0; i < 32 && !shutting; i++)
            {
                seed = 2 * i + 1;
                from = leg == HunterState.Wander && i % 2 == 1 ? b : a;
                to = from == a ? b : a;
                hunter = new FrontRoomsMapHunter(world, new FrontRoomsHunterTuning { sightRange = 0f }, body, null, seed);
                if (!door.open)
                {
                    world.TryOpenDoor(a, b);
                    for (var k = 0; k < 90; k++) world.TickDoorsForTools(Dt);
                }
                player.position = world.CellCenter(c);
                Physics.SyncTransforms();
                // Placed with nothing to search, it listens, then wanders; a noise where it stands makes it search round there.
                hunter.DebugPlace(world.CellCenter(from));
                if (leg == HunterState.Search) hunter.Noise(world.CellCenter(from), 1000f);
                for (var t = 0f; t < 12f && !shutting; t += Dt)
                {
                    hunter.Tick(Dt, player.position, player.position + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
                    world.TickDoorsForTools(Dt);
                    Physics.SyncTransforms();
                    var plan = Plan(door, hunter.Position);
                    shutting = hunter.State == leg && world.CellOf(hunter.Position) == from && hunter.DebugSteering.Contains("next " + to)
                        && (plan.y < -ModuleUnits.RelayRadius || Flat(hunter.Position - door.hinge.position) > reachRelay);
                    // A wander that set off another way is no use: the next seed.
                    if (!shutting && hunter.State == HunterState.Wander && leg == HunterState.Wander && world.CellOf(hunter.Position) != from) break;
                }
            }
            if (!shutting)
            {
                Check(false, label + "no " + leg + " leg went through the open door " + a + "→" + b + " in 32 tries");
                continue;
            }
            // The player shuts it in front of the Relay (from the stop side) before its next step.
            ShutAndLatch(world, door, world.Player, body);
            var latched = !door.open && door.angle == 0f && world.PassageBetween(a, b) == FrontRoomsMapWorld.Passage.ClosedDoor;
            var blows = 0;
            var states = new List<HunterState>();
            hunter.DoorBlow += _ => blows++;
            hunter.StateChanged += s => states.Add(s);
            var walkedRound = false;
            // The first state change after the shut (a wander's end), when and where.
            var changeAt = -1f;
            var changeCell = from;
            for (var t = 0f; t < (round ? 60f : 20f); t += Dt)
            {
                hunter.Tick(Dt, player.position, player.position + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
                world.TickDoorsForTools(Dt);
                Physics.SyncTransforms();
                if (world.CellOf(hunter.Position) == to) walkedRound = true;
                if (changeAt < 0f && states.Count > 0)
                {
                    changeAt = t;
                    changeCell = world.CellOf(hunter.Position);
                }
            }
            var kept = latched && blows == 0 && hunter.DoorsBroken == 0 && !door.broken && !door.open
                && !states.Contains(HunterState.Hunt) && !states.Contains(HunterState.BreakDoor);
            // A way round: the wander goes on round the shut door to its end (6 cells or more off), so it ends away from the door's
            // cell; ending the leg at the door, or stalling there, would end it in that cell. No way round: it never gets past the
            // door, and a wander is over at once, where it stands.
            var wanderOver = states.Count > 0 && states[0] == HunterState.Listen;
            var ended = round ? wanderOver && changeCell != from
                : !walkedRound && (leg != HunterState.Wander || (wanderOver && changeCell == from && changeAt < .5f));
            Check(kept && ended,
                label + "seed " + seed + ", the door " + from + "→" + to + " shut mid-leg stays shut and unbroken (" + hunter.DoorsBroken + " breaks, " + blows + " blows); then "
                + (states.Count > 0 ? string.Join(" → ", states) : "no state change")
                + (round ? "; the wander ended in " + changeCell + " after " + changeAt.ToString("0.00") + " s"
                    + (changeCell == from ? " (at the door)" : " (walked round" + (walkedRound ? ", through " + to : "") + ")")
                    : leg == HunterState.Wander ? "; the wander ended in " + changeCell + " after " + changeAt.ToString("0.00") + " s" + (walkedRound ? "; it got into " + to : "")
                    : walkedRound ? "; it got into " + to : "; it never got past the door"));
        }
    }

    /// <summary>Cells of walking from a to b over open passages only (no shut door: a wander's or a search's rule), up to maxDepth; -1 when b is out of reach.</summary>
    static int OpenWalk(FrontRoomsMapWorld world, GridCoord a, GridCoord b, int maxDepth)
    {
        var depth = new Dictionary<GridCoord, int> { [a] = 0 };
        var queue = new Queue<GridCoord>();
        queue.Enqueue(a);
        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            if (cell == b) return depth[cell];
            if (depth[cell] >= maxDepth) continue;
            foreach (var s in Steps)
            {
                var n = cell + s;
                if (depth.ContainsKey(n) || !world.IsBuilt(n) || world.PassageBetween(cell, n) != FrontRoomsMapWorld.Passage.Open) continue;
                depth[n] = depth[cell] + 1;
                queue.Enqueue(n);
            }
        }
        return -1;
    }

    /// <summary>
    /// A route of at most 10 cells from a to b that keeps off the edge between them, through open passages and other
    /// built shut doors (not glass or walls). The shut doors on it go into <paramref name="doors"/>. False when there is none.
    /// </summary>
    static bool RouteRound(FrontRoomsMapWorld world, GridCoord a, GridCoord b, List<(GridCoord, GridCoord)> doors)
    {
        var cameFrom = new Dictionary<GridCoord, GridCoord>();
        var depth = new Dictionary<GridCoord, int> { [a] = 0 };
        var queue = new Queue<GridCoord>();
        queue.Enqueue(a);
        while (queue.Count > 0 && !depth.ContainsKey(b))
        {
            var cell = queue.Dequeue();
            if (depth[cell] >= 10) continue;
            foreach (var s in Steps)
            {
                var n = cell + s;
                if (depth.ContainsKey(n) || !world.IsBuilt(n) || (cell == a && n == b)) continue;
                var p = world.PassageBetween(cell, n);
                if (p != FrontRoomsMapWorld.Passage.Open && (p != FrontRoomsMapWorld.Passage.ClosedDoor || world.DoorBetween(cell, n) == null)) continue;
                depth[n] = depth[cell] + 1;
                cameFrom[n] = cell;
                queue.Enqueue(n);
            }
        }
        if (!depth.ContainsKey(b)) return false;
        if (doors != null)
            for (var n = b; n != a; n = cameFrom[n])
                if (world.PassageBetween(cameFrom[n], n) == FrontRoomsMapWorld.Passage.ClosedDoor) doors.Add((cameFrom[n], n));
        return true;
    }

    /// <summary>True when a cell lies in an intact carved room (or module) of its chunk: a search there keeps to that room.</summary>
    static bool InRoom(FrontRoomsMapWorld world, GridCoord cell)
    {
        var chunk = world.Cache.Get(MapGrid.ChunkOf(cell));
        var o = chunk.Origin;
        for (var r = 0; r < chunk.rooms.Length; r++)
            if (chunk.RoomIntact(r) && chunk.rooms[r].Contains(cell.x - o.x, cell.y - o.y)) return true;
        return false;
    }

    // ---------- Losing sight: it follows only a door it saw the player go through ----------

    static void RelayLosesTrack(List<GameObject> roots)
    {
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), -47f, "MAP INTERACTION TEST / loses track");
        world.BuildForCapture();
        var player = new GameObject("test player").transform;
        player.SetParent(world.transform, false);
        var body = player.gameObject.AddComponent<CapsuleCollider>();
        body.height = ModuleUnits.PlayerHeight;
        body.radius = ModuleUnits.PlayerRadius;
        body.center = Vector3.up * (ModuleUnits.PlayerHeight * .5f);
        // Open cells straight on from the door on both sides (c behind a, e beyond b), so from c it sees through the open door into e.
        if (!FindDoor(world, out var a, out var b, out var c, (d, near) =>
            {
                var far = d.a == near ? d.b : d.a;
                var beyond = new GridCoord(2 * far.x - near.x, 2 * far.y - near.y);
                return world.IsBuilt(beyond) && world.PassageBetween(far, beyond) == FrontRoomsMapWorld.Passage.Open;
            }))
        {
            Check(false, "lose track: no closed door with open cells straight on from it on both sides near the spawn");
            return;
        }
        var e = new GridCoord(2 * b.x - a.x, 2 * b.y - a.y);
        var door = world.DoorBetween(a, b);
        // A second door well out of its sight (over 24 m off), for the player to go through unseen.
        FrontRoomsMapWorld.Door away = null;
        foreach (var d in Doors(world).Values)
        {
            var candidate = world.DoorBetween(d.a, d.b);
            if (candidate == null || candidate == door || !world.IsBuilt(d.a) || !world.IsBuilt(d.b)) continue;
            if (Flat(world.CellCenter(d.a) - world.CellCenter(a)) <= 24f || Flat(world.CellCenter(d.b) - world.CellCenter(a)) <= 24f) continue;
            away = candidate;
            break;
        }
        if (away == null) { Check(false, "lose track: no second door over 24 m from " + a); return; }
        world.TryOpenDoor(a, b);
        for (var k = 0; k < 90; k++) world.TickDoorsForTools(Dt);
        if (world.PassageBetween(a, b) != FrontRoomsMapWorld.Passage.Open) { Check(false, "lose track: the door " + a + "→" + b + " did not open"); return; }

        // Points on the line through the middle of the opening, square to the wall: negative in a and c, positive in b and e.
        var mouth = world.CrossingPoint(a, b);
        var across = world.CellCenter(b) - world.CellCenter(a);
        across.y = 0f;
        across.Normalize();
        Vector3 Out(float metres)
        {
            var p = mouth + across * metres;
            p.y = world.CellCenter(a).y;
            return p;
        }
        var gone = world.CellCenter(away.b);

        // The player at a point for some frames: the frames on which the Relay saw them.
        int Stand(FrontRoomsMapHunter hunter, Vector3 feet, int frames)
        {
            var seen = 0;
            player.position = feet;
            Physics.SyncTransforms();
            for (var k = 0; k < frames; k++)
            {
                hunter.Tick(Dt, feet, feet + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
                world.TickDoorsForTools(Dt);
                Physics.SyncTransforms();
                if (hunter.SeesPlayer) seen++;
            }
            return seen;
        }

        // The player gone: where the Relay begins its search once it has lost track (null if it never does).
        Vector3? SearchAfter(FrontRoomsMapHunter hunter)
        {
            player.position = gone;
            Physics.SyncTransforms();
            var hunting = false;
            for (var t = 0f; t < 15f; t += Dt)
            {
                hunter.Tick(Dt, gone, gone + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
                world.TickDoorsForTools(Dt);
                Physics.SyncTransforms();
                if (hunter.State == HunterState.Hunt) hunting = true;
                else if (hunting && hunter.State == HunterState.Search) return hunter.Position;
            }
            return null;
        }

        var tuning = new FrontRoomsHunterTuning { sightRange = 12f, lostSightSeconds = 1.5f };
        // Unseen: in sight in a, then out of it and through the far door. It goes where it last saw them, not through that door.
        var unseen = new FrontRoomsMapHunter(world, tuning, body, null, 3);
        unseen.DebugPlace(Out(-4.5f));
        var seenInA = Stand(unseen, Out(-1f), 6);
        var seenAway = Stand(unseen, world.CellCenter(away.a), 6) + Stand(unseen, world.CellCenter(away.b), 6);
        var at = SearchAfter(unseen);
        Check(seenInA == 6 && seenAway == 0 && at.HasValue && world.CellOf(at.Value) == a && Flat(at.Value - Out(-1f)) < 1f,
            "lose track: the player went through " + away.a + "→" + away.b + " out of its sight (seen " + seenInA + "/6 frames in " + a + ", " + seenAway + "/12 there): it searches from "
            + (at.HasValue ? world.CellOf(at.Value).ToString() : "nowhere") + ", where it last saw them (expected " + a + ")");

        // Seen (the control): in a, through the door into b and on into e, all in sight 0.1 s each, then gone. It follows through the door it saw them take.
        var watched = new FrontRoomsMapHunter(world, tuning, body, null, 3);
        watched.DebugPlace(Out(-4.5f));
        var seenAll = Stand(watched, Out(-1f), 6) + Stand(watched, Out(1f), 6) + Stand(watched, Out(4.5f), 6);
        var at2 = SearchAfter(watched);
        Check(seenAll == 18 && at2.HasValue && world.CellOf(at2.Value) == b,
            "lose track (control): it saw the player go through " + a + "→" + b + " and on into " + e + " (" + seenAll + "/18 frames in sight) 0.2 s before it lost them: it follows through the door and searches from "
            + (at2.HasValue ? world.CellOf(at2.Value).ToString() : "nowhere") + " (expected " + b + ")");

        // Seen too long before (the control stretched): through the door in sight, 1.2 s in sight in b, on into e, then gone. Its
        // last sight comes 1.28 s after the crossing, past the 1.0 s, so it goes where it last saw them. A slow chase, so it
        // stays well short of the player while they stand in b.
        var stale = new FrontRoomsMapHunter(world, new FrontRoomsHunterTuning { sightRange = 12f, lostSightSeconds = 1.5f, chaseSpeed = 1f }, body, null, 3);
        stale.DebugPlace(Out(-4.5f));
        var seenStale = Stand(stale, Out(-1f), 6) + Stand(stale, Out(1f), 72) + Stand(stale, Out(4.5f), 6);
        var at3 = SearchAfter(stale);
        Check(seenStale == 84 && at3.HasValue && world.CellOf(at3.Value) == e && Flat(at3.Value - Out(4.5f)) < 1f,
            "lose track (stale): it saw the player go through " + a + "→" + b + ", then 1.2 s in " + b + " and on into " + e + " (" + seenStale + "/84 frames in sight), 1.28 s after the crossing: it searches from "
            + (at3.HasValue ? world.CellOf(at3.Value).ToString() : "nowhere") + ", where it last saw them (expected " + e + ")");

        // A gap in its sight: seen in a, out of sight for a moment, then seen in b and on into e. It saw them on each side of the door
        // but never watched the crossing (no two ticks in a row), so it goes where it last saw them, not through the door.
        var gap = new FrontRoomsMapHunter(world, tuning, body, null, 3);
        gap.DebugPlace(Out(-4.5f));
        var seenBefore = Stand(gap, Out(-1f), 6);
        var seenInGap = Stand(gap, world.CellCenter(away.a), 3);
        var seenAfter = Stand(gap, Out(1f), 3) + Stand(gap, Out(4.5f), 6);
        var at4 = SearchAfter(gap);
        Check(seenBefore == 6 && seenInGap == 0 && seenAfter == 9 && at4.HasValue && world.CellOf(at4.Value) == e && Flat(at4.Value - Out(4.5f)) < 1f,
            "lose track (gap): seen in " + a + " (" + seenBefore + "/6), unseen for 3 frames (" + seenInGap + "/3), then seen in " + b + " and " + e + " (" + seenAfter + "/9): it never watched the crossing, so it searches from "
            + (at4.HasValue ? world.CellOf(at4.Value).ToString() : "nowhere") + ", where it last saw them (expected " + e + ")");
    }

    // ---------- Lamp overrides (interface v1) ----------

    // ---------- The Relay's warning stages (v2 §9.1) ----------

    /// <summary>
    /// §9.1: a stage starts on the first sample in its band; its minimum time (4 s / 3 s) counts from entry and only holds
    /// it in; it ends once W has stayed past its exit band (36 / 24 m) for 1 s; off the map is stage 0 at once.
    /// </summary>
    static void WarnStages(List<GameObject> roots)
    {
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), -49f, "MAP INTERACTION TEST / warn stages");
        world.BuildForCapture();
        var tuning = new FrontRoomsHunterTuning { sightRange = 0f };
        var events = new List<int>();
        FrontRoomsMapHunter Fresh()
        {
            var h = new FrontRoomsMapHunter(world, tuning, null, null, 7);
            events.Clear();
            h.WarnStageChanged += events.Add;
            return h;
        }
        // Feed W for some seconds (whole frames), the way the tick takes it.
        void Feed(FrontRoomsMapHunter h, float w, float seconds)
        {
            for (var k = Mathf.RoundToInt(seconds / Dt); k > 0; k--) h.DebugWarnSample(w, Dt);
        }
        string Log(FrontRoomsMapHunter h) => "stage " + h.WarnStage + ", events [" + string.Join(", ", events) + "], run " + h.WarnRunSeconds.ToString("F2") + " s, stage-2 run " + h.WarnStage2RunSeconds.ToString("F2") + " s";

        // 40 → 15 in one sample: stage 2 on that sample, stage 1 included.
        var h1 = Fresh();
        h1.DebugWarnSample(40f, Dt);
        var at40 = h1.WarnStage;
        h1.DebugWarnSample(15f, Dt);
        Check(at40 == 0 && h1.WarnStage == 2 && events.Count == 1 && events[0] == 2, "warn stages: W 40 → 15 in one sample is stage 2 on that sample (" + Log(h1) + ")");
        // Past both exit bands from 0.5 s, inside the 3 s minimum: it holds; the minimum ends at 3 s with W past 36 for 2.5 s, so 2 → 0 there.
        Feed(h1, 15f, .5f);
        Feed(h1, 37f, 2.4f);
        var inDwell = h1.WarnStage;
        Feed(h1, 37f, .2f);
        Check(inDwell == 2 && h1.WarnStage == 0 && events.Count == 2 && events[1] == 0 && h1.WarnRunSeconds == 0f,
            "warn stages: W 37 inside stage 2's 3 s does not drop it (stage " + inDwell + " at 2.9 s); at 3 s, past 36 m for over 1 s, 2 → 0 (" + Log(h1) + ")");

        // After the minimum: 25 m (past 24, inside 36) for 1 s drops 2 → 1, not before; the stage ≥ 1 run keeps going and stage 1's 4 s starts again.
        var h2 = Fresh();
        Feed(h2, 15f, 3.5f);
        Feed(h2, 25f, .9f);
        var at09 = h2.WarnStage;
        Feed(h2, 25f, .15f);
        var run = h2.WarnRunSeconds;
        Check(at09 == 2 && h2.WarnStage == 1 && events.Count == 2 && events[1] == 1 && run > 4.4f && h2.WarnStage2RunSeconds == 0f,
            "warn stages: after the 3 s, W 25 for 0.9 s keeps stage 2 (" + at09 + "), for 1 s drops it to 1; the stage ≥ 1 run goes on (" + Log(h2) + ")");
        Feed(h2, 37f, 3.8f);
        var inDwell1 = h2.WarnStage;
        Feed(h2, 37f, .3f);
        Check(inDwell1 == 1 && h2.WarnStage == 0 && events.Count == 3 && events[2] == 0 && h2.WarnRunSeconds == 0f,
            "warn stages: a 2 → 1 drop restarts stage 1's 4 s (still " + inDwell1 + " at 3.8 s past 36 m), then 1 → 0 (" + Log(h2) + ")");

        // A dip back inside the band before the second is up starts the confirm again.
        var h3 = Fresh();
        Feed(h3, 15f, 3.5f);
        Feed(h3, 25f, .9f);
        Feed(h3, 23f, Dt);
        Feed(h3, 25f, .9f);
        var jitter = h3.WarnStage;
        Feed(h3, 25f, .15f);
        Check(jitter == 2 && h3.WarnStage == 1 && events.Count == 2, "warn stages: 0.9 s past 24 m, one sample at 23, 0.9 s past again keeps stage 2; the full second drops it (" + Log(h3) + ")");

        // 2 → 0 needs W past 36 m for the same second; past 24 for 1.1 s but past 36 only for 0.6 s is 2 → 1.
        var h4 = Fresh();
        Feed(h4, 15f, 3.5f);
        Feed(h4, 30f, .5f);
        Feed(h4, 40f, .6f);
        Check(h4.WarnStage == 1 && events.Count == 2 && events[1] == 1, "warn stages: past 24 m for 1.1 s but past 36 m for 0.6 s drops 2 → 1, not to 0 (" + Log(h4) + ")");

        // Going up is never held: 1 → 2 the sample W reaches 18, inside stage 1's 4 s.
        var h5 = Fresh();
        Feed(h5, 25f, .5f);
        h5.DebugWarnSample(17f, Dt);
        Check(h5.WarnStage == 2 && events.Count == 2 && events[0] == 1 && events[1] == 2 && h5.WarnRunSeconds > .45f && h5.WarnStage2RunSeconds == 0f,
            "warn stages: 1 → 2 on the sample W reaches 17 m, 0.5 s into stage 1 (" + Log(h5) + ")");
        // Off the map (infinite W) is stage 0 at once, inside a minimum time.
        h5.DebugWarnSample(float.PositiveInfinity, Dt);
        Check(h5.WarnStage == 0 && events.Count == 3 && events[2] == 0 && h5.WarnRunSeconds == 0f, "warn stages: off the map inside stage 2's 3 s is stage 0 on that sample (" + Log(h5) + ")");

        // Through the tick: released next to the player is stage 2 on its first tick; its chunk dropped, stage 0 on the next, inside the 3 s.
        var mid = world.CellOf(world.SpawnWorldPosition);
        GridCoord? playerCell = null, relayCell = null;
        for (var y = mid.y - 12; y <= mid.y + 12 && !playerCell.HasValue; y++)
        for (var x = mid.x - 12; x <= mid.x + 12 && !playerCell.HasValue; x++)
        {
            var p = new GridCoord(x, y);
            var r = new GridCoord(x + 2, y);
            // Different chunks, so dropping the Relay's leaves the player's field standing.
            if (world.IsBuilt(p) && world.IsBuilt(r) && MapGrid.ChunkOf(p) != MapGrid.ChunkOf(r)) { playerCell = p; relayCell = r; }
        }
        if (!playerCell.HasValue) { Check(false, "warn stages (tick): no built cell pair 2 cells apart across a chunk border near the spawn"); return; }
        var feet = world.CellCenter(playerCell.Value);
        // DebugPlace clears the stage and its timers: the next tick enters the new spot's band on its first sample.
        var h7 = Fresh();
        var beside = feet + Vector3.right;
        h7.DebugPlace(beside);
        h7.Tick(Dt, feet, feet + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
        var placedStage = h7.WarnStage;
        h7.DebugPlace(beside);
        Check(placedStage == 2 && h7.WarnStage == 0 && h7.WarnRunSeconds == 0f && events.Count == 2, "warn stages (tick): DebugPlace clears the stage and its timers (" + Log(h7) + ")");
        var h6 = Fresh();
        h6.DebugPlace(world.CellCenter(relayCell.Value));
        h6.Tick(Dt, feet, feet + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
        var first = h6.WarnStage;
        var w6 = h6.WarningDistance;
        for (var t = 0f; t < .5f; t += Dt) h6.Tick(Dt, feet, feet + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
        var before = h6.WarnStage;
        var relayAt = world.CellOf(h6.Position);
        world.DropChunkForTools(MapGrid.ChunkOf(relayAt));
        var dropped = !world.IsBuilt(relayAt);
        h6.Tick(Dt, feet, feet + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
        Check(first == 2 && before == 2 && dropped && h6.WarnStage == 0 && events.Count == 2 && events[0] == 2 && events[1] == 0,
            "warn stages (tick): placed " + relayCell.Value + " next to the player at " + playerCell.Value + " (W " + w6.ToString("F1") + " m) is stage " + first + " on the first tick, "
            + before + " 0.5 s later; its chunk dropped (" + (dropped ? "unbuilt" : "STILL BUILT") + "), stage 0 on the next tick (" + Log(h6) + ")");
    }

    // ---------- W1: the warning's lamp bursts (RELAY §9.2; C10, bridged onto Relay v1) ----------

    static void WarnLamps(List<GameObject> roots)
    {
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), -53f, "MAP INTERACTION TEST / warn lamps");
        world.BuildForCapture();
        var flashing = FrontRoomsSettings.ReduceFlashing;
        try
        {
            if (FrontRoomsSettings.ReduceFlashing) FrontRoomsSettings.SetReduceFlashing(false);
            WarnLampChecks(world, "warn lamps: ");
        }
        finally
        {
            if (FrontRoomsSettings.ReduceFlashing != flashing) FrontRoomsSettings.SetReduceFlashing(flashing);
        }
    }

    static void WarnLampChecks(FrontRoomsMapWorld world, string label)
    {
        var mid = world.CellOf(world.SpawnWorldPosition);
        var far = world.CellCenter(new GridCoord(mid.x + 40, mid.y + 40));
        var probe = new FrontRoomsWarnLamps(world, 1);
        var player = default(GridCoord);
        var found = false;
        for (var y = mid.y - 10; y <= mid.y + 10 && !found; y++)
        for (var x = mid.x - 10; x <= mid.x + 10 && !found; x++)
        {
            var c = new GridCoord(x, y);
            if (!world.IsBuilt(c)) continue;
            probe.RoomForTools(world.CellCenter(c), far);
            if (probe.RoomSet.Count >= 4 && probe.LiveLamps.Count >= 2) { player = c; found = true; }
        }
        if (!found) { Check(false, label + "no room with 2 live lamps near the spawn"); return; }
        var p = world.CellCenter(player);
        var live = new List<GridCoord>(probe.LiveLamps);
        var modes = live.Select(world.LampModeOf).ToList();

        // The Relay inside the room: no lamp within 2 cells of it is ever live.
        probe.RoomForTools(p, world.CellCenter(live[0]));
        var relayCell = live[0];
        Check(probe.LiveLamps.All(c => Mathf.Max(Mathf.Abs(c.x - relayCell.x), Mathf.Abs(c.y - relayCell.y)) > 2),
            label + "with the Relay in the room, no live lamp within 2 cells of it (" + probe.LiveLamps.Count + " of " + live.Count + " left)");

        var warn = new FrontRoomsWarnLamps(world, 12345);
        var clock = 0f;
        var times = new List<float>();
        var cells = new List<GridCoord>();
        world.LampDipped += (c, at) => { times.Add(clock); cells.Add(c); };
        var bursts = 0;
        var burstLamps = -1;
        warn.WarnBurst += (centre, n) => { bursts++; burstLamps = n; };
        var minRatio = 1f;
        void Run(float seconds, int stage, bool chase)
        {
            for (var s = 0f; s < seconds - 1e-4f; s += Dt)
            {
                clock += Dt;
                warn.Tick(Dt, stage, chase, p, far);
                world.TickFixturesForTools(Dt);
                foreach (var c in live)
                    if (world.LampBaseLevel(c) > .2f) minRatio = Mathf.Min(minRatio, LampRatio(world, c));
            }
        }

        // Stage 0: nothing.
        Run(10f, 0, false);
        Check(bursts == 0 && times.Count == 0 && world.LampOverrideCount == 0, label + "stage 0 for 10 s: no burst, no dip, no override (" + bursts + ", " + times.Count + ")");

        // Stage 1: the first burst 0.3-1.0 s in; then 14 s of bursts.
        var stage1 = clock;
        Run(1.2f, 1, false);
        var first = times.Count > 0 ? times[0] - stage1 : -1f;
        Check(bursts == 1 && first >= FrontRoomsWarnLamps.FirstBurstMin - .01f && first <= FrontRoomsWarnLamps.FirstBurstMax + 2f * FrontRoomsWarnLamps.Jitter + Dt && burstLamps == live.Count,
            label + "stage 1: the first burst " + first.ToString("0.00") + " s in, on " + burstLamps + " of " + live.Count + " live lamps");
        Run(14f, 1, false);
        // Dips: starts closer than 0.2 s are one dip; dips closer than 1 s are one burst.
        var dips = new List<List<int>>();
        for (var i = 0; i < times.Count; i++)
        {
            if (dips.Count == 0 || times[i] - times[dips[dips.Count - 1][0]] > .2f) dips.Add(new List<int>());
            dips[dips.Count - 1].Add(i);
        }
        var together = dips.All(d => d.Select(i => cells[i]).Distinct().Count() == d.Count && d.Count == live.Count
            && times[d[d.Count - 1]] - times[d[0]] <= 2f * FrontRoomsWarnLamps.Jitter + Dt);
        var burstDips = new List<int>();
        var burstStarts = new List<float>();
        for (var k = 0; k < dips.Count; k++)
        {
            var t = times[dips[k][0]];
            if (k == 0 || t - times[dips[k - 1][0]] > 1f) { burstDips.Add(0); burstStarts.Add(t); }
            burstDips[burstDips.Count - 1]++;
        }
        var spacingOk = true;
        for (var k = 1; k < dips.Count; k++)
        {
            var gap = times[dips[k][0]] - times[dips[k - 1][0]];
            if (gap <= 1f && Mathf.Abs(gap - FrontRoomsWarnLamps.DipSpacing) > 2f * FrontRoomsWarnLamps.Jitter + Dt) spacingOk = false;
        }
        var minGap = float.MaxValue;
        for (var k = 1; k < burstStarts.Count; k++) minGap = Mathf.Min(minGap, burstStarts[k] - burstStarts[k - 1]);
        Check(burstDips.Count >= 2 && burstDips.All(n => n == 2 || n == 3) && together && spacingOk && minGap >= FrontRoomsWarnLamps.Gap1Min,
            label + "stage 1: " + burstDips.Count + " bursts of " + string.Join("/", burstDips) + " dips, every live lamp within 80 ms (" + together + "), 0.55 s apart (" + spacingOk + "), bursts at least " + minGap.ToString("0.0") + " s apart");
        Check(minRatio >= FrontRoomsWarnLamps.DipDepthMin - .03f && minRatio <= FrontRoomsWarnLamps.DipDepthMax + .03f,
            label + "each dip takes a lamp to " + minRatio.ToString("0.00") + " of its level (0.55-0.70)");
        var maxPerSecond = 0;
        for (var i = 0; i < times.Count; i++)
            maxPerSecond = Mathf.Max(maxPerSecond, Enumerable.Range(0, times.Count).Count(j => cells[j] == cells[i] && times[j] >= times[i] && times[j] < times[i] + 1f));
        Check(maxPerSecond <= 2, label + "at most " + maxPerSecond + " dip starts a second on any lamp (photosafety: under 3)");

        // Stage 2: once a burst has ended 2.5 s ago or more, entering stage 2 bursts at once, with 2 dips.
        Run(.05f, 1, false);
        for (var guard = 0; guard < 2000 && (warn.Bursting || clock - LastStart(times) < 1.1f + FrontRoomsWarnLamps.Stage2Rearm); guard++) Run(Dt, 1, false);
        var before2 = bursts;
        var at2 = clock;
        var startsBefore = times.Count;
        Run(1.4f, 2, false);
        var stage2Dips = times.Skip(startsBefore).Select(t => Mathf.Round((t - times[startsBefore]) / FrontRoomsWarnLamps.DipSpacing)).Distinct().Count();
        Check(bursts == before2 + 1 && times.Count > startsBefore && times[startsBefore] - at2 <= 2f * FrontRoomsWarnLamps.Jitter + 2f * Dt && stage2Dips == 2,
            label + "entering stage 2 (" + (at2 - LastStartBefore(times, startsBefore)).ToString("0.0") + " s after the last dip) bursts at once with " + stage2Dips + " dips");

        // A chase starts no burst (the one playing finishes).
        var beforeChase = bursts;
        Run(1.5f, 2, true);
        var afterFinish = bursts;
        Run(10f, 2, true);
        Check(bursts == afterFinish && afterFinish - beforeChase <= 1, label + "a chase starts no burst in 10 s (" + (bursts - afterFinish) + ")");

        // Back to stage 0: every dip gone within its release, the held lamps back to their own modes.
        Run(.6f, 0, false);
        var restored = live.Select(world.LampModeOf).SequenceEqual(modes);
        Check(world.LampOverrideCount == 0 && !warn.Bursting && restored, label + "back at stage 0: " + world.LampOverrideCount + " overrides left, own modes back (" + restored + ")");
        var quiet = times.Count;
        Run(8f, 0, false);
        Check(times.Count == quiet, label + "stage 0 again for 8 s: no dip (" + (times.Count - quiet) + ")");

        // Reduce flashing: a burst is one sag per lamp.
        FrontRoomsSettings.SetReduceFlashing(true);
        var reduced = new FrontRoomsWarnLamps(world, 777);
        var start = times.Count;
        for (var s = 0f; s < 2.6f; s += Dt) { clock += Dt; reduced.Tick(Dt, 1, false, p, far); world.TickFixturesForTools(Dt); }
        var perLamp = times.Skip(start).Count();
        FrontRoomsSettings.SetReduceFlashing(false);
        reduced.Reset();
        Check(reduced.BurstCount == 1 && perLamp == live.Count, label + "Reduce flashing: one burst is one sag per lamp (" + perLamp + " starts for " + live.Count + " lamps)");
        for (var s = 0f; s < 2f; s += Dt) world.TickFixturesForTools(Dt);

        // Stuttering lamps never take part (the lamp check's one-frame snap at the first dip and at stage 0): a lamp of
        // the room turned Failing, and one turned Stutter, are never live, never dipped, and keep their mode through
        // bursts in both stages and the return to stage 0; no other lamp of the room changes mode either.
        var failing = live[0];
        var stutter = live.Count >= 3 ? live[1] : failing;
        world.SetLampMode(failing, ModuleLamp.Failing);
        if (stutter != failing) world.SetLampMode(stutter, ModuleLamp.Stutter);
        for (var s = 0f; s < .5f; s += Dt) world.TickFixturesForTools(Dt);
        probe.RoomForTools(p, far);
        var roomCells = new List<GridCoord>(probe.RoomSet);
        var roomModes = roomCells.Select(world.LampModeOf).ToList();
        var leftOut = !probe.LiveLamps.Contains(failing) && !probe.LiveLamps.Contains(stutter);
        var flicker = new FrontRoomsWarnLamps(world, 4242);
        var flickerStart = times.Count;
        var flickerBursts = 0;
        flicker.WarnBurst += (centre, n) => flickerBursts++;
        var modesKept = true;
        void RunFlicker(float seconds, int stage)
        {
            for (var s = 0f; s < seconds - 1e-4f; s += Dt)
            {
                clock += Dt;
                flicker.Tick(Dt, stage, false, p, far);
                world.TickFixturesForTools(Dt);
                if (!roomCells.Select(world.LampModeOf).SequenceEqual(roomModes)) modesKept = false;
            }
        }
        RunFlicker(9f, 1);
        RunFlicker(5f, 2);
        RunFlicker(3f, 0);
        var dippedStuttering = cells.Skip(flickerStart).Count(c => c == failing || c == stutter);
        Check(leftOut && flickerBursts >= 2 && dippedStuttering == 0 && modesKept && world.LampModeOf(failing) == ModuleLamp.Failing && world.LampModeOf(stutter) == (stutter == failing ? ModuleLamp.Failing : ModuleLamp.Stutter),
            label + "a Failing and a Stutter lamp of the room are left out (" + leftOut + "): " + flickerBursts + " bursts, " + dippedStuttering + " dips on them, every room lamp kept its mode (" + modesKept + ")");
        flicker.Reset();
        world.SetLampMode(failing, ModuleLamp.Auto);
        if (stutter != failing) world.SetLampMode(stutter, ModuleLamp.Auto);
        for (var s = 0f; s < 2f; s += Dt) world.TickFixturesForTools(Dt);

        // M2 (RELAY 9.5): from stage 1 the room's Dead lamps strike no blink until stage 0 (the map's SetAmbientHold);
        // a Dead lamp outside the room strikes as ever; the hold moves with the player and Reset lets it go.
        probe.RoomForTools(p, far);
        var roomSet = new List<GridCoord>(probe.RoomSet);
        var deadIn = roomSet.Where(c => world.LampLevel(c) != FrontRoomsMapWorld.NoLamp).Take(3).ToList();
        GridCoord? other = null;
        for (var y = mid.y - 10; y <= mid.y + 10 && !other.HasValue; y++)
        for (var x = mid.x - 10; x <= mid.x + 10 && !other.HasValue; x++)
        {
            var c = new GridCoord(x, y);
            if (world.LampLevel(c) == FrontRoomsMapWorld.NoLamp || roomSet.Any(r => Mathf.Max(Mathf.Abs(r.x - c.x), Mathf.Abs(r.y - c.y)) < 4)) continue;
            probe.RoomForTools(world.CellCenter(c), far);
            if (probe.RoomSet.Contains(c) && !probe.RoomSet.Any(deadIn.Contains)) other = c;
        }
        if (deadIn.Count == 0 || !other.HasValue) { Check(false, label + "M2: no lamp in the room (" + deadIn.Count + ") or no lamp in another room near the spawn"); return; }
        var outside = other.Value;
        foreach (var c in deadIn) world.SetLampMode(c, ModuleLamp.Dead);
        world.SetLampMode(outside, ModuleLamp.Dead);
        var holder = new FrontRoomsWarnLamps(world, 31337);
        int litIn = 0, litOutside = 0;
        void RunHold(float seconds, int stage, Vector3 at)
        {
            for (var s = 0f; s < seconds - 1e-4f; s += Dt)
            {
                clock += Dt;
                holder.Tick(Dt, stage, false, at, far);
                world.TickFixturesForTools(Dt);
                foreach (var c in deadIn) if (world.LampBaseLevel(c) > 0f) litIn++;
                if (world.LampBaseLevel(outside) > 0f) litOutside++;
            }
        }
        RunHold(45f, 1, p);
        RunHold(15f, 2, p);
        var heldAll = deadIn.All(world.AmbientHeld) && holder.HeldCells.Count == roomSet.Count && !world.AmbientHeld(outside);
        Check(litIn == 0 && litOutside > 0 && heldAll,
            label + "M2: through 60 s of warning (stages 1 and 2) the room's " + deadIn.Count + " Dead lamps never strike (" + litIn + " lit frames), held (" + heldAll + "); a Dead lamp in another room does (" + litOutside + ")");
        RunHold(Dt, 0, p);
        var released = holder.HeldCells.Count == 0 && world.AmbientHoldCount == 0;
        litIn = 0;
        RunHold(45f, 0, p);
        Check(released && litIn > 0, label + "M2: at stage 0 the hold lets go (" + released + ") and the room's Dead lamps strike again within 45 s (" + litIn + " lit frames)");
        RunHold(.5f, 1, p);
        var heldHere = deadIn.All(world.AmbientHeld);
        RunHold(Dt, 1, world.CellCenter(outside));
        var moved = heldHere && !deadIn.Any(world.AmbientHeld) && world.AmbientHeld(outside);
        holder.Reset();
        Check(moved && world.AmbientHoldCount == 0 && holder.HeldCells.Count == 0,
            label + "M2: the hold moves with the player to another room (" + moved + "); Reset lets go of it (" + world.AmbientHoldCount + " held)");
        foreach (var c in deadIn) world.SetLampMode(c, ModuleLamp.Auto);
        world.SetLampMode(outside, ModuleLamp.Auto);
        for (var s = 0f; s < 2f; s += Dt) world.TickFixturesForTools(Dt);
    }

    static float LastStart(List<float> times) => times.Count > 0 ? times[times.Count - 1] : float.NegativeInfinity;
    static float LastStartBefore(List<float> times, int count) => count > 0 ? times[count - 1] : float.NegativeInfinity;

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
        Check(cracks.Count == 2 && cracks[0] == 1 && cracks[1] == 2 && floor >= .5f && floor < .5f + 2f * Dt && resumed.impact == record.impact && resumed.hold == floor,
            "glass: cracks 1 then 2, once each (" + string.Join(",", cracks) + "); a release keeps the progress (" + floor.ToString("0.000") + ", kept in the record); a resumed strike keeps the impact");

        // A rebuild: the window goes and comes back cracked, from the progress it had.
        var chunk = MapGrid.ChunkOf(window.a);
        world.RebuildChunk(chunk);
        if (!released.Contains(window)) { chunk = MapGrid.ChunkOf(window.b); world.RebuildChunk(chunk); }
        var again = built.LastOrDefault(x => x.window.edge == window.edge);
        Check(released.Contains(window) && again.window != null && again.window != window && again.record.stage == 2 && again.window.stage == 2
              && Mathf.Abs(again.window.hold - p * FrontRoomsShotTimings.GlassBreak.HoldSeconds) < 1e-4f && again.record.impact == record.impact,
            "glass: a rebuild raises WindowReleased, then WindowBuilt with the record (stage " + again.record.stage + "), resuming from the kept progress (" + again.window.hold.ToString("0.000") + ")");

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

        // A knock below Crack1 is kept too (Red 2026-10-22): 0.2 s, let go, struck again elsewhere: 0.2 kept, the first impact stays.
        var third = built.Select(x => x.window).FirstOrDefault(x => x.pane != null && x.stage == 0 && x.hold == 0f && x != window && x != other && world.IsBuilt(x.a) && world.IsBuilt(x.b));
        if (third != null)
        {
            var thirdPane = third.pane.GetComponent<Collider>();
            world.BeginGlassHold(thirdPane, third.root.TransformPoint(new Vector3(.3f, 1.4f, 0f)), third.root.forward);
            world.Hold(thirdPane, .2f, out _);
            world.ReleaseHold(thirdPane);
            world.TryGetGlassBreak(third.edge, out var knocked);
            world.BeginGlassHold(thirdPane, third.root.TransformPoint(new Vector3(-.3f, .9f, 0f)), third.root.forward);
            world.TryGetGlassBreak(third.edge, out var reknocked);
            Check(Mathf.Abs(third.hold - .2f) < 1e-5f && third.stage == 0 && Mathf.Abs(knocked.hold - .2f) < 1e-5f && reknocked.impact == knocked.impact,
                "glass: a 0.2 s knock is kept (" + third.hold.ToString("0.000") + ") and a second strike keeps its impact " + reknocked.impact.ToString("F3"));
            world.ReleaseHold(thirdPane);
        }
        else Check(false, "glass: no third intact window for the kept-knock check");

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

    /// <summary>
    /// Visual Q16-1: KeepClearAtStart keeps the furnishing out of a world-XZ rect (the placard's floor) while a start area
    /// stands, and SetStartArea empties the list. The start area sits far off, so only the rect changes the room.
    /// </summary>
    static void StartKeepClear(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles)
    {
        var cs = MapGrid.CellSize;
        var far = new RectInt(200, 200, 3, 3);
        var farDoor = new GridCoord(201, 203);
        var m = Room(6, 6, ZoneTheme.Office);
        m.fill = ModuleFill.Office;
        // Low dressing colliders in a world, as world-XZ rects.
        List<Rect> Low(FrontRoomsMapWorld w) => w.GetComponentsInChildren<Transform>(true).Where(t => t.name == "office dressing")
            .SelectMany(t => t.GetComponentsInChildren<Collider>(true)).Select(c => c.bounds).Where(b => b.min.y <= ModuleUnits.RelayHeight)
            .Select(b => Rect.MinMaxRect(b.min.x, b.min.z, b.max.x, b.max.z)).ToList();
        // A probe with a far start area and no rect: where the kit puts something, well inside the room.
        var probe = ModuleWorld(roots, profiles, m, 1, 1, -50f, "MAP INTERACTION TEST / start keep-clear probe", w => w.SetStartArea(far, farDoor));
        var origin = probe.transform.TransformPoint(new Vector3(cs, 0f, cs));
        var hit = Low(probe).Select(r => r.center - new Vector2(origin.x, origin.z))
            .FirstOrDefault(c => c.x > 2f && c.y > 2f && c.x < m.WidthMetres - 2f && c.y < m.DepthMetres - 2f);
        if (hit == default) { Check(false, "start keep-clear: the Office kit put nothing inside the probe room"); return; }
        Rect Around(FrontRoomsMapWorld w)
        {
            var o = w.transform.TransformPoint(new Vector3(cs, 0f, cs));
            return new Rect(o.x + hit.x - .8f, o.z + hit.y - .8f, 1.6f, 1.6f);
        }
        bool Inside(Rect a, Rect b) => a.xMin < b.xMax - .02f && b.xMin + .02f < a.xMax && a.yMin < b.yMax - .02f && b.yMin + .02f < a.yMax;

        // The same room with a 1.6 m rect round that spot: nothing low in it, and the room is still furnished.
        Rect kept = default;
        var world = ModuleWorld(roots, profiles, m, 1, 1, -51f, "MAP INTERACTION TEST / start keep-clear", w =>
        {
            w.SetStartArea(far, farDoor);
            kept = Around(w);
            w.KeepClearAtStart(kept);
        });
        var low = Low(world);
        var inRect = low.Count(r => Inside(r, kept));
        Check(low.Count > 0 && inRect == 0, "start keep-clear: KeepClearAtStart keeps the Office fill out of a 1.6 m rect where it put furniture without it ("
            + inRect + " of " + low.Count + " low pieces in it)");

        // SetStartArea after the rect empties the list: the furniture comes back.
        Rect dropped = default;
        var reset = ModuleWorld(roots, profiles, m, 1, 1, -52f, "MAP INTERACTION TEST / start keep-clear reset", w =>
        {
            w.SetStartArea(far, farDoor);
            dropped = Around(w);
            w.KeepClearAtStart(dropped);
            w.SetStartArea(far, farDoor);
        });
        var back = Low(reset).Count(r => Inside(r, dropped));
        Check(back > 0, "start keep-clear: SetStartArea empties the list (" + back + " low pieces back in the rect)");
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
    static FrontRoomsMapWorld ModuleWorld(List<GameObject> roots, List<FrontRoomsLevelProfile> profiles, RoomModuleData m, int x0, int y0, float period, string name, Action<FrontRoomsMapWorld> beforeBuild = null)
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
        beforeBuild?.Invoke(world);
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
