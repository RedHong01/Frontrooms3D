# Hunter · production B · 40 · The Relay under the ceiling lamps (PL01)

Line 游戏视觉 (game visual), workflow `W/pool_look`, stage PL01, 2026-11-03 (America/Los_Angeles). Run by stage agents on
account B (relay of account G's run, which the 19:23 session limit stopped after the TODAY measurement; the Mac rebooted at
about 20:18, after run t3 had finished). The decision ask in Chinese is `41_ceiling_lamps_for_red.md`; the sheet is
`sheets/40_ceiling_lamps.jpg`; one film per option is in `video/40_ceiling_lamps_<option>.mp4`.

**Red's design (verbatim, `research/hunter/11_squeezed_giant.md`):** "我希望怪物能够像是巨大的身躯挤在小小的这个空间里". Rule 2 of
that page: in every room the back of the shoulders (or the hood) is within 2-5 cm of the ceiling tiles.

**The finding:** combined test 11-02/11-03, `W/combined_test_1102/VERDICT.md` note (a): "PL01 frame 11_relay.png: the Relay's
head clips into a ceiling light at 3 m. It is an eye note, and a playtester may see it." Frame:
`W/proj_bstations/Verification/main-autopilot/11_relay.png` (seed 2554, player about 3 m behind); crop
`images/40_11_relay_top.jpg`.

**What was done:** main read only (no Unity on main, no git, no download); everything measured and rendered in the clone
`W/proj_unlock` with the production body (`Resources/Creatures/Relay_NightShift`, LOD0, `FRONTROOMS_RELAY_V2` on) and the
lamps built by the kits exactly as the map builds them. Three options prototyped in clone-only copies; the recommended one is
packaged as `W/apply/relay_lamp_clearance` and is **not applied**. Nothing landed in main.

**Reused, not redone:** the map's fixture rule (`FrontRoomsMapWorld.BuildFixture`, main `f6750c56`), the L03 kit
(`FrontRoomsTrofferKit`, `2ab5cdf1`), the zone lamps (`FrontRoomsLampKit`, `009fc4ed`), the body (`FrontRoomsRelayBody`,
`7de0dea6`; rig `542df78c`; tests `ba9d8c73`), the pool-look harness pattern (game camera, post stack, fill light). All main
sha1s re-checked at 20:47: unchanged, no open CLAIM on them.

## 0. The answer in seven lines

1. **It is the upper back, not the hood.** In every state the body's highest LOD0 vertex is on the shrug / chest (vertices 1235,
   6925, 6931); the hood stays 83-143 mm under the ceiling. The dark dome in `11_relay.png` is the upper back.
2. **In the walk it does not touch the lamp.** L03 hangs its frame 23.8 mm under the tiles; the walking shrug is 26.8 mm under
   them (2.9 m rooms), 25.5 mm (2.4 m): 3.0 / 1.7 mm clear. The "clip" there is how it reads: a dark, unlit dome laid over the
   bright lens with no gap, because the lamp's light sits 33 mm *below* the body's top and lights nothing on it.
3. **Two states really poke in:** the lock-on hold (9.4 mm into the frame, 8.2 mm into the lens recess) and the door blow
   (23.3 mm into the frame, 0.5 mm through the lens plate). The pin's locators miss the mesh's true top in those poses.
4. **Main moved at 18:47:** Office rooms now hang the O-C pendant 0.60 m down (bottom 2.30 m in a 2.9 m room) and back-of-house
   Low modules the 4 ft wrap. The walking Relay goes **through** them (section 1.6): up to 601 mm into a pendant, 82 mm into a
   wrap. That is a real clip, far larger than L03's.
5. **The floor (section 2), in every option but TODAY:** the true top kept 3 mm under the tile or the lamp's frame, the fold
   under deep fixtures, the Listen glove ON a plain tile. Result in 85,490 baked frames per option: **zero** contacts of any kind.
6. Options for L03 on top of the floor: **A** duck under lamps, **B** head bow, **C** contact (the lamp's light on the body's
   top, its shade on the floor; no pose change).
7. **Recommended: C** (section 3.5), packaged, not applied. Its honest flaw is in section 3.5.

## 1. Measurements: what the game does today (TODAY)

Set (clone, harness `FrontRoomsRelayLamps`, section 8): Level 0 corridors on the map's 3 m grid, one cell wide, ten cells
long, with the map's lamp per cell (root on the ceiling plane at the cell centre + 0.3 m in Z, long side along Z, the "Light"
spot 0.06 m under it: `MapWorld.BuildFixture`): a 2.9 m corridor along Z, a 2.4 m Low corridor along Z, a 2.9 m corridor along
X, the same 2.9 m corridor with its +X wall cut away for side views; since main `f6750c56` also a 2.9 m Office corridor with
O-C pendants (plus a cut copy) and a 2.4 m back-of-house corridor with 4 ft wraps. 26 of the 70 L03 lamps shadow-capable by
the map's own hash (seed 2554). The body: `FrontRoomsRelayRig` + Night Shift, scripted states at 60 Hz, LOD0 baked
(`SkinnedMeshRenderer.BakeMesh`) on every tick and tested vertex by vertex against each fixture's real bounds. Final numbers
are run t6 (85,490 baked frames per option); stills and films are run t7 (the same code).

### 1.1 The lamps as built (world, relative to the ceiling plane)

| Lamp | Where (main `f6750c56`) | Lowest point | Lens | Lit area / whole fixture (XZ) | Light |
|---|---|---|---|---|---|
| L03 troffer | Level 0, Low, Tall, start room, station | frame face **-23.8 mm** | plate -1.0 mm, recessed inside the return walls (to -20.5 mm) | 0.510 x 1.124 / 0.624 x 1.224 m | spot 162/96 deg at -60 mm, aimed down, range 10 m, i 5, shadows off (1 in 3 soft within 9 m of the player) |
| O-C pendant | Office rooms of Standard height | body bottom **-601.5 mm** (2.30 m in a 2.9 m room) | side strips and top lens (lowest -545 mm) | 0.270 x 2.426 / 0.277 x 2.448 m | the same spot at -60 mm, Office colour (0.90, 0.96, 1.00), i 6 |
| 4 ft wrap | back-of-house Low modules | **-82.0 mm** (surface-mounted) | the wraparound diffuser is its underside | 0.250 x 1.235 / 0.250 x 1.235 m | the same spot, Level 0 colour, i 5 |

Numbers from the kits as instanced (run logs); the troffer hangs by `Hang` 21.5 mm so its lens face is 1 mm under the tile and
its T-bar skirt is the outer wall (no ceiling hole). **The lamp's Light (-60 mm) is under the body's top (-27 mm) whenever
the body is under it**, so the top of the body gets nothing from the lamp it touches, and with shadows off the light passes
through the body onto the floor (with the map's soft shadows on, a light inside the mesh casts no body shadow either: frame
`pl01shadow_today.jpg` is the same as `pl01_today.jpg`).

### 1.2 The top per state, TODAY (mm relative to the ceiling plane)

| State | 2.9 m corridor top | part | 2.4 m Low top | locator top (what the pin sees) | pin |
|---|---|---|---|---|---|
| Walk 2.6 m/s | -26.8 | shrug/chest (v1235) | -25.5 | -20.0 | -0.054 / -0.095 m |
| Run 4.3 m/s | -26.8 | shrug/chest | -25.5 | -20.0 | same |
| IdleListen (25 positions round a lamp) | -26.8 | shrug/chest | -25.5 | -20.0 | same |
| Search | -26.8 | shrug/chest | -25.5 | -20.0 | same |
| v2 lock-on hold (C37 1.7 s) | **-12.3** | shrug (v6925) | -22.1 | -20.0 | -0.071..-0.062 |
| "almost" stop (from Listen / Walk) | -26.5 | shrug | -26.3 | -20.0 | -0.058 |
| BreakDoor, 5 blows (no door: squeeze 0) | **-0.5** | chest/shrug (v1235, 6925, 6931) | -0.5 | -20.0 | -0.077..-0.048 |
| Stagger | -26.8 | shrug/chest | -25.5 | -20.0 | -0.054 |

The hood's top stays at -142 mm in the walk (-124 lock-on, -83..-87 door blow); the right glove's top at -98 mm in Listen.

### 1.3 Contacts and penetrations, TODAY (every frame of every state; L03)

| State | into the frame (T-bar ring) | into the lens recess | through the lens plate | glove on a fixture |
|---|---|---|---|---|
| Walk, Run, Listen, Search, "almost", Stagger (2.9, 2.4, along X) | 0 | 0 | 0 | 0 |
| Lock-on hold, 2.9 m | **9.4 mm** (152 frames) | **8.2 mm** (735 frames) | 0 | 0 |
| Lock-on hold, 2.4 m | 1.7 mm (74 frames) | 0 | 0 | 0 |
| BreakDoor, 2.9 / 2.4 m | **23.3 mm** (110 / 98 frames) | 19.5 mm (510 / 377) | **0.5 mm** (226 / 133 frames) | 0 |

Totals over the three L03 corridors: 700 frames into the frame, 2180 into the recess, 454 through the lens plate.

Cause (checked in code and on the mesh): `FrontRoomsRelayBody.Tick` pins the locators `top_883/1069/1396/3563/5245`, the
shrug bone and `hood_front_top` to h - 0.02 (pin clamped -0.12..+0.02). Each locator rides ONE bone. Vertex 6925 is skinned
to the shrug alone but sits elsewhere on it and rises past `top_5245` when the lock-on squares the shoulders; vertex 1235 sits
where `top_1069` sits at bind but is skinned 0.33 chest / 0.24 spine_02 / rest, so when the blow pitches the chest 22 deg the
locator dips with the chest while the vertex stays up. The P1 test (`FrontRoomsRelayRigTests`, line 297) checks only the
static Walk pose and allows the mesh top to reach h + 0.005, so it passes.

### 1.4 The Listen glove, TODAY

`10_plan.md` section 2: "In Listen the right glove rests flat on a ceiling tile, the electrician feeling the ceiling". As
built, the glove's top is **98 mm under the tiles** in a 2.9 m room (body-local 0.375, 2.802, 0.912 m): the Listen key puts it
44 mm under, and the pin then lowers the whole body 54 mm. In 2.4 m rooms the Listen key is not used (the low pose). From below
a glove 10 cm under a lens reads as a hand on the lamp (`listen_today.jpg`); in the glove sweep (175 positions round a lamp,
lateral -0.75..+0.75 m, along -1.8..+1.8 m) its top point floats under a lamp's footprint in 16 of them (14 under the lens itself).

### 1.5 What `11_relay.png` shows

Seen from the player's eye 3 m behind, the line of sight over the body's top meets the ceiling only 6 cm beyond it, so the
top's silhouette always lies on the lens image when the top is under a lamp: a millimetre-scale gap is invisible (one pixel is
about 3.7 mm at 3 m at 1080p and 76 deg). What tells an eye that a body is in front of a lamp and not in it is light: the lamp
would light the top of a body 3 mm under it more than anything else in the room, and the floor under the body would lose most
of that lamp's light. TODAY neither happens (section 1.1). The crop `images/40_11_relay_top.jpg` shows the whole unlit dome
over the lens with no cut line.

### 1.6 Main moved at 18:47: the Office pendants and the back-of-house wraps

`FrontRoomsMapWorld.BuildFixture` (main `556b9099` -> `f6750c56`, 关卡设计 for 灯具适配, landed 18:47) now asks
`FrontRoomsLampKit.ForCell` for each cell's lamp: Office rooms of Standard height hang O-C's 8 ft lensed-indirect pendant with its
body's bottom 0.60 m under the ceiling (`PendantDrop`, kept only where that bottom is at least 2.2 m: the player's headroom),
back-of-house Low modules hang the 4 ft wrap surface-mounted on the grid, every other cell L03. The body knows nothing of this:
it presses its shrug 27 mm under the tiles whatever hangs there.

| TODAY | frames inside a fixture | deepest | where |
|---|---|---|---|
| Office 2.9 m, walking along the pendant row | **455 / 455** | **574.7 mm** | the whole upper back and shoulders through the pendant body (`pendant_today.jpg`, `pendantside_today.jpg`) |
| Office, lock-on hold / door blow | 2400 / 2400, 3750 / 3750 | 589.2 / 601.0 mm | the same |
| Low 2.4 m back-of-house, walking under the wraps | 242 / 455 | **56.5 mm** | the shrug through the wrap's diffuser (`wrapside_today.jpg`) |
| Low back-of-house, lock-on hold / door blow | 1056 / 2400, 1515 / 3750 | 59.9 / 81.5 mm | the same |

In the Office the map puts one pendant per 3 m cell along Z (2.45 m long, 0.55 m gaps), on the corridor's own axis, so a
Relay walking an Office corridor lengthwise is inside a pendant almost the whole way. This is a real clip, at every distance,
and it is in the game since 18:47 whatever Red picks below.

## 2. The floor: what A, B and C all carry

Every option but TODAY carries the same four parts (they fix sections 1.3, 1.4 and 1.6 whatever is picked for L03):

1. **The live-lamp registry** (`FrontRoomsTrofferKit`): `RegisterLamp` from `FrontRoomsTrofferKit.Build` (L03: the lens and the
   housing's renderers on desktop, the one phone mesh on the phone tier) and from `FrontRoomsLampKit` (the O-C pendant with its
   housing, the 4 ft wrap); a static spatial hash on the map's 3 m cell; `LampsNear(world, radius, into[])` without allocation.
   A destroyed lamp leaves the hash when a query meets it, a moved one (chunk rebuilt or shifted) is measured again and
   re-hashed, an inactive one is skipped; `SubsystemRegistration` clears it. Read only: the lamp logic never sees it.
2. **The true top.** The pin also reads the mesh: the LOD0 vertices on the hull of the upper body (spine_02, chest, shrug,
   clavicles, neck_01; the extreme vertex for each of 577 directions) and of the right glove, 190 in all, skinned with four
   weights as the GPU does. The pin is the smaller of the old one and what keeps every hull vertex `SkinClear` 3 mm under the
   tile or the frame of a lamp over it (ramped over 5 cm round a fixture); for it the pin may reach `PinExtra` 4 cm under its
   old -0.12 m floor (the door blow under a lamp in a 2.4 m room needed 18 mm more: run t5 left 14.9 mm there, t6 none).
3. **The deep fold.** A fixture whose bottom hangs more than 5 cm (pendant, wrap) folds the room pose as a lower ceiling within
   0.22 m of a top marker, now or 0.6 s ahead along the ground velocity, with the existing fold 0.25 s / unfold 1.0 s smoothing;
   below 2.4 m the knees take the rest. Office corridor: the top walks at -626 mm (25 mm under the pendant's bottom) in the low
   pose; back-of-house: -97..-108 mm under the 82 mm wraps.
4. **The Listen glove on a plain tile.** The glove's hull is placed 4 mm under the tiles: the arm turns about the vertical
   through its shoulder by the smallest step (5 deg) whose glove, turned **and** lifted, is 8 cm off every fixture, outward up to
   50 deg, inward at most 20 deg (further in, the forearm crosses the face); the lift waits for the turn, comes in with the
   cube of the Listen weight and is clamped to the exact lift to the tiles (it never lags above them). Where no such turn exists
   the glove stays where the Listen key puts it: 98 mm under, never on the lamp. The search runs every 6 ticks; between, the
   last goal holds while it stays clear.

| Floor result, run t6 (85,490 baked frames per option) | TODAY | A | B | C |
|---|---|---|---|---|
| frames into an L03 frame / recess / through the lens | 700 / 2180 / 454 | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 |
| frames inside an Office pendant / a wrap | 8368 / 3548 | 0 / 0 | 0 / 0 | 0 / 0 |
| frames through the tiles; glove on a fixture | 0; 0 | 0; 0 | 0; 0 | 0; 0 |
| Listen glove, 2.9 m (350 positions in two corridors) | 350 float 98 mm under | 232 on a tile, 13 float, 105 too low (the duck) | 332 on a tile, 18 float | 332 on a tile, 18 float |

## 3. The options for L03 (sheet `sheets/40_ceiling_lamps.jpg`, films `video/`)

Same cameras, timing and exposure in every column: PL01 vantage (eye 1.62 m, 3 m behind the root, level, the top under the
lamp of cell 4), side at 4 m (cut corridor), front (eye 1.62 at the corridor's south cell, the Relay walking at the camera, its
top under the lamp of cell 2 at 6.3 m), Listen (side at 3.0 m; the root 0.9 m short of and 0.25 m across from the lamp of
cell 5, where TODAY's glove floats under the lens's edge), close (1.2 m out, 0.9 m behind the lamp centre), the lamp over the
Relay dead, the Office pendant (PL01 vantage and side), the back-of-house wrap (side). Desktop Ultra, 1920 x 1080.

### 3.1 TODAY

No change. Reads as a clip at the PL01 vantage (dark unlit dome on the lit lens); real clips in the lock-on and the door blow,
through every pendant and wrap; the glove floats.

### 3.2 A · duck under lamps

While a top marker is within 0.10 m of an L03's outer rectangle (where it is now: no look-ahead), the room pose folds to the
lamp's frame less 0.10 m (the pin target 0.124 m under the ceiling), the knees take it as they take the pin, fold 0.25 s /
unfold 1.0 s. Numbers: walking a Level 0 corridor the top swings between **-147 and -100 mm** (a 47 mm dip under every lamp; at
2.6 m/s a lamp every 1.15 s, never back to the -27 mm squeeze); run -103 mm; no contact in any state.
What a player sees: from behind a lit gap above the shoulders, and the lamp's own light now reaches the shrug (the top is under
the Light); in motion a giant that stoops under each lamp and half rises between them, 10-15 cm under the tiles all along Level 0.
Under a lamp the Listen glove cannot reach the tile (105 positions too low; in the LISTEN still it ends 256 mm under the tiles
after a 40 deg turn). **Rule 11 risk:** stooping at every lamp can read
as the Relay keeping away from light.

### 3.3 B · head bow

While the hood marker is within 0.10 m of a fixture, the neck and head bow forward and down 13 deg (neck 30 % / 30 %, head
40 %), 0.25 s in / 1.0 s out; the shoulders stay pinned, so the back still brushes the tiles. Numbers: the hood top moves from
-142.3 to -141.7 mm: it is never the top (the shrug is), so the bow changes nothing at the contact; no contact in any state (the
floor). What a player sees: from behind nothing at all (the head hangs under the hump; `pl01_B.jpg` equals `pl01_today.jpg`);
from the front the head dips at each lamp and lifts after it. **Rule 11 risk:** a head that ducks at each lamp can read as
flinching from light.

### 3.4 C · contact

No pose change. Under a lit lens: (1) the lamp's light on the body's top: a spot 0.30 m over the lens centre with the lens as
its aperture cookie (the lens as the light sees it at the body's top), so light lands only where the lens could light the
body, never on the fixture's return walls or the tiles (run t3 lit the return walls in dashed strips; the cookie took them out:
their luma 44.5 = TODAY's); 0.35 of the lamp's own intensity times how far the top is under the lens and within 8 cm of the
fixture, the lamp's colour; it reads the lamp's Light every tick, so it follows the flicker, dim, dead lamp, Relay warning,
blackout and the player's light radius; (2) the floor shade: a soft multiply quad under the body (the troffer kit's soil
material), 0.5 at the lamp's full light. The lamps themselves never change (`10_plan.md` records Red's staged-warning decision
that lamps never change where the Relay is). Numbers at the PL01 frame: the top's luma 72.7 -> 83.7, the floor under the body
82.1 -> 77.7, the return walls unchanged. What a player sees: as the giant passes under each lamp its hump catches the lamp's
light and the floor dims under it: a body pressed against the ceiling, brushing the lamps, still 27 mm under the tiles. **Rule
11:** nothing avoids light; the Relay walks straight through it and is lit by it; the shade is its own, under it, and hides
nothing of the player.

### 3.5 Recommendation: C

C keeps Red's squeeze (the top 27 mm under the tiles, inside "2-5 cm"), turns the reading from "cut into the lamp" to "pressed
under the lamp", and with the floor it removes every real clip measured. A gives up the squeeze in Level 0 and bobs; B does not
touch the contact and is invisible from the finding's own vantage; both carry a rule 11 risk.
**Honest flaw:** from 3 m behind the dome still covers the lower half of the lens image, as a body 3 mm under a lamp would; a
viewer who expects a visible gap will still see the body meeting the lamp. The light is a stand-in for the lens's area (a point
source with the lens as aperture) and the floor shade is a soft blob, not the body's own shadow shape. Under a dead lamp
nothing lights the top (right, but then it reads as TODAY). In Office rows the floor's fold keeps the Relay in its low pose
almost the whole way (section 1.6); that changes its Office silhouette and is part of the ask.

## 4. Cost (COST row of the sheet)

Tick times are `FrontRoomsRelayBody.LastTickMs` means from run t6 in the 2.9 m corridor, taken with the machine under load
after the reboot (load average 70-130): **UNDER LOAD, not valid as a budget**, relative only.

| | TODAY | A | B | C (package) |
|---|---|---|---|---|
| lines (RelayBody, TrofferKit, LampKit) | 0 | about +580 | about +590 | **+725 / -6** (the contact about 158 of them) |
| tick, walk / Listen | 0.018 / 0.018 ms | 0.109 / 0.282 ms | 0.077 / 0.163 ms | 0.079 / 0.215 ms |
| draws and lights | - | - | - | while under a lit lens: +1 spot light with a 128 px cookie (pixel light, Forward+, cookie atlas on at every level), +1 transparent floor quad; WebGL: the quad only |
| risks | the clips stay | squeeze lost in Level 0, bob, glove short under lamps, rule 11 | contact unchanged, invisible from behind, rule 11 | the flaw in 3.5; one more light per Relay on phones |

## 5. Not measured, and limits

- GPU frame time of the contact light and the quad (rule 25: not measured). Phone tier not rendered: the contact light is one
  more pixel light with a cookie while the Relay is under a lit lens (one URP asset at every level, cookies on).
- Unity did not rebuild the ambient probe within 20 s in any run: every frame keeps the scene's probe, the same in every column.
- Only the map's own layout: corridors one cell wide, one lamp per 3 m cell. Rooms wider than a cell, Tall rooms, the station
  and the start room are not set up; the registry and the rules do not depend on the layout.
- The door squeeze through a real door was not run (squeeze 0); the pin is off while squeezing anyway (`doorW`).
- The glove: 18 of 350 positions leave it floating 98 mm under a lens (no turn within 50 deg out / 20 deg in reaches a tile).
- The Office fold in motion was not filmed (the films show the L03 corridor); stills `pendant_*`, `pendantside_*`.
- The registry forgets a destroyed lamp lazily (when a query or a new lamp meets its cell): entries of lamps in cells nobody
  visits again stay until the next play session (a few hundred bytes each).
- `FrontRoomsRelayRigTests` P1 (line 297) still allows h + 0.005; with the package the true top stays 3 mm under the tile.
  Tightening P1 to h - 0.002 and adding the lock-on and door-blow poses is a follow-up, not in the package.

## 6. The package (recommended option C + the floor; NOT applied)

`W/apply/relay_lamp_clearance` (convention v2, built by `W/tools/mkapply.py`): 3 paths, 0 new, the 3 `.meta` files kept.

| Path | main (base) | payload |
|---|---|---|
| `Assets/Scripts/Rendering/Relay/FrontRoomsRelayBody.cs` | `7de0dea6` | `56ed1e42` |
| `Assets/Scripts/Rendering/FrontRoomsTrofferKit.cs` | `2ab5cdf1` | `47ba7f66` |
| `Assets/Scripts/Rendering/FrontRoomsLampKit.cs` (灯具适配's last landing) | `009fc4ed` | `ad48774f` |

Gate (rule 25): Roslyn macOS and iOS against main with the 3 overrides, 0 errors; `DRY RUN OK`; a rehearsal `--apply` on a
scratch copy gave `MANIFEST OK`. `FrontRoomsRelayBody.LampRules` (default true) turns the whole thing off for tools and tests.
All in game-visual paths (rule 3); no contract needed. If Red picks A or B instead, the same floor ships with that option's few
lines (the clone prototype has all three behind `LampOption`).

## 7. Files

- Sheet `sheets/40_ceiling_lamps.jpg` (+ `W/pool_look/pl01/sheets/40_ceiling_lamps_fit.txt`, ALL LABELS FIT).
- Films `video/40_ceiling_lamps_today.mp4`, `_A`, `_B`, `_C`: 10.0 s each (1 s title card, 4.5 s follow cam 3 m behind, 4.5 s
  the Relay walking at a still camera under the lamps), 30 fps, H.264 CRF 23, yuv420p, faststart, no audio stream, no logo.
- Frames and numbers: `W/pool_look/pl01/renders/t7/` (stills, film frames, `shots.tsv`), `W/pool_look/pl01/renders/t6/`
  (`measure_<option>.tsv`, `_positions`, `_walk_frames`, `_glove`, `_top_vertices`).
- Prototype sources: `W/pool_look/pl01/proto/src/` (`relay_lamps_block.cs.txt` the clone block with `LampOption`,
  `make_prod_block.py` derives `relay_lamps_prod_block.cs.txt`, `splice_body.py` / `splice_prod.py` build the two bodies from
  main's base in `proto/base/`, `troffer_registry.cs.txt`).

## 8. The harness (to run it again)

Clone-only `W/proj_unlock/Assets/Editor/RelayLamps/FrontRoomsRelayLamps{,.Set,.Measure,.Shots}.cs` (ASCII, no tuples). Edit-mode,
new scene, staged over editor updates, exits by itself (no `-quit`; renders need graphics):

```
cd W/pool_look/pl01/tmp && QUIET_CHECKS=3 /bin/zsh ./run_pl01.zsh <run> -rlOptions today,A,B,C -rlDo measure,stills,film \
   -rlFilmFrames 270 [-rlListenOffset -0.9] [-rlListenLateral -0.25]
```

It waits until no Unity runs on any `W/proj_*`, then runs `FrontRoomsRelayLamps.RunBatch` with `-buildTarget OSXUniversal
-frGlassRT off`; output `W/pool_look/pl01/renders/<run>/` (`run_log.txt` ends `FINISHED fail=n`). Films:
`W/venv/bin/python W/pool_look/pl01/tmp/build_film.py <run>`; sheet: `W/pool_look/pl01/tmp/make_sheet.py`.
