# 10 — Run! rooms: three visual directions to pick from

Date: 2026-10-03. Status: DESIGN DONE, renders pending (the three direction agents of this workflow render the pre-render briefs in §7 in their own clones).
Nothing here is implemented. Red's project was not opened or changed. Only this file was written.

**Inputs read:** `03_run_research.md` (all of it), `01_inventory.md`, `04_captures.md` and its frames, `research/interactables/10_spec.md` (door family D5, §2.2–2.4), `research/relay_pursuit/30_narrative.md` (EGRESS), `Documentation/RELAY_PURSUIT_REDESIGN.md` §4.2, §7, §9, `research/wallpaper_motion/20_level_design_phosphor.md` (chase wave, lamp-override layer), `research/hunter/10_hunter_directions.md` §1–2, `research/webgl/01`, `02`, `03` (light cap), `research/office_and_film/22_era_lock.md`, `FONTS_PERIOD_1990.md`, and in the clone `FrontRoomsRoomModuleData.cs`, `FrontRoomsSurfaces.cs`, `FrontRoomsPostStack.cs`, `Assets/Editor/Rendering/FrontRoomsKitLookdev.cs`.

**Marks.** **SP** = a starting value for look-dev or playtest, not a decision. **UNVERIFIED** = not checked against a source or a frame. **CR** = a contract request to another chat (an exact proposal; nobody edits their files).

---

## 0. Short answer (for Red)

There was no visual plan for Run!. Here are three. All three use the same room: a **1-cell-wide corridor, 9 cells (27 m) long**, entered from Level 0 or Office through an arch, ending in **a door you can shut**. It is a **trigger room** (EGRESS): stepping 1.5 m in trips the alarm and calls the Relay. The three differ in **what the building does when it trips**.

| | Direction | One line | What changes on the trip |
|---|---|---|---|
| **A** | **RED WARD** | The canon hospital corridor. A row of hanging EXIT signs, too many of them. | **The lights turn red.** The white tubes cut out at once; only the red signs keep burning. The only white left is the door's glass. |
| **B** | **EMERGENCY POWER** | A back-of-house service corridor on the alarm circuit. Twin-head battery lamps on the walls. | **The power goes.** Mains drop, 0.35 s of black, then hard white pools from the battery lamps. Red is only on fire equipment. |
| **C** | **HOUSE LIGHTS** | A glazed-tile link corridor, lit hard and white, with fire doors held open on magnets. | **The doors go.** All lights come to full in a wave that runs ahead of you to the exit. The held doors release and swing shut, ahead and behind. |

**My recommendation: B · EMERGENCY POWER** (§6). In one sentence: it is the game's own chase language at full strength (EGRESS already says "in CHASE the lamps drop to emergency level"), it is what a real 1990 building does, it matches Dark Deception's pools-and-red-accents grammar, and it asks the least of the map chat.
- A is the closest to your Week 1 pitch ("the lights turn red") and to canon. Its risk: a red field hides value, and it adds a colour language used nowhere else.
- C is the fairest and the most unusual (exposure instead of darkness). Its risk: it is the least "horror" and it needs moving doors, the biggest map ask.

**What I need from you:** pick A, B or C after the renders (§7). Then answer the open questions in §9 (mainly: may a trigger module author its own door; is the alarm local-sound or silent).

---

## 1. What all three share

### 1.1 Role (R1 from `03` §1.4)
- **A Run! room is an alarm corridor**, a trigger room (`ModuleTrigger.Alarm`, proposed in `RELAY_PURSUIT_REDESIGN.md` §9). Not a zone, not the look of every chase.
- **Readable before entry:** from 2 m outside the entrance at 1.6 m eye height you see its signature object and the far door (`03` §5.8 item 1).
- **Trip:** the trigger arms ≥ 1.5 m inside, past the threshold. Fires once per room instance. It calls the Relay (`relay.Summon`).
- **Reward:** a long straight to a door you can shut. Placement follows the map chat's rules: one per 2–3 zones, ≥ 15 cells from the start door, never in the first zone, never in a dead end without a shuttable door (`RELAY_PURSUIT_REDESIGN.md` §9).

### 1.2 One layout for all three (so the renders compare)
Corridor-local metres. +Z runs from the entrance to the goal. x = 0 is the corridor axis.

| Item | Value | Why |
|---|---|---|
| Cells | 1 × 9 cells, Standard height **2.9 m** | map grid: 3 m cells, walls 0.16 m centred on cell lines |
| Clear width | **2.84 m** (walls at x = ±1.5, faces at ±1.42) | close to a real 8 ft (2.44 m) hospital corridor (`03` §2) |
| Length | entrance wall at z = 0, goal wall at z = 27 (face 26.92) | one full sprint is 27.5 m (5.5 m/s × 5 s) |
| Trip line | z = 1.5 | arming ≥ 1.5 m inside |
| Trip line → goal door face | **25.4 m = 4.6 s at sprint** | the sprint reaches the door with 0.4 s to spare |
| Entrance | an arch 1.4 × 2.2 m centred (map arch range 1.1–1.8 × 2.2). C uses a held-open door pair instead (§4) | an inner-zone opening |
| Goal | one door 1.0 × 2.1 m centred in the far wall, **RN-X** (new free Run exit door: RN-F's wood-grain leaf, crossbar exit device, a latch, a wired-glass vision lite; §1.4) | "shutting a door is the escape verb"; it must latch so the Relay has to break it |
| Beyond the goal | the next zone (Level 0 or Office) | the corridor ends on its way somewhere |
| Lamp rhythm | pools every 6 m in A and B → **0.92 Hz** at sprint; C is continuous, door pairs every 9 m → 0.61 Hz | all under the 3 Hz line (`03` §5.3) |

The 27 m straight does not exist in the map today (Standard rooms are 2–4 cells, so 12 m max). That is the first contract request (§8, CR-1).

### 1.3 States
Every direction has the same four states. Only their look differs.

| State | When | Notes |
|---|---|---|
| **Armed** | before the trip | the readable look; obeys the Relay's stage-1 warning flicker (`LampFx.Warn`) like any other room |
| **Tripping** | 0–1.5 s after `RoomTriggered(Alarm)` | one scripted change, ≤ 3 changes per second per lamp (`RELAY_PURSUIT_REDESIGN.md` §7.4) |
| **Tripped** | until the zone resets | steady. **No light reacts to where the Relay is** (Red's staging: nothing changes at the Relay's position). The map's chase wave (`LampFx.Dip`), power sags and warning bursts skip these cells (CR-4): the alarm already told you |
| **Reset** | on the Relay's WITHDRAW (restoral) | the armed look comes back over 1.5–3 s, with its sound |

Under **Reduce Flashing** every change uses ≥ 0.5 s attacks and releases, and any flasher burns steady.

### 1.4 Shared hardware and rules
- **Door family** (`interactables/10_spec.md` D5): **RN-F** = the free Run door (wood-grain laminate leaf 0.174, stainless armor plate, push plate on the pull face, offset D-pull, closer, **no latch**), **RN-K** = the locked Run door (almond enamel steel leaf in a dark-bronze frame, "STAFF ONLY" plate). The FREE/LOCKED value rule holds: a wood leaf in a white Run wall reads 4.2 : 1 (0.174 vs 0.738).
  - **RN-F cannot be the goal:** it has no latch, so a shut RN-F would not hold the Relay. Real fire exit doors must self-close and latch.
  - **New member needed (all three): RN-X "Run exit".** Leaf `Kit_DoorLeaf_Ward_Lite` = RN-F's wood-grain laminate leaf (FREE value) with a 0.25 × 0.75 m wired-glass vision lite centred at 1.5 m; `Kit_DoorFrame_Steel`; `Kit_ExitDevice_Crossbar` on the push face (the corridor side, as EX-F); lever trim, bored latch and strike on the far face; closer. It opens away from the runner and latches when shut, so the Relay must break it (2.5 s at tier 1).
  - The lite shows the next zone's light. That makes the goal emissive, so it reads past the 16 m lamp radius (and the 10 m WebGL radius).
  - The wired look needs `Glass_Wired` (glass track; not made yet). Prototypes use a stand-in (§7.3).
- **EXIT sign:** `Kit_ExitSign` housing 0.330 × 0.200 × 0.060 (spec §2.4). Red letters only, 6 in (152 mm), on an off-white face. Face set in **TeX Gyre Heros Bold Condensed** (`Assets/Fonts/Period1990/TeXGyre/texgyreheroscn-bold.otf`), replacing today's Arial. "EXIT" has no enclosed counters, so a stamped stencil face needs no bridges. **No arrows** (the exit is straight ahead). No Light component on the wall sign in the WebGL tier.
- **Era (1990, `22_era_lock.md`):** US English caps on signs, no brands, no printed date after 1990, nothing designed after 1993. No green running man (ISO; not US in 1990).
- **Value before colour** (`03` §5): the route, the obstacles, the door and the Relay must read in greyscale. **Red stays steady.** Any flash is small, ≤ 1 Hz, synced, with a sound.
- **Dark is never cover** (`30_narrative.md` §6): darkness sits in full-width bands between pools, never in pockets.
- **Obstacles** read at **8 m** (1.5 s at sprint), never need a jump, and leave a lane ≥ 1.4 m.
- **Footsteps:** every direction has a hard floor (VCT). FMOD has no hard-floor surface today (`SoundIds.Surface { Carpet, CarpetTile, Metal }`). CR-S1.
- **Ambient and post:** the global trilight ambient (#333026 / #423D2B / #665C3D) and the global lift (+.035) make the Run room's blacks the highest of all rooms today (p5 0.027, `03` §6). Each direction carries **its own corridor ambient** and **its own local post volume** (`FrontRoomsPost_Run_<A|B|C>`, priority 2, blend 1.0 m, loaded by `FrontRoomsPostStack.EnsureZoneVolume`, which sets priority 1, so the rig raises it to 2). The Office volume is priority 1, so Run wins inside the corridor.
- **Fixtures are the visual chat's.** The module sets `ModuleLamp.Off` on all 9 cells (no map fixture), and a visual-chat rig, `FrontRoomsRunRig`, builds the corridor's lamps, signs and devices. It owns their culling (16 m desktop; WebGL tier 10 m, cap 31, per `webgl/02` §224).

---

## 2. Direction A · RED WARD — "the lights turn red"

**Idea.** The wiki's Level ! made honest: a white hospital ward corridor at night, a hanging EXIT sign every 6 m (code only needs one within 30 m of every point; this is the Backrooms' countable error, a real object repeated too often), and when you trip it the tubes cut out and the corridor is left burning red. The only white light is the glass in the door you must reach.

### 2.1 Lighting
| | Armed ("night ward") | Tripped |
|---|---|---|
| Troffers | 2'×4' prismatic, the stream fixture (pan `Painted_Metal` + `Troffer_Lens` + volumetric beam, density .012), one per cell at the cell centre + 0.3 m Z (map grammar). **Night switching:** cells 0, 2, 4, 6, 8 lit, cells 1, 3, 5, 7 off (lens glow 0.04). Lit: spot 162° / 96°, colour **(0.93, 0.96, 1.00)** cool white, intensity **3.5**, range 9 (SP). Steady | all off in **one cut** at t = 0 (level 1 → 0 over 0.08 s; lens glow decays with τ 0.12 s as phosphor afterglow, then 0.04) |
| Hanging EXIT signs | 4 double-faced signs on two chrome rods at **z = 6, 12, 18, 24**, housing centre 2.42 m (bottom 2.32). Plus a wall `Kit_ExitSign` over the goal door (top 2.38). Faces `Run_ExitSign` (new period face), emission 3.2. Red light per sign: a downward spot (1.00, 0.13, 0.07), intensity **2.4**, range 6.5, 170° / 120°, soft shadows; plus an unshadowed point (same colour) 0.5, range 2.2 for the ceiling glow (SP) | unchanged: **steady**. Now the only fill |
| Goal | the RN-X lite glows with the next zone's light (a lit room beyond; no extra Light needed) | the lite is the only white in the corridor. It is the brightest thing in view |
| Ambient (corridor only) | 0.6 × global | 0.15 × global, tinted (1.0, 0.35, 0.30): red bounce off white walls |
| Post `FrontRoomsPost_Run_A` | lift (1, 1, 1, 0); white balance temperature 0 (cancels the global +9 so the red stays red, not orange); contrast +8; saturation 0; bloom threshold 1.0, intensity .70; vignette .30 (SP) | same |
| Measured targets, tripped S1 (SP) | — | median Y 0.015–0.04 (ETB 0.002–0.004 is too dark to read obstacles); p95/p5 ≥ 5; saturated red 25–50 % of pixels; goal lite = the frame's brightest region |

- **Rhythm:** red pools under the signs every 6 m → 0.92 Hz at a sprint. The sign row is the leading line (ETB's lens row, made of real objects).
- **Reacts to the chase:** it does not. Red never pulses (photosafety: a red field must not flash, `03` §5.6). The trip is one step from white to red, a single change.
- **Hand-over in:** from Level 0's warm yellow (1, .96, .88) at 5.0 per cell into half the lamps at a colder white: the ward reads quieter and colder before you step in, with red glints already hanging in it. From Office the same.
- **Hand-over out:** opening the goal door throws a wedge of warm light onto the red floor. The next zone is normal.
- **Reset:** troffers re-strike one by one over 1.5 s with the stream's ballast model (≤ 2 flickers each). The signs stay.

### 2.2 Surfaces, ceiling, floor, colour
- **Walls:** `Run_Wall` as it is (white semi-gloss #E3E1D9, albedo luminance 0.738, scuffs). Bright walls are what make red light read as a lit space, not a black one.
- **Handrails:** a continuous hospital rail both sides, top 0.92 m: almond vinyl cover #C9C3B2 over aluminium, 0.14 m tall × 0.06 m proud, brackets every 1.2 m, returns at doors. Two side leading lines at hand height.
- **Ceiling:** `Run_Ceiling` 2'×2' lay-in tile at 2.9 m.
- **Floor:** `Run_Floor` 12" VCT #DAD7CC with the wax lane. A waxed floor that catches the red signs (as in ETB) is a P2 extra: needs a reflection probe (§7.6).
- **Palette:** white, cream, almond rails, chrome, **teal** vinyl (the 1970s–80s chairs), red only from the signs.

### 2.3 Signage (period, no brands, no dates)
- "EXIT" on every sign, no arrows.
- Side doors (all **LOCKED**, RN-K, canon: "side doors are locked"): z = 4.5 left, 10.5 right, 16.5 left, 22.5 right. Plates: "4-11", "STAFF ONLY", "4-13", "LINEN" (`Kit_DoorSign` / `Kit_DoorNumberPlate`).
- One "QUIET PLEASE" placard by the entrance (period hospital, optional).

### 2.4 Props (canon: chairs and beds in the way)
| z | Side | Prop | Lane left |
|---|---|---|---|
| 7.0 | right | a 1980s hydraulic transport stretcher (`Kit_Stretcher`, new: 2.0 × 0.70 m, chrome frame, white vinyl pad, side rails down), turned 12° into the lane | 1.6 m |
| 12.5 | left | three chrome sled-base visitor chairs, teal vinyl, ganged (`Kit_VisitorChair` ×3, new) | 2.1 m |
| 18.5 | right | a folding chrome wheelchair, black sling (`Kit_Wheelchair`, new), turned 30°; a 5-caster IV pole 1.9 m beside it | 1.7 m |
| 23.5 | left | a linen cart, stainless frame, white canvas bag (`Kit_LinenCart`, new) | 2.0 m |

Each obstacle sits under a sign (in a red pool), so it is lit when you reach it. Left and right alternate: a mild weave, never a block.
- **Device (readable trigger):** a ceiling smoke detector (white disc Ø 0.15, red LED) at z 3.0. In standby the LED blinks once every 8 s; tripped it burns steady. It is a detail, not the read; the read is the sign row.

### 2.5 At a sprint, and fairness
- From outside (S2), the white corridor and its hanging EXIT row read at once against yellow Level 0: the row is on the axis, visible through the arch from about 1 m inside onward.
- Value: tripped, the walls next to each sign stay lit (red only), so the dark Relay (body ≤ #3B, `hunter/10` §1.4) reads against them. Expected ≥ 6 : 1 (to be measured, §7.5).
- **Risks:** (1) for a protan player (about 1 in 50 men) red light looks dim, so A tripped reads as a dark corridor with one white door: still playable, because the cut is also a value drop; (2) a saturated-red field narrows what the eye can separate; obstacles get lit faces under the signs to compensate; (3) a new colour grammar: red light means "run" here and nowhere else.

### 2.6 Needs from the map chat (A)
CR-1 to CR-6 (§8). Plus the 4 locked side doors: either LOCKED door edges authored by the module (the same `ModuleEdge.Door` kind as CR-6a, member RN-K, never unlockable), or wall props that look locked (no map change). Either way they never open, so the LOCKED grammar never lies.

### 2.7 Needs from the sound chat (A)
- `Run.TubesOut`: one ballast/contactor clunk for the whole corridor, and the fixture hum dropping out together (4 nearest hum voices go to 0 with the Lights).
- Signs are incandescent: **silent**. The silence after the clunk is the point.
- `Run.Reset`: ballast strikes per troffer (existing strike Foley).
- Hard floor footsteps (CR-S1) and a hard-corridor reverb (CR-S2).

### 2.8 Cost (A)
| | Desktop (reference, highest spec) | WebGL tier (gated) |
|---|---|---|
| Lights in the corridor | armed 5 troffer spots + 4 × 2 sign lights + 1 wall sign = 14; tripped 9 | tripped: 5 sign spots, no points; armed: 5 troffers + 5 signs = 10 |
| Shadowed | 5 troffers (armed), 5 sign spots | 0–1 (nearest sign) |
| With the anteroom (6) visible | ≤ 20 | ≤ 16, under the cap of 31 |
| New assets | `Kit_ExitSign_Hanging` (rods, double face), `Kit_Stretcher`, `Kit_Wheelchair`, `Kit_VisitorChair`, `Kit_IVPole`, `Kit_LinenCart`, `Kit_Handrail_Hospital`, `Kit_SmokeDetector`; new `Run_ExitSign` face texture | same meshes at LOD1 |
| Code | `FrontRoomsRunRig` (states, cut, reset), the corridor ambient (§7.3), `FrontRoomsPost_Run_A` | WebGL quality-level gate |
| Rough days (visual) | 6–8 | +1 |

---

## 3. Direction B · EMERGENCY POWER — "the power goes"

**Idea.** A back-of-house corridor, the building's own service spine, on the alarm circuit. When you trip it, the mains drop, the corridor goes black for a third of a second, and then the battery lamps on the walls snap on: hard white pools with dark bands between them. Red appears only where a 1990 building puts it: the EXIT sign, the pull station, the bell, the sprinkler main overhead.

### 3.1 Lighting
| | Armed | Tripped |
|---|---|---|
| Ceiling fixtures | 4 ft two-lamp **wraparound**, prismatic acrylic, surface-mounted on the deck (`Kit_WrapFixture_4ft`, new: 1.22 × 0.25 × 0.08 m), one per cell at the cell centre + 0.3 m Z, axis along Z. Spot 162° / 96°, colour **(0.90, 0.95, 1.00)** (4100 K cool white), intensity **5.5**, range 10, one in three soft-shadowed (map rule). Steady; one stutter tube for life in cell 5 | mains drop: all 9 go 1 → 0 in **0.06 s** at t = 0 (contactor). Lens glow 0.03 |
| Twin-head battery units | 4 units (`Kit_EmergencyTwinHead`, new hero asset): z = **3 (right), 9 (left), 15 (right), 21 (left)**, box centre 2.45 m. Off. A red "AC ON" neon pilot glows (emissive only) | at **t = 0.35 s** all heads come on together: filament rise 0 → 1 over 0.18 s, each unit 0–40 ms apart (independent transfer relays). Pilots go out |
| Heads (desktop) | — | 8 spots. Each unit: one head aimed +Z, one aimed −Z, both pitched −30°, so the pools land **between** units at z ≈ 6, 12 and 18 (two heads meet in each), unit 3's −Z head lights the threshold, and the floor under each unit stays dark (true to life). Exception: unit 21's +Z head is aimed at the goal door leaf (pitch −13°); its cone lights the floor from z ≈ 24 to the door. Colour **(1.00, 0.84, 0.64)** (sealed-beam incandescent, ~2800 K), intensity **4.0** (SP, tune to the targets below), range 11, outer 50° / inner 20° (hard edge), soft shadows strength .95, cookie `Cookie_SealedBeam` (256 px: hot core, one faint ring, light lens fluting). Each head also gets the stream's additive volumetric beam frustum (2.5 m, density .02): visible cones in dusty air |
| EXIT signs | one flag-mounted double-faced sign on the right wall at z 13.5 (projects 0.35 m, bottom 2.32), facing down the corridor; one wall sign above the goal door. Emissive faces; desktop adds a point (1, .13, .07) 0.6, range 1.8 each | unchanged (on the emergency circuit) |
| Goal | RN-X lite (next zone's light) | the door leaf sits in the hottest pool (unit 21's aimed head) and under the lit EXIT. Brightest object in the corridor |
| Ambient (corridor only) | 0.6 × global | **0.10 × global**, neutral |
| Post `FrontRoomsPost_Run_B` | lift (1, 1, 1, −0.01); contrast +12; saturation −10; white balance temperature −6 (the warm-white pools read white against Level 0's yellow); post-exposure −0.1; bloom threshold 1.0, intensity .60; vignette .32 (SP) | same |
| Measured targets, tripped S1 (SP) | armed median Y ≥ Level 0 Standard's (0.09) | pool-centre floor ≈ **0.25 ×** the armed floor at the same spot (real emergency light is 1/10–1/20 of normal; we keep it readable); gap floor ≥ **pool / 40** (the 40 : 1 code limit, `03` §4.2); p95/p5 ≥ 8 (Kane's back-of-house 8–9.5); saturated red 1–3 % |

- **Rhythm:** a pool every 6 m → 0.92 Hz at a sprint, the "pulse you feel without flashing" (`03` §5.3). The red sprinkler main overhead is a continuous leading line through pools and gaps.
- **Reacts to the chase:** it does not. The building is on battery; the pools hold steady. The trip itself (mains drop, black, pools) is the one reaction, and it is the same thing the map's chase wave does in miniature elsewhere ("lamps drop to emergency level", EGRESS), so the player learns one language.
- **Hand-over in:** from Level 0 (warm, 5.0) into a cooler, brighter, harder corridor with a dark ceiling: it looks like the building's back stairs before anything happens. From Office (same lamp colour, green grade): B is set apart by the dark exposed ceiling and the block walls.
- **Hand-over out:** the goal door opens into a lit zone: the first normal light after the pools.
- **Reset:** heads drop out (one change), wraps re-strike with ≤ 2 flicks each in a sweep from the entrance, 0.15 s per cell; pilots relight.

### 3.2 Surfaces, ceiling, floor, colour
- **Walls:** painted concrete block, 8 × 16 in (0.203 × 0.406 m), running bond, tooled joints. **Two-tone:** lower 1.2 m grey-green #8E958B, upper off-white #DAD7CB, semi-gloss. A charcoal vinyl bumper rail (#3A3A38, 0.15 m tall) at 0.9 m on standoffs. Stainless corner guards at door jambs. New `Run_Block` texture set (A/N/S; joints in N).
- **Ceiling:** no tiles. Painted concrete soffit #45463F at 2.9 m (dark, so pools read on floor and walls, and the fixtures and heads pop). Hung under it:
  - the **sprinkler main**, Ø 0.10 m, painted red #A3201A, at 2.62 m along x = +0.55, brass upright heads every 3 m;
  - a cable tray 0.30 × 0.08 m at 2.70 m along x = −0.60;
  - three 3/4 in EMT conduits on the left wall at 2.55–2.65 m, a junction box every 3 m.
- **Floor:** VCT grey-beige #B9B4A6 with every 4th row a darker #7E7A70 accent row (a common period pattern), worn wax lane. Darker than A's floor so the pools show.
- **Palette:** institutional grey-green and off-white, dark ceiling, warm-white light, red only on fire equipment (OSHA 1910.144: red = fire protection), nothing yellow except the mop bucket.

### 3.3 Signage
- Level 0 side of the entrance, beside the arch at 1.5 m: an ANSI-style plate **"NOTICE — AUTHORIZED PERSONNEL ONLY"** (blue NOTICE header, the 1970s–80s ANSI Z35.1 scheme; UNVERIFIED detail of the header colour). This is the readable cue with the twin-heads behind it.
- Side doors, all LOCKED RN-K: z 5.0 right "ELECTRICAL ROOM / AUTHORIZED PERSONNEL ONLY"; z 17.0 left "STAFF ONLY"; z 23.0 right a stencilled **"NOT AN EXIT"** (OSHA, since 1974).
- Pull station: "FIRE" / "PULL DOWN" in white on red. Extinguisher cabinet: "FIRE EXTINGUISHER" in vertical red letters.
- EXIT signs as §1.4.

### 3.4 Props
| z | Side | Prop | Lane left |
|---|---|---|---|
| 1.0 | right | red pull station at 1.2 m (`Kit_PullStation`, new) and a 6 in red bell above it at 2.3 m (`Kit_FireBell`, new) | — |
| 6.4 | right | a dolly of 8 grey steel folding chairs | 2.0 m |
| 12.0 | left | janitor cart: grey plastic, yellow mop bucket and wringer, mop | 1.9 m |
| 10.2 | left wall | semi-recessed extinguisher cabinet at 1.1 m (`Kit_ExtinguisherCabinet`, new) | — |
| 18.2 | right | two-wheel hand truck with 3 cartons, leaning on the wall | 2.0 m |

Every obstacle sits **in a pool**, never in a gap. Nothing lies in the dark bands, so the bands never look like places to hide.

### 3.5 At a sprint, and fairness
- From outside (S2): the dark ceiling, the red sprinkler line, the projecting twin-heads in profile and the lit goal at the end, through the arch. The twin-heads are the readable object: "this corridor is on the alarm".
- The 0.35 s black is one flash (down, then up), under WCAG's 3 per second.
- **Relay read:** looking back, the heads aimed +Z face you; the Relay crosses the pools and is backlit by them as it passes through the gaps. Dark body against a lit pool behind: the Kane door-gap read (4.9–7.5 : 1, `03` §3.3).
- **Risks:** (1) the global fill light and lift wash out the gaps unless the corridor ambient and local post land (§7.3); (2) the glare of a head aimed at the camera can hide a figure right in front of it (tune with the pitch, measure in C4).

### 3.6 Needs from the map chat (B)
CR-1 to CR-6 only (§8; CR-6 is the goal door every direction needs). No moving parts, no other new door types.

### 3.7 Needs from the sound chat (B)
- `Run.MainsDrop` (t = 0): a contactor drop-out clunk; every fixture hum stops at once.
- `Run.TransferRelay` ×4 (t ≈ 0.35 s, 0–40 ms apart along the corridor): small relay clicks from each unit. These are the "relay pull-in click" already in the motif (`SOUND_FOLEY_MOTIF_RESEARCH.md` §4.3–4.4).
- Battery units: a faint charger buzz in standby that stops on the trip (optional).
- The bell stays **silent** (EGRESS: the building-wide alarm is silent). Optional: one dead tick of the clapper.
- `Run.Reset`: mains return, heads click off, ballasts strike.
- CR-S1, CR-S2.

### 3.8 Cost (B)
| | Desktop | WebGL tier |
|---|---|---|
| Lights | armed 9 wraps; tripped 8 heads + 2 sign points = 10 | armed 9; tripped **4** (one wide spot per unit, 80°, no cookie, no beam) |
| Shadowed | 3 wraps (armed), 8 heads (tripped) | nearest head only |
| With the anteroom | ≤ 16 | ≤ 15 |
| New assets | `Kit_EmergencyTwinHead` (hero LOD0: box, 2 heads on yokes, pilot, test button), `Kit_WrapFixture_4ft`, `Kit_PullStation`, `Kit_FireBell`, `Kit_ExtinguisherCabinet`, sprinkler main + heads + hangers, cable tray, EMT + boxes, bumper rail, janitor cart, hand truck, chair dolly; `Run_Block` ×2 tones, `Run_ServiceVCT`, `Run_SoffitPaint`; `Cookie_SealedBeam` | LOD1; no cookie |
| Code | `FrontRoomsRunRig`, corridor ambient, `FrontRoomsPost_Run_B`; two-tone walls as two slabs per wall | gate |
| Rough days (visual) | 8–10 | +1 |

---

## 4. Direction C · HOUSE LIGHTS — "the doors go"

**Idea.** A link corridor between wings: glazed tile, white plaster, a continuous row of bare fluorescent strips. It is the brightest place in the game, and you can see down it from far away, a white slot in a yellow wall (Kane's lit tunnel; the A24 trailer runs in full light). When you trip it, nothing goes dark: every strip comes to full in a wave that runs ahead of you to the exit, red flashers start over the doorways, and the fire doors held open on magnets let go and swing shut, ahead of you and behind you. The horror is exposure. There is no dark anywhere, and it can see you from 27 m.

### 4.1 Lighting
| | Armed ("economy switching") | Tripped |
|---|---|---|
| Strips | 9 bare-lamp **8 ft two-lamp T12 strips** (`Kit_StripFixture_8ft`, new: white enamel channel 2.44 × 0.06 × 0.10 m, two bare tubes, no lens) on the axis at 2.86 m, **3 end to end per 9 m compartment** (7.32 m, centred; the cross walls at z 9 and 18 break the row). Numbered 1–9 from the entrance. **Every other strip off** (1, 3, 5, 7, 9 on; the 1970s–80s energy-saving practice; tubes in place, grey). Strip 7 has one dead tube with orange cathode end-glow. Colour **(0.94, 0.97, 1.00)** cool white | the off strips strike in a **front from the entrance to the goal at 8 m/s** (the chase wave's speed): strip i starts at t = 0.1 + z_i / 8 s, one 60 ms flick to 30 % then a 0.2 s rise to 100 % (2 changes per strip). The lit strips step 0.9 → 1.0 |
| Real lights (desktop) | two spots per strip, 0.6 m either side of its centre (18), 165° / 120°, range 9, intensity **4.5** (SP); the 10 under lit strips on | all 18 on. Soft shadows on every third (6) |
| Flashers | red incandescent beacons (`Kit_FlasherLamp`, new: Ø 0.10 m red glass dome on a 4 in box) centred on the approach (−Z) face of each pair's header (for the z 0 pair that is the Level 0 side) and over the goal, at 2.6 m. Off | **1 Hz, 50 % duty, all in sync**, from t = 0.3 s. **Emissive only, no Light** beyond a 0.8 m point at 0.3, so the flashing area stays tiny (WCAG red-flash rule). Steady under Reduce Flashing |
| Goal | RN-X lite + wall EXIT + the goal flasher | the door is the **only large dark shape** in a white field (wood leaf 0.174 in a 0.8 wall), framed by the only red light. Rule 5 ("the door is the brightest thing") holds in its inverted form: most legible, not brightest. Said honestly |
| Ambient (corridor only) | 1.0 × global | 1.2 × global (white bounce) |
| Post `FrontRoomsPost_Run_C` | post-exposure +0.25; contrast +10; saturation −22; white balance temperature −10, tint +4 (cool, away from Level 0's warm green); lift (1, 1, 1, 0); highlights (1, 1, 1, −0.05) to keep white walls off the clip; bloom threshold 1.1, intensity .35; grain .20 (SP) | same |
| Measured targets (SP) | armed S1 median Y ≈ 1.3 × Level 0 Standard's (0.09 → ~0.12) | tripped S1 median Y 0.30–0.40 (the A24 run, 0.35); p95/p5 ≥ 10; saturated red ≤ 0.5 % |

- **Rhythm:** continuous light, no pools. The rhythm is the door pairs every 9 m (0.61 Hz) and the strip joints every 2.44 m (low contrast).
- **Reacts to the chase:** the lights are locked at full and skip the chase wave. The dynamic element is the doors: the Relay pushes through each closed pair (+0.4 s, a loud double slap), and you see the leaves flap and swing back behind you through the wired glass.
- **Hand-over in:** a white slot through the arch: the strongest "readable before entry" of the three. From Office: same.
- **Hand-over out:** from overlit into dimmer yellow (or the Office's green). The eye drops; the next zone looks murky for a moment (local post blend 1.0 m).
- **Reset:** holders do not re-latch by themselves (true to life: someone props the doors). Lights step back to economy (one change per off strip). Doors stay shut until pushed.

### 4.2 Doors (the signature)
- **Pairs at z = 0 (the entrance), 9 and 18.** Each: an opening **2.24 × 2.1 m** (2 × 1.12 m leaves, the stream's leaf width) in a 0.16 m cross wall with a transom wall above 2.1 m and 0.30 m jambs; dark-bronze steel frame (`Kit_DoorFrame_Steel`, pair version).
- **Leaves:** RN-F pair version (wood-grain laminate, **no latch**: cross-corridor smoke doors in health care need not latch, UNVERIFIED for the 1990 code; push plates, kick plates), each with a 0.25 × 0.75 m wired lite at 1.15–1.90 m; regular-arm closer on each leaf (`Kit_DoorCloser_*`, existing spec).
- **Held open** at 90° (swinging toward +Z, the egress direction) by **magnetic holders** (`Kit_MagHolder`, new: grey box 0.10 × 0.10 × 0.08 m on a 0.28 m wall extension at 1.95 m; armature plate on the leaf's top corner).
- **On the trip (t = 0):** all holders release together (one shared thunk per pair). Closer curve: 90° → 10° linearly over 3.0 s, then 10° → 0° over 1.0 s, eased (period closers sweep in 3–7 s). No latch: a shut leaf rests closed and pushes open.
- From the trip line at a sprint you reach the z 9 pair at 1.4 s, when it is about 54° open: you push through a closing door. The z 0 pair shuts behind you.
- Header plate on each pair, both faces: **"FIRE DOORS — DO NOT OBSTRUCT"** (red caps on white plastic).

### 4.3 Surfaces, ceiling, floor, colour
- **Walls:** a glazed structural-tile wainscot to 1.52 m (8 × 16 in units, buff-white glaze #E6E1D2, smoothness .85, grout #9C978A, bullnose cap, cove base unit; a mid-century institutional finish, second-hand in 1990), painted plaster above, #EEEBE2 eggshell. New `Run_GlazedTile` and `Run_Plaster`. Two slabs per wall.
- **Ceiling:** flat painted gypsum #EDEBE4 at 2.9 m, no tiles. The strip row is the only feature.
- **Floor:** VCT #D9D5C8 with two charcoal border bands (#4A4844, 0.30 m wide, 0.30 m from each wall): leading lines in value, not colour.
- **Palette:** white on white, cool; the wood door leaves are the only warm, dark shapes; red only in the flashers and the pull station.

### 4.4 Props
- z 5.5, right: a 1980s floor burnisher, grey and orange, cord coiled on its handle (`Kit_FloorBurnisher`, new). Lane 2.0 m.
- z 22.0, left: a folded wheelchair against the wall (shares `Kit_Wheelchair` with A). Lane 2.2 m.
- z 1.0, right: the red pull station (shares B's).
- A corridor plaque "C-LINK" at 1.5 m by the entrance.
- Nothing else. The emptiness is the point: a clean, exposed sightline.

### 4.5 At a sprint, and fairness
- The fairest of the three: the Relay reads ≥ 10 : 1 against white at any distance; obstacles read at 20 m.
- The doors are a fair test: they close in 4 s and never latch; the pushes cost the Relay too.
- **Risks:** (1) the least "horror" by darkness; the dread must come from exposure, the doors and sound; (2) it inverts the game's grammar (danger = lights fail); here danger = lights come on; (3) the biggest map ask (moving door pairs inside a module, push-through on contact).

### 4.6 Needs from the map chat (C)
CR-1 to CR-5, plus **CR-7** (held door pairs inside the module, release on the trigger, push-through at a run, Relay cost) and CR-6 (the goal door). This is about the size of the door state machine's own pair work.

### 4.7 Needs from the sound chat (C)
- `Run.HolderRelease` ×3 (t = 0): the magnetic door-holder release, EGRESS's preferred lock-on base ("the building just acted").
- `Run.CloserSweep` per leaf: hydraulic closer hiss, a soft bump at rest (no latch click).
- `Run.StripStrike` per strip along the front: rapid-start tick and a hum swell.
- `Run.FlasherTick`: 1 Hz incandescent flasher relay, synced with the light.
- `Run.DoorPushThrough` (player at a run) and `Relay.DoorPushThrough` (+0.4 s slap).
- CR-S1, CR-S2.

### 4.8 Cost (C)
| | Desktop | WebGL tier |
|---|---|---|
| Lights | armed 10, tripped 18 + 4 tiny flasher points = 22 | one per strip: armed 5, tripped 9 (intensity doubled); flashers emissive only |
| Shadowed | 6 | 0 |
| With the anteroom | ≤ 28 | ≤ 15 |
| New assets | `Kit_StripFixture_8ft`, `Kit_MagHolder`, `Kit_FlasherLamp`, RN-F pair leaves with lites, pair frame, `Kit_FloorBurnisher`, `Kit_Wheelchair`, `Kit_PullStation`; `Run_GlazedTile`, `Run_Plaster`, border VCT | LOD1 |
| Code | `FrontRoomsRunRig` (front, flashers), door rig and closer solver (spec exists), corridor post | gate |
| Rough days (visual) | 10–12, plus the map's door work | +1 |

---

## 5. Side by side

| | A · RED WARD | B · EMERGENCY POWER | C · HOUSE LIGHTS |
|---|---|---|---|
| Space | hospital ward corridor (canon) | back-of-house service corridor (Kane, EGRESS) | glazed-tile link corridor |
| Trip changes | colour (white → red) | level (normal → black → pools) | level up + doors move |
| Where red is | the fill (signs) | fire equipment + EXIT only | flashers + EXIT only |
| Tripped median Y (target) | 0.015–0.04 | ~0.01–0.03 with pools to ~0.15 | 0.30–0.40 |
| Relay read at 12 m | ≥ 6 : 1 (to measure) | backlit in gaps (to measure) | ≥ 10 : 1 |
| Greyscale / protan read | weakest | good | best |
| Era 1990 | each object yes; the red fill is a Backrooms exaggeration | strongest | strong (economy switching, held doors) |
| Canon / Red's Week 1 pitch | strongest | medium | weak |
| Fits EGRESS | partly | best (its chase grammar in full) | good (holders = the lock-on cue) |
| Photosafety | one cut, red steady | one 0.35 s black | strike front + 1 Hz tiny flashers |
| Map asks | CR-1–6 (+ locked side doors) | CR-1–6 | CR-1–7 (largest) |
| Desktop lights tripped / WebGL | 9 / 5 | 10 / 4 | 22 / 9 |
| Visual days | 6–8 | 8–10 | 10–12 |

---

## 6. Recommendation: B · EMERGENCY POWER

1. **One language for the whole game.** The map's chase wave already "drops lamps to emergency level" (EGRESS, `30_narrative_phosphor.md` §4). B is that idea at full strength in one room, so a player who has seen a chase knows what B means the first time, and the reverse.
2. **Real 1990, no exaggeration needed.** Twin-head battery units, white pools within the 40 : 1 code limit, red only on fire equipment. It meets the era lock and the "value before colour" rule without special pleading.
3. **Red's primary reference.** Dark Deception's chase corridors are warm pools with small red accents along a leading line (`03` §3.5). B is that grammar: pools every 6 m, a red sprinkler line overhead.
4. **Fair by construction.** Obstacles sit in pools; the dark bands are full-width; the Relay is backlit as it crosses them; the goal is the hottest pool.
5. **Cheapest honest option for the map chat.** No moving parts, no new door types beyond the goal door every direction needs.

What I would borrow if Red likes them: A's hanging EXIT sign over the goal (a stronger far read), and C's held doors at the entrance only, as a later upgrade (doors that close behind you on the trip). Neither is in the B render, so the three stay distinct.

---

## 7. Pre-render protocol (for the three direction agents)

Nothing goes into Red's project. Each agent works in its own clone and writes only images and a report into this folder.

### 7.1 Setup
- **Clone:** `cp -Rc <scratchpad>/proj_rooms <scratchpad>/proj_run_<key>` (key = `red_ward`, `emergency_power`, `house_lights`). One Unity at a time per clone. Never open `Frontrooms3D`.
- **Script:** `Assets/Editor/Rendering/FrontRoomsRunLookdev.cs` in the clone, modelled on `FrontRoomsKitLookdev.cs` (same folder): empty scene, `FrontRoomsLook.ApplyAmbient()`, `FrontRoomsPostStack.Ensure(root)`, a camera with `FrontRoomsPostStack.ConfigureCamera`, clear colour #22231C, near 0.06, far 80, 1920 × 1080 ARGB32 RT with 4× MSAA, **render twice per shot**. Entry `FrontRoomsRunLookdev.RunBatch` with `-runShots` (comma list) to re-shoot single frames.
- **Run:** `/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath <clone> -executeMethod FrontRoomsRunLookdev.RunBatch -logFile <log>` (with graphics; ≤ 600000 ms per call).
- **Root:** world **(4992, 0, 4992)** (26 × 192 m), so every world-projected print lands as in `04_captures.md`. Corridor-local coordinates below are added to it.
- **No directional fill light.** The game's "Soft ambient direction" (0.16) lights through every ceiling (audit F5; WG6 plans its removal). It would erase A's and B's dark. Note this in the report.

### 7.2 Shared geometry
- **Level 0 anteroom:** 3 × 2 cells, x −4.5…4.5, z −6…0, 2.9 m. `L0_Wallpaper` / `L0_Carpet` / `L0_Ceiling` (`FrontRoomsSurfaces.Room(RoomRule.Lobby, slot)`), walls 0.16 m on cell lines, `_CeilingHeight` 2.9 on wall renderers. Six map lamps, one per cell: lens 0.6 × 0.025 × 1.2 at the cell centre + 0.3 m Z, URP Lit albedo (1, .98, .92), smoothness .1, emission (1, .96, .84) × 2.6; spot 0.06 m below, 162° / 96°, (1, .96, .88), intensity 5, range 10; the two lamps nearest the arch cast soft shadows. This copies `FrontRoomsMapWorld.cs:1173-1227`.
- **Corridor:** x −1.5…1.5, z 0…27, walls 0.16 m centred on x = ±1.5, z = 0 and z = 27. Arch in the z = 0 wall: x −0.7…0.7, height 2.2 (C: the door pair opening instead). A dark-bronze steel cased frame (`Prop_SteelBrown`, 0.05 m face) on the corridor side of the arch marks the finish change.
- **Goal:** RN-X, 1.0 × 2.1 (built with the kit's door scripts if present in the clone, else a faithful blockout: wood-grain laminate leaf, dark-bronze steel frame, crossbar exit device on the corridor face at 1.0 m, kick plate, closer body) with a 0.25 × 0.75 m lite at 1.15–1.90 m, shut.
- **Stub beyond the goal:** one Level 0 cell, z 27…30, one map lamp, so the lite and the 5 mm door gap show warm light.
- **Relay proxy:** if the scene's current Relay (`Hunter` in `FrontRooms3D.unity`) can be instanced into the look-dev scene, use it. Otherwise a proxy: capsule body r 0.24, top 1.73 m; shoulder box 0.78 × 0.24 × 0.30 at 1.72 m; head sphere Ø 0.28 centred at 1.60 m, 0.12 m forward; body albedo **#2B2928** smoothness .25, head **#D8D4C8** smoothness .35; casts shadows. Say which was used.
- **Wired glass stand-in** (until `Glass_Wired` exists): URP Lit transparent (.80, .85, .86, α .25), smoothness .9, plus an alpha-tested quad 2 mm behind it with a 12.5 mm square wire grid (0.6 mm lines, #6E6E6A).

### 7.3 Shared rendering rules
- **Corridor ambient:** give corridor renderers `lightProbeUsage = CustomProvided` and an SH set through a MaterialPropertyBlock (`CopySHCoefficientArraysFrom`), scaled from the global trilight per state (A: 0.6 armed / 0.15 tinted (1, .35, .30) tripped; B: 0.6 / 0.10; C: 1.0 / 1.2). If URP ignores it in this version, fall back to a Light Probe Group with hand-set SH, and say so. Production would use a `_FR_AmbientScale` term in the Surface shader (a cost line, not for now).
- **Local post:** create `Assets/Resources/Rendering/FrontRoomsPost_Run_<A|B|C>.asset` with the values in §2–4 and add it with `FrontRoomsPostStack.EnsureZoneVolume(root, "Run_<A|B|C>", corridor bounds, 1.0f)`, then set its `priority` to 2 (the helper sets 1).
- **States are deterministic:** a clone-only component `FrontRoomsRunLookdevRig` with `Evaluate(float t)` (t = seconds since the trip; t < 0 = armed) sets every light, emission, door angle and ambient from the tables above. No Play Mode, no random rolls.
- **Signature asset at hero quality, the rest blockout.** The direction's signature object (A: the hanging EXIT sign; B: the twin-head unit; C: the magnet holder, the door pair and the strip fixture) is modelled in Blender in the clone (`<clone>/Tools/Blender/frontrooms_kit/assets/run_*.py`, kitlib conventions, LOD0) and imported with the clone's `FrontRoomsKitImporter`. Secondary props may be correct-size, correct-albedo blockouts. Say which is which.
- **Sign faces:** generate the new period faces (EXIT, plates) with TeX Gyre Heros Bold Condensed / Bold from `Assets/Fonts/Period1990/TeXGyre/`. No Arial.

### 7.4 Shots (all 1920 × 1080; 72° vertical FOV unless noted; the game uses 76°)
| Id | State | Eye (corridor-local) | Look at | What it tests |
|---|---|---|---|---|
| **S1a** sightline | armed | (0, 1.62, 1.5) | (0, 1.30, 27.0) | the long view at eye height from the trip line |
| **S1b** sightline | tripped, t = +3 s | same | same | the Run look; the goal read |
| **S2** doorway from Level 0 | armed | (0.35, 1.62, −2.0), 2 m outside the arch | (0, 1.25, 27.0) | readable before entry: signature + far door visible |
| **S3** detail | direction-specific (below), 50° FOV | | | the signature object at hero quality |
| **C1** chase | t = −0.5 s | (0, 1.62, 0.8) | (0, 1.30, 27.0) | stepping in |
| **C2** chase | mid-change (A t = +0.10; B t = +0.20; C t = +0.90) | (0, 1.62, 1.5) | same | the light behaviour |
| **C3** chase | t = +2.0 s | (0, 1.62, 12.5) (5.5 m/s × 2 s) | (0, 1.30, 27.0) | the goal mid-run |
| **C4** chase | t = +4.0 s | (0, 1.62, 23.5), looking back | (0, 1.40, 0.0) | Relay proxy on the axis at z 11.5 (12 m behind), facing the camera: the silhouette test (C: the z 18 pair is shut between them, so the read is through its lites) |

S3 per direction:
- **A:** eye (0.9, 1.62, 9.3) → the sign at (0, 2.42, 12.0), tripped. Shows the stencil face, rods, red spill on tiles and wall, the rail below.
- **B:** eye (−0.6, 1.62, 6.8) → the unit at (1.34, 2.45, 9.0), tripped. Heads, cookie pool, beam cones, pilot (off), sprinkler main, bell in the background.
- **C:** **S3a** armed: eye (0.7, 1.62, 6.6) → the held leaf and holder at (−1.12, 1.6, 9.6). **S3b** t = +1.0 s: same camera, leaf mid-swing (about 63°), armature plate free, flasher on.

Derived frames (Python, Pillow, `/usr/bin/python3`): greyscale copies of S1a, S1b, S2, C4 (linear Rec. 709 Y, re-encoded to sRGB); a protan copy of S1b and C4 (Machado 2009 matrix, severity 1.0, applied in linear RGB); one contact sheet.

### 7.5 Measurements and pass/fail
Use `<scratchpad>/run_research/measure.py` (Y p5 / p50 / p95 / p99.5, contrast, saturated red, mean RGB) on every frame. Add:
1. **Readable before entry (S2):** the signature object and the goal door are both in frame and unoccluded. The goal door ≥ 40 px tall (expected ≈ 53 px at 29 m).
2. **Goal (S1b greyscale):** the mean Y of the goal region (door + lite + sign) is the frame's highest region, or second after a fixture (C: report the door's contrast against its surround instead).
3. **Relay (C4):** render the proxy once more with a flat unlit ID colour to get a mask. Mean Y inside the body mask vs a 12 px ring outside it: **≥ 4 : 1**. Also on the protan copy.
4. **Obstacles (S1b):** each obstacle's mean Y vs its immediate background ≥ 2 : 1 (report per obstacle).
5. **No pockets (B):** floor Y along the axis sampled every 0.5 m: min ≥ max / 40.
6. **Photosafety:** a table of every change per lamp in the first 2 s after the trip; ≤ 3 per second; saturated-red area that changes (A: one step only; C: flasher area in px at 2 m).
7. **Direction targets** from §2.1, §3.1, §4.1: report measured vs target.

### 7.6 Optional P2 (only if time is left)
- A reflection probe at the corridor centre (256 px, rendered per state) so waxed VCT catches signs and pools. The Surface shader has no box projection (inventory §1.2), so reflections will not line up: note it.
- A WebGL-tier preview of S1b (the gated light set from the cost table) on desktop.

### 7.7 Outputs
- Images: `images/run_dir_<key>_<id>.jpg` (JPG q85, ≤ 1920 px wide; ids `s1a`, `s1b`, `s2`, `s3` (C: `s3a`, `s3b`), `c1`–`c4`, `s1a_grey`, `s1b_grey`, `s2_grey`, `c4_grey`, `s1b_protan`, `c4_protan`, `sheet`).
- Report: `11_run_dir_<key>.md` in this folder: what was built (hero vs blockout), every value actually used, the measurement table, pass/fail per §7.5, deviations from this brief and why.
- The look-dev script as `harness/FrontRoomsRunLookdev_<key>.cs.txt`.

---

## 8. Contract requests (exact proposals; for Red's approval, then the owners)

**Map chat (关卡设计).** Nobody edits these files except their owner.
- **CR-1 · A 27 m straight.** A module `Run_AlarmCorridor_1x9` (width 1, depth 9, Standard, `theme` = the host zone's). Today a module is placed only into a carved room that fits, and Standard rooms are 2–4 cells. Proposal: `public enum ModulePlacement : byte { Room, Corridor }` (append-only) and `public ModulePlacement placement = ModulePlacement.Room;` on `RoomModuleData`. `Corridor` lets the stamp claim a straight run of `depth` cells, one cell wide, whose long sides become walls, whose south edge is an `Arch` into the same zone, and whose north edge is the goal door (CR-6). The map chat may prefer a carve rule instead; the need is the 1 × 9 straight.
- **CR-2 · Trigger.** `ModuleTrigger.Alarm` on this module (already proposed in `RELAY_PURSUIT_REDESIGN.md` §9): arming 1.5 m inside the south edge, one shot per room instance, raises `RoomTriggered(Alarm, source, tag)` with `tag = "run"`. Add **`RoomReset(source)`** when the zone resets (WITHDRAW), so the corridor can return to armed.
- **CR-3 · Finish override.** `public enum ModuleFinish : byte { Zone, Run }` (append-only), `public ModuleFinish finish = ModuleFinish.Zone;`. With `Run`, the map gives this module's shell the Run materials (the visual chat supplies them through `FrontRoomsSurfaces`), splits the south edge like a theme border, and skips Level 0 piles, columns (`ModuleColumns.None`) and the Office kit (`ModuleFill.None`).
- **CR-4 · Lamps belong to the rig.** All 9 cells use `ModuleLamp.Off` (no map fixture, exists today). The visual chat's `FrontRoomsRunRig` builds the lights. The map exposes the module instance (cells, rotation, trigger state) to the rig when the chunk is built and dropped (`ChunkBuilt` / `ChunkDropped`, already requested by the wallpaper chat), and forwards `WarnStage` so the armed corridor can play the stage-1 warning on its own fixtures. The lamp-override layer (`LampFx` Dip / Sag / Warn) skips these cells.
- **CR-5 · Placement.** As §9 of the pursuit redesign: one per 2–3 zones, ≥ 15 cells from the start door, not in the first zone, the arch on its own zone's side, ≥ 9 cells from any Relay entry, never at a dead end unless its goal door leads on.
- **CR-6 · The goal door.** Red's decision 1 puts doors only on zone borders. Two ways: (a) let a module with `trigger != None` author `ModuleEdge.Door` (append-only) on its own perimeter, member RN-X (latching); or (b) place the module so its north edge lies on a zone border, where the map makes the door anyway (member chosen as Run because the module says so). (a) needs Red's OK because it changes decision 1. Direction A's locked side doors use the same edge kind, or stay as non-opening props.
- **CR-7 (C only) · Held door pairs.** `ModuleEdge.DoorPairHeld` (append-only) on inner edges: a 2.24 × 2.1 m pair, no latch, held open; on `RoomTriggered` the visual rig animates the close over 4 s; the map's door state machine treats each leaf as `Ajar` while closing and `Closed (unlatched)` after; the player pushes through on contact at walk or sprint without Use; the Relay pays +0.4 s per pair (as Ajar); the leaves block the Relay's sight except through the lites only if the glass track's sight rule allows (default: they block).

**Sound chat (声音设计).**
- **CR-S1 ·** a hard-floor footstep surface `Surface.Vinyl` (VCT), dampness 0, for every Run direction (append to `SoundIds.Surface`).
- **CR-S2 ·** a hard-corridor reverb for Run cells (short, bright, long tail down the axis).
- **CR-S3 ·** the direction's events from §2.7 / §3.7 / §4.7, raised by `FrontRoomsRunRig` as C# events with positions (`RunStateChanged(state, pos)`, plus per-object events). Per `AUDIO_CONTRACT.md`, the visual side never touches audio files.
- **CR-S4 ·** the rig's lights must not be picked up as "fixture hum" voices unless the sound chat wants them (today the hum follows the 4 nearest Lights registered by name).

**Interactables track.** The RN-X member (`Kit_DoorLeaf_Ward_Lite` = RN-F's leaf with a 0.25 × 0.75 m wired lite, plus `Kit_ExitDevice_Crossbar`, latch and lever trim) for all three; for C, the RN-F pair version (no latch) and `Kit_MagHolder`. **Glass track.** `Glass_Wired`.

---

## 9. Open questions for Red

1. **Pick:** A, B or C (after the renders). Or B with A's goal sign and C's entrance doors later.
2. **Goal door:** may a trigger module author its own door (CR-6a, changes decision 1), or must Run corridors end on a zone border (CR-6b)?
3. **Sound on the trip:** EGRESS says the building alarm is silent. Is a local device sound allowed (A: the clunk; B: contactor and transfer relays; C: magnet release and flashers)? All three are designed around one.
4. **Run in Office zones:** allowed (same module, Office anteroom), or Level 0 only?
5. **Title stream:** should the stream's Run profile (not in play) be rebuilt to match the pick, or left as it is?

---

## 10. Sources

All research sources are in `SOURCES.md` (section "03 Run! research") and `03_run_research.md` §10. This file adds no new external source. Every number here comes from the code (`file:line` in `01_inventory.md` and `03`), the research docs named at the top, or is a starting value marked SP. Media still wanted (1990 twin-head unit, crash-bar doors, door holder, hospital corridor, back-of-house corridor, early strobe or flasher) is listed in `media_candidates.md` §A; the C direction would add: a 1970s–80s glazed structural-tile corridor and economy-switched (delamped) fluorescent strips (to be appended there by the visual chat when it asks Red for the batch).
