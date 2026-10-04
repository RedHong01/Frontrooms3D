#!/usr/bin/env node
import fs from "node:fs";
import path from "node:path";
import process from "node:process";

const root = path.resolve(process.argv[2] ?? path.join(import.meta.dirname, "../.."));
const read = (relative) => JSON.parse(fs.readFileSync(path.join(root, relative), "utf8"));
const fail = (message) => { throw new Error(message); };
const contract = read("Migration/exports/frontrooms_contract.json");
const kit = read("Migration/exports/kit_manifest.json");

if (contract.schema !== "frontrooms.unreal.migration.contract" || contract.schemaVersion !== 1) fail("contract schema");
const constants = contract.constants;
if (constants.cellSizeMeters !== 3 || constants.chunkCells !== 8 || constants.chunkSizeMeters !== 24 || constants.worldRebaseThresholdMeters !== 192) fail("map constants");
if (JSON.stringify(contract.validationSeeds) !== JSON.stringify([2554, 20388, 20261001])) fail("golden seeds");
if (contract.modules.length !== 4 || new Set(contract.modules.map((m) => m.name)).size !== 4) fail("room modules");
if (contract.audio.eventCount !== 31 || contract.audio.busCount !== 5 || contract.audio.parameterCount !== 19) fail("audio contract");
for (const field of ["source", "mesh"]) for (const model of kit.models) if (path.isAbsolute(model[field])) fail(`absolute path: ${model[field]}`);
if (kit.schema !== "frontrooms.unreal.kit-manifest" || kit.schemaVersion !== 1) fail("kit schema");
if (kit.count !== 113 || kit.missingFbx.length !== 0) fail("kit count or missing FBX");
if (kit.totals.triangles !== 225673 || kit.totals.anchors !== 490 || kit.totals.colliders !== 55) fail("kit totals");
console.log(`FrontRooms Unreal export gate passed: ${contract.modules.length} modules, ${kit.count} kits, ${contract.audio.eventCount} FMOD events`);
