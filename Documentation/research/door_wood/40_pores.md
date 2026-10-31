# Door specks: the pore pits in the door normal maps (package door_pores)

游戏视觉, 2026-10-31. Package `W/apply/door_pores.sh` (v2, READY, not applied). Sheet `W/door_speck/door_pores_before_after.jpg`.

## What Red saw

Two crops with no words (`ref/red_crop_1.webp`, `ref/red_crop_2_lobby_edge.webp` = cell "F4 PERIOD · LOBBY EDGE 57.2×17.5" of `research/door_frame/images/door_frame_options.jpg`). Inside the lamp's highlight on the upper left of the wooden leaf, and on the frame head above it, the surface broke into bright separate drops, like water on the door. Outside the highlight the wood looked right. A satin lacquered door should show one soft highlight.

## Cause, as measured

- The drops came from the normal maps, not the albedo or the mask. With a flat normal the same renders show no drops (the "flat-normal floor" below).
- The pore term of the height in `gen_door_wood.py` made pits with 10 to 17 deg walls: leaf N tilt p99 10.0 deg, max 16.8; frame p99 13.0, max 18.9; 10 % of texels over 3 deg.
- At smoothness 0.72 the lamp's highlight is about 2.9 deg wide at half height, so each pit wall caught the lamp on its own and showed as a separate bright drop.
- Material or shader values did not fix it: `_BumpScale` 0.25 left 48 specks at LOBBY EDGE, `_Smoothness` 0.85 left 294, smoothness 0.5 to 0.55 would lose the satin look Red picked, and specular anti-aliasing does nothing at mip 0.

## What changed

`Tools/lookdev/gen_door_wood.py`, FINAL maps only:

- `FINAL_LACQUER = dict(pore_relief=.05, telegraph=0.0)`, passed to `face()` and `frame()` for FINAL. The height is `-blur(pores, .6) * pore_relief + .10 * telegraph * blur(dark, 2.0)`. A pore keeps about 14 um of its 272 um, under 1 deg.
- `telegraph` 0: on an 8-bit normal map the faint grain term could only make whole 0.45 deg code steps, which ribbed the highlight.
- `save_normal(..., centred=True)` for FINAL adds half a code, so a flat texel lands on code 128 instead of flickering between 127 and 128.
- Only the height changes. `pores` also builds the albedo and the S mask, so those are byte for byte the same.

The three N maps it writes: `DoorVeneer_N.png`, `DoorFrame_N.png`, `DoorFrameU_N.png`. Those four files are the whole payload.

## Numbers (specks / pits in the region; flat-normal floor in brackets)

| Pose | Before | After |
|---|---|---|
| LOBBY EDGE desktop, leaf | 385 / 139 | 6 / 2 (6 / 1) |
| LOBBY EDGE desktop, frame | 119 / 30 | 2 / 3 (4 / 4) |
| LOBBY EDGE phone, leaf | 420 / 149 | 14 / 13 (14 / 12) |
| LOBBY EDGE phone, frame | 133 / 40 | 1 / 4 (2 / 3) |
| 1.2 m desktop, leaf / frame | 250 / 36 | 8 (6) / 9 (11) |
| 1.2 m phone, leaf / frame | 246 / 47 | 2 (2) / 7 (5) |
| Grazing desktop, leaf / frame | 24 / 43 | 15 (13) / 17 (18) |
| L0-F leaf core 45 cm, lamp mirror direction | 530 | 43 (42) |
| L0-F head 45 cm | 65 / 34 | 1 / 0 (1 / 0) |
| Break stages at 70 cm, M / D1 / D2 | 855 / 908 / 835 | 12 / 37 / 67 (12 / 38 / 62) |
| Office leaf 70 cm | 530 | 24 (20) |

N tilt p99 / max: leaf 10.03 / 16.84 -> 0.71 / 1.31 deg; frame and FrameU 13.04 / 18.93 -> 0.71 / 1.59 deg; texels over 3 deg 10 % -> 0 %. Pores still read faintly in the sheen as slightly duller dashes (through the unchanged S map), never as dots. Full tables: `after_r1/speck_table.tsv`, `work_r1/before_x/speck_table.tsv`; 40 extra sheets in `after_r1/extra/`.

## What did not change

- Albedo and S maps, all 6 byte-identical to main (Red's pick G, FINAL_FACE, and the walnut colour; frames F4).
- All 9 texture `.meta` files (GUIDs, importer), sizes and formats: leaf N 1024x2048, frame N 1024x2048, FrameU N 2048x1024.
- The 7 door materials and the shader. No C#. Phone cost does not rise (the new N PNGs are smaller: leaf 257 KB against 1.3 MB).
- The research options DW1 to DW4 come out byte-identical to main's generator. The generator is deterministic (two runs, same 9 hashes).

## Assets that share these maps

- `Door_Veneer.mat`: `Kit_DoorLeaf_Veneer_DA` on L0-F, OF-F and EX-F; the title and station leaves; FrontRoomsMapWorld; FrontRooms3DGame darkMat; FrontRoomsStairwell (code reference).
- `Door_Veneer_Damage_M`, `_D1`, `_D2`, `_TORN`: the same DoorVeneer A/N/S plus their own damage normal, so they take the fix; the dent rings still read.
- `Door_Frame.mat`: `Kit_DoorFrame_Wood_DA`.
- `Door_FrameU.mat`: the title kit casing (FrontRoomsDoubleDoorKit), FrontRoomsRoomStream linings, FrontRoomsStationDress walnut.

## Limits

1. The title before/after renders are mirror twins (the harness picked a different one of two equal leaves); judged against each run's own floor.
2. The title casing poses show no highlight on the casing; FrameU was checked by its map numbers and Door_Frame at the L0-F head and jamb.
3. Station and D1.6 were not rendered (harness: no leaf for station). The stairwell leaves are `Stair_SteelLeaf`, not wood.
4. ASTC was not measured; the phone profile renders with OSXUniversal imports.

## Package

`door_pores.sh`: 4 replaced paths (the three N maps, then the generator), 3 N `.meta` kept, no new path. Section 5 also refuses if any of the 6 A/S maps is not at the sha1 the N maps were rendered with. Bases = main 2026-10-31 00:31 (generator e1e67110 = door_veneer_g's payload). Dry run against main: DRY RUN OK. Scratch rehearsal `W/tools/_test/door_pores_1031/t_apply_script_tests.log`: MANIFEST OK, then ALREADY APPLIED; a changed A map REFUSED. Builder: `W/door_speck/build_package.py`.
