# 50: Door break, rethought. The whole animation, the model and texture structure, and one prototype video (2026-10-30)

Owner: 游戏视觉 (game visuals), account A, session local_5656cffd. Status: DESIGN for Red. **Nothing in this file lands in main.** The prototype it scopes is built in a clone, and Red gets one MP4. Red confirms the look before anything lands; that rule has stood since 10-03.

Red's ask, 2026-10-30, verbatim:
- "目前在房门被破坏的这个部分，我希望你能够在现有的这个基础上思考整套房门被破坏的动画是什么（我需要你research 可破坏地形一般都是怎么做模型结构和贴图结构，然后优化当前我这个游戏的这部分。）"
- "在你准备好research和优化之后，给我节省token的方式去录制视频给我看你做的效果"

Inputs:
- Four research lenses written today: model, texture, reference and current. Their sources are in `SOURCES.md` under "Research 2026-10-30 (...)", and the media they want is in `media_candidates.md`.
- Earlier door-break work: `01`, `02`, `10_design.md`, `20_dir_A/B/C.md`, `21_C_diagnosis.md`, `22_C_rework.md`, and `NOTE_1030_db1_handover_READ_FIRST.md`.
- The D1.6 kit doors and the GD3c glass design (`research/glass/destruction/gd3c/10_design.md`).

Frames used below: **door root** = Unity metres, origin on the pivot axis at the floor. +Y is up. +Z runs along the opening, with the latch end at Z 0.995. ±X are the two faces. The **burst face** is the face the leaf breaks toward (the player's room, A12). The **push face** is the Relay's side.

---

## 0. Short answer

1. **Today a break shows no damage.** Each blow jolts the leaf 4 to 8 mm. On the last blow the leaf is thrown to 105°, rests at 80° tilted 3°, and the strike drops. DB1's renders tried three directions (A, B, C2) on the D1.6 kit, and none of them reads as "staged, like the glass" from the player's 3 m. Their stage details are 5 to 54 mm, which is 1 to 12 px on screen. **New rule: every stage must change something at least 50 mm across (12 px at 3 m), and light must show it.**
2. **What real doors do sets the story.** Our doors are double-acting doors on pivots, with no stop, and the latch side is the part that gives [RF16 to RF20]. A solid-core veneer leaf does not hole in its middle. It **splits along the grain at the lock**: first a hairline, then a lit slot, then the latch block tears out, and the free leaf swings through and tears a pivot. A steel leaf **dents, bows and opens its edge seam**, and stays whole.
3. **The beat.** One discrete change per blow, on the blow frame, under the blow sound. Nothing grows between blows. Blow 1 is elastic: a rock and dust, but no mark (The Haunting [RF1]). The stages count back from the last blow, so every tier ends the same way. With 5 blows the order is **E · M · D1 · D2 · FINAL**, then the break.
4. **The final pose looks stuck, not open.** Recommended pose (a): the top pivot gives, and the leaf **leans about 110 mm out of plane on its bottom pivot**, with a dark gap at the head. The latch end shows a torn **90 × 660 mm notch** (that is 21 × 152 px at 3 m). This matches HUD B·4 ("hangs off its hinge"), so the HUD needs no change.
5. **Model structure, as shipped games do it** (Left 4 Dead, Amnesia, RE7, Siege, Control, THE FINALS):
   - Everything is pre-fractured offline, in our Blender kit generator. Nothing is cut at runtime.
   - The **intact leaf asset stays byte-identical**. The first damaging blow swaps it for a `body` with a window cut out at the lock, plus a **damage cassette** (395 × 1200 mm) that has one mesh per stage.
   - The pieces are split by material: veneer, crossband, particleboard core, hardwood stile and lock block.
   - Splinters are real geometry fixed to the cut edges, not alpha cards.
6. **Texture structure:**
   - Cut faces get their own material slot, `Door_CoreWood`, mapped by depth across the 44 mm thickness: veneer, glue, crossband, particleboard, crossband, veneer. The hardwood stile gets `Door_CoreStile`.
   - Both use our existing FrontRoomsSurface shader with metre mesh UVs, so the prototype needs no shader change.
   - A `_FR_DAMAGE` onset mask (Frostbite 2 style, UV-free, in leaf object space) comes in production. It carries push-face bruising, close hairlines and, for steel, enamel crazing.
   - No URP decals: there is no Decal feature, a projector on a swinging leaf would also hit the jamb, and WebGL only has screen-space decals.
7. **Runtime:**
   - Stages are discrete swaps, hidden by the jolt, a 2 to 3 frame hit-stop on the leaf, the camera shake, chips and dust, and a light pulse.
   - Debris is a **baked throw** (RE7 style). It is deterministic and cheap, and its rest poses are known, so persistence costs a 16-byte record per door, built on GD3c's record idea. A knock is never undone.
   - Phones get the LOD1 cassettes and capped debris. WebGL does not dress doors at all today, so it is unchanged.
8. **This workflow builds one prototype:** the veneer leaf (L0-F), the full T1 sequence, a recommended final pose plus the alternative. It renders deterministically from a Play-mode harness in a clone and is encoded to **one silent MP4 of about 52 s at 1080p60**, with the beats burned in as captions. To save tokens, the agent checks numbers plus one contact sheet, never single frames (§4.6).
9. **Production after Red approves takes about 11 working days** (§5). Hooks from 关卡设计 (level design) and 声音设计 (sound design) are listed as asks in §3.10. None of their files is touched.
10. **Six questions for Red** are in §6. The most important are the final pose (a) or (b), and whether a half-broken door stays weakened for gameplay.

---

## 1. Background

### 1.1 What Red asked, in order
- **10-03:** doors should break in stages, like the glass, and Red sees the visual plan before it lands (`10_design.md`).
- **10-07 and 10-08:** three directions were pre-rendered.
  - A "Jamb split", B "Lock hole" and C "Too big for the door".
  - C read fake. `21_C_diagnosis.md` explains why: a glass crack graph on wood (F1), cracks painted as dark ink (F2), and holes as flat black blobs (F4).
- **10-29 and 10-30:** DB1 rendered A, B and C2 again on the landed D1.6 kit (52 PNGs, `images/db1_*.jpg`).
  - It was stopped before Red was asked to pick, because Red widened the task to this rethink.

### 1.2 What the game does today (current lens; line numbers are main's 10-30 tree)

| Step | Code | What the player sees |
|---|---|---|
| Break starts | `FrontRoomsMapHunter.cs` L1240-1250: `SwingAwayFrom`, `MarkBeingBroken`, `blowsStruck = 0` | Nothing |
| Each blow | `TickBreak` L1630-1669: `DoorBlow(position)`, `SwingAwayFrom`, `JoltForBlow(door, i, n)` | The leaf jolts 4 to 8 mm and 0.6 to 1.2° on a 0.12 s spring. The camera shakes 0.2 to 0.6° within 8 m. The Relay rig makes a chest snap |
| Break | `FrontRoomsMapWorld.BreakDoor` L3057-3079 | The leaf is thrown 0 → 105° in 0.12 s and bounces to 80° in 0.18 s. It tilts 3° in its own plane about the top of the hinge edge (butt-hinge physics, not pivot physics). `DetachStrike` drops the strike to the floor |
| After | `brokenDoors`, rebuilt with a cube leaf at 80° / 3°, then `DressDoor` | The same pose, forever. `Use` returns `Broken`, and HUD B·4 shows "BROKEN · WON'T SHUT" |
| Relay leaves mid-break | `StopBreaking` L1623-1627 | The damage is gone. The next break starts again from blow 0 |

Blow clocks: one blow every 0.5 s, and the last one 0.1 s before the break.

| Tier (v1, live) | T | Blows at (s) | v2 (`FRONTROOMS_RELAY_V2`) |
|---|---|---|---|
| T1 | 2.5 | 0.5, 1.0, 1.5, 2.0, 2.4 | 2.5 s, same |
| T2 | 2.2 | 0.5, 1.0, 1.5, 2.1 | 2.3 s: 0.5, 1.0, 1.5, 2.0, **2.2** (the last two are 0.2 s apart) |
| T3 | 1.9 | 0.5, 1.0, 1.5, 1.8 | 2.1 s: 0.5, 1.0, 1.5, 2.0 |
| T4 | 1.6 | 0.5, 1.0, 1.5 | 1.9 s: 0.5, 1.0, 1.5, 1.8 |
| T5 | 1.3 | 0.5, 1.0, 1.2 | 1.9 s: the same as T4 |

### 1.3 The facts about the D1.6 leaf that a break must work with
- `Kit_DoorLeaf_Veneer_DA`:
  - a 44 mm leaf, LOD0/1/2 = 2,736 / 1,068 / 148 triangles, switching at 4 m and 12 m;
  - two slots, `Door_Veneer` and `Prop_Brass`;
  - a lofted slab of a few large quads, with vertex-colour wear rings.
- **It is a hollow shell.** It has no core, edge band or veneer layer, and no damage UV set (handover note). Breaking it needs authored cut faces.
- **Free hooks:** the anchors `damage_latch` (0, 1.10, 0.995) and `head_dust_a/b` (0, 2.098, 0.05 / 0.95). They were authored for this work (`interact_door_da_common.py` L237, L363-364), and no code reads them.
- **Lockset:** centre Y 1.000, Z 0.920 (75 mm backset). The latch is at Z 0.995, the strike at Z 0.998. Brush fins seal the latch gap.
- **Rig:** `FrontRoomsDoorRig` copies the map collider's offset onto the kit leaf (collider follow) and owns `DetachStrike` and `BeatWobble`. `DoorBlow` carries a position only, not the door.
- **WebGL:** `DressOnWebGL = false` (Door.cs L54), so WebGL keeps cube doors. Handheld devices are dressed (`DressOnHandheld = true`).

### 1.4 The lesson from A, B, C and DB1
DB1's overview at 3 m (`images/db1_overview.jpg`):
- A's M, D1 and D2 look like S0. Its stages were jamb details 5 to 20 mm wide.
- B reads only from D2, through a 54 mm hole, which is 3 px at 12 m.
- C2 reads, but through the wall rather than the door, and it never passed review.

The common fault is **scale**. Each stage was designed at the scale of a close-up, while the player sees the door from 2 to 6 m (230 px/m at 3 m, 115 px/m at 6 m, from the 76° vertical FOV at 1080 px). Hairline cracks of 0.3 to 0.8 mm are under one pixel even at 1 m.

This design keeps what was right in those passes:
- A: the latch side fails;
- B: through-light at the bore, a rose and lever that loosen, and the cassette build plan in `20_dir_B.md` §4;
- 21_C: wood breaks along the grain, cracks are never ink, and light is never faked with black.

It moves the stage changes up to **leaf scale**.

---

## 2. Research summary (sources lead; diagrams only support)

Tags are those in `SOURCES.md`. [read] means the text was read; footage timestamps were checked in `01` or by the reference lens.

### 2.1 How our real doors fail
- **Hardware: double-acting doors are centre-hung on pivots.**
  - The bottom pivot carries the whole leaf, and the top pivot only keeps it aligned. Locksmith Ledger, "More to Pivots Than Meets the Eye", 2006 [RF16, read].
  - They latch into a double-lipped strike with no stop [RF17][RF19].
  - A bevelled latch "can be pushed right through" such a strike [RF18].
  - Battering is aimed "adjacent to the door lock assembly, or adjacent to the center hinge" (US 4,832,388) [RF20].
  - So the latch side gives, the leaf swings through with nothing to stop it, and the hardware that tears is a pivot.
- **A solid particleboard-core veneer door is five-ply.**
  - The build-up is face veneer (about 0.6 mm), crossband (about 1.6 mm, there "to eliminate telegraphing"), and a particleboard core with hardwood stiles and rails and wood lock blocks [S20][RF23][RF24][RF25][M-WD1][T-WD].
  - It breaks at the lock: a granular core, long fibrous stile splinters, and veneer split along the grain (`02` §2; S33; [T-WF]).
  - It does not hole in the middle. Only hollow-core doors do [S22].
- **A hollow-metal door** has 18 ga skins (0.042 in) over kraft honeycomb [RF26][S24][S25][T-HM]. It dents permanently, its enamel crazes down to primer, and its edge seam opens to brown honeycomb. It never splinters (`02` §2.3).
- **Failure order at the latch** (`02` §2 adapted to pivots by the reference lens, lengths are ESTIMATES):
  1. an elastic rock;
  2. hairline splits at the bore;
  3. the stile splits 150 to 400 mm above and below the bore;
  4. the latch block breaks out;
  5. the leaf swings through and a pivot tears.
- **How long it lasts:** at Relay-scale energy a real 1990 wood office door gives on blow 1 or 2, and a hollow-metal one on blow 2 to 4 (`02` §6; S18). Five blows is a game choice, defended by the horror countdown below.

### 2.2 How film and games stage it
- **The Shining (1980), bathroom door** [S4], https://www.youtube.com/watch?v=WDpipB4yehk, 1:16-2:10:
  - first blows from outside;
  - splits along the grain in the lock-side panel by 1:24;
  - the blade bursts through at 1:30;
  - long strips and a slit of light at 1:48;
  - a see-through hole with splinters bent toward Wendy at 1:50-1:52;
  - the hand reaches for the lock at 2:08.
  - About 8 to 10 blows over about 50 s. Each blow adds damage, and nothing changes between blows.
- **The Haunting (1963):** a laminated door bulges inward with no damage at all; a prop man pushed it from behind [RF1, read][RF2]. That is our blow-1 elastic beat.
- **Left 4 Dead:** an intact door plus three damage models, then gibs. Torn holes show the lit corridor through a dark door [V1][M-L4D]; footage at https://www.youtube.com/watch?v=UHE_5DaZXk4, 0:35-0:54.
  - L4D walls hide a pre-made broken-edge model and reveal it, so a hole never reads as a straight cut [V2].
- **Amnesia:** an intact mesh, `DamageMesh1/2` at health thresholds, then a pre-broken entity thrown along centre-to-piece vectors **plus the door's own velocity** [F1][M-AM].
  - Damage lands on the animation's impact frame [F2].
  - Amnesia: The Bunker clip: https://www.youtube.com/watch?v=adCV8RXTWdo, 0:19-0:30 [F4].
- **Resident Evil 7:**
  - Jack's wall entrance is a one-frame burst, and the hole stays: https://www.youtube.com/watch?v=5xEJMY0EcFE, 0:09-0:24.
  - Capcom fractured offline in Maya with wood-splinter settings, picked the best of many simulations, baked them, and unwrapped cut faces by script; the broken mesh has about 3× the intact polygons [C1][M-RE7].
- **The door is a countdown read by ear.**
  - Outlast players run "when you hear Chris Walker start banging on a door" [RF9].
  - Alien: Isolation's locked door is a timed shelter of about 15 to 20 s [RF13].
  - Dead by Daylight's breakable wall takes 2.34 s and has its own Alert cue [RF5, read].
- **The end state must look wrong.** RE4 (2023) and Friday the 13th leave a broken door that stays broken and becomes an exit [RF11][RF12][RF14].

### 2.3 Model structure in shipped games and engines
- **Pre-fracture is the norm, and runtime cutting the expensive exception.**
  - Rainbow Six Siege cuts only planar layers at runtime: 2D polygon clipping, then extrusion. One wall of 2 drywall + 2 wood layers costs 8.1 ms on PC, and its studs and debris are pre-fragmented (L'Heureux, GDC 2016, slides ~35-40, ~56, ~72) [M-R6]: https://media.gdcvault.com/gdc2016/Presentations/LHeureux_Julien_Art_Of_Destruction.pdf
  - THE FINALS pre-fractures in Houdini "with seams carefully hidden until the asset is damaged", from watertight inputs, and merges door frames into the building mesh [M-FN1]: https://www.sidefx.com/community/making-the-procedural-buildings-of-the-finals-using-houdini/
  - Red Faction: Armageddon hides "invisible shards" until destruction and sets shard size per material [M-RFG1].
  - RayFire for Unity pre-fractures on every platform and swaps in a prepared prefab ("Reference Demolition") [M-RF1].
- **Split by material first.**
  - "Objects are separated into different parts based on their physical material" (Siege slide ~29) [M-R6].
  - Control tags material on the smallest unit, in nested groups of pre-joined pieces (Remedy, GDC 2020) [M-CT1][M-CT2]: https://www.gdcvault.com/play/1026820/Destructible-Environments-in-Control-Lessons
  - Houdini's wood fracture cuts along the grain in two directions, plus cut planes across it, with separate glue strengths along and across the grain; that is what makes wood split long [M-HO1]: https://www.sidefx.com/docs/houdini/destruction/wood.html
- **Hierarchy and connections, not free physics.**
  - Unreal Chaos: one level per fracture pass, a connection graph with strain per link, and a damage threshold per cluster level [M-UE1-3]: https://dev.epicgames.com/documentation/en-us/unreal-engine/destruction-overview
  - Siege: a hierarchical decomposition plus a connection-based leaf graph [M-R6].
  - Teardown: what falls is a connectivity question, decided per voxel volume [M-TD1].
- **For doors:** one damage model per stage is proven (L4D 3, Amnesia 2). Swapping only a **local damage zone** is cheaper than swapping the whole leaf. It is the same idea as L4D's hidden broken edge and Red Faction's hidden shards, and the same plan as `20_dir_B.md` §4.

### 2.4 Texture structure
- **Cut faces get their own material.**
  - Unreal duplicates material slots for internal faces and has an `InternalMaterialID` [T-UE1][T-UE2].
  - Blender Cell Fracture has a material index for interior faces [T-BL]. RayFire has an inner material with a mapping scale [T-RF].
  - Unreal's fracture auto-UV bakes `DistanceToExternal` for interior faces, so an interior shader knows depth [T-UE3]: https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Plugins/PlanarCut/UE__PlanarCut__EBakeAttributes
  - Apex sets the interior UV scale in geometric distance per texture unit, the same idea as our metre UVs [T-UE4].
- **Masks on the intact surface.**
  - Frostbite 1 needed a hand-made UV mask per destructible part, "time consuming". Frostbite 2 moved to designer spheres → a distance field, with `saturate(d*mult + offset)` and detail noise for a ragged growing edge, at 0.20 to 0.77 ms per scene on PS3 (Kihl, SIGGRAPH 2010, slides 7-18) [T-FB]: https://www.slideshare.net/DICEStudio/siggraph10-arrdestruction-maskinginfrostbite2
- **One motif drives both the cut and the texture.** Siege's "texture cutter" builds the cut outline from a tileable motif authored in UV space (slide 42) [T-R6].
- **Decorations, not decals, on broken edges.** Siege compared GPU decals with real-geometry "decorations". Feature-bound decorations sit on edges and vertices, protrude, and vanish with their feature (pages 45-48) [T-R6]. Control used decals only on surfaces that could not really break [T-CT].
- **URP limits here.**
  - DBuffer decals need a DepthNormals prepass and do not run on GL backends [T-UN1][T-UN3]: https://docs.unity.com/en-us/engine/6000.6/manual/visual-effects/decals/renderer-feature-decal/reference
  - Our FrontRoomsSurface has no DBuffer keywords [T-UN2].
  - A MaterialPropertyBlock drops a renderer out of the SRP Batcher [T-UN4].

### 2.5 Runtime and budgets
- **Control** uses three tiers: rigid bodies for big debris; particles, rigid hierarchies and decals for small debris; flipbooks for dust. At most 200 active rigid bodies [M-CT2].
- **Siege:**
  - debris is "well-placed replacements", instanced and recycled [M-R6];
  - it replicates events, not mesh states, and rollback would need a full surface backup per step (pages 76, 87) [T-R6].
- **RE7** bakes its hero breaks and caps live breaks at about 50 pieces [C1].
- **Our own GD3c glass** (landed 10-29) already has three useful parts:
  - a staged record per edge, plus a restore on rebuild ("a knock is never undone");
  - a debris world in a local physics scene, with a reduced tier for WebGL and phones;
  - mesh particles, because main has no Particle System module.

### 2.6 Media still wanted (not downloaded)
`media_candidates.md` lists them under "Research 2026-10-30 (model / reference / texture)". Priority 1 is real photos of forced flush doors at the lock stile, and of hollow-metal doors with opened edge seams. Red fetches them by hand.

---

## 3. The design

### 3.1 Rules
1. **One change per blow, on the blow frame.** Nothing grows between blows [F2][S4][GP].
2. **Damage only adds, and is never undone.** That holds across stages, across chunk rebuilds, and when the Relay gives up halfway (the GD3c rule).
3. **The 3 m rule.** From D1 on, every stage changes something at least 50 mm across (12 px at 3 m, 6 px at 6 m), lit or silhouetted. Sub-pixel detail (hairlines, the 0.6 mm veneer) is a close-range bonus, never the read.
4. **The damage sits where real doors fail:** the latch side and the lock block, plus the Relay's contact on the push face. The leaf stays whole elsewhere.
5. **Light is the read.** A slot of the far room's light through the split, a crescent at the bore, then the flood at the break. Cracks are never drawn as ink (21_C F2).
6. **Intact doors cost nothing.** The intact leaf asset, its material and its draw stay byte-identical. Damage assets load only on the first damaging blow.
7. **Render-only.** The map owns gameplay (colliders, timings, the throw). The visual never feeds back into it.
8. **Desktop is the reference tier.** Phones get a reduced tier, gated at runtime, and desktop output does not change by one byte. WebGL is its own track (`webgl-separate-track`).

### 3.2 The stage clock: count back from the last blow
Stage of blow `i` (0-based) of `n`: `stage(i) = 4 - (n - 1 - i)`, floored at 0.

| Stage | Name | Meaning |
|---|---|---|
| 0 | **E** (elastic) | rock, dust, no mark |
| 1 | **M** (marks) | close-range damage, plus a rose tip |
| 2 | **D1** | split |
| 3 | **D2** | lit slot |
| 4 | **FINAL** | the latch tears out; the break follows 0.1 s later |

| Blows | Order | Tiers |
|---|---|---|
| 5 | E, M, D1, D2, FINAL | v1 T1; v2 T1, T2 |
| 4 | M, D1, D2, FINAL | v1 T2, T3; v2 T3, T4, T5 |
| 3 | D1, D2, FINAL | v1 T4, T5 |

- Fewer blows compress the story from the front. D1, D2 and FINAL always play.
- **Kept damage:** the stage shown is `max(kept stage, stage(i))`. A door the Relay half broke and left still shows its split when the Relay returns, even if gameplay restarts its blow count (Q2).
- Every blow, whatever its stage, also plays the elastic rock (§3.9).
- The unread `DoorBreak.Damage1At / Damage2At / RevealHold` fractions are replaced by this blow-index rule (ask M5).

### 3.3 Veneer leaf (Veneer_DA; Veneer_Oak and Ward by slot swap): blow by blow, T1

| Blow (s) | Stage | Burst face (the player's side) | Push face (the Relay's side) | Light on the burst side | What reads at 3 m | Sound beat (ask S1) |
|---|---|---|---|---|---|---|
| 1 (0.5) | E | The leaf rocks toward the player: the latch end goes 6 mm out of plane and back in 0.12 s. Dust sifts from `head_dust_a/b`. No mark | The Relay's forearm lands at Y about 1.3, 0.2 m from the latch end. The leaf gives away from it | A 2-frame hairline of light past the latch fin, if the far room is lit | the rock and the dust | boom (Stage 0) |
| 2 (1.0) | M | The burst-face rose tips 3°. A 300 mm hairline along the grain at the bore, with a veneer lip lifted 0.4 mm (close range only). Rock 9 mm | A contact dish 2 mm deep, Ø 140 mm (reads in grazing light) | a pin of light at the bore edge for 1 frame | the rose tip, the rock, the dust | + creak and crackle |
| 3 (1.5) | D1 | **The stile splits through**, along the grain, from Y 0.78 to 1.24. The latch strip (Z 0.905 to 0.995) stands 4 mm proud. A veneer lip lifts 2 mm, with 3 short splinters. The lever droops 6° | The dish is 4 mm deep, with crushed-veneer wrinkles. The push-face rose is pushed in 3 mm | a line of light 3 to 5 mm wide along the split near the bore; leak card 15 % | a 460 mm split (106 px) with a lit line | + crack (the split) |
| 4 (2.0) | D2 | The split runs Y 0.62 to 1.42 and **opens 12 to 20 mm at the bore**. The strip is pushed out 15 to 25 mm. A **veneer petal** (110 × 220 mm, veneer and crossband) peels out 40°, showing the particleboard. A splinter fringe stands on both lips, bent toward the player. A branch crack runs down the edge band. The lever droops 12° | The rose is crooked 8° and the dish is 6 mm deep. The back edge of the strip stands proud | **a lit slot** about 800 × 15 mm, plus a crescent of light at the bore (the rose shifted 8 mm); leak card 40 % | the slot (184 px tall), the petal (28 × 46 px), the proud strip | + splintering tear |
| 5 (2.4) | FINAL | **The latch block tears out.** The strip (90 × 660 × 44 mm), with the faceplate and the latchbolt, breaks free toward the player with about 14 splinters and 16 crumbs. The petal stays, hanging by its fibres. The lockset stays in the leaf, its levers dangling 25 to 35° | The leaf lurches away from the Relay, and its arm follows through | the notch opens to the far room; the breach light flares (desktop) | the strip and splinters in flight, the notch | + tear-out (latch block, screws) |
| break (2.5) | BROKEN | The map throws the leaf toward the player. It hits its stop, bounces to 80°, and pose (a) settles by 2.8 s. The strike falls (existing `DetachStrike`). Debris lands within 1.6 s, and the dust hangs for 2 s | The doorway opens behind the leaf. The Relay is back-lit in it, seen from the burst side | full opening; the breach light fades over 1 s | everything | Break, StopLimit, DebrisImpact series |

**What the player sees from the far side** (rare: the player is behind the Relay):
- the Relay's back and its blows;
- the push face's dish and the crooked rose;
- from D2, the strip's back edge standing proud;
- at FINAL, the leaf leaving them, with the notch flashing past as the leaf turns.

**Hit-stop:** at D1, D2 and FINAL the visual leaf holds its jolt peak for 2, 2 and 3 frames. The camera never stops (`10_design.md` §5).

### 3.4 Final state and pose (HUD B·4)
- **The broken leaf shows:**
  - the torn latch notch, Z 0.905 to 0.995 × Y 0.72 to 1.38, with ragged stepped split faces, crumbled core and splinter teeth at its top and bottom;
  - the half-open bore with the lockset chassis inside;
  - the dangling levers and the hanging petal.
- **On the floor (burst side):**
  - the latch strip, 0.3 to 0.8 m from the latch jamb;
  - splinters and crumbs in a fan along the leaf's path, within 1.5 m;
  - the strike, as today.
- **Pose (a), recommended (physics, [RF16]):**
  - The top pivot gives. The leaf keeps the map's 80° rest and **leans 3° out of plane about the bottom pivot** (0, 0.015, 0.036), so its top moves about 110 mm (25 px at 3 m) in the throw direction.
  - It also sags 0.6° in plane, so the bottom of the latch end rests on the carpet.
  - The leaf's top pivot plate is bent up 6 mm. A dark gap of 25 to 35 mm opens at the head between the hinge stile and the soffit cap.
- **Pose (b), the alternative (DB1 r3):**
  - The bottom arm tears off the leaf foot. The leaf hangs from the top pivot, turned 0.8° in plane with the latch corner on the carpet, and twisted 1.5° out of plane.
  - It reads less at 3 m, because a 0.8° in-plane turn is a 13 mm change at the head.
- Both poses need the map's `PoseLeaf` changed so the collider agrees (ask M3). The prototype films (a), with (b) as a short alternative.

### 3.5 Steel leaf (Steel_DA, SteelLite_DA): blow by blow (design only; the prototype does not build it)

| Stage | Burst face | Push face | Light | What reads at 3 m | Sound |
|---|---|---|---|---|---|
| E | a rock, a hollow boom; dust from the head | a shallow dish 1 mm deep (onset mask) | none | the rock | boom with a metallic ring |
| M | nothing new (an 18 ga skin hides it) | a dish 3 mm deep, Ø 200 mm, at the contact, with a star of crazed enamel (onset mask) | none | the dish, in reflections | + ring |
| D1 | **the leaf bows 8 mm around the lock** (cassette); the strike lips bend | the dish is 6 mm deep, with crazing rings and chips; primer shows at the tightest bend | the latch gap opens 4 mm | the bow in the reflections, the bent lips | + buckle |
| D2 | the lock edge crushes; **the edge seam opens 100 to 300 mm**, showing brown kraft honeycomb; the bow reaches 15 mm | the dish is 10 mm deep, with bare steel at its rim | the latch gap is 8 to 10 mm, with a lit line | the opened seam (brown, 300 mm), the lit gap | + tear of sheet metal |
| FINAL | the bolt bends and the strike lips fold flat; the latch tears through; paint chips, a rubber silencer and gypsum crumbs from the frame | the leaf lurches away | flood | the fold, the chips | + shriek and bang |
| BROKEN | the leaf stays whole, permanently bowed, in pose (a) | | | the bow and the lean | |

- **Material:** `Door_CoreSteel` (kraft honeycomb cell texture on the seam's inner faces, primer and bare-steel edge strips). The enamel crazing lives in the `_FR_DAMAGE` onset mask (§3.8).
- **Option for Red (Q3):** steel takes 0.5 s longer (6 / 5 / 5 / 4 / 4 blows), so it always keeps at least one M.

### 3.6 Other doors and the frames
- **Veneer_Oak:** the same cassettes with the oak slot.
- **Ward:** the same cassettes plus its own extras (later).
- **SteelLite:** the steel cassettes, cut to avoid its lite.
- **EXIT doors are never dressed** (Door.cs L141), so they are out of scope.
- **Frames:** production adds a **strike-zone insert** per frame type (Y 0.70 to 1.30 cut out of the frame mesh, the same cassette idea):
  - Wood_DA: the jamb splits beside the strike at FINAL. A 68 × 420 mm strip cracks and stays, with splinter teeth (Direction A's best part).
  - Steel_DA: bent strike lips and crushed silencers.
- The prototype keeps today's strike fall.

### 3.7 Model structure (Blender kit generator, procedural)

**Assets.** These are new modules beside the D1.6 modules. The intact assets are never edited.

| Asset | Contents | LOD0 / LOD1 / LOD2 triangles (budget) |
|---|---|---|
| `Kit_DoorLeafBreak_Veneer_DA`, part `body` | the intact leaf with the cassette window removed; the same loft, metric UVs, slots and wear colours outside the window, with exactly shared boundary vertices | ≤ 2.9k / ≤ 1.1k / ≤ 160 (intact: 2,736 / 1,068 / 148) |
| `cas_M` | the cassette at stage M: a hairline split (open 0.6 to 1.2 mm), a lifted lip, the push-face dish | ≤ 1.0k / ≤ 300 / the zone as plain quads |
| `cas_D1` | the split through the thickness, the strip 4 mm proud in 4 segments along Y (a stepped bend), a veneer lip, 3 splinters, the dish | ≤ 2.2k / ≤ 600 / ≤ 40 |
| `cas_D2` | a 12 to 20 mm slot, the strip 15 to 25 mm out, the petal, a fringe of 20 to 30 splinters, the branch crack, the bore crescent, the leak card | ≤ 4.5k / ≤ 1.2k / ≤ 60 (a dark slot) |
| `cas_TORN` | the notch with stepped faces, splinter teeth, crumbled core, the half-open bore, the hanging petal | ≤ 4.0k / ≤ 1.0k / ≤ 60 |
| `deb_*` | the strip (with the faceplate), ≤ 14 splinters, ≤ 16 crumb clusters, 2 screws, petal fragments | ≤ 3k in total (merged on the floor), LOD1 ≤ 600 |
| Steel (later) | `Kit_DoorLeafBreak_Steel_DA`: body + `cas_M/D1/D2/TORN` with the bow baked in vertices, the seam peel, honeycomb geometry in the seam | ≤ 6k / 9k / 10k at LOD0 |

**The cassette window.** In door-root coordinates it covers Z 0.600 to 0.995 (to the latch end) and Y 0.50 to 1.70, through the full thickness. It holds:
- the lock block and bore;
- the latch end between those heights, with its faceplate;
- the Relay's contact point (Y 1.1 to 1.6, Z 0.645 to 0.845).

**How it is built.** One function per stage takes `stage` and `seed`:
1. **Body.** A boolean difference of the cassette box from the D1.6 slab builders (`da.slab`, `edge_plate`; EXACT with hole tolerance, as the handover found). The interface faces are then deleted, so `body` + `cas_*` close the leaf exactly. The boundary loop is checked to ≤ 0.05 mm.
2. **The split motif** is one 2D polyline per seed, with a run-out of ±6 mm over 400 mm.
   - It runs along the grain (+Y) at Z 0.905 to 0.935, from the bore upward and downward. Its length grows per stage: ±150 mm at M, Y 0.78 to 1.24 at D1, Y 0.62 to 1.42 at D2.
   - The same motif drives the mesh cut **and** the production onset mask (the Siege texture cutter idea), so a texture crack and a geometry crack can never disagree.
3. **Material split** (Siege, Control, Houdini wood):
   - a veneer skin 0.6 mm on each face;
   - a crossband line;
   - the particleboard core, which crumbles into crumb clusters (granular, never stringy);
   - the hardwood stile and lock block (Z ≥ 0.96, and around the bore), which split long, with splinters.
   - Each material gets its own cut style: along the grain with long pieces for the stile, short and granular for the core, torn and lifting for the veneer.
4. **Splinters are real geometry** (Siege's feature-bound decorations). They are thin tapered prisms, 30 to 180 mm long and 2 to 8 mm wide, rooted on the split lips and bent toward the burst face, with no alpha.
5. **Normals.** Inside the window the generator drops the kit's custom normals, re-derives sharp edges and flat-shades the planar faces. Boolean-smeared normals caused shading bands on 10-03 (`20_dir_B.md` §5).
6. **Cut-face UVs** are written in metres: U = depth from the −X face (0 to 0.044 m), V = height along the grain. Material `Door_CoreWood` is used for cut faces in the core, and `Door_CoreStile` for cut faces with Z ≥ 0.96 or inside the lock block (§3.8).
7. **Sidecar JSON** (as the D1.6 sidecars):
   - the parts per stage;
   - the boundary loop hash;
   - anchors: `bore`, `split_top/bot` per stage, `contact_push`, `notch`, `leak_card`, `petal_hinge`;
   - the triangle counts per stage and LOD;
   - the LOD distances, the same as the intact leaf (4 m and 12 m).
8. **Mirror.** A12 bursts toward either face, so the cassette and debris are authored for a +X burst. A −X burst uses a −1 X scale on the cassette node; Unity flips the winding. The lockset parts stay the rig's.
9. **Seeds.** Production uses 3 split motifs and 4 debris throws per family, picked by the edge hash. The prototype uses 1 and 1.

**Debris bake (RE7 style).** `door_break_bake.py` runs a Blender rigid-body simulation of the FINAL:
- The pieces start at their D2 / TORN poses.
- Starting velocities:
  - the strip 0.5 to 1.5 m/s toward the burst side, because it is briefly held by the strike while the leaf leaves;
  - splinters 2 to 5 m/s along the leaf's path, adding the leaf's own velocity at their root (Amnesia's rule [F1]);
  - crumbs 1 to 3 m/s, falling.
- The leaf is a kinematic box keyed on the map's throw: 0 → 105° in 0.12 s from 0.1 s after FINAL, then the bounce. The floor, the saddle and the wall slab around the opening are passive.
- Output: transform keys at 30 Hz per piece until settle (≤ 1.6 s), the settle frame, and a **contact list** (time, piece, speed, surface) for the sound asks.

### 3.8 Texture structure

| Layer | Where | How | Cost |
|---|---|---|---|
| Face veneer | `body` and the cassette faces outside the damage | the existing `Door_Veneer` (or Red's new wood pick from `research/door_wood/`, a material parameter); metric mesh UVs, tile 1.12 × 2.62 m; unchanged | none |
| `Door_CoreWood` | cut faces in the core | FrontRoomsSurface with `_FR_MESH_UV`, tile 0.044 × 0.5 m. Albedo, normal and mask maps are a **cross-section strip** across U: 0.6 mm pale veneer, a dark glue line, 1.6 mm crossband (cross-grain fibres), fine dense particleboard face layer, then coarse low-density core [T-PB], mirrored. V carries the particleboard grain. Built from `Prop_Chipboard` (1024 px/m) plus painted layer lines, 2048 × 256 | the same shader and variant as the face |
| `Door_CoreStile` | stile and lock-block split faces | FrontRoomsSurface, mesh UVs: a long-fibre split face along the grain (lighter, torn, matte), with end grain on cross cuts. Built from `Prop_WoodOak` / `Prop_Studs` | the same |
| Splinters | geometry | `Door_CoreStile` for fibre, `Door_Veneer` on the veneer petals' outer face | the same |
| `Door_CoreSteel` (later) | seam inner faces, crushed edges | a kraft honeycomb texture on the inner faces; primer and bare steel bands by depth | the same |
| `_FR_DAMAGE` onset mask (production P2) | intact faces near the damage (body and cassette) | One onset map per family, 512² RGBA, covering 0.40 × 0.60 m around `damage_latch`, plus a normal map. R = the stage at which a texel turns (0.25 / 0.5 / 0.75 / 1.0), G = cavity or height, B = smoothness change, A = unused for wood. Projected in **leaf object space** (UV-free, so LOD0/1/2 match), with separate burst-face and push-face motifs. The shader shows a texel when onset ≤ stage, with Frostbite-style detail noise ([T-FB] s10). Stage values come from 4 × 2 shared materials (stage × burst side), **not** a MaterialPropertyBlock, so the SRP Batcher holds [T-UN4]. It carries the push-face bruise and crush, hairlines up close, dust wipes, and steel enamel crazing and primer | `shader_feature_local_fragment`, so intact doors keep today's variant; +2 texture fetches and a few ALU only on damaged doors |
| Decals | none | URP Decal is not set up; a projector on a swinging leaf would project onto the jamb and floor; WebGL only has screen space [T-UN1][T-UN3] | none |
| Leak card | inside the split and the bore | an emissive strip of the far lamp's colour × the far lamp level × the stage curve (0 / 0 / 0.15 / 0.40 / 1.0), pulsing to 100 % on each blow and decaying in 0.12 s (`10_design.md` §4.5) | 1 draw |

**Cracks are never dark albedo.** A crack reads by cavity and normal, with a lighter lifted-fibre edge, and by real gaps that show the real room behind (21_C F2).

### 3.9 Runtime

**Component.** `FrontRoomsDoorBreakVisual` is render-only. It sits on the dressed rig's "Leaf rig" and is created on the first damaging blow, so intact doors carry nothing.
- `Blow(int index, int count, Vector3 from)`: works out the stage (§3.2), swaps on this frame, runs the hit-stop and the elastic rock, fires the VFX, and pulses the leak card.
- `Final()`: frees the debris throw and hands the latchbolt and the faceplate to the strip.
- `Broken(Vector3 from)`: runs on `DoorBrokenFrom`, after the map's throw: the rest pose, the breach light fade, and the floor merge at settle.
- `Restore(record)`: runs on `DressDoor` for a damaged or broken door.
- It also raises `DoorStageChanged(door, stage)` for sound (ask S2).

**Swap.**
- On the first damaging blow, the intact leaf's renderers are switched off and `body` + `cas_<stage>` are instantiated under an inserted **"Break pose" node** under "Leaf rig". The rig's parts are moved under that node, so the jolt, the lean and the lockset all move together.
- Later stages enable a different cassette child. There is no blending: the stages are discrete, as in L4D, Amnesia and the glass.

**Hiding the swap.** All on one frame:
- the blow sound;
- the jolt (4 to 8 mm and 0.6 to 1.2°, from the map), plus the visual elastic rock (the latch end 6 / 9 / 12 / 16 / 16 mm out toward the burst side for stages E to FINAL, out in 0.03 s and back in 0.12 s);
- the hit-stop;
- the camera shake;
- a chip and fibre burst at the change;
- the light pulse.

Outside the window the meshes are vertex-identical, so only the window changes (`10_design.md` §5).

**Light.**
- The real gaps pass the far room's real light.
- The leak card guarantees the read when the far lamps are weak.
- On desktop, an optional **breach spot light**: one pooled shadowed spot 1.2 m behind the door on the Relay's side, low from D1, flaring at the break for 0.3 s and fading over 1 s. It back-lights the Relay (RE7's silhouette). One at a time, ≤ 3 s. Not used on phones.

**Debris.**
- The FINAL plays the baked throw (transform keys, ≤ 0.1 ms CPU), with no physics scene. The landing is known in advance, and the door's burst strip is always clear 1.2 m in front of it.
- At settle the pieces merge into **one static floor mesh per door** (1 to 2 draws, ≤ 3k triangles at LOD0).
- After a shift, GD3c's env-hash check applies: `Physics.CheckBox` per piece drops any piece that would sit inside a new prop.
- The live local-physics debris world of GD3c stays available if Red finds baked throws too canned (Q4).

**VFX.** `FrontRoomsDoorBreakVfx` follows the `FrontRoomsGlassVfx` pattern: no Particle System module, fixed 1/120 s substeps, keyed random, and analytic floor and wall collisions in the door root.
- Mesh particles (opaque, with the core and stile materials):
  - a fibre and chip burst of 30 per stage change;
  - 120 at FINAL;
  - crumbs fall straight from the bore at D2 and FINAL.
- Dust:
  - ≤ 16 soft lit cards per blow from `head_dust_a/b` and the split;
  - 40 at the break;
  - camera-facing, alpha-blended, hanging for 2 s.

**Persistence.**
- **Record.** `FrontRoomsDoorBreakRecord`, 16 bytes per edge, in visual-side session memory, the same as GD3c's packed record:

  | Field | Size |
  |---|---|
  | edge | long, 8 B |
  | stage | byte |
  | burst sign | sbyte |
  | motif seed | byte |
  | throw variant | byte |
  | flags (pose, lever state) | ushort |
  | break time | half |

  The debris rest poses come from the bake asset (variant + mirror), so no per-piece data is stored.
- **Restore** on `DressDoor`:
  - stage > 0 and not broken: `body` + `cas_<stage>`;
  - broken: `cas_TORN`, the floor debris at rest, `DetachStrike(atRest)` (exists), the pose, and the drooped lever.
  - A rebuild happens at least 48 m away, so it is never seen.
- **When the Relay leaves mid-break**, the record keeps the stage even if the map restarts its blow count (rule 2). With ask M2 the map can keep it too.

**Budgets per damaged door** (ESTIMATES; desktop is the reference):

| Item | Desktop | Phones (handheld tier) |
|---|---|---|
| Leaf triangles (body + cassette), LOD0 | ≤ 6.9k (2.5× intact; RE7 measured about 3×) | LOD1 only: ≤ 2.3k |
| Extra draws over intact | +2 to 3 (the core and stile slots), plus 1 to 2 for floor debris | +2 |
| Live debris | ≤ 36 bodies for ≤ 1.6 s (Control's cap is 200, RE7's about 50) | ≤ 12, one combined dynamic mesh |
| VFX | 30 per stage, 120 at FINAL, dust 16 per blow | 10 / 40 / 4 |
| Breach light | 1 shadowed spot, ≤ 3 s, one door at a time | none |
| Memory | one stage-mesh set per family, shared by every door, ≤ 1.2 MB; ≤ 60 KB per baked throw | LOD1 set ≤ 0.4 MB |
| Record | 16 B | 16 B |
| CPU | swap ≤ 0.05 ms; throw ≤ 0.1 ms; VFX ≤ 0.2 ms while live | the same or less |
| Ray tracing (G14, Mac) | stage meshes are rigid; acceleration structures are built at chunk build or on the swap frame, never per frame; debris is not traced | none |

**Tiers.**
- Phones are gated at runtime by `FrontRoomsHandheld.Active` and the same profile switch GD3c uses. They get the LOD1 cassettes, the capped debris and VFX, and no breach light. Desktop and Editor output stays byte-identical.
- **WebGL is not dressed** (`DressOnWebGL = false`), so it keeps today's cube-leaf break. If WebGL dressing is ever enabled, it takes the phone tier.

### 3.10 Hooks we need from other lines (asks only; their files are not touched)

**关卡设计 (map), contract lines** for a `CONTRACT_map.diff` in production, as GD3c did:
- **M1. The door on each blow.** `event Action<Door, int, int, Vector3> DoorBlowStruck` (door, index, count, from), raised in `TickBreak` on the same frame as `DoorBlow` and `JoltForBlow`. Today `DoorBlow` carries only a position.
- **M2. Kept damage** (gameplay; Red's Q2). Keep `blowsStruck` per edge when `StopBreaking` runs, resume from it, and expose it for a rebuilt chunk. The visual keeps its own stage either way.
- **M3. The DA broken pose.** For `DoorActing.Double`, `PoseLeaf` should lean about the bottom pivot: pose (a), 3° out of plane and 0.6° sag. Or hang from the top pivot: pose (b). This replaces today's butt-hinge 3° about the top of the hinge edge, so the collider matches the leaf.
- **M4. The throw on a DA door.** Check the 105° throw against the burst-side wall and casing. Stop at the wall-face angle, then bounce to 80°, and put StopLimit there.
- **M5. Timings.** Retire the unread `ShotTimings.DoorBreak.Damage1At / Damage2At / RevealHold`; the stage clock is the blow index.
- **M6. v2 T2.** Its last two blows are 0.2 s apart. Keep that (D2 then shows for only 0.2 s), or space them at least 0.3 s apart.
- **M7 (optional, Q3).** +0.5 s break time on LOCKED steel doors.

**声音设计 (sound) asks** (DB1 §6 drafted these; restated):
- **S1.** `Door/Blow` gets the parameters `Stage` (0 to 4) and `DoorType` (0 wood, 1 steel). Layers by stage:
  - wood: E boom; + creak (M); + split crack (D1); + splintering tear (D2); + tear-out with screws (FINAL);
  - steel: boom and ring; buckle; sheet tear; shriek.
- **S2.** A crack layer exactly on the swap frame (±1 frame), driven by the visual's `DoorStageChanged(door, stage)`.
- **S3.** A new `Mechanism/Door/DebrisImpact` (size class, speed, surface), fired from the bake's contact list, so the times are known in advance and exact.
- **S4.** Break: a pivot-shear layer (metal and screws). The wall hit at the end of the throw is already StopLimit.
- **S5.** `BrokenTried`: the hanging leaf grinds on its bottom pivot (pose a) or creaks on its top pivot (pose b), together with the existing `BeatWobble`.

**Ours (visual), no ask needed:** the Relay rig's blow contact (forearm at 1.1 to 1.6 m, 0.15 to 0.35 m from the latch end), which sets `contact_push`.

---

## 4. The prototype (this workflow: one build stage, one test round, one MP4)

### 4.1 Goal
Show Red the whole veneer-leaf sequence at game scale, in the game's own light and post, from the player's view and three others, with the final pose and its alternative. It must be enough to approve or redirect the look. Nothing lands.

### 4.2 Scope

**In:**
- the L0-F door (Wood_DA frame + Veneer_DA leaf), bursting toward the player;
- the T1 sequence (5 blows), plus one T5 take (3 blows);
- `body` and the cassettes `cas_M`, `cas_D1`, `cas_D2` and `cas_TORN` at **LOD0 and LOD1**;
- one split motif and one baked throw;
- `Door_CoreWood` and `Door_CoreStile` (FrontRoomsSurface, mesh UVs, no shader change);
- the elastic rock and hit-stop, the leak card, the breach spot light (desktop), mesh-particle chips and crumbs, and dust cards;
- pose (a), plus pose (b) as an alternative hold.

**Out (production):**
- the `_FR_DAMAGE` onset mask and any FrontRoomsSurface edit;
- LOD2 cassettes; mirror bursts; Oak and Ward;
- steel; frame strike-zone inserts;
- the record and restore, and the floor merge;
- the phone tier;
- map and sound changes. The video is **silent**, because clones are silent; the beats are burned in as captions.

### 4.3 Files to make
All of them go in a fresh clone, `W/proj_doorbreak`, made with `W/tools/mkclone.sh proj_doorbreak` and then `install_tools.py` (silencer and test bundle id). W = `/Users/redwang/FrontRoomsVisualWork`.

| # | File (in the clone unless W) | Role |
|---|---|---|
| 1 | `Tools/Blender/frontrooms_kit/assets/interact_door_break_common.py` | Shared code: cassette window, split motif (seeded 2D polyline), boolean cut with hole tolerance and boundary check, material split, metric cut-face UVs, splinter prisms, crumb clusters, petal, flat-shading inside the window, sidecar writer. Reads D1.6 constants from `interact_door_da_common`, never changes it |
| 2 | `Tools/Blender/frontrooms_kit/assets/interact_door_leaf_veneer_da_break.py` | `NAME = "Kit_DoorLeafBreak_Veneer_DA"`: `body` LOD0/1/2, `cas_M/D1/D2/TORN` LOD0/1, `deb_*`, `leak_card`. Output `Assets/Resources/Props/Models/Kit_DoorLeafBreak_Veneer_DA.fbx` + `.json` |
| 3 | `Tools/Blender/frontrooms_kit/door_break_bake.py` | The rigid-body bake of FINAL with the kinematic leaf throw. Output `Assets/Resources/Props/Models/Kit_DoorLeafBreak_Veneer_DA_throw0.json` (30 Hz keys, settle frame, contact list). Fallback if the Blender bake fails: scripted ballistics with analytic floor and wall, in the same JSON format |
| 4 | `Tools/door_break/make_core_textures.py` (numpy + PIL) | `Assets/Resources/Surfaces/Textures/Door_CoreWood_{A,N,S}.png` (2048 × 256 cross-section strip) and `Door_CoreStile_{A,N,S}.png` (1024², long-fibre split face), from `Prop_Chipboard` / `Prop_WoodOak` |
| 5 | `Assets/Scripts/Rendering/Doors/FrontRoomsDoorBreakVisual.cs` | The runtime component (§3.9) without the record: stage swap, Break pose node, rock and hit-stop, lever and rose transforms, strip handoff, throw playback, leak card, breach light, poses (a) and (b) |
| 6 | `Assets/Scripts/Rendering/Doors/FrontRoomsDoorBreakVfx.cs` | Chips, fibre and crumbs as mesh particles, and dust cards (URP `Particles/Simple Lit` on quads in the prototype); budgets as in §3.9 |
| 7 | `Assets/Editor/DoorBreak/FrontRoomsDoorBreakFilm.cs` | Clone-only Play-mode harness (ASCII only, no tuples, `FrontRoomsObjectId.Of` instead of `GetInstanceID`). It builds the `Door_CoreWood` / `Door_CoreStile` materials (clone-only), runs the takes and renders the cameras (§4.4, §4.5) |
| 8 | `W/door_break/DB2/run_film.sh` | Checks `W/tools/unity_jobs.sh`, waiting once a minute while 2 jobs run. Runs Blender (files 2 and 3), then `Unity -batchmode -projectPath W/proj_doorbreak -buildTarget OSXUniversal -executeMethod FrontRoomsDoorBreakFilm.RunBatch -frGlassRT off -logFile ...` (no `-quit`, no `-audioCapture`, no `-nographics`) |
| 9 | `W/door_break/DB2/compose_film.py` | Uses `W/tools/shotframe`: title and end cards in the Shot format, a caption strip on every frame from `frames.tsv`, the stage-hold segment, and the contact sheet |
| 10 | `W/door_break/DB2/encode.sh` | `ffmpeg -framerate 60 -i %05d.jpg -c:v libx264 -crf 20 -preset slow -pix_fmt yuv420p -movflags +faststart`, then `ffprobe` for duration and frame count |

**Outputs:**
- `W/door_break/DB2/out/door_break_proto_v1.mp4`;
- `W/door_break/DB2/out/contact_sheet.jpg` (Shot format, status FOR RED);
- `film_log.txt`, `frames.tsv`, `stats.json` (triangles per part and LOD, swap frames, settle time, ROI pixel changes);
- the frames, as JPG q90 under the clone's `Verification/door_break_film/`.

No main file is written.

### 4.4 Scene setup
- **Play mode, the game's own run.**
  - `FrontRooms3D.unity`, seed 516574485 via the title handoff (`RequestTitleStart` after `Random.InitState(4242)`, as the GD3c repro does).
  - The Relay is held dormant (`hunterTuning.releaseDelaySeconds = 1e6`).
  - `Time.captureDeltaTime = 1/60` (1/240 for the slow-motion take).
  - Game lights and post (`FrontRoomsPostStack.ConfigureCamera` on the harness cameras), Glass RT off.
- **Door choice** (logged):
  - the nearest L0-F door within 40 m of the start, with both cells Level 0;
  - at least 3.5 m clear along the face normal on both sides;
  - the Relay-side room's lamp alive, and its lamp level at least that of the player-side room. Both levels are logged. **No harness lights.**
- **Each take:**
  1. `world.ResetDoorForTools(door)`, then reset the visual.
  2. `world.SwingAwayFrom(door, relayPos)` and `world.MarkBeingBroken(door, true)`. `relayPos` is 0.6 m off the push face at the lock height, so the burst goes toward the player-side cameras.
  3. At the tier's blow times: `world.JoltForBlow(door, i, n)`, `breakVisual.Blow(i, n, relayPos)`, and `hunterRig.DoorBlow(i, n)` if the rig can be shown.
  4. At T: `world.BreakDoor(door, relayPos)`. The real `DoorBrokenFrom` then gives `DetachStrike`, and the visual's `Broken` runs.
  5. Render until 2.0 s after the break.
- **The Relay in C3.** The harness moves the game's `FrontRoomsRelayRig` to its break stance on the push side, facing the door, and keeps it visible. If the dormant rig cannot be shown, it is hidden and `relay=hidden` is logged; the video still works.
- **Multi-camera.** Each simulated frame renders every camera of that take into its own RenderTexture, so one simulation feeds all views, as the GD3c lab did.

### 4.5 What the camera must show (about 52 s, 1920 × 1080, 60 fps)

| # | Segment | Camera | Take | Length |
|---|---|---|---|---|
| 0 | Title card (Shot format: "DOOR BREAK · PROTOTYPE 1 · VENEER LEAF L0-F", FOR RED) | none | none | 2 s |
| 1 | **Player view, real time** | **C1**: burst side, 3.0 m from the leaf face along its normal, shifted 0.35 m toward the latch side, eye 1.62 m, vertical FOV 76°, aimed at Y 1.1 on the lock. The game's blow camera shake is on | T1, from −0.5 s to +2.0 s around the break | 5 s |
| 2 | Stage holds | C1 frames held 1.0 s each: S0, E, M, D1, D2, FINAL, BROKEN, captioned | from take 1 | 7 s |
| 3 | Close, burst side | **C2**: 1.0 m from the lock, Y 1.15, FOV 50°, 20° off-normal toward the hinge (to see the strip stand proud and the slot) | T1 real time, then FINAL to break at 0.25× (simulated at 240 fps from 2.35 to 2.75 s) | 4 + 1.6 s |
| 4 | Relay side | **C3**: push side, 3.5 m back, eye 1.62, FOV 76°, the Relay in frame (or logged hidden) | T1 | 5 s |
| 5 | Edge view | **C4**: player side, 2.0 m along the wall from the latch jamb, 1.4 m high, looking at the latch end about 65° off the normal | T1 | 4 s |
| 6 | Mid distance (LOD1) | **C5**: burst side, 6 m, eye 1.62, FOV 76° | T1 | 4 s |
| 7 | Fast tier | C1 | T5 (3 blows: D1, D2, FINAL) | 3 s |
| 8 | Broken-state orbit | **C6**: a 6 s orbit of 120° at 1.8 m radius around the latch side, 1.3 m high, starting 2 s after settle: notch, petal, lever, lean, debris, strike | after take 1 | 6 s |
| 9 | Pose (b) alternative | C1 and C4, a 2 s hold each, captioned "POSE B · ALT" | pose (b) applied | 4 s |
| 10 | End card | the contact sheet | none | 2 s |

**Caption strip on every frame** (shotframe `caption_strip`): `T1 · BLOW 3/5 · D1 · 1.50 s · C1 3.0 m`. A white tick flashes in the strip on the blow frames, standing in for the sound beat.

### 4.6 Token-saving recording (Red: "节省token的方式")
1. **Frames are rendered, not watched.** The harness renders deterministic frames in batch. Nothing is screen-recorded, and no frame is opened by the agent.
2. **Numbers first.** `stats.json` is the main check: swap frames equal blow frames, triangle budgets, settle time, and changed pixels in the lock region per stage. It costs a few hundred tokens.
3. **One image to look at.** The agent opens only `contact_sheet.jpg`: 12 tiles at 1920 wide (C1 per stage, C2 at D2 and FINAL, C3 at a blow, C4 at BROKEN, C5 at D2, the orbit, pose b). It opens one crop sheet only if a gate fails.
4. **No per-frame upload.** No Figma upload per frame and no review or critic stage (rule 23). The VL entry is batched later with the contact sheet.
5. **One MP4 for Red,** under 60 MB, sent by the session as a file.

### 4.7 The one test round (gates, numbers only)
| Gate | Check | Pass |
|---|---|---|
| G1 | Roslyn compile of the clone (`W/tools/roslyn_check.py`) | 0 errors |
| G2 | Generator log | The boundary loop between `body` and every cassette matches within 0.05 mm. Every stage is within its triangle budget. No non-manifold edge except intended open splinter roots |
| G3 | `frames.tsv` | Every stage swap lands on its blow frame. No change happens between blows. Hit-stop frames are 2 / 2 / 3 |
| G4 | **The 3 m read**: changed pixels (luma Δ > 6/255) in C1's lock-region box between consecutive stage holds | ≥ 400 px each for M→D1, D1→D2, D2→FINAL and FINAL→BROKEN. E→M is logged only (M is close-range by design) |
| G5 | Throw | Settle ≤ 1.6 s after FINAL. No piece below the floor or inside the wall slab at any key |
| G6 | MP4 | `ffprobe` duration 50 to 56 s at 60 fps, the frame count equals the composed frames, size ≤ 60 MB |

### 4.8 Risks and fallbacks
- **The dormant Relay rig cannot be posed.** C3 runs without it, logged.
- **The far room is darker than the near room on every candidate door.** The leak card still reads; the harness logs the lamp levels, and no fake light is added.
- **The boolean fails on the open D1.6 slab.** Use the closed LOD1 section as the cutter base (the handover notes LOD0 has 96 open edges), or loft the body window directly from the D1.6 ring builder.
- **The Blender rigid-body bake fails.** Use the scripted-ballistics fallback (§4.3 item 3).
- **The Unity job cap.** At most 2 visual jobs. Wait once a minute. Never touch proj_build, proj_sound_*, main or "We're Car".
- **Render time.** About 2,300 frames at about 0.25 s each is roughly 10 minutes, plus 3 to 5 minutes of startup.

---

## 5. Production plan, after Red approves (ESTIMATES, working days)

| Phase | Work | Days |
|---|---|---|
| P0 (this workflow) | The prototype and video | 1 build stage |
| P1 Veneer family | All LODs (LOD2 cassettes); 3 motifs and 4 throws by edge hash; mirror bursts; Oak and Ward slot swaps; the floor merge; `FrontRoomsDoorBreakRecord` and restore on `DressDoor`; the env-hash check after a shift; the kept stage when the Relay leaves; edit-mode tests (determinism, seams, budgets) | 3 |
| P2 Surfaces and shader | `_FR_DAMAGE` onset mask and normal in FrontRoomsSurface (burst and push motifs from the split motif, detail noise, 8 shared stage materials); `Door_CoreWood` / `Door_CoreStile` / `Door_CoreSteel` as SurfaceDefs; adopting Red's door-wood pick | 1.5 |
| P3 Steel family | `Kit_DoorLeafBreak_Steel_DA` (and SteelLite): baked bow, dish, seam peel with honeycomb, crazing in the mask; chips, silencer and gypsum crumbs; throws | 3 |
| P4 Frame strike-zone inserts | Wood_DA jamb split (Direction A's strip) and Steel_DA bent lips and silencers; head dust | 1.5 |
| P5 Phone tier | LOD1-only cassettes, combined debris, VFX caps, no breach light; desktop byte-identical check | 1 |
| P6 Hand-offs and landing | `CONTRACT_map.diff` for M1 to M6 and the sound asks S1 to S5, written into the handoff files (no messages); package, `rebase_apply.sh` dry run, Roslyn + the one covering suite (rules 22 and 23); land when Red says yes; a VL batch with the contact sheets | 1 |
| **Total** | | **about 11 days**, plus the map and sound lines' own time for their hooks |

---

## 6. Open questions for Red

> **ANSWERED 2026-10-30 12:08 (Red, 游戏视觉 chat, verbatim "1a，2 要，3 要，4 预算，5 要"):** 1 = (a) the top pivot tears and the leaf leans ~110 mm on its bottom pivot; 2 = yes, kept blows (M2: the Relay resumes from the blows already struck); 3 = yes, steel takes 0.5 s longer (6 / 5 / 5 / 4 / 4 blows); 4 = baked throws (4 variants per family); 5 = yes, the leak card. 6 = the walnut face (Red's door-wood pick, landed 10-30). Production P1–P6 starts now.

1. **Final pose.**
   - (a), recommended: the top pivot tears, and the leaf leans about 110 mm on its bottom pivot, with a gap at the head. It reads clearly at 3 m and follows the real pivot load.
   - (b): the bottom arm tears and the leaf hangs from its top pivot, with the latch corner on the carpet. It is more subtle, about a 13 mm change at the head.
   - Either one is a map contract line (M3). The video shows both.
2. **Does a half-broken door stay weak for gameplay?**
   - Visually it always stays damaged.
   - Recommended: yes. When the Relay comes back it continues from the blows it already struck (M2). The player then learns that a split door buys less time.
   - The alternative: the map restarts at blow 0, and the visuals only keep the look.
3. **Steel takes 0.5 s longer** (6 / 5 / 5 / 4 / 4 blows), so a steel door always shows a dent before it gives. Default: off.
4. **Debris.**
   - Baked throws, recommended: deterministic, cheap, exact restore. They can look the same on every door of a family, which 4 variants per family soften.
   - GD3c's live physics scene: it reacts to furniture and bodies, but costs more and cannot know its rest in advance.
5. **The leak card.** An emissive line in the split, scaled by the far lamp, so the slot reads even when the far lamps are weak. It is honest when the far lamp is dead (0). Recommended: yes.
6. **The wood face.** The break takes the face veneer as a parameter. Which `research/door_wood/` option should the production break use? The prototype uses today's `Door_Veneer`.
