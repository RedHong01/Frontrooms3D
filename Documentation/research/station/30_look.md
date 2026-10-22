# 30 · Security desk: look, kit inventory, light plan (游戏视觉, 2026-10-22)

Workflow `station-look`, stage 1 (look and directions, no Unity). Written 2026-10-22 by 游戏视觉.
- **Status: PROPOSAL, paper only.** Every number comes from the kit sidecars, the code or the light math in `data/light_math.py.txt`. The blockout stage (clone `W/proj_station`) proves them with `20_layout.md` §8 and the extra checks in §6 here.
- **Read first:** `00_brief.md`, `01_research.md`, `02_refs.md`, `20_layout.md` (G1–G10, plans W, D, H), `05_visual_answers.md`; tutorial `10_proposal.md` §4 and `20_requests.md` V1–V6 (inputs, not re-opened); `LEVEL_MODULE_SPEC.md`.
- Nothing was built, merged or downloaded. Unity was not opened. The only files written are this one, `data/light_math.py.txt` and the visual half of `10_directions.md`.

**Marks.**
- **SP**: a starting value to tune in blockout or playtest.
- **ESTIMATE**: my judgement or trade practice, not a source.
- **[memory]**: unverified recall.
- **For 叙事**: a layout or story change that is 叙事设计's to accept; I only propose it.

---

## 0. Short answer

**The three visual calls:**

| # | Call | Settled answer |
|---|---|---|
| 1 | Light leaving the room (20 §2.2) | **(b) in every module, made by the plan: E1 looks at the set's face** (W′ 24°, D′ 21°, H 0° off the screen axis). The TV's light physically cannot reach a doorway 8–11 m away in URP (0.000–0.005 at E1), and even at 6 m it is a 0–15 % tint under the corridor's own lamps. What reads from the corridor is the inside: a Dim/Off room with one lavender-blue patch that changes at every card cut, framed by E1. **No spill light** (c); kept only as a fallback if the squint test fails. §4.1 |
| 2 | The key lit by the screen (G4) | **Measure from the screen centre, not the set's corner:** the board goes on a wall perpendicular to the set's wall, 30–45° off the screen axis (f between s and 1.73 s), ≤ 2.0 m away, inside the 100° inner cone. **Plan W's corner set fails** (69°, E 0.13, a third of a Dim lamp's own wash). Fixed plan **W′** puts the set on a wall bracket 1.09 m off the corner wall: 38°, 1.87 m, E 0.64. Plan D already passes (40°, 1.97 m, E 0.63). Consequence: a literal G5 cannot hold with G4 (§4.2). |
| 3 | Annunciator colours (01 §2) | **Green POWER lamp only, steady, emissive, no realtime light.** Neutral putty cabinet (not fire red), zone and TROUBLE windows as off-white label tiles that are never lit, so colour shows only on the one lamp. It stays lit after the take. §4.3 |

**Directions (visual side; full text in `10_directions.md`):** I agree with 叙事: **B for Floor 1 and Level 0 floors, A for Office floors, C later**, with two plan amendments for 叙事 to accept: **W′** (set on an E-wall bracket, E1 in the W wall's south cell) and **D′** (E1 in the south wall's middle cell, on the set's face, behind the guard's chair).

**Kits:** 7 station objects exist as they are, 2 more exist and only get a hero pass or a colour (the set, the guard's chair), 5 need a variant, 21 rows are new. 5 new kits and 1 variant carry the grammar (counter, TV bracket, tandem beam, key switch plate, annunciator; the laminate credenza); the rest tell the story. Hero tier about 12 days for both modules, or a blockout tier of about 2 days that matches V1's approved 1-day scope plus the five grammar pieces (§1.4).

---

## 1. Kit inventory against the research

Sidecars: `Assets/Resources/Props/Models/<name>.json` (main, read 2026-10-22). Sizes are W × H × D in metres from `boundsMin/boundsMax`; tris are LOD0 / LOD1. Budgets for new kits are hero LOD0 / LOD1 / LOD2, in the interactables format (`research/interactables/10_spec.md` §1.8, §4.2), LOD distances 3 / 8 / 40 m for things the player handles and about 8 m for LOD1 on furniture (`LEVEL_MODULE_SPEC.md` §8). Props ≤ 0.3 m cast no shadows.

**Tier:** **G** = carries the station grammar G1–G10 (needed for V1 to work); **S** = story props (V1 hero pass); **P2** = Phase 2.

### 1.1 The beacon and its mounts

| # | Called for | Research size | Kit today | Status | Action | Budget | Tier |
|---|---|---|---|---|---|---|---|
| S01 | The beacon TV (01 §2; G3, G4) | the guard's own set; 21 in flat-square approved | `Kit_CRTTV` 0.62 × 0.50 × 0.49, 1,806 / 818, no LOD2; `screen` anchor (0, 0.284, 0.245) | exists | Hero pass for the room's hero: bezel and mask, tuner and volume knobs, rod socket, rear vents; same footprint and anchors. Ships first with today's mesh | 9,000 / 3,000 / 400 | S |
| S02 | Wall bracket (20 §4.1; 02 §3.8 "high on a cart or a wall bracket") | set "2.0 m up" (plan W) | none | **new** | `Kit_TVWallBracket`: wall plate, a 0.56 × 0.45 platform tilting 0–20° down, a strap over the set, a `top` support. Over a walkway the set's **bottom ≥ 2.10 m** (the Relay walks at ≤ 2.05 m, `LEVEL_MODULE_SPEC.md` §7), so its top is 2.60: **Standard (2.9 m) rooms only**; in a Low room (2.4) it cannot hang over a walk line | 3,000 / 1,000 / 120 | G (B) |
| S03 | TV cart (02 §3.8; P97) | AV carts 34–48 in | none | new, optional | `Kit_TVCart` (two steel shelves, casters): a Low-ceiling Waiting variant and plan H | 4,000 / 1,400 / 200 | P2 |
| S04 | Low cabinet under the set (plan D) | about 30 in | `Kit_Credenza` 1.52 × 0.76 × 0.46, cherry + brass: the store's merchandise look | **variant** | `Kit_Credenza_Laminate`: same mesh, `Prop_LaminateBeige` for `Prop_WoodCherry`, `Prop_Chrome` for `Prop_Brass` | 1,108 / 738 | G (A) |

### 1.2 Counter, desk, seating

| # | Called for | Research size | Kit today | Status | Action | Budget | Tier |
|---|---|---|---|---|---|---|---|
| S05 | Reception / security counter (01 §1; G7; plan W `\|`) | **transaction ledge ~42 in = 1.07 m** (guides 42–48 in); **guard's work surface 29–30 in = 0.74–0.76 m**; ledge depth 10–14 in, desk depth ~30 in + ledge; single-guard length 6–10 ft; knee space ≥ 27 × 30 × 19 in; **no low section** (a 1990 desk is pre-ADAAG) | none. `Kit_OfficeDesk` gives only the work surface | **new family** | `Kit_StationCounter_Run120`: 1.20 m run; visitor ledge 1.07 high × 0.30 deep on a 0.76-deep base; staff work surface 0.74 with the knee space; a dead door-release button and an intercom master under the ledge. `Kit_StationCounter_End`: end panel, staff side open (G7). `Kit_StationCounter_Ledge120`: the ledge alone against a wall (plan D's sign-in counter; a guard booth's shelf in C). Two runs + an end = 2.45 m, inside 6–10 ft | Run 3,500 / 1,200 / 150; End 1,200 / 400 / 60; Ledge 1,800 / 600 / 80 | G |
| S06 | Guard desk (plan D `G`) | 29–30 in top | `Kit_OfficeDesk` 1.524 × 0.741 × 0.762 (top 29.2 in), 1,366 / 560 | **exists, matches** | — | — | G (A) |
| S07 | Guard's chair (`g`) | — | `Kit_TaskChair` 0.62 × 0.95 × 0.65, 4,484 / 1,792, black fabric | exists; colour variant optional | `Kit_TaskChair_Navy` (`Prop_FabricNavy`), matching the jacket and the research's navy | — | S |
| S08 | Tandem seating, two short rows (G10; 02 §3.8) | 3 seats on a steel beam (seat pitch ~0.6, seat 0.45 high: ESTIMATE) | none. `Kit_Sofa3*` and `Kit_ClubChair*` are exactly the upholstered showroom rows A24 owns | **new** | `Kit_TandemBeam3` 1.80 × 0.80 × 0.70: black steel beam on two T-legs, vinyl seats and backs. `Kit_TandemBeamTable`: a beam end table with a built-in ashtray (period) | 6,000 / 2,000 / 250; table 2,000 / 700 / 100 | G (B) |
| S09 | Visitor chairs, 4 (plan D `h`) | — | `Kit_LadderChair` (oak, domestic), `Kit_TaskChair` (casters) | **new** | `Kit_GuestChairSled`: chrome sled base, navy vinyl seat and back. Useful to the Office kit too. Blockout uses `Kit_TaskChair` | 3,500 / 1,200 / 150 | S (A) |

### 1.3 The take cluster: key, alarm, paper

| # | Called for | Research size | Kit today | Status | Action | Budget | Tier |
|---|---|---|---|---|---|---|---|
| S10 | **The stair key's own board**, a device on the alarm panel (01 §2, §5) | key "about 1.4 m up" (20 legend) | `Kit_KeyHookBoard` 0.30 × 0.40, y 1.25–1.65, 8 brass cup hooks at 1.42 / 1.54, dark wood, 4,392 (budget 4,000 / 1,400 / 150) | **new** (same host convention) | `Kit_KeySwitchPlate`: a 0.15 × 0.22 steel plate on a 4 in box, **one** hook on a plunger switch, `key_hook` at 1.42 (same as the board's `hook_6`, so the D1.6 hung pose and the KeySpot height carry over), a ½ in EMT stub, a `plate_sign` anchor above it. Tags `interactable`, `key_host`, `wall_decor`; no collider; LOD 3 / 8 / 40. Why not the board: seven empty hooks on a domestic board read "keys are missing"; one hook on a switch reads "this key is wired" | 3,500 / 1,200 / 150 | G (blockout may use `Kit_KeyHookBoard`) |
| S11 | The stair key | — | `Kit_KeyRing` + `Kit_Key_Zone(_Nickel)` + `Kit_KeyTag_*`: 2,000 + 2,400 + 1,200 | exists (D1.6 assembly) | Tag string `STAIR A` is a placeholder (叙事) | 5,600 | G |
| S12 | **Fire-alarm annunciator** (01 §2) | lamp windows, silence / reset / lamp test, locked glass door (P13, P46, P47) | none | **new** | `Kit_FireAnnunciator` 0.38 × 0.50 × 0.10, centre 1.50 m: putty enamel cabinet, locked glazed door with a key cylinder, 8 zone windows + 1 TROUBLE window as label tiles, a Ø 10 mm green POWER jewel, three toggles behind the glass. §4.3 | 6,000 / 2,000 / 200 | G |
| S13 | The wire from the key to the panel (the fiction of the take, 01 §5) | — | `Kit_ConduitEMT` (1.0 m, 576, budget 600 / 220 / 24), `Kit_ConduitStrap` | **exists** | 1–2 runs from the plate's box to the cabinet, 3 straps | ~2,000 | G |
| S14 | Key cabinet, the building's keys (01 §2; §6 `KEY CONTROL`) | shallow steel wall cabinet, beige or grey, numbered hooks | `Kit_KeyCabinet` 0.719 (door open) × 0.46 × 0.084, y 1.24–1.70, almond, 24 hooks, 6,804 | exists; **variant** | Door art `KEY CONTROL / ALL KEYS MUST BE SIGNED OUT` (平面视觉). New `Kit_KeyBunchDressing` (one merged static key + tag) on ~12 hooks: twelve D1.6 hero keys would cost 67k tris | 6,804 + 12 × 300 / 100 / 0 | S |
| S15 | Sign-out sheet; Daily Activity Report (01 §2, §6) | clipboard 9 × 12.5 in [memory] | none | **new** | `Kit_Clipboard`: hardboard, steel clip, 3–5 sheets; Wall (on a nail by the cabinet) or DeskTop. Sheets from one paper atlas `Prop_StationPaper` (平面视觉's art, 叙事's strings); EXAMINE-readable | 1,800 / 600 / 60 | S |
| S16 | Visitor register (01 §2, §6) | bound book | none | **new** | `Kit_RegisterBook`: an open ledger 0.43 × 0.30, a pen on a bead chain | 2,500 / 800 / 80 | S |
| S17 | Post-orders binder | three-ring binder | `Kit_Binders` 0.30 × 0.29 × 0.28, 360 | **variant** | spine `POST ORDERS` from the atlas | 360 | S |
| S18 | Key telephone (01 §2) | 1A2-era key set with line lamps | `Kit_DeskPhone` 0.20 × 0.09 × 0.33, 966 / 290, putty | **exists** (an electronic key set of 1983 reads the same, P42) | Line lamps dark (§4.3) | 966 | S |
| S19 | The empty radio charger (01 §2) | drop-in desk charger | none | **new** | `Kit_RadioCharger`: black, one empty well, a dark charge lamp, a spare battery beside it | 2,000 / 700 / 80 | S |
| S20 | Intercom, door-release button (01 §2) | under the ledge | — | in S05 | parts of the counter run | — | S |
| S21 | Plates and signs (01 §6: `STAIR KEY` plate, `NO SMOKING`, the EXIT door's business-hours plate) | engraved plastic | `Kit_DoorSign` 0.254 × 0.076, 664, `Prop_SignEngraved` | **variant** | Art from 平面视觉; a 0.30 × 0.10 size for the two-line key plate | 664 each | G (key plate) / S |
| S22 | Tent card `PLEASE SIGN IN` | — | none | **new** | `Kit_TentCard` | 300 / 100 / 0 | S |

### 1.4 The guard's leftovers, dressing, later

| # | Called for | Kit today | Status | Action | Budget | Tier |
|---|---|---|---|---|---|---|
| S23 | **Jacket on the chair back** (01 §2; 02 §3.2, §3.10 false occupancy) | none | **new, hero** | `Kit_JacketDraped`: navy serge, cloth-baked over `Kit_TaskChair`'s back (top 0.953), blank shoulder patch. It is the silhouette seen from E1 in D′ | 8,000 / 2,500 / 300 | S |
| S24 | Coffee cup | none | **new** | `Kit_CoffeeCup`: 8 oz foam cup or a mug (`Prop_Ceramic`), a cold coffee disc and a ring stain | 1,200 / 400 / 0 | S |
| S25 | Ashtray under `NO SMOKING` | `Kit_Urn` is a decor urn | **new** | `Kit_SmokingStand` (B: floor stand ~0.6 m, chrome and a black bowl, butts); `Kit_Ashtray` (A: glass, on the desk) | 2,500 / 800 / 100; 1,500 / 500 / 0 | S |
| S26 | The watchclock is gone: its empty hook (01 §2) | `Kit_KeyHook` is a 7 mm cup hook | **new** | `Kit_HookHeavy`: a cast coat hook with a cut strap stub | 1,200 / 400 / 0 | S |
| S27 | Newspaper, lunch bag | none | new, optional | `Kit_Newspaper`, `Kit_LunchBag` (paper from the atlas, no real titles) | 1,500 / 500 / 0; 800 / 300 / 0 | P2 |
| S28 | Desk lamp (tutorial §4.2 "a desk lamp") | `Kit_TableLampPleated` is a domestic pleated lamp | new, optional | `Kit_DeskLampSwingArm`, **off**: he watched in the dark (no light, G3) | 4,000 / 1,400 / 150 | P2 |
| S29 | Magazine rack (B) | none | **new** | `Kit_MagazineRack`: wall, 3 pockets, blank-title covers from the atlas | 3,000 / 1,000 / 120 | S (B) |
| S30 | Wall clock over E1 | `Kit_WallClock` Ø 0.32, 538 | **exists** | — | — | S |
| S31 | Trash bin, paper stack, coat stand | `Kit_TrashBin`, `Kit_PaperStack`, `Kit_HatStand` | **exists** | — | — | S |
| S32 | Dim troffers; the warm lamp at E1 | map fixtures | **exists** | module lamp modes, §3 | — | G |
| S33 | CCTV monitor, a still (02 §3.7) | `Kit_CRTMonitor` is a beige PC monitor | new, optional | `Kit_CCTVMonitor9`: 9 in, metal case | 3,500 / 1,200 / 150 | P2 |
| S34 | Station plan beside the key (tutorial §5.4) | `Kit_EvacPlacard` + `Lens` | variant | art (G3) | 1,764 | P2 |
| S35 | Watchclock key stations on corridors (20 §6) | none | new | `Kit_WatchclockStation`: hinged box, chained key, number plate | 2,500 / 800 / 100 | P2 |
| S36 | Direction C posts | none | new | `Kit_WallPhone` (warden), `Kit_TimeClock` + `Kit_TimeCardRack` (receiving), `Kit_DockButtonStation`; the booth shelf is S05's Ledge; `Kit_WindowFrame_Alu` exists for a booth | 2,000–3,500 each | P2 |

**Counts:** exists as is S06, S11, S13, S18, S30, S31, S32, plus S01 and S07 (usable today); variants S04, S14, S17, S21, S34, S07 (colour); new S02, S03, S05, S08, S09, S10, S12, S15, S16, S19, S22–S29, S33, S35, S36 (S20 lives inside S05).

### 1.5 Room budgets (and one ask)

`LEVEL_MODULE_SPEC.md` §8 caps a dressed room at **LOD0 ≤ 60k (Level 0) / ≤ 120k (Office)**, renderers ≤ 120, colliders ≤ 40.

| Module | LOD0 tally with the hero kits | Against the cap |
|---|---|---|
| Station · Waiting (B, plan W′) | ~94k: TV 9.0k, bracket 3.0k, counter 8.2k, two beams + table 14.0k, key plate 3.5k, key 5.6k, annunciator 6.0k, conduit 2.0k, cabinet + dressing 10.4k, two clipboards 3.6k, register 2.5k, chair 4.5k, jacket 8.0k, smoking stand 2.5k, magazine rack 3.0k, the rest 7.7k | **over Level 0's 60k** |
| Station · Desk (A, plan D′) | ~87k: as above without the bracket, beams and rack, plus four guest chairs 14.0k, sign-in ledge 4.8k, desk, credenza, ashtray | inside Office's 120k |

- Renderers: about 30 placed kits with LOD groups, ≤ ~90. Colliders: about 10 (counter, beams, chairs, stand, bin). Both fit.
- **Ask to 关卡设计 (budgets are map-owned):** treat the station, one room per floor, as a hero room with **≤ 100k LOD0 in both themes**. Without that, B drops to ~64k by removing the key cabinet, keeping today's 1.8k TV, lighter beams and jacket. That loses the "one key apart from the cabinet" read (01 §2).

### 1.6 Cost (ESTIMATE; one day = a kit through the Blender pipeline with sidecar, LODs, colliders, check render)

| Tier | What | Days |
|---|---|---|
| **Blockout** (what V1's approved 1 day can stretch to) | Existing kits + blockout-grade S02, S05, S08, S10, S12 (one material, LOD0 only, ~1/5 budget) + S04; both modules authored; §8 checks | **~2** |
| Grammar kits at hero spec | S02 0.5, S04 0.1, S05 1.0, S08 1.0, S10 0.5, S12 1.0 | 4.1 |
| Story kits at hero spec | S01 0.75, S07 0.1, S09 0.75, S14 0.35, S15 0.25, S16 0.5, S17 0.1, S19 0.5, S21 0.1, S22 0.1, S23 1.0, S24 0.25, S25 0.75, S26 0.25, S29 0.5, paper atlas hook-up 0.5 | 6.75 |
| Two modules, checks, renders | | 1.5 |
| **Hero total, both modules** | | **~12.4** |

By direction: **B alone ~10.7 days**, **A after B ~2 days** (S04, S09, the desk ashtray, module), **A alone ~9.4**, C 1.5–3 days per extra post (Phase 2).

---

## 2. Materials and colours

**Rule:** the station adds **no new shell surface**. It is a room of its floor, so walls, carpet, ceiling and troffers stay the zone's (`Resources/Surfaces`). All the difference is in the props and the light. Texture means below are the mean colour of each slot's base map (sRGB, measured 2026-10-22).

**Colours kept out on purpose:**
- the red family (ALARM ZONE card, RELAY §6);
- amber (the TROUBLE / sanctuary lamp, RELAY §10);
- pale yellow-green (the glow ink, W6: one colour, one meaning);
- cherry and walnut veneer and beige upholstered armchairs (the furniture store, A24);
- mauve with teal as a pair (the decade's stereotype, 01 §1): at most one small accent of one of them.

### 2.1 B · After-Hours Reception (Level 0 shell)

| Part | Slot (exists unless marked) | Colour |
|---|---|---|
| Shell | `L0_Wallpaper` (hard-edge paper), `L0_Carpet` #897853, `L0_Ceiling` #CCC6B4, `Troffer_Lens`, `Cove_Base` | the floor's own |
| Counter front | **new `Prop_LaminateGrey`**: colour-through grey laminate (P36) | #8E8E88 SP |
| Counter ledge top / edge | `Prop_WoodOak` #9A6939 / `Prop_PVCEdge` | oak, dark edge |
| Staff work surface; toe base | `Prop_LaminateBeige`; `Prop_Vinyl` #141312 | |
| Tandem beams | `Prop_SteelBlack` #1D1D1D; seats **new `Prop_VinylNavy`** (`Prop_Vinyl`'s maps, navy tint) | #26324A SP, smoothness 0.45 |
| TV bracket; the set | `Prop_SteelBlack`; `Kit_CRTTV`'s own black plastic | |
| Annunciator; key plate | `Prop_SteelPutty` #A69E88, `Prop_Glass`, `Prop_LEDGreen`; conduit `Prop_Aluminium` | putty |
| Key cabinet | `Prop_SteelAlmond` | almond |
| Jacket; guard's chair | `Prop_FabricNavy` #222F4A | navy |
| Smoking stand; magazine rack | `Prop_Chrome`, `Prop_SteelBlack` | |
| All paper | **new `Prop_StationPaper`** atlas (平面视觉 art, 叙事 strings) | paper white #DCD8CC |

**Read:** yellow paper and tan carpet (the floor) / grey and oak (the counter) / black steel and navy (seats, jacket) / putty and almond (the building's equipment) / one black set, and its lavender-blue light.

### 2.2 A · The Post (Office shell)

| Part | Slot | Colour |
|---|---|---|
| Shell | `Office_Wall` #B5AE9D, `Office_Carpet` #535A61, `Office_Ceiling` #CDC9BE, `Troffer_Lens`, `Cove_Base`; module columns **None** (a column would cut the sight lines) | the floor's own |
| Guard desk | `Kit_OfficeDesk`: `Prop_LaminateBeige`, `Prop_SteelBrown`, `Prop_SteelPutty` | beige and putty |
| Credenza under the set | `Kit_Credenza_Laminate` (S04) | beige laminate, chrome pulls |
| Guest chairs | `Prop_Chrome` sled, `Prop_VinylNavy` | navy |
| Sign-in ledge | `Prop_LaminateGrey`, `Prop_WoodOak` top | |
| Annunciator, key plate, cabinet, jacket, paper | as B | |
| Desk ashtray | `Prop_Glass` | clear |

**Read:** greige and grey-blue (the floor) / beige and putty steel (the post's furniture) / navy (chairs, jacket) / the same set and light. The Office grade (cooler) does the rest.

### 2.3 C · The Night Posts (Phase 2)

- **Warden station** (Office palette): a small steel desk (`Kit_OfficeDesk`), a putty wall phone (`Prop_PlasticPutty`), the posted plan (`Kit_EvacPlacard`).
- **Receiving desk** (back of house): `Prop_Plywood`, `Prop_PinePallet`, `Prop_SteelBlack`, a time clock in grey enamel, galvanised dock-door steel (new slot). Strictly no showroom (A24).
- **Guard booth:** S05's ledge under `Kit_WindowFrame_Alu`.

---

## 3. Light plan

### 3.1 The TV light (V3, as 游戏视觉 builds it for the station)

V3 is our own request row; these are its numbers for the station set.

| Property | Value | Why |
|---|---|---|
| Type, place | Spot at the `screen` anchor, 0.02 m out, along the screen normal | |
| Cone | **inner 100°, outer 160°** (SP) | Full light out to 50° off the axis, 48 % at 60°, nothing past 80°. G4's "≤ 45°, max 60°" becomes the light's own shape, and nothing behind the screen plane is lit (a CRT does not light its own wall) |
| Range | **URP `range` 9 m** (SP) | URP fades a light to zero at its range: `(1 − (d/r)⁴)²`. With range 6 the set keeps 27 % at 5 m and nothing at 6 m, so "about 6 m" needs range 9 (64 % kept at 6 m) |
| Intensity | **I = 4.0 × √(Y / 0.140), clamped 2.0–5.6** (SP); Y = the frame's mean linear luminance | The ad (`FrontRooms_Ad_01.mp4`, sha1 8bde2101) is five still cards a loop: Y 0.116, 0.266, 0.092, 0.118, 0.037. A linear map swings the light 7.2× and leaves the key at 14 % on the dark WTCZ card. The square root keeps every cut readable (steps of 1.1–1.8×) and keeps the key above half its mean light |
| Colour | The frame's mean linear colour, normalised to 1, blended 35 % toward the set's white (0.86, 0.91, 1.00) (SP) | The loop's mean is lavender (0.96, 0.77, 1.00). Its cards are violet, mauve-pink, a dark navy with yellow type, violet, and WTCZ blue: against the troffers' (1, 0.96, 0.88) it reads as "a TV". US sets of the period often ran a cool white [memory, unverified] |
| Driver | **A track baked offline from the ad** (10 Hz: 160 samples of colour and Y per 16 s loop), indexed by the VideoPlayer's time; τ 0.12 s, Reduce flashing 0.5 s | No GPU read-back. WebGL has no async read-back, and a synchronous one stalls. The same curve on every platform, and photosafety is checked offline on the exact curve. Re-bake when the ad changes |
| Photosafety (measured) | 5 cuts a loop, ≥ 2 s apart: at most **1 change a second** (limit 3); the largest step is +77 % (the loop wrap) | WCAG 2.3.1 / RELAY §9.5 |
| Collapse (V2) | The light follows the dot to zero within ~0.35 s at τ 0.12 | One change |
| Shadows | Desktop: **Soft**, high resolution tier (1024 in the 4096 atlas), as V3. **WebGL: Hard, 512** (amends V3's "none on WebGL", §3.5) | |
| Gating | **On whenever the beacon plays**, up to one chunk (24 m) from the player; shadows within `shadowRadius` 9 m | The map's 16 m lamp radius fades from 13 m, but the reveal is seen from the last junction, 12–15 m from the set. One light per floor |

Implementation notes for V2/V3:
- The station set must not pause its video beyond `FrontRoomsScreenVideo.DefaultVisibleDistance` (18 m), or its light track and the beacon would desync.
- The light is the set's, owned by `FrontRoomsScreenVideo` (or a sibling), not a map fixture. It never takes part in W1: the lamps dip, the TV doesn't.

### 3.2 Reference levels (same units throughout; `data/light_math.py.txt`)

| Source | Where | E |
|---|---|---|
| Steady troffer (I 5) | floor below / at 1.5 m / near wall at eye height | 0.61 / 0.42 / 1.01 |
| Dim troffer (× 0.42) | same | 0.26 / 0.18 / 0.42 |
| Scene fill (soft ambient direction + trilight) | anywhere; the stairwell measured it (`stairwell/10_directions.md` §1.6) | ~0.05–0.13 |
| TV, mean card (I 4), on its axis | 1 / 2 / 3 / 4 / 5 / 6 / 8 m | 4.0 / 1.0 / 0.43 / 0.23 / 0.13 / 0.07 / 0.01 |

The TV's wash tops a Dim pool out to about 3 m and a Steady pool only inside about 2 m. Beyond 4 m it is a tint.

### 3.3 Plan W′ (B, Station · Waiting), lamp cells

3 × 2 cells, Standard 2.9 m. The plan itself is in §5.1.

| Cell (col, row from SW) | Holds | Lamp |
|---|---|---|
| (0, 0) | E1 (W wall), the warm lamp | **Steady**: G1's warm lamp and W1's anchor |
| (1, 0) | the escape line to E1, ashtray stand | Dim |
| (2, 0) | staff strip south end, E2, key cabinet | Dim |
| (0, 1) | magazine rack, the back of the rows | Dim |
| (1, 1) | the rows' front, the wash | **Off** |
| (2, 1) | the set, the key plate, the annunciator | **Off**: the key is lit by the set alone |

- **W1:** 1 Steady + 3 Dim = 4 live lamps, inside the room (W1 widens only below 2, `FrontRoomsWarnLamps.cs`). Off lamps have no fixture and never dip.
- **Before the take:** brightest are the screen, then the key plate area (0.64), the counter top (0.45), the north-gap floor (0.31), the Dim pools (0.18–0.26), the rest (~0.1).
- **After the take:** the plate falls to ~0.12. The brightest shapes from the board are E1's Steady cell (0.6+) and the corridor beyond E2, which is G8 on paper. Blockout check 1c proves it.

### 3.4 Plan D′ (A, Station · Desk), lamp cells

3 × 3 cells, Office (Standard). The plan is in §5.2.

| Cell | Holds | Lamp |
|---|---|---|
| (1, 0) | E1 (S wall middle), the warm lamp | **Steady** (Office 5.5) |
| (0, 0), (2, 0) | entry sides | Dim |
| (0, 1), (0, 2) | sign-in ledge, guest chairs | Dim |
| (1, 1) | guard desk and chair | Dim |
| (2, 1) | E2, the escape line | Dim |
| (1, 2) | between the set and the desk | **Off** |
| (2, 2) | the set, the key plate, the annunciator | **Off** |

- W1: 1 Steady + 6 Dim.
- The set on the credenza (screen 1.04 m up) washes the floor ahead (0.36 at 2 m), the jacket's front (0.24) and the ceiling over itself (0.17). The desk top sits almost edge-on to it (0.04), so the desk reads as a silhouette.

### 3.5 Plan H (2 × 4, end-on; long rooms only)

- The set on a bracket on an end wall, facing down the hall. E1 is in the far end wall, on the axis (11 m).
- The key on a side wall at s 1.1–1.3, f 1.2–1.5. E2 beside the key on the other side wall.
- E1's cell is Steady; the two cells at the set's end are Off; the rest are Dim.

### 3.6 Lamp and shadow budgets

**Map rules:** lamps lit within `lightRadius` 16 m (3 m fade), shadows within `shadowRadius` 9 m, about 1 lamp in 3 shadowed by hash (`FrontRoomsMapWorld.cs`). A module's Off cell has **no fixture**.

| | Realtime lights | Shadowed (average) | Against an Auto room of the same size |
|---|---|---|---|
| W′ | 4 troffers + TV = 5 | ~1.3 + 1 | 6 lights, ~2 shadowed: **−1 light, +0.3 shadow** |
| D′ | 7 troffers + TV = 8 | ~2.3 + 1 | 9 lights, ~3 shadowed: **−1 light, +0.3 shadow** |

**WebGL (separate tier; desktop untouched):**
- Lights: net zero or fewer, so the 32-light cap is unaffected.
- **Shadow on the TV light: Hard, 512.** Unshadowed, the light passes through walls. In W′ it would put 0.18 (mean card) on the floor of the room north of the station, against that room's 0.4–0.6: a cold patch that steps at every cut, beside a wall. That is a beacon through a wall, which the sound rule forbids for the same reason. WebGL already renders spot shadows for one map lamp in three, and the station's Off cells remove lamps.
- To measure in the WebGL tier pass (per-draw cost of one more spot shadow pass over ~30 station props). If it is too dear, fall back to light layers on the WebGL URP asset only; that needs the map's shell split per room, a question for 关卡设计.

---

## 4. The three calls, settled

### 4.1 Light leaving the room → (b), made by the plan; no spill light

**The math** (§3.2; `data/light_math.py.txt`):
- E1 sits **8.9 m (W′), 8.9 m (D′), 11 m (H)** from the screen. The TV's light there is **0.000** with range 6, and **≤ 0.005** with range 9.
- Answer (a), "the set within about 6 m of E1": at 6 m the light is 0.07 (range 9) or 0 (range 6). The corridor's Steady troffers already give 0.42–0.61 there, and G1 puts a warm Steady lamp just inside E1. A 0–15 % tint lands where it is swamped. **(a) adds nothing that reads.**
- Answer (c), "a synced spill light at E1":
  - To show in a lit corridor it would need near-troffer intensity, so it makes a cold, stepping pool on the corridor floor with no visible source: a fake.
  - It sits on top of G1's warm lamp.
  - It adds a light per floor.
  - On WebGL an unshadowed spill leaks into the rooms beside the doorway.
  - **Rejected.** It stays a fallback only if the blockout squint test fails: then I ≤ 1.0, range 3 m, synced to the same track, Soft (desktop) / Hard (WebGL) shadow.

**What does read** is contrast inside the room:
- The TV side is Dim or Off (0.1–0.26), and the set washes its own zone at 0.3–0.64 in lavender-blue that changes at every card cut.
- Through E1 that is a darker opening with one moving coloured patch: "a TV is on in there" (tutorial §4.6 "a moving cold glow in one doorway").
- So **every module puts E1 on the set's face**, and from the last junction you see the screen itself plus its wash:
  - **W′:** E1 in the W wall's south cell, 22° off E1's axis and 24° off the screen's (G1's ±30° holds); the lit key plate patch is also in that view, 31° off the axis.
  - **D′:** E1 in the S wall's middle cell, 21° / 21°; the guard's chair and jacket stand 15–17° right of E1's axis, just left of and below the screen, against its wash (the eye-to-screen line passes 1.19 m over the chair's spot, above its 0.95 m back).
  - **H:** on the axis.
- Plan W as drawn (E1 in the S wall's west cell) is 52° off E1's axis, outside G1. Plan D as drawn sees the washed zone at 39° only from a corridor that approaches on the diagonal, which a grid map does not make. Both amendments are in §5 for 叙事.

**Proof:** blockout check 1 (squint from the last junction) for W, W′, D, D′, H.

### 4.2 The key lit by the screen → the position rule; plan W′

**Measure from the screen centre.** It stands **0.54–0.57 m** off the wall the set hangs on or stands against (`Kit_CRTTV`: screen 0.245 m before the pivot, back 0.241 m behind it, plus the bracket or a stand gap).

**The rule.** With the key on a wall perpendicular to the set's wall:
- let **s** be the screen centre's distance from the key's wall, and **f** the key's distance in front of the screen plane;
- the plan angle off the screen axis is atan(s/f), and the incidence on the key is 90° minus that;
- **keep 30° ≤ atan(s/f) ≤ 45°** (f between s and 1.73 s), so that the key is in front of the plane and is not lit at a graze (incidence ≤ 60°);
- **3-D distance ≤ 2.0 m;**
- **tilt the set** so the key stays inside the 100° inner cone (≤ 50° in 3-D): 20° down on a bracket at 2.10 m; level on a credenza.
- Then the key gets **E ≥ 0.6 on the mean card and ≥ 0.32 on the darkest** (with §3.1's mapping). That is 6–25× the lamps' share at the key, because the set's cell is Off: "readable by the screen's light alone" (§8.2).
- **G4's 60° is the hard maximum.**

**Plan W's corner set fails**:
- The set in the corner faces the diagonal, but its screen stands 0.566 m off both walls.
- On either adjacent wall the angle never drops below 45° (it tends to 45° only far away) and stays over 60° for the first ~2.7 m.
- At plan W's position (the N wall, 1.5 m from the corner) the key is **69° off the axis** (tilt 15°) at 67.5° incidence: **E 0.13 on the mean card, 0.07 on the darkest.** That is a third of a Dim lamp's own wall wash (0.42), so the key would be lit by the lamps, not the set.
- Tilting 30° only reaches 60°.
- 20 §4.4's "both adjacent walls about 45° off its axis" is true of the walls' direction, not of points near the corner.

| Plan | s / f | Off axis | Distance | Incidence | E mean / darkest | |
|---|---|---|---|---|---|---|
| W as drawn | corner | 69° | 1.37 m | 67.5° | 0.13 / 0.07 | fails |
| **W′** | 1.09 / 1.22 | **38°** | 1.87 m | 56° | **0.64 / 0.33** | passes |
| D (as drawn, kept) | 1.24 / 1.51 | **40°** | 1.97 m | 52° | **0.63 / 0.32** | passes |

**W′ (the fix):**
- The set on `Kit_TVWallBracket` on the **E wall** (the staff strip's back wall), screen centre 1.09 m south of the N wall, bottom 2.10, tilted 20° down, facing W over the counter toward the rows and E1.
- The key plate on the **N wall at x 7.05**, just behind the counter's line at its north end, with the annunciator 0.6 m to its right and the conduit between them.

**What this means for G5 (for 叙事):**
- The key is taken from ≤ 0.9 m (the key pickup reach, interactables §4.1), so a set that lights the key from the front stands about **100° to the side** of the take view (W′ 102°, D 99°).
- So G4 and a literal G5 ("the set within 30–50° of the view at the take") cannot both hold for a wall-hung key. G5's own fallback carries it:
  - the light on the plate and the key in front of the player drops from 0.45–0.76 to ~0.12 (**73–84 % in one change**);
  - on Floor 1 the C18 close-up frames the lit tag as it goes dark.
- **Proposed G5 wording:** "at the take, the light on the key board drops in view". Blockout check 3 measures it.

### 4.3 The annunciator → green POWER lamp only

- **Lit:** one Ø 10 mm green jewel marked POWER (the `Prop_LEDGreen` hue, ~125°). It is steady, emissive only, adds no realtime light and has no bloom flare.
- **Never lit by gameplay:** the 8 zone windows, the TROUBLE window and the lamp test. A lit zone would be a radar; red means ALARM ZONE and amber means TROUBLE in this game.
- **No colour on the unlit parts:**
  - the windows are **off-white engraved label tiles** (lamp windows with labels, P47), not red or amber lenses, so colour appears only where a lamp is lit;
  - the cabinet is **putty enamel**, not fire red. Period remote annunciators came both ways (ESTIMATE); we take the one that keeps red's meaning.
- **The glow ink:**
  - W6's pale yellow-green is a large, low-saturation emissive in the dark;
  - the POWER lamp is a 10 mm saturated green point on a panel;
  - they differ in hue, size and saturation. The blockout's era/look critic checks the pair side by side.
- **After the take it stays lit.** The take drops only the desk's non-emergency circuit (01 §5), so the set dies and the panel stays awake: the one point of colour left in the take frame, 0.6 m to the right of the empty hook. It does not dip in W1 (it is not a map lamp).
- **The wire tells the fiction:**
  - an EMT conduit from the key plate's box to the cabinet shows "this key is on the alarm" without a word;
  - it sits under the honest plate (`STAIR KEY` / `ALARMED…`, 01 §6; the plate is 教程设计 / 总控's call).
- **The same rule for other small lamps in view:**
  - the phone's line lamps are **dark**. A winking hold would be a second blinking light in view (G3). If 叙事 wants the hold call, a steady lamp;
  - the charger's lamp is dark (nothing in it).

---

## 5. Plan amendments (for 叙事 to accept; layout logic is 叙事's)

Scale as `20_layout.md` §4: 1 character = 0.3 m across, 1 line = 0.6 m down. North is up.

Legend: `[TV` the set (`[` its back), `K` key plate, `A` annunciator, `|` counter, `H` tandem seat, `g` guard's chair, `C` key cabinet, `m` magazine rack, `a` ashtray stand, `G` guard desk, `h` guest chair, `=` sign-in ledge, `L` the Steady lamp, `o` a Dim lamp (an Off cell shows none), `~` the set's wash, `*` the sight line from E1, `E` E1, `v` / `>` E2.

### 5.1 Plan W′: Waiting (B), 3 × 2, set on an E-wall bracket

```
+---------+---------+---------+
|                   ~ ~ K A    |   K key plate, N wall x 7.05, hook 1.42; A annunciator; conduit K-A
|     o  H    H    ~ ~ ~ ~     |
|m       H    H  ~ ~ ~ ~ ~ ~[TV|   set on the E-wall bracket: screen z 4.75, bottom 2.10, 20 deg down, faces W
|        H    H   ~ ~ ~ ~      |   H two tandem beams facing the set (cells (1,1), (2,1) Off)
+                *   ~ |       +   | counter x 6.65-7.40, z 1.0-3.6; staff strip 1.44 m, open at both ends
|            *         |   g   |   g guard's chair, turned to face the set (proposal)
E     L  *      o      |  o  C |   C key cabinet + sign-out clipboard (E wall)
E    *                 |       |   E1 arch, W wall south cell: set 22 deg off its axis, 8.9 m
E                      |       |
|               a              |   a ashtray stand under NO SMOKING
+---------+---------+----vvvv-+   E2 arch 1.2 m, S wall east cell, at the strip's south end
```

- **From E1:** the rows' backs facing a playing set across the room ("a room built for waiting, where nobody waits"). Beyond them the counter on the right, and the key plate lit on the N wall with the green dot beside it.
- **The take:** step past the counter's north end into the strip's mouth and face north: the key, its plate, the conduit, the green lamp. The set is ~100° to the right; the light on the key goes out (§4.2).
- **Leave:**
  - E2 is 5 m straight down the strip;
  - E1 is across the room;
  - the dead set hangs over the strip, in view as you turn south.
- **G6 / G7 / G8:**
  - nothing over 1.07 m stands between the key and either exit; the set hangs above head height;
  - the strip is 1.44 m and open at both ends;
  - the take zone is 2.2 × 2.2 m;
  - the counter's knee space is too small for the capsule.
- **Story change for 叙事:** the guard's chair is turned round to face the set, its back to the counter and the visitors ("the watcher faces the wrong way", carried into B).
- **Standard ceiling only** (S02). A Low-ceiling Waiting needs the cart (S03), Phase 2.

### 5.2 Plan D′: Desk (A), 3 × 3, E1 on the set's face

```
+---------+---------+---------+
|                       [TV]   |   set on Kit_Credenza_Laminate, N wall x 7.6, faces S, screen 1.04 m
|=    o                        |
|=                  ~ ~ ~ ~ ~ A|   A annunciator; K key plate, E wall 2.0 m S of the N wall
|=  h h              ~ ~ ~ ~  K|   (cells (1,2), (2,2) Off)
+                      *~ ~ ~  +
|   h h          GGGGG         |   G guard desk facing the set; g chair + jacket, back to E1
|     o         o   g     o    >   E2 arch 1.2 m (or map door), E wall middle cell
|                    *         >
|                   *          |
+                              +
|                  *           |
|     o         L *       o    |
|                              |
|                *             |
|                              |
+---------+--EEEEEE-+---------+   E1 arch 1.8 m, S wall middle cell: set 21 deg off its axis, 8.9 m
```

- **Two changes from plan D:**
  - **E1** moves from the SW corner to the S wall's middle cell;
  - **the desk** moves about 1.6 m east (x 5.0–6.5, from about 3.3–5.4), so the chair and jacket stand in front of the set's wash as seen from E1. This part is optional: with 叙事's desk position the chair is 3° left of E1's axis, 24° away from the set, against an unlit wall.
  - The set, key, ledge, guest chairs and E2 stay as 叙事 drew them. The E side stays clear for the escape line (2.3 m between the desk's east end and the E wall).
- **From E1:** the empty chair with its jacket, back to you, facing a playing set, which is D's strongest image ("he watched the set, not the door") seen square from the door. The lit key plate is on the right wall, 32° off the axis.
- **The take:** the same as D (99°; the light on the key drops).
- **Leave:** E2 is 2.3 m south along the E wall; E1 is 7 m.
- **For 叙事:** the oblique two-step reveal becomes "glow and chair, then the key". If you want to keep the oblique reveal, it needs (c) and the cost in §4.1.

---

## 6. What the blockout stage must prove (clone `W/proj_station`)

All six checks of `20_layout.md` §8, in the shot format, with VL rows, plus:
1. **W as drawn against W′**, and **D against D′**: the key's light from the set alone (spot lit, lamps off) at the mean card and the WTCZ card. Target ≥ 0.3 on the darkest card.
2. The light drop at the take, measured on the plate (target ≥ 70 %, one change).
3. The squint test from the last junction (1–2 cells out on E1's axis) for W′, D′ and H: the screen and its wash must be the brightest moving thing in view.
4. WebGL tier: the light through the N wall with and without the TV shadow, and the draw cost of the shadow.
5. The annunciator's green beside the glow ink's colour (era/look critic).
6. Low ceiling: confirm that the bracket set does not fit over a walk line (bottom 2.10 + 0.50 > 2.35).

Then the Figma section `FRONTROOMS · SECURITY DESK (STATION) · PROPOSAL` in the stairwell grammar (SW00–SW07), and the joint `40_for_red_draft.md`.

## 7. For other lines (no Red decision needed)

- **关卡设计:**
  - the station's budget as a hero room, ≤ 100k LOD0 in both themes (§1.5);
  - Station · Waiting needs a Standard room;
  - module columns None;
  - can a module fix an arch's width, so E1 is the widest (1.6–1.8 m, G1)?
  - "no door on E1" (20 §7 Q2);
  - the WebGL light-layer fallback needs the shell split per room (§3.6).
- **平面视觉:** one `Prop_StationPaper` atlas: sign-out sheet, DAR, register page, post-orders spine, tent card, magazine covers, annunciator labels, key plate, `NO SMOKING`, `KEY CONTROL`. Strings are 叙事's.
- **叙事:**
  - W′ and D′ (§5);
  - the G5 wording (§4.2);
  - the turned chair in B;
  - dark phone lamps;
  - the `STAIR A` tag.
- **声音设计:** none from the look. The set's light and sound share the VideoPlayer clock (§3.1).

## 8. Files

- `30_look.md`: this file.
- `data/light_math.py.txt`: the light and geometry math (URP falloff and cone, the plans, the ad's measured track); `python3 data/light_math.py.txt` reprints every number in §4.
- `10_directions.md`: the visual half, appended under a dated header; 叙事's text is untouched.
