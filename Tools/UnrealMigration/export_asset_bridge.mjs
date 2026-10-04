#!/usr/bin/env node
/** Build a deterministic bridge manifest for reusing Unity-side assets in UE. */
import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import process from "node:process";

const root = path.resolve(process.argv[2] ?? path.join(import.meta.dirname, "../.."));
const output = path.join(root, "Migration/exports/asset_bridge.json");
const includeRoots = [
  ["Assets/Resources/Props/Models", [".fbx", ".json"]],
  ["Assets/Resources/Models/Office", [".fbx"]],
  ["Assets/Resources/Surfaces", [".png", ".jpg", ".jpeg", ".tga", ".exr", ".mat"]],
  ["Assets/Resources/Brand", [".svg", ".png", ".shader"]],
  ["Assets/Resources/Fonts", [".ttf", ".otf"]],
  ["Assets/Resources/Audio", [".wav", ".ogg", ".mp3"]],
  ["Assets/Resources/Lighting", [".exr", ".asset"]],
  ["Assets/StreamingAssets", [".mp4", ".bank"]],
];

const digest = (file) => {
  const hash = crypto.createHash("sha256");
  hash.update(fs.readFileSync(file));
  return hash.digest("hex");
};
const files = [];
for (const [relativeRoot, extensions] of includeRoots) {
  const absoluteRoot = path.join(root, relativeRoot);
  if (!fs.existsSync(absoluteRoot)) continue;
  const walk = (directory) => {
    for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
      const absolute = path.join(directory, entry.name);
      if (entry.isDirectory()) walk(absolute);
      else if (extensions.includes(path.extname(entry.name).toLowerCase())) {
        const relative = path.relative(root, absolute).split(path.sep).join("/");
        const extension = path.extname(entry.name).toLowerCase();
        const kind = extension === ".fbx" ? "mesh" : extension === ".json" ? "sidecar" : extension === ".bank" ? "fmod-bank" : extension === ".mp4" ? "video" : extension === ".wav" || extension === ".ogg" || extension === ".mp3" ? "audio" : extension === ".mat" || extension === ".shader" ? "unity-material-source" : extension === ".ttf" || extension === ".otf" ? "font" : extension === ".svg" ? "vector" : "texture";
        files.push({ source: relative, destination: `Content/FrontRooms/UnitySource/${relative.slice("Assets/".length)}`, kind, bytes: fs.statSync(absolute).size, sha256: digest(absolute) });
      }
    }
  };
  walk(absoluteRoot);
}
files.sort((a, b) => a.source.localeCompare(b.source));
const byKind = Object.groupBy ? Object.groupBy(files, (file) => file.kind) : files.reduce((groups, file) => ((groups[file.kind] ??= []).push(file), groups), {});
const manifest = {
  schema: "frontrooms.unreal.asset-bridge",
  schemaVersion: 1,
  sourceRoot: "Assets",
  destinationRoot: "Migration/Unreal/Content/FrontRooms/UnitySource",
  files,
  counts: Object.fromEntries(Object.entries(byKind).map(([kind, values]) => [kind, values.length])),
  totalBytes: files.reduce((sum, file) => sum + file.bytes, 0),
};
fs.mkdirSync(path.dirname(output), { recursive: true });
fs.writeFileSync(output, JSON.stringify(manifest, null, 2) + "\n");
console.log(`wrote ${output} (${files.length} source assets)`);
