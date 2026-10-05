# FRONTROOMSSS build-name contract

The user-facing product and generated artifact basename is `FrontRoomsss`.

| Target | Generated name | Stable identity kept on purpose |
| --- | --- | --- |
| Windows | `FrontRoomsss.exe` and Unity-generated sibling data/debug folders | `com.redwang.frontrooms3d` |
| macOS | `FrontRoomsss.app` and `FrontRoomsss` executable | `com.redwang.frontrooms3d` |
| WebGL | `FrontRoomsss` in the generated page title and loader metadata | hashed Unity files are regenerated as a set |
| iPhone | `FrontRoomsss` Xcode export/output basename | bundle identifier and Unity Xcode target names |
| Android | `FrontRoomsss.apk` / `FrontRoomsss.aab` | application id and signing identity |
| Unreal | `FrontRoomsss.uproject` and `ProjectName=FrontRoomsss` | `FrontRooms` C++ module and `/Script/FrontRooms.*` reflection paths |

Unity scene paths, C# type names, serialized GUIDs, and the application identifier remain stable.
Those are linkage identities; changing them would invalidate serialized references, installed-app updates, or Unreal reflection/build products.

Generated Mac, WebGL, iPhone, Android, Windows, and Unreal packages must be regenerated from the source branch. Do not rename files inside an existing bundle or mix old WebGL hashed files with a new export.
