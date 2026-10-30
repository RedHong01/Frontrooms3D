# 54: Door break dust and grit. Research and spec (2026-10-30)

Owner: 游戏视觉 (game visuals), account A, session local_5656cffd, door-break production workflow, stage "research + spec: the dust and grit of a door break". Status: SPEC for the production stages. **Nothing in this file is built yet and nothing lands in main.** It replaces the dust part of `50_rethink.md` §3.9 ("Dust: ≤ 16 soft lit cards per blow … 40 at the break") and the dust in the DB2 prototype (`51_prototype.md`).

Red's ask, 2026-10-30, verbatim: "同时目前门被撞之后掉落下来的碎沙，我希望能够有更多颗粒，这部分需要你research一下，现在白色的原型雾气看起来不是特别的realistic"

Frames as in `50` (door root: metres, origin on the pivot axis at the floor, +Y up, +Z along the opening with the latch end at Z 0.995, +X = the burst face after the root's mirror). Screen scale from `50` §1.4: 230 px/m at 3 m (1 px = 4.3 mm), 690 px/m at 1 m (1 px = 1.45 mm), 2,300 px/m at 0.3 m (1 px = 0.43 mm), at 1080p and a 76° vertical FOV.

Tags in [brackets] are in §6. [read] = the text was read; [viewed] = frames were looked at by this stage (contact sheets made from the YouTube player, nothing downloaded); [search] = a search summary only, not opened.

---

## 0. Short answer

1. **Why it looks like white fog.** The prototype has one dust type: big soft round cards (0.10 to 0.36 m across and growing), a radial-blob texture, a pale beige colour (0.78, 0.74, 0.66) lit by the troffers, alpha 0.25 to 0.55, almost weightless (gravity −0.06 m/s²), 16 per blow and 40 at the break. That is a cloud of round white puffs. And the size class Red is asking for, **grit of 0.1 to 2 mm, does not exist in it at all**: the only small solids are 30 to 120 chips (3 to 16 mm) and 14 to 30 crumbs (2 to 5 mm).
2. **What real impact debris is:** mostly grains. A particleboard core is made of wood particles bonded at a low internal strength, so when it fails it sheds its own particles: crumbs and flakes of 1 to 6 mm, grit of 0.1 to 1 mm, and a small fine fraction. Everything at 0.3 mm or more falls in 0.5 to 3.5 s, **size-sorted**: crumbs first, then grit, then a slow fine sift. Only particles under about 0.1 mm hang in the air. They make a thin tan veil that is almost invisible under diffuse ceiling light and **only bright when it is back-lit**, for example in a shaft of light through a gap. Footage agrees: sawdust in the air is a speckled tan field of separate grains, not a smooth puff [DG-F1 0:03-0:09], and debris falling from a ceiling is a streaky vertical curtain with a low translucent veil behind it [DG-F2 5:54-6:04]. Neither is ever round or white.
3. **The spec replaces the fog with five layers:**
   - **Grit:** GPU-drawn grains, 0.1 to 2 mm, a few thousand per break. They are streaked by their own motion, never thinner than one pixel (their opacity carries the true size), and coloured per grain from the real door materials.
   - **Crumbs and chips:** mesh particles, as today, with three wood materials.
   - **Haze:** a thin, lit layer of wisps. It is never round, fades into geometry and near the camera, and is lit by a forward-scattering phase function, so it glows only against light. It also gets an analytic light shaft through the split, slot and notch, driven by the same far-lamp level as the leak card.
   - **Dust skin:** one floor quad per door that the landed grit builds up, and that persists.
   - **The trickle:** after D2 and FINAL the crumbled core keeps pouring sand out of the slot and the notch for about a second. This is the "碎沙掉落" read at 3 m.
4. **Counts, desktop hero:** grit 260 / 460 / 800 / 1,270 / 2,350 for E / M / D1 / D2 / FINAL, plus 800 at the break (≈ 5,900 per T1 break, at most 6,144 live). Crumbs 0 / 24 / 60 / 120 / 300 / 40. Chips 0 / 10 / 30 / 30 / 120. Haze 3 / 6 / 8 / 10 / 14 / 8 wisps.
   **Phones:** about 25 % of the grit (live cap 1,536), 33 % of the crumbs and chips, 40 % of the haze. The phone set is the first N of the desktop set, picked by a keyed hash, so the two tiers never disagree.
5. **Budgets:**
   - Draws: 7 at most while anything is live (grit 1, crumbs 3, chips 1, haze 1, skin 1); after the settle, the skin adds 1 draw beyond P1's floor merge.
   - Grit: ≤ 6,144 instances × 6 procedural vertices, from one 192 KB buffer.
   - Haze fill: ≤ 0.15 screens at C1 (3 m) and ≤ 1.0 screen in the worst close view (C2 at 1 m, FINAL), at 1080p; phones ≤ 0.35 screen.
   - CPU: ≤ 0.2 ms on the main thread plus ≤ 0.3 ms in jobs at the peak.
   - Memory: ≤ 0.6 MB shared, plus 12 KB per broken door.
6. **Deterministic:** keyed hashes instead of the prototype's sequential `System.Random`, emission snapped to the 1/120 s substep grid from the blow time, analytic collisions including the leaf on the baked throw curve, and a restore from baked clusters and patches. The 16-byte record does not change.
7. **Delete from the prototype:** `DustTexture`, `DustMaterial` (URP Simple Lit beige), the `D` struct and its list, `HeadDust`, `BreakDust`, `BuildDust`, the crossed cards, the size growth and the floor clamp, plus the two calls in `FrontRoomsDoorBreakVisual` (`HeadDust(16)`, `BreakDust(40)`). Details in §4.10.

---

## 1. Diagnosis: the prototype's dust (clone `W/proj_doorbreak`, read 2026-10-30)

| What | Where (clone) | Effect on screen |
|---|---|---|
| A radial blob texture: `a = (1 − r)² × (0.55 + 0.9 × Perlin)` on a 64² RGBA | `FrontRoomsDoorBreakVfx.cs` L92-111 | Every puff has a **round** soft silhouette. Real dust starts as jets, sheets and falling streams; it becomes shapeless only after about 1 s and is never disc-shaped |
| `Particles/Simple Lit`, `_BaseColor (0.78, 0.74, 0.66)`, alpha blend, no soft particles | L113-129 | A pale grey-beige lit by 0.96 to 0.99 lamps reads **white** against the walnut leaf and the dark doorway. There is no depth fade, so it cuts flat against the leaf and floor |
| Crossed vertical card pairs with a fixed normal | L301-324 | Lambert on a vertical card: every puff is lit the same from every side, like matte paint. At angles the crossing shows as an X |
| Half-size 0.04 to 0.18 m, growing +0.05 m/s; alpha 0.25 to 0.55 | L174-205, L252 | **0.1 to 0.36 m blobs**, so 23 to 83 px each at 3 m. Each card reads on its own as a puff |
| Drag 1.6 /s, gravity −0.06 m/s², clamped at y ≥ 0.05 | L249-253 | The dust floats and drifts and never falls. Real dust is mostly grains that fall |
| 16 cards per blow (5/8 at the head), 40 at the break | Visual L314, L357 | A few large puffs, instead of many small grains |
| Solids: chips 30 per stage and 120 at FINAL (3 to 16 mm); crumbs 14 / 30 at D2 / FINAL (2 to 5 mm) | Vfx L138-172; Visual L300, L315 | No grains under 2 mm at all, so **nothing reads as sand** |
| A sequential `System.Random` | L41, L58, L131-133 | Deterministic, but particle k depends on everything emitted before it. A phone tier with fewer particles, or a 3-blow take, reshuffles every later particle |

The prototype's own frames show it (look at the files; they are not reproduced here): `W/door_break/DB2/out/full/S3_C2_000681.jpg` (D1, 1.53 s: a chain of white blobs along the latch edge), `S3_C2_000768.jpg` (2.98 s: white haze over the leaf), and `S1_C1_000752.jpg` (2.72 s, player view: a grey puff in the doorway at the Relay's chest).

---

## 2. Research

### 2.1 Footage (viewed this stage; contact sheets from the player, nothing saved)

| Tag | Clip | Times viewed | What it shows | Use |
|---|---|---|---|---|
| [DG-F1] | Explosive Science, "Slow Motion Blowing Sawdust" (2014), https://www.youtube.com/watch?v=Mh4xGCn1eMs | every 2 s, 0:01-0:23 | **0:03-0:09:** a speckled tan cloud of **separate visible grains**, warm brown where lit, against black. **0:11-0:15:** the dense core is gone; a sparse field of specks falls out. **0:17-0:23:** almost clear, with grains resting on the board and only a faint veil left. Never a smooth or round puff | Size sorting; speckle, not fog; tan, not white |
| [DG-F2] | Neil Coker / Hawthorn Plaster Repairs, "Slow - Motion Plaster Ceiling Collapse" (2018), https://www.youtube.com/watch?v=AK3TA1ws-sA | every 30 s, then 5:54, 5:56, 5:58, 6:00, 6:02, 6:04 | **5:54-6:04:** as the board is pulled, old dust and gypsum fall as a **streaky vertical curtain**: dark grey-brown granular strands, with a translucent grey veil that lags behind and spreads low near the floor. **0:10:** a chunk drops with a short trail. The fall column holds its shape; it does not balloon | The head sift and the break's top-pivot tear: a falling curtain plus a low veil; aged dust is grey-brown and gypsum is off-white |
| [DG-F3] | CuteStockFootage, "Slow Motion Dust Particles On Light Beam" (2020), https://www.youtube.com/watch?v=CyqgHB3yCmI | not viewed (stock) | (listed for Red) dust seen only inside a beam | The beam-only visibility, §2.4 |
| [DG-F4] | Livermore-Pleasanton FD, "Forcible Entry - Inward Swinging Door" (2017), https://www.youtube.com/watch?v=qp8Ipf-Xcbo | every 4 s, 0:03-0:47 | A steel training prop with replaceable inserts. No debris, no dust | Rejected |
| [DG-F5] | 1fastdrawgal, "SlowMo Door" (RED demo, 2013), https://www.youtube.com/watch?v=mq1T0yCudGw | every 6 s, 0:03-1:19 | A prop panel door kicked flat from 0:45. No splinters, no dust | Rejected |
| [DG-F6] | Ubisoft, "R6 Siege X: Environmental Destruction" (2025), https://www.youtube.com/watch?v=ELrtKTGiI84 | every 4 s | A marketing cut. **0:45:** a breach with a debris spray of chunks and paper, with dust kept thin and brief | Weak support for "debris, not smoke" |
| [DG-F7] | Blacktail Studio, "Woodshop Safety in Slow Motion" (2024, 80,000 fps), https://www.youtube.com/watch?v=03JlqoZfBfw | chapters 3:07-4:50 | Kickback demos; no dust read at this sampling | Rejected |

No usable slow-motion clip of a real solid-core door breaking was found: searches on YouTube, Bing and stock sites gave props and memes (also still open in `02` §10). The wanted list is in `media_candidates.md`, "Research 2026-10-30 (dust and grit)".

### 2.2 The material: what the debris is made of
- **Particleboard is graded.** It has finer particles in the face layers and coarser ones in the core [DG-M1]. Commercial core furnish carries many fines: swapping 20 % of the fines for coarse particles raised internal bond by 40 % [DG-M2]. Core density is about 660 to 700 kg/m³ [DG-M3]. A three-layer board has denser faces [T-PB].
  - So a failing core sheds its own furnish: flakes and chips of 1 to 6 mm, fines of 0.1 to 1 mm, and a little true dust. It crumbles; it does not splinter (`02` §2, `50` §2.1).
- **The other materials on our door:**
  - the hardwood stile and lock block: long fibres and slivers of 5 to 180 mm, with fibre dust;
  - the walnut face veneer, about 0.6 mm thick: dark red-brown slivers;
  - the urea-formaldehyde glue line: dark amber flecks, under 1 %;
  - the frame and wall: gypsum and joint compound. Frames flexing under door impacts crack the compound at the frame corners [DG-B1, search];
  - on top of the leaf: the leaf's top edge is a 44 mm horizontal ledge that is never cleaned. In a 1990 office it holds years of grey-brown settled dust and lint, and every blow shakes it off. This is the head sift.
  - **Steel** (P3): enamel flakes in the door colour, primer specks, kraft honeycomb paper bits at the opened seam, and gypsum crumbs from the frame anchors. No wood.
- **Colour, measured from our own textures** (mean sRGB, 2026-10-30). The particles take their palettes from these same maps (§4.4), so the grains match the cut faces:

| Texture | Mean sRGB | Role |
|---|---|---|
| `Door_CoreWood_A` (clone) | 159, 133, 97 | particleboard core: tan |
| `Prop_Chipboard_A` | 154, 133, 95 | the same family |
| `Door_CoreStile_A` (clone) | 196, 151, 102 | fresh hardwood fibre: pale, lighter |
| `DoorVeneer_A` (walnut, landed 10-30) | 132, 81, 61 | walnut slivers: dark red-brown |
| `Office_Drywall_A` | 181, 176, 150 | the papered wall face. A gypsum core is whiter, about 225, 222, 212 (ESTIMATE) |
| `Carpet_LoopPile_A` (Level 0 floor) | 139, 121, 77 | **almost the colour of the core grit** |
| `Office_CarpetTile_A` | 89, 89, 89 | grey tile: tan grit reads strongly on it |

  On the Level 0 loop pile, core crumbs (159, 133, 97) nearly vanish by colour. The floor read must come from the **pale fresh stile fibre**, the **dark walnut slivers**, and the small lightening of the dust skin.

### 2.3 Ballistics: how fast each size falls
Aerosol settling goes as d² (Stokes; about 0.003 × d[µm]² cm/s for unit density, 1 to 100 µm [DG-P1]). Above about 0.1 mm the drag turns quadratic. The table below is this stage's own calculation (Schiller-Naumann drag; wood particle 650 kg/m³ with a shape factor of 1.6 on the drag for flakes; gypsum 1,000 kg/m³ with 1.3). **All values are ESTIMATES.**

| d (mm) | Wood terminal speed (m/s) | Time to fall 1.1 m (bore) | 2.1 m (head) | Gypsum terminal speed | What the eye sees |
|---|---|---|---|---|---|
| 0.05 | 0.03 | 37 s | 71 s | 0.06 | hangs: **haze** |
| 0.1 | 0.11 | 10 s | 19 s | 0.20 | a slow drift; a "snow" only inside a beam |
| 0.2 | 0.34 | 3.2 s | 6.2 s | 0.58 | **fine sift**: a lingering fall |
| 0.3 | 0.60 | 1.9 s | 3.5 s | 0.97 | sift |
| 0.5 | 1.10 | 1.1 s | 2.0 s | 1.70 | **grit**: streaks |
| 1 | 2.2 | 0.66 s | 1.1 s | 3.3 | grit; crumbs |
| 2 to 5 | 4 to 7 (flakes flutter; use 2.5 to 4) | 0.5 s | 0.7 to 0.8 s | 6 to 10 | **crumbs**: near free fall, one bounce |

- So **each blow leaves a size-sorted rain.** Crumbs land in about 0.5 s. Grit lands in 1 to 2 s. The fine sift goes on for 3 to 6 s. With blows every 0.5 s these overlap: by D2 there is a continuous sand sift from the head and the split that grows with every blow. That is the read Red asks for.
- **Haze:** under 0.1 mm the dust moves with the air.
  - It leaves as a short jet, because the jolt pumps air through the crack, and the jet stops within about 0.15 s.
  - It is then carried by the leaf's gust at the break. The latch end sweeps 0 → 105° in 0.12 s, about 15 m/s at the tip.
  - Dusty air is heavier than clear air, so the veil slumps slowly toward the floor ([DG-F2] 5:58-6:04).
  - It stops being visible by dilution within 1.5 to 3 s.

### 2.4 Light: when dust is visible
- **Dust is seen by scattering, and it scatters forward.** The Henyey-Greenstein asymmetry g is about 0.2 to 0.7 for aerosols and above 0.65 for mineral dust [DG-P2]. With g = 0.6, normalised so isotropic = 1:
  - back-lit (light coming toward the eye): **×10**;
  - side-lit: ×0.40;
  - front-lit: ×0.16.
- **Our office is lit by ceiling troffers.** A player looking roughly level sees dust side-lit from above, so it is faint. Dust becomes visible where light comes *toward* the player: through the split, the slot and the notch, and through the opening after the break, when the far room is lit. That is the "visible mostly in light shafts" behaviour. It is also why the prototype's isotropic Simple Lit cards look wrong: they are equally bright from every side.
- **Games:**
  - Battlefield 3 Close Quarters: "the focus here should be on debris rather than smoke", with dust bursts that "burst and dissipate quickly" so rooms are not "literally filled with smoke for long periods of time" (Nadab Göksu, DICE, 2012) [DG-G1, read].
  - Bayonetta 3 drew rubble and glass debris as GPU particles, "a lot of debris with a low load", and added point-light calculations so debris picks up the colour of nearby lights (Yasuda, PlatinumGames, 2023) [DG-G2, read].
  - Control: rigid bodies, then particles plus decals, then flipbooks for dust [M-CT2].
  - DOOM (2016) lights particles in a decoupled, resolution-independent pass, with a particle light atlas [DG-G3, search].
  - Frostbite puts particles into one physically based participating-media model with a phase g [DG-G4, search].
- **Unity (URP 6000.6):** Particles Lit "Soft Particles" and "Camera Fading" exist and work only with the URP Depth Texture enabled [DG-G5, read]. Ours is on: `FrontRooms_URP.asset` `m_RequireDepthTexture: 1`, Forward+ (`m_RenderingMode: 2`), HDR, MSAA 4×, which settings can lower to 2× or 1× (`FrontRoomsQuality.cs` L225-226). The project has no Particle System and no VFX Graph module (`50` §2.5), so VFX Graph six-way lighting [DG-G6] is not available as is. Its idea (baked lighting directions in a flipbook) is noted as a later option for a dense FINAL puff.
- **Sub-pixel grains:** a 0.5 mm grain at 3 m is 0.12 px wide. Humus' phone-wire AA "clamps the width to a pixel and instead fades with an alpha value" [DG-G7, read]. Applied to grit, a grain is drawn at least one pixel wide, with opacity equal to its true covered fraction. Its motion streak over a 1/120 s exposure (half of a 60 fps frame) gives the length: 2 to 20 px for 0.5 to 5 m/s at 3 m.

### 2.5 Floors and what stays
- **Carpet keeps fines in the pile.** Coarser particles come back up more easily from carpet than from hard floors when people walk on it [DG-P3, search]. In a loop pile:
  - grit under about 1 mm drops between the loops and stops being seen as grains, leaving at most a faint lightening on the loop tops;
  - crumbs and splinters sit on top.
- **On a hard floor** (VCT in station zones), grit stays visible as a scatter, and it bounces and slides.
- **After a real break, the floor shows:**
  - a fan of pale fresh-wood chips and splinters;
  - dark veneer slivers;
  - crumbs, mostly within 0.6 m of the lock;
  - a faint pale patch under the notch and under the head line on both sides.
  The patch is visible in grazing light and gone from far above (`50` §3.4 already places the strip and the fan).

### 2.6 What this means for our door (synthesis)

| Class | Size | Share of count (wood) | Colour | Ballistics | At 3 m | Kept on the floor? |
|---|---|---|---|---|---|---|
| Haze | < 0.1 mm | (optical depth, not count) | tan (split), grey-white (head, gypsum) | air-borne: jet, gust, slump | only back-lit: a glow in the slot shaft, a veil in the doorway | no |
| Fine sift | 0.1 to 0.3 mm | 45 % of the grit | per palette | v_t 0.1 to 0.6 m/s: a lingering fall | faint streaks, bright in shafts | carpet: into the dust skin |
| Grit | 0.3 to 2 mm | 55 % of the grit | per palette | 0.6 to 4 m/s: streaks | **the sand**: streams and sifts | carpet: the skin; hard floor: scatter |
| Crumbs | 2 to 6 mm | ≈ 10 % of the grit count | core / stile / veneer | near free fall, 1 bounce | specks of 1 px | yes, merged mesh |
| Chips and slivers | 6 to 50 mm | tens | stile, veneer | ballistic, flutter | 2 to 12 px | yes, merged mesh |
| Baked debris | 30 to 660 mm | 31 pieces (`51`) | per piece | baked throw | the big read | yes (`50` §3.9) |

---

## 3. Design rules for dust and grit
1. **Grains, not fog.** The particles that read are solid grains, streaked by their motion. Haze is a thin, lit layer under them and never carries the read alone.
2. **Never opaque, never white.** The haze's own colour is the dust albedo (tan for wood, grey-white for gypsum) times the light it scatters. One wisp's opacity is ≤ 0.12, and the haze stays ≤ 0.30 where it is densest. Its outline is never round.
3. **Light decides visibility.** The haze and the grit use the forward-scattering phase (g = 0.6, back-light gain clamped at ×6). The far lamp leaks through the split, slot and notch as an analytic shaft. Front-lit dust nearly disappears; back-lit dust glows.
4. **Physical sizes and speeds, one tunable visual gain.** The motion is physical (§2.3). The *drawn* grain size is `d × GritVisualScale(distance)`: 1.0 within 1.2 m, rising to 1.5 at 3 m and beyond (phones 1.3 → 2.0). It is flagged for Red. With the gain at 1.0 everywhere the look is purely physical.
5. **Size-sorted, discrete, on the beat.** Every emitter fires on the blow frame (snapped to the substep). The only thing that continues between blows is the falling and trickling that real grains do. No new damage appears.
6. **Dust is gone ≤ 3 s after the break** (Battlefield's rule). Only the floor keeps a trace.
7. **Deterministic and tier-consistent.** The phone set is a prefix of the desktop set. Restores are exact statistically: the scatter comes back from the same seed, and single grains cannot be told apart.
8. **Render only, silent.** No map changes; nothing makes a sound (rule 24); the sound ask is optional (§4.12).

---

## 4. Spec

### 4.1 Particle types

| Type | Draw | Shape | Material and colour | Lighting | Notes |
|---|---|---|---|---|---|
| **Grit** (0.1 to 2 mm, all materials) | **1 draw**: `Graphics.RenderPrimitives` (6 vertices per grain, no vertex buffer), reading a `GraphicsBuffer` of grains (32 B each) written once per frame by the CPU sim. It runs for every camera, so the film harness's multi-camera renders work | A camera-facing quad stretched along the grain's **screen-space velocity** × 1/120 s exposure (streak capped at 24 px). It is at least 1 px wide; the opacity carries the true coverage (phone-wire AA [DG-G7]). The sprite is one of 8 irregular grain silhouettes in a 128 × 16 R8 atlas, with a pseudo-normal from the silhouette, so grains read as specks at 0.3 m and never as squares | New shader `FrontRooms/DoorGrit` (Resources/Rendering, 游戏视觉's path). Colour = `palette[material][index]` (8 linear entries per material, sampled from the textures in §2.2 at build time), × a per-grain value jitter ±12 % | Per vertex (all 6 vertices share one grain): Forward+ cluster loop, ≤ 8 lights, Lambert with wrap 0.3 on a **tumbling** normal (spin keyed per grain, which gives the twinkle of DG-F1), + the slot or notch shaft (§4.6) × the HG phase + SH ambient | **MSAA ≥ 2: `AlphaToMask On`** (order-independent, writes depth, no sorting). **MSAA 1:** a coverage-weighted keep: grain i is drawn opaque if `hash(i) < coverage`, else dropped. This is decided in the vertex shader from a `_FR_GritMsaa1` uniform set from `FrontRoomsQuality`, so it needs no keyword. The 1 px floor uses `_ScreenParams` (the render scale is respected) |
| **Crumbs** (2 to 6 mm, wood) | **3 draws**: a dynamic mesh per material, as the prototype does | Irregular polyhedra, 8 shapes of 8 to 14 triangles (glass-crumb style [GD3c], not the prototype's 8-triangle octahedron). Flakes are 2.5:1:0.4 | `Door_CoreWood` (65 %), `Door_CoreStile` (25 %), `Door_Veneer` walnut (10 %). FrontRoomsSurface with mesh UVs at a random metric offset per crumb, so each crumb shows a different patch of the real texture. No vertex colour is needed | FrontRoomsSurface, shadows off | They are merged into the floor mesh at the settle (P1) |
| **Chips and slivers** (6 to 50 mm) | **1 draw** (existing) | Thin tapered prisms (6 triangles), 2.5 to 6 mm wide, along the grain. They replace the 4-triangle tetra `ChipShape` | `Door_CoreStile` (fibre along U); 1 in 6 is `Door_Veneer` (a walnut skin) | FrontRoomsSurface | As today, but emitted along the split polyline (§4.3) |
| **Haze** | **1 draw**: `RenderPrimitives` from a 64-entry buffer, premultiplied alpha (One, OneMinusSrcAlpha), transparent queue, no sorting (thin, single-colour veils blend almost in any order) | A camera-facing quad. Its long axis follows the wisp's screen velocity, with aspect 1:1 to 1:3 by emitter. The texture is an 8-frame **wisp atlas** (1024 × 512 RG8: R density from domain-warped fBm with filaments, under an anisotropic window that is never radially symmetric; G erosion order). Over its life the wisp **erodes** (alpha threshold rises with G) and breaks into filaments instead of fading as a disc | New shader `FrontRooms/DoorHaze`. Albedo per emitter (linear): wood split (0.45, 0.33, 0.19); head dust (0.36, 0.33, 0.29); gypsum (0.70, 0.68, 0.63) | Per pixel on desktop: ≤ 4 nearest lights from the Forward+ cluster, **with their shadow maps** (so haze in the leaf's shadow is dark and haze in a lit gap is bright), × HG(g 0.6) normalised so isotropic = 1, back gain clamped at ×6; + the analytic slot or notch shaft (§4.6); + SH ambient × 0.5. **Soft particles**: depth fade over 0.15 m (`_CameraDepthTexture`). **Camera fade**: 0.25 → 0.6 m. Alpha = 1 − exp(−τ). Phones: per vertex, no shadows | Optical depth τ0 at a wisp's centre (desktop): head 0.05, split 0.08, FINAL 0.12, break head 0.08. Pixel alpha is clamped at ≤ 0.30 |
| **Dust skin** | **1 draw** per damaged door (transparent, after opaque, `Offset −1,−1`, 1.5 mm above the floor) | One quad 1.6 × 1.4 m on the burst side (Z −0.30 to 1.30, X 0 to 1.40) plus 1.0 × 0.5 m under the head on the push side. A 128 × 112 R8 density texture (12.5 mm texels) | New shader `FrontRooms/DoorDustSkin`. It blends toward the fines albedo, with alpha = 0.35 × density, × a 0.5 to 1.0 pile mask (dust sits on loop tops), taken from the luminance of the floor's own albedo (`Carpet_LoopPile_A`, sampled in world metres at the carpet's tiling) | SH and the main lights per vertex | **Carpet:** grit under 1 mm is absorbed on landing (it fades over 0.15 s and adds its mass into the texture). **Hard floor:** grit bounces (e 0.25, friction 0.6), rests, and is splatted into the texture after 2 s at rest, which shows a grain-noise detail where the density exceeds 0.3. The skin is uploaded at most 10 times a second, and only while grit lands |

### 4.2 Simulation (all types)
- **One `FrontRoomsDoorBreakVfx` per damaged door** (as now). It uses fixed 1/120 s substeps, analytic collisions in the door root, and runs as an `IJobParallelFor` in the `FrontRoomsGlassDebrisJobs` style (Burst only if the visual line already ships it; it is not required). The main thread does the emission, one `GraphicsBuffer.SetData` (≤ 192 KB) and the mesh uploads.
- **Forces per class:**
  - gravity;
  - quadratic drag set so that each grain reaches its terminal speed from §2.3 (per-grain d and density);
  - flakes of 1 mm or more flutter: a lateral sine of 0.05 to 0.15 m/s at 3 to 6 Hz, keyed per grain;
  - haze: `v = v0·exp(−t/0.15) + gust + slump + 0.05 m/s curl drift`. The slump is −0.08 m/s, ramped in after 0.4 s. The size grows as `s0 + (s1 − s0)(1 − exp(−t/0.5))`.
- **Gust at the break.** For 0.25 s from the throw start, haze and grit under 0.5 mm inside the swept wedge get 0.25 × the leaf's surface speed at their radius, along the leaf normal, decaying with τ 0.2 s. The leaf angle comes from the throw curve in `Kit_DoorLeafBreak_Veneer_DA_throwN.json` (the same curve the bake used), so the gust is deterministic.
- **Collisions** (analytic, door root):
  - the floor at y 0, where carpet absorbs (above) and a hard floor bounces;
  - the wall slab |x| < 0.08 outside the opening;
  - **the shut leaf** (|x| < 0.022 inside the leaf footprint) until FINAL;
  - after FINAL, the leaf as a kinematic plane on the throw curve. Grains hitting it take its velocity at the contact (Amnesia's rule, `50` §2.2).
  - Grains do not collide with the baked debris; at these sizes it cannot be seen.
- **Life:**
  - grit: until it lands, at most 7 s;
  - crumbs and chips: until they rest, then they stay for the merge;
  - haze: 1.0 to 2.6 s by emitter (§4.3).
  - The last haze is gone ≤ 3.0 s after the break.

### 4.3 Emitters per beat (T1 order; fewer blows drop beats from the front, as `50` §3.2)

Counts are **desktop hero / phone**. Positions come from the cassette sidecar: `head_dust_a/b`, `bore`, `split_top/bot` per stage and the motif polyline, `notch`, `leak_card`, `contact_push`, and the pivots. The **split** emitters sample *along the active segment of the motif polyline*, uniform by length, on the burst face (x = +0.022 in the leaf's current pose, so the rock carries them). **10 % of every split count goes to the push face** (veneer crushed at the Relay's contact; C3 sees it).

| Beat | Emitter | Grit | Crumbs | Chips | Haze | Speed and direction | Mix (count) |
|---|---|---|---|---|---|---|---|
| every blow | **Head sift**: the dust ledge on the leaf top (Z 0.05 to 0.95, Y 2.098), shaken off. 65 % falls to the burst side, because the leaf rocks toward the player | 260 × 0.6^k, where k = blows struck before this one (the ledge empties): 260 / 156 / 94 / 56 / 34. Phone 25 % | none | none | 3 (E), 2 after (phone 1) | v0 0.05 to 0.3 m/s outward, then falling as a **curtain**. Haze sheets 1:3 vertical, trailing the curtain | settled dust 70 % (grey-brown palette), wood 15 %, gypsum from the head joint 15 % (0 % before D1) |
| E (0.5 s) | head sift only | 260 / 65 | 0 | 0 | 3 / 1 | as above | |
| M (1.0 s) | **Split breath**: ±150 mm of the motif around the bore | +300 / 75 | 24 / 8 | 10 / 3 (fine slivers) | +4 / 2 jets | 0.5 to 1.5 m/s in a 35° fan about +X (air pumped through the crack). Jet wisps 1:2 | core 70 %, stile 20 %, walnut 10 %, glue flecks 0.5 % |
| D1 (1.5 s) | **Split through**: Y 0.78 to 1.24 | +700 / 175 | 60 / 20 | 30 / 10 | +6 / 3 | 0.8 to 2.0 m/s fan; 15 % fall straight out of the split (v0 under 0.2) | as M |
| D2 (2.0 s) | **Slot open**: Y 0.62 to 1.42 | +600 / 150 burst | 120 / 40 | 30 / 10 | +8 / 3 | 1 to 2.5 m/s | as M; + petal underside chipboard 10 % |
| D2 | **Slot trickle** (the sand pour) | +300 / 75 over 1.0 s | (in the 120) | | | rate r(t) = r0·e^(−t/0.35), cut at 1.0 s; leaves the slot's lower half at v0 under 0.15 m/s and falls as **a stream 15 to 40 mm wide** | core 80 %, stile 20 % |
| FINAL (2.4 s) | **Notch burst**: the 90 × 660 mm notch outline | +1,800 / 450 | 300 / 100 | 120 / 40 | 14 / 6 | 1.5 to 4 m/s along the strip's path, plus the leaf's velocity at the root | core 55 %, stile 30 %, walnut 10 %, frame or jamb 5 % (wood frame: the frame palette; steel frame: gypsum) |
| FINAL | **Notch trickle** | +500 / 125 over 1.2 s | (in the 300) | | | as the slot trickle, τ 0.35 s, cut at 1.2 s; from the notch's torn core face | core 85 %, stile 15 % |
| BREAK (2.5 s) | **Top pivot tear** at the hinge-side head (Z 0.0 to 0.15, Y 2.1): screws and the pivot arm rip out of the head, pose (a) | +500 / 125 | 40 / 13 | 0 | 6 / 2 | a downward curtain 0.2 to 1 m/s, + the gust | gypsum 45 %, frame wood 25 %, settled dust 30 % |
| BREAK | **Edge shake** when the leaf hits its stop: the latch edge and the top edge | +300 / 75 | 0 | 0 | 2 / 1 | 0.1 to 0.5 m/s off the edges | as the head sift |
| **Totals T1** | | **≈ 5,900 / 1,475** | **544 / 181** | **190 / 63** | **≈ 45 / 19** | live caps: grit **6,144 / 1,536**, crumbs 600 / 200, chips 600 / 200, haze 64 / 24 | |

- **Overflow:** when a cap is reached, the oldest grains of the finest class are dropped first, because they are the least visible. The order is deterministic.
- **Steel leaf (P3; no wood):**
  - head sift as above;
  - M and D1: enamel flakes, 40 and 80 (1 to 6 mm flat flakes, flutter, a specular glint: the door paint palette plus a primer speck at 15 %);
  - D2 seam: kraft bits, 60 (brown paper fibres, 1 to 4 mm), with grit 200;
  - FINAL: gypsum crumbs from the frame anchors and silencer bits, grit 800, crumbs 60;
  - haze at 40 % of the wood counts: steel makes little dust.
- **The leak card coupling.** Each blow's leak pulse (0.12 s) also pulses the slot or notch shaft intensity (§4.6), so dust in the shaft flashes on the blow frame. This is the same rule as `50` §3.8.

### 4.4 Palettes
- A build step (in the P1 generator; numpy, no Unity) samples **8 linear colours per material** from the albedo maps:
  - `Door_CoreWood_A` (core);
  - `Door_CoreStile_A` (stile);
  - `DoorVeneer_A` (walnut; whatever veneer the slot holds);
  - `DoorFrame_A` / `DoorFrameU_A` (frame wood, the slot the frame holds);
  - `Door_Enamel`'s albedo with a primer entry (steel; `PaintedMetal_A` for painted frames);
  - fixed entries for gypsum (sRGB 225, 222, 212 to 205, 203, 196), settled dust (sRGB 118, 110, 99 to 150, 140, 126) and glue (sRGB 120, 78, 30).
- Fines are lighter than the solid (more scattering surfaces): grit at 0.5 mm or less gets × 1.15 on its value.
- The palettes are written into the family sidecar (`palettes`), so the runtime needs no texture reads.

### 4.5 Settle and persistence
- **At the door's settle** (the bake's settle frame, `51` G5: 1.07 s after FINAL), the resting crumbs and chips merge into P1's floor mesh in slices of 1 ms or less. This is the GD3c lesson: one whole-frame merge cost 21.9 ms (`FrontRoomsGlassDebrisWorld.cs` L34).
- **The grit is never kept as geometry.** It lives on as the **dust skin's density texture**, which keeps updating while the fine sift lands (up to 6 s).
- **Restore** (`DressDoor`, ≥ 48 m away, never seen):
  - The **dust skin** is rebuilt as the sum of **beat patches** (E, M, D1, D2: one 64 × 56 R8 patch per motif seed, 3 KB each) and the **break patch** of the throw variant (128 × 112 R8, 12 KB). The patches are baked offline by an editor tool that runs the same C# integrator. It is mirrored by the root's X scale.
  - **Crumbs and chips** come back as GD3c-style **clusters** (centre, extent, count and seed, 12 B each, ≤ 12 per variant) baked into the throw JSON, and are re-scattered from the seed ("their exact places cannot be seen", `FrontRoomsGlassDebrisPack.cs` L16).
  - **Nothing new goes in the 16-byte record** (`50` §3.9). The stage, the motif seed, the throw variant and the burst sign choose all of it.
- **Kept damage (M2).** When the Relay resumes, beats already shown never re-emit their split or notch dust. The head sift continues its emptying ledge (k = total blows struck).

### 4.6 The light-shaft term (haze and grit)
Uniforms per damaged door, set by `FrontRoomsDoorBreakVisual` (which already computes the leak card):
- `_FR_LeakRect`: the opening's centre, U and V half-axes and normal in world space. It is the split or slot strip from the stage's `leak_card` anchor, or the notch at FINAL.
- `_FR_LeakLamp`: the far lamp's world position, and colour × `FarLampLevel` × max(stage curve 0 / 0 / 0.15 / 0.40 / 1.0, blow pulse).

For a sample P on the burst side:
- Q = where the ray from the lamp to P crosses the leaf plane;
- `mask` = a soft rectangle test of Q (4 mm soft edge);
- in-scatter += albedo × LeakLamp × mask × HG(g, cos(light direction, view direction)) / |P − lamp|², at about 20 ALU.

This gives a thin bright **sheet of lit dust** in front of the D2 slot, and a lit fan through the notch at FINAL, exactly where the real far light passes. It needs no shadow-map resolution at 15 mm. After the break the opening is the doorway, and the far room's real, shadowed lights take over, plus the desktop breach spot (`50` §3.9). Phones keep the analytic term (cheap) without shadows.

### 4.7 Determinism (with the baked throws)
- **Every random value** is `FrontRoomsGlassFractureMeshes.Rand01(seed, salt, i, j)` (the GD3c keyed hash), with seed = hash(edge, motif seed, throw variant), salt = emitter id, i = beat, j = particle index. The prototype's sequential `System.Random` goes.
- **The phone tier draws indices 0 to N−1** of the same lists, so it is a strict subset of desktop.
- **Emission time** = the blow time snapped to the 1/120 s grid, counted from the break start the map gives. The result does not depend on the render frame rate.
- The integrator, collisions and gust are pure functions of state and the throw curve. The offline patch baker runs the same code.
- **The harness check:** two runs at different frame rates give the same grain rest-hash histogram (§4.11 gate DG4).

### 4.8 Budgets per damaged door (desktop is the reference; one door breaks at a time)

| Item | Desktop hero | Phones (handheld tier) |
|---|---|---|
| Draws while live | ≤ 7 (grit 1, crumbs 3, chips 1, haze 1, skin 1) | ≤ 7 (the same paths) |
| Draws at rest | +1 (skin) beyond P1's floor merge | +1 |
| Grit instances | ≤ 6,144 live × 6 procedural vertices = 36.9k vertices | ≤ 1,536 = 9.2k vertices |
| Crumbs and chips | ≤ 600 + 600 live, ≤ 14 triangles each | ≤ 200 + 200 |
| Haze | ≤ 64 wisps. Fill ≤ 0.15 screen at C1 (3 m), ≤ 1.0 screen worst (C2 at 1 m at FINAL), at 1080p | ≤ 24; ≤ 0.35 screen at native resolution |
| Grit fill | ≤ 0.12 screen (thin streaks, ≤ 24 × 3 px) | ≤ 0.05 |
| Pixel cost | haze: ~60 ALU, 1 to 2 fetches, ≤ 4 shadowed lights per pixel; grit: vertex lighting only | haze per vertex; ≤ 25 ALU per pixel |
| CPU | main ≤ 0.2 ms (emit, SetData, mesh upload); jobs ≤ 0.3 ms at the FINAL peak | main ≤ 0.12 ms; jobs ≤ 0.15 ms |
| GPU (ESTIMATE, M1-class at 1080p) | ≤ 0.35 ms worst, ≤ 0.1 ms typical | ≤ 0.25 ms worst on A15-class |
| Memory | shared: grit buffer 192 KB, haze buffer 2 KB, wisp atlas ≈ 0.35 MB with mips, grain atlas 2 KB, patches ≤ 60 KB per family; per door: skin 12 KB | wisp atlas 512 × 256 (≈ 0.09 MB); grit buffer 48 KB |
| Record | +0 B (16 B as in `50`) | +0 B |
| Ray tracing (G14) | none: procedural draws are not in the acceleration structure; crumb meshes `RayTracingMode.Off` | none |
| WebGL | not dressed (`DressOnWebGL = false`), unchanged. If doors are ever dressed there, WebGL2 has no StructuredBuffer in the vertex stage, so it would need a merged-mesh grit path at phone counts (not in scope) | |

`50` §3.9's VFX row ("30 per stage, 120 at FINAL, dust 16 per blow"; phones "10 / 40 / 4") is superseded by this table and §4.3.

### 4.9 Tiers and settings
- **Phones:** gated by `FrontRoomsHandheld.Active` with the GD3c profile switch.
  - Counts as §4.3.
  - Haze per vertex without shadows.
  - `GritVisualScale` 1.3 → 2.0.
  - No breach light.
  - Desktop and Editor output stays byte-identical.
- **Desktop settings presets.** LOW power, or the BATTERY preset if it ever reaches desktop, may take the phone counts. HIGH and above keep the hero counts. The MSAA setting picks A2C or the coverage keep at runtime (§4.1). Nothing else changes per preset.

### 4.10 Files (in the clone; packaged later)
- **New:**
  - `Assets/Resources/Rendering/FrontRoomsDoorGrit.shader`, `FrontRoomsDoorHaze.shader`, `FrontRoomsDoorDustSkin.shader`;
  - `Tools/door_break/make_dust_textures.py`: the wisp atlas and the grain atlas, procedural numpy. It writes `Assets/Resources/Surfaces/Textures/Door_DustWisp.png` and `Door_Grain.png`;
  - the palette sampler, inside `interact_door_break_common.py`'s sidecar writer;
  - `Assets/Editor/DoorBreak/FrontRoomsDoorBreakDustBake.cs`: the beat and break patches and the crumb clusters, written into the throw and sidecar JSON. Editor only, ASCII, no tuples.
- **Changed:**
  - `FrontRoomsDoorBreakVfx.cs`: rewritten around a Grit / Crumb / Chip / Haze / Skin split, with the job sim and the keyed hash;
  - `FrontRoomsDoorBreakVisual.cs`: emitter calls per §4.3, the shaft uniforms, the gust start, the skin restore;
  - `FrontRoomsDoorBreakFilm.cs`: VFX stats.
- **Delete from the prototype** (clone line numbers as read today; move the old copy to `W/_partial/door_break_proto_dust_20261030/` before the rewrite):
  - `FrontRoomsDoorBreakVfx.cs`:
    - L22 `MaxDust`;
    - L33-37 `struct D`;
    - L40 `dust` list;
    - L44-45 `dustMat`, `dustTex`;
    - L68-71 the dust part of the constructor;
    - L92-111 `DustTexture` (the radial blob);
    - L113-129 `DustMaterial` (Simple Lit, beige 0.78 / 0.74 / 0.66);
    - L174-190 `HeadDust`;
    - L192-205 `BreakDust`;
    - L244-255 the dust step (drag 1.6, gravity −0.06, growth +0.05, y clamp 0.05);
    - L301-324 `BuildDust` (crossed cards);
    - L342-344 its disposal;
    - L41 / L58 / L131-133 the sequential `System.Random`;
    - L260-264 the tetra `ChipShape` and L265-269 the octahedron `CrumbShape`, replaced by §4.1's shapes.
  - `FrontRoomsDoorBreakVisual.cs`: L314 `vfx.HeadDust(16)` and L357 `vfx.BreakDust(40)`. L300 `StageChips` and L315 `BoreCrumbs` become §4.3's emitters.

### 4.11 Gates for the LAND stage's one test round (rule 23; earlier stages only compile-check and smoke)

| Gate | Check | Pass |
|---|---|---|
| DG1 not fog | In C1's lock box and the doorway box at E, D1, FINAL + 0.3 s and BREAK + 1.0 s: the haze-only pass's mean alpha, and its 99th percentile | mean ≤ 0.08, p99 ≤ 0.30. No haze pixel brighter than the brightest lit wall pixel in the frame unless it lies in the shaft mask |
| DG2 grains | Grit emitted per beat equals §4.3 (desktop and phone). Peak live ≤ cap. The colour histogram of the grit matches the mix ±3 % | exact counts |
| DG3 read | C1 ROI changed pixels between D2 and D2 + 0.5 s (the trickle) and between FINAL and FINAL + 0.5 s | ≥ 400 px each (the `50` §4.7 G4 threshold) |
| DG4 determinism | Runs at 60 and 30 fps give the same rest-position histogram (12.5 mm bins) and the same skin texture to within 1 LSB. The phone set is a prefix of the desktop set | identical |
| DG5 cost | The stats' peak main and job ms, haze fill (projected area sum) and draws | within §4.8 |
| DG6 gone | No haze alive at BREAK + 3.0 s | 0 |

The film adds one contact-sheet row: D2 + 0.4 s and FINAL + 0.5 s from C1 and C2, and BREAK + 1.0 s from C1. It is still one contact sheet, numbers first (`50` §4.6).

### 4.12 Asks and open choices
- **Map (关卡设计): none.** The throw curve and the blow times are already in M1 and the bake.
- **Sound (声音设计): optional S6**, written into the handoff only, no message. A soft "sift / pour" layer under `Door/Blow` from D2, following `DoorStageChanged`, ≤ −24 dB under the blow. The game stays silent until sound decides; rule 24 holds.
- **For Red** (shown in the next video; defaults are in brackets):
  - (a) `GritVisualScale` at 3 m [1.5; 1.0 = purely physical];
  - (b) an optional **ceiling sift**: the lay-in tile above the door sheds mineral-fibre dust on the heaviest blows (D2, FINAL) [off: it is scenery the door does not own];
  - (c) six-way lit flipbooks for the FINAL puff, baked in Blender [not now: the thin haze does not need them].

### 4.13 Estimate
About 1.5 working days inside P1 and P5:
- shaders and atlases: 0.5;
- the sim rewrite with jobs and the hash: 0.5;
- the skin, the patch bake and the restore: 0.5.

The film and gates are part of the existing video and LAND stages.

---

## 5. What the next stage must know
- The clone was re-synced from main at the start of this stage (see this stage's entry in `52_production.md`). Main now also has the double-door hardware (kick and push plates, `FrontRoomsDoubleDoorKit.cs`). Re-sync again at your own start.
- This stage built nothing in the clone and ran no Unity.
- Read §1 before touching `FrontRoomsDoorBreakVfx.cs`: the old dust code goes as a whole.
- Grit needs `GraphicsBuffer` + `Graphics.RenderPrimitives`. No current main script uses them (`FrontRoomsWallFixtureRenderer` uses `RenderMeshInstanced`), so compile-check on both mac and iOS defines early.
- `FrontRoomsSurface` has no vertex colour. Keep crumbs on it with random UV offsets (§4.1). Do not add a vertex-colour path to the shared shader.

---

## 6. Sources (this file)

**Footage, viewed:** [DG-F1] to [DG-F7] are in §2.1 with their URLs and times.

**Games, engines and rendering:**
- [DG-G1] D. Veselka, MP1st, 2012-05-24, quoting Nadab Göksu (senior VFX artist, DICE) on Battlefield 3: Close Quarters "HD Destruction": https://mp1st.com/?p=18300 [read]
- [DG-G2] T. Yasuda (lead VFX artist, Bayonetta 3), "Umbran Studies: VFX Vol.2 Background Effects", PlatinumGames blog, 2023-12-25: https://platinumgames.com/official-blog/article/13708 [read]
- [DG-G3] T. Sousa, J. Geffroy, "The Devil is in the Details: idTech 666", SIGGRAPH 2016 Advances in Real-Time Rendering (decoupled particle lighting, particle light atlas): https://advances.realtimerendering.com/s2016/Siggraph2016_idTech6.pdf [search]
- [DG-G4] S. Hillaire, "Physically Based and Unified Volumetric Rendering in Frostbite", SIGGRAPH 2015 (particles voxelised into an extinction volume; phase g per froxel): https://www.slideshare.net/DICEStudio/physically-based-and-unified-volumetric-rendering-in-frostbite [search]
- [DG-G5] Unity Manual 6000.6, URP Particles Lit shader reference (Soft Particles with Surface Fade near and far; Camera Fading; both need the Depth Texture): https://docs.unity.com/en-us/engine/current/manual/materials-and-shaders/built-in/shaders-in-universalrp/reference/particles-lit-shader [read]
- [DG-G6] Unity blog, "Realistic smoke lighting with 6-way lighting in VFX Graph" (January 2023): https://unity.com/blog/engine-platform/realistic-smoke-with-6-way-lighting-in-vfx-graph ; VFX Graph docs: https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.0/manual/six-way-lighting.html [search]
- [DG-G7] Humus' (Emil Persson's) Phone-Wire AA, reported by Geeks3D 2012-06-26 ("clamps the width to a pixel and instead fades with an alpha value"): https://geeks3d.com/?p=8334 [read]. E. Persson, "Wire Antialiasing", GPU Pro 5, pp. 211-218: https://gameenginegems.com/gemsdb/article.php?id=1223 [search]
- [DG-G8] Remedy, J. Richter, "Destructible Environments in Control: Lessons in Procedural Destruction", GDC 2020 video: https://www.youtube.com/watch?v=kODJsQGXanU (30:10; the tiers in [M-CT2]; timestamps not checked)
- [DG-G9] J. L'Heureux, "The Art of Destruction in Rainbow Six: Siege", GDC 2016 video: https://www.youtube.com/watch?v=SjkQxowsL0I (47:53; [M-R6]; timestamps not checked)

**Materials:**
- [DG-M1] Natural Resources Canada, "Particle board" (multi-layer: finer particles in the surface layers, coarser in the core): https://natural-resources.canada.ca/forest-forestry/forest-industry-trade/particle-board [search]
- [DG-M2] Wood and Fiber Science, "Improving Core Bond Strength of Particleboard Through Particle Size Redistribution" (core furnish rich in fines; 20 % fines swapped for coarse raised IB 40 %): https://wfs.swst.org/index.php/wfs/article/view/752 [search]
- [DG-M3] US 8398905 B2, "Particle board" (intermediate layer about 660 to 700 kg/m³): https://patents.google.com/patent/US8398905 [search]
- [T-PB] Wang et al. 2023 (three-layer board; see `SOURCES.md`)

**Physics:**
- [DG-P1] T. Peters, Aerosol formulae, Univ. of Iowa IH course (settling ≈ 0.003 d² cm/s, unit density, 1 to 100 µm): https://myweb.uiowa.edu/tpeters/IH1/Aerosols/AerosolFormulae.pdf [search]
- [DG-P2] Henyey-Greenstein g for aerosols (0.2 to 0.7), haze (0.7 to 0.8): arXiv 1311.5338, https://arxiv.org/pdf/1311.5338 [search]. Mineral dust g above 0.65: Atmos. Chem. Phys. 18, 17735 (2018), https://acp.copernicus.org/articles/18/17735/2018/acp-18-17735-2018.html [search]
- [DG-P3] "A comparative study of walking-induced dust resuspension using a consistent test mechanism" (carpet vs hard floors): https://healthyhousingsolutions.com/clearinghouse/a-comparative-study-of-walking-induced-dust-resuspension-using-a-consistent-test-mechanism [search]

**Buildings:**
- [DG-B1] CGC (USG Canada) Construction Handbook ch. 12, "Problems, remedies and preventive measures" (cracking at door frames): https://www.usg.com/content/dam/USG_Marketing_Communications/canada/product_promotional_materials/finished_assets/cgc-construction-handbook-ch12-problems-remedies-and-preventive-measures-can-en.pdf [search, not opened]

**Our own:**
- prototype frames `W/door_break/DB2/out/full/S1_C1_000752.jpg`, `S3_C2_000681.jpg`, `S3_C2_000748.jpg`, `S3_C2_000768.jpg`;
- texture means measured with ffmpeg (§2.2);
- `FrontRooms_URP.asset`, `FrontRooms_URP_Renderer.asset`, `FrontRoomsQuality.cs` L225-226, `FrontRoomsGlassVfx.cs` L1-60, `FrontRoomsGlassDebrisPack.cs` L1-17, `FrontRoomsGlassDebrisWorld.cs` L24-36 (main, read-only).
