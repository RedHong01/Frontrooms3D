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
/// - C22, the exit area (X, the EXIT's stairwell cell, plan v2 D1, D3, D5-D7, D9): X chosen by
///   FrontRoomsExitPlanner and set before the build; EffectiveEdge (N to X the EXIT door, X's other edges
///   walls); X is no map (not built, every passage a wall, no path field reaches it, N's zone, no lamp) and
///   nothing of its own is built; only X's chunk builds differently, with or without the title's start area;
///   the exit lamp group on both tick paths; the EXIT door swings into X and is never locked by the key
///   rule; the Relay never goes into X, and X is a safe zone (Red 2026-10-22: a player in X is never seen,
///   so never caught, not even in the doorway; a chase ends the moment they step in, and the Relay walks to its
///   stand at the door, stops facing it (StoppedAtSafeDoor) and walks 4 m off (L11, SafeDoorBeat); a noise inside
///   X is never heard, open or shut; the door's own noise on its line is heard from N alone; ExitOpeningCost is
///   the field's door rule); SealExit (no prompt or E from the call on, locked at the latch, kept through a rebuild, at
///   once for a shut door); X's chunk never shifts; NextFloor leaves nothing behind, the same seed builds
///   the same floor byte for byte at the same root, and a new seed a new floor there at the carried tier.
/// - C22, the stairwell in X (FrontRoomsStairwell, plan v2 D8): made at the map's CellCenter under the game's
///   root, not the map; every piece but the floor slab at least WallHalf + 0.01 m inside X's lines and clear of
///   the map's walls (no overlap, a measured gap on all four sides); the floor collider always on, the ceiling
///   at 2.40 m; the EXIT leaf swings into X through the south doorway without touching it, and a body walks
///   from N into X; SetVisible and the lamp (one point light inside X's lines, its level for the blackout);
///   the static south leaf from Seal on; through NextFloor (it stays, X the new start area) JoinNextFloor
///   closes X's open north line flush with the map's walls (no ray from the door cell finds the void); the
///   north door opens away from its opener (out of X for a player inside, into X from beyond), stands still
///   under DoorsPaused, shuts and locks for good; X's two north corners, where the end wall meets the map's
///   walls, are closed on every open side (rays across each post square meet a face flush with the walls) on
///   floors with a free wall end and an L there, and nothing of the stairwell overlaps the map.
/// - C22, Relay Park (plan v2 D12): a Relay breaking a door parks Dormant at once (the door's mark lifted, route,
///   search, noise, sight and warning cleared, the body hidden, no door stops on it, run counts kept), hears
///   nothing, and is released again only after a full release delay; across NextFloor its path field and the
///   warning lamps' room are the new floor's, and it comes back on the new floor's map.
/// - C23 step 1, the EXIT sign, the exit lamp hold and the front rules (Red 2026-10-22, with the visual chat):
///   rules 3 (N Standard) and 4 (no glass on the EXIT door's sightline, none a revisit shift could glaze) hold
///   on every EXIT planned over a scan of floor seeds, read with the test's own loops; for each sign kind (the
///   flat sign, and the end-on blade where a corridor runs past the door) one LIT-locked sign over the header on
///   N's side (bottom 2.18 m since round 2, centred, its back on the wall face, facing N, deaf to SetMains and a LOOSE deal),
///   the kit the corridor rule asks for, touching nothing, seen from N, none on X's side (the stairwell hides
///   that place), no window built on the sightline; every lamp within 2 cells of N Steady and still in the exit
///   group, the ring beyond its own; the sign rebuilt and dropped with X's chunk, gone with NextFloor; the hold
///   never in the start area.
/// - C23 round 2 (10_directions.md 1.2): the sign on the steel frame's exit_sign_p anchor (bottom 2.18 m, its back
///   0.08 m from the wall's centre line, read from the kit), for both kinds, just over the head trim, casting no
///   shadow, EmissionGain 2.0; the EXIT door's frame a renderer of its own that casts no shadow while an ordinary
///   door's frame stays in its block's trim mesh and casts; the crossbar on the EXIT leaf's push face (N's side,
///   1.0 m up, 0.1 m in from the latch edge, no colliders) following the leaf open and jolted, rebuilt and
///   dropped with X's chunk, gone with NextFloor; the hold taken up by the start-area cells ClearStartArea gives
///   back (the title strip of seed 20656951's floor 1, as the game makes it).
/// - C23 round 3 (agreed with the visual chat; 10_directions.md 7), the EXIT door for the dressing and its lock:
///   ExitDoorBuilt goes out once per build of the EXIT edge (none with no exit area), with the chunk registered,
///   handing over the door, its hinge, its unscaled leaf rig (the crossbar "EXIT crossbar" under it) and the "EXIT
///   frame" object (one renderer, no shadow); what a handler hangs on the leaf follows the swing and a jolt, what it
///   hangs on the frame stays; it fires again, with fresh transforms and the old dressing gone, after RebuildChunk,
///   a drop and the stream's return, and on floors 2 and 3 after NextFloor; a throwing handler is logged and the
///   build and the next handler go on. ExitLocked is false on floors 1-3; SetExitLocked(true) raises ExitLockChanged
///   once (a second call nothing), and the locked EXIT is a locked door no key opens (the locked prompt, Kind Locked
///   even with N's zone key, E rattles with the rig following, TryOpenDoor false); a rebuild keeps it, unlocking
///   opens it, the seal stays apart, and NextFloor (or no exit area) resets it with no event.
/// - A26 step 1, the floor-1 start group's data (notes/floor1_start_group_plan.md 4.2; the stand-in template, six seeds
///   at both door parities and one in the 4 x 3 footprint): every start plans with A1 on; the carve the cache holds
///   (FrontRoomsMapCarve, SetCarve) is step 0's overlay in every edge slot of every chunk it touches, with its rooms,
///   columns and keys; BorderEdge returns each forced border edge and EdgeNoGenerate agrees on every slot; no room on a
///   protected cell, no column on X's corners; no zone key in the station or X; the station's lamps as the template's,
///   C1 Steady, C2..N and X Auto; c and c' joined to the map with the strip standing and given back; C22's rules, N-X
///   Open, G1 and the walk H + k on the carved cache; a re-Get identical; IsPinned, SkinOf, ArchOverride and
///   MayBecomeWindow (never inside a pinned chunk); ClearCarve gives every chunk back raw.
/// - A26 step 2, the planner and the station module (plan 4.3, 4.9): the marker kinds appended; the profile asset holds no
///   station and its pool none; the stand-in station is valid and From reads it back as the stand-in template, one with no
///   entrance marker is no station; every break of the station contract is refused; Plan on seed 20656951 at both door
///   parities sets the carve, X 4 cells north of E1 at chunk-local (5, 2) and (5, 6) (pins), FromStartGroup's EXIT (band
///   8, the flat sign); FromStartGroup refuses a raw cache; Plan again and on a fresh cache gives the same hash; no template
///   or a refused one leaves no plan and no carve; 4 x 3 (walk 7); A1 through Plan; the validator's floor 1 with the station.
/// - A26 step 3, the station in the built world (plan 4.4, 4.7, 9; the pin seed at both door parities, set up as the game
///   will, the stand-in station with props, test materials for its skin): FloorHasStation false before SetStation, true
///   after, false after ClearStation and NextFloor; the queries (InStation, the walk D..N, the anchors, StationPathDistance),
///   FixtureKeepOut, no Relay entry in the station, StationShellBuilt; (d) ReadyAround waits for the props, which the map
///   places a few a frame, the axis bookcase and the crate in the door's sweep left out; (a) the shell is the data across
///   every template edge, E1 1.7 m and E2 1.2 m measured, centred; (b) the station's material on every face of the room,
///   the liner on the south side but the stream door, the zone's paper outside, the jambs of E1 and E2 the only seams;
///   (c) 80 rays at the facade corners; (e) c and c' keep revision 0 through a 31 s drop while a control chunk shifts, the
///   set hidden while neither stands; (g) the riser block, floor to ceiling in the north-east cell, is architecture and a
///   blind Relay walks round it, never into it; (f) after ClearStartArea D's south side is a wall in the station's material
///   with the static entrance on it and the liner gone; ClearStation takes the carve and the set away.
/// - A26 step 5, M7: floor 1's first send with a station (plan 4.10; the pin seed at both door parities, set up as the game
///   will, the stand-in station): the gate (StationSendDue) at path distance 0, 1 and 2 through E1 and through E2, with and
///   without the key, the key needed or not: Dormant / Dormant / sent (never sent without the key while it is needed), and
///   sent at 150 s wherever the player is; every send outside the station, its ring, the strip and the band round the walk
///   to the EXIT, rung 1 within 8-10 walking cells on the far side, the first leg a Hunt to the take point with the key and
///   Listen without; rungs forced empty walk the ladder down to today's Arrive; 300 forced releases (the ladder, today's
///   Arrive and the leash relay) never land in the station or its ring; the grace (no chase beyond 6 m before the first
///   lamp dip); M6's seam (TakeStationKey raises StationKeyTaken once, with the KeySpot on the floor; HuntStation sends a
///   released Relay there, round the riser block); ClearStation forgets the key.
/// - M3 / M6 / R3, the floor key (notes/a26_group_m3_plan.md 6.1; the pin seed at both door parities, set up as the game
///   will, the EXIT locked before Begin): the one-line prompts are the key HUD's; with no station nothing changes (no
///   floor key rule, the keys flag rules the inner doors, zone keys, the zone keys' prompt); with one the floor key lies at
///   the KeySpot with its trigger, no zone key is built, a rebuild or a drop brings it back once; the locked EXIT reads
///   LOCKED . NEEDS STAIR KEY with the floor key's identity and rattles; Floor 1 has no locked inner doors (A13), and with
///   that lifted the floor key, not a zone key, opens one from either side; E on the floor key raises KeyPickupStarted,
///   StationKeyTaken (at the KeySpot on the floor) and KeyTaken in that order, FloorHasStation ([Preserve]) and
///   StationKeyIsTaken read by reflection as FrontRoomsStationPresence reads them (R3 is the visual line's: true, false ->
///   true, true), the wayfinding moves; M6's hunt and M7's gate with the key; the EXIT then reads E . UNLOCK,
///   the key turn's commit lifts the lock before DoorUnlocked (with the floor key) and the leaf swings into X (with and
///   without a key-turn owner); ClearStation and NextFloor drop the rule and the key, floor 2 keeps today's zone keys.
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
            ExitArea(roots);
            Stairwell(roots);
            RelayPark(roots);
            ExitSign(roots);
            ExitDoor(roots);
            StartGroupData(roots);
            StartGroupPlanner(roots);
            StartGroupWorld(roots);
            StartGroupRelay(roots);
            StartGroupKeys(roots);
            StartGroupRoute(roots);
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
        Check(minRatio >= FrontRoomsWarnLamps.FirstDepthMin - .03f && minRatio <= FrontRoomsWarnLamps.FirstDepthMax + .03f,
            label + "the deepest dip takes a lamp to " + minRatio.ToString("0.00") + " of its level (M3: a burst's first dip 0.55-0.62, the later ones 0.65-0.75)");
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

        // M1 (lamp check 10_lamp_look_check.md): a lamp of the room turned Failing and one turned Stutter are held from
        // stage 1, crossfaded (Failing at Dim, Stutter at Steady): live, dipped with the rest, the look formula while
        // held, their mode (and every room lamp's) never changed. At stage 0 they stay held 2-6 s, cross back over
        // 1.2 s and are let go; a lamp left behind (the player out of the room) lets go the same way; Reset at once.
        var failing = live[0];
        var stutter = live.Count >= 3 ? live[1] : failing;
        world.SetLampMode(failing, ModuleLamp.Failing);
        if (stutter != failing) world.SetLampMode(stutter, ModuleLamp.Stutter);
        for (var s = 0f; s < 12f && world.LampHoldCount > 0; s += Dt) world.TickFixturesForTools(Dt);
        for (var s = 0f; s < .5f; s += Dt) world.TickFixturesForTools(Dt);
        probe.RoomForTools(p, far);
        var roomCells = new List<GridCoord>(probe.RoomSet);
        var roomModes = roomCells.Select(world.LampModeOf).ToList();
        var joined = probe.LiveLamps.Contains(failing) && probe.LiveLamps.Contains(stutter);
        var watched = stutter != failing ? new[] { failing, stutter } : new[] { failing };
        var flicker = new FrontRoomsWarnLamps(world, 4242);
        var flickerStart = times.Count;
        var flickerBursts = 0;
        var flickerLamps = 0;
        flicker.WarnBurst += (centre, n) => { flickerBursts++; flickerLamps = Mathf.Max(flickerLamps, n); };
        var modesKept = true;
        int lookFrames = 0, lookWrong = 0;
        var firstOut = new float[watched.Length];
        var rampFrames = new int[watched.Length];
        for (var k = 0; k < watched.Length; k++) firstOut[k] = -1f;
        var stage0At = 0f;
        void RunFlicker(float seconds, int stage, Vector3 at)
        {
            for (var s = 0f; s < seconds - 1e-4f; s += Dt)
            {
                clock += Dt;
                flicker.Tick(Dt, stage, false, at, far);
                world.TickFixturesForTools(Dt);
                if (!roomCells.Select(world.LampModeOf).SequenceEqual(roomModes)) modesKept = false;
                for (var k = 0; k < watched.Length; k++)
                {
                    var c = watched[k];
                    if (!world.LampHoldForTools(c, out var w, out _, out var letting, out var delay)) continue;
                    if (letting && delay <= 0f && w < 1f)
                    {
                        if (firstOut[k] < 0f) firstOut[k] = clock - stage0At;
                        if (w > 0f) rampFrames[k]++;
                    }
                    if (w < 1f || (letting && delay <= 0f)) continue;
                    world.LampClockForTools(c, out var lc, out var ph);
                    var t = lc + ph;
                    var look = world.LampModeOf(c) == ModuleLamp.Failing ? .42f + .03f * Mathf.Sin(t * 90f) : .98f + .02f * Mathf.Sin(t * 110f);
                    lookFrames++;
                    if (BitConverter.SingleToInt32Bits(world.LampBaseLevel(c)) != BitConverter.SingleToInt32Bits(look)) lookWrong++;
                }
            }
        }
        RunFlicker(9f, 1, p);
        RunFlicker(5f, 2, p);
        var dippedFlickering = cells.Skip(flickerStart).Count(c => watched.Contains(c));
        var heldBoth = watched.All(c => flicker.HeldFlickering.Contains(c) && world.LampHoldWeight(c) >= 1f);
        Check(joined && heldBoth && flickerBursts >= 2 && flickerLamps == live.Count && dippedFlickering >= flickerBursts * watched.Length && lookFrames > 0 && lookWrong == 0 && modesKept,
            label + "M1: a Failing and a Stutter lamp of the room are held (" + heldBoth + ") and live (" + joined + "): " + flickerBursts + " bursts on " + flickerLamps + " of " + live.Count
            + " lamps, " + dippedFlickering + " dips on them; held, the Dim / Steady formula bit for bit (" + lookWrong + " of " + lookFrames + " frames off); every room lamp kept its mode (" + modesKept + ")");

        // Stage 0: still held 2 s or more, then each crosses back over 1.2 s and is let go, its own mode again.
        stage0At = clock + Dt;
        RunFlicker(1.9f, 0, p);
        var stillHeld = watched.All(c => world.LampHoldWeight(c) >= 1f) && flicker.HeldFlickering.Count == 0;
        RunFlicker(9.5f, 0, p);
        var letGo = !watched.Any(world.LampHeld);
        var outOk = true;
        for (var k = 0; k < watched.Length; k++)
            if (firstOut[k] < FrontRoomsWarnLamps.RestoreMin - Dt || firstOut[k] > FrontRoomsWarnLamps.RestoreMax + 3f * Dt || rampFrames[k] < 59) outOk = false;
        Check(stillHeld && letGo && outOk && modesKept && world.LampModeOf(failing) == ModuleLamp.Failing && (stutter == failing || world.LampModeOf(stutter) == ModuleLamp.Stutter),
            label + "M1: at stage 0 they stay held (" + stillHeld + "), cross back " + string.Join(" / ", firstOut.Select(f => f.ToString("0.00"))) + " s after it (2-6) over "
            + string.Join(" / ", rampFrames) + " frames (1.2 s = 72), and are let go (" + letGo + "), their modes kept");

        // A lamp left behind (the player's cell far off, not this room) lets go 2-6 s later; Reset lets go at once.
        var mover = new FrontRoomsWarnLamps(world, 4343);
        for (var s = 0f; s < 1.5f; s += Dt) { clock += Dt; mover.Tick(Dt, 1, false, p, far); world.TickFixturesForTools(Dt); }
        var heldThen = watched.All(c => world.LampHoldWeight(c) >= 1f);
        clock += Dt;
        mover.Tick(Dt, 1, false, far, far);
        world.TickFixturesForTools(Dt);
        var leftBehind = watched.All(c => world.LampHoldForTools(c, out var w, out _, out var letting, out var delay) && letting && w >= 1f
            && delay >= FrontRoomsWarnLamps.RestoreMin - 2f * Dt && delay <= FrontRoomsWarnLamps.RestoreMax);
        mover.Reset();
        for (var s = 0f; s < 12f && watched.Any(world.LampHeld); s += Dt) world.TickFixturesForTools(Dt);
        var resetter = new FrontRoomsWarnLamps(world, 4444);
        for (var s = 0f; s < 1.5f; s += Dt) { clock += Dt; resetter.Tick(Dt, 1, false, p, far); world.TickFixturesForTools(Dt); }
        var heldAgain = watched.All(c => world.LampHoldWeight(c) >= 1f);
        resetter.Reset();
        world.TickFixturesForTools(Dt);
        var atOnce = watched.All(c => world.LampHoldForTools(c, out var w, out _, out var letting, out var delay) && letting && delay <= 0f && w < 1f && w > .95f);
        for (var s = 0f; s < 6f && watched.Any(world.LampHeld); s += Dt) world.TickFixturesForTools(Dt);
        var goneAfterReset = !watched.Any(world.LampHeld) && resetter.HeldFlickering.Count == 0;
        Check(heldThen && leftBehind && heldAgain && atOnce && goneAfterReset,
            label + "M1: held again (" + heldThen + ", " + heldAgain + "); a lamp left behind lets go 2-6 s later (" + leftBehind + "); Reset lets go at once (" + atOnce
            + "), every hold gone within 6 s (" + goneAfterReset + ")");
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

    // ---------- C22: the exit area (X, the EXIT's stairwell cell), its door's seal, the next floor in place ----------

    /// <summary>
    /// C22 step 2 (plan v2 c22_stairwell_plan_v2.md, D1, D3, D5-D7, D9). X is chosen as the game will choose it
    /// (FrontRoomsExitPlanner.ChooseExit on the map's own cache, floor 1's band) and set before the build
    /// (NextFloor with the same seed, SetExitArea, Begin: the order of every floor). One map runs the whole
    /// section, as one map runs a whole run; a second one checks the exit area beside the title's start area.
    /// </summary>
    static void ExitArea(List<GameObject> roots)
    {
        const string label = "exit area: ";
        var profile = FrontRoomsLevelProfiles.Resolve();
        var world = World(roots, profile, -55f, "MAP INTERACTION TEST / exit area");
        // Every window the map has announced and not yet released: what the glass kit holds on to.
        var announced = new HashSet<FrontRoomsMapWorld.Window>();
        var released = 0;
        world.WindowBuilt += (w, r) => announced.Add(w);
        world.WindowReleased += w => { if (announced.Remove(w)) released++; };
        world.BuildForCapture();
        var eye = world.Player;
        var spawn = eye.position;
        var seed = world.Seed;
        var mapRoot = world.transform.position;
        var plain = ChunkFingerprints(world);
        var plainCount = world.BuiltChunkCount;

        // X from data, as the game will choose it on floor 1 (here the walk starts at the spawn cell, with no start area).
        var start = world.CellOf(spawn);
        var plan = FrontRoomsExitPlanner.ChooseExit(world.Cache, new CellRect(0, 0, 0, 0), start, seed, FrontRoomsExitPlanner.Floor1BandMin, FrontRoomsExitPlanner.Floor1BandMax);
        if (!plan.found) { Check(false, label + "the planner found no EXIT from " + start + " (seed " + seed + ")"); return; }
        var x = plan.exitCell;
        var n = plan.frontCell;
        var chunkX = MapGrid.ChunkOf(x);
        var west = new GridCoord(x.x - 1, x.y);
        var east = new GridCoord(x.x + 1, x.y);
        var north = new GridCoord(x.x, x.y + 1);

        // The same floor again with X set before the build, as every floor is made.
        world.NextFloor(seed, 1, new RectInt(0, 0, 0, 0), default(GridCoord));
        var costNoExit = world.ExitOpeningCost();
        world.SetExitArea(x);
        eye.position = spawn;
        world.Begin(eye, true);
        Physics.SyncTransforms();

        // 1. Only X's chunk builds differently.
        var withExit = ChunkFingerprints(world);
        var xName = "Chunk " + chunkX + " ";
        var same = 0;
        var xChanged = false;
        var changed = new List<string>();
        foreach (var pair in plain)
        {
            var match = withExit.TryGetValue(pair.Key, out var h) && h == pair.Value;
            if (pair.Key.StartsWith(xName)) xChanged = !match;
            else if (match) same++;
            else changed.Add(pair.Key);
        }
        Check(withExit.Count == plain.Count && changed.Count == 0 && same == plain.Count - 1 && xChanged,
            label + "X " + x + " (N " + n + ", " + plan.distance + " walking cells, dead end " + plan.deadEnd + "): of " + plain.Count + " chunks only X's " + chunkX
            + " builds differently (" + same + " the same, X's changed " + xChanged + ", others changed: " + string.Join(", ", changed.Take(4)) + ")");

        // 2. EffectiveEdge: the exit rule on X's four edges, the generated edge everywhere else.
        var below = new GridCoord(n.x, n.y - 1);
        var beside = new GridCoord(west.x, west.y + 1);
        Check(world.EffectiveEdge(n, x) == EdgeKind.Door && world.EffectiveEdge(x, n) == EdgeKind.Door && world.EffectiveEdge(x, west) == EdgeKind.Wall
              && world.EffectiveEdge(east, x) == EdgeKind.Wall && world.EffectiveEdge(x, north) == EdgeKind.Wall
              && world.EffectiveEdge(below, n) == world.Cache.Edge(below, n) && world.EffectiveEdge(west, beside) == world.Cache.Edge(west, beside),
            label + "EffectiveEdge: N-X the EXIT door (generated " + world.Cache.Edge(n, x) + "), X's west, east and north edges walls (generated "
            + world.Cache.Edge(west, x) + ", " + world.Cache.Edge(x, east) + ", " + world.Cache.Edge(x, north) + "), the edges round them as generated");

        // 3. X is no map: to IsBuilt, PassageBetween, the path fields (nav and hearing), zones and lamps.
        var field = new FrontRoomsMapPathField(400f);
        var fieldsReached = 0;
        var fieldsBuilt = 0;
        foreach (var s in Steps)
        {
            var c = x + s;
            if (!world.IsBuilt(c)) continue;
            fieldsBuilt++;
            field.Build(world, c);
            if (!float.IsPositiveInfinity(field.DistanceTo(x))) fieldsReached++;
        }
        var walled = Steps.All(s => world.PassageBetween(x, x + s) == FrontRoomsMapWorld.Passage.Wall && world.PassageBetween(x + s, x) == FrontRoomsMapWorld.Passage.Wall
            && float.IsPositiveInfinity(world.OpeningCostBetween(x + s, x)));
        Check(world.HasExitArea && world.ExitCell == x && world.ExitFrontCell == n && world.InExitArea(x) && world.IsReserved(x) && !world.IsBuilt(x) && world.IsChunkBuiltAt(x)
              && world.IsBuilt(n) && !world.InExitArea(n) && walled && fieldsBuilt > 0 && fieldsReached == 0 && world.ZoneOf(x).id == world.ZoneOf(n).id
              && world.LampModeOf(x) == ModuleLamp.Off && world.LampLevel(x) == FrontRoomsMapWorld.NoLamp,
            label + "X is no map: not built (its chunk is), every passage a wall, no path field from its " + fieldsBuilt + " built neighbours reaches it ("
            + fieldsReached + " do), its zone is N's, it has no lamp");

        // 4. Nothing of X's own: no floor or ceiling, no lamp, prop or furniture, no outlet face or fixture on its side.
        var inside = world.CellCenter(x);
        var noFloor = !Physics.Raycast(inside + Vector3.up, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore);
        var noCeiling = !Physics.Raycast(inside + Vector3.up, Vector3.up, 10f, ~0, QueryTriggerInteraction.Ignore);
        var floorAtN = Physics.Raycast(world.CellCenter(n) + Vector3.up, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore);
        var things = new List<string>();
        foreach (var c in world.GetComponentsInChildren<Collider>(true))
            if (!c.isTrigger && !world.IsArchitecture(c) && InCell(world, x, c.bounds.center, .05f)) things.Add(c.name);
        foreach (var l in world.GetComponentsInChildren<Light>(true))
            if (InCell(world, x, l.transform.position, 0f)) things.Add(l.name);
        int sets = 0, faces = 0, fixtures = 0;
        var chunkRoot = ChunkRoot(world, chunkX);
        if (chunkRoot != null)
            foreach (var set in chunkRoot.GetComponentsInChildren<FrontRoomsWallFixtureSet>(true))
            {
                sets++;
                foreach (var f in set.faces) if (f.cell == x) faces++;
                foreach (var f in set.fixtures) if (InCell(world, x, chunkRoot.TransformPoint(f.position), .02f)) fixtures++;
            }
        Check(chunkRoot != null && noFloor && noCeiling && floorAtN && things.Count == 0 && faces == 0 && fixtures == 0,
            label + "nothing of X's own: no floor (" + noFloor + "; N has one: " + floorAtN + "), no ceiling (" + noCeiling + "), no lamp, prop or furniture ("
            + string.Join(", ", things.Take(4)) + "), no outlet face (" + faces + ") or fixture (" + fixtures + ") on its side in " + sets + " outlet sets");

        // 5. The exit lamp group (D9): ExitLamps holds exactly the lamps within their light's range of X, on both tick paths.
        eye.position = world.CellCenter(n) + Vector3.up * .05f;
        var nearOnly = FrontRoomsMapWorld.TickFixturesNearOnly;
        try
        {
            foreach (var webgl in new[] { false, true })
            {
                FrontRoomsMapWorld.TickFixturesNearOnly = webgl;
                var lamps = world.GetComponentsInChildren<Light>(true);
                // A zero step recomputes every lamp without moving its flicker clock, so the four reads compare.
                world.ExitLamps = 1f;
                world.TickFixturesForTools(0f);
                var full = LampSnapshot(lamps);
                world.ExitLamps = 0f;
                world.TickFixturesForTools(0f);
                var dark = LampSnapshot(lamps);
                var darkMismatches = world.CheckLampStatesForTools(out _, out _, null);
                world.ExitLamps = .5f;
                world.TickFixturesForTools(0f);
                var half = LampSnapshot(lamps);
                var halfMismatches = world.CheckLampStatesForTools(out _, out _, null);
                world.ExitLamps = 1f;
                world.TickFixturesForTools(0f);
                var back = LampSnapshot(lamps);
                int group = 0, groupLit = 0, groupDark = 0, groupHalved = 0, others = 0, othersSame = 0, restored = 0;
                foreach (var l in lamps)
                {
                    if (back[l] == full[l]) restored++;
                    if (InExitGroup(world, x, l))
                    {
                        group++;
                        if (dark[l] < 0f) groupDark++;
                        if (full[l] > 0f)
                        {
                            groupLit++;
                            if (Mathf.Abs(half[l] - full[l] * .5f) < 1e-4f) groupHalved++;
                        }
                    }
                    else
                    {
                        others++;
                        if (dark[l] == full[l] && half[l] == full[l]) othersSame++;
                    }
                }
                Check(group > 0 && groupLit > 0 && groupDark == group && groupHalved == groupLit && othersSame == others && restored == lamps.Length && darkMismatches == 0 && halfMismatches == 0,
                    label + (webgl ? "the WebGL tick" : "the desktop tick") + ": ExitLamps 0 holds all " + group + " lamps within range of X dark (" + groupDark + "), 0.5 halves the "
                    + groupLit + " lit ones (" + groupHalved + "), the other " + others + " never change (" + othersSame + "), 1 restores all (" + restored + "/" + lamps.Length
                    + "); the lamp formula agrees (" + darkMismatches + ", " + halfMismatches + " mismatches)");
            }
        }
        finally
        {
            FrontRoomsMapWorld.TickFixturesNearOnly = nearOnly;
            world.ExitLamps = 1f;
        }

        // 6. The EXIT door: it swings into X, no zone key locks it, and it reads as a plain door until the seal.
        var door = world.ExitDoor;
        if (door == null) { Check(false, label + "no EXIT door built on " + n + "-" + x); return; }
        var keys = world.DoorsNeedKeysForTools;
        world.DoorsNeedKeysForTools = true;
        var others2 = Doors(world).Values.Select(d => world.DoorBetween(d.a, d.b)).Where(d => d != null && d != door).ToList();
        var othersLocked = others2.Count > 0 && others2.All(world.LockedByRule);
        var exitFree = !world.LockedByRule(door) && !world.KeyFor(door, eye.position).HasValue;
        var openPrompt = world.Describe(door.leaf, out var hold);
        var openKind = world.Kind(door.leaf, eye.position, out _, out _);
        world.DoorsNeedKeysForTools = keys;
        Check(world.IsExitDoor(door) && door.a == n && door.b == x && !door.open && door.swing == 1f && FrontRoomsMapWorld.OnSwingSide(door, world.CellCenter(x))
              && !FrontRoomsMapWorld.OnSwingSide(door, world.CellCenter(n)) && exitFree && othersLocked && openPrompt != null && openPrompt.Contains("OPEN DOOR") && !hold
              && openKind == FrontRoomsMapWorld.AimKind.Open,
            label + "the EXIT door swings into X (swing " + door.swing + "); with doors needing keys it alone is not locked by the rule (" + others2.Count
            + " others are), reads OPEN DOOR, kind " + openKind);
        // ExitOpeningCost (what a noise in X pays at the door) follows the field's door rule as the leaf moves.
        var costShut = world.ExitOpeningCost();
        var costAjar = -1f;
        var opened = world.TryOpenDoor(n, x);
        for (var k = 0; k < 240; k++)
        {
            world.TickDoorsForTools(Dt);
            if (door.open && door.angle > 1f && door.angle < FrontRoomsMapWorld.DoorPassableDegrees - 1f) costAjar = world.ExitOpeningCost();
        }
        Physics.SyncTransforms();
        var leafInX = InCell(world, x, door.leaf.bounds.center, 0f);
        var shutPrompt = world.Describe(door.leaf, out _);
        Check(opened && door.open && door.angle > 90f && leafInX && world.PassageBetween(n, x) == FrontRoomsMapWorld.Passage.Wall && shutPrompt != null
              && shutPrompt.Contains("SHUT DOOR") && world.Kind(door.leaf, eye.position, out _, out _) == FrontRoomsMapWorld.AimKind.Shut,
            label + "opened from N it swings into X (" + door.angle.ToString("0") + " deg, the leaf in X: " + leafInX + "), reads SHUT DOOR, and is still no way for the Relay ("
            + world.PassageBetween(n, x) + ")");
        var costOpen = world.ExitOpeningCost();
        Check(float.IsPositiveInfinity(costNoExit) && costShut == FrontRoomsMapPathField.ShutOpening && costAjar == FrontRoomsMapPathField.AjarOpening && costOpen == 0f
              && float.IsPositiveInfinity(world.OpeningCostBetween(n, x)),
            label + "ExitOpeningCost is the field's door rule on the EXIT door: shut " + costShut + ", ajar " + costAjar + ", open past " + FrontRoomsMapWorld.DoorPassableDegrees
            + " deg " + costOpen + ", no exit area " + costNoExit + " (OpeningCostBetween N-X stays " + world.OpeningCostBetween(n, x) + ": X is no map)");

        // 7. The Relay never goes into X. A player body outside the map's own objects.
        var playerRoot = new GameObject("MAP INTERACTION TEST / exit area player") { hideFlags = HideFlags.DontSave };
        roots.Add(playerRoot);
        var body = playerRoot.AddComponent<CapsuleCollider>();
        body.height = ModuleUnits.PlayerHeight;
        body.radius = ModuleUnits.PlayerRadius;
        body.center = Vector3.up * (ModuleUnits.PlayerHeight * .5f);
        var into = world.CellCenter(x) - world.CellCenter(n);
        into.y = 0f;
        into.Normalize();
        var mouth = world.CrossingPoint(n, x);
        mouth.y = world.CellCenter(n).y;
        // Where it stands to face a door (the hunter's BreakFaceDistance, 0.45 m), on N's side.
        var stand = mouth - into * .45f;

        // The safe zone (Red, 2026-10-22: the stairwell is a safe zone, the monster cannot get in). A chase: from N's centre it
        // sees the player in N, 0.6 m short of the open EXIT door (beyond its catch distance), and chases; the player
        // steps well inside X. The chase ends that tick, it never sees or catches them in X, never steps into X, and
        // goes where it last saw them, on N's side.
        var outside = mouth - into * .6f;
        var chaser = new FrontRoomsMapHunter(world, new FrontRoomsHunterTuning { sightRange = 12f }, body, null, 11);
        var caught = false;
        chaser.Caught += () => caught = true;
        chaser.DebugPlace(world.CellCenter(n));
        int seen = 0, inX = 0;
        var chased = false;
        playerRoot.transform.position = outside;
        Physics.SyncTransforms();
        for (var t = 0f; t < 1f && !chased; t += Dt)
        {
            chaser.Tick(Dt, outside, outside + Vector3.up * ModuleUnits.PlayerEye, into);
            world.TickDoorsForTools(Dt);
            Physics.SyncTransforms();
            if (chaser.State == HunterState.Chase) chased = true;
        }
        playerRoot.transform.position = inside;
        Physics.SyncTransforms();
        int stops = 0, dormant = 0;
        var stopFeet = Vector3.zero;
        chaser.StoppedAtSafeDoor += f => { stops++; stopFeet = f; };
        chaser.Tick(Dt, inside, inside + Vector3.up * ModuleUnits.PlayerEye, -into);
        var endedAtOnce = chaser.State != HunterState.Chase;
        var beatAtOnce = chaser.SafeDoorBeat;
        var chasedAgain = false;
        float stoppedFor = 0f, facing = 1f, beatEnd = -1f, leftBy = 0f;
        for (var t = 0f; t < 10f; t += Dt)
        {
            chaser.Tick(Dt, inside, inside + Vector3.up * ModuleUnits.PlayerEye, -into);
            world.TickDoorsForTools(Dt);
            Physics.SyncTransforms();
            if (chaser.SeesPlayer) seen++;
            if (chaser.State == HunterState.Chase) chasedAgain = true;
            if (chaser.State == HunterState.Dormant) dormant++;
            if (world.CellOf(chaser.Position) == x) inX++;
            if (stops > 0 && chaser.SafeDoorBeat && !chaser.Moving && chaser.State == HunterState.Hunt)
            {
                stoppedFor += Dt;
                facing = Mathf.Min(facing, Vector3.Dot(chaser.Heading, into));
            }
            if (beatEnd < 0f && !chaser.SafeDoorBeat)
            {
                beatEnd = t;
                leftBy = Flat(chaser.Position - stopFeet);
            }
        }
        Check(chased && endedAtOnce && seen == 0 && !chasedAgain && inX == 0 && !caught && world.CellOf(outside) == n,
            label + "safe zone: a chase (" + chased + ") ends the tick the player steps into X (" + endedAtOnce + "); for 10 s the player inside is never seen (" + seen
            + " ticks), never chased again (" + chasedAgain + "), never caught (" + caught + "), and it never steps into X (" + inX + " ticks)");
        Check(beatAtOnce && stops == 1 && Flat(stopFeet - stand) < .3f && stoppedFor >= 1.1f && facing > .9f && beatEnd > 0f && beatEnd <= 8f + Dt && leftBy >= 4f - .05f && dormant == 0,
            label + "L11: the Relay walks to its stand at the door (StoppedAtSafeDoor " + stops + "x, " + Flat(stopFeet - stand).ToString("0.00") + " m from the stand), stops there facing the door for "
            + stoppedFor.ToString("0.00") + " s (min facing " + facing.ToString("0.00") + "), then walks away: the beat ends at " + beatEnd.ToString("0.00") + " s, " + leftBy.ToString("0.00")
            + " m from where it stopped; it never parks or vanishes meanwhile (" + dormant + " dormant ticks)");
        // The player stands in the doorway, just inside X, the Relay at its stand on N's side, within catch distance: never caught.
        var doorway = mouth + into * .15f;
        playerRoot.transform.position = doorway;
        chaser.DebugPlace(stand);
        Physics.SyncTransforms();
        var doorwaySeen = 0;
        for (var t = 0f; t < 3f && !caught; t += Dt)
        {
            chaser.Tick(Dt, doorway, doorway + Vector3.up * ModuleUnits.PlayerEye, -into);
            world.TickDoorsForTools(Dt);
            Physics.SyncTransforms();
            if (chaser.SeesPlayer) doorwaySeen++;
            if (world.CellOf(chaser.Position) == x) inX++;
        }
        Check(!caught && doorwaySeen == 0 && inX == 0 && world.CellOf(doorway) == x && Flat(stand - doorway) < .7f,
            label + "safe zone: a player in the doorway, 0.15 m inside X, " + Flat(stand - doorway).ToString("0.00") + " m from the Relay's stand, is never seen ("
            + doorwaySeen + ") or caught (" + caught + "), and it never steps into X (" + inX + " ticks)");

        // The safe zone: a noise made inside X is never heard, whatever its radius. The door's own noise lies on its line
        // (DoorMoved fires at the opening's centre), the map's side: heard from N alone, and hunted to N's side of the door.
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
            // Blind, so they hunt by ear alone.
            var tuning = new FrontRoomsHunterTuning { sightRange = 0f };
            var ear = new FrontRoomsMapHunter(world, tuning, body, null, 13);
            ear.DebugPlace(world.CellCenter(far));
            ear.Noise(inside, 400f / tuning.hearing);
            var heard = ear.ListenPoint.HasValue || ear.State == HunterState.Hunt;
            // On the door's line (1 cm into X, inside the leaf's thickness): the door's own noise, no step added.
            var onLine = mouth + into * .01f;
            var lineDeaf = new FrontRoomsMapHunter(world, tuning, body, null, 14);
            lineDeaf.DebugPlace(world.CellCenter(far));
            lineDeaf.Noise(onLine, (farDistance - 1f) / tuning.hearing);
            var lineEar = new FrontRoomsMapHunter(world, tuning, body, null, 15);
            lineEar.DebugPlace(world.CellCenter(far));
            lineEar.Noise(onLine, (farDistance + 1f) / tuning.hearing);
            var lineHeard = world.CellOf(onLine) == x && !lineDeaf.ListenPoint.HasValue && lineEar.ListenPoint.HasValue;
            var nearest = float.MaxValue;
            var earInX = 0;
            for (var t = 0f; t < 20f; t += Dt)
            {
                lineEar.Tick(Dt, inside, inside + Vector3.up * ModuleUnits.PlayerEye, -into);
                world.TickDoorsForTools(Dt);
                Physics.SyncTransforms();
                nearest = Mathf.Min(nearest, Flat(lineEar.Position - stand));
                if (world.CellOf(lineEar.Position) == x) earInX++;
            }
            Check(!heard && lineHeard && nearest < .2f && earInX == 0,
                label + "safe zone: a noise inside X is never heard, even at 400 m (heard " + heard + "); on the door's line it is the door's own, from N alone ("
                + lineHeard + ", " + far + " is " + farDistance.ToString("0.0") + " m from N); that hunt ends at N's side of the door (" + nearest.ToString("0.00")
                + " m from the stand), never in X (" + earInX + " ticks)");
        }
        // The Relays stay where they are; the door must not stop on them.
        world.RelayBody = null;

        // 8. The seal (D7), with the player well inside X and the door open.
        eye.position = inside + Vector3.up * .05f;
        int moved = 0, latched = 0, sealedEvents = 0;
        FrontRoomsMapWorld.Door sealedDoor = null;
        world.DoorMoved += p => moved++;
        world.DoorLatched += (d, p) => { if (world.IsExitDoor(d)) latched++; };
        world.ExitDoorSealed += (d, p) => { sealedEvents++; sealedDoor = d; };
        var asked = world.SealExit();
        var midway = world.ExitSealing && !world.ExitSealed && world.Describe(door.leaf, out _) == null
            && world.Kind(door.leaf, eye.position, out _, out _) == FrontRoomsMapWorld.AimKind.None && world.Use(door.leaf) == FrontRoomsMapWorld.UseResult.None
            && !world.TryOpenDoor(n, x) && !world.TryOpenDoor(x, n) && moved == 1 && sealedEvents == 0;
        Check(asked && midway, label + "SealExit shuts the open EXIT door for the player (DoorMoved " + moved + "): from the call on no prompt, no kind, nothing on E or from TryOpenDoor; not sealed yet");
        for (var k = 0; k < 180; k++) world.TickDoorsForTools(Dt);
        Check(world.ExitSealed && latched == 1 && sealedEvents == 1 && sealedDoor == door && !door.open && door.angle < .01f,
            label + "it locks as it latches: ExitSealed " + world.ExitSealed + ", DoorLatched " + latched + "x, ExitDoorSealed " + sealedEvents + "x with the door, shut at " + door.angle.ToString("0.0") + " deg");
        var after = world.Describe(door.leaf, out _) == null && world.Kind(door.leaf, eye.position, out _, out _) == FrontRoomsMapWorld.AimKind.None
            && world.Use(door.leaf) == FrontRoomsMapWorld.UseResult.None && !world.TryOpenDoor(n, x);
        for (var k = 0; k < 120; k++) world.TickDoorsForTools(Dt);
        var again = world.SealExit();
        Check(after && again && sealedEvents == 1 && !door.open && door.angle < .01f && world.PassageBetween(n, x) == FrontRoomsMapWorld.Passage.Wall,
            label + "sealed for good: no prompt or kind, E and TryOpenDoor do nothing, it stays shut; SealExit again changes nothing (" + sealedEvents + " event)");
        // Shut and sealed, a noise in X is still never heard (the safe zone); the shut door's cost is the field's ShutOpening.
        if (farDistance > 0f)
        {
            var shutTuning = new FrontRoomsHunterTuning { sightRange = 0f };
            var shutCost = world.ExitOpeningCost();
            var shutEar = new FrontRoomsMapHunter(world, shutTuning, body, null, 17);
            shutEar.DebugPlace(world.CellCenter(far));
            shutEar.Noise(inside, 400f / shutTuning.hearing);
            world.RelayBody = null;
            Check(shutCost == FrontRoomsMapPathField.ShutOpening && !shutEar.ListenPoint.HasValue && shutEar.State != HunterState.Hunt,
                label + "safe zone: behind the shut EXIT door (cost " + shutCost + ") a noise in X is never heard, even at 400 m (" + shutEar.ListenPoint.HasValue + ")");
        }
        world.RebuildChunk(chunkX);
        var rebuilt = world.ExitDoor;
        Check(rebuilt != null && rebuilt != door && !rebuilt.open && rebuilt.angle < .01f && rebuilt.swing == 1f && world.ExitSealed && world.Describe(rebuilt.leaf, out _) == null
              && world.Use(rebuilt.leaf) == FrontRoomsMapWorld.UseResult.None && !world.TryOpenDoor(n, x),
            label + "rebuilt, the EXIT door comes back shut, sealed and swinging into X");

        // 9. X's chunk never shifts (D5); a neighbour dropped as long comes back shifted.
        var neighbour = new GridCoord(chunkX.x > 0 ? chunkX.x - 1 : chunkX.x + 1, chunkX.y);
        var neighbourBuilt = world.IsChunkBuiltAt(MapGrid.ChunkOrigin(neighbour));
        var revisionX = world.Cache.Get(chunkX).revision;
        var revisionNeighbour = world.Cache.Get(neighbour).revision;
        var shiftedBefore = world.ShiftedChunks;
        world.DropChunkForTools(chunkX);
        world.DropChunkForTools(neighbour);
        world.StreamForTools(spawn, profile.shiftAfterSeconds + 5f);
        var shiftedDoor = world.ExitDoor;
        Check(neighbourBuilt && world.ShiftedChunks == shiftedBefore + 1 && world.Cache.Get(chunkX).revision == revisionX && world.Cache.Get(neighbour).revision != revisionNeighbour
              && world.IsChunkBuiltAt(x) && !world.IsBuilt(x) && shiftedDoor != null && !shiftedDoor.open && world.Describe(shiftedDoor.leaf, out _) == null,
            label + "both dropped past the shift delay: X's chunk " + chunkX + " comes back unshifted (revision " + world.Cache.Get(chunkX).revision + "), the neighbour "
            + neighbour + " shifted (" + revisionNeighbour + " to " + world.Cache.Get(neighbour).revision + "); X still reserved, its door still sealed");

        // 10. Records of the floor, then NextFloor: nothing of them, nor of anything built, is left behind.
        world.DoorsNeedKeysForTools = true;
        FrontRoomsMapWorld.Door keyed = null, forced = null;
        foreach (var d in Doors(world).Values)
        {
            var candidate = world.DoorBetween(d.a, d.b);
            if (candidate == null || candidate.open || candidate.broken || world.IsExitDoor(candidate) || !world.IsBuilt(d.a) || !world.IsBuilt(d.b)) continue;
            // Not in the chunk shifted above, so the same seed builds the same door on its edge again (checked in 11).
            if (MapGrid.ChunkOf(d.a) == neighbour || MapGrid.ChunkOf(d.b) == neighbour) continue;
            if (keyed == null) keyed = candidate;
            else { forced = candidate; break; }
        }
        if (keyed == null || forced == null) { world.DoorsNeedKeysForTools = keys; Check(false, label + "fewer than two shut doors near the spawn"); return; }
        var keyZone = world.ZoneOf(keyed.a).id;
        world.GiveKeyForTools(keyZone);
        world.KeyArrived(keyZone);
        world.TryOpenDoor(keyed.a, keyed.b);
        world.BreakDoor(forced, world.CellCenter(forced.b));
        world.DoorsNeedKeysForTools = keys;
        // Mid-swing: the doors are still moving when the floor goes.
        for (var k = 0; k < 20; k++) world.TickDoorsForTools(Dt);
        var keyedA = keyed.a;
        var keyedB = keyed.b;
        var forcedA = forced.a;
        var forcedB = forced.b;
        var lampCell = start;
        for (var r = 0; r < 4 && world.LampLevel(lampCell) == FrontRoomsMapWorld.NoLamp; r++) lampCell = new GridCoord(start.x + r + 1, start.y);
        var lampModeBefore = world.LampModeOf(lampCell);
        world.SetLampMode(lampCell, ModuleLamp.Dead);
        world.SetLampOverride(lampCell, FrontRoomsMapWorld.LampFx.Dip, .5f, 10f);
        // The warning lamps' holds (M2's on Dead strikes, M1's on the look) are records of the floor too.
        world.SetAmbientHold(lampCell, true);
        world.SetLampHold(lampCell, ModuleLamp.Dim, .3f);
        var window = world.NearestIntactWindowForTools(spawn);
        var windowEdge = window != null ? window.edge : 0L;
        if (window != null)
        {
            var pane = window.pane.GetComponent<Collider>();
            world.BeginGlassHold(pane, window.pane.transform.position, window.root.forward);
            for (var t = 0f; t < 10f; t += Dt) if (world.Hold(pane, Dt, out _)) break;
        }
        var corner = new GridCoord(MapGrid.ChunkOf(start).x + 2, MapGrid.ChunkOf(start).y + 2);
        if (corner == chunkX) corner = new GridCoord(corner.x - 4, corner.y);
        world.DropChunkForTools(corner);
        var before = new Dictionary<string, int>();
        world.FloorStateForTools(before);
        var recorded = new[] { "built", "openDoors", "brokenDoors", "unlockedDoors", "movingDoors", "keysHeld", "keysAnnounced", "lampModes", "lampOverrides", "ambientHolds", "lampHolds", "glassBreaks", "droppedAt" };
        var missing = recorded.Where(k => !before.TryGetValue(k, out var v) || v == 0).ToList();
        var windowsBefore = announced.Count;
        var releasedBefore = released;
        var revisionBefore = world.PassageRevision;
        world.NextFloor(seed, 1, new RectInt(0, 0, 0, 0), default(GridCoord));
        var state = new Dictionary<string, int>();
        world.FloorStateForTools(state);
        var left = state.Where(p => p.Value != 0).Select(p => p.Key + " " + p.Value).ToList();
        var children = new List<string>();
        foreach (Transform t in world.transform) if (t != eye) children.Add(t.name);
        var colliders = world.GetComponentsInChildren<Collider>(true).Length;
        var lights = world.GetComponentsInChildren<Light>(true).Length;
        var renderers = world.GetComponentsInChildren<Renderer>(true).Length;
        Check(missing.Count == 0, label + "before NextFloor the floor holds every kind of record (" + string.Join(", ", before.Where(p => p.Value > 0).Select(p => p.Key + " " + p.Value))
            + "; missing: " + string.Join(", ", missing) + ")");
        Check(left.Count == 0 && children.Count == 0 && colliders == 0 && lights == 0 && renderers == 0 && world.BuiltChunkCount == 0,
            label + "NextFloor leaves nothing: lookups and records " + (left.Count == 0 ? "all 0" : string.Join(", ", left)) + "; under the map " + children.Count + " objects besides the player ("
            + string.Join(", ", children.Take(4)) + "), " + colliders + " colliders, " + lights + " lamps, " + renderers + " renderers");
        Check(windowsBefore > 0 && announced.Count == 0 && released - releasedBefore == windowsBefore,
            label + "NextFloor releases every window it announced (WindowReleased " + (released - releasedBefore) + " of " + windowsBefore + ")");
        Check(!world.HasExitArea && !world.ExitSealing && !world.ExitSealed && world.ExitLamps == 1f && world.ExitDoor == null && world.PassageRevision != revisionBefore
              && world.Seed == seed && world.GenerationTier == 1 && !world.ReadyAround(0) && !world.Settled && world.transform.position == mapRoot,
            label + "NextFloor forgets the exit area and its seal, moves PassageRevision on (" + revisionBefore + " to " + world.PassageRevision + "), keeps the root, and waits for Begin");

        // 11. The same seed again, with no exit area: byte for byte the first build, at the same root, with none of the old records.
        eye.position = spawn;
        world.Begin(eye, true);
        Physics.SyncTransforms();
        var replay = ChunkFingerprints(world);
        var identical = replay.Count == plain.Count && plain.All(p => replay.TryGetValue(p.Key, out var h) && h == p.Value);
        var keyedAgain = world.DoorBetween(keyedA, keyedB);
        var forcedAgain = world.DoorBetween(forcedA, forcedB);
        var clean = keyedAgain != null && !keyedAgain.open && !world.IsUnlockedForTools(keyedAgain) && forcedAgain != null && !forcedAgain.broken && !forcedAgain.open
            && !world.HasKeyFor(keyZone) && world.KeysHeld == 0 && world.LampModeOf(lampCell) == lampModeBefore && world.LampOverrideCount == 0
            && (window == null || !world.TryGetGlassBreak(windowEdge, out _));
        Check(identical && world.BuiltChunkCount == plainCount && world.transform.position == mapRoot,
            label + "the same seed builds the same floor at the same root: " + replay.Count(p => plain.TryGetValue(p.Key, out var h) && h == p.Value) + "/" + plain.Count + " chunks byte for byte the same");
        Check(clean, label + "none of the old floor's records come back: the opened door is shut and locked again, the broken one whole, no key, the lamp's own mode, no override, no glass break");

        // 12. A new seed at the carried tier: a new floor at the same root, its start area the old X, then its own EXIT.
        var seed2 = FrontRoomsExitPlanner.FloorSeed(seed, 2, 0);
        var startDoor = north;
        world.NextFloor(seed2, 2, new RectInt(x.x, x.y, 1, 1), startDoor);
        var plan2 = FrontRoomsExitPlanner.ChooseExit(world.Cache, new CellRect(x.x, x.y, 1, 1), startDoor, seed2, FrontRoomsExitPlanner.LaterBandMin, FrontRoomsExitPlanner.LaterBandMax);
        if (plan2.found) world.SetExitArea(plan2.exitCell);
        eye.position = inside + Vector3.up * .05f;
        world.Begin(eye, true);
        Physics.SyncTransforms();
        var floor2 = ChunkFingerprints(world);
        var sameAsFloor1 = floor2.Count(p => plain.TryGetValue(p.Key, out var h) && h == p.Value);
        var tiers = 0;
        var centre = MapGrid.ChunkOf(x);
        for (var dy = -2; dy <= 2; dy++)
        for (var dx = -2; dx <= 2; dx++)
            if (world.Cache.Get(new GridCoord(centre.x + dx, centre.y + dy)).tier == 2) tiers++;
        Check(world.Seed == seed2 && world.GenerationTier == 2 && tiers == 25 && world.BuiltChunkCount == plainCount && world.transform.position == mapRoot && sameAsFloor1 == 0,
            label + "floor 2 (seed " + seed2 + ") builds at the same root, " + world.BuiltChunkCount + " chunks, none like floor 1's (" + sameAsFloor1 + "), " + tiers + "/25 generated at the carried tier 2");
        Check(world.HasStartArea && world.InStartArea(x) && !world.IsBuilt(x) && world.IsBuilt(startDoor) && world.EffectiveEdge(x, startDoor) == EdgeKind.Open
              && world.EffectiveEdge(west, x) == EdgeKind.Wall && world.EffectiveEdge(x, east) == EdgeKind.Wall && world.EffectiveEdge(n, x) == EdgeKind.Wall
              && world.PassageBetween(startDoor, x) == FrontRoomsMapWorld.Passage.Wall && world.ZoneOf(x).id == world.ZoneOf(startDoor).id,
            label + "floor 2's start area is the old X: no map, walled on three sides, open to its door cell " + startDoor + " (where the stairwell's north door stands)");
        if (!plan2.found) Check(false, label + "floor 2: the planner found no EXIT from " + startDoor);
        else
        {
            var events = sealedEvents;
            var door2 = world.ExitDoor;
            var sealedAtOnce = world.SealExit() && world.ExitSealed && sealedEvents == events + 1 && (door2 == null ? sealedDoor == null : sealedDoor == door2 && !door2.open);
            Check(sealedAtOnce && !world.InExitArea(x) && world.InExitArea(plan2.exitCell),
                label + "floor 2's EXIT " + plan2.exitCell + (door2 != null ? " (built)" : " (its chunk not built)") + " was shut: SealExit seals it at once, with one ExitDoorSealed");
        }

        // 13. Beside the title's start area (floor 1 as the game plays it): only X's chunk builds differently from the start area alone.
        var titled = World(roots, profile, -57f, "MAP INTERACTION TEST / exit area beside a start area");
        var strip = new RectInt(2, -6, 5, 8);
        var stripDoor = new GridCoord(4, 2);
        titled.SetStartArea(strip, stripDoor);
        titled.BuildForCapture();
        var titleEye = titled.Player;
        var titleSpawn = titleEye.position;
        var stripOnly = ChunkFingerprints(titled);
        var plan3 = FrontRoomsExitPlanner.ChooseExit(titled.Cache, new CellRect(strip.x, strip.y, strip.width, strip.height), stripDoor, titled.Seed,
            FrontRoomsExitPlanner.Floor1BandMin, FrontRoomsExitPlanner.Floor1BandMax);
        if (!plan3.found) { Check(false, label + "beside the start area: the planner found no EXIT from " + stripDoor); return; }
        var x3 = plan3.exitCell;
        titled.NextFloor(titled.Seed, 1, strip, stripDoor);
        titled.SetExitArea(x3);
        titleEye.position = titleSpawn;
        titled.Begin(titleEye, true);
        var both = ChunkFingerprints(titled);
        var x3Name = "Chunk " + MapGrid.ChunkOf(x3) + " ";
        var same3 = 0;
        var x3Changed = false;
        foreach (var pair in stripOnly)
        {
            var match = both.TryGetValue(pair.Key, out var h) && h == pair.Value;
            if (pair.Key.StartsWith(x3Name)) x3Changed = !match;
            else if (match) same3++;
        }
        var inStrip = new GridCoord(stripDoor.x, stripDoor.y - 1);
        Check(both.Count == stripOnly.Count && same3 == stripOnly.Count - 1 && x3Changed && titled.InStartArea(inStrip) && titled.InExitArea(x3) && !titled.InStartArea(x3)
              && titled.EffectiveEdge(inStrip, stripDoor) == EdgeKind.Open && titled.EffectiveEdge(new GridCoord(strip.xMin - 1, -3), new GridCoord(strip.xMin, -3)) == EdgeKind.Wall
              && titled.EffectiveEdge(plan3.frontCell, x3) == EdgeKind.Door,
            label + "beside the title's start area, X " + x3 + ": " + same3 + "/" + (stripOnly.Count - 1) + " other chunks build as with the start area alone, X's changed " + x3Changed
            + "; EffectiveEdge keeps the start area's rule (open to its door, walled to the map) beside the exit rule");
    }

    // ---------- C22: the stairwell in X (FrontRoomsStairwell), plan v2 D8 ----------

    /// <summary>
    /// C22 step 3 (plan v2 D8). X is chosen and set as the game will (ChooseExit on the map's cache, SetExitArea
    /// before Begin) and the stairwell is made at that moment, under a root standing in for the game's. The same
    /// map then goes through NextFloor into the floor that starts in X, as a run does, and the stairwell with it.
    /// </summary>
    static void Stairwell(List<GameObject> roots)
    {
        const string label = "stairwell: ";
        var profile = FrontRoomsLevelProfiles.Resolve();
        var world = World(roots, profile, -59f, "MAP INTERACTION TEST / stairwell");
        world.BuildForCapture();
        var eye = world.Player;
        var spawn = eye.position;
        var seed = world.Seed;
        var start = world.CellOf(spawn);
        var plan = FrontRoomsExitPlanner.ChooseExit(world.Cache, new CellRect(0, 0, 0, 0), start, seed, FrontRoomsExitPlanner.Floor1BandMin, FrontRoomsExitPlanner.Floor1BandMax);
        if (!plan.found) { Check(false, label + "the planner found no EXIT from " + start + " (seed " + seed + ")"); return; }
        var x = plan.exitCell;
        var n = plan.frontCell;
        var doorCell = new GridCoord(x.x, x.y + 1);
        var cs = MapGrid.CellSize;
        var game = new GameObject("MAP INTERACTION TEST / stairwell game root") { hideFlags = HideFlags.DontSave };
        roots.Add(game);

        // The floor as the game makes it: X set and the stairwell made before the build.
        world.NextFloor(seed, 1, new RectInt(0, 0, 0, 0), default(GridCoord));
        world.SetExitArea(x);
        var stairwell = FrontRoomsStairwell.Create(world, x, game.transform);
        var hiddenBeforeBuild = !stairwell.Visible && stairwell.GetComponentsInChildren<Renderer>(true).All(r => !r.enabled) && !stairwell.Lamp.enabled;
        eye.position = spawn;
        world.Begin(eye, true);
        stairwell.SetVisible(world.IsChunkBuiltAt(x));
        Physics.SyncTransforms();

        // 1. Where it stands, and what it hangs under.
        var hinges = stairwell.NorthHinge.name.StartsWith("Door hinge") && stairwell.SouthHinge.name.StartsWith("Door hinge")
            && stairwell.NorthLeaf.transform.parent == stairwell.NorthHinge && stairwell.SouthLeaf.transform.parent == stairwell.SouthHinge;
        Check(stairwell.transform.parent == game.transform && !stairwell.transform.IsChildOf(world.transform) && stairwell.Map == world && stairwell.Cell == x
              && (stairwell.transform.position - world.CellCenter(x)).sqrMagnitude < 1e-8f && stairwell.transform.rotation == world.transform.rotation
              && hiddenBeforeBuild && stairwell.Visible && hinges,
            label + "made for X " + x + " at the map's CellCenter, under the game's root, not the map; hidden until X's chunk was built; hinges named for the sound layer ("
            + stairwell.NorthHinge.name + ", " + stairwell.SouthHinge.name + ")");

        // 2. Every piece but the floor slab stays inside X's lines by more than half a wall.
        var minInset = float.MaxValue;
        var nearestPiece = "";
        var pieceCount = 0;
        foreach (var mf in stairwell.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.name == "Floor") continue;
            pieceCount++;
            LocalBox(world, mf.transform, mf.sharedMesh, out var lo, out var hi);
            var inset = Mathf.Min(Mathf.Min(lo.x - x.x * cs, (x.x + 1) * cs - hi.x), Mathf.Min(lo.z - x.y * cs, (x.y + 1) * cs - hi.z));
            if (inset < minInset) { minInset = inset; nearestPiece = mf.name; }
        }
        Check(pieceCount >= 19 && minInset >= ModuleUnits.WallHalf + .01f - 1e-4f,
            label + "all " + pieceCount + " pieces but the floor slab stand " + minInset.ToString("0.000") + " m or more inside X's lines (the nearest: " + nearestPiece
            + "; WallHalf + 0.01 = " + (ModuleUnits.WallHalf + .01f).ToString("0.00") + ")");

        // 3. The floor (a box collider over the whole cell, under the map's walls) and the ceiling.
        var inside = world.CellCenter(x);
        var floorBox = stairwell.transform.Find("Floor");
        var floorCollider = floorBox != null ? floorBox.GetComponent<BoxCollider>() : null;
        var floorSpans = false;
        if (floorBox != null)
        {
            LocalBox(world, floorBox, floorBox.GetComponent<MeshFilter>().sharedMesh, out var flo, out var fhi);
            floorSpans = Mathf.Abs(flo.x - x.x * cs) < 1e-3f && Mathf.Abs(fhi.x - (x.x + 1) * cs) < 1e-3f && Mathf.Abs(flo.z - x.y * cs) < 1e-3f
                && Mathf.Abs(fhi.z - (x.y + 1) * cs) < 1e-3f && Mathf.Abs(fhi.y) < 1e-3f && Mathf.Abs(flo.y + ModuleUnits.FloorSlab) < 1e-3f;
        }
        var down = Physics.Raycast(inside + Vector3.up, Vector3.down, out var floorHit, 3f, ~0, QueryTriggerInteraction.Ignore);
        var up = Physics.Raycast(inside + Vector3.up, Vector3.up, out var ceilingHit, 10f, ~0, QueryTriggerInteraction.Ignore);
        Check(floorCollider != null && floorCollider.enabled && floorSpans && down && floorHit.collider == floorCollider && Mathf.Abs(floorHit.distance - 1f) < 1e-3f
              && up && ceilingHit.collider.name == "Ceiling" && ceilingHit.collider.transform.IsChildOf(stairwell.transform)
              && Mathf.Abs(ceilingHit.distance + 1f - FrontRoomsStairwell.CeilingHeight) < 1e-3f,
            label + "its floor is a box collider over the whole cell, top on the floor line, that a ray in X lands on (the map has none there); its ceiling stands at "
            + (up ? (ceilingHit.distance + 1f).ToString("0.00") : "-") + " m");

        // 4. Clear of the map's walls on X's four lines: no overlap, and a gap measured on each side (south: over the EXIT door's frame).
        var overlaps = MapOverlaps(world, stairwell);
        var faces = new List<string>();
        var minGap = float.MaxValue;
        var facesAtLine = 0;
        foreach (var d in Steps)
        {
            var dir = world.transform.TransformDirection(new Vector3(d.x, 0f, d.y));
            // Over the doorways at both ends (the door heads end at 2.17 m), at mid height at the sides.
            var from = inside + Vector3.up * (d.y != 0 ? 2.3f : 1.2f);
            var shell = float.MaxValue;
            var mapFace = float.MaxValue;
            foreach (var h in Physics.RaycastAll(from, dir, 3f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.collider.transform.IsChildOf(stairwell.transform)) shell = Mathf.Min(shell, h.distance);
                else if (world.IsArchitecture(h.collider)) mapFace = Mathf.Min(mapFace, h.distance);
            }
            var gap = mapFace - (shell + FrontRoomsStairwell.ShellThickness);
            minGap = Mathf.Min(minGap, gap);
            if (Mathf.Abs(mapFace - (cs * .5f - ModuleUnits.WallHalf)) < 1e-3f) facesAtLine++;
            faces.Add("(" + d.x + "," + d.y + ") shell " + shell.ToString("0.000") + " map " + mapFace.ToString("0.000"));
        }
        Check(overlaps.Count == 0 && facesAtLine == 4 && minGap >= .01f - 1e-4f,
            label + "clear of the map on X's lines: no stairwell box overlaps a map wall or door (" + string.Join(", ", overlaps.Take(4)) + "); the map's faces stand at the wall's half on all "
            + facesAtLine + " sides, at least " + minGap.ToString("0.000") + " m behind the shell's outer faces (" + string.Join("; ", faces) + ")");

        // 5. The EXIT leaf swings into X through the south doorway without touching it, and a body walks from N into X.
        var door = world.ExitDoor;
        if (door == null) { Check(false, label + "no EXIT door built on " + n + "-" + x); return; }
        eye.position = world.CellCenter(n) + Vector3.up * .05f;
        var opened = world.TryOpenDoor(n, x);
        for (var k = 0; k < 240; k++) world.TickDoorsForTools(Dt);
        Physics.SyncTransforms();
        var leafTouches = BoxOverlaps(door.leaf as BoxCollider, stairwell, true);
        var walkTouches = new List<string>();
        for (var s = 0; s <= 24; s++)
        {
            var p = Vector3.Lerp(world.CellCenter(n), inside, s / 24f);
            var count = Physics.OverlapCapsuleNonAlloc(p + Vector3.up * (ModuleUnits.PlayerRadius + .05f), p + Vector3.up * (ModuleUnits.PlayerHeight - ModuleUnits.PlayerRadius),
                ModuleUnits.PlayerRadius - .01f, overlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
                if (overlapBuffer[i].transform.IsChildOf(stairwell.transform) || world.IsArchitecture(overlapBuffer[i])) walkTouches.Add(s + " " + overlapBuffer[i].name);
        }
        Check(opened && door.open && door.angle > 90f && InCell(world, x, door.leaf.bounds.center, 0f) && leafTouches.Count == 0 && walkTouches.Count == 0,
            label + "the EXIT leaf opens into X (" + door.angle.ToString("0") + " deg) through the south doorway touching none of it (" + string.Join(", ", leafTouches.Take(4))
            + "), and a body walks from N's centre to X's touching no wall (" + string.Join(", ", walkTouches.Take(4)) + ")");

        // 6. SetVisible and the lamp.
        var allRenderers = stairwell.GetComponentsInChildren<Renderer>(true);
        var allColliders = stairwell.GetComponentsInChildren<Collider>(true);
        var southRenderer = stairwell.SouthLeaf.GetComponent<Renderer>();
        stairwell.SetVisible(false);
        var allOff = allRenderers.All(r => !r.enabled) && !stairwell.Lamp.enabled;
        var collidersKept = allColliders.Where(c => c != stairwell.SouthLeaf).All(c => c.enabled)
            && Physics.Raycast(inside + Vector3.up, Vector3.down, out var hiddenHit, 3f, ~0, QueryTriggerInteraction.Ignore) && hiddenHit.collider == floorCollider;
        stairwell.SetVisible(true);
        var allOn = allRenderers.Where(r => r != southRenderer).All(r => r.enabled) && !southRenderer.enabled && !stairwell.SouthLeaf.enabled && stairwell.Lamp.enabled;
        Check(allOff && collidersKept && allOn,
            label + "SetVisible(false) turns off all " + allRenderers.Length + " renderers and the lamp and leaves the " + (allColliders.Length - 1)
            + " colliders on (the floor still holds a body); SetVisible(true) turns them back on, all but the south leaf, which waits for the seal");
        var lamp = stairwell.Lamp;
        var lights = stairwell.GetComponentsInChildren<Light>(true);
        var lampAt = world.transform.InverseTransformPoint(lamp.transform.position);
        var lampRoom = Mathf.Min(Mathf.Min(lampAt.x - x.x * cs, (x.x + 1) * cs - lampAt.x), Mathf.Min(lampAt.z - x.y * cs, (x.y + 1) * cs - lampAt.z));
        stairwell.LampLevel = 0f;
        var dark = !lamp.enabled;
        stairwell.LampLevel = .5f;
        var halfLit = lamp.enabled && Mathf.Abs(lamp.intensity - FrontRoomsStairwell.LampIntensity * .5f) < 1e-4f;
        stairwell.SetVisible(false);
        var hiddenDark = !lamp.enabled;
        stairwell.SetVisible(true);
        stairwell.LampLevel = 1f;
        var fullLit = lamp.enabled && Mathf.Abs(lamp.intensity - FrontRoomsStairwell.LampIntensity) < 1e-4f;
        Check(lights.Length == 1 && lamp.type == LightType.Point && lamp.range <= 1.5f + 1e-4f && lamp.shadows == LightShadows.None && lampRoom >= lamp.range - 1e-4f
              && lampAt.y > 0f && lampAt.y < FrontRoomsStairwell.CeilingHeight && dark && halfLit && hiddenDark && fullLit,
            label + "one lamp, a point light of range " + lamp.range.ToString("0.00") + " m, no shadows, " + lampRoom.ToString("0.00") + " m from X's nearest line (its light ends inside X); LampLevel 0 dark ("
            + dark + "), 0.5 half (" + halfLit + "), 1 full (" + fullLit + "); hidden, it is off (" + hiddenDark + ")");

        // 7. The seal: the map's EXIT door locks shut, then the static leaf stands in the south doorway.
        world.SealExit();
        for (var k = 0; k < 120; k++) world.TickDoorsForTools(Dt);
        var exitShut = world.ExitSealed && !door.open && door.angle < .01f;
        stairwell.Seal();
        stairwell.Seal();
        Physics.SyncTransforms();
        var sealTouches = BoxOverlaps(stairwell.SouthLeaf as BoxCollider, stairwell, false);
        var sealOverlaps = MapOverlaps(world, stairwell);
        Check(exitShut && stairwell.Sealed && southRenderer.enabled && stairwell.SouthLeaf.enabled && sealTouches.Count == 0 && sealOverlaps.Count == 0,
            label + "sealed once the EXIT door locked: the static leaf shows in the south doorway with its collider, touching nothing (" + string.Join(", ", sealTouches.Take(4))
            + ") and no map wall or the shut EXIT leaf (" + string.Join(", ", sealOverlaps.Take(4)) + ")");

        // 8. Floor 2 starts in X: NextFloor leaves the stairwell standing, and JoinNextFloor closes X's open north line.
        var refused = !stairwell.JoinNextFloor() && !stairwell.Joined;
        var parts = stairwell.GetComponentsInChildren<Transform>(true).Length;
        var seed2 = FrontRoomsExitPlanner.FloorSeed(seed, 2, 0);
        world.NextFloor(seed2, 2, new RectInt(x.x, x.y, 1, 1), doorCell);
        var plan2 = FrontRoomsExitPlanner.ChooseExit(world.Cache, new CellRect(x.x, x.y, 1, 1), doorCell, seed2, FrontRoomsExitPlanner.LaterBandMin, FrontRoomsExitPlanner.LaterBandMax);
        if (plan2.found) world.SetExitArea(plan2.exitCell);
        var survived = stairwell != null && stairwell.transform.parent == game.transform && stairwell.GetComponentsInChildren<Transform>(true).Length == parts
            && (stairwell.transform.position - world.CellCenter(x)).sqrMagnitude < 1e-8f && stairwell.Sealed;
        eye.position = inside + Vector3.up * .05f;
        world.Begin(eye, true);
        stairwell.SetVisible(world.IsChunkBuiltAt(x));
        var joined = stairwell.JoinNextFloor() && stairwell.JoinNextFloor() && stairwell.Joined;
        Physics.SyncTransforms();
        var overlaps2 = MapOverlaps(world, stairwell);
        var minInset2 = float.MaxValue;
        var endWall = stairwell.transform.Find("North end wall");
        foreach (var mf in stairwell.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.name == "Floor" || (endWall != null && mf.transform.IsChildOf(endWall))) continue;
            LocalBox(world, mf.transform, mf.sharedMesh, out var lo, out var hi);
            minInset2 = Mathf.Min(minInset2, Mathf.Min(Mathf.Min(lo.x - x.x * cs, (x.x + 1) * cs - hi.x), Mathf.Min(lo.z - x.y * cs, (x.y + 1) * cs - hi.z)));
        }
        Check(refused && survived && joined && endWall != null && world.InStartArea(x) && overlaps2.Count == 0 && minInset2 >= ModuleUnits.WallHalf + .01f - 1e-4f,
            label + "JoinNextFloor is refused while X is not the start area; NextFloor leaves the stairwell standing, sealed, where it was; on floor 2 (seed " + seed2
            + ", X its start area) the end wall is built and nothing of the stairwell overlaps the new map's walls (" + string.Join(", ", overlaps2.Take(4)) + "), the shell still "
            + minInset2.ToString("0.000") + " m inside X's lines");

        // From the door cell, X's north line reads as a wall: flush where a map wall's face would be, and no ray finds the void over the stairwell.
        var doorCeiling = MapGrid.CeilingHeight(world.ZoneOf(doorCell).height);
        var doorCentre = world.CellCenter(doorCell);
        var southward = world.transform.TransformDirection(Vector3.back);
        var eastward = world.transform.TransformDirection(Vector3.right);
        var faceAt = cs * .5f - ModuleUnits.WallHalf;
        int rays = 0, flush = 0;
        var escapes = new List<string>();
        foreach (var dx in new[] { -1.2f, -.75f, .75f, 1.2f })
        foreach (var h in new[] { .3f, 1.2f, 2f, 2.3f, doorCeiling - .05f })
        {
            rays++;
            var from = doorCentre + eastward * dx + Vector3.up * h;
            var hit = Physics.Raycast(from, southward, out var rayHit, 6f, ~0, QueryTriggerInteraction.Ignore);
            if (hit && Mathf.Abs(rayHit.distance - faceAt) < 1e-3f) flush++;
            if (!hit || rayHit.distance > faceAt + 1e-3f) escapes.Add("x " + dx + " y " + h + ": " + (hit ? rayHit.distance.ToString("0.00") + " " + rayHit.collider.name : "nothing"));
        }
        foreach (var sx in new[] { -1f, 1f })
        foreach (var h in new[] { .3f, 1.2f, 2.3f, 2.6f, doorCeiling - .05f })
        {
            rays++;
            // Toward X's north corners, where the end wall meets the map's walls.
            var from = doorCentre + eastward * (sx * .6f) + Vector3.up * h;
            var corner = inside + eastward * (sx * cs * .5f) - southward * (cs * .5f) + Vector3.up * h;
            var dir = corner - from;
            var reach = dir.magnitude;
            var hit = Physics.Raycast(from, dir / reach, out var rayHit, 6f, ~0, QueryTriggerInteraction.Ignore);
            if (!hit || rayHit.distance > reach + .25f) escapes.Add("corner " + sx + " y " + h + ": " + (hit ? rayHit.distance.ToString("0.00") + " " + rayHit.collider.name : "nothing"));
        }
        Check(escapes.Count == 0 && flush >= 10,
            label + "seen from the door cell " + doorCell + " (ceiling " + doorCeiling.ToString("0.0") + " m), X's north line is closed: " + flush + " of 20 straight rays meet a wall face "
            + faceAt.ToString("0.00") + " m out, as a map wall's; no ray of " + rays + " (corners too) finds the void over X (" + string.Join("; ", escapes.Take(6)) + ")");

        // 9. The north door: away from a player in X, onto the new floor; it stands still under DoorsPaused, then shuts and locks for good.
        int moved = 0, latched = 0;
        stairwell.NorthDoorMoved += p => moved++;
        stairwell.NorthDoorLatched += p => latched++;
        var player = inside;
        var openedNorth = stairwell.Open(player);
        var twice = stairwell.Open(player);
        world.DoorsPaused = true;
        for (var k = 0; k < 30; k++) stairwell.TickForTools(Dt);
        var held = stairwell.NorthDoorAngle == 0f && moved == 0;
        world.DoorsPaused = false;
        for (var k = 0; k < 60; k++) stairwell.TickForTools(Dt);
        Physics.SyncTransforms();
        var leafMesh = stairwell.NorthLeaf.GetComponent<MeshFilter>().sharedMesh;
        LocalBox(world, stairwell.NorthLeaf.transform, leafMesh, out var openLo, out var openHi);
        var openCell = new GridCoord(Mathf.FloorToInt((openLo.x + openHi.x) * .5f / cs), Mathf.FloorToInt((openLo.z + openHi.z) * .5f / cs));
        var northTouches = BoxOverlaps(stairwell.NorthLeaf as BoxCollider, stairwell, false);
        Check(openedNorth && !twice && held && stairwell.NorthDoorOpen && Mathf.Abs(stairwell.NorthDoorAngle - FrontRoomsStairwell.OpenDegrees) < .01f && stairwell.NorthDoorSwing == 1f
              && openCell == doorCell && northTouches.Count == 0 && moved == 1,
            label + "Open for a player in X swings the north leaf away from them, out of X onto the new floor (" + stairwell.NorthDoorAngle.ToString("0") + " deg, the leaf in "
            + openCell + "), touching nothing (" + string.Join(", ", northTouches.Take(4)) + "); a second Open does nothing; DoorsPaused held it still; NorthDoorMoved " + moved + "x");
        stairwell.Close();
        for (var k = 0; k < 60; k++) stairwell.TickForTools(Dt);
        Physics.SyncTransforms();
        LocalBox(world, stairwell.NorthLeaf.transform, leafMesh, out var shutLo, out var shutHi);
        var shutInside = shutLo.z >= x.y * cs + ModuleUnits.WallHalf + .01f && shutHi.z <= (x.y + 1) * cs - ModuleUnits.WallHalf - .01f;
        var reopened = stairwell.Open(player);
        stairwell.Close();
        Check(stairwell.NorthDoorLocked && !stairwell.NorthDoorOpen && stairwell.NorthDoorAngle == 0f && latched == 1 && moved == 2 && !reopened && shutInside,
            label + "Close turns it back into its frame (NorthDoorMoved " + moved + "x) and it latches locked (NorthDoorLatched " + latched + "x); nothing opens it again");

        // The other way: opened for someone beyond it, a north door swings into its X.
        var far = new GridCoord(x.x + 40, x.y);
        var other = FrontRoomsStairwell.Create(world, far, game.transform);
        var beyond = world.CellCenter(new GridCoord(far.x, far.y + 1));
        var openedOther = other.Open(beyond);
        for (var k = 0; k < 60; k++) other.TickForTools(Dt);
        LocalBox(world, other.NorthLeaf.transform, other.NorthLeaf.GetComponent<MeshFilter>().sharedMesh, out var otherLo, out var otherHi);
        var otherCell = new GridCoord(Mathf.FloorToInt((otherLo.x + otherHi.x) * .5f / cs), Mathf.FloorToInt((otherLo.z + otherHi.z) * .5f / cs));
        Check(openedOther && other.NorthDoorSwing == -1f && otherCell == far && Mathf.Abs(other.NorthDoorAngle - FrontRoomsStairwell.OpenDegrees) < .01f,
            label + "opened for someone beyond it (north of the door), a stairwell's north leaf swings away from them, into its own X (" + otherCell + ")");
        UnityEngine.Object.DestroyImmediate(other.gameObject);

        // 10. X's north corners, where the end wall meets the map's walls: closed on every open side. The post square at
        // each corner faces open floor from the door cell's side when the edge running north from it is Open, and from
        // the side cell when the edge running away from X along the line is Open too (a free wall end); with a wall
        // there it is an L. This floor first, then the first later floor seeds that pass SeedFor's tests and whose
        // corners hold a free end and an L.
        int freeEnds = 0, ells = 0, runs = 0, cornerRays = 0;
        var gaps = NorthCornerGaps(world, stairwell, x, ref freeEnds, ref ells, ref runs, ref cornerRays);
        var cornerOverlaps = MapOverlaps(world, stairwell);
        var probed = new List<string> { seed2 + " (" + CornerKinds(world, x) + ")" };
        UnityEngine.Object.DestroyImmediate(stairwell.gameObject);
        bool wantFree = freeEnds == 0, wantEll = ells == 0;
        for (var k = 1; k <= 64 && (wantFree || wantEll); k++)
        {
            var floorSeed = FrontRoomsExitPlanner.FloorSeed(seed, 2, k);
            world.NextFloor(floorSeed, 2, new RectInt(x.x, x.y, 1, 1), doorCell);
            // Only a floor the game can start in X: SeedFor's tests (a Standard Level 0 door cell, an open way on).
            if (!FrontRoomsExitPlanner.StartRowStandard(world.Cache, doorCell, 0) || !FrontRoomsExitPlanner.StartRowLeadsOn(world.Cache, doorCell, 0, 1, true)) continue;
            var kinds = CornerKinds(world, x);
            var hasFree = kinds.Contains("free end");
            var hasEll = kinds.Contains("L");
            if (!(wantFree && hasFree) && !(wantEll && hasEll)) continue;
            eye.position = inside + Vector3.up * .05f;
            world.Begin(eye, true);
            var probe = FrontRoomsStairwell.Create(world, x, game.transform);
            var probeJoined = probe.JoinNextFloor();
            Physics.SyncTransforms();
            if (!probeJoined) gaps.Add("floor seed " + floorSeed + ": JoinNextFloor refused");
            gaps.AddRange(NorthCornerGaps(world, probe, x, ref freeEnds, ref ells, ref runs, ref cornerRays));
            cornerOverlaps.AddRange(MapOverlaps(world, probe));
            probed.Add(floorSeed + " (" + kinds + ")");
            UnityEngine.Object.DestroyImmediate(probe.gameObject);
            if (hasFree) wantFree = false;
            if (hasEll) wantEll = false;
        }
        Check(gaps.Count == 0 && cornerOverlaps.Count == 0 && freeEnds >= 1 && ells >= 1,
            label + "X's north corners are closed where the end wall meets the map: " + cornerRays + " rays across the post squares of " + (freeEnds + ells)
            + " open corners (" + freeEnds + " free wall ends, " + ells + " L; " + runs + " closed by a wall running on) all meet a face flush with the walls ("
            + string.Join("; ", gaps.Take(6)) + "); no overlap (" + string.Join(", ", cornerOverlaps.Take(4)) + "); floors " + string.Join(", ", probed));
    }

    // C22: what stands at X's two north corners on a floor that starts in X, read from the map's edges: "free end" when the
    // edge running north from the corner and the edge running away from X along the line are both Open, "L" when only
    // the first is, "run" when a wall runs on north (X's side wall continues; nothing faces open floor there).
    static string CornerKinds(FrontRoomsMapWorld world, GridCoord x)
    {
        var door = new GridCoord(x.x, x.y + 1);
        var kinds = new List<string>();
        foreach (var side in new[] { -1, 1 })
        {
            var beside = new GridCoord(x.x + side, x.y);
            var besideNorth = new GridCoord(x.x + side, x.y + 1);
            if (world.EffectiveEdge(besideNorth, door) != EdgeKind.Open) kinds.Add((side < 0 ? "west " : "east ") + "run");
            else kinds.Add((side < 0 ? "west " : "east ") + (world.EffectiveEdge(beside, besideNorth) == EdgeKind.Open ? "free end" : "L"));
        }
        return string.Join(", ", kinds);
    }

    // C22: rays across the post square at each of X's north corners, from wherever it faces open floor: south from the
    // door cell's side (0.9 m out) across the post's width, and (a free end) along the line from the side cell across its
    // depth. Each must meet a face exactly WallHalf past the corner, flush with the end wall's and the map walls' faces;
    // a notch lets it run on to a wall's end cap or the end wall's side. Only architecture and the end wall count.
    static List<string> NorthCornerGaps(FrontRoomsMapWorld world, FrontRoomsStairwell stairwell, GridCoord x, ref int freeEnds, ref int ells, ref int runs, ref int rays)
    {
        var gaps = new List<string>();
        var cs = MapGrid.CellSize;
        const float Out = .9f;
        var expected = Out - ModuleUnits.WallHalf;
        var centre = world.CellCenter(x);
        var north = world.transform.TransformDirection(Vector3.forward);
        var east = world.transform.TransformDirection(Vector3.right);
        var door = new GridCoord(x.x, x.y + 1);
        var endWall = stairwell.transform.Find("North end wall");
        if (endWall == null) { gaps.Add("no end wall"); return gaps; }
        foreach (var side in new[] { -1, 1 })
        {
            var beside = new GridCoord(x.x + side, x.y);
            var besideNorth = new GridCoord(x.x + side, x.y + 1);
            if (world.EffectiveEdge(besideNorth, door) != EdgeKind.Open) { runs++; continue; }
            var sideOpen = world.EffectiveEdge(beside, besideNorth) == EdgeKind.Open;
            if (sideOpen) freeEnds++;
            else ells++;
            var corner = centre + east * (side * cs * .5f) + north * (cs * .5f);
            var name = (side < 0 ? "west" : "east") + (sideOpen ? " free end" : " L");
            foreach (var h in new[] { .3f, 1.2f, 2f, 2.3f })
            {
                foreach (var across in new[] { -.07f, -.03f, .03f, .07f })
                {
                    rays++;
                    var d = ArchitectureHit(world, endWall, corner + east * across + north * Out + Vector3.up * h, -north, 3f);
                    if (Mathf.Abs(d - expected) > 1e-3f) gaps.Add(name + " from the door cell at " + across.ToString("0.00") + " m, y " + h + ": " + d.ToString("0.000") + " m, not " + expected.ToString("0.000"));
                }
                if (!sideOpen) continue;
                foreach (var along in new[] { -.07f, -.03f, .03f, .07f })
                {
                    rays++;
                    var d = ArchitectureHit(world, endWall, corner + north * along + east * (side * Out) + Vector3.up * h, -east * side, 3f);
                    if (Mathf.Abs(d - expected) > 1e-3f) gaps.Add(name + " from the side at " + along.ToString("0.00") + " m, y " + h + ": " + d.ToString("0.000") + " m, not " + expected.ToString("0.000"));
                }
            }
        }
        return gaps;
    }

    // The nearest hit on the map's architecture (shell, doors, windows) or the stairwell's end wall; infinity for none.
    static float ArchitectureHit(FrontRoomsMapWorld world, Transform endWall, Vector3 from, Vector3 direction, float reach)
    {
        var best = float.PositiveInfinity;
        foreach (var hit in Physics.RaycastAll(from, direction, reach, ~0, QueryTriggerInteraction.Ignore))
            if (world.IsArchitecture(hit.collider) || hit.collider.transform.IsChildOf(endWall))
                best = Mathf.Min(best, hit.distance);
        return best;
    }

    // ---------- C22: the Relay parked for a floor change (FrontRoomsMapHunter.Park), plan v2 D12 ----------

    /// <summary>
    /// C22 step 3 (plan v2 D12). A Relay released as in play, then caught in the middle of breaking a door, is
    /// parked as the seal parks it; the same Relay then serves the next floor of the same map (NextFloor).
    /// </summary>
    static void RelayPark(List<GameObject> roots)
    {
        const string label = "relay park: ";
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), -61f, "MAP INTERACTION TEST / relay park");
        world.BuildForCapture();
        var eye = world.Player;
        var spawn = eye.position;
        if (!FindDoor(world, out var a, out var b, out var c)) { Check(false, label + "no closed door with an open cell behind it near the spawn"); return; }
        var door = world.DoorBetween(a, b);
        var playerRoot = new GameObject("MAP INTERACTION TEST / relay park player") { hideFlags = HideFlags.DontSave };
        roots.Add(playerRoot);
        var body = playerRoot.AddComponent<CapsuleCollider>();
        body.height = ModuleUnits.PlayerHeight;
        body.radius = ModuleUnits.PlayerRadius;
        body.center = Vector3.up * (ModuleUnits.PlayerHeight * .5f);
        var rigRoot = new GameObject("MAP INTERACTION TEST / relay park rig") { hideFlags = HideFlags.DontSave };
        roots.Add(rigRoot);
        var tuning = new FrontRoomsHunterTuning { sightRange = 0f };
        var hunter = new FrontRoomsMapHunter(world, tuning, body, rigRoot.transform, 21);
        var states = new List<HunterState>();
        hunter.StateChanged += s => states.Add(s);
        var warnStages = new List<int>();
        hunter.WarnStageChanged += s => warnStages.Add(s);
        var arrivals = 0;
        hunter.Arrived += (p, tag) => arrivals++;
        var target = world.CellCenter(b);
        playerRoot.transform.position = target;
        Physics.SyncTransforms();

        // 1. Released as in play (its release timer runs up to the delay), then sent to break the door.
        var firstRelease = false;
        for (var t = 0f; t < tuning.releaseDelaySeconds + 2f && !firstRelease; t += Dt)
        {
            hunter.Tick(Dt, target, target + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
            firstRelease = hunter.Released;
        }
        hunter.DebugPlace(world.CellCenter(c));
        hunter.Noise(target, 1000f);
        var breaking = false;
        for (var t = 0f; t < 20f && !breaking; t += Dt)
        {
            hunter.Tick(Dt, target, target + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
            world.TickDoorsForTools(Dt);
            Physics.SyncTransforms();
            breaking = hunter.State == HunterState.BreakDoor && world.IsBeingBroken(door) && hunter.BlowIndex >= 0;
        }
        var blowBefore = hunter.BlowIndex;
        var stageBefore = hunter.WarnStage;
        var heardBefore = hunter.ListenPoint.HasValue;
        var statesBefore = states.Count;
        var relaysBefore = hunter.Relays;
        var brokenBefore = hunter.DoorsBroken;
        var arrivalsBefore = arrivals;
        rigRoot.SetActive(true);

        // 2. Park: Dormant at once, everything of the floor dropped.
        hunter.Park();
        var dormantOnce = states.Count == statesBefore + 1 && states[states.Count - 1] == HunterState.Dormant;
        hunter.Park();
        var noSecond = states.Count == statesBefore + 1;
        Check(firstRelease && breaking && stageBefore > 0 && heardBefore && hunter.State == HunterState.Dormant && !hunter.Released && dormantOnce && noSecond,
            label + "a Relay breaking a door (blow " + blowBefore + " struck, warning stage " + stageBefore + ") parks Dormant, StateChanged once (" + dormantOnce + "); parking again changes nothing");
        Check(!world.IsBeingBroken(door) && !door.broken && world.Describe(door.leaf, out _) != null && hunter.BlowIndex == -1,
            label + "the door it was breaking: its mark lifted, still whole, a prompt again, no blow pending");
        Check(hunter.DebugSteering.StartsWith("path 0/0 detour 0/0") && !hunter.ListenPoint.HasValue && !hunter.SeesPlayer && !hunter.Moving && hunter.DoorSqueeze == 0f
              && hunter.WarnStage == 0 && warnStages.Count > 0 && warnStages[warnStages.Count - 1] == 0 && float.IsPositiveInfinity(hunter.WarningDistance)
              && float.IsPositiveInfinity(hunter.PathDistanceToPlayer) && float.IsPositiveInfinity(hunter.PathOpeningCost) && hunter.WarnRunSeconds == 0f
              && hunter.WarnStage2RunSeconds == 0f && !hunter.PlayerSeesRelay,
            label + "route (" + hunter.DebugSteering + "), noise, sight and warning cleared: stage 0 at once (WarnStageChanged 0), W and F infinite");
        Check(!rigRoot.activeSelf && world.RelayBody != null && !world.RelayBody().HasValue && hunter.DoorsBroken == brokenBefore && hunter.Relays == relaysBefore,
            label + "its body is hidden (rig off), no door stops on it any more, and the run's counts stay (doors broken " + hunter.DoorsBroken + ", relays " + hunter.Relays + ")");

        // 3. Parked it hears nothing, and only a whole release delay brings it back.
        hunter.Noise(target, 1000f);
        var deaf = !hunter.ListenPoint.HasValue && hunter.State == HunterState.Dormant;
        var releasedAt = -1f;
        for (var t = 0f; t < tuning.releaseDelaySeconds + 1f; t += Dt)
        {
            hunter.Tick(Dt, target, target + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
            world.TickDoorsForTools(Dt);
            if (hunter.Released) { releasedAt = t + Dt; break; }
        }
        Check(deaf && releasedAt >= tuning.releaseDelaySeconds - Dt && hunter.Relays == relaysBefore + 1 && arrivals == arrivalsBefore + 1 && !door.broken && !world.IsBeingBroken(door),
            label + "parked it hears no noise; its release timer starts again from 0: released " + releasedAt.ToString("0.00") + " s later (delay " + tuning.releaseDelaySeconds.ToString("0.0")
            + " s), appearing anew (relays " + hunter.Relays + "), the door never broken");

        // 4. Across NextFloor: path fields and the warning lamps' room are the new floor's, and the parked Relay comes back on its map.
        var from = world.CellOf(spawn);
        var probe = new FrontRoomsMapHunter(world, tuning, body, null, 23);
        probe.DebugPlace(world.CellCenter(from));
        var cells = new List<GridCoord>();
        for (var dy = -8; dy <= 8; dy++)
        for (var dx = -8; dx <= 8; dx++) cells.Add(new GridCoord(from.x + dx, from.y + dy));
        var floor1 = cells.Select(cell => probe.PathDistanceFromRelay(cell)).ToList();
        var warn = new FrontRoomsWarnLamps(world, 31);
        var relayFar = world.CellCenter(new GridCoord(from.x + 20, from.y));
        for (var t = 0f; t < 3f && warn.BurstCount == 0; t += Dt) warn.Tick(Dt, 1, false, spawn, relayFar);
        var room1 = new List<GridCoord>(warn.RoomSet);
        warn.Reset();
        hunter.Park();
        var revisionBefore = world.PassageRevision;
        world.NextFloor(FrontRoomsExitPlanner.FloorSeed(world.Seed, 2, 0), 2, new RectInt(0, 0, 0, 0), default(GridCoord));
        eye.position = spawn;
        world.Begin(eye, true);
        Physics.SyncTransforms();
        var fresh = new FrontRoomsMapHunter(world, tuning, body, null, 24);
        fresh.DebugPlace(world.CellCenter(from));
        int same = 0, changed = 0;
        for (var i = 0; i < cells.Count; i++)
        {
            var kept = probe.PathDistanceFromRelay(cells[i]);
            var anew = fresh.PathDistanceFromRelay(cells[i]);
            if (kept == anew || (float.IsPositiveInfinity(kept) && float.IsPositiveInfinity(anew))) same++;
            if (!(floor1[i] == anew || (float.IsPositiveInfinity(floor1[i]) && float.IsPositiveInfinity(anew)))) changed++;
        }
        Check(world.PassageRevision != revisionBefore && same == cells.Count && changed > 0,
            label + "a path field made on one floor is not read on the next: after NextFloor (PassageRevision " + revisionBefore + " to " + world.PassageRevision + ") a Relay that stood still reads "
            + same + "/" + cells.Count + " distances as a new one does (" + changed + " differ from floor 1's)");
        var bursts = warn.BurstCount;
        for (var t = 0f; t < 3f && warn.BurstCount == bursts; t += Dt) warn.Tick(Dt, 1, false, spawn, relayFar);
        var room2 = new HashSet<GridCoord>(warn.RoomSet);
        var freshWarn = new FrontRoomsWarnLamps(world, 32);
        freshWarn.RoomForTools(spawn, relayFar);
        var expected = new HashSet<GridCoord>(freshWarn.RoomSet);
        Check(warn.BurstCount == bursts + 1 && room2.SetEquals(expected) && !expected.SetEquals(room1),
            label + "after Reset at the seal, the warning's next burst in the same cell dips the new floor's room (" + room2.Count + " cells, as worked out anew: " + room2.SetEquals(expected)
            + "), not the old floor's (" + room1.Count + " cells)");
        var back = false;
        for (var t = 0f; t < tuning.releaseDelaySeconds + 2f && !back; t += Dt)
        {
            hunter.Tick(Dt, spawn, spawn + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
            back = hunter.Released;
        }
        // Its own fields stop at 45 m (it appears 9-15 cells out): an uncapped field from the player measures the walk.
        var walk = new FrontRoomsMapPathField(400f);
        walk.Build(world, world.CellOf(spawn));
        var walkTo = walk.DistanceTo(world.CellOf(hunter.Position));
        Check(back && world.IsBuilt(world.CellOf(hunter.Position)) && !float.IsPositiveInfinity(walkTo),
            label + "the parked Relay is released on the new floor, in a built cell of its map " + world.CellOf(hunter.Position) + ", " + walkTo.ToString("0.0")
            + " m of the new floor's walking from the player");
    }

    // A piece's box in the map's frame: the corners of its mesh's bounds through its transform (any rotation), as min and max.
    // ---------- C23 step 1: the EXIT sign over the EXIT door, the exit lamp hold, the front rules ----------

    /// <summary>
    /// C23 step 1 (Red 2026-10-22; agreed with the visual chat: research/stairwell/10_directions.md 7,
    /// research/exit_sign/70_lit_lock.md). The front rules on every EXIT planned over a scan of floor seeds, read
    /// with this test's own loops; then one EXIT of each sign kind (the flat sign, and the end-on blade where a
    /// corridor runs past the door) is set and built as the game does it, and its sign, the exit lamp hold and
    /// the stairwell beside it are checked; last, the hold beside a start area.
    /// </summary>
    static void ExitSign(List<GameObject> roots)
    {
        const string label = "exit sign: ";
        var profile = FrontRoomsLevelProfiles.Resolve();
        var world = World(roots, profile, -63f, "MAP INTERACTION TEST / exit sign");
        world.BuildForCapture();
        var eye = world.Player;
        var seed = world.Seed;
        var start = world.CellOf(eye.position);
        var none = new CellRect(0, 0, 0, 0);

        // 1. Rules 3 and 4 on every EXIT the planner picks over a scan of floor seeds (from the spawn cell, floor 1's
        //    band); the corridor flag against the edges; one EXIT of each sign kind kept for the build checks.
        const int scan = 40;
        int found = 0, frontOk = 0, corridorOk = 0, corridors = 0, rejectedHeight = 0, rejectedSightline = 0, before = 0;
        var problems = new List<string>();
        var pickSeeds = new Dictionary<bool, int>();
        var pickPlans = new Dictionary<bool, ExitPlan>();
        for (var k = 0; k < scan; k++)
        {
            var s = k == 0 ? seed : FrontRoomsExitPlanner.FloorSeed(seed, 9, k);
            world.NextFloor(s, 1, new RectInt(0, 0, 0, 0), default(GridCoord));
            var p = FrontRoomsExitPlanner.ChooseExit(world.Cache, none, start, s, FrontRoomsExitPlanner.Floor1BandMin, FrontRoomsExitPlanner.Floor1BandMax);
            if (!p.found) continue;
            found++;
            before += p.beforeFrontRules;
            rejectedHeight += p.rejectedHeight;
            rejectedSightline += p.rejectedSightline;
            var why = FrontRuleProblem(world.Cache, p.exitCell);
            if (why == null) frontOk++;
            else if (problems.Count < 4) problems.Add("seed " + s + " X " + p.exitCell + ": " + why);
            if (OwnCorridor(world.Cache, p.frontCell) == p.corridor) corridorOk++;
            if (p.corridor) corridors++;
            if (!pickSeeds.ContainsKey(p.corridor))
            {
                pickSeeds[p.corridor] = s;
                pickPlans[p.corridor] = p;
            }
        }
        Check(found > scan / 2 && frontOk == found && corridorOk == found && pickSeeds.Count == 2,
            label + "rules 3 and 4 hold on all " + frontOk + " of " + found + " EXITs planned over " + scan + " floor seeds from " + start
            + " (N Standard; no glass on the edges round N, N + (0, -1) and N + (0, -2) but the EXIT door, none a revisit shift could glaze; they turned down "
            + rejectedHeight + " and " + rejectedSightline + " of the " + before + " cells every other rule let through" + (problems.Count > 0 ? "; " + string.Join("; ", problems) : "")
            + "); the corridor flag matches the edges on " + corridorOk + " (" + corridors + " corridors, " + (found - corridors) + " flat)");

        // 2-7. Each sign kind, built.
        foreach (var end in new[] { false, true })
        {
            if (!pickSeeds.ContainsKey(end)) { Check(false, label + "no EXIT with " + (end ? "a corridor" : "no corridor") + " in " + scan + " floor seeds"); continue; }
            SignChecks(roots, world, eye, pickSeeds[end], pickPlans[end], end, label + (end ? "end-on blade: " : "flat sign: "));
        }

        // 8. The hold never reaches into the start area: a start strip whose top row lies two cells south of N (X three
        //    rows north of it, off the strip and its ring) gets no lamp mode; the other cells of the 5 x 5 but X do.
        //    C23 round 2: then ClearStartArea gives the strip back, and the hold takes up its cells in the 5 x 5.
        var probe = pickPlans.ContainsKey(false) ? pickPlans[false] : pickPlans.ContainsKey(true) ? pickPlans[true] : default(ExitPlan);
        if (probe.found)
        {
            var pn = probe.frontCell;
            var strip = new RectInt(pn.x - 2, pn.y - 5, 5, 4);
            world.NextFloor(seed, 1, new RectInt(0, 0, 0, 0), default(GridCoord));
            var rolled = RolledModes(world, pn);
            world.NextFloor(seed, 1, strip, new GridCoord(pn.x, pn.y - 1));
            world.SetExitArea(probe.exitCell);
            var state = new Dictionary<string, int>();
            world.FloorStateForTools(state);
            var stripCells = 0;
            for (var dy = -2; dy <= 2; dy++)
            for (var dx = -2; dx <= 2; dx++)
                if (world.InStartArea(new GridCoord(pn.x + dx, pn.y + dy))) stripCells++;
            var modes = state.TryGetValue("lampModes", out var m) ? m : -1;
            Check(stripCells == 5 && modes == 25 - stripCells - 1,
                label + "the hold never reaches into the start area: with a start strip's top row 2 cells south of N " + pn + ", " + modes + " lamp modes are set (25 cells, less the strip's "
                + stripCells + " and X)");
            HoldAfterRelease(world, pn, strip, rolled, label + "beside a start strip: ");
        }

        // 9. The overlap as the game meets it (C23 round 2; 5 of the 100 floor-1 EXITs of the verification): seed 20656951's
        //    floor 1, its title strip where the game makes it (RootFor, as FrontRoomsMapVerification places it), its planned
        //    EXIT (X (81, 1), N (81, 0) there), whose 5 x 5 takes 5 cells of the strip: the hold reaches them once
        //    ClearStartArea gives the strip back.
        const int overlapSeed = 20656951;
        var cs9 = MapGrid.CellSize;
        var titleDoor = new GridCoord(Mathf.FloorToInt(FrontRooms3DGame.TitleCenterX / cs9), Mathf.FloorToInt((FrontRoomsRoomStream.RoomLength * 1.5f + cs9 * .5f) / cs9));
        var titleRows = Mathf.RoundToInt(FrontRoomsRoomStream.RoomLength * 2f / cs9);
        world.NextFloor(overlapSeed, 1, new RectInt(0, 0, 0, 0), default(GridCoord));
        // The root from before G1 (placardWest false): G1 may move the game's root for this seed, and the overlap case is
        // still a real geometry the hold must handle.
        var root = FrontRoomsExitPlanner.RootFor(world.Cache, titleDoor, 2, titleRows, placardWest: false);
        var gameStrip = new RectInt(root.doorCell.x - 2, root.doorCell.y - titleRows, 5, titleRows);
        var overlapPlan = FrontRoomsExitPlanner.ChooseExit(world.Cache, new CellRect(gameStrip.x, gameStrip.y, gameStrip.width, gameStrip.height), root.doorCell, overlapSeed,
            FrontRoomsExitPlanner.Floor1BandMin, FrontRoomsExitPlanner.Floor1BandMax);
        if (!overlapPlan.found) Check(false, label + "seed " + overlapSeed + ": the planner found no floor-1 EXIT from the title door " + root.doorCell);
        else
        {
            var on = overlapPlan.frontCell;
            var rolled9 = RolledModes(world, on);
            world.NextFloor(overlapSeed, 1, gameStrip, root.doorCell);
            world.SetExitArea(overlapPlan.exitCell);
            Check(overlapPlan.exitCell == new GridCoord(81, 1),
                label + "seed " + overlapSeed + " floor 1 as the game makes it: title door " + root.doorCell + ", strip " + gameStrip + ", X " + overlapPlan.exitCell + " (N " + on + ", "
                + overlapPlan.distance + " walking cells), the verification's X (81, 1)");
            HoldAfterRelease(world, on, gameStrip, rolled9, label + "seed " + overlapSeed + " floor 1: ");
        }
        world.NextFloor(seed, 1, new RectInt(0, 0, 0, 0), default(GridCoord));
    }

    // Each cell's own lamp mode in the 5 x 5 round N, with no start area and no exit area set (its chunk generated first).
    static Dictionary<GridCoord, ModuleLamp> RolledModes(FrontRoomsMapWorld world, GridCoord n)
    {
        var rolled = new Dictionary<GridCoord, ModuleLamp>();
        for (var dy = -FrontRoomsMapWorld.ExitLampHoldCells; dy <= FrontRoomsMapWorld.ExitLampHoldCells; dy++)
        for (var dx = -FrontRoomsMapWorld.ExitLampHoldCells; dx <= FrontRoomsMapWorld.ExitLampHoldCells; dx++)
        {
            var c = new GridCoord(n.x + dx, n.y + dy);
            world.Cache.Get(MapGrid.ChunkOf(c));
            rolled[c] = world.LampModeOf(c);
        }
        return rolled;
    }

    /// <summary>
    /// C23 round 2: the exit area set beside a start area whose cells fall in the 5 x 5 round N. While the start area
    /// stands they have no lamp mode; ClearStartArea gives them back and the hold takes them up: every cell of the 5 x 5
    /// but X then carries a lamp mode (24) and reads Steady (or Off where a module took the lamp out). Before the fix
    /// they kept their own rolled modes (the count stayed 24 less the overlap).
    /// </summary>
    static void HoldAfterRelease(FrontRoomsMapWorld world, GridCoord n, RectInt strip, Dictionary<GridCoord, ModuleLamp> rolled, string label)
    {
        var overlap = new List<GridCoord>();
        foreach (var c in rolled.Keys) if (world.InStartArea(c)) overlap.Add(c);
        var state = new Dictionary<string, int>();
        world.FloorStateForTools(state);
        var before = state.TryGetValue("lampModes", out var b) ? b : -1;
        world.ClearStartArea();
        state.Clear();
        world.FloorStateForTools(state);
        var after = state.TryGetValue("lampModes", out var a) ? a : -1;
        // LampModeOf predicts a chunk not generated yet with Auto lamps: generate them, as RolledModes did.
        foreach (var c in rolled.Keys) world.Cache.Get(MapGrid.ChunkOf(c));
        int steady = 0, rolledOther = 0;
        var modes = new List<string>();
        foreach (var c in overlap)
        {
            var mode = world.LampModeOf(c);
            modes.Add(rolled[c] + ">" + mode);
            if (mode == ModuleLamp.Steady || (mode == ModuleLamp.Off && rolled[c] == ModuleLamp.Off)) steady++;
            if (rolled[c] != ModuleLamp.Steady && rolled[c] != ModuleLamp.Off) rolledOther++;
        }
        var allSteady = rolled.Keys.All(c => c == new GridCoord(n.x, n.y + 1) || world.LampModeOf(c) == ModuleLamp.Steady || (world.LampModeOf(c) == ModuleLamp.Off && rolled[c] == ModuleLamp.Off));
        Check(overlap.Count > 0 && before == 24 - overlap.Count && after == 24 && steady == overlap.Count && allSteady && !world.HasStartArea,
            label + "the hold after the start area " + strip + " is given back: " + overlap.Count + " of N " + n + "'s 5 x 5 cells lay in it (" + string.Join(", ", overlap.Take(5))
            + "; " + rolledOther + " of them roll a mode other than Steady; own mode > now: " + string.Join(" ", modes) + "), " + before + " lamp modes set while it stood, " + after + " after ClearStartArea (24 = the 5 x 5 but X); "
            + steady + " of the " + overlap.Count + " now read Steady, the whole 5 x 5 too (" + allSteady + ")");
    }

    /// <summary>One EXIT, set before the build as the game does it: its sign, what stands round it, the lamp hold, and what the chunk's life does to them.</summary>
    static void SignChecks(List<GameObject> roots, FrontRoomsMapWorld world, Transform eye, int seed, ExitPlan plan, bool end, string label)
    {
        var x = plan.exitCell;
        var n = plan.frontCell;
        var cs = MapGrid.CellSize;
        var chunkX = MapGrid.ChunkOf(x);
        world.NextFloor(seed, 1, new RectInt(0, 0, 0, 0), default(GridCoord));
        // Each cell's own lamp mode round N before the hold, its chunk generated first (LampModeOf only predicts for one that is not).
        var own = new Dictionary<GridCoord, ModuleLamp>();
        for (var dy = -3; dy <= 3; dy++)
        for (var dx = -3; dx <= 3; dx++)
        {
            var c = new GridCoord(n.x + dx, n.y + dy);
            world.Cache.Get(MapGrid.ChunkOf(c));
            own[c] = world.LampModeOf(c);
        }
        world.SetExitArea(x);
        eye.position = world.CellCenter(n) + Vector3.up * .05f;
        world.Begin(eye, true);
        Physics.SyncTransforms();
        var signs = world.GetComponentsInChildren<FrontRoomsExitSign>(true);
        var sign = world.ExitSign;
        var chunkRoot = ChunkRoot(world, chunkX);
        if (sign == null || chunkRoot == null) { Check(false, label + "no EXIT sign over " + n + "-" + x + " (X's chunk built: " + world.IsChunkBuiltAt(x) + ")"); return; }

        // 2. One sign, in X's chunk, the kit the corridor rule asks for, locked LIT.
        SignBox(world, sign, out var lo, out var hi);
        var depth = hi.z - lo.z;
        var builtEnd = BuiltCorridor(world, n);
        var lockedLit = sign.LockLit && sign.State == FrontRoomsExitSign.Temperament.Lit && Mathf.Abs(sign.Level - 1f) < 1e-5f && !sign.NeedsTick && sign.RedLight == null
            && sign.Weights == new Vector4(1f, 1f, 0f, 0f);
        sign.SetMains(.3f);
        for (var k = 0; k < 30; k++) sign.Tick(Dt);
        var mainsIgnored = sign.State == FrontRoomsExitSign.Temperament.Lit && Mathf.Abs(sign.Level - 1f) < 1e-5f;
        sign.Configure(7u, FrontRoomsExitSign.Temperament.Loose, null, 0f);
        var dealIgnored = sign.State == FrontRoomsExitSign.Temperament.Lit && !sign.NeedsTick && Mathf.Abs(sign.Level - 1f) < 1e-5f;
        Check(signs.Length == 1 && signs[0] == sign && sign.transform.parent == chunkRoot && builtEnd == end && plan.corridor == end && world.ExitSignIsEnd == end
              && (end ? depth > .3f : depth < .1f) && sign.name.Contains("end") == end && lockedLit && mainsIgnored && dealIgnored,
            label + "X " + x + " (seed " + seed + "): one EXIT sign under the map, in X's chunk " + chunkX + ", the " + (end ? "end-on blade" : "flat sign") + " (" + sign.name + ", "
            + depth.ToString("0.000") + " m deep), as the corridor rule says (N's sides as built " + world.EffectiveEdge(n, new GridCoord(n.x - 1, n.y)) + " / "
            + world.EffectiveEdge(n, new GridCoord(n.x + 1, n.y)) + ": " + (builtEnd ? "a corridor runs past" : "no corridor") + "); LockLit " + sign.LockLit + ", " + sign.State
            + ", Level " + sign.Level.ToString("0.000") + ", no light, no tick (" + lockedLit + "); SetMains 0.3 and a LOOSE deal leave it LIT (" + mainsIgnored + ", " + dealIgnored + ")");

        // 3. Where it hangs (C23 round 2; 10_directions 1.2): on the steel frame's exit_sign_p anchor, read from the kit:
        //    centred on the opening, bottom 2.18 m (+- 0.005) just over the map's head trim, its back 0.08 m from the
        //    wall's centre line (N's face of the wall), its front into N, under N's Standard ceiling; no window built on
        //    the door's sightline.
        var line = x.y * cs;
        var face = line - ModuleUnits.WallHalf;
        var centre = (x.x + .5f) * cs;
        var ceilingUp = Physics.Raycast(world.CellCenter(n) + Vector3.up, Vector3.up, out var ceilingHit, 10f, ~0, QueryTriggerInteraction.Ignore);
        var ceiling = ceilingUp ? ceilingHit.point.y - world.transform.position.y : 0f;
        var intoN = world.CellCenter(n) - world.CellCenter(x);
        intoN.y = 0f;
        intoN.Normalize();
        var facing = Vector3.Dot(sign.transform.forward, intoN);
        var alongWall = Mathf.Abs(Vector3.Dot(sign.transform.right, world.transform.right));
        var names = new HashSet<string>(world.GetComponentsInChildren<Transform>(true).Select(t => t.name));
        var glassBuilt = 0;
        for (var k = 0; k < FrontRoomsExitPlanner.SightlineCells; k++)
        {
            var cell = new GridCoord(n.x, n.y - k);
            foreach (var s in Steps)
            {
                var other = cell + s;
                if (other == x) continue;
                if (world.EffectiveEdge(cell, other) == EdgeKind.Window || names.Contains("Window " + cell + "-" + other) || names.Contains("Window " + other + "-" + cell)) glassBuilt++;
            }
        }
        var frameInfo = FrontRoomsKitLibrary.GetInfo(FrontRoomsMapWorld.ExitSignFrameKit);
        var anchor = Vector3.zero;
        var anchorFound = frameInfo != null && frameInfo.TryAnchor(FrontRoomsMapWorld.ExitSignAnchor, out anchor);
        var anchorOk = anchorFound && Mathf.Abs(anchor.y - FrontRoomsMapWorld.ExitSignBottom) < 1e-4f && Mathf.Abs(-anchor.x - FrontRoomsMapWorld.ExitSignBack) < 1e-4f
            && Mathf.Abs(anchor.z - ModuleUnits.DoorWidth * .5f) < 1e-4f;
        var exitFrame = chunkRoot.Find("EXIT frame " + n + "-" + x);
        var headTop = float.MinValue;
        if (exitFrame != null)
            foreach (var r in exitFrame.GetComponentsInChildren<Renderer>(true)) headTop = Mathf.Max(headTop, r.bounds.max.y - world.transform.position.y);
        var back = line - hi.z;
        Check(anchorOk && Mathf.Abs(lo.y - FrontRoomsMapWorld.ExitSignBottom) <= .005f && exitFrame != null && lo.y > headTop && lo.y - headTop < .02f && hi.y < ceiling - 1e-3f
              && world.ZoneOf(n).height == ZoneHeight.Standard && Mathf.Abs(ceiling - ModuleUnits.StandardCeiling) < 1e-3f
              && Mathf.Abs((lo.x + hi.x) * .5f - centre) <= .005f && Mathf.Abs(back - FrontRoomsMapWorld.ExitSignBack) <= .001f && facing > .999f && alongWall > .999f && glassBuilt == 0,
            label + "on the steel frame's anchor (" + FrontRoomsMapWorld.ExitSignFrameKit + " " + FrontRoomsMapWorld.ExitSignAnchor + " " + (anchorFound ? anchor.ToString("F3") : "missing")
            + ": the map's 2.18 / 0.08 " + (anchorOk ? "agree" : "DISAGREE") + "): bottom " + lo.y.ToString("0.000") + " m (2.18 +- 0.005), on the map's head trim, which ends at "
            + headTop.ToString("0.000") + " m (clearance " + (lo.y - headTop).ToString("0.000") + " m), top " + hi.y.ToString("0.000") + " under N's Standard ceiling at " + ceiling.ToString("0.000")
            + ", centred on the opening (" + ((lo.x + hi.x) * .5f - centre).ToString("0.000") + " m off), its back " + back.ToString("0.000") + " m from the wall's centre line ("
            + (back - ModuleUnits.WallHalf).ToString("0.000") + " off N's face), facing into N (" + facing.ToString("0.000") + "), its "
            + (end ? "faces read along the wall" : "face across the wall") + "; " + glassBuilt + " windows built on the 9 edges of the door's sightline");

        // 3b. No shadow from the sign, EmissionGain 2.0 (10_directions 1.2, the strip fix); the EXIT frame is a renderer of
        //     its own that casts none, while an ordinary door's frame stays in its block's trim mesh and casts.
        int signRenderers = 0, signCasters = 0;
        foreach (var r in sign.GetComponentsInChildren<Renderer>(true))
        {
            signRenderers++;
            if (r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off) signCasters++;
        }
        Check(signRenderers > 0 && signCasters == 0 && Mathf.Approximately(sign.EmissionGain, FrontRoomsMapWorld.ExitSignEmissionGain) && sign.State == FrontRoomsExitSign.Temperament.Lit,
            label + "the sign casts no shadow: " + signCasters + " of its " + signRenderers + " renderers (both LODs) cast; EmissionGain " + sign.EmissionGain.ToString("0.0") + " (2.0), still LIT");
        FrameChecks(world, chunkRoot, n, x, label);

        // 4. It touches nothing and nothing hides it from N.
        var boxCentre = world.transform.TransformPoint((lo + hi) * .5f);
        var half = (hi - lo) * .5f - Vector3.one * .001f;
        var touching = Physics.OverlapBox(boxCentre, half, world.transform.rotation, ~0, QueryTriggerInteraction.Ignore).Select(c => c.name).ToList();
        var signBounds = new Bounds(boxCentre, hi - lo - Vector3.one * .002f);
        var overlapping = new List<string>();
        foreach (var r in chunkRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (r.transform.IsChildOf(sign.transform) || !r.enabled) continue;
            // The chunk's shell (walls, trims, ceilings) comes in 6 m blocks, and chunk-wide meshes: their boxes say nothing.
            if (r.transform.parent != null && r.transform.parent.name.StartsWith("Block ")) continue;
            var size = r.bounds.size;
            if (Mathf.Max(size.x, Mathf.Max(size.y, size.z)) > cs) continue;
            if (r.bounds.Intersects(signBounds)) overlapping.Add(r.name);
        }
        var outletsNear = 0;
        foreach (var set in chunkRoot.GetComponentsInChildren<FrontRoomsWallFixtureSet>(true))
            foreach (var f in set.fixtures)
                if ((chunkRoot.TransformPoint(f.position) - boxCentre).magnitude < .5f) outletsNear++;
        var eyeN = world.CellCenter(n) + Vector3.up * ModuleUnits.PlayerEye;
        var hidden = Physics.Linecast(eyeN, boxCentre, out var blockHit, ~0, QueryTriggerInteraction.Ignore);
        Check(touching.Count == 0 && overlapping.Count == 0 && outletsNear == 0 && !hidden,
            label + "it touches nothing: " + touching.Count + " colliders in its box (" + string.Join(", ", touching.Take(3)) + "), " + overlapping.Count + " props, lamps or leaves ("
            + string.Join(", ", overlapping.Take(3)) + "), " + outletsNear + " outlets within 0.5 m; seen from N's centre at eye height with nothing between ("
            + (hidden ? blockHit.collider.name : "clear") + ")");

        // 5. Nothing on X's side (10_directions 1.2: no EXIT sign inside the stair): the sign lies wholly on N's side of
        //    the wall, the stairwell holds none of its own and touches it nowhere, and from inside X (the door shut) the
        //    wall and the stairwell stand between the eye and the sign.
        var game = new GameObject("MAP INTERACTION TEST / exit sign game root") { hideFlags = HideFlags.DontSave };
        roots.Add(game);
        var stairwell = FrontRoomsStairwell.Create(world, x, game.transform);
        Physics.SyncTransforms();
        var fromMiddle = world.CellCenter(x) + Vector3.up * ModuleUnits.PlayerEye;
        var middleBlocked = Physics.Linecast(fromMiddle, boxCentre, out var middleHit, ~0, QueryTriggerInteraction.Ignore);
        var touchingStairwell = Physics.OverlapBox(boxCentre, half, world.transform.rotation, ~0, QueryTriggerInteraction.Ignore).Length;
        var stairSigns = stairwell.GetComponentsInChildren<FrontRoomsExitSign>(true).Length;
        Check(hi.z <= line - ModuleUnits.WallHalf + 1e-3f && middleBlocked && touchingStairwell == 0 && stairSigns == 0,
            label + "nothing on X's side: the sign ends " + (line - hi.z).ToString("0.000") + " m short of X's line (on N's face); the stairwell carries " + stairSigns
            + " EXIT signs and touches this one nowhere (" + touchingStairwell + "); from X's middle the way to it is shut (" + (middleBlocked ? middleHit.collider.name : "seen") + ")");
        UnityEngine.Object.DestroyImmediate(game);
        Physics.SyncTransforms();

        // 6. The exit lamp hold: every lamp within two cells of N is Steady and built so (its level 0.96-1.00 over 10 s),
        //    each one in the exit group, so the blackout still takes it; the ring beyond keeps its own modes.
        var lights = new Dictionary<GridCoord, Light>();
        foreach (var l in world.GetComponentsInChildren<Light>(true))
            if (l.name == "Light") lights[world.CellOf(l.transform.position)] = l;
        int held = 0, heldSteady = 0, wasOther = 0, skipped = 0, ring = 0, ringOwn = 0, inGroup = 0;
        var heldBuilt = new List<GridCoord>();
        foreach (var pair in own)
        {
            var c = pair.Key;
            var cheb = Mathf.Max(Mathf.Abs(c.x - n.x), Mathf.Abs(c.y - n.y));
            if (cheb > FrontRoomsMapWorld.ExitLampHoldCells)
            {
                ring++;
                if (world.LampModeOf(c) == pair.Value) ringOwn++;
                continue;
            }
            if (world.IsReserved(c) || pair.Value == ModuleLamp.Off)
            {
                if (world.LampModeOf(c) == ModuleLamp.Off) skipped++;
                continue;
            }
            held++;
            if (pair.Value != ModuleLamp.Steady) wasOther++;
            if (world.LampModeOf(c) == ModuleLamp.Steady) heldSteady++;
            if (world.LampLevel(c) == FrontRoomsMapWorld.NoLamp || !lights.TryGetValue(c, out var light)) continue;
            heldBuilt.Add(c);
            if (InExitGroup(world, x, light)) inGroup++;
        }
        float minLevel = float.MaxValue, maxLevel = float.MinValue;
        for (var k = 0; k < 600; k++)
        {
            world.TickFixturesForTools(Dt);
            foreach (var c in heldBuilt)
            {
                var v = world.LampBaseLevel(c);
                minLevel = Mathf.Min(minLevel, v);
                maxLevel = Mathf.Max(maxLevel, v);
            }
        }
        int litBefore = 0, darkAt0 = 0, backAt1 = 0;
        foreach (var c in heldBuilt) if (lights[c].enabled) litBefore++;
        world.ExitLamps = 0f;
        world.TickFixturesForTools(0f);
        foreach (var c in heldBuilt) if (!lights[c].enabled) darkAt0++;
        world.ExitLamps = 1f;
        world.TickFixturesForTools(0f);
        foreach (var c in heldBuilt) if (lights[c].enabled) backAt1++;
        Check(held > 0 && heldSteady == held && heldBuilt.Count > 0 && inGroup == heldBuilt.Count && minLevel >= .959f && maxLevel <= 1.0001f && litBefore > 0
              && darkAt0 == heldBuilt.Count && backAt1 == litBefore && ring == 24 && ringOwn == ring && skipped == 25 - held,
            label + "the exit lamp hold: all " + held + " lamps within 2 cells of N read Steady (" + heldSteady + "; " + wasOther + " were not before), " + skipped
            + " cells (X, no lamp) left alone; the " + heldBuilt.Count + " built ones run at " + minLevel.ToString("0.000") + "-" + maxLevel.ToString("0.000") + " over 10 s and all lie in the exit group ("
            + inGroup + "): ExitLamps 0 darkens " + darkAt0 + ", 1 relights " + backAt1 + " of the " + litBefore + " lit; the " + ring + " cells of the ring beyond keep their own modes (" + ringOwn + ")");

        // 6b. The crossbar on the EXIT leaf (C23 round 2).
        var barPosition = CrossbarChecks(world, n, x, label);

        // 7. Built with X's chunk: a rebuild makes a new sign in the same place, a drop takes it, the stream brings it
        //    back; NextFloor takes it and the hold with the floor. The crossbar and the EXIT frame go the same way.
        var oldSign = sign;
        var oldPosition = sign.transform.position;
        world.RebuildChunk(chunkX);
        var rebuilt = world.ExitSign;
        var rebuiltOk = oldSign == null && rebuilt != null && (rebuilt.transform.position - oldPosition).sqrMagnitude < 1e-8f && rebuilt.LockLit
            && rebuilt.State == FrontRoomsExitSign.Temperament.Lit && world.GetComponentsInChildren<FrontRoomsExitSign>(true).Length == 1
            && heldBuilt.All(c => world.LampModeOf(c) == ModuleLamp.Steady);
        var barRebuilt = world.ExitCrossbar != null && (ToMap(world, world.ExitCrossbar).MultiplyPoint3x4(Vector3.zero) - barPosition).sqrMagnitude < 1e-10f && Named(world, FrontRoomsMapWorld.ExitCrossbarName) == 1 && Named(world, "EXIT frame ") == 1;
        world.DropChunkForTools(chunkX);
        var dropped = world.ExitSign == null && world.GetComponentsInChildren<FrontRoomsExitSign>(true).Length == 0;
        var barDropped = world.ExitCrossbar == null && Named(world, FrontRoomsMapWorld.ExitCrossbarName) == 0 && Named(world, "EXIT frame ") == 0;
        world.StreamForTools(eye.position, 0f);
        var signBack = world.ExitSign;
        var backOk = signBack != null && signBack.LockLit && signBack.State == FrontRoomsExitSign.Temperament.Lit && (signBack.transform.position - oldPosition).sqrMagnitude < 1e-8f;
        var barBack = world.ExitCrossbar != null && (ToMap(world, world.ExitCrossbar).MultiplyPoint3x4(Vector3.zero) - barPosition).sqrMagnitude < 1e-10f && Named(world, FrontRoomsMapWorld.ExitCrossbarName) == 1;
        world.NextFloor(seed, 1, new RectInt(0, 0, 0, 0), default(GridCoord));
        var gone = world.ExitSign == null && world.GetComponentsInChildren<FrontRoomsExitSign>(true).Length == 0 && !world.ExitSignIsEnd;
        var barGone = world.ExitCrossbar == null && Named(world, FrontRoomsMapWorld.ExitCrossbarName) == 0 && Named(world, "EXIT frame ") == 0 && Named(world, "Leaf rig") == 0;
        foreach (var c in heldBuilt) world.Cache.Get(MapGrid.ChunkOf(c));
        var ownAgain = heldBuilt.All(c => world.LampModeOf(c) == own[c]);
        Check(rebuiltOk && dropped && backOk && gone && ownAgain,
            label + "built with X's chunk: RebuildChunk makes a new LIT-locked sign in the same place, the hold kept (" + rebuiltOk + "); a drop takes it (" + dropped
            + "), the stream brings it back (" + backOk + "); NextFloor takes it with the floor (" + gone + ") and the lamps have their own modes again (" + ownAgain + ")");
        Check(barRebuilt && barDropped && barBack && barGone,
            label + "the crossbar and the EXIT frame go with X's chunk: rebuilt in the same place (" + barRebuilt + "), dropped (" + barDropped + "), streamed back (" + barBack
            + "), gone after NextFloor with the leaf rig (" + barGone + ")");
    }

    // ---------- C23 round 3: the EXIT door for the visual dressing (ExitDoorBuilt) and its lock (ExitLocked) ----------

    // What one ExitDoorBuilt call handed over, what stood round it then, and the stand-in dressing it hung.
    sealed class ExitBuildRecord
    {
        public FrontRoomsMapWorld.Door door;
        public Transform hinge, leaf, frame, crossbar, dressLeaf, dressFrame;
        public bool locked;
        public readonly List<string> problems = new List<string>();
    }

    const string DressLeafName = "TEST dressing on the leaf", DressFrameName = "TEST dressing on the frame";

    /// <summary>
    /// C23 round 3 (agreed with the visual chat, 10_directions.md 7). A stand-in for the visual dressing listens to
    /// ExitDoorBuilt: it checks what it is handed (the door ExitDoor gives, its hinge, the unscaled leaf rig with the
    /// crossbar under it, the EXIT frame object with its one shadowless renderer, the sign standing) and hangs one object
    /// on the leaf and one on the frame, fresh on every build. Then: once per build, again after a rebuild, a drop and
    /// the return, and on floors 2 and 3; a throwing handler is logged and changes nothing. ExitLocked and its seam.
    /// </summary>
    static void ExitDoor(List<GameObject> roots)
    {
        const string label = "exit door: ";
        var profile = FrontRoomsLevelProfiles.Resolve();
        var world = World(roots, profile, -65f, "MAP INTERACTION TEST / exit door");
        var builds = new List<ExitBuildRecord>();
        Action<FrontRoomsMapWorld.Door, Transform, Transform, Transform> dresser = (door, hinge, leaf, frame) =>
        {
            var r = new ExitBuildRecord { door = door, hinge = hinge, leaf = leaf, frame = frame, locked = world.ExitLocked };
            builds.Add(r);
            if (door == null || world.ExitDoor != door || !world.IsExitDoor(door) || world.DoorBetween(door.a, door.b) != door) r.problems.Add("door not the registered EXIT");
            if (door == null || hinge == null || hinge != door.hinge) { r.problems.Add("hinge"); return; }
            if (leaf == null || leaf.name != FrontRoomsMapWorld.ExitLeafRigName || leaf.parent != hinge || leaf == door.leaf.transform || leaf.localScale != Vector3.one
                || (leaf.lossyScale - Vector3.one).sqrMagnitude > 1e-10f) r.problems.Add("leaf rig");
            int frameRenderers = 0, frameCasters = 0;
            if (frame != null)
                foreach (var mr in frame.GetComponentsInChildren<MeshRenderer>(true))
                {
                    frameRenderers++;
                    if (mr.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off) frameCasters++;
                }
            if (frame == null || frame.name != "EXIT frame " + door.a + "-" + door.b || frame.parent != hinge.parent || frame.localPosition != Vector3.zero
                || frame.localRotation != Quaternion.identity || frame.localScale != Vector3.one || frameRenderers != 1 || frameCasters != 0) r.problems.Add("frame");
            r.crossbar = leaf != null ? leaf.Find(FrontRoomsMapWorld.ExitCrossbarName) : null;
            if (FrontRoomsKitLibrary.Has(FrontRoomsMapWorld.ExitCrossbarKit) && (r.crossbar == null || r.crossbar != world.ExitCrossbar)) r.problems.Add("crossbar");
            if (world.ExitSign == null) r.problems.Add("sign");
            if (leaf == null || frame == null) return;
            // The dressing: one object on the leaf's push face, one on the frame over the head, both from the door's shut frame.
            r.dressLeaf = new GameObject(DressLeafName).transform;
            r.dressLeaf.SetParent(leaf, false);
            r.dressLeaf.localPosition = new Vector3(ModuleUnits.DoorLeafThickness * .5f, 1.6f, .5f);
            r.dressFrame = new GameObject(DressFrameName).transform;
            r.dressFrame.SetParent(frame, false);
            r.dressFrame.localPosition = hinge.localPosition + door.closed * new Vector3(ModuleUnits.WallHalf, ModuleUnits.DoorHeight + .1f, .5f);
            r.dressFrame.localRotation = door.closed;
        };
        world.ExitDoorBuilt += dresser;
        world.BuildForCapture();
        var withoutExit = builds.Count;
        var eye = world.Player;
        var seed = world.Seed;
        var start = world.CellOf(eye.position);
        var none = new CellRect(0, 0, 0, 0);

        // Floor 1: an EXIT planned from the spawn, set before the build as the game does.
        var plan = default(ExitPlan);
        var floorSeed = seed;
        for (var k = 0; k < 20 && !plan.found; k++)
        {
            floorSeed = k == 0 ? seed : FrontRoomsExitPlanner.FloorSeed(seed, 9, k);
            world.NextFloor(floorSeed, 1, new RectInt(0, 0, 0, 0), default(GridCoord));
            plan = FrontRoomsExitPlanner.ChooseExit(world.Cache, none, start, floorSeed, FrontRoomsExitPlanner.Floor1BandMin, FrontRoomsExitPlanner.Floor1BandMax);
        }
        if (!plan.found) { Check(false, label + "no floor-1 EXIT planned from " + start + " over 20 floor seeds"); return; }
        var x = plan.exitCell;
        var n = plan.frontCell;
        var chunkX = MapGrid.ChunkOf(x);
        var lockedAtSet = world.ExitLocked;
        world.SetExitArea(x);
        lockedAtSet |= world.ExitLocked;
        eye.position = world.CellCenter(n) + Vector3.up * .05f;
        world.Begin(eye, true);
        Physics.SyncTransforms();
        var first = builds.Count == withoutExit + 1 ? builds[builds.Count - 1] : null;
        var chunkRoot = ChunkRoot(world, chunkX);
        Check(withoutExit == 0 && first != null && first.problems.Count == 0 && first.door.a == n && first.door.b == x && !first.locked
              && chunkRoot != null && chunkRoot.Find("EXIT frame " + n + "-" + x) == first.frame && Named(world, DressLeafName) == 1 && Named(world, DressFrameName) == 1,
            label + "ExitDoorBuilt: " + withoutExit + " calls for the floor built with no exit area; " + (builds.Count - withoutExit) + " for floor 1's EXIT " + n + "-" + x + " (seed " + floorSeed
            + ") with the chunk registered (ExitDoor, ExitSign, ExitCrossbar answer in the handler): the door, its hinge, the leaf rig \"" + FrontRoomsMapWorld.ExitLeafRigName
            + "\" (unscaled, under the hinge, not the scaled cube leaf), the crossbar \"" + FrontRoomsMapWorld.ExitCrossbarName + "\" found under it, the frame "
            + (first != null && first.frame != null ? first.frame.name : "(none)") + " (X's chunk root, no offset, 1 renderer casting no shadow)"
            + (first != null && first.problems.Count > 0 ? "; problems: " + string.Join(", ", first.problems) : ""));

        // The leaf rig follows the swing and a jolt, and so does what hangs on it; what hangs on the frame never moves.
        var door = first != null ? first.door : world.ExitDoor;
        if (first == null || door == null || first.leaf == null || first.dressLeaf == null) { Check(false, label + "no EXIT build to swing"); return; }
        var leafCube = door.leaf.transform;
        var cubeShut = ToMap(world, leafCube);
        var rigShut = ToMap(world, first.leaf);
        var onLeafRig = cubeShut.inverse * rigShut;
        var onLeafDress = cubeShut.inverse * ToMap(world, first.dressLeaf);
        var frameDressAt = ToMap(world, first.dressFrame).MultiplyPoint3x4(Vector3.zero);
        var hingeShut = ToMap(world, first.hinge);
        var rigIsHinge = (rigShut.MultiplyPoint3x4(Vector3.zero) - hingeShut.MultiplyPoint3x4(Vector3.zero)).magnitude < 1e-6f
            && Vector3.Angle(rigShut.MultiplyVector(Vector3.forward), hingeShut.MultiplyVector(Vector3.forward)) < .01f;
        var opened = world.TryOpenDoor(n, x);
        for (var k = 0; k < 600 && door.angle < ModuleUnits.DoorSwingDegrees - .05f; k++) world.TickDoorsForTools(Dt);
        for (var k = 0; k < 120; k++) world.TickDoorsForTools(Dt);
        var openAngle = door.angle;
        float rigFollow, rigTurn, dressFollow, dressTurn;
        Carried(world, leafCube, first.leaf, onLeafRig, out rigFollow, out rigTurn);
        Carried(world, leafCube, first.dressLeaf, onLeafDress, out dressFollow, out dressTurn);
        var rigSwung = Vector3.Angle(rigShut.MultiplyVector(Vector3.forward), ToMap(world, first.leaf).MultiplyVector(Vector3.forward));
        var frameStill = (ToMap(world, first.dressFrame).MultiplyPoint3x4(Vector3.zero) - frameDressAt).magnitude;
        var leafBefore = leafCube.localPosition;
        world.JoltLeaf(door, .03f, 2f, .5f);
        for (var k = 0; k < 6; k++) world.TickDoorsForTools(Dt);
        var jolted = (leafCube.localPosition - leafBefore).magnitude;
        float joltFollow, joltTurn;
        Carried(world, leafCube, first.dressLeaf, onLeafDress, out joltFollow, out joltTurn);
        world.ResetDoorForTools(door);
        var rigHome = (ToMap(world, first.leaf).MultiplyPoint3x4(Vector3.zero) - rigShut.MultiplyPoint3x4(Vector3.zero)).magnitude;
        var crossbarOnRig = first.crossbar == null ? !FrontRoomsKitLibrary.Has(FrontRoomsMapWorld.ExitCrossbarKit) : first.crossbar.IsChildOf(first.leaf);
        Check(rigIsHinge && opened && openAngle > 90f && Mathf.Abs(rigSwung - openAngle) < .05f && rigFollow < 1e-4f && rigTurn < .05f && dressFollow < 1e-4f && dressTurn < .05f
              && frameStill < 1e-6f && jolted > 1e-3f && joltFollow < 1e-4f && joltTurn < .05f && rigHome < 1e-5f && crossbarOnRig,
            label + "the leaf follows the swing: shut, the rig is the hinge's frame (" + rigIsHinge + "); opened from N (" + opened + ") to " + openAngle.ToString("0.0") + " degrees, it turned "
            + rigSwung.ToString("0.0") + " degrees with the leaf (" + rigFollow.ToString("0.000000") + " m, " + rigTurn.ToString("0.000") + " degrees off the cube leaf), the dressing on it too ("
            + dressFollow.ToString("0.000000") + " m, " + dressTurn.ToString("0.000") + " degrees), the dressing on the frame stayed (" + frameStill.ToString("0.000000") + " m); a jolt moves the leaf "
            + jolted.ToString("0.000") + " m and the dressing with it (" + joltFollow.ToString("0.000000") + " m); shut again the rig is home (" + rigHome.ToString("0.000000") + " m); the crossbar under the rig ("
            + crossbarOnRig + ")");

        // Again after RebuildChunk: fresh transforms in the same place, the old ones and their dressing destroyed.
        var before = builds.Count;
        var old = builds[before - 1];
        world.RebuildChunk(chunkX);
        var rebuilt = builds.Count == before + 1 ? builds[before] : null;
        var rebuiltOk = rebuilt != null && rebuilt.problems.Count == 0 && rebuilt.door != old.door && old.hinge == null && old.leaf == null && old.frame == null
            && old.dressLeaf == null && old.dressFrame == null && Named(world, DressLeafName) == 1 && Named(world, DressFrameName) == 1
            && (ToMap(world, rebuilt.leaf).MultiplyPoint3x4(Vector3.zero) - rigShut.MultiplyPoint3x4(Vector3.zero)).magnitude < 1e-5f;

        // A drop takes it (no call), the stream's return builds it again (one call).
        before = builds.Count;
        old = builds[before - 1];
        world.DropChunkForTools(chunkX);
        var droppedQuiet = builds.Count == before && world.ExitDoor == null && old.leaf == null && Named(world, DressLeafName) == 0 && Named(world, DressFrameName) == 0;
        world.StreamForTools(eye.position, 0f);
        var back = builds.Count == before + 1 ? builds[before] : null;
        var backOk = back != null && back.problems.Count == 0 && back.door == world.ExitDoor && Named(world, DressLeafName) == 1;
        Check(rebuiltOk && droppedQuiet && backOk,
            label + "ExitDoorBuilt again after RebuildChunk, with fresh transforms in the same place and the old ones and their dressing destroyed (" + rebuiltOk
            + "); a drop raises nothing and takes the dressing (" + droppedQuiet + "); the stream's return raises it once more (" + backOk + ")");

        // A handler that throws: logged, and the build and the next handler go on.
        var logged = new List<string>();
        Application.LogCallback catcher = (text, stack, type) =>
        {
            if (type == LogType.Exception && text.Contains("TEST (expected)")) logged.Add(text);
        };
        Action<FrontRoomsMapWorld.Door, Transform, Transform, Transform> thrower = (d, h, l, f) =>
        {
            throw new InvalidOperationException("TEST (expected): a throwing ExitDoorBuilt handler; the map logs it and builds on");
        };
        world.ExitDoorBuilt -= dresser;
        world.ExitDoorBuilt += thrower;
        world.ExitDoorBuilt += dresser;
        Application.logMessageReceived += catcher;
        before = builds.Count;
        try { world.RebuildChunk(chunkX); }
        finally
        {
            Application.logMessageReceived -= catcher;
            world.ExitDoorBuilt -= thrower;
        }
        var guardState = new Dictionary<string, int>();
        world.FloorStateForTools(guardState);
        var afterThrow = builds.Count == before + 1 ? builds[before] : null;
        Check(logged.Count == 1 && afterThrow != null && afterThrow.problems.Count == 0 && world.IsChunkBuiltAt(x) && world.ExitDoor == afterThrow.door && world.ExitSign != null
              && guardState["failedChunks"] == 0 && Named(world, DressLeafName) == 1,
            label + "a throwing ExitDoorBuilt handler is logged (" + logged.Count + " exception) and breaks nothing: the next handler ran (" + (afterThrow != null) + "), X's chunk is built and registered with its door and sign, "
            + guardState["failedChunks"] + " failed chunks");

        // ExitLocked: false on floor 1; the seam.
        door = world.ExitDoor;
        int lockChanges = 0;
        FrontRoomsMapWorld.Door lockDoor = null;
        var lockState = false;
        world.ExitLockChanged += (d, locked) => { lockChanges++; lockDoor = d; lockState = locked; };
        var floor1Unlocked = !lockedAtSet && !world.ExitLocked && builds.All(b => !b.locked) && !world.LockedByRule(door);
        var openPrompt = world.Describe(door.leaf, out _);
        var openKind = world.Kind(door.leaf, eye.position, out _, out _);
        var set1 = world.SetExitLocked(true);
        var changesAfter1 = lockChanges;
        var set2 = world.SetExitLocked(true);
        Check(set1 && !set2 && changesAfter1 == 1 && lockChanges == 1 && lockDoor == door && lockState && world.ExitLocked,
            label + "SetExitLocked(true) raises ExitLockChanged once with the EXIT door and true (" + changesAfter1 + "), a second call raises nothing (" + lockChanges + " in all, returned " + set1 + "/" + set2 + ")");

        // Locked: the locked prompt, Kind Locked, E rattles (the rig following), TryOpenDoor false; N's zone key does not fit it.
        int lockedRaised = 0, rattledRaised = 0;
        world.DoorLocked += p => lockedRaised++;
        world.DoorRattled += (d, p) => { if (d == world.ExitDoor) rattledRaised++; };
        var lockedPrompt = world.Describe(door.leaf, out _);
        var lockedKind = world.Kind(door.leaf, eye.position, out _, out var haveKey);
        var rule = world.LockedByRule(door);
        var keyNone = !world.KeyFor(door, eye.position).HasValue;
        var use = world.Use(door.leaf);
        var rig = builds[builds.Count - 1].leaf;
        var cubeHome = door.leaf.transform.localPosition;
        var onLeafNow = ToMap(world, door.leaf.transform).inverse * ToMap(world, rig);
        float rattleShift = 0f, rattleFollow = 0f;
        for (var k = 0; k < 30; k++)
        {
            world.TickDoorsForTools(Dt);
            rattleShift = Mathf.Max(rattleShift, (door.leaf.transform.localPosition - cubeHome).magnitude);
            float m, deg;
            Carried(world, door.leaf.transform, rig, onLeafNow, out m, out deg);
            rattleFollow = Mathf.Max(rattleFollow, m);
        }
        var tryOpen = world.TryOpenDoor(n, x);
        Check(lockedPrompt != null && lockedPrompt.StartsWith("LOCKED") && lockedKind == FrontRoomsMapWorld.AimKind.Locked && !haveKey && rule && keyNone
              && use == FrontRoomsMapWorld.UseResult.Rattled && lockedRaised == 1 && rattledRaised == 1 && rattleShift > 1e-3f && rattleFollow < 1e-4f && !tryOpen && !door.open && door.angle == 0f,
            label + "locked, it is a locked map door: \"" + lockedPrompt + "\" (was \"" + openPrompt + "\"), Kind " + lockedKind + " (was " + openKind + "), LockedByRule " + rule + ", KeyFor none ("
            + keyNone + "); E answers " + use + " with DoorLocked " + lockedRaised + "x and DoorRattled " + rattledRaised + "x, the leaf jolts " + rattleShift.ToString("0.000") + " m and the rig with it ("
            + rattleFollow.ToString("0.000000") + " m off); TryOpenDoor " + tryOpen + ", still shut");
        world.GiveKeyForTools(world.ZoneOf(n).id);
        for (var k = 0; k < Mathf.CeilToInt((FrontRoomsShotTimings.Rattle.Duration + .1f) / Dt); k++) world.TickDoorsForTools(Dt);
        var keyKind = world.Kind(door.leaf, eye.position, out _, out var haveZoneKey);
        var keyFor = world.KeyFor(door, eye.position);
        var keyUse = world.Use(door.leaf);
        var keyTry = world.TryOpenDoor(n, x);
        Check(keyKind == FrontRoomsMapWorld.AimKind.Locked && !haveZoneKey && !keyFor.HasValue && keyUse == FrontRoomsMapWorld.UseResult.Rattled && !keyTry && !door.open
              && !world.IsUnlockedForTools(door) && !world.IsHeldForKey(door),
            label + "holding N's zone key changes nothing: Kind " + keyKind + " (not LockedHaveKey), KeyFor " + (keyFor.HasValue ? keyFor.Value.ToString() : "none") + ", E " + keyUse + ", TryOpenDoor " + keyTry
            + "; not unlocked, no key turn");

        // A rebuild keeps the lock (no event; the build reads it), unlocking opens it, the seal stays apart.
        var changesBefore = lockChanges;
        before = builds.Count;
        world.RebuildChunk(chunkX);
        door = world.ExitDoor;
        var keptRecord = builds.Count == before + 1 ? builds[before] : null;
        var kept = keptRecord != null && keptRecord.locked && world.ExitLocked && lockChanges == changesBefore && door != null
            && (world.Describe(door.leaf, out _) ?? "").StartsWith("LOCKED") && world.Kind(door.leaf, eye.position, out _, out _) == FrontRoomsMapWorld.AimKind.Locked;
        for (var k = 0; k < Mathf.CeilToInt((FrontRoomsShotTimings.Rattle.Duration + .1f) / Dt); k++) world.TickDoorsForTools(Dt);
        var unset = world.SetExitLocked(false);
        var unlockedEvent = lockChanges == changesBefore + 1 && lockDoor == door && !lockState && !world.ExitLocked;
        var reopenPrompt = world.Describe(door.leaf, out _);
        var reopenKind = world.Kind(door.leaf, eye.position, out _, out _);
        var reopened = world.TryOpenDoor(n, x) && door.open;
        world.ResetDoorForTools(door);
        world.SetExitLocked(true);
        var changesBeforeSeal = lockChanges;
        var sealedOk = world.SealExit() && world.ExitSealed && world.ExitLocked && lockChanges == changesBeforeSeal && world.Describe(door.leaf, out _) == null
            && world.Kind(door.leaf, eye.position, out _, out _) == FrontRoomsMapWorld.AimKind.None && world.Use(door.leaf) == FrontRoomsMapWorld.UseResult.None;
        Check(kept && unset && unlockedEvent && reopenPrompt == openPrompt && reopenKind == openKind && reopened && sealedOk,
            label + "RebuildChunk keeps the lock with no ExitLockChanged, and its ExitDoorBuilt reads it (" + kept + "); SetExitLocked(false) raises (door, false) once (" + unlockedEvent + "): \""
            + reopenPrompt + "\", Kind " + reopenKind + ", TryOpenDoor opens it (" + reopened + "); locked again, the seal still seals it and leaves the lock alone, no event (" + sealedOk + ")");

        // NextFloor resets the lock with no event for the torn-down door; floors 2 and 3: ExitDoorBuilt on their EXIT, unlocked.
        var dressedLeaf = builds[builds.Count - 1].dressLeaf;
        var changesBeforeFloor = lockChanges;
        before = builds.Count;
        var seed2 = FrontRoomsExitPlanner.FloorSeed(seed, 2, 0);
        world.NextFloor(seed2, 2, new RectInt(x.x, x.y, 1, 1), new GridCoord(x.x, x.y + 1));
        var reset = !world.ExitLocked && !world.HasExitArea && lockChanges == changesBeforeFloor && builds.Count == before && dressedLeaf == null
            && Named(world, DressLeafName) == 0 && Named(world, DressFrameName) == 0;
        var noArea = world.SetExitLocked(true);
        reset &= !noArea && !world.ExitLocked && lockChanges == changesBeforeFloor;
        Check(reset,
            label + "NextFloor resets ExitLocked to false with no ExitLockChanged (" + (lockChanges - changesBeforeFloor) + " raised) and takes the dressing; with no exit area SetExitLocked does nothing (" + noArea + ")");

        var floorsUnlocked = floor1Unlocked;
        var floorNotes = new List<string> { "floor 1 " + n + "-" + x + " unlocked " + floor1Unlocked };
        var prevX = x;
        for (var floor = 2; floor <= 3; floor++)
        {
            var fs = FrontRoomsExitPlanner.FloorSeed(seed, floor, 0);
            var startDoor = new GridCoord(prevX.x, prevX.y + 1);
            if (floor > 2) world.NextFloor(fs, floor, new RectInt(prevX.x, prevX.y, 1, 1), startDoor);
            var p = FrontRoomsExitPlanner.ChooseExit(world.Cache, new CellRect(prevX.x, prevX.y, 1, 1), startDoor, fs, FrontRoomsExitPlanner.LaterBandMin, FrontRoomsExitPlanner.LaterBandMax);
            if (!p.found) { floorsUnlocked = false; floorNotes.Add("floor " + floor + ": no EXIT from " + startDoor); break; }
            before = builds.Count;
            world.SetExitArea(p.exitCell);
            var lockedSet = world.ExitLocked;
            eye.position = world.CellCenter(p.frontCell) + Vector3.up * .05f;
            world.Begin(eye, true);
            Physics.SyncTransforms();
            var r = builds.Count == before + 1 ? builds[before] : null;
            var ok = r != null && r.problems.Count == 0 && r.door.a == p.frontCell && r.door.b == p.exitCell && !r.locked && !lockedSet && !world.ExitLocked
                && !world.LockedByRule(r.door) && Named(world, DressLeafName) == 1 && Named(world, DressFrameName) == 1;
            floorsUnlocked &= ok;
            floorNotes.Add("floor " + floor + " " + p.frontCell + "-" + p.exitCell + " (seed " + fs + "): " + (builds.Count - before) + " call, unlocked " + (r != null && !r.locked && !world.ExitLocked)
                + (r != null && r.problems.Count > 0 ? ", problems " + string.Join("/", r.problems) : ""));
            prevX = p.exitCell;
        }
        Check(floorsUnlocked && lockChanges == changesBeforeFloor,
            label + "ExitDoorBuilt on each next floor's EXIT after NextFloor, and ExitLocked false on floors 1-3 (none raised ExitLockChanged): " + string.Join("; ", floorNotes));
        world.ExitDoorBuilt -= dresser;
    }

    // ---------- A26 step 1: the start group's data (the carve the cache holds) ----------

    // The starts: door world cell (TitleCenterX's column, 2 + 4t), chunk-local row 2 for an even t and 6 for an odd t.
    // Six seeds of step 0's matrix, each at one even row and t = 1, picked for what they exercise: 20261086 t 0 and
    // 20261015 t 1 drop a window-join root under A1; 20261100 t 2 walls glass round N; the others move keys, read key
    // spots again, open repair phase C and place columns again. The last row runs the 4 x 3 footprint (H 3 kept supported).
    // StartGroupData adds one start found by a scan: the first whose carve touches a chunk beyond c and c'.
    static readonly int[] StartGroupSeeds = { 20261035, 20261086, 20261015, 20261100, 20261029, 20261056, 20261035 };
    static readonly int[] StartGroupEvenRow = { 0, 0, 0, 2, 0, 0, 0 };
    static readonly int[] StartGroupDepth = { 4, 4, 4, 4, 4, 4, 3 };

    static void StartGroupData(List<GameObject> roots)
    {
        const string label = "start group data: ";
        var profile = FrontRoomsLevelProfiles.Resolve();
        // A world that never builds: NextFloor gives it the game's cache for a seed (profile numbers and modules, tier 1),
        // and EdgeNoGenerate (private) is read from it.
        var world = World(roots, profile, 61f, "MAP INTERACTION TEST / start group data");
        var noGenerate = typeof(FrontRoomsMapWorld).GetMethod("EdgeNoGenerate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (noGenerate == null) { Check(false, label + "FrontRoomsMapWorld.EdgeNoGenerate not found"); return; }
        var doorX = Mathf.FloorToInt(FrontRooms3DGame.TitleCenterX / MapGrid.CellSize);
        const int n = MapGrid.ChunkCells;

        var plans = new List<string>();
        var plansOk = true;
        int starts = 0, retried = 0, slots = 0, borderForced = 0, borderSeedDiffers = 0, borderOverridesPlan = 0, noGenerateRead = 0;
        int zonesSeen = 0, keysMoved = 0, pinnedTallEdges = 0, pinnedTallShifting = 0, dropped = 0, touchedChunks = 0, touchedBeyond = 0;
        var overlayBad = new List<string>();
        var borderBad = new List<string>();
        var noGenerateBad = new List<string>();
        var roomsBad = new List<string>();
        var keysBad = new List<string>();
        var lampsBad = new List<string>();
        var joinBad = new List<string>();
        var rulesBad = new List<string>();
        var regetBad = new List<string>();
        var queryBad = new List<string>();
        var clearBad = new List<string>();

        var startSeeds = new List<int>();
        var startRows = new List<int>();
        var startDepths = new List<int>();
        for (var s = 0; s < StartGroupSeeds.Length; s++)
        for (var odd = 0; odd < 2; odd++)
        {
            startSeeds.Add(StartGroupSeeds[s]);
            startRows.Add(odd == 0 ? StartGroupEvenRow[s] : 1);
            startDepths.Add(StartGroupDepth[s]);
        }
        // One more start: the first (seeds from 20261001, t 0 then 1) whose carve touches a chunk beyond c and c' (a
        // moved key's home, or a repair through their outer border), so a neighbour's copies are written and read too.
        var beyond = "none in 300 seeds";
        var modules = profile.ModuleData();
        for (var seed = 20261001; seed < 20261301 && startSeeds.Count == StartGroupSeeds.Length * 2; seed++)
        for (var row = 0; row < 2; row++)
        {
            var probe = FrontRoomsExitPlanner.CacheFor(profile.generation, modules, seed, 1);
            var p = FrontRoomsStartGroup.EvaluateCarve(probe, StartGroupTemplate.StandIn(4, 1, 4), new GridCoord(doorX, 2 + 4 * row), FrontRoomsStartGroup.CanonicalStripRows);
            if (!p.found) continue;
            var why = "";
            foreach (var m in p.carve.keyMoves) if (!p.carve.IsPinned(m.zone)) why = "key of zone " + m.zone + " moved";
            foreach (var f in p.carve.forced.Values)
                if (!p.carve.IsPinned(MapGrid.ChunkOf(f.a)) || !p.carve.IsPinned(MapGrid.ChunkOf(f.b))) why = f.source + " " + f.kind + " " + f.a + "-" + f.b;
            if (why.Length == 0) continue;
            startSeeds.Add(seed);
            startRows.Add(row);
            startDepths.Add(4);
            beyond = seed + " t" + row + " (" + why + ")";
            break;
        }

        for (var s = 0; s < startSeeds.Count; s++)
        {
            var seed = startSeeds[s];
            var row = startRows[s];
            var depth = startDepths[s];
            var tag = seed + " t" + row + " H" + depth;
            world.NextFloor(seed, 1, new RectInt(0, 0, 0, 0), default(GridCoord));
            var cache = world.Cache;
            var template = StartGroupTemplate.StandIn(depth, 1, 4);
            starts++;
            var plan = FrontRoomsStartGroup.EvaluateCarve(cache, template, new GridCoord(doorX, 2 + 4 * row), FrontRoomsStartGroup.CanonicalStripRows);
            if (!plan.found) { plansOk = false; plans.Add(tag + " NO ROOT"); continue; }
            var l = plan.layout;
            var overlay = plan.carve;
            if (plan.zoneRoots > 1) retried++;
            var planOk = plan.Asserts && plan.g1 && plan.leadsOnE2 && plan.leadsOnC1 && overlay.windowJoins == 0 && overlay.repairFailures == 0;
            plansOk &= planOk;
            var carve = FrontRoomsMapCarve.From(overlay, template, l, plan.root);
            plans.Add(tag + " root " + plan.root + " (" + plan.zoneRoots + " tried) " + carve.Hash + (planOk ? "" : " PLAN PROBLEM"));
            var c = l.DoorChunk;
            var cx = l.ExitChunk;

            // What the overlay says, read before the carve is set (raw chunks): every array slot of every chunk the carve
            // touches, the rooms and columns of its clones, the zone keys; the seed's own border edges; the raw data.
            var want = new Dictionary<GridCoord, EdgeKind[]>();
            var rawPrint = new Dictionary<GridCoord, ulong>();
            var wantHasKey = new Dictionary<GridCoord, bool>();
            var wantKey = new Dictionary<GridCoord, GridCoord>();
            var wantSite = new Dictionary<GridCoord, GridCoord>();
            foreach (var coord in carve.Touched)
            {
                rawPrint[coord] = ChunkDataPrint(cache.Get(coord));
                want[coord] = OverlayEdges(cache, overlay, coord);
                wantHasKey[coord] = overlay.KeyOf(cache, coord, out var kc, out var sc);
                wantKey[coord] = kc;
                wantSite[coord] = sc;
            }
            var seedBorder = new Dictionary<long, EdgeKind>();
            foreach (var e in carve.Edges)
                if (MapGrid.ChunkOf(e.a) != MapGrid.ChunkOf(e.b)) seedBorder[MapGrid.EdgeId(e.a, e.b)] = cache.Generator.BorderEdge(e.a, e.b.x > e.a.x);
            var tallEdges = new List<GridCoord>();
            foreach (var coord in carve.Pins)
            {
                var o = MapGrid.ChunkOrigin(coord);
                for (var j = 0; j < n; j++)
                for (var i = 0; i < n; i++)
                {
                    var a = new GridCoord(o.x + i, o.y + j);
                    var tallA = cache.ZoneOf(a).height == ZoneHeight.Tall;
                    if (i < n - 1 && tallA != (cache.ZoneOf(new GridCoord(a.x + 1, a.y)).height == ZoneHeight.Tall)) { tallEdges.Add(a); tallEdges.Add(new GridCoord(a.x + 1, a.y)); }
                    if (j < n - 1 && tallA != (cache.ZoneOf(new GridCoord(a.x, a.y + 1)).height == ZoneHeight.Tall)) { tallEdges.Add(a); tallEdges.Add(new GridCoord(a.x, a.y + 1)); }
                }
            }

            cache.SetCarve(carve);

            // 1. The cache's chunks are the overlay, slot for slot (east, north and both border copies), with its rooms,
            // columns and keys: the materialised carve is the one step 0 measured.
            var bad = 0;
            foreach (var coord in carve.Touched)
            {
                var chunk = cache.Get(coord);
                var got = ChunkEdges(chunk);
                var expect = want[coord];
                for (var k = 0; k < expect.Length; k++) { slots++; if (got[k] != expect[k]) bad++; }
                if (overlay.chunks.TryGetValue(coord, out var clone))
                {
                    var same = clone.rooms.Length == chunk.rooms.Length && clone.pillar.SequenceEqual(chunk.pillar) && clone.pillarStyle.SequenceEqual(chunk.pillarStyle);
                    for (var r = 0; same && r < clone.rooms.Length; r++)
                        same = clone.rooms[r].x == chunk.rooms[r].x && clone.rooms[r].y == chunk.rooms[r].y && clone.rooms[r].w == chunk.rooms[r].w
                            && clone.rooms[r].h == chunk.rooms[r].h && ReferenceEquals(clone.ModuleOf(r), chunk.ModuleOf(r));
                    if (!same) bad++;
                }
                if (chunk.hasKey != wantHasKey[coord] || (chunk.hasKey && (chunk.keyCell != wantKey[coord] || chunk.keySiteCell != wantSite[coord]))) bad++;
            }
            if (bad > 0) overlayBad.Add(tag + ": " + bad);
            dropped += carve.RoomDrops;
            touchedChunks += carve.Touched.Count;
            foreach (var coord in carve.Touched) if (!carve.IsPinned(coord)) touchedBeyond++;

            // 2. Every forced border edge: BorderEdge returns the carve's kind (the seed's differs on borderSeedDiffers).
            foreach (var e in carve.Edges)
            {
                if (MapGrid.ChunkOf(e.a) == MapGrid.ChunkOf(e.b)) continue;
                borderForced++;
                var id = MapGrid.EdgeId(e.a, e.b);
                if (cache.Generator.BorderEdge(e.a, e.b.x > e.a.x) != e.kind) borderBad.Add(tag + " " + e.a + "-" + e.b);
                if (seedBorder[id] != e.kind) borderSeedDiffers++;
            }
            borderOverridesPlan += carve.BorderOverrides;

            // 3. EdgeNoGenerate (the world's no-generate read: BorderEdge on a border, the chunk's data inside) agrees on
            // every slot of every touched chunk.
            foreach (var coord in carve.Touched)
            {
                var o = MapGrid.ChunkOrigin(coord);
                var got = ChunkEdges(cache.Get(coord));
                for (var j = 0; j < n; j++)
                for (var i = 0; i < n; i++)
                {
                    var a = new GridCoord(o.x + i, o.y + j);
                    if (NoGenerate(noGenerate, world, a, new GridCoord(a.x + 1, a.y)) != got[MapGrid.LocalIndex(i, j)]) noGenerateBad.Add(tag + " " + a + " east");
                    if (NoGenerate(noGenerate, world, a, new GridCoord(a.x, a.y + 1)) != got[n * n + MapGrid.LocalIndex(i, j)]) noGenerateBad.Add(tag + " " + a + " north");
                    noGenerateRead += 2;
                }
                for (var k = 0; k < n; k++)
                {
                    if (NoGenerate(noGenerate, world, new GridCoord(o.x - 1, o.y + k), new GridCoord(o.x, o.y + k)) != got[2 * n * n + k]) noGenerateBad.Add(tag + " " + coord + " west " + k);
                    if (NoGenerate(noGenerate, world, new GridCoord(o.x + k, o.y - 1), new GridCoord(o.x + k, o.y)) != got[2 * n * n + n + k]) noGenerateBad.Add(tag + " " + coord + " south " + k);
                    noGenerateRead += 2;
                }
            }

            // 4. No room rect over a protected cell (station, C1..N, X); no column on X's corners.
            foreach (var coord in carve.Pins)
            {
                var chunk = cache.Get(coord);
                foreach (var rect in chunk.rooms)
                    for (var j = rect.y; j < rect.y + rect.h; j++)
                    for (var i = rect.x; i < rect.x + rect.w; i++)
                        if (l.Protected(chunk.Cell(i, j))) roomsBad.Add(tag + " room " + rect.x + "," + rect.y + " " + rect.w + "x" + rect.h + " over " + chunk.Cell(i, j));
            }
            {
                var xc = cache.Get(cx);
                int xi = l.exit.x - xc.Origin.x, xj = l.exit.y - xc.Origin.y;
                if (xc.pillar[xi + xj * (n + 1)] || xc.pillar[xi + 1 + xj * (n + 1)] || xc.pillar[xi + (xj + 1) * (n + 1)] || xc.pillar[xi + 1 + (xj + 1) * (n + 1)])
                    roomsBad.Add(tag + " column on X's corner");
            }

            // 5. No zone key in the station or X: the zones owning a station cell or X, each key in its own zone.
            var zones = new List<GridCoord>();
            for (var y = l.station.y; y < l.station.y + l.station.h; y++)
            for (var x = l.station.x; x < l.station.x + l.station.w; x++)
            {
                var id = cache.ZoneOf(new GridCoord(x, y)).id;
                if (!zones.Contains(id)) zones.Add(id);
            }
            if (!zones.Contains(cache.ZoneOf(l.exit).id)) zones.Add(cache.ZoneOf(l.exit).id);
            foreach (var zone in zones)
            {
                zonesSeen++;
                var home = cache.Get(zone);
                if (!home.hasKey) continue;
                if (l.InStation(home.keyCell) || l.InStation(home.keySiteCell) || home.keyCell == l.exit || home.keySiteCell == l.exit || cache.ZoneOf(home.keyCell).id != zone)
                    keysBad.Add(tag + " zone " + zone + " key " + home.keyCell + " site " + home.keySiteCell);
            }
            keysMoved += carve.KeyMoves;

            // 6. Lamps: the station's as the template's (never Auto), C1 Steady, C2..N and X Auto (no dropped module's lamp left).
            for (var j = 0; j < l.station.h; j++)
            for (var i = 0; i < l.station.w; i++)
            {
                var cell = new GridCoord(l.station.x + i, l.station.y + j);
                var lamp = LampOf(cache, cell);
                if (lamp != template.LampAt(i, j) || lamp == ModuleLamp.Auto) lampsBad.Add(tag + " " + cell + " " + lamp);
            }
            if (LampOf(cache, l.e1Out) != ModuleLamp.Steady) lampsBad.Add(tag + " C1 " + LampOf(cache, l.e1Out));
            for (var i = 2; i <= l.corridor; i++)
                if (LampOf(cache, l.Corridor(i)) != ModuleLamp.Auto) lampsBad.Add(tag + " C" + i + " " + LampOf(cache, l.Corridor(i)));
            if (LampOf(cache, l.exit) != ModuleLamp.Auto) lampsBad.Add(tag + " X " + LampOf(cache, l.exit));

            // 7. The repair leaves c and c' joined to the map, read with this test's own walk: while the strip stands and
            // once ClearStartArea gives it back.
            var standing = UnjoinedPieces(cache, l, carve.Pins, true);
            var back = UnjoinedPieces(cache, l, carve.Pins, false);
            if (standing.Length > 0) joinBad.Add(tag + " strip standing: " + standing);
            if (back.Length > 0) joinBad.Add(tag + " strip given back: " + back);

            // 8. C22's rules (FromStartGroup's asserts, 4.3) with the planner's own functions on the carved cache, G1, and
            // the walk D to N (ExitPlan.distance = H + k).
            var strip = l.strip;
            var steps = FrontRoomsExitPlanner.WalkStepsTo(cache, strip, l.door, l.front, l.exit, FrontRoomsExitPlanner.WidestBandMax);
            var sight = FrontRoomsExitPlanner.Sightline(cache, l.exit);
            if (!FrontRoomsExitPlanner.ExitCellFits(cache, strip, l.exit) || !FrontRoomsExitPlanner.IsDeadEnd(cache, l.exit)
                || !FrontRoomsExitPlanner.KeepsChunkWhole(cache, strip, l.exit, l.front) || !FrontRoomsExitPlanner.FrontStandard(cache, l.front)
                || sight != FrontRoomsExitPlanner.SightlineView.Clear || FrontRoomsExitPlanner.CorridorPastDoor(cache, strip, l.exit)
                || !FrontRoomsExitPlanner.StartDoorWestOpen(cache, l.door) || steps + 1 != depth + l.corridor || cache.Edge(l.front, l.exit) != EdgeKind.Open)
                rulesBad.Add(tag + " fits " + FrontRoomsExitPlanner.ExitCellFits(cache, strip, l.exit) + " dead " + FrontRoomsExitPlanner.IsDeadEnd(cache, l.exit)
                    + " whole " + FrontRoomsExitPlanner.KeepsChunkWhole(cache, strip, l.exit, l.front) + " sight " + sight + " walk " + (steps + 1));

            // 9. Queries: IsPinned c and c' only (5 x 5 round c), SkinOf the footprint only (and its ring), ArchOverride on
            // E1 and E2 only, with the template's spans centred; MayBecomeWindow never on an edge inside a pinned chunk.
            for (var dy = -2; dy <= 2; dy++)
            for (var dx = -2; dx <= 2; dx++)
            {
                var coord = new GridCoord(c.x + dx, c.y + dy);
                if (cache.IsPinned(coord) != (coord == c || coord == cx)) queryBad.Add(tag + " IsPinned " + coord);
            }
            for (var y = l.station.y - 1; y <= l.station.y + l.station.h; y++)
            for (var x = l.station.x - 1; x <= l.station.x + l.station.w; x++)
            {
                var cell = new GridCoord(x, y);
                if ((cache.SkinOf(cell) == MapSkin.Station) != l.InStation(cell)) queryBad.Add(tag + " SkinOf " + cell);
            }
            var e1 = cache.ArchOverride(l.e1Out, l.e1In, out var e1w, out var e1c) && Mathf.Approximately(e1w, template.e1Width) && Mathf.Approximately(e1c, MapGrid.CellSize * .5f);
            var e2 = cache.ArchOverride(l.e2In, l.e2Out, out var e2w, out var e2c) && Mathf.Approximately(e2w, template.e2Width) && Mathf.Approximately(e2c, MapGrid.CellSize * .5f);
            var other = cache.ArchOverride(l.door, new GridCoord(l.door.x, l.door.y - 1), out _, out _) || cache.ArchOverride(l.Corridor(1), l.Corridor(2), out _, out _);
            if (!e1 || !e2 || other || cache.Edge(l.e1In, l.e1Out) != EdgeKind.Arch || cache.Edge(l.e2Out, l.e2In) != EdgeKind.Arch) queryBad.Add(tag + " ArchOverride");
            for (var k = 0; k < tallEdges.Count; k += 2)
            {
                pinnedTallEdges++;
                if (FrontRoomsExitPlanner.MayBecomeWindow(cache, tallEdges[k], tallEdges[k + 1], cx)) queryBad.Add(tag + " MayBecomeWindow " + tallEdges[k] + "-" + tallEdges[k + 1]);
            }

            // 10. A re-Get is identical: every touched chunk dropped, then got again in the other order.
            var carved = new Dictionary<GridCoord, ulong>();
            foreach (var coord in carve.Touched) carved[coord] = ChunkDataPrint(cache.Get(coord));
            foreach (var coord in carve.Touched) cache.Drop(coord);
            for (var k = carve.Touched.Count - 1; k >= 0; k--)
                if (ChunkDataPrint(cache.Get(carve.Touched[k])) != carved[carve.Touched[k]]) regetBad.Add(tag + " " + carve.Touched[k]);

            // 11. ClearCarve: no override, nothing pinned, every touched chunk back to its raw data.
            cache.ClearCarve();
            if (cache.Carve != null || cache.Generator.BorderOverride != null || cache.IsPinned(c) || cache.IsPinned(cx) || cache.SkinOf(l.door) != MapSkin.Zone
                || cache.ArchOverride(l.e1In, l.e1Out, out _, out _))
                clearBad.Add(tag + " state");
            foreach (var coord in carve.Touched)
                if (ChunkDataPrint(cache.Get(coord)) != rawPrint[coord]) clearBad.Add(tag + " " + coord);
            for (var k = 0; k < tallEdges.Count; k += 2)
                if (MapGrid.ChunkOf(tallEdges[k]) != cx && FrontRoomsExitPlanner.MayBecomeWindow(cache, tallEdges[k], tallEdges[k + 1], cx)) pinnedTallShifting++;
        }

        Check(plansOk && starts == StartGroupSeeds.Length * 2 + 1,
            label + starts + " starts planned (A1 on: " + retried + " took a later zone root), C22 asserts, G1 and leads-on on the overlay, no window join; the start touching a chunk beyond c and c': "
            + beyond + ": " + string.Join("; ", plans));
        Check(overlayBad.Count == 0 && slots > 0,
            label + "the carve the cache holds is step 0's overlay in all " + slots + " edge slots (east, north, west and south copies) of the " + touchedChunks + " chunks the carves touch ("
            + touchedBeyond + " beyond c and c'), with its rooms, columns and keys (" + dropped + " rooms dropped)" + (overlayBad.Count > 0 ? "; differ: " + string.Join(", ", overlayBad) : ""));
        Check(borderBad.Count == 0 && borderForced > 0 && borderSeedDiffers == borderOverridesPlan,
            label + "BorderEdge returns the carve's kind on all " + borderForced + " forced border edges (" + borderSeedDiffers + " differ from the seed, the plan counted " + borderOverridesPlan + ")"
            + (borderBad.Count > 0 ? "; wrong: " + string.Join(", ", borderBad.Take(8)) : ""));
        Check(noGenerateBad.Count == 0 && noGenerateRead > 0,
            label + "EdgeNoGenerate agrees with the chunks on all " + noGenerateRead + " edge slots of the touched chunks" + (noGenerateBad.Count > 0 ? "; differs: " + string.Join(", ", noGenerateBad.Take(8)) : ""));
        Check(roomsBad.Count == 0,
            label + "no room rect over a protected cell (station, C1..N, X) and no column on X's corners" + (roomsBad.Count > 0 ? ": " + string.Join(", ", roomsBad.Take(8)) : ""));
        Check(keysBad.Count == 0 && zonesSeen > 0,
            label + "no zone key in the station or X, each in its own zone (" + zonesSeen + " zones owning a station cell or X, " + keysMoved + " keys moved)" + (keysBad.Count > 0 ? ": " + string.Join(", ", keysBad.Take(8)) : ""));
        Check(lampsBad.Count == 0,
            label + "lamps written: the station's as the template's (never Auto), C1 Steady, C2..N and X Auto" + (lampsBad.Count > 0 ? ": " + string.Join(", ", lampsBad.Take(8)) : ""));
        Check(joinBad.Count == 0,
            label + "the repair leaves c and c' joined to the map (every piece with a cell that is not Tall walks out; a Tall-only one has glass out), strip standing and given back"
            + (joinBad.Count > 0 ? ": " + string.Join("; ", joinBad.Take(8)) : ""));
        Check(rulesBad.Count == 0,
            label + "on the carved cache, ExitCellFits, IsDeadEnd, KeepsChunkWhole, FrontStandard, Sightline Clear, no corridor past the door, N-X Open, G1 (StartDoorWestOpen) and the walk D to X = H + k"
            + (rulesBad.Count > 0 ? ": " + string.Join("; ", rulesBad.Take(8)) : ""));
        Check(regetBad.Count == 0,
            label + "a re-Get of every touched chunk (dropped, got again in the other order) is identical" + (regetBad.Count > 0 ? ": " + string.Join(", ", regetBad.Take(8)) : ""));
        Check(queryBad.Count == 0,
            label + "IsPinned c and c' only, SkinOf Station on the footprint only, ArchOverride on E1 and E2 only (their spans, centred), MayBecomeWindow false on all "
            + pinnedTallEdges + " Tall/lower edges inside pinned chunks" + (queryBad.Count > 0 ? ": " + string.Join(", ", queryBad.Take(8)) : ""));
        Check(clearBad.Count == 0,
            label + "ClearCarve: no override, nothing pinned, every touched chunk rebuilds as its raw data (" + pinnedTallShifting + " of those Tall/lower edges outside X's chunk may glaze again)"
            + (clearBad.Count > 0 ? ": " + string.Join(", ", clearBad.Take(8)) : ""));
    }

    // ---------- A26 step 2: the planner, the station module, FromStartGroup, the validator's floor 1 ----------

    // Seed 20656951 (ExitSign's section 9: its floor-1 X is (81, 1) without a station) with the stand-in station (H 4,
    // k 4): X chunk-local (5, 2) from an even door row 2 + 4t (t 0) and (5, 6) from an odd one (t 1), plan 4.1. Recorded
    // from the verification's start group (2026-10-29, the same seed and rows: root (-64, -64), D (149, 66) and (149, 70)).
    const int StationPinSeed = 20656951;
    static readonly GridCoord StationPinEven = new GridCoord(149, 74), StationPinOdd = new GridCoord(149, 78);

    static void StartGroupPlanner(List<GameObject> roots)
    {
        const string label = "start group plan: ";
        var profile = FrontRoomsLevelProfiles.Resolve();
        var modules = profile.ModuleData();
        var doorX = Mathf.FloorToInt(FrontRooms3DGame.TitleCenterX / MapGrid.CellSize);
        FrontRoomsMapCache CacheFor(int seed) => FrontRoomsExitPlanner.CacheFor(profile.generation, modules, seed, 1);

        // 1. The marker kinds are appended (old bytes keep their values); the profile asset holds no station (the
        //    landing's switch is off) and its random pool none.
        Check((int)ModuleMarkerKind.KeySpot == 0 && (int)ModuleMarkerKind.RelayEntry == 1 && (int)ModuleMarkerKind.StationBeacon == 2 && (int)ModuleMarkerKind.Placard == 3
            && (int)ModuleMarkerKind.StationEntrance == 4,
            label + "marker kinds KeySpot 0, RelayEntry 1, then the appended StationBeacon 2, Placard 3, StationEntrance 4");
        var poolStations = modules.Count(m => m.IsStation);
        Check(profile.stationModule == null && profile.StationData() == null && poolStations == 0,
            label + "the profile " + AssetDatabase.GetAssetPath(profile) + " has no station module (stationModule empty, StationData null) and none of its " + modules.Count + " pool modules is a station");

        // 2. The stand-in station (4 x 4 and 4 x 3) passes every module rule and the station contract, and From reads it
        //    back as the stand-in template; a room without the station entrance is no station (From refuses it).
        var notes = new List<string>();
        var standInOk = true;
        foreach (var depth in new[] { 4, 3 })
        {
            var m = FrontRoomsMapVerification.StandInStation(depth);
            var errors = new List<string>();
            m.Clone().Validate(errors, new List<string>(), null);
            var t = StartGroupTemplate.From(m, null, out var why);
            var stand = StartGroupTemplate.StandIn(depth, 1, StartGroupTemplate.CorridorCells);
            var same = t != null && t.depth == depth && t.corridor == 4 && t.entrance == 2 && t.e2Row == 1 && t.e1Width == stand.e1Width && t.e2Width == stand.e2Width;
            for (var j = 0; same && j < depth; j++)
            for (var i = 0; i < StartGroupTemplate.Width; i++)
            {
                same &= t.LampAt(i, j) == stand.LampAt(i, j);
                if (i < StartGroupTemplate.Width - 1) same &= t.InnerEast(i, j) == EdgeKind.Open;
                if (j < depth - 1) same &= t.InnerNorth(i, j) == EdgeKind.Open;
            }
            standInOk &= m.IsStation && errors.Count == 0 && same;
            notes.Add("4 x " + depth + ": " + (errors.Count == 0 ? "valid" : string.Join(" | ", errors)) + ", From " + (t != null ? t.Name : "refused (" + why + ")"));
        }
        var plain = FrontRoomsMapVerification.StandInStation(4);
        plain.markers = plain.markers.Where(mk => mk.kind != ModuleMarkerKind.StationEntrance).ToArray();
        var plainErrors = new List<string>();
        plain.Clone().Validate(plainErrors, new List<string>(), null);
        var plainTemplate = StartGroupTemplate.From(plain, null, out var plainWhy);
        Check(standInOk && !plain.IsStation && plainTemplate == null && !plainErrors.Any(e => e.StartsWith("Station: ")),
            label + "the stand-in station is valid and From gives the stand-in template (H, k 4, door column 2, E2 row 1, spans, lamps, inner edges Open): " + string.Join("; ", notes)
            + "; without its entrance marker it is no station (no station rule asked) and From refuses it (" + plainWhy + ")");

        // 3. Each break of the station contract is an error of ValidateStation, and From refuses the module. The CRT TV's
        //    footprint carries its screen anchor as KitFootprint gives it (Kit_CRTTV's sidecar: 0, 0.284, 0.245); a stand-in
        //    screen kit with its screen near its top (0.45 m) breaks R3's height rule under the ceiling rule's 2.85 m.
        Func<string, float[]> footprint = kit => kit == "Kit_CRTTV" ? new[] { -.31f, -.25f, .31f, .25f, .5f, 0f, .284f, .245f }
            : kit == "Kit_CRTTV_High" ? new[] { -.31f, -.25f, .31f, .25f, .5f, 0f, .45f, .245f }
            : kit == "Kit_Tall" ? new[] { -.3f, -.3f, .3f, .3f, 1.8f } : null;
        var screen = new ModuleProp { kit = "Kit_CRTTV", x = 9.35f, z = 11.6f, y = 2.3f, yaw = 180f };
        // R3's breaks (the last four): checked to fail on R3's own rule.
        const int r3Cases = 4;
        string[] caseNames =
        {
            "5 cells wide", "E1 off the axis", "E2 at row 2", "a south opening", "an east opening", "the start door's west inner edge walled (G1)",
            "the entrance column walled", "an Auto lamp", "one Steady lamp on the south row", "a Relay entry", "two key spots", "no placard",
            "the entrance at column 1", "weight 1", "turned", "Auto columns", "Auto fill", "Office", "a screen in the axis column", "a screen hung at 2.10 m",
            "a 1.8 m prop on the axis",
            "R3: a screen centre 2.75 m up", "R3: the beacon 2.80 m up", "R3: a 1.8 m prop in the push-in, 1.0 m in front of the screen",
            "R3: the TV and its beacon at x 9.9 m, the riser block in the push-in",
        };
        Action<RoomModuleData>[] breaks =
        {
            m => { m.width = 5; m.Normalize(); },
            m => { m.north[2] = ModuleEdge.Wall; m.north[1] = ModuleEdge.Arch; },
            m => { m.west[1] = ModuleEdge.Wall; m.west[2] = ModuleEdge.Arch; },
            m => m.south[0] = ModuleEdge.Arch,
            m => m.east[1] = ModuleEdge.Open,
            m => m.innerEast[1] = ModuleEdge.Wall,
            m => m.innerNorth[2] = ModuleEdge.Wall,
            m => m.lamps[5] = ModuleLamp.Auto,
            m => m.lamps[1] = ModuleLamp.Dim,
            m => m.markers = m.markers.Concat(new[] { new ModuleMarker { kind = ModuleMarkerKind.RelayEntry, x = 4.5f, z = 4.5f, tag = "", host = "" } }).ToArray(),
            m => m.markers = m.markers.Concat(new[] { new ModuleMarker { kind = ModuleMarkerKind.KeySpot, x = 3f, z = 3f, tag = "", host = "" } }).ToArray(),
            m => m.markers = m.markers.Where(mk => mk.kind != ModuleMarkerKind.Placard).ToArray(),
            m => m.markers = m.markers.Select(mk => mk.kind == ModuleMarkerKind.StationEntrance ? new ModuleMarker { kind = mk.kind, x = 4.5f, z = mk.z, tag = "", host = "" } : mk).ToArray(),
            m => m.weight = 1f,
            m => m.allowRotate = true,
            m => m.columns = ModuleColumns.Auto,
            m => m.fill = ModuleFill.Auto,
            m => m.theme = ZoneTheme.Office,
            m => m.props = new[] { new ModuleProp { kit = screen.kit, x = 8.9f, z = screen.z, y = screen.y, yaw = screen.yaw } },
            m => m.props = new[] { new ModuleProp { kit = screen.kit, x = screen.x, z = screen.z, y = 2.1f, yaw = screen.yaw } },
            m => m.props = new[] { new ModuleProp { kit = "Kit_Tall", x = 7.5f, z = 6f, y = 0f, yaw = 0f } },
            m => m.props = new[] { new ModuleProp { kit = "Kit_CRTTV_High", x = screen.x, z = screen.z, y = screen.y, yaw = screen.yaw } },
            m => m.markers = m.markers.Select(mk => mk.kind == ModuleMarkerKind.StationBeacon ? new ModuleMarker { kind = mk.kind, x = mk.x, z = mk.z, y = 2.8f, yaw = mk.yaw, tag = "", host = "" } : mk).ToArray(),
            m => m.props = new[] { screen, new ModuleProp { kit = "Kit_Tall", x = screen.x, z = 10.6f, y = 0f, yaw = 0f } },
            m =>
            {
                m.props = new[] { new ModuleProp { kit = screen.kit, x = 9.9f, z = screen.z, y = screen.y, yaw = screen.yaw } };
                m.markers = m.markers.Select(mk => mk.kind == ModuleMarkerKind.StationBeacon ? new ModuleMarker { kind = mk.kind, x = 9.9f, z = mk.z, y = mk.y, yaw = mk.yaw, tag = "", host = "" } : mk).ToArray();
            },
        };
        var refused = 0;
        var missed = new List<string>();
        var r3Notes = new List<string>();
        var r3Ok = true;
        for (var k = 0; k < breaks.Length; k++)
        {
            var m = FrontRoomsMapVerification.StandInStation(4);
            breaks[k](m);
            var errors = new List<string>();
            m.Clone().Validate(errors, new List<string>(), footprint);
            var t = StartGroupTemplate.From(m, footprint, out var why);
            if (t == null && errors.Any(e => e.StartsWith("Station: "))) refused++;
            else missed.Add(caseNames[k] + " (" + (t == null ? why : "From accepted it") + ")");
            if (k < breaks.Length - r3Cases) continue;
            var r3 = errors.Where(e => e.StartsWith("Station: ") && e.Contains("R3")).ToList();
            r3Ok &= r3.Count > 0;
            r3Notes.Add(caseNames[k] + ": " + (r3.Count > 0 ? string.Join(" | ", r3) : "NO R3 error (" + string.Join(" | ", errors) + ")"));
        }
        var hung = FrontRoomsMapVerification.StandInStation(4);
        hung.props = new[] { screen };
        var hungErrors = new List<string>();
        hung.Clone().Validate(hungErrors, new List<string>(), footprint);
        Check(refused == breaks.Length && missed.Count == 0 && hungErrors.Count == 0 && StartGroupTemplate.From(hung, footprint, out _) != null,
            label + refused + " of " + breaks.Length + " breaks of the station contract refused (an error of ValidateStation, From null): " + string.Join(", ", caseNames)
            + "; the screen at x 9.35 m, 2.3 m up passes" + (missed.Count > 0 ? "; NOT refused: " + string.Join("; ", missed) : "") + (hungErrors.Count > 0 ? "; screen: " + string.Join(" | ", hungErrors) : ""));
        // R3 (the playing TV): the stand-in's TV (screen centre 2.58 m, its push-in x 8.85-9.85 m, clear of the riser at x 10.30 m)
        // passes with a no-collider prop in its push-in, at both depths; each R3 break fails on R3's own rule.
        var r3Pass = new List<string>();
        foreach (var depth in new[] { 4, 3 })
        {
            var m = FrontRoomsMapVerification.StandInStation(depth);
            var top = m.DepthMetres;
            m.props = new[] { new ModuleProp { kit = screen.kit, x = screen.x, z = top - .4f, y = screen.y, yaw = screen.yaw },
                new ModuleProp { kit = "Kit_Tall", x = screen.x, z = top - 1.4f, y = 0f, yaw = 0f, noCollider = true } };
            var errors = new List<string>();
            m.Clone().Validate(errors, new List<string>(), footprint);
            r3Ok &= errors.Count == 0;
            r3Pass.Add("4 x " + depth + ": " + (errors.Count == 0 ? "valid" : string.Join(" | ", errors)));
        }
        Check(r3Ok, label + "R3 (screen centre <= " + RoomModuleData.StationScreenTop.ToString("0.00") + " m, the push-in " + RoomModuleData.StationPushInHalf.ToString("0.00") + " m either side out to "
            + RoomModuleData.StationPushInDepth.ToString("0.00") + " m clear of colliders and the riser block): the stand-in TV with a no-collider prop in its push-in passes (" + string.Join("; ", r3Pass)
            + "); " + string.Join("; ", r3Notes));

        // 4. Plan on the pin seed at both parities with the stand-in: the carve is on the cache, c and c' pinned, X straight
        //    north of E1 at chunk-local (5, 2) and (5, 6), the EXIT FromStartGroup's (band 8, one candidate, the flat sign,
        //    a dead end), G1, no warning; the pins recorded.
        var template = StartGroupTemplate.From(FrontRoomsMapVerification.StandInStation(4), null, out _);
        var pinNotes = new List<string>();
        var pinsOk = template != null;
        for (var t = 0; t < 2 && template != null; t++)
        {
            var cache = CacheFor(StationPinSeed);
            var plan = FrontRoomsStartGroup.Plan(cache, template, new GridCoord(doorX, 2 + 4 * t));
            var x = plan.exit.exitCell;
            var origin = MapGrid.ChunkOrigin(MapGrid.ChunkOf(x));
            var local = new GridCoord(x.x - origin.x, x.y - origin.y);
            var e = plan.exit;
            var l = plan.layout;
            var ok = plan.Ready && string.IsNullOrEmpty(plan.warning) && local == new GridCoord(5, t == 0 ? 2 : 6) && x == (t == 0 ? StationPinEven : StationPinOdd)
                && x == new GridCoord(l.door.x, l.door.y + 8) && e.frontCell == new GridCoord(x.x, x.y - 1) && e.distance == 8 && e.bandMin == 8 && e.bandMax == 8 && e.candidates == 1
                && !e.widened && !e.throughGlass && !e.corridor && e.deadEnd && cache.Carve == plan.mapCarve && cache.IsPinned(l.DoorChunk) && cache.IsPinned(l.ExitChunk)
                && FrontRoomsExitPlanner.StartDoorWestOpen(cache, l.door);
            pinsOk &= ok;
            pinNotes.Add("t " + t + ": root " + plan.root + " (ring " + plan.ring + ", " + plan.zoneRoots + " tried), D " + l.door + ", X " + x + " chunk-local " + local + ", walk " + e.distance
                + ", " + (plan.mapCarve != null ? plan.mapCarve.Hash : "no carve") + (ok ? "" : " WRONG" + (plan.error != null ? " (" + plan.error + ")" : "")));
        }
        Check(pinsOk,
            label + "seed " + StationPinSeed + " with the stand-in: the carve on the cache, X 4 cells north of E1 at chunk-local (5, 2) and (5, 6), the pins " + StationPinEven + " and " + StationPinOdd
            + ", EXIT band 8-8, one candidate, the flat sign, a dead end, G1, no warning: " + string.Join("; ", pinNotes));

        // 5. FromStartGroup: refused on a cache with no carve; on the carved cache, the plan's EXIT. Plan again on the same
        //    cache, and on a fresh one that generated two far chunks first: the same root, hash and X.
        if (template != null)
        {
            var door = new GridCoord(doorX, 6);
            var cache = CacheFor(StationPinSeed);
            var plan = FrontRoomsStartGroup.Plan(cache, template, door);
            var carve = cache.Carve;
            cache.ClearCarve();
            var raw = FrontRoomsExitPlanner.FromStartGroup(cache, plan.layout, out var rawWhy);
            cache.SetCarve(carve);
            var carved = FrontRoomsExitPlanner.FromStartGroup(cache, plan.layout, out var carvedWhy);
            var again = FrontRoomsStartGroup.Plan(cache, template, door);
            var fresh = CacheFor(StationPinSeed);
            var far = MapGrid.ChunkOf(plan.exit.exitCell);
            fresh.Get(new GridCoord(far.x + 2, far.y - 3));
            fresh.Get(new GridCoord(far.x - 1, far.y + 2));
            var freshPlan = FrontRoomsStartGroup.Plan(fresh, template, door);
            Check(plan.Ready && !raw.found && rawWhy != null && carved.found && carvedWhy == null && carved.exitCell == plan.exit.exitCell && carved.distance == plan.exit.distance
                && again.Ready && again.mapCarve.Hash == plan.mapCarve.Hash && again.root == plan.root && freshPlan.Ready && freshPlan.mapCarve.Hash == plan.mapCarve.Hash
                && freshPlan.root == plan.root && freshPlan.exit.exitCell == plan.exit.exitCell,
                label + "FromStartGroup refuses a cache with no carve (" + rawWhy + ") and gives the plan's EXIT on the carved one; Plan again on the same cache and on a fresh one (two far chunks first): hash "
                + (plan.mapCarve != null ? plan.mapCarve.Hash : "-") + " / " + (again.mapCarve != null ? again.mapCarve.Hash : "-") + " / " + (freshPlan.mapCarve != null ? freshPlan.mapCarve.Hash : "-"));

            // 6. No plan, so today's start (4.11): no template, a template the rules refuse; either leaves the cache with no
            //    carve (an earlier one cleared). The 4 x 3 footprint: X chunk-local (5, 1), walk 7. A1 through Plan: seed
            //    20261086 t 0 drops a window-join root and takes a later one, with no warning.
            var none = FrontRoomsStartGroup.Plan(cache, null, door);
            var noneCleared = cache.Carve == null;
            FrontRoomsStartGroup.Plan(cache, template, door);
            var deep = StartGroupTemplate.StandIn(5, 1, 4);
            var refusedPlan = FrontRoomsStartGroup.Plan(cache, deep, door);
            var shallow = StartGroupTemplate.From(FrontRoomsMapVerification.StandInStation(3), null, out _);
            var h3 = shallow != null ? FrontRoomsStartGroup.Plan(CacheFor(StationPinSeed), shallow, new GridCoord(doorX, 2)) : null;
            var h3x = h3 != null ? h3.exit.exitCell : default(GridCoord);
            var h3Origin = MapGrid.ChunkOrigin(MapGrid.ChunkOf(h3x));
            var a1 = FrontRoomsStartGroup.Plan(CacheFor(20261086), template, new GridCoord(doorX, 2));
            Check(!none.Ready && none.error != null && noneCleared && !refusedPlan.Ready && refusedPlan.error != null && cache.Carve == null
                && h3 != null && h3.Ready && h3x.x - h3Origin.x == 5 && h3x.y - h3Origin.y == 1 && h3.exit.distance == 7
                && a1.Ready && a1.zoneRoots > 1 && string.IsNullOrEmpty(a1.warning) && a1.mapCarve.WindowJoins == 0,
                label + "no template (" + none.error + ") or a refused one (" + refusedPlan.error + "): no plan and no carve left on the cache; 4 x 3: X " + h3x + " chunk-local ("
                + (h3x.x - h3Origin.x) + ", " + (h3x.y - h3Origin.y) + "), walk " + (h3 != null ? h3.exit.distance : -1) + "; A1: seed 20261086 t 0 on zone root " + a1.zoneRoots
                + (a1.Ready ? ", " + a1.mapCarve.WindowJoins + " window joins" : " (" + a1.error + ")") + (string.IsNullOrEmpty(a1.warning) ? "" : ", warned: " + a1.warning));

            // 7. The validator's floor 1 with the station (ValidateExits, as the verification's start group runs it): seed
            //    20656951 from the autopilot's door row, floors 1-3 pass.
            var settings = profile.generation.Clone();
            settings.seed = StationPinSeed;
            var run = FrontRoomsMapValidator.ValidateExits(settings, 3, door, FrontRoomsStartGroup.StripHalfCells, FrontRoomsStartGroup.CanonicalStripRows, modules, template, out var runPlan);
            Check(run.passed && runPlan != null && runPlan.Ready && run.floors.Count == 3 && run.floors.All(f => f.exit.found),
                label + "the validator's floor 1 with the station (CheckExit with the band 8, CheckStartGroup, a fresh-cache plan, the shift round X), then floors 2 and 3 from its X: seed "
                + StationPinSeed + " " + (run.passed ? "PASS" : "FAIL: " + string.Join(" | ", run.errors)) + ", X " + string.Join(", ", run.floors.Select(f => f.exit.exitCell.ToString()).ToArray()));
        }
    }

    // ---------- A26 step 3: the station in the built world (MapWorld's station, FrontRoomsStationSet) ----------

    /// <summary>
    /// A26 step 3 (plan 4.4, 4.7, 9; 6 row 3). The pin seed's floor 1 is made as the game will make it (NextFloor to the
    /// seed, Plan on the map's cache, the title strip, SetStation with the stand-in station and props, SetExitArea, Begin,
    /// the chunks streamed in) at both door parities, the station skin given test materials so it can be told from
    /// Level 0's, and the built world is read back against the data: the queries and FloorHasStation; (d) the hold;
    /// (a) the shell; (b) the skin; (c) the facade corners; (e) the pins; (g) the riser block and the Relay; (f)
    /// ClearStartArea; and ClearStation.
    /// </summary>
    static void StartGroupWorld(List<GameObject> roots)
    {
        var profile = FrontRoomsLevelProfiles.Resolve();
        var template = StartGroupTemplate.From(FrontRoomsMapVerification.StandInStation(4), null, out var why);
        if (template == null) { Check(false, "start group world: the stand-in station is refused: " + why); return; }
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        var wallSkin = new Material(lit) { name = "MIT station wall", hideFlags = HideFlags.DontSave };
        var floorSkin = new Material(lit) { name = "MIT station floor", hideFlags = HideFlags.DontSave };
        try
        {
            for (var t = 0; t < 2; t++) StartGroupWorldAt(roots, profile, template, StationSetModule(), wallSkin, floorSkin, t);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(wallSkin);
            UnityEngine.Object.DestroyImmediate(floorSkin);
        }
    }

    // The stand-in station (4 x 4) with props for the set piece, in metres from its south-west corner: a chair, a filing
    // cabinet, the TV hung at 2.3 m, a wall clock, and two the set must leave out: a 1.8 m bookcase on the door axis (B8)
    // and a crate in the start door's sweep.
    static RoomModuleData StationSetModule()
    {
        var m = FrontRoomsMapVerification.StandInStation(4);
        var top = m.DepthMetres;
        m.props = new[]
        {
            new ModuleProp { kit = "Kit_LadderChair", x = 4.5f, z = 4.5f, y = 0f, yaw = 180f },
            new ModuleProp { kit = "Kit_FilingCabinet", x = 11.5f, z = 5f, y = 0f, yaw = 270f },
            new ModuleProp { kit = "Kit_CRTTV", x = 9.35f, z = top - .4f, y = 2.3f, yaw = 180f },
            new ModuleProp { kit = "Kit_WallClock", x = 3f, z = top - ModuleUnits.WallHalf, y = 2.2f, yaw = 180f },
            new ModuleProp { kit = "Kit_Bookcase", x = 7.5f, z = 6f, y = 0f, yaw = 0f },
            new ModuleProp { kit = "Kit_Crate", x = 7.5f, z = .7f, y = 0f, yaw = 0f },
        };
        return m;
    }

    static void StartGroupWorldAt(List<GameObject> roots, FrontRoomsLevelProfile profile, StartGroupTemplate template, RoomModuleData module,
        Material wallSkin, Material floorSkin, int t)
    {
        var label = "start group world, t " + t + ": ";
        const float cs = MapGrid.CellSize;
        const System.Reflection.BindingFlags Hidden = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var doorX = Mathf.FloorToInt(FrontRooms3DGame.TitleCenterX / MapGrid.CellSize);
        var world = World(roots, profile, 67f + 2f * t, "MAP INTERACTION TEST / start group world t" + t);
        world.StationSkinForTools(wallSkin, floorSkin);
        // Edit mode runs no Awake: BuildForCapture makes the map's materials and its eye (as every map test starts).
        world.BuildForCapture();
        var eye = world.Player;
        world.NextFloor(StationPinSeed, 1, new RectInt(0, 0, 0, 0), default(GridCoord));
        var noStationYet = !world.FloorHasStation && !world.HasStation && world.Station == null && world.StationReady && world.StationExitWalk.Count == 0;
        var plan = FrontRoomsStartGroup.Plan(world.Cache, template, new GridCoord(doorX, 2 + 4 * t));
        if (!plan.Ready) { Check(false, label + "no plan on the map's cache: " + plan.error); return; }
        var l = plan.layout;
        var s = l.station;
        var c = l.DoorChunk;
        var c2 = l.ExitChunk;
        var strip = new RectInt(l.door.x - FrontRoomsStartGroup.StripHalfCells, l.door.y - 8, FrontRoomsStartGroup.StripHalfCells * 2 + 1, 8);
        world.SetStartArea(strip, l.door);
        var shells = new List<GridCoord>();
        int shellRenderers = 0, shellForeign = 0;
        world.StationShellBuilt += (coord, list) =>
        {
            shells.Add(coord);
            shellRenderers += list.Count;
            shellForeign += list.Count(r => r.sharedMaterial != wallSkin && r.sharedMaterial != floorSkin);
        };
        var set = world.SetStation(plan, module);
        world.SetExitArea(plan.exit.exitCell);
        eye.position = world.CellCenter(l.door) + Vector3.up * .05f;
        world.Begin(eye, false);
        world.StreamForTools(eye.position, 0f);
        Physics.SyncTransforms();
        var station = world.Station;
        if (!set || station == null) { Check(false, label + "SetStation refused the plan (" + plan.mapCarve.Hash + ")"); return; }

        // Room metres (from the station's south-west corner) to world space, and an edge's points (across > 0 into hi).
        float x0 = s.x * cs, z0 = s.y * cs, x1 = (s.x + s.w) * cs, z1 = (s.y + s.h) * cs;
        Vector3 Map(float x, float y, float z) => world.transform.TransformPoint(new Vector3(x, y, z));
        Vector3 Local(Vector3 p) => world.transform.InverseTransformPoint(p);
        Vector3 EdgePoint(GridCoord lo, GridCoord hi, float along, float across, float h) =>
            hi.x > lo.x ? Map((lo.x + 1) * cs + across, h, lo.y * cs + along) : Map(lo.x * cs + along, h, (lo.y + 1) * cs + across);
        bool ShellHit(Vector3 from, Vector3 to)
        {
            var d = to - from;
            foreach (var hit in Physics.RaycastAll(from, d.normalized, d.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (hit.collider is MeshCollider && hit.collider.gameObject.name == "Collision" && world.IsArchitecture(hit.collider)) return true;
            return false;
        }

        // 1. The queries, FloorHasStation, the keep-out rect, the Relay entries and StationShellBuilt.
        var placard = world.StationAnchor(ModuleMarkerKind.Placard, out var placardAt, out _, out var placardCell);
        var keySpot = world.StationAnchor(ModuleMarkerKind.KeySpot, out var keyAt, out _, out var keyCell);
        var walk = world.StationExitWalk;
        var keepOut = (RectInt)typeof(FrontRoomsMapWorld).GetMethod("FixtureKeepOut", Hidden).Invoke(world, new object[] { c });
        var expectedKeepOut = new RectInt(strip.xMin, strip.yMin, strip.xMax - strip.xMin, l.exit.y + 1 - strip.yMin);
        var entries = new List<ValueTuple<Vector3, string>>();
        world.RelayEntries(entries);
        var entriesInStation = entries.Count(e => world.InStation(world.CellOf(e.Item1)));
        // The chunks holding station cells: c alone at t 0 (rows 2-5), c and c' at t 1 (rows 6-9).
        var stationChunks = new HashSet<GridCoord> { MapGrid.ChunkOf(l.door), MapGrid.ChunkOf(l.e1In) };
        var distances = new[]
        {
            world.StationPathDistance(l.door, 3), world.StationPathDistance(l.e1Out, 3), world.StationPathDistance(l.Corridor(2), 3), world.StationPathDistance(l.Corridor(3), 3),
            world.StationPathDistance(l.front, 3), world.StationPathDistance(l.e2Out, 3),
        };
        Check(noStationYet && world.FloorHasStation && world.HasStation && world.StationRect.Equals(new RectInt(s.x, s.y, 4, 4)) && world.InStation(l.door) && !world.InStation(l.e1Out)
            && world.StationModule == module && walk.Count == 8 && walk[0] == l.door && walk[walk.Count - 1] == l.front && walk[4] == l.e1Out
            && placard && placardCell == new GridCoord(s.x + 1, s.y + 3) && Mathf.Abs(Local(placardAt).y - 1.56f) < 1e-3f
            && keySpot && keyCell == new GridCoord(s.x + 3, s.y + 3) && world.StationBeaconCell == new GridCoord(s.x + 3, s.y + 3)
            && distances[0] == 0 && distances[1] == 1 && distances[2] == 2 && distances[3] == 3 && distances[4] == 4 && distances[5] == 1
            && keepOut.Equals(expectedKeepOut) && entriesInStation == 0 && stationChunks.All(shells.Contains) && shells.All(stationChunks.Contains) && shellRenderers > 0 && shellForeign == 0,
            label + "seed " + StationPinSeed + ", D " + l.door + ", station " + world.StationRect + ": FloorHasStation false before SetStation and true after; InStation, the walk D..N ("
            + walk.Count + " cells), the placard " + placardCell + " at 1.56 m, the key spot " + keyCell + ", the beacon cell " + world.StationBeaconCell + "; StationPathDistance D / C1 / C2 / C3 / N / E2o (cap 3) "
            + string.Join(" / ", distances.Select(d => d.ToString()).ToArray()) + "; FixtureKeepOut " + keepOut + " (strip, station, corridor and X: " + expectedKeepOut + "); " + entries.Count
            + " Relay entries, " + entriesInStation + " in the station; StationShellBuilt for " + string.Join(", ", shells.Distinct().Select(x => x.ToString()).ToArray()) + " with " + shellRenderers
            + " renderers, " + shellForeign + " not in the station's materials");

        // 2. (d) The hold: with the chunks built and the rooms furnished, ReadyAround waits for the station's props, which
        //    the map's own step (Update's StepStation, 3 ms) places a few a frame; the axis bookcase and the crate in the
        //    door's sweep are left out.
        var waiting = station.PropsWaiting;
        var readyBefore = world.ReadyAround(world.BuildRadius);
        var stepStation = typeof(FrontRoomsMapWorld).GetMethod("StepStation", Hidden);
        var frames = 0;
        while (!world.StationReady && frames < 60) { stepStation.Invoke(world, null); frames++; }
        var readyAfter = world.ReadyAround(world.BuildRadius);
        var propsRoot = station.transform.Find("Station props");
        var kits = propsRoot != null ? propsRoot.Cast<Transform>().Select(p => p.name).ToList() : new List<string>();
        Check(waiting == 6 && !readyBefore && world.StationReady && readyAfter && frames >= 1 && station.PropsPlaced == 4 && station.PropsLeftOutCount == 2 && kits.Count == 4
            && !kits.Any(k => k.Contains("Bookcase") || k.Contains("Crate")) && station.DoorSweep.Equals(new Rect(l.door.x * cs, z0, cs, ModuleUnits.DoorClearDepth + ModuleUnits.WallHalf))
            && Mathf.Abs(station.AxisStrip.center.x - (l.door.x + .5f) * cs) < 1e-4f && Mathf.Abs(station.E1Clear.width - template.e1Width) < 1e-4f && Mathf.Abs(station.E2Clear.height - template.e2Width) < 1e-4f,
            label + "(d) ReadyAround " + readyBefore + " with " + waiting + " props waiting, " + world.StationReady + " / " + readyAfter + " after " + frames + " StepStation frames; "
            + station.PropsPlaced + " placed (" + string.Join(", ", kits.ToArray()) + "), " + station.PropsLeftOutCount + " left out (the 1.8 m bookcase on the axis, the crate in the door's sweep)");

        // 3. (a) The built shell is the data: across every template edge (not a reserved cell's), 27 rays at 1.0 m meet the
        //    chunk's shell collider exactly where the data has a wall; E1 and E2 are measured to 5 mm.
        int walls = 0, opens = 0, arches = 0, shellRays = 0;
        var shellBad = new List<string>();
        var spans = new List<string>();
        foreach (var e in world.Cache.Carve.Edges)
        {
            if (!e.isProtected || world.IsReserved(e.a) || world.IsReserved(e.b)) continue;
            if (world.EffectiveEdge(e.a, e.b) != e.kind) { shellBad.Add(e.a + "-" + e.b + " builds " + world.EffectiveEdge(e.a, e.b) + ", data " + e.kind); continue; }
            if (e.kind == EdgeKind.Arch)
            {
                arches++;
                int first = -1, last = -1;
                for (var k = 0; k <= 520; k++)
                {
                    shellRays++;
                    var along = .2f + k * .005f;
                    if (ShellHit(EdgePoint(e.a, e.b, along, -.5f, 1f), EdgePoint(e.a, e.b, along, .5f, 1f))) continue;
                    if (first < 0) first = k;
                    last = k;
                }
                var width = first < 0 ? 0f : (last - first + 1) * .005f;
                var centre = first < 0 ? 0f : .2f + (first + last) * .5f * .005f;
                var isE1 = e.b == l.e1Out;
                var want = isE1 ? template.e1Width : template.e2Width;
                var underTop = !ShellHit(EdgePoint(e.a, e.b, 1.5f, -.5f, 2.1f), EdgePoint(e.a, e.b, 1.5f, .5f, 2.1f));
                var header = ShellHit(EdgePoint(e.a, e.b, 1.5f, -.5f, 2.3f), EdgePoint(e.a, e.b, 1.5f, .5f, 2.3f));
                spans.Add((isE1 ? "E1 " : "E2 ") + width.ToString("0.000") + " m centred at " + centre.ToString("0.000"));
                var range = isE1 ? width >= StartGroupTemplate.E1MinWidth && width <= StartGroupTemplate.E1MaxWidth : width >= StartGroupTemplate.E2MinWidth && width <= StartGroupTemplate.E2MaxWidth;
                if (!range || Mathf.Abs(width - want) > .011f || Mathf.Abs(centre - cs * .5f) > .011f || !underTop || !header)
                    shellBad.Add((isE1 ? "E1 " : "E2 ") + width + " m at " + centre + " (open at 2.1 m " + underTop + ", header at 2.3 m " + header + ")");
                continue;
            }
            var blocked = 0;
            for (var k = 0; k < 27; k++)
            {
                shellRays++;
                var along = .2f + k * .1f;
                if (ShellHit(EdgePoint(e.a, e.b, along, -.5f, 1f), EdgePoint(e.a, e.b, along, .5f, 1f))) blocked++;
            }
            if (e.kind == EdgeKind.Wall) walls++; else opens++;
            if ((e.kind == EdgeKind.Wall && blocked != 27) || (e.kind == EdgeKind.Open && blocked != 0)) shellBad.Add(e.a + "-" + e.b + " " + e.kind + ": " + blocked + "/27 rays blocked");
        }
        Check(shellBad.Count == 0 && arches == 2 && walls > 0 && opens > 0,
            label + "(a) the shell is the data: " + walls + " walls, " + opens + " open edges, " + arches + " arches (" + string.Join("; ", spans.ToArray()) + ") over " + shellRays
            + " rays; colliders on the walls only" + (shellBad.Count > 0 ? "; WRONG: " + string.Join("; ", shellBad.Take(6).ToArray()) : ""));

        // 4. (b) The skin, with a collider laid on every shell renderer of c and c' and on the set's panels (test only):
        //    every station wall faces the room in the station's material and the maze in its zone's paper; the liner covers
        //    the south side but the stream door; at E1 and E2 the jambs split (the seams), nowhere else. No paper vertex
        //    in the room, no station wall vertex outside its walls, no station floor outside its cells.
        var skins = new Dictionary<Collider, Material>();
        var added = new List<Component>();
        void Lay()
        {
            foreach (var a in added) if (a != null) UnityEngine.Object.DestroyImmediate(a);
            added.Clear();
            skins.Clear();
            foreach (Transform chunkRoot in world.transform)
            {
                if (!chunkRoot.name.StartsWith("Chunk " + c + " ") && !chunkRoot.name.StartsWith("Chunk " + c2 + " ")) continue;
                foreach (var r in chunkRoot.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (r.transform.parent == null || !r.transform.parent.name.StartsWith("Block ")) continue;
                    var mc = r.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = r.GetComponent<MeshFilter>().sharedMesh;
                    added.Add(mc);
                    skins[mc] = r.sharedMaterial;
                }
            }
            foreach (var r in station.Renderers)
            {
                if (r == null || !r.gameObject.activeInHierarchy || r.transform.IsChildOf(propsRoot)) continue;
                var box = r.GetComponent<BoxCollider>();
                if (box == null) { box = r.gameObject.AddComponent<BoxCollider>(); added.Add(box); }
                skins[box] = r.sharedMaterial;
            }
            Physics.SyncTransforms();
        }
        Material Skin(Vector3 from, Vector3 dir, float reach, out float distance)
        {
            distance = float.PositiveInfinity;
            Material found = null;
            foreach (var hit in Physics.RaycastAll(from, dir, reach, ~0, QueryTriggerInteraction.Ignore))
                if (skins.TryGetValue(hit.collider, out var m) && hit.distance < distance) { distance = hit.distance; found = m; }
            return found;
        }
        Lay();
        int skinRays = 0, seamRays = 0;
        var skinBad = new List<string>();
        var doorWest = (l.door.x + .5f) * cs - FrontRoomsStationSet.StreamDoorWidth * .5f;
        var doorEast = doorWest + FrontRoomsStationSet.StreamDoorWidth;
        for (var j = s.y; j < s.y + s.h; j++)
        for (var i = s.x; i < s.x + s.w; i++)
        {
            var inside = new GridCoord(i, j);
            foreach (var step in Steps)
            {
                var outside = inside + step;
                if (l.InStation(outside)) continue;
                var insideIsLo = step.x > 0 || step.y > 0;
                var lo = insideIsLo ? inside : outside;
                var hi = insideIsLo ? outside : inside;
                var into = insideIsLo ? 1f : -1f;
                var built = world.EffectiveEdge(lo, hi);
                var south = step.y < 0;
                world.Cache.ArchOverride(lo, hi, out var aw, out var ac);
                foreach (var along in new[] { .4f, .75f, 1.5f, 2.25f, 2.6f })
                foreach (var h in new[] { .5f, 1.5f, 2.5f })
                {
                    skinRays++;
                    var from = EdgePoint(lo, hi, along, -.6f * into, h);
                    var dir = (EdgePoint(lo, hi, along, .6f * into, h) - from).normalized;
                    var m = Skin(from, dir, .8f, out var d);
                    string bad = null;
                    var inOpening = built == EdgeKind.Arch && h < ModuleUnits.ArchTop && Mathf.Abs(along - ac) < aw * .5f;
                    var x = lo.x * cs + along;
                    var inDoor = south && x > doorWest && x < doorEast && h < FrontRoomsStationSet.StreamHeaderBottom;
                    if (inOpening || inDoor) { if (m != null && d < .6f + ModuleUnits.WallHalf) bad = "an opening is not open"; }
                    else if (south) { if (m != wallSkin || Mathf.Abs(d - (.6f - FrontRoomsStationSet.FacadeFace - FrontRoomsStationSet.LinerProud)) > .002f) bad = "the liner"; }
                    else if (m != wallSkin || Mathf.Abs(d - (.6f - ModuleUnits.WallHalf)) > .002f) bad = "the room's face";
                    if (bad != null) skinBad.Add(bad + " at " + lo + "-" + hi + " " + along + " m, " + h + " m: " + (m != null ? m.name : "nothing") + " at " + d.ToString("0.000"));
                    if (south || inOpening) continue;
                    // From the maze: its zone's paper.
                    skinRays++;
                    var paper = world.WallPaperOf(outside);
                    var mo = Skin(EdgePoint(lo, hi, along, .6f * into, h), -dir, .8f, out var od);
                    if (mo != paper || Mathf.Abs(od - (.6f - ModuleUnits.WallHalf)) > .002f)
                        skinBad.Add("the maze's face at " + lo + "-" + hi + " " + along + " m, " + h + " m: " + (mo != null ? mo.name : "nothing") + " at " + od.ToString("0.000") + ", not " + (paper != null ? paper.name : "null"));
                }
                if (built != EdgeKind.Arch) continue;
                // The seams: in the arch, the jambs' inner halves are the station's, the outer halves the zone's paper.
                foreach (var side in new[] { -1f, 1f })
                foreach (var h in new[] { .5f, 1.5f })
                foreach (var half in new[] { -1f, 1f })
                {
                    seamRays++;
                    var from = EdgePoint(lo, hi, ac, half * .04f * into, h);
                    var dir = (EdgePoint(lo, hi, ac + side, half * .04f * into, h) - from).normalized;
                    var m = Skin(from, dir, 1.2f, out var d);
                    var want = half < 0f ? wallSkin : world.WallPaperOf(outside);
                    if (m != want || Mathf.Abs(d - aw * .5f) > .002f) skinBad.Add("the jamb at " + lo + "-" + hi + " side " + side + " half " + half + ": " + (m != null ? m.name : "nothing") + " at " + d.ToString("0.000"));
                }
            }
        }
        int wallVerts = 0, floorVerts = 0, paperInRoom = 0, wallOut = 0, floorOut = 0;
        var paperMaterials = new HashSet<Material>();
        for (var j = s.y - 1; j <= s.y + s.h; j++)
        for (var i = s.x - 1; i <= s.x + s.w; i++)
            if (!l.InStation(new GridCoord(i, j))) paperMaterials.Add(world.WallPaperOf(new GridCoord(i, j)));
        foreach (Transform chunkRoot in world.transform)
        {
            if (!chunkRoot.name.StartsWith("Chunk ")) continue;
            foreach (var r in chunkRoot.GetComponentsInChildren<MeshRenderer>(true))
            {
                var m = r.sharedMaterial;
                if (m != wallSkin && m != floorSkin && !paperMaterials.Contains(m)) continue;
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                foreach (var v in mf.sharedMesh.vertices)
                {
                    var p = Local(r.transform.TransformPoint(v));
                    const float Eps = .001f;
                    var inRoom = p.x > x0 + ModuleUnits.WallHalf + Eps && p.x < x1 - ModuleUnits.WallHalf - Eps && p.z > z0 + ModuleUnits.WallHalf + Eps && p.z < z1 - ModuleUnits.WallHalf - Eps;
                    if (m == wallSkin)
                    {
                        wallVerts++;
                        if (p.x < x0 - ModuleUnits.WallHalf - Eps || p.x > x1 + ModuleUnits.WallHalf + Eps || p.z < z0 - ModuleUnits.WallHalf - Eps || p.z > z1 + ModuleUnits.WallHalf + Eps) wallOut++;
                    }
                    else if (m == floorSkin)
                    {
                        floorVerts++;
                        if (p.x < x0 - Eps || p.x > x1 + Eps || p.z < z0 - Eps || p.z > z1 + Eps) floorOut++;
                    }
                    else if (inRoom) paperInRoom++;
                }
            }
        }
        Check(skinBad.Count == 0 && wallVerts > 0 && floorVerts > 0 && paperInRoom == 0 && wallOut == 0 && floorOut == 0,
            label + "(b) the skin: " + skinRays + " rays meet the station's material on every face of the room (the liner on the south side, " + (doorEast - doorWest).ToString("0.0")
            + " m of stream door left open) and the zone's paper on the maze side; " + seamRays + " rays in E1 and E2 split at the jambs (the only seams); vertices: " + wallVerts
            + " station wall (" + wallOut + " outside its walls), " + floorVerts + " station floor (" + floorOut + " outside its cells), " + paperInRoom + " paper in the room"
            + (skinBad.Count > 0 ? "; WRONG: " + string.Join("; ", skinBad.Take(6).ToArray()) : ""));

        // 5. (c) The facade corners at x = 3dx-6 and 3dx+6 (the C22 notch precedent): 80 rays. Along the door line the side
        //    walls run down to it (a ray 3-40 cm in front of the facade meets the wall's face), and toward the facade the
        //    liner reaches each wall's face (a ray 1 cm from the face meets the liner, 4 mm proud of the facade).
        var cornerRays = 0;
        var cornerBad = new List<string>();
        foreach (var corner in new[] { x0, x1 })
        {
            var inward = corner == x0 ? 1f : -1f;
            foreach (var h in new[] { .3f, 1.2f, 2f, 2.6f })
            {
                foreach (var dz in new[] { .03f, .06f, .1f, .2f, .4f })
                {
                    cornerRays++;
                    var m = Skin(Map(corner + inward * .9f, h, z0 + dz), world.transform.TransformDirection(Vector3.right * -inward), 1.2f, out var d);
                    if (m != wallSkin || Mathf.Abs(d - (.9f - ModuleUnits.WallHalf)) > .002f) cornerBad.Add("side wall at " + corner + ", " + dz + " m off the facade, " + h + " m: " + (m != null ? m.name : "nothing") + " at " + d.ToString("0.000"));
                }
                foreach (var dx in new[] { .09f, .1f, .15f, .3f, .6f })
                {
                    cornerRays++;
                    var m = Skin(Map(corner + inward * dx, h, z0 + .9f), world.transform.TransformDirection(Vector3.back), 1.2f, out var d);
                    if (m != wallSkin || Mathf.Abs(d - (.9f - FrontRoomsStationSet.FacadeFace - FrontRoomsStationSet.LinerProud)) > .002f)
                        cornerBad.Add("liner at " + corner + " + " + (inward * dx) + ", " + h + " m: " + (m != null ? m.name : "nothing") + " at " + d.ToString("0.000"));
                }
            }
        }
        Check(cornerRays == 80 && cornerBad.Count == 0,
            label + "(c) the facade corners at x " + x0 + " and " + x1 + " (map metres): " + cornerRays + " rays, the side walls reach the door line and the liner reaches each wall's face"
            + (cornerBad.Count > 0 ? "; WRONG: " + string.Join("; ", cornerBad.Take(6).ToArray()) : ""));
        foreach (var a in added) if (a != null) UnityEngine.Object.DestroyImmediate(a);
        added.Clear();

        // 6. (e) The pins: c, c' and a control chunk dropped, then streamed back past the shift delay (31 s): c and c' keep
        //    revision 0 and the carve; the control chunk shifts. The set hides while neither c nor c' stands.
        var control = new GridCoord(c.x - 1, c.y);
        var controlBuilt = world.IsChunkBuiltAt(MapGrid.ChunkOrigin(control));
        var revControl = world.Cache.Get(control).revision;
        var revs = world.Cache.Get(c).revision + world.Cache.Get(c2).revision;
        var shiftedBefore = world.ShiftedChunks;
        world.DropChunkForTools(c);
        world.DropChunkForTools(c2);
        world.DropChunkForTools(control);
        var hidden = !station.Visible && station.Renderers.All(r => r == null || !r.enabled);
        world.StreamForTools(eye.position, profile.shiftAfterSeconds + 1f);
        Physics.SyncTransforms();
        var carved = world.Cache.Carve.Edges.All(e => world.Cache.Edge(e.a, e.b) == e.kind);
        Check(controlBuilt && revs == 0 && world.Cache.Get(c).revision == 0 && world.Cache.Get(c2).revision == 0 && world.Cache.Get(control).revision != revControl
            && world.ShiftedChunks == shiftedBefore + 1 && carved && hidden && station.Visible && world.IsChunkBuiltAt(MapGrid.ChunkOrigin(c)),
            label + "(e) dropped for " + (profile.shiftAfterSeconds + 1f) + " s: c " + c + " and c' " + c2 + " come back at revision " + world.Cache.Get(c).revision + " / " + world.Cache.Get(c2).revision
            + " with every forced edge, the control chunk " + control + " shifted (" + revControl + " to " + world.Cache.Get(control).revision + "); the set hidden while neither stood (" + hidden + ") and shown again");

        // 7. (g) The riser chase: in the north-east cell, floor to ceiling, its faces 1.3 m and 0.9 m from the cell's west and
        //    south crossing lines, architecture, the key spot outside it. A blind Relay sent from beside its west face to a
        //    noise beside its south face (the straight line runs through it) walks round it, never into it; it cannot ghost
        //    through it (the furniture-blind clearance is blocked), and would, were it furniture.
        var riser = station.RiserBlock;
        var rb = riser.bounds;
        var rlo = Local(rb.min);
        var rhi = Local(rb.max);
        var keyLocal = Local(keyAt);
        var shape = Mathf.Abs(rlo.x - (x1 - FrontRoomsStationSet.RiserWidth)) < 1e-3f && Mathf.Abs(rhi.x - x1) < 1e-3f && Mathf.Abs(rlo.z - (z1 - FrontRoomsStationSet.RiserDepth)) < 1e-3f
            && Mathf.Abs(rhi.z - z1) < 1e-3f && Mathf.Abs(rlo.y) < 1e-3f && rhi.y >= ModuleUnits.StandardCeiling && rlo.x - (x1 - cs) >= .9f - 1e-3f && rlo.z - (z1 - cs) >= .9f - 1e-3f
            && (keyLocal.x < rlo.x || keyLocal.z < rlo.z);
        var player = new GameObject("MIT station player");
        player.transform.SetParent(world.transform, false);
        player.transform.position = Map(x0 + 1.5f, 0f, z0 + 1.5f);
        var body = player.AddComponent<CapsuleCollider>();
        body.height = ModuleUnits.PlayerHeight;
        body.radius = ModuleUnits.PlayerRadius;
        body.center = Vector3.up * (ModuleUnits.PlayerHeight * .5f);
        Physics.SyncTransforms();
        var hunter = new FrontRoomsMapHunter(world, new FrontRoomsHunterTuning { sightRange = 0f }, body, null, 7);
        var from3 = Map(rlo.x - .6f, 0f, z1 - 1f);
        var to3 = Map(x1 - .6f, 0f, rlo.z - .6f);
        var pathClear = typeof(FrontRoomsMapHunter).GetMethod("PathClear", Hidden);
        var ghostBlocked = !(bool)pathClear.Invoke(hunter, new object[] { from3, to3, false });
        world.UnregisterArchitecture(riser);
        var ghostAsFurniture = (bool)pathClear.Invoke(hunter, new object[] { from3, to3, false });
        world.RegisterArchitecture(riser);
        hunter.DebugPlace(from3);
        hunter.Noise(to3, 1000f);
        var feet = player.transform.position;
        var closest = float.PositiveInfinity;
        var arrived = -1f;
        for (var tt = 0f; tt < 20f && arrived < 0f; tt += Dt)
        {
            hunter.Tick(Dt, feet, feet + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
            world.TickDoorsForTools(Dt);
            Physics.SyncTransforms();
            var p = Local(hunter.Position);
            var dx = Mathf.Max(rlo.x - p.x, 0f, p.x - rhi.x);
            var dz = Mathf.Max(rlo.z - p.z, 0f, p.z - rhi.z);
            closest = Mathf.Min(closest, Mathf.Sqrt(dx * dx + dz * dz));
            if (Vector3.Distance(Local(hunter.Position), Local(to3)) < .5f) arrived = tt;
        }
        UnityEngine.Object.DestroyImmediate(player);
        Check(shape && world.IsArchitecture(riser) && ghostBlocked && ghostAsFurniture && arrived >= 0f && closest >= ModuleUnits.RelayRadius - .05f,
            label + "(g) the riser block " + rlo.ToString("F2") + "-" + rhi.ToString("F2") + " (faces " + (rlo.x - (x1 - cs)).ToString("0.00") + " / " + (rlo.z - (z1 - cs)).ToString("0.00")
            + " m from the cell's crossing lines, key spot outside) is architecture; the Relay cannot ghost through it (" + ghostBlocked + "; as furniture it could: " + ghostAsFurniture
            + "), and walks round it to the noise in " + (arrived >= 0f ? arrived.ToString("0.00") + " s" : "no time (not arrived)") + ", its body at least " + closest.ToString("0.00") + " m from the block");

        // 8. (f) ClearStartArea, then D's chunk rebuilt: the station's south edges are data walls (rays from inside meet the
        //    shell across the whole south side, the station's material on D's wall), the liner is gone and the static
        //    entrance stands on D's wall; the plate's anchor moves onto the wall's face.
        world.ClearStartArea();
        world.RebuildChunk(c);
        Physics.SyncTransforms();
        var southBlocked = 0;
        for (var i = s.x; i < s.x + s.w; i++)
            foreach (var along in new[] { .4f, 1f, 1.5f, 2f, 2.6f })
                if (ShellHit(EdgePoint(new GridCoord(i, s.y - 1), new GridCoord(i, s.y), along, .6f, 1.2f), EdgePoint(new GridCoord(i, s.y - 1), new GridCoord(i, s.y), along, -.2f, 1.2f))) southBlocked++;
        var dWall = 0;
        foreach (Transform chunkRoot in world.transform)
        {
            if (!chunkRoot.name.StartsWith("Chunk " + c + " ")) continue;
            foreach (var r in chunkRoot.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.sharedMaterial != wallSkin) continue;
                foreach (var v in r.GetComponent<MeshFilter>().sharedMesh.vertices)
                {
                    var p = Local(r.transform.TransformPoint(v));
                    if (Mathf.Abs(p.z - (z0 + ModuleUnits.WallHalf)) < 1e-3f && p.x > l.door.x * cs - 1e-3f && p.x < (l.door.x + 1) * cs + 1e-3f) dWall++;
                }
            }
        }
        station.PlateAnchor(out var plateAt, out _);
        var plate = Local(plateAt);
        var entranceShown = station.Entrance.activeSelf && station.Entrance.GetComponentsInChildren<Renderer>().Length == 5 && station.Entrance.GetComponentsInChildren<Renderer>().All(r => r.enabled);
        Check(!world.HasStartArea && world.EffectiveEdge(new GridCoord(l.door.x, l.door.y - 1), l.door) == EdgeKind.Wall && southBlocked == s.w * 5 && dWall > 0 && !station.Liner.activeSelf
            && entranceShown && Mathf.Abs(plate.z - (z0 + ModuleUnits.WallHalf)) < 1e-3f && Mathf.Abs(plate.y - FrontRoomsStationSet.PlateCentreHeight) < 1e-3f
            && plate.x + FrontRoomsStationSet.PlateWidth * .5f + FrontRoomsStationSet.PlateDoorGap - doorWest < 1e-3f,
            label + "(f) after ClearStartArea and D's chunk rebuilt: " + southBlocked + "/" + (s.w * 5) + " rays across the south side meet the shell, D's wall faces the room in the station's material ("
            + dWall + " vertices), the liner gone, the static entrance standing; the plate's anchor at x " + plate.x.ToString("0.000") + ", " + plate.y.ToString("0.000") + " m on the wall's face");

        // 9. ClearStation: FloorHasStation false, the set and its architecture gone, the carve off the cache and the chunks
        //    it touched rebuilt raw (no station material left); then NextFloor keeps it false.
        world.ClearStation();
        var leftWall = 0;
        foreach (var r in world.GetComponentsInChildren<MeshRenderer>(true)) if (r.sharedMaterial == wallSkin || r.sharedMaterial == floorSkin) leftWall++;
        var cleared = !world.FloorHasStation && !world.HasStation && world.Station == null && world.Cache.Carve == null && !world.IsArchitecture(riser) && leftWall == 0
            && world.StationReady && world.StationPathDistance(l.door, 3) == 4 && world.EffectiveEdge(l.e1In, l.e1Out) == world.Cache.Edge(l.e1In, l.e1Out);
        world.NextFloor(StationPinSeed, 1, new RectInt(0, 0, 0, 0), default(GridCoord));
        Check(cleared && !world.FloorHasStation,
            label + "ClearStation: FloorHasStation false, no set, no carve, the riser no longer architecture, " + leftWall + " renderers in the station's materials after the rebuild; NextFloor keeps it false");
    }

    // A26 step 5: the parities whose unforced send from C2 found rung 1 (the plan's first choice), over both.
    static int relayRung1Sends;

    /// <summary>
    /// A26 step 5 (plan 4.10; 6 row 5): M7, floor 1's first send with a station. The pin seed's floor 1 is made as the game
    /// will make it (as StartGroupWorld, the stand-in station without props, streamed round C2) at both door parities, and
    /// the gate, the ladder, the exclusions, the forced releases, the grace and M6's seam are checked on it.
    /// </summary>
    static void StartGroupRelay(List<GameObject> roots)
    {
        var profile = FrontRoomsLevelProfiles.Resolve();
        var template = StartGroupTemplate.From(FrontRoomsMapVerification.StandInStation(4), null, out var why);
        if (template == null) { Check(false, "start group relay: the stand-in station is refused: " + why); return; }
        relayRung1Sends = 0;
        for (var t = 0; t < 2; t++) StartGroupRelayAt(roots, profile, template, t);
        Check(relayRung1Sends >= 1, "start group relay: rung 1 (8-10 cells, far side) chosen from C2 at " + relayRung1Sends + " of 2 door parities");
    }

    static void StartGroupRelayAt(List<GameObject> roots, FrontRoomsLevelProfile profile, StartGroupTemplate template, int t)
    {
        var label = "start group relay, t " + t + ": ";
        var doorX = Mathf.FloorToInt(FrontRooms3DGame.TitleCenterX / MapGrid.CellSize);
        var world = World(roots, profile, 91f + 2f * t, "MAP INTERACTION TEST / start group relay t" + t);
        world.BuildForCapture();
        var eye = world.Player;
        world.NextFloor(StationPinSeed, 1, new RectInt(0, 0, 0, 0), default(GridCoord));
        var plan = FrontRoomsStartGroup.Plan(world.Cache, template, new GridCoord(doorX, 2 + 4 * t));
        if (!plan.Ready) { Check(false, label + "no plan on the map's cache: " + plan.error); return; }
        var l = plan.layout;
        var s = l.station;
        world.SetStartArea(new RectInt(l.door.x - FrontRoomsStartGroup.StripHalfCells, l.door.y - 8, FrontRoomsStartGroup.StripHalfCells * 2 + 1, 8), l.door);
        if (!world.SetStation(plan, FrontRoomsMapVerification.StandInStation(4))) { Check(false, label + "SetStation refused the plan"); return; }
        world.SetExitArea(plan.exit.exitCell);
        var c2 = l.Corridor(FrontRoomsMapHunter.StationLeaveCells);
        // Streamed round C2, where the gate opens for a player leaving through E1 (the game streams round the player).
        eye.position = world.CellCenter(c2) + Vector3.up * .05f;
        world.Begin(eye, false);
        world.StreamForTools(eye.position, 0f);
        Physics.SyncTransforms();

        var playerGo = new GameObject("MIT relay player");
        playerGo.transform.SetParent(world.transform, false);
        var body = playerGo.AddComponent<CapsuleCollider>();
        body.height = ModuleUnits.PlayerHeight;
        body.radius = ModuleUnits.PlayerRadius;
        body.center = Vector3.up * (ModuleUnits.PlayerHeight * .5f);
        var feet = Vector3.zero;
        void Stand(GridCoord cell)
        {
            feet = world.CellCenter(cell);
            playerGo.transform.position = feet;
            Physics.SyncTransforms();
        }
        Vector3 PlayerEye() => feet + Vector3.up * ModuleUnits.PlayerEye;
        // The plan's rules, re-derived here: the station and its ring; the band 2 cells round the walk D..N and X (one
        // column, D's); the strip.
        bool Ringed(GridCoord cell) => cell.x >= s.x - 1 && cell.x <= s.x + s.w && cell.y >= s.y - 1 && cell.y <= s.y + s.h;
        bool Banded(GridCoord cell) => Mathf.Abs(cell.x - l.door.x) <= 2 && cell.y >= l.door.y - 2 && cell.y <= l.exit.y + 2;
        // A send's place and rung are right: never ringed; rungs 1-3 off the band and the strip; each rung's walking cells;
        // rung 1 on the far side.
        string SendProblem(FrontRoomsMapHunter.FirstSendReport r)
        {
            if (Ringed(r.cell)) return "in the station or its ring";
            if (r.rung < 4 && (Banded(r.cell) || l.InStrip(r.cell))) return "in the exit-walk band or the strip";
            if (r.rung == 1 && (r.cells < FrontRoomsStartGroup.Rung1Min || r.cells > FrontRoomsStartGroup.Rung1Max || !r.farSide)) return "rung 1 at " + r.cells + " cells, far " + r.farSide;
            if (r.rung == 2 && (r.cells < FrontRoomsStartGroup.Rung2Min || r.cells > FrontRoomsStartGroup.Rung2Max)) return "rung 2 at " + r.cells + " cells";
            if (r.rung == 3 && (r.cells < FrontRoomsStartGroup.Rung3Min || r.cells > FrontRoomsStartGroup.Rung3Max)) return "rung 3 at " + r.cells + " cells";
            return null;
        }
        var takeOk = world.StationAnchor(ModuleMarkerKind.KeySpot, out var keyAt, out _, out var keyCell);
        var takePoint = new Vector3(keyAt.x, world.CellCenter(keyCell).y, keyAt.z);
        var seed = 100 + 10 * t;
        FrontRoomsMapHunter NewHunter(FrontRoomsHunterTuning tuning) => new FrontRoomsMapHunter(world, tuning ?? new FrontRoomsHunterTuning(), body, null, seed++);
        FrontRoomsMapHunter.FirstSendPolicy Policy(bool key, int empty)
        {
            var policy = FrontRoomsMapHunter.FirstSendPolicy.ForStation(world);
            policy.huntTo = key ? takePoint : (Vector3?)null;
            policy.emptyRungsForTools = empty;
            return policy;
        }

        // 1. The gate at path distance 0, 1 and 2, through E1 (E1's inside, C1, C2) and through E2 (E2's inside, E2o, a cell
        //    one step on through an Open edge), with and without the key, the key needed or not: Waiting, Waiting, then Left
        //    (Waiting without the key while it is needed); the game sends only when it is due, so the first two stay
        //    Dormant and the third is sent (ReleaseNow). 150 s: Fallback anywhere; 149.9 s: still Waiting.
        // E2's third cell: one step on from E2o through an Open edge (path 2); with none, one through a shut door (beyond
        // the path's reach, so out as well).
        var e2Two = default(GridCoord);
        var e2TwoFound = false;
        for (var pass = 0; pass < 2 && !e2TwoFound; pass++)
            foreach (var step in Steps)
            {
                var n = l.e2Out + step;
                if (l.InStation(n) || !world.IsBuilt(n)) continue;
                var open = world.PassageBetween(l.e2Out, n) == FrontRoomsMapWorld.Passage.Open;
                if (pass == 0 ? !open || world.StationPathDistance(n, 3) != 2 : world.StationPathDistance(n, 3) < 2) continue;
                e2Two = n;
                e2TwoFound = true;
                break;
            }
        var routes = new[] { new[] { l.e1In, l.e1Out, c2 }, new[] { l.e2In, l.e2Out, e2Two } };
        var paths = new List<string>();
        var gateBad = new List<string>();
        var sends = new List<string>();
        var sendBad = new List<string>();
        var sent = 0;
        for (var r = 0; r < 2; r++)
        {
            var route = routes[r];
            for (var i = 0; i < 3; i++) paths.Add((r == 0 ? "E1 " : "E2 ") + route[i] + " " + world.StationPathDistance(route[i], 3));
            for (var mode = 0; mode < 4; mode++)
            {
                var needsKey = mode >= 2;
                var key = (mode & 1) == 1;
                var hunter = NewHunter(null);
                for (var i = 0; i < 3; i++)
                {
                    var cell = route[i];
                    var path = world.StationPathDistance(cell, 3);
                    var gate = FrontRoomsMapHunter.StationSendDue(world, cell, 0f, key, needsKey);
                    var want = path >= 2 && (key || !needsKey) ? FrontRoomsMapHunter.StationGate.Left : FrontRoomsMapHunter.StationGate.Waiting;
                    if ((i < 2 ? path != i : path < 2) || gate != want)
                        gateBad.Add((r == 0 ? "E1 " : "E2 ") + cell + " path " + path + (needsKey ? " key needed" : "") + (key ? " key" : "") + ": " + gate);
                    if (gate == FrontRoomsMapHunter.StationGate.Waiting)
                    {
                        if (hunter.Released) gateBad.Add("released before due at " + cell);
                        continue;
                    }
                    Stand(cell);
                    var ok = hunter.ReleaseNow(Policy(key, 0), feet, PlayerEye(), Vector3.forward);
                    var rep = hunter.LastFirstSend;
                    var leg = key ? HunterState.Hunt : HunterState.Listen;
                    var problem = !ok || !hunter.Released ? "not sent" : SendProblem(rep);
                    if (problem == null && hunter.State != leg) problem = "first leg " + hunter.State;
                    if (problem == null && key && (!hunter.ListenPoint.HasValue || (hunter.ListenPoint.Value - takePoint).sqrMagnitude > 1e-6f)) problem = "the hunt is not to the take point";
                    sent++;
                    sends.Add((r == 0 ? "E1" : "E2") + (needsKey ? " needs key" : "") + (key ? " key" : "") + ": rung " + rep.rung + " (" + rep.rung1 + "/" + rep.rung2 + "/" + rep.rung3 + ") "
                        + rep.cell + " " + rep.cells + " cells " + rep.straight.ToString("0.0") + " m " + (rep.farSide ? "far" : "near") + " " + hunter.State);
                    if (problem != null) sendBad.Add(sends[sends.Count - 1] + ": " + problem);
                    if (r == 0 && mode == 0)
                    {
                        if (rep.rung == 1) relayRung1Sends++;
                        if (rep.rung1 > 0 && rep.rung != 1) sendBad.Add("C2 has " + rep.rung1 + " rung 1 cells but drew rung " + rep.rung);
                    }
                }
            }
        }
        // The stream rooms behind the start door (the start area, walled off from the map) are never out of the station.
        var streamRoom = new GridCoord(l.door.x, l.door.y - 1);
        var inStream = FrontRoomsMapHunter.StationSendDue(world, streamRoom, 0f, true, false);
        Stand(l.e1In);
        var fallback = FrontRoomsMapHunter.StationSendDue(world, l.e1In, FrontRoomsMapHunter.StationSendSeconds, false, true);
        var notYet = FrontRoomsMapHunter.StationSendDue(world, l.e1In, FrontRoomsMapHunter.StationSendSeconds - .1f, false, true);
        var fallbackHunter = NewHunter(null);
        var fallbackSent = fallback == FrontRoomsMapHunter.StationGate.Fallback && fallbackHunter.ReleaseNow(Policy(false, 0), feet, PlayerEye(), Vector3.forward);
        var fallbackProblem = fallbackSent ? SendProblem(fallbackHunter.LastFirstSend) : "not sent";
        Check(e2TwoFound && gateBad.Count == 0 && sent == 6 && sendBad.Count == 0 && notYet == FrontRoomsMapHunter.StationGate.Waiting && fallbackProblem == null && fallbackHunter.State == HunterState.Listen
                && world.InStartArea(streamRoom) && inStream == FrontRoomsMapHunter.StationGate.Waiting,
            label + "the gate (path cells " + string.Join(", ", paths.ToArray()) + "): Dormant / Dormant / sent through E1 and E2 with and without the key (never sent without it while it is needed); "
            + sent + " sends: " + string.Join("; ", sends.ToArray()) + "; at 150 s from E1's inside " + fallback + " (149.9 s " + notYet + "), sent rung " + fallbackHunter.LastFirstSend.rung
            + " to " + fallbackHunter.LastFirstSend.cell + "; in the stream room behind D " + inStream + (gateBad.Count + sendBad.Count > 0 ? "; WRONG: " + string.Join("; ", gateBad.Concat(sendBad).Take(6).ToArray()) : "")
            + (fallbackProblem != null ? "; fallback " + fallbackProblem : ""));

        // 2. Rungs forced empty walk the ladder: from C2 without the key, rung 1 off gives rung 2 (6-11 cells, any side),
        //    1 and 2 off rung 3 (14-20), all three off today's Arrive (9-15, behind first), the station and its ring kept out.
        Stand(c2);
        var ladder = new List<string>();
        var ladderBad = new List<string>();
        foreach (var empty in new[] { 1, 3, 7 })
        {
            var hunter = NewHunter(null);
            var ok = hunter.ReleaseNow(Policy(false, empty), feet, PlayerEye(), Vector3.forward);
            var rep = hunter.LastFirstSend;
            var want = (empty & 2) == 0 && rep.rung2 > 0 ? 2 : (empty & 4) == 0 && rep.rung3 > 0 ? 3 : 4;
            var problem = !ok ? "not sent" : rep.rung != want ? "rung " + rep.rung + ", not " + want : SendProblem(rep);
            if (problem == null && rep.rung == 4 && rep.cells >= 0 && (rep.cells < 9 || rep.cells > 15)) problem = "Arrive at " + rep.cells + " cells";
            ladder.Add("off " + empty + ": rung " + rep.rung + " " + rep.cell + " " + rep.cells + " cells");
            if (problem != null) ladderBad.Add("off " + empty + ": " + problem);
        }
        Check(ladderBad.Count == 0, label + "rungs forced empty walk the ladder from C2: " + string.Join("; ", ladder.ToArray()) + (ladderBad.Count > 0 ? "; WRONG: " + string.Join("; ", ladderBad.ToArray()) : ""));

        // 3. 300 forced releases from 100 player cells spread over the walks 0-16 from D (the station, the corridor, the
        //    maze round them): the ladder, today's Arrive (every rung forced empty) and the leash relay (placed off the
        //    built map, one tick: Arrive). None lands in the station or its ring; the ladder's never in the band or the
        //    strip. How many of those starts had a station or ring cell in Arrive's 9-15 is counted (the rule was needed).
        var reach = new Dictionary<GridCoord, int> { { l.door, 0 } };
        var order = new List<GridCoord> { l.door };
        var queue = new Queue<GridCoord>();
        queue.Enqueue(l.door);
        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            if (reach[cell] >= 16) continue;
            foreach (var step in Steps)
            {
                var n = cell + step;
                if (reach.ContainsKey(n) || !world.IsBuilt(n)) continue;
                var passage = world.PassageBetween(cell, n);
                if (passage == FrontRoomsMapWorld.Passage.Wall || passage == FrontRoomsMapWorld.Passage.Glass) continue;
                reach[n] = reach[cell] + 1;
                order.Add(n);
                queue.Enqueue(n);
            }
        }
        // Whether a station or ring cell lies 9-15 walking cells from a start (the Relay's Search: built cells, doors pass).
        bool StationInBand(GridCoord from)
        {
            var d = new Dictionary<GridCoord, int> { { from, 0 } };
            var q = new Queue<GridCoord>();
            q.Enqueue(from);
            while (q.Count > 0)
            {
                var cell = q.Dequeue();
                if (d[cell] >= 15) continue;
                foreach (var step in Steps)
                {
                    var n = cell + step;
                    if (d.ContainsKey(n) || !world.IsBuilt(n)) continue;
                    var passage = world.PassageBetween(cell, n);
                    if (passage == FrontRoomsMapWorld.Passage.Wall || passage == FrontRoomsMapWorld.Passage.Glass) continue;
                    d[n] = d[cell] + 1;
                    if (d[n] >= 9 && Ringed(n)) return true;
                    q.Enqueue(n);
                }
            }
            return false;
        }
        var ladderHunter = NewHunter(null);
        var arriveHunter = NewHunter(null);
        var leashHunter = NewHunter(null);
        var far = world.CellCenter(new GridCoord(l.door.x + 200, l.door.y + 200));
        int releases = 0, landedInRing = 0, ladderInBand = 0, notSent = 0, needed = 0;
        var rungsUsed = new int[5];
        var forcedBad = new List<string>();
        for (var k = 0; k < 100; k++)
        {
            var from = order[k * order.Count / 100];
            Stand(from);
            if (StationInBand(from)) needed++;
            for (var kind = 0; kind < 3; kind++)
            {
                var hunter = kind == 0 ? ladderHunter : kind == 1 ? arriveHunter : leashHunter;
                hunter.Park();
                bool ok;
                if (kind < 2) ok = hunter.ReleaseNow(Policy(false, kind == 0 ? 0 : 7), feet, PlayerEye(), Vector3.forward);
                else
                {
                    hunter.DebugPlace(far);
                    hunter.Tick(Dt, feet, PlayerEye(), Vector3.forward);
                    ok = world.IsBuilt(world.CellOf(hunter.Position));
                }
                releases++;
                if (!ok) { notSent++; continue; }
                var cell = world.CellOf(hunter.Position);
                if (Ringed(cell)) { landedInRing++; forcedBad.Add((kind == 0 ? "ladder" : kind == 1 ? "arrive" : "leash") + " from " + from + " to " + cell); }
                if (kind == 0)
                {
                    rungsUsed[hunter.LastFirstSend.rung]++;
                    if (hunter.LastFirstSend.rung < 4 && (Banded(cell) || l.InStrip(cell))) { ladderInBand++; forcedBad.Add("ladder from " + from + " in the band at " + cell); }
                }
            }
        }
        Check(releases == 300 && landedInRing == 0 && ladderInBand == 0 && notSent == 0 && needed > 0,
            label + releases + " forced releases from " + order.Count + " cells within 16 of D (100 starts, " + needed + " with the station or its ring 9-15 cells away): " + landedInRing
            + " in the station or its ring, " + ladderInBand + " of the ladder's in the exit-walk band or the strip, " + notSent + " found no cell; the ladder's rungs 1/2/3/4 "
            + rungsUsed[1] + "/" + rungsUsed[2] + "/" + rungsUsed[3] + "/" + rungsUsed[4] + (forcedBad.Count > 0 ? "; WRONG: " + string.Join("; ", forcedBad.Take(6).ToArray()) : ""));

        // 4. The grace: a Relay standing in N sees the player 9 m off in C1 and starts no chase while ChaseGrace holds;
        //    within 6 m (the player in C3) it chases; without the grace it chases from 9 m.
        var graceHunter = NewHunter(null);
        graceHunter.DebugPlace(world.CellCenter(l.front));
        graceHunter.ChaseGrace = true;
        Stand(l.e1Out);
        graceHunter.Tick(Dt, feet, PlayerEye(), Vector3.forward);
        var heldSees = graceHunter.SeesPlayer;
        var heldState = graceHunter.State;
        Stand(l.Corridor(3));
        graceHunter.Tick(Dt, feet, PlayerEye(), Vector3.forward);
        var closeState = graceHunter.State;
        var plainHunter = NewHunter(null);
        plainHunter.DebugPlace(world.CellCenter(l.front));
        Stand(l.e1Out);
        plainHunter.Tick(Dt, feet, PlayerEye(), Vector3.forward);
        Check(heldSees && heldState != HunterState.Chase && closeState == HunterState.Chase && plainHunter.SeesPlayer && plainHunter.State == HunterState.Chase,
            label + "the grace: seen 9 m off from N, " + heldState + " (sees " + heldSees + "); within 6 m " + closeState + "; with no grace " + plainHunter.State);

        // 5. M6's seam: TakeStationKey raises StationKeyTaken once, at the KeySpot brought down to the floor; a second take
        //    raises nothing; ForStation's policy then hunts there. HuntStation leaves a Dormant Relay as it is and sends a
        //    released one (Listen) to the take point: blind, it walks there round the riser block and stops within 0.5 m,
        //    never closer to the block than its body. ClearStation forgets the key.
        var raised = 0;
        var raisedAt = Vector3.zero;
        void OnTaken(Vector3 at) { raised++; raisedAt = at; }
        world.StationKeyTaken += OnTaken;
        var before = world.StationKeyIsTaken;
        var first = world.TakeStationKey();
        var second = world.TakeStationKey();
        world.StationKeyTaken -= OnTaken;
        var policyAfter = FrontRoomsMapHunter.FirstSendPolicy.ForStation(world);
        var dormant = NewHunter(null);
        dormant.HuntStation(takePoint);
        var walker = NewHunter(new FrontRoomsHunterTuning { sightRange = 0f });
        Stand(c2);
        var walkerSent = walker.ReleaseNow(Policy(false, 0), feet, PlayerEye(), Vector3.forward);
        var listened = walker.State;
        walker.HuntStation(world.StationTakePoint);
        var hunting = walker.State;
        var riser = world.Station.RiserBlock.bounds;
        float closest = float.PositiveInfinity, block = float.PositiveInfinity, arrivedAt = -1f;
        for (var tt = 0f; tt < 45f && arrivedAt < 0f; tt += Dt)
        {
            walker.Tick(Dt, feet, PlayerEye(), Vector3.forward);
            world.TickDoorsForTools(Dt);
            Physics.SyncTransforms();
            var p = walker.Position;
            var dx = Mathf.Max(riser.min.x - p.x, 0f, p.x - riser.max.x);
            var dz = Mathf.Max(riser.min.z - p.z, 0f, p.z - riser.max.z);
            block = Mathf.Min(block, Mathf.Sqrt(dx * dx + dz * dz));
            var gap = new Vector2(p.x - takePoint.x, p.z - takePoint.z).magnitude;
            closest = Mathf.Min(closest, gap);
            if (gap < .5f) arrivedAt = tt;
        }
        Check(takeOk && !before && first && !second && raised == 1 && (raisedAt - takePoint).sqrMagnitude < 1e-6f && world.StationKeyIsTaken && (world.StationTakePoint - takePoint).sqrMagnitude < 1e-6f
            && policyAfter != null && policyAfter.huntTo.HasValue && (policyAfter.huntTo.Value - takePoint).sqrMagnitude < 1e-6f && dormant.State == HunterState.Dormant
            && walkerSent && listened == HunterState.Listen && hunting == HunterState.Hunt && arrivedAt >= 0f && block >= ModuleUnits.RelayRadius - .05f,
            label + "M6's seam: StationKeyTaken raised " + raised + " time(s) at the KeySpot on the floor " + keyCell + " (a second take " + second + "), ForStation hunts there; a Dormant Relay stays "
            + dormant.State + "; one sent from C2 (" + listened + ") hunts to the take point (" + hunting + ") and reaches it in " + (arrivedAt >= 0f ? arrivedAt.ToString("0.0") + " s" : "no time (closest "
            + closest.ToString("0.00") + " m)") + ", its body at least " + block.ToString("0.00") + " m from the riser block");

        world.ClearStation();
        Check(!world.StationKeyIsTaken && !world.TakeStationKey() && FrontRoomsMapHunter.FirstSendPolicy.ForStation(world) == null,
            label + "ClearStation forgets the key (StationKeyIsTaken " + world.StationKeyIsTaken + "), no take and no policy without a station");
        UnityEngine.Object.DestroyImmediate(playerGo);
    }

    // ---------- M3 / M6 / R3: the floor key (notes/a26_group_m3_plan.md 6.1) ----------

    static void StartGroupKeys(List<GameObject> roots)
    {
        var profile = FrontRoomsLevelProfiles.Resolve();
        var template = StartGroupTemplate.From(FrontRoomsMapVerification.StandInStation(4), null, out var why);
        if (template == null) { Check(false, "start group keys: the stand-in station is refused: " + why); return; }
        // The one-line prompts are the key HUD's words (A22).
        var middot = "  \u00B7  ";
        Check(FrontRoomsMapWorld.LockedNeedsStairKey == FrontRoomsKeyHud.Strings.Locked + middot + FrontRoomsKeyHud.Strings.NeedsKey
              && FrontRoomsMapWorld.UnlockPrompt == FrontRoomsKeyHud.Strings.Unlock && FrontRoomsMapHunter.StationSendNeedsKey,
            "start group keys: the map's one-line prompts are the key HUD's (\"" + FrontRoomsMapWorld.LockedNeedsStairKey + "\", \"" + FrontRoomsMapWorld.UnlockPrompt
            + "\"); M7's send needs the key (StationSendNeedsKey " + FrontRoomsMapHunter.StationSendNeedsKey + ")");
        StartGroupKeysOff(roots);
        ZoneKeysOffRule(roots);
        for (var t = 0; t < 2; t++) StartGroupKeysAt(roots, profile, template, t);
    }

    // The keys built under a map: zone keys and floor keys, active ones only (a rebuilt chunk's old root is inactive).
    static void CountKeys(FrontRoomsMapWorld world, out int zoneKeys, out int floorKeys, out Transform floorKey)
    {
        zoneKeys = floorKeys = 0;
        floorKey = null;
        foreach (var t in world.GetComponentsInChildren<Transform>(false))
        {
            if (t.name.StartsWith("Key \u00B7 zone", StringComparison.Ordinal)) zoneKeys++;
            else if (t.name == FrontRoomsMapWorld.FloorKeyName) { floorKeys++; floorKey = t; }
        }
    }

    // A shut map door (not the EXIT) between two built cells within 12 cells of a cell, or null.
    static FrontRoomsMapWorld.Door InnerDoorNear(FrontRoomsMapWorld world, GridCoord around)
    {
        for (var r = 1; r <= 12; r++)
        for (var y = around.y - r; y <= around.y + r; y++)
        for (var x = around.x - r; x <= around.x + r; x++)
        {
            if (Mathf.Max(Mathf.Abs(x - around.x), Mathf.Abs(y - around.y)) != r) continue;
            var cell = new GridCoord(x, y);
            if (!world.IsBuilt(cell)) continue;
            foreach (var s in Steps)
            {
                var other = cell + s;
                var door = world.DoorBetween(cell, other);
                if (door == null || door.open || door.broken || world.IsExitDoor(door) || !world.IsBuilt(other) || world.InStation(cell) || world.InStation(other)
                    || world.IsReserved(cell) || world.IsReserved(other)) continue;
                return door;
            }
        }
        return null;
    }

    // 1. Station off (today's map, the profile as shipped and with keys on): no floor key rule, FloorHasStation false, the
    //    inner doors' rule is the keys flag, zone keys and no floor key, the zone keys' prompt; nothing locks the EXIT.
    static void StartGroupKeysOff(List<GameObject> roots)
    {
        const string label = "start group keys, station off: ";
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), 115f, "MAP INTERACTION TEST / start group keys off");
        world.KeyPickupNeedsE = true;
        world.BuildForCapture();
        CountKeys(world, out var zoneKeys, out var floorKeys, out _);
        var flagOff = world.InteriorDoorsLocked == world.DoorsNeedKeysForTools && !world.DoorsNeedKeysForTools;
        world.DoorsNeedKeysForTools = true;
        var flagOn = world.InteriorDoorsLocked && world.DoorsNeedKeysForTools;
        var player = world.Player;
        var door = InnerDoorNear(world, world.CellOf(world.SpawnWorldPosition));
        string prompt = null;
        var kind = FrontRoomsMapWorld.AimKind.None;
        var keyZone = default(GridCoord);
        var haveKey = true;
        if (door != null)
        {
            player.position = world.CellCenter(door.a) + Vector3.up * .05f;
            prompt = world.Describe(door.leaf, out _);
            kind = world.Kind(door.leaf, player.position, out keyZone, out haveKey);
        }
        world.DoorsNeedKeysForTools = false;
        Check(!world.FloorKeyRule && !world.FloorKeyHeld && !world.FloorHasStation && !world.NoInteriorLocksForTools && flagOff && flagOn
              && zoneKeys > 0 && floorKeys == 0 && door != null && prompt == "LOCKED" + "  \u00B7  " + "NEEDS THIS ZONE'S KEY" && kind == FrontRoomsMapWorld.AimKind.Locked
              && keyZone == world.ZoneOf(door.a).id && !haveKey && world.KeyZoneAt(door.a) == world.ZoneOf(door.a).id && !world.ExitLocked,
            label + "FloorKeyRule and FloorHasStation false; InteriorDoorsLocked is the keys flag (off " + flagOff + ", on " + flagOn + "); " + zoneKeys + " zone keys and "
            + floorKeys + " floor keys built; with keys on a shut inner door reads \"" + prompt + "\", Kind " + kind + " with its zone's key " + keyZone + " (held " + haveKey + "); ExitLocked "
            + world.ExitLocked);
    }

    // 1b. Q1 B (Red 2026-10-29 17:21): ZoneKeysOff, which the game sets on every floor below a station floor 1. On: every
    //     built chunk with a key site, rebuilt, builds no zone key (its data keeps the site), and no inner door waits for
    //     one (InteriorDoorsLocked false with keys on). Off again: the same chunks build their zone keys as before. False
    //     by default, and NextFloor clears it.
    static void ZoneKeysOffRule(List<GameObject> roots)
    {
        const string label = "zone keys off (Q1 B): ";
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), 117f, "MAP INTERACTION TEST / zone keys off");
        world.KeyPickupNeedsE = true;
        world.BuildForCapture();
        var offByDefault = !world.ZoneKeysOff;
        CountKeys(world, out var before, out _, out _);
        world.ZoneKeysOff = true;
        var sites = new List<GridCoord>();
        var sitesKept = true;
        // Each pass rebuilds the chunk of the nearest zone key still built; with the flag ignored the same key comes back.
        for (var guard = 0; guard < 64; guard++)
        {
            var key = world.NearestKeyForTools(world.SpawnWorldPosition, out _, out var chunk);
            if (key == null) break;
            world.RebuildChunk(chunk);
            if (!sites.Contains(chunk)) sites.Add(chunk);
            sitesKept &= world.Cache.Get(chunk).hasKey;
        }
        CountKeys(world, out var on, out var floorKeysOn, out _);
        world.DoorsNeedKeysForTools = true;
        var locksOn = world.InteriorDoorsLocked;
        world.DoorsNeedKeysForTools = false;
        world.ZoneKeysOff = false;
        foreach (var chunk in sites) world.RebuildChunk(chunk);
        CountKeys(world, out var after, out _, out _);
        world.DoorsNeedKeysForTools = true;
        var locksOff = world.InteriorDoorsLocked;
        world.DoorsNeedKeysForTools = false;
        world.ZoneKeysOff = true;
        world.NextFloor(StationPinSeed, 1, new RectInt(0, 0, 0, 0), default(GridCoord));
        var cleared = !world.ZoneKeysOff;
        Check(offByDefault && before > 0 && sites.Count == before && sitesKept && on == 0 && floorKeysOn == 0 && !locksOn && after == before && locksOff && cleared,
            label + "false by default " + offByDefault + "; on: " + sites.Count + " built chunks with a key site rebuilt, " + on + " zone keys built (was " + before
            + "), their sites kept in the data " + sitesKept + ", inner doors locked with keys on " + locksOn + "; off again: the same chunks rebuilt, " + after
            + " zone keys, locked with keys on " + locksOff + "; NextFloor clears it " + cleared);
    }

    static void StartGroupKeysAt(List<GameObject> roots, FrontRoomsLevelProfile profile, StartGroupTemplate template, int t)
    {
        var label = "start group keys, t " + t + ": ";
        var fk = FrontRoomsMapWorld.FloorKeyZone;
        var doorX = Mathf.FloorToInt(FrontRooms3DGame.TitleCenterX / MapGrid.CellSize);
        var world = World(roots, profile, 111f + 2f * t, "MAP INTERACTION TEST / start group keys t" + t);
        world.BuildForCapture();
        world.KeyPickupNeedsE = true;
        var eye = world.Player;
        world.NextFloor(StationPinSeed, 1, new RectInt(0, 0, 0, 0), default(GridCoord));
        var plan = FrontRoomsStartGroup.Plan(world.Cache, template, new GridCoord(doorX, 2 + 4 * t));
        if (!plan.Ready) { Check(false, label + "no plan on the map's cache: " + plan.error); return; }
        var l = plan.layout;
        world.SetStartArea(new RectInt(l.door.x - FrontRoomsStartGroup.StripHalfCells, l.door.y - 8, FrontRoomsStartGroup.StripHalfCells * 2 + 1, 8), l.door);
        var ruleBefore = world.FloorKeyRule || (world.FloorHasStation && !world.StationKeyIsTaken);
        if (!world.SetStation(plan, FrontRoomsMapVerification.StandInStation(4))) { Check(false, label + "SetStation refused the plan"); return; }
        world.SetExitArea(plan.exit.exitCell);
        // The map's events, in the order raised: S KeyPickupStarted, H StationKeyTaken, T KeyTaken, L ExitLockChanged (false),
        // U DoorUnlocked; and the lock's sets (true).
        var order = new List<string>();
        int lockSets = 0, lockedRaised = 0, rattledRaised = 0;
        FrontRoomsMapWorld.Door lockDoor = null;
        var heardAt = Vector3.zero;
        var unlockZones = new List<GridCoord>();
        world.KeyPickupStarted += (z, p) => order.Add(z == fk ? "S" : "s");
        world.StationKeyTaken += p => { order.Add("H"); heardAt = p; };
        world.KeyTaken += z => order.Add(z == fk ? "T" : "t");
        world.ExitLockChanged += (d, locked) => { if (locked) lockSets++; else { order.Add("L"); lockDoor = d; } };
        world.DoorUnlocked += (d, z, p) => { order.Add(world.IsExitDoor(d) ? "U" : "u"); unlockZones.Add(z); };
        world.DoorLocked += p => lockedRaised++;
        world.DoorRattled += (d, p) => rattledRaised++;
        // The game's PlanExit: the floor-key floor's EXIT locked before Begin (no door built: ExitLockChanged(null, true)).
        var lockedAtStart = world.SetExitLocked(true);
        eye.position = world.CellCenter(l.door) + Vector3.up * .05f;
        world.Begin(eye, false);
        world.StreamForTools(eye.position, 0f);
        Physics.SyncTransforms();
        world.StationAnchor(ModuleMarkerKind.KeySpot, out var keyAt, out _, out var keyCell);
        var keyChunk = MapGrid.ChunkOf(keyCell);
        var takePoint = new Vector3(keyAt.x, world.CellCenter(keyCell).y, keyAt.z);
        var n = l.front;
        var x = l.exit;

        // 2. The rule is on: one floor key at the KeySpot (6 cm up) with its look+E trigger, no zone key anywhere; a rebuild
        //    of its chunk and a drop of c and c' with the stream's return bring it back once, never twice.
        CountKeys(world, out var zoneKeys, out var floorKeys, out var floorKey);
        var keyPose = floorKey != null && (floorKey.position - (keyAt + Vector3.up * .06f)).magnitude < 1e-3f;
        var trigger = floorKey != null ? floorKey.GetComponent<Collider>() : null;
        var takeable = trigger != null && trigger.enabled && trigger.isTrigger && world.Describe(trigger, out _) == "E" + "  \u00B7  " + "TAKE KEY";
        world.RebuildChunk(keyChunk);
        CountKeys(world, out var zoneAfterRebuild, out var floorAfterRebuild, out _);
        world.DropChunkForTools(l.DoorChunk);
        if (l.ExitChunk != l.DoorChunk) world.DropChunkForTools(l.ExitChunk);
        world.StreamForTools(eye.position, 0f);
        Physics.SyncTransforms();
        CountKeys(world, out var zoneAfterDrop, out var floorAfterDrop, out floorKey);
        trigger = floorKey != null ? floorKey.GetComponent<Collider>() : null;
        Check(!ruleBefore && world.FloorKeyRule && !world.FloorKeyHeld && world.FloorHasStation && !world.StationKeyIsTaken && !world.InteriorDoorsLocked && world.NoInteriorLocksForTools
              && floorKeys == 1 && keyPose && takeable && zoneKeys == 0 && floorAfterRebuild == 1 && zoneAfterRebuild == 0 && floorAfterDrop == 1 && zoneAfterDrop == 0 && trigger != null
              && world.KeyZoneAt(l.door) == fk && world.KeyZoneAt(n) == fk,
            label + "SetStation turns M3 on (FloorKeyRule, FloorHasStation true, StationKeyIsTaken false; no locked inner doors, A13); " + floorKeys + " floor key at the KeySpot " + keyCell + " in chunk " + keyChunk
            + " (6 cm up " + keyPose + ", look+E trigger " + takeable + "), " + zoneKeys + " zone keys built; after RebuildChunk " + floorAfterRebuild + " / " + zoneAfterRebuild
            + ", after a drop of c and c' and the stream's return " + floorAfterDrop + " / " + zoneAfterDrop);

        // 3. The EXIT locked since the start (one ExitLockChanged(null, true), ExitDoorBuilt reads it): without the floor key,
        //    from N, Kind Locked with the floor key's identity, "LOCKED . NEEDS STAIR KEY", E rattles, TryOpenDoor false.
        var door = world.ExitDoor;
        if (door == null) { Check(false, label + "the EXIT door is not built"); return; }
        eye.position = world.CellCenter(n) + Vector3.up * .05f;
        var lockedKind = world.Kind(door.leaf, eye.position, out var lockedZone, out var lockedHave);
        var lockedWords = world.Describe(door.leaf, out _);
        var noKeyFor = !world.KeyFor(door, eye.position).HasValue;
        var rattle = world.Use(door.leaf);
        var tried = world.TryOpenDoor(n, x);
        Check(lockedAtStart && lockSets == 1 && world.ExitLocked && world.LockedByRule(door) && lockedKind == FrontRoomsMapWorld.AimKind.Locked && lockedZone == fk && !lockedHave
              && lockedWords == FrontRoomsMapWorld.LockedNeedsStairKey && noKeyFor && rattle == FrontRoomsMapWorld.UseResult.Rattled && lockedRaised == 1 && rattledRaised == 1 && !tried && !door.open,
            label + "the EXIT locked from the start (" + lockSets + " ExitLockChanged(true), before Begin): Kind " + lockedKind + " with the floor key's identity (" + (lockedZone == fk) + "), held "
            + lockedHave + ", \"" + lockedWords + "\"; E answers " + rattle + " (DoorLocked " + lockedRaised + "x, DoorRattled " + rattledRaised + "x); TryOpenDoor " + tried);
        for (var k = 0; k < Mathf.CeilToInt((FrontRoomsShotTimings.Rattle.Duration + .1f) / Dt); k++) world.TickDoorsForTools(Dt);

        // 6a. Inner doors on the station floor, keys on: none is locked (A13). With Floor 1's rule lifted (tools) one is,
        //     by the floor key: Locked with its identity, "LOCKED . NEEDS STAIR KEY", E rattles; its zone's key never fits.
        world.DoorsNeedKeysForTools = true;
        var inner = InnerDoorNear(world, l.door);
        if (inner == null) { Check(false, label + "no shut inner door within 12 cells of D"); world.DoorsNeedKeysForTools = false; return; }
        var a = inner.a;
        var innerFrom = world.CellCenter(a) + Vector3.up * .05f;
        eye.position = innerFrom;
        var openKind = world.Kind(inner.leaf, innerFrom, out _, out _);
        var openWords = world.Describe(inner.leaf, out _);
        var openRule = world.LockedByRule(inner);
        world.NoInteriorLocksForTools = false;
        var innerKind = world.Kind(inner.leaf, innerFrom, out var innerZone, out var innerHave);
        var innerWords = world.Describe(inner.leaf, out _);
        var before = rattledRaised;
        var innerUse = world.Use(inner.leaf);
        for (var k = 0; k < Mathf.CeilToInt((FrontRoomsShotTimings.Rattle.Duration + .1f) / Dt); k++) world.TickDoorsForTools(Dt);
        world.GiveKeyForTools(world.ZoneOf(a).id);
        var zoneKeyKind = world.Kind(inner.leaf, innerFrom, out _, out var zoneKeyHave);
        var zoneKeyFor = world.KeyFor(inner, innerFrom);
        Check(openKind == FrontRoomsMapWorld.AimKind.Open && openWords == "E" + "  \u00B7  " + "OPEN DOOR" && !openRule && world.InteriorDoorsLocked && innerKind == FrontRoomsMapWorld.AimKind.Locked
              && innerZone == fk && !innerHave && innerWords == FrontRoomsMapWorld.LockedNeedsStairKey && innerUse == FrontRoomsMapWorld.UseResult.Rattled && rattledRaised == before + 1
              && zoneKeyKind == FrontRoomsMapWorld.AimKind.Locked && !zoneKeyHave && !zoneKeyFor.HasValue && !inner.open,
            label + "inner door " + inner.a + "-" + inner.b + " with keys on: Kind " + openKind + ", \"" + openWords + "\", LockedByRule " + openRule + " (A13: Floor 1 has none); with the rule lifted Kind "
            + innerKind + " by the floor key (" + (innerZone == fk) + "), \"" + innerWords + "\", E " + innerUse + "; holding its zone's key: Kind " + zoneKeyKind + ", KeyFor none " + !zoneKeyFor.HasValue);

        // 7a. Wayfinding before the take: the locked inner door is no way (infinite), the EXIT never one either.
        var way = new FrontRoomsWayfinding.WorldMap(world);
        var revBefore = way.Revision;
        var costBefore = way.Crossing(inner.a, inner.b, out _);

        // 8 (set-up). M6, as the game wires it (OnStationKeyTaken: relay.HuntStation(at)): a Relay released at C2 (Listen,
        //    no hearing range needed) and a Dormant one, both hearing the take.
        var c2 = l.Corridor(FrontRoomsMapHunter.StationLeaveCells);
        var playerGo = new GameObject("MIT keys player");
        playerGo.transform.SetParent(world.transform, false);
        var body = playerGo.AddComponent<CapsuleCollider>();
        body.height = ModuleUnits.PlayerHeight;
        body.radius = ModuleUnits.PlayerRadius;
        body.center = Vector3.up * (ModuleUnits.PlayerHeight * .5f);
        var feet = world.CellCenter(c2);
        playerGo.transform.position = feet;
        Physics.SyncTransforms();
        var released = new FrontRoomsMapHunter(world, new FrontRoomsHunterTuning { sightRange = 0f }, body, null, 300 + t);
        var policy = FrontRoomsMapHunter.FirstSendPolicy.ForStation(world);
        var sent = released.ReleaseNow(policy, feet, feet + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
        var sentState = released.State;
        var dormant = new FrontRoomsMapHunter(world, new FrontRoomsHunterTuning(), body, null, 310 + t);
        Action<Vector3> hunt = p => { released.HuntStation(p); dormant.HuntStation(p); };
        world.StationKeyTaken += hunt;
        // 9 (before). M7's gate needs the key: Waiting at C2 without it (until 150 s after the shut: Fallback).
        var gateNoKey = FrontRoomsMapHunter.StationSendDue(world, c2, 0f, world.StationKeyIsTaken, FrontRoomsMapHunter.StationSendNeedsKey);
        var gateFallback = FrontRoomsMapHunter.StationSendDue(world, c2, FrontRoomsMapHunter.StationSendSeconds, world.StationKeyIsTaken, FrontRoomsMapHunter.StationSendNeedsKey);
        // 11 (before). FloorHasStation and StationKeyIsTaken by reflection, as the visual line's FrontRoomsStationPresence
        //     reads them (R3 is theirs; the map adds no TV hook); [Preserve] on FloorHasStation.
        var hasStationProperty = typeof(FrontRoomsMapWorld).GetProperty("FloorHasStation");
        var takenProperty = typeof(FrontRoomsMapWorld).GetProperty("StationKeyIsTaken");
        var hasStationPreserved = hasStationProperty != null && hasStationProperty.GetCustomAttributes(typeof(UnityEngine.Scripting.PreserveAttribute), false).Length == 1;
        var hasStationBefore = hasStationProperty != null && (bool)hasStationProperty.GetValue(world);
        var takenBefore = takenProperty == null || (bool)takenProperty.GetValue(world);

        // 4. The take by E (Use on its trigger): KeyTaken; KeyPickupStarted, StationKeyTaken (once, at the KeySpot on the
        //    floor), KeyTaken in that order in the same call (no presenter); the key held, gone, its trigger spent; again: None.
        eye.position = world.CellCenter(keyCell) + Vector3.up * .05f;
        order.Clear();
        var take = world.Use(trigger);
        var takeOrder = string.Join("", order.ToArray());
        CountKeys(world, out _, out var floorAfterTake, out _);
        var again = trigger != null ? world.Use(trigger) : FrontRoomsMapWorld.UseResult.None;
        var secondTake = world.TakeStationKey();
        world.RebuildChunk(keyChunk);
        CountKeys(world, out var zoneAfterTake, out var floorAfterTakeRebuild, out _);
        var hasStationAfter = hasStationProperty != null && (bool)hasStationProperty.GetValue(world);
        var takenAfter = takenProperty != null && (bool)takenProperty.GetValue(world);
        Check(take == FrontRoomsMapWorld.UseResult.KeyTaken && takeOrder == "SHT" && (heardAt - takePoint).sqrMagnitude < 1e-6f && world.FloorKeyHeld && world.HasKeyFor(fk)
              && world.StationKeyIsTaken && (world.StationTakePoint - takePoint).sqrMagnitude < 1e-6f && world.FloorHasStation && floorAfterTake == 0
              && (trigger == null || !trigger.enabled) && again == FrontRoomsMapWorld.UseResult.None && !secondTake && floorAfterTakeRebuild == 0 && zoneAfterTake == 0
              && hasStationPreserved && hasStationBefore && !takenBefore && hasStationAfter && takenAfter,
            label + "E on the floor key: " + take + "; events " + takeOrder + " (KeyPickupStarted, StationKeyTaken, KeyTaken), StationKeyTaken at the KeySpot on the floor; FloorKeyHeld "
            + world.FloorKeyHeld + ", StationKeyIsTaken " + world.StationKeyIsTaken + "; by reflection ([Preserve] on FloorHasStation " + hasStationPreserved + ") FloorHasStation, StationKeyIsTaken "
            + hasStationBefore + ", " + takenBefore + " -> " + hasStationAfter + ", " + takenAfter + "; the key gone (" + floorAfterTake + " built), its trigger spent; E again " + again + ", TakeStationKey again " + secondTake + "; a rebuild of its chunk: "
            + floorAfterTakeRebuild + " floor keys, " + zoneAfterTake + " zone keys");

        // 8. M6: the released Relay now hunts to the take point (no hearing range); the Dormant one is not moved, its
        //    first send's policy hunts there. 9: the gate is due at C2 with the key.
        world.StationKeyTaken -= hunt;
        var policyAfter = FrontRoomsMapHunter.FirstSendPolicy.ForStation(world);
        var gateKey = FrontRoomsMapHunter.StationSendDue(world, c2, 0f, world.StationKeyIsTaken, FrontRoomsMapHunter.StationSendNeedsKey);
        Check(sent && sentState == HunterState.Listen && released.State == HunterState.Hunt && released.ListenPoint.HasValue && (released.ListenPoint.Value - takePoint).sqrMagnitude < 1e-6f
              && dormant.State == HunterState.Dormant && !dormant.Released && policyAfter != null && policyAfter.huntTo.HasValue && (policyAfter.huntTo.Value - takePoint).sqrMagnitude < 1e-6f
              && gateNoKey == FrontRoomsMapHunter.StationGate.Waiting && gateFallback == FrontRoomsMapHunter.StationGate.Fallback && gateKey == FrontRoomsMapHunter.StationGate.Left,
            label + "M6: a Relay sent from C2 (" + sentState + ") hunts to the take point after the take (" + released.State + "); a Dormant one stays " + dormant.State + ", its first send hunts there; "
            + "M7's gate at C2 " + gateNoKey + " without the key (" + gateFallback + " at 150 s), " + gateKey + " with it");
        UnityEngine.Object.DestroyImmediate(playerGo);

        // 7b + 6b. After the take: the wayfinding's revision moved and the locked inner door is a shut opening; the floor key
        //     opens the inner door from either side (any zone), DoorUnlocked naming the floor key.
        // The take's rebuild of the key's chunk may have built the inner door again: the door as it stands now.
        inner = world.DoorBetween(inner.a, inner.b);
        if (inner == null) { Check(false, label + "the inner door is gone after the rebuild"); return; }
        var revAfter = way.Revision;
        var costAfter = way.Crossing(inner.a, inner.b, out _);
        var fromB = world.CellCenter(inner.b) + Vector3.up * .05f;
        var keyKindA = world.Kind(inner.leaf, innerFrom, out var keyZoneA, out var keyHaveA);
        var keyKindB = world.Kind(inner.leaf, fromB, out var keyZoneB, out var keyHaveB);
        var innerWordsKey = world.Describe(inner.leaf, out _);
        world.KeyTurnOwner = null;
        order.Clear();
        unlockZones.Clear();
        var innerOpen = world.Use(inner.leaf);
        Check(revAfter != revBefore && float.IsPositiveInfinity(costBefore) && !float.IsPositiveInfinity(costAfter) && keyKindA == FrontRoomsMapWorld.AimKind.LockedHaveKey
              && keyKindB == FrontRoomsMapWorld.AimKind.LockedHaveKey && keyZoneA == fk && keyZoneB == fk && keyHaveA && keyHaveB && innerWordsKey == FrontRoomsMapWorld.UnlockPrompt
              && innerOpen == FrontRoomsMapWorld.UseResult.Opened && string.Join("", order.ToArray()) == "u" && unlockZones.Count == 1 && unlockZones[0] == fk && world.IsUnlockedForTools(inner),
            label + "after the take the wayfinding moved (revision " + revBefore + " -> " + revAfter + "), the locked inner door costs " + costBefore + " -> " + costAfter + "; the floor key opens it from "
            + "both sides (" + keyKindA + " / " + keyKindB + ", \"" + innerWordsKey + "\"), E " + innerOpen + " with DoorUnlocked naming the floor key");
        world.NoInteriorLocksForTools = true;
        world.DoorsNeedKeysForTools = false;

        // 5. The EXIT with the floor key, from N: Kind LockedHaveKey, "E . UNLOCK"; t 0 with a key-turn owner (the game's
        //    shot): Use is a KeyTurn, held; CommitUnlock lifts the lock (ExitLockChanged(door, false) once) and then
        //    DoorUnlocked names the floor key; ReleaseKeyTurn opens the leaf into X. t 1 with none (tools, the walker):
        //    Use unlocks and opens at once, the same events in the same order. A rebuild keeps it unlocked.
        door = world.ExitDoor;
        eye.position = world.CellCenter(n) + Vector3.up * .05f;
        var haveKind = world.Kind(door.leaf, eye.position, out var haveZone, out var haveHeld);
        var haveWords = world.Describe(door.leaf, out _);
        var keyFor = world.KeyFor(door, eye.position);
        order.Clear();
        unlockZones.Clear();
        lockDoor = null;
        string turnNote;
        bool turnOk;
        if (t == 0)
        {
            world.KeyTurnOwner = new object();
            var turn = world.Use(door.leaf);
            var held = world.IsHeldForKey(door) && world.ExitLocked && order.Count == 0;
            world.CommitUnlock(door);
            var commitOrder = string.Join("", order.ToArray());
            world.ReleaseKeyTurn(door, true, eye.position, true);
            turnOk = turn == FrontRoomsMapWorld.UseResult.KeyTurn && held && commitOrder == "LU";
            turnNote = "E " + turn + " (held, still locked, nothing raised: " + held + "), CommitUnlock raises " + commitOrder;
            world.KeyTurnOwner = null;
        }
        else
        {
            var opened = world.Use(door.leaf);
            turnOk = opened == FrontRoomsMapWorld.UseResult.Opened && string.Join("", order.ToArray()) == "LU";
            turnNote = "E with no key-turn owner " + opened + ", raising " + string.Join("", order.ToArray());
        }
        for (var k = 0; k < 120 && door.angle < FrontRoomsMapWorld.DoorPassableDegrees; k++) world.TickDoorsForTools(Dt);
        var north = world.CellCenter(x) - world.CellCenter(n);
        north.y = 0f;
        north.Normalize();
        Physics.SyncTransforms();
        var leafDepth = Vector3.Dot(door.leaf.bounds.center - world.CrossingPoint(n, x), north);
        var unlockedEdge = world.IsUnlockedForTools(door);
        world.RebuildChunk(MapGrid.ChunkOf(x));
        var rebuilt = world.ExitDoor;
        Check(haveKind == FrontRoomsMapWorld.AimKind.LockedHaveKey && haveZone == fk && haveHeld && haveWords == FrontRoomsMapWorld.UnlockPrompt && keyFor.HasValue && keyFor.Value == fk && turnOk
              && lockDoor == door && unlockZones.Count == 1 && unlockZones[0] == fk && !world.ExitLocked && unlockedEdge && door.angle >= FrontRoomsMapWorld.DoorPassableDegrees && leafDepth > .2f
              && lockSets == 1 && rebuilt != null && world.IsUnlockedForTools(rebuilt) && !world.LockedByRule(rebuilt),
            label + "the EXIT with the floor key: Kind " + haveKind + " (" + (haveZone == fk) + ", held " + haveHeld + "), \"" + haveWords + "\", KeyFor the floor key " + (keyFor.HasValue && keyFor.Value == fk)
            + "; " + turnNote + " (ExitLockChanged(door, false), then DoorUnlocked with the floor key); ExitLocked " + world.ExitLocked + "; the leaf " + door.angle.ToString("0") + " deg, its middle "
            + leafDepth.ToString("0.00") + " m into X; a rebuild keeps it unlocked (" + (rebuilt != null && world.IsUnlockedForTools(rebuilt)) + ")");

        if (t == 0)
        {
            // 12. ClearStation (a refused plan, tools): the rule, the floor key held and FloorHasStation go; zone keys come back
            //     as chunks are built again.
            world.ClearStation();
            for (var cy = -2; cy <= 2; cy++)
            for (var cx = -2; cx <= 2; cx++)
            {
                var coord = new GridCoord(l.DoorChunk.x + cx, l.DoorChunk.y + cy);
                if (world.IsChunkBuiltAt(MapGrid.ChunkOrigin(coord))) world.RebuildChunk(coord);
            }
            CountKeys(world, out var zoneCleared, out var floorCleared, out _);
            Check(!world.FloorKeyRule && !world.FloorKeyHeld && !world.HasKeyFor(fk) && !(world.FloorHasStation && !world.StationKeyIsTaken) && !world.NoInteriorLocksForTools && zoneCleared > 0 && floorCleared == 0
                  && world.KeyZoneAt(l.door) == world.ZoneOf(l.door).id,
                label + "ClearStation: FloorKeyRule " + world.FloorKeyRule + ", the floor key held " + world.HasKeyFor(fk) + ", FloorHasStation " + world.FloorHasStation + ", StationKeyIsTaken " + world.StationKeyIsTaken + "; rebuilt round D: "
                + zoneCleared + " zone keys, " + floorCleared + " floor keys");
            return;
        }

        // 10. NextFloor: the rule, the floor key held and FloorHasStation go, ExitLocked false with no event; floor 2's
        //     zone keys are built as today, and no floor key.
        var setsBefore = lockSets;
        var liftsBefore = order.Count(o => o == "L");
        var seed2 = FrontRoomsExitPlanner.FloorSeed(StationPinSeed, 2, 0);
        var startDoor = new GridCoord(x.x, x.y + 1);
        world.NextFloor(seed2, 2, new RectInt(x.x, x.y, 1, 1), startDoor);
        var nextOk = !world.FloorKeyRule && !world.FloorKeyHeld && !world.HasKeyFor(fk) && !world.FloorHasStation && !world.ExitLocked && world.KeysHeld == 0;
        var p2 = FrontRoomsExitPlanner.ChooseExit(world.Cache, new CellRect(x.x, x.y, 1, 1), startDoor, seed2, FrontRoomsExitPlanner.LaterBandMin, FrontRoomsExitPlanner.LaterBandMax);
        if (p2.found) world.SetExitArea(p2.exitCell);
        eye.position = world.CellCenter(startDoor) + Vector3.up * .05f;
        world.Begin(eye, false);
        world.StreamForTools(eye.position, 0f);
        Physics.SyncTransforms();
        CountKeys(world, out var zone2, out var floor2, out _);
        Check(nextOk && p2.found && !world.ExitLocked && lockSets == setsBefore && order.Count(o => o == "L") == liftsBefore && zone2 > 0 && floor2 == 0 && world.KeyZoneAt(startDoor) == world.ZoneOf(startDoor).id,
            label + "NextFloor: FloorKeyRule, the floor key held, FloorHasStation and ExitLocked false (" + nextOk + "), no lock event; floor 2 (EXIT " + (p2.found ? p2.exitCell.ToString() : "none")
            + ") builds " + zone2 + " zone keys and " + floor2 + " floor keys, its EXIT unlocked");
    }

    static EdgeKind NoGenerate(System.Reflection.MethodInfo method, FrontRoomsMapWorld world, GridCoord a, GridCoord b) =>
        (EdgeKind)method.Invoke(world, new object[] { a, b });

    static ModuleLamp LampOf(FrontRoomsMapCache cache, GridCoord cell)
    {
        var chunk = cache.Get(MapGrid.ChunkOf(cell));
        return chunk.lamp[MapGrid.LocalIndex(cell.x - chunk.Origin.x, cell.y - chunk.Origin.y)];
    }

    // A chunk's edge slots in one array: east (64), north (64), west (8), south (8).
    static EdgeKind[] ChunkEdges(MapChunk chunk)
    {
        const int n = MapGrid.ChunkCells;
        var e = new EdgeKind[2 * n * n + 2 * n];
        Array.Copy(chunk.east, 0, e, 0, n * n);
        Array.Copy(chunk.north, 0, e, n * n, n * n);
        Array.Copy(chunk.west, 0, e, 2 * n * n, n);
        Array.Copy(chunk.south, 0, e, 2 * n * n + n, n);
        return e;
    }

    // The same slots as the overlay reads them (forced first, else the cache's edge), before the carve is set.
    static EdgeKind[] OverlayEdges(FrontRoomsMapCache cache, StartGroupCarve overlay, GridCoord coord)
    {
        const int n = MapGrid.ChunkCells;
        var o = MapGrid.ChunkOrigin(coord);
        var e = new EdgeKind[2 * n * n + 2 * n];
        for (var j = 0; j < n; j++)
        for (var i = 0; i < n; i++)
        {
            var a = new GridCoord(o.x + i, o.y + j);
            e[MapGrid.LocalIndex(i, j)] = overlay.Edge(cache, a, new GridCoord(a.x + 1, a.y));
            e[n * n + MapGrid.LocalIndex(i, j)] = overlay.Edge(cache, a, new GridCoord(a.x, a.y + 1));
        }
        for (var k = 0; k < n; k++)
        {
            e[2 * n * n + k] = overlay.Edge(cache, new GridCoord(o.x - 1, o.y + k), new GridCoord(o.x, o.y + k));
            e[2 * n * n + n + k] = overlay.Edge(cache, new GridCoord(o.x + k, o.y - 1), new GridCoord(o.x + k, o.y));
        }
        return e;
    }

    // Every array of a chunk's data (the module rooms by instance).
    static ulong ChunkDataPrint(MapChunk c)
    {
        fingerprint = 1469598103934665603UL;
        MixInt(c.coord.x); MixInt(c.coord.y); MixInt(c.revision); MixInt(c.tier);
        for (var k = 0; k < MapGrid.CellsPerChunk; k++)
        {
            MixInt(c.zone[k].x); MixInt(c.zone[k].y); MixInt((int)c.height[k]);
            MixInt((int)c.east[k]); MixInt((int)c.north[k]); MixInt((int)c.lamp[k]);
        }
        for (var k = 0; k < MapGrid.ChunkCells; k++) { MixInt((int)c.west[k]); MixInt((int)c.south[k]); }
        for (var k = 0; k < c.pillar.Length; k++) { MixBool(c.pillar[k]); MixInt(c.pillarStyle[k]); }
        MixBool(c.hasKey); MixInt(c.keyCell.x); MixInt(c.keyCell.y); MixInt(c.keySiteCell.x); MixInt(c.keySiteCell.y);
        MixBool(c.keySpot); MixFloat(c.keyX); MixFloat(c.keyZ); MixFloat(c.keyY); MixFloat(c.keyYaw); MixString(c.keyHost);
        MixInt(c.rooms.Length);
        for (var r = 0; r < c.rooms.Length; r++)
        {
            MixInt(c.rooms[r].x); MixInt(c.rooms[r].y); MixInt(c.rooms[r].w); MixInt(c.rooms[r].h);
            var m = c.ModuleOf(r);
            MixInt(m == null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(m));
        }
        return fingerprint;
    }

    // The pieces of c and c' (cells but the station, X and, while it stands, the strip) that are not joined to the map,
    // as this test reads it: a piece over walkable edges with a cell that is not Tall must walk to a cell beyond c and c';
    // a Tall-only piece (left through windows by design) needs glass to such a cell or to a cell that is not Tall. One
    // cell of each piece that is not, or "".
    static string UnjoinedPieces(FrontRoomsMapCache cache, StartGroupLayout l, IReadOnlyList<GridCoord> region, bool stripStands)
    {
        var cells = new List<GridCoord>();
        var inside = new HashSet<GridCoord>();
        foreach (var coord in region)
        {
            var o = MapGrid.ChunkOrigin(coord);
            for (var j = 0; j < MapGrid.ChunkCells; j++)
            for (var i = 0; i < MapGrid.ChunkCells; i++)
            {
                var cell = new GridCoord(o.x + i, o.y + j);
                if (l.InStation(cell) || cell == l.exit || (stripStands && l.InStrip(cell))) continue;
                cells.Add(cell);
                inside.Add(cell);
            }
        }
        bool Beyond(GridCoord cell) => !region.Contains(MapGrid.ChunkOf(cell)) && !(stripStands && l.InStrip(cell));
        Dictionary<GridCoord, int> Label(Func<EdgeKind, bool> joins, List<bool> lower, List<bool> reaches)
        {
            var label = new Dictionary<GridCoord, int>();
            foreach (var start in cells)
            {
                if (label.ContainsKey(start)) continue;
                var id = lower.Count;
                lower.Add(false);
                reaches.Add(false);
                label[start] = id;
                var queue = new Queue<GridCoord>();
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    var cell = queue.Dequeue();
                    if (cache.ZoneOf(cell).height != ZoneHeight.Tall) lower[id] = true;
                    foreach (var step in Steps)
                    {
                        var next = cell + step;
                        if (inside.Contains(next))
                        {
                            if (label.ContainsKey(next) || !joins(cache.Edge(cell, next))) continue;
                            label[next] = id;
                            queue.Enqueue(next);
                        }
                        else if (Beyond(next) && joins(cache.Edge(cell, next))) reaches[id] = true;
                    }
                }
            }
            return label;
        }
        var walkLower = new List<bool>();
        var walkOut = new List<bool>();
        var walk = Label(FrontRoomsExitPlanner.Walkable, walkLower, walkOut);
        var glassLower = new List<bool>();
        var glassOut = new List<bool>();
        var glass = Label(MapGrid.Passable, glassLower, glassOut);
        var seen = new HashSet<int>();
        var problems = new List<string>();
        foreach (var cell in cells)
        {
            var w = walk[cell];
            if (!seen.Add(w)) continue;
            var g = glass[cell];
            if (walkLower[w] ? !walkOut[w] : !(glassLower[g] || glassOut[g])) problems.Add(cell.ToString());
        }
        return string.Join(" ", problems);
    }

    // The front rules as this test reads them (not the planner's code): N Standard; no glass on the edges round N,
    // N + (0, -1) and N + (0, -2) but N-X; none of them between a Tall cell and a lower one inside a chunk other than X's.
    static string FrontRuleProblem(FrontRoomsMapCache cache, GridCoord x)
    {
        var n = new GridCoord(x.x, x.y - 1);
        if (cache.ZoneOf(n).height != ZoneHeight.Standard) return "N " + n + " is " + cache.ZoneOf(n).height;
        var chunkX = MapGrid.ChunkOf(x);
        for (var k = 0; k < 3; k++)
        {
            var cell = new GridCoord(n.x, n.y - k);
            foreach (var s in Steps)
            {
                var other = cell + s;
                if (other == x) continue;
                if (cache.Edge(cell, other) == EdgeKind.Window) return "glass on " + cell + "-" + other;
                var tallCell = cache.ZoneOf(cell).height == ZoneHeight.Tall;
                var tallOther = cache.ZoneOf(other).height == ZoneHeight.Tall;
                var chunk = MapGrid.ChunkOf(cell);
                if (tallCell != tallOther && chunk == MapGrid.ChunkOf(other) && chunk != chunkX) return "a shift could glaze " + cell + "-" + other;
            }
        }
        return null;
    }

    // The corridor rule from the generated edges: N's west and east edges both let the player through.
    static bool OwnCorridor(FrontRoomsMapCache cache, GridCoord n)
    {
        var w = cache.Edge(n, new GridCoord(n.x - 1, n.y));
        var e = cache.Edge(n, new GridCoord(n.x + 1, n.y));
        return w != EdgeKind.Wall && w != EdgeKind.Window && e != EdgeKind.Wall && e != EdgeKind.Window;
    }

    // The same from the edges as built.
    static bool BuiltCorridor(FrontRoomsMapWorld world, GridCoord n)
    {
        var w = world.EffectiveEdge(n, new GridCoord(n.x - 1, n.y));
        var e = world.EffectiveEdge(n, new GridCoord(n.x + 1, n.y));
        return w != EdgeKind.Wall && w != EdgeKind.Window && e != EdgeKind.Wall && e != EdgeKind.Window;
    }

    // Transforms under the map whose name starts with a prefix.
    static int Named(FrontRoomsMapWorld world, string prefix) => world.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith(prefix));

    // ---------- M5: the wall-cue route's station target (tutorial 20_requests.md M5; MAP_ROUTE_SPEC 1, 5; A20) ----------

    /// <summary>
    /// M5 with the wall-cue driver bound to a map (FrontRoomsWayCueDriver.Bind, then TickForTools, as LateUpdate): station
    /// off, nothing changes (no station, the field a fresh build's); on the stand-in station at both parities, before the
    /// take the field's target is the TV's cell (StationBeaconCell, on its north wall: StationBeaconEdge) and every
    /// station cell walks its dirs to it, BeaconPath from D measures to it; the take (StationKeyTaken) turns the key taken
    /// at once and, after one rebuild, the target N with the beacon off; ClearStation (t 0) and NextFloor (t 1) drop the
    /// station from the field at the next step.
    /// </summary>
    static void StartGroupRoute(List<GameObject> roots)
    {
        var profile = FrontRoomsLevelProfiles.Resolve();
        var template = StartGroupTemplate.From(FrontRoomsMapVerification.StandInStation(4), null, out var why);
        if (template == null) { Check(false, "start group route: the stand-in station is refused: " + why); return; }
        StartGroupRouteOff(roots, profile);
        for (var t = 0; t < 2; t++) StartGroupRouteAt(roots, profile, template, t);
    }

    // The cells a walk along the field's dirs takes from a cell to a goal (-1: it never gets there in 64 steps).
    static int RouteSteps(FrontRoomsWayfinding way, GridCoord from, GridCoord goal)
    {
        var c = from;
        for (var i = 0; i <= 64; i++)
        {
            if (c == goal) return i;
            if (!way.Route(c, out var d)) return -1;
            c = new GridCoord(c.x + FrontRoomsWayfinding.DirX[d], c.y + FrontRoomsWayfinding.DirY[d]);
        }
        return -1;
    }

    static void StartGroupRouteOff(List<GameObject> roots, FrontRoomsLevelProfile profile)
    {
        const string label = "start group route, station off: ";
        var world = World(roots, profile, 119f, "MAP INTERACTION TEST / start group route off");
        world.BuildForCapture();
        var driver = world.gameObject.AddComponent<FrontRoomsWayCueDriver>();
        try
        {
            driver.Bind(world, null);
            driver.TickForTools(Dt);
            var way = driver.Way;
            var fresh = new FrontRoomsWayfinding(new FrontRoomsWayfinding.WorldMap(world));
            fresh.SetExit(world.HasExitArea ? world.ExitCell : (GridCoord?)null);
            fresh.Rebuild();
            var taken = world.TakeStationKey();
            driver.TickForTools(Dt);
            Check(!world.FloorHasStation && !world.HasStation && world.StationBeaconEdge == -1 && !driver.StationFromMap && !way.Station.HasValue && !way.StationKeyTaken
                  && !way.TargetIsStation && way.Hash() == fresh.Hash() && !taken,
                label + "FloorHasStation " + world.FloorHasStation + ": the driver takes no station (from the map " + driver.StationFromMap + ", station " + (way.Station.HasValue ? way.Station.Value.ToString() : "none")
                + "), its field is a fresh build's (hash " + way.Hash() + " / " + fresh.Hash() + ", target " + (way.HasTarget ? way.Target.ToString() : "none") + "); TakeStationKey " + taken
                + ", the key still untaken in the field");
        }
        finally { driver.Unbind(); }
    }

    static void StartGroupRouteAt(List<GameObject> roots, FrontRoomsLevelProfile profile, StartGroupTemplate template, int t)
    {
        var label = "start group route, t " + t + ": ";
        var doorX = Mathf.FloorToInt(FrontRooms3DGame.TitleCenterX / MapGrid.CellSize);
        var world = World(roots, profile, 121f + 2f * t, "MAP INTERACTION TEST / start group route t" + t);
        world.BuildForCapture();
        var eye = world.Player;
        world.NextFloor(StationPinSeed, 1, new RectInt(0, 0, 0, 0), default(GridCoord));
        var plan = FrontRoomsStartGroup.Plan(world.Cache, template, new GridCoord(doorX, 2 + 4 * t));
        if (!plan.Ready) { Check(false, label + "no plan on the map's cache: " + plan.error); return; }
        var l = plan.layout;
        world.SetStartArea(new RectInt(l.door.x - FrontRoomsStartGroup.StripHalfCells, l.door.y - 8, FrontRoomsStartGroup.StripHalfCells * 2 + 1, 8), l.door);
        var module = FrontRoomsMapVerification.StandInStation(4);
        if (!world.SetStation(plan, module)) { Check(false, label + "SetStation refused the plan"); return; }
        world.SetExitArea(plan.exit.exitCell);
        world.SetExitLocked(true);
        eye.position = world.CellCenter(l.door) + Vector3.up * .05f;
        world.Begin(eye, false);
        world.StreamForTools(eye.position, 0f);
        Physics.SyncTransforms();
        var n = l.front;
        var x = l.exit;
        var driver = world.gameObject.AddComponent<FrontRoomsWayCueDriver>();
        try
        {
            driver.Bind(world, null);
            var boundStation = driver.Way.Station;
            driver.TickForTools(Dt);
            var way = driver.Way;

            // 1. Before the take: the TV's cell (the stand-in's beacon: x 9.35 m, 0.4 m under the north wall, facing south, so
            //    the north-east cell, on its +z wall), the target, its dir at the wall, every station cell walking to it, and
            //    BeaconPath from D to it (0 m in the TV's cell).
            var tv = world.StationBeaconCell;
            var edge = world.StationBeaconEdge;
            var expected = new GridCoord(l.station.x + 3, l.station.y + l.station.h - 1);
            var moduleOk = module.StationBeaconCell(out var bi, out var bj, out var be) && bi == 3 && bj == 3 && be == 1;
            var tvRouted = way.Route(tv, out var tvDir);
            var cells = 0;
            var maxSteps = 0;
            var stray = new List<string>();
            for (var j = l.station.y; j < l.station.y + l.station.h; j++)
            for (var i = l.station.x; i < l.station.x + l.station.w; i++)
            {
                var c = new GridCoord(i, j);
                var s = RouteSteps(way, c, tv);
                cells++;
                if (s < 0) stray.Add(c.ToString());
                else maxSteps = Mathf.Max(maxSteps, s);
            }
            var beacon = way.BeaconPath(l.door, out var metres, out var shut, out var portal);
            var atTv = way.BeaconPath(tv, out var tvMetres, out _, out _);
            var orthogonal = Mathf.Abs(tv.x - l.door.x) + Mathf.Abs(tv.y - l.door.y);
            Check(boundStation == tv && driver.StationFromMap && tv == expected && moduleOk && edge == 1 && way.Station == tv && !way.StationKeyTaken && way.TargetIsStation && way.Target == tv
                  && tvRouted && tvDir == 1 && stray.Count == 0 && beacon && metres > 0f && metres <= orthogonal * MapGrid.CellSize + 1e-3f && shut == 0 && atTv && tvMetres < 1e-3f,
                label + "before the take the driver holds the map's station from Bind (" + (boundStation.HasValue ? boundStation.Value.ToString() : "none") + "): the TV's cell " + tv + " (the north-east cell "
                + expected + "; the module's beacon cell (" + bi + ", " + bj + ") edge " + be + "), edge " + edge + " (+z); the target " + (way.TargetIsStation ? "the TV " : "N ") + way.Target
                + ", its dir " + tvDir + " at the TV's wall; " + (cells - stray.Count) + "/" + cells + " station cells walk the dirs to the TV (at most " + maxSteps + " steps"
                + (stray.Count > 0 ? "; NOT: " + string.Join(", ", stray) : "") + "); BeaconPath from D " + l.door + ": " + (beacon ? metres.ToString("0.00") + " m, " + shut + " shut openings, portal "
                + world.transform.InverseTransformPoint(portal).ToString("F2") : "none") + ", from the TV's cell " + (atTv ? tvMetres.ToString("0.00") + " m" : "none"));

            // 2. The take (the map's StationKeyTaken): the key taken in the field at once, then one rebuild leads every station
            //    cell and D to N, with the beacon off.
            var revision = way.Revision;
            var taken = world.TakeStationKey();
            var heard = way.StationKeyTaken;
            driver.TickForTools(Dt);
            var beaconAfter = way.BeaconPath(l.door, out _, out _, out _);
            var toN = RouteSteps(way, l.door, n);
            var e1ToN = RouteSteps(way, l.e1In, n);
            var straysAfter = 0;
            for (var j = l.station.y; j < l.station.y + l.station.h; j++)
            for (var i = l.station.x; i < l.station.x + l.station.w; i++)
                if (RouteSteps(way, new GridCoord(i, j), n) < 0) straysAfter++;
            var nRouted = way.Route(n, out var nDir);
            Check(taken && heard && way.StationKeyTaken && way.Revision == revision + 1 && !way.TargetIsStation && way.HasTarget && way.Target == n && way.Station == tv && !beaconAfter
                  && toN == l.station.h - 1 + l.corridor && e1ToN == l.corridor && straysAfter == 0 && nRouted && nDir == 1,
                label + "the take (TakeStationKey " + taken + "): the field's key taken at once " + heard + ", one rebuild (revision " + revision + " -> " + way.Revision + "), the target "
                + (way.TargetIsStation ? "STILL the TV " : "N ") + way.Target + " (N " + n + ", X " + x + "), BeaconPath " + (beaconAfter ? "STILL ON" : "off") + "; D walks to N in " + toN
                + " steps (H - 1 + k = " + (l.station.h - 1 + l.corridor) + "), E1 in " + e1ToN + " (k " + l.corridor + "); " + straysAfter + " station cells do not reach N; N's dir " + nDir + " (into X)");

            // 3. The map lets its station go: ClearStation (t 0) or NextFloor (t 1). The next step drops it from the field, the
            //    key untaken, and the field no longer leads to the TV.
            string how;
            if (t == 0)
            {
                world.ClearStation();
                how = "ClearStation";
            }
            else
            {
                var seed2 = FrontRoomsExitPlanner.FloorSeed(StationPinSeed, 2, 0);
                world.NextFloor(seed2, 2, new RectInt(x.x, x.y, 1, 1), new GridCoord(x.x, x.y + 1));
                how = "NextFloor";
            }
            driver.TickForTools(Dt);
            Check(!world.FloorHasStation && !world.HasStation && !driver.StationFromMap && !way.Station.HasValue && !way.StationKeyTaken && !way.TargetIsStation,
                label + how + ": FloorHasStation " + world.FloorHasStation + "; the field's station " + (way.Station.HasValue ? way.Station.Value.ToString() : "none") + ", key taken " + way.StationKeyTaken
                + ", from the map " + driver.StationFromMap + ", the target " + (way.HasTarget ? (way.TargetIsStation ? "the TV " : "N ") + way.Target : "none"));
        }
        finally { driver.Unbind(); }
    }

    // Any object's box in map space, over every mesh under it (both LODs).
    static void MeshBox(FrontRoomsMapWorld world, Transform root, out Vector3 lo, out Vector3 hi)
    {
        lo = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        hi = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            LocalBox(world, mf.transform, mf.sharedMesh, out var a, out var b);
            lo = Vector3.Min(lo, a);
            hi = Vector3.Max(hi, b);
        }
    }

    // A door's head trim: its top corner on the hinge side, on the face toward the hinge's +X, in its chunk root's frame
    // (BuildEdge: the head is (width + 2 TrimFace) along, WallThickness + 2 TrimProud across, from DoorHeight up by TrimFace).
    static Vector3 HeadCorner(FrontRoomsMapWorld.Door door) =>
        door.hinge.localPosition + door.closed * new Vector3(ModuleUnits.WallHalf + ModuleUnits.TrimProud, ModuleUnits.DoorHeight + ModuleUnits.TrimFace, -ModuleUnits.TrimFace);

    // Renderers under a root (only those under a "Block ..." object, with blocksOnly) in one material whose mesh has a vertex at a point of the root's frame.
    static List<MeshRenderer> RenderersWithVertex(Transform root, Vector3 local, string material, bool blocksOnly)
    {
        var found = new List<MeshRenderer>();
        var point = root.TransformPoint(local);
        foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (blocksOnly && (r.transform.parent == null || !r.transform.parent.name.StartsWith("Block "))) continue;
            if (r.sharedMaterial == null || r.sharedMaterial.name != material) continue;
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;
            var p = mf.transform.InverseTransformPoint(point);
            foreach (var v in mf.sharedMesh.vertices)
                if ((v - p).sqrMagnitude < 1e-6f) { found.Add(r); break; }
        }
        return found;
    }

    /// <summary>
    /// C23 round 2 (10_directions 1.2, the strip fix): the EXIT door's frame (its jambs and head trim) is a renderer of
    /// its own, "EXIT frame N-X", in the trim material, that casts no shadow and still receives them; its head is in
    /// that mesh and in no block's trim mesh. An ordinary door near N keeps its frame in its block's trim mesh, which
    /// casts shadows as before; the map has one EXIT frame.
    /// </summary>
    static void FrameChecks(FrontRoomsMapWorld world, Transform chunkRoot, GridCoord n, GridCoord x, string label)
    {
        var door = world.ExitDoor;
        var frame = chunkRoot.Find("EXIT frame " + n + "-" + x);
        int renderers = 0, casters = 0, vertices = 0;
        var receives = true;
        var material = string.Empty;
        if (frame != null)
            foreach (var r in frame.GetComponentsInChildren<MeshRenderer>(true))
            {
                renderers++;
                if (r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off) casters++;
                receives &= r.receiveShadows;
                if (r.sharedMaterial != null) material = r.sharedMaterial.name;
                var mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) vertices += mf.sharedMesh.vertexCount;
            }
        if (door == null || frame == null || renderers == 0) { Check(false, label + "the EXIT frame: none built (door " + (door != null) + ", frame " + (frame != null) + ")"); return; }
        var corner = HeadCorner(door);
        var inFrame = RenderersWithVertex(frame, frame.InverseTransformPoint(chunkRoot.TransformPoint(corner)), material, false).Count;
        var inBlocks = RenderersWithVertex(chunkRoot, corner, material, true).Count;
        // An ordinary door: the nearest built one to N that is not the EXIT.
        FrontRoomsMapWorld.Door other = null;
        for (var r = 1; r <= 12 && other == null; r++)
            for (var dy = -r; dy <= r && other == null; dy++)
            for (var dx = -r; dx <= r && other == null; dx++)
            {
                if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                var c = new GridCoord(n.x + dx, n.y + dy);
                foreach (var step in new[] { new GridCoord(1, 0), new GridCoord(0, 1) })
                {
                    var d = world.DoorBetween(c, c + step);
                    if (d != null && !world.IsExitDoor(d) && d.hinge != null) { other = d; break; }
                }
            }
        var otherBlocks = other != null ? RenderersWithVertex(other.hinge.parent, HeadCorner(other), material, true) : new List<MeshRenderer>();
        var otherCast = otherBlocks.Count > 0 && otherBlocks.All(r => r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.On);
        var otherOwn = other != null && other.hinge.parent.Find("EXIT frame " + other.a + "-" + other.b) != null;
        var frames = Named(world, "EXIT frame ");
        Check(casters == 0 && receives && vertices == 72 && inFrame == 1 && inBlocks == 0 && other != null && otherBlocks.Count == 1 && otherCast && !otherOwn && frames == 1,
            label + "the EXIT frame " + frame.name + ": " + renderers + " renderer in " + material + ", " + vertices + " vertices (2 jambs and the head), " + casters + " casting a shadow, receiving ("
            + receives + "); its head's corner in that mesh (" + inFrame + ") and in no block trim mesh (" + inBlocks + "); an ordinary door "
            + (other != null ? other.a + "-" + other.b : "(none found)") + " keeps its frame in its block's trim mesh (" + otherBlocks.Count + "), casting (" + otherCast + "), no frame of its own ("
            + !otherOwn + "); " + frames + " EXIT frame under the map");
    }

    /// <summary>
    /// C23 round 2 (10_directions 1.2): Kit_ExitDevice_Crossbar on the EXIT leaf's push face, N's side: its back on the
    /// leaf's N face, 1.0 m up, its latch case 0.1 m in from the leaf's latch edge, the bar along the leaf, no colliders,
    /// under the door's hinge. Opened from N, it swings with the leaf into X (the same place on the leaf, the same turn);
    /// a jolt carries it with the leaf; shut again, it is back where it was. Returns its place shut, in the map's frame.
    /// </summary>
    static Vector3 CrossbarChecks(FrontRoomsMapWorld world, GridCoord n, GridCoord x, string label)
    {
        var door = world.ExitDoor;
        var bar = world.ExitCrossbar;
        if (!FrontRoomsKitLibrary.Has(FrontRoomsMapWorld.ExitCrossbarKit)) { Check(false, label + "the crossbar: the kit " + FrontRoomsMapWorld.ExitCrossbarKit + " is missing"); return Vector3.zero; }
        if (door == null || bar == null) { Check(false, label + "the crossbar: none on the EXIT leaf (door built " + (door != null) + ")"); return Vector3.zero; }
        // Everything in the map's frame (ToMap): exact, where world positions this far out keep only a millimetre.
        var line = x.y * MapGrid.CellSize;
        var leafFace = line - ModuleUnits.DoorLeafThickness * .5f;
        MeshBox(world, bar, out var lo, out var hi);
        var chunkM = ToMap(world, door.hinge.parent);
        var along = chunkM.MultiplyVector(door.closed * Vector3.forward).normalized;
        var hingeAt = chunkM.MultiplyPoint3x4(door.hinge.localPosition);
        var latchEdge = hingeAt + along * (ModuleUnits.DoorWidth - ModuleUnits.DoorLeafGap * .5f);
        var barM = ToMap(world, bar);
        var barAt = barM.MultiplyPoint3x4(Vector3.zero);
        var inset = Vector3.Dot(latchEdge - barAt, along);
        var backGap = leafFace - hi.z;
        var proud = leafFace - lo.z;
        var colliders = bar.GetComponentsInChildren<Collider>(true).Length;
        var onHinge = bar.IsChildOf(door.hinge);
        var placed = onHinge && colliders == 0 && Mathf.Abs(backGap) <= .001f && proud > .05f && proud < .15f && Mathf.Abs(barAt.y - ModuleUnits.DoorHandleHeight) <= .001f
            && Mathf.Abs((lo.y + hi.y) * .5f - ModuleUnits.DoorHandleHeight) <= .005f && Mathf.Abs(inset - FrontRoomsMapWorld.ExitCrossbarLatchInset) <= .001f
            && lo.x >= hingeAt.x + ModuleUnits.DoorLeafGap * .5f - 1e-3f && hi.x <= hingeAt.x + ModuleUnits.DoorWidth - ModuleUnits.DoorLeafGap * .5f + 1e-3f;

        // Open it from N and let it come to rest on its stop; then a jolt; then shut it again. Each time the crossbar
        // must stand where the leaf carries it: its place and axes from the leaf's frame as it was shut.
        var leaf = door.leaf.transform;
        var leafM = ToMap(world, leaf);
        var onLeaf = leafM.inverse * barM;
        var opened = world.TryOpenDoor(n, x);
        for (var k = 0; k < 600 && door.angle < ModuleUnits.DoorSwingDegrees - .05f; k++) world.TickDoorsForTools(Dt);
        for (var k = 0; k < 120; k++) world.TickDoorsForTools(Dt);
        var openAngle = door.angle;
        float follow, turn;
        Carried(world, leaf, bar, onLeaf, out follow, out turn);
        var openAt = ToMap(world, bar).MultiplyPoint3x4(Vector3.zero);
        var swung = (openAt - barAt).magnitude;
        var intoX = openAt.z - line;
        var leafBefore = leaf.localPosition;
        world.JoltLeaf(door, .03f, 2f, .5f);
        for (var k = 0; k < 6; k++) world.TickDoorsForTools(Dt);
        var jolted = (leaf.localPosition - leafBefore).magnitude;
        float joltFollow, joltTurn;
        Carried(world, leaf, bar, onLeaf, out joltFollow, out joltTurn);
        world.ResetDoorForTools(door);
        var shutAgain = (ToMap(world, bar).MultiplyPoint3x4(Vector3.zero) - barAt).magnitude;
        Check(placed,
            label + "the crossbar (" + bar.name + ") on the EXIT leaf's push face, N's side: its back " + backGap.ToString("0.0000") + " m off the leaf's N face, " + proud.ToString("0.000")
            + " m proud into N; its latch case " + barAt.y.ToString("0.000") + " m up (the bar " + lo.y.ToString("0.000") + "-" + hi.y.ToString("0.000") + "), " + inset.ToString("0.000")
            + " m in from the leaf's latch edge, spanning " + (lo.x - hingeAt.x).ToString("0.000") + "-" + (hi.x - hingeAt.x).ToString("0.000") + " m from the hinge jamb; " + colliders
            + " colliders; under the door's hinge (" + onHinge + ")");
        Check(opened && openAngle > 90f && follow < 1e-4f && turn < .05f && swung > .5f && intoX > .5f && jolted > 1e-3f && joltFollow < 1e-4f && joltTurn < .05f && shutAgain < 1e-5f,
            label + "the crossbar follows the leaf: opened from N (" + opened + ") to " + openAngle.ToString("0.0") + " degrees it stays put on the leaf (" + follow.ToString("0.000000") + " m, "
            + turn.ToString("0.000") + " degrees off), swung " + swung.ToString("0.000") + " m, now " + intoX.ToString("0.000") + " m into X; a jolt moves the leaf " + jolted.ToString("0.000")
            + " m and the crossbar with it (" + joltFollow.ToString("0.000000") + " m, " + joltTurn.ToString("0.000") + " degrees off); shut again it is back (" + shutAgain.ToString("0.000000") + " m)");
        return barAt;
    }

    // Where the leaf carries a part now (its pose on the leaf, onLeaf, taken shut), against where the part is: metres
    // between the two origins and degrees between their axes, all in the map's frame.
    static void Carried(FrontRoomsMapWorld world, Transform leaf, Transform part, Matrix4x4 onLeaf, out float metres, out float degrees)
    {
        var expected = ToMap(world, leaf) * onLeaf;
        var actual = ToMap(world, part);
        metres = (expected.MultiplyPoint3x4(Vector3.zero) - actual.MultiplyPoint3x4(Vector3.zero)).magnitude;
        degrees = Mathf.Max(Vector3.Angle(expected.MultiplyVector(Vector3.right), actual.MultiplyVector(Vector3.right)),
            Vector3.Angle(expected.MultiplyVector(Vector3.forward), actual.MultiplyVector(Vector3.forward)));
    }

    // The sign's box in map space, over every mesh of it (both LODs).
    static void SignBox(FrontRoomsMapWorld world, FrontRoomsExitSign sign, out Vector3 lo, out Vector3 hi)
    {
        lo = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        hi = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        foreach (var mf in sign.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            LocalBox(world, mf.transform, mf.sharedMesh, out var a, out var b);
            lo = Vector3.Min(lo, a);
            hi = Vector3.Max(hi, b);
        }
    }

    static void LocalBox(FrontRoomsMapWorld world, Transform t, Mesh mesh, out Vector3 lo, out Vector3 hi)
    {
        var b = mesh.bounds;
        lo = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        hi = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        var toMap = ToMap(world, t);
        for (var k = 0; k < 8; k++)
        {
            var corner = b.center + Vector3.Scale(b.extents, new Vector3((k & 1) == 0 ? -1f : 1f, (k & 2) == 0 ? -1f : 1f, (k & 4) == 0 ? -1f : 1f));
            var p = toMap.MultiplyPoint3x4(corner);
            lo = Vector3.Min(lo, p);
            hi = Vector3.Max(hi, p);
        }
    }

    /// <summary>
    /// A transform's matrix into the map's frame: its local transforms chained up to the map root, exact at any world
    /// offset (the test maps sit 12 km out, where a world position keeps only about a millimetre); through world space
    /// for one that is not under the map.
    /// </summary>
    static Matrix4x4 ToMap(FrontRoomsMapWorld world, Transform t)
    {
        if (!t.IsChildOf(world.transform)) return world.transform.worldToLocalMatrix * t.localToWorldMatrix;
        var m = Matrix4x4.identity;
        for (var c = t; c != world.transform; c = c.parent) m = Matrix4x4.TRS(c.localPosition, c.localRotation, c.localScale) * m;
        return m;
    }

    static readonly Collider[] overlapBuffer = new Collider[64];

    // The map's walls, doors and windows that any enabled box of the stairwell overlaps (each box 1 mm smaller all round: touching is not overlapping).
    static List<string> MapOverlaps(FrontRoomsMapWorld world, FrontRoomsStairwell stairwell)
    {
        var found = new List<string>();
        foreach (var box in stairwell.GetComponentsInChildren<BoxCollider>(true))
        {
            if (!box.enabled) continue;
            var count = OverlapOf(box);
            for (var i = 0; i < count; i++)
                if (world.IsArchitecture(overlapBuffer[i])) found.Add(box.name + " / " + overlapBuffer[i].name);
        }
        return found;
    }

    // What a box collider overlaps (1 mm smaller all round), itself left out; with stairwellOnly, only the stairwell's own colliders.
    static List<string> BoxOverlaps(BoxCollider box, FrontRoomsStairwell stairwell, bool stairwellOnly)
    {
        var found = new List<string>();
        if (box == null) { found.Add("no box collider"); return found; }
        var count = OverlapOf(box);
        for (var i = 0; i < count; i++)
        {
            var other = overlapBuffer[i];
            if (other == box || (stairwellOnly && !other.transform.IsChildOf(stairwell.transform))) continue;
            found.Add(other.name);
        }
        return found;
    }

    static int OverlapOf(BoxCollider box)
    {
        var t = box.transform;
        var half = Vector3.Scale(box.size, t.lossyScale) * .5f - Vector3.one * .001f;
        return Physics.OverlapBoxNonAlloc(t.TransformPoint(box.center), half, overlapBuffer, t.rotation, ~0, QueryTriggerInteraction.Ignore);
    }

    static bool InCell(FrontRoomsMapWorld world, GridCoord cell, Vector3 world3, float inset)
    {
        var p = world.transform.InverseTransformPoint(world3);
        var cs = MapGrid.CellSize;
        return p.x > cell.x * cs + inset && p.x < (cell.x + 1) * cs - inset && p.z > cell.y * cs + inset && p.z < (cell.y + 1) * cs - inset;
    }

    // The map's exit group, worked out again from the lamp's cell and range (FrontRoomsMapWorld.ExitLampGroup's rule).
    static bool InExitGroup(FrontRoomsMapWorld world, GridCoord x, Light light)
    {
        var cell = world.CellOf(light.transform.position);
        var cs = MapGrid.CellSize;
        var cx = (cell.x + .5f) * cs;
        var cz = (cell.y + .5f) * cs;
        var dx = Mathf.Max(x.x * cs - cx, 0f, cx - (x.x + 1) * cs);
        var dz = Mathf.Max(x.y * cs - cz, 0f, cz - (x.y + 1) * cs);
        return dx * dx + dz * dz < light.range * light.range;
    }

    // Each lamp's intensity while lit, -1 while off.
    static Dictionary<Light, float> LampSnapshot(Light[] lamps)
    {
        var snapshot = new Dictionary<Light, float>();
        foreach (var l in lamps) snapshot[l] = l.enabled ? l.intensity : -1f;
        return snapshot;
    }

    static Transform ChunkRoot(FrontRoomsMapWorld world, GridCoord coord)
    {
        var prefix = "Chunk " + coord + " ";
        foreach (Transform t in world.transform) if (t.name.StartsWith(prefix)) return t;
        return null;
    }

    // ---------- Build fingerprints (C22): what each chunk built, without the lamps' running state ----------

    static ulong fingerprint;
    static readonly MaterialPropertyBlock fingerprintBlock = new MaterialPropertyBlock();

    static void Mix(ulong v)
    {
        fingerprint ^= v;
        fingerprint *= 1099511628211UL;
        fingerprint ^= fingerprint >> 29;
    }

    static void MixFloat(float f) => Mix((ulong)(uint)BitConverter.SingleToInt32Bits(f));
    static void MixInt(int i) => Mix((ulong)(uint)i);
    static void MixBool(bool b) => Mix(b ? 11UL : 13UL);
    static void MixVector(Vector3 v) { MixFloat(v.x); MixFloat(v.y); MixFloat(v.z); }

    static void MixString(string s)
    {
        if (s == null) { Mix(17UL); return; }
        foreach (var c in s) Mix(c);
        Mix(0xffUL);
    }

    /// <summary>
    /// A fingerprint of every chunk under the map, by its object's name: each object's name, place and
    /// components, every mesh's vertices, normals, UVs and triangles, the colliders, each lamp as built (type,
    /// range, angles, colour; not the lit state and intensity the tick sets), the renderers' materials and
    /// ceiling height (not the lens glow), and the outlet sets' faces and fixtures. Two builds of one seed with
    /// the same reserved cells agree exactly.
    /// </summary>
    static Dictionary<string, ulong> ChunkFingerprints(FrontRoomsMapWorld world)
    {
        var result = new Dictionary<string, ulong>();
        foreach (Transform t in world.transform)
        {
            if (!t.name.StartsWith("Chunk ")) continue;
            fingerprint = 1469598103934665603UL;
            Fingerprint(t);
            result[t.name] = fingerprint;
        }
        return result;
    }

    static void Fingerprint(Transform t)
    {
        MixString(t.name);
        MixBool(t.gameObject.activeSelf);
        MixVector(t.localPosition);
        MixFloat(t.localRotation.x); MixFloat(t.localRotation.y); MixFloat(t.localRotation.z); MixFloat(t.localRotation.w);
        MixVector(t.localScale);
        foreach (var c in t.GetComponents<Component>())
        {
            if (c == null) { MixString("missing component"); continue; }
            MixString(c.GetType().Name);
            if (c is MeshFilter mf) FingerprintMesh(mf.sharedMesh);
            else if (c is MeshCollider mc) { MixBool(mc.enabled); FingerprintMesh(mc.sharedMesh); }
            else if (c is BoxCollider bc) { MixBool(bc.enabled); MixBool(bc.isTrigger); MixVector(bc.center); MixVector(bc.size); }
            else if (c is Collider col) { MixBool(col.enabled); MixBool(col.isTrigger); }
            else if (c is Light light)
            {
                MixInt((int)light.type); MixFloat(light.range); MixFloat(light.spotAngle); MixFloat(light.innerSpotAngle);
                MixFloat(light.color.r); MixFloat(light.color.g); MixFloat(light.color.b);
            }
            else if (c is Renderer r)
            {
                MixBool(r.enabled);
                MixInt((int)r.shadowCastingMode);
                foreach (var m in r.sharedMaterials) MixString(m != null ? m.name : null);
                if (r.HasPropertyBlock())
                {
                    r.GetPropertyBlock(fingerprintBlock);
                    MixFloat(fingerprintBlock.GetFloat("_CeilingHeight"));
                }
            }
            else if (c is FrontRoomsWallFixtureSet set)
            {
                MixInt(set.coord.x); MixInt(set.coord.y); MixInt(set.seed); MixInt(set.faces.Count); MixInt(set.fixtures.Count); MixInt(set.extraCount);
                foreach (var f in set.faces)
                {
                    MixVector(f.start); MixVector(f.direction); MixFloat(f.length); MixVector(f.normal); MixInt((int)f.kind);
                    MixFloat(f.openFrom); MixFloat(f.openTo); MixFloat(f.openBottom); MixFloat(f.openTop);
                    Mix((ulong)f.edgeId); MixInt(f.side); MixInt(f.cell.x); MixInt(f.cell.y); MixBool(f.foreign); MixInt(f.room);
                }
                foreach (var f in set.fixtures)
                {
                    MixInt((int)f.kit); MixVector(f.position); MixVector(f.normal); MixFloat(f.roll); MixFloat(f.tilt); MixInt((int)f.cls); MixInt(f.face);
                }
            }
        }
        for (var i = 0; i < t.childCount; i++) Fingerprint(t.GetChild(i));
    }

    static void FingerprintMesh(Mesh m)
    {
        if (m == null) { MixString("no mesh"); return; }
        MixString(m.name);
        MixInt(m.vertexCount);
        foreach (var v in m.vertices) MixVector(v);
        foreach (var v in m.normals) MixVector(v);
        foreach (var u in m.uv) { MixFloat(u.x); MixFloat(u.y); }
        foreach (var i in m.triangles) MixInt(i);
    }
}
