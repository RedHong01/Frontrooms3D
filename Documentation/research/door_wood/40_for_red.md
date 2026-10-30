# Door wood: four texture options for Red

*游戏视觉 (account A, session local_5656cffd), 2026-10-30. This is for viewing only. Nothing here changes the game, and nothing lands until you pick. The options were built and rendered in the clone `W/proj_doorwood`. Door geometry is not touched; only the textures and materials change.*

Your ask (10-30, 游戏视觉 chat): "同时，目前游戏这个木纹的这个材质看起来很奇怪，我希望你能research一下更realistic的门的样式，结构什么的不用变，纯粹是贴图纹理的方案选择我需要你revise".

## 0. The one question

| Question | Options | Recommendation | Blocks |
|---|---|---|---|
| **Door wood texture: which one?** | **0** keep today, **1** plain-sliced red oak, **2** rotary birch, **3** rift-sliced red oak, **4** plain-sliced walnut | **1** | Nothing else waits on this. Landing starts once you answer. |

Reply with one number. By default the frame matches the leaf wood. If you want a different frame, say "1 with walnut frame" (or similar).

**Comparison sheet** (Shot format): `research/door_wood/images/door_wood_options.jpg`. The original is `W/door_wood/sheet/door_wood_options.jpg`.
- Columns are options 0–4. Rows: (A) your viewpoint, standing in the title room and looking through the open double door into the station; (A2) the same view with the door shut, so both leaves face you; (B) the shut title leaf at 0.6 m; (C) a D1.6 maze door (L0-F) at 3 m; and a centre crop of B.
- Each row is rendered in **one frame with time frozen**. Only the door materials are swapped between columns, so light, camera and the rest of the scene are identical across a row.
- Seed 516574485 (the stream-door harness seed). Game camera, FOV 76.
- There is one sheet per option: `images/door_wood_opt0.jpg` … `opt4.jpg`. The raw 1920×1080 frames are in `W/proj_doorwood/Verification/door_wood/opt<N>/`.

## 1. Why today's wood looks strange

These numbers come from the audit sheet `W/door_wood/audit/door_wood_audit_current.jpg` and the code.

**The leaf texture (`Door_Veneer`) is fake.** It is not a scan. `Tools/lookdev/gen_surfaces.py` `veneer()` draws it from a single sine wave.
- **Even stripes.** One frequency at **2.38 cm** carries 47 % of the grain's power. Every stripe has the same width, contrast and colour, so the leaf reads as corduroy. These are the "fine regular vertical stripes" in your screenshot.
- **Raised ridges.** Each stripe is also a bump: the normal map averages |xy| **0.052**. A real veneer door is flat under its lacquer, so this is about 2–4× more relief than a lacquered face shows.
- **No matched leaves.** There are no veneer-leaf joints, no cathedral arches and no change from leaf to leaf. Both faces of both title leaves share one set of UVs, so they show identical stripes.
- **The tile does not fit the leaf.**
  - On every title leaf face the texture repeats 1.07 × 1.01 times, so a **wrap seam** shows 75 mm from one edge.
  - The dark "kick" band painted into the bottom of the texture wraps round to the top, leaving a **19 mm dark line** along the top edge.
- **Low resolution.** The texture has 914 × 782 px/m, which turns soft at arm's length.
- **No lacquer.** Smoothness is a flat 0.47.

**The frames have their own problems.**
- **The title door frame shows cross grain.** The title kit's frame meshes run U along the casing (`FrontRoomsDoubleDoorKit` `Box(…, grainAxis)` and `Sweep`). `Prop_WoodWalnut` draws its grain along V, so on the casing and linings the grain runs across the wood.
- **The D1.6 OF-F / EX-F leaves repeat.** They use the furniture texture `Prop_WoodOak` with the grain turned onto U. Its 1 m tile repeats **2.08 times** up each leaf.

## 2. What a real 1990 office door looks like (short; the sources are in the research notes)

**Construction.** The standard commercial door is a 1-3/4 in solid-core flush door. It is factory finished in clear lacquer, which is flat and satin. Its edges are solid hardwood of the same species.

**Species and cut.** In the 1980s and early 90s the default clear-finish face was **plain-sliced red oak**: A grade, book-matched, running assembly. The UMich MasterSpec 08 14 16 and the NIU 08 1416 standard both call for it.
- Plain slicing gives **cathedral arches** in the middle of each veneer leaf and straight grain toward its edges.
- In a **book match**, every other leaf is flipped. The figure mirrors at each joint, and leaves alternate slightly lighter and darker, the "barber pole" effect.
- In a **running** assembly the leaves vary in width, and a part leaf sits at one edge.

**Other faces of the period.**
- Rotary birch: the budget face for schools, hospitals and back offices.
- Rift oak: quiet straight grain, at extra cost.
- Walnut and cherry: executive floors.

**Sources:**
- UMich MasterSpec 08 14 16: https://umaec.umich.edu/for.archs/masterspec/08/MS081416.pdf
- NIU 08 1416: https://www.hr.niu.edu/facilities/pdf/standards/Div-08-1416-Flush-Wood-Doors.pdf
- VT Industries face-match bulletin: https://www.vtindustries.com/webres/File/architectural-doors/Technical%20Bulletins/VT-Technical-Bulletin-Veneer-Face-Match-and-Assembly.pdf
- WDMA glossary: https://www.wdma.com/the-door-glossary

**Media.** The download wish-list, with licences and sizes, is in `media_candidates.md`. Nothing on it has been downloaded.

## 3. The options as the player sees them

### What all four options share

**One texture covers a whole leaf.** The face tile is 1.25 × 2.75 m at 2048 × 4096 px, which is **1638 × 1489 px/m**. That still holds detail at 0.6 m.
- Both the title leaf (1.20 × 2.64 m) and the D1.6 leaf (0.99 × 2.08 m) fit inside one tile, so **no repeat appears on any leaf**.
- U = 0 always falls on a veneer-leaf joint, so a wrap can only ever read as a joint.

**The grain is modelled from real wood structure.**
- Growth rings vary in width and come in groups (wet and dry years).
- Each ring has a sharp latewood-to-earlywood edge.
- Pores and fibre streaks are included.
- The local CC0 Poly Haven scan, already in the project and approved 2026-10-02, adds high-pass micro detail only.

**Leaves vary slightly.** Each leaf has a small tone and hue difference, plus a hairline at each joint.

**The surface is lacquered and flat.**
- Smoothness is 0.68–0.72 on the wood and lower in the pores.
- The normal map carries only the pore dimples, so it averages |xy| 0.012–0.027 (today: 0.052).

**The frame matches.** Each option has a solid-stock frame texture in the same wood, seamless, at 2048 px/m across the grain. The saddles keep today's oak.

**Brightness check (T6).** D1.6 has a rule: a locked steel door must read at least 1 stop brighter than a free veneer door in a dim Level 0 cell. The figures for each option are below; birch is the one that fails.
- How it is measured: the mean linear luminance of each leaf albedo is compared with `Door_Enamel` (PaintedMetal × 1.129/1.116/1.079, Y 0.683).

### The options

| # | Option | What the player sees | Steel door brighter by (T6, needs ≥ 1 stop) |
|---|---|---|---|
| 0 | **Today** | Orange corduroy. Even 2.4 cm ribs catch the light like ribbed plastic, every leaf is identical, and there is a dark line at the top. | 1.87 stops |
| 1 | **Plain-sliced red oak**, book match, running (5 leaves, 24–26 cm) | **A normal office door of the period.** From your viewpoint the double door reads as tan-amber wood with soft arches. At 3 m the maze door clearly reads as wood veneer, not stripes. At 0.6 m you see cathedral arches mirrored at faint joints, open oak pores and a satin sheen. The frame is matching oak. | **1.21 stops** (passes; a 15 % deeper amber would give about 1.45 if more margin is wanted) |
| 2 | **Rotary birch**, one-piece face | A pale honey sheet with a soft, wild, plywood-like figure and no joints: a school or back-office door. It is the lightest by far, and in the corridor it glows almost like the almond steel doors. | **0.23 stops: fails T6.** Fine for the title door only; on the maze doors a locked door no longer stands out. |
| 3 | **Rift-sliced red oak**, slip match (8 leaves, 15–16.5 cm) | Quiet, straight comb grain. Leaves repeat side by side (slip match), and lines vary in spacing and strength. From 3 m it reads as almost plain tan wood. It is the calmest choice and the closest to today's stripes, but irregular, flat and lacquered. | 1.29 stops |
| 4 | **Plain-sliced walnut**, book match, running (4 leaves, 30–32 cm) | Dark chocolate wood with soft arches: an executive-floor door. Doors become dark shapes against the yellow wallpaper, the strongest contrast of the set, and at 3 m the figure mostly disappears. The frame is walnut. | 2.80 stops |

### Rendered door colour

This is the mean sRGB of the maze door at 3 m, in the same frame for every option:

| Option | Mean sRGB |
|---|---|
| Today | (115, 82, 40) |
| 1 | (135, 102, 59) |
| 2 | (162, 144, 103) |
| 3 | (134, 100, 56) |
| 4 | (80, 58, 30) |

### Procedural stand-ins

All four options are procedural. This is because no free (CC0) scan exists of a whole matched door face.
- **Option 1.** The most realistic source would be a matched-flitch scan. The archviz ones are paid, for example Poliigon's matched oak sheets, which are reference only. Option 1 is the procedural stand-in, with the real CC0 red-oak scan supplying the micro detail.
- **Option 3.** The best CC0 source is Poly Haven `oak_veneer_01`, which is 1.83 m long, so one scan would cover a whole leaf height. It has not been downloaded; it is the second row of table A in `media_candidates.md`. Option 3 is its procedural stand-in. If you tick that row, 3 can be rebuilt from the real scan.
- **Option 2.** No CC0 birch scan exists. The closest stand-in candidate is Poly Haven `white_maple_veneer`.

## 4. Cost

| | Today | Each option (1–4 are the same size) |
|---|---|---|
| **Texture memory, Mac/PC** (GPU, with mips) | `Door_Veneer` 3 × 1024×2048 BC7 = **8.0 MiB** | Leaf: A 2048×4096 DXT1 5.33 + N 1024×2048 DXT5nm 2.67 + S 1024×2048 DXT1 1.33 = **9.3 MiB**. Door frame 1024×2048 set = **5.3 MiB**. Total **14.7 MiB**, which is **+6.7 MiB** over today. If the title kit's frame UVs stay as they are, the frame needs a second, turned copy: 20.0 MiB, +12.0 MiB. |
| **Shader cost** | — | **None.** Same `FrontRooms/Surface` shader, same variant, same three samples. No keyword and no second lobe. The lacquer is carried by the single specular lobe (high smoothness, flat normal). A true URP clear coat (second lobe, `_CLEARCOAT`) is not proposed: it would change the shared surface shader and add a lobe to every door pixel. |
| **WebGL / phone tier** | — | The same 2048 platform cap as every other surface today, set per platform only. The leaf albedo drops to 1024×2048, so leaf + frame ≈ 10.7 MiB. A WebGL-only 1024 cap on the frame would bring it to ≈ 6.7 MiB, about today's footprint. Mac/Win stay at the full tier. |
| **Furniture** | — | Unchanged. `Prop_WoodWalnut` and `Prop_WoodOak` stay for chairs, cases and counters; the doors get their own materials. |

Measured in the clone: `W/proj_doorwood/Verification/door_wood/memory.txt`. The editor's Profiler figure is 2× because it includes the editor's CPU copy; the numbers above are the GPU half.

### Work to land the one you pick

This is 游戏视觉 work, about half a day plus one capture round. Geometry is unchanged throughout.
1. **Textures.** The generator goes into `Tools/lookdev/`, and its outputs go into `Resources/Surfaces/Textures` with the importer settings above.
2. **Materials.**
   - `Door_Veneer` SurfaceDef: new texture, tile 1.25 × 2.75, macro wear 0.08.
   - New `Door_Frame` SurfaceDef: tile 0.5 × 2.0.
3. **Frames.** The title and station frames use `Door_Frame` instead of `Prop_WoodWalnut`.
   - `FrontRoomsStationDress.cs:152` is ours.
   - `FrontRoomsRoomStream.cs:1334` is a one-line change in the stream script. It goes through its owner, or through a kit default.
   - The title kit's frame UVs are turned so the grain runs along the wood. This also fixes today's cross grain, and one frame texture then serves both kits.
   - The `Kit_DoorFrame_Wood_DA` casing, lining and stop slot goes to `Door_Frame`, via a Blender re-export with the same geometry.
4. **OF-F / EX-F leaves.** These are `Kit_DoorLeaf_Veneer_Oak_DA`. The slot is re-pointed to `Door_Veneer` and the kit is re-exported without its turned-grain UV flag. That removes the 2.08× repeat.
5. **Check.**
   - Roslyn, then one run of this capture harness. The stairwell door (`FrontRoomsStairwell.cs:381`) also uses `Door_Veneer` and gets checked in the same run.
   - Verification images go to the verification log.

## 5. Recommendation: 1, plain-sliced red oak

**It is what the building would really have.** The specs of the period name plain-sliced red oak, book-matched, running, as the default clear-finish office door.

**It fixes every cause of "strange" at once.**
- The irregular rings and cathedral arches replace the sine stripes.
- A flat lacquer face replaces the ridges.
- Visible leaf joints with leaf-to-leaf variation replace one repeated tile.
- With no wrap on any leaf, the seam and the dark top line go.
- The texture has twice the detail at arm's length.
- The frame grain now runs along the wood.

**It still reads as wood at every distance in the sheet.** At 3 m it is clearly veneer, and at 0.6 m it is detailed without looking busy.

**It keeps T6.** The locked steel door stays 1.21 stops brighter.

**Why not the others?**
- **2 (birch)** fails T6 on the maze doors.
- **3 (rift oak)** is correct, but from a distance it flattens into plain tan, which is close to the anonymous look we are moving away from.
- **4 (walnut)** is an executive-floor wood. It would turn every corridor door into a dark slab against the yellow wallpaper.

## 6. Files

**Generator** (deterministic, numpy + Pillow, no downloads):
- `W/proj_doorwood/Tools/lookdev/gen_door_wood.py`, with a copy at `W/door_wood/gen_door_wood.py`.
- Run: `python gen_door_wood.py <out> <Tools/lookdev/cc0_src/polyhaven> [options]`.

**Capture harness** (clone-only):
- `W/proj_doorwood/Assets/Editor/DoorWood/FrontRoomsDoorWoodCapture.cs`, with a copy in `W/door_wood/`.
- Entry point: `FrontRoomsDoorWoodCapture.RunBatch`.
- It sets up the importers and materials, then renders the seed-4242 run, swapping every `Door_Veneer` slot plus the walnut on door frames.

**Sheet script:** `W/door_wood/make_sheet.py`, which uses `W/tools/shotframe`.

**Textures and materials** (clone only): `W/proj_doorwood/Assets/DoorWoodOptions/`. Each option has `DW<N>_Leaf`, `DW<N>_FrameV` and `DW<N>_FrameU` materials, plus `_A/_N/_S` textures.

**Logs:** `W/door_wood/logs/r2_capture.log` (0 compile errors) and `W/proj_doorwood/Verification/door_wood/log.txt`.
