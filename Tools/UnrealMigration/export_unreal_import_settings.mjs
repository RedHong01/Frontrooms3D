#!/usr/bin/env node
/** Generate UE5 ImportAssets commandlet settings from the Unity bridge. */
import fs from "node:fs";
import path from "node:path";
import process from "node:process";

const root = path.resolve(process.argv[2] ?? path.join(import.meta.dirname, "../.."));
const bridge = JSON.parse(fs.readFileSync(path.join(root, "Migration/exports/asset_bridge.json"), "utf8"));
const meshFiles = bridge.files.filter((file) => file.kind === "mesh");
// Keep every Unity FBX in its own destination folder. Unity variants often
// share the same mesh object names, so putting all files in one UE folder
// would make Interchange overwrite a prior variant and lose source coverage.
const groups = meshFiles.map((file) => {
  const category = file.source.includes("/Models/Office/") ? "Office" : "Props";
  const stem = path.basename(file.source, path.extname(file.source));
  return {
    GroupName: `FrontRooms_${category}_${stem}`,
    Filenames: [path.resolve(root, file.source).replaceAll("/", "\\")],
    DestinationPath: `/Game/FrontRooms/UnityImported/${category}/${stem}`,
    bReplaceExisting: true,
    bSkipReadOnly: false,
  };
});
const settings = {
  ImportGroups: groups,
};
const output = path.join(root, "Migration/exports/unreal_import_settings.json");
fs.writeFileSync(output, JSON.stringify(settings, null, 2) + "\n");
console.log(`wrote ${output} (${meshFiles.length} FBX files in ${settings.ImportGroups.length} collision-safe groups)`);
