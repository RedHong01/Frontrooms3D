# 10 · Stairwell directions (C22): three looks behind the floor's EXIT door

Workflow `c22-stairwell-look`, stage 2 (Directions). Written 2026-10-22 by 游戏视觉.
- **Read first:** `01_research.md` (code facts, geometry, references) and `SOURCES.md` (S1… ids used below).
- **Where the work happened:** pre-renders and blockout kits were made only in the clone `W/proj_stair` (main dfc0086 + working tree, synced 07:2x). Nothing was merged. Main was not opened in Unity. The only files written in main are in this folder.
- **Status:** PROPOSAL. Red picks A, B or C. C22 itself is decided and is not re-asked. C23 (lit `Kit_ExitSign` + crossbar) is what all three show.

**Marks.**
- **SP**: a starting value, not a decision.
- **ESTIMATE**: trade practice, not a code text.
- **UNVERIFIED**: not checked against a source.
- **CR**: a request to another line.

---

## 0. Short answer

| | Direction | One line | Relay, exit approach (whole / body) | Relay on the turn landing (whole / body) | Walk-path light, p2 to median |
|---|---|---|---|---|---|
| **A** | **Painted block + steel pan stair** | The generic 1980s office egress stair: two-tone painted block, a darker band that follows the stair, grey steel pan stairs, picket guard, red standpipe on every landing. | **4.87 / 10.25 : 1** | 1.51 / **4.78 : 1** | 2.5–12.3 fc-eq |
| **B** | **Bare concrete service stair** | Hotel back-of-house: plywood-formed concrete, galvanized two-rail pipe guard, yellow nosings, stencilled numbers, caged globes. | **4.22 / 9.06 : 1** | 1.15 / 3.09 : 1 ✗ | 2.4–11.4 fc-eq |
| **C** | **The stair that pretends to be inside** | The maze's paper, carpet, ceiling tiles and troffers carry on into a carpeted back stair with an oak rail and brass nosings. | **4.38 / 9.38 : 1** | 2.02 / **7.90 : 1** | 2.8–19.9 fc-eq |

The bar is 4 : 1 (the Run! study, `room_visuals/21_run_fix_1014.md` §3). "Whole" means all Relay pixels against a 12 px ring around it, the Run! B method. "Body" means the dark body only, the Run! A method. §5 has every number.

**My recommendation: A**, with its readability fix built in (§2.7).
- It is the code-true 1990 stair (S1, S3).
- It gives the clearest "you have left the floor" step: paper and carpet give way to painted block and steel in one door.
- Its band and numerals make the descent read best.
- It reuses the Run! B kits (twin-head unit, wraparound).
- It passes the Relay body test in both places.

C is the cheapest and the strangest look. It reads the Relay best, but it undoes the step out of the floor and is the least code-true. It would suit a later "deeper floors" event, which is narrative's call. B is the grittiest look, but its stair fails the Relay test even with a white band.

**What the measurements found (for every direction):**
1. **The exit approach passes in all three** at 4.2–4.9 : 1 (body 9–10 : 1).
   - B passes only because its door was changed from dark grey-brown to grey primer. With the dark door it read **3.65 : 1, a FAIL** (r1). The door behind the Relay is part of the readability.
2. **On the turn landing, nothing passes the "whole" test** (1.2–2.0 : 1), and more light does not fix it.
   - At 2× and 3× the landing light it read 1.7 / 2.0 (A), 1.5 / 1.8 (B), 3.0 / 3.6 (C).
   - The cause is the game rig's pale head. It reads 2.3–3.2 : 1 against the landing, while the dark body reads 3.1–7.9 : 1, and the two means cancel.
   - At the exit, against Level 0 paper, the head reads only **1.06–1.10 : 1**: the blank head is paper-coloured. This is a flag for Relay design (§7).
   - Under C22 the Relay never enters the stairwell (01 §3.2: the old map is released, and the new Relay starts after the far door shuts). So the in-stair test protects the future, not today's game.
3. **The stair is never dark.**
   - The walk path stays at or above **2.4 fc-eq at p2** in every frame. The UBC 1991 §3313 minimum is 1 fc. Max/min is at most 6.8 : 1, against NFPA's 40 : 1.
   - With every stair fixture **off**, the game's own fill still gives the walk path 2.0–5.1 fc-eq. That fill is the scene's "Soft ambient direction" (82 % of it passes its own shadows) plus the trilight ambient (r8).
   - So "darkness is never cover" holds by construction. It also means the stair can never be made truly dark for mood without a zone ambient.
4. **Two pipeline facts for production** (§1.6):
   - `FrontRooms/Surface` keys its wall grime on world y, so a stair below y 0 gets full floor grime.
   - Map lamps that cast no shadow light the stair through its walls.

---

## 1. What all three share

### 1.1 Geometry (01 §3.3, unchanged)
- **Footprint:** 1 × 2 cells, clear 2.84 × 5.84 m. Walls 0.16 m on the cell lines.
- **Floor to floor 3.50 m:** 2 flights of 10 risers × 175 mm, 9 treads × 280 mm.
- **Widths:** flights 1.22 m, well 0.40 m, landings 1.66 m deep.
- **Doors:** centred in the landing end wall at z 0. The far door is directly below the exit door.
- **Flight sides:**
  - Flight 1 (exit landing D → turn landing T) runs along the latch-side wall (−x), so the open leaf never hides it.
  - Flight 2 (T → the far-door landing) runs along +x.
- **Built for the renders:** floors −2…5 plus a pit floor, all identical except the floor numeral. This is the periodic shaft of 01 §3.4. It works for a real descent and for the rebase alike, and it makes the well read as endless.
- **Barrier gates (01 §3.5.1):**
  - On the exit landing: across the up flight.
  - On the far-door landing: across the down flight. UBC 3309(e) requires this one, so it is the real-world device.
  - Each gate has a blank sign plate.

### 1.2 The exit door and the sign (C23, all three)
- **Exit door:** `Kit_DoorFrame_Steel` + `Kit_DoorLeaf_Steel` (no lite) in A and B.
  - C uses the maze's own `Kit_DoorLeaf_Veneer` in `Kit_DoorFrame_Wood`. Mineral-core wood fire doors were real in 1990 [ESTIMATE].
- **Push face (maze side):** `Kit_ExitDevice_Crossbar` at the spec mount: door (−0.022, 1.000, 0.900), Euler (0, −90, 0), scale (−1, 1, 1).
- **Pull face (stair side):** blank, with `Kit_DoorCloser_Body/Arm/Forearm/Shoe` (regular arm to the head shoe) and the stair-side plates (§1.4).
- **Sign:** `Kit_ExitSign` on the frame's `exit_sign_p` anchor: bottom at 2.18 m, maze face, `FrontRoomsExitSign` Configure(Lit), EmissionGain 1, no spill light (as `FrontRoomsRoomStream.BuildDoorExitSign`). Measured LIT level 1.00.
- **Ceiling height:** the exit side needs a Standard (2.9 m) ceiling. On a Low (2.4 m) ceiling the sign tops out at 2.392 m (01 §3.2).
- **Inside the stair:** no EXIT sign. Every stair door is "an exit" by code, but the game has one EXIT per floor.

### 1.3 Light rules (all three)
- **Fixtures:** one per landing, two per floor.
- **Placement ("edge"):** each fixture hangs **over the end-wall edge of its landing**, 0.30–0.45 m off the end wall.
  - The door-landing fixture washes the door. When you arrive at the far door, that door is the brightest thing in view, which is Run! rule 5.
  - The turn-landing fixture washes the wall you walk toward.
  - The first pass had the fixture over the landing centre (r1). Edge placement moved the in-stair Relay read only a little (A 1.26 → 1.22, B 1.19 → 1.15, C 1.87 → 2.02). It was kept for the door and wall wash.
- **Shadows:** soft shadows on the three nearest floors only (6 lights). Further floors are unshadowed with a 6–6.5 m range.
- **Everything is steady:** no flicker and no state change, well inside the 3-changes-per-second limit and Reduce Flashing. A deeper floor may lose one normal fixture later, but never a whole landing (01 §3.5.3).
- **Maze lamps:** all 9 are shadowed in these renders. An unshadowed map lamp within its 10 m range lights the stair floor through the wall (§1.6).
- **Scene fill:** the scene's "Soft ambient direction" is included: directional 0.16, rgb (.769, .816, .800), shadow strength 0.18, copied from `FrontRooms3D.unity`. So is `FrontRoomsLook.ApplyAmbient()`. Both are part of the game's light.

### 1.4 Signs (placeholders; strings and type belong to 叙事 / 平面视觉)
- **Floor numeral on the door wall** beside every stair door (latch side, about 1.15–1.25 m). The placeholder seven-segment shapes count down (7 at the exit floor, 6 at the far door). That only stands in for the scheme of 01 §0.9b.
- **Floor plate at 5 ft** on the pull face of the leaf (NYC LL5, S4), with the re-entry / `NO RE-ENTRY` plate below it. Both are blank.
- **Gates:** a blank plate each.
- **No photoluminescent strips, tactile signs or running man:** all post-1990 (01 §1.5).

### 1.5 Frames and measurements (per direction)
- **Six frames,** through the game's post (`FrontRoomsPostStack.Ensure` + `ConfigureCamera`), 1920 × 1080, 4× MSAA, vFOV 76 (the game camera), eye 1.62 m:

| Frame | Camera |
|---|---|
| `door` | maze side, 5 m back, on the closed exit door and its EXIT sign |
| `landing` | just inside the door, looking 35° down into the well (the first view in) |
| `flight` | top of flight 1, down to the turn landing |
| `detail` | 0.3 m view (vFOV 30 at 0.6 m) of tread 5's nosing and the guard |
| `relay_stair` | as `flight`, with the Relay on the turn landing, 0.77 m from the end wall |
| `relay_maze` | Level 0, 8.6 m back, the Relay 1.7 m in front of the exit door |

- **Extra passes for every frame:**
  - a flat ID mask (walk path, maze floor, Relay, signs, emitters, doors);
  - a Relay-parts mask (pale head / dark body / detail);
  - a white-Lambert float pass with the real lights, ambient and fill, no post. This gives the illuminance E on the walk path.
- **fc-eq** = E / (median E of the Level 0 floor in the `door` frames, 1.28) × 50 fc. The 50 fc for a 1990 office floor is an ESTIMATE, so fc-eq is relative.
- **The Relay** is the game's rig (`FrontRooms3DGame.CreateHunter`, walk pose), scaled ×0.655 from its native 3.13 m top to `ModuleUnits.RelayHeight` 2.05 m. Its arms make it 1.73 m wide. It is the prototype: the squeezed giant will change every Relay number.
- **Scripts:** `data/measure_stair.py.txt` and `data/make_images.py.txt`. The run log is `data/runs.md`.

### 1.6 Production facts found on the way
1. **World-y grime.** `FrontRoomsSurface.shader:455` computes the floor grime band from world `positionWS.y`. Any wall below y 0 gets the full band (35 % darker, times `_FloorGrime`). All stair materials here run with grime 0.
   - A real descent (01 §3.4 A, the new map at y −3.5·n) would darken the new floor's walls too. Either rebase (§3.4 B) or add a per-renderer floor offset (a 游戏视觉 shader change).
   - This is a point **for the rebase**.
2. **Light-tightness.** Map lamps cast no shadow two times in three, and an unshadowed lamp lights through walls. The stair needs one of:
   - the start-area lamp hold (`StartLampsNorth/South`) extended to the stairwell shell; or
   - its own rendering layer (`m_SupportsLightLayers` is 0 in `FrontRooms_URP.asset`, so that is a URP-asset change).
   
   The reverse is true too: the landing fixtures must stay shadowed near the shell.
3. **The fill floor.** The scene directional passes 82 % through every shadow. That sets a floor of 0.09–0.13 E (3.5–5 fc-eq) on the walk path with every stair lamp off (r8). No zone can go below it.

---

## 2. A · Painted block + steel pan stair (recommended)

![A](images/stair_dir_a_sheet.jpg)

**2.1 Idea.**
- The 1980s office core stair (01 §1.2–1.3, §1.8).
- Walls of 8 in block, painted putty eggshell over a grey-green semi-gloss band. The band follows the flights at 1.1 m above the nosings. That is BSS 120's "paint pattern on the wall" slope cue (S3 Fig. 2.7.5).
- Grey enamel steel pan stairs with channel stringers and a concrete fill in light-grey deck enamel. An open soffit shows the pans and stringers of the flight above.
- Picket guards with a pipe rail worn to bare steel on top. A red standpipe and hose valve on every door landing. A twin-head battery unit over every door.

**2.2 Materials** (Resources/Surfaces; "new" = a slot to add, clone values are SP)

| Surface | Slot | Value in the renders |
|---|---|---|
| Walls above the band | **new `Stair_Block`**: the Run! B block texture (`Run_Block_{A,N,S}`, clone-only today; 0.4 m tile = 8 × 16 in running bond) | tint #D6D1C2, smoothness ×0.55, macro tone .16 / dirt .18, grime 0 |
| Band (dado), flights only | `Stair_Block` (same texture), own material | #6E7A6D, smoothness ×1.05 |
| Steel (stringers, pans, risers, guard, gate) | **new `Stair_SteelEnamel`** (flat; or `Painted_Metal` tinted) | #6C706B, smoothness .48 |
| Tread and landing fill | **new `Stair_TreadFill`** on the new `Stair_ConcreteFloor` texture (procedural, 1.219 m tile, `data/gen_stair_textures.py.txt`) | **#BDBEB8** light-grey deck enamel, smoothness ×1.35 |
| Pan nosing lip | **new `Stair_PanNosing`** | #7A5A48 (chipped to red-oxide primer) |
| Rails | **new `Stair_RailEnamel`**; production puts the bare-steel top in a wear mask | #5E625D; bare top #A8A9A4, metallic .85 |
| Door leaf / frame | `Prop_SteelPutty` (exists) / frame #5F5A50 | #B7B09E |
| Standpipe, valve | **new `Stair_RedEnamel`**; `Prop_Brass` (exists) | #8E1E17 |
| Fixtures | `Painted_Metal` + `Run_WrapLens`; `Prop_SteelAlmond`, `Run_NeonPilot`, `Run_UnitLegend`, `Run_SealedBeamLens` (Run! B, clone-only) | lens emission ×1.6; pilot red |

**2.3 Kits and budgets** (production LOD0 / LOD1 / LOD2 tris)
- **New:**
  - `Kit_StairFlight_SteelPan` (one flight, 2 C10 stringers, 9 pans with fill and nosing, 10 risers): 7,000 / 2,200 / 400.
  - `Kit_StairLanding_SteelPan` (2.84 × 1.66, edge channel, 3 joists): 1,800 / 600 / 100.
  - `Kit_StairGuard_Picket` (one flight run: Ø 42 pipe rail, bottom rail, about 23 half-inch pickets at 110 mm): 4,000 / 1,200 / 150. LOD2 is an alpha-tested card.
  - `Kit_StairGuard_WellEnd` (0.46 m across the well, 1.067 m high): 800 / 250 / 40.
  - `Kit_StairRail_Wall` (pipe rail, 12 in returns, 3 brackets): 1,500 / 500 / 80.
  - `Kit_StairGate_Picket` (1.19 × 1.0 m): 1,800 / 600 / 100.
  - `Kit_Standpipe_Riser` (3.5 m segment and coupling): 900 / 300 / 60.
  - `Kit_HoseValve_2half` (angle valve, wheel, cap, chain): 3,000 / 900 / 150.
  - `Kit_SprinklerPendant`: 600 / 150 / 0.
  - Numeral decal sheet: 2 tris a digit, type by 平面视觉.
  - Band mesh: about 18 tris a wall run.
- **Reused, Run! B (clone-only, to land with it):**
  - `Kit_WrapFixture_4ft`: 200 tris today. A hero pass is proposed at 1,500 / 500 / 100.
  - `Kit_EmergencyTwinHead` + `_Yoke` + `_Lamp`: 3,706 + 2 × (916 + 2,480).
- **Existing in main:**
  - `Kit_DoorFrame_Steel` 3,866, `Kit_DoorLeaf_Steel` 3,930.
  - `Kit_DoorCloser_*` 6,210, `Kit_ExitDevice_Crossbar` 3,588, `Kit_ExitSign` 3,136.
- **Blockout as rendered (floor period):** 224 renderers, 29 materials, 48,171 tris (door set 28,249, lights and twin-head 10,978). These counts include the kits' LOD1/LOD2 meshes; at LOD0 the door set is 14,006 and the twin-head 10,498. The whole 9-floor shaft is 1,754 renderers and 337,775 tris (`data/stats_A.txt`).
- **Production estimate per floor period:** about 63k tris at LOD0, about 30 draws once merged per material.

**2.4 Light plan**
- **Normal fixtures:** one 2-lamp 4 ft wraparound per landing on the soffit, 0.30 m off the end wall (over the door; over the far wall).
  - Spot 170/120°, I 3.2 (SP), range 6.5, cool white (.95, .98, 1.00).
  - Soft shadows on the nearest 3 floors.
- **Emergency:** twin-head unit inside over each stair door at 2.36 m, under normal power. Only the red AC pilot glows, steady.
  - The heads are already aimed down the flight and at the landing, for any later emergency state (01 §1.6). The battery state is not proposed here.
- **EXIT:** §1.2.
- **Measured:** the walk path is 2.5–12.3 fc-eq (p2 to median) with max/min ≤ 4.5 : 1. The post median Y is 0.026–0.027 in the stair against 0.131 at the maze door, about a fifth of the maze.

**2.5 Sound hooks** (for 声音设计; only events are posted; names are proposals)
- **Footsteps:** surface tag "steel pan" on treads (a slightly hollow tap, 01 §1.7), "concrete fill" on landings.
- **Reverb:** a stair zone. Hard block and steel give a long, bright tail with flutter (RT60 about 2 s, ESTIMATE). The echo must stay clearly the player's own, so it never imitates the Relay's W2 footsteps.
- **Hum:** a 120 Hz ballast bed per wraparound, following its intensity like the map's fixture hum.
- **Exit door (common to all three):**
  - The full real sequence: crossbar push, latch retract, swing, closer check, latch strike. This is the one door where Red's 10-03 wooden-door bans do not describe reality (01 §0.9a; freesound 341176, CC0, S26).
  - Optional at the far door: a stack-pressure whistle under the leaf and a heavy pull, if 关卡设计 ever holds that door.
- **Distant slams up and down the shaft:** narrative only, never directional.

**2.6 How the descent reads**
- Every landing is the same except its numeral. The dark band zigzags down both side walls, and the picket guards repeat down the well.
- The pale light-grey treads stand out against grey steel, so the steps read at a glance.
- The red pilot dots repeat one floor down. They are static decor, not a cue.

**2.7 Relay readability** (the fix is part of the spec)
- **As first built** (fill #8F918C, band on the end walls too): body 3.93 : 1 on the turn landing, a FAIL (r3).
- **As spec'd:** light-grey fill and the band only along the flights. The landing end walls are plain putty, which BSS needs only for the slope. Body **4.78 : 1** (own ring 4.26), whole 1.51, head 2.93 (r6).
- **Exit approach:** whole **4.87**, body 10.25 (protan 4.82).

**2.8 WebGL** (separate track; desktop unchanged)
- Bake one floor period's lighting (a lightmap or an APV scenario) and reuse it for every floor: the shaft is static and periodic. Keep at most the 2 nearest fixtures realtime and unshadowed.
- Baking is also the WebGL light-tightness fix: unshadowed realtime lights leak through slabs.
- Merge each floor per material. Pickets are A's cost (60 renderers per floor in the blockout): one merged mesh per run, with a card at LOD2.
- Render about 3 floors and let fog take the well below.
- Textures: `Stair_Block` and `Stair_ConcreteFloor` at 1024 on the WebGL tab.

**2.9 Risks**
- A is the coolest, greyest of the three, and the film grade turns the grey-green band olive.
- It needs the most new kits: flight, landing, guard, gate, valve.
- The wraparound is a 200-tri Run! asset that needs a hero pass.

---

## 3. B · Bare concrete service stair

![B](images/stair_dir_b_sheet.jpg)

**3.1 Idea.**
- The hotel back-of-house stair (01 §1.8): cast-in-place concrete with plywood-form panel seams and snap-tie cones, unpainted.
- Monolithic flights with a sloped soffit, troweled concrete treads with 50 mm safety-yellow nosings (OSHA 1910.144 yellow, S13).
- A galvanized two-rail pipe guard with posts on base flanges. That is the 12 in rule for areas not open to the public (UBC 1712, 01 §1.1).
- Big stencilled numerals with bridges. A caged vapour-tight incandescent globe per landing. A coffee-can ashtray on one landing: a narrative hook, not a cue.

**3.2 Materials**

| Surface | Slot | Value |
|---|---|---|
| Walls, soffits, flight bodies | **new `Stair_Concrete`** (procedural: two 4 × 8 ft panels, ties on a 2 ft grid, fins, form oil, bug holes; 2.438 m tile = 256/105) | tint #D9D6CF (walls), #C9C5BD (bodies), #CFCBC3 (risers) |
| Treads, landings | **new `Stair_ConcreteFloor`** | #D2D0CA |
| Nosing stripes | **new `Stair_SafetyYellow`** | #D9A712, 50 mm, on every tread and landing edge |
| Guard, rails, gate | **new `Stair_Galvanized`**; production reuses the outlets fix-1 spangle grain (`research/outlets`) | #A4A7A3, metallic .75; hand-polished top #B9BBB7 |
| Door leaf | **new `Stair_DoorPrimer`** (grey primer) | **#8C887E**. With the first #4F4741 leaf, the Relay at the exit read 3.65 : 1 (FAIL) |
| Globe | **new `Stair_GlobeLit`** (emissive glass) + galvanized cage | emission (1, .82, .60) × 4 |
| Standpipe, valve | as A | as A |

**3.3 Kits and budgets**
- **New:**
  - `Kit_StairFlight_CastConcrete`: one solid with tread, riser and body submeshes, nosing stripes as a decal strip. 1,200 / 400 / 60.
  - `Kit_StairLanding_Slab`: 300 / 100 / 20.
  - `Kit_StairGuard_PipeTwoRail` (top and mid rail, 3 posts with flanges, Ø 48): 3,000 / 900 / 120.
  - `Kit_StairRail_Wall` (galvanized): 1,500 / 500 / 80.
  - `Kit_StairGate_Pipe` (frame, mid rail, brace): 1,200 / 400 / 60.
  - `Kit_JellyJarGlobe` (box, globe, 4-wire cage): 2,500 / 800 / 120.
  - Standpipe, valve and sprinkler as A. Stencil numeral decals.
- **Reused:** twin-head unit as A.
- **Blockout floor period:** 133 renderers, 30 materials, 50,135 tris. Shaft: 1,066 renderers, 354,399 tris.
- **Production per floor period:** about 47k tris at LOD0, about 25 draws.

**3.4 Light plan**
- **Normal fixtures:** one caged globe per landing on the end wall: at the door landing beside the door (x +0.95, 2.30 m), at the turn landing on the far wall's centre (2.35 m).
  - A point light, so the globe lights its own wall as a hot halo. I 2.6 (SP), range 6, warm (1, .84, .66), about 2,850 K.
  - Soft shadows on the nearest floors.
- **Emergency:** twin-head unit as A.
- **EXIT:** §1.2.
- **Measured:** the walk path is 2.4–11.4 fc-eq with max/min ≤ 4.8 : 1. The post median Y is 0.020–0.028: the darkest stair of the three. Warm light plus the film's green-yellow grade turns the concrete brown.

**3.5 Sound hooks**
- **Footsteps:** "concrete", a dead, gritty thud.
- **Reverb:** the longest and darkest tail (bare concrete, RT60 about 2.5 s, ESTIMATE).
- **Hum:** none, since incandescent lamps do not buzz. This is the quietest stair: the player's own steps carry it, with the same echo rule as A.
- **Grit:** a little grit underfoot on the landing with the ashtray, optional.
- **Exit door:** as A.

**3.6 How the descent reads**
- The yellow nosing stripes stack into a ladder of lines down the well. That is the strongest "down" graphic of the three.
- Warm globe pools step down floor by floor. The stencilled numerals are the largest (0.60 m).

**3.7 Relay readability**
- **Turn landing:** body **3.09 : 1** (own ring 2.66), a FAIL. Whole 1.15, head 3.18.
- **Tried:** a white-painted band 1.5 m high following the stair (r5) raised the body only to 3.39. Not adopted.
- **Exit approach:** whole **4.22**, body 9.06 (protan 4.16), with the grey-primer door.

**3.8 WebGL**
- The cheapest geometry, since the flights are solids.
- It adds two 2048² texture sets (WebGL tab 1024, about 2.7 MB with mips).
- Bake as A. A point light with shadows costs 6 atlas slices, so WebGL bakes the globes.

**3.9 Risks**
- B fails the in-stair Relay body test.
- It is the darkest stair, so its brightness rests most on the scene fill (§1.6.3).
- It is the furthest from the maze palette, which some may read as "a different game".

---

## 4. C · The stair that pretends to be inside

![C](images/stair_dir_c_sheet.jpg)

**4.1 Idea.**
- The exit door looks like every other maze door: veneer leaf, wood frame, only the crossbar and the EXIT sign mark it.
- Behind it the maze simply continues, sloped:
  - Level 0 chevron paper on all walls, cove base;
  - loop-pile carpet over treads and risers, brass nosing bars;
  - a 2.9 m drop ceiling of the maze's own 2 × 4 tiles over each landing, with the maze's own troffer;
  - an oak handrail on square balusters with newel posts, oak skirt boards along the flights;
  - a brass suite-number plate (`Kit_DoorNumberPlate`) as the only floor sign.
- This is the uncanny "home office" back stair (Kane Pixels ties every change of look to a nameable object; 01 §2.1). It is plausible only as a hotel guest stair: carpeted landings near guest floors, 01 §1.8 [ESTIMATE].
- **It is wrong on purpose:**
  - no emergency unit, no standpipe;
  - combustible finishes in a rated exit enclosure. UBC 1991 Ch. 42 interior-finish classes were not read: UNVERIFIED.

**4.2 Materials**

| Surface | Slot | Value |
|---|---|---|
| Walls | `L0_Wallpaper` (exists), as a **new `Stair_L0_Wallpaper` copy with grime 0** (§1.6.1) | as the maze |
| Treads, risers, landings | `L0_Carpet` (exists), as a grime-0 copy | as the maze |
| Landing ceilings | `L0_Ceiling` (exists) + the Level 0 troffer lens | as the maze |
| Flight soffits, bulkheads | **new `Stair_SoffitPlaster`** (`Office_Drywall` texture) | #D8CC97, the paper's ground |
| Rail, balusters, newels, skirt | `Prop_WoodOak` / `Prop_WoodWalnut` (exist); flat tints in the blockout | #7A5530 rail, #8A6337 balusters |
| Nosing, latch, plate | `Prop_Brass` (exists) | — |
| Door | `Door_Veneer` (exists) via `Kit_DoorLeaf_Veneer`, `Kit_DoorFrame_Wood` | — |

**4.3 Kits and budgets**
- **New:**
  - `Kit_StairFlight_Carpeted` (one solid; carpet, plaster and brass-nosing submeshes): 1,500 / 500 / 80.
  - `Kit_StairLanding_DropCeiling` (slab, carpet, tile ceiling, bulkhead, cove): 600 / 200 / 40.
  - `Kit_StairGuard_OakBaluster` (moulded rail, 18 balusters, 2 newels with caps): 3,500 / 1,000 / 150.
  - `Kit_StairRail_WallOak` (brass brackets): 1,200 / 400 / 60.
  - `Kit_StairGate_Oak` (low wooden gate, brass latch): 1,200 / 400 / 60.
  - `Kit_SkirtBoard_Oak`.
- **Reused:** the maze troffer (map fixture code), `Kit_DoorNumberPlate`, the veneer door set (11,826 tris at LOD0 with the closer; 25,213 counting all LODs).
- **Blockout floor period:** 144 renderers, 23 materials, 32,681 tris. Shaft: 1,103 renderers, 200,929 tris.
- **Production per floor period:** about 27k tris at LOD0, about 20 draws. It shares batches with the maze.

**4.4 Light plan**
- **Normal fixtures:** the Level 0 map troffer, turned across the landing (1.2 m along x, 0.6 m deep), 0.45 m off the end wall in the 2.9 m drop ceiling.
  - Spot 5, 162/96°, the map's lamp colour. Range 6.5 instead of the map's 10, so it does not reach through slabs.
  - Soft shadows on the nearest floors.
- **No emergency unit visible.** EXIT outside only.
- **Measured:** the walk path is 2.8–19.9 fc-eq with max/min ≤ 6.8 : 1. The post median Y is 0.017–0.059: the brightest stair, and on the landings the closest to the maze.

**4.5 Sound hooks**
- **Footsteps:** "carpet", exactly as the maze.
- **Reverb:** dead and short (carpet and paper).
- **Hum:** the maze's troffer hum continues unchanged.
- **The idea:** the stair sounds like the maze. The only change is the slope, and perhaps one creaking tread (oak stringers, narrative).
- **Exit door:** a veneer leaf with a crossbar and a closer. The closer is real here too.

**4.6 How the descent reads**
- No stair grammar: only the slope, the oak rail and the brass nosing glints. Each landing is "a maze room with a troffer", so the descent reads as "the same room again, lower".
- The suite plate is the only progress, and it is small (0.2 m).
- This is the most Backrooms of the three and the least legible.

**4.7 Relay readability**
- **Turn landing:** body **7.90 : 1** (own ring 6.67), the best of the three, because bright paper sits behind it. Whole 2.02, head 2.27.
- **Exit approach:** whole **4.38**, body 9.38 (protan 4.25).

**4.8 WebGL**
- No new textures except the soffit, and every big surface batches with the maze. This is the cheapest direction.
- Bake as A. The troffer lens is the map's lens.

**4.9 Risks**
- It undoes the research's key moment: the step from carpet and wallpaper to bare block that says "you have left the floor" (01 §1.8).
- The one safe room looks like the place the Relay hunts you.
- The stair's identity rests entirely on the sign outside the door.
- It is the least code-true. If used, it fits better as a deep-floor event (a change is noticed, 01 §1.6 / BSS §2.4.8). That is a narrative and system-design call, not mine.

---

## 5. All measurements (final set; `data/metrics_final.json`)

Linear Rec. 709 Y of the post frame. Walk = the descent's tread and landing surfaces in view. E = white-Lambert illuminance (Unity units); fc-eq as §1.5. Relay ratios are hi/lo of mean Y against a 12 px ring.

| Dir | Frame | Median Y | p5 / p95 | Walk Y median | Walk E p2 / median (fc-eq) | Relay whole (protan) | Body / own ring | Head |
|---|---|---|---|---|---|---|---|---|
| A | door | 0.131 | 0.018 / 0.322 | — | maze floor E 1.28 = 50 | — | — | — |
| A | landing | 0.026 | 0.014 / 0.086 | 0.037 | 0.156 / 0.316 (6.1 / 12.3) | — | — | — |
| A | flight | 0.027 | 0.012 / 0.150 | 0.030 | 0.077 / 0.224 (3.0 / 8.7) | — | — | — |
| A | detail | 0.014 | 0.009 / 0.024 | 0.016 | 0.064 / 0.079 (2.5 / 3.1) | — | — | — |
| A | relay_stair | 0.026 | 0.012 / 0.149 | 0.029 | 0.076 / 0.218 (3.0 / 8.5) | 1.51 (1.51) | **4.78** / 4.26 | 2.93 |
| A | relay_maze | 0.130 | 0.019 / 0.286 | — | — | **4.87** (4.82) | 10.25 / 9.67 | 1.07 |
| B | door | 0.130 | 0.018 / 0.313 | — | — | — | — | — |
| B | landing | 0.028 | 0.015 / 0.062 | 0.041 | 0.124 / 0.293 (4.8 / 11.4) | — | — | — |
| B | flight | 0.027 | 0.013 / 0.079 | 0.027 | 0.079 / 0.169 (3.1 / 6.6) | — | — | — |
| B | detail | 0.020 | 0.011 / 0.055 | 0.020 | 0.061 / 0.110 (2.4 / 4.3) | — | — | — |
| B | relay_stair | 0.027 | 0.013 / 0.078 | 0.026 | 0.078 / 0.155 (3.0 / 6.0) | 1.15 (1.14) | 3.09 / 2.66 ✗ | 3.18 |
| B | relay_maze | 0.129 | 0.019 / 0.285 | — | — | **4.22** (4.16) | 9.06 / 8.40 | 1.10 |
| C | door | 0.130 | 0.018 / 0.315 | — | — | — | — | — |
| C | landing | 0.059 | 0.015 / 0.147 | 0.067 | 0.233 / 0.510 (9.1 / 19.9) | — | — | — |
| C | flight | 0.040 | 0.012 / 0.152 | 0.042 | 0.084 / 0.300 (3.3 / 11.7) | — | — | — |
| C | detail | 0.017 | 0.009 / 0.054 | 0.020 | 0.071 / 0.115 (2.8 / 4.5) | — | — | — |
| C | relay_stair | 0.039 | 0.012 / 0.148 | 0.041 | 0.083 / 0.292 (3.2 / 11.4) | 2.02 (1.96) | **7.90** / 6.67 | 2.27 |
| C | relay_maze | 0.129 | 0.019 / 0.286 | — | — | **4.38** (4.25) | 9.38 / 8.69 | 1.06 |

**Supporting runs** (`data/runs.md`):
- **Centre lamps (r1):** whole on the turn landing A 1.26, B 1.19, C 1.87. B at the exit approach with the dark door: 3.65 FAIL.
- **Light gain sweep, turn landing whole:**

| Gain | A | B | C |
|---|---|---|---|
| ×2 | 1.72 | 1.45 | 2.97 |
| ×3 | 2.04 | 1.81 | 3.64 |

- **A before the fix (r3):** body 3.93.
- **B with a white band (r5):** body 3.39.
- **Stair lamps off (r8):** walk E median 0.090–0.131 (3.5–5.1 fc-eq), post median Y 0.014–0.019.

**Photosafety:** every light is static, so there are 0 changes per second.
**Spread** (p95 + .005) / (p5 + .005): the stair frames run 2.1–9.1, the maze 12–14.

**Images** (`images/`, own renders, JPEG q90):
- `stair_dir_{a,b,c}_{door,landing,flight,detail,relay_stair,relay_maze}.jpg`: 18 full frames.
- `stair_dir_{a,b,c}_sheet.jpg`;
- `stair_dirs_compare.jpg`;
- `stair_v_relay_readability.jpg`: crops, whole and body;
- `stair_v_illuminance.jpg`: walk path coloured by fc-eq.

---

## 6. Comparison

| | A | B | C |
|---|---|---|---|
| Code-true 1990 | yes (S1, S3, S4) | yes | no (on purpose) |
| "You have left the floor" | strongest | strong | none |
| Descent read | band + numerals + repeats | yellow ladder + stencils | slope only, small plate |
| Relay at the exit (whole) | 4.87 ✓ | 4.22 ✓ (needs the light door) | 4.38 ✓ |
| Relay on the landing (body) | 4.78 ✓ (with the fix) | 3.09 ✗ | 7.90 ✓ |
| Walk light p2 / median | 2.5 / 3.1–12.3 fc-eq | 2.4 / 4.3–11.4 | 2.8 / 4.5–19.9 |
| New textures | 2 sets (block, floor) | 2 sets (concrete, floor) | 0 (+ soffit tint) |
| New kits | most (pan flight, guard, gate, valve) | middle | fewest (reuses maze) |
| Floor period, production est. (LOD0) | ~63k tris, ~30 draws | ~47k, ~25 | ~27k, ~20 |
| WebGL | bake; merge pickets | bake; cheapest geometry | bake; cheapest overall |

**Pick: A.**
- **Second choice:** C as a deep-floor variant, if narrative wants the stair to start "pretending" after some floor. It reuses the maze's materials, so the extra cost is small.
- **Not recommended as the base:** B, unless Red wants grit over Relay readability.

---

## 7. For other lines (one message each; C22 itself not re-asked)

- **关卡设计** (after 01 §3.6's five questions):
  - (a) Light-tightness (§1.6.2): extend the start-area lamp hold to the stairwell shell, or ask 游戏视觉 for a light-layer change.
  - (b) If the real descent puts the new map at y ≠ 0, the world-y grime (§1.6.1) favours the rebase.
  - (c) The exit door's landing needs a Standard 2.9 m ceiling on the maze side.
- **声音设计:**
  - The surface tags and reverb zones in §2.5 / §3.5 / §4.5.
  - The exit-door Foley exception (crossbar and closer are real on this one door; 01 §0.9a).
  - Every echo stays the player's own.
- **叙事 / 平面视觉:**
  - The numbering scheme. The placeholders count down 7, 6…
  - `NO RE-ENTRY` and re-entry wording, gate plate text, the stair letter.
  - The numeral face and size. A: painted, about 0.46 m. B: stencil with bridges, about 0.60 m. C: suite plate.
- **Relay design** (系统设计 / 游戏视觉 hunter work):
  - The prototype's pale "blank head" reads 1.06–1.10 : 1 against Level 0 paper at the exit.
  - Every Relay number here is the ×0.655 prototype. Re-run `relay_stair` and `relay_maze` with the squeezed-giant rig when it exists (`-stairShots relay_stair,relay_maze`).

## 8. Files

- **This spec:** `10_directions.md`.
- **Images:** `images/` (24 files, 5.9 MB, own renders).
- **Data:** `data/`:
  - the harness `FrontRoomsStairLookdev.cs.txt`;
  - `gen_stair_textures.py.txt`, `measure_stair.py.txt`, `make_images.py.txt`;
  - `metrics_*.json`, `stats_{A,B,C}.txt`, `poses_*.tsv`, `log_*.txt`, `runs.md`, `run_status.txt`.
- **Clone only:**
  - `W/proj_stair`: the harness in `Assets/Editor/Rendering/`, the blockout kit prefabs in `Assets/StairBlockout/{A,B,C}/BK_{Door,ExitDoor,Flight,Gate,Guard,Landing,Light,Paint|Skirt,Services,Sign,WallRail}.prefab`, Stair_* textures, and the Run! B kits copied from `W/proj_run_emergency_power`;
  - raw renders in `W/stair_work/runs/`.
- **VL specs:** `W/vl_pending/stairwell.md` (VL325–VL326; VL327–VL328 stay free for the later stages).
- **Figma:** none in this stage (`30_figma.md` not written yet).
