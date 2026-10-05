# FrontRooms Unreal 光追 / Lumen / HDR 审计

日期：2026-10-05  
引擎：UE 5.8.3（`D:\UE_5.8`）  
目标：仅 Windows Win64，DX12 + `PCD3D_SM6`

## 结论

旧编辑器日志显示项目启动时光追被项目配置关闭：

```
LogRendererCore: Ray tracing is disabled. Reason: disabled through project setting (r.RayTracing=0)
```

旧配置没有显式选择 Lumen GI/Reflection，也没有打开硬件 RT、Ray Tracing Proxy 或 Virtual Shadow Maps。Nanite 已经开启，但这只保证 Nanite 几何路径，并不自动开启 Lumen 或 DXR。

`DefaultEngine.ini` 已完成以下 Windows 配置：

- `r.DynamicGlobalIlluminationMethod=1`（Lumen GI）
- `r.ReflectionMethod=1`（Lumen Reflections）
- `bEnableRayTracing=True`（项目硬件 RT 支持）
- `bUseHardwareRayTracingForLumen=True`（DXR 可用时 Lumen 走硬件路径，不可用时保留软件回退）
- `bGenerateRayTracingProxies=True` / `r.RayTracing.RayTracingProxies.ProjectEnabled=1`（为 Nanite 资产生成 RT 代理和可流式 LOD）
- `r.Shadow.Virtual.Enable=1` / `ShadowMapMethod=1`（Virtual Shadow Maps）
- `bEnableRayTracingShadows=False` / `r.RayTracing.Shadows=0`（避免当前房间生成的多盏可移动点光源逐灯发起 RT shadow dispatch）
- `r.AllowHDR=1` 及原有 1000 nit peak / 300 nit paper white 校色保持不变

本机注册表的显示驱动条目确认了 `NVIDIA GeForce RTX 5090 Laptop GPU`
（NVIDIA driver `32.0.15.7284`），因此 DX12/DXR 硬件路径具备硬件条件；
Intel 集显条目仍保留，编辑器和 Win64 运行时应选择 RTX 设备。
旧编辑器日志进一步确认已选择 `D3D12 Adapter Id = 0`、24,051 MB 显存，
`D3D12 ray tracing tier 1.1 and bindless resources are supported`，并记录
`NVIDIA Ray Tracing Cluster Operations supported and enabled`；旧实例唯一关闭
光追的原因是项目 CVar `r.RayTracing=0`。日志还确认 HDR 输出枚举成功（Max
Luminance 231.4、MaxFullFrameLuminance 110.8；这是当前显示器能力，不修改项目
1000 nit 校色合同）。

Direct-light shadows 采用 VSM，Lumen 的 GI、反射和天空遮蔽使用硬件 RT；这是当前地图结构下较稳妥的质量/性能平衡。以后需要电影级单灯阴影时，可以只对方向光或少数重点光源启用 Ray Traced Shadows，不应全局打开。

## 验证证据

配置静态 gate：

```
node Tools/UnrealMigration/validate_nanite_config.mjs D:\Frontrooms3D
FrontRooms Nanite/RT config passed: Win64-only, DX12, PCD3D_SM6, Nanite enabled, Lumen hardware RT enabled, VSM shadows
```

旧编辑器实例的日志位于 `Migration/Unreal/Saved/Logs/FrontRoomsUE.log`，记录的是配置修改前的 `r.RayTracing=0`。编辑器必须完全退出后重新启动，才能加载新的 renderer settings；请使用 `Tools/UnrealMigration/open_unreal_editor.ps1`，不要只重新加载地图。

使用 `-nullrhi` 的 smoke/commandlet 会把 metadata 记为 `raytracing=0`，因为 Null RHI 没有 GPU/DXR。这一项不能作为硬件 RT 验证；硬件状态要从重启后的真实 DX12 编辑器或 Win64 包日志确认：

```
LogCsvProfiler: Metadata set : rhiname="D3D12"
LogCsvProfiler: Metadata set : shaderplatform="PCD3D_SM6"
LogCsvProfiler: Metadata set : raytracing="1"
```

## 重启后的检查

1. 退出旧的 UE 编辑器，重新执行 `Tools/UnrealMigration/open_unreal_editor.ps1 -Root D:\Frontrooms3D`。
2. 打开 Output Log，确认 `r.RayTracing=1`，且没有 `disabled through project setting`。
3. 在控制台执行 `r.Lumen.HardwareRayTracing 1`、`r.Shadow.Virtual.Enable 1` 后使用 `profilegpu` 检查 GPU pass。配置文件已默认设置这两个开关，控制台只用于现场确认。
4. 在 PIE/Win64 包中观察 `stat gpu`、`stat lumen` 和 `profilegpu`：若硬件 RT 不可用，Lumen 会自动退回软件 tracing，仍保持可运行；如果显示驱动报告设备不支持 DXR，应保留软件回退，不强制崩溃。

## 已知限制

- 当前 smoke 使用 Null RHI 验证数据、输入、HUD、HDR 和资产合同，因此不会证明真实 GPU 的 DXR 性能。
- 本机的旧编辑器进程在修改配置时已经打开；在新的编辑器进程启动前，旧窗口继续显示旧光追状态是正常的。
- 这是 Win64-only 配置；不要把 Mac/Linux/移动端 shader format 加回项目的 `TargetPlatforms` 或 Windows cook 命令。
