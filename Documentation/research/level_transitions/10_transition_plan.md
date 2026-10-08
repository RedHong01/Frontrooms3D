# Level transitions · 10 · Plan: five variations and their pre-render briefs

Date: 2026-10-03. This is a plan only. Nothing is implemented in Red's project, no media was downloaded and nothing was written to Figma. The only new file is this one.

**Inputs:**
- `01_ip_research.md`: Backrooms IPs and level-art practice.
- `02_real_buildings.md`: US commercial interiors, 1975–1993.
- `03_cut_inventory.md`: our cuts, as means per 24 m chunk over 1620 chunks.
- `shots.json`, `images/shot1..4_before.jpg`.

**Also read:**
- the map code in the clone `proj_trans`;
- `../interactables/10_spec.md`: R3's door and window frames, already designed;
- `Tools/Blender/frontrooms_kit/assets/doorway_studs.py`: the existing stud doorway kit;
- `../wallpaper_motion/`: the print and ink layers on the Level 0 paper;
- `Documentation/VISUAL_CHAT_TASKS.md`: N1, N3, R3, Q4.

**Status of the clone:** `proj_trans` matches Red's project for `FrontRoomsMap.cs`, `FrontRoomsModuleUnits.cs`, the level profile and the surface shader (md5-checked today). `FrontRoomsMapWorld.cs` is the exception. Red's copy changed at 12:21 today: doors are now single-acting (`FixedSwing`, stops, latch). The wall, opening, floor, ceiling and lamp code is the same in both. The clone stays frozen as the BEFORE base. Any contract text must be rebased on the 12:21 file.

---

## 0. For Red

- **What the IPs do.** No Backrooms IP changes the wall finish on a bare line in the open. Every change sits on something: a door, a deep doorway, a stair, a corner or a dark stretch, or it is hidden by a black cut.
  - The closest case to ours is Kane Pixels, *Found Footage #3*, 34:18–34:28. A yellow wall has a deep recess with a doorway in it.
  - The white finish wraps the whole jamb, and the yellow stops at the outside corner.
  - The black base and the carpet run straight through, and the white side has its own cooler, brighter light.
- **What real buildings do.** In US offices of 1975–1993, a finish never ends on a bare wall. It stops on something built: a steel frame, a corner bead, a pilaster, a header or a carpet bar. The newer work owns the joint. So a 1990 office fit-out built inside an older space owns every frame, base end and bar.
- **Why ours looks cut.** The map changes five things on the same 3 m cell line and builds nothing there: paper, carpet, ceiling grid, ceiling height and lamp level. The edge rule ignores theme (`FrontRoomsMap.cs:360-373`). So Office and Level 0 meet through open edges (0.89 per chunk) and bare arches (0.37 per chunk), which no IP and no real building has. Corner posts also z-fight (3.17 per chunk).
- **Five variations, each a different idea:**
  1. **Frame.** Every change sits on a built piece the Office side owns: a cased arch, a portal, a steel door frame, a base.
  2. **Renovation.** The Office fit-out is visibly eating Level 0: stripped paper, primer, carpet cut back, missing ceiling tiles.
  3. **Neck.** Every frameless crossing becomes a 0.6 m deep doorway lined in Office paint, like Kane's recess.
  4. **Drift.** The two looks mix over ±4.5 m in whole building units: paper drops, carpet tiles and ceiling tiles.
  5. **Light lead.** The light changes first: Office light turns cool, and Level 0 border cells go dark.
- **None of them changes the map layout.** Zones, maze, rooms, doors and edge kinds stay the same, so the four fixed shots compare one to one.
  - Only Neck adds colliders.
  - All five include two bug fixes, called B0: one for the corner posts and one for the grime band.
- **My provisional pick:**
  - **Frame** as the base grammar everywhere.
  - **Renovation** as a sparse story layer on the Level 0 side.
  - The **cool Office light** from Light lead.
  - **Neck** instead of Frame's thin portal, if you prefer Kane's deep doorway.
  - **Drift** later, tied to higher tiers.
  - I will confirm this after the pre-renders.
- **What you decide:**
  - the variation or the mix;
  - whether Office is the newer layer;
  - whether colliders may change;
  - whether Office lamps go cool white;
  - whether transitions get rougher at higher tiers.

---

## 1. What we are fixing

From `03_cut_inventory.md`, per 24 m chunk, mean over 1620 chunks. Severity: 5 = reads as a bug at walking distance.

| Id | Cut | Per chunk | Severity | In the fixed shots |
|---|---|---:|---:|---|
| A | Open edge on an Office \| Level 0 border | 0.89 | 5 | shot2 (ahead and at left), shot4 (right) |
| B | Arch on a border, jambs half paper, half paint | 0.37 | 4 | shot1, shot4 (far) |
| C | Flat seam: paper switches mid-plane | 0.61 | 4 | shot2 |
| D | Corner-post z-fight stripe | 3.17 | 3 | close frames only (`cut_post_*`) |
| E | Split free wall end | 0.17 | 2 | close frames only |
| F | Door on a border (always Level 0 Low \| Office, also a 0.5 m step) | 1.37 | 2 | shot3 |
| G | Window on a border | 0.33 | 2 | `cut_window_*` |
| H | Solid border wall | 2.28 | 1 | shot4 |
| I | Seam hidden in an inside corner | 2.76 | 0 | — |
| J | Height step through a door or window | 5.81 | 2 | shot3 |
| K | Height step behind a wall | 4.94 | 0 | — |
| L | Grime band set for the taller side | 10.74 edges | 1 | not framed |
| M | Floor material change | 4.58 m | (in A, B, F) | shot1, shot2 |
| N | Ceiling grid change at the same height | 2.67 m | (in A) | shot2 |
| O | Lamps: same colour both sides, Office +10 % | per cell | 1 | all |
| P | Columns, bulkheads | 0 | 0 | — |
| Q | Baseboards: none on map walls | — | 0 | — |
| R | Title stream to map handoff | 1 per run | 1 | `cut_handoff_*` |
| S | RoomStream rule change | 0 today | 3 if played | `cut_stream_*` |

Frameless crossings (A + B) occur 1.26 times per chunk and appear in 32 % of chunks. Theme-border edges number 5.24 per chunk. Office is 16.3 % of all cells.

---

## 2. Rules that hold for all five variations

### 2.1 Layout freeze

No variation changes zones, the maze, rooms, modules, columns, edge kinds or door positions. This does two jobs:
- the four fixed shots frame the same borders before and after;
- navigation, the Relay and the 66 map interaction tests are untouched, except by Neck, which adds collision mass.

Each pre-render proves this: the `-plan` JSON round each eye must match `logs/shot<k>_plan.json`.

An inserted connector corridor (new cells between themes) was considered and left out. It would move the maze, the zones and every shot. Neck is its compressed form that keeps the layout. A full connector stays a later generator option for the map chat if Red likes Neck.

### 2.2 The newer layer owns the joint (default)

Office is read as the 1990 fit-out built into an older Level 0 (`02` Rule 5, Kane FF#3 34:21). Where one finish must wrap a jamb, a post or a wall end, it is Office paint. R3's door spec already follows this: "the office fit-out installs the doors on its own boundary" (`interactables/10_spec.md`). Red can reverse it, but every variation below assumes it.

### 2.3 B0: two bug fixes in every variation

These are bugs, not looks. Every pre-render includes them, so the comparison is fair. Frame also renders B0 alone (tag `b0`).

- **B0.1 Corner ownership (cut D, 3.17 per chunk).** This applies at a cell corner where wall pieces of different face finish meet. That means any two-skin split piece, or single pieces of different materials.
  - Two pieces must never overlap in the 0.16 × 0.16 m post square.
  - If two collinear pieces meet (a straight run), they keep their 0.08 m extensions. Every perpendicular piece stops at the post face (`mayExtendStart`/`mayExtendEnd` = false).
  - At an L corner (no collinear pair), the piece whose exposed outside face is Office extends over the post, and the other stops.
  - Corners with one finish stay as today.
- **B0.2 Grime band per side (cut L, 10.74 edges per chunk).** Each face of a height-border wall is built in its own side's height-class block, so its `_CeilingHeight` is its own ceiling. Where both sides use the same material, two 0.08 m skins are still built.

### 2.4 Contract shape

Everything that touches walls, openings, floors, ceilings, lamps or collision lives in `FrontRoomsMap/*`. The map chat owns it, so each change is a **contract request**. To keep the map-side diff small, each variation is written as two parts:
- a **visual-owned kit**, `Scripts/Rendering/Transitions/FrontRoomsTransitionKit.cs`, a pure function from an edge or face descriptor to render-only boxes and meshes per material;
- **one or two hooks** in `BuildEdge`, `BuildInto` or `BuildFixture`, which pass the descriptor and append the result to the map's builders.

This follows the pattern the outlets task (N3) uses. The pre-render clones mark their hooks `// TRANSITION <key>`, and those lines are the draft contract.

| Owner | What |
|---|---|
| Map chat | `Resolve`, `BuildEdge`/`Piece`, `BuildInto`, `BuildFixture`, `AddZoneGrades`, collision, `ModuleUnits` constants, `LEVEL_MODULE_SPEC.md`, Dress API keep-clear, lamp interface v1 |
| Visual chat | the transition kit, materials (`Resources/Surfaces` via RenderSetup), textures (`Tools/lookdev`), the `FrontRooms/Surface` shader, Blender kit meshes, lamp look (colours), post grade, R3 frames |
| Wallpaper chat, 平面视觉 | the print and EGRESS ink on the Level 0 paper: Renovation and Drift remove paper in places, so ink placement must avoid or accept that |

### 2.5 WebGL track

Desktop is the reference, built at top spec. WebGL changes are WebGL-only.

No variation may add per-renderer `MaterialPropertyBlock`s, because they already break SRP batching on WebGL (`webgl/` baseline). Use materials, keywords and global textures instead.

### 2.6 Era lock

The game is set in 1990: no trademarks, and nothing printed after 1990. All the pieces below existed by 1990:
- hollow-metal frames;
- corner beads;
- rubber cove base;
- aluminium carpet bars;
- 2×2 and 2×4 lay-in ceilings;
- carpet tiles (from 1973);
- vinyl corner guards (from 1969);
- cool-white F40 tubes;
- beige masking tape.

Blue painter's tape is not cleared: its date is unverified, and the existing `Kit_DoorwayStuds` uses it.

### 2.7 Other tracks this touches

- **R3 (door and window kit).** Frame and Neck use R3's `Kit_DoorFrame_Steel` (member OF-F) on border doors. If the FBX is not in the clone, they use a proxy built to R3's envelope.
- **N3 (outlets).** Outlets sit 18 in (0.46 m) to the bottom. A 0.10 m base does not clash.
- **Q4 (title seam).** Not a theme border; unchanged.
- **Lamp interface v1** (map chat, agreed): Light lead's border rule should go through `SetLampMode` when it exists.

---

## 3. The five variations

### V1 · Frame: the frame owns the joint (`frame`)

**Idea.** Every Office | Level 0 change sits on a built piece that the Office side owns: a cased arch, a portal across an open edge, a steel door frame, a pilaster cap, a corner guard, and a base on Office walls. The finishes still change on the cell line, but always on an object.

**Research basis.**
- `01`: P1 and P2. Kane FF 6:52 (a fire door), FF#3 34:21 (the jamb in one finish) and Static Dead End 1:58.
- Valve's "area in common".
- Skyrim: one door-frame size and "glue kits".
- `02`: Rules 1, 3, 4 and 5, and kit K1–K10. The `rb_01` portal (Interiors, June 1990) puts the carpet change on the portal line.

**What you see:**

| Cut | What you see |
|---|---|
| Open edge | A portal: two slim pilasters (0.12 m) against the side walls and a header at 2.55 m. Each side keeps its paper up to the pilaster, and a thin aluminium bar marks the carpet change. The ceiling grids stop against the header. |
| Arch | A cased opening. Jambs and head are one Office paint, with a 6 mm round bead on each edge. The paper stops at the bead, and a bar sits on the floor. |
| Door | A dark-bronze steel frame with casing on both faces, a 16 mm stop and a 12 mm saddle (R3). The Office base stops against the casing. |
| Flat seams, wall ends | Covered by the portal pilasters. Elsewhere a 0.20 m pilaster cap. |
| Office outside corners | Beige vinyl corner guards. |

**Mechanism.** A kit pass at border edges (boxes and small bevelled meshes) and base runs on Office faces. The arch reveal needs a 0.012 m inset of the wall pieces, so the lining does not z-fight.

**Contract.**

| Map chat | Visual chat |
|---|---|
| One `BuildEdge` hook for theme-border edges | Kit meshes |
| 0.012 m `BeadInset` on pieces beside border arches | Bead profile |
| Office face extents for the base | Material choices |
| B0 | R3 frames |
| New constants: `BeadInset` 0.012, `PortalPilaster` 0.12, `PortalHeaderDrop` = `BulkheadDepth` 0.35, `BarWidth` 0.035; base = `CoveHeight` 0.10 | |

**Gameplay.** None.
- All pieces are render-only. Portal pilasters stand 0.12 m from the wall face, inside the player's 0.30 m radius band.
- The clear width stays 2.44 m or more.
- The map chat may add the pilasters to collision; nav would still pass.

**Cost.**
- About 0.3–0.6 k triangles per opening, plus about 0.3 k per chunk of base.
- 0–2 extra draws per 6 m block that holds a border: Prop_Aluminium, and Cove_Base where a block has none yet.
- 0 MB of new texture; it re-uses Office_Wall, Cove_Base, Prop_Aluminium and Prop_Vinyl.
- WebGL is the same: about 5–15 extra draws per frame near borders, under 0.5 % of about 3,200.

**Era.** Exact 1990 commercial practice.

**Risk.** It can read as tidy and "too correct" for the Backrooms. It fixes the bug but adds no mood.

### V2 · Renovation: the newer layer is still going on (`renovation`)

**Idea.** Office is a 1990 fit-out still being built into Level 0. Every border shows the work on the Level 0 side, and the Office side is finished.
- Within three paper drops (2.25 m) of the border, the paper steps down from intact, to stripped, to primed drywall, to fresh Office paint.
- The carpet is cut back 0.6 m with the slab showing.
- Ceiling tiles are missing or new.
- A few work props stand about.

By the time you reach the line, both sides are already paint, so the line itself disappears.

**Research basis.**
- `01`: P5 and P10. Wikidot Red Rooms (the paper peels to show the next colour), Kane Pitfalls 11:30 (stained paper) and the Async insert.
- The 2002 photo shows a store under fit-out.
- The A24 set shows painter's tape and a half-finished wall (`Research/week02/kit-references/a24/a24_trailer_0115_blue_painters_tape.jpg`).
- `02`: §7 (paint over paper, GA-214 patch flashing from 1990, moved partitions) and Rule 6.
- The existing `Kit_DoorwayStuds` was modelled from A24 Still A.

**What you see:**

| Cut | What you see |
|---|---|
| Open edge | Both corridor walls step through the stages over the last 2.25 m. Then a 0.6 m strip of bare slab with trowelled adhesive, and loose carpet tiles. Overhead, a missing tile and two new white ones. |
| Arch | A raw reveal: bare drywall with metal corner beads showing and compound spots. The stages run round it. |
| Door | On the Level 0 face, the paper is cut ragged round the new steel frame, with a halo of joint compound. The Office face is finished. |
| Wall end | A raw cut end: a paper lip over the gypsum edge. |

**Mechanism.**
- Band faces are split into 0.75 m drops on the paper's own world grid, each with a stage material. There are no overlay quads.
- An authored torn-edge mesh, a slab strip, tile meshes and props.
- Textures are authored (made with `gen_surfaces.py`-style tools and the CC0 sources already on disk), not shader noise.

**Contract.**

| Map chat | Visual chat |
|---|---|
| A hook that hands the kit each Level 0 face run near a border (ends, normal, height, distance along the run, run hash, chunk tier) and each crossing's floor and ceiling cells | 4–5 surface sets |
| Splitting those faces into drops | Torn-edge meshes |
| A hole in one ceiling slab tile | Props |
| Props kept clear by the Dress API | The WebGL tier |
| B0 | |

**Gameplay.** None. Props are render-only and stand against walls outside the 1.0 m keep-clear.

**Cost.**
- About 1.5–6 k triangles per dressed run, including props.
- 4–6 extra draws per block with a dressed run, plus shadow passes for props.
- Texture memory: about 50–65 MB on desktop (4 sets at 2048² plus a 2048 × 512 alpha atlas) and about 15–20 MB on WebGL (1024²).
- On WebGL the tear meshes and props beyond 10 m are dropped. That gate is WebGL-only.

**Era.** Period renovation methods. Unbranded buckets, and beige masking tape.

**Risk.**
- If it appears at every border, the map reads as a building site. So the recommendation uses it on 25–35 % of border runs.
- It also removes paper where the EGRESS ink might sit.

### V3 · Neck: a thick, lined threshold (`neck`)

**Idea.** Every frameless crossing (open edge or arch) becomes a short deep doorway: a 0.60 m deep block centred on the line, with a 1.8 m opening for an open edge or the arch's own width.
- The whole inner reveal is Office paint.
- The Level 0 paper stops at the outer corner.
- The Office carpet starts at the Level 0 mouth.
- A 2.2 m soffit hides both ceiling grids.

Doors get a 0.30 m lined recess on their stop side, with a 2.4 m soffit. This is Kane's FF#3 recess, and the transition becomes a small space of its own.

**Research basis.**
- Kane FF#3 34:18–34:28: the recess, the newer finish on the jamb, the base and carpet running through, and its own light.
- Valve's "area in common" and RE Village's neutral hub, both scaled down.
- Fandom Level 4: entered through an "office sector" metal door or long dry corridors.
- Dsc00159: an arcade as the divider.
- `02` Rule 4: the threshold is thick.

**What you see:**

| Cut | What you see |
|---|---|
| Open edge | A deep doorway across the corridor. Both corridor walls die into its cheeks, so the flat seams are buried. |
| Arch | The same throat, round the arch. |
| Consecutive open edges in a room | An arcade of deep openings. |
| Door | It sits at the back of a lined recess whose soffit is the Low ceiling height. The height step becomes a built soffit. |
| Window | Unchanged, so the climb path stays the same. |

**Mechanism.** New collision geometry in `BuildEdge` at border Open and Arch edges and at border doors (stop side), plus linings, beads, a carpet overlay and a bar.

**Contract.**

| Map chat (large) | Visual chat |
|---|---|
| Neck and recess geometry with collision | Lining, beads, carpet overlay, bar |
| New constants: `NeckDepth` 0.60, `NeckOpening` 1.80, `RecessDepth` 0.30, `RecessWidth` 1.60, `RecessTop` 2.40 | |
| Dress API keep-clear measured from the neck face (+0.22 m) | |
| Nav 60/60, interaction and designer tests rerun | |
| B0 | |

**Gameplay: yes.**
- Open-edge crossings narrow from 2.84 m to 1.80 m.
- Each neck takes 0.22 m from both cells along 3 m.
- Sightlines get shorter.
- The Relay (radius 0.30, height 2.05) still fits every opening (at least 1.1 × 2.2).
- Door swing is safe, because the recess is on the stop side only.

**Cost.**
- About 0.4 k triangles per neck.
- 0–1 extra draws per block.
- Collision boxes merge into the existing chunk mesh collider (0 new colliders).
- 0 MB.
- WebGL is the same.

**Era.** Thick chase walls and smoke vestibules are period-correct.

**Risk.**
- Heavy if frequent (1.26 necks plus 1.37 recesses per chunk).
- Cells next to a neck feel cramped.
- More work for the map chat.

### V4 · Drift: the place half-remembers (`drift`)

**Idea.** The two looks drift into each other over about ±4.5 m. They mix in whole building units, so one zone's paper drops (0.75 m), carpet tiles (0.6 m) and ceiling tiles (0.6 × 1.2 m) appear in the other, more often near the line.
- There are no soft blends and no noise edges: every change sits on a real unit joint.
- At the line the mix is 50/50, so no straight line marks the change.

**Research basis.**
- Fandom Level 0: hallways "gradually transition" into Level 1.
- Fandom Level 37 drifts into 37.1 "without one taking notice".
- Wikidot Red Rooms.
- A24 designer Danny Vermette: the deeper you go, the less things are "remembered".
- Kane Pitfalls 5:55: the same architecture with a new palette.
- `02`: real carpet dye lots, carpet-tile patches and ceiling tiles chosen per room (`rb_05`, `rb_06`, `rb_07`).

**What you see:**

| Cut | What you see |
|---|---|
| Open edge, flat seam | Lost in a band of mixed drops, squares and tiles. |
| Arch | The arch has one finish (chosen per edge), with mixed units round it. |
| Door, window | Unchanged frames; the walls round them mix. |

**Mechanism.**
- A global theme-field texture with one texel per cell: the signed distance to the other theme.
- A `_FR_DRIFT` keyword on the six theme materials, each carrying its counterpart texture set.
- Per unit, `pOffice = smoothstep(−W, W, s)` is compared with a hash.
- Reveal and end-cap linings use non-drift copies, because 0.75 m drops line up with the cell lines.

**Contract.**

| Map chat (tiny) | Visual chat (nearly all of it) |
|---|---|
| `ChunkBuilt`/`ChunkReleased` events, already queued for the RT track | Shader, field, materials |
| The reveal lining in `BuildEdge` | |
| B0 | |
| `ZoneOf` is already public | |

**Gameplay.** None.

**Cost.**
- 0 triangles and 0 draws.
- For pixels within W of a border: about 4 extra texture samples and about 25 extra ALU (estimate, about 0.1–0.3 ms at 1080p on desktop, to be measured).
- About 3 KB of field texture; 0 new texture sets, because both zones' sets are already loaded.
- One more `shader_feature_local`.
- On WebGL the GPU cost is similar, and WebGL is CPU/draw-bound, so it is fine there.

**Era.** Fine: mixed dye lots and replacement tiles are real. The spread itself is uncanny by design.

**Risk.**
- It is a shader effect, and Red finds shader-made damage fake. That is why it uses units, not blends.
- It does not put the change on a built object, so it breaks the research's main rule.
- It also knocks paper, and with it the ink, out of single drops.

### V5 · Light lead: the light changes first (`lightlead`)

**Idea.** Office lamps become cool white and 9 % brighter.
- Level 0 cells with a frameless crossing into Office get a dead lamp.
- Level 0 cells whose crossing is a door or window get a dim lamp.
- Office cells at a crossing are forced steady.

You approach the border through shadow and see the next zone lit in its own colour. The surface cut sits where the light already changes, and mostly in the dark.

**Research basis.**
- Kane FF 2:09–2:21 (a green pocket) and 4:44–4:52 (a dark lead-in).
- FF#3 34:21 (the new side is cooler and brighter).
- Informational Video 7:08–7:24.
- Static Dead End 2:10: a door marks a light zone, not a material zone.
- The Complex 4:46; POOLS (light from the next space comes first); Wikidot Blackout Zones.
- 1990 offices used cool-white F40 tubes.

**What you see:**
- At open edges and arches: a dark Level 0 cell and a lit cool opening.
- Through doors: a dim, warm Level 0 room seen from a steady, cool Office.
- Close up, the split jamb and the flat seams are still there, lit from the Office side.

**Mechanism.** Only `BuildFixture` changes: a lamp colour per theme, a border lamp rule, and a cool copy of the Office lens.

**Contract.**

| Map chat | Visual chat |
|---|---|
| The border rule, through lamp interface v1 (`SetLampMode`) or in `BuildFixture` | The colour values and lens (the lamp look) |
| `lampColor` per theme | Post-grade tuning |
| B0 | |

**Gameplay.**
- About 1.1 dead and 1.6 dim lamps per chunk (about 1.7 % and 2.5 % of lamps).
- The player sees less near borders.
- The Relay's lamp-flicker warning (`LampFx.Warn`) cannot show in a dead cell. 系统设计 should know.
- Colliders: none.

**Cost.**
- 0 triangles and 0 draws.
- About 1–2 fewer additional lights per chunk, a small gain on desktop and on WebGL.
- One lens material.

**Era.** Exact: cool white in offices, warmer in retail.

**Risk.**
- It hides rather than fixes.
- If every border is dark, darkness becomes a tell, though that may be a feature for wayfinding.

### Not proposed

**A "canon anomaly" seam** (a hard cut styled as misregistration or a doubled strip) was considered and dropped. The research says it would not read as intended:
- no IP uses a single-edge glitch;
- Kane's glitches cover whole rooms and are events (Static Dead End 2:36–4:00);
- our seam already reads as a bug, which is Red's complaint.

It could return as a scripted one-off event at high tiers, never as the border style.

---

## 4. Every cut type, every variation

| Cut (per chunk) | V1 Frame | V2 Renovation | V3 Neck | V4 Drift | V5 Light lead |
|---|---|---|---|---|---|
| A open edge (0.89) | Portal: pilasters 0.12, header 2.55, bar | 2.25 m stage band on both walls, slab strip, tiles out or new, props | 0.6 m neck, 1.8 m opening, soffit 2.2 | ±4.5 m unit mix; line lost | Level 0 cell dark, Office lit cool |
| B arch (0.37) | Cased: Office paint reveal, 6 mm beads, bar | Raw reveal, exposed beads, band round it | Neck round the arch opening | Reveal one finish (hash), mix round it | Level 0 cell dark; jamb still split close up |
| C flat seam (0.61) | Lands on the portal pilaster; else a pilaster cap 0.20 × 0.04 | Absorbed: paint meets paint at the line | Buried in the neck cheek; else a stub 0.30 × 0.08 | Dissolved in the drop mix | In shadow; unchanged |
| D post z-fight (3.17) | B0.1 + Office corner guard | B0.1 (post in primer if in band) | B0.1 | B0.1 | B0.1 |
| E split wall end (0.17) | Office end cap with beads | Raw cut end, paper lip | Neck cheek or stub | Cap in one finish (hash) | Unchanged |
| F door (1.37) | R3 steel frame, stop, saddle; Office base stops | Paper cut ragged round the frame, compound halo | Frame at the back of a 0.30 m recess, soffit 2.4 | Frame unchanged; walls mix | Level 0 side dim, Office steady cool |
| G window (0.33) | R3 steel window frame | Same halo as doors | Frame only (climb path unchanged) | Unchanged; walls mix | Level 0 side dim |
| H solid wall (2.28) | Unchanged; Office base on the Office face | Band only near its ends | Unchanged | Faces near the border mix | Unchanged |
| I inside corner (2.76) | Unchanged (correct) | Unchanged | Unchanged | Mixed | Unchanged |
| J step via opening (5.81) | Frame; the wall over the opening stays the step face | Unchanged | Recess soffit at 2.4 = built step (border doors) | Unchanged | Unchanged |
| K step behind wall (4.94) | — | — | — | — | — |
| L grime band (10.74 edges) | B0.2 | B0.2 | B0.2 | B0.2 | B0.2 |
| M floor (4.58 m) | Aluminium bar or saddle on the line | Carpet cut back 0.6 m, slab strip | Change at the Level 0 mouth, bar | 0.6 m squares mix | In the dark cell |
| N ceiling (2.67 m) | Hidden behind the portal header | Tiles missing or new in a 1-cell band | Hidden by the soffit | Tiles mix | In the dark cell |
| O lamps | Unchanged | Unchanged | Unchanged | Unchanged | This is the change |
| P columns, bulkheads | — | — | — | — | — |
| Q baseboards | Office base on all Office walls (Level 0 none) | Unchanged | Office base through the neck | Unchanged | Unchanged |
| R title handoff (1/run) | Level 0 \| Level 0: nothing new (R3's door family covers the start door later) | n/a | n/a | n/a | n/a |
| S RoomStream (0 today) | Same kit on stream door walls if it returns | Could dress Shift and Office rooms | Not needed | Field could drive Shift rooms | Rule colours already differ |

---

## 5. Cost summary (estimates; the pre-renders measure them)

| | Geometry | Draws (desktop) | Lights | New texture memory | Shader | Colliders | WebGL |
|---|---|---|---|---|---|---|---|
| V1 Frame | 0.3–0.6 k tris per opening, about 0.3 k per chunk of base | +0–2 per border block | 0 | 0 MB | none | none | same; +5–15 draws/frame near borders |
| V2 Renovation | 1.5–6 k tris per dressed run | +4–6 per dressed block | 0 | about 50–65 MB | none | none (props render-only) | 1024² sets (about 15–20 MB); no tear meshes or props past 10 m |
| V3 Neck | about 0.4 k tris per neck | +0–1 per block | 0 | 0 MB | none | yes, merged into the chunk mesh | same |
| V4 Drift | 0 | 0 | 0 | about 3 KB | +4 samples, about 25 ALU in band pixels | none | same (GPU-side only) |
| V5 Light lead | 0 | 0 | −1 to −2 per chunk | one lens material | none | none | small gain |

---

## 6. Pre-render protocol (common to every brief)

**Paths:**
- SCR = `/private/tmp/claude-501/-Users-redwang-Desktop-ArtCenter-Fall26T7-EGAM-401A-01-Individual-Game-Project/5656cffd-bc90-45f6-86a3-09b26549df8d/scratchpad`
- DOC = `/Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01 Individual Game Project/Frontrooms3D/Documentation/research/level_transitions`

**Steps:**

1. **Copy.** Run `cp -Rc SCR/proj_trans SCR/proj_trans_<key>` and remove the copy's `Temp/`.
   - Never re-sync from Red's project. Red's `FrontRoomsMapWorld.cs` changed at 12:21 today; the clone is the frozen BEFORE base (clone MapWorld md5 `e5204f9c7a7e07f9fa6d584915e6297e`).
   - Never open Unity on Frontrooms3D.
2. **Harness unchanged.** `Assets/Editor/Audit/FrontRoomsTransitionAudit.cs` must keep md5 `76a624e3217630e637bdb381af7ca5b1`. `DOC/shots.json` is unchanged.
3. **Baseline before any edit.** Run ShotsBatch with `-tag basecheck`. Compare the four JPGs with `DOC/images/shot<k>_before.jpg` using `cmp`. They must be byte-identical; if not, stop and report.
4. **Implement B0 (§2.3) plus the variation, in the copy only.**
   - Put the variation logic in new files under `Assets/Scripts/Rendering/Transitions/`.
   - Keep edits to `FrontRoomsMap/*` to small hooks marked `// TRANSITION <key>`.
5. **Layout freeze.** Render with `-plan` and diff each `<id>_plan.json` against `DOC/logs/shot<k>_plan.json`. They must be identical.
6. **Asset preparation** (materials, textures, meshes) goes in separate Unity calls before the render.
   - Never run two Unity processes on one copy. Each call may take up to 600000 ms. Graphics must be on (no `-nographics`).
   - Unity: `/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath <copy> -executeMethod <Class.Method> -quit -logFile <log>`.
   - Python with numpy/PIL: `/usr/bin/python3`.
   - Blender: `/Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup --python <script>`. New Blender modules go in the **copy's** `Tools/Blender/frontrooms_kit/assets/` with the prefix `trans_`. They move to Red's project only after Red's pick.
   - The orchestrator should not run more than two Unity renders at once, because Red is working on the same Mac.
7. **Render all four shots in one call:** `-executeMethod FrontRoomsTransitionAudit.ShotsBatch -quit -logFile <log> -shots DOC/shots.json -out SCR/shots_<key> -tag <key> -plan`.
   - Read the log for errors.
   - Compare each `<id>_<key>.txt` lamp line with the before run (lit 84 / 82 / 83 / 88).
8. **Look at all four frames.** Fix and re-render the whole set if anything:
   - floats, clips or z-fights;
   - touches the camera (near plane 0.06);
   - does not read as the brief says.
9. **Optional close frames.** `EvidenceBatch -out SCR/evidence_<key> -perType 3 -noCount` re-renders the cut evidence with the same candidates (the layout is frozen). Its file names match `DOC/images/cut_*.jpg`. Copy only the post, seam, wall-end and step frames you need, as `var_<key>_close_<type>.jpg`.
10. **Outputs.** These, and nothing else, go into Red's project:
    - `DOC/images/var_<key>_shot1..4.jpg`: the harness JPGs, q85, 1920 × 1080.
    - `DOC/images/var_<key>_sheet.jpg`: four rows, before on the left and after on the right, labelled, 1920 px wide, q85.
    - Optional close frames.
    - `DOC/1N_var_<key>.md`, with N = 1..5 in the order of §3, containing:
      - what was built, with numbers;
      - the diff of the map files against the clone base (the draft contract);
      - new assets and their sizes;
      - measured counts within 46 m of each eye: renderers, materials, triangles and lit lights, from your own stats editor method in the copy, not the harness;
      - texture memory added;
      - deviations from the brief;
      - the era check;
      - the log paths.
11. **Limits.**
    - No downloads and no Figma writes.
    - Era lock 1990: no trademarks, nothing printed after 1990.
    - Highest spec for anything modelled: real profiles, bevels, and a LOD0 that holds up at 0.3 m.

**The four fixed shots** (map space, root (4992, 0, 4992), seed 516574485, eye 1.62 m, FOV 76):
- **shot1:** the arch on Z = 609.0 between (264,202) Level 0 and (264,203) Office, plus the arch on X = 795.0 to (265,202) Office.
- **shot2:** the corridor crossing the open edge on Z = 588.0 between (255,195) Level 0 and (255,196) Office. There is also an open edge on X = 765.0 between the eye cell (255,194) and (254,194) Office.
- **shot3:** the door on Z = 600.0 from (253,199) Office 2.9 m into (253,200) Level 0 Low 2.4 m. In the clone the leaf swings into Level 0.
- **shot4:** the wide view. The shot1 arch is 7.5 m ahead, and the open edge on X = 798.0 between (265,201) Level 0 and (266,201) Office is at right.

---

## 7. Pre-render briefs

### 7.1 V1 Frame (tags `b0`, then `frame`)

**Step 0.** Implement B0 only and render all four shots with `-tag b0`. Output `var_b0_shot1..4.jpg` and `var_b0_sheet.jpg`. Then add V1 and render `-tag frame`.

**Definitions.** A theme-border edge is one whose two cells have different `ZoneOf().theme`. The Office side owns every piece.

1. **Arch on a border becomes a cased opening.**
   - Shorten the wall pieces beside the opening by 0.012 m at the opening edges, and raise the head piece's bottom by 0.012 m.
   - Fill that gap with a lining: two jamb strips and one head strip, each 0.012 thick and 0.16 deep, with 6 mm quarter-round beads (8 segments) on both arrises, all in Office_Wall.
   - The reveal becomes one Office paint finish, and the Level 0 paper stops at the bead.
   - Floor: an aluminium binder bar, 0.035 wide × 0.005 high with a 3 mm rounded top, on the line, as long as the clear width. Prop_Aluminium, shadows off.
2. **Open edge on a border becomes a portal.**
   - Two pilasters, 0.12 wide along the edge × 0.20 deep (0.02 proud of each wall face) × full ceiling height, at the two ends of the edge, against whatever wall continues there.
   - A header between them, 0.20 deep, from ceiling − 0.35 m (2.55 on Standard) to the ceiling.
   - 6 mm beads on every arris, in Office_Wall.
   - Consecutive border open edges on one line share a 0.24 m pilaster at each shared corner and one continuous header, forming an arcade.
   - Render-only (clear width 2.44 m in a corridor).
   - The bar runs between the pilasters.
   - Ceiling: both grids die into the header faces, with a 0.022 × 0.022 white wall angle on each header face at the ceiling.
3. **Door on a border gets a steel frame.**
   - If `Kit_DoorFrame_Steel` exists in the copy, place it as `interactables/10_spec.md` §1.4 says (member OF-F).
   - Otherwise build a proxy from that envelope:
     - casing on both faces, 0.0795–0.105 m off the wall centre, 0.075 wide;
     - jambs 0–2.175 m, head 2.098–2.175 m, mitred;
     - a 16 mm stop on the push side;
     - a saddle 0.152 × 0.012 with 1:2 bevels, in Prop_Aluminium.
   - Frame: Painted_Metal tinted dark bronze, about sRGB (0.20, 0.17, 0.13). Today's Cove_Base trim stays hidden inside.
   - Window on a border: the same casing proxy round the 1.4 m × (0.35–2.0) opening, or R3's Office window frame if present.
4. **Wall ends and leftover flat seams.**
   - End cap: one Office_Wall lining with beads round the 0.16 m end face.
   - Flat seams not covered by a portal: a pilaster cap 0.20 wide × 0.04 proud, full height, beaded, centred on the seam.
   - Both render-only.
5. **Corner guards** on every exposed outside corner of an Office face on a border wall: 0.05 × 0.05 m wings, 1.22 m tall above the base, beige vinyl (Prop_Vinyl or a beige copy), 2 mm thick, 3 mm nose radius. Render-only.
6. **Cove base**, 0.10 high × 0.006 proud, Cove_Base, on every Office wall face in the built chunks.
   - It stops 0.005 m short of casings.
   - It wraps outside corners, portal pilasters and end caps.
   - No base on Level 0 walls. Shadows off.
7. Lamps, floors (other than bars and saddles) and ceilings are unchanged.

**What each shot must show:**
- **shot1:** both arches cased, with an Office paint reveal, beads and a bar; the Office base visible beyond.
- **shot2:** a portal on Z = 588.0 (pilasters at about X 765.08–765.20 and 767.80–767.92, header 2.55–2.90). Both corridor walls change paper on the pilasters, and a bar sits on the line. On X = 765.0 at the left, the open edge beside the eye (row 194) is also a portal and the arch in row 195 is cased.
- **shot3:** the door in a dark-bronze frame with casing, stop and saddle; the Office base stops at the casing.
- **shot4:** the cased arch far ahead, the portal on X = 798.0 at right, and the Office base.

**Report:**
- counts over the 25 built chunks: cased openings, portals, frames, caps and guards, plus metres of base;
- added triangles and renderers.

**Draft contract:**
- one `BuildEdge` hook (kind, a, b, themes, heights, opening width, centre, top, sill, along, across, start) to `FrontRoomsTransitionKit`;
- `BeadInset`;
- base runs on Office faces;
- new `ModuleUnits` constants.

### 7.2 V2 Renovation (tag `renovation`)

**The band.**
- It covers Level 0 wall faces in the same wall run as a border, within 3 paper drops (2.25 m) of the border line.
- It also covers the Level 0 face of a border wall within 2.25 m of a border opening.
- Drops are world-anchored at multiples of 0.75 m along the wall, in the shader's PlanarFrame (u = dot(posWS, t)), so a 3 m cell holds 4 drops and they match the paper's own seams.

**Stage by drop index n from the border** (n = 0 touches it):

| n | Stage | Material |
|---|---|---|
| 0 | Fresh Office paint | Office_Wall_Fresh: Office_Wall + 4 % value, a roller-lap normal at 0.23 m, a cut-in line 0.05 m from corners |
| 1 | Primer over taped drywall | Wall_Primer: flat warm white, 0.05 m joint tape every 1.22 m, screw spots every 0.30 m on the 0.41 m stud lines, joint-compound flashing |
| 2 | Stripped | Wall_Stripped: drywall face paper with grey-brown backing remnants, amber adhesive streaks and a few gouges |
| 3 and up | Paper intact | today's paper |

**Variation per run (by run hash):**
- 60 % of runs as above;
- 25 % stop at stripped (n = 0..1 stripped);
- 15 % have only n = 0 stripped.

Build the band faces as per-drop boxes in the copy's `Piece()`. Use no overlay quads.

**Torn edge.** On the boundary between the last intact drop and the stripped one, add an alpha-tested mesh:
- a ragged contour 5–40 mm deep, with 2–3 longer tongues;
- a 0.3 mm lip that shows the paper's white base layer;
- the top 0.10 m curling 15–25 mm off the wall;
- 1–2 hanging scraps per run.

Build it in Blender (`trans_paper_tear.py`) or as a mesh with an authored alpha atlas (Paper_TornEdge, 2048 × 512). No shader noise.

**Openings:**
- **Border arches:** a bare drywall reveal with exposed galvanised corner beads (0.012) and compound spots, in Wall_Primer. The beads use Prop_Aluminium.
- **Border doors and windows:** on the Level 0 face round the casing, a 0.08–0.20 m halo of joint compound and a ragged paper cut-line. The Office face is finished.

**Floor.**
- The Level 0 loop pile ends on a straight cut 0.6 m before the line, across the opening or the corridor.
- The strip shows Slab_Adhesive: grey concrete, amber trowel ridges at a 3 mm notch, and a chalk line.
- A 5 mm frayed edge mesh finishes the cut carpet.
- Office tiles run to the line.
- 2–4 loose Office tiles (0.6 × 0.6 × 0.007, Office_Carpet, turned 2–12°) lie on the strip or lean in a stack against a wall.

**Ceiling.** In each Level 0 cell at a border crossing:
- one 0.6 × 1.2 tile is missing, showing a dark plenum box 0.25 m deep with a hint of duct and wire. Build that cell's ceiling slab in pieces round the hole; if that is too costly, use the next item only;
- one tile is pushed up askew;
- in the 0.6 m nearest the line, two new 2×2 tiles (Office_Ceiling, 6 % brighter) sit in the old 2×4 grid.

**Props.** Render-only, against walls, at least 1.0 m clear of openings, 1–2 per run:
- from the existing kit where possible: `step_stool`;
- new: an unbranded 5-gallon compound bucket (`trans_bucket.py`) and a paint tray with roller (`trans_paint_tray.py`);
- a folded drop cloth;
- a box of carpet tiles (CardboardSet001);
- paper scraps;
- beige masking tape (Tape005 on disk).

No labels and no brands.

**Office side.** Unchanged. Lamps unchanged.

**Textures.**
- 2048² sets (A, N, S) made with `/usr/bin/python3` in the style of `gen_surfaces.py`, in the copy's `Tools/lookdev`.
- World-projected through FrontRooms/Surface. Tile sizes must divide 192 m.
- Reuse the CC0 sources in `Tools/lookdev/cc0_src` (Tape005, Leaking001/006, Smear007, SurfaceImperfections*, beige_wall_001, CardboardSet001, polystyrene) and credit them in the report.

**What each shot must show:**
- **shot1:** drops stepping from paper to stripped to primer to fresh paint toward both arches, a visible torn edge, a bare beaded reveal, the slab strip before the arch, a missing tile over the eye cell, and a stool against the left wall.
- **shot2:** both corridor walls stepping down over the last 2.25 m before Z = 588.0; the slab strip and loose tiles at the line; a missing and a new tile overhead.
- **shot3:** through the door, the Level 0 paper cut ragged round the frame with a compound halo, and the slab strip just past the door.
- **shot4:** the band on the Level 0 walls toward X = 798.0 and round the far arch.

**Report:**
- band metres per chunk;
- tear meshes and props;
- triangles and renderers;
- texture memory, with a WebGL 1024² estimate.

**Draft contract:**
- the face-run hook (ends, normal, height, distance along the run, run hash, chunk tier);
- splitting faces into drops;
- a ceiling-slab hole;
- Dress keep-clear.

Note for the wallpaper chat: stripped and painted drops carry no print and no ink.

### 7.3 V3 Neck (tag `neck`)

1. **Open and arch edges on a border become necks.**
   - Replace the wall on that edge with a block 0.60 m deep centred on the line (0.30 each side, so 0.22 beyond today's skins), spanning the full 3 m edge.
   - It has one opening:
     - arch edges keep their hashed width and centre (`ArchOpening`);
     - open edges get 1.80 m (`ArchMaxWidth`), centred;
     - the top is 2.20 (`ArchTop`, or ceiling − 0.20 if lower).
   - Two cheeks and a header to the ceiling, all built with `Solid` so they are in the collision mesh.
   - Faces:
     - the face toward Level 0: L0_Wallpaper;
     - the face toward Office: Office_Wall;
     - the whole 0.60 m deep inner reveal (both cheeks and the soffit): Office_Wall;
     - 6 mm beads on the four mouth arrises.
   - Consecutive border open edges on one line form a run of necks sharing cheeks.
   - Where a corridor wall meets a neck, it dies into the cheek.
2. **Floor.** The change moves to the Level 0 mouth.
   - An Office_Carpet overlay, 0.30 m deep × the opening width, 0.001 m above the Level 0 slab, between the mouth and the line.
   - An aluminium binder bar (0.035 × 0.005) on the mouth line.
   - Office cove base (0.10) runs along both cheek faces inside the neck and on the Office faces. There is none on the Level 0 face.
3. **Ceiling.** The 2.2 m soffit hides both grids. Each grid dies into the neck face, with a 0.022 m white wall angle.
4. **Doors on a border get a stop-side recess.** The stop side is the side the leaf does not swing into; in the clone, shot3's leaf swings into Level 0, so the recess is on the Office side.
   - Thicken the wall by 0.30 m on that side over the full 3 m.
   - Cut a recess 1.60 wide × 2.40 high × 0.30 deep, centred on the door. The door and frame stay on the wall line at the back of the recess.
   - Line it in Office_Wall with beads; the soffit sits at 2.40 m.
   - It goes into collision. Check that the leaf still swings to 95° clear of everything.
5. **Windows** are unchanged. **Flat seams** not at a neck get a pilaster stub 0.30 × 0.08 proud, beaded and render-only. **Posts:** B0.1. **Lamps** are unchanged, with no new real lights.
6. **Layout freeze** still holds: edge kinds are unchanged and `CrossingPoint` stays the opening centre.

**What each shot must show:**
- **shot1:** the Z = 609.0 arch as a 0.6 m deep throat lined in Office paint; the paper stopping at the outer bead; Office carpet from the Level 0 mouth with a bar; the soffit hiding the ceiling change. The X = 795.0 arch is also a neck, with its cheek face 0.70 m right of the eye. Expect it to fill the right edge, and report it.
- **shot2:** a 1.8 × 2.2 m deep opening across the corridor on Z = 588.0, with both corridor walls dying into the cheeks. On X = 765.0 at the left, the open edge in the eye's own cell (row 194) and the arch in row 195 are necks too, 1.45 m away.
- **shot3:** the door at the back of a 0.30 m Office-lined recess with a 2.4 m soffit.
- **shot4:** the far arch neck and the X = 798.0 neck at right.

**Checks:**
- No new geometry is within 0.06 m of any eye.
- Any furnished prop that intersects a cheek is a keep-clear contract item; list it.
- The Relay body (radius 0.30, height 2.05) fits every opening.

**Report:**
- necks and recesses per chunk;
- triangles and collision boxes;
- the narrowing of each crossing (2.84 m to 1.80 m for open edges);
- props needing a deeper keep-clear.

**Draft contract:**
- neck and recess geometry with collision;
- the five new constants;
- the Dress keep-clear change;
- the test reruns.

### 7.4 V4 Drift (tag `drift`)

1. **Theme field.** New file, visual-owned: `Assets/Scripts/Rendering/Transitions/FrontRoomsThemeField.cs`.
   - After the map builds (end of `BuildForCapture`, or each `BuildInto`, in the copy), cover the built window with one texel per cell: 40 × 40 for 5 × 5 chunks.
   - Each texel holds the signed distance in metres from the cell centre to the nearest cell of the other theme, minus 1.5, so 0 lies on the shared line. It is negative in Level 0, positive in Office, and clamped to ±6. Compute it by BFS using `world.ZoneOf`.
   - Store it as an RHalf texture, bilinear, clamped.
   - Globals: `_FR_ThemeField`, `_FR_ThemeFieldST` (world XZ to uv), `_FR_DriftWidth` = 4.5, `_FR_DriftSeed` = the run seed.
   - No `MaterialPropertyBlock`s.
2. **Shader.** In the copy of FrontRooms/Surface, add `shader_feature_local _FR_DRIFT`.
   - New properties:
     - `_FR_ThemeSelf` (0 Level 0, 1 Office);
     - the counterpart set: `_AltBaseMap`, `_AltBumpMap`, `_AltMaskMap`, `_AltTileSize`, `_AltBaseColor`, `_AltSmoothness`, `_AltMacroTone`, `_AltStainStrength`;
     - `_DriftUnit`: walls 0.75 × 1000 (full-height drops), floors 0.6 × 0.6, ceilings the Level 0 tile (0.6 × 1.2). Check that the units fall on the tees of both grids. All units are anchored at world 0 in the same metres as the tile.
   - Per pixel:
     1. unit index = floor(metres / unit), and the unit centre in world from that index;
     2. s = the field at the unit centre, and pOffice = smoothstep(−W, +W, s);
     3. h = hash(unit index, face plane id = round(dot(posWS, n) / 0.04), axis sign, seed);
     4. unitTheme = h < pOffice ? Office : Level 0;
     5. if unitTheme is the material's own theme, shade as today; otherwise shade with the Alt set, using SampleGrad with derivatives taken outside the branch.
   - Enable it on L0_Wallpaper, L0_Carpet, L0_Ceiling, Office_Wall, Office_Carpet and Office_Ceiling, each pointing at its counterpart.
3. **Reveals and ends.** Drops line up with cell lines, so a jamb straddles two drops. On border arches and split wall ends, build the jambs, soffit and end caps as one lining box in a non-drift copy of one theme's material, chosen per edge by hash (50/50). No beads.
4. **Posts:** B0.1. Door and window frames stay as today. Lamps and the post grade are unchanged.
5. **Width.** The main render uses W = 4.5 m. Optional tags if time allows: `drift_w3` and `drift_w6`.

**What each shot must show:**
- **shot1:** through the arch, a few beige loop-pile squares on the Office floor near the line and one or two yellow drops on Office walls. On the Level 0 side, a grey drop and a few blue-grey squares. White 2×2 tiles among the 2×4 near the line. The reveal in one finish.
- **shot2:** the corridor mixed over ±4.5 m round Z = 588.0, with no straight line left.
- **shot3:** mixed drops near the door, and a few grey drops in the Level 0 Low room.
- **shot4:** the X = 798.0 edge lost in a mixed band.

**Checks:**
- No unit shows two finishes.
- No soft or noisy edges.
- No mip seams at unit edges.
- Pixels outside every band are identical to the before frames.

**Report:**
- units switched per chunk;
- GPU time at each eye with and without `_FR_DRIFT` (30 frames each, from your own editor method);
- shader variant count;
- memory.

**Draft contract:**
- `ChunkBuilt`/`ChunkReleased` events;
- the reveal lining in `BuildEdge`;
- B0.

### 7.5 V5 Light lead (tag `lightlead`)

There is no geometry change and no surface change besides B0.

1. **Lamp colour by theme.** Add `lampColor` to `ThemeMaterials`.
   - Level 0 stays at (1.00, 0.96, 0.88), intensity 5.0.
   - Office becomes (0.90, 0.96, 1.00), intensity 6.0 (was 5.5).
   - The Office lens is `Troffer_Lens_Cool`, a copy of Troffer_Lens with its emission shifted to the same cool white at the same luminance. The Level 0 lens is unchanged.
2. **Border lamp rule** in `BuildFixture`, after the tier roll, for Auto lamps only (module lamps keep theirs). Use `Cache.Edge(a, b)` and `ZoneOf`.

| Cell | Lamp mode |
|---|---|
| Level 0 cell with an Open or Arch edge to Office | 3, dead |
| Level 0 cell whose only crossings to Office are Doors or Windows | 4, dim |
| Office cell with any crossing to Level 0 | 0, steady |
| Wall-only contact | unchanged |

3. **Checks at clock 0.** Dead lamps are dark (no blink) and dim lamps are at their dim level. The lamp line in each `.txt` shows fewer lit lamps than before.
4. **Optional tag `lightlead_soft`:** every Level 0 border cell dim instead of dead.

**What each shot must show:**
- **shot1:** the eye cell (264,202) dark, and the Office beyond both arches lit cool and brighter.
- **shot2:** cells (255,194) and (255,195) dark, and the Office corridor past Z = 588.0 lit cool.
- **shot3:** the Office cell steady and cool, and the Level 0 Low room through the door dim and warm.
- **shot4:** cells (264,202) and (265,201) dark, the near Level 0 room lit warm, and Office cool in the distance.

**Report:**
- dead and dim lamps per chunk;
- lit lamps per shot against before;
- the share of Level 0 cells made dark;
- the gameplay notes: visibility near borders, and the Relay flicker warning in dead cells.

**Draft contract:**
- the border rule via lamp interface v1 or `BuildFixture`;
- `lampColor` per theme.

---

## 8. Recommendation (provisional; confirm after the pre-renders)

**Base: V1 Frame everywhere, with B0.**
- All three sources agree on this rule: the IPs (P1, P2), real buildings (Rules 1, 3 and 5) and level art ("the transition is a piece of the kit").
- It fixes every cut type at its cause.
- It changes no collider and adds no texture.
- It costs under 0.5 % of draws on WebGL.
- It is deterministic and exact for the era.
- It reuses R3's frames, which already follow the "fit-out owns the boundary" rule.

**Story layer: V2 Renovation on 25–35 % of border runs**, chosen by run hash, denser in chunks generated at higher tiers.
- Frames alone are correct but quiet.
- Renovation explains why an office exists inside Level 0 and gives the place a timeline. Kane's Async insert, the A24 set and the 2002 photo all point that way.
- Using it on a minority of runs keeps it a discovery, not wallpaper.
- Its texture cost is the largest, so WebGL gets a 1024² tier.

**Seasoning: V5's colour only.** Cool Office lamps and the cool lens cost nothing and make light lead through every opening.
- Keep the forced dark border cells off by default, or as a small bias on the existing temperament odds.
- Darkness at every border would become a tell, and it hides the Relay's flicker warning.

**Alternative to V1 at frameless crossings: V3 Neck.** It is the most canon-faithful single image (Kane FF#3 34:21). But it:
- narrows crossings to 1.8 m;
- adds collision;
- changes kit keep-clear;
- triples the map chat's work.

Pick it over V1's portal if the renders show the portal reads too thin.

**Later: V4 Drift**, on top of V1 (the frames stay and units drift round them), for tier 4–5 chunks or a special zone. It fits the A24 "less remembered" idea. It should not be the default: it does not put the change on a built object, and Red reads shader-made change as fake.


**Update 2026-10-08 (fix stage, after the pre-renders and the critic):**
- The pick was rendered over today's main (`16_pick_and_close.md`). It holds as the recommendation, but in Red's own frame (shot 4) it changes only 1.1 % of pixels. Up close (portal, dressed crossings) it reads.
- A fourth geometric family was added and rendered: **Close**, a map rule that closes Office | Level 0 borders like height borders (door or wall). Frameless crossings 1.26 → 0 per chunk, doors 1.37 → 2.53. It is the alternative if Red wants the change out of sight; it changes the layout.
- Red's questions are now four (`40_for_red.md`): the pick, the map rule, light, media.

---

## 9. What Red decides

1. **The pick:** one variation or a mix (recommended: V1 + sparse V2 + V5 colour).
2. **Who owns the joint:** Office as the newer 1990 fit-out (default), or the reverse.
3. **Colliders:** may border crossings narrow? Only V3 needs this.
4. **Office light:** cool white (0.90, 0.96, 1.00) at 6.0, or the same warm light as Level 0.
5. **Base:** Office-only (the base marks the newer side), or one shared base in both zones (Kane FF#3: one black base runs through).
6. **Tiers:** should borders get rougher with tier (more renovation, then drift at tier 4–5)?
7. **Media batch** for the Figma research frames (`media_candidates.md`, `media_candidates_buildings.md`). The visual chat asks once for the whole batch.

## 10. Next steps

1. **Pre-renders.** This workflow runs the five briefs in §7 in private copies.
2. **Figma.** The visual chat lays out the section **FRONTROOMS · LEVEL TRANSITIONS** on page 2099:76, at x 33937, y 2000 (agreed with 平面视觉, `VISUAL_CHAT_TASKS.md` N1). It holds:
   - research frames from on-disk media (Dsc00161, Dsc00159, the A24 tape still, Interiors 1990 `rb_01`–`rb_12`);
   - the before/after sheets per variation.
3. **After Red's pick:**
   - a contract request to the map chat, rebased on Red's 12:21 `FrontRoomsMapWorld.cs`;
   - the visual kit at hero spec, in Blender modules under `Tools/Blender/frontrooms_kit/assets/`;
   - materials via RenderSetup;
   - WebGL-only gates.
4. **Not touched here:** the title handoff (R, Q4) and RoomStream (S). The chosen kit can be applied to the stream door walls later, which is the visual chat's to do.
