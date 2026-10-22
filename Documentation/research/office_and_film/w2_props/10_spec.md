# W2 · Office prop period fixes: brass torchiere, armless task chair (spec)

Status: SPEC (research stage of workflow `office-props-w2`, 2026-10-21). Owner: 游戏视觉. Nothing is built yet.
Red's decision (VISUAL_CHAT_TASKS.md:15 and :416, 2026-10-21): "the torchiere to brass with a glass/acrylic shade; the task chair to armless grey/coloured fabric (keep the black armed one as a variant)".

Every claim carries a source tag. The key is in §9. `[GEN]` marks general knowledge that no project file backs; treat it as an assumption. `[MEAS]` marks my own measurement on a scan; the method is given where it is used.

## 1. What 平面视觉 flagged (REAL vs MODEL, Figma file 0tCbAiVUlrPId3RWd9LRif)

| Card | Their note | Image |
|---|---|---|
| R39 · Halogen torchiere, node 2350:1174 | Lede: "Same type, but 1993 Sears torchieres are brass, not black." Different: "Sears 1993: brass with glass or acrylic shades. The black steel halogen bowl is mid-'90s." | `images/R39_torchiere_card_2350-1174_refmasked.png` |
| R03 · Task chair, node 2349:907 | Lede: "1993 chairs are mostly armless, in grey or coloured fabric." Different: "Catalogue chairs come in grey, red, navy or pink fabric; ours is black with arms." | `images/R03_task_chair_card_2349-907_refmasked.png` |
| K40 · Halogen torchiere, node 2328:1066 | W 362 · D 362 · H 1853 mm, era 1985–97, "current stock", black steel + white plastic | `images/K40_torchiere_sheet_2328-1066.png` |
| K04 · Task chair, node 2325:947 | W 617 · D 653 · H 953 mm, era 1985–98, black plastic / black fabric / chrome | `images/K04_task_chair_sheet_2325-947.png` |

The repo copies of the R cards have their reference panels masked. Those panels are third-party scans (Sears 1993, and one Commons photo under CC BY-SA 4.0), and COMMON.md rule 7 keeps third-party media out of this public repo. The unmasked cards and the measured crops are in `/Users/redwang/FrontRoomsVisualWork/w2_props/research/`: `R39_torchiere_figma.png`, `R03_task_chair_figma.png`, `sears1993_p319_N_measured.jpg`, `sears1993_p319_N_bowl_cup_zoom.png`, `sears1993_p319_N_base_zoom.png`, `sears1993_p359_chairs_MNP.jpg` and `sears1993_p358_B_office_chair.jpg`. The originals and their licences are listed in `Research/week02/kit-references/SOURCES.md`.

**Era lock:** now = 1990, nothing designed after 1993, printed dates ≤ 1990 [ERA §2]. A 1993 catalogue sits on the lock's edge and inside it. Kit_TaskChair is dated 1985–98 and Kit_Torchiere 1985–97, both "current" [ERA rows 40, 76]. Halogen torchieres were sold in the US from 1983 [ERA §5, CPSC 1997 recall].

## 2. Today's state (read 2026-10-21)

- **`assets/torchiere.py` [TPY]:** a black halogen uplight. Ø0.36 bowl at 1.852 m on a black steel pole and base. Rotary dimmer pod at 1.05 m. R7s tube on a bracket. Cord and plug. Slots `Prop_SteelBlack`, `Prop_PlasticWhite`. `no_collider`. Anchors `light` and `dimmer`. Pile: Tall, mass 0, domestic70s, topper, states Upright/EdgeLean.
- **Kit_Torchiere sidecar [SCT]:** 636 / 267 tris (LOD0 / LOD1); footprint 0.362²; minCeiling 1.902; no `lodDistances`.
- **`assets/task_chair.py` [TCPY]:** a black operator chair with loop arms. 5-star nylon base with twin casters, 3-stage shroud over a chrome gas cylinder, mechanism with a tension knob, height paddle (user's right) and tilt lever, welted cushions.
  - Slots: `Prop_FabricChair`, `Prop_PlasticBlack`, `Prop_Chrome`.
  - One collider box, sized to the real bounds.
  - Anchors `sit` and `back_top`; support `seat` (0.40 × 0.38).
  - Pile: Seat, mass 1, office90s, states U/I/S/B.
  - The docstring names three real furniture brands. They must go (rule: no real brands anywhere).
- **Kit_TaskChair sidecar [SCC]:** 4,484 / 1,792 tris (LOD0 / LOD1); bounds 0.617 × 0.953 × 0.653.
- **GUIDs to keep [meta files]:**
  - Kit_Torchiere: fbx `a2e24975960dd4565ab49a1cdb646116`, json `a954ac716fc0044849bff5062d5929b0`.
  - Kit_TaskChair: fbx `99ce0ad616e3f42c7849c8d429537d95`, json `7a8141bd7e3064dac85a510a8b65f829`.
  - The material remaps in `.fbx.meta` are rewritten on reimport by FrontRoomsKitImporter.OnPreprocessModel (AddRemap) [KI:22-48].
- **Where they are used:**
  - Chair: only `FrontRoomsOfficeKit.Chair` → `BuildStation` [OK:54, 959-964]. 85 % of stations get one, at x U(−0.25, 0.25), z = deskZ + deskDepth/2 + U(0.15, 0.55), yaw 180° ± 50°, colliders on.
  - Torchiere: no Office use. It appears in piles, through the auto library of every kit with a pile block [FP:296-320; KLIB:151-157, names sorted ordinally], and in the 关卡设计 sample module `L0_WaitingRoom_4x3`: `Wall("Kit_Torchiere", 6.6, West)`, stored at x 0.291 [LD:318; Assets/Levels/Modules/L0_WaitingRoom_4x3.asset].
  - The anchors `light` and `dimmer` have no consumer in code (grep of `TryAnchor`). No Light is ever spawned with the lamp.
- **Context:**
  - The player's eye is at 1.62 m [MU:92].
  - LOD switch heights come from sidecar `lodDistances` at a 76° reference vFOV; without them, LOD1 starts at 10 % of screen height and the lamp culls at 2 % (large props) [KI:80-104].
  - Task chairs are the biggest triangle item in the Office view: 8 chairs × ~1.8k = 14.3k tris [WGL:114].

## 3. Kit_Torchiere → brass halogen torchiere (name and GUIDs frozen; content replaced)

### 3.1 Type: the Sears 1993 "N" body, kept halogen
- **The period source.** The one we have is Sears 1993 p.319 item N, "Torchiere lamp": a "terraced acrylic shade with gold rim", 66½ in high, 13 lb [S319 text]. Items M–P have a "solid-brass terraced base and oversized center pole" [S319 text]. Note that N is an incandescent lamp: "up to a 150-watt bulb" on a "three-way switch" [S319 text].
- **The decision.** Keep the kit a **halogen** torchiere, as the era lock describes it ("300 W halogen uplight" [ERA row 76]) and as the W2 brief asks. Build it on N's brass body and proportions. Inside the bowl, an R7s tube on a bracket replaces N's socket.
- **What it costs.** The two types look the same from eye level (§3.6). The halogen choice only decides the shade material (§3.4) and the part seen from above.
- **The alternative, not chosen:** a literal N. That means an acrylic bowl, a 150 W A-lamp socket and a three-way switch knob in the same place.

### 3.2 Dimensions
Measured on the full-res scan `sears1993_n0332.jpg` [MEAS]. Scale: 1,689 mm (Sears' 66½ in) over 1,715 px, from rim to floor point, gives 0.98 mm/px, ±3 % for perspective.

| Part | Spec | Source |
|---|---|---|
| Overall height (floor → bowl rim) | **1.689 m** (today 1.852) | S319 text |
| Bowl | rim **Ø0.39**, foot Ø0.12, depth **0.11**; wall ~40° from horizontal; **4 terraces** on the outside, with fine moulded rings on each tread | MEAS 400 / 125 / 112 px; S319 photo, zoom |
| Gold rim | brass band at the lip, ~4 mm tall, proud of the glass by ~2 mm | S319 text "gold rim"; photo |
| Socket cup | polished brass hemisphere, **Ø0.10 × 0.05**, rolled lip, neck collar into the pole; bowl foot seated in it | S319 photo zoom |
| Pole | plain polished brass tube, **Ø0.024**; no visible couplings or collars between cup and base | MEAS ~24 px; S319 "oversized center pole" |
| Base | round, **Ø0.245**, about 0.055 high: a ~20 mm vertical rim, then **5 concentric terraces** rising to a boss of ~Ø0.045 at the pole | MEAS 250 px; S319 "terraced base", photo zoom |
| Cord and plug | from the back of the base, ending in a two-pin plug (today's routing kept, scaled to the new base) | TPY |

Front (knob side) = −Y in Blender, +Z in Unity [KL conventions].

### 3.3 Finish: **polished, lacquered brass**
- **Why polished:** Sears shows mirror-bright brass with sharp highlights on every lamp M–P, and calls the line "our best brass lamps" with a "traditional brass finish" (R, S) [S319 text and photo]. Satin or brushed brass appears on no period lamp in our files. "Brushed finishes came later" is [GEN], not sourced.
- **The material:** use the existing **`Prop_Brass`**: metallic 1, smoothness 0.6, base colour (0.69, 0.541, 0.29) [RS:363; Prop_Brass.mat]. The synthesis target is 0.55–0.65 [SYN §5.2].
  - At 0.6 it reads as lacquered brass with a decade of haze, which fits a 1990 room of second-hand stock [ERA §1].
  - It also matches Kit_TableLampPleated and the lock hardware, which share the slot.
  - Do **not** change `Prop_Brass` in W2. 38 kit sidecars list it (grep), and RenderSetup is being changed tonight by outlets_p4b.

### 3.4 Shade: **frosted (satin-etched) glass**, terraced bowl, rendered opaque
- **Why glass.** Red allowed either glass or acrylic [VCT:416]. Sears' acrylic bowl goes with a 150 W incandescent bulb [S319 text]. A 300 W halogen tube needs a heat-proof bowl [GEN]; the 1997 recall was about fire hazards [ERA §5]. Glass keeps the kit honest as a halogen lamp.
- **Why frosted, not clear.** N's bowl is a milky white translucent with fine rings [S319 photo]. A clear bowl would show the holder through it.
- **The material:** opaque **`Prop_PlasticWhite`** on both faces. Mean albedo #D9D5C8 [measured on Prop_PlasticWhite_A.png; SYN §5.2 target #D9D5C8]; mask smoothness ~0.52 [Prop_PlasticWhite_S.png], which is a satin, frosted read.
  - Reject `Prop_Glass`. It is clear URP Lit Transparent at α 0.16 [RS:416], so it reads clear, costs a sorted transparent draw and shows the holder.
  - Reject `Prop_LampShade`. Its smoothness of 0.1 [RS:377] reads as paper or fabric.
- **Geometry:** a real 4 mm wall. The terraces are geometry (riser, rounded nose, tread). Model the moulded rings only as one shallow groove per tread; finer rings would need a new normal map, which is out of scope.

### 3.5 Dimmer
- **What and where:** a rotary knob on the **side of the brass socket cup**, facing front (−Y), at ~1.555 m. Sears N has its switch knob in that spot [S319 photo zoom]. In the halogen kit the knob is the full-range dimmer for the tube; "halogen torchieres had full-range dimmers" rests only on the house claim in [TPY].
- **Size and finish:** knob Ø0.022 × 0.014, knurled with 12 ribs, polished brass.
- **What goes:** today's black dimmer pod on the pole at 1.05 m [TPY]. It belongs to the black halogen look that R39 calls mid-'90s.

### 3.6 Bulb glow, and whether the shade is emissive: **not emissive (lamp off) in W2**
- **How it lights when on.** An uplight sends the tube's light to the ceiling, and the frosted bowl glows from inside, brightest near the foot [GEN].
- **The bulb is out of sight.** The rim sits at 1.689 m, 7 cm above the 1.62 m eye [MU:92], so a standing player sees only the bowl's underside. The tube is seen only from above: a pile EdgeLean, or a three-view top.
- **Default: off.** Every placement is an unplugged lamp: pile pieces are "placed wrong", intact [FP header], and there is the L0 waiting-room module. No code spawns a Light for it (§2). An emissive bowl would read as a light source that lights nothing. The synthesis lists no lit lamps except the later lamp-lit island [SYN §6.6 "Lamp-lit island (A8, later)"]. So the bowl stays `Prop_PlasticWhite` and the tube stays `Prop_PlasticWhite` (off).
- **Later, a lit variant (not W2, a new name, ask Red first):**
  - Use VARIANTS to swap the bowl to `Prop_LampShadeLit` (emission 1.4/1.1/0.65 [RS:396]) and the tube to `Prop_BulbLit` (4/3.5/2.6 [RS:397]).
  - The spawner adds a real upward Light at the `light` anchor at 3000 K [FILM T07; SYN line 636].
  - A uniform emission glows flat. The real foot-bright gradient needs an emission mask (a new texture).

### 3.7 Slots (3 of the ≤ 4 allowed [SYN §5.1]; all existing)
| Slot | Parts |
|---|---|
| `Prop_Brass` | base, pole, cup and collar, gold rim band, dimmer knob, tube bracket and end caps |
| `Prop_PlasticWhite` | bowl (both faces, lip), halogen tube |
| `Prop_PlasticBlack` | cord, plug body, a thin felt ring under the base (no source for cord colour; house choice) |

`Prop_SteelBlack` leaves this kit. No new material, so there is **no RenderSetup hunk**.

### 3.8 Budgets, hero check at 0.3 m
**The hero check.** At 0.3 m and 76° vFOV, a 1080 px frame shows 0.469 m, which is 2.3 px/mm [KI formula]. Ring counts are chosen so that a silhouette facet's sagitta stays at or under ~1 px:
- bowl: **64 sides** (0.24 mm at r 0.195);
- base: **48** (0.26 mm);
- cup: **32**;
- pole: **20**;
- knob: 24.

Comparable hero kits: Kit_ExitSign 3,136 / 984 tris; Kit_FloorBoxTombstone 3,576 / 1,118 / 84 (LOD0 / LOD1 / LOD2); Kit_CRTMonitor 3,096 / 1,456 [their sidecars].

| LOD | Tris (budget) | Content |
|---|---|---|
| LOD0 | **≤ 4,500**: bowl ~2,050, base ~1,150, cup ~380, pole ~120, rim ~380, rest ~400 | full spec above |
| LOD1 | **≤ 1,600** (`LODS = (0.35, 0.09)`) | collapse; knob and plug `fr_lod1_drop` |
| LOD2 | **≤ 400** | silhouette: bowl, cup, pole, base; tube, cord and knob dropped (`fr_lod2_drop`) |

**Switch distances.** `kit.meta["lodDistances"] = [8, 20, 54]`. With the LODGroup size of 1.69 m, these switch at 13.5 % / 5.4 % / 2.0 % of screen height [KI:84-95]. The 54 m cull equals today's default 2 % cull [KI:103], so the pile spike stays visible across a hall.

### 3.9 Anchors, collider, pile
- **Anchors:**
  - `light` (0, 0, 1.62) Blender: bowl centre just above the tube, for a future upward Light.
  - `dimmer` (0, −0.062, 1.555): the cup knob. It moves from 1.05 m; nothing reads it today (§2).
- **Collider:** add **one box** of the bowl footprint × the full height, 0.39 × 0.39 × 1.689, centred (0, 0, 0.845).
  - Today's `no_collider` [TPY; SYN §5.3] lets the player walk through the lamp in L0_WaitingRoom (module noCollider 0).
  - The bowl spans 1.58–1.69 m, below the 1.75 m player height [MU:92], so the box is physically right.
  - Pile pieces have their boxes disabled anyway [SYN §5.1].
  - The footprint is the union of the colliders, so it stays bowl-sized: 0.39² (was 0.362²) [KL export].
- **Pile:** unchanged (Tall, mass 0, domestic70s, topper, Upright/EdgeLean, tolerance 0.3) [SCT].
  - It is still the highest thin spike of Still B [FILM F02], now in brass with a white bowl.
  - Tags: pile, lamp, domestic.
- **Derived sidecar values:** minCeiling = 1.689 + 0.05 = **1.739** [KL export]; placement Floor.

## 4. Kit_TaskChair → armless 1990 task chair, grey (name and GUIDs frozen; content replaced)

### 4.1 Period reference
Sears 1993 p.359 [S359]:
- **M "Desk chair":** "pneumatic seat height adjustment (from 17 to 22 inches), contoured swivel seat. Polyester covered seat and back".
- **N "Task chair":** "pneumatic lift…, manual back and angle adjustment".
- **P "Ergonomic chair with arms":** the only one of the three with arms.
- **Colour options:** listed only for R, the "multi-purpose chair": "17 Red, 775 Royal navy, 29 Hot pink, 944 Ebony".
- **Photos:** M/N are armless, in grey to taupe fabric, on a black 5-star base with twin casters.
- **Other pages:** p.358 B has "gray nylon upholstery" [S358]; p.356 K has a "black fabric task chair" [S356].

### 4.2 Build (rewrite `assets/task_chair.py` from today's module, which already holds the base, casters and helpers)

| Part | Spec | Source |
|---|---|---|
| Overall | ~0.64 W × 0.66 D × **0.93** H | today's base [TCPY]; N back proportion [S359 photo] |
| Seat | 0.48 W × 0.46 D, **top 0.45 m**; contoured: dish, waterfall front, back slightly narrower; **plain pads, no welt** (M/N show none); on a black shell | S359 text "contoured", 17–22 in = 0.43–0.56 m; photo; TCPY shell |
| Back | **shield/oval pad 0.44 W × 0.42 H**, plan curve plus lumbar swell, top 0.93 m; black plastic outer shell | S359 N photo; TCPY shell |
| Back bar | black flat-steel upright, **visible in a 0.05–0.06 m gap** between seat and back; runs under the seat to the mechanism | S359 M/N photo; TCPY spine |
| Back adjust | back-angle knob at the bar root (rear of the mechanism); back-height knob on the bar's rear | S359 N text "manual back and angle"; TCPY |
| Mechanism | black seat plate and housing; **height lever**: a long rod out to the **user's right (−X)**, angled down and forward, black paddle grip. No left tilt lever and no front tension knob (neither shows on M/N) | S359 M/N photo; TCPY |
| Gas lift | **smooth black tube housing Ø0.07** (no bellows; bellows are P / armed) plus **exposed chrome piston Ø0.05, ~0.06 m visible** under the seat | S359 M/N photo |
| Base | today's 5-star (tip radius 0.318, tapered nylon legs, hub) with **twin-wheel casters** at mixed swivel angles | TCPY; S359 photo |
| Arms | **none**; arm loops, pads and straps deleted | R03; S359 M/N |

Hero refinements, for at most ~1 px of facet at 0.3 m (§3.8 method):
- cushion corner steps 5 → **8**;
- gas piston and housing 12 → **24** sides;
- caster wheels 8 → **16**;
- hub 12 → **24**.

The tris saved by dropping the arms pay for these.

### 4.3 Upholstery colour and slots (3 slots, all existing; no RenderSetup hunk)

| Slot | Kit_TaskChair (grey) | Kit_TaskChair_Navy (VARIANT) |
|---|---|---|
| fabric | **`Prop_FabricCubicle`**: slate grey, mean albedo #59636D (texture), poly-wool herringbone [SYN §5.2] | **`Prop_FabricNavy`**: #222F49 (texture), rough linen [SYN §5.2] |
| `Prop_PlasticBlack` | base, casters, shells, plate, bar, lever, knobs, housing | same |
| `Prop_Chrome` | piston | same |

- **Why grey.** It comes from Sears M/N/P and p.358 B "gray nylon" [S358, S359]. The only existing mid-grey fabric is FabricCubicle. FabricCharcoal (#39393B) reads almost black, which undoes Red's "not black" fix.
- **The risk.** The chair is in the same fabric as the panel behind it.
  - **Gate:** in the look-dev `office_desk` frame the chair must still separate from the panel.
  - **If it fails:** add `Prop_FabricGrey`, a SurfaceDef reusing the `Prop_FabricCharcoal` texture with a tint of ~1.6, neutral #5C5C5E. It ships as a `.mat` with a new GUID plus a **separate** RenderSetup diff, since `Prop_SteelAlmond` [RS] is the precedent for texture plus tint.
- **Why navy.** It follows Sears "775 Royal navy" [S359] and R03's colour list.
- **Not now:** red and hot pink [S359] have no fabric slot. They would need new materials, so they are deferred.
- **How the variant is built:** `VARIANTS = {"Kit_TaskChair_Navy": {"Prop_FabricCubicle": "Prop_FabricNavy"}}` [BA:60-64]. The variant gets new fbx/json GUIDs and copies the sidecar, including its pile block.

### 4.4 Budgets
| LOD | Tris | Note |
|---|---|---|
| LOD0 | **≤ 5,200** | comparables: QueenAnne 4,918; Bergere 4,213; Sofa3 5,918 [sidecars] |
| LOD1 | **≤ 1,800** (`LODS = (0.35, 0.09)`) | must not exceed today's 1,792: chairs are the Office's biggest triangle item [WGL:114] |
| LOD2 | **≤ 450** | seat, back, column and star silhouette; casters as one lump each |

- **Switch distances:** `lodDistances = [6, 12, 30]`. With a size of ~0.93 m, these switch at 9.9 % / 5 % / 2 %, the same as today's default LOD1 point (10 %) and cull (2 %) [KI:102-103]. LOD2 is new.
- **Anchors and support:** `sit` (0, seat_y, 0.45); `back_top` (0, ~0.30, 0.93); support `seat` 0.40 × 0.38. Names unchanged [SCC].
- **Collider:** one box = the real bounds; chairs are static, not pushable [TCPY; SYN §5.1].
- **Pile:** unchanged (Seat, mass 1, office90s, U/I/S/B, tolerance 0.45) [SCC]. Tags: office, seat.
- **Docstring:** drop the brand names. Say "1990 open-plan task chair, c. 1985–93".

## 5. Kit_TaskChair_Armed: today's black chair, renamed (NEW name; report it to 关卡设计)
- **Module:** new `assets/task_chair_armed.py`, a copy of today's `task_chair.py` with only these changes:
  - `NAME = "Kit_TaskChair_Armed"`;
  - `LODS = (0.40, 0.10)` replaces `LOD1`, giving 4,484 / ~1,792 / ~450 tris;
  - `kit.meta["lodDistances"] = [6, 12, 30]`;
  - the brand names leave the docstring;
  - **no `kit.pile(...)`.** This keeps office chairs a minority in piles [01:181] and the variant a station-only "odd chair".
- **Unchanged:** geometry, slots (`Prop_FabricChair`, `Prop_PlasticBlack`, `Prop_Chrome`), collider, anchors, support.
- **GUIDs:** new GUIDs for the `.fbx.meta` and `.json.meta`, written by hand following main's format [COMMON rule 14].
- **Naming style:** `Kit_<Base>_<Variant>`, as in Kit_ClubChair_Oatmeal, Kit_Sofa3_Charcoal and Kit_DoorLeaf_SteelLite.

## 6. Office dressing (FrontRoomsOfficeKit.cs, visual-owned): shares, deterministic, rng stream untouched

1. **Room lot:** one per Dress call, from the seed, using no `Rng` draw. Let `h = Mix((uint)seed)`.
   - **70 %** of rooms get `Kit_TaskChair` (grey) and **30 %** get `Kit_TaskChair_Navy`.
   - Why: Sears sells a chair in the colour you "state" per order [S359]. An office bought chairs in lots, so one room shares one colour.
2. **Odd chair:** per station, `Mix(h, (uint)station) < 0.15 · 2³²` gives **`Kit_TaskChair_Armed`**. That is a 15 % share, "a small share".
3. **Keep every existing `rng` call, in today's order:** `Chance(.85)`, then the chairZ draw, then x, then yaw [OK:959-964]. The choice is an integer hash of (seed, station node), so the positions, rotations and every later station come out bit-identical. Only the mesh changes.
4. **No statics.** Dress jobs are sliced and interleave across frames [OK header]. Pass the lot down as a parameter (Solve → the pod and row builders → BuildStation), or store it as a field on the job's build.
5. **Fallback:** if a variant kit is missing (`FrontRoomsKitLibrary.Has`), use `Kit_TaskChair`, since missing assets are skipped [OK:42].
6. **Piles and the torchiere:**
   - The armless grey and navy chairs join the pile library automatically; Armed does not (§5).
   - Pile layouts per seed change, because the bounds are new and there is one more Seat piece, but they stay deterministic per seed [FP header].
   - The torchiere gets no dressing change.

## 7. Notes for the package (to 关卡设计 and the merge)
- **New kit names:** `Kit_TaskChair_Armed`, `Kit_TaskChair_Navy`.
- **Frozen names, content replaced:** `Kit_Torchiere`, `Kit_TaskChair`.
- **Map-visible changes to Kit_Torchiere:**
  - height 1.852 → 1.689, minCeiling 1.902 → 1.739;
  - footprint 0.362² → 0.39²;
  - **it now has a collider**, so the player can no longer walk through it.
- **Effect on L0_WaitingRoom_4x3.** Its stored x of 0.291 still leaves 16 mm between the bowl and the wall face (was 30 mm) [ME AgainstWall: half-wall 0.08 + gap 0.03 + half-footprint]. No clash. Regenerating the sample restores the gap; that is 关卡设计's call and file.
- **Kit_TaskChair** keeps its bounds within ±2 cm.
- **No RenderSetup hunk** unless the §4.3 gate fails.
- **Follow-ups for other owners:**
  - 22_era_lock rows 40 and 76: descriptions, plus a row for Armed;
  - re-render K04 and K40 and refresh R03/R39 (平面视觉 · 印刷与演示);
  - VL specs VL308–VL311 only. Suggested split:
    - VL308: torchiere hero + three-view;
    - VL309: armless grey + navy hero;
    - VL310: armed variant + office frames showing the share;
    - VL311: pile + L0 waiting room, plus a LOD switch strip.

## 8. Open risks
- **Chair against the panels.** The FabricCubicle chair may merge with the panels (gate in §4.3).
- **Brass smoothness.** At 0.6, Prop_Brass may read satin next to chrome. Raising it is a global change, outside W2.
- **Geometry vs. the card.** The 64-side terraced bowl is the costliest part. If LOD0 runs over 4,500, cut the base to 40 sides before the bowl.
- **The halogen choice.** Halogen plus frosted glass rests on [GEN] heat reasoning. The literal acrylic/150 W reading of Sears N is the documented alternative (§3.1).

## 9. Source key
- **S319 / S356 / S358 / S359:** Sears Annual Home Catalog 1993, p.319 (n0332), p.356 (n0369), p.358 (n0371), p.359 (n0372). Files are `Research/week02/kit-references/sears1993/sears1993_n0332.jpg`, etc., with crops in `sears1993_crops/`. Internet Archive `sears-home-1993` [kit-references/SOURCES.md].
- **R39 / R03 / K40 / K04:** Figma nodes 2350:1174, 2349:907, 2328:1066 and 2325:947 (sections 2349:852 REAL vs MODEL and 2324:852 THREE-VIEW + ERA).
- **ERA:** `research/office_and_film/22_era_lock.md`.
- **SYN:** `10_synthesis.md`.
- **FILM:** `02_film_shots.md` (F02, T07).
- **01:** `01_film_production.md` (line 181: office pieces are a minority accent in piles).
- **TPY / TCPY:** `Tools/Blender/frontrooms_kit/assets/torchiere.py` / `task_chair.py`.
- **KL / BA:** `kitlib.py` / `build_asset.py`.
- **SCT / SCC:** `Assets/Resources/Props/Models/Kit_Torchiere.json` / `Kit_TaskChair.json`.
- **RS:** `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs`.
- **KI:** `FrontRoomsKitImporter.cs`.
- **Office scripts:** OK `Assets/Scripts/Office/FrontRoomsOfficeKit.cs`; FP `FrontRoomsFurniturePile.cs`; KLIB `FrontRoomsKitLibrary.cs`.
- **Map files:** LD `Assets/Editor/FrontRoomsMap/FrontRoomsLevelDesigner.cs`; ME `FrontRoomsModuleEditing.cs`; MU `Assets/Scripts/FrontRoomsMap/FrontRoomsModuleUnits.cs`.
- **WGL:** `research/webgl/03_measured_budgets.md`.
- **VCT:** `Documentation/VISUAL_CHAT_TASKS.md`.
- **COMMON:** `Documentation/Takeover_2026-10-18/briefs/COMMON.md`.
