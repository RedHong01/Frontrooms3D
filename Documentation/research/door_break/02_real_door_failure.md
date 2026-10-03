# 02 — How real 1990 doors fail under repeated blows (door break, DB1)

Status: RESEARCH, 2026-10-03. Nothing in the game or the real project was changed. No media was downloaded. Wanted media is listed in `media_candidates.md` (section "02").

Task (Red, 2026-10-03 ~12:05): break doors in stages, like the glass, with a different model for each damage state. This file answers one part: **how real doors of 1990 fail under repeated blunt blows**, and what that means for the FrontRooms door family and the Relay's blows. How AAA games stage door damage is a separate file. AAA destruction in general is already covered in `../glass/destruction/01_conference_destruction.md` and `10_glass_destruction_plan.md`, and is not repeated here.

**Binding inputs:** `../interactables/00_map_constraints.md`, `../interactables/10_spec.md` (the door family), `../interactables/05_locked_door_type.md`, `../interaction_audit/10_audit_report.md` §3.7 and F11, `Assets/Scripts/FrontRoomsShots/FrontRoomsShotTimings.cs` (`DoorBreak`), `../hunter/11_squeezed_giant.md`, `../office_and_film/22_era_lock.md`.

**Tags**
- **[read]**: I opened the page or document and quote it.
- **[search]**: taken from a search-engine summary. The page itself was not opened.
- **ESTIMATE**: my number or inference, with no direct source.
- **UNVERIFIED**: nobody has checked it in a test or a capture.

**Words used**
- **P** = the push side, where the stops are. **S** = the swing side, the room the leaf opens into (`10_spec.md` §1.2).
- **BURST** = the Relay is on P and drives the leaf into the far room S. The player is usually on S, so the leaf comes toward the player.
- **RIP** = the Relay is on S and pulls the leaf toward itself. The player is on P, behind the stops, so the leaf goes away from the player.
- **Strike** = the metal plate on the frame that the latch goes into. **Jamb** = the side of the frame. **Stile** = the side edge of the leaf. **Lock stile** = the latch edge of the leaf.

---

## 0. Short answer for Red

1. **A real door almost never fails in the middle. It fails at the latch.** Every blow ends up as a load on the latch, then the strike, then the jamb. The 1975 US government door test names exactly five parts that can fail: the door, the hinges, the lock, the jamb/strike and the jamb/wall (S1). In wood frames, the jamb at the strike is what breaks first. Three patents from 1976, 1988 and 1992 and Consumer Reports' kick tests all say so (S3–S5, S17, S18).
2. **Wood breaks, steel bends.** Fire-service teaching puts it in one line: "Steel bends, wood breaks or shears and brick/glass shatters" (S15). So the two FrontRooms door types need two different damage languages:
   - **Wood (all FREE doors):** cracks that run along the grain, a jamb that splits beside the strike, splinters, a strike plate that tears out with a strip of wood, casing that pops off.
   - **Steel (all LOCKED doors):** dents that stay, a leaf that bows, a lock edge that crushes, an edge seam that opens, a frame that spreads, bolts that bend. Paint flakes off, drywall cracks and dust falls. **No splinters.**
3. **Each blow leaves a permanent mark, and the end is sudden.** That is the same rhythm as the glass, so Red's idea is physically right. Wood cracks grow a little with each blow, like glass cracks. Steel dents add up. Then the latch clears the strike (about 9.5 mm of frame movement for a 1/2" latch, about 22 mm for a 1" deadbolt) and the leaf is free in one frame.
4. **The standard test of the period is itself a staged sequence.** NILECJ-STD-0306.00 (1975) hits a locked door twice each at 80, 120, 160 and 200 J: eight blows of rising energy (S1). Consumer Reports swings a 100 lb steel ram eight times, higher each time (S17). FrontRooms' five blows at 0.5 s can borrow that structure.
5. **Steel doors really do take longer.** On metal-skinned entry doors, one hit broke a wood-edged stile, while a steel edge needed seven (a 1990s Premdor brochure, quoted in S18). Breaching instructors rank hollow metal as needing "maximum force" (S16). Red's optional lever "steel doors take longer" (`05` §8 item 4) has real support. ESTIMATE: one extra blow.
6. **Each Relay blow is about 2–3× the strongest blow in the 1975 test** (ESTIMATE, §6). A real 1990 wood office door would give on the first or second such blow. Five blows is a game choice. A good reason to keep it: the Relay is crammed under the ceiling and cannot wind up.
7. **Light comes through the latch edge, not through the leaf.** A solid-core or steel leaf does not let light through until the end. Light shows first as a pulse along the latch edge on each blow, then as a thin permanent line and two wedges at the top and bottom latch corners, then as the full opening (§4).
8. **Two realism notes on today's plan** (§8): show damage **on a blow, never between blows**; and a leaf with a torn top hinge **drops** at the latch side by about 1° until its corner touches the floor. As I read the code, today's 3° crooked pose lifts the latch side instead.

---

## 1. The FrontRooms door family, built the way a 1990 building would build it

### 1.1 What each member really is

The spec's members (`10_spec.md` §2.1) map onto three real constructions. The failure order follows from the construction.

| Group | Members | Leaf (1990 build) | Frame | Lock and strike | Fails first |
|---|---|---|---|---|---|
| **W-W**: wood leaf, wood frame | L0-F | Flush **solid particleboard core**, 5-ply: face veneer about 1/40" (0.6 mm), crossband 1/16" (1.6 mm) with its grain across the face grain, particleboard core 28–32 lb/ft³, hardwood stiles at least 1-1/8" (28.5 mm) after trimming. 1-3/4" (44 mm) thick. About 43–55 kg for the game's 0.98 × 2.08 m leaf (S20, S23; mass ESTIMATE from 4.3–5.5 lb/ft²). The IP canon says "solid-core doors" (`05` §3). | Wood jamb with an **applied 16 mm stop** and casing nailed on both faces (`10_spec.md` §2.3) | **Bored** lever lockset: a 2-1/8" (54 mm) cross bore and a 1" (25 mm) edge bore in the lock stile, a 1/2" (12.7 mm) deadlatch. A thin strike held by **two short wood screws** | **The jamb beside the strike splits along its grain.** The strike tears out with a strip of wood. |
| **W-S**: wood leaf, steel frame | OF-F, EX-F, RN-F | As W-W, oak veneer. RN-F has a plastic-laminate face and armor plates. | 16 ga pressed-steel frame with a **formed (integral) stop**, in a stud and drywall wall. The strike is screwed to a steel reinforcement (S21). | OF-F: bored lever as W-W. EX-F: a rim exit device on P. RN-F: **no latch** (push/pull, closer only). | **The leaf's lock stile.** The steel strike holds, so the latch tears out through the wood around the bore and the stile splits. |
| **S-S**: steel leaf, steel frame | L0-K, OF-K, RN-K, EX-K | **Hollow-metal**, 18 ga (1.0 mm) face sheets (SDI Level 2, `02` §2.1), kraft-paper honeycomb core (the usual interior core, S24), a visible edge seam on both stiles, top and bottom channels, 10 ga hinge reinforcements and a lock reinforcement inside (S21, S25). About 43–49 kg for a 3'0" × 7'0" door (S34). | 16 ga steel frame, as W-S. In 1990 offices usually a **knock-down drywall frame** held to the studs by anchors (S2, S25; the wall type is ESTIMATE). | **Mortise** lock: a deadlocking latch (3/4" throw, typical [search]) and a **1" (25 mm) deadbolt** (`10_spec.md` D6), into a 4-7/8" strike on the frame's strike reinforcement (S21). | **The frame spreads and the lock edge crushes.** Then the bolts bend or shear. The skins dent and the leaf bows. |

**Hollow-core wood (not in the kit).** It is listed for contrast and as a fallback, because cheap 1990 interiors had it. Two 1/8" (3 mm) skins of hardboard or lauan plywood are glued over a paper honeycomb with about 2" cells, with 2-1/4" stiles and a lock block about 20" long (S22; S2 defines the type). It weighs about 13–17 kg (S23, ESTIMATE). It punches through: "a massive hole was ripped right through the center of the door" (S27).

### 1.2 Why these weak links (one load path)

A blow on the leaf has only four ways out:
1. through the **latch or bolt** into the **strike**, then into the **jamb**, then the **wall**;
2. through the **hinges** into the hinge jamb;
3. in BURST, onto nothing else, because the leaf is moving **away** from the stops;
4. in RIP, if the Relay hits instead of pulling, into the **stops** all round.

So in both FrontRooms cases (the Relay always breaks toward S, `00`), **the latch carries the whole load and bears on the S side of the strike opening.** The wood or steel between the strike opening and the S face of the jamb is what fails. Real reports put the weakness exactly there:
- "The hollow space is close to an edge of a jamb leaving only a thin layer of wood to break away in order to force entry" (La Beaud, filed 1976, S3).
- "A shoulder thrust or a hard kick is usually sufficient to break the jamb and open the door" (S3).
- Strike screws are "frequently no longer than 3/4" and frequently only 1/2"" and the strike is "rarely more than 1/16" in thickness" (Lozano, filed 1988, S4).
- "a well placed kick will usually break the striker plate from the doorjamb fracturing the jamb in the process" (Schimpf, filed 1992, S5).
- Consumer Reports in the 1990s: "Door frames often split with little force applied" (quoted in S18). Its later ram tests again found the jamb splitting at the strike, usually because of the short strike screws [search] (S17).

**The latch only has to clear the strike.**
- A bored latch throws 1/2" (12.7 mm). With the 1/8" (3 mm) edge gap, it is engaged by only about **9.5 mm**.
- The 1975 test checks exactly that: the jamb must not spread an extra 3/8" (9.5 mm) for classes I–II, or 1/2" (13 mm) for III–IV, under 6–22 kN (S1).
- A 3/4" mortise latch is engaged about 16 mm, and a 1" deadbolt about 22 mm (ESTIMATE from the throws).
- That is why the deadbolt doors need the bolt to **bend or shear**, or the frame to spread a long way, before they open (S11: a deadbolt's "significantly longer throw" needs a much bigger spread).

**Wood fails along its grain.** Wood is weakest in tension across the grain: "its ability to resist this stress is minimal", and the failure is sudden and brittle (S30). A jamb has its grain running vertically, so the crack runs **up and down** from the strike: "usually splitting the strike side jamb somewhere above the deadbolt strike plate and somewhere below the doorknob latch strike plate" ([search]: a jamb-reinforcement patent's background found next to S5; also S29).

**Steel fails by bending.** "Metal doors are different than wood because the metal must bend to the point of permanent deformity to release the bolt" (S15). "Steel and aluminum have a ductile capability; they can bend and stay deformed if bent far enough" (S15).

---

## 2. How each group fails, blow by blow (real behaviour)

The blows here are big, close to the latch, and repeated. They have the energy of a ram (§6). "Blow 1, 2…" is the real order of events, not game time. §7 maps it onto the Relay.

### 2.1 W-W: veneer door in a wood frame (L0-F)

| Order | What happens | Evidence |
|---|---|---|
| 1 | **Elastic.** The leaf jumps toward S, the latch bears on the strike, everything springs back. Paint or varnish crazes in a hairline at the strike and along the stop/casing joints. Dust shakes off the head. The free top and bottom latch corners move most, because the latch holds the leaf at 1.0 m only. | Firefighters "push on the top or kick the bottom of the door to determine how much play there is" (S12); "push the door at the top, bottom and center" (S13). |
| 2 | **The jamb starts to split** at the strike, on the S side of the mortise. A crack runs up and down the grain from the strike, 100–300 mm each way (ESTIMATE). The strike's two screws start to pull. The casing on S over the latch jamb lifts a few mm as the jamb piece rotates. | S3, S4, S5, S29 |
| 3 | **The split opens** to 2–5 mm (ESTIMATE). A strip of jamb, roughly 15–25 mm wide and 300–500 mm long (ESTIMATE), is now held only by its ends and the casing nails. The strike sits crooked. The leaf no longer returns fully to its stop: it rests a few mm off it. | S5 ("fracturing the jamb"), S1 (jamb/strike failure = "splitting, bending, or fracture of the door jamb at the strike") |
| 4 | **The strip breaks out.** Long splinters stand proud. The strike hangs on one screw or lies on the floor. The casing at the latch side pops off its nails and stands 5–20 mm off the wall (ESTIMATE). | S29 ("check the surrounding trim, or casing, for cracks or separation from the wall") |
| Break | The latch clears. The leaf swings open fast. The latchbolt usually stays in the leaf, often bent. If the leaf hits something at the end of its swing, the hinges are levered: "This would exert leverage on the hinges that could tear them out" (S7). Hinge screws are short ("frequently only 1/2"", S4), so the **top hinge** pulls first, because it carries the leaf's weight in tension (S35 [search]). | S4, S7, S1 (hinge failure = "damage to the leaves or pin"; jamb/wall failure = "pull-out of the attachment screws") |

**What the leaf itself does.** A solid particleboard-core leaf rarely breaks from blows like these: in a rage-room test, a solid-core door took heavy objects with "a few nicks and scratches" (S27). Two marks do appear on it:
- **Bending cracks on the far face.** A blow bends the leaf, and the face away from the blow is stretched sideways. The face veneer's grain is vertical, so it splits **along** the grain: short vertical hairlines near the latch stile (physics; ESTIMATE). In BURST these face the player.
- **A crushed print** where the blow lands, with veneer torn around the rose if the blow hits the lockset.

### 2.2 W-S: veneer door in a steel frame (OF-F, EX-F, RN-F)

The steel frame does not split. "Whereas a wood frame may splinter, the concrete-filled metal frame doesn't even give" (Pressler 2000, S6). A drywall frame does give a little, but it stays whole. So **the leaf's lock stile becomes the weak link.**

| Order | What happens | Evidence |
|---|---|---|
| 1 | Elastic, as W-W. The steel frame rings, a dull "tunk" (ESTIMATE). Dust and joint-compound flakes fall at the frame's head joints. | — |
| 2 | **The lock stile starts to split.** The latch is a 25 mm tube in a 25 mm edge bore, beside a 54 mm cross bore. Only about 43 mm of wood is left between the bore and the edge with a 2-3/4" backset (ESTIMATE from standard bore sizes). The edge band cracks along its glue line. | Wrap-around plates exist because doors split there; they reinforce "right where most doors fail: around the latch and deadbolt" (S36 [search]). One hit "broke the wood-edged stile" (Premdor, S18). |
| 3 | The split runs vertically from the bore through the stile, 150–400 mm (ESTIMATE). The **rose is pushed crooked** into the face; the veneer tears around it. Particleboard crumbles at the break: it is granular, not stringy (S33 [search]: particleboard does not hold screws well, which is why doors get solid blocking). | S20 (stile edge "split resistance" is a tested property, 525 lbf by NWWDA TM-5) |
| 4 | **The lock stile breaks out** around the latch. The latch unit, or the whole bored lockset, starts to pull out of the bore toward S. The strike on the steel frame bends: its lip curls toward S. | S1 (door failure = "any splitting or fracture of the door which allows it to be opened") |
| Break | The latch tears through the edge. The leaf opens with a **ragged notch in its lock edge** at 1.0 m and the lockset hanging from it or knocked out. If the lockset is out, **the 54 mm cross bore is a round hole right through the leaf** at handle height. | ESTIMATE from the bore geometry |

**Members with special hardware (P2; the map has no doors for them yet):**
- **EX-F (exit device on P).** In BURST, a push on the bar opens a real exit door. There is nothing to break. The honest version is one shove: the bar's end cases crack and the leaf flies open. In RIP, the surface-mounted rim strike tears off the frame. ESTIMATE.
- **RN-F (no latch).** Only the closer holds it. A thrown leaf takes "a violent shock load at full extension", which can bend the arm, crack the closer body or strip its screws (S31 [search]). The "break" is the closer arm snapping off its shoe, with oil spotting the floor (oil ESTIMATE).

### 2.3 S-S: hollow-metal door in a steel frame (all LOCKED members)

| Order | What happens | Evidence |
|---|---|---|
| 1 | **A dent that stays.** Where the blow lands, the 1 mm face sheet dishes in a shallow round dent, 100–250 mm across (ESTIMATE). The paper honeycomb behind it crushes. The enamel crazes in rings around the dent, and flakes to grey primer at its rim (`05` §6.2 primer; ESTIMATE). The leaf rings: a hollow metallic boom. | "If the door is impacted in a way that causes a hole... When the damage is just a dent and hasn't penetrated through the outer layer" (SDI, S26). "Steel ... can bend and stay deformed" (S15). |
| 2 | **The leaf bows** around the lock. The lock stile moves toward S by a few mm and does not come all the way back. The 1/2"–1" bolts now bear on the strike's S edge. The rubber **silencers** on the frame stop are crushed and one may pop out. In a drywall frame, the latch jamb **flexes and spreads**; the joint compound along the frame's face cracks. | Frame spread is the standard burglar and fire-service attack on this kind of opening (S2: "jimmying"; S11). Wood-stud walls "allow the frame to 'bow out'"; a metal frame in block "could suffer significant damage" (S11). |
| 3 | **The lock edge starts to crush.** The steel edge channel folds near the lock. The **visible edge seam opens** for some distance above and below the lock, showing the brown paper honeycomb inside (ESTIMATE for length: 100–300 mm). The frame's strike area bends: the formed stop kinks toward S. Drywall cracks run diagonally from the frame corners. | "Most steel doors have a seam ... on the interior-edge ... that runs the entire length"; a bad attack ends "peeling the door apart" (S9). "Work your tool up and down to crush the door edge and spread the door away from the frame" (S9). SDI lists kraft honeycomb as a core (S25). |
| 4 | **The bolts bend.** The deadbolt is levered in its strike; the latch tongue twists. The strike plate's screws start to shear or strip. Dents on the face join up into a broad dish around the lock. | S1 (lock failure = "any damage to the lock mechanism or bolt which allows the door to be opened") |
| Break | The bolts **bend out** of the strike, or the deadbolt **shears**. A sheared tip stays in the strike. The leaf opens **whole**: it does not splinter and it does not come off its hinges. The hinge reinforcements are welded and hold (SDI notes only that welds can break under abuse, S26). The leaf keeps a permanent bow and a crushed lock edge. The frame stays spread, with a gap at the strike. | Gustin: prying "exerts considerable stress to a dead bolt, often breaking it away from the lock mechanism and pulling it out of its strike" (S8). "Doors subjected to vandalism or forced entry typically need to be replaced" (S26). |

**Masonry vs drywall.** In a grouted masonry wall the steel frame "doesn't even give" (S6), so all the damage goes into the leaf and the lock. FrontRooms walls are 0.16 m thick with wallpaper or painted drywall, which reads as a stud partition. So **the frame should visibly spread** and **the wall should crack and shed gypsum dust** at the frame. ESTIMATE: Office partitions as steel studs, Level 0 as wood or steel studs; both crack the same way.

**Where to hit.** Breaching guidance aims "halfway between the doorknob and the frame" (S16). Fire service: start "about six inches from the center of the lock"; further away, "the door [will] bend and crush but the lock will not break" (S9). So **blows near the lock break the lock; blows mid-leaf only dent and bow the leaf.** The same holds for a spreading tool set too close to a lock: "the exterior skin ... will be the first metallic component to fail", because locks and hinges have "much less ductility" than the skin (S10, written about car doors, the same physics). The Relay's blows should land within about 0.15–0.35 m of the latch edge, at 1.1–1.6 m high (its door pose puts its shoulder and face there, `11_squeezed_giant.md`). Mid-leaf dents on steel can stay as the first marks.

### 2.4 Hollow-core wood (contrast only)

- The struck skin **punches through** at the blow. The hole is ragged; on lauan plywood it tears along the grain, so it is taller than wide. The far skin delaminates and bulges. Torn paper honeycomb shows inside (S22, S27; shape ESTIMATE).
- "Hollow core doors require minimal force to breach" (S16). The NBS definition: "some interior hollow core doors have nothing except perimeter stiles and rails separating the facing sheets" (S2).
- Use only if Red wants a "cheap" door variant. It is the only type where blows make **holes through the leaf** that pass light.

### 2.5 Hinges, stops, casing and the end of the swing

- **Hinge pins** rarely shear from blunt blows. Pins are cut with saws or driven out with tools in breaching. Under force, the leaves bend and the screws pull (S1 hinge failure definition; S8 on fasteners: strikes "result in shearing the fastener or at least loosening it"). ESTIMATE: keep hinge damage to a torn top hinge on wood frames and a bent leaf on steel.
- **Stops.** In both FrontRooms cases the leaf moves away from the stops, so **the stops survive.** A stop only breaks if the Relay hits the leaf from S, driving it into the stops. Then the applied wood stop splits off its nails, and a steel formed stop folds. (Wood frames have an applied stop; in metal frames the stop is part of the frame, S14.) That is not today's rule, so it is a variant only.
- **Casing** is nailed through the wallpaper to the jamb edge. When the jamb strip moves, the casing over it pops 5–20 mm off the wall and the wallpaper tears along its edge (ESTIMATE).
- **The end of the swing.** The game throws the leaf open in 0.12 s (`DoorBreak.ThrowSeconds`). That is about 13.5 m/s at the latch edge and about 1.5 kJ for a 50 kg leaf (ESTIMATE). A real door thrown that hard hits whatever is behind it: a base stop (bent or snapped), a closer arm (bent, shoe torn off, S31), or the wall. A lever or knob driven into drywall leaves the familiar **round dent or hole at 1.0 m** (S37 [search]). After the impact, the leaf rebounds and hangs off its torn hinge. That is the physical reason for `BounceRestDeg` 80 and `CrookedDeg`.

---

## 3. What stays, what falls

Everything below is render-only (`00`: no colliders; the opening must stay passable). Sizes are ESTIMATES unless a source is given.

| Piece | Group | Size | Count per break | Where it lands | Stays or falls | Evidence |
|---|---|---|---|---|---|---|
| Jamb strip with the strike still screwed to it | W-W | 15–25 × 300–500 mm, 15–20 mm thick | 1 | Toward S (in both cases the latch pushes it to S) | falls, or hangs from one end | S3, S5, S17 |
| Long splinters, along the grain | W-W, W-S (stile) | 50–300 mm long, 2–10 mm thick | 6–15 (the audit asked for 8–15, §3.7) | Fan toward S, mostly below 1.3 m | fall; some stay standing out of the break | S6, S12 ("splinter, crack, or disintegrate") |
| Strike plate | all | bored 0.0286 × 0.0572; mortise 0.032 × 0.200 (spec) | 1 | Within 1 m of the latch jamb on S | W-W: falls with the jamb strip or alone. S-S: often stays, bent, on one screw | S1, S4 |
| Strike and hinge screws | W-W | #8–#12 wood screws, 13–25 mm (1/2"–1", S4) | 2–6 | Floor near the jamb; wood fibres on the threads | fall | S4 |
| Particleboard crumbs | W-S | 1–5 mm granules | a handful at the stile break | Below the lock stile | fall | S33 |
| Veneer and finish flakes | wood | 5–30 mm, paper-thin | many | Below the blow point | fall | ESTIMATE |
| Enamel paint chips, grey primer behind | S-S | 2–15 mm | many, around dents | Below the dents and the lock edge | fall | ESTIMATE; `05` §6.2 |
| Rubber silencers | S-S, W-S | Ø 8 × 2.5 mm (spec) | 1–2 | Floor at the latch jamb | fall | ESTIMATE |
| Drywall crumbs and gypsum dust | W-S, S-S (drywall frames) | dust and 5–30 mm crumbs | puffs at each blow | Along the jamb and at the head corners | fall; the dust hangs in the light | S11; ESTIMATE |
| Dust from the head | all | dust | one puff per blow | From the head joint (`head_dust_a/b` anchors) | falls through the light | audit §3.7 |
| Casing (latch side) | W-W | full height | 1 | Stays on its upper nails, 5–20 mm off the wall | stays | S29 |
| Deadbolt tip (sheared) | S-S | Ø about 16 × 25 mm | 0–1 | Stays inside the strike | stays | S8 |
| Lever, knob or rose | W-S, S-S (RIP) | — | 0–1 | Hangs bent, or knocked out of a bored lock | stays or falls | S1 knob impact test |
| Closer arm | OF, RN, EX | 0.24 + 0.26 m arms (spec) | 0–1 | Snaps at the shoe; hangs from the body | stays on the leaf | S31 |
| The leaf | all | — | 1 | Swings open and hangs | **stays on its hinges** | S8, S26; nothing in any source took a leaf off its hinges with blunt blows |

---

## 4. Light: where and when it leaks

**Rule: a solid-core or steel leaf passes no light until it fails. Light leaks at the latch edge and its corners.** The stop overlaps the leaf by 13 mm, with a 3 mm silencer gap behind it (`10_spec.md` §1.4). So the leaf must move more than about 16 mm toward S before there is a straight path past the stop. Before that, light can only bounce along the 3 mm edge channel. It shows as a thin **glow**, not a view.

| Stage | BURST: the player on S looks at the S face | RIP: the player on P looks at the stops | Evidence |
|---|---|---|---|
| Each early blow | The leaf jumps 4–8 mm (`DoorBreak` jolt). For about 0.12 s the slot behind the stop opens: **a pulse of far-side glow runs along the latch edge, strongest at the top and bottom corners**, then shuts. | The gap between leaf and stop flashes open as the leaf is pulled, **strongest at lock height**, because the pull comes through the lever. | `00` slits; S12, S13 (corners have play) |
| After the jamb or stile splits | The leaf rests 2–10 mm off its stop: **a thin, steady glow line** on the latch edge, and **two glowing wedges** at the top and bottom latch corners, where the free corners sag forward. | A steady dark-and-glow slot along the stop, widest at 1.0 m, narrowing toward the corners. | ESTIMATE from the stop geometry |
| Just before the break | The glow line widens to 10–20 mm along most of the latch edge. Gypsum and head dust drift through it as **visible beams**. | The same from P. The bent latch or bolt may show in the slot as a dark tongue. | ESTIMATE |
| Holes in the leaf | Only in **hollow-core wood** (punch-through) and in **W-S when the bored lockset is knocked out**: a 54 mm round hole at 1.0 m, lit from behind. Solid-core and steel leaves have no through-holes. | Same | §2.2, §2.4 |
| Break | The full opening: the Relay back-lit by the far room (`DoorBreak.RevealHold` 0.3 s). | The leaf swings away and the Relay's room lights the opening. | audit §3.7 |

The first two rows matter most. **The light pulse on each blow** is cheap: a transform jolt already exists (`MapWorld.JoltForBlow`). It reads at distance, and it tells the player "the next blow could be the one" before any model changes.

---

## 5. Sound, per material (for the sound chat)

| Group | Each blow | Damage growing | The break | Evidence |
|---|---|---|---|---|
| W-W | A heavy dull thud (a 45–55 kg leaf), a short rattle of the lever and latch, and a woody **crack** from the jamb from blow 2 | Crackle of fibres tearing, a nail squeal from the casing, a screw tick | A sharp split, splinters skittering, the leaf whoosh, then a slam at the end of the swing | S3–S5; DOOR_FOLEY_SEGMENTS.md already lists a wooden door kick/break recording, freesound 160213 |
| W-S | As W-W, plus a dull ring from the steel frame | Crack from the leaf edge; the rose grinding on the face | A tearing split at the lock stile; the lockset clatters | ESTIMATE |
| S-S | A **hollow metallic boom** with a short ring; the paper core damps it (ESTIMATE). Silencers thump. | The pitch drops as the dents deepen and the leaf stops ringing (ESTIMATE). Drywall crunch, frame creak | A metallic **snap or bang** as the bolt bends out or shears, then a boom at the end of the swing | Gustin: firefighters hear the change from a metallic "clang" to a solid "thud" when the tool reaches the steel jamb (S7) |
| Hollow-core | A light, boomy drum | A crack and paper rip | A burst through the skin | S27 |

`Mechanism/Door/Blow` already carries a `Damage` parameter (`SoundDirector.cs:533-538`). A `doorType` parameter (wood vs hollow metal) was already asked for in `05` §8 item 9.

---

## 6. The Relay's blows against real numbers

| Blow | Energy | Source |
|---|---|---|
| NILECJ-STD-0306.00 (1975) door impact test | 2 blows each at 80, 120, 160, 200 J; 6" (15 cm) hemispherical steel ram on a pendulum | S1 [read] |
| NILECJ bolt and hinge impact tests | the same 80 → 200 J ladder, aimed 20 cm in from the lock edge, or 20 cm from the bottom hinge | S1 [read] |
| ANSI/BHMA A156.2 (bored locks), Grade 1 impact | 2 blows each at 60, 90, 120 ft-lbf (81, 122, 163 J) | S19 [read] |
| Consumer Reports kick test | a 100 lb (45 kg) steel ram, 8 swings, each from higher up | S17 [search] |
| Enforcer police ram | 16 kg; "more than three tonnes of impact force" | S32 [read]. Energy at a 6–8 m/s swing: 290–510 J (ESTIMATE) |
| **The Relay** | **250–550 J per blow (ESTIMATE).** A 3.1–3.3 m heavy body is about 1.75³ ≈ 5.4× a person's mass, so about 450–540 kg. A short shoulder shove with about 120 kg moving at 2–3 m/s gives 240–540 J. A forearm hammer with a 27 kg arm at 6 m/s gives about 490 J. | ESTIMATE, from `11_squeezed_giant.md` sizes |

**What this means:**
- Each Relay blow is **1.2–2.7× the top blow** of the 1975 test.
- A 1990 wood office door in a wood frame, with its two short strike screws, is a class I door at best. It would give on **blow 1 or 2**.
- A hollow-metal door in a steel frame with a 1" deadbolt might take **2–4** (ESTIMATE; S18's 1 vs 7 strikes points the same way).
- **Five blows is a game choice.** It is easy to justify, because the squeezed giant **cannot wind up**: its back is pressed to the ceiling tiles (`11_squeezed_giant.md` rule 2). Its blows are short shoves, not swings. The first blows probe; the last ones commit.
- The 1975 test and Consumer Reports both use a **rising ladder of blows**. The Relay's blows should look and sound stronger each time. The jolt already grows from 4 to 8 mm and the shake from 0.2° to 0.6° (`DoorBreak`).

---

## 7. Mapping onto FrontRooms

### 7.1 Which blow shows which state

Facts: one blow every 0.5 s from 0.5 s, the last one 0.1 s before the door gives, `BlowCount` = round(break time ÷ 0.5) (`FrontRoomsMapHunter.cs:132-135, 985-994`). At 2.5 s that is 5 blows at 0.5, 1.0, 1.5, 2.0 and 2.4 s, and the break at 2.5 s. Tiers shrink the break time down to 1.3 s, which gives 3 blows.

Real doors change state **at a blow**, never between blows. Today `Damage1At` 0.5 and `Damage2At` 0.8 are fractions of the break time (1.25 s and 2.0 s). 1.25 s falls between blows 2 and 3. **Recommendation:** count back from the last blow, so every tier gets the same story.

| Blow | Name | 5 blows (2.5 s) | 4 blows (≈2.0 s) | 3 blows (≈1.3 s) |
|---|---|---|---|---|
| all but the last three | **MARK**: no model swap. Jolt, light pulse, dust, decal-level marks (crazing, a first dent on steel). | 1 (0.5 s), 2 (1.0 s) | 1 (0.5 s) | — |
| N−2 | **D1**: first model state | 3 (1.5 s) | 2 (1.0 s) | 1 (0.5 s) |
| N−1 | **D2**: second model state | 4 (2.0 s) | 3 (1.5 s) | 2 (1.0 s) |
| N (0.1 s before the break) | **FINAL**: the release. The leaf twitches, debris starts, the latch clears. | 5 (2.4 s) | 4 (1.9 s) | 3 (1.2 s) |
| break | **BROKEN**: throw, bounce, hang | 2.5 s | 2.0 s | 1.3 s |

With 5 blows this lands D1 at 60 % and D2 at 80 % of the time. That is close to today's 50 % / 80 %, so the shot plan barely moves.

If Red takes "steel doors take longer": +1 blow for S-S (6 blows at 2.5 + 0.5 s; ESTIMATE). That is a map tuning field, so it is a contract request, not a visual change.

### 7.2 What each state must show (both cases)

**W-W (L0-F)**

| State | Leaf | Frame | Seen in BURST (player on S) | Seen in RIP (player on P) |
|---|---|---|---|---|
| MARK | crazed finish at the blow; nothing structural | hairline at the strike; dust | lever rattles; glow pulse at the corners | glow pulse at lock height; the stop line flickers |
| D1 | short vertical veneer splits on the S face near the lock stile (bending); crushed print on the P face | **jamb split started**: a 200–400 mm vertical crack above and below the strike; strike tilted; S casing lifted 2–3 mm | split jamb and crooked strike right in view; steady glow line | leaf sits 2–5 mm off the stop at 1.0 m; the P casing is untouched |
| D2 | as D1, plus the latch edge rubbed and dented | **jamb strip loose**, 3–5 mm open, splinters standing out; strike hanging; S casing 10–20 mm off | splinters toward the player; corner wedges of light | a wider slot at 1.0 m; the lever on S is gone from view; dust in the slot |
| BROKEN | leaf intact, latchbolt bent; top hinge torn: its screws out of the jamb, the leaf dropped about 1° at the latch side | strip and strike gone (on the floor); a raw notch in the jamb at 1.0 m; torn screw holes at the top hinge | the leaf comes at the player and rebounds; the notch and the floor debris stay | the leaf swings away; from P the notch is on the far face of the jamb, so the player sees the torn edge in profile |

**W-S (OF-F; EX-F and RN-F per §2.2)**

| State | Leaf | Frame | BURST | RIP |
|---|---|---|---|---|
| MARK | crazed finish; the rose rocks | dust; compound flakes at the head joints | glow pulse | glow pulse |
| D1 | **lock stile split** from the bore, along the edge band; rose pushed crooked, veneer torn round it | strike lip starting to curl toward S | crack in the lock edge and the crooked rose in view | slot at lock height |
| D2 | split open 3–5 mm; particleboard showing in the crack; the lockset loose in its bore | strike bent; silencers flattened | crumbs falling below the lock; light leaking through the cracked stile | wider slot; the lever on S pulled out of line |
| BROKEN | a **ragged notch** in the lock edge at 1.0 m; the lockset hanging or knocked out, leaving a 54 mm hole | strike bent but in place; frame whole | notch and hole face the player; light through the hole | the leaf leaves with its notch; the frame is clean but for the bent strike |

**S-S (all LOCKED members)**

| State | Leaf | Frame | BURST | RIP |
|---|---|---|---|---|
| MARK | 1–2 shallow dents on P with paint crazing (BURST); none in RIP | dust | dents are on the far face, so the player only sees a faint **bulge** on S; boom; glow pulse | knob twisted 3–5° (`05` §8 item 7 hard stop); glow pulse |
| D1 | dents joined into a broad dish round the lock; the leaf **bowed** 5–10 mm at the lock stile; paint flaking to primer at the dent rims | silencers crushed, one popped; **frame spread** 3–5 mm at the strike; compound cracks along the frame face | bow and flaking on S; diagonal drywall crack from the head corner | slot at lock height; the stop line kinks |
| D2 | **lock edge crushed**; **edge seam open** 100–300 mm round the lock, brown honeycomb showing; armor front bent | strike reinforcement bent; drywall cracked and shedding at the jamb | honeycomb visible in the edge; gypsum dust in the light | knob loose or torn off, leaving its spindle; bolt glint in the slot |
| BROKEN | leaf whole, permanently bowed; bolts bent out, the deadbolt tip may be sheared; **no splinters** | frame spread at the strike; strike hanging on one screw; drywall broken at the jamb | leaf comes at the player, rebounds, and hangs square (hinges hold) | leaf swings away; the frame damage faces the player |

**Minimum model set** (one for each row group; render-only children of `Leaf rig` and the frame):
- **Leaf:** `_D1`, `_D2`, `_Broken` per leaf type (veneer, steel; laminate later).
- **Frame strike zone:** a replaceable 0.5 m piece at the strike per frame type (wood, steel), `_D1`, `_D2`, `_Broken`. The kit's wood frame models only a 2 mm lining over the map's wall end (`10_spec.md` §1.4), so a split jamb needs this piece to have real depth: ESTIMATE 15–20 mm.
- **Debris set** per group (§3).
- **Decals** for MARK: crazing, dents, a wall hole for the lever.

### 7.3 Hard limits from the map (unchanged)

- The leaf collider stays 0.05 × 2.08 × 0.98 under `Door hinge {a}-{b}`, and its name stays (`00`).
- Damage geometry, splinters and debris have **no colliders**.
- After the break, nothing may hang inside the 1.0 × 2.1 opening. Notches and loose strips stay within the jamb and casing envelope (`10_spec.md` §1.4: X ±0.105, Z −0.075 → 1.075). Floor debris lies flat.
- Jolts go on the leaf child, never the hinge (audit F11; `10_spec.md` §6.5 item 6).

---

## 8. Realism notes on today's plan and code (read-only; owners decide)

1. **Damage only on a blow.** `Damage1At` 0.5 lands at 1.25 s, between blows. Use the count-back rule in §7.1. Owner: visual chat (`FrontRoomsShotTimings`).
2. **The crooked hang.** `PoseLeaf` turns the broken leaf in its own plane about the top of its hinge edge (`FrontRoomsMapWorld.cs:2219-2230`; the leaf home is (0, 1.04, 0.50), set at `:1097`). As I read the math (Unity `AngleAxis(-3°, right)` about the top), the bottom of the hinge edge moves about 109 mm toward the latch, and the latch side **rises** about 52 mm.
   - A real leaf with a torn hinge does the opposite. When the top hinge tears (the top hinge carries the weight in tension), the leaf turns about its lower hinges. Its latch side **drops** until the bottom latch corner touches the floor. With the 15 mm floor gap that is only about 0.9°. The top of the hinge edge then stands about 28 mm off the jamb.
   - The visible cue is the torn top hinge (screws out, a gap at the top) and a corner scraping the floor, not a large tilt.
   - This is a map file, so it is a contract request: in-plane drop about 1°, pivot at the bottom hinge, latch corner down to the floor (UNVERIFIED in a capture).
3. **The jolt direction suits both cases.** `JoltForBlow` always jolts toward S (`MapWorld:2201-2207`). That is right for BURST (pushed away) and RIP (pulled toward the Relay). No change.
4. **Exit and Run doors (P2).** A real exit door opens with a push on its bar, and RN-F has no latch. In BURST these should open with one shove, not five blows. A rule for the map chat when those doors exist.
5. **Steel vs wood timing** is real (§0 item 5). Red's call.
6. **The swing must end on something.** If the leaf bounces at 95°, there should be a reason: a closer (Office, Run and Exit have one), a base stop, or the wall. L0-F has no closer. ESTIMATE suggestion: a period rubber-tipped base stop on the skirting, which bends or snaps on the throw.

---

## 9. Media

**Reuse, already on disk** (credit from their existing ledgers):
- `../interaction_audit/images/70_…`–`80_relay_broken_+1000ms.png`: today's inert break, the "before" for the proposal (audit F11).
- `../interactables/proposal/media/kane_emg_0125_door_six_bolts.jpg`: IP canon, a door with six bolts in Kane's *Everything Must Go* (SOURCES.md there). It shows how the IP treats a locked door.
- `../interactables/proposal/media/ingame_door_gap_16_sideB_hinge_inline_darknear.png` and the `door_gap3` floor-leak pair: in-engine proof of how a lit slit reads, the base for §4's glow line.
- `Research/week02/`: nothing about door damage (checked: 1,142 files, no door-failure media).

**Wanted, not downloaded** (full rows in `media_candidates.md`, section "02"):
- two Commons photos of real forced doors in Germany (a ram-damaged apartment door, CC BY-SA 4.0; a split jamb at a storage-facility entrance, CC BY-SA 3.0 DE);
- two public-domain DVIDS photos of police ram training;
- the NILECJ 1975 standard's ram and fixture figures (public domain);
- three copyrighted references for internal viewing only: Fire Engineering's metal-door forcing video, Masonite's rage-room page, and the Consumer Reports kick test.

---

## 10. Open items and UNVERIFIED

1. All crack lengths, dent sizes, splinter counts and gap widths marked ESTIMATE come from the mechanics, not from measured tests. A slow-motion reference clip of a ram on a solid-core door in a wood frame, and one on a hollow-metal door in a drywall frame, would settle them. Both are on the media list.
2. The Relay's blow energy (§6) is an estimate from body scaling.
3. The mortise latch throw (3/4") is typical [search]; check it against a period catalogue (Best 35H/45H, Sargent 7800/8200) before modelling bent bolts.
4. The 1990 wall type behind the frames (wood or steel studs, drywall) is inferred from the 0.16 m walls.
5. Particleboard break texture (granular) is from screw-holding literature [search], not from a photo of a broken door edge.
6. The crooked-pose reading in §8 item 2 is from reading the code, not from a capture.

---

## 11. Sources

All read or searched on 2026-10-03. "PD" = US government work, public domain.

| # | Source | Date | Status | Licence |
|---|---|---|---|---|
| S1 | NILECJ-STD-0306.00, *Physical Security of Door Assemblies and Components*, US DOJ LEAA / NBS Law Enforcement Standards Laboratory. https://ojp.gov/pdffiles1/Digitization/32269NCJRS.pdf | Dec 1975 (issued May 1976) | [read], text layer of the scan: §2–5, test 5.15, 5.17, 5.18 failure definitions | PD |
| S2 | J. S. Stroik, *Terms and Definitions for Door and Window Security*, NBS Special Publication 480-22. https://ojp.gov/pdffiles1/Digitization/41910NCJRS.pdf | May 1977 | [read]: HOLLOW CORE DOOR, DRYWALL FRAME, GROUTED FRAME, JIMMYING, JAMB PEELING | PD |
| S3 | US 4,057,275, L. J. La Beaud, door jamb reinforcing plate. https://patents.google.com/patent/US4057275 | filed 1976-07-28 | [read] background | public patent |
| S4 | US 4,854,622, A. R. Lozano, door assembly resisting forced entry. https://patents.google.com/patent/US4854622 | filed 1988-07-29 | [read] background | public patent |
| S5 | US 5,241,790, G. A. Schimpf, kick-proof doorjamb reinforcing. https://patents.google.com/patent/US5241790 | filed 1992-08-25 | [read] background; the "above the deadbolt strike … below the doorknob latch strike" wording is [search] | public patent |
| S6 | B. Pressler, "Forcing Doors", *Fire Engineering*. https://www.fireengineering.com/firefighting/forcing-doors/ | 2000-05-01 | [read] | © |
| S7 | B. Gustin, "Tips for Improving Effectiveness in Forcible Entry, Part 2", *Fire Engineering*. https://www.fireengineering.com/firefighting/tips-for-improving-effectiveness-in-forcible-entry-part-2 | 2008-06-01 | [read] | © |
| S8 | B. Gustin, "… Part 3", *Fire Engineering*. https://www.fireengineering.com/firefighting-equipment/tips-for-improving-effectiveness-in-forcible-entry-part-3-2/ | 2008-08 | [read] | © |
| S9 | R. Morris, "Forcing Outward-Swinging Steel Doors", *Fire Engineering*. https://www.fireengineering.com/?p=294475 | 2012-02-01 | [read] | © |
| S10 | C. Hofschulte, "Key to the City: Shearing Force and Conventional Forcible Entry", *Fire Engineering*. https://www.fireengineering.com/firefighter-training/key-to-the-city-shearing-force-and-conventional-forcible-entry/ | 2025-06-05 | [read]; background on skins failing before locks | © |
| S11 | A. J. Hansen, "Forcible Entry: The Frame Spreader", *Fire Engineering*. https://www.fireengineering.com/?p=277950 | 2017-09-01 | [read] | © |
| S12 | J. Miles, J. Tobin, "Forcible Entry Using a Set of Irons", *Fire Engineering*. https://www.fireengineering.com/leadership/forcible-entry-using-a-set-of-iron | 2005-04-01 | [read] | © |
| S13 | R. Morris, "Forcible Entry: Conducting the Size-Up", *Firehouse*. https://www.firehouse.com/operations-training/article/10544601/forcible-entry-conducting-the-sizeup | 1998-07-01 | [read] | © |
| S14 | "Back to Basics: Effective Forcible Entry", *Firehouse*. https://www.firehouse.com/operations-training/article/10506309/back-to-basics-effective-forcible-entry | 2007-07-02 | [read]: stop vs jamb in wood and metal frames | © |
| S15 | "Door Materials Affect Forcible Entry Tactics", *Firefighter Nation*. https://www.firefighternation.com/?p=86634 | 2010-11-01 | [read] | © |
| S16 | "Door Rams", CAInstructor / Tear Gas Resource. https://cainstructor.com/Articles/Door%20Ram%20August%202014/Door%20Rams.html | 2014 | [read] | © |
| S17 | P. Hope, "Spending $10 on a door strike plate will make your lock safer", Consumer Reports. https://www.consumerreports.org/door-locks/spending-10-dollars-on-a-door-strike-plate-will-make-your-lock-safer | 2017-08-01 | [read]; the ram method (100 lb ram, 8 swings) and jamb-splitting result are [search] from CR's lock pages | © |
| S18 | Wikipedia, "Door security" (quotes a 1990s Premdor brochure, p. 6, and 1990s Consumer Reports). https://en.wikipedia.org/wiki/Door_security | read 2026-10-03 | [read]; the brochure itself not seen | CC BY-SA |
| S19 | "ANSI/BHMA Standards for Residential Grade Hardware", *Locksmith Ledger* (A156.2-2003 impact ladder). https://www.locksmithledger.com/article/10228250/ansi-bhma-standards-for-residential-grade-hardware | 2010-06-01 | [read] | © |
| S20 | Section 08211 "Flush Wood Doors", *Spec Disk 1998 Spring* (cites NWWDA I.S.1-A, AWI 1300). https://discmaster.textfiles.com/file/16843/Spec%20Disk%201998%20Spring%204.0.iso/wp/08211bue.wpd/08211bue.pdf | 1998 | [read]: PC-5 core, 1/40" veneer, 1/16" crossbands, stiles, TM-5/8/10 values, lockblocks | manufacturer spec text |
| S21 | Section 08110 "Steel Doors and Frames", same disk. https://discmaster.textfiles.com/file/16843/Spec%20Disk%201998%20Spring%204.0.iso/wp/08110gpc.wpd/08110gpc.pdf | 1998 | [read]: 10 ga hinge and 18 ga lock reinforcement, 16 ga frames, 4-7/8" strike, A250.4 | manufacturer spec text |
| S22 | NYC HPD Standard Specification 8B "Hollow Core Wood Doors". https://www.nyc.gov/assets/hpd/downloads/pdfs/services/08b-hollow-core-wood-doors.pdf | Jan 2010 | [read] | public agency |
| S23 | Forte Openings, "Cores". https://forteopenings.com/cores | read 2026-10-03 | [read]: weights per ft² | © |
| S24 | Beacon CDL, "5 Core Types for Hollow Metal Doors". https://beaconcdl.com/5-core-types-for-hollow-metal-doors/ | read 2026-10-03 | [read] | © |
| S25 | Steel Door Institute, glossary. https://steeldoor.org/glossary/ | read 2026-10-03 | [read] | © |
| S26 | Steel Door Institute, "Damaged Steel Doors: Repair or Replace?". https://steeldoor.org/articles/14-07/ | read 2026-10-03 | [read] | © |
| S27 | Masonite, "Can Masonite Solid Core Doors Survive a Rage Room?". https://www.masonite.com/discover-and-learn/can-our-solid-core-doors-survive-a-rage-room/ | updated 2024-12-09 | [read] | © |
| S28 | This Old House, "How to Repair a Split Door Jamb". https://www.thisoldhouse.com/doors/how-to-repair-a-split-door-jamb | read 2026-10-03 | [read] ("the latch-side jamb's the weak link") | © |
| S29 | EngineerFix, "How to Fix a Kicked In Door Frame". https://engineerfix.com/how-to-fix-a-kicked-in-door-frame/ | read 2026-10-03 | [read] | © |
| S30 | Weyerhaeuser, "Tension Perpendicular to Grain Considerations in Wood Design". https://www.techsupport.weyerhaeuser.com/hc/en-us/articles/36008756840339 | read 2026-10-03 | [search] | © |
| S31 | Doorwaysplus, "Closer backcheck adjustment for high wind exposure". https://www.doorwaysplus.com/blog/our-blog-1/closer-backcheck-adjustment-for-high-wind-exposure-why-the-default-setting-fails-exterior-doors-531 | read 2026-10-03 | [search] | © |
| S32 | Wikipedia, "Enforcer (battering ram)". https://en.wikipedia.org/wiki/Enforcer_(battering_ram) | read 2026-10-03 | [read] | CC BY-SA |
| S33 | VT Industries, WDMA performance duty levels (particleboard needs blocking for screws). https://www.vtindustries.com/webres/File/architectural-doors/Technical%20Bulletins/VT-Technical-Bulletin-WDMA-Performance-Duty-Levels.pdf | 2026 | [search]; the PDF text could not be extracted | © |
| S34 | Steel door weights, 3'0" × 7'0" 18 ga (43–49 kg). https://v2.tchco.com/products/view/303670 | read 2026-10-03 | [search] | © |
| S35 | Sagging-door repair guides: the top hinge in tension, the bottom in compression. https://www.woodsmith.com/review/how-to-fix-a-sagging-door/ | read 2026-10-03 | [search] | © |
| S36 | Wrap-around door reinforcers. https://www.directdoorhardware.com/wrap-around-door-reinforcers.htm | read 2026-10-03 | [search] | © |
| S37 | Doorknob holes in drywall. https://h2obungalow.com/stop-doors-from-hitting-walls/ | read 2026-10-03 | [search] | © |
| — | Freesound 160213, qubodup, wooden door kick/break: https://freesound.org/s/160213/ | — | already listed in `Documentation/DOOR_FOLEY_SEGMENTS.md` | per Freesound page |
