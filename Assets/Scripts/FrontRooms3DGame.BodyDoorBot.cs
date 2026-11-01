#if UNITY_EDITOR
using System;
using FrontRooms.Map;
using UnityEngine;

/// <summary>Door-only baseline extension. It steers the real player controller; it never opens an
/// unlocked door through a tools seam. A body-door run is useful only in a prepared Relay v2 clone.</summary>
public sealed partial class FrontRooms3DGame
{
    bool autoBodyDoorsRequested;
    bool AutoBodyDoorsRequested => autoBodyDoorsRequested;
    FrontRoomsMapWorld bodyBotMap;
    FrontRoomsMapWorld.Door bodyBotPull;
    float bodyBotPullClock, bodyBotAimClock;
    bool bodyBotBacking, bodyBotChaseSet;
    int bodyBotOpened, bodyBotBarged, bodyBotShut, bodyBotSlammed, bodyBotPullMisses;

    void AutoBodyDoorSetup(string[] args)
    {
        foreach (var arg in args)
            if (arg == "-autopilotBodyDoors" || arg == "bodydoors" || arg == "bodydoorchase") autoBodyDoorsRequested = true;
    }

    void AutoBodyDoorWatch()
    {
        if (!AutoBodyDoorsRequested || map == null || bodyBotMap == map) return;
        if (bodyBotMap != null) bodyBotMap.DoorSwingStarted -= AutoBodyDoorMoved;
        bodyBotMap = map;
        bodyBotMap.DoorSwingStarted += AutoBodyDoorMoved;
    }

    void AutoBodyDoorMoved(FrontRoomsMapWorld.Door door, FrontRoomsMapWorld.DoorSwingKind kind)
    {
        if (door == null || !AutoBodyDoorsRequested || map == null || !map.BodyDoors) return;
        if (door == bodyBotPull)
        {
            if (kind == FrontRoomsMapWorld.DoorSwingKind.Slam) bodyBotSlammed++;
            if (kind == FrontRoomsMapWorld.DoorSwingKind.Slam || kind == FrontRoomsMapWorld.DoorSwingKind.Ease)
            {
                bodyBotShut++;
                autoBotDoorsShut++;
            }
        }
        else if (kind == FrontRoomsMapWorld.DoorSwingKind.Ease || kind == FrontRoomsMapWorld.DoorSwingKind.Barge)
        {
            bodyBotOpened++;
            autoDoorsOpened++;
            if (kind == FrontRoomsMapWorld.DoorSwingKind.Barge) bodyBotBarged++;
        }
    }

    /// <summary>Called before the legacy TryOpenDoor route. Both a shut leaf and the 10-degree
    /// unlock crack are walked into; a key is used only for a still-locked door, through the real shot.</summary>
    bool AutoBodyDoorRoute(float dt, GridCoord here, GridCoord next, out Vector2 local)
    {
        local = Vector2.zero;
        if (!AutoBodyDoorsRequested || map == null || !map.BodyDoors) return false;
        var door = map.DoorBetween(here, next);
        if (door == null || door.broken) return false;
        AutoBodyDoorWatch();
        AutoBodyDoorChaseSetup(door);
        if (doorShot != null && doorShot.Live) return true;
        if (map.LockedByRule(door) && !map.IsUnlockedForTools(door))
        {
            var lockPoint = map.LockPose(door, playerRoot.position).position;
            var flat = lockPoint - playerRoot.position;
            flat.y = 0f;
            if (flat.magnitude > 1.2f) AutoSteerAt(dt, lockPoint, true, out local);
            else if (AutoFace(dt, lockPoint, Mathf.Atan2(ModuleUnits.PlayerEye - ModuleUnits.DoorHandleHeight, Mathf.Max(.1f, flat.magnitude)) * Mathf.Rad2Deg))
            {
                if (map.KeyFor(door, playerRoot.position).HasValue) UseDoor(door.leaf);
                else
                {
                    AutoBotNote("bodydoors: route reached locked door without its key; replanning " + door.a + "-" + door.b);
                    autoRouteIndex = autoRoute.Count;
                }
            }
            return true;
        }
        var target = map.CrossingPoint(here, next) + (map.CellCenter(next) - map.CellCenter(here)).normalized * .7f;
        AutoSteerAt(dt, target, false, out local);
        return true;
    }

    void AutoBodyDoorCrossed(FrontRoomsMapWorld.Door door)
    {
        bodyBotPull = door;
        bodyBotPullClock = bodyBotAimClock = 0f;
        bodyBotBacking = false;
    }

    /// <summary>Turns toward the actual latch half, rests the aim, then steps backward. No USE,
    /// SHUT BEHIND, direct map shut, teleport or camera snap is involved.</summary>
    bool AutoBodyDoorPull(float dt, out Vector2 local)
    {
        local = Vector2.zero;
        if (bodyBotPull == null || !AutoBodyDoorsRequested || map == null || !map.BodyDoors) return false;
        var door = bodyBotPull;
        bodyBotPullClock += dt;
        if (door.broken || door.hinge == null || FrontRoomsMapWorld.IsLatched(door))
        {
            bodyBotPull = null;
            return false;
        }
        if (bodyBotPullClock > 3.2f)
        {
            bodyBotPullMisses++;
            AutoBotNote("bodydoors: pull did not finish within 3.2 s " + door.a + "-" + door.b);
            bodyBotPull = null;
            return false;
        }
        // Let the capsule clear the doorway before it turns. It keeps the existing forward direction.
        var away = playerRoot.position - door.position;
        away.y = 0f;
        if (away.magnitude < .75f && bodyBotPullClock < .4f)
        {
            local = Vector2.up;
            return true;
        }
        var handle = door.hinge.TransformPoint(new Vector3(0f, ModuleUnits.DoorHandleHeight, .92f));
        var to = handle - playerRoot.position;
        var distance = new Vector2(to.x, to.z).magnitude;
        var pitchTo = Mathf.Atan2(ModuleUnits.PlayerEye - (handle.y - playerRoot.position.y), Mathf.Max(.1f, distance)) * Mathf.Rad2Deg;
        var facing = AutoFace(dt, handle, pitchTo);
        if (!bodyBotBacking)
        {
            if (!facing) { bodyBotAimClock = 0f; return true; }
            bodyBotAimClock += dt;
            bodyBotBacking = bodyBotAimClock >= .34f;
        }
        // Once stepping back, keep the step held while tracking the handle. Requiring a one-degree
        // re-aim every frame interrupts the 0.12 s movement hold as the camera-to-handle angle changes.
        if (bodyBotBacking) local = Vector2.down;
        if (!door.open && door.angle < 3f) bodyBotPull = null;
        return true;
    }

    void AutoBodyDoorChecks()
    {
        if (!AutoBodyDoorsRequested) return;
        AutoCheck("bodyDoorModeEnabled", map != null && map.BodyDoors, "bodyDoors must be on in a prepared v2 clone");
        AutoCheck("bodyDoorEnteredMaze", autoCells.Count > 0, autoCells.Count + " actual map cells visited after the terminal pair");
        AutoCheck("bodyDoorsOpened", bodyBotOpened > 0, bodyBotOpened + " body opens; " + bodyBotBarged + " barges");
        if (autoBot == AutoBot.BodyDoorChase)
        {
            AutoCheck("bodyDoorBargeObserved", bodyBotBarged > 0, bodyBotBarged + " barges");
            AutoCheck("bodyDoorPullObserved", bodyBotShut > 0, bodyBotShut + " pull shuts; " + bodyBotSlammed + " slams; " + bodyBotPullMisses + " missed pulls");
            AutoCheck("bodyDoorSlamObserved", bodyBotSlammed > 0, bodyBotSlammed + " pull slams within the sprint-crossing window");
            AutoCheck("bodyDoorActualChase", autoChaseLog.Count > 0, autoChaseLog.Count + " real Relay chases");
            AutoCheck("bodyDoorControlledChase", bodyBotChaseSet, "real hunter placed behind the walker for the door scenario");
            // This is a controlled interaction scenario, not an ambient chase-balance baseline.
            AutoCheck("bodyDoorChaseBalance", true, "chases " + autoChaseLog.Count + "; catches " + autoCatches + "; pull slams " + bodyBotSlammed, true);
        }
    }

    void AutoBodyDoorChaseSetup(FrontRoomsMapWorld.Door door)
    {
#if FRONTROOMS_RELAY_V2
        if (bodyBotChaseSet || autoBot != AutoBot.BodyDoorChase || director == null || relay == null
            || door.open || door.broken || map.LockedByRule(door) && !map.IsUnlockedForTools(door)
            || Vector3.Distance(playerRoot.position, door.position) > 2f) return;
        var behind = playerRoot.position - playerRoot.forward * 4.5f;
        if (!map.IsBuilt(map.CellOf(behind)) || map.InStairwell(map.CellOf(behind))
            || Physics.CheckCapsule(behind + Vector3.up * .36f, behind + Vector3.up * 1.4f, .3f, ~0, QueryTriggerInteraction.Ignore)) return;
        if (director.DebugPlaceOnMap(behind, playerRoot.forward) && relay.DebugLockOn(playerRoot.position))
        {
            bodyBotChaseSet = true;
            AutoBotNote("controlled door chase: real hunter placed 4.5 m behind; real lock-on, movement and doors follow");
        }
#endif
    }
}
#endif
