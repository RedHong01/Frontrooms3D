# Unity ↔ Unreal 同步政策

这条政策适用于整个迁移期间：**任何 Unity side 的更新都必须在同一个工作项中评估并同步到 Unreal side；没有对应 Unreal 更新、迁移记录和验证结果的 Unity 改动，不得被视为迁移基线。**

Unity `Frontrooms3D/` 是行为、数据和验收合同的权威源。Unreal 是同一合同的实现和表现升级。Unreal 可以增加更高规格的材质、灯光、反射、雾、动画和后期，但不能让这些表现升级悄悄改变玩法、碰撞、状态时序、输入、音频触发或确定性地图结果。

## 必须同步的变更

| Unity side 更新 | Unreal side 必须同步 |
|---|---|
| C# gameplay、AI、移动、碰撞、门、窗、钥匙、Relay 状态 | C++ 核心、Actor/Component、StateTree/EQS 表现层和对应 functional test |
| `ScriptableObject`、JSON sidecar、MapHash、模块参数、单位和预算 | `UPrimaryDataAsset`、importer、chunk generator、DataAsset 版本和 golden JSON |
| 输入键、触控、相机、HUD 状态和提示 | Enhanced Input、Player/Camera、UMG/CommonUI 和 input trace |
| FBX、纹理、字体、SVG、视频、碰撞、LOD、锚点和 pile 数据 | Content Browser 资产、Material Instance、Composite Font、MediaPlayer、simple collision 和装饰 Actor |
| URP shader、后期、雾、反射、玻璃、RT 规则 | UE Material Function/Custom HLSL、Lumen/Reflection Capture、Post Process 和质量档配置 |
| FMOD event、bus、参数、衰减、遮挡和对象命名 | FMOD Studio UE integration、bank、event instance、参数绑定和 audio regression |
| Unity 验证报告、FAIL/FLAG/WAIT-RED、截图、性能预算 | Unreal Automation/Functional Test、截图/音频 trace、Insights/GPU budget 和差异记录 |
| 关卡/文档中的设计合同、常量、发行目标 | 迁移文档、DataAsset schema、平台设置和发布验收清单 |

## 每次 Unity 更新的同步流程

1. 在 Unity checkout 记录变更的 commit、文件、原因、影响范围和是否改变行为合同。未提交改动也必须记录，不能等到最后再追溯。
2. 重新运行 `Tools/UnrealMigration/export_contract.py`；如果涉及 Props sidecar，再运行 `export_kit_manifest.py`；如果涉及地图生成，再运行 Unity Editor 的 golden chunk exporter。
3. 在 Unreal side 建立对应实现或明确记录“无需代码变化”的理由。视觉升级要标记为 `Presentation Delta`，行为和数据变化不能只写成视觉升级。
4. 用相同 seed（`2554`、`20388`、`20261001`）和相同 input trace 重跑 Unity/Unreal 回归；地图、状态、碰撞和事件序列必须通过约定容差。
5. 更新 `Migration/STATUS.md` 和本文件的同步记录，附 Unity commit、Unreal commit、生成物路径和验证命令。
6. 在同步完成前，Unity 改动保持 `Pending Unreal Sync`；它不能被选为“已迁移”的新 golden baseline。

## Mac / Windows 协作约定

Mac 开发者只需要维护 Unity 源工程并生成可提交的同步快照；Unreal
编辑器和 Win64 打包固定在 Windows 上执行。仓库路径永远使用 `/` 分隔符，
导出物不能写入绝对路径，也不能把 `Library/`、`Temp/`、Unreal
`Binaries/`、`Intermediate/` 或 `Saved/` 当成同步输入。

在 Mac 上（Unity 工程根目录）运行：

```bash
python3 Tools/UnrealMigration/export_contract.py
python3 Tools/UnrealMigration/export_kit_manifest.py
node Tools/UnrealMigration/export_asset_bridge.mjs
node Tools/UnrealMigration/export_unreal_import_settings.mjs
node Tools/UnrealMigration/export_unreal_texture_settings.mjs
python3 Tools/UnrealMigration/sync_unity_unreal.py --update
```

提交 `Migration/exports/frontrooms_contract.json`、`kit_manifest.json`、
`asset_bridge.json` 和 `unity_sync_manifest.json`。`unity_sync_report.json`
是本机诊断文件，默认被忽略。Windows 侧收到 Mac 的提交后运行：

```powershell
& "$env:UE_PYTHON" Tools/UnrealMigration/sync_unity_unreal.py --check
& .\Tools\UnrealMigration\smoke_test.ps1
```

`UE_PYTHON` 可以指向 UE 安装自带的 Python；若系统已有 Python，直接用
`python` 也可以。`--check` 会比较 Unity 源文件和所有迁移生成物的 SHA-256，
列出新增、删除和修改，并确认 Unreal 项目的 `TargetPlatforms` 仍然只有
`Win64`。有任何差异时状态必须保持 `Pending Unreal Sync`，直到 Windows
侧重新生成或更新对应 Unreal 实现并通过 smoke test。

这个快照不要求 Mac 安装 Unreal。它保留 Unity 版本、Git revision、GUID
sidecar、场景、脚本、模型、纹理、字体、音频、视频、FMOD 和项目设置的
内容哈希，因此 Mac 上的 Unity 改动能在 Windows 上被确定地识别；实际
Unreal 导入、编辑器验证和 Windows build 仍由 Windows 工作项完成。

## 允许的视觉升级边界

Unreal 可以使用更高分辨率纹理、Nanite、Lumen、体积雾、Control Rig、硬件 RT 和更精细的后期。此类升级必须保持以下合同不变：

- 同一个 seed 产生相同的 zone、chunk、module、edge、key 和 lamp 状态；
- 同一个 input trace 产生相同的 phase、移动、冲刺、门窗、Relay 和 caught 时序；
- FMOD event、parameter、bus 和 gameplay hook 仍然在同一个触发点发生；
- 视觉改变不能偷偷扩大或缩小碰撞、可达空间、听觉/视线范围或交互距离。

如果为了电影质感故意改变上述任一项，必须先把它记录为设计变更，同时修改 Unity 基线、golden trace 和 Unreal 实现；不能只改 Unreal。

## 同步状态记录

每个迁移工作项至少记录四个状态：

`Unity Changed` → `Contract Exported` → `Unreal Updated` → `Unity/Unreal Verified`

缺少任一状态时，状态栏必须明确写 `Pending Unreal Sync`、`Pending Export` 或 `Pending Verification`，不能写 `Done`。
