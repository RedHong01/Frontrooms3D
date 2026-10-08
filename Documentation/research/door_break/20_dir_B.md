# 20 — Direction B · "The Lock Hole": pre-render build (DB1)

Status: **PRE-RENDER, done 2026-10-07 19:5x.** Red asked for a pure-visual proposal first; nothing lands in the game until he picks. Nothing in the game, the kit modules or Red's project code was changed. The only files this track writes under `Frontrooms3D` are this report, the images in `images/` (`db_B_*`, `dir_B_*`) and two index rows (`VERIFICATION_LOG.md` VL136, `media_candidates.md` §20). In Figma it placed one verification slide, VL136 (frame `2805:6338`), and updated the log's cover. No media was downloaded.

Brief: `10_design.md` §8.0 (common setup) and §8.2 (Direction B). Words (P, S, BURST, RIP, M, D1, D2, D2+, FINAL, S4) as in `10_design.md` §1.1. `$W` = `/Users/redwang/FrontRoomsVisualWork`; this direction's work folder is `$W/door_break/B/`.

History: built on 2026-10-03 in the old scratchpad, which the 2026-10-05 reboot wiped (scripts, models and renders lost). On 2026-10-07 16:3x the scripts were replayed from the agent transcripts into `$W/door_break/B/` (`_recover/`), the kit and the Relay poses were rebuilt, and every frame was re-rendered and re-checked (§5). The finals started at 17:36, stopped at the 17:46 usage limit after 4 frames, and resumed at 18:16.

---

## 0. Short answer for Red

- **It reads: The Shining at true scale.** Each blow takes one piece of the lock away. D1: the rose cracks, the lever droops, and 5 grain splits run up and down from the bore. D2: the lockset drops at your feet and **a real 54 mm hole opens at 1.0 m, lit from behind**. D2+: the Relay's lens fills the hole. FINAL: the stile tears out round the hole. Like the glass, every stage adds to the last one.
- **In play the hole is a dot.** At 3 m and the game's 76° FOV the 54 mm hole is **12 px** wide at 1080p (`dir_B_D2_V1w.jpg`). With the near lamp at 15 % it is the brightest thing in the frame. With both rooms lit (L-C) it is a pale disc, not a hole. **B needs a lit far room or a dark near room**, as the brief said.
- **The close-ups are the money.** At 0.3–0.6 m (V3) the torn 0.6 mm veneer lip, the crushed particleboard in the bore, the lens behind the hole and the gloved fingers each read in one look.
- **RIP is the scariest beat.** Your own lever jerks 30° down, then your rose is pulled into the face and the hole lights up, then two black fingers come through **88 and 70 mm** toward you and hook toward the latch.
- **Steel is the weak member.** The punched cylinder leaves a **32 mm hole in the skin (29.6 mm clear)**: 7 px in play. The enamel star-crazing (11 radials, 3 rings, grey primer) reads in the loupe and at 0.6 m, not at 3 m in play. No fingers (they do not fit).
- **Cost is low** (§4): a veneer stage set of ≤ 4.1k tris (slab + damage parts), lock parts reused as baked debris, one new Relay beat (peek / reach) and a 1-draw finger mesh.
- **My recommendation:** use B for the D2 → FINAL beat on lever doors (OF-F, and L0-F if Red accepts the staging, §6 item 5), together with Direction A's jamb split for the last blow, as `10_design.md` §9 suggests. Use the steel coin hole only under L-B or L-D.

---

## 1. What was built

### 1.1 Members and the kit they use

The intact doors are the interactables kit, built from the current modules in `Frontrooms3D/Tools/Blender/frontrooms_kit/assets/` (read-only import; files unchanged since 2026-10-03 16:56) into `$W/door_break/B/kit_blend/` on 2026-10-07 16:34 (`build_kit.py`, 0 failures). The leaf and frame keep their parts separate (slab, hinge halves, faceplate), so the stage scripts cut only the slab.

| Member | Leaf | Frame | Lock | Other |
|---|---|---|---|---|
| **OF-F** (W-S) | `Kit_DoorLeaf_Veneer_Oak` (44 mm solid core: the IP's "solid-core doors"; oak slot `Prop_WoodOak`, chrome hinges) | `Kit_DoorFrame_Steel` | `Kit_Lock_Rose` + `Kit_Lock_Lever` on both faces at (±0.022, 1.000, 0.920), `Kit_Lock_Latchbolt_Bored`, `Kit_Lock_StrikeBored` | Closer on S: `Kit_DoorCloser_Body / Arm / Forearm / Shoe` |
| **L0-K** (S-S) | `Kit_DoorLeaf_Steel` (almond enamel `Door_Enamel`, kick plates) | `Kit_DoorFrame_Steel` | `Kit_Lock_Escutcheon` + `Kit_Lock_Knob` (knob at Y 0.9365) + IC cylinder (`Kit_Lock_CylinderShell`, `Kit_Lock_Plug`) at Y 1.000 on both faces, `Kit_Lock_Latchbolt_Mortise`, `Kit_Lock_Deadbolt`, `Kit_Lock_StrikeMortise` | `Kit_DoorSign` and `Kit_DoorNumberPlate` "14" on both faces |

### 1.2 Stages (render-side concept geometry; every stage contains the earlier damage)

Tier 1 clock: D1 at 1.5 s, D2 at 2.0 s, D2+ between 2.0 and 2.4 s, FINAL at 2.4 s, the break at 2.5 s (`10_design.md` §2.2).

**OF-F, oak veneer (`db_door.py`)**

| Stage | What changes |
|---|---|
| **S0** | The intact kit |
| **M** (RIP only) | Your (P) lever jerked **30° down**; the S lever turns with it on the shared spindle (the Relay works the S lever) |
| **D1** | The S rose cracked (a radial crack through the die-cast flange), pushed **5° crooked** and 2.5 mm proud; the S lever droops 24°. A ragged ring of veneer torn round the rose. **5 grain splits** run from the bore **up and down only**, **85–195 mm** long, V-section **2.4 mm wide and 3.5 mm deep** at the bore, each with a pale lifted lip of raw wood. 2 short crush splits on the P face. 6 veneer flakes and 8 fibre crumbs on the S floor |
| **D2** | The lockset is driven through toward S. **The real 54 mm cross bore** (2-1/8", backset 70 mm) opens at Y 1.0 m. S face: veneer and crossband torn out round the bore, longer along the grain; a **0.6 mm veneer lip** in petals bent 25–75° toward S, translucent at the edges. Bore wall: the leaf in section by depth (face veneer, pale crossband, granular particleboard core) with crushed granules on its lower lip. The latch tube is left in the bore. **6 splits**, now 92–224 mm, 3.6 mm wide, 6 mm deep. P face: the rose's crushed dish. **Floor (S):** the cracked rose, the lever and the chassis with its two through-bolts, 46 + 18 particleboard crumbs, 10 flakes. **Floor (P):** your-side trim, beaten off first |
| **D2+** | The same leaf. BURST: Giant B's opal lens fills the hole (pose `peek`, §1.4). RIP: two gloved fingers through the bore (§1.5) |
| **FINAL** | The stile tears: a ragged **notch about 80 × 120 mm** from the bore through the lock edge at 1.0 m, with hardwood edge-band splinters standing along the grain; the latch and faceplate go with it. The leaf jolts **8 mm** at the latch edge (0.47°). 38 particleboard crumbs and 9 splinters in the air |
| **S4** | The leaf at **80°** into S, **1° crooked** on the bottom hinge (wood, `10_design.md` §3.2). The notch and the hole stay. The closer arm swung back along the leaf, the forearm hanging 70° from the bent elbow stud. The lockset, the latch with its faceplate and an edge chunk on the S floor; 30 crumbs and 7 splinters further out; 3 drops of closer oil |

**L0-K, almond enamel hollow-metal (`db_steel.py`)**

| Stage | What changes |
|---|---|
| **S0** | The intact kit |
| **D1** | The lock case shoved toward S: the S escutcheon rocks out **10°** on its bottom screw, the S cylinder is driven **8 mm** out of its collar, the knob droops 10°. The enamel round the cylinder bulges and star-crazes: **5 radial cracks (0.62 mm) and 2 ring arcs**, reaching past the escutcheon, with small enamel chips popped off along them. 5 enamel chips on the floor |
| **D2** | The P cylinder punched in, the S cylinder driven out. A **32 mm hole** at Y 1.0 m (29.6 mm clear through the case), inside a **60 mm dish** pushed out toward S (the punch came from P), its rim flared 1–2 mm. **11 radial cracks** (about half fork once) **and 3 ring arcs** in the enamel; the enamel flakes to **grey primer** in jagged pieces near the hole and in small chips along the radials; **bare steel** in a 3 mm ragged band at the rim. The S escutcheon's top half is bent out **78°** about a tear line, its top screw pulled; the S knob is torn off its spindle (the 8 mm square spindle and the sheared shank ring show). The hole's wall is the mortise case's threaded bore (dark zinc). **Floor (S):** the knob, the cylinder shell and plug, 22 enamel chips, 10 primer flakes. **No fingers** (Giant B's ~33 mm finger does not fit a 29.6 mm hole) |
| **D2+** | BURST: the lens light behind the hole (the hole goes from lit-room warm to lens white) |
| **FINAL** | The leaf jolts 8 mm at the latch edge; 16 enamel chips in the air |
| **S4** | The leaf at 80°, square (steel hangs square). The popped latch-side frame silencer and 30 gypsum crumbs join the floor debris |

The steel crack map is a 2048² procedural image over a 150 mm patch (R crack, G primer, B bare steel, A height) on a 160 × 56 polar mesh, so it can be baked straight into the `Door_DamageMarks` atlas later (`10_design.md` §4.2).

### 1.3 The set, the lights and the render (§8.0)

- One 0.16 m wall on a cell line, the 1.0 × 2.1 opening centred on a 3 m cell edge. **P** = Low 2.4 m Level 0 (Chevron wallpaper, loop-pile carpet, fissured tiles). **S** = Standard 2.9 m: **Office** for OF-F (drywall, carpet tile, 2 × 2 tiles, parabolic louvres), Level 0 for L0-K. Each room 6 × 6 m (Z −1 → 5). Four 0.6 × 1.2 troffers per room. Textures: a snapshot of `Assets/Resources/Surfaces` in `$W/door_break/B/tex/`.
- **L-B** near lamp 15 %, far steady, plus the breach spot (1.2 m behind the door on the Relay's side, 1.8 m up, aimed at the lock; 35 / 55 / 260 / 120 W at D1 / D2 / FINAL / S4; the bulb itself is not seen by the camera, like a game light). **L-C** Office louvre 5.5 (165 W per lamp) and the −22 % saturation grade. **L-D** near steady, far dead: the lens is the only light behind the door. Thin homogeneous dust in both rooms (density 0.022 in L-B, 0.014 in L-D, 0.008 otherwise). The Office grade also applies to every camera standing in the Office room.
- **Calibration** to audit frame 77 (`../interaction_audit/images/77_relay_broken_witness.png`): the lit Level 0 wallpaper in the far-side frames (P room, steady lamp) has a mean linear luminance of **0.221** over three patches (0.267 and 0.226 in `db_B_OFF_burst_D2P_V4w_LB`, 0.171 on the back wall in `_V4_LB`) against **0.183** for frame 77's two wall patches (0.241 lit, 0.125 lower): **+0.27 stop**, inside ±0.3 (the near lit patch alone: +0.15 stop). Exposure −1.6 EV under AgX. The render's wallpaper is a little more saturated than frame 77's (the game's post desaturates it).
- **Render:** Blender 4.3, Cycles on Metal, 1920 × 1080, 256 samples (adaptive, threshold 0.01, min 64), OpenImageDenoise with albedo and normal, AgX (the kit preview's view transform), exposure −1.6 EV, mild bloom on emitters only (the URP bloom stand-in). Strip loupes 720 × 720 at the same settings. JPG q85, 4:4:4.
- **Scale check** (§8.0): `db_B_OFF_burst_D2_V1_LB_scale.jpg` has a plain 1.80 m figure at the latch side: the hole sits at hip height.

### 1.4 The Relay: Giant B in two new poses

Built with `creatures/giant_b_night_shift.py` (unchanged since 2026-10-02 20:40, so W1 has not changed the body), `LENS_GLOW` 3.0, placed in the door root (`build_giant.py` → `creature/Giant_B_peek.blend`, `Giant_B_lever.blend`).

- **`peek`** (BURST, on P, Low 2.4 m): a variant of `low`. The spine folds to 58°, the hood hangs square to the leaf, **lens centre at 0.99 m**, the **hood hem on the leaf**. Left glove flat on the wall past the latch jamb, right glove flat on the leaf below the lock. Top 2.354 m (under the 2.4 m ceiling). 15,652 tris.
- **`lever`** (RIP, on S, 2.9 m): a variant of `std`. Stooped at the latch side, lens at 1.52 m looking down at the lever, **left glove closed over the S lever**, right glove on the wall. Top 2.727 m. 15,404 tris.
- **The brief's lens distance cannot hold:** it asks for the lens 0.25 m off the P face *and* the hood touching the leaf. Giant B's lens sits only 17 mm inside its hood hem, so I kept the hood on the leaf (lens 0.11 m off the face).
- **Colour:** the coverall is #2E3B33 (the slot's sRGB value, linearised); its sheen is cut to 0.04 and tinted, or AgX washes it to pale sage.

### 1.5 The fingers (RIP D2+)

Two of Giant B's gloved fingers, stacked, forced through the bore from S. Giant finger ~33 mm (1.65 × a person, `10_design.md` §11 item 7). The black canvas squeezes to **26 mm** in the bore, so two fill it; past the hole they swell back to 30 mm. They stand **88 mm and 70 mm proud** of your face, and the last joint hooks **52° and 38° toward the latch**. Knuckle bulge and crease, a stitched side welt, and the palm set back from the S face so thin crescents of light outline the fingers. Worn black canvas (the `Prop_FabricChair` weave, roughness ≥ 0.84, a low grey sheen). Concept mesh 34,200 tris at render subdivision (2 fingers, 2 welts, palm); a game mesh needs about 1.5k (§4.1).

---

## 2. Frames

All in `images/`. Names: `db_B_<member>_<case>_<stage>_<view>_<light>.jpg`; strips `db_B_strip_<member>_<case>_<light>.jpg`; key frames again as `dir_B_<stage>_<view>.jpg`. Views as §8.0, plus: **V3c** = V3's eye with a 22° lens (the hero crop); **V1z** = V1's eye with a 9° lens on the lock (the strip loupes, 720 × 720); **V3a** = the player crouched at the hole (eye 1.08 m, 0.43 m off the S face, 15° off the bore axis: the straightest look through the 44 mm-deep bore); **V4b** = the peek pose from the hinge side, low; **V4c** = RIP, the glove on the S lever, from S.

**Stage strips** (3840 × 720; six or five panels from V1 at 45°, each with a true-optics loupe from the same eye in the corner above the lock):
- `db_B_strip_OFF_burst_LB.jpg` (= **`dir_B_strip.jpg`**): S0 → D1 → D2 → D2+ → FINAL → S4, leak light.
- `db_B_strip_OFF_burst_LC.jpg`: the same under the Office light.
- `db_B_strip_L0K_burst_LB.jpg`: S0 → D1 → D2 → D2+ → FINAL → S4, leak light.
- `db_B_strip_OFF_rip_LB.jpg`: S0 → M → D2 → D2+ (fingers) → S4, from P, leak light.

**Key frames** (`dir_B_*`):

| Key frame | Source frame |
|---|---|
| `dir_B_strip.jpg` | `db_B_strip_OFF_burst_LB.jpg` |
| `dir_B_S0_V1.jpg` | `db_B_OFF_burst_S0_V1_LB.jpg` |
| `dir_B_D1_V3c.jpg` | `db_B_OFF_burst_D1_V3c_LC.jpg` |
| `dir_B_D2_V3c.jpg` | `db_B_OFF_burst_D2_V3c_LB.jpg` |
| `dir_B_D2P_V3c.jpg` | `db_B_OFF_burst_D2P_V3c_LD.jpg` |
| `dir_B_FINAL_V3c.jpg` | `db_B_OFF_burst_FINAL_V3c_LB.jpg` |
| `dir_B_S4_V5.jpg` | `db_B_OFF_burst_S4_V5_LB.jpg` |
| `dir_B_D2_V1w.jpg` | `db_B_OFF_burst_D2_V1w_LB.jpg` |
| `dir_B_D2P_V4b.jpg` | `db_B_OFF_burst_D2P_V4b_LB.jpg` |
| `dir_B_RIP-M_V4c.jpg` | `db_B_OFF_rip_M_V4c_LB.jpg` |
| `dir_B_RIP-D2P_V3c.jpg` | `db_B_OFF_rip_D2P_V3c_LB.jpg` |
| `dir_B_STEEL-D2_V3c.jpg` | `db_B_L0K_burst_D2_V3c_LB.jpg` |
| `dir_B_STEEL-D2P_V3a.jpg` | `db_B_L0K_burst_D2P_V3a_LD.jpg` |

**Every frame:**

| File | Member, case | Stage | View, light | Shows |
|---|---|---|---|---|
| `db_B_L0K_burst_D1_V1_LB.jpg` | L0-K BURST | D1 | player at 3 m, 45°, L-B leak | escutcheon rocked, crazing |
| `db_B_L0K_burst_D1_V1z_LB.jpg` | L0-K BURST | D1 | loupe: V1's eye, 9°, on the lock, L-B leak | escutcheon rocked, crazing |
| `db_B_L0K_burst_D1_V3c_LB.jpg` | L0-K BURST | D1 | V3's eye, 22° crop, L-B leak | escutcheon rocked, crazing |
| `db_B_L0K_burst_D2P_V1_LB.jpg` | L0-K BURST | D2+ | player at 3 m, 45°, L-B leak | the lens light in the coin hole |
| `db_B_L0K_burst_D2P_V1z_LB.jpg` | L0-K BURST | D2+ | loupe: V1's eye, 9°, on the lock, L-B leak | the lens light in the coin hole |
| `db_B_L0K_burst_D2P_V3_LD.jpg` | L0-K BURST | D2+ | close-up, 0.6 m, 62°, L-D dark behind | the lens light in the coin hole |
| `db_B_L0K_burst_D2P_V3a_LD.jpg` | L0-K BURST | D2+ | crouched at the hole, L-D dark behind | the lens light in the coin hole |
| `db_B_L0K_burst_D2P_V3c_LD.jpg` | L0-K BURST | D2+ | V3's eye, 22° crop, L-D dark behind | the lens light in the coin hole |
| `db_B_L0K_burst_D2_V1_LB.jpg` | L0-K BURST | D2 | player at 3 m, 45°, L-B leak | knob off, 32 mm coin hole |
| `db_B_L0K_burst_D2_V1w_LB.jpg` | L0-K BURST | D2 | player at 3 m, 76° (game FOV), L-B leak | knob off, 32 mm coin hole |
| `db_B_L0K_burst_D2_V1z_LB.jpg` | L0-K BURST | D2 | loupe: V1's eye, 9°, on the lock, L-B leak | knob off, 32 mm coin hole |
| `db_B_L0K_burst_D2_V3_LB.jpg` | L0-K BURST | D2 | close-up, 0.6 m, 62°, L-B leak | knob off, 32 mm coin hole |
| `db_B_L0K_burst_D2_V3c_LB.jpg` | L0-K BURST | D2 | V3's eye, 22° crop, L-B leak | knob off, 32 mm coin hole |
| `db_B_L0K_burst_FINAL_V1_LB.jpg` | L0-K BURST | FINAL | player at 3 m, 45°, L-B leak | jolt + chips |
| `db_B_L0K_burst_FINAL_V1z_LB.jpg` | L0-K BURST | FINAL | loupe: V1's eye, 9°, on the lock, L-B leak | jolt + chips |
| `db_B_L0K_burst_S0_V1_LB.jpg` | L0-K BURST | S0 | player at 3 m, 45°, L-B leak | intact |
| `db_B_L0K_burst_S0_V1z_LB.jpg` | L0-K BURST | S0 | loupe: V1's eye, 9°, on the lock, L-B leak | intact |
| `db_B_L0K_burst_S4_V1_LB.jpg` | L0-K BURST | S4 | player at 3 m, 45°, L-B leak | leaf at 80°, parts on the floor |
| `db_B_L0K_burst_S4_V1z_LB.jpg` | L0-K BURST | S4 | loupe on the S floor, L-B leak | leaf at 80°, parts on the floor |
| `db_B_L0K_burst_S4_V5_LB.jpg` | L0-K BURST | S4 | S floor afterwards, L-B leak | leaf at 80°, parts on the floor |
| `db_B_OFF_burst_D1_V1_LB.jpg` | OF-F BURST | D1 | player at 3 m, 45°, L-B leak | rose cracked, 5 grain splits |
| `db_B_OFF_burst_D1_V1_LC.jpg` | OF-F BURST | D1 | player at 3 m, 45°, L-C Office | rose cracked, 5 grain splits |
| `db_B_OFF_burst_D1_V1z_LB.jpg` | OF-F BURST | D1 | loupe: V1's eye, 9°, on the lock, L-B leak | rose cracked, 5 grain splits |
| `db_B_OFF_burst_D1_V1z_LC.jpg` | OF-F BURST | D1 | loupe: V1's eye, 9°, on the lock, L-C Office | rose cracked, 5 grain splits |
| `db_B_OFF_burst_D1_V3c_LC.jpg` | OF-F BURST | D1 | V3's eye, 22° crop, L-C Office | rose cracked, 5 grain splits |
| `db_B_OFF_burst_D2P_V1_LB.jpg` | OF-F BURST | D2+ | player at 3 m, 45°, L-B leak | Giant B's lens in the hole |
| `db_B_OFF_burst_D2P_V1_LC.jpg` | OF-F BURST | D2+ | player at 3 m, 45°, L-C Office | Giant B's lens in the hole |
| `db_B_OFF_burst_D2P_V1w_LD.jpg` | OF-F BURST | D2+ | player at 3 m, 76° (game FOV), L-D dark behind | Giant B's lens in the hole |
| `db_B_OFF_burst_D2P_V1z_LB.jpg` | OF-F BURST | D2+ | loupe: V1's eye, 9°, on the lock, L-B leak | Giant B's lens in the hole |
| `db_B_OFF_burst_D2P_V1z_LC.jpg` | OF-F BURST | D2+ | loupe: V1's eye, 9°, on the lock, L-C Office | Giant B's lens in the hole |
| `db_B_OFF_burst_D2P_V3_LD.jpg` | OF-F BURST | D2+ | close-up, 0.6 m, 62°, L-D dark behind | Giant B's lens in the hole |
| `db_B_OFF_burst_D2P_V3a_LD.jpg` | OF-F BURST | D2+ | crouched at the hole, L-D dark behind | Giant B's lens in the hole |
| `db_B_OFF_burst_D2P_V3c_LD.jpg` | OF-F BURST | D2+ | V3's eye, 22° crop, L-D dark behind | Giant B's lens in the hole |
| `db_B_OFF_burst_D2P_V4_LB.jpg` | OF-F BURST | D2+ | far side, 45°, L-B leak | Giant B's lens in the hole; the Relay in the peek pose |
| `db_B_OFF_burst_D2P_V4b_LB.jpg` | OF-F BURST | D2+ | peek pose, hinge side, low, L-B leak | Giant B's lens in the hole; the Relay in the peek pose |
| `db_B_OFF_burst_D2P_V4w_LB.jpg` | OF-F BURST | D2+ | far side, 76°, L-B leak | Giant B's lens in the hole; the Relay in the peek pose |
| `db_B_OFF_burst_D2_V1_LB.jpg` | OF-F BURST | D2 | player at 3 m, 45°, L-B leak | lockset gone, 54 mm hole lit |
| `db_B_OFF_burst_D2_V1_LB_scale.jpg` | OF-F BURST | D2 | player at 3 m, 45°, L-B leak | lockset gone, 54 mm hole lit; 1.80 m scale figure |
| `db_B_OFF_burst_D2_V1_LC.jpg` | OF-F BURST | D2 | player at 3 m, 45°, L-C Office | lockset gone, 54 mm hole lit |
| `db_B_OFF_burst_D2_V1w_LB.jpg` | OF-F BURST | D2 | player at 3 m, 76° (game FOV), L-B leak | lockset gone, 54 mm hole lit |
| `db_B_OFF_burst_D2_V1z_LB.jpg` | OF-F BURST | D2 | loupe: V1's eye, 9°, on the lock, L-B leak | lockset gone, 54 mm hole lit |
| `db_B_OFF_burst_D2_V1z_LC.jpg` | OF-F BURST | D2 | loupe: V1's eye, 9°, on the lock, L-C Office | lockset gone, 54 mm hole lit |
| `db_B_OFF_burst_D2_V3_LB.jpg` | OF-F BURST | D2 | close-up, 0.6 m, 62°, L-B leak | lockset gone, 54 mm hole lit |
| `db_B_OFF_burst_D2_V3a_LB.jpg` | OF-F BURST | D2 | crouched at the hole, L-B leak | lockset gone, 54 mm hole lit |
| `db_B_OFF_burst_D2_V3c_LB.jpg` | OF-F BURST | D2 | V3's eye, 22° crop, L-B leak | lockset gone, 54 mm hole lit |
| `db_B_OFF_burst_FINAL_V1_LB.jpg` | OF-F BURST | FINAL | player at 3 m, 45°, L-B leak | stile torn, notch at 1.0 m |
| `db_B_OFF_burst_FINAL_V1_LC.jpg` | OF-F BURST | FINAL | player at 3 m, 45°, L-C Office | stile torn, notch at 1.0 m |
| `db_B_OFF_burst_FINAL_V1z_LB.jpg` | OF-F BURST | FINAL | loupe: V1's eye, 9°, on the lock, L-B leak | stile torn, notch at 1.0 m |
| `db_B_OFF_burst_FINAL_V1z_LC.jpg` | OF-F BURST | FINAL | loupe: V1's eye, 9°, on the lock, L-C Office | stile torn, notch at 1.0 m |
| `db_B_OFF_burst_FINAL_V3c_LB.jpg` | OF-F BURST | FINAL | V3's eye, 22° crop, L-B leak | stile torn, notch at 1.0 m |
| `db_B_OFF_burst_S0_V1_LB.jpg` | OF-F BURST | S0 | player at 3 m, 45°, L-B leak | intact |
| `db_B_OFF_burst_S0_V1_LC.jpg` | OF-F BURST | S0 | player at 3 m, 45°, L-C Office | intact |
| `db_B_OFF_burst_S0_V1z_LB.jpg` | OF-F BURST | S0 | loupe: V1's eye, 9°, on the lock, L-B leak | intact |
| `db_B_OFF_burst_S0_V1z_LC.jpg` | OF-F BURST | S0 | loupe: V1's eye, 9°, on the lock, L-C Office | intact |
| `db_B_OFF_burst_S4_V1_LB.jpg` | OF-F BURST | S4 | player at 3 m, 45°, L-B leak | leaf at 80°, lockset on the floor |
| `db_B_OFF_burst_S4_V1_LC.jpg` | OF-F BURST | S4 | player at 3 m, 45°, L-C Office | leaf at 80°, lockset on the floor |
| `db_B_OFF_burst_S4_V1z_LB.jpg` | OF-F BURST | S4 | loupe on the S floor, L-B leak | leaf at 80°, lockset on the floor |
| `db_B_OFF_burst_S4_V1z_LC.jpg` | OF-F BURST | S4 | loupe on the S floor, L-C Office | leaf at 80°, lockset on the floor |
| `db_B_OFF_burst_S4_V5_LB.jpg` | OF-F BURST | S4 | S floor afterwards, L-B leak | leaf at 80°, lockset on the floor |
| `db_B_OFF_rip_D2P_V1_LB.jpg` | OF-F RIP | D2+ | player at 3 m, 45°, L-B leak | two fingers through the hole |
| `db_B_OFF_rip_D2P_V1z_LB.jpg` | OF-F RIP | D2+ | loupe: V1's eye, 9°, on the lock, L-B leak | two fingers through the hole |
| `db_B_OFF_rip_D2P_V3_LB.jpg` | OF-F RIP | D2+ | close-up, 0.6 m, 62°, L-B leak | two fingers through the hole |
| `db_B_OFF_rip_D2P_V3a_LB.jpg` | OF-F RIP | D2+ | crouched at the hole, L-B leak | two fingers through the hole |
| `db_B_OFF_rip_D2P_V3c_LB.jpg` | OF-F RIP | D2+ | V3's eye, 22° crop, L-B leak | two fingers through the hole |
| `db_B_OFF_rip_D2_V1_LB.jpg` | OF-F RIP | D2 | player at 3 m, 45°, L-B leak | your rose pulled in, hole lit |
| `db_B_OFF_rip_D2_V1z_LB.jpg` | OF-F RIP | D2 | loupe: V1's eye, 9°, on the lock, L-B leak | your rose pulled in, hole lit |
| `db_B_OFF_rip_M_V1_LB.jpg` | OF-F RIP | M | player at 3 m, 45°, L-B leak | your lever jerked 30° down |
| `db_B_OFF_rip_M_V1z_LB.jpg` | OF-F RIP | M | loupe: V1's eye, 9°, on the lock, L-B leak | your lever jerked 30° down |
| `db_B_OFF_rip_M_V4_LB.jpg` | OF-F RIP | M | S side (the Relay's), 45°, L-B leak | your lever jerked 30° down; the Relay's glove on the S lever |
| `db_B_OFF_rip_M_V4c_LB.jpg` | OF-F RIP | M | from S, hinge side, L-B leak | your lever jerked 30° down; the Relay's glove on the S lever |
| `db_B_OFF_rip_M_V4w_LB.jpg` | OF-F RIP | M | S side (the Relay's), 76°, L-B leak | your lever jerked 30° down; the Relay's glove on the S lever |
| `db_B_OFF_rip_S0_V1_LB.jpg` | OF-F RIP | S0 | player at 3 m, 45°, L-B leak | intact, from P |
| `db_B_OFF_rip_S0_V1z_LB.jpg` | OF-F RIP | S0 | loupe: V1's eye, 9°, on the lock, L-B leak | intact, from P |
| `db_B_OFF_rip_S4_V1_LB.jpg` | OF-F RIP | S4 | player at 3 m, 45°, L-B leak | leaf at 80° (toward S) |
| `db_B_OFF_rip_S4_V1z_LB.jpg` | OF-F RIP | S4 | loupe on the notch in the open leaf, L-B leak | leaf at 80° (toward S) |

### 2.1 Verification (VL136)

The review images are logged as **VL136 · DB1 · Lock-hole stages read at 3 m** (Figma frame `2805:6338` in FRONTROOMS · VISUAL VERIFICATION LOG; verdict **FLAG**: the wood stages read at a glance; the in-play dot needs a lit far room or a dark near room, and steel is a small read). Images, in slot order:
- `images/db_B_review_stage_loupes.jpg` (VL136, `2805:6346`): every stage's loupe, OF-F BURST, L0-K BURST and OF-F RIP.
- `images/db_B_review_heroes_V3c.jpg` (VL136, `2805:6347`): the eight close-ups.
- `images/db_B_review_dot_in_play.png` (VL136, `2805:6348`): the 76° frame and 1:6 pixel crops of the hole in play, with the measured sizes.
- `images/db_B_review_it3_it4.jpg` (VL136, `2805:6349`): iteration 3 → 4, fingers, peek view, steel D1.

---

## 3. Piece counts and triangles per stage

From `$W/door_break/B/models/stats.json` (`export_models.py`; the concept assets are `$W/door_break/B/models/DB_B_<member>_<case>_<stage>.blend`, in the door root frame, Blender = (−X, −Z, Y), no set or lights). "Slab" = the leaf slab alone (the intact veneer slab is 600 tris, the steel slab 636). "Damage" = every new leaf part (veneer lip, granules, split lips, latch tube, notch splinters, crater patch, spindle stub, torn screw). "Door total" = everything still on the door and frame (it drops at D2 because the S lockset becomes debris). Debris pieces = connected islands.

| Stage asset | Slab tris | Damage parts | Damage tris | Door + frame tris | Debris objects / pieces / tris | Relay tris |
|---|---|---|---|---|---|---|
| `DB_B_OFF_burst_S0` | 600 | 0 | 0 | 26,742 | 0 / 0 / 0 | 0 |
| `DB_B_OFF_burst_D1` | 1,554 | 5 | 140 | 27,936 | 2 / 14 / 268 | 0 |
| `DB_B_OFF_burst_D2` | 2,596 | 9 | 1,536 | 16,870 | 12 / 94 / 15,368 | 0 |
| `DB_B_OFF_burst_D2P` | 2,596 | 9 | 1,536 | 16,870 | 12 / 94 / 15,368 | 15,652 (Giant B peek) |
| `DB_B_OFF_burst_FINAL` | 2,236 | 9 | 1,486 | 15,800 | 14 / 141 / 16,308 | 0 |
| `DB_B_OFF_burst_S4` | 2,236 | 9 | 1,486 | 15,800 | 19 / 137 / 16,618 | 0 |
| `DB_B_OFF_rip_M` | 600 | 0 | 0 | 26,742 | 0 / 0 / 0 | 0 |
| `DB_B_OFF_rip_D2P` | 2,596 | 9 | 1,536 | 16,870 | 4 / 82 / 1,624 | 34,200 (fingers + palm) |
| `DB_B_L0K_burst_S0` | 636 | 0 | 0 | 40,556 | 0 / 0 / 0 | 0 |
| `DB_B_L0K_burst_D1` | 636 | 1 | 17,760 | 58,316 | 1 / 5 / 104 | 0 |
| `DB_B_L0K_burst_D2` | 834 | 4 | 18,584 | 38,346 | 5 / 42 / 9,824 | 0 |
| `DB_B_L0K_burst_D2P` | 834 | 4 | 18,584 | 38,346 | 5 / 42 / 9,824 | 15,652 (Giant B peek) |
| `DB_B_L0K_burst_FINAL` | 834 | 4 | 18,584 | 38,346 | 6 / 58 / 10,112 | 0 |
| `DB_B_L0K_burst_S4` | 834 | 4 | 18,584 | 38,346 | 7 / 73 / 10,504 | 0 |

**Against the budgets (`10_design.md` §6):**
- Veneer leaf (budget ≤ 4k / ≤ 6k / ≤ 7.5k at D1 / D2 / Broken): the cut slab plus its damage parts is **1.7k at D1, 4.1k at D2, 3.7k at FINAL**. Inside.
- Steel leaf (≤ 6k / ≤ 9k / ≤ 10k): the concept crater patch alone is **17.8k–18.6k** tris (a 160 × 56 polar mesh, so the render can displace the dish and the flake edges). **Over.** For the game: a 24-segment dish of about 0.4k tris plus the crack map as an atlas decal (normal + albedo), §4 item 3.
- Live debris (≤ 40 wood, ≤ 30 steel pieces for ≤ 1.5 s): the concept floors hold **94–141** (wood) and **42–73** (steel) pieces because every crumb is its own island. **Over** as pieces; in the game the crumbs are 2–3 merged clusters inside the baked throw (≤ 30 pieces) plus the floor decal.
- Relay: the peek pose reuses Giant B (15.7k); the fingers are new (34.2k at render subdivision, about 1.5k as a game mesh).

---

## 4. How it would be built for real

Nothing here lands until Red picks. If B is picked:

1. **Stage meshes ("one model per state").** `Kit_DoorLeaf_Veneer_D1 / _D2 / _Broken` and `Kit_DoorLeaf_Steel_D1 / _D2 / _Broken` (`10_design.md` §4.1), made by the interactables modules with a `STAGE` parameter that runs the same cuts as `db_door.py` / `db_steel.py`. Rules for the kit version:
   - Cut only a **local patch** of the slab (Y 0.70–1.30, Z 0.70–0.995), so every vertex outside it stays identical to the intact leaf (same UVs, same slots): the glass plan's exact-tiling rule. The swap then changes only the patch.
   - **Drop the kit's custom normals inside the patch**, re-derive sharp edges and flat-shade the planar faces. The boolean smears the weighted normals across its long triangles (up to 14° off on a flat face), which showed as broad shading bands across the whole leaf on 2026-10-03 (fixed, §5).
   - The bore wall needs its own small UV island and a `Door_LeafCore` slot (veneer / crossband / particleboard by depth), or a 256² strip in the damage atlas.
   - The veneer lip is real 0.6 mm geometry (solidified petals), not alpha.
2. **Pre-fracture and debris.** The rose tilt, the lever droop, the knob droop and the escutcheon rock are **transforms of the separate G2 lock parts** on the blow frame. At D2 the S lock parts become **baked debris** (RE7 style, `10_design.md` §4.3): 6 baked throws of rose + lever + chassis (OF-F) or knob + cylinder (L0-K), landing 0.3–0.7 m from the latch jamb on S. The crumbs are 2–3 pre-merged clusters inside the same throw. The P trim (BURST) or the trim that goes into the Relay's glove (RIP) is hidden, not thrown. The FINAL notch is a pre-cut piece of the D2 slab that leaves with the leaf.
3. **Decals (marks).** D1's star-crazing, primer flakes and the raw split lips can be atlas quads (`Door_DamageMarks`, ≤ 4 per blow, 1 draw, no URP decal feature needed). The steel crack map here is already the right input (R crack, G primer, B bare steel, A height).
4. **Particles** (if the particle module is approved): a fibre-and-flake burst on D1, the crumb burst from the bore on D2 (particleboard 1–5 mm, falling straight), enamel chips on steel. Without the module: the bursts are extra baked pieces.
5. **Light through the hole.** The hole is real geometry, so on desktop the pooled breach spot (`10_design.md` §4.5) throws a coin of light onto the near floor and lights the bore wall, and the dust makes a short beam. The D2 → D2+ change is a colour and level change in the hole (lit-room warm → lens white). WebGL (separate track): one additive "hole card" inside the bore, its brightness = the far lamp level × the stage curve, 1 draw.
6. **The Relay rig beat.** A new `peek` / `reach` beat between blow n−1 and n (0.4 s at tier 1): BURST lowers the lens to 1.0 m against the leaf so its glow fills the hole; RIP pushes the finger mesh through (0.25 s in, the hook 0.15 s). The fingers are a separate 1-draw mesh on the hand bone, render-only. **The Relay's sight still stops at the leaf collider**: the hole is render-only, so you can see it and it cannot see you.
7. **Gameplay is untouched.** The collider (0.05 × 2.08 × 0.98 under `Door hinge {a}-{b}`) and its name stay; every new piece is render-only; after the break nothing stands in the opening except the leaf at 80° (checked in every S4 frame).

### 4.1 Costs (ESTIMATES; desktop is the reference, WebGL a separate tier)

| Item | Desktop | WebGL |
|---|---|---|
| Veneer leaf stage meshes | slab + damage ≈ 1.7k / 4.1k / 3.7k tris (D1 / D2 / Broken) | LOD1 ≤ 1.5k each |
| Steel leaf stage meshes | slab 0.6–0.8k + dish ≈ 0.4k + 4 small parts ≈ 0.8k; crazing in the atlas | ≤ 1.2k |
| Lock debris throws | rose + lever + chassis as built ≈ 7.5k (lever 3,974, rose 2,728); a floor LOD of about 1.5k is enough | 1 combined mesh ≤ 600 |
| Crumbs / flakes | ≤ 30 pieces in the throw, then 1 floor mesh ≤ 3k tris | ≤ 12 pieces |
| Fingers | ≈ 1.5k tris, 1 draw, only between blow n−1 and n | the same mesh, 1 subdivision less |
| Breach light | 1 shadowed spot for ≤ 3 s per break (as the design) | none: the hole card |
| Rig | 2 new poses (peek, reach) + the lever grab for RIP M | same |
| Memory | ≤ 1 MB of stage meshes per door type; crack map baked into the shared marks atlas | half |

---

## 5. Iterations (what I looked at and fixed)

Every frame was opened and judged. Quick passes are 960 × 540 at 48 samples; the review sheets are in `$W/door_break/B/out/`.

| Pass | Found | Fix |
|---|---|---|
| 2026-10-03, it. 1 (28 frames) | **Shading bands** across the cut veneer leaf at Y ≈ 0.85 and 1.20 m. Traced: albedo clean, diffuse-only still banded, so normals: the kit's custom normals smeared by the boolean (up to 13.8° on a flat face) | Custom normals cleared in the cut, sharp edges re-derived, planar faces flat: 0.02° worst |
| | D1 splits invisible at 3 m | Splits 1.6 → 2.4 mm wide, raw lips 1.4 → 2.6 mm |
| | Oak read peach under AgX + the Office grade | Blue pulled to 0.84 in the oak tint (golden oak, closer to the IP's door) |
| | The S4 closer forearm floated outside the frame | Sign error in the elbow point; arm now 30° off the open leaf, forearm hanging 70° |
| | The steel coin hole read as a **chrome dome** and the crazing as a **dartboard** | Dish 3.4 → 1.6 mm with a flared rim; dull bare steel; enamel flakes in jagged pieces near the hole and small chips along the radials |
| | The fingers looked like two smooth tubes | Rebuilt: squeezed in the bore, swollen past it, knuckle, crease, welt, matte canvas |
| 2026-10-03, it. 2–3 | The Office grade missing on Office cameras outside L-C; steel D1 hairlines (0.22 mm) unreadable at 3 m; no frame saw through the bore | The grade follows the camera; D1 cracks 0.45 mm and the escutcheon rock 4.5 → 7°; new views V3a (crouched at the hole) and V4b (peek) |
| 2026-10-07 16:4x, recovery re-check (55 quick frames, replayed scripts) | The coverall still pale sage; the lit bore wall read as a **white ball** in the hole; the glove tint too black (no knuckle form) and its sheen drew a white rubbery rim; the palm blocked the light round the fingers; the breach bulb itself showed through the hole and hid the D2 → D2+ change; steel D1 still weak in the loupe; V4b hid the hood behind the shoulder | Coverall sheen 0.12 → 0.04; bore wall dark zinc with 1.5 mm threads (the light now shows as a lit far ring: a tunnel); glove tint ×1.9 → ×3.0 and sheen 0.7 → 0.3 grey; palm lowered and set back so crescents of light outline the fingers; breach bulb hidden from the camera; steel D1 rock 7 → 10°, cylinder out 5 → 8 mm, cracks 0.62 mm, chips 1.2–3.2 mm; V4b moved low to the hinge side, V4c added (it. 4) |
| 2026-10-07 17:22–17:35, it. 4 check (14 quick frames) | Hole brightness in the loupes: OF-F D2 → D2+ goes from warm (155/140/116) to lens white (166/161/154); steel D2 → D2+ 160/146/121 → 140/133/122 (whiter, less warm). Fingers outlined; steel D1 reads in the loupe | — (finals started 17:36) |
| 2026-10-07 18:1x, it. 5 (this run) | No close-up showed D1 or FINAL; §8.0's 1.80 m scale figure was missing; the loupe list had no size, and 16:9 loupes would be squeezed into the square strip insets | Added heroes `OFF_burst_D1_V3c_LC`, `OFF_burst_FINAL_V3c_LB`, `L0K_burst_D1_V3c_LB`; added the 1.80 m figure frame; loupes rendered square (720 × 720) |
| Finals 17:36–19:5x | Every final was opened: the four strips and the 23 loupes as sheets, the 17 close-ups (V3, V3c, V3a) at full size or as sheets (crops at 1:1 for the fingers and the steel hole), the far-side, V5 and V1w frames as sheets. Kept as rendered. One change of pick: the steel lens hero (`dir_B_STEEL-D2P`) is the crouched V3a, because V3c (35° off the hole's axis) sees only the bore wall, which is dark under L-D. In-play numbers measured on the finals (`scripts/measure.py`): OF-F D2 hole 12 × 12 px, the frame's brightest pixels, 16.5× the door round it; L0-K lit core 4 × 6 px at half max (7.2 px by geometry); the D2+ lens core under L-D 8 × 9 px, 3.8× the lit door | |

---

## 6. Open issues

1. **The dot needs the dark.** Under L-C (both rooms lit) the D2 hole is a pale disc; D2+ (the lens) is only a little whiter than D2. The read depends on the lamp look (`10_design.md` §4.5): the far room must be brighter than the near one, or the lens must be the only light (L-D).
2. **Steel is a small read.** 7 px in play. From the head-dip view (V3, about 35° off the hole's axis) you cannot see through a 30 mm hole in a 44 mm leaf (it blocks any view more than about 34° off axis): you see its lit walls. The punched mortise cylinder is an ESTIMATE of how that lock fails; photos of a pulled / punched cylinder and of a knob torn off its spindle are wanted (`media_candidates.md` §20, `db13`, `db14`).
3. **Steel FINAL adds little** on its own (jolt + chips). Pair it with Direction A's lock-edge crush and bent bolts for the last blow.
4. **Lens distance** (§1.4): 0.11 m, not 0.25 m. Red decides which part of the brief wins (hood on the leaf, or the lens 0.25 m back with a visible gap).
5. **W-W (L0-F)** was not rendered. Real wood-frame doors fail at the jamb first; on L0-F the lock hole is a staging choice (`10_design.md` §8.2 Risks).
6. **The oak tint** (blue × 0.84) is a render choice; the game's `Prop_WoodOak` material is unchanged.
7. **The Relay rig beat** (peek / reach) and the RIP lever grab are new work for the visual chat (Relay rig); the finger mesh is a new asset. The peek pose was posed by hand on Giant B's rig in Blender: it needs the real rig's limits.
8. **Concept counts are over the game budget** for the steel crater patch and the crumb pieces (§3); the game versions are specified in §4.
9. **Lead references are pending Red's approval:** The Shining stills `db01a–e` (1:30 blade, 1:48 grain split, 1:52 see-through hole, 2:05 face, 2:08 hand to the lock) and the L4D2 lit hole `db02` are listed in `media_candidates.md` (rows 51–52). On disk and usable now: `../interactables/proposal/media/kane_emg_0125_door_six_bolts.jpg` (the IP's locked door) and `a24_trailer_0139_dark_door_level0.jpg` (a dark door in Level 0: the dark-hole read), credited in that folder's `SOURCES.md`.
10. **Era check:** every part is the 1990 kit (bored lever lockset, mortise lock with IC cylinder, surface closer, door sign, enamel number plate). No brands, no maker marks, nothing printed after 1990. The 1.80 m scale figure is a plain grey mannequin.
11. **Main moved** (Codex 2026-10-03, then about 30 commits since d610d3a). This direction writes nothing into the game, so nothing was merged. It imports the kit modules and the creature module read-only from main; both are unchanged since 2026-10-03 / 10-02. `codex_audit/20_findings.md` does not exist; `00_main_state.md` and the `10_review_*.md` files list no finding for door-break.

---

## 7. Reproduce

```
B=$W/door_break/B ; BL=/Applications/Blender.app/Contents/MacOS/Blender
$BL -b --factory-startup --python $B/scripts/build_kit.py                 # intact kit -> kit_blend/
$BL -b --factory-startup --python $B/scripts/build_giant.py -- peek lever  # creature/Giant_B_peek|lever.blend
$BL -b --factory-startup --python $B/scripts/render_shot.py -- OFF_burst_D2_V3c_LB [--quick] [--res WxH] [--person --tag scale]
$B/scripts/runner.sh $B/scripts/final_main.txt r1      # shared-queue finals (several runners can share one list)
$B/scripts/runner.sh $B/scripts/final_loupes.txt r3 --res 720x720
$B/scripts/runner.sh $B/scripts/final_extra.txt r4
$BL -b --factory-startup --python $B/scripts/export_models.py             # models/*.blend + stats.json
/Applications/Blender.app/Contents/Resources/4.3/python/bin/python3.11 $B/scripts/publish.py   # JPGs, strips, dir_B_*
```

Scripts: `db_lib.py` (set, materials, lights, camera, render), `db_door.py` (door assembly, OF-F stages, closer), `db_steel.py` (L0-K stages, crack map), `render_shot.py` (shots, debris, fingers, the Relay, lights, scale figure), `compose.py` (JPGs, strips with loupes), `publish.py` (this folder's images), `export_models.py` (concept assets + stats).
