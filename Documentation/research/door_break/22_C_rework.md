# 22 — Direction C rework, round 1: a real 1990 stud wall breaks round the door (DB1)

Status: **REWORK ROUND 1, 2026-10-14.** Answers Red's note of 2026-10-08 on `images/db_C_L0F_burst_S4_V1_LA.jpg` ("improve this broken structure: right now it does not look real enough"). Direction C's idea stays: the giant crushes the doorway, the wall gives, the break reads at 12 m. Its **break structure** was rebuilt from scratch so it is physically right for 1990 US interior construction, following `21_C_realism_research.md` (= **[21]**, its §6 rules and §10 checklist are the spec) and `21_C_diagnosis.md` (= **[DX]**, fixes F1–F15).

Nothing in the game, the kit, the map or Red's project code was changed. This step wrote only this file, the `images/db_C2_*` frames and sheets (the old `db_C_*` / `dir_C_*` stay for comparison), the code copies in `code_C2/` and one verification-log row (§7). No media was downloaded. Work folder: `$W/door_break/C/rework/` (`$W` = `/Users/redwang/FrontRoomsVisualWork`).

__RESULTS__

---

## 1. What was wrong, in one line each, and what replaced it

| Red / visual chat | Old cause ([DX]) | C2 (this round) |
|---|---|---|
| Thick dark branching "tree" cracks | A glass crack graph (forks p = 0.3, radials + rings, 70 cracks) drawn as **near-black albedo** 4.75–9 mm wide (F1, F2) | **No crack darkens albedo anywhere.** The break is **grid-driven**: it follows the stud edges, the 1.68 m sheet joint, the header's screw row and the top plate. Hairlines (D1) exist only as a 0.3–0.5 mm **crease** in the vinyl's height map. Darkness comes only from real gaps and real cavities |
| Holes are flat black blobs | Round-topped cell holes, a 0.02-albedo plenum box catching half the rays, square 12.7 mm rims, painted torn paper as the rim (F3, F4) | **Knocked-out bays**: straight at the studs (368 mm clear), ragged across, a white crumbled **bevel** of gypsum on every edge with the paper overhanging it, real **studs, kings, header, top plate and screws** inside, the far board's **grey back paper** lit by the room, light through only where both faces are open |
| Torn wallpaper reads as camouflage cloth | A two-tone noise, 57 % kraft brown, the "face paper" as dark as the print, sticker outlines, zero-thickness leaf flaps (F5–F7) | **Layers in physical order** with crisp, fibrous boundaries: the white paper **backing** dominates, then cream face paper (or white joint compound / cream tape where they are), grey-brown plies only as small streaks; the strips **lift at their seams and curl** with the backing out; 0.35 mm thick sheets with a white torn edge |
| Floor debris reads as paper flakes | Chunks with camouflage faces and stepped rims 0.64 m out, pebble crumbs, zig-zag "lightning" strips, powder 0.45 m out (F8, F9) | Debris **cut from the same wall**: 12.7 mm plates and chips with the room's chevron on their face, grey back paper behind, white crumbled edges; power-law sizes; most mass within 0.3 m of the base; gypsum grains 2–15 mm; a **powder fan from the base** with a ridge on the base line |
| Little dust; clean edges; weak scale | Room haze off in L-A, no dust at S4, grit as 35 mm "rain" (F10, F12) | Staged dust (grit on every blow, puffs out of the corners and bays, a burst at FINAL, a veil still hanging at S4, haze on from D2); grit as 1.5–6 mm grains; **scale cues of known size** everywhere (406 mm studs, 52 mm tape, 8 mm screw heads, 12.7 mm board, 0.75 m paper strips) |

---

## 2. The wall that is built now

### 2.1 Build-up (default (a) of [21] §1.2; RED decisions unchanged)

| Part | C2 | Source |
|---|---|---|
| Board | 1/2 in gypsum board, **12.7 mm**, both faces; face paper 0.4 mm (wallpaper + manila), back paper 0.3 mm | [21] §1.1, checklist 1 |
| Studs | **true 2×6, 38.1 × 139.7 mm**, kiln-dried pine (`Prop_Studs`), at **406.4 mm** centres from the king studs outward; clear bays **368.3 mm**. The map wall is 0.16 m, so the boards' backs are 134.6 mm apart and the studs run 2.55 mm into each board's back (hidden, not shortened) | checklist 3; [DX] F15 (the old 38 × 135 "stud") |
| Opening | rough opening Z −0.025…1.025 (1.0 clear + 19 mm jamb + 6 mm shim), head 2.13; **jack + king each side (76 mm)**; the board notched round it (layout L1, by the book), its cut edge under the casing | [21] §1.2–1.3 |
| Header | **built-up 2×8 on edge**: three 2×8 with two 1/2 in plywood spacers = 139.4 mm, Y 2.13–2.314 (the 2×6-wall form of the double header; the plies show as 2.5 mm light/dark stripes) | [21] §1.2, checklist 4 |
| Cripples, plates | cripples on the same 406 mm layout above the header; **double top plate** 2.824–2.90; bottom plate cut out across the opening | checklist 4 |
| Sheets and joints | hung horizontally; the **tapered joint at Y 1.68** on both faces: 52.4 mm paper tape, a 270 mm compound band (under the paper; it shows where the paper tears); the back paper's **cream wrap** 28 mm each side of the joint | [21] §1.3, §1.5; checklist 5, 11, 12 |
| Fasteners | screws on every king, stud and cripple at 406 mm (1.695, 2.101, 2.507, 2.885 above the joint), the header at 305 mm (Y 2.222); **35 mm compound spots** over each head (under the paper) | [21] §1.4; checklist 6 |
| Wallcovering | the game's chevron print as **paper-backed vinyl** (RED default), **seams every 0.75 m** (Z = −0.75, 0, 0.75, 1.5); the P board **raw above the 2.4 m tiles** (cream face paper with compound spots) | [21] §1.7, §1.1 |
| Plenum (P) | an open-bottom box at ~0.25 albedo with hanger wires, lit by the P room through the lifted / missing tile (the old one was a closed 0.02 box) | [DX] F4(a) |

### 2.2 Every broken edge has its layers

A broken edge is built as seven rings from the front paper to the back paper, with per-vertex insets measured into the material and noise along the edge ([21] rule 5, checklist 1, 9–11):

| Edge type | Paper edge vs break | Core at the front | Core at the back | Back paper | Look |
|---|---|---|---|---|---|
| D2 crack (plates still on the wall) | 0–0.6 mm | 0.3–1.8 mm | 0.1–1.2 mm | 0–0.5 mm | a 1–3 mm slit with a lit lip, paper torn white along it where it stepped |
| Hole edge on the blown-out face (S, FINAL/S4) | the paper **overhangs 2–12 mm** | **4–14.5 mm back** (a 30–55° crumbled bevel, the exit-side cone) | 0–3.5 mm | 0–4 mm past the core | a **white gypsum band 5–30 mm wide in projection** round every hole, the paper fringe over it |
| Plate / chunk that broke out | fringe 1–10 mm | 1.5–8 mm | 0–3 mm | 0–3.5 mm | white crumbled rims on every chunk |
| Struck face (P) | 0–7 mm | 1–6 mm | 2–10 mm (crushed) | — | crushed core, paper torn round it |
| Sheet joint (factory long edge) | 0.2 mm, no core | — | — | — | paper-wrapped, cream: the hole bottoms on the joint stay straight |

The mid ring carries an extra 0.5–2 mm crumble; the gypsum shader is #DDD8CC–#F0EDE6 with 0.5–2 mm voids, roughness 0.96 and **no ambient-occlusion darkening** ([DX] F4(d)).

### 2.3 The wallpaper layer (map, 1 mm texels over Z −0.80…1.90, Y 1.05…2.90)

| Depth | Layer | sRGB | Where |
|---|---|---|---|
| 0 | the print (vinyl), roughness from the game's `_S` | the game texture | everywhere intact |
| 1 | the white paper **backing** / torn fibre edge | #E2DDCE–#ECE8DB | the dominant colour of every tear; a 1 mm fibre edge along every break |
| 2 | the under-layer: **cream face paper** #D8CFB8, or **sizing** #E9E6DD with paste sheen (seam gaps), **joint compound** #ECEBE6 (the 270 mm band, the screw spots), **paper tape** #E3DAC6 | | where the whole wallcovering came away (~34 % of a torn zone) |
| 3 | **grey-brown filler plies** #988A72–#ACA088 | | where the board's top ply came off with the paper: small streaks along the peel direction, ≤ 11 % of a torn zone, only on face paper |
| 4 | gypsum #E0DBCF–#EEEBE3 | | pull-through craters |

Plus powder (a soft film below the breaks, lighter round the hole edges, no rain lines) and a height channel (creases 0.3–0.5 mm, pop domes 0.5–0.9 mm, tape bubbles 1.4–2.2 mm, crushed folds on P, seam lips). Torn zones run along the breaks in **straight-ish runs of 20–120 mm with corners**, 0–40 mm wide, with gaps where the paper held ([DX] F6).

---

## 3. Stage by stage (cumulative: every stage contains the one before)

Load paths as [21] §2.1: the frame's rack stays a 0.48 % drift (hairlines only); the **holes come from the out-of-plane push**: the giant's elbows reach past both jambs (Giant B `press3` measured: hinge side Z −0.22…−0.09, Y 1.76–2.11, 15 mm off the P face; latch side Z 1.08–1.23, Y 1.87–2.18, **28 mm into** the P face at D2) and its back sits over the head.

| Stage | S face (player, blow-out side) | P face (Relay, struck side) | Frame / leaf |
|---|---|---|---|
| **S0** | intact | intact | kit |
| **M** | nothing on the wall (bulge 0.8 mm); grit grains and a pale sheet out of the head gap | tiles jump 4–16 mm | — |
| **D1** | 4 hairlines from the board's **notch corners under the casing** (48° out, 60° in, 0.1–0.3 m): a 0.3 mm crease, no line; **4 pops** (header row at Z 0.355 / 0.66, king L at 2.507, king R at 2.10); the seam at Z 0 ticks open 0.7 mm; dust puffs at both head corners | the P casing bears into the board: crushed fine folds 14 mm wide along its outer edge | rack 3 mm, head lift 2 mm (the mitre opens 1–2 mm), pry 1.5 mm |
| **D2** | the hairlines end on the king edges and run along the header's top edge; **three bays break into plates** along the stud edges, the joint, the header screw row and the top plate: **L1** (hinge side, Z −0.47…−0.10, 1.68–2.58, 4 plates), **R1** (latch side, Z 1.10…1.47, 1.68–2.44, 3 plates), **H2** (over the head between the cripples, Z 0.34…0.71, 2.22–2.82, 3 plates); plates **step 0.5–4.5 mm** and tilt 0.5–0.9° about their stud / joint edge; the bays **bulge** up to 12–14 mm (held at the studs); torn runs along the stepped edges; the **tape edges crack and bubble** over L1 and R1; seams open 2–2.8 mm; two seam strips **lift 60–70 mm and curl** (Z 0 over the left head corner, Z 1.5 right of R1); creases + short torn runs along the header's top edge and partway up the king and cripple edges beside H2 (the paper bridges them); powder film below the bays | **elbow dents**: hinge side 10 mm with wrinkled, unbroken paper; latch side the board breaks in round the elbow (2 plates pushed 26–30 mm into the cavity) | rack 8 mm, lift 5 mm; the casing pried 6 mm, the head mitre tip breaks off (4°); bow 30 mm |
| **FINAL** | whole bays let go: in L1, R1 and H2 the plates that fly (with their **fragments**, 48–59 pieces in the air, 0.10–0.30 m out and falling) and the plates that **swing on paper hinges** (L1c on the king edge 22°, R1b on stud 3's edge 26°, H2b on the top plate line 14°, H2c on cripple 1's edge 16°); a remnant of the top sheet stays on the joint at the bottom of L1 and R1 (the tape held it); **pull-through craters** on the rows that held; the screws stand in the exposed stud and header faces; the paper tears round the holes and hangs (a flap over L1, a fold below R1); the joint tape tears and lifts; the dust burst rolls out of the bays and down the wall; grains in the air | the P board breaks in at L1 and over the head (H2): **through-holes** in all three bays | pry 16 mm, nails visible in the gap |
| **S4** | knocked-out bays with framing inside, plates **hanging 33–54°** (L1c 38°, R1b 46°, H2b 54°, H2c 33°), the seam strips peeled **0.22–0.26 m** and curled 215–230° (backing out), the tape torn along the hole bottoms, powder and grains on the bottom ledges; on the floor: the fragments, grains, paper scraps, casing splinters, the powder fan and ridge; a dust veil still round the head | inward-torn bays, one plate hanging into the cavity per bay; chips and powder on the P floor | the head casing **pried 20 mm** at the latch end and **split along the grain** at its outer nail line (raw walnut #9C7A55 in the crack), finish nails in the gap; leaf open 80° |

**RIP** (L0-F, D2, from P): the frame is dragged toward S, so the P casing crushes into the P board, the board beside both kings and over the header **steps into the wall** 1.5–4 mm along the king edges and the header line (5 plates), paper tears along the stepped edges, three seams open, the Z 1.5 seam strip lifts; the latch stop splits as before.

**L0-K** (steel): the same wall events, the cuts shifted 12 mm (a different member, not a copy); the steel KD frame pinches the board along its returns (crushed folds) and opens 3–4.5 mm at the latch-side mitre, as before.

---

## 4. Debris, powder, dust, light

- **Fragments** ([21] §3.1): each plate that flies breaks into 1–2 big pieces plus chips (area kept: the chips are cut from the plate, so they carry its exact print). L0-F S4: 48 floor pieces, 8 of them ≥ 100 mm carrying 82 % of the mass, 40 chips 15–100 mm; L0-K: 59 pieces. Face up / down 50 : 50, chips tilted 0–14° on the pile, one big piece per side bay **leaning on the base** outside the opening, nothing on the open leaf's footprint.
- **Distance from the wall face** (mass): L0-F **67 % within 0.3 m**, 83 % within 0.5 m (median 0.35 m, p90 0.74 m); L0-K 64 % within 0.3 m after a deterministic re-place (the first draw gave 43 %). Old: median 0.64 m.
- **Grains**: 627 per S4 frame (gypsum, angular, flattened), Pareto sizes p10 / p50 / p90 = 2.2 / 3.8 / 13–15 mm, **75 % within 0.15 m** of the base; 40 more on each hole's bottom ledge; core lumps 8–35 mm.
- **Paper scraps** (22, 12–70 mm, curled, print side up 60 %) and **casing splinters** (6, 30–140 mm, raw walnut) on the S floor.
- **Powder**: a floor decal at 5 mm from the wall face out 1.6 m: a fan from the base under each bay (densest at 0–0.1 m, thinning by 0.4–0.9 m, faintly radial), a 3–5 mm **ridge** on the base line, powder on the cove base's top, the S head casing's top, the saddle and the open leaf's top edge (S4). On the wall a soft film below the breaks.
- **Dust**: room haze on in L-A from D2 on; grit grains 1.5–6 mm on every blow (M–FINAL); puffs at the head corners (D1, density 8), at the bays (D2, 7); the FINAL burst (10 puffs, 0.26 m); at S4 a veil round the head and the doorway (0.32) instead of nothing.
- **Light**: geometry only. A crack in one face opens into a closed cavity; the old "white specks" are gone. Light passes where **both faces are open in the same bay** (L1, R1 and H2 at FINAL/S4; the latch-side elbow bay at D2 through the 1–3 mm cracks only).
- **Ceiling tile** ([DX] F13): the tile over the door breaks on a ragged line; painted face down, **unpainted grey back** (#8C8579) and fibrous **core** (#A39783) on every edge; the S4 halves warp 4 mm.

## 5. Casing, nails, corner bead, leaf

- **Casing** ([21] §2.8): the mitre opens with the head lift (D1 1–2 mm), its short-grain tip breaks off at D2 (4°, 7° from FINAL), the head casing pries 6 / 16 / 20 mm (D2 / FINAL / S4) at the latch end, **finish nails (1.8 mm, galvanized #8D8F8C) bridge the gap** at the header and jamb nail lines (some still in the wall, some pulled out and standing out of the casing), and at S4 the casing **splits along the grain** at its outer nail line from mid-span to the latch end (3–4 mm open at the end, raw wood in the split). It never drops below the head (Y ≥ 2.098).
- **Corner bead** ([21] §1.6, §2.9): a cased wood door has **no outside corner** within reach, so C's set has no bead (adding one would be wrong for this door). It is shown as a **dress-option study** at a papered wall end (the Level 0 photo's wall ends): galvanized bead, 32 mm perforated flanges, round nose, fasteners at 229 mm, the nose dented about 9 mm with a kink, compound cracked off in slabs exposing the flange and its fasteners: `images/db_C2_bead_study_S4_LA.jpg`. ESTIMATE / study only.
- **Leaf** ([DX] F14): the steel leaf's **star crack is gone** (no radials, no rings): only the dish, paint cracks in short arcs along the tight bend at the dish rim and flakes to grey primer there. The wood leaf: the struck face's crazing is **pale lifted lacquer** (not dark ink) plus a few short grain splits at the print rim; the far face shows 4–6 short vertical hairlines near the latch stile instead of the "hairy oval".

---

## 6. Frames

All in `images/`, 1920 × 1080, Blender 4.3 Cycles (Metal), 256 spp, OIDN, AgX, exposure −0.9 EV (the C calibration, unchanged), the same set, lights, cameras and Giant B `press3` as the C pre-render. Names `db_C2_<member>_<case>_<stage>_<view>_<light>.jpg`.

__FRAMES__

---

## 7. Re-check against the diagnosis targets ([DX] §5) and the hard rules

__CHECKS__

---

## 8. Open items, RED decisions, UNVERIFIED

1. **Red agrees the §8.3 change.** `10_design.md` §8.3 still asks for "the most literal glass read" and a crack graph "like the glass's". C2 replaces it with the grid-driven drywall break. The §8.3 build notes are not edited until Red agrees ([DX] §0).
2. **Wall build (RED, [21] §1.2):** default (a) 2×6 + 1/2 in board kept. Option (b), 2×4 with two layers of 5/8 in board per face, would show **two** board edges (31.8 mm of gypsum) at every break.
3. **Wallpaper type (RED, [21] §1.7):** paper-backed vinyl kept (white backing at every tear). A Type II fabric-backed vinyl would show a woven #D9CFB9 back instead.
4. **No photo of broken drywall is on disk.** The break's shape, the hinges, the debris spread and several widths are ESTIMATES from [21] (mechanics, tests, repair and breach practice). The reference sheet compares C2 only with the intact build on disk. The two most useful candidates, `dv04` (a real body through a stud wall, 31 s) and `dw15` (CUREE W-15 damage photos), wait for Red's approval in `media_candidates.md` §21; with them the bay shape, the hinge angles and the debris spread can be checked.
5. **D2 at 12 m is honest but quieter than before.** The old D2 read at 12 m through 9 mm black lines that were not physical. C2's D2 reads at 12 m only through the dust puffs, the two lifted seam strips and the pale torn runs; FINAL and S4 read strongly (bays, hanging plates, peeled strips, the fan). If the game needs a louder D2 at distance, the honest lever is the sound and the leaf jolt ([21] §10.2), not lines.
6. The game build (the map contract, budgets, baked stage meshes) is still as `20_dir_C.md` §4; C2 is a concept rebuild. Its wall is a render-only vector mesh (§9 counts), far over the game budget, like the old one.
7. The corner-bead study is not part of C's door (see §5).

---

## 9. Reproduce, files

```
W=/Users/redwang/FrontRoomsVisualWork; RW=$W/door_break/C/rework
$W/door_break/C/py $RW/scripts/drywall.py L0F burst          # layout, staged events, maps, debris, leaf maps -> $RW/data/L0F_burst
$W/door_break/C/py $RW/scripts/drywall.py L0K burst
$W/door_break/C/py $RW/scripts/drywall.py L0F rip
$RW/scripts/final2.sh "L0F burst S4 V1:76:LA,V6:76:LA:human,..." ...   # Blender 4.3 + scene2.py -> renders/final, images/db_C2_*.jpg, models/DB_C2_*.blend
/Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup --python $RW/scripts/bead_study.py -- <out.png> 1920x1080 128
$W/door_break/C/py $RW/scripts/publish2.py strips before reference distance details
$W/door_break/C/py $RW/scripts/measure2.py $RW/renders/final L0F    # section 7 numbers -> $RW/look/measure_L0F.json
Blender -b --factory-startup $RW/models/DB_C2_L0F_burst_S4.blend --python $RW/scripts/probe2.py -- $RW/data/L0F_burst/stage_S_S4.json $RW/look/probe_L0F_S4.json
```

| File | What |
|---|---|
| `scripts/common.py` | the wall build (framing, joints, layout), stage parameters, the push field and the stud-held bulge |
| `scripts/geom2d.py` | ragged break lines, polygon splitting, offsets |
| `scripts/drywall.py` | the generator: staged events per face and case, band rows for every broken edge, the wallpaper maps, the floor powder, debris lists, the leaf maps |
| `scripts/scene2.py` | the Blender scene: the C pre-render's set / kit / leaf / lights / cameras / Relay (`scene.py`, unchanged parts copied), the new wall, framing, plenum, tiles, casing nails and split, debris, powder, dust |
| `scripts/bead_study.py` | the corner-bead dress-option study |
| `scripts/publish2.py`, `measure2.py`, `probe2.py`, `viz2d.py` | sheets, re-check numbers, ray probe, 2D map previews |

Copies of these scripts as `.py.txt` are in `code_C2/` next to this file (so they survive a scratch wipe). Stage models: `$RW/models/DB_C2_<member>_<case>_<stage>.blend`.
