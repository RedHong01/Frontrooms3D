# 00 Inventory: what is in the code today (baseline)

Line 技术美术调研 (`tech_art`), stage: research draft (code inventory), 2026-11-04.
Red's ask (via 总控, 2026-11-04): "在做无限走廊的时候，我需要你也开启一个task用来 research专业的Technical artist会怎么做，提高画质表现的同时，节省性能。"
What was done: static read of the Unity project on main, read only. No Unity, no web, no download. `find -flags +dataless` = 0 in Assets, ProjectSettings, Documentation before reading. Files named `* 2.cs` skipped.
Paths are relative to `Frontrooms3D/`. Every fact carries `path:line`.

STATUS: complete 2026-11-04 (sections A-F and the waste list written; cross-checked with `/Users/redwang/FrontRoomsVisualWork/tech_art/MEASURED.md`, disagreements in F.3). URP package facts are read from `Library/PackageCache/com.unity.render-pipelines.universal@327d93d1cbc8` (17.6.0), static read only.

## 0. Short answer
1. Everything the player walks through is built at runtime (stream rooms from code + kit FBX, maze chunks as merged 6 m block meshes). Nothing is static-batched, instanced or GPU-resident; the SRP Batcher is the only batching path (MEASURED.md: 0 static / instanced / dynamic batched draws).
2. Title corridor: 5 slots x ~85 drawn renderers (~125 live with LOD children), 12 spots per room (4 soft-shadowed). Rooms behind shut doors stay fully live and casting, and the room behind keeps its 12 lights on until recycled; the clone measured both as removable with no image change (A.4, waste list).
3. Maze: 25 chunks / 1,600 cells built, ~1,600 lamps alive, ~87 lit and visible (8-15 shadowed), frustum culling only; every lens gets an MPB write every frame on desktop (B.3-B.5).
4. The directional "Soft ambient direction" still renders a 2048 x 2-cascade main-light shadow at strength 0.18; switching that shadow off removed 678-851 draws per maze view with no image change (B.6).
5. All custom 3D shaders are SRP-Batcher-compatible by layout; FrontRooms/Surface (225 of 239 materials) has no lightmap / APV / box-projection / DBuffer / cross-fade / DOTS-instancing variants, and its DepthNormals pass uses vertex normals (C).
6. Settings: Forward+, MSAA 4x, HDR, URP 17.6.0; ULTRA writes one value, the additional shadow atlas 4096 -> 8192 (URP trims it to the tiles used); Glass RT is a Mac-only Metal pass, now in universal players (D).
7. Textures: 316 surface PNGs, max 4096, HQ compression, mips on, streaming off everywhere; the October NPOT wallpaper problem is gone, 12 NPOT prop maps remain; 255 kit FBX, 228 with LODs, no cross-fade, authored for lodBias 1 but run at lodBias 2 on desktop Ultra (E).
8. To settle by measurement: whether SSAO still doubles draws (October yes; the clone's single toggles say no, but Glass RT may keep the prepass), plus two stale facts in the brief / MEASURED.md (LOD dither is BlueNoise, not Bayer; 219 of 230 surface materials use FrontRooms/Surface, not 83 of 86) (F.3).

## Files read (sha1)

| file (relative to `Frontrooms3D/` unless absolute; `<URP>` = `Library/PackageCache/com.unity.render-pipelines.universal@327d93d1cbc8`, 17.6.0) | lines | sha1 at read | at 11:4x |
|---|---|---|---|
| `Assets/Scripts/FrontRoomsRoomStream.cs` | 2222 | `32482f23fbc1e386ff5668fae01d5a2288d18dd9` | same |
| `Assets/Scripts/FrontRoomsRoomStream.BodyDoors.cs` | 119 | `e03224854c1cc8d9bafdc5acf3d76b893160311b` | same |
| `Assets/Scripts/Rendering/FrontRoomsTrofferKit.cs` | 395 | `47ba7f660d4e6ab452f88c150d3b9fa1235dad88` | same |
| `Assets/Scripts/Rendering/FrontRoomsLampKit.cs` | 369 | `129c19a923c5f6a4861a06b36441155620cf2457` | same |
| `Assets/Scripts/FrontRoomsFilmMesh.cs` | 166 | `732658abf52c87edbdf0d11a8ba4e66ff0766e52` | same |
| `Assets/Scripts/Rendering/FrontRoomsSurfaces.cs` | 103 | `fe0a1ed70c7eb43304be0a6a24d83d363a7aa173` | same |
| `Assets/Scripts/Rendering/FrontRoomsDoubleDoorKit.cs` | 776 | `b7ab5af0d3d518d22c1a3fe1d78d4fccfaa3f725` | same |
| `Assets/Scripts/Rendering/Doors/FrontRoomsDoorLite.cs` | 514 | `17f9bcb8a3a6d7cfdef4860ecac05bf00dbd57ce` | same |
| `Assets/Scripts/Office/FrontRoomsWallFixtureRenderer.cs` | 1152 | `9085ea1ba9e3e954141b2736f5bb5b4fbde9c21f` | same |
| `Assets/Scripts/Office/FrontRoomsOfficeKit.cs` | 1022 | `fac472a0e08653c269a62bc6e8a0346306253889` | same |
| `Assets/Scripts/Office/FrontRoomsKitLibrary.cs` | 382 | `bd532b071c121f8eed9f8b2aabeb3bd4fbd2d94d` | same |
| `Assets/Scripts/FrontRooms3DGame.cs` | 7965 | `d059a6de21291c206216c0cc7e14945150522c50` | same |
| `Assets/Scenes/FrontRooms3D.unity` | 2936 | `0fb29ab1adf81fb0a115666f9e49055bdfe1c1ae` | same |
| `Assets/Scripts/Rendering/FrontRoomsLook.cs` | 62 | `898dee87990a4744a5669ff36ceaa53fc48f1190` | same |
| `Assets/Scripts/Rendering/FrontRoomsZoneReflection.cs` | 391 | `040ba42bc4fd4f088ff1d62639d49e88939bb0ec` | same |
| `Assets/Scripts/Rendering/FrontRoomsZoneReflectionDriver.cs` | 14 | `801aca30dd77f9e003f68f103a134c1ac2aec6ed` | same |
| `Assets/Scripts/Rendering/FrontRoomsPostStack.cs` | 76 | `6882466191d50443248c9504ee0767e3e88f3a9f` | same |
| `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs` | 7249 | `d660ed557293d7b525db80c4f78edfed145b90d8` | same |
| `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.Pool.cs` | 1213 | `54ce7ffae54d030af89d890fa03ff953f81e17c3` | same |
| `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.BodyDoors.cs` | 934 | `6e2e643acd4daa925358b48d7194c02834e5bdcb` | same |
| `Assets/Scripts/FrontRoomsMap/FrontRoomsLevelProfile.cs` | 456 | `d76fa209300aeda98cebf34419652e8a1edda946` | same |
| `Assets/Scripts/FrontRoomsMap/FrontRoomsMap.cs` | 1095 | `9bc4dc9aaab1d093e350bdd83b83ce3c767464e8` | same |
| `Assets/Levels/FrontRoomsLevel0.asset` | 110 | `11d347610a4a48ec7309b7231bc61a93ceec51d3` | same |
| `Assets/Scripts/Rendering/FrontRoomsExitSign.cs` | 530 | `ca6f7439c0d468670ec71688f9664d1e717bd946` | same |
| `Assets/Scripts/Media/FrontRoomsScreenVideo.cs` | 1718 | `bb19c7aa8125f98aa6ed8728fc89b478bf43aad3` | same |
| `/Users/redwang/FrontRoomsVisualWork/title_stream/survey.md` | 547 | `1dae5fd6c9802cc580a4fe4ecf9871649a307133` | same |
| `/Users/redwang/FrontRoomsVisualWork/title_stream/journal.txt` | 18 | `a53394aca4e575cbd2d36fc4890f67940b445f32` | changed: 7aa8ac3edd69102cc1591010173f4c2c2fd3fd85 |
| `/Users/redwang/FrontRoomsVisualWork/proj_doorlean/Assets/Scripts/FrontRoomsRoomStream.cs` | 2222 | `32482f23fbc1e386ff5668fae01d5a2288d18dd9` | same |
| `Assets/Resources/Props/Troffer/Prefabs/Troffer_L03.prefab` | 458 | `819abb4ff8f3a64dd234a168694ad48a513f999c` | same |
| `Assets/Resources/Props/Troffer/Prefabs/Troffer_L03_Soil.prefab` | 145 | `792e6abc2f8c3a19957ad9420ae00ab3078f5454` | same |
| `Assets/Resources/Props/Troffer/Prefabs/Troffer_L03_Phone.prefab` | 124 | `a6c623598855fe8edb1e071466fdb1ebc971dbfd` | same |
| `Assets/Scripts/FrontRoomsRoomRule.cs` | 5 | `b89b153e7ff094c8ddadea9c6a6d1629e383742b` | same |
| `Assets/Settings/FrontRooms_URP.asset` | 143 | `18584f2bb983c440f38ba4af08f098caf1cc1678` | same |
| `Assets/Settings/FrontRooms_URP_Renderer.asset` | 97 | `15ec2379d6cac9f110cf55e307bdc5e4c97c2839` | same |
| `ProjectSettings/QualitySettings.asset` | 343 | `a8ae14023b12f8aa4616c4a70beb5bf75df0a46e` | same |
| `ProjectSettings/GraphicsSettings.asset` | 69 | `403e8c9fddb6fdad3a5b5614019592ceb91c848c` | same |
| `ProjectSettings/ProjectSettings.asset` | 986 | `9dbc5e1dde44a55e00639096fbf03a36b388f9c0` | same |
| `Assets/Scripts/FrontRoomsQuality.cs` | 451 | `88027cf868ac7d45ef7bc27e3fe17e8c7b20cc1b` | same |
| `Documentation/research/webgl/03_measured_budgets.md` | 366 | `23ea42172a6fb6269414c4a050d13957f25cd503` | same |
| `Documentation/research/interaction_audit/03_rendering_quality.md` | 353 | `acf900f312ad16ca7808b33a70696024d79e47bf` | same |
| `Assets/Scripts/Rendering/FrontRoomsMetalGlassRTRendererFeature.cs` | 322 | `d4ced5318993350542aa12514fc1e78d6baffe72` | same |
| `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRT.cs` | 212 | `b951084ff70dd20fe93515cbdf8dddd0650a7509` | same |
| `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTNative.cs` | 190 | `00257b343527e9c158af5b5ef2e8d8fb71b24acb` | same |
| `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTSystem.cs` | 3165 | `43b4a8c4c5c6d58e4dccb36e0c37730166b7eb3b` | same |
| `Assets/Scripts/Rendering/FrontRoomsMetalGlassRT.cs` | 23 | `0aa64b9da39b5deff21ef1ce44cd3d095bf87f41` | same |
| `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTCamera.cs` | 78 | `0c50556c74950fd16e609762457d096b6a131e62` | same |
| `Assets/Resources/Rendering/FrontRoomsPost.asset` | 315 | `f385ea868da1b3e938be0674dc325cb92bc03139` | same |
| `Assets/Resources/Rendering/FrontRoomsPost_Office.asset` | 165 | `a5bbd6c3e4aa00c345ff5a2024b556fab9c09437` | same |
| `Assets/Resources/Rendering/FrontRoomsPost_Run_B.asset` | 245 | `9a3eecf44022bc6db303adf2c263e5fa94282fcb` | same |
| `Assets/Shaders/FrontRoomsMetalGlassRTComposite.shader` | 52 | `5f90a91cb21a9dcf2276b5a077fc6082a36753c0` | same |
| `Assets/Resources/Brand/WhiteLogoUI.shader` | 63 | `44cec7efc6522141363d43092591cf43683b10c0` | same |
| `Assets/Resources/Rendering/FrontRoomsDoorGrit.shader` | 160 | `13812a5526a20e1597a0d224ede1ecc0f84160c4` | same |
| `Assets/Resources/Rendering/FrontRoomsGlass.shader` | 1309 | `e135b40e60a04c0c4c06af2be511c4ff6a48302b` | same |
| `Assets/Resources/Rendering/FrontRoomsSurface.shader` | 1110 | `8f3587e0c0e921e63423d252503a1e08ab792c4a` | same |
| `Assets/Resources/Rendering/FrontRoomsDoorDustSkin.shader` | 108 | `6e408da171f4883bbf1ac42d153a67696032f508` | same |
| `Assets/Resources/Rendering/RunSoffitHalo.shader` | 76 | `56c1ff6a27b76f3c517a86acfc2b488cee0e0017` | same |
| `Assets/Resources/Rendering/FrontRoomsHeldItemDepthPunch.shader` | 50 | `b4aea56af9714367f691498417e6faf33e8bcf90` | same |
| `Assets/Resources/Rendering/FrontRoomsDoorHaze.shader` | 193 | `de9fcdc4b294690ec98b28b017ca5b2464fc93a4` | same |
| `Assets/Resources/Rendering/FrontRoomsScreenVideo.shader` | 346 | `c73d8095995b15b739d46cab60f876675d0e92d1` | same |
| `Assets/Resources/Rendering/RunBeamCone.shader` | 96 | `76d652f30c6b21be05e648b174b0ad205c4b850c` | same |
| `Assets/Resources/Rendering/FrontRoomsTerminalRaster.shader` | 91 | `90432dd7b7a4bfafa087f7d49e7f5ca1d4aad1d3` | same |
| `Assets/Resources/Rendering/FrontRoomsReflectionBlend.shader` | 78 | `ae21ee4129c6fb197b638f97c2b6941e722a5c6d` | same |
| `Assets/Resources/Lighting/VolumetricBeam.shader` | 84 | `8ebb7fdf3e4c1e830aa458e15fe6f6c209b7c039` | same |
| `Assets/Shaders/GlassRT/FrontRoomsGlassRTPrepass.shader` | 91 | `0db6d49c66f7463b04828d30a740490fd7cc37eb` | same |
| `Assets/Resources/UI/KeyHud/FrontRoomsHudVector.shader` | 301 | `9c42687257edc29032638d45bb4a0597ab615774` | same |
| `Assets/Resources/Props/Lamps/Shaders/FrontRoomsLampGlow.shader` | 70 | `86ece7505cdeef0c8af1e6353338986b58762865` | same |
| `Assets/Resources/Rendering/FrontRoomsDoorDust.hlsl` | 60 | `76cc58b0dae7aea09908f5506da7066934145085` | same |
| `Assets/Resources/Rendering/FrontRoomsGlassFoil.hlsl` | 139 | `d2e155402060d76303924cccacb8596a2eeda0a6` | same |
| `Assets/Editor/Rendering/FrontRoomsKitImporter.cs` | 120 | `4faea6aa1d53b9ce491efed842d791d2da6bbcc2` | same |
| `Assets/Scripts/Rendering/Stairwell/FrontRoomsStairwellDresser.cs` | 892 | `2abeed3fc115181d5519e3e836a9f939fdabce8d` | same |
| `Assets/Scripts/Rendering/FrontRoomsRunRig.cs` | 2131 | `97727e7973d6c1bb740791490ff3a3c968183ec7` | same |
| `Assets/Scripts/Rendering/FrontRoomsStationDress.cs` | 585 | `759a22a906aa736e96788d5df2dd29f9652bde61` | same |
| `Assets/Scripts/Rendering/FrontRoomsStationTvLight.cs` | 229 | `213f15e2c6a1d03ac48a8123ad7a7fc27d5b669a` | same |
| `Assets/Scripts/Office/FrontRoomsWallFixtures.cs` | 1402 | `79f906d56c5814fa7941b3b353d4ddc9bf22bb64` | same |
| `Assets/Scripts/Rendering/Relay/FrontRoomsRelayBody.cs` | 1915 | `56ed1e42103afa693475e662a25a06d77e0042fe` | same |
| `Assets/Scripts/Rendering/Screens/FrontRoomsTerminalScreen.cs` | 615 | `2a24d801e75e085bd816eaaeca0e59940e339782` | same |
| `Assets/Scripts/FrontRoomsMap/FrontRoomsStairwell.cs` | 474 | `ffab08ea4988eb3979e2b9cf4ad4d68b347cc840` | same |
| `Assets/Scripts/FrontRoomsMap/Structures/FrontRoomsStructuresLook.cs` | 300 | `a09486d64e2d7ca39ab895038efd05ba82fdb8db` | same |
| `Assets/Editor/FrontRooms3DBuild.cs` | 175 | `5d69654ac60a6796d655e62b8991b05835b343ce` | same |
| `Assets/Editor/FrontRoomsCloudBuild.cs` | 242 | `255db88c2125ee81103221cb295af146e52418dd` | same |
| `Assets/UniversalRenderPipelineGlobalSettings.asset` | 477 | `9c1b15eaec6c43b062115b1f5df0d45661a574d3` | same |
| `Packages/manifest.json` | 18 | `897d401147b6aa7eb12effb8d40b58d0b1a9cd27` | same |
| `Assets/Plugins/macOS/libFrontRoomsMetalGlassRT.dylib.meta` | 29 | `0377540dd78413f7fb4304d9735d1024ad6d65ce` | same |
| `NativePlugin/FRGlassRTScene.h` | 271 | `5df6b4d880228b308d7d6d016249590ef65efecd` | same |
| `Assets/Resources/Creatures/Relay_NightShift.json` | 240 | `8ae04811da4052d55ab2c2c2038abffd1ddcd594` | same |
| `Assets/Resources/Props/Models/Kit_DoorPushPlate.json` | 135 | `354ff061e0e0181514308c5f20c25597ca18bbbf` | same |
| `Assets/Resources/Props/Models/Kit_DoorPushPlate.fbx.meta` | 134 | `17b73e6bb6fa0f91206a595897b3b4165be3543c` | same |
| `Documentation/research/glass/rt/30_final.md` | 1270 | `d93364e05fad00875474556d44c37e83fefbd60e` | same |
| `Documentation/research/glass/rt/40_for_red.md` | 107 | `a1006ecd0cd7c9985fa9bff2657ef17ea9269dac` | same |
| `Documentation/research/glass/rt/NOTE_1022_dylib_not_in_player.md` | 7 | `216c982b678c2950d0a64f5cbeeb63d573dfa342` | same |
| `<URP>/package.json` | 41 | `3839d0006a5c73b4f235ee8e1d23c97f1f778e69` | same |
| `<URP>/Runtime/Data/UniversalRenderPipelineAsset.cs` | 2097 | `7af3e26ed2a927414d690386d5bbad46f8536327` | same |
| `<URP>/Runtime/Data/UniversalRenderPipelineAssetPrefiltering.cs` | 375 | `03ce15fca5ec7b9da26809b216d4f3a2089c1872` | same |
| `<URP>/Editor/ShaderBuildPreprocessor.cs` | 1347 | `c7e4e7dc5c2e7814634b8c370a897fa14f342826` | same |
| `<URP>/Runtime/RendererFeatures/ScreenSpaceAmbientOcclusion.cs` | 381 | `491c87b04790848080f92fb563a8c1792b5b0b4c` | same |
| `<URP>/Runtime/UniversalRendererRenderGraph.cs` | 2031 | `881fd02ba0037ec16fa82350c46baa585e3ae2a0` | same |
| `<URP>/Runtime/UniversalRenderer.cs` | 889 | `9a0dfe7dedc70855f7822a06853996f83915ad1f` | same |
| `<URP>/Runtime/UniversalRenderPipeline.cs` | 2943 | `036b7eede8630b5f67ae89a023ad09dfa3f62e10` | same |
| `<URP>/Runtime/Passes/AdditionalLightsShadowCasterPass.cs` | 1083 | `ece00fc5dd90885ca82b3ca1e662194adf1fa80c` | same |
| `<URP>/Runtime/Passes/AdditionalLightsShadowAtlasLayout.cs` | 372 | `978af4416fb7d0b56df94239fe7d696009a482e2` | same |
| `<URP>/Runtime/UniversalAdditionalLightData.cs` | 264 | `b2724b6b51ea882464dc09bc14860330d4e2db36` | same |
| `<URP>/Runtime/ShadowUtils.cs` | 772 | `50f227d7bb3539b3ffdafdb3956b64471a0217fa` | same |
| `<URP>/Shaders/Lit.shader` | 555 | `de5f340dcbd83ceba81363e67fed8622334ffb05` | same |
| `<URP>/Shaders/DepthNormalsPass.hlsl` | 83 | `53972a724e1799ccbb33700a2381be6f4fb1c51d` | same |
| `<URP>/ShaderLibrary/GlobalIllumination.hlsl` | 655 | `219893652f042f4aa1d5ed1b95bac4ec0ef61790` | same |
| `<URP>/ShaderLibrary/Core.hlsl` | 277 | `31680dbbb91c7d0a98629a806ae8dfd4dbc8925d` | same |
| `<URP>/ShaderLibrary/DOTS.hlsl` | 13 | `acfbcd0061263f0676c2ffdeaf718eadff18ac33` | same |
| `<URP>/ShaderLibrary/Shadows.hlsl` | 638 | `ac2068302fcf57f143f62f1d9cdd79f61885f618` | same |
| `/Users/redwang/FrontRoomsVisualWork/tech_art/MEASURED.md` | 41 | `f13013c77df7189ebff4a35a864c4e1e69416ec3` | same |
| `<project root>/Documentation/Takeover_2026-10-18/handoff/visual.md` | 90 | `148b4760dd9a68ade4dba543bc8200f3b67764ac` | same |
| `/Users/redwang/FrontRoomsVisualWork/title_stream/concepts.md` | 462 | `affba4272a47dd7c1a23339f74b243272ed1aaf1` | same |

111 files hashed (`shasum`) when read and again at the end. Only `title_stream/journal.txt` changed (that workflow is appending). Also read without a per-file hash: the 263 kit sidecars `Assets/Resources/Props/Models/*.json` and 255 `*.fbx.meta` (by script), the `.meta` of 1,467 image files under `Assets/Resources` (by script), the 239 `.mat` under `Assets/Resources` (by script).

## A. Title corridor (FrontRoomsRoomStream)

Files: `Assets/Scripts/FrontRoomsRoomStream.cs` (2222 lines, sha1 32482f23), `FrontRoomsRoomStream.BodyDoors.cs` (119 lines, e0322485). Created by `FrontRooms3DGame.BuildTitleCorridor` (`Assets/Scripts/FrontRooms3DGame.cs:730-758`).

### A.1 Pool, size, recycle, rebase
- 5 room slots (`MaxRooms = 5`, `FrontRoomsRoomStream.cs:15`); room 11.5 m wide, 2.9 m high, 12 m long (`:16-18`); door opening 2.4 m (`:19`); wall 0.26 m (`:35`).
- All 5 slots are built once in `Initialize` (`:362-374`); `Tick` allocates no room or mesh after that (comment `:461-462`).
- Recycle (`MaintainPool`, `:965-1013`): the oldest slot moves to the front only when its sequence is below `current - 1`, the camera is at least 8 m past its end (`RecycleDistance = 8`, `:51`, test `:981`) and the door of the room after it is shut (`:985-986`). The moved slot gets `ResetRoomLights` (lights off), `RefreshRoomMaterials` and its rear seal back on (`:1007-1011`). Steady state is therefore rooms c-1 (behind), c (current), c+1, c+2, c+3 (ahead).
- Doors open by proximity (camera within 4 m of the door, `:805-806`), 0.9 s (`DoorOpenSeconds`, `:50`), 88 degrees (`:2066`). A door behind the camera snaps shut once the camera is 4 m past it (`:837-845`).
- Rebase: `RebaseThreshold = 192` m (`:53`); when |camera z| > 192 the camera and all 5 roots move back by floor(z/192)*192 (`RebaseIfNeeded`, `:1030-1059`). No rebase or recycle after `EndStreamAt` (frozen, `:977`, `:1034`).
- Title camera crawl 1.15 m/s (`TitleSpeed`, `:42`); handoff transition 6.2 m/s then 1.2 m/s (`:47-49`).

### A.2 How one room is built (`BuildRoom`, `:1149-1229`; `roomTemplate` is null in the game, `FrontRooms3DGame.cs:747`)
All geometry is code-built at runtime; no room prefab. Boxes come from `FrontRoomsFilmMesh.GetPlanarBox` (24-vertex planar box, cached by size string, `Assets/Scripts/FrontRoomsFilmMesh.cs:24-67`), each `Box` adds a MeshRenderer + BoxCollider (`FrontRoomsRoomStream.cs:2073-2090`). Default shadow casting (On) unless set.

Renderer count per slot (code count, desktop tier, the slot's active profile only):

| group | renderers drawn | notes, file:line |
|---|---|---|
| Shell | 6: carpet floor, ceiling, 2 wallpaper walls, 2 baseboards | `:1163-1168`; cast shadows (default On) |
| Rear seal | 1 (active only while the room is not connected) | `:1223-1224`; toggled `:756`, `:829`, `:1007-1008`, `:2036` |
| Door wall | 3 ShadowsOnly boxes (returns + header) + 6 reveal quads (3 room side, 3 far side) | `:1324-1333` (ShadowsOnly), `:1350-1358`; reveals cast off, receive off (`:2123-2124`); far-side reveals get receiveShadows at EndStreamAt (`:572-573`); each reveal quad is a new Mesh per room (`:2101`) |
| Door frame (FrontRoomsDoubleDoorKit, Faces.Both) | 4: lining+casing, saddle, brush strips, hinge jamb halves | `Assets/Scripts/Rendering/FrontRoomsDoubleDoorKit.cs:92-103` |
| Door leaves | left 9, right 8: slab, lite glass, 2 push plates + 2 kick plates (kit instances, LODGroup 1.5 / 6 / 40 m), hinge leaves, hinge barrels, meeting brush (left only) | `FrontRoomsDoubleDoorKit.cs:121-166`, lite `:313-327` (new lite-glass Mesh per leaf, `:321`); plate LOD distances from `Assets/Resources/Props/Models/Kit_DoorPushPlate.json` (lodDistances 1.5, 6, 40; LOD0 2238 tris, LOD1 18, LOD2 10) |
| Troffers (12 fixtures) | 12 x (Lens 1 + Housing 1 of 3 LODs + Soil 1) = 36 | `FrontRoomsTrofferKit.Build`, `Assets/Scripts/Rendering/FrontRoomsTrofferKit.cs:148-182`; Housing LODGroup size 1.224, transitions 0.196 / 0.065 / 0.017 (`Assets/Resources/Props/Troffer/Prefabs/Troffer_L03.prefab:317-330`, comment: 4 m / 12 m / culled 46 m, `FrontRoomsTrofferKit.cs:15-16`); Soil LODGroup culled at 0.0717 (`Troffer_L03_Soil.prefab:135-140`, about 12 m); all three shadows off (`FrontRoomsTrofferKit.cs:163-164`, prefab m_CastShadows 0). Phone/WebGL tier: lens only (`:19-21`, `:167`) |
| Volumetric beams | 12 (one per fixture), transparent | `FrontRoomsRoomStream.cs:1258-1281`; a new 8-vertex 4-triangle Mesh per fixture (`:1273`, `:1283-1309`), 60 meshes per pool |
| Outlets, switches, jacks | 0 renderer components: drawn by `Graphics.RenderMeshInstanced` / `RenderMesh` in `beginCameraRendering` | `FrontRoomsRoomStream.cs:1585-1586`; `Assets/Scripts/Office/FrontRoomsWallFixtureRenderer.cs:1-33`, `:1110-1151`; culled at 12 m (plates), never past 40 m |
| Profile props | Lobby / Shift: none. Run: 24 primitive boxes/cylinder + 2 Kit_ExitSign_Hanging. Exit: 1 Kit_ExitSign. Office: `FrontRoomsOfficeKit.Dress` (seeded kit instances) | `:1568-1646`; Run `:1604-1635`; Exit `:1643`; Office `:1587-1598` |

Total drawn per Lobby slot on desktop, ESTIMATE from the rows above: 6 + 1 + 3 (shadow only) + 6 + 4 + 17 + 36 + 12 = 85 renderers (the rear seal only while sealed). The title_stream survey's own code count (`/Users/redwang/FrontRoomsVisualWork/title_stream/survey.md:238`) is "shell 6 + door wall 9 + door kit ~30 + L03 36 / 12 phone + 12 beams + rear seal", which counts the LOD renderers of the 8 plate kits. Cross-check with MEASURED.md (`:22`): ~125 live renderers per slot = 85 + 12 x 2 troffer-housing LOD1/LOD2 + 8 x 2 plate LOD1/LOD2 renderers (alive, one LOD drawn at a time); in-frustum 379-504 renderers, 47-55 with an MPB, 55-66 transparent.

All five profile variants are built in every slot (`BuildProfileProps`, `:1571-1579`), only the slot's own rule is active (`:1579`, `:1772-1774`). In the title every slot is Lobby: `Initialize` forces `lobbyOnlyTitle = true` (`:349`) and `RuleForStreamSequence` returns Lobby until `BeginPlayableSequence` (`:443-451`, `:677-692`). So each title slot holds an inactive Office dressing, an inactive Run set (with its 2 red lights) and an inactive Exit sign.

### A.3 Lights
- Per room 12 spot lights, one per troffer (`:1193-1213`): spotAngle 162, innerSpotAngle 96, range 10 m (Run 6.5 m, `:1502`), intensity Lobby 5.2 / Shift 4.2 / Office 6.0 / Run 0.6 / Exit 4.4 (`:1490-1500`), bounceIntensity 0.82 (`:86`, `:1213`; realtime GI is off, comment `:1983-1986`).
- Shadows: the 4 centre-column fixtures (indices 0-3) are `LightShadows.Soft`, the 8 side-column fixtures `None` (`:1210`); shadowStrength 0.92, shadowNearPlane 0.1 (`:1211-1212`). No per-light shadow resolution is set in code, so they use the pipeline default tier (see D).
- Run profile adds 2 red spot lights per room (162 / 140 degrees, intensity 3.6, range 9.5, `LightShadows.Soft`, strength 0.85) under the hanging signs (`:2146`, `:2167-2178`).
- Light states: a slot's lights are disabled by `ResetRoomLights` when it is built or recycled (`:1836-1842`, `SetFixtureOutput` sets `light.enabled = false`, `:1989`). A room's lamps are cued only when the door into it finishes opening (`ScheduleRoomLight`, `:757`, `:830`, `:1106`); room 0 is lit at once (`:379`). Each lamp has its own strike delay 0.4-2.2 s, flicker, rise, and may be dead or unstable (`:1850-1898`; odds `:1506-1516`).
- Lights are NOT switched off when the door behind the player shuts (`:837-845` touches only the door). Room c-1 keeps all 12 lights (4 shadowed) on until it is recycled. So in the crawl 2-3 rooms are lit: 24-36 spot lights, 8-12 shadowed. The 2026-10-02 measure (24 visible, then 36 with 13 shadowed; `Documentation/research/webgl/03_measured_budgets.md` via the survey `survey.md:62`, `:149`) fits this; the 13th shadowed light is the directional fill (B.6). MEASURED.md (11-04) agrees: 20(6) / 24(9) / 35(13) visible (shadowed) lights at z 4.7 / 10.5 / 17.4, and the slot behind keeps 12 lights on, 4 shadowed, until recycled.
- After the terminal door shuts in play, `FrontRooms3DGame.UpdateStartRooms` fades every Light under the title root over `StreamFadeSeconds` and destroys it (`FrontRooms3DGame.cs:2130-2152`), then `DisposeOneRoom` removes one room per frame (`FrontRoomsRoomStream.cs:588-621`; `FrontRooms3DGame.cs:2154`).

### A.4 Rooms behind closed doors
- Rooms ahead that are not connected (c+1 before its door opens, c+2, c+3) stay active GameObjects: every renderer is live; only their lights are off. Each one sits behind the rear seal of its own slot (`:1223`) and the shut door, so none of it can be seen, but the camera looks straight down +Z at them and only frustum culling applies (no occlusion data for runtime geometry; the camera has `useOcclusionCulling = true`, `FrontRooms3DGame.cs:518`, but there is no baked data, scene `m_OcclusionCullingData: {fileID: 0}`, `Assets/Scenes/FrontRooms3D.unity:12`).
- Rooms behind the camera (c-1) are also active and lit (above).
- Door lites: a stream lite is LIVE (transmits, the next room is drawn through it) only once the next room is connected or the door moves; otherwise DARK (opaque) (`FrontRoomsRoomStream.cs:625-632`, `Assets/Scripts/Rendering/Doors/FrontRoomsDoorLite.cs:6-22`).
- `EndStreamAt` sets every slot past the terminal room inactive and resets its lights (`FrontRoomsRoomStream.cs:560-568`): that is the only place stream rooms are deactivated.

### A.5 Materials and shaders in a stream room
- Shell: per-profile surfaces from `Resources/Surfaces` via `FrontRoomsSurfaces.Room` (`Assets/Scripts/Rendering/FrontRoomsSurfaces.cs:20-30`; FrontRooms/Surface, world-projected, comment `:5-8`). Lobby: `L0_Wallpaper`, `L0_Carpet`, `L0_Ceiling`; trim `Cove_Base`; lens fallback `Troffer_Lens`; door leaf `Door_Veneer` (`FrontRooms3DGame.cs:713-727`).
- Door frame: `Door_FrameU` or `Prop_WoodWalnut`, `Prop_WoodOak`, `Prop_PlasticBlack`, a runtime URP Lit "Door / brushed steel" (`FrontRoomsRoomStream.cs:1366-1374`, `:1527`).
- Runtime-created URP Lit materials (`FrontRoomsSurfaces.Lit`, `Shader.Find("Universal Render Pipeline/Lit")`, cached by name, `FrontRoomsSurfaces.cs:86-102`): Office putty steel, Run chrome / vinyl / mattress / handrail, door brushed steel (`FrontRoomsRoomStream.cs:1518-1528`).
- Troffer: lens `Troffer_L03_Lens.mat` (FrontRooms/Surface, _EMISSION, comment `FrontRoomsTrofferKit.cs:86`), housing `Troffer_Trim.mat`, soil `Troffer_L03_Soil.mat` (URP Unlit multiply, comment `:17-18`).
- Beam: one runtime material on `Resources/Lighting/VolumetricBeam` shader, renderQueue 3000 (`FrontRoomsRoomStream.cs:302-316`). `BuildVolumetricBeam` writes `_Color` on that one shared material for every beam built (`:1277`); the per-beam colour then comes from the property block.
- Door lite: shared `Glass_Wired` / `Glass_WiredDark` at rest, pooled fade materials while fading (`FrontRoomsDoorLite.cs:15-16`, `:37`, `:46`).

### A.6 MaterialPropertyBlock sites (stream)
| site | renderer | properties | cadence |
|---|---|---|---|
| `FrontRoomsRoomStream.cs:2003-2010` (`ApplyFixtureVisual`) | 12 lenses per room | `_EmissionColor`, `_Color` | every frame for each lamp in Striking, Rising or Unstable state (`TickLamp` `:918`, `:935`, `TickUnstableLamp` `:962`); once on reset, dead, steady-activate and RefreshRoomMaterials (`:903`, `:1841`, `:1961-1971`, `:1771`). Every lens holds a block from build (ResetRoomLights at `:1221`) |
| `FrontRoomsRoomStream.cs:2013-2020` | 12 beams per room | `_Color`, `_Density`, `_DiffuseCoefficient`; also `renderer.enabled` | same cadence as the lens |
| `Assets/Scripts/Rendering/FrontRoomsExitSign.cs:470-474` | exit-sign diffusers (Run 2 per room, Exit 1) | per-slot block via `Apply()` | on temperament changes and drop-outs (`Tick`, called from `TickExitSigns` `FrontRoomsRoomStream.cs:879-885`) |
| `Assets/Scripts/Office/FrontRoomsWallFixtureRenderer.cs:548-558` | papered / painted outlet plates (RenderMesh `matProps`) | `_FR_PlanarSnap`, `_BumpScale` | two static blocks, made once (`:29`) |
| `Assets/Scripts/Media/FrontRoomsScreenVideo.cs:937-940` | title TV glass | face block | on change |

### A.7 Fake volumetric beams
- One per troffer, 12 per room, 60 in the pool (`:1192`). Mesh: a 2-quad tapered strip (front and back faces only, 8 vertices, 4 triangles), top 0.36 x 0.06 m, bottom 0.56 x 0.12 m, 1.0 m long (`volumetricRange`, `:92`, `:1283-1309`). Shadows off, receive off, light probes off, reflection probes off (`:1269-1272`). Density 0.012 (`:89`). Shader and blend: see C (`Assets/Resources/Lighting/VolumetricBeam.shader`).
- A beam renderer is disabled while its lamp is off (`:2019`).

### A.8 Reflection probes, light probes, LOD
- No ReflectionProbe component in the stream. `Initialize` sets the global zone reflection to the Level 0 cube (`:387`; `FrontRoomsLook.SetZoneReflection`, `Assets/Scripts/Rendering/FrontRoomsLook.cs:38-41`): one baked HDR cubemap as `RenderSettings.customReflectionTexture` (`Assets/Scripts/Rendering/FrontRoomsZoneReflection.cs:1-38`; cubes `Assets/Resources/Rendering/Reflections/Refl_Level0|Office|Tall|DeadLamp.exr`).
- No light probes anywhere in code (grep `LightProbeGroup|LightProbes.|ProbeVolume` in Assets/Scripts: no hit). Ambient is Trilight + SH ambient probe (`FrontRoomsLook.cs:43-61`).
- LODGroups in a stream room come only from imported kits: troffer housing and soil (above), the 8 door plate kits, the Office kit models, Kit_ExitSign*, the title TV (`CullWithBracket`, `FrontRoomsRoomStream.cs:1706-1719`). The shell, door frame, leaves, beams and Run primitives have no LOD.

### A.9 What the title_stream work (游戏视觉) changes
- Status 2026-11-04 11:32: Build stage (restarted 11:32 after the outage; journal `/Users/redwang/FrontRoomsVisualWork/title_stream/journal.txt`, last lines). Nothing of the stream is changed yet: `W/proj_doorlean` was resynced to main bc9cf07c at 10:47 and its `FrontRoomsRoomStream.cs` is still sha1 32482f23, as on main. The recommended option is TF-1 TOUR; the Build list (`/Users/redwang/FrontRoomsVisualWork/title_stream/concepts.md:410-421`) includes `TitleFamily` / `TitleProgramme` (Today default), a vignette bank, **lights off behind shut doors (on at Space)**, **legacy variants built only for the legacy cycle**, rebase only in a Level 0 room, vignettes Office / Run! / Station / Pool (Pool behind a flag), and an airlock handoff. Its worst-moment estimate is about +250-300 prop draws plus shadows for the Office room (`concepts.md:352`). Journal `/Users/redwang/FrontRoomsVisualWork/title_stream/journal.txt` (last line 10:20); package `W/apply/title_stream_rooms` held for Red (`Documentation/Takeover_2026-10-18/handoff/visual.md:22`).
- Today on main: the title is pinned to Lobby replicas (`lobbyOnlyTitle`, forced true in `Initialize` `:349`); the RoomRule enum has 5 values, no Pool (`Assets/Scripts/FrontRoomsRoomRule.cs:5`); profile cycle after the handoff is Shift, Office, Run, Exit, Lobby (`:431-441`).
- Planned direction (survey + journal, not code): the title passes through the designed families (Office, Run!, Pool, station...) via a "vignette bank re-parented into sealed slots" (journal 10:20). Cost precedent the survey cites: a Run! B dress in one stream slot measured +666 draws, +97 SetPass, +245k tris over the Ward room (`survey.md:59`, `:233-234`, from run_b_production/20_build.md T11). Phones and WebGL cap visible additional lights at 32 (`survey.md:64`).

## B. Normal maze floor (FrontRoomsMap)

Files: `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs` (7249 lines), `FrontRoomsMap.cs`, `FrontRoomsLevelProfile.cs`, `Assets/Levels/FrontRoomsLevel0.asset`, lamp kits `Assets/Scripts/Rendering/FrontRoomsTrofferKit.cs` and `FrontRoomsLampKit.cs`.

### B.1 Grid, chunks, streaming
- Cell 3 m, chunk 8 x 8 cells = 24 m (`FrontRoomsMap.cs:57-60`).
- Build radius 2 chunks each way, square ring: 5 x 5 = 25 chunks = 1,600 cells, 120 m across (`Assets/Levels/FrontRoomsLevel0.asset:47`; `FrontRoomsLevelProfile.cs:26-27`; drop test `FrontRoomsMapWorld.cs:2490`, nearest ring first `:2494-2518`). One new chunk per frame (asset `:48`). A chunk left for 30 s comes back shifted (asset `:49`, `FrontRoomsMapWorld.cs:2508-2512`).
- Camera far plane = `SightDistance = buildRadius x 24 - 2` = 46 m (`FrontRoomsMapWorld.cs:100`; applied `FrontRooms3DGame.cs:1063`, `:1778`, `:2103-2105`).
- Run! rooms off (`runRooms` default false, `FrontRoomsLevelProfile.cs:51`, not set in the asset), foil off, sash pry off (asset `:109`), body doors on (asset `:110`). The pool family runs only in the structures scene (`FrontRoomsMapWorld.Pool.cs:7-12`).

### B.2 What one chunk builds (`BuildInto`, `FrontRoomsMapWorld.cs:2595-2771`)
- Shell (floors, ceilings, walls, trims, columns) is merged: one mesh per 6 m block (2 x 2 cells) x ceiling-height class (3) x material (`:2606-2616`, `:2733-2744`). Every shell renderer casts shadows (`:2791`) and gets its own MaterialPropertyBlock with `_CeilingHeight`, set once at build (`:2792-2797`). One MeshCollider per chunk (`:2745-2750`).
- Lamps: one per cell unless a module turns it off (`:2718`, `:3672-3756`). Kind per zone (`FrontRoomsLampKit.ForCell`, `:3689`): Level 0, Low and Tall = L03 troffer (desktop: Lens + Housing LODGroup of 3 renderers + Soil = 5 renderer components, up to 3 drawn; phone: lens only); Office Standard rooms = O-C pendant (Lens + Glow quad on FrontRooms/LampGlow + Housing LODGroup; 3 draws desktop, 2 phone); back-of-house Low modules = wrap (1 renderer) (`Assets/Scripts/Rendering/FrontRoomsLampKit.cs:5-45`).
- Also per chunk: doors, windows, keys, outlets (instanced, no renderer components), transition kits (`:2641-2642`, `:2723-2728`), one local Office grade Volume per Office rectangle (`:4613-4648`), Office dressing (`Furnish`, `:2769`).
- ESTIMATE: 1,600 lamps x 5 renderer components = 8,000 lamp renderers alive on desktop in a full 5 x 5 build. MEASURED.md (clone, 11-04) counts ~12.8k live renderers in the maze; 10-03 counted ~3,400 renderers with 1,597 lamp lenses (`Documentation/research/webgl/03_measured_budgets.md:70`, `:100-109`). The L03 kit (landed 10-31) explains most of the rise.

### B.3 Lamp lights
- Spot 162 / 96 degrees, range 10 m (12 m when the ceiling is above 4 m), no cookie, shadows None at build, strength 0.92, near plane 0.1 (`FrontRoomsMapWorld.cs:3713-3727`); intensity = zone lamp intensity, x 1.6 in Tall (`:3726`).
- About 1 lamp in 3 may cast shadows (`castsShadow = hash < .34`, `:3732`).
- Every frame on desktop (`TickFixtures`, `:3840-3867`), for every fixture of every built chunk (about 1,600): the Light is enabled within `lightRadius` 16 m (fade over the last 3 m), soft shadows only for `castsShadow` lamps within `shadowRadius` 9 m (`:3855-3860`; asset `:51-52`). The near-only variant `TickFixturesNear` is WebGL only (`:3833`).
- ESTIMATE: 16 m radius = 804 m2 / 9 m2 = ~89 lit lamps; 9 m radius = ~28 cells x 0.34 = ~10 shadowed. MEASURED.md: 87 visible lights, 8-15 shadowed, all pass URP culling. 10-03: 86-91 visible, 5-20 shadowed.
- No per-light shadow resolution is set in code (map or stream), so every spot uses URP's default tier High (`UniversalAdditionalLightData.cs:66-71`, `:99`, URP 17.6.0 package source) = 1024 (asset tier High, `Assets/Settings/FrontRooms_URP.asset:53`). URP sizes spot bias from tan(spotAngle/2) x range / resolution (`ShadowUtils.cs:390`, `:414`): 162 degrees and 10 m give 63.1 m / 1024 = 6.2 cm per texel (same as the October note).
- Start-area lamps are held dark while the stream rooms can be seen (`StartLampsNorth/South`, `:3791-3809`; `FrontRooms3DGame.cs:1075-1076`, `:2099`, `:2126`).

### B.4 MaterialPropertyBlock sites (maze)
| site | renderer | properties | cadence |
|---|---|---|---|
| `FrontRoomsMapWorld.cs:3861-3865` | every lamp lens in all built chunks (and the pendant's glow, `:3865`) | `_EmissionColor`, `_FR_LampDip` | every frame, every fixture (desktop path) |
| `FrontRoomsMapWorld.cs:3908-3915` | same, WebGL near-only path | same | lit lamps every frame, unlit ones on a change > 0.05 (`:3835`) |
| `FrontRoomsMapWorld.cs:2794-2796` | every shell block renderer whose material has `_CeilingHeight` | `_CeilingHeight` | once at build |
| `Assets/Scripts/FrontRoomsMap/FrontRoomsStairwell.cs:287-289`, `:364` | stairwell pieces, stairwell lens | block | build / lamp |
- MEASURED.md: ~935 in-frustum renderers carry an MPB in the maze (of 2.5k-3.2k in the frustum).
- 10-03: `TickFixtures` cost 1.5-2.7 ms per frame (M3 Max editor; `03_measured_budgets.md:19`, `:70`). The loop is unchanged in shape (all fixtures, every frame).
- Glass RT also reads the lens emission every frame for every fixture within 34 m of the camera (`Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTSystem.cs:37-41`).

### B.5 Culling
- Frustum and the 46 m far plane only. No CullingGroup, portal or visibility code in `Assets/Scripts` (grep `CullingGroup`: no hit; "portal" appears only in wayfinding/audio and the transition kit). One special case: the Station set piece is shown only while its chunk or X's is built (`FrontRoomsMapWorld.cs:1662-1675`).
- Kit LODGroups and the outlet renderer's distance cull (plates 12 m, cap 40 m, `FrontRoomsWallFixtureRenderer.cs:5-7`, `:945`) are the only distance culls.

### B.6 Directional fill "Soft ambient direction"
- Still in the shipped scene: `Assets/Scenes/FrontRooms3D.unity:2243-2340`. Directional, enabled, intensity 0.16, colour (0.769, 0.816, 0.80), soft shadows (`m_Type: 2`), strength 0.18, bias 0.045, normal bias 0.28, near 0.1, realtime (`m_Lightmapping: 4`), URP shadow tier field 2. `RenderSettings.sun` is unset (`m_Sun: {fileID: 0}`, scene `:40`), so URP takes it as the main light.
- The code that builds it for a fresh scene uses intensity 0.22 (`FrontRooms3DGame.cs:552-565`); the scene value 0.16 is what the game uses.
- It pays for the main-light shadow map: 2048, 2 cascades, 40 m, split 0.25 (`FrontRooms_URP.asset:44-46`, `:57-59`); LOW preset 1024 / 1 cascade / 25 m (`FrontRoomsQuality.cs:233-239`).
- MEASURED.md: its shadow off removes 678-851 draws in the maze (5,328 -> 4,492, 4,842 -> 4,164, 7,379 -> 6,528: -12 to -16 %) and 103-137 in the corridor, image change 0.00-0.08 % (noise floor 0.00-0.07 %).

### B.7 Office dressing
- `FrontRoomsOfficeKit.Dress/BeginDress` (`Assets/Scripts/Office/FrontRoomsOfficeKit.cs:435-458`), seeded per room, kit FBX instances from `Resources/Props/Models` via `FrontRoomsKitLibrary.Spawn` (`Assets/Scripts/Office/FrontRoomsKitLibrary.cs:221-244`). Props larger than 0.3 m cast shadows (`:286-287`, applied in `ApplyMaterials` `:345-372`).
- LOD0 / LOD1 (/ LOD2) triangles per piece (sidecars `Assets/Resources/Props/Models/*.json`): task chair 5,168 / 1,601 / 420; CRT monitor 3,096 / 1,456; desk 1,366 / 560; keyboard 1,306 / 98; vending 2,432 / 1,020; water cooler 2,398 / 1,006; copier 2,204 / 924; filing cabinet 1,040 / 446; cubicle panel 456 / 184; mouse 280; paper stack 286.
- The count per room is seed-dependent and was not counted statically. 10-03 measured: Office dressing 140 LOD-active renderers / 356 draws / 71.7k tris over the whole 5 x 5 map; an Office view had 252 prop draws / 56.6k tris in the frustum (`03_measured_budgets.md:93`, `:104`, `:114`).

## C. Shaders

21 custom shader files in `Assets`: 17 runtime `.shader`, 2 runtime `.hlsl` includes (`FrontRoomsDoorDust.hlsl`, `FrontRoomsGlassFoil.hlsl`) and 2 editor-only `.shader` in `Assets/Editor/Rendering/PrintP0`; no ShaderGraph, no .compute, no .cginc. Every runtime 3D shader keeps all its material properties inside one `CBUFFER_START(UnityPerMaterial)` (checked by script; the properties outside it are `[Toggle(KEYWORD)]` keyword toggles and `_Cull` render state, not uniforms), so all are SRP-Batcher-compatible by layout. FrontRooms/Glass declares the buffer once per pass; the two blocks are identical (`FrontRoomsGlass.shader:187-246` vs `:979-1036`, diffed).

| shader (file) | passes (LightMode) | blend / zwrite | notable keywords | clip / discard | materials in Resources |
|---|---|---|---|---|---|
| FrontRooms/Surface (`Assets/Resources/Rendering/FrontRoomsSurface.shader`, 1110 lines) | ForwardLit (UniversalForward) `:161-1055`; ShadowCaster `:1056-1073`; DepthOnly `:1075-1090`; DepthNormals `:1092-1107`. No Meta pass | opaque, ZWrite On, Cull [_Cull] | `multi_compile_local _ _FR_MESH_UV` `:177`; shader_feature_local _EMISSION, _FR_PRINT, _FR_LAMP_FIELD, _FR_OWN_LIGHTS, _FR_DAMAGE, _FR_SHEEN `:178-189`; global `shader_feature _FR_DEBUG_VIEW` `:193`; URP: main/additional light shadows, EVALUATE_SH_*, _SHADOWS_SOFT*, _SCREEN_SPACE_OCCLUSION, _LIGHT_COOKIES, _LIGHT_LAYERS, _CLUSTER_LIGHT_LOOP, rendering layers, fog, instancing `:195-206` | none | 225 (219 in Surfaces, 6 in Props) |
| FrontRooms/Glass (`FrontRoomsGlass.shader`, 1309) | ForwardLit `:141`; FRGlassRTPrepass `:961` | Blend One OneMinusSrcAlpha, ZWrite Off (`:145-146`); prepass ZWrite On, Blend Off | _FR_GLASS_GRIME, _FR_GLASS_WIRE, `multi_compile_fragment _FR_GLASS_RT` `:161`; _REFLECTION_PROBE_BLENDING / BOX_PROJECTION / ATLAS `:169-171` | prepass only: `clip` `:1225`, `:1227`, `:1230` | 6 |
| FrontRooms/VolumetricBeam (`Assets/Resources/Lighting/VolumetricBeam.shader`, 84) | one UniversalForward pass `:20-23` | Blend One One, Cull Off, ZWrite Off, ZTest LEqual (`:15-18`), queue Transparent | fog, instancing only | none | 0 (runtime material, `FrontRoomsRoomStream.cs:311`) |
| FrontRooms/LampGlow (`Assets/Resources/Props/Lamps/Shaders/FrontRoomsLampGlow.shader`, 70) | UniversalForward | Blend One One, ZWrite Off, Transparent-10 (`:20-24`) | fog, instancing | none | 1 |
| FrontRooms/RunB/BeamCone (`RunBeamCone.shader`, 96) | UniversalForward | Blend One One, Cull Off, ZWrite Off (`:23-26`); samples the depth texture (`:38`) | fog, instancing | none | 0 |
| FrontRooms/RunB/SoffitHalo (`RunSoffitHalo.shader`, 76) | UniversalForward | Blend One One, ZWrite Off | fog, instancing | none | 0 |
| FrontRooms/ScreenVideo (`FrontRoomsScreenVideo.shader`, 346) | UniversalForward `:89-90`; DepthOnly `:273`; DepthNormalsOnly `:306` | opaque, ZWrite On | `_GBUFFER_NORMALS_OCT` `:315` | none | 1 |
| FrontRooms/DoorDustSkin, DoorGrit, DoorHaze (`FrontRoomsDoor*.shader`) | UniversalForward | DustSkin SrcAlpha/OneMinusSrcAlpha ZWrite Off (`:27-30`); Grit alpha-test ZWrite On; Haze One/OneMinusSrcAlpha ZWrite Off (`:30-32`) | additional lights, _CLUSTER_LIGHT_LOOP; Haze also _ADDITIONAL_LIGHT_SHADOWS + _SHADOWS_SOFT* (`:39-42`) | discard: DustSkin `:72`, Grit `:149`, `:152`, Haze `:180` | 0 (runtime) |
| Hidden/FrontRooms/GlassRTPrepass (`Assets/Shaders/GlassRT/FrontRoomsGlassRTPrepass.shader`) | FRGlassRTPrepass `:22` | ZWrite On, Blend Off | - | `clip` `:79`, `:83` | 0 |
| Hidden/FrontRooms/MetalGlassRTComposite, ReflectionBlend, TerminalRaster, HeldItemDepthPunch | utility passes | - | - | - | 0 |
| UI: FrontRooms/UI/HudVector, UI/FrontRooms White Logo | UI | - | UI clip rect / alpha clip | `clip` HudVector `:294` | 0 |

Plus 5 materials on Universal Render Pipeline/Lit (`Surfaces/Glass_Edge`, `Glass_Shard`, `Prop_StencilBlack`, `Prop_LensNonGlare`, `Run_StencilNotExit`) and 1 on URP Unlit (`Props/Troffer/Materials/Troffer_L03_Soil`). 239 materials under `Assets/Resources`, none elsewhere in Assets. Keyword use in the 225 Surface materials (m_ValidKeywords): _FR_MESH_UV 186, _EMISSION 27, _FR_DAMAGE 12, _FR_OWN_LIGHTS 11, _FR_PRINT 5, _FR_SHEEN 4, _FR_LAMP_FIELD 1. Runtime-created materials on URP Lit come on top (A.5).

FrontRooms/Surface against URP 17.6 Lit's ForwardLit (`Library/PackageCache/com.unity.render-pipelines.universal@327d93d1cbc8/Shaders/Lit.shader:142-186`):
- Missing: LIGHTMAP_ON, DIRLIGHTMAP_COMBINED, LIGHTMAP_SHADOW_MIXING, SHADOWS_SHADOWMASK, DYNAMICLIGHTMAP_ON, USE_LEGACY_LIGHTMAPS; the APV include `ProbeVolumeVariants.hlsl`; _REFLECTION_PROBE_BLENDING, _REFLECTION_PROBE_BOX_PROJECTION, _REFLECTION_PROBE_ATLAS; _SCREEN_SPACE_REFLECTION; _SCREEN_SPACE_IRRADIANCE; _DBUFFER_MRT1/2/3; LOD_FADE_CROSSFADE; DEBUG_DISPLAY; `DOTS.hlsl` (no DOTS_INSTANCING_ON, so no GPU Resident Drawer / BRG path); `instancing_options renderinglayer`.
- Without the keyword declared, URP defines `_REFLECTION_PROBE_BOX_PROJECTION` and `_REFLECTION_PROBE_BLENDING` as 0 (`ShaderLibrary/GlobalIllumination.hlsl:35-40`), so Surface materials can never box-project or blend probes, also in the Forward+ cluster path (`:294-314`).
- Indirect diffuse is SH only: `inputData.bakedGI = SampleSH(...)` (`FrontRoomsSurface.shader:1008`).
- `_FORWARD_PLUS` is deprecated since 6.1 and replaced by `_CLUSTER_LIGHT_LOOP` (`ShaderLibrary/Core.hlsl:8-13`); Surface already uses `_CLUSTER_LIGHT_LOOP` (`:203`).
- The DepthNormals pass includes URP's generic `Shaders/DepthNormalsPass.hlsl` (`:1105`), which writes the vertex normal (`DepthNormalsPass.hlsl:44`, `:69-75`); there is no `_NORMALMAP` path, so SSAO never sees wallpaper, carpet or ceiling relief.
- `_FR_SHEEN` (4 creature materials) runs `UniversalFragmentPBR` a second time per pixel for a white copy (`FRSheen`, `FrontRoomsSurface.shader:813-831`, called `:1043`): two full light loops on those pixels.
- Forward pass texture fetches: 18 `SAMPLE_TEXTURE2D*` call sites, keyword-dependent (`:512-1027`: print arrays `:512-548`, paper/base, mask, normal `:559-875`, two macro wear taps `:883-884`, substrate `:928`, damage `:950`, `:991`, lamp field or emission `:1024-1027`).
- URP 17.6 Lit also carries `_SCREEN_SPACE_REFLECTION` / `_SCREENSPACEREFLECTIONS_OFF` (`Lit.shader:136`, `:151`); the runtime SSR and GTAO code paths sit behind `URP_SCREEN_SPACE_REFLECTION || MODERN_SSAO` (`Runtime/UniversalRenderPipeline.cs:839`; SSAO GTAO fields under `#if MODERN_SSAO`, `Runtime/RendererFeatures/ScreenSpaceAmbientOcclusion.cs:47-63`). Neither define is set in `ProjectSettings/ProjectSettings.asset:836-848` or the URP asmdef: UNCONFIRMED as supported features of 6000.6.

Volumetric beam output, from its fragment (`VolumetricBeam.shader:65-79`): alpha = saturate(_Density x _DiffuseCoefficient x fades), colour = _Color x alpha, additive. With the stream's values (density 0.012, coefficient 0.82, `FrontRoomsRoomStream.cs:86`, `:89`) the peak is 0.012 x 0.82 = 0.0098 of the lamp colour (ESTIMATE, all fades at 1). It does not read depth (hard intersections) and ignores shadows.

## D. Rendering features and settings

### D.1 Pipeline asset `Assets/Settings/FrontRooms_URP.asset` (all six quality levels point to it, guid 9f471c95, `ProjectSettings/QualitySettings.asset:51`, `:104`, `:157`, `:210`, `:263`, `:316`; also the default, `ProjectSettings/GraphicsSettings.asset:40`)
| setting | value | line |
|---|---|---|
| depth texture / opaque texture | required / off | `:22-23` |
| HDR / precision | on / 0 (32-bit R11G11B10) | `:26-27` |
| MSAA / render scale / upscaling filter | 4x / 1.0 / 0 (Auto) | `:28-30` |
| LOD cross-fade / dithering | on / 1 = **BlueNoise** (enum BayerMatrix 0, BlueNoise 1, Stencil 2: `UniversalRenderPipelineAsset.cs:369-379` in the 17.6 package). The brief's "Bayer" is wrong | `:33-34` |
| light probe system | 0 = legacy probes (APV budgets present, unused) | `:36-43` |
| main light | per pixel, shadows on, 2048 | `:44-46` |
| additional lights | per pixel, 8 per object, shadows on, atlas 4096, tiers 256 / 512 / 1024 | `:47-53` |
| reflection probes | blending off, box projection off, atlas on | `:54-56` |
| shadow distance / cascades / split / border | 40 m / 2 / 0.25 / 0.2 | `:57-62` |
| depth / normal bias | 0.6 / 0.6 | `:63-64` |
| soft shadows / quality | on / 3 = High | `:66`, `:69` |
| cookies | 2048, format 3 | `:70-71`, `:75` |
| SRP Batcher / dynamic batching | on / off | `:72-73` |
| light layers | off | `:76` |
| store actions optimisation | 0 (Auto) | `:78` |
| Adaptive Performance | on | `:79` |
| colour grading | HDR, LUT 32 | `:80-81` |
| GPU Resident Drawer / small-mesh cull / GPU occlusion | 0 (off) / 0 / off | `:86-88` |
| volume update mode | 0 | `:94` |
| shader prefilter (serialized, recomputed by URP at build: `Editor/ShaderBuildPreprocessor.cs:1156-1158`) | e.g. SSAO DepthNormals kept (`m_PrefilterSSAODepthNormals: 0`), soft-shadow quality variants Low/Medium/High stripped (`:126-128` = 1; the generic `_SHADOWS_SOFT` variant is kept, `:129` = 0), DBuffer MRT stripped (`:123-125`), screen-space irradiance stripped (`:131`) | `:103-138` |

### D.2 Renderer `Assets/Settings/FrontRooms_URP_Renderer.asset`
- Forward+ (`m_RenderingMode: 2`, `:51`), depth priming off (`:52`), copy depth mode 1 = after transparents (`:53`), Native RenderPass off (`:31`), intermediate texture mode 1 (`:57`), all layer masks everything (`:34-42`).
- Feature 1, SSAO (`:58-83`): method 0 = BlueNoise, full resolution (Downsample 0), not after-opaque, source 1 = DepthNormals, intensity 1.6, direct-lighting strength 0.35, radius 0.45, samples 0 = High (12), blur 0 = High (bilateral), falloff 40 m (enums: `ScreenSpaceAmbientOcclusion.cs:65-101` in the 17.6 package).
- Feature 2, "FrontRooms Metal Glass RT" (`:84-97`; `Assets/Scripts/Rendering/FrontRoomsMetalGlassRTRendererFeature.cs`): passes are compiled only for `UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX` (empty stubs elsewhere, `:319-320`) and run only on Metal, Game Base cameras with `FrontRoomsGlassRTCamera` and quality not Off (`:57-69`). Prepass + trace at `AfterRenderingPrePasses` (`:55`), end pass after transparents (`:47`). The trace pass asks for the depth texture before opaques (`ConfigureInput(Depth)`, `:50`).
- URP 17.6 forces a depth prepass when normals are requested (SSAO DepthNormals) or when depth is needed before AfterRenderingOpaques (the Glass RT pass) (`Runtime/UniversalRendererRenderGraph.cs:1584-1595`). Depth priming is only possible with MSAA off (`:1641`, `isNotMSAA`), so at MSAA 4x it can never engage.

### D.3 Glass RT and the native plugin
- Plugin `Assets/Plugins/macOS/libFrontRoomsMetalGlassRT.dylib`: now a universal binary (x86_64 + arm64, `file`), importer OSXUniversal CPU AnyCPU (`.dylib.meta:23-26`): it can now load in universal Mac players (the 10-22 note said it was in no player build, `Documentation/research/glass/rt/NOTE_1022_dylib_not_in_player.md`). Source in `NativePlugin/` (Metal, MSL compiled at runtime).
- Default quality: High at quality level 3+ on GPUs with 16+ cores (M3/M4 Pro/Max), else Off; Ultra only on request (`Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRT.cs:6-12`; `FrontRoomsGlassRTSystem.cs:204`). Needs Apple9, Metal 3, macOS 14 (`FrontRoomsGlassRTSystem.cs:447`).
- Caps: TLAS candidates 2,048 at High, 4,096 at Ultra (`FrontRoomsGlassRTSystem.cs:2385`, `:2511`; native `kMaxInstances = 4096`, `NativePlugin/FRGlassRTScene.h:167`); lamps 128 within 30 m (`:35-36`); lens emission read for every fixture within 34 m every frame (`:37-41`); shadowed lamps per hit 6 High / 8 Ultra (`:103`); registration 0.2 ms per frame, 2 ms burst (`:59`, `:64`). Full-resolution targets: GlassDepth R32F, GlassNormal RGBA16F, reflection output (renderer feature comments `:7-12`).
- Measured cost (10-22 verification, arm64 player, M3 Max, under load): main thread p50 0.35-0.53 ms (0.17 ms facing away); GPU at 1440p High 4.7-13.1 ms over 21 runs (4.7-5.0 quiet) with the pane filling the frame, 5.4-6.0 ms at 1.5 m; 120 s autopilot map frames p50 1.9 / p95 3.5 ms GPU (`Documentation/research/glass/rt/30_final.md:29`, `:153`, `:163`, `:166`; `40_for_red.md:5-12`). MEASURED.md: glassrt_off changes 3-6 draws in the maze.

### D.4 Post (`Assets/Scripts/Rendering/FrontRoomsPostStack.cs`)
- One global volume, priority 0, profile `Resources/Rendering/FrontRoomsPost` (`:15-31`); local zone volumes (trigger box, priority 1, blend 2.5 m) for Office (`FrontRoomsMapWorld.cs:4647`, structures scenes) and Run_B (`FrontRoomsRunRig.cs:1124`, `:1137`; structures) (`FrontRoomsPostStack.cs:41-62`). BLOOM LOW adds a global priority-100 override that turns bloom HQ filtering off (`FrontRoomsQuality.cs:368-389`).
- Camera setup (`FrontRoomsPostStack.cs:64-75`): allowHDR, allowMSAA, post on, shadows on, camera AA None (MSAA only), dithering on, Glass RT opt-in.
- `FrontRoomsPost.asset` (overridden values): LensDistortion -0.04, scale 1.01 (`:13-30`); Bloom skip 1, threshold 1.05, intensity 0.55, scatter 0.72, clamp 65472, tint (1, 0.93, 0.78), HQ filtering on, filter 0, downscale 0 (half), max iterations 6, no dirt (`:41-80`); ShadowsMidtonesHighlights shadows (0.97, 1, 0.94), highlights (1, 0.99, 0.93), ranges 0-0.3 / 0.55-1 (`:91-114`); Tonemapping mode 2 = ACES (`:125-154`); WhiteBalance +9 / -7 (`:188-196`); ColorAdjustments post-exposure 0.15, contrast -6, saturation -8 (`:207-224`); Vignette 0.26, smoothness 0.45, colour (0.05, 0.045, 0.03) (`:235-252`); FilmGrain type 4 (Medium3) 0.22, response 0.75 (`:263-277`); ChromaticAberration 0.06 (`:288-293`); LiftGammaGain lift (1, 0.99, 0.94, 0.035) (`:304-315`).
- `FrontRoomsPost_Office.asset`: WhiteBalance +1 / -14; SMH shadows (0.95, 1, 1); FilmGrain Thin1 0.18 / 0.8; ColorAdjustments contrast +4, filter (0.95, 0.99, 0.97), saturation -22; lift (0.98, 1, 0.98, 0.02); CA 0.03 (`:13-165`).
- `FrontRoomsPost_Run_B.asset`: Vignette 0.32 black; ColorAdjustments -0.1 / +12 / -10; WhiteBalance -6; Bloom threshold 1, intensity 0.6, scatter 0.7, HQ off; SMH as global; FilmGrain Medium3 0.22; CA 0.06 (`:13-245`).
- Not used: depth of field, motion blur, screen-space lens flare, Panini, auto exposure.

### D.5 Camera
- `FrontRooms3DGame.CreateCamera` (`Assets/Scripts/FrontRooms3DGame.cs:509-522`): FOV 76, near 0.06 m, far 80 m (map play: 46 m, B.1; start rooms: extended to see down the stream, `:2103-2105`), HDR, MSAA allowed, `useOcclusionCulling = true` (no data, A.4), solid-colour clear `#22231C`. `Application.targetFrameRate = 60` (`:580`).

### D.6 Quality levels and presets
- `ProjectSettings/QualitySettings.asset`: 6 levels Very Low / Low / Medium / High / Very High / Ultra (`:10`, `:63`, `:116`, `:169`, `:222`, `:275`), lodBias 0.3 / 0.4 / 0.7 / 1 / 1.5 / **2** (`:36`, `:89`, `:142`, `:195`, `:248`, `:301`), aniso 0/0/1/1/2/2, vSync 0/0/1/1/1/1, mip limit 1 only on Very Low, streaming mipmaps off everywhere, LOD cross-fade on. Defaults: Standalone 5 (Ultra), WebGL 3, iPhone 2, Android 2 (`:328-342`). The cloud/build scripts call `QualitySettings.SetQualityLevel(3)` (`Assets/Editor/FrontRoomsCloudBuild.cs:174`, `:222`; `Assets/Editor/FrontRooms3DBuild.cs:173`); which level a shipped player starts on is UNVERIFIED here.
- `Assets/Scripts/FrontRoomsQuality.cs`: presets Low / Medium / High / Ultra (`:20`); default Ultra on desktop, High on handhelds (`:74`). Writes only values that differ from the asset (`:289-292`); restores on quit (`:320-334`).
  - Resolution: desktop Low 70 %, Medium 85 %, High/Ultra 100 % (`:143-145`); handheld AUTO 3.2 Mpx budget (2.0 at LOW), quantised to 5 %, clamp 50-100 % (`:23-24`, `:247-261`). Below 100 %: FSR 1 (SHARP) or Linear (SOFT) (`:227-228`).
  - MSAA: LOW 1x, MEDIUM 2x, HIGH 4x (`:148`, `:230-231`). SSAO: LOW/MEDIUM half resolution by reflection on the internal `Downsample` field (`:149`, `:336-365`). Bloom: LOW/MEDIUM HQ filtering off (`:151`, `:368-389`).
  - Shadows: LOW 25 m, 1 cascade, main 1024, additional 1024; MEDIUM additional 2048; HIGH (also the default at Ultra) additional atlas **8192 on desktop** (`HighAdditionalShadowAtlas`, `:205-208`, `:233-240`), 4096 on handhelds and WebGL. So desktop ULTRA does write one value: the atlas 4096 -> 8192.
  - URP trims the additional-light atlas render target to the used tiles, next power of two (`Runtime/Passes/AdditionalLightsShadowCasterPass.cs:614-626`): 8-15 shadowed 1024 tiles fit in 4096 x 4096, so the 8192 size is only allocated when more than 16 tiles are needed.
  - Frame rate: handheld 30/60 (ProMotion 30/40/60/120), desktop vsync / 30 / 60 / 120 / MAX (`:26-31`, `:263-287`). iOS Low Power Mode -> 30 fps and LOW (`:218-223`).
- No STP, TAA, MetalFX or FSR 2 in code. MEASURED.md: the URP 17.6 IUpscaler framework (STP, FSR2, DLSS) sits behind `ENABLE_UPSCALER_FRAMEWORK`, not defined here.

### D.7 Graphics settings (`ProjectSettings/GraphicsSettings.asset`)
- `m_BrgStripping: 0` (`:49`): BRG variants not kept (MEASURED.md: GRD logs "BatchRendererGroup Variants setting must be Keep All"). Lightmap and fog stripping automatic (`:46-47`), instancing stripping 0 (`:48`), video shaders always included (`:28`), camera-relative culling off (`:68-69`).
- URP global settings (`Assets/UniversalRenderPipelineGlobalSettings.asset`): strip debug variants on (`:28`), strip unused post-processing variants **off** (`:22`, `:29`), strip unused variants on (`:23`, `:30`), strip screen-coord override on (`:24`, `:31`), export variants on (`:17`).

### D.8 Player settings (`ProjectSettings/ProjectSettings.asset`)
- Linear colour space (`m_ActiveColorSpace: 1`, `:50`), multithreaded rendering on (`:54`).
- Graphics APIs: iOS automatic (Metal, `:548-550`), WebGL automatic (WebGL2, `:551-553`); no Standalone entry, so Mac/Windows use Unity's automatic list (which Windows API 6000.6 picks: UNCONFIRMED).
- Static batching: Standalone on, WebGL off, iPhone on; dynamic batching off everywhere (`:534-543`). MEASURED.md: 0 static, 0 instanced, 0 dynamic batched draws in every view (all geometry is built at runtime; nothing calls `StaticBatchingUtility`).
- Skinning: `gpuSkinning: 0`, `meshDeformation: 0` (CPU) (`:102-103`). The one skinned character is the Relay (`Assets/Scripts/Rendering/Relay/FrontRoomsRelayBody.cs:78`, `:961-980`; `Assets/Resources/Creatures/Relay_NightShift.json`: 36 bones, LOD0 11,497 / LOD1 5,499 / LOD2 2,199 tris).
- Texture compression defaults: no per-target default format (`m_BuildTargetDefaultTextureCompressionFormat: []`, `:570`); WebGL builds force the Generic subtarget (`FrontRooms3DBuild.cs:169`, `FrontRoomsCloudBuild.cs:218`). Metal API validation on (`:243`) (editor/player debug setting).
- Light layers off in the asset, yet the station code writes rendering-layer bits on its renderers and the TV light; the file itself says they are inert until light layers are on (`Assets/Scripts/Rendering/FrontRoomsStationDress.cs:23`, `:371-376`; `FrontRoomsStationTvLight.cs:106`).

### D.9 Reflections and GI
- Scene: no skybox, default reflection mode Skybox at 0.3, baked and realtime GI off, no LightingData (`Assets/Scenes/FrontRooms3D.unity:29-54`). At runtime `FrontRoomsLook.ApplyAmbient` (now called in the game's Awake, `FrontRooms3DGame.cs:603`) sets Trilight ambient, Exp2 fog 0.014, and rebuilds the SH probe (`FrontRoomsLook.cs:43-61`); `FrontRoomsZoneReflection` puts one of 4 baked HDR cubes (`Resources/Rendering/Reflections/Refl_*.exr`, 1536 x 256 strips) in as the custom reflection at linear 0.45-0.5 (`FrontRoomsZoneReflection.cs:43-48`), cross-fading through an HDR cube RT (6 faces x 9 mips = 54 draws per frame only while a fade runs, `:19-21`).
- ReflectionProbe components exist only in the stairwell (realtime via script, 64 px, box projection set, `Assets/Scripts/Rendering/Stairwell/FrontRoomsStairwellDresser.cs:701-721`), the Run! rig (custom cube, disabled until used, `FrontRoomsRunRig.cs:1330-1341`; Run! rooms are off) and the structures scene (`FrontRoomsStructuresLook.cs:179`). With box projection off in the asset and no keyword in Surface, box projection is inert for Surface materials (C).
- No light probes, no APV, no lightmaps.

## E. Textures and meshes

Method: a script read every image file under `Assets/Resources` (PNG/JPG/EXR headers for size) and its `.meta` (texture type, shape, mips, streaming, npot scale, per-platform blocks). Files named `* 2.png` skipped (2 in Surfaces/Textures: `Alarm_ExitAlu_A 2.png`, `Alarm_ExitAlu_N 2.png`). Texture2DArray `.asset` files (print arrays) are not in these counts.

### E.1 Import settings (all of `Assets/Resources`, 1,467 image files)
- Streaming mipmaps: off on 1,467 of 1,467. Mipmaps on all 3D textures (Surfaces 316 / 316, Props 17 / 17).
- `Surfaces/Textures`: 316 files (was 166 on 10-02). Default platform max 4096 on all 316; compression CompressedHQ (2) on 313, Compressed (1) on 2, none (0) on 1; aniso 16 + trilinear on 310 (aniso 4 bilinear on 5, 0 on 1); 84 imported as normal maps. Per-platform blocks with `overridden: 1`: WebGL 103, iOS 40, Android 31, Standalone 1 (10-02: none). The Standalone blocks that read max 2048 / Compressed are `overridden: 0`, so desktop uses the default 4096 / HQ.
- Sizes (Surfaces): 1024x1024 116, 2048x2048 67, 512x512 24, 1024x512 21, 2048x1024 11, 256x256 11, plus six 4096x4096 (`Prop_StationPaper_A`, `Prop_KeyTagNo_A/S`, `Creature_TwillSpruce_A/N/S`).
- NPOT in Surfaces: 12, all with `nPOTScale: 0` (None): `Prop_PlateBusinessHours_A/N/S` 1832x612, `Prop_KeyboardKeys_A/N/S` 2048x768, `Prop_ScreenCRT_A/E/S` 1024x768, `Prop_VendingHeader_A/E/S` 1024x96. The 2048x3072 wallpaper maps of October are gone: the wallpaper is now `Wallpaper_Paper_M/N/S` and `Wallpaper_Print_P` at 2048x2048 plus `Wallpaper_Substrate_N` 1024x512.
- `Props/Troffer/Textures` 8 and `Props/Lamps/*/Textures` 9: all POT, max 2048, HQ, mips, no overrides.
- Other folders: `WorldText` 997 (localized copies of surface and print textures for 9 languages; 234 NPOT; WebGL overrides on 567), `UI` 77 (76 NPOT, uncompressed, no mips), `Print` 25 (23 NPOT, 18 uncompressed, two with max 16384), `Rendering` 10 (reflection cube strips 1536x256 EXR, desk stills), `Language` 12, `Brand` 7, `Terminal` 6.

### E.2 GPU memory, ESTIMATE
Arithmetic per texture: longest side capped at the platform's max size; bits per pixel: BC7 8 (colour, HQ), BC5 8 (normal, HQ), BC4 4 (single channel), BC1 4 / BC3 8 (Compressed without / with alpha), RGBA32 32 for uncompressed and for NPOT with npot None (the rule the 10-03 measurement showed: three 2048x3072 maps = 96 MB = 3 x 2048 x 3072 x 4 B x 4/3); x 4/3 with mips; x 6 for cubes. The format rules are Unity's documented desktop defaults as I know them, not re-read here: UNVERIFIED in detail.
| set | files | desktop (Standalone) MiB | WebGL MiB | iOS MiB |
|---|---|---|---|---|
| Surfaces/Textures, every file resident | 316 | 890.6 | 594.3 | 863.0 |
| Props (Troffer + Lamps) | 17 | 14.0 | 14.0 | 14.0 |
| WorldText, every language resident | 997 | 1,590.7 | 1,205.7 | 1,590.7 |
| all of Resources scanned | 1,467 | 2,661 | 1,952 | 2,649 |
These are upper bounds: `Resources` assets load when a material or script asks for them, and one language is active. A concrete working set: a Lobby stream room's materials (L0 wallpaper, carpet, ceiling, cove base, door veneer, frame, oak, black plastic, push plate, stainless, L03 lens / trim / soil, wired glass) reference 35 unique textures = 114.2 MiB on desktop; adding the Office wall, carpet and ceiling gives 47 textures = 158.2 MiB. Examples: a 2048x2048 HQ map with mips = 2048 x 2048 x 1 B x 4/3 = 5.33 MiB; `DoorVeneer_A` 2048x4096 = 10.7 MiB. The measured texture memory on 10-03 was 335-339 MB for a map frame (`03_measured_budgets.md:18`); not re-measured (MEASURED.md has no memory row).
- Largest single files on desktop (each 21.3 MiB): the six 4096x4096 above (and the WorldText copies of `Prop_StationPaper_A`); the largest uncompressed ones: `WorldText/*/Print/Stair/id_floor_glyphs.png` 2048x2512 NPOT RGBA32 = 19.6 MiB each.

### E.3 Meshes and LOD
- `Assets/Resources/Props/Models`: 255 FBX, 263 JSON sidecars. From the sidecar triangle fields: 127 models have 3 LODs, 101 have 2, 27 have 1. LOD0 triangles: total 623,508, median 2,030, max 11,784 (`Kit_ChairDolly8`). Next: `Kit_TandemBeam3*` 10,099 (x3 colours), `Kit_TandemBeam2Table*` 8,991 (x3), `Kit_AlarmDetector` 8,590, `Kit_CRTTV_Station` 7,210, `Kit_KeyCabinet` 6,804, `Kit_JanitorCart` 6,587, `Kit_ExitSign_Hanging(_Dead)` 6,502 (10-02: 54 FBX, no LODs).
- LOD naming: FBX children `_LOD0/_LOD1/_LOD2` give Unity's automatic LODGroup (`Assets/Editor/Rendering/FrontRoomsKitImporter.cs:66-69`). The importer sets switch heights from the sidecar `lodDistances` at the 76 degree reference FOV, `size / (2 d tan 38)` (`:93-110`), else LOD1 below 10 % and cull below 2-3 % (`:115-116`); `fadeMode = None` always (`:111`, `:118`); mesh compression off (`:43`); read/write off except door-break debris (`:49`). Common sidecar distances: none (59 models), [4, 12, never cull] (39), [1.5, 4, 12] (29).
- Unity scales every LOD switch by `QualitySettings.lodBias`; the desktop default level Ultra has lodBias 2 (D.6), so each switch happens at twice the authored distance (troffer housing LOD0 to 8 m, LOD1 to 24 m, cull at 92 m, beyond the 46 m far plane). The outlet renderer divides its distances by lodBias the same way (`FrontRoomsWallFixtureRenderer.cs:945`); `FrontRoomsTerminalScreen.cs:527` too.
- Lamp kit meshes (lens / housing LOD0 / LOD1 / LOD2 / phone tris, from the kit comments and the title_stream survey): L03 10 / 396 / 216 / 44 / 66 (`survey.md:261`); O-C pendant lens 6 / housing 770 / 322 / 90 / phone 18, plus its 2-triangle glow quad (`FrontRoomsLampKit.cs:26-33`; the survey's 8 / 776 / 328 / 96 / 20, `survey.md:277`, adds the glow); R-B strip 64 + 32 / 2,362 / 874 / 14 / 42 + 16 (`FrontRoomsLampKit.cs:13-20`).
- Other notable kits: push plate LOD0 2,238 (LOD1 18) and kick plate LOD0 2,978 (LOD1 18), 8 per stream door; outlets `Kit_OutletDuplex` 2,880 / 954 / 42, `Kit_OutletHandyBox` 3,860 / 1,300 / 80; Relay 11,497 / 5,499 / 2,199.
- Runtime-built meshes: map shell blocks (per chunk, `FrontRoomsMapWorld.cs:2733-2744`), `FrontRoomsFilmMesh` planar boxes (shared, cached by size), and per-instance meshes in the stream: 60 beam meshes (`FrontRoomsRoomStream.cs:1273`), 30 reveal quads (`:2101`), 10 lite-glass meshes (`FrontRoomsDoubleDoorKit.cs:321`) per pool.

## F. Delta vs 2026-10-02 audit and 10-03 budgets

Sources: `Documentation/research/interaction_audit/03_rendering_quality.md` §1 (lines 20-162) and `Documentation/research/webgl/03_measured_budgets.md` §0 (lines 9-22). New measurements: `/Users/redwang/FrontRoomsVisualWork/tech_art/MEASURED.md` (clone proj_build from main bc9cf07c, 11-04).

### F.1 Changed since October
| item | October | now (file:line) |
|---|---|---|
| Engine / URP | 6000.3.10f1, URP 17.3 (audit §1.8, budgets §1) | 6000.6.3f1, URP 17.6.0 (`Packages/manifest.json:4`; package `@327d93d1cbc8`) |
| Renderer features | SSAO only (audit `:55-56`) | SSAO + "FrontRooms Metal Glass RT" (`FrontRooms_URP_Renderer.asset:27-29`, `:84-97`); plugin now universal (D.3) |
| Quality presets | none; 6 levels on one asset, differing only in aniso / LOD bias / vsync (audit `:58-62`) | `FrontRoomsQuality.cs` Low/Medium/High/Ultra (D.6); the 6 levels still all point at one asset |
| Additional-light shadow atlas | 4096 (audit `:40`) | 4096 in the asset, 8192 written at runtime on desktop at SHADOWS HIGH, i.e. also at ULTRA (`FrontRoomsQuality.cs:205-208`, `:236`) |
| Reflections | no probe in any scene, Skybox reflection 0.3 with no skybox (audit `:71-77`) | 4 baked HDR zone cubes as the custom reflection, linear 0.45-0.5 (`FrontRoomsZoneReflection.cs:43-48`); probes only in stairwell / Run! rig / structures (D.9) |
| Ambient code | `FrontRoomsLook.ApplyAmbient` never ran in the game (audit `:14`, `:71`) | called in Awake (`FrontRooms3DGame.cs:603`) and in `ApplySceneLighting` (`:556`) |
| Kit LODs | 54 FBX, no LODs, no LODGroup prefabs (audit `:136`) | 255 FBX, 228 with LOD1, 127 with LOD2; heights from sidecars; no cross-fade (E.3) |
| Map troffer | primitive white cube on URP Lit, constant emission (audit `:97`, `:14`) | L03 kit: lens + housing LODGroup + soil on desktop (`FrontRoomsTrofferKit.cs`); zone families pendant / wrap / strip (`FrontRoomsLampKit.cs`) |
| Wallpaper textures | three NPOT 2048x3072 maps uncompressed, 96 MB (budgets `:18`) | no 2048x3072 left; wallpaper maps 2048x2048 POT (E.1). 12 NPOT prop maps remain |
| Surface textures | 166 PNG, no per-platform overrides (audit `:134-135`) | 316 PNG; overrides WebGL 103 / iOS 40 / Android 31 / Standalone 1 (E.1) |
| Surface materials | 86, 83 on FrontRooms/Surface (audit `:81`) | 230 in Resources/Surfaces, 219 on FrontRooms/Surface; 239 materials in Resources (C). MEASURED.md still quotes "83 of 86": stale |
| Corridor content | flat lens, no door kit, no TV, no outlets | L03 troffers, double-door kit with HW-DA1 plates and lites, title TV, instanced outlets (A.2) |
| Corridor frame | 816 batches, 69 SetPass, 9,038 tris, 416 shadow casters, 24 visible lights (9 shadowed) (budgets `:64`) | 833-952 draw calls, 74-104 SetPass, 153k-224k tris, 308-507 shadow casters, 20(6)-35(13) lights (MEASURED.md `:11`; counters are "draw calls", October's were "batches") |
| Maze frame | 2,435-4,828 batches, 874-1,086 SetPass, 113k-723k tris, 86-91 lights (budgets `:55-63`) | 4,842-7,379 draw calls, 1,176-1,564 SetPass, 0.68M-1.28M tris, 2,240-4,542 shadow casters, 87 lights (8-15 shadowed) (MEASURED.md `:29`) |
| Live renderers | title ~300, map ~3,400 (budgets `:70`) | corridor ~125 per slot, maze ~12.8k (MEASURED.md `:22`, `:34`) |

### F.2 Still true
- Forward+, MSAA 4x, HDR, render scale 1, SRP Batcher on, dynamic batching off, legacy light probes (none placed), probe blending off, box projection off, cookies 2048, light layers off, GPU Resident Drawer off, depth priming off, opaque texture off, Native RenderPass off (D.1, D.2; audit `:33-50`).
- FrontRooms/Surface: SH-only indirect, no LIGHTMAP_ON / APV include, no box-projection keyword, no DBuffer, no LOD_FADE_CROSSFADE, DepthNormals from vertex normals (C; audit `:83-88`).
- The directional "Soft ambient direction": intensity 0.16, soft shadows, strength 0.18, main light with a 2048 x 2-cascade 40 m map (B.6; audit `:103-105`). MEASURED.md now shows its shadow has no visible effect.
- Map lamps: 162 / 96 degree spots, range 10 / 12 m, 1 in 3 may shadow within 9 m, lit within 16 m, default 1024 tier, 6.2 cm texel bias (B.3; audit `:92-112`).
- Lens MPB every frame for every fixture on desktop; shell MPB for `_CeilingHeight` (B.4; budgets `:68`).
- Stream: 12 spots per room, 4 shadowed, 5 slots (A; audit `:99-101`). Beams additive, ZWrite Off, no depth read (C; audit `:114`).
- Frustum culling only in the maze; "only 0.2-12 % of in-frustum objects visible" was measured in October and not re-measured; MEASURED.md's `rooms_portal` row (A, corridor) shows the same kind of gap in the title (846 -> 419, 833 -> 244, 952 -> 480 draws with no image change).
- Camera: FOV 76, near 0.06, far 80 / 46 m, AA None + MSAA, dithering, 60 fps target (D.5; audit `:120-121`). Post: ACES, bloom 1.05 / 0.55 / 0.72 HQ, WB +9 / -7, contrast -6, saturation -8, exposure +0.15, lift +0.035, vignette 0.26, grain Medium3 0.22, CA 0.06, distortion -0.04 (D.4; audit `:122-128`).
- All quality levels on one URP asset; Standalone default level 5, WebGL 3 (D.6; audit `:60-62`).
- Mip streaming off everywhere (E.1; audit `:135`).

### F.3 Disagreements to settle in the measure stage
- SSAO prepass: October measured batches = 2 x in-frustum + shadow casters, the x2 being the DepthNormals prepass "that exists only for SSAO" (budgets `:15`, `:72`). MEASURED.md: SSAO off removes only 4 draws. In URP 17.6 a depth prepass is forced either by a normals request (SSAO DepthNormals) or by any depth read before opaques (the Glass RT trace at AfterRenderingPrePasses, `FrontRoomsMetalGlassRTRendererFeature.cs:50`, `:55`) (`UniversalRendererRenderGraph.cs:1584-1595`). MEASURED.md toggled SSAO and Glass RT one at a time, so the prepass may survive each single toggle on the Mac. A both-off variant is needed before saying the x2 is gone. Whatever the answer, the prepass is from vertex normals only (C).
- LOD cross-fade dithering: the brief says Bayer; the asset says BlueNoise (D.1).
- Surface material count: MEASURED.md says 83 of 86; it is 219 of 230 (C, F.1).
- The October audit expected 16+ shadowed lamps to shrink in the 4096 atlas (audit `:112`, UNVERIFIED there); the code now writes 8192, but URP trims the target to the tiles used (D.6), so the extra size only matters when more than 16 tiles are requested.

## Waste candidates (facts only)

Each item: what costs, where, and the measured effect where MEASURED.md (clone, 11-04, 1920x1080, desktop ULTRA; image change noise floor 0.00-0.07 %) has one. No designs here.

GPU / draw cost with no visible result measured:
- **Stream rooms behind shut doors stay live.** Slots c+1 (before its door opens), c+2, c+3 and c-1 keep every renderer active and casting; the only deactivation is `EndStreamAt` (`FrontRoomsRoomStream.cs:560-568`; seals `:1223-1224`; frustum culling only, A.4). MEASURED `rooms_portal`: 846 -> 419, 833 -> 244, 952 -> 480 draws; shadow casters 308 -> 203, 348 -> 103, 507 -> 259; image change 0.02 / 0.07 / 0.00 % (noise).
- **Lights left on behind the shut rear door.** Room c-1 keeps 12 lights, 4 shadowed, from the moment its door shuts (`:837-845`) until recycle resets them (`:1009`). MEASURED `behind_lights_off`: 833 -> 716 and 952 -> 836 draws, lights 24(9) -> 13(5) and 35(13) -> 24(9), image change 0.07 / 0.00 %.
- **Directional fill shadow.** "Soft ambient direction" casts soft shadows at strength 0.18 (`Assets/Scenes/FrontRooms3D.unity:2276-2297`) and pays the 2048 x 2-cascade 40 m main-light map (`FrontRooms_URP.asset:44-46`, `:57-59`) under a ceiling everywhere. MEASURED `dirshadow_off`: -678 to -851 draws in the maze, -103 to -137 in the corridor; the removed draws equal the removed shadow-caster draws (2,327 -> 1,491, 2,240 -> 1,562, 4,542 -> 3,691); image change 0.00-0.08 %.
- **Stream troffer shadows (title only).** 4 soft-shadowed spots per room (`FrontRoomsRoomStream.cs:1210`) at the default 1024 tier, 162 degree cone = 6.2 cm texel (B.3). MEASURED `addshadow_off` in the corridor: 846 -> 675, 833 -> 588, 952 -> 566 draws, image change 0.04 / 0.07 / 0.00 % from eye-level views. In the maze the lamp shadows ARE visible (1.9 / 2.1 / 31.8 %), so this holds for the corridor only.
- **Fake beams.** 60 additive transparent renderers, each with its own MPB (`FrontRoomsRoomStream.cs:1192`, `:2013-2020`); peak output 0.012 x 0.82 = 0.0098 of the lamp colour (C). MEASURED `beams_off`: -6 to -11 draws, -8 to -18 SetPass, image change 0.02 / 0.26 / 0.09 % (above noise in one view only).
- **Lamps behind the camera are visible lights.** All lamps lit within 16 m pass URP culling because the 162 degree cones reach the frustum from behind: 87 visible lights per maze view (MEASURED.md `:34`; `FrontRoomsMapWorld.cs:3719`, `:3855-3858`).

CPU and batching:
- **Lens MPB every frame for every lamp.** `TickFixtures` writes `_EmissionColor` + `_FR_LampDip` to every lens (and pendant glow) of all ~1,600 fixtures each frame on desktop, lit or not, steady or not (`FrontRoomsMapWorld.cs:3846-3866`); the WebGL path already skips unchanged far lamps (`:3869-3906`). 10-03: 1.5-2.7 ms per frame. Unity's documented rule (not re-read here) is that a renderer with an MPB drops out of the SRP Batcher; MEASURED counts ~935 in-frustum renderers with an MPB in the maze.
- **Shell MPB for a per-height constant.** Every shell block renderer gets its own MPB holding `_CeilingHeight` (`:2792-2797`), whose value is one of 3 height classes plus the chunk's y (`:2775-2776`, `:2741`).
- **No batching path engages.** 0 static, 0 instanced, 0 dynamic batched draws; draw calls 5.6-12x SetPass (MEASURED.md `:37`). GPU Resident Drawer cannot engage: `m_BrgStripping: 0` (`ProjectSettings/GraphicsSettings.asset:49`) and FrontRooms/Surface has no DOTS instancing (C) (MEASURED.md `:38`).
- **Wall-match outlet plates are single draws.** Papered / painted plates use `Graphics.RenderMesh` one instance at a time because the wall material has no instancing (`FrontRoomsWallFixtureRenderer.cs:8-10`, `:1138-1146`; instanced buckets use `RenderMeshInstanced`, `:1133`).
- **Glass RT when no glass is in view.** Main thread 0.17 ms per frame facing away from any window (`Documentation/research/glass/rt/30_final.md:163`); lens emission read every frame within 34 m (`FrontRoomsGlassRTSystem.cs:37-41`). Mac only.

Memory and build:
- **Hidden variants built in every title slot.** 5 profile variants per slot (25 per pool), the Office dressing among them, while the title only shows Lobby (`FrontRoomsRoomStream.cs:1571-1579`, `:349`). Inactive, so no draw cost; memory and build time only.
- **Duplicate per-instance meshes.** 60 identical beam meshes (`:1273`), 30 reveal quads (`:2101`), 10 lite-glass meshes (`FrontRoomsDoubleDoorKit.cs:321`) per pool.
- **NPOT textures with npot None.** 12 prop maps in Surfaces (`Prop_KeyboardKeys_*` 2048x768, `Prop_PlateBusinessHours_*` 1832x612, `Prop_ScreenCRT_*` 1024x768, `Prop_VendingHeader_*` 1024x96), 234 in WorldText, 23 in Print (18 also set uncompressed). ESTIMATE by the 10-03 rule: `Prop_KeyboardKeys_A` = 2048 x 768 x 4 B x 4/3 = 8.0 MiB uncompressed vs 2.0 MiB as BC7.
- **No mip streaming.** Off on all 1,467 textures (E.1); the six 4096x4096 maps are 21.3 MiB each with every mip resident.
- **LOD distances doubled at Ultra.** Kit LOD heights are authored for lodBias 1 (`FrontRoomsKitImporter.cs:93-110`); the desktop default level Ultra runs lodBias 2 (`ProjectSettings/QualitySettings.asset:301`, D.6), so LOD0 pieces such as the 2,238-triangle push plates (8 per stream door) and 2,880-triangle outlets stay at LOD0 to twice the authored distance. Visible effect not measured.
- **Variants with no user.** LOD cross-fade is on in the asset (`FrontRooms_URP.asset:33`) but every kit LODGroup has `fadeMode = None` (`FrontRoomsKitImporter.cs:111`, `:118`) and FrontRooms/Surface has no LOD_FADE_CROSSFADE; "strip unused post-processing variants" is off (`Assets/UniversalRenderPipelineGlobalSettings.asset:22`, `:29`). Build size and compile time only.
- **Depth prepass from vertex normals.** Whatever keeps it (F.3), the DepthNormals pass draws every opaque again with geometric normals only (`FrontRoomsSurface.shader:1092-1107`), so it buys SSAO no surface relief.
