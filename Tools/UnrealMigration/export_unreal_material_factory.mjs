#!/usr/bin/env node
/**
 * Build a deterministic UE material/texture channel contract from Unity
 * material profiles.  This is intentionally data-only: UE Editor import and
 * material authoring can consume the manifest without losing Unity's A/N/S/E/M/P
 * texture semantics or scalar/color values.
 */
import fs from "node:fs";
import path from "node:path";

const root = path.resolve(process.argv[2] ?? path.join(import.meta.dirname, "../.."));
const profilePath = path.join(root, "Migration/exports/unreal_material_profiles.json");
const output = path.join(root, "Migration/exports/unreal_material_factory.json");
const profiles = JSON.parse(fs.readFileSync(profilePath, "utf8"));
const bridge = JSON.parse(fs.readFileSync(path.join(root, "Migration/exports/asset_bridge.json"), "utf8"));

// These settings are intentionally explicit because the Windows editor
// commandlet applies them to the imported UTexture2D assets.  Keep the
// semantic contract in this portable manifest so a Mac-side Unity export and
// a Windows UE rebuild produce the same texture settings.
const channelSpec = {
  A: { name: "base_color", ueProperty: "BaseColor", colorSpace: "sRGB", compression: "Default", ueCompressionSettings: "TC_Default", ueSrgb: true, ueNormalMap: false, description: "Unity albedo/base color" },
  N: { name: "normal", ueProperty: "Normal", colorSpace: "Linear", compression: "Normalmap", ueCompressionSettings: "TC_Normalmap", ueSrgb: false, ueNormalMap: true, description: "Unity tangent-space normal" },
  S: { name: "smoothness", ueProperty: "Roughness", colorSpace: "Linear", compression: "Masks", ueCompressionSettings: "TC_Masks", ueSrgb: false, ueNormalMap: false, invert: true, description: "Unity smoothness; UE roughness is 1 - S" },
  E: { name: "emissive", ueProperty: "EmissiveColor", colorSpace: "Linear", compression: "Default", ueCompressionSettings: "TC_Default", ueSrgb: false, ueNormalMap: false, description: "Unity emission" },
  M: { name: "mask", ueProperty: "MaterialMask", colorSpace: "Linear", compression: "Masks", ueCompressionSettings: "TC_Masks", ueSrgb: false, ueNormalMap: false, description: "Unity macro/grime/mask data" },
  P: { name: "print", ueProperty: "PrintColor", colorSpace: "sRGB", compression: "Default", ueCompressionSettings: "TC_Default", ueSrgb: true, ueNormalMap: false, description: "Unity print/decal color" },
};
const channelKeys = Object.keys(channelSpec);

const walk = (dir) => fs.readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
  const full = path.join(dir, entry.name);
  return entry.isDirectory() ? walk(full) : [full];
});
const guidToSource = new Map();
for (const file of walk(path.join(root, "Assets"))) {
  if (!file.endsWith(".meta")) continue;
  const guid = fs.readFileSync(file, "utf8").match(/^guid:\s*([0-9a-f]+)\s*$/m)?.[1];
  if (guid) guidToSource.set(guid, file.slice(0, -5));
}
const suffixOf = (source) => {
  const match = path.basename(source).match(/_([ANSEMP])\.[^.]+$/i);
  return match?.[1]?.toUpperCase() ?? null;
};
const sourceChannelOf = (source) => {
  const suffix = suffixOf(source);
  if (suffix) return suffix;
  // The Unity brand logo is a UI/print asset rather than a linear material
  // mask.  Preserve its sRGB intent when it has no A/N/S/E/M/P suffix.
  if (/\/Brand\//i.test(source)) return "P";
  // Any future unsuffixed surface input gets the conservative color default;
  // material property names still win for profile references below.
  return "A";
};
const ueTexturePath = (source) => {
  const stem = path.basename(source, path.extname(source));
  return `/Game/FrontRooms/UnityImported/Textures/${stem}.${stem}`;
};
const ueSettingsFor = (channel) => {
  const spec = channel ? channelSpec[channel] : null;
  if (!spec) return null;
  return {
    compressionSettings: spec.ueCompressionSettings,
    sRGB: spec.ueSrgb,
    normalMap: spec.ueNormalMap,
    textureGroup: "TEXTUREGROUP_World",
  };
};
const propertyFallback = (property) => {
  if (/normal|bump/i.test(property)) return "N";
  if (/emission/i.test(property)) return "E";
  if (/print/i.test(property)) return "P";
  if (/mask|macro|grime|occlusion|metallic|spec/i.test(property)) return "M";
  if (/base|main|albedo/i.test(property)) return "A";
  return null;
};

const textureEntries = new Map();
const resolveTexture = (property, ref) => {
  const source = guidToSource.get(ref.guid);
  if (!source) return { property, guid: ref.guid, channel: propertyFallback(property), source: null, ueAsset: null };
  const relative = path.relative(root, source).replaceAll("\\", "/");
  const channel = suffixOf(source) ?? propertyFallback(property);
  const entry = {
    property,
    guid: ref.guid,
    source: relative,
    channel,
    ueAsset: ueTexturePath(source),
    ueTextureSettings: ueSettingsFor(channel),
  };
  textureEntries.set(relative, {
    source: relative,
    channel,
    ueAsset: entry.ueAsset,
    ueTextureSettings: entry.ueTextureSettings,
  });
  return entry;
};

const materials = profiles.profiles.map((profile) => {
  const channels = Object.fromEntries(channelKeys.map((key) => [key, []]));
  const textures = Object.entries(profile.textures ?? {}).map(([property, ref]) => {
    const resolved = resolveTexture(property, ref);
    if (resolved.channel && channels[resolved.channel]) channels[resolved.channel].push(resolved);
    return resolved;
  });
  const scalarParameters = { ...(profile.floats ?? {}) };
  const ueParameters = {};
  if (Number.isFinite(scalarParameters._Smoothness)) ueParameters.Roughness = 1 - scalarParameters._Smoothness;
  if (Number.isFinite(scalarParameters._Metallic)) ueParameters.Metallic = scalarParameters._Metallic;
  if (Number.isFinite(scalarParameters._OcclusionStrength)) ueParameters.OcclusionStrength = scalarParameters._OcclusionStrength;
  if (Number.isFinite(scalarParameters._Cutoff)) ueParameters.OpacityMaskClipValue = scalarParameters._Cutoff;
  if (profile.colors?._BaseColor) ueParameters.BaseColor = profile.colors._BaseColor;
  if (profile.colors?._EmissionColor) ueParameters.EmissiveColor = profile.colors._EmissionColor;
  return {
    name: profile.name,
    source: profile.source,
    sourceSha256: profile.sourceSha256,
    shaderGuid: profile.shaderGuid,
    textures,
    channels,
    scalarParameters,
    colorParameters: profile.colors ?? {},
    ueParameters,
  };
});

const channelCounts = Object.fromEntries(channelKeys.map((key) => [key, 0]));
for (const entry of textureEntries.values()) if (entry.channel && channelCounts[entry.channel] !== undefined) channelCounts[entry.channel]++;
const sourceTextures = bridge.files.filter((file) => file.kind === "texture").map((file) => {
  const channel = sourceChannelOf(file.source);
  return { source: file.source, channel, ueAsset: ueTexturePath(file.source) };
});
// Profiles only reference the textures used by a material.  The import
// contract must still configure every Unity texture that was staged into UE,
// including source textures that are currently unreferenced (for example a
// future room variant).  Add those entries to the same deterministic factory
// list so the commandlet cannot silently leave an imported texture at the UE
// default settings.
for (const entry of sourceTextures) {
  if (!textureEntries.has(entry.source)) {
    textureEntries.set(entry.source, {
      source: entry.source,
      channel: entry.channel,
      ueAsset: entry.ueAsset,
      ueTextureSettings: ueSettingsFor(entry.channel),
    });
  }
}
const sourceChannelCounts = Object.fromEntries(channelKeys.map((key) => [key, 0]));
for (const entry of sourceTextures) if (entry.channel && sourceChannelCounts[entry.channel] !== undefined) sourceChannelCounts[entry.channel]++;
const unresolved = materials.flatMap((material) => material.textures.filter((texture) => !texture.source).map((texture) => `${material.name}:${texture.property}:${texture.guid}`));
const payload = {
  schema: "frontrooms.unreal.material-factory",
  schemaVersion: 1,
  targetEngine: "5.8.3",
  targetPlatforms: ["Win64"],
  sourceProfile: "Migration/exports/unreal_material_profiles.json",
  destination: "/Game/FrontRooms/UnityImported/Textures",
  channelSemantics: channelSpec,
  textures: [...textureEntries.values()].sort((a, b) => a.source.localeCompare(b.source)),
  textureCount: textureEntries.size,
  channelCounts,
  sourceTextureCount: sourceTextures.length,
  sourceTextures,
  sourceChannelCounts,
  unresolvedTextures: unresolved,
  materials,
  notes: [
    "Import textures with the generated texture settings before applying this manifest.",
    "A/N/S/E/M/P suffix is authoritative; property names are only a fallback for unsuffixed Unity references.",
    "S is Unity smoothness and must be inverted when driving UE Roughness.",
  ],
};
fs.mkdirSync(path.dirname(output), { recursive: true });
fs.writeFileSync(output, JSON.stringify(payload, null, 2) + "\n");
console.log(`wrote ${output} (${materials.length} material profiles, ${textureEntries.size} textures, ${unresolved.length} unresolved)`);
if (materials.length < 90 || textureEntries.size < 100 || unresolved.length > 0) process.exitCode = 1;
