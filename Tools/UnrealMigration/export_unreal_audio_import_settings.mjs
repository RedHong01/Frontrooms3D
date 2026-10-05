#!/usr/bin/env node
import fs from "node:fs";
import path from "node:path";

const root = path.resolve(process.argv[2] || process.cwd());
const files = [
  ["Assets/Resources/Audio/door-creak.wav", "Unity/door-creak"],
  ["AudioSource/FMOD_Library/Ambience/amb_air_hall_loop.wav", "FMOD/Ambience/amb_air_hall_loop"],
  ["AudioSource/FMOD_Library/Door/door_stream_swing_01.wav", "FMOD/Door/door_stream_swing_01"],
  ["AudioSource/FMOD_Library/Door/door_latch_soft_01.wav", "FMOD/Door/door_latch_soft_01"],
  ["AudioSource/FMOD_Library/Foley/plr_key_pickup_01.wav", "FMOD/Foley/plr_key_pickup_01"],
  ["AudioSource/FMOD_Library/Foley/plr_step_any_run_cloth_01.wav", "FMOD/Foley/plr_step_any_run_cloth_01"],
  ["AudioSource/FMOD_Library/Relay/rly_step_carpet_walk_body_01.wav", "FMOD/Relay/rly_step_carpet_walk_body_01"],
  ["AudioSource/FMOD_Library/Window/win_shatter_01.wav", "FMOD/Window/win_shatter_01"],
];

const groups = [];
for (const [relative, destination] of files) {
  const filename = path.join(root, relative);
  if (!fs.existsSync(filename)) throw new Error(`Missing audio source: ${relative}`);
  groups.push({
    GroupName: `FrontRooms_Audio_${destination.replaceAll("/", "_")}`,
    Filenames: [filename],
    DestinationPath: `/Game/FrontRooms/Audio/${destination}`,
    bReplaceExisting: true,
    bSkipReadOnly: false,
  });
}

const output = path.join(root, "Migration/exports/unreal_audio_import_settings.json");
fs.writeFileSync(output, `${JSON.stringify({ ImportGroups: groups }, null, 2)}\n`);
console.log(`wrote ${output} (${groups.length} Unity/FMOD audio sources)`);
