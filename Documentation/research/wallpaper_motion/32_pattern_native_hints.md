# Pattern-native hints: the wallpaper's own elements point the way

Status: analysis and proposal, 2026-10-03. Written by the narrative chat at Red's request. Nothing is implemented.
- It replaces the overlay glyph approach in `30_narrative_phosphor.md` A.6/A.11 and the concept renders GN07–GN09, if Red confirms.
- Mechanics stay with the wallpaper-print chat (`20_level_design_phosphor.md`, "LD").

## 1. What Red asked for

Red reviewed the overlay preview: pale chevrons pasted over WP03 (`research/wallpaper_motion/30_narrative_phosphor.md` GN07–GN08). Red's verdict: it is not good, because it reads as a separate layer and breaks immersion (跳戏).

**The request:** the hint should come from **the wallpaper pattern itself**. Its own graphic elements go through motion transitions: deformation, rotation and position change.

**The reference** is Red's Figma frame 2502:4565 ("vector wall:hard_edge", eight WP03 tiles). In one tile the grey band of a field chevron is turned 90° into a `>` pointing along the wall. Nothing is added: an element of the print has changed orientation.

**Reading:**
- The baseline is WP03 as printed: every field chevron points up.
- A hint is an anomaly in that baseline: an element that has turned, flattened or moved. This is The Exit 8's logic, noticing the changed sign, applied to the décor.
- No new shapes, no type, no glow overlay. If the player doesn't look, it is just wallpaper.

## 2. The vocabulary: what moves, and what each change means

WP03's elements (`Tools/print/patterns/hard_edge.py`), per 750 mm roll:
- **Stripes** (static): cream, pink, slate, hairlines. They hold the structure, the roll seams and the "every 3 m looks the same" rule, so they never move.
- **Field chevron stacks** (the main carriers): two per unit, half-dropped. The upper stack is grey/pink/slate; the lower is grey/cream. Each is 206.7 mm wide with 55° arms.
- **Motif-band arrows** (secondary carriers): small layered down-arrows with diamonds, 4 per roll.

The LD messages are kept (LD R4–R9). Each one is re-expressed as a transform of those elements:

| LD message | Transform of WP03's own elements | Motion type | Why it reads |
|---|---|---|---|
| GROUND | none: chevrons point up | — | The baseline |
| FLOW (way on) | The field chevrons on the route wall turn ±90° to point along the route | Rotation, plus a slight arm compression to fit the field (deformation) | An up-pointing print suddenly points somewhere |
| HERE (exit) | The chevrons in the fields either side of a door turn to face the door and converge | Rotation | The pattern frames the door |
| STOP (no exit) | The chevrons flatten: arm angle 55° → about 0°, into closed horizontal bars | Deformation | Keeps the "stop = closed bars" family (LD R4) |
| BREACH (dead door) | The chevron splits at the apex and the two arms slide apart | Deformation + translation | Broken |
| Pressure (chase) | The small motif-band arrows flip to point along the route, carried by the 8 m/s wave. The field chevrons follow the FLOW/HERE rule | Rotation + a travelling wave | The wave becomes visible motion in the paper |
| Forged FLOW (T4+, ON) | A turned chevron with the **band order wrong** (pink above grey) or a mirrored arm angle: a misprint | Wrong deformation | The close tell: the forger reprints by hand |

**Granularity and strength by tier** (an LD §6 lever). A 3 m face holds 4 rolls, so 8 field stacks per band. How many elements turn sets how readable the hint is from a distance:

| Tier | What turns | Read |
|---|---|---|
| T0 | Every lower-stack grey band on the face (8) | Readable at about 10–12 m |
| T2 | One per roll (4) | — |
| T4+ | A single element, as in Red's mock | Exit 8's pure anomaly register, near only; forgeries are added |

**Prototype loops:**
- `flow_turn`, `here_converge` and `stop_flatten`, built from hard_edge geometry.
- Scratchpad loops, to be published to Figma after Red confirms.
- In each, the turned chevrons stay inside their own field column, like a reprinted strip.

### 2b. Graphic rules from 平面视觉 (so it never breaks immersion)
1. **Only parts the print already has may move.** The stripes are the rails and never move. Changes stay inside one repeat and never cross a stripe.
2. **Every change must look like a real printing or paperhanging error, and be quantized:**
   - a 90° turn (Red's `>`);
   - a mirror flip;
   - two blocks swapped;
   - an offset by the pattern's own step;
   - one colour plate out of register by a few mm.
3. **No free deformation and no smooth scaling.** Those read as digital animation.
4. **Shape and colour never change.** A turned chevron keeps its stroke widths and its grey/pink/slate order. No new colour.
   - Up close it should look as if a 1990 paperhanger cut a piece crooked or hung it upside down. That is a real installation fault, and it fits "the building re-papers itself".
5. **The vocabulary, as geometric transforms of the original blocks:**
   - **turn:** one stack rotated 90° (way on);
   - **converge:** the blocks either side of a door mirrored to face it (exit);
   - **flatten:** the chevron's height taken to zero, a bar (dead end).
6. **Sparing use:** at most one change per roll strip, inside the 0.8–2.0 m eye band.

**Consequence for the motion (a decision for Red, §8):**
- Red asked for visible motion transitions (deformation, rotation, movement).
- 平面视觉's rules say the *states* must be quantized print errors.
- Proposed reconciliation:
  - the end states are quantized (turned / mirrored / flattened, at exact geometry);
  - the in-between is either hidden (unseen, or inside a lamp gasp) or shown as **discrete steps**: 0° → 45° → 90° in two snaps on the lamp's flicker, like a relay latching. That matches the Relay's latch motif (`research/hunter/03` §6).
- A smooth tween is used nowhere.

**Visual chat constraints:**
- the static/movable split must pass the P0 parity tests (T1/T2);
- every per-cell state is a pure function of (seed, cell, event counters), with no hidden state.

## 3. When the change happens (it must not be seen happening, except once)

LD's commit gate (R10) and the print rules (`10_synthesis.md` §5) already give the timing:
1. **Unseen.** A cell's wall changes only while that cell has been out of view for at least 0.5 s.
2. **In the lamp's gasp.** A failing or dying lamp's dropout masks the change. You see the wall before the flicker and after it, never during: the Haunted Mansion portraits.
   - This is the new meaning of the lamp link. **Hints appear where lamps fail or die**, because those are the only places the paper can move unwatched.
3. **At a sag or wave front** (LD §4): the self-test sag and the chase wave. The wave is the one place where the turning travels visibly along the corridor at 8 m/s, as a rehung ripple.
4. **The first-dark beat is the one in-view turn.** A lamp ahead dies in three dips, and across the dips the chevrons on that wall visibly rotate to point on. This teaches the rule: "when the light goes, the paper turns".

Transition shapes:

| Context | Duration |
|---|---|
| Inside a gasp | 0.3–0.6 s |
| Unseen | Instant |
| Wave | 0.4 s per field, staggered 60–120 ms (like `10_synthesis.md`'s strip wipe) |

Changes are smooth, with at most 3 changes per second per wall (photosafety).

## 4. The phosphor glow: keep it as one of the print's own inks (recommended), or drop it

The overlay layer goes away. Two options:

| Option | Look in a dead cell | Fit |
|---|---|---|
| **A. One print ink is ZnS:Cu** (narrative's preference: the cream ink; 平面视觉 advises no glow at all, rule 4) | In the dark, the cream bands and stripes glow pale yellow-green (平面视觉's colour). The turned chevron, which has cream in its lower stack, shows as a glowing turned shape. It is the décor's own shapes, nothing added | Keeps Red's glow-ink decision. Period-correct (a 1990 phosphor ink in a decorative print). Dark cells stay readable |
| B. No glow | Dead cells read only by spill from neighbours; hints live in failing cells | Simplest. Loses the "where the light dies, the paper points" beat in fully dark cells |

## 5. How it is built (owners)

**Recommended: a hybrid print**, refined by the wallpaper-print chat's generator notes:
- **Turns (FLOW, HERE, pressure) are a UV rotation of a per-element sprite.**
  - The shader finds the element slot from the tile-local position, rotates that slot's local UV by θ(cell, progress), and samples one sprite exported from `hard_edge.py`.
  - Turning is smooth and continuous, keeps mip anti-aliasing, and costs one extra fetch on element pixels only.
- **Flatten (STOP) and split (BREACH) are not rotations.** They need either a few sprite variants with a crossfade inside the masking gasp, or analytic polygons from the element table (fwidth AA).
- **Fit rule.** A whole stack is about 207 × 330 mm, so turned it would overrun the 207 mm field into the stripes. Red's mock turns only the **grey band of the lower chevron**, which fits. Proposed:
  - the turning part is the lower stack's grey band, or a single band;
  - or the whole stack scales to about 0.62 while it turns;
  - never let a turned shape cross into the stripes.
- **Direction per face.** The print's PlanarFrame u is mirrored between the two sides of a wall, so θ must come from the world-axis FaceSign (LD §9), never from print u. Otherwise a `>` points the wrong way on one side.

The generator can export:
- (a) the element table: 2 field stacks + 8 motif arrows per roll, anchors and polygons in mm;
- (b) the static layer with those elements removed;
- (c) per-element sprites, 4× supersampled.

All three come from the same vector, so the parts register exactly.

**The sections below are the first draft:** This is the 10_synthesis.md "procedural SDF" fallback, which suits the hard-edge vector design that Red chose.
- **Static layer (texture):** stripes plus the motif-band background, exported by `hard_edge.py` without the movable elements. It is the existing `_FR_Print` path at 2048².
- **Movable elements (procedural, in the wall shader):** the field chevron stacks and motif arrows, drawn from a small element table that `hard_edge.py` exports (field x-range, apex, band list with ink and thickness, arm angle, unit drop).
  - For each wall pixel, the shader finds the element instance, applies the inverse transform, and evaluates the band SDF to get an ink id and then the material palette. It anti-aliases with `fwidth`.
  - Per-cell transform parameters come from `_FR_CellState`: rotation θ, arm scale (flatten), split, slide and transition progress. The state texture already reserves R for the print.
  - Cost: a few ALU per pixel and no extra texture fetch. Infinitely sharp, so no new flipbook slices and no memory cost.
- **Rejected: flipbook variants** (pre-rendered slices for every turn, converge and flatten at 2048²). That means too many slices and the whole print duplicated.

| Owner | Work |
|---|---|
| Wallpaper-print chat | Revise LD R4/R11 (messages become transforms; drop InkShape glyphs and `_FR_InkType`); export the static layer + element table from `hard_edge.py`; design the transition motion; the forgery rule |
| Visual chat (游戏视觉) | The procedural element pass in FrontRooms/Surface under `_FR_PRINT`; the state-texture fields; option A's phosphor ink (cream emission gated by LD R1) |
| Map chat (关卡设计) | `FrontRoomsWayfinding` already assigns messages per cell (LD §11). It now writes transform targets and directions instead of glyph codes. Commit gate unchanged |
| 平面视觉 | The transformation vocabulary as graphic design, so it still reads as a believable 1990 print: which elements may turn, how far, and the flatten/split shapes. The placard legend becomes turned / converged / flattened chevrons |
| Narrative | The fiction update (§6) and retiring the micro-type strings |

## 5b. The placard under the new scheme (it is being built now)

The visual chat is building the Red-approved placard with an InkShape glyph legend. That legend must follow the new language, so Red should decide both together.
- **Legend `IN POWER FAILURE`** becomes three rows of WP03's own elements, drawn at the placard's declared scale:
  - a turned stack: `WAY ON`;
  - a mirrored pair facing a door: `EXIT`;
  - a flattened bar: `NO EXIT`.
- **Lighting:** under light the placard teaches the baseline (chevrons up) beside the three changed states. Under option A the cream ink of the legend glows in the dark; under option B the legend reads only under light.
- **Unchanged:** the plan, the title block (LEVEL 0 | SHEET A-2 OF 4 | PRINTED 03/90), `YOU ARE HERE` left deliberately in the start room, and the footer.

## 6. What changes in the fiction (EGRESS stays)

**The fiction:**
- The building still re-papers itself unseen (canon: the Backrooms shifts when unobserved), and the plan is still always current.
- The difference: **the egress plan is no longer a hidden underprint. It is the décor's orientation.** The mill printed the pattern so it could point. When the building redraws its plan, the chevrons are reprinted turned.
- The phosphor (option A) is the cream ink: "invisible by day, there when the power fails".

**What it retires:**
- the micro-type strings (THIS WAY OUT…, A.11 layers 0–7);
- the hand-lettered forgery (layer 8);
- the type-based roll stamp.

**Proposed to keep:**
- the T3 scratch-throughs (rare words scratched through the paper, a physical mark in albedo, not glow);
- the placard, with its legend redrawn as chevron transforms;
- the OCCUPANT LOAD title block, but only on the placard.

## 7. Risks
- **Subtlety.** Pattern hints are easy to miss. That is the intent (anomaly literacy), but LD's follow-rate targets (§13) must be re-measured. Mitigations:
  - the first-dark beat shows the turn in view;
  - T0 turns both stacks;
  - whole-wall turns, never a single field.
- **Distance.** A field chevron is about 0.2 m wide, about 14 px at 10 m (1080p, FOV 76°). Turned versus up reads at 10–12 m; end walls at 20 m need the whole wall turned.
- **WP03's own up-chevrons look like arrows already.** The hint must be a change against that baseline (sideways, converging, flattened), never "up", which is the default.
- **Determinism and commit rules** carry over from LD unchanged.

## 8. Decisions for Red
1. Confirm the direction: pattern-native transforms replace the overlay glyphs and micro-type.
2. **Motion:** quantized states with hidden or stepped in-betweens (平面视觉's rule, recommended), or visible smooth transitions (Red's original wording)?
3. Glow: option A (the cream ink is phosphorescent) or B (no glow, 平面视觉's advice)?
4. Keep the scratch-throughs, and redraw the placard legend as in §5b?
5. After confirmation, I replace GN07–GN09 in Figma with pattern-native renders and loops, and hand §5 to the owners.
