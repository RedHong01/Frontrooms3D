# 10 — Door break design: staged damage, like the glass (DB1)

Status: DESIGN, 2026-10-03. Nothing in the game, Red's project or any clone was changed. No media was downloaded. This file is the only output of this step, plus a short "10" section in `media_candidates.md`.

Task (Red, 2026-10-03 ~12:05, translated): "When a door is being broken, I want it to be like the glass shattering: make different models that answer each damage state of the door. First research how AAA games generally do it, then give me a pure-visual proposal in Figma; implement after I confirm."

**Built from:**
- `01_aaa_doors.md` = **[01]** (how shipped games stage a door break);
- `02_real_door_failure.md` = **[02]** (how real 1990 doors fail; words P, S, BURST, RIP, MARK, D1, D2, FINAL, BROKEN);
- `../glass/destruction/10_glass_destruction_plan.md` = **[GP]** (staging, swap hiding, debris, budgets; reused, not repeated);
- `../interactables/00_map_constraints.md` = **[00]**, `10_spec.md` = **[SPEC]**, `05_locked_door_type.md` = **[05]**;
- `../interaction_audit/10_audit_report.md` §3.7 and F9/F11 = **[AUD]**, `FrontRoomsShotTimings.proposal.cs.txt`;
- `../hunter/11_squeezed_giant.md` = **[SG]**, `../hunter/10_hunter_directions.md` §8.3.

**Code read (read-only, real project, 2026-10-03 ~12:5x):** `FrontRoomsMapHunter.cs:132-139, 975-1000`, `FrontRoomsMapWorld.cs:76-82, 130-141, 177-200, 1262-1288, 2218-2262, 2410-2460, 1325-1345, 2804-2830`, `FrontRoomsLevelProfile.cs:110-170`, `FrontRoomsSoundDirector.cs:505-540`, `FrontRoomsRelayRig.cs:38-43`, `FrontRoomsRoomStream.cs:998-1015`, `FrontRoomsHunter.cs:21, 247-248`, `Packages/manifest.json`.

**Models read:** the interactables workflow has built the door kit. `Tools/Blender/frontrooms_kit/assets/interact_door_*.py`, `interact_lock_*.py` (12:0x–12:4x), FBX in `scratchpad/proj_int/Assets/Resources/Props/Models/Kit_Door*.fbx`, previews in `scratchpad/interact_previews/G1/`. The leaf module's docstring confirms a **44 mm solid-core** flush leaf. The squeezed giants are built too (`creatures/giant_[a-d]_*.py`, poses `low` / `std` / `door` / `tall`; previews in `scratchpad/creature_prev/giant_b/`).

**Tags:** **ESTIMATE** = my number or judgement. **UNVERIFIED** = not checked in a capture or a source. **CONTRACT** = a change to a file another chat owns, given as an exact proposal. **RED** = needs Red's decision.

---

## 0. Short answer for Red

1. **Yes: one model per damage state, swapped on the blows.** It is what Left 4 Dead (3 damage models) and Amnesia (2 damage meshes, then a pre-broken door thrown in pieces) ship [01 §0]. We do the same: **intact, D1, D2, BROKEN**, plus small permanent marks on the early blows.
2. **"Like the glass" means the same rhythm, in wood and steel.**
   - The glass pops a crack stage on each strike, never grows between strikes, and leaves teeth in the frame and glass on the floor [GP §0].
   - A wood door pops **cracks along the grain** on each blow, leaves **splinter teeth** at the strike and **splinters, screws and the torn strike** on the floor.
   - A steel door **dents and bows** on each blow. Its enamel **star-crazes** around each dent (radial and ring cracks in the paint, the closest thing a steel door has to cracked glass). It leaves paint chips, a popped rubber silencer and gypsum dust.
3. **The stages count back from the last blow,** so every tier tells the same story. With 5 blows: marks at 0.5 and 1.0 s, **D1 at 1.5 s, D2 at 2.0 s, the release at 2.4 s, the break at 2.5 s.** At the fastest tier (3 blows), D1 lands on the first blow.
4. **Every swap is hidden** on the blow frame: the leaf jolts, the camera shakes, the blow sounds, chips and dust burst at the damage, and the gap flashes with light.
5. **Light is the hole.** Solid-core and steel leaves pass no light until they fail. Light first pulses along the latch edge on each blow, then stays as a thin line with two bright corner wedges, then floods the opening when the door goes and the Relay stands back-lit in it.
6. **Three visual directions for Red to pick from (§8):**
   - **A, "Jamb Split":** forcible-entry realism. The latch side fails, the leaf stays whole, light does the talking.
   - **B, "The Lock Hole":** The Shining at true scale. The Relay drives the lockset through. A lit 54 mm hole opens at 1.0 m, and the Relay's light or its fingers come through it.
   - **C, "Too Big for the Door":** the squeezed giant's body is the damage. The frame racks, cracks run out from the corners across the wall like a cracked pane, the ceiling tiles above jump, and the leaf takes a shoulder-wide print.
7. **My recommendation:** A as the base on every door, with B's lock hole as the D2/FINAL beat on lever doors and steel doors. B's hole is real physics for a bored lockset, and RIP produces it on its own (§3.2). Keep C as the bold option for Red to judge from the renders.
8. **Nothing lands before Red confirms.** The game side needs a few exact contract changes from the map chat (§7.1) and some event parameters from the sound chat (§7.2).

---

## 1. Words, frames and fixed rules

### 1.1 Words (from [02])

| Word | Meaning |
|---|---|
| **P** | Push side, where the stops are |
| **S** | Swing side, the room the leaf opens into |
| **BURST** | The Relay is on P and drives the leaf into S. The player is usually on S, so the leaf comes **toward** the player. The map knows this: `DoorBrokenFrom(door, from, fromSwingSide)` with `fromSwingSide = false` (`MapWorld:82, 1279`) |
| **RIP** | The Relay is on S and pulls the leaf toward itself, through the lever or knob (the only thing it can grip on a flush S face). The player is on P, behind the stops. `fromSwingSide = true` |
| **Stages** | **S0** intact · **M** marks (no model swap) · **S1 = D1** · **S2 = D2** · **S3 = FINAL + BREAK** · **S4** aftermath (persistent) |
| **Groups** | **W-W** wood leaf in a wood frame (L0-F) · **W-S** wood leaf in a steel frame (OF-F; EX-F and RN-F are P2) · **S-S** hollow-metal leaf in a steel frame (every LOCKED member) [02 §1.1] |

### 1.2 Door root frame (Unity metres, [SPEC] §1.2)

- Origin: the hinge-jamb edge of the opening, on the wall centre line, at floor level.
- **+Z** along the opening (hinge jamb Z 0 → latch jamb Z 1.0). **+Y** up. **+X = S** (swing side). **−X = P** (stops).
- Blender = (−X, −Z, Y).
- Key points: leaf faces X ±0.022; leaf Y 0.015–2.095; lock and lever at (±0.022, 1.000, 0.920); `damage_latch` (0, 1.10, 0.9939); bored strike (0, 1.000, 0.998); mortise strike centre (0, 0.968, 0.998); hinges at Y 0.254–0.368, 1.057–1.171, 1.859–1.973; stops on P (X −0.0605 → −0.025, latch Z 0.982–0.998); casing S X 0.0795–0.105, latch casing Z 0.998–1.075.

### 1.3 Hard rules (from [00], [02] §7.3, [SPEC] §6.7)

1. The leaf collider stays 0.05 × 2.08 × 0.98 under `Door hinge {a}-{b}`, with its name. Models replace only renderers.
2. **Every damage mesh, mark, splinter and debris piece is render-only** (`kit.no_collider()`).
3. **After the break, nothing hangs inside the 1.0 × 2.1 opening.** Loose strips, casings and notches stay inside the jamb and casing envelope (Z −0.075 → 1.075, Y ≤ 2.175). Floor debris lies flat.
4. **The broken visual leaf stays within about 0.05 m of the collider's posed box.** Otherwise the E ray and the Relay's sight would hit an invisible box. (New rule from this design; it limits Direction C, §8.3.)
5. Jolts go on the `Leaf rig`, never the hinge ([AUD] F11; [SPEC] §6.5 item 6).
6. No Light components in kit assets. The one exception proposed here is a single pooled **breach light**, desktop only, owned by the visual chat's break system, not by any door asset (§4.5).

---

## 2. The stage clock: blows → stages

### 2.1 Facts from the code

- One blow every 0.5 s from 0.5 s in, and the last one 0.1 s before the door gives. `BlowCount = round(breakSeconds / 0.5)` (`MapHunter:132-139, 985-994`).
- `breakDoorSeconds` = 2.5 s × the tier's `breakDoor` factor: 1, 0.88, 0.76, 0.64, 0.52 (`LevelProfile:166-170`).
- Each blow raises `DoorBlow(position)` and calls `MapWorld.JoltForBlow(door, index, count)`. The sound chat already sends `Damage = (index + 1) / count` (`SoundDirector:526-538`). The Relay rig gets `DoorBlow(blow, blowCount)` (`RelayRig:39-43`).
- `ShotTimings.DoorBreak.Damage1At` 0.5 and `Damage2At` 0.8 exist, but no code reads them. At 2.5 s they would land at 1.25 s, **between** blows [02 §7.1].

### 2.2 The rule: count back from the last blow

Damage changes **on a blow, never between blows** [01 §4.2][02 §8.1][GP §2.5]. With blow index `i` (0-based) of `n`:
- `i = n − 1`: **FINAL** (the release; the break follows 0.1 s later);
- `i = n − 2`: **D2**;
- `i = n − 3`: **D1**;
- earlier blows: **M** (marks only).

| Tier | Break (s) | Blows | Blow times (s) | M | D1 | D2 | FINAL | Break | D2 lasts |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 2.5 | 5 | 0.5, 1.0, 1.5, 2.0, 2.4 | 0.5, 1.0 | 1.5 | 2.0 | 2.4 | 2.5 | 0.4 s |
| 2 | 2.2 | 4 | 0.5, 1.0, 1.5, 2.1 | 0.5 | 1.0 | 1.5 | 2.1 | 2.2 | 0.6 s |
| 3 | 1.9 | 4 | 0.5, 1.0, 1.5, 1.8 | 0.5 | 1.0 | 1.5 | 1.8 | 1.9 | 0.3 s |
| 4 | 1.6 | 3 | 0.5, 1.0, 1.5 | — | 0.5 | 1.0 | 1.5 | 1.6 | 0.5 s |
| 5 | 1.3 | 3 | 0.5, 1.0, 1.2 | — | 0.5 | 1.0 | 1.2 | 1.3 | 0.2 s |

- **Fewer blows compress the story from the front.** The marks drop first; D1, D2 and FINAL always play. At tiers 4–5 the very first blow already splits the jamb. That is the right feeling for a late, angrier Relay.
- With 5 blows, D1 and D2 land at 60 % and 80 % of the time, close to today's 50 % / 80 %, so the shot plan barely moves [02 §7.1].
- **Steel takes longer (RED, optional).** Real hollow-metal doors need more blows than wood [02 §0 item 5]. Proposal: **+0.5 s on LOCKED doors** (6 / 5 / 5 / 4 / 4 blows). Then steel keeps at least one dent mark (M) at every tier. It is a map tuning field (CONTRACT, §7.1 item 6). Default: off.
- **Short breaks:** if a future tuning gives 2 blows, D1 is skipped and D2 carries D1's marks (stages are cumulative). With 1 blow, only FINAL plays.

### 2.3 The glass, translated (what "like the glass" means for each part)

| Glass plan [GP §2.3] | Wood door (W-W, W-S) | Steel door (S-S) |
|---|---|---|
| Strike 1: crack fins pop inside the intact pane, a crushed spot, chips | D1: cracks pop **along the grain** at the strike or the lock stile; veneer flakes and fine splinters burst | D1: dents join into a dish; **enamel star-crazing** (radial + ring paint cracks) around it; paint chips |
| Strike 2: the pane swaps to its pre-cut pieces at final pose, facets catch light | D2: the jamb strip (or lock stile) **stands loose**; splinter teeth stand proud; the crack opens and light shows through it | D2: the lock edge **crushes and the edge seam opens**, showing the brown paper honeycomb; light in the slot |
| Shatter: pieces released in waves, outward from the hit | FINAL + BREAK: the strip, strike and splinters leave toward S with the leaf's speed added (Amnesia's rule [01 §3.2]) | FINAL + BREAK: bolts bend out; chips, a silencer and gypsum fall; the leaf leaves **whole** |
| Teeth stay in the frame | **Splinter teeth** stay at the jamb notch | The **sheared deadbolt tip** stays in the strike; the frame stays spread |
| Glass stays on the carpet | Strip, strike, screws, splinters, flakes stay on the S floor | Paint chips, silencer, gypsum crumbs stay on both floors |
| No growth between strikes | No change between blows | No change between blows |

---

## 3. Staged damage per door type

Columns: what the leaf and frame show; what the player sees in BURST (from S) and RIP (from P); how it is represented (§4). Sizes are from [02] §2–§4 unless marked ESTIMATE. "Blow" times are tier 1.

### 3.1 W-W: veneer leaf in a wood frame (L0-F, Level 0 free door)

The weak link is **the jamb beside the strike**: it splits along its grain [02 §1.2, §2.1].

| Stage (tier 1) | Leaf | Frame and wall | BURST: player on S sees | RIP: player on P sees | Representation |
|---|---|---|---|---|---|
| **S0** before 0.5 s | Intact kit leaf | Intact kit frame | Nothing; the stops close every gap | Nothing | Kit meshes |
| **M1** blow 1, 0.5 s | Varnish crazes in a 100–200 mm print where the blow lands: P face in BURST; round the S rose in RIP (ESTIMATE) | A varnish hairline at the strike; dust from the head | Leaf jolts 4 mm; **light pulse along the latch edge, strongest at the top and bottom corners** (0.12 s); the S lever rattles; dust falls through the lamp light | **Your lever jerks down 20–35° and springs back** (the Relay is pulling the S lever on the same spindle); a light pulse at lock height | Map jolt; mark overlay M1; leak-card pulse; dust at `head_dust_a/b`; lever animation (RIP) |
| **M2** blow 2, 1.0 s | Second print; the crazing joins | The hairline at the strike grows to ~100 mm; a 1 mm gap line opens under the S latch casing | As M1, stronger (5 mm jolt) | As M1; the lever comes back 3° low | Mark overlay M2 (adds to M1) |
| **S1 = D1** blow 3, 1.5 s | 2–4 short **vertical veneer splits** on the far face near the lock stile, Y 0.9–1.4, 60–150 mm long (the leaf bends; the face away from the blow stretches). A crushed print on the struck face | **Jamb split started:** a vertical crack 200–400 mm above and below the strike, on the S side of the mortise. Strike tilted 2–3°. S latch casing lifted 2–3 mm, pivoting on its upper nails | The split jamb and the crooked strike in plain view. **A steady 1–2 mm glow line** on the latch edge. 6–10 veneer flakes and a puff of fibre | The leaf rests 2–5 mm off the stop at 1.0 m: **a thin dark-and-glow slot along the stop**. P casing untouched | **Swap:** leaf `_D1`, frame strike-zone insert `_D1`. Casing transform. Chip/fibre burst. Leak cards steady 15 % |
| **S2 = D2** blow 4, 2.0 s | As D1, plus the latch edge rubbed and dented at 1.0 m | **Jamb strip loose:** 15–25 mm wide, 300–500 mm long, open 3–5 mm. **3–6 long splinters stand proud toward S** (50–300 mm). Strike hangs on one screw. S casing 10–20 mm off the wall; the wallpaper tears along its edge | **Splinter teeth pointing at you; two glowing wedges** at the top and bottom latch corners; the glow line widens to 3–5 mm; dust drifts through it as thin beams | The slot widens to 5–10 mm at 1.0 m, with dust in it. Your lever hangs 15° low and loose | **Swap:** leaf `_D2`, insert `_D2`. Casing transform 15 mm, tilt 1.5°. Wallpaper-tear overlay. Splinter burst. Leak cards 40 % |
| **S3a = FINAL** blow 5, 2.4 s | `_Broken`: the latchbolt bent, the latch edge crushed | The strip breaks out. Strike, strip and 2–4 screws released | The leaf twitches at full jolt (8 mm); **the strip and splinters fly toward you**; the whole latch edge flares with light | The slot flares; the lever snaps back up (load gone) | **Swap:** leaf `_Broken`, insert `_Broken`. Debris released (baked trajectories, §4.3). Leak cards 100 % |
| **S3b = BREAK** 2.5 s | Thrown open (map: 0.12 s to 105°). At about **105° its hinge stile hits the S casing on the hinge side** (the kit's clearance at 95° is 8.7 mm [SPEC §1.5]; about 0.9 mm per degree there, ESTIMATE). That blow **levers the top hinge's screws out**. It bounces back to 80° in 0.18 s | The hinge-side S casing cracks at the impact; the top hinge's frame leaf bends out | The leaf comes at you and rebounds; the far room's light floods the opening; **the Relay stands back-lit** for 0.3 s (`RevealHold`) | The leaf swings away from you; the Relay is revealed beyond it, lit by its own room | Map throw + bounce; casing crack swap (hinge zone); breach light flare (desktop, §4.5); debris lands 0.25–0.6 s |
| **S4** aftermath, persistent | Leaf at 80° on S, **top hinge torn**: screws out, a 20–30 mm gap at the top of the hinge edge, the latch corner dropped onto the floor (about 1°, [02 §8.2]) | A raw notch at the strike, 20 mm deep, ~400 mm tall, with splinter teeth; latch casing 15–20 mm off the wall; cracked hinge-side casing | Floor (S, within 1 m of the latch jamb): the jamb strip with the strike still screwed to it, 2–6 screws with wood fibres on the threads, 8–15 splinters, veneer flakes | From P the notch is seen in profile on the far side of the jamb; prints on the P face | `_Broken` leaf and insert stay. Floor debris merged into one static mesh. Marks stay |

### 3.2 W-S: wood leaf in a steel frame (OF-F; EX-F and RN-F in §3.4)

The steel frame does not split. **The leaf's lock stile is the weak link**: the latch tears out through the wood around the 54 mm bore [02 §2.2].

| Stage | Leaf | Frame and wall | BURST (S) | RIP (P) | Representation |
|---|---|---|---|---|---|
| **M** | Crazing at the rose; the rose rocks 1–2° | The steel frame rings; joint-compound flakes fall at the head joints | Glow pulse; S lever rattles | Your lever jerks and springs back | Marks; leak pulse; compound flakes with the head dust |
| **D1** | **Lock stile split** from the bore along the edge band's glue line, vertical, 150–400 mm. Rose pushed crooked 3–5°, veneer torn round it | Strike lip curls 1 mm toward S | The crack in the lock edge and the crooked rose | Slot at lock height. **Your rose is pulled into the face**: a crushed veneer ring round it (ESTIMATE) | Swap leaf `_D1`; frame insert `_D1` (strike lip); chips |
| **D2** | Split open 3–5 mm; **granular particleboard** shows in the crack (not stringy fibre); the lockset loose: rose tilted 8°, lever drooping 20° | Strike bent; silencers flattened | Crumbs falling below the lock; light through the cracked stile | Wider slot; your lever hangs loose | Swap leaf `_D2`; crumbs (particleboard, 1–5 mm) |
| **FINAL** | The latch tears through the edge: a **ragged notch** in the lock edge at 1.0 m (~80 × 120 mm, ESTIMATE). The lockset hangs by its spindle or falls out. **If it falls out, the 54 mm cross bore is a round hole through the leaf** | Strike bent, frame whole | Lockset and crumbs fall at your feet | In RIP the Relay tears the lockset out toward itself: **your rose and lever are pulled through and gone**, leaving a lit 54 mm hole | Swap leaf `_Broken` (notch, hole option); lockset parts become debris |
| **BREAK** | Thrown as W-W; the **closer arm** (Office has a closer on S) is over-driven past 95°: the forearm tears off its shoe | Shoe torn from the head casing; frame face band dented at the hinge-side impact | Leaf at you; closer arm whips; reveal | Leaf away; reveal | Closer forearm released as debris on one pivot; frame zone swap |
| **S4** | Leaf at 80° with notch (and hole); closer forearm hanging from the leaf; 2–3 drops of closer oil on the floor (ESTIMATE) | Strike bent but in place; hairline drywall cracks at the head corners | Lockset and crumbs on the S floor | — | Persistent |

### 3.3 S-S: hollow-metal leaf in a steel frame (L0-K, OF-K; RN-K, EX-K in §3.4)

**Steel bends, wood breaks** [02 §0 item 2]. The frame spreads, the lock edge crushes, the bolts bend. **No splinters.** The hinges are welded to 10 ga reinforcements and **hold**, so the broken steel leaf hangs **square**, not crooked [02 §7.2].

| Stage | Leaf | Frame and wall | BURST (S) | RIP (P) | Representation |
|---|---|---|---|---|---|
| **M** | A **dent that stays**, on the struck face: a shallow dish 100–250 mm across, 2–4 mm deep. **Enamel star-crazing** round it: radial cracks plus 2–3 rings, flaking to grey primer at the rim | Dust | Dents are on the far face; you see a faint **bulge** (1–2 mm) and hear a hollow boom; glow pulse | **Your knob twists 3–5° and stops dead** (hard stop, [05] §8 item 7); glow pulse | Marks on the struck face only (P in BURST, seen in V4). From S the player gets the boom and the light; a 1–2 mm bulge reads only in raking light, so it waits for the `_D1` mesh (ESTIMATE) |
| **D1** | Dents joined into a broad dish round the lock, 300–400 mm; **leaf bowed 5–10 mm** at the lock stile; paint flaking to primer at the dent rims | **Silencers crushed, one pops out** and falls. **Frame spread 3–5 mm** at the strike. Joint compound cracks along the frame face. **Diagonal drywall cracks from both head corners**, 150–300 mm, on both faces | The bow and the flaking on S; a diagonal crack from the head corner | Slot at lock height; the stop line kinks | Swap leaf `_D1`, frame insert `_D1` (spread, kinked stop). Wall-crack overlays. Silencer + paint-chip debris |
| **D2** | **Lock edge crushed:** the edge channel folds; **the edge seam opens 100–300 mm** above and below the lock, showing **brown kraft honeycomb**; the mortise armor front bent | Strike reinforcement bent; drywall cracked and shedding at the jamb | Honeycomb visible in the bowed edge; gypsum dust in the light | **Your knob goes limp** (the S knob is torn off its spindle); a bolt glint in the slot | Swap leaf `_D2`, insert `_D2`. Gypsum crumbs and dust |
| **FINAL** | Bolts bend out; the 25 mm deadbolt may **shear**, its tip staying in the strike | Strike hangs on one screw | A metallic snap; chips fly | Same | Swap leaf `_Broken`, insert `_Broken` |
| **BREAK** | Thrown open whole; at ~105° the leaf's hinge stile strikes the frame's S face band (dent) and rebounds; **no crooked hang** | Face band dented at the hinge side; closer forearm torn (OF-K) | Leaf at you; boom; reveal | Leaf away; reveal | Map throw (crooked 0° for steel: CONTRACT §7.1 item 5) |
| **S4** | Permanently bowed; crushed lock edge with honeycomb; bent bolts | Frame spread with a gap at the strike; sheared bolt tip in the strike; drywall broken at the jamb; corner cracks | Paint chips (2–15 mm), a silencer, gypsum crumbs on **both** floors | — | Persistent |

### 3.4 Run, Exit and the title stream (P2 / P3)

No map door uses these members yet ([SPEC] §2.1). Design only:

| Member | What differs | Note |
|---|---|---|
| **RN-F** ward door (laminate, no latch, closer) | Nothing latches it, so in BURST **one shove** opens it. The closer arm snaps off its shoe; the P-face stainless armor plate takes a broad dent; the laminate **chips** at the edges (no grain splits). In RIP the Relay yanks the offset D-pull: its standoffs bend and one tears out | CONTRACT when Run doors exist: a one-blow rule for latchless doors [02 §8.4] |
| **RN-K** utility door | As S-S. With Red's lite option (`Kit_DoorLeaf_SteelLite`, wired glass): **the lite is the literal glass beat.** D1 cracks the wired glass; D2 crazes it and it sags on its wire; BROKEN leaves it bulged and hanging on the wire. Uses the glass track's `Wired6` profile [GP §2.1] | Needs the glass track |
| **EX-F** exit (wood leaf, exit device on P) | In BURST a push on the bar opens a real exit door: **one shove**, the bar's end cases crack. In RIP the rim strike tears off the frame | CONTRACT when Exit doors exist |
| **EX-K** locked exit | As S-S. The dead EXIT sign above P jumps on each blow and drops onto one screw at FINAL (ESTIMATE) | Sign is a frame anchor (`exit_sign_p`) |
| **Title stream double doors** (`RoomStream.BreakDoor`, old hunter `FrontRoomsHunter.cs:247-248`) | 2.4 m pairs, no stages today | P3: needs a pair version of the kit first [SPEC §2.1] |

### 3.5 What every broken door shares

- **The leaf always goes to S** (both cases) and rests at 80° ([00]; `MapWorld:1270-1287`).
- **The physical reason for the bounce** is the hinge stile hitting the S casing near 105° (§3.1 BREAK). That impact is what tears the top hinge on wood and dents the frame face on steel. No base stop is needed (ESTIMATE; check on a capture).
- **The opening stays clear.** Strips and casings stay in the casing envelope; floor debris is flat and render-only.
- **Damage persists.** A door left at D1 or D2 (the Relay relayed away mid-break, `MapHunter:469, 970-973`) keeps its stage; the next break starts from it. A broken door comes back broken after a chunk rebuild (`MapWorld:1118-1122` already restores the angle). Stages never heal, like the glass [GP §3.6 item 5].

---

## 4. How each stage exists in the game

### 4.1 Stage meshes ("one model per state")

| Asset (new; names follow [SPEC] §9) | Stages | Notes |
|---|---|---|
| `Kit_DoorLeaf_Veneer_D1 / _D2 / _Broken` (+ `_Oak` by slot swap) | 3 | Authored from the intact module with a `STAGE` parameter. **Outside the damage zone every stage is vertex-identical to the intact leaf** (same UVs, same slots), so a swap changes only the damage zone. This is the glass plan's "exact tiling" rule [GP §2.5 item 3] |
| `Kit_DoorLeaf_Steel_D1 / _D2 / _Broken` | 3 | Bow is baked into the vertices (no blend shapes: the Animation module is not installed, [AUD] F9). Honeycomb is real geometry in the open seam |
| `Kit_DoorFrame_Wood_StrikeZone_D0…_Broken` | 4 | The latch jamb from Y 0.70 to 1.30 is cut out of `Kit_DoorFrame_Wood` into its own insert. The split needs real jamb depth (15–20 mm) [02 §7.2]: see CONTRACT §7.1 item 8 |
| `Kit_DoorFrame_Steel_StrikeZone_D0…_Broken` | 4 | Spread, kinked stop, curled strike lip, crushed silencers |
| `Kit_DoorFrame_*_HingeZone_Broken` | 1 | Cracked S casing (wood) or dented face band (steel) at the 105° impact; torn top hinge screws (wood) |
| `Kit_DoorCasing_Wood_LatchS` | transform only | The S latch casing becomes its own object so it can lift 2 → 15 mm and tilt 1.5° on D1/D2. No new triangles |
| Lock parts (G2) | transforms + debris | Lever/knob jerks (RIP), rose tilt, lockset knocked out (Direction B) use the separate lock parts [SPEC] §3.1 |

The intact assets are **not** changed for this; the stage assets sit beside them.

### 4.2 Marks (M blows; also carried by every later stage)

- A small set of **render-only overlay quads**, 1 mm proud of the faces, alpha-clipped, from one atlas `Door_DamageMarks`: varnish crazing, the strike hairline, enamel star-crazing, primer flakes, wallpaper tears, drywall corner cracks.
- ≤ 4 quads per blow, ≤ 24 per door, 1 draw.
- **No shader change and no URP decals** (decals would need the Decal renderer feature, UNVERIFIED in our URP assets). A later shader damage mask is possible but NEEDS APPROVAL.

### 4.3 Debris: baked, like Resident Evil 7

- RE7 fractured hero breaks offline, simulated many times, kept the best, and baked them [01 §3.6]. We do the same in Blender: per group (wood, wood-on-steel, steel) and per side (BURST, RIP), **6 baked throws**, chosen by the door's edge hash.
- Each throw is transform keys only (30 Hz, ≤ 1.5 s, ≤ 40 pieces). It plays the same on desktop and WebGL, needs no physics scene, and **the settled layout is known in advance**, so a rebuilt chunk restores exactly the same floor.
- Pieces leave outward from the damage point **plus the leaf's own direction** (Amnesia's impulse rule [01 §3.2]): in BURST and RIP alike, toward S.
- After landing, the pieces merge into one static floor mesh per door (1 draw).
- Why not the glass's live physics scene [GP §4.3]: door debris lands on a clear 1.2 m keep-clear strip, the throw is always the same direction, and persistence is free. The glass needs aim-dependent fractures; the door does not.

### 4.4 Particles

- Dust per blow from `head_dust_a/b`; fibre and chip bursts on D1/D2; the big burst at FINAL.
- **The particle module is still not installed** (`Packages/manifest.json` has physics but no `com.unity.modules.particlesystem`; [AUD] F9). Enabling it is the same one-line approval the glass plan needs (RED, shared ask).
- Without it: dust as a few scripted camera-facing cards, and the bursts as extra baked debris pieces.

### 4.5 Light leak (the most important read)

- **Leak cards:** thin emissive strips in the latch-edge channel and at the top and bottom latch corners, double-sided and hidden by the real stop and leaf geometry until the leaf moves. Their brightness = the far room's lamp colour × its lamp level (0 if the far lamp is dead) × the stage curve: pulse to 100 % on every blow and decay in 0.12 s (the jolt spring); steady 0 / 15 % / 40 % / 100 % for M / D1 / D2 / FINAL. 1 draw.
- **Breach light (desktop only):** one pooled spot light with shadows, placed 1.2 m behind the door on the Relay's side at 1.8 m, aimed through the door. Off until D1, low with the leak, then **flares at the break for the 0.3 s reveal** and fades over 1 s. It throws real light shapes through the gaps onto the near floor and wall, and it back-lights the Relay (RE7's silhouette [01 §3.6]). Only one door breaks at a time, so it costs one light for ≤ 3 s. Not in WebGL.
- **Dust beams:** additive cards along the gaps at D2 (no volumetrics in our URP setup).
- The leak only reads if the far room is brighter than the near one. Two lamps in three cast no shadows today [00], so the lamp look (visual chat) matters as much as the models.

### 4.6 The Relay's part (visual chat, Relay rig)

- Blow poses already fire on `DoorBlow` (chest snap). Each case needs its own contact: **BURST** a forearm or shoulder blow at 1.1–1.6 m, 0.15–0.35 m from the latch edge [02 §2.3]; **RIP** a yank on the S lever or knob.
- Capcom made the fist capsule bigger to sell the hit [01 §3.6]. Our giant's contact should look bigger than the damage: a 0.32 m hand [SG].
- Direction B adds a **peek / reach** beat between blows N−1 and N (§8.2). Direction C adds the ceiling press during the break (§8.3).

---

## 5. How the swaps are hidden

1. **On the blow frame.** The stage swap, the sound transient, the camera shake (0.2–0.6° within 8 m) and the Relay's chest snap all fire on the same frame [01 §3.2][GP §2.5 item 1].
2. **The leaf is moving.** The jolt moves it 4–8 mm and 0.6–1.2° on that frame, so before and after cannot be compared pixel by pixel.
3. **A burst covers the damage zone:** fibre and flakes (wood), paint chips (steel), dust from the head (all) [01 §4.3].
4. **The light flash** at the gap is the brightest change in the frame and takes the eye.
5. **Stages are cumulative and identical outside the damage zone** (§4.1), with the same UVs and materials, so only the damage zone changes.
6. **Object hit-stop:** the leaf holds its jolt peak for 2–3 frames on D1, D2 and FINAL (33 / 40 / 50 ms). The camera never stops [GP §2.5 item 5].
7. **The break hides in motion:** `_Broken` swaps in on FINAL under the strongest jolt and the biggest burst, and 0.1 s later the leaf is flying at 13.5 m/s at its edge [02 §2.5], with a 1.0° shake and the breach-light flare.
8. **Off screen,** swaps happen silently and restore from the record.

---

## 6. Budgets (ESTIMATES; desktop is the reference, WebGL is a separate tier)

| Item | Desktop | WebGL |
|---|---|---|
| Veneer leaf: intact / D1 / D2 / Broken tris | 2.6k / ≤ 4k / ≤ 6k / ≤ 7.5k (about 3× at Broken, as RE7 measured [01 §3.6]) | LOD1 of each, ≤ 1.5k |
| Steel leaf | 4.2k / ≤ 6k / ≤ 9k / ≤ 10k | ≤ 2k |
| Strike-zone insert (per frame type) | 0.4k / 1.2k / 3k / 2.5k | ≤ 0.6k |
| Marks | ≤ 24 quads, 1 draw, atlas 1024² | atlas 512² |
| Leak cards | ≤ 12 quads, 1 draw | same |
| Breach light | 1 shadowed spot, ≤ 3 s, only while a door breaks within 12 m | none |
| Live debris at the break | ≤ 40 pieces (wood), ≤ 30 (steel), for ≤ 1.5 s; Capcom capped real-time breaks at about 50 [01 §3.6] | ≤ 12, combined into 1 draw |
| Particles (if approved) | dust 40 per blow, 60 per D-burst, 250 at the break | 10 / 15 / 40 |
| Persistent per damaged or broken door | +2 draws (marks, floor debris ≤ 3k tris, LOD1 ≤ 800) | +1 draw |
| Memory | ≤ 1 MB of stage meshes per door type, shared by all doors; ≤ 0.6 MB of baked throws per group | half |
| CPU | swap ≤ 0.05 ms; debris playback ≤ 0.1 ms; breach light ~0.2–0.5 ms while on (UNVERIFIED) | swap and playback only |
| Ray tracing (G14, Mac) | Stage meshes are rigid: build their structures when the chunk builds, never on a blow frame. Debris not traced | none |

---

## 7. What other chats need to do

### 7.1 Map chat (关卡设计): CONTRACT requests, exact

1. **An event per blow with the door and the side.** In `MapHunter:994` pass the Relay's position: `world.JoltForBlow(breakingDoor, BlowIndex, BlowCount, position)`. In `MapWorld.JoltForBlow` add:
   `public event Action<Door, int, int, bool> DoorBlowStruck; // door, index, count, fromSwingSide`
   raised once per blow with `OnSwingSide(door, from)`. (Interim without it: the visual side matches `DoorBlow(position)` to the nearest door and reads `relay.BlowIndex/BlowCount`, but it cannot know the side before the break.)
2. **Count-back stages in `ShotTimings.DoorBreak`** (replaces `Damage1At` / `Damage2At`, which nothing reads):
   ```csharp
   public const int D1BlowsFromEnd = 2, D2BlowsFromEnd = 1;
   // 0 MARK, 1 D1, 2 D2, 3 FINAL for blow i (0-based) of n
   public static int Stage(int i, int n) => i >= n - 1 ? 3 : i == n - 2 ? 2 : i == n - 3 ? 1 : 0;
   public static readonly float[] LeafHoldSeconds = { 0f, .033f, .040f, .050f }; // leaf-only hit-stop per stage
   ```
   and hold the jolt at its peak for `LeafHoldSeconds[stage]` in `PoseLeaf`.
3. **A damage record per edge,** kept across rebuilds like `brokenDoors` (`MapWorld:307`): `Dictionary<long, (int stage, bool fromSwingSide)> doorDamage`, with `public int DamageStage(Door)` and `public bool BrokenFromSwingSide(Door)`. A resumed break starts from the stage reached.
4. **Restore on build.** Where `BuildEdge` restores a broken door (`MapWorld:1118-1122`), pass the stage and side to the dress call (the facade [SPEC] §6.1, `DressDoor(door, stage, fromSwingSide)`), so the right stage meshes and floor debris come back.
5. **The crooked hang** [02 §8.2]: today's `PoseLeaf` turns the leaf about the top of its hinge edge, which **lifts** the latch side about 52 mm (code reading, UNVERIFIED in a capture). Proposal: pivot about the **bottom** hinge (Y ≈ 0.31) and drop the latch side until its corner touches the floor, about **1.0° for wood doors and 0° for steel** (welded hinges hold). Replace `CrookedDeg 3` with `CrookedDegWood = 1f, CrookedDegSteel = 0f`. **RED:** the 1° drop is honest but subtle; the torn top hinge (§3.1 S4) carries the read. Keeping 3° is Red's call.
6. **Optional, RED: steel takes longer.** `lockedBreakExtraSeconds = .5f` added to `breakDoorSeconds` for locked doors (§2.2). Default 0.
7. **P2: latchless and exit doors.** When RN-F and EX-F exist on the map, a BURST on them is one shove (one blow, then the throw) [02 §8.4].
8. **The jamb needs depth at the strike (wood frames).** The kit's wood lining is a 2 mm skin over the map's wall end [SPEC] §1.4, so a 20 mm notch has nothing to cut into. Proposal: on kit-dressed door edges, skip the jamb trim box (already offered as [SPEC] §6.5 item 9) and **recess the latch wall end by 25 mm between Y 0.70 and 1.30** (render mesh only; no collider, nav or opening change). The kit's strike-zone insert fills the recess. If refused, the notch becomes a 2 mm painted recess plus the lifted casing and the standing splinters: fine at 3 m, weak at 0.5 m.
9. **Confirm rule §1.3 item 4** (the broken visual leaf within 0.05 m of the collider's posed box).
10. Already requested elsewhere and still needed: jolts on the `Leaf rig` ([SPEC] §6.5 item 6) and the 0.3 s `RevealHold` with the Relay in the doorway ([AUD] §3.7: no code reads `RevealHold` yet).

### 7.2 Sound chat (声音)

| Event | Change | Why |
|---|---|---|
| `Mechanism/Door/Blow` | Add `DoorType` (0 wood, 1 hollow metal; asked in [05] §8 item 9), `Stage` (0 M, 1 D1, 2 D2, 3 FINAL) and `Side` (0 BURST, 1 RIP). Keep `Damage` | The crack layer (wood split; steel crunch and pop) must land on the swap frame ± 1 frame [GP §4.5] |
| RIP layer | On each RIP blow: the player-side lever or knob wrenched, the latch rattling in the strike, the silencers thumping | RIP's horror is your own handle moving |
| `Mechanism/Door/DebrisImpact` (new) | Position, `Material` (strip, small metal, splinter, paint/gypsum), `Mass`; fired from the baked throw's known landing times | Clatter, screw ticks, splinter skitter, gypsum crumble [02 §5] |
| `Mechanism/Door/Break`, `DoorStopLimit` | Add `DoorType` and `Side`; the stop-limit hit is the 105° casing impact | Wood: split, screw squeal; steel: bolt snap, boom |
| `Mechanism/Door/CloserSnap` (new) | Office, Run and Exit doors on the throw | Closer arm over-driven [02 §2.2] |
| `Mechanism/Door/Stress` (optional loop) | After D1, between blows: wood creak, steel tick; stops on the break | Sound may move between blows; the picture does not (L4D's `PressureDelay` idea [01 §3.1]) |

`FrontRoomsDoorSound.MarkBroken` already suppresses Handle and Unlatch on a broken door (`SoundDirector:513-519`).

### 7.3 Visual chat (owner of this work)

- Stage meshes, inserts, marks atlas, leak cards, breach light, baked throws, debris sets (§4).
- Relay rig: BURST and RIP contacts; Direction B's peek/reach; Direction C's ceiling press.
- Ask Red once for: the particle module (shared with the glass), the media batch (§8, `media_candidates.md` 01, 02, 10).

---

## 8. Three visual directions

All three keep the stage clock (§2), the hard rules (§1.3), the hiding rules (§5) and both swing cases. They differ in **where the damage goes, what it reveals, and the distance it reads at.**

| | A · Jamb Split | B · The Lock Hole | C · Too Big for the Door |
|---|---|---|---|
| Where the damage is | Latch jamb (wood) / lock edge and frame (steel) | The lockset and the 54 mm bore | The giant's contact: shoulder band on the leaf, frame corners, wall, ceiling |
| What it reveals | Light along the latch edge | Light, then the Relay's light or fingers through a hole | The Relay's size |
| Reads best at | 1–4 m, in light | 1–6 m, best in the dark | 6–12 m |
| Physics | Honest [02] | Honest for bored locksets [02 §2.2]; the peek is staging | Art direction past 1990 physics |
| Lead references | Real forced doors (df01, df02, db05, df05) | The Shining (db01), L4D2 lit hole (db02) | RE7 Jack's wall (db03); the squeezed giant |
| Build cost | Lowest | + lock parts as debris, + one rig beat | Highest: wall and ceiling overlays, rig press |

### 8.0 Common pre-render setup (every brief uses it)

**Tooling.** Blender 4.3 headless (`/Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup --python …`), in the render workflow's private clone. Build the intact kit from the current modules (`interact_door_frame_wood/steel.py`, `interact_door_leaf_veneer/steel.py`, `interact_lock_*.py`, `interact_door_closer_*.py`) so the renders match the latest G1/G2 fixes; the FBX in `proj_int` are the fallback. Damage stages are new render-side scripts (e.g. `door_break/db_stages.py`) in that clone; they become kit modules only after Red picks.

**Set.**
- One 0.16 m wall on a cell line with the door centred in a 3 m cell edge. **P side = a Low zone** (2.4 m ceiling, Level 0); **S side = a Standard zone** (2.9 m; Level 0, or Office for Office members). Map doors sit on Low↔Standard borders [00], and the spec suggests swinging into the Standard room [SPEC] §6.5 item 1.
- Each side two cells deep (6 m) and two cells wide (6 m: Z −1.0 → 5.0, the extra cell on the latch side), so V1 at 3 m and V2 at Z 2.56 sit inside the rooms. For V6 add a 4-cell (12 m) corridor on S.
- Surfaces from `Assets/Resources/Surfaces` textures: Level 0 Chevron wallpaper, carpet, ceiling tiles with 2×4 troffer lenses; Office drywall, carpet tile and louvre lenses.
- Scale check: one frame per direction includes a 1.80 m reference person silhouette at the latch side.

**Cameras** (door root frame, §1.2; Unity vertical FOV, as the game camera `fieldOfView = 76`, `FrontRooms3DGame.cs:264`; eye 1.62 m, `ModuleUnits.PlayerEye`). Mirror X for the other side.

| View | BURST position (player on S) | Aim at | FOV (vertical) | Shows |
|---|---|---|---|---|
| **V1** player at 3 m | (+3.0, 1.62, 0.50) | (0, 1.10, 0.50) | 76° (game) **and** 45° (slide) | The whole door and surround, as played |
| **V2** 45° latch side | (+1.556, 1.62, 2.556): 2.2 m from the latch jamb at 45° | (0, 1.10, 0.92) | 50° | The latch edge, strike and casing in depth |
| **V3** close-up | (+0.55, 1.35, 0.80): the head-dip distance | (+0.022, 1.10, 0.92) | 62° | The split, the hole or the dents at 0.3–0.6 m |
| **V4** far side | (−3.0, 1.62, 0.50) | (0, 1.10, 0.50) | 76° and 45° | The Relay's side: prints, dents, stops; with and without the Relay |
| **V5** floor afterwards | (+1.40, 1.62, 1.70) | (+0.50, 0.00, 0.90) | 62° | The hung leaf at 80°, the notch, the debris fan on S |
| **V6** 12 m read | (+12.0, 1.62, 0.50) | (0, 1.10, 0.50) | 76° | Does the stage read from the far end of a corridor |

For **RIP** (player on P): V1, V2, V3 use −X; V4 is taken from S; **V5 stays on S** (the debris is always on S).

**Lights.**

| Set | Near room (player) | Far room (Relay) | Use |
|---|---|---|---|
| **L-A** Level 0 steady | Troffer per cell centre: area light 0.6 × 1.2 m pointing down + emissive lens, colour (1, 0.96, 0.88), matching the map lamp (`MapWorld:1339`, Level 0 intensity 5) | Same | Clean stage strips |
| **L-B** leak | Lamp at 15 % ("dim/failing") | Steady lamp + the breach light (§4.5) at 1.2 m behind the door, 1.8 m high | **The money frames:** glow line, corner wedges, dust beams, the back-lit reveal |
| **L-C** Office | Office louvre lamp (intensity 5.5, `MapWorld:2824`), drywall; −22 % saturation grade in the compositor | Same | Office members |
| **L-D** dark behind | Steady | Dead (0) | The inverse read: the opening is black; only the Relay's own light (Giant B's lens) shows |

Calibrate exposure so the Level 0 wallpaper matches the in-engine audit frame `../interaction_audit/images/77_relay_broken_witness.png` within ±0.3 stop. Add a thin dust volume in both rooms, denser in L-B.

**The Relay.** Giant B "Night Shift" (`creatures/giant_b_night_shift.py`), the lead pick [hunter 10 §8.3], with `LENS_GLOW` 3.0 (Hunt). Pose `low` on the Low side (top ≤ 2.37 m, face 1.30–1.65), `std` on the Standard side, `door` for the reveal in the opening. W1 is still open: if Red picks another body, re-render only the Relay frames.

**Render.** Cycles, 1920 × 1080, 256 samples, OpenImageDenoise, the kit preview's view transform. JPG q85 to `Documentation/research/door_break/images/`. Names: `db_<A|B|C>_<member>_<burst|rip>_<stage>_<view>_<light>.jpg` (for example `db_A_L0F_burst_D2_V1_LB.jpg`). One **stage strip** per member and case: S0 → M → D1 → D2 → FINAL → S4 from V1, six panels in one 3840 × 720 JPG.

**Checks before a frame is used** (a critic pass, as for the glass [GP §5.1]):
1. After the break, nothing inside the 1.0 × 2.1 opening except the leaf at 80° (§1.3 items 3–4).
2. Stages are cumulative: every later stage contains the earlier damage.
3. Damage faces the right side for the case (BURST: splinters and debris toward S).
4. Era: nothing after 1990, no brands, no maker marks.
5. Hardware at the spec's positions; the leaf hangs as §3 says (wood 1° drop with a torn top hinge, steel square).

### 8.1 Direction A · "Jamb Split" (forcible-entry realism)

**Idea.** The door fails exactly where a 1990 door fails: at the latch. Wood splits along the grain and the strike tears out on a strip of jamb; steel dents, bows and crushes at the lock edge while its frame spreads. The leaf itself stays whole. The only hole is light: a pulse along the latch edge on each blow, then a steady line with two bright corner wedges, then the whole opening and the Relay back-lit in it. Each blow leaves a permanent mark, popping like glass cracks.

**Why.** It is honest [02], period-true and the cheapest to build (local inserts plus transforms). The colliders stay truthful. The real ram clip shows the same thing: blows with no visible leaf damage, then the whole leaf swings open [01 §5].

**Risks.** Subtle at 8–12 m, and in a dead-lamp room the light cue disappears. The drama rests on light, sound and the reveal.

**Lead references.** Pending Red's approval: `df01` (Commons, ram-damaged apartment door), `df02` (Commons, split jamb and torn strike), `db05` (ram on the lock side, the leaf swings open intact), `df05` (NILECJ 1975 ram figures, public domain). On disk: `../interaction_audit/images/70–80` (today's inert break, the "before"), `../interactables/proposal/media/kane_emg_0053_oak_door_lever.jpg` (the IP's office door), `ingame_door_gap_16_sideB_hinge_inline_darknear.png` (how a lit slit reads in engine).

**Pre-render brief A.**
- **Members:** L0-F (W-W, L-A/L-B) and OF-K (S-S, L-C). Optional third: OF-F (W-S) at D2 and S4 only.
- **Stages and views:**
  - Stage strip, BURST, V1 at 76°: S0, M2, D1, D2, FINAL (strip and 8–15 splinters in flight 0.05 s after the blow, leaf still shut), S4 (leaf at 80°). One strip per member: 2 strips.
  - Stage strip, RIP, V1 from P: S0, D1, D2, S4. 2 strips.
  - V1 at 45°: D2 under **L-B** (the glow line, corner wedges, dust beams) for both members. **The hero frame.**
  - V2: D1 and D2 for both members (the split jamb / the folded lock edge).
  - V3: D1, D2, S4 for both members (0.55 m: grain split and splinter teeth; enamel star-crazing, primer, honeycomb in the open seam).
  - V4: D2, BURST, without the Relay (prints on the P face, the stops intact) for both members; one with the Relay in pose `low` mid-blow, forearm on the leaf 0.25 m from the latch edge at 1.3 m.
  - V5: S4 for both members (wood: strip with the strike still on it, screws, splinters within 1 m of the latch jamb; steel: paint chips, a silencer, gypsum).
  - Reveal: V1 at 45° under L-B, 0.1 s after the break: leaf at 100°, debris in the air, the Relay in pose `door`, back-lit, lens on.
  - V6: L0-F at S0 and D2 under L-B: does the glow line read at 12 m?
- **Count:** about 30 frames + 4 strips.
- **Build notes:** wood splits are V-section cuts along +Y with 0.3–0.8 mm jagged walls and fibre tufts; splinters are tapered 5–7-sided prisms, 2–10 mm thick, with end grain; the loose strip is a separate body hinged on its upper casing nails. Steel dents are smooth dish displacements (no creases below 250 mm); the bow is a 5–10 mm sine across the lock stile; the honeycomb is 12 mm brown kraft hex cells, torn at the seam; paint is three layers (almond enamel 0.1 mm over grey primer over bare steel at the deepest scuffs).

### 8.2 Direction B · "The Lock Hole" (The Shining at true scale)

**Idea.** The Relay goes for the lock, the way Jack went for the panel beside the lock. Every blow lands on the lockset. D1 cracks the rose and splits the veneer **in rays along the grain from the bore** (wood's version of the glass's radials). D2 drives the lockset through: on your side the rose and lever drop at your feet, and **a 54 mm hole opens at 1.0 m, lit from behind**. In the half second before the last blow, the hole fills with the Relay's light (Giant B's lens), or two gloved fingertips push through and hook toward the latch. Then the stile tears and the leaf goes.
- **RIP is the scariest version:** your own lever is yanked into the door and disappears; the hole lights up; then the fingers come through toward you.
- **Steel:** the knob is torn off and the cylinder punched in, leaving a coin-size lit hole (about 32 mm, ESTIMATE). The enamel star-crazes around it, the closest any door gets to cracked glass. Fingers do not fit, only light.

**Why.** It is the strongest horror beat in the references (weapon or body through the door before it gives [01 §4.5]), and it stays honest: a bored lockset really can be driven out, the hole is the real 2-1/8" cross bore, and it is the only through-hole a solid-core leaf gives [02 §2.2, §4]. The Relay's sight still stops at the leaf collider, so **you can see it but it cannot see you** through the hole, the same as the lite option [05] §6.4.

**Risks.** At 3 m the hole is a bright dot of about 12 px at the game's 76° FOV in 1080p (about 23 px in the 45° slide view): that is the point, but it needs L-B or L-D to read. It needs a new Relay rig beat (peek and reach) and the giant's finger (about 33 mm at 1.65× a person) to fit the 54 mm bore. W-W doors in real life fail at the jamb first, so on L0-F the lockset going is a staging choice.

**Lead references.** Pending: `db01a–e` (The Shining 1:30 blade, 1:48 grain split, 1:52 see-through hole, 2:05 face, 2:08 hand to the lock), `db02` (L4D2: the lit corridor white through a torn hole). On disk: `../interactables/proposal/media/kane_emg_0125_door_six_bolts.jpg` (the IP's locked door), `a24_trailer_0139_dark_door_level0.jpg` (a dark door in Level 0: the dark-hole read).

**Pre-render brief B.**
- **Members:** OF-F (W-S, oak, chrome bored lockset; the honest hole) under L-C and L-B; L0-K (S-S, Level 0) under L-B and L-D.
- **Stages and views:**
  - Stage strip, BURST, V1 at 45°: S0, D1 (rose cracked, 4–6 grain splits radiating 80–200 mm up and down from the bore), D2 (lockset gone, hole lit; the rose and lever on the S floor), D2+ (the same frame with the Relay's lens filling the hole), FINAL (stile torn, notch at 1.0 m), S4. Both members.
  - Stage strip, RIP, V1 from P at 45°: S0, M (your lever jerked 30° down), D2 (your rose pulled into the face and gone, hole lit), D2+ (fingertips 60–90 mm through the hole toward you), S4. OF-F only.
  - **V3 close-ups are the hero frames:** the hole at D2 (L-B), the lens behind the hole (L-D), the fingers through the hole (RIP, L-B), the steel coin hole with star-crazing (L-B and L-D).
  - V1 at 76° at D2: what the dot looks like at 3 m in play.
  - V4 far side at D2+: the Relay in a stoop with its lens 0.25 m off the P face at 1.0 m (BURST); the Relay's gloved hand on the S lever (RIP).
  - V5 S4: the lockset parts on the floor, the notch, the hole.
- **The Relay pose:** a new `peek` variant of Giant B `low`: lens centre lowered to 1.0 m, 0.25 m behind the hole, hood touching the leaf; for the reach, the index and middle fingers of the near glove through the bore, 60–90 mm proud of the far face, bent toward the latch.
- **Count:** about 26 frames + 3 strips.
- **Build notes:** the hole lip is torn veneer 0.6 mm thick bent outward toward the viewer's side, with crushed particleboard in the bore wall; the grain splits radiate only up and down (never across the grain); the steel coin hole has an inward-dished crater 60 mm across with 8–12 radial and 2–3 ring cracks in the enamel.

### 8.3 Direction C · "Too Big for the Door" (creature-scale crush)

**Idea.** The damage is the giant's body, not the lock. The Relay's back is jammed against the ceiling [SG], so each blow is its whole squeezed body against the doorway. On each blow the ceiling tiles above the door jump on its side and dust curtains through the head gap. The frame racks. **Cracks run out from both head corners across the wallpaper and drywall, popping a little further on every blow, like a pane cracking around the door.** The leaf takes a shoulder-wide print (0.35 × 0.5 m) at 1.4–1.9 m and bows 20–40 mm. The head casing cracks at its mitre. At the break, drywall chunks drop on both sides and the leaf hangs creased across at shoulder height.

**Why.** It is the most literal "glass" read (a crack network spreading in pops), it reads from 12 m, and it makes the door the squeezed giant's moment: the room is too small for it, and you see the building give. Diagonal cracks from frame corners are real for frames in stud walls [02 §2.3]; the scale of them here is not.

**Risks.** The biggest build and the farthest from 1990 physics. The ceiling tiles are map geometry, so the tile jump needs either render-only overlay tiles or a map change. The opening must stay clear (no casing hanging below 2.1 m) and the bowed leaf must stay within 0.05 m of its collider (§1.3 item 4), so the leaf can only bow, not fold. It may compete with the Hunter's own silhouette work.

**Lead references.** Pending: `db03` (RE7, Jack bursts through the wall in one frame; back-lit; the hole stays), Lady Dimitrescu ducking through a doorway (wanted, `01` list). On disk: Giant B door and low pose renders (`scratchpad/creature_prev/giant_b/Giant_B_NightShift_door_a.png`, `_low_a.png`; the Hunter section HR16 in Figma), `../interaction_audit/images/70_relay_break_witness_t1000ms.png` (today's Relay at a door).

**Pre-render brief C.**
- **Members:** L0-F and L0-K, both on the Low↔Standard border with the Relay in the Low room (2.4 m, the deepest fold). BURST only, plus one RIP frame.
- **Stages and views:**
  - Stage strip, BURST, V1 at 76°: S0, M (tiles lifted 10–20 mm on P, dust curtain through the head gap), D1 (shoulder print, two corner cracks 300–500 mm long), D2 (frame racked 5–10 mm, head casing cracked at the mitre and lifted 5 mm, cracks 0.6–1.0 m with wallpaper torn along them, leaf bowed 20–40 mm), FINAL (drywall chunks in the air both sides), S4. Both members.
  - **V6 at 12 m** for D2 and S4 (the distance read is this direction's claim).
  - V4 far side at D2: **the Relay in pose `low` pressed under the 2.4 m ceiling against the door**, both forearms on the leaf at 1.5–1.9 m, tiles lifted round its back. The key frame of this direction.
  - V2 at D2 and S4: the crack network in depth.
  - V5 at S4: drywall chunks, gypsum dust, wallpaper strips on both floors.
  - One RIP frame, V1 from P at D2: the wall round the frame bulging toward S, cracks, the stop line splitting.
- **Count:** about 20 frames + 2 strips.
- **Build notes:** the cracks follow a generator like the glass's crack graph [GP §2.6] but on the wall plane: 2–3 main cracks from each head corner at 35–55° above horizontal, forking with probability 0.3, later cracks ending on earlier ones; each stage only adds segments. The racked frame is a 5–10 mm parallelogram shear; the shoulder print is a 3–6 mm dish on wood (crushed veneer, no hole) and 8–15 mm on steel; all of it stays inside the casing envelope and the leaf's ±0.05 m band.

---

## 9. Recommendation and decisions for Red

| # | Decision | Options | My recommendation |
|---|---|---|---|
| 1 | Direction | A, B, C, or a mix | **A on every door + B's lock hole as the D2/FINAL beat** on lever doors and steel doors. Judge C from the renders |
| 2 | Through-holes | Only the honest lock bore (B) / an art-directed fist-size hole near the latch [01 §6 item 6] / none (A only) | The honest bore |
| 3 | Steel takes longer | +0.5 s on locked doors, or the same | +0.5 s (steel keeps a dent mark at every tier) |
| 4 | Crooked hang | Honest 1° drop on wood, square on steel / today's 3° | 1° + a visibly torn top hinge |
| 5 | Particle module | Enable (shared with the glass) / scripted cards only | Enable |
| 6 | Media batch | Approve `media_candidates.md` sections 01, 02, 10 | Approve priority 1 |

---

## 10. Figma section outline (for the next step)

Section **FRONTROOMS · DOOR BREAK · VISUAL PROPOSAL** (x 59977, y 2000 on page 2099:76, [VISUAL_CHAT_TASKS DB1]). Frames 1920 × 1080 in the Hunter section's grammar.

| Frame | Content |
|---|---|
| DB00 | The problem: today's break is inert (audit frames 70–80) and Red's ask |
| DB01 | AAA: one model per state (L4D, Amnesia code facts), the swap on the hit, The Shining stills (when approved) |
| DB02 | Real doors: the latch fails, wood breaks, steel bends (forced-door photos, NILECJ figure when approved) |
| DB03 | The stage clock: blows → stages per tier, BURST and RIP (a diagram supporting the frames) |
| DB04 | Like the glass: glass stages beside door stages (§2.3) |
| DB05–DB07 | Directions A, B, C: lead reference, stage strip, hero frames, risks |
| DB08 | Wood vs steel language across the door family (§3) |
| DB09 | What changes and who does it (§7), budgets (§6) |
| DB10 | Decisions for Red (§9) |

---

## 11. Open items and UNVERIFIED

1. The 105° casing impact (§3.1 BREAK) is from the spec's 8.7 mm clearance at 95° and an estimated 0.9 mm per degree. Check on a capture.
2. The crooked-pose reading (`PoseLeaf` lifts the latch side) is from code, not a capture [02 §8.2].
3. All crack lengths, dent sizes, splinter counts and gap widths are [02]'s ESTIMATES or mine.
4. Enamel star-crazing around a dent (radial and ring cracks) is from coatings practice (the impact tests of organic coatings show cracking round the dent); I read no source for it today. A photo is on the media list (`media_candidates.md` section 10).
5. Whether URP decals are enabled in our renderer assets was not checked; the marks avoid them.
6. The breach light's cost in Forward+ with shadows is UNVERIFIED.
7. Giant B's finger fitting the 54 mm bore is from [SG]'s 0.32 m hand (finger ~33 mm, ESTIMATE).
8. Run and Exit members and the title stream's double doors are design-only (§3.4).
9. The hunter body (W1) is not chosen; Relay frames may need re-rendering.

---

## 12. Sources

Research: [01], [02] and their source lists (01 §8, 02 §11). Reused, not repeated: [GP] and `../glass/destruction/01_conference_destruction.md`.

Project files read (read-only): listed at the top of this file, plus `Documentation/VISUAL_CHAT_TASKS.md` (DB1, W1), `Tools/Blender/frontrooms_kit/assets/interact_door_leaf_veneer.py` (docstring), `interact_door_leaf_steel.py` (anchors), `creatures/giant_b_night_shift.py` (header, POSES, LENS_GLOW), `../interactables/proposal/media/` (file list), `Research/week02/ip-research/` (file list; no door-break media).
