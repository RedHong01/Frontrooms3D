# NOTE 2026-10-22 15:5x · Red: look A confirmed, and THE STAIRWELL IS A SAFE ZONE (read before you build)

**Red's own words, in the 游戏视觉 chat (account A, session `local_5656cffd…`), about 15:5x.** Red showed VL325 (`images/stair_dirs_compare.jpg`, row A: EXIT, first view in, turn back, far door) and wrote:

> 选择这套，同时这种楼梯间其实是安全区

1. **Look A is confirmed** (same as Red's 13:00 answer).
2. **The stairwell is a safe zone.** This is a new gameplay rule.
   - Map side (关卡设计, their files): the Relay never enters the stairwell, never arrives in it, and loses the hunt once the player is inside. The 游戏视觉 session has asked them, and 系统设计 records the rule.
   - What it means for the visual side (this workflow, stairwell-a-production, wf_52cdacc1-46a):
     - **The stairwell must READ as safe from its door.** Light is steady and calm: no warning dips, no Failing or Stutter lamps, no WarnBurst reaction inside. If the stairwell lamps are on the map's warn system, write a contract diff that keeps them out of it. The twin-head emergency unit and steady lights read as "protected". It is the one place where the light never threatens.
     - **Keep the map lamps' light out of the stairwell** (the rendering-layer item in `NOTE_1022_c22_landed_READ_FIRST.md`). A Level 0 lamp dipping through the stair wall would break the safe reading.
     - **No Relay sightline into the stairwell from outside** that could suggest it will follow. Check the door's vision panel (if any) and the far door.
     - **The threshold should say "protected".** The heavy steel door closing behind the player (closer, latch) is the moment the danger is shut out. Make the door frames and the closer read clearly, and note it for 声音设计 (the closer's thud and the latch).
     - **Signage:** period stairs carried code signs. Any new string ("AREA OF REFUGE" is a candidate; ADA 1990 / UBC areas of refuge) comes only through 叙事设计 (strings) and 平面视觉 · 信息设计 (lettering). Do not invent one. List it as a suggestion in your report.
   - Record in `70_production_a.md` how each of these is met, with frames.

**Red added right after (same chat):** "也就是说怪物是进不来的" — the monster cannot get in. The Relay is never inside the stairwell, and its door is never broken (DB1 never applies to the stairwell door).

**Tutorial L11 (总控 / 教程, 15:5x):** the safe zone is taught as lesson 11 (tutorial 10_proposal §1 and §3, row 11), with no placard text and no pop-up. The teaching comes from four things: the steel door's crash bar and closer shutting and latching behind the player; the stairwell's light, always steady with no warnings (**W1's dimming outside must NOT come through the stair door**); the Relay visibly stopping and turning at the door (关卡设计); and the hunt sound fading after the door shuts (声音设计). What this asks of the visual side: keep look A's stairwell light steady, and keep W1 out of the stairwell (light layers / no shared lamps).

**New sign from 叙事设计 (16:0x; spec `narrative/40` §5):** `AREA OF RESCUE ASSISTANCE` with the International Symbol of Accessibility. That is ADAAG 1991 §4.3.11.5 and the 1991 term; "area of refuge" came later. It goes on the exit landing's stair-side wall beside the door and is lit steady like an exit sign. Provide a mount (anchor + size slot) and a steady emissive panel or lamp for it; 平面视觉 · 信息设计 letters the art. There is no call box and no "wait here" text: help never comes, and the player must go on down. The fiction is otherwise unchanged (EGRESS, FLOOR WARDEN and the stair strings).

**Sign 7 art is READY (信息设计, 16:0x; stair SPEC v1.3 row 7).** Mount it in this production.
- **Files:** `~/FrontRoomsVisualWork/info_type/stair_signs/out/rescue/` (README; albedo and emission PNGs at 4 px/mm; the symbol as SVG; layout.json).
- **Face:** 18.5 × 6.5 in (470 × 165 mm). White legend and symbol on accessibility blue #004A8C, Heros Bold 1.25 in.
- **Box:** like our exit signs, white steel, 19.5 × 7.5 × 2.25 in.
- **Light:** steady, on the emergency circuit. It never flickers or dies with the lamps. Emission: legend and symbol 1.0, field 0.4. No red and no arrows.
- **Mount:** the exit landing, stair side, latch wall, centred over the stair ID sign with a 2 in gap. Box bottom 1.753 m, top 1.943 m (under ADAAG's 80 in line).
- **Check frame for 信息设计:** lit, from just inside the door after it shuts. Shot format.
