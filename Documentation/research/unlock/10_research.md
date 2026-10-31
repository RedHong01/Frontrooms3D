# 10 · Unlock: the period lock on the EXIT and how the key opens it (research, 2026-10-31)

**Line:** 游戏视觉, session local_5656cffd, account A. **Stage:** research only. Nothing in main was changed except this new file.

**Red's ask (10-31, verbatim):** "目前在玩家开锁的时候，没有真的开锁的动画（micro cut scene）并且目前的门锁似乎没有一个对应的结构，因此我需要你research在那个年代的锁是什么样的，然后把这部分的锁的结构，以及钥匙是怎么打开锁的这个micro cut scene给做出来". The screenshot shows the locked steel EXIT (crash bar, chain through it, padlock, STAIR A plate, wired lite, HUD padlock + "E · 开锁").

**Tags.**
- [read]: the page was opened and read in this pass.
- [search]: known only from a search summary; the page itself was not opened.
- [main]: read from main's code or docs.
- [computed]: worked out here from physics or geometry.
- [estimate]: judgement, to be checked on a real lock or real footage.
- [memory]: from prior knowledge, not checked in this pass.

No file was downloaded. Sources are in §9.

---

## 0. Why the lock in main reads as "no structure" [main]

These are the concrete gaps behind Red's "没有一个对应的结构".

1. **The key goes into nothing.**
   - The EXIT has no D1.6 rig. The map's `else` branch dresses every door except the EXIT (`FrontRoomsMapWorld.cs` ~2821).
   - So `LockPose(door, from)` falls back to the spec keyhole: 1.0 m up, 0.08 m in from the latch edge (0.92 m from the hinge), 6.5 mm proud of the leaf face (`FrontRoomsMapWorld.cs` 4520–4541).
   - That point is the exit device's latch case, which has no keyway. The padlock hangs at `PadlockAt` (0.135, 0.765, 0.98) in the leaf rig (`FrontRoomsExitChainKit.cs` 58), which is **about 0.24 m below that point, 0.13 m out from it, and 0.06 m toward the latch**.
   - The camera dips to the latch case and the key turns in bare paint.
2. **The lock never acts.** At the commit (`ExitLockChanged(false)`, 0.874 s) LOCKED is swapped for UNLOCKED in one frame, with the chain already coiled on the floor (`70_a13_exit_chain.md` §1). Nothing pops, swings or falls.
3. **The anchor is under-built.**
   - The eye is a 44 mm ring in 9 mm wire (`EyeRing .022`, `EyeWire .0045` radius).
   - It is thinner than the 12.7 mm chain it holds, and it has no plate or fixings, so it reads as a picture hook.
4. **The padlock and the chain do not fit as real parts.**
   - The chain is ½ in proof coil (12.7 mm wire).
   - Two links over one shackle need 2 × 12.7 = 25.4 mm of inside width. A real 2 in laminated lock has 24 mm (15/16 in, §3).
   - Main's lock was widened to 30.4 mm between the legs to make it fit. A ½ in chain really takes a 2½ in (64 mm) lock with an 11 mm shackle (§3).
5. **The chain cannot come off by itself.** It is wrapped twice round the bar and joined through a free eye, so without a hand nothing could unwrap it. The house style has no hands (DoorShot language B, "the camera moves and the objects act, no hands"; `FrontRoomsDoorShot.cs` 12).

---

## 1. What really locked a crash-bar steel EXIT in a c. 1990 office

| # | Means | How it looks and works | Legal for an occupied exit c. 1990? | Fit for us |
|---|---|---|---|---|
| A | **Exit device, night-latch function**: rim cylinder in the outside trim (rim or mortise device) | The key works only from the *other* side (the stair side); the bar always releases from the push side. Hex-key dogging holds the latch back by day. Dogging is not allowed on fire-rated ("-F") devices [search, U9]. | Yes. This is what the code wants. | Wrong side: no key ever turns on the push side, so it cannot be the player's lock. |
| B | **Delayed egress (special locking)**: a magnetic or electric lock that releases 15 s after a push, with a sign | Permitted since about NFPA 101-1981 [search, U10; vendor blog, verify]. A 1990 box with a sign and a horn. | Yes, under conditions. | Electric, not keyed: wrong for a key cut scene. |
| C | **Chain + padlock round both crossbars of a pair** | The commonest real violation in photos and reports: "push bars chained together with a metal chain and padlock" [search, U7]. Stardust (Dublin, 1981): padlocked chains on the panic bars; doormen were told to unlock them and "hang the chain on one bar" so the doors still looked locked [read, U5]. Also theatre and field-house cases [read, U4]. | **No.** "No additional locking device (deadlock, chain, padlock & hasp, etc.)" may go on a door that needs panic hardware [read, U3]. OSHA 1910.36(d)(1): open "from the inside at all times without keys, tools, or special knowledge"; a padlocked chain is a violation while the building is occupied [search, U6]. | Our EXIT is a **single leaf**, so there is no second bar. |
| D | **Chain + padlock from one crossbar to a fixed anchor on the push side** (handrail, pipe, column, or an anchor fitted for it) | The single-leaf version of C. The anchor must be on the push side, because the leaf swings away from the person. With a little slack, a push opens a gap of a few cm and the chain snaps taut, which is the rattle. | **No** (same rules). A deliberate after-hours "security" fix by staff, as at Hamlet, NC, 1991: exits locked or blocked "to stop theft", 25 dead [search, U8]. | **This is our scene.** It needs a real anchor (§5). |
| E | **Hasp and staple + padlock** across the latch jamb | A hinged hasp screwed to the frame face, its staple on the leaf, and a padlock in the staple. The code names it ("padlock & hasp") [read, U3]. | No. | A clean alternative (§8 Q1): a short, rigid structure with no chain physics. |
| F | **Surface or barrel bolt (padlockable)** | Barrel bolts on the inside of exit pairs are still among OSHA's findings [search, U6]. | No. | Weak read at distance. |

**Period meaning (for 叙事).**
- Chaining an exit at night was a known, illegal staff practice of the period.
- The keys lived with the staff: at Stardust "the door keys were kept in the cash office" [read, U5].
- Our floor key coming from the **station (security desk)** is the 1990-true chain of custody: the guard chains the stair doors after hours, and the key hangs at the desk.
- Stardust witnesses waited **2–3 minutes** at chained doors while staff unlocked them [read, U5]. In real life an unlock is not a 1-second act; the game compresses it (§6).

---

## 2. Padlocks of the era

**Laminated steel** (the 1990 hardware-store default and the HUD padlock's family):
- **History.** Harry Soref's design: stamped steel plates stacked and riveted round the parts; US 1,490,987, granted 1924 [search, U12]. It was cheap on a punch press and resisted impact [read, U13; search, U12]. The brand's TV image of the period was the padlock shot through with a rifle that stays locked: "Tough Under Fire", Super Bowl 1974, which ran until 1983 and then in a variant until 1985 [search, U14]. That is the American padlock look of 1990.
- **Construction** [read, U15, U16]:
  - The plates (labelled A–U in US 2,379,437, so about 21) are held by through-fasteners whose ends are upset.
  - The shackle has a **long shank (heel)** with a spring under it and a **short shank (toe)**.
  - A **notch in each shank** takes the locking member: a pivoted multiple latch (1945), L-shaped sliding plates (1978), or, in today's catalogue, **dual ball bearings** [read, U1, U2].
  - The pin-tumbler plug's cam or web withdraws the locking member.
  - The spring raises the shackle until a **shoulder on the heel meets a stop**. Then the shackle **swivels** so the toe hangs outside the body [read, U16].
  - Return springs bring the plug back to locked [read, U16]. So the plug springs back after the turn and the key can come out: the lock is **not key-retaining**. A key-retaining lock is sold as a feature, for example the solid-steel A5200 [search, U17].
- **Keyway.** On the **bottom face**; the key goes in upward. Main's padlock already has it there (`FrontRoomsExitChainKit.cs` 384).

| Size class (catalogue) | Body W | Body H × D | Shackle Ø | Vertical clearance | Inside width | Pins | Source |
|---|---|---|---|---|---|---|---|
| 1-9/16 in laminated (No. 3 class) | 40 mm | not stated | 7 mm (9/32) | 19 mm (3/4) | 16 mm (5/8) | 4 | [read] U2 |
| **2 in laminated (No. 5 class)** | **51 mm** | **38 × 25 mm** (1½ × 1 in, retail lists; one listing differs) | **9.5–10 mm (3/8)** | **25 mm (1 in)** | **24 mm (15/16)** | 4 | [read] U1, [search] U18 |
| 2½ in laminated | 64 mm | not read | 11 mm (7/16) | not read | not read | 4 | [search] U19 |
| Solid steel, chrome (A5200 class) | 44 mm | not read | 8 mm (5/16) | 29 mm | not read | 5–6, key-retaining | [search] U17 |

**Finish and wear** [memory, check on product photos]:
- **Laminated:** zinc-plated plate edges with fine rust lines between plates outdoors. On a dry indoor door the plates stay grey-silver.
- A **brass plug face** sits round the keyway on the bottom, polished by the key. The rivet heads are on the bottom and top faces.
- The shackle is bright hardened steel, polished where the chain links ride it.

**Brass and shrouded:**
- Solid-brass padlocks were for outdoor and marine use.
- Shrouded high-security locks (the shackle hidden in a raised shroud) were for loading docks and gates, and too expensive for an office's stair door [estimate].

---

## 3. The chain

Welded grade 30 proof coil, zinc-plated ("galvanized") or uncoated black. Spec tables disagree by a few hundredths of an inch; the ranges are given [search, U20].

| Trade size | Wire | Inside L × W | Outside L × W [computed] | WLL | Weight |
|---|---|---|---|---|---|
| 5/16 in | 7.9 mm | 1.27–1.29 × 0.44–0.47 in (32–33 × 11–12 mm) | ≈ 48 × 28 mm | 1,900 lb | — |
| **3/8 in** | **9.5 mm (0.375; Peerless lists .404)** | **1.23–1.38 × 0.55–0.62 in (31–35 × 14–16 mm)** | **≈ 54 × 34 mm** | 2,650 lb | 1.41 lb/ft |
| ½ in (main) | 12.7 mm | 1.70–1.75 × 0.75 in (43–44 × 19 mm) | ≈ 69 × 44 mm (main: 64 × 44) | 4,500 lb | 2.37–2.42 lb/ft |

**How it was hung.**
- **Real practice.** A short length round the bar(s), with the padlock's shackle through two links: the end link and a link of the standing part, chosen to take up the slack.
- **The padlock hangs at the lowest point,** body down.
- **The weight.** Main's 1.43 m of ½ in chain weighs about 5 kg [computed], which is heavier than anyone would rig on a door. 3/8 in is the period hardware-store choice, and 5/16 the light one [estimate].

---

## 4. What makes it read as a real structure

1. **An anchor that is the chain's home.**
   - A forged eye or a welded staple on a **steel plate with four masonry anchors** reads as hardware someone installed for this purpose.
   - The eye's wire must be **at least the chain's wire**.
   - c. 1990 the corridor wall at a stair enclosure is CMU or gypsum shaft wall, so the fixings are expansion anchors with hex heads [estimate].
   - The building painter paints over it in the corridor colour, and it chips where the chain rubs [estimate].
2. **Load paths you can see.**
   - The chain bears on the **crossbar** (links crossed at 45° round it).
   - It runs **taut** to the anchor, with a 20–40 mm give, so a push shows a 2–4 cm gap and a snap [estimate].
   - The padlock's shackle carries **two links**.
   - The padlock **rests** against something (the latch case or the leaf face) instead of floating in air.
3. **Witness marks** [estimate, from the logic of use]:
   - scuffs in the crossbar's finish where the links ride;
   - a grey arc of scrapes on the leaf paint under the bar where the padlock knocks;
   - a rust or graphite smudge under the anchor.
4. **Parts that fit:**
   - the shackle Ø is smaller than the link's inside width (3/8 in shackle in a 14–16 mm link);
   - the shackle's inside width holds 2 link wires (2 × 9.5 = 19 mm < 24 mm);
   - the link's inside length holds the shackle wire plus the next link's wire (9.5 + 9.5 = 19 mm < 31–35 mm) [computed from §2–3].

---

## 5. Recommended structure (R1, for the build stage)

**Choice: one fixed end.** The chain's standing end is fixed to a wall anchor plate on the push side. Its free end makes **one turn** round the crossbar and comes back. The padlock joins the free end link to a standing-part link.

Why R1:
- It is real: it is the single-leaf form of practice D.
- It is the only routing whose release **gravity can finish without a hand**. The free end drops off the bar and the chain swings from its anchor, so the no-hands house style stays.
- It also explains the UNLOCKED state: the chain hangs from its home.

| Part | Recommendation | Main today |
|---|---|---|
| Anchor | Plate 76 × 127 × 6 mm (3 × 5 × ¼ in), long side vertical, centred **0.14 m past the latch side of the opening, 1.00 m up** (main's spot). 4 hex-head anchors, ⅜ in (14 mm A/F heads). A **½ in (12.7 mm) round-bar staple** welded to it, standing 38 mm out, with a 25 × 30 mm clear opening. Painted in the corridor colour, chipped at the staple. | eye ring 44 mm in 9 mm wire, no plate |
| Standing end | The first link closed round the staple with a ⅜ in screw-pin quick link (zinc) [estimate: period hardware-store part] | free through the eye |
| Chain | **⅜ in grade 30 proof coil: wire 9.5 mm, link ≈ 54 × 34 mm outside, pitch ≈ 33 mm.** Finish: dull zinc grey, or main's dark self-coloured `Prop_ChainOxide` (#34302B) if the 30 m read needs the dark. **Length about 0.95 m** [computed]: staple to bar 0.32 m, one turn round the bar 0.15 m, back to the lock 0.20 m, tail 0.28 m. | ½ in, 1.43 m, 2 turns |
| Wrap | One turn round the crossbar 0.10–0.16 m in from the latch case (inside main's 0.09–0.19 band), links crossed at 45°, taut toward the staple | 2 turns |
| Padlock | **2 in laminated class: 51 × 46 × 26 mm.** Keep main's 52 × 46 × 26, which is the HUD family's. Shackle **9.6 mm wire, legs 34 mm apart centre to centre (24 mm inside), 25 mm clear height when shut.** Keyway on the bottom with a brass plug face (Ø ≈ 14 mm) and 4 rivet heads. Not key-retaining. Lamination count: keep the HUD family's 7 grooves as a stylised read (real plates are many and thin; §2). | legs 40 mm apart |
| Lock pose | It hangs on the free end link and a standing link **just below the bar's latch end, about 0.93 m up, about 0.10 m out from the leaf**. Its back rests on the latch case / leaf, so its **bottom (keyway) tips about 20–30° toward the corridor**. The dipped camera sees the keyway at a grazing angle, and the lock has a bearing surface. | 0.765 m, free in air |
| Push response (existing `DoorRattled`) | The leaf gives 25 mm, the chain goes taut, the padlock knocks the leaf | as main |

**If Red wants to keep the ½ in chain** (it reads better at 30 m): use a **2½ in lock (64 mm body, 11 mm shackle)** and a ⅝ in staple. That breaks the HUD padlock's 52 mm family proportion; see Q2.

**UNLOCKED** (the end of the cut, and the state on a fresh build when unlocked):
- The chain hangs from the staple down the wall.
- Its tail pools on the floor at the latch jamb (about 0.25 m of chain on the floor).
- The open padlock hangs on a standing link with the toe swung out.
- This **replaces main's floor coil**. A13 is 游戏视觉's code, so no contract is needed for that.

---

## 6. Exactly how the key opens it

Three columns:
- **Real** is a person at a real door.
- **Physics** is computed for the no-hands version.
- **Game** maps it onto the map's existing shot clock [main: `FrontRoomsShotTimings.cs` Unlock].

No slow-motion footage of a key opening a padlock was found or viewed in this pass. The only slow-motion padlock film found is a bullet test at 80,000 fps [search, U21]. Mechanism timings are therefore **[computed]**, and human timings are **[estimate]** until a lock is filmed at 240 fps (Q4).

| Step | What happens inside | Real | Physics | Game (s on the shot clock) |
|---|---|---|---|---|
| 0 Find the keyway | The keyway faces the floor. A person lifts the body with the free hand and tips it toward the eye. | 0.5–1.0 s [estimate] | — | **0.00–0.30** camera dip (map's). The lock is pre-tipped 20–30° (§5), so no lift is needed. |
| 1 Approach | — | — | — | **0.30–0.55** `KeyApproach`. The key rises from the lower right to 3 cm **below** the keyway, blade vertical, cuts toward the leaf. |
| 2 Insert | The blade passes under the 4–5 pin stacks; each pin rides up and down every cut ("pins rise"); at full depth each key pin's top meets the shear line [read, U11]. Ticks, then the shoulder seats on the plug face. | 0.15–0.3 s [estimate] | 4–5 ticks over 20–25 mm | **0.55–0.68** `KeyInsert`, 25 mm (main's blade 25 mm; a 46 mm body has room). Ticks about 0.57 / 0.59 / 0.61 / 0.63 / 0.65, seat **0.68**. The hanging body is pushed up 1–2 mm and settles. |
| 3 Turn | The plug and its cam turn and draw the latches or balls out of the shackle notches against their springs [read, U11, U16]. About 90° (the map's `TurnDeg`); the direction is set by the lock (clockwise is common) [estimate]. | 0.15–0.25 s | — | **0.68–0.86** `KeyTurn`, 90°, first 10° stiff (map's `TurnResistShare` .35). The hanging body twists 3–5° with the torque and back. |
| 4 Release ("pop") | The spring under the heel drives the shackle out until the heel's shoulder hits the stop [read, U16]. On a lock hanging from its shackle, it is **the body that drops**, by the travel. | instant | Travel ≈ 10–12 mm [estimate: toe engagement 8–10 mm + 2–4 mm clear]. Fall plus spring over 10 mm ≈ **30–45 ms** (free fall alone 45 ms) [computed]: 2–3 frames at 60 fps. Hard stop, 1–2 mm rebound, still by about 80 ms. | **0.86 commit** = `BoltRetract` = `ExitLockChanged(false)` (now 0.874; same frame ±1). Body down 11 mm in 2 frames, no easing, rebound 1.5 mm, still by 0.95. |
| 5 Swivel | With the toe clear, the shackle turns about the heel. A person twists the body; here the tension of the hanging free end pulls the toe-side link sideways. The toe–body gap after the pop (2–4 mm) is **smaller than a link's wire (9.5 mm)**, so the link cannot slip out without the swivel [computed]. | 0.2–0.4 s | 90–120° in about 0.08 s under 0.3 kg of hanging chain [estimate] | **0.87–0.94**. The toe swings out about 110° toward the hinge side. |
| 6 Slip | The end link slides along the bow and off the toe. | 0.1–0.3 s (by hand) | about 50 ms | **0.94** `ChainSlip`: the end link drops off the toe. |
| 7 Chain off the bar | By hand, the free end is unwound and pulled off. Here the free end, now unheld, slides off the bar's single turn under its own weight. The turn opens as the tail falls. | 1–3 s by hand | Free fall from about 0.93 m to the floor = √(2h/g) ≈ **0.44 s** [computed] | **0.94–1.00** the turn opens and the bar is clear by 1.00, before the map's leaf ajar (1.00–1.25; no contract change). **≈ 1.38–1.45** the tail hits the floor and pools. |
| 8 Swing | The chain swings from the staple. A hanging chain of about 0.7 m swings with a period of about 1.4 s [computed, compound pendulum √(2/3·L), L ≈ 0.7 m]. The open padlock rides on its standing link and knocks the leaf or jamb once. | — | sway decays over 1.5–2.5 s | **0.95–1.55** in the shot, then on the dresser's own clock after `ShotEnded`. Padlock knock about 1.10. |
| 9 Key out | The plug's return springs bring it home, and a non-retaining key comes out [read, U16]. | 0.2 s | — | **0.90–1.15** `KeyWithdraw` (map's), downward, then out to the lower right (`ShotKeyExit` .20). |

**The whole act.**
- **Real:** about 4–8 s for one person [estimate] (2–3 minutes at Stardust with staff and key-finding [read, U5]).
- **Game:** the map's 1.55 s shot, with every mechanical event inside it and the sway carried past it.

---

## 7. How horror games and films stage a short unlock

| Work | Staging | Length / sync | Hand? | Source |
|---|---|---|---|---|
| *Resident Evil* (1996; HD 2015) | The door opening is its own short film between rooms: slow push to the door, pass through, creak. It masks loading and is used for scares later (RE2's door scare). | a few seconds, sound-led [not measured] | none: object only | [read] U22, [search] U23 |
| *Resident Evil 7* (2017) | First-person doors creak open and stay ajar; the camera never leaves the eye. | short [not measured] | the player's hands appear for key items [memory] | [search] U24 |
| *Alien: Isolation* (2014) | Doors open by keycard, code, the access tuner or a cutting torch. Each is a diegetic tool action in first person that keeps you exposed while you do it. | seconds, interruptible [memory] | hands and tool | [search] U25 |
| *Amnesia: The Bunker* (2023) | Padlocks have no key: shoot them or throw a brick; chains need the cutters. The lock is physical and breaks on screen. | physics, sound-led | the hand holds the tool | [search] U26 |
| *Silent Hill 2*, *Outlast* | Asked for; nothing citable on their unlock beats was found in this pass. | — | — | — |
| *Notorious* (1946) | The film grammar for "the key matters": one crane move from a wide hall to an extreme close-up of the key in a hand. Later the cellar shot inverts it. | — | the hand is the frame | [search] U27 |
| Liz England, "The Door Problem" (2014) | The design questions: can doors lock, and what tells the player a locked door *will* open as opposed to one that never will. | — | — | [search] U28 |

**What we take from them.**
1. **Length.**
   - These beats are short and sound-led.
   - Our 1.55 s already sits in the band.
   - Real actions take 4–8 s. Games keep only the readable mechanical events: insert, turn, pop, drop.
2. **The commit is a sound plus one hard movement.**
   - In every case the "it gave" moment is one sharp sound on one visible snap.
   - For us that is the body drop at 0.86, which is also the gameplay commit.
3. **No hand is a legitimate style** (the RE door films are object-only).
   - It holds only if the objects move as they would by physics. That is why R1's gravity release matters.
   - A hand would break the house language B and is not proposed.
   - Red's 10-14 HUD-U3-D (the HUD key moves in its panel, not into a lock) stays as decided. This in-world cut runs with it.
4. **Show the lock before the key** (A13 already does).
   - The rattle on a locked push (the leaf gives 25 mm and the chain snaps taut) answers England's question.
   - It makes this door "locked but openable", because the lock is a thing on this door.

---

## 8. Cut-scene beat plan (for the build stage)

The camera timeline stays the map's: dip to pose P, 0.45 m out, 39° down, FOV −14° (`FrontRoomsDoorShot.cs` 15–17). **Only the target moves.**

**Contract diff (关卡设计, map file), the one change:**
- For the EXIT, `LockPose` returns the **padlock's keyway**: position the keyway centre, +Z up into the lock (tipped 20–30° as built), +Y toward the leaf.
- It should do this through a seam the visual dresser fills, for example `IFrontRoomsExitLock.Keyway(face)` set by `FrontRoomsExitChain` at `ExitDoorBuilt`. The map then never reads visual objects.
- With pose P at 0.45 m and 39° down from a keyway at about 0.93 m, the eye sits at about 1.29 m, a dip of about 0.3 m like the map doors' [computed].

| t (s) | Beat (map name) | The lock and chain (visual) | Key | Sound sync point (名称 only; 声音设计 owns events) |
|---|---|---|---|---|
| 0.00–0.30 | dip | still; the chain hangs taut | in hand off-screen | — |
| 0.30–0.55 | KeyApproach | still | lower right → 3 cm under the keyway | — |
| 0.55–0.68 | KeyInsert (PinCatch about 0.60) | body up 1–2 mm, settles | 25 mm in, upward | `Unlock.PinTick` ×5 (0.57–0.65), `Unlock.KeySeat` 0.68 |
| 0.68–0.86 | KeyTurn | body twists 3–5° and back | 90°, first 10° stiff | `Unlock.KeyTurn` 0.68 |
| **0.86** | **BoltRetract = commit = ExitLockChanged(false)** | **body drops 11 mm in 2 frames, rebound 1.5 mm** | rides down with the body | **`Unlock.ShackleRelease` 0.86** (replaces the bolt sound for the EXIT) |
| 0.87–0.94 | — | toe swivels out about 110° | — | `Exit.ShackleSwivel` 0.88 |
| 0.94 | — | end link slips off the toe | — | `Exit.ChainSlip` 0.94 |
| 0.90–1.15 | KeyWithdraw | — | out downward, then lower right | `Unlock.KeyOut` 0.90 |
| 0.94–1.00 | — | the turn opens; the bar is clear | — | `Exit.ChainRun` 0.95–1.05 |
| 1.00–1.25 | leaf ajar (map's) | the chain swings from the staple; the padlock knocks the leaf about 1.10 | — | `Exit.PadlockKnock` 1.10 |
| 1.38–1.45 | — | the tail hits the floor and pools | — | `Exit.ChainFloor` 1.40 |
| 1.45–1.55 | fade / ShotEnded (map's) | sway continues | — | `Exit.ChainSway` (tail, decays to 3.0) |

**Notes.**
- **All motion is procedural on the dresser's own clock,** started by the shot's beat events (`Land(...)`). There is no animation clip, so it runs on desktop, iPhone and iPad alike (three-platform parity).
- **Cancel before the commit** (the Relay within 6 m, `CancelRelayDistance`): the key backs out, and the lock settles back to LOCKED with no pop.
- **The links during the run** follow a precomputed spline from the LOCKED to the UNLOCKED path, with the pinned links (staple, padlock) moving by rule. No physics engine and no allocation per frame.

---

## 9. Decisions for Red (with what the player feels)

Each item gives the options as the player would feel them, then the cost.

1. **Q1 — the lock's form.**
   - **(a) R1, chain to a wall staple (recommended).** The player sees a chain they can follow from the bar to its anchor. On the key turn the padlock drops, the chain falls off the bar and swings from the wall. This is the biggest "it really opened" moment.
     - Cost: anchor plate, retopologised chain, release motion, and the one map seam above.
   - **(b) Hasp and staple + padlock across the latch jamb.** It is compact and rigid. It reads at once as a lock fitted to this door, but there is no chain fall: only the pop and the hasp swinging open (0.2 s).
     - Cost: less work, but it replaces A13's chain look.
   - **(c) Keep main's two turns and coil.** Not recommended: nothing can take the chain off without a hand.
2. **Q2 — chain size.**
   - **⅜ in (recommended).** Period-true; it fits a 2 in lock and keeps the HUD family's 52 mm lock. The chain is 25 % thinner at 30 m: 70 §4 must be re-measured (19 px today on desktop).
   - **½ in.** It reads more at distance, but needs a 2½ in, 64 mm lock that no longer matches the HUD padlock.
3. **Q3 — the UNLOCKED state.**
   - The chain hanging from the staple, its tail on the floor and the padlock hanging open, replacing main's floor coil.
   - It is the honest end of the motion. It is also what a guard does: the lock is hung on the chain so it is not lost.
4. **Q4 — reference footage.**
   - May 平面视觉/游戏视觉 film a real 2 in laminated padlock on ⅜ in chain at 240 fps (phone), silently per rule 24 (no playback with sound)?
   - That would replace this file's [estimate] timings for pop travel, swivel and slip with measured ones.
   - No downloads are needed; the lock is about $10.

---

## 10. Sources

| ID | Source | URL | Status | Used for |
|---|---|---|---|---|
| U1 | Master Lock, No. 5 product page | https://masterlock.com/products/product/5 | [read] | 2 in body, ⅜ in shackle, 1 in tall, 15/16 in wide, 4 pins, dual ball bearing |
| U2 | Master Lock, No. 3 product page | https://www.masterlock.com/products/product/3 | [read] | 40 mm body, 7 mm shackle, 19 × 16 mm, 4 pins |
| U3 | L. Greene, "Back-2-Basics: Panic Hardware", iDigHardware, 2013-10-15 | https://idighardware.com/?p=16278 | [read] | "No additional locking device (deadlock, chain, padlock & hasp, etc.)" |
| U4 | L. Greene, "WW: Ice Time", iDigHardware, 2013-12-11, and its comments | https://idighardware.com/?p=16894 | [read] | chained crossbars at a theatre and a field house (comments) |
| U5 | Stardust inquest, doorman's testimony, BreakingNews.ie | https://www.breakingnews.ie/ireland/stardust-doorman-tells-inquest-of-putting-chains-around-exit-doors-to-trick-teenagers-1489869.html | [read] | 1981 padlocked chains on panic bars; hung on one bar; keys in the cash office; 2–3 min waits |
| U6 | OSHA 29 CFR 1910.36; J. J. Keller / SafetyNow summaries | https://www.osha.gov/laws-regs/regulations/standardnumber/1910/1910.36 ; https://www.jjkellersafety.com/resources/articles/2025/exit-strategy-unlocked-doors | [search] | (d)(1) text; padlocked chain is a violation; barrel locks finding. The 1990 text was under the older 1910.37 numbering [memory, not read] |
| U7 | NC DHHS inspection report (2022) | https://info.ncdhhs.gov/dhsr/acls/star/docs/2022/20220930-921042.pdf | [search] | "push bars were chained together with a metal chain and padlock" |
| U8 | Hamlet, NC fire 1991: Deseret News 1991-09-04; IChemE summary; USFA-TR-057 (not opened) | https://www.deseret.com/1991/9/4/18939322/locked-doors-prevented-escape ; https://www.icheme.org/media/16675/hamlet-incident-summary-03-sep-91.pdf | [search] | doors locked against theft, 25 dead; period case |
| U9 | SecurityParts, Von Duprin 880NL trim guide; Allegion KB | https://www.securityparts.com/von-duprin-880nl-trim-guide ; https://kc.allegion.com/kb/user-question/rim-device-latch-fully-retract | [search] | night-latch function, rim cylinder, no dogging on fire devices |
| U10 | SDC Security, delayed egress overview | https://solutions.sdcsecurity.com/blog/delayed-egress-overview | [search] | NFPA 101 15 s delayed release from about 1981 (vendor claim) |
| U11 | The Engineering Mindset, "How padlocks work" | https://www.theengineeringmindset.com/how-padlocks-work/ | [read] | pins to the shear line, cam pushes the latches, spring pushes the shackle, rotates freely |
| U12 | Smithsonian, "Master Lock has had hold on industry for 100 years"; Wikipedia, Harry Soref | https://www.smithsonianmag.com/innovation/master-lock-has-had-hold-on-industry-for-100-years-180977743/ ; https://en.wikipedia.org/wiki/Harry_Soref | [search] | US 1,490,987 (1924); stacked riveted plates |
| U13 | Wikipedia, "Padlock" | https://en.wikipedia.org/wiki/Padlock | [read] | laminated plates riveted; shackles swing away or slide; locking dog / balls |
| U14 | CSMonitor, "Tough under fire, Master Lock 1973"; AdWeek | https://m.csmonitor.com/Business/2013/0130/Five-greatest-Super-Bowl-ads-of-all-time-video/Tough-under-fire-Master-Lock-1973 ; https://www.adweek.com/brand-marketing/the-super-bowl-ad-that-worked-so-well-it-no-longer-needs-to-be-in-the-big-game/ | [search] | the period's padlock image; ran 1974–83, variant to 1985 |
| U15 | US 4,098,100, "Laminated padlock" (1978) | https://patents.google.com/patent/US4098100A | [read] | plates; J shackle; spring under the heel; L-shaped locking plates; rotator |
| U16 | US 2,379,437, "Laminated padlock", Norwalk Lock Co. (1945) | https://patents.google.com/patent/US2379437 | [read] | plates A–U; heel spring; shoulder and stop; swivel; double latch; plug return springs |
| U17 | LockWiki, American 5200; retailer listings | https://LockWiki.com/index.php/American_5200 ; https://www.craftmasterhardware.com/parent-brand/american-lock/maa5200n-american-lock-no-a5200n-solid-steel-padlock | [search] | 44 mm, 5/16 in shackle, 1⅛ in clearance, key-retaining |
| U18 | Retail listings for the No. 5 (Ace, Newegg, Intercity) | https://www.acehardware.com/p/53087 ; https://eshop.intercityindustrial.com/commercial-steel-padlock-2--1 | [search] | body 1½ in H × 1 in D |
| U19 | RS Components 0108888 | https://th.rs-online.com/web/p/padlocks/0108888 | [search] | laminated 64 mm body, 11 mm shackle |
| U20 | Redboar Chain (⅜, 5/16 pails); Colville/Campbell ⅜ and ½ listings; Peerless via summary; Lexco ½ | https://redboarchain.com/products/3-8-grade-30-proof-coil-chain-zinc-plated-5-gallon-pail-63ft ; https://redboarchain.com/products/5-16-grade-30-proof-coil-chain-zinc-plated-5-gallon-pail-92ft ; https://colvillefasteners.xologic.com/brand-campbell/proofcoil-chain3-8z-p35-rl/sku-V4857-t0722327 ; https://catalog.lexcocable.com/printitem/chains/grade-30-proof-coil-chains/50-0001?plpver=10 | [search] | link dimensions, WLL, weights |
| U21 | Gigazine on the Slow Mo Guys padlock shot | https://www.gigazine.net/gsc_news/en/20240702-bullet-break-padlock-slow-motion | [search] | the only slow-motion padlock film found (not a key opening) |
| U22 | S. Trinh, "Resident Evil — Loading Screens and Doors", Game Developer, 2020-03-04 | https://www.gamedeveloper.com/design/resident-evil---loading-screens-and-doors | [read] | door film masks loading; creak; tension |
| U23 | Dread Central, RE2's door transition scare | https://www.dreadcentral.com/editorials/489745/resident-evil-2-the-door-transition-scare-that-changed-horror/ | [search] | door beat reused as a scare |
| U24 | Twinfinite, *Resident Evil 7* review | https://twinfinite.net/ps4/resident-evil-7-biohazard-review/ | [search] | first-person doors creak and stay ajar |
| U25 | Wikipedia, *Alien: Isolation* | https://en.wikipedia.org/wiki/Alien:_Isolation | [search] | keycards, codes, access tuner, cutting torch |
| U26 | Prima Games, *Amnesia: The Bunker* doors and padlocks | https://primagames.com/tips/how-to-break-open-wooden-doors-and-padlocks-in-amnesia-the-bunker | [search] | padlocks shot or bricked; chains need cutters |
| U27 | Criterion, "How Hitchcock pulled off a shot for the ages" (*Notorious*) | https://www.criterion.com/current/posts/6156-how-hitchcock-pulled-off-a-shot-for-the-ages | [search] | crane to the key ECU; the key as subject |
| U28 | L. England, "The Door Problem" (2014) | https://lizengland.com/blog/2014/04/the-door-problem/ | [search] | what tells a player a locked door will open |

**Not found or not read in this pass (open):**
- NFPA 101-1988 text on locks (§5-2.1.5) and the 1980s OSHA 1910.37 wording;
- Hamlet's USFA-TR-057 itself;
- any slow-motion footage of a key opening a padlock;
- citable descriptions of the unlock beats in *Silent Hill* and *Outlast*;
- the lamination count and body height of a 1990 No. 5.
