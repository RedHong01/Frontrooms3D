# Bathroom rooms · 30 · Work estimate for 系统设计

Date: 2026-10-31. Line: 浴室房间调研 (account A, `local_bd00d052`). For schedule items R3 (this research) and R4 (the build) in `system_design/systems.json`.

**Status: an estimate, not a plan anyone has accepted.** It assumes Red takes the recommended answers in `20_for_red.md`. Answers that differ change it as noted at the end.

## 1. Basis

- **Unit:** planned agent hours (PAH), the unit of `systems.json` → `schedule.note`: about 32 PAH landed in 8 clock hours on 10-30 under rule 23, so **about 4 PAH per clock hour** while several lines run at once.
- **Ranges** are low–high. New work (water has never existed in this game) gets wide ranges.
- **Scope** is v1 as `10_research.md` defines it: the fitness club on a B level, wadeable water, the Relay under the same law, static water level, no washroom, no pump-room interior, no swimming.
- Owners follow the existing lines: 关卡设计 (map), 交互设计 (player), 追猎 (Relay), 游戏视觉 (looks, kit, `FrontRooms/Surface`), 声音设计, 平面视觉 and 叙事设计 (signs and strings), 设置菜单与画质 (quality tiers).

## 2. Work packages

| # | Package | Owner | Contents | PAH | Needs first |
|---|---|---|---|---:|---|
| P0 | Four spikes | 游戏视觉, 设置菜单 | Planar camera under Render Graph on 6000.6.3f1; analytic basin in a copy of `FrontRooms/Surface`; ring list and print quads; iPhone and iPad GPU time | 9 | nothing |
| P1 | Map group | 关卡设计 | A planned group carved before `Begin` (as `FrontRoomsStartGroup`): 5 × 8 hall aligned to one chunk, locker rooms, showers, towel desk, pump-room door. Basin cells skip the per-cell floor; basin, deck and gutter meshes. Clear of the stair-shaft cells. Revisit shifts leave it alone. On the route, the EXIT across the water. Validator: the group exists, the pool is not counted as a sanctuary. Water class per cell as data | 14–18 | Red's D1, D2 |
| P2 | Player | 交互设计 | Wet, Ankle, Wade classes from the cell; × 0.85 and × 0.5; no run in the wade; the plunge as a scripted drop with one event; climb-out on the 0.6 s climb with a taller lift; camera clamp over the basin and near plane ≤ 0.1 m; touch parity (walk into the edge) | 8–10 | P1 cells |
| P3 | Relay | 追猎 | `k` on every speed by cell; path cost `1/k`; basin edges Open with the 0.6 s step; Arrive never on water; the mirrored ray in `PlayerSeesRelay` and Arrive's tests; rig dropped by depth; hearing values (slow wade 6, ankle 3, plunge 18 / +8, climb-out 6); both v1 and v2 paths; bot run that starts mid-pool at stage 2 | 10–14 | P1 cells; D3, D4 |
| P4 | Water look | 游戏视觉 | Water surface (Fresnel, probe, rings, specular); analytic basin refraction and absorption in `FrontRooms/Surface`; caustic flipbook and thrown light; waterline in `Surface` and in the Relay's shader; wet floor and print quads; Reduce Motion path; WebGL variant; bake `Refl_Pool` and its dead-lamp twin | 20–28 | P0 |
| P4b | Planar reflection, desktop Ultra / High | 游戏视觉, 设置菜单 | Second renderer without SSAO, layer mask, ⅓–½ resolution, in-view only; Medium at ¼ half rate | 6–10 | P0 spike 1 passes |
| P5 | Surfaces and kit | 游戏视觉 | Options sheet for D8 (3–4). Six surfaces: pool mosaic, lane line and target, deck, wall tile with trim, basin edge, ceiling (9–12). About 16 props at hero LOD0: ladder, corner steps, guard chair, ring buoy and pole, wall phone, rules board, lockers ×2, bench, shower set, floor drain, main drain grate, gutter grate, wet-niche lamp, vapour-tight lamp, towel stack and bin (24–40). Lamp setup in the room set (4–6) | 40–62 | D8 |
| P6 | Sound | 声音设计 | Foley: TileWet, Ankle, Wade, plunge, climb-out, Relay water steps (CC0 library as since 10-22); Zone Pool with a 2.5–4 s tail; pump hum; captions; stage-2 acceptance re-measured by capture (rule 24) | 10–14 | P2, P3 events |
| P7 | Signs and strings | 平面视觉, 叙事设计 | Depth marks, NO DIVING, rules board, locker numbers, entry, MEN / WOMEN, PUMP ROOM; the pool log on the live clock; localisation per that line's rule | 8–12 | D1 |
| P8 | Land and check | each owner | Rules 22 and 23: Roslyn and the one covering suite per landing; one build and one test round for the whole | 4–6 | all |

**Total: about 129–183 PAH** (P4b included). Without P4b: 123–173.

## 3. Critical path

1. **Red's answers** (D1–D4, D8). R4's `after` already says "Red's picks by 11-01".
2. **P0 and P1 in parallel.** P1 is the long pole of the logic: nothing in water can be tested without basin cells.
3. **P2 and P3 together**, then a **playable greybox**: a grey basin with the probe-only water surface and placeholder kit. About **55 PAH** from the start.
4. **P4, P5, P6, P7 in parallel** on the greybox. P5 is the largest single package.
5. **P8.**

## 4. Calendar

R4 in `systems.json` today: start 11-02, target 11-07, a placeholder that says water is the least certain item.

- **Capacity.** Account B is weekly-limited until **Nov 3 17:00**. Until then account A carries every line and was at 42 % of its week on 10-31. R2 (Run! rooms, 11-01 to 11-03) runs beside R4 on the same map files, so P1 queues behind R2's map work.
- **Greybox (≈ 55 PAH):** at 4 PAH an hour that is about 14 running hours, in practice 11-02 to **11-05** with one account for the first day and a half.
- **Full v1 (≈ 155 PAH, mid-range):** about 39 running hours, in practice **11-08 to 11-09** with both accounts from Nov 3.

**Recommendation for 系统设计:**
- Split R4 into **R4a, playable greybox, 11-02 → 11-05**, and **R4b, v1 look and sound, → 11-09**. R4b is two days after today's placeholder.
- **To hold 11-07 instead**, cut: P4b (probe only everywhere, −6 to −10), the kit to ten props (ladder, guard chair, lockers, bench, shower set, ring buoy, wet-niche lamp, vapour-tight lamp, drain grate, rules board; −10 to −16), the thrown light (−2 to −4). That is about −20 to −30 PAH, roughly a day.
- **R3** (this research) is done on 10-31, its target.

## 5. If Red picks differently

| Answer | Change |
|---|---|
| D1 B, C or D (another origin) | P5 and P7 re-scoped; B (old bathhouse) also needs a new tile and arch family, +15–25 PAH, and fights the Poolrooms guard |
| D2 B (a club on every B floor) | P1 +6–10: placement per floor, variety so it does not repeat |
| D3 B (the Relay stays out) | P3 −4, but the pool becomes a sanctuary: the sanctuary budget and §10 rules need rework, +4–6 for 关卡设计 |
| D3 C (the Relay wades faster) | P3 same; breaks RELAY §5's "walking escapes anything but a chase"; needs Red's word on that rule |
| D5 B (the Relay reads prints in its cone) | P3 +4–6 and a new sense to teach; P8 +2 |
| D6 B (a swimming deep end) | P2 +10–14 (surface swim, eye fixed above water), P3 +6–10 (the Relay in deep water), P4 +4 (deeper absorption); about +25 PAH |
| D7 B or C (washroom module) | +10–16 (module, fixtures); a live mirror adds +6–10 and the reflection-leak work for mirrors |
| D9 B (Mac ray-traced water in v1) | +20–40 PAH, with a timing risk on top of the 5.4–6.0 ms the glass already costs |
| **D1 all** (Red 10-31 ~14:00, K chat: "全部都做，像是Claude那样出了图片之后我来负责筛选方案/variation"): the club on B2 leading into a pool level beyond it ((c) in full), the look chosen from images first | Direction images 6–10 (about 20 greybox stills of a, b, c with variations, `40_direction_images.md`); P1 +10–16 (wading and connector cells beyond the far door, water across doorways, EXIT moved into the level); P3 +2–4; P4 +6–10 (water across doorways, ankle-deep rooms, more caustic area); P5 +15–25 (full-tile family, tiled ceiling, more modules, no arches); P6 +2–4; P7 ±0; the connector lamps are 游戏视觉's `zone_lamps` P6 (its estimate). **About +41–69 PAH → 170–252 PAH** (P4b included). At about 4 PAH per running hour: R4a ≈ 11-06, R4b ≈ 11-12 to 11-13 (K's guess, 10-31; 系统设计 dates it) |
| **Structure M1–M7 + palette F1** (Red 10-31 ~15:35, K chat: all seven moves of `18_structure_fusion.md` kept; club palette F1, turquoise white from the club on) | On top of "D1 all": M1 suspended-ceiling bays over water with dead troffers and missing tiles (reuse Level 0 ceiling) +2–4; M2 tiled column module and grid rules +2–3; M3 fire stair into water, drowned gate, the dark zone shell and its lighting +11–17; M4 service gallery with observation windows, incl. an underwater view behind glass (shader work, 游戏视觉) +10–16; M5 arched door module, keystone and pilaster trims, glass-block wall material, oculus +8–12; M6 repeated club sequence (level design), racquetball court / aerobics room module without mirrors (D7) +6–10; M7 beach-entry cells, channels, weirs between two water heights, slide tube +12–18; F1 zone look (ambient, fog, `FrontRoomsPost_Pool` grade, lamp colours; 游戏视觉) +4–6. **About +55–86 PAH → 225–338 PAH.** `17_water.md` (water sub-session) may move P4/P4b again. Proposal for 系统设计: R4a greybox (club, hall, T rooms, M1 M2 M3, dark-zone shell) ≈ 11-07; R4b look and sound (F1, water, M5, M7) ≈ 11-13; a new R4c (M4 windows, M6 court, polish) ≈ 11-17 (K's guess) |
| **Water technique** (`17_water.md` §6, water sub-session of the K session, 10-31 15:30) | P0 9 → 10–12; P4 20–28 → 24–34; P4b 6–10 → 10–16; new **P4c** desktop wave window and surface-driven caustics 12–18 (Q2: v1 or v1.1). **About +21–33 PAH → 246–371 PAH** with the rows above. Critical path unchanged (P4c runs beside P4/P5). Later **P4d** Mac ray-traced water through GlassRT +16–26, not in v1 (Q3) |
| **Red's water answers** (10-31 ~15:4x, K chat: Q1 "wrong", Q2 "v1", Q3 "v1", Q4 "follow") | Q1: club water physical, level water absorption ×3–5, one or two rooms ×9 (no size change). Q2: P4c in v1 (already counted). **Q3: P4d, ray-traced water on the Mac through GlassRT, moves into v1: +16–26 PAH → about 262–397 PAH.** It depends on GlassRT (G14) and its D1 (c) gate landing first, adds about 3–5 ms on 16+ core Macs [EST], and puts 游戏视觉's plugin on the R4b path. Q4: the dark zone's grade follows the warm wet-niche lamps (part of the F1 grade ask) |
| **Dark zone one level down in v1** (Red 10-31 ~15:36, K chat: picked "lower"; `50_modules_and_contracts.md` C9 revised) | A sunken region (floor about -2.52 m, water -1.5 m) reached by walking down the fire stair: P1 +8–14 (second height in chunks, stair cells, streaming), P3 +10–16 (Relay pathing, sight and hearing across the stairwell), P2 +2–4 (stairs into water), validators +2. **About +22–36 PAH → about 284–433 PAH.** Lands with R4a's greybox at the earliest; puts 关卡设计's Relay work on the critical path |

## 6. P2 confirmed by 交互设计 (2026-11-01, answer to bathroom-111)

Scope accepted as in §2 P2 and `50_modules_and_contracts.md` §4, with C4 and the revised C9 added. No swimming (D6 A as recommended). Measured against main: the player is a `CharacterController` (step offset 0.3 m, gravity), the window climb is 0.6 s with a 0.35 m lift and a level landing (`TryStartClimb`, `Climb`), the desktop near plane is already 0.06 m.

| Piece | PAH |
|---|---:|
| Water class at the feet from `WaterAt` (C2), measured as feet height against `waterY` so a BeachEntry slope blends Wet → Ankle without a step | 1–1.5 |
| Speed × 0.85 Ankle, × 0.5 Wade; no run in the wade on keyboard, pad and touch (the past-the-disc run drag answers as a walk); stamina untouched | 1–1.5 |
| Gait events for P3 and P6 only (TileWet, Ankle, Wade steps; slow wade); game code posts, it plays nothing | 0.5–1 |
| Plunge: walking off a deck edge into a basin cell is a scripted drop (about 0.35 s) with one `PlayerPlunged` event and a camera dip floored at 0.12 m above the surface; a Reduce Motion path; one heavy haptic on handhelds | 1.5 |
| Climb-out: walking into a basin edge (every device, touch included, as with windows) runs `Climb` with a landing at deck height. The lift is 1.07–1.37 m, not 0.35 m: proposal 0.9–1.1 s instead of 0.6 s (a Red-visible feel choice; until then 0.6 s with the taller lift). Corner steps and ladders are walked, no climb | 2 |
| C4: BeachEntry needs nothing from the controller (about 5°); a weir edge is a step down by gravity, a step up by the 0.3 m step offset; a weir higher than 0.3 m uses the climb-out | 1–1.5 |
| Camera clamp over a basin: the eye never below the surface plus 0.12 m (stance drops, plunge dip); the handheld near plane checked ≤ 0.1 m | 0.5–1 |
| Flat-floor assumptions: Peek and Inspect floor height, body-door `feet`, key pickup ray (no door leaf in water, C12, so mostly a check) | 0.5 |
| C9 (revised): the fire stair's 0.18 m risers walk on the step offset; class changes along the flight; the eye clamp in the 1.02 m dark-zone water | 2–3 |
| Tests: KeyBinding and touch driver checks, a bot that starts mid-pool, Roslyn; one combined test (rule 25) | 1–1.5 |

**P2 = 11–15 PAH** (§2's 8–10 plus C4 +1–2 and C9 +2–3; the C9 row in §5 said +2–4). Needs first: P1 cells with `water`, `floorY` and `waterY` (C2), and P3's hearing values to name the events. Risks: the climb-out time is a feel choice for Red; touch run suppression in the wade must not teach that the run is gone elsewhere (the run returns on the deck with no message).

**Decided 2026-11-01 14:06 by Red** (总控 chat, account F, answer to the climb-out ask: option 1 = 0.6 s as the window climb, option 2 = about 1.0 s for the 1.07–1.37 m lift, 总控 recommended 2): "1.0 秒（推荐）". The climb-out out of a basin runs `Climb` in **1.0 s** with the landing at deck height; the window climb stays 0.6 s. No PAH change (a constant). Builder: 交互设计 with P2.
