#!/usr/bin/env node
/** Generate UE5 ImportAssets commandlet settings from the Unity bridge. */
import fs from "node:fs";
import path from "node:path";
import process from "node:process";

const root = path.resolve(process.argv[2] ?? path.join(import.meta.dirname, "../.."));
const bridge = JSON.parse(fs.readFileSync(path.join(root, "Migration/exports/asset_bridge.json"), "utf8"));
const meshFiles = bridge.files.filter((file) => file.kind === "mesh");
const groups = new Map([["Office", []], ["Props", []]]);
for (const file of meshFiles) {
  const group = file.source.includes("/Models/Office/") ? "Office" : "Props";
  groups.get(group).push(path.resolve(root, file.source).replaceAll("/", "\\"));
}
const settings = {
  ImportGroups: [...groups.entries()].filter(([, files]) => files.length).map(([name, files]) => ({
    GroupName: `FrontRooms_${name}`,
    Filenames: files,
    DestinationPath: `/Game/FrontRooms/UnityImported/${name}`,
    bReplaceExisting: true,
    bSkipReadOnly: false,
  })),
};
const output = path.join(root, "Migration/exports/unreal_import_settings.json");
fs.writeFileSync(output, JSON.stringify(settings, null, 2) + "\n");
console.log(`wrote ${output} (${meshFiles.length} FBX files in ${settings.ImportGroups.length} groups)`);
