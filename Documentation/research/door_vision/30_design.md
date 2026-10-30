# 30 — Door vision lites: the design (DA-L1 lite, what shows through, the PEEK, the Relay one-way rule)

- **Line:** 游戏视觉, session local_5656cffd (account A), door-vision-panel workflow, stage "Design".
- **Date:** 2026-10-30.
- **Status:** DESIGN. Nothing is built. Main is read-only; this file is the only write. Items marked **Q** wait for Red; items marked **CONTRACT** are asks to the line that owns the file; everything else is decided here within 游戏视觉's own files.
- **Red's asks (2026-10-30, verbatim):**
  - "所有双向门都加，包括地图上的门。最符合当年规范，玩家能隔着门窗偷看，也可能透过窗看见 Relay，是很经典的恐怖桥段。然后relay默认看不到，以及我需要你research一下如何解决门后房间加载的问题，看现有带有窗户的游戏都是怎么处理这个部分的去进行优化"
  - "同时让游戏视觉根据这个当年规范，调研这种门窗户的样式和结构，然后思考怎么revise，同时玩家可以靠近窗户去偷看窗户外部，具体的方式和玩家close up看撤离等一系列intractable是一样的"
- **Inputs:** `10_period.md` (period rules, the lite spec, sources P1–P26), `20_games.md` (shipped-game techniques, sources P/S/G/L/M), the stage audit of main (12:43; streaming numbers, sight rays, door kits), `../door_break/50_rethink.md` + `52_production.md`, and main's code read on 2026-10-30 (cited with file:line).
- **Units and frames:** 1 in = 25.4 mm. D1.6 door root: Y up from the floor, Z along the opening from the pivot axis (latch end Z 0.995), X through the leaf (face S = +X). Title pair: Z from the leaf's hinge edge.

---

## 0. Answer first

| Topic | Decision |
|---|---|
| The lite | **DA-L1**, one per leaf, every double-acting leaf: 10 × 30 in visible (254 × 762 mm), bottom 45 in (Y 1.143), top 75 in (Y 1.905). D1.6 map leaves: centre Z 0.400. Title / station pair: centred on the leaf (Z 0.598). Basis: Cal. Title 8 §3235(i) (≥ 200 sq in per leaf, bottom ≤ 48 in, wired glass named) [10_period P1] |
| Glass | 1/4 in polished wired glass, 6.8 mm, diamond mesh (12.5 mm, 0.46 mm wire), about 80 % light through, 9 % reflected. Drawn by `FrontRooms/Glass` with a new **filtered, analytic wire term** (no texture, no shimmer). Desktop: RT reflection (G14) on map lites; raster zone-cube everywhere else |
| Frame | Steel and laminate leaves: the existing pressed-steel kit section (19 mm face, 45° bevels, 2.5 mm proud, six oval-head screws on face S), pocket widened to 6.8 mm. Wood leaves: a mitred bead in the leaf's own wood (19 mm face, 45° chamfer, about 6 mm proud, no visible fasteners). Both live in the leaf's existing material slot: **no extra opaque draw** |
| LODs | The hole is real at **every** LOD (so the picture always matches the sight rules). Bead/kit: full section LOD0, 4-point section LOD1, flat ring LOD2. Glass in every LOD. The old "dark glass skin at LOD2" pattern is dropped |
| What shows through | One rule, two states. **LIVE:** the real room, drawn as it already is (no fake room anywhere). **DARK:** an opaque dark wired-glass pane that keeps its reflection (Valve's `func_areaportalwindow` "rendered window" [20_games P5], and what real glass shows when the far side is dark [G8]). LIVE ↔ DARK crossfades in 0.40 s. Map doors are LIVE whenever the far cell is built and dressed (always, in normal play). Title stream doors are LIVE only once the room behind is connected; until then DARK, which hides the rear seal. The station entrance and the shut terminal door are permanently DARK and have no hole |
| The peek | A new EXAMINE kind, **PEEK**, on the same registry and grammar as the placard: aim at the glass from ≤ 0.9 m → the E·1 eye + "E · LOOK THROUGH" → E → the camera glides to 0.10 m from the glass and looks through it → BACK leaves. Unlike other close-ups the **world keeps running** (**Q-A**). The threat gate refuses and ends it; a Relay seen through the glass gets a 0.8 s glimpse before the shot ends (**Q-B**) |
| Relay one-way | `RelaySeesThroughLites = false` (Red's default). The Relay's own sight ray keeps stopping at the solid leaf collider (it already does). Every **player-side** test (PlayerSeesRelay, RelayOnScreen, Arrive / step-in "unseen", Withdraw) sees through an intact or cracked lite. Seeing the Relay through a lite **counts** for ThreatPerceivable (proposal to 关卡设计 / 系统设计) |
| Door break | The kit ends 54 mm outside the cassette window (Z 0.600). The lite stays intact through E, M, D1, D2; at the broken pose it **cracks from a corner and holds** (GD3c Wired6 crack, no piece falls). SteelLite's special cassette cut is no longer needed |
| Not a window | The player cannot break a lite, it never sounds an alarm or calls the Relay, nobody climbs through it. Proposed answer to 系统设计: **a lite is not a window** |
| Prototype | Veneer_DA (walnut) + Steel_DA leaves, the title pair and the station skin, the wire shader, LIVE/DARK, the live PEEK with the Relay beat and the one-way rule as clone-only contract glue; one silent MP4 and one test round (§10) |

---

## 1. The lite per kit (DA-L1)

### 1.1 Which leaves get it

| Kit | Who uses it | Lite | Frame |
|---|---|---|---|
| `Kit_DoorLeaf_Veneer_DA` (walnut, `Door_Veneer`) | every shipped map door (`doorsNeedKeys` 0, `FrontRoomsLevel0.asset:50`) | DA-L1 at Z 0.400 | walnut bead |
| `Kit_DoorLeaf_Veneer_Oak_DA` | oak rows | DA-L1 at Z 0.400 | oak bead |
| `Kit_DoorLeaf_Ward_DA` (laminate, armor plates) | ward rows | DA-L1 at Z 0.400 | steel kit, satin finish like its armor (Ward's armor tops out at Y 0.831, 293 mm below the kit) |
| `Kit_DoorLeaf_Steel_DA` (+ PaintedMetal) | locked rows | DA-L1 at Z 0.400 | steel kit, `Door_Enamel` |
| `Kit_DoorLeaf_SteelLite_DA` | the RN-K option, unused | **retired**: its 4 × 25 in lite is half the legal area, sits inside the break window (Z 0.70–0.81) and is clear glass [10_period §0]. Moved to `_partial` when production lands, never deleted | — |
| `FrontRoomsDoubleDoorKit`, `Faces.Both` (title stream doors) | every stream room's end door | DA-L1 centred (Z 0.598 from the hinge edge) | walnut bead (C# mesh) |
| `FrontRoomsDoubleDoorKit`, `Faces.Plus` (station static entrance) | `FrontRoomsStationDress` | **applied skin, no hole** (§3.5): the leaf is a 2.25 mm skin on the station's data wall, so there is nothing behind it to see | walnut bead + DARK glass skin |
| Single-acting kits (`Kit_DoorLeaf_SteelLite`, etc.) | the dormant stop-door mode | unchanged: a 100 sq in narrow lite is legal on a non-DA door | — |
| C22 stairwell EXIT door | the steel exit with a crash bar | **none**: Title 8 forbids panic hardware on a DA door, so an EXIT door is never double-acting [P1] | — |

### 1.2 Geometry (all numbers from `10_period.md` §4, re-checked here)

**D1.6 leaves (0.99 m, 44 mm, door root):**

| Part | Z | Y | X / size |
|---|---|---|---|
| Visible glass | 0.273–0.527 | 1.143–1.905 | 254 × 762 mm |
| Glass blank (visible + 9.5 mm a side) | 0.2635–0.5365 | 1.1335–1.9145 | 273 × 781 × 6.8 mm, X ±0.0034 |
| Leaf cut-out | = glass blank | | through the 44 mm slab |
| Kit / bead outer edge | 0.254–0.546 | 1.124–1.924 | 19 mm sight face; steel 2.5 mm proud, wood about 6 mm proud |
| `lite_centre` anchor (new on every DA leaf) | 0.400 | 1.524 | X 0 |
| `lite_rect` sidecar meta | [0.273, 0.527, 1.143, 1.905] | | read by the map's sight rule and the peek, never a literal in C# |

Clearances: 54 mm of intact face to the break window (Z 0.600); 449 mm to the latch end; 240 mm to the hinge stile; 171 mm of top rail; lever / escutcheon (Z 0.920, Y ≤ 1.0) and tag plate (Z 0.790, Y 1.000) clear in both axes.

**Locked-door sign** (`sign_s/_p`, plate 254 × 76 mm): the lite covers today's anchor (Y 1.524, Z 0.500). It moves to the top rail, **centre Y 2.010, Z 0.400** (plate Y 1.972–2.048), 48 mm above the kit and 47 mm below the leaf top. The 10 in plate sits exactly over the 10 in lite. The sign stays an EXAMINE target ("READ SIGN"); at Y 2.01 its pose uses the high-face angle (40°, `InspectDef.HighFaceY` 1.9).

**Title / station pair (1.1955 × 2.639 m leaves, 44.5 mm):** visible glass Z 0.471–0.725, Y 1.143–1.905; kit Z 0.452–0.744, Y 1.124–1.924; 312 mm clear of the push plate (Z 1.056–1.157) and 849 mm above the kick plate. The two lites of a pair sit 1.198 m apart. If Red prefers the D1.6 numbers literally, the fallback is centre Z 0.400 (defaults table, §9.2).

### 1.3 Frames

**Steel kit (Steel_DA, Ward_DA):** the `Kit_DoorLeaf_SteelLite` section (`interact_door_leaf_steel_lite.py` `kit_section`), unchanged except the inner lip X ±0.0031 → ±0.0035 for 6.8 mm glass. Painted with the leaf (`Door_Enamel`, Ward: its satin armor finish). Six slotted oval-head screws on face S only (LOD0). Both faces otherwise identical.

**Wood bead (Veneer_DA walnut, Veneer_Oak_DA, title pair walnut):** mitred solid bead, 19 mm sight face, 45° chamfer at the glass, about 6 mm proud of the leaf face, hidden pins, grain running along each bead (metric UVs turned per side), the leaf's own wood and finish. The profile is an ESTIMATE until a manufacturer stop drawing is read [P21 returned 404]; it is one constant table in the generator.

**Material slots:** the bead and the steel kit are written into the leaf's existing submesh 0 (`Door_Veneer` / `Door_Oak` / `Door_Enamel`). Screws use the existing `Prop_Chrome`. Only the glass is a new slot (`Glass_Wired`). So a lite costs **one transparent draw and no opaque draw**.

### 1.4 LODs (switching at 4 m and 12 m, as the leaves do today)

| LOD | Slab | Frame | Glass | Added triangles (budget) |
|---|---|---|---|---|
| 0 | cut-out with its 44 mm reveal faces (dark core colour, hidden under the bead pocket) | full section; steel adds the six screws | closed 6.8 mm box | wood ≤ +400, steel ≤ +650 |
| 1 | the same cut (the LOD1 cutter `SteelLite_DA` already uses) | 4-point section (sight chamfer, face, outer edge) | the same box | ≤ +160 |
| 2 | a rectangular hole in the 148-tri slab | a flat ring quad per face | one quad per face | ≤ +40 |

Why the hole stays at LOD2: the Relay is perceivable up to 16 m (`RelayLitMetres`, `FrontRoomsMapHunter.cs:105`) and the player-side sight rule sees through the lite at any range. A lite that turns opaque at 12 m would hide a Relay the rules call visible. At 12 m the lite is 15 × 44 px at 1080p, enough to show a lit figure.

**Shadows.** The D1.6 "Leaf shadow" is a shadows-only copy of the map's cube (`FrontRoomsInteractableKit.Door.cs:545-558`). It becomes a shared shadows-only **holed box** (about 40 triangles; the hole = the visible glass), so a shadowed lamp (≤ 9 m) in a lit room behind the door throws a lit rectangle through the lite onto our floor, the period read of a lit door at night. The glass casts no shadow (as every `FrontRooms/Glass` pane). The title pair's leaf mesh casts its own (holed) shadow.

### 1.5 What the generator must assert (production and prototype)

- kit outer Z ≤ 0.546 and ≥ 0.050 m from the cassette window (Z 0.600) and from the `_FR_DAMAGE` mask (0.40 × 0.60 m at `damage_latch`);
- bead/kit to cut-out closure ≤ 0.1 mm; glass inside its pocket with ≥ 0.5 mm clearance;
- sign plate ≥ 40 mm above the kit and ≥ 40 mm below the leaf top;
- triangle budgets per LOD (§1.4);
- the sidecar carries `lite_centre`, `lite_rect`, `liteGlass` [0.273, 0.781, 0.0068] and an `inspect` block of kind `peek` (§4.2).

---

## 2. How the glass looks

### 2.1 The wire (new `_FR_GLASS_WIRE`, `shader_feature_local_fragment`, only lite materials compile it)

The mesh is sub-pixel at gameplay range (2.9 px pitch, 0.11 px wire at 3 m [10_period §5.3]), so a texture would shimmer. The shader draws it analytically in pane metres:

- **Pattern:** two families of parallel wires, `_WirePitch` 0.0125 m apart, turned by `_WireAngle` (45° = diamond m1, the 1992 US spec; 0° = square Georgian). `_WireWidth` 0.00046 m. The exact US rhombus is UNVERIFIED, so v1 is a square grid turned 45° and the angle stays a parameter until the media stage checks a photo.
- **Filtering:** per wire family, the coverage is the box-filtered pulse train over the pixel footprint (from `fwidth` of the pane coordinate): exact when the pitch is many pixels, fading to the mean `width / pitch` as the pitch nears 2 px. Total coverage C = 1 − (1 − Cu)(1 − Cv); the far-field mean is 7.2 %. `_WireSoftPx` widens the filter (the peek uses 2.5 px: the eye focuses on the far room, so the near wire goes soft).
- **Shading:** the wire is opaque steel. It adds C to the pane's alpha and its own lit colour (dark grey, a little specular), so it reads dark against a lit far side and glints when a lamp on the viewer's side catches it [10_period §5.3].
- **Cost:** about 25 ALU per glass pixel, no texture fetch.

### 2.2 Transmission, tint and reflection

| Quantity | LIVE value | Note |
|---|---|---|
| Light through | about 80 % (glass alpha face-on 0.14 + wire 7.2 %) | GGI 1/4 polished wire: VLT 80 %, R 9 % [P19] |
| Reflection | `_PaneF0` 0.08–0.09 face-on, rising with Fresnel (the shader already does two surfaces) | unchanged shader physics |
| Tint | a green-grey veil from the base colour (linear .02/.03/.022). The premultiplied blend cannot multiply the background, so the true green transmission tint is applied **in the peek** by the shot's colour filter (§4.5), where it matters; at gameplay range the veil is enough | "almost colorless with slight green tint" [P20] |
| Grime | `_FR_GLASS_GRIME` with lite-sized rules: a faint dust line inside the bead (always) plus at most ONE of: forehead/nose smudge at Y 1.55–1.70, hand smear at Y 1.15–1.35, or none, chosen by the door's edge hash ("say it once") | [10_period §7] |

The one-way-mirror effect needs no code. The shader already lays the reflection over a background dimmed by the pane, so a dark far room behind a lit corridor reads as a mirror, and a lit far room reads as a window. That is exactly the physics games rely on [G8], and it matches `RelayLit`.

**Reflections by tier:**
- **Desktop Mac (High/Ultra):** map lites are RT receivers (G14; `_RTReceive` 1, the prepass rendering-layer bit). They live under chunk roots, so the RT chunk scan registers them without map edits; the swinging leaf is a dynamic instance the RT system already tracks within 15 m. The RT stream gate (`FrontRoomsGlassRTSystem.cs:56-86`) fades from the zone cube as it does for windows. The peeked lite turns RT off (its reflection is cut to 15 % anyway, §4.5). Title-stream lites are not under the map, so they keep the raster zone cube.
- **Everything else:** the zone cube (`FrontRoomsZoneReflection`), as every pane today.

### 2.3 The two states: LIVE and DARK

| State | What the pane does | When |
|---|---|---|
| **LIVE** | transmits as §2.2: the real room behind, drawn as the game already draws it | the far side is ready (§3) |
| **DARK** | transmission 0: an opaque dark wired-glass pane (base linear 0.006), wire at low contrast, the same reflection on top. Valve's "rendered window" [P5] | the far side is not ready, or the door is a dead door |
| fade | LIVE ↔ DARK over **0.40 s**, sine in-out | never a pop |

Implementation (ours): two shared materials (`Glass_Wired`, `Glass_WiredDark`) keep the SRP Batcher; a lite that is fading borrows one of **four pooled fade materials** for 0.40 s and then returns to the shared one. A new render-only component `FrontRoomsDoorLite` keeps the registry, the state, the fade and the peek parameters. DARK is fully opaque (not 0.97), so a lamp-lit rear seal 0.15 m behind the glass can never show through.

### 2.4 Cracked and held (the broken door, §6)

At the broken pose the glass swaps once to a cracked state from GD3c's crack graph with the parked `Wired6` profile (`FrontRoomsGlassProfileData.cs:150-156`, `hangsOnWire = true`, keyed hash `FrontRoomsGlassFractureMeshes.Rand01`, deterministic per door): 2–4 crack lines from the hinge-side top corner and/or the latch-side bottom corner (the racked diagonal), crack-mirror faces as GD3c draws them, **no piece removed**, a 1–2 mm bow. A wired lite never shatters; that is the windows' look and would be wrong here. `Wired6` gains one field, the mesh angle (diamond), and its budget is sized for a 0.27 × 0.78 m pane. Fallback if the generator's S1-only path is not ready in time: three pre-baked crack motifs written by the same generator into the kit, chosen by edge hash.

---

## 3. What the player sees behind every door type

### 3.1 The rule

**No fake rooms.** Every LIVE lite shows the real room, which the game already draws (no baked occlusion is possible for a generated map, `FrontRooms3DGame.cs:482`, so rooms behind shut doors are drawn whenever they are in the frustum; a lite adds no scene draws). When the real room is not ready, the lite is DARK. This is the shipped pattern with the best record: Metroid Prime's doors wait for the room [S1], moved from the door to the glass; Valve's window fades to an opaque reflective pane when the room behind is closed off [P5]. Interior mapping, impostors and a camera per lite are rejected for the reasons in `20_games.md` §2.4 (they lie the moment the door swings, or cost a scene render per door).

### 3.2 Map doors (D1.6), the far room built and dressed: LIVE

- The far cell is next to the door, so it is in the player's chunk or a ring-1 neighbour. The map keeps a 5 × 5 chunk block built (radius 2, 24 m chunks, `FrontRoomsLevelProfile.cs:27`, `FrontRoomsMapWorld.cs:2243-2249`). Unbuilt ground begins 48 m away; the far plane is 46 m. **Any room a lite can show is built.**
- The 30 s revisit shift only happens when a chunk is rebuilt at ring 2 (`FrontRoomsMapWorld.cs:2265`), 48 m or more away: never through a lite.
- Lamps (16 m) and shadows (9 m) are measured from the player; a room seen through a lite 3–8 m away is inside both. Beyond 16 m the rooms are dark, so a long view through glass ends in darkness by itself.
- **Readiness for LIVE** = the far cell's chunk is built **and** furnished **and** its lamps have had their first evaluation (CONTRACT M-L4). In normal walking this is always true.

### 3.3 Map doors, the far room NOT ready: DARK, then LIVE

- It only happens after a jump: run start, the C22 floor change, `RebuildChunk` (tools, `SetStation`, `ClearStation`; `FrontRoomsMapWorld.cs:747`). Furnishing runs in 3 ms slices on desktop, 2 ms on WebGL (`FrontRoomsMapWorld.cs:684-686`), so a few lites near the player may be DARK for well under 2 s.
- **Chosen technique:** the existing preload (ring 2 always built) + the readiness gate + the DARK fallback. Optional (CONTRACT M-L5): the furnishing slices take chunks behind lites in view first.
- No peek is offered on a DARK lite. A peek in progress ends if its lite turns DARK or its chunk is rebuilt (`FrontRoomsInspect.AbortRequested`, as for every close-up target).
- Not chosen: a box-projected capture as a far layer. It would be frozen (no flicker, no Relay) and stale after a shift, and normal play never needs it.

### 3.4 Title stream doors (`FrontRoomsDoubleDoorKit`, `Faces.Both`)

| Moment | Behind the glass | Lite state |
|---|---|---|
| Door ahead, its room prepared but not connected (title crawl and frozen play) | the next room, unlit (`ResetRoomLights`, `FrontRoomsRoomStream.cs:1793`), with its **rear seal** 0.15 m behind the leaf plane (11.5 × 2.9 × 0.18 m, `FrontRoomsRoomStream.cs:1193`) | **DARK**: the seal can never show through. Physically right as well: a lit room looking at a dark room sees a mirror |
| The door starts to open (`BeginDoorOpening` drops the seal, :1979-1993) or the room is connected (:735, :805-808) | the real room | **LIVE** (0.40 s fade) |
| Rooms behind the player (play, frozen stream) | connected, lit rooms | LIVE (looking back through a shut door is honest: the room is there) |
| Terminal door during the start hold (≤ 3 s, the map not ready, north lamps 0) | `terminalSeal` 0.24 m past the line | **DARK**; no peek |
| Terminal door after `CloseTerminalDoor` (never reopens) | the dark terminal room, disposed rooms, unshadowed map lamps that light through walls | **dead door: DARK for good**, no peek |

Consequences:
- **The aligned-lite tunnel (20_games Q3) does not happen:** every door ahead is DARK until it starts to open, so at most one connected room is ever seen through a lite. The lites stay aligned and period-correct; no offset is needed. Q3 is closed without asking Red.
- **Recycling and re-dressing** only touch rooms that are not connected (DARK lites) or behind the camera. One guard is still asked (CONTRACT T-L2): a room is recycled only when the door between it and the player is outside the stream camera's frustum.
- **Peeks never happen on stream doors in practice:** in play every non-terminal door opens by proximity within ±4 m (`FrontRoomsRoomStream.cs:783`), the title crawl has no control, and the terminal door is DARK. The rule stays generic anyway.

### 3.5 Station static entrance (`Faces.Plus`, after `ClearStartArea`)

- The leaves are a 44.5 mm skin whose centre plane sits inside the 0.16 m data wall (`FrontRoomsStationDress.cs:150`); the wall's room face is only 2.25 mm behind the leaf face. A cut would show wallpaper in the hole, and behind the wall there is only the disposed stream.
- **Design:** no hole. The walnut bead stands on the station face; inside it a 1 mm DARK glass skin sits 0.5 mm proud of the leaf (no z-fight with the wall, which is 2.25 mm further back). With the station lit and the "room" behind dark, a real lite would read as a dark mirror, which is what this shows. Its reflection is the zone cube, or the RT reflection on desktop if the RT scan registers the station set; the prototype checks which.
- **No peek** (no aim box). The same "dead door" rule as the shut terminal door: the way back is dark glass. Alternative if Red wants a peek here: 关卡设计 cuts the data wall behind the lite and a shallow dark vestibule is built; not recommended (cost, and nothing to see).

---

## 4. The PEEK (Red: "靠近窗户去偷看窗户外部", the same way as the close-ups)

### 4.1 What the player does (desktop and phone)

1. Walks up to a shut door and looks at the glass from within **0.9 m** (eye to glass), within **35°** of face-on, standing or nearly still (≤ 0.5 m/s).
2. The crosshair shows the **E·1 eye** and the prompt **"E  ·  LOOK THROUGH"** (touch: the USE key reads LOOK THROUGH).
3. E (touch: USE). The camera glides in 0.40 s to just in front of the glass and looks through it. The HUD fades as for every close-up; the hint line shows **"E  ·  BACK"**.
4. Mouse / drag looks around through the slot; looking toward an edge leans the head toward it a little.
5. E, S or Esc (touch: the BACK chip or USE) leaves; the camera returns in 0.40 s.

### 4.2 It is an EXAMINE target of a new kind

- `InspectKind.Peek` (append-only enum) in `FrontRoomsInspect` (ours, `Rendering/Inspect`). It reuses the registry, `Find`, `CanOpen`, the hint line, the damped look, `Opened` / `Closed` (so the key HUD latches the eye exactly as for the placard) and `AbortRequested`.
- The target comes from the leaf kit's sidecar `inspect` block: kind `peek`, face `lite_centre`, size [0.254, 0.762], two-sided (the face toward the player is used), `reach` 0.9, `approachAngle` 35, sound `glass` (an ask, §8.6).
- **Aim box:** the visible glass inset 10 mm, as deep as the leaf plus 10 mm on each face, so it wins over the leaf collider under the existing "a box on a face wins up to 2 cm behind the physics hit" rule (`FrontRooms3DGame.cs:2595-2603`).
- `Available`: the lite is LIVE, the door is shut (leaf within 2° of closed; on an ajar door you just look through the gap), not broken.
- **Priority against the door (CONTRACT I-L1):** inside the peek gate the lite owns E; anywhere else on the leaf, or beyond 0.9 m, E opens the door as today. In a chase the threat gate hides the lite's prompt, so E always reaches the door. The ≤ 0.5 m/s condition keeps a player who walks up and presses E from peeking by accident; 交互设计 tunes it.

### 4.3 The shot (numbers)

| Item | Value | Why |
|---|---|---|
| Camera position | 0.100 m from the glass mid-plane, on the player's side, centred on the lite in Z, Y = clamp(eye, 1.35, 1.75) (1.62 for our eye) | `20_games` R5 asks 8–12 cm; at 0.10 m the glass fills 105° of the 108° screen width at 16:9 [10_period §8.4] |
| Frame read | the bead's chamfer shows as a thin dark sliver at both screen edges (about 50 px each at 16:9; wider on 19.5:9 phones). The view is a framed slot, as in life | geometry: the sight line at tan 1.315 vs the screen edge at 1.389 |
| Near plane | **0.02 m** while the camera is within 0.25 m of the glass, 0.06 m outside | at 0.06 m and 0.10 m distance, a 12° turn already pushes the near-plane corner into the bead; at 0.02 m no allowed turn can |
| World clamp | the rig's 0.1 m spherecast ignores the peeked leaf's collider | otherwise the clamp pushes the camera back off the glass (`FrontRoomsCameraRig.cs:524`) |
| FOV | the player's FOV (delta 0) | the lite frames itself; no zoom step, no CLOSER hint |
| Look | yaw ±35°, pitch ±30°, damped (Inspect `BaseLookSeconds` 0.12) | |
| Head lean | past 70 % of the yaw or pitch clamp, the camera slides along the glass toward that side: ±0.05 m across, Y within 1.35–1.75 | one input (look) on both desktop and touch |
| Travel in / out | 0.40 s in (Inspect `TravelMin`), sine in-out; out 0.40 s position, 0.15 s rotation (`FrontRoomsShotTimings.Inspect`) | the same feel as every close-up |
| Glimpse exit (Relay seen) | after the 0.8 s hold, out in 0.25 s | faster than BACK; **Q-B** |
| Threat exit (chase, blow, spotting) | at once, `Unlock.ChaseFreeSnap` 0.15 s | as every shot (`rig.CancelForRelay`) |
| Cooldown | 0.3 s (`Inspect.Cooldown`) | |

### 4.4 The world keeps running (Q-A)

Every EXAMINE close-up pauses the world (`SetPhase(Phase.Inspect)`, timeScale 0, C8; `FrontRooms3DGame.cs:2283, 3927-3931`). A peek cannot: its whole point is the live room behind the glass, lamps that stutter and a Relay that may walk in. Paused, the Relay would be a statue and every peek a free safe pause.

So the peek runs in **Phase.Playing** (CONTRACT I-L2): movement locked, look routed to the peek, everything else ticks. The player is exposed while peeking: the Relay can come up behind them, which is the price of looking. A lab switch `PeekPausesWorld` (default false) keeps the paused variant one flag away for Red's comparison.

### 4.5 How the glass reads in the shot

- **Far room:** sharp, live, lit by its own lamps.
- **Wire:** a soft diamond mesh in the foreground (86 px pitch, 3.2 px wire at 0.10 m, softened by `_WireSoftPx` 2.5).
- **Reflection:** down to 15 % of normal, ramped over the travel, as when you cup your hands against a window. RT is off on this lite.
- **Tint:** the shot volume adds a colour filter of (0.95, 0.985, 0.955) linear and −0.32 EV (80 % light through); vignette +0.10.
- **Adaptation:** the exposure eases toward the far cell's light with the EXAMINE adapt (`FrontRoomsInspectLook`, `AdaptTau` 0.6 s), capped at +0.5 EV. Cells below `RelayLitLevel` are never lifted into view, so a Relay standing in the dark stays a dark shape (§5.3).
- **Grime:** if this door drew the forehead smudge, it hangs out of focus in front of the view.
- On phones without the post stack the colour filter is skipped; the glass's own veil remains.

### 4.6 Entering and leaving

| Gate | Effect |
|---|---|
| `ThreatPerceivable(cam)` true (`FrontRoomsMapHunter.cs:347`) | no prompt, no entry (as today) |
| lite DARK, door not shut, door broken, in a shot, climbing, glass shot active | no prompt (`CanShowInspect` terms, `FrontRooms3DGame.cs:2258-2262`, plus `Available`) |
| E / S / Esc after 0.2 s, BACK chip | normal return |
| chase, ChaseFading, BlowHeard, Spotting hold, or `Threat` (chase or sight within 6 m) | immediate cancel |
| **RelayOnScreen through this lite** (lit Relay in view) | **0.8 s glimpse hold, then the 0.25 s return** (Q-B). During the hold, input does not cancel; nothing else changes |
| the leaf moves (the Relay opens it with v2 OPEN, a blow jolts it) | immediate cancel. The Relay opening the door you are peeking through is the jump scare this feature earns for free |
| lite turns DARK, chunk rebuilt, target disabled | `AbortRequested` |
| focus loss | Paused, with the world; resume returns into the peek |
| Duo unfold | leave the peek, then settings (Red 10-29: unfold during play opens settings) |
| caught | the existing caught flow |

### 4.7 Phones

- Doors are dressed on handheld (`DressOnHandheld = true`), so lites and the peek are on.
- Touch follows 交互设计's rules: the USE key's label follows the prompt, drag looks, the BACK chip leaves, no pinch zoom, **no TEXT panel** (a peek has nothing to read), the existing inspect-in/out haptics.
- No RT, no grime normal maps (dust film only), the same wire term.

---

## 5. The Relay one-way rule

### 5.1 Today

`Visible(from, to)` (`FrontRoomsMapHunter.cs:2149-2163`) casts on all layers; the nearest non-Relay collider decides. The D1.6 leaf collider (0.05 × 2.08 × 0.98, the whole leaf, `FrontRoomsMapWorld.cs:2769-2793`) blocks every ray both ways. So **the Relay already cannot see through a lite**, and neither can any player-side test.

### 5.2 The rule (CONTRACT M-L2, S-L1, S-L2)

- `RelaySeesThroughLites = false` by default (a 系统设计 tuning row). `Sees` (relay → player) keeps `Visible`, unchanged.
- A new `VisibleToPlayer(from, to)` for every **player-side** test: when the nearest blocker is a door-leaf collider and the hit point, in that door's root frame, lies inside `lite_rect` inset 10 mm, and the lite is not a dead door, the ray continues from 6 cm past the hit (at most 3 leaves per ray). It costs one extra raycast, only when a ray meets a leaf at lite height.
- It is used by: `RelayInView` (so `PlayerSeesRelay` and `RelayOnScreen`), `UnseenAt` and the arrival / step-in candidate checks (`FrontRoomsMapHunter.cs:1111, 1123, 1272, 1379`; V2 249–251, 397–401, 851, 884), and so Withdraw / FADE / Pressure, which read `PlayerSeesRelay`.
- The two-point test still applies: both the 1.60 m and the 1.95 m points must be seen. Through a lite whose top is at 1.905 m this holds unless the player stands far back while the Relay hugs the door; then the top of its head is behind the top rail and it is, correctly, only partly seen.
- **While peeking** (CONTRACT M-L3) the player-side tests also use the peek camera as a second eye: a place is "unseen" only if it is unseen from both. Otherwise the Relay could arrive in the 105° peek wedge, which the base eye 0.9 m back cannot see (20_games §4.2).
- An intact or cracked lite passes player sight and blocks the Relay's. A broken-open door is an opening.

### 5.3 Does seeing the Relay through a lite count for ThreatPerceivable? (proposal to 关卡设计 / 系统设计)

**Yes**, with `RelayLit`, through `RelayOnScreen` as above. Reasons:
- The player perceives the threat. A close-up that pauses the world must not open while the Relay is in sight through a door (it would be a free freeze), and the prompts should hide as they do for a Relay in the open.
- It is what ends a peek after the glimpse (§4.6).
- `RelayLit` keeps the physics honest: a Relay standing in the dark behind glass is not perceivable and does not trip the gate; the player may make out a dark shape, and that ambiguity is the scare.

Side effect for 声音设计: `StepsAudible` turns true when `PlayerSeesRelay` is true (`FrontRoomsMapHunter.cs:316-323`), so a Relay seen through a lite becomes audible. The map should expose `PlayerSeesRelayThroughLite` so the steps keep the door's occlusion (ask A-L3).

---

## 6. The door break and the lite

| Item | Design |
|---|---|
| Overlap | none. Kit Z 0.254–0.546 against the cassette window Z 0.600–0.995 (54 mm) and the damage mask (49 mm). The Relay's blow contact (forearm 0.15–0.35 m from the latch end, Z 0.645–0.845) is clear |
| `body` mesh | the break generator (`interact_door_break_common.py`) builds `body` from the **lited** slab through the same `extra` hook, so `body` inherits the cut; the window boundary loop and its 0.05 mm check are unchanged. Order: the lite lands in the leaf generators first; door-break P1 regenerates against it (both are 游戏视觉; whoever lands second rebases) |
| Lite parts on the swap | the glass and bead/kit are a `lite` part of the leaf kit. At the first damaging blow they move under the "Break pose" node with the rest, so the jolt, the lean and the lite move together |
| Stages E, M, D1, D2 | the lite stays intact (the blows land at the latch). It rides each jolt and elastic rock, which flickers its reflection for free |
| FINAL / BROKEN (pose a: 3° lean about the bottom pivot, 0.6° sag) | the glass swaps once to the cracked-and-held state (§2.4) on the FINAL frame, hidden by the same hit-stop, shake and dust as the cassette swap |
| Steel family (P3) | one steel cassette set for every steel leaf; "SteelLite: the steel cassettes, cut to avoid its lite" (`50_rethink.md` §3.6) is dropped |
| Breach light (desktop, from D1) | the spot 1.2 m behind the door now also shines through the lite: the Relay's head and torso silhouette in the glass while it bashes (RE7's silhouette, `50_rethink.md` §3.9). No extra cost |
| Record / persistence | the crack is part of the BROKEN record (`FrontRoomsDoorBreakRecord`); `Restore` re-applies it on `DressDoor` |

---

## 7. Budgets per tier (ESTIMATES; desktop is the reference)

| Item | Desktop (Mac/Win) | Phones (handheld) | WebGL |
|---|---|---|---|
| Triangles per leaf | LOD0 wood ≤ +400, steel ≤ +650; LOD1 ≤ +160; LOD2 ≤ +40 | the same LODGroup | doors are cubes (`DressOnWebGL = false`): nothing until **Q-C** |
| Draws | +1 transparent per lite in view; +0 opaque; +0 shadow (the shadow mesh is replaced, not added) | +1 per lite | see Q-C: +1 per lite (holed cube 1 draw + glass 1 draw) |
| Scene draws behind the lite | +0 (already drawn) | +0 | +0 |
| Pixel shader | wire ≈ 25 ALU on glass pixels; grime as windows | wire; dust film only | wire; no grime |
| Reflections | RT on map lites (G14), budget ≤ +0.3 ms GPU at a 3 m view of two lites; the peeked lite RT off | zone cube | zone cube |
| CPU | `FrontRoomsDoorLite` state + fades ≤ 0.02 ms/frame (lites within 20 m, readiness from cached chunk flags); `VisibleToPlayer` ≤ 10 extra raycasts per 10 Hz sight refresh | the same | the same rules, platform-identical |
| Memory | shared meshes ≤ 80 KB per family; 2 materials + 4 pooled fade materials | the same | — |
| Peek | near 0.02 m, shot volume (filter, exposure, vignette) | no post filter where the tier runs none | after Q-C only |
| Title pair lites | yes (raster reflection) | yes | gated `#if UNITY_WEBGL` until Q-C, so WebGL stays byte-identical |

---

## 8. Contracts (asks; this line edits none of these files)

### 8.1 关卡设计 (map), for a `CONTRACT_map_lite.diff`

- **M-L1 Lite data.** The map knows a DA leaf has a lite from the kit sidecar (`lite_rect`, `lite_centre`); no literal numbers in map code. The leaf collider stays one solid box (player bumps and the Relay's sight).
- **M-L2 Player-side sight through lites.** `VisibleToPlayer` as §5.2, used by `RelayInView`, `UnseenAt`, the V1 arrival probes and the V2 step-in / candidate checks. `Sees` unchanged.
- **M-L3 The peek eye.** While a peek runs, the game passes the peek camera pose (`relay.SetExtraEye(Pose?)`); player-side "unseen" tests must hold from both eyes.
- **M-L4 Readiness.** `bool CellReadyForLite(GridCoord cell)`: chunk built, furnished, first lamp evaluation done. An event when a chunk turns ready or is rebuilt. `ReadyAround(1)` stands in for it in the prototype.
- **M-L5 (optional) Furnish priority.** Chunks behind lites in view within 12 m furnish first after a jump.
- **M-L6 The sign anchor.** The locked-door sign moves to (Y 2.010, Z 0.400); the facade reads the anchor, so no code change is expected. Please confirm.
- **M-L7 Glass rules.** A lite is not a window: `Describe` never offers BREAK GLASS on it, it is never breakable by the player, never alarmed, never a climb. Aiming at a leaf outside the lite's peek gate keeps today's door prompts.
- **M-L8 The peek is silent.** It raises no noise event; the player is still.
- **M-L9 (door break, already asked as M3)** `PoseLeaf` pose (a): the lite rides the leaf, no extra ask.

### 8.2 关卡设计, the title stream (`FrontRoomsRoomStream.cs`), for a `CONTRACT_stream_lite.diff`

- **T-L1 State calls.** `FrontRoomsDoubleDoorKit.SetLiteLive(door, true)` where a room is connected (:735, :805-808) and where `BeginDoorOpening` drops the seal (:1992); `SetLiteLive(door, false)` (instant) on recycle (:985-986) and when a room is built; DARK during the terminal hold; `SetLiteDead(door)` in `CloseTerminalDoor` (:622).
- **T-L2 Recycle guard.** Recycle only when the door between the recycled room and the player is outside the stream camera's frustum (always true in the forward title crawl; a guard, not a behaviour change).
- The rear seals stay exactly as they are.

### 8.3 系统设计 (data rows in `Documentation/system_design/*.json`)

- **S-L1** `RelaySeesThroughLites` = false.
- **S-L2** Player-side sight (PlayerSeesRelay, PlayerPerceivesRelay, RelayOnScreen, the unseen tests) sees through intact and cracked lites; the threat gate counts a lit Relay seen through a lite.
- **S-L3** Peek glimpse hold 0.8 s (Q-B).
- **S-L4 The brief's question, "does breaking a lite count as a window for the alarm?"** Proposed: **no**. The player cannot break a lite; the only crack comes from the Relay's own door break, whose noise is the break itself. No alarm, no call, not in the alarmed-window set.
- **S-L5** Not proposed now: a Relay "watching at the glass" behaviour. The lites make it possible later.

### 8.4 交互设计 (interaction), for a `CONTRACT_interaction_peek.diff`

- **I-L1** The peek gate and priority (§4.2): reach 0.9 m (+0.1 hysteresis), approach 35° (+5°), speed ≤ 0.5 m/s, door shut; the lite owns E only inside the gate.
- **I-L2** `BeginPeek` / `EndPeek` in `FrontRooms3DGame`: stays in Phase.Playing (world live), movement locked, look routed to `FrontRoomsInspect.Tick` on the world clock, the exits of §4.6 including the 0.8 s glimpse. `InspectingNow` and `Inspecting` stay true for the HUD and touch layers, but no timeScale change.
- **I-L3** The camera rig: `ShotKind.Peek` (append-only), the world clamp ignores the peeked leaf, near plane 0.02 m inside 0.25 m of the glass.
- **I-L4** Touch: the USE label, drag to look, the BACK chip, no pinch, no TEXT panel, the existing haptics; Duo unfold leaves the peek first.
- **I-L5** The key HUD: `AimKind.Examine` (the E·1 eye) is reused; no new glyph. If 平面视觉 later wants a distinct peek eye, `AimKind.Peek` is appended then.

### 8.5 叙事设计 and 平面视觉

- **N-L1 (叙事设计)** the strings: desktop **"E  ·  LOOK THROUGH"** (two spaces either side of the middot, as every prompt), touch **"LOOK THROUGH"**. They may prefer another verb (LOOK IN, PEEK); the code takes it from `FrontRoomsInspectCatalog`.
- **G-L1 (平面视觉)** no new type: the existing prompt slot and hint line, the E·1 eye. Paper notices taped on lites (Title 8's "obstruction to vision") are a later dressing item through 平面视觉, not v1.

### 8.6 声音设计 (asks only; rule 24, this line makes no sound)

- **A-L1** `InspectSound` gains `Glass`: a soft touch on glass for the peek's in and out (or reuse Plastic).
- **A-L2** While peeking, the far room is a little less occluded (the ear is at the door).
- **A-L3** Steps of a Relay seen through a lite keep the door's occlusion (`PlayerSeesRelayThroughLite`, §5.3).
- **A-L4** The broken door's FINAL layer gains a wired-glass crack (a dry tick-crack; no shatter).

### 8.7 Our own lines (游戏视觉)

- **Door break (wf_2028e947-01b):** §6; regenerate `body` from the lited slab; the `lite` part moves under "Break pose"; drop the SteelLite cassette cut.
- **Glass track:** un-park `Wired6` for lites; add the mesh-angle field; size its budget for a 0.27 × 0.78 m pane.
- **WebGL track:** Q-C.

---

## 9. For Red

### 9.1 Questions (each blocks one step)

**Q-A. Does the world keep running while you peek?** *(blocks contract I-L2; the prototype films the recommended answer and keeps the other one behind a switch)*
- Background: every close-up so far (the evacuation plan, signs, keys, the station TV) pauses the world while you look.
- Option 1, live (recommended): the far room moves while you watch, lamps stutter, the Relay can walk in, and it can also come up behind you while you are pressed to the glass. Peeking is a risk you choose.
- Option 2, paused like the other close-ups: safe and consistent, but the room behind is a frozen picture and the Relay is a statue.
- Cost: option 1 needs the one live path in the game script (contract I-L2); option 2 needs nothing new but loses the beat you asked for.

**Q-B. When the Relay appears in your peek, does the shot hold for a moment?** *(blocks the exit timing, S-L3)*
- Option 1 (recommended): you see it for 0.8 s, then the camera pulls you back in 0.25 s. It cannot see you through the glass, so the glimpse is the point.
- Option 2: you are pulled back the instant it is on screen, as every other close-up does with a threat. Safer, but you barely see it.
- Cost: option 1 is one timer in the exit; nothing else changes.

**Q-C. Do WebGL players get the lites?** *(blocks the WebGL track only; not the prototype)*
- Today WebGL map doors are plain cubes and the title pair would be gated off, so WebGL players see no windows and cannot peek, while the sight rules (which are the same on every platform) still treat the lite as glass.
- Option 1 (recommended): the WebGL track adds the lite to the cube door and the title pair: +1 draw per lite in view (about +2 to +8 against the ~3,200-draw limit), no RT, no grime.
- Option 2: WebGL keeps blank doors.

### 9.2 Defaults this design takes unless Red says otherwise (not blocking)

| Item | Default | The other choice |
|---|---|---|
| Mesh | diamond (the 1992 US spec) | square Georgian (the UK product) |
| Height | period 45–75 in | modern ADA 43–73 in (everything 2 in lower; all clearances still hold) |
| Title pair position | centred on each leaf | Z 0.400 like the map leaves |
| Ward_DA frame | steel kit, satin | a paint-grade wood bead |
| Station entrance and the shut terminal door | dark glass, no peek | a wall cut and a dark vestibule (关卡设计) |
| Peek reach | 0.9 m, standing still | 交互设计 tunes |

---

## 10. Prototype scope (the next stage: one build, one test round, one MP4)

**Where:** a new clone `W/proj_doorlite` (W = `/Users/redwang/FrontRoomsVisualWork`) made with `tools/mkclone.sh` from main, then `install_tools.py proj_doorlite` (the silencer and the test bundle id). Not `proj_doorbreak`: both lines touch the D1.6 leaf generators, so they rebase at landing (§6). Main stays read-only. ≤ 2 Unity jobs through `tools/unity_jobs.sh` (wait when the other 游戏视觉 workflows hold the slots). Rule 24: no sound. Rule 23: no review stage, one test round. Harness ASCII, no tuples, no `GetInstanceID`. LF only.

**Build (in the clone):**

| # | File | What |
|---|---|---|
| 1 | `Tools/Blender/frontrooms_kit/assets/interact_door_lite_common.py` (new) | DA-L1 constants; the cut-out; the 6.8 mm glass; the wood bead section and the steel kit section (6.8 mm pocket) swept with mitres; LOD0/1/2 parts; the `lite` part tag; sidecar `lite_centre`, `lite_rect`, `liteGlass`, inspect block kind `peek`; the §1.5 asserts |
| 2 | `interact_door_leaf_veneer_da.py`, `interact_door_leaf_steel_da.py` | the lite through the existing `extra(kit, level, cuts)` hook (walnut bead; steel kit with screws) |
| 3 | `interact_door_da_common.py` | `sign_s/_p` → (Y 2.010, Z 0.400) |
| 4 | `Assets/Resources/Rendering/FrontRoomsGlass.shader` | `_FR_GLASS_WIRE` (pitch, width, angle, soft px), `_LiteDark`, `_PeekReflect`; materials `Glass_Wired`, `Glass_WiredDark` |
| 5 | `Assets/Scripts/Rendering/Doors/FrontRoomsDoorLite.cs` (new) | registry, LIVE/DARK with a readiness delegate (stub: `ReadyAround(1)` + a lab "force not ready" switch), the 0.40 s fade with 4 pooled materials, RT receive on desktop, the peek parameters |
| 6 | `Office/FrontRoomsInteractableKit.Door.cs` | the holed shadows-only leaf proxy; lite registration on `DressDoor` |
| 7 | `Rendering/FrontRoomsDoubleDoorKit.cs`, `Rendering/FrontRoomsStationDress.cs` | holed leaf mesh + walnut bead + glass for `Faces.Both`; the DARK skin + bead for `Faces.Plus`; `SetLiteLive` / `SetLiteDead`; gated off on WebGL |
| 8 | `Rendering/Inspect/FrontRoomsInspect*.cs` | `InspectKind.Peek`: the glass-relative pose, look clamps, head lean, near-plane switch, the world clock, the reflection / wire / volume parameters, `PeekPausesWorld` lab switch |
| 9 | clone-only glue, written out as contract diffs, **never applied to main by this line** | `FrontRooms3DGame.cs` + `FrontRoomsCameraRig.cs` (BeginPeek / EndPeek in Phase.Playing, the exits and the 0.8 s glimpse, `ShotKind.Peek`, the clamp ignore) → `CONTRACT_interaction_peek.diff`; `FrontRoomsMapHunter.cs` (`VisibleToPlayer`, the lite lookup, the peek eye) → `CONTRACT_map_lite.diff`; a stream stub for the title test bed → `CONTRACT_stream_lite.diff` |
| 10 | `Assets/Editor/DoorLite/FrontRoomsDoorLiteFilm.cs` (new, ASCII) | a deterministic Play-mode harness: a fixed seed with a lit corridor and a walnut DA door and a steel DA door; a title-pair test bed and a station-entrance skin; the Relay placed and walked by the tools API; scripted camera; `captureDeltaTime` 1/60; `frames.tsv`, `stats.json`, `film_log.txt` |

Not in scope: Oak_DA, Ward_DA, PaintedMetal (the same code path, production); `Kit_DoorLeaf_SteelLite_DA` (left untouched); WebGL; a device run; anything in main; real strings (the prototype shows the proposed "E  ·  LOOK THROUGH"); sound; the cracked-held lite if the GD3c S1-only path is not ready (then it moves to door-break production with the fallback motifs, §2.4).

**The MP4 (silent, about 50 s, 1920 × 1080, 60 fps, captions burned in, encoded once; checked as numbers plus one contact sheet):**

| Shot | Content | s |
|---|---|---|
| S1 | walk-up to the walnut DA door from 14 m to 0.8 m: LOD2 → LOD1 → LOD0 swaps, the wire filtering, the lit far room through the glass with the corridor's reflection | 7 |
| S2 | aim at 0.8 m: the E·1 eye and "E  ·  LOOK THROUGH"; E; the 0.40 s glide; look left, right, up; the head lean; BACK; the return | 9 |
| S3 | the Relay beat: a second peek; a lit Relay walks across the far room 2–4 m behind the glass; the 0.8 s glimpse; the 0.25 s pull-back. On-screen numbers: Sees = false, PlayerSeesRelay = true, RelayOnScreen = true, the hold timer | 8 |
| S4 | the dark Relay: it stands in an unlit cell behind the lite (RelayLit false); the peek goes on; a dark shape only | 4 |
| S5 | DARK fallback: force "not ready", the lite fades to dark reflective glass in 0.40 s and the prompt goes; ready again, LIVE | 5 |
| S6 | the steel DA door: the steel kit with face-S screws at 3 m and 1 m, LOD1 at 6 m | 5 |
| S7 | the title pair: the door ahead DARK with its seal behind it, the door opens and the lite turns LIVE; the station entrance's dark skin with its reflection | 7 |
| S8 | wire stability: 2 s lateral dollies at 3 m and 6 m | 4 |
| S9 | (if ready) the broken pose (a) with the cracked-and-held lite | 3 |

**The one test round (numbers only):**

| Gate | Pass |
|---|---|
| G1 Generator | triangle budgets per LOD (§1.4); kit outer Z ≤ 0.546 and ≥ 50 mm from Z 0.600 and from the damage mask; bead-to-cut closure ≤ 0.1 mm; glass pocket clearance ≥ 0.5 mm; sign ≥ 40 mm from the kit and the leaf top |
| G2 Compile | Roslyn + Unity compile clean for the macOS and iOS defines; no new warnings; harness ASCII, no tuples, no `GetInstanceID` |
| G3 Draws | +1 transparent draw per lite in view, +0 opaque, +0 shadow, measured on S1 / S6 frames |
| G4 Wire | S8: the lite's mean luminance varies ≤ 2 % frame to frame at 3 m (no shimmer); the mesh resolves (local contrast ≥ 10 %) at 1 m and in the peek |
| G5 Peek | the camera at 0.100 ± 0.002 m from the glass plane; near plane 0.02 m inside the 0.25 m band and 0.06 m outside it; no camera or near-plane corner inside any leaf or bead surface at the look extremes (geometric log); travel 0.40 s, return 0.40 s |
| G6 One-way | S3: Sees = false on 100 % of samples while the door is shut; PlayerSeesRelay = true on every sample where both points pass inside the lite; the glimpse ends 0.80 ± 0.02 s after the first RelayOnScreen frame |
| G7 States | LIVE ↔ DARK in 0.40 ± 0.02 s; no peek prompt while DARK; S7: the lite area over the seal no brighter than the DARK reference (the seal never shows); the station skin shows no z-fight |
| G8 Cost | `FrontRoomsDoorLite` ≤ 0.02 ms/frame; desktop RT on vs off on map lites ≤ +0.3 ms GPU at the S1 3 m frame |
| G9 Isolation | every other kit's FBX/JSON hash unchanged; `SteelLite_DA` untouched; WebGL build defines compile with the title-pair lite gated off |
| G10 Rules | LF only (`grep -lU $'\r'` empty); silencer present; ≤ 2 Unity jobs used |

**Deliverables:** the MP4 and its contact sheet, `stats.json`, the three contract diffs, a stage entry appended to a new `door_vision/50_prototype.md`, and verification images in the Shot format for the VERIFICATION LOG batch. Estimate: about 2 working days.

---

## 11. Sources

- Period rules and the lite spec: `10_period.md` §0–§9 and its sources P1 (Cal. Title 8 §3235, https://www.dir.ca.gov/title8/3235.html), P5 / P6 (San Diego CWP 08110 Feb 1991, 08800 Feb 1992), P10 (wired glass), P15 (1985 lite-kit patent), P16–P20 (wired glass data), P21 (wood stops).
- Game techniques: `20_games.md` §2–§5 and its sources P3–P5 (Valve areaportal and `func_areaportalwindow`, https://developer.valvesoftware.com/wiki/Func_areaportalwindow), P13 (DunGen), S1 (Metroid Prime doors), G1 (interior mapping), G8 (one-way mirror), M1–M4 (monsters through glass; one-way sight as a rule).
- Door break: `../door_break/50_rethink.md` §1.3, §3.4–§3.10; `52_production.md`.
- Our code (main, read 2026-10-30): `Rendering/Inspect/FrontRoomsInspect.cs` 1-120, `FrontRoomsInspectDef.cs`, `FrontRoomsInspectable.cs`, `FrontRoomsInspectCatalog.cs` 14-51; `FrontRooms3DGame.cs` 479, 482, 2231-2303, 2585-2665, 3914-3931, 4040-4075; `FrontRoomsShots/FrontRoomsCameraRig.cs` 6-17, 269-272, 524; `FrontRoomsShotTimings.cs` 62, 180-194; `FrontRoomsMap/FrontRoomsMapHunter.cs` 100, 105, 260-350, 1111-1123, 1272, 1379, 2088-2163; `FrontRoomsMap/FrontRoomsMapWorld.cs` 209, 684-686, 747, 849, 2243-2272, 2769-2793; `FrontRoomsRoomStream.cs` 1-60, 551-556, 640-660, 720-812, 950-990, 1193, 1975-1995; `Rendering/FrontRoomsDoubleDoorKit.cs` 1-75; `Office/FrontRoomsInteractableKit.Door.cs` 1-70, 545-558; `Rendering/Glass/FrontRoomsGlassProfileData.cs` 150-156; `Rendering/Glass/FrontRoomsGlassSceneColor.cs` 1-30; `Rendering/GlassRT/FrontRoomsGlassRTSystem.cs` 1-100; `Resources/Rendering/FrontRoomsGlass.shader` 1-150; `Tools/Blender/frontrooms_kit/assets/interact_door_leaf_steel_lite.py`, `interact_door_leaf_steel_lite_da.py`, `interact_door_da_common.py` 214-237.
