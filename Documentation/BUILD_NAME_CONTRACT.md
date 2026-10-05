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

## Compatibility allowlist

A full-text scan will still find a small set of deliberate legacy tokens. They are linkage keys, not product names: 

- `Assets/Scenes/FrontRooms3D.unity` and its `.meta` GUID;
- `FrontRooms3DGame`, other C# type/namespace names, and Unity serialized script references;
- `FrontRooms/...` shader, resource, FMOD source/bank, and Input paths;
- `com.redwang.frontrooms3d` and the UE `FrontRooms` module/`/Script/FrontRooms.*` reflection paths;
- historical research, logs, and verification evidence that records the pre-rename checkout.

These tokens stay stable so serialized scenes, asset GUIDs, installed-app updates, FMOD discovery, and Unreal reflection continue to resolve. Build outputs, editor menus, generated labels, active documentation, and project display metadata use `FrontRoomsss`.

The checked-in FMOD banks under `Assets/StreamingAssets/FMOD` are the build input for this branch. FMOD source-project auto-refresh is disabled because the checked-in `FMOD/FrontRooms/FrontRooms.fspro` is a metadata source without a generated `Build` directory; this keeps the existing bank GUIDs and event cache intact and prevents a build-time refresh error. Re-enable source-project refresh only after generating and validating a complete FMOD bank set in FMOD Studio.
