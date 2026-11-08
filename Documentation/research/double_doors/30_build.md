# Double doors · 30 · Build: the zone pairs

- **Line:** 游戏视觉 (ledger key `visual`), account G. Workflow visual-zone-doors-refine (wf_afdae4db-b34, parent session local_1fc9f09c), stage "Build A: kit API + Office, Station D + Waiting, LOW, Run!", 2026-11-06 10:48 PST on. **Part A** is this stage's; **Part B** (the pool) is appended by Build B, **Part S** (the stairwell doors) by the Stairwell doors stage, **Part M** (the map's door rows) by the Map doors stage, **Part R** (the Run! polish: the side doors' frame, the compartment pairs' closers) by the Run! polish stage.
- **Status:** Part A BUILT IN THE CLONE `W/proj_doorlean` and **LANDED in main 2026-11-06 15:34** (`W/apply/zone_double_doors`, MANIFEST OK, 67 paths; its docs 15:35). Part B (the pool) LANDED in main 2026-11-06 19:36 (`W/apply/zone_double_doors_pool`, MANIFEST OK; held off in the game by `PoolDoorsLive`, §B7, §B15). Part S (the stairwell doors) LANDED 20:40, live (`W/apply/stairwell_doors`, §S9). **Part M** (the map's door rows) LANDED 21:57, live (`W/apply/zone_map_doors`, §M9). **Part R** (the Run! polish) is built in the clone and PACKAGED as `W/apply/run_door_polish` (16 paths, DRY RUN OK, Roslyn macOS + iOS 0 errors), not landed: §R9. Gate: Roslyn macOS + iOS 0 errors, Unity compile in the clone 0 `error CS`, the leaf-wear shader's variant gate (§A7). The Package stage makes the apply package from the list in §A9; the Renders stage shoots the TODAY | NEW sheets and films. **Fix A** (2026-11-06 14:0x-15:xx, Review A's FIX list) changed the code, five kits, the masks and six materials and re-shot every NEW render: §A12; the statements below that it changed carry "(Fix A ...)". **Package** (2026-11-06 15:16-15:4x): `W/apply/zone_double_doors`, DRY RUN OK on main, rehearsal MANIFEST OK, Roslyn macOS + iOS 0 errors: §A13.
- **Red's asks** (this line's chat, plain user turns, verbatim): 2026-11-05 19:17:06 "同时我需要游戏视觉显根据当年做调研，去翻新每一个区域的双开门模型，规格和结构上的要求和之前一样"; 19:17:37 "把这个工作当作第一要务"; 19:35:21 "目前level 0的门不需要改变，我是需要你改变办公室，车站，后场，run！，泳池等的门"; 19:39:25 "我需要你看一下之前在项目里做的所有门相关的调研，现在还剩下那些门没有细化，给我清点一下，避免重复性工作，然后做细化和模型升级的调研和模型重做等工作。不要推翻之前已经做了的工作".
- **Red's decisions built here** (journal PARENT NOTEs): 21:15:15 "按默认做" (D-3 station D's face; D-4 the Office leaf stays walnut G); 21:25:37 "这里改成选项 2：整扇门都换成区域的门。* 玩家在 Level 0 房间里走向下一扇门时，会先看到办公室的门，算是一点预告。" (D-1 = the whole door). D-2 (the back-of-house map member) is the Map stage's; it reuses this part's back-of-house language.
- **Built from:** `20_design.md` Part A (this folder), on the landed kit `FrontRoomsDoubleDoorKit` b7ab5af0 and everything the inventory's §2 lists (reused, not redone).
- **Marks:** door space as the kit (x across, 0 the opening's centre; y up; z through, +Z the stream's swing face). Tris are LOD0 / LOD1 / LOD2 from the kits' sidecars. "C" = `W/proj_doorlean`.

---

# Part A · The kit API, Office, station D + night post, back-of-house LOW, Run!

## A0. What was built, in ten lines

1. **One switch, one look per door.** `FrontRoomsDoubleDoorKit.Today` (default false; always true on WebGL players) and `Zone { Level0, Office, NightPost, BackOfHouse, Run, Pool }`. Level0 is today's walnut pair byte for byte. Pool is in the API and wears Level0 until Build B builds its face (`ZoneBuilt`).
2. **The landed kit file changes by one word** (`partial`); everything new is in `FrontRoomsDoubleDoorKit.Zones.cs`: `Pair`, `BuildPair`, `Prepare`, `Dress`, `SetStationFace`, the trim stops, `SoundOf`, the zone slabs.
3. **Seven Blender kits** with hand-built LOD0 / 1 / 2 and sidecars: the F4 steel pair frame (1,088 / 228 / 132), station D's casing (516 / 132 / 108), two casing guards (324 / 28 / 4), the F1 and F5 pressed-steel lite kits (536 / 112 / 16) and the F12 sight-proof louvre (1,184 / 240 / 24). (Fix A, §A12; Build A had 144 / 96 / 72, 108 / 28 / 4 and 992 / 240 / 24.)
4. **Seven materials and three wear masks**: the back-of-house grey (`Door_BackOfHouse`), and the leaves' `_L` / `_R` materials with the new `_FR_LEAFWEAR` layer (office, service and cart masks, 1024 x 2048 each).
5. **One shader change:** `FrontRooms/Surface` gains `_FR_LEAFWEAR` (a `shader_feature_local_fragment`, off in every existing material: their variants compile as before).
6. **The title stream:** door *s* wears the look of the room it opens into, else of the room it closes, else Level 0; dressed in `AssignTitleFamily` while the slot is sealed; every zone part is built once per slot at `BuildDoor`, switched off.
7. **The start door is always a Level 0 door** (rule S1): a Level 0 room facing a zone door hands over one room later; the terminal's far door is dressed Level 0 unseen.
8. **Station D (D-3)** on both builders: the stream's terminal pair at `MapRunStarted` and StationDress's static entrance, the same parts in the same place; its bronze cap stands 1 mm proud of the map's south liner (Fix A).
9. **The vignettes' end trims** meet the steel frame's face edge (1.2628) instead of stopping 20 mm short; under `Today` they keep today's 1.27; the Office furniture keep-outs and the pool's band keep today's numbers.
10. **Proofs** (§A6): Today = true renders today (title and start door within 1/255; station D on both builders identical to the pixel and object for object); a Level 0 room changes only inside its door's frame outline with the zone looks on; the S1 handoff and station D's face; batch smokes of Tour and Descent with no exception of ours; the pairs against the design's estimates in §A6.5 (after Fix A, §A12: B-2 24 % over; S-1 and S-2 over by the cap's and the guards' detail).

## A1. Kits (Blender 4.3 silent, `Tools/Blender/frontrooms_kit/build_asset.py`, hand-built LODs `fr_lods`)

All seven are new; every module asserts its envelope (the build stops on a miss). Shared numbers live in `door_pair_common.py` (the pair's door space, the F4 section, the guards' and lites' rules); it mirrors `FrontRoomsDoubleDoorKit`'s constants and asserts them (2.5 mm barrel-to-rise, 1.0 mm barrel-to-guard, the barrels 1 mm in front of the land).

| Kit | Module | LOD0 / LOD1 / LOD2 tris | Switch (m, lodBias 1, vFOV 76) | Slot | What it is |
|---|---|---|---|---|---|
| `Kit_DoorFrame_SteelPair_DA` | `door_frame_steel_pair_da.py` | 1,088 / 228 / 132 | 4 / 12 / never | `Prop_SteelBrown` | the F4 16 ga sleeve round the 2.4 x 2.66 m opening, both faces: soffit x 1.200, the 12 mm hinge step (land at \|z\| 0.02225), the rise at x 1.212, the 50.8 mm face band at \|z\| 0.0331, the return to z 0.0195; mitred head welded and ground; the fluted saddle in the frame's finish with 7 countersunk flat heads. LOD0 3-step 3 mm convex / 1.5 mm concave bends; LOD1 1-step, the saddle 3 facets, no screws; LOD2 sharp bends. Sidecar `casingOuter` 1.2628, `casingTop` 2.7228 |
| `Kit_DoorCasing_StationPair` | `door_casing_station_pair.py` | 516 / 132 / 108 (Fix A; was 144 / 96 / 72) | 4 / 12 / never | `Prop_SteelBrown` | D-3: the +Z face only, the rise from the walnut lining's end, a 38 mm band to today's 1.250, the return to z 0.0195; the cap over the lining's +Z end (x 1.200-1.212, head y 2.660-2.672). Fix A: the cap fills to z 0.025 (1 mm over the map's south liner, which hid Build A's 0.5 mm cap), its inner face at x 1.1995, 0.5 mm notches round the barrels, closed boxes on shared breakpoints (LOD0 notched, LOD1 / LOD2 five boxes) |
| `Kit_DoorGuard_Casing38` | `door_guard_casing38.py` (+ `_common`) | 324 / 28 / 4 (Fix A; was 108) | 2 / 8 / 30 | `Station_StainlessGuard` | D-3's guard: 1.5 mm #4 stainless formed over the casing's inner arris (inside radius = the casing's 3 mm bend: it sits on the steel), face wing 35 mm, side wing down the rise to the cap (Fix A: 0.05 mm into the thick cap's face), y 0.10-1.32, eased edges (Fix A: 8-step bend, 0.6 mm ease in 2 segments); 1.0 mm clear of the shut barrels |
| `Kit_DoorGuard_Casing51` | `door_guard_casing51.py` | 324 / 28 / 4 (Fix A; was 108) | 2 / 8 / 30 | `Station_StainlessGuard` | S-2's guard on the frame kit's 50.8 mm band, side wing to 0.3 mm over the frame's concave bend at the land (Fix A: 8-step bend, 0.6 mm ease) |
| `Kit_DoorLite_F1_Steel` | `door_lite_f1_steel.py` (+ `door_lite_steel_pair_common.py`) | 536 / 112 / 16 | 2 / 8 / 40 | `Painted_Metal` (swapped to the leaf's paint) | B-2's 10 x 30 in lite: interact_door_lite_common's pressed section on the 44.5 mm leaf (flange back 0.1 mm under the face, top 2.55 mm proud), both faces, 6 slotted oval heads on -Z; 300 sq in, bottom 45 in. Fix A: the flange's outer edge is a feathered slope (16-22 deg) from the flat at 11.5 mm into the face at 19 mm (Build A: a 45 deg chamfer over a 0.65 mm step), at LOD1 too; screws at 7.5 mm on the flat |
| `Kit_DoorLite_F5_Steel` | `door_lite_f5_steel.py` | 536 / 112 / 16 | 2 / 8 / 40 | as F1 | C-1's 15 x 15 in lite, 6 screws on -Z (Red's set A, lite_common: "F5 takes six too"); 225 sq in, bottom 47 in |
| `Kit_DoorLouvre_F12` | `door_louvre_f12.py` | 1,184 / 240 / 24 (Fix A; was 992) | 2 / 8 / 40 | as F1 | B-2's A2 louvre, 10 x 16 in at 13-29 in: 16 inverted-V 20 ga blades at 25.4 mm, each 41 mm deep rising 29.3 mm (55 deg: every horizontal sight line meets a blade; light passes) with a 5 mm hemmed lip down at both feet (the louvre's horizontal lines from standing height, §A8 #9), every face turned by its section normal, the pressed frame with its leg run to the centre plane, 8 screws on -Z; its frame 36.2 mm over the 10 in kick plate. LOD1 the blades' outer faces only. Fix A: the frame takes the feathered edge; each lip ends in a modelled hem (r 0.45 mm), not a flat cut |

FBX + JSON in `Assets/Resources/Props/Models/`, imported in C by `FrontRoomsKitImporter` (LODGroups from the `_LOD0/1/2` names and the sidecars' `lodDistances`). Blender log: `W/zone_doors/logs/blender_kits.log`.

## A2. Materials, masks and the shader

| Asset | Recipe | Used by |
|---|---|---|
| `Door_BackOfHouse.mat` | `Painted_Metal`'s maps, `_BaseColor` stored (0.6682, 0.6629, 0.6607), which Unity decodes to the linear tint (0.404, 0.397, 0.394) (A8 #11): the maps' mean linear (0.650, 0.610, 0.512) lands at sRGB about (140, 135, 124), albedo Y 0.24 (1.06 stops over walnut G, 1.5 under `Door_Enamel`: T6 holds) | B-2's lite and louvre kits; the D-2 map member's free row (Map stage) |
| `Door_BackOfHouse_L` / `_R.mat` | the same + `_FR_LEAFWEAR`, `DoorWear_BackOfHouse`, chip = grey shop primer, linear (0.150, 0.150, 0.145), stored sRGB-encoded (0.4236, 0.4236, 0.4169) (Fix A: Build A stored the door break's primer (0.270, 0.181, 0.117) as if linear; the Color slot linearised it to (0.059, 0.027, 0.013): near-black red "drops") | B-2's slabs |
| `Door_Veneer_OfficeWear_L` / `_R.mat` | `Door_Veneer` (walnut G, unchanged maps and values) + `_FR_LEAFWEAR`, `DoorWear_Office`, chip = pale walnut, linear (0.43, 0.26, 0.13) stored (0.6872, 0.5469, 0.3959) (Fix A) | O-1's slabs (`Door_Veneer` itself untouched) |
| `Door_RunAlmond_L` / `_R.mat` | `Prop_SteelAlmond` + `_FR_LEAFWEAR`, `DoorWear_Run`, chip = the grey primer as B-2 (Fix A) | C-1's slabs |
| `Textures/DoorWear_{Office,BackOfHouse,Run}.png` | 1024 x 2048 RGBA linear (R grime, G dull, B burnish, A chip), from `Tools/lookdev/gen_door_wear.py` (numpy, fixed seeds 51106-51108): soft, noise-broken hand halos round the push plates (patchy, on the meeting-edge side, with finger smudges), knuckle marks at the meeting stile, the Office's chair-arm band and pale nicks over the kick plate, the service door's primer chips and hand-truck marks, the cart chips, rubber and dents; the HW-DA1 plate rects clear. Fix A: chips are angular flakes (5-8 sided, wider than tall, in clusters, 3 x 3 supersampled), not round blobs; the Office's halo, knuckle and edge grime about 60 % stronger. Importer: `FrontRoomsSurfaceTextureImporter` (linear, BC7, mips); the metas cap iOS and WebGL at 1024 | the leaf materials |

Rect L = (0, 0, 1 / 1.1955, 1 / 2.639), rect R = (1.1955, 0, -1 / 1.1955, 1 / 2.639): the left slab's mesh-UV u runs from its hinge edge, the right one's from its meeting edge, so mask u 1 is always the meeting edge.

**Shader** (`Assets/Resources/Rendering/FrontRoomsSurface.shader`, main 87a758e1 + 26 lines): `[Toggle(_FR_LEAFWEAR)] _UseLeafWear`, `_FR_LeafWearMap`, `_FR_LeafWearRect`, `_FR_LeafWearChip`; `#pragma shader_feature_local_fragment _FR_LEAFWEAR` (ForwardLit); `_FR_LeafWearRect` and `_FR_LeafWearChip` in the shared CBUFFER (one SRP-batcher layout for every pass); one fetch in mesh-UV metres after the base samples: albedo x (1 - 0.35 R), then toward the chip colour by A (before the macro wear); smoothness x (1 - 0.5 G) x (1 - 0.3 A) + 0.20 B (after the smoothness is formed). Mesh-UV materials only (`_FR_MESH_UV`). No existing material enables it, so every variant they use compiles as before.

## A3. The code (all 游戏视觉's files, RULES rule 3; no file of 关卡设计, 交互设计 or 声音设计)

| File | Base (main) | Change |
|---|---|---|
| `Assets/Scripts/Rendering/FrontRoomsDoubleDoorKit.cs` | b7ab5af0 | one word: `public static partial class` |
| `Assets/Scripts/Rendering/FrontRoomsDoubleDoorKit.Zones.cs` | new | the zone API (below) |
| `Assets/Scripts/FrontRoomsRoomStream.cs` | 6893ef1a | `RoomSlot.door` / `doorZone` / `doorFamilySide`; `BuildDoor` builds the pair with `BuildPair` (the same objects in the same order) and fills the old fields from it, then `PrepareDoorZones`; the D-3 hook on `FrontRooms3DGame.MapRunStarted`; `DoorZoneOfHinge` (inert, the sound contract's option (b)) |
| `Assets/Scripts/FrontRoomsRoomStream.TitleRooms.cs` | 8100738c | `DoorZoneOf(family)`, `DoorZoneFor`, `DressSlotDoor` (the last step of `AssignTitleFamily`), `PrepareDoorZones`; `AirlockTerminal` rule S1; `PrepareTitleHandoff` dresses the terminal's far door Level 0; the edit-mode family preview shows each family's door; `OnDestroy` lets go of the D-3 hook's stream |
| `Assets/Scripts/Rendering/Title/FrontRoomsTitleVignettes.cs` | 5133251e | `DoorCasingHalf` / `DoorCasingTop` read the kit's trim stops; the Office furniture keep-outs stay 1.27 (`DoorKeepHalf`: the seeded layout must not move); the pool's band ends read `TrimStopHalfFor(Zone.Pool)` (today's until Build B) |
| `Assets/Scripts/Rendering/FrontRoomsStationDress.cs` | 759a22a9 | `DressEntrance` builds the pair with `BuildPair(Plus)` (then `PlaceLeaf` as before) and calls `SetStationFace(pair, true)` before the renderer loop, so `own` and `SetMask` take the new parts |
| `Assets/Resources/Rendering/FrontRoomsSurface.shader` | 87a758e1 | `_FR_LEAFWEAR` (§A2) |

**`FrontRoomsDoubleDoorKit.Zones.cs`**, the API of 20_design §A2.3 as built:

- `enum Zone : byte { Level0, Office, NightPost, BackOfHouse, Run, Pool }` and `enum DoorSound : byte { Wood, Steel, Composite }` (append-only).
- `static bool Today` (false: the zone looks; `#if UNITY_WEBGL && !UNITY_EDITOR` always true). `ZoneCasingOuter` 1.2628, `ZoneCasingTop` 2.7228, `TodayTrimHalf` 1.27, `TodayTrimTop` 2.73; `TrimStopHalf` / `TrimStopTop` and `TrimStopHalfFor(zone)` / `TrimStopTopFor(zone)`; `ZoneBuilt(zone)` (Pool false until Build B); `SoundOf(zone)` (Steel for BackOfHouse and Run, Composite for Pool, else Wood).
- `sealed class Pair`: `Faces`, `Zone`, `FamilySide`, `StationFace`, `Frame`, `Pivot(hand)`, `Carrier(hand)`, `HingeBody(hand)`, `Slab(hand)`; it holds today's parts (the walnut lining and casing, the oak saddle, the hinge renderers, the slabs' meshes and material, each lite's glass) and the prepared zone parts.
- `BuildPair(pivotParent, frame, m, faces, colliders)`: `BuildFrame`, then `BuildLeaf` for hand -1 and +1, exactly as the callers built it before, gathered by exact child name (`ChildNamed`; Fix A: `Transform.Find` read the '/' in "door hinges / jamb halves" and "left|right door hinge barrels / swing side" as a path, returned null, and the zone hardware never reached those barrels; the names stay, 声音设计 reads them).
- `Prepare(pair, zone)`: once per zone and slot, Faces.Both only, nothing under `Today`: the frame kit (shared by the four whole-door zones), the night post's 4 guards, the back-of-house pair's 2 lite kits and 2 louvres, the Run! pair's 2 lite kits, all switched off; the zone's slab and glass meshes cached. A kit that cannot be loaded marks the zone failed: that door keeps today's look (logged once).
- `Dress(pair, zone, familySide)`: switches today's lining / casing and saddle against the frame kit; sets the 5 hinge renderers' material (satin chrome for the Office, 630 stainless for the others, today's for Level 0); swaps each slab's shared mesh (walnut F1, the two hollow-metal slabs, today's) and material (the `_L` / `_R` leaf materials); turns the lite and louvre kits and the night post's guards (on the family face) on or off; re-aims a lite whose form changes (`FrontRoomsDoorLite.AttachStream` on the same glass, `Ready` kept, DARK until the room behind connects). Nothing is instantiated; a re-dress that changes the lite form allocates only DoorLite's one-element material array per leaf.
- `SetStationFace(pair, on)`: §A5.
- Meshes (cached in the kit's dictionary): `LeafMeshLiteRect` (today's `LeafMeshLite` with another rect: the Office's F1 in the walnut bead), `SteelSlab` (`SlabBuilder`: the slab with 1.5 mm arrises, stacked cut-outs of visible + 9.5 mm, the stile edges' 0.6 x 0.4 mm seam with its ends capped; UVs as the walnut slab, metres from the min corner; Fix A: a conforming grid, every face, reveal, edge strip and chamfer split at every hole side, rail and groove line: no T-junction, every edge in two faces; 224 tris a Run! leaf, 308 a back-of-house leaf), `GlassMeshFor` (F1 / F5 glass, as `BuildLite`'s), `FrameMeshMinusZ` (today's linings and -Z casing, vertex for vertex), `LiningsMesh`.
- Names (none holds "floor", "carpet", "ceiling", "wall", "seal", "partition" or "threshold marker"): `door frame steel (zone kit)`, `left|right door lite kit F1|F5`, `left|right door louvre F12`, `door casing guard ±Z ±x`, `door casing steel (station face)`, `door casing guard +Z ±x (station face)`, `door frame lining and casing -Z`, `door linings`.

## A4. The title stream

- **Which look:** door *s* (the far door of room *s*): `DoorZoneOf(FamilyForSequence(s + 1))` if built (side +1), else `DoorZoneOf(room.family)` if built (side -1), else Level0. Families: Office -> Office, Waiting -> NightPost, Low -> BackOfHouse, Run -> Run, Pool / DarkPool / Club -> Pool (Level0 until Build B).
- **When:** `DressSlotDoor` is the last step of `AssignTitleFamily` (Initialize's rooms 0-4 before the first frame; `MaintainPool`'s recycled slot, sealed two shut doors away). Development builds log a re-dress of the current room's or the next one's door (none was logged in the smokes, §A6).
- **Tour** (pool stops on, Pool not built yet): doors 0 Level0, 1 Office (+Z), 2 Office (-Z), 3 Run (+Z), 4 Run (-Z), 5 NightPost (+Z, guards on the night post's face), 6 NightPost (-Z), 7 and 8 Level0 (the pool's, until Build B), 9 Office ... **Descent:** 1 Office, 2 Run, 3 BackOfHouse, 4 NightPost, 5 NightPost (the night post's exit: the pool ahead is not built), 6 and 7 Level0, 8 Office.
- **Prepared once:** `BuildDoor` calls `PrepareDoorZones` for every family `WantedFamilies(false)` returns, so a recycle switches parts only. Tour: 7 inactive objects a slot (the frame kit, the night post's 4 guards, Run!'s 2 F5 kits; the Office needs none), 35 in the five slots; Descent 11 a slot (plus the back-of-house's 2 lite kits and 2 louvres), 55. Their meshes are the kits' shared meshes.
- **Rule S1:** `AirlockTerminal` passes on when the room is a family room or its far door wears a zone look (the room past it is sealed); `PrepareTitleHandoff` then dresses the terminal's far door Level0, only when the terminal lies past the camera's room (unseen). Today: every door is Level0, so the rule is today's.
- **Trims:** the vignettes are built at `BeginTitleRooms` with the stops of the moment (`Today` decides): 1.2628 / 2.7228 with the zone looks, 1.27 / 2.73 under Today.

## A5. Station D (D-3) on both builders

- **The stream's terminal pair** (start-area phase): `FrontRoomsRoomStream.HookStationFace` (`RuntimeInitializeOnLoadMethod`) listens to `FrontRooms3DGame.MapRunStarted`; with the stream ended (`frozen`), `map.HasStation` and not `Today`, the terminal slot's pair gets `SetStationFace(true)`. In `StartRunInPlace` the event comes after `EndStreamAt` and `TerminalDoorHeld = true`: the door is shut, its +Z face on the map side.
- **StationDress's static entrance** (after `ClearStartArea`): `BuildPair(Plus)`, `PlaceLeaf` x 2, `SetStationFace(true)`.
- **What `SetStationFace(true)` does:** today's walnut "door frame lining and casing" off; on: a walnut mesh of the same linings (Both: plus today's -Z casing, vertex for vertex), `Kit_DoorCasing_StationPair` (38 mm band; the cap over the lining's end, since Fix A to z 0.025, 1 mm proud of the map's south liner, notched round the barrels), `Kit_DoorGuard_Casing38` on both jambs (+Z, y 0.10-1.32), the +Z barrels' material 630 stainless. Kept byte for byte: the leaves, F4b (live on the stream, the dead skin on the entrance), the oak saddle, the -Z face, the brushes, the HW-DA1 plates. The BUSINESS HOURS plate stays at `PlateAnchor` (75 mm from the opening; the band ends at today's 1.250), the rail and base stop 50 mm out, no header panel (the band tops at 2.710).

## A6. Proofs (the sheets are in `sheets/`, Shot format, every label checked)

**How.** Batch Play mode in C (`FrontRoomsTitleCapture`, clone only; OSXUniversal, audio off), 1920 x 1080, quality High, game time at 60 Hz, the title camera. S1 = main's code (Tour and the start door also with main's shader; Descent already had the new shader, which no material of main's code enables); S2 and S3 = Build A, `-doorsToday on` (Today) or `off` (the zone looks); S4 = the door lab (edit mode). A difference is 8-bit sRGB, the max over R, G and B. The title TV screen (x 0-130, y 340-430 px) is masked: its video differs between any two runs. Frames after Space (the map: ho_map, ho_open, ho_back) are not compared pixel for pixel: the run's seed is random, and two runs of one build differ across the frame there. **The noise floor**, two runs of one build (S2 vs S3): 0 to 4 px at 1/255 a frame, none over it.

### A6.1 Today = true renders today (`double_doors_build_a_today.jpg`, PASS)

| Frames | Compared | Over 1/255 | At 1/255 |
|---|---|---|---|
| Tour r0: Level 0 room 0, door 0 | S1 tour vs S2 tour_today | 0 | 3 |
| Tour r1: Level 0 room 1 (the Office's door ahead) | the same | 0 | 1,029, all round the TV (x 0-255, y 102-538: its glow on the wall) |
| Tour r2, r2a: the Office | the same | 0 | 223, 226 (§A10 item 1) |
| Tour r3, r4, r4a, r5, r6, r8 | the same | 0 | 0 |
| Tour r7, r9 | the same | 0 | 2, 1 |
| The start door: s0, ho_space, ho_door | S1 start0 vs S2 start0_today | 0 | 0 |
| Descent d1-d8 | S1 descent vs S2 descent_today | 0 | d1 1, d2 223 (the Office), d6 2, the others 0 |
| Station D, the stream's terminal pair (Both): -Z and +Z at 3 m, both 3/4 views, both jambs at 0.35 m | S4 lab: main's calls vs Build A's, one bay | 0 | 0 (6 shots) |
| Station D, StationDress's static entrance (Plus): +Z at 3 m, 3/4, jamb | the same | 0 | 0 (3 shots) |

**Station D is proved in the lab** because the game reaches its +Z face only after Space, where the seed changes every run, and S1's main-code harness could not shoot the back view (§A8 #10). `FrontRoomsZoneDoorLab -zdTodayProof on` (clone only) builds each builder's pair three times in the same place: with main's own calls (RoomStream.BuildDoor + SetHingeSide 6893ef1a; StationDress.DressEntrance 759a22a9), with Build A's (BuildPair, PlaceLeaf, and every zone call the game makes on a pair: Prepare for each zone, Dress Level 0, SetStationFace), and with main's again. It dumps every transform, component, mesh (content hash), material and collider: main vs Build A **identical** (60 objects on the stream pair, 34 on the static entrance), main vs main identical, the 9 shots identical to the pixel.

### A6.2 Level 0 stays Level 0 with the zone looks (`double_doors_build_a_level0.jpg`, PASS)

| Frames (S1 main vs S2 zone looks) | The door in view | Over 1/255 | Where |
|---|---|---|---|
| Tour r0 | door 0, between two Level 0 rooms | 0 (1 px at 1/255) | none |
| s0, ho_space, ho_door | the start door | 0 | none |
| Tour r7, r8; Descent d6, d7 | the pool's doors (Level 0 until Build B) | 0 | none |
| Tour r1, r9; Descent d1 | Level 0 room, the Office's door ahead | 23,398; 25,873; 23,398 | x 761-1160, y 367-796: the door's frame outline |
| Tour r3 | Level 0 room, Run!'s door ahead | 149,090 | x 759-1158, y 367-796 |
| Tour r5 | Level 0 room, the night post's door ahead | 16,198 | x 761-1160, y 366-796 |

In a Level 0 room only the door changes: nothing outside its frame outline moves by more than 1/255. In the family rooms their own end trims move too, as built (§A4 Trims: they now meet the frame's face edge): the night post's (Tour r6, Descent d5) reach x 747-1172 and y 810 beside the frame (d5 also 5 px at 2/255 along the base's shadow); Run!'s (r4, d3) 84 and 141 px just outside the outline, 3/255 at most.

### A6.3 The looks in the title, rule S1, station D (`double_doors_build_a_title.jpg` and `_lab.jpg` PENDING for the Renders stage, `_station.jpg` PASS)

- **Every door wears the look §A4 names, on the face it names:** Tour 1-2 Office, 3-4 Run!, 5-6 night post (guards on the night post's face), 9 Office; Descent 1 Office, 2 Run!, 3 back-of-house (seen from Run!: F1 kits and F12 louvres), 4-5 night post, 8 Office. The title sheet pairs TODAY and NEW at 4.5 m on the forward walk.
- **Rule S1:** Space in Tour room 1, facing the Office's door: the airlock is room 2, turned Level 0, and the start door is walnut (S2 and S3 start1_zone: ho_airlock, ho_door). Space in Descent's Office (room 2): room 3 (Run!) turned Level 0, door 3 walnut (S3 descent_space2). Under Today the rule is today's (every door is Level 0).
- **Station D (D-3) in the game:** S3 start0_zone's back view (turned round 5.5 m past the start door) shows the +Z face with the steel casing and both guards; start0_today's shows today's walnut casing (crops on the station sheet). (Fix A: the 630 barrels and the bronze cap were not in these frames: the barrels kept today's material (the Find bug) and the map's liner hid the cap; both show since, §A12.) The lab shows both builders with the same parts in the same place.
- **The lab** (S3, the lipped louvre): every look at 3 m and 3/4, the jambs at 0.35 m, the lites at 0.45-0.5 m, the louvre at 0.6 m and from 1.2 m at standing eye, the saddle at 0.5 m: no gap, slit or float at the frame's feet, the guards seated on the casing 1.0 mm off the barrels, the louvre reading as blades and lips. Its bays are geometry checks under two troffer spots, not the game's light.

### A6.4 Smokes and logs (`W/zone_doors/logs/`)

| Session | Runs | Result |
|---|---|---|
| S1 11:00-11:30 | tour, start0, descent (main's code), start1 (Build A's first smoke) | rc 0, 0 `error CS`, 0 console errors |
| S2 11:31-11:59 | the shader gate, the lab, tour / start0 / descent / start1 with the zone looks, tour / start0 / descent under Today | the same |
| S3 11:59-12:03 | the lab (lipped louvre), descent_zone, start0_zone, start0_today, start1_zone, descent_space2 | the same |
| S4 12:15-12:16 | the lab's Today proof | the same |

The one exception in every Play run, main's code included, is Unity's own QuickSearch indexer at Editor start (`SearchDatabase.cs:351`, before any scene loads). No "re-dressed while it may be seen" warning (it logs in the Editor too: `Debug.isDebugBuild`), no "could not be built". The title keeps main's timeline: Tour exits at 95.45 s and Descent at 85.02 s of game time in S1 and S2 alike.

### A6.5 The pairs against the design's estimates (20_design.md §A7)

| Look | Design [E] LOD0 / LOD1 / LOD2 | Measured LOD0 (lab census: the whole pair with its plates) | LOD1 / LOD2 | Renderers | Note |
|---|---|---|---|---|---|
| O-1 Office | +874 / +84 / −46 | **+862** | +2 / −94 | −1 | |
| S-1 Station D (per builder) | +270 / +55 / −40 | **+1,074** (Fix A; Build A +270) | +98 / +26 (Build A +62 / −10) | +3 (design +2) | the guards are two renderers; Fix A: the cap's closed boxes +372 (LOD1 / LOD2 +36), the guards' 8-step bend and ease +216 each |
| S-2 Night post | +974 / +108 / −38 | **+1,510** (Fix A; Build A +1,078) | +58 / −86 | +1 (design ±0) | two guards, on the family face only; Fix A: +216 a guard |
| B-2 Back-of-house | +3,290 / +540 / −110 | **+4,470** (+36 %; Build A +3,686, +12 %) | +874 / +154 (Build A +474 / −246) | +3 | each louvre 1,184 against 800 [E]: 16 blades with their sheet thickness, lips and (Fix A) hems; Fix A: the conforming slab +200 a leaf (every level), the hems +192 a louvre |
| C-1 Run! | +1,870 / +160 / −210 | **+1,934** (Fix A; Build A +1,662) | +226 / −62 (Build A −46 / −334) | +1 | Fix A: the conforming slab +136 a leaf (every level) |

Today's pair: 26,506 tris at LOD0 with its 8 plates, 21 renderers (S4 census). LOD1 / LOD2 by the design's own accounting with measured inputs: the kits' sidecars, minus today's walnut frame parts they replace (226 tris at every distance; the station's +Z casing half 90), plus the slab change measured at LOD0 (back-of-house −232, Run! −272; the slabs have one level). Masks: 3 x 2.7 MiB on desktop (BC7, mips), 0.7 MiB each at the iOS and WebGL cap of 1024. VISUAL_STANDARD §1.4: B-2 is over its estimate, said plainly here and in the summary for Red. **Fix A census** (the door lab, 2026-11-06 15:02, `W/zone_doors/captures/fa/lab/lab_log.txt`, the whole pair at LOD0 with its plates): Level 0 bay 26,578; Office 27,440 (+862); night post 28,088 (+1,510); back-of-house 31,048 (+4,470); Run! 28,512 (+1,934); station D stream pair 27,652 (+1,074): the table's LOD0 column to the triangle.

## A7. The gate

| Check | Result | Where |
|---|---|---|
| Roslyn (Unity 6000.6.3f1's own csc, defines and references), on main's tree with the 6 changed and new C# files of §A9 in place | macOS: COMPILE OK, 0 errors (Assembly-CSharp 280 files and every assembly that references it); iOS defines: COMPILE OK, 0 errors. Run at 11:24, after the last edit of those files (11:23:38) | `W/zone_doors/_roslyn/gate_mac`, `gate_ios` |
| Unity compile of C (batch, OSXUniversal) | 0 `error CS` in all 20 runs, S1 to S4 | `W/zone_doors/logs/s*_runs.txt` |
| The leaf-wear shader (`ShaderData.Pass.CompileVariant`, clone-only `FrontRoomsLeafWearCompileCheck`) | **PASS**: 273 Surface materials, 18 keyword sets among them, 27 checked (ForwardLit fragment on Metal macOS, Metal iOS and D3D with the full global set; LOD_FADE_CROSSFADE on the leaf-wear sets; vertex; ShadowCaster, DepthOnly, DepthNormals, MotionVectors): 112 OK, 0 failed. Its broken control fails 12 of 12 and the control's clean twin compiles 4 of 4, so the gate can fail. 916 s | `W/zone_doors/logs/leafwear_compile_check.txt` |
| The clone-only harness | Roslyn Assembly-CSharp-Editor 0 errors | `W/zone_doors/_roslyn/harness` |
| The apply dry run | `W/apply/zone_double_doors` (67 paths): **DRY RUN OK** against main; a rehearsal on a scratch copy of main **MANIFEST OK**, then ALREADY APPLIED; Roslyn re-run with the package's own payload (macOS and iOS 0 errors). The Package stage, §A13 | `W/zone_doors/logs/pkgA_gate.txt` |

## A8. Where the build differs from 20_design.md Part A, and why

| # | Design | Built | Why |
|---|---|---|---|
| 1 | the frame and casing returns end on the paper plane, \|z\| 0.022 | they reach \|z\| 0.0195, 0.5 mm into the wall face | station D's data wall face is at 0.020 (StationDress's entrance stands on it): a return ending at 0.022 would leave a 2 mm slit along the casing's outer edge on the static entrance (Red 10-29: "为什么现在门还是悬空的，两处还是有缝隙"); on the stream the foot simply sinks 2.5 mm behind the paper |
| 2 | F5 lite kit with 8 screws | 6 screws | Red's set A as `interact_door_lite_common` records it ("F5 takes six too"), the same count as the map's Run! leaves |
| 3 | the guards' inner bend 1 mm, face wings 38 / 50.8 mm | the inner bend = the casing's 3 mm outer bend; face wings 35.0 / 47.8 mm (to where the casing's outer bend starts); side wings stop 0.35 mm over the cap (38) and 0.3 mm over the frame's concave bend (51) | a guard formed tighter than the steel it covers stands off the arris (a slit at the corner); stopping short of the bends keeps it seated |
| 4 | tris [E]: frame 1,100 / 310 / 180; casing 260 / 120 / 40; guards 50 / 12 / 4; F1 500 / 120 / 24, F5 600 / 140 / 24; louvre 800 / 200 / 40 | 1,088 / 228 / 132; 144 / 96 / 72; 108 / 28 / 4; 536 / 112 / 16 (both); 992 / 240 / 24 | the sidecars (§A1); the guards' eased edges cost 58 tris at LOD0, the louvre's 16 blades with their sheet thickness and lips 320; the pairs' totals in §A6.5 |
| 5 | (not in the design) | the Office furniture keep-outs stay at 1.27 (`DoorKeepHalf`) | the seeded Office layout (OfficeDressSeed 285) must not move: a 7 mm shift of a keep-out can change which planner cells are free |
| 6 | Pool in the API | `Zone.Pool` exists, `ZoneBuilt(Pool)` is false: the pool family's doors wear Level0 and the pool's band ends keep today's stop (1.27) until Build B | Build B builds the pool face (D-5) |
| 7 | "about 11 inactive objects per slot, 55" | Tour 7 a slot (35); Descent 11 a slot (55) | the Office look needs no object (its lite is in the slab mesh) and Tour has no back-of-house room |
| 8 | the no-pop log "if its slot's portal is drawn" | logs a re-dress of the current room's door or the next one's | the portal flag is a frame stale at a recycle (UpdatePortals runs after MaintainPool) |
| 9 | the louvre's blades: plain inverted V's | each blade has a 5 mm hemmed lip down at both feet; its faces are turned explicitly; LOD1 keeps the outer faces only (304 to 240 tris) | the first lab (S2) showed the louvre as one flat panel: the blades' slopes all face one way and, under soft light, read as a single plane; the lips give the horizontal lines and the shadow on the slope below, as a pressed louvre has (S3 lab: it reads as blades from 1.2 m at standing eye). Sight-proof still holds (rise + lip over the pitch) |
| 10 | "render diffs of station D" under Today, in the game | in the lab (`FrontRoomsZoneDoorLab -zdTodayProof`, §A6.1) | the game reaches station D's +Z face only after Space, where the map seed changes every run (two runs of one build differ across the frame), and main's harness could not shoot the back view; the lab builds both builders' pairs with main's own calls and with Build A's in the same place |
| 11 | `Door_BackOfHouse`, `_L`, `_R`: `_BaseColor` (0.404, 0.397, 0.394), meant as the linear tint that lands the leaf at sRGB about (140, 135, 124), albedo Y 0.24 | (0.6682, 0.6629, 0.6607), the sRGB encoding of that same linear tint (fixed in C by the Renders A stage, 2026-11-06 12:5x; journal) | a material Color is sRGB in this linear-space project and Unity linearises it before the shader, so the value as built gave the shader (0.136, 0.130, 0.129): the leaf rendered at albedo Y about 0.08-0.09, darker than walnut G (lab S3: leaf Y 0.028 against walnut 0.036 under the same light), not the designed Y 0.24. The lite and louvre kits take the same paint. `sheets/double_doors_renders_boh.jpg` shows both (paint row, "Build A value") |

## A9. For the Package stage: the paths, with main's bases (checked unchanged in main 2026-11-06 12:2x)

(The payload sha1s in the table are Build A's; Fix A changed 28 of the 67 files. The package carries the final ones: `W/apply/zone_double_doors_expected.txt`, §A13.)

| Path | Main base (sha1) | Clone payload (sha1 at this stage) |
|---|---|---|
| `Assets/Scripts/Rendering/FrontRoomsDoubleDoorKit.cs` | b7ab5af0d3d518d22c1a3fe1d78d4fccfaa3f725 | f40a10f6641573a59596cd689e35fb94374bc61f |
| `Assets/Scripts/Rendering/FrontRoomsDoubleDoorKit.Zones.cs` + `.meta` | ABSENT | 89494382a7a25b3debc6a84579788b57f0d001cd, meta 6349cfc7422249ca90e96460568179fae6199313 (guid fdb02e20fed2b4145b180ef9fb0f8a25) |
| `Assets/Scripts/FrontRoomsRoomStream.cs` | 6893ef1abce248a45f52152c758b19d89c312338 | 4153e5cfb20928c93edc088f3cdf297121401a0f |
| `Assets/Scripts/FrontRoomsRoomStream.TitleRooms.cs` | 8100738cada47b1ae25e36e03528a9c67a5b5277 | d86034d7ac47c5e92f6e03c871c3a63b8fd36667 |
| `Assets/Scripts/Rendering/Title/FrontRoomsTitleVignettes.cs` | 5133251e28aef34622b121c4ea66d15b3fe896a1 | 0a96b41de07fb0703f23e5f1841b2be962e37700 |
| `Assets/Scripts/Rendering/FrontRoomsStationDress.cs` | 759a22a906aa736e96788d5df2dd29f9652bde61 | 99e8b043d54c72ccb5be7f63646b6248ffc751fa |
| `Assets/Resources/Rendering/FrontRoomsSurface.shader` | 87a758e1db3aca17728eac18f60e144ba525c430 | a5302188fb43db9ebd361fd495a623787f06da93 |
| `Assets/Resources/Props/Models/Kit_DoorFrame_SteelPair_DA`, `Kit_DoorCasing_StationPair`, `Kit_DoorGuard_Casing38`, `Kit_DoorGuard_Casing51`, `Kit_DoorLite_F1_Steel`, `Kit_DoorLite_F5_Steel`, `Kit_DoorLouvre_F12` (`.fbx`, `.json`, and their Unity-made `.meta`s) | ABSENT | 28 files |
| `Assets/Resources/Surfaces/Door_BackOfHouse`, `Door_BackOfHouse_L`, `_R`, `Door_Veneer_OfficeWear_L`, `_R`, `Door_RunAlmond_L`, `_R` (`.mat` + `.meta`) | ABSENT | 14 files |
| `Assets/Resources/Surfaces/Textures/DoorWear_Office`, `DoorWear_BackOfHouse`, `DoorWear_Run` (`.png` + `.meta`) | ABSENT | 6 files |
| `Tools/Blender/frontrooms_kit/assets/door_pair_common.py`, `door_frame_steel_pair_da.py`, `door_casing_station_pair.py`, `door_guard_casing_common.py`, `door_guard_casing38.py`, `door_guard_casing51.py`, `door_lite_steel_pair_common.py`, `door_lite_f1_steel.py`, `door_lite_f5_steel.py`, `door_louvre_f12.py` | ABSENT | 10 files |
| `Tools/lookdev/gen_door_wear.py` | ABSENT | 1 file |

Every payload file's sha1 at this stage (67 files: the 8 above, 28 kit files, 14 material files, 6 texture files, 11 tools) is in `W/zone_doors/tools/buildA_payload.sha1`; main has none of the new paths (checked 12:2x). Never package (clone only): `Assets/Editor/ZoneDoors/*` (the shader gate, its broken control, the door lab), `Assets/Editor/TitleStream/*` (the title captures), `Assets/Scripts/CloneOnly/*` (`-doorsToday`, silence). Landing order: the assets and the shader before the code, in one package (the code loads the kits by name and falls back to today's door when one is missing, so any order is safe). The importer is unchanged, so there is no import race in Red's open Editor (handoff §6).

**Changed by the Renders A stage (A8 #11):** three of the 14 material files: `Door_BackOfHouse.mat` eda4a52c22e87f86e97af34d72869c349e487463, `Door_BackOfHouse_L.mat` 6e376b97169d52de7af81e99ecf6b9e846097154, `Door_BackOfHouse_R.mat` f32b73cec7637994910afe1eb11f27dc9f870c95 (their `.meta`s unchanged); `W/zone_doors/tools/buildA_payload.sha1` carries the new sha1s. Every other payload file checked unchanged in C after the renders.

## A10. Open items (none blocks the package)

1. **The Office's 1/255 residual under Today.** 223 px at exactly 1/255 (226 in r2a), the same pixels in Tour and Descent, on shadow edges of the desks, the cubicle panels and the floor (0.011 % of the frame, none over 1/255). It is deterministic (two Build A runs agree there to the pixel), so it is not noise; it is not the door (the lab: object for object identical), not the trims (their values under Today are bit-equal to main's), not the shader (Tour's S1 had main's shader, Descent's the new one, and both show the same 223 px). One title run with main's code put back in C would isolate it. Every other title frame under Today sits inside the noise floor.
2. **Renders for Red:** done, §A11 (six sheets, seven films; flash check: no door above 3.0, Tour door 4 rises from 0.5 to 2.0).
3. **Phones and WebGL:** the phone tier is rendered in §A11 (Handheld quality, small kits at LOD1); this item's text below is Build A's. The kits switch at their sidecars' distances and the masks cap at 1024 on iOS and WebGL; the Renders stage's phone tier (iPhone, iPad) checks Office, Run! and back-of-house at 1:1. WebGL players keep today's door (`Today` is always true there); not exercised here, nor GlassRT past the title (the new lites' glass is today's `Glass_Wired` through `FrontRoomsDoorLite`).
4. **Asks for Red** (this stage's open_for_red): the back-of-house paint (built: warm grey, sRGB about (140, 135, 124); the alternatives cool machine grey (128, 133, 135) and institutional green-grey (124, 133, 118): 20_design.md §A4.4); the start-door rule (S1 built; S1b keeps the previewed family room and makes the room after it the airlock; S1c lets the zone door be the start door, against D-3: 20_design.md §A3.4).
5. **Sound** (声音设计 paused): the back-of-house and Run! leaves are steel but keep today's wooden StreamOpen / StreamClose. Contract `W/zone_doors/contracts/sound.md`: (a) keep it, or (b) a steel DA take set chosen by the door's look; the hook for (b) is built and inert (`DoorZoneOfHinge`, `SoundOf`).
6. **The pool (D-5):** Build B. Until then `ZoneBuilt(Pool)` is false: the pool family's doors wear Level 0 and its band keeps the 1.27 stop.
7. **Records to update at landing** (not this stage's: nothing under main is edited here): `research/door_inventory/10_inventory.md` (the title pair's Office, night post, back-of-house and Run! faces and station D's +Z face become built, with the kits' names and tris), the visual handoff, LANDINGS. (Package stage: the inventory's rows are updated in the staged copy that lands with this file, §A13.)
8. **D-2** (the Map stage): the back-of-house map member reuses `Door_BackOfHouse`, `Kit_DoorLite_F1_Steel` and `Kit_DoorLouvre_F12` from this package (one back-of-house language).
9. **Phones: the Handheld renderer's SSAO** (Fix A, §A12 item 4): `FrontRooms_URP_Handheld_Renderer`'s SSAO (After Opaque, Source Depth; 设置菜单与画质预设's file, tech_art rows 13 / 13b, Red "全部按推荐" 11-04) turns the back-of-house louvre black on phones and leaves a thin dark line at the lite frames' foot. Contract `W/zone_doors/contracts/settings_handheld_ssao.md` (to 设置菜单 via 总控; Red's pick): A keep, B the desktop mode (parity, loses the prepass saving), C keep the mode with radius 0.12 m / intensity 0.8 (REC: standing view = desktop to 0.07 stop, crouched 1.2 stops darker).
10. **Residuals after Fix A** (none is a gap): (a) at 0.35 m the guards' bend still sparkles (specular aliasing of `Station_StainlessGuard`, metallic 1, smoothness 0.68, on a 4.5 mm bend; an 8-step bend and grain UVs did not change it: speckle 353 px before, 381 after in np_heroA; sub-pixel from 1 m; the material is Red's E1 / E2 stainless, so not changed here); (b) at the crouched 0.35 m view a 1-px dotted line under the louvre lips at the eye's own height (the paint's normal map in a pixel-wide crevice seen edge-on; it stays with SSAO off and with the modelled hem; standing views look down on the blades); (c) the 630 barrels read as today's: `Prop_Stainless630` (0.64, 0.63, 0.60; metallic 0.9, smoothness 0.6, the brushed normal map) is all but today's runtime "Door / brushed steel" (the same colour, metallic 0.9, smoothness 0.62), so the 630 zones' hinge change is ≤ 2/255 at the night post's hero; the Office's satin chrome is the visible one.

## A11. Renders for Red (the Renders A stage, 2026-11-06 12:3x-13:2x; every NEW image and film re-shot by Fix A, §A12)

All in C (clone-only harness), nothing in main, no download, silent (C has `m_DisableAudio` 1; the films carry no audio stream). Every sheet is Shot format with `ALL LABELS FIT`; every film is 1920 x 1080, 30 fps, H.264 BT.709, silent, with the FRONTROOMSSS tone mark (`W/tools/recording_mark/recording_mark.py --mode tone`), about 3 Mbps.

| File (this folder) | What Red sees |
|---|---|
| `sheets/double_doors_renders_overview.jpg` (3200 x 2948, WAIT-RED: the paint, the phone SSAO) | one row per look (Office, station D, night post, back-of-house, Run!): TODAY, NEW (REC), the 0.35 m hero, the cost |
| `sheets/double_doors_renders_office.jpg` | O-1 against today: the preview at eye 1.62 m and 3 m, the phone tier, 3/4 at 3 m, the lite's top (F1 ends at 1.905 m, F4b runs on), the lite's foot and plate, the hinge jamb, the phone hero, lights off, both doors seen from inside the Office, cost |
| `sheets/double_doors_renders_station.jpg` | S-1: the station face at 3 m, phone, 3/4, the guard at 0.35 m, the casing's foot, phone hero, lights off (the map's dip), the player's own view turned round 5.5 m past the door, the stream face (unchanged), cost |
| `sheets/double_doors_renders_nightpost.jpg` | S-2: preview, phone, 3/4, both doors from inside the night post (guards on that face), the guard at 0.35 m, the jamb on the Level 0 side, phone hero, lights off, cost |
| `sheets/double_doors_renders_boh.jpg` (WAIT-RED) | B-2: preview from the Run! room, phone, 3/4 from the LOW side, the louvre and the F1 lite at 0.35 m, the louvre from 1.2 m standing, the jamb, phone hero, lights off, both faces inside LOW (its exit door wears S-2), cost; the paint row B-2a warm grey REC / B-2b cool grey / B-2c green-grey / Build A's value (the A8 #11 bug) |
| `sheets/double_doors_renders_run.jpg` | C-1: preview, phone, 3/4, the F5 lite at 0.35 m, the jamb, the meeting stile and cart band, phone hero, lights off, both doors from inside Run!, cost |
| `video/double_doors_tour_new.mp4` (95.4 s, 36 MB) | the title Tour in TF-1 order with Part A's doors (doors 1-6, 9); the pool's doors 7-8 stay Level 0 until Build B |
| `video/double_doors_descent_low.mp4` (12 s) | Descent 30-42 s: through the Run! room, the back-of-house pair opens into LOW |
| `video/double_doors_swing_{office,run,nightpost,low}.mp4` (7 s each) | TODAY \| NEW side by side (the central 960 px of each frame), 3 s before the latch to 4 s after: the swing and the walk through |
| `video/double_doors_swing_station.mp4` (7.6 s) | TODAY \| NEW: the walk through the start door, then turning round 5.5 m past it: the leaves shut on the station face |

### A11.1 How

- **The harness** (`Assets/Editor/TitleStream/FrontRoomsTitleCapture.cs` in C, v3 by `W/zone_doors/tools/renders_patch_harness.py`; copy in `W/zone_doors/tools/harness/`): pose sets in a door's own space, fired when the title camera crosses a point (or at the back turn after Space), rendered by a second camera made from the game camera (`CopyFrom`, `FrontRoomsPostStack.ConfigureCamera`: post, MSAA, GlassRT; its URP data copied), three renders a pose. Checked against the game's own frame at the game camera's place: 2-3/255 at most in Tour and the start area; in the Descent Run! room isolated sparkles and the crash rail's highlight (21-41/255), none on the door. The door's slot is drawn for each pose (door culling hides the room behind the title camera, its far door included). `doors.tsv` logs every swing.
- **TODAY** is the same run with `-doorsToday on`: game time is 60 Hz, so TODAY and NEW line up frame for frame and pose for pose; the pose pairs differ only inside the door's outline (Tour: every eye view's difference over 2/255 lies in x 651-1268, y 206-848; the night post's inside view adds 13 px of 3-4/255 on the floor at the far left).
- **Rows:** eye 1.62 m at 3 m; the phone tier; 3/4 at 3 m (40 deg; the back-of-house one from the LOW side, since the Run! corridor is 2.8 m wide); heroes square on at 0.35 m (Red 10-30: close-ups face the target) and the jambs oblique; lights off = every lamp of the camera's room out by the stream's own lamp path (`ApplyLampLight` / `ApplyFixtureVisual`, applied and restored inside one frame), ambient and reflections as the game has them; at station D, the map's own dark: the Relay's first-dark dip (`MapWorld.SetLampOverride` Dip to 0 on the 81 cells round the door) and any light left within 12 m off; inside views of both doors of each family room.
- **Desktop** = quality Ultra (the Standalone default). **Phone tier** = quality Handheld + `FrontRoomsHandheld.Force(IPhonePro)` + the troffer phone lens, with the plates, guards, lite kits and louvres forced to LOD1 (`ReducedTier` is compiled for UNITY_IOS only), the frame kits by distance. The Handheld level excludes Standalone, so for those six runs C's `QualitySettings.asset` let the Editor use it (clone only; main's file put back after, edaed722); textures stay at their Standalone import (the iOS 1024 caps are build-target imports). The title lockup (logo) is not drawn.
- **Runs** (`W/zone_doors/logs/ra_runs.txt`): tour, descent, start x today / new at Ultra, the same six at the phone tier, two extra (the Office lite's top): every run 0 `error CS`; the one exception in each is Unity's QuickSearch indexer at start-up, as in every run of A6.4.

### A11.2 What the player sees, per look

- **O-1 Office:** from the Level 0 room the Office pair now shows ahead in a dark-bronze pressed-steel frame; the leaves stay Red's walnut G; the lites are the narrower 10 x 30 in F1 in the walnut bead, ending at 1.905 m (today's F4b runs on to 2.36 m); satin-chrome hinges; both doors of the Office match. In the yellow Level 0 room the bronze band reads almost black. (Fix A: the satin chrome now reaches every barrel and the jamb flanges; the hero rows are shown at display +2 EV, where the bead and the subtle office wear read.)
- **S-1 station D:** only the station face changes (the 38 mm pressed-steel casing, two stainless guards, 630 barrels); the stream face stays Level 0. From 3 m the casing reads close to today's walnut one; the guards show from about 1 m in. The face shows only when the body-door leaves have shut, that is when the player is 4-5 m past the door (at 3 m they still stand open). (Fix A: the bronze cap over the lining's end now stands 1 mm proud of the map's south liner, so no pale strip of wall paint runs up the jambs; the +Z barrels are 630 now.)
- **S-2 night post:** today's walnut leaves in the bronze frame, stainless guards on the night post's face only, so the preview from Level 0 changes only by the frame. (Fix A: 630 hinges on every barrel; the guards bend in 8 steps.)
- **B-2 back-of-house:** warm-grey hollow-metal leaves (after the A8 #11 fix), an F1 pressed-steel lite with its screws, the F12 louvre over the 10 in kick plate (its blades and lips read as lines from standing height); inside LOW the exit door wears S-2 (the room it opens into). The lit room beyond the blades is not in these frames: the title stream lights a room only when its door opens. (Fix A: the lite and louvre frames end in a feathered edge, no dark outline; the chips are grey-primer flakes, no longer round dark drops; on phones the louvre is black under the Handheld SSAO: A10 #9.)
- **C-1 Run!:** almond hollow-metal leaves with the 15 x 15 in F5 lites and the cart band; both doors of the Run! room match; from the Level 0 room it is the brightest door face. (Fix A: the F5 frame's near-black foot line is gone; the cart band's chips are grey-primer flakes.)

### A11.3 Photosafety (`Tools/lookdev/flash_check.py`: its own functions on 30 x 18 block means of 640 x 360 decodes; area rule, limit 3 flashes in any 1 s)

| Film | TODAY | NEW | Note |
|---|---|---|---|
| Tour, whole film (frames) | 2.5 (block 4.5) | 2.5 (block 4.5) | the worst seconds are the walks through door 1 (15.1 s) and door 8 (88.1 s) in both |
| Tour door 1 Office (latch -1.5 s to +4.5 s) | 2.5 | 2.5 | |
| Tour doors 0 / 2 / 3 / 5 / 6 / 7 / 8 | 0.5 / 0 / 1.5 / 1.0 / 1.0 / 1.5 / 2.5 | the same | |
| Tour door 4 Run! (the goal pair) | 0.5 | **2.0** | the almond leaves; under the limit by 1.0 |
| Descent door 3 back-of-house | 0.0 | 0.0 | |
| Red flashes, every film | 0.0 | 0.0 | |
| The deliverables (the CLI on each final MP4, Fix A re-encode) | | tour 2.5, descent clip 0.0, swings office 1.5, run 1.5, night post 0.5, low 0.0, station 2.5 (the final file; other encodes of the same frames read 2.5-3.0, at most the limit: the TODAY and NEW halves turn round together at 6 s; on the frames the NEW half alone is 2.0, TODAY 1.5, side by side 2.5): all PASS | Renders A's row (office 2.5, run 2.0, night post 1.0) did not reproduce (Review A); these are the CLI's own outputs, `W/zone_doors/logs/fa_flash_cli.txt` |

No door raises any second above 3.0, and the film's worst second is unchanged. A first cut of the Descent clip (28-42 s) failed the CLI at 3.5 after H.264 although its frames give 2.5 in TODAY and NEW alike: the window is the Run! room's own light at 28.8-28.9 s, four seconds before the back-of-house door opens; the clip now starts at 30 s (0.0). A finding for any film that starts in the title's Run! room.

### A11.4 Cost in the title (the game camera's frame, 12-frame medians, NEW against TODAY)

| View (the title camera 7 m short of the door) | Draws | Tris |
|---|---|---|
| Office preview (Tour c1) / inside (c2) | -4 / -2 | +1,728 / +3,516 (Fix A; Build A +2,972: c2 sees the Run! door's conforming slabs) |
| Run! preview (c3) / inside (c4) | +4 / +4 | +2,240 / +6,544 (Fix A; Build A +1,152 / +4,912) |
| Night post preview (c5) / inside (c6) | +4 / 0 | +1,856 / +3,560 |
| Back-of-house from Run! (Descent c3) / inside LOW (c4) | +16 / +4 | +10,432 / +1,856 (Fix A; Build A +8,032 / +1,856) |
| Station D | not comparable: the map's seed changes per run (the lab census stands: +270 tris, +3 renderers a builder) | |

Frame tris count every pass (the shadowed lights count a pair again), so they run above the pair's own LOD0 delta (§A6.5).

### A11.5 Limits

The iPad is the same tier (its 4:3 frame not shot); WebGL is not rendered (`Today` is forced there: today's door); the pool's doors stay Level 0 until Build B; the 3/4 views use the game's 76 deg lens (wide). Disk: `W/zone_doors/captures/ra` 3.7 GB (2.5 GB of film frames, 0.8 GB of intermediate encodes) and `ra_smoke` 0.1 GB: candidates for Red to delete once the films are seen; Fix A adds `W/zone_doors/captures/fa` (the NEW runs again, 2.0 GB) and `captures/fixa_diag` (small), the same kind of candidates.

## A12. Fix A: Review A's FIX list (2026-11-06 13:59-15:2x)

The stage that applied the independent critic's FIX list for track A (journal `W/zone_doors/journal.txt`, Review A FINDING 1-6 and its verdict). All in C (`W/proj_doorlean`), nothing in main, no git, no download, silent; pre-fix copies of every touched file in `W/zone_doors/clone_only_save/pre_fixA_20261106/`. Where the critic's remedy rested on a cause the measurements did not bear out, the cause was fixed instead and the remedy not done (items 3 and 4).

| # | Finding (Review A) | Cause, measured | What changed | After |
|---|---|---|---|---|
| 1 | The zone hinges never change material (the Office's satin chrome, every 630 hinge, station D's +Z barrels) | `Gather` looked the parts up with `Transform.Find`, which reads the '/' in "door hinges / jamb halves" and "left\|right door hinge barrels / swing side" as a path: both null, so only the leaf flanges took the zone finish | `ChildNamed` (exact child name) for every part `Gather` reads; the names stay (声音设计 reads them); Level0 and Today still restore today's hardware | the probe at station D: the +Z barrels are `Prop_Stainless630` (were "Door / brushed steel"); the Office barrels satin chrome (up to 17/255 against Build A on them); the 630 barrels change ≤ 2/255 because the 630 material is all but today's stand-in (A10 #10c) |
| 2 | Station D: a pale 12 mm strip of wall paint up both jambs, the 0.5 mm bronze cap not seen | measured in the game (clone-only harness flag `probe` at the back turn): the map's `Station set/South liner/South liner west|east` (Station_Wall) spans door space x ≤ −1.200 / ≥ 1.200, z 0.020-0.024, in front of the cap (z 0.02225-0.02275); after `ClearStartArea` the static entrance stands on D's data wall (face at 0.020), where the cap showed | ours only (no map file, no map object touched): `Kit_DoorCasing_StationPair`'s cap fills the lining's end to z 0.025 (1 mm in front of the liner), its inner face at x 1.1995 (0.5 mm into the opening: no plane shared with the liner's edge), thin (0.5 mm) only in a 5.5 mm notch round each barrel (asserted clear shut and in a −Z swing to 10°), closed boxes on shared breakpoints; `Kit_DoorGuard_Casing38`'s wing ends 0.05 mm into the cap's face | sd_heroA x 838-863 mean sRGB (86, 87, 66) today, (76, 76, 56) Build A, **(43, 38, 21)** now (bronze); builder 2 checked in the door lab (static entrance on the data wall): cap seated, no slit; LOD0 516 tris |
| 3 | Seams at the steel kits: a near-black 1 mm line along the F5 frame's foot, dotted seams round the louvre frame, dashes along the F1 / F5 sides at 3 m, a dotted bright line on the night post guard | (a) the kits' 45° outer chamfer over a 0.65 mm square step faced away from the ceiling lamps on the bottom rail (the "crack" ran exactly along the chamfer, x 61-897 = the frame's outer corners, not 15 mm past them) and caught acne and coverage dots elsewhere; (b) the steel slab had T-junctions and open edges: a Python port of `SlabBuilder` counts 24 / 40 T-junctions and 48 / 72 edges not shared by two faces (Run! / back-of-house); (c) the guard's line is specular aliasing on its 4.5 mm bend | (a) `door_pair_common.steel_section`: the flange ends in a feathered slope (16-22° off the face) from the flat at 11.5 mm into the face at 19 mm, at LOD1 too; LOD2 rings slope into the face; screws at 7.5 mm on the flat; (b) `SlabBuilder.Slab` a conforming grid: 0 T-junctions, every edge in two faces (224 / 308 tris a leaf); (c) guards: 8-step bend, 0.6 mm ease in 2 segments, the #4 grain run along the length by their own UVs; the louvre's lips end in a modelled hem | run_heroA rows 787-790: min-of-row means 9-10 → **58-70** (a soft band, the lamp-shaded bevel); the louvre frame's outline continuous at 0.35 m and 1.2 m; the 3 m dashes gone (run_eye, boh_eye); (c) unchanged: A10 #10a |
| 4 | Phone tier: the louvre a black see-through grille, the F5 frame gone | not LOD1 and not shadows (diag runs `W/zone_doors/captures/fixa_diag/`, harness flags `nossao`, `noshadow`): louvre linear Y at the hero, phone LOD1 0.0176-0.0179, phone LOD0 0.0176, phone receiving no shadow 0.0176, phone SSAO off 0.1683, desktop 0.0736-0.0740; the Handheld renderer's SSAO (After Opaque, Source Depth) multiplies the lit image in the 41 mm recess | the critic's remedies (a closed two-faced LOD1, no shadow receiving) would change nothing: **not done**. Ours: LOD1 frames keep the feathered edge (the outline reads on phones). The renderer is 设置菜单's: contract `W/zone_doors/contracts/settings_handheld_ssao.md` (A10 #9); the sheets show options B and C (flags `ssaodesk`, `ssaosoft`) | phone, SSAO in the desktop mode: 0.0741 / 0.0750 (= desktop to the 4th decimal); option C (radius 0.12, intensity 0.8): 0.0323 crouched, **0.0785** standing (desktop 0.0748) |
| 5 | The service and cart chips read as round dark red-brown drops (blood) | soft round blobs, and the chip colour stored as if linear in an sRGB Color slot (A8 #11's bug again): (0.27, 0.181, 0.117) rendered as linear (0.059, 0.027, 0.013) | `gen_door_wear.py` `flakes()`: angular 5-8 sided flakes, wider than tall, in clusters, 3 x 3 supersampled; steel leaves' chip = grey shop primer, linear (0.150, 0.150, 0.145) stored sRGB-encoded (0.4236, 0.4236, 0.4169); the Office's pale walnut stored (0.6872, 0.5469, 0.3959) | flakes A > 0.5: back-of-house 674 texels, Run! 603, Office 200 (round blobs 500 / 738 / 299); renders: grey flakes and rubber streaks over the kick plates |
| 6 | Heroes too dark to read; the office wear invisible (0 px over 16/255) | the game's light at the door; the office mask's halo and the chip colour too weak (and the bug of item 5) | office halo / knuckle / edge grime about 60 % stronger; hero rows on every sheet shown at display +2 EV (both columns alike, a soft shoulder; the eye, 3 m and dark rows stay at the game's exposure) | office_heroA against today: 2,645 px over 8/255, **365 over 16/255** (Build A 329 / 0) |
| 7 | Doc and sheet text | captions claimed 630 / chrome barrels and a cap the frames did not show; A11.3's deliverable flash row did not reproduce | captions corrected (the claims are true now), sheets rebuilt (ALL LABELS FIT), A1-A11 notes, A11.3 from the CLI | this section |

**Re-renders** (same frames, NEW runs only: every fix runs only with the zone looks, so TODAY is unchanged and stays `W/zone_doors/captures/ra/*_today*`): desktop tour_new, descent_new, start_new (poses + films) and phone tour_new_phone, descent_new_phone, start_new_phone, plus the TODAY / NEW option rows `*_phone_x`, `*_phone_s`, in `W/zone_doors/captures/fa/`; three passes (the louvre hem and the guards' UVs came after the first look), 22 Unity runs in all with the diagnostics and the lab, every one 0 `error CS` (the one exception in each is Unity's QuickSearch indexer at start-up). Sheets `sheets/double_doors_renders_{overview,office,station,nightpost,boh,run}.jpg` rebuilt (ALL LABELS FIT; boh and run WAIT-RED: the paint, the phone SSAO); films `video/*.mp4` re-encoded (1080p30, H.264, no audio stream, tone mark), CLI flash PASS: tour 2.5 (= today), swings office 1.5, run 1.5, night post 0.5, low 0.0, station 2.5 (encodes of the same frames read 2.5-3.0, the comparison film's two halves turning together; the NEW half alone 2.0 on frames), descent clip 0.0; frames: Tour door 4 (Run!) 2.0 against today's 0.5, every other door as today.

**Files changed** (sha1s in `W/zone_doors/tools/buildA_payload.sha1`, 67 files, 28 of them changed by Fix A): `FrontRoomsDoubleDoorKit.Zones.cs` (28bc4ed6); `Kit_DoorCasing_StationPair`, `Kit_DoorGuard_Casing38`, `_51`, `Kit_DoorLite_F1_Steel`, `_F5_Steel`, `Kit_DoorLouvre_F12` (.fbx; .json where the counts or meta changed; the .metas and GUIDs unchanged); the six `_L` / `_R` leaf materials; the three wear masks; seven Blender / lookdev modules. The other five C# files and the shader are Build A's (unchanged). Clone-only, never packaged: the harness flags (`probe`, `nossao`, `noshadow`, `ssaodesk`, `ssaosoft`; copy in `W/zone_doors/tools/harness/`).

**Gate:** Roslyn on main's tree with the six C# files of the package (zsh array; the .rsp lists the 6 C paths): macOS COMPILE OK 0 errors, iOS COMPILE OK 0 errors (`W/zone_doors/_roslyn/fixA_mac`, `fixA_ios`); Unity compile in C 0 `error CS` in all 22 runs; apply package `W/apply/zone_double_doors_a` (mkapply from the payload: 67 paths, 61 new, 6 metas kept) **DRY RUN OK** against main (every base matches: b7ab5af0, 6893ef1a, 8100738c, 5133251e, 759a22a9, 87a758e1). The shader is Build A's (its variant gate stands, §A7).

## A13. The package (the Package A stage, 2026-11-06 15:16-15:4x)

Nothing in main, no git, no download, no Unity run in this stage (the payload is Fix A's, compiled in C in 22 runs), silent.

**`W/apply/zone_double_doors`** (apply convention v2; made by `W/tools/mkapply.py` from `W/zone_doors/pkg_overlay_a`, which equals C's 67 files and `W/zone_doors/tools/buildA_payload.sha1`): `zone_double_doors.sh` (no argument = dry run, `--apply` writes), `_bases.txt` (67 rows: 61 ABSENT, 6 at main's sha1), `_expected.txt` (67), `_keep.txt` (the 6 `.meta`s of the replaced files, GUIDs kept), `_payload/`, `_base/` (the 6 exact bases), `.diff`. It supersedes `W/apply/zone_double_doors_a` (Fix A's dry run of the same payload; never apply both).

| Path group | Main base | Count |
|---|---|---|
| `FrontRoomsDoubleDoorKit.cs`, `FrontRoomsRoomStream.cs`, `FrontRoomsRoomStream.TitleRooms.cs`, `Title/FrontRoomsTitleVignettes.cs`, `FrontRoomsStationDress.cs`, `FrontRoomsSurface.shader` | b7ab5af0, 6893ef1a, 8100738c, 5133251e, 759a22a9, 87a758e1 (unchanged in main at 15:2x; their `.meta`s kept) | 6 replaced |
| `FrontRoomsDoubleDoorKit.Zones.cs` + `.meta` (guid fdb02e20...) | ABSENT | 2 new |
| 7 kits (`.fbx`, `.json` + their `.meta`s), 7 materials + `.meta`s, 3 wear masks + `.meta`s | ABSENT | 48 new (24 new GUIDs; 25 with `Zones.cs.meta`, none used in main's Assets) |
| 10 Blender modules, `Tools/lookdev/gen_door_wear.py` | ABSENT | 11 new |

- **Write order** (`_expected.txt`, edited after mkapply; the set is unchanged, mkapply's order kept in `W/zone_doors/tools/zone_double_doors_expected.mkapply_order.txt`): the `.meta`s, then the shader, then **each kit's `.json` before its `.fbx`** (`FrontRoomsKitImporter` reads the sidecar while it imports the model, for the material remaps and the LOD heights, and declares no dependency on it: a model imported before its sidecar would keep wrong LODs until a reimport), the materials and masks, the tools, the C# last. The importer itself is unchanged, so there is no import race with Red's open Editor (handoff §6).
- **Main API read** (two rows in the script's step 5, the map line's, read only): `public static event Action<FrontRoomsMapWorld, FrontRoomsMapHunter> MapRunStarted;` in `FrontRooms3DGame.cs` and `public bool HasStation` in `FrontRoomsMapWorld.cs` (the D-3 hook). The dry run refuses if either moves.
- **Checks at 15:2x:** no open CLAIM on these paths in LANDINGS (its last row 12:52, before Fix A's gate); every text payload LF (mkapply refuses a CR); no Editor script in the payload (the harnesses are clone only: `Assets/Editor/ZoneDoors/*`, `Assets/Editor/TitleStream/*`, `Assets/Scripts/CloneOnly/*` are never packaged).
- **Dry run:** `bash W/apply/zone_double_doors.sh` against main: all 67 bases match, 6 kept `.meta`s unchanged, 67 payload files match, 25 new GUIDs unused, both API rows found: **DRY RUN OK**.
- **Rehearsal:** a scratch copy of main (its 3,903 Assets `.meta`s, none an iCloud placeholder; the 6 bases; ProjectVersion; the 2 API files): DRY RUN OK, `--apply` **MANIFEST OK** (67 paths, 6 kept `.meta`s unchanged), again **ALREADY APPLIED**.
- **Gate (rule 25):** Roslyn on main's tree with the package's own 6 C# payload files (`W/zone_doors/tools/pkg_gate.zsh`, a zsh array; both `.rsp` files list the 6 payload paths; the iOS one carries `UNITY_IOS` and `PLATFORM_IOS`): macOS **COMPILE OK, 0 errors** (Assembly-CSharp 280 files, 48 warnings, main's count; Editor 89 files), iOS **COMPILE OK, 0 errors** (`W/zone_doors/_roslyn/pkgA_mac`, `pkgA_ios`; log `W/zone_doors/logs/pkgA_gate.txt`). Unity: 0 `error CS` in Fix A's 22 runs on these files; the shader is the one the variant gate passed (a5302188, §A7).
- **What goes live at landing:** the title's zone looks at once (`FrontRoomsDoubleDoorKit.Today = false` is the default; `Today = true` is the way back, one line, and it is forced on WebGL players); station D's +Z face on both builders; Level 0 doors byte for byte. The map, the input code and every sound file are untouched.
- **Docs staged with it** (`W/zone_doors/to_main/Frontrooms3D/Documentation/research/`): `double_doors/10_research.md`, `20_design.md`, `30_build.md`, `SOURCES.md`, `sheets/` (15 sheets + 15 `_fit.txt`, every one ALL LABELS FIT), `video/` (7 films, 1080p30, no audio stream); `door_inventory/10_inventory.md` (rows updated in place, rule 20). No `images/`: nothing was downloaded, and our drawings are on the design sheets.
- **Contracts** (`W/zone_doors/contracts/`): `sound.md` for Red (声音设计 is paused: (a) keep the wooden takes, (b) a steel DA take set chosen by the look; the hook is built and inert); `settings_handheld_ssao.md` for 设置菜单 via 总控, with Red's pick (A keep, B the desktop mode, C softened REC). Nothing for 关卡设计 or 交互设计: no map or input file changes, and pivots, colliders and names stay.
- **Combined-test rows and the VL proposal:** `W/vl_pending/zone_double_doors.md`.
- **Still open after landing** (§A10): Red's asks (the back-of-house paint, the start-door rule S1 / S1b / S1c, the sound (a) / (b), the phone SSAO A / B / C); the Office's 223 px at 1/255 under `Today`; the guards' sparkle at 0.35 m; the 1-px line under the louvre lips at the crouched hero; the 630 barrels all but today's. The pool (Part B) builds on this API.

---

# Part B · The pool and dark pool doors (D-5)

- **Stage:** Build B, workflow visual-zone-doors-refine (wf_afdae4db-b34, parent session local_1fc9f09c, account G), 2026-11-06 15:17 PST on. Built from `20_design.md` Part B on Part A's API (landed in main at 15:34 as `W/apply/zone_double_doors`, §A13).
- **Status:** BUILT IN THE CLONE `W/proj_doorlean`, nothing landed in main, no git, no download, silent (C has `m_DisableAudio` 1; every capture is frames only). Gate: Roslyn macOS + iOS 0 errors, Unity compile in C 0 `error CS` (§B10). The Package stage builds `zone_double_doors_pool` from §B11. **Renders B** (§B13, 16:33-17:3x): the pool sheet, the overview with the pool rows, the Tour and Descent films with every new door, a swing film for the Pool and the Dark pool; the photosafety bound measured (§B13.4: a decision for Red). **Fix B** (§B14, 18:14 on, Review B's list): the A1 ring's foot feathered on every LOD and kept at LOD0 within 2 m on phones, the frame one step deeper aqua (F1) for the dark pool's goal, the saddle's bevels in the frame's gel coat (the light line under door 6 removed), PD-B's dark read captioned, the porthole face kept as an honest flaw for Red (no glass value removes it); every PD-A / PD-C image and film re-shot; W/apply/zone_double_doors_pool built, DRY RUN OK. **Package B** (§B15, 19:22 on): the package checked again on main as it stands (DRY RUN OK), rehearsed on a scratch copy of main (MANIFEST OK, then ALREADY APPLIED), Roslyn macOS + iOS 0 errors with the payload's 2 C# files; the docs and the inventory rows staged. At landing nothing is visible: the pool doors stay today's walnut pair until `PoolDoorsLive` is set.
- **Red's decisions built here** (journal PARENT NOTEs): 21:15:15 "按默认做" (D-5: research, three options + TODAY, **the recommended one built**, Red may still pick any of the three from the sheet); 21:25:37 "这里改成选项 2：整扇门都换成区域的门。* 玩家在 Level 0 房间里走向下一扇门时，会先看到办公室的门，算是一点预告。" (D-1: the whole door, both faces; the preview from the Level 0 room before).
- **What is built:** PD-A (REC), the moulded FRP pair, as the title Pool and Dark pool doors (and the Club's, when it is built). PD-B (clear-anodised aluminium) and PD-C (aqua epoxy steel) are drawn for the sheet on the parts that exist, switched by `FrontRoomsDoubleDoorKit.PoolDoorFinish` (20_design §B4.4); their own kits are built only if Red picks one.
- **Marks** as Part A: door space x across, y up, z through, +Z the stream's swing face; tris LOD0 / LOD1 / LOD2 from the kits' sidecars; "C" = `W/proj_doorlean`.

## B1. Kits (Blender 4.3 silent, `Tools/Blender/frontrooms_kit/build_asset.py`, hand-built LODs `fr_lods`)

Both new, on Part A's shared module (`door_pair_common.py`: the pair's door space, the F4 envelope, the barrels' clearances, read only). Every module asserts its envelope; the build stops on a miss. Log `W/zone_doors/logs/blender_kits_b.log`.

| Kit | Module | LOD0 / LOD1 / LOD2 tris | Switch (m, lodBias 1, vFOV 76) | Slots | What it is |
|---|---|---|---|---|---|
| `Kit_DoorFrame_FRPPair_DA` | `door_frame_frp_pair_da.py` | 1,550 / 444 / 196 | 4 / 12 / never | `Door_PoolGelcoat_Frame`, `Prop_Stainless630` | PD-A's one-piece moulded FRP frame: Part A's outer path (`door_frame_steel_pair_da.frame_outer`: soffit x 1.200, the 12 mm hinge step with its land at \|z\| 0.02225, the rise at x 1.212, the 50.8 mm face band at \|z\| 0.0331 to x 1.2628, the return to z 0.0195) in a 4 mm moulded wall; moulded arrises r 4 mm, the land's inside corner r 1.5 mm (the barrels keep their 2.5 mm); the face band's two outer head corners rounded r 12.7 mm in elevation by 8 / 4 / 2 corner rings (points outside x 1.2501 follow the round, the rest keep the square mitre: the inner corners stay square), no mitre and no joint line; the steel kit's saddle (12 flutes of 0.8 mm, the 12 mm screw land, 7 countersunk flat heads) in 630 stainless; since Fix B (§B14.5) a 630 fluted tread in a moulded FRP threshold: the saddle's two 1:2 bevels, their eased arrises and lips (faces more than 46 mm off z 0) are in the frame's gel coat slot (no new slot, no new draw). LOD1 2-segment arrises, the saddle as 3 facets, no screws; LOD2 sharp. Asserts: the envelope (1.2628 / 2.7228 / 0.0331, every level), nothing in the opening or where a barrel swings, nothing behind the feet past the rise, the head corner rounded |
| `Kit_DoorLite_A1_FRP` | `door_lite_a1_ring.py` | 2,560 / 768 / 192 (Fix B; Build B 2,048 / 512 / 192) | 2 / 8 / 40 | `Door_PoolGelcoat` | the A1 porthole's moulded glazing ring, both faces alike, no screws: a 64-segment lathe of the §B3.3 section (10 profile segments a face since Fix B): the lip bedded on the pane at 3.05 mm from the centre plane, the inner face to 14.0 mm, the crown 6.35 mm proud of the leaf face, then (Fix B, §B14.4) a concave flank and a feathered foot at 19 deg into the face: the foot 0.1 mm into the face at r 0.2251 (450.2 mm), the visible foot line at 449.6 mm. LOD1 32 x 6 (the foot at 19 deg); LOD2 24 x 2 (the crown, the foot at 23 deg). Phones keep LOD0 within 2 m (the ring kits skip ReducedTier). Asserts: the clear aperture never narrowed (r 0.2032: 201.1 sq in, bottom 47.0 in), nothing in the leaf's body outside the cut-out, 233 mm to the push plate (>= 230), over 0.5 m above the kick plate, every level's last facet at most 24 deg off the face and seated. Sidecar `liteForm` A1, `liteShape` round, `liteRect`, `cutRect`, the radii |

FBX + JSON in `Assets/Resources/Props/Models/` (their `.meta`s made by Unity at the first import, fresh GUIDs). A mesh check (Blender, welded): the frame is watertight but at its floor ends, the ring open only at its lip and foot, no non-manifold edge, no degenerate face. The lab census of a whole PD-A pair is in §B8.

## B2. Materials and textures

| Asset | Recipe | Used by |
|---|---|---|
| `Textures/DoorGelcoat_A`, `_N`, `_S.png` | `Tools/lookdev/gen_door_gelcoat.py` (new; numpy, fixed seeds, deterministic): 1024² over a 0.6 m tile (0.59 mm a texel), periodic. `_A` near white, linear 0.985 x a ±0.75 % pigment mottle (sRGB mean 0.993); `_N` the orange peel (2-4 mm, 150-300 cycles a tile), a softer 6-12 mm flow-out swell and the chopped-strand print-through (2,600 straight bundles 25-50 mm long, 0.6-1.2 mm wide, blurred by the 0.5 mm skin), normal tilt mean 1.53 deg at full strength; `_S` R smoothness 0.78 ± 0.022 (the peel's crests a little glossier, the strands' print a little duller), G 1, B 0, A 1. Metas from `PaintedMetal_*` with fresh GUIDs (the importer sets them by suffix: `_A` sRGB, `_N` normal map, `_S` linear) | the four gel-coat materials |
| `Textures/DoorWear_Pool.png` | `Tools/lookdev/gen_door_wear.py` (Part A's generator, a `pool` preset and `--only`, so the three landed masks are not rewritten): the level preset of §B3.6, 1024 x 2048 RGBA linear: gloss-dull hand ovals round the push plate (G .45, strongest at the meeting edge) and a softer palm zone above it (G .22), a faint grey core (R .08), the chalky bloom low on the leaf in the 19 mm strips beside the kick plate and just above it (G .30, A .25, fading upward by 0.35 m), the calcium tide line along the bottom (A .70, its upper edge wavy), sparse dried splash spots 1-4 mm (A .40), no burnish; the plate rects clear. Meta from `DoorWear_BackOfHouse` (iOS / WebGL capped at 1024) with a fresh GUID | the leaves |
| `Door_PoolGelcoat.mat` | `Painted_Metal`'s recipe with the gel-coat set: `_BaseColor` sRGB (230, 230, 224) stored as is (a material Color is sRGB in this project; Unity linearises it: albedo Y 0.77 with the map), `_TileSize` 0.6, smoothness x 1 (0.78), `_BumpScale` 0.35, macro tone / dirt 0.06 / 0.05 (the level is pristine) | the ring kits; the map's PL leaf (Map stage) |
| `Door_PoolGelcoat_L`, `_R.mat` | the above, `_BumpScale` 0.6, plus `_FR_LEAFWEAR` with `DoorWear_Pool`, rect L (0, 0, 1/1.1955, 1/2.639) / R (1.1955, 0, −1/1.1955, 1/2.639) (Part A's), chip = the calcium's chalk white sRGB (236, 234, 226), stored sRGB | PD-A's slabs |
| `Door_PoolGelcoat_Frame.mat` | the gel-coat set one step deeper than the course aqua (Fix B, §B14.3: F1, `_BaseColor` (0.2588, 0.6196, 0.6392), sRGB 66, 158, 163; Build B had `Prop_PoolTile_Course`'s own (0.40, 0.76, 0.77), sRGB 102, 194, 196, albedo Y 0.45), smoothness x 1.026 (0.80), `_BumpScale` 0.6 | the FRP frame kit and its saddle bevels |
| `Glass_Tempered.mat`, `Glass_TemperedDark.mat` | `Glass_Wired` / `Glass_WiredDark` with `_FR_GLASS_WIRE` off (`m_ValidKeywords` empty, `_Wire` 0); everything else the same 6 mm float glass. No shader change: `FrontRoomsGlass` applies DARK outside the wire block | the pool's lites (all three options) |
| `Door_PoolAluminium.mat` | `Prop_Aluminium`'s maps, base (0.80, 0.81, 0.81), metallic 1, smoothness x 0.98 (≈ 0.55) | PD-B's swap render |
| `Door_PoolEpoxy.mat` | `Door_Enamel`'s recipe (`Painted_Metal`'s maps, its gloss) tinted to the course aqua: the maps' mean is linear (0.649, 0.611, 0.512), the tint that lands it on the aqua's linear (0.133, 0.540, 0.552) is (0.205, 0.883, 1.078), stored sRGB-encoded (0.495, 0.947, 1.034) | PD-C's swap render |

No shader changes: the leaves use Part A's `_FR_LEAFWEAR` keyword set (`_FR_LEAFWEAR _FR_MESH_UV`, already in the variant gate of §A7), the others `_FR_MESH_UV`; the tempered glass is the existing no-wire glass variant. The bump strengths are the look round's (§B9 #3): the generated relief at full strength read as crinkled film in the highlights at 0.35-0.4 m, and the ring's steep foot sparkled.

## B3. The code (two of 游戏视觉's files, RULES rule 3; no file of 关卡设计, 交互设计 or 声音设计)

| File | Base (main, since 15:34) | Change |
|---|---|---|
| `Assets/Scripts/Rendering/FrontRoomsDoubleDoorKit.Zones.cs` | 28bc4ed6 (Part A) | the pool's look (below) |
| `Assets/Scripts/Rendering/Doors/FrontRoomsDoorLite.cs` | 17f9bcb8 | `Glazing` and `Shape`, append-only (below) |

**`FrontRoomsDoubleDoorKit.Zones.cs`:**

- `enum PoolFinish : byte { Frp, Aluminium, Epoxy }` (append-only) and `static PoolFinish PoolDoorFinish = Frp`: set before a title is built, as `Today`; each pair keeps the finish its pool parts were built with. `ZoneBuilt(Pool)` is true, so `TrimStopHalfFor(Pool)` / `TopFor(Pool)` give Part A's envelope (1.2628 / 2.7228; `Today` 1.27 / 2.73): TitleVignettes' pool band now runs into the frame (its ends were already wired to it, §A3). `SoundOf(Pool)`: Composite for Frp, Steel for the other two (data for the sound contract; nothing reads it).
- `Prepare(pair, Pool)` → `EnsurePoolParts`: Frp spawns `Kit_DoorFrame_FRPPair_DA` under "double door frame" ("door frame frp (zone kit)") and one `Kit_DoorLite_A1_FRP` under each slab at the porthole's centre ("left|right door lite kit A1 frp"); Aluminium spawns Part A's `Kit_DoorFrame_SteelPair_DA` as an instance of its own with its slot swapped to `Door_PoolAluminium`, and Part A's `Kit_DoorLite_F1_Steel` per leaf, swapped; Epoxy the steel frame instance in `Door_PoolEpoxy` and PD-A's ring kits, swapped. All switched off, `ReducedTier` on the small kits; the pool needs no Part A frame (its own replaces it). A kit that cannot load marks the look failed: that door keeps today's look (as Part A).
- `Dress(pair, Pool, side)`: today's walnut lining / casing and oak saddle off, Part A's steel frame off, the pool frame on; the hinge renderers 630 (`Prop_Stainless630`, as Part A's zones); each slab's shared mesh and material (`Door_PoolGelcoat_L|_R`; PD-B `Door_PoolAluminium`, PD-C `Door_PoolEpoxy`); the rings (or PD-B's F1 kits) on; each glass object's mesh swapped and re-aimed: lite form 3 (the A1 disc, `Glazing.Tempered`, `Shape.Round`) or 4 (PD-B: a 6 mm F1 pane, Tempered, Rect). Dressing away from the pool turns every pool part off and re-aims the lite Wired, Rect. Pivots, names, colliders, the sound components and the swing are untouched; nothing is instantiated once prepared (Part A's rule).
- Meshes (cached in the kit's dictionary, shared by every pool door): `LeafMeshRound` (`RoundSlabBuilder`): the 1.1955 x 2.639 x 0.0445 m slab with moulded r 3 mm arrises (3 segments; the 8 corners spherical patches whose edges lie on the arrises' end arcs), the A1 cut-out (425.4 mm, 32 sides, at the porthole's centre: slab y +0.0595), a parting seam round all four edges at mid-thickness (0.5 mm wide, 0.2 mm deep; PD-C's swap uses the hollow-metal seam, 0.6 x 0.4 mm). Each face is a quad strip from the cut-out to a 0.52 m square of its radial projections (the cut-out's 32 vertices include the diagonals, so the square's corners are vertices of both), then four fans from the square's sides to the face's corners: the face's edges keep only their two corners, so the arris strips run whole along them and only the through-thickness corner strips split, at the seam. Conforming and watertight (Fix A's lesson); 544 tris a leaf; metre UVs from the slab's min corner on the faces (the wear mask's frame), by the dominant normal axis elsewhere. The glass: `GlassMeshFor(3)` = the 32-sided 423.4 x 6.0 mm disc (`MeshBuilder.Cylinder`, the cut-out's phase, 1 mm short of it all round, 128 tris); `GlassMeshFor(4)` = a 6.0 mm F1 pane.
- Names (none holds "floor", "carpet", "ceiling", "wall", "seal", "partition" or "threshold marker": `RefreshRoomMaterials`): `door frame frp (zone kit)`, `door frame steel (zone kit, pool aluminium|epoxy)`, `left|right door lite kit A1 frp`, `left|right door lite kit A1 (pool epoxy)`, `left|right door lite kit F1 (pool aluminium)`. The glass object keeps today's name ("lite glass (lobby)"), as Part A's zones do; its mesh is named for the porthole (the design's "lite glass (porthole)" would rename an object `Gather` finds by name).

**`FrontRoomsDoorLite.cs`** (20_design §B3.5; today's lites keep Wired and Rect, their materials, fade pool and sight test as before):

- `enum Glazing : byte { Wired, Tempered }`, `enum Shape : byte { Rect, Round }` (append-only); `LiteGlazing`, `LiteShape` on each lite; names `Glass_Tempered` / `Glass_TemperedDark`.
- `AttachStream(owner, glass, root, rect, dead)` now calls a new overload with `Glazing.Wired, Shape.Rect`; the overload sets the glazing (giving a borrowed fade material back to its own pool first when it changes) and the shape, then does what the old call did.
- `Apply` takes the lite's glazing's shared pair; `Borrow(glazing, must)`: the tempered glazing has its own pool (`FadePoolSize` 4, made from `Glass_Tempered`); `ReturnBorrowed` returns to the pool it came from. The tempered materials load only when a tempered lite exists (`EnsureTempered`), so a build without one loads what it loaded before. `LiveSharedOf(glazing)`, `DarkSharedOf(glazing)`.
- `CollectGlass` (map kits, the break body) also finds `Glass_Tempered` / `_TemperedDark` slots and sets the lite Tempered (for the Map stage's PL leaf); today's kits carry only `Glass_Wired`.
- `PassesPlayerSight`: a Round lite passes inside the rect's inscribed circle, inset 10 mm. `Census` adds "tempered pool n (free m)" only once a tempered fade material exists (today's string is unchanged).

## B4. The title stream: which doors wear the pool door

No stream or vignette file changes: Part A's rule (§A4) gives the pool's doors the pool look as soon as `ZoneBuilt(Pool)` is true, and `PrepareDoorZones` prepares it for any programme whose families hold Pool, DarkPool or Club.

| Programme | Door | Between | Look (side) | Build A | Build B (measured, `doors.tsv`) |
|---|---|---|---|---|---|
| Tour | 7 | L0 7 → POOL 8 | Pool (+1, the preview) | Level0 | Pool |
| Tour | 8 | POOL 8 → L0 9 | Pool (−1) | Level0 | Pool |
| Descent | 5 | NIGHT POST 5 → POOL 6 | Pool (+1) | NightPost (the night post's exit; the pool was not built) | Pool |
| Descent | 6 | POOL 6 → DARK POOL 7 | Pool (+1) | Level0 | Pool |
| Descent | 7 | DARK POOL 7 → WAKE 8 | Pool (−1) | Level0 | Pool |

Descent's door 5 changes from the night post's look to the pool's: Part A's rule (the room it opens into, when that family is built), not a new decision. Every other door of both programmes keeps Part A's look (§B6). Rule S1 (§A4) now also hands over one room later when Space is pressed in the Level 0 room before the pool (its far door wears a zone look): Part A's rule, unchanged.

## B5. PD-B and PD-C: the sheet's swap renders

`PoolDoorFinish` (the clone-only switch `-poolFinish Frp|Aluminium|Epoxy` in `FrontRoomsTitleProgrammeArgs`, and the door lab's pool bays) draws the two other options with parts that exist, as 20_design §B4.4 planned:

| Option | Leaf slab | Lite | Frame | Glass | What the render cannot show (written on its tiles) |
|---|---|---|---|---|---|
| PD-B clear-anodised aluminium | Part A's hollow-metal slab with the F1 cut-out only (`SteelSlab`, conforming), `Door_PoolAluminium` | Part A's `Kit_DoorLite_F1_Steel` in the aluminium | Part A's steel frame kit, an instance in the aluminium (its saddle too) | `Glass_Tempered` F1 pane, 6 mm | the lite kit's 6 screw heads (snap-in stops have none); pressed bends, no mitre line and no fasteners on the frame |
| PD-C aqua epoxy steel | the round leaf with the hollow-metal seam, `Door_PoolEpoxy` | PD-A's ring kit in the epoxy | Part A's steel frame kit, an instance in the epoxy | `Glass_Tempered` disc | the ring is PD-A's moulded crown, not the pressed steel ring with 8 screws |

Their own kits (`Kit_DoorFrame_AluPair_DA`, `Kit_DoorLite_A1_Steel`) are built only if Red picks one. In the title (bb1 `tour_alu`, `tour_epoxy`): PD-B reads as a dark satin-metal pair with two narrow slots (it mirrors the dark rooms), the band stopping at a silver frame; PD-C as an aqua door the colour of the band with two round windows.

## B6. Proofs (Unity session BB1, 2026-11-06 15:50-16:02; frames `W/zone_doors/captures/bb1/<run>/`, tables `W/zone_doors/buildB/`)

**How.** As §A6 and §A11.1: batch Play mode in C (`FrontRoomsTitleCapture` v3, clone only; OSXUniversal, audio off), 1920 x 1080, quality Ultra (the desktop default), game time at 60 Hz, the pose cameras made from the game camera. The pose file is Fix A's (`poses_fixa/tour.txt`, `descent.txt`) plus the pool's sets (`W/zone_doors/tools/poses_b/`: Tour P7 from Level 0 room 7, P7o with door 7 open, P8 inside the Pool; Descent Q5 from the night post, Q6 inside the lit Pool, Q7 inside the Dark pool); films 72-95.4 s (Tour) and 51-85 s (Descent) at 30 fps. Comparisons by `W/zone_doors/tools/bb_compare.py` (8-bit sRGB, the max over R, G, B; the title TV masked). Runs: the door lab (pool bays), the lab's Today proof, Tour and Descent with the zone looks and under `Today`, PD-B and PD-C in the Tour.

### B6.1 Today = true renders today

| What | Compared | Result |
|---|---|---|
| The door lab's Today proof (`-zdTodayProof`): the stream pair (Both, 60 objects) and the static entrance (Plus, 34 objects), each built with main's own calls, with Build B's (BuildPair, PlaceLeaf, Prepare for every zone, the pool's included, Dress Level 0, SetStationFace) and with main's again | dumps; and the 27 shots against Build A's S4 lab | main vs Build B **IDENTICAL**, main vs main IDENTICAL; the four dumps byte-identical to S4's; the 27 shots **0 px** apart from S4's |
| Tour's 27 Part A poses under Today | Renders A's `ra/tour_today` | 25 identical to the pixel; `np_dark` 4 px and `run_dark` 3 px at the frame's centre (max 24 / 6): the same pixels differ between the sessions with the zone looks on too (§B6.2), so they belong to the session, not to the doors |
| Tour's c-shots c1-c6 under Today | the same | identical, but c1's second title TV (its video frame, x 388-445, y 428-465) |
| Descent's 14 Part A poses and c3, c4 under Today | `ra/descent_today` | max 1/255 |
| Tour's film 72-95.4 s under Today | `ra/tour_today` frames 2160 on | the mean colour equal to 0.01 level in every frame; from 73.7 s a low noise over the whole frame (mean \|d\| 1.1, 99.9th percentile 6, the largest 32 px block mean 3.3, no door-shaped structure: `bb_filmblocks.py`). The pool poses' extra renders at 72.17 s (this session's, not Renders A's) shift a time-sliced state, most likely the title pool's probe capture; TODAY and NEW of this session carry it alike |

Under `Today` the pool code is inert (`Prepare` and `Dress` return before touching a pair, `ZoneBuilt` is not reached) and `FrontRoomsDoorLite`'s Wired path takes the same materials, pool and sight test as before; the lab's object-for-object dump is the proof of record, as in Part A.

### B6.2 Level 0 and Part A unchanged with the pool look on

| What | Compared | Result |
|---|---|---|
| Tour's 27 Part A poses (Office, Run!, night post) with the zone looks | Fix A's final `fa/tour_new` | 25 identical to the pixel; `np_dark` 4 px, `run_dark` 3 px (as §B6.1) |
| Tour's c1-c6 | the same | identical, but c1's title TV |
| Descent's 14 Part A poses (back-of-house) and c3, c4 | Fix A's `fa/descent_new` | max 1/255 |
| Level 0 room 7 with the pool door ahead: `pool_eye` (3 m), `pool_preview11` (11 m), `pool_dark`, `pool_q34` | this session's TODAY vs NEW | every pixel over 2/255 inside door 7's outline: x 651-1268, y 206-848 at 3 m; x 879-1040, y 452-625 at 11 m (the q34 view: the door's own region) |
| The film in Level 0 room 7, 72.0-74.7 s (door 7 shut ahead) | the same | the difference box is the door's, growing as the camera nears it (832-1088 x 411-703 at 72.0 s to 730-1217 x 288-857 at 74.5 s) |
| The night post with the pool's door ahead (Descent `dp5_eye`) | the same | inside the door's outline, plus 248 px below it: the white leaf's reflection in the night post's floor and the ends of its base trims (Part A's stops) |
| Inside the pool rooms (`pool_back`, `pool_exit`, `dp6_*`, `dp7_*`) | the same | the door, the band's ends (now at the frame) and the door's reflections in the glossy tile and the water |

### B6.3 The smokes

Every run of BB1 (8), BB2 (4), BB3 (2) and BB4 (2) ended rc 0 with 0 `error CS`; the one exception in each is Unity's QuickSearch indexer at Editor start (`SearchDatabase.cs:351`), as in every run since S1. The title keeps its timeline (Tour exits at 95.42 s, Descent at 85.02 s, as in Build A). `doors.tsv` shows the pool doors' zone as Pool (Tour 7, 8; Descent 5, 6, 7); no "re-dressed while it may be seen" warning, no "could not be built".

## B7. Photosafety: the pool doors stay off in the title until the title's lamp strike is bounded

`Tools/lookdev/flash_check.py`'s own functions (WCAG 2.3.1 general and red flashes, the area rule on 30 x 18 blocks of 640 x 360 decodes; limit 3 flashes in any 1 s), on the films of BB1-BB3 (`W/zone_doors/tools/bb_flash.py`, `bb_flash_window.py`; `W/zone_doors/buildB/flash.json`):

| Window | TODAY (walnut) | PD-A white (as built) | PD-A, smoothness .62 (V2) | PD-A, .62 + sRGB 218 218 210 (V3) | PD-B aluminium | PD-C aqua epoxy |
|---|---|---|---|---|---|---|
| Tour 84-92 s: the walk through door 8 (POOL → Level 0 room 9) | 2.5 | **3.5** (worst 1.5 s 3.5-4.0) | 3.5 | 3.0 (worst 1.5 s 3.5) | 3.0 | 3.0 |
| Tour door 7 window (Level 0 → POOL) | 1.5 | 2.0 | | | | |
| Descent doors 5 / 6 / 7 | 0.5 / 1.5 / 0.0 | 2.5 / 2.0 / 1.0 | | | | |
| Red flashes, every film | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 | 0.0 |

**The cause, measured:** the flashes at door 8 are the title's own lamp strikes. When a door opens, the room past it cues its ballasts (`FrontRoomsRoomStream.ConfigureLightProfile`): each strikes 0.4-2.2 s later with 0-5 flickers of period 0.13-0.34 s between output 0.92 and 0.035. The camera walks between the open leaves just as room 9's first troffers strike: the worst block (a troffer high in the frame) swings 0.02 ↔ 0.55 in linear luminance in TODAY and in PD-A alike, and in TODAY 93 of the left third's 120 blocks already swing by 0.1 or more (2.5 flashes, just under the limit). The white leaves, an arm's length from those troffers and seen at a grazing angle, carry the same pulses (their side-third mean 0.10 ↔ 0.27 against walnut's 0.04 ↔ 0.09): 114 of 120 blocks, 3.5 flashes. Softening the white (V2, V3) or a darker option (PD-B, PD-C: 3.0, at the limit) does not give a margin; walnut's 2.5 is not one either.

**What is built, then:** `FrontRoomsDoubleDoorKit.PoolDoorsLive` (static, default **false**). `ZoneBuilt(Pool)` returns it, so in the game the pool's doors wear Level 0 and the band keeps today's stop, exactly Build A's state (main's today: §B6.4 proves it). Everything of PD-A is built and switches on with one line once the strike is bounded: the light design run's LW0 (photosafety first; Red's "全部按推荐" of 2026-11-06 11:08 on research/light_design/10_research.md) is where the title's strike belongs, since it also leaves doors 1 and 8 with no margin today (HV.25 recorded 3.0 there before). Re-measure door 8 with PD-A when it lands. The door lab and the renders set the switch on (clone-only `-poolDoors on`); BB1-BB3 ran with the look live (the switch came after them).

## B8. Cost (lab census: the whole pair with its plates, LOD0; `W/zone_doors/captures/bb1/lab_pool/lab_log.txt`)

| Look | LOD0 measured | Δ against today's 26,578 | LOD1 / LOD2 Δ (sidecars and code meshes) | Renderers / draws Δ | Design [E] (20_design §B9) |
|---|---|---|---|---|---|
| PD-A REC | 32,942 (Fix B: 33,966 from the sidecars, the rings +512 a kit) | **+6,364** (+24 %); Fix B **+7,388** (+28 %) | +2,186 / +1,298; Fix B +2,698 / +1,298 | +1 renderer, +2 draws (the frame kit's 2 slots replace the walnut frame and oak saddle; 2 ring kits); Fix B unchanged | +5,454 / +1,362 / +400, +2 draws |
| PD-B (swap render) | 28,584 | +2,006 | +298 / +10 | +1 / +1 | +1,824 / +214 / −86, +2 (its own kit would have 2 slots) |
| PD-C (swap render) | 32,480 | +5,902 | +1,970 / +1,234 | +1 / +1 | +4,466 / +1,046 / +302, +1 |

- **PD-A's parts:** frame kit 1,550 / 444 / 196 against the walnut frame and saddle's 226 at every distance (+1,324 / +218 / −30); ring kits 2 x 2,048 / 512 / 192; the round slabs 2 x 544 against today's lite slab 2 x 188 (+712 at every distance: a code mesh has one level); the discs 2 x 128 against 2 x 12 (+232).
- **Over the design's estimate** (VISUAL_STANDARD §1.4, said plainly): LOD0 +910 (the frame's 4 mm wall with its rounded arrises and round head corners +250, the round slab 544 against ≈ 270 a leaf: the parting seam round all four edges and the 8 spherical corners); LOD2 +898 (the slab's 712 at every distance, against the design's +60, and the rings' 192 crown against 96).
- **Textures (new):** the gel-coat set 3 x 1024² (BC7 with mips, ≈ 4.0 MiB) and `DoorWear_Pool` 1024 x 2048 (≈ 2.7 MiB): ≈ 6.7 MiB desktop, ≈ 4.7 MiB at the phones' 1024 cap of the mask. **Materials (new):** 6 for PD-A (`Door_PoolGelcoat`, `_L`, `_R`, `_Frame`, `Glass_Tempered`, `_TemperedDark`), 2 for the swaps.
- **With `PoolDoorsLive` false** (the package's default) the game draws nothing new: `Prepare` builds no pool part (ZoneBuilt is false) and no pool material is loaded; the textures still ship in the desktop build (everything under `Resources` is packed), ≈ 6.7 MiB.
- **In the title:** at most one pool door is within 2 m of the camera (Tour door 7 or 8); the Tour's frame at the pool preview carries the pair's +6.4 k at LOD0 for the seconds the camera is within the rings' 2 m.

### B6.4 The package's default (`PoolDoorsLive` false) shows today's pool doors (session BB4, 16:20-16:23)

`tour_gateoff`: the Tour with the zone looks on and main's default switch (the run's own log: "pool doors False"; `doors.tsv`: doors 7 and 8 Level0), the same pose file as BB1. Its 18 pool poses against BB1's TODAY: 13 identical (max ≤ 1/255), `pool_dark` 2 px (the dark poses' sparkle), and the rest Part A's looks seen in mirrors or through glass: `pool_heroC` 600 px (the 630 push plates mirror door 6's night-post frame behind the camera), `pool_back_heroA` 369 px at the frame's right edge, `pool_exit_q34` 202 px at its top, `pool_exit_far` 8 px through door 8's lite (door 9 is the Office's), `pool_band` 24 px (max 19). Its 27 Part A poses against BB1's NEW: identical (max ≤ 1) but the two dark-pose sparkles. The door lab (the switch on there) still builds PD-A at 32,942 tris.

## B9. Where the build differs from 20_design.md Part B, and why

| # | Design | Built | Why |
|---|---|---|---|
| 1 | the pool's look live in the title once built (`ZoneBuilt(Pool)` true) | built, but **off in the game** behind `PoolDoorsLive` (false) | photosafety (§B7): the Tour's walk through door 8 reaches 3.5 flashes a second with PD-A's white leaves (limit 3); the design's B11 expected no rise ("a white leaf differs less from the lit room behind it than walnut does") but the flashes come from the strikes at arm's length, not from the room behind |
| 2 | the ring's lip at 4.0 mm on a 1 mm silicone bed (not modelled) | the lip at 3.05 mm, bedded on the pane | the white bed and the white ring are one material and one look; a 1 mm slit under the lip would show the cut-out's wall at grazing angles |
| 3 | the gel-coat maps at full strength | `_BumpScale` 0.6 on the leaves and the frame, 0.35 on the rings | the look round (BB1 → BB2): at full strength the orange peel read as crinkled film in the highlights at 0.35-0.4 m, and the rings' steep foot sparkled (specular aliasing of the peel on a 3 mm facet); after it the ring's foot is a clean line and the peel a soft waviness |
| 4 | the ring's foot on the face (19.0, 22.25) | (19.04, 22.15): 0.1 mm into the face along the last facet's slope | seated, no slit and no shared plane (the lite kits' rule, `FACE_EMBED`) |
| 5 | the ring kit LOD2 24 x a flat annulus (96 tris) | 24 x 2 profile segments, the crown (192) | a flat annulus at the crown's height would float over the lip; the crown keeps the silhouette at 8-40 m |
| 6 | the round slab ≈ 270 tris | 544 | the moulded r 3 mm arrises need curved strips and 8 spherical corners to meet without a T-junction (Fix A's lesson), and the parting seam runs round all four edges, its groove carried round the corners |
| 7 | the frame kit ≈ 1,300 / 280 / 150 | 1,550 / 444 / 196 | the 4 mm wall's inner path keeps its own fillet at the land (r 5.5), and the round head corners take 8 / 4 / 2 rings at every level |
| 8 | the glass object renamed "lite glass (porthole)" | today's name kept ("lite glass (lobby)"); the mesh is named for the porthole | `Gather` finds today's glass by that exact name; Part A's zones keep it too |
| 9 | PD-B's own aluminium kits | Part A's steel frame instance and F1 kit in `Door_PoolAluminium` | the design's swap-render plan (§B4.4): the own kits only if Red picks PD-B |
| 10 | PD-C's pressed steel ring with 8 screws | PD-A's moulded ring in `Door_PoolEpoxy` | as #9 |
| 11 | (not in the design) | `PoolFinish` set per pair at `Prepare` (each pair keeps its finish) | the door lab shows the three finishes side by side in one scene |
| 12 | the ring's foot (19.0, 22.25), the last facet 70 deg off the face (#4) | Fix B: a concave flank and a feathered foot, (18.9, 23.8) -> (20.2, 22.75) -> (21.9, 22.15), the last facet 19 deg off the face on LOD0 and LOD1, 23 deg on LOD2; ring Ø 450.2 mm, 233 mm to the push plate | Review B finding 4: the steep foot faced away from the lamps and took SSAO in its crease: a dark 3-4 px outline on desktop, a near-black 32-gon on phones (§B14.4) |
| 13 | the frame in the course aqua (the band's own colour) | Fix B: one step deeper (F1, sRGB 66 158 163) | the design's own remedy (§B13 item 2): the dark pool's goal reads by its outline (§B14.3); in the lit pool the band now meets a slightly deeper frame |
| 14 | the saddle all 630 stainless | Fix B: 630 tread, the bevels and arrises in the frame's gel coat | the title's unshadowed fill light drew a specular line on the stainless arris seen from the dark pool (§B14.5) |
| 15 | the ring kits start at LOD1 on phones (ReducedTier, as every leaf kit) | Fix B: the ring kits skip ReducedTier: LOD0 within 2 m on phones, +1,792 tris a kit there | the LOD1 32-gon's corners and its outline showed at the 0.35 m hero on phones (§B14.4) |

## B10. The gate

| Check | Result | Where |
|---|---|---|
| Roslyn (Unity 6000.6.3f1's csc, the project's defines) on main's tree (Build A landed) with the 2 changed C# files (`FrontRoomsDoubleDoorKit.Zones.cs` dcfa7f60, `FrontRoomsDoorLite.cs` 3eccaa77; zsh array; each Assembly-CSharp `.rsp` lists the 2 C paths) | macOS **COMPILE OK, 0 errors** (Assembly-CSharp 280 files, Editor 89); iOS defines **COMPILE OK, 0 errors**; run after the last edit of either file, and again at 16:30 on main as it stood then (same result) | `W/zone_doors/tools/gateB.zsh`, `W/zone_doors/_roslyn/buildB3_mac`, `buildB3_ios`, `buildB4_mac`, `buildB4_ios` |
| Roslyn of C itself (`--all`, the clone-only harness included) | COMPILE OK, 0 errors (Assembly-CSharp 282, Editor 110) | `W/zone_doors/_roslyn/buildB_clone` |
| Unity compile of C (batch, OSXUniversal) | 0 `error CS` in all 16 runs of BB1-BB4 (8 + 4 + 2 + 2) (the new kits, textures and materials imported at BB1's first open; the final code in BB4) | `W/zone_doors/logs/bb*_runs.txt` |
| Shaders | no change (the leaves use Part A's `_FR_LEAFWEAR` set, already in §A7's variant gate; the tempered glass is the existing no-wire variant) | |
| The apply dry run | the Package stage's (§B11) | |

## B11. For the Package stage: the paths, with main's bases (main holds Build A since 15:34; all 38 checked again 16:28: the 3 bases unchanged, the 35 new paths absent; again 17:1x, 18:5x and 19:23, §B15)

| Path | Main base (sha1) | Clone payload (sha1) |
|---|---|---|
| `Assets/Scripts/Rendering/FrontRoomsDoubleDoorKit.Zones.cs` | 28bc4ed6bb20b909a851582e2014e80ef5a75b97 (Part A; its `.meta` kept) | ac4633d31a974ddd8603cd4708a2026ec21589b6 (Fix B, §B14.4; Build B dcfa7f60f77bc4326416fb760bad055d35b7369a) |
| `Assets/Scripts/Rendering/Doors/FrontRoomsDoorLite.cs` | 17f9bcb8a3a6d7cfdef4860ecac05bf00dbd57ce (its `.meta` kept) | 3eccaa7762d51228cd23d70528eab973be675c89 |
| `Tools/lookdev/gen_door_wear.py` | 338481da5f7f2f3cfc228774eae8eafdd00511a6 (Part A) | 768a4a0320c054751cea02c6045d777453e0049b |
| `Assets/Resources/Props/Models/Kit_DoorFrame_FRPPair_DA`, `Kit_DoorLite_A1_FRP` (`.fbx`, `.json`, and their Unity-made `.meta`s) | ABSENT | 8 files (Fix B rebuilt both `.fbx` and the ring's `.json`, §B14.8; the `.meta`s kept) |
| `Assets/Resources/Surfaces/Door_PoolGelcoat`, `_L`, `_R`, `_Frame`, `Glass_Tempered`, `Glass_TemperedDark`, `Door_PoolAluminium`, `Door_PoolEpoxy` (`.mat` + `.meta`) | ABSENT | 16 files |
| `Assets/Resources/Surfaces/Textures/DoorGelcoat_A`, `_N`, `_S`, `DoorWear_Pool` (`.png` + `.meta`) | ABSENT | 8 files |
| `Tools/Blender/frontrooms_kit/assets/door_frame_frp_pair_da.py`, `door_lite_a1_ring.py`, `Tools/lookdev/gen_door_gelcoat.py` | ABSENT | 3 files |

Every payload file's sha1 with its base is in `W/zone_doors/tools/buildB_payload.sha1` (38 files: 3 replaced, 35 new; 16 new GUIDs, none used in main's Assets). Write order as Part A's package: the `.meta`s first, **each kit's `.json` before its `.fbx`** (`FrontRoomsKitImporter` reads the sidecar while it imports the model), the textures before the materials, the code last (it loads everything by name and keeps today's door if anything is missing). Never package the clone-only files this stage touched: `Assets/Editor/ZoneDoors/FrontRoomsZoneDoorLab.cs` (the pool bays), `Assets/Scripts/CloneOnly/FrontRoomsTitleProgrammeArgs.cs` (`-poolFinish`, `-poolDoors`). Every text file is LF; no Editor script is in the payload. What goes live at landing: nothing visible (`PoolDoorsLive` false): the pool doors of the title stay today's walnut pair until the switch is set (one line) after §B7's strike fix.

## B12. Open items (for the parent and Red)

1. **Photosafety (§B7), blocks turning the pool doors on:** the title's lamp strike at the walk through a door needs a bound (doors 1 and 8 have no margin today; PD-A's white makes door 8 3.5). Renders B measured the bound (§B13.4): room 9 with the title's own 1-flick cap 3.0 (no margin), with no flick 1.0. REC: a room entered through a live pool door reveals with no strike flick (one rule in `FrontRoomsRoomStream.TitleRooms`, tied to `PoolDoorsLive`), then `PoolDoorsLive` true; Red's OK first (the title stream model, inventory §2). LW0 is the pool water's glints, not this strike. Or Red picks PD-B / PD-C (3.0, at the limit).
2. **Red's pick (D-5):** PD-A (REC, built), PD-B or PD-C (`double_doors_renders_pool.jpg`, the films `double_doors_swing_pool.mp4`, `double_doors_swing_darkpool.mp4`, `double_doors_tour_all.mp4`, `double_doors_descent_all.mp4`: §B13, re-shot in §B14). PD-A's flaws: no period source shows an FRP DA pair at a US pool; white on white in the lit Pool (the deeper F1 frame outlines it); in the Dark pool the leaf is about as pale as its wall and the goal reads by its F1 outline and the gaps (§B14.3). **The portholes stay DARK until a pool door opens** (the title draws no room behind a shut door, and the vision lites show DARK there: inventory §2), so the "two discs of the cool tiled room" (20_design §B5) and the "two warm round windows" never show before the door opens: from 5-12 m PD-A and PD-C read as a face (two dark discs over the "||" push plates), the focal point of each walk toward a pool door in the films; no glass value removes it (§B14.2): Red's call.
3. **Sound (声音设计 paused):** the pool leaves are FRP; they keep today's wooden takes; `SoundOf(Pool)` gives Composite (PD-A) or Steel (PD-B / PD-C) for option (b) of `W/zone_doors/contracts/sound.md`. Nothing changes while the pool doors are off.
4. **The map's Pool member** (PL-F / PL-K) is the Map stage's: it reuses `Door_PoolGelcoat` (the rings' material, its relief at 0.35: the PL leaf may want its own `_L` / `_R` at 0.6), `Glass_Tempered`, the ring section and `FrontRoomsDoorLite`'s Tempered / Round support (`CollectGlass` already reads tempered slots).
5. **Not measured here:** WebGL (`Today` forced: no pool door), GlassRT on the tempered panes (they keep `_RTReceive` 1 as the wired lites), **frame time** (rule 25: batch renders are not timed; said on the sheets' COST rows since Fix B). The phone tier is in §B13.2 and §B14.4: Build B's LOD1 ring drew a near-black 32-gon outline at the 0.35 m hero (Review B; B13.2 had reported only the inner edge); since Fix B the rings keep LOD0 within 2 m with a feathered foot; today's Handheld SSAO still darkens the ring's inner edge (0.67 stop in Build B), Part A's SSAO contract (option C) lifts it.
6. **Disk** (candidates for Red to delete once the films are made; Renders B adds `W/zone_doors/captures/rb1` 4.8 GB, §B13.6): Build B's captures are 2.9 GB: `W/zone_doors/captures/bb1` 1.9 GB (the four title films' frames), `bb2` 0.5 GB, `bb3` 0.3 GB, `bb4` 0.2 GB; `W/zone_doors/tmp_kits/b` 6 MB. The disk is 96 % full (76 GiB free at 16:28).

## B13. Renders B: what Red sees for the pool, and the whole title (2026-11-06 16:33 on)

- **Stage:** Renders B, workflow visual-zone-doors-refine (wf_afdae4db-b34, parent session local_1fc9f09c, account G). Renders only: the build is §B1-§B12's; no file of the payload changed (the payload list of §B11 stands). Clone `W/proj_doorlean`, nothing landed in main, no git, no download, silent (`m_DisableAudio` 1, frames only).
- **What Red gets:** the pool sheet `sheets/double_doors_renders_pool.jpg` (TODAY | PD-A REC | PD-B | PD-C, the Pool and the Dark pool rows, cost), the overview `sheets/double_doors_renders_overview.jpg` with the pool rows added, the title films with every new door (`video/double_doors_tour_all.mp4`, `video/double_doors_descent_all.mp4`) and a swing film each for the Pool and the Dark pool (`video/double_doors_swing_pool.mp4`, `video/double_doors_swing_darkpool.mp4`).
- **The one clone-only change:** the title harness (`Assets/Editor/TitleStream/FrontRoomsTitleCapture.cs`, never packaged) gained `-tcStrikeCap "seq:n"`: the title's own `StrikeCapped` rule (§B13.4) applied to the rooms named, for the films' photosafety. PD-A is put live in the title by the clone-only `-poolDoors on` (the package ships `PoolDoorsLive` false, §B7).

### B13.1 How (sessions RB1 and RB2; frames `W/zone_doors/captures/rb1/<run>/`, logs `W/zone_doors/logs/rb1_*`, `rb2_*`)

As §A11.1 and §B6: batch Play mode in C, OSXUniversal, 1920 x 1080, game time 60 Hz, the pose cameras made from the game camera (Renders A's calibration: 2-3/255 on the Tour's frame). Desktop = quality Ultra (the desktop default); phone tier = Handheld + the iPhone 16 Pro path (`-tcPhone on`), the small kits forced to LOD1 as `ReducedTier` does on phones (plates, lite and ring kits), with C's `QualitySettings.asset` opened for Standalone for those runs only (`tools/fixa_quality.sh`, main's file put back after). Pose files `W/zone_doors/tools/poses_rb/` (Build B's pool sets plus the head corner, the ring's oblique section, the kick plate from the pool side, door 8 from Level 0 room 9; the Dark pool's lite, plates, kick plate, lights off, door 7 from WAKE; the phone poses). Scripts `tools/rb_all.sh`, `rb2_all.sh`; films `tools/rb_films.py`; flash on frames `tools/rb_flash.py` (`W/zone_doors/rendersB/flash.json`), on the final files `rendersB/flash_cli.txt`; sheets `tools/rb_sheets.py`.

| Run | Programme | Look | Film | Poses |
|---|---|---|---|---|
| `tour_pda` | Tour | PD-A (`-poolDoors on`), room 9 capped at 1 flick (§B13.4) | 0-95.4 s | the pool's 22 + c7, c8 |
| `tour_today`, `tour_pdb`, `tour_pdc` | Tour | `Today` / PD-B / PD-C | 70-80 s (door 7) | the same 22 + c7, c8 |
| `descent_pda` | Descent | PD-A | 0-85.0 s | the dark pool's 16 + c5, c6, c7 |
| `descent_today`, `descent_pdb`, `descent_pdc` | Descent | `Today` / PD-B / PD-C | 70-85 s (door 7) | the same 16 + c5-c7 |
| `tour_*_phone`, `descent_*_phone` (8) | Tour, Descent | the four looks | none | eye, lite hero (+ the SSAO option C pose), both faces |
| `tour_pda_c0` (RB2) | Tour | PD-A, room 9's lamps rising with no flick | 0-95.4 s | none |

### B13.2 The pool sheet (`sheets/double_doors_renders_pool.jpg`, 3200 x 11685, + `_fit.txt` ALL LABELS FIT)

Columns **TODAY | PD-A REC | PD-B | PD-C**, the same camera and exposure in each row (only the door changes). Rows: the Pool (Tour doors 7 and 8): eye 1.62 m at 3 m from Level 0 room 7; the preview from 11 m; the phone tier; 3/4 at 3 m; at 0.35 m square on, the lite, then the ring's section (oblique), the push plates, the kick plate and saddle, the hinge jamb and the frame's head corner (+2 EV: at 2.7 m it sits in the ceiling's shade in every look); lights off (room 7's lamps off); inside the Pool, door 7's +Z face, its lite from the pool side, the exit door 8, door 8 from the pool's far end (the design's white-on-white check, 20_design §B13 item 2), the leaves open at 88° over the dry sill. The Dark pool (Descent): the goal (door 7) at 3 m, the phone tier, 3/4, the lite at 0.35 m (+2 EV), door 6 looking back to the lit pool. The COST row last.

What the renders show (looked at row by row):

| Where | TODAY | PD-A REC | PD-B | PD-C |
|---|---|---|---|---|
| From Level 0 room 7 (3 m, 11 m, phone) | the walnut pair: Level 0 goes on | a white door with two round windows in an aqua outline: the pool announced before the door opens | a dark satin metal pair with two narrow slots: a service door | an aqua door with two round windows |
| 0.35 m | walnut, the wired lite's bead | the moulded ring a soft white collar on the gel coat, its foot a clean line (§B9 #3); the orange peel a faint waviness in the highlight; 630 plates (their landed hand wear), the fluted stainless saddle, 630 barrels on the aqua jamb, the head's rounded corner | the F1 pane in Part A's steel lite kit (its 6 screw heads, §B5); aluminium faces | PD-A's crown in the epoxy; aqua jamb and saddle |
| Inside the Pool | dark wood in white tile | white on white: leaf / tile wall linear luminance 1.12 at door 8 (walnut 0.17, PD-B 0.24, PD-C 0.76); it still reads at 10 m by the aqua frame, the black gaps, the plates and the two round windows | dark metal with cyan streaks | the band's own colour |
| Lights off (room 7) | the walnut pair nearly gone | the pale leaf and the aqua outline stay | near gone | a dark teal mass |
| The Dark pool | a dark mass, two tall slots | the leaf about as pale as its wall (the design's flaw, confirmed); the goal reads by its outline and the two round windows; the 12 lamps off change the frame by at most 27/255 (mean 0.3): the room is the off state already, so the sheet shows door 6 instead of a second dark row | near black | a dark teal mass, two round windows |

**Phone tier** (Handheld + the iPhone 16 Pro path, the rings at LOD1): correction by Review B / Fix B: not every look held its read: at the 0.35 m hero the LOD1 ring drew a near-black 32-gon outline round its foot (20-24 against the face's 165, 4-6 px, still there with option C), which this paragraph missed; Fix B feathers the foot and keeps the rings at LOD0 within 2 m on phones (§B14.4, the sheet's new 'phone hero 0.35 m' row). Today's Handheld SSAO (After Opaque, Depth: Part A's finding, `W/zone_doors/contracts/settings_handheld_ssao.md`) darkens the ring's inner edge: linear luminance 0.167 against the desktop's 0.266 (−0.67 stop); the contract's option C (radius 0.12, intensity 0.8, recommended) gives 0.307 (pose `pool_heroA_ssaoC`, `captures/rb1/tour_pda_phone/`). Not on the sheet (one more row for one value); the contract covers it with Part A's louvre.

### B13.3 The films and the flash check

**Fix B re-shot these four films (§B14.7): the files in `video/` are Fix B's; the table below is Renders B's record.** `Tools/lookdev/flash_check.py` (WCAG 2.3.1 general and red, the area rule, 30 x 18 blocks; limit 3 flashes in any 1 s): on the frames per door window (`tools/rb_flash.py`, `rendersB/flash.json`) and on each final file (`rendersB/flash_cli.txt`).

| Film (1080p30, H.264, silent, tone mark) | What it shows | Final file (CLI) | Frames |
|---|---|---|---|
| `video/double_doors_tour_all.mp4` (95.4 s, 36.0 MB) | the Tour with every new door: Office O-1, Run! C-1, the night post S-2, the pool PD-A (doors 7 and 8); room 9, past door 8, reveals with its lamps rising without the strike flicker (§B13.4), said on the picture (top left) from 0.5 s before door 8 opens to 8 s after (84.7-93.2 s) | **2.0 PASS** | whole 2.5 (door 1's walk-through, 2.5 in every look since Renders A); doors 0-8: 0.5 / 2.5 / 0.0 / 1.5 / 2.0 / 1.0 / 1.0 / 2.0 / **1.0** |
| `video/double_doors_descent_all.mp4` (85.0 s, 32.1 MB) | the Descent with every new door: Office, Run!, back-of-house B-2, the night post, the pool PD-A (doors 5, 6, 7: the pool, the dark pool, the goal); no light changed | **3.0 PASS, at the limit**: the worst second, 28.9-29.9 s in the right third, is the Run! room's own light (TODAY's too, 2.5 on the frames; Renders A cut its clip at 30 s for it), not a door; the pool doors read 1.5 (57 s) and 1.0 (78 s) | whole 2.5; doors 0-7: 0.5 / 2.5 / 1.0 / 0.0 / 0.0 / 2.5 / 2.0 / 1.0 |
| `video/double_doors_swing_pool.mp4` (7 s, 2.6 MB) | Tour door 7 (Level 0 room 7 → the Pool), 3 s before it opens to 4 s after, the four looks in a 2 x 2 grid at the same seconds | **1.0 PASS** | door 7: TODAY 1.5, PD-A 2.0 |
| `video/double_doors_swing_darkpool.mp4` (7 s, 2.6 MB) | Descent door 7 (the Dark pool → WAKE, the goal), the same | **0.0 PASS** | door 7: PD-A 1.0 |

Red flashes 0.0 in every film. Both swing films and the Tour film pass with a margin; the Descent film passes at the limit on a light that is not ours to change here (the Run! room's, the title stream model).

### B13.4 Photosafety: the room past the pool door (a decision for Red)

The walk through Tour door 8 (the Pool → Level 0 room 9), PD-A live, measured on the frames of the same title seconds (§B7 for the cause: room 9's troffers strike as door 8 opens and the white leaves at arm's length carry every flick):

| Room 9's reveal | PD-A at door 8 | Source |
|---|---|---|
| as today (each lamp 0-5 flicks of 0.13-0.34 s, then its rise) | **3.5** over the limit | BB2 `tour_v1` (the final material values) |
| the title's own `StrikeCapped` rule (at most 1 flick a lamp, as the pool rooms and the waiting room already reveal: `FrontRoomsRoomStream.TitleRooms` `CappedStrikeFlicks`) | **3.0** the limit, no margin | RB1 `tour_pda` (`-tcStrikeCap 9:1`) |
| no flick: each lamp rises from dark at its own delay (0.4-2.2 s) and rate (0.45-1.6 s); dead and failing lamps unchanged | **1.0** | RB2 `tour_pda_c0` (`-tcStrikeCap 9:0`), the Tour film's take |
| TODAY's walnut pair, room 9 as today | 2.5 | BB1 |

**Recommended (REC): a room entered through a live pool door reveals with no strike flick.** One rule in our own title file (`FrontRoomsRoomStream.TitleRooms`, beside `StrikeCapped`): when `FrontRoomsDoubleDoorKit.PoolDoorsLive` is true and the room's entrance door wears the Pool look, each of its lamps gets 0 flicks (delay, rise, dead and failing lamps as they are). Today that is Tour room 9 and Descent's WAKE (its door 7 already reads 1.0); with `PoolDoorsLive` false it changes nothing. Then `PoolDoorsLive` true ships PD-A. What the player sees: in that one Level 0 room the lights come on lamp by lamp, warming up, without the stutter of the other rooms; everything else is today's.

**Why Red is asked:** it changes how one Level 0 room's lights come on in the title, and the inventory (`research/door_inventory/10_inventory.md` §2) lists the title stream model as not to be redone. No other run covers it: the light design's LW0 is the pool water's glints (`research/light_design/10_research.md`, 'Checks per step'), not this strike. **Alternatives:** keep PD-A off until the title's strike is bounded in general (nothing is planned); or pick PD-B / PD-C (3.0 with room 9 as today: at the limit, no margin; the same rule would give them one).

### B13.5 Cost in the title (counters at the c-shots, medians of 12 frames, every pass counted, shadow casters included)

| Shot | TODAY | PD-A REC | PD-B | PD-C |
|---|---|---|---|---|
| c7: Level 0 room 7 at 5 m, the pool door ahead (the preview) | 267 draws, 35,992 tris | 275 (+8), 44,916 (+8,924) | 271 (+4), 38,232 (+2,240) | 271 (+4), 43,560 (+7,568) |
| c8: inside the Pool at 5 m, door 8 ahead | 285, 34,420 | 287 (+2), 40,632 (+6,212) | 285 (±0), 36,596 (+2,176) | 285 (±0), 39,708 (+5,288) |

The pair itself: §B8 (PD-A +6,364 / +2,186 / +1,298, +2 draws, ≈ 6.7 MiB; since Fix B +7,388 / +2,698 / +1,298, §B14.8). Frame time is not measured (rule 25: batch renders are not timed; said on the sheets' COST rows since Fix B); the phone tier's cost is the pair at LOD1 (+2,186; since Fix B +2,698, and the rings at LOD0 within 2 m: +3,584 there). Fix B's counters (c7 PD-A 275 draws, 45,940 tris: +9,948; c8 287, 41,656: +7,236) are in §B14.8.

### B13.6 Files, gate, what is left

- **Staged** (`W/zone_doors/to_main/Frontrooms3D/Documentation/research/double_doors/`): `sheets/double_doors_renders_pool.jpg` + `_fit.txt` (new); `sheets/double_doors_renders_overview.jpg` + `_fit.txt` (replaced in place: Part A's five rows from the same Fix A frames, plus the Pool and the Dark pool, the header rewritten; the Part A version kept at `W/zone_doors/rendersB/overview_partA_before.jpg`); `video/double_doors_tour_all.mp4`, `double_doors_descent_all.mp4`, `double_doors_swing_pool.mp4`, `double_doors_swing_darkpool.mp4` (new); this section.
- **Not packaged** (clone only or W): the harness's `-tcStrikeCap`; `W/zone_doors/tools/rb_all.sh`, `rb2_all.sh`, `rb_films.py`, `rb_flash.py`, `rb_sheets.py`, `poses_rb/`.
- **Gate:** no payload file changed here. Main moved at 16:52-16:55 (the structures G1 landing: the Surface shader's inert `_TileLot.z`, the lamp kit merge, `FrontRoomsMapWorld.Bevel.cs`, 5 new C# files); re-checked at 17:1x: §B11's 38 paths as recorded (3 bases unchanged, 35 absent in main, the clone's files unchanged), the 16 new GUIDs used by no `.meta` in main; Roslyn on main as it stands + the 2 C# overrides: macOS and iOS COMPILE OK, 0 errors (Assembly-CSharp 285 files; each `.rsp` lists the 2 clone paths; `W/zone_doors/_roslyn/rendersB_mac`, `rendersB_ios`); Roslyn of C with the harness change `--all`: 0 errors; Unity: 17 runs (RB1 16, RB2 1), each rc 0 with 0 `error CS` (the one exception in each, Unity's QuickSearch indexer at start-up, as since S1). C's `QualitySettings.asset` is main's again (edaed722).
- **Disk:** the frames of this stage are 4.8 GB (`W/zone_doors/captures/rb1`; the films' sources are `tour_pda_c0/film`, `descent_pda/film` and the four 70-85 s windows): deletion candidates for Red once the films are kept, with Build B's 2.9 GB (§B12 item 6). 76 GiB free at 17:2x.

## B14. Fix B: Review B's list (2026-11-06 18:14-19:3x)

- **Stage:** Fix B, workflow visual-zone-doors-refine (wf_afdae4db-b34, parent session local_1fc9f09c, account G). In C = `W/proj_doorlean`; nothing landed in main, no git, no download, silent (`m_DisableAudio` 1; frames only). Journal: `W/zone_doors/journal.txt`, the FIX-B lines.
- **Review B's list** (most severe first): (1) PD-A and PD-C read as a face at 5-12 m; (2) the Descent goal (door 7) vanishes into its wall in the Dark pool; (3) the A1 ring's foot renders as a dark outline (desktop) and a near-black 32-gon (phones); (4) a bright cyan line along the saddle under door 6 seen from the Dark pool; (5) PD-B renders dark gunmetal in Level 0 room 7; (6) the sheet and this file did not say several of these, nor that the portholes stay dark until a door opens, nor that frame time is not measured.
- **How it was tested:** the title harness (clone only) gained per-pose switches, each restored inside its frame: `mat=` (a material's values on clones for one pose; a run-time mask texture), `darkback` (the far room's lamps off), `probeoff` / `anchor=` (the frame kit's reflection probe), `probes` and `lights` (logs), `nodir` / `nonear=` (lights off), `gvec=` / `gfloat=` (a shader global for one pose). So one Tour and one Descent run show every variant in the same frame (sessions FBV, FBD-FBD4: `W/zone_doors/captures/fb/`); the final looks were then re-shot with Renders B's poses and film windows (session FB1: `W/zone_doors/captures/fb1/`).

### B14.1 What was done

| # | Finding | Done | Measured (stops of linear Y; rows 3 and 4 in 8-bit levels) | Left |
|---|---|---|---|---|
| 1 | the face (DARK porthole discs over the "||" plates) | nothing kept: the glass stays as built; an honest flaw on the sheet and a tile of what was tried | disc vs leaf at 3 m -3.87 st; the best material variant -3.05 st (it glows over the wall with the lamps off) | Red's call (B14.2) |
| 2 | the goal vanishes in the Dark pool | the frame one step deeper aqua (F1, sRGB 66 158 163) | frame vs leaf at 3 m -0.44 -> **-0.68 st** (head vs wall -0.98), at 9 m -0.42 -> -0.63; the leaf stays as pale as its wall (-0.05) | F2 / W2 on the sheet for Red; door 8's walk at the flash limit (B14.7) |
| 3 | the ring's foot outline | the foot feathered at 19 deg on LOD0 / LOD1 (23 deg LOD2); the ring kits keep LOD0 within 2 m on phones | desktop foot band min / face at 6 o'clock 42/172 -> **67/172**; phones 22/170 -> 22/170 (today's Handheld SSAO) and **56/166 with SSAO option C** | the phone crease is the SSAO contract's (Red's pick) |
| 4 | the light line under door 6 | the saddle's bevels and arrises in the frame's gel coat (the 630 tread kept) | 8-bit rows 841-848 of `dp7_back`, mean / max 50/141 -> **28/52**, under the floor's 37 (TODAY 26/44) (B14.5) | PD-B's swap keeps its line, 48/133 (its own kit only if picked) |
| 5 | PD-B dark in Level 0 rooms | captioned, with the lab tile | leaf 0.042 vs walnut 0.053, wall 0.139 (no local probe); 0.095 in the pool (box probe) | a PD-B build needs a probe or a lighter, rougher finish |
| 6 | the texts | the sheet header and rows, B12 items 2 and 5, B13.2's phone paragraph corrected; frame time "not measured" on every COST tile | | |

### B14.2 Fix 1: the porthole face (kept as built, said plainly)

**Why the portholes are dark.** The title draws no room behind a shut door (ta_door_culling) and the vision lites show DARK there (inventory §2: LIVE / DARK, PEEK). So until a pool door opens, each porthole is an opaque dark pane that keeps its reflection: the "two discs of the cool tiled room" (20_design §B5) and the dark pool's "two warm round windows" never show before the door opens. From 5-12 m two dark discs sit over the dark "||" of the HW-DA1 push plates on a pale leaf: a face. TODAY's dark wired slots sit at the same absolute level (linear Y 0.02-0.03) but on a dark walnut leaf and as tall slots: no face.

**What was tried** (both tempered materials changed together: `FrontRoomsDoorLite`'s fade borrows a copy of the LIVE material and animates only `_LiteDark`, so any other difference between `Glass_Tempered` and `Glass_TemperedDark` would pop at the start of every DARK -> LIVE fade, i.e. as the door opens):

| Variant | Disc vs leaf, Level 0 room 7 at 3 m | Door 8 at 10 m (the pool) | Room 7's lamps off (disc / leaf / wall) | LIVE from the pool side (disc) |
|---|---|---|---|---|
| as built (reflectance 0.085, film 0.04 x 0.4) | -3.87 st (0.025 / 0.369) | -4.30 st | 0.024 / 0.053 / 0.030 | 0.097 |
| G1 film 0.15 x 1.5 | -3.71 | -3.98 | | 0.109 |
| G2 film 0.25 x 2.5 | -3.45 | -3.46 | 0.032 | 0.129 |
| G3 film 0.35 x 3.5 | -3.09 | -2.84 | 0.041 | 0.160 |
| G4 reflectance 0.16, smoothness 0.86 | -3.26 | -3.55 | 0.036 | 0.142 |
| G5 film 0.25 x 2.5 + reflectance 0.14 + smoothness 0.88 | -3.05 | -3.00 | 0.042 (over the wall) | 0.162 |

**Why none is kept.** The DARK pane's base is the glass shader's fixed 0.006 (the vision lites' DARK block, not ours to change here); its light is the physical 8.5 % reflection of a dim room, and the film's scatter follows the room's ambient, which is low in the title's Level 0 rooms and does not drop when the lamps go out. The critic's target (-1.5 st, TODAY's slot against walnut) is out of reach: the best variant lifts the discs by about 1.2 st, still reads as a face at 10 m, glows a flat olive over the wall in a dark room (the "nothing glows in a dead-lamp room" rule) and veils the LIVE view (the disc from the pool side 0.097 -> 0.162, two thirds brighter). The sheet says so in its header, shows the face in the rows where it is ('portholes DARK: a face') and the variants in its Fix B section.

**Red's options** (not decided here): accept the face (PD-A as built); PD-B's rectangular F1 lites (no face; a service door, gunmetal in Level 0 rooms); or a change outside this fix: let a pool door's lites go LIVE before it opens (the title stream model and the vision lites are on the inventory's DO NOT REDO list, so only on Red's word).

### B14.3 Fix 2: the goal in the Dark pool (frame F1 built)

| Variant (Descent door 7 from 3 m / 9 m; the lit Pool's door 8 from 3 m) | Dark pool: frame jamb vs leaf | Dark pool: head vs wall | Dark pool: leaf vs wall | Lit pool: frame vs wall |
|---|---|---|---|---|
| Build B: the course aqua (102 194 196) | -0.44 / -0.42 st | -0.80 | -0.05 | -0.28 |
| **F1, one step deeper (66 158 163), built** | **-0.68 / -0.63** | **-0.98** | -0.04 | -0.94 |
| F2, two steps (46 128 135) | -0.84 / -0.80 | -1.14 | -0.05 | -1.60 |
| W1 leaf one step warmer (232 224 208) | | | -0.07 | |
| W2 leaf warmer and darker (226 216 196) | -0.33 | -0.80 | -0.17 | |

The Dark pool is lit by its cyan water light, so luminance there is nearly the green channel alone: a warmer white barely darkens (W2 -0.17 st) and reads cream in the lit Pool, which Red turned down for the pool's tile on 10-31 ("我希望底色不应该是偏暖的米白，而是和dream pool更相似的瓷白"). The deeper frame gives the goal an outline in the dark and helps white on white in the lit Pool; the pool band now meets a frame a shade deeper than itself. F1 is the design's own one step (20_design §B13 item 2); F2 and W2 are on the sheet for Red. The goal's leaf stays as pale as its wall: it reads by the F1 outline, the black gaps and the dark porthole discs.

### B14.4 Fix 3: the ring's foot (`door_lite_a1_ring.py`, `Kit_DoorLite_A1_FRP` rebuilt)

- **Cause** (Review B): Build B's last facet, (18.0, 25.0) -> (19.04, 22.15), stood about 70 deg off the leaf face, the class Fix A removed from the steel kits: under the ring it faced away from the ceiling lamps and took SSAO in its crease.
- **Section now** (d outward from the clear edge, w from the centre plane, mm): the lip, the inner face and the crown as built to the top (12.6, 28.6), then (15.6, 27.5), (17.6, 25.6), (18.9, 23.8) (55 deg), a concave turn to (20.2, 22.75) (39 deg) and the foot (21.9, 22.15) at 19.4 deg, 0.1 mm under the face; the visible foot line at d 21.6 (Ø 449.6 mm), the outer radius 225.1 mm, 233 mm to the push plate (the assert asks 230). LOD1 (17.6, 25.6), (20.2, 22.75), the foot (19 deg); LOD2 the crown and the foot (23 deg). A new assert: every level's last facet is seated and at most 24 deg off the face. Tris 2,560 / 768 / 192 (Build B 2,048 / 512 / 192).
- **Phones:** `FrontRoomsDoubleDoorKit.Zones.cs` `SpawnLeafKit(..., bool reduced = true)`; the A1 ring kits pass false, so a phone keeps LOD0 within the 2 m switch (no 32-gon at the hero) and takes LOD1 from 2 m; every Part A leaf kit still starts at LOD1 there. Cost on phones: +1,792 tris a ring kit within 2 m of a pool door (+3,584 a pair).
- **Measured** (`pool_heroA`, 0.35 m, the ring's foot band r 218-232 mm against the face beyond, 8-bit, at 6 / 7:30 / 9 o'clock): desktop Build B 42 / 65 / 100 against 172, Fix B **67 / 77 / 107**: a soft grey band (the flank's underside in shade) instead of a stair-stepped dark line. Phones (Handheld): Build B 22 / 25 / 23 against 170, Fix B 22 / 25 / 24 (the 32-gon gone, the dark crease left), Fix B with the SSAO contract's option C **56 / 56 / 75** (Build B with C 28 / 30 / 34). So the phone's black crease is today's Handheld SSAO (After Opaque, depth: Fix A's finding for the louvre), not the mesh; `W/zone_doors/contracts/settings_handheld_ssao.md` (option C recommended) is where it closes. The sheet's new row 'phone hero 0.35 m' shows it, and its Fix B section shows desktop and phone before / after (+ option C).

### B14.5 Fix 4: the line under door 6 (`door_frame_frp_pair_da.py`, `Kit_DoorFrame_FRPPair_DA` rebuilt)

Isolated in the title at `dp7_back` (Descent door 6 seen from the Dark pool), one switch a pose (8-bit rows 841-848, mean / max; the floor below 37):

| Pose | Line | Reading |
|---|---|---|
| as built | 50 / 140 | |
| the lit Pool's 12 lamps off (`darkback`) | 50 / 141 | not the lit Pool's lamps |
| the Dark pool's lights off (`dark`) | 50 / 141 | not its own lamps or niches |
| the frame kit's reflection probe off, or anchored on either side | 50 / 140 | the kit blends no probe (`GetClosestReflectionProbes`: none) |
| the water light off (`_FR_SubThrown.x` 0, `_FR_SubWL.w` 0) | 50 / 140 | not the water's thrown or bounced light |
| a cavity mask 0.45 / 0.30 / 0.20 on the saddle | 47 / 123 each | not indirect light |
| satin 630, smoothness 0.40 / 0.30 | 47-53 / 123-131 | spread, not gone |
| the saddle metallic 0 | 31 / 53 | gone: a specular on the metal |
| **the directional fill light "Soft ambient direction" off** (70 deg down, intensity 0.16, unshadowed) | **18 / 20** | **the cause** |

The title's unshadowed fill light makes a 1-2 px specular line on the stainless saddle's eased arris and bevel, seen from the Dark pool where everything else is dark. The light belongs to the title's zone look (not changed here). **Fix:** the saddle is a 630 fluted tread in a moulded FRP threshold: its faces more than 46 mm off z 0 (the two 1:2 bevels, their 1 mm arrises, the lips) take the frame's gel coat slot, the tread and its 7 screws stay `Prop_Stainless630`. The frame's aqua now runs round the opening into the threshold; no new slot, no new material, no new draw; the kit's tris, slots and sidecar are unchanged. **After:** 28 / 52 (TODAY 26 / 44). PD-B's swap render keeps its line (48 / 133: Part A's steel kit has one slot for frame and saddle); a PD-B kit would take the same threshold.

### B14.6 Fix 5: PD-B in Level 0 rooms (captioned)

The title's Level 0 rooms carry no local reflection probe, so the clear-anodised leaf mirrors the dark default reflection: linear Y 0.042 in Level 0 room 7 (TODAY's walnut 0.053, the wall 0.139), 0.095 in the Pool (its box probe), light satin in the door lab (a probe). That is how PD-B would look in the game, so the swap is not "corrected"; the pool sheet's PD-B column and its Fix B section say so, with the lab tile. If Red picks PD-B, its build needs a probe in those rooms or a lighter, rougher finish (20_design §B5's "the yellow room smeared in its faces" needs the probe).

### B14.7 The films and the flash check (FB1 frames; files replaced in `video/`)

| Film (1080p30, H.264, silent, tone mark) | Final file (CLI) | Frames (`tools/fb_flash.py`, `W/zone_doors/fixB/flash.json`) | Renders B |
|---|---|---|---|
| `double_doors_tour_all.mp4` (95.4 s, 36.0 MB, sha1 9384cee8) | **2.0 PASS** | whole 2.5; doors 0-8: 0.5 / 2.5 / 0.0 / 1.5 / 2.0 / 1.0 / 1.0 / 2.0 / 2.0; **the worst second 87.0-88.0 s (the walk through door 8) 3.0, at the limit** | 2.0; door 8 1.0 (1.5 in that second) |
| `double_doors_descent_all.mp4` (85.0 s, 32.0 MB, eed8a702) | **2.0 PASS** | whole 2.5; doors 5 / 6 / 7: 2.5 / 2.0 / 1.5; worst 2 s window 3.0 at 56.5 s (door 5, as Renders B) | 3.0 (the Run! room's own light) |
| `double_doors_swing_pool.mp4` (7 s, 40025201) | **1.0 PASS** | PD-A door 7 2.0, PD-C 1.5 | 1.0 |
| `double_doors_swing_darkpool.mp4` (7 s, 5159b9f9) | **0.0 PASS** | PD-A door 7 1.5, PD-C 0.0 | 0.0 |

**FLAG (photosafety, door 8 with the no-flicker preview):** the sessions are deterministic (room 9's ceiling blocks equal to 0.001 between Renders B and Fix B), so the rise from 1.5 to 3.0 in 87-88 s comes from the deeper F1 frame's jamb sweeping past the camera at arm's length (the left sixth's mean 0.187 -> 0.182 at 87.5 s), which tips a few more blocks over the area rule. Every film still passes; the door 8 walk has no margin left on the frames. The pool doors stay off in the game (`PoolDoorsLive` false); B13.4's condition stands and gains one line: re-measure door 8 with the real code before switching them on, and if it is 3.0 or more, Red picks between the F1 frame (the goal's outline) and Build B's course aqua (the margin).

### B14.8 Cost, files, gate

- **Cost per pair** (kit sidecars; Build B's lab census + the rings' +512 a kit; not re-measured in the lab): PD-A 33,966 tris LOD0 (**+7,388** against today's 26,578), LOD1 +2,698, LOD2 +1,298, +1 renderer, +2 draws, ≈ 6.7 MiB; PD-C 33,504 (+6,926), LOD1 +2,482; PD-B unchanged. Phones: the rings at LOD0 within 2 m, +3,584 a pair there. Title counters (medians of 12 frames, every pass): c7 PD-A 275 draws (+8), 45,940 tris (+9,948; Build B +8,924), c8 287 (+2), 41,656 (+7,236); PD-C c7 271 (+4), 44,584 (+8,592). **Frame time: not measured** (rule 25).
- **C changes** (pre-edit copies and SHA1SUMS in `W/zone_doors/clone_only_save/pre_fixB_20261106/`): `FrontRoomsDoubleDoorKit.Zones.cs` dcfa7f60 -> ac4633d3; `Tools/Blender/frontrooms_kit/assets/door_lite_a1_ring.py` e5b21cc7 -> a645a266 and `door_frame_frp_pair_da.py` 0c8dcbf7 -> 249965e1 (Blender 4.3 silent, log `W/zone_doors/logs/blender_kits_fixb.log`, built to `W/zone_doors/tmp_kits/fb`); `Kit_DoorLite_A1_FRP.fbx` 70afb14f -> 32985b11, `.json` fe2a90c9 -> c9bd2089; `Kit_DoorFrame_FRPPair_DA.fbx` c25a21ce -> 9de0449f (`.json` unchanged); `Door_PoolGelcoat_Frame.mat` 12a3f689 -> 6e2ef9db; every `.meta` kept. Clone only, never packaged: the harness `Assets/Editor/TitleStream/FrontRoomsTitleCapture.cs` (the per-pose switches above).
- **Gate:** Roslyn on main as it stands with the 2 C# from C (`Zones.cs` ac4633d3, `FrontRoomsDoorLite.cs` 3eccaa77; zsh array; each `.rsp` lists the 2 clone paths): macOS **COMPILE OK, 0 errors** (Assembly-CSharp 285, Editor 89), iOS **COMPILE OK, 0 errors** (`W/zone_doors/_roslyn/fixB_mac`, `fixB_ios`); Roslyn of C `--all` 0 errors; Unity compile in C: 0 `error CS` in all 18 runs of FBV (2), FBD-FBD4 (7) and FB1 (9) (the one exception in each is Unity's QuickSearch indexer, as since S1). **Apply package:** `W/apply/zone_double_doors_pool` (mkapply from the overlay `W/zone_doors/pkg_overlay_b`; 38 paths: 35 new, 3 replaced, 2 metas kept; 16 new GUIDs unused in main; write order as Part A's: metas, textures, each kit's `.json` before its `.fbx`, materials, tools, code; mkapply's own order kept in `W/zone_doors/tools/zone_double_doors_pool_expected.mkapply_order.txt`): **DRY RUN OK** against main. Payload sha1s: `W/zone_doors/tools/buildB_payload.sha1` (Build B's list in `W/zone_doors/fixB/buildB_payload_before_fixB.sha1`). Main moved since Renders B only by scene 4's landings (16:52-16:55 G1, 18:01 Structures); not synced into C (inert outside scene 4), the 3 bases unchanged.
- **Staged here:** `sheets/double_doors_renders_pool.jpg` (3200 x 15832) and `_overview.jpg`, both `_fit.txt` ALL LABELS FIT (Renders B's versions kept at `W/zone_doors/fixB/`); the four films above; this section.
- **Disk:** Fix B's frames are `W/zone_doors/captures/fb` (variants and diagnostics) and `fb1` (the re-shoot, with its film frames): deletion candidates for Red with Renders B's `rb1` (4.8 GB) and Build B's `bb1`-`bb4` (2.9 GB) once the films are kept.

## B15. The package (the Package B stage, 2026-11-06 19:22 on)

Nothing in main, no git, no download, no Unity run in this stage (the payload is Fix B's, compiled in C by FB1's 9 runs), silent.

**`W/apply/zone_double_doors_pool`** (apply convention v2; made by `W/tools/mkapply.py` at 18:51 from `W/zone_doors/pkg_overlay_b`, which equals C's 38 files and `W/zone_doors/tools/buildB_payload.sha1`: checked again at 19:23, 0 mismatches): `zone_double_doors_pool.sh` (no argument = dry run, `--apply` writes), `_bases.txt` (38 rows: 35 ABSENT, 3 at main's sha1), `_expected.txt` (38), `_keep.txt` (the 2 `.meta`s of the replaced C# files, GUIDs kept), `_payload/`, `_base/` (the 3 exact bases), `.diff`.

| Path group | Main base | Count |
|---|---|---|
| `Assets/Scripts/Rendering/FrontRoomsDoubleDoorKit.Zones.cs`, `Assets/Scripts/Rendering/Doors/FrontRoomsDoorLite.cs`, `Tools/lookdev/gen_door_wear.py` | 28bc4ed6 (Part A), 17f9bcb8, 338481da (Part A); unchanged in main at 19:23; the two C# `.meta`s kept (3e4ed5d1, 6349cfc7) | 3 replaced |
| 2 kits (`Kit_DoorFrame_FRPPair_DA`, `Kit_DoorLite_A1_FRP`: `.fbx`, `.json` and their `.meta`s), 8 materials + `.meta`s, 4 textures + `.meta`s | ABSENT | 32 new (16 new GUIDs, none used in main's 4,144 Assets `.meta`s) |
| `Tools/Blender/frontrooms_kit/assets/door_frame_frp_pair_da.py`, `door_lite_a1_ring.py`, `Tools/lookdev/gen_door_gelcoat.py` | ABSENT | 3 new |

- **Write order** (`_expected.txt`, as Part A's package; the set is mkapply's, its own order kept in `W/zone_doors/tools/zone_double_doors_pool_expected.mkapply_order.txt`): the 16 `.meta`s, the 4 textures, **each kit's `.json` before its `.fbx`** (`FrontRoomsKitImporter` reads the sidecar while it imports the model and declares no dependency on it), the 8 materials, the 4 tools, the 2 C# files last. No importer or postprocessor is in the payload, so nothing races Red's open Editor (handoff §6): the textures import by name through main's `FrontRoomsSurfaceTextureImporter`, the kits through main's `FrontRoomsKitImporter`.
- **Main API read:** none listed in the script (its step 5); the Roslyn gate covers every call into main (Part A's kit, `FrontRoomsSurfaces`, `KitLibrary`, `MeshBuilder`).
- **Checks at 19:2x:** LANDINGS' last row 18:40 (scene-4 docs), no open CLAIM on these paths; every text payload LF; no Editor script in the payload (clone only, never packaged: `Assets/Editor/ZoneDoors/*`, `Assets/Editor/TitleStream/*`, `Assets/Scripts/CloneOnly/*`); main's Assets `.meta`s are no iCloud placeholders.
- **Dry run** (19:23): `bash W/apply/zone_double_doors_pool.sh` against main: all 38 bases match (35 absent, 3 at their recorded sha1), the 2 kept `.meta`s unchanged, the 38 payload files match, 16 new GUIDs unused: **DRY RUN OK** (`W/zone_doors/pkgB/dryrun_main.txt`).
- **Rehearsal:** a scratch copy of main (its 4,144 Assets `.meta`s, the 3 bases, ProjectVersion): DRY RUN OK, `--apply` **MANIFEST OK** (38 paths, 2 kept `.meta`s unchanged; the manifest written beside the scratch target, none in `W/apply`), again **ALREADY APPLIED** (`W/zone_doors/pkgB/rehearsal_dry.txt`, `_apply.txt`, `_again.txt`).
- **Gate (rule 25):** Roslyn on main's tree as it stands with the package's own 2 C# payload files (`W/zone_doors/tools/pkgB_gate.zsh`, a zsh array; each Assembly-CSharp `.rsp` lists the 2 payload paths; the iOS one carries `UNITY_IOS` and `PLATFORM_IOS`): macOS **COMPILE OK, 0 errors** (Assembly-CSharp 285 files, 48 warnings: main's count, none in the 2 files; Editor 89), iOS **COMPILE OK, 0 errors** (`W/zone_doors/_roslyn/pkgB_mac`, `pkgB_ios`; log `W/zone_doors/logs/pkgB_gate.txt`). Unity: 0 `error CS` in Fix B's 18 runs, FB1's 9 on this exact payload. No shader in the payload (the leaves use Part A's `_FR_LEAFWEAR` set, already in §A7's variant gate; the tempered glass is the existing no-wire variant).
- **What goes live at landing: nothing visible.** `FrontRoomsDoubleDoorKit.PoolDoorsLive` is false: the title's pool doors stay today's walnut pair, no pool part is built and no pool material is loaded (§B6.4: the gate-off run equals Build A's state); `FrontRoomsDoorLite`'s Wired / Rect path is today's (§B6.1); `Today` and WebGL as Part A. The new textures ship in the desktop build anyway (everything under `Resources` is packed): ≈ 6.7 MiB. The map, input and sound files are untouched.
- **Turning the pool doors on** (a follow-up package, after Red): (1) Red's D-5 pick (PD-A is built; PD-B or PD-C would get their own kits first, §B5); (2) Red's OK for the photosafety rule of §B13.4 (a room entered through a live pool door reveals with no strike flick: one rule in `FrontRoomsRoomStream.TitleRooms`); (3) then `PoolDoorsLive = true` and a re-measure of Tour door 8 with the real code (§B14.7: 3.0 on the frames with the F1 frame; at 3.0 or more Red picks between F1 and Build B's course aqua).
- **Docs staged with it** (`W/zone_doors/to_main/Frontrooms3D/Documentation/research/`): `double_doors/30_build.md` (Part B whole, B1-B15; the head and §B11 updated in place), `20_design.md` (its head and Part B's status line: built, where the build differs), `10_research.md` (Part P's status line), `sheets/double_doors_renders_pool.jpg` + `_fit.txt` (new), `double_doors_renders_overview.jpg` + `_fit.txt` (replaced: Part A's five rows plus the Pool and the Dark pool), `double_doors_build_b_lab.jpg`, `_proofs.jpg`, `_title.jpg` + their `_fit.txt` (new; the title sheet re-issued with the SUPERSEDED chip by `W/zone_doors/tools/pkgB_supersede.py`, its tiles and cost Build B's, so nobody picks from it: Red picks from the pool sheet), `video/double_doors_tour_all.mp4`, `double_doors_descent_all.mp4`, `double_doors_swing_pool.mp4`, `double_doors_swing_darkpool.mp4` (new; H.264 1080p30, one video stream, no audio); `door_inventory/10_inventory.md` (rows updated in place, rule 20). `SOURCES.md` unchanged (no new source). No `images/`: nothing was downloaded.
- **Contracts** (`W/zone_doors/contracts/`): `sound.md` (a Package B section for Red, English and Chinese: no sound change at landing; when the pool doors go live, (a) keep the wooden takes, REC now, or (b) a Composite DA take set when 声音设计 resumes); `settings_handheld_ssao.md` (Fix B's note: the ring's crease on phones is today's Handheld SSAO; option C lifts it). Nothing for 交互设计: no member, pivot, collider, name or input file changes (the Map stage's Pool member brings the museum stand's note). Nothing for 关卡设计: no map file changes; `FrontRoomsMapHunter`'s call to `FrontRoomsDoorLite.PassesPlayerSight` keeps today's Rect test for every map lite (the Round test applies only to round lites, and no map door has one).
- **Combined-test rows and the VL proposal:** `W/vl_pending/zone_double_doors_pool.md` (P1-P8).
- **Shared staged files:** the Stairwell doors stage (started 19:23, the same workflow) appends its own part to this file and edits rows D1 / D3 of the inventory in the same staging folder. Part B's docs as they stood at PACKAGE READY are snapshotted with their sha1s in `W/zone_doors/pkgB/docs_ready/` (`MANIFEST.txt`), so Part B's docs can land without a half-written later part.
- **Still open after landing:** Red's D-5 pick; the strike rule (before `PoolDoorsLive`); the porthole face (accept it, PD-B's rectangular F1 lites, or LIVE portholes before the door opens, which touches the inventory's DO NOT REDO list); the frame F1 against Build B's course aqua or F2 / W2; the phone SSAO option C (Part A's contract); the sound (a) / (b); frame time not measured; `video/double_doors_tour_new.mp4` (Part A's Tour) is superseded by `double_doors_tour_all.mp4` and stays until Red deletes it.
- **Disk:** this stage wrote only text, one sheet and small logs; the frames of Build B, Renders B and Fix B (`W/zone_doors/captures/bb1`-`bb4`, `rb1`, `fb`, `fb1`, about 11 GB) are deletion candidates for Red once the films are kept.

---

# Part S · The stairwell doors: the EXIT door, the north door, the sealed south leaf (inventory rows D1, D3)

- **Line, stage:** 游戏视觉, account G, workflow visual-zone-doors-refine (wf_afdae4db-b34, parent local_1fc9f09c), stage "Stairwell doors: EXIT + north door", 2026-11-06 19:23 PST on. Journal `W/zone_doors/journal.txt` (STAIR lines).
- **Red's asks** (this line's chat, plain user turns, verbatim): 2026-11-05 19:17:06 "同时我需要游戏视觉显根据当年做调研，去翻新每一个区域的双开门模型，规格和结构上的要求和之前一样"; 19:35:21 "目前level 0的门不需要改变，我是需要你改变办公室，车站，后场，run！，泳池等的门"; 19:39:25 "…现在还剩下那些门没有细化…然后做细化和模型升级的调研和模型重做等工作。不要推翻之前已经做了的工作". The stairwell is not Level 0; the inventory (`R/door_inventory/10_inventory.md` §1D, §3.6, §4.3 item 6) lists its EXIT and north doors as the least modelled doors in the game, with their research and dress done.
- **Status:** PACKAGED 2026-11-06 20:3x, not landed: built in the clone `W/proj_doorlean`; `W/apply/stairwell_doors` DRY RUN OK on main, rehearsal MANIFEST OK, Roslyn macOS + iOS 0 errors (§S9). Nothing in main, no git, no download, silent (`m_DisableAudio` 1). No new Red decision is asked: the look is the landed stairwell A palette (Red 10-22 "全部按推荐", look A); the one new ask is the sound contract (§S10, `W/zone_doors/contracts/sound.md`).
- **Reused, not redone** (inventory §2): the stairwell A dress (putty leaf #B7B09E, frame #5F5A50, knuckles, kick plate, STAIR A / NO REENTRY / REENTRY ON THIS FLOOR / NOT AN EXIT plates, the regular-arm closer, the F13 lite and its LIVE / DARK logic, the safe zone's own-lights rule, the lit EXIT sign); A13 (chain, staple, padlock) and the unlock micro cut with the U1 fall; the crossbar; the F4 frame research (door_frame 10 §1.2: SDI 111A's 2 in face, 5/8 in stop, 1-15/16 in door rabbet, 7/16 in return); the stair research (stairwell 01 :155: a 1-3/4 in hollow-metal leaf in a welded hollow-metal frame, three ball-bearing hinges, a fire label on the hinge edge of the leaf and of the frame); interactables 10_spec (silencers on the latch stop, the leaf's edge seam, the 3 deg lock-edge bevel). No web research was needed.

## S0. What was built, in ten lines

1. **Render-only kits over the map's boxes**, as D1.6 dresses map doors: the map's cube leaves, the EXIT's trim and the shell's box frames keep their objects and colliders (A13, the dress, the body doors and the sound hang on them) and are not drawn (`forceRenderingOff`, because `FrontRoomsStairwell.SetVisible` re-enables `enabled`). No 关卡设计 contract.
2. **Three leaves** (`Kit_DoorLeaf_Stair` the EXIT, `_StairWide` the sealed south leaf in the shell's 1.12 m doorway, `_StairNorth` the next floor's start door): a 1990 hollow-metal stair door in the map's own 50 mm envelope (§S1.1), 4 mm gaps, a 3.2 mm head gap, an 8 mm undercut, the formed 1/16 in arrises, a visible edge seam on both stiles, the 3 deg lock-edge bevel, the F13 cut-out for the existing lite kit (not the north door), the leaf halves of the three butts at the dresser's knuckles, a painted-over fire label on the hinge edge; the north door's bored latch (faceplate + retracted bolt) at its lever.
3. **The EXIT frame** (`Kit_DoorFrame_StairExit`): SDI 111A's double-rabbet profile on the map trim's own envelope (50.8 mm faces 11.1 mm proud of each wall face, 182.2 mm through): the second rabbet and the 5/8 in stop on N's side (the leaf swings into the stair), three grey rubber silencers on the latch stop, the frame halves of the butts, the fire label on the hinge soffit; faces mitred and ground; no shadow (C23). No strike: the crossbar's rim latch is drawn retracted, and a rim strike would stand where A13's chain runs to its staple.
4. **The shell's two doorway frames** (`Kit_DoorFrame_StairSouth`, `_StairNorth`): the F4 2 in face 7/16 in proud of the stair wall (the boxes were 70 mm and 20 mm proud), the soffit through the 60 mm shell, block infill flush with the wall for the rest of the box's 70 mm hole (the band runs on over it); the south one carries its soffit on to the EXIT frame and covers the paper strip the shell's wider opening left, so from the stair the reveal reads as one deep steel throat; the north one has no stop (the body door may swing both ways), the lever's ANSI strike and the corridor-side face in the every-light finish.
5. **Finishes** on the project's PaintedMetal maps at today's mean colours (putty #B7B09E, frame #5F5A50: A13's leaf / lock contrast is set by the leaf's mean, §S5.3), smoothness at today's means; the leaves carry the new `DoorWear_Stair` layer (hand grime and a burnished latch edge at hand height, shoe scuffs low, a mop line, grey-primer chips on the latch arris and the bottom corners); the stair-side faces keep the safe zone's own-lights finish.
6. **LOD2** for `Kit_ExitDevice_Crossbar` (68 tris) and the four closer kits (body 40, shoe 12, arm 32, forearm 80), hand-built; their LOD0 and LOD1 are the landed meshes, vertex for vertex (§S2.3).
7. **The dresser** (`FrontRoomsStairwellDresser.cs`, ours) hangs the kits on the rigs it already dresses; every A13 and stair anchor stays where it was; the two closers' shoes move onto the new frame face (11.1 mm proud instead of the boxes' 20 mm); `FrontRoomsStairwellDresser.Today` = true (always on WebGL players) is today's dress, byte for byte.
8. **Sound:** nothing audible changes; the north door's lever event (`NorthDoorLever`) and the stair hinges' steel Foley are a contract for Red (声音设计 is paused): §S10.
9. **Proofs and renders:** §S5-§S7 (TODAY | NEW at the same poses in played runs of the main scene with the map's EXIT autopilot: the maze side, the unlock cut with the anchors logged, the stairwell inside, floor 2's start, the next corridor, the phone tier).
10. **Cost:** §S7.

## S1. Design: a 1990 stair door inside the map's envelope

### S1.1 Why the kits keep the map's geometry

The map builds the EXIT as a 1.0 x 2.1 m door edge with a cube leaf (0.05 x 2.08 x 0.98 m) on a hinge at the wall's centre line, trim boxes round it (`MapWorld.BuildExitFrame`: 50.8 mm faces, 182.2 mm through), and the stairwell (`FrontRoomsStairwell.Doorway`) builds the same kind of cube leaves and 70 mm box frames in its 60 mm shell. Every anchor the landed dress and A13 hang on is seated on those boxes:

| Anchor | Where it sits | Kept by |
|---|---|---|
| Crossbar (`MapWorld.MountExitCrossbar`) | its back on the leaf's push face, x 0.025 | the leaf's 50 mm |
| A13 turn, chain contacts (`FrontRoomsExitChainFall`: LeafX .025, LeafZ1 .99, latch case x <= .12) | the leaf's face; the latch jamb's casing box x <= .105, z .995-1.075 in the shut frame | the leaf's face; the frame inside the trim's envelope (11.1 mm proud) |
| Staple plate | N's wall face, 0.14 m past the opening, 1.0 m up | unchanged (the frame stops at 1.0508) |
| Plates, kick plate, closer body, knuckles | the leaf's faces (+1 mm, 0, 0, +8 mm) | the leaf's 50 mm |
| F13 lite kit (Kit_DoorLite_Exit, scaled 25/22) | a 50 mm leaf | the leaf's 50 mm; the kit leaf's own cut-out at the kit's rect |
| EXIT sign | 2.18 m on N's wall, 29 mm over the trim's head | the frame's head top at 2.1508 |
| Closer shoes | on the shell frames' head face | moved onto the new face (§S4) |

So the leaves keep 50 mm: a 2 in heavy-duty hollow-metal leaf (a 1-3/4 in one would leave every anchor 2.75 mm off its face), and the EXIT frame keeps the trim's outer envelope, which is already the period frame's face and return (door_frame 10 §1.2). Where the map's simplifications show, the kits say so instead of hiding them: the leaf turns on the wall's centre line, so the door rabbet on the stair side is 118 mm deep where a real frame's is 49 mm, and a leaf's hinge-side corner passes inside the hinge jamb when open (as the cube does in the trim today).

### S1.2 The leaf (one builder, three sizes)

- **Envelope:** x +-0.025; y 0.008-2.0968 (an 8 mm undercut, the stairwell has no saddle; a 3.2 mm head gap); z 0.004 to the opening less 0.004 (the cube leaves 10 mm, the 20 mm head gap the stop now hides from N's side).
- **Edges:** the face sheets' formed 1/16 in arris (r 1.6 mm, 2 segments); a visible edge seam (0.7 x 0.5 mm groove) down both stiles, ground but not filled; the lock edge bevelled 3 deg (1/8 in in 2 in: the push face is the short side); the top and bottom arrises rounded alike.
- **Cut-out:** the F13 slot for the landed `Kit_DoorLite_Exit` (its rect 0.679-0.7806 x 1.27-1.905, pocket 9.5 mm + 0.5 mm so the slab never shares a plane with the kit's pocket legs). The dresser hangs the lite kit as today; the cube is no longer holed.
- **Hardware the leaf owns:** the leaf halves of the three 4-1/2 in butts, mortised in the hinge edge at the dresser's knuckles (0.29 / 1.07 / 1.86 m); a fire label (2-1/2 x 1-1/4 in, two domed rivets) on the hinge edge at 1.465 m, painted over (stairwell 01 :155). The north door adds a bored latch's satin-chrome faceplate and its retracted bolt in the lock edge at the lever's 0.968 m.
- **Faces:** -X (P) and +X (S) in their own slots (the dresser's safe-zone rule: a stair face takes the own-lights finish), the edges with the room face. EXIT: P (N's side) `Stair_DoorSteel`, S (the stair) `Stair_DoorSteelInside`. North: P is the stair face (it is pushed out of X): `Stair_DoorSteelInside`, S (the next corridor) `Stair_DoorSteel`. Sealed south: every face `Stair_DoorSteelInside` (as today's cube).
- **Sizes:** `Kit_DoorLeaf_Stair` and `_StairNorth` for the 1.0 m map door, `_StairWide` for the shell's 1.12 m south doorway (a 3'8" leaf); the wide and north leaves remap their faces' u so `DoorWear_Stair` lands on every stair face with the latch edge at u 1.

### S1.3 The frames

- **EXIT (`Kit_DoorFrame_StairExit`):** the 16 ga section swept round the opening and mitred at the head: from N's return foot (0.5 mm into the wall face) the 2 in face 11.1 mm proud, the 1-9/16 in second rabbet, the 5/8 in stop (its stair-side face 2 mm off the leaf's push face, bridged by three grey rubber silencers on the latch stop at 0.35 / 1.30 / 1.90 m), the deep door rabbet, the stair-side 2 in face and its return; 3 mm convex and 1.5 mm concave bends. The whole section stands 0.5 mm into the opening (`SOFFIT_IN`): the map's papered wall ends on the opening's line, and a soffit on that line lost the depth test to the paper (the first capture showed a strip of Level 0 wallpaper in the door rabbet). The frame halves of the butts on the hinge soffit; the fire label on the hinge soffit past the leaf, visible from the stair. No shadow (C23). No strike (S0 item 3).
- **The shell's doorways (`Kit_DoorFrame_StairSouth` 1.12 m, `_StairNorth` 1.0 m):** the box frames filled a 70 mm hole in the shell's wall (the wall stops at their outer edge), 20 mm proud of the stair face. The kits fill the same hole: the F4 2 in face 7/16 in proud of the stair wall, the soffit through the shell, the last 19.2 mm of the hole as block infill flush with the stair wall (`Stair_Block` is world-planar: the coursing runs on; the dresser's band and cut-in line now stop at the 2 in face and cover the infill), the outer face flush with the shell's outer face. South: the soffit runs on 18.9 mm to the plane of the EXIT frame's stair face, with a 9.2 mm flange on each jamb over the strip of map paper the shell's wider opening (LeafRoom 0.06) left beside the EXIT frame; the EXIT leaf's 95 deg swing stays 17 mm off the flanges. North: no stop (in body-door mode the leaf may swing back into the stair: a stop would be crossed), the lever's ANSI strike (satin chrome, its opening dark) on the latch soffit at 0.968 m, the frame halves of the butts (the north door's knuckles are on the corridor side), the corridor side in the every-light finish (the next corridor sees 60 mm of it inside the end wall's opening).

## S2. Kits (Blender 4.3 silent, `Tools/Blender/frontrooms_kit/build_asset.py`, hand-built LODs `fr_lods`)

Modules (new, in `Tools/Blender/frontrooms_kit/assets/`): `door_stair_common.py` (the shared numbers, the leaf and frame builders, the UV rules, the LOD2 add-on, asserts), `door_leaf_stair.py`, `door_leaf_stair_wide.py`, `door_leaf_stair_north.py`, `door_frame_stair_exit.py`, `door_frame_stair_south.py`, `door_frame_stair_north.py`. Built to `W/zone_doors/tmp_kits/stair`, copied into C; the new kits' `.meta`s by Unity at import (fresh GUIDs). Logs `W/zone_doors/logs/blender_kits_stair*.log`.

### S2.1 The new kits

| Kit | LOD0 / LOD1 / LOD2 tris | Slots | Switch (m, lodBias 1) | What |
|---|---|---|---|---|
| `Kit_DoorLeaf_Stair` | 560 / 80 / 32 | Stair_DoorSteel, Stair_DoorSteelInside, Stair_Hinge | 5 / 15 / never | the EXIT leaf (0.992 x 2.0888 x 0.05 m, F13 cut-out) |
| `Kit_DoorLeaf_StairWide` | 560 / 80 / 32 | Stair_DoorSteelInside, Stair_Hinge | 5 / 15 / never | the sealed south leaf (1.112 m, F13 cut-out) |
| `Kit_DoorLeaf_StairNorth` | 680 / 104 / 12 | Stair_DoorSteel, Stair_DoorSteelInside, Stair_Hinge, Prop_ChromeSatin | 5 / 15 / never | the north door (no lite; the latch faceplate and bolt) |
| `Kit_DoorFrame_StairExit` | 1,018 / 216 / 120 | Stair_DoorFrame (N's side), Stair_DoorFrameInside (the stair side), Prop_Rubber, Stair_Hinge | 4 / 12 / never | the EXIT frame on the trim's envelope (shadows off) |
| `Kit_DoorFrame_StairSouth` | 142 / 94 / 70 | Stair_DoorFrameInside, Stair_Block | 4 / 12 / never | the shell's south doorway (1.12 m), the bridge to the EXIT frame |
| `Kit_DoorFrame_StairNorth` | 516 / 84 / 60 | Stair_DoorFrameInside, Stair_DoorFrame, Stair_Block, Stair_Hinge, Prop_ChromeSatin, Prop_PlasticBlack | 4 / 12 / never | the shell's north doorway (1.0 m), the lever's strike |

LOD0 is low for a hero kit because a hollow-metal leaf is flat: its detail is in the edges, the hardware (separate kits) and the finish; every arris, the seams, the bevel, the butts, the silencers, the label and its rivets are modelled, and the paint's stipple and wear are in the maps. LOD1 keeps the envelope and the cut-out with 1-step arrises; LOD2 is a box with the cut-out (the lite kit's glass must still show through it at distance), so no LOD switch changes the silhouette.

### S2.2 Asserts in the modules (every build passes)

- Leaf: the envelope (x +-0.025, y 0.008-2.0968, z 0.004 to the opening less 0.004; the latch edge's rounded corner 0.1 mm short of the line); no vertex at or beyond the EXIT frame's stop (x <= -0.027 in the kit frame); swung about the map's centre-line axis from 0 to 95 deg (both ways for the north door) the latch edge's worst reach stays 4 mm off the latch soffit (0.99591 of 1.0) and never enters the stop.
- EXIT frame: inside the trim's envelope (x +-0.0911, z -0.0508 to 1.0508, y to 2.1508), so the chain fall's casing box still holds the chain off it; no vertex inside the shut leaf; the latch-side stop no nearer the crossbar's latch head than z 0.980 (the head ends at 0.9702); the silencers 0.1 mm off the leaf's push face.
- Shell frames: inside the box frames' envelope (and the 18.9 mm bridge for the south one); nothing inside the shut leaf.

### S2.3 LOD2 for the landed hardware (P-1)

`interact_exit_device.py` and `interact_door_closer_{body,shoe,arm,forearm}.py` each gain a `lod2_proxy` and a `build()` wrapper that registers `door_stair_common.add_lod2` after the module's own build: LOD0 and LOD1 are made exactly as before (the same parts, the same decimation or `lod1_keep_all`), then a hand-built proxy is joined as `<NAME>_LOD2` (kitlib's metre UVs, the same slots). The sidecars' `lodDistances` were already (d01, d12, dcull); with a third level `FrontRoomsKitImporter` gives LOD2 the range LOD1 had past d12, and the cull stays where it was.

| Kit | LOD0 / LOD1 (unchanged) | LOD2 (new) | Proxy | LOD2 range |
|---|---|---|---|---|
| `Kit_ExitDevice_Crossbar` | 3,588 / 3,588 | 68 | two chamfered case prisms + an 8-sided bar | 6-25 m |
| `Kit_DoorCloser_Body` | 3,236 / 1,067 | 40 | a chamfered cover prism + the spindle boss | 5-20 m |
| `Kit_DoorCloser_Shoe` | 922 / 922 | 12 | a box | 5-20 m |
| `Kit_DoorCloser_Arm` | 1,112 / 1,112 | 32 | a box + a hexagonal hub | 5-20 m |
| `Kit_DoorCloser_Forearm` | 940 / 940 | 80 | the eye, the climbed neck, the rod and the stud as hexagonal tubes | 5-20 m |

**Checked:** the re-exported FBX files imported in Blender beside main's: LOD0 and LOD1 have the same vertex count, triangle count, slots and a hash of every vertex position equal to main's for all five kits (scratchpad `stair/cmp_fbx.py`); only `_LOD2` is new. The `.meta`s are kept (GUIDs and import settings); the sidecars gain `trianglesLod2` and a new `lodNote`.

## S3. Materials and the wear mask

| File | What |
|---|---|
| `Resources/Surfaces/Stair_DoorSteel.mat` | FrontRooms/Surface, mesh UV, PaintedMetal A / N / S (the project's painted-metal set, as Door_Enamel and Part A's back-of-house leaves), base sRGB (0.8698, 0.8606, 0.8369) = today's putty #B7B09E over the albedo map's mean (linear 0.6495 / 0.6100 / 0.5117), smoothness 0.9568 = today's 0.5 over the mask's mean 0.5226, `_BumpScale` 0.55 and `_TileSize` 0.6 (the stipple of a brushed enamel, not stucco: the first capture's 1.0 at 1 m read as stucco at 0.4 m), macro tone / dirt 0.10 / 0.08 as `Stair_SteelLeaf`, `_FR_LEAFWEAR` with `DoorWear_Stair`, rect (0.004, 0.008, 1/0.992, 1/2.0888), chip = the grey shop primer (linear 0.15, stored sRGB 0.4236 / 0.4236 / 0.4169, as Part A). The every-light finish: the EXIT's N face, the north door's corridor face |
| `Stair_DoorSteelInside.mat` | the same + `_FR_OWN_LIGHTS` (the safe zone), rect mirrored (-0.004, 0.008, -1/0.992, 1/2.0888) for the stair faces' u = -Z |
| `Stair_DoorFrame.mat`, `Stair_DoorFrameInside.mat` | PaintedMetal maps, base sRGB (0.4568, 0.4462, 0.4325) = #5F5A50, smoothness 0.9185 = today's 0.48, `_BumpScale` 0.7, `_TileSize` 0.6; Inside with `_FR_OWN_LIGHTS` |
| `Textures/DoorWear_Stair.png` | 1024 x 2048 RGBA linear (R grime, G dull, B burnish, A chip), from the new `Tools/lookdev/gen_door_wear_stair.py` (numpy, seed 61106; the channel contract of `gen_door_wear.py`, the stair leaf's own layout): the latch stile darkened and dulled at hand height (0.85-1.55 m) with broken palm marks and a burnished edge (0.92-1.3 m); shoe and rubber scuffs low (0.03-0.40 m, more toward the latch); a mop line at the foot; angular grey-primer flakes along the latch arris, at the bottom corners and a few knocks on the lower latch half. Means R 0.046, G 0.061, B 0.002, A 0.001. Meta from `DoorWear_BackOfHouse` (iOS / WebGL capped at 1024), fresh GUID |

Today's `Stair_SteelLeaf`, `_SteelLeafInside`, `Stair_Frame`, `_FrameInside` are untouched (`Today` uses them); the lite kit's two faces take the new leaf finishes when the kits are live, so its pressed trim matches the leaf's paint.

## S4. The code (`Assets/Scripts/Rendering/Stairwell/FrontRoomsStairwellDresser.cs`, ours, main 2abeed3f)

- **`Today`** (static; WebGL players always true, rule 5) and **`KitsLive`** = !Today and all six kits in the library; read once per dressing, so a dressed EXIT or stairwell keeps the look it was dressed with.
- **EXIT** (`OnExitDoorBuilt`): the cube's renderer and the trim's renderer get the putty and frame materials as today, then `forceRenderingOff`; `KitUnder(leaf dress, Kit_DoorLeaf_Stair, push)` and `KitUnder(frame dress, Kit_DoorFrame_StairExit, push, shadows off)`: each kit under a root scaled (-push, 1, 1), as the closer kits are; no stair-face skin; the F13 lite hung as today without holing the cube, its faces in the new finishes; the closer's shoe on the south frame's face (`ExitShoeOutKit` 0.2011 for 0.21).
- **Shell** (`DressShell`): the six `Frame ...` boxes keep their material and are `forceRenderingOff`; both leaf cubes likewise (`SetLeaf(..., hide)`); the band and cut-in line stop at the 2 in face (`KitFrameFace` 0.0508 for `TrimFace` 0.07). North door: `Kit_DoorLeaf_StairNorth`, knuckles on the corridor side, `Kit_DoorFrame_StairNorth` at the hinge's shut pose (`KitAtHinge`, under the stair dress, so it never swings), no skin. Sealed south leaf: `Kit_DoorLeaf_StairWide` under the sealed dress (shown from the seal on, as today), the dead lite in the inside finish, the closer's shoe at `SealedShoeOutKit` 0.0611, `Kit_DoorFrame_StairSouth` under the stair dress (it stands from the start: the EXIT is seen through it until the seal).
- **Unchanged:** every name A13, the cut, the sound and the tools read (`STAIR A dress (...)`, `Leaf rig`, `EXIT crossbar`, `EXIT frame N-X`, `Door hinge stairwell north / south`, `Door leaf`); the plates, knuckles, closer solve, lights, probe, signs, the safe-zone gate; colliders (render-only); Today's path, line for line.
- Gate: Roslyn on main with this one file (`W/zone_doors/tools/gateS.zsh`): macOS COMPILE OK 0 errors, iOS COMPILE OK 0 errors, each `.rsp` listing the clone path once.

## S5. Proofs (Unity sessions sd2 and sd3 in the clone, 2026-11-06 20:14-20:20)

**How.** Played runs of the main scene, `FrontRoomsMainScenePlaytest.RunBatch -autopilotExit -autopilotStation 4 -autopilotSeed 2554 -autopilotBodyDoors` (the shipped profile's body doors; without the flag the legacy autopilot stood 340 s at the title stream's start door: session sd1, stopped, its 21 frames in `W/zone_doors/captures/sd1/new/`), with the clone-only probe `Assets/Editor/Audit/StairDoors/FrontRoomsStairDoorsProbe.cs` (never packaged; `-stairDoorsProbe -sdLabel <run> [-sdToday] [-sdPhone]`). The probe moves the game camera to fixed poses at fixed moments of the run (floor 1's stairwell at t 5 s, the maze side at t 10.5 s, floor 2 from t 25 s) and gives it back; through the unlock cut it sets `Time.captureDeltaTime` to 1/60 s, so the cut's frames fall on the same clock in every run. Six runs (`W/zone_doors/tools/stair_run.sh`, the Unity slot protocol, logs `W/zone_doors/logs/sd2_*`, `sd3_*`): sd2 new / today / phone, sd3 new / phone / today_phone; 0 `error CS` in each; 36 frames a run (23 poses, 4 ladder frames, 9 cut frames) and `report.txt`, in `W/zone_doors/captures/<session>/<run>/`. The new runs end rc 138 at Unity's teardown, after the report and every frame are written; the others exit 0.

### S5.1 Today is today

- **Code:** with `KitsLive` false every changed line takes main's branch (§S4: the kit calls, the hides, the shoe offsets, the band gap and the lite's hole are all behind `kits`); what runs unconditionally is the constants, the two helpers' definitions and the `KitsLive` read.
- **Run:** the TODAY runs (sd2 today, sd3 today_phone) draw the map's boxes and no kit (the census: the 6 box frames drawn, no kit root), and their frames are today's dress (the sheets' left column). Not compared with a run of main itself: the dresser is the package's only code file, and its diff is the proof.

### S5.2 The unlock cut plays exactly

- **Logged** at the cut's start and at the clocks 0.30 / 0.63 / 0.87 / 0.93 / 1.02 / 1.12 / 1.32 / 1.82 / 2.62 s (the 1/60 s steps nearest 0.30-2.60): the leaf rig's pivot, the crossbar's world position, the staple's, the bounds of A13's renderers and `showingLocked`. **TODAY (sd2) = NEW (sd2) = NEW (sd3) on every line** (a diff of the reports' anchor sections). The cut's own motion is in all three alike: the leaf rig's 1.5 mm shake at 0.87 s, the chain's fall (A13's bounds from y 0.890 down to -0.007 by 1.82 s), the leaf swinging into the stair (the crossbar from z 53.975 to 54.662 at 2.62 s); the run reaches floor 2 at the same frame in each.
- **Seen:** the maze sheet's section 2 sets five of the clocks side by side (0.30 key in, 0.93 the slip, 1.10 the run, 1.30 the fall, 1.80 the leaf swings in): the chain, the padlock, the staple and the camera are the same; only the leaf's and the frame's surfaces differ.

### S5.3 A13's leaf / lock contrast (the ladder)

The probe measures stairwell 80's ladder poses (5 / 8.6 / 12 / 20 m on the leaf's axis) on masks: the leaf as drawn (the kit's renderers when live, else the cube) and A13's padlock; the mean Y of each mask's pixels.

| leaf / lock | 5 m | 8.6 m | 12 m | 20 m |
|---|---|---|---|---|
| TODAY desktop (sd2) | 1.37 | 1.18 | 1.10 | 1.01 |
| NEW desktop (sd2 / sd3) | 1.35 / 1.36 | 1.18 / 1.21 | 1.11 / 1.09 | 0.99 / 0.95 |
| TODAY phone (sd3) | 1.30 | 1.12 | 0.95 | 0.89 |
| NEW phone (sd2 / sd3) | 1.28 / 1.25 | 1.06 / 1.05 | 0.94 / 0.93 | 0.89 / 0.86 |
| lock pixels, desktop / phone | 378 / 103-105 | 85-86 / 15-16 | 37 / 15-16 | 9 / 5 |

- **The leaf** is deterministic: NEW is 1-2 % under TODAY at every distance (Y 0.0276 / 0.0282 at 5 m, 0.0336 / 0.0340 at 20 m): the wear layer's grime (mask R mean 0.046); the material's mean colour is today's (§S3).
- **The lock's** few pixels move up to 4 % between two NEW runs of the same build (Y 0.0337 / 0.0352 at 20 m), so the ratios carry about +-0.04 of run-to-run noise. Every NEW-TODAY difference is inside it except the phone at 8.6 m (-0.06 / -0.07), where the leaf is equal and the lock's 16 px read 6 % brighter in both NEW phone runs (not isolated; the new frame face behind the padlock's edge pixels is the likely cause).
- **Why the ratios are low in both columns:** at t 10.5 s the run's EXIT is the dark dead end stairwell 80 describes ("In play, floor 1's EXIT is a dark dead end"): leaf Y 0.03 here, 0.27-0.29 in stairwell 80's lit LOCKED ladder (3.97 / 4.65 / 4.22 / 5.23, 5.53 at 30 m). That lit ladder was not re-run (it ran in `W/proj_stairA2`, another clone); with the leaf 1-2 % darker and the lock unchanged it would read 1-2 % lower (3.97 to about 3.9): **inferred, not measured**; the combined test checks it (§S9, row SD3).

## S6. Renders (Shot format via `W/zone_doors/tools/sd_sheets.py`; TODAY left, NEW REC right, the same frame in each row)

- **`sheets/stairwell_doors_maze.jpg`** (2400 x 9633, `_fit.txt` ALL LABELS FIT), "the EXIT from the maze". Section 1, the maze side: 3 m with A13 locked; 3/4 at 2.7 m on the latch side; the 8.6 m ladder pose; heroes at 0.35 m (the latch jamb under the chain, the head under the EXIT sign, the latch jamb's foot, the hinge jamb). Section 2, the unlock cut at five clocks (§S5.2). Section 3, COST TODAY / NEW with the ladder (§S7).
- **`sheets/stairwell_doors_inside.jpg`** (2400 x 13584, ALL LABELS FIT), "the stair side". Section 1, floor 1's stairwell: the north door at eye height 1.62 m and 3/4, heroes at its lever and its hinge side; the EXIT's stair face at eye 2.4 m and 3/4; heroes at the closer head, the hinge jamb's reveal, the latch jamb, the F13 lite (LIVE). Section 2, floor 2: the sealed leaf at eye height and 3/4, heroes at its hinge and its closer; the north door from X; the next corridor's NOT AN EXIT face. Section 3, the phone tier (Handheld quality, the iPhone 16 Pro path, the dresser's reduced tier): the north door, the EXIT's stair face, the sealed leaf.
- Hero frames are lifted +0.5 to +2 EV in both columns alike (said in each label): the stair at 0.35 m is dark as played. Raw frames: TODAY `W/zone_doors/captures/sd2/today`, NEW `sd3/new`, phone `sd3/today_phone` and `sd3/phone`; sd2's new and phone runs are repeats (sd2 new still had the EXIT frame before its stair-side faces took the inside finish, §S8 item 6).
- What the sheets show (opened row by row): from the maze, the EXIT reads as the same putty door in a frame with a real stop and rabbet (subtle at 3 m by design: the colours and the envelope are today's; the heroes carry the change); from the stair, the reveal that showed a strip of Level 0 wallpaper and three boxes is one brown-grey steel throat with the deep rabbet, the butts and the fire label; the north door is a steel stair door with its latch at the lever and the butts on the corridor side; the sealed leaf sits in a 2 in frame face with the closer's shoe on it; the phone tier is as dark as today.

## S7. Cost (the run's census of the stair doors before the seal, drawn at LOD0; the kits' sidecars)

| Root | TODAY tris / renderers drawn | NEW | Change |
|---|---|---|---|
| EXIT leaf rig (stair dress + A13 + crossbar) | 22,372 / 18 | 22,924 / 18 | +552 (the leaf kit for the holed cube and its skin) |
| EXIT frame dress | 3,454 / 3 | 4,472 / 4 | +1,018 (the frame kit; the trim boxes sit outside this root and are no longer drawn) |
| north door hinge | 6,836 / 6 | 7,790 / 8 | +954 (the leaf kit and its corridor-side butts for the cube and its skin) |
| the shell's doorways | 72 / 6 (6 boxes) | 658 / 2 (2 kits) | +586 |
| **all** | **32,734 / 33** | **35,844 / 32** | **+3,110 (+9.5 %), -1 renderer** |

- The phone tier's census is the same (the reduced tier changes the lights, not these roots). After the seal the sealed leaf's kit (560 at LOD0) is drawn where the holed cube and its skin were; not in the census, which runs before the seal.
- **LOD1 / LOD2** (the kits' sidecars): leaves 80 / 32 (north 104 / 12), frames 216 / 120, 94 / 70, 84 / 60. The landed hardware's new LOD2 draws 68 (crossbar) and 40 / 12 / 32 / 80 (closer body, shoe, arm, forearm) past 15-25 m where it drew 3,588 and 1,067 / 922 / 1,112 / 940: a saving at distance.
- **Materials** +4 (`Stair_DoorSteel`, `_DoorSteelInside`, `Stair_DoorFrame`, `_DoorFrameInside`); the kits' other slots are landed ones. **Texture** +1: `DoorWear_Stair` 1024 x 2048, about 2.7 MiB on desktop with mips, 0.7 MiB on phones (512 x 1024 at their 1024 cap); like everything under `Resources`, it ships in every build.
- **Not measured:** draw calls and frame time (rule 25: batch renders are not timed; said on the COST row).

## S8. Where the build differs from the plan, and the honest flaws

1. **A 2 in leaf, not the research's 1-3/4 in** (stairwell 01 :155): every landed anchor sits on the map leaf's 50 mm faces (§S1.1); a 1-3/4 in leaf would leave each 2.75 mm off its face. A heavy-duty 2 in hollow-metal leaf is period-correct too.
2. **The stair-side door rabbet is 118 mm deep** (a real frame's is 49 mm): the map hinges the leaf on the wall's centre line, and the frame keeps the trim's envelope. It shows in the stair-side heroes (the hinge jamb's reveal), not at eye height.
3. **The leaf's hinge-side corner passes inside the hinge jamb when the EXIT is open** (as the cube does in the trim today): seen only edge-on, from the stair, with the EXIT open.
4. **No strike on the EXIT frame:** the crossbar's rim latch is drawn retracted, and a rim strike would stand where A13's chain runs to its staple. **No stop on the north doorway:** in body-door mode its leaf swings both ways.
5. **From the maze the change is subtle, by design:** the same colours and the same envelope (A13's contrast and every anchor depend on them); the change is in the frame's stop and rabbet, the leaf's edges and wear (the heroes) and on the stair side.
6. **Fixed during the build, from the captures** (all in the packaged kits and materials): Level 0 wallpaper winning the depth test in the EXIT's deep rabbet (the frame section 0.5 mm into the opening, `SOFFIT_IN`; TODAY still shows the strip, a gain); the paint's orange peel reading as stucco at 0.4 m (`_BumpScale` 0.55 / 0.7, `_TileSize` 0.6); the maze 3/4 pose standing behind N's side wall (pose moved); the EXIT frame's stair-side faces taking the map's warm lamps through X's walls (their own slot in `Stair_DoorFrameInside`). sd2's new run predates this last fix; the sheets use sd3.
7. **A lit wedge on the deep rabbet** at the EXIT's hinge jamb (the safe zone's own spot throws the shell's shadow edge across the rabbet): physically right, but at 0.35 m it reads as a bright triangle (`f1_inside_hero_hinge`).
8. **The phone tier is as dark as today:** under the reduced tier's light most of the kits' edges and wear sink, so the phone rows are nearly the same in both columns (the frames' outline and the reveal still read).
9. **The ladder** (§S5.3): low ratios in the played light in both columns; the 20 m lock is 9 px; the phone at 8.6 m reads 0.06 lower (the lock, not the leaf).
10. **LOD0 is low for a hero kit** (560-1,018 tris): the leaf is flat; the detail is in the modelled edges, the separate hardware kits and the maps.
11. **Not done:** the lit A13 ladder (inferred, §S5.3); a census after the seal; draw calls and frame time; a TODAY | NEW film of the cut (the cut is proved by frames at 1/60 s steps and the logged anchors); the walkable stair and the C23 chevrons (not this task).
12. **Unity rewrote the 5 hardware `.fbx.meta`s** on re-import (a third `lODScreenPercentages` entry for the new LOD2; GUID, remaps and every other setting unchanged): the payload carries the rewrite, so main's first import does not leave them dirty.

## S9. The package and the gate (2026-11-06 20:3x)

- **`W/apply/stairwell_doors`** (`W/tools/mkapply.py`, overlay `W/zone_doors/pkg_overlay_s` = the clone, 63 files, 0 mismatches): **63 paths**, 21 replaced (`Assets/Scripts/Rendering/Stairwell/FrontRoomsStairwellDresser.cs` 2abeed3f; the 5 hardware kits' `.fbx`, `.fbx.meta` (main's GUID) and `.json`; their 5 Blender modules `interact_exit_device.py`, `interact_door_closer_{body,shoe,arm,forearm}.py`), 42 new (the 6 kits' `.fbx`, `.json` and both `.meta`s; `Stair_DoorSteel`, `_DoorSteelInside`, `Stair_DoorFrame`, `_DoorFrameInside` `.mat` + `.meta`; `Textures/DoorWear_Stair.png` + `.meta`; `door_stair_common.py`, `door_leaf_stair{,_wide,_north}.py`, `door_frame_stair_{exit,south,north}.py`; `Tools/lookdev/gen_door_wear_stair.py`); **6 `.meta`s kept** (the dresser's, the 5 hardware `.json.meta`s); 17 new GUIDs, none used in main's Assets.
- **Write order** (`stairwell_doors_expected.txt`, edited as Packages A and B; the set unchanged, mkapply's order saved in `W/zone_doors/tools/stairwell_doors_expected.mkapply_order.txt`): the 22 `.meta`s, the mask, each kit's `.json` before its `.fbx` (`FrontRoomsKitImporter` reads the sidecar at import: remaps and LOD heights, no dependency declared), the 4 materials, the 13 tools, the C#.
- **Checks:** DRY RUN OK against main (`W/zone_doors/logs/stairwell_doors_dryrun_main.txt`); rehearsal on a scratch copy of main (its 4,162 Assets `.meta`s, none dataless, the 16 other bases, ProjectVersion): DRY RUN OK, `--apply` MANIFEST OK (63 paths, 6 kept unchanged), again ALREADY APPLIED, the end state = the clone on all 63 paths (`logs/stairwell_doors_rehearsal.txt`). **Gate:** Roslyn on main's tree as it stands + the payload's 1 C# (zsh array; both `.rsp` list the payload path once): macOS COMPILE OK 0 errors (Assembly-CSharp 285 files, Editor 89), iOS COMPILE OK 0 errors (`logs/stairwell_doors_gate.txt`). Unity 0 `error CS` in the clone's 6 runs.
- **No DEPENDS ON:** Parts A and B are in main and share no file with this package (main's docs unchanged since 19:36: this file 74b0be7f, the inventory cb5712b1).
- **Lands live:** `Today` is false, so the stair doors change at once (rule 25); `FrontRoomsStairwellDresser.Today = true` (WebGL players always) is today's dress. Render-only: no map, input or sound file, no collider, name or pivot changes, so no 关卡设计 or 交互设计 contract.
- **Docs that land with it** (staged in `W/zone_doors/to_main/Frontrooms3D/Documentation/research/`, snapshot in `W/zone_doors/pkgS/docs_ready/` with `MANIFEST.txt`): this file (Part S appended), `door_inventory/10_inventory.md` (rows D1, D3, §0, §2, §3.6, §4.3, §4.4), `sheets/stairwell_doors_maze.jpg` and `stairwell_doors_inside.jpg` with their `_fit.txt`.
- **Combined-test rows SD1-SD9:** `W/vl_pending/stairwell_doors.md` (the default look, Today, A13 and the cut, body doors, the seal, LOD and GlassRT, phone and pad with the first frame-time measure, Red's Editor after the landing, silence).

## S10. Open items

- **Sound (for Red; 声音设计 paused):** the contract in `W/zone_doors/contracts/sound.md` ("Stairwell doors", English + 中文): a `NorthDoorLever` event in `FrontRoomsStairwell` (关卡设计's file, about 3 lines, at the lever phase / the body door's release) and the steel takes for the stair doors when `KitsLive`; (a) keep today's takes now (REC: nothing audible changes at landing), (b) a steel lever and Foley set when 声音设计 resumes. Nothing waits on it.
- **For the combined test:** the lit A13 ladder (SD3), the stairwell's frame time on the phone and the pad (SD7), the hardware metas on Red's first import (SD8).
- **Disk:** the captures of this stage are small (`W/zone_doors/captures/sd1`-`sd3`, about 50 MB); the clone-only probe stays in `W/proj_doorlean/Assets/Editor/Audit/StairDoors/` (never packaged).

---

# Part M · The map's door rows: the Office finish, the Pool, back-of-house and Stair members (inventory rows B3-B7)

- **Line and stage:** 游戏视觉, account G, the zone doors refinement's Map doors stage (workflow wf_afdae4db-b34, parent session local_1fc9f09c), 2026-11-06 20:4x on; journal `W/zone_doors/journal.txt`, MAP lines.
- **Status:** BUILT IN THE CLONE `W/proj_doorlean`, PACKAGED as `W/apply/zone_map_doors` (118 paths, §M9), not landed. Live at landing: the back-of-house member and the Office rows; the Pool member wherever a pool plan exists (scene 4's district P); the Stair member after the map line's contract (`W/apply/zone_map_doors_contract`). `FrontRoomsInteractableKit.DoorRowsToday = true` gives every map door as before, line for line.
- **Red's words that bind it** (this line's chat, plain user turns): 2026-11-05 19:35:21 "目前level 0的门不需要改变，我是需要你改变办公室，车站，后场，run！，泳池等的门"; 21:15:15 "按默认做" (D-4: the Office leaf stays walnut G); 21:26:27 "选项 2：新增一类后场门（漆面钢门、踢板、百叶）。* 玩家在矮区会看到更像设备间的门。* 代价：关卡设计要加规则；一部分今天是 Level 0 样子的门会变。这里也该成这个选项" (D-2). D-5 (the pool door) at its recommendation PD-A, built in Part B; Red may still pick (PARENT NOTE 19:37: "The Map stage builds the Pool member on PD-A's leaf (built) unless Red picks otherwise").
- **Agreed with 关卡设计 / 总控** (PARENT NOTEs 21:57, 10:27): the members appended at the end of `FrontRoomsDoorMember`, Pool then BackOfHouse (rows 8-9, 10-11); in `DoorMemberFor`, the map line's `BackOfHouseDoor(door)` right after the map hook; precedence Run / Exit > Office / Pool > BackOfHouse > Lobby; the predicate is in main (`FrontRoomsMapWorld.BackOfHouse.cs` 26846a31, 10:23), so no DEPENDS ON.
- **Built from** (reused, never redone): the D1.6 rows and member rule (`interactables/d16/10_design.md` §3; `FrontRoomsInteractableKit.Door.cs` 0dbdc107), the DA leaf builders (`interact_door_da_common.py`, `interact_door_leaf_steel_da.py`, `interact_door_leaf_veneer_da.py`, `interact_door_lite_common.py`), HW-DA1's plate builder, Part A's back-of-house language (B-2: `Door_BackOfHouse`, the A2 louvre `Kit_DoorLouvre_F12`), Part B's pool language (PD-A: `Door_PoolGelcoat`, the A1 ring `Kit_DoorLite_A1_FRP`, `Glass_Tempered`, `FrontRoomsDoorLite` Tempered / Round), the door break (`door_break_lite_bases.py`'s method), the stairwell A finishes (Part S), the research (`10_research.md` §B.5, Part O §O2.6, Part P; `20_design.md` §B10).

## M0. What was built, in ten lines

1. **Three members appended** to `FrontRoomsDoorMember`: Pool (4: PL-F / PL-K), BackOfHouse (5: BH-F / BH-K), Stair (6: ST-F / ST-K). `DoorMemberFor`: the map's word (Run; Stair once the map gives it), then `BackOfHouseDoor`, then the Standard side: Office, then a pool zone (`PoolRules.IsPool`), else Lobby.
2. **Office rows within D-4's default:** OF-F keeps Red's walnut G in the dark-bronze F4 frame with satin chrome, and gains what the title Office pair (O-1) has: HW-DA1's 10 in 630 kick plate on both faces (new `Kit_DoorKickPlate_Map`) and the Office wear (`Door_Veneer_OfficeMap`, mask `DoorWear_OfficeMap`). OF-K keeps the key door's almond (T6) in the Office wear (`Door_Enamel_OfficeMap`). L0-F / L0-K are untouched.
3. **Back-of-house member (D-2 option 2):** BH-F = new `Kit_DoorLeaf_SteelLouvre_DA` (the painted hollow-metal DA leaf, F1, stainless kick plates, the A2 louvre's cut-out filled by the landed `Kit_DoorLouvre_F12`) in B-2a's warm grey with the service wear (`Door_BackOfHouse_Map`); BH-K = its locked twin in the key door's almond with L0-K's set and sign; both in the dark-bronze `Kit_DoorFrame_Steel_DA`.
4. **Pool member (D-5 at PD-A):** PL-F = new `Kit_DoorLeaf_FRP_DA` (the d16 DA envelope as a moulded FRP shell, r 3 mm arrises, a parting seam, the A1 16 in porthole on the hinge side with its 1/4 in tempered pane; the landed ring spawned into it) in the white gel coat with the pool wear (`Door_PoolGelcoat_Map`), in the new moulded `Kit_DoorFrame_FRP_DA` (the course aqua one step deeper), 630 trim and kick plates; PL-K = the locked twin in that aqua (T6 turned: the locked leaf darker in the white zones).
5. **Stair member:** ST-F = the stairwell A language on the D1.6 kits (`Kit_DoorLeaf_Steel_PaintedMetal_DA` in the putty `Stair_DoorSteel_Map`, `Kit_DoorFrame_Steel_DA` in `Stair_DoorFrame` #5F5A50); ST-K = L0-K's leaf in that frame. Only the map's word gives it: the contract for PL_FireStair's south door.
6. **Kits, LOD0 / LOD1 / LOD2:** louvre leaves 5,532 / 1,744 / 232 and 5,552; FRP leaves 4,486 / 1,760 / 346 and 4,506; FRP frame 2,428 / 880 / 320; map kick plate (+ _B) 2,950 / 18 / 10; break bases 5,490 / 1,870 / 310 (louvre) and 4,462 / 1,874 / 444 (FRP).
7. **The break:** the new leaves hang on the steel family's cassettes through their own bases (the intact leaf minus the window: the louvre, the lite and the porthole stay through every stage), with face-keyed palettes (the grey over its grey primer; the gel coats over chopped-strand glass, the PU foam core in the kraft slot) and stage materials for every new face.
8. **Tiers:** desktop as built; phones dress the map doors as today (`DressOnHandheld`), the door break's phone tier as landed; WebGL players do not dress map doors.
9. **Proofs and renders** (§M5-§M7): played runs of the main scene (seed 516574485), TODAY | NEW at the same doors and poses, desktop and phone, the T1 break, T6 measured in place, the LOD0 census per dressed door.
10. **Contracts:** 关卡设计 (M8 answers Stair for the pool's fire stair door), 交互设计 (the museum stand cycles seven members), sound for Red (the back-of-house doors break with the existing steel takes).

## M1. The members and the rule (the code; 游戏视觉's files only, RULES rule 3)

| File | Base (main) | Change |
|---|---|---|
| `Assets/Scripts/Rendering/Doors/FrontRoomsDoorRig.cs` | 44a78529 | `FrontRoomsDoorMember { Lobby, Office, Run, Exit, Pool, BackOfHouse, Stair }` (append-only: rows index `(int)member * 2`) |
| `Assets/Scripts/Office/FrontRoomsInteractableKit.Door.cs` | 0dbdc107 | `DoorRowsToday`; `DoorMemberFor` (below); `DoorRow` fields; `TodayRows` (the 8 landed rows) and `Rows` (14); `Build`: the face and trim swaps, `MirrorLeafWear`, the round lite, `BuildZoneParts`, `Trimmed`; `ShadowProxy` with the round and the two-hole proxies; the frame finish swap in `DressDoor` |
| `Assets/Scripts/Rendering/Doors/FrontRoomsDoorLite.cs` | 3eccaa77 | append-only: `AttachMap(..., Shape)`, `HoledShadowBox(rect, rect2)`, `HoledShadowBoxRound(rect)` (+ their caches in `ResetStatics`) |
| `Assets/Scripts/Rendering/Doors/FrontRoomsDoorBreakVisual.cs` | db66bc4e | `FamilyNames` + the two new bases; `Family.slotSwaps`; the map faces in `ReadIntactSurfaces`; a stainless hardware fallback for a steel-family leaf with no chrome; `FamilySwap` in `ApplySurfaces`; the face-keyed palette in `PaletteArray`; the row's frame finish in the frame break; the core override for the VFX |
| `Assets/Scripts/Rendering/Doors/FrontRoomsDoorBreakVfx.cs` | d3382c34 | append-only: `OverrideSolid(int, Material)` |

**`DoorMemberFor`** (precedence as agreed 2026-11-05 21:57): the map hook first (`DoorMemberFromMap`: Run today; Stair once 关卡设计 lands the contract); then `map.BackOfHouseDoor(door)` → BackOfHouse; then the Standard side's Office test (today's lines) → Office; then a pool zone on the Standard side (`PoolRules.IsPool`; both Low: either side) → Pool; else Lobby. The predicate is already false wherever the Office or the pool test answers, and a pool door keeps Pool whatever zone it opens into. Under `DoorRowsToday` the back-of-house and pool tests are skipped and `Row` reads `TodayRows`: today's rule and rows, the same objects.

**`DoorRowsToday`** (static, false shipped): the way back and the TODAY renders, as `FrontRoomsDoubleDoorKit.Today` and `FrontRoomsStairwellDresser.Today` are. WebGL players never dress map doors (`DressOnWebGL` false), so they need no override.

**Build:** after the leaf spawns, `faceFrom -> faceTo` (the Office faces), the lock trim (`trim`: the pool's `Prop_Stainless630` for the satin chrome, on the leaf's own plates and every lock part through `Trimmed`), `MirrorLeafWear` (below), the lite attached Round for the porthole, then `BuildZoneParts`: the kick plates (OF-F, PL-F, PL-K: `Kit_DoorKickPlate_Map` at kick_s, `_B` at kick_p, the plate's back 0.1 mm off the face) and the pair kit that fills the new leaf's cut-out (BH: `Kit_DoorLouvre_F12` at (0, 0.5334, 0.400); PL: `Kit_DoorLite_A1_FRP` at (0, 1.397, 0.324); turned -90 deg about Y so the kit's x runs along the opening and its screws face S; scaled 0.98876 through the leaf so the flange back and the ring's foot sit 0.1 mm into the 44 mm map face; slot finish per row; never culled, because the leaf's LOD2 keeps its cut-out at every distance). A free row on a leaf with no rose anchor (ST-F's steel leaf) takes the lever at its latch's height; every landed free row's leaf has the anchor, so nothing else moves.

**`MirrorLeafWear`:** the map leaves' metre UVs run u = +Z on face S and u = -Z on face P (probed on the landed leaves, `uvprobe.py` in the stage's scratchpad), and the surface texture importer gives every surface texture Repeat. The map members' masks (`DoorWear_*Map`) are therefore sampled with wrapU Mirror, set once per mask texture when a leaf wearing one is dressed: both faces read the latch edge at u 1. Only these three masks are touched; today's rows carry no mask.

**Shadow proxies:** the porthole leaf's proxy has a round 32-gon hole over the clear 16 in (`HoledShadowBoxRound`), the louvre leaf's proxy a second hole over the louvre (`HoledShadowBox(lite, louvre)`: light passes its blades); every other row keeps its proxy.

## M2. The rows (`Rows`; `TodayRows` = the landed 8)

| Code | Frame | Leaf | Face | Trim | Parts | Signs, plates |
|---|---|---|---|---|---|---|
| L0-F, L0-K | as landed | as landed | as landed | as landed | none | as landed |
| **OF-F** | Steel_DA (dark bronze) | Veneer_DA (walnut G) | `Door_Veneer_OfficeMap` | satin chrome (leafChrome) | 630 kick plates S / P | none |
| **OF-K** | Steel_DA | Steel_DA (almond) | `Door_Enamel_OfficeMap` | satin chrome | in-mesh stainless kick plates | EMPLOYEES ONLY, number plates |
| RN-F, RN-K, EX-F, EX-K | as landed | as landed | as landed | as landed | none | as landed |
| **PL-F** | `Kit_DoorFrame_FRP_DA` (aqua) | `Kit_DoorLeaf_FRP_DA` | `Door_PoolGelcoat_Map` (in the kit) | 630 | A1 ring (white), 630 kick plates | none |
| **PL-K** | `Kit_DoorFrame_FRP_DA` | `Kit_DoorLeaf_FRPLocked_DA` | `Door_PoolGelcoat_MapAqua` (in the kit) | 630 | A1 ring (`Door_PoolGelcoat_Frame`), 630 kick plates | EMPLOYEES ONLY, number plates |
| **BH-F** | Steel_DA | `Kit_DoorLeaf_SteelLouvre_DA` | `Door_BackOfHouse_Map` (in the kit) | satin chrome | A2 louvre (`Door_BackOfHouse`) | none |
| **BH-K** | Steel_DA | `Kit_DoorLeaf_SteelLouvreLocked_DA` | `Door_Enamel` (in the kit) | satin chrome | A2 louvre (`Door_Enamel`) | EMPLOYEES ONLY, number plates |
| **ST-F** | Steel_DA in `Stair_DoorFrame` | Steel_PaintedMetal_DA | `Stair_DoorSteel_Map` | satin chrome | none | none |
| **ST-K** | Steel_DA in `Stair_DoorFrame` | Steel_DA (almond) | as L0-K | satin chrome | none | number plates (no sign) |

## M3. Kits (Blender 4.3 silent, `Tools/Blender/frontrooms_kit/build_asset.py`, hand-built LODs `fr_lods`)

New modules in `Tools/Blender/frontrooms_kit/assets/`: `door_leaf_map_common.py` (the shared numbers and builders: the louvre's and the porthole's places, the FRP shell, the latch preps, the kick plates, the porthole's pane, the sidecar blocks, the asserts), `door_leaf_steel_louvre_da.py`, `door_leaf_steel_louvre_locked_da.py`, `door_leaf_frp_da.py`, `door_leaf_frp_locked_da.py`, `door_frame_frp_da.py`, `door_kick_plate_map.py`; and `Tools/Blender/frontrooms_kit/door_break_map_bases.py` (the two break bases). Built to `W/zone_doors/tmp_kits/map`, copied into C; their `.meta`s by Unity at import (fresh GUIDs). Logs `W/zone_doors/logs/blender_kits_map*.log`, `blender_map_bases.log`.

| Kit | LOD0 / LOD1 / LOD2 tris | Slots | Switch (m, lodBias 1) | What |
|---|---|---|---|---|
| `Kit_DoorLeaf_SteelLouvre_DA` | 5,532 / 1,744 / 232 | Door_BackOfHouse_Map, Prop_Aluminium, Prop_Chrome, Glass_Wired | 4 / 12 / never | BH-F: Steel_DA's 16 ga faces, channel lines, kick plates both faces, the pressed F1 lite painted with the leaf (6 heads on S), the A2 louvre's cut-out (Z 0.2635-0.5365, Y 0.3207-0.7461) in every level, the bored latch's satin-chrome faceplate at the lever (Y 1.000), rose anchors |
| `Kit_DoorLeaf_SteelLouvreLocked_DA` | 5,552 / 1,744 / 232 | Door_Enamel, Prop_Aluminium, Prop_Chrome, Glass_Wired | 4 / 12 / never | BH-K: the same with Steel_DA's 8 in mortise armor front and the key-door anchors (sign on the top rail, plates) |
| `Kit_DoorLeaf_FRP_DA` | 4,486 / 1,760 / 346 | Door_PoolGelcoat_Map, Glass_Tempered, Prop_Stainless630 | 4 / 12 / never | PL-F: the moulded FRP shell in the d16 envelope (r 3 mm arrises, 4 / 3 / 2 rings; a parting seam on the latch end; wear rings), the A1 porthole cut in every level (a 64 / 32 / 24-gon, r 0.2127) with its 6 mm tempered pane (r 0.2117), 630 pivot plates, the bored latch's 630 faceplate |
| `Kit_DoorLeaf_FRPLocked_DA` | 4,506 / 1,760 / 346 | Door_PoolGelcoat_MapAqua, Glass_Tempered, Prop_Stainless630 | 4 / 12 / never | PL-K: the same with a 630 mortise armor front and the key-door anchors |
| `Kit_DoorFrame_FRP_DA` | 2,428 / 880 / 320 | Door_PoolGelcoat_Frame, Prop_Stainless630, Prop_PlasticBlack | 4 / 12 / never | Steel_DA's F4 envelope as a moulded FRP sleeve (4 mm wall, r 4 mm arrises; the face band's outer head corners r 12.7 mm in elevation, one moulding), the mortise strike prep and box, the fluted 630 tread in a gel-coat threshold (Part B's Fix B rule), brush seals, 630 pivot caps |
| `Kit_DoorKickPlate_Map` (+ `_B`) | 2,950 / 18 / 10 | Prop_KickPlate630 (`_B`: 630B), Prop_PlasticBlack, Prop_Stainless630 | 2.5 / 10 / 50 | HW-DA1's 10 in plate for the 0.99 m map leaf: 0.94 x 0.254 x 0.050 in, four bevelled edges, eight #6 oval-head Phillips screws; the pair plate's worn map, cropped (metre UVs), not stretched |
| `Kit_DoorLeafBreak_SteelLouvre_DA` | 5,490 / 1,870 / 310 | Door_Enamel, Glass_Wired, Prop_Aluminium, Prop_Chrome | 4 / 12 / never | the louvre leaf minus the steel window (Z 0.60-0.995 x Y 0.50-1.22); the steel family's cassettes, debris, throws, anchors; + palette `Door_BackOfHouse_Map` |
| `Kit_DoorLeafBreak_FRP_DA` | 4,462 / 1,874 / 444 | Door_Enamel, Glass_Tempered, Prop_Stainless630 | 4 / 12 / never | the FRP leaf minus the same window; palettes for the gel coat (white and aqua, over chopped-strand glass), the PU foam core (kraft slot), the polyester resin (glue slot), the aqua frame (steelframe slot); `slotSwaps` Door_CoreSteel -> Door_CoreFoam |

**Asserts (every build passes):** the d16 envelope and the swing radius (`assert_leaf`); the louvre frame and the porthole ring end before Z 0.5495 (the break window's clearance: the cassettes never cut them); the louvre frame 39 mm over the kick plate and 44 mm under the damage mask; A1 >= 200 sq in with its bottom at 47 in (<= 48); the ring clear of the top-rail sign and the kick plate; the pane 1 mm inside the cut-out; the FRP frame inside Steel_DA's envelope (`assert_frame`), nothing in the opening, the head corner rounded, its face 3.1 mm in front of the map's door trims (the trim face lies inside the 4 mm wall, hidden; Steel_DA's 1.5 mm sheet cleared it from behind instead).

**Reused, not rebuilt:** the A2 louvre is the landed `Kit_DoorLouvre_F12` and the A1 ring the landed `Kit_DoorLite_A1_FRP` (Part A, Part B; both built for the pair's 1-3/4 in leaf): one look for the title pairs and the map doors, no second kit.

## M4. Materials, masks, the break

| Asset | Recipe | Used by |
|---|---|---|
| `Textures/DoorWear_OfficeMap.png`, `_BackOfHouseMap.png`, `_PoolMap.png` | 1024 x 2048 RGBA linear (R grime, G dull, B burnish, A chip), new `Tools/lookdev/gen_door_wear_map.py` (numpy, seeds 61116-61118; gen_door_wear.py's channel contract and noise, the map leaf's own layout: u = Z / 1.0, v = Y / 2.11). Office: the hand halo round the lever and the push zone by the latch stile, knuckle marks, the latch edge darkened, the chair-arm band (Y 0.58-0.79), pale cart nicks over the kick plate. Back-of-house: grime at the lever, grey-primer chips and hand-truck marks over the plate, knocks round the louvre's lower corners, a duller lower half. Pool: wet-palm dulling, the palm zone, the chalky bloom low and beside the plates, the calcium tide line, splash spots. The plates, the louvre and the porthole cleared. Metas from `DoorWear_BackOfHouse` (iOS / WebGL capped at 1024), fresh GUIDs | the face materials |
| `Door_Veneer_OfficeMap.mat` | `Door_Veneer_OfficeWear_L` with the map mask and rect (0, 0, 1, 1 / 2.11): walnut G unchanged, chip pale walnut | OF-F's leaf |
| `Door_Enamel_OfficeMap.mat` | `Door_Enamel` + `_FR_LEAFWEAR`, the Office map mask, chip the grey shop primer (stored sRGB-encoded as Part A's) | OF-K's leaf |
| `Door_BackOfHouse_Map.mat` | `Door_BackOfHouse_L` (B-2a, sRGB about 140, 135, 124) with the back-of-house map mask | BH-F's leaf |
| `Door_PoolGelcoat_Map.mat`, `_MapAqua.mat` | `Door_PoolGelcoat_L` with the pool map mask; the aqua takes `Door_PoolGelcoat_Frame`'s colour (0.2588, 0.6196, 0.6392) and smoothness 1.026 | PL-F, PL-K |
| `Stair_DoorSteel_Map.mat` | `Stair_DoorSteel` without the stair leaf's mask (its rect is the stair kit's, not the d16 leaf's): the landed putty | ST-F |
| `Door_CoreFoam.mat` | `Door_CoreGypsum`'s maps tinted to a closed-cell PU foam cream, fines the foam's colour | the FRP family's cassettes and debris |
| 24 stage materials `<face>_Damage_{M,D1,D2,TORN}` | each new face's settings + the landed onset block of `Door_Veneer_Damage_*` (the walnut) or `Door_Enamel_Damage_*` (the rest); the gel coats' fines the foam's colour | the break (`FrontRoomsDoorBreakVisual.StageMaterial`) |

All written by `W/zone_doors/tools/map_mats.py` from the landed materials (GUIDs in `W/zone_doors/tools/guids_materials_map.json`).

**The break, per row:** OF-F stays on the walnut veneer family (its face reads as `Door_Veneer`, its palette the walnut's: the Office wear changes no debris colour); OF-K on the steel family as L0-K; BH-F / BH-K on `Kit_DoorLeafBreak_SteelLouvre_DA` (6 blows; the grey's own palette and grey primer, the almond's the landed steel one); PL-F / PL-K on `Kit_DoorLeafBreak_FRP_DA` (6 blows; a dished, crazed gel-coat skin and a parted edge over foam; the tempered pane stays through the stages, as the wired lites do); ST-F / ST-K on the steel family, the frame break in the row's #5F5A50. The FRP frame has no frame break insert (it shows as built through the break).

## M5. Proofs (Unity sessions md1 and md2 in the clone, 2026-11-06 21:25-21:35; frames `W/zone_doors/captures/md1`, `md2`)

The clone-only probe `FrontRoomsMapDoorsProbe` (`W/proj_doorlean/Assets/Editor/Audit/MapDoors/`, never packaged) plays the main scene, starts the run itself (`Random.InitState(4242)` before `RequestTitleStart`: seed 516574485, as W/proj_doorbreak's film harness), holds the Relay, and moves the game camera to fixed poses in each door's root frame (carried lights off, put back after each render). It picks its doors from map data only, so both runs pick the same ones: an Office door seen from the Office side (OF), a door the map line's `BackOfHouseDoor` names seen from the lobby (BH), a Level 0 reference door (L0), and two Level 0 doors the NEW run dresses as PL and ST through `DoorMemberOverride` (the pool's zones exist only in scene 4's district P). `-mdToday` sets `DoorRowsToday`; `-mdPhone` the Handheld quality, the iPhone 16 Pro path and the door break's phone tier. Runs: `W/zone_doors/tools/map_run.sh` (the Unity slot protocol), 5 runs, every one rc 0 and 0 `error CS`.

- **Level 0 unchanged:** the L0 reference door's five desktop views and its phone view are pixel-identical between the NEW and the TODAY runs (max difference 0 at 1920 x 1080 and 1920 x 883): the shipped mode leaves a Level 0 door exactly as `DoorRowsToday` draws it. `DoorRowsToday` itself draws today's rows by construction: `Row` returns the landed row objects, and every new line in `Build` is a no-op for them (no face, trim, part or mask; the rose anchors exist; the shadow proxy is the same call).
- **Every member dresses** with its row (the probe's COST lines name the row, the member, the leaf and the frame), on desktop and on the phone tier.
- **The breaks run** (T1 stand-in calls, the film harness's): OF on the walnut veneer family (5 blows: M, D1, D2, FINAL, BROKEN), BH on `Kit_DoorLeafBreak_SteelLouvre_DA` and PL on `Kit_DoorLeafBreak_FRP_DA` (6 blows, steel), each ending in pose (a) with the louvre, the lite and the porthole intact through the stages.
- **T6 measured in place** (the leaf's mean linear luminance over Z 0.58-0.86, Y 0.36-0.90 on the camera's face, clear of the lite, the louvre, the porthole, the plates and the lever; the same door dressed free and locked in the same light):

| Door | TODAY: locked over free | NEW: locked over free | NEW phone | Note |
|---|---|---|---|---|
| OF (the Office's light) | +1.90 stops | +1.89 | +1.55 | the Office wear changes the patch's mean by 0 |
| BH (the lobby's light) | +1.35 (L0-K / L0-F) | **+1.01** | **+0.69** | thin margin: the warm grey is closer to the almond than walnut is; TODAY's L0 on the phone tier is +0.90 already |
| PL (Level 0's yellow light; stand-in) | +1.35 | **-0.95 (turned)** | -0.81 | the aqua is darker; by albedo 1.49 stops under the white; in a pool's white light expected near the albedo |
| ST (stand-in) | +1.35 | **+0.57** | +0.41 | fails T6 by design: the contract asks the fire stair door never be locked |

Locked doors are not in play while `doorsNeedKeys` is 0 (inventory row B2).

## M6. Renders (Shot format via `W/zone_doors/tools/map_sheets.py`; TODAY left, NEW right, the same door and pose in each row)

Staged in `research/double_doors/sheets/`, every `_fit.txt` ALL LABELS FIT, dark rows lifted alike in both columns (`ra_sheets.lift`, said on each tile):
- `map_doors_overview.jpg` (2400 x 1278, WAIT-RED): every member at 3 m, TODAY above NEW, and the four locked rows.
- `map_doors_office.jpg` (2400 x 8030, PASS): OF-F at 3 m, 3/4, 8 m, the lever hero, the foot of the leaf, OF-K; the phone tier; the break at M, D2, FINAL and at rest; COST.
- `map_doors_boh.jpg` (2400 x 7310, WAIT-RED: B-2's paint is Red's open ask): BH-F at 3 m, 3/4, the lever hero with the louvre, the louvre hero, BH-K; the phone tier; the break; COST.
- `map_doors_pool.jpg` (2400 x 8030, WAIT-RED: D-5 and T6 turned): PL-F at 3 m, 3/4, the porthole hero, the lever hero, PL-K, ST-F; the phone tier; the break; COST.

What the player sees: an Office door now carries its kick plates and reads as the title Office pair's single sibling (walnut, dark bronze, satin chrome, 630 plates); a service zone's doors read as grey steel doors with a stainless plate in walnut country (the louvre from about 1.5 m); a pool zone's door is the white FRP porthole door in an aqua moulded frame; the locked ones keep the almond key door except the pool's, which is aqua.

## M7. Cost (the runs' census of the dressed door at LOD0: frame, leaf, hardware, plates, parts; shadow-only proxies apart)

| Door | TODAY | NEW | Change |
|---|---|---|---|
| OF-F (Office) | 19,616 tris, 8 renderers, 15 submeshes | 25,516, 10, 21 | +5,900 tris (the two kick plates at LOD0, 2,950 each, inside 2.5 m; 18 tris each beyond), +2 renderers |
| BH-F (a service zone's door) | 19,262, 8, 16 (L0-F) | 23,388, 9, 17 | +4,126 tris, +1 renderer (the louvre) |
| PL-F (scene 4's pool zones) | 19,262, 8, 16 (L0-F) | 30,016, 11, 22 | +10,754 tris, +3 renderers (ring, two plates) |
| ST-F (after the contract) | 19,262, 8, 16 | 22,208, 8, 16 | +2,946 |
| OF-K / BH-K / PL-K / ST-K | 35,882, 17, 40 (L0-K) | 35,882 / 37,082 / 43,710 / 35,842 | 0 / +1,200 / +7,828 / -40 |

Per floor (the map line's census: about 76.7 back-of-house doors a floor of 9 x 9 chunks, 0.95 a chunk): +4.1 k tris at LOD0 for each back-of-house door in LOD0 range (LOD1 and LOD2 add little: the louvre 240 / 24 tris, the leaf 1,744 / 232 against Veneer_DA's 1,228 / 188; the louvre is never culled, as the leaf's cut-out is not). Textures: +3 masks, 2.7 MiB each on desktop (1024 x 2048, 8 bits a texel with mips), at most 0.7 MiB on iOS (capped at 1024: 512 x 1024); each loads with the first door of its member. Materials: +6 faces and `Door_CoreFoam`, +24 break stage materials (loaded only by a break): 31. Draws (the table's submeshes, before batching): +1 a back-of-house door (the louvre), +6 an Office door (the two plates' three slots, inside 2.5 m), +6 a pool door; the locked rows +0 / +1 / +6 / -6. Frame time: not measured (batch renders are not timed; rule 25).

## M8. Where the build differs from the plan, and the honest flaws

1. **A seventh member, Stair** (the agreement of 2026-11-05 21:57 named Pool and BackOfHouse): the task's PL_FireStair row needs a member of its own, since rows are indexed by member. It is appended after BackOfHouse (rows 12-13), so every agreed index holds, and only the map's word gives it (§M10). The museum contract therefore cycles the enum's length (7), not the "% 6" the agreement named.
2. **The map leaves carry their own masks** (`DoorWear_*Map`), not the title pairs' (`DoorWear_Office`, `_BackOfHouse`, `_Pool`): those are laid out on the pair's leaves; the map leaf is 0.99 x 2.11 m with metre UVs mirrored between its faces (§M1, `MirrorLeafWear`).
3. **A kick plate of the map's own** (`Kit_DoorKickPlate_Map`, 0.94 m): HW-DA1's plate is cut to the pair leaf's width. The louvre and the ring are the title pairs' kits, scaled 0.98876 through the leaf (1-3/4 in against the map's 44 mm): 1.1 % thinner, not visible.
4. **The louvre is faint at 3 m on desktop:** in the lobby's light the blades read as a slightly lighter panel (mean linear luminance 0.0204 against the leaf's 0.0192 in the 3 m frame). The blades read from about 1.5 m, in the heroes and on the phone tier (the Handheld SSAO darkens the slots). The Surface shader reads no vertex colour, so there is no cheap AO; a baked cavity in the blades' map is the next step if Red wants it at 3 m.
5. **The Office wear is subtle in the Office's light:** the T6 patch's mean is unchanged by it (§M5); the hand halo and the chair-arm band read at 0.4-1 m (the lever hero), not at 3 m. The kick plates read at 3 m.
6. **PL and ST are shown on Level 0 doors** (`DoorMemberOverride`, said on every tile): the main scene has no pool plan (scene 4's district P only) and the Stair member waits for the contract. The pool door is therefore seen in Level 0's yellow light, not in the pool's white light.
7. **T6:** BH holds by +1.01 stops on desktop and +0.69 on the phone (thin; TODAY's L0 rows are +0.90 on the phone already). PL is turned on purpose (the locked aqua 0.95 stop darker in Level 0's light, 1.49 by albedo; no locked door can be brighter than the white gel coat): Red's ask. ST-K fails (+0.57): the contract asks the fire stair door never be locked.
8. **The FRP break rides the steel cassettes:** their arrises are 1.5 mm against the FRP shell's 3 mm, a small step at the window's edge seen only at the break's close range; the FRP frame has no frame-break insert (it stays as built through the break). BH-F's bored latch faceplate shows as the steel family's mortise armor front once the break starts (the cassettes carry it).
9. **The phone runs pick other doors than the desktop runs** (the handheld start path places the run elsewhere); both phone runs pick the same doors, so every phone row is still TODAY | NEW at one door.
10. **Not done:** frame time and draw calls in the game (MD9); the probe's 8 m view is not clamped to the clearance (BH's 8 m pose stood behind a wall 5.9 m out, so that row is off the sheet); 平面视觉's floor tags E-G for the museum stand.

## M9. The package and the gate (2026-11-06 21:39-21:51)

- **`W/apply/zone_map_doors`** (`W/tools/mkapply.py`, overlay `W/zone_doors/pkg_overlay_m` = the clone's 118 files, list `W/zone_doors/tools/map_payload_paths.txt`): **118 paths**. 5 replaced: `FrontRoomsInteractableKit.Door.cs` 0dbdc107, `FrontRoomsDoorRig.cs` 44a78529, `FrontRoomsDoorLite.cs` 3eccaa77, `FrontRoomsDoorBreakVisual.cs` db66bc4e, `FrontRoomsDoorBreakVfx.cs` d3382c34 (their `.meta`s kept). 113 new: the 9 kits' `.fbx`, `.json` and both `.meta`s; the 3 masks with `.meta`; the 31 materials with `.meta`; 9 tools (the 7 modules in `frontrooms_kit/assets/`, `door_break_map_bases.py`, `Tools/lookdev/gen_door_wear_map.py`). 52 new GUIDs, none used in main's 4,179 Assets `.meta`s.
- **Write order** (`zone_map_doors_expected.txt`, edited as Packages A, B and S; the set unchanged, mkapply's order saved in `W/zone_doors/tools/zone_map_doors_expected.mkapply_order.txt`): the 52 `.meta`s, the 3 masks, the 31 materials, each kit's `.json` before its `.fbx` (the importer reads the sidecar at import), the 9 tools, the 5 C#.
- **Main's API read** (the package's `--api` rows, checked at apply): the map line's `FrontRoomsMapWorld.BackOfHouseDoor` (`FrontRoomsMapWorld.BackOfHouse.cs` 26846a31) and `PoolRules.IsPool`.
- **Checks:** DRY RUN OK against main (`W/zone_doors/logs/zone_map_doors_dryrun_main.txt`); rehearsal on a scratch copy of main (`W/zone_doors/rehearsal_zmd`: main's 4,179 Assets `.meta`s, none dataless, the 7 code files, ProjectVersion): DRY RUN OK, `--apply` MANIFEST OK (118 paths, 5 kept unchanged), again ALREADY APPLIED, all 118 payload files matching (`logs/zone_map_doors_rehearsal.txt`).
- **Gate (rule 25):** Roslyn on main's tree as it stands + the payload's 5 C# (zsh array; both `.rsp` list the 5 payload paths): macOS COMPILE OK 0 errors (Assembly-CSharp 285 files, Editor 89), iOS COMPILE OK 0 errors (`logs/zone_map_doors_gate.txt`). With both contracts' proposed files too (`tools/gateMK.zsh`, 8 overrides, 21:51): macOS and iOS COMPILE OK 0 errors; both contract diffs `patch --dry-run` clean on main's bases, unchanged at 21:51 (`logs/zone_map_doors_contracts_check.txt`). Unity 0 `error CS` in the clone's 5 runs. No shader.
- **No DEPENDS ON:** the map line's predicate (in main since 10:23), Parts A and B and the stairwell doors are in main; the 5 bases are main's at 21:51.
- **Lands live** (rule 25): `DoorRowsToday` is false, so the back-of-house member (about a third of today's Lobby doors: the map line's census, 1,534 of 4,431 = 34.6 %, about 0.95 a chunk) and the Office rows show at once; the Pool member wherever a pool plan exists; the Stair member after the map contract. `DoorRowsToday = true` is today's rule and rows. Render-only: no collider, swing, lock rule, name, pivot or sound event changes.
- **Docs that land with it** (staged in `W/zone_doors/to_main/Frontrooms3D/Documentation/research/`, snapshot in `W/zone_doors/pkgM/docs_ready/` with `MANIFEST.txt`): this file (Part M appended), `door_inventory/10_inventory.md` (rows B3-B7, G1, H4, H5, H8, §0, §2, §3, §4.3, §4.4), `sheets/map_doors_{overview,office,boh,pool}.jpg` with their `_fit.txt`.
- **Combined-test rows MD1-MD9:** `W/vl_pending/zone_map_doors.md` (Level 0 unchanged, back-of-house doors, Office doors, the break, locked rows, phone and pad, scene 4's pool route, the museum stand, cost).

## M10. Contracts and open items

- **关卡设计 (via 总控):** `W/apply/zone_map_doors_contract` (README, `CONTRACT_map.diff`, `base/`, `proposed/`): a pure predicate `IsPoolFireStairDoor(Door)` in `FrontRoomsMapWorld.Pool.cs` (24dd1f7a) and M8 answering Stair for that door in `FrontRoomsMapWorld.cs` (8a1612df); after our package; inert outside a pool plan. Asks: may a fire stair door ever be locked (we suggest `LockedByRule` leave it free; else ST-K shows and T6 fails); do later "doors into a stair" join the same answer.
- **交互设计:** `W/apply/zone_map_doors_museum_contract` (`CONTRACT_museum.diff`, `FrontRoomsInteractablesShowcase.Museum.cs` ce55826e): the 1.7 stand cycles the enum's 7 members (A-G: Lobby, Office, Run!, Exit, Pool, Back of house, Stair); the floor tags E-G wait for 平面视觉 · 信息设计's type.
- **Sound (for Red; 声音设计 paused):** `W/zone_doors/contracts/sound.md` "Map doors" (English + 中文): the back-of-house doors (and scene 4's pool doors) blow and crack with the existing steel takes, because `DoorType` follows the break family's Steel flag; (a) keep, REC, as built; (b) wooden takes only through 声音设计's paused file. Nothing waits on it.
- **Red's asks** (the sheets' chips): B-2's paint for the back-of-house leaf (a warm grey, REC and built; b cool grey; c green-grey; one answer covers the title LOW pair and the map member); the pool member at PD-A with T6 turned (the alternative: a grey free leaf so a white locked one can read brighter); D-5 itself (PD-A built; PD-B / PD-C as Part B); whether the louvre must read at 3 m on desktop (§M8.4).
- **Disk:** this stage's captures (`W/zone_doors/captures/md1`, `md2`) and the clone-only probe (`W/proj_doorlean/Assets/Editor/Audit/MapDoors/`, never packaged) stay outside main.

---

# Part R · The Run! polish: the side doors' frame, the compartment pairs' closers (inventory rows C2, C3)

- **Line and stage:** 游戏视觉, account G, the zone doors refinement's Run! polish stage (workflow wf_afdae4db-b34, parent session local_1fc9f09c), 2026-11-06 22:0x on; journal `W/zone_doors/journal.txt`, RUN lines.
- **Red's asks** (this line's chat, plain user turns, verbatim): 2026-11-05 19:17:06 "同时我需要游戏视觉显根据当年做调研，去翻新每一个区域的双开门模型，规格和结构上的要求和之前一样"; 19:35:21 "目前level 0的门不需要改变，我是需要你改变办公室，车站，后场，run！，泳池等的门"; 19:39:25 "…现在还剩下那些门没有细化…然后做细化和模型升级的调研和模型重做等工作。不要推翻之前已经做了的工作". The inventory (`R/door_inventory/10_inventory.md` §3.4, §4.3 item 7) leaves two Run! items: C2, a frame built for the side doors' 0.16 m wall; C3, closers on the held-open compartment leaves.
- **Status:** PACKAGED 2026-11-06 22:4x, not landed: built in the clone `W/proj_doorlean`; `W/apply/run_door_polish` DRY RUN OK on main, rehearsal MANIFEST OK, Roslyn macOS + iOS 0 errors (§R9). Nothing in main, no git, no download, silent (`m_DisableAudio` 1). No new Red decision is asked; one later option is recorded (§R10).
- **Reused, not redone** (inventory §2): the Run! B door set (RN-F, the door tell, the RN-K side doors with their plates, the compartment frames at 115 deg on magnets: the pose is kept), the RN-K row's leaf `Kit_DoorLeaf_SteelRun_DA` and its locked trim, the F4 frame research (door_frame 10: SDI's 2 in face, 7/16 in return), the DA pair's language (black brush strips in the gaps), the closer kits with their LOD2 (Part S, §S2.3), the stairwell's regular-arm solve (`FrontRoomsStairwellDresser`), the leaf-wear layer `_FR_LEAFWEAR` and the map rows' kit-leaf mask layout (Part M), the research `10_research.md` R.4 / R.5 and its sources Z1-Z3. No web research was needed.
- **Not done here, by decision:** the goal door RN-F (no model change; its phone-tier read is a light question, §R10); the T4 accordion and the D4 changed door (Red declined them, inventory row C4).

## R0. What was built, in ten lines

1. **The side doors' frame** (`Kit_DoorFrame_Steel_DA_Wall`): the F4 16 ga sleeve cut to the corridor face, at its true section (the 50.8 mm face 11.1 mm proud, the 7/16 in return into the block, the soffit back to the wall face), mitred, welded and ground; it replaces the both-faces `Kit_DoorFrame_Steel_DA` squeezed x0.92 into the wall.
2. **The gaps the prop showed as bare wall** now read as a door's: black brush strips on the wall face behind the hinge, latch and head gaps, a black backer behind the 15 mm undercut (no saddle: a service door over VCT), and the RN-K row's mortise strike on the latch soffit, whose corridor lip shows in the 9.1 mm return.
3. **The frame and the leaf share one root:** the kit is authored in the prop leaf's own root, so the dress spawns both at one point; the leaf keeps its place (its corridor face 2 mm proud of the wall).
4. **The compartment pairs' closers:** each held-open leaf of FD1 / FD2 carries the landed regular-arm closer (body, arm, forearm, shoe) on its pull face, the shoe on the frame's head casing, the arm solved once for the held pose.
5. **The armature** (`Kit_RunMagnetArmature`): a swivel disc on a stud and door plate, on each leaf's pull face where the wall magnet meets it; the holder moves 0.12 m nearer the frame line so the two meet (today the leaf's face ran 16 mm into the magnet).
6. **The closers' paint** matches the leaves' 630 hardware (the 1992 spec: "spray painted to match door hardware"): the kick plates' satin stainless.
7. **The Run! wear on the almond props** (`Door_RunAlmond_Map`, mask `DoorWear_RunMap`): the knob's hand marks, cart rubber and primer chips over the kick plate, the mop line: the C-1 pair's wear language on the kit leaf, on the side doors and the compartment leaves.
8. **One switch:** `FrontRoomsRunDress.DoorsToday` (true: today's dress, byte for byte; WebGL players always true, rule 5); without the new kits in the library each item falls back to today's.
9. **Where it shows:** the title's RUN! room (Tour, room 4) has the ELECTRICAL ROOM side door: live at landing; the map's Run! rooms and the compartment frames are inert (runRooms false, frames only in a receding or framed layout, rule 5b).
10. **No sound, no collider, no name a peer reads changes:** every new part is render-only; the props never open; the side door's frame keeps its name "run b side door frame" (RunRigTests R10).

## R1. Design: what a 1990 service door and a held-open fire pair show here

### R1.1 The side door (RN-K prop)

The prop stands on a solid wall: no opening behind it, so its DA leaf (44.5 mm) sits with its corridor face 2 mm proud of the wall and its back half in the wall. A real DA leaf centred in a 0.16 m wall sits 58 mm behind the wall face; the frame's soffit then shows 69 mm deep behind the casing. Without a pocket in the wall the reveal can only be the casing's 11.1 mm less the leaf's 2 mm: 9.1 mm (research R.4's honest flaw, unchanged by the kit). What the kit corrects is what the squeeze distorted and what the gaps showed:

| Part | TODAY (Steel_DA x0.92) | NEW (Steel_DA_Wall) |
|---|---|---|
| Casing | 10.2 mm proud, the section 8 % thin, the far casing buried in the wall | 50.8 x 11.1 mm, the SDI cased-open section, its return 0.5 mm into the wall |
| Soffit | the both-faces sleeve, 73.6 mm into the wall at its centre | the corridor half only: from the casing to 4 mm into the wall |
| Hinge, latch, head gaps | the wall's paint 2 mm behind the leaf (a light line) | black brush strips on the wall face (the DA pair's language); the radiused DA stile turns over them |
| Foot | a lit strip 15 mm high under the leaf (the buried saddle never showed) | the undercut's black backer: the room behind is dark |
| Strike | buried with the frame's strike box | `Kit_Lock_StrikeMortise_DA` on the latch soffit, 0.3 mm proud; its corridor lip shows in the return |
| Saddle, pivot caps | buried in the wall | none (a service door over VCT is undercut; the pivot sits on the leaf's buried centre plane) |

### R1.2 The compartment pairs (FD1 / FD2)

- **Why closers:** a held-open fire door shuts by its closer when the magnet lets go (US 3,204,154, research Z2); the 1992 spec's "Make labeled doors self-closing" (Z1 §2.6). The research chose the stair door's regular arm on the pull face (R.5).
- **The pose is kept** (DO NOT REDO: 115 deg on magnets). At 115 deg the pull face looks at the wall and a little back toward the frame; the regular arm's body sits on it at the top rail, the shoe on the head casing's goal face. The frame hangs the leaves on butt knuckles 4 mm proud of that face, so the shoe stands only 16 mm in front of the shut leaf's centre plane, where a frame centred in the wall gives 125 mm (the kits' door root). The geometry was solved for the held pose (scratchpad `closer_geom.py`, then in Unity): with the spindle 0.35 m from the knuckle (the stairwell's 0.52 cannot reach: 0.57 m > a + b) and the shoe 0.10 m in, the elbow opens 110 deg, the main arm stays on the pull side, and the forearm crosses over the leaf's top by the hinge 0.116 m from the elbow, where its gooseneck has already lifted the rod's underside to 2.112 m (17 mm over the 2.095 top).
- **The armature:** the 1989 holder's half on the door (US 4,852,919, Z3): a 64 mm plated disc on a swivel ball, a 10 mm stud with a hex boss, a 44 mm door plate with two oval heads, a neoprene grommet; the disc modelled at the hold-open's 25 deg so its face lies flat on the wall magnet. Today's holder centre (z +0.86 from the frame line) put the leaf's face 16 mm inside the magnet; at z +0.7438 the disc's face meets the coil cap 0.3 mm clear (scratchpad `holder_geom.py`: the leaf face 105 mm off the wall there, the holder 68.8 mm, the armature 32.7 mm + 3 mm of tilt).
- **Where it shows:** the closer bodies and the armatures stand in the wedge the frame's stub, the wall and the leaf close on three sides. From the corridor the runner sees, from the goal side, the shoes on the head casing and the forearms over the leaves' tops; the bodies only from the wedge (the hero rows). A real held-open pair reads the same way.
- **Finish:** "spray painted to match door hardware": the leaves' 630 kick plates' satin stainless (the first build's bare aluminium slot reflected the dark soffit as near black in the lab).

### R1.3 The wear

The C-1 pair (Part A) wears `DoorWear_Run`; the almond props beside it were clean. `DoorWear_RunMap` lays the same language on the kit leaf (u = Z / 1.0, v = Y / 2.11, the map rows' layout, Part M): a locked door is tried, not pushed, so a small patchy halo round the knob and knuckle marks at the latch stile; cart and hand-truck rubber and grey-primer chips over the kick plate, more toward the latch half; the mop line at the foot; a duller lower half. Face P reads the mask mirrored (wrapU Mirror, as `DoorParts.MirrorLeafWear` does for the map rows; set by the dress, the map file's helper being private).

## R2. Kits (Blender 4.3 silent, `Tools/Blender/frontrooms_kit/build_asset.py`, hand-built LODs `fr_lods`)

Modules (new, in `Tools/Blender/frontrooms_kit/assets/`): `door_frame_steel_da_wall.py`, `run_magnet_armature.py`. Built to `W/zone_doors/tmp_kits/run`, copied into C; the `.meta`s by Unity at import (fresh GUIDs). Log `W/zone_doors/logs/blender_kits_run.log`.

| Kit | LOD0 / LOD1 / LOD2 tris | Slots | Switch (m, lodBias 1) | What |
|---|---|---|---|---|
| `Kit_DoorFrame_Steel_DA_Wall` | 204 / 140 / 108 | Prop_SteelBrown, Prop_PlasticBlack | 4 / 12 / never | the sleeve (3-step 3 mm convex bends, 16 ga) and the four black strips; authored in the prop leaf's root, the wall face at X 0.020 |
| `Kit_RunMagnetArmature` | 1,092 / 236 / 76 | Prop_Chrome (turned satin by the dress), Prop_Rubber | 2 / 7 / 20 | door plate, two oval heads, hex boss, stud, swivel ball, grommet, the 64 mm disc at 25 deg with its chamfers and rivet counterbore |
| reused: `Kit_DoorCloser_Body` / `_Arm` / `_Forearm` / `_Shoe` | 3,236 / 1,067 / 40; 1,112 / 1,112 / 32; 940 / 940 / 80; 922 / 922 / 12 | Prop_SteelBrown (turned 630), Prop_Chrome (turned satin) | 1.5 / 5 / 20 | the landed regular arm with Part S's LOD2 |
| reused: `Kit_Lock_StrikeMortise_DA` | 792 / 326 / 32 | Prop_Chrome (turned satin), Prop_PlasticBlack | 1.5 / 4 / 12 | the RN-K row's strike |

The frame's LOD0 is low for a hero kit because a pressed-steel sleeve is flat: its detail is the bends (four segments on each 3 mm bend at LOD0, one at LOD1, sharp at LOD2), and the strips stay in every level (a far door keeps its dark gaps). **Asserts** (every build passes): the casing face at X 0.0311 at every level; the face band 50.8 mm on both jambs and the head; nothing of the sleeve inside the shut DA leaf (faces +-0.022, the stile's arc about z 0.036, the flat end at 0.995); the strips behind the leaf's corridor face and in front of the wall face. The armature asserts nothing behind the leaf face but its plate's 0.2 mm and the disc's contact 32.7 mm out.

## R3. Materials and the wear mask

| File | What |
|---|---|
| `Resources/Surfaces/Door_RunAlmond_Map.mat` | `Prop_SteelAlmond` (its maps and values unchanged) + `_FR_LEAFWEAR` with `DoorWear_RunMap`, rect (0, 0, 1, 1 / 2.11), chip = the grey shop primer stored sRGB (0.4236, 0.4236, 0.4169), as Part A and Part M. Worn by the side doors' and the compartment leaves' almond slot under the new look |
| `Resources/Surfaces/Textures/DoorWear_RunMap.png` | 1024 x 2048 RGBA linear (R grime, G dull, B burnish, A chip), from the new `Tools/lookdev/gen_door_wear_run_map.py` (numpy + Pillow, seed 61121; it imports `gen_door_wear_map.py`'s layout and helpers and never edits it). Means R 0.014, G 0.017, B 0.002, A 0.000 (sparse chips). Meta from `DoorWear_BackOfHouseMap` (linear, BC7, mips; iOS / WebGL at 1024), fresh GUID |

No new shader, no existing material touched. The closers' paint and the strike, the armature and the frame use landed materials (`Prop_StainlessSatin`, `Prop_ChromeSatin`, `Prop_SteelBrown`, `Prop_PlasticBlack`, `Prop_Rubber`).

## R4. The code (`Assets/Scripts/Rendering/FrontRoomsRunDress.cs`, ours, main b94ade20)

- **`DoorsToday`** (static; `#if UNITY_WEBGL && !UNITY_EDITOR` always true): read while a dress is built, as `FrontRoomsDoubleDoorKit.Today`.
- **`SideDoors`:** with the wall frame kit in the library and not `DoorsToday`, the frame is `Kit_DoorFrame_Steel_DA_Wall` at the leaf's own point and yaw (no squeeze), with `Kit_Lock_StrikeMortise_DA` under it at (0, 0.968, 0.9977) turned as the map's strike, in satin chrome; the leaf's almond slot takes `Door_RunAlmond_Map`. Else today's lines, unchanged.
- **`CompartmentFrames`:** with every closer kit and the armature in the library and not `DoorsToday`, `HoldOpenHardware` on each leaf: a root under the leaf scaled (pull, 1, 1) (the right leaf's pull face is the kit's +X, the left one's -X, as the stairwell hangs its closers), the body at (0.022, 2.0275, 0.375) turned (0, 90, 0), the arm and the forearm at the spindle, the shoe under a frame-fixed root at the knuckle in the shut leaf's yaw at (0.016, 2.13, 0.10), `SolveCloser` (the stairwell's two circles, arm 0.24, forearm 0.26, the root farther from the leaf; the forearm on the arm's `elbow_top`, 12 mm up, per the forearm kit's own frame), the armature at (0.022, 1.95, 0.7401); shadows off, the paint and the chrome turned; the holder at z +0.7438. Else today's lines.
- **`WornAlmondMaterial`** loads `Door_RunAlmond_Map` and sets its mask's wrapU to Mirror once.
- **Unchanged:** every name the rig, the tests and the tools read (`run b side door frame` / `leaf` / `escutcheon` …, `run b compartment frame` / `leaf` / `magnet`); the Today path line for line; colliders (none); the rig's lights, probes and gate (`FrontRoomsRunRig` untouched).
- **Gate:** Roslyn on main with this one file (`W/zone_doors/tools/gateR.zsh`, then `pkgR_gate.zsh` on the payload): macOS COMPILE OK 0 errors, iOS COMPILE OK 0 errors, each `.rsp` listing the override once.

## R5. Proofs (Unity sessions rd1 and rd2 in the clone, 2026-11-06 22:32-22:37; frames `W/zone_doors/captures/rd1`, `rd2`)

**How.** (1) The clone-only lab `Assets/Editor/Audit/RunDoors/FrontRoomsRunDoorsLab.cs` (edit mode, never packaged): `RunRigLayout.Corridor(9, 1.4, 1, frames: true)` (the three side doors at z 5 R / 17 L / 23 R, FD1 at z 9, FD2 at z 18) in the look-dev shell with today's dress and rig (`RunRigHost.OwnLamps`), built with `DoorsToday` true (TODAY) and false (NEW), then both with the rig's handheld lights (`ForceHandheldForTools`); the game's film look (`FrontRoomsPostStack`), 1920 x 1080, vFOV 76, MSAA 4; armed, then tripped 2 s on the rig's clock; hero rows with a soft fill riding the camera in both columns. (2) The title: `FrontRoomsTitleCapture` pose set in the Tour's RUN! room (room 4, door 3's space), `-runDoorsToday on|off` through the clone-only `FrontRoomsTitleProgrammeArgs`, desktop Ultra and the phone path (Handheld + the iPhone 16 Pro path). Scripts `W/zone_doors/tools/rd_run.sh`, specs `rd1_spec.txt`, `rd2_spec.txt`, poses `poses_run/`; every run rc 0, 0 `error CS`.

- **A finding about the lab, fixed:** rd1 built TODAY and NEW 200 m apart; the VCT, the block and the macro maps are world-projected, so every texel of the two rooms differed (floor means 9-18/255 apart). rd2 builds one room at a time in the same place, shoots it and destroys it: TODAY and NEW now differ only at the doors (`fd_eye` 133 px over 6/255, `fd_q34` 475, `sd_eye` 5,042, all inside the doors' boxes).
- **Today is today:** with `DoorsToday` true every changed line takes main's branch (the frame squeezed, the almond, the holder at +0.86, no hold-open hardware); the TODAY room's census lists today's parts and materials only. Not compared with a run of main itself: the dress is the package's only code file and its diff is the proof.
- **The title:** TODAY and NEW differ only inside the side door's bounding box in all seven poses (the eye pose 3,576 px over 6/255; the off state 867).
- **The solve, logged** (rd2 `lab_log.txt`): at all four leaves the arm sits at the spindle (0.062, 2.068, 0.35) yawed 76.3 deg, the forearm on the elbow (0.1187, 2.080, 0.1168) yawed 146.2 deg (the scratchpad solve to 1 mm); the armatures at (+-1.3207, 1.95, z + 0.7565), the holders at z + 0.7438.
- **The rig's tests** (`FrontRoomsRunRigTests`): untouched by the package; R10 (3 frames named "run b side door frame", 3 leaves, every lite dead, no closer in the plain corridor, every frame renderer inside the wall's far face) holds by construction (the names are kept; Corridor1x9 has no frames; the new frame reaches 23 mm into the wall). Not run (rule 25: the combined test runs them, row RD5).

## R6. Renders (Shot format via `W/zone_doors/tools/run_sheets.py`; TODAY left, NEW REC right, the same frame in each row)

- **`sheets/run_door_polish_side.jpg`** (2400 x 11112, `_fit.txt` ALL LABELS FIT), "Run! side doors · a frame built for the wall". In the title: 3/4 at 3 m, grazing at 2 m, heroes at the latch jamb (the strike's lip), the foot (the backer), the knob (the wear), the off state; the lab: square on, heroes at the hinge jamb (the strip under the stile's arc) and the head corner, STAFF ONLY and NOT AN EXIT at 3 m; the phone rows (title and lab); tripped; COST.
- **`sheets/run_door_polish_compartment.jpg`** (2400 x 9672, ALL LABELS FIT), "Run! compartment pairs · closers and armatures". FD1 at 3 m, looking back from 3.3 m (the arms over the leaves' tops), along each wall (the stub hides the wedge), under the closer; heroes: the closer, the magnet and armature (TODAY: the leaf through the magnet), the shoe and the forearm from the goal side; phone; tripped; COST.
- Hero rows: the lab's carry the camera fill (no display lift); the title's are lifted +2 EV in both columns (the title has no fill); dark rows +1.5 EV; said on each tile. Raw frames: `W/zone_doors/captures/rd2/lab/` (TODAY, NEW, phones), `rd1/title_{today,new}[_phone]/`; rd1's lab frames are superseded (the 200 m apart rooms).
- **What the sheets show** (looked at row by row): at eye height the side door's change is small by nature (a 0.9 mm thicker casing at its true profile): it lives in the gaps (dark where the wall showed), the foot and the strike's lip, and in the leaf's wear; the compartment pair's change shows from the goal side as the shoes and forearms and in the heroes as a period hold-open: closer, arm, armature on the magnet.

## R7. Cost (the lab's census at LOD0; the kits' sidecars)

| Group | TODAY tris / renderers / draws | NEW | Change |
|---|---|---|---|
| the 3 side doors (frame, leaf, trim, plate or stencil) | 61,620 / 21 / 50 | 58,518 / 24 / 53 | **-3,102** (a door: the frame 2,030 -> 204, the strike +792), +1 renderer a door |
| the 2 compartment lines (frame, 4 leaves, 4 holders) | 30,312 / 10 / 36 | 59,520 / 30 / 72 | **+29,208** = 4 x (closer 6,210 + armature 1,092), +5 renderers a leaf |

- **At distance** the closers draw LOD1 past 1.5 m (4,041 a leaf) and LOD2 past 5 m (164), culled at 20 m; the armature LOD1 past 2 m (236), LOD2 past 7 m (76); the strike LOD2 past 4 m (32), culled at 12 m. From FD1's approach (3 m and more) the four closers draw 4 x 4,041 at most, 4 x 164 beyond 5 m.
- **Texture +1:** `DoorWear_RunMap` 1024 x 2048, about 2.7 MiB on desktop with mips, 0.7 MiB on phones; like everything under `Resources` it ships in every build. **Material +1:** `Door_RunAlmond_Map`. **Shader:** none.
- **Where the cost lands in today's game:** the title's RUN! room only (its one side door: -1,034 tris, +1 renderer); the compartment lines are in no shipped layout (rule 5b).
- **Not measured:** draw calls after batching and frame time (rule 25: batch renders are not timed; said on the COST rows).

## R8. Where the build differs from the research, and the honest flaws

1. **The frame's tris: 204, not the research's ≈ 1,100 [E]:** the one-face sleeve has no saddle, caps, strike box or far casing to carry; its section keeps the true bends. The strike is a separate landed kit (+792, culled at 12 m).
2. **No saddle on the side doors** (the research's frame was "cut from" the DA frame, saddle included): the prop's leaf stands on the wall face, so a DA saddle would stick out 56 mm into the corridor past an 11 mm casing; a 1990 service door over VCT is undercut instead, and the backer shows the dark room behind.
3. **The reveal stays 9.1 mm** (a real leaf sits 69 mm back): a true recess needs a pocket in the wall: ours in the title room's block slab (`FrontRoomsTitleVignettes`), a 关卡设计 contract in the map's Run! rooms. Recorded for Red after SP7 (§R10), not built.
4. **The closers' spindle sits 0.35 m from the knuckle, not the stairwell's 0.52:** at 115 deg a 0.52 spindle is 0.57 m from the shoe, beyond the arm's 0.50 reach. The shoe stands 16 mm in front of the shut leaf's plane, because the frame hangs its leaves on knuckles in front of its casing (the landed kit); at the held pose the geometry is sound (§R1.2), shut it would not be (the leaves never shut).
5. **The closers face the walls:** from the corridor only the shoes and the forearms over the leaves' tops show (from the goal side); the bodies and the armatures read in the wedge only. A real held-open pair reads the same; said on the sheet.
6. **The leaves' latch corners still rest 8.4 mm into the block** (today's 115 deg pose, kept): the leaf now meets the magnet through its armature and the wall at its corner at once.
7. **The leaves stay double-acting kits** standing in for a rated single-acting pair (research R.5's flaw, unchanged): the bull-nosed stile shows within about 2 m.
8. **The closers' paint changed in the build:** bare `Prop_Aluminium` (the research's [E]) reflected the dark soffit as near black in the lab; the 630 satin stainless of the leaves' kick plates reads as the sprayed finish and still "matches door hardware".
9. **The forearm sits on the arm's `elbow_top` (12 mm up)**, per the forearm kit's own frame; the stairwell's dresser seats it at the spindle's height (`SolveCloser`: `c.spindle.y`, so by the code its shoe end stands 12 mm under the shoe's pivot; not looked at here). Not changed here (Part S's file); noted for the next stairwell touch.
10. **The lab's first session (rd1) compared rooms 200 m apart** (world-projected maps differ): superseded by rd2 (§R5); the title captures were deterministic from the start.

## R9. The package and the gate (2026-11-06 22:4x)

- **`W/apply/run_door_polish`** (`W/tools/mkapply.py`, overlay `W/zone_doors/pkg_overlay_r` = the clone, list `W/zone_doors/tools/run_payload_paths.txt`): **16 paths**, 1 replaced (`Assets/Scripts/Rendering/FrontRoomsRunDress.cs` b94ade20 -> 0b16d231), 15 new (`Kit_DoorFrame_Steel_DA_Wall` and `Kit_RunMagnetArmature`: `.fbx`, `.json` and both `.meta`s; `Door_RunAlmond_Map.mat` + `.meta`; `Textures/DoorWear_RunMap.png` + `.meta`; `door_frame_steel_da_wall.py`, `run_magnet_armature.py`; `Tools/lookdev/gen_door_wear_run_map.py`); **1 `.meta` kept** (the dress's); 6 new GUIDs, none used in main's 4,231 Assets metas. API rows: `Kit_DoorCloser_Body.json` carries `"trianglesLod2"` (Part S, landed 20:40) and `Door.cs` carries `LeafPlateFrom` / `LeafPlateTo`.
- **Write order** (`run_door_polish_expected.txt`, edited as the earlier packages; the set unchanged, mkapply's order saved in `W/zone_doors/tools/run_door_polish_expected.mkapply_order.txt`): the 6 `.meta`s, the mask, the material, each kit's `.json` before its `.fbx` (`FrontRoomsKitImporter` reads the sidecar at import), the 3 tools, the C#.
- **Checks:** DRY RUN OK against main (`W/zone_doors/logs/run_door_polish_dryrun_main.txt`); rehearsal on a scratch copy of main (its 4,229 Assets `.meta`s, none dataless, the base, the two API files, ProjectVersion; `W/zone_doors/rehearsal_run`): DRY RUN OK, `--apply` MANIFEST OK (16 paths, 1 kept unchanged), again ALREADY APPLIED, the end state = the overlay on all 16 paths (`logs/run_door_polish_rehearsal.txt`). **Gate:** Roslyn on main's tree as it stands + the payload's 1 C# (`W/zone_doors/tools/pkgR_gate.zsh`, zsh array; both `.rsp` list the payload path once): macOS COMPILE OK 0 errors (Assembly-CSharp 285 files, Editor 89), iOS COMPILE OK 0 errors (`logs/run_door_polish_gate.txt`). Unity 0 `error CS` in the clone's runs (rd1: 5, rd2: 1).
- **No DEPENDS ON:** the closer kits' LOD2 (Part S) and the map rows (Part M) are in main; the package shares no file with them.
- **Lands live:** `DoorsToday` is false, so the title's RUN! room shows the new side door at once (rule 25); the compartment hardware waits for a framed layout. Render-only: no map, input or sound file, no collider, name or pivot changes, so no 关卡设计, 交互设计 or 声音设计 contract.
- **Docs that land with it** (staged in `W/zone_doors/to_main/Frontrooms3D/Documentation/research/`, snapshot in `W/zone_doors/pkgR/docs_ready/` with `MANIFEST.txt`): this file (head + Part R), `door_inventory/10_inventory.md` (rows C1-C3, §0, §2, §3.4, §4.3, §4.4), `sheets/run_door_polish_side.jpg` and `run_door_polish_compartment.jpg` with their `_fit.txt`.
- **Combined-test rows RD1-RD7:** `W/vl_pending/run_door_polish.md` (the title's side door, Today, a framed corridor, distances, the rig's tests, phone and pad, silence).

## R10. Open items

- **Sound:** none. The side doors never open and the compartment leaves never move; no event, take or parameter changes (声音设计 stays paused; nothing for Red).
- **For Red, later (not asked now):** a true recess for the side doors (the leaf 58 mm into the wall, the steel soffit showing 69 mm deep): ours in the title's RUN! room (a pocket in `FrontRoomsTitleVignettes`' block slab), a 关卡设计 contract in the map's Run! rooms; worth it only after Red walks SP7 (research R.4).
- **The goal door RN-F:** no model change (inventory row C1); its phone-tier leaf reads dark in every Run! look (Y .043 vs .25, HV.26) and its 5 m read is PARTIAL (HV.23): a light question for the Run! light work, not a door model.
- **The map's RN-K row** wears the clean almond (`Door_Enamel` as today); when Run! enters the game, the row can take `Door_RunAlmond_Map` (one `Face(...)` in `FrontRoomsInteractableKit.Door.cs`'s rows, ours) so the map's locked Run! doors match the props: not done here (no map row in this package's scope; Part M owns the rows).
- **T4 accordion doors and the D4 changed door:** Red declined (row C4); untouched.
- **Disk:** the captures are PNG frames, about 290 MB (`W/zone_doors/captures/rd1` 174 MB, `rd2` 113 MB; rd1's lab frames are superseded and a deletion candidate for Red); the clone-only lab (`W/proj_doorlean/Assets/Editor/Audit/RunDoors/`) and the `-runDoorsToday` switch (`Assets/Scripts/CloneOnly/FrontRoomsTitleProgrammeArgs.cs`) stay outside main; the scratch `W/zone_doors/rehearsal_run` is a deletion candidate for Red.
