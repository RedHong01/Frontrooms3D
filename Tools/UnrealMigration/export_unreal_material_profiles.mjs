#!/usr/bin/env node
/** Export Unity URP .mat values as a deterministic UE material profile. */
import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";

const root = path.resolve(process.argv[2] ?? path.join(import.meta.dirname, "../.."));
const sourceRoot = path.join(root, "Assets/Resources/Surfaces");
const output = path.join(root, "Migration/exports/unreal_material_profiles.json");
const walk = (dir) => fs.readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
  const full = path.join(dir, entry.name);
  return entry.isDirectory() ? walk(full) : [full];
});
const scalarRe = /^\s*-\s+([^:]+):\s*([-+0-9.eE]+)\s*$/gm;
const colorRe = /^\s*-\s+([^:]+):\s*\{r:\s*([-+0-9.eE]+),\s*g:\s*([-+0-9.eE]+),\s*b:\s*([-+0-9.eE]+),\s*a:\s*([-+0-9.eE]+)\}\s*$/gm;
const textureRe = /^\s*-\s+([^:]+):\s*\n\s+m_Texture:\s*\{fileID:\s*(-?\d+),\s*guid:\s*([0-9a-f]+),\s*type:\s*(\d+)\}/gm;
const parse = (file) => {
  const text = fs.readFileSync(file, "utf8");
  const name = text.match(/^\s*m_Name:\s*(.+?)\s*$/m)?.[1]?.trim();
  if (!name) return null;
  const shader = text.match(/^\s*m_Shader:\s*\{fileID:\s*(-?\d+),\s*guid:\s*([0-9a-f]+),\s*type:\s*(\d+)\}\s*$/m);
  const floats = {};
  for (const match of text.matchAll(scalarRe)) floats[match[1].trim()] = Number(match[2]);
  const colors = {};
  for (const match of text.matchAll(colorRe)) colors[match[1].trim()] = match.slice(2).map(Number);
  const textures = {};
  for (const match of text.matchAll(textureRe)) {
    if (Number(match[2]) !== 0) textures[match[1].trim()] = { fileId: Number(match[2]), guid: match[3], type: Number(match[4]) };
  }
  return {
    name,
    source: path.relative(root, file).replaceAll("\\", "/"),
    sourceSha256: crypto.createHash("sha256").update(fs.readFileSync(file)).digest("hex"),
    shaderGuid: shader?.[2] ?? null,
    floats,
    colors,
    textures,
  };
};
const profiles = walk(sourceRoot).filter((file) => file.endsWith(".mat")).sort().map(parse).filter(Boolean);
const payload = {
  schema: "frontrooms.unreal.material-profiles",
  schemaVersion: 1,
  sourceRoot: "Assets/Resources/Surfaces",
  profiles,
  notes: {
    baseColor: "Use sRGB texture/color for Base Color",
    normal: "Use linear normal texture with UE's normal-map compression",
    smoothness: "Unity Smoothness maps to UE Roughness as 1 - Smoothness",
    metallic: "Unity Metallic maps to UE Metallic",
    emission: "Unity emission color/texture maps to UE Emissive Color",
  },
};
fs.mkdirSync(path.dirname(output), { recursive: true });
fs.writeFileSync(output, JSON.stringify(payload, null, 2) + "\n");
console.log(`wrote ${output} (${profiles.length} Unity material profiles)`);
if (profiles.length < 90) process.exitCode = 1;
