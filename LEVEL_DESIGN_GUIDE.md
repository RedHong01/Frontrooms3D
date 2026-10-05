# 3D level design handoff

## Scene

Open Assets/Scenes/FrontRooms3D.unity. This is the first-person greybox scene. It contains the FrontRoomsss bootstrap object; the floor, walls, ceiling, lights, notes, key, openings and hunter are generated when the scene starts.

## Change the route

Edit `Assets/Scripts/FrontRoomsMap/FrontRoomsLevelProfile.cs` for the authored room profile. `FrontRoomsMapWorld` consumes that profile at runtime; the old `FrontRoomsLevel.cs` 2D prototype file is no longer part of this project.

- Room modules and openings are defined by the profile and its `Assets/Levels` assets.
- `FrontRoomsMapWorld` builds the runtime grid, doors, windows, and navigation from that profile.
- `FrontRoomsLevelProfile.Default` supplies a safe fallback when no profile asset is assigned.

The 3D presentation is generated in Assets/Scripts/FrontRooms3DGame.cs, inside BuildWorld(). Edit that method to replace greybox cubes with prefabs, move furniture, add lights or change the first-person camera. The Walk, Run and Radius constants near the top tune the prototype feel.

## Reset and build

Use FrontRoomsss → Create Scene to regenerate a clean bootstrap scene. Press Play and then Space to enter the first-person run. Use FrontRoomsss → Cloud Build macOS or Cloud Build Windows to export a player for the target machine.

This MVP is deliberately code-authored so a route can be changed quickly. A later pass can move the room data into a ScriptableObject or prefab scene once the 2D/3D comparison answers the current design question.
