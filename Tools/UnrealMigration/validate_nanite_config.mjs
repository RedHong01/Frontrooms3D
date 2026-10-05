import fs from "node:fs";
import path from "node:path";

const root = path.resolve(process.argv[2] ?? ".");
const projectPath = path.join(root, "Migration", "Unreal", "FrontRoomsUE.uproject");
const engineIniPath = path.join(root, "Migration", "Unreal", "Config", "DefaultEngine.ini");
const project = JSON.parse(fs.readFileSync(projectPath, "utf8"));
const engineIni = fs.readFileSync(engineIniPath, "utf8");

const platforms = project.TargetPlatforms ?? [];
if (platforms.length !== 1 || platforms[0] !== "Win64") {
  throw new Error(`TargetPlatforms must be exactly [Win64], got ${JSON.stringify(platforms)}`);
}
const required = [
  /^r\.Nanite\.ProjectEnabled\s*=\s*1\s*$/m,
  /^DefaultGraphicsRHI\s*=\s*DefaultGraphicsRHI_DX12\s*$/m,
  /^\+D3D12TargetedShaderFormats\s*=\s*PCD3D_SM6\s*$/m,
  /^-D3D12TargetedShaderFormats\s*=\s*PCD3D_SM5\s*$/m,
  /^-D3D11TargetedShaderFormats\s*=\s*PCD3D_SM5\s*$/m,
  /^r\.DynamicGlobalIlluminationMethod\s*=\s*1\s*$/m,
  /^r\.ReflectionMethod\s*=\s*1\s*$/m,
  /^bEnableRayTracing\s*=\s*True\s*$/m,
  /^bUseHardwareRayTracingForLumen\s*=\s*True\s*$/m,
  /^bGenerateRayTracingProxies\s*=\s*True\s*$/m,
  /^bEnableRayTracingShadows\s*=\s*False\s*$/m,
  /^ShadowMapMethod\s*=\s*1\s*$/m,
  /^r\.RayTracing\s*=\s*1\s*$/m,
  /^r\.Lumen\.HardwareRayTracing\s*=\s*1\s*$/m,
  /^r\.RayTracing\.RayTracingProxies\.ProjectEnabled\s*=\s*1\s*$/m,
  /^r\.RayTracing\.Shadows\s*=\s*0\s*$/m,
  /^r\.Shadow\.Virtual\.Enable\s*=\s*1\s*$/m,
];
for (const pattern of required) {
  if (!pattern.test(engineIni)) throw new Error(`Missing Nanite/SM6 setting: ${pattern}`);
}

console.log("FrontRooms Nanite/RT config passed: Win64-only, DX12, PCD3D_SM6, Nanite enabled, Lumen hardware RT enabled, VSM shadows");
