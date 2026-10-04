# FrontRooms3D · iPhone / Android build plan

状态：T0/T1 已落第一版，T2 菜单闭环与 T3 触觉接口已接入，2026-10-04。本文把 Figma 手机交互规格和当前 Unity 工程状态对齐；移动设备验收和 iPhone/Android build 仍未完成。

设计来源：Figma 文件 `0tCbAiVUlrPId3RWd9LRif`，节点 `2528:5403`（`FRONTROOMS · TOUCH CONTROLS · iOS + ANDROID · 1920×1080`）。关键子节点：`2530:5061`（screen masters）、`2530:3828`（Touch / Stick）、`2530:3854`（Touch / Use）、`2528:5439`（Thumb map）、`2528:5446`（Same size in the eye）、`2528:5488`（Build plan）。

## 目前的工程基线

- 权威工程是 `Frontrooms3D/`，Unity `6000.3.10f1`，URP `17.3.0`；启动场景只有 `Assets/Scenes/FrontRooms3D.unity`。
- 现有构建脚本只有 macOS、Windows、WebGL。没有 iOS Xcode 导出、Android APK/AAB 或移动端构建入口。
- `FrontRooms3DGame` 的移动核心读取已切到 `FrontRoomsInput.FrameSnapshot`（移动、视角、冲刺、USE、暂停、开始、重试、shot-back）；设置行触控已直接复用 `FrontRoomsSettings`。
- HUD 仍在运行时创建为 Screen Space Overlay；新增的 `FrontRoomsTouchControls` + `FrontRoomsTouchControlsView` 会在移动运行时创建独立 Touch Canvas、safe-area、浮动摇杆、冲刺 socket、USE 视觉/88 hit、Pause 视觉/44 hit，以及 Title/Pause/Settings/Caught 菜单命中区。
- Android API 33+ predictive Back bridge、旧设备 Escape fallback、暂停/设置/恢复映射已接入；重启确认、设置行点击和 Caught 重试已走同一触控状态机。
- `FrontRoomsMobileHaptics` / `FrontRoomsMobileInteractionEvents` 已接入 USE、冲刺、玻璃、被捕获和菜单语义事件；当前实现是 `Handheld.Vibrate()` 粗粒度基线，原生 Core Haptics / Android `performHapticFeedback` 与设备强度校准仍待做。
- `activeInputHandler: 2` 目前是 Both；Figma 计划要求先建立新的 Input System facade，且 Android 不应继续依赖 Both。
- iOS/Android 播放模块已安装。iOS 目标版本目前为 15.0；Android 最低 SDK 为 25，Target SDK 仍为 0；两端 application identifier、图标、签名/keystore 和商店元数据尚未配置。
- FMOD 已包含 iOS 静态库和 Android 多架构库；移动 FMOD banks、加载时序和真机输出仍未验证。
- FMOD 的 iOS/Android plugin libraries 虽然在 `Assets/Plugins/FMOD/platforms/`，但 `FMODStudioSettings.asset` 当前没有启用 PlatformAndroid/PlatformIOS，FMOD Studio 工程的已构建平台仍是 Desktop；移动 banks 需要单独生成并验证。
- 自定义 Metal glass RT 只针对 macOS Metal；iOS/Android 必须走可接受的玻璃 fallback。不能把桌面 RT 的存在当作移动端已支持。
- 当前 URP 还是桌面高档配置：HDR、MSAA 4x、SSAO、主灯/附加灯阴影和 40m shadow distance。移动质量层要单独降级并实机 profiling；现有关卡基线约束是单房间 LOD0 ≤120k tris（Office）/≤60k tris（其他）、≤120 renderers、≤40 colliders，autopilot 的桌面验收目标是平均 ≥55 fps、p99 frame time ≤33 ms。
- 当前 Android application entry 是 GameActivity；Unity `6000.3.10f1` 与 Figma 计划的 `6000.3.13+` 之间存在 GameActivity 版本和 ANR 风险。升级或改回 Activity 之前，必须在独立分支做冷启动、暂停、返回手势和长时间运行回归。
- 本机 Unity PlaybackEngines 已安装 AndroidPlayer、iOSSupport，bundled Android SDK 也有 API 36；API 36 本身不是安装阻塞，仍需在 PlayerSettings/Gradle 中显式 target 36。

## Figma 要实现的交互规格

### 画面和安全区

- iPhone 横屏基准为 `874×402 pt`（约 `2622×1206 px @3x`）；设计给出的安全区是左 62、右 62、上 0、下 21 pt。运行时仍要以 `Screen.safeArea` 验证具体机型，并保留 home indicator / edge gesture 处理。
- Android 20:9 横屏基准为 `915×412 pt`；安全区不写死，运行时读取并应用。
- iPad 11 横屏基准为 `1180×820 pt`，底部安全区 20 pt；Figma 明确要求 iPad 不显示 haptics 开关。
- 小 HUD 文本维持桌面阅读尺寸；Phone 的 zone/meta/prompt/caption 规格分别是 Source Serif 4 26/26、IBM Plex Mono 11/14、Bayon 17/17、Source Serif 4 17/20。Phone 大标题约为 48/44，正文根据 Figma 的 same-size 规则压缩。

### 游戏中触控层

- 左下是浮动移动摇杆：底圈直径 120 pt；触点落在哪里，摇杆中心就在哪里。Sprint 不是边缘触发，而是上方独立 socket 的 latch；拇指进入上半区才进入 sprint，回拉或抬起结束。进入 sprint 发轻 haptic；shot 中回拉对应桌面 S。
- 右半屏任何没有从 USE 按钮开始的拖动都控制 look。USE 只在准星指向可用对象时显示，视觉直径 72 pt，命中区 88 pt；Open/Shut/Take/Locked/Hold/Tap mode/Pressed 都是显式状态。
- Hold to break glass 的进度环位于 USE 按钮外侧；Tap mode 显示 TAP，并沿用现有 `TapToBreak` 语义。准星手机为 12 pt 点，Figma 组件容器为 24 pt。
- 右上 Pause 视觉 32 pt、命中区 44 pt；Android back 和离开应用也进入 pause。
- 顶部 safe area 内显示 zone、key、pause；底部 hint card 和可选 captions 位于中央，不遮住左右触控区。

### 必须覆盖的状态

1. Calm：移动摇杆、zone、pause、准星、hint card。
2. Door in reach：Walk 摇杆、OPEN DOOR prompt、USE/OPEN。
3. Hold to break glass：Hold prompt、hold bar、USE/BREAK 和外圈进度。
4. Sprint：socket latch、stamina 五段、key readout、captions。
5. Thumb map：左半 MOVE、右半 LOOK、顶部只允许 pause；以安全区和 hit area 可视化验收。
6. Android 20:9 door、iPad 11 door：分别验证运行时 safe area 和尺寸规则。
7. Title、Pause、Settings、Caught：分别支持 TAP TO START、RESUME/SETTINGS/RESTART、可点击设置、TRY AGAIN。

### 设置和辅助功能

Settings 规格包含 HDR Render、Camera Motion、Reduce Flashing、Break Glass（Hold Use / Tap Use）、Captions、Relay Readout、Look Speed、Invert Look、Gyro Look、Stick（Floating / Fixed）、Sprint（Socket / Button）、Controls Size、Controls Opacity。Gyro 默认关闭；iOS Reduce Motion 开启时，Camera Motion 应降为安全模式。Gameplay haptics 与 control haptics 分开，iPad 隐藏 haptics 选项。

## 实施架构

### T0 · facade，保持桌面行为（已落代码，待完整回归）

新增项目内的 `FrontRoomsInput`（运行时 facade），向游戏逻辑只提供：

- `Move`：二维移动意图；
- `Look`：二维视角增量；
- `SprintHeld` / `SprintToggle`；
- `UsePressed` / `UseHeld` / `UseReleased`；
- `PausePressed`、`StartPressed`、`SettingsPressed`、`RestartPressed`、`ShotBackPressed`。

facade 底层使用 Unity Input System actions；桌面、Windows 和 WebGL 绑定到与今天相同的键鼠语义，不能继续把 `Input.GetAxisRaw`/`Input.GetKey` 散落在玩法代码里。迁移范围不只 `FrontRooms3DGame`，还包括 `FrontRoomsMap/FrontRoomsMapWalker` 和 WebGL 音频解锁路径中对 `Input.anyKeyDown`/`GetMouseButtonDown` 的读取。每帧先生成一次 `FrameSnapshot`，再由 Update/UpdateMapPlay/UpdateAim 消费，避免多处重复读取 `WasPressedThisFrame` 改变现有消费顺序；Editor autopilot 直接注入 snapshot。当前 `Assets/Scripts/Input/FrontRoomsInput.cs` 已提供 snapshot、Input System 1.18 actions、virtual snapshot 和 Editor 注入 API，`FrontRooms3DGame` 的移动核心已接线。Unity Editor 域重载/脚本编译未报 CS 错误；完整 Play Mode、autopilot 与 WebGL 行为仍需回归。

### T1 · 核心触控和 safe area（已落第一版，待设备校准）

实现 `TouchInputLayer` 与 `TouchHud`（可以继续由现有运行时 Canvas 创建，但控件必须有明确的 hit rect 和状态机）。浮动摇杆使用 EnhancedTouch 的自定义 touch-id 状态机，不直接叠加多个 `OnScreenStick`；多指释放/同帧新指的行为要在低端 Android 真机回归：

- 左浮动摇杆：touch id、dead zone、clamp、socket latch、抬起清理；
- 右侧 look：排除 USE 起始区域，按 safe area 和左右手模式交换；
- contextual USE：按当前 `aimed` / `aimedHold` 映射 Open/Shut/Take/Locked/Hold/Tap/Pressed；
- pause、start、restart、settings、Android back；
- `Screen.safeArea` 驱动的顶部 HUD、底部 hint/caption 和 hit rect；
- `CanvasScaler` 在 phone/tablet 上以 Figma 的 1× pt 规格校准，而不是把 1920×1080 直接缩放后假定等价。

当前 `Assets/Scripts/Input/FrontRoomsTouchControls.cs` 使用 EnhancedTouch touch-id 状态机，`FrontRoomsTouchControlsView.cs` 创建运行时控件视觉，并由 `FrontRooms3DGame.Mobile.cs` 同步阶段、准星 USE、菜单和 Back 状态。T1 先在 Unity Device Simulator 做布局检查，再在真实 iPhone/Android 上验证双指同时移动和视角；当前尚无设备截图或手势日志。

Android API 36 的 back 不应再依赖 `KeyCode.Escape`；通过 Unity predictive-back 支持或最小 Java bridge 注册 `OnBackInvokedCallback`，只派发 `BackPressed` 给 pause 状态机。

### T2 · 菜单、设置和触控状态（第一版已接入，待设备回归）

把 Title/Pause/Settings/Caught 从键盘专属路径变成可点击 chip 与 row。当前已接入 Title start、Pause restart/settings、Settings 六行、Caught try-again、Android Back 和 Pause restart confirm/cancel；Settings 的值变更复用已有 `FrontRoomsSettings` 状态。controls size 80–140%、opacity、left-handed、look speed、invert、gyro、stick（Floating / Fixed）、Sprint（Socket / Button）仍需补齐。任何触控控件在 pointer down/up/cancel、pause、应用失焦和场景重载时都要释放对应 touch id。

### T3 · haptics、gyro 和移动质量层（haptics 第一版已接入）

新增 iOS/Android 条件编译的 haptic adapter，已覆盖 sprint engage、USE pressed/released、glass progress/break、caught 和菜单确认；当前使用 `Handheld.Vibrate()` 基线并保留语义事件总线，原生 Core Haptics / Android `performHapticFeedback`、gameplay/control 两个开关和设备强度校准仍待做。Gyro 默认关闭，开启后与右侧 drag look 叠加时要有确定的优先级。Camera Motion、Reduce Flashing 和 captions/relay readout 都必须在移动设备上可关闭或降级。

质量层先建立 Mobile 级别：关闭 macOS-only glass RT，验证标准 URP glass fallback、HDR、MSAA、反射、灯光 tick、纹理尺寸和 shader variant；移动默认应从较低的 MSAA、阴影图集/距离、灯光数量和 SSAO 档位开始，再用设备 profile 决定是否提升。构建时显式选择 iPhone Metal，Android 先 Vulkan、必要时 GLES3 fallback，并检查日志中没有 RT dylib/PInvoke 加载。目标设备先锁定近期 iPhone（Metal）与 Android arm64（Vulkan，必要时 GLES3 fallback）。

### T4 · 构建和真机验证

新增独立的 `FrontRoomsMobileBuild` Editor 入口，分别导出 iOS Xcode 工程和 Android APK/AAB；不要改写现有 Mac/WebGL 构建 profile。构建脚本负责设置版本号、application identifier、横屏方向、安全区相关 PlayerSettings、IL2CPP、Android API 36、纹理压缩和 mobile quality tier，但不把个人签名、keystore 或 provisioning profile 写入仓库。当前项目 Unity 是 `6000.3.10f1`，Figma 计划写的是 `6000.3.13+`；升级前要先在独立分支导入、编译和验证场景/插件。

建议的第一批发布验证顺序：

1. iOS Debug Xcode export → 一台真实 iPhone 横屏运行；
2. Android arm64 Debug APK → 一台 20:9 真机横屏运行；
3. iOS Release/TestFlight candidate 与 Android AAB；
4. 再决定是否提交商店。

每个平台都记录 BuildReport、包大小、启动时间、首个 room 的内存峰值、稳定帧率、触控状态截图和 FMOD/haptic 日志。没有设备运行记录时，只能称为导出成功，不能称为 mobile build 完成。

## 验收矩阵

| 层 | 完成证据 | 当前状态 |
| --- | --- | --- |
| 输入 facade | 桌面/WASM/autopilot 与基线行为一致，脚本化输入检查通过 | 代码已接线；完整回归待做 |
| Touch / Stick | Calm、Walk、Sprint、Winded 四态；双指移动+look；socket latch 正确释放 | 第一版代码；设备待测 |
| Touch / Use | Open/Shut/Take/Locked/Hold/Tap/Pressed；hold 环和 tap mode 与玻璃逻辑一致 | prompt/USE/hold ring 已接线；原生 haptics 与设备回归待做 |
| Safe area | iPhone 62/62/0/21、Android runtime、iPad bottom 20 的截图和数值日志 | 运行时 safe area 已接入；截图待做 |
| Menus | Title/Pause/Settings/Caught 全部可触控；失焦/back 会 pause；重启需要确认 | 第一版代码已接线；Device Simulator/实机回归待做 |
| Mobile render | iOS Metal 与 Android arm64 的 fallback、HDR/MSAA、玻璃、FMOD 均有设备记录 | 未开始 |
| Build | Xcode export、APK/AAB、包版本/identifier/图标正确 | 未开始 |
| Store/TestFlight | 仅在签名、隐私、图标、启动图和设备验收完成后决定 | 未开始 |

## 需要先定下的六个产品决定

1. Sprint 以 Figma 的 socket latch 为默认，还是允许 Settings 里的 button 模式作为默认？
2. Gyro look 是否保持默认关闭？
3. 近期 iPhone 目标 60 fps，还是以 30 fps 作为 Android/低端设备统一目标？
4. 桌面是否也迁移到新 Input System，还是 T0 保持旧桌面路径并只通过 facade 兼容？
5. Mobile v1 是否支持外接 controller？
6. 第一阶段只做内部 iOS/Android build 与设备测试，还是直接准备 TestFlight / Play 商店提交？

默认执行建议：先采用 socket latch、gyro off、近期 iPhone 60 fps、桌面先保持行为不变、v1 暂不承诺 controller，先完成 Debug 真机包再决定商店渠道。

## 下一步

下一步是用 Device Simulator 校准 safe-area 与 hit rect，再补齐触控设置和原生 haptics/gyro，最后新增独立 iOS/Android 导出入口。每一阶段都要留下对应的 Play Mode/设备截图和日志，再推进下一层。
