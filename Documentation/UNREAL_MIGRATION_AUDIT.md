# FrontRooms Unity → Unreal 全量迁移审计

审计日期：2026-10-04（PDT）  
审计对象：`Frontrooms3D/`  现有 Unity 工程  
Unity：`6000.3.10f1`；渲染：URP `17.3.0`  
Git HEAD：`70644f0`（与 `origin/main` 同步；工作树另有未提交改动）

## 结论先行

这个项目可以迁移到 Unreal，但不能通过“导入 Unity 场景”完成。它的场景、房间、地图、道具碰撞、Relay、HUD 和大部分音频触发都是运行时代码生成的；真正需要迁移的是一套确定性游戏系统和一批原始资产。

你已选择 **桌面优先（macOS/Windows）**，并要求 **行为等价，同时利用 Unreal 把视觉升级到更高纹理和电影质感**。因此目标不是逐像素复刻 URP，而是把玩法、节奏、可读性、状态时序和交互保持在 golden baseline 内，再用 UE 的材质、灯光、反射、雾和后期建立更高规格的画面。建议建立独立的 UE5 项目，用 **C++ 保留确定性核心，Blueprint/UMG/Data Asset 负责编辑和表现**。Unity 工程继续作为行为基线，直到 UE 版完成逐层验收。当前机器没有发现 Unreal Editor/UnrealEditor 可执行文件，只有 `/Applications/Epic Games Launcher.app`，因此本轮完成的是源审计、迁移规格和验收契约；没有虚构一个已经导入或打包成功的 Unreal 项目。

迁移期间的硬规则是：**任何 Unity side 的更新都必须同步评估并更新 Unreal side。** Unity 的 C#、数据资产、JSON sidecar、地图常量、输入、音频、材质合同、资产、验证报告和文档都属于同步范围；Unreal 可以在表现层升级为电影质感，但不能悄悄分叉行为、数据、碰撞、状态时序或事件触发。详细流程见 `Migration/UNITY_UNREAL_SYNC_POLICY.md`。

“完整迁移”必须先锁定发行目标：UE 适合 Windows/macOS/Linux 桌面和移动端；现有 WebGL 不能直接作为 UE 的同一目标继续交付。若浏览器版本是硬要求，应保留 Unity WebGL 或另做 WebGPU/Pixel Streaming 路线，不应把它写成 UE WebGL 已完成。

## 1. 审计边界和当前状态

### 1.1 权威源

- 当前唯一 Unity 源工程：`Frontrooms3D/`。
- 启动场景：`Assets/Scenes/FrontRooms3D.unity`，`EditorBuildSettings.asset` 只启用这一场景。
- 关卡配置：`Assets/Levels/FrontRoomsLevel0.asset` 和四个 `Assets/Levels/Modules/*.asset`。
- 运行逻辑入口：`Assets/Scripts/FrontRooms3DGame.cs`、`FrontRoomsRoomStream.cs`、`FrontRoomsMap/`。
- 不能把根目录的 `Builds/`、`Library/`、`Temp/`、WebGL 输出或旧的 2D MVP 当作源。

### 1.2 工作树风险

`git status` 当前不是干净状态。已修改/未跟踪内容包含：

- `FrontRooms3DGame.cs` 的 InputSystem、移动端、自动基线和 Relay 噪声路径；
- `FrontRooms3DGame.Baseline.cs`、`FrontRooms3DGame.Mobile.cs`；
- `FrontRoomsMap/FrontRoomsRelayDirectorPolicy.cs` 及测试；
- `Scripts/Input/`、移动端计划和基线脚本；
- Relay、触控、可视聊天和交互研究文档。

审计后工作树又出现了 `Assets/Scripts/Office/FrontRoomsOfficeKit.cs`、`Packages/manifest.json` 的修改，以及 `Assets/Scripts/Media/`、`Assets/StreamingAssets/FrontRooms_Ad_01.mp4` 等新内容；最终迁移清单必须以冻结时重新执行的 `git status` 为准。`com.unity.modules.video` 和 MP4 不能漏进媒体清单。

迁移前必须把这批差异标为 `migration-freeze`，逐项决定进入 UE 的版本。不能只 checkout HEAD，也不能把所有未提交实验代码自动并入。当前工作树状态的证据是 `git -C Frontrooms3D status --short`。

### 1.3 已有证据和不能过度宣称的内容

已有自动证据：

- `Logs/FrontRooms/level-verification.json`：8/8 关卡数据检查通过；
- `Verification/map-verification-latest.json`：100/100 seed 的网格/可达性检查通过；
- `Verification/relay-nav-test.json`：60/60 Relay 导航试验通过；
- `Verification/race-slice-latest.json`：抽象竞速图的结构检查通过；
- `Verification/main-autopilot/report.json`：seed 2554 的自动流程被捕获，平均约 58.7 FPS，但存在约 1440 ms 的地图交接尖峰；
- `VALIDATION.md` 明确写着，headless 第一人称路线不能可靠退出，人工完整游玩和 Windows 构建没有完成。

这说明数据层和一部分测试层可迁移，不能证明 Unity 当前产品已完成，也不能直接当作 UE 运行时通过。`Documentation/VERIFICATION_LOG.md` 仍记录了玻璃、门、钥匙、Relay 破门、G14 RT 等 FAIL/FLAG/WAIT-RED 项；这些应作为 UE 的回归清单，而不是被当成已完成内容。

### 1.4 已发现的陈旧入口

- 根目录 `Verification/project-import.log` 仍指向已经弃用的 `FrontRooms3DMVP`，不能作为当前工程导入证据；
- `README.md`、`LEVEL_DESIGN_GUIDE.md`、`UNITY_EDITOR_WORKFLOW.md` 的部分段落仍引用不存在的 `Assets/Scripts/FrontRoomsLevel.cs`；当前实际关卡入口是 `FrontRoomsLevelProfile`/`FrontRoomsMapWorld`；
- `ProjectSettings/ProjectSettings.asset` 仍有旧 2D template metadata 和不存在的 `Assets/Scenes/RedScene.unity`；
- `Assets/Editor/FrontRooms3DBuild.cs` 的旧 macOS 路径会把 URP pipeline 置空，只有 CloudBuild 路径保持 URP；迁移基线应以 CloudBuild 配置或修正后的构建脚本为准；
- `Documentation/WEBGL_BUILD.md` 仍含旧本机路径，不能作为可复现的发布命令。

这些陈旧入口不改变 UE 目标，但会让“从哪里迁移、哪个 build 是基准”产生误判，应在 P0 冻结时单独清理或标注。

## 2. 工程规模和资源盘点

### 2.1 代码和工具

| 区域 | 当前规模 | 迁移含义 |
|---|---:|---|
| `Assets/Scripts` | 63 个 `.cs`，约 26,249 行（含 `.meta`/其余文件约 135 个） | 需要 C++ 核心重写；不能按 MonoBehaviour 一对一机械翻译 |
| `Assets/Editor` | 45 个 `.cs`（含 `.meta`/其余文件约 116 个），约 14,921 行 | Level Designer、验证器、构建器要改为 UE Editor Utility/Commandlet/Automation |
| `Packages/manifest.json` | InputSystem 1.18、uGUI 2.0、Physics、JSON、URP 17.3 等 | 没有 Addressables、VFX Graph、Cinemachine 或网络包；迁移面集中但运行时自建系统很多 |
| 场景 | 3 个当前作者场景，另有 `_Recovery/0.unity` 恢复场景 | 场景本身很薄；主要内容运行时生成 |
| 运行生成 | Title 房间池、地图 chunk、灯、门、窗、道具、钥匙、Relay | 必须先迁移数据和生成器，再做 UE Level/Blueprint 表现 |

### 2.2 资产

`Assets` 中约 2,154 个文件（含 `.meta`）。无 `.prefab`、`.anim`、`.controller`；这点很关键，因为不存在可直接批量转换的 prefab/Animator 图。

| 类型 | 数量/内容 | UE 处理 |
|---|---:|---|
| FBX | 123（10 个 Office 模型、约 113 个 Kit 道具） | 按 FBX 2020.2、单位/轴向、LOD、碰撞重新导入；不能只拖入 Content Browser |
| JSON sidecar | 114 | 解析为 `UPrimaryDataAsset` 或导入后的 DataTable，保留 bounds、collider boxes、anchors、supports、pile、LOD 元数据 |
| PNG | 209；其中约 166 张表面纹理 | 按 A/N/S/E/M/P 后缀分组设置 sRGB、法线和通道语义；A/N/S 不能批量按同一规则导入 |
| MAT | 91 | 84 个使用 `FrontRooms/Surface`，4 个 `FrontRooms/Glass`，另有 URP Lit/builtin；全部重建为 Material Function + Material Instance |
| EXR | 4 个 zone reflection cubemap | 转为 UE Reflection Capture/Lumen 输入，并重写跨 zone 混合 |
| 字体 | Resources 中 3 个运行时字体；Assets/Fonts 另有参考字体集合 | UE Composite Font/Slate 字体资产；保留 OFL 许可和 fallback |
| SVG/PNG 品牌 | 14 个 glyph/lockup SVG、logo PNG | UMG/Slate 分层 logo，重做 trailing-S 动画和白色材质 |
| Shader | Surface、Glass、Reflection、Volumetric、WhiteLogo、RT prepass/composite 等 | URP shader 不能自动变成 UE 材质；逐项重写节点/Custom HLSL |

`FrontRoomsKitLibrary` 的真实材质映射来自 FBX 子材质名和 `Resources/Surfaces/<slot>.mat`，而 `materials_manifest.json` 当前为空数组；UE 导入脚本不能依赖这个 manifest，应该以 FBX slot + JSON sidecar 建立显式映射。

当前 `FrontRoomsSurface` 还引用 `_FR_Print` Texture2DArray，但 checkout 中没有 `Assets/Resources/Print/`、`FR_Print_HardEdge.asset` 或 `.print.json`；相关 staging 只在 `Tools/print/unity_staging`。迁移前必须固定 print slice、编码、mip 和 BC5/R8G8 规则，否则“更高纹理”会建立在缺失源数据上。

`Tools/Blender` 仍保存 Blender 源文件和导出脚本；`Tools/lookdev/cc0_src` 本地约 245 MB、65 个 ZIP 且未被 Git 跟踪。若迁移要求可重建高规格 lookdev，P0 必须把这些外部源、许可证和导出版本固定成独立资产包，不能只把已经导出的 FBX/PNG 视为完整源。

Office/Kit 的部分实例会在 Unity 运行时强制 `scale * 100`。UE 使用厘米单位（1 m = 100 uu），必须通过导入预设和一组尺⼨ golden test 验证，不可凭肉眼修正。

## 3. 当前运行时架构

### 3.1 主流程

`FrontRooms3DGame` 是 2,789 行的 orchestrator，工作树还有 Baseline/Mobile partial。Phase 为 `Title → Playing → Paused → Caught`。Awake 创建字体、HUD、音频、标题房间和预热地图；Update 读输入、驱动 Player、地图、Relay、镜头、HUD 和事件。

UE 拆为：

- `AFrontRoomsGameMode`：run phase、seed、胜负和重试；
- `AFrontRoomsPlayerController` + `AFrontRoomsCharacter`：鼠标/键盘/触控、移动、冲刺、体力、眼高；
- `UGameInstanceSubsystem`：设置、日志和未来存档 schema；
- `UFrontRoomsInteractionComponent`：line trace、门/窗/钥匙交互；
- `UFrontRoomsCameraDirector`：base eye、head-dip、glass/vault/caught shots；
- `UGameplayMessageSubsystem` 或项目内 typed dispatcher：替代 C# static events。

### 3.2 Title 房间流

源码当前固定房间池以 `FrontRoomsRoomStream` 为准（读取源码中的 `MaxRooms`，不要沿用旧 README 的 3-room 描述）。流程是 Lobby → Shift → Office → Run → Exit，门约 0.9 秒，长距离时做约 192 m 的坐标 rebase，Space/Enter 后在门口把控制交给玩家。

UE 实现为 `AFrontRoomsRoomStreamActor` + 五个可回收 slot。C++ 决定 readiness、序列、rebase、handoff；Level Sequence/Timeline 只负责门、灯和 logo 表现。必须先写 title lifecycle 测试，再制作美术版本。

### 3.3 Level 0 地图和模块

`FrontRoomsMap` 是 3 m cell、24 m chunk（8×8）的 deterministic infinite grid；`FrontRoomsMapWorld` 按 budget 生成 shell、地板、天花、碰撞、灯、门、窗、钥匙和 Office/pile dressing。四个模块是：

- `Low_Storage_2x3`；
- `L0_WaitingRoom_4x3`；
- `Office_Bullpen_4x4`；
- `Tall_PillarHall_6x5`。

UE 结构：

- `FGridCoord/FMapHash/FMapChunk/FZoneInfo` 等纯数据结构；
- `UFrontRoomsLevelProfile` / `UFrontRoomsRoomModule`（Primary Data Asset）；
- `UFrontRoomsWorldSubsystem` 负责 seed、chunk 生命周期和状态表；
- `AFrontRoomsChunkManager` 负责 ring/buildRadius、异步生成、Drop/Rebuild/Shift；
- 静态 shell 优先 Static Mesh/HISM，必要时 ProceduralMesh；道具优先 HISM；碰撞按 sidecar 生成 simple boxes；
- PCG 仅用于装饰，不能让 PCG 改变 gameplay graph 或 seed 结果。

要保留 `MapHash` 的 salt/revision、模块 rotation、tier 权重、KeySpot/RelayEntry marker 和 lamp override。每个 seed 的 JSON golden 输出必须在 Unity/UE 之间一致。

### 3.4 玩家、交互、镜头

当前合同：眼高 1.62 m，walk 3.2 m/s，run 5.5 m/s，Reach 2.4 m，冲刺约 5 秒，恢复延迟 1 秒；WASD/arrows + mouse、Shift、E、Space/Enter、Esc、O、R、S；门/玻璃/钥匙/笔记/暂停/结果页都由同一输入 facade 读入。

UE 使用 Enhanced Input：`IA_Move`、`IA_Look`、`IA_Sprint`、`IA_Use`、`IA_Start`、`IA_Pause`、`IA_Settings`、`IA_Restart`、`IA_ShotBack`，桌面和触控分别用 Mapping Context。不要把旧的 `Input.GetKey*` 式散读逻辑搬过去。

门、窗、钥匙分别做 `AFrontRoomsDoor`、`AFrontRoomsWindow`、`AFrontRoomsKey`。门的单向/锁定/钥匙/Relay 破门状态机要保留；玻璃的 hold/tap、crack stage、0.6 s vault 和运行内 break record 要保留。Motion Warping/Montage 可作为表现层，曲线和碰撞状态仍由 C++ 驱动。

### 3.5 Relay

当前活跃路径是 `FrontRoomsMapHunter`（约 1,109 行）+ `FrontRoomsRelayRig`；旧 `FrontRoomsHunter` 线性 brain 与 `FrontRoomsMaze`/`FrontRoomsRace` 需要先标 legacy。Relay 状态包含 Dormant、Listen、Wander、Hunt、Search、Chase、BreakDoor；路径是自定义 cell BFS + 0.25 m detour，含 sight/hearing、door rule、wall/glass、body-fit、leash 和 relay entry。

UE 第一版必须保留这套自定义逻辑为可测试的 `UFrontRoomsHunterBrain`，由 `AFrontRoomsRelayCharacter` 执行移动。Behavior Tree/StateTree/EQS/NavMesh 只能做表现或优化；直接换成 NavMesh 会改变穿门、听声、失去视线和卡位行为。现有 primitive rig 没有 Animator/clip，需用 Skeletal Mesh + Control Rig/Anim Blueprint 或先用组件化 primitive 复刻 procedural gait；最终生产 rig 的 24–36 bone、Idle_Listen/Walk/Run/Stagger 目标属于后续资产阶段。

### 3.6 UI、字体、输入设备

Unity UI 是运行时 UGUI + UI Toolkit 混合，包含 room/threat HUD、crosshair、prompt、hold bar、stamina、key、captions、pause/settings/caught、触控 safe-area 和分层 logo。UE 用 UMG/CommonUI（Slate 只用于底层字体/矢量支持）拆成 `WBP_Title`、`WBP_GameHUD`、`WBP_PauseSettings`、`WBP_Caught`、`WBP_TouchControls`。字体和 1920×1080 / 72 px margin / 12-column rhythm 作为设计 token，不要在迁移时把 HUD 改成默认 UE 样式。

### 3.7 音频

FMOD 是实际音频路径；Unity `AudioManager.m_DisableAudio=1`，legacy AudioSource 不应迁移。源项目有 `FMOD/FrontRooms/FrontRooms.fspro`、5 个 banks（Master/Ambience/Music/SFX 等）以及 31 个事件、3 个全局参数（Tension/Zone/Tier）、多路局部参数和总线。

优先使用 FMOD Studio UE plugin：保留 `.fspro`/源素材，按 UE 平台重建 banks，再由 `UFrontRoomsAudioSubsystem` 把 typed gameplay events 接到 `StudioEventInstance`。不能把 Unity 的 `.bank` 当普通音频拖入 UE，也不能搬 Unity FMOD native binaries。若改用 MetaSounds，必须手工重建 31 个 event、bus、参数、衰减、遮挡、loop 和 snapshot，工作量单独估算。

### 3.8 渲染和原生插件

URP Forward+、HDR/MSAA、fog、post、zone cubemap、Surface/Glass/Reflection/Volumetric shader 要全部重做为 UE Material/Material Function、PostProcessVolume、Reflection Capture/Lumen 等。

`NativePlugin/FrontRoomsMetalGlassRT` 是 macOS 专用 Objective-C++/Metal + dylib，不是可移植的 UE plugin。审计记录它在当前 Unity main 中对窗口玻璃 coverage 为 0，却增加了约 1.56 ms p50 / 5.66 ms p99 主线程和 1,271 次 mesh/BLAS 上传（`VERIFICATION_LOG.md` VL086/VL087）。UE 基线不要依赖它；先用 Lumen/Reflection Capture/屏幕空间方案完成玻璃可读性，再把硬件 RT 作为可选质量档并重新测量。

## 4. Unity → Unreal 映射表

| Unity | Unreal | 迁移方式 |
|---|---|---|
| `MonoBehaviour` orchestrator | GameMode/GameState/Character/Subsystem/ActorComponent | 拆职责，保留事件合同 |
| `ScriptableObject` profile/module | `UPrimaryDataAsset` / DataTable | C# 字段导出 JSON，再由 Editor Utility 生成资产 |
| `Resources.Load` | SoftObjectPath / Asset Manager / Primary Asset | 显式注册，不能继续隐式按字符串搜索 |
| `FrontRoomsMapWorld` | WorldSubsystem + ChunkManager | 先 C++ 确定性，再 HISM/ProceduralMesh 表现 |
| `FrontRoomsRoomStream` | 五 slot RoomStream Actor + Level Instances | 固定池、门 readiness、rebase、handoff 全部由代码控制 |
| CharacterController | Character + CharacterMovementComponent | 单位、碰撞半径、step offset 逐项对齐 |
| 自定义 hunter BFS/A* | 可测试 C++ brain + Relay Character | NavMesh 仅优化，不替换 golden 行为 |
| UGUI/UI Toolkit | UMG/CommonUI/Slate | 保持 HUD 信息层级、字体和安全区 |
| InputSystem facade | Enhanced Input | 输入 trace 作为跨引擎 golden |
| FMOD Unity | FMOD Studio UE plugin | 重建 UE banks；不搬 Unity plugin binaries |
| URP Surface/Glass | UE Material Functions/Instances | 按 A/N/S/E/M/P 语义导入纹理 |
| URP post/fog/reflection | PostProcessVolume/Lumen/Reflection Capture | 区域过渡需要自定义 blend |
| Metal dylib | UE optional renderer path | 不进入 baseline；重做并单独验收 |
| macOS/WebGL build scripts | UE packaging/automation | WebGL 需保留 Unity 或另做浏览器路线 |

## 5. 分阶段完整迁移路线

每个阶段都必须遵守 Unity → Contract Export → Unreal → 双端验证的同步闭环。Unity 侧新改动如果没有对应 Unreal 记录，状态只能是 `Pending Unreal Sync`，不能进入新的 golden baseline。

### P0：冻结和基线（必须先做）

1. 把 Unity 工作树提交或打 tag，命名为 `unity-migration-freeze`；列出 HEAD 与未提交差异。
2. 排除 `Library/Temp/Logs/UserSettings/Builds` 等生成物，只保留源码、资产、FMOD 源、验证报告和工具。
3. 固定 golden seeds：`2554`、`20388`、默认 profile seed `20261001`；导出 8×8 chunk JSON、module selection、edge/key/lamp state、title lifecycle、input trace、Relay state trace、门/玻璃 trace、FMOD event trace。
4. 把现有 FAIL/FLAG/WAIT-RED 分为“必须等价”和“明确重做”，不要在迁移中掩盖问题。

### P1：UE 空项目和数据层

1. 安装并锁定团队 UE5 版本；建立独立 `FrontRoomsUE.uproject`，不覆盖 Unity。
2. 创建 `Source/FrontRooms` C++ 模块、`Content/FrontRooms/{Core,Maps,Modules,Props,Materials,UI,Audio,FX}` 目录。
3. 写 JSON 导出器：`FrontRoomsLevelProfile`、四个 `RoomModuleData`、114 个 sidecar、Race spec、设置和运行时常量。
4. 写 UE Data Asset importer，建立 asset name → mesh/material/collider/anchor/support 的显式索引。
5. 先跑 Commandlet/Automation：同 seed 的地图 JSON 必须逐字段一致。

### P2：地图、Title 和玩家

1. 先完成 deterministic grid/chunk/generator/validator，再做视觉。
2. 用 HISM/Static Mesh 建 shell、灯、门、窗、key；用 Level Instance 或自定义 chunk pool 做 streaming。
3. 完成五 slot Title、门 0.9 s、rebase、Space/Enter handoff、map readiness/close。
4. 完成 Character、Enhanced Input、sprint/stamina、line trace、pause/retry/caught。
5. 用 Functional Test 复现 door/window/key/vault/locked prompt。

### P3：Relay 和镜头

1. 直译 `FGridCoord`/route/sight/hearing/door break/leash/search 等纯逻辑到 C++。
2. 以相同 seed + 相同输入 trace 对齐 state transition、route cells、catch distance、door blow 时间。
3. 先用组件化 primitive rig 达到行为等价，再导入生产 skeletal rig/Control Rig/AnimBP。
4. 迁移 CameraRig 的 base eye、head dip、glass shot、caught shot 和 FOV/shake。

### P4：资产、材质、音频和 UI

1. FBX 以 1 m = 100 uu、UE 坐标/法线/LOD 预设批量导入；先做 10 个代表 kit 的尺寸和碰撞验收，再批量导入其余 113 个。
2. 重建 Material Functions：world-projected surface、packed A/N/S、emissive lens、glass/grime、zone tint、logo。
3. 导入三套运行时字体和 logo 分层，完成 UMG 状态页面和 touch safe-area。
4. 接入 FMOD UE plugin，重建 banks 并验证 31 events、3 globals、local parameters、3D occlusion/listener。
5. 重新做 zone reflection、fog、post、volumetric beam；不要把 Unity RT 当硬依赖。

### P5：平台、性能和发布

1. 桌面首发：Windows 优先打包，macOS 开发/打包；UE 官方打包文档覆盖 Windows/macOS/Linux 桌面目标。
2. 移动端只在触控 UI、FMOD bank、内存、纹理和动态灯预算通过后做 iOS/Android。
3. 浏览器目标单独决策：保留 Unity WebGL，或做 Pixel Streaming/WebGPU 客户端；不把“UE WebGL”列为默认交付。
4. 用 Unreal Insights、Stat Unit、GPU Visualizer 和 FMOD profiler 建立桌面/移动预算：目标 60 FPS，交接和 chunk build 不得出现秒级尖峰。

### P6：并行验收和弃用 Unity

1. 同 seed、同输入 trace、同 resolution 分别截图 Unity/UE；行为允许按帧容差，地图/数据要求字节级一致。
2. 人工逐项走 Title → Shift → Office → Run → Exit、钥匙门、玻璃、Relay 听声/追逐/破门、pause/retry/caught、字幕和触控。
3. 只有 UE 桌面包、音频、性能、崩溃日志和回归套件全部通过，才把 Unity 标成 legacy；不要先删除 Unity 源。

## 6. 验收标准

### 数据和确定性

- 同一 seed 的 zone、chunk、module、rotation、edge、key、lamp、marker、branch 输出逐字段一致；
- 100 seed validator 通过：可达性、钥匙/门顺序、无非法边角穿越、模块边界和灯状态；
- streaming 的 Build/Drop/Rebuild/Shift 不改变已记录的 gameplay state；
- title 五 slot、handoff、rebase 事件顺序一致。

### 行为

- 同一输入 trace 的 phase、position、zone、tier、stamina、door/window/key 状态一致；
- Relay state、听声半径、失去视线、search、chase、break-door、catch distance 与 Unity golden trace 在约定帧容差内一致；
- 碰撞、门缝、玻璃 hold/tap/crack/vault、prompt 和镜头锁定均有 functional test。

### 视觉和声音

- 1920×1080 桌面截图至少覆盖 title、五类房间、Office、Run、Exit、door/key/window/Relay；
- 纹理色彩空间、法线方向、单位、LOD、碰撞尺寸、字体 fallback 通过代表 kit 和代表房间检查；
- FMOD 31 event、3 global parameter、bus routing、3D attenuation/occlusion 全部 resolve；
- 玻璃反射以 UE baseline 方案验收，RT 作为可选档；不得以 Unity 当前 RT coverage=0 的路径作为“已迁移”证据。

### 发布和平台

- Windows/macOS 桌面包可以启动、读取 bank、进入完整 route、退出并生成日志；
- 触控 UI 在至少一台真实设备上验证；
- Web 目标明确写成 Unity WebGL 保留、Pixel Streaming 或另做 WebGPU，不能标称 UE WebGL 已完成。

## 7. 当前不可自动迁移/需要人工重做的项目

1. Unity YAML 场景中的运行时生成内容：没有一个安全的“场景导入即完成”路径。
2. C# gameplay/AI/editor code：需要 C++/Blueprint/UMG/Editor Utility 重写。
3. URP custom shaders、UI Toolkit SVG logo 动画、zone reflection blend。
4. Metal Objective-C++/Metal RT dylib。
5. FMOD Unity integration、platform binaries、bank 加载桥接。
6. Procedural Relay rig 和不存在的 Animator clips。
7. Resources.Load 隐式资源系统、JSON sidecar 碰撞和 kit material remap。
8. 现有 WebGL 发布链；UE 不能直接承担同一 WebGL 交付承诺。

## 8. 电影质感升级的边界

视觉升级应发生在行为基线通过之后，并有单独的“表现层预算”，避免用更重的资产掩盖地图或 AI 回归。建议顺序是：

1. 先保留 3 m cell、门/窗/钥匙/Relay 的 gameplay collision 和镜头 framing；
2. 再把灰盒 shell 换成高规格模块化墙、地毯、天花、门五金和 Office kit，补齐真实碰撞、LOD、Nanite/非 Nanite 质量档；
3. 用高分辨率 A/N/S/E/M/P 纹理、Material Functions、局部污渍/湿度/磨损和 decal 层重建墙、地面、玻璃和金属；
4. 用 Lumen/Reflection Capture、体积雾、局部 fluorescent flicker、contact shadow、Post Process 和可选硬件 RT 提升镜头质感；
5. 用 Control Rig/AnimBP 做 Relay 的 Idle_Listen、Walk、Run、Stagger 和门破坏动作；
6. 每一次升级都必须通过“同一输入 trace 下的状态/碰撞/交互不变”和“Windows/macOS 目标帧率、内存、加载尖峰不超预算”两组门槛。

UE 版的电影质感不等于把所有灯改成动态、把所有几何都设成最高档。标题流和地图的 24 m chunk、五 slot pool、16 m light radius、阴影范围、材质实例数、LOD/HLOD 和异步 chunk build 要先形成明确预算；否则会重现 Unity 当前 Office dress 交接约 1.44 s 的尖峰。

## 9. 首个可交付切片

真正开始 UE 实现时，第一切片应限定为：**一个可运行的 UE 桌面 build，保留五房 Title、一个固定 seed 的 Level 0 chunk、第一人称移动/冲刺、一个可开门、一个可拾 key、Relay Listen→Chase、UMG HUD 和一条 FMOD footstep/door 事件**。它通过 P0/P1/P2 的 golden tests 后，再并入其余模块、玻璃、Office dressing、移动端和高质量反射。

这样可以把“完整迁移”拆成可审查的行为闭环；在没有 Unreal Editor、且 Unity 工作树仍有未提交差异时，直接声称已经全部迁移是不可靠的。
