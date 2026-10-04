#!/usr/bin/env node
/** Generate UE5 ImportAssets settings for Unity surface/lighting textures. */
import fs from "node:fs";
import path from "node:path";
import process from "node:process";

const root = path.resolve(process.argv[2] ?? path.join(import.meta.dirname, "../.."));
const bridge = JSON.parse(fs.readFileSync(path.join(root, "Migration/exports/asset_bridge.json"), "utf8"));
const files = bridge.files.filter((file) => file.kind === "texture");
const settings = {
  ImportGroups: [{
    GroupName: "FrontRooms_SurfaceTextures",
    Filenames: files.map((file) => path.resolve(root, file.source).replaceAll("/", "\\")),
    DestinationPath: "/Game/FrontRooms/UnityImported/Textures",
    bReplaceExisting: true,
    bSkipReadOnly: false,
  }],
};
const output = path.join(root, "Migration/exports/unreal_texture_import_settings.json");
fs.writeFileSync(output, JSON.stringify(settings, null, 2) + "\n");
console.log(`wrote ${output} (${files.length} textures)`);
