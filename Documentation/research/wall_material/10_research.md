# Level 0 wall material: how AAA walls read as wallpaper, and what ours lacks

Red, 2026-10-30, in the wallpaper chat: "然后目前墙上应该有一层类似于墙纸的纹理，我需要你research一般3A游戏这部分都是怎么做的，然后优化当前我这个游戏墙壁的表现", then "就是在有墙纸的level里，比如说level 0".

Research: workflow `wf_c56b2d96-a47` (29 agents, read-only, nothing downloaded). Sources: `SOURCES.md`. The prototype and its results are in §6.

## 1. The answer

The wall reads as a print on flat plaster because everything that says "wallpaper" reaches only the albedo, and the print alone spans 3.2:1 in luminance. The relief that does reach the lights is about 0.1 mm high at wavelengths of 6 mm or less, so it is gone past about 2 m.

A real wall is told from paint by three things, in this order:
1. **Strips:** seams, and a small tone step from sheet to sheet.
2. **Sheen:** a sheen above matte shows every seam and every bump in the wall under the paper. A flat finish makes the wall read as one even surface.
3. **Relief at the scale of the wall,** not of the paper: hanging and the substrate, not the emboss. A Type II vinyl is only 0.41 mm thick in total.

So the work is not more detail in the print. The print stays exactly as it is, and the paper around it starts behaving like hung paper.

## 2. How high-end games build these walls

- **Layers, not one texture.** Studios ship 2–4 run-time layers blended by vertex colour or masks, with height-aware transitions. The Order: 1886 is 90–95 % tiling with up to 4 layers plus height maps and decal projectors. The Last of Us Part I uses RGB vertex-colour texture sets. Alan Wake 2 uses height data and painted masks. Layers exist to "add unique variation to tiled textures" (The Order, SIGGRAPH 2013 notes).
- **Decals and mesh cards for edges.** Projected decals mark where frames hung, tape, scuffs. Edge decals and alpha cards handle peels and lifted seams.
- **Close detail is normal + cavity.** Detail normals are RNM-blended. Micro-shadowing from cavity started with Uncharted 4; Call of Duty: WWII auto-generates cavity for every normal-mapped surface.
- **Relief for thin goods is a normal map.** POM is kept for real thickness (the Capitol gate in The Last of Us Part I, rivets and panel lines in Part II).
- **Wear has a cause.** Dishonored 2: walls must not "look like regular grids", and wear works through roughness and diffuse. The Vanishing of Ethan Carter studied where dirt and dust settle. The Order blends grime through cavity masks.
- **Filtering is part of the look.** Call of Duty: WWII ties gloss to normal length, so a filtered detail stays stable.

No studio source says how it draws a wallpaper seam. Faye Smith's UE4 interior adds seams and dirt in Mixer so a tiled paper looks like strips, which is the closest public example.

## 3. Real 1985–1993 office wallcovering

- **Roll widths:** commercial Type II vinyl is 54 in (1.37 m); Type I is 27 in (0.69 m); residential paper is 20.5 in or 28 in. Our 0.75 m tile is close to the 27 in and 28 in goods, and to the 30 in panels sold from the A24 film.
- **Seams:** vertical and plumb, never horizontal. Butt or double-cut, a hairline when new. They show through tone change, a dull line of adhesive left behind, or a burnished line.
- **Shading:** printing leaves a colour change from one edge of a sheet to the other, and lots differ. Plain and textured goods are reverse-hung to hide it; matched patterns are hung in sequence. It is most visible looking along a wall.
- **Ageing:** paper backing grows 1–2 % when wet, which peaks or opens the joints. A documented aged seam opened to 1/8 in (3.2 mm). Edges lift and curl first where it is damp.
- **The wall underneath:** drywall joints every 48 in telegraph through light goods as faint bands.
- **Sheen:** commercial vinyl is low sheen, about 6–10 gloss units at 60°. Manufacturers warn that grazing light magnifies texture and seams.

## 4. Genre references

- The canonical wall is probably beige paper made yellow by the camera: production designer Danny Vermette says "white balance is off". Much of the reference look is the camera and the light, not the albedo. Re-grading our print toward beige is a separate art-direction decision for the print and graphic lines, and is not part of this work.
- The A24 team tested wallpaper and carpet combinations so the pattern would not striate on camera at corridor distance, which is why every term below is filtered.
- Alan Wake 2 authored materials a bit wetter than normal, so that the player senses it. Sheen is felt more than seen.

## 5. Our wall, measured (audit of main)

| What | Measured | Why it matters |
|---|---|---|
| Lit relief | 5.2° RMS at mip 0, 0.5° at mip 3; mip 3 starts at 2 m | Past 2 m the wall shades as a flat plane |
| Smoothness | constant 0.213 ± 0.008 | No sheen anywhere, so nothing catches the lamps |
| Strips | all identical; paper, normal and mask repeat every 0.75 m | One continuous coat, one stamped tile |
| Baked seam | 1 mm wide, gone by 2–4 m, slightly lighter than the paper past 8 m | Wrong at both ends of the range |
| Print vs paper | print 3.2:1, paper modulation ±3.4 % | The eye reads the print only |
| Wear | albedo only; tone × dirt ±1.9 % over 48 m | Stains read as paint on the surface |
| Cavity | reaches indirect light only (URP) | Lamps never see a groove |
| Shift paper | same relief and sheen as the clean Lobby | Aged paper is only darker |

Toksvig-style specular anti-aliasing would add nothing: the averaged normal length stays at 0.994.

## 6. Plan, and what was built

Ranked by impact over cost. "Built" = in the prototype clone `W/proj_wallmat`.

| Rank | Item | Cost | State |
|---|---|---|---|
| 1 | Seam drawn in the shader, in metres, lit and distance-stable | ~30 ALU | **Built** |
| 2 | Strip tone: edgeshade, shade lots, per-wall age, per-strip gloss and emboss | ~15 ALU | **Built** |
| 3 | Wear changes the sheen; damp lifts seams; grime height varies per strip | ~10 ALU | **Built** |
| 4 | Each strip shows a different length of the roll (paper fetches only) | ~6 ALU | **Built** |
| 5 | The wall under the paper: new 1024 × 512 normal, 6 × 3 m, about 0.7 MB | 1 fetch, shared sampler | **Built** |
| 6 | Micro-shadowing from cavity on direct light | one more URP lighting fork | Not built: small alone, and a fork to keep in step |
| 7 | Shift and Exit become older paper (values only) | none | See §7 |
| 8 | Decal receiving + a procedural decal set | shader + renderer feature | Later |
| 9 | Ghost emboss dial | one 2048² BC5 | Later: changes what the moving print reads as |
| 10 | Seams kept off outside corners | vertex data from the map | Map line |
| 11 | Mesh flaps for lifted seams in deep zones | map + props | Map line |

**Rules every item keeps:**
- The print fetch, `PrintInk` and `CueInk` never change, so the print driver and the arrows land on the same pixels.
- Strips are not reverse-hung: the chevron is a matched pattern. (This list first said seams stay on the print's strip edges; §7 lifts that.)
- Every id is 192 m periodic (`strip & 255`, plane mod 768).
- Nothing lightens or emits: walls never glow.

**Not recommended:** POM or deeper emboss; moving seams to 1.37 m (a matched print cannot); reverse-hanging; warping the print for cockle (it shares its mapping with the cue); blur or misregistration inside the print; Toksvig; photo textures of any kind.

## 7. Sheets: what Red asked for next

Red, 2026-10-30 ~17:30: "目前看起来还不够像是真的墙纸，我在思考墙纸的话应该更像是那种一张张贴上去，（因为每一张墙纸的大小应该是有限的，有那种墙纸的接缝）我希望你research之后给我几个不同的方案". Then: "除此之外我也认为目前的墙纸看起来纹理比较少，表面特别平整光滑，所以看起来不太像是真的墙纸".

**Why our seams never showed.** K00 repeats every 0.375 m, so the 0.75 m tile holds two motifs. The old seam sits at uv integers, inside the vertical stripes at x 0–134 of 2048, where nothing can read. The plain field is x 433–999. A seam is only where the paper changes, so it can sit anywhere: §6's "seams stay on the strip edges" was too strict. One constant print shift per sheet moves the static print, the live array and the cue together, so a real pattern mismatch is safe too.

**What makes sheets readable at 2–5 m** (ranking and "est." are judgement). A tidy hairline falls under one pixel: at 1080p, 1 px ≈ 2.2 mm at 3 m and 3.6 mm at 5 m. So sheets only read if the wall shows errors of 3 mm or more.

1. **Pattern break at the seam:** 3–6 mm is allowed on modern contract goods; poor jobs exceed it (est. 10–25 mm). Chevrons show it worst.
2. **Open gap showing the wall:** under 1 mm to a few mm when fresh; est. 20–40 mm after decades.
3. **Lap band with a shadow:** up to 51 mm on old work; the ridge is one layer, up to 0.7 mm on vinyl.
4. **Lifted edge or flap:** seams lift and edges curl; est. 100–500 mm long.
5. **Dark seam grime:** est. 1–3 mm.
6. **Stepped ceiling line:** bias-printed stock drops about 6 mm per sheet.

| Seam | Line mm | What you see | Era | Job |
|---|---|---|---|---|
| Wide lap | up to 51 | Double band, hung away from the light to hide its shadow | Into the 1930s–40s, US South | Fast one-edge work |
| Narrow lap, pre-trimmed | est. 5–15 | Welt, gross mismatch | 1950s on, corners and amateurs (inferred) | Cheap; barred in modern spec |
| Wire lap | 1.6 | Thin raised line | Undated guide | Skilled |
| Butt | est. 0–0.5 | Nothing; a ridge if forced, a gap if loose | Pro standard from the 1920s | Pro |
| Undertrimmed | est. 1–3 | Slight doubled pattern | Factory fault | Any |
| Double-cut | hairline | No ridge | On plaster by 1946 | Pro |

**Mismatch:** 1/4 in (6 mm) up or down on modern goods, 1/8 in on one product. Paper stretches, so the match holds for the top 700–900 mm and then departs. Wet expansion of 1–2 % is 5–10 mm on a 520 mm strip. Lapping pre-trimmed paper shifts the pattern by the lap width.

**Horizontal joints.** The norm is floor-to-ceiling drops with no horizontal seam. The period basis for offcuts is "filling back" (1946): sheets split around doors and windows to save paper, adding vertical and sometimes horizontal seams. Railroaded goods, murals and repair patches also give horizontal joints. No 1970s–80s US source describes a cheap job, so a wall of short lengths is an art decision for this building, not a period fact.

**Ageing order (inferred, no source gives one):** white edges and curl at hanging; gaps as it dries; discolouring and fading with seam grime over years; mould at the vinyl–gypsum interface in humid buildings; wide gaps and curled strip ends over decades.

**Best visual reference:** the Terminus Hotel room (real bold geometric paper, closed 1983–2018): pale wall gaps, one top flap, curled short pieces under the window. No film or game artist was found discussing seams. The A24 film's corridor stills show no seams at web resolution.

## 7b. Surface textures of the period

Limits: no 1985–93 catalogue and no published emboss depth were found; CCC-W-408D was unreadable and ASTM F793 is paywalled. Fit (1–5), depths and every "est." are estimates.

| Type | Fit | Feature mm | Depth mm | At 2 m / 5 m | Notes |
|---|---|---|---|---|---|
| Sand / pebble | 5 | grain 0.7–3 est. | 0.05–0.15 est. | faint / sheen only | Seen on a chevron sample sold as the Backrooms paper (seller claim) |
| Linen emboss | 5 | pitch 0.8–1.25; slubs 10–40 est. | 0.05–0.2 est. | slubs / no | 1993 GenCorp weave patent |
| Burlap emboss | 4 | pitch 1.7 | 0.1–0.25 est. | weave / clumps | Real burlap as the analogue |
| Suede | 4 | mottle 20–80 est. | under 0.1 est. | yes / sheen | Wolf-Gordon best-seller |
| Stucco | 3 | 3–15 est. | 0.2–0.4 est. | yes / faint | Sold as "heavy depth" |
| Strié, grasscloth look | 3 | 1–3 × 30–200 est. | 0.1–0.2 est. | yes / streaks | No spec found |
| Natural grasscloth | 2 | reed 1–3 est. | 0.3–1 est. | yes / streaks | Cannot be scrubbed, so a poor office fit |
| Blown vinyl | 2 | varies | laminate 0.3–1.0 | yes / yes | Foam |
| Flock, foil | 1 | fibres 0.4–6 | about fibre height | pattern; mirror | 1970s |
| Lincrusta, Anaglypta | 1 | none found | none found | yes / yes | Declined |

- **Thickness bound:** Type II vinyl is 0.43–0.69 mm across embosses, so relief is at most about 0.25 mm (inference).
- **Scale in the game:** at 1080p and 75° a pixel covers 1.6 mm at 2 m and 4 mm at 5 m, so real threads are sub-pixel. A 5–30 mm band has to carry the read. That is a deliberate exaggeration: the prototype's 6 mm canvas and 9 mm burlap are three to five times the real pitch.
- **Sheen:** Imperial's 1987 matte topcoat gives 60° gloss of 8 or less. Glossy goods show flaws, embossed goods hide minor ones.
- **Emboss and print:** emboss may come before or after printing. The sample's grain runs through the ink: random, not in register. So the normal stays independent of the chevron, which the prototype already does. A height-linked ink speckle is a possible addition.
- **Picks:** sand/pebble as the base, linen, burlap, then a suede mottle (20–80 mm, low amplitude) to break the sheen at 5 m.
- **Game art:** no wallpaper-specific breakdown was found. General technique: bake normal variance into roughness mips, and add multi-scale macro layers.

## 8. Prototype

Clone `W/proj_wallmat`, Lobby paper only. Each option is a `look_<X>.txt` over the same shader. Sheet and surface are independent.

**Sheets** (`images/sheet_options_1030.jpg`): A hairline butt seam, pattern matched. B ordinary job: 1.4 mm seam, 2.5–4 mm mismatch, stronger sheet tone. C lapped: 13 mm lap, edge shadow, 4 mm mismatch. D short lengths 1.1–2.1 m with horizontal joints. E as B on 1.5 m sheets.

**Surfaces** (`images/texture_options_1030.jpg`): 1 fine linen (3 mm), 2 canvas (6 mm), 3 burlap (9 mm), 4 grasscloth (horizontal fibres), 5 pebbled vinyl (6–16 mm). Each replaces the paper's normal map, and the relief also goes into the tone, because the tilt alone does not read past 2 m under soft overhead light.

**Combinations:** `images/combo_options_1030.jpg`.

## 9. Red's pick and the package

Red, 2026-10-30 ~20:25: "C+2". Lapped sheets and the canvas emboss, plus a suede mottle (20–80 mm) in the substrate map. Before and after: `images/final_before_after_1030.jpg`.

| Control | Lobby | Shift | Exit |
|---|---|---|---|
| Seam position in the tile | 0.35 | 0.35 | 0.35 |
| Pattern mismatch mm | 4 | 6 | 3 |
| Lap mm | 13 | 13 | 13 |
| Seam dirt | .5 | .8 | .3 |
| Smoothness scale | 2.2 | 1.7 | 2 |
| Emboss strength / in the tone | 1 / 2 | .8 / 1.6 | 1 / 1.6 |
| Substrate strength / in the tone | .8 / 1.5 | 1.1 / 2.2 | .7 / 1.2 |

The weave is about three times the real pitch on purpose (§7b). WebGL compiles the substrate fetch out; the rest reaches it.

**Gate, on the clone synced to main a4e9844a:**
- Roslyn macOS + iOS 0 errors.
- P0: T2 PASS (specular independent of the print), Invariance PASS (non-wallpaper bit-identical), live = static. T1 parity no longer applies, because the paper changed on purpose.
- Variant compile: Metal/iOS and GLES3/WebGL OK.
- Walking shimmer, 3 mm a frame, second temporal difference against a no-emboss control: 1.00–1.07 ×. Film grain sets the floor, so a small effect could hide under it.

**Package:** `W/apply/wallmat_l0.sh` (11 paths), landed by 游戏视觉 2026-10-30 20:38.

**Follow-up checks, 2026-10-30 evening:**
- **POT paper set** (`W/apply/wallmat_pot.sh`): the 2048 × 3072 set stayed uncompressed under Unity 6, about 96 MB. At 2048 × 2048 it imports as BC7, about 17 MB by format. In game the difference from NPOT is 0.5–0.9 of 255 per channel, grain included.
- **Live cue** (`images/cue_on_sheets_1030.jpg`): through the driver's tool hooks in the real corridor. Stage 1 turned 6 cells, the chase wave reached 1571, no exceptions. The arrows read through the canvas, and a seam may cross an arrow.
- **GPU cost:** shipped shader against the pre-sheets shader, interleaved, same textures, M3 Max at 7680 × 3544 so the GPU is the limit: wall-filling frame +2.6 % (69.5 vs 67.7 ms), corridor +1.8 % (74.1 vs 72.9 ms). This is a Mac proxy. At iPhone size the Editor's CPU side hid the GPU, so that run was discarded.

**Still not checked:** frame time on a real phone (needs a device build, Red's call); phone texture format; Exit in game (Shift was seen only as its values on the corridor's material); the WebGL look (`briefs/webgl.md` Inbox).
