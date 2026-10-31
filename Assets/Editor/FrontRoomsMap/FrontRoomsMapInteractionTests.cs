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
///   into the far cell and rests at 80°, 3° crooked out of plane + 0.6° sag (M3, pose (a)); the hinge never turns
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
///   will, the stand-in station): the onboarding's gate (Red 2026-10-30, the 3-argument StationSendDue: Waiting at 149
///   cells explored and at 179.9 s, Explored at 150 cells, Fallback at 180 s, the key taken changing nothing, Waiting with no
///   station); the tools' gate (-autopilotOnboardOff, the 5-argument StationSendDue) at path distance 0, 1 and 2 through E1
///   and through E2, with and without the key, the key needed or not: Dormant / Dormant / sent (never sent without the key
///   while it is needed), sent at 150 s wherever the player is, never from the stream room; A15's third-zone rung retired
///   (never fires); every send outside the station, its ring, the strip and the band round the walk
///   to the EXIT, rung 1 within 8-10 walking cells on the far side, the first leg a Hunt to the take point with the key and
///   Listen without; rungs forced empty walk the ladder down to today's Arrive; 300 forced releases (the ladder, today's
///   Arrive and the leash relay) never land in the station or its ring; the grace (no chase beyond 6 m before the first
///   lamp dip); M6's seam (TakeStationKey raises StationKeyTaken once, with the KeySpot on the floor; HuntStation sends a
///   released Relay there, round the riser block); ClearStation forgets the key. The onboarding's run-level first send (Red
///   2026-10-30): ReleaseNow with a policy sets FirstSendDone and OnFirstSend, a plain release neither, DebugPlace and Park
///   clear OnFirstSend only; on floor 2 below the station (NextFloor: the old X the start stairwell, a new X) the generic
///   ladder (FirstSendPolicy.Generic) from cells round the start door, four looks each: unseen, no station layout, rung 1
///   8-10 cells behind the player, rungs 2 and 3 in their bands, never in or within 2 cells (Chebyshev) of either
///   stairwell, rungs forced empty down to Arrive; v1: the Dormant release releaseDelaySeconds after the shut goes through
///   ReleasePolicy (the gentle first send) or, the first send done, Arrive; v2: a FirstSend dispatch with no station takes
///   the generic ladder and the next Normal dispatch clears OnFirstSend.
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
/// - Relay v2 V0 (inert contracts, 2026-10-29): HunterState appends Away 7 / Arrive 8 / Withdraw 9; the tuning's v2
///   loudness table and glass radius, CopyFrom copying every field; the tier table's v2 rows, which v1's ApplyTo never
///   reads; OpenAjar (Passage.Ajar, 30 deg away from the Relay in 1.0 s, DoorOpenBegan(Relay) only, kept by edge, E opens
///   it fully, refused when open, Ajar or broken, ended by a reset, a break or NextFloor); LampOverrideStarted.
/// - Relay v2 V2 (RELAY 8 amended 2026-10-14; computer_ui 20_design 6.1 and 6.6; 2026-10-29), the player's side of sight:
///   the reference frustum (76 deg vertical, 16:9, about +-54 deg across, the look's pitch counting, to SightDistance);
///   RelayInView needs both Relay points (1.6 and 1.95 m) in the frustum and both rays clear (an occluder on either ray
///   hides it); PlayerSeesRelay never reads a device camera (its FOV and aspect change nothing) while RelayOnScreen
///   follows them (16:9 sees a Relay 50 deg off, 4:3 and a 60 deg FOV do not); RelayLit (XZ 16 m, W 0.15 over the cell
///   and its 4 neighbours from map.LampLevel; a dark Relay is still seen, not perceived); PlayerPerceivesRelay; the
///   Threat reading's v1 stand-ins (a chase; sight within 6 m flat); ThreatPerceivable's holds (3 s after a chase,
///   dropped at Dormant; a door break only from a blow heard within 36 m until the break; never SeesPlayer alone).
/// - Relay v2 (step V3a; only with FRONTROOMS_RELAY_V2, the v1 rows kept verbatim without it): RelayV2Loop (Arm,
///   Dispatch and Arrive v2's hard rules, the step-in, Listen then the hunt, the give-up beat, patrol at tier 3 and none
///   under FADE, Withdraw, Away, SendAway, an unbuilt cell, FADE's cautious look, Disarm); the v2 variants of RelayEntry
///   (Arrived at the step-in), RelayPark (no timer: Arm and a call), RelayKeepsShutDoors' wander rows (the patrol) and
///   StartGroupRelay's leash kind (Away on an unbuilt cell); the v2 first send (FirstSend), on floor 1's station and with
///   the generic ladder below it.
/// - Relay v2 (step V3b; only with FRONTROOMS_RELAY_V2): the Ajar push (PushAjar to its rest, passable within 0.4 s, still
///   Ajar by edge; SettleAjar back to 30 deg; E opens it fully); OPEN from either side (licensed at the dispatch, at the
///   face, the leaf away, 1.0 s then the push, the leaf back Ajar once it is through) and the push from the leaf's side
///   (the stance clear of the sweep); "shut the door and walk away ends an investigation" (a WALL: 2 s at the door, the
///   give-up beat with no patrol, Withdraw); a door heard moving in a chase is a BREAK licence, a chase with none meets a
///   WALL; the way round at most 1.5x is taken, beyond it a WALL; Withdraw's one OPEN; RedispatchKind (Handoff, Walk,
///   Queue, NoiseOnly); the handoff and the walk re-dispatch through the director, a trip queued with the budget spent
///   and its QueuedCallOut at the next placement; the quiet-player sweep (Q2); RelayKeepsShutDoors' patrol row without a
///   way round now WALLs (2 s at the door) before the leg is dropped.
/// - V3c (systems design 2026-10-30). C32's W fix (define-agnostic, the first send's ladder is shared): every ladder send
///   in StartGroupRelay offers rung 1-2 cells only at W at most FirstSendMaxW (30 m, WarningDistanceAt), a rung 1-2 send
///   stands at W at most 30 m and report.w is WarningDistanceAt at the placement; FirstSendW: the generic ladder round
///   the spawn with the rule and without it (FirstSendNoWForTools, the same seed): WarningDistanceAt is Tick's W (min(F,
///   3 x straight) of a player-rooted 45 m field), the rule leaves out exactly the cells over 30 m, and a shut door
///   between the player and an otherwise rung-1 cell pushes it over 30 m and it is left out. Only with
///   FRONTROOMS_RELAY_V2: the sweep picks waypoint 1 where it can step in (from the player cells where V3b's sweep ended
///   at the step-in it walks to waypoint 1, every cell it walks at W 21 m or more); a device tripped in a chase is heard
///   and queued (the real hunter and director: no re-dispatch, QueuedCallOut after the Away).
/// - Relay v2 (step V4a; only with FRONTROOMS_RELAY_V2): RelayV4aHearing (k per distance and through a shut opening; k 0 at
///   radius 1000; the side-correct estimate; which noise wins and the 1.5 s limit; a repeat; BreakDoor's slot; a chase
///   without sight moves only the last known point; deaf off the map, FADE, Withdraw's half range; DebugPlace clears;
///   tier hearing; the counts through an Away and a handoff; the game's walk and its loudness table). ExitArea's line
///   noise and RelayV2Loop's two noises follow the new rules.
/// - Relay v2 (step V4b; only with FRONTROOMS_RELAY_V2): RelayV4bSight (the numbers; LookYaw; the calm and chase cones;
///   T(d), freeze, drain; "almost"; the G3 gate; touch, through shut, Ajar and Open doors; the committed hold and C37's
///   1.7 s; re-acquire, a new visit, a handoff; no catch before the chase; the ramp; the lose meter; the 24 m exit; a
///   lock-on during BreakDoor; LoseTrack's order; the full search and the cautious look; A42; Threat's source; the real
///   director; the late additions; RELAY 8's worked estimate). Rows that start from a chase go through ChaseNowV4b.
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
            RelayDoorBreakContract(roots);
            RelayKeepsShutDoors(roots);
            RelayLosesTrack(roots);
            RelaySight(roots);
            Keys(roots, profiles);
            BrokenDoors(roots, profiles);
            Columns(roots, profiles);
            InnerWalls(roots, profiles);
            FillKeepsClear(roots, profiles);
            StartKeepClear(roots, profiles);
            BuildGuard(roots);
            Tiers(roots, profiles);
            RelayV2Contracts(roots);
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
#if FRONTROOMS_RELAY_V2
            RelayV2Loop(roots);
            RelayV3bAjar(roots);
            RelayV3bDoors(roots);
            RelayV3bWallReplan(roots);
            RelayV3bWithdrawOpen(roots);
            RelayV3bCalls(roots);
            RelayV4aHearing(roots);
            RelayV4bSight(roots);
#endif
            ExitSign(roots);
            ExitDoor(roots);
            StartGroupData(roots);
            StartGroupPlanner(roots);
            StartGroupWorld(roots);
            StartGroupRelay(roots);
            FirstSendW(roots);
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
        //    D1.6: a dressed door's rig takes the shift (IFrontRoomsDoorRig.Jolt) and the cube leaf stays home.
        var rigged = door.rig != null;
        world.CommitUnlock(door);
        var shiftedMax = 0f;
        for (var k = 0; k < 12; k++)
        {
            frame++;
            world.TickDoorsForTools(Dt);
            shiftedMax = Mathf.Max(shiftedMax, (door.leaf.transform.localPosition - home).magnitude);
        }
        Check(unlocked.Count == 1 && unlocked[0].door == door && unlocked[0].zone == zone && world.IsUnlockedForTools(door) && world.IsHeldForKey(door) && !door.open
              && (rigged ? shiftedMax == 0f : shiftedMax > FrontRoomsShotTimings.Unlock.LeafShiftMetres * .5f) && door.leaf.transform.localPosition == home,
            label + "CommitUnlock: DoorUnlocked once for this door and zone " + zone + ", unlocked and still held, the " + (rigged ? "rig's leaf jolts (the cube leaf " : "leaf shifts ") + (shiftedMax * 1000f).ToString("0.0") + " mm" + (rigged ? ")" : "") + " and settles home");
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
                && maxAngle >= FrontRoomsMapWorld.DoorBreakThrowDegrees - 1f && maxStep <= HingeStepLimit && Mathf.Abs(tilt - CrookedTilt) < .05f,
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
            && Mathf.Abs(tilt - CrookedTilt) < .05f,
            label + "the leaf keeps its side: thrown to " + maxAngle.ToString("0.0") + "° into " + restCell + " (" + (restCell == a ? "the Relay's cell" : "not the Relay's cell " + a) + "), it rests there at " + door.angle.ToString("0.0") + "°, "
            + tilt.ToString("0.0") + "° crooked");
        Check(maxStep <= HingeStepLimit, label + "the hinge never turns more than " + HingeStepLimit.ToString("0") + "° in a frame (largest " + maxStep.ToString("0.0") + "°)");
    }

    // ---------- The Relay leaving a break ----------

    // M3: the broken leaf's whole turn, pose (a): CrookedDeg out of plane, then the sag.
    static readonly float CrookedTilt = Quaternion.Angle(Quaternion.identity, Quaternion.AngleAxis(FrontRoomsShotTimings.DoorBreak.CrookedDeg, Vector3.forward) * Quaternion.AngleAxis(FrontRoomsShotTimings.DoorBreak.CrookedSagDeg, Vector3.right));

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

    // ---------- The door break contract (游戏视觉's door_break, Red 2026-10-30: "1a，2 要，3 要"): M1, M2, M3, M6, M7 ----------

    static void RelayDoorBreakContract(List<GameObject> roots)
    {
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), -39f, "MAP INTERACTION TEST / door break contract");
        world.BuildForCapture();
        var player = new GameObject("test player").transform;
        player.SetParent(world.transform, false);
        var body = player.gameObject.AddComponent<CapsuleCollider>();
        body.height = ModuleUnits.PlayerHeight;
        body.radius = ModuleUnits.PlayerRadius;
        body.center = Vector3.up * (ModuleUnits.PlayerHeight * .5f);
        // One break: placed behind the door, a noise beyond it, until `until` says stop or the door gives. Blow times are StateTime.
        List<(int index, int count, float at)> Run(FrontRoomsMapHunter h, FrontRoomsMapWorld.Door door, Vector3 target, GridCoord behind, Func<int, bool> until)
        {
            var blows = new List<(int index, int count, float at)>();
            var struck = new List<int>();
            void OnStruck(FrontRoomsMapWorld.Door d, int i, int n, Vector3 from) { Check(d == door && (from - h.Position).sqrMagnitude < .01f, "door break contract: DoorBlowStruck carries the door and where it stands"); struck.Add(i); }
            void OnBlow(Vector3 p) { blows.Add((h.BlowIndex, h.BlowCount, h.StateTime)); Check(struck.Count > 0 && struck[struck.Count - 1] == h.BlowIndex, "door break contract: DoorBlowStruck comes first, with the same blow (" + h.BlowIndex + ")"); }
            h.DoorBlowStruck += OnStruck;
            h.DoorBlow += OnBlow;
            player.position = target;
            Physics.SyncTransforms();
            h.DebugPlace(world.CellCenter(behind));
            h.Noise(target, 1000f);
            for (var t = 0f; t < 25f && !door.broken && !until(blows.Count); t += Dt)
            {
                h.Tick(Dt, target, target + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
                world.TickDoorsForTools(Dt);
                Physics.SyncTransforms();
            }
            h.DoorBlowStruck -= OnStruck;
            h.DoorBlow -= OnBlow;
            return blows;
        }
        void Times(string label, List<(int index, int count, float at)> blows, float[] expected, int count)
        {
            Check(blows.Count == expected.Length && blows.All(x => x.count == count),
                label + blows.Count + " blows of " + (blows.Count > 0 ? blows[0].count : 0) + " (expected " + expected.Length + " of " + count + "): " + string.Join(", ", blows.Select(x => x.index + "@" + x.at.ToString("0.000"))));
            for (var i = 0; i < Mathf.Min(expected.Length, blows.Count); i++)
                Check(Mathf.Abs(blows[i].at - expected[i]) <= Dt * .6f, label + "blow " + blows[i].index + " at " + blows[i].at.ToString("0.000") + " s (expected " + expected[i].ToString("0.000") + ")");
        }

        // M2: three blows, it is taken away; the map keeps them; back at the door it strikes on from the fourth.
        if (!FindDoor(world, out var a, out var b, out var c)) { Check(false, "door break contract: no closed door"); return; }
        var kept = world.DoorBetween(a, b);
        var tuning = new FrontRoomsHunterTuning { sightRange = 0f };
        var hunter = new FrontRoomsMapHunter(world, tuning, body, null, 5);
        var first = Run(hunter, kept, world.CellCenter(b), c, n => n >= 3);
        Check(first.Count == 3 && !kept.broken, "M2: three blows struck, the door still whole (" + first.Count + ")");
        hunter.DebugPlace(world.CellCenter(c));
        Check(world.BlowsKept(kept) == 3 && !world.IsBeingBroken(kept) && hunter.BlowIndex == first[2].index, "M2: leaving keeps the 3 blows on the edge (" + world.BlowsKept(kept) + ")");
        var second = Run(hunter, kept, world.CellCenter(b), c, n => false);
        Times("M2, the resumed break: ", second, new[] { .5f, .9f }, 5);
        Check(second.Count == 2 && second[0].index == 3 && second[1].index == 4 && kept.broken && world.BlowsKept(kept) == 0, "M2: it strikes on from blow 3, the door gives, the kept count goes with it");
        for (var i = 0; i < 60; i++) world.TickDoorsForTools(Dt);
        // M3: the leaf leans about the bottom pivot, about 110 mm at the head (undressed cube leaf, the collider).
        var head = kept.leaf.transform.localPosition + kept.leaf.transform.localRotation * new Vector3(0f, kept.leaf.transform.localScale.y * .5f, 0f);
        var lean = head.x;
        Check(Mathf.Abs(Mathf.Abs(lean) - .11f) < .01f && Mathf.Sign(lean) == Mathf.Sign(kept.swing), "M3: pose (a), the head " + Mathf.Abs(lean * 1000f).ToString("0") + " mm off the hinge jamb");

        // M6: v2 T2 (2.3 s): the last two blows at least 0.3 s apart.
        if (FindDoor(world, out var a2, out var b2, out var c2))
        {
            var t2 = new FrontRoomsHunterTuning { sightRange = 0f, breakDoorSeconds = 2.3f };
            var h2 = new FrontRoomsMapHunter(world, t2, body, null, 5);
            Times("M6, 2.3 s: ", Run(h2, world.DoorBetween(a2, b2), world.CellCenter(b2), c2, n => false), new[] { .5f, 1f, 1.5f, 1.9f, 2.2f }, 5);
        }
        else Check(false, "M6: no second closed door");

        // M7: a locked (steel) door takes 0.5 s longer: 6 blows at T1.
        world.DoorsNeedKeysForTools = true;
        try
        {
            if (FindDoor(world, out var a3, out var b3, out var c3, (d, near) => world.LockedByRule(d)))
            {
                var h3 = new FrontRoomsMapHunter(world, new FrontRoomsHunterTuning { sightRange = 0f }, body, null, 5);
                Times("M7, a locked door: ", Run(h3, world.DoorBetween(a3, b3), world.CellCenter(b3), c3, n => false), new[] { .5f, 1f, 1.5f, 2f, 2.5f, 2.9f }, 6);
            }
            else Check(false, "M7: no third closed door");
        }
        finally { world.DoorsNeedKeysForTools = false; }
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
#if FRONTROOMS_RELAY_V2
                // v2's patrol keeps to its own zone while a leg fits there: a wander's door is one its leg must take (PatrolDoorV2).
                while (!found && FindDoor(world, out a, out b, out c, (d, near) => !tried.Contains(d.edge) && (leg != HunterState.Wander || PatrolDoorV2(world, d, near))
                    && RouteRound(world, near, d.a == near ? d.b : d.a, null)))
#else
                while (!found && FindDoor(world, out a, out b, out c, (d, near) => !tried.Contains(d.edge) && RouteRound(world, near, d.a == near ? d.b : d.a, null)))
#endif
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
#if FRONTROOMS_RELAY_V2
            // v2: a wander's door is one its patrol leg must take (PatrolDoorV2: its near cell in a small room of its own zone).
            else found = FindDoor(world, out a, out b, out c, (d, near) => (leg == HunterState.Wander ? PatrolDoorV2(world, d, near) : !InRoom(world, near))
                && OpenWalk(world, near, d.a == near ? d.b : d.a, 64) < 0);
#else
            else found = FindDoor(world, out a, out b, out c, (d, near) => !InRoom(world, near) && OpenWalk(world, near, d.a == near ? d.b : d.a, 64) < 0);
#endif
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
#if FRONTROOMS_RELAY_V2
            // Tools: how many seeds reached a patrol (Wander), and the last seed's steering.
            var patrols = 0;
            var lastSteering = "";
#endif
            // Where a leg goes is the rng's pick: try seeds (odd, so each is its own rng) until one leaves its start cell
            // through the open door. A wander starts on either side in turn: one side may be a pocket with nowhere 6 cells off.
            for (var i = 0; i < 32 && !shutting; i++)
            {
                seed = 2 * i + 1;
                from = leg == HunterState.Wander && i % 2 == 1 ? b : a;
                to = from == a ? b : a;
                hunter = new FrontRoomsMapHunter(world, new FrontRoomsHunterTuning { sightRange = 0f }, body, null, seed);
#if FRONTROOMS_RELAY_V2
                // Relay v2 (V3a): no idle wander; a Wander is the patrol after the give-up beat (tier 3 and up), so this
                // Relay gets a long patrol: placed with nothing to search it gives up at once, beats 2 s, then patrols.
                hunter.TierV2 = new FrontRoomsTierV2 { patrolSeconds = 600f };
#endif
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
#if FRONTROOMS_RELAY_V2
                if (hunter.State == HunterState.Wander) patrols++;
                lastSteering = hunter.State + " " + hunter.DebugSteering;
#endif
            }
            if (!shutting)
            {
#if FRONTROOMS_RELAY_V2
                Check(false, label + "no " + leg + " leg went through the open door " + a + "->" + b + " in 32 tries (v2: zones " + world.ZoneOf(a).id + " / " + world.ZoneOf(b).id
                    + ", " + patrols + " seeds patrolling, the last: " + lastSteering + ")");
#else
                Check(false, label + "no " + leg + " leg went through the open door " + a + "→" + b + " in 32 tries");
#endif
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
#if FRONTROOMS_RELAY_V2
            // v2: a patrol leg's end starts the next leg (still Wander, no state change): the leg is over on the first frame
            // its next step is no longer the shut door's far cell from its cell; and whether it left that cell afterwards.
            var legDroppedAt = -1f;
            var leftFrom = false;
#endif
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
#if FRONTROOMS_RELAY_V2
                if (legDroppedAt < 0f && !(hunter.State == leg && world.CellOf(hunter.Position) == from && hunter.DebugSteering.Contains("next " + to))) legDroppedAt = t;
                if (world.CellOf(hunter.Position) != from) leftFrom = true;
#endif
            }
            var kept = latched && blows == 0 && hunter.DoorsBroken == 0 && !door.broken && !door.open
                && !states.Contains(HunterState.Hunt) && !states.Contains(HunterState.BreakDoor);
#if FRONTROOMS_RELAY_V2
            if (leg == HunterState.Wander)
            {
                // A way round: it walks round or takes another leg away from the door's cell; no way round: it never gets past
                // the door. V3b (RELAY 5): that door is a WALL: it walks to the door, listens 2 s, and only then is the leg through
                // it dropped, where it stands (DoorsWalled once).
                var v2Ended = round ? legDroppedAt >= 0f && (walkedRound || leftFrom)
                    : !walkedRound && legDroppedAt >= FrontRoomsMapHunter.WallListenSeconds - Dt && legDroppedAt <= FrontRoomsMapHunter.WallListenSeconds + 1.5f && hunter.DoorsWalled == 1;
                Check(kept && v2Ended,
                    label + "(v2 patrol) seed " + seed + ", the door " + from + "->" + to + " shut mid-leg stays shut and unbroken (" + hunter.DoorsBroken + " breaks, " + blows + " blows); then "
                    + (states.Count > 0 ? string.Join(" -> ", states) : "no state change") + "; the leg through it dropped after " + legDroppedAt.ToString("0.00") + " s (WALL " + hunter.DoorsWalled + "x)"
                    + (walkedRound ? "; it got into " + to : "") + (leftFrom ? "; it left " + from : "; it stayed in " + from));
                continue;
            }
#endif
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

#if FRONTROOMS_RELAY_V2
    /// <summary>
    /// Relay v2 (V3a): a door a patrol leg must take. The patrol picks its leg's end 4-12 cells off in its own zone while one
    /// fits there, so the near cell is in a room whose zone holds no cell 4 or more cells of open walking from it (the leg
    /// has to leave the zone), and the far cell is in another zone.
    /// </summary>
    static bool PatrolDoorV2(FrontRoomsMapWorld world, FrontRoomsMapWorld.Door d, GridCoord near)
    {
        var far = d.a == near ? d.b : d.a;
        var zone = world.ZoneOf(near).id;
        if (!InRoom(world, near) || world.ZoneOf(far).id == zone) return false;
        var depth = new Dictionary<GridCoord, int> { [near] = 0 };
        var queue = new Queue<GridCoord>();
        queue.Enqueue(near);
        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            if (depth[cell] >= 4) return false;
            foreach (var s in Steps)
            {
                var n = cell + s;
                if (depth.ContainsKey(n) || !world.IsBuilt(n) || world.PassageBetween(cell, n) != FrontRoomsMapWorld.Passage.Open || world.ZoneOf(n).id != zone) continue;
                depth[n] = depth[cell] + 1;
                queue.Enqueue(n);
            }
        }
        return true;
    }

#endif
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
        ChaseNowV4b(unseen, Out(-1f), () => Stand(unseen, Out(-1f), 1));
        var seenInA = Stand(unseen, Out(-1f), 6);
        var seenAway = Stand(unseen, world.CellCenter(away.a), 6) + Stand(unseen, world.CellCenter(away.b), 6);
        var at = SearchAfter(unseen);
        Check(seenInA == 6 && seenAway == 0 && at.HasValue && world.CellOf(at.Value) == a && Flat(at.Value - Out(-1f)) < 1f,
            "lose track: the player went through " + away.a + "→" + away.b + " out of its sight (seen " + seenInA + "/6 frames in " + a + ", " + seenAway + "/12 there): it searches from "
            + (at.HasValue ? world.CellOf(at.Value).ToString() : "nowhere") + ", where it last saw them (expected " + a + ")");

        // Seen (the control): in a, through the door into b and on into e, all in sight 0.1 s each, then gone. It follows through the door it saw them take.
        var watched = new FrontRoomsMapHunter(world, tuning, body, null, 3);
        watched.DebugPlace(Out(-4.5f));
        ChaseNowV4b(watched, Out(-1f), () => Stand(watched, Out(-1f), 1));
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
#if FRONTROOMS_RELAY_V2
        // V4b: the chase's speed is the tier table's.
        stale.TierV2 = new FrontRoomsTierV2 { chaseSpeed = 1f };
#endif
        ChaseNowV4b(stale, Out(-1f), () => Stand(stale, Out(-1f), 1));
        var seenStale = Stand(stale, Out(-1f), 6) + Stand(stale, Out(1f), 72) + Stand(stale, Out(4.5f), 6);
        var at3 = SearchAfter(stale);
        Check(seenStale == 84 && at3.HasValue && world.CellOf(at3.Value) == e && Flat(at3.Value - Out(4.5f)) < 1f,
            "lose track (stale): it saw the player go through " + a + "→" + b + ", then 1.2 s in " + b + " and on into " + e + " (" + seenStale + "/84 frames in sight), 1.28 s after the crossing: it searches from "
            + (at3.HasValue ? world.CellOf(at3.Value).ToString() : "nowhere") + ", where it last saw them (expected " + e + ")");

        // A gap in its sight: seen in a, out of sight for a moment, then seen in b and on into e. It saw them on each side of the door
        // but never watched the crossing (no two ticks in a row), so it goes where it last saw them, not through the door.
        var gap = new FrontRoomsMapHunter(world, tuning, body, null, 3);
        gap.DebugPlace(Out(-4.5f));
        ChaseNowV4b(gap, Out(-1f), () => Stand(gap, Out(-1f), 1));
        var seenBefore = Stand(gap, Out(-1f), 6);
        var seenInGap = Stand(gap, world.CellCenter(away.a), 3);
        var seenAfter = Stand(gap, Out(1f), 3) + Stand(gap, Out(4.5f), 6);
        var at4 = SearchAfter(gap);
        Check(seenBefore == 6 && seenInGap == 0 && seenAfter == 9 && at4.HasValue && world.CellOf(at4.Value) == e && Flat(at4.Value - Out(4.5f)) < 1f,
            "lose track (gap): seen in " + a + " (" + seenBefore + "/6), unseen for 3 frames (" + seenInGap + "/3), then seen in " + b + " and " + e + " (" + seenAfter + "/9): it never watched the crossing, so it searches from "
            + (at4.HasValue ? world.CellOf(at4.Value).ToString() : "nowhere") + ", where it last saw them (expected " + e + ")");
    }

    // ---------- Relay v2 V2: the three named sight tests, the Threat reading, ThreatPerceivable (RELAY 8, 2026-10-29) ----------

    /// <summary>
    /// Relay v2 step V2 (RELAY 8 amended 2026-10-14; computer_ui 20_design 6.1 and 6.6). The reference frustum and the
    /// pure parts first (W, RelayLit's thresholds, Threat's v1 stand-ins), then a built map: a player cell and a Relay
    /// cell 6-11 m apart in clear view, the Relay's lamp set Steady. RelayInView needs both points and both rays; the
    /// device camera moves RelayOnScreen and never PlayerSeesRelay; a dark lamp makes a seen Relay unperceived;
    /// ThreatPerceivable holds 3 s after a chase (Dormant drops it) and hides a door break only while its blows are heard.
    /// </summary>
    static void RelaySight(List<GameObject> roots)
    {
        const string label = "relay sight: ";
        var inv = System.Globalization.CultureInfo.InvariantCulture;

        // 1. The reference frustum at the origin: 76 deg vertical at 16:9 (about +-54.2 deg across), to its far plane.
        var o = new Vector3(0f, ModuleUnits.PlayerEye, 0f);
        Vector3 Dir(float yaw, float up) => Quaternion.Euler(-up, yaw, 0f) * Vector3.forward;
        var level = FrontRoomsRelayFrustum.Reference(o, Vector3.forward, 46f);
        var across = Mathf.Atan(Mathf.Tan(FrontRoomsRelayFrustum.ReferenceFov * .5f * Mathf.Deg2Rad) * FrontRoomsRelayFrustum.ReferenceAspect) * Mathf.Rad2Deg;
        Check(Mathf.Abs(across - 54.25f) < .05f && level.Contains(o + Dir(52f, 0f) * 10f) && level.Contains(o + Dir(-52f, 0f) * 10f) && !level.Contains(o + Dir(57f, 0f) * 10f)
              && level.Contains(o + Dir(0f, 36f) * 10f) && !level.Contains(o + Dir(0f, 40f) * 10f) && level.Contains(o + Dir(0f, -36f) * 10f)
              && level.Contains(o + Vector3.forward * 45f) && !level.Contains(o + Vector3.forward * 47f) && !level.Contains(o + Dir(180f, 0f) * 5f),
            label + "the reference frustum: +-" + across.ToString("0.0", inv) + " deg across (52 in, 57 out), +-38 deg up and down (36 in, 40 out), to its far plane (45 m in, 47 out), nothing behind");
        // Its pitch counts: the old test was a flat +-60 deg with no pitch.
        var down = FrontRoomsRelayFrustum.Reference(o, Dir(0f, -45f), 46f);
        Check(!down.Contains(o + Vector3.forward * 10f) && down.Contains(o + Dir(0f, -40f) * 10f) && down.Contains(o + Dir(0f, -80f) * 10f),
            label + "the look's pitch counts: looking 45 deg down, a point level with the eye 10 m ahead is out (the old flat test counted it); 40 and 80 deg down are in");

        // 2. W = min(1, L + 0.02 sum L_n) (NoLamp counts 0) and RelayLit's two thresholds.
        var w1 = FrontRoomsMapHunter.LampW(.1f, .5f, .5f, .5f, 0f);
        var w2 = FrontRoomsMapHunter.LampW(.1f, 1f, 1f, 1f, 1f);
        var w3 = FrontRoomsMapHunter.LampW(FrontRoomsMapWorld.NoLamp, 1f, 1f, 1f, 1f);
        var none = FrontRoomsMapWorld.NoLamp;
        Check(Mathf.Abs(w1 - .13f) < 1e-5f && Mathf.Abs(w2 - .18f) < 1e-5f && Mathf.Abs(w3 - .08f) < 1e-5f && FrontRoomsMapHunter.LampW(1f, 1f, 1f, 1f, 1f) == 1f
              && FrontRoomsMapHunter.LampW(none, none, none, none, none) == 0f,
            label + "W: own 0.1 with neighbours 0.5/0.5/0.5/none " + w1.ToString("0.###", inv) + ", with 4 full " + w2.ToString("0.###", inv) + "; no lamp of its own among 4 full "
            + w3.ToString("0.###", inv) + "; capped at 1; no lamp anywhere 0");
        Check(FrontRoomsMapHunter.RelayLitMetres == 16f && FrontRoomsMapHunter.RelayLitLevel == .15f && FrontRoomsMapHunter.LitBy(16f, .15f) && !FrontRoomsMapHunter.LitBy(16.01f, 1f)
              && !FrontRoomsMapHunter.LitBy(3f, .149f) && !FrontRoomsMapHunter.LitBy(3f, w1) && FrontRoomsMapHunter.LitBy(3f, w2) && !FrontRoomsMapHunter.LitBy(3f, w3),
            label + "RelayLit's thresholds: XZ 16 m with W 0.15 is lit; 16.01 m, or W 0.149, is not; own 0.1 is lit only with its neighbours (0.13 no, 0.18 yes); a cell with no lamp among 4 full ones is dark (0.08)");

        // 3. The Threat reading's v1 stand-ins (V4b swaps the source inside the hunter, not its callers).
        var chaseFar = FrontRoomsRelayThreat.V1(true, false, 20f);
        var spot6 = FrontRoomsRelayThreat.V1(false, true, 6f);
        Check(chaseFar.TargetAcquiredRecently && !chaseFar.Spotting && chaseFar.Any && spot6.Spotting && !spot6.TargetAcquiredRecently && spot6.Any
              && !FrontRoomsRelayThreat.V1(false, true, 6.01f).Any && !FrontRoomsRelayThreat.V1(false, true, -1f).Any && !FrontRoomsRelayThreat.V1(false, false, 1f).Any
              && !default(FrontRoomsRelayThreat).Any && FrontRoomsRelayThreat.SpottingStandInMetres == 6f,
            label + "Threat's v1 stand-ins: a chase at 20 m is TargetAcquiredRecently; sight within 6 m flat is Spotting (6 m counts, 6.01 does not); sight not out (-1), no sight, or nothing is no threat");

        // 4. A built map: a player cell and a lamp-lit Relay cell 6-11 m apart, both Relay points and its eye in clear view.
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), -71f, "MAP INTERACTION TEST / relay sight");
        world.BuildForCapture();
        Physics.SyncTransforms();
        var mid = world.CellOf(world.SpawnWorldPosition);
        var found = false;
        var pc = default(GridCoord);
        var rc = default(GridCoord);
        for (var y = mid.y - 10; y <= mid.y + 10 && !found; y++)
        for (var x = mid.x - 10; x <= mid.x + 10 && !found; x++)
        {
            var p = new GridCoord(x, y);
            if (!world.IsBuilt(p)) continue;
            var pe = world.CellCenter(p) + Vector3.up * ModuleUnits.PlayerEye;
            for (var dy = -4; dy <= 4 && !found; dy++)
            for (var dx = -4; dx <= 4 && !found; dx++)
            {
                var r = new GridCoord(x + dx, y + dy);
                if (!world.IsBuilt(r) || world.LampLevel(r) < 0f) continue;
                var rp = world.CellCenter(r);
                var apart = new Vector2(rp.x - pe.x, rp.z - pe.z).magnitude;
                if (apart < 6f || apart > 11f) continue;
                if (Physics.Linecast(pe, rp + Vector3.up * FrontRoomsMapHunter.RelayLowPoint, ~0, QueryTriggerInteraction.Ignore)
                    || Physics.Linecast(pe, rp + Vector3.up * FrontRoomsMapHunter.RelayHighPoint, ~0, QueryTriggerInteraction.Ignore)
                    || Physics.Linecast(pe, rp + Vector3.up * ModuleUnits.RelayEye, ~0, QueryTriggerInteraction.Ignore)) continue;
                pc = p;
                rc = r;
                found = true;
            }
        }
        if (!found) { Check(false, label + "no player cell and lit Relay cell 6-11 m apart in clear view near the spawn"); return; }
        world.SetLampMode(rc, ModuleLamp.Steady);
        for (var s = 0f; s < 1f; s += Dt) world.TickFixturesForTools(Dt);
        var feet = world.CellCenter(pc);
        var eye = feet + Vector3.up * ModuleUnits.PlayerEye;
        var relayFeet = world.CellCenter(rc);
        var lowPoint = relayFeet + Vector3.up * FrontRoomsMapHunter.RelayLowPoint;
        var highPoint = relayFeet + Vector3.up * FrontRoomsMapHunter.RelayHighPoint;
        var ahead = new Vector3(relayFeet.x - feet.x, 0f, relayFeet.z - feet.z);
        var metres = ahead.magnitude;
        ahead /= metres;
        var where = metres.ToString("0.0", inv) + " m off (" + pc + " to " + rc + ")";

        // Not released: nothing to see, no threat.
        var watcher = new FrontRoomsMapHunter(world, new FrontRoomsHunterTuning { sightRange = 0f }, null, null, 41);
        watcher.DebugRefreshSight(feet, eye, ahead);
        var dormantQuiet = !watcher.PlayerSeesRelay && !watcher.RelayLit && !watcher.Threat.Any && !watcher.ThreatPerceivable(null);
        watcher.DebugPlace(relayFeet);
        watcher.DebugRefreshSight(feet, eye, ahead);
        var w = FrontRoomsMapHunter.LampW(world, rc);
        var reference = FrontRoomsRelayFrustum.Reference(eye, ahead, world.SightDistance);
        var seenAhead = watcher.PlayerSeesRelay && watcher.RelayLit && watcher.PlayerPerceivesRelay && watcher.RelayInView(reference);
        watcher.DebugRefreshSight(feet, eye, -ahead);
        var turnedAway = !watcher.PlayerSeesRelay && watcher.RelayLit && !watcher.PlayerPerceivesRelay;
        Check(dormantQuiet && seenAhead && turnedAway && w >= .9f && world.SightDistance > 40f,
            label + "a Relay " + where + ", its lamp Steady (W " + w.ToString("0.00", inv) + "): Dormant it is neither seen nor a threat; placed, looked at, PlayerSeesRelay, RelayLit and PlayerPerceivesRelay; "
            + "turned away, not seen (still lit)");

        // RelayInView: an occluder on either point's ray hides it, though both points stay inside the frustum.
        var blockGo = new GameObject("MAP INTERACTION TEST / relay sight occluder") { hideFlags = HideFlags.DontSave };
        roots.Add(blockGo);
        var box = blockGo.AddComponent<BoxCollider>();
        box.size = new Vector3(.4f, .12f, .2f);
        bool SeenPast(Vector3 at)
        {
            blockGo.transform.SetPositionAndRotation(at, Quaternion.LookRotation(ahead));
            Physics.SyncTransforms();
            watcher.DebugRefreshSight(feet, eye, ahead);
            return watcher.PlayerSeesRelay;
        }
        var lowBlocked = SeenPast((eye + lowPoint) * .5f);
        var lowBlockedAsked = watcher.RelayInView(reference);
        var highBlocked = SeenPast((eye + highPoint) * .5f);
        blockGo.SetActive(false);
        Physics.SyncTransforms();
        watcher.DebugRefreshSight(feet, eye, ahead);
        var clearAgain = watcher.PlayerSeesRelay;
        Check(reference.Contains(lowPoint) && reference.Contains(highPoint) && !lowBlocked && !lowBlockedAsked && !highBlocked && clearAgain,
            label + "RelayInView needs both points: a 12 cm block on the 1.6 m point's ray hides it (" + !lowBlocked + "), on the 1.95 m point's ray too (" + !highBlocked
            + "), both points inside the frustum; the block gone, seen again (" + clearAgain + ")");

        // The device camera: RelayOnScreen follows its FOV and aspect, PlayerSeesRelay never does. The Relay 50 deg off the look.
        var camGo = new GameObject("MAP INTERACTION TEST / relay sight camera") { hideFlags = HideFlags.DontSave };
        roots.Add(camGo);
        var camera = camGo.AddComponent<Camera>();
        camera.enabled = false;
        var side = Quaternion.Euler(0f, 50f, 0f) * ahead;
        camGo.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(side));
        camera.fieldOfView = 76f;
        camera.aspect = 16f / 9f;
        watcher.DebugRefreshSight(feet, eye, side);
        var seesWide = watcher.PlayerSeesRelay;
        var onWide = watcher.RelayOnScreen(camera);
        camera.aspect = 4f / 3f;
        var onIpad = watcher.RelayOnScreen(camera);
        watcher.DebugRefreshSight(feet, eye, side);
        var seesIpad = watcher.PlayerSeesRelay;
        camera.aspect = 16f / 9f;
        camera.fieldOfView = 60f;
        var onNarrow = watcher.RelayOnScreen(camera);
        watcher.DebugRefreshSight(feet, eye, side);
        var seesNarrow = watcher.PlayerSeesRelay;
        camera.fieldOfView = 76f;
        var onBack = watcher.RelayOnScreen(camera);
        Check(seesWide && onWide && !onIpad && seesIpad && !onNarrow && seesNarrow && onBack,
            label + "the Relay 50 deg off the look: PlayerSeesRelay " + seesWide + " / " + seesIpad + " / " + seesNarrow + " whatever the camera; RelayOnScreen on a 76 deg 16:9 camera "
            + onWide + ", at 4:3 " + onIpad + ", at a 60 deg FOV " + onNarrow + ", back at 76 deg 16:9 " + onBack + " (it follows the camera at once, no hold)");

        // RelayLit: the Relay's own lamp out (its neighbours give at most 0.08) makes a seen Relay unperceived and off every screen.
        camGo.transform.rotation = Quaternion.LookRotation(ahead);
        var dip = world.SetLampOverride(rc, FrontRoomsMapWorld.LampFx.Dip, 0f, float.PositiveInfinity);
        for (var s = 0f; s < 1.5f; s += Dt) world.TickFixturesForTools(Dt);
        watcher.DebugRefreshSight(feet, eye, ahead);
        var darkW = FrontRoomsMapHunter.LampW(world, rc);
        var darkSeen = watcher.PlayerSeesRelay && !watcher.RelayLit && !watcher.PlayerPerceivesRelay && watcher.RelayInView(FrontRoomsRelayFrustum.Of(camera)) && !watcher.RelayOnScreen(camera);
        world.RemoveLampOverride(dip);
        for (var s = 0f; s < 2f; s += Dt) world.TickFixturesForTools(Dt);
        watcher.DebugRefreshSight(feet, eye, ahead);
        var relit = watcher.RelayLit && watcher.PlayerPerceivesRelay && watcher.RelayOnScreen(camera);
        Check(darkW < FrontRoomsMapHunter.RelayLitLevel && darkSeen && relit,
            label + "its lamp dipped to 0 (W " + darkW.ToString("0.00", inv) + "): still PlayerSeesRelay (a dark Relay counts there), not RelayLit, not perceived, in the camera's view but not RelayOnScreen; the dip gone, lit and on screen again");
        // RelayLit's 16 m on the flat (it needs no ray): the player 15.5 m off is in range, 16.5 m off is not.
        watcher.DebugRefreshSight(relayFeet + Vector3.right * 15.5f, eye, ahead);
        var litNear = watcher.RelayLit;
        watcher.DebugRefreshSight(relayFeet + Vector3.right * 16.5f, eye, ahead);
        var litFar = watcher.RelayLit;
        Check(litNear && !litFar, label + "RelayLit on the map: the player's feet 15.5 m off " + litNear + ", 16.5 m off " + litFar);

        // 5. ThreatPerceivable after a chase: hidden through it and for 3 s after it ends (the chase bed's fade), then shown.
        //    It sees the player at once (v1, 12 m in clear view) and chases; made blind it loses them after lostSightSeconds.
        var chaseTuning = new FrontRoomsHunterTuning();
        var chaser = new FrontRoomsMapHunter(world, chaseTuning, null, null, 43);
        chaser.DebugPlace(relayFeet);
        chaser.Tick(Dt, feet, eye, ahead);
        ChaseNowV4b(chaser, feet, () => chaser.Tick(Dt, feet, eye, ahead));
        var chased = chaser.State == HunterState.Chase && chaser.Threat.TargetAcquiredRecently && chaser.ThreatPerceivable(null);
        chaseTuning.sightRange = 0f;
        var ended = -1;
        var shownAt = -1;
        var hiddenInChase = true;
        var hiddenAtEnd = false;
        for (var i = 1; i <= 900 && shownAt < 0; i++)
        {
            chaser.Tick(Dt, feet, eye, ahead);
            var hides = chaser.ThreatPerceivable(null);
            if (ended < 0)
            {
                if (chaser.State == HunterState.Chase) { hiddenInChase &= hides; continue; }
                ended = i;
                hiddenAtEnd = hides && chaser.ChaseFading && !chaser.Threat.Any;
                continue;
            }
            if (!hides) shownAt = i;
        }
        var heldFor = ended > 0 && shownAt > 0 ? (shownAt - ended) * Dt : -1f;
        Check(chased && hiddenInChase && hiddenAtEnd && Mathf.Abs(heldFor - FrontRoomsMapHunter.ChaseFadeSeconds) <= 1.5f * Dt,
            label + "a chase hides the prompt (TargetAcquiredRecently " + chased + "); after it ends (" + chaser.State + ") the prompt stays hidden " + heldFor.ToString("0.000", inv)
            + " s (expected 3 s, the chase bed's fade), then shows");
        // Dormant drops the hold: parked half a second into the fade, the prompt shows at once.
        var parkTuning = new FrontRoomsHunterTuning();
        var parked = new FrontRoomsMapHunter(world, parkTuning, null, null, 44);
        parked.DebugPlace(relayFeet);
        parked.Tick(Dt, feet, eye, ahead);
        ChaseNowV4b(parked, feet, () => parked.Tick(Dt, feet, eye, ahead));
        parkTuning.sightRange = 0f;
        for (var i = 0; i < 900 && parked.State == HunterState.Chase; i++) parked.Tick(Dt, feet, eye, ahead);
        for (var i = 0; i < 30; i++) parked.Tick(Dt, feet, eye, ahead);
        var fading = parked.ThreatPerceivable(null) && parked.ChaseFading;
        parked.Park();
        Check(fading && !parked.ThreatPerceivable(null) && !parked.ChaseFading,
            label + "parked (Dormant) half a second into the fade, the hold lets go at once (fading before " + fading + ")");

        // 6. A door break: hidden only from the first blow heard (within 36 m of the player's eye) until the door gives (the break
        //    heard); out of earshot never. v1's old gate hid the prompt for the whole BreakDoor at any range.
        Check(Mathf.Abs(new FrontRoomsMapHunter(world, new FrontRoomsHunterTuning(), null, null, 45).BlowGapSeconds - 1.5f) < 1e-4f
              && FrontRoomsMapHunter.BlowAudibleMetres == 36f && FrontRoomsMapHunter.BreakAudibleMetres == 40f,
            label + "BlowGap at the 2.5 s break is 1.5 s (the longest gap between its blows 0.5 s, + 1 s); blows heard within 36 m, the break within 40 m");
        bool Break(float eyeOffset, int seed, out int earlyHidden, out int blowTicks, out int blowShown, out int afterHidden, out float heardFrom)
        {
            earlyHidden = blowTicks = blowShown = afterHidden = 0;
            heardFrom = -1f;
            if (!FindDoor(world, out var a, out var b, out var c)) return false;
            var door = world.DoorBetween(a, b);
            var target = world.CellCenter(b);
            var playerEye = target + Vector3.up * ModuleUnits.PlayerEye + Vector3.right * eyeOffset;
            heardFrom = Vector3.Distance(door.position, playerEye);
            var breaker = new FrontRoomsMapHunter(world, new FrontRoomsHunterTuning { sightRange = 0f }, null, null, seed);
            breaker.DebugPlace(world.CellCenter(c));
            breaker.Noise(target, 1000f);
            var blows = 0;
            breaker.DoorBlow += _ => blows++;
            var after = 0;
            for (var t = 0f; t < 40f && after < 60; t += Dt)
            {
                breaker.Tick(Dt, target, playerEye, Vector3.forward);
                world.TickDoorsForTools(Dt);
                Physics.SyncTransforms();
                var hides = breaker.ThreatPerceivable(null);
                if (door.broken)
                {
                    after++;
                    if (hides) afterHidden++;
                }
                else if (breaker.State == HunterState.BreakDoor)
                {
                    if (blows == 0) { if (hides) earlyHidden++; }
                    else
                    {
                        blowTicks++;
                        if (!hides) blowShown++;
                    }
                }
            }
            return door.broken;
        }
        var nearBroke = Break(0f, 46, out var nearEarly, out var nearTicks, out var nearShown, out var nearAfter, out var nearFrom);
        Check(nearBroke && nearEarly == 0 && nearTicks > 60 && nearShown == 0 && nearAfter == 0,
            label + "a break heard (the door " + nearFrom.ToString("0.0", inv) + " m from the eye): shown before the first blow (" + nearEarly + " ticks hidden), hidden from the first blow to the break ("
            + nearShown + "/" + nearTicks + " ticks shown), shown once the door gives (" + nearAfter + " of 60 ticks hidden)");
        var farBroke = Break(40f, 47, out var farEarly, out var farTicks, out var farShown, out var farAfter, out var farFrom);
        Check(farBroke && farFrom > FrontRoomsMapHunter.BlowAudibleMetres && farEarly == 0 && farTicks > 60 && farShown == farTicks && farAfter == 0,
            label + "a break out of earshot (the eye " + farFrom.ToString("0.0", inv) + " m from the door): the prompt never hides (" + farShown + "/" + farTicks + " blow ticks shown)");
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
        // C04 a (V1, 2026-10-29): 150 unique cells per tier, capped at 5.
        Check(rules.cellsPerTier == 150 && rules.TierForCells(0) == 1 && rules.TierForCells(149) == 1 && rules.TierForCells(150) == 2 && rules.TierForCells(599) == 4
              && rules.TierForCells(600) == 5 && rules.TierForCells(10000) == 5 && rules.TierForCells(-3) == 1,
            "tiers: cells 0-149 tier 1, 150 tier 2, 599 tier 4, 600 tier 5, 10000 capped at 5");
        var unset = new FrontRoomsTierRules { cellsPerTier = 0 };
        Check(unset.TierForCells(149) == 1 && unset.TierForCells(150) == 2, "tiers: a Cells Per Tier of 0 (a profile saved before it) reads 150");
        unset.Normalize();
        Check(unset.cellsPerTier == 150, "tiers: Normalize gives a Cells Per Tier of 0 the default 150 (" + unset.cellsPerTier + ")");
        var t1 = rules.At(1);
        Check(t1.LampMode(.61f) == 0 && t1.LampMode(.63f) == 1 && t1.LampMode(.83f) == 2 && t1.LampMode(.93f) == 3 && t1.LampMode(.98f) == 4,
            "tiers: tier 1 lamp odds are the old 62 / 20 / 10 / 5 / 3");
        // P6 (V1): stutter <= 0.20 and failing <= 0.12 at every tier, the excess on steady; T5 .51/.20/.12/.12/.05.
        // The code rows and the resolved profile's own rows (an asset saved with the old odds is capped where read).
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        string Row(FrontRoomsTier r) => r.Steady.ToString("0.00", inv) + "/" + r.Stutter.ToString("0.00", inv) + "/" + r.Failing.ToString("0.00", inv) + "/" + r.Dead.ToString("0.00", inv) + "/" + r.Dim.ToString("0.00", inv);
        bool Capped(FrontRoomsTier r) => r.Stutter <= .2f + 1e-5f && r.Failing <= .12f + 1e-5f && Mathf.Abs(r.Steady + r.Stutter + r.Failing + r.Dead + r.Dim - 1f) < 1e-4f;
        var assetRules = FrontRoomsLevelProfiles.Resolve().tiers ?? new FrontRoomsTierRules();
        var lampRows = new List<string>();
        var allCapped = true;
        for (var t = 1; t <= 5; t++)
        {
            allCapped &= Capped(rules.At(t)) && Capped(assetRules.At(t)) && Row(rules.At(t)) == Row(assetRules.At(t));
            lampRows.Add("T" + t + " " + Row(rules.At(t)));
        }
        Check(allCapped && Row(rules.At(5)) == "0.51/0.20/0.12/0.12/0.05" && Row(rules.At(4)) == "0.54/0.20/0.12/0.10/0.04",
            "tiers: lamp odds capped at every tier, code and profile alike (steady/stutter/failing/dead/dim): " + string.Join(", ", lampRows));
        var old5 = new FrontRoomsTier(1.29f, 1.6f, .52f, 1.6f, .40f, .25f, .18f, .12f);
        Check(Row(old5) == "0.51/0.20/0.12/0.12/0.05" && old5.lampStutter == .25f && old5.LampMode(.505f) == 0 && old5.LampMode(.515f) == 1 && old5.LampMode(.705f) == 1
              && old5.LampMode(.715f) == 2 && old5.LampMode(.825f) == 2 && old5.LampMode(.835f) == 3 && old5.LampMode(.945f) == 3 && old5.LampMode(.955f) == 4,
            "tiers: the old T5 row (.40/.25/.18/.12) rolls at " + Row(old5) + ", its numbers left as they are");

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

        // Generation (D6, V1 2026-10-29): a chunk keeps the tier of its first generation, through a revisit shift too.
        var cache = new FrontRoomsMapCache(FrontRoomsLevelProfiles.Resolve().Generation(4242));
        var first = cache.Get(new GridCoord(0, 0));
        cache.Tier = 3;
        var again = cache.Get(new GridCoord(0, 0));
        var fresh = cache.Get(new GridCoord(5, 0));
        cache.Shift(new GridCoord(0, 0));
        var shifted = cache.Get(new GridCoord(0, 0));
        Check(first.tier == 1 && ReferenceEquals(first, again) && fresh.tier == 3 && shifted.tier == 1 && shifted.revision == 1 && !ReferenceEquals(first, shifted)
              && cache.TierOf(new GridCoord(0, 0)) == 1 && cache.TierOf(new GridCoord(5, 0)) == 3 && cache.TierOf(new GridCoord(-7, 9)) == 3,
            "tiers: a generated chunk keeps tier 1 after the run reaches tier 3, and so does its revisit shift (revision " + shifted.revision + ", tier " + shifted.tier + "); a new chunk takes 3");
        // Zones: each keeps the tier the cache first met it at, by a chunk's generation or a ZoneOf.
        var zoneFirst = first.ownZone.id;
        var zoneCell = first.zone[0];
        var zoneFresh = fresh.ownZone.id;
        var zoneFar = cache.ZoneOf(new GridCoord(400, 400)).id;
        cache.Tier = 5;
        cache.Shift(new GridCoord(5, 0));
        var fresh2 = cache.Get(new GridCoord(5, 0));
        var zoneNew = cache.ZoneOf(new GridCoord(-400, 400)).id;
        Check(cache.ZoneTier(zoneFirst) == 1 && cache.ZoneTier(zoneCell) == 1 && cache.ZoneTier(zoneFresh) == 3 && cache.ZoneTier(zoneFar) == 3 && fresh2.tier == 3
              && cache.ZoneTier(zoneNew) == 5 && cache.ZoneTier(zoneFirst) == 1,
            "tiers: ZoneTier keeps the first record (" + cache.ZoneTier(zoneFirst) + "/" + cache.ZoneTier(zoneFresh) + "/" + cache.ZoneTier(zoneFar) + "/" + cache.ZoneTier(zoneNew)
            + ", expected 1/3/3/5) after the run reaches tier 5 and a shift");
        var newCache = new FrontRoomsMapCache(FrontRoomsLevelProfiles.Resolve().Generation(4242)) { Tier = 2 };
        Check(newCache.ZoneTier(zoneFirst) == 2 && newCache.TierOf(new GridCoord(0, 0)) == 2, "tiers: a new cache starts empty (zone and chunk take its tier, 2)");
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

    // ---------- Relay v2 V0: inert contracts (states, tuning, tier rows, Ajar, the lamp hook) ----------

    static void RelayV2Contracts(List<GameObject> roots)
    {
        const string label = "v2 contracts: ";
        var inv = System.Globalization.CultureInfo.InvariantCulture;

        // States: appended, so every v1 value is unchanged.
        Check((int)HunterState.Dormant == 0 && (int)HunterState.Chase == 4 && (int)HunterState.BreakDoor == 5 && (int)HunterState.Wander == 6
              && (int)HunterState.Away == 7 && (int)HunterState.Arrive == 8 && (int)HunterState.Withdraw == 9 && Enum.GetValues(typeof(HunterState)).Length == 10,
            label + "HunterState appends Away 7, Arrive 8, Withdraw 9; Dormant..Wander keep 0..6");

        // Tuning: the v2 loudness table beside v1's numbers, which stay as they were.
        var t = new FrontRoomsHunterTuning();
        Check(t.glassNoiseRadius == 40f && t.sprintNoiseRadius == 26f && t.doorNoiseRadius == 14f && t.releaseDelaySeconds == 3f && t.hearing == 1.4f
              && t.sprintLoudness == 27f && t.easeLoudness == 6f && t.unlockLoudness == 6f && t.rattleLoudness == 6f && t.bargeLoudness == 18f
              && t.slamLoudness == 27f && t.pryLoudness == 8f && t.glassLoudness == 60f && t.deviceLoudness == 45f && t.walkLoudness == 9f,
            label + "tuning: glass 40 m (the game's old const), legacy sprint 26 / door 14 / release 3 s / hearing 1.4 kept; v2 loudness sprint 27, ease, unlock and rattle 6, barge 18, slam 27, pry 8, glass 60, device 45, walk 9");
        // A36 (Red 2026-10-30, one gait rule): the walk is 9 at every tier on every platform.
        var loud = new FrontRoomsHunterTuning { walkLoudness = 12f };
        var walk = string.Join("/", Enumerable.Range(1, 5).Select(n => t.WalkLoudness(n).ToString("0.#", inv)));
        var handheld = string.Join("/", Enumerable.Range(1, 5).Select(n => loud.HandheldWalkLoudness(n).ToString("0.#", inv)));
        Check(walk == "9/9/9/9/9" && handheld == "12/12/12/12/12" && t.WalkLoudness(9) == 9f,
            label + "tuning: walk by tier " + walk + " m (A36: every tier, every platform); the handheld walk is the same walk (" + handheld + " with walk 12)");
        var fields = typeof(FrontRoomsHunterTuning).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var src = new FrontRoomsHunterTuning();
        var k = 0;
        foreach (var f in fields)
        {
            k++;
            if (f.FieldType == typeof(float)) f.SetValue(src, 100f + k);
            else if (f.FieldType == typeof(int)) f.SetValue(src, 100 + k);
            else if (f.FieldType == typeof(bool)) f.SetValue(src, !(bool)f.GetValue(src));
        }
        var dst = new FrontRoomsHunterTuning();
        dst.CopyFrom(src);
        var missed = fields.Where(f => !Equals(f.GetValue(src), f.GetValue(dst))).Select(f => f.Name).ToList();
        var odd = fields.Where(f => f.FieldType != typeof(float) && f.FieldType != typeof(int) && f.FieldType != typeof(bool)).Select(f => f.Name).ToList();
        // 26 since A36 (one walkLoudness replaced walkLoudnessT3 / T4 / T5 and handheldWalkLoudness).
        Check(missed.Count == 0 && odd.Count == 0 && fields.Length >= 26,
            label + "tuning: CopyFrom copies every one of its " + fields.Length + " fields (missed: " + (missed.Count == 0 ? "none" : string.Join(", ", missed))
            + (odd.Count > 0 ? "; not a number: " + string.Join(", ", odd) : "") + ")");

        // The tier table's v2 rows: the spec's numbers, and v1's ApplyTo reads none of them.
        var rules = new FrontRoomsTierRules();
        string Col(Func<FrontRoomsTierV2, float> get) => string.Join("/", Enumerable.Range(1, 5).Select(n => get(rules.V2At(n)).ToString("0.###", inv)));
        Check(rules.v2.Length == 5 && Col(r => r.chaseSpeed) == "4.2/4.3/4.4/4.4/4.4" && Col(r => r.breakDoorSeconds) == "2.5/2.3/2.1/1.9/1.9"
              && Col(r => r.hearing) == "1/1.07/1.14/1.21/1.3" && Col(r => r.spotMul) == "1/0.95/0.9/0.85/0.8" && Col(r => r.arriveDelay) == "3/2.75/2.5/2.25/2"
              && Col(r => r.searchSeconds) == "15/15/16/17/18" && Col(r => r.searchSpots) == "3/3/3/4/4" && Col(r => r.patrolSeconds) == "0/0/15/25/35"
              && Col(r => r.zonesPerDevice) == "3/2.5/2/1.75/1.5" && Col(r => r.sweepSeconds) == "40/45/50/55/60" && Col(r => r.callThreshold) == "60/56/52/48/45"
              && Col(r => r.relaxSeconds) == "100/90/80/70/60" && rules.V2At(9) == rules.V2At(5) && rules.V2At(0) == rules.V2At(1),
            label + "tier table: v2 rows T1-T5 chase " + Col(r => r.chaseSpeed) + " m/s, break " + Col(r => r.breakDoorSeconds) + " s, hearing " + Col(r => r.hearing)
            + ", spotMul " + Col(r => r.spotMul) + ", arrive " + Col(r => r.arriveDelay) + " s, search " + Col(r => r.searchSeconds) + " s / " + Col(r => r.searchSpots)
            + " spots, patrol " + Col(r => r.patrolSeconds) + " s, one device per " + Col(r => r.zonesPerDevice) + " zones");
        var mutated = new FrontRoomsTierRules();
        foreach (var row in mutated.v2) { row.chaseSpeed = 99f; row.hearing = 9f; row.breakDoorSeconds = 9f; row.searchSeconds = 99f; row.searchSpots = 9; }
        var same = true;
        for (var n = 1; n <= 5; n++)
        {
            var x = new FrontRoomsHunterTuning();
            rules.At(n).ApplyTo(x);
            var y = new FrontRoomsHunterTuning();
            mutated.At(n).ApplyTo(y);
            same &= x.chaseSpeed == y.chaseSpeed && x.hearing == y.hearing && x.breakDoorSeconds == y.breakDoorSeconds
                && x.searchLookSeconds == y.searchLookSeconds && x.searchMaxSeconds == y.searchMaxSeconds;
        }
        var old = new FrontRoomsTierRules { v2 = null };
        old.Normalize();
        Check(same && old.v2 != null && old.v2.Length == 5 && old.V2At(3).patrolSeconds == 15f,
            label + "tier table: v1's ApplyTo reads none of the v2 rows; a profile saved before them gets the spec's rows on Normalize");

        // OpenAjar on a door on a chunk line (kept through a rebuild), the Relay in b, the player in a.
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), -69f, "MAP INTERACTION TEST / v2 ajar");
        world.BuildForCapture();
        if (!FindDoor(world, out var a, out var b, out _, (d, cell) => MapGrid.ChunkOf(d.a) != MapGrid.ChunkOf(d.b))) { Check(false, label + "no closed door on a chunk line near the spawn"); return; }
        var door = world.DoorBetween(a, b);
        var player = world.Player;
        player.position = world.CellCenter(a) + Vector3.up * .05f;
        Physics.SyncTransforms();
        void Tick(float seconds) { for (var s2 = 0f; s2 < seconds - 1e-4f; s2 += Dt) world.TickDoorsForTools(Dt); }
        int moved = 0, handles = 0, latched = 0, pulled = 0;
        var began = new List<FrontRoomsMapWorld.DoorOpenSource>();
        world.DoorMoved += _ => moved++;
        world.DoorHandleTurned += (d, p) => handles++;
        world.DoorLatched += (d, p) => latched++;
        world.DoorPulled += (d, p) => pulled++;
        world.DoorOpenBegan += (d, src2, latch) => { if (d.edge == door.edge) began.Add(src2); };
        var relay = world.CellCenter(b);
        var revision = world.PassageRevision;
        var opened = world.OpenAjar(door, relay);
        var passageNow = world.PassageBetween(a, b);
        var costNow = world.OpeningCostBetween(a, b);
        var atOnce = passageNow == FrontRoomsMapWorld.Passage.Ajar && Mathf.Abs(costNow - FrontRoomsMapPathField.AjarOpening) < 1e-4f
            && world.PassageRevision != revision && world.IsAjar(door) && !door.open;
        Tick(.5f);
        var half = door.angle;
        Tick(.6f);
        var farSide = FrontRoomsMapWorld.OnSwingSide(door, world.CellCenter(a)) && !FrontRoomsMapWorld.OnSwingSide(door, relay);
        Check(opened && atOnce && half > 5f && half < FrontRoomsMapWorld.AjarDegrees - .5f && Mathf.Abs(door.angle - FrontRoomsMapWorld.AjarDegrees) < .01f && farSide && !door.open
              && world.PassageBetween(a, b) == FrontRoomsMapWorld.Passage.Ajar,
            label + "OpenAjar: Ajar at once (" + passageNow + ", " + costNow.ToString("0.#", inv) + " m), the leaf pushed to " + door.angle.ToString("0.0", inv) + " deg in 1.0 s ("
            + half.ToString("0.0", inv) + " at 0.5 s) on the far side from the Relay (" + farSide + "), not open");
        Check(began.Count == 1 && began[0] == FrontRoomsMapWorld.DoorOpenSource.Relay && moved == 0 && handles == 0 && latched == 0 && pulled == 0,
            label + "OpenAjar raises DoorOpenBegan(Relay) only (" + string.Join(" ", began) + "): DoorMoved " + moved + " (no noise), DoorHandleTurned " + handles
            + " (no camera jolt), DoorLatched " + latched + ", DoorPulled " + pulled);
        var aimKind = world.Kind(door.leaf, player.position, out _, out _);
        var aimText = world.Describe(door.leaf, out var aimHold);
        Check(aimKind == FrontRoomsMapWorld.AimKind.Open && aimText != null && aimText.Contains("OPEN DOOR") && !aimHold,
            label + "an Ajar door aims as " + aimKind + " (" + aimText + "): the player's E opens it");

        var chunks = new HashSet<GridCoord> { MapGrid.ChunkOf(a), MapGrid.ChunkOf(b) };
        foreach (var c in chunks) world.RebuildChunk(c);
        var rebuilt = world.DoorBetween(a, b);
        var state = new Dictionary<string, int>();
        world.FloorStateForTools(state);
        var ajarCount = state.TryGetValue("ajarDoors", out var n1) ? n1 : -1;
        Check(rebuilt != null && rebuilt != door && !rebuilt.open && Mathf.Abs(rebuilt.angle - FrontRoomsMapWorld.AjarDegrees) < .01f
              && FrontRoomsMapWorld.OnSwingSide(rebuilt, world.CellCenter(a)) && world.PassageBetween(a, b) == FrontRoomsMapWorld.Passage.Ajar && ajarCount == 1,
            label + "kept by edge: rebuilt Ajar at " + (rebuilt != null ? rebuilt.angle.ToString("0.0", inv) : "-") + " deg on the same side, ajarDoors " + ajarCount);
        if (rebuilt == null) return;

        began.Clear();
        moved = 0;
        var e = world.Use(rebuilt.leaf);
        Tick(3f);
        world.FloorStateForTools(state);
        Check(e == FrontRoomsMapWorld.UseResult.Opened && rebuilt.open && rebuilt.angle >= FrontRoomsMapWorld.DoorPassableDegrees
              && world.PassageBetween(a, b) == FrontRoomsMapWorld.Passage.Open && !world.IsAjar(rebuilt) && state["ajarDoors"] == 0
              && began.Count == 1 && began[0] == FrontRoomsMapWorld.DoorOpenSource.Use && moved > 0,
            label + "E opens an Ajar door fully (" + e + ", " + rebuilt.angle.ToString("0", inv) + " deg, " + world.PassageBetween(a, b) + ") as the player's own open: DoorOpenBegan("
            + string.Join(" ", began) + "), DoorMoved " + moved + "; the Ajar record goes");

        var refusedOpen = !world.OpenAjar(rebuilt, relay);
        world.ResetDoorForTools(rebuilt);
        var reopened = world.OpenAjar(rebuilt, relay);
        var refusedAjar = !world.OpenAjar(rebuilt, relay);
        world.ResetDoorForTools(rebuilt);
        world.FloorStateForTools(state);
        var resetOk = world.PassageBetween(a, b) == FrontRoomsMapWorld.Passage.ClosedDoor && !world.IsAjar(rebuilt) && state["ajarDoors"] == 0 && rebuilt.angle == 0f;
        world.OpenAjar(rebuilt, relay);
        Tick(1.2f);
        world.BreakDoor(rebuilt, relay);
        Tick(4f);
        world.FloorStateForTools(state);
        var brokeOk = rebuilt.broken && world.PassageBetween(a, b) == FrontRoomsMapWorld.Passage.Open && !world.IsAjar(rebuilt) && state["ajarDoors"] == 0;
        var refusedBroken = !world.OpenAjar(rebuilt, relay);
        Check(refusedOpen && reopened && refusedAjar && resetOk && brokeOk && refusedBroken,
            label + "OpenAjar refuses an open (" + refusedOpen + "), Ajar (" + refusedAjar + ") or broken (" + refusedBroken + ") door; ResetDoorForTools ("
            + resetOk + ") and a break (" + brokeOk + ") end Ajar");

        // LampOverrideStarted: with LampDipped, for a built lamp only, carrying the kind.
        var spawnCell = world.CellOf(world.SpawnWorldPosition);
        var lamp = default(GridCoord);
        var haveLamp = false;
        for (var ly = spawnCell.y - 8; ly <= spawnCell.y + 8 && !haveLamp; ly++)
        for (var lx = spawnCell.x - 8; lx <= spawnCell.x + 8 && !haveLamp; lx++)
        {
            var c = new GridCoord(lx, ly);
            if (world.LampLevel(c) != FrontRoomsMapWorld.NoLamp) { lamp = c; haveLamp = true; }
        }
        var startedCells = new List<GridCoord>();
        var startedAt = new List<Vector3>();
        var startedKinds = new List<FrontRoomsMapWorld.LampFx>();
        var dippedAt = new List<Vector3>();
        world.LampDipped += (c, p) => dippedAt.Add(p);
        world.LampOverrideStarted += (c, p, kind) => { startedCells.Add(c); startedAt.Add(p); startedKinds.Add(kind); };
        var h1 = world.SetLampOverride(lamp, FrontRoomsMapWorld.LampFx.Warn, .3f, .2f);
        var h2 = world.SetLampOverride(lamp, FrontRoomsMapWorld.LampFx.Sag, .6f, .2f);
        world.SetLampOverride(new GridCoord(spawnCell.x + 400, spawnCell.y + 400), FrontRoomsMapWorld.LampFx.Dip, .5f, .2f);
        world.RemoveLampOverride(h1);
        world.RemoveLampOverride(h2);
        Check(haveLamp && startedCells.Count == 2 && dippedAt.Count == 2 && startedCells.All(c => c == lamp)
              && startedKinds[0] == FrontRoomsMapWorld.LampFx.Warn && startedKinds[1] == FrontRoomsMapWorld.LampFx.Sag && startedAt[0] == dippedAt[0] && startedAt[1] == dippedAt[1],
            label + "LampOverrideStarted once per override on a built lamp (" + startedCells.Count + ": " + string.Join(" ", startedKinds) + ") at LampDipped's point; none for an unbuilt cell");

        // NextFloor forgets an Ajar door with the rest of the floor.
        if (FindDoor(world, out var a2, out var b2, out _))
        {
            var other = world.DoorBetween(a2, b2);
            var ajarBefore = world.OpenAjar(other, world.CellCenter(b2));
            world.FloorStateForTools(state);
            var countBefore = state["ajarDoors"];
            world.NextFloor(world.Seed, 1, new RectInt(0, 0, 0, 0), default(GridCoord));
            world.FloorStateForTools(state);
            Check(ajarBefore && countBefore == 1 && state["ajarDoors"] == 0 && world.PassageBetween(a2, b2) != FrontRoomsMapWorld.Passage.Ajar,
                label + "NextFloor forgets the floor's Ajar doors (" + countBefore + " to " + state["ajarDoors"] + ")");
        }
        else Check(false, label + "no second closed door for the NextFloor case");
    }

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
        // D1.6: a dressed key's root is the surface point itself; the cube lies 6 cm up.
        var dressedKey = key != null && key.GetComponentInChildren<IFrontRoomsKeyRig>(true) != null;
        var expected = new Vector3(cs + mx, dressedKey ? 0f : .06f, cs + mz);
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
#if FRONTROOMS_RELAY_V2
        // Relay v2 (V3a): no timed release; Arrive v2 places it backstage and Arrived fires at the step-in.
        RelayEntryV2(world, entry, entryCell);
#else
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
#endif
    }

#if FRONTROOMS_RELAY_V2
    /// <summary>
    /// Relay v2 (V3a) of RelayEntry's two rows: Arrive v2 places the Relay backstage (Arrive: not released, no Arrived) and
    /// Arrived fires at the step-in, the tier's arrive delay later, with the entry's tag; an arrival in an unmarked cell has
    /// none. The player stands 16 cells of walking or more from the entry and 14.5 m straight (W at least 42 m), cannot see
    /// it and faces away from it; the call's estimate lies 9-12 cells from the entry, the nearest the player first (soft
    /// rules a, d, e and f keep the entry). For the unmarked row the estimate is 22 cells or more from the entry, so the
    /// entry is no candidate (6-20 cells).
    /// </summary>
    static void RelayEntryV2(FrontRoomsMapWorld world, Vector3 entry, GridCoord entryCell)
    {
        var depth = Walk(world, entryCell, 40);
        var eye = Vector3.up * ModuleUnits.PlayerEye;
        var player = new GameObject("test player").transform;
        player.SetParent(world.transform, false);
        var body = player.gameObject.AddComponent<CapsuleCollider>();
        body.height = ModuleUnits.PlayerHeight;
        body.radius = ModuleUnits.PlayerRadius;
        body.center = Vector3.up * (ModuleUnits.PlayerHeight * .5f);
        var tuning = new FrontRoomsHunterTuning { sightRange = 0f };
        var seed = 9;
        var players = new List<GridCoord>();
        foreach (var pair in depth.OrderBy(x => x.Value).ThenBy(x => x.Key.x).ThenBy(x => x.Key.y))
        {
            if (pair.Value < 16 || players.Count >= 12) continue;
            var feet = world.CellCenter(pair.Key);
            if (Flat(feet - entry) < 14.5f) continue;
            if (!Physics.Linecast(feet + eye, entry + Vector3.up * 1.6f) || !Physics.Linecast(feet + eye, entry + Vector3.up * 1.95f)) continue;
            players.Add(pair.Key);
        }
        string entryRow = null, plainRow = null;
        var tries = 0;
        for (var k = 0; k < players.Count && (entryRow == null || plainRow == null); k++)
        {
            var p = world.CellCenter(players[k]);
            player.position = p;
            Physics.SyncTransforms();
            var forward = p - entry;
            forward.y = 0f;
            forward.Normalize();
            for (var pass = 0; pass < 2; pass++)
            {
                if (pass == 0 ? entryRow != null : plainRow != null) continue;
                var estimates = depth.Where(x => pass == 0 ? x.Value >= 9 && x.Value <= 12 : x.Value >= 22)
                    .OrderBy(x => Flat(world.CellCenter(x.Key) - p)).ThenBy(x => x.Key.x).ThenBy(x => x.Key.y).Take(4).Select(x => x.Key).ToList();
                foreach (var est in estimates)
                {
                    var hunter = new FrontRoomsMapHunter(world, tuning, body, null, seed++);
                    var arrivedAt = new List<Vector3>();
                    var arrivedTags = new List<string>();
                    hunter.Arrived += (pos, tag) => { arrivedAt.Add(pos); arrivedTags.Add(tag); };
                    hunter.Arm();
                    tries++;
                    var call = new FrontRoomsRelayCall(world.CellCenter(est), 0, FrontRoomsRelayCallCause.Summon, false, false, 20f);
                    if (!hunter.Dispatch(call, FrontRoomsRelayPlacement.Normal, p, p + eye, forward)) continue;
                    var atEntry = Flat(hunter.Position - entry) < .01f;
                    if ((pass == 0) != atEntry) continue;
                    var backstage = hunter.State == HunterState.Arrive && !hunter.Released && arrivedAt.Count == 0;
                    var delay = hunter.ArriveSecondsLeft;
                    var stepAt = -1f;
                    for (var t = 0f; t < 15f && stepAt < 0f; t += Dt)
                    {
                        hunter.Tick(Dt, p, p + eye, forward);
                        if (hunter.State != HunterState.Arrive) stepAt = t + Dt;
                    }
                    var ok = backstage && delay > 0f && stepAt >= delay - Dt && hunter.Released && arrivedAt.Count == 1 && Flat(arrivedAt[0] - hunter.Position) < .01f
                        && (pass == 0 ? arrivedTags[0] == "doorway" : arrivedTags[0] == null);
                    var row = (ok ? "" : "WRONG: ") + "placed " + (atEntry ? "at the entry" : "in " + world.CellOf(hunter.Position)) + " for the estimate " + est + " (" + hunter.LastArriveReport
                        + "), backstage " + backstage + " (Arrive, not released, no Arrived); stepped in " + stepAt.ToString("0.00") + " s later (delay " + delay.ToString("0.00") + " s), Arrived "
                        + arrivedAt.Count + "x with tag " + (arrivedTags.Count > 0 ? (arrivedTags[0] ?? "none") : "-");
                    if (pass == 0) entryRow = row;
                    else plainRow = row;
                    break;
                }
            }
        }
        Check(entryRow != null && !entryRow.StartsWith("WRONG"), "relay entry (v2): Arrive at the module's entry behind the player: "
            + (entryRow ?? "never placed at the entry in " + tries + " tries from " + players.Count + " player cells"));
        Check(plainRow != null && !plainRow.StartsWith("WRONG"), "relay entry (v2): an arrival in an unmarked cell has no tag: "
            + (plainRow ?? "never placed in an unmarked cell in " + tries + " tries from " + players.Count + " player cells"));
    }

    /// <summary>
    /// Relay v2 (V3a): a call placed for a hunter that is Away, as the director does (Normal): the estimate at the cells
    /// 5-10 walking cells from the player, the nearest first, at most 40 tries; true at the first placement (Arrive).
    /// </summary>
    static bool DispatchNear(FrontRoomsMapWorld world, FrontRoomsMapHunter hunter, Vector3 feet, Vector3 forward, out FrontRoomsRelayCall call)
    {
        call = null;
        var tries = 0;
        foreach (var pair in Walk(world, world.CellOf(feet), 10).OrderBy(x => x.Value).ThenBy(x => x.Key.x).ThenBy(x => x.Key.y))
        {
            if (pair.Value < 5) continue;
            if (++tries > 40) break;
            var c = new FrontRoomsRelayCall(world.CellCenter(pair.Key), 0, FrontRoomsRelayCallCause.Summon, false, false, 20f);
            if (!hunter.Dispatch(c, FrontRoomsRelayPlacement.Normal, feet, feet + Vector3.up * ModuleUnits.PlayerEye, forward)) continue;
            call = c;
            return true;
        }
        return false;
    }
#endif

    static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;

    /// <summary>
    /// For a row about what follows a chase. Under the define (V4b) a chase starts only from a lock-on: the meter is filled, the
    /// gate skipped (DebugLockOn), and <paramref name="tick"/> runs out the hold, the player standing at <paramref name="feet"/>.
    /// Define off it does nothing: v1 chases at first sight.
    /// </summary>
    static void ChaseNowV4b(FrontRoomsMapHunter h, Vector3 feet, Action tick)
    {
#if FRONTROOMS_RELAY_V2
        h.DebugLockOn(feet);
        for (var i = 0; i < 180 && h.State != HunterState.Chase; i++) tick();
#endif
    }

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
        ChaseNowV4b(chaser, outside, () =>
        {
            chaser.Tick(Dt, outside, outside + Vector3.up * ModuleUnits.PlayerEye, into);
            world.TickDoorsForTools(Dt);
            Physics.SyncTransforms();
        });
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
#if FRONTROOMS_RELAY_V2
        // V4b (RELAY 8): a player in a sanctuary cell, in reach and in its cone: no fill, no touch, no lock-on, and the meter it had drains.
        var sanctuaryHunter = new FrontRoomsMapHunter(world, new FrontRoomsHunterTuning(), body, null, 7);
        sanctuaryHunter.DebugPlace(stand, into);
        sanctuaryHunter.DebugHoldLook(0f);
        sanctuaryHunter.GateOpenForTools = true;
        var sanctuaryFed = 0;
        var sanctuaryAcquired = 0;
        sanctuaryHunter.TargetAcquired += p => sanctuaryAcquired++;
        for (var t = 0f; t < 2f; t += Dt)
        {
            sanctuaryHunter.Tick(Dt, doorway, doorway + Vector3.up * ModuleUnits.PlayerEye, -into);
            world.TickDoorsForTools(Dt);
            Physics.SyncTransforms();
            if (sanctuaryHunter.Spotting > 0f || sanctuaryHunter.TouchingPlayer || sanctuaryHunter.SeesPlayer) sanctuaryFed++;
        }
        Check(world.InStairwell(world.CellOf(doorway)) && sanctuaryFed == 0 && sanctuaryAcquired == 0,
            label + "safe zone (V4b): the player in the doorway, in its cone and 0.6 m off: no fill, no touch, no lock-on (" + sanctuaryFed + " ticks fed, " + sanctuaryAcquired + " lock-ons)");
#endif

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
#if FRONTROOMS_RELAY_V2
            // V4a (RELAY 7): the range is loudness x the tier's hearing, never tuning.hearing, and a noise at the edge of it carries k 2,
            // so where that hunt ends is read on an exact one (radius 400, k 0).
            lineDeaf.Noise(onLine, farDistance - 1f);
            var lineEar = new FrontRoomsMapHunter(world, tuning, body, null, 15);
            lineEar.DebugPlace(world.CellCenter(far));
            lineEar.Noise(onLine, farDistance + 1f);
            var lineHeard = world.CellOf(onLine) == x && !lineDeaf.ListenPoint.HasValue && lineEar.ListenPoint.HasValue;
            lineEar = new FrontRoomsMapHunter(world, tuning, body, null, 16);
            lineEar.DebugPlace(world.CellCenter(far));
            lineEar.Noise(onLine, 400f);
#else
            lineDeaf.Noise(onLine, (farDistance - 1f) / tuning.hearing);
            var lineEar = new FrontRoomsMapHunter(world, tuning, body, null, 15);
            lineEar.DebugPlace(world.CellCenter(far));
            lineEar.Noise(onLine, (farDistance + 1f) / tuning.hearing);
            var lineHeard = world.CellOf(onLine) == x && !lineDeaf.ListenPoint.HasValue && lineEar.ListenPoint.HasValue;
#endif
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

#if FRONTROOMS_RELAY_V2
    // ---------- Relay v2 (V3a): Arm, Dispatch, Arrive v2, the step-in, the give-up beat, patrol, Withdraw and Away ----------

    /// <summary>What one run of the v2 loop did, Arm to Away (seconds from the dispatch; -1: never).</summary>
    sealed class V2Run
    {
        public FrontRoomsMapHunter hunter;
        public bool placed, backstage, hardOk, secondRefused, huntToPoint;
        public float delay = -1f, stepAt = -1f, huntAt = -1f, gaveUpAt = -1f, patrolAt = -1f, withdrawAt = -1f, awayAt = -1f;
        public int arrived, gaveUp, withdrew;
        public HunterState gaveUpState = HunterState.Dormant;
        public Vector3 arrivedAt, stepPos;
        public string arriveReport = "";
        public readonly List<FrontRoomsRelayAwayReason> away = new List<FrontRoomsRelayAwayReason>();

        public string Times() => "step-in " + stepAt.ToString("0.00") + " s (delay " + delay.ToString("0.00") + "), hunt " + huntAt.ToString("0.00") + ", gave up " + gaveUpAt.ToString("0.00")
            + " (" + gaveUpState + ", " + gaveUp + "x), patrol " + patrolAt.ToString("0.00") + ", Withdraw " + withdrawAt.ToString("0.00") + ", Away " + awayAt.ToString("0.00")
            + " (" + (away.Count > 0 ? string.Join(", ", away) : "none") + ", Withdrew " + withdrew + "x)";
    }

    /// <summary>
    /// A blind Relay (it never sees the player) armed and dispatched near a player standing still, then ticked until it is
    /// Away again (at most 120 s): its loop runs on its own clocks. <paramref name="fadeAtStepIn"/>: FADE at the step-in.
    /// </summary>
    static V2Run RunV2Loop(FrontRoomsMapWorld world, Collider body, int seed, FrontRoomsTierV2 tier, bool fadeAtStepIn, Vector3 feet, Vector3 forward)
    {
        var r = new V2Run();
        var eye = feet + Vector3.up * ModuleUnits.PlayerEye;
        var h = new FrontRoomsMapHunter(world, new FrontRoomsHunterTuning { sightRange = 0f }, body, null, seed);
        r.hunter = h;
        if (tier != null) h.TierV2 = tier;
        var t = 0f;
        h.Arrived += (p, tag) => { r.arrived++; r.arrivedAt = p; };
        h.GaveUp += p =>
        {
            r.gaveUp++;
            if (r.gaveUpAt >= 0f) return;
            r.gaveUpAt = t + Dt;
            r.gaveUpState = h.State;
        };
        h.Withdrew += p => r.withdrew++;
        h.WentAway += reason => r.away.Add(reason);
        h.Arm();
        r.placed = DispatchNear(world, h, feet, forward, out var call);
        if (!r.placed) return r;
        r.arriveReport = h.LastArriveReport;
        var p0 = h.Position;
        var cell = world.CellOf(p0);
        r.backstage = h.State == HunterState.Arrive && !h.Released && !h.OnMap && h.Armed && r.arrived == 0 && h.WarnStage == 0 && h.CurrentCall == call && h.ArriveHuntPoint.HasValue;
        r.hardOk = world.IsBuilt(cell) && !world.InStartArea(cell) && !world.InStairwell(cell) && 3f * Flat(p0 - feet) >= FrontRoomsMapHunter.ArriveMinW - .01f
            && Physics.Linecast(eye, p0 + Vector3.up * 1.6f) && Physics.Linecast(eye, p0 + Vector3.up * 1.95f);
        r.secondRefused = !h.Dispatch(call, FrontRoomsRelayPlacement.Normal, feet, eye, forward) && h.State == HunterState.Arrive && (h.Position - p0).sqrMagnitude < 1e-8f;
        r.delay = h.ArriveSecondsLeft;
        for (; t < 120f && r.awayAt < 0f; t += Dt)
        {
            h.Tick(Dt, feet, eye, forward);
            world.TickDoorsForTools(Dt);
            Physics.SyncTransforms();
            var now = t + Dt;
            if (r.stepAt < 0f && h.State != HunterState.Arrive)
            {
                r.stepAt = now;
                r.stepPos = h.Position;
                if (fadeAtStepIn) h.Fade(false);
            }
            if (r.stepAt >= 0f && r.huntAt < 0f && h.State == HunterState.Hunt)
            {
                r.huntAt = now;
                r.huntToPoint = h.ListenPoint.HasValue && h.ArriveHuntPoint.HasValue && (h.ListenPoint.Value - h.ArriveHuntPoint.Value).sqrMagnitude < 1e-6f;
            }
            if (r.patrolAt < 0f && h.State == HunterState.Wander) r.patrolAt = now;
            if (r.withdrawAt < 0f && h.State == HunterState.Withdraw) r.withdrawAt = now;
            if (h.State == HunterState.Away) r.awayAt = now;
        }
        return r;
    }

    /// <summary>
    /// The hunter's v2 loop (RELAY 4, 5, 11; step V3a) on a built map, a blind Relay and a player standing at the spawn:
    /// - Arm: Dispatch is refused while Dormant; Dormant to Away once (armed, off the map, not released); Away ticks change
    ///   nothing (no timed release).
    /// - Dispatch (Normal, Arrive v2): Arrive backstage (not released, no Arrived, stage 0, the call kept, a hunt point); the
    ///   hard rules hold where it stands (built, not the start area or a stairwell, 14 m straight or more for W at least
    ///   42 m, unseen at 1.6 m and 1.95 m); a second Dispatch is refused (only from Away).
    /// - The step-in after the tier's arrive delay (3 s): released, Arrived once, there; Listen 1.5 s, then the hunt to
    ///   ArriveHuntPoint.
    /// - Nothing found: GaveUp once, in Listen (the 2 s beat); no patrol at tier 1; Withdraw, then Away within the 25 s
    ///   backstop, Withdrew and WentAway once with a map reason; off the map, still armed.
    /// - A tier-3 patrol (patrolSeconds 15): Wander 2 s after the beat began, at most 15 s, then Withdraw; under FADE none.
    /// - SendAway(Test) from Arrive: WentAway(Test) and no Withdrew; a new Dispatch works from Away.
    /// - An unbuilt cell under it: Away (UnbuiltCell) at once (no leash).
    /// - FADE: a noise gets one cautious look (the second is not heard); without FADE both are (V4a: 1.5 s apart).
    /// - Disarm: Dormant, not armed.
    /// </summary>
    static void RelayV2Loop(List<GameObject> roots)
    {
        const string label = "relay v2 loop: ";
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), -73f, "MAP INTERACTION TEST / relay v2 loop");
        world.BuildForCapture();
        var playerRoot = new GameObject("MAP INTERACTION TEST / relay v2 player") { hideFlags = HideFlags.DontSave };
        roots.Add(playerRoot);
        var body = playerRoot.AddComponent<CapsuleCollider>();
        body.height = ModuleUnits.PlayerHeight;
        body.radius = ModuleUnits.PlayerRadius;
        body.center = Vector3.up * (ModuleUnits.PlayerHeight * .5f);
        var feet = world.CellCenter(world.CellOf(world.SpawnWorldPosition));
        playerRoot.transform.position = feet;
        Physics.SyncTransforms();
        var eye = feet + Vector3.up * ModuleUnits.PlayerEye;
        var forward = Vector3.forward;
        var tuning = new FrontRoomsHunterTuning { sightRange = 0f };

        // 1. Arm.
        var h0 = new FrontRoomsMapHunter(world, tuning, body, null, 51);
        var states0 = new List<HunterState>();
        h0.StateChanged += s => states0.Add(s);
        var dormantCall = new FrontRoomsRelayCall(world.CellCenter(world.CellOf(feet) + new GridCoord(6, 0)), 0, FrontRoomsRelayCallCause.Summon, false, false, 20f);
        var refused = !h0.Dispatch(dormantCall, FrontRoomsRelayPlacement.Normal, feet, eye, forward) && h0.State == HunterState.Dormant && !h0.Armed;
        h0.Arm();
        h0.Arm();
        var at0 = h0.Position;
        var idle = true;
        for (var t = 0f; t < 5f; t += Dt)
        {
            h0.Tick(Dt, feet, eye, forward);
            idle &= h0.State == HunterState.Away && !h0.Released && !h0.OnMap && h0.WarnStage == 0 && !h0.PlayerSeesRelay;
        }
        Check(refused && idle && h0.Armed && states0.Count == 1 && states0[0] == HunterState.Away && (h0.Position - at0).sqrMagnitude < 1e-8f && h0.Relays == 0,
            label + "Arm: Dispatch refused while Dormant (" + refused + "); Dormant -> Away once (" + string.Join(", ", states0) + "), armed, off the map, not released; 5 s of Away ticks change nothing ("
            + idle + ", no timed release)");

        // 2-4. A tier-1 run (no patrol).
        var r1 = RunV2Loop(world, body, 52, new FrontRoomsTierV2(), false, feet, forward);
        Check(r1.placed && r1.backstage && r1.hardOk && r1.secondRefused,
            label + "Dispatch (Normal): " + (r1.placed ? r1.arriveReport : "no placement in 40 estimates") + "; backstage (Arrive, not released, no Arrived, stage 0, the call kept) " + r1.backstage
            + "; the hard rules where it stands (built, not the start area or a stairwell, 14 m straight or more, unseen at 1.6 and 1.95 m) " + r1.hardOk + "; a second Dispatch refused " + r1.secondRefused);
        var stepOk = r1.placed && Mathf.Abs(r1.delay - 3f) < 1e-3f && r1.stepAt >= r1.delay - 2f * Dt && r1.stepAt <= r1.delay + 2f * Dt && r1.arrived == 1 && Flat(r1.arrivedAt - r1.stepPos) < .01f;
        var huntOk = r1.huntAt > 0f && r1.huntAt - r1.stepAt >= FrontRoomsMapHunter.StepInListenSeconds - Dt && r1.huntAt - r1.stepAt <= FrontRoomsMapHunter.StepInListenSeconds + 2f * Dt && r1.huntToPoint;
        Check(stepOk && huntOk, label + "the step-in after the tier's arrive delay, Arrived once there, released; Listen 1.5 s, then the hunt to ArriveHuntPoint (" + r1.huntToPoint + "): " + r1.Times());
        var beat = r1.gaveUpAt > 0f && r1.gaveUp == 1 && r1.gaveUpState == HunterState.Listen && r1.patrolAt < 0f && r1.withdrawAt - r1.gaveUpAt >= FrontRoomsMapHunter.GiveUpSeconds - Dt
            && r1.withdrawAt - r1.gaveUpAt <= FrontRoomsMapHunter.GiveUpSeconds + 2f * Dt;
        var gone = r1.awayAt > 0f && r1.awayAt - r1.withdrawAt <= FrontRoomsMapHunter.WithdrawBackstopSeconds + 1f && r1.withdrew == 1 && r1.away.Count == 1
            && r1.away[0] != FrontRoomsRelayAwayReason.Test && r1.away[0] != FrontRoomsRelayAwayReason.UnbuiltCell && r1.away[0] != FrontRoomsRelayAwayReason.GiveUp
            && r1.hunter.LastAwayReason == r1.away[0] && !r1.hunter.Released && !r1.hunter.OnMap && r1.hunter.Armed && r1.hunter.WarnStage == 0;
        Check(beat && gone, label + "nothing found: the give-up beat (GaveUp once, Listen 2 s), no patrol at tier 1, Withdraw, then Away within the 25 s backstop (Withdrew and WentAway once, a map reason), "
            + "off the map and still armed: " + r1.Times());

        // 5. Tier 3's patrol, and none under FADE.
        var r3 = RunV2Loop(world, body, 53, new FrontRoomsTierV2 { patrolSeconds = 15f }, false, feet, forward);
        var patrolOk = r3.placed && r3.gaveUpAt > 0f && r3.patrolAt > 0f && r3.patrolAt - r3.gaveUpAt >= FrontRoomsMapHunter.GiveUpSeconds - Dt && r3.patrolAt - r3.gaveUpAt <= FrontRoomsMapHunter.GiveUpSeconds + 2f * Dt
            && r3.withdrawAt > r3.patrolAt && r3.withdrawAt - r3.patrolAt <= 15f + 2f * Dt && r3.away.Count == 1;
        var rf = RunV2Loop(world, body, 54, new FrontRoomsTierV2 { patrolSeconds = 15f }, true, feet, forward);
        var fadeOk = rf.placed && rf.gaveUpAt > 0f && rf.patrolAt < 0f && rf.withdrawAt - rf.gaveUpAt >= FrontRoomsMapHunter.GiveUpSeconds - Dt && rf.withdrawAt - rf.gaveUpAt <= FrontRoomsMapHunter.GiveUpSeconds + 2f * Dt
            && rf.away.Count == 1;
        Check(patrolOk && fadeOk, label + "a tier-3 patrol (15 s): Wander after the beat, then Withdraw: " + r3.Times() + "; under FADE no patrol: " + rf.Times());

        // 6. SendAway from Arrive.
        var h4 = new FrontRoomsMapHunter(world, tuning, body, null, 55);
        var away4 = new List<FrontRoomsRelayAwayReason>();
        var withdrew4 = 0;
        h4.WentAway += reason => away4.Add(reason);
        h4.Withdrew += p => withdrew4++;
        h4.Arm();
        var placed4 = DispatchNear(world, h4, feet, forward, out _);
        h4.SendAway(FrontRoomsRelayAwayReason.Test);
        var sentAway = placed4 && h4.State == HunterState.Away && away4.Count == 1 && away4[0] == FrontRoomsRelayAwayReason.Test && withdrew4 == 0
            && h4.LastAwayReason == FrontRoomsRelayAwayReason.Test && !h4.Released && h4.Armed;
        var again = DispatchNear(world, h4, feet, forward, out _) && h4.State == HunterState.Arrive;
        Check(sentAway && again, label + "SendAway(Test) from Arrive: Away, WentAway(" + (away4.Count > 0 ? away4[0].ToString() : "none") + ") once, Withdrew " + withdrew4
            + "x (never on the map); a new Dispatch from Away places it again " + again);

        // 7. An unbuilt cell under it.
        var h5 = new FrontRoomsMapHunter(world, tuning, body, null, 56);
        var away5 = new List<FrontRoomsRelayAwayReason>();
        h5.WentAway += reason => away5.Add(reason);
        h5.Arm();
        h5.DebugPlace(world.CellCenter(world.CellOf(feet) + new GridCoord(400, 400)));
        var onMap5 = h5.OnMap;
        h5.Tick(Dt, feet, eye, forward);
        Check(onMap5 && h5.State == HunterState.Away && away5.Count == 1 && away5[0] == FrontRoomsRelayAwayReason.UnbuiltCell && h5.LastAwayReason == FrontRoomsRelayAwayReason.UnbuiltCell,
            label + "placed on an unbuilt cell it goes Away (" + (away5.Count > 0 ? away5[0].ToString() : "none") + ") on its next tick, no leash: " + h5.State);

        // 8. FADE: one cautious look.
        var sources = Walk(world, world.CellOf(feet), 6).Where(x => x.Value >= 3).OrderBy(x => x.Value).ThenBy(x => x.Key.x).ThenBy(x => x.Key.y)
            .Select(x => world.CellCenter(x.Key)).Take(2).ToList();
        if (sources.Count == 2)
        {
            var h6 = new FrontRoomsMapHunter(world, tuning, body, null, 57);
            h6.Arm();
            h6.DebugPlace(feet);
            h6.Fade(false);
            h6.Noise(sources[0], 1000f);
            var heardFirst = h6.State == HunterState.Hunt && h6.ListenPoint.HasValue && (h6.ListenPoint.Value - sources[0]).sqrMagnitude < 1e-6f;
            h6.Noise(sources[1], 1000f);
            var secondIgnored = h6.ListenPoint.HasValue && (h6.ListenPoint.Value - sources[0]).sqrMagnitude < 1e-6f;
            var h7 = new FrontRoomsMapHunter(world, tuning, body, null, 58);
            h7.Arm();
            h7.DebugPlace(feet);
            h7.Noise(sources[0], 1000f);
            // V4a (RELAY 7): at most one re-target per 1.5 s, so the second comes after it.
            for (var k = 0; k < 91; k++) h7.Tick(Dt, feet, eye, forward);
            h7.Noise(sources[1], 1000f);
            var bothHeard = h7.ListenPoint.HasValue && (h7.ListenPoint.Value - sources[1]).sqrMagnitude < 1e-6f;
            Check(heardFirst && secondIgnored && bothHeard && h6.Fading && !h7.Fading,
                label + "under FADE a noise gets one cautious look (the first heard " + heardFirst + ", the second not " + secondIgnored + "); without FADE both are heard " + bothHeard);
        }
        else Check(false, label + "no two noise cells 3-6 cells from the player");

        // 9. Disarm.
        r1.hunter.Disarm();
        Check(r1.hunter.State == HunterState.Dormant && !r1.hunter.Armed && !r1.hunter.Released && !r1.hunter.OnMap,
            label + "Disarm (a floor change): Dormant, not armed (" + r1.hunter.State + ")");
    }
#endif

#if FRONTROOMS_RELAY_V2
    // ---------- Relay v2 (V3b): door licences OPEN / BREAK / WALL, the Ajar push, Withdraw's one OPEN, re-dispatch, the sweep ----------

    /// <summary>A V3b test player: a capsule on a root of its own.</summary>
    static CapsuleCollider BodyV3b(List<GameObject> roots, string name)
    {
        var root = new GameObject(name) { hideFlags = HideFlags.DontSave };
        roots.Add(root);
        var body = root.AddComponent<CapsuleCollider>();
        body.height = ModuleUnits.PlayerHeight;
        body.radius = ModuleUnits.PlayerRadius;
        body.center = Vector3.up * (ModuleUnits.PlayerHeight * .5f);
        return body;
    }

    /// <summary>A shut door the Relay may open with open cells straight on from it on both sides: c behind a, e beyond b.</summary>
    static bool LineDoorV3b(FrontRoomsMapWorld world, out GridCoord a, out GridCoord b, out GridCoord c, out GridCoord e, Func<FrontRoomsMapWorld.Door, GridCoord, bool> where = null)
    {
        e = default;
        if (!FindDoor(world, out a, out b, out c, (d, near) =>
            {
                var far = d.a == near ? d.b : d.a;
                var beyond = new GridCoord(2 * far.x - near.x, 2 * far.y - near.y);
                return world.RelayMayOpen(d) && world.IsBuilt(beyond) && world.PassageBetween(far, beyond) == FrontRoomsMapWorld.Passage.Open
                    && (where == null || where(d, near));
            })) return false;
        e = new GridCoord(2 * b.x - a.x, 2 * b.y - a.y);
        return true;
    }

    /// <summary>The nearest cell (by walking from the spawn) at least <paramref name="metres"/> straight from a point: where the player stands, out of the way.</summary>
    static Vector3 FarFeetV3b(FrontRoomsMapWorld world, Vector3 from, float metres)
    {
        foreach (var pair in Walk(world, world.CellOf(world.SpawnWorldPosition), 40).OrderBy(x => x.Value).ThenBy(x => x.Key.x).ThenBy(x => x.Key.y))
        {
            var p = world.CellCenter(pair.Key);
            if (Flat(p - from) >= metres) return p;
        }
        return from + new Vector3(metres, 0f, 0f);
    }

    /// <summary>
    /// Walking cells from a cell (open passages and Ajar doors; shut doors too when <paramref name="doors"/>), never across
    /// the edge between <paramref name="blockA"/> and <paramref name="blockB"/>, up to maxDepth. <paramref name="from"/>
    /// (optional) gets each cell's previous cell.
    /// </summary>
    static Dictionary<GridCoord, int> WalkV3b(FrontRoomsMapWorld world, GridCoord start, int maxDepth, bool doors, GridCoord blockA, GridCoord blockB, Dictionary<GridCoord, GridCoord> from = null)
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
                if ((cell == blockA && n == blockB) || (cell == blockB && n == blockA)) continue;
                var p = world.PassageBetween(cell, n);
                if (p == FrontRoomsMapWorld.Passage.Wall || p == FrontRoomsMapWorld.Passage.Glass) continue;
                if (p == FrontRoomsMapWorld.Passage.ClosedDoor && !doors) continue;
                depth[n] = depth[cell] + 1;
                if (from != null) from[n] = cell;
                queue.Enqueue(n);
            }
        }
        return depth;
    }

    /// <summary>The player shuts a door (as ShutAndLatch, with a stand-in body for the hand); nothing tells the Relay (unheard).</summary>
    static void ShutUnheardV3b(FrontRoomsMapWorld world, FrontRoomsMapWorld.Door door, Collider hand)
    {
        var keep = world.Player.position;
        if (!door.open)
        {
            world.Use(door.leaf);
            for (var k = 0; k < 90; k++) world.TickDoorsForTools(Dt);
        }
        ShutAndLatch(world, door, world.Player, hand);
        world.Player.position = keep;
        hand.transform.position = new Vector3(0f, -100f, 0f);
        Physics.SyncTransforms();
    }

    /// <summary>One V3b hunter run: its door events (with the time and state of each), the give-up beat, Withdraw and Away.</summary>
    sealed class DoorRunV3b
    {
        public readonly FrontRoomsMapHunter h;
        public float t, walledAt = -1f, gaveUpAt = -1f, withdrawAt = -1f, awayAt = -1f, breakAt = -1f, wanderAt = -1f, huntAt = -1f, stepAt = -1f;
        public int opened, pushed, walled, gaveUp, blows, withdrew;
        public HunterState gaveUpState = HunterState.Dormant;
        public readonly List<float> openedTimes = new List<float>(), pushedTimes = new List<float>();
        public readonly List<HunterState> openedStates = new List<HunterState>();
        public readonly List<Vector3> openedAt = new List<Vector3>(), openedFrom = new List<Vector3>(), pushedFrom = new List<Vector3>();
        public readonly List<string> openedLicences = new List<string>();
        public readonly List<FrontRoomsRelayAwayReason> away = new List<FrontRoomsRelayAwayReason>();

        public DoorRunV3b(FrontRoomsMapHunter hunter)
        {
            h = hunter;
            h.RelayDoorOpened += p =>
            {
                opened++;
                openedTimes.Add(t);
                openedStates.Add(h.State);
                openedAt.Add(p);
                openedFrom.Add(h.Position);
                openedLicences.Add(h.DoorLicences);
            };
            h.RelayDoorPushed += p =>
            {
                pushed++;
                pushedTimes.Add(t);
                pushedFrom.Add(h.Position);
            };
            h.DoorWalled += p =>
            {
                walled++;
                if (walledAt < 0f) walledAt = t;
            };
            h.DoorBlow += p => blows++;
            h.GaveUp += p =>
            {
                gaveUp++;
                if (gaveUpAt >= 0f) return;
                gaveUpAt = t;
                gaveUpState = h.State;
            };
            h.Withdrew += p => withdrew++;
            h.WentAway += reason => away.Add(reason);
        }

        /// <summary>Ticks the hunter and the doors (the player standing at <paramref name="feet"/>) for at most <paramref name="seconds"/>, until <paramref name="until"/>.</summary>
        public void Run(FrontRoomsMapWorld world, Vector3 feet, Vector3 forward, float seconds, Func<bool> until)
        {
            var eye = feet + Vector3.up * ModuleUnits.PlayerEye;
            var end = t + seconds;
            while (t < end - 1e-4f)
            {
                t += Dt;
                var before = h.State;
                h.Tick(Dt, feet, eye, forward);
                world.TickDoorsForTools(Dt);
                Physics.SyncTransforms();
                if (stepAt < 0f && before == HunterState.Arrive && h.State != HunterState.Arrive) stepAt = t;
                if (breakAt < 0f && h.State == HunterState.BreakDoor) breakAt = t;
                if (huntAt < 0f && h.State == HunterState.Hunt) huntAt = t;
                if (wanderAt < 0f && h.State == HunterState.Wander) wanderAt = t;
                if (withdrawAt < 0f && h.State == HunterState.Withdraw) withdrawAt = t;
                if (awayAt < 0f && h.State == HunterState.Away) awayAt = t;
                if (until != null && until()) return;
            }
        }
    }

    /// <summary>
    /// Relay v2 step V3b, the map's half of RELAY 10's push (FrontRoomsMapWorld, nothing calls it before the define): OPEN
    /// leaves a door Ajar (Passage.Ajar, cost AjarOpening, not open, 30 deg away from the Relay); PushAjar takes the leaf on
    /// to its rest on the same side, passable within 0.4 s, the door still Ajar by edge; SettleAjar brings it back to 30 deg
    /// in 1.0 s; a shut or an open door is neither pushed nor settled; the player's E opens an Ajar door fully.
    /// </summary>
    static void RelayV3bAjar(List<GameObject> roots)
    {
        const string label = "relay v3b ajar push: ";
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), -77f, "MAP INTERACTION TEST / relay v3b ajar");
        world.BuildForCapture();
        if (!LineDoorV3b(world, out var a, out var b, out _, out _)) { Check(false, label + "no shut door the Relay may open near the spawn"); return; }
        var door = world.DoorBetween(a, b);
        var relay = world.CellCenter(a);
        world.Player.position = relay + Vector3.up * .05f;
        Physics.SyncTransforms();
        void Run(float seconds) { for (var s2 = 0f; s2 < seconds - 1e-4f; s2 += Dt) world.TickDoorsForTools(Dt); }

        var mayOpen = world.RelayMayOpen(door);
        var shutRefused = !world.PushAjar(door, relay) && !world.SettleAjar(door) && door.angle == 0f;
        var opened = world.OpenAjar(door, relay);
        Run(FrontRoomsMapWorld.RelayOpenSeconds + .1f);
        var ajarAngle = door.angle;
        var ajarCost = world.OpeningCostBetween(a, b);
        var ajarOk = opened && world.PassageBetween(a, b) == FrontRoomsMapWorld.Passage.Ajar && Mathf.Abs(ajarCost - FrontRoomsMapPathField.AjarOpening) < 1e-4f
            && !door.open && world.IsAjar(door) && Mathf.Abs(ajarAngle - FrontRoomsMapWorld.AjarDegrees) < .01f && FrontRoomsMapWorld.OnSwingSide(door, world.CellCenter(b));
        Check(mayOpen && shutRefused && ajarOk,
            label + "OPEN leaves the door Ajar: RelayMayOpen " + mayOpen + "; a shut door is neither pushed nor settled (" + shutRefused + "); OpenAjar -> " + world.PassageBetween(a, b)
            + ", cost " + ajarCost.ToString("0.#", inv) + " m, door.open " + door.open + ", IsAjar " + world.IsAjar(door) + ", " + ajarAngle.ToString("0.0", inv) + " deg away from the Relay");

        var pushed = world.PushAjar(door, relay);
        var passableAt = -1f;
        var sideKept = true;
        for (var t = 0f; t < 1f - 1e-4f; t += Dt)
        {
            world.TickDoorsForTools(Dt);
            if (passableAt < 0f && door.angle >= FrontRoomsMapWorld.DoorPassableDegrees) passableAt = t + Dt;
            if (door.angle > 1f) sideKept &= FrontRoomsMapWorld.OnSwingSide(door, world.CellCenter(b));
        }
        var pushedAngle = door.angle;
        var pushOk = pushed && passableAt > 0f && passableAt <= FrontRoomsMapWorld.RelayPushSeconds + 2f * Dt && pushedAngle >= FrontRoomsMapWorld.DoorPassableDegrees && sideKept
            && world.PassageBetween(a, b) == FrontRoomsMapWorld.Passage.Open && world.IsAjar(door) && !door.open;
        Check(pushOk,
            label + "PushAjar takes the leaf on to " + pushedAngle.ToString("0", inv) + " deg on its own side (" + sideKept + "), passable " + passableAt.ToString("0.00", inv) + " s after the push (0.4 s); "
            + world.PassageBetween(a, b) + ", still Ajar by edge " + world.IsAjar(door) + ", not open " + !door.open);

        var settled = world.SettleAjar(door);
        Run(FrontRoomsMapWorld.RelayOpenSeconds + .1f);
        var settledAngle = door.angle;
        var settleOk = settled && Mathf.Abs(settledAngle - FrontRoomsMapWorld.AjarDegrees) < .01f && world.PassageBetween(a, b) == FrontRoomsMapWorld.Passage.Ajar && world.IsAjar(door) && !door.open
            && FrontRoomsMapWorld.OnSwingSide(door, world.CellCenter(b)) && !world.SettleAjar(door);
        Check(settleOk,
            label + "SettleAjar brings the leaf back to " + settledAngle.ToString("0.0", inv) + " deg in 1.0 s (" + world.PassageBetween(a, b) + ", Ajar " + world.IsAjar(door) + "); a second settle is refused");

        var use = world.Use(door.leaf);
        Run(3f);
        var eOk = use == FrontRoomsMapWorld.UseResult.Opened && door.open && door.angle >= FrontRoomsMapWorld.DoorPassableDegrees && !world.IsAjar(door)
            && world.PassageBetween(a, b) == FrontRoomsMapWorld.Passage.Open && !world.PushAjar(door, relay) && !world.SettleAjar(door);
        Check(eOk,
            label + "E opens a door the Relay left Ajar fully (" + use + ", " + door.angle.ToString("0", inv) + " deg, Ajar " + world.IsAjar(door) + "); an open door is neither pushed nor settled");
        world.ResetDoorForTools(door);
    }

    /// <summary>
    /// Relay v2 step V3b, the hunter at doors (RELAY 5, 10), a blind Relay sent on foot (Redirect: the dispatch's OPEN
    /// licences) along a straight line of cells c, a, b, e through one shut door:
    /// - OPEN from either side: licensed at the dispatch; at the door's face it OPENs (RelayDoorOpened once, the leaf away
    ///   from it), 1.0 s later it pushes (RelayDoorPushed once), walks through, and the leaf settles back Ajar once it is
    ///   clear; the licence is spent; no blow, no break, no WALL.
    /// - An Ajar door from the side its leaf hangs to: no OPEN, no licence needed, one push from the stance clear of the
    ///   leaf's sweep (a push the leaf ran into its body would be pushed again).
    /// - "Shut the door and walk away ends an investigation": the door open at the dispatch (no licence), shut by the player
    ///   unheard: a WALL (no way round within 1.5 times): it walks to the door, listens 2 s, gives up (the beat, no patrol
    ///   even with a tier patrol) and Withdraws; the door stays shut and whole.
    /// - In a chase, the door it hears move (a sound at the door's centre) is a BREAK licence and nothing else: the chase
    ///   breaks through; without it (the player never crossed in its sight) the chase meets a WALL and breaks nothing. A
    ///   device in a chase is heard only (RedispatchKind NoiseOnly).
    /// </summary>
    static void RelayV3bDoors(List<GameObject> roots)
    {
        const string label = "relay v3b doors: ";
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), -79f, "MAP INTERACTION TEST / relay v3b doors");
        world.BuildForCapture();
        var body = BodyV3b(roots, "MAP INTERACTION TEST / relay v3b doors player");
        var hand = BodyV3b(roots, "MAP INTERACTION TEST / relay v3b doors hand");
        if (!LineDoorV3b(world, out var a, out var b, out var c, out var e)) { Check(false, label + "no shut door the Relay may open with open cells straight on from it near the spawn"); return; }
        var door = world.DoorBetween(a, b);
        var feet = FarFeetV3b(world, door.position, 24f);
        body.transform.position = feet;
        hand.transform.position = new Vector3(0f, -100f, 0f);
        Physics.SyncTransforms();
        var forward = Vector3.forward;
        var tuning = new FrontRoomsHunterTuning { sightRange = 0f };
        var mouth = world.CrossingPoint(a, b);

        // 1-2. OPEN from a, then from b.
        for (var side = 0; side < 2; side++)
        {
            world.ResetDoorForTools(door);
            for (var k = 0; k < 10; k++) world.TickDoorsForTools(Dt);
            var start = side == 0 ? c : e;
            var near = side == 0 ? a : b;
            var far = side == 0 ? b : a;
            var goal = side == 0 ? e : c;
            var h = new FrontRoomsMapHunter(world, tuning, body, null, 61 + side);
            var r = new DoorRunV3b(h);
            h.DebugPlace(world.CellCenter(start));
            var sent = h.Redirect(new FrontRoomsRelayCall(world.CellCenter(goal), 0, FrontRoomsRelayCallCause.Device, true, false, 20f));
            var licensed = sent && h.HasOpenLicence(door) && !h.HasBreakLicence(door) && h.State == HunterState.Hunt;
            float crossedAt = -1f, settledAt = -1f;
            var leafAway = false;
            r.Run(world, feet, forward, 15f, () =>
            {
                if (r.opened == 1 && r.pushed == 0 && door.angle > 1f) leafAway = FrontRoomsMapWorld.OnSwingSide(door, world.CellCenter(far)) && !FrontRoomsMapWorld.OnSwingSide(door, world.CellCenter(near));
                if (crossedAt < 0f && world.CellOf(h.Position) == far) crossedAt = r.t;
                if (crossedAt >= 0f && settledAt < 0f && Mathf.Abs(door.angle - FrontRoomsMapWorld.AjarDegrees) < .01f && world.IsAjar(door)) settledAt = r.t;
                return settledAt >= 0f;
            });
            var openAt = r.openedTimes.Count > 0 ? r.openedTimes[0] : -1f;
            var pushAt = r.pushedTimes.Count > 0 ? r.pushedTimes[0] : -1f;
            var face = r.openedFrom.Count > 0 && world.CellOf(r.openedFrom[0]) == near && Flat(r.openedFrom[0] - mouth) < 1.2f;
            var timing = openAt > 0f && pushAt - openAt >= FrontRoomsMapWorld.RelayOpenSeconds - 2f * Dt && pushAt - openAt <= FrontRoomsMapWorld.RelayOpenSeconds + 3f * Dt
                && crossedAt > pushAt && crossedAt - pushAt <= 2f;
            var restOk = settledAt > 0f && world.IsAjar(door) && !door.open && !door.broken && FrontRoomsMapWorld.OnSwingSide(door, world.CellCenter(far));
            Check(licensed && r.opened == 1 && r.pushed == 1 && leafAway && face && timing && restOk && !h.HasOpenLicence(door) && h.DoorsOpened == 1 && h.DoorsPushed == 1
                  && r.walled == 0 && r.blows == 0 && h.DoorsBroken == 0 && r.openedStates.Count > 0 && r.openedStates[0] == HunterState.Hunt,
                label + "OPEN from " + (side == 0 ? "a" : "b") + " (" + start + " -> " + goal + "): licensed at the dispatch " + licensed + "; at the face (" + face + ") RelayDoorOpened " + r.opened
                + "x at " + openAt.ToString("0.00", inv) + " s, the leaf away from it " + leafAway + "; pushed " + r.pushed + "x " + (pushAt - openAt).ToString("0.00", inv) + " s later; through into "
                + far + " " + (crossedAt - pushAt).ToString("0.00", inv) + " s after the push; the leaf back Ajar at " + door.angle.ToString("0.0", inv) + " deg " + (settledAt - crossedAt).ToString("0.00", inv)
                + " s after; licence spent " + !h.HasOpenLicence(door) + "; WALL " + r.walled + ", blows " + r.blows + ", breaks " + h.DoorsBroken);
        }

        // 3. The door now rests Ajar with its leaf on a's side (it was OPENed from b): a Relay from a pushes it from the stance clear of the sweep.
        {
            var leafOnA = world.IsAjar(door) && FrontRoomsMapWorld.OnSwingSide(door, world.CellCenter(a));
            var h = new FrontRoomsMapHunter(world, tuning, body, null, 63);
            var r = new DoorRunV3b(h);
            h.DebugPlace(world.CellCenter(c));
            var sent = h.Redirect(new FrontRoomsRelayCall(world.CellCenter(e), 0, FrontRoomsRelayCallCause.Device, true, false, 20f));
            var noLicence = sent && !h.HasOpenLicence(door) && !h.HasBreakLicence(door);
            var maxAngle = 0f;
            var crossedAt = -1f;
            var sideKept = true;
            r.Run(world, feet, forward, 10f, () =>
            {
                maxAngle = Mathf.Max(maxAngle, door.angle);
                if (r.pushed > 0 && crossedAt < 0f && door.angle > 1f) sideKept &= FrontRoomsMapWorld.OnSwingSide(door, world.CellCenter(a));
                if (crossedAt < 0f && world.CellOf(h.Position) == b) crossedAt = r.t;
                return crossedAt >= 0f && r.t - crossedAt > 2.5f;
            });
            var stanceClear = r.pushedFrom.Count > 0 && world.CellOf(r.pushedFrom[0]) == a && !FrontRoomsMapWorld.IsLatched(door);
            Check(leafOnA && noLicence && r.opened == 0 && r.pushed == 1 && stanceClear && maxAngle >= FrontRoomsMapWorld.DoorPassableDegrees && sideKept && crossedAt > 0f
                  && r.walled == 0 && r.blows == 0 && h.DoorsBroken == 0 && world.IsAjar(door) && !door.broken,
                label + "an Ajar door from the side its leaf hangs to (" + leafOnA + "): no licence needed (" + noLicence + "), no OPEN (" + r.opened + "), pushed " + r.pushed
                + "x from the stance clear of the sweep (" + stanceClear + "; a leaf stopped on its body would be pushed again), the leaf to " + maxAngle.ToString("0", inv)
                + " deg on its own side (" + sideKept + "), through into " + b + " " + (crossedAt >= 0f ? "at " + crossedAt.ToString("0.00", inv) + " s" : "never") + "; WALL " + r.walled + ", breaks " + h.DoorsBroken);
        }

        // 4. Shut the door and walk away: open at the dispatch, shut by the player unheard.
        {
            world.ResetDoorForTools(door);
            world.TryOpenDoor(a, b);
            for (var k = 0; k < 90; k++) world.TickDoorsForTools(Dt);
            var h = new FrontRoomsMapHunter(world, tuning, body, null, 64);
            // A tier with a patrol: the WALL's give-up has none all the same.
            h.TierV2 = new FrontRoomsTierV2 { patrolSeconds = 15f };
            var r = new DoorRunV3b(h);
            h.DebugPlace(world.CellCenter(c));
            var sent = h.Redirect(new FrontRoomsRelayCall(world.CellCenter(e), 0, FrontRoomsRelayCallCause.Device, true, false, 20f));
            var noLicence = sent && door.open && !h.HasOpenLicence(door) && !h.HasBreakLicence(door) && h.State == HunterState.Hunt;
            ShutUnheardV3b(world, door, hand);
            var shut = !door.open && world.PassageBetween(a, b) == FrontRoomsMapWorld.Passage.ClosedDoor;
            var gotThrough = false;
            var atWall = false;
            var states = new List<HunterState>();
            h.StateChanged += s => states.Add(s);
            r.Run(world, feet, forward, 40f, () =>
            {
                if (world.CellOf(h.Position) == b) gotThrough = true;
                atWall |= h.AtWall;
                return h.State == HunterState.Away;
            });
            var listen = r.gaveUpAt - r.walledAt;
            var ok = noLicence && shut && r.walled == 1 && h.DoorsWalled == 1 && atWall && r.gaveUp == 1 && r.gaveUpState == HunterState.Listen
                && listen >= FrontRoomsMapHunter.WallListenSeconds - Dt && listen <= FrontRoomsMapHunter.WallListenSeconds + 1.5f
                && r.wanderAt < 0f && r.withdrawAt > 0f && r.withdrawAt - r.gaveUpAt >= FrontRoomsMapHunter.GiveUpSeconds - Dt && r.withdrawAt - r.gaveUpAt <= FrontRoomsMapHunter.GiveUpSeconds + 2f * Dt
                && r.awayAt > 0f && !gotThrough && !door.open && !door.broken && !world.IsAjar(door) && r.opened == 0 && r.blows == 0 && h.DoorsBroken == 0;
            Check(ok,
                label + "shut the door and walk away ends an investigation: open at the dispatch (no licence " + noLicence + "), shut by the player unheard (" + shut + "); WALL " + r.walled
                + "x at " + r.walledAt.ToString("0.00", inv) + " s, listened " + listen.ToString("0.00", inv) + " s (2 s at the door), GaveUp " + r.gaveUp + "x in " + r.gaveUpState
                + ", no patrol (tier patrol 15 s; Wander " + (r.wanderAt >= 0f) + "), Withdraw " + (r.withdrawAt - r.gaveUpAt).ToString("0.00", inv) + " s after the beat began, Away at "
                + r.awayAt.ToString("0.00", inv) + " s (" + (r.away.Count > 0 ? r.away[0].ToString() : "none") + "); never through " + !gotThrough + "; the door shut and whole " + (!door.open && !door.broken)
                + ", OPEN " + r.opened + ", blows " + r.blows + " (" + string.Join(" -> ", states) + ")");
        }

        // 5. A chase: the door it hears move is a BREAK licence (and nothing else); without it the chase meets a WALL.
        var sighted = new FrontRoomsHunterTuning { sightRange = 12f, lostSightSeconds = 6f };
        var across = world.CellCenter(b) - world.CellCenter(a);
        across.y = 0f;
        across.Normalize();
        var inB = mouth + across * 2.5f;
        inB.y = world.CellCenter(b).y;
        var inA = mouth - across * 4.5f;
        inA.y = world.CellCenter(a).y;
        var lookBack = -across;
        for (var heard = 0; heard < 2; heard++)
        {
            world.ResetDoorForTools(door);
            world.TryOpenDoor(a, b);
            for (var k = 0; k < 90; k++) world.TickDoorsForTools(Dt);
            body.transform.position = inB;
            Physics.SyncTransforms();
            var h = new FrontRoomsMapHunter(world, sighted, body, null, 65 + heard);
            var r = new DoorRunV3b(h);
            h.DebugPlace(inA);
            ChaseNowV4b(h, inB, () => r.Run(world, inB, lookBack, Dt, null));
            r.Run(world, inB, lookBack, 2f, () => h.State == HunterState.Chase);
            var chasing = h.State == HunterState.Chase;
            var deviceKind = h.RedispatchKind(world.CellCenter(c), inB, inB + Vector3.up * ModuleUnits.PlayerEye);
            var crossedSeen = h.HasBreakLicence(door);
            ShutUnheardV3b(world, door, hand);
            var listenBefore = h.ListenPoint;
            if (heard == 1) h.Noise(door.position, 1000f);
            var licence = h.HasBreakLicence(door);
            var stillChase = h.State == HunterState.Chase && h.ListenPoint == listenBefore;
            var leftChase = -1f;
            r.Run(world, inB, lookBack, 12f, () =>
            {
                if (leftChase < 0f && h.State != HunterState.Chase && h.State != HunterState.BreakDoor) leftChase = r.t;
                return heard == 1 ? door.broken && h.State != HunterState.BreakDoor : leftChase >= 0f && r.t - leftChase > 3f;
            });
            if (heard == 1)
                Check(chasing && deviceKind == FrontRoomsRelayRedispatchKind.NoiseOnly && !crossedSeen && licence && stillChase && r.breakAt > 0f && door.broken && h.DoorsBroken == 1 && r.walled == 0,
                    label + "in a chase (" + chasing + ") the door it hears move (a sound at the door's centre, after the player shut it) is a BREAK licence " + licence + " (none before: the player never crossed in its sight, "
                    + !crossedSeen + ") and nothing else (still chasing, ListenPoint kept: " + stillChase + "); it breaks through (BreakDoor at " + r.breakAt.ToString("0.00", inv) + " s, broken " + door.broken
                    + ", WALL " + r.walled + "); a device tripped in the chase is " + deviceKind);
            else
                Check(chasing && deviceKind == FrontRoomsRelayRedispatchKind.NoiseOnly && !crossedSeen && !licence && r.walled >= 1 && r.breakAt < 0f && !door.broken && h.DoorsBroken == 0 && leftChase > 0f,
                    label + "in a chase with no licence (the shut unheard, never crossed in its sight): WALL " + r.walled + "x, it waits out its lose meter (the chase over at " + leftChase.ToString("0.00", inv)
                    + " s), then " + h.State + "; no break (" + h.DoorsBroken + ", door broken " + door.broken + "); a device tripped in the chase is " + deviceKind);
        }
        world.ResetDoorForTools(door);
    }

    /// <summary>
    /// Relay v2 step V3b, RELAY 5's WALL against 1.5 times: a door with a way round it (the doors on that way opened first,
    /// so the way round is open passages). The door open at the dispatch and shut by the player unheard: to a goal whose
    /// way round is at most 1.4 times the way through, it plans round (no WALL, it gets there, the door stays shut); to
    /// the cell just beyond the door (way round over 2 times) it is a WALL and the hunt gives up.
    /// </summary>
    static void RelayV3bWallReplan(List<GameObject> roots)
    {
        const string label = "relay v3b WALL vs 1.5x: ";
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), -81f, "MAP INTERACTION TEST / relay v3b wall replan");
        world.BuildForCapture();
        var body = BodyV3b(roots, "MAP INTERACTION TEST / relay v3b wall player");
        var hand = BodyV3b(roots, "MAP INTERACTION TEST / relay v3b wall hand");
        hand.transform.position = new Vector3(0f, -100f, 0f);
        GridCoord a = default, b = default, c = default, e = default, goal = default;
        var found = false;
        var tried = new HashSet<long>();
        var candidates = 0;
        string why = "no shut door with a way round it within 10 cells";
        var through = 0;
        var round = 0;
        while (!found && LineDoorV3b(world, out a, out b, out c, out e, (d, near) => !tried.Contains(d.edge)))
        {
            var door0 = world.DoorBetween(a, b);
            tried.Add(door0.edge);
            if (++candidates > 40) break;
            // The way round (doors allowed), its shut doors opened, so it is open passages.
            var prev = new Dictionary<GridCoord, GridCoord>();
            var roundAll = WalkV3b(world, a, 10, true, a, b, prev);
            if (!roundAll.ContainsKey(b)) continue;
            for (var n = b; n != a; n = prev[n])
                if (world.PassageBetween(prev[n], n) == FrontRoomsMapWorld.Passage.ClosedDoor) world.TryOpenDoor(prev[n], n);
            for (var k = 0; k < 90; k++) world.TickDoorsForTools(Dt);
            var roundOpen = WalkV3b(world, a, 10, false, a, b);
            if (!roundOpen.ContainsKey(b)) continue;
            // Through: the door open (walked as open); round: the door's edge blocked. From c (where it starts) and from a (where it meets the door).
            world.TryOpenDoor(a, b);
            for (var k = 0; k < 90; k++) world.TickDoorsForTools(Dt);
            var tc = WalkV3b(world, c, 40, false, c, c);
            var ta = WalkV3b(world, a, 40, false, a, a);
            var ra = WalkV3b(world, a, 60, false, a, b);
            var allC = WalkV3b(world, c, 40, true, c, c);
            var allCBlocked = WalkV3b(world, c, 60, true, a, b);
            foreach (var pair in ta.OrderBy(x => x.Value).ThenBy(x => x.Key.x).ThenBy(x => x.Key.y))
            {
                var g = pair.Key;
                var d0 = pair.Value;
                if (d0 < 3 || !tc.ContainsKey(g) || tc[g] != d0 + 1 || !ra.ContainsKey(g)) continue;
                if (ra[g] <= d0 || ra[g] > 1.4f * d0) continue;
                // Every shortest way from c (any door) goes through this door.
                if (!allC.ContainsKey(g) || allC[g] != tc[g] || (allCBlocked.ContainsKey(g) && allCBlocked[g] <= tc[g])) continue;
                goal = g;
                through = d0;
                round = ra[g];
                found = true;
                break;
            }
            if (!found)
            {
                why = "no goal with the way round at most 1.4x past " + candidates + " doors";
                world.ResetDoorForTools(door0);
            }
        }
        if (!found) { Check(false, label + why); return; }
        var door = world.DoorBetween(a, b);
        var feet = FarFeetV3b(world, door.position, 30f);
        body.transform.position = feet;
        Physics.SyncTransforms();
        var tuning = new FrontRoomsHunterTuning { sightRange = 0f };

        for (var wall = 0; wall < 2; wall++)
        {
            if (!door.open)
            {
                world.TryOpenDoor(a, b);
                for (var k = 0; k < 90; k++) world.TickDoorsForTools(Dt);
            }
            var target = wall == 0 ? goal : e;
            var h = new FrontRoomsMapHunter(world, tuning, body, null, 67 + wall);
            var r = new DoorRunV3b(h);
            h.DebugPlace(world.CellCenter(c));
            var sent = h.Redirect(new FrontRoomsRelayCall(world.CellCenter(target), 0, FrontRoomsRelayCallCause.Device, true, false, 20f));
            ShutUnheardV3b(world, door, hand);
            var reachedAt = -1f;
            var gotThrough = false;
            r.Run(world, feet, Vector3.forward, 40f, () =>
            {
                if (world.CellOf(h.Position) == b) gotThrough = true;
                if (reachedAt < 0f && world.CellOf(h.Position) == target) reachedAt = r.t;
                return wall == 0 ? reachedAt >= 0f : r.gaveUp > 0;
            });
            var whole = !door.open && !door.broken && !world.IsAjar(door) && h.DoorsBroken == 0 && r.blows == 0;
            if (wall == 0)
                Check(sent && reachedAt > 0f && r.walled == 0 && whole,
                    label + "the way round " + round + " cells vs " + through + " through (" + ((float)round / through).ToString("0.00", inv) + "x, at most 1.5x) from " + a + " to " + target
                    + ": it plans round (WALL " + r.walled + ") and gets there at " + reachedAt.ToString("0.00", inv) + " s; the door stays shut and whole " + whole + " (OPEN " + r.opened + " elsewhere)");
            else
                Check(sent && r.walled == 1 && r.gaveUp == 1 && r.gaveUpState == HunterState.Listen && r.gaveUpAt - r.walledAt >= FrontRoomsMapHunter.WallListenSeconds - Dt && !gotThrough && whole,
                    label + "to " + target + " just beyond the door the way round is " + (round > 0 ? "over 1.5x" : "none") + ": WALL " + r.walled + "x, listened " + (r.gaveUpAt - r.walledAt).ToString("0.00", inv)
                    + " s, GaveUp " + r.gaveUp + "x in " + r.gaveUpState + "; never through " + !gotThrough + "; the door shut and whole " + whole);
        }
        world.ResetDoorForTools(door);
    }

    /// <summary>
    /// Relay v2 step V3b, RELAY 11's Withdraw "failing that, with one OPEN on its own route": a Relay placed by a dispatch
    /// (its arrival cell is exit 1) walks on foot through a shut door it OPENs; the player then shuts that door unheard.
    /// With no way back without a shut door, its Withdraw OPENs that one door (once, in Withdraw) and goes Away.
    /// </summary>
    static void RelayV3bWithdrawOpen(List<GameObject> roots)
    {
        const string label = "relay v3b withdraw OPEN: ";
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), -83f, "MAP INTERACTION TEST / relay v3b withdraw open");
        world.BuildForCapture();
        var body = BodyV3b(roots, "MAP INTERACTION TEST / relay v3b withdraw player");
        var hand = BodyV3b(roots, "MAP INTERACTION TEST / relay v3b withdraw hand");
        hand.transform.position = new Vector3(0f, -100f, 0f);
        var feet = world.CellCenter(world.CellOf(world.SpawnWorldPosition));
        body.transform.position = feet;
        Physics.SyncTransforms();
        var forward = Vector3.forward;
        var tuning = new FrontRoomsHunterTuning { sightRange = 0f };
        var notes = new List<string>();
        string row = null;
        var pass = false;
        for (var seed = 71; seed < 79 && row == null; seed++)
        {
            var h = new FrontRoomsMapHunter(world, tuning, body, null, seed);
            h.Arm();
            if (!DispatchNear(world, h, feet, forward, out _)) { notes.Add(seed + ": no placement"); continue; }
            var r = new DoorRunV3b(h);
            r.Run(world, feet, forward, 10f, () => h.State != HunterState.Arrive);
            if (h.State == HunterState.Arrive) { notes.Add(seed + ": no step-in"); continue; }
            var p = world.CellOf(h.Position);
            // D: a shut door it may open, its near side open walking from P, its far side not (within 64); g: 1-3 open cells past it.
            var open = WalkV3b(world, p, 64, false, p, p);
            FrontRoomsMapWorld.Door pick = null;
            GridCoord g = default;
            foreach (var pair in open.Where(x => x.Value <= 10).OrderBy(x => x.Value).ThenBy(x => x.Key.x).ThenBy(x => x.Key.y))
            {
                foreach (var s in Steps)
                {
                    var f = pair.Key + s;
                    if (open.ContainsKey(f) || !world.IsBuilt(f) || world.PassageBetween(pair.Key, f) != FrontRoomsMapWorld.Passage.ClosedDoor) continue;
                    var d = world.DoorBetween(pair.Key, f);
                    if (d == null || !world.RelayMayOpen(d)) continue;
                    var past = WalkV3b(world, f, 3, false, f, f);
                    var beyond = past.Where(x => x.Value >= 1 && !open.ContainsKey(x.Key)).OrderByDescending(x => x.Value).ThenBy(x => x.Key.x).ThenBy(x => x.Key.y).Select(x => x.Key).ToList();
                    if (beyond.Count == 0) continue;
                    pick = d;
                    g = beyond[0];
                    break;
                }
                if (pick != null) break;
            }
            if (pick == null) { notes.Add(seed + ": no door from " + p); h.SendAway(FrontRoomsRelayAwayReason.Test); continue; }
            h.Redirect(new FrontRoomsRelayCall(world.CellCenter(g), 0, FrontRoomsRelayCallCause.Device, true, false, 20f));
            var reached = false;
            r.Run(world, feet, forward, 25f, () => reached = world.CellOf(h.Position) == g);
            if (!reached || r.opened != 1 || pick.broken || Flat(r.openedAt[0] - pick.position) > .05f) { notes.Add(seed + ": the walk to " + g + " " + (reached ? "opened " + r.opened : "never got there")); h.SendAway(FrontRoomsRelayAwayReason.Test); continue; }
            ShutUnheardV3b(world, pick, hand);
            var shut = !pick.open && !world.IsAjar(pick) && world.PassageBetween(pick.a, pick.b) == FrontRoomsMapWorld.Passage.ClosedDoor;
            r.Run(world, feet, forward, 90f, () => h.State == HunterState.Away);
            var inWithdraw = new List<int>();
            for (var i = 0; i < r.openedStates.Count; i++) if (r.openedStates[i] == HunterState.Withdraw) inWithdraw.Add(i);
            // The one OPEN is a shut door on its way back (the search's way, which may take another shut door than the one it came by).
            var atPick = inWithdraw.Count == 1 && Flat(r.openedAt[inWithdraw[0]] - pick.position) < .05f;
            var spent = inWithdraw.Count == 1 && r.openedLicences[inWithdraw[0]].Contains("withdraw OPEN spent");
            var exitReached = r.away.Count > 0 && r.away[r.away.Count - 1] == FrontRoomsRelayAwayReason.ExitReached;
            pass = shut && r.withdrawAt > 0f && inWithdraw.Count == 1 && spent && exitReached && r.awayAt > 0f && r.opened == 2 && !pick.broken && r.blows == 0;
            row = "seed " + seed + ": placed and stepped in at " + p + " (exit 1), sent on foot through the door " + pick.a + "-" + pick.b + " (OPEN 1) to " + g + ", the door then shut by the player unheard ("
                + shut + "); Withdraw at " + r.withdrawAt.ToString("0.00", inv) + " s OPENed " + inWithdraw.Count + " door on its way back ("
                + (atPick ? "the one shut" : inWithdraw.Count == 1 ? "another shut door, at " + world.CellOf(r.openedAt[inWithdraw[0]]) : "-") + "; the one OPEN spent " + spent + "), Away at " + r.awayAt.ToString("0.00", inv) + " s (" + (r.away.Count > 0 ? r.away[r.away.Count - 1].ToString() : "none") + "); OPEN " + r.opened + " in all, blows " + r.blows;
        }
        Check(pass, label + (row ?? "no run: " + string.Join("; ", notes)));
    }

    /// <summary>
    /// Relay v2 step V3b, on-map device calls (RELAY 5, 6) with the real hunter:
    /// - RedispatchKind: off the map queued; investigating (a search) with the device more than 20 walking cells away, W at
    ///   least 30 m and stage 0: Handoff; the device within 20 cells, or W under 30 m: Walk; the give-up beat: Walk; FADE:
    ///   queued.
    /// - Through the director (tier 1, budget 1): the handoff goes straight from the map to Arrive at a new placement (no
    ///   WentAway, no Withdrew, no Away), Redispatched(handoff) once, one re-dispatch spent, OnFirstSend kept; the next trip
    ///   (budget spent) waits in the queued slot and goes out after the Away with QueuedCallOut (the CALLED tick) at its placement.
    /// - A re-dispatch on foot: a device within 20 cells is a hunt to it (Redispatched(walk)), its way licensed.
    /// - The quiet-player sweep (Q2): 4 waypoints, the first at W 21-30 m along the heading, every one at W 21 m or more;
    ///   patrolled for the tier's sweep seconds, then the give-up beat and Withdraw; a noise ends it (a hunt). The tier
    ///   table's sweep seconds are 40/45/50/55/60.
    /// </summary>
    static void RelayV3bCalls(List<GameObject> roots)
    {
        const string label = "relay v3b calls: ";
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), -85f, "MAP INTERACTION TEST / relay v3b calls");
        world.BuildForCapture();
        var body = BodyV3b(roots, "MAP INTERACTION TEST / relay v3b calls player");
        var playerCell = world.CellOf(world.SpawnWorldPosition);
        var feet = world.CellCenter(playerCell);
        body.transform.position = feet;
        Physics.SyncTransforms();
        var eye = feet + Vector3.up * ModuleUnits.PlayerEye;
        var tuning = new FrontRoomsHunterTuning { sightRange = 0f };
        var field = new FrontRoomsMapPathField(45f);
        field.Build(world, playerCell);
        float W(GridCoord cell) => Mathf.Min(field.DistanceTo(cell), 3f * Vector3.Distance(world.CellCenter(cell), feet));
        var fromPlayer = Walk(world, playerCell, 40);

        // 1. RedispatchKind.
        {
            GridCoord? spot = null;
            foreach (var pair in fromPlayer.OrderBy(x => x.Value).ThenBy(x => x.Key.x).ThenBy(x => x.Key.y))
                if (pair.Value >= 14 && W(pair.Key) >= 36f && !world.InStartArea(pair.Key) && !world.InStairwell(pair.Key)) { spot = pair.Key; break; }
            if (!spot.HasValue) { Check(false, label + "no cell at W 36 m or more within 40 cells of the spawn"); }
            else
            {
                var relayAt = world.CellCenter(spot.Value);
                var forward = Flat(feet - relayAt) > .1f ? (feet - relayAt).normalized : Vector3.forward;
                forward.y = 0f;
                var fromRelay = Walk(world, spot.Value, 40);
                var farList = fromRelay.Where(x => x.Value >= 24).OrderBy(x => x.Value).ThenBy(x => x.Key.x).ThenBy(x => x.Key.y).Select(x => x.Key).ToList();
                var nearList = fromRelay.Where(x => x.Value >= 6 && x.Value <= 12).OrderBy(x => x.Value).ThenBy(x => x.Key.x).ThenBy(x => x.Key.y).Select(x => x.Key).ToList();
                if (farList.Count == 0 || nearList.Count == 0) Check(false, label + "no device cells 24+ and 6-12 walking cells from " + spot.Value);
                else
                {
                    var farDevice = world.CellCenter(farList[0]);
                    var nearDevice = world.CellCenter(nearList[0]);
                    var h = new FrontRoomsMapHunter(world, tuning, body, null, 81);
                    var gaveUp = false;
                    h.GaveUp += p => gaveUp = true;
                    var dormant = h.RedispatchKind(farDevice, feet, eye);
                    h.Arm();
                    var awayKind = h.RedispatchKind(farDevice, feet, eye);
                    // Investigating: a hunt on foot to a cell 3-5 cells off, as far from the player (a placed Relay with nothing to search gives up at once).
                    var huntList = fromRelay.Where(x => x.Value >= 3 && x.Value <= 5 && W(x.Key) >= 36f).OrderBy(x => x.Value).ThenBy(x => x.Key.x).ThenBy(x => x.Key.y).Select(x => x.Key).ToList();
                    h.DebugPlace(relayAt);
                    if (huntList.Count > 0) h.Redirect(new FrontRoomsRelayCall(world.CellCenter(huntList[0]), 0, FrontRoomsRelayCallCause.Device, true, false, 20f));
                    h.Tick(Dt, feet, eye, forward);
                    var searching = h.State == HunterState.Hunt;
                    var w0 = h.WarningDistance;
                    var stage0 = h.WarnStage;
                    var seen0 = h.PlayerSeesRelay;
                    var far = h.RedispatchKind(farDevice, feet, eye);
                    var near = h.RedispatchKind(nearDevice, feet, eye);
                    var beatKind = FrontRoomsRelayRedispatchKind.Queue;
                    var beat = false;
                    for (var t = 0f; t < 40f && !gaveUp; t += Dt) h.Tick(Dt, feet, eye, forward);
                    if (gaveUp && h.State == HunterState.Listen)
                    {
                        beat = true;
                        beatKind = h.RedispatchKind(farDevice, feet, eye);
                    }
                    h.Fade(false);
                    var fadeKind = h.RedispatchKind(farDevice, feet, eye);
                    // W under 30 m: a Relay 3-5 cells from the player.
                    var closeList = fromPlayer.Where(x => x.Value >= 3 && x.Value <= 5 && W(x.Key) < 30f).OrderBy(x => x.Value).ThenBy(x => x.Key.x).ThenBy(x => x.Key.y).Select(x => x.Key).ToList();
                    var closeKind = FrontRoomsRelayRedispatchKind.Queue;
                    var closeW = -1f;
                    if (closeList.Count > 0)
                    {
                        var h2 = new FrontRoomsMapHunter(world, tuning, body, null, 82);
                        h2.Arm();
                        h2.DebugPlace(world.CellCenter(closeList[0]));
                        h2.Tick(Dt, feet, eye, forward);
                        closeW = h2.WarningDistance;
                        var farFromClose = Walk(world, closeList[0], 40).Where(x => x.Value >= 24).OrderBy(x => x.Value).ThenBy(x => x.Key.x).ThenBy(x => x.Key.y).Select(x => x.Key).ToList();
                        closeKind = farFromClose.Count > 0 ? h2.RedispatchKind(world.CellCenter(farFromClose[0]), feet, eye) : FrontRoomsRelayRedispatchKind.Queue;
                    }
                    Check(dormant == FrontRoomsRelayRedispatchKind.Queue && awayKind == FrontRoomsRelayRedispatchKind.Queue && searching && w0 >= FrontRoomsMapHunter.HandoffMinW && stage0 == 0 && !seen0
                          && far == FrontRoomsRelayRedispatchKind.Handoff && near == FrontRoomsRelayRedispatchKind.Walk && beat && beatKind == FrontRoomsRelayRedispatchKind.Walk
                          && fadeKind == FrontRoomsRelayRedispatchKind.Queue && closeW >= 0f && closeW < FrontRoomsMapHunter.HandoffMinW && closeKind == FrontRoomsRelayRedispatchKind.Walk,
                        label + "RedispatchKind: Dormant " + dormant + ", Away " + awayKind + "; investigating (a hunt " + searching + ") at W " + w0.ToString("0.0", inv) + " m, stage " + stage0 + ", unseen " + !seen0
                        + ": a device " + fromRelay[farList[0]] + " cells off (over 20) " + far + ", one " + fromRelay[nearList[0]] + " cells off " + near + "; the give-up beat (" + beat + ") " + beatKind + "; under FADE " + fadeKind
                        + "; at W " + closeW.ToString("0.0", inv) + " m (under 30) " + closeKind);
                }
            }
        }

        // 2-3. Through the director: the handoff, the queued trip and its CALLED tick; then a walk re-dispatch.
        for (var walk = 0; walk < 2; walk++)
        {
            var h = new FrontRoomsMapHunter(world, tuning, body, null, 83 + walk);
            var d = new FrontRoomsRelayDirector(h);
            var forward = Vector3.forward;
            var redispatched = new List<bool>();
            var redispatchCalls = new List<FrontRoomsRelayCall>();
            var calledCauses = new List<FrontRoomsRelayCallCause>();
            var queuedOut = new List<Vector3>();
            var queuedOutState = HunterState.Dormant;
            int wentAway = 0, withdrew = 0, directorAway = 0, sounds = 0;
            d.Redispatched += (call, handoff) => { redispatched.Add(handoff); redispatchCalls.Add(call); };
            d.Called += (cause, explicitCall, at) => calledCauses.Add(cause);
            d.QueuedCallOut += p => { queuedOut.Add(p); queuedOutState = h.State; };
            d.DeviceTriggered += call => sounds++;
            d.Away += reason => directorAway++;
            h.WentAway += reason => wentAway++;
            h.Withdrew += p => withdrew++;
            d.Arm(false, false);
            var summonCell = fromPlayer.Where(x => x.Value >= 5 && x.Value <= 10).OrderBy(x => x.Value).ThenBy(x => x.Key.x).ThenBy(x => x.Key.y).Select(x => x.Key).FirstOrDefault();
            d.Summon(world.CellCenter(summonCell));
            var t = 0f;
            void Step()
            {
                t += Dt;
                d.Tick(Dt, 1, false, feet, eye, forward);
                h.Tick(Dt, feet, eye, forward);
                world.TickDoorsForTools(Dt);
                Physics.SyncTransforms();
            }
            while (t < 30f && !(h.State == HunterState.Listen && h.OnMap)) Step();
            if (!(h.State == HunterState.Listen && h.OnMap)) { Check(false, label + (walk == 0 ? "handoff" : "walk") + ": the summoned Relay never stepped in (" + h.State + ", " + h.LastArriveReport + ")"); continue; }
            var stepCell = world.CellOf(h.Position);
            var stepW = h.WarningDistance;
            var fromRelay = Walk(world, stepCell, 44);
            FrontRoomsRelayRedispatchKind kind;
            Vector3 device;
            if (walk == 0)
            {
                var list = fromRelay.Where(x => x.Value >= 24 && fromPlayer.ContainsKey(x.Key) && fromPlayer[x.Key] >= 14 && W(x.Key) >= 36f && !world.InStartArea(x.Key) && !world.InStairwell(x.Key))
                    .OrderBy(x => x.Value).ThenBy(x => x.Key.x).ThenBy(x => x.Key.y).Select(x => x.Key).ToList();
                if (list.Count == 0) { Check(false, label + "handoff: no device cell 24+ cells from " + stepCell + " at W 36 m or more"); continue; }
                device = world.CellCenter(list[0]);
            }
            else
            {
                var list = fromRelay.Where(x => x.Value >= 6 && x.Value <= 12).OrderBy(x => x.Value).ThenBy(x => x.Key.x).ThenBy(x => x.Key.y).Select(x => x.Key).ToList();
                if (list.Count == 0) { Check(false, label + "walk: no device cell 6-12 cells from " + stepCell); continue; }
                device = world.CellCenter(list[0]);
            }
            kind = h.RedispatchKind(device, feet, eye);
            var firstBefore = h.OnFirstSend;
            var budgetBefore = d.Policy.Redispatches;
            var canBefore = d.Policy.CanRedispatch;
            calledCauses.Clear();
            sounds = 0;
            var took = d.DeviceTrigger(device, "glass");
            var budgetSpent = d.Policy.Redispatches == budgetBefore + 1 && !d.Policy.CanRedispatch;
            if (walk == 0)
            {
                var handedOff = took && redispatched.Count == 1 && redispatched[0] && d.Handoffs == 1 && d.Redispatches == 1 && h.Handoffs == 1 && h.State == HunterState.Arrive
                    && h.CurrentCall != null && h.CurrentCall.Cause == FrontRoomsRelayCallCause.Handoff && calledCauses.Count == 1 && calledCauses[0] == FrontRoomsRelayCallCause.Handoff && sounds == 1;
                var noAwayHooks = wentAway == 0 && withdrew == 0 && directorAway == 0 && d.Phase == FrontRoomsRelayDirectorPhase.Encounter;
                var report = h.LastArriveReport;
                var arrivedAgain = false;
                while (t < 60f && !arrivedAgain) { Step(); arrivedAgain = h.OnMap; }
                var huntToDevice = h.ArriveHuntPoint.HasValue && Flat(h.ArriveHuntPoint.Value - device) < MapGrid.CellSize * 3f;
                Check(canBefore && kind == FrontRoomsRelayRedispatchKind.Handoff && handedOff && noAwayHooks && budgetSpent && h.OnFirstSend == firstBefore && arrivedAgain && huntToDevice && wentAway == 0,
                    label + "the handoff (stepped in at " + stepCell + ", W " + stepW.ToString("0.0", inv) + " m, the device " + fromRelay[world.CellOf(device)] + " cells off, kind " + kind + "): Redispatched(handoff) "
                    + redispatched.Count + "x, straight to Arrive (" + report + "), Called(" + string.Join(" ", calledCauses) + "), the device sounded " + sounds + "x; no WentAway/Withdrew/Away ("
                    + wentAway + "/" + withdrew + "/" + directorAway + "), phase " + d.Phase + "; budget " + d.Policy.Redispatches + " spent (CanRedispatch " + d.Policy.CanRedispatch + "); OnFirstSend kept ("
                    + h.OnFirstSend + "); stepped in again " + arrivedAgain + " to hunt the device's point " + huntToDevice);

                // The next trip: the budget is spent, so it waits in the queued slot; after the Away its call goes out with the CALLED tick.
                var second = device + new Vector3(MapGrid.CellSize, 0f, 0f);
                var queuedTook = d.DeviceTrigger(second, "glass");
                var queued = !queuedTook && d.Policy.HasQueuedDevice && d.DevicesQueuedOnMap == 1 && redispatched.Count == 1;
                h.SendAway(FrontRoomsRelayAwayReason.Test);
                var outBefore = queuedOut.Count;
                while (t < 120f && queuedOut.Count == outBefore) Step();
                Check(queued && queuedOut.Count == 1 && (queuedOut[0] - second).sqrMagnitude < 1e-6f && queuedOutState == HunterState.Arrive && !d.Policy.HasQueuedDevice,
                    label + "a trip with the budget spent waits in the queued slot (" + queued + ", on the map " + d.DevicesQueuedOnMap + "x); after the Away it goes out: QueuedCallOut " + queuedOut.Count
                    + "x at its own point, raised at the placement (the Relay " + queuedOutState + ")");
            }
            else
            {
                var walked = took && redispatched.Count == 1 && !redispatched[0] && redispatchCalls[0].Cause == FrontRoomsRelayCallCause.Device && d.Redispatches == 1 && d.Handoffs == 0 && h.Handoffs == 0
                    && h.State == HunterState.Hunt && h.ArriveHuntPoint.HasValue && Flat(h.ArriveHuntPoint.Value - device) < MapGrid.CellSize * 2f && wentAway == 0 && calledCauses.Count == 1
                    && calledCauses[0] == FrontRoomsRelayCallCause.Device;
                Check(kind == FrontRoomsRelayRedispatchKind.Walk && walked && budgetSpent,
                    label + "a re-dispatch on foot (the device " + fromRelay[world.CellOf(device)] + " cells off, kind " + kind + "): Redispatched(walk) " + redispatched.Count + "x, the Relay " + h.State
                    + " to the device's point, Called(" + string.Join(" ", calledCauses) + "), " + h.DoorLicences + "; budget " + d.Policy.Redispatches + " spent, no WentAway (" + wentAway + ")");
            }
        }

        // 4. The sweep (Q2), from a few player cells round the spawn. Its placement is Arrive's (hard rules), so from where it
        // steps in no leg may keep W 21 m (every way to a waypoint passing the player): then it ends at once in the give-up
        // beat. Such runs are counted, not failed; at least one must walk its waypoints for the tier's seconds.
        {
            var rules = new FrontRoomsTierRules { v2 = null };
            rules.Normalize();
            var tierSeconds = new List<float>();
            for (var i = 1; i <= 5; i++) tierSeconds.Add(rules.V2At(i).sweepSeconds);
            var tiersOk = tierSeconds.Count == 5 && tierSeconds[0] == 40f && tierSeconds[1] == 45f && tierSeconds[2] == 50f && tierSeconds[3] == 55f && tierSeconds[4] == 60f;
            var spots = new List<GridCoord> { playerCell };
            foreach (var want in new[] { 4, 8, 12, 16, 20 })
            {
                var at = fromPlayer.Where(x => x.Value == want && !world.InStartArea(x.Key) && !world.InStairwell(x.Key)).OrderBy(x => x.Key.x).ThenBy(x => x.Key.y).Select(x => x.Key).ToList();
                if (at.Count > 0) spots.Add(at[0]);
            }
            var placedRuns = 0;
            var fourWays = 0;
            var fullWalks = 0;
            var collapsed = 0;
            var structureOk = true;
            var endsOk = true;
            var walkMinW = float.PositiveInfinity;
            var rows = new List<string>();
            var walkedSpots = new List<int>();
            var headings = new List<Vector3>();
            for (var k = 0; k < spots.Count; k++)
            {
                var spot = spots[k];
                var sFeet = world.CellCenter(spot);
                var sEye = sFeet + Vector3.up * ModuleUnits.PlayerEye;
                body.transform.position = sFeet;
                Physics.SyncTransforms();
                var sField = new FrontRoomsMapPathField(45f);
                sField.Build(world, spot);
                var around = Walk(world, spot, 10);
                var heading = Vector3.forward;
                var sum = Vector3.zero;
                foreach (var pair in around)
                {
                    if (pair.Value < 8) continue;
                    var dir = world.CellCenter(pair.Key) - sFeet;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > .01f) sum += dir.normalized;
                }
                if (sum.sqrMagnitude > 1e-4f) heading = sum.normalized;
                headings.Add(heading);
                var h = new FrontRoomsMapHunter(world, tuning, body, null, 87 + k);
                h.TierV2 = new FrontRoomsTierV2 { sweepSeconds = 12f, patrolSeconds = 15f };
                var r = new DoorRunV3b(h);
                h.Arm();
                if (!h.Sweep(new FrontRoomsRelayCall(sFeet, 0, FrontRoomsRelayCallCause.Sweep, false, false, 20f), false, sFeet, sEye, heading, heading))
                {
                    rows.Add(spot + ": not placed (" + h.LastArriveReport + ")");
                    continue;
                }
                placedRuns++;
                var ways = new List<GridCoord>(h.SweepWaypointsV2);
                var w1 = Mathf.Min(sField.DistanceTo(ways[0]), 3f * Vector3.Distance(world.CellCenter(ways[0]), sFeet));
                var dir1 = world.CellCenter(ways[0]) - sFeet;
                dir1.y = 0f;
                var along = dir1.sqrMagnitude > .01f ? Vector3.Dot(dir1.normalized, heading) : -1f;
                var leastW = float.PositiveInfinity;
                foreach (var wp in ways) leastW = Mathf.Min(leastW, Mathf.Min(sField.DistanceTo(wp), 3f * Vector3.Distance(world.CellCenter(wp), sFeet)));
                var placed = h.State == HunterState.Arrive && h.Sweeping && Flat(h.SweepTarget - world.CellCenter(ways[0])) < .01f && h.LastArriveReport.StartsWith("sweep of");
                // Up to 4: after the first, each next waypoint must lie 3-12 open cells on with every cell of that way at W 21 m or more.
                structureOk &= placed && ways.Count >= 1 && ways.Count <= FrontRoomsMapHunter.SweepWaypoints && w1 >= FrontRoomsMapHunter.SweepMinW - .01f
                    && w1 <= FrontRoomsMapHunter.SweepNearMaxW + .01f && along >= 0f && leastW >= FrontRoomsMapHunter.SweepMinW - .01f;
                if (ways.Count == FrontRoomsMapHunter.SweepWaypoints) fourWays++;
                var runMinW = float.PositiveInfinity;
                var reached = new HashSet<GridCoord>();
                r.Run(world, sFeet, heading, 60f, () =>
                {
                    if (h.State == HunterState.Wander)
                    {
                        runMinW = Mathf.Min(runMinW, h.WarningDistance);
                        var cell = world.CellOf(h.Position);
                        if (ways.Contains(cell)) reached.Add(cell);
                    }
                    return h.State == HunterState.Away;
                });
                var walked = r.wanderAt > 0f;
                var walkedFor = walked ? r.gaveUpAt - r.wanderAt : 0f;
                var listen = (walked ? r.wanderAt : r.gaveUpAt) - r.stepAt;
                var ends = r.stepAt > 0f && listen >= FrontRoomsMapHunter.StepInListenSeconds - Dt && listen <= FrontRoomsMapHunter.StepInListenSeconds + 2f * Dt && r.gaveUp == 1
                    && r.gaveUpState == HunterState.Listen && r.withdrawAt - r.gaveUpAt >= FrontRoomsMapHunter.GiveUpSeconds - Dt
                    && r.withdrawAt - r.gaveUpAt <= FrontRoomsMapHunter.GiveUpSeconds + 2f * Dt && r.awayAt > 0f && walkedFor <= 12f + 3f * Dt;
                endsOk &= ends;
                if (walked)
                {
                    walkMinW = Mathf.Min(walkMinW, runMinW);
                    walkedSpots.Add(k);
                    if (walkedFor >= 12f - 2f * Dt) fullWalks++;
                }
                else collapsed++;
                rows.Add(spot + ": " + ways.Count + " waypoints (placed " + placed + "), W1 " + w1.ToString("0.0", inv) + " m, cos " + along.ToString("0.00", inv) + ", least " + leastW.ToString("0.0", inv) + "; "
                    + (walked ? "walked " + walkedFor.ToString("0.00", inv) + " s (" + reached.Count + " waypoints reached, least W " + runMinW.ToString("0.0", inv) + ")" : "no leg kept W 21 m: ended at the step-in")
                    + ", then GaveUp, Withdraw " + (r.withdrawAt - r.gaveUpAt).ToString("0.00", inv) + " s later, Away " + (r.awayAt > 0f));
            }
            Check(tiersOk && placedRuns >= 3 && structureOk && endsOk && fullWalks >= 1 && walkMinW >= FrontRoomsMapHunter.SweepMinW - MapGrid.CellSize,
                label + "the sweep (tier table " + string.Join("/", tierSeconds.Select(x => x.ToString("0", inv))) + " s; this tier 12 s): " + placedRuns + " of " + spots.Count + " placed (" + fourWays + " with all "
                + FrontRoomsMapHunter.SweepWaypoints + " waypoints), the first at W 21-30 m along the heading, all at W 21 m or more (" + structureOk + "); " + fullWalks + " walked the full 12 s, "
                + collapsed + " ended at the step-in (no leg keeping W 21 m from where Arrive placed it); every one: the step-in's 1.5 s, GaveUp in Listen, Withdraw 2 s later (no patrol), Away ("
                + endsOk + "); least W while walking " + walkMinW.ToString("0.0", inv) + " m - " + string.Join("; ", rows));

            // A noise ends a sweep under way: a hunt to the sound (an ordinary encounter).
            string noiseRow = null;
            var noiseOk = false;
            foreach (var k in walkedSpots)
            {
                var spot = spots[k];
                var sFeet = world.CellCenter(spot);
                var sEye = sFeet + Vector3.up * ModuleUnits.PlayerEye;
                body.transform.position = sFeet;
                Physics.SyncTransforms();
                var h = new FrontRoomsMapHunter(world, tuning, body, null, 87 + k);
                h.TierV2 = new FrontRoomsTierV2 { sweepSeconds = 12f };
                var r = new DoorRunV3b(h);
                h.Arm();
                if (!h.Sweep(new FrontRoomsRelayCall(sFeet, 0, FrontRoomsRelayCallCause.Sweep, false, false, 20f), false, sFeet, sEye, headings[k], headings[k])) continue;
                r.Run(world, sFeet, headings[k], 20f, () => h.State == HunterState.Wander);
                if (h.State != HunterState.Wander || !h.Sweeping) continue;
                var near2 = Walk(world, spot, 2).Where(x => x.Value == 2).OrderBy(x => x.Key.x).ThenBy(x => x.Key.y).Select(x => x.Key).ToList();
                var source = near2.Count > 0 ? world.CellCenter(near2[0]) : sFeet;
                h.Noise(source, 1000f);
                noiseOk = h.State == HunterState.Hunt && !h.Sweeping && h.ListenPoint.HasValue && (h.ListenPoint.Value - source).sqrMagnitude < 1e-6f;
                noiseRow = "from " + spot + ", walking the sweep: a noise 2 cells from the player -> " + h.State + " to the sound, Sweeping " + h.Sweeping;
                break;
            }
            Check(noiseOk, label + "a noise ends the sweep (an ordinary encounter): " + (noiseRow ?? "no sweep reached its walk again"));
        }

        // 5. V3c (Fix 3, systems design 2026-10-30, "(b) + guard"): waypoint 1 is picked where the Relay can step in. On a
        //    fresh copy of this map (its doors as built), from the same 6 player cells along the same headings, at tier 5's
        //    60 s: from the cells where V3b's sweep ended at the step-in (lean_v3b_on4: (4, 4), (-2, 6), (-7, -1),
        //    (-11, -1)) it walks, reaches waypoint 1, and every cell it walks has W at least 21 m (WarningDistanceAt of the
        //    cell, the planner's own measure; every leg and replan is planned on such cells). Every other cell that walks
        //    keeps that too. The guard (LastSweepRuleB), the waypoint-1 tries and the placement's time (up to 12 PlaceV2
        //    tries) are reported.
        {
            var fresh = World(roots, FrontRoomsLevelProfiles.Resolve(), -87f, "MAP INTERACTION TEST / relay v3c sweep");
            fresh.BuildForCapture();
            var fBody = BodyV3b(roots, "MAP INTERACTION TEST / relay v3c sweep player");
            var fCell = fresh.CellOf(fresh.SpawnWorldPosition);
            var fFrom = Walk(fresh, fCell, 40);
            var fSpots = new List<GridCoord> { fCell };
            foreach (var want in new[] { 4, 8, 12, 16, 20 })
            {
                var at = fFrom.Where(x => x.Value == want && !fresh.InStartArea(x.Key) && !fresh.InStairwell(x.Key)).OrderBy(x => x.Key.x).ThenBy(x => x.Key.y).Select(x => x.Key).ToList();
                if (at.Count > 0) fSpots.Add(at[0]);
            }
            var v3bEnded = new List<GridCoord> { new GridCoord(4, 4), new GridCoord(-2, 6), new GridCoord(-7, -1), new GridCoord(-11, -1) };
            var rows = new List<string>();
            int endedFound = 0, endedWalked = 0, endedReached = 0, endedKept = 0, allWalked = 0, guards = 0;
            var endedOk = true;
            var placeMs = 0.0;
            var cellLeast = float.PositiveInfinity;
            for (var k = 0; k < fSpots.Count; k++)
            {
                var spot = fSpots[k];
                var sFeet = fresh.CellCenter(spot);
                var sEye = sFeet + Vector3.up * ModuleUnits.PlayerEye;
                fBody.transform.position = sFeet;
                Physics.SyncTransforms();
                // The heading as row 4 takes it: the mean direction to the cells 8-10 walking cells off.
                var heading = Vector3.forward;
                var sum = Vector3.zero;
                foreach (var pair in Walk(fresh, spot, 10))
                {
                    if (pair.Value < 8) continue;
                    var dir = fresh.CellCenter(pair.Key) - sFeet;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > .01f) sum += dir.normalized;
                }
                if (sum.sqrMagnitude > 1e-4f) heading = sum.normalized;
                var h = new FrontRoomsMapHunter(fresh, tuning, fBody, null, 87 + k);
                h.TierV2 = new FrontRoomsTierV2 { sweepSeconds = 60f, patrolSeconds = 15f };
                var r = new DoorRunV3b(h);
                h.Arm();
                var wasEnded = v3bEnded.Contains(spot);
                if (wasEnded) endedFound++;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var placed = h.Sweep(new FrontRoomsRelayCall(sFeet, 0, FrontRoomsRelayCallCause.Sweep, false, false, 20f), false, sFeet, sEye, heading, heading);
                watch.Stop();
                placeMs = Math.Max(placeMs, watch.Elapsed.TotalMilliseconds);
                if (!placed)
                {
                    rows.Add(spot + ": not placed (" + h.LastArriveReport + ")");
                    if (wasEnded) endedOk = false;
                    continue;
                }
                var ruleB = h.LastSweepRuleB;
                var tries = h.LastSweepFirstTries;
                if (ruleB) guards++;
                List<GridCoord> ways = null;
                var cellMin = float.PositiveInfinity;
                var ownMin = float.PositiveInfinity;
                var firstAt = -1f;
                var reached = new HashSet<GridCoord>();
                r.Run(fresh, sFeet, heading, 90f, () =>
                {
                    if (h.State == HunterState.Wander)
                    {
                        if (ways == null) ways = new List<GridCoord>(h.SweepWaypointsV2);
                        var cell = fresh.CellOf(h.Position);
                        cellMin = Mathf.Min(cellMin, h.WarningDistanceAt(cell, sFeet));
                        ownMin = Mathf.Min(ownMin, h.WarningDistance);
                        if (ways.Contains(cell)) reached.Add(cell);
                        if (firstAt < 0f && cell == ways[0]) firstAt = r.t - r.wanderAt;
                    }
                    return h.State == HunterState.Away;
                });
                var walked = r.wanderAt > 0f && ways != null;
                var kept = walked && cellMin >= FrontRoomsMapHunter.SweepMinW - .01f;
                if (walked)
                {
                    allWalked++;
                    cellLeast = Mathf.Min(cellLeast, cellMin);
                }
                if (wasEnded)
                {
                    if (walked) endedWalked++;
                    if (firstAt >= 0f) endedReached++;
                    if (kept) endedKept++;
                    endedOk &= walked && firstAt >= 0f && kept;
                }
                var walkedFor = r.gaveUpAt > 0f ? r.gaveUpAt - r.wanderAt : r.t - r.wanderAt;
                rows.Add(spot + (wasEnded ? " (V3b: ended at the step-in)" : "") + ": " + (ruleB ? "the guard (rule b)" : "waypoint 1 where it steps in") + " after " + tries + (tries == 1 ? " try" : " tries") + ", "
                    + (walked ? "walked " + walkedFor.ToString("0.0", inv) + " s, waypoint 1 " + (firstAt >= 0f ? "reached " + firstAt.ToString("0.0", inv) + " s in" : "NOT reached") + " (" + reached.Count + " of "
                        + ways.Count + " waypoints), least W of the cells walked " + cellMin.ToString("0.0", inv) + " m (the Relay's own W " + ownMin.ToString("0.0", inv) + " m)" : "no leg: ended at the step-in")
                    + "; GaveUp " + r.gaveUp + ", Away " + (r.awayAt > 0f));
            }
            Check(endedFound == v3bEnded.Count && endedOk && allWalked >= endedFound && cellLeast >= FrontRoomsMapHunter.SweepMinW - .01f,
                label + "V3c, waypoint 1 where it can step in (a fresh copy of the map, tier 5's 60 s): from the " + endedFound + " player cells where V3b's sweep ended at the step-in, " + endedWalked + " walk, "
                + endedReached + " reach waypoint 1, " + endedKept + " keep every cell walked at W >= 21 m; of all " + fSpots.Count + " cells " + allWalked + " walk (least W of the cells walked "
                + cellLeast.ToString("0.0", inv) + " m), " + guards + " by the guard (rule b); the placement took at most " + placeMs.ToString("0.0", inv) + " ms - " + string.Join("; ", rows));
        }

        // 6. V3c (Fix 2, systems design 2026-10-30, reading 2 changed): a device tripped in a chase is heard and queued, with
        //    the real hunter and director. There is no re-dispatch in a chase (RELAY 5), so RELAY 4's slot: the device sounds
        //    once (DeviceTriggered), no Redispatched, no budget spent, Attention unchanged, DevicesHeardOnly and
        //    DevicesQueuedOnMap 1, the slot holds it; after the Away its call goes out: QueuedCallOut once, at the trip's
        //    point, raised at the placement after Called(Device).
        {
            var sighted = new FrontRoomsHunterTuning { sightRange = 12f, lostSightSeconds = 6f };
            var h = new FrontRoomsMapHunter(world, sighted, body, null, 93);
            var d = new FrontRoomsRelayDirector(h);
            int sounds = 0, redispatched = 0;
            var events = new List<string>();
            var queuedOut = new List<Vector3>();
            var outState = HunterState.Dormant;
            d.DeviceTriggered += call => sounds++;
            d.Redispatched += (call, handoff) => redispatched++;
            d.Called += (cause, explicitCall, at) => events.Add("Called(" + cause + ")");
            d.QueuedCallOut += p =>
            {
                queuedOut.Add(p);
                events.Add("QueuedCallOut");
                outState = h.State;
            };
            d.Arm(false, false);
            var pFeet = feet;
            var forward = Vector3.forward;
            var t = 0f;
            void Step()
            {
                t += Dt;
                var pEye = pFeet + Vector3.up * ModuleUnits.PlayerEye;
                d.Tick(Dt, 1, false, pFeet, pEye, forward);
                h.Tick(Dt, pFeet, pEye, forward);
                world.TickDoorsForTools(Dt);
                Physics.SyncTransforms();
            }
            void StandAt(Vector3 at)
            {
                pFeet = at;
                body.transform.position = at;
                Physics.SyncTransforms();
            }
            var summonCell = fromPlayer.Where(x => x.Value >= 5 && x.Value <= 10).OrderBy(x => x.Value).ThenBy(x => x.Key.x).ThenBy(x => x.Key.y).Select(x => x.Key).FirstOrDefault();
            d.Summon(world.CellCenter(summonCell));
            while (t < 30f && !h.OnMap) Step();
            var stepped = h.OnMap;
            // The player walks up to it, into its sight (12 m): a chase.
            var chased = false;
            if (stepped)
                foreach (var cell in Walk(world, world.CellOf(h.Position), 3).Where(x => x.Value >= 2).OrderBy(x => x.Value).ThenBy(x => x.Key.x).ThenBy(x => x.Key.y).Select(x => x.Key).ToList())
                {
                    StandAt(world.CellCenter(cell));
                    ChaseNowV4b(h, pFeet, Step);
                    for (var i = 0; i < 30 && h.State != HunterState.Chase; i++) Step();
                    if (h.State != HunterState.Chase) continue;
                    chased = true;
                    break;
                }
            var heardQueued = false;
            var trip = Vector3.zero;
            var kind = FrontRoomsRelayRedispatchKind.Queue;
            var tripRow = stepped ? "no chase began" : "the summoned Relay never stepped in (" + h.State + ", " + h.LastArriveReport + ")";
            if (chased)
            {
                var playerCellNow = world.CellOf(pFeet);
                var tripCell = Walk(world, playerCellNow, 30).Where(x => x.Value >= 20 && !world.InStartArea(x.Key) && !world.InStairwell(x.Key))
                    .OrderBy(x => x.Value).ThenBy(x => x.Key.x).ThenBy(x => x.Key.y).Select(x => x.Key).FirstOrDefault();
                trip = world.CellCenter(tripCell);
                kind = h.RedispatchKind(trip, pFeet, pFeet + Vector3.up * ModuleUnits.PlayerEye);
                var attention = d.Policy.Attention;
                var budget = d.Policy.Redispatches;
                var canBefore = d.Policy.CanRedispatch;
                events.Clear();
                sounds = 0;
                var took = d.DeviceTrigger(trip, "glass");
                heardQueued = !took && kind == FrontRoomsRelayRedispatchKind.NoiseOnly && h.State == HunterState.Chase && sounds == 1 && redispatched == 0 && d.Redispatches == 0
                    && d.Policy.Redispatches == budget && d.DevicesHeardOnly == 1 && d.DevicesQueuedOnMap == 1 && d.Policy.HasQueuedDevice && d.Policy.QueuedDeviceId == "glass"
                    && Mathf.Abs(d.Policy.Attention - attention) < 1e-4f && events.Count == 0 && queuedOut.Count == 0;
                tripRow = "chasing from " + world.CellOf(h.Position) + " (the player at " + playerCellNow + "), a glass trip at " + tripCell + ": kind " + kind + ", took " + took + ", sounded " + sounds
                    + "x, Redispatched " + redispatched + "x, budget " + d.Policy.Redispatches + " spent (CanRedispatch " + canBefore + " -> " + d.Policy.CanRedispatch + "), Attention "
                    + attention.ToString("0.0", inv) + " -> " + d.Policy.Attention.ToString("0.0", inv) + "; heard only " + d.DevicesHeardOnly + ", queued on the map " + d.DevicesQueuedOnMap
                    + ", the slot " + (d.Policy.HasQueuedDevice ? d.Policy.QueuedDeviceId : "empty") + ", the Relay still " + h.State;
                // The chase ends in an Away (as the game's LoseTrack or FADE would); the player back at the spawn.
                h.SendAway(FrontRoomsRelayAwayReason.Test);
                StandAt(feet);
                while (t < 200f && queuedOut.Count == 0) Step();
            }
            StandAt(feet);
            var calledAt = events.IndexOf("Called(Device)");
            var outAt = events.IndexOf("QueuedCallOut");
            Check(chased && heardQueued && queuedOut.Count == 1 && (queuedOut[0] - trip).sqrMagnitude < 1e-6f && outState == HunterState.Arrive && calledAt >= 0 && calledAt < outAt
                    && !d.Policy.HasQueuedDevice && sounds == 1,
                label + "V3c, a device tripped in a chase is heard and queued (the real hunter and director): " + tripRow + "; after the Away: " + string.Join(" ", events.ToArray()) + ", QueuedCallOut "
                + queuedOut.Count + "x" + (queuedOut.Count > 0 ? " at the trip's point " + ((queuedOut[0] - trip).sqrMagnitude < 1e-6f) + ", raised at the placement (the Relay " + outState + ")" : "")
                + ", the slot " + (d.Policy.HasQueuedDevice ? "still full" : "empty") + ", the device sounded " + sounds + "x in all");
        }
    }
#endif

#if FRONTROOMS_RELAY_V2
    // ---------- Relay v2 (V4a): hearing, the sense link (RELAY 5, 6, 7, 10, 11) ----------

    /// <summary>Metres from a noise's cell to a cell on a one-off field (as the hunter's heard test reads them), and the opening cost of that way.</summary>
    static float HeardPathV4a(FrontRoomsMapWorld world, GridCoord source, GridCoord at, out float opening)
    {
        var field = new FrontRoomsMapPathField(90f);
        field.Build(world, source);
        opening = field.OpeningCostTo(at);
        return field.DistanceTo(at);
    }

    static int CellsApartV4a(GridCoord a, GridCoord b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

    static bool NearV4a(float a, float b) => Mathf.Abs(a - b) < .01f;

    static bool SamePointV4a(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < 1e-6f;

    /// <summary>Ticks the hunter and the doors for a while, the player standing at <paramref name="feet"/>.</summary>
    static void TickV4a(FrontRoomsMapWorld world, FrontRoomsMapHunter h, Vector3 feet, float seconds)
    {
        var eye = feet + Vector3.up * ModuleUnits.PlayerEye;
        for (var s = 0f; s < seconds - 1e-4f; s += Dt)
        {
            h.Tick(Dt, feet, eye, Vector3.forward);
            world.TickDoorsForTools(Dt);
            Physics.SyncTransforms();
        }
    }

    /// <summary>A hunter's private field, for the rows that have no read-out for it (the search's spots, the lose meter).</summary>
    static object FieldV4a(object target, string name) =>
        target.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(target);

    static int SpotsV4a(FrontRoomsMapHunter h) => ((System.Collections.ICollection)FieldV4a(h, "sweep")).Count;

    static void ShutDoorV4a(FrontRoomsMapWorld world, FrontRoomsMapWorld.Door door)
    {
        world.ResetDoorForTools(door);
        for (var k = 0; k < 10; k++) world.TickDoorsForTools(Dt);
        Physics.SyncTransforms();
    }

    static void OpenDoorV4a(FrontRoomsMapWorld world, FrontRoomsMapWorld.Door door, GridCoord a, GridCoord b)
    {
        world.ResetDoorForTools(door);
        world.TryOpenDoor(a, b);
        for (var k = 0; k < 90; k++) world.TickDoorsForTools(Dt);
        Physics.SyncTransforms();
    }

    /// <summary>
    /// Relay v2 step V4a, a blind Relay (a chase row excepted) on a built map, the shut door c, a | b, e on a line:
    /// - k: EstimateErrorCells' table (half rounds up, +1 through a shut opening past half range, at most 3); a real noise
    ///   reads the path metres, the range, the margin and k, its estimate within k cells of the source; out of range nothing;
    ///   heard the same on F when the source is the player's cell; through a shut door past half range +1, inside it or with
    ///   the door open none; k 0 at radius 1000: the exact point.
    /// - Side-correct: 40 draws behind a shut door keep the source's opening cost from the Relay; a noise on the door's line
    ///   lands in the far cell from the Relay, from both sides; a step beside the door is one cell (no second root).
    /// - Which noise wins: a weak noise is Outweighed until the link's margin has decayed at 1.5 m/s; at most one re-target
    ///   per 1.5 s (Limited at 0 and 1.4 s, Retargeted at 1.5 s, Limited again at once).
    /// - Repeat: inside 6 s and 2 cells k halves at most once per re-target (ruled 2026-10-30): RepeatHeld inside the limit, k
    ///   left, the look upgraded; Repeat past it (halved once, then k 0: the exact point); 2 cells off is a repeat, 3 is not; 6 s on is not; a repeat in a running cautious look (one spot) restarts it
    ///   as the full search (3 spots).
    /// - BreakDoor queues (one slot, the margin rule), a door's licence at once, taken as the break ends; DebugPlace empties it.
    /// - A chase: deaf with sight; without it a noise moves only the last known point (state, ListenPoint, the lose meter
    ///   unchanged), LoseTrack goes there; HeardDoorSinceSight notes a door heard after sight is lost, sight clears it and
    ///   the link.
    /// - Deaf in Away and Arrive, heard after the step-in; FADE's one look; Withdraw's half range, its one look, the walk out.
    /// - DebugPlace clears the link and the counts, facing sets Heading.
    /// - Tier hearing (the table 1.0 to 1.3; the range scales; tuning.hearing has no effect).
    /// - The counts: HeardRetargets from a dispatch to the next, kept through an Away and a handoff; HeardRetargetsTotal.
    /// - The game's noises (a game component never enabled, its Relay and director put in by reflection): the walk is 9 x
    ///   hearing on the map, the Walk gait only, the slow gait 0, never the panel (Attention unchanged); sprint 27, glass 60,
    ///   device 45, door 14.
    /// </summary>
    static void RelayV4aHearing(List<GameObject> roots)
    {
        const string label = "relay v4a hearing: ";
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), -89f, "MAP INTERACTION TEST / relay v4a hearing");
        world.BuildForCapture();
        var body = BodyV3b(roots, "MAP INTERACTION TEST / relay v4a hearing player");
        var hand = BodyV3b(roots, "MAP INTERACTION TEST / relay v4a hearing hand");
        hand.transform.position = new Vector3(0f, -100f, 0f);
        // A shut door nothing shorter goes round: the way from e to c pays its shut opening.
        if (!LineDoorV3b(world, out var a, out var b, out var c, out var e, (d, near) =>
            {
                var far = d.a == near ? d.b : d.a;
                var behind = new GridCoord(2 * near.x - far.x, 2 * near.y - far.y);
                var beyond = new GridCoord(2 * far.x - near.x, 2 * far.y - near.y);
                return HeardPathV4a(world, beyond, behind, out var cost) < 18.5f && cost >= FrontRoomsMapPathField.ShutOpening;
            }))
        {
            Check(false, label + "no shut door with nothing shorter round it near the spawn");
            return;
        }
        var door = world.DoorBetween(a, b);
        var spawnCell = world.CellOf(world.SpawnWorldPosition);
        var spawnFeet = world.CellCenter(spawnCell);
        // The player stands well out of it (the Relay is blind); the door rows put the Relay at c or e.
        var feet = FarFeetV3b(world, door.position, 24f);
        body.transform.position = feet;
        Physics.SyncTransforms();
        var tuning = new FrontRoomsHunterTuning { sightRange = 0f };
        var fromSpawn = Walk(world, spawnCell, 16);
        var spawnField = new FrontRoomsMapPathField(90f);
        spawnField.Build(world, spawnCell);
        var open = fromSpawn.Where(p => p.Value >= 5 && spawnField.OpeningCostTo(p.Key) == 0f).OrderBy(p => p.Value).ThenBy(p => p.Key.x).ThenBy(p => p.Key.y).Select(p => p.Key).ToList();
        if (open.Count == 0) { Check(false, label + "no open cell 5 or more cells of walking from the spawn"); return; }
        var x = open[0];
        var far2 = open.Where(cell => CellsApartV4a(cell, x) >= 5).ToList();
        if (far2.Count == 0) { Check(false, label + "no two open cells 5 cells apart round the spawn"); return; }
        var y = far2[0];
        var px = world.CellCenter(x);
        var py = world.CellCenter(y);
        var dx = HeardPathV4a(world, x, spawnCell, out _);

        // 1. k: the pure rule.
        {
            var bad = new List<string>();
            void K(float d, float range, bool shut, int want)
            {
                var k = FrontRoomsMapHunter.EstimateErrorCells(d, range, shut);
                if (k != want) bad.Add(d.ToString("0.##", inv) + " of " + range.ToString("0.##", inv) + (shut ? " shut" : "") + ": " + k + " not " + want);
            }
            K(0f, 27f, false, 0);
            K(6.7f, 27f, false, 0);
            K(6.75f, 27f, false, 1);
            K(13.5f, 27f, false, 1);
            K(20.2f, 27f, false, 1);
            K(20.25f, 27f, false, 2);
            K(27f, 27f, false, 2);
            K(13.5f, 27f, true, 1);
            K(13.6f, 27f, true, 2);
            K(27f, 27f, true, 3);
            K(54f, 27f, false, 3);
            K(54f, 27f, true, 3);
            K(40f, 1000f, false, 0);
            K(5f, 0f, false, 0);
            Check(bad.Count == 0, label + "k = round(2d / (L h)), half up, +1 through a shut opening past half range, at most 3 (14 cases)" + (bad.Count > 0 ? ": " + string.Join("; ", bad.ToArray()) : ""));
        }

        // 2. A real noise over open cells: k 0, 1, 2 from how far inside its range it is.
        {
            var ok = true;
            var text = new List<string>();
            var multiples = new[] { 5f, 2.2f, 1.05f, 1f };
            var wants = new[] { 0, 1, 2, 2 };
            for (var i = 0; i < multiples.Length; i++)
            {
                var loud = dx * multiples[i];
                var h = new FrontRoomsMapHunter(world, tuning, body, null, 101 + 2 * i);
                h.DebugPlace(spawnFeet);
                h.Noise(px, loud);
                var link = h.SenseLink;
                var within = WalkV3b(world, x, wants[i], true, x, x);
                var fine = h.LastHeard == FrontRoomsRelayHeard.Linked && h.State == HunterState.Hunt && link.Active && link.K == wants[i] && h.LastHeardK == wants[i] && link.Cautious && link.Repeats == 0
                    && NearV4a(h.LastHeardMetres, dx) && NearV4a(h.LastHeardRange, loud) && NearV4a(link.Margin, loud - dx) && NearV4a(h.LastHeardMargin, loud - dx)
                    && (wants[i] == 0 ? link.Cell == x && SamePointV4a(link.Point, px) && h.ListenPoint.HasValue && SamePointV4a(h.ListenPoint.Value, px)
                        : link.Cell != x && within.ContainsKey(link.Cell) && SamePointV4a(link.Point, world.CellCenter(link.Cell)) && h.ListenPoint.HasValue && SamePointV4a(h.ListenPoint.Value, link.Point));
                ok &= fine;
                text.Add("L " + loud.ToString("0.0", inv) + ": k " + link.K + " at " + link.Cell + ", margin " + link.Margin.ToString("0.0", inv) + (fine ? "" : " WRONG (" + h.LastHeard + ", " + link + ")"));
            }
            var deaf = new FrontRoomsMapHunter(world, tuning, body, null, 109);
            deaf.DebugPlace(spawnFeet);
            deaf.Noise(px, dx - .5f);
            var beyond = deaf.LastHeard == FrontRoomsRelayHeard.OutOfRange && !deaf.SenseLink.Active && !deaf.ListenPoint.HasValue && deaf.State == HunterState.Search;
            var onF = new FrontRoomsMapHunter(world, tuning, body, null, 111);
            onF.DebugPlace(spawnFeet);
            onF.Tick(Dt, px, px + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
            onF.Noise(px, dx * 2.2f);
            var heardOnF = onF.LastHeard == FrontRoomsRelayHeard.Linked && NearV4a(onF.LastHeardMetres, dx) && NearV4a(onF.LastHeardRange, dx * 2.2f);
            Check(ok && beyond && heardOnF,
                label + "a noise " + dx.ToString("0.0", inv) + " m along the path: " + string.Join("; ", text.ToArray()) + "; each estimate within k cells of the source, the noise's own point at k 0; just past its range OutOfRange, no link "
                + beyond + " (" + deaf.LastHeard + "); the player's own cell, on F: " + onF.LastHeard + " at " + onF.LastHeardMetres.ToString("0.0", inv) + " m");
        }

        // 3. Through a shut opening.
        var relayC = world.CellCenter(c);
        var relayE = world.CellCenter(e);
        var dShut = HeardPathV4a(world, e, c, out var openingShut);
        {
            int KOf(float loud, int seed)
            {
                var h = new FrontRoomsMapHunter(world, tuning, body, null, seed);
                h.DebugPlace(relayC);
                h.Noise(relayE, loud);
                return h.LastHeard == FrontRoomsRelayHeard.Linked ? h.LastHeardK : -1;
            }
            var inside = KOf(dShut * 2.2f, 113);
            var past = KOf(dShut * 1.9f, 115);
            var edge = KOf(dShut, 117);
            OpenDoorV4a(world, door, a, b);
            var dOpen = HeardPathV4a(world, e, c, out var openingOpen);
            var pastOpen = KOf(dOpen * 1.9f, 119);
            ShutDoorV4a(world, door);
            Check(openingShut >= FrontRoomsMapPathField.ShutOpening && openingOpen == 0f && inside == 1 && past == 2 && edge == 3 && pastOpen == 1,
                label + "through the shut door (" + dShut.ToString("0.0", inv) + " m, opening " + openingShut.ToString("0", inv) + "): inside half range k " + inside + ", past it k " + past + " (1 + 1), at the edge k " + edge
                + " (2 + 1, the most); the door open (" + dOpen.ToString("0.0", inv) + " m) past half range k " + pastOpen + ", no +1");
        }

        // 4. k 0 at radius 1000, through the shut door too.
        {
            var h = new FrontRoomsMapHunter(world, tuning, body, null, 121);
            h.DebugPlace(relayC);
            h.Noise(relayE, 1000f);
            var link = h.SenseLink;
            Check(h.LastHeard == FrontRoomsRelayHeard.Linked && link.K == 0 && link.Cell == e && SamePointV4a(link.Point, relayE) && h.ListenPoint.HasValue && SamePointV4a(h.ListenPoint.Value, relayE) && h.State == HunterState.Hunt,
                label + "radius 1000: " + h.LastHeard + ", " + link + ", the exact point " + SamePointV4a(link.Point, relayE) + ", ListenPoint on it");
        }

        // 5. Side-correct.
        {
            var fromRelay = new FrontRoomsMapPathField(90f);
            fromRelay.Build(world, c);
            var side = fromRelay.OpeningCostTo(e);
            var twoSteps = WalkV3b(world, e, 2, true, e, e);
            var drawn = new HashSet<GridCoord>();
            var wrong = 0;
            for (var i = 0; i < 40; i++)
            {
                var h = new FrontRoomsMapHunter(world, tuning, body, null, 2 * i + 201);
                h.DebugPlace(relayC);
                h.Noise(relayE, dShut * 1.5f);
                var link = h.SenseLink;
                drawn.Add(link.Cell);
                if (h.LastHeard != FrontRoomsRelayHeard.Linked || link.K != 2 || !twoSteps.ContainsKey(link.Cell) || Mathf.Abs(fromRelay.OpeningCostTo(link.Cell) - side) > .5f) wrong++;
            }
            Check(wrong == 0 && drawn.Count >= 2 && !drawn.Contains(e) && twoSteps.ContainsKey(a) && !drawn.Contains(a) && !drawn.Contains(c),
                label + "side-correct: 40 draws of k 2 behind the shut door, " + drawn.Count + " cells (" + string.Join(" ", drawn.Select(g => g.ToString()).ToArray()) + "), " + wrong
                + " off the source's side; two steps from the source reach " + a + " on the Relay's side (" + twoSteps.ContainsKey(a) + "), never drawn");

            var hA = new FrontRoomsMapHunter(world, tuning, body, null, 123);
            hA.DebugPlace(relayC);
            hA.Noise(door.position, 14f);
            var hB = new FrontRoomsMapHunter(world, tuning, body, null, 125);
            hB.DebugPlace(relayE);
            hB.Noise(door.position, 14f);
            var dLine = HeardPathV4a(world, a, c, out _);
            var fromA = hA.LastHeard == FrontRoomsRelayHeard.Linked && hA.SenseLink.Cell == b && SamePointV4a(hA.SenseLink.Point, world.CellCenter(b)) && hA.LastHeardK == 0 && NearV4a(hA.LastHeardMetres, dLine);
            var fromB = hB.LastHeard == FrontRoomsRelayHeard.Linked && hB.SenseLink.Cell == a && SamePointV4a(hB.SenseLink.Point, world.CellCenter(a)) && hB.LastHeardK == 0 && NearV4a(hB.LastHeardMetres, dLine);
            var mouth = world.CrossingPoint(a, b);
            var across = (world.CellCenter(b) - world.CellCenter(a)).normalized;
            var beside = mouth + across * .5f;
            var hS = new FrontRoomsMapHunter(world, tuning, body, null, 127);
            hS.DebugPlace(relayC);
            hS.Noise(beside, 89f);
            var dBeside = HeardPathV4a(world, b, c, out var openingBeside);
            var oneCell = hS.LastHeard == FrontRoomsRelayHeard.Linked && NearV4a(hS.LastHeardMetres, dBeside) && openingBeside >= FrontRoomsMapPathField.ShutOpening && hS.SenseLink.Cell == b && SamePointV4a(hS.SenseLink.Point, beside);
            Check(fromA && fromB && oneCell,
                label + "a noise on the door's line lands in the far cell from the Relay: from " + c + " in " + hA.SenseLink.Cell + " (" + hA.LastHeardMetres.ToString("0.0", inv) + " m, both cells rooted), from " + e + " in " + hB.SenseLink.Cell
                + "; a step 0.5 m beside the door is one cell (" + hS.LastHeardMetres.ToString("0.0", inv) + " m, through the shut door, at the noise's own point " + oneCell + ")");
        }

        // 6. Which noise wins: the margin's decay.
        {
            var h = new FrontRoomsMapHunter(world, tuning, body, null, 129);
            h.DebugPlace(spawnFeet);
            h.Noise(px, dx + 6f);
            var linked = h.LastHeard == FrontRoomsRelayHeard.Linked && NearV4a(h.SenseLink.Margin, 6f);
            var cell0 = h.SenseLink.Cell;
            var listen0 = h.ListenPoint;
            var clear = CellsApartV4a(y, cell0) > FrontRoomsMapHunter.RepeatCells;
            int outweighed = 0, changed = 0;
            var firstDecay = -1f;
            var t = 0f;
            var retargetAt = -1f;
            var marginAt = 0f;
            var heardAs = FrontRoomsRelayHeard.None;
            while (t < 6f && retargetAt < 0f)
            {
                TickV4a(world, h, feet, .1f);
                t += .1f;
                if (t < FrontRoomsMapHunter.RetargetSeconds + .05f) continue;
                var link = h.SenseLink;
                if (firstDecay < 0f && NearV4a(link.MarginNow, link.Margin - FrontRoomsMapHunter.LinkDecayPerSecond * link.Age) && NearV4a(link.Age, t)) firstDecay = link.MarginNow;
                var dNow = HeardPathV4a(world, y, world.CellOf(h.Position), out _);
                h.Noise(py, dNow + 3f);
                if (h.LastHeard == FrontRoomsRelayHeard.Outweighed)
                {
                    outweighed++;
                    if (h.SenseLink.Cell != cell0 || h.ListenPoint != listen0) changed++;
                    continue;
                }
                retargetAt = t;
                marginAt = link.MarginNow;
                heardAs = h.LastHeard;
            }
            Check(linked && clear && firstDecay > 0f && outweighed >= 2 && changed == 0 && heardAs == FrontRoomsRelayHeard.Retargeted && retargetAt >= 1.9f && retargetAt <= 2.2f && marginAt <= 3f + .01f
                  && NearV4a(h.SenseLink.Margin, 3f) && h.HeardRetargets == 1,
                label + "which noise wins: a link of margin 6 fades at 1.5 m/s (" + firstDecay.ToString("0.00", inv) + " m left at 1.6 s); a noise of margin 3 is Outweighed " + outweighed + "x, the link unmoved (" + changed
                + " moved), then " + heardAs + " at " + retargetAt.ToString("0.0", inv) + " s with the link down to " + marginAt.ToString("0.00", inv) + " m; re-targets " + h.HeardRetargets);
        }

        // 7. At most one re-target per 1.5 s.
        {
            var h = new FrontRoomsMapHunter(world, tuning, body, null, 131);
            h.DebugPlace(spawnFeet);
            h.Noise(px, 1000f);
            var linked = h.LastHeard == FrontRoomsRelayHeard.Linked;
            bool OnX() => h.ListenPoint.HasValue && SamePointV4a(h.ListenPoint.Value, px);
            h.Noise(py, 1000f);
            var limited0 = h.LastHeard == FrontRoomsRelayHeard.Limited && OnX();
            TickV4a(world, h, feet, 1.4f);
            h.Noise(py, 1000f);
            var limited1 = h.LastHeard == FrontRoomsRelayHeard.Limited && OnX();
            TickV4a(world, h, feet, .1f);
            h.Noise(py, 1000f);
            var taken = h.LastHeard == FrontRoomsRelayHeard.Retargeted && h.ListenPoint.HasValue && SamePointV4a(h.ListenPoint.Value, py) && SamePointV4a(h.SenseLink.Point, py);
            h.Noise(px, 1000f);
            var limitedAgain = h.LastHeard == FrontRoomsRelayHeard.Limited && h.ListenPoint.HasValue && SamePointV4a(h.ListenPoint.Value, py);
            Check(linked && limited0 && limited1 && taken && limitedAgain && h.HeardLinks == 2 && h.HeardRetargets == 1 && h.HeardRetargetsTotal == 1,
                label + "at most one re-target per 1.5 s: a winning noise 3 or more cells off is Limited at once (" + limited0 + ") and at 1.4 s (" + limited1 + "), Retargeted at 1.5 s (" + taken + "), Limited again right after (" + limitedAgain
                + "); links " + h.HeardLinks + ", re-targets " + h.HeardRetargets);
        }

        // 8. Repeat.
        {
            var loud = dx * 1.05f;
            var h = new FrontRoomsMapHunter(world, tuning, body, null, 133);
            h.DebugPlace(spawnFeet);
            h.Noise(px, loud);
            var first = h.SenseLink;
            h.Noise(px, loud);
            var heldAs = h.LastHeard;
            var held = h.SenseLink;
            TickV4a(world, h, feet, 1.6f);
            h.Noise(px, loud);
            var againAs = h.LastHeard;
            var again = h.SenseLink;
            // Ruled 2026-10-30: k halves at most once per re-target; inside the limit only the look upgrades.
            var heldOk = first.K == 2 && first.Cautious && heldAs == FrontRoomsRelayHeard.RepeatHeld && held.K == first.K && held.Repeats == 1 && !held.Cautious && held.Cell == first.Cell && SamePointV4a(held.Point, first.Point);
            var againOk = againAs == FrontRoomsRelayHeard.Repeat && again.K == Mathf.Min(held.K, h.LastHeardK) / 2 && again.Repeats == 2 && !again.Cautious;
            TickV4a(world, h, feet, 1.6f);
            h.Noise(px, loud);
            var thirdAs = h.LastHeard;
            var third = h.SenseLink;
            var thirdOk = thirdAs == FrontRoomsRelayHeard.Repeat && third.K == 0 && third.Repeats == 3 && !third.Cautious && SamePointV4a(third.Point, px);
            Check(heldOk && againOk && thirdOk,
                label + "a repeat within 6 s and 2 cells: k stays " + first.K + " -> " + held.K + " at once (" + heldAs + ", the estimate left, cautious " + first.Cautious + " -> " + held.Cautious + "); 1.6 s on it halves once, k " + again.K + " (" + againAs + "); 3.2 s on k " + third.K + " (" + thirdAs + ", the exact point)");

            // 2 cells off is a repeat, 3 is not; 6 s on is not.
            var cells = fromSpawn.Keys.OrderBy(k => k.x).ThenBy(k => k.y).ToList();
            var two = cells.Where(k => CellsApartV4a(k, x) == FrontRoomsMapHunter.RepeatCells).ToList();
            var three = cells.Where(k => CellsApartV4a(k, x) == FrontRoomsMapHunter.RepeatCells + 1).ToList();
            if (two.Count == 0 || three.Count == 0) Check(false, label + "no cells 2 and 3 cells from " + x);
            else
            {
                var h2 = new FrontRoomsMapHunter(world, tuning, body, null, 135);
                h2.DebugPlace(spawnFeet);
                h2.Noise(px, 1000f);
                h2.Noise(world.CellCenter(two[0]), 1000f);
                var at2 = h2.LastHeard;
                var h3 = new FrontRoomsMapHunter(world, tuning, body, null, 137);
                h3.DebugPlace(spawnFeet);
                h3.Noise(px, 1000f);
                h3.Noise(world.CellCenter(three[0]), 1000f);
                var at3 = h3.LastHeard;
                var h6 = new FrontRoomsMapHunter(world, tuning, body, null, 139);
                h6.DebugPlace(spawnFeet);
                h6.Noise(px, dx + 12f);
                var k6 = h6.SenseLink.K;
                TickV4a(world, h6, feet, FrontRoomsMapHunter.RepeatSeconds + .1f);
                var stillLinked = h6.SenseLink.Active;
                var dNow = HeardPathV4a(world, x, world.CellOf(h6.Position), out _);
                h6.Noise(px, dNow + 12f);
                Check(at2 == FrontRoomsRelayHeard.RepeatHeld && at3 == FrontRoomsRelayHeard.Limited && stillLinked && h6.LastHeard == FrontRoomsRelayHeard.Retargeted && h6.SenseLink.Repeats == 0 && h6.SenseLink.K == h6.LastHeardK,
                    label + "2 cells from the estimate is a repeat (" + at2 + "), 3 is not (" + at3 + "); at 6.1 s the same noise is no repeat (" + h6.LastHeard + ", repeats " + h6.SenseLink.Repeats + ", k " + h6.SenseLink.K + " its own, was " + k6 + ")");
            }

            // A repeat in a running look: one spot becomes the full search.
            var hLook = new FrontRoomsMapHunter(world, tuning, body, null, 141);
            hLook.DebugPlace(spawnFeet);
            hLook.Noise(spawnFeet, 1000f);
            for (var i = 0; i < 60 && hLook.State != HunterState.Search; i++) hLook.Tick(Dt, feet, feet + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
            var lookState = hLook.State;
            var lookSpots = SpotsV4a(hLook);
            var lookCautious = hLook.SenseLink.Cautious;
            hLook.Noise(spawnFeet, 1000f);
            var upgradedAs = hLook.LastHeard;
            Check(lookState == HunterState.Search && lookCautious && lookSpots == 1 && upgradedAs == FrontRoomsRelayHeard.RepeatHeld && hLook.State == HunterState.Search && !hLook.SenseLink.Cautious && SpotsV4a(hLook) == 3,
                label + "a lone noise ends in a cautious look (" + lookState + ", " + lookSpots + " spot); a repeat there (" + upgradedAs + ") restarts it as the full search (" + SpotsV4a(hLook) + " spots)");
        }

        // 9. BreakDoor queues.
        {
            ShutDoorV4a(world, door);
            FindDoor(world, out var p2, out var q2, out _, (d, near) => d.edge != door.edge);
            var door2 = world.DoorBetween(p2, q2);
            var h = new FrontRoomsMapHunter(world, tuning, body, null, 143);
            var run = new DoorRunV3b(h);
            h.DebugPlace(relayC);
            h.Noise(relayE, 1000f);
            run.Run(world, feet, Vector3.forward, 20f, () => h.State == HunterState.BreakDoor);
            var breaking = h.State == HunterState.BreakDoor;
            var listen = h.ListenPoint;
            var licensedBefore = door2 != null && h.HasBreakLicence(door2);
            h.Noise(door2 != null ? door2.position : relayC, 89f);
            var queuedDoor = h.LastHeard == FrontRoomsRelayHeard.Queued && h.SenseQueued && h.State == HunterState.BreakDoor && h.ListenPoint == listen;
            var licenceNow = door2 != null && h.HasBreakLicence(door2);
            var dWeak = HeardPathV4a(world, c, world.CellOf(h.Position), out _);
            h.Noise(relayC, dWeak + 1f);
            var weakLost = h.LastHeard == FrontRoomsRelayHeard.Outweighed && h.SenseQueued;
            h.Noise(relayC, 1000f);
            var strongTaken = h.LastHeard == FrontRoomsRelayHeard.Queued && h.SenseQueued && h.State == HunterState.BreakDoor && h.ListenPoint == listen;
            var linkHeld = h.SenseLink.Active && SamePointV4a(h.SenseLink.Point, relayE);
            run.Run(world, feet, Vector3.forward, 6f, () => h.State != HunterState.BreakDoor);
            var after = h.LastHeard;
            Check(breaking && door2 != null && !licensedBefore && queuedDoor && licenceNow && weakLost && strongTaken && linkHeld && !h.SenseQueued && after == FrontRoomsRelayHeard.Retargeted && h.State == HunterState.Hunt
                  && SamePointV4a(h.SenseLink.Point, relayC) && h.ListenPoint.HasValue && SamePointV4a(h.ListenPoint.Value, relayC) && door.broken,
                label + "BreakDoor queues: a door's noise waits in the slot (" + queuedDoor + "), its BREAK licence at once (" + licenceNow + "); a faint noise loses to it (" + weakLost + "), a louder one replaces it (" + strongTaken + "), state and ListenPoint unmoved, the link held ("
                + linkHeld + "); as the break ends it is taken: " + after + ", the slot empty " + !h.SenseQueued + ", " + h.State + " to the queued point");

            ShutDoorV4a(world, door);
            var hd = new FrontRoomsMapHunter(world, tuning, body, null, 145);
            var runD = new DoorRunV3b(hd);
            hd.DebugPlace(relayC);
            hd.Noise(relayE, 1000f);
            runD.Run(world, feet, Vector3.forward, 20f, () => hd.State == HunterState.BreakDoor);
            var breakingD = hd.State == HunterState.BreakDoor;
            hd.Noise(relayC, 1000f);
            var inSlot = hd.SenseQueued;
            hd.DebugPlace(relayC, Vector3.right);
            var cleared = !hd.SenseQueued && !hd.SenseLink.Active && hd.HeardLinks == 0;
            runD.Run(world, feet, Vector3.forward, .5f, null);
            Check(breakingD && inSlot && cleared && !hd.SenseLink.Active,
                label + "DebugPlace in a break empties the slot and the link (" + cleared + "): nothing is taken after it (" + !hd.SenseLink.Active + ")");
            ShutDoorV4a(world, door);
        }

        // 10. A chase.
        {
            var sighted = new FrontRoomsHunterTuning { sightRange = 12f, lostSightSeconds = 6f };
            var mouth = world.CrossingPoint(a, b);
            var across = world.CellCenter(b) - world.CellCenter(a);
            across.y = 0f;
            across.Normalize();
            var inB = mouth + across * 2.5f;
            inB.y = world.CellCenter(b).y;
            var inA = mouth - across * 4.5f;
            inA.y = world.CellCenter(a).y;
            var lookBack = -across;

            OpenDoorV4a(world, door, a, b);
            body.transform.position = inB;
            Physics.SyncTransforms();
            var h = new FrontRoomsMapHunter(world, sighted, body, null, 147);
            var run = new DoorRunV3b(h);
            h.DebugPlace(inA);
            ChaseNowV4b(h, inB, () => run.Run(world, inB, lookBack, Dt, null));
            run.Run(world, inB, lookBack, 2f, () => h.State == HunterState.Chase);
            var chasing = h.State == HunterState.Chase;
            var seenAt = h.LastKnownPoint;
            h.Noise(relayC, 1000f);
            var deafSight = h.LastHeard == FrontRoomsRelayHeard.Deaf && !h.SenseLink.Active && SamePointV4a(h.LastKnownPoint, seenAt) && h.State == HunterState.Chase;
            ShutUnheardV3b(world, door, hand);
            var listen = h.ListenPoint;
            var meter = (float)FieldV4a(h, "lostTime");
            h.Noise(relayC, 1000f);
            var onlyPoint = h.LastHeard == FrontRoomsRelayHeard.Linked && h.State == HunterState.Chase && h.ListenPoint == listen && SamePointV4a(h.LastKnownPoint, relayC) && SamePointV4a(h.SenseLink.Point, relayC)
                && (float)FieldV4a(h, "lostTime") == meter;
            var leftAt = -1f;
            var leftTo = HunterState.Chase;
            var reached = -1f;
            run.Run(world, inB, lookBack, 14f, () =>
            {
                if (leftAt < 0f && h.State != HunterState.Chase && h.State != HunterState.BreakDoor)
                {
                    leftAt = run.t;
                    leftTo = h.State;
                }
                if (leftAt >= 0f && reached < 0f && Flat(h.Position - relayC) < 1f) reached = run.t;
                return reached >= 0f;
            });
            Check(chasing && deafSight && onlyPoint && leftTo == HunterState.Hunt && reached > 0f && h.DoorsBroken == 0,
                label + "a chase: with sight deaf (" + deafSight + "); the door shut between them, a noise moves only the last known point (" + onlyPoint + ": state, ListenPoint and the lose meter as they were); LoseTrack goes there ("
                + leftTo + " at " + leftAt.ToString("0.00", inv) + " s, there at " + reached.ToString("0.00", inv) + " s)");

            OpenDoorV4a(world, door, a, b);
            body.transform.position = inB;
            Physics.SyncTransforms();
            var h2 = new FrontRoomsMapHunter(world, sighted, body, null, 149);
            var run2 = new DoorRunV3b(h2);
            h2.DebugPlace(inA);
            ChaseNowV4b(h2, inB, () => run2.Run(world, inB, lookBack, Dt, null));
            run2.Run(world, inB, lookBack, 2f, () => h2.State == HunterState.Chase);
            var chasing2 = h2.State == HunterState.Chase;
            ShutUnheardV3b(world, door, hand);
            var noneYet = h2.HeardDoorSinceSight == null;
            h2.Noise(door.position, 1000f);
            var noted = h2.HeardDoorSinceSight == door && h2.HeardDoorSeconds == 0f && h2.HasBreakLicence(door) && h2.State == HunterState.Chase;
            // The player steps into sight on the Relay's side, ahead of it (the chase cone, V4b): sight replaces all of it.
            var back = mouth - across * 1f;
            back.y = world.CellCenter(a).y;
            body.transform.position = back;
            Physics.SyncTransforms();
            run2.Run(world, back, across, 1f, () => h2.SeesPlayer && h2.HeardDoorSinceSight == null);
            var sightCleared = h2.SeesPlayer && h2.HeardDoorSinceSight == null && !h2.SenseLink.Active;
            var blind = new FrontRoomsMapHunter(world, tuning, body, null, 151);
            blind.DebugPlace(relayC);
            blind.Noise(door.position, 1000f);
            Check(chasing2 && noneYet && noted && sightCleared && blind.HeardDoorSinceSight == null,
                label + "HeardDoorSinceSight (V4b's seam): a door heard after sight is lost is noted (" + noted + ", with its BREAK licence, still a chase), sight clears it and the link (" + sightCleared
                + "); never set by a Relay that has not seen the player (" + (blind.HeardDoorSinceSight == null) + ")");
            ShutDoorV4a(world, door);
            body.transform.position = feet;
            Physics.SyncTransforms();
        }

        // 11. Deaf off the map; FADE; Withdraw.
        {
            body.transform.position = spawnFeet;
            Physics.SyncTransforms();
            var h = new FrontRoomsMapHunter(world, tuning, body, null, 153);
            h.Noise(px, 1000f);
            var deafDormant = h.LastHeard == FrontRoomsRelayHeard.Deaf;
            h.Arm();
            h.Noise(px, 1000f);
            var deafAway = h.LastHeard == FrontRoomsRelayHeard.Deaf && !h.ListenPoint.HasValue && !h.SenseLink.Active && h.State == HunterState.Away;
            var placed = DispatchNear(world, h, spawnFeet, Vector3.forward, out _);
            h.Noise(px, 1000f);
            var deafArrive = placed && h.LastHeard == FrontRoomsRelayHeard.Deaf && !h.ListenPoint.HasValue && !h.SenseLink.Active && h.State == HunterState.Arrive;
            for (var t = 0f; t < 10f && h.State == HunterState.Arrive; t += Dt) TickV4a(world, h, spawnFeet, Dt);
            h.Noise(px, 1000f);
            var heardThen = h.LastHeard == FrontRoomsRelayHeard.Linked && h.ListenPoint.HasValue && h.OnMap;
            Check(deafDormant && deafAway && deafArrive && heardThen,
                label + "deaf off the map: Dormant " + deafDormant + ", Away " + deafAway + ", Arrive " + deafArrive + " (no link, no ListenPoint); after the step-in the same noise is " + h.LastHeard);

            var hf = new FrontRoomsMapHunter(world, tuning, body, null, 155);
            hf.Arm();
            hf.DebugPlace(spawnFeet);
            hf.Fade(false);
            hf.Noise(px, 1000f);
            var fadeFirst = hf.LastHeard == FrontRoomsRelayHeard.Linked && hf.SenseLink.Cautious;
            hf.Noise(py, 1000f);
            var fadeSecond = hf.LastHeard == FrontRoomsRelayHeard.Deaf;
            Check(fadeFirst && fadeSecond, label + "FADE: the first noise is one cautious look (" + fadeFirst + "), the next is not heard (" + fadeSecond + ")");

            var hw = new FrontRoomsMapHunter(world, tuning, body, null, 157);
            var runW = new DoorRunV3b(hw);
            hw.Arm();
            var placedW = DispatchNear(world, hw, spawnFeet, Vector3.forward, out _);
            runW.Run(world, spawnFeet, Vector3.forward, 40f, () => hw.State == HunterState.Withdraw);
            var withdrawing = placedW && hw.State == HunterState.Withdraw;
            var cell = world.CellOf(hw.Position);
            var near = Walk(world, cell, 4).Where(p => p.Value >= 2).OrderBy(p => p.Value).ThenBy(p => p.Key.x).ThenBy(p => p.Key.y).Select(p => p.Key).FirstOrDefault();
            var dW = HeardPathV4a(world, near, cell, out _);
            hw.Noise(world.CellCenter(near), dW * 1.5f);
            var beyondHalf = hw.LastHeard == FrontRoomsRelayHeard.OutOfRange && hw.State == HunterState.Withdraw;
            hw.Noise(world.CellCenter(near), dW * 2.5f);
            var heardHalf = hw.LastHeard == FrontRoomsRelayHeard.Linked && NearV4a(hw.LastHeardRange, dW * 1.25f) && hw.State == HunterState.Hunt && hw.SenseLink.Cautious;
            hw.Noise(world.CellCenter(near), 1000f);
            var lookSpent = hw.LastHeard == FrontRoomsRelayHeard.Deaf;
            runW.Run(world, spawnFeet, Vector3.forward, 60f, () => hw.State == HunterState.Away);
            Check(withdrawing && beyondHalf && heardHalf && lookSpent && hw.State == HunterState.Away,
                label + "Withdraw hears at half range (" + dW.ToString("0.0", inv) + " m: L " + (dW * 1.5f).ToString("0.0", inv) + " " + beyondHalf + ", L " + (dW * 2.5f).ToString("0.0", inv) + " " + heardHalf
                + "), one cautious look (the next " + lookSpent + "), then the walk out resumes: " + hw.State + " at " + runW.awayAt.ToString("0.0", inv) + " s");
        }

        // 12. DebugPlace clears; facing sets Heading.
        {
            var h = new FrontRoomsMapHunter(world, tuning, body, null, 159);
            h.DebugPlace(spawnFeet);
            h.Noise(px, 1000f);
            TickV4a(world, h, feet, FrontRoomsMapHunter.RetargetSeconds + .1f);
            h.Noise(py, 1000f);
            var counted = h.SenseLink.Active && h.HeardLinks == 2 && h.HeardRetargets == 1 && h.HeardRetargetsTotal == 1;
            h.DebugPlace(spawnFeet, new Vector3(3f, 0f, 4f));
            var heading = (h.Heading - new Vector3(.6f, 0f, .8f)).sqrMagnitude < 1e-6f;
            var clear = !h.SenseLink.Active && !h.SenseQueued && h.HeardLinks == 0 && h.HeardRetargets == 0 && h.HeardRetargetsTotal == 1 && h.State == HunterState.Search;
            h.DebugPlace(spawnFeet, Vector3.zero);
            var kept = (h.Heading - new Vector3(.6f, 0f, .8f)).sqrMagnitude < 1e-6f;
            Check(counted && clear && heading && kept,
                label + "DebugPlace clears the link and the encounter's counts (" + clear + "; the run's total stays " + h.HeardRetargetsTotal + "); facing sets Heading (" + heading + ", " + h.Heading.ToString("0.00") + "), no facing keeps it (" + kept + ")");
        }

        // 13. Tier hearing, never the scene's.
        {
            var rules = new FrontRoomsTierRules { v2 = null };
            rules.Normalize();
            var table = new List<float>();
            for (var i = 1; i <= 5; i++) table.Add(rules.V2At(i).hearing);
            var tableOk = NearV4a(table[0], 1f) && NearV4a(table[1], 1.07f) && NearV4a(table[2], 1.14f) && NearV4a(table[3], 1.21f) && NearV4a(table[4], 1.3f);
            var loud = dx / 1.2f;
            FrontRoomsRelayHeard Heard(FrontRoomsHunterTuning t, FrontRoomsTierV2 tier, int seed, out float range)
            {
                var h = new FrontRoomsMapHunter(world, t, body, null, seed);
                if (tier != null) h.TierV2 = tier;
                h.DebugPlace(spawnFeet);
                h.Noise(px, loud);
                range = h.LastHeardRange;
                return h.LastHeard;
            }
            var baseHeard = Heard(tuning, null, 161, out _);
            var t5Heard = Heard(tuning, new FrontRoomsTierV2 { hearing = 1.3f }, 163, out var t5Range);
            var loudScene = Heard(new FrontRoomsHunterTuning { sightRange = 0f, hearing = 5f }, null, 165, out _);
            var quietScene = Heard(new FrontRoomsHunterTuning { sightRange = 0f, hearing = .1f }, new FrontRoomsTierV2 { hearing = 1.3f }, 167, out var quietRange);
            Check(tableOk && baseHeard == FrontRoomsRelayHeard.OutOfRange && t5Heard == FrontRoomsRelayHeard.Linked && NearV4a(t5Range, loud * 1.3f) && loudScene == FrontRoomsRelayHeard.OutOfRange
                  && quietScene == FrontRoomsRelayHeard.Linked && NearV4a(quietRange, loud * 1.3f),
                label + "hearing is the tier's (" + string.Join(" ", table.Select(v => v.ToString("0.00", inv)).ToArray()) + "): L " + loud.ToString("0.0", inv) + " at " + dx.ToString("0.0", inv) + " m is " + baseHeard + " at 1.0, " + t5Heard
                + " at 1.3 (range " + t5Range.ToString("0.0", inv) + "); tuning.hearing has no effect (5: " + loudScene + ", 0.1: " + quietScene + ")");
        }

        // 14. The counts: an Away keeps them, a dispatch starts them, a handoff keeps them.
        {
            body.transform.position = spawnFeet;
            Physics.SyncTransforms();
            var eye = spawnFeet + Vector3.up * ModuleUnits.PlayerEye;
            var forward = Vector3.forward;
            var h = new FrontRoomsMapHunter(world, tuning, body, null, 169);
            h.Arm();
            var placed = DispatchNear(world, h, spawnFeet, forward, out _);
            for (var t = 0f; t < 10f && h.State == HunterState.Arrive; t += Dt) TickV4a(world, h, spawnFeet, Dt);
            var onMap = h.OnMap;
            h.Noise(px, 1000f);
            TickV4a(world, h, spawnFeet, FrontRoomsMapHunter.RetargetSeconds + .1f);
            h.Noise(py, 1000f);
            var counted = h.HeardLinks == 2 && h.HeardRetargets == 1 && h.HeardRetargetsTotal == 1;
            h.SendAway(FrontRoomsRelayAwayReason.Test);
            var keptAway = h.State == HunterState.Away && !h.SenseLink.Active && h.HeardLinks == 2 && h.HeardRetargets == 1 && h.HeardRetargetsTotal == 1;
            var again = DispatchNear(world, h, spawnFeet, forward, out _);
            var fresh = again && h.State == HunterState.Arrive && h.HeardLinks == 0 && h.HeardRetargets == 0 && h.HeardRetargetsTotal == 1;
            Check(placed && onMap && counted && keptAway && fresh,
                label + "HeardRetargets: " + counted + " on the map; the Away keeps them for the report (" + keptAway + "), the next dispatch starts again and the run's total stays (" + fresh + ", total " + h.HeardRetargetsTotal + ")");

            // The handoff, through the director (RelayV3bCalls' way there).
            var field = new FrontRoomsMapPathField(45f);
            field.Build(world, spawnCell);
            float W(GridCoord cell) => Mathf.Min(field.DistanceTo(cell), 3f * Vector3.Distance(world.CellCenter(cell), spawnFeet));
            var fromPlayer = Walk(world, spawnCell, 40);
            var hh = new FrontRoomsMapHunter(world, tuning, body, null, 171);
            var d = new FrontRoomsRelayDirector(hh);
            d.Arm(false, false);
            var summonCell = fromPlayer.Where(p => p.Value >= 5 && p.Value <= 10).OrderBy(p => p.Value).ThenBy(p => p.Key.x).ThenBy(p => p.Key.y).Select(p => p.Key).FirstOrDefault();
            d.Summon(world.CellCenter(summonCell));
            var clock = 0f;
            void Step()
            {
                clock += Dt;
                d.Tick(Dt, 1, false, spawnFeet, eye, forward);
                hh.Tick(Dt, spawnFeet, eye, forward);
                world.TickDoorsForTools(Dt);
                Physics.SyncTransforms();
            }
            while (clock < 30f && !(hh.State == HunterState.Listen && hh.OnMap)) Step();
            var stepped = hh.State == HunterState.Listen && hh.OnMap;
            var relayCell = world.CellOf(hh.Position);
            var fromRelay = Walk(world, relayCell, 44);
            var devices = fromRelay.Where(p => p.Value >= 24 && fromPlayer.ContainsKey(p.Key) && fromPlayer[p.Key] >= 14 && W(p.Key) >= 36f && !world.InStartArea(p.Key) && !world.InStairwell(p.Key))
                .OrderBy(p => p.Value).ThenBy(p => p.Key.x).ThenBy(p => p.Key.y).Select(p => p.Key).ToList();
            var near = fromRelay.Where(p => p.Value >= 3 && p.Value <= 6 && W(p.Key) >= 36f).OrderBy(p => p.Value).ThenBy(p => p.Key.x).ThenBy(p => p.Key.y).Select(p => p.Key).ToList();
            var farNoise = fromRelay.Where(p => p.Value >= 7 && p.Value <= 10 && W(p.Key) >= 36f).OrderBy(p => p.Value).ThenBy(p => p.Key.x).ThenBy(p => p.Key.y).Select(p => p.Key).ToList();
            if (!stepped || devices.Count == 0 || near.Count == 0 || farNoise.Count == 0) Check(false, label + "handoff: stepped in " + stepped + ", " + devices.Count + " device cells, " + near.Count + " + " + farNoise.Count + " noise cells");
            else
            {
                hh.Noise(world.CellCenter(near[0]), 1000f);
                for (var i = 0; i < 95; i++) Step();
                hh.Noise(world.CellCenter(farNoise[0]), 1000f);
                var before = hh.HeardRetargets;
                var kind = hh.RedispatchKind(world.CellCenter(devices[0]), spawnFeet, eye);
                var took = d.DeviceTrigger(world.CellCenter(devices[0]), "glass");
                Check(before == 1 && kind == FrontRoomsRelayRedispatchKind.Handoff && took && hh.Handoffs == 1 && hh.State == HunterState.Arrive && hh.HeardRetargets == before && hh.HeardRetargetsTotal == before && !hh.SenseLink.Active,
                    label + "a handoff keeps the encounter's count: " + before + " before, " + hh.HeardRetargets + " after (" + kind + ", " + hh.Handoffs + " handoff, " + hh.State + "), the link gone (" + !hh.SenseLink.Active + ")");
            }
            body.transform.position = feet;
            Physics.SyncTransforms();
        }

        // 15. The game's noises: the real WalkStepNoise and RelayNoise on a game component that never woke (made on an inactive object).
        {
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var type = typeof(FrontRooms3DGame);
            var root = new GameObject("MAP INTERACTION TEST / relay v4a game") { hideFlags = HideFlags.DontSave };
            root.SetActive(false);
            roots.Add(root);
            var game = root.AddComponent<FrontRooms3DGame>();
            var player = new GameObject("MAP INTERACTION TEST / relay v4a game player") { hideFlags = HideFlags.DontSave };
            roots.Add(player);
            var gait = type.GetProperty("PlayerGait");
            var walkStep = type.GetMethod("WalkStepNoise", flags);
            var relayNoise = type.GetMethod("RelayNoise", flags);
            var kinds = type.GetNestedType("RelayNoiseKind", System.Reflection.BindingFlags.NonPublic);
            var keepGait = gait.GetValue(null);
            // Inside 9 m along the path; x is past it.
            var closeCell = fromSpawn.Keys.Where(k => spawnField.DistanceTo(k) >= 6f).OrderBy(k => spawnField.DistanceTo(k)).ThenBy(k => k.x).ThenBy(k => k.y).First();
            var dClose = spawnField.DistanceTo(closeCell);
            FrontRoomsMapHunter Relay(int seed, FrontRoomsTierV2 tier)
            {
                var h = new FrontRoomsMapHunter(world, tuning, body, null, seed);
                if (tier != null) h.TierV2 = tier;
                h.DebugPlace(spawnFeet);
                type.GetField("relay", flags).SetValue(game, h);
                return h;
            }
            type.GetField("playerRoot", flags).SetValue(game, player.transform);
            FrontRoomsRelayHeard Walked(FrontRoomsMapHunter h, FrontRoomsGait g, Vector3 at)
            {
                player.transform.position = at;
                gait.SetValue(null, g);
                walkStep.Invoke(game, null);
                return h.LastHeard;
            }
            var h1 = Relay(173, null);
            var d1 = new FrontRoomsRelayDirector(h1);
            d1.Arm(false, false);
            h1.DebugPlace(spawnFeet);
            type.GetField("director", flags).SetValue(game, d1);
            var attention = d1.Policy.Attention;
            var heard = Walked(h1, FrontRoomsGait.Walk, world.CellCenter(closeCell));
            var range1 = h1.LastHeardRange;
            var panel = Mathf.Abs(d1.Policy.Attention - attention) < 1e-5f;
            var h5 = Relay(175, new FrontRoomsTierV2 { hearing = 1.3f });
            Walked(h5, FrontRoomsGait.Walk, world.CellCenter(closeCell));
            var range5 = h5.LastHeardRange;
            var hFar = Relay(177, null);
            var farWalk = Walked(hFar, FrontRoomsGait.Walk, px);
            var hSlow = Relay(179, null);
            var slow = Walked(hSlow, FrontRoomsGait.Slow, world.CellCenter(closeCell));
            var hStill = Relay(181, null);
            var still = Walked(hStill, FrontRoomsGait.Still, world.CellCenter(closeCell));
            var hRun = Relay(183, null);
            var run = Walked(hRun, FrontRoomsGait.Run, world.CellCenter(closeCell));
            gait.SetValue(null, keepGait);
            float Loudness(string kind, int seed)
            {
                var h = Relay(seed, null);
                relayNoise.Invoke(game, new[] { Enum.Parse(kinds, kind), (object)spawnFeet });
                return h.LastHeard == FrontRoomsRelayHeard.Linked ? h.LastHeardRange : -1f;
            }
            var sprint = Loudness("Sprint", 185);
            var glass = Loudness("Glass", 187);
            var device = Loudness("Device", 189);
            var doorNoise = Loudness("Door", 191);
            type.GetField("relay", flags).SetValue(game, null);
            type.GetField("director", flags).SetValue(game, null);
            Check(heard == FrontRoomsRelayHeard.Linked && NearV4a(range1, 9f) && panel && NearV4a(range5, 11.7f) && dx > 9f && farWalk == FrontRoomsRelayHeard.OutOfRange
                  && slow == FrontRoomsRelayHeard.None && still == FrontRoomsRelayHeard.None && run == FrontRoomsRelayHeard.None,
                label + "the walk (the game's WalkStepNoise): " + heard + " at " + dClose.ToString("0.0", inv) + " m, range " + range1.ToString("0.0", inv) + " at hearing 1.0, " + range5.ToString("0.0", inv) + " at 1.3; " + dx.ToString("0.0", inv) + " m off " + farWalk
                + "; never the panel (Attention " + attention.ToString("0.0", inv) + " -> " + d1.Policy.Attention.ToString("0.0", inv) + "); slow gait " + slow + ", still " + still + ", run " + run + " (a sprint step is its own noise)");
            Check(NearV4a(sprint, 27f) && NearV4a(glass, 60f) && NearV4a(device, 45f) && NearV4a(doorNoise, 14f),
                label + "the game's loudness (RelayNoise): sprint " + sprint.ToString("0.#", inv) + ", glass " + glass.ToString("0.#", inv) + ", device " + device.ToString("0.#", inv) + ", door " + doorNoise.ToString("0.#", inv) + " until V5 splits it");
        }
    }
#endif

#if FRONTROOMS_RELAY_V2
    // ---------- Relay v2 V4b: sight, lock-on, chase, search (RELAY 8, C37, the 2026-10-30 rulings) ----------

    /// <summary>Clear at knee, eye and head height along a flat line, and a shoulder either side of it.</summary>
    static bool LineClearV4b(Vector3 from, Vector3 dir, float metres)
    {
        var side = Vector3.Cross(Vector3.up, dir).normalized;
        foreach (var height in new[] { .4f, ModuleUnits.PlayerEye, ModuleUnits.RelayEye })
            foreach (var off in new[] { -.35f, 0f, .35f })
                if (Physics.Raycast(from + Vector3.up * height + side * off, dir, metres, ~0, QueryTriggerInteraction.Ignore)) return false;
        return true;
    }

    /// <summary>
    /// Relay v2 step V4b, on a built map: a long clear line, an open spot and a door that fits. One row per line of the plan's
    /// Scope, C37's, and the late additions. Rows that are not about the G3 gate set GateOpenForTools; cone rows hold the head.
    /// </summary>
    static void RelayV4bSight(List<GameObject> roots)
    {
        const string label = "relay v4b: ";
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), -91f, "MAP INTERACTION TEST / relay v4b");
        world.BuildForCapture();
        var body = BodyV3b(roots, "MAP INTERACTION TEST / relay v4b player");
        var hand = BodyV3b(roots, "MAP INTERACTION TEST / relay v4b hand");
        body.transform.position = hand.transform.position = new Vector3(0f, -100f, 0f);
        Physics.SyncTransforms();
        var spawnCell = world.CellOf(world.SpawnWorldPosition);
        var spawnFeet = world.CellCenter(spawnCell);
        var cells = Walk(world, spawnCell, 40).Where(p => !world.InStartArea(p.Key) && !world.InStairwell(p.Key))
            .OrderBy(p => p.Value).ThenBy(p => p.Key.x).ThenBy(p => p.Key.y).Select(p => p.Key).ToList();

        // The longest clear line (the chase's 15 m row needs 16), and the most open spot.
        var line0 = Vector3.zero;
        var dir = Vector3.forward;
        var lineMetres = 0f;
        foreach (var want in new[] { 22f, 20f, 18f, 16.5f, 15f, 13f, 11f })
        {
            foreach (var cell in cells)
            {
                foreach (var s in Steps)
                {
                    var d = world.CellCenter(cell + s) - world.CellCenter(cell);
                    d.y = 0f;
                    d.Normalize();
                    if (!LineClearV4b(world.CellCenter(cell), d, want)) continue;
                    var end = world.CellOf(world.CellCenter(cell) + d * want);
                    if (!world.IsBuilt(end) || world.InStartArea(end) || world.InStairwell(end)) continue;
                    line0 = world.CellCenter(cell);
                    dir = d;
                    lineMetres = want;
                    break;
                }
                if (lineMetres > 0f) break;
            }
            if (lineMetres > 0f) break;
        }
        if (lineMetres < 11f) { Check(false, label + "no clear line of 11 m near the spawn"); return; }
        var spot = Vector3.zero;
        var fwd = Vector3.forward;
        var spotMetres = 0f;
        foreach (var want in new[] { 3.4f, 3f, 2.6f, 2.2f })
        {
            foreach (var cell in cells)
            {
                var open = true;
                for (var deg = 0; deg < 360 && open; deg += 15) open = LineClearV4b(world.CellCenter(cell), Quaternion.AngleAxis(deg, Vector3.up) * Vector3.forward, want);
                if (!open) continue;
                spot = world.CellCenter(cell);
                spotMetres = want;
                break;
            }
            if (spotMetres > 0f) break;
        }
        if (spotMetres < 2.2f) { Check(false, label + "no spot open 2.2 m all round near the spawn"); return; }
        var r = spotMetres - .2f;
        if (!LineDoorV3b(world, out var a, out var b, out var c, out var e)) { Check(false, label + "no door with open cells straight on from it on both sides near the spawn"); return; }
        var door = world.DoorBetween(a, b);
        var mouth = world.CrossingPoint(a, b);
        mouth.y = world.CellCenter(a).y;
        var across = world.CellCenter(b) - world.CellCenter(a);
        across.y = 0f;
        across.Normalize();
        var inA = mouth - across * 4.5f;
        var inB = mouth + across * 2.5f;
        var awayFeet = FarFeetV3b(world, spawnFeet, 30f);
        var up = Vector3.up * ModuleUnits.PlayerEye;
        var side = Vector3.Cross(Vector3.up, dir).normalized;
        var jig = 0;

        void Put(Vector3 feet)
        {
            body.transform.position = feet;
            Physics.SyncTransforms();
        }
        FrontRoomsMapHunter New(FrontRoomsHunterTuning t, int seed, Vector3 at, Vector3 facing, bool gateOpen)
        {
            var h = new FrontRoomsMapHunter(world, t ?? new FrontRoomsHunterTuning(), body, null, seed);
            h.DebugPlace(at, facing);
            h.GateOpenForTools = gateOpen;
            return h;
        }
        // One tick with the player at feet; moving: a small step every other tick, so never "still".
        void Step(FrontRoomsMapHunter h, Vector3 feet, bool moving = false)
        {
            var at = feet;
            if (moving && ++jig % 2 == 0) at += side * .01f;
            h.Tick(Dt, at, at + up, Vector3.forward);
            world.TickDoorsForTools(Dt);
            Physics.SyncTransforms();
        }
        // A lock-on from here on (DebugLockOn), the hold run out: the first tick of the chase.
        void ToChase(FrontRoomsMapHunter h, Vector3 feet, bool moving = false)
        {
            Put(feet);
            h.DebugLockOn(feet);
            for (var i = 0; i < 180 && h.State != HunterState.Chase; i++) Step(h, feet, moving);
        }
        void Blind(FrontRoomsMapHunter h, Vector3 feet, float seconds)
        {
            for (var s = 0f; s < seconds - 1e-4f; s += Dt) Step(h, feet);
        }
        var chaseStates = new[] { HunterState.Chase, HunterState.BreakDoor };

        // 1. The numbers.
        {
            var bad = new List<string>();
            void Same(string what, float got, float want)
            {
                if (Mathf.Abs(got - want) > 1e-4f) bad.Add(what + " " + got.ToString("0.###", inv) + " not " + want.ToString("0.###", inv));
            }
            Same("hold first 5.9 m", FrontRoomsMapHunter.LockOnHold(false, 5.9f), 1.7f);
            Same("hold first 6.0 m", FrontRoomsMapHunter.LockOnHold(false, 6f), 1.7f);
            Same("hold first 6.1 m", FrontRoomsMapHunter.LockOnHold(false, 6.1f), .7f);
            Same("hold first 12 m", FrontRoomsMapHunter.LockOnHold(false, 12f), .7f);
            Same("hold again 1 m", FrontRoomsMapHunter.LockOnHold(true, 1f), .35f);
            Same("hold again 9 m", FrontRoomsMapHunter.LockOnHold(true, 9f), .35f);
            Same("close metres", FrontRoomsMapHunter.CloseLockOnMetres, 6f);
            Same("close extra", FrontRoomsMapHunter.CloseLockOnExtraSeconds, 1f);
            Same("T 12 m T1", FrontRoomsMapHunter.SpotSeconds(12f, 1f), .78f);
            Same("T 12 m T5", FrontRoomsMapHunter.SpotSeconds(12f, .8f), .624f);
            var rows = FrontRoomsTierRules.DefaultsV2();
            var spots = new[] { 3, 3, 3, 4, 4 };
            var seconds = new[] { 15f, 15f, 16f, 17f, 18f };
            var chase = new[] { 4.2f, 4.3f, 4.4f, 4.4f, 4.4f };
            var mul = new[] { 1f, .95f, .9f, .85f, .8f };
            for (var i = 0; i < 5; i++)
            {
                Same("T" + (i + 1) + " spotMul", rows[i].spotMul, mul[i]);
                Same("T" + (i + 1) + " search seconds", rows[i].searchSeconds, seconds[i]);
                Same("T" + (i + 1) + " spots", rows[i].searchSpots, spots[i]);
                Same("T" + (i + 1) + " chase", rows[i].chaseSpeed, chase[i]);
            }
            var capped = New(null, 300, spot, Vector3.forward, true);
            capped.TierV2 = new FrontRoomsTierV2 { chaseSpeed = 5f };
            Same("the chase cap", capped.ChaseSpeed, 4.4f);
            Same("catch", new FrontRoomsHunterTuning().catchDistance, .7f);
            Check(bad.Count == 0, label + "the numbers: C37's two constants and LockOnHold (5.9, 6.0 long, 6.1 not; re-acquire 0.35 s at any distance), T(12 m) at T1 and T5, spotMul, search seconds and spots, chase speed by tier, the 4.4 cap, catch 0.7 m"
                + (bad.Count > 0 ? ": " + string.Join("; ", bad.ToArray()) : "") + " (fixtures: a clear line of " + lineMetres.ToString("0.#", inv) + " m, a spot open " + spotMetres.ToString("0.#", inv) + " m)");
        }

        // 2. The head.
        {
            var blind = new FrontRoomsHunterTuning { sightRange = 0f };
            var held = New(blind, 301, spot, fwd, true);
            held.DebugHoldLook(100f);
            var clampUp = NearV4a(held.LookYaw, FrontRoomsMapHunter.HeadLimitDegrees);
            held.DebugHoldLook(-100f);
            var clampDown = NearV4a(held.LookYaw, -FrontRoomsMapHunter.HeadLimitDegrees);
            var c1 = New(blind, 303, spot, fwd, true);
            float lo = 0f, hi = 0f;
            var standing = 0;
            for (var i = 0; i < 90; i++)
            {
                Step(c1, awayFeet);
                if (c1.Moving) continue;
                standing++;
                lo = Mathf.Min(lo, c1.LookYaw);
                hi = Mathf.Max(hi, c1.LookYaw);
            }
            var w = New(blind, 305, line0, dir, true);
            w.Noise(line0 + dir * (lineMetres - 1f), 1000f);
            float wLo = 0f, wHi = 0f;
            var walked = 0;
            for (var i = 0; i < 300 && w.State == HunterState.Hunt; i++)
            {
                Step(w, awayFeet);
                if (!w.Moving) continue;
                walked++;
                wLo = Mathf.Min(wLo, w.LookYaw);
                wHi = Mathf.Max(wHi, w.LookYaw);
            }
            var limit = FrontRoomsMapHunter.WalkSweepDegrees + .01f;
            var fade = New(blind, 307, spot, fwd, true);
            fade.Fade(false);
            var fadeMost = 0f;
            for (var i = 0; i < 90; i++)
            {
                Step(fade, awayFeet);
                fadeMost = Mathf.Max(fadeMost, Mathf.Abs(fade.LookYaw));
            }
            Check(clampUp && clampDown && standing > 30 && hi - lo >= 35f && hi <= 70.01f && lo >= -70.01f && walked > 60 && wHi >= 30f && wHi <= limit && wLo <= -30f && wLo >= -limit && fadeMost < .5f,
                label + "LookYaw: clamped at 70 either way (" + clampUp + ", " + clampDown + "); standing it canvasses (" + lo.ToString("0", inv) + " to " + hi.ToString("0", inv) + " deg over " + standing + " ticks); walking the sweep stays in 40 ("
                + wLo.ToString("0", inv) + " to " + wHi.ToString("0", inv) + " over " + walked + " ticks); under FADE forward (at most " + fadeMost.ToString("0.0", inv) + " deg)");

            // Tracks at the meter's 0.5, forward in a chase.
            var bearing = 30f;
            var p = spot + Quaternion.AngleAxis(bearing, Vector3.up) * fwd * r;
            Put(p);
            var tr = New(null, 309, spot, fwd, false);
            for (var i = 0; i < 120 && tr.Spotting < FrontRoomsMapHunter.AlmostLevel; i++) Step(tr, p, true);
            var reached = tr.Spotting >= FrontRoomsMapHunter.AlmostLevel;
            for (var i = 0; i < 18; i++) Step(tr, p, true);
            var tracked = Vector3.SignedAngle(new Vector3(tr.Heading.x, 0f, tr.Heading.z), new Vector3(p.x - tr.Position.x, 0f, p.z - tr.Position.z), Vector3.up);
            var trackOk = reached && Mathf.Abs(tr.LookYaw - tracked) < 2f;
            var ch = New(null, 311, spot, fwd, true);
            ToChase(ch, p);
            for (var i = 0; i < 30; i++) Step(ch, p);
            Check(trackOk && ch.State == HunterState.Chase && Mathf.Abs(ch.LookYaw) < 1f,
                label + "the head tracks from the meter's 0.5 (" + tr.LookYaw.ToString("0.0", inv) + " deg, the player at " + tracked.ToString("0.0", inv) + "); locked forward in a chase (" + ch.LookYaw.ToString("0.0", inv) + " deg, " + ch.State + ")");
        }

        // 3. The calm cone.
        {
            bool Sees(int seed, float head, float bearing, float metres, Vector3 from, Vector3 forward)
            {
                var h = New(null, seed, from, forward, true);
                h.DebugHoldLook(head);
                var p = from + Quaternion.AngleAxis(bearing, Vector3.up) * forward * metres;
                Put(p);
                h.Tick(Dt, p, p + up, Vector3.forward);
                return h.SeesPlayer;
            }
            bool SeesEye(int seed, float degrees)
            {
                var h = New(null, seed, spot, fwd, true);
                h.DebugHoldLook(0f);
                var p = spot + fwd * .9f;
                Put(p);
                h.Tick(Dt, p, p + Vector3.up * (ModuleUnits.RelayEye + Mathf.Tan(degrees * Mathf.Deg2Rad) * .9f), Vector3.forward);
                return h.SeesPlayer;
            }
            var edge = Sees(313, 0f, 59f, r, spot, fwd) && Sees(315, 0f, -59f, r, spot, fwd) && !Sees(317, 0f, 61f, r, spot, fwd) && !Sees(319, 0f, -61f, r, spot, fwd);
            var headOk = Sees(321, 70f, 129f, r, spot, fwd) && !Sees(323, 70f, 131f, r, spot, fwd) && Sees(325, -70f, -129f, r, spot, fwd) && !Sees(327, -70f, -131f, r, spot, fwd);
            var rear = !Sees(329, 70f, 180f, r, spot, fwd) && !Sees(331, -70f, 180f, r, spot, fwd);
            var vertical = SeesEye(333, 39f) && !SeesEye(335, 41f) && SeesEye(337, -39f) && !SeesEye(339, -41f);
            var range = Sees(341, 0f, 0f, 10.9f, line0, dir) && (lineMetres < 12.5f || (Sees(343, 0f, 0f, 11.9f, line0, dir) && !Sees(345, 0f, 0f, 12.1f, line0, dir)));
            Check(edge && headOk && rear && vertical && range,
                label + "the calm cone: 59 deg in, 61 out, either side (" + edge + "); round the head, 129 in, 131 out with the head at 70 (" + headOk + "); the rear blind (" + rear + "); 39 deg in, 41 out, up and down (" + vertical + "); 11.9 m in, 12.1 m out (" + range + ")");
        }

        // 4. The spotting meter.
        float SpotTime(int seed, float metres, float mul, bool moving, out float eyes)
        {
            var h = New(null, seed, line0, dir, true);
            h.DebugHoldLook(0f);
            h.TierV2 = new FrontRoomsTierV2 { spotMul = mul };
            var feet = line0 + dir * metres;
            Put(feet);
            eyes = Vector3.Distance(line0 + Vector3.up * ModuleUnits.RelayEye, feet + up);
            var t = 0f;
            var at = -1f;
            h.TargetAcquired += p => { if (at < 0f) at = t; };
            for (var i = 0; i < 180 && at < 0f; i++)
            {
                t += Dt;
                Step(h, feet, moving);
            }
            return at;
        }
        {
            var t9 = SpotTime(347, 9f, 1f, true, out var e9);
            var t9b = SpotTime(349, 9f, .8f, true, out _);
            var t4 = SpotTime(351, 4f, 1f, true, out var e4);
            var ok = Mathf.Abs(t9 - FrontRoomsMapHunter.SpotSeconds(e9, 1f)) <= 1.5f * Dt && Mathf.Abs(t9b - FrontRoomsMapHunter.SpotSeconds(e9, .8f)) <= 1.5f * Dt && Mathf.Abs(t4 - FrontRoomsMapHunter.SpotSeconds(e4, 1f)) <= 1.5f * Dt;
            Check(ok, label + "T(d): lock-on at 9 m at T1 " + t9.ToString("0.000", inv) + " s (T " + FrontRoomsMapHunter.SpotSeconds(e9, 1f).ToString("0.000", inv) + "), T5 " + t9b.ToString("0.000", inv) + " s, at 4 m " + t4.ToString("0.000", inv) + " s");

            // Freeze: half rate once still 0.5 s, past 6 m.
            var still9 = SpotTime(353, 9f, 1f, false, out _);
            var still4 = SpotTime(355, 4f, 1f, false, out _);
            var whole = FrontRoomsMapHunter.SpotSeconds(e9, 1f);
            var want = FrontRoomsMapHunter.FreezeStillSeconds + (1f - FrontRoomsMapHunter.FreezeStillSeconds / whole) * whole / FrontRoomsMapHunter.FreezeScale;
            Check(Mathf.Abs(still9 - want) <= 3f * Dt && Mathf.Abs(still4 - t4) <= 1.5f * Dt,
                label + "freeze: a still player 9 m off takes " + still9.ToString("0.000", inv) + " s (half rate from 0.5 s: " + want.ToString("0.000", inv) + "; moving " + t9.ToString("0.000", inv) + "); 4 m off the freeze does nothing (" + still4.ToString("0.000", inv) + " s)");

            // Drain: nothing for 0.3 s, then 0.5/s; under 0.5 no look.
            var tuning = new FrontRoomsHunterTuning { sightRange = 12f };
            var feet = line0 + dir * 9f;
            Put(feet);
            var h = New(tuning, 357, line0, dir, false);
            h.DebugHoldLook(0f);
            for (var i = 0; i < 60 && h.Spotting < .3f; i++) Step(h, feet, true);
            var from = h.Spotting;
            tuning.sightRange = 0f;
            Blind(h, feet, .27f);
            var kept = h.Spotting;
            Blind(h, feet, .43f);
            var drained = h.Spotting;
            var wantDrain = from - FrontRoomsMapHunter.SpotDrainPerSecond * .4f;
            Check(from >= .3f && NearV4a(kept, from) && Mathf.Abs(drained - wantDrain) <= .025f,
                label + "the meter drains 0.5/s after 0.3 s: " + from.ToString("0.000", inv) + ", at 0.27 s " + kept.ToString("0.000", inv) + ", at 0.7 s " + drained.ToString("0.000", inv) + " (want " + wantDrain.ToString("0.000", inv) + ")");

            // FADE: twice the time, lock-on only within 6 m.
            var fadeTuning = new FrontRoomsHunterTuning();
            var f5 = New(fadeTuning, 359, line0, dir, true);
            f5.DebugHoldLook(0f);
            f5.Fade(false);
            var at5 = line0 + dir * 5f;
            Put(at5);
            var tf = 0f;
            for (var i = 0; i < 180 && !f5.LockedOn; i++)
            {
                tf += Dt;
                Step(f5, at5, true);
            }
            var fe = Vector3.Distance(line0 + Vector3.up * ModuleUnits.RelayEye, at5 + up);
            var f9 = New(fadeTuning, 361, line0, dir, true);
            f9.DebugHoldLook(0f);
            f9.Fade(false);
            var at9 = line0 + dir * 9f;
            Put(at9);
            var meterTop = 0f;
            for (var i = 0; i < 120; i++)
            {
                Step(f9, at9, true);
                meterTop = Mathf.Max(meterTop, f9.Spotting);
            }
            Check(f5.LockedOn && Mathf.Abs(tf - 2f * FrontRoomsMapHunter.SpotSeconds(fe, 1f)) <= 1.5f * Dt && !f9.LockedOn && meterTop <= FrontRoomsMapHunter.GateLevel + 1e-4f && f9.GateHolds == 0,
                label + "FADE: twice the time at 5 m (" + tf.ToString("0.000", inv) + " s), and at 9 m no lock-on: the meter stops at " + meterTop.ToString("0.00", inv) + ", no gate hold logged (" + f9.GateHolds + ")");
        }

        // 5. "Almost": it stops, one AlmostSeen, a cautious look on the drain.
        {
            var tuning = new FrontRoomsHunterTuning { sightRange = 12f };
            var feet = line0 + dir * 10.5f;
            Put(feet);
            var h = New(tuning, 363, line0, dir, false);
            h.DebugHoldLook(0f);
            var almost = 0;
            h.AlmostSeen += p => almost++;
            h.Noise(line0 + dir * 7f, 1000f);
            var huntState = h.State;
            var walked = false;
            for (var i = 0; i < 120 && h.Spotting < FrontRoomsMapHunter.AlmostLevel; i++)
            {
                Step(h, feet, true);
                walked |= h.Moving;
            }
            var stopped = h.Spotting >= FrontRoomsMapHunter.AlmostLevel;
            Step(h, feet, true);
            var stopAt = h.Position;
            var stops = h.AlmostStopped && !h.Moving && h.State == HunterState.Hunt;
            for (var i = 0; i < 6; i++) Step(h, feet, true);
            stops &= !h.Moving && (h.Position - stopAt).sqrMagnitude < 1e-6f && h.AlmostStopped;
            var once = almost == 1;
            var glimpse = h.LastKnownPoint;
            tuning.sightRange = 0f;
            var looked = -1f;
            var t = 0f;
            for (var i = 0; i < 90 && looked < 0f; i++)
            {
                t += Dt;
                Step(h, feet);
                if (h.Moving) looked = t;
            }
            var wentOn = false;
            var searchCautious = false;
            var searchSpots = -1;
            for (var i = 0; i < 600 && !wentOn; i++)
            {
                Step(h, feet);
                if (h.State != HunterState.Search) continue;
                wentOn = true;
                searchCautious = h.SearchCautious;
                searchSpots = h.SearchSpots.Count;
            }
            Check(huntState == HunterState.Hunt && walked && stopped && stops && once && looked >= FrontRoomsMapHunter.SpotDrainHoldSeconds - 2f * Dt && looked <= FrontRoomsMapHunter.SpotDrainHoldSeconds + 3f * Dt && wentOn && searchCautious && searchSpots == 1 && almost == 1 && Flat(h.Position - glimpse) <= 1.5f,
                label + "\"almost\": walking a hunt, at the meter's 0.5 it stops (" + stops + ", still Hunt) and AlmostSeen fires once (" + almost + "x); sight lost, the meter drains from " + FrontRoomsMapHunter.AlmostLevel.ToString("0.0", inv)
                + " or more and at " + looked.ToString("0.00", inv) + " s it walks on, a cautious look at the glimpse point (Search, cautious " + searchCautious + ", " + searchSpots + " spot, " + Flat(h.Position - glimpse).ToString("0.0", inv) + " m from it)");
        }

        // 6. The G3 gate: stage 2 at once here (W under 18 m), so the 3 s run is what it waits for.
        {
            var feet = line0 + dir * 9f;
            Put(feet);
            var h = New(null, 365, line0, dir, false);
            var lines = new List<string>();
            h.GateHoldLogged += line => lines.Add(line);
            var top = 0f;
            var t = 0f;
            var acquiredAt = -1f;
            h.TargetAcquired += p => acquiredAt = t;
            var lookAt = -1f;
            var from = h.Position;
            for (var i = 0; i < 420 && acquiredAt < 0f; i++)
            {
                t += Dt;
                Step(h, feet, true);
                if (acquiredAt < 0f && h.WarnRunSeconds < FrontRoomsMapHunter.GateStage1Seconds - .02f) top = Mathf.Max(top, h.Spotting);
                if (lookAt < 0f && h.Moving && h.Spotting >= FrontRoomsMapHunter.GateLevel - .01f) lookAt = t;
            }
            var walked = Flat(h.Position - from);
            var ok = acquiredAt >= FrontRoomsMapHunter.GateStage1Seconds - 3f * Dt && top <= FrontRoomsMapHunter.GateLevel + 1e-4f && h.LastLockOnStage1Seconds >= FrontRoomsMapHunter.GateStage1Seconds - 1e-3f
                && h.LastLockOnStage2Seconds >= FrontRoomsMapHunter.GateStage2Seconds - 1e-3f && h.GateHolds == 1 && lines.Count == 1 && h.LastGateHoldSeconds > FrontRoomsMapHunter.GateHoldSeconds;
            var looks = lookAt > 0f && lookAt >= FrontRoomsMapHunter.GateHoldSeconds && walked > .5f;
            Check(ok, label + "the G3 gate: the meter stays at " + top.ToString("0.00", inv) + " or less until stage 1 or more has run 3 s and stage 2 1 s: lock-on at " + acquiredAt.ToString("0.00", inv) + " s (runs " + h.LastLockOnStage1Seconds.ToString("0.00", inv) + " / "
                + h.LastLockOnStage2Seconds.ToString("0.00", inv) + " s, stage " + h.LastLockOnStage + "); one hold logged (" + h.GateHolds + ", " + h.LastGateHoldSeconds.ToString("0.00", inv) + " s): " + (lines.Count > 0 ? lines[0] : "none"));
            Check(looks, label + "held at the gate over 1.5 s it drops the stare and takes a cautious look: walking at " + lookAt.ToString("0.00", inv) + " s, " + walked.ToString("0.0", inv) + " m closer by the lock-on");

            // A hold that ends with sight lost is logged too. Touch is exempt (the touch row); a first lock-on within 6 m is gated as any.
            var tuning = new FrontRoomsHunterTuning { sightRange = 12f };
            var near = line0 + dir * 4f;
            Put(near);
            var close = New(tuning, 367, line0, dir, false);
            var closeLines = new List<string>();
            close.GateHoldLogged += line => closeLines.Add(line);
            var closeTop = 0f;
            for (var i = 0; i < 84; i++)
            {
                Step(close, near, true);
                closeTop = Mathf.Max(closeTop, close.Spotting);
            }
            var closeHeld = !close.LockedOn && close.TargetAcquisitions == 0 && closeTop <= FrontRoomsMapHunter.GateLevel + 1e-4f && closeTop >= FrontRoomsMapHunter.GateLevel - .01f;
            tuning.sightRange = 0f;
            Blind(close, near, .1f);
            Check(closeHeld && close.GateHolds == 1 && closeLines.Count == 1 && closeLines[0].Contains("sight lost"),
                label + "C37 leaves the gate alone: a first lock-on 4 m off is held at " + closeTop.ToString("0.00", inv) + " with stage runs short (" + closeHeld + "); the hold's line when sight goes: " + (closeLines.Count > 0 ? closeLines[0] : "none"));
        }

        // 7. Touch.
        {
            bool Touched(int seed, FrontRoomsHunterTuning t, Vector3 at, Vector3 facing, Vector3 feet, float seconds, out float meter, out int acquired)
            {
                var h = New(t, seed, at, facing, false);
                h.DebugHoldLook(0f);
                Put(feet);
                var count = 0;
                h.TargetAcquired += p => count++;
                var touching = false;
                meter = 0f;
                for (var s = 0f; s < seconds - 1e-4f; s += Dt)
                {
                    Step(h, feet);
                    touching |= h.TouchingPlayer;
                    meter = Mathf.Max(meter, h.Spotting);
                }
                acquired = count;
                return touching;
            }
            var behind = spot - fwd * 1.2f;
            var h1 = New(null, 369, spot, fwd, false);
            h1.DebugHoldLook(0f);
            Put(behind);
            var times = 0f;
            var acquiredAt = -1f;
            var stage1 = -1f;
            h1.TargetAcquired += p =>
            {
                acquiredAt = times;
                stage1 = h1.WarnRunSeconds;
            };
            var meter6 = 0f;
            for (var i = 0; i < 40 && acquiredAt < 0f; i++)
            {
                times += Dt;
                Step(h1, behind);
                if (i == 5) meter6 = h1.Spotting;
            }
            var fills = NearV4a(meter6, 6f * Dt * FrontRoomsMapHunter.TouchFillPerSecond) && acquiredAt > 0f && Mathf.Abs(acquiredAt - .25f) <= 1.5f * Dt && !h1.SeesPlayer && h1.LastLockOnTouch && stage1 < FrontRoomsMapHunter.GateStage1Seconds
                && h1.LastLockOnLongHold && NearV4a(h1.LastLockOnHoldSeconds, 1.7f);
            Check(fills, label + "touch: 1.2 m behind it, out of the cone (sees " + h1.SeesPlayer + "), it fills at 4/s (" + meter6.ToString("0.000", inv) + " in 6 ticks), lock-on at " + acquiredAt.ToString("0.00", inv) + " s, exempt from the gate (stage run "
                + stage1.ToString("0.00", inv) + " s), and a touch lock-on counts for C37 (long " + h1.LastLockOnLongHold + ", " + h1.LastLockOnHoldSeconds.ToString("0.00", inv) + " s, touch " + h1.LastLockOnTouch + ")");
            var in14 = Touched(371, null, spot, fwd, spot - fwd * 1.4f, .1f, out _, out _);
            var out16 = Touched(373, null, spot, fwd, spot - fwd * 1.6f, .5f, out var m16, out var n16);
            var blindOff = Touched(375, new FrontRoomsHunterTuning { sightRange = 0f }, spot, fwd, spot - fwd * 1.2f, .5f, out var mBlind, out var nBlind);
            var wall = new GameObject("MAP INTERACTION TEST / relay v4b wall") { hideFlags = HideFlags.DontSave };
            roots.Add(wall);
            wall.transform.position = spot - fwd * .6f + Vector3.up;
            wall.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            wall.AddComponent<BoxCollider>().size = new Vector3(1.6f, 2f, .1f);
            Physics.SyncTransforms();
            var walled = Touched(377, null, spot, fwd, spot - fwd * 1.2f, .5f, out var mWall, out var nWall);
            UnityEngine.Object.DestroyImmediate(wall);
            Physics.SyncTransforms();
            Check(in14 && !out16 && m16 == 0f && n16 == 0 && !blindOff && mBlind == 0f && nBlind == 0 && !walled && mWall == 0f && nWall == 0,
                label + "touch: 1.4 m " + in14 + ", 1.6 m " + out16 + "; off at sightRange 0 (" + blindOff + "); a capsule's way blocked in the same cell " + walled + " (meter " + mWall.ToString("0.00", inv) + ")");

            // Across a door: shut and Ajar never, Open yes.
            var near = mouth - across * .5f;
            var far = mouth + across * .5f;
            ShutDoorV4a(world, door);
            var shut = Touched(379, null, near, -across, far, 3f, out var mShut, out var nShut);
            ShutDoorV4a(world, door);
            var opened = world.OpenAjar(door, near);
            for (var k = 0; k < 90; k++) world.TickDoorsForTools(Dt);
            Physics.SyncTransforms();
            var ajarPassage = world.PassageBetween(a, b);
            var ajar = Touched(381, null, near, -across, far, 2f, out var mAjar, out var nAjar);
            OpenDoorV4a(world, door, a, b);
            var open = Touched(383, null, near, -across, far, .3f, out _, out var nOpen);
            ShutDoorV4a(world, door);
            Check(!shut && mShut == 0f && nShut == 0, label + "the player behind a shut door, the Relay 1.0 m off: no touch, no TargetAcquired in 3 s (meter " + mShut.ToString("0.00", inv) + ", " + nShut + " lock-ons)");
            Check(opened && ajarPassage == FrontRoomsMapWorld.Passage.Ajar && !ajar && mAjar == 0f && nAjar == 0 && open && nOpen == 1,
                label + "touch through an Ajar door: none (" + ajarPassage + ", meter " + mAjar.ToString("0.00", inv) + ", " + nAjar + " lock-ons); the same door Open: touch, lock-on (" + nOpen + ")");
        }

        // 8. The lock-on: TargetAcquired, the committed hold, StateChanged(Chase), the ramp. C37.
        {
            void Lock(int seed, float metres, out float hold, out FrontRoomsMapHunter hunter, out bool stood, out bool reported)
            {
                var feet = line0 + dir * metres;
                Put(feet);
                var h = New(null, seed, line0, dir, true);
                h.DebugHoldLook(0f);
                var t = 0f;
                var acquiredAt = -1f;
                var chaseAt = -1f;
                var chased = 0;
                h.TargetAcquired += p => acquiredAt = t;
                h.StateChanged += s => { if (s == HunterState.Chase && chaseAt < 0f) chaseAt = t; };
                h.ChaseStarted += m => chased++;
                var state = HunterState.Dormant;
                var still = true;
                var held = true;
                var at = h.Position;
                for (var i = 0; i < 300 && chaseAt < 0f; i++)
                {
                    t += Dt;
                    Step(h, feet, true);
                    if (acquiredAt < 0f)
                    {
                        state = h.State;
                        at = h.Position;
                        continue;
                    }
                    if (chaseAt >= 0f) break;
                    held &= h.State == state && h.LockedOn && h.LockingOn;
                    still &= (h.Position - at).sqrMagnitude < 1e-6f && !h.Moving;
                }
                hold = chaseAt - acquiredAt;
                hunter = h;
                stood = still && chased == 1;
                reported = held && h.State == HunterState.Chase;
            }
            Lock(385, 4f, out var hold4, out var h4, out var stood4, out var reported4);
            Check(Mathf.Abs(hold4 - 1.7f) <= 1.5f * Dt && NearV4a(h4.LastLockOnHoldSeconds, 1.7f) && h4.LastLockOnLongHold && h4.LongLockHolds == 1 && !h4.LastLockOnReacquired && Mathf.Abs(h4.LastLockOnMetres - 4f) < .05f && stood4 && reported4,
                label + "C37: a first lock-on 4 m off holds " + hold4.ToString("0.000", inv) + " s (LastLockOnHoldSeconds " + h4.LastLockOnHoldSeconds.ToString("0.00", inv) + ", long " + h4.LastLockOnLongHold + ", " + h4.LongLockHolds + " this run), standing (" + stood4
                + "), reporting the state it began in, LockedOn and LockingOn (" + reported4 + "), then StateChanged(Chase)");
            Lock(387, 9f, out var hold9, out var h9, out var stood9, out var reported9);
            Check(Mathf.Abs(hold9 - .7f) <= 1.5f * Dt && NearV4a(h9.LastLockOnHoldSeconds, .7f) && !h9.LastLockOnLongHold && h9.LongLockHolds == 0 && stood9 && reported9,
                label + "a first lock-on 9 m off holds " + hold9.ToString("0.000", inv) + " s, no long hold (" + h9.LongLockHolds + ")");
            Lock(389, 5.95f, out var hold595, out var h595, out _, out _);
            Lock(391, 6.05f, out var hold605, out var h605, out _, out _);
            Check(Mathf.Abs(hold595 - 1.7f) <= 1.5f * Dt && Mathf.Abs(hold605 - .7f) <= 1.5f * Dt && h595.LockOnMetres.Count == 1 && h595.LockOnHolds.Count == 1 && h605.LockOnHolds[0] == h605.LastLockOnHoldSeconds,
                label + "C37's edge: 5.95 m " + hold595.ToString("0.00", inv) + " s, 6.05 m " + hold605.ToString("0.00", inv) + " s (LockOnHold is pure: 6.0 m long, 6.1 m not, row 1)");
            var numbers = h4.ChaseNumbers;
            Check(numbers.StartsWith("1 lock-ons (1 long holds): 4.0 m 1.70 s; 1 chases from: 4.0 m") && h4.ChaseStartMetres.Count == 1 && Mathf.Abs(h4.LastChaseStartMetres - 4f) < .05f,
                label + "ChaseNumbers has the row's numbers: \"" + numbers + "\"");

            // Committed: sight gone at once, the hold runs out, the chase starts.
            var tuning = new FrontRoomsHunterTuning { sightRange = 12f };
            var feet2 = line0 + dir * 9f;
            var lost = New(tuning, 393, line0, dir, true);
            lost.DebugHoldLook(0f);
            Put(feet2);
            for (var i = 0; i < 90 && !lost.LockingOn; i++) Step(lost, feet2, true);
            var began = lost.LockingOn;
            tuning.sightRange = 0f;
            var left = 0f;
            for (var i = 0; i < 90 && lost.State != HunterState.Chase; i++)
            {
                left += Dt;
                Step(lost, feet2);
            }
            Check(began && lost.State == HunterState.Chase && left <= .7f + Dt && left >= .7f - 3f * Dt,
                label + "the hold is committed: sight lost as it began, " + lost.State + " " + left.ToString("0.00", inv) + " s on");

            // The ramp.
            var far = line0 + dir * (lineMetres - .5f);
            var ramp = New(null, 395, line0, dir, true);
            ToChase(ramp, far);
            var start = ramp.Position;
            for (var i = 0; i < 36; i++) Step(ramp, far);
            var afterRamp = Flat(ramp.Position - start);
            var at36 = ramp.Position;
            for (var i = 0; i < 30; i++) Step(ramp, far);
            var fullSpeed = Flat(ramp.Position - at36) * 2f;
            var v = ramp.ChaseSpeed;
            Check(Mathf.Abs(afterRamp - .5f * v * .6f) <= .25f && Mathf.Abs(fullSpeed - v) <= .25f,
                label + "the ramp: " + afterRamp.ToString("0.00", inv) + " m in 0.6 s (half of " + v.ToString("0.0", inv) + " m/s), then " + fullSpeed.ToString("0.00", inv) + " m/s");

            // No catch before the chase, in the hold or out of touch and sight.
            var close = spot + fwd * .3f;
            var ct = new FrontRoomsHunterTuning { sightRange = 12f };
            var catcher = New(ct, 397, spot, fwd, false);
            catcher.DebugHoldLook(0f);
            var caught = false;
            catcher.Caught += () => caught = true;
            Put(close);
            var holdAt = -1f;
            var tc = 0f;
            for (var i = 0; i < 180 && !caught; i++)
            {
                tc += Dt;
                Step(catcher, close);
                if (holdAt < 0f && catcher.LockingOn) holdAt = tc;
            }
            Check(holdAt > 0f && caught && Mathf.Abs(tc - holdAt - 1.7f) <= 2f * Dt,
                label + "no catch 0.3 m off before the chase: not at the first sight, not in the 1.7 s hold (it began at " + holdAt.ToString("0.00", inv) + " s, the catch came at " + tc.ToString("0.00", inv) + " s, with the chase; " + catcher.State + "), and the catch distance is tuning.catchDistance");
            var dt = new FrontRoomsHunterTuning { sightRange = 12f };
            var noCatch = New(dt, 399, spot, fwd, true);
            var missed = false;
            noCatch.Caught += () => missed = true;
            Put(spot + fwd * 2f);
            noCatch.DebugLockOn(spot + fwd * 2f);
            dt.sightRange = 0f;
            Put(close);
            for (var i = 0; i < 300 && noCatch.State != HunterState.Chase; i++) Step(noCatch, close);
            var chasing = noCatch.State == HunterState.Chase;
            for (var i = 0; i < 60 && noCatch.State == HunterState.Chase; i++) Step(noCatch, close);
            Check(chasing && !missed, label + "in a chase with neither sight nor touch (sightRange 0) it never catches a player 0.3 m off (" + !missed + ")");
        }

        // 9. Re-acquisition and "encounter" = one visit.
        {
            var tuning = new FrontRoomsHunterTuning();
            var h = new FrontRoomsMapHunter(world, tuning, body, null, 401);
            var forward = Vector3.forward;
            h.Arm();
            var eye = spawnFeet + up;
            var feet = spawnFeet;
            Put(feet);
            void Run(float seconds, Func<bool> until)
            {
                for (var s = 0f; s < seconds; s += Dt)
                {
                    h.Tick(Dt, feet, feet + up, forward);
                    world.TickDoorsForTools(Dt);
                    Physics.SyncTransforms();
                    if (until()) return;
                }
            }
            var placed = DispatchNear(world, h, spawnFeet, forward, out _);
            Run(10f, () => h.State != HunterState.Arrive);
            var near = h.Position + Vector3.right * 3f;
            var firstOk = placed && h.OnMap && h.DebugLockOn(near) && !h.LastLockOnReacquired && h.LastLockOnLongHold && NearV4a(h.LastLockOnHoldSeconds, 1.7f) && h.LongLockHolds == 1;
            Run(3f, () => h.State == HunterState.Chase);
            tuning.sightRange = 0f;
            Run(4f, () => h.State != HunterState.Chase);
            var lostOk = !h.LockedOn && h.Spotting == 0f;
            tuning.sightRange = 12f;
            near = h.Position + Vector3.right * 3f;
            var againOk = h.DebugLockOn(near) && h.LastLockOnReacquired && !h.LastLockOnLongHold && NearV4a(h.LastLockOnHoldSeconds, .35f) && h.LongLockHolds == 1 && h.TargetAcquisitions == 2;
            Check(firstOk && lostOk && againOk,
                label + "re-acquired in the same visit, 3 m off: 0.35 s, no long hold (" + h.LastLockOnHoldSeconds.ToString("0.00", inv) + " s, " + h.LongLockHolds + " long); the first was 1.7 s; the meter starts empty after a lost chase (" + lostOk + ")");
            h.SendAway(FrontRoomsRelayAwayReason.Test);
            var away = h.State == HunterState.Away && !h.LockedOn;
            var placed2 = DispatchNear(world, h, spawnFeet, forward, out _);
            Run(10f, () => h.State != HunterState.Arrive);
            near = h.Position + Vector3.right * 3f;
            var newVisit = placed2 && h.OnMap && h.DebugLockOn(near) && !h.LastLockOnReacquired && h.LastLockOnLongHold && NearV4a(h.LastLockOnHoldSeconds, 1.7f) && h.LongLockHolds == 2;
            Check(away && newVisit,
                label + "a new visit gets a new first lock-on: after the Away (" + away + ") and a dispatch, 3 m off holds " + h.LastLockOnHoldSeconds.ToString("0.00", inv) + " s (long " + h.LastLockOnLongHold + ", " + h.LongLockHolds + " this run)");

            // A handoff is the same encounter: on its way to a device, handed off, the next lock-on is a re-acquisition.
            var hh = new FrontRoomsMapHunter(world, tuning, body, null, 403);
            hh.Arm();
            void RunH(float seconds, Func<bool> until)
            {
                for (var s = 0f; s < seconds; s += Dt)
                {
                    hh.Tick(Dt, feet, feet + up, forward);
                    world.TickDoorsForTools(Dt);
                    Physics.SyncTransforms();
                    if (until()) return;
                }
            }
            var placedH = DispatchNear(world, hh, spawnFeet, forward, out _);
            RunH(10f, () => hh.State != HunterState.Arrive);
            hh.DebugLockOn(hh.Position + Vector3.right * 3f);
            RunH(3f, () => hh.State == HunterState.Chase);
            tuning.sightRange = 0f;
            RunH(4f, () => hh.State != HunterState.Chase);
            tuning.sightRange = 12f;
            var fromRelay = Walk(world, world.CellOf(hh.Position), 44);
            var fromPlayer = Walk(world, spawnCell, 40);
            var walkTo = fromRelay.Where(x => x.Value >= 2 && x.Value <= 4).OrderBy(x => x.Value).ThenBy(x => x.Key.x).ThenBy(x => x.Key.y).Select(x => x.Key).FirstOrDefault();
            hh.Redirect(new FrontRoomsRelayCall(world.CellCenter(walkTo), 0, FrontRoomsRelayCallCause.Device, true, false, 20f));
            var handed = false;
            foreach (var device in fromRelay.Where(x => x.Value >= 24 && fromPlayer.ContainsKey(x.Key) && fromPlayer[x.Key] >= 14 && !world.InStartArea(x.Key) && !world.InStairwell(x.Key)).OrderBy(x => x.Value).ThenBy(x => x.Key.x).ThenBy(x => x.Key.y).Select(x => x.Key).Take(30).ToList())
            {
                var call = new FrontRoomsRelayCall(world.CellCenter(device), 0, FrontRoomsRelayCallCause.Handoff, true, false, 20f);
                if (!hh.Handoff(call, feet, feet + up, forward)) continue;
                handed = true;
                break;
            }
            var arriving = handed && hh.State == HunterState.Arrive && !hh.LockedOn;
            RunH(10f, () => hh.State != HunterState.Arrive);
            var onward = hh.OnMap && hh.DebugLockOn(hh.Position + Vector3.right * 3f) && hh.LastLockOnReacquired && NearV4a(hh.LastLockOnHoldSeconds, .35f) && hh.LongLockHolds == 1;
            Check(placedH && arriving && onward,
                label + "a handoff does not start a new encounter: handed off (" + handed + ", " + hh.State + "), the next lock-on is a re-acquisition (" + hh.LastLockOnReacquired + ", " + hh.LastLockOnHoldSeconds.ToString("0.00", inv) + " s, " + hh.LongLockHolds + " long)");
            Put(awayFeet);
        }

        // 10. The chase: the cone, the lose meter, the 24 m exit, the speed.
        {
            bool ChaseSees(int seed, Vector3 from, Vector3 forward, float bearing, float metres)
            {
                var h = New(null, seed, from, forward, true);
                ToChase(h, from + forward * 2f);
                var p = from + Quaternion.AngleAxis(bearing, Vector3.up) * (new Vector3(h.Heading.x, 0f, h.Heading.z)).normalized * metres;
                Put(p);
                h.Tick(Dt, p, p + up, Vector3.forward);
                return h.SeesPlayer;
            }
            var cone = ChaseSees(405, spot, fwd, 109f, r) && ChaseSees(407, spot, fwd, -109f, r) && !ChaseSees(409, spot, fwd, 111f, r) && !ChaseSees(411, spot, fwd, -111f, r);
            var reach = lineMetres >= 15.5f ? ChaseSees(413, line0, dir, 0f, 14.9f) && !ChaseSees(415, line0, dir, 0f, 15.1f) : ChaseSees(413, line0, dir, 0f, lineMetres - .5f);
            Check(cone && reach, label + "chase sight: 109 deg in, 111 out of the body, either side (" + cone + "); to 15 m (14.9 in, 15.1 out, " + reach + ", on a clear line of " + lineMetres.ToString("0.#", inv) + " m)");

            float LoseAfter(int seed, Action<FrontRoomsMapHunter> before)
            {
                var tuning = new FrontRoomsHunterTuning { sightRange = 12f };
                var h = New(tuning, seed, line0, dir, true);
                var feet = line0 + dir * 8f;
                ToChase(h, feet);
                before?.Invoke(h);
                tuning.sightRange = 0f;
                var t = 0f;
                for (var i = 0; i < 300 && h.State == HunterState.Chase; i++)
                {
                    t += Dt;
                    Step(h, feet);
                }
                return t;
            }
            var plain = LoseAfter(417, null);
            var capped = LoseAfter(419, h => h.CapChase(1f));
            var hard = LoseAfter(421, h => h.Fade(true));
            Check(Mathf.Abs(plain - 2f) <= 2f * Dt && Mathf.Abs(capped - 1f) <= 2f * Dt && Mathf.Abs(hard - .5f) <= 2f * Dt,
                label + "the lose meter ends a chase at " + plain.ToString("0.00", inv) + " s, past chaseCap " + capped.ToString("0.00", inv) + " s, past the hard end " + hard.ToString("0.00", inv) + " s");
            var dTuning = new FrontRoomsHunterTuning { sightRange = 12f };
            var dh = New(dTuning, 423, line0, dir, true);
            var dFeet = line0 + dir * 10f;
            ToChase(dh, dFeet);
            dTuning.sightRange = 0f;
            for (var i = 0; i < 60; i++) Step(dh, dFeet);
            var filled = dh.LoseMeter;
            dTuning.sightRange = 12f;
            var seen = 0;
            for (var i = 0; i < 30; i++)
            {
                Step(dh, dFeet);
                if (dh.SeesPlayer) seen++;
            }
            var drained = dh.LoseMeter;
            dTuning.sightRange = 0f;
            var rest = 0f;
            for (var i = 0; i < 180 && dh.State == HunterState.Chase; i++)
            {
                rest += Dt;
                Step(dh, dFeet);
            }
            Check(Mathf.Abs(filled - 1f) <= 2f * Dt && seen == 30 && Mathf.Abs(drained - .75f) <= 2f * Dt && Mathf.Abs(rest - 1.25f) <= 3f * Dt,
                label + "the lose meter fills 1/s (" + filled.ToString("0.00", inv) + " in 1 s), drains 0.5/s with sight (" + drained.ToString("0.00", inv) + " after 0.5 s, " + seen + "/30 ticks seen), and a glimpse does not reset it: the rest takes " + rest.ToString("0.00", inv) + " s");

            // F over 24 m for 1 s unseen. Beyond the shut door, a cell where the door's cost is what puts F past 24 m.
            ShutDoorV4a(world, door);
            var fromA = new FrontRoomsMapPathField(90f);
            fromA.Build(world, c);
            var shutCost = FrontRoomsMapPathField.ShutOpening;
            var farCell = Walk(world, e, 30).Keys.Where(k => !world.InStartArea(k) && !world.InStairwell(k) && fromA.OpeningCostTo(k) == shutCost && fromA.DistanceTo(k) > 24.5f && fromA.DistanceTo(k) - shutCost < 23f)
                .OrderBy(k => k.x).ThenBy(k => k.y).FirstOrDefault();
            var hasFar = fromA.OpeningCostTo(farCell) == shutCost && fromA.DistanceTo(farCell) > 24.5f && fromA.DistanceTo(farCell) - shutCost < 23f;
            if (!hasFar) Check(false, label + "the 24 m exit: no cell beyond the shut door with F 24.5 m or more only by the door's cost");
            else
            {
                var feetFar = world.CellCenter(farCell);
                float Exit(int seed, bool licensed, out HunterState then, out float f)
                {
                    var tuning = new FrontRoomsHunterTuning { sightRange = 12f };
                    var h = New(tuning, seed, world.CellCenter(c), across, true);
                    ToChase(h, world.CellCenter(c) + across * 1f);
                    tuning.sightRange = 0f;
                    Put(feetFar);
                    if (licensed) h.Noise(door.position, 1000f);
                    Step(h, feetFar);
                    f = h.PathDistanceToPlayer;
                    var t = Dt;
                    for (var i = 0; i < 60 && h.State == HunterState.Chase; i++)
                    {
                        t += Dt;
                        Step(h, feetFar);
                    }
                    then = h.State;
                    return t;
                }
                var over = Exit(425, false, out var overState, out var overF);
                var licensedAt = Exit(427, true, out var licensedState, out var licensedF);
                Check(overF > 24f && Mathf.Abs(over - 1f) <= .15f && (licensedState == HunterState.Chase || licensedState == HunterState.BreakDoor),
                    label + "F over 24 m for 1 s unseen ends a chase (F " + overF.ToString("0.0", inv) + ", " + overState + " at " + over.ToString("0.00", inv) + " s, before the lose meter's 2.0 s); the same door licensed counts 0 (F " + licensedF.ToString("0.0", inv) + " less its door: still "
                    + licensedState + " at " + licensedAt.ToString("0.00", inv) + " s)");
            }
            Put(awayFeet);
            ShutDoorV4a(world, door);
        }

        // 11. A lock-on during BreakDoor abandons the break; the blows stay kept.
        {
            ShutDoorV4a(world, door);
            var relayC = world.CellCenter(c);
            var h = New(null, 429, relayC, across, false);
            var run = new DoorRunV3b(h);
            h.DebugHoldLook(0f);
            Put(awayFeet);
            h.Noise(world.CellCenter(e), 1000f);
            run.Run(world, awayFeet, Vector3.forward, 8f, () => h.State == HunterState.BreakDoor && run.blows >= 2);
            var breaking = h.State == HunterState.BreakDoor && run.blows >= 2;
            var blows = run.blows;
            var beside = h.Position - across * 1f;
            Put(beside);
            run.Run(world, beside, across, .6f, () => h.LockedOn);
            var lockedAt = h.LockedOn && h.LockingOn;
            var state = h.State;
            var kept = world.BlowsKept(door);
            run.Run(world, beside, across, 2.5f, () => h.State == HunterState.Chase);
            Check(breaking && lockedAt && state == HunterState.Hunt && !door.broken && !door.open && !world.IsBeingBroken(door) && kept == blows && h.State == HunterState.Chase && h.LastLockOnTouch,
                label + "a lock-on during BreakDoor abandons the break: " + blows + " blows struck, then " + state + " in the hold, the door shut and whole (broken " + door.broken + "), " + kept + " blows kept, then " + h.State);
            Put(awayFeet);
            ShutDoorV4a(world, door);
        }

        // 12. LoseTrack, from what it perceived, in order.
        {
            OpenDoorV4a(world, door, a, b);
            var near = mouth - across * 1f;
            var gone = awayFeet;
            Vector3? LostTo(int seed, Action<FrontRoomsMapHunter, FrontRoomsHunterTuning> setup)
            {
                var tuning = new FrontRoomsHunterTuning { sightRange = 12f };
                var h = New(tuning, seed, inA, across, true);
                ToChase(h, near);
                setup?.Invoke(h, tuning);
                Put(gone);
                var left = false;
                for (var i = 0; i < 900; i++)
                {
                    Step(h, gone);
                    if (!left && h.State != HunterState.Chase) left = true;
                    if (left && h.State == HunterState.Search) return h.Position;
                }
                return null;
            }
            // A noise heard since sight went, over the door it saw them take (setup: they crossed in sight).
            var noisePoint = world.CellCenter(c);
            Action<FrontRoomsMapHunter, FrontRoomsHunterTuning> crossed = (h, t) =>
            {
                var there = mouth + across * 1f;
                Put(near);
                Step(h, near);
                Put(there);
                Step(h, there);
                Step(h, there);
            };
            var toSeenDoor = LostTo(431, crossed);
            var toNoise = LostTo(433, (h, t) =>
            {
                crossed(h, t);
                t.sightRange = 0f;
                h.Noise(noisePoint, 1000f);
            });
            var toLastSeen = LostTo(435, null);
            var toHeardDoor = LostTo(437, (h, t) =>
            {
                t.sightRange = 0f;
                var field = typeof(FrontRoomsMapHunter).GetField("heardDoorV2", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field.SetValue(h, door);
            });
            Check(toNoise.HasValue && Flat(toNoise.Value - noisePoint) < 2f && toSeenDoor.HasValue && world.CellOf(toSeenDoor.Value) == b && toLastSeen.HasValue && Flat(toLastSeen.Value - near) < 1.5f && toHeardDoor.HasValue && world.CellOf(toHeardDoor.Value) == b,
                label + "LoseTrack's order: a noise heard since sight went first (search at " + (toNoise.HasValue ? world.CellOf(toNoise.Value).ToString() : "nowhere") + ", the noise's cell " + c + ", over a door it saw taken); a door heard move (" + (toHeardDoor.HasValue ? world.CellOf(toHeardDoor.Value).ToString() : "nowhere")
                + ", far side " + b + "); the door it saw taken in its last 1.0 s (" + (toSeenDoor.HasValue ? world.CellOf(toSeenDoor.Value).ToString() : "nowhere") + "); else the last-seen point (" + (toLastSeen.HasValue ? world.CellOf(toLastSeen.Value).ToString() : "nowhere") + ")");
            Put(awayFeet);
            ShutDoorV4a(world, door);
        }

        // 13. The full search and the cautious look.
        {
            // A chase on the line, the player walking on down it in sight, then gone: the search it begins.
            FrontRoomsMapHunter Searcher(int seed, FrontRoomsTierV2 tier, bool fade, out GridCoord around)
            {
                var tuning = new FrontRoomsHunterTuning { sightRange = 12f };
                var h = New(tuning, seed, line0, dir, true);
                h.TierV2 = tier;
                var feet = line0 + dir * 7f;
                ToChase(h, feet, true);
                if (fade) h.Fade(false);
                for (var i = 0; i < 12; i++)
                {
                    feet += dir * .05f;
                    Step(h, feet);
                }
                tuning.sightRange = 0f;
                for (var i = 0; i < 900 && h.State != HunterState.Search; i++) Step(h, feet);
                around = world.CellOf(h.LastKnownPoint);
                return h;
            }
            var rows = FrontRoomsTierRules.DefaultsV2();
            var t1 = Searcher(437, rows[0], false, out var around1);
            var t5 = Searcher(439, rows[4], false, out _);
            var fading = Searcher(441, rows[0], true, out _);
            Check(t1.State == HunterState.Search && !t1.SearchCautious && t1.SearchSpots.Count == 3 && t5.State == HunterState.Search && t5.SearchSpots.Count == 4 && fading.State == HunterState.Search && fading.SearchCautious && fading.SearchSpots.Count == 1,
                label + "after LoseTrack the full search: the tier's spots (T1 " + t1.SearchSpots.Count + ", T5 " + t5.SearchSpots.Count + ", cautious " + t1.SearchCautious + "); under FADE a cautious look (" + fading.SearchCautious + ", " + fading.SearchSpots.Count + " spot)");

            // Spots: within the shrinking radius, no two together, mostly hidden from the point and ahead; second best about 30 %.
            var depth = WalkV3b(world, around1, 8, false, around1, around1);
            var aroundEye = world.CellCenter(around1) + Vector3.up * ModuleUnits.RelayEye;
            var radiusBad = new List<string>();
            int passes = 0, hidden = 0, ahead = 0;
            var firsts = new Dictionary<GridCoord, int>();
            var runs = 30;
            var behindLater = 0;
            for (var seed = 0; seed < runs; seed++)
            {
                var h = Searcher(1000 + 2 * seed, rows[0], false, out var around);
                var list = h.SearchSpots;
                if (list.Count == 0) continue;
                firsts[list[0]] = firsts.TryGetValue(list[0], out var had) ? had + 1 : 1;
                var d0 = WalkV3b(world, around, 8, false, around, around);
                for (var i = 0; i < list.Count; i++)
                {
                    passes++;
                    var reach = Mathf.Max(2, 5 - i);
                    if (!d0.TryGetValue(list[i], out var steps) || steps > reach || steps < 1) radiusBad.Add(seed + ":" + i + " " + list[i] + " depth " + (d0.TryGetValue(list[i], out var got) ? got : -1));
                    for (var j = 0; j < i; j++)
                        if (CellsApartV4a(list[i], list[j]) <= 1) radiusBad.Add(seed + ": " + list[i] + " next to " + list[j]);
                    var eyeAt = world.CellCenter(list[i]) + Vector3.up * ModuleUnits.RelayEye;
                    var from = world.CellCenter(around) + Vector3.up * ModuleUnits.RelayEye;
                    if (Physics.Linecast(from, eyeAt, ~0, QueryTriggerInteraction.Ignore)) hidden++;
                    var off = world.CellCenter(list[i]) - world.CellCenter(around);
                    off.y = 0f;
                    var dot = Vector3.Dot(off.normalized, dir);
                    if (i == 0 && dot > 0f) ahead++;
                    if (i > 0 && dot < 0f) behindLater++;
                }
            }
            var most = firsts.Count > 0 ? firsts.Values.Max() : 0;
            var otherShare = runs > 0 ? 1f - most / (float)runs : 0f;
            Check(radiusBad.Count == 0 && passes >= 3 * runs - 3,
                label + "the spots stay inside the shrinking radius (5, 4, 3 cells of open walking) and never side by side, over " + runs + " seeds (" + passes + " spots)" + (radiusBad.Count > 0 ? ": " + string.Join("; ", radiusBad.Take(6).ToArray()) : ""));
            Check(hidden >= passes * 7 / 10 && ahead >= runs / 2 && otherShare >= .1f && otherShare <= .6f && behindLater > 0,
                label + "spots: " + hidden + " of " + passes + " hidden from the point, the first ahead of its last heading in " + ahead + "/" + runs + "; sub-optimal: another first spot in " + (otherShare * 100f).ToString("0", inv) + " % of seeds (second best 30 %), and a later pass turns back in "
                + behindLater + " spots (the one backtrack)");

            // The tier's seconds cut a full search short.
            var quick = Searcher(443, new FrontRoomsTierV2 { searchSeconds = 2f, searchSpots = 4 }, false, out _);
            var searchedFor = 0f;
            for (var i = 0; i < 600 && quick.State == HunterState.Search; i++)
            {
                searchedFor += Dt;
                Step(quick, awayFeet);
            }
            Check(quick.State != HunterState.Search && searchedFor <= 2.2f,
                label + "the tier's search seconds end it: " + searchedFor.ToString("0.00", inv) + " s at searchSeconds 2 (" + quick.State + ")");

            // Listens: 1.2 s per spot in the full search; the cautious look 1.5 s, one spot, 1.2 s.
            float[] Listens(int seed, bool full)
            {
                var blind = new FrontRoomsHunterTuning { sightRange = 0f };
                var h = New(blind, seed, spot, fwd, true);
                var listens = new List<float>();
                h.Noise(spot, 1000f);
                if (full) h.Noise(spot, 1000f);
                var standing = 0f;
                for (var i = 0; i < 1800; i++)
                {
                    var wasSearch = h.State == HunterState.Search;
                    Step(h, awayFeet);
                    if (h.State != HunterState.Search)
                    {
                        if (wasSearch)
                        {
                            // A full search can end on its walk, at the tier's seconds.
                            if (standing > .05f) listens.Add(standing);
                            break;
                        }
                        continue;
                    }
                    if (h.Moving)
                    {
                        if (standing > .05f) listens.Add(standing);
                        standing = 0f;
                    }
                    else standing += Dt;
                }
                return listens.ToArray();
            }
            var look = Listens(445, false);
            var fullListens = Listens(447, true);
            var lookOk = look.Length == 2 && Mathf.Abs(look[0] - FrontRoomsMapHunter.CautiousListenSeconds) <= 3f * Dt && Mathf.Abs(look[1] - FrontRoomsMapHunter.SpotListenSeconds) <= 3f * Dt;
            var fullOk = fullListens.Length >= 2 && fullListens.All(x => Mathf.Abs(x - FrontRoomsMapHunter.SpotListenSeconds) <= 4f * Dt);
            Check(lookOk && fullOk,
                label + "listens: the cautious look " + string.Join(", ", look.Select(x => x.ToString("0.00", inv)).ToArray()) + " s (1.5 s, one spot, 1.2 s); a full search " + string.Join(", ", fullListens.Select(x => x.ToString("0.00", inv)).ToArray()) + " s (1.2 s each)");
        }

        // 14. A42: a door's vision panel is opaque to the Relay.
        {
            ShutDoorV4a(world, door);
            var relayAt = mouth - across * 2.5f;
            var feet = mouth + across * 1.5f;
            Put(feet);
            var leaf = door.leaf;
            leaf.enabled = false;
            Physics.SyncTransforms();
            var fromEye = relayAt + Vector3.up * ModuleUnits.RelayEye;
            var toEye = feet + up;
            var rayClear = !Physics.Linecast(fromEye, Vector3.MoveTowards(toEye, fromEye, .5f), ~0, QueryTriggerInteraction.Ignore);
            var through = New(null, 449, relayAt, across, true);
            through.DebugHoldLook(0f);
            var count = 0;
            through.TargetAcquired += p => count++;
            var seen = 0;
            for (var i = 0; i < 90; i++)
            {
                Step(through, feet, true);
                if (through.SeesPlayer) seen++;
            }
            leaf.enabled = true;
            Physics.SyncTransforms();
            OpenDoorV4a(world, door, a, b);
            var control = New(null, 451, relayAt, across, true);
            control.DebugHoldLook(0f);
            Step(control, feet, true);
            var sawOpen = control.SeesPlayer;
            ShutDoorV4a(world, door);
            Check(rayClear && seen == 0 && count == 0 && through.Spotting == 0f && sawOpen,
                label + "A42: a shut leaf with its collider off (a ray clear of it, " + rayClear + ", as if the lite let it through): never seen (" + seen + " ticks), no meter, no lock-on (" + count + "); the door open the same line sees (" + sawOpen + ")");
            Put(awayFeet);
        }

        // 15. The Threat reading's source.
        {
            var tuning = new FrontRoomsHunterTuning { sightRange = 12f };
            var feet = line0 + dir * 9f;
            Put(feet);
            var h = New(tuning, 453, line0, dir, false);
            h.DebugHoldLook(0f);
            Step(h, feet, true);
            var glimpse = h.SeesPlayer && h.Spotting < FrontRoomsMapHunter.AlmostLevel && !h.Threat.Any && !h.ThreatPerceivable(null);
            for (var i = 0; i < 60 && h.Spotting < FrontRoomsMapHunter.AlmostLevel; i++) Step(h, feet, true);
            var spotted = h.Threat.Spotting && !h.Threat.TargetAcquiredRecently;
            h.GateOpenForTools = true;
            for (var i = 0; i < 60 && !h.LockedOn; i++) Step(h, feet, true);
            var locked = h.LockedOn && h.LockingOn && h.Threat.TargetAcquiredRecently && h.ThreatPerceivable(null) && h.State != HunterState.Chase;
            for (var i = 0; i < 90 && h.State != HunterState.Chase; i++) Step(h, feet, true);
            var inChase = h.State == HunterState.Chase && h.Threat.TargetAcquiredRecently && h.InChase;
            tuning.sightRange = 0f;
            for (var i = 0; i < 180 && h.State == HunterState.Chase; i++) Step(h, feet);
            Check(glimpse && spotted && locked && inChase && !h.LockedOn && !h.Threat.Any && h.ChaseFading,
                label + "Threat reads the real meter and lock-on: a glimpse is nothing (" + glimpse + "), the meter's 0.5 Spotting (" + spotted + "), TargetAcquired through the hold (" + locked + ") and the chase (" + inChase + "), none after LoseTrack (" + h.Threat + ", the chase's fade held " + h.ChaseFading + ")");
        }

        // 16. With the real director: +15 at the fill, a lock-on counted once its chase has lasted 5 s, the highest rate plus Chase.
        {
            var feet = spawnFeet;
            var h = new FrontRoomsMapHunter(world, new FrontRoomsHunterTuning(), body, null, 455);
            var d = new FrontRoomsRelayDirector(h);
            var forward = Vector3.forward;
            var t = 0f;
            void Beat()
            {
                t += Dt;
                d.Tick(Dt, 1, false, feet, feet + up, forward);
                h.Tick(Dt, feet, feet + up, forward);
                world.TickDoorsForTools(Dt);
                Physics.SyncTransforms();
            }
            Put(feet);
            d.Arm(false, false);
            var summonCell = Walk(world, spawnCell, 12).Where(x => x.Value >= 5 && x.Value <= 10).OrderBy(x => x.Value).ThenBy(x => x.Key.x).ThenBy(x => x.Key.y).Select(x => x.Key).FirstOrDefault();
            d.Summon(world.CellCenter(summonCell));
            while (t < 30f && !(h.OnMap && h.State != HunterState.Arrive)) Beat();
            var stepped = h.OnMap;
            // A slow chase, the player standing clear ahead of it: long enough for the 5 s count.
            h.TierV2 = new FrontRoomsTierV2 { chaseSpeed = .01f };
            var before = d.Policy.Pressure;
            var locks = d.LockOns;
            var toward = forward;
            for (var deg = 0; deg < 360; deg += 45)
            {
                var probe = Quaternion.AngleAxis(deg, Vector3.up) * forward;
                if (!LineClearV4b(h.Position, probe, 3.2f)) continue;
                toward = probe;
                break;
            }
            var there = h.Position + toward * 3f;
            feet = there;
            Put(feet);
            h.DebugLockOn(feet);
            var plus15 = d.Policy.Pressure >= before + 15f - 1e-3f;
            var notYet = d.LockOns == locks && d.Policy.LockOnsCounted == 0;
            for (var i = 0; i < 180 && h.State != HunterState.Chase; i++) Beat();
            Beat();
            var counted = d.LockOns == locks + 1 && d.Policy.LockOnsCounted == 0;
            var p0 = d.Policy.Pressure;
            for (var i = 0; i < 60; i++) Beat();
            var rate = d.Policy.Pressure - p0;
            var seen = h.SeesPlayer;
            var early = d.Policy.LockOnsCounted == 0;
            for (var i = 0; i < 245 && h.State == HunterState.Chase; i++) Beat();
            var five = d.Policy.LockOnsCounted == 1;
            Check(stepped && plus15 && notYet && counted && Mathf.Abs(rate - 6f) <= .2f && early && five,
                label + "the director on the real hunter: Pressure +15 at TargetAcquired (" + plus15 + "), the lock-on's clock from the chase (LockOns " + d.LockOns + "); in a chase with stage and sight the highest rate only: " + rate.ToString("0.0", inv) + " a second (sees " + seen + "); counted toward lockOnCap at 5 s of chase (" + early + ", " + five + ")");
            Put(awayFeet);
        }

        // 17. The late additions (systems design, 2026-10-30).
        {
            // One Withdraw clock, through a look, and past 20 s the look stops.
            var blind = new FrontRoomsHunterTuning { sightRange = 0f };
            var feet = spawnFeet;
            var forward = Vector3.forward;
            Put(awayFeet);
            var hw = new FrontRoomsMapHunter(world, blind, body, null, 457);
            var run = new DoorRunV3b(hw);
            hw.Arm();
            var placed = DispatchNear(world, hw, spawnFeet, forward, out _);
            run.Run(world, feet, forward, 45f, () => hw.State == HunterState.Withdraw);
            var withdrawing = placed && hw.State == HunterState.Withdraw;
            var entered = run.t;
            run.Run(world, feet, forward, .5f, null);
            var cell = world.CellOf(hw.Position);
            var nearCell = Walk(world, cell, 3).Where(p => p.Value >= 2).OrderBy(p => p.Value).ThenBy(p => p.Key.x).ThenBy(p => p.Key.y).Select(p => p.Key).FirstOrDefault();
            hw.Noise(world.CellCenter(nearCell), 1000f);
            var looked = hw.State == HunterState.Hunt;
            run.Run(world, feet, forward, 18f, () => hw.State == HunterState.Withdraw || hw.State == HunterState.Away);
            var backIn = hw.State == HunterState.Withdraw;
            run.Run(world, feet, forward, Dt, null);
            var elapsed = run.t - entered;
            var oneClock = backIn && Mathf.Abs(hw.WithdrawSeconds - elapsed) <= .1f && elapsed >= 2f;
            Check(withdrawing && looked && oneClock,
                label + "Withdraw keeps one clock: entered, a look, back in Withdraw " + elapsed.ToString("0.00", inv) + " s after the first entry, its clock " + hw.WithdrawSeconds.ToString("0.00", inv) + " s (" + hw.State + ")");

            var hl = new FrontRoomsMapHunter(world, blind, body, null, 459);
            var runL = new DoorRunV3b(hl);
            hl.Arm();
            var placedL = DispatchNear(world, hl, spawnFeet, forward, out _);
            runL.Run(world, feet, forward, 45f, () => hl.State == HunterState.Withdraw);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var clockNow = (float)FieldV4a(hl, "clock");
            var startedAt = clockNow - 19.6f;
            typeof(FrontRoomsMapHunter).GetField("withdrawAtV2", flags).SetValue(hl, startedAt);
            var cellL = world.CellOf(hl.Position);
            var nearL = Walk(world, cellL, 8).Where(p => p.Value >= 6).OrderBy(p => p.Value).ThenBy(p => p.Key.x).ThenBy(p => p.Key.y).Select(p => p.Key).FirstOrDefault();
            hl.Noise(world.CellCenter(nearL), 1000f);
            var lookedL = hl.State == HunterState.Hunt;
            runL.Run(world, feet, forward, 1f, () => hl.State != HunterState.Hunt);
            var stopped = hl.State == HunterState.Withdraw || hl.State == HunterState.Away;
            var stoppedAt = (float)FieldV4a(hl, "clock") - startedAt;
            runL.Run(world, feet, forward, .1f, () => hl.State == HunterState.Away);
            var terminal = hl.State == HunterState.Away || hl.WithdrawTerminal;
            runL.Run(world, feet, forward, 8f, () => hl.State == HunterState.Away);
            var awayAt = (float)FieldV4a(hl, "clock") - startedAt;
            Check(placedL && lookedL && stopped && terminal && stoppedAt >= FrontRoomsMapHunter.WithdrawTimeoutSeconds - Dt && stoppedAt <= FrontRoomsMapHunter.WithdrawTimeoutSeconds + 3f * Dt && hl.State == HunterState.Away && awayAt <= FrontRoomsMapHunter.WithdrawBackstopSeconds,
                label + "the look never buys time: past 20 s it stops where it stands (at " + stoppedAt.ToString("0.00", inv) + " s on the one clock, " + hl.State + ", terminal " + terminal + "), Away at " + awayAt.ToString("0.00", inv) + " s (25 s backstop, G6)");

            // A door it sees move counts as heard: in a chase with sight, and in the hold.
            OpenDoorV4a(world, door, a, b);
            var seenBeyond = inB;
            Put(seenBeyond);
            var hc = New(null, 461, inA, across, true);
            var rc = new DoorRunV3b(hc);
            ToChase(hc, seenBeyond);
            rc.Run(world, seenBeyond, -across, .2f, null);
            var withSight = hc.State == HunterState.Chase && hc.SeesPlayer;
            hc.Noise(door.position, 14f);
            var sawChase = hc.HasBreakLicence(door) && hc.LastHeard == FrontRoomsRelayHeard.Deaf;
            ShutUnheardV3b(world, door, hand);
            var keptChase = hc.State == HunterState.Chase;
            rc.Run(world, seenBeyond, -across, 10f, () => door.broken);
            var through = door.broken && rc.walled == 0 && rc.breakAt > 0f;
            Check(withSight && sawChase && keptChase && through,
                label + "a door it sees move counts as heard: in a chase with sight the player beyond it shuts it: BREAK licence " + sawChase + " (" + hc.LastHeard + "), still " + (keptChase ? "Chase" : "not Chase") + "; it breaks through, no WALL (" + rc.walled + ", broken " + door.broken + ")");
            OpenDoorV4a(world, door, a, b);
            Put(seenBeyond);
            var hh = New(null, 463, inA, across, true);
            var rh = new DoorRunV3b(hh);
            hh.DebugLockOn(seenBeyond);
            rh.Run(world, seenBeyond, -across, .1f, null);
            var holding = hh.LockingOn;
            hh.Noise(door.position, 14f);
            var sawHold = hh.HasBreakLicence(door);
            ShutUnheardV3b(world, door, hand);
            rh.Run(world, seenBeyond, -across, 10f, () => door.broken);
            Check(holding && sawHold && door.broken && rh.walled == 0,
                label + "and in the hold (" + holding + "): BREAK licence " + sawHold + ", the chase goes on through the door (broken " + door.broken + ", WALL " + rh.walled + ")");
            ShutDoorV4a(world, door);
            Put(awayFeet);

            // A dispatch's look comes down to one spot, unless a repeat upgrades it.
            var hd = new FrontRoomsMapHunter(world, blind, body, null, 465);
            var runD = new DoorRunV3b(hd);
            hd.Arm();
            var placedD = DispatchNear(world, hd, spawnFeet, forward, out _);
            runD.Run(world, feet, forward, 30f, () => hd.State == HunterState.Search);
            var dispatchLook = placedD && hd.State == HunterState.Search && hd.SearchCautious && hd.SearchSpots.Count == 1;
            var at = hd.Position;
            hd.Noise(at, 1000f);
            hd.Noise(at, 1000f);
            runD.Run(world, feet, forward, 5f, () => hd.State == HunterState.Search);
            var upgraded = hd.State == HunterState.Search && !hd.SearchCautious && hd.SearchSpots.Count == 3;
            Check(dispatchLook && upgraded,
                label + "a dispatch's look is one spot (" + dispatchLook + "); a repeat upgrades it to the full search (" + hd.SearchSpots.Count + " spots)");

            // The look on the way out counts toward lockOnCap.
            var hx = new FrontRoomsMapHunter(world, blind, body, null, 467);
            var dx = new FrontRoomsRelayDirector(hx);
            void Beat()
            {
                dx.Tick(Dt, 1, false, feet, feet + up, forward);
                hx.Tick(Dt, feet, feet + up, forward);
                world.TickDoorsForTools(Dt);
                Physics.SyncTransforms();
            }
            dx.Arm(false, false);
            var summonCell = Walk(world, spawnCell, 12).Where(x => x.Value >= 5 && x.Value <= 10).OrderBy(x => x.Value).ThenBy(x => x.Key.x).ThenBy(x => x.Key.y).Select(x => x.Key).FirstOrDefault();
            dx.Summon(world.CellCenter(summonCell));
            var ticks = 0;
            while (ticks++ < 3600 && hx.State != HunterState.Withdraw) Beat();
            var outWay = hx.State == HunterState.Withdraw;
            var countBefore = dx.Policy.LockOnsCounted;
            var cellX = world.CellOf(hx.Position);
            var nearX = Walk(world, cellX, 3).Where(p => p.Value >= 2).OrderBy(p => p.Value).ThenBy(p => p.Key.x).ThenBy(p => p.Key.y).Select(p => p.Key).FirstOrDefault();
            hx.Noise(world.CellCenter(nearX), 1000f);
            var lookedX = hx.State == HunterState.Hunt;
            Check(outWay && lookedX && dx.Policy.LockOnsCounted == countBefore + 1,
                label + "the look on the way out counts toward lockOnCap: LockOnsCounted " + countBefore + " -> " + dx.Policy.LockOnsCounted + " (" + hx.State + ", phase " + dx.Phase + ")");
            Put(awayFeet);
        }

        // 18. RELAY 8's worked estimate at tier 5, staged: the lock-on 9 m off, a 2 s bar, 0.4 s to react, one real corner within
        //     12 m (the way on hidden from the whole run up to it), the farthest such corner on the map.
        {
            var rows = FrontRoomsTierRules.DefaultsV2();
            var cellSize = MapGrid.CellSize;
            var route = new List<Vector3>();
            var relayAt = Vector3.zero;
            var runDir = Vector3.forward;
            var cornerAt = 0f;
            var eyeUp = Vector3.up * ModuleUnits.RelayEye;
            bool Open(GridCoord p, GridCoord q) => world.IsBuilt(q) && !world.InStartArea(q) && !world.InStairwell(q) && world.PassageBetween(p, q) == FrontRoomsMapWorld.Passage.Open;
            foreach (var cell in cells)
            {
                foreach (var s in Steps)
                {
                    var p0 = world.CellCenter(cell);
                    var d = world.CellCenter(cell + s) - p0;
                    d.y = 0f;
                    d.Normalize();
                    if (!LineClearV4b(p0, d, 9.5f)) continue;
                    var straight = new List<GridCoord>();
                    var last = cell;
                    for (var i = 1; i <= 7; i++)
                    {
                        var n = new GridCoord(cell.x + s.x * i, cell.y + s.y * i);
                        if (!Open(last, n) || !LineClearV4b(p0, d, Mathf.Min(i * cellSize + .5f, 21.5f))) break;
                        straight.Add(n);
                        last = n;
                    }
                    for (var k = straight.Count - 1; k >= 2; k--)
                    {
                        var corner = straight[k];
                        if (route.Count > 0 && (k + 1) * cellSize - 9f <= cornerAt) break;
                        foreach (var turn in new[] { new GridCoord(s.y, s.x), new GridCoord(-s.y, -s.x) })
                        {
                            var way = new List<GridCoord>();
                            var w = corner;
                            for (var i = 1; i <= 6; i++)
                            {
                                var n = new GridCoord(corner.x + turn.x * i, corner.y + turn.y * i);
                                if (!Open(w, n)) break;
                                way.Add(n);
                                w = n;
                            }
                            if (way.Count < 3) continue;
                            // A real corner: a wall all along the side toward the run, so the only way after the player is round it.
                            var hidden = true;
                            foreach (var cellOn in way)
                                hidden &= !world.IsBuilt(new GridCoord(cellOn.x - s.x, cellOn.y - s.y)) || world.PassageBetween(cellOn, new GridCoord(cellOn.x - s.x, cellOn.y - s.y)) == FrontRoomsMapWorld.Passage.Wall;
                            for (var m = 0f; m <= (k + 1) * cellSize - cellSize && hidden; m += cellSize)
                                for (var i = 0; i < way.Count && hidden; i++)
                                    hidden = Physics.Linecast(p0 + d * m + eyeUp, world.CellCenter(way[i]) + up, ~0, QueryTriggerInteraction.Ignore);
                            if (!hidden) continue;
                            relayAt = p0;
                            runDir = d;
                            cornerAt = (k + 1) * cellSize - 9f;
                            route.Clear();
                            route.Add(p0 + d * 9f);
                            route.Add(world.CellCenter(corner));
                            route.AddRange(way.Select(x => world.CellCenter(x)));
                            break;
                        }
                    }
                }
            }
            if (route.Count == 0) Check(false, label + "the worked estimate: no corner within 12 m of a 9 m run, its way on hidden from the run, near the spawn");
            else
            {
                var h = New(null, 469, relayAt, runDir, true);
                h.DebugHoldLook(0f);
                h.TierV2 = rows[4];
                Put(route[0]);
                var legs = 0;
                var pos = route[0];
                var t = 0f;
                var cueAt = -1f;
                var chaseAt = -1f;
                var gapAtChase = 0f;
                var gapAtLost = 0f;
                var lostAt = -1f;
                var caught = false;
                h.TargetAcquired += p => cueAt = t;
                h.Caught += () => caught = true;
                var closest = float.MaxValue;
                int chaseTicks = 0, seenTicks = 0;
                var topLose = 0f;
                var trace = new List<string>();
                for (var i = 0; i < 1200 && !caught && lostAt < 0f; i++)
                {
                    t += Dt;
                    if (cueAt >= 0f && t >= cueAt + .4f)
                    {
                        var left = (t < cueAt + 2.4f ? 5.5f : 3.2f) * Dt;
                        while (left > 0f && legs < route.Count - 1)
                        {
                            var to = route[legs + 1] - pos;
                            to.y = 0f;
                            if (to.magnitude <= left)
                            {
                                pos = route[legs + 1];
                                left -= to.magnitude;
                                legs++;
                            }
                            else
                            {
                                pos += to.normalized * left;
                                left = 0f;
                            }
                        }
                    }
                    Put(pos);
                    Step(h, pos);
                    if (chaseAt < 0f && h.State == HunterState.Chase)
                    {
                        chaseAt = t;
                        gapAtChase = Flat(h.Position - pos);
                    }
                    if (chaseAt >= 0f)
                    {
                        closest = Mathf.Min(closest, Flat(h.Position - pos));
                        chaseTicks++;
                        if (h.SeesPlayer) seenTicks++;
                        topLose = Mathf.Max(topLose, h.LoseMeter);
                        if (trace.Count < 40 && chaseTicks % 15 == 0) trace.Add((t - cueAt).ToString("0.00", inv) + ":" + Flat(h.Position - pos).ToString("0.0", inv) + (h.SeesPlayer ? "s" : "") + "/" + h.LoseMeter.ToString("0.0", inv) + "/" + h.State + "/" + world.CellOf(h.Position) + world.CellOf(pos));
                    }
                    if (chaseAt >= 0f && lostAt < 0f && h.State != HunterState.Chase && h.State != HunterState.BreakDoor)
                    {
                        lostAt = t;
                        gapAtLost = Flat(h.Position - pos);
                    }
                }
                Check(cueAt > 0f && chaseAt > 0f && !caught && lostAt > 0f,
                    label + "the worked estimate, T5: lock-on at 9 m, a corner " + cornerAt.ToString("0.#", inv) + " m on; the chase " + (chaseAt - cueAt).ToString("0.00", inv) + " s after the cue with the gap " + gapAtChase.ToString("0.0", inv) + " m (estimate 10.7), closest " + closest.ToString("0.0", inv)
                    + " m, lost " + (lostAt > 0f ? (lostAt - chaseAt).ToString("0.0", inv) : "never") + " s into the chase with the gap " + gapAtLost.ToString("0.0", inv) + " m, caught " + caught + "; in sight " + seenTicks + "/" + chaseTicks + " ticks, lose meter to " + topLose.ToString("0.00", inv)
                    + (cueAt > 0f && chaseAt > 0f && !caught && lostAt > 0f ? "" : "; trace " + string.Join(" ", trace.ToArray())));
            }
            Put(awayFeet);
        }
    }
#endif

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

#if FRONTROOMS_RELAY_V2
        // 1. Relay v2 (V3a): no timed release. Armed as at the start door's shut (Away: off the map, not released), then
        //    placed by tools to break the door.
        hunter.Arm();
        var firstRelease = hunter.Armed && hunter.State == HunterState.Away && !hunter.Released && !hunter.OnMap;
#else
        // 1. Released as in play (its release timer runs up to the delay), then sent to break the door.
        var firstRelease = false;
        for (var t = 0f; t < tuning.releaseDelaySeconds + 2f && !firstRelease; t += Dt)
        {
            hunter.Tick(Dt, target, target + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
            firstRelease = hunter.Released;
        }
#endif
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

#if FRONTROOMS_RELAY_V2
        // 3. Relay v2 (V3a): parked (Disarm) it hears nothing and stays Dormant however long (no timer); armed again it is
        //    Away, a call places it anew backstage (Arrive, no Arrived), and it is on the map at its step-in (Arrived once).
        hunter.Noise(target, 1000f);
        var deaf = !hunter.ListenPoint.HasValue && hunter.State == HunterState.Dormant;
        var staysDormant = true;
        for (var t = 0f; t < tuning.releaseDelaySeconds + 1f; t += Dt)
        {
            hunter.Tick(Dt, target, target + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
            world.TickDoorsForTools(Dt);
            staysDormant &= hunter.State == HunterState.Dormant && !hunter.Released;
        }
        hunter.Arm();
        var awayAgain = hunter.State == HunterState.Away && !hunter.Released;
        var placedAgain = DispatchNear(world, hunter, target, Vector3.forward, out _);
        var backstage = placedAgain && hunter.State == HunterState.Arrive && !hunter.Released && arrivals == arrivalsBefore;
        var steppedAt = -1f;
        for (var t = 0f; t < 15f && steppedAt < 0f; t += Dt)
        {
            hunter.Tick(Dt, target, target + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
            world.TickDoorsForTools(Dt);
            if (hunter.Released) steppedAt = t + Dt;
        }
        Check(deaf && staysDormant && awayAgain && backstage && steppedAt >= 0f && hunter.Relays == relaysBefore + 1 && arrivals == arrivalsBefore + 1 && !door.broken && !world.IsBeingBroken(door),
            label + "(v2) parked it hears no noise and stays Dormant for " + (tuning.releaseDelaySeconds + 1f).ToString("0.0") + " s (no timer: " + staysDormant + "); armed again it is Away ("
            + awayAgain + "), a call places it anew backstage (" + backstage + ", relays " + hunter.Relays + ") and it steps in " + steppedAt.ToString("0.00") + " s later (Arrived "
            + (arrivals - arrivalsBefore) + "x); the door never broken");
#else
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
#endif

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
#if FRONTROOMS_RELAY_V2
        // Relay v2 (V3a): on the new floor it is armed at the start door's shut (Away) and a call places it; on the map at its step-in.
        hunter.Arm();
        var back = DispatchNear(world, hunter, spawn, Vector3.forward, out _);
        for (var t = 0f; t < 15f && back && !hunter.Released; t += Dt)
            hunter.Tick(Dt, spawn, spawn + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
        back &= hunter.Released;
#else
        var back = false;
        for (var t = 0f; t < tuning.releaseDelaySeconds + 2f && !back; t += Dt)
        {
            hunter.Tick(Dt, spawn, spawn + Vector3.up * ModuleUnits.PlayerEye, Vector3.forward);
            back = hunter.Released;
        }
#endif
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
        Check(anchorOk && Mathf.Abs(lo.y - FrontRoomsMapWorld.ExitSignBottom) <= .005f && exitFrame != null && lo.y > headTop && lo.y - headTop < .03f && hi.y < ceiling - 1e-3f
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

        // 1. The marker kinds are appended (old bytes keep their values); the profile asset holds the station (the
        //    joint switch-on, 2026-10-29: Station_FrontDesk_4x4, 游戏视觉's station-look-v1, contract C-8), which passes
        //    every module rule and the station contract with the kit's footprints; its random pool holds none.
        Check((int)ModuleMarkerKind.KeySpot == 0 && (int)ModuleMarkerKind.RelayEntry == 1 && (int)ModuleMarkerKind.StationBeacon == 2 && (int)ModuleMarkerKind.Placard == 3
            && (int)ModuleMarkerKind.StationEntrance == 4,
            label + "marker kinds KeySpot 0, RelayEntry 1, then the appended StationBeacon 2, Placard 3, StationEntrance 4");
        var poolStations = modules.Count(m => m.IsStation);
        var station = profile.StationData();
        var stationErrors = new List<string>();
        if (station != null) station.Validate(stationErrors, new List<string>(), FrontRoomsMapWorld.KitFootprint);
        Check(profile.stationModule != null && profile.stationModule.name == "Station_FrontDesk_4x4" && station != null && station.IsStation
            && stationErrors.Count == 0 && poolStations == 0,
            label + "the profile " + AssetDatabase.GetAssetPath(profile) + " has the station module " + (profile.stationModule != null ? profile.stationModule.name : "(none)")
            + " (" + (station == null ? "no data" : stationErrors.Count == 0 ? "valid" : string.Join(" | ", stationErrors)) + ") and none of its " + modules.Count + " pool modules is a station");

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
        var takeOk = world.StationAnchor(ModuleMarkerKind.KeySpot, out var keyAt, out var keyTurn, out var keyCell);
        // C-1: the take point stands 0.5 m in front of the key along its facing, on the floor.
        var takeAt = keyAt + keyTurn * Vector3.forward * FrontRoomsMapWorld.StationTakeStandOff;
        var takePoint = new Vector3(takeAt.x, world.CellCenter(keyCell).y, takeAt.z);
        takeOk &= world.StationTakeSpot(out var takeSpot) && (takeSpot - takePoint).sqrMagnitude < 1e-6f;
        var seed = 100 + 10 * t;
        FrontRoomsMapHunter NewHunter(FrontRoomsHunterTuning tuning) => new FrontRoomsMapHunter(world, tuning ?? new FrontRoomsHunterTuning(), body, null, seed++);
        FrontRoomsMapHunter.FirstSendPolicy Policy(bool key, int empty)
        {
            var policy = FrontRoomsMapHunter.FirstSendPolicy.ForStation(world);
            policy.huntTo = key ? takePoint : (Vector3?)null;
            policy.emptyRungsForTools = empty;
            return policy;
        }
        // V3c, C32's W fix (systems design 2026-10-30): after every ladder send here (the station's and floor 2's generic),
        // rungs 1 and 2 offered only cells at W at most FirstSendMaxW (30 m), W as the warning stage reads it once the Relay
        // stands there (WarningDistanceAt, the player where the send was made); a rung 1-2 send stands at W at most 30 m;
        // report.w is WarningDistanceAt at the placement (rungs 1-3 stand at the cell's centre). One row at the end.
        int wSends = 0, wCells1 = 0, wCells2 = 0, wOut1 = 0, wOut2 = 0, wPlaced12 = 0;
        var wMax = 0f;
        var wBad = new List<string>();
        void WRule(FrontRoomsMapHunter hn, string where)
        {
            var rep = hn.LastFirstSend;
            wSends++;
            wOut1 += rep.rung1WOut;
            wOut2 += rep.rung2WOut;
            foreach (var c in hn.LastFirstSendRung1)
            {
                wCells1++;
                var w = hn.WarningDistanceAt(c, feet);
                if (!(w <= FrontRoomsMapHunter.FirstSendMaxW)) wBad.Add(where + ": rung 1 cell " + c + " at W " + w.ToString("0.0") + " m");
            }
            foreach (var c in hn.LastFirstSendRung2)
            {
                wCells2++;
                var w = hn.WarningDistanceAt(c, feet);
                wMax = Mathf.Max(wMax, w);
                if (!(w <= FrontRoomsMapHunter.FirstSendMaxW)) wBad.Add(where + ": rung 2 cell " + c + " at W " + w.ToString("0.0") + " m");
            }
            if (rep.rung >= 1 && rep.rung <= 2)
            {
                wPlaced12++;
                if (!(rep.w <= FrontRoomsMapHunter.FirstSendMaxW)) wBad.Add(where + ": rung " + rep.rung + " placed at W " + rep.w.ToString("0.0") + " m");
            }
            if (rep.rung >= 1 && rep.rung <= 3 && !(Mathf.Abs(rep.w - hn.WarningDistanceAt(rep.cell, feet)) <= 1e-3f))
                wBad.Add(where + ": report.w " + rep.w.ToString("0.00") + " m, WarningDistanceAt " + hn.WarningDistanceAt(rep.cell, feet).ToString("0.00") + " m at " + rep.cell);
        }

        // 0. The onboarding's gate (Red 2026-10-30, delay by exploration; the 3-argument StationSendDue the game asks): Explored at
        //    StationExploreCells (150) unique floor cells, else Fallback at StationSendSeconds (180) since the start door
        //    shut, else Waiting; Explored first when both hold; no station (or no world): Waiting whatever the counts. The
        //    key and the player's cell are no argument at all (the stream room cannot count: the game's floorExplored is
        //    the tier's cells, never the title's rooms, the station or X).
        var onbExplore = FrontRoomsMapHunter.StationExploreCells;
        var onbSeconds = FrontRoomsMapHunter.StationSendSeconds;
        var onb149 = FrontRoomsMapHunter.StationSendDue(world, 0f, onbExplore - 1);
        var onb150 = FrontRoomsMapHunter.StationSendDue(world, 0f, onbExplore);
        var onb1799 = FrontRoomsMapHunter.StationSendDue(world, onbSeconds - .1f, 0);
        var onb180 = FrontRoomsMapHunter.StationSendDue(world, onbSeconds, 0);
        var onbBoth = FrontRoomsMapHunter.StationSendDue(world, onbSeconds, onbExplore);
        var onbUnder = FrontRoomsMapHunter.StationSendDue(world, onbSeconds - .1f, onbExplore - 1);
        var onbNull = FrontRoomsMapHunter.StationSendDue(null, 1000f, 1000);
        var onbKeyBefore = world.StationKeyIsTaken;
        Check(onbExplore == 150 && Mathf.Approximately(onbSeconds, 180f) && Mathf.Approximately(FrontRoomsMapHunter.OnboardOffSendSeconds, 150f) && !FrontRoomsMapHunter.StationSendNeedsKey
                && (int)FrontRoomsMapHunter.StationGate.Explored == 4 && (int)FrontRoomsMapHunter.StationGate.Fallback == 2 && (int)FrontRoomsMapHunter.StationGate.Left == 1
                && onb149 == FrontRoomsMapHunter.StationGate.Waiting && onb150 == FrontRoomsMapHunter.StationGate.Explored
                && onb1799 == FrontRoomsMapHunter.StationGate.Waiting && onb180 == FrontRoomsMapHunter.StationGate.Fallback
                && onbBoth == FrontRoomsMapHunter.StationGate.Explored && onbUnder == FrontRoomsMapHunter.StationGate.Waiting && onbNull == FrontRoomsMapHunter.StationGate.Waiting && !onbKeyBefore,
            label + "the onboarding's gate (3-argument StationSendDue, StationExploreCells " + onbExplore + ", StationSendSeconds " + onbSeconds.ToString("0") + ", the key gates "
            + FrontRoomsMapHunter.StationSendNeedsKey + "): 149 cells " + onb149 + ", 150 cells " + onb150 + "; 179.9 s " + onb1799 + ", 180 s " + onb180 + "; both " + onbBoth
            + "; 149 cells at 179.9 s " + onbUnder + "; no world " + onbNull + " (key taken " + onbKeyBefore + ")");

        // 1. The tools' gate (-autopilotOnboardOff: M7's old gate, the 5-argument StationSendDue) at path distance 0, 1 and
        //    2, through E1 (E1's inside, C1, C2) and through E2 (E2's inside, E2o, a cell one step on through an Open edge),
        //    with and without the key, the key needed or not: Waiting, Waiting, then Left (Waiting without the key while it
        //    is needed); the game sends only when it is due, so the first two stay Dormant and the third is sent
        //    (ReleaseNow). OnboardOffSendSeconds (150 s): Fallback anywhere; 149.9 s: still Waiting.
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
                    if (ok) WRule(hunter, "gate " + cell);
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
        var fallback = FrontRoomsMapHunter.StationSendDue(world, l.e1In, FrontRoomsMapHunter.OnboardOffSendSeconds, false, true);
        var notYet = FrontRoomsMapHunter.StationSendDue(world, l.e1In, FrontRoomsMapHunter.OnboardOffSendSeconds - .1f, false, true);
        var fallbackHunter = NewHunter(null);
        var fallbackSent = fallback == FrontRoomsMapHunter.StationGate.Fallback && fallbackHunter.ReleaseNow(Policy(false, 0), feet, PlayerEye(), Vector3.forward);
        if (fallbackSent) WRule(fallbackHunter, "fallback");
        var fallbackProblem = fallbackSent ? SendProblem(fallbackHunter.LastFirstSend) : "not sent";
        Check(e2TwoFound && gateBad.Count == 0 && sent == 6 && sendBad.Count == 0 && notYet == FrontRoomsMapHunter.StationGate.Waiting && fallbackProblem == null && fallbackHunter.State == HunterState.Listen
                && world.InStartArea(streamRoom) && inStream == FrontRoomsMapHunter.StationGate.Waiting,
            label + "the tools' gate (-autopilotOnboardOff; path cells " + string.Join(", ", paths.ToArray()) + "): Dormant / Dormant / sent through E1 and E2 with and without the key (never sent without it while it is needed); "
            + sent + " sends: " + string.Join("; ", sends.ToArray()) + "; at 150 s from E1's inside " + fallback + " (149.9 s " + notYet + "), sent rung " + fallbackHunter.LastFirstSend.rung
            + " to " + fallbackHunter.LastFirstSend.cell + "; in the stream room behind D " + inStream + (gateBad.Count + sendBad.Count > 0 ? "; WRONG: " + string.Join("; ", gateBad.Concat(sendBad).Take(6).ToArray()) : "")
            + (fallbackProblem != null ? "; fallback " + fallbackProblem : ""));
        // A15's third-zone rung (V3a, systems design P5) is retired by the onboarding (Red 2026-10-30): the 6-argument
        // StationSendDue is the tools' gate whatever the zones, and never ThirdZone (with or without FRONTROOMS_RELAY_V2).
        var zoneThree = FrontRoomsMapHunter.StationSendDue(world, l.e1In, 0f, false, true, FrontRoomsMapHunter.StationThirdZone);
        var zoneMany = FrontRoomsMapHunter.StationSendDue(world, l.e1In, 0f, false, true, 99);
        var zoneFallback = FrontRoomsMapHunter.StationSendDue(world, l.e1In, FrontRoomsMapHunter.OnboardOffSendSeconds, false, true, FrontRoomsMapHunter.StationThirdZone);
        var zoneLeft = FrontRoomsMapHunter.StationSendDue(world, c2, 0f, true, true, FrontRoomsMapHunter.StationThirdZone);
        var zoneStream = FrontRoomsMapHunter.StationSendDue(world, streamRoom, 0f, false, true, FrontRoomsMapHunter.StationThirdZone);
        Check(zoneThree == FrontRoomsMapHunter.StationGate.Waiting && zoneMany == FrontRoomsMapHunter.StationGate.Waiting && zoneFallback == FrontRoomsMapHunter.StationGate.Fallback
                && zoneLeft == FrontRoomsMapHunter.StationGate.Left && zoneStream == FrontRoomsMapHunter.StationGate.Waiting,
            label + "A15's third-zone rung retired (6-argument StationSendDue = the tools' gate): from E1's inside without the key 3 zones " + zoneThree + ", 99 zones " + zoneMany
            + "; at 150 s with 3 zones " + zoneFallback + "; from C2 with the key and 3 zones " + zoneLeft + "; the stream room with 3 zones " + zoneStream);

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
            if (ok) WRule(hunter, "forced off " + empty);
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
                if (kind < 2)
                {
                    ok = hunter.ReleaseNow(Policy(false, kind == 0 ? 0 : 7), feet, PlayerEye(), Vector3.forward);
                    if (ok) WRule(hunter, (kind == 0 ? "ladder" : "arrive") + " from " + from);
                }
                else
                {
                    hunter.DebugPlace(far);
                    hunter.Tick(Dt, feet, PlayerEye(), Vector3.forward);
#if FRONTROOMS_RELAY_V2
                    // Relay v2 (V3a): no leash. Off the built map it goes Away (UnbuiltCell) at once, so it can never land in the
                    // station or its ring that way.
                    ok = hunter.State == HunterState.Away && hunter.LastAwayReason == FrontRoomsRelayAwayReason.UnbuiltCell;
#else
                    ok = world.IsBuilt(world.CellOf(hunter.Position));
#endif
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

#if FRONTROOMS_RELAY_V2
        // 4. V4b: ChaseGrace is retired (the G3 gate on every lock-on subsumes it). Facing the player 9 m off, in one tick and
        //    with or without the grace, a sighting starts no chase and is no Threat (the prompt stays); nor does the grace hold a
        //    lock-on: with it, within 6 m, DebugLockOn locks on, Threat TargetAcquiredRecently.
        {
            var toE1 = world.CellCenter(l.e1Out) - world.CellCenter(l.front);
            toE1.y = 0f;
            var graceHunter = NewHunter(null);
            graceHunter.DebugPlace(world.CellCenter(l.front), toE1);
            graceHunter.ChaseGrace = true;
            var plainHunter = NewHunter(null);
            plainHunter.DebugPlace(world.CellCenter(l.front), toE1);
            Stand(l.e1Out);
            graceHunter.Tick(Dt, feet, PlayerEye(), Vector3.forward);
            plainHunter.Tick(Dt, feet, PlayerEye(), Vector3.forward);
            var same = graceHunter.SeesPlayer == plainHunter.SeesPlayer && graceHunter.State == plainHunter.State && graceHunter.State != HunterState.Chase
                && NearV4a(graceHunter.Spotting, plainHunter.Spotting) && graceHunter.Spotting > 0f;
            var heldThreat = graceHunter.Threat;
            var heldHides = graceHunter.ThreatPerceivable(null);
            Check(same, label + "V4b: ChaseGrace is retired: seen 9 m off from N, with it " + graceHunter.State + " (meter " + graceHunter.Spotting.ToString("0.000") + "), without it " + plainHunter.State + " (meter "
                + plainHunter.Spotting.ToString("0.000") + ")");
            Stand(l.Corridor(3));
            var close = graceHunter.DebugLockOn(feet);
            var closeThreat = graceHunter.Threat;
            Check(!heldThreat.Any && !heldHides && close && closeThreat.TargetAcquiredRecently,
                label + "Threat (V4b's source): the glimpse " + heldThreat + " (prompt hidden " + heldHides + "); a lock-on within 6 m under the grace " + closeThreat);
        }
#else
        // 4. The grace: a Relay standing in N sees the player 9 m off in C1 and starts no chase while ChaseGrace holds;
        //    within 6 m (the player in C3) it chases; without the grace it chases from 9 m.
        var graceHunter = NewHunter(null);
        graceHunter.DebugPlace(world.CellCenter(l.front));
        graceHunter.ChaseGrace = true;
        Stand(l.e1Out);
        graceHunter.Tick(Dt, feet, PlayerEye(), Vector3.forward);
        var heldSees = graceHunter.SeesPlayer;
        var heldState = graceHunter.State;
        // V2 (RELAY 8, 2026-10-29): the Threat reading and the prompt gate on the held glimpse.
        var heldThreat = graceHunter.Threat;
        var heldHides = graceHunter.ThreatPerceivable(null);
        Stand(l.Corridor(3));
        graceHunter.Tick(Dt, feet, PlayerEye(), Vector3.forward);
        var closeState = graceHunter.State;
        var closeThreat = graceHunter.Threat;
        var plainHunter = NewHunter(null);
        plainHunter.DebugPlace(world.CellCenter(l.front));
        Stand(l.e1Out);
        plainHunter.Tick(Dt, feet, PlayerEye(), Vector3.forward);
        Check(heldSees && heldState != HunterState.Chase && closeState == HunterState.Chase && plainHunter.SeesPlayer && plainHunter.State == HunterState.Chase,
            label + "the grace: seen 9 m off from N, " + heldState + " (sees " + heldSees + "); within 6 m " + closeState + "; with no grace " + plainHunter.State);
        // V2: a glimpse 9 m off under the grace is no Threat (no camera shot cancels on it) and hides no prompt (never SeesPlayer
        // alone); the chase within 6 m is TargetAcquiredRecently and Spotting; the plain chase from 9 m TargetAcquiredRecently only.
        var plainThreat = plainHunter.Threat;
        Check(!heldThreat.Any && !heldHides && closeThreat.TargetAcquiredRecently && closeThreat.Spotting && plainThreat.TargetAcquiredRecently && !plainThreat.Spotting,
            label + "Threat (V2 v1 stand-ins): the held glimpse " + heldThreat + " (prompt hidden " + heldHides + "); the chase within 6 m " + closeThreat + "; the plain chase from 9 m " + plainThreat);
#endif

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
        if (walkerSent) WRule(walker, "walker");
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

        // 5b. The onboarding (Red 2026-10-30): the key no longer gates. With it taken (above), the onboarding's gate is still
        //     Waiting at 0 cells and 0 s and at 149 cells / 179.9 s from C2, Explored only at 150 cells, Fallback only at 180 s;
        //     the tools' gate (needsKey true) is Left at C2 with it.
        var keyGate0 = FrontRoomsMapHunter.StationSendDue(world, 0f, 0);
        var keyGateUnder = FrontRoomsMapHunter.StationSendDue(world, FrontRoomsMapHunter.StationSendSeconds - .1f, FrontRoomsMapHunter.StationExploreCells - 1);
        var keyGate150 = FrontRoomsMapHunter.StationSendDue(world, 0f, FrontRoomsMapHunter.StationExploreCells);
        var keyGate180 = FrontRoomsMapHunter.StationSendDue(world, FrontRoomsMapHunter.StationSendSeconds, 0);
        var keyTools = FrontRoomsMapHunter.StationSendDue(world, c2, 0f, world.StationKeyIsTaken, true);
        Check(world.StationKeyIsTaken && keyGate0 == FrontRoomsMapHunter.StationGate.Waiting && keyGateUnder == FrontRoomsMapHunter.StationGate.Waiting
                && keyGate150 == FrontRoomsMapHunter.StationGate.Explored && keyGate180 == FrontRoomsMapHunter.StationGate.Fallback && keyTools == FrontRoomsMapHunter.StationGate.Left,
            label + "the key is not needed (onboarding): with the key taken the onboarding's gate is " + keyGate0 + " at 0 cells / 0 s, " + keyGateUnder + " at 149 cells / 179.9 s, "
            + keyGate150 + " at 150 cells, " + keyGate180 + " at 180 s; the tools' gate at C2 with the key " + keyTools);

#if FRONTROOMS_RELAY_V2
        // V3a (C32, A26 M7): floor 1's first send under the director. Refused while Dormant; armed (Away), a FirstSend Dispatch
        // places it by the ladder from the player in C2 backstage (Arrive: not released, no Arrived) with the hunt point the
        // take point (the key is taken); it steps in after the tier's arrive delay (unseen only), Arrived once, listens 1.5 s,
        // then hunts to the take point.
        var v2First = NewHunter(new FrontRoomsHunterTuning { sightRange = 0f });
        var v2Arrived = 0;
        v2First.Arrived += (pos, tag) => v2Arrived++;
        Stand(c2);
        var v2Call = new FrontRoomsRelayCall(world.StationTakePoint, 0, FrontRoomsRelayCallCause.Device, true, true, 20f);
        var v2Refused = !v2First.Dispatch(v2Call, FrontRoomsRelayPlacement.FirstSend, feet, PlayerEye(), Vector3.forward) && v2First.State == HunterState.Dormant;
        v2First.Arm();
        var v2Sent = v2First.Dispatch(v2Call, FrontRoomsRelayPlacement.FirstSend, feet, PlayerEye(), Vector3.forward);
        var v2Rep = v2First.LastFirstSend;
        if (v2Sent) WRule(v2First, "v2 first send");
        var v2Problem = !v2Sent ? "not sent" : v2First.State != HunterState.Arrive ? "state " + v2First.State : v2First.Released ? "released at placement"
            : v2Arrived != 0 ? "Arrived at placement" : SendProblem(v2Rep);
        var v2HuntPoint = v2First.ArriveHuntPoint;
        var v2Delay = v2First.ArriveSecondsLeft;
        float v2StepAt = -1f, v2HuntAt = -1f;
        for (var tt = 0f; tt < 20f && v2HuntAt < 0f; tt += Dt)
        {
            v2First.Tick(Dt, feet, PlayerEye(), Vector3.forward);
            world.TickDoorsForTools(Dt);
            Physics.SyncTransforms();
            if (v2StepAt < 0f && v2First.State != HunterState.Arrive) v2StepAt = tt + Dt;
            if (v2StepAt >= 0f && v2First.State == HunterState.Hunt) v2HuntAt = tt + Dt;
        }
        var v2Listened = v2HuntAt - v2StepAt;
        Check(v2Refused && v2Problem == null && v2HuntPoint.HasValue && (v2HuntPoint.Value - takePoint).sqrMagnitude < 1e-6f && Mathf.Abs(v2Delay - v2First.TierV2.arriveDelay) < 1e-3f
                && v2Delay > 0f && v2StepAt >= v2Delay - Dt && v2Arrived == 1 && v2HuntAt > 0f && v2Listened >= FrontRoomsMapHunter.StepInListenSeconds - Dt
                && v2Listened <= FrontRoomsMapHunter.StepInListenSeconds + 2f * Dt && v2First.ListenPoint.HasValue && (v2First.ListenPoint.Value - takePoint).sqrMagnitude < 1e-6f,
            label + "(v2) the first send from C2: refused while Dormant " + v2Refused + "; armed, FirstSend placed rung " + v2Rep.rung + " " + v2Rep.cell + " " + v2Rep.cells
            + " cells backstage (" + (v2Problem ?? "ok") + "), the hunt point the take point " + (v2HuntPoint.HasValue && (v2HuntPoint.Value - takePoint).sqrMagnitude < 1e-6f)
            + "; stepped in " + v2StepAt.ToString("0.00") + " s later (arrive delay " + v2Delay.ToString("0.00") + " s), Arrived " + v2Arrived + "x; hunted to the take point "
            + v2Listened.ToString("0.00") + " s after the step-in");
        // The onboarding (Red 2026-10-30): that dispatch was the run's first send, on the station's layout.
        Check(v2First.FirstSendDone && v2First.OnFirstSend && v2Rep.station && v2Rep.unseen,
            label + "(v2) the FirstSend dispatch is the run's first send: FirstSendDone " + v2First.FirstSendDone + ", OnFirstSend " + v2First.OnFirstSend + ", the station's layout "
            + v2Rep.station + ", unseen " + v2Rep.unseen);
#endif

        // 6. The onboarding (Red 2026-10-30): the run's first send is a run-level fact. A hunter made for the run has none;
        //    ReleaseNow with floor 1's policy sets FirstSendDone and OnFirstSend; a release with no policy (today's Arrive)
        //    sets neither; DebugPlace (tools) and Park (the floor change) clear OnFirstSend and keep FirstSendDone.
        Stand(c2);
        var runHunter = NewHunter(null);
        var runFresh = !runHunter.FirstSendDone && !runHunter.OnFirstSend && runHunter.ReleasePolicy == null;
        var runSent = runHunter.ReleaseNow(Policy(false, 0), feet, PlayerEye(), Vector3.forward);
        var runRep = runHunter.LastFirstSend;
        if (runSent) WRule(runHunter, "run");
        var runOnFloor1 = runSent && runHunter.FirstSendDone && runHunter.OnFirstSend && runRep.station && runRep.unseen && SendProblem(runRep) == null;
        var noPolicyHunter = NewHunter(null);
        var noPolicySent = noPolicyHunter.ReleaseNow(null, feet, PlayerEye(), Vector3.forward);
        var noPolicyNone = noPolicySent && !noPolicyHunter.FirstSendDone && !noPolicyHunter.OnFirstSend && noPolicyHunter.LastFirstSend.rung == 4 && !noPolicyHunter.LastFirstSend.station;
        var toolsHunter = NewHunter(null);
        var toolsSent = toolsHunter.ReleaseNow(Policy(false, 0), feet, PlayerEye(), Vector3.forward) && toolsHunter.OnFirstSend;
        toolsHunter.DebugPlace(world.CellCenter(l.front));
        var toolsCleared = toolsSent && toolsHunter.FirstSendDone && !toolsHunter.OnFirstSend;
        runHunter.Park();
        var runParked = runHunter.State == HunterState.Dormant && runHunter.FirstSendDone && !runHunter.OnFirstSend;
        Check(runFresh && runOnFloor1 && noPolicyNone && toolsCleared && runParked,
            label + "the run's first send (onboarding): a new hunter has none (" + runFresh + "); ReleaseNow with floor 1's policy (rung " + runRep.rung + ", " + runRep.cell + ", station "
            + runRep.station + ", unseen " + runRep.unseen + ") sets FirstSendDone and OnFirstSend (" + runOnFloor1 + "); a release with no policy neither (" + noPolicyNone
            + "); DebugPlace after a first send keeps FirstSendDone and clears OnFirstSend (" + toolsCleared + "); Park, the floor change, the same (" + runParked + ")");

        world.ClearStation();
        Check(!world.StationKeyIsTaken && !world.TakeStationKey() && FrontRoomsMapHunter.FirstSendPolicy.ForStation(world) == null
                && FrontRoomsMapHunter.StationSendDue(world, 1000f, 1000) == FrontRoomsMapHunter.StationGate.Waiting,
            label + "ClearStation forgets the key (StationKeyIsTaken " + world.StationKeyIsTaken + "), no take and no policy without a station; the onboarding's gate without one "
            + FrontRoomsMapHunter.StationSendDue(world, 1000f, 1000) + " at 1000 cells / 1000 s");

        // 7. Floor 2 below the station, as the game makes it (ChangeFloor: SeedFor, NextFloor with the old X the one-cell start
        //    stairwell and its door X + (0, 1), the new X planned and set before Begin). The player left floor 1 before the
        //    onboarding's gate, so the run's first send comes here by the generic ladder (FirstSendPolicy.Generic: no layout).
        var x1 = plan.exit.exitCell;
        var seed2 = FrontRoomsExitPlanner.SeedFor(StationPinSeed, 2, x1, profile.Generation(StationPinSeed), profile.ModuleData(), 1, out _).seed;
        var startDoor2 = new GridCoord(x1.x, x1.y + 1);
        world.NextFloor(seed2, 2, new RectInt(x1.x, x1.y, 1, 1), startDoor2);
        var plan2 = FrontRoomsExitPlanner.ChooseExit(world.Cache, new CellRect(x1.x, x1.y, 1, 1), startDoor2, seed2, FrontRoomsExitPlanner.LaterBandMin, FrontRoomsExitPlanner.LaterBandMax);
        if (!plan2.found)
        {
            Check(false, label + "floor 2 (seed " + seed2 + "): the planner found no EXIT from " + startDoor2);
            UnityEngine.Object.DestroyImmediate(playerGo);
            return;
        }
        var x2 = plan2.exitCell;
        world.SetExitArea(x2);
        eye.position = world.CellCenter(startDoor2) + Vector3.up * .05f;
        world.Begin(eye, false);
        world.StreamForTools(eye.position, 0f);
        Physics.SyncTransforms();
        bool NearStair(GridCoord cell) =>
            (Mathf.Abs(cell.x - x1.x) <= 2 && Mathf.Abs(cell.y - x1.y) <= 2) || (Mathf.Abs(cell.x - x2.x) <= 2 && Mathf.Abs(cell.y - x2.y) <= 2);
        bool Behind(GridCoord cell, Vector3 look)
        {
            var off = world.CellCenter(cell) - feet;
            return off.x * look.x + off.z * look.z < 0f;
        }

        // 7a. The generic ladder from one cell at each walking distance 0-12 from the start door (the first in x, then y),
        //     each looking north, east, south and west. Each send: placed, no station layout, unseen; A40 (system design, 10-30):
        //     anchored on the FLOW target (N here) when its field reaches the player, rung 1 8-10 walking cells on the far
        //     side (the field's margin at least FarSideMetres) and rungs 1-3 never within 2 cells of the player's route to N;
        //     else rung 1 behind the player (the cell's centre less the feet, dot the look < 0, re-derived here); rung 2
        //     6-11, rung 3 14-20, rung 4 today's Arrive; rung 1 drawn whenever it offered a cell;
        //     never in the start area or a stairwell; rungs 1-3 never within 2 cells (Chebyshev) of the old X (the start
        //     stairwell) or the new X. How many starts had such a cell 6-20 walking cells away is counted (the rule had work).
        var walk2 = Walk(world, startDoor2, 12);
        var starts2 = walk2.GroupBy(p => p.Value).OrderBy(g => g.Key).Select(g => g.OrderBy(p => p.Key.x).ThenBy(p => p.Key.y).First().Key).ToList();
        var looks = new[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
        var genHunter = NewHunter(null);
        var genRungs = new int[5];
        var genWrong = new List<string>();
        int genSends = 0, genOffered1 = 0, genNeeded = 0;
        foreach (var from in starts2)
        {
            Stand(from);
            if (Walk(world, from, FrontRoomsStartGroup.Rung3Max).Any(p => p.Value >= FrontRoomsStartGroup.Rung2Min && NearStair(p.Key))) genNeeded++;
            foreach (var look in looks)
            {
                genHunter.Park();
                var ok = genHunter.ReleaseNow(FrontRoomsMapHunter.FirstSendPolicy.Generic(), feet, PlayerEye(), look);
                var rep = genHunter.LastFirstSend;
                if (ok) WRule(genHunter, "floor 2 " + from + " looking " + look);
                genSends++;
                if (rep.rung >= 0 && rep.rung <= 4) genRungs[rep.rung]++;
                if (rep.rung1 > 0) genOffered1++;
                string problem = null;
                if (!ok || !genHunter.Released) problem = "not sent";
                else if (rep.station) problem = "on the station's layout";
                else if (rep.flow && genHunter.LastFlowRoute.Any(r => Mathf.Abs(rep.cell.x - r.x) <= 2 && Mathf.Abs(rep.cell.y - r.y) <= 2) && rep.rung < 4)
                    problem = "within 2 cells of the player's FLOW route";
                else if (!rep.unseen) problem = "seen";
                else if (world.InStartArea(rep.cell) || world.InStairwell(rep.cell)) problem = "in a stairwell";
                else if (rep.rung < 4 && NearStair(rep.cell)) problem = "within 2 cells of a stairwell";
                else if (rep.rung == 1 && (rep.cells < FrontRoomsStartGroup.Rung1Min || rep.cells > FrontRoomsStartGroup.Rung1Max || !rep.farSide
                         || (rep.flow ? !(rep.margin >= FrontRoomsStartGroup.FarSideMetres) : !Behind(rep.cell, look))))
                    problem = "rung 1 at " + rep.cells + " cells, far side " + rep.farSide + (rep.flow ? " (FLOW margin " + rep.margin.ToString("0.0") + " m)" : " (behind " + Behind(rep.cell, look) + ")");
                else if (rep.rung == 2 && (rep.cells < FrontRoomsStartGroup.Rung2Min || rep.cells > FrontRoomsStartGroup.Rung2Max)) problem = "rung 2 at " + rep.cells + " cells";
                else if (rep.rung == 3 && (rep.cells < FrontRoomsStartGroup.Rung3Min || rep.cells > FrontRoomsStartGroup.Rung3Max)) problem = "rung 3 at " + rep.cells + " cells";
                else if (rep.rung1 > 0 && rep.rung != 1) problem = "rung 1 offered " + rep.rung1 + " cells but drew rung " + rep.rung;
                if (problem != null) genWrong.Add(from + " looking " + look + ": rung " + rep.rung + " " + rep.cell + " " + rep.cells + " cells: " + problem);
            }
        }
        // Rungs forced empty from the start door: rung 1 off gives rung 2, 1 and 2 off rung 3, all three off today's Arrive (9-15).
        Stand(startDoor2);
        var ladder2 = new List<string>();
        foreach (var empty in new[] { 1, 3, 7 })
        {
            genHunter.Park();
            var policy2 = FrontRoomsMapHunter.FirstSendPolicy.Generic();
            policy2.emptyRungsForTools = empty;
            var ok = genHunter.ReleaseNow(policy2, feet, PlayerEye(), Vector3.forward);
            var rep = genHunter.LastFirstSend;
            if (ok) WRule(genHunter, "floor 2 forced off " + empty);
            var want = (empty & 2) == 0 && rep.rung2 > 0 ? 2 : (empty & 4) == 0 && rep.rung3 > 0 ? 3 : 4;
            var problem = !ok ? "not sent" : rep.rung != want ? "rung " + rep.rung + ", not " + want : rep.station ? "on the station's layout"
                : !rep.unseen ? "seen" : world.InStairwell(rep.cell) || world.InStartArea(rep.cell) ? "in a stairwell"
                : rep.rung < 4 && NearStair(rep.cell) ? "within 2 cells of a stairwell"
                : rep.rung == 4 && rep.cells >= 0 && (rep.cells < 9 || rep.cells > 15) ? "Arrive at " + rep.cells + " cells" : null;
            ladder2.Add("off " + empty + ": rung " + rep.rung + " " + rep.cell + " " + rep.cells + " cells");
            if (problem != null) genWrong.Add("forced off " + empty + ": " + problem);
        }
        Check(genWrong.Count == 0 && genSends == starts2.Count * 4 && starts2.Count >= 10 && genRungs[1] > 0 && genNeeded > 0,
            label + "floor 2 (seed " + seed2 + ", the start stairwell " + x1 + ", X " + x2 + "): the generic ladder from " + starts2.Count + " starts 0-" + walk2.Values.Max()
            + " cells from the start door x 4 looks, " + genSends + " sends: rungs 1/2/3/4 " + genRungs[1] + "/" + genRungs[2] + "/" + genRungs[3] + "/" + genRungs[4] + " (rung 1 offered at "
            + genOffered1 + "); " + genNeeded + " starts with a stairwell's 2-cell box 6-20 cells away; forced empty: " + string.Join("; ", ladder2.ToArray())
            + (genWrong.Count > 0 ? "; WRONG: " + string.Join("; ", genWrong.Take(6).ToArray()) : ""));

#if !FRONTROOMS_RELAY_V2
        // 7b. v1: the Dormant release keeps its timing (releaseDelaySeconds of ticks after the floor's start door shut) and
        //     places through ReleasePolicy. A run with no first send yet (a new hunter, Generic): the ladder, FirstSendDone
        //     and OnFirstSend. The run hunter (its first send on floor 1, parked): no policy (the game sets none once it is
        //     done), today's Arrive, OnFirstSend false, FirstSendDone kept, its floor-1 report kept.
        Stand(startDoor2);
        var belowHunter = NewHunter(null);
        belowHunter.ReleasePolicy = FrontRoomsMapHunter.FirstSendPolicy.Generic();
        runHunter.ReleasePolicy = null;
        var releaseDelay = new FrontRoomsHunterTuning().releaseDelaySeconds;
        float belowAt = -1f, runAt = -1f;
        for (var tt = 0f; tt < releaseDelay + 1f && (belowAt < 0f || runAt < 0f); tt += Dt)
        {
            if (belowAt < 0f)
            {
                belowHunter.Tick(Dt, feet, PlayerEye(), Vector3.forward);
                if (belowHunter.Released) belowAt = tt + Dt;
            }
            if (runAt < 0f)
            {
                runHunter.Tick(Dt, feet, PlayerEye(), Vector3.forward);
                if (runHunter.Released) runAt = tt + Dt;
            }
        }
        var belowRep = belowHunter.LastFirstSend;
        if (belowAt >= 0f) WRule(belowHunter, "v1 release below");
        var belowOk =Mathf.Abs(belowAt - releaseDelay) <= 1.5f * Dt && belowHunter.FirstSendDone && belowHunter.OnFirstSend && !belowRep.station && belowRep.unseen
            && belowRep.rung >= 1 && belowRep.rung <= 4 && !world.InStairwell(belowRep.cell) && (belowRep.rung == 4 || !NearStair(belowRep.cell));
        var runOk = Mathf.Abs(runAt - releaseDelay) <= 1.5f * Dt && runHunter.FirstSendDone && !runHunter.OnFirstSend && runHunter.LastFirstSend.station
            && runHunter.LastFirstSend.cell == runRep.cell;
        Check(belowOk && runOk,
            label + "(v1) floor 2's Dormant release " + releaseDelay.ToString("0.0") + " s after the shut: a run with no first send yet released at " + belowAt.ToString("0.00")
            + " s through ReleasePolicy (Generic): rung " + belowRep.rung + " " + belowRep.cell + " " + belowRep.cells + " cells, station " + belowRep.station + ", unseen " + belowRep.unseen
            + ", FirstSendDone " + belowHunter.FirstSendDone + ", OnFirstSend " + belowHunter.OnFirstSend + "; the run whose first send came on floor 1 released at " + runAt.ToString("0.00")
            + " s by Arrive (OnFirstSend " + runHunter.OnFirstSend + ", FirstSendDone " + runHunter.FirstSendDone + ", floor 1's report kept " + (runHunter.LastFirstSend.cell == runRep.cell) + ")");
#else
        // 7b. v2: no timed release. Armed on floor 2 (no station), a FirstSend dispatch takes the generic ladder (no layout):
        //     backstage (Arrive), unseen, FirstSendDone and OnFirstSend, its arrive report "first send (generic)". Sent Away,
        //     OnFirstSend stays until the next dispatch; a later Normal (else Relaxed) dispatch clears it, FirstSendDone stays.
        Stand(startDoor2);
        var v2Below = NewHunter(new FrontRoomsHunterTuning { sightRange = 0f });
        v2Below.Arm();
        var belowCall = new FrontRoomsRelayCall(world.CellCenter(startDoor2), 0, FrontRoomsRelayCallCause.Summon, false, false, 20f);
        var v2BelowSent = v2Below.Dispatch(belowCall, FrontRoomsRelayPlacement.FirstSend, feet, PlayerEye(), Vector3.forward);
        var v2BelowRep = v2Below.LastFirstSend;
        if (v2BelowSent) WRule(v2Below, "v2 below");
        var v2BelowReport = v2Below.LastArriveReport ?? "";
        var v2BelowFirst = v2BelowSent && v2Below.State == HunterState.Arrive && !v2BelowRep.station && v2BelowRep.unseen && v2Below.FirstSendDone && v2Below.OnFirstSend
            && v2BelowReport.StartsWith("first send (generic)", StringComparison.Ordinal) && !world.InStairwell(v2BelowRep.cell) && (v2BelowRep.rung == 4 || !NearStair(v2BelowRep.cell));
        v2Below.SendAway(FrontRoomsRelayAwayReason.Test);
        var v2BelowAway = v2Below.State == HunterState.Away && v2Below.OnFirstSend && v2Below.FirstSendDone;
        var v2Later = "none";
        foreach (var pair in Walk(world, startDoor2, 10).OrderBy(p => p.Value).ThenBy(p => p.Key.x).ThenBy(p => p.Key.y))
        {
            if (pair.Value < 5) continue;
            var laterCall = new FrontRoomsRelayCall(world.CellCenter(pair.Key), 0, FrontRoomsRelayCallCause.Summon, false, false, 20f);
            if (v2Below.Dispatch(laterCall, FrontRoomsRelayPlacement.Normal, feet, PlayerEye(), Vector3.forward)) { v2Later = "Normal"; break; }
            if (v2Below.Dispatch(laterCall, FrontRoomsRelayPlacement.Relaxed, feet, PlayerEye(), Vector3.forward)) { v2Later = "Relaxed"; break; }
        }
        var v2LaterOk = v2Later != "none" && v2Below.State == HunterState.Arrive && !v2Below.OnFirstSend && v2Below.FirstSendDone;
        Check(v2BelowFirst && v2BelowAway && v2LaterOk,
            label + "(v2) floor 2, no station: FirstSend " + v2BelowSent + " by \"" + v2BelowReport + "\" (station " + v2BelowRep.station + ", unseen " + v2BelowRep.unseen + "), FirstSendDone "
            + v2Below.FirstSendDone + "; Away, OnFirstSend kept " + v2BelowAway + "; the next dispatch (" + v2Later + ") clears OnFirstSend (" + v2LaterOk + ")");
#endif
        // V3c, C32's W fix: the ladder sends above (WRule after each).
        Check(wBad.Count == 0 && wSends >= 100 && wCells2 > 0,
            label + "C32's W fix (systems design 2026-10-30): " + wSends + " ladder sends here (the station's and floor 2's generic), their rungs 1/2 offered " + wCells1 + "/" + wCells2
            + " cells, every one at W <= " + FrontRoomsMapHunter.FirstSendMaxW.ToString("0") + " m as the warning stage reads it there (WarningDistanceAt; the most " + wMax.ToString("0.0")
            + " m); " + wPlaced12 + " rung 1-2 sends, each at W <= 30 m; report.w = WarningDistanceAt at every rung 1-3 placement; left out for W over 30 m: " + wOut1 + "/" + wOut2
            + " rung 1/2 cells" + (wBad.Count > 0 ? "; WRONG: " + string.Join("; ", wBad.Take(6).ToArray()) : ""));
        UnityEngine.Object.DestroyImmediate(playerGo);
    }

    // ---------- V3c: C32's W fix on the first send's ladder (systems design 2026-10-30) ----------

    /// <summary>
    /// C32's W fix (systems design 2026-10-30; define-agnostic, the first send's ladder is shared by v1 and v2): the ladder's
    /// rungs 1 and 2 also need W at most FirstSendMaxW (30 m) at the cell, W as the warning stage reads it once the Relay
    /// stands there, so stage 1 starts on arrival. From one cell at each walking distance 0-12 from the spawn (the first
    /// in x, then y), each looking north, east, south and west, the generic ladder (no station) twice with the same seed:
    /// with the rule (the default) and without it (FirstSendNoWForTools, the ladder as before 10-30).
    /// - WarningDistanceAt is Tick's W: min(F, 3 x straight), F from a player-rooted 45 m path field built here.
    /// - With the rule every rung 1 and rung 2 cell has W at most 30 m; a rung 1-2 send stands at W at most 30 m;
    ///   report.w is WarningDistanceAt at the placement (rungs 1-3).
    /// - Without it the ladder offers the same cells plus exactly the ones the rule left out (rung1WOut, rung2WOut), each
    ///   at W over 30 m; rung 3 is the same.
    /// - A shut door between the player and an otherwise rung-1 cell: offered without the rule, its W over 30 m only by the
    ///   door's opening cost (9 m on F's way), and left out with it (rung1WOut at least 1).
    /// </summary>
    static void FirstSendW(List<GameObject> roots)
    {
        const string label = "first send W (C32's W fix): ";
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var maxW = FrontRoomsMapHunter.FirstSendMaxW;
        var world = World(roots, FrontRoomsLevelProfiles.Resolve(), 131f, "MAP INTERACTION TEST / first send W");
        world.BuildForCapture();
        var playerGo = new GameObject("MIT first send W player");
        playerGo.transform.SetParent(world.transform, false);
        var body = playerGo.AddComponent<CapsuleCollider>();
        body.height = ModuleUnits.PlayerHeight;
        body.radius = ModuleUnits.PlayerRadius;
        body.center = Vector3.up * (ModuleUnits.PlayerHeight * .5f);
        var spawn = world.CellOf(world.SpawnWorldPosition);
        var walk = Walk(world, spawn, 12);
        var starts = walk.GroupBy(p => p.Value).OrderBy(g => g.Key).Select(g => g.OrderBy(p => p.Key.x).ThenBy(p => p.Key.y).First().Key).ToList();
        var looks = new[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
        var field = new FrontRoomsMapPathField(45f);
        int sends = 0, notSent = 0, cells1 = 0, cells2 = 0, out1 = 0, out2 = 0, placed12 = 0, toolChecked = 0, toolMismatch = 0, beforeOver = 0, moved = 0;
        var rungsWith = new int[5];
        var rungsWithout = new int[5];
        float keptMax = 0f, outMin = float.PositiveInfinity, toolWorst = 0f;
        var bad = new List<string>();
        string doorRow = null;
        var doorOk = false;
        var seed = 300;
        foreach (var from in starts)
        {
            var feet = world.CellCenter(from);
            var eye = feet + Vector3.up * ModuleUnits.PlayerEye;
            playerGo.transform.position = feet;
            Physics.SyncTransforms();
            field.Build(world, from);
            foreach (var look in looks)
            {
                seed++;
                var with = new FrontRoomsMapHunter(world, new FrontRoomsHunterTuning(), body, null, seed);
                var without = new FrontRoomsMapHunter(world, new FrontRoomsHunterTuning(), body, null, seed) { FirstSendNoWForTools = true };
                var okWith = with.ReleaseNow(FrontRoomsMapHunter.FirstSendPolicy.Generic(), feet, eye, look);
                var okWithout = without.ReleaseNow(FrontRoomsMapHunter.FirstSendPolicy.Generic(), feet, eye, look);
                sends++;
                if (!okWith || !okWithout) { notSent++; continue; }
                var a = with.LastFirstSend;
                var b = without.LastFirstSend;
                var a1 = new HashSet<GridCoord>(with.LastFirstSendRung1);
                var a2 = new HashSet<GridCoord>(with.LastFirstSendRung2);
                var b1 = new HashSet<GridCoord>(without.LastFirstSendRung1);
                var b2 = new HashSet<GridCoord>(without.LastFirstSendRung2);
                var where = from + " looking " + look;
                rungsWith[Mathf.Clamp(a.rung, 0, 4)]++;
                rungsWithout[Mathf.Clamp(b.rung, 0, 4)]++;
                out1 += a.rung1WOut;
                out2 += a.rung2WOut;
                cells1 += a1.Count;
                cells2 += a2.Count;
                // Rung 2's cells without the rule are every cell rungs 1-2 looked at (rung 1's are among them).
                foreach (var c in b2)
                {
                    var w = with.WarningDistanceAt(c, feet);
                    var mine = Mathf.Min(field.DistanceTo(c), 3f * Vector3.Distance(world.CellCenter(c), feet));
                    toolChecked++;
                    if (!(Mathf.Abs(w - mine) <= 1e-3f)) { toolMismatch++; toolWorst = Mathf.Max(toolWorst, Mathf.Abs(w - mine)); }
                    var fits = w <= maxW;
                    if (a2.Contains(c) != fits) bad.Add(where + ": " + c + " at W " + w.ToString("0.0", inv) + " m " + (fits ? "left out of" : "offered by") + " rung 2");
                    if (b1.Contains(c) && a1.Contains(c) != fits) bad.Add(where + ": " + c + " at W " + w.ToString("0.0", inv) + " m " + (fits ? "left out of" : "offered by") + " rung 1");
                    if (fits) keptMax = Mathf.Max(keptMax, w);
                    else outMin = Mathf.Min(outMin, w);
                }
                if (!a1.IsSubsetOf(b1) || !a2.IsSubsetOf(b2)) bad.Add(where + ": the rule offered a cell the ladder without it did not");
                if (b.rung1WOut != 0 || b.rung2WOut != 0 || a.rung1 != a1.Count || a.rung2 != a2.Count || a.rung1 + a.rung1WOut != b.rung1 || a.rung2 + a.rung2WOut != b.rung2 || a.rung3 != b.rung3)
                    bad.Add(where + ": rungs with the rule " + a.rung1 + "/" + a.rung2 + "/" + a.rung3 + " (left out " + a.rung1WOut + "/" + a.rung2WOut + "), without " + b.rung1 + "/" + b.rung2 + "/" + b.rung3
                        + " (left out " + b.rung1WOut + "/" + b.rung2WOut + ")");
                if (a.rung >= 1 && a.rung <= 2)
                {
                    placed12++;
                    if (!(a.w <= maxW)) bad.Add(where + ": rung " + a.rung + " placed at W " + a.w.ToString("0.0", inv) + " m");
                }
                if (a.rung >= 1 && a.rung <= 3 && !(Mathf.Abs(a.w - with.WarningDistanceAt(a.cell, feet)) <= 1e-4f))
                    bad.Add(where + ": report.w " + a.w.ToString("0.00", inv) + " m, WarningDistanceAt " + with.WarningDistanceAt(a.cell, feet).ToString("0.00", inv) + " m");
                if (b.rung >= 1 && b.rung <= 2 && b.w > maxW) beforeOver++;
                if (a.rung != b.rung || a.cell != b.cell) moved++;
                // The door case: a cell offered as rung 1 without the rule, whose W is over 30 m only by a shut door's
                // opening cost on F's way (W less that cost at most 30 m), left out with the rule.
                if (doorOk) continue;
                foreach (var c in b1)
                {
                    if (a1.Contains(c)) continue;
                    var f = field.DistanceTo(c);
                    var cost = field.OpeningCostTo(c);
                    var straight3 = 3f * Vector3.Distance(world.CellCenter(c), feet);
                    if (float.IsInfinity(f) || cost < FrontRoomsMapPathField.ShutOpening - .01f || Mathf.Min(f - cost, straight3) > maxW) continue;
                    var w = with.WarningDistanceAt(c, feet);
                    var steps = Walk(world, from, FrontRoomsStartGroup.Rung3Max);
                    doorOk = w > maxW && a.rung1WOut >= 1 && !a1.Contains(c) && !a2.Contains(c) && without.WarningDistanceAt(c, feet) > maxW;
                    doorRow = where + ": " + c + " (" + (steps.TryGetValue(c, out var n) ? n.ToString() : "?") + " walking cells, on rung 1's side) offered as rung 1 without the rule; F "
                        + f.ToString("0.0", inv) + " m of which " + cost.ToString("0.0", inv) + " m opening cost (a shut door on its way), 3 x straight " + straight3.ToString("0.0", inv) + " m: W "
                        + w.ToString("0.0", inv) + " m (" + Mathf.Min(f - cost, straight3).ToString("0.0", inv) + " m without the door's cost); with the rule left out (rung 1 "
                        + a1.Contains(c) + ", rung 2 " + a2.Contains(c) + ", rung1WOut " + a.rung1WOut + ", rung2WOut " + a.rung2WOut + "); the sends: without rung " + b.rung + " " + b.cell
                        + " W " + b.w.ToString("0.0", inv) + " m, with rung " + a.rung + " " + a.cell + " W " + a.w.ToString("0.0", inv) + " m";
                    break;
                }
            }
        }
        Check(bad.Count == 0 && notSent == 0 && toolMismatch == 0 && toolChecked > 0 && starts.Count >= 10 && sends == starts.Count * looks.Length && cells2 > 0 && out1 + out2 > 0,
            label + sends + " generic sends from " + starts.Count + " cells 0-" + walk.Values.Max() + " from the spawn x 4 looks, each with the rule and without it (FirstSendNoWForTools, the same seed): rungs 1/2/3/4 "
            + rungsWith[1] + "/" + rungsWith[2] + "/" + rungsWith[3] + "/" + rungsWith[4] + " with, " + rungsWithout[1] + "/" + rungsWithout[2] + "/" + rungsWithout[3] + "/" + rungsWithout[4] + " without ("
            + moved + " sends moved; without the rule " + beforeOver + " rung 1-2 sends stood at W over 30 m); rung 1/2 cells offered " + cells1 + "/" + cells2 + ", every one at W <= "
            + maxW.ToString("0", inv) + " m (the most " + keptMax.ToString("0.0", inv) + " m), the " + out1 + "/" + out2 + " left out each over it (the least " + (float.IsInfinity(outMin) ? "-" : outMin.ToString("0.0", inv))
            + " m), the rest the same cells; " + placed12 + " rung 1-2 sends at W <= 30 m; report.w = WarningDistanceAt at every rung 1-3 placement; WarningDistanceAt = min(F, 3 x straight) of a 45 m field from the player at "
            + toolChecked + " cells (" + toolMismatch + " off, worst " + toolWorst.ToString("0.000", inv) + " m)" + (bad.Count > 0 ? "; WRONG: " + string.Join("; ", bad.Take(6).ToArray()) : ""));
        Check(doorOk, label + "a shut door between the player and an otherwise rung-1 cell pushes W over 30 m and the cell is left out: " + (doorRow ?? "no such cell found round the spawn"));
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
              && FrontRoomsMapWorld.UnlockPrompt == FrontRoomsKeyHud.Strings.Unlock && !FrontRoomsMapHunter.StationSendNeedsKey,
            "start group keys: the map's one-line prompts are the key HUD's (\"" + FrontRoomsMapWorld.LockedNeedsStairKey + "\", \"" + FrontRoomsMapWorld.UnlockPrompt
            + "\"); the onboarding's send no longer needs the key (StationSendNeedsKey " + FrontRoomsMapHunter.StationSendNeedsKey + ", Red 2026-10-30)");
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
        // C-1: TakeStationKey's default point is StationTakeSpot, 0.5 m in front of the key.
        world.StationTakeSpot(out var takePoint);
        var n = l.front;
        var x = l.exit;

        // 2. The rule is on: one floor key at the KeySpot (6 cm up) with its look+E trigger, no zone key anywhere; a rebuild
        //    of its chunk and a drop of c and c' with the stream's return bring it back once, never twice.
        CountKeys(world, out var zoneKeys, out var floorKeys, out var floorKey);
        // D1.6 (contract v9): a dressed floor key's root is the KeySpot itself (hung from its hook); the cube lies 6 cm up.
        var dressedFloorKey = floorKey != null && floorKey.GetComponentInChildren<IFrontRoomsKeyRig>(true) != null;
        var keyPose = floorKey != null && (floorKey.position - (keyAt + Vector3.up * (dressedFloorKey ? 0f : .06f))).magnitude < 1e-3f;
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
        // 9 (before). The onboarding's gate (Red 2026-10-30) never reads the key: Waiting at 0 cells and 0 s, Fallback at 180 s.
        //    The tools' gate (-autopilotOnboardOff, M7's old one, the key needed): Waiting at C2 without it (Fallback at 150 s).
        var gateNoKey = FrontRoomsMapHunter.StationSendDue(world, 0f, 0);
        var gateFallback = FrontRoomsMapHunter.StationSendDue(world, FrontRoomsMapHunter.StationSendSeconds, 0);
        var toolsNoKey = FrontRoomsMapHunter.StationSendDue(world, c2, 0f, world.StationKeyIsTaken, true);
        var toolsFallback = FrontRoomsMapHunter.StationSendDue(world, c2, FrontRoomsMapHunter.OnboardOffSendSeconds, world.StationKeyIsTaken, true);
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
        //    first send's policy hunts there. 9: the onboarding's gate is still Waiting with the key (it never gates); the
        //    tools' gate is due at C2 with it.
        world.StationKeyTaken -= hunt;
        var policyAfter = FrontRoomsMapHunter.FirstSendPolicy.ForStation(world);
        var gateKey = FrontRoomsMapHunter.StationSendDue(world, 0f, 0);
        var toolsKey = FrontRoomsMapHunter.StationSendDue(world, c2, 0f, world.StationKeyIsTaken, true);
        Check(sent && sentState == HunterState.Listen && released.State == HunterState.Hunt && released.ListenPoint.HasValue && (released.ListenPoint.Value - takePoint).sqrMagnitude < 1e-6f
              && dormant.State == HunterState.Dormant && !dormant.Released && policyAfter != null && policyAfter.huntTo.HasValue && (policyAfter.huntTo.Value - takePoint).sqrMagnitude < 1e-6f
              && gateNoKey == FrontRoomsMapHunter.StationGate.Waiting && gateFallback == FrontRoomsMapHunter.StationGate.Fallback && gateKey == FrontRoomsMapHunter.StationGate.Waiting
              && toolsNoKey == FrontRoomsMapHunter.StationGate.Waiting && toolsFallback == FrontRoomsMapHunter.StationGate.Fallback && toolsKey == FrontRoomsMapHunter.StationGate.Left,
            label + "M6: a Relay sent from C2 (" + sentState + ") hunts to the take point after the take (" + released.State + "); a Dormant one stays " + dormant.State + ", its first send hunts there; "
            + "the onboarding's gate at 0 cells / 0 s " + gateNoKey + " without the key, " + gateKey + " with it (" + gateFallback + " at 180 s); the tools' gate at C2 " + toolsNoKey
            + " without the key (" + toolsFallback + " at 150 s), " + toolsKey + " with it");
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
    // (BuildEdge: the head is (width + 2 face) along, WallThickness + 2 proud across, from DoorHeight up by face; face and proud are the door's or the EXIT's trim).
    static Vector3 HeadCorner(FrontRoomsMapWorld.Door door, float face, float proud) =>
        door.hinge.localPosition + door.closed * new Vector3(ModuleUnits.WallHalf + proud, ModuleUnits.DoorHeight + face, -face);

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
        var corner = HeadCorner(door, FrontRoomsMapWorld.ExitTrimFace, FrontRoomsMapWorld.ExitTrimProud);
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
        var otherBlocks = other != null ? RenderersWithVertex(other.hinge.parent, HeadCorner(other, FrontRoomsMapWorld.DoorTrimFace, FrontRoomsMapWorld.DoorTrimProud), material, true) : new List<MeshRenderer>();
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
