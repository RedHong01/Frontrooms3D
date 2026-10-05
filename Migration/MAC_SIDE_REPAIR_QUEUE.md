# Mac 端维修清单：Unity → Unreal Win64

这份文件给 Mac 端 Codex 使用。Mac 端只修改 Unity/FMOD 源数据并提交可复核的导出物；Unreal 编辑器、资产导入、smoke test 和 Windows package 固定在 Windows 上执行。不要直接修改 `Migration/Unreal/Content/**/*.uasset` 来掩盖 Unity 源问题。

## 当前基线

- Unity：`6000.3.10f1`，URP `17.3.0`。
- Unreal：`5.8.3`，目标平台只有 `Win64`。
- Unity golden chunks 已由 Windows 上的 Unity 6000.3.10f1 导出并验证：seed `2554`、`20388`、`20261001`，每个半径 1、9 个 chunk。
- 当前完整 Windows smoke 已通过，但 FMOD 仍是显式的 source-bank drift：运行时解析 `28/31` 个事件。
- 所有当前源侧待修项都必须在 Windows 复验通过后才可以从 `Migration/STATUS.md` 的 pending gate 中移除。

## 0. Windows `dotnet.exe` 弹窗的边界

如果 Windows 端继续出现 `dotnet.exe - 应用程序错误 / 0xe0434352`，不要把它当成
Unity 源代码或 Mac 资产损坏。UE 启动时会调用 UBT 的
`Build.bat -Mode=ValidatePlatforms -OutputSDKs -AllPlatforms`；本机只有 Win64
工具链，非 Win64 SDK 探测会触发 CLR 异常，旧的 UE5.6 Zen/UBT profile 还会让问题复现。
Windows 端已经将项目 profile 固定为工作区内的 `.ue58-profile`，并设置
`UE_SKIP_UBT_SDK_SETUP=1`，项目和打包仍严格只允许 Win64。Mac 端不需要修改 Unity
资产来处理这个弹窗，也不要把其它平台 SDK 加入迁移目标；若 Windows 端需要重现，使用：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File `
  Tools\UnrealMigration\open_unreal_editor.ps1 -Root D:\Frontrooms3D
```

只有在这个启动器仍失败时，才检查 Windows 上的 `D:\UE_5.8` 安装和 `.ue58-profile` 日志，
不要用旧的 UE5.6 快捷方式启动工程。

同样的 Nanite 项目设置已写入 `Migration/Unreal/Config/DefaultEngine.ini`：Win64 使用
DX12，目标 shader format 为 `PCD3D_SM6`，并启用 `r.Nanite.ProjectEnabled=1`。这属于
Windows Unreal 工程配置，Mac 端不要改成 Metal 或把 Mac 加入目标平台；如果 Mac 端
提交了 Unity 网格变化，Windows 端重启编辑器后再检查 Nanite 资产和 shader cook。

## 1. 必须修复：FMOD 三个缺失事件

当前缺失的事件路径是：

```text
event:/Mechanism/Door/StreamOpen
event:/Mechanism/Door/StreamClose
event:/Mechanism/Door/StreamLock
```

Windows 证据：

- `FMOD/FrontRooms/FrontRooms.fspro` 只有 90 字节 `<objects />` 根节点，序列化对象数为 0。
- `FMOD/FrontRooms/Metadata/Event` 有 31 个事件 XML，C# 契约与 Metadata 路径差异为 0。
- `Assets/StreamingAssets/FMOD` 与 Unreal 暂存的五个 bank 当前 SHA-256 完全一致。
- `Migration/exports/fmod_bank_manifest.json` 的状态是 `source-bank-drift`，不是完成状态。

在有匹配版本的 FMOD Studio/CLI 的 Mac 机器上，从仓库根目录执行：

```sh
python3 Tools/audio/fmod_frontrooms.py all
fmodstudiocl \
  -script Tools/audio/build/fmod_build_frontrooms.js \
  FMOD/FrontRooms/FrontRooms.fspro
```

Unity runtime 报告 FMOD Studio API `2.3.15 (build 168126)`，Metadata 使用 `Studio.02.03.00`；使用兼容的 FMOD Studio 版本打开项目。生成完成后，只复制这五个 Desktop bank：

```sh
for bank in Master.bank Master.strings.bank Ambience.bank SFX.bank Music.bank; do
  cp "FMOD/FrontRooms/Build/Desktop/$bank" "Assets/StreamingAssets/FMOD/$bank"
done
python3 Tools/UnrealMigration/sync_unity_unreal.py --update
```

不要手工编辑 bank 二进制、复制其他平台 bank，或把 Unity `.meta` 文件复制到 Unreal。把新的 bank hash 随 Unity 源变更提交。

## 2. 必须修复或逐项批准：FBX 切线/退化几何

当前 Unreal 导入日志有 108 条 warning，涉及 35 个唯一 StaticMesh。问题分成两类：

1. `RenderData Bounds` 与 `MeshDescription Bounds` 不一致。代表资产 `Kit_Binders` 在 UE 5.8.3 的 transient probe 中启用 `RemoveDegenerates` 后已对齐，源 `.uasset` 没有被修改。
2. near-zero tangent/bi-normal。代表资产 `Kit_DoorFrame_Steel_LOD0` 在重算 normals/tangents、MikkTSpace 和 `RemoveDegenerates` 后仍有这类诊断；这通常需要源模型的 UV0、退化面或硬边修复。

重点先检查：

```text
Kit_DoorFrame_Steel_LOD0/LOD1
Kit_DoorLeaf_Veneer_LOD0/LOD1
Kit_MiniBlind_Lowered_LOD1
Kit_PaperStack
```

Mac 端处理顺序：

1. 在 Unity ModelImporter 或源 DCC 中确认每个 LOD 有有效 UV0，删除零面积面，合并重复顶点，确保法线和切线可以由 MikkTSpace 从 UV0 重建。
2. 对有问题的模型设置等价的 Unity 导入选项：计算 normals、计算 MikkTSpace tangents、保留与运行时一致的轴向/单位；不要改变 authored collider 或 LOD sidecar。
3. 重新导出/刷新对应 FBX 后，运行 Unity 侧导出器并提交源 FBX、`.meta` 和 hash 变化。
4. 如果源模型必须保留当前几何，逐资产写出经过美术确认的例外，说明为何允许 near-zero tangent；不能用全局静默 warning 代替审查。

Windows 侧会用 UE commandlet 做可逆验证：

```powershell
& 'D:\UE_5.8\Engine\Binaries\ThirdParty\Python3\Win64\python.exe' `
  Tools\UnrealMigration\sync_unity_unreal.py --root D:\Frontrooms3D --check
powershell.exe -NoProfile -ExecutionPolicy Bypass -File `
  Tools\UnrealMigration\run_unreal_asset_pipeline.ps1 -Root D:\Frontrooms3D
```

验收依据是 `Migration/exports/unreal_mesh_build_probe_binders.json`、`unreal_mesh_build_probe_doorframe.json` 和最新 `Migration/exports/unreal_asset_audit.json`。只有 warning 数量下降到已解释的例外集合，才能关闭这项 gate。

## 3. 每次 Unity 改动都要重新导出并同步

Mac 端从仓库根目录运行：

```sh
python3 Tools/UnrealMigration/export_contract.py
python3 Tools/UnrealMigration/export_kit_manifest.py
node Tools/UnrealMigration/export_asset_bridge.mjs
node Tools/UnrealMigration/export_unreal_import_settings.mjs
node Tools/UnrealMigration/export_unreal_texture_settings.mjs
"/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -quit -projectPath . \
  -executeMethod FrontRoomsUnrealChunkExporter.ExportGoldenChunksCommandLine \
  -frontRoomsExportRadius 1 -frontRoomsExportSeeds 2554,20388,20261001 \
  -logFile Migration/exports/unity_chunks/golden_export.log
python3 Tools/UnrealMigration/validate_golden_chunks.py --root .
python3 Tools/UnrealMigration/sync_unity_unreal.py --update
python3 Tools/UnrealMigration/sync_unity_unreal.py --check
```

提交这些生成物（以及对应源改动）：

```text
Migration/exports/frontrooms_contract.json
Migration/exports/kit_manifest.json
Migration/exports/asset_bridge.json
Migration/exports/unreal_import_settings.json
Migration/exports/unreal_texture_import_settings.json
Migration/exports/unity_chunks/seed-*.json
Migration/exports/unity_golden_chunk_validation.json
Migration/exports/unity_sync_manifest.json
```

如果 Unity 6000.x 自动给大量纹理 `.meta` 增加 tvOS/WindowsStoreApps 平台默认段，先确认这不是有意的 Unity source change；不要把编辑器噪声当成资产修复提交。

## 4. Windows 侧最终验收

Mac 提交后，Windows 端按顺序执行：

```powershell
$uePython = 'D:\UE_5.8\Engine\Binaries\ThirdParty\Python3\Win64\python.exe'
& $uePython Tools\UnrealMigration\sync_unity_unreal.py --root D:\Frontrooms3D --check
& $uePython Tools\UnrealMigration\validate_golden_chunks.py --root D:\Frontrooms3D
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools\UnrealMigration\run_unreal_asset_pipeline.ps1 -Root D:\Frontrooms3D
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools\UnrealMigration\smoke_test.ps1 -Root D:\Frontrooms3D
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools\UnrealMigration\build_windows.ps1 -Root D:\Frontrooms3D
```

FMOD 修复完成后的严格检查必须不带 `--allow-missing-event`，并且报告 `31/31` resolved；随后 smoke 的 `fmod-banks`、runtime map、HDR 和 Win64 package 都要重新通过。Unreal 启动与打包使用 `.ue58-profile`，避免 UE5.6 的 Zen/UBT 缓存污染；不要直接用旧 UE5.6 shortcut 启动这个项目。

## 完成定义

- `sync_unity_unreal.py --check` 无 Unity 或 generated artifact 漂移。
- `validate_golden_chunks.py` 通过三个 seed、9 个 chunk/seed 和 MapHash vectors。
- FBX bounds/tangent warnings 已逐项修复或有明确的美术例外记录。
- 严格 FMOD probe 解析 31/31 事件。
- UE 5.8.3 smoke 通过，且 `build_windows.ps1` 产出 Win64 Development package。
- 将 Windows 最新 report、asset audit、FMOD manifest、mesh probe 报告和 package 路径回填到 `Migration/STATUS.md`。
