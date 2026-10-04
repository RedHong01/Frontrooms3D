#!/usr/bin/env node
import fs from "node:fs";
import crypto from "node:crypto";
import path from "node:path";
import process from "node:process";

const root = path.resolve(process.argv[2] ?? path.join(import.meta.dirname, "../.."));
const manifestPath = path.join(root, "Migration/exports/asset_bridge.json");
if (!fs.existsSync(manifestPath)) throw new Error("asset_bridge.json is missing; run export_asset_bridge.mjs first");
const manifest = JSON.parse(fs.readFileSync(manifestPath, "utf8"));
if (manifest.schema !== "frontrooms.unreal.asset-bridge" || manifest.schemaVersion !== 1) throw new Error("asset bridge schema");
if (!manifest.files.length || manifest.counts.mesh < 123 || manifest.counts.sidecar !== 113 || !manifest.counts.audio || !manifest.counts["fmod-bank"]) throw new Error("asset bridge has no required Unity asset inventory");
for (const file of manifest.files) {
  if (path.isAbsolute(file.source) || path.isAbsolute(file.destination)) throw new Error(`absolute bridge path: ${file.source}`);
  if (!/^[a-f0-9]{64}$/.test(file.sha256)) throw new Error(`bad sha256: ${file.source}`);
  const sourcePath = path.join(root, file.source);
  if (!fs.existsSync(sourcePath)) throw new Error(`missing source asset: ${file.source}`);
  if (fs.statSync(sourcePath).size !== file.bytes) throw new Error(`source size changed; regenerate bridge: ${file.source}`);
  const hash = crypto.createHash("sha256").update(fs.readFileSync(sourcePath)).digest("hex");
  if (hash !== file.sha256) throw new Error(`source contents changed; regenerate bridge: ${file.source}`);
}
console.log(`FrontRooms asset bridge gate passed: ${manifest.files.length} files (${manifest.totalBytes} bytes)`);
