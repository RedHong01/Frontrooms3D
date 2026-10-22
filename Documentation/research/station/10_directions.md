# 10 · Security desk: concept directions (joint; 游戏视觉 leads the visual side)

**Status: v0, the narrative half by 叙事设计 (2026-10-22).**
- **From now on 游戏视觉 owns edits to this file.** It adds the look per direction (kit mapping, materials, light plan, blockout renders, Figma), and 叙事 sends changes by message. That way two chats never edit it at once.
- **Basis:** `01_research.md` (real 1990 practice and codes), `02_refs.md` (references and IP distance), `20_layout.md` (grammar G1–G10 and plans W, D, H).
- **Every direction keeps grammar G1–G10, the tutorial's rules** (one TV, the key at the station, the take kills the set, the hunter is sent there) **and the strings in `01` §6.** Directions differ in story, dressing and plan, never in rules.

## Direction A · "The Post" (Office floors; plan D)

- **Concept:** the night guard's desk at an Office crossroads. He set up his own TV on a cabinet, against his post orders, and his chair still faces it, with its back to the way in.
  - The key board hangs on the wall the screen washes.
  - The sign-in counter stands against the side wall, with a few visitor chairs.
- **Story it tells without words:**
  - he watched the set, not the door;
  - he left on his round with the clock, the radio and the flashlight;
  - the coffee stayed.
- **Reveal:** oblique. First the cold light on the far wall, then the set and the key (plan D).
- **Real basis:** the lobby security desk and guard post (`01` §1, §2); *Die Hard* and *Halloween II*'s "watcher faces the wrong way" (`02` R-P1, R-P2).
- **Risk:** the FNAF office. Mitigated: you cross it and take from it, you never sit; no monitor wall; no doors or light buttons (`02` §5).

## Direction B · "After-Hours Reception" (Level 0 floors, Floor 1; plan W)

- **Concept:** a building waiting room that the night guard used as his post.
  - A reception counter with a staff strip behind it, where the key board hangs.
  - Two short rows of steel tandem seats face a set on a corner wall bracket.
  - An ashtray stand under a NO SMOKING sign, a magazine rack, a wall clock over the door.
- **Story:** a room built for waiting, where nobody waits. The set is still on for nobody. The guard's stool and log are behind the counter.
- **Reveal:** near face-on across the room. The waiting room faces its set, so the logic is instant for a first-time player (plan W).
- **Real basis:** clinic, DMV and dealer-lounge waiting rooms with a high-mounted set (`01` §6.4); the floor reception counter (`01` §1).
- **Risks:**
  - A24's showroom rows. Mitigated with institutional tandem seating in two short rows, never upholstered armchairs.
  - The existing `L0_WaitingRoom_4x3` module. Mitigated: the set, the counter and the warm lamp make this a different room.

## Direction C · "The Night Posts" (both themes, Phase 2: one real post type per floor)

- **Concept:** every floor's desk borrows a **different** real 1990 post (`01` §1.1). For example:
  - Floor 1: B (reception);
  - B1: A (guard post);
  - B2: a **floor warden station** (NYC LL5: a small desk with a wall phone and the floor's posted plan);
  - B3: a **receiving desk** at a dock in the stock rooms (a time clock, a receiving log, dock-door buttons; no showroom and no brand);
  - then repeat with small changes.
- **Story:** the descent passes the building's night posts one by one, the same grammar each time. Players learn "normal" on Floor 1 and spot what changed (*The Exit 8*, *P.T.*; `02` R-G6, R-G7).
- **Cost:** 2–3 more modules after V1. It needs M4 to pick the module by floor.
- **Risk:** the dock desk leans toward the furniture store, so it is kept strictly back-of-house (A24 distance).

## Narrative recommendation

- **B for Floor 1 and Level 0 floors; A for Office floors.** These are exactly V1's two modules (Station · Waiting / Station · Desk), so the research changes their look and plan, not the schedule.
- **C waits until the loop is proven** in playtest.
- Optional later layers:
  - the watchclock key-station trail (`20` §6);
  - "the night goes on as you go down" on the set (`02` §4).

## For 游戏视觉 to add (visual side)

1. Per direction: the kit mapping (existing kits, new kits from the gap list in `05_visual_answers.md`), materials and colours (`01` §1), and the light plan (G3, and the spill decision in `20` §2.2).
2. Blockout renders in a clone, for each direction, in the stairwell format (SW00–SW07). Run the checks in `20` §8.
3. A recommendation on the visual side, then one joint message to Red.

---

# Visual half (游戏视觉, 2026-10-22; workflow `station-look`, stage 1, paper only)

**游戏视觉 wrote everything below this line on 2026-10-22. 叙事设计's text above is unchanged.**
- Basis: `30_look.md` (kit inventory S01–S36, materials, light plan, the three calls with the light math) and `data/light_math.py.txt`.
- Not yet proven in Unity. The blockout stage (clone `W/proj_station`) runs `20_layout.md` §8 plus `30_look.md` §6, then the Figma section and the joint draft for Red.
- **Shared by all three directions:**
  - the set is the beacon light: a spot at the screen, cone 100° / 160°, URP range 9, intensity from a track baked from the ad (`30_look.md` §3.1);
  - the set's cell and its neighbour are Off, the other cells Dim, E1's cell Steady;
  - the key hangs on a one-hook switch plate wired by conduit to a putty annunciator whose only lit lamp is the green POWER jewel;
  - the shell stays the floor's own (no new wall, carpet or ceiling surface).

## A · "The Post" (Office floors), visual side

- **Look:** a working guard post in an Office room. A beige-laminate steel desk faces a black 21 in set on a low laminate credenza. A navy jacket hangs on the chair, whose back is to the door. A grey sign-in ledge with an oak top stands against the side wall, with four chrome sled chairs in navy vinyl. On the wall the set washes: the key plate, the conduit, the annunciator, the cabinet marked KEY CONTROL.
- **Palette:** the Office shell (greige drywall #B5AE9D, grey-blue tile #535A61); beige laminate and putty steel; navy; almond (the cabinet); the set's lavender-blue light. No red, amber or pale yellow-green; no cherry or walnut.
- **Key props:** `Kit_OfficeDesk`, `Kit_TaskChair` + new `Kit_JacketDraped`, `Kit_Credenza_Laminate` (variant), new `Kit_GuestChairSled` ×4, new `Kit_StationCounter_Ledge120`, new `Kit_KeySwitchPlate`, new `Kit_FireAnnunciator`, `Kit_ConduitEMT`, `Kit_KeyCabinet`, new `Kit_Clipboard`, new `Kit_RegisterBook`, new `Kit_RadioCharger`, new `Kit_CoffeeCup`, new `Kit_Ashtray`, `Kit_DeskPhone` (line lamps dark).
- **Light:**
  - plan D′, 3 × 3: (1, 2) and (2, 2) Off, E1's cell (1, 0) Steady, six Dim;
  - the set on the credenza (screen 1.04 m) washes the floor ahead (0.36 at 2 m), the jacket's front (0.24) and the key (0.63 mean card, 0.32 darkest; 40° off its axis, 1.97 m).
- **From E1** (D′: E1 in the S wall's middle cell, 8.9 m from the set, 21° off both axes): the chair and jacket from behind, in front of a playing set, and the lit key plate on the right wall. From the corridor: a dim room with one lavender-blue patch that changes every 2–4 s.
- **At the take:**
  - the player faces the E wall: plate, key, conduit, the green dot;
  - the set is ~99° to the left, out of view;
  - the light on the key drops ~80 % in one change, and the green dot stays lit.
- **Cost:** shares every take-cluster kit with B. A alone ~9.4 days at hero spec; **after B, ~2 days** (credenza variant, guest chairs, desk ashtray, module). The blockout tier fits in V1's 2-day blockout pass.
- **Differs from 叙事's plan D in one place (for 叙事):** E1 moves from the SW corner onto the set's face (and, optionally, the desk 1.6 m east so the chair stands against the wash). As drawn, the set is 39° off E1's axis and its wash is seen only from a diagonal approach a grid map does not make, so answer (b) fails and (c) would be needed (`30_look.md` §4.1, §5.2). The key position of plan D already passes G4 and is kept.

## B · "After-Hours Reception" (Level 0 floors, Floor 1), visual side

- **Look:**
  - a building waiting room inside the paper-and-carpet floor: two short rows of steel tandem seats in navy vinyl, facing a black set on a steel wall bracket high on the far wall;
  - a grey colour-through laminate counter with an oak ledge (1.07 m) and the guard's work surface (0.74 m) behind it;
  - in the counter's open staff strip: the guard's chair turned round to face the set, the clipboard, the phone, the empty charger, the cold cup;
  - on the north wall at the strip's mouth: the key plate, the conduit and the annunciator, lit by the set;
  - a chrome ashtray stand under NO SMOKING, a wall magazine rack, a clock over E1.
- **Palette:** the Level 0 shell (hard-edge paper, tan loop carpet #897853); grey and oak; black steel and navy; putty and almond; the set's light. Institutional, never the showroom: no upholstered armchairs and no sofas (A24), no cherry.
- **Key props:** new `Kit_TVWallBracket`, new `Kit_TandemBeam3` ×2 + `Kit_TandemBeamTable`, new `Kit_StationCounter_Run120` ×2 + `_End`, new `Kit_SmokingStand`, new `Kit_MagazineRack`, `Kit_WallClock`, and the shared take cluster (A's list without the desk, credenza and guest chairs).
- **Light:**
  - plan W′, 3 × 2: (1, 1) and (2, 1) Off, E1's cell (0, 0) Steady, three Dim;
  - the set on the bracket (bottom 2.10 m, 20° down) washes the key plate (0.64 mean, 0.33 darkest; 38° off its axis, 1.87 m), the counter top (0.45) and the floor of the strip's mouth (0.31).
- **From E1** (W′: W wall south cell, 8.9 m, set 22° off E1's axis): the rows' backs facing a playing set, the counter to the right, the lit key plate with its green dot beyond. "A room built for waiting, where nobody waits."
- **At the take:**
  - past the counter's north end, facing north;
  - the set is ~100° to the right, overhead;
  - the light on the key drops ~80 %, the green dot stays.
  - Leaving by E2, 5 m down the strip, you pass beside and under the dead set.
- **Cost:** ~10.7 days at hero spec (it carries the shared take cluster). The blockout tier is ~2 days with A. LOD0 ~94k is over Level 0's 60k room cap, so the station needs to be a hero room at ≤ 100k (a 关卡设计 budget call), or B is cut to ~64k without the key cabinet.
- **Differs from 叙事's plan W (for 叙事):**
  1. **The set leaves the corner.** From the corner its screen stands 0.57 m off both walls, so the key 1.5 m along the N wall is 69° off its axis and gets 0.13: a third of a Dim lamp's own wash. No spot on a wall next to a corner set comes within 45°. On the E-wall bracket the same key is at 38° and gets 0.64 (`30_look.md` §4.2).
  2. **E1 moves to the W wall's south cell**, so the set is 22° off E1's axis (G1; plan W's E1 was 52° off) and its wash is framed through E1 (answer (b)).
  3. **The guard's chair is turned to face the set**, its back to the counter: "the watcher faces the wrong way", carried into B.
  4. **Standard ceilings only:** over a walk line the set's bottom must clear the Relay (2.10 m), so a Low room needs the cart variant (Phase 2).

## C · "The Night Posts" (Phase 2), visual side

- **Look:** the same grammar, the same take cluster (plate, conduit, annunciator, the set) and the same light rules. The post type changes the furniture:
  - **warden station:** a small steel desk, a putty wall phone, the posted floor plan; Office palette;
  - **receiving desk:** plywood, pallets, black steel, a grey time clock and card rack, dock-door buttons; strictly back of house;
  - **guard booth:** S05's ledge under `Kit_WindowFrame_Alu`.
- **From E1 and at the take:** as A or B, by plan.
- **Cost:** `Kit_WallPhone`, `Kit_TimeClock`, `Kit_TimeCardRack`, `Kit_DockButtonStation` (2–3.5k LOD0 each), 1.5–3 days per extra post, and M4 picking the module by floor.
- **Risk:** the receiving desk leans toward the store; it gets no showroom furniture at all.

## Visual recommendation

- **We agree with 叙事:**
  - **B (plan W′) for Floor 1 and Level 0 floors;**
  - **A (plan D′) for Office floors;**
  - **C after the loop is proven.**
- B is the right first floor for the look as well. A waiting room facing a set explains itself to a first-time player in one glance, and on Floor 1 the station comes 2–6 cells after the locked EXIT, when the player most needs the room to be readable.
- **Where we differ:** only in the plans (W′, D′), for light reasons a blockout will show side by side. The story, rules and strings are unchanged.
- **Order:**
  1. The blockout tier (~2 days, both modules) proves the grammar and the three calls in the next stage.
  2. The hero kits (~12 days) follow after the Alpha, B's first.
- **For the joint message to Red:**
  - A or B per theme (as above);
  - the hero-room budget;
  - whether the take beat accepts "the light on the key goes out" as G5's frame (the set itself is out of view at the take, by geometry).

## Blockout results (游戏视觉, 2026-10-22 12:4x; workflow `station-look`, stage 2)

**Full table:** `30_look.md` §9; images `images/` (VL354–VL358). Eight plans were built in clone `W/proj_station`, in the game's own map, and measured on the six checks of `20_layout.md` §8.
- **B (Level 0, Floor 1):** **W′ rev 3** is the only Waiting plan that passes everything except the literal G5.
  - It keeps W′'s set (E-wall bracket) and key (N wall).
  - It moves E1 to the W wall's north cell, onto the set's axis, so the screen shows through E1 from the junction.
  - It widens the staff strip to 2.0 m, so the turned guard chair leaves 1.1 m.
  - W as drawn fails the key light (68°, 45 %) and the take drop (45 %). W′ as amended on paper fails the junction squint and the strip (0.7 m).
- **A (Office floors):** **D′ rev 3** has E1 in the S wall's east cell, on the set's axis, and the desk left of it, so the chair and the jacket sit under the screen. It passes as D′ does, plus the junction squint. D as drawn fails the junction and "exits brightest after the take" (1.35×).
- **H** passes as drawn, for long rooms.
- **Shared findings, for 叙事 to accept:**
  - G1's "set within ±30° of E1's axis" becomes **≤ about 12°**. The set's light adds 0.000 at E1, so only the screen itself, framed by E1, reads from the junction.
  - G5 becomes "the light on the key board drops in view". The set is 85–113° off every take view, and the board loses 82–86 % of its light in one change.
- **For V3 (游戏视觉's own row):** a luminance-preserving light colour. As specified, the dark WTCZ card gives only 43–50 % of the key's light; preserving luminance raises that to 70–76 %.
- **Lamps follow tutorial V1 (two Steady at the entrance).** This corrects `30_look.md` §3.3.
