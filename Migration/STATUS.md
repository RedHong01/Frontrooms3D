# Migration status

Branch: `unreal-migration`  
Unity baseline: `6000.3.10f1` / URP `17.3.0`  
Contract schema: `1`

## Completed in this worktree

- [x] Copy the audit into the Git worktree.
- [x] Export the authored level profile and four room modules into a portable JSON contract.
- [x] Record map units, chunk dimensions, rebase threshold, build scene and golden seeds.
- [x] Record the 31 FMOD events, 5 buses and 19 parameters.
- [x] Add the Python `MapHash`/xorshift oracle with Unity-derived golden vectors.
- [x] Add source-backed regression tests for the exporter and hash oracle.
- [x] Add the Unity Editor golden-chunk exporter for the three validation seeds.
- [x] Export all 113 prop sidecars into a mesh/collider/anchor/LOD manifest.

## Next implementation gates

- [x] Compile the engine-independent C++ hash implementation against the Python oracle.
- [x] Define engine-independent C++ grid/zone/chunk data shapes from the Unity contract.
- [ ] Run the golden-chunk exporter in Unity and review the per-seed JSON.
- [ ] Add the UE `UPrimaryDataAsset` importer for the profile/modules/sidecars.
- [ ] Add the first UE runtime slice: fixed Title stream, one chunk, player movement, door, key and Relay Listen→Chase.
- [ ] Add screenshot/input/audio traces before upgrading materials and lighting.

The JSON contract is generated, so rerun the exporter after changing the Unity
source. Do not hand-edit `Migration/exports/frontrooms_contract.json`.
