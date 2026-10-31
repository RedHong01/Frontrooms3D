# Door frames: how thick should they be? For Red

*游戏视觉 (account B, session local_a9a1dce2), 2026-10-30. For viewing only. Nothing in the game has changed and nothing lands until you pick. The options were built and rendered in the clone `W/proj_doorframe`. Research: `10_period_frames.md`, `SOURCES.md`. [E] marks my inference.*

Your ask: "目前门框稍微有一些太厚了，我需要你调研那个年代门框应该是多大".

## The question

**How thick should the map's door frames be: F0, F1, F2, F3 or F4?** I recommend **F4**. Reply with one option, and add "+ putty" if you also want the steel frames repainted (see Finish below).

**Sheet:** `research/door_frame/images/door_frame_options.jpg` (original: `W/door_frame/sheet/door_frame_options.jpg`). Columns are the options, plus F4 in putty paint. Rows are the office door at 3 m and 1.2 m, head-on, the corner, the wall side, the steel door, the lobby door (twice), the title pair and the EXIT door.

## You were right, and this is the size

A US office door frame of about 1990 shows **2 in (51 mm)** of steel and stands **7/16 in (11 mm)** off the wall, or **2-1/4 × 11/16 in (57 × 17.5 mm)** if it is wood. Ours are **77 × 25 mm**.

Two limits on that evidence:
- No source dated 1993 or earlier that I read states "2 in". The earliest dated text is 1996 (Ceco and gpc specs). I carry it back to 1990 as [E].
- The wood numbers come from an undated carpentry text. No period photo has been checked yet.

## Why ours looks heavier than 77 mm

The player sees casing plus the dark reveal beside the leaf. That band reads **98 mm per side** on the steel frame, 9.8 % of the leaf.

- **The main cause:** the kit is a sleeve around the map's 70 × 20 mm trims, so it cannot go below 73 × 22.5 mm while those trims exist.
- **The head:** it is the same width as the jambs, which joins everything into one heavy square.
- **The dark finish:** it merges casing, reveal and shadow into one band.
- **The wall:** it is 160 mm thick, which keeps a deep reveal in every option.

## The options, as the player sees them

Sizes are face × thickness off the wall, in mm. "Band" is what the render shows on one side, reveal included, head-on and averaged over both jambs. Px are per side at 3 m, 1080p. Title pair: 50 × 14, no change in any option, identical pixels.

| Option | Steel frame (every map door but L0-F) | Wood frame (L0-F) | EXIT | Band, steel | Band, wood | Px at 3 m, steel / wood | Map change |
|---|---|---|---|---|---|---|---|
| **F0** today | 77 × 25 | 77 × 25 | 70 × 20 | 98 mm | 101 mm | 22.5 / 22.5 | none |
| **F1** kit-only limit | 73 × 22.5 | 73 × 22.5 | 70 × 20 | 93 mm | 97 mm | 21.5 / 21.5 | none |
| **F2** a touch slimmer | 63.5 × 19.1 | 63.5 × 19.1 | 70 × 20 | 82 mm | 85 mm | 18.5 / 18.5 | door trims |
| **F3** period share of our 1.0 m door | 55.6 × 12.7 | 57.2 × 17.5 + 4.8 reveal | 55.6 × 12.7 | 73 mm | 83 mm | 16.5 / 18.5 | door trims + EXIT |
| **F4** closest to period | **50.8 × 11.1** | **57.2 × 17.5 + 4.8 reveal** | **50.8 × 11.1** | **69 mm** | **83 mm** | **15.5 / 18.5** | door trims + EXIT |

- **F1:** you will not see this at playing distance. It is 1 px per side at 3 m, and the corner still reads as today. This is the honest limit without touching the map.
- **F2:** the frame is clearly lighter and the door gets more of the wall, but the steel frame still reads as a band rather than a line. The steel size is not a stock size. It only keeps steel and wood the same width.
- **F3:** the steel reads as a thin line and the head stops being a heavy bar. Up close the frame sits much flatter on the wall. The frame has the same share of the door as on a real 3-0 door.
- **F4:** F3 with the real stock numbers, 69 mm against 73 mm. At 3 m it is 1 px per side slimmer than F3, so you are choosing the numbers more than the look. The frame takes about 30 % less of the view than today. Our door is 9 % wider than a 3-0, so F4 takes a slightly smaller share of it than a real frame does (5.1 % of the opening against 5.6 %).
- **Wood in F3 and F4:** a plain flat casing with a thin shadow line where it steps back from the opening. It ends up at about the same band as F2, but thinner off the wall. My first version had the period slope, and on the head it mirrored the ceiling lamp as a white bar. So the casing has a flat crown and a rounded outer edge, and I dropped the back band.
- **EXIT in F3 and F4:** the same steel size as the other doors. The EXIT sign stays where it is on the wall, so its gap above the frame grows from 10 mm to about 29 mm (visible in the last row).

## Finish (optional, works with any option)

**"+ putty"** swaps the steel frame's dark bronze for our existing `Prop_SteelPutty`. Period notes have steel frames painted the same putty as a steel door, while dark bronze on steel is our own choice [E].

It shrinks the dark band more than any geometry change, to about 18 mm head-on on F4, because the casing nearly joins the wall (luma 116 against 130) and only the reveal stays dark. But it changes the look of every door in the game, not only the frame's size: doors lose their dark outline against the wallpaper. I would keep the dark frame unless you want that change. The sheet shows it on the steel-frame rows only.

## What it costs

| | F1 | F2 | F3 | F4 |
|---|---|---|---|---|
| **Steel_DA tris** (LOD0/1/2) | 2,030 / 724 / 276, unchanged | unchanged | unchanged | unchanged |
| **Wood_DA tris** (today 1,724 / 940 / 252) | 1,604 / 820 / 252 | unchanged | 1,676 / 892 / 252 | 1,676 / 892 / 252 |
| **Map tris** | 0 | 0, same boxes, smaller | 0 | 0 |
| **Kits rebuilt** | 5 | 5 | 5 | 5 |
| **Map contract (关卡设计)** | none | 2 door-only constants | those 2 + an EXIT value | those 2 + an EXIT value |

- **The 5 kits:** `Kit_DoorFrame_Wood_DA`, `Kit_DoorFrame_Steel_DA`, `Kit_DoorFrame_Steel_Alu_DA`, `Kit_DoorFrameBreak_Wood_DA`, `Kit_DoorFrameBreak_Steel_DA`. The single-acting kits stay untouched.
- **Map contract:** door-only constants at `FrontRoomsMapWorld.cs:2754` (`.045 × .008`, and for EXIT `.0508 × .0111` in F4). `ModuleUnits.TrimFace` and `TrimProud` must not change, because they also size the stairwell walls, the station set and `ClearOfSwing`. The trims cover no gap, so shrinking them opens no hole.
- **Also unchanged:** colliders, leaf, pivot, anchors, textures and materials.
- **Door break:** the break kit generator ran without error on F2, F3 and F4 in Blender, but its strike-zone fields were written for the old width. They still need a retune and a look before landing.

## Recommendation: F4

- It is the only option built on real numbers, so it needs no scaling story.
- It gives the biggest drop, from 98 to 69 mm, and it also takes the thickness off the wall, from 25 to 11 mm, which is what makes the frame read thin close up.
- It costs the same as F3, which you could not tell from it at 3 m.
- F2 saves only the EXIT value and leaves the steel frame 25 % wider than a period one.
- F1 is the only option with no map change, but it does not answer your ask.

## What it blocks

Nothing else waits on this. A pick from F2 to F4 needs 关卡设计 to land the map constants, and it opens one follow-up for you: **windows stay at 75.5 × 25**, so from F2 on they would be wider than the doors on the same wall. That is a separate call, not part of this one.

## Not shown or not tested

- One door on one seed (516574485), at High on desktop, 1080p, at LOD0 distance. No phone or iPad frame, no LOD1 or LOD2 view.
- No open leaf and no swing. The 95° clearance is an assert only; a thinner frame helps it. No test suite was run.
- Break stages and the break kits are not rendered.
- The Run doors and the unused Steel_Alu kit are not rendered. The Run doors use the same Steel_DA frame.
- The EXIT door's stairwell side is not rendered. It stays 70 mm in every option, so in F3 and F4 the two faces of that doorway differ.
- Windows are not rendered beside a slimmed door.
- No period photo has been compared with these frames. `media_candidates.md` lists what is wanted, all unverified, and needs your OK.

**Method:** one Unity run per option, same seed, same door, the views pinned to the same frame numbers. Wall pixels outside the door differ by at most 0.012 of 255 between options, so the light is the same. Raw frames and `measure.json` are in `W/door_frame/out/`.
