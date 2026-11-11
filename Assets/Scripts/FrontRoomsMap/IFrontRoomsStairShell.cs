using System;
using UnityEngine;

namespace FrontRooms.Map
{
    /// <summary>
    /// The walkable stair's shell as the map and the game drive it (contract Documentation/research/stairwell/
    /// 60_walkable_stair_for_map.md v1.1, sections 3-6; Red 2026-10-22 13:00 "C22 合进去之后做真楼梯", restarted 2026-11-10
    /// "现在就开"). The map line owns this interface; 游戏视觉's FrontRoomsStairShell (look A: flights, landings, guards, rails,
    /// standpipe, turn light) implements it and hands the game a factory (FrontRoomsStairShellHost.Create). Only
    /// FrontRoomsLevelProfile.walkableStair with a factory set turns the stair on; otherwise every floor keeps C22's
    /// one-cell vestibule (FrontRoomsStairwell) exactly as before.
    ///
    /// Cells (MAP_GENERATION.md "Walkable stair"): N is the maze cell in front of the EXIT door, X1 = N + (0, 1) the door
    /// end (the exit landing on top, the far landing below it), X2 = N + (0, 2) the turn end (the turn landing at half
    /// height). The EXIT door is the map's, on N-X1; the far door is the shell's, directly below it on the same line.
    /// The door normal is +z (map north) on every floor.
    ///
    /// Frames and timing. Every point is world space. The shell is made at the old floor's level (the exit landing on
    /// the map's floor, y = the map root's y) and is parented under the game's root, never the map: NextFloor tears
    /// down everything the map built, and the shell outlives the floor it was made on. At the change (variant B, the
    /// map root never moves) the game moves Root up by FloorToFloor together with the player, so the far landing comes
    /// to the new floor's level, and then calls Rebased with that delta. Points read after Rebased are the moved ones.
    ///
    /// The game calls, in this order over a shell's life: the factory (with the floor's EXIT planned); SetFloor and
    /// SetLampLevel; ShowExitLeafShut(true) at the EXIT's latch; at the change Rebased, SetFloor (the new floor),
    /// FarCeiling, SetFarWallPaper; on arrival at the far landing OpenFarDoor, or HoldFarDoor and OpenFarDoor at most
    /// StartDoorHoldLimit later; CloseFarDoor once the player is out on the new floor; then the game destroys Root when
    /// the map has dropped the cells round it (as C22's stairwell goes).
    ///
    /// The far door (交互设计 2026-11-10, Red's door rule of 10-31): it opens by itself, never by USE. The game starts
    /// the open as the feet step onto the far landing, outside the leaf's sweep; the shell's leaf yields at a body
    /// (it never takes a step into a CharacterController: it waits and goes on once clear). The hold is silent and
    /// wordless. Neither the leaf nor its lever is a USE or EXAMINE target: the shell gives them no map collider and no
    /// FrontRoomsInspectable, so no prompt shows and the touch USE button stays unlit. FarDoorMoved and FarDoorLatched
    /// are C22's NorthDoorMoved and NorthDoorLatched: the game raises its StairFarDoorMoved / StairFarDoorLatched from
    /// them for the sound and haptics layers.
    /// </summary>
    public interface IFrontRoomsStairShell
    {
        /// <summary>The middle of the EXIT door's line (X1's south line) on the exit landing's floor.</summary>
        Vector3 ExitDoorPoint { get; }

        /// <summary>
        /// Unit, level, pointing from N into the stair (+z on the map). The seal plane: the game seals the EXIT once the
        /// feet stand at least 1.40 m past ExitDoorPoint along this (C22's ExitSealDepth, inside the ~1.57 m landing and
        /// past the 0.9 m leaf's swing).
        /// </summary>
        Vector3 ExitDoorForward { get; }

        /// <summary>The middle of the far door's line, on the far landing's floor (directly below ExitDoorPoint by FloorToFloor).</summary>
        Vector3 FarDoorPoint { get; }

        /// <summary>The far door leaf's hinge (the autopilot keeps its last waypoint on the latch side, out of the swing).</summary>
        Transform FarDoorHinge { get; }

        /// <summary>Floor to floor, metres (3.50 in look A): the change moves Root and the player up by this much.</summary>
        float FloorToFloor { get; }

        /// <summary>
        /// The walk down, in order: the exit landing, flight 1 (along +x, the EXIT's latch side), the turn landing, flight
        /// 2 (along -x), the far landing, the far door (its last point on the latch side, outside the leaf's swing). The
        /// autopilot walks them; read again after Rebased.
        /// </summary>
        Vector3[] PathWaypoints { get; }

        /// <summary>
        /// How far along the walk down a point is: 0 on the EXIT door's line, 1 on the far door's line, rising along the
        /// waypoints (a point off the path reads the nearest point of the path). The far door's rules read it, not flat
        /// distance: the far door stands about 1 m (flat) from the EXIT door.
        /// </summary>
        float PathProgress(Vector3 feet);

        /// <summary>The feet stand on the far landing (its trigger): the far door may open.</summary>
        bool OnFarLanding(Vector3 feet);

        /// <summary>Open the far door (it swings into the stair, over the far landing). Called once.</summary>
        void OpenFarDoor();

        /// <summary>
        /// The hold pose while the new floor is not ready on arrival: the lever goes down and the leaf gives 1-2 mm. It
        /// never reads as locked (10_directions.md 1.6). OpenFarDoor follows within StartDoorHoldLimit (3 s).
        /// </summary>
        void HoldFarDoor();

        /// <summary>Shut the far door for good once the player is out (it latches and locks: FarDoorLatched). Nothing opens it again.</summary>
        void CloseFarDoor();

        /// <summary>The far leaf left its frame (OpenFarDoor) or started back into it (CloseFarDoor).</summary>
        event Action FarDoorMoved;

        /// <summary>The far leaf latched after CloseFarDoor and is locked for good (the game's floor start waits for it).</summary>
        event Action FarDoorLatched;

        /// <summary>
        /// The static shut EXIT leaf and the exit-level wall as seen from the stair: shown from the EXIT's latch on (the
        /// map's EXIT door goes with NextFloor).
        /// </summary>
        void ShowExitLeafShut(bool shut);

        /// <summary>The far-door wall's maze face takes N''s paper (the map's material for that cell), after Rebased.</summary>
        void SetFarWallPaper(Material paper);

        /// <summary>
        /// N''s ceiling height over the far landing, metres (2.9, Standard, unless the map says otherwise): the far-door wall's
        /// paper bakes its grime band under it. The game sets it at the change from N''s zone (SeedForStair prefers a Standard
        /// N'; a seed fallback may give another height).
        /// </summary>
        float FarCeiling { get; set; }

        /// <summary>
        /// The game has moved Root by <paramref name="delta"/> (the change, +FloorToFloor up): re-bake whatever was baked
        /// from world y (every FrontRoomsSurface material's _CeilingHeight from its own new world y) and move anything that
        /// is not a child of Root.
        /// </summary>
        void Rebased(Vector3 delta);

        /// <summary>The numeral plates: the floor (1 = the title's) and its label (FloorLabel: "LEVEL 0", "LEVEL B1", ...).</summary>
        void SetFloor(int floor, string label);

        /// <summary>Where the seal, lock and latch beats are played from (a child of Root, not the map hinge 3.5 m below after the change).</summary>
        Transform LatchSoundAnchor { get; }

        /// <summary>The shell's root: at X1's door-edge centre on the exit landing's floor, facing ExitDoorForward. The game moves it.</summary>
        Transform Root { get; }

        /// <summary>
        /// The shell's lights, 0 dark .. 1 normal: the seal blackout and any power state. They follow only this (the stair is a safe
        /// zone, Red 2026-10-22 22:47: no warn burst or strike hold of the map reaches them). The game calls it wherever it drives
        /// C22's FrontRoomsStairwell.LampLevel: the walk replaces the seal blackout (contract 60 section 4), so the stair stays at 1.
        /// </summary>
        void SetLampLevel(float level);

        /// <summary>
        /// The shell's light at a world point, 0..1 inside the shell (0 while it is hidden), -1 outside it: EXAMINE's CellLightAt in
        /// X1 / X2, which hold no map lamp (for C22's vestibule FrontRoomsStairwellDresser.StairwellLampAt).
        /// </summary>
        float LightAt(Vector3 worldPos);
    }

    /// <summary>
    /// Optional for a shell: renderers and lights on or off. The game calls it, when the shell implements it, as it calls
    /// C22's FrontRoomsStairwell.SetVisible: on while the chunk of X1 is built (and while the player is inside), off
    /// otherwise. Colliders are never touched. A shell without it stays visible.
    /// </summary>
    public interface IFrontRoomsStairShellVisible
    {
        void SetVisible(bool on);
    }

    /// <summary>
    /// Where the game gets its stair shells. 游戏视觉's package sets Create at load (a RuntimeInitializeOnLoadMethod);
    /// with Create null the walkable stair is off whatever the profile says, and every floor keeps C22's vestibule.
    /// </summary>
    public static class FrontRoomsStairShellHost
    {
        /// <summary>
        /// (parent, x1DoorEdgeCentre, doorNormal) -> a new shell: parent is the game's root, x1DoorEdgeCentre the middle
        /// of X1's south line on the old floor's floor (world), doorNormal the unit +z of the map (from N into X1).
        /// </summary>
        public static Func<Transform, Vector3, Vector3, IFrontRoomsStairShell> Create;

        /// <summary>A shell can be made now (a factory is set).</summary>
        public static bool Available => Create != null;
    }
}
